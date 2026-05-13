using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 航图顶层状态枚举（ADR-0008 五态层级状态机）。
/// </summary>
public enum ChartState
{
	/// <summary>正在加载内容域数据，等待门控检查完成。</summary>
	Loading = 0,
	/// <summary>航图已打开，玩家正在浏览可选航线。</summary>
	Browsing = 1,
	/// <summary>玩家已选定一条航线，待确认出航。</summary>
	RouteSelected = 2,
	/// <summary>出航已确认，终端状态——不可逆。</summary>
	DepartureConfirmed = 3,
	/// <summary>内容域加载失败，显示错误提示和重试按钮。</summary>
	Error = 4,
}

/// <summary>
/// 航线子状态枚举（ADR-0008 四态子状态机）。
/// </summary>
public enum RouteSubState
{
	/// <summary>可被玩家选择。</summary>
	Browsable = 0,
	/// <summary>当前被玩家选中。</summary>
	Selected = 1,
	/// <summary>条件不满足，不可选（但可见）。</summary>
	Unavailable = 2,
	/// <summary>出航确认后的终端锁定状态。</summary>
	Locked = 3,
}

/// <summary>
/// chart_state_transition() 纯函数的返回值。
/// </summary>
public sealed class TransitionResult
{
	/// <summary>目标状态。</summary>
	public ChartState NewState { get; }
	/// <summary>转换是否被允许。</summary>
	public bool Allowed { get; }

	/// <param name="newState">目标状态。</param>
	/// <param name="allowed">是否允许转换。</param>
	public TransitionResult(ChartState newState, bool allowed)
	{
		NewState = newState;
		Allowed = allowed;
	}
}

/// <summary>
/// 内容域状态枚举（用于四大域门控检查）。
/// </summary>
public enum DomainState
{
	/// <summary>域数据加载完成，可以使用。</summary>
	Complete,
	/// <summary>域数据加载失败或未就绪。</summary>
	Failed,
	/// <summary>域数据正在加载中。</summary>
	Loading,
}

/// <summary>
/// 航线静态定义（由外部注册，供 ChartManager 查询使用）。
/// </summary>
public sealed class RouteStaticData
{
	/// <summary>航线 ID。</summary>
	public string RouteId { get; }
	/// <summary>起点地点 ID。</summary>
	public string OriginId { get; }
	/// <summary>终点地点 ID。</summary>
	public string DestinationId { get; }
	/// <summary>距离带（"short" / "medium" / "long"）。</summary>
	public string DistanceBand { get; }
	/// <summary>静态风险标签。</summary>
	public IReadOnlyList<string> HazardTags { get; }

	/// <param name="routeId">航线 ID。</param>
	/// <param name="originId">起点 ID。</param>
	/// <param name="destinationId">终点 ID。</param>
	/// <param name="distanceBand">距离带。</param>
	/// <param name="hazardTags">风险标签。</param>
	public RouteStaticData(string routeId, string originId, string destinationId,
		string distanceBand, IReadOnlyList<string> hazardTags)
	{
		RouteId = routeId;
		OriginId = originId;
		DestinationId = destinationId;
		DistanceBand = distanceBand;
		HazardTags = hazardTags.ToArray();
	}
}

/// <summary>
/// Chart / Route Planning Autoload #9（ADR-0008）。
/// 管理航图状态机、航线可见性与可选择性公式、两步出航确认流程。
/// 唯一真相源——下游系统只读查询，不得自行缓存航线状态。
/// </summary>
public sealed class ChartManager
{
	// ── 常量 ────────────────────────────────────────────────────────
	/// <summary>重试冷却默认时长（秒）。</summary>
	public const double DefaultRetryCooldown = 2.0;

	/// <summary>航图知识状态 Unknown（对应 LocationKnowledgeState.Unknown = 0）。</summary>
	private const int KnowledgeUnknown = 0;
	/// <summary>航图知识状态 Rumored（对应 LocationKnowledgeState.Rumored = 1）。</summary>
	private const int KnowledgeRumored = 1;

	// ── 内部状态 ─────────────────────────────────────────────────────
	private ChartState _chartState = ChartState.Loading;
	private string _selectedRouteId = "";
	private bool _hideRumored;
	private double _retryCooldownRemaining;
	private int _internalWarningCounter;

