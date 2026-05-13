using CloudWeaverVoyage.Core;

// Story 002 — Route Visibility & Selectability Formulas
// 覆盖 AC-1 到 AC-15 全部验收标准

static ChartManager BuildManager(
	Func<string, int>? knowledgeFn = null,
	Func<string, bool>? traversableFn = null,
	string dockedAt = "location.glass-harbor",
	bool hideRumored = false)
{
	var mgr = new ChartManager();
	// 注册测试航线
	mgr.RegisterRoute(new RouteStaticData("route.unknown-route", "location.glass-harbor", "location.a", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.rumored-route", "location.glass-harbor", "location.b", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.identified-route", "location.glass-harbor", "location.c", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.verified-route", "location.glass-harbor", "location.d", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.blocked-route", "location.glass-harbor", "location.e", "short", Array.Empty<string>()));
	mgr.RegisterRoute(new RouteStaticData("route.wrong-origin", "location.OTHER-PORT", "location.f", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	// 默认知识状态映射
	mgr.SetKnowledgeQueryDelegate(knowledgeFn ?? (routeId => routeId switch
	{
		"route.unknown-route" => 0,   // Unknown
		"route.rumored-route" => 1,   // Rumored
		"route.identified-route" => 2, // Identified
		"route.verified-route" => 3,  // Verified
		"route.blocked-route" => 2,   // Identified（但不可通行）
		"route.wrong-origin" => 2,    // Identified（但起点不对）
		_ => 0,
	}));
	mgr.SetTraversableQueryDelegate(traversableFn ?? (routeId =>
		routeId != "route.blocked-route"));
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

Console.WriteLine("=== Story 002: Route Visibility & Selectability Formulas ===\n");

// ── Formula 1 — route_visibility ──────────────────────────────────

// AC-1: unknown → false
{
	var mgr = BuildManager();
	Assert(!mgr.RouteVisibility("route.unknown-route", false), "AC-1: unknown → false");
}

// AC-2: rumored + hide_rumored=true → false
{
	var mgr = BuildManager(hideRumored: true);
	Assert(!mgr.RouteVisibility("route.rumored-route", true), "AC-2: rumored + hide_rumored → false");
}

// AC-3: rumored + hide_rumored=false → true
{
	var mgr = BuildManager();
	Assert(mgr.RouteVisibility("route.rumored-route", false), "AC-3: rumored + show → true");
}

// AC-4: identified/verified + hide_rumored=true/false → true
{
	var mgr = BuildManager();
	Assert(mgr.RouteVisibility("route.identified-route", false), "AC-4a: identified+show → true");
	Assert(mgr.RouteVisibility("route.identified-route", true),  "AC-4b: identified+hide → true");
	Assert(mgr.RouteVisibility("route.verified-route", false),   "AC-4c: verified+show → true");
	Assert(mgr.RouteVisibility("route.verified-route", true),    "AC-4d: verified+hide → true");
}

// AC-5: 查询失败（返回 -1）→ 视为 unknown → false
{
	var mgr = BuildManager(knowledgeFn: _ => -1);
	Assert(!mgr.RouteVisibility("route.identified-route", false),
		"AC-5: 查询失败视为 unknown → false");
}

// ── Formula 2 — route_selectability 短路求值 ──────────────────────

// AC-6: visibility=false → "hidden"（短路）
{
	var mgr = BuildManager();
	Assert(mgr.RouteSelectability("route.unknown-route") == "hidden",
		"AC-6: unknown → 'hidden'（分支 1 短路）");
	// 验证短路：对 unknown 航线调用 RouteSelectability 时不触发 traversable 查询
	// OpenChart() 末尾的 ReevaluateAllRoutes 会对可见航线查询（属于正常流程），
	// 此处仅验证对 hidden 航线的手动查询不额外触发 traversable。
	int traversableCallCount = 0;
	var mgr2 = BuildManager(traversableFn: routeId =>
	{
		traversableCallCount++;
		return true;
	});
	// 重置计数器（OpenChart 内部已对可见航线完成初始评估）
	traversableCallCount = 0;
	mgr2.RouteSelectability("route.unknown-route"); // hidden 分支短路——不调用 traversable
	Assert(traversableCallCount == 0, "AC-6: hidden 分支不调用 traversable 查询（性能验证）");
}

// AC-7: chart_state=DEPARTURE_CONFIRMED → "locked"
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.identified-route");
	mgr.ConfirmDeparture(); // → DepartureConfirmed
	Assert(mgr.RouteSelectability("route.identified-route") == "locked",
		"AC-7: DepartureConfirmed → 'locked'（分支 2）");
	Assert(mgr.RouteSelectability("route.verified-route") == "locked",
		"AC-7: 其他可见航线也为 'locked'");
}

// AC-8: traversable=false → "unavailable"（分支 3）
{
	var mgr = BuildManager();
	Assert(mgr.RouteSelectability("route.blocked-route") == "unavailable",
		"AC-8: traversable=false → 'unavailable'（分支 3）");
}

// AC-9: origin ≠ docked_location → "unavailable"（分支 4）
{
	var mgr = BuildManager();
	Assert(mgr.RouteSelectability("route.wrong-origin") == "unavailable",
		"AC-9: origin≠docked → 'unavailable'（分支 4）");
}

// AC-10: route_id == selected_route_id + ROUTE_SELECTED → "selected"（分支 5）
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.identified-route");
	Assert(mgr.CurrentState == ChartState.RouteSelected, "AC-10: 前置：进入 ROUTE_SELECTED");
	Assert(mgr.RouteSelectability("route.identified-route") == "selected",
		"AC-10: 已选航线 → 'selected'（分支 5）");
}

// AC-11: ROUTE_SELECTED + 非已选航线 → "browsable"（分支 6）
{
	var mgr = BuildManager();
	mgr.SelectRoute("route.identified-route");
	Assert(mgr.RouteSelectability("route.verified-route") == "browsable",
		"AC-11: ROUTE_SELECTED + 非已选可见航线 → 'browsable'（分支 6）");
}

// AC-12: BROWSING + 满足所有条件 → "browsable"（分支 7）
{
	var mgr = BuildManager();
	Assert(mgr.CurrentState == ChartState.Browsing, "AC-12: 前置：BROWSING 状态");
	Assert(mgr.RouteSelectability("route.identified-route") == "browsable",
		"AC-12: BROWSING + 满足条件 → 'browsable'（分支 7）");
}

// AC-13: 完整短路链验证（8 种场景）
{
	// 场景 1: unknown → hidden
	var mgr1 = BuildManager();
	Assert(mgr1.RouteSelectability("route.unknown-route") == "hidden",
		"AC-13[1]: unknown → hidden");

	// 场景 2: rumored + hide_rumored=true → hidden
	var mgr2 = BuildManager(hideRumored: true);
	Assert(mgr2.RouteSelectability("route.rumored-route") == "hidden",
		"AC-13[2]: rumored+hide → hidden");

	// 场景 3: DEPARTURE_CONFIRMED → locked
	var mgr3 = BuildManager();
	mgr3.SelectRoute("route.identified-route");
	mgr3.ConfirmDeparture();
	Assert(mgr3.RouteSelectability("route.identified-route") == "locked",
		"AC-13[3]: DepartureConfirmed → locked");

	// 场景 4: identified + traversable=false → unavailable
	var mgr4 = BuildManager();
	Assert(mgr4.RouteSelectability("route.blocked-route") == "unavailable",
		"AC-13[4]: identified+not-traversable → unavailable");

	// 场景 5: identified + origin≠docked → unavailable
	var mgr5 = BuildManager();
	Assert(mgr5.RouteSelectability("route.wrong-origin") == "unavailable",
		"AC-13[5]: origin≠docked → unavailable");

	// 场景 6: identified + ROUTE_SELECTED + self → selected
	var mgr6 = BuildManager();
	mgr6.SelectRoute("route.identified-route");
	Assert(mgr6.RouteSelectability("route.identified-route") == "selected",
		"AC-13[6]: self in ROUTE_SELECTED → selected");

	// 场景 7: identified + ROUTE_SELECTED + other → browsable
	var mgr7 = BuildManager();
	mgr7.SelectRoute("route.identified-route");
	Assert(mgr7.RouteSelectability("route.verified-route") == "browsable",
		"AC-13[7]: other in ROUTE_SELECTED → browsable");

	// 场景 8: identified + BROWSING → browsable
	var mgr8 = BuildManager();
	Assert(mgr8.RouteSelectability("route.identified-route") == "browsable",
		"AC-13[8]: identified in BROWSING → browsable");
}

// AC-14: accessibility 查询失败 → traversable 默认 false → "unavailable"
{
	// SetTraversableQueryDelegate 抛出异常 → SafeQueryTraversable 返回 false
	var mgr = BuildManager(traversableFn: routeId =>
	{
		if (routeId == "route.identified-route") throw new InvalidOperationException("accessibility error");
		return true;
	});
	Assert(mgr.RouteSelectability("route.identified-route") == "unavailable",
		"AC-14: accessibility 查询异常 → 默认 false → 'unavailable'，不崩溃");
}

// AC-15: AirshipHub 未就绪（_getCurrentDockedLocation=null）→ 所有航线 UNAVAILABLE
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData("route.any", "location.glass-harbor", "location.dest", "short", Array.Empty<string>()));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2); // Identified
	mgr.SetTraversableQueryDelegate(_ => true);
	// 不设置 DockedLocationDelegate → 默认返回 ""
	mgr.OpenChart();
	// origin="location.glass-harbor" ≠ "" → unavailable
	Assert(mgr.RouteSelectability("route.any") == "unavailable",
		"AC-15: AirshipHub 未就绪 → docked='', 所有航线 origin≠'' → unavailable");
}

// ── ReevaluateAllRoutes 集成验证 ──
{
	var mgr = BuildManager();
	Assert(mgr.RouteSelectability("route.identified-route") == "browsable",
		"REEVAL: 初始 browsable");
	mgr.SetTraversableQueryDelegate(_ => false);
	mgr.ReevaluateAllRoutes();
	Assert(mgr.GetRouteSubState("route.identified-route") == RouteSubState.Unavailable,
		"REEVAL: 重评后 → UNAVAILABLE");
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.ReevaluateAllRoutes();
	Assert(mgr.GetRouteSubState("route.identified-route") == RouteSubState.Browsable,
		"REEVAL: 恢复后 → BROWSABLE");
}

Console.WriteLine();
Console.WriteLine($"Story 002 Visibility & Selectability: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
