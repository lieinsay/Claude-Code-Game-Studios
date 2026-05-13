using CloudWeaverVoyage.Core;

// Story 003 — Ability Multi-Path Unlock System
// 覆盖 AC-1 到 AC-14 全部验收标准

// ── 辅助：构建标准 IntelManager（含 3 条 MVP 能力路径配置）──
static IntelManager BuildManager()
{
	var mgr = new IntelManager();

	// 注册规律事件权重（供条件求值使用）
	mgr.RegisterPatternEventWeights("pattern.bird-flight-direction", new Dictionary<string, int>
	{
		["bird-narrative-hint"] = 1,
		["bird-log-migration"] = 2,
		["bird-partner-comment"] = 3,
		["bird-passive-island"] = 4,   // passive_observation
		["bird-active-study"] = 7,
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

	// 地点标签索引（供 location_visit_count 条件求值）
	mgr.RegisterLocationTags("location.lighthouse-bay", new[] { "has_lighthouse" });
	mgr.RegisterLocationTags("location.lighthouse-peak", new[] { "has_lighthouse" });

	// ── ability.bird-flight-understanding ──
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
			new AbilityUnlockPath("path_b_intel_observation", new[]
			{
				new AbilityCondition("intel_consumed", new Dictionary<string, object>
				{
					["intel_id"] = "intel.bird-migration-notes",
				}),
				new AbilityCondition("observation_event_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.bird-flight-direction",
					["min_count"] = 1,
				}),
			}),
			new AbilityUnlockPath("path_c_partner_passive", new[]
			{
				new AbilityCondition("partner_in_crew", new Dictionary<string, object>
				{
					["partner_id"] = "partner.old-sailor",
				}),
				new AbilityCondition("observation_event_type_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.bird-flight-direction",
					["event_type"] = "passive_observation",
					["min_count"] = 1,
				}),
			}),
		}));

	// ── ability.lighthouse-signal-interpretation ──
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.lighthouse-signal-interpretation",
		new[]
		{
			new AbilityUnlockPath("path_a_pattern_confirmed", new[]
			{
				new AbilityCondition("pattern_state", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.lighthouse-signals",
					["required_state"] = PatternState.Confirmed,
				}),
			}),
			new AbilityUnlockPath("path_b_intel_observation", new[]
			{
				new AbilityCondition("intel_consumed", new Dictionary<string, object>
				{
					["intel_id"] = "intel.signal-codex",
				}),
				new AbilityCondition("observation_event_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.lighthouse-signals",
					["min_count"] = 1,
				}),
			}),
			new AbilityUnlockPath("path_c_world_repair", new[]
			{
				new AbilityCondition("repair_completed", new Dictionary<string, object>
				{
					["repair_node_id"] = "repair_lighthouse_01",
				}),
			}),
			new AbilityUnlockPath("path_d_partner_visit", new[]
			{
				new AbilityCondition("partner_in_crew", new Dictionary<string, object>
				{
					["partner_id"] = "partner.lighthouse-keeper-descendant",
				}),
				new AbilityCondition("location_visit_count", new Dictionary<string, object>
				{
					["location_tag"] = "has_lighthouse",
					["min_count"] = 1,
					["required_state"] = LocationKnowledgeState.Verified,
				}),
			}),
		}));

	// ── ability.fog-navigation ──
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.fog-navigation",
		new[]
		{
			new AbilityUnlockPath("path_a_pattern_confirmed", new[]
			{
				new AbilityCondition("pattern_state", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.fog-navigation",
					["required_state"] = PatternState.Confirmed,
				}),
			}),
			new AbilityUnlockPath("path_b_intel_observation", new[]
			{
				new AbilityCondition("intel_consumed", new Dictionary<string, object>
				{
					["intel_id"] = "intel.fog-compass-manual",
				}),
				new AbilityCondition("observation_event_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.fog-navigation",
					["min_count"] = 1,
				}),
			}),
			new AbilityUnlockPath("path_c_experience", new[]
			{
				new AbilityCondition("fog_traversal_count", new Dictionary<string, object>
				{
					["min_count"] = 3,
				}),
			}),
			new AbilityUnlockPath("path_d_partner_observation", new[]
			{
				new AbilityCondition("partner_in_crew", new Dictionary<string, object>
				{
					["partner_id"] = "partner.cartographer",
				}),
				new AbilityCondition("observation_event_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.fog-navigation",
					["min_count"] = 2,
				}),
			}),
		}));

	mgr.Initialize();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 003: Ability Multi-Path Unlock System ===\n");

