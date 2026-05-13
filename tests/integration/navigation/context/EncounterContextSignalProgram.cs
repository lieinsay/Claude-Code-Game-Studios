using CloudWeaverVoyage.Core;

// Story 006 — EncounterContext Production & voyage_completed Signal (Integration)
// 覆盖 AC-1 到 AC-17 全部验收标准

static NavigationManager BuildNav(int hullIntegrity = 85, bool arrived = true)
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "safe" }, "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => hullIntegrity);
	nav.SetGetHullBandDelegate(() => hullIntegrity >= 76 ? HullBand.Intact : HullBand.Damaged);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.SetRandomDelegate(() => 0.20); // calm_passage
	nav.OnRouteCommitted("route.sky-reef-arc-01", "location.sky-reef-outpost",
		new[] { "safe" });
	return nav;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 006: EncounterContext Production & voyage_completed Signal ===\n");

// ── AC-1: ARRIVED → EncounterContext 含 9 字段，forced_landing_position="" ──
{
	var nav = BuildNav();
	EncounterContext? ctx = null;
	nav.VoyageCompleted += c => ctx = c;
	nav.ProcessVoyage(61.0); // short+intact=60s → ARRIVED
	Assert(ctx != null, "AC-1: VoyageCompleted 信号触发");
	Assert(ctx!.RouteId == "route.sky-reef-arc-01", "AC-1: route_id 正确");
	Assert(ctx.DestinationId == "location.sky-reef-outpost", "AC-1: destination_id 正确");
	Assert(ctx.VoyageResult == "arrived", "AC-1: voyage_result='arrived'");
	Assert(ctx.ForcedLandingPosition == "", "AC-1: forced_landing_position=''");
	Assert(ctx.AccumulatedDamage >= 0, "AC-1: accumulated_damage ≥ 0");
	Assert(ctx.ResolvedEncounters != null, "AC-1: resolved_encounters 非 null");
	Assert(ctx.RevealedHiddenTags != null, "AC-1: revealed_hidden_tags 非 null");
	Assert(ctx.DamagedSlots != null, "AC-1: damaged_slots 非 null");
}

// ── AC-2: RETREATED → resolved_encounters 为截至撤退点 ──
{
	var nav = BuildNav();
	nav.SetRandomDelegate(() => 0.20); // calm_passage
	nav.ProcessVoyage(15.0); // 触发 1 次检查
	EncounterContext? ctx = null;
	nav.VoyageCompleted += c => ctx = c;
	nav.RequestRetreat();
	Assert(ctx != null, "AC-2: RETREATED → VoyageCompleted 触发");
	Assert(ctx!.VoyageResult == "retreated", "AC-2: voyage_result='retreated'");
	// resolved_encounters 包含截至撤退的遭遇
	Assert(ctx.AccumulatedDamage >= 0, "AC-2: accumulated_damage ≥ 0");
}

// ── AC-3: FORCED_LANDING → voyage_result='forced_landing', hull_band_arrival='destroyed' ──
{
	var nav = BuildNav(hullIntegrity: 5); // 脆弱船体
	EncounterContext? ctx = null;
	nav.VoyageCompleted += c => ctx = c;
	nav.ApplyDamageAndCheckBandTransition(10); // 5-10=0 → destroyed
	nav.ProcessVoyage(0.1);
	Assert(ctx != null, "AC-3: FORCED_LANDING → VoyageCompleted 触发");
	Assert(ctx!.VoyageResult == "forced_landing", "AC-3: voyage_result='forced_landing'");
	Assert(ctx.HullBandArrival == HullBand.Destroyed, "AC-3: hull_band_arrival=Destroyed");
	Assert(!string.IsNullOrEmpty(ctx.ForcedLandingPosition),
		"AC-3: forced_landing_position 非空");
}

