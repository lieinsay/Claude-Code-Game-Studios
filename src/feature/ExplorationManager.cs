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

/// <summary>携带物品堆（Pool 5 内容，F-11-04 损耗结算输入）。</summary>
public sealed class CarriedStack
{
	/// <summary>资源 ID。</summary>
	public string ResourceId { get; }
	/// <summary>数量。</summary>
	public int Quantity { get; }
	/// <summary>是否为 Unique 物品（Q=1, MaxStack=1 的特殊物品）。</summary>
	public bool IsUnique { get; }
	/// <summary>最大堆叠数。</summary>
	public int MaxStack { get; }

	/// <param name="resourceId">资源 ID。</param>
	/// <param name="quantity">数量。</param>
	/// <param name="isUnique">是否为 Unique 物品。</param>
	/// <param name="maxStack">最大堆叠数。</param>
	public CarriedStack(string resourceId, int quantity, bool isUnique, int maxStack)
	{
		ResourceId = resourceId;
		Quantity = quantity;
		IsUnique = isUnique;
		MaxStack = maxStack;
	}
}

/// <summary>F-11-04 撤离损耗结算输出。</summary>
public sealed class ExtractionSettlementResult
{
	/// <summary>已转移物品列表：(ResourceId, 保留数量, 损耗数量)。</summary>
	public IReadOnlyList<(string ResourceId, int Quantity, int Lost)> Transferred { get; }
	/// <summary>损耗物品列表：(ResourceId, 损耗数量)。</summary>
	public IReadOnlyList<(string ResourceId, int Quantity)> Lost { get; }
	/// <summary>总损耗数量。</summary>
	public int TotalLostQty { get; }

	/// <param name="transferred">已转移物品列表。</param>
	/// <param name="lost">损耗物品列表。</param>
	/// <param name="totalLostQty">总损耗数量。</param>
	public ExtractionSettlementResult(
		IReadOnlyList<(string, int, int)> transferred,
		IReadOnlyList<(string, int)> lost,
		int totalLostQty)
	{
		Transferred = transferred;
		Lost = lost;
		TotalLostQty = totalLostQty;
	}
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

	/// <summary>正常撤离损耗系数（F-11-04）。</summary>
	public const double LambdaSuccess = 0.08;
	/// <summary>强制撤离损耗系数（F-11-04，retreat_flagged=true 时使用）。</summary>
	public const double LambdaForced = 0.25;

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

	// Story 005 — Extraction & Settlement 委托
	/// <summary>Pool 5 获取已携带物品委托（#5 ResourcesManager）。</summary>
	private Func<IReadOnlyList<CarriedStack>>? _getCarriedStacksFn;
	/// <summary>批量原子转移至飞艇仓库委托（#5 ResourcesManager.extract_carried_to_storage）。</summary>
	private Func<IReadOnlyList<CarriedStack>, bool>? _extractCarriedToStorageFn;
	/// <summary>情报揭示委托（#6 IntelManager.reveal_from_exploration）。</summary>
	private Action<string>? _revealIntelFn;
	/// <summary>结算快照写入委托（#3 Persistence）。</summary>
	private Func<ExtractionSettlementResult, bool>? _triggerSettlementSnapshotFn;
	/// <summary>重试调度委托（秒 + 回调）。</summary>
	private Action<double, Action>? _scheduleRetryCallbackFn;
	/// <summary>显示结算失败 UI 委托。</summary>
	private Action? _emitSettlementFailedUiFn;

	// 环境威胁活跃状态（HandleTriggeredThreat 中 Environmental 分支设置）
	private bool _envThreatActive;

	// MVP：注册的搜索点总数（由场景层/测试注入，默认 6）
	private int _registeredSearchPointCount = 6;

	// EC-11-03 重试相关
	private static readonly double[] RetryDelays = [1.0, 2.0, 4.0, 8.0];
	private ExtractionSettlementResult? _pendingSettlement;

	// Story 006 — 持久化快照与会话恢复
	/// <summary>上次存储配额警告时间戳（秒）。用于 30s 防抖。</summary>
	private double _lastSnapshotWarningTime = double.NegativeInfinity;
	/// <summary>配额警告冷却时长（秒）。</summary>
	private const double QuotaWarningCooldown = 30.0;

	// 页面可见性中断标志（由 Platform #2 通过 OnPageHidden/OnPageVisible 控制）
	private bool _markArrivingInterrupted;
	private bool _markExtractionInterrupted;

