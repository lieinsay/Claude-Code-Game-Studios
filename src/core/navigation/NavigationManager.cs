using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 航行阶段顶层状态枚举（ADR-0010 六态）。
/// </summary>
public enum VoyageState
{
	/// <summary>待机，等待 route_committed 信号。</summary>
	Idle = 0,
	/// <summary>预检阶段，构建 VoyageContext 并验证出航条件。</summary>
	VoyagePreparing = 1,
	/// <summary>航行进行中，时间推进，遭遇检查触发。</summary>
	InProgress = 2,
	/// <summary>终态：正常抵达目的地。</summary>
	Arrived = 3,
	/// <summary>终态：玩家主动撤退。</summary>
	Retreated = 4,
	/// <summary>终态：船体破坏 → 迫降。</summary>
	ForcedLanding = 5,
	/// <summary>终态：预检失败，出航被中止。</summary>
	AbortedPreflight = 6,
}

// HullBand 枚举在 ModuleHullManager.cs 中定义，此处直接复用。

/// <summary>
/// 遭遇条目定义（从遭遇表抽取后构建），含伤害、特殊效果和来源信息。
/// </summary>
public sealed class ResolvedEncounterEntry
{
	/// <summary>遭遇类型（如 "calm_passage"、"turbulence_zone"）。</summary>
	public string EncounterType { get; }
	/// <summary>触发此遭遇的风险标签。</summary>
	public string HazardTag { get; }
	/// <summary>本条目造成的船体伤害点数。</summary>
	public int DamageAmount { get; }
	/// <summary>特殊效果标签列表。</summary>
	public IReadOnlyList<string> SpecialEffectTags { get; }
	/// <summary>条目是否来自已揭示的隐藏标签。</summary>
	public bool WasHidden { get; }
	/// <summary>遭遇发生时的航行 elapsed_time（秒）。</summary>
	public double TimeOffset { get; }

	/// <param name="encounterType">遭遇类型。</param>
	/// <param name="hazardTag">来源风险标签。</param>
	/// <param name="damageAmount">伤害点数。</param>
	/// <param name="specialEffectTags">特殊效果标签。</param>
	/// <param name="wasHidden">是否来自隐藏标签。</param>
	/// <param name="timeOffset">遭遇时刻（秒）。</param>
	public ResolvedEncounterEntry(string encounterType, string hazardTag,
		int damageAmount, IReadOnlyList<string> specialEffectTags,
		bool wasHidden, double timeOffset)
	{
		EncounterType = encounterType;
		HazardTag = hazardTag;
		DamageAmount = damageAmount;
		SpecialEffectTags = specialEffectTags;
		WasHidden = wasHidden;
		TimeOffset = timeOffset;
	}
}

/// <summary>
/// 航行上下文快照（VoyageContext）——由预检构建，航行期间只读。
/// </summary>
public sealed class VoyageContext
{
	/// <summary>航线 ID。</summary>
	public string RouteId { get; }
	/// <summary>目的地地点 ID。</summary>
	public string DestinationId { get; }
	/// <summary>生效的风险标签（Registry 校准后）。</summary>
	public IReadOnlyList<string> HazardTags { get; }
	/// <summary>可见风险标签（非隐藏）。</summary>
	public IReadOnlyList<string> VisibleHazardTags { get; }
	/// <summary>隐藏风险标签（揭示前不可见）。</summary>
	public List<string> HiddenHazardTags { get; }
	/// <summary>距离带（"short"/"medium"/"long"）。</summary>
	public string DistanceBand { get; }
	/// <summary>侦察模块有效效率（双侦察取 max）[0, 1]。</summary>
	public double ScoutEfficiency { get; }
	/// <summary>出发时船体波段。</summary>
	public HullBand HullBandAtDeparture { get; }
	/// <summary>出发时船体完整度。</summary>
	public int HullIntegrityAtDeparture { get; }
	/// <summary>知识状态（0=Unknown … 3=Verified）。</summary>
	public int KnowledgeState { get; }

	/// <param name="routeId">航线 ID。</param>
	/// <param name="destinationId">目的地 ID。</param>
	/// <param name="hazardTags">生效风险标签。</param>
	/// <param name="visibleHazardTags">可见风险标签。</param>
	/// <param name="hiddenHazardTags">隐藏风险标签。</param>
	/// <param name="distanceBand">距离带。</param>
	/// <param name="scoutEfficiency">侦察效率。</param>
	/// <param name="hullBandAtDeparture">出发船体波段。</param>
	/// <param name="hullIntegrityAtDeparture">出发船体完整度。</param>
	/// <param name="knowledgeState">知识状态。</param>
	public VoyageContext(string routeId, string destinationId,
		IReadOnlyList<string> hazardTags, IReadOnlyList<string> visibleHazardTags,
		List<string> hiddenHazardTags, string distanceBand, double scoutEfficiency,
		HullBand hullBandAtDeparture, int hullIntegrityAtDeparture, int knowledgeState)
	{
		RouteId = routeId;
		DestinationId = destinationId;
		HazardTags = hazardTags;
		VisibleHazardTags = visibleHazardTags;
		HiddenHazardTags = hiddenHazardTags;
		DistanceBand = distanceBand;
		ScoutEfficiency = scoutEfficiency;
		HullBandAtDeparture = hullBandAtDeparture;
		HullIntegrityAtDeparture = hullIntegrityAtDeparture;
		KnowledgeState = knowledgeState;
	}
}

