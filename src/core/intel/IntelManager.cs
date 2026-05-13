using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 规律知识状态：玩家对空海规律的观测程度。
/// </summary>
public enum PatternState
{
	/// <summary>尚未触发任何观测事件，规律不出现在日志中。</summary>
	Undiscovered = 0,
	/// <summary>累计观测分数达到 partial_threshold，规律模糊可见。</summary>
	PartiallyObserved = 1,
	/// <summary>累计观测分数达到 confirmation_threshold，基础机械收益激活。</summary>
	Confirmed = 2,
}

/// <summary>
/// 地点/航线/实体的知识状态：玩家对某处的了解程度。
/// </summary>
public enum LocationKnowledgeState
{
	/// <summary>从未获得任何情报，实体在航图中不可见。</summary>
	Unknown = 0,
	/// <summary>收到置信度不足的传闻，显示虚线轮廓和部分风险标签。</summary>
	Rumored = 1,
	/// <summary>获得可靠情报或消费了情报条目，实体完全可见。</summary>
	Identified = 2,
	/// <summary>玩家亲身到访，最终状态，不可降级。</summary>
	Verified = 3,
}

/// <summary>
/// 传闻来源记录（每个来源独立保留）。
/// </summary>
public sealed class RumorSource
{
	/// <summary>来源标签，如 "old-harbormaster"。</summary>
	public string SourceTag { get; }
	/// <summary>风险标签列表。</summary>
	public IReadOnlyList<string> HazardTags { get; }
	/// <summary>置信度 [0, 100]。</summary>
	public int Confidence { get; }
	/// <summary>置信度文本映射。</summary>
	public string ConfidenceLabel => Confidence <= 33 ? "不确定" : Confidence <= 66 ? "可靠" : "权威";

	/// <param name="sourceTag">来源标签。</param>
	/// <param name="hazardTags">风险标签。</param>
	/// <param name="confidence">置信度 [0, 100]。</param>
	public RumorSource(string sourceTag, IReadOnlyList<string> hazardTags, int confidence)
	{
		SourceTag = sourceTag;
		HazardTags = hazardTags.ToArray(); // 防御性复制，保证不可变性
		Confidence = Math.Clamp(confidence, 0, 100);
	}
}

/// <summary>
/// 规律状态快照（供查询使用）。
/// </summary>
public sealed class PatternSnapshot
{
	/// <summary>规律 ID。</summary>
	public string PatternId { get; }
	/// <summary>当前状态。</summary>
	public PatternState State { get; }
	/// <summary>累计观测分数。</summary>
	public int ObservationScore { get; }
	/// <summary>已触发的唯一事件 ID 集合（只读副本）。</summary>
	public IReadOnlySet<string> TriggeredEvents { get; }
	/// <summary>是否已设置 pattern_usage_success。</summary>
	public bool PatternUsageSuccess { get; }

	/// <param name="patternId">规律 ID。</param>
	/// <param name="state">当前状态。</param>
	/// <param name="score">观测分数。</param>
	/// <param name="events">已触发事件集合。</param>
	/// <param name="usageSuccess">是否激活 confirmed+。</param>
	public PatternSnapshot(string patternId, PatternState state, int score,
		IReadOnlySet<string> events, bool usageSuccess)
	{
		PatternId = patternId;
		State = state;
		ObservationScore = score;
		TriggeredEvents = events;
		PatternUsageSuccess = usageSuccess;
	}
}

/// <summary>
/// 地点知识快照（供查询使用）。
/// </summary>
public sealed class LocationSnapshot
{
	/// <summary>地点 ID。</summary>
	public string LocationId { get; }
	/// <summary>当前知识状态。</summary>
	public LocationKnowledgeState State { get; }
	/// <summary>所有传闻来源（各来源独立保留）。</summary>
	public IReadOnlyList<RumorSource> RumorSources { get; }