	// 依赖注入委托（Story 006）
	/// <summary>当前挂钟时间（秒）。供快照防抖使用；默认 Environment.TickCount64。</summary>
	private Func<double>? _wallClockFn;
	/// <summary>Pool 5 一致性修复委托（EC-11-09）。</summary>
	private Action? _poolReconcileFn;
	/// <summary>触发通用探索快照写入委托（#3 Persistence，搜索成功/撤离触发时调用）。</summary>
	private Func<bool>? _captureSnapshotFn;
	/// <summary>获取 Pool 5 实际已占用槽数委托（#5 ResourcesManager）。</summary>
	private Func<int>? _getPoolActualOccupiedFn;
	/// <summary>获取 Pool 5 追踪已占用槽数委托（#5 ResourcesManager）。</summary>
	private Func<int>? _getPoolTrackedOccupiedFn;

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

	/// <summary>撤离结算完成，参数为 (transferredCount, intelCount)。</summary>
	public event Action<int, int>? ExtractionCompleted;

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

	/// <summary>注入 Pool 5 获取已携带物品委托（#5 ResourcesManager）。</summary>
	public void SetGetCarriedStacksDelegate(Func<IReadOnlyList<CarriedStack>> fn) =>
		_getCarriedStacksFn = fn;

	/// <summary>注入批量原子转移至飞艇仓库委托（#5 ResourcesManager.extract_carried_to_storage）。</summary>
	public void SetExtractCarriedToStorageDelegate(Func<IReadOnlyList<CarriedStack>, bool> fn) =>
		_extractCarriedToStorageFn = fn;

	/// <summary>注入情报揭示委托（#6 IntelManager.reveal_from_exploration）。</summary>
	public void SetRevealIntelDelegate(Action<string> fn) => _revealIntelFn = fn;

	/// <summary>注入结算快照写入委托（#3 Persistence）。</summary>
	public void SetTriggerSettlementSnapshotDelegate(Func<ExtractionSettlementResult, bool> fn) =>
		_triggerSettlementSnapshotFn = fn;

	/// <summary>注入重试调度委托（定时回调：秒 + 回调函数）。</summary>
	public void SetScheduleRetryCallbackDelegate(Action<double, Action> fn) =>
		_scheduleRetryCallbackFn = fn;

	/// <summary>注入显示结算失败 UI 委托。</summary>
	public void SetEmitSettlementFailedUiDelegate(Action fn) =>
		_emitSettlementFailedUiFn = fn;

	/// <summary>注入当前探索点注册的搜索点总数（场景层/测试调用，MVP 默认 6）。</summary>
	public void SetRegisteredSearchPointCount(int count) =>
		_registeredSearchPointCount = count;

	/// <summary>注入挂钟时间委托（返回秒；供快照防抖使用）。</summary>
	public void SetWallClockDelegate(Func<double> fn) => _wallClockFn = fn;

	/// <summary>注入 Pool 5 一致性修复委托（EC-11-09，#5 ResourcesManager）。</summary>
	public void SetPoolReconcileDelegate(Action fn) => _poolReconcileFn = fn;

	/// <summary>注入通用探索快照写入委托（#3 Persistence，搜索成功与撤离触发时调用）。</summary>
	public void SetCaptureSnapshotDelegate(Func<bool> fn) => _captureSnapshotFn = fn;

	/// <summary>注入 Pool 5 实际占用槽数获取委托（#5 ResourcesManager）。</summary>
	public void SetGetPoolActualOccupiedDelegate(Func<int> fn) => _getPoolActualOccupiedFn = fn;

	/// <summary>注入 Pool 5 追踪占用槽数获取委托（#5 ResourcesManager）。</summary>
	public void SetGetPoolTrackedOccupiedDelegate(Func<int> fn) => _getPoolTrackedOccupiedFn = fn;

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
		TriggerSnapshot();
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
		TriggerSnapshot();
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
		TriggerSnapshot();
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

	// ── Story 003 — Threat Triggering & Scout Preview ────────────────

	/// <summary>威胁类别枚举。</summary>
	public enum ThreatCategory { Environmental = 0, Guard = 1 }
	/// <summary>侦察预览等级枚举。</summary>
	public enum ScoutPreviewLevel { None = 0, Presence = 1, Full = 2 }

	/// <summary>威胁点定义（测试注入用）。</summary>
	public sealed class ThreatPoint
	{
		/// <summary>威胁 ID。</summary>
		public string ThreatId { get; }
		/// <summary>威胁类别。</summary>
		public ThreatCategory Category { get; }
		/// <summary>触发半径。</summary>
		public double TriggerRadius { get; }
		/// <summary>位置（简化为一维距离）。</summary>
		public double Position { get; }
		/// <summary>是否活跃（false = 已清除）。</summary>
		public bool IsActive { get; set; }

		/// <param name="threatId">威胁 ID。</param>
		/// <param name="category">威胁类别。</param>
		/// <param name="triggerRadius">触发半径。</param>
		/// <param name="position">位置坐标（简化）。</param>
		public ThreatPoint(string threatId, ThreatCategory category,
			double triggerRadius, double position)
		{
			ThreatId = threatId;
			Category = category;
			TriggerRadius = triggerRadius;
			Position = position;
			IsActive = true;
		}
	}

