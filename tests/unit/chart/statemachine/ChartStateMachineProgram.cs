using CloudWeaverVoyage.Core;

// Story 001 — Chart State Machine & Content Domain Gate
// 覆盖 AC-1 到 AC-18 全部验收标准

// ── 辅助：构建标准 ChartManager（全域 COMPLETE + 1 条可见航线）──
static ChartManager BuildBrowsing(string dockedAt = "location.glass-harbor")
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.RegisterRoute(new RouteStaticData(
		"route.high-risk-mvp", "location.glass-harbor", "location.danger-zone",
		"medium", new[] { "storm", "pirates" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2); // Identified
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => dockedAt);
	mgr.OpenChart();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 001: Chart State Machine & Content Domain Gate ===\n");

// ── AC-1: 全域 COMPLETE → LOADING → BROWSING ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-1: 全域 COMPLETE → BROWSING");
	Assert(mgr.VisibleRoutes.Count == 2, "AC-1: 2 条可见航线");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-1: 航线子状态初始为 BROWSABLE");
}

// ── AC-2: 域加载失败 → ERROR，记录失败域 ──
{
	var mgr = new ChartManager();
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Failed);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.OpenChart();
	Assert(mgr.CurrentState == ChartState.Error, "AC-2: 域失败 → ERROR");
	Assert(mgr.FailedDomainStates.TryGetValue("threats", out var ts) && ts == DomainState.Failed,
		"AC-2: _failed_domain_states 记录失败域");
}

// ── AC-3: BROWSING + SELECT browsable → ROUTE_SELECTED，子状态变 SELECTED ──
{
	var mgr = BuildBrowsing();
	ChartState? sigNew = null;
	mgr.ChartStateChanged += (_, n) => sigNew = n;
	bool result = mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(result, "AC-3: SelectRoute 返回 true");
	Assert(mgr.CurrentState == ChartState.RouteSelected, "AC-3: 状态 → ROUTE_SELECTED");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Selected,
		"AC-3: 航线子状态 → SELECTED");
	Assert(sigNew == ChartState.RouteSelected, "AC-3: ChartStateChanged 信号触发");
}

// ── AC-4: ROUTE_SELECTED + Esc/Deselect → BROWSING，子状态回 BROWSABLE ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	bool result = mgr.DeselectRoute();
	Assert(result, "AC-4: DeselectRoute 返回 true");
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-4: 状态 → BROWSING");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-4: 航线子状态回 BROWSABLE");
	Assert(mgr.SelectedRouteId == "", "AC-4: 无选中航线");
}

// ── AC-5: ERROR + cooldown=0 → RETRY → LOADING → BROWSING ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Failed);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.OpenChart(); // → ERROR，cooldown=2.0
	Assert(mgr.CurrentState == ChartState.Error, "AC-5: 进入 ERROR");
	// 冷却结束后修复域状态，重试
	mgr.TickCooldown(2.1); // 冷却耗尽
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.SetTraversableQueryDelegate(_ => true);
	bool retried = mgr.RetryOpenChart();
	Assert(retried, "AC-5: RetryOpenChart 返回 true");
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-5: 重试后 → BROWSING");
}

// ── AC-6: DEPARTURE_CONFIRMED 终端守卫——任何触发返回 allowed:false ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.ConfirmDeparture(); // → DepartureConfirmed
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed, "AC-6: 进入终端状态");
	// 任何触发均被拒绝
	var r1 = mgr.ChartStateTransition("SELECT");
	var r2 = mgr.ChartStateTransition("DESELECT");
	var r3 = mgr.ChartStateTransition("CONFIRM");
	var r4 = mgr.ChartStateTransition("RETRY");
	Assert(!r1.Allowed && !r2.Allowed && !r3.Allowed && !r4.Allowed,
		"AC-6: 所有触发均返回 allowed=false");
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed,
		"AC-6: 状态保持 DEPARTURE_CONFIRMED");
}

// ── AC-7: 同帧两次 CONFIRM——仅第一次有效 ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	int commitCount = 0;
	mgr.RouteCommitted += (_, _, _) => commitCount++;
	mgr.ConfirmDeparture(); // 第一次
	mgr.ConfirmDeparture(); // 第二次——终端守卫，should no-op
	Assert(commitCount == 1, "AC-7: route_committed 信号仅发出一次");
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed,
		"AC-7: 状态仍为 DEPARTURE_CONFIRMED");
}

// ── AC-8: ERROR → COMPLETE 触发被拒绝（必须通过 RETRY → LOADING）──
{
	var mgr = new ChartManager();
	mgr.SetDomainState("routes", DomainState.Failed);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.OpenChart(); // → ERROR
	var r = mgr.ChartStateTransition("COMPLETE");
	Assert(!r.Allowed, "AC-8: ERROR + COMPLETE 触发被拒绝");
	Assert(mgr.CurrentState == ChartState.Error, "AC-8: 状态仍为 ERROR");
}

