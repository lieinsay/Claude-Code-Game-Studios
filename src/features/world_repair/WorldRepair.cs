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
/// MVP visual contract consumed by feedback/UI tests until scene assets exist.
/// </summary>
public sealed record RepairVisualSnapshot(
    string NodeId,
    string SpriteState,
    bool HaloVisible,
    bool BeamVisible,
    string BeamColorRgba,
    int ParticleCount,
    double ParticleSpawnRadiusPx,
    double ParticleMinLifetimeSec,
    double ParticleMaxLifetimeSec,
    double ModulateAlpha,
    bool CeremonyActive,
    double CeremonyElapsedSec,
    double CeremonyDurationSec,
    bool UiCloseInteractable);

/// <summary>
/// MVP audio cue contract. Concrete resources are supplied by audio/feedback later.
/// </summary>
public sealed record RepairAudioCue(
    string NodeId,
    string CueId,
    double DurationSec,
    string Description);

/// <summary>
/// Sole Feature-layer owner of world repair node lifecycle state.
/// </summary>
public sealed class WorldRepair
{
    public const string MvpNodeId = "repair_node.starlight_dock";
    public const string VisualStateKnown = "known";
    public const string VisualStateRepaired = "repaired";
    public const double DefaultRepairCeremonyDurationSec = 5.0d;
    public const double MinRepairCeremonyDurationSec = 0.5d;
    public const double BreathingPeriodSec = 3.0d;
    public const string RepairedBeamColorRgba = "1.0,0.9,0.6,0.3";
    public const string CommitFailedPlayerMessage = "提交失败，材料未消耗";

    private readonly Registry? _registry;
    private readonly Dictionary<string, RepairNodeDefinition> _definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RepairNodeState> _repairNodes = new(StringComparer.Ordinal);
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private readonly List<string> _downstreamErrors = [];
    private readonly List<string> _completedNodeIds = [];
    private readonly List<RepairAudioCue> _audioCues = [];
    private readonly Dictionary<string, RepairCeremonyRuntime> _ceremonies = new(StringComparer.Ordinal);
    private Func<string, IReadOnlyDictionary<string, int>, ResourceOperationResult>? _commitDeposit;
    private Func<string, IReadOnlyDictionary<string, int>>? _pool6DepositQuery;
    private double _repairCeremonyDurationSec = DefaultRepairCeremonyDurationSec;

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

    /// <summary>Consumer callback errors captured while continuing fan-out.</summary>
    public IReadOnlyList<string> DownstreamErrors => _downstreamErrors;

    /// <summary>Audio cue log for MVP contract tests and adapters.</summary>
    public IReadOnlyList<RepairAudioCue> AudioCues => _audioCues;

    /// <summary>Current clamped repair ceremony duration.</summary>
    public double RepairCeremonyDurationSec => _repairCeremonyDurationSec;

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