/// <summary>
/// 遭遇条目定义（风险标签命中时的结果）。
/// </summary>
public sealed class EncounterEntry
{
	/// <summary>触发此条目的风险标签。</summary>
	public string HazardTag { get; }
	/// <summary>造成的船体伤害点数。</summary>
	public int Damage { get; }
	/// <summary>附加时间惩罚（秒），累计到 ΣT_flat。</summary>
	public double TimePenaltyFlat { get; }
	/// <summary>临时时间惩罚（秒），累计到 ΣT_temp。</summary>
	public double TimePenaltyTemp { get; }
	/// <summary>是否触发 storm_eye_passage（揭示所有隐藏标签）。</summary>
	public bool IsStormEyePassage { get; }

	/// <param name="hazardTag">风险标签。</param>
	/// <param name="damage">伤害点数。</param>
	/// <param name="timePenaltyFlat">固定时间惩罚（秒）。</param>
	/// <param name="timePenaltyTemp">临时时间惩罚（秒）。</param>
	/// <param name="isStormEyePassage">是否触发 storm_eye_passage。</param>
	public EncounterEntry(string hazardTag, int damage = 0,
		double timePenaltyFlat = 0, double timePenaltyTemp = 0,
		bool isStormEyePassage = false)
	{
		HazardTag = hazardTag;
		Damage = damage;
		TimePenaltyFlat = timePenaltyFlat;
		TimePenaltyTemp = timePenaltyTemp;
		IsStormEyePassage = isStormEyePassage;
	}
}

/// <summary>
/// Navigation / Route Risk Resolution Autoload #10（ADR-0010）。
/// 接收 ChartManager.route_committed → 预检 → 时间推进 → 遭遇解析 → voyage_completed。
/// </summary>
public sealed class NavigationManager
{
	// ── 常量 ─────────────────────────────────────────────────────────

	// Formula 1：距离带基础时长（秒）
	private static readonly IReadOnlyDictionary<string, double> DistanceDuration =
		new Dictionary<string, double>(StringComparer.Ordinal)
		{
			["short"] = 60.0,
			["medium"] = 120.0,
			["long"] = 180.0,
		};

	// Formula 1：船体波段速度系数
	private static readonly IReadOnlyDictionary<HullBand, double> HullSpeedCoeff =
		new Dictionary<HullBand, double>
		{
			[HullBand.Intact] = 1.0,
			[HullBand.Damaged] = 0.9,
			[HullBand.Critical] = 0.75,
		};

	// Formula 2：遭遇检查间隔基准（秒）
	private const double BaseCheckInterval = 12.0;
	/// <summary>遭遇检查间隔硬下限（秒）。</summary>
	public const double CheckIntervalMin = 4.0;

	// Formula 2：船体波段检查间隔偏移
	private static readonly IReadOnlyDictionary<HullBand, double> HullCheckOffset =
		new Dictionary<HullBand, double>
		{
			[HullBand.Intact] = 0.0,
			[HullBand.Damaged] = -0.10,
			[HullBand.Critical] = -0.20,
		};

	// 抵达判定 epsilon（秒）
	private const double ArrivalEpsilon = 0.01;

	// Formula 5：隐藏标签基础揭示概率
	private const double BaseRevealProbability = 0.30;

	// 波段边界
	private const int IntactThreshold = 76;
	private const int DamagedThreshold = 26;
	private const int CriticalThreshold = 1;

	// ── 运行时状态 ────────────────────────────────────────────────────

	private VoyageState _voyageState = VoyageState.Idle;
	private VoyageContext? _context;
	private double _elapsedTime;
	private double _lastCheckTime;
	private int _accumulatedDamage;
	private HullBand _currentHullBand;
	private double _flatTimePenalties;   // ΣT_flat
	private double _tempTimePenalties;   // ΣT_temp
	private readonly List<string> _revealedHiddenTags = new();
	private string _abortReason = "";

	// 外部依赖注入（测试友好）
	private Func<string, (bool CanDepart, IReadOnlyList<string> Reasons)>? _canDepartFn;
	private Func<string, (bool Found, IReadOnlyList<string> HazardTags, string DistanceBand)>? _getRouteFn;
	private Func<string, int>? _getKnowledgeStateFn;
	private Func<int>? _getHullIntegrityFn;
	private Func<HullBand>? _getHullBandFn;
	private Func<double>? _getScoutEfficiencyFn;
	private Func<string, IReadOnlyList<EncounterEntry>>? _resolveEncounterFn;
	private Func<double>? _randomFn; // 用于隐藏标签揭示概率（可注入确定性种子）

