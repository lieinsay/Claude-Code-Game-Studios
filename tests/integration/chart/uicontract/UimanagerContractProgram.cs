using CloudWeaverVoyage.Core;

// Story 006 — UIManager Query Interface & Signal Contract (Integration)
// 覆盖 AC-1 到 AC-16 全部验收标准

static ChartManager BuildBrowsing(bool hideRumored = false, string dockedAt = "location.glass-harbor")
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.RegisterRoute(new RouteStaticData(
		"route.storm-cut-01", "location.glass-harbor", "location.danger-zone",
		"medium", new[] { "storm" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.sky-reef-arc-01" ? 2 : 1); // sky-reef=identified, storm=rumored
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => dockedAt);
	mgr.SetHideRumored(hideRumored);
	mgr.OpenChart();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 006: UIManager Query Interface & Signal Contract ===\n");

// ── AC-1: GetChartStateString() — BROWSING ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.GetChartStateString() == "BROWSING", "AC-1: GetChartStateString() → 'BROWSING'");
}

// ── AC-1 其他状态 ──
{
	var mgr = new ChartManager(); // Loading
	Assert(mgr.GetChartStateString() == "LOADING", "AC-1b: 初始 → 'LOADING'");
	mgr.SetDomainState("routes", DomainState.Failed);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.OpenChart();
	Assert(mgr.GetChartStateString() == "ERROR", "AC-1c: 域失败 → 'ERROR'");
}

// ── AC-2: GetVisibleRoutes() — 排序后 [sky-reef, storm-cut] ──
{
	var mgr = BuildBrowsing();
	var visible = mgr.GetVisibleRoutes();
	Assert(visible.Count == 2, "AC-2: 2 条可见航线");
	Assert(visible[0] == "route.sky-reef-arc-01",
		"AC-2: sky-reef(201) 排在 storm-cut(302) 之前");
}

// ── AC-3: GetSelectedRoute() — 选中/未选中 ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.GetSelectedRoute() == "", "AC-3: 未选中时返回空字符串");
	mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(mgr.GetSelectedRoute() == "route.sky-reef-arc-01", "AC-3: 选中后返回 routeId");
}

// ── AC-4: GetFilterState() — hide_rumored ──
{
	var mgr = BuildBrowsing(hideRumored: true);
	var state = mgr.GetFilterState();
	Assert(state.TryGetValue("hide_rumored", out var hr) && hr is true,
		"AC-4: hide_rumored=true → GetFilterState()['hide_rumored']=true");
}

// ── AC-5: 查询接口不返回视觉属性 ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	var display = mgr.GetRouteDisplayData("route.sky-reef-arc-01");
	// 验证返回值均为数据类型（int/string/bool/list），不含颜色/坐标
	Assert(display["knowledge_state"] is int, "AC-5: knowledge_state 为 int（枚举值），非颜色");
	Assert(display["selectability"] is string, "AC-5: selectability 为 string（枚举名），非视觉属性");
	Assert(display["hazard_tags"] is IReadOnlyList<string>, "AC-5: hazard_tags 为字符串数组，非图标引用");
	// 确认不含颜色、透明度等视觉字段
	Assert(!display.ContainsKey("color"), "AC-5: 不含 color 字段");
	Assert(!display.ContainsKey("opacity"), "AC-5: 不含 opacity 字段");
	Assert(!display.ContainsKey("line_width"), "AC-5: 不含 line_width 字段");
}

