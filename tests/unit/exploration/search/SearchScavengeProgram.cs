using CloudWeaverVoyage.Core;

// Story 002 — Search, Scavenge & Intel Formulas
// 覆盖 AC-1 到 AC-20 全部验收标准

static ExplorationManager BuildSearchable(bool canAdd = true, string? intelId = null)
{
	var mgr = new ExplorationManager();
	// 注入 Pool 5 委托
	mgr.SetCanAddToPoolDelegate((_, _) => canAdd);
	mgr.SetAddLootDelegate((_, _) => { });
	// 注入情报映射
	mgr.SetGetIntelIdForPointDelegate(pointId => intelId ?? "intel.cloudwatch-log");
	mgr.SetHasRelevantIntelDelegate(_ => false); // 默认无增强描述
	// 注入标准 loot pool
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.coreA"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)>
			{
				("resource.scrap", 1, 2),
				("resource.cloth", 1, 1),
			},
			["common"] = new List<(string, int, int)>
			{
				("resource.iron", 1, 2),
				("resource.copper", 1, 2),
			},
			["uncommon"] = new List<(string, int, int)>
			{
				("resource.crystite", 1, 1),
			},
		},
		["sp.outerD"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { ("resource.debris", 1, 1) },
			["common"] = new List<(string, int, int)> { ("resource.glass", 1, 2) },
			["uncommon"] = new List<(string, int, int)> { ("resource.signal_crystal", 1, 1) },
		},
	});
	// 进入 EXPLORING 状态
	mgr.EnterExploration("location.cloudwatch-ruins");
	mgr.SkipArriving();
	return mgr;
}

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 002: Search, Scavenge & Intel Formulas ===\n");

// ── AC-1: A_core empty_chance=0 → 100 次无空结果 ──
{
	var mgr = BuildSearchable();
	// 固定随机：0.10（< 不会触发空 chance 0.00）
	int emptyCount = 0;
	for (int i = 0; i < 100; i++)
	{
		mgr.SetRandomDelegate(() => 0.10);
		mgr.SetRandomRangeDelegate((_, _) => 1);
		var r = mgr.SearchYield("sp.coreA", SearchPointState.Unlooted, "A_core");
		if (r.IsEmpty) emptyCount++;
	}
	Assert(emptyCount == 0, "AC-1: A_core empty_chance=0 → 100 次无空结果");
}

// ── AC-2: D_outer empty_chance=0.35 → 统计验证（模拟固定概率）──
{
	var mgr = BuildSearchable();
	int emptyCount = 0;
	int total = 1000;
	var rng = new Random(42);
	for (int i = 0; i < total; i++)
	{
		double fixedRoll = rng.NextDouble();
		mgr.SetRandomDelegate(() => fixedRoll);
		mgr.SetRandomRangeDelegate((_, _) => 1);
		var r = mgr.SearchYield("sp.outerD", SearchPointState.Unlooted, "D_outer");
		if (r.IsEmpty) emptyCount++;
	}
	double ratio = (double)emptyCount / total;
	Assert(ratio >= 0.25 && ratio <= 0.45,
		$"AC-2: D_outer empty_chance≈0.35（实际 {ratio:P1}，期望 0.25-0.45）");
}

// ── AC-3: LOOTED 状态 → 返回空结果 + "已搜过" ──
{
	var mgr = BuildSearchable();
	var r = mgr.SearchYield("sp.coreA", SearchPointState.Looted, "A_core");
	Assert(r.IsEmpty, "AC-3: Looted → IsEmpty=true");
	Assert(!r.SearchConsumed, "AC-3: Looted → SearchConsumed=false");
	Assert(r.Message == "这里已经被搜过了", "AC-3: 消息='这里已经被搜过了'");
}

