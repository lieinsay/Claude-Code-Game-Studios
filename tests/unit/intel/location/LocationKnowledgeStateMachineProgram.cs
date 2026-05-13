using CloudWeaverVoyage.Core;

// Story 002 — Location Knowledge State Machine & Rumor System
// 覆盖 AC-1 到 AC-20 全部验收标准

static IntelManager BuildManager()
{
	var mgr = new IntelManager();
	// AC-1: 初始 sky-reef-arc-01 → Identified
	mgr.SeedLocationKnowledge("route.sky-reef-arc-01", LocationKnowledgeState.Identified, "空港基础航图");
	// AC-2: 初始 high-risk-mvp → Rumored
	mgr.SeedLocationKnowledge("route.high-risk-mvp", LocationKnowledgeState.Rumored, "旧港务员");
	// AC-3: 初始 glass-harbor → Identified
	mgr.SeedLocationKnowledge("location.glass-harbor", LocationKnowledgeState.Identified, "空港基础航图");
	mgr.Initialize();
	return mgr;
}

int pass = 0, fail = 0;

void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 002: Location Knowledge State Machine & Rumor System ===\n");

// ── AC-1: 初始 sky-reef-arc-01 → Identified ──
{
	var mgr = BuildManager();
	var snap = mgr.QueryLocationSnapshot("route.sky-reef-arc-01");
	Assert(snap.State == LocationKnowledgeState.Identified, "AC-1: sky-reef-arc-01 初始为 Identified");
	Assert(snap.RumorSources.Any(s => s.SourceTag == "空港基础航图"), "AC-1: 来源含 '空港基础航图'");
}

// ── AC-2: 初始 high-risk-mvp → Rumored ──
{
	var mgr = BuildManager();
	Assert(mgr.QueryKnowledgeState("route.high-risk-mvp") == LocationKnowledgeState.Rumored,
		"AC-2: high-risk-mvp 初始为 Rumored");
}

// ── AC-3: 初始 glass-harbor → Identified ──
{
	var mgr = BuildManager();
	Assert(mgr.QueryKnowledgeState("location.glass-harbor") == LocationKnowledgeState.Identified,
		"AC-3: glass-harbor 初始为 Identified");
}

// ── AC-4: 未初始化地点 → Unknown ──
{
	var mgr = BuildManager();
	Assert(mgr.QueryKnowledgeState("location.uncharted-isle") == LocationKnowledgeState.Unknown,
		"AC-4: 未初始化地点返回 Unknown");
}

// ── AC-5: confidence=40 → Unknown → Rumored ──
{
	var mgr = BuildManager();
	string? sigLocation = null; string? sigSource = null;
	mgr.RumorReceived += (loc, src) => { sigLocation = loc; sigSource = src; };
	mgr.RevealRumor("location.test-isle", "old-harbormaster", new[] { "rocks" }, 40);
	Assert(mgr.QueryKnowledgeState("location.test-isle") == LocationKnowledgeState.Rumored,
		"AC-5: confidence=40 → Rumored");
	Assert(sigLocation == "location.test-isle", "AC-5: RumorReceived 信号触发");
	var snap = mgr.QueryLocationSnapshot("location.test-isle");
	Assert(snap.RumorSources[0].ConfidenceLabel == "可靠", "AC-5: confidence=40 → '可靠'");
}

// ── AC-6: confidence=25 → Rumored，标注 "不确定" ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.test-isle-b", "merchant", new[] { "fog" }, 25);
	Assert(mgr.QueryKnowledgeState("location.test-isle-b") == LocationKnowledgeState.Rumored,
		"AC-6: confidence=25 → Rumored");
	var snap = mgr.QueryLocationSnapshot("location.test-isle-b");
	Assert(snap.RumorSources[0].ConfidenceLabel == "不确定", "AC-6: confidence=25 → '不确定'");
}

// ── AC-7: confidence=75 (≥67) → Unknown 直跳 Identified ──
{
	var mgr = BuildManager();
	string? advLoc = null;
	LocationKnowledgeState? advOld = null, advNew = null;
	mgr.KnowledgeAdvanced += (loc, o, n) => { advLoc = loc; advOld = o; advNew = n; };
	mgr.RevealRumor("location.authority-isle", "navy-chart", Array.Empty<string>(), 75);
	Assert(mgr.QueryKnowledgeState("location.authority-isle") == LocationKnowledgeState.Identified,
		"AC-7: confidence=75 → Identified（跳过 Rumored）");
	Assert(advOld == LocationKnowledgeState.Unknown, "AC-7: KnowledgeAdvanced old=Unknown");
	Assert(advNew == LocationKnowledgeState.Identified, "AC-7: KnowledgeAdvanced new=Identified");
}

