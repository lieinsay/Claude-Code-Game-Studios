using CloudWeaverVoyage.Core;

// Story 006 — Downstream Query Interface
// 覆盖 AC-1 到 AC-16 全部验收标准

static IntelManager BuildManager()
{
	var mgr = new IntelManager();

	// 注册规律
	mgr.RegisterPatternEventWeights("pattern.bird-flight-direction", new Dictionary<string, int>
	{
		["bird-narrative-hint"] = 1,
		["bird-log-migration"] = 2,
		["bird-passive-island"] = 4,
		["bird-active-study"] = 7,
	});
	mgr.RegisterPatternEventWeights("pattern.lighthouse-signals", new Dictionary<string, int>
	{
		["light-passive-watch"] = 4,
	});
	mgr.RegisterPatternEventWeights("pattern.fog-navigation", new Dictionary<string, int>
	{
		["fog-narrative-hint"] = 1,
	});

	// 注册路线静态定义
	mgr.RegisterRouteDefinition(new RouteDefinition(
		"route.sky-reef-arc-01",
		new[] { "rocks", "wind-shear" },
		"" // 无能力要求
	));
	mgr.RegisterRouteDefinition(new RouteDefinition(
		"route.lighthouse-approach",
		new[] { "fog", "hidden-reef" },
		"ability.lighthouse-signal-interpretation" // 需要灯塔能力
	));

	// 注册地点静态定义
	mgr.RegisterLocationDefinition(new LocationDefinition(
		"location.whisper-isle",
		new[] { "fog", "pirates" }
	));

	// 注册能力静态定义
	mgr.RegisterAbilityDefinition(new AbilityDefinition(
		"ability.bird-flight-understanding",
		"鸟类飞行规律",
		"你已掌握通过鸟群飞行方向判断风向和安全航线的技能。",
		"据说老水手们能从鸟群飞行中读出航路……"
	));
	mgr.RegisterAbilityDefinition(new AbilityDefinition(
		"ability.lighthouse-signal-interpretation",
		"灯塔信号解读",
		"你已学会解读古老灯塔的信号密码。",
		"灯塔守护者的后代或许能教你……"
	));

	// 注册能力路径配置（供 get_ability_list 使用）
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.bird-flight-understanding",
		new[]
		{
			new AbilityUnlockPath("path_a", new[]
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
			new AbilityUnlockPath("path_c", new[]
			{
				new AbilityCondition("repair_completed", new Dictionary<string, object>
					{ ["repair_node_id"] = "repair_lighthouse_01" }),
			}),
		}));

	// 初始化地点知识
	mgr.SeedLocationKnowledge("route.sky-reef-arc-01", LocationKnowledgeState.Identified, "空港基础航图");

	mgr.Initialize();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 006: Downstream Query Interface ===\n");

// ── AC-1: query_knowledge_state — RUMORED 地点 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.harbor-z", "old-harbormaster",
		new[] { "rocks" }, 40); // confidence=40 → Rumored
	var snap = mgr.QueryLocationSnapshot("location.harbor-z");
	Assert(snap.State == LocationKnowledgeState.Rumored, "AC-1: state=RUMORED");
	Assert(snap.RumorSources.Count == 1, "AC-1: rumor_sources 含 1 条");
	Assert(snap.RumorSources[0].Confidence == 40, "AC-1: confidence=40");
	Assert(snap.RumorSources[0].SourceTag == "old-harbormaster", "AC-1: source_tag 正确");
}

// ── AC-2: query_knowledge_state — VERIFIED 地点 ──
{
	var mgr = BuildManager();
	mgr.PlayerArrivedAt("location.visited-bay");
	mgr.SetPersonalNote("location.visited-bay", "这里有个隐藏的补给点");
	var snap = mgr.QueryLocationSnapshot("location.visited-bay");
	Assert(snap.State == LocationKnowledgeState.Verified, "AC-2: state=VERIFIED");
	var notes = mgr.QueryLocationDiscovery("location.visited-bay").PersonalNotes;
	Assert(notes == "这里有个隐藏的补给点", "AC-2: personal_notes 正确");
}