	// 各航线子状态: routeId → RouteSubState
	private readonly Dictionary<string, RouteSubState> _routeStates =
		new(StringComparer.Ordinal);

	// 当前可见航线（知识状态 ≥ Rumored 且通过筛选）
	private readonly List<string> _visibleRoutes = new();

	// 失败域状态记录: domainId → DomainState
	private readonly Dictionary<string, DomainState> _failedDomainStates =
		new(StringComparer.Ordinal);

	// 航线静态定义: routeId → RouteStaticData
	private readonly Dictionary<string, RouteStaticData> _routeStaticData =
		new(StringComparer.Ordinal);

	// 内容域状态: domainId → DomainState（由外部注入，模拟 Registry.get_domain_state）
	private readonly Dictionary<string, DomainState> _domainStates =
		new(StringComparer.Ordinal);

	// 知识状态查询委托（由外部注入，替代直接调用 IntelManager）
	private Func<string, int>? _queryKnowledgeState;
	private Func<string, bool>? _queryTraversable;

	// 当前停靠地点查询委托（替代直接调用 AirshipHub）
	private Func<string>? _getCurrentDockedLocation;

	// ── 事件 ─────────────────────────────────────────────────────────
	/// <summary>
	/// 航图顶层状态变更时发出，参数为 (oldState, newState)。
	/// </summary>
	public event Action<ChartState, ChartState>? ChartStateChanged;

	/// <summary>
	/// 航线被选中时发出，参数为 (routeId, destinationId)。
	/// </summary>
	public event Action<string, string>? RouteSelected;

	/// <summary>
	/// 出航确认（route_committed）信号，参数为 (routeId, destinationId, hazardTags)。
	/// </summary>
	public event Action<string, string, IReadOnlyList<string>>? RouteCommitted;

	/// <summary>
	/// 航线子状态变更时发出，参数为 (routeId, oldSubState, newSubState)。
	/// </summary>
	public event Action<string, RouteSubState, RouteSubState>? RouteSubStateChanged;

	// ── 事件（Story 003 — Departure Confirmation）────────────────────
	/// <summary>
	/// 出航因 traversable 变更或快照校验失败而中止，参数为 (routeId, reason)。
	/// reason: "route_not_traversable" | "snapshot_invalid"。
	/// </summary>
	public event Action<string, string>? RouteSelectionFailed;

	// ── 事件（Story 004 — Display Ordering）─────────────────────────
	/// <summary>
	/// hide_rumored 筛选器状态切换时发出，参数为新的 hide_rumored 值。
	/// </summary>
	public event Action<bool>? FilterChanged;

	// ── 事件（Story 006 — UIManager Signal Contract）──────────────────
	/// <summary>
	/// 航线因世界修复等事件获得增强时发出，参数为 (routeId, enhancementId)。
	/// </summary>
	public event Action<string, string>? RouteEnhanced;

	// ── 属性 ─────────────────────────────────────────────────────────
	/// <summary>当前航图顶层状态。</summary>
	public ChartState CurrentState => _chartState;

	/// <summary>当前选中的航线 ID（未选中时为空字符串）。</summary>
	public string SelectedRouteId => _selectedRouteId;

	/// <summary>是否隐藏仅传闻状态的航线。</summary>
	public bool HideRumored => _hideRumored;

	/// <summary>重试冷却剩余时间（秒）。</summary>
	public double RetryCooldownRemaining => _retryCooldownRemaining;

	/// <summary>内容域加载部分失败计数。</summary>
	public int InternalWarningCounter => _internalWarningCounter;

	/// <summary>失败域状态快照（Error 状态时填充）。</summary>
	public IReadOnlyDictionary<string, DomainState> FailedDomainStates => _failedDomainStates;

	/// <summary>当前可见航线列表（只读）。</summary>
	public IReadOnlyList<string> VisibleRoutes => _visibleRoutes;

	// ── 初始化 / 注册 ─────────────────────────────────────────────────

	/// <summary>
	/// 注册航线静态定义（应在 OpenChart() 前完成）。
	/// </summary>
	/// <param name="data">航线静态数据。</param>
	public void RegisterRoute(RouteStaticData data)
	{
		_routeStaticData[data.RouteId] = data;
	}