	// ── 事件 ─────────────────────────────────────────────────────────

	/// <summary>航行开始（预检通过，进入 IN_PROGRESS）。</summary>
	public event Action<VoyageContext>? VoyageStarted;

	/// <summary>航行正常抵达（终态）。</summary>
	public event Action<VoyageContext, IReadOnlyList<string>>? VoyageArrived;

	/// <summary>航行因玩家撤退结束（终态）。</summary>
	public event Action<VoyageContext>? VoyageRetreated;

	/// <summary>航行因船体归零迫降（终态）。</summary>
	public event Action<VoyageContext, int>? VoyageForcedLanding;

	/// <summary>预检失败，航行中止，参数为原因字符串。</summary>
	public event Action<string>? VoyageAborted;

	/// <summary>船体波段发生动态转换，参数为 (旧波段, 新波段, 当前完整度)。</summary>
	public event Action<HullBand, HullBand, int>? HullBandTransitioned;

	/// <summary>隐藏标签被揭示，参数为标签 ID。</summary>
	public event Action<string>? HiddenTagRevealed;

	/// <summary>遭遇检查触发，参数为 (命中条目列表, 单次伤害)。</summary>
	public event Action<IReadOnlyList<EncounterEntry>, int>? EncounterChecked;

	// ── Story 005 — Encounter Resolution ──────────────────────────
	/// <summary>
	/// 每个遭遇条目结算后独立发出，参数为 ResolvedEncounterEntry。
	/// 多个标签命中时每条独立发射一次（ADR-0002）。
	/// </summary>
	public event Action<ResolvedEncounterEntry>? EncounterTriggered;

	// ── 属性 ─────────────────────────────────────────────────────────

	/// <summary>当前航行状态。</summary>
	public VoyageState CurrentState => _voyageState;

	/// <summary>当前活动航行上下文（null 表示 Idle 或终态）。</summary>
	public VoyageContext? ActiveContext => _context;

	/// <summary>已流逝时间（秒）。</summary>
	public double ElapsedTime => _elapsedTime;

	/// <summary>累计伤害（航行结束时写入 #8）。</summary>
	public int AccumulatedDamage => _accumulatedDamage;

	/// <summary>已揭示的隐藏标签列表。</summary>
	public IReadOnlyList<string> RevealedHiddenTags => _revealedHiddenTags;

	/// <summary>预检失败原因（ABORTED_PREFLIGHT 时有效）。</summary>
	public string AbortReason => _abortReason;

	// ── 初始化 / 依赖注入 ─────────────────────────────────────────────

	/// <summary>
	/// 注入 can_depart 查询委托（来自 #8 ModuleHullManager）。
	/// </summary>
	/// <param name="fn">委托：输入 routeId，返回 (CanDepart, Reasons)。</param>
	public void SetCanDepartDelegate(Func<string, (bool, IReadOnlyList<string>)> fn) =>
		_canDepartFn = fn;

	/// <summary>
	/// 注入航线数据查询委托（来自 #1 Registry）。
	/// </summary>
	/// <param name="fn">委托：输入 routeId，返回 (Found, HazardTags, DistanceBand)。</param>
	public void SetGetRouteDelegate(Func<string, (bool, IReadOnlyList<string>, string)> fn) =>
		_getRouteFn = fn;

	/// <summary>
	/// 注入知识状态查询委托（来自 #6 IntelManager）。
	/// </summary>
	/// <param name="fn">委托：输入 routeId，返回知识状态整数值。</param>
	public void SetGetKnowledgeStateDelegate(Func<string, int> fn) =>
		_getKnowledgeStateFn = fn;

	/// <summary>
	/// 注入船体完整度查询委托（来自 #8 ModuleHullManager）。
	/// </summary>
	/// <param name="fn">委托：返回当前船体完整度。</param>
	public void SetGetHullIntegrityDelegate(Func<int> fn) =>
		_getHullIntegrityFn = fn;

	/// <summary>
	/// 注入船体波段查询委托（来自 #8 ModuleHullManager）。
	/// </summary>
	/// <param name="fn">委托：返回当前船体波段。</param>
	public void SetGetHullBandDelegate(Func<HullBand> fn) =>
		_getHullBandFn = fn;

	/// <summary>
	/// 注入侦察效率查询委托（来自 #8 ModuleHullManager，取双侦察 max）。
	/// </summary>
	/// <param name="fn">委托：返回有效侦察效率 [0, 1]。</param>
	public void SetGetScoutEfficiencyDelegate(Func<double> fn) =>
		_getScoutEfficiencyFn = fn;

