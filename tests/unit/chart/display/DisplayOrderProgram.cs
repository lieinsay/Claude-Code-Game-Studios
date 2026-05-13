using CloudWeaverVoyage.Core;

// Story 004 — Route Display Ordering & Filtering
// 覆盖 AC-1 到 AC-14 全部验收标准

// 知识状态: 0=Unknown 1=Rumored 2=Identified 3=Verified

static ChartManager BuildManager(
	Dictionary<string, int>? knowledgeMap = null,
	Dictionary<string, string>? distanceMap = null,
	bool hideRumored = false)
{
	var mgr = new ChartManager();
	// 注册各种测试航线
	var routes = new[]
	{
		("route.verified-short",  "location.a", "short"),
		("route.verified-medium", "location.a", "medium"),
		("route.verified-long",   "location.a", "long"),
		("route.identified-short",  "location.a", "short"),
		("route.identified-medium", "location.a", "medium"),
		("route.identified-long",   "location.a", "long"),
		("route.rumored-short",  "location.a", "short"),
		("route.rumored-medium", "location.a", "medium"),
		("route.rumored-long",   "location.a", "long"),
		("route.unknown",        "location.a", "short"),
		// MVP 两条航线
		("route.sky-reef-arc-01", "location.glass-harbor", "short"),
		("route.storm-cut-01",    "location.glass-harbor", "medium"),
	};
	foreach (var (id, origin, dist) in routes)
		mgr.RegisterRoute(new RouteStaticData(id, origin, "location.dest", dist, Array.Empty<string>()));

	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);

	var defaultKnowledge = new Dictionary<string, int>(StringComparer.Ordinal)
	{
		["route.verified-short"] = 3,
		["route.verified-medium"] = 3,
		["route.verified-long"] = 3,
		["route.identified-short"] = 2,
		["route.identified-medium"] = 2,
		["route.identified-long"] = 2,
		["route.rumored-short"] = 1,
		["route.rumored-medium"] = 1,
		["route.rumored-long"] = 1,
		["route.unknown"] = 0,
		["route.sky-reef-arc-01"] = 2,  // identified
		["route.storm-cut-01"] = 1,     // rumored
	};
	var km = knowledgeMap ?? defaultKnowledge;
	mgr.SetKnowledgeQueryDelegate(routeId => km.TryGetValue(routeId, out var k) ? k : 0);
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
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

Console.WriteLine("=== Story 004: Route Display Ordering & Filtering ===\n");

// ── AC-1: verified + short → 101 ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteDisplayOrder("route.verified-short") == 101,
		"AC-1: verified+short → 101");
}

// ── AC-2: identified + medium → 202 ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteDisplayOrder("route.identified-medium") == 202,
		"AC-2: identified+medium → 202");
}

// ── AC-3: rumored + long → 303 ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteDisplayOrder("route.rumored-long") == 303,
		"AC-3: rumored+long → 303");
}

// ── AC-4: unknown → 999（防御性） ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteDisplayOrder("route.unknown") == 999,
		"AC-4: unknown → 999");
}

// ── AC-5: 全部 9 种组合 ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteDisplayOrder("route.verified-short") == 101, "AC-5[1]: verified+short=101");
	Assert(mgr.RouteDisplayOrder("route.verified-medium") == 102, "AC-5[2]: verified+medium=102");
	Assert(mgr.RouteDisplayOrder("route.verified-long") == 103, "AC-5[3]: verified+long=103");
	Assert(mgr.RouteDisplayOrder("route.identified-short") == 201, "AC-5[4]: identified+short=201");
	Assert(mgr.RouteDisplayOrder("route.identified-medium") == 202, "AC-5[5]: identified+medium=202");
	Assert(mgr.RouteDisplayOrder("route.identified-long") == 203, "AC-5[6]: identified+long=203");
	Assert(mgr.RouteDisplayOrder("route.rumored-short") == 301, "AC-5[7]: rumored+short=301");
	Assert(mgr.RouteDisplayOrder("route.rumored-medium") == 302, "AC-5[8]: rumored+medium=302");
	Assert(mgr.RouteDisplayOrder("route.rumored-long") == 303, "AC-5[9]: rumored+long=303");
}

// ── AC-6: MVP 排序——sky-reef (201) 排在 storm-cut (302) 之前 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.sky-reef-arc-01", "location.glass-harbor", "location.dest", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.storm-cut-01", "location.glass-harbor", "location.dest", "medium", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.sky-reef-arc-01" ? 2 : 1); // identified vs rumored
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.OpenChart();
	Assert(mgr.RouteDisplayOrder("route.sky-reef-arc-01") == 201,
		"AC-6: sky-reef-arc-01 display_order=201");
	Assert(mgr.RouteDisplayOrder("route.storm-cut-01") == 302,
		"AC-6: storm-cut-01 display_order=302");
	var visible = mgr.GetVisibleRoutes();
	Assert(visible.Count >= 2 && visible[0] == "route.sky-reef-arc-01",
		"AC-6: sky-reef-arc-01 排在 storm-cut-01 之前");
}