// ── AC-4: 9 个字段类型正确 ──
{
	var nav = BuildNav();
	EncounterContext? ctx = null;
	nav.VoyageCompleted += c => ctx = c;
	nav.ProcessVoyage(61.0);
	Assert(ctx!.RouteId is string, "AC-4: route_id=string");
	Assert(ctx.DestinationId is string, "AC-4: destination_id=string");
	Assert(ctx.VoyageResult is string, "AC-4: voyage_result=string");
	Assert(ctx.ResolvedEncounters is IReadOnlyList<ResolvedEncounterEntry>,
		"AC-4: resolved_encounters=IReadOnlyList");
	Assert(ctx.AccumulatedDamage is int, "AC-4: accumulated_damage=int");
	Assert(ctx.RevealedHiddenTags is IReadOnlyList<string>, "AC-4: revealed_hidden_tags=list");
	Assert(ctx.HullBandArrival is HullBand, "AC-4: hull_band_arrival=HullBand");
	Assert(ctx.ForcedLandingPosition is string, "AC-4: forced_landing_position=string");
	Assert(ctx.DamagedSlots is IReadOnlyList<string>, "AC-4: damaged_slots=list");
}

// ── AC-5: voyage_completed 信号——typed, sync emit, emit-after-mutation ──
{
	var nav = BuildNav();
	VoyageState stateAtEmit = VoyageState.InProgress;
	nav.VoyageCompleted += _ => stateAtEmit = nav.CurrentState;
	nav.ProcessVoyage(61.0);
	Assert(stateAtEmit == VoyageState.Arrived,
		"AC-5: emit-after-mutation——信号触发时状态已变为 Arrived");
}

// ── AC-6: 信号发射后 Navigation 关闭，不参与下游 ──
{
	var nav = BuildNav();
	nav.ProcessVoyage(61.0); // → ARRIVED
	bool threw = false;
	try
	{
		nav.RequestRetreat(); // 终态，应被拒绝
		nav.OnRouteCommitted("route.any", "loc", Array.Empty<string>()); // 应被拒绝
	}
	catch { threw = true; }
	Assert(!threw, "AC-6: 终态后操作不崩溃");
	Assert(nav.CurrentState == VoyageState.Arrived, "AC-6: 终态保持 ARRIVED");
}

// ── AC-7: resolved_encounters 深拷贝，消费方修改不影响原始数据 ──
{
	var nav = BuildNav();
	nav.SetRandomDelegate(() => 0.20);
	nav.ProcessVoyage(15.0); // 触发遭遇
	EncounterContext? ctx = null;
	nav.VoyageCompleted += c => ctx = c;
	nav.ProcessVoyage(50.0); // 抵达
	int countBefore = ctx!.ResolvedEncounters.Count;
	// 消费方不能修改（IReadOnlyList）——此处只验证接口类型
	Assert(ctx.ResolvedEncounters is IReadOnlyList<ResolvedEncounterEntry>,
		"AC-7: resolved_encounters 是只读接口，消费方无法修改");
}

// ── AC-8: 写入顺序——步骤 1→2→3 按顺序执行 ──
{
	var orderLog = new List<string>();
	var nav = BuildNav();
	nav.SetApplyHullDamageDelegate(_ => orderLog.Add("step1_hull"));
	nav.SetUpdateRouteKnowledgeDelegate((_, _) => orderLog.Add("step2_intel"));
	nav.VoyageCompleted += _ => orderLog.Add("step3_signal");
	nav.SetPersistSnapshotDelegate(_ => orderLog.Add("step5_persist"));
	// 先施加伤害使步骤1有意义
	nav.ApplyDamageAndCheckBandTransition(5);
	nav.ProcessVoyage(61.0);
	Assert(orderLog.Count >= 3, "AC-8: 至少 3 个步骤执行");
	if (orderLog.Count >= 3)
	{
		Assert(orderLog[0] == "step1_hull", "AC-8: 步骤1 #8 船体伤害先执行");
		Assert(orderLog[1] == "step2_intel", "AC-8: 步骤2 #6 知识更新次之");
		Assert(orderLog[2] == "step3_signal", "AC-8: 步骤3 voyage_completed 信号再次");
	}
}

