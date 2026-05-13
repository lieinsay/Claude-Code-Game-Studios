using CloudWeaverVoyage.Core;

// Story 008 — Edge Cases, Error Recovery & Keyboard Navigation (Integration)
// 覆盖 AC-1 到 AC-21 全部验收标准

static ChartManager BuildManager(
	Func<string, int>? knowledgeFn = null,
	Func<string, bool>? traversableFn = null,
	string dockedAt = "location.glass-harbor",
	bool hideRumored = false,
	bool threatsOk = true)
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.RegisterRoute(new RouteStaticData(
		"route.storm-cut-01", "location.glass-harbor", "location.danger-zone",
		"medium", new[] { "storm" }));
	mgr.RegisterRoute(new RouteStaticData(
		"route.rumored-route", "location.glass-harbor", "location.rumored-dest",
		"long", new[] { "unknown" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", threatsOk ? DomainState.Complete : DomainState.Failed);
	mgr.SetKnowledgeQueryDelegate(knowledgeFn ?? (_ => 2));
	mgr.SetTraversableQueryDelegate(traversableFn ?? (_ => true));
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

Console.WriteLine("=== Story 008: Edge Cases, Error Recovery & Keyboard Navigation ===\n");

// ── AC-1: EC-1 部分域失败 → ERROR，保留失败域信息 ──
{
	var mgr = BuildManager(threatsOk: false);
	Assert(mgr.CurrentState == ChartState.Error, "AC-1: threats=FAILED → ERROR");
	Assert(mgr.FailedDomainStates.TryGetValue("threats", out var ts) && ts == DomainState.Failed,
		"AC-1: 失败域信息保留");
	// 修复后重试
	mgr.TickCooldown(2.1);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.SetTraversableQueryDelegate(_ => true);
	bool retried = mgr.RetryOpenChart();
	Assert(retried && mgr.CurrentState == ChartState.Browsing,
		"AC-1: 修复后 RETRY → BROWSING");
}

// ── AC-2: EC-2 部分查询失败 → 优雅降级，不进入 ERROR ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.ok", "location.glass-harbor", "location.dest-a", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.fail", "location.glass-harbor", "location.dest-b", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.fail" ? -1 : 2); // route.fail 查询失败
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-2: 部分查询失败 → 仍进入 BROWSING（优雅降级）");
	Assert(mgr.InternalWarningCounter == 1, "AC-2: _internal_warning_counter=1");
	Assert(mgr.VisibleRoutes.Count == 1, "AC-2: 失败航线不在可见列表");
}

// ── AC-3: EC-3 连点保护——DEPARTURE_CONFIRMED 期间所有输入拦截 ──
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.sky-reef-arc-01");
	int commitCount = 0;
	mgr.RouteCommitted += (_, _, _) => commitCount++;
	mgr.ConfirmDeparture();
	// 模拟连点 10 次
	for (int i = 0; i < 10; i++)
		mgr.ConfirmDeparture();
	Assert(commitCount == 1, "AC-3: 连点 10+ 次，route_committed 仅一次");
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed, "AC-3: 状态保持 DEPARTURE_CONFIRMED");
}

// ── AC-4: EC-4 同帧两次 CONFIRM → 第二次终端守卫拒绝 ──
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.sky-reef-arc-01");
	int commitCount = 0;
	mgr.RouteCommitted += (_, _, _) => commitCount++;
	bool r1 = mgr.ConfirmDeparture();
	bool r2 = mgr.ConfirmDeparture(); // 第二次
	Assert(r1 && !r2, "AC-4: 第一次成功，第二次被拒绝");
	Assert(commitCount == 1, "AC-4: route_committed 仅一次");
}

// ── AC-5: EC-5 knowledge 撤销 → 强制取消选择 ──
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.SetKnowledgeQueryDelegate(_ => 0); // unknown
	mgr.OnKnowledgeChanged("location.glass-harbor", 0);
	Assert(mgr.CurrentState == ChartState.Browsing,
		"AC-5: knowledge→unknown 强制取消选择 → BROWSING");
}

// ── AC-6: EC-6 停靠地变更 → re-evaluation ──
{
	var mgr = BuildManager(dockedAt: "location.glass-harbor");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-6: 前置 glass-harbor 出发可选");
	mgr.SetDockedLocationDelegate(() => "location.other-port");
	mgr.OnDockedLocationChanged("location.other-port");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Unavailable,
		"AC-6: dock 变更 → glass-harbor 起点航线 → UNAVAILABLE");
}

