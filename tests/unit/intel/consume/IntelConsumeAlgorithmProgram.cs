using CloudWeaverVoyage.Core;

// Story 004 — IntelConsumeResult Algorithm
// 覆盖 AC-1 到 AC-15 全部验收标准

static IntelManager BuildManager()
{
	var mgr = new IntelManager();

	// 注册规律事件权重
	mgr.RegisterPatternEventWeights("pattern.bird-flight-direction", new Dictionary<string, int>
	{
		["bird-narrative-hint"] = 1,
		["bird-log-migration"] = 2,
		["bird-passive-island"] = 4,
		["bird-active-study"] = 7,
	});

	// 注册 intel 定义
	// intel.bird-migration-notes: 关联地点 + 规律 + 能力
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes",
		"鸟类迁徙笔记",
		new[] { "location.whisper-isle", "route.bird-migration-corridor" },
		new[] { "pattern.bird-flight-direction" },
		"bird-log-migration",
		new[] { "ability.bird-flight-understanding" }));

	// intel.pure-narrative: 纯叙事情报（无关联内容）
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.pure-narrative",
		"旧航海日志",
		Array.Empty<string>(),
		Array.Empty<string>(),
		"",
		Array.Empty<string>()));

	// 注册能力解锁路径（供 Rule 4 使用）
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.bird-flight-understanding",
		new[]
		{
			// Path B: intel_consumed + observation_event_count ≥ 1
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
		}));

	// 初始化地点状态
	mgr.SeedLocationKnowledge("location.whisper-isle", LocationKnowledgeState.Rumored, "旧传言");
	// route.bird-migration-corridor 保持 Unknown（默认）
	// location.already-identified 设为 Identified
	mgr.SeedLocationKnowledge("location.already-identified", LocationKnowledgeState.Identified, "航图");
	// location.already-verified 设为 Verified
	mgr.SeedLocationKnowledge("location.already-verified", LocationKnowledgeState.Verified, "亲身探索");

	mgr.Initialize();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 004: IntelConsumeResult Algorithm ===\n");

// ── AC-1: 基本消费流程 — success=true，返回 intelId 和 displayName ──
{
	var mgr = BuildManager();
	var result = mgr.ConsumeIntel("intel.pure-narrative");
	Assert(result.Success, "AC-1: 成功消费返回 success=true");
	Assert(result.IntelId == "intel.pure-narrative", "AC-1: intelId 正确");
	Assert(result.IntelDisplayName == "旧航海日志", "AC-1: displayName 来自定义");
	Assert(result.ErrorCode == "", "AC-1: 无错误码");
}

// ── AC-2: 已消耗 → ERR_INTEL_ALREADY_CONSUMED ──
{
	var mgr = BuildManager();
	mgr.ConsumeIntel("intel.pure-narrative"); // 第一次
	var result = mgr.ConsumeIntel("intel.pure-narrative"); // 第二次
	Assert(!result.Success, "AC-2: 重复消耗 success=false");
	Assert(result.ErrorCode == "ERR_INTEL_ALREADY_CONSUMED", "AC-2: 错误码正确");
	Assert(result.LocationAdvancements.Count == 0, "AC-2: LocationAdvancements 为空");
	Assert(result.AbilityUnlocks.Count == 0, "AC-2: AbilityUnlocks 为空");
	Assert(result.PatternObservations.Count == 0, "AC-2: PatternObservations 为空");
}

// ── AC-3: 重复消耗不产生任何状态变更 ──
{
	var mgr = BuildManager();
	mgr.ConsumeIntel("intel.bird-migration-notes"); // 首次：rumored→identified 等
	var stateBefore = mgr.QueryKnowledgeState("location.whisper-isle");
	var scoreBefore = mgr.GetPatternSnapshot("pattern.bird-flight-direction")?.ObservationScore ?? 0;
	mgr.ConsumeIntel("intel.bird-migration-notes"); // 重复：应无任何变化
	Assert(mgr.QueryKnowledgeState("location.whisper-isle") == stateBefore,
		"AC-3: 重复消耗不改变地点状态");
	Assert((mgr.GetPatternSnapshot("pattern.bird-flight-direction")?.ObservationScore ?? 0) == scoreBefore,
		"AC-3: 重复消耗不改变 observation_score");
}