// ── AC-8: Rumored + confidence=80 → Identified，来源保留 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.rumored-bay", "old-harbormaster", new[] { "fog" }, 40); // → Rumored
	mgr.RevealRumor("location.rumored-bay", "navy-chart", Array.Empty<string>(), 80);  // → Identified
	Assert(mgr.QueryKnowledgeState("location.rumored-bay") == LocationKnowledgeState.Identified,
		"AC-8: Rumored + confidence=80 → Identified");
	var snap = mgr.QueryLocationSnapshot("location.rumored-bay");
	Assert(snap.RumorSources.Count == 2, "AC-8: 两个来源均保留");
}

// ── AC-9: ConsumeIntel Unknown → Identified ──
{
	var mgr = BuildManager();
	var result = mgr.AdvanceLocationFromIntel("location.new-isle");
	Assert(result.HasValue, "AC-9: ConsumeIntel 返回有效变化");
	Assert(result!.Value.OldState == LocationKnowledgeState.Unknown, "AC-9: 旧状态 Unknown");
	Assert(result!.Value.NewState == LocationKnowledgeState.Identified, "AC-9: 新状态 Identified");
	Assert(mgr.QueryKnowledgeState("location.new-isle") == LocationKnowledgeState.Identified,
		"AC-9: 地点状态已变为 Identified");
}

// ── AC-10: ConsumeIntel Rumored → Identified ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.rumored-place", "src", Array.Empty<string>(), 40); // → Rumored
	var result = mgr.AdvanceLocationFromIntel("location.rumored-place");
	Assert(result.HasValue, "AC-10: Rumored ConsumeIntel 返回有效变化");
	Assert(mgr.QueryKnowledgeState("location.rumored-place") == LocationKnowledgeState.Identified,
		"AC-10: Rumored → Identified");
}

// ── AC-11: ConsumeIntel Identified → 无变化，返回 null ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.auth-place", "navy", Array.Empty<string>(), 80); // → Identified
	var result = mgr.AdvanceLocationFromIntel("location.auth-place");
	Assert(!result.HasValue, "AC-11: Identified ConsumeIntel 返回 null");
	Assert(mgr.QueryKnowledgeState("location.auth-place") == LocationKnowledgeState.Identified,
		"AC-11: 状态保持 Identified");
}

// ── AC-12: ConsumeIntel Verified → 无变化，返回 null ──
{
	var mgr = BuildManager();
	mgr.PlayerArrivedAt("location.visited-place"); // → Verified
	var result = mgr.AdvanceLocationFromIntel("location.visited-place");
	Assert(!result.HasValue, "AC-12: Verified ConsumeIntel 返回 null");
	Assert(mgr.QueryKnowledgeState("location.visited-place") == LocationKnowledgeState.Verified,
		"AC-12: 状态保持 Verified");
}

// ── AC-13: player_arrived_at Identified → Verified ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.destination", "navy", Array.Empty<string>(), 80); // → Identified
	string? advLoc = null; LocationKnowledgeState? advNew = null;
	mgr.KnowledgeAdvanced += (loc, _, n) => { advLoc = loc; advNew = n; };
	mgr.PlayerArrivedAt("location.destination");
	Assert(mgr.QueryKnowledgeState("location.destination") == LocationKnowledgeState.Verified,
		"AC-13: Identified → Verified");
	Assert(advNew == LocationKnowledgeState.Verified, "AC-13: KnowledgeAdvanced 信号 new=Verified");
	var snap = mgr.QueryLocationSnapshot("location.destination");
	Assert(snap.RumorSources.Any(s => s.SourceTag == "亲身探索"),
		"AC-13: 来源含 '亲身探索'");
}

// ── AC-14: 开拓者路径 Unknown → Verified（跳过 Rumored/Identified）──
{
	var mgr = BuildManager();
	mgr.PlayerArrivedAt("location.uncharted-isle");
	Assert(mgr.QueryKnowledgeState("location.uncharted-isle") == LocationKnowledgeState.Verified,
		"AC-14: Unknown 直跳 Verified（开拓者路径）");
}

// ── AC-15: 已 Verified 再次 player_arrived_at → 静默忽略，不重复发信号 ──
{
	var mgr = BuildManager();
	mgr.PlayerArrivedAt("location.visited");
	int signalCount = 0;
	mgr.KnowledgeAdvanced += (_, _, _) => signalCount++;
	mgr.PlayerArrivedAt("location.visited");
	Assert(mgr.QueryKnowledgeState("location.visited") == LocationKnowledgeState.Verified,
		"AC-15: 重复 player_arrived_at 状态保持 Verified");
	Assert(signalCount == 0, "AC-15: 不重复发出 KnowledgeAdvanced 信号");
}