// ── AC-1: OR 逻辑 — 仅 Path B 满足即解锁（Path A/C 不满足）──
{
	var mgr = BuildManager();
	// Path A 不满足（pattern 未 confirmed），Path C 不满足（无伙伴）
	// Path B 满足：intel_consumed + observation_event_count ≥ 1
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		Array.Empty<string>(), Array.Empty<string>(), "",
		Array.Empty<string>()));
	mgr.ConsumeIntel("intel.bird-migration-notes");
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // 1 事件
	bool result = mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(result, "AC-1: OR 逻辑 — 仅 Path B 满足，Path A/C 不满足，仍可解锁");
}

// ── AC-2: AND 逻辑 — Path B 仅 intel_consumed，无观测事件 → 不解锁 ──
{
	var mgr = BuildManager();
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		Array.Empty<string>(), Array.Empty<string>(), "",
		Array.Empty<string>()));
	mgr.ConsumeIntel("intel.bird-migration-notes");
	// 无观测事件 → Path B AND 逻辑不满足
	bool result = mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(!result, "AC-2: AND 逻辑 — Path B 仅 1/2 条件满足不解锁");
}

// ── AC-3 (Path A): pattern.bird-flight-direction confirmed → 解锁 ──
{
	var mgr = BuildManager();
	// 达到 Confirmed: score ≥ 10
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11
	string? sigAbility = null; string? sigPath = null;
	mgr.AbilityUnlocked += (a, p) => { sigAbility = a; sigPath = p; };
	bool result = mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(result, "AC-3: Path A pattern confirmed → 解锁");
	Assert(sigAbility == "ability.bird-flight-understanding", "AC-3: AbilityUnlocked 信号触发");
	Assert(sigPath == "path_a_pattern_confirmed", "AC-3: 信号 pathId=path_a_pattern_confirmed");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-3: 能力状态变为 Unlocked");
}

// ── AC-4 (Path B): intel_consumed + observation_event_count ≥ 1 → 解锁 ──
{
	var mgr = BuildManager();
	// 通过 IntelDefinition 消费 intel
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		Array.Empty<string>(), Array.Empty<string>(), "",
		Array.Empty<string>()));
	mgr.ConsumeIntel("intel.bird-migration-notes"); // Rule 5 标记已消耗
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // ≥1 事件
	bool result = mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(result, "AC-4: Path B intel+observation → 解锁");
}

// ── AC-5 (Path B 不满足): intel 已消耗但无观测事件 ──
{
	var mgr = BuildManager();
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		Array.Empty<string>(), Array.Empty<string>(), "",
		Array.Empty<string>()));
	mgr.ConsumeIntel("intel.bird-migration-notes");
	// 无观测事件
	bool result = mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(!result, "AC-5: Path B 无观测事件 → 不解锁");
}

// ── AC-6 (Path C): partner_in_crew + passive_observation ≥ 1 → 解锁 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island");
	mgr.OnPartnerJoined("partner.old-sailor");
	mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-6: Path C partner + passive_obs → 解锁（即使规律 undiscovered）");
}

// ── AC-7 (Path C): repair_lighthouse_01 完成 → lighthouse 解锁 ──
{
	var mgr = BuildManager();
	mgr.OnRepairCompleted("repair_lighthouse_01");
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-7: Path C repair_completed → lighthouse 解锁");
}

// ── AC-8 (Path D): partner_in_crew + location verified ≥ 1 → lighthouse 解锁 ──
{
	var mgr = BuildManager();
	// 先到达地点（Verified），再加入伙伴触发重新评估
	mgr.PlayerArrivedAt("location.lighthouse-bay"); // → Verified
	mgr.OnPartnerJoined("partner.lighthouse-keeper-descendant"); // 触发 ReevaluateAbilityUnlocks
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-8: Path D partner + lighthouse verified → 解锁");
}