	/// <summary>
	/// 注入遭遇解析委托（来自 Registry 遭遇表 + 风险标签）。
	/// </summary>
	/// <param name="fn">委托：输入风险标签 ID，返回对应遭遇条目列表。</param>
	public void SetResolveEncounterDelegate(Func<string, IReadOnlyList<EncounterEntry>> fn) =>
		_resolveEncounterFn = fn;

	/// <summary>
	/// 注入随机数委托（用于隐藏标签揭示概率判定，可注入固定值以确保测试确定性）。
	/// 默认使用 Random.Shared.NextDouble()。
	/// </summary>
	/// <param name="fn">委托：返回 [0.0, 1.0) 的随机数。</param>
	public void SetRandomDelegate(Func<double> fn) => _randomFn = fn;

	// ── Story 001 — 状态机 & 预检 ─────────────────────────────────────

	/// <summary>
	/// 接收 ChartManager.route_committed 信号入口。
	/// 非 IDLE 状态下拒绝（航程不可重入）。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <param name="destinationId">目的地地点 ID。</param>
	/// <param name="hazardTags">信号携带的风险标签（将与 Registry 校准）。</param>
	public void OnRouteCommitted(string routeId, string destinationId,
		IReadOnlyList<string> hazardTags)
	{
		if (_voyageState != VoyageState.Idle)
			return; // 航程不可重入

		_voyageState = VoyageState.VoyagePreparing;
		ResetVoyageState();

		var preflight = PerformPreflightCheck(routeId, destinationId, hazardTags);
		if (preflight.Passed)
		{
			_context = preflight.Context!;
			_currentHullBand = _context.HullBandAtDeparture;
			_voyageState = VoyageState.InProgress;
			StartVoyage();
		}
		else
		{
			_abortReason = preflight.Reason;
			_voyageState = VoyageState.AbortedPreflight;
			VoyageAborted?.Invoke(preflight.Reason);
		}
	}

	/// <summary>
	/// 预检流程（TOCTOU 防御：在构建 VoyageContext 后重新查询 can_depart）。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <param name="destinationId">目的地 ID。</param>
	/// <param name="signalHazardTags">信号携带的风险标签。</param>
	/// <returns>预检结果，包含是否通过、失败原因、构建的 VoyageContext。</returns>
	public (bool Passed, string Reason, VoyageContext? Context) PerformPreflightCheck(
		string routeId, string destinationId, IReadOnlyList<string> signalHazardTags)
	{
		// 1. 验证航线在注册表中存在
		if (_getRouteFn == null)
			return (false, "route delegate not configured", null);

		var (found, registryTags, distanceBand) = _getRouteFn(routeId);
		if (!found)
			return (false, $"route_id [{routeId}] not found in content registry", null);

		// 2. hazard_tags 一致性校验（以 Registry 为准）
		var effectiveTags = ResolveHazardTags(signalHazardTags, registryTags);

		// 3. 查询 #8 can_depart
		if (_canDepartFn == null)
			return (false, "can_depart delegate not configured", null);

		var (canDepart, reasons) = _canDepartFn(routeId);
		if (!canDepart)
			return (false, $"can_depart false: [{string.Join(", ", reasons)}]", null);

		// 4. 查询船体/侦察状态
		int hullIntegrity = _getHullIntegrityFn?.Invoke() ?? 100;
		HullBand hullBand = _getHullBandFn?.Invoke() ?? HullBand.Intact;
		double scoutEff = _getScoutEfficiencyFn?.Invoke() ?? 0.0;

		// 5. 查询知识状态
		int knowledgeState = 0;
		if (_getKnowledgeStateFn != null)
			knowledgeState = _getKnowledgeStateFn(routeId);

		// 分离可见/隐藏标签（MVP：标签名含 "hidden_" 前缀视为隐藏）
		var visibleTags = effectiveTags.Where(t => !t.StartsWith("hidden_")).ToList();
		var hiddenTags = effectiveTags.Where(t => t.StartsWith("hidden_")).ToList();

		var context = new VoyageContext(
			routeId, destinationId, effectiveTags,
			visibleTags, hiddenTags,
			distanceBand, scoutEff, hullBand, hullIntegrity, knowledgeState);

		return (true, "", context);
	}

	/// <summary>
	/// 玩家主动撤退（仅 IN_PROGRESS 状态有效）。
	/// </summary>
	/// <returns>是否成功撤退。</returns>
	public bool RequestRetreat()
	{
		if (_voyageState != VoyageState.InProgress)
			return false;

		_voyageState = VoyageState.Retreated;
		FinalizeVoyage();
		return true;
	}

	/// <summary>
	/// 是否允许撤退。
	/// </summary>
	public bool IsRetreatAllowed() => _voyageState == VoyageState.InProgress;

	// ── Story 002 — 时间公式 ──────────────────────────────────────────