// ── AC-16: Verified 状态下 reveal_rumor 被静默拒绝 ──
{
	var mgr = BuildManager();
	mgr.PlayerArrivedAt("location.verified-bay");
	int rumCount = 0;
	mgr.RumorReceived += (_, _) => rumCount++;
	mgr.RevealRumor("location.verified-bay", "stranger", new[] { "rocks" }, 30);
	Assert(mgr.QueryKnowledgeState("location.verified-bay") == LocationKnowledgeState.Verified,
		"AC-16: Verified 状态 reveal_rumor 被拒绝，状态不变");
	Assert(rumCount == 0, "AC-16: RumorReceived 信号未触发");
}

// ── AC-17: Identified 状态下低置信度 reveal_rumor 不退回 Rumored ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.ident-bay", "navy", Array.Empty<string>(), 80); // → Identified
	mgr.RevealRumor("location.ident-bay", "stranger", new[] { "fog" }, 30);   // 低置信度，不降级
	Assert(mgr.QueryKnowledgeState("location.ident-bay") == LocationKnowledgeState.Identified,
		"AC-17: Identified 不退回 Rumored");
}

// ── AC-18: Rumored 不退回 Unknown ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.rumored-x", "src", Array.Empty<string>(), 40); // → Rumored
	// 状态机没有退回机制，直接验证
	Assert(mgr.QueryKnowledgeState("location.rumored-x") == LocationKnowledgeState.Rumored,
		"AC-18: Rumored 不可退回 Unknown");
}

// ── AC-19: 同一 source_tag 不追加重复条目 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.harbor-z", "old-harbormaster", new[] { "rocks" }, 55);
	mgr.RevealRumor("location.harbor-z", "old-harbormaster", new[] { "fog" }, 80);
	var snap = mgr.QueryLocationSnapshot("location.harbor-z");
	Assert(snap.RumorSources.Count == 1, "AC-19: 同 source_tag 不追加重复，仍为 1 条来源");
	Assert(snap.State == LocationKnowledgeState.Rumored, "AC-19: 重复来源不推进状态");
}

// ── AC-20: confidence 文本映射 ──
{
	var r1 = new RumorSource("s", Array.Empty<string>(), 20);
	var r2 = new RumorSource("s", Array.Empty<string>(), 50);
	var r3 = new RumorSource("s", Array.Empty<string>(), 80);
	Assert(r1.ConfidenceLabel == "不确定", "AC-20: confidence=20 → '不确定'");
	Assert(r2.ConfidenceLabel == "可靠",   "AC-20: confidence=50 → '可靠'");
	Assert(r3.ConfidenceLabel == "权威",   "AC-20: confidence=80 → '权威'");

	// 边界值
	var r0  = new RumorSource("s", Array.Empty<string>(), 0);
	var r33 = new RumorSource("s", Array.Empty<string>(), 33);
	var r34 = new RumorSource("s", Array.Empty<string>(), 34);
	var r66 = new RumorSource("s", Array.Empty<string>(), 66);
	var r67 = new RumorSource("s", Array.Empty<string>(), 67);
	Assert(r0.ConfidenceLabel  == "不确定", "AC-20: confidence=0  → '不确定'");
	Assert(r33.ConfidenceLabel == "不确定", "AC-20: confidence=33 → '不确定'");
	Assert(r34.ConfidenceLabel == "可靠",   "AC-20: confidence=34 → '可靠'");
	Assert(r66.ConfidenceLabel == "可靠",   "AC-20: confidence=66 → '可靠'");
	Assert(r67.ConfidenceLabel == "权威",   "AC-20: confidence=67 → '权威'");

	// 置信度钳制
	var mgr = BuildManager();
	mgr.RevealRumor("location.clamp-test", "src-neg", Array.Empty<string>(), -10);
	mgr.RevealRumor("location.clamp-test2", "src-over", Array.Empty<string>(), 150);
	var snapNeg  = mgr.QueryLocationSnapshot("location.clamp-test");
	var snapOver = mgr.QueryLocationSnapshot("location.clamp-test2");
	Assert(snapNeg.RumorSources[0].Confidence  == 0,   "AC-20 EDGE: confidence=-10 钳制到 0");
	Assert(snapOver.RumorSources[0].Confidence == 100, "AC-20 EDGE: confidence=150 钳制到 100");
}

Console.WriteLine();
Console.WriteLine($"Story 002 Location State Machine: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