// ── AC-3: query_knowledge_state — 未初始化 ID 返回安全默认值 ──
{
	var mgr = BuildManager();
	var snap = mgr.QueryLocationSnapshot("location.nonexistent");
	Assert(snap.State == LocationKnowledgeState.Unknown, "AC-3: 未初始化地点 state=UNKNOWN");
	Assert(snap.RumorSources.Count == 0, "AC-3: rumor_sources=[]");
}

// ── AC-4: query_route_knowledge — IDENTIFIED 路线 ──
{
	var mgr = BuildManager();
	// route.sky-reef-arc-01 已 Seed 为 Identified
	var result = mgr.QueryRouteKnowledge("route.sky-reef-arc-01");
	Assert(result.State == LocationKnowledgeState.Identified, "AC-4: state=IDENTIFIED");
	Assert(result.VisibleHazards.Count == 2, "AC-4: 所有静态风险标签可见");
	Assert(result.HiddenHazardCount == 0, "AC-4: hidden_hazard_count=0");
}

// ── AC-5: query_route_knowledge — RUMORED 路线（2 个冲突来源）──
{
	var mgr = BuildManager();
	mgr.RevealRumor("route.lighthouse-approach", "merchant", new[] { "fog" }, 40);
	mgr.RevealRumor("route.lighthouse-approach", "navy-scout", new[] { "hidden-reef" }, 55);
	var result = mgr.QueryRouteKnowledge("route.lighthouse-approach");
	Assert(result.State == LocationKnowledgeState.Rumored, "AC-5: state=RUMORED");
	Assert(result.Sources.Count == 2, "AC-5: 2 个来源");
	// 两个来源各揭示不同风险，合计 2 个可见（fog + hidden-reef），hidden=0
	Assert(result.VisibleHazards.Count >= 1, "AC-5: 至少 1 个可见风险标签");
}

// ── AC-6: query_route_accessibility — 无能力要求且已 Identified ──
{
	var mgr = BuildManager();
	var result = mgr.QueryRouteAccessibility("route.sky-reef-arc-01");
	Assert(result.Traversable, "AC-6: 无能力要求 + Identified → traversable=true");
	Assert(result.BlockedByAbility == "", "AC-6: blocked_by_ability 为空");
	Assert(!result.BlockedByKnowledge, "AC-6: blocked_by_knowledge=false");
}

// ── AC-7: query_route_accessibility — 需要 locked 能力 → traversable=false ──
{
	var mgr = BuildManager();
	var result = mgr.QueryRouteAccessibility("route.lighthouse-approach");
	Assert(!result.Traversable, "AC-7: 需要 locked 能力 → traversable=false");
	Assert(result.BlockedByAbility == "ability.lighthouse-signal-interpretation",
		"AC-7: blocked_by_ability 正确");
}

// ── AC-8: query_pattern_state — confirmed + confirmed+ ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2 → 13
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1 → 14
	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction");
	var snap = mgr.QueryPatternState("pattern.bird-flight-direction");
	Assert(snap.State == PatternState.Confirmed, "AC-8: state=CONFIRMED");
	Assert(snap.ObservationScore == 14, "AC-8: observation_score=14");
	Assert(snap.PatternUsageSuccess, "AC-8: is_confirmed_plus=true");
	Assert(snap.TriggeredEvents.Count == 4, "AC-8: triggered_events 含 4 个事件");
}

// ── AC-9: query_pattern_state — 未初始化返回安全默认值 ──
{
	var mgr = BuildManager();
	var snap = mgr.QueryPatternState("pattern.nonexistent");
	Assert(snap.State == PatternState.Undiscovered, "AC-9: 未初始化 state=UNDISCOVERED");
	Assert(snap.ObservationScore == 0, "AC-9: score=0");
}

// ── AC-10: query_ability_state — 已解锁 ──
{
	var mgr = BuildManager();
	mgr.OnRepairCompleted("repair_lighthouse_01");
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-10: 已解锁能力返回 UNLOCKED");
}

// ── AC-11: query_ability_state — 未初始化 ID 返回 LOCKED ──
{
	var mgr = BuildManager();
	Assert(mgr.QueryAbilityState("ability.nonexistent") == AbilityState.Locked,
		"AC-11: 未初始化 ability_id 返回 LOCKED");
}