	/// <summary>威胁触发结果。</summary>
	public sealed class ThreatTriggerResult
	{
		/// <summary>是否触发。</summary>
		public bool Triggered { get; }
		/// <summary>触发的威胁 ID（未触发时为空）。</summary>
		public string ThreatId { get; }
		/// <summary>威胁类别。</summary>
		public ThreatCategory Category { get; }

		/// <param name="triggered">是否触发。</param>
		/// <param name="threatId">威胁 ID。</param>
		/// <param name="category">类别。</param>
		public ThreatTriggerResult(bool triggered, string threatId = "", ThreatCategory category = ThreatCategory.Environmental)
		{
			Triggered = triggered;
			ThreatId = threatId;
			Category = category;
		}
	}

	// 触发概率表
	private const double TriggerProbEnvironmental = 1.0;
	private const double TriggerProbGuard = 0.70;

	// η_scout 快照（ARRIVING 时快照，探索中不变）
	private double _etaScoutSnapshot;

	// 委托：#8 ModulesManager 写入伤害
	private Action<int>? _applyExplorationHullDamageFn;
	// 委托：#12 CombatManager 可用性检查
	private Func<bool>? _isCombatManagerAvailableFn;
	// 委托：#12 CombatManager 发起战斗
	private Action<string, ThreatCategory>? _initiateThreatFn;
	// 委托：η_scout 快照获取
	private Func<double>? _getScoutEfficiencyFn;

	/// <summary>已触发的威胁列表（本次会话）。</summary>
	private readonly List<ThreatPoint> _sessionThreatPoints = new();

	// 战斗回调结果
	private bool _retreatFlagged;

	// 事件
	/// <summary>威胁触发时发出，参数为 (threatId, category)。</summary>
	public event Action<string, ThreatCategory>? ThreatTriggered;
	/// <summary>威胁清除时发出，参数为 threatId。</summary>
	public event Action<string>? ThreatCleared;

	// Story 006 事件
	/// <summary>存储配额不足时发出（30s 防抖）。场景层订阅并显示非阻塞 HUD 警告。</summary>
	public event Action? QuotaWarningEmitted;
	/// <summary>会话从快照恢复至 EXPLORING 时发出。场景层订阅并显示"你在探索中中断了"提示。</summary>
	public event Action? SessionRestoredNotice;
	/// <summary>会话从 EXTRACTING 快照恢复至 EXPLORING 时发出。场景层将玩家放置在撤离锚点旁。</summary>
	public event Action? ExtractionInterruptedOnRestore;

	/// <summary>注入 #8 船体伤害写入委托（探索中环境威胁造成伤害）。</summary>
	public void SetApplyExplorationHullDamageDelegate(Action<int> fn) =>
		_applyExplorationHullDamageFn = fn;

	/// <summary>注入 #12 CombatManager 可用性检查委托。</summary>
	public void SetIsCombatManagerAvailableDelegate(Func<bool> fn) =>
		_isCombatManagerAvailableFn = fn;

	/// <summary>注入 #12 CombatManager 发起战斗委托。</summary>
	public void SetInitiateThreatDelegate(Action<string, ThreatCategory> fn) =>
		_initiateThreatFn = fn;

	/// <summary>注入 η_scout 获取委托（ARRIVING 时快照）。</summary>
	public void SetGetScoutEfficiencyDelegate(Func<double> fn) =>
		_getScoutEfficiencyFn = fn;

	/// <summary>注册威胁点（测试/场景层调用）。</summary>
	/// <param name="tp">威胁点定义。</param>
	public void RegisterThreatPoint(ThreatPoint tp) => _sessionThreatPoints.Add(tp);

	/// <summary>
	/// F-11-02 威胁触发判定。
	/// 环境威胁靠近必触发（P=1.0）；守卫威胁靠近 P=0.70，交互 P=1.0；已清除威胁直接跳过。
	/// </summary>
	/// <param name="playerPosition">玩家位置（简化为一维距离）。</param>
	/// <param name="triggerType">"proximity" 或 "interaction"。</param>
	/// <returns>按优先级排序的触发结果列表。</returns>
	public IReadOnlyList<ThreatTriggerResult> CheckThreatTrigger(double playerPosition, string triggerType)
	{
		var triggered = new List<(ThreatTriggerResult Result, ThreatPoint Point)>();

		foreach (var tp in _sessionThreatPoints)
		{
			if (!tp.IsActive) continue;

			var result = CheckSingleThreatTrigger(tp, triggerType, playerPosition);
			if (result.Triggered)
				triggered.Add((result, tp));
		}

		// 按优先级排序：环境 > 守卫，距离近 > 远，同距离按 ThreatId 字典序
		triggered.Sort((a, b) =>
		{
			if (a.Point.Category != b.Point.Category)
				return a.Point.Category == ThreatCategory.Environmental ? -1 : 1;
			double distA = Math.Abs(playerPosition - a.Point.Position);
			double distB = Math.Abs(playerPosition - b.Point.Position);
			if (Math.Abs(distA - distB) > 0.01)
				return distA.CompareTo(distB);
			return string.Compare(a.Point.ThreatId, b.Point.ThreatId, StringComparison.Ordinal);
		});

		// 处理每个触发的威胁
		foreach (var (result, tp) in triggered)
		{
			HandleTriggeredThreat(tp);
			// 如果撤离被打断，停止处理后续威胁
			if (_phase == ExplorationPhase.Extracting)
			{
				InterruptExtraction("threat");
				break;
			}
		}

		return triggered.Select(t => t.Result).ToList();
	}

