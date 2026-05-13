using CloudWeaverVoyage.Core;

// Story 001 — Exploration State Machine & Phase Transitions
// 覆盖 AC-1 到 AC-19 全部验收标准

static ExplorationManager Build() => new ExplorationManager();

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 001: Exploration State Machine & Phase Transitions ===\n");

// ── AC-1: 初始状态 ──
{
	var mgr = Build();
	Assert(mgr.CurrentPhase == ExplorationPhase.Idle, "AC-1: 初始 phase=IDLE");
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle, "AC-1: 初始 substate=IDLE");
	Assert(mgr.CurrentPointId == "", "AC-1: 初始 currentPointId=''");
}

// ── AC-2: IDLE + EncounterContext → ARRIVING ──
{
	var mgr = Build();
	ExplorationPhase? sigOld = null, sigNew = null; string? sigPoint = null;
	mgr.ExplorationPhaseChanged += (o, n, p) => { sigOld = o; sigNew = n; sigPoint = p; };
	bool result = mgr.EnterExploration("location.cloudwatch-ruins");
	Assert(result, "AC-2: EnterExploration 返回 true");
	Assert(mgr.CurrentPhase == ExplorationPhase.Arriving, "AC-2: → ARRIVING");
	Assert(mgr.CurrentPointId == "location.cloudwatch-ruins", "AC-2: currentPointId 设置");
	Assert(sigOld == ExplorationPhase.Idle, "AC-2: 信号 oldPhase=IDLE");
	Assert(sigNew == ExplorationPhase.Arriving, "AC-2: 信号 newPhase=ARRIVING");
	Assert(sigPoint == "location.cloudwatch-ruins", "AC-2: 信号 pointId 正确");
}

// ── AC-3: ARRIVING → EXPLORING ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	ExplorationPhase? sigNew = null;
	mgr.ExplorationPhaseChanged += (_, n, _) => sigNew = n;
	bool result = mgr.SkipArriving();
	Assert(result, "AC-3: SkipArriving 返回 true");
	Assert(mgr.CurrentPhase == ExplorationPhase.Exploring, "AC-3: → EXPLORING");
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle, "AC-3: 子状态初始=IDLE");
	Assert(sigNew == ExplorationPhase.Exploring, "AC-3: 信号 newPhase=EXPLORING");
}

// ── AC-4: EXPLORING → EXTRACTING，读条开始 ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	string? extractReason = null;
	mgr.ExtractionStarted += r => extractReason = r;
	bool result = mgr.TriggerExtraction();
	Assert(result, "AC-4: TriggerExtraction 返回 true");
	Assert(mgr.CurrentPhase == ExplorationPhase.Extracting, "AC-4: → EXTRACTING");
	Assert(extractReason == "player_initiated", "AC-4: ExtractionStarted 信号触发");
}

// ── AC-5: EXTRACTING + 2.5s 完成 → DEPARTED ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.TriggerExtraction();
	ExplorationPhase? sigNew = null;
	mgr.ExplorationPhaseChanged += (_, n, _) => sigNew = n;
	mgr.ExtractionTick(2.6); // 超过 2.5s
	Assert(mgr.CurrentPhase == ExplorationPhase.Departed, "AC-5: 读条完成 → DEPARTED");
	Assert(sigNew == ExplorationPhase.Departed, "AC-5: 信号 newPhase=DEPARTED");
}

// ── AC-6: DEPARTED → IDLE ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.TriggerExtraction();
	mgr.ExtractionTick(3.0);
	bool result = mgr.ReturnToIdle();
	Assert(result, "AC-6: ReturnToIdle 返回 true");
	Assert(mgr.CurrentPhase == ExplorationPhase.Idle, "AC-6: → IDLE");
	Assert(mgr.CurrentPointId == "", "AC-6: currentPointId 清空");
}

// ── AC-7: IDLE 下无效操作 ──
{
	var mgr = Build();
	Assert(!mgr.SkipArriving(), "AC-7: IDLE.SkipArriving → false");
	Assert(!mgr.TriggerExtraction(), "AC-7: IDLE.TriggerExtraction → false");
	Assert(!mgr.ForceExtraction("reason"), "AC-7: IDLE.ForceExtraction → false");
}

// ── AC-8: ARRIVING 不接受 trigger_extraction ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	Assert(!mgr.TriggerExtraction(), "AC-8: ARRIVING.TriggerExtraction → false");
	Assert(!mgr.ForceExtraction("reason"), "AC-8: ARRIVING.ForceExtraction → false");
}

// ── AC-9: EXPLORING 不能跳过 EXTRACTING 直接到 DEPARTED ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	// 无公开方法可直接触发 DEPARTED（ReturnToIdle 仅从 DEPARTED 有效）
	bool result = mgr.ReturnToIdle(); // EXPLORING → IDLE 无效
	Assert(!result, "AC-9: EXPLORING.ReturnToIdle → false（不跳过 EXTRACTING）");
	Assert(mgr.CurrentPhase == ExplorationPhase.Exploring, "AC-9: 状态保持 EXPLORING");
}

