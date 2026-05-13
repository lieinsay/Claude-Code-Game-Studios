using CloudWeaverVoyage.Core;

// Story 005 — Extraction, Settlement & State Variant Transition (Integration)
// 覆盖 AC-1 到 AC-22 全部验收标准

// ── 辅助工具 ────────────────────────────────────────────────────────
int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
    if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
    else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

static ExplorationManager BuildMgr() => new ExplorationManager();

// 构造一个处于 EXPLORING 状态的管理器（IDLE → ARRIVING → EXPLORING）
static ExplorationManager BuildExploring(string pointId = "sp.ruins-01")
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

// 驱动撤离读条直到完成（2.5s）
static void TickToCompletion(ExplorationManager mgr)
{
    mgr.ExtractionTick(1.0);
    mgr.ExtractionTick(1.0);
    mgr.ExtractionTick(0.6); // 2.6s > ExtractionDuration=2.5
}

Console.WriteLine("=== Story 005: Extraction, Settlement & State Variant Transition ===\n");

// ──────────────────────────────────────────────────────────────────────────
// AC-1: TriggerExtraction → session_phase=EXTRACTING + ExtractionStarted 发射
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("── AC-1: TriggerExtraction 状态机 ──");
{
    var mgr = BuildExploring();
    bool extractionStartedFired = false;
    string? startedReason = null;
    mgr.ExtractionStarted += r => { extractionStartedFired = true; startedReason = r; };

    bool result = mgr.TriggerExtraction();
    Assert(result, "AC-1: TriggerExtraction 返回 true");
    Assert(mgr.CurrentPhase == ExplorationPhase.Extracting, "AC-1: phase=EXTRACTING");
    Assert(extractionStartedFired, "AC-1: ExtractionStarted 事件已发射");
    Assert(startedReason == "player_initiated", "AC-1: 原因='player_initiated'");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-2: 读条中途 InterruptExtraction → 进度重置 + 回 EXPLORING (Threatened substate)
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-2: 中途中断撤离 ──");
{
    var mgr = BuildExploring();
    bool interruptedFired = false;
    mgr.ExtractionInterrupted += _ => interruptedFired = true;

    mgr.TriggerExtraction();
    mgr.ExtractionTick(1.0); // 读条进行中（1.0s < 2.5s）
    Assert(mgr.CurrentPhase == ExplorationPhase.Extracting, "AC-2: 读条中阶段=EXTRACTING");

    mgr.InterruptExtraction("threat");
    Assert(mgr.CurrentPhase == ExplorationPhase.Exploring, "AC-2: 中断后 phase=EXPLORING");
    Assert(mgr.ExtractionElapsed == 0, "AC-2: 读条进度重置为 0");
    Assert(interruptedFired, "AC-2: ExtractionInterrupted 事件已发射");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-3: 被打断后可再次 TriggerExtraction
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-3: 打断后可再次触发撤离 ──");
{
    var mgr = BuildExploring();
    mgr.TriggerExtraction();
    mgr.ExtractionTick(0.5);
    mgr.InterruptExtraction("threat");

    bool secondTrigger = mgr.TriggerExtraction();
    Assert(secondTrigger, "AC-3: 打断后再次 TriggerExtraction 返回 true");
    Assert(mgr.CurrentPhase == ExplorationPhase.Extracting, "AC-3: 再次触发后 phase=EXTRACTING");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-4: 2.5s 完成 → ExtractionCompleted 发射（phase → DEPARTED）
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-4: 读条完成 → ExtractionCompleted 发射 ──");
{
    var mgr = BuildExploring();
    bool completedFired = false;
    mgr.ExtractionCompleted += (_, _) => completedFired = true;

    mgr.TriggerExtraction();
    TickToCompletion(mgr);
    Assert(mgr.CurrentPhase == ExplorationPhase.Departed, "AC-4: 完成后 phase=DEPARTED");
    Assert(completedFired, "AC-4: ExtractionCompleted 事件已发射");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-5: λ_success=0.08, basic_supply×20 → ComputeLoss(20, 0.08)=2, 保留 18
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-5 ~ AC-11: ComputeLoss 与损耗结算公式 ──");
{
    int loss = ExplorationManager.ComputeLoss(20, ExplorationManager.LambdaSuccess);
    // ceil(20 × 0.08) = ceil(1.6) = 2
    Assert(loss == 2, "AC-5: ComputeLoss(20, 0.08)=2");

    var mgr = BuildMgr();
    var stacks = new List<CarriedStack>
    {
        new CarriedStack("basic_supply", 20, false, 99),
    };
    var result = mgr.ExtractionLossSettlement(stacks, retreatFlagged: false);
    Assert(result.Transferred.Count == 1, "AC-5: 转移列表含 1 条");
    Assert(result.Transferred[0].Quantity == 18, "AC-5: 保留 18");
    Assert(result.Transferred[0].Lost == 2, "AC-5: 损耗字段=2");
    Assert(result.TotalLostQty == 2, "AC-5: TotalLostQty=2");
}

// AC-6: retreat_flagged=true, λ_forced=0.25, basic_supply×20 → ComputeLoss(20, 0.25)=5, 保留 15
{
    int loss = ExplorationManager.ComputeLoss(20, ExplorationManager.LambdaForced);
    // ceil(20 × 0.25) = ceil(5) = 5
    Assert(loss == 5, "AC-6: ComputeLoss(20, 0.25)=5");

    var mgr = BuildMgr();
    var stacks = new List<CarriedStack>
    {
        new CarriedStack("basic_supply", 20, false, 99),
    };
    var result = mgr.ExtractionLossSettlement(stacks, retreatFlagged: true);
    Assert(result.Transferred[0].Quantity == 15, "AC-6: 保留 15（retreat_flagged）");
    Assert(result.TotalLostQty == 5, "AC-6: TotalLostQty=5");
}

// AC-7: Unique 物品 (IsUnique=true, MaxStack=1) → lost=0，全量转移
{
    var mgr = BuildMgr();
    var stacks = new List<CarriedStack>
    {
        new CarriedStack("artifact.crystal-lens", 1, true, 1),
    };
    var result = mgr.ExtractionLossSettlement(stacks, retreatFlagged: false);
    Assert(result.Transferred.Count == 1, "AC-7: 转移列表含 1 条");
    Assert(result.Transferred[0].Quantity == 1, "AC-7: Unique 物品全量转移，Qty=1");
    Assert(result.Transferred[0].Lost == 0, "AC-7: Unique 物品 Lost=0");
    Assert(result.Lost.Count == 0, "AC-7: 损耗列表为空");
    Assert(result.TotalLostQty == 0, "AC-7: TotalLostQty=0");
}

// AC-8: Q=1 non-Unique → ComputeLoss(1, 0.08)=0
{
    int loss = ExplorationManager.ComputeLoss(1, ExplorationManager.LambdaSuccess);
    Assert(loss == 0, "AC-8: ComputeLoss(1, 0.08)=0（qty≤1 无损耗）");

    var mgr = BuildMgr();
    var stacks = new List<CarriedStack>
    {
        new CarriedStack("basic_supply", 1, false, 99),
    };
    var result = mgr.ExtractionLossSettlement(stacks, retreatFlagged: false);
    Assert(result.Transferred[0].Lost == 0, "AC-8: Q=1 non-Unique Lost=0");
    Assert(result.TotalLostQty == 0, "AC-8: Q=1 non-Unique TotalLostQty=0");
}

// AC-9: Q=3, λ=0.08 → ceil(3×0.08)=1 → loss=min(2,1)=1，保留 2
{
    int loss = ExplorationManager.ComputeLoss(3, 0.08);
    // ceil(3 × 0.08) = ceil(0.24) = 1; min(qty-1=2, 1) = 1
    Assert(loss == 1, "AC-9: ComputeLoss(3, 0.08)=1");

    var mgr = BuildMgr();
    var stacks = new List<CarriedStack>
    {
        new CarriedStack("ration", 3, false, 10),
    };
    var result = mgr.ExtractionLossSettlement(stacks, retreatFlagged: false);
    Assert(result.Transferred[0].Quantity == 2, "AC-9: Q=3 保留 2");
    Assert(result.TotalLostQty == 1, "AC-9: Q=3 损耗 1");
}

// AC-10: 混合堆（Unique + non-Unique×2）→ 批量原子转移成功（通过 FinalizeExtraction）
{
    // Arrange
    var mgr = BuildExploring();
    bool transferCalled = false;
    int transferBatchCount = 0;

    var stacks = new List<CarriedStack>
    {
        new CarriedStack("artifact.lens", 1, true, 1),      // Unique
        new CarriedStack("basic_supply", 10, false, 99),    // non-Unique
        new CarriedStack("iron_scrap", 5, false, 50),       // non-Unique
    };
    mgr.SetGetCarriedStacksDelegate(() => stacks);
    mgr.SetExtractCarriedToStorageDelegate(batch =>
    {
        transferCalled = true;
        transferBatchCount = batch.Count;
        return true;
    });
    mgr.SetTriggerSettlementSnapshotDelegate(_ => true);

    // Act: 驱动到 FinalizeExtraction
    mgr.TriggerExtraction();
    TickToCompletion(mgr);

    // Assert: 转移委托被调用一次，包含全部 3 条
    Assert(transferCalled, "AC-10: extract_carried_to_storage 被调用（原子批量）");
    Assert(transferBatchCount == 3, $"AC-10: 批量转移包含 3 条（实际={transferBatchCount}）");
}

// AC-11: λ=0.0 → ComputeLoss 对所有 Q 返回 0
{
    int loss1 = ExplorationManager.ComputeLoss(1, 0.0);
    int loss10 = ExplorationManager.ComputeLoss(10, 0.0);
    int loss100 = ExplorationManager.ComputeLoss(100, 0.0);
    Assert(loss1 == 0, "AC-11: ComputeLoss(1, 0.0)=0");
    Assert(loss10 == 0, "AC-11: ComputeLoss(10, 0.0)=0");
    Assert(loss100 == 0, "AC-11: ComputeLoss(100, 0.0)=0");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-12 ~ AC-19: StateVariantTransition 的 8 种规则
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-12 ~ AC-19: StateVariantTransition 8 种规则 ──");
{
    // AC-12: Unlooted + !allSearched + !env → Unlooted
    var r12 = ExplorationManager.StateVariantTransition(SearchPointState.Unlooted, false, false);
    Assert(r12 == SearchPointState.Unlooted, "AC-12: Unlooted+!all+!env → Unlooted");

    // AC-13: Unlooted + allSearched + !env → Looted
    var r13 = ExplorationManager.StateVariantTransition(SearchPointState.Unlooted, true, false);
    Assert(r13 == SearchPointState.Looted, "AC-13: Unlooted+all+!env → Looted");

    // AC-14: Unlooted + any + env=true → DangerChanged
    var r14a = ExplorationManager.StateVariantTransition(SearchPointState.Unlooted, false, true);
    var r14b = ExplorationManager.StateVariantTransition(SearchPointState.Unlooted, true, true);
    Assert(r14a == SearchPointState.DangerChanged, "AC-14a: Unlooted+!all+env → DangerChanged");
    Assert(r14b == SearchPointState.DangerChanged, "AC-14b: Unlooted+all+env → DangerChanged");

    // AC-15: Looted + env=true → DangerChanged
    var r15 = ExplorationManager.StateVariantTransition(SearchPointState.Looted, false, true);
    Assert(r15 == SearchPointState.DangerChanged, "AC-15: Looted+env=true → DangerChanged");

    // AC-16: Looted + !env → Looted
    var r16a = ExplorationManager.StateVariantTransition(SearchPointState.Looted, false, false);
    var r16b = ExplorationManager.StateVariantTransition(SearchPointState.Looted, true, false);
    Assert(r16a == SearchPointState.Looted, "AC-16a: Looted+!all+!env → Looted");
    Assert(r16b == SearchPointState.Looted, "AC-16b: Looted+all+!env → Looted");

    // AC-17: DangerChanged + !allSearched + !env → Unlooted
    var r17 = ExplorationManager.StateVariantTransition(SearchPointState.DangerChanged, false, false);
    Assert(r17 == SearchPointState.Unlooted, "AC-17: DangerChanged+!all+!env → Unlooted");

    // AC-18: DangerChanged + allSearched + !env → Looted
    var r18 = ExplorationManager.StateVariantTransition(SearchPointState.DangerChanged, true, false);
    Assert(r18 == SearchPointState.Looted, "AC-18: DangerChanged+all+!env → Looted");

    // AC-19: DangerChanged + env=true → DangerChanged
    var r19a = ExplorationManager.StateVariantTransition(SearchPointState.DangerChanged, false, true);
    var r19b = ExplorationManager.StateVariantTransition(SearchPointState.DangerChanged, true, true);
    Assert(r19a == SearchPointState.DangerChanged, "AC-19a: DangerChanged+!all+env → DangerChanged");
    Assert(r19b == SearchPointState.DangerChanged, "AC-19b: DangerChanged+all+env → DangerChanged");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-20: FinalizeExtraction 执行顺序：转移→情报→状态变体→snapshot→ExtractionCompleted
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-20: FinalizeExtraction 执行顺序 ──");
{
    var mgr = BuildExploring();
    var callOrder = new List<string>();

    // 注入委托，记录调用顺序
    mgr.SetGetCarriedStacksDelegate(() =>
    {
        callOrder.Add("get_carried");
        return new List<CarriedStack>
        {
            new CarriedStack("basic_supply", 5, false, 99),
        };
    });
    mgr.SetExtractCarriedToStorageDelegate(batch =>
    {
        callOrder.Add("extract_to_storage");
        return true;
    });
    mgr.SetRevealIntelDelegate(id => callOrder.Add($"reveal_intel:{id}"));
    mgr.SetTriggerSettlementSnapshotDelegate(s =>
    {
        callOrder.Add("snapshot");
        return true;
    });

    bool completedFired = false;
    mgr.ExtractionCompleted += (transferred, intel) =>
    {
        callOrder.Add("extraction_completed");
        completedFired = true;
    };

    // 模拟情报点已交互（通过内部实现注入）：先让 PerformIntelInteraction 记录一个情报点
    // 由于 _interactedIntelPoints 是私有的，用 PerformIntelInteraction 触发
    mgr.SetGetIntelIdForPointDelegate(id => $"intel.{id}");
    mgr.SetCanAddToPoolDelegate((_, _) => true);
    mgr.SetAddLootDelegate((_, _) => { });
    mgr.PerformIntelInteraction("intel-point-01");

    mgr.TriggerExtraction();
    TickToCompletion(mgr);

    // 验证关键步骤都被调用
    Assert(callOrder.Contains("get_carried"), "AC-20: get_carried 被调用");
    Assert(callOrder.Contains("extract_to_storage"), "AC-20: extract_to_storage 被调用");
    Assert(callOrder.Contains("reveal_intel:intel.intel-point-01"), "AC-20: reveal_intel 被调用");
    Assert(callOrder.Contains("snapshot"), "AC-20: snapshot 被调用");
    Assert(completedFired, "AC-20: ExtractionCompleted 事件被发射");
    // 验证顺序：extract_to_storage 在 snapshot 之前
    int extractIdx = callOrder.IndexOf("extract_to_storage");
    int snapshotIdx = callOrder.IndexOf("snapshot");
    int completedIdx = callOrder.IndexOf("extraction_completed");
    Assert(extractIdx < snapshotIdx, "AC-20: extract_to_storage 在 snapshot 之前");
    Assert(snapshotIdx < completedIdx, "AC-20: snapshot 在 ExtractionCompleted 之前");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-21: snapshot 失败 → AttemptSettlementRetry 调度 4 次重试；
//        全部失败 → emitSettlementFailedUi 被调用 + pendingSettlement 非 null
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-21: snapshot 重试机制 ──");
{
    var mgr = BuildExploring();
    int retryScheduleCount = 0;
    bool failedUiCalled = false;
    List<Action> scheduledCallbacks = new();
    var recordedDelays = new List<double>();   // 验证 delay 序列 1s/2s/4s/8s

    mgr.SetGetCarriedStacksDelegate(() => new List<CarriedStack>
    {
        new CarriedStack("scrap", 10, false, 99),
    });
    mgr.SetExtractCarriedToStorageDelegate(_ => true);
    mgr.SetTriggerSettlementSnapshotDelegate(_ => false); // 始终失败
    mgr.SetScheduleRetryCallbackDelegate((delay, cb) =>
    {
        retryScheduleCount++;
        recordedDelays.Add(delay);
        scheduledCallbacks.Add(cb);
    });
    mgr.SetEmitSettlementFailedUiDelegate(() => failedUiCalled = true);

    mgr.TriggerExtraction();
    TickToCompletion(mgr);

    // 第一次 snapshot 失败后，立即调度第 1 次重试
    Assert(retryScheduleCount == 1, "AC-21: 第 1 次 snapshot 失败后调度第 1 次重试");

    // 模拟依次触发所有调度的回调（每次调用后，retry 再调度下一次）
    for (int i = 0; i < 4; i++)
    {
        if (scheduledCallbacks.Count > i)
            scheduledCallbacks[i]();
    }

    Assert(retryScheduleCount == 4, $"AC-21: 调度次数=4（实际={retryScheduleCount}）");
    // 验证 delay 序列 1s/2s/4s/8s
    Assert(recordedDelays.Count == 4
        && recordedDelays[0] == 1.0 && recordedDelays[1] == 2.0
        && recordedDelays[2] == 4.0 && recordedDelays[3] == 8.0,
        $"AC-21: delay 序列=[{string.Join(",", recordedDelays)}]，期望=[1,2,4,8]");
    Assert(failedUiCalled, "AC-21: 全部重试失败后 emitSettlementFailedUi 被调用");
    // snapshot 委托仍返回 false → RetrySettlement 应返回 false（记录了 pendingSettlement）
    Assert(mgr.RetrySettlement() == false,
        "AC-21: snapshot 仍失败，RetrySettlement 返回 false（pendingSettlement 已记录）");
}

// AC-21b: 手动重试成功 → ExtractionCompleted 发射，pendingSettlement 清除
{
    var mgr = BuildExploring();
    int snapshotAttemptCount = 0;
    bool completedAfterRetry = false;
    List<Action> scheduledCallbacks = new();

    mgr.SetGetCarriedStacksDelegate(() => new List<CarriedStack>
    {
        new CarriedStack("scrap", 10, false, 99),
    });
    mgr.SetExtractCarriedToStorageDelegate(_ => true);
    // snapshot 前 5 次失败，第 6 次成功（用于验证 RetrySettlement）
    mgr.SetTriggerSettlementSnapshotDelegate(_ =>
    {
        snapshotAttemptCount++;
        return false; // 全部失败，触发 emitSettlementFailedUi
    });
    mgr.SetScheduleRetryCallbackDelegate((_, cb) => scheduledCallbacks.Add(cb));
    mgr.SetEmitSettlementFailedUiDelegate(() => { });
    mgr.ExtractionCompleted += (_, _) => completedAfterRetry = true;

    mgr.TriggerExtraction();
    TickToCompletion(mgr);
    // 触发所有 4 次自动重试
    for (int i = 0; i < 4; i++)
    {
        if (scheduledCallbacks.Count > i)
            scheduledCallbacks[i]();
    }

    // 此时 pendingSettlement 非 null，手动重试
    // 修改 snapshot 让它成功
    mgr.SetTriggerSettlementSnapshotDelegate(_ => true);
    bool retryResult = mgr.RetrySettlement();
    Assert(retryResult, "AC-21b: 手动重试成功返回 true");
    Assert(completedAfterRetry, "AC-21b: 手动重试成功后 ExtractionCompleted 发射");
}

// ──────────────────────────────────────────────────────────────────────────
// AC-22: ExtractionCompleted 发射后 phase → DEPARTED
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── AC-22: ExtractionCompleted 发射后 phase=DEPARTED ──");
{
    var mgr = BuildExploring();
    bool completedFired = false;
    ExplorationPhase phaseAtCompletion = ExplorationPhase.Idle;

    mgr.SetGetCarriedStacksDelegate(() => new List<CarriedStack>());
    mgr.SetTriggerSettlementSnapshotDelegate(_ => true);

    mgr.ExtractionCompleted += (_, _) =>
    {
        completedFired = true;
        phaseAtCompletion = mgr.CurrentPhase;
    };

    mgr.TriggerExtraction();
    TickToCompletion(mgr);

    Assert(completedFired, "AC-22: ExtractionCompleted 事件已发射");
    Assert(phaseAtCompletion == ExplorationPhase.Departed, "AC-22: 发射时 phase=DEPARTED");
    // AC-22 的 Hub 场景跳转（Platform #2 收到信号后过渡回 Hub + session_phase→IDLE）
    // 需要 Platform #2 场景集成，不在无头测试范围内，由 playtest 验证。
}

// ──────────────────────────────────────────────────────────────────────────
// 额外边界验证
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n── 额外边界验证 ──");

// ComputeLoss 边界：qty=0
{
    int loss = ExplorationManager.ComputeLoss(0, 0.08);
    Assert(loss == 0, "EDGE: ComputeLoss(0, 0.08)=0（qty≤1）");
}

// ComputeLoss 边界：lambda 极大
{
    int loss = ExplorationManager.ComputeLoss(10, 99.0);
    Assert(loss == 9, "EDGE: ComputeLoss(10, 99.0)=9（最多损耗 qty-1）");
}

// Unique 物品 retreat_flagged=true 时仍不损耗
{
    var mgr = BuildMgr();
    var stacks = new List<CarriedStack>
    {
        new CarriedStack("artifact.rare", 1, true, 1),
    };
    var result = mgr.ExtractionLossSettlement(stacks, retreatFlagged: true);
    Assert(result.Transferred[0].Lost == 0, "EDGE: Unique+retreat_flagged → 仍不损耗");
}

// 空 carriedStacks → TotalLostQty=0，Transferred 为空
{
    var mgr = BuildMgr();
    var result = mgr.ExtractionLossSettlement(new List<CarriedStack>(), false);
    Assert(result.Transferred.Count == 0, "EDGE: 空 carriedStacks → Transferred=0");
    Assert(result.TotalLostQty == 0, "EDGE: 空 carriedStacks → TotalLostQty=0");
}

// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"Story 005 Extraction Settlement: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
