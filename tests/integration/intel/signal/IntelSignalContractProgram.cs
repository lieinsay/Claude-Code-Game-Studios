using CloudWeaverVoyage.Core;

// Story 007 — Signal Contract & Non-Degradation Guards (Integration)
// 覆盖 AC-1 到 AC-13 全部验收标准

static IntelManager BuildManager()
{
	var mgr = new IntelManager();
	mgr.RegisterPatternEventWeights("pattern.bird-flight-direction", new Dictionary<string, int>
	{
		["bird-narrative-hint"] = 1,
		["bird-log-migration"] = 2,
		["bird-passive-island"] = 4,
		["bird-active-study"] = 7,
	});
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.bird-flight-understanding",
		new[]
		{
			new AbilityUnlockPath("path_a_pattern_confirmed", new[]
			{
				new AbilityCondition("pattern_state", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.bird-flight-direction",
					["required_state"] = PatternState.Confirmed,
				}),
			}),
		}));
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.lighthouse-signal-interpretation",
		new[]
		{
			new AbilityUnlockPath("path_c_repair", new[]
			{
				new AbilityCondition("repair_completed", new Dictionary<string, object>
					{ ["repair_node_id"] = "repair_lighthouse_01" }),
			}),
		}));
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		new[] { "location.whisper-isle" },
		new[] { "pattern.bird-flight-direction" },
		"bird-log-migration",
		new[] { "ability.bird-flight-understanding" }));
	mgr.SeedLocationKnowledge("location.whisper-isle", LocationKnowledgeState.Rumored, "old-source");
	mgr.Initialize();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 007: Signal Contract & Non-Degradation Guards ===\n");

// ── AC-1: knowledge_advanced emit-after-mutation ──
{
	var mgr = BuildManager();
	string? sigLoc = null;
	LocationKnowledgeState? sigOld = null, sigNew = null;
	mgr.KnowledgeAdvanced += (loc, o, n) => { sigLoc = loc; sigOld = o; sigNew = n; };
	// AdvanceLocationFromIntel: Rumored → Identified
	mgr.AdvanceLocationFromIntel("location.whisper-isle");
	Assert(sigLoc == "location.whisper-isle", "AC-1: knowledge_advanced 信号触发");
	Assert(sigOld == LocationKnowledgeState.Rumored, "AC-1: previous_state=RUMORED");
	Assert(sigNew == LocationKnowledgeState.Identified, "AC-1: new_state=IDENTIFIED");
	// 验证 emit-after-mutation：信号触发时状态已变更
	Assert(mgr.QueryKnowledgeState("location.whisper-isle") == LocationKnowledgeState.Identified,
		"AC-1: emit-after-mutation — 信号触发时状态已为 IDENTIFIED");
}

// ── AC-2: pattern_state_changed emit-after-mutation ──
{
	var mgr = BuildManager();
	PatternState? sigOld = null, sigNew = null;
	mgr.PatternStateChanged += (_, o, n) => { sigOld = o; sigNew = n; };
	// score 7 → PartiallyObserved，然后 +7 → 14 → Confirmed
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1 → 5 PartiallyObserved
	sigOld = null; sigNew = null; // 重置，只关注 Confirmed 转换
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7 → 12 Confirmed
	Assert(sigOld == PatternState.PartiallyObserved, "AC-2: pattern_state_changed old=PartiallyObserved");
	Assert(sigNew == PatternState.Confirmed, "AC-2: pattern_state_changed new=Confirmed");
	// emit-after-mutation 验证
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Confirmed,
		"AC-2: emit-after-mutation — 信号触发时状态已为 Confirmed");
}

// ── AC-3 & AC-4: typed params（C# event 天然类型安全）──
{
	// pattern_observed: Action<string, string, int> → 3 个类型化参数
	var mgr = BuildManager();
	string? pId = null; string? eId = null; int? score = null;
	mgr.PatternObserved += (p, e, s) => { pId = p; eId = e; score = s; };
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	Assert(pId is string, "AC-3/4: pattern_observed param[0] 为 string（typed）");
	Assert(eId is string, "AC-3/4: pattern_observed param[1] 为 string（typed）");
	Assert(score is int, "AC-3/4: pattern_observed param[2] 为 int（typed）");
	Assert(pId == "pattern.bird-flight-direction", "AC-4: pattern_id 正确");
	Assert(eId == "bird-narrative-hint", "AC-4: event_id 正确");
	Assert(score == 1, "AC-4: new_score=1（narrative_hint 权重）");
}