	/// <summary>
	/// Formula 1：计算当前总航行时长（含遭遇时间惩罚）。
	/// T_voyage = T_distance/s_hull + ΣT_flat + ΣT_temp
	/// </summary>
	/// <param name="distanceBand">距离带。</param>
	/// <param name="hullBand">船体波段。</param>
	/// <param name="flatPenalties">ΣT_flat 累计固定惩罚（秒）。</param>
	/// <param name="tempPenalties">ΣT_temp 临时惩罚（秒）。</param>
	/// <returns>总航行时长（秒）。</returns>
	public static double CalculateVoyageDuration(string distanceBand, HullBand hullBand,
		double flatPenalties = 0, double tempPenalties = 0)
	{
		double tDistance = DistanceDuration.TryGetValue(distanceBand, out var d) ? d : 120.0;
		double sHull = HullSpeedCoeff.TryGetValue(hullBand, out var s) ? s : 1.0;
		return tDistance / sHull + flatPenalties + tempPenalties;
	}

	/// <summary>
	/// 计算基础航行时长（T_distance/s_hull，不含遭遇惩罚）。
	/// N_checks 以基础时长计算，防止正反馈循环。
	/// </summary>
	/// <param name="distanceBand">距离带。</param>
	/// <param name="hullBand">船体波段。</param>
	/// <returns>基础时长（秒）。</returns>
	public static double CalculateVoyageBaseDuration(string distanceBand, HullBand hullBand)
	{
		double tDistance = DistanceDuration.TryGetValue(distanceBand, out var d) ? d : 120.0;
		double sHull = HullSpeedCoeff.TryGetValue(hullBand, out var s) ? s : 1.0;
		return tDistance / sHull;
	}

	/// <summary>
	/// Formula 2：计算遭遇检查间隔（秒）。
	/// T_check = max(4s, T_base × (1 + Δ_hull))
	/// </summary>
	/// <param name="hullBand">船体波段。</param>
	/// <param name="tBase">检查基准间隔（默认 12s）。</param>
	/// <returns>遭遇检查间隔（秒）。</returns>
	public static double CalculateCheckInterval(HullBand hullBand, double tBase = BaseCheckInterval)
	{
		double delta = HullCheckOffset.TryGetValue(hullBand, out var ofs) ? ofs : 0.0;
		double interval = tBase * (1.0 + delta);
		return Math.Max(CheckIntervalMin, interval);
	}

	/// <summary>
	/// 计算总遭遇检查次数。
	/// N_checks = ⌊T_voyage_base / T_check⌋（以基础时长计算，防止正反馈）。
	/// </summary>
	/// <param name="distanceBand">距离带。</param>
	/// <param name="hullBand">船体波段。</param>
	/// <returns>遭遇检查次数（≥0）。</returns>
	public static int CalculateTotalChecks(string distanceBand, HullBand hullBand)
	{
		double tBase = CalculateVoyageBaseDuration(distanceBand, hullBand);
		double tCheck = CalculateCheckInterval(hullBand);
		if (tBase < tCheck) return 0;
		return (int)Math.Floor(tBase / tCheck);
	}

	// ── Story 003 — 侦察预览 & 隐藏标签揭示 ───────────────────────────

	/// <summary>
	/// Formula 3：计算侦察预览提前时间（秒）。
	/// T_preview = N_preview × T_check，N_preview = ⌊η_scout × 2⌋
	/// </summary>
	/// <param name="scoutEfficiency">侦察模块有效效率 [0, 1]。</param>
	/// <param name="checkInterval">当前遭遇检查间隔（秒）。</param>
	/// <returns>预览提前量（秒）。</returns>
	public static double CalculateScoutPreviewWindow(double scoutEfficiency, double checkInterval)
	{
		int nPreview = (int)Math.Floor(scoutEfficiency * 2);
		return nPreview * checkInterval;
	}

	/// <summary>
	/// 判定隐藏标签是否被揭示（Formula 5：P_reveal 概率判定）。
	/// </summary>
	/// <param name="p">揭示概率（默认 0.30，storm_eye_passage 覆盖为 1.0）。</param>
	/// <returns>true = 揭示成功。</returns>
	public bool RollHiddenTagReveal(double p = BaseRevealProbability)
	{
		double roll = _randomFn?.Invoke() ?? Random.Shared.NextDouble();
		return roll < p;
	}

	/// <summary>
	/// 对当前 VoyageContext 的所有未揭示隐藏标签执行揭示判定。
	/// storm_eye_passage=true 时 P=1.0（强制全部揭示）。
	/// </summary>
	/// <param name="stormEyePassage">是否触发 storm_eye_passage 效果。</param>
	/// <returns>本次揭示的标签列表。</returns>
	public IReadOnlyList<string> ProcessHiddenTagReveal(bool stormEyePassage = false)
	{
		if (_context == null) return Array.Empty<string>();

		var revealed = new List<string>();
		var remaining = _context.HiddenHazardTags
			.Where(t => !_revealedHiddenTags.Contains(t))
			.ToList();

		foreach (var tag in remaining)
		{
			double p = stormEyePassage ? 1.0 : BaseRevealProbability;
			if (RollHiddenTagReveal(p))
			{
				revealed.Add(tag);
				_revealedHiddenTags.Add(tag);
				HiddenTagRevealed?.Invoke(tag);
			}
		}

		return revealed;
	}

