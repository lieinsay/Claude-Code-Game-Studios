using CloudWeaverVoyage.Core;

// Story 005 — Upstream Event Receivers
// 覆盖 AC-1 到 AC-14 全部验收标准

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
	mgr.RegisterPatternEventWeights("pattern.fog-navigation", new Dictionary<string, int>
	{
		["fog-narrative-hint"] = 1,
		["fog-log-fragment"] = 2,
	});
	mgr.RegisterLocationTags("location.lighthouse-bay", new[] { "has_lighthouse" });

	// 注册雾中穿行能力（Path C: fog_traversal_count ≥ 3）
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.fog-navigation",
		new[]
		{
			new AbilityUnlockPath("path_c_experience", new[]
			{
				new AbilityCondition("fog_traversal_count", new Dictionary<string, object>
					{ ["min_count"] = 3 }),
			}),
		}));

	// 注册鸟类理解能力（Path C: partner + passive_observation）
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.bird-flight-understanding",
		new[]
		{
			new AbilityUnlockPath("path_a_pattern", new[]
			{
				new AbilityCondition("pattern_state", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.bird-flight-direction",
					["required_state"] = PatternState.Confirmed,
				}),
			}),
			new AbilityUnlockPath("path_c_partner_passive", new[]
			{
				new AbilityCondition("partner_in_crew", new Dictionary<string, object>
					{ ["partner_id"] = "partner.old-sailor" }),
				new AbilityCondition("observation_event_type_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.bird-flight-direction",
					["event_type"] = "passive_observation",
					["min_count"] = 1,
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

Console.WriteLine("=== Story 005: Upstream Event Receivers ===\n");

// ── AC-1: report_observation_event — 正常累分 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	var snap = mgr.GetPatternSnapshot("pattern.bird-flight-direction")!;
	Assert(snap.ObservationScore == 3, "AC-1: observation_score 正确累加");
	Assert(snap.TriggeredEvents.Count == 2, "AC-1: triggered_events 追加正确");
}

// ── AC-2: report_observation_event — 无效 pattern_id 不崩溃 ──
{
	var mgr = BuildManager();
	bool threw = false;
	try { mgr.ReportObservationEvent("pattern.nonexistent", "some-event"); }
	catch { threw = true; }
	Assert(!threw, "AC-2: 无效 pattern_id 不崩溃");
	// 未知 pattern 的 score 应为 0（无权重表，事件权重=0）
	Assert((mgr.GetPatternSnapshot("pattern.nonexistent")?.ObservationScore ?? 0) == 0,
		"AC-2: 无效 pattern 事件权重为 0，score 不变");
}

// ── AC-3: report_observation_event 后 ReevaluateAbilityUnlocks 触发 ──
{
	var mgr = BuildManager();
	mgr.OnPartnerJoined("partner.old-sailor"); // 满足 Path C 的伙伴条件
	// 触发 passive_observation 事件 → 触发重新评估 → Path C 满足
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-3: report_observation_event 后 ReevaluateAbilityUnlocks 已调用，能力解锁");
}

// ── AC-4: report_pattern_usage_success — Confirmed + usage_success → confirmed+ ──
{
	var mgr = BuildManager();
	// 先达到 Confirmed
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11
	bool sigFired = false;
	mgr.PatternUsageConfirmed += _ => sigFired = true;
	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction");
	Assert(mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-4: Confirmed + usage_success → is_confirmed_plus=true");
	Assert(sigFired, "AC-4: pattern_usage_confirmed signal 触发");
}

// ── AC-5: report_pattern_usage_success — PartiallyObserved 时提前设置，confirmed+ 仍为 false ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // +1
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration");  // +2
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 7 → PartiallyObserved
	mgr.ReportPatternUsageSuccess("pattern.bird-flight-direction");
	Assert(!mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-5: PartiallyObserved 时 is_confirmed_plus 仍为 false");
	// 继续累分达到 Confirmed → confirmed+ 自动激活
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study"); // +7 → 14 Confirmed
	Assert(mgr.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-5: 达到 Confirmed 后 is_confirmed_plus 自动激活");
}

// ── AC-6: report_navigation_event — fog_traversal_completed 累计 ──
{
	var mgr = BuildManager();
	mgr.ReportNavigationEvent("fog_traversal_completed");
	mgr.ReportNavigationEvent("fog_traversal_completed");
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Locked,
		"AC-6: 2 次穿越未解锁");
	mgr.ReportNavigationEvent("fog_traversal_completed"); // 第 3 次
	Assert(mgr.QueryAbilityState("ability.fog-navigation") == AbilityState.Unlocked,
		"AC-6: 3 次穿越后 fog-navigation 解锁");
}

// ── AC-7: report_navigation_event — 其他事件类型不报错 ──
{
	var mgr = BuildManager();
	bool threw = false;
	try
	{
		mgr.ReportNavigationEvent("route_travel_completed");
		mgr.ReportNavigationEvent("player_entered_zone");
		mgr.ReportNavigationEvent("player_hit_obstacle");
		mgr.ReportNavigationEvent("unknown_future_event");
	}
	catch { threw = true; }
	Assert(!threw, "AC-7: 其他/未知事件类型不崩溃");
}

// ── AC-8: on_partner_joined — 追加 + 重评能力 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island");
	mgr.OnPartnerJoined("partner.old-sailor");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-8: partner_joined 后 _reevaluate_ability_unlocks 触发解锁");
}

// ── AC-9: on_partner_joined — 重复调用不追加 ──
{
	var mgr = BuildManager();
	mgr.OnPartnerJoined("partner.old-sailor");
	int sigCount = 0;
	mgr.AbilityUnlocked += (_, _) => sigCount++;
	mgr.OnPartnerJoined("partner.old-sailor"); // 重复
	// 已在 crew，伙伴条件已满足，但能力未满足其他条件，不重复解锁
	Assert(sigCount == 0, "AC-9: 重复 on_partner_joined 不追加重复，不重复触发信号");
}

// ── AC-10: on_partner_left — 移除伙伴，已解锁能力保持 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island");
	mgr.OnPartnerJoined("partner.old-sailor");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-10: 解锁前验证");
	mgr.OnPartnerLeft("partner.old-sailor");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Unlocked,
		"AC-10: 伙伴离队后已解锁能力保持 Unlocked");
}