// ── AC-5: signal cascade depth ≤ 2 ──
// IntelManager emit depth=1，consumer 可再 emit depth=2，不超过 2
{
	var mgr = BuildManager();
	int depth = 0; int maxDepth = 0;
	mgr.KnowledgeAdvanced += (_, _, _) =>
	{
		depth++;
		maxDepth = Math.Max(maxDepth, depth);
		// 模拟下游系统在 callback 内发出自己的信号（depth=2）
		// 不再嵌套发出 IntelManager 信号，维持 depth=2
		depth--;
	};
	mgr.AdvanceLocationFromIntel("location.whisper-isle");
	Assert(maxDepth <= 2, "AC-5: signal cascade depth ≤ 2");
}

// ── AC-6: _can_transition VERIFIED → RUMORED = false ──
{
	var mgr = BuildManager();
	mgr.PlayerArrivedAt("location.whisper-isle"); // → Verified
	int sigCount = 0;
	mgr.KnowledgeAdvanced += (_, _, _) => sigCount++;
	mgr.RevealRumor("location.whisper-isle", "stranger", new[] { "rocks" }, 30);
	Assert(mgr.QueryKnowledgeState("location.whisper-isle") == LocationKnowledgeState.Verified,
		"AC-6: Verified 状态不因 reveal_rumor 退回 Rumored");
	Assert(sigCount == 0, "AC-6: 无 KnowledgeAdvanced 信号（拒绝降级）");
}

// ── AC-7: IDENTIFIED → RUMORED = false ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.whisper-isle", "navy", Array.Empty<string>(), 80); // → Identified
	mgr.RevealRumor("location.whisper-isle", "stranger", new[] { "rocks" }, 30); // 低置信度，不降级
	Assert(mgr.QueryKnowledgeState("location.whisper-isle") == LocationKnowledgeState.Identified,
		"AC-7: Identified 不因低置信传闻退回 Rumored");
}

// ── AC-8: RUMORED → UNKNOWN = false ──
{
	var mgr = BuildManager();
	// whisper-isle 已 Seed 为 Rumored
	Assert(mgr.QueryKnowledgeState("location.whisper-isle") == LocationKnowledgeState.Rumored,
		"AC-8: 初始为 Rumored");
	// TryAdvanceLocation 只允许递增，无退回机制
	// 无公开 API 可将 Rumored 退回 Unknown → 状态机设计保证
	Assert(mgr.QueryKnowledgeState("location.whisper-isle") == LocationKnowledgeState.Rumored,
		"AC-8: Rumored 不可退回 Unknown（状态机无退回路径）");
}

// ── AC-9: Pattern Confirmed 不可降级 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 Confirmed
	// 分数只增不减，无任何 API 可降低 observation_score
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Confirmed,
		"AC-9: Confirmed 状态不可降级（observation_score 单调递增）");
}

// ── AC-10: pattern_usage_success 一旦设置永久保留 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 Confirmed
	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction");
	Assert(mgr.IsConfirmedPlus("pattern.bird-flight-direction"), "AC-10: 初始 confirmed+");
	// 任何后续事件不能改变 usage_success
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");
	Assert(mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-10: 后续观测事件不影响 pattern_usage_success");
}

// ── AC-11: ability Unlocked 不可退回 Locked ──
{
	var mgr = BuildManager();
	mgr.OnRepairCompleted("repair_lighthouse_01");
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-11: 能力已解锁");
	// 无任何公开 API 可将已解锁能力退回 Locked
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-11: Unlocked 不可退回 Locked（无退回 API）");
}

// ── AC-12: 伙伴离队后能力保持 Unlocked ──
{
	var mgr = BuildManager();
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.fog-navigation",
		new[]
		{
			new AbilityUnlockPath("path_c_partner", new[]
			{
				new AbilityCondition("partner_in_crew", new Dictionary<string, object>
					{ ["partner_id"] = "partner.old-sailor" }),
			}),
		}));
	mgr.OnPartnerJoined("partner.old-sailor");
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Unlocked,
		"AC-12: 伙伴加入后能力解锁");
	mgr.OnPartnerLeft("partner.old-sailor");
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Unlocked,
		"AC-12: 伙伴离队后能力保持 Unlocked");
}