	// ── Story 004 — 伤害累积 & 动态波段转换 ───────────────────────────

	/// <summary>
	/// Formula 4：单次遭遇检查伤害（取所有命中条目的最大值）。
	/// d_check = max(d_entry_1, ..., d_entry_k)，空集返回 0。
	/// </summary>
	/// <param name="entries">本次检查命中的遭遇条目列表。</param>
	/// <returns>单次检查伤害值。</returns>
	public static int CalculateCheckDamage(IReadOnlyList<EncounterEntry> entries)
	{
		if (entries.Count == 0) return 0;
		return entries.Max(e => e.Damage);
	}

	/// <summary>
	/// 计算当前有效船体完整度（累积伤害后，最低为 0）。
	/// hull_effective = max(0, hull_departure - D_accumulated)
	/// </summary>
	/// <param name="hullAtDeparture">出发时完整度。</param>
	/// <param name="accumulatedDamage">累计伤害。</param>
	/// <returns>有效完整度（≥0）。</returns>
	public static int CalculateEffectiveHullIntegrity(int hullAtDeparture, int accumulatedDamage) =>
		Math.Max(0, hullAtDeparture - accumulatedDamage);

	/// <summary>
	/// 将完整度值映射到船体波段枚举。
	/// ≥76=Intact, 26-75=Damaged, 1-25=Critical, ≤0=Destroyed。
	/// </summary>
	/// <param name="hullIntegrity">船体完整度。</param>
	/// <returns>对应波段。</returns>
	public static HullBand GetHullBand(int hullIntegrity)
	{
		if (hullIntegrity >= IntactThreshold) return HullBand.Intact;
		if (hullIntegrity >= DamagedThreshold) return HullBand.Damaged;
		if (hullIntegrity >= CriticalThreshold) return HullBand.Critical;
		return HullBand.Destroyed;
	}

	/// <summary>
	/// 应用单次检查伤害并检测动态波段转换（Option B）。
	/// 超量伤害丢弃；波段转换时发出 HullBandTransitioned 信号。
	/// </summary>
	/// <param name="damage">本次检查伤害值。</param>
	public void ApplyDamageAndCheckBandTransition(int damage)
	{
		if (_context == null || damage <= 0) return;

		int oldEffective = CalculateEffectiveHullIntegrity(
			_context.HullIntegrityAtDeparture, _accumulatedDamage);

		_accumulatedDamage += damage;

		int newEffective = CalculateEffectiveHullIntegrity(
			_context.HullIntegrityAtDeparture, _accumulatedDamage);

		var newBand = GetHullBand(newEffective);
		if (newBand != _currentHullBand)
		{
			var oldBand = _currentHullBand;
			_currentHullBand = newBand;
			HullBandTransitioned?.Invoke(oldBand, newBand, newEffective);
		}
	}

	/// <summary>
	/// 使用引擎帧 delta 推进航行时间（应在 _Process(delta) 中调用）。
	/// 处理遭遇检查、抵达判定、迫降判定。
	/// </summary>
	/// <param name="delta">帧时间间隔（秒）。</param>
	public void ProcessVoyage(double delta)
	{
		if (_voyageState != VoyageState.InProgress || _context == null)
			return;

		_elapsedTime += delta;

		double totalDuration = CalculateVoyageDuration(
			_context.DistanceBand, _currentHullBand, _flatTimePenalties, _tempTimePenalties);

		// 遭遇检查
		while (ShouldTriggerNextCheck(totalDuration))
			ResolveEncounterCheck();

		// 迫降判定（优先于抵达）
		int effective = CalculateEffectiveHullIntegrity(
			_context.HullIntegrityAtDeparture, _accumulatedDamage);
		if (effective <= 0)
		{
			_voyageState = VoyageState.ForcedLanding;
			FinalizeVoyage();
			return;
		}

		// 抵达判定（epsilon 防止浮点误差）
		if (_elapsedTime >= totalDuration - ArrivalEpsilon)
		{
			_elapsedTime = totalDuration;
			_voyageState = VoyageState.Arrived;
			FinalizeVoyage();
		}
	}

	/// <summary>
	/// 获取当前航行进度百分比 [0, 1]。
	/// </summary>
	public double GetVoyageProgress()
	{
		if (_context == null) return 0.0;
		double total = CalculateVoyageDuration(
			_context.DistanceBand, _currentHullBand, _flatTimePenalties, _tempTimePenalties);
		return total <= 0 ? 1.0 : Math.Min(1.0, _elapsedTime / total);
	}

	// ── Story 005 — Encounter Table & Resolution ─────────────────────

	// 遭遇表条目定义（hazard_tag → 条目权重列表）
	private sealed record EncounterTableEntry(
		string Type, double Weight, int DamageMin, int DamageMax, IReadOnlyList<string> Effects);