	/// <summary>单个威胁触发判定。</summary>
	public ThreatTriggerResult CheckSingleThreatTrigger(ThreatPoint tp,
		string triggerType, double playerPosition)
	{
		if (!tp.IsActive)
			return new ThreatTriggerResult(false);

		if (triggerType == "interaction")
			return new ThreatTriggerResult(true, tp.ThreatId, tp.Category);

		if (triggerType == "proximity")
		{
			double dist = Math.Abs(playerPosition - tp.Position);
			if (dist > tp.TriggerRadius)
				return new ThreatTriggerResult(false);
			double prob = tp.Category == ThreatCategory.Environmental
				? TriggerProbEnvironmental : TriggerProbGuard;
			if (Roll() < prob)
				return new ThreatTriggerResult(true, tp.ThreatId, tp.Category);
		}

		return new ThreatTriggerResult(false);
	}

	/// <summary>处理已触发威胁（内部）。</summary>
	private void HandleTriggeredThreat(ThreatPoint tp)
	{
		ThreatTriggered?.Invoke(tp.ThreatId, tp.Category);
		SetSubstate(ExplorationSubstate.Threatened);

		if (tp.Category == ThreatCategory.Environmental)
		{
			// 环境威胁：自行处理（施加伤害或封锁路径）
			_envThreatActive = true;
			try { _applyExplorationHullDamageFn?.Invoke(2); } // MVP 固定 2 点
			catch { /* 不崩溃 */ }
		}
		else if (tp.Category == ThreatCategory.Guard)
		{
			// 守卫威胁：传递至 CombatManager
			bool combatAvailable = _isCombatManagerAvailableFn?.Invoke() ?? false;
			if (!combatAvailable)
			{
				// #12 不可用 → 惰性降级（inert）
				// 守卫保持活跃但不触发伤害
				return;
			}
			try { _initiateThreatFn?.Invoke(tp.ThreatId, tp.Category); }
			catch { /* 不崩溃 */ }
		}
	}

	/// <summary>
	/// 战斗结果回调（由 CombatManager #12 调用）。
	/// </summary>
	/// <param name="outcome">"suppressed"/"tanked"/"retreated"。</param>
	/// <param name="threatId">对应的威胁 ID。</param>
	public void OnCombatResult(string outcome, string threatId)
	{
		switch (outcome)
		{
			case "suppressed":
				var tp = _sessionThreatPoints.FirstOrDefault(t => t.ThreatId == threatId);
				if (tp != null) tp.IsActive = false;
				ThreatCleared?.Invoke(threatId);
				break;
			case "tanked":
				// 威胁保持活跃
				break;
			case "retreated":
				_retreatFlagged = true;
				ForceExtraction("retreat");
				break;
		}
		SetSubstate(ExplorationSubstate.Idle);
	}

	/// <summary>
	/// F-11-03 侦察预览等级映射。
	/// 使用进入探索时快照的 η_scout——不反映实时变化。
	/// </summary>
	/// <returns>预览等级。</returns>
	public ScoutPreviewLevel GetScoutPreviewLevel()
	{
		if (_etaScoutSnapshot <= 0.0) return ScoutPreviewLevel.None;
		if (_etaScoutSnapshot >= 1.0) return ScoutPreviewLevel.Full;
		return ScoutPreviewLevel.Presence;
	}

	/// <summary>快照当前 η_scout（ARRIVING 时调用）。</summary>
	public void SnapshotEtaScout()
	{
		_etaScoutSnapshot = _getScoutEfficiencyFn?.Invoke() ?? 0.0;
	}

	// ── Story 004 — EncounterContext Consumption & ARRIVING Entry ─────

	// 已记录的 internal error log（测试用）
	private readonly List<string> _internalErrorLog = new();
	// 当前会话的 EncounterContext
	private EncounterContext? _encounterContext;
	// 当前入场模式
	private string _arrivalMode = "";

	/// <summary>内部错误日志列表（测试可读取）。</summary>
	public IReadOnlyList<string> InternalErrorLog => _internalErrorLog;
	/// <summary>当前入场模式（"arrived"/"forced_landing"/"retreated"）。</summary>
	public string ArrivalMode => _arrivalMode;
	/// <summary>当前会话 EncounterContext。</summary>
	public EncounterContext? CurrentEncounterContext => _encounterContext;

