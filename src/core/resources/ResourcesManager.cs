using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 资源池标识。旧名称保留给既有 C# parity 测试，新名称映射到当前 GDD 池语义。
/// </summary>
public enum ResourcePool
{
    OnPerson = 0,
    InStorage = 1,
    Loaded = 2,
    Listed = 3,
    Carried = 4,
    Deposited = 5,
    Storage = InStorage,
    Cargo = Loaded,
    Supply = Listed,
    Repair = Deposited,
}

/// <summary>
/// 资源操作结果码，保持资源变更的成功/失败原因可被 UI 与测试明确消费。
/// </summary>
public enum ResourceResult
{
    Success = 0,
    ErrNotInitialized = 1,
    ErrTargetFull = 2,
    ErrSourceInsufficient = 3,
    ErrInvalidQuantity = 4,
    ErrMissingReference = 5,
    ErrDeprecatedId = 6,
    ErrCapacityZero = 7,
    ErrCarrySlotsFull = 8,
    ErrCarryStackFull = 9,
    ErrStorageFull = 10,
    ErrCargoNotInBay = 11,
    ErrBusy = 12,
    ErrKindMismatch = 13,
}

/// <summary>
/// 资源堆的只读快照，用于测试、UI 查询和下游系统读取当前池状态。
/// </summary>
public sealed record ResourceStackSnapshot(int SlotIndex, string ResourceId, int Quantity);

/// <summary>
/// 货舱中单个货物物品的运行时拆包元数据。
/// </summary>
public sealed record CargoItemSnapshot(
    int SlotIndex,
    string CargoId,
    string LinkedResourceId,
    int ResourceQuantity,
    string MassClass);

/// <summary>
/// 货舱占用查询中的单个堆条目。
/// </summary>
public sealed record CargoBayUsageStack(
    int SlotIndex,
    string ResourceId,
    int Quantity,
    int Volume,
    string MassClass);

/// <summary>
/// 货舱模块损毁后生成的损失条目。
/// </summary>
public sealed record CargoBayLossSnapshot(
    string CargoId,
    string LinkedResourceId,
    int LossQuantity,
    int RetainedQuantity);

/// <summary>
/// 货舱模块损毁后生成的可回收货箱条目。
/// </summary>
public sealed record RecoverableCrateSnapshot(
    string CargoId,
    string LinkedResourceId,
    int Quantity);

/// <summary>
/// 货舱模块损毁处理的结构化结果。
/// </summary>
public sealed record CargoBayDestructionResult(
    ResourceResult Result,
    IReadOnlyList<CargoBayLossSnapshot> Losses,
    IReadOnlyList<RecoverableCrateSnapshot> Crates,
    int PreviousUsedVolume)
{
    /// <summary>
    /// 损毁处理是否成功完成。
    /// </summary>
    public bool Success => Result == ResourceResult.Success;
}

/// <summary>
/// 资源快照迁移中被拆分、丢弃或降级的条目。
/// </summary>
public sealed record ResourceMigrationLogEntry(
    string ReasonCode,
    ResourcePool Pool,
    string ResourceId,
    int Quantity);

/// <summary>
/// 静态 mass_class 对应的容量与重量配置。
/// </summary>
public sealed record ResourceMassProfile(string MassClass, int Volume, int Weight);

/// <summary>
/// 单次资源操作的结构化结果。
/// </summary>
public sealed record ResourceOperationResult(
    ResourceResult Result,
    int QuantityChanged,
    int MergeQuantity,
    int OverflowQuantity)
{
    /// <summary>
    /// 操作是否成功。
    /// </summary>
    public bool Success => Result == ResourceResult.Success;

    /// <summary>
    /// 创建一个成功结果。
    /// </summary>
    public static ResourceOperationResult Ok(int quantityChanged, int mergeQuantity = 0, int overflowQuantity = 0)
    {
        return new ResourceOperationResult(
            ResourceResult.Success,
            quantityChanged,
            mergeQuantity,
            overflowQuantity);
    }

    /// <summary>
    /// 创建一个失败结果。
    /// </summary>
    public static ResourceOperationResult Fail(ResourceResult result, int mergeQuantity = 0, int overflowQuantity = 0)
    {
        return new ResourceOperationResult(result, 0, mergeQuantity, overflowQuantity);
    }
}

/// <summary>
/// Result of a market purchase preflight owned by ResourcesManager.
/// </summary>
public sealed record ResourcePurchaseValidation(bool Valid, string Reason, int TotalCost);

/// <summary>
/// 探索结算或池状态转换的聚合结果，记录保留入仓与损失销毁的资源数量。
/// </summary>
public sealed record ResourceTransitionSummary(
    ResourceResult Result,
    IReadOnlyDictionary<string, int> Retained,
    IReadOnlyDictionary<string, int> Lost)
{
    /// <summary>
    /// 转换是否成功。
    /// </summary>
    public bool Success => Result == ResourceResult.Success;

    /// <summary>
    /// 创建一个成功转换结果。
    /// </summary>
    public static ResourceTransitionSummary Ok(
        Dictionary<string, int>? retained = null,
        Dictionary<string, int>? lost = null)
    {
        return new ResourceTransitionSummary(
            ResourceResult.Success,
            retained ?? new Dictionary<string, int>(StringComparer.Ordinal),
            lost ?? new Dictionary<string, int>(StringComparer.Ordinal));
    }

    /// <summary>
    /// 创建一个失败转换结果。
    /// </summary>
    public static ResourceTransitionSummary Fail(ResourceResult result)
    {
        return new ResourceTransitionSummary(
            result,
            new Dictionary<string, int>(StringComparer.Ordinal),
            new Dictionary<string, int>(StringComparer.Ordinal));
    }
}

/// <summary>
/// 资源系统的 C# 核心模型，负责稳定 ID 身份、堆叠合并和池内数量查询。
/// </summary>
public sealed class ResourcesManager
{
    private const int CarryBaseSlots = 5;
    private const int CarriedBaseSlots = 5;
    private const int StorageBaseVolume = 1000;
    private const int CargoBayBaseVolume = 0;
    private const string CurrencyResourceId = "resource.cloud_coin";

    private static readonly Dictionary<string, int> SupplyClassCapacity = new(StringComparer.Ordinal)
    {
        ["basic"] = 99,
        ["repair"] = 99,
        ["navigation"] = 20,
        ["local-specialty"] = 10,
        ["local_specialty"] = 10,
        ["intel"] = 1,
    };

    private static readonly Dictionary<string, ResourceMassProfile> MassClassProfiles = new(StringComparer.Ordinal)
    {
        ["light"] = new ResourceMassProfile("light", Volume: 50, Weight: 1),
        ["medium"] = new ResourceMassProfile("medium", Volume: 120, Weight: 3),
        ["heavy"] = new ResourceMassProfile("heavy", Volume: 200, Weight: 6),
    };

    private static readonly ResourcePool[] DepositSourcePools =
    [
        ResourcePool.OnPerson,
        ResourcePool.InStorage,
        ResourcePool.Carried,
    ];

    private static readonly ResourcePool[] PersistedPools =
    [
        ResourcePool.OnPerson,
        ResourcePool.InStorage,
        ResourcePool.Loaded,
    ];

    private readonly Registry? _registry;
    private readonly Dictionary<ResourcePool, List<ResourceStack>> _pools = new();
    private readonly Dictionary<string, string> _supplyClassCache = new(StringComparer.Ordinal);
    private readonly List<ResourceMigrationLogEntry> _migrationLog = [];
    private int _carrySlotBonus;
    private int _storageVolumeBonus;
    private int _cargoModuleVolumeBonus;
    private int _cargoBayTrappedVolume;
    private bool _isMutating;

    /// <summary>
    /// 创建资源管理器，可选接入内容注册表以读取资源定义。
    /// </summary>
    public ResourcesManager(Registry? registry = null)
    {
        _registry = registry;
    }

    /// <summary>
    /// 资源加入池后触发。旧事件保留给当前 parity 测试与临时消费者。
    /// </summary>
    public event Action<ResourcePool, string, int>? ResourceAdded;

    /// <summary>
    /// 资源从池中移除后触发。旧事件保留给当前 parity 测试与临时消费者。
    /// </summary>
    public event Action<ResourcePool, string, int>? ResourceRemoved;

    /// <summary>
    /// 资源总量变更后触发。旧事件保留给当前 parity 测试与临时消费者。
    /// </summary>
    public event Action<ResourcePool, string, int>? ResourceChanged;

    /// <summary>
    /// 任意资源池内容完成变更后触发；参数为已完成变更的池。
    /// </summary>
    public event Action<ResourcePool>? PoolChanged;

    /// <summary>
    /// 跨池转移成功完成后触发，早于相关池的 PoolChanged 通知。
    /// </summary>
    public event Action<ResourcePool, ResourcePool, string, int>? TransferCompleted;

    /// <summary>
    /// 货物拆包成功完成后触发。
    /// </summary>
    public event Action<string, string, int>? CargoUnpacked;

    /// <summary>
    /// 修复材料提交成功后触发。
    /// </summary>
    public event Action<string>? DepositCommitted;

    /// <summary>
    /// 修复材料提交失败后触发，reason 为机器可读错误码。
    /// </summary>
    public event Action<string, string>? DepositFailed;

    /// <summary>
    /// 货舱总装载质量变化后触发。
    /// </summary>
    public event Action<int>? MassChanged;

    /// <summary>
    /// 货舱模块损毁造成损失后触发，参数为损失数量与可回收数量。
    /// </summary>
    public event Action<int, int>? CargoBayLossNotified;