	/// <param name="locationId">地点 ID。</param>
	/// <param name="state">知识状态。</param>
	/// <param name="rumorSources">传闻来源列表。</param>
	public LocationSnapshot(string locationId, LocationKnowledgeState state,
		IReadOnlyList<RumorSource> rumorSources)
	{
		LocationId = locationId;
		State = state;
		RumorSources = rumorSources;
	}
}

/// <summary>
/// Intel / Knowledge System Autoload #6。
/// 追踪玩家对空海世界的规律知识（Story 001）和地点知识（Story 002）。
/// 唯一真相源——下游系统只读查询，不得自行缓存状态。
/// </summary>
public sealed class IntelManager
{
	// ── 规律状态机常量 ──────────────────────────────────────────────
	/// <summary>默认 partial 阈值：observation_score ≥ 5 → PartiallyObserved。</summary>
	public const int DefaultPartialThreshold = 5;
	/// <summary>默认 confirmation 阈值：observation_score ≥ 10 → Confirmed。</summary>
	public const int DefaultConfirmationThreshold = 10;

	// 事件权重（ADR-0007）
	private const int WeightNarrativeHint = 1;
	private const int WeightLogFragment = 2;
	private const int WeightPartnerComment = 3;
	private const int WeightPassiveObservation = 4;
	private const int WeightActiveInvestigation = 7;

	// ── 地点知识常量 ──────────────────────────────────────────────
	/// <summary>置信度 ≥ 67 视为权威来源，直接提升至 Identified。</summary>
	public const int AuthorityConfidenceThreshold = 67;

	// ── 内部数据结构 ──────────────────────────────────────────────
	// Pattern: patternId → PatternEntry
	private readonly Dictionary<string, PatternEntry> _patterns =
		new(StringComparer.Ordinal);

	// Pattern 阈值覆盖: patternId → (partial, confirmation)
	private readonly Dictionary<string, (int Partial, int Confirmation)> _thresholdOverrides =
		new(StringComparer.Ordinal);

	// 事件权重表: patternId → (eventId → weight)
	private readonly Dictionary<string, Dictionary<string, int>> _eventWeightTable =
		new(StringComparer.Ordinal);

	// Location: locationId → LocationEntry
	private readonly Dictionary<string, LocationEntry> _locations =
		new(StringComparer.Ordinal);

	// ── 事件（Story 001 — Pattern）──────────────────────────────
	/// <summary>
	/// 观测事件触发时发出，参数为 (patternId, eventId, newScore)。
	/// </summary>
	public event Action<string, string, int>? PatternObserved;

	/// <summary>
	/// 规律状态变更时发出，参数为 (patternId, oldState, newState)。
	/// </summary>
	public event Action<string, PatternState, PatternState>? PatternStateChanged;

	/// <summary>
	/// pattern_usage_success 触发后达到 confirmed+ 时发出，参数为 patternId。
	/// </summary>
	public event Action<string>? PatternUsageConfirmed;

	// ── 事件（Story 002 — Location）──────────────────────────────
	/// <summary>
	/// 收到传闻时发出，参数为 (locationId, sourceTag)。
	/// </summary>
	public event Action<string, string>? RumorReceived;

	/// <summary>
	/// 地点知识状态推进时发出，参数为 (locationId, oldState, newState)。
	/// </summary>
	public event Action<string, LocationKnowledgeState, LocationKnowledgeState>? KnowledgeAdvanced;

	// ── 初始化 ────────────────────────────────────────────────────
	/// <summary>管理器是否已完成初始化。</summary>
	public bool IsInitialized { get; private set; }

	/// <summary>
	/// 注册规律的事件权重表。应在 Initialize() 前调用。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	/// <param name="eventWeights">事件 ID → 权重 的映射。</param>
	public void RegisterPatternEventWeights(string patternId, IReadOnlyDictionary<string, int> eventWeights)
	{
		var weights = new Dictionary<string, int>(eventWeights, StringComparer.Ordinal);
		_eventWeightTable[patternId] = weights;
	}