// ── AC-4: Rule 2 — 关联地点知识推进 ──
{
	var mgr = BuildManager();
	var result = mgr.ConsumeIntel("intel.bird-migration-notes");
	Assert(result.LocationAdvancements.Count == 2, "AC-4: 2 个关联地点均推进");
	var whisper = result.LocationAdvancements.FirstOrDefault(a => a.LocationId == "location.whisper-isle");
	var corridor = result.LocationAdvancements.FirstOrDefault(a => a.LocationId == "route.bird-migration-corridor");
	Assert(whisper != null, "AC-4: whisper-isle 出现在推进列表");
	Assert(whisper?.PreviousState == LocationKnowledgeState.Rumored, "AC-4: whisper-isle 旧状态 Rumored");
	Assert(whisper?.NewState == LocationKnowledgeState.Identified, "AC-4: whisper-isle 新状态 Identified");
	Assert(corridor != null, "AC-4: corridor 出现在推进列表");
	Assert(corridor?.PreviousState == LocationKnowledgeState.Unknown, "AC-4: corridor 旧状态 Unknown");
	Assert(corridor?.NewState == LocationKnowledgeState.Identified, "AC-4: corridor 新状态 Identified");
}

// ── AC-5: 已达 Identified 的地点不在推进列表中 ──
{
	var mgr = BuildManager();
	// 注册含 already-identified 的 intel
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.test-identified", "测试",
		new[] { "location.already-identified" },
		Array.Empty<string>(), "",
		Array.Empty<string>()));
	var result = mgr.ConsumeIntel("intel.test-identified");
	Assert(result.LocationAdvancements.Count == 0,
		"AC-5: 已 Identified 的地点不出现在推进列表");
}

// ── AC-6: 已达 Verified 的地点不在推进列表中 ──
{
	var mgr = BuildManager();
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.test-verified", "测试",
		new[] { "location.already-verified" },
		Array.Empty<string>(), "",
		Array.Empty<string>()));
	var result = mgr.ConsumeIntel("intel.test-verified");
	Assert(result.LocationAdvancements.Count == 0,
		"AC-6: 已 Verified 的地点不出现在推进列表");
}

// ── AC-7: 纯叙事情报（linked_content_ids 为空）location_advancements 为空 ──
{
	var mgr = BuildManager();
	var result = mgr.ConsumeIntel("intel.pure-narrative");
	Assert(result.LocationAdvancements.Count == 0,
		"AC-7: 纯叙事情报 location_advancements 为空，不报错");
}

// ── AC-8: Rule 3 — 对关联规律添加 log_fragment 观测事件 ──
{
	var mgr = BuildManager();
	var result = mgr.ConsumeIntel("intel.bird-migration-notes");
	Assert(result.PatternObservations.Count == 1, "AC-8: 1 条规律观测记录");
	var obs = result.PatternObservations[0];
	Assert(obs.PatternId == "pattern.bird-flight-direction", "AC-8: patternId 正确");
	Assert(obs.EventId == "bird-log-migration", "AC-8: eventId 正确");
	Assert(obs.EventType == "log_fragment", "AC-8: eventType=log_fragment");
	Assert(obs.AddedScore == 2, "AC-8: added_score=2（log_fragment 权重）");
	Assert(obs.NewObservationScore == 2, "AC-8: new_observation_score=2");
}

// ── AC-9: Rule 3 去重 — 事件已触发时不追加 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-log-migration"); // 预先触发
	var result = mgr.ConsumeIntel("intel.bird-migration-notes");
	Assert(result.PatternObservations.Count == 0,
		"AC-9: 已触发事件重复时不追加到 pattern_observations");
	Assert(mgr.GetPatternSnapshot("pattern.bird-flight-direction")?.ObservationScore == 2,
		"AC-9: observation_score 仍为预先触发的 2，未重复累加");
}

// ── AC-10: 无 linked_patterns → pattern_observations 为空 ──
{
	var mgr = BuildManager();
	var result = mgr.ConsumeIntel("intel.pure-narrative");
	Assert(result.PatternObservations.Count == 0, "AC-10: 无关联规律 pattern_observations 为空");
}

