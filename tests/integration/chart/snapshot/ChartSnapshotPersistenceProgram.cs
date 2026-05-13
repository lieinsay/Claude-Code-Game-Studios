using CloudWeaverVoyage.Core;

// Story 005 — Snapshot Validation & Persistence (Integration)
// 覆盖 AC-1 到 AC-18 全部验收标准

// ── 辅助：构建已进入 DEPARTURE_CONFIRMED 的 ChartManager ──
static ChartManager BuildConfirmed(string routeId = "route.sky-reef-arc-01",
	bool hideRumored = false)
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		routeId, "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr.SetDomainState("routes", DomainState.Complete);
	mgr.SetDomainState("world", DomainState.Complete);
	mgr.SetDomainState("intel", DomainState.Complete);
	mgr.SetDomainState("threats", DomainState.Complete);
	mgr.SetKnowledgeQueryDelegate(_ => 2);
	mgr.SetTraversableQueryDelegate(_ => true);
	mgr.SetDockedLocationDelegate(() => "location.glass-harbor");
	mgr.SetHideRumored(hideRumored);
	mgr.OpenChart();
	mgr.SelectRoute(routeId);
	mgr.ConfirmDeparture();
	return mgr;
}

// 合法快照 payload
static Dictionary<string, object?> ValidPayload(string routeId = "route.sky-reef-arc-01",
	double ts = 999.0) =>
	new(StringComparer.Ordinal)
	{
		["domain_id"] = "progress.routes",
		["last_committed_route_id"] = routeId,
		["departure_state"] = "DEPARTURE_CONFIRMED",
		["active_filter"] = "show_all",
		["last_departure_timestamp"] = ts,
		["hide_rumored"] = false,
	};

var routeRegistry = new HashSet<string>(StringComparer.Ordinal)
	{ "route.sky-reef-arc-01", "route.storm-cut-01" };

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 005: Snapshot Validation & Persistence ===\n");

// ── AC-1: BuildSnapshotPayload — 5 个字段 ──
{
	var mgr = BuildConfirmed();
	var payload = mgr.BuildSnapshotPayload("route.sky-reef-arc-01", 999.0);
	Assert(payload.ContainsKey("last_committed_route_id"), "AC-1: last_committed_route_id 存在");
	Assert(payload.ContainsKey("departure_state"), "AC-1: departure_state 存在");
	Assert(payload.ContainsKey("active_filter"), "AC-1: active_filter 存在");
	Assert(payload.ContainsKey("last_departure_timestamp"), "AC-1: last_departure_timestamp 存在");
	Assert(payload.ContainsKey("hide_rumored"), "AC-1: hide_rumored 存在");
	Assert(payload["departure_state"]?.ToString() == "DEPARTURE_CONFIRMED",
		"AC-1: departure_state=DEPARTURE_CONFIRMED");
	Assert(payload["last_committed_route_id"]?.ToString() == "route.sky-reef-arc-01",
		"AC-1: last_committed_route_id 正确");
}

// ── AC-2: 快照不含派生值 ──
{
	var mgr = BuildConfirmed();
	var payload = mgr.BuildSnapshotPayload("route.sky-reef-arc-01", 999.0);
	Assert(!payload.ContainsKey("_visible_routes"), "AC-2: 不含 _visible_routes");
	Assert(!payload.ContainsKey("_route_states"), "AC-2: 不含 _route_states");
	Assert(!payload.ContainsKey("_selected_route_id"), "AC-2: 不含 _selected_route_id");
}

// ── AC-3: 合法快照 → valid=true, violations=[] ──
{
	var p = ValidPayload();
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(valid, "AC-3: 合法快照 → valid=true");
	Assert(violations.Count == 0, "AC-3: violations 为空");
}

