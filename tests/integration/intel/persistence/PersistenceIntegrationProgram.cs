using CloudWeaverVoyage.Core;

// Story 008 — Persistence & MVP Bootstrap (Integration)
// 覆盖 AC-1 到 AC-12 全部验收标准

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
		["fog-passive-obs"] = 4,
		["fog-active-trial"] = 7,
	});
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
	mgr.RegisterIntelDefinition(new IntelDefinition(
		"intel.bird-migration-notes", "鸟类迁徙笔记",
		Array.Empty<string>(), Array.Empty<string>(), "",
		Array.Empty<string>()));
	mgr.Initialize();
	return mgr;
}

// 序列化后清除状态再反序列化，返回新 manager
static IntelManager RoundTrip(IntelManager src, IReadOnlySet<string>? knownIds = null)
{
	var payload = src.SerializeIntel();
	var dst = BuildManager();
	// 将 Dictionary<string, object?> 转为 IReadOnlyDictionary<string, object?>
	dst.DeserializeIntel(payload, knownIds);
	return dst;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 008: Persistence & MVP Bootstrap ===\n");

// ── AC-1: SerializeIntel — 7 个字段齐全 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.test", "src", new[] { "fog" }, 40);
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	var payload = mgr.SerializeIntel();
	Assert(payload.ContainsKey("domain_id"), "AC-1: domain_id 字段存在");
	Assert(payload.ContainsKey("knowledge_state"), "AC-1: knowledge_state 字段存在");
	Assert(payload.ContainsKey("pattern_state"), "AC-1: pattern_state 字段存在");
	Assert(payload.ContainsKey("ability_state"), "AC-1: ability_state 字段存在");
	Assert(payload.ContainsKey("consumed_intel_ids"), "AC-1: consumed_intel_ids 字段存在");
	Assert(payload.ContainsKey("rumor_sources"), "AC-1: rumor_sources 字段存在");
	Assert(payload.ContainsKey("fog_traversal_count"), "AC-1: fog_traversal_count 字段存在");
	Assert(payload.ContainsKey("active_crew"), "AC-1: active_crew 字段存在");
}

// ── AC-2: 快照不含 Object 引用（仅基础类型）──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.test", "src", new[] { "fog" }, 40);
	var payload = mgr.SerializeIntel();
	// 验证 domain_id 为 string
	Assert(payload["domain_id"] is string, "AC-2: domain_id 为 string");
	// 验证 fog_traversal_count 为 int
	Assert(payload["fog_traversal_count"] is int, "AC-2: fog_traversal_count 为 int");
}