// ── AC-6: GetRouteDisplayData — 含所需数据字段，无视觉属性 ──
{
	var mgr = BuildBrowsing();
	var data = mgr.GetRouteDisplayData("route.sky-reef-arc-01");
	Assert(data.ContainsKey("route_id"), "AC-6: 含 route_id");
	Assert(data.ContainsKey("display_order"), "AC-6: 含 display_order");
	Assert(data.ContainsKey("knowledge_state"), "AC-6: 含 knowledge_state");
	Assert(data.ContainsKey("selectability"), "AC-6: 含 selectability");
	Assert(data.ContainsKey("traversable"), "AC-6: 含 traversable");
	Assert(data.ContainsKey("hazard_tags"), "AC-6: 含 hazard_tags");
	Assert(data.ContainsKey("distance_band"), "AC-6: 含 distance_band");
	Assert(data.ContainsKey("origin_id"), "AC-6: 含 origin_id");
	Assert(data.ContainsKey("destination_id"), "AC-6: 含 destination_id");
}

// ── AC-7: route_committed 信号——3 个 typed 参数 ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	string? sigRoute = null; string? sigDest = null; IReadOnlyList<string>? sigHazards = null;
	mgr.RouteCommitted += (r, d, h) => { sigRoute = r; sigDest = d; sigHazards = h; };
	mgr.ConfirmDeparture();
	Assert(sigRoute is string, "AC-7: route_committed param[0] 为 string（typed）");
	Assert(sigDest is string, "AC-7: route_committed param[1] 为 string（typed）");
	Assert(sigHazards is IReadOnlyList<string>, "AC-7: route_committed param[2] 为 IReadOnlyList<string>（typed）");
	Assert(sigRoute == "route.sky-reef-arc-01", "AC-7: route_id 正确");
	Assert(sigDest == "location.sky-reef-outpost", "AC-7: destination_id 正确");
}

// ── AC-8: route_selection_failed 信号——2 个 typed 参数 ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.SetTraversableQueryDelegate(_ => false); // 触发失败
	string? sigRoute = null; string? sigReason = null;
	mgr.RouteSelectionFailed += (r, reason) => { sigRoute = r; sigReason = reason; };
	mgr.ConfirmDeparture();
	Assert(sigRoute is string, "AC-8: route_selection_failed param[0] 为 string");
	Assert(sigReason is string, "AC-8: route_selection_failed param[1] 为 string");
	Assert(sigReason == "route_not_traversable", "AC-8: reason 正确");
}

// ── AC-9: route_enhanced 信号——2 个 typed 参数 ──
{
	var mgr = BuildBrowsing();
	string? sigRoute = null; string? sigEnhancement = null;
	mgr.RouteEnhanced += (r, e) => { sigRoute = r; sigEnhancement = e; };
	mgr.NotifyRouteEnhanced("route.sky-reef-arc-01", "world-repair-01");
	Assert(sigRoute is string, "AC-9: route_enhanced param[0] 为 string");
	Assert(sigEnhancement is string, "AC-9: route_enhanced param[1] 为 string");
	Assert(sigRoute == "route.sky-reef-arc-01", "AC-9: routeId 正确");
	Assert(sigEnhancement == "world-repair-01", "AC-9: enhancementId 正确");
}

// ── AC-10: chart_state_changed 信号——2 个 typed 参数 ──
{
	var mgr = new ChartManager();
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	ChartState? sigOld = null; ChartState? sigNew = null;
	mgr.ChartStateChanged += (o, n) => { sigOld = o; sigNew = n; };
	mgr.OpenChart(); // Loading → Browsing
	Assert(sigOld is ChartState, "AC-10: chart_state_changed param[0] 为 ChartState（typed）");
	Assert(sigNew is ChartState, "AC-10: chart_state_changed param[1] 为 ChartState（typed）");
	Assert(sigOld == ChartState.Loading, "AC-10: old=Loading");
	Assert(sigNew == ChartState.Browsing, "AC-10: new=Browsing");
}

// ── AC-11: filter_changed 信号——1 个 typed 参数 ──
{
	var mgr = BuildBrowsing();
	bool? sigHide = null;
	mgr.FilterChanged += h => sigHide = h;
	mgr.ToggleHideRumored(true);
	Assert(sigHide is bool, "AC-11: filter_changed param 为 bool（typed）");
	Assert(sigHide == true, "AC-11: hide_rumored=true");
}