	/// <summary>
	/// 设置内容域状态（模拟 Registry.get_domain_state，由外部或测试注入）。
	/// </summary>
	/// <param name="domainId">域 ID（"routes"/"world"/"intel"/"threats"）。</param>
	/// <param name="state">域状态。</param>
	public void SetDomainState(string domainId, DomainState state)
	{
		_domainStates[domainId] = state;
	}

	/// <summary>
	/// 注入知识状态查询委托（替代直接调用 IntelManager.QueryRouteKnowledge）。
	/// 返回值：0=Unknown, 1=Rumored, 2=Identified, 3=Verified, -1=查询失败。
	/// </summary>
	/// <param name="queryFn">委托函数：输入 routeId，返回知识状态 int。</param>
	public void SetKnowledgeQueryDelegate(Func<string, int> queryFn)
	{
		_queryKnowledgeState = queryFn;
	}

	/// <summary>
	/// 注入可通行性查询委托（替代直接调用 IntelManager.QueryRouteAccessibility）。
	/// </summary>
	/// <param name="queryFn">委托函数：输入 routeId，返回是否可通行。</param>
	public void SetTraversableQueryDelegate(Func<string, bool> queryFn)
	{
		_queryTraversable = queryFn;
	}

	/// <summary>
	/// 注入当前停靠地点查询委托（替代直接调用 AirshipHub.GetCurrentDockedLocation）。
	/// AirshipHub 未就绪时返回空字符串。
	/// </summary>
	/// <param name="queryFn">委托函数：返回当前停靠地点 ID。</param>
	public void SetDockedLocationDelegate(Func<string> queryFn)
	{
		_getCurrentDockedLocation = queryFn;
	}

	/// <summary>
	/// 设置是否隐藏仅传闻状态的航线。
	/// </summary>
	/// <param name="hide">true = 隐藏传闻航线。</param>
	public void SetHideRumored(bool hide)
	{
		_hideRumored = hide;
	}

	// ── Story 001 — 顶层状态机 ────────────────────────────────────────

	/// <summary>
	/// 打开航图入口。执行内容域门控检查，成功则加载航线进入 Browsing，失败进入 Error。
	/// </summary>
	public void OpenChart()
	{
		_chartState = ChartState.Loading;
		_internalWarningCounter = 0;
		_visibleRoutes.Clear();
		_routeStates.Clear();
		_selectedRouteId = "";

		if (!CheckContentDomains())
		{
			ApplyTransition(ChartStateTransition("FAIL"));
			return;
		}

		// 批量查询航线知识状态，构建可见列表
		int failedCount = 0;
		foreach (var routeId in _routeStaticData.Keys)
		{
			int knowledge = SafeQueryKnowledgeState(routeId);
			if (knowledge < 0)
			{
				failedCount++;
				continue;
			}
			if (knowledge == KnowledgeUnknown)
				continue;

			_visibleRoutes.Add(routeId);
			_routeStates[routeId] = RouteSubState.Browsable;
		}

		_internalWarningCounter = failedCount;
		ApplyTransition(ChartStateTransition("COMPLETE"));
	}

	/// <summary>
	/// chart_state_transition() 纯函数——不修改状态，只返回转换结果。
	/// </summary>
	/// <param name="trigger">触发器字符串（"COMPLETE"/"FAIL"/"SELECT"/"DESELECT"/"CONFIRM"/"RETRY"）。</param>
	/// <returns>转换结果，含目标状态和是否允许。</returns>
	public TransitionResult ChartStateTransition(string trigger)
	{
		// 终端状态守卫——DEPARTURE_CONFIRMED 不可逆
		if (_chartState == ChartState.DepartureConfirmed)
			return new TransitionResult(ChartState.DepartureConfirmed, false);

		switch (_chartState)
		{
			case ChartState.Loading:
				if (trigger == "COMPLETE")
					return new TransitionResult(ChartState.Browsing, true);
				if (trigger == "FAIL")
					return new TransitionResult(ChartState.Error, true);
				break;

			case ChartState.Browsing:
				if (trigger == "SELECT")
					return new TransitionResult(ChartState.RouteSelected, true);
				break;

			case ChartState.RouteSelected:
				if (trigger == "DESELECT")
					return new TransitionResult(ChartState.Browsing, true);
				if (trigger == "CONFIRM")
					return new TransitionResult(ChartState.DepartureConfirmed, true);
				break;

			case ChartState.Error:
				if (trigger == "RETRY" && _retryCooldownRemaining <= 0.0)
					return new TransitionResult(ChartState.Loading, true);
				break;
		}

		// 默认拒绝——未列出的 (state, trigger) 组合
		return new TransitionResult(_chartState, false);
	}