// ── AC-12: get_pattern_log — 只返回 PartiallyObserved 和 Confirmed ──
{
	var mgr = BuildManager();
	// bird: Confirmed (score=11)
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 Confirmed
	// lighthouse: PartiallyObserved (score=5 → 刚好达到 partial_threshold)
	mgr.RegisterPatternEventWeights("pattern.lighthouse-signals", new Dictionary<string, int>
	{
		["light-passive-watch"] = 4,
		["light-narrative-hint"] = 1,
	});
	mgr.ReportObservationEvent("pattern.lighthouse-signals", "light-passive-watch");    // +4
	mgr.ReportObservationEvent("pattern.lighthouse-signals", "light-narrative-hint");   // +1 → 5 PartiallyObserved
	// fog: Undiscovered (no events)
	var log = mgr.GetPatternLog();
	Assert(log.Count == 2, "AC-12: 日志含 2 条目（bird Confirmed + lighthouse PartiallyObserved）");
	Assert(log.Any(s => s.PatternId == "pattern.bird-flight-direction" && s.State == PatternState.Confirmed),
		"AC-12: bird Confirmed 出现在日志");
	Assert(log.Any(s => s.PatternId == "pattern.lighthouse-signals" && s.State == PatternState.PartiallyObserved),
		"AC-12: lighthouse PartiallyObserved 出现在日志");
	Assert(!log.Any(s => s.PatternId == "pattern.fog-navigation"),
		"AC-12: fog Undiscovered 不出现在日志");
}

// ── AC-13: get_pattern_log — 所有 Undiscovered 返回空列表 ──
{
	var mgr = BuildManager();
	var log = mgr.GetPatternLog();
	Assert(log.Count == 0, "AC-13: 所有规律 Undiscovered 时返回空列表");
}

// ── AC-14: get_ability_list — 含 3 条目 ──
{
	var mgr = BuildManager();
	mgr.OnRepairCompleted("repair_lighthouse_01"); // lighthouse 解锁
	var list = mgr.GetAbilityList();
	Assert(list.Count == 2, "AC-14: get_ability_list 返回 2 条目（已注册路径的能力）");
	var bird = list.FirstOrDefault(i => i.AbilityId == "ability.bird-flight-understanding");
	var lighthouse = list.FirstOrDefault(i => i.AbilityId == "ability.lighthouse-signal-interpretation");
	Assert(bird != null, "AC-14: bird-flight 在列表中");
	Assert(lighthouse != null, "AC-14: lighthouse 在列表中");
	Assert(bird?.State == AbilityState.Locked, "AC-14: bird-flight 为 LOCKED");
	Assert(lighthouse?.State == AbilityState.Unlocked, "AC-14: lighthouse 为 UNLOCKED");
}

// ── AC-15: get_ability_list — locked 能力含 unlock_hint ──
{
	var mgr = BuildManager();
	var list = mgr.GetAbilityList();
	var bird = list.First(i => i.AbilityId == "ability.bird-flight-understanding");
	Assert(!string.IsNullOrEmpty(bird.UnlockHint), "AC-15: locked 能力含 unlock_hint 文本");
	Assert(bird.Description == "", "AC-15: locked 能力 description 为空");
}

// ── AC-16: query_location_discovery — IDENTIFIED 地点 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.whisper-isle", "navy", Array.Empty<string>(), 80); // → Identified
	var result = mgr.QueryLocationDiscovery("location.whisper-isle");
	Assert(result.State == LocationKnowledgeState.Identified, "AC-16: state=IDENTIFIED");
	Assert(result.HazardVisibility.Count == 2, "AC-16: 含 2 条风险标签");
	Assert(result.HazardVisibility.All(h => h.Visible), "AC-16: Identified 时所有标签可见");
}

// ── 额外边缘: query_route_accessibility — Unknown 路线 ──
{
	var mgr = BuildManager();
	var result = mgr.QueryRouteAccessibility("route.never-visited");
	Assert(!result.Traversable, "EDGE: Unknown 路线 traversable=false");
	Assert(result.BlockedByKnowledge, "EDGE: Unknown 路线 blocked_by_knowledge=true");
}

Console.WriteLine();
Console.WriteLine($"Story 006 Downstream Query Interface: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