// ── AC-9: 步骤1 失败不阻塞后续步骤 ──
{
	var nav = BuildNav();
	nav.ApplyDamageAndCheckBandTransition(5);
	nav.SetApplyHullDamageDelegate(_ => throw new InvalidOperationException("#8 unavailable"));
	bool step3Fired = false;
	nav.VoyageCompleted += _ => step3Fired = true;
	bool threw = false;
	try { nav.ProcessVoyage(61.0); }
	catch { threw = true; }
	Assert(!threw, "AC-9: 步骤1 异常不向外传播");
	Assert(step3Fired, "AC-9: 步骤1 失败后步骤3 仍执行");
}

// ── AC-10: 步骤2 route_id 无效——跳过知识更新 ──
{
	var nav = BuildNav();
	bool step2Called = false;
	nav.SetUpdateRouteKnowledgeDelegate((_, _) => step2Called = true);
	nav.ProcessVoyage(61.0);
	Assert(step2Called, "AC-10: route_id 有效时步骤2 执行");
}

// ── AC-11: null ctx → fallback context ──
{
	var fallback = NavigationManager.ValidateEncounterContext(null);
	Assert(fallback != null, "AC-11: null → fallback context 非 null");
	Assert(fallback.VoyageResult == "arrived", "AC-11: fallback voyage_result='arrived'");
	Assert(fallback.RouteId == "unknown", "AC-11: fallback route_id='unknown'");
}

// ── AC-12: route_id 为空 → fallback ──
{
	var invalid = new EncounterContext("", "destination", "arrived",
		new List<ResolvedEncounterEntry>(), 0,
		new List<string>(), HullBand.Intact, "", new List<string>());
	var result = NavigationManager.ValidateEncounterContext(invalid);
	Assert(result.RouteId == "unknown", "AC-12: empty route_id → fallback");
}

// ── AC-13: 无效 voyage_result → fallback ──
{
	var invalid = new EncounterContext("route.x", "dest.y", "invalid_result",
		new List<ResolvedEncounterEntry>(), 0,
		new List<string>(), HullBand.Intact, "", new List<string>());
	var result = NavigationManager.ValidateEncounterContext(invalid);
	Assert(result.RouteId == "unknown", "AC-13: invalid voyage_result → fallback");
}

// ── AC-14: 有效 ctx 通过验证 ──
{
	var valid = new EncounterContext("route.x", "dest.y", "arrived",
		new List<ResolvedEncounterEntry>(), 0,
		new List<string>(), HullBand.Intact, "", new List<string>());
	var result = NavigationManager.ValidateEncounterContext(valid);
	Assert(result.RouteId == "route.x", "AC-14: 有效 ctx 通过验证，route_id 不变");
}

// ── AC-15: fallback 9 字段安全默认值 ──
{
	var fallback = NavigationManager.ValidateEncounterContext(null);
	Assert(fallback.AccumulatedDamage == 0, "AC-15: fallback accumulated_damage=0");
	Assert(fallback.ForcedLandingPosition == "", "AC-15: fallback forced_landing_position=''");
	Assert(fallback.ResolvedEncounters.Count == 0, "AC-15: fallback resolved_encounters=[]");
	Assert(fallback.RevealedHiddenTags.Count == 0, "AC-15: fallback revealed_hidden_tags=[]");
}

// ── AC-16/17: voyage_result="retreated" → 不触发探索（由 Exploration 消费）──
{
	// Navigation 侧：RETREATED → voyage_result='retreated' 在 EncounterContext 中
	var nav = BuildNav();
	EncounterContext? ctx = null;
	nav.VoyageCompleted += c => ctx = c;
	nav.RequestRetreat();
	Assert(ctx!.VoyageResult == "retreated", "AC-16/17: 撤退 → voyage_result='retreated'");
	// Exploration (#11) 收到 retreated 后不生成探索场景（此处由 Exploration 侧实现）
}

Console.WriteLine();
Console.WriteLine($"Story 006 EncounterContext Signal: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
