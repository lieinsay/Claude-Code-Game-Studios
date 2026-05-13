using System;
using System.Collections.Generic;
using System.Linq;

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
/// 能力解锁状态。
/// </summary>
public enum AbilityState
{
	/// <summary>未解锁，对应机械效果不可用。</summary>
	Locked = 0,
	/// <summary>已解锁，永久有效，不可逆转。</summary>
	Unlocked = 1,
}

/// <summary>
/// 能力解锁路径配置（数据驱动，由 Registry 加载）。
/// </summary>
public sealed class AbilityPathConfig
{
	/// <summary>能力 ID。</summary>
	public string AbilityId { get; }
	/// <summary>所有解锁路径，OR 逻辑：任意一条满足即解锁。</summary>
	public IReadOnlyList<AbilityUnlockPath> Paths { get; }

	/// <param name="abilityId">能力 ID。</param>
	/// <param name="paths">解锁路径列表。</param>
	public AbilityPathConfig(string abilityId, IReadOnlyList<AbilityUnlockPath> paths)
	{
		AbilityId = abilityId;
		Paths = paths;
	}
}

/// <summary>
/// 单条解锁路径：包含多个条件，AND 逻辑——全部满足才算路径满足。
/// </summary>
public sealed class AbilityUnlockPath
{
	/// <summary>路径 ID，如 "path_a_pattern_confirmed"。</summary>
	public string PathId { get; }
	/// <summary>路径内所有条件。</summary>
	public IReadOnlyList<AbilityCondition> Conditions { get; }

	/// <param name="pathId">路径 ID。</param>
	/// <param name="conditions">条件列表。</param>
	public AbilityUnlockPath(string pathId, IReadOnlyList<AbilityCondition> conditions)
	{
		PathId = pathId;
		Conditions = conditions;
	}
}

/// <summary>
/// 单个解锁条件，类型化参数字典。
/// </summary>
public sealed class AbilityCondition
{
	/// <summary>条件类型，对应 evaluator 键名。</summary>
	public string Type { get; }
	/// <summary>条件参数（类型化，避免裸 object 载荷）。</summary>
	public IReadOnlyDictionary<string, object> Params { get; }

	/// <param name="type">条件类型。</param>
	/// <param name="parameters">条件参数。</param>
	public AbilityCondition(string type, IReadOnlyDictionary<string, object> parameters)
	{
		Type = type;
		Params = parameters;
	}
}

/// <summary>
/// intel 条目静态定义（由 Registry 提供）。
/// </summary>
public sealed class IntelDefinition
{
	/// <summary>情报 ID。</summary>
	public string IntelId { get; }
	/// <summary>玩家可见名称。</summary>
	public string DisplayName { get; }
	/// <summary>关联地点/航线 ID 列表（消费时推进知识状态）。</summary>
	public IReadOnlyList<string> LinkedContentIds { get; }
	/// <summary>关联规律 ID 列表（消费时添加 log_fragment 事件）。</summary>
	public IReadOnlyList<string> LinkedPatterns { get; }
	/// <summary>对关联规律添加的事件 ID（权重固定为 log_fragment=2）。</summary>
	public string PatternEventId { get; }
	/// <summary>消费后需要检查解锁条件的能力 ID 列表。</summary>
	public IReadOnlyList<string> UnlockConditionForAbilities { get; }

	/// <param name="intelId">情报 ID。</param>
	/// <param name="displayName">显示名称。</param>
	/// <param name="linkedContentIds">关联地点/航线 ID。</param>
	/// <param name="linkedPatterns">关联规律 ID。</param>
	/// <param name="patternEventId">规律事件 ID。</param>
	/// <param name="unlockConditionForAbilities">能力 ID 列表。</param>
	public IntelDefinition(string intelId, string displayName,
		IReadOnlyList<string> linkedContentIds,
		IReadOnlyList<string> linkedPatterns,
		string patternEventId,
		IReadOnlyList<string> unlockConditionForAbilities)
	{
		IntelId = intelId;
		DisplayName = displayName;
		LinkedContentIds = linkedContentIds;
		LinkedPatterns = linkedPatterns;
		PatternEventId = patternEventId;
		UnlockConditionForAbilities = unlockConditionForAbilities;
	}
}

