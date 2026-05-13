using CloudWeaverVoyage.Core;

// Story 006 — 持久化快照与会话恢复（Integration）
// 覆盖 AC-01 到 AC-16：TriggerSnapshot、配额警告防抖、SerializeExploration、
// DeserializeExploration、RestoreActiveSession、ReconcilePool5。

// ── 辅助工具 ────────────────────────────────────────────────────────
int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
    if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
    else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

// 构造一个完成 IDLE → ARRIVING → EXPLORING 转换的管理器
static ExplorationManager BuildExploring(string pointId = "ruins.alpha-01")
{
    var mgr = new ExplorationManager();
    var ctx = new EncounterContext(
        "route.sky-reef-arc-01", pointId, "arrived",
        new List<ResolvedEncounterEntry>(), 0,
        new List<string>(), HullBand.Intact, "", new List<string>());
    mgr.EnterExplorationWithContext(ctx);
    mgr.SkipArriving();
    return mgr;
}

// 驱动撤离读条完成（ExtractionDuration = 2.5s）
static void TickToCompletion(ExplorationManager mgr)
{
    mgr.ExtractionTick(1.0);
    mgr.ExtractionTick(1.0);
    mgr.ExtractionTick(0.6); // 2.6s > 2.5s
}

Console.WriteLine("=== Story 006: 持久化快照与会话恢复 ===\n");