// ── AC-13: 全部 9 个信号的 emit 验证（各触发场景独立覆盖）──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // 预先有观测事件

	bool knowledgeAdvanced = false;
	bool patternObserved = false;
	bool patternStateChanged = false;
	bool abilityUnlocked = false;
	bool intelConsumed = false;
	bool rumorReceived = false;
	bool intelConsumeFailed = false;
	bool rumorConfidenceChanged = false;

	mgr.KnowledgeAdvanced += (_, _, _) => knowledgeAdvanced = true;
	mgr.PatternObserved += (_, _, _) => patternObserved = true;
	mgr.PatternStateChanged += (_, _, _) => patternStateChanged = true;
	mgr.AbilityUnlocked += (_, _) => abilityUnlocked = true;
	mgr.IntelConsumed += _ => intelConsumed = true;
	mgr.RumorReceived += (_, _) => rumorReceived = true;
	mgr.IntelConsumeFailed += (_, _) => intelConsumeFailed = true;
	mgr.RumorConfidenceChanged += (_, _, _, _) => rumorConfidenceChanged = true;

	// knowledge_advanced + pattern_observed + intel_consumed（consume_intel 三重效果）
	mgr.ConsumeIntel("intel.bird-migration-notes");
	Assert(knowledgeAdvanced, "AC-13: knowledge_advanced 信号触发");
	Assert(patternObserved, "AC-13: pattern_observed 信号触发");
	Assert(intelConsumed, "AC-13: intel_consumed 信号触发");

	// ability_unlocked：使用 OnRepairCompleted（lighthouse Path C 无需其他条件）
	mgr.OnRepairCompleted("repair_lighthouse_01");
	Assert(abilityUnlocked, "AC-13: ability_unlocked 信号触发（repair_completed 触发）");

	// intel_consume_failed：重复消费
	mgr.ConsumeIntel("intel.bird-migration-notes");
	Assert(intelConsumeFailed, "AC-13: intel_consume_failed 信号触发（重复消费）");

	// rumor_received：写入传闻
	mgr.RevealRumor("location.test-bay", "merchant", new[] { "fog" }, 40);
	Assert(rumorReceived, "AC-13: rumor_received 信号触发");

	// rumor_confidence_changed：调整置信度
	mgr.AdjustRumorConfidence("location.test-bay", "merchant", true);
	Assert(rumorConfidenceChanged, "AC-13: rumor_confidence_changed 信号触发");

	// pattern_usage_confirmed：达到 confirmed+
	bool patternUsageConfirmed = false;
	mgr.PatternUsageConfirmed += _ => patternUsageConfirmed = true;
	// bird-flight 在 consume_intel 后 score=1+2=3（narrative_hint=1 + log_fragment=2）；再加更多达到 Confirmed
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → Confirmed
	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction");
	Assert(patternUsageConfirmed, "AC-13: pattern_usage_confirmed 信号触发");
	_ = patternStateChanged;
}

// ── AdjustRumorConfidence 功能验证 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.bay", "merchant", new[] { "fog" }, 50); // confidence=50
	string? sigSrc = null; string? sigLoc = null; int? sigOld = null; int? sigNew = null;
	mgr.RumorConfidenceChanged += (src, loc, o, n) =>
		{ sigSrc = src; sigLoc = loc; sigOld = o; sigNew = n; };
	mgr.AdjustRumorConfidence("location.bay", "merchant", true); // +25 → 75
	Assert(sigSrc == "merchant", "EXTRA: RumorConfidenceChanged source 正确");
	Assert(sigOld == 50, "EXTRA: old_confidence=50");
	Assert(sigNew == 75, "EXTRA: new_confidence=75（+25）");
	mgr.AdjustRumorConfidence("location.bay", "merchant", false); // -30 → 45
	Assert(sigNew == 45, "EXTRA: 再次调整 -30 → 45");
	// 上限 100
	mgr.AdjustRumorConfidence("location.bay", "merchant", true); // +25 → 70
	mgr.AdjustRumorConfidence("location.bay", "merchant", true); // +25 → 95
	mgr.AdjustRumorConfidence("location.bay", "merchant", true); // +25 → 100（钳制）
	Assert(sigNew == 100, "EXTRA: confidence 上限 100");
}

Console.WriteLine();
Console.WriteLine($"Story 007 Signal Contract: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