	/// <summary>
	/// 为规律设置阈值覆盖。partial 必须严格小于 confirmation。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	/// <param name="partialThreshold">partial 覆盖阈值。</param>
	/// <param name="confirmationThreshold">confirmation 覆盖阈值。</param>
	public void SetThresholdOverride(string patternId, int partialThreshold, int confirmationThreshold)
	{
		if (partialThreshold >= confirmationThreshold)
			throw new ArgumentException(
				$"partial_threshold ({partialThreshold}) 必须严格小于 confirmation_threshold ({confirmationThreshold})");
		_thresholdOverrides[patternId] = (partialThreshold, confirmationThreshold);
	}

	/// <summary>
	/// 批量写入初始地点知识状态（用于新游戏引导数据）。
	/// 仅当目标地点尚未存在时写入。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	/// <param name="initialState">初始知识状态。</param>
	/// <param name="sourceTag">来源标签（可选）。</param>
	public void SeedLocationKnowledge(string locationId, LocationKnowledgeState initialState,
		string sourceTag = "空港基础航图")
	{
		if (_locations.ContainsKey(locationId))
			return;
		var entry = new LocationEntry(initialState);
		if (initialState != LocationKnowledgeState.Unknown)
		{
			entry.AddRumorSource(new RumorSource(sourceTag, Array.Empty<string>(), 100));
		}
		_locations[locationId] = entry;
	}

	/// <summary>
	/// 完成初始化，验证阈值配置。
	/// </summary>
	public void Initialize()
	{
		ValidateThresholds();
		IsInitialized = true;
	}

	// ── Story 001 — Pattern State Machine ──────────────────────────

	/// <summary>
	/// 上报规律观测事件。同一事件 ID 仅计首次触发，重复调用不累分。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	/// <param name="eventId">事件 ID。</param>
	public void ReportObservationEvent(string patternId, string eventId)
	{
		if (string.IsNullOrEmpty(patternId) || string.IsNullOrEmpty(eventId))
			return;

		if (!_eventWeightTable.ContainsKey(patternId))
		{
			// 未注册的规律：惰性初始化，权重 0——保证不崩溃
		}

		var entry = GetOrInitPattern(patternId);

		if (entry.TriggeredEvents.Contains(eventId))
			return; // 去重：同一事件仅计首次

		int weight = GetEventWeight(patternId, eventId);
		var oldState = ComputePatternState(entry.ObservationScore, patternId);

		entry.TriggeredEvents.Add(eventId);
		entry.ObservationScore += weight;

		var newState = ComputePatternState(entry.ObservationScore, patternId);
		PatternObserved?.Invoke(patternId, eventId, entry.ObservationScore);

		if (newState != oldState)
		{
			PatternStateChanged?.Invoke(patternId, oldState, newState);
			// usage_success 已提前设置但当时未达 Confirmed，状态变为 Confirmed 时补发信号
			if (newState == PatternState.Confirmed && entry.PatternUsageSuccess)
				PatternUsageConfirmed?.Invoke(patternId);
		}
	}

	/// <summary>
	/// 上报规律成功使用。仅当规律已达 Confirmed 时激活 confirmed+。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	public void ReportPatternUsageSuccess(string patternId)
	{
		var entry = GetOrInitPattern(patternId);
		bool wasConfirmedPlus = IsConfirmedPlus(patternId);
		entry.PatternUsageSuccess = true;

		if (!wasConfirmedPlus && IsConfirmedPlus(patternId))
			PatternUsageConfirmed?.Invoke(patternId);
	}

	/// <summary>
	/// 计算规律当前状态（仅基于观测分数，不修改状态）。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	public PatternState GetPatternState(string patternId)
	{
		var entry = _patterns.TryGetValue(patternId, out var e) ? e : null;
		int score = entry?.ObservationScore ?? 0;
		return ComputePatternState(score, patternId);
	}

	/// <summary>
	/// 是否已达到 confirmed+（Confirmed 且 pattern_usage_success = true）。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	public bool IsConfirmedPlus(string patternId)
	{
		var entry = _patterns.TryGetValue(patternId, out var e) ? e : null;
		if (entry == null) return false;
		return ComputePatternState(entry.ObservationScore, patternId) == PatternState.Confirmed
			&& entry.PatternUsageSuccess;
	}