	/// <summary>
	/// 玩家选择一条航线（必须处于 Browsing 状态且航线可选择）。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <returns>是否成功选中。</returns>
	public bool SelectRoute(string routeId)
	{
		if (RouteSelectability(routeId) != "browsable")
			return false;

		var result = ChartStateTransition("SELECT");
		if (!result.Allowed)
			return false;

		_selectedRouteId = routeId;
		SetRouteSubState(routeId, RouteSubState.Selected);
		ApplyTransition(result);
		_routeStaticData.TryGetValue(routeId, out var data);
		RouteSelected?.Invoke(routeId, data?.DestinationId ?? "");
		return true;
	}

	/// <summary>
	/// 玩家取消选择（Esc 或点击空白区域），回到 Browsing。
	/// </summary>
	/// <returns>是否成功取消。</returns>
	public bool DeselectRoute()
	{
		var result = ChartStateTransition("DESELECT");
		if (!result.Allowed)
			return false;

		if (!string.IsNullOrEmpty(_selectedRouteId))
			SetRouteSubState(_selectedRouteId, RouteSubState.Browsable);
		_selectedRouteId = "";
		ApplyTransition(result);
		return true;
	}

	/// <summary>
	/// 第一步出航确认请求——刷新可通行性，返回最新摘要数据供 UI 显示浮层。
	/// 不修改状态，不发出 route_committed 信号。
	/// </summary>
	/// <param name="routeId">要确认的航线 ID。</param>
	/// <returns>最新摘要 Dictionary（含 traversable / hazard_tags / distance_band），或 null 表示请求无效。</returns>
	public Dictionary<string, object>? RequestConfirmDeparture(string routeId)
	{
		if (_chartState != ChartState.RouteSelected)
			return null;
		if (_selectedRouteId != routeId)
			return null;

		// 刷新最新可通行性数据
		bool traversable = SafeQueryTraversable(routeId);
		_routeStaticData.TryGetValue(routeId, out var data);

		return new Dictionary<string, object>(StringComparer.Ordinal)
		{
			["route_id"] = routeId,
			["traversable"] = traversable,
			["hazard_tags"] = (object)(data?.HazardTags.ToList() ?? new List<string>()),
			["distance_band"] = data?.DistanceBand ?? "medium",
			["destination_id"] = data?.DestinationId ?? "",
		};
	}

	/// <summary>
	/// 第二步出航确认——先刷新可通行性检查，通过后发出 route_committed 信号并进入 DepartureConfirmed 终端状态。
	/// traversable=false 时强制取消选择并发出 RouteSelectionFailed。
	/// </summary>
	/// <returns>是否成功确认。</returns>
	public bool ConfirmDeparture()
	{
		if (string.IsNullOrEmpty(_selectedRouteId))
			return false;

		var result = ChartStateTransition("CONFIRM");
		if (!result.Allowed)
			return false;

		var routeId = _selectedRouteId;

		// Step 1 刷新：重新查询可通行性（获取最新风险状态）
		bool traversable = SafeQueryTraversable(routeId);
		if (!traversable)
		{
			// ADR-0002：fail 信号在状态回退之前发射
			RouteSelectionFailed?.Invoke(routeId, "route_not_traversable");
			ForceDeselect(routeId);
			return false;
		}

		_routeStaticData.TryGetValue(routeId, out var data);
		ApplyTransition(result); // 进入 DepartureConfirmed，所有子状态 → LOCKED
		// 使用刷新后的最新风险标签
		RouteCommitted?.Invoke(routeId, data?.DestinationId ?? "", data?.HazardTags ?? Array.Empty<string>());
		return true;
	}