    /// <summary>Injects a Pool 6 deposited-record query used during crash recovery.</summary>
    public void SetPool6DepositQuery(Func<string, IReadOnlyDictionary<string, int>> query)
    {
        _pool6DepositQuery = query;
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
        _audioCues.Clear();
        _ceremonies.Clear();

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
        if (_repairNodes.TryGetValue(nodeId, out var node))
        {
            node.RepairState = RepairState.Known;
        }
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
            EmitVisualStateChanged(nodeId, VisualStateKnown);
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

    /// <summary>Returns whether #11 should expose the repair interaction at the current location.</summary>
    public bool IsRepairInteractionAvailableAtLocation(string nodeId, string currentLocationId)
    {
        return IsRepairInteractionAvailable(nodeId)
            && _definitions.TryGetValue(nodeId, out var definition)
            && !string.IsNullOrWhiteSpace(currentLocationId)
            && string.Equals(definition.LinkedLocationId, currentLocationId, StringComparison.Ordinal);
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

    /// <summary>Returns the UI quantity selector max for a resource at a repair node.</summary>
    public int GetMaxOfferQuantity(string nodeId, string resourceId)
    {
        if (!_definitions.TryGetValue(nodeId, out var definition)
            || !_repairNodes.TryGetValue(nodeId, out var node)
            || !definition.RequiredResources.TryGetValue(resourceId, out var required))
        {
            return 0;
        }

        return Math.Max(0, required - node.Deposited.GetValueOrDefault(resourceId, 0));
    }

    /// <summary>
    /// Validates a proposed batch against node existence, lifecycle state, required materials, and remaining gaps.
    /// </summary>
    public DepositValidationResult ValidateDeposit(string nodeId, IReadOnlyDictionary<string, int>? offer)
    {
        var violations = new List<RepairDepositViolation>();
        if (string.IsNullOrWhiteSpace(nodeId) || !_repairNodes.ContainsKey(nodeId) || !_definitions.ContainsKey(nodeId))
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

    /// <summary>Defensive validation overload for untyped script/UI payloads.</summary>
    public DepositValidationResult ValidateDeposit(string nodeId, IReadOnlyDictionary<string, object?>? offer)
    {
        return ValidateDeposit(nodeId, NormalizeOffer(offer));
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

        PlayDepositConfirmAudio(nodeId);

        node.RepairProgress = ComputeRepairProgress(nodeId, node.Deposited);
        var deposited = GetDeposited(nodeId);
        EmitRepairProgressChanged(nodeId, node.RepairProgress, deposited);

        var completed = CheckRepairCompletion(nodeId, node.Deposited);
        if (completed)
        {
            TryTransitionState(nodeId, RepairState.Repaired);
            TriggerRepairCeremony(nodeId);
            EmitRepairCompleted(nodeId);
            EmitVisualStateChanged(nodeId, VisualStateRepaired);
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
            TriggerRepairCeremony(nodeId);
            EmitRepairCompleted(nodeId);
            EmitVisualStateChanged(nodeId, VisualStateRepaired);
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

    /// <summary>Returns the player-facing message for a submit result.</summary>
    public static string GetSubmitFailureMessage(RepairSubmitResult result)
    {
        return result == RepairSubmitResult.ErrCommitFailed ? CommitFailedPlayerMessage : string.Empty;
    }

    /// <summary>Clamps the configurable repair ceremony duration.</summary>
    public void SetRepairCeremonyDurationSec(double durationSec)
    {
        _repairCeremonyDurationSec = durationSec <= 0.0d
            ? MinRepairCeremonyDurationSec
            : Math.Max(MinRepairCeremonyDurationSec, durationSec);
    }

    /// <summary>Advances active repair ceremonies by gameplay delta, never wall-clock time.</summary>
    public void TickCeremony(double deltaSec)
    {
        if (deltaSec <= 0.0d)
        {
            return;
        }

        foreach (var ceremony in _ceremonies.Values)
        {
            if (!ceremony.Active)
            {
                continue;
            }

            ceremony.ElapsedSec = Math.Min(ceremony.ElapsedSec + deltaSec, ceremony.DurationSec);
            if (ceremony.ElapsedSec >= ceremony.DurationSec)
            {
                ceremony.Active = false;
            }
        }
    }

    /// <summary>Returns the MVP visual state contract for one repair node.</summary>
    public RepairVisualSnapshot GetVisualSnapshot(string nodeId)
    {
        if (!_repairNodes.TryGetValue(nodeId, out var node))
        {
            return new RepairVisualSnapshot(
                nodeId,
                VisualStateKnown,
                false,
                false,
                RepairedBeamColorRgba,
                0,
                0.0d,
                0.0d,
                0.0d,
                1.0d,
                false,
                0.0d,
                _repairCeremonyDurationSec,
                true);
        }

        _ceremonies.TryGetValue(nodeId, out var ceremony);
        var repaired = node.RepairState == RepairState.Repaired;
        var elapsed = ceremony?.ElapsedSec ?? 0.0d;
        return new RepairVisualSnapshot(
            nodeId,
            repaired ? VisualStateRepaired : VisualStateKnown,
            repaired,
            repaired,
            RepairedBeamColorRgba,
            repaired ? 7 : 0,
            repaired ? 48.0d : 0.0d,
            repaired ? 2.0d : 0.0d,
            repaired ? 4.0d : 0.0d,
            repaired ? ComputeBreathingAlpha(elapsed) : 1.0d,
            ceremony?.Active ?? false,
            elapsed,
            ceremony?.DurationSec ?? _repairCeremonyDurationSec,
            true);
    }

    /// <summary>Builds the progress.world-repair payload. Derived progress is intentionally omitted.</summary>
    public Dictionary<string, object?> SerializeWorldRepair()
    {
        var nodes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (nodeId, node) in _repairNodes)
        {
            nodes[nodeId] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["repair_state"] = (int)node.RepairState,
                ["deposited"] = node.Deposited.ToDictionary(
                    pair => pair.Key,
                    pair => (object?)pair.Value,
                    StringComparer.Ordinal),
            };
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["domain_id"] = "progress.world-repair",
            ["nodes"] = nodes,
        };
    }

    /// <summary>Builds the ADR-0003 SnapshotPackage for world repair progress.</summary>
    public SnapshotPackage BuildSnapshotPackage()
    {
        var package = new SnapshotPackage
        {
            DomainId = "progress.world-repair",
            SnapshotSchemaVersion = 1,
            DomainState = SnapshotDomainState.Ready,
        };
        package.ContentDomainVersions["world-repair"] = "2026-05-13";
        package.StableIdRefs.AddRange(_repairNodes.Keys.OrderBy(id => id, StringComparer.Ordinal));
        foreach (var (key, value) in SerializeWorldRepair())
        {
            package.Payload[key] = value;
        }

        return package;
    }

    /// <summary>Registers world repair with the persistence pipeline.</summary>
    public void RegisterPersistence(Persistence persistence)
    {
        persistence.RegisterDomainSerializer("progress.world-repair", BuildSnapshotPackage);
        persistence.RegisterDomainDeserializer("progress.world-repair", RestoreFromSnapshotPackage);
    }

    /// <summary>Restores world repair from an ADR-0003 SnapshotPackage.</summary>
    public void RestoreFromSnapshotPackage(SnapshotPackage package)
    {
        if (package.DomainId != "progress.world-repair")
        {
            return;
        }

        DeserializeWorldRepair(package.Payload);
    }

    /// <summary>Restores world repair state and recomputes all derived progress values.</summary>
    public void DeserializeWorldRepair(IReadOnlyDictionary<string, object?> snapshot)
    {
        _repairNodes.Clear();
        _completedNodeIds.Clear();

        foreach (var (nodeId, rawNode) in ReadObjectMap(snapshot, "nodes"))
        {
            var nodeData = ToObjectMap(rawNode);
            var state = (RepairState)ReadInt(nodeData, "repair_state", (int)RepairState.Unrevealed);
            var deposited = ReadIntMap(nodeData, "deposited");
            _repairNodes[nodeId] = new RepairNodeState
            {
                RepairState = state,
                RepairProgress = ComputeRepairProgress(nodeId, deposited),
                VisualState = state == RepairState.Repaired ? VisualStateRepaired : VisualStateKnown,
            };

            foreach (var (resourceId, quantity) in deposited)
            {
                _repairNodes[nodeId].Deposited[resourceId] = quantity;
            }

            if (state == RepairState.Repaired)
            {
                _completedNodeIds.Add(nodeId);
            }
        }

        CrossValidateWithPool6();
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

    private void PlayDepositConfirmAudio(string nodeId)
    {
        _audioCues.Add(new RepairAudioCue(
            nodeId,
            "repair.deposit_confirm",
            0.35d,
            "short metal-and-stone confirmation"));
    }

    private void TriggerRepairCeremony(string nodeId)
    {
        _ceremonies[nodeId] = new RepairCeremonyRuntime(_repairCeremonyDurationSec);
        _audioCues.Add(new RepairAudioCue(
            nodeId,
            "repair.ceremony_hum_chime",
            2.5d,
            "rising hum into clear chime"));
    }

    private static double ComputeBreathingAlpha(double elapsedSec)
    {
        var breath = Math.Sin(elapsedSec * Math.Tau / BreathingPeriodSec);
        return Math.Clamp(0.95d + (breath * 0.05d), 0.9d, 1.0d);
    }

    private void CrossValidateWithPool6()
    {
        if (_pool6DepositQuery is null)
        {
            return;
        }

        foreach (var (nodeId, node) in _repairNodes)
        {
            var pool6Deposits = _pool6DepositQuery(nodeId);
            foreach (var (resourceId, poolQty) in pool6Deposits)
            {
                var localQty = node.Deposited.GetValueOrDefault(resourceId, 0);
                if (poolQty > localQty)
                {
                    _warnings.Add($"WorldRepair: deposited mismatch for {nodeId}/{resourceId}; pool6={poolQty}, local={localQty}");
                    node.Deposited[resourceId] = poolQty;
                }
            }

            node.RepairProgress = ComputeRepairProgress(nodeId, node.Deposited);
            if (node.RepairState != RepairState.Repaired && CheckRepairCompletion(nodeId, node.Deposited))
            {
                node.RepairState = RepairState.Repaired;
                node.VisualState = VisualStateRepaired;
                if (!_completedNodeIds.Contains(nodeId, StringComparer.Ordinal))
                {
                    _completedNodeIds.Add(nodeId);
                }
            }
        }
    }

    private void EmitRepairProgressChanged(
        string nodeId,
        double progress,
        IReadOnlyDictionary<string, int> deposited)
    {
        if (RepairProgressChanged is null)
        {
            return;
        }

        foreach (Action<string, double, IReadOnlyDictionary<string, int>> handler in RepairProgressChanged.GetInvocationList())
        {
            TryInvoke(() => handler(nodeId, progress, deposited), "repair_progress_changed");
        }
    }

    private void EmitRepairCompleted(string nodeId)
    {
        if (RepairCompleted is null)
        {
            return;
        }

        foreach (Action<string> handler in RepairCompleted.GetInvocationList())
        {
            TryInvoke(() => handler(nodeId), "repair_completed");
        }
    }

    private void EmitVisualStateChanged(string nodeId, string visualState)
    {
        if (VisualStateChanged is null)
        {
            return;
        }

        foreach (Action<string, string> handler in VisualStateChanged.GetInvocationList())
        {
            TryInvoke(() => handler(nodeId, visualState), "visual_state_changed");
        }
    }

    private void TryInvoke(Action action, string signalName)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _downstreamErrors.Add($"{signalName}:{ex.GetType().Name}:{ex.Message}");
        }
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
            return NormalizeOffer(objectMap);
        }

        if (value is IDictionary<string, object?> mutableMap)
        {
            return NormalizeOffer(new Dictionary<string, object?>(mutableMap, StringComparer.Ordinal));
        }

        return new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, int> NormalizeOffer(IReadOnlyDictionary<string, object?>? offer)
    {
        var normalized = new Dictionary<string, int>(StringComparer.Ordinal);
        if (offer is null)
        {
            return normalized;
        }

        foreach (var (resourceId, rawQuantity) in offer)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                continue;
            }

            if (TryReadQuantity(rawQuantity, out var quantity))
            {
                normalized[resourceId] = quantity;
            }
            else
            {
                normalized[resourceId] = 0;
            }
        }

        return normalized;
    }