// ── AC-11: on_repair_completed — 追加并重评 ──
{
	var mgr = BuildManager();
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.lighthouse-signal-interpretation",
		new[]
		{
			new AbilityUnlockPath("path_c_world_repair", new[]
			{
				new AbilityCondition("repair_completed", new Dictionary<string, object>
					{ ["repair_node_id"] = "repair_lighthouse_01" }),
			}),
		}));
	mgr.OnRepairCompleted("repair_lighthouse_01");
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-11: on_repair_completed 后能力解锁");
}

// ── AC-12: on_repair_completed — 重复调用不重复追加 ──
{
	var mgr = BuildManager();
	mgr.OnRepairCompleted("repair_lighthouse_01");
	int sigCount = 0;
	mgr.AbilityUnlocked += (_, _) => sigCount++;
	mgr.OnRepairCompleted("repair_lighthouse_01"); // 重复
	Assert(sigCount == 0, "AC-12: 重复 on_repair_completed 不重复触发信号");
}

// ── AC-13: player_arrived_at 后 ReevaluateAbilityUnlocks 触发 ──
{
	var mgr = BuildManager();
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.lighthouse-signal-interpretation",
		new[]
		{
			new AbilityUnlockPath("path_d_partner_visit", new[]
			{
				new AbilityCondition("partner_in_crew", new Dictionary<string, object>
					{ ["partner_id"] = "partner.lighthouse-keeper-descendant" }),
				new AbilityCondition("location_visit_count", new Dictionary<string, object>
				{
					["location_tag"] = "has_lighthouse",
					["min_count"] = 1,
					["required_state"] = LocationKnowledgeState.Verified,
				}),
			}),
		}));
	mgr.OnPartnerJoined("partner.lighthouse-keeper-descendant");
	mgr.PlayerArrivedAt("location.lighthouse-bay"); // → Verified，触发重评
	Assert(mgr.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-13: player_arrived_at 后 ReevaluateAbilityUnlocks 触发，灯塔 Path D 解锁");
}

// ── AC-14: player_arrived_at — 未注册地点不崩溃，仍推进至 Verified ──
{
	var mgr = BuildManager();
	bool threw = false;
	try { mgr.PlayerArrivedAt("location.dynamic-generated-isle"); }
	catch { threw = true; }
	Assert(!threw, "AC-14: 未注册地点 player_arrived_at 不崩溃");
	Assert(mgr.QueryKnowledgeState("location.dynamic-generated-isle") == LocationKnowledgeState.Verified,
		"AC-14: 未注册地点仍推进至 Verified");
}

Console.WriteLine();
Console.WriteLine($"Story 005 Upstream Event Receivers: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
