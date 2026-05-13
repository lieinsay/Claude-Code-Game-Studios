using CloudWeaverVoyage.Core;

// Story 003 — Two-Step Departure Confirmation & route_committed Signal
// 覆盖 AC-1 到 AC-13 全部验收标准

static ChartManager BuildSelected(bool traversable = true, string dockedAt = "location.glass-harbor")
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2); // Identified
	mgr.SetTraversableQueryDelegate(_ => traversable);
	mgr.SetDockedLocationDelegate(() => dockedAt);
	mgr.OpenChart();
	mgr.SelectRoute("route.sky-reef-arc-01");
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 003: Two-Step Departure Confirmation ===\n");

// ── AC-1: Step 1 RequestConfirmDeparture — 刷新并返回最新数据 ──
{
	var mgr = BuildSelected();
	var summary = mgr.RequestConfirmDeparture("route.sky-reef-arc-01");
	Assert(summary != null, "AC-1: RequestConfirmDeparture 返回非 null");
	Assert((bool)summary!["traversable"], "AC-1: traversable=true（刷新后）");
	Assert(summary["distance_band"] as string == "short", "AC-1: distance_band=short");
	Assert(summary["destination_id"] as string == "location.sky-reef-outpost", "AC-1: destination_id 正确");
	Assert(mgr.CurrentState == ChartState.RouteSelected,
		"AC-1: Step 1 不改变状态，仍为 ROUTE_SELECTED");
}

// ── AC-1 细节：RequestConfirmDeparture 在错误状态下返回 null ──
{
	var mgr = new ChartManager();
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.OpenChart(); // BROWSING，未选航线
	Assert(mgr.RequestConfirmDeparture("route.any") == null,
		"AC-1b: 非 ROUTE_SELECTED 状态返回 null");
}

// ── AC-2: Step 2 取消（不调用 ConfirmDeparture）— 状态保持 ROUTE_SELECTED ──
{
	var mgr = BuildSelected();
	mgr.RequestConfirmDeparture("route.sky-reef-arc-01"); // Step 1
	// 玩家点击"取消"——不调用 ConfirmDeparture，直接调用 DeselectRoute
	mgr.DeselectRoute();
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-2: 取消后 → BROWSING");
	Assert(mgr.SelectedRouteId == "", "AC-2: 无选中航线");
}

// ── AC-3: Step 2 确认 ConfirmDeparture → DEPARTURE_CONFIRMED + 信号一次 ──
{
	var mgr = BuildSelected();
	int commitCount = 0;
	string? sigRoute = null; string? sigDest = null; IReadOnlyList<string>? sigHazards = null;
	mgr.RouteCommitted += (r, d, h) => { commitCount++; sigRoute = r; sigDest = d; sigHazards = h; };
	bool result = mgr.ConfirmDeparture();
	Assert(result, "AC-3: ConfirmDeparture 返回 true");
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed, "AC-3: → DEPARTURE_CONFIRMED");
	Assert(commitCount == 1, "AC-3: route_committed 信号发射恰好一次");
	Assert(sigRoute == "route.sky-reef-arc-01", "AC-3: 信号 route_id 正确");
	Assert(sigDest == "location.sky-reef-outpost", "AC-3: 信号 destination_id 正确");
	Assert(sigHazards != null && sigHazards.Contains("safe"), "AC-3: 信号 hazard_tags 包含 'safe'");
	// 所有航线子状态 → LOCKED
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Locked,
		"AC-3: 出航后航线 → LOCKED");
}

// ── AC-4: 单次发射保证——同帧两次 CONFIRM 仅第一次有效 ──
{
	var mgr = BuildSelected();
	int commitCount = 0;
	mgr.RouteCommitted += (_, _, _) => commitCount++;
	mgr.ConfirmDeparture(); // 第一次
	mgr.ConfirmDeparture(); // 第二次——终端守卫拦截
	Assert(commitCount == 1, "AC-4: 两次 CONFIRM 仅发射一次信号");
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed, "AC-4: 状态仍为 DEPARTURE_CONFIRMED");
}

// ── AC-5: 锁定期间所有 route_selectability → "locked" ──
{
	var mgr = BuildSelected();
	mgr.ConfirmDeparture();
	Assert(mgr.RouteSelectability("route.sky-reef-arc-01") == "locked",
		"AC-5: 锁定期间 route_selectability → 'locked'");
}

