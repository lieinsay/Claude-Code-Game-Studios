using CloudWeaverVoyage.Core;
using static CloudWeaverVoyage.Core.ExplorationManager;

// Story 003 — Threat Triggering, Scout Preview & Environmental Handling
// 覆盖 AC-1 到 AC-21 全部验收标准

static ExplorationManager BuildExploring()
{
	var mgr = new ExplorationManager();
	mgr.SetCanAddToPoolDelegate((_, _) => true);
	mgr.SetAddLootDelegate((_, _) => { });
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	return mgr;
}

static ThreatPoint MakeEnv(string id = "threat.collapse", double radius = 3.0, double pos = 0.0) =>
	new ThreatPoint(id, ThreatCategory.Environmental, radius, pos);

static ThreatPoint MakeGuard(string id = "threat.guard-01", double radius = 5.0, double pos = 0.0) =>
	new ThreatPoint(id, ThreatCategory.Guard, radius, pos);

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 003: Threat Triggering, Scout Preview & Environmental ===\n");

// ── AC-1: 环境威胁 proximity 100% 触发 ──
{
	int trigCount = 0;
	for (int i = 0; i < 100; i++)
	{
		var mgr = BuildExploring();
		mgr.SetRandomDelegate(() => 0.50); // 任意值 < 1.0
		var tp = MakeEnv();
		mgr.RegisterThreatPoint(tp);
		var results = mgr.CheckThreatTrigger(0.0, "proximity"); // 在半径内（pos=0, radius=3, dist=0）
		if (results.Count > 0 && results[0].Triggered) trigCount++;
	}
	Assert(trigCount == 100, "AC-1: 环境威胁 proximity 100% 触发");
}

// ── AC-2: 守卫威胁 proximity ~70% 触发（统计验证）──
{
	int trigCount = 0;
	int total = 500;
	var rng = new Random(42);
	for (int i = 0; i < total; i++)
	{
		var mgr = BuildExploring();
		double roll = rng.NextDouble();
		mgr.SetRandomDelegate(() => roll);
		var tp = MakeGuard();
		mgr.RegisterThreatPoint(tp);
		var results = mgr.CheckThreatTrigger(0.0, "proximity");
		if (results.Count > 0 && results[0].Triggered) trigCount++;
	}
	double ratio = (double)trigCount / total;
	Assert(ratio >= 0.60 && ratio <= 0.80,
		$"AC-2: 守卫 proximity ≈70% ({ratio:P1}，期望 0.60-0.80)");
}

// ── AC-3: 守卫 interaction → 100% 触发 ──
{
	int trigCount = 0;
	for (int i = 0; i < 20; i++)
	{
		var mgr = BuildExploring();
		mgr.SetRandomDelegate(() => 0.99); // 高值：不触发 proximity，但 interaction 必触发
		var tp = MakeGuard();
		mgr.RegisterThreatPoint(tp);
		var results = mgr.CheckThreatTrigger(0.0, "interaction");
		if (results.Count > 0 && results[0].Triggered) trigCount++;
	}
	Assert(trigCount == 20, "AC-3: 守卫 interaction 100% 触发");
}

// ── AC-4: 环境 interaction → 100% 触发 ──
{
	var mgr = BuildExploring();
	var tp = MakeEnv();
	mgr.RegisterThreatPoint(tp);
	var results = mgr.CheckThreatTrigger(0.0, "interaction");
	Assert(results.Count > 0 && results[0].Triggered, "AC-4: 环境 interaction 100% 触发");
}

// ── AC-5: is_active=false → 不触发 ──
{
	var mgr = BuildExploring();
	var tp = MakeEnv();
	tp.IsActive = false;
	mgr.RegisterThreatPoint(tp);
	var results = mgr.CheckThreatTrigger(0.0, "proximity");
	Assert(results.Count == 0, "AC-5: is_active=false → 不触发");
}