	/// <summary>
	/// 校验 EncounterContext，失败时返回 fallback context。
	/// 5 种故障条件：null、缺 route_id、缺 destination_id、无效 voyage_result、resolved_encounters 非列表。
	/// </summary>
	/// <param name="ctx">待校验的 EncounterContext（可为 null）。</param>
	/// <returns>有效 context 或 fallback context。</returns>
	public EncounterContext ValidateEncounterContext(EncounterContext? ctx)
	{
		if (ctx == null)
		{
			LogInternalError("Exploration: null EncounterContext — using fallback");
			return BuildFallbackContext();
		}
		if (string.IsNullOrEmpty(ctx.RouteId))
		{
			LogInternalError("Exploration: EncounterContext missing route_id");
			return BuildFallbackContext();
		}
		if (string.IsNullOrEmpty(ctx.DestinationId))
		{
			LogInternalError("Exploration: EncounterContext missing destination_id");
			return BuildFallbackContext();
		}
		if (ctx.VoyageResult != "arrived" && ctx.VoyageResult != "retreated"
			&& ctx.VoyageResult != "forced_landing")
		{
			LogInternalError($"Exploration: invalid voyage_result '{ctx.VoyageResult}'");
			return BuildFallbackContext();
		}
		// resolved_encounters 已是类型化列表，不需要 is Array 检查——类型安全保证
		return ctx;
	}

	/// <summary>
	/// 消费 EncounterContext 进入探索会话（校验 → 入场模式路由 → ARRIVING 阶段）。
	/// </summary>
	/// <param name="ctx">来自 Navigation #10 voyage_completed 信号的 EncounterContext。</param>
	/// <returns>是否成功进入。</returns>
	public bool EnterExplorationWithContext(EncounterContext? ctx)
	{
		if (_phase != ExplorationPhase.Idle) return false;

		var validated = ValidateEncounterContext(ctx);
		_encounterContext = validated;
		_arrivalMode = validated.VoyageResult;

		// 快照 η_scout
		SnapshotEtaScout();

		// 进入 ARRIVING
		_currentPointId = validated.DestinationId;
		if (!TransitionPhase(ExplorationPhase.Arriving)) return false;

		// 按 voyage_result 路由入场模式
		if (validated.VoyageResult == "forced_landing"
			&& string.IsNullOrEmpty(validated.ForcedLandingPosition))
		{
			LogInternalError("Exploration: forced_landing without position — using normal entry");
			_arrivalMode = "arrived"; // fallback 至正常入场
		}

		return true;
	}

	/// <summary>构建 fallback EncounterContext（9 字段全为安全默认值）。</summary>
	public static EncounterContext BuildFallbackContext() =>
		new EncounterContext(
			"unknown", "cloudwatch-ruins-fallback", "arrived",
			new List<ResolvedEncounterEntry>(), 0,
			new List<string>(), HullBand.Intact, "", new List<string>());

	private void LogInternalError(string message) => _internalErrorLog.Add(message);

	// ── Story 005 — Extraction Loss Settlement & State Variant Transition ──

	/// <summary>
	/// F-11-04 计算单堆物品损耗量。
	/// qty≤1 或 lambda≤0 时损耗为 0；结果取 min(qty-1, ceil(qty×λ))。
	/// </summary>
	/// <param name="qty">物品数量。</param>
	/// <param name="lambda">损耗系数。</param>
	/// <returns>损耗数量。</returns>
	public static int ComputeLoss(int qty, double lambda)
	{
		if (qty <= 1) return 0;
		if (lambda <= 0.0) return 0;
		int raw = (int)Math.Ceiling(qty * lambda);
		return Math.Min(qty - 1, Math.Max(0, raw));
	}

	/// <summary>
	/// F-11-04 撤离损耗结算。
	/// Unique 物品（IsUnique=true AND MaxStack=1）永不损耗；每堆至少保留 1（qty≤1 则不损）。
	/// retreat_flagged=true 使用 λ_forced=0.25，否则 λ_success=0.08。
	/// </summary>
	/// <param name="carriedStacks">携带物品堆列表。</param>
	/// <param name="retreatFlagged">是否为强制撤退。</param>
	/// <returns>损耗结算结果。</returns>
	public ExtractionSettlementResult ExtractionLossSettlement(
		IReadOnlyList<CarriedStack> carriedStacks, bool retreatFlagged)
	{
		var transferred = new List<(string, int, int)>();
		var lost = new List<(string, int)>();
		int totalLost = 0;

		foreach (var stack in carriedStacks)
		{
			if (stack.IsUnique && stack.MaxStack == 1)
			{
				// Unique 物品：永不损耗，全量转移
				transferred.Add((stack.ResourceId, stack.Quantity, 0));
				continue;
			}

			double lambda = retreatFlagged ? LambdaForced : LambdaSuccess;
			int lossQty = ComputeLoss(stack.Quantity, lambda);
			int retainedQty = stack.Quantity - lossQty;

			if (lossQty > 0)
			{
				lost.Add((stack.ResourceId, lossQty));
				totalLost += lossQty;
			}
			transferred.Add((stack.ResourceId, retainedQty, lossQty));
		}

		return new ExtractionSettlementResult(transferred, lost, totalLost);
	}