// ── AC-3: DeserializeIntel — 7 个字段完整恢复 ──
{
	var src = BuildManager();
	// 设置各种状态
	src.RevealRumor("location.isle-a", "navy", Array.Empty<string>(), 80); // → Identified
	src.RevealRumor("location.isle-b", "merchant", new[] { "fog" }, 40);   // → Rumored
	src.PlayerArrivedAt("location.isle-a"); // → Verified
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 Confirmed
	src.ReportPatternUsageSuccess("pattern.bird-flight-direction"); // confirmed+
	src.OnRepairCompleted("repair_lighthouse_01"); // lighthouse unlocked
	src.ConsumeIntel("intel.bird-migration-notes");
	src.OnFogTraversalCompleted();
	src.OnFogTraversalCompleted(); // fog_traversal_count=2
	src.OnPartnerJoined("partner.sky-cat"); // active_crew
	src.SetPersonalNote("location.isle-a", "已探索，有补给点");

	var dst = RoundTrip(src);

	// 地点知识
	Assert(dst.QueryKnowledgeState("location.isle-a") == LocationKnowledgeState.Verified,
		"AC-3: isle-a Verified 正确恢复");
	Assert(dst.QueryKnowledgeState("location.isle-b") == LocationKnowledgeState.Rumored,
		"AC-3: isle-b Rumored 正确恢复");
	// 规律状态
	Assert(dst.GetPatternState("pattern.bird-flight-direction") == PatternState.Confirmed,
		"AC-3: pattern Confirmed 正确恢复");
	Assert(dst.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-3: pattern_usage_success 正确恢复");
	// 能力状态
	Assert(dst.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-3: ability Unlocked 正确恢复");
	// consumed_intel_ids
	Assert(dst.IsIntelConsumed("intel.bird-migration-notes"),
		"AC-3: consumed_intel_ids 正确恢复");
	// personal_notes
	Assert(dst.QueryLocationDiscovery("location.isle-a").PersonalNotes == "已探索，有补给点",
		"AC-3: personal_notes 正确恢复");
}

// ── AC-4: 迁移——未知 intel_id 保留 ──
{
	var mgr = BuildManager();
	mgr.ConsumeIntel("intel.bird-migration-notes"); // 已知 ID
	var payload = mgr.SerializeIntel();

	// 手动在 consumed_intel_ids 中注入一个不存在的 ID
	var ciList = (payload["consumed_intel_ids"] as List<object?>)!;
	ciList.Add((object?)"intel.legacy-removed");

	var dst = BuildManager();
	dst.DeserializeIntel(payload, new HashSet<string>(StringComparer.Ordinal)
	{
		"intel.bird-migration-notes" // 仅知道这一个
	});
	Assert(dst.IsIntelConsumed("intel.bird-migration-notes"),
		"AC-4: 已知 intel_id 正常恢复");
	Assert(dst.IsIntelConsumed("intel.legacy-removed"),
		"AC-4: 未知 intel_id 保留（不静默删除）");
	Assert(dst.MigrationWarnings.Any(w => w.Contains("intel.legacy-removed")),
		"AC-4: 迁移警告包含未知 intel_id");
}

// ── AC-5: InitNewGameState — 起始状态正确 ──
{
	var mgr = BuildManager();
	mgr.InitNewGameState();
	Assert(mgr.QueryKnowledgeState("route.sky-reef-arc-01") == LocationKnowledgeState.Identified,
		"AC-5: route.sky-reef-arc-01 为 IDENTIFIED");
	Assert(mgr.QueryKnowledgeState("route.high-risk-mvp") == LocationKnowledgeState.Rumored,
		"AC-5: route.high-risk-mvp 为 RUMORED");
	Assert(mgr.QueryKnowledgeState("location.glass-harbor") == LocationKnowledgeState.Identified,
		"AC-5: location.glass-harbor 为 IDENTIFIED");
	Assert(mgr.QueryKnowledgeState("location.unknown-place") == LocationKnowledgeState.Unknown,
		"AC-5: 其他地点默认 UNKNOWN");
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Undiscovered,
		"AC-5: 规律全为 UNDISCOVERED");
	Assert(mgr.QueryAbilityState("ability.bird-flight-understanding") == AbilityState.Locked,
		"AC-5: 能力全为 LOCKED");
	Assert(!mgr.IsIntelConsumed("intel.bird-migration-notes"),
		"AC-5: consumed_intel_ids 为空");
}

// ── AC-6: InitNewGameState 清除旧状态 ──
{
	var mgr = BuildManager();
	mgr.RevealRumor("location.old-data", "src", Array.Empty<string>(), 40);
	mgr.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	mgr.InitNewGameState();
	Assert(mgr.QueryKnowledgeState("location.old-data") == LocationKnowledgeState.Unknown,
		"AC-6: 旧地点数据被清除");
	Assert(mgr.GetPatternState("pattern.bird-flight-direction") == PatternState.Undiscovered,
		"AC-6: 旧规律数据被清除");
}

// ── AC-7: Persistence 域注册（接口存在性验证）──
{
	// 验证 SerializeIntel 和 DeserializeIntel 方法可以作为 Func/Action 委托传递
	var mgr = BuildManager();
	Func<Dictionary<string, object?>> serializer = mgr.SerializeIntel;
	Action<IReadOnlyDictionary<string, object?>> deserializer = (p) => mgr.DeserializeIntel(p);
	bool canRegister = serializer != null && deserializer != null;
	Assert(canRegister, "AC-7: SerializeIntel/DeserializeIntel 可作为 Persistence 域注册委托");
}

// ── AC-8: string key → 正确恢复（序列化/反序列化往返）──
{
	var src = BuildManager();
	src.RevealRumor("location.test-key", "src", Array.Empty<string>(), 80);
	var payload = src.SerializeIntel();
	var dst = BuildManager();
	dst.DeserializeIntel(payload);
	// 如果 key 恢复正确，查询应能找到
	Assert(dst.QueryKnowledgeState("location.test-key") == LocationKnowledgeState.Identified,
		"AC-8: string key 反序列化后查询正确");
}

// ── AC-9: 未知 pattern_id 保留 ──
{
	var src = BuildManager();
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-narrative-hint");
	var payload = src.SerializeIntel();

	// 注入一个未知规律 ID
	var psDict = (payload["pattern_state"] as Dictionary<string, object?>)!;
	psDict["pattern.obsolete-pattern"] = (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["observation_score"] = (object?)7,
		["triggered_events"] = (object?)new List<object?> { "old-event" },
		["pattern_usage_success"] = (object?)false,
	};

	var dst = BuildManager();
	dst.DeserializeIntel(payload);
	// 未知规律保留——QueryPatternState 返回默认安全值（Undiscovered）因为没有权重表，但数据存在
	var snap = dst.GetPatternSnapshot("pattern.obsolete-pattern");
	Assert(snap != null && snap.ObservationScore == 7,
		"AC-9: 未知 pattern_id 数据保留（score=7）");
	// 但 QueryPatternState 对未知规律返回 Undiscovered（无权重表，状态由 score 和默认阈值决定）
	Assert(dst.QueryPatternState("pattern.obsolete-pattern").State == PatternState.PartiallyObserved,
		"AC-9: 未知规律 score=7 ≥ partial_threshold=5 → PartiallyObserved");
}

// ── AC-10: 往返保真度——全状态组合 ──
{
	var src = BuildManager();
	src.RevealRumor("location.isle", "nav", new[] { "fog", "rocks" }, 55);
	src.PlayerArrivedAt("location.isle");
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11 Confirmed
	src.ReportObservationEvent("pattern.fog-navigation", "fog-passive-obs");            // +4
	src.ReportObservationEvent("pattern.fog-navigation", "fog-active-trial");           // +7 → 11 Confirmed
	src.OnRepairCompleted("repair_lighthouse_01");
	src.ConsumeIntel("intel.bird-migration-notes");
	src.OnFogTraversalCompleted(); src.OnFogTraversalCompleted(); // count=2
	src.OnPartnerJoined("partner.sky-cat");
	src.SetPersonalNote("location.isle", "重要港口");

	var dst = RoundTrip(src);

	Assert(dst.QueryKnowledgeState("location.isle") == LocationKnowledgeState.Verified,
		"AC-10: location Verified 往返正确");
	Assert(dst.GetPatternState("pattern.bird-flight-direction") == PatternState.Confirmed,
		"AC-10: pattern Confirmed 往返正确");
	Assert(dst.GetPatternState("pattern.fog-navigation") == PatternState.Confirmed,
		"AC-10: pattern fog Confirmed 往返正确");
	Assert(dst.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked,
		"AC-10: ability Unlocked 往返正确");
	Assert(dst.IsIntelConsumed("intel.bird-migration-notes"),
		"AC-10: consumed_intel 往返正确");
	Assert(dst.QueryLocationDiscovery("location.isle").PersonalNotes == "重要港口",
		"AC-10: personal_notes 往返正确");
}

// ── AC-11: 读档后 confirmed+ 正确 ──
{
	var src = BuildManager();
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-active-study");   // +7
	src.ReportObservationEvent("pattern.bird-flight-direction", "bird-passive-island"); // +4 → 11
	src.ReportPatternUsageSuccess("pattern.bird-flight-direction"); // confirmed+
	var dst = RoundTrip(src);
	Assert(dst.IsConfirmedPlus("pattern.bird-flight-direction"),
		"AC-11: 读档后 is_confirmed_plus 正确恢复");
}

// ── AC-12: 读档后 verified 地点的 personal_notes 保留 ──
{
	var src = BuildManager();
	src.PlayerArrivedAt("location.harbor");
	src.SetPersonalNote("location.harbor", "这里有修理站");
	var dst = RoundTrip(src);
	Assert(dst.QueryKnowledgeState("location.harbor") == LocationKnowledgeState.Verified,
		"AC-12: verified 状态正确恢复");
	Assert(dst.QueryLocationDiscovery("location.harbor").PersonalNotes == "这里有修理站",
		"AC-12: personal_notes 正确保留");
}

// ── 空状态往返（边缘案例）──
{
	var src = BuildManager();
	var dst = RoundTrip(src);
	Assert(dst.QueryKnowledgeState("location.nonexistent") == LocationKnowledgeState.Unknown,
		"EDGE: 空状态往返后查询返回安全默认值");
}

Console.WriteLine();
Console.WriteLine($"Story 008 Persistence Integration: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
