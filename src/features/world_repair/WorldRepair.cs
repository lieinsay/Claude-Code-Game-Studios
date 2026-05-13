using System;
using System.Collections.Generic;
using System.Linq;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Feature;

/// <summary>
/// Repair node lifecycle state for WorldRepair Autoload #13.
/// </summary>
public enum RepairState
{
    Unknown = -1,
    Unrevealed = 0,
    Known = 1,
    Repaired = 2,
}

/// <summary>
/// Static repair node definition read from Registry.
/// </summary>
public sealed record RepairNodeDefinition(
    string NodeId,
    string Name,
    string LinkedLocationId,
    IReadOnlyDictionary<string, int> RequiredResources,
    IReadOnlyList<string> UnlockedRoutes,
    string RouteEnhancementEffect,
    double RouteEnhancementMagnitude,
    bool PreRepairRouteTraversable,
    string VisualStateAnchor);

/// <summary>
/// Runtime snapshot for one repair node.
/// </summary>
public sealed record RepairNodeSnapshot(
    string NodeId,
    RepairState RepairState,
    IReadOnlyDictionary<string, int> Deposited,
    double RepairProgress,
    string VisualState);

/// <summary>
/// Result returned by state transition attempts.
/// </summary>
public sealed record RepairStateTransitionResult(RepairState NewState, bool Allowed, string Reason);

/// <summary>
/// Interaction contract for repair UI without blocking physical arrival.
/// </summary>
public sealed record RepairInteractionInfo(
    bool NodeExists,
    bool InteractionAvailable,
    bool MaterialsRevealed,
    IReadOnlyDictionary<string, string> MaterialLabels,
    string UnlockPreview,
    string VisualState);

/// <summary>
/// Machine-readable deposit validation violation.
/// </summary>
public enum RepairDepositViolation
{
    InvalidNode,
    EmptyOffer,
    InvalidMaterial,
    ExcessQuantity,
    AlreadyRepaired,
}

/// <summary>
/// submit_deposit result code.
/// </summary>
public enum RepairSubmitResult
{
    Success,
    ErrValidationFailed,
    ErrCommitFailed,
}

/// <summary>
/// Validation result for one offered repair deposit batch.
/// </summary>
public sealed record DepositValidationResult(
    bool Valid,
    IReadOnlyList<RepairDepositViolation> Violations);

/// <summary>
/// Result returned by submit_deposit.
/// </summary>
public sealed record RepairDepositResult(
    RepairSubmitResult Result,
    IReadOnlyList<RepairDepositViolation> Violations,
    IReadOnlyDictionary<string, int> Deposited,
    double Progress,
    bool Completed);

/// <summary>
/// Route enhancement payload produced when a repair node completes.
/// </summary>
public sealed record RouteEnhancementPayload(
    string RouteId,
    string EffectType,
    double Magnitude,
    bool Unlock);

/// <summary>
/// Sole Feature-layer owner of world repair node lifecycle state.
/// </summary>
public sealed class WorldRepair
{
    public const string MvpNodeId = "repair_node.starlight_dock";
    public const string VisualStateKnown = "known";
    public const string VisualStateRepaired = "repaired";

    private readonly Registry? _registry;
    private readonly Dictionary<string, RepairNodeDefinition> _definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RepairNodeState> _repairNodes = new(StringComparer.Ordinal);
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private readonly List<string> _completedNodeIds = [];
    private Func<string, IReadOnlyDictionary<string, int>, ResourceOperationResult>? _commitDeposit;

    /// <summary>Raised when a repair node visual state changes.</summary>
    public event Action<string, string>? VisualStateChanged;

    /// <summary>Raised after deposited counters and progress are updated.</summary>
    public event Action<string, double, IReadOnlyDictionary<string, int>>? RepairProgressChanged;

    /// <summary>Raised when a repair node is completed.</summary>
    public event Action<string>? RepairCompleted;

    /// <summary>Raised when a repair deposit fails.</summary>
    public event Action<string, string>? RepairFailed;

    /// <summary>Raised when materials are deposited to a repair node.</summary>
    public event Action<string, string, int>? DepositCommitted;

    /// <summary>Whether the system has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Non-fatal diagnostics collected for tests and debug panels.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>Fatal or configuration diagnostics collected for tests and debug panels.</summary>
    public IReadOnlyList<string> Errors => _errors;