	/// <summary>
	/// F-11-05 状态变体转换（8 种规则，env_threat_active 优先）。
	/// </summary>
	/// <param name="currentState">当前搜索点状态。</param>
	/// <param name="allSearched">是否所有搜索点已消耗。</param>
	/// <param name="envThreatActive">环境威胁是否活跃。</param>
	/// <returns>转换后的搜索点状态。</returns>
	public static SearchPointState StateVariantTransition(
		SearchPointState currentState, bool allSearched, bool envThreatActive)
	{
		// 环境威胁活跃时优先 → DangerChanged
		if (envThreatActive) return SearchPointState.DangerChanged;

		return currentState switch
		{
			SearchPointState.Unlooted => allSearched
				? SearchPointState.Looted
				: SearchPointState.Unlooted,
			SearchPointState.Looted => SearchPointState.Looted,
			SearchPointState.DangerChanged => allSearched
				? SearchPointState.Looted
				: SearchPointState.Unlooted,
			_ => currentState,
		};
	}

	// ── Story 006 — 持久化快照、会话恢复与 Pool 一致性 ────────────────

	/// <summary>
	/// 触发通用探索快照（搜索成功路径与撤离触发时调用）。
	/// 内部调用 _captureSnapshotFn；失败时经 HandleSnapshotFailure 处理（30s 防抖配额警告）。
	/// </summary>
	private void TriggerSnapshot()
	{
		if (_captureSnapshotFn == null) return;
		bool ok = _captureSnapshotFn.Invoke();
		if (!ok)
			HandleSnapshotFailure();
	}

	/// <summary>
	/// 快照写入失败处理（AC-14/15）。
	/// 30s 防抖：两次失败间隔不足 30s 时不重复发射 QuotaWarningEmitted。
	/// </summary>
	private void HandleSnapshotFailure()
	{
		double now = _wallClockFn?.Invoke() ?? (Environment.TickCount64 / 1000.0);
		if (now - _lastSnapshotWarningTime >= QuotaWarningCooldown)
		{
			_lastSnapshotWarningTime = now;
			QuotaWarningEmitted?.Invoke();
		}
	}

	/// <summary>
	/// EC-11-09 Pool 5 一致性修复。
	/// 无参数——直接读 _getPoolActualOccupiedFn 与 _getPoolTrackedOccupiedFn 委托获取值；
	/// 若不一致则调用 _poolReconcileFn。
	/// </summary>
	public void ReconcilePool5()
	{
		if (_getPoolActualOccupiedFn == null || _getPoolTrackedOccupiedFn == null) return;
		int actual = _getPoolActualOccupiedFn.Invoke();
		int tracked = _getPoolTrackedOccupiedFn.Invoke();
		if (actual != tracked)
			_poolReconcileFn?.Invoke();
	}

	/// <summary>
	/// 序列化当前探索会话状态（供 #3 Persistence 写入快照）。
	/// 返回 key→value 字典，所有集合均为逗号分隔字符串。
	/// </summary>
	/// <returns>状态字典（空阶段时返回 null）。</returns>
	public Dictionary<string, string>? SerializeExploration()
	{
		if (_phase == ExplorationPhase.Idle) return null;

		var d = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["phase"] = _phase.ToString(),
			["substate"] = _substate.ToString(),
			["current_point_id"] = _currentPointId,
			["extraction_elapsed"] = _extractionElapsed.ToString("R"),
			["extraction_active"] = _extractionActive.ToString(),
			["retreat_flagged"] = _retreatFlagged.ToString(),
			["env_threat_active"] = _envThreatActive.ToString(),
			["arrival_mode"] = _arrivalMode,
			["searched_points"] = string.Join(",", _searchedPoints),
			["interacted_intel_points"] = string.Join(",", _interactedIntelPoints),
			["mark_arriving_interrupted"] = _markArrivingInterrupted.ToString(),
			["mark_extraction_interrupted"] = _markExtractionInterrupted.ToString(),
		};

		// 搜索点状态变体
		var spStateEntries = _searchPointStates
			.Select(kv => $"{kv.Key}:{(int)kv.Value}");
		d["search_point_states"] = string.Join(";", spStateEntries);