	/// <summary>
	/// 返回所有状态为 PartiallyObserved 或 Confirmed 的规律快照列表（供日志 UI 使用）。
	/// Undiscovered 规律不出现在列表中以保持神秘感。
	/// </summary>
	/// <returns>只读快照列表，每项为一个 <see cref="PatternSnapshot"/>；列表本身是新分配的副本，可安全迭代。</returns>
	public IReadOnlyList<PatternSnapshot> GetPatternLog()
	{
		var result = new List<PatternSnapshot>();
		foreach (var (id, entry) in _patterns)
		{
			var state = ComputePatternState(entry.ObservationScore, id);
			if (state == PatternState.Undiscovered)
				continue;
			result.Add(new PatternSnapshot(id, state, entry.ObservationScore,
				entry.TriggeredEvents, entry.PatternUsageSuccess));
		}
		return result;
	}

	/// <summary>
	/// 返回指定规律的完整快照。规律不存在时返回 null。
	/// </summary>
	/// <param name="patternId">规律 ID。</param>
	public PatternSnapshot? GetPatternSnapshot(string patternId)
	{
		if (!_patterns.TryGetValue(patternId, out var entry))
			return null;
		var state = ComputePatternState(entry.ObservationScore, patternId);
		return new PatternSnapshot(patternId, state, entry.ObservationScore,
			entry.TriggeredEvents, entry.PatternUsageSuccess);
	}

	// ── Story 002 — Location Knowledge State Machine ───────────────

	/// <summary>
	/// 查询地点当前知识状态。未初始化的地点返回 Unknown。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	public LocationKnowledgeState QueryKnowledgeState(string locationId)
	{
		return _locations.TryGetValue(locationId, out var entry)
			? entry.State
			: LocationKnowledgeState.Unknown;
	}

	/// <summary>
	/// 查询地点知识快照（含所有来源）。未初始化的地点返回 Unknown 快照。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	public LocationSnapshot QueryLocationSnapshot(string locationId)
	{
		if (!_locations.TryGetValue(locationId, out var entry))
			return new LocationSnapshot(locationId, LocationKnowledgeState.Unknown,
				Array.Empty<RumorSource>());
		return new LocationSnapshot(locationId, entry.State, entry.GetRumorSourcesCopy());
	}

	/// <summary>
	/// 写入传闻。confidence &lt; 67 → Rumored；confidence ≥ 67 → Identified。
	/// Verified 终态拒绝传闻；同一 source_tag 不追加重复记录。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	/// <param name="sourceTag">来源标签。</param>
	/// <param name="hazardTags">风险标签列表。</param>
	/// <param name="confidence">置信度 [0, 100]，自动钳制。</param>
	public void RevealRumor(string locationId, string sourceTag,
		IReadOnlyList<string> hazardTags, int confidence)
	{
		var entry = GetOrInitLocation(locationId);

		if (entry.State == LocationKnowledgeState.Verified)
			return; // 终态，静默拒绝

		// 去重：同一 source_tag 不追加
		if (entry.HasRumorSource(sourceTag))
			return;

		int clampedConf = Math.Clamp(confidence, 0, 100);
		entry.AddRumorSource(new RumorSource(sourceTag, hazardTags, clampedConf));
		RumorReceived?.Invoke(locationId, sourceTag);

		LocationKnowledgeState targetState = clampedConf >= AuthorityConfidenceThreshold
			? LocationKnowledgeState.Identified
			: LocationKnowledgeState.Rumored;

		TryAdvanceLocation(entry, locationId, targetState);
	}

	/// <summary>
	/// 消费情报条目推进地点知识（Unknown/Rumored → Identified）。
	/// Identified 和 Verified 状态不受影响。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	/// <returns>推进前后的状态变化记录，未推进时返回 null。</returns>
	public (LocationKnowledgeState OldState, LocationKnowledgeState NewState)?
		ConsumeIntel(string locationId)
	{
		var entry = GetOrInitLocation(locationId);
		if (entry.State >= LocationKnowledgeState.Identified)
			return null;

		var oldState = entry.State;
		entry.State = LocationKnowledgeState.Identified;
		KnowledgeAdvanced?.Invoke(locationId, oldState, LocationKnowledgeState.Identified);
		return (oldState, LocationKnowledgeState.Identified);
	}