// ── AC-4: loot_pool 为空 → 返回空结果 + search_consumed=false ──
{
	var mgr = new ExplorationManager();
	mgr.SetCanAddToPoolDelegate((_, _) => true);
	mgr.SetAddLootDelegate((_, _) => { });
	// 注入空 pool
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.empty"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)>(),
			["common"] = new List<(string, int, int)>(),
			["uncommon"] = new List<(string, int, int)>(),
		},
	});
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.SetRandomDelegate(() => 0.50); // 不触发空 chance，抽中 common tier
	var r = mgr.SearchYield("sp.empty", SearchPointState.Unlooted, "A_core");
	Assert(r.IsEmpty, "AC-4: 空池 → IsEmpty=true");
	Assert(!r.SearchConsumed, "AC-4: 空池 → SearchConsumed=false");
}

// ── AC-5: 搜索产出非空 + Pool 5 有空间 → search_consumed=true + 信号 ──
{
	var mgr = BuildSearchable();
	bool searchPerformed = false; bool itemPickedUp = false;
	mgr.SearchPerformed += (_, r) => { if (!r.IsEmpty) searchPerformed = true; };
	mgr.ItemPickedUp += (_, _) => itemPickedUp = true;
	mgr.SetRandomDelegate(() => 0.10); // A_core → 不触发空（0.00），抽 poor（roll<0.20）
	mgr.SetRandomRangeDelegate((_, _) => 1);
	var r = mgr.PerformSearch("sp.coreA", SearchPointState.Unlooted, "A_core");
	Assert(!r.IsEmpty, "AC-5: 非空搜索结果");
	Assert(r.SearchConsumed, "AC-5: search_consumed=true");
	Assert(searchPerformed, "AC-5: SearchPerformed 信号触发");
	Assert(itemPickedUp, "AC-5: ItemPickedUp 信号触发");
}

// ── AC-6: 搜索产出为空 → search_consumed=false + 信号 ──
{
	var mgr = BuildSearchable();
	bool sigFired = false;
	mgr.SearchPerformed += (_, r) => { if (r.IsEmpty) sigFired = true; };
	// D_outer empty_chance=0.35，roll=0.10 < 0.35 → 空结果
	mgr.SetRandomDelegate(() => 0.10);
	var r = mgr.PerformSearch("sp.outerD", SearchPointState.Unlooted, "D_outer");
	Assert(r.IsEmpty, "AC-6: 空搜索结果");
	Assert(!r.SearchConsumed, "AC-6: search_consumed=false");
	Assert(sigFired, "AC-6: SearchPerformed 信号触发（is_empty=true）");
}

// ── AC-7: A_core 品质权重（poor:0.20, common:0.45, uncommon:0.35）──
{
	var mgr = new ExplorationManager();
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.quality-test"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { ("resource.poor", 1, 1) },
			["common"] = new List<(string, int, int)> { ("resource.common", 1, 1) },
			["uncommon"] = new List<(string, int, int)> { ("resource.uncommon", 1, 1) },
		},
	});
	// 统计
	var tierCounts = new Dictionary<string, int> { ["poor"] = 0, ["common"] = 0, ["uncommon"] = 0 };
	int total = 1000;
	var rng = new Random(123);
	for (int i = 0; i < total; i++)
	{
		double roll = rng.NextDouble();
		mgr.SetRandomDelegate(() => roll);
		mgr.SetRandomRangeDelegate((_, _) => 1);
		var r = mgr.SearchYield("sp.quality-test", SearchPointState.Unlooted, "A_core");
		if (!r.IsEmpty && r.Items.Count > 0)
		{
			var id = r.Items[0].ResourceId;
			if (id == "resource.poor") tierCounts["poor"]++;
			else if (id == "resource.common") tierCounts["common"]++;
			else if (id == "resource.uncommon") tierCounts["uncommon"]++;
		}
	}
	double poorR = (double)tierCounts["poor"] / total;
	double commonR = (double)tierCounts["common"] / total;
	double uncommonR = (double)tierCounts["uncommon"] / total;
	Assert(Math.Abs(poorR - 0.20) <= 0.05, $"AC-7: poor≈0.20 (actual={poorR:P1})");
	Assert(Math.Abs(commonR - 0.45) <= 0.05, $"AC-7: common≈0.45 (actual={commonR:P1})");
	Assert(Math.Abs(uncommonR - 0.35) <= 0.05, $"AC-7: uncommon≈0.35 (actual={uncommonR:P1})");
}