// ── AC-12: 信号发射顺序——chart_state_changed 先于 route_committed ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	var order = new List<string>();
	mgr.ChartStateChanged += (_, _) => order.Add("chart_state_changed");
	mgr.RouteCommitted += (_, _, _) => order.Add("route_committed");
	mgr.ConfirmDeparture();
	// 实现中 ApplyTransition 先发出 chart_state_changed，ConfirmDeparture 后发出 RouteCommitted
	Assert(order.Count == 2, "AC-12: 两个信号均发射");
	Assert(order[0] == "chart_state_changed", "AC-12: chart_state_changed 先发射");
	Assert(order[1] == "route_committed", "AC-12: route_committed 后发射");
}

// ── AC-13: 失败时信号顺序——route_selection_failed 先于 chart_state_changed ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.SetTraversableQueryDelegate(_ => false);
	var order = new List<string>();
	mgr.RouteSelectionFailed += (_, _) => order.Add("route_selection_failed");
	mgr.ChartStateChanged += (_, _) => order.Add("chart_state_changed");
	mgr.ConfirmDeparture();
	// ForceDeselect 发出 chart_state_changed，但 RouteSelectionFailed 先触发
	Assert(order.Count >= 2, "AC-13: 两个信号均发射");
	Assert(order[0] == "route_selection_failed", "AC-13: route_selection_failed 先发射");
	Assert(order[1] == "chart_state_changed", "AC-13: chart_state_changed 后发射（状态回退）");
}

// ── AC-14: UIManager 接收 chart_state_changed — 收到新旧状态 ──
{
	var mgr = BuildBrowsing();
	ChartState receivedOld = ChartState.Loading;
	ChartState receivedNew = ChartState.Loading;
	mgr.ChartStateChanged += (o, n) => { receivedOld = o; receivedNew = n; };
	mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(receivedOld == ChartState.Browsing, "AC-14: UIManager 收到 old=Browsing");
	Assert(receivedNew == ChartState.RouteSelected, "AC-14: UIManager 收到 new=RouteSelected");
}

// ── AC-15: GetRouteDisplayData — 验证完整字段集 ──
{
	var mgr = BuildBrowsing();
	var data = mgr.GetRouteDisplayData("route.sky-reef-arc-01");
	// 所有字段为纯数据
	Assert((string?)data["route_id"] == "route.sky-reef-arc-01", "AC-15: route_id 正确");
	Assert((int?)data["display_order"] == 201, "AC-15: display_order=201（identified+short）");
	Assert(data["knowledge_state"] is int, "AC-15: knowledge_state 为 int");
	Assert(data["selectability"] is string, "AC-15: selectability 为 string");
	Assert(data["traversable"] is bool, "AC-15: traversable 为 bool");
}

// ── AC-16: AirshipHub 未就绪 → _get_current_docked_location_safe() 返回 "" → 所有航线 unavailable ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.any", "location.glass-harbor", "location.dest", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.SetTraversableQueryDelegate(_ => true);
	// 不设置 DockedLocationDelegate → 默认返回 ""
	mgr.OpenChart();
	// origin="location.glass-harbor" ≠ "" → unavailable
	Assert(mgr.RouteSelectability("route.any") == "unavailable",
		"AC-16: AirshipHub 未就绪 → docked='', 航线 origin≠'' → unavailable，不崩溃");
}

// ── 验证 emit-after-mutation 模式 ──
{
	var mgr = BuildBrowsing();
	ChartState stateAtEmit = ChartState.Loading;
	mgr.ChartStateChanged += (_, n) => stateAtEmit = mgr.CurrentState;
	mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(stateAtEmit == ChartState.RouteSelected,
		"EMIT-AFTER: 信号发射时状态已变更为 RouteSelected");
}

Console.WriteLine();
Console.WriteLine($"Story 006 UIManager Contract: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