// ── AC-9 (Path C): fog_traversal_count ≥ 3 → fog-navigation 解锁 ──
{
	var mgr = BuildManager();
	mgr.OnFogTraversalCompleted();
	mgr.OnFogTraversalCompleted();
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Locked,
		"AC-9: 2 次穿越 < 3 → 未解锁");
	mgr.OnFogTraversalCompleted(); // 第 3 次触发 ReevaluateAbilityUnlocks
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Unlocked,
		"AC-9: 3 次穿越 → fog-navigation 解锁");
}

// ── AC-10 (Path D): partner + observation ≥ 2 → fog-navigation 解锁 ──
{
	var mgr = BuildManager();
	mgr.OnPartnerJoined("partner.cartographer");
	mgr.ReportObservationEvent("pattern.fog-navigation", "fog-narrative-hint"); // 1 事件
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Locked,
		"AC-10: partner + 1 事件 < 2 → 未解锁");
	mgr.ReportObservationEvent("pattern.fog-navigation", "fog-log-fragment"); // 2 事件
	// ReportObservationEvent 后触发 ReevaluateAbilityUnlocks（需要直接调用 CheckUnlock）
	mgr.CheckUnlockConditions("ability.fog-navigation");
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Unlocked,
		"AC-10: partner + 2 事件 → fog-navigation 解锁");
}

// ── AC-11: 已解锁能力 CheckUnlockConditions 短路返回 true，不重复信号 ──
{
	var mgr = BuildManager();
	mgr.OnRepairCompleted("repair_lighthouse_01"); // → lighthouse Unlocked
	int signalCount = 0;
	mgr.AbilityUnlocked += (_, _) => signalCount++;
	bool result = mgr.CheckUnlockConditions("ability.lighthouse-signal-interpretation");
	Assert(result, "AC-11: 已解锁能力返回 true");
	Assert(signalCount == 0, "AC-11: 不重复 emit AbilityUnlocked 信号");
}

// ── AC-12: 解锁后 partner 离队，能力保持 Unlocked ──
{
	var mgr = BuildManager();
	// 先触发观测，再加入伙伴（触发解锁）
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island");
	mgr.OnPartnerJoined("partner.old-sailor"); // 触发 ReevaluateAbilityUnlocks → 解锁
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-12: 解锁前验证");
	mgr.OnPartnerLeft("partner.old-sailor"); // 伙伴离队
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-12: 伙伴离队后能力保持 Unlocked（不可逆）");
}

// ── AC-13: 两条路径同时满足，解锁一次，pathId 为第一条 ──
{
	var mgr = BuildManager();
	// Path A 满足（pattern confirmed）+ Path B 也满足（intel + observation）
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 Confirmed
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		Array.Empty<string>(), Array.Empty<string>(), "",
		Array.Empty<string>()));
	mgr.ConsumeIntel("intel.bird-migration-notes");
	// 此时 Path A 和 Path B 都满足
	int sigCount = 0; string? firstPath = null;
	mgr.AbilityUnlocked += (_, p) => { sigCount++; firstPath = p; };
	bool result = mgr.CheckUnlockConditions("ability.bird-flight-understanding");
	Assert(result, "AC-13: 两条路径满足时能力解锁");
	Assert(sigCount == 1, "AC-13: 仅发出一次 AbilityUnlocked 信号");
	Assert(firstPath == "path_a_pattern_confirmed", "AC-13: unlock_path 为第一条路径 path_a");
}

// ── AC-14: 启动验证 — 未知条件类型抛出 InvalidOperationException ──
{
	bool caught = false;
	try
	{
		var mgr = new IntelManager();
		mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
			"ability.test-unknown",
			new[]
			{
				new AbilityUnlockPath("path_x", new[]
				{
					new AbilityCondition("unknown_condition_type_xyz",
						new Dictionary<string, object>()),
				}),
			}));
		mgr.Initialize(); // 应在此抛出 InvalidOperationException
	}
	catch (InvalidOperationException ex)
	{
		caught = ex.Message.Contains("unknown_condition_type_xyz");
	}
	Assert(caught, "AC-14: 未知条件类型在 Initialize() 时抛出含类型名的异常");
}

Console.WriteLine();
Console.WriteLine($"Story 003 Ability Unlock: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