// ── AC-7: 同层级不同距离——short 排 medium 之前 ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteDisplayOrder("route.identified-short") < mgr.RouteDisplayOrder("route.identified-medium"),
		"AC-7: same-knowledge short (201) < medium (202)");
	Assert(mgr.RouteDisplayOrder("route.identified-medium") < mgr.RouteDisplayOrder("route.identified-long"),
		"AC-7: same-knowledge medium (202) < long (203)");
}

// ── AC-8: 相同 display_order → 按 route_id 字典序打破平局 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.z-identified-short", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.a-identified-short", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2); // both identified
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	// 两条均为 201，按 route_id 字典序：a < z
	var visible = mgr.GetVisibleRoutes();
	Assert(visible.Count >= 2 && visible[0] == "route.a-identified-short",
		"AC-8: 相同 display_order 按 route_id 字典序（a < z）");
	// 多次查询结果稳定
	var visible2 = mgr.GetVisibleRoutes();
	Assert(visible.SequenceEqual(visible2), "AC-8: 排序结果稳定");
}

// ── AC-9: hide_rumored=false → 全部 3 条可见，按 display_order 排序 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.r-a", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.r-b", "location.a", "location.d", "medium", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.r-c", "location.a", "location.d", "long", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.r-c" ? 1 : 2); // r-c rumored, others identified
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	var visible = mgr.GetVisibleRoutes();
	Assert(visible.Count == 3, "AC-9: hide_rumored=false → 3 条全部可见");
	// identified-short(201) < identified-medium(202) < rumored-long(303)
	Assert(visible[0] == "route.r-a" && visible[2] == "route.r-c",
		"AC-9: 按 display_order 正确排序");
}

// ── AC-10: hide_rumored=true → rumored 被过滤 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.r-a", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.r-b", "location.a", "location.d", "medium", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.r-c", "location.a", "location.d", "long", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.r-c" ? 1 : 2);
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	bool sigFired = false;
	mgr.FilterChanged += _ => sigFired = true;
	mgr.ToggleHideRumored(true);
	var visible = mgr.GetVisibleRoutes();
	Assert(visible.Count == 2, "AC-10: hide_rumored=true → 2 条 identified");
	Assert(!visible.Contains("route.r-c"), "AC-10: rumored 航线不在列表中");
	Assert(sigFired, "AC-10: FilterChanged 信号触发");
}

// ── AC-11: 全部 rumored + hide_rumored=true → 空列表 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.r-x", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 1); // all rumored
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	mgr.ToggleHideRumored(true);
	var visible = mgr.GetVisibleRoutes();
	Assert(visible.Count == 0, "AC-11: 全 rumored + hide=true → 空列表");
}

// ── AC-12: hide_rumored 状态持久（同帧多次查询一致）──
{
	var mgr = BuildManager(hideRumored: true);
	var v1 = mgr.GetVisibleRoutes();
	var v2 = mgr.GetVisibleRoutes();
	Assert(v1.SequenceEqual(v2), "AC-12: hide_rumored 状态持久，多次查询一致");
}

// ── AC-13: 查询失败 → 知识状态视为 unknown → 999 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.fail", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => -1); // 查询失败
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	Assert(mgr.RouteDisplayOrder("route.fail") == 999, "AC-13: 查询失败 → 999");
}

// ── AC-14: 未知 distance_band → 视为 medium（防御性默认）──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.unknown-dist", "location.a", "location.d",
		"unknown_band", Array.Empty<string>())); // 未知距离带
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2); // identified
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	// identified(rank=2) × 100 + medium_default(rank=2) = 202
	Assert(mgr.RouteDisplayOrder("route.unknown-dist") == 202,
		"AC-14: 未知 distance_band → medium(2) → identified×100+2 = 202，不崩溃");
}

// ── ToggleHideRumored 已选航线被隐藏时强制取消选择 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.rumored-sel", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.identified-sel", "location.a", "location.d", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(r => r == "route.rumored-sel" ? 1 : 2);
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.a");
	mgr.OpenChart();
	mgr.SelectRoute("route.rumored-sel"); // 选中传闻航线
	Assert(mgr.CurrentState == ChartState.RouteSelected, "EXTRA: 选中传闻航线");
	mgr.ToggleHideRumored(true); // 筛选器隐藏 rumored → 强制取消选择
	Assert(mgr.CurrentState == ChartState.Browsing, "EXTRA: 被隐藏的已选航线强制取消选择 → BROWSING");
}

Console.WriteLine();
Console.WriteLine($"Story 004 Display Ordering: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