// ── AC-7: EC-7 能力解锁 → UNAVAILABLE→BROWSABLE ──
{
	var traversable = false;
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.blocked", "location.glass-harbor", "location.dest", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.SetTraversableQueryDelegate(_ => traversable);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	Assert(mgr.GetRouteSubState("route.blocked") == RouteSubState.Unavailable,
		"AC-7: 前置 traversable=false → UNAVAILABLE");
	traversable = true;
	mgr.OnAbilityChanged("ability.unlock", "path");
	Assert(mgr.GetRouteSubState("route.blocked") == RouteSubState.Browsable,
		"AC-7: 能力解锁后 → BROWSABLE");
}

// ── AC-8: EC-8 route 从注册表删除 → 缓存清理 ──
{
	var mgr = BuildManager();
	Assert(mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"), "AC-8: 前置 sky-reef 可见");
	// 模拟注册表删除 sky-reef（仅保留 storm-cut 和 rumored-route）
	mgr.PurgeStaleRoutes(new HashSet<string>(StringComparer.Ordinal)
		{ "route.storm-cut-01", "route.rumored-route" });
	Assert(!mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"),
		"AC-8: 从注册表删除的航线从缓存移除");
}

// ── AC-9: EC-12 所有航线 unknown → 空 BROWSING（非 ERROR）──
{
	var mgr = BuildManager(knowledgeFn: _ => 0); // all unknown
	Assert(mgr.CurrentState == ChartState.Browsing,
		"AC-9: 所有 unknown → BROWSING（非 ERROR）");
	Assert(mgr.GetVisibleRoutes().Count == 0,
		"AC-9: 可见列表为空");
	Assert(mgr.GetEmptyChartReason() == ChartManager.EmptyChartReason.NoKnownRoutes,
		"AC-9: empty_reason=NoKnownRoutes");
}

// ── AC-10: EC-13 当前港口无可出发航线 → UNAVAILABLE → context ──
{
	var mgr = BuildManager(dockedAt: "location.nowhere"); // 无航线以此为起点
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-10: BROWSING");
	Assert(mgr.GetEmptyChartReason() == ChartManager.EmptyChartReason.NoDepartableAtPort,
		"AC-10: empty_reason=NoDepartableAtPort");
}

// ── AC-11: EC-14 全传闻+hide_rumored=true → 空航图 ──
{
	var mgr = BuildManager(knowledgeFn: _ => 1, hideRumored: true); // all rumored + hidden
	Assert(mgr.GetVisibleRoutes().Count == 0, "AC-11: 全传闻+hide → 空列表");
	Assert(mgr.GetEmptyChartReason() == ChartManager.EmptyChartReason.AllRumoredHidden,
		"AC-11: empty_reason=AllRumoredHidden");
	// 切回 false 恢复
	mgr.ToggleHideRumored(false);
	Assert(mgr.GetVisibleRoutes().Count > 0, "AC-11: hide=false 后恢复可见");
}

// ── AC-12: EC-15 traversable=true 但 knowledge=unknown → hidden（第一分支短路）──
{
	var mgr = BuildManager(knowledgeFn: _ => 0, traversableFn: _ => true); // unknown+traversable
	// route_visibility → false（unknown） → hidden（短路，不执行 traversable 查询）
	Assert(mgr.RouteSelectability("route.sky-reef-arc-01") == "hidden",
		"AC-12: knowledge=unknown → hidden（route_visibility 短路，不调用 traversable）");
}

// ── AC-13: EC-16 第一步和第二步间风险变更 → 浮层使用最新数据 ──
{
	var mgr = BuildManager(traversableFn: _ => true);
	mgr.SelectRoute("route.sky-reef-arc-01");
	// 第一步：获取当前摘要（此时 traversable=true）
	var summary = mgr.RequestConfirmDeparture("route.sky-reef-arc-01");
	Assert(summary != null && (bool)summary["traversable"],
		"AC-13: 第一步摘要 traversable=true");
	// 风险变更：traversable→false
	mgr.SetTraversableQueryDelegate(_ => false);
	// 第二步确认：刷新后发现 traversable=false → 失败
	string? failReason = null;
	mgr.RouteSelectionFailed += (_, r) => failReason = r;
	bool confirmed = mgr.ConfirmDeparture();
	Assert(!confirmed, "AC-13: traversable 变更后第二步确认失败");
	Assert(failReason == "route_not_traversable", "AC-13: 使用最新数据检测到阻塞");
}

// ── AC-14: 键盘导航顺序与 display_order 一致 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.sky-reef-arc-01", "location.glass-harbor", "location.a", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.storm-cut-01", "location.glass-harbor", "location.b", "medium", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.sky-reef-arc-01" ? 2 : 1); // identified vs rumored
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	var navOrder = mgr.GetKeyboardNavOrder();
	Assert(navOrder.Count == 2, "AC-14: 2 条航线");
	Assert(navOrder[0] == "route.sky-reef-arc-01",
		"AC-14: Tab 顺序——sky-reef(201) 优先于 storm-cut(302)");
}

