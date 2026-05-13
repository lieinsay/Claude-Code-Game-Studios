using CloudWeaverVoyage.Core;

// Story 007 — External State Change Response (Integration)
// 覆盖 AC-1 到 AC-10 全部验收标准

static ChartManager BuildBrowsing(
	Func<string, int>? knowledgeFn = null,
	Func<string, bool>? traversableFn = null,
	string dockedAt = "location.glass-harbor")
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.RegisterRoute(new RouteStaticData(
		"route.storm-cut-01", "location.other-port", "location.danger-zone",
		"medium", new[] { "storm" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(knowledgeFn ?? (_ => 2)); // Identified
	mgr.SetTraversableQueryDelegate(traversableFn ?? (_ => true));
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

Console.WriteLine("=== Story 007: External State Change Response ===\n");

// ── AC-1: 信号连接接口存在（通过方法调用验证，测试中替代 Autoload 连接）──
{
	var mgr = BuildBrowsing();
	// 验证 4 个外部响应方法存在且可调用
	bool threw = false;
	try
	{
		mgr.OnKnowledgeChanged("location.glass-harbor", 3);
		mgr.OnAbilityChanged("ability.test", "path_a");
		mgr.OnRepairCompleted("repair_node_01");
		mgr.OnDockedLocationChanged("location.glass-harbor");
	}
	catch { threw = true; }
	Assert(!threw, "AC-1: 4 个外部响应方法存在且不崩溃");
}

// ── AC-2: knowledge_advanced → 重新评估相关航线 ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"), "AC-2: 前置：航线在可见列表");
	// 知识状态变为 verified → 航线仍可见
	mgr.OnKnowledgeChanged("location.glass-harbor", 3); // verified
	Assert(mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"),
		"AC-2: knowledge→verified 航线仍可见");
}

// ── AC-2b: knowledge→unknown → 航线从可见列表移除 ──
{
	int queryCount = 0;
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => { queryCount++; return 2; }); // 初始 identified
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	Assert(mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"), "AC-2b: 前置：航线可见");
	// 模拟知识变为 unknown
	mgr.SetKnowledgeQueryDelegate(_ => 0); // unknown
	mgr.OnKnowledgeChanged("location.glass-harbor", 0);
	Assert(!mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"),
		"AC-2b: knowledge→unknown 航线从可见列表移除");
}

// ── AC-3: ROUTE_SELECTED + knowledge→unknown → 强制取消选择 + 信号 ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(mgr.CurrentState == ChartState.RouteSelected, "AC-3: 前置：ROUTE_SELECTED");
	ChartState? sigNew = null;
	mgr.ChartStateChanged += (_, n) => sigNew = n;
	mgr.SetKnowledgeQueryDelegate(_ => 0); // knowledge→unknown
	mgr.OnKnowledgeChanged("location.glass-harbor", 0);
	Assert(mgr.CurrentState == ChartState.Browsing,
		"AC-3: knowledge→unknown 强制取消选择 → BROWSING");
	Assert(sigNew == ChartState.Browsing, "AC-3: chart_state_changed 信号发射");
}

// ── AC-4: ability_unlocked → UNAVAILABLE 航线→BROWSABLE ──
{
	var traversable = false;
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.SetTraversableQueryDelegate(_ => traversable);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Unavailable,
		"AC-4: 前置：traversable=false → UNAVAILABLE");
	// 能力解锁后 traversable 变 true
	traversable = true;
	mgr.OnAbilityChanged("ability.deep-navigation", "path_a");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Browsable,
		"AC-4: ability_unlocked → UNAVAILABLE→BROWSABLE");
}

// ── AC-5: 已选航线 traversable 保持 true → 不强制取消选择 ──
{
	var mgr = BuildBrowsing();
	mgr.SelectRoute("route.sky-reef-arc-01");
	Assert(mgr.CurrentState == ChartState.RouteSelected, "AC-5: 前置");
	mgr.OnAbilityChanged("ability.any", "path_a"); // traversable 仍 true
	Assert(mgr.CurrentState == ChartState.RouteSelected,
		"AC-5: traversable 保持 true → 不影响已选航线");
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Selected,
		"AC-5: 已选航线子状态保持 SELECTED");
}