		return d;
	}

	/// <summary>
	/// 从快照字典恢复探索会话（供 #3 Persistence 在崩溃恢复时调用）。
	/// 先清除现有状态再写入，最后发射相应恢复信号。
	/// </summary>
	/// <param name="data">SerializeExploration 输出的字典。</param>
	/// <returns>是否恢复成功。</returns>
	public bool DeserializeExploration(IReadOnlyDictionary<string, string> data)
	{
		if (data == null || !data.TryGetValue("phase", out var phaseStr)) return false;
		if (!Enum.TryParse<ExplorationPhase>(phaseStr, out var phase)) return false;
		// AC-20: DEPARTED 不应持久化为活跃会话——忽略并记录 warning
		if (phase == ExplorationPhase.Departed)
		{
			LogInternalError("Exploration: cannot restore DEPARTED phase — ignoring active session");
			return false;
		}

		// 清除当前会话状态
		_searchPointStates.Clear();
		_searchedPoints.Clear();
		_interactedIntelPoints.Clear();
		_sessionThreatPoints.Clear();
		_pendingSettlement = null;
		_extractionElapsed = 0;
		_extractionActive = false;
		_retreatFlagged = false;
		_envThreatActive = false;
		_markArrivingInterrupted = false;
		_markExtractionInterrupted = false;

		// 恢复基本字段
		_currentPointId = data.TryGetValue("current_point_id", out var pid) ? pid : "";
		_arrivalMode = data.TryGetValue("arrival_mode", out var am) ? am : "";

		if (data.TryGetValue("retreat_flagged", out var rf))
			_retreatFlagged = bool.TryParse(rf, out var rfBool) && rfBool;

		if (data.TryGetValue("env_threat_active", out var eta))
			_envThreatActive = bool.TryParse(eta, out var etaBool) && etaBool;

		if (data.TryGetValue("mark_arriving_interrupted", out var mai))
			_markArrivingInterrupted = bool.TryParse(mai, out var maiBool) && maiBool;

		if (data.TryGetValue("mark_extraction_interrupted", out var mei))
			_markExtractionInterrupted = bool.TryParse(mei, out var meiBool) && meiBool;

		// 恢复搜索过的点集合
		if (data.TryGetValue("searched_points", out var sp) && !string.IsNullOrEmpty(sp))
		{
			foreach (var s in sp.Split(',', StringSplitOptions.RemoveEmptyEntries))
				_searchedPoints.Add(s);
		}

		// 恢复已交互情报点集合
		if (data.TryGetValue("interacted_intel_points", out var iip) && !string.IsNullOrEmpty(iip))
		{
			foreach (var s in iip.Split(',', StringSplitOptions.RemoveEmptyEntries))
				_interactedIntelPoints.Add(s);
		}

		// 恢复搜索点状态变体
		if (data.TryGetValue("search_point_states", out var sps) && !string.IsNullOrEmpty(sps))
		{
			foreach (var entry in sps.Split(';', StringSplitOptions.RemoveEmptyEntries))
			{
				var parts = entry.Split(':');
				if (parts.Length == 2 && int.TryParse(parts[1], out var stateVal))
					_searchPointStates[parts[0]] = (SearchPointState)stateVal;
			}
		}

		// 判断恢复阶段：EXTRACTING → 回到 EXPLORING（会话曾在撤离中断，放置玩家在撤离锚点旁）
		bool wasExtracting = phase == ExplorationPhase.Extracting;
		ExplorationPhase restorePhase = wasExtracting ? ExplorationPhase.Exploring : phase;

		// 直接设置阶段（绕过 TransitionPhase 以恢复任意合法阶段），再发射信号
		var prevPhase = _phase;
		_phase = restorePhase;
		if (restorePhase == ExplorationPhase.Exploring)
			_substate = ExplorationSubstate.Idle;

		ExplorationPhaseChanged?.Invoke(prevPhase, _phase, _currentPointId);

		// 发射恢复提示信号
		if (restorePhase == ExplorationPhase.Exploring)
		{
			SessionRestoredNotice?.Invoke();
			if (wasExtracting)
				ExtractionInterruptedOnRestore?.Invoke();
		}

		// EC-11-09：恢复后立即做 Pool 5 一致性检查
		ReconcilePool5();

		return true;
	}

	/// <summary>
	/// 恢复活跃探索会话（崩溃后的入口点）。
	/// 直接设置 _phase = Exploring 并发射 ExplorationPhaseChanged（Idle → Exploring）。
	/// </summary>
	/// <param name="destinationId">恢复会话的目的地探索点 ID。</param>
	public void RestoreActiveSession(string destinationId)
	{
		_currentPointId = destinationId;
		var prev = _phase;
		_phase = ExplorationPhase.Exploring;
		_substate = ExplorationSubstate.Idle;
		ExplorationPhaseChanged?.Invoke(prev, ExplorationPhase.Exploring, _currentPointId);
		SessionRestoredNotice?.Invoke();
	}

	/// <summary>
	/// 页面隐藏/窗口失焦时由 Platform #2 调用（EC-11-20）。
	/// ARRIVING 时标记中断；EXTRACTING 时标记读条需要中断。
	/// </summary>
	public void OnPageHidden()
	{
		if (_phase == ExplorationPhase.Arriving)
			_markArrivingInterrupted = true;
		else if (_phase == ExplorationPhase.Extracting)
			_markExtractionInterrupted = true;
	}

	/// <summary>
	/// 页面恢复可见/窗口获得焦点时由 Platform #2 调用（EC-11-20）。
	/// ARRIVING 中断 → 自动跳过至 EXPLORING；EXTRACTING 中断 → 读条中止并回 EXPLORING。
	/// </summary>
	public void OnPageVisible()
	{
		if (_markArrivingInterrupted && _phase == ExplorationPhase.Arriving)
		{
			_markArrivingInterrupted = false;
			SkipArriving();
		}
		else if (_markExtractionInterrupted && _phase == ExplorationPhase.Extracting)
		{
			_markExtractionInterrupted = false;
			InterruptExtraction("page_visibility");
		}
		else if (_phase == ExplorationPhase.Exploring)
		{
			SetSubstate(ExplorationSubstate.Idle);
		}
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
		// (1) 损耗结算 + 批量转移
		var carried = _getCarriedStacksFn?.Invoke()
			?? Array.Empty<CarriedStack>();
		var settlement = ExtractionLossSettlement(carried, _retreatFlagged);

		bool transferOk = _extractCarriedToStorageFn?.Invoke(
			settlement.Transferred
				.Select(t => new CarriedStack(t.ResourceId, t.Quantity, false, 99))
				.ToList()) ?? true;

		// (2) 情报结算：intelPointId → intelId 转换后揭示
		foreach (var intelPointId in _interactedIntelPoints)
		{
			string intelId = _getIntelIdForPointFn?.Invoke(intelPointId) ?? intelPointId;
			_revealIntelFn?.Invoke(intelId);
		}

		// (3) 状态变体转换（_envThreatActive 已在 Story 003 逻辑中追踪）
		bool allSearched = AllSearchPointsConsumed();
		var newVariant = StateVariantTransition(
			_searchPointStates.TryGetValue(_currentPointId, out var sv) ? sv : SearchPointState.Unlooted,
			allSearched,
			_envThreatActive);
		_searchPointStates[_currentPointId] = newVariant;

		// (4) 阶段 → DEPARTED
		TransitionPhase(ExplorationPhase.Departed);

		// (5) 持久化快照
		bool snapshotOk = transferOk && (_triggerSettlementSnapshotFn?.Invoke(settlement) ?? true);

		if (!snapshotOk)
		{
			// EC-11-03 重试
			AttemptSettlementRetry(settlement, 0);
			return;
		}

		// (6) 发射 extraction_completed
		ExtractionCompleted?.Invoke(settlement.Transferred.Count, _interactedIntelPoints.Count);
	}

	private void AttemptSettlementRetry(ExtractionSettlementResult settlement, int attempt)
	{
		if (attempt >= RetryDelays.Length)
		{
			_pendingSettlement = settlement;
			_emitSettlementFailedUiFn?.Invoke();
			return;
		}
		_pendingSettlement = settlement;
		_scheduleRetryCallbackFn?.Invoke(RetryDelays[attempt], () =>
		{
			bool ok = _triggerSettlementSnapshotFn?.Invoke(_pendingSettlement ?? settlement) ?? true;
			if (!ok)
			{
				AttemptSettlementRetry(settlement, attempt + 1);
			}
			else
			{
				_pendingSettlement = null;
				ExtractionCompleted?.Invoke(settlement.Transferred.Count, _interactedIntelPoints.Count);
			}
		});
	}

	/// <summary>手动重试结算（结算失败 UI 点击"重试"按钮后调用）。</summary>
	/// <returns>重试是否成功；无待处理结算时返回 false。</returns>
	public bool RetrySettlement()
	{
		if (_pendingSettlement == null) return false;
		bool ok = _triggerSettlementSnapshotFn?.Invoke(_pendingSettlement) ?? true;
		if (ok)
		{
			var s = _pendingSettlement;
			_pendingSettlement = null;
			ExtractionCompleted?.Invoke(s.Transferred.Count, _interactedIntelPoints.Count);
		}
		return ok;
	}

	/// <summary>判断本次会话所有注册的搜索点是否已全部消耗。</summary>
	private bool AllSearchPointsConsumed() =>
		_registeredSearchPointCount > 0 && _searchedPoints.Count >= _registeredSearchPointCount;

	private void ClearSessionState()
	{
		_currentPointId = "";
		_substate = ExplorationSubstate.Idle;
		_extractionElapsed = 0;
		_extractionActive = false;
		_retreatFlagged = false;
		_envThreatActive = false;
		_searchedPoints.Clear();
		_interactedIntelPoints.Clear();
		_pendingSettlement = null;
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