// ── AC-15: 键盘 Enter 选中航线 ──
{
	var mgr = BuildManager();
	bool result = mgr.SelectRoute("route.sky-reef-arc-01"); // 等同于 Enter
	Assert(result && mgr.CurrentState == ChartState.RouteSelected,
		"AC-15: Enter → SELECT → ROUTE_SELECTED");
}

// ── AC-16: 键盘 Esc 取消选择 ──
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.sky-reef-arc-01");
	bool result = mgr.DeselectRoute(); // 等同于 Esc
	Assert(result && mgr.CurrentState == ChartState.Browsing,
		"AC-16: Esc → DESELECT → BROWSING");
}

// ── AC-17: 键盘完整出航流程 ──
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.sky-reef-arc-01"); // Enter 选中
	var summary = mgr.RequestConfirmDeparture("route.sky-reef-arc-01"); // Enter 打开浮层
	Assert(summary != null, "AC-17: 第一步摘要有效");
	bool confirmed = mgr.ConfirmDeparture(); // 浮层内 Enter 确认
	Assert(confirmed && mgr.CurrentState == ChartState.DepartureConfirmed,
		"AC-17: 键盘完整出航流程 → DEPARTURE_CONFIRMED");
}

// ── AC-18: DEPARTURE_CONFIRMED 期间所有交互被禁用 ──
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.ConfirmDeparture();
	Assert(!mgr.IsInteractionAllowed(), "AC-18: DEPARTURE_CONFIRMED → IsInteractionAllowed=false");
	// 所有后续操作均被状态守卫拦截
	bool r1 = mgr.SelectRoute("route.storm-cut-01");
	bool r2 = mgr.DeselectRoute();
	bool r3 = mgr.ConfirmDeparture();
	Assert(!r1 && !r2 && !r3, "AC-18: 所有操作在 DEPARTURE_CONFIRMED 状态被拒绝");
}

// ── AC-19: stale route_id → 快照校验失败 ──
{
	var routeRegistry = new HashSet<string>(StringComparer.Ordinal) { "route.sky-reef-arc-01" };
	var p = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["domain_id"] = "progress.routes",
		["last_committed_route_id"] = "route.stale-not-in-registry",
		["departure_state"] = "DEPARTURE_CONFIRMED",
		["active_filter"] = "show_all",
		["last_departure_timestamp"] = 999.0,
	};
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid && violations.Any(v => v.Contains("route_id not found in registry")),
		"AC-19: stale route_id → 校验失败");
}

// ── AC-20: 未来时间戳 → 校验失败 ──
{
	var routeRegistry = new HashSet<string>(StringComparer.Ordinal) { "route.sky-reef-arc-01" };
	var p = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["domain_id"] = "progress.routes",
		["last_committed_route_id"] = "route.sky-reef-arc-01",
		["departure_state"] = "DEPARTURE_CONFIRMED",
		["active_filter"] = "show_all",
		["last_departure_timestamp"] = 9999999.0, // 远未来
	};
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid && violations.Contains("timestamp in future"),
		"AC-20: 未来时间戳 → 校验失败");
}

// ── AC-21: 缺失必需字段 → 校验失败，恢复时回退 ──
{
	var routeRegistry = new HashSet<string>(StringComparer.Ordinal) { "route.sky-reef-arc-01" };
	var p = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		// 缺少 departure_state 和 last_departure_timestamp
		["domain_id"] = "progress.routes",
		["last_committed_route_id"] = "route.sky-reef-arc-01",
	};
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid && violations.Any(v => v.Contains("missing field")),
		"AC-21: 缺失字段 → 校验失败（'missing field'）");
	// 恢复时：departure_state 非 DEPARTURE_CONFIRMED → Loading
	var mgr = new ChartManager();
	mgr.RestoreFromSnapshot(p);
	Assert(mgr.CurrentState == ChartState.Loading,
		"AC-21: 损坏快照恢复 → Loading（干净状态）");
}

// ── IsInteractionAllowed 在 Error 状态下为 false ──
{
	var mgr = BuildManager(threatsOk: false); // → ERROR
	Assert(!mgr.IsInteractionAllowed(), "EXTRA: ERROR 状态 → IsInteractionAllowed=false");
}

// ── IsInteractionAllowed 在 Browsing 状态下为 true ──
{
	var mgr = BuildManager();
	Assert(mgr.IsInteractionAllowed(), "EXTRA: Browsing 状态 → IsInteractionAllowed=true");
}

Console.WriteLine();
Console.WriteLine($"Story 008 Edge Cases: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