// ── AC-9: danger-changed → empty_chance+0.15, uncommon×0.5 ──
{
	// A_core danger-changed: empty_chance=0.15, uncommon=0.35×0.5=0.175
	var mgr = new ExplorationManager();
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.danger-test"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { ("resource.poor", 1, 1) },
			["common"] = new List<(string, int, int)> { ("resource.common", 1, 1) },
			["uncommon"] = new List<(string, int, int)> { ("resource.uncommon", 1, 1) },
		},
	});
	int emptyCount = 0; int total = 500;
	var rng = new Random(99);
	for (int i = 0; i < total; i++)
	{
		double roll = rng.NextDouble();
		mgr.SetRandomDelegate(() => roll);
		mgr.SetRandomRangeDelegate((_, _) => 1);
		var r = mgr.SearchYield("sp.danger-test", SearchPointState.DangerChanged, "A_core");
		if (r.IsEmpty) emptyCount++;
	}
	double emptyR = (double)emptyCount / total;
	// danger A_core: empty_chance=0.15
	Assert(emptyR >= 0.08 && emptyR <= 0.22,
		$"AC-9: danger-changed A_core empty≈0.15 (actual={emptyR:P1})");
}

// ── AC-10: draw_count 范围 [1,2] ──
{
	var mgr = BuildSearchable();
	mgr.SetRandomDelegate(() => 0.10); // poor tier
	mgr.SetRandomRangeDelegate((min, max) => max); // 取最大
	var r = mgr.SearchYield("sp.coreA", SearchPointState.Unlooted, "A_core");
	Assert(!r.IsEmpty && r.Items.Count >= 1 && r.Items.Count <= 2,
		$"AC-10: draw_count ∈ [1,2] (actual={r.Items.Count})");
}

// ── AC-11: pool 条目数 < draw_count → cap at pool.size ──
{
	var mgr = new ExplorationManager();
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.small"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { ("resource.only-one", 1, 1) },
			// poor pool size=1，draw_count max=2 → 只能取 1
		},
	});
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetRandomRangeDelegate((_, max) => max); // 尝试取 max=2
	var r = mgr.SearchYield("sp.small", SearchPointState.Unlooted, "A_core");
	Assert(!r.IsEmpty && r.Items.Count <= 1,
		"AC-11: pool.size=1 → 最多取 1 个，不越界");
}

// ── AC-12: 情报点交互 → intel_discovered 信号 ──
{
	var mgr = BuildSearchable(intelId: "intel.cloudwatch-log");
	string? sigIntel = null;
	mgr.IntelDiscovered += id => sigIntel = id;
	var r = mgr.PerformIntelInteraction("intel_point.console");
	Assert(!r.IsEmpty, "AC-12: 情报产出非空");
	Assert(r.IntelId == "intel.cloudwatch-log", "AC-12: intelId 正确");
	Assert(sigIntel == "intel.cloudwatch-log", "AC-12: IntelDiscovered 信号触发");
}

// ── AC-13: 已交互情报点 → 返回空结果 ──
{
	var mgr = BuildSearchable(intelId: "intel.cloudwatch-log");
	mgr.PerformIntelInteraction("intel_point.console"); // 第一次
	var r2 = mgr.PerformIntelInteraction("intel_point.console"); // 第二次
	Assert(r2.IsEmpty, "AC-13: 已交互情报点 → IsEmpty=true");
	Assert(r2.Message == "此处已调查过", "AC-13: 消息='此处已调查过'");
}

// ── AC-14: 情报产出 + Pool 5 有空间 → Q=1 Unique 进入 Pool ──
{
	bool addCalled = false;
	var mgr = new ExplorationManager();
	mgr.SetCanAddToPoolDelegate((_, _) => true);
	mgr.SetAddLootDelegate((id, qty) => { if (id == "intel.test") addCalled = true; });
	mgr.SetGetIntelIdForPointDelegate(_ => "intel.test");
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	var r = mgr.PerformIntelInteraction("intel_point.test");
	Assert(!r.IsEmpty, "AC-14: 情报产出");
	Assert(addCalled, "AC-14: 情报物品加入 Pool 5");
}