/// <summary>
/// consume_intel() 的返回结果（Story 004）。
/// </summary>
public sealed class IntelConsumeResult
{
	/// <summary>是否成功消费。</summary>
	public bool Success { get; }
	/// <summary>错误码，成功时为空字符串。</summary>
	public string ErrorCode { get; }
	/// <summary>消费的情报 ID。</summary>
	public string IntelId { get; }
	/// <summary>情报显示名称。</summary>
	public string IntelDisplayName { get; }
	/// <summary>地点知识状态推进记录列表。</summary>
	public IReadOnlyList<LocationAdvancement> LocationAdvancements { get; }
	/// <summary>能力解锁记录列表。</summary>
	public IReadOnlyList<AbilityUnlockRecord> AbilityUnlocks { get; }
	/// <summary>规律观测添加记录列表。</summary>
	public IReadOnlyList<PatternObservationRecord> PatternObservations { get; }

	/// <param name="success">是否成功。</param>
	/// <param name="errorCode">错误码。</param>
	/// <param name="intelId">情报 ID。</param>
	/// <param name="intelDisplayName">显示名称。</param>
	/// <param name="locationAdvancements">地点推进列表。</param>
	/// <param name="abilityUnlocks">能力解锁列表。</param>
	/// <param name="patternObservations">规律观测列表。</param>
	public IntelConsumeResult(bool success, string errorCode, string intelId,
		string intelDisplayName,
		IReadOnlyList<LocationAdvancement> locationAdvancements,
		IReadOnlyList<AbilityUnlockRecord> abilityUnlocks,
		IReadOnlyList<PatternObservationRecord> patternObservations)
	{
		Success = success;
		ErrorCode = errorCode;
		IntelId = intelId;
		IntelDisplayName = intelDisplayName;
		LocationAdvancements = locationAdvancements;
		AbilityUnlocks = abilityUnlocks;
		PatternObservations = patternObservations;
	}

	/// <summary>构建失败结果的工厂方法。</summary>
	/// <param name="intelId">情报 ID。</param>
	/// <param name="errorCode">错误码。</param>
	/// <returns>失败结果实例。</returns>
	public static IntelConsumeResult Fail(string intelId, string errorCode) =>
		new(false, errorCode, intelId, "",
			Array.Empty<LocationAdvancement>(),
			Array.Empty<AbilityUnlockRecord>(),
			Array.Empty<PatternObservationRecord>());
}

/// <summary>地点知识推进记录。</summary>
public sealed class LocationAdvancement
{
	/// <summary>地点 ID。</summary>
	public string LocationId { get; }
	/// <summary>推进前状态。</summary>
	public LocationKnowledgeState PreviousState { get; }
	/// <summary>推进后状态。</summary>
	public LocationKnowledgeState NewState { get; }

	/// <param name="locationId">地点 ID。</param>
	/// <param name="previousState">推进前状态。</param>
	/// <param name="newState">推进后状态。</param>
	public LocationAdvancement(string locationId,
		LocationKnowledgeState previousState, LocationKnowledgeState newState)
	{
		LocationId = locationId;
		PreviousState = previousState;
		NewState = newState;
	}
}

/// <summary>能力解锁记录。</summary>
public sealed class AbilityUnlockRecord
{
	/// <summary>能力 ID。</summary>
	public string AbilityId { get; }
	/// <summary>能力显示名称。</summary>
	public string AbilityDisplayName { get; }
	/// <summary>触发解锁的路径 ID。</summary>
	public string UnlockPath { get; }

	/// <param name="abilityId">能力 ID。</param>
	/// <param name="abilityDisplayName">显示名称。</param>
	/// <param name="unlockPath">路径 ID。</param>
	public AbilityUnlockRecord(string abilityId, string abilityDisplayName, string unlockPath)
	{
		AbilityId = abilityId;
		AbilityDisplayName = abilityDisplayName;
		UnlockPath = unlockPath;
	}
}