    public WorldRepair(Registry? registry = null)
    {
        _registry = registry;
    }

    /// <summary>Injects the ResourcesManager commit_deposit implementation.</summary>
    public void SetResourcesManager(ResourcesManager resources)
    {
        _commitDeposit = resources.CommitDeposit;
    }

    /// <summary>Injects a test or adapter commit_deposit handler.</summary>
    public void SetCommitDepositHandler(Func<string, IReadOnlyDictionary<string, int>, ResourceOperationResult> handler)
    {
        _commitDeposit = handler;
    }

    /// <summary>Marks the repair system as ready and loads new-game state.</summary>
    public void Initialize()
    {
        IsInitialized = true;
        InitNewGameState();
    }

    /// <summary>
    /// Initializes a new game from Registry repair-node definitions.
    /// </summary>
    public void InitNewGameState()
    {
        _definitions.Clear();
        _repairNodes.Clear();
        _completedNodeIds.Clear();

        foreach (var definition in LoadDefinitionsFromRegistry())
        {
            _definitions[definition.NodeId] = definition;
            _repairNodes[definition.NodeId] = new RepairNodeState
            {
                RepairState = RepairState.Unrevealed,
                VisualState = VisualStateKnown,
            };
        }

        if (!_repairNodes.ContainsKey(MvpNodeId))
        {
            _errors.Add($"WorldRepair: MVP node '{MvpNodeId}' not found in Registry");
        }
    }

    /// <summary>
    /// Registers a repair node directly. Kept for existing parity runners and focused tests.
    /// </summary>
    public void RegisterRepairNode(string nodeId, Dictionary<string, int> requirements)
    {
        RegisterRepairNodeDefinition(new RepairNodeDefinition(
            nodeId,
            nodeId,
            "",
            new Dictionary<string, int>(requirements, StringComparer.Ordinal),
            Array.Empty<string>(),
            "",
            0.0d,
            true,
            ""));
    }