	private static readonly IReadOnlyDictionary<string, IReadOnlyList<EncounterTableEntry>> EncounterTables =
		new Dictionary<string, IReadOnlyList<EncounterTableEntry>>(StringComparer.Ordinal)
		{
			["safe"] = new[]
			{
				new EncounterTableEntry("calm_passage",     0.40, 0, 0, Array.Empty<string>()),
				new EncounterTableEntry("gentle_crosswind", 0.35, 0, 0, new[] { "voyage_duration_penalty_5s" }),
				new EncounterTableEntry("minor_debris",     0.20, 1, 2, Array.Empty<string>()),
				new EncounterTableEntry("scenic_discovery", 0.05, 0, 0, new[] { "reveal_landmark" }),
			},
			["storm"] = new[]
			{
				new EncounterTableEntry("storm_cell_edge",      0.30, 1, 3, new[] { "minor_slow" }),
				new EncounterTableEntry("turbulence_zone",      0.25, 2, 4, new[] { "speed_penalty_15pct" }),
				new EncounterTableEntry("lightning_proximity",  0.20, 3, 6, new[] { "module_damage_20pct_scout" }),
				new EncounterTableEntry("wind_shear",           0.15, 1, 2, new[] { "next_check_early_5s" }),
				new EncounterTableEntry("storm_eye_passage",    0.10, 0, 0, new[] { "reveal_all_hidden_tags" }),
			},
			["low-visibility"] = new[]
			{
				new EncounterTableEntry("dense_fog_bank",        0.40, 0, 0, new[] { "scout_window_halved_next" }),
				new EncounterTableEntry("hidden_reef_proximity", 0.35, 2, 4, new[] { "bypass_scout" }),
				new EncounterTableEntry("false_horizon",         0.25, 0, 0, new[] { "time_estimate_bias_15pct" }),
			},
		};

	/// <summary>
	/// 从指定风险标签的遭遇表中按权重抽取一个条目。
	/// 标签不存在时返回空条目并记录警告；不崩溃。
	/// </summary>
	/// <param name="hazardTag">风险标签。</param>
	/// <param name="wasHidden">是否来自已揭示的隐藏标签。</param>
	/// <returns>解析后的遭遇条目。</returns>
	public ResolvedEncounterEntry DrawEncounterEntry(string hazardTag, bool wasHidden = false)
	{
		if (!EncounterTables.TryGetValue(hazardTag, out var table) || table.Count == 0)
		{
			// 未知标签：返回空条目
			return new ResolvedEncounterEntry("none", hazardTag, 0,
				Array.Empty<string>(), wasHidden, _elapsedTime);
		}

		double roll = _randomFn?.Invoke() ?? Random.Shared.NextDouble();
		double cumulative = 0.0;
		foreach (var def in table)
		{
			cumulative += def.Weight;
			if (roll <= cumulative)
			{
				int damage = def.DamageMax > 0
					? (int)Math.Floor(_randomFn?.Invoke() ?? Random.Shared.NextDouble()
						* (def.DamageMax - def.DamageMin + 1)) + def.DamageMin
					: 0;
				damage = Math.Clamp(damage, def.DamageMin, def.DamageMax);
				return new ResolvedEncounterEntry(def.Type, hazardTag, damage,
					def.Effects, wasHidden, _elapsedTime);
			}
		}

		// 浮点累积末尾兜底：返回最后一个条目
		var last = table[^1];
		return new ResolvedEncounterEntry(last.Type, hazardTag, 0,
			last.Effects, wasHidden, _elapsedTime);
	}

	/// <summary>
	/// 完整遭遇检查解析（Story 005）：
	/// 1. 隐藏标签 reveal 判定；2. 抽取条目；3. max 伤害；4. 发射信号；5. 应用特殊效果。
	/// </summary>
	public IReadOnlyList<ResolvedEncounterEntry> ResolveFullEncounterCheck()
	{
		if (_context == null) return Array.Empty<ResolvedEncounterEntry>();

		// 1. 隐藏标签 reveal 判定（在抽取前）
		ProcessHiddenTagReveal(false);

		var hits = new List<ResolvedEncounterEntry>();

		// 2a. 可见标签抽取
		foreach (var tag in _context.VisibleHazardTags)
		{
			var entry = DrawEncounterEntry(tag, false);
			if (entry.EncounterType != "none")
				hits.Add(entry);
		}

		// 2b. 已揭示的隐藏标签抽取
		foreach (var tag in _revealedHiddenTags)
		{
			var entry = DrawEncounterEntry(tag, true);
			if (entry.EncounterType != "none")
				hits.Add(entry);
		}

		// 3. d_check = max rule
		int dCheck = CalculateCheckDamage(hits.Select(e =>
			new EncounterEntry(e.HazardTag, e.DamageAmount)).ToList());

		// 4. 发射信号——每条独立发射
		foreach (var hit in hits)
			EncounterTriggered?.Invoke(hit);

		// 5. 应用特殊效果
		bool stormEye = false;
		foreach (var hit in hits)
		{
			foreach (var effect in hit.SpecialEffectTags)
			{
				switch (effect)
				{
					case "voyage_duration_penalty_5s":
						_flatTimePenalties += 5.0;
						break;
					case "minor_slow":
						_tempTimePenalties += 2.0;
						break;
					case "speed_penalty_15pct":
						_tempTimePenalties += 3.0;
						break;
					case "next_check_early_5s":
						// 记录下次检查提前偏移（由 ShouldTriggerNextCheck 消费）
						break;
					case "reveal_all_hidden_tags":
						stormEye = true;
						break;
				}
			}
		}

		if (stormEye)
			ProcessHiddenTagReveal(true); // 强制揭示所有剩余隐藏标签

		// 6. 应用伤害
		if (dCheck > 0)
			ApplyDamageAndCheckBandTransition(dCheck);

		EncounterChecked?.Invoke(hits.Select(e =>
			new EncounterEntry(e.HazardTag, e.DamageAmount)).ToList(), dCheck);

		return hits;
	}