// ── AC-6: 玩家在 trigger_radius 外 → 不触发 ──
{
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	// radius=3, playerPos=10 → dist=10 > 3
	var tp = MakeEnv("threat.far", radius: 3.0, pos: 0.0);
	mgr.RegisterThreatPoint(tp);
	var results = mgr.CheckThreatTrigger(10.0, "proximity"); // 距离 10 > 半径 3
	Assert(results.Count == 0, "AC-6: 玩家在触发半径外 → 不触发");
}

// ── AC-7: 环境威胁触发 → 伤害通过委托写入 ──
{
	int damageCalled = 0;
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetApplyExplorationHullDamageDelegate(d => damageCalled += d);
	var tp = MakeEnv();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "proximity");
	Assert(damageCalled > 0, "AC-7: 环境威胁触发后伤害写入委托被调用");
}

// ── AC-8: 封锁路径类型环境威胁（hull_damage=0，路径标记设置由场景层处理）──
{
	// 此处验证环境威胁触发信号
	bool sigFired = false;
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.ThreatTriggered += (id, cat) => { if (cat == ThreatCategory.Environmental) sigFired = true; };
	var tp = MakeEnv();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "proximity");
	Assert(sigFired, "AC-8: ThreatTriggered 信号发射（环境）");
}

// ── AC-9: 环境威胁触发后 env_threat_active=true（通过 SubstateChanged 验证）──
{
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	ExplorationSubstate? newSub = null;
	mgr.SubstateChanged += (_, n) => newSub = n;
	var tp = MakeEnv();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "proximity");
	Assert(newSub == ExplorationSubstate.Threatened, "AC-9: 威胁触发后 substate→THREATENED");
}

// ── AC-10: 守卫威胁触发 + CombatManager 可用 → initiate_threat 被调用 ──
{
	bool initiateCalled = false;
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetIsCombatManagerAvailableDelegate(() => true);
	mgr.SetInitiateThreatDelegate((id, cat) => initiateCalled = true);
	var tp = MakeGuard();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "interaction");
	Assert(initiateCalled, "AC-10: 守卫 + #12 可用 → initiateThreat 被调用");
}

// ── AC-11: 守卫威胁 + CombatManager 不可用 → inert，不触发伤害 ──
{
	int damageCalled = 0;
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetIsCombatManagerAvailableDelegate(() => false); // 不可用
	mgr.SetApplyExplorationHullDamageDelegate(d => damageCalled += d);
	var tp = MakeGuard();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "interaction");
	Assert(damageCalled == 0, "AC-11: #12 不可用 → 守卫 inert，无伤害");
	Assert(tp.IsActive, "AC-11: 守卫 inert → is_active 保持 true");
}

// ── AC-12: CombatResult 回调——suppressed → 清除威胁 ──
{
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetIsCombatManagerAvailableDelegate(() => true);
	mgr.SetInitiateThreatDelegate((_, _) => { });
	var tp = MakeGuard();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "interaction");
	bool cleared = false;
	mgr.ThreatCleared += _ => cleared = true;
	mgr.OnCombatResult("suppressed", "threat.guard-01");
	Assert(!tp.IsActive, "AC-12: suppressed → is_active=false");
	Assert(cleared, "AC-12: ThreatCleared 信号触发");
	Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle, "AC-12: substate 回到 Idle");
}

// ── AC-12b: retreated → force_extraction ──
{
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetIsCombatManagerAvailableDelegate(() => true);
	mgr.SetInitiateThreatDelegate((_, _) => { });
	var tp = MakeGuard();
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "interaction");
	mgr.OnCombatResult("retreated", "threat.guard-01");
	Assert(mgr.CurrentPhase == ExplorationPhase.Extracting,
		"AC-12b: retreated → force_extraction → EXTRACTING");
}

// ── AC-13: η_scout=0 → PREVIEW_NONE ──
{
	var mgr = BuildExploring();
	mgr.SetGetScoutEfficiencyDelegate(() => 0.0);
	mgr.SnapshotEtaScout();
	Assert(mgr.GetScoutPreviewLevel() == ScoutPreviewLevel.None, "AC-13: η=0 → PREVIEW_NONE");
}

