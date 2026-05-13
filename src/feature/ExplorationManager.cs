using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

// ── 枚举 ────────────────────────────────────────────────────────────

/// <summary>探索会话阶段枚举（ADR-0013 四阶段状态机）。</summary>
public enum ExplorationPhase
{
	/// <summary>待机，等待 EncounterContext 到来。</summary>
	Idle = 0,
	/// <summary>抵达过渡阶段。</summary>
	Arriving = 1,
	/// <summary>主探索阶段。</summary>
	Exploring = 2,
	/// <summary>撤离读条阶段。</summary>
	Extracting = 3,
	/// <summary>终态：会话结束，结算执行后回到 Idle。</summary>
	Departed = 4,
}

/// <summary>EXPLORING 内的子状态。</summary>
public enum ExplorationSubstate
{
	Idle = 0,
	Moving = 1,
	Searching = 2,
	Threatened = 3,
	ExtractingSub = 4,
}

/// <summary>搜索点状态变体。</summary>
public enum SearchPointState
{
	/// <summary>未搜索。</summary>
	Unlooted = 0,
	/// <summary>已搜索。</summary>
	Looted = 1,
	/// <summary>威胁状态变体（空率↑，Uncommon↓）。</summary>
	DangerChanged = 2,
}

/// <summary>搜索产出结果。</summary>
public sealed class SearchYieldResult
{
	/// <summary>产出物品列表（resource_id → quantity）。</summary>
	public IReadOnlyList<(string ResourceId, int Quantity)> Items { get; }
	/// <summary>是否为空结果。</summary>
	public bool IsEmpty { get; }
	/// <summary>是否消耗了搜索次数（空结果=false，非空=true）。</summary>
	public bool SearchConsumed { get; }
	/// <summary>给玩家的消息文字（可为空）。</summary>
	public string Message { get; }

	/// <param name="items">产出物品。</param>
	/// <param name="isEmpty">是否为空。</param>
	/// <param name="searchConsumed">是否消耗搜索。</param>
	/// <param name="message">消息文字。</param>
	public SearchYieldResult(IReadOnlyList<(string, int)> items, bool isEmpty,
		bool searchConsumed, string message = "")
	{
		Items = items;
		IsEmpty = isEmpty;
		SearchConsumed = searchConsumed;
		Message = message;
	}

	/// <summary>构建空结果的工厂方法。</summary>
	public static SearchYieldResult Empty(string message = "") =>
		new(Array.Empty<(string, int)>(), true, false, message);
}

/// <summary>情报点交互结果。</summary>
public sealed class IntelInteractionResult
{
	/// <summary>产出的情报 ID（空字符串表示无产出）。</summary>
	public string IntelId { get; }
	/// <summary>是否为空结果。</summary>
	public bool IsEmpty { get; }
	/// <summary>是否因容量满而阻塞。</summary>
	public bool CapacityBlocked { get; }
	/// <summary>消息文字。</summary>
	public string Message { get; }

	/// <param name="intelId">情报 ID。</param>
	/// <param name="isEmpty">是否为空。</param>
	/// <param name="capacityBlocked">是否被容量阻塞。</param>
	/// <param name="message">消息文字。</param>
	public IntelInteractionResult(string intelId, bool isEmpty,
		bool capacityBlocked = false, string message = "")
	{
		IntelId = intelId;
		IsEmpty = isEmpty;
		CapacityBlocked = capacityBlocked;
		Message = message;
	}
}

/// <summary>
/// Exploration / Scavenge Scenario Autoload #11（ADR-0013）。
/// 管理 4 阶段探索会话状态机，执行 6 个核心公式，逻辑/场景分离。
/// </summary>
public sealed class ExplorationManager
{
	// ── 常量 ─────────────────────────────────────────────────────────
	/// <summary>撤离读条时长（秒）。</summary>
	public const double ExtractionDuration = 2.5;