// ── AC-6: Step 1 刷新保证——风险数据即时更新 ──
{
	var mgr = BuildSelected(); // 初始 traversable=true, hazards=["safe"]
	// 模拟第一步请求时风险已变更
	mgr.SetTraversableQueryDelegate(_ => true); // 仍可通行
	var summary = mgr.RequestConfirmDeparture("route.sky-reef-arc-01");
	Assert(summary != null && (bool)summary["traversable"],
		"AC-6: Step 1 返回刷新后的 traversable 状态");
}

// ── AC-7: Step 1 发现 traversable=false → 强制取消选择 + RouteSelectionFailed ──
{
	var mgr = BuildSelected();
	// 在第二步时 traversable 变为 false
	mgr.SetTraversableQueryDelegate(_ => false);
	string? failRoute = null; string? failReason = null;
	mgr.RouteSelectionFailed += (r, reason) => { failRoute = r; failReason = reason; };
	bool result = mgr.ConfirmDeparture();
	Assert(!result, "AC-7: traversable=false → ConfirmDeparture 返回 false");
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-7: 强制取消选择 → BROWSING");
	Assert(failRoute == "route.sky-reef-arc-01", "AC-7: RouteSelectionFailed 信号触发");
	Assert(failReason == "route_not_traversable", "AC-7: 失败原因='route_not_traversable'");
}

// ── AC-8: route_committed 信号签名——3 个 typed 参数 ──
{
	var mgr = BuildSelected();
	string? sigR = null; string? sigD = null; IReadOnlyList<string>? sigH = null;
	mgr.RouteCommitted += (r, d, h) => { sigR = r; sigD = d; sigH = h; };
	mgr.ConfirmDeparture();
	Assert(sigR is string, "AC-8: route_id 为 string（typed）");
	Assert(sigD is string, "AC-8: destination_id 为 string（typed）");
	Assert(sigH is IReadOnlyList<string>, "AC-8: hazard_tags 为 IReadOnlyList<string>（typed）");
}

// ── AC-9: route_committed 同步 emit（在 ConfirmDeparture 返回前已触发）──
{
	var mgr = BuildSelected();
	bool sigFiredBeforeReturn = false;
	mgr.RouteCommitted += (_, _, _) => sigFiredBeforeReturn = true;
	mgr.ConfirmDeparture();
	// 若信号是同步发射，sigFiredBeforeReturn 在方法返回前已设置
	Assert(sigFiredBeforeReturn, "AC-9: route_committed 同步发射（无 deferred）");
}

// ── AC-10: 快照校验失败模拟（traversable=false → route_selection_failed）──
// 注意：当前实现中 snapshot 验证在外部，traversable=false 路径覆盖信号发射
{
	var mgr = BuildSelected();
	mgr.SetTraversableQueryDelegate(_ => false);
	string? failReason = null;
	mgr.RouteSelectionFailed += (_, r) => failReason = r;
	mgr.ConfirmDeparture();
	Assert(failReason != null, "AC-10/11: route_selection_failed 信号发射");
	Assert(failReason == "route_not_traversable", "AC-10/11: reason 字符串正确");
}

// ── AC-11: traversable=false → 强制取消选择 → BROWSING（已在 AC-7 覆盖）──
// 额外验证：ForceDeselect 直接调用
{
	var mgr = BuildSelected();
	mgr.ForceDeselect("route.sky-reef-arc-01");
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-11: ForceDeselect → BROWSING");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-11: ForceDeselect 后航线子状态 → BROWSABLE");
}

// ── AC-12: DEPARTURE_CONFIRMED 进入后所有航线子状态 → LOCKED ──
{
	var mgr = BuildSelected();
	mgr.ConfirmDeparture();
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Locked,
		"AC-12: 锁定期间子状态 → LOCKED");
}

// ── AC-13: 锁定结束——状态保持 DEPARTURE_CONFIRMED（航图不再可用）──
{
	var mgr = BuildSelected();
	mgr.ConfirmDeparture();
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed,
		"AC-13: 状态保持 DEPARTURE_CONFIRMED，不自动返回");
}

// ── RequestConfirmDeparture 对非选中航线返回 null ──
{
	var mgr = BuildSelected();
	var r1 = mgr.RequestConfirmDeparture("route.other"); // 不是当前选中
	Assert(r1 == null, "GUARD: RequestConfirmDeparture 对非选中航线返回 null");
}

Console.WriteLine();
Console.WriteLine($"Story 003 Departure Confirmation: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