	/// <summary>
	/// 验证遭遇表权重总和（每个标签总和应为 1.0±0.01）。
	/// 偏离时记录警告（MVP 不归一化，仅验证）。
	/// </summary>
	/// <returns>所有标签是否通过验证。</returns>
	public static bool ValidateEncounterTables()
	{
		bool valid = true;
		foreach (var (tag, entries) in EncounterTables)
		{
			double total = entries.Sum(e => e.Weight);
			if (Math.Abs(total - 1.0) > 0.01)
				valid = false;
		}
		return valid;
	}

	// ── 私有辅助 ──────────────────────────────────────────────────────

	private void ResetVoyageState()
	{
		_context = null;
		_elapsedTime = 0;
		_lastCheckTime = 0;
		_accumulatedDamage = 0;
		_flatTimePenalties = 0;
		_tempTimePenalties = 0;
		_revealedHiddenTags.Clear();
		_abortReason = "";
		_currentHullBand = HullBand.Intact;
	}

	private void StartVoyage()
	{
		_lastCheckTime = 0;
		VoyageStarted?.Invoke(_context!);
	}

	private void FinalizeVoyage()
	{
		switch (_voyageState)
		{
			case VoyageState.Arrived:
				VoyageArrived?.Invoke(_context!, _revealedHiddenTags);
				break;
			case VoyageState.Retreated:
				VoyageRetreated?.Invoke(_context!);
				break;
			case VoyageState.ForcedLanding:
				VoyageForcedLanding?.Invoke(_context!,
					CalculateEffectiveHullIntegrity(_context!.HullIntegrityAtDeparture, _accumulatedDamage));
				break;
		}
	}

	private bool ShouldTriggerNextCheck(double totalDuration)
	{
		if (_elapsedTime >= totalDuration)
			return false;
		double nextCheckTime = _lastCheckTime + CalculateCheckInterval(_currentHullBand);
		return _elapsedTime >= nextCheckTime;
	}

	private void ResolveEncounterCheck()
	{
		if (_context == null) return;

		_lastCheckTime += CalculateCheckInterval(_currentHullBand);

		// 收集本次检查命中的遭遇条目
		var entries = new List<EncounterEntry>();
		bool stormEye = false;

		foreach (var tag in _context.VisibleHazardTags.Concat(_revealedHiddenTags))
		{
			if (_resolveEncounterFn == null) continue;
			var tagEntries = _resolveEncounterFn(tag);
			entries.AddRange(tagEntries);
			if (tagEntries.Any(e => e.IsStormEyePassage))
				stormEye = true;
		}

		// 计算伤害（max rule）
		int damage = CalculateCheckDamage(entries);

		// 积累时间惩罚
		foreach (var e in entries)
		{
			_flatTimePenalties += e.TimePenaltyFlat;
			_tempTimePenalties += e.TimePenaltyTemp;
		}

		// 应用伤害 + 波段转换检测
		if (damage > 0)
			ApplyDamageAndCheckBandTransition(damage);

		// 隐藏标签揭示
		ProcessHiddenTagReveal(stormEye);

		EncounterChecked?.Invoke(entries, damage);
	}

	private static List<string> ResolveHazardTags(
		IReadOnlyList<string> signalTags, IReadOnlyList<string> registryTags)
	{
		var registrySet = new HashSet<string>(registryTags, StringComparer.Ordinal);
		var signalSet = new HashSet<string>(signalTags, StringComparer.Ordinal);

		// 以 Registry 为准：取两者交集，Registry 独有标签也加入
		var result = new List<string>(registryTags); // Registry 所有标签作为基础
		// 不需要额外操作：已包含 Registry 的全部标签
		// signal 多出的标签（不在 Registry）已被排除
		return result;
	}
}