    /// <summary>
    /// 快照恢复过程中因容量或版本变更丢弃资源时触发。
    /// </summary>
    public event Action<string, ResourcePool, string, int>? ResourceMigrationNotice;

    /// <summary>
    /// 最近一次快照恢复产生的迁移日志。
    /// </summary>
    public IReadOnlyList<ResourceMigrationLogEntry> MigrationLog => _migrationLog.ToArray();

    /// <summary>
    /// 管理器是否已初始化所有资源池。
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 初始化所有资源池；重复调用会清空现有运行时资源状态。
    /// </summary>
    public void Initialize()
    {
        _pools.Clear();
        foreach (ResourcePool pool in Enum.GetValues<ResourcePool>())
        {
            if (!_pools.ContainsKey(pool))
            {
                _pools[pool] = new List<ResourceStack>();
            }
        }

        IsInitialized = true;
    }

    /// <summary>
    /// 注册旧 parity 路径使用的供给类别覆盖。新实现优先读取 Registry 定义。
    /// </summary>
    public void RegisterSupplyClass(string resourceId, string supplyClass)
    {
        _supplyClassCache[resourceId] = supplyClass;
    }

    /// <summary>
    /// 设置随身与探索局内池的槽位加成。
    /// </summary>
    public void SetCarrySlotBonus(int bonus)
    {
        _carrySlotBonus = Math.Max(0, bonus);
    }

    /// <summary>
    /// 设置飞艇仓库容积加成。
    /// </summary>
    public void SetStorageVolumeBonus(int bonus)
    {
        _storageVolumeBonus = Math.Max(0, bonus);
    }

    /// <summary>
    /// 设置货舱模块提供的容积加成。
    /// </summary>
    public void SetCargoModuleVolumeBonus(int bonus)
    {
        UpdateCargoBayEffectiveVolume(bonus);
    }

    /// <summary>
    /// 向目标池加入一个带不可变拆包元数据的货物物品。
    /// </summary>
    public ResourceOperationResult AddCargo(
        ResourcePool pool,
        string cargoId,
        string linkedResourceId,
        int resourceQuantity)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            if (resourceQuantity <= 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
            }

            var cargoResolution = ResolveResourceDefinition(cargoId, stackRuleOverride: null);
            if (cargoResolution.Result != ResourceResult.Success || cargoResolution.Definition is null)
            {
                return ResourceOperationResult.Fail(cargoResolution.Result);
            }