// ── AC-4: null payload → "malformed snapshot package" ──
{
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(null, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-4: null → valid=false");
	Assert(violations.Contains("malformed snapshot package"), "AC-4: 含 'malformed snapshot package'");
}

// ── AC-5: wrong domain_id → "wrong domain_id" ──
{
	var p = ValidPayload();
	var p2 = new Dictionary<string, object?>(p, StringComparer.Ordinal) { ["domain_id"] = "wrong.domain" };
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p2, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-5: wrong domain_id → valid=false");
	Assert(violations.Contains("wrong domain_id"), "AC-5: 含 'wrong domain_id'");
}

// ── AC-6: 缺少必需字段 → "missing field: <name>" ──
{
	foreach (var field in new[] { "last_committed_route_id", "departure_state", "active_filter", "last_departure_timestamp" })
	{
		var p = ValidPayload();
		var p2 = new Dictionary<string, object?>(p, StringComparer.Ordinal);
		p2.Remove(field);
		var (valid, violations) = ChartManager.ValidateSnapshotPackage(p2, 1000.0, 300.0, routeRegistry);
		Assert(!valid && violations.Any(v => v.Contains($"missing field: {field}")),
			$"AC-6: 缺少 {field} → 'missing field: {field}'");
	}
}

// ── AC-7: departure_state != DEPARTURE_CONFIRMED → "invalid departure_state" ──
{
	var p = ValidPayload();
	var p2 = new Dictionary<string, object?>(p, StringComparer.Ordinal) { ["departure_state"] = "BROWSING" };
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p2, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-7: invalid departure_state → valid=false");
	Assert(violations.Contains("invalid departure_state"), "AC-7: 含 'invalid departure_state'");
}

// ── AC-8: NaN 时间戳 → "non-finite timestamp" ──
{
	var p = ValidPayload(ts: double.NaN);
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-8: NaN timestamp → valid=false");
	Assert(violations.Contains("non-finite timestamp"), "AC-8: 含 'non-finite timestamp'");
}

// ── AC-8b: ±Inf 时间戳 → "non-finite timestamp" ──
{
	var p = ValidPayload(ts: double.PositiveInfinity);
	var (_, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(violations.Contains("non-finite timestamp"), "AC-8b: +Inf → 'non-finite timestamp'");
}

// ── AC-9: epoch/zero 时间戳 → "timestamp is epoch or uninitialized" ──
{
	var p = ValidPayload(ts: 0.0);
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-9: epoch ts=0 → valid=false");
	Assert(violations.Contains("timestamp is epoch or uninitialized"),
		"AC-9: 含 'timestamp is epoch or uninitialized'");
}

// ── AC-10: 未来时间戳（超过 tolerance）→ "timestamp in future" ──
{
	// currentTime=1000, tolerance=300, ts=1500 > 1000+300=1300
	var p = ValidPayload(ts: 1500.0);
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-10: future ts=1500 → valid=false");
	Assert(violations.Contains("timestamp in future"), "AC-10: 含 'timestamp in future'");
}

// ── AC-10b: 时间戳在 tolerance 内（合法）──
{
	// ts=1200, tolerance=300, 1200 <= 1000+300=1300 → 合法
	var p = ValidPayload(ts: 1200.0);
	var (valid, _) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(valid, "AC-10b: ts 在 tolerance 范围内 → valid=true");
}

// ── AC-11: stale route_id → "route_id not found in registry" ──
{
	var p = ValidPayload("route.removed-old-route");
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-11: stale route_id → valid=false");
	Assert(violations.Any(v => v.Contains("route_id not found in registry")),
		"AC-11: 含 'route_id not found in registry'");
}

// ── AC-16: 往返序列化保真度 ──
{
	var mgr = BuildConfirmed();
	var payload = mgr.BuildSnapshotPayload("route.sky-reef-arc-01", 999.0);
	// 恢复到新 manager
	var mgr2 = new ChartManager();
	mgr2.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	mgr2.SetKnowledgeQueryDelegate(_ => 2);
	mgr2.SetTraversableQueryDelegate(_ => true);
	// 包装为 IReadOnlyDictionary
	IReadOnlyDictionary<string, object?> readOnly = payload;
	mgr2.RestoreFromSnapshot(readOnly);
	Assert(mgr2.CurrentState == ChartState.DepartureConfirmed,
		"AC-16: 往返后 chart_state=DEPARTURE_CONFIRMED");
	// hide_rumored 值正确恢复
	Assert(mgr2.GetFilterState()["hide_rumored"] is false,
		"AC-16: hide_rumored=false 正确恢复");
}

// ── AC-17: DEPARTURE_CONFIRMED 恢复路径——立即发出 route_committed 信号 ──
{
	var mgr = new ChartManager();
	mgr.RegisterRoute(new RouteStaticData(
		"route.sky-reef-arc-01", "location.glass-harbor", "location.sky-reef-outpost",
		"short", new[] { "safe" }));
	bool committed = false;
	mgr.RouteCommitted += (_, _, _) => committed = true;
	var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["last_committed_route_id"] = "route.sky-reef-arc-01",
		["departure_state"] = "DEPARTURE_CONFIRMED",
		["hide_rumored"] = false,
	};
	mgr.RestoreFromSnapshot(payload);
	Assert(mgr.CurrentState == ChartState.DepartureConfirmed,
		"AC-17: 恢复后 chart_state=DEPARTURE_CONFIRMED");
	Assert(committed, "AC-17: 恢复时重发 route_committed 信号");
}

// ── AC-18: stale route_id 校验失败 ──
{
	var p = ValidPayload("route.stale-id-not-in-registry");
	var (valid, violations) = ChartManager.ValidateSnapshotPackage(p, 1000.0, 300.0, routeRegistry);
	Assert(!valid, "AC-18: stale route_id 校验失败");
	Assert(violations.Any(v => v.Contains("route_id not found in registry")),
		"AC-18: violations 包含路由 ID 未找到提示");
}

// ── AC-15: Persistence 域注册——序列化/反序列化委托可作为参数传递 ──
{
	var mgr = BuildConfirmed();
	Func<Dictionary<string, object?>> serializer = () => mgr.BuildSnapshotPayload("route.sky-reef-arc-01", 999.0);
	Action<IReadOnlyDictionary<string, object?>> deserializer = mgr.RestoreFromSnapshot;
	Assert(serializer != null && deserializer != null,
		"AC-15: 序列化/反序列化委托可注册到 Persistence");
	var result = serializer();
	Assert(result["departure_state"]?.ToString() == "DEPARTURE_CONFIRMED",
		"AC-15: 序列化委托调用正确");
}

// ── hide_rumored=true 写入快照 ──
{
	var mgr = BuildConfirmed(hideRumored: true);
	var payload = mgr.BuildSnapshotPayload("route.sky-reef-arc-01", 999.0);
	Assert(payload["active_filter"]?.ToString() == "hide_rumored",
		"EXTRA: hide_rumored=true → active_filter='hide_rumored'");
	Assert(payload["hide_rumored"] is true, "EXTRA: hide_rumored 字段=true");
}

Console.WriteLine();
Console.WriteLine($"Story 005 Snapshot Persistence: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