    /// <summary>
    /// Registers a full repair node definition directly.
    /// </summary>
    public void RegisterRepairNodeDefinition(RepairNodeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.NodeId))
        {
            _errors.Add("WorldRepair: repair node definition missing node_id");
            return;
        }

        _definitions[definition.NodeId] = definition;
        _repairNodes[definition.NodeId] = new RepairNodeState
        {
            RepairState = RepairState.Unrevealed,
            VisualState = VisualStateKnown,
        };
    }

    /// <summary>
    /// Player physically arrived at a repair node. This always reveals the node if it exists.
    /// </summary>
    public void OnPlayerArrivedAtRepairNode(string nodeId)
    {
        if (!_repairNodes.ContainsKey(nodeId))
        {
            _warnings.Add($"WorldRepair: unknown repair node '{nodeId}'");
            return;
        }

        if (GetRepairState(nodeId) != RepairState.Unrevealed)
        {
            return;
        }

        if (TryTransitionState(nodeId, RepairState.Known).Allowed)
        {
            _repairNodes[nodeId].VisualState = VisualStateKnown;
            VisualStateChanged?.Invoke(nodeId, VisualStateKnown);
        }
    }

    /// <summary>
    /// Intel reveal can also promote an unrevealed node to known.
    /// </summary>
    public void OnIntelRevealedRepairNode(string nodeId)
    {
        if (!_repairNodes.ContainsKey(nodeId))
        {
            _warnings.Add($"WorldRepair: unknown repair node '{nodeId}'");
            return;
        }

        TryTransitionState(nodeId, RepairState.Known);
    }

    /// <summary>
    /// Attempts the only valid lifecycle transitions.
    /// </summary>
    public RepairStateTransitionResult TryTransitionState(string nodeId, RepairState targetState)
    {
        if (!_repairNodes.TryGetValue(nodeId, out var node))
        {
            _warnings.Add($"WorldRepair: unknown repair node '{nodeId}'");
            return new RepairStateTransitionResult(RepairState.Unknown, false, "invalid_node");
        }

        var current = node.RepairState;
        if (targetState == RepairState.Known)
        {
            if (current == RepairState.Unrevealed)
            {
                node.RepairState = RepairState.Known;
                return new RepairStateTransitionResult(RepairState.Known, true, "revealed");
            }

            return new RepairStateTransitionResult(current, false, "no_op");
        }

        if (targetState == RepairState.Repaired)
        {
            if (current == RepairState.Known)
            {
                node.RepairState = RepairState.Repaired;
                node.VisualState = VisualStateRepaired;
                if (!_completedNodeIds.Contains(nodeId, StringComparer.Ordinal))
                {
                    _completedNodeIds.Add(nodeId);
                }

                return new RepairStateTransitionResult(RepairState.Repaired, true, "completed");
            }

            _warnings.Add($"WorldRepair: invalid transition {current} -> {targetState} for '{nodeId}'");
            return new RepairStateTransitionResult(current, false, "invalid_transition");
        }

        _warnings.Add($"WorldRepair: invalid target state '{targetState}' for '{nodeId}'");
        return new RepairStateTransitionResult(current, false, "invalid_transition");
    }

    /// <summary>Returns the current state of a repair node.</summary>
    public RepairState GetRepairState(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node)
            ? node.RepairState
            : RepairState.Unknown;
    }

    /// <summary>Returns the current state of a repair node. Compatibility alias.</summary>
    public RepairState GetNodeState(string nodeId) => GetRepairState(nodeId);

    /// <summary>Returns deposited materials for the node.</summary>
    public IReadOnlyDictionary<string, int> GetDeposited(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node)
            ? new Dictionary<string, int>(node.Deposited, StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);
    }

    /// <summary>Returns repair progress for the node.</summary>
    public double GetRepairProgress(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node) ? node.RepairProgress : 0.0d;
    }

    /// <summary>Computes repair progress from the current deposited counters.</summary>
    public double RepairProgress(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node)
            ? ComputeRepairProgress(nodeId, node.Deposited)
            : 0.0d;
    }

    /// <summary>Computes repair progress from an explicit deposited map.</summary>
    public double RepairProgress(string nodeId, IReadOnlyDictionary<string, int> deposited)
    {
        return ComputeRepairProgress(nodeId, deposited);
    }

    /// <summary>Returns true when every required resource is fully deposited.</summary>
    public bool RepairCompletion(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node)
            && CheckRepairCompletion(nodeId, node.Deposited);
    }

    /// <summary>Returns true when every required resource is satisfied by an explicit deposited map.</summary>
    public bool RepairCompletion(string nodeId, IReadOnlyDictionary<string, int> deposited)
    {
        return CheckRepairCompletion(nodeId, deposited);
    }

    /// <summary>Returns the definition read for a node.</summary>
    public RepairNodeDefinition? GetRepairNodeDefinition(string nodeId)
    {
        return _definitions.TryGetValue(nodeId, out var definition) ? definition : null;
    }

    /// <summary>Returns the route enhancement payloads produced by a completed repair node.</summary>
    public IReadOnlyList<RouteEnhancementPayload> GetRouteEnhancements(string nodeId)
    {
        if (!_definitions.TryGetValue(nodeId, out var definition))
        {
            return Array.Empty<RouteEnhancementPayload>();
        }

        return definition.UnlockedRoutes
            .Select(routeId => new RouteEnhancementPayload(
                routeId,
                definition.RouteEnhancementEffect,
                definition.RouteEnhancementMagnitude,
                !definition.PreRepairRouteTraversable))
            .ToArray();
    }

    /// <summary>Applies a proportional hazard reduction while flooring at zero.</summary>
    public static double ApplyHazardReduction(double currentHazard, double reductionMagnitude)
    {
        var safeHazard = Math.Max(0.0d, currentHazard);
        var safeMagnitude = Math.Max(0.0d, reductionMagnitude);
        return Math.Max(0.0d, safeHazard - (safeHazard * safeMagnitude));
    }

    /// <summary>Returns a node snapshot for tests, UI, and persistence.</summary>
    public RepairNodeSnapshot? GetNodeSnapshot(string nodeId)
    {
        if (!_repairNodes.TryGetValue(nodeId, out var node))
        {
            return null;
        }

        return new RepairNodeSnapshot(
            nodeId,
            node.RepairState,
            new Dictionary<string, int>(node.Deposited, StringComparer.Ordinal),
            node.RepairProgress,
            node.VisualState);
    }

    /// <summary>Returns whether the repair UI can be opened for a node.</summary>
    public bool IsRepairInteractionAvailable(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node)
            && node.RepairState != RepairState.Repaired;
    }

    /// <summary>
    /// Builds UI-facing repair info. Low intel hides exact material labels but never blocks interaction.
    /// </summary>
    public RepairInteractionInfo GetRepairInteractionInfo(string nodeId, bool intelIdentified)
    {
        if (!_repairNodes.TryGetValue(nodeId, out var node) || !_definitions.TryGetValue(nodeId, out var definition))
        {
            return new RepairInteractionInfo(
                false,
                false,
                false,
                new Dictionary<string, string>(StringComparer.Ordinal),
                "unknown_effect",
                VisualStateKnown);
        }

        var materialLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (resourceId, quantity) in definition.RequiredResources)
        {
            materialLabels[resourceId] = intelIdentified ? $"{resourceId}:{quantity}" : "?";
        }

        return new RepairInteractionInfo(
            true,
            node.RepairState != RepairState.Repaired,
            intelIdentified,
            materialLabels,
            intelIdentified ? string.Join(",", definition.UnlockedRoutes) : "unknown_effect",
            node.VisualState);
    }

    /// <summary>Checks whether a repair node can accept deposits.</summary>
    public bool CanDeposit(string nodeId)
    {
        return _repairNodes.TryGetValue(nodeId, out var node)
            && node.RepairState != RepairState.Repaired;
    }

    /// <summary>
    /// Validates a proposed batch against node existence, lifecycle state, required materials, and remaining gaps.
    /// </summary>
    public DepositValidationResult ValidateDeposit(string nodeId, IReadOnlyDictionary<string, int>? offer)
    {
        var violations = new List<RepairDepositViolation>();
        if (!_repairNodes.ContainsKey(nodeId) || !_definitions.ContainsKey(nodeId))
        {
            violations.Add(RepairDepositViolation.InvalidNode);
            return new DepositValidationResult(false, violations);
        }

        if (GetRepairState(nodeId) == RepairState.Repaired)
        {
            violations.Add(RepairDepositViolation.AlreadyRepaired);
        }

        if (offer is null || !offer.Any(pair => pair.Value > 0))
        {
            violations.Add(RepairDepositViolation.EmptyOffer);
            return new DepositValidationResult(false, violations);
        }

        var required = _definitions[nodeId].RequiredResources;
        var deposited = _repairNodes[nodeId].Deposited;
        foreach (var (resourceId, quantity) in offer)
        {
            if (quantity <= 0)
            {
                continue;
            }

            if (!required.ContainsKey(resourceId))
            {
                violations.Add(RepairDepositViolation.InvalidMaterial);
                continue;
            }

            var remaining = Math.Max(0, required[resourceId] - deposited.GetValueOrDefault(resourceId, 0));
            if (quantity > remaining)
            {
                violations.Add(RepairDepositViolation.ExcessQuantity);
            }
        }

        return new DepositValidationResult(violations.Count == 0, violations.Distinct().ToArray());
    }

    /// <summary>
    /// Submits one repair batch: validate, commit to Resources, update counters, recompute progress, then complete if ready.
    /// </summary>
    public RepairDepositResult SubmitDeposit(string nodeId, IReadOnlyDictionary<string, int>? offer)
    {
        var validation = ValidateDeposit(nodeId, offer);
        if (!validation.Valid)
        {
            RepairFailed?.Invoke(nodeId, "validation_failed");
            return new RepairDepositResult(
                RepairSubmitResult.ErrValidationFailed,
                validation.Violations,
                GetDeposited(nodeId),
                GetRepairProgress(nodeId),
                false);
        }

        var positiveOffer = offer!
            .Where(pair => pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        var commitResult = _commitDeposit?.Invoke(nodeId, positiveOffer)
            ?? ResourceOperationResult.Ok(positiveOffer.Values.Sum());
        if (!commitResult.Success)
        {
            RepairFailed?.Invoke(nodeId, "commit_failed");
            return new RepairDepositResult(
                RepairSubmitResult.ErrCommitFailed,
                Array.Empty<RepairDepositViolation>(),
                GetDeposited(nodeId),
                GetRepairProgress(nodeId),
                false);
        }

        if (GetRepairState(nodeId) == RepairState.Unrevealed)
        {
            TryTransitionState(nodeId, RepairState.Known);
        }

        var node = _repairNodes[nodeId];
        foreach (var (resourceId, quantity) in positiveOffer)
        {
            node.Deposited[resourceId] = node.Deposited.GetValueOrDefault(resourceId, 0) + quantity;
            DepositCommitted?.Invoke(nodeId, resourceId, quantity);
        }

        node.RepairProgress = ComputeRepairProgress(nodeId, node.Deposited);
        var deposited = GetDeposited(nodeId);
        RepairProgressChanged?.Invoke(nodeId, node.RepairProgress, deposited);

        var completed = CheckRepairCompletion(nodeId, node.Deposited);
        if (completed)
        {
            TryTransitionState(nodeId, RepairState.Repaired);
            RepairCompleted?.Invoke(nodeId);
            VisualStateChanged?.Invoke(nodeId, VisualStateRepaired);
        }

        return new RepairDepositResult(
            RepairSubmitResult.Success,
            Array.Empty<RepairDepositViolation>(),
            GetDeposited(nodeId),
            GetRepairProgress(nodeId),
            completed);
    }

    /// <summary>
    /// Compatibility single-material commit used by the existing foundation parity runner.
    /// </summary>
    public bool CommitDeposit(string nodeId, string resourceId, int quantity)
    {
        if (!CanDeposit(nodeId) || quantity <= 0)
        {
            RepairFailed?.Invoke(nodeId, "cannot_deposit");
            return false;
        }

        var node = _repairNodes[nodeId];
        node.Deposited[resourceId] = node.Deposited.GetValueOrDefault(resourceId, 0) + quantity;
        DepositCommitted?.Invoke(nodeId, resourceId, quantity);

        node.RepairProgress = ComputeRepairProgress(nodeId, node.Deposited);
        if (CheckRepairCompletion(nodeId, node.Deposited))
        {
            if (GetRepairState(nodeId) == RepairState.Unrevealed)
            {
                TryTransitionState(nodeId, RepairState.Known);
            }

            TryTransitionState(nodeId, RepairState.Repaired);
            RepairCompleted?.Invoke(nodeId);
            VisualStateChanged?.Invoke(nodeId, VisualStateRepaired);
        }

        return true;
    }

    /// <summary>Returns the list of completed repair node IDs.</summary>
    public IReadOnlyList<string> GetCompletedNodes()
    {
        return _completedNodeIds.ToArray();
    }

    /// <summary>Returns all runtime node IDs.</summary>
    public IReadOnlyList<string> GetRepairNodeIds()
    {
        return _repairNodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private bool CheckRepairCompletion(string nodeId, IReadOnlyDictionary<string, int> deposited)
    {
        if (!_definitions.TryGetValue(nodeId, out var definition) || definition.RequiredResources.Count == 0)
        {
            return false;
        }

        foreach (var (resourceId, required) in definition.RequiredResources)
        {
            if (required <= 0)
            {
                continue;
            }

            if (deposited.GetValueOrDefault(resourceId, 0) < required)
            {
                return false;
            }
        }

        return true;
    }

    private double ComputeRepairProgress(string nodeId, IReadOnlyDictionary<string, int> deposited)
    {
        if (!_definitions.TryGetValue(nodeId, out var definition) || definition.RequiredResources.Count == 0)
        {
            return 0.0d;
        }

        var total = 0.0d;
        var count = 0;
        foreach (var (resourceId, required) in definition.RequiredResources)
        {
            count++;
            if (required <= 0)
            {
                total += 1.0d;
                continue;
            }

            total += Math.Min((double)deposited.GetValueOrDefault(resourceId, 0) / required, 1.0d);
        }

        return count == 0 ? 0.0d : Math.Clamp(total / count, 0.0d, 1.0d);
    }

    private IEnumerable<RepairNodeDefinition> LoadDefinitionsFromRegistry()
    {
        if (_registry is null)
        {
            yield break;
        }

        foreach (var entity in _registry.ListByKind("repair-node"))
        {
            var definition = DefinitionFromEntity(entity);
            if (definition is null)
            {
                _errors.Add("WorldRepair: malformed repair-node entry skipped");
                continue;
            }

            yield return definition;
        }
    }

    private static RepairNodeDefinition? DefinitionFromEntity(IReadOnlyDictionary<string, object?> entity)
    {
        var nodeId = ReadString(entity, "node_id");
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            nodeId = ReadString(entity, "id");
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        var required = ReadIntMap(entity, "required_resources");
        if (required.Count == 0)
        {
            required = ReadIntMap(entity, "required_materials");
        }

        return new RepairNodeDefinition(
            nodeId,
            ReadString(entity, "display_name", ReadString(entity, "name", nodeId)),
            ReadString(entity, "linked_location_id", ReadString(entity, "location_id")),
            required,
            ReadStringList(entity, "unlocked_routes", ReadNestedStringList(entity, "unlocks", "routes")),
            ReadNestedString(entity, "route_enhancement", "effect", ReadString(entity, "route_enhancement_effect")),
            ReadNestedDouble(entity, "route_enhancement", "magnitude", ReadDouble(entity, "route_enhancement_magnitude")),
            ReadNestedBool(entity, "pre_repair_route_state", "traversable", true),
            ReadString(entity, "visual_state_anchor"));
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> entity, string key, string fallback = "")
    {
        return entity.TryGetValue(key, out var value) ? value?.ToString() ?? fallback : fallback;
    }

    private static double ReadDouble(IReadOnlyDictionary<string, object?> entity, string key, double fallback = 0.0d)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return Convert.ToDouble(value);
    }

    private static IReadOnlyDictionary<string, int> ReadIntMap(IReadOnlyDictionary<string, object?> entity, string key)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        if (value is IReadOnlyDictionary<string, int> typed)
        {
            return new Dictionary<string, int>(typed, StringComparer.Ordinal);
        }

        if (value is IReadOnlyDictionary<string, object?> objectMap)
        {
            return objectMap.ToDictionary(
                pair => pair.Key,
                pair => Convert.ToInt32(pair.Value),
                StringComparer.Ordinal);
        }

        if (value is IDictionary<string, object?> mutableMap)
        {
            return mutableMap.ToDictionary(
                pair => pair.Key,
                pair => Convert.ToInt32(pair.Value),
                StringComparer.Ordinal);
        }

        return new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> ReadStringList(
        IReadOnlyDictionary<string, object?> entity,
        string key,
        IReadOnlyList<string>? fallback = null)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return fallback ?? Array.Empty<string>();
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

        return fallback ?? Array.Empty<string>();
    }

    private static IReadOnlyList<string> ReadNestedStringList(
        IReadOnlyDictionary<string, object?> entity,
        string mapKey,
        string nestedKey)
    {
        if (!entity.TryGetValue(mapKey, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is IReadOnlyDictionary<string, string[]> typed && typed.TryGetValue(nestedKey, out var typedList))
        {
            return typedList;
        }

        if (value is IReadOnlyDictionary<string, object?> objectMap && objectMap.TryGetValue(nestedKey, out var nested))
        {
            return nested is System.Collections.IEnumerable list
                ? list.Cast<object?>().Select(item => item?.ToString() ?? "").Where(item => item.Length > 0).ToArray()
                : Array.Empty<string>();
        }

        return Array.Empty<string>();
    }

    private static string ReadNestedString(
        IReadOnlyDictionary<string, object?> entity,
        string mapKey,
        string nestedKey,
        string fallback = "")
    {
        if (!entity.TryGetValue(mapKey, out var value) || value is null)
        {
            return fallback;
        }

        if (value is IReadOnlyDictionary<string, object?> objectMap && objectMap.TryGetValue(nestedKey, out var nested))
        {
            return nested?.ToString() ?? fallback;
        }

        return fallback;
    }

    private static double ReadNestedDouble(
        IReadOnlyDictionary<string, object?> entity,
        string mapKey,
        string nestedKey,
        double fallback = 0.0d)
    {
        if (!entity.TryGetValue(mapKey, out var value) || value is null)
        {
            return fallback;
        }

        if (value is IReadOnlyDictionary<string, object?> objectMap && objectMap.TryGetValue(nestedKey, out var nested))
        {
            return Convert.ToDouble(nested);
        }

        return fallback;
    }

    private static bool ReadNestedBool(
        IReadOnlyDictionary<string, object?> entity,
        string mapKey,
        string nestedKey,
        bool fallback)
    {
        if (!entity.TryGetValue(mapKey, out var value) || value is null)
        {
            return fallback;
        }

        if (value is IReadOnlyDictionary<string, object?> objectMap && objectMap.TryGetValue(nestedKey, out var nested))
        {
            return nested is bool boolValue ? boolValue : fallback;
        }

        return fallback;
    }

    private sealed class RepairNodeState
    {
        public RepairState RepairState { get; set; } = RepairState.Unrevealed;
        public Dictionary<string, int> Deposited { get; } = new(StringComparer.Ordinal);
        public double RepairProgress { get; set; }
        public string VisualState { get; set; } = VisualStateKnown;
    }
}