    private static bool TryReadQuantity(object? value, out int quantity)
    {
        quantity = 0;
        switch (value)
        {
            case int intValue:
                quantity = intValue;
                return true;
            case long longValue:
                quantity = checked((int)longValue);
                return true;
            case double doubleValue:
                quantity = (int)Math.Floor(doubleValue);
                return true;
            case float floatValue:
                quantity = (int)Math.Floor(floatValue);
                return true;
            case decimal decimalValue:
                quantity = (int)Math.Floor(decimalValue);
                return true;
            case string stringValue when double.TryParse(stringValue, out var parsed):
                quantity = (int)Math.Floor(parsed);
                return true;
            default:
                return false;
        }
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

    private static IEnumerable<KeyValuePair<string, object?>> ReadObjectMap(
        IReadOnlyDictionary<string, object?> data,
        string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        return value is IReadOnlyDictionary<string, object?> objectMap
            ? objectMap
            : [];
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

    private static int ReadInt(IReadOnlyDictionary<string, object?> data, string key, int fallback = 0)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            double doubleValue => checked((int)doubleValue),
            float floatValue => checked((int)floatValue),
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            _ => fallback,
        };
    }

    private sealed class RepairNodeState
    {
        public RepairState RepairState { get; set; } = RepairState.Unrevealed;
        public Dictionary<string, int> Deposited { get; } = new(StringComparer.Ordinal);
        public double RepairProgress { get; set; }
        public string VisualState { get; set; } = VisualStateKnown;
    }

    private sealed class RepairCeremonyRuntime
    {
        public RepairCeremonyRuntime(double durationSec)
        {
            DurationSec = durationSec;
        }

        public bool Active { get; set; } = true;
        public double ElapsedSec { get; set; }
        public double DurationSec { get; }
    }
}
