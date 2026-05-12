using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 资源池标识。旧名称保留给既有 C# parity 测试，新名称映射到当前 GDD 池语义。
/// </summary>
public enum ResourcePool
{
    Carried = 0,
    Storage = 1,
    Repair = 2,
    Supply = 3,
    Currency = 4,
    Cargo = 5,
    OnPerson = Carried,
    InStorage = Storage,
    Deposited = Repair,
    Listed = Supply,
    Loaded = Cargo,
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
}

/// <summary>
/// 资源堆的只读快照，用于测试、UI 查询和下游系统读取当前池状态。
/// </summary>
public sealed record ResourceStackSnapshot(int SlotIndex, string ResourceId, int Quantity);

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
    public static ResourceOperationResult Fail(ResourceResult result)
    {
        return new ResourceOperationResult(result, 0, 0, 0);
    }
}

/// <summary>
/// 资源系统的 C# 核心模型，负责稳定 ID 身份、堆叠合并和池内数量查询。
/// </summary>
public sealed class ResourcesManager
{
    private static readonly Dictionary<string, int> SupplyClassCapacity = new(StringComparer.Ordinal)
    {
        ["basic"] = 99,
        ["repair"] = 99,
        ["navigation"] = 20,
        ["local-specialty"] = 10,
        ["local_specialty"] = 10,
        ["intel"] = 1,
    };

    private readonly Registry? _registry;
    private readonly Dictionary<ResourcePool, List<ResourceStack>> _pools = new();
    private readonly Dictionary<string, string> _supplyClassCache = new(StringComparer.Ordinal);

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
    /// 将指定资源添加到目标池，按 ADR-0005 的 fill-fullest-first 规则合并。
    /// </summary>
    public ResourceOperationResult Add(ResourcePool pool, string resourceId, int quantity)
    {
        return AddCore(pool, resourceId, quantity, stackRuleOverride: null, allowMultipleUniqueStacks: true, emitEvents: true);
    }

    /// <summary>
    /// 从指定池移除资源。数量不足时全操作失败且不修改状态。
    /// </summary>
    public ResourceOperationResult Remove(ResourcePool pool, string resourceId, int quantity)
    {
        return RemoveCore(pool, resourceId, quantity, emitEvents: true);
    }

    /// <summary>
    /// 按稳定 ID 在两个池之间转移资源；匹配不依赖显示名或其他 UI 字段。
    /// </summary>
    public ResourceOperationResult Transfer(ResourcePool fromPool, ResourcePool toPool, string resourceId, int quantity)
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
            return added;
        }

        ResourceRemoved?.Invoke(fromPool, resourceId, quantity);
        ResourceAdded?.Invoke(toPool, resourceId, quantity);
        ResourceChanged?.Invoke(fromPool, resourceId, GetQuantity(fromPool, resourceId));
        ResourceChanged?.Invoke(toPool, resourceId, GetQuantity(toPool, resourceId));
        return ResourceOperationResult.Ok(quantity, added.MergeQuantity, added.OverflowQuantity);
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

        var result = AddCore(
            pool,
            resourceId,
            quantity,
            stackRuleOverride: stackRule,
            allowMultipleUniqueStacks: false,
            emitEvents: true);

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

    private ResourceOperationResult AddCore(
        ResourcePool pool,
        string resourceId,
        int quantity,
        string? stackRuleOverride,
        bool allowMultipleUniqueStacks,
        bool emitEvents)
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
        var stacks = GetPool(pool);
        var beforeTotal = GetQuantity(pool, resourceId);

        if (string.Equals(definition.StackRule, "unique", StringComparison.Ordinal))
        {
            if (!allowMultipleUniqueStacks && beforeTotal > 0)
            {
                return ResourceOperationResult.Fail(ResourceResult.ErrTargetFull);
            }

            for (var i = 0; i < quantity; i++)
            {
                stacks.Add(new ResourceStack(resourceId, 1));
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
            mergeTarget.Stack.Quantity += mergeQuantity;
            remaining -= mergeQuantity;
        }

        var overflowQuantity = remaining;
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
        if (emitEvents)
        {
            ResourceRemoved?.Invoke(pool, resourceId, quantity);
            ResourceChanged?.Invoke(pool, resourceId, GetQuantity(pool, resourceId));
        }

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
            new ResourceDefinition(resourceId, stackRule, Math.Max(1, maxStack), supplyClass));
    }

    private static ResourceDefinition DefinitionFromEntity(
        string resourceId,
        IReadOnlyDictionary<string, object?> entity,
        string? stackRuleOverride)
    {
        var supplyClass = ReadString(entity, "supply_class");
        var stackRule = stackRuleOverride ?? ReadString(entity, "stack_rule");
        if (string.IsNullOrWhiteSpace(stackRule))
        {
            stackRule = string.Equals(supplyClass, "intel", StringComparison.Ordinal) ? "unique" : "stackable";
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

        return new ResourceDefinition(
            resourceId,
            stackRule,
            Math.Max(1, maxStack),
            supplyClass);
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

    private void EmitAdded(ResourcePool pool, string resourceId, int quantity, bool emitEvents)
    {
        if (!emitEvents)
        {
            return;
        }

        ResourceAdded?.Invoke(pool, resourceId, quantity);
        ResourceChanged?.Invoke(pool, resourceId, GetQuantity(pool, resourceId));
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

    private sealed record ResourceDefinition(
        string ResourceId,
        string StackRule,
        int MaxStack,
        string SupplyClass);

    private sealed record ResourceDefinitionResolution(ResourceResult Result, ResourceDefinition? Definition);

    private sealed class ResourceStack
    {
        public ResourceStack(string resourceId, int quantity)
        {
            ResourceId = resourceId;
            Quantity = quantity;
        }

        public string ResourceId { get; }

        public int Quantity { get; set; }

        public ResourceStack Clone()
        {
            return new ResourceStack(ResourceId, Quantity);
        }
    }
}