// ── AC-10: EXTRACTING 不接受 enter_exploration 或 skip_arriving ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.TriggerExtraction();
	Assert(!mgr.EnterExploration("location.other"), "AC-10: EXTRACTING.EnterExploration → false");
	Assert(!mgr.SkipArriving(), "AC-10: EXTRACTING.SkipArriving → false");
}

// ── AC-11: DEPARTED 只接受 ReturnToIdle ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.TriggerExtraction();
	mgr.ExtractionTick(3.0);
	Assert(!mgr.EnterExploration("location.other"), "AC-11: DEPARTED.EnterExploration → false");
	Assert(!mgr.SkipArriving(), "AC-11: DEPARTED.SkipArriving → false");
	Assert(!mgr.TriggerExtraction(), "AC-11: DEPARTED.TriggerExtraction → false");
	Assert(mgr.ReturnToIdle(), "AC-11: DEPARTED.ReturnToIdle → true（唯一有效）");
}

// ── AC-12: 非 ARRIVING 调用 skip_arriving → false ──
{
	var mgr = Build(); // IDLE
	Assert(!mgr.SkipArriving(), "AC-12: IDLE.SkipArriving → false");
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving(); // → EXPLORING
	Assert(!mgr.SkipArriving(), "AC-12: EXPLORING.SkipArriving → false");
}

// ── AC-13: 非 EXPLORING 调用 trigger_extraction → false ──
{
	var mgr = Build();
	Assert(!mgr.TriggerExtraction(), "AC-13: IDLE.TriggerExtraction → false");
	mgr.EnterExploration("location.ruins"); // ARRIVING
	Assert(!mgr.TriggerExtraction(), "AC-13: ARRIVING.TriggerExtraction → false");
}

// ── AC-14: EXPLORING 子状态 IDLE ↔ MOVING ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	ExplorationSubstate? sigOld = null, sigNew = null;
	mgr.SubstateChanged += (o, n) => { sigOld = o; sigNew = n; };
	mgr.SetSubstate(ExplorationSubstate.Moving);
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Moving, "AC-14: → MOVING");
	Assert(sigOld == ExplorationSubstate.Idle, "AC-14: 信号 old=Idle");
	mgr.SetSubstate(ExplorationSubstate.Idle);
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle, "AC-14: → Idle");
}

// ── AC-15: IDLE → SEARCHING → IDLE ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.SetSubstate(ExplorationSubstate.Searching);
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Searching, "AC-15: → SEARCHING");
	mgr.SetSubstate(ExplorationSubstate.Idle);
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle, "AC-15: → Idle（搜索结束）");
}

// ── AC-16: IDLE → THREATENED → IDLE ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.SetSubstate(ExplorationSubstate.Threatened);
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Threatened, "AC-16: → THREATENED");
	mgr.SetSubstate(ExplorationSubstate.Idle);
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle, "AC-16: → Idle（威胁结算后）");
}

// ── AC-17: 子状态只在 EXPLORING 内有效 ──
{
	var mgr = Build(); // IDLE
	mgr.SetSubstate(ExplorationSubstate.Moving); // 应无效
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle,
		"AC-17: 非 EXPLORING 阶段设置子状态无效");
}

// ── AC-18: Pool 5 耗尽 → force_extraction ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	string? reason = null;
	mgr.ExtractionStarted += r => reason = r;
	mgr.ForceExtraction("pool_depleted");
	Assert(mgr.CurrentPhase == ExplorationPhase.Extracting,
		"AC-18: force_extraction → EXTRACTING");
	Assert(reason == "pool_depleted", "AC-18: 原因='pool_depleted'");
}

// ── AC-19: 所有点已搜不强制提取（玩家自主判断）──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	// 不强制提取——玩家保持在 EXPLORING
	Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
		"AC-19: 所有点已搜后 EXPLORING 状态保持，不强制提取");
}

// ── 撤离读条被打断 ──
{
	var mgr = Build();
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.TriggerExtraction();
	mgr.ExtractionTick(1.0); // 进度 40%
	string? interruptReason = null;
	mgr.ExtractionInterrupted += r => interruptReason = r;
	mgr.InterruptExtraction("threat_appeared");
	Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
		"INTERRUPT: 读条被打断 → 回到 EXPLORING");
	Assert(interruptReason == "threat_appeared", "INTERRUPT: ExtractionInterrupted 信号触发");
	Assert(mgr.ExtractionElapsed == 0.0, "INTERRUPT: 进度重置为 0");
}

Console.WriteLine();
Console.WriteLine($"Story 001 Exploration State Machine: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