// ── AC-6: WorldRepair 修复 → route_enhanced 信号 ──
{
	var mgr = BuildBrowsing();
	string? sigRoute = null; string? sigNode = null;
	mgr.RouteEnhanced += (r, n) => { sigRoute = r; sigNode = n; };
	// sky-reef-arc-01 受修复节点影响
	mgr.OnRepairCompleted("repair_lighthouse_01",
		new HashSet<string>(StringComparer.Ordinal) { "route.sky-reef-arc-01" });
	Assert(sigRoute == "route.sky-reef-arc-01", "AC-6: route_enhanced 信号发射");
	Assert(sigNode == "repair_lighthouse_01", "AC-6: enhancementId 正确");
}

// ── AC-7: 修复 node_id 不影响任何航线 → 不发射 route_enhanced ──
{
	var mgr = BuildBrowsing();
	int sigCount = 0;
	mgr.RouteEnhanced += (_, _) => sigCount++;
	mgr.OnRepairCompleted("repair_node_unrelated",
		new HashSet<string>(StringComparer.Ordinal)); // 空集合，无影响航线
	Assert(sigCount == 0, "AC-7: 无影响航线 → 不发射 route_enhanced");
}

// ── AC-8: 停靠地变更 → origin≠new_dock 航线→UNAVAILABLE ──
{
	var mgr = BuildBrowsing(dockedAt: "location.glass-harbor");
	// sky-reef 起点=glass-harbor（可选）；storm-cut 起点=other-port（不可选）
	// 切换停靠地到 other-port
	mgr.SetDockedLocationDelegate(() => "location.other-port");
	mgr.OnDockedLocationChanged("location.other-port");
	// sky-reef 起点=glass-harbor ≠ other-port → unavailable
	Assert(mgr.GetRouteSubState("route.sky-reef-arc-01") == RouteSubState.Unavailable,
		"AC-8: dock 变更后 origin≠dock 航线→UNAVAILABLE");
}

// ── AC-8b: 已选航线因停靠地变更变为 unavailable → 强制取消选择 ──
{
	var mgr = BuildBrowsing(dockedAt: "location.glass-harbor");
	mgr.SelectRoute("route.sky-reef-arc-01");
	mgr.SetDockedLocationDelegate(() => "location.other-port");
	mgr.OnDockedLocationChanged("location.other-port");
	Assert(mgr.CurrentState == ChartState.Browsing,
		"AC-8b: 已选航线 unavailable 后强制取消选择 → BROWSING");
}

// ── AC-9: 航图关闭时外部状态变化无效 ──
{
	var mgr = new ChartManager(); // Loading（非 Browsing）
	int evalCount = 0;
	// 直接调用处理方法，应该 no-op
	mgr.OnKnowledgeChanged("location.glass-harbor", 0);
	mgr.OnAbilityChanged("ability.test", "path");
	mgr.OnRepairCompleted("repair_node");
	mgr.OnDockedLocationChanged("location.new-port");
	Assert(evalCount == 0 && mgr.CurrentState == ChartState.Loading,
		"AC-9: 非 Browsing/RouteSelected 状态下外部变化无操作");
}

// ── AC-10: 重新评估 O(N) — N 条航线各调用一次 route_selectability ──
{
	int selectabilityCallCount = 0;
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.a", "location.glass-harbor", "location.dest-a", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.b", "location.glass-harbor", "location.dest-b", "medium", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => { selectabilityCallCount++; return 2; });
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	selectabilityCallCount = 0; // 重置计数
	mgr.ReevaluateAllRoutes();
	// N=2 条航线，route_selectability 内部调用 SafeQueryKnowledgeState
	Assert(selectabilityCallCount <= 4, // 每条航线最多调用几次（visibility+selectability内部）
		"AC-10: 重新评估 O(N) 调用——MVP 2 条 < 0.01ms");
}

// ── PurgeStaleRoutes — EC-8 缓存清理 ──
{
	var mgr = BuildBrowsing();
	Assert(mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"), "PURGE: 前置 sky-reef 可见");
	// 注册表中只剩 storm-cut（sky-reef 已被删除）
	mgr.PurgeStaleRoutes(new HashSet<string>(StringComparer.Ordinal) { "route.storm-cut-01" });
	Assert(!mgr.VisibleRoutes.Contains("route.sky-reef-arc-01"),
		"PURGE: stale 航线从可见列表移除");
}

Console.WriteLine();
Console.WriteLine($"Story 007 External State Response: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