	/// <summary>
	/// 强制取消选择当前航线，回到 Browsing 状态（由刷新失败或筛选器变更触发）。
	/// </summary>
	/// <param name="routeId">被强制取消的航线 ID（可为空）。</param>
	public void ForceDeselect(string? routeId = null)
	{
		if (_chartState != ChartState.RouteSelected)
			return;

		var deselectedId = routeId ?? _selectedRouteId;
		_selectedRouteId = "";
		var oldState = _chartState;
		_chartState = ChartState.Browsing;

		if (!string.IsNullOrEmpty(deselectedId))
			SetRouteSubState(deselectedId, RouteSubState.Browsable);

		if (oldState != _chartState)
			ChartStateChanged?.Invoke(oldState, _chartState);
	}

	/// <summary>
	/// 重试打开航图（仅在 Error 状态且冷却结束后有效）。
	/// </summary>
	/// <returns>是否成功触发重试。</returns>
	public bool RetryOpenChart()
	{
		var result = ChartStateTransition("RETRY");
		if (!result.Allowed)
			return false;

		ApplyTransition(result);
		OpenChart();
		return true;
	}

	/// <summary>
	/// 推进重试冷却计时器（每帧调用，单位：秒）。
	/// </summary>
	/// <param name="delta">帧时间间隔（秒）。</param>
	public void TickCooldown(double delta)
	{
		if (_retryCooldownRemaining > 0.0)
			_retryCooldownRemaining = Math.Max(0.0, _retryCooldownRemaining - delta);
	}

	/// <summary>
	/// 查询指定航线的子状态。未注册航线返回 Unavailable。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	public RouteSubState GetRouteSubState(string routeId) =>
		_routeStates.TryGetValue(routeId, out var s) ? s : RouteSubState.Unavailable;

	// ── Story 002 — 可见性与可选择性公式 ─────────────────────────────

	/// <summary>
	/// Formula 1：航线可见性纯函数。
	/// Unknown 航线永不渲染；hide_rumored=true 时隐藏传闻航线；查询失败视为 Unknown。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <param name="hideRumored">是否隐藏传闻航线。</param>
	/// <returns>true = 可见。</returns>
	public bool RouteVisibility(string routeId, bool hideRumored)
	{
		int knowledge = SafeQueryKnowledgeState(routeId);

		if (knowledge < 0 || knowledge == KnowledgeUnknown)
			return false;

		if (hideRumored && knowledge == KnowledgeRumored)
			return false;

		return true;
	}

	/// <summary>
	/// Formula 2：航线可选择性纯函数（短路求值，共 6 个分支）。
	/// 返回值："hidden" / "locked" / "unavailable" / "selected" / "browsable"。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <returns>可选择性标签字符串。</returns>
	public string RouteSelectability(string routeId)
	{
		// 分支 1: 不可见 → hidden（短路）
		if (!RouteVisibility(routeId, _hideRumored))
			return "hidden";

		// 分支 2: 出航已确认 → locked（终端状态）
		if (_chartState == ChartState.DepartureConfirmed)
			return "locked";

		// 分支 3: 不可通行（缺少能力）→ unavailable
		bool traversable = SafeQueryTraversable(routeId);
		if (!traversable)
			return "unavailable";

		// 分支 4: 起点不匹配当前停靠地 → unavailable
		string dockedLocation = GetCurrentDockedLocationSafe();
		if (_routeStaticData.TryGetValue(routeId, out var data)
			&& data.OriginId != dockedLocation)
			return "unavailable";

		// 分支 5: 当前已选中 → selected
		if (routeId == _selectedRouteId && _chartState == ChartState.RouteSelected)
			return "selected";

		// 分支 6 / 7: 默认可选
		return "browsable";
	}

	/// <summary>
	/// 重新评估所有可见航线的子状态（外部状态变化后调用）。
	/// </summary>
	public void ReevaluateAllRoutes()
	{
		foreach (var routeId in _visibleRoutes)
		{
			var selectability = RouteSelectability(routeId);
			var newSubState = SelectabilityToSubState(selectability);
			SetRouteSubState(routeId, newSubState);
		}
	}

	// ── Story 004 — Display Ordering & Filtering ──────────────────────