// ── AC-11: Rule 4 — Path B 条件满足时能力解锁 ──
{
	var mgr = BuildManager();
	// 消费前需先有观测事件（Path B 的第二个条件）
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	var result = mgr.ConsumeIntel("intel.bird-migration-notes");
	// 消费后 intel_consumed=true + 观测事件 ≥ 1 → Path B 满足
	Assert(result.AbilityUnlocks.Count == 1, "AC-11: ability_unlocks 含 1 条目");
	var unlock = result.AbilityUnlocks[0];
	Assert(unlock.AbilityId == "ability.bird-flight-understanding", "AC-11: abilityId 正确");
	Assert(unlock.UnlockPath.Contains("path_b"), "AC-11: unlock_path 含 'path_b'");
}

// ── AC-12: Rule 4 — Path B 条件不满足时 ability_unlocks 为空 ──
// 注意：Rule 3 会为关联规律添加 log_fragment 事件（count=1），
//       因此将 min_count 设为 2 来验证"事件数不足时不解锁"。
{
	var mgr = BuildManager();
	// 重新注册需要 2 个事件的能力路径
	mgr.RegisterAbilityPathConfig(new AbilityPathConfig(
		"ability.bird-flight-understanding",
		new[]
		{
			new AbilityUnlockPath("path_b_intel_observation_strict", new[]
			{
				new AbilityCondition("intel_consumed", new Dictionary<string, object>
					{ ["intel_id"] = "intel.bird-migration-notes" }),
				new AbilityCondition("observation_event_count", new Dictionary<string, object>
				{
					["pattern_id"] = "pattern.bird-flight-direction",
					["min_count"] = 2, // Rule 3 只添加 1 个，不满足
				}),
			}),
		}));
	var result = mgr.ConsumeIntel("intel.bird-migration-notes");
	// Rule 3 添加 bird-log-migration → count=1 < 2 → Path B 不满足
	Assert(result.AbilityUnlocks.Count == 0,
		"AC-12: observation_event_count < min_count 时 ability_unlocks 为空");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Locked,
		"AC-12: 能力保持 locked");
}

// ── AC-13: Rule 5 — 消费后 IsIntelConsumed 返回 true ──
{
	var mgr = BuildManager();
	Assert(!mgr.IsIntelConsumed("intel.pure-narrative"), "AC-13: 消费前 false");
	mgr.ConsumeIntel("intel.pure-narrative");
	Assert(mgr.IsIntelConsumed("intel.pure-narrative"), "AC-13: 消费后 IsIntelConsumed=true");
}

// ── AC-14: ERR_INTEL_NOT_FOUND ──
{
	var mgr = BuildManager();
	var result = mgr.ConsumeIntel("intel.nonexistent");
	Assert(!result.Success, "AC-14: success=false");
	Assert(result.ErrorCode == "ERR_INTEL_NOT_FOUND", "AC-14: 错误码正确");
	Assert(result.LocationAdvancements.Count == 0, "AC-14: 无地点推进");
	Assert(result.PatternObservations.Count == 0, "AC-14: 无规律观测");
}

// ── AC-15: 三重效果 — 地点+规律+能力同时填充 ──
{
	var mgr = BuildManager();
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint"); // 预先有观测事件
	string? intelConsumedSig = null;
	mgr.IntelConsumed += id => intelConsumedSig = id;
	var result = mgr.ConsumeIntel("intel.bird-migration-notes");
	Assert(result.Success, "AC-15: 三重效果 success=true");
	Assert(result.LocationAdvancements.Count == 2, "AC-15: location_advancements 含 2 条目");
	Assert(result.PatternObservations.Count == 1, "AC-15: pattern_observations 含 1 条目");
	Assert(result.AbilityUnlocks.Count == 1, "AC-15: ability_unlocks 含 1 条目");
	Assert(intelConsumedSig == "intel.bird-migration-notes", "AC-15: IntelConsumed 信号触发");
}

Console.WriteLine();
Console.WriteLine($"Story 004 IntelConsumeResult: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