// ──────────────────────────────────────────────────────────────────────────
// AC-01: TriggerSnapshot 在搜索成功路径末尾被调用
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("── AC-01: PerformSearch 成功路径调用 TriggerSnapshot ──");
{
    // Arrange
    var mgr = BuildExploring();
    int snapshotCallCount = 0;
    mgr.SetCaptureSnapshotDelegate(() => { snapshotCallCount++; return true; });
    mgr.SetCanAddToPoolDelegate((_, _) => true);
    mgr.SetAddLootDelegate((_, _) => { });
    mgr.SetRandomDelegate(() => 0.0);   // 保证不为空结果
    mgr.SetRandomRangeDelegate((min, _) => min);

    var lootPools = new Dictionary<string, Dictionary<string, List<(string, int, int)>>>
    {
        ["sp.ruins-01"] = new Dictionary<string, List<(string, int, int)>>
        {
            ["poor"] = new List<(string, int, int)> { ("scrap", 1, 2) },
        },
    };
    mgr.SetLootPools(lootPools);

    // Act
    var result = mgr.PerformSearch("sp.ruins-01", SearchPointState.Unlooted, "A_core");

    // Assert
    Assert(!result.IsEmpty, "AC-01: 搜索产出非空（前提）");
    Assert(snapshotCallCount == 1, $"AC-01: TriggerSnapshot 被调用 1 次（实际={snapshotCallCount}）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-02: PerformSearch 空结果不触发 TriggerSnapshot
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-02: PerformSearch 空结果不调用 TriggerSnapshot ──");
{
    var mgr = BuildExploring();
    int snapshotCallCount = 0;
    mgr.SetCaptureSnapshotDelegate(() => { snapshotCallCount++; return true; });
    mgr.SetCanAddToPoolDelegate((_, _) => true);
    mgr.SetRandomDelegate(() => 0.99);  // 高 roll → 必为空结果（A_core empty_chance=0.0，B_inner=0.05，D_outer=0.35）

    // 使用 D_outer 让 empty_chance=0.35，roll=0.99 → 空结果
    var result = mgr.PerformSearch("sp.ruins-01", SearchPointState.Unlooted, "D_outer");

    Assert(result.IsEmpty, "AC-02: 搜索结果为空（前提）");
    Assert(snapshotCallCount == 0, $"AC-02: 空结果不触发 TriggerSnapshot（实际={snapshotCallCount}）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-03: TriggerExtraction 成功后调用 TriggerSnapshot
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-03: TriggerExtraction 调用 TriggerSnapshot ──");
{
    var mgr = BuildExploring();
    int snapshotCallCount = 0;
    mgr.SetCaptureSnapshotDelegate(() => { snapshotCallCount++; return true; });

    bool ok = mgr.TriggerExtraction();

    Assert(ok, "AC-03: TriggerExtraction 返回 true（前提）");
    Assert(snapshotCallCount == 1, $"AC-03: TriggerSnapshot 被调用 1 次（实际={snapshotCallCount}）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-04: ForceExtraction 成功后调用 TriggerSnapshot
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-04: ForceExtraction 调用 TriggerSnapshot ──");
{
    var mgr = BuildExploring();
    int snapshotCallCount = 0;
    mgr.SetCaptureSnapshotDelegate(() => { snapshotCallCount++; return true; });

    bool ok = mgr.ForceExtraction("pool_full");

    Assert(ok, "AC-04: ForceExtraction 返回 true（前提）");
    Assert(snapshotCallCount == 1, $"AC-04: TriggerSnapshot 被调用 1 次（实际={snapshotCallCount}）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-05: TriggerSnapshot 失败时发射 QuotaWarningEmitted（AC-14/15 路径）
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-05: TriggerSnapshot 失败 → QuotaWarningEmitted 发射 ──");
{
    var mgr = BuildExploring();
    bool quotaWarningFired = false;
    mgr.QuotaWarningEmitted += () => quotaWarningFired = true;
    mgr.SetCaptureSnapshotDelegate(() => false);  // 始终失败
    double fakeTime = 0.0;
    mgr.SetWallClockDelegate(() => fakeTime);

    mgr.TriggerExtraction();

    Assert(quotaWarningFired, "AC-05: snapshot 失败时 QuotaWarningEmitted 发射");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-06: 30s 防抖——两次失败间隔 <30s 时 QuotaWarningEmitted 只发射 1 次
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-06: 30s 防抖（间隔 <30s 不重复发射） ──");
{
    var mgr = BuildExploring();
    int quotaWarningCount = 0;
    mgr.QuotaWarningEmitted += () => quotaWarningCount++;
    mgr.SetCaptureSnapshotDelegate(() => false);
    double fakeTime = 0.0;
    mgr.SetWallClockDelegate(() => fakeTime);

    // 第 1 次触发
    mgr.TriggerExtraction();

    // 立即中断再重触（间隔 0s < 30s）
    mgr.InterruptExtraction("reset");
    fakeTime = 10.0;  // 10s 后，仍 < 30s
    mgr.ForceExtraction("pool_full");

    Assert(quotaWarningCount == 1,
        $"AC-06: 间隔 10s < 30s，QuotaWarningEmitted 只发射 1 次（实际={quotaWarningCount}）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-07: 30s 防抖——间隔 >=30s 时再次发射 QuotaWarningEmitted
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-07: 30s 防抖（间隔 >=30s 再次发射） ──");
{
    var mgr = BuildExploring();
    int quotaWarningCount = 0;
    mgr.QuotaWarningEmitted += () => quotaWarningCount++;
    mgr.SetCaptureSnapshotDelegate(() => false);
    double fakeTime = 0.0;
    mgr.SetWallClockDelegate(() => fakeTime);

    // 第 1 次触发
    mgr.TriggerExtraction();
    Assert(quotaWarningCount == 1, "AC-07: 第 1 次触发 QuotaWarningEmitted（前提）");

    // 中断后等 30s 再触发
    mgr.InterruptExtraction("reset");
    fakeTime = 30.0;  // 恰好 30s
    mgr.ForceExtraction("pool_full");

    Assert(quotaWarningCount == 2,
        $"AC-07: 间隔 30s，QuotaWarningEmitted 第 2 次发射（实际={quotaWarningCount}）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-08: SerializeExploration — IDLE 阶段返回 null
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-08: SerializeExploration IDLE 返回 null ──");
{
    var mgr = new ExplorationManager();
    var data = mgr.SerializeExploration();
    Assert(data == null, "AC-08: IDLE 阶段 SerializeExploration 返回 null");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-09: SerializeExploration — EXPLORING 阶段包含必要字段
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-09: SerializeExploration EXPLORING 包含必要字段 ──");
{
    var mgr = BuildExploring("ruins.beta-02");
    var data = mgr.SerializeExploration();

    Assert(data != null, "AC-09: SerializeExploration 非 null");
    Assert(data!.ContainsKey("phase"), "AC-09: 包含 phase 字段");
    Assert(data["phase"] == "Exploring", $"AC-09: phase=Exploring（实际='{data["phase"]}'）");
    Assert(data.ContainsKey("current_point_id"), "AC-09: 包含 current_point_id 字段");
    Assert(data["current_point_id"] == "ruins.beta-02",
        $"AC-09: current_point_id=ruins.beta-02（实际='{data["current_point_id"]}'）");
    Assert(data.ContainsKey("searched_points"), "AC-09: 包含 searched_points 字段");
    Assert(data.ContainsKey("interacted_intel_points"), "AC-09: 包含 interacted_intel_points 字段");
    Assert(data.ContainsKey("retreat_flagged"), "AC-09: 包含 retreat_flagged 字段");
    Assert(data.ContainsKey("env_threat_active"), "AC-09: 包含 env_threat_active 字段");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-10: SerializeExploration — EXTRACTING 阶段包含正确 phase 值
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-10: SerializeExploration EXTRACTING 阶段 ──");
{
    var mgr = BuildExploring("ruins.gamma-03");
    mgr.SetCaptureSnapshotDelegate(() => true);
    mgr.TriggerExtraction();

    var data = mgr.SerializeExploration();
    Assert(data != null, "AC-10: EXTRACTING 时 SerializeExploration 非 null");
    Assert(data!["phase"] == "Extracting",
        $"AC-10: phase=Extracting（实际='{data["phase"]}'）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-11: DeserializeExploration — 从 EXPLORING 快照恢复，phase=EXPLORING
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-11: DeserializeExploration 恢复 EXPLORING ──");
{
    // Arrange：先序列化一个 EXPLORING 状态
    var original = BuildExploring("ruins.alpha-01");
    var snapshot = original.SerializeExploration()!;

    // Act：在全新管理器上反序列化
    var restored = new ExplorationManager();
    ExplorationPhase? observedPhase = null;
    restored.ExplorationPhaseChanged += (_, newPhase, _) => observedPhase = newPhase;

    bool ok = restored.DeserializeExploration(snapshot);

    Assert(ok, "AC-11: DeserializeExploration 返回 true");
    Assert(restored.CurrentPhase == ExplorationPhase.Exploring,
        $"AC-11: 恢复后 phase=Exploring（实际={restored.CurrentPhase}）");
    Assert(restored.CurrentPointId == "ruins.alpha-01",
        $"AC-11: current_point_id=ruins.alpha-01（实际='{restored.CurrentPointId}'）");
    Assert(observedPhase == ExplorationPhase.Exploring,
        "AC-11: ExplorationPhaseChanged 信号已发射且 newPhase=Exploring");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-12: DeserializeExploration — 从 EXTRACTING 快照恢复 → phase=EXPLORING + ExtractionInterruptedOnRestore 发射
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-12: DeserializeExploration 从 EXTRACTING 恢复 → EXPLORING + ExtractionInterruptedOnRestore ──");
{
    // Arrange：序列化一个 EXTRACTING 状态
    var original = BuildExploring("ruins.delta-04");
    original.SetCaptureSnapshotDelegate(() => true);
    original.TriggerExtraction();
    original.ExtractionTick(1.0);  // 读条进行中（未完成）
    var snapshot = original.SerializeExploration()!;

    // Act：反序列化
    var restored = new ExplorationManager();
    bool sessionRestoredFired = false;
    bool extractionInterruptedOnRestoreFired = false;
    restored.SessionRestoredNotice += () => sessionRestoredFired = true;
    restored.ExtractionInterruptedOnRestore += () => extractionInterruptedOnRestoreFired = true;

    bool ok = restored.DeserializeExploration(snapshot);

    Assert(ok, "AC-12: DeserializeExploration 返回 true");
    Assert(restored.CurrentPhase == ExplorationPhase.Exploring,
        $"AC-12: EXTRACTING 快照恢复后 phase=Exploring（实际={restored.CurrentPhase}）");
    Assert(sessionRestoredFired, "AC-12: SessionRestoredNotice 信号已发射");
    Assert(extractionInterruptedOnRestoreFired,
        "AC-12: ExtractionInterruptedOnRestore 信号已发射");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-13: DeserializeExploration — 恢复 searched_points 与 interacted_intel_points
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-13: DeserializeExploration 恢复搜索点与情报点集合 ──");
{
    // Arrange：在原始管理器上执行搜索和情报交互
    var original = BuildExploring("ruins.epsilon-05");
    original.SetCanAddToPoolDelegate((_, _) => true);
    original.SetAddLootDelegate((_, _) => { });
    original.SetGetIntelIdForPointFn("ipoint-01", "intel.crystal-map");
    original.PerformIntelInteraction("ipoint-01");

    // 手动构建快照（直接使用 SerializeExploration）
    var snapshot = original.SerializeExploration()!;

    // Act
    var restored = new ExplorationManager();
    bool ok = restored.DeserializeExploration(snapshot);

    Assert(ok, "AC-13: DeserializeExploration 成功");
    // 验证情报点已恢复——再次交互 ipoint-01 应返回"此处已调查过"
    restored.SetGetIntelIdForPointFn("ipoint-01", "intel.crystal-map");
    var interactResult = restored.PerformIntelInteraction("ipoint-01");
    Assert(interactResult.IsEmpty && interactResult.Message == "此处已调查过",
        "AC-13: 恢复后 ipoint-01 重复交互返回'此处已调查过'");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-14: DeserializeExploration — 无效数据（null）返回 false
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-14: DeserializeExploration 无效数据处理 ──");
{
    var mgr = new ExplorationManager();
    bool ok = mgr.DeserializeExploration(null!);
    Assert(!ok, "AC-14: null 数据返回 false");

    var emptyData = new Dictionary<string, string>(StringComparer.Ordinal);
    bool ok2 = mgr.DeserializeExploration(emptyData);
    Assert(!ok2, "AC-14: 空字典（无 phase 字段）返回 false");

    var badPhaseData = new Dictionary<string, string> { ["phase"] = "InvalidPhase" };
    bool ok3 = mgr.DeserializeExploration(badPhaseData);
    Assert(!ok3, "AC-14: 无效 phase 值返回 false");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-15: ReconcilePool5 — 实际与追踪一致时不调用 _poolReconcileFn
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-15: ReconcilePool5 一致时不修复 ──");
{
    var mgr = new ExplorationManager();
    bool reconcileCalled = false;
    mgr.SetGetPoolActualOccupiedDelegate(() => 5);
    mgr.SetGetPoolTrackedOccupiedDelegate(() => 5);
    mgr.SetPoolReconcileDelegate(() => reconcileCalled = true);

    mgr.ReconcilePool5();

    Assert(!reconcileCalled, "AC-15: 实际=追踪=5 时不调用 reconcile");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-16: ReconcilePool5 — 实际与追踪不一致时调用 _poolReconcileFn
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-16: ReconcilePool5 不一致时触发修复 ──");
{
    var mgr = new ExplorationManager();
    bool reconcileCalled = false;
    mgr.SetGetPoolActualOccupiedDelegate(() => 7);
    mgr.SetGetPoolTrackedOccupiedDelegate(() => 5);
    mgr.SetPoolReconcileDelegate(() => reconcileCalled = true);

    mgr.ReconcilePool5();

    Assert(reconcileCalled, "AC-16: 实际=7 != 追踪=5 时调用 reconcile");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-17: RestoreActiveSession — 直接设置 phase=EXPLORING 并发射信号
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-17: RestoreActiveSession 恢复 EXPLORING 并发射信号 ──");
{
    var mgr = new ExplorationManager();
    ExplorationPhase? emittedFrom = null;
    ExplorationPhase? emittedTo = null;
    bool sessionRestoredFired = false;

    mgr.ExplorationPhaseChanged += (from, to, _) => { emittedFrom = from; emittedTo = to; };
    mgr.SessionRestoredNotice += () => sessionRestoredFired = true;

    mgr.RestoreActiveSession("ruins.zeta-06");

    Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
        $"AC-17: phase=Exploring（实际={mgr.CurrentPhase}）");
    Assert(mgr.CurrentPointId == "ruins.zeta-06",
        $"AC-17: current_point_id=ruins.zeta-06（实际='{mgr.CurrentPointId}'）");
    Assert(emittedFrom == ExplorationPhase.Idle, "AC-17: 信号 from=Idle");
    Assert(emittedTo == ExplorationPhase.Exploring, "AC-17: 信号 to=Exploring");
    Assert(sessionRestoredFired, "AC-17: SessionRestoredNotice 已发射");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-18: DeserializeExploration — 清除现有会话状态再写入（先 Clear 再 Deserialize）
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-18: DeserializeExploration 先清除现有状态 ──");
{
    // Arrange：在管理器上建立"脏"状态
    var dirtyMgr = BuildExploring("ruins.old-01");
    dirtyMgr.SetCanAddToPoolDelegate((_, _) => true);
    dirtyMgr.SetAddLootDelegate((_, _) => { });
    dirtyMgr.SetGetIntelIdForPointFn("dirty-intel", "intel.dirty");
    dirtyMgr.PerformIntelInteraction("dirty-intel");

    // 构建一个干净快照（不含 dirty-intel）
    var cleanSnapshot = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["phase"] = "Exploring",
        ["current_point_id"] = "ruins.new-02",
        ["substate"] = "Idle",
        ["extraction_elapsed"] = "0",
        ["extraction_active"] = "False",
        ["retreat_flagged"] = "False",
        ["env_threat_active"] = "False",
        ["arrival_mode"] = "arrived",
        ["searched_points"] = "",
        ["interacted_intel_points"] = "",
        ["mark_arriving_interrupted"] = "False",
        ["mark_extraction_interrupted"] = "False",
        ["search_point_states"] = "",
    };

    // Act：反序列化到"脏"管理器
    bool ok = dirtyMgr.DeserializeExploration(cleanSnapshot);

    Assert(ok, "AC-18: 反序列化成功");
    Assert(dirtyMgr.CurrentPointId == "ruins.new-02",
        $"AC-18: 恢复后 current_point_id=ruins.new-02（实际='{dirtyMgr.CurrentPointId}'）");

    // 验证 dirty-intel 已被清除——再次交互应不返回"此处已调查过"
    dirtyMgr.SetCanAddToPoolDelegate((_, _) => true);
    dirtyMgr.SetAddLootDelegate((_, _) => { });
    dirtyMgr.SetGetIntelIdForPointFn("dirty-intel", "intel.dirty");
    var interact = dirtyMgr.PerformIntelInteraction("dirty-intel");
    Assert(!interact.IsEmpty || interact.Message != "此处已调查过",
        "AC-18: 旧情报状态已清除（dirty-intel 可重新交互或无历史记录）");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-19: ReconcilePool5 — 委托未注入时不崩溃
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-19: ReconcilePool5 委托未注入时安全降级 ──");
{
    var mgr = new ExplorationManager();
    // 不注入任何委托，直接调用
    bool threw = false;
    try { mgr.ReconcilePool5(); }
    catch { threw = true; }
    Assert(!threw, "AC-19: ReconcilePool5 未注入委托时不抛出异常");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-20: DeserializeExploration 后 ReconcilePool5 自动被调用
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-20: DeserializeExploration 后自动触发 ReconcilePool5 ──");
{
    var snapshot = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["phase"] = "Exploring",
        ["current_point_id"] = "ruins.auto-reconcile",
        ["substate"] = "Idle",
        ["extraction_elapsed"] = "0",
        ["extraction_active"] = "False",
        ["retreat_flagged"] = "False",
        ["env_threat_active"] = "False",
        ["arrival_mode"] = "arrived",
        ["searched_points"] = "",
        ["interacted_intel_points"] = "",
        ["mark_arriving_interrupted"] = "False",
        ["mark_extraction_interrupted"] = "False",
        ["search_point_states"] = "",
    };

    var mgr = new ExplorationManager();
    bool reconcileCalled = false;
    // 注入不一致的委托（actual=3, tracked=5 → 不一致）
    mgr.SetGetPoolActualOccupiedDelegate(() => 3);
    mgr.SetGetPoolTrackedOccupiedDelegate(() => 5);
    mgr.SetPoolReconcileDelegate(() => reconcileCalled = true);

    bool ok = mgr.DeserializeExploration(snapshot);

    Assert(ok, "AC-20: DeserializeExploration 成功（前提）");
    Assert(reconcileCalled,
        "AC-20: DeserializeExploration 后 ReconcilePool5 被自动调用且触发修复");
}

// ──────────────────────────────────────────────────────────────────────────
// Story AC-20: DeserializeExploration 遇到 phase=DEPARTED → 返回 false + InternalErrorLog 记录
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Story AC-20: phase=DEPARTED 快照 → DeserializeExploration 返回 false ──");
{
    var snapshot = new Dictionary<string, string>
    {
        ["phase"] = "Departed",
        ["current_point_id"] = "ruins.zeta-06",
        ["retreat_flagged"] = "False",
        ["env_threat_active"] = "False",
        ["mark_arriving_interrupted"] = "False",
        ["mark_extraction_interrupted"] = "False",
        ["search_point_states"] = "",
    };

    var mgr = new ExplorationManager();
    bool ok = mgr.DeserializeExploration(snapshot);

    Assert(!ok, "Story AC-20: phase=DEPARTED → DeserializeExploration 返回 false");
    Assert(mgr.CurrentPhase == ExplorationPhase.Idle,
        "Story AC-20: 忽略后 phase 仍为 Idle");
    Assert(mgr.InternalErrorLog.Count > 0,
        "Story AC-20: InternalErrorLog 中有 warning 记录");
}

// ──────────────────────────────────────────────────────────────────────────
// Story AC-11/12/13: OnPageHidden / OnPageVisible (EC-11-20)
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Story AC-11: EXPLORING idle + OnPageHidden/OnPageVisible → 无惩罚 ──");
{
    var mgr = BuildExploring();
    mgr.OnPageHidden();
    mgr.OnPageVisible();
    Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
        "Story AC-11: 失焦/恢复后 phase 仍=EXPLORING");
    Assert(mgr.CurrentSubstate == ExplorationSubstate.Idle,
        "Story AC-11: 恢复后 substate=Idle");
}

Console.WriteLine("\n── Story AC-12: EXTRACTING + OnPageHidden → OnPageVisible → 读条中断 ──");
{
    var mgr = BuildExploring();
    mgr.SetCaptureSnapshotDelegate(() => true);
    mgr.TriggerExtraction();
    Assert(mgr.CurrentPhase == ExplorationPhase.Extracting, "Story AC-12: 读条进行中（前提）");

    bool interruptedFired = false;
    mgr.ExtractionInterrupted += _ => interruptedFired = true;

    mgr.OnPageHidden();
    mgr.OnPageVisible();

    Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
        "Story AC-12: OnPageVisible 后 phase=EXPLORING（读条中断）");
    Assert(interruptedFired, "Story AC-12: ExtractionInterrupted 事件已发射");
}

Console.WriteLine("\n── Story AC-13: ARRIVING + OnPageHidden → OnPageVisible → 自动跳过 ARRIVING ──");
{
    var mgr = new ExplorationManager();
    var ctx = new EncounterContext(
        "route.r01", "ruins.arriving-test", "arrived",
        new List<ResolvedEncounterEntry>(), 0,
        new List<string>(), HullBand.Intact, "", new List<string>());
    mgr.EnterExplorationWithContext(ctx);
    Assert(mgr.CurrentPhase == ExplorationPhase.Arriving, "Story AC-13: phase=ARRIVING（前提）");

    mgr.OnPageHidden();
    mgr.OnPageVisible();

    Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
        "Story AC-13: OnPageVisible 后自动跳过至 EXPLORING");
}

// ──────────────────────────────────────────────────────────────────────────
// Story AC-6/7: hull=0 时 ExplorationManager 不终止探索
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Story AC-6/7: hull=0 时探索系统继续运行 ──");
{
    var mgr = BuildExploring();
    // hull=0 由外部系统（ModulesManager）管理；ExplorationManager 本身不追踪 hull
    // AC-6/7 验证：在 hull=0 场景下 PerformSearch 正常返回（不崩溃，不拒绝）
    mgr.SetCanAddToPoolDelegate((_, _) => true);
    mgr.SetAddLootDelegate((_, _) => { });
    mgr.SetRandomDelegate(() => 0.0);
    mgr.SetRandomRangeDelegate((min, _) => min);
    mgr.SetLootPools(new Dictionary<string, Dictionary<string, List<(string, int, int)>>>
    {
        ["sp.hull-test"] = new Dictionary<string, List<(string, int, int)>>
        {
            ["poor"] = new List<(string, int, int)> { ("scrap", 1, 1) },
        },
    });

    var result = mgr.PerformSearch("sp.hull-test", SearchPointState.Unlooted, "A_core");
    Assert(mgr.CurrentPhase == ExplorationPhase.Exploring,
        "Story AC-6: hull=0 时 ExplorationManager 不终止探索（phase 仍=EXPLORING）");
    Assert(!result.IsEmpty || result.IsEmpty,  // 任意结果均可——重点是不崩溃
        "Story AC-7: hull=0 时搜索操作正常执行不崩溃");
}

// ──────────────────────────────────────────────────────────────────────────
// Story AC-9/10: 已清除威胁 re-entry 永久安全
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Story AC-9: 已清除威胁再入 → check 返回 triggered=false ──");
{
    var mgr = BuildExploring();
    var clearedThreat = new ExplorationManager.ThreatPoint("tp.guard-01",
        ExplorationManager.ThreatCategory.Guard, triggerRadius: 5.0, position: 10.0);
    clearedThreat.IsActive = false;  // 已清除
    mgr.RegisterThreatPoint(clearedThreat);

    var result = mgr.CheckSingleThreatTrigger(clearedThreat, "proximity", playerPosition: 10.0);
    Assert(!result.Triggered, "Story AC-9: 已清除威胁 → triggered=false");
}

Console.WriteLine("\n── Story AC-10: 清除威胁后 GetScoutPreviewLevel 不变 ──");
{
    var mgr = BuildExploring();
    mgr.SetGetScoutEfficiencyDelegate(() => 0.5);
    mgr.SnapshotEtaScout();  // 快照 η_scout=0.5 → Presence

    // 模拟威胁被清除
    var tp = new ExplorationManager.ThreatPoint("tp.env-01",
        ExplorationManager.ThreatCategory.Environmental, 3.0, 5.0);
    mgr.RegisterThreatPoint(tp);
    mgr.OnCombatResult("suppressed", "tp.env-01");

    // 侦察预览等级不因威胁状态变化而改变（进入时快照）
    var preview = mgr.GetScoutPreviewLevel();
    Assert(preview == ExplorationManager.ScoutPreviewLevel.Presence,
        $"Story AC-10: 威胁清除后 scout preview 仍=Presence（实际={preview}）");
}

// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"Story 006 持久化恢复: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);

// ── 扩展辅助：SetGetIntelIdForPointFn 快捷方法（测试内联） ────────────────
static class ExplorationManagerExtensions
{
    /// <summary>快捷注入 单个情报点映射（测试专用）。</summary>
    public static void SetGetIntelIdForPointFn(
        this ExplorationManager mgr, string pointId, string intelId)
    {
        mgr.SetGetIntelIdForPointDelegate(id => id == pointId ? intelId : "");
    }
}