	/// <summary>
	/// Formula 5：航线显示优先级纯函数。
	/// rank = knowledge_rank×100 + distance_rank，值越小排越前。
	/// 未知知识状态返回 999（防御性默认值）。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <returns>显示优先级整数 [101, 303] 或 999。</returns>
	public int RouteDisplayOrder(string routeId)
	{
		int knowledge = SafeQueryKnowledgeState(routeId);

		int rankByKnowledge = knowledge switch
		{
			3 => 1, // Verified
			2 => 2, // Identified
			1 => 3, // Rumored
			_ => 0, // Unknown 或查询失败 → 999
		};

		if (rankByKnowledge == 0)
			return 999;

		_routeStaticData.TryGetValue(routeId, out var data);
		int rankByDistance = (data?.DistanceBand ?? "") switch
		{
			"short" => 1,
			"medium" => 2,
			"long" => 3,
			_ => 2, // 未知距离带视为 medium
		};

		return rankByKnowledge * 100 + rankByDistance;
	}

	/// <summary>
	/// 返回当前可见航线列表，按 display_order 排序，相同 order 按 routeId 字典序打破平局。
	/// 已应用 hide_rumored 筛选器。
	/// </summary>
	/// <returns>排序后的可见航线 ID 只读列表。</returns>
	public IReadOnlyList<string> GetVisibleRoutes()
	{
		var visible = _visibleRoutes
			.Where(routeId => RouteVisibility(routeId, _hideRumored))
			.OrderBy(routeId => RouteDisplayOrder(routeId))
			.ThenBy(routeId => routeId, StringComparer.Ordinal)
			.ToList();
		return visible;
	}

	/// <summary>
	/// 切换 hide_rumored 筛选器。若当前已选航线因筛选被隐藏，强制取消选择。
	/// 发出 FilterChanged 信号。
	/// </summary>
	/// <param name="hide">true = 隐藏传闻航线。</param>
	public void ToggleHideRumored(bool hide)
	{
		if (_hideRumored == hide)
			return;

		_hideRumored = hide;

		// 若已选航线被筛选隐藏，强制取消选择
		if (!string.IsNullOrEmpty(_selectedRouteId)
			&& !RouteVisibility(_selectedRouteId, _hideRumored))
		{
			ForceDeselect(_selectedRouteId);
		}

		FilterChanged?.Invoke(_hideRumored);
	}