	private static readonly IReadOnlyDictionary<string, double> EmptyChanceUnlooted =
		new Dictionary<string, double>(StringComparer.Ordinal)
		{
			["A_core"] = 0.00,
			["B_inner"] = 0.05,
			["C_mid"] = 0.20,
			["D_outer"] = 0.35,
		};

	private static readonly IReadOnlyDictionary<string, double> EmptyChanceDangerChanged =
		new Dictionary<string, double>(StringComparer.Ordinal)
		{
			["A_core"] = 0.15,
			["B_inner"] = 0.20,
			["C_mid"] = 0.35,
			["D_outer"] = 0.50,
		};

	private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>
		QualityWeightsUnlooted = new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal)
		{
			["A_core"] = new Dictionary<string, double>(StringComparer.Ordinal)
				{ ["poor"] = 0.20, ["common"] = 0.45, ["uncommon"] = 0.35 },
			["B_inner"] = new Dictionary<string, double>(StringComparer.Ordinal)
				{ ["poor"] = 0.30, ["common"] = 0.45, ["uncommon"] = 0.25 },
			["C_mid"] = new Dictionary<string, double>(StringComparer.Ordinal)
				{ ["poor"] = 0.35, ["common"] = 0.40, ["uncommon"] = 0.25 },
			["D_outer"] = new Dictionary<string, double>(StringComparer.Ordinal)
				{ ["poor"] = 0.45, ["common"] = 0.40, ["uncommon"] = 0.15 },
		};

	// ── 内部状态 ─────────────────────────────────────────────────────
	private ExplorationPhase _phase = ExplorationPhase.Idle;
	private ExplorationSubstate _substate = ExplorationSubstate.Idle;
	private string _currentPointId = "";
	private double _extractionElapsed;
	private bool _extractionActive;

	// 搜索点状态：pointId → SearchPointState
	private readonly Dictionary<string, SearchPointState> _searchPointStates =
		new(StringComparer.Ordinal);

	// 已搜索的搜索点（本次会话）
	private readonly HashSet<string> _searchedPoints = new(StringComparer.Ordinal);

	// 已交互的情报点（本次会话）
	private readonly HashSet<string> _interactedIntelPoints = new(StringComparer.Ordinal);

	// 依赖注入委托
	private Func<string, int, bool>? _canAddToPoolFn;    // Pool 5 容量检查
	private Action<string, int>? _addLootFn;              // Pool 5 添加物品
	private Func<double>? _randomFn;                      // 随机数 [0,1)
	private Func<int, int, int>? _randomRangeFn;          // 随机整数 [min, max]
	private Func<string, bool>? _hasRelevantIntelFn;      // #6 intel 门控
	private Func<string, string>? _getIntelIdForPointFn;  // 情报点→情报 ID

	// loot_pool 注入：searchPointId → tier → [(resourceId, quantity_min, quantity_max)]
	private Dictionary<string, Dictionary<string, List<(string ResourceId, int QMin, int QMax)>>>
		_lootPools = new(StringComparer.Ordinal);

	// ── 事件 ─────────────────────────────────────────────────────────
	/// <summary>阶段转换信号：(旧阶段, 新阶段, 探索点 ID)。</summary>
	public event Action<ExplorationPhase, ExplorationPhase, string>? ExplorationPhaseChanged;

	/// <summary>子状态转换信号：(旧子状态, 新子状态)。</summary>
	public event Action<ExplorationSubstate, ExplorationSubstate>? SubstateChanged;

	/// <summary>撤离读条开始，参数为触发原因。</summary>
	public event Action<string>? ExtractionStarted;

	/// <summary>撤离读条进度更新 [0,1]。</summary>
	public event Action<double>? ExtractionProgressChanged;

	/// <summary>撤离读条被中断，参数为原因。</summary>
	public event Action<string>? ExtractionInterrupted;

	/// <summary>搜索完成，参数为 (spId, result)。</summary>
	public event Action<string, SearchYieldResult>? SearchPerformed;

	/// <summary>物品捡起，参数为 (resourceId, quantity)。</summary>
	public event Action<string, int>? ItemPickedUp;

	/// <summary>情报发现，参数为 intelId。</summary>
	public event Action<string>? IntelDiscovered;

	/// <summary>容量警告，参数为 (poolId, usedSlots)。</summary>
	public event Action<int, int>? CapacityWarning;

	// ── 属性 ─────────────────────────────────────────────────────────
	/// <summary>当前会话阶段。</summary>
	public ExplorationPhase CurrentPhase => _phase;

	/// <summary>当前 EXPLORING 子状态。</summary>
	public ExplorationSubstate CurrentSubstate => _substate;

	/// <summary>当前探索点 ID。</summary>
	public string CurrentPointId => _currentPointId;

	/// <summary>撤离读条已流逝时间（秒）。</summary>
	public double ExtractionElapsed => _extractionElapsed;

	// ── 依赖注入 ─────────────────────────────────────────────────────

	/// <summary>注入 Pool 5 容量检查委托（#5 ResourcesManager）。</summary>
	public void SetCanAddToPoolDelegate(Func<string, int, bool> fn) => _canAddToPoolFn = fn;

	/// <summary>注入 Pool 5 添加物品委托（#5 ResourcesManager）。</summary>
	public void SetAddLootDelegate(Action<string, int> fn) => _addLootFn = fn;

	/// <summary>注入随机数委托（返回 [0,1)，可注入固定值确保测试确定性）。</summary>
	public void SetRandomDelegate(Func<double> fn) => _randomFn = fn;

	/// <summary>注入随机整数委托（含两端）。</summary>
	public void SetRandomRangeDelegate(Func<int, int, int> fn) => _randomRangeFn = fn;

	/// <summary>注入 #6 情报门控委托（IntelManager.has_relevant_intel）。</summary>
	public void SetHasRelevantIntelDelegate(Func<string, bool> fn) => _hasRelevantIntelFn = fn;

	/// <summary>注入情报点→情报 ID 映射委托。</summary>
	public void SetGetIntelIdForPointDelegate(Func<string, string> fn) => _getIntelIdForPointFn = fn;

	/// <summary>注入搜索点战利品池（测试用，替代 Registry 查询）。</summary>
	public void SetLootPools(Dictionary<string, Dictionary<string, List<(string, int, int)>>> pools) =>
		_lootPools = pools;

	// ── Story 001 — 状态机 ────────────────────────────────────────────

	/// <summary>
	/// 收到 EncounterContext 后进入探索会话（IDLE → ARRIVING）。
	/// </summary>
	/// <param name="destinationId">目的地探索点 ID（来自 EncounterContext）。</param>
	/// <returns>是否成功进入。</returns>
	public bool EnterExploration(string destinationId)
	{
		if (_phase != ExplorationPhase.Idle) return false;
		_currentPointId = destinationId; // 先设置 pointId，信号发射时已可读
		return TransitionPhase(ExplorationPhase.Arriving);
	}

	/// <summary>
	/// 跳过抵达过渡阶段（ARRIVING → EXPLORING）。
	/// </summary>
	/// <returns>是否成功。</returns>
	public bool SkipArriving()
	{
		if (_phase != ExplorationPhase.Arriving) return false;
		return TransitionPhase(ExplorationPhase.Exploring);
	}

	/// <summary>
	/// 玩家主动触发撤离（EXPLORING → EXTRACTING）。
	/// </summary>
	/// <returns>是否成功。</returns>
	public bool TriggerExtraction()
	{
		if (_phase != ExplorationPhase.Exploring) return false;
		if (!TransitionPhase(ExplorationPhase.Extracting)) return false;
		_extractionElapsed = 0;
		_extractionActive = true;
		ExtractionStarted?.Invoke("player_initiated");
		return true;
	}

	/// <summary>
	/// 强制触发撤离（Pool 5 耗尽等条件）。
	/// </summary>
	/// <param name="reason">强制原因。</param>
	/// <returns>是否成功。</returns>
	public bool ForceExtraction(string reason)
	{
		if (_phase != ExplorationPhase.Exploring) return false;
		if (!TransitionPhase(ExplorationPhase.Extracting)) return false;
		_extractionElapsed = 0;
		_extractionActive = true;
		ExtractionStarted?.Invoke(reason);
		return true;
	}

	/// <summary>
	/// 推进撤离读条（应在每帧调用）。
	/// </summary>
	/// <param name="delta">帧时间（秒）。</param>
	public void ExtractionTick(double delta)
	{
		if (!_extractionActive || _phase != ExplorationPhase.Extracting) return;
		_extractionElapsed += delta;
		double progress = Math.Clamp(_extractionElapsed / ExtractionDuration, 0.0, 1.0);
		ExtractionProgressChanged?.Invoke(progress);

		if (_extractionElapsed >= ExtractionDuration)
		{
			_extractionActive = false;
			FinalizeExtraction();
		}
	}

	/// <summary>
	/// 中断撤离读条（威胁打断时调用），回到 EXPLORING。
	/// </summary>
	/// <param name="reason">中断原因。</param>
	public void InterruptExtraction(string reason)
	{
		if (!_extractionActive) return;
		_extractionActive = false;
		_extractionElapsed = 0;
		ExtractionInterrupted?.Invoke(reason);
		TransitionPhase(ExplorationPhase.Exploring);
	}

	/// <summary>
	/// 结算完成后回到 IDLE（DEPARTED → IDLE）。
	/// </summary>
	/// <returns>是否成功。</returns>
	public bool ReturnToIdle() => TransitionPhase(ExplorationPhase.Idle);

	/// <summary>
	/// 更新 EXPLORING 子状态。
	/// </summary>
	/// <param name="newSubstate">目标子状态。</param>
	public void SetSubstate(ExplorationSubstate newSubstate)
	{
		if (_phase != ExplorationPhase.Exploring) return;
		var old = _substate;
		_substate = newSubstate;
		if (old != newSubstate)
			SubstateChanged?.Invoke(old, newSubstate);
	}

	// ── Story 002 — Search, Scavenge & Intel Formulas ─────────────────

	/// <summary>
	/// F-11-01 搜索产出投骰（含自由搜索保证）。
	/// 空结果 search_consumed=false，玩家可继续搜索其他点。
	/// </summary>
	/// <param name="spId">搜索点 ID。</param>
	/// <param name="state">搜索点当前状态变体。</param>
	/// <param name="zone">区域（A_core/B_inner/C_mid/D_outer）。</param>
	/// <returns>搜索产出结果。</returns>
	public SearchYieldResult SearchYield(string spId, SearchPointState state, string zone)
	{
		// 已搜索过 → 返回已枯竭消息
		if (state == SearchPointState.Looted)
			return SearchYieldResult.Empty("这里已经被搜过了");

		double emptyChance = GetEmptyChance(state, zone);
		double roll = Roll();

		if (roll < emptyChance)
			return SearchYieldResult.Empty();

		// 品质档位抽取
		var weights = GetQualityWeights(state, zone);
		string tier = WeightedRandomTier(weights);

		// 从 loot pool 抽取
		if (!_lootPools.TryGetValue(spId, out var poolByTier)
			|| !poolByTier.TryGetValue(tier, out var pool)
			|| pool.Count == 0)
		{
			return SearchYieldResult.Empty("这里似乎还能找到些什么，但已经什么都没有了");
		}

		var (drawMin, drawMax) = GetDrawCount(tier);
		int drawCount = RollRange(drawMin, Math.Min(drawMax, pool.Count));
		var selected = SampleWithoutReplacement(pool, drawCount);

		var items = selected
			.Select(e => (e.ResourceId, RollRange(e.QMin, e.QMax)))
			.ToList();

		return new SearchYieldResult(items, false, true);
	}

	/// <summary>
	/// 执行搜索点搜索：调用 SearchYield，处理容量检查，发射信号。
	/// </summary>
	/// <param name="spId">搜索点 ID。</param>
	/// <param name="state">当前状态变体。</param>
	/// <param name="zone">区域。</param>
	/// <returns>搜索产出结果。</returns>
	public SearchYieldResult PerformSearch(string spId, SearchPointState state, string zone)
	{
		if (_phase != ExplorationPhase.Exploring)
			return SearchYieldResult.Empty("当前阶段不允许搜索");

		var result = SearchYield(spId, state, zone);

		if (result.IsEmpty)
		{
			SearchPerformed?.Invoke(spId, result);
			return result;
		}

		// 容量检查
		foreach (var (resourceId, qty) in result.Items)
		{
			if (_canAddToPoolFn != null && !_canAddToPoolFn(resourceId, qty))
			{
				CapacityWarning?.Invoke(5, 5);
				// 容量满时 search_consumed 保持 false
				var blocked = new SearchYieldResult(result.Items, false, false, "背包已满");
				SearchPerformed?.Invoke(spId, blocked);
				return blocked;
			}
		}

		// 加入背包
		foreach (var (resourceId, qty) in result.Items)
		{
			_addLootFn?.Invoke(resourceId, qty);
			ItemPickedUp?.Invoke(resourceId, qty);
		}

		_searchedPoints.Add(spId);
		SearchPerformed?.Invoke(spId, result);
		return result;
	}

	/// <summary>
	/// F-11-06 情报点交互（固定产出 1 个 Unique 情报物品）。
	/// </summary>
	/// <param name="intelPointId">情报点 ID。</param>
	/// <returns>情报交互结果。</returns>
	public IntelInteractionResult PerformIntelInteraction(string intelPointId)
	{
		if (_interactedIntelPoints.Contains(intelPointId))
			return new IntelInteractionResult("", true, message: "此处已调查过");

		string intelId = _getIntelIdForPointFn?.Invoke(intelPointId) ?? "";
		if (string.IsNullOrEmpty(intelId))
			return new IntelInteractionResult("", true, message: "无情报");

		// 容量检查（Unique 物品 max_stack=1）
		if (_canAddToPoolFn != null && !_canAddToPoolFn(intelId, 1))
		{
			CapacityWarning?.Invoke(5, 5);
			return new IntelInteractionResult(intelId, false, capacityBlocked: true);
		}

		_addLootFn?.Invoke(intelId, 1);
		_interactedIntelPoints.Add(intelPointId);
		IntelDiscovered?.Invoke(intelId);
		return new IntelInteractionResult(intelId, false);
	}

	/// <summary>
	/// 获取搜索点描述文字（#6 情报门控）。
	/// </summary>
	/// <param name="spId">搜索点 ID。</param>
	/// <param name="defaultDescription">默认描述。</param>
	/// <param name="enhancedDescription">增强描述（有相关情报时使用）。</param>
	/// <returns>应显示的描述文字。</returns>
	public string GetSearchPointDescription(string spId,
		string defaultDescription, string enhancedDescription)
	{
		bool hasIntel = _hasRelevantIntelFn?.Invoke(spId) ?? false;
		return hasIntel ? enhancedDescription : defaultDescription;
	}

	// ── 私有辅助 ──────────────────────────────────────────────────────

	private bool TransitionPhase(ExplorationPhase target)
	{
		var current = _phase;
		bool allowed = (current, target) switch
		{
			(ExplorationPhase.Idle, ExplorationPhase.Arriving) => true,
			(ExplorationPhase.Arriving, ExplorationPhase.Exploring) => true,
			(ExplorationPhase.Exploring, ExplorationPhase.Extracting) => true,
			(ExplorationPhase.Extracting, ExplorationPhase.Exploring) => true, // 被打断
			(ExplorationPhase.Extracting, ExplorationPhase.Departed) => true,
			(ExplorationPhase.Departed, ExplorationPhase.Idle) => true,
			_ => false,
		};

		if (!allowed) return false;

		_phase = target;

		if (target == ExplorationPhase.Exploring)
			_substate = ExplorationSubstate.Idle;

		if (target == ExplorationPhase.Idle)
			ClearSessionState();

		ExplorationPhaseChanged?.Invoke(current, target, _currentPointId);
		return true;
	}

	private void FinalizeExtraction()
	{
		TransitionPhase(ExplorationPhase.Departed);
	}

	private void ClearSessionState()
	{
		_currentPointId = "";
		_substate = ExplorationSubstate.Idle;
		_extractionElapsed = 0;
		_extractionActive = false;
		_searchedPoints.Clear();
		_interactedIntelPoints.Clear();
	}

	private double Roll() => _randomFn?.Invoke() ?? Random.Shared.NextDouble();

	private int RollRange(int min, int max)
	{
		if (_randomRangeFn != null) return _randomRangeFn(min, max);
		return min == max ? min : Random.Shared.Next(min, max + 1);
	}

	private static double GetEmptyChance(SearchPointState state, string zone)
	{
		var table = state == SearchPointState.DangerChanged
			? EmptyChanceDangerChanged : EmptyChanceUnlooted;
		return table.TryGetValue(zone, out var v) ? v : 0.20;
	}

	private static IReadOnlyDictionary<string, double> GetQualityWeights(
		SearchPointState state, string zone)
	{
		if (state == SearchPointState.DangerChanged)
		{
			// danger-changed: Uncommon ×0.5，差额加给 Poor
			if (!QualityWeightsUnlooted.TryGetValue(zone, out var baseW))
				return new Dictionary<string, double> { ["poor"] = 1.0 };
			double uncommon = (baseW.TryGetValue("uncommon", out var u) ? u : 0) * 0.5;
			double poor = (baseW.TryGetValue("poor", out var p) ? p : 0)
				+ (baseW.TryGetValue("uncommon", out var u2) ? u2 : 0) * 0.5;
			double common = baseW.TryGetValue("common", out var c) ? c : 0;
			return new Dictionary<string, double>(StringComparer.Ordinal)
			{
				["poor"] = poor, ["common"] = common, ["uncommon"] = uncommon,
			};
		}
		return QualityWeightsUnlooted.TryGetValue(zone, out var w)
			? w : new Dictionary<string, double> { ["poor"] = 1.0 };
	}

	private string WeightedRandomTier(IReadOnlyDictionary<string, double> weights)
	{
		double total = weights.Values.Sum();
		if (total <= 0) return "poor";
		double roll = Roll() * total;
		double cumulative = 0;
		foreach (var (tier, weight) in weights)
		{
			cumulative += weight;
			if (roll <= cumulative) return tier;
		}
		return "poor";
	}

	private static (int Min, int Max) GetDrawCount(string tier) => tier switch
	{
		"poor" => (1, 2),
		"common" => (1, 2),
		"uncommon" => (1, 2),
		_ => (1, 1),
	};

	private List<(string ResourceId, int QMin, int QMax)> SampleWithoutReplacement(
		List<(string ResourceId, int QMin, int QMax)> pool, int count)
	{
		var copy = pool.ToList();
		var result = new List<(string, int, int)>();
		for (int i = 0; i < count && copy.Count > 0; i++)
		{
			int idx = (int)Math.Floor(Roll() * copy.Count);
			idx = Math.Clamp(idx, 0, copy.Count - 1);
			result.Add(copy[idx]);
			copy.RemoveAt(idx);
		}
		return result;
	}
}
