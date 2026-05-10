using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Feature;

/// <summary>
/// Repair node lifecycle state.
/// </summary>
public enum RepairState
{
    Unknown = 0,
    Known = 1,
    MaterialsCommitted = 2,
    Repairing = 3,
    Repaired = 4,
}

/// <summary>
/// Sole Feature-layer Autoload. Owns repair conditions, state changes,
/// and unlock results. repair_completed consumed by 4 cross-layer systems.
/// </summary>
public sealed class WorldRepair
{
    private readonly Dictionary<string, RepairNodeState> repairNodes = new(StringComparer.Ordinal);
    private readonly List<string> completedNodeIds = new();

    /// <summary>Raised when a repair node is completed.</summary>
    public event Action<string>? RepairCompleted;

    /// <summary>Raised when a repair deposit fails.</summary>
    public event Action<string, string>? RepairFailed;

    /// <summary>Raised when materials are deposited to a repair node.</summary>
    public event Action<string, string, int>? DepositCommitted;

    /// <summary>Whether the system has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Marks the repair system as ready.</summary>
    public void Initialize()
    {
        IsInitialized = true;
    }

    /// <summary>Registers a repair node with required materials.</summary>
    public void RegisterRepairNode(string nodeId, Dictionary<string, int> requirements)
    {
        repairNodes[nodeId] = new RepairNodeState
        {
            State = RepairState.Known,
            Requirements = requirements,
            Deposited = new Dictionary<string, int>(StringComparer.Ordinal),
        };
    }

    /// <summary>Checks whether a repair node can accept deposits.</summary>
    public bool CanDeposit(string nodeId)
    {
        return repairNodes.TryGetValue(nodeId, out var node)
            && node.State < RepairState.Repaired;
    }

    /// <summary>
    /// Commits a material deposit to a repair node.
    /// If all requirements are met, the node transitions to Repaired.
    /// </summary>
    public bool CommitDeposit(string nodeId, string resourceId, int quantity)
    {
        if (!CanDeposit(nodeId))
        {
            RepairFailed?.Invoke(nodeId, "cannot_deposit");
            return false;
        }

        var node = repairNodes[nodeId];
        node.Deposited.TryGetValue(resourceId, out var current);
        node.Deposited[resourceId] = current + quantity;
        DepositCommitted?.Invoke(nodeId, resourceId, quantity);

        var allMet = true;
        foreach (var (reqId, required) in node.Requirements)
        {
            var deposited = node.Deposited.GetValueOrDefault(reqId, 0);
            if (deposited < required)
            {
                allMet = false;
                break;
            }
        }

        if (allMet)
        {
            node.State = RepairState.Repaired;
            completedNodeIds.Add(nodeId);
            RepairCompleted?.Invoke(nodeId);
        }

        return true;
    }

    /// <summary>Returns the list of completed repair node IDs.</summary>
    public IReadOnlyList<string> GetCompletedNodes()
    {
        return completedNodeIds;
    }

    /// <summary>Returns the current state of a repair node.</summary>
    public RepairState GetNodeState(string nodeId)
    {
        return repairNodes.TryGetValue(nodeId, out var node)
            ? node.State
            : RepairState.Unknown;
    }

    private sealed class RepairNodeState
    {
        public RepairState State { get; set; } = RepairState.Unknown;
        public Dictionary<string, int> Requirements { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> Deposited { get; set; } = new(StringComparer.Ordinal);
    }
}