	/// <summary>
	/// 返回指定航线用于 UI 展示的全部数据（不含视觉属性）。
	/// </summary>
	/// <param name="routeId">航线 ID。</param>
	/// <returns>展示数据字典。</returns>
	public Dictionary<string, object?> GetRouteDisplayData(string routeId)
	{
		int knowledge = SafeQueryKnowledgeState(routeId);
		bool traversable = SafeQueryTraversable(routeId);
		_routeStaticData.TryGetValue(routeId, out var data);

		return new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["route_id"] = routeId,
			["display_order"] = RouteDisplayOrder(routeId),
			["knowledge_state"] = knowledge,
			["selectability"] = RouteSelectability(routeId),
			["traversable"] = traversable,
			["hazard_tags"] = data?.HazardTags,
			["distance_band"] = data?.DistanceBand ?? "medium",
			["origin_id"] = data?.OriginId ?? "",
			["destination_id"] = data?.DestinationId ?? "",
		};
	}

	// ── Story 005 — Snapshot Validation & Persistence ────────────────

	/// <summary>
	/// 构建出航快照 payload（仅含独立状态变量，不含派生值）。
	/// 应在 DEPARTURE_CONFIRMED 进入后调用。
	/// </summary>
	/// <param name="routeId">已提交的航线 ID。</param>
	/// <param name="timestamp">出航确认时间戳（Unix 时间）。</param>
	/// <returns>快照 payload 字典（纯基础类型，可序列化为 Canonical JSON）。</returns>
	public Dictionary<string, object?> BuildSnapshotPayload(string routeId, double timestamp)
	{
		return new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["last_committed_route_id"] = routeId,
			["departure_state"] = "DEPARTURE_CONFIRMED",
			["active_filter"] = _hideRumored ? "hide_rumored" : "show_all",
			["last_departure_timestamp"] = timestamp,
			["hide_rumored"] = _hideRumored,
		};
	}

	/// <summary>
	/// Formula 4：快照包有效性校验纯函数。
	/// 返回校验结果，violations 列表为空时 valid=true。
	/// </summary>
	/// <param name="payload">待校验的快照 payload 字典（null 视为格式错误）。</param>
	/// <param name="currentTime">当前 Unix 时间（用于时间戳校验）。</param>
	/// <param name="timestampTolerance">允许的未来时间戳容差（秒），默认 300s。</param>
	/// <param name="routeRegistry">当前注册表中所有航线 ID 集合（用于 stale ID 校验）。</param>
	/// <returns>校验结果：{Valid, Violations}。</returns>
	public static (bool Valid, IReadOnlyList<string> Violations) ValidateSnapshotPackage(
		IReadOnlyDictionary<string, object?>? payload,
		double currentTime,
		double timestampTolerance,
		IReadOnlySet<string> routeRegistry)
	{
		var violations = new List<string>();

		if (payload == null)
		{
			violations.Add("malformed snapshot package");
			return (false, violations);
		}

		// domain_id 校验
		if (!payload.TryGetValue("domain_id", out var domainIdRaw)
			|| domainIdRaw?.ToString() != "progress.routes")
		{
			if (payload.TryGetValue("domain_id", out _))
				violations.Add("wrong domain_id");
			// 若字段缺失，后续 required 字段检查会覆盖
		}

		// 必需字段检查
		string[] required = { "last_committed_route_id", "departure_state", "active_filter", "last_departure_timestamp" };
		foreach (var field in required)
		{
			if (!payload.ContainsKey(field))
				violations.Add($"missing field: {field}");
		}

		// departure_state 必须为 DEPARTURE_CONFIRMED
		if (payload.TryGetValue("departure_state", out var dsRaw)
			&& dsRaw?.ToString() != "DEPARTURE_CONFIRMED")
		{
			violations.Add("invalid departure_state");
		}

		// 时间戳校验
		if (payload.TryGetValue("last_departure_timestamp", out var tsRaw))
		{
			double ts = Convert.ToDouble(tsRaw);
			if (double.IsNaN(ts) || double.IsInfinity(ts))
				violations.Add("non-finite timestamp");
			else if (ts <= 0.0)
				violations.Add("timestamp is epoch or uninitialized");
			else if (ts > currentTime + timestampTolerance)
				violations.Add("timestamp in future");
		}

		// stale route_id 校验
		if (payload.TryGetValue("last_committed_route_id", out var routeIdRaw))
		{
			var routeId = routeIdRaw?.ToString() ?? "";
			if (string.IsNullOrEmpty(routeId) || !routeRegistry.Contains(routeId))
				violations.Add($"route_id not found in registry: {routeId}");
		}

		return (violations.Count == 0, violations);
	}

	/// <summary>
	/// 从快照 payload 恢复 ChartManager 状态。
	/// 若 departure_state=DEPARTURE_CONFIRMED，直接进入终端状态并重发 RouteCommitted 信号。
	/// </summary>
	/// <param name="payload">序列化的快照 payload 字典。</param>
	public void RestoreFromSnapshot(IReadOnlyDictionary<string, object?> payload)
	{
		var lastRouteId = payload.TryGetValue("last_committed_route_id", out var r)
			? r?.ToString() ?? "" : "";
		var departureState = payload.TryGetValue("departure_state", out var ds)
			? ds?.ToString() ?? "" : "";
		_hideRumored = payload.TryGetValue("hide_rumored", out var hr) && hr is bool b && b;

		if (departureState == "DEPARTURE_CONFIRMED")
		{
			// 直接进入终端状态（lock_remaining=0，立即移交 Navigation）
			_chartState = ChartState.DepartureConfirmed;
			_selectedRouteId = "";
			// 重发 route_committed 以触发 Navigation 上下文构建
			_routeStaticData.TryGetValue(lastRouteId, out var data);
			RouteCommitted?.Invoke(lastRouteId, data?.DestinationId ?? "", data?.HazardTags ?? Array.Empty<string>());
		}
		else
		{
			_chartState = ChartState.Loading;
		}
	}

	// ── Story 006 — UIManager Query Interface ─────────────────────────

	/// <summary>
	/// 返回航图当前顶层状态的字符串表示（供 UIManager 读取）。
	/// </summary>
	/// <returns>状态字符串："LOADING"/"BROWSING"/"ROUTE_SELECTED"/"DEPARTURE_CONFIRMED"/"ERROR"。</returns>
	public string GetChartStateString() => _chartState switch
	{
		ChartState.Loading => "LOADING",
		ChartState.Browsing => "BROWSING",
		ChartState.RouteSelected => "ROUTE_SELECTED",
		ChartState.DepartureConfirmed => "DEPARTURE_CONFIRMED",
		ChartState.Error => "ERROR",
		_ => "LOADING",
	};

	/// <summary>
	/// 返回当前选中的航线 ID。未选中时返回空字符串（非 null）。
	/// </summary>
	public string GetSelectedRoute() => _selectedRouteId;

	/// <summary>
	/// 返回筛选器状态字典，供 UIManager 读取。
	/// </summary>
	/// <returns>含 "hide_rumored" bool 字段的字典。</returns>
	public Dictionary<string, object> GetFilterState() =>
		new(StringComparer.Ordinal) { ["hide_rumored"] = _hideRumored };

	/// <summary>
	/// 发出 RouteEnhanced 信号（由世界修复等外部系统触发）。
	/// </summary>
	/// <param name="routeId">被增强的航线 ID。</param>
	/// <param name="enhancementId">增强事件 ID。</param>
	public void NotifyRouteEnhanced(string routeId, string enhancementId)
	{
		RouteEnhanced?.Invoke(routeId, enhancementId);
	}

	// ── 私有辅助 ─────────────────────────────────────────────────────

	private bool CheckContentDomains()
	{
		string[] requiredDomains = { "routes", "world", "intel", "threats" };
		bool allComplete = true;
		_failedDomainStates.Clear();

		foreach (var domainId in requiredDomains)
		{
			var state = _domainStates.TryGetValue(domainId, out var ds) ? ds : DomainState.Failed;
			_failedDomainStates[domainId] = state;
			if (state != DomainState.Complete)
				allComplete = false;
		}

		return allComplete;
	}

	private void ApplyTransition(TransitionResult result)
	{
		if (!result.Allowed)
			return;

		var oldState = _chartState;
		_chartState = result.NewState;

		switch (_chartState)
		{
			case ChartState.DepartureConfirmed:
				// 所有航线子状态 → LOCKED（终端）
				foreach (var routeId in _visibleRoutes.ToList())
					SetRouteSubState(routeId, RouteSubState.Locked);
				break;
			case ChartState.Error:
				_retryCooldownRemaining = DefaultRetryCooldown;
				break;
		}

		if (oldState != _chartState)
			ChartStateChanged?.Invoke(oldState, _chartState);
	}

	private void SetRouteSubState(string routeId, RouteSubState newState)
	{
		var oldState = _routeStates.TryGetValue(routeId, out var s) ? s : RouteSubState.Browsable;
		if (oldState == newState)
			return;

		// UNAVAILABLE → SELECTED 禁止
		if (oldState == RouteSubState.Unavailable && newState == RouteSubState.Selected)
			return;

		// LOCKED → 任意 禁止（终端）
		if (oldState == RouteSubState.Locked)
			return;

		_routeStates[routeId] = newState;
		RouteSubStateChanged?.Invoke(routeId, oldState, newState);
	}

	private int SafeQueryKnowledgeState(string routeId)
	{
		if (_queryKnowledgeState == null)
			return KnowledgeUnknown;
		try { return _queryKnowledgeState(routeId); }
		catch { return -1; }
	}

	private bool SafeQueryTraversable(string routeId)
	{
		if (_queryTraversable == null)
			return true; // 无委托时默认可通行（测试友好）
		try { return _queryTraversable(routeId); }
		catch { return false; }
	}

	private string GetCurrentDockedLocationSafe()
	{
		if (_getCurrentDockedLocation == null)
			return "";
		try { return _getCurrentDockedLocation(); }
		catch { return ""; }
	}

	private static RouteSubState SelectabilityToSubState(string selectability) => selectability switch
	{
		"selected" => RouteSubState.Selected,
		"unavailable" => RouteSubState.Unavailable,
		"locked" => RouteSubState.Locked,
		_ => RouteSubState.Browsable,
	};
}