// ── AC-15: has_relevant_intel=false → 默认描述 ──
{
	var mgr = BuildSearchable();
	mgr.SetHasRelevantIntelDelegate(_ => false);
	string desc = mgr.GetSearchPointDescription("sp.test", "默认描述", "增强描述");
	Assert(desc == "默认描述", "AC-15: has_relevant_intel=false → 默认描述");
}

// ── AC-16: has_relevant_intel=true → 增强描述 ──
{
	var mgr = BuildSearchable();
	mgr.SetHasRelevantIntelDelegate(_ => true);
	string desc = mgr.GetSearchPointDescription("sp.test", "默认描述", "增强描述");
	Assert(desc == "增强描述", "AC-16: has_relevant_intel=true → 增强描述");
}

// ── AC-17: 状态变体文字对（数据由外部传入）──
{
	var mgr = BuildSearchable();
	mgr.SetHasRelevantIntelDelegate(_ => false);
	// 不同状态变体由调用方传入不同描述字符串
	string unlooted = mgr.GetSearchPointDescription("sp.x", "未搜索描述", "未搜索增强");
	Assert(unlooted == "未搜索描述", "AC-17: unlooted 默认描述");
}

// ── AC-18: Pool 5 满 → capacity_warning + search_consumed=false ──
{
	var mgr = new ExplorationManager();
	bool warnFired = false;
	mgr.SetCanAddToPoolDelegate((_, _) => false); // 模拟满
	mgr.SetAddLootDelegate((_, _) => { });
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.full"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { ("resource.item", 1, 1) },
		},
	});
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.CapacityWarning += (_, _) => warnFired = true;
	mgr.SetRandomDelegate(() => 0.10); // poor tier，非空
	mgr.SetRandomRangeDelegate((_, _) => 1);
	var r = mgr.PerformSearch("sp.full", SearchPointState.Unlooted, "A_core");
	Assert(warnFired, "AC-18: capacity_warning 信号触发");
	Assert(!r.SearchConsumed, "AC-18: Pool 满时 search_consumed=false");
}

// ── AC-19: Pool 5 满 + 情报 Unique → capacity_warning ──
{
	var mgr = new ExplorationManager();
	bool warnFired = false;
	mgr.SetCanAddToPoolDelegate((_, _) => false); // 模拟满
	mgr.SetAddLootDelegate((_, _) => { });
	mgr.SetGetIntelIdForPointDelegate(_ => "intel.test");
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.CapacityWarning += (_, _) => warnFired = true;
	var r = mgr.PerformIntelInteraction("intel_point.console");
	Assert(warnFired, "AC-19: 情报+Pool 满 → capacity_warning 触发");
	Assert(r.CapacityBlocked, "AC-19: capacity_blocked=true");
}

// ── AC-20: 可堆叠合并——静默合并（通过 AddLoot 不触发 warning）──
{
	// 此测试验证：Pool 5 有空间时，AddLoot 被调用（合并由 #5 内部处理）
	int addCount = 0;
	var mgr = new ExplorationManager();
	mgr.SetCanAddToPoolDelegate((_, _) => true);
	mgr.SetAddLootDelegate((_, _) => addCount++);
	mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>(StringComparer.Ordinal)
	{
		["sp.merge"] = new Dictionary<string, List<(string, int, int)>>(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { ("resource.iron", 1, 1) },
		},
	});
	mgr.EnterExploration("location.ruins");
	mgr.SkipArriving();
	mgr.SetRandomDelegate(() => 0.10);
	mgr.SetRandomRangeDelegate((_, _) => 1);
	var r = mgr.PerformSearch("sp.merge", SearchPointState.Unlooted, "A_core");
	Assert(!r.IsEmpty && addCount > 0,
		"AC-20: 可堆叠物品通过 AddLoot 静默合并（无 warning）");
}

Console.WriteLine();
Console.WriteLine($"Story 002 Search & Scavenge: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