// ── AC-9: LOADING + SELECT 被拒绝（默认拒绝）──
{
	var mgr = new ChartManager(); // 初始 Loading 状态
	var r = mgr.ChartStateTransition("SELECT");
	Assert(!r.Allowed, "AC-9: LOADING + SELECT 被拒绝");
}

// ── AC-10: 航线子状态 BROWSABLE + BROWSING → SELECT → SELECTED ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-10: 初始 BROWSABLE");
	mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Selected,
		"AC-10: BROWSABLE → SELECTED");
}

// ── AC-11: SELECTED + DESELECT → BROWSABLE ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.DeselectRoute();
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-11: SELECTED → BROWSABLE");
}

// ── AC-12: BROWSABLE + condition_change → UNAVAILABLE ──
{
	var mgr = BuildBrowsing();
	// 通过 ReevaluateAllRoutes 模拟条件变化（可通行变 false）
	mgr.SetTraversableQueryDelegate(_ => false);
	mgr.ReevaluateAllRoutes();
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Unavailable,
		"AC-12: 条件变化后 BROWSABLE → UNAVAILABLE");
}

// ── AC-13: UNAVAILABLE + condition_change → BROWSABLE ──
{
	var mgr = BuildBrowsing();
	mgr.SetTraversableQueryDelegate(_ => false);
	mgr.ReevaluateAllRoutes(); // → UNAVAILABLE
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.ReevaluateAllRoutes(); // → BROWSABLE
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-13: 条件恢复后 UNAVAILABLE → BROWSABLE");
}

// ── AC-14: DEPARTURE_CONFIRMED → 所有航线子状态 → LOCKED ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.ConfirmDeparture();
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Locked,
		"AC-14: 出航确认后选中航线 → LOCKED");
	Assert(mgr.GetRouteSubState("route.high-risk-mvp") == RouteSubState.Locked,
		"AC-14: 出航确认后其他航线 → LOCKED");
}

// ── AC-15: 内容域门控——全部 COMPLETE → COMPLETE 触发 ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-15: 全域 COMPLETE 进入 BROWSING");
	Assert(mgr.FailedDomainStates.All(kv => kv.Value == DomainState.Complete),
		"AC-15: 无失败域（所有域 COMPLETE）");
}

// ── AC-16: 部分航线查询失败——内部警告计数，不阻断 ──
{
	int queryCount = 0;
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.ok", "location.glass-harbor", "location.dest-a", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData(
		"route.fail", "location.glass-harbor", "location.dest-b", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(routeId =>
	{
		queryCount++;
		return routeId == "route.fail" ? -1 : 2; // route.fail 查询失败
	});
	mgr.OpenChart();
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-16: 部分失败不阻断，仍进入 BROWSING");
	Assert(mgr.InternalWarningCounter == 1, "AC-16: 内部警告计数=1");
	Assert(mgr.VisibleRoutes.Count == 1, "AC-16: 失败航线不出现在可见列表");
}

// ── AC-17: RETRY cooldown=0 时成功触发 ──
{
	var mgr = new ChartManager();
	mgr.SetDomainState("routes", DomainState.Failed);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.OpenChart(); // → ERROR + cooldown=2.0
	mgr.TickCooldown(2.0); // 精确耗尽
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 0); // Unknown，无可见路线
	Assert(mgr.RetryCooldownRemaining <= 0.0, "AC-17: 冷却已耗尽");
	bool result = mgr.RetryOpenChart();
	Assert(result, "AC-17: 冷却结束后 RETRY 成功");
}

// ── AC-18: 冷却期间 RETRY 被拒绝 ──
{
	var mgr = new ChartManager();
	mgr.SetDomainState("routes", DomainState.Failed);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.OpenChart(); // → ERROR + cooldown=2.0
	mgr.TickCooldown(1.5); // 还剩 0.5s
	var r = mgr.ChartStateTransition("RETRY");
	Assert(!r.Allowed, "AC-18: 冷却期间 RETRY 被拒绝（allowed=false）");
	Assert(mgr.CurrentState == ChartState.Error, "AC-18: 状态保持 ERROR");
	// 重试按钮在冷却期间禁用
	bool retried = mgr.RetryOpenChart();
	Assert(!retried, "AC-18: RetryOpenChart 冷却期间返回 false");
}

// ── UNAVAILABLE → SELECTED 禁止 ──
{
	var mgr = BuildBrowsing();
	mgr.SetTraversableQueryDelegate(_ => false);
	mgr.ReevaluateAllRoutes(); // → UNAVAILABLE
	bool result = mgr.SelectRoute("route.sky-reef-arc-01"); // 应失败
	Assert(!result, "GUARD: UNAVAILABLE 航线不可被选中");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Unavailable,
		"GUARD: 子状态保持 UNAVAILABLE");
}

Console.WriteLine();
Console.WriteLine($"Story 001 Chart State Machine: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