	/// <summary>
	/// 玩家到达地点，直接推进至 Verified（包含开拓者路径 Unknown → Verified）。
	/// 已是 Verified 时静默忽略，不重复发出信号。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	public void PlayerArrivedAt(string locationId)
	{
		var entry = GetOrInitLocation(locationId);
		if (entry.State == LocationKnowledgeState.Verified)
			return;

		var oldState = entry.State;
		entry.State = LocationKnowledgeState.Verified;
		entry.AddRumorSource(new RumorSource("亲身探索", Array.Empty<string>(), 100));
		KnowledgeAdvanced?.Invoke(locationId, oldState, LocationKnowledgeState.Verified);
	}

	// ── 私有辅助 ──────────────────────────────────────────────────

	private PatternEntry GetOrInitPattern(string patternId)
	{
		if (!_patterns.TryGetValue(patternId, out var entry))
		{
			entry = new PatternEntry();
			_patterns[patternId] = entry;
		}
		return entry;
	}

	private LocationEntry GetOrInitLocation(string locationId)
	{
		if (!_locations.TryGetValue(locationId, out var entry))
		{
			entry = new LocationEntry(LocationKnowledgeState.Unknown);
			_locations[locationId] = entry;
		}
		return entry;
	}

	private PatternState ComputePatternState(int score, string patternId)
	{
		int partial = DefaultPartialThreshold;
		int confirmation = DefaultConfirmationThreshold;
		if (_thresholdOverrides.TryGetValue(patternId, out var ov))
		{
			partial = ov.Partial;
			confirmation = ov.Confirmation;
		}

		if (score >= confirmation) return PatternState.Confirmed;
		if (score >= partial) return PatternState.PartiallyObserved;
		return PatternState.Undiscovered;
	}

	private int GetEventWeight(string patternId, string eventId)
	{
		if (_eventWeightTable.TryGetValue(patternId, out var eventMap)
			&& eventMap.TryGetValue(eventId, out int w))
			return w;
		return 0;
	}

	private void TryAdvanceLocation(LocationEntry entry, string locationId,
		LocationKnowledgeState targetState)
	{
		// 非降级保护：只允许数值递增
		if ((int)targetState <= (int)entry.State)
			return;

		var oldState = entry.State;
		entry.State = targetState;
		KnowledgeAdvanced?.Invoke(locationId, oldState, targetState);
	}

	private void ValidateThresholds()
	{
		foreach (var (patternId, ov) in _thresholdOverrides)
		{
			if (ov.Partial >= ov.Confirmation)
				throw new InvalidOperationException(
					$"规律 {patternId}: partial_threshold ({ov.Partial}) >= confirmation_threshold ({ov.Confirmation})，partially_observed 状态不可达");
		}
	}

	// ── 私有内部类 ────────────────────────────────────────────────

	private sealed class PatternEntry
	{
		public int ObservationScore { get; set; }
		public HashSet<string> TriggeredEvents { get; } = new(StringComparer.Ordinal);
		public bool PatternUsageSuccess { get; set; }
	}

	private sealed class LocationEntry
	{
		private readonly List<RumorSource> _rumorSources = new();

		public LocationKnowledgeState State { get; set; }

		public LocationEntry(LocationKnowledgeState initialState) => State = initialState;

		public bool HasRumorSource(string sourceTag)
		{
			foreach (var s in _rumorSources)
				if (string.Equals(s.SourceTag, sourceTag, StringComparison.Ordinal))
					return true;
			return false;
		}

		public void AddRumorSource(RumorSource source) => _rumorSources.Add(source);

		public IReadOnlyList<RumorSource> GetRumorSourcesCopy() =>
			_rumorSources.AsReadOnly();
	}
}
