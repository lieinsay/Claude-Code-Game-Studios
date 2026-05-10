using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Resource pool identifiers matching the legacy GDScript prototype.
/// </summary>
public enum ResourcePool
{
    Carried = 0,
    Storage = 1,
    Repair = 2,
    Supply = 3,
    Currency = 4,
    Cargo = 5,
}

/// <summary>
/// Owns 6 resource pools with "fill fullest first" stack merge.
/// Shared by Hub, Exploration, and Settlement.
/// </summary>
public sealed class ResourcesManager
{
    private static readonly Dictionary<string, int> SupplyClassCapacity = new(StringComparer.Ordinal)
    {
        ["basic"] = 99,
        ["repair"] = 99,
        ["navigation"] = 20,
        ["local_specialty"] = 10,
        ["intel"] = 1,
    };

    private readonly Dictionary<ResourcePool, Dictionary<string, int>> pools = new();
    private readonly Dictionary<string, string> supplyClassCache = new(StringComparer.Ordinal);

    /// <summary>Raised when a resource is added to a pool.</summary>
    public event Action<ResourcePool, string, int>? ResourceAdded;

    /// <summary>Raised when a resource is removed from a pool.</summary>
    public event Action<ResourcePool, string, int>? ResourceRemoved;

    /// <summary>Raised when a resource quantity changes.</summary>
    public event Action<ResourcePool, string, int>? ResourceChanged;

    /// <summary>Whether the manager has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Initializes all 6 resource pools.</summary>
    public void Initialize()
    {
        foreach (ResourcePool pool in Enum.GetValues<ResourcePool>())
        {
            pools[pool] = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        IsInitialized = true;
    }

    /// <summary>Registers a supply class for a resource ID to use in stack cap lookups.</summary>
    public void RegisterSupplyClass(string resourceId, string supplyClass)
    {
        supplyClassCache[resourceId] = supplyClass;
    }

    /// <summary>
    /// Adds items to a pool using "fill fullest first" merge strategy.
    /// Returns the quantity actually added (may be less if capacity-constrained).
    /// </summary>
    public int AddItem(ResourcePool pool, string resourceId, int quantity, string stackRule = "stackable")
    {
        if (!IsInitialized)
        {
            return 0;
        }

        var poolDict = pools[pool];
        if (stackRule == "unique")
        {
            if (!poolDict.ContainsKey(resourceId))
            {
                poolDict[resourceId] = 1;
                ResourceAdded?.Invoke(pool, resourceId, 1);
                return 1;
            }

            return 0;
        }

        var maxStack = GetMaxStack(resourceId);
        var remaining = quantity;

        if (poolDict.TryGetValue(resourceId, out var existingQty) && existingQty > 0)
        {
            var space = maxStack - existingQty;
            if (space > 0)
            {
                var toAdd = Math.Min(remaining, space);
                poolDict[resourceId] += toAdd;
                remaining -= toAdd;
            }
        }

        while (remaining > 0)
        {
            var newStackQty = Math.Min(remaining, maxStack);
            if (poolDict.ContainsKey(resourceId))
            {
                poolDict[resourceId] += newStackQty;
            }
            else
            {
                poolDict[resourceId] = newStackQty;
            }

            remaining -= newStackQty;
        }

        var added = quantity - remaining;
        if (added > 0)
        {
            ResourceAdded?.Invoke(pool, resourceId, added);
            ResourceChanged?.Invoke(pool, resourceId, poolDict[resourceId]);
        }

        return added;
    }

    /// <summary>
    /// Removes items from a pool. Returns the quantity actually removed.
    /// </summary>
    public int RemoveItem(ResourcePool pool, string resourceId, int quantity)
    {
        var poolDict = pools[pool];
        if (!poolDict.TryGetValue(resourceId, out var current))
        {
            return 0;
        }

        var removed = Math.Min(quantity, current);
        poolDict[resourceId] -= removed;
        if (poolDict[resourceId] <= 0)
        {
            poolDict.Remove(resourceId);
        }

        ResourceRemoved?.Invoke(pool, resourceId, removed);
        ResourceChanged?.Invoke(pool, resourceId, poolDict.GetValueOrDefault(resourceId, 0));
        return removed;
    }

    /// <summary>Returns the quantity of a resource in a pool.</summary>
    public int GetQuantity(ResourcePool pool, string resourceId)
    {
        return pools[pool].GetValueOrDefault(resourceId, 0);
    }

    /// <summary>Returns true when a pool has at least the specified quantity.</summary>
    public bool HasItem(ResourcePool pool, string resourceId, int quantity = 1)
    {
        return GetQuantity(pool, resourceId) >= quantity;
    }

    /// <summary>Returns a copy of the pool contents.</summary>
    public Dictionary<string, int> GetPoolContents(ResourcePool pool)
    {
        return new Dictionary<string, int>(pools[pool], StringComparer.Ordinal);
    }

    /// <summary>Returns the max stack size for a resource ID.</summary>
    public int GetMaxStack(string resourceId)
    {
        if (supplyClassCache.TryGetValue(resourceId, out var supplyClass)
            && SupplyClassCapacity.TryGetValue(supplyClass, out var cap))
        {
            return cap;
        }

        return 99;
    }
}