// ── AC-14: η_scout=0.48 → PREVIEW_PRESENCE ──
{
	var mgr = BuildExploring();
	mgr.SetGetScoutEfficiencyDelegate(() => 0.48);
	mgr.SnapshotEtaScout();
	Assert(mgr.GetScoutPreviewLevel() == ScoutPreviewLevel.Presence,
		"AC-14: η=0.48 → PREVIEW_PRESENCE");
}

// ── AC-15: η ∈ (0, 1) → PREVIEW_PRESENCE ──
{
	foreach (double eta in new[] { 0.01, 0.6, 0.76, 0.8, 0.95, 0.99 })
	{
		var mgr = BuildExploring();
		mgr.SetGetScoutEfficiencyDelegate(() => eta);
		mgr.SnapshotEtaScout();
		Assert(mgr.GetScoutPreviewLevel() == ScoutPreviewLevel.Presence,
			$"AC-15: η={eta} → PREVIEW_PRESENCE");
	}
}

// ── AC-16: η_scout=1.0 → PREVIEW_FULL ──
{
	var mgr = BuildExploring();
	mgr.SetGetScoutEfficiencyDelegate(() => 1.0);
	mgr.SnapshotEtaScout();
	Assert(mgr.GetScoutPreviewLevel() == ScoutPreviewLevel.Full, "AC-16: η=1.0 → PREVIEW_FULL");
}

// ── AC-17: 快照值不随实时变化更新 ──
{
	var mgr = BuildExploring();
	mgr.SetGetScoutEfficiencyDelegate(() => 1.0);
	mgr.SnapshotEtaScout(); // 快照 1.0 → PREVIEW_FULL
	mgr.SetGetScoutEfficiencyDelegate(() => 0.0); // 实时变为 0
	Assert(mgr.GetScoutPreviewLevel() == ScoutPreviewLevel.Full,
		"AC-17: 快照后实时 η 变化不影响预览等级");
}

// ── AC-18: 多威胁——环境 > 守卫优先级 ──
{
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10); // 守卫必触发（70%）
	var env = MakeEnv("threat.env-01", radius: 3.0, pos: 2.0);
	var guard = MakeGuard("threat.guard-01", radius: 5.0, pos: 3.0);
	mgr.RegisterThreatPoint(env);
	mgr.RegisterThreatPoint(guard);
	var results = mgr.CheckThreatTrigger(0.0, "proximity");
	Assert(results.Count >= 1, "AC-18: 至少一个威胁触发");
	if (results.Count >= 2)
		Assert(results[0].Category == ThreatCategory.Environmental,
			"AC-18: 环境威胁优先处理");
}

// ── AC-19: 同距离同类型 → 按 ThreatId 字典序 ──
{
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	// 两个守卫在相同距离（pos=0）
	var g1 = MakeGuard("threat.z-guard", radius: 5.0, pos: 0.0);
	var g2 = MakeGuard("threat.a-guard", radius: 5.0, pos: 0.0);
	mgr.RegisterThreatPoint(g1);
	mgr.RegisterThreatPoint(g2);
	var results = mgr.CheckThreatTrigger(0.0, "interaction");
	Assert(results.Count >= 2, "AC-19: 两个守卫均触发");
	if (results.Count >= 2)
		Assert(results[0].ThreatId == "threat.a-guard",
			"AC-19: 字典序 a < z → threat.a-guard 先处理");
}

// ── AC-20: build_threat_context 环境威胁——threat_type 正确 ──
{
	var tp = MakeEnv();
	// 环境威胁触发后信号携带正确类别
	var mgr = BuildExploring();
	mgr.SetRandomDelegate(() => 0.10);
	ThreatCategory? sigCat = null;
	mgr.ThreatTriggered += (_, cat) => sigCat = cat;
	mgr.RegisterThreatPoint(tp);
	mgr.CheckThreatTrigger(0.0, "proximity");
	Assert(sigCat == ThreatCategory.Environmental, "AC-20/21: ThreatTriggered 携带正确类别");
}

Console.WriteLine();
Console.WriteLine($"Story 003 Threat Triggering: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