            if (!IsCargoKind(cargoResolution.Definition))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
            }

            var linkedResolution = ResolveResourceDefinition(linkedResourceId, stackRuleOverride: null);
            if (linkedResolution.Result != ResourceResult.Success || linkedResolution.Definition is null)
            {
                return ResourceOperationResult.Fail(linkedResolution.Result);
            }

            if (IsCargoKind(linkedResolution.Definition))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
            }

            var result = AddCore(
                pool,
                cargoId,
                quantity: 1,
                stackRuleOverride: null,
                allowMultipleUniqueStacks: true,
                emitEvents: true,
                cargoInstance: new CargoInstance(linkedResourceId, resourceQuantity));
            EmitMassChangedIfNeeded(pool, beforeMass, result.Success);
            return result;
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 将指定资源添加到目标池，按 ADR-0005 的 fill-fullest-first 规则合并。
    /// </summary>
    public ResourceOperationResult Add(ResourcePool pool, string resourceId, int quantity)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            var result = AddCore(pool, resourceId, quantity, stackRuleOverride: null, allowMultipleUniqueStacks: true, emitEvents: true);
            EmitMassChangedIfNeeded(pool, beforeMass, result.Success);
            return result;
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 从指定池移除资源。数量不足时全操作失败且不修改状态。
    /// </summary>
    public ResourceOperationResult Remove(ResourcePool pool, string resourceId, int quantity)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            var result = RemoveCore(pool, resourceId, quantity, emitEvents: true);
            EmitMassChangedIfNeeded(pool, beforeMass, result.Success);
            return result;
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 消耗指定池中的资源；语义等同移除，消耗结果不进入任何可取回资源池。
    /// </summary>
    public ResourceOperationResult Consume(ResourcePool pool, string resourceId, int quantity)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            if (!IsInitialized)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
            }

            if (quantity < 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
            }

            if (quantity == 0)
            {
                return ResourceOperationResult.Ok(0);
            }

            var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride: null);
            if (resolution.Definition is null)
            {
                return ResourceOperationResult.Fail(resolution.Result);
            }

            if (IsCargoKind(resolution.Definition))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
            }

            var result = RemoveCore(pool, resourceId, quantity, emitEvents: true);
            EmitMassChangedIfNeeded(pool, beforeMass, result.Success);
            return result;
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 按稳定 ID 在两个池之间转移资源；匹配不依赖显示名或其他 UI 字段。
    /// </summary>
    public ResourceOperationResult Transfer(ResourcePool fromPool, ResourcePool toPool, string resourceId, int quantity)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            if (!IsInitialized)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
            }

            if (quantity < 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
            }

            if (quantity == 0)
            {
                return ResourceOperationResult.Ok(0);
            }

            if (fromPool == ResourcePool.Deposited)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
            }

            if (GetQuantity(fromPool, resourceId) < quantity)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient);
            }

            var before = ClonePools();
            var removed = RemoveCore(fromPool, resourceId, quantity, emitEvents: false);
            if (!removed.Success)
            {
                RestorePools(before);
                return removed;
            }

            var added = AddCore(toPool, resourceId, quantity, stackRuleOverride: null, allowMultipleUniqueStacks: true, emitEvents: false);
            if (!added.Success)
            {
                RestorePools(before);
                return IsTransferTargetCapacityFailure(added.Result)
                    ? ResourceOperationResult.Fail(
                        ResourceResult.ErrTargetFull,
                        added.MergeQuantity,
                        added.OverflowQuantity)
                    : added;
            }

            TransferCompleted?.Invoke(fromPool, toPool, resourceId, quantity);
            ResourceChanged?.Invoke(fromPool, resourceId, GetQuantity(fromPool, resourceId));
            ResourceChanged?.Invoke(toPool, resourceId, GetQuantity(toPool, resourceId));
            PoolChanged?.Invoke(fromPool);
            PoolChanged?.Invoke(toPool);
            EmitMassChangedForLoadedTransfer(fromPool, toPool, beforeMass);
            return ResourceOperationResult.Ok(quantity, added.MergeQuantity, added.OverflowQuantity);
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 探索成功撤离时，将局内携带池中的所有资源原子归入飞艇仓库。
    /// </summary>
    public ResourceTransitionSummary ExtractCarriedToStorage()
    {
        if (!TryEnterMutation())
        {
            return ResourceTransitionSummary.Fail(ResourceResult.ErrBusy);
        }

        try
        {
            if (!IsInitialized)
            {
                return ResourceTransitionSummary.Fail(ResourceResult.ErrNotInitialized);
            }

            var carriedSummary = GetCarriedQuantitySummary();
            if (carriedSummary.Count == 0)
            {
                return ResourceTransitionSummary.Ok();
            }

            var before = ClonePools();
            var addResult = AddSummaryToStorage(carriedSummary);
            if (addResult != ResourceResult.Success)
            {
                RestorePools(before);
                return ResourceTransitionSummary.Fail(addResult);
            }

            ClearCarriedAndEmit(carriedSummary, carriedSummary);
            return ResourceTransitionSummary.Ok(carriedSummary);
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 探索失败时按损失比例结算 carried 池；损失进入 destroyed 终态，保留量归入仓库。
    /// </summary>
    public ResourceTransitionSummary ApplyExtractionLoss(double lossRatio)
    {
        if (!TryEnterMutation())
        {
            return ResourceTransitionSummary.Fail(ResourceResult.ErrBusy);
        }

        try
        {
            if (!IsInitialized)
            {
                return ResourceTransitionSummary.Fail(ResourceResult.ErrNotInitialized);
            }

            if (double.IsNaN(lossRatio) || double.IsInfinity(lossRatio))
            {
                return ResourceTransitionSummary.Fail(ResourceResult.ErrInvalidQuantity);
            }

            var carriedStacks = GetCarriedStackQuantities();
            if (carriedStacks.Length == 0)
            {
                return ResourceTransitionSummary.Ok();
            }

            var before = ClonePools();
            var retained = new Dictionary<string, int>(StringComparer.Ordinal);
            var lost = new Dictionary<string, int>(StringComparer.Ordinal);
            var planResult = BuildExtractionLossSummary(carriedStacks, Math.Clamp(lossRatio, 0.0d, 1.0d), retained, lost);
            if (planResult != ResourceResult.Success)
            {
                return ResourceTransitionSummary.Fail(planResult);
            }

            var addResult = AddSummaryToStorage(retained);
            if (addResult != ResourceResult.Success)
            {
                RestorePools(before);
                return ResourceTransitionSummary.Fail(addResult);
            }

            ClearCarriedAndEmit(GetCarriedQuantitySummary(), retained);
            return ResourceTransitionSummary.Ok(retained, lost);
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 玩家确认后丢弃指定池中的资源；丢弃结果进入 destroyed 终态。
    /// </summary>
    public ResourceOperationResult Discard(ResourcePool pool, string resourceId, int quantity)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            if (!IsInitialized)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
            }

            if (quantity < 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
            }

            if (!IsDiscardablePool(pool))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
            }

            if (quantity == 0)
            {
                return ResourceOperationResult.Ok(0);
            }

            var result = RemoveCore(pool, resourceId, quantity, emitEvents: true);
            EmitMassChangedIfNeeded(pool, beforeMass, result.Success);
            return result;
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 战斗中从 carried 池消耗资源；这是对 Consume(carried, ...) 的薄封装。
    /// </summary>
    public ResourceOperationResult ConsumeInCombat(string resourceId, int quantity)
    {
        return Consume(ResourcePool.Carried, resourceId, quantity);
    }

    /// <summary>
    /// 查询修复提交所需资源是否满足；该查询不修改任何资源池。
    /// </summary>
    public bool CanDeposit(string repairNodeId, IReadOnlyDictionary<string, int> resourceCosts)
    {
        return IsInitialized
            && IsValidDomainId(repairNodeId)
            && resourceCosts is not null
            && ValidateDepositCosts(resourceCosts) == ResourceResult.Success
            && HasDepositableResources(resourceCosts);
    }

    /// <summary>
    /// 将修复所需资源原子提交到 deposited 终态池。
    /// </summary>
    public ResourceOperationResult CommitDeposit(
        string repairNodeId,
        IReadOnlyDictionary<string, int> resourceCosts)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        try
        {
            var validation = ValidateDepositRequest(repairNodeId, resourceCosts);
            if (validation != ResourceResult.Success)
            {
                EmitDepositFailed(repairNodeId, validation);
                return ResourceOperationResult.Fail(validation);
            }

            if (!HasDepositableResources(resourceCosts))
            {
                EmitDepositFailed(repairNodeId, ResourceResult.ErrSourceInsufficient);
                return ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient);
            }

            var before = ClonePools();
            var result = MoveDepositCosts(resourceCosts);
            if (!result.Success)
            {
                RestorePools(before);
                EmitDepositFailed(repairNodeId, result.Result);
                return result;
            }

            DepositCommitted?.Invoke(repairNodeId);
            foreach (var pool in GetChangedPools(before, DepositSourcePools))
            {
                PoolChanged?.Invoke(pool);
            }

            return result;
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 执行购买结算，将资源从 listed 池原子转入飞艇仓库。
    /// </summary>
    public ResourceOperationResult ExecutePurchase(string goodId, int quantity)
    {
        if (IsMarketGood(goodId))
        {
            var validation = ValidatePurchase(goodId, quantity);
            if (!validation.Valid)
            {
                return validation.Reason == "capacity_full"
                    ? ResourceOperationResult.Fail(ResourceResult.ErrTargetFull)
                    : ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient);
            }

            if (!TryEnterMutation())
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
            }

            try
            {
                var before = ClonePools();
                var currencyRemoved = RemoveCore(ResourcePool.InStorage, CurrencyResourceId, validation.TotalCost, emitEvents: false);
                if (!currencyRemoved.Success)
                {
                    RestorePools(before);
                    return currencyRemoved;
                }

                var goodAdded = AddCore(ResourcePool.InStorage, goodId, quantity, stackRuleOverride: null, allowMultipleUniqueStacks: true, emitEvents: false);
                if (!goodAdded.Success)
                {
                    RestorePools(before);
                    return IsTransferTargetCapacityFailure(goodAdded.Result)
                        ? ResourceOperationResult.Fail(ResourceResult.ErrTargetFull, goodAdded.MergeQuantity, goodAdded.OverflowQuantity)
                        : goodAdded;
                }

                ResourceRemoved?.Invoke(ResourcePool.InStorage, CurrencyResourceId, validation.TotalCost);
                ResourceAdded?.Invoke(ResourcePool.InStorage, goodId, quantity);
                ResourceChanged?.Invoke(ResourcePool.InStorage, CurrencyResourceId, GetQuantity(ResourcePool.InStorage, CurrencyResourceId));
                ResourceChanged?.Invoke(ResourcePool.InStorage, goodId, GetQuantity(ResourcePool.InStorage, goodId));
                PoolChanged?.Invoke(ResourcePool.InStorage);
                return ResourceOperationResult.Ok(quantity, goodAdded.MergeQuantity, goodAdded.OverflowQuantity);
            }
            finally
            {
                ExitMutation();
            }
        }

        return Transfer(ResourcePool.Listed, ResourcePool.InStorage, goodId, quantity);
    }

    /// <summary>
    /// Validates market purchase currency and storage capacity without mutating pools.
    /// </summary>
    public ResourcePurchaseValidation ValidatePurchase(string goodId, int quantity)
    {
        if (!IsInitialized)
        {
            return new ResourcePurchaseValidation(false, "system_unavailable", 0);
        }

        if (quantity <= 0)
        {
            return new ResourcePurchaseValidation(false, "invalid_quantity", 0);
        }

        var resolution = ResolveResourceDefinition(goodId, stackRuleOverride: null);
        if (resolution.Result != ResourceResult.Success || resolution.Definition is null)
        {
            return new ResourcePurchaseValidation(false, "missing_reference", 0);
        }

        var totalCost = checked(GetPurchasePrice(goodId) * quantity);
        if (GetPlayerCurrency() < totalCost)
        {
            return new ResourcePurchaseValidation(false, "insufficient_funds", totalCost);
        }

        if (PreviewAddToStorage(goodId, quantity) != ResourceResult.Success)
        {
            return new ResourcePurchaseValidation(false, "capacity_full", totalCost);
        }

        return new ResourcePurchaseValidation(true, string.Empty, totalCost);
    }

    /// <summary>
    /// Returns the current cloud coin balance tracked in storage.
    /// </summary>
    public int GetPlayerCurrency()
    {
        return GetQuantity(ResourcePool.InStorage, CurrencyResourceId);
    }

    /// <summary>
    /// 执行上架结算，将仓库资源原子转入 listed 池；价格由集市系统持有。
    /// </summary>
    public ResourceOperationResult ListForSale(string resourceId, int quantity, int price)
    {
        return price < 0
            ? ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity)
            : Transfer(ResourcePool.InStorage, ResourcePool.Listed, resourceId, quantity);
    }

    /// <summary>
    /// 探索拾取入口，将战利品加入 carried 池。
    /// </summary>
    public ResourceOperationResult AddLoot(string resourceId, int quantity)
    {
        return Add(ResourcePool.Carried, resourceId, quantity);
    }

    /// <summary>
    /// 旧 API：添加资源并返回实际加入数量；未初始化或失败时返回 0。
    /// </summary>
    public int AddItem(ResourcePool pool, string resourceId, int quantity, string stackRule = "stackable")
    {
        if (!IsInitialized || quantity <= 0)
        {
            return 0;
        }

        if (!TryEnterMutation())
        {
            return 0;
        }

        var beforeMass = GetTotalLoadedMass();
        ResourceOperationResult result;
        try
        {
            result = AddCore(
                pool,
                resourceId,
                quantity,
                stackRuleOverride: stackRule,
                allowMultipleUniqueStacks: false,
                emitEvents: true);
            EmitMassChangedIfNeeded(pool, beforeMass, result.Success);
        }
        finally
        {
            ExitMutation();
        }

        return result.Success ? result.QuantityChanged : 0;
    }

    /// <summary>
    /// 旧 API：移除资源并返回实际移除数量；数量不足时不修改状态。
    /// </summary>
    public int RemoveItem(ResourcePool pool, string resourceId, int quantity)
    {
        if (!IsInitialized || quantity <= 0)
        {
            return 0;
        }

        var result = Remove(pool, resourceId, quantity);
        return result.Success ? result.QuantityChanged : 0;
    }

    /// <summary>
    /// 返回指定池内某资源的聚合数量。
    /// </summary>
    public int GetQuantity(ResourcePool pool, string resourceId)
    {
        return GetPool(pool)
            .Where(stack => string.Equals(stack.ResourceId, resourceId, StringComparison.Ordinal))
            .Sum(stack => stack.Quantity);
    }

    /// <summary>
    /// 查询指定池是否至少拥有给定数量的资源。
    /// </summary>
    public bool HasItem(ResourcePool pool, string resourceId, int quantity = 1)
    {
        return GetQuantity(pool, resourceId) >= quantity;
    }

    /// <summary>
    /// 返回聚合后的池内容副本。
    /// </summary>
    public Dictionary<string, int> GetPoolContents(ResourcePool pool)
    {
        return GetPool(pool)
            .GroupBy(stack => stack.ResourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity), StringComparer.Ordinal);
    }

    /// <summary>
    /// 返回池内堆结构快照；传入 resourceId 时仅返回该稳定 ID 的堆。
    /// </summary>
    public IReadOnlyList<ResourceStackSnapshot> GetStacks(ResourcePool pool, string? resourceId = null)
    {
        return GetPool(pool)
            .Select((stack, index) => new ResourceStackSnapshot(index, stack.ResourceId, stack.Quantity))
            .Where(stack => resourceId is null || string.Equals(stack.ResourceId, resourceId, StringComparison.Ordinal))
            .ToArray();
    }

    /// <summary>
    /// 返回指定池槽位的货物元数据；槽位无货物时返回 null。
    /// </summary>
    public CargoItemSnapshot? GetCargoItem(ResourcePool pool, int slotIndex)
    {
        var stacks = GetPool(pool);
        if (slotIndex < 0 || slotIndex >= stacks.Count)
        {
            return null;
        }

        var stack = stacks[slotIndex];
        var resolution = ResolveResourceDefinition(stack.ResourceId, stackRuleOverride: null);
        if (resolution.Definition is null || !IsCargoKind(resolution.Definition))
        {
            return null;
        }

        var linkedResourceId = string.IsNullOrWhiteSpace(stack.LinkedResourceId)
            ? resolution.Definition.LinkedResourceId
            : stack.LinkedResourceId;
        var resourceQuantity = stack.ResourceQuantity > 0
            ? stack.ResourceQuantity
            : resolution.Definition.ResourceQuantity;

        return new CargoItemSnapshot(
            slotIndex,
            stack.ResourceId,
            linkedResourceId,
            resourceQuantity,
            resolution.Definition.MassClass);
    }

    /// <summary>
    /// 原子销毁货舱中的一个货物物品，并将其关联资源存入飞艇仓库。
    /// </summary>
    public ResourceOperationResult UnpackCargo(int cargoSlotIndex)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        var beforeMass = GetTotalLoadedMass();
        try
        {
            if (!IsInitialized)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
            }

            var loaded = GetPool(ResourcePool.Loaded);
            if (cargoSlotIndex < 0 || cargoSlotIndex >= loaded.Count)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrCargoNotInBay);
            }

            var cargoStack = loaded[cargoSlotIndex];
            var cargoResolution = ResolveResourceDefinition(cargoStack.ResourceId, stackRuleOverride: null);
            if (cargoResolution.Result != ResourceResult.Success || cargoResolution.Definition is null)
            {
                return ResourceOperationResult.Fail(cargoResolution.Result);
            }

            if (!IsCargoKind(cargoResolution.Definition))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrCargoNotInBay);
            }

            var linkedResourceId = string.IsNullOrWhiteSpace(cargoStack.LinkedResourceId)
                ? cargoResolution.Definition.LinkedResourceId
                : cargoStack.LinkedResourceId;
            if (string.IsNullOrWhiteSpace(linkedResourceId))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrMissingReference);
            }

            var resourceQuantity = cargoStack.ResourceQuantity > 0
                ? cargoStack.ResourceQuantity
                : cargoResolution.Definition.ResourceQuantity;
            if (resourceQuantity <= 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
            }

            var linkedResolution = ResolveResourceDefinition(linkedResourceId, stackRuleOverride: null);
            if (linkedResolution.Result != ResourceResult.Success || linkedResolution.Definition is null)
            {
                return ResourceOperationResult.Fail(linkedResolution.Result);
            }

            if (IsCargoKind(linkedResolution.Definition))
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
            }

            var before = ClonePools();
            var stored = AddCore(
                ResourcePool.InStorage,
                linkedResourceId,
                resourceQuantity,
                stackRuleOverride: null,
                allowMultipleUniqueStacks: true,
                emitEvents: false);

            if (!stored.Success)
            {
                RestorePools(before);
                if (stored.Result is ResourceResult.ErrTargetFull or ResourceResult.ErrCapacityZero)
                {
                    return ResourceOperationResult.Fail(
                        ResourceResult.ErrStorageFull,
                        stored.MergeQuantity,
                        stored.OverflowQuantity);
                }

                return stored;
            }

            loaded.RemoveAt(cargoSlotIndex);
            CargoUnpacked?.Invoke(cargoStack.ResourceId, linkedResourceId, resourceQuantity);
            ResourceChanged?.Invoke(ResourcePool.Loaded, cargoStack.ResourceId, GetQuantity(ResourcePool.Loaded, cargoStack.ResourceId));
            ResourceChanged?.Invoke(ResourcePool.InStorage, linkedResourceId, GetQuantity(ResourcePool.InStorage, linkedResourceId));
            PoolChanged?.Invoke(ResourcePool.Loaded);
            PoolChanged?.Invoke(ResourcePool.InStorage);
            EmitMassChangedIfNeeded(ResourcePool.Loaded, beforeMass, true);

            return ResourceOperationResult.Ok(
                resourceQuantity,
                stored.MergeQuantity,
                stored.OverflowQuantity);
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 返回资源的堆叠上限。Registry 定义优先，旧供给类别覆盖作为兼容回退。
    /// </summary>
    public int GetMaxStack(string resourceId)
    {
        var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride: null);
        if (resolution.Definition is not null)
        {
            return resolution.Definition.MaxStack;
        }

        var supplyClass = _supplyClassCache.GetValueOrDefault(resourceId, "basic");
        return SupplyClassCapacity.GetValueOrDefault(supplyClass, 99);
    }

    /// <summary>
    /// 返回资源静态 mass_class 对应的容积与重量配置。
    /// </summary>
    public ResourceMassProfile GetMassProfile(string resourceId)
    {
        var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride: null);
        var massClass = resolution.Definition?.MassClass ?? "light";
        return GetMassProfileForClass(massClass);
    }

    /// <summary>
    /// 返回槽位制池当前已用槽位数。
    /// </summary>
    public int GetUsedSlots(ResourcePool pool)
    {
        return GetPool(pool).Count;
    }

    /// <summary>
    /// 返回槽位制池总槽位数；非槽位制池返回 0。
    /// </summary>
    public int GetTotalSlots(ResourcePool pool)
    {
        if (pool == ResourcePool.OnPerson)
        {
            return CarryBaseSlots + _carrySlotBonus;
        }

        if (pool == ResourcePool.Carried)
        {
            return CarriedBaseSlots + _carrySlotBonus;
        }

        return 0;
    }

    /// <summary>
    /// 返回容积制池当前已用容积；每个堆按其 mass_class 占用一次容积。
    /// </summary>
    public int GetUsedVolume(ResourcePool pool)
    {
        if (!IsVolumeBasedPool(pool))
        {
            return 0;
        }

        return GetPool(pool).Sum(stack => GetMassProfile(stack.ResourceId).Volume);
    }

    /// <summary>
    /// 返回容积制池总容积；非容积制池返回 0。
    /// </summary>
    public int GetTotalVolume(ResourcePool pool)
    {
        if (pool == ResourcePool.InStorage)
        {
            return StorageBaseVolume + _storageVolumeBonus;
        }

        if (pool == ResourcePool.Loaded)
        {
            return CargoBayBaseVolume + _cargoModuleVolumeBonus;
        }

        return 0;
    }

    /// <summary>
    /// 返回当前货舱中所有货物按 mass_class 映射计算出的总装载质量。
    /// </summary>
    public int GetTotalLoadedMass()
    {
        return GetPool(ResourcePool.Loaded)
            .Sum(stack => GetMassProfile(stack.ResourceId).Weight * stack.Quantity);
    }

    /// <summary>
    /// 返回飞艇仓库的聚合资源摘要；查询不受重入保护限制。
    /// </summary>
    public Dictionary<string, int> GetStorageSummary()
    {
        return GetPoolContents(ResourcePool.InStorage);
    }

    /// <summary>
    /// 返回随身物品栏总槽位容量。
    /// </summary>
    public int GetCarryCapacity()
    {
        return GetTotalSlots(ResourcePool.OnPerson);
    }

    /// <summary>
    /// 返回飞艇仓库总容积容量。
    /// </summary>
    public int GetStorageCapacity()
    {
        return GetTotalVolume(ResourcePool.InStorage);
    }

    /// <summary>
    /// 返回货舱总容积容量。
    /// </summary>
    public int GetCargoBayCapacity()
    {
        return GetTotalVolume(ResourcePool.Loaded);
    }

    /// <summary>
    /// Updates the effective cargo bay volume provided by installed cargo modules.
    /// Loaded goods beyond the new volume become trapped but are not destroyed.
    /// </summary>
    public void UpdateCargoBayEffectiveVolume(int newVolume)
    {
        _cargoModuleVolumeBonus = Math.Max(0, newVolume);
        _cargoBayTrappedVolume = Math.Max(0, GetUsedVolume(ResourcePool.Loaded) - GetCargoBayCapacity());
    }

    /// <summary>
    /// Current loaded cargo volume that is present but inaccessible because module capacity fell.
    /// </summary>
    public int GetCargoBayTrappedVolume()
    {
        _cargoBayTrappedVolume = Math.Max(0, GetUsedVolume(ResourcePool.Loaded) - GetCargoBayCapacity());
        return _cargoBayTrappedVolume;
    }

    /// <summary>
    /// 返回货舱占用快照，供模块系统判断模块是否可移除。
    /// </summary>
    public Dictionary<string, object?> GetCargoBayUsage()
    {
        var stacks = GetPool(ResourcePool.Loaded)
            .Select((stack, index) => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["slot_index"] = index,
                ["resource_id"] = stack.ResourceId,
                ["quantity"] = stack.Quantity,
                ["volume"] = GetMassProfile(stack.ResourceId).Volume,
                ["mass_class"] = GetMassProfile(stack.ResourceId).MassClass,
            })
            .Cast<object?>()
            .ToList();

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["used_volume"] = GetUsedVolume(ResourcePool.Loaded),
            ["effective_volume"] = GetCargoBayCapacity(),
            ["trapped_volume"] = GetCargoBayTrappedVolume(),
            ["stacks"] = stacks,
        };
    }

    /// <summary>
    /// 查询 carried 池中 supply_class 为 intel 的资源。
    /// </summary>
    public Dictionary<string, int> GetCarriedIntel()
    {
        return FilterCarried(resourceId =>
            string.Equals(GetSupplyClass(resourceId), "intel", StringComparison.Ordinal));
    }

    /// <summary>
    /// 查询 carried 池中包含指定 material tag 的资源；无匹配时返回空字典。
    /// </summary>
    public Dictionary<string, int> GetCarriedContentsByTag(string materialTag)
    {
        if (string.IsNullOrWhiteSpace(materialTag))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        return FilterCarried(resourceId => GetMaterialTags(resourceId).Contains(materialTag, StringComparer.Ordinal));
    }

    /// <summary>
    /// 航线系统入口，从飞艇仓库原子消耗航线所需资源。
    /// </summary>
    public ResourceOperationResult ConsumeForRoute(IReadOnlyDictionary<string, int> resourceCosts)
    {
        return ConsumeFromStorageCosts(resourceCosts);
    }

    /// <summary>
    /// 构建 progress.resources 快照 payload，仅包含持久化池 1-3 与容量加成。
    /// </summary>
    public Dictionary<string, object?> BuildProgressResourcesSnapshot()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["domain"] = "resources",
            ["version"] = 1,
            ["pools"] = SerializePersistedPools(),
            ["bonuses"] = BuildBonusSnapshot(),
        };
    }

    /// <summary>
    /// 构建 ADR-0003 使用的 progress.resources SnapshotPackage。
    /// </summary>
    public SnapshotPackage BuildSnapshotPackage()
    {
        var package = new SnapshotPackage
        {
            DomainId = "progress.resources",
            SnapshotSchemaVersion = 1,
            DomainState = SnapshotDomainState.Ready,
        };
        package.ContentDomainVersions["resources"] = "2026-05-09";
        package.StableIdRefs.AddRange(CollectPersistedStableIds());
        foreach (var (key, value) in BuildProgressResourcesSnapshot())
        {
            package.Payload[key] = value;
        }

        return package;
    }

    /// <summary>
    /// 将本资源系统注册到 Persistence 的 progress 领域序列化边界。
    /// </summary>
    public void RegisterPersistence(Persistence persistence)
    {
        persistence.RegisterDomainSerializer("progress.resources", BuildSnapshotPackage);
        persistence.RegisterDomainDeserializer("progress.resources", package => TryRestoreFromSnapshotPackage(package));
    }

    /// <summary>
    /// 从 SnapshotPackage 恢复 progress.resources 状态。
    /// </summary>
    public bool TryRestoreFromSnapshotPackage(SnapshotPackage package)
    {
        return package.DomainId == "progress.resources"
            && RestoreFromProgressResources(package.Payload);
    }

    /// <summary>
    /// 从 progress.resources payload 恢复池 1-3 与容量加成。
    /// </summary>
    public bool RestoreFromProgressResources(IReadOnlyDictionary<string, object?> snapshot)
    {
        if (!TryEnterMutation())
        {
            return false;
        }

        try
        {
            return RestoreFromProgressResourcesCore(snapshot);
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 重置为新游戏起始状态；未提供快照时使用 MVP 默认起始资源。
    /// </summary>
    public bool ResetForNewGame(IReadOnlyDictionary<string, object?>? startingSnapshot = null)
    {
        if (!TryEnterMutation())
        {
            return false;
        }

        try
        {
            return RestoreFromProgressResourcesCore(startingSnapshot ?? BuildDefaultStartingSnapshot());
        }
        finally
        {
            ExitMutation();
        }
    }

    /// <summary>
    /// 处理货舱模块被摧毁后的货物损失与可回收货箱生成。
    /// </summary>
    public CargoBayDestructionResult HandleCargoBayModuleDestroyed()
    {
        if (!TryEnterMutation())
        {
            return new CargoBayDestructionResult(ResourceResult.ErrBusy, [], [], GetUsedVolume(ResourcePool.Loaded));
        }

        try
        {
            return HandleCargoBayModuleDestroyedCore();
        }
        finally
        {
            ExitMutation();
        }
    }

    private Dictionary<string, int> FilterCarried(Func<string, bool> predicate)
    {
        return GetPool(ResourcePool.Carried)
            .Where(stack => predicate(stack.ResourceId))
            .GroupBy(stack => stack.ResourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity), StringComparer.Ordinal);
    }

    private string GetSupplyClass(string resourceId)
    {
        var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride: null);
        return resolution.Definition?.SupplyClass ?? _supplyClassCache.GetValueOrDefault(resourceId, string.Empty);
    }

    private IReadOnlyList<string> GetMaterialTags(string resourceId)
    {
        var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride: null);
        return resolution.Definition?.MaterialTags ?? Array.Empty<string>();
    }

    private ResourceOperationResult ConsumeFromStorageCosts(IReadOnlyDictionary<string, int> resourceCosts)
    {
        if (!TryEnterMutation())
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrBusy);
        }

        try
        {
            if (!IsInitialized)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
            }

            if (resourceCosts is null)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrMissingReference);
            }

            var validation = ValidateDepositCosts(resourceCosts);
            if (validation != ResourceResult.Success)
            {
                return ResourceOperationResult.Fail(validation);
            }

            var before = ClonePools();
            foreach (var (resourceId, quantity) in resourceCosts.Where(entry => entry.Value > 0))
            {
                var removed = RemoveCore(ResourcePool.InStorage, resourceId, quantity, emitEvents: false);
                if (!removed.Success)
                {
                    RestorePools(before);
                    return removed;
                }
            }

            foreach (var resourceId in resourceCosts.Keys.Order(StringComparer.Ordinal))
            {
                ResourceChanged?.Invoke(ResourcePool.InStorage, resourceId, GetQuantity(ResourcePool.InStorage, resourceId));
            }

            PoolChanged?.Invoke(ResourcePool.InStorage);
            return ResourceOperationResult.Ok(resourceCosts.Values.Where(quantity => quantity > 0).Sum());
        }
        finally
        {
            ExitMutation();
        }
    }

    private Dictionary<string, object?> SerializePersistedPools()
    {
        return PersistedPools.ToDictionary(
            PoolKey,
            pool => (object?)SerializePool(pool),
            StringComparer.Ordinal);
    }

    private List<object?> SerializePool(ResourcePool pool)
    {
        return GetPool(pool)
            .Select((stack, index) => SerializeStack(stack, index))
            .Cast<object?>()
            .ToList();
    }

    private Dictionary<string, object?> SerializeStack(ResourceStack stack, int index)
    {
        var serialized = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["slot_index"] = index,
            ["resource_id"] = stack.ResourceId,
            ["quantity"] = stack.Quantity,
        };

        if (!string.IsNullOrWhiteSpace(stack.LinkedResourceId))
        {
            serialized["linked_resource_id"] = stack.LinkedResourceId;
        }

        if (stack.ResourceQuantity > 0)
        {
            serialized["resource_quantity"] = stack.ResourceQuantity;
        }

        return serialized;
    }

    private Dictionary<string, object?> BuildBonusSnapshot()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["carry_slot_bonus"] = _carrySlotBonus,
            ["carry_volume_bonus"] = 0,
            ["storage_volume_bonus"] = _storageVolumeBonus,
            ["cargo_module_volume_bonus"] = _cargoModuleVolumeBonus,
        };
    }

    private IReadOnlyList<string> CollectPersistedStableIds()
    {
        return PersistedPools
            .SelectMany(pool => GetPool(pool))
            .SelectMany(stack => new[] { stack.ResourceId, stack.LinkedResourceId })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private bool RestoreFromProgressResourcesCore(IReadOnlyDictionary<string, object?> snapshot)
    {
        Initialize();
        _migrationLog.Clear();
        ApplyBonuses(ReadObjectMap(snapshot, "bonuses"));
        RestorePersistedPools(ReadObjectMap(snapshot, "pools"));
        return true;
    }

    private void ApplyBonuses(IReadOnlyDictionary<string, object?> bonuses)
    {
        _carrySlotBonus = Math.Max(0, ReadInt(bonuses, "carry_slot_bonus", 0));
        _storageVolumeBonus = Math.Max(0, ReadInt(bonuses, "storage_volume_bonus", 0));
        _cargoModuleVolumeBonus = Math.Max(0, ReadInt(bonuses, "cargo_module_volume_bonus", 0));
    }

    private void RestorePersistedPools(IReadOnlyDictionary<string, object?> pools)
    {
        foreach (var pool in PersistedPools)
        {
            foreach (var stackData in ReadObjectList(pools, PoolKey(pool)).OrderBy(stack => ReadInt(stack, "slot_index", 0)))
            {
                RestoreStackWithMigration(pool, stackData);
            }
        }
    }

    private void RestoreStackWithMigration(ResourcePool pool, IReadOnlyDictionary<string, object?> stackData)
    {
        var resourceId = ReadString(stackData, "resource_id");
        var quantity = ReadInt(stackData, "quantity", 0);
        var linkedResourceId = ReadString(stackData, "linked_resource_id");
        var resourceQuantity = ReadInt(stackData, "resource_quantity", 0);
        RestoreQuantityWithMigration(pool, resourceId, quantity, linkedResourceId, resourceQuantity);
    }

    private void RestoreQuantityWithMigration(
        ResourcePool pool,
        string resourceId,
        int quantity,
        string linkedResourceId,
        int resourceQuantity)
    {
        var remaining = quantity;
        while (remaining > 0)
        {
            var chunk = Math.Min(remaining, GetMaxStack(resourceId));
            var result = AddCore(
                pool,
                resourceId,
                chunk,
                stackRuleOverride: null,
                allowMultipleUniqueStacks: true,
                emitEvents: false,
                cargoInstance: string.IsNullOrWhiteSpace(linkedResourceId) ? null : new CargoInstance(linkedResourceId, resourceQuantity));
            if (!result.Success)
            {
                LogMigration("ERR_RESTORE_CAPACITY_OVERFLOW", pool, resourceId, remaining);
                return;
            }

            remaining -= chunk;
        }
    }

    private static Dictionary<string, object?> BuildDefaultStartingSnapshot()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["domain"] = "resources",
            ["version"] = 1,
            ["pools"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["on_person"] = new List<object?>(),
                ["in_storage"] = new List<object?>
                {
                    StackPayload(0, "resource.basic_supply", 10),
                    StackPayload(1, "resource.repair_kit", 4),
                },
                ["loaded"] = new List<object?>(),
            },
            ["bonuses"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["carry_slot_bonus"] = 0,
                ["carry_volume_bonus"] = 0,
                ["storage_volume_bonus"] = 0,
                ["cargo_module_volume_bonus"] = 500,
            },
        };
    }

    private CargoBayDestructionResult HandleCargoBayModuleDestroyedCore()
    {
        var previousVolume = GetUsedVolume(ResourcePool.Loaded);
        var beforeMass = GetTotalLoadedMass();
        var hadStacks = GetPool(ResourcePool.Loaded).Count > 0;
        var losses = new List<CargoBayLossSnapshot>();
        var crates = new List<RecoverableCrateSnapshot>();
        foreach (var stack in GetPool(ResourcePool.Loaded))
        {
            var linked = GetLinkedResourceId(stack);
            var loss = CalculateExtractionLoss(stack.Quantity, 0.4d);
            var retained = stack.Quantity - loss;
            losses.Add(new CargoBayLossSnapshot(stack.ResourceId, linked, loss, retained));
            if (retained > 0)
            {
                crates.Add(new RecoverableCrateSnapshot(stack.ResourceId, linked, retained));
            }
        }

        GetPool(ResourcePool.Loaded).Clear();
        _cargoModuleVolumeBonus = 0;
        if (hadStacks)
        {
            PoolChanged?.Invoke(ResourcePool.Loaded);
            EmitMassChangedIfChanged(beforeMass);
            CargoBayLossNotified?.Invoke(losses.Sum(loss => loss.LossQuantity), crates.Sum(crate => crate.Quantity));
        }

        return new CargoBayDestructionResult(ResourceResult.Success, losses, crates, previousVolume);
    }

    private string GetLinkedResourceId(ResourceStack stack)
    {
        if (!string.IsNullOrWhiteSpace(stack.LinkedResourceId))
        {
            return stack.LinkedResourceId;
        }

        var resolution = ResolveResourceDefinition(stack.ResourceId, stackRuleOverride: null);
        return resolution.Definition?.LinkedResourceId ?? string.Empty;
    }

    private void LogMigration(string reasonCode, ResourcePool pool, string resourceId, int quantity)
    {
        var entry = new ResourceMigrationLogEntry(reasonCode, pool, resourceId, quantity);
        _migrationLog.Add(entry);
        ResourceMigrationNotice?.Invoke(reasonCode, pool, resourceId, quantity);
    }

    private ResourceOperationResult AddCore(
        ResourcePool pool,
        string resourceId,
        int quantity,
        string? stackRuleOverride,
        bool allowMultipleUniqueStacks,
        bool emitEvents,
        CargoInstance? cargoInstance = null)
    {
        if (!IsInitialized)
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
        }

        if (quantity < 0)
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
        }

        if (quantity == 0)
        {
            return ResourceOperationResult.Ok(0);
        }

        var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride);
        if (resolution.Result != ResourceResult.Success || resolution.Definition is null)
        {
            return ResourceOperationResult.Fail(resolution.Result);
        }

        var definition = resolution.Definition;
        var kindValidation = ValidateKindForPool(pool, definition);
        if (kindValidation != ResourceResult.Success)
        {
            return ResourceOperationResult.Fail(kindValidation);
        }

        var stacks = GetPool(pool);
        var beforeTotal = GetQuantity(pool, resourceId);
        var hasMatchingStack = beforeTotal > 0;
        var cargoPayload = cargoInstance ?? CargoInstanceFromDefinition(definition);

        if (string.Equals(definition.StackRule, "unique", StringComparison.Ordinal))
        {
            if (!allowMultipleUniqueStacks && beforeTotal > 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrTargetFull);
            }

            var uniqueCapacity = CheckCapacityForNewStacks(pool, definition, quantity, hasMatchingStack);
            if (uniqueCapacity != ResourceResult.Success)
            {
                return ResourceOperationResult.Fail(uniqueCapacity, mergeQuantity: 0, overflowQuantity: quantity);
            }

            for (var i = 0; i < quantity; i++)
            {
                stacks.Add(new ResourceStack(
                    resourceId,
                    1,
                    cargoPayload?.LinkedResourceId ?? string.Empty,
                    cargoPayload?.ResourceQuantity ?? 0));
            }

            EmitAdded(pool, resourceId, quantity, emitEvents);
            return ResourceOperationResult.Ok(quantity, mergeQuantity: 0, overflowQuantity: quantity);
        }

        var remaining = quantity;
        var mergeQuantity = 0;
        var mergeTarget = stacks
            .Select((stack, index) => new { Stack = stack, Index = index })
            .Where(item =>
                string.Equals(item.Stack.ResourceId, resourceId, StringComparison.Ordinal)
                && item.Stack.Quantity < definition.MaxStack)
            .OrderByDescending(item => item.Stack.Quantity)
            .ThenBy(item => item.Index)
            .FirstOrDefault();

        if (mergeTarget is not null)
        {
            mergeQuantity = Math.Min(remaining, definition.MaxStack - mergeTarget.Stack.Quantity);
            remaining -= mergeQuantity;
        }

        var overflowQuantity = remaining;
        var newStacks = DivideRoundUp(overflowQuantity, definition.MaxStack);
        var capacity = CheckCapacityForNewStacks(pool, definition, newStacks, hasMatchingStack);
        if (capacity != ResourceResult.Success)
        {
            return ResourceOperationResult.Fail(capacity, mergeQuantity, overflowQuantity);
        }

        if (mergeTarget is not null)
        {
            mergeTarget.Stack.Quantity += mergeQuantity;
        }

        while (remaining > 0)
        {
            var stackQuantity = Math.Min(remaining, definition.MaxStack);
            stacks.Add(new ResourceStack(resourceId, stackQuantity));
            remaining -= stackQuantity;
        }

        EmitAdded(pool, resourceId, quantity, emitEvents);
        return ResourceOperationResult.Ok(quantity, mergeQuantity, overflowQuantity);
    }

    private ResourceOperationResult RemoveCore(ResourcePool pool, string resourceId, int quantity, bool emitEvents)
    {
        if (!IsInitialized)
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrNotInitialized);
        }

        if (quantity < 0)
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrInvalidQuantity);
        }

        if (quantity == 0)
        {
            return ResourceOperationResult.Ok(0);
        }

        if (pool == ResourcePool.Deposited)
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrKindMismatch);
        }

        if (GetQuantity(pool, resourceId) < quantity)
        {
            return ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient);
        }

        var stacks = GetPool(pool);
        var remaining = quantity;
        foreach (var item in stacks
            .Select((stack, index) => new { Stack = stack, Index = index })
            .Where(item => string.Equals(item.Stack.ResourceId, resourceId, StringComparison.Ordinal))
            .OrderByDescending(item => item.Stack.Quantity)
            .ThenBy(item => item.Index)
            .ToArray())
        {
            if (remaining == 0)
            {
                break;
            }

            var removed = Math.Min(remaining, item.Stack.Quantity);
            item.Stack.Quantity -= removed;
            remaining -= removed;
        }

        stacks.RemoveAll(stack => stack.Quantity <= 0);
        EmitRemoved(pool, resourceId, quantity, emitEvents);

        return ResourceOperationResult.Ok(quantity);
    }

    private ResourceDefinitionResolution ResolveResourceDefinition(string resourceId, string? stackRuleOverride)
    {
        if (_registry is not null)
        {
            var query = _registry.QueryById(resourceId);
            if (query.Status == RegistryQueryStatus.Deprecated)
            {
                return new ResourceDefinitionResolution(
                    ResourceResult.ErrDeprecatedId,
                    query.Entity is null ? null : DefinitionFromEntity(resourceId, query.Entity, stackRuleOverride));
            }

            if (query.Status != RegistryQueryStatus.Found || query.Entity is null)
            {
                return new ResourceDefinitionResolution(ResourceResult.ErrMissingReference, null);
            }

            return new ResourceDefinitionResolution(
                ResourceResult.Success,
                DefinitionFromEntity(resourceId, query.Entity, stackRuleOverride));
        }

        var supplyClass = _supplyClassCache.GetValueOrDefault(resourceId, "basic");
        var stackRule = stackRuleOverride
            ?? (string.Equals(supplyClass, "intel", StringComparison.Ordinal) ? "unique" : "stackable");
        var maxStack = SupplyClassCapacity.GetValueOrDefault(supplyClass, 99);
        if (string.Equals(stackRule, "unique", StringComparison.Ordinal))
        {
            maxStack = 1;
        }

        return new ResourceDefinitionResolution(
            ResourceResult.Success,
            new ResourceDefinition(
                resourceId,
                stackRule,
                Math.Max(1, maxStack),
                supplyClass,
                "light",
                "resource",
                string.Empty,
                0,
                Array.Empty<string>()));
    }

    private static ResourceDefinition DefinitionFromEntity(
        string resourceId,
        IReadOnlyDictionary<string, object?> entity,
        string? stackRuleOverride)
    {
        var supplyClass = ReadString(entity, "supply_class");
        var kind = ReadString(entity, "kind");
        var stackRule = stackRuleOverride ?? ReadString(entity, "stack_rule");
        if (string.IsNullOrWhiteSpace(stackRule))
        {
            stackRule = string.Equals(kind, "cargo", StringComparison.Ordinal)
                || string.Equals(supplyClass, "intel", StringComparison.Ordinal)
                    ? "unique"
                    : "stackable";
        }

        var maxStack = ReadInt(entity, "max_stack", 0);
        if (maxStack <= 0 && !string.IsNullOrWhiteSpace(supplyClass))
        {
            maxStack = SupplyClassCapacity.GetValueOrDefault(supplyClass, 99);
        }

        if (string.Equals(stackRule, "unique", StringComparison.Ordinal))
        {
            maxStack = 1;
        }

        var massClass = ReadString(entity, "mass_class");
        if (string.IsNullOrWhiteSpace(massClass))
        {
            massClass = "light";
        }

        return new ResourceDefinition(
            resourceId,
            stackRule,
            Math.Max(1, maxStack),
            supplyClass,
            massClass,
            kind,
            ReadString(entity, "linked_resource_id"),
            ReadInt(entity, "resource_quantity", 0),
            ReadStringList(entity, "material_tags"));
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> entity, string key)
    {
        return entity.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> entity, string key, int fallback)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            double doubleValue => checked((int)doubleValue),
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            _ => fallback,
        };
    }

    private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, object?> entity, string key)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text) ? Array.Empty<string>() : [text];
        }

        if (value is System.Collections.IEnumerable items)
        {
            return items
                .Cast<object?>()
                .Select(item => item?.ToString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyDictionary<string, object?> ReadObjectMap(
        IReadOnlyDictionary<string, object?> data,
        string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        if (value is IReadOnlyDictionary<string, object?> readOnly)
        {
            return readOnly;
        }

        if (value is IDictionary<string, object?> mutable)
        {
            return new Dictionary<string, object?>(mutable, StringComparer.Ordinal);
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadObjectList(
        IReadOnlyDictionary<string, object?> data,
        string key)
    {
        if (!data.TryGetValue(key, out var value) || value is not System.Collections.IEnumerable items)
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        return items
            .Cast<object?>()
            .Select(ToObjectMap)
            .Where(map => map.Count > 0)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, object?> ToObjectMap(object? value)
    {
        return value switch
        {
            IReadOnlyDictionary<string, object?> readOnly => readOnly,
            IDictionary<string, object?> mutable => new Dictionary<string, object?>(mutable, StringComparer.Ordinal),
            _ => new Dictionary<string, object?>(StringComparer.Ordinal),
        };
    }

    private static string PoolKey(ResourcePool pool)
    {
        return pool switch
        {
            ResourcePool.OnPerson => "on_person",
            ResourcePool.InStorage => "in_storage",
            ResourcePool.Loaded => "loaded",
            ResourcePool.Listed => "listed",
            ResourcePool.Carried => "carried",
            ResourcePool.Deposited => "deposited",
            _ => pool.ToString(),
        };
    }

    private static Dictionary<string, object?> StackPayload(int slotIndex, string resourceId, int quantity)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["slot_index"] = slotIndex,
            ["resource_id"] = resourceId,
            ["quantity"] = quantity,
        };
    }

    private IReadOnlyList<ResourcePool> GetChangedPools(
        IReadOnlyDictionary<ResourcePool, List<ResourceStack>> before,
        IEnumerable<ResourcePool> pools)
    {
        return pools
            .Where(pool =>
            {
                IReadOnlyList<ResourceStack> beforeStacks = before.TryGetValue(pool, out var stacks)
                    ? stacks
                    : Array.Empty<ResourceStack>();
                return !StackListsEqual(beforeStacks, GetPool(pool));
            })
            .ToArray();
    }

    private static bool StackListsEqual(IReadOnlyList<ResourceStack> left, IReadOnlyList<ResourceStack> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i].ResourceId != right[i].ResourceId || left[i].Quantity != right[i].Quantity)
            {
                return false;
            }
        }

        return true;
    }

    private static string ToErrorCode(ResourceResult result)
    {
        return result switch
        {
            ResourceResult.Success => "SUCCESS",
            ResourceResult.ErrNotInitialized => "ERR_NOT_INITIALIZED",
            ResourceResult.ErrTargetFull => "ERR_TARGET_FULL",
            ResourceResult.ErrSourceInsufficient => "ERR_SOURCE_INSUFFICIENT",
            ResourceResult.ErrInvalidQuantity => "ERR_INVALID_QUANTITY",
            ResourceResult.ErrMissingReference => "ERR_MISSING_REFERENCE",
            ResourceResult.ErrDeprecatedId => "ERR_DEPRECATED_ID",
            ResourceResult.ErrCapacityZero => "ERR_CAPACITY_ZERO",
            ResourceResult.ErrCarrySlotsFull => "ERR_CARRY_SLOTS_FULL",
            ResourceResult.ErrCarryStackFull => "ERR_CARRY_STACK_FULL",
            ResourceResult.ErrStorageFull => "ERR_STORAGE_FULL",
            ResourceResult.ErrCargoNotInBay => "ERR_CARGO_NOT_IN_BAY",
            ResourceResult.ErrBusy => "ERR_BUSY",
            ResourceResult.ErrKindMismatch => "ERR_KIND_MISMATCH",
            _ => "ERR_UNKNOWN",
        };
    }

    private void EmitAdded(ResourcePool pool, string resourceId, int quantity, bool emitEvents)
    {
        if (!emitEvents)
        {
            return;
        }

        ResourceAdded?.Invoke(pool, resourceId, quantity);
        ResourceChanged?.Invoke(pool, resourceId, GetQuantity(pool, resourceId));
        PoolChanged?.Invoke(pool);
    }

    private void EmitRemoved(ResourcePool pool, string resourceId, int quantity, bool emitEvents)
    {
        if (!emitEvents)
        {
            return;
        }

        ResourceRemoved?.Invoke(pool, resourceId, quantity);
        ResourceChanged?.Invoke(pool, resourceId, GetQuantity(pool, resourceId));
        PoolChanged?.Invoke(pool);
    }

    private void EmitMassChangedIfNeeded(ResourcePool pool, int beforeMass, bool success)
    {
        if (success && pool == ResourcePool.Loaded)
        {
            EmitMassChangedIfChanged(beforeMass);
        }
    }

    private void EmitMassChangedForLoadedTransfer(ResourcePool fromPool, ResourcePool toPool, int beforeMass)
    {
        if (fromPool == ResourcePool.Loaded || toPool == ResourcePool.Loaded)
        {
            EmitMassChangedIfChanged(beforeMass);
        }
    }

    private void EmitMassChangedIfChanged(int beforeMass)
    {
        var afterMass = GetTotalLoadedMass();
        if (afterMass != beforeMass)
        {
            MassChanged?.Invoke(afterMass);
        }
    }

    private void EmitDepositFailed(string repairNodeId, ResourceResult result)
    {
        DepositFailed?.Invoke(repairNodeId, ToErrorCode(result));
    }

    private bool TryEnterMutation()
    {
        if (_isMutating)
        {
            return false;
        }

        _isMutating = true;
        return true;
    }

    private void ExitMutation()
    {
        _isMutating = false;
    }

    private List<ResourceStack> GetPool(ResourcePool pool)
    {
        if (!_pools.TryGetValue(pool, out var stacks))
        {
            stacks = new List<ResourceStack>();
            _pools[pool] = stacks;
        }

        return stacks;
    }

    private Dictionary<ResourcePool, List<ResourceStack>> ClonePools()
    {
        return _pools.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(stack => stack.Clone()).ToList());
    }

    private void RestorePools(Dictionary<ResourcePool, List<ResourceStack>> snapshot)
    {
        _pools.Clear();
        foreach (var (pool, stacks) in snapshot)
        {
            _pools[pool] = stacks.Select(stack => stack.Clone()).ToList();
        }
    }

    private ResourceResult CheckCapacityForNewStacks(
        ResourcePool pool,
        ResourceDefinition definition,
        int newStacks,
        bool hasMatchingStack)
    {
        if (newStacks <= 0)
        {
            return ResourceResult.Success;
        }

        if (IsSlotBasedPool(pool))
        {
            return GetUsedSlots(pool) + newStacks <= GetTotalSlots(pool)
                ? ResourceResult.Success
                : hasMatchingStack ? ResourceResult.ErrCarryStackFull : ResourceResult.ErrCarrySlotsFull;
        }

        if (IsVolumeBasedPool(pool))
        {
            var totalVolume = GetTotalVolume(pool);
            if (totalVolume <= 0)
            {
                return ResourceResult.ErrCapacityZero;
            }

            var requiredVolume = checked(newStacks * GetMassProfileForClass(definition.MassClass).Volume);
            return GetUsedVolume(pool) + requiredVolume <= totalVolume
                ? ResourceResult.Success
                : ResourceResult.ErrTargetFull;
        }

        return ResourceResult.Success;
    }

    private static ResourceMassProfile GetMassProfileForClass(string massClass)
    {
        return MassClassProfiles.GetValueOrDefault(massClass, MassClassProfiles["light"]);
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        return value <= 0 ? 0 : (value + divisor - 1) / divisor;
    }

    private ResourceResult ValidateDepositRequest(
        string repairNodeId,
        IReadOnlyDictionary<string, int> resourceCosts)
    {
        if (!IsInitialized)
        {
            return ResourceResult.ErrNotInitialized;
        }

        if (!IsValidDomainId(repairNodeId) || resourceCosts is null)
        {
            return ResourceResult.ErrMissingReference;
        }

        return ValidateDepositCosts(resourceCosts);
    }

    private ResourceResult ValidateDepositCosts(IReadOnlyDictionary<string, int> resourceCosts)
    {
        foreach (var (resourceId, quantity) in resourceCosts)
        {
            if (quantity < 0)
            {
                return ResourceResult.ErrInvalidQuantity;
            }

            if (quantity == 0)
            {
                continue;
            }

            var resolution = ResolveResourceDefinition(resourceId, stackRuleOverride: null);
            if (resolution.Result != ResourceResult.Success || resolution.Definition is null)
            {
                return resolution.Result;
            }

            if (IsCargoKind(resolution.Definition))
            {
                return ResourceResult.ErrKindMismatch;
            }
        }

        return ResourceResult.Success;
    }

    private bool HasDepositableResources(IReadOnlyDictionary<string, int> resourceCosts)
    {
        foreach (var (resourceId, quantity) in resourceCosts)
        {
            if (quantity > 0 && GetDepositableQuantity(resourceId) < quantity)
            {
                return false;
            }
        }

        return true;
    }

    private int GetDepositableQuantity(string resourceId)
    {
        return DepositSourcePools.Sum(pool => GetQuantity(pool, resourceId));
    }

    private ResourceOperationResult MoveDepositCosts(IReadOnlyDictionary<string, int> resourceCosts)
    {
        var changed = 0;
        foreach (var (resourceId, quantity) in resourceCosts)
        {
            if (quantity <= 0)
            {
                continue;
            }

            var removed = RemoveFromDepositSources(resourceId, quantity);
            if (!removed.Success)
            {
                return removed;
            }

            var added = AddCore(ResourcePool.Deposited, resourceId, quantity, null, true, false);
            if (!added.Success)
            {
                return added;
            }

            changed += quantity;
        }

        return ResourceOperationResult.Ok(changed);
    }

    private ResourceOperationResult RemoveFromDepositSources(string resourceId, int quantity)
    {
        var remaining = quantity;
        foreach (var pool in DepositSourcePools)
        {
            var available = GetQuantity(pool, resourceId);
            var take = Math.Min(remaining, available);
            if (take <= 0)
            {
                continue;
            }

            var removed = RemoveCore(pool, resourceId, take, emitEvents: false);
            if (!removed.Success)
            {
                return removed;
            }

            remaining -= take;
            if (remaining == 0)
            {
                return ResourceOperationResult.Ok(quantity);
            }
        }

        return ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient);
    }

    private Dictionary<string, int> GetCarriedQuantitySummary()
    {
        var summary = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var stack in GetPool(ResourcePool.Carried))
        {
            AddSummaryQuantity(summary, stack.ResourceId, stack.Quantity);
        }

        return summary;
    }

    private ResourceQuantity[] GetCarriedStackQuantities()
    {
        return GetPool(ResourcePool.Carried)
            .Select(stack => new ResourceQuantity(stack.ResourceId, stack.Quantity))
            .ToArray();
    }

    private ResourceResult AddSummaryToStorage(IReadOnlyDictionary<string, int> summary)
    {
        foreach (var (resourceId, quantity) in summary)
        {
            if (quantity <= 0)
            {
                continue;
            }

            var added = AddCore(
                ResourcePool.InStorage,
                resourceId,
                quantity,
                stackRuleOverride: null,
                allowMultipleUniqueStacks: true,
                emitEvents: false);
            if (!added.Success)
            {
                return added.Result;
            }
        }

        return ResourceResult.Success;
    }

    private int GetPurchasePrice(string goodId)
    {
        if (_registry is null)
        {
            return 0;
        }

        var query = _registry.QueryById(goodId);
        return query.Status == RegistryQueryStatus.Found && query.Entity is not null
            ? ReadInt(query.Entity, "price", 0)
            : 0;
    }

    private bool IsMarketGood(string goodId)
    {
        if (_registry is null)
        {
            return false;
        }

        var query = _registry.QueryById(goodId);
        return query.Status == RegistryQueryStatus.Found
            && query.Entity is not null
            && query.Entity.ContainsKey("price");
    }

    private ResourceResult PreviewAddToStorage(string goodId, int quantity)
    {
        var before = ClonePools();
        try
        {
            return AddCore(
                ResourcePool.InStorage,
                goodId,
                quantity,
                stackRuleOverride: null,
                allowMultipleUniqueStacks: true,
                emitEvents: false).Result;
        }
        finally
        {
            RestorePools(before);
        }
    }

    private ResourceResult BuildExtractionLossSummary(
        IReadOnlyList<ResourceQuantity> carriedStacks,
        double normalizedRatio,
        Dictionary<string, int> retained,
        Dictionary<string, int> lost)
    {
        foreach (var carried in carriedStacks)
        {
            var resolution = ResolveResourceDefinition(carried.ResourceId, stackRuleOverride: null);
            if (resolution.Result != ResourceResult.Success || resolution.Definition is null)
            {
                return resolution.Result;
            }

            var loss = CalculateExtractionLoss(carried.Quantity, normalizedRatio);
            AddSummaryQuantity(retained, carried.ResourceId, carried.Quantity - loss);
            AddSummaryQuantity(lost, carried.ResourceId, loss);
        }

        return ResourceResult.Success;
    }

    private void ClearCarriedAndEmit(
        IReadOnlyDictionary<string, int> removed,
        IReadOnlyDictionary<string, int> retained)
    {
        GetPool(ResourcePool.Carried).Clear();
        foreach (var (resourceId, quantity) in removed)
        {
            ResourceRemoved?.Invoke(ResourcePool.Carried, resourceId, quantity);
            ResourceChanged?.Invoke(ResourcePool.Carried, resourceId, 0);
        }

        foreach (var (resourceId, quantity) in retained)
        {
            if (quantity <= 0)
            {
                continue;
            }

            ResourceAdded?.Invoke(ResourcePool.InStorage, resourceId, quantity);
            ResourceChanged?.Invoke(ResourcePool.InStorage, resourceId, GetQuantity(ResourcePool.InStorage, resourceId));
        }
    }

    private static int CalculateExtractionLoss(int quantity, double lossRatio)
    {
        if (quantity <= 1 || lossRatio <= 0.0d)
        {
            return 0;
        }

        var proportionalLoss = (int)Math.Ceiling(quantity * lossRatio);
        return Math.Min(quantity - 1, Math.Max(1, proportionalLoss));
    }

    private static void AddSummaryQuantity(Dictionary<string, int> summary, string resourceId, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        summary[resourceId] = summary.GetValueOrDefault(resourceId, 0) + quantity;
    }

    private static bool IsSlotBasedPool(ResourcePool pool)
    {
        return pool is ResourcePool.OnPerson or ResourcePool.Carried;
    }

    private static bool IsVolumeBasedPool(ResourcePool pool)
    {
        return pool is ResourcePool.InStorage or ResourcePool.Loaded;
    }

    private static bool IsTransferTargetCapacityFailure(ResourceResult result)
    {
        return result is ResourceResult.ErrTargetFull
            or ResourceResult.ErrCarrySlotsFull
            or ResourceResult.ErrCarryStackFull;
    }

    private static bool IsDiscardablePool(ResourcePool pool)
    {
        return pool is ResourcePool.OnPerson
            or ResourcePool.InStorage
            or ResourcePool.Loaded
            or ResourcePool.Carried;
    }

    private static bool IsValidDomainId(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private static ResourceResult ValidateKindForPool(ResourcePool pool, ResourceDefinition definition)
    {
        if (IsCargoKind(definition))
        {
            return pool == ResourcePool.Loaded
                ? ResourceResult.Success
                : ResourceResult.ErrKindMismatch;
        }

        return pool == ResourcePool.Loaded
            ? ResourceResult.ErrKindMismatch
            : ResourceResult.Success;
    }

    private static bool IsCargoKind(ResourceDefinition definition)
    {
        return string.Equals(definition.Kind, "cargo", StringComparison.Ordinal);
    }

    private static CargoInstance? CargoInstanceFromDefinition(ResourceDefinition definition)
    {
        if (!IsCargoKind(definition))
        {
            return null;
        }

        return new CargoInstance(definition.LinkedResourceId, definition.ResourceQuantity);
    }

    private sealed record ResourceDefinition(
        string ResourceId,
        string StackRule,
        int MaxStack,
        string SupplyClass,
        string MassClass,
        string Kind,
        string LinkedResourceId,
        int ResourceQuantity,
        IReadOnlyList<string> MaterialTags);

    private sealed record ResourceDefinitionResolution(ResourceResult Result, ResourceDefinition? Definition);

    private sealed record CargoInstance(string LinkedResourceId, int ResourceQuantity);

    private sealed record ResourceQuantity(string ResourceId, int Quantity);

    private sealed class ResourceStack
    {
        public ResourceStack(
            string resourceId,
            int quantity,
            string linkedResourceId = "",
            int resourceQuantity = 0)
        {
            ResourceId = resourceId;
            Quantity = quantity;
            LinkedResourceId = linkedResourceId;
            ResourceQuantity = resourceQuantity;
        }

        public string ResourceId { get; }

        public int Quantity { get; set; }

        public string LinkedResourceId { get; }

        public int ResourceQuantity { get; }

        public ResourceStack Clone()
        {
            return new ResourceStack(ResourceId, Quantity, LinkedResourceId, ResourceQuantity);
        }
    }
}