/// <summary>规律观测添加记录。</summary>
public sealed class PatternObservationRecord
{
	/// <summary>规律 ID。</summary>
	public string PatternId { get; }
	/// <summary>添加的事件 ID。</summary>
	public string EventId { get; }
	/// <summary>事件类型字符串（如 "log_fragment"）。</summary>
	public string EventType { get; }
	/// <summary>本次添加的分数。</summary>
	public int AddedScore { get; }
	/// <summary>添加后的观测总分。</summary>
	public int NewObservationScore { get; }
	/// <summary>添加前的规律状态。</summary>
	public PatternState PreviousPatternState { get; }
	/// <summary>添加后的规律状态。</summary>
	public PatternState NewPatternState { get; }

	/// <param name="patternId">规律 ID。</param>
	/// <param name="eventId">事件 ID。</param>
	/// <param name="eventType">事件类型。</param>
	/// <param name="addedScore">添加分数。</param>
	/// <param name="newObservationScore">新总分。</param>
	/// <param name="previousPatternState">旧状态。</param>
	/// <param name="newPatternState">新状态。</param>
	public PatternObservationRecord(string patternId, string eventId, string eventType,
		int addedScore, int newObservationScore,
		PatternState previousPatternState, PatternState newPatternState)
	{
		PatternId = patternId;
		EventId = eventId;
		EventType = eventType;
		AddedScore = addedScore;
		NewObservationScore = newObservationScore;
		PreviousPatternState = previousPatternState;
		NewPatternState = newPatternState;
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

	// ── Story 003 — Ability Unlock ─────────────────────────────────
	// 能力状态: abilityId → AbilityState
	private readonly Dictionary<string, AbilityState> _abilityStates =
		new(StringComparer.Ordinal);

	// 解锁路径配置: abilityId → AbilityPathConfig
	private readonly Dictionary<string, AbilityPathConfig> _abilityPaths =
		new(StringComparer.Ordinal);

	// 外部状态快照（由上游系统注入，用于条件求值）
	private readonly HashSet<string> _activeCrewPartners = new(StringComparer.Ordinal);
	private readonly HashSet<string> _completedRepairs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _locationTagIndex =
		new(StringComparer.Ordinal); // tag → set of locationId
	private int _fogTraversalCount;

	// 条件求值器注册表: conditionType → evaluator
	private readonly Dictionary<string, Func<AbilityCondition, bool>> _conditionEvaluators =
		new(StringComparer.Ordinal);

	// 上次触发解锁的路径 ID（供 IntelConsumeResult 填充）
	private string _lastUnlockPathUsed = "";

	// ── Story 004 — IntelConsumeResult ────────────────────────────
	// 已消费情报 ID 集合
	private readonly HashSet<string> _consumedIntelIds = new(StringComparer.Ordinal);

	// intel 定义缓存: intelId → IntelDefinition
	private readonly Dictionary<string, IntelDefinition> _intelDefCache =
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

	// ── 事件（Story 003 — Ability Unlock）─────────────────────────
	/// <summary>
	/// 能力解锁时发出，参数为 (abilityId, unlockPathId)。仅首次解锁发出。
	/// </summary>
	public event Action<string, string>? AbilityUnlocked;

	// ── 事件（Story 004 — IntelConsumeResult）─────────────────────
	/// <summary>
	/// 情报成功消费后发出，参数为 intelId。
	/// </summary>
	public event Action<string>? IntelConsumed;

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
	/// 注册能力解锁路径配置（数据驱动，应在 Initialize() 前调用）。
	/// </summary>
	/// <param name="config">能力解锁路径配置。</param>
	public void RegisterAbilityPathConfig(AbilityPathConfig config)
	{
		_abilityPaths[config.AbilityId] = config;
	}

	/// <summary>
	/// 注册 intel 定义（由 Registry 提供，应在 Initialize() 前调用）。
	/// </summary>
	/// <param name="def">intel 静态定义。</param>
	public void RegisterIntelDefinition(IntelDefinition def)
	{
		_intelDefCache[def.IntelId] = def;
	}

	/// <summary>
	/// 向地点标签索引注册地点（供 location_visit_count 条件求值使用）。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	/// <param name="tags">该地点的标签集合。</param>
	public void RegisterLocationTags(string locationId, IEnumerable<string> tags)
	{
		foreach (var tag in tags)
		{
			if (!_locationTagIndex.TryGetValue(tag, out var set))
			{
				set = new HashSet<string>(StringComparer.Ordinal);
				_locationTagIndex[tag] = set;
			}
			set.Add(locationId);
		}
	}

	/// <summary>
	/// 通知伙伴加入船员（由伙伴系统调用）。
	/// </summary>
	/// <param name="partnerId">伙伴 ID。</param>
	public void OnPartnerJoined(string partnerId)
	{
		_activeCrewPartners.Add(partnerId);
		ReevaluateAbilityUnlocks();
	}

	/// <summary>
	/// 通知伙伴离队船员（由伙伴系统调用）。能力解锁状态不受影响。
	/// </summary>
	/// <param name="partnerId">伙伴 ID。</param>
	public void OnPartnerLeft(string partnerId)
	{
		_activeCrewPartners.Remove(partnerId);
	}

	/// <summary>
	/// 通知世界修复节点完成（由修复系统调用）。
	/// </summary>
	/// <param name="repairNodeId">修复节点 ID。</param>
	public void OnRepairCompleted(string repairNodeId)
	{
		_completedRepairs.Add(repairNodeId);
		ReevaluateAbilityUnlocks();
	}

	/// <summary>
	/// 通知穿越雾区一次（由航行系统调用）。
	/// </summary>
	public void OnFogTraversalCompleted()
	{
		_fogTraversalCount++;
		ReevaluateAbilityUnlocks();
	}

	/// <summary>
	/// 完成初始化，验证阈值配置并注册条件求值器。
	/// </summary>
	public void Initialize()
	{
		ValidateThresholds();
		RegisterConditionEvaluators();
		ValidateAbilityPaths();
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
	/// 因消费情报推进单个地点知识（Unknown/Rumored → Identified）。
	/// Identified 和 Verified 状态不受影响。由 <see cref="ConsumeIntel"/> Rule 2 调用。
	/// </summary>
	/// <param name="locationId">地点 ID。</param>
	/// <returns>推进前后的状态变化记录，未推进时返回 null。</returns>
	public (LocationKnowledgeState OldState, LocationKnowledgeState NewState)?
		AdvanceLocationFromIntel(string locationId)
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

	// ── Story 003 — Ability Multi-Path Unlock ─────────────────────

	/// <summary>
	/// 检查指定能力的解锁条件。OR 跨路径、AND 跨条件。
	/// 已解锁能力直接返回 true（首行短路，不重复发出信号）。
	/// </summary>
	/// <param name="abilityId">能力 ID。</param>
	/// <returns>能力是否已解锁（本次或之前）。</returns>
	public bool CheckUnlockConditions(string abilityId)
	{
		if (_abilityStates.TryGetValue(abilityId, out var current)
			&& current == AbilityState.Unlocked)
			return true;

		if (!_abilityPaths.TryGetValue(abilityId, out var config))
			return false;

		foreach (var path in config.Paths)
		{
			if (IsPathSatisfied(path))
			{
				_abilityStates[abilityId] = AbilityState.Unlocked;
				_lastUnlockPathUsed = path.PathId;
				AbilityUnlocked?.Invoke(abilityId, path.PathId);
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 查询能力当前状态。未注册能力返回 Locked。
	/// </summary>
	/// <param name="abilityId">能力 ID。</param>
	/// <returns>能力状态。</returns>
	public AbilityState QueryAbilityState(string abilityId) =>
		_abilityStates.TryGetValue(abilityId, out var s) ? s : AbilityState.Locked;

	// ── Story 004 — IntelConsumeResult Algorithm ───────────────────

	/// <summary>
	/// 消费一份情报，按 5 条规则顺序执行：
	/// ① 已消耗检查 → ② 推进地点知识 → ③ 添加规律观测 → ④ 检查能力解锁 → ⑤ 标记已消耗。
	/// 返回 <see cref="IntelConsumeResult"/>，三重效果（地点+规律+能力）为预期行为。
	/// </summary>
	/// <param name="intelId">情报 ID。</param>
	/// <returns>消费结果，含地点推进、能力解锁、规律观测三组记录。</returns>
	public IntelConsumeResult ConsumeIntel(string intelId)
	{
		// Rule 1: 已消耗检查
		if (_consumedIntelIds.Contains(intelId))
			return IntelConsumeResult.Fail(intelId, "ERR_INTEL_ALREADY_CONSUMED");

		// 验证 intel 存在
		if (!_intelDefCache.TryGetValue(intelId, out var def))
			return IntelConsumeResult.Fail(intelId, "ERR_INTEL_NOT_FOUND");

		var locationAdvancements = new List<LocationAdvancement>();
		var patternObservations = new List<PatternObservationRecord>();
		var abilityUnlocks = new List<AbilityUnlockRecord>();

		// Rule 2: 推进关联地点知识
		foreach (var locationId in def.LinkedContentIds)
		{
			var adv = AdvanceLocationFromIntel(locationId);
			if (adv.HasValue)
				locationAdvancements.Add(new LocationAdvancement(
					locationId, adv.Value.OldState, adv.Value.NewState));
		}

		// Rule 3: 添加规律 log_fragment 观测事件
		if (!string.IsNullOrEmpty(def.PatternEventId))
		{
			foreach (var patternId in def.LinkedPatterns)
			{
				var rec = AddPatternLogFragment(patternId, def.PatternEventId);
				if (rec != null)
					patternObservations.Add(rec);
			}
		}

		// Rule 4+5: 先标记已消耗（Rule 5 提前），再检查能力解锁（Rule 4）
		// 原因：intel_consumed 条件需要查询本 intel 是否已消耗，
		//       若 Rule 5 在 Rule 4 之后执行，当前消费的 intel 无法触发自身的 Path B 条件。
		_consumedIntelIds.Add(intelId);

		foreach (var abilityId in def.UnlockConditionForAbilities)
		{
			if (QueryAbilityState(abilityId) == AbilityState.Locked
				&& CheckUnlockConditions(abilityId))
			{
				abilityUnlocks.Add(new AbilityUnlockRecord(
					abilityId, "", _lastUnlockPathUsed));
			}
		}
		IntelConsumed?.Invoke(intelId);

		return new IntelConsumeResult(true, "", intelId, def.DisplayName,
			locationAdvancements, abilityUnlocks, patternObservations);
	}

	/// <summary>
	/// 查询情报是否已消耗。
	/// </summary>
	/// <param name="intelId">情报 ID。</param>
	/// <returns>是否已消耗。</returns>
	public bool IsIntelConsumed(string intelId) =>
		_consumedIntelIds.Contains(intelId);

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

	private bool IsPathSatisfied(AbilityUnlockPath path)
	{
		foreach (var cond in path.Conditions)
		{
			if (!EvaluateCondition(cond))
				return false;
		}
		return true;
	}

	private bool EvaluateCondition(AbilityCondition cond)
	{
		if (!_conditionEvaluators.TryGetValue(cond.Type, out var evaluator))
		{
			// 启动时 ValidateAbilityPaths 已记录 error；运行时回退 false
			return false;
		}
		return evaluator(cond);
	}

	private void ReevaluateAbilityUnlocks()
	{
		foreach (var abilityId in _abilityPaths.Keys)
		{
			if (QueryAbilityState(abilityId) == AbilityState.Locked)
				CheckUnlockConditions(abilityId);
		}
	}

	private void RegisterConditionEvaluators()
	{
		_conditionEvaluators["pattern_state"] = cond =>
		{
			var patternId = (string)cond.Params["pattern_id"];
			var required = (PatternState)cond.Params["required_state"];
			var entry = _patterns.TryGetValue(patternId, out var e) ? e : null;
			return ComputePatternState(entry?.ObservationScore ?? 0, patternId) >= required;
		};

		_conditionEvaluators["intel_consumed"] = cond =>
			_consumedIntelIds.Contains((string)cond.Params["intel_id"]);

		_conditionEvaluators["observation_event_count"] = cond =>
		{
			var patternId = (string)cond.Params["pattern_id"];
			var minCount = Convert.ToInt32(cond.Params["min_count"]);
			var entry = _patterns.TryGetValue(patternId, out var e) ? e : null;
			return (entry?.TriggeredEvents.Count ?? 0) >= minCount;
		};

		_conditionEvaluators["observation_event_type_count"] = cond =>
		{
			var patternId = (string)cond.Params["pattern_id"];
			var eventType = (string)cond.Params["event_type"];
			var minCount = Convert.ToInt32(cond.Params["min_count"]);
			var entry = _patterns.TryGetValue(patternId, out var e) ? e : null;
			if (entry == null) return false;
			int count = entry.TriggeredEvents.Count(eid => GetEventType(patternId, eid) == eventType);
			return count >= minCount;
		};

		_conditionEvaluators["partner_in_crew"] = cond =>
			_activeCrewPartners.Contains((string)cond.Params["partner_id"]);

		_conditionEvaluators["repair_completed"] = cond =>
			_completedRepairs.Contains((string)cond.Params["repair_node_id"]);

		_conditionEvaluators["location_visit_count"] = cond =>
		{
			var tag = (string)cond.Params["location_tag"];
			var minCount = Convert.ToInt32(cond.Params["min_count"]);
			var requiredState = cond.Params.TryGetValue("required_state", out var rs)
				? (LocationKnowledgeState)rs
				: LocationKnowledgeState.Verified;
			if (!_locationTagIndex.TryGetValue(tag, out var locIds))
				return false;
			int count = locIds.Count(lid =>
				QueryKnowledgeState(lid) >= requiredState);
			return count >= minCount;
		};

		_conditionEvaluators["fog_traversal_count"] = cond =>
			_fogTraversalCount >= Convert.ToInt32(cond.Params["min_count"]);
	}

	// 从事件权重表推断事件类型（按 ADR-0007 权重映射）
	private string GetEventType(string patternId, string eventId)
	{
		int weight = GetEventWeight(patternId, eventId);
		return weight switch
		{
			1 => "narrative_hint",
			2 => "log_fragment",
			3 => "partner_comment",
			4 => "passive_observation",
			7 => "active_investigation",
			_ => "unknown",
		};
	}

	private PatternObservationRecord? AddPatternLogFragment(string patternId, string eventId)
	{
		var entry = GetOrInitPattern(patternId);
		if (entry.TriggeredEvents.Contains(eventId))
			return null; // 去重

		var oldScore = entry.ObservationScore;
		var oldState = ComputePatternState(oldScore, patternId);

		entry.TriggeredEvents.Add(eventId);
		entry.ObservationScore += WeightLogFragment;

		var newScore = entry.ObservationScore;
		var newState = ComputePatternState(newScore, patternId);

		PatternObserved?.Invoke(patternId, eventId, newScore);
		if (newState != oldState)
		{
			PatternStateChanged?.Invoke(patternId, oldState, newState);
			if (newState == PatternState.Confirmed && entry.PatternUsageSuccess)
				PatternUsageConfirmed?.Invoke(patternId);
		}

		return new PatternObservationRecord(patternId, eventId, "log_fragment",
			WeightLogFragment, newScore, oldState, newState);
	}

	private void ValidateAbilityPaths()
	{
		foreach (var (abilityId, config) in _abilityPaths)
		{
			foreach (var path in config.Paths)
			{
				foreach (var cond in path.Conditions)
				{
					if (!_conditionEvaluators.ContainsKey(cond.Type))
					{
						throw new InvalidOperationException(
							$"missing condition evaluator for type '{cond.Type}' in ability '{abilityId}' path '{path.PathId}'");
					}
				}
			}
		}
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
