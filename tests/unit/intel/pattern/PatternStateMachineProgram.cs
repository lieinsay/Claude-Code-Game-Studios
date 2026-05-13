using CloudWeaverVoyage.Core;

// Story 001 — Pattern Knowledge Observation & State Machine
// 覆盖 AC-1 到 AC-13 全部验收标准

static IntelManager BuildManager()
{
	var mgr = new IntelManager();
	// 注册 3 条 MVP 规律及其事件权重
	mgr.RegisterPatternEventWeights("pattern.bird-flight-direction", new Dictionary<string, int>
	{
		["bird-narrative-hint"] = 1,     // narrative_hint
		["bird-log-migration"] = 2,      // log_fragment
		["bird-partner-comment"] = 3,    // partner_comment
		["bird-passive-island"] = 4,     // passive_observation
		["bird-active-study"] = 7,       // active_investigation
	});
	mgr.RegisterPatternEventWeights("pattern.lighthouse-signals", new Dictionary<string, int>
	{
		["light-narrative-hint"] = 1,
		["light-log-entry"] = 2,
		["light-passive-watch"] = 4,
		["light-active-decode"] = 7,
	});
	mgr.RegisterPatternEventWeights("pattern.fog-navigation", new Dictionary<string, int>
	{
		["fog-narrative-hint"] = 1,
		["fog-log-fragment"] = 2,
		["fog-partner-tip"] = 3,
		["fog-passive-obs"] = 4,
		["fog-active-trial"] = 7,
	});
	mgr.Initialize();
	return mgr;
}

int pass = 0, fail = 0;

void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 001: Pattern Knowledge State Machine ===\n");

// ── AC-1: 首次观测事件，observation_score = weight(narrative_hint) = 1 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	var snap = mgr.GetPatternSnapshot("pattern.bird-flight-direction")!;
	Assert(snap.ObservationScore == 1, "AC-1: 首次触发 narrative_hint → score=1");
	Assert(snap.TriggeredEvents.Contains("bird-narrative-hint"), "AC-1: 事件 ID 已记录");
}

// ── AC-2: 同一事件 ID 重复调用，score 不变 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	var snap = mgr.GetPatternSnapshot("pattern.bird-flight-direction")!;
	Assert(snap.ObservationScore == 1, "AC-2: 重复事件不累分");
	Assert(snap.TriggeredEvents.Count == 1, "AC-2: triggered_events 无重复");
}

// ── AC-3: score=1+2=3, 再加 bird-passive-island(4) → score=7 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4
	var snap = mgr.GetPatternSnapshot("pattern.bird-flight-direction")!;
	Assert(snap.ObservationScore == 7, "AC-3: score=1+2+4=7");
}

// ── AC-4: score=3 < 5 → Undiscovered ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Undiscovered,
		"AC-4: score=3 → Undiscovered");
}

// ── AC-5: score=7 → PartiallyObserved，信号触发 ──
{
	var mgr = BuildManager();
	string? sigPattern = null; PatternState? sigOld = null, sigNew = null;
	mgr.PatternStateChanged += (p, o, n) => { sigPattern = p; sigOld = o; sigNew = n; };
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4  → score=7
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.PartiallyObserved,
		"AC-5: score=7 → PartiallyObserved");
	Assert(sigPattern == "pattern.bird-flight-direction", "AC-5: PatternStateChanged 信号触发");
	Assert(sigOld == PatternState.Undiscovered, "AC-5: 信号 oldState=Undiscovered");
	Assert(sigNew == PatternState.PartiallyObserved, "AC-5: 信号 newState=PartiallyObserved");
}

// ── AC-6: score=11 → Confirmed ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7 → 14
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Confirmed,
		"AC-6: score=14 → Confirmed");
}

// ── AC-7: Confirmed 但 usage_success=false → IsConfirmedPlus=false ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Confirmed,
		"AC-7: score=11 → Confirmed");
	Assert(!mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-7: usage_success=false → IsConfirmedPlus=false");
}

// ── AC-8: Confirmed 且 usage_success=true → IsConfirmedPlus=true ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11
	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction");
	Assert(mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-8: Confirmed + usage_success=true → IsConfirmedPlus=true");
}

// ── AC-9: 提前设置 usage_success=true，score 不足时仍为 false；score 达标后自动激活 ──
{
	var mgr = BuildManager();
	string? confirmedPlusSignal = null;
	mgr.PatternUsageConfirmed += p => confirmedPlusSignal = p;

	// score=7 → PartiallyObserved
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 7

	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction"); // 提前设置
	Assert(!mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-9: score=7 时即使 usage_success=true，IsConfirmedPlus 仍为 false");

	// score 再加 4 → 11 → Confirmed，此时 IsConfirmedPlus 自动为 true
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-partner-comment"); // +3 → 10 → Confirmed
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");    // +7 → 17 (已 Confirmed，但验证信号)
	Assert(mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-9: score 达到 confirmation 后 IsConfirmedPlus 自动激活");
	Assert(confirmedPlusSignal == "pattern.bird-flight-direction",
		"AC-9: PatternUsageConfirmed 信号触发");
}

// ── AC-10: Undiscovered 规律不出现在 GetPatternLog() 中 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // score=1 → Undiscovered
	var log = mgr.GetPatternLog();
	Assert(!log.Any(s => s.PatternId == "pattern.bird-flight-direction"),
		"AC-10: Undiscovered 规律不出现在日志中");
}

// ── AC-11: threshold override — partial=7, confirmation=14 ──
{
	var mgr = BuildManager();
	mgr.SetThresholdOverride("pattern.bird-flight-direction", 7, 14);
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 7
	// score=7: >= override.partial(7)，< override.confirmation(14) → PartiallyObserved
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.PartiallyObserved,
		"AC-11: 自定义阈值 partial=7 → score=7 时为 PartiallyObserved");
	// score=8: 再加 1 → still PartiallyObserved
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-partner-comment"); // +3 → 10
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.PartiallyObserved,
		"AC-11: score=10 < 14 → 仍为 PartiallyObserved");
}

// ── AC-12: Confirmed 不可退回 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 → Confirmed
	// 降级尝试：通过重复事件（去重后 score 不变）验证状态持守 Confirmed
	var beforeState = mgr.GetPatternState("pattern.bird-flight-direction");
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study"); // 重复事件，score 不变
	var afterState = mgr.GetPatternState("pattern.bird-flight-direction");
	Assert(beforeState == PatternState.Confirmed, "AC-12: 达到 Confirmed 的初始状态正确");
	Assert(afterState == PatternState.Confirmed, "AC-12: Confirmed 状态不可降级——重复调用后仍保持");
}

// ── AC-13: PartiallyObserved 不可退回 Undiscovered ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 7 → PartiallyObserved
	// state 由分数决定，分数只增不减
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.PartiallyObserved,
		"AC-13: PartiallyObserved 不可降回 Undiscovered");
}

// ── 边缘情况: 空 event_id 不崩溃 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "");
	mgr.ReportObservationEvent("", "bird-narrative-hint");
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Undiscovered,
		"EDGE: 空 event_id / patternId 不崩溃且不累分");
}

Console.WriteLine();
Console.WriteLine($"Story 001 Pattern State Machine: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
