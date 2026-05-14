using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Feature;

/// <summary>
/// Runtime settlement activity state owned by SettlementManager.
/// </summary>
public enum SettlementState
{
    Dormant = 0,
    Recovering = 1,
    Active = 2,
}

/// <summary>
/// Runtime market stall lifecycle state.
/// </summary>
public enum StallState
{
    Closed = 0,
    OpenBasic = 1,
    OpenExpanded = 2,
}

/// <summary>
/// Runtime NPC market presence state.
/// </summary>
public enum NpcState
{
    Absent = 0,
    Idle = 1,
    Active = 2,
}

/// <summary>
/// Result returned when SettlementManager validates a market purchase.
/// </summary>
public sealed record PurchaseValidation(bool Valid, string Reason, int TotalCost);

/// <summary>
/// Result returned after attempting a market purchase.
/// </summary>
public sealed record PurchaseExecutionResult(bool Success, string GoodId, int Quantity, int TotalCost, string Reason);

/// <summary>
/// Immutable runtime snapshot for one settlement.
/// </summary>
public sealed record SettlementRuntimeSnapshot(
    string SettlementId,
    SettlementState SettlementState,
    IReadOnlyList<string> CompletedNodeIds);

/// <summary>
/// Immutable runtime snapshot for one stall.
/// </summary>
public sealed record StallRuntimeSnapshot(string StallId, StallState StallState, string SettlementId);

/// <summary>
/// Immutable runtime snapshot for one market NPC.
/// </summary>
public sealed record NpcRuntimeSnapshot(string NpcId, NpcState NpcState, string StallId);

/// <summary>
/// Feature-layer owner for port village market state, stall unlocks, purchase validation, and settlement snapshots.
/// </summary>
public sealed class SettlementManager
{
    public const string ProgressDomainId = "progress.settlement-market";
    public const string MvpSettlementId = "settlement.glass-harbor";
    public const string DefaultStallId = "stall.gh-general";
    public const string PurchaseFailCapacity = "capacity_full";
    public const string PurchaseFailFunds = "insufficient_funds";

    private readonly Registry _registry;
    private readonly Dictionary<string, SettlementRuntimeState> _settlements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StallRuntimeState> _stalls = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NpcRuntimeState> _npcs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _connectedSources = new(StringComparer.Ordinal);
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private ResourcesManager? _resources;
    private Func<bool>? _captureSnapshot;
    private Func<IReadOnlyDictionary<string, object?>?>? _restoreSnapshot;
    private Action<string, string, WorldVector2>? _registerFocusTarget;

    public SettlementManager(Registry registry)
    {
        _registry = registry;
    }

    /// <summary>Raised after a stall is opened or requested by interaction.</summary>
    public event Action<string, string>? StallOpened;

    /// <summary>Raised after a stall state mutates.</summary>
    public event Action<string, int, int>? StallStateChanged;

    /// <summary>Raised after an NPC state mutates.</summary>
    public event Action<string, int, int>? NpcStateChanged;

    /// <summary>Raised after a purchase succeeds.</summary>
    public event Action<string, int, int>? PurchaseCompleted;

    /// <summary>Raised after a purchase fails.</summary>
    public event Action<string, string>? PurchaseFailed;

    /// <summary>Raised after settlement activity changes.</summary>
    public event Action<string, int>? SettlementActivityChanged;

    /// <summary>Non-fatal warnings collected for tests and debug surfaces.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>Fatal configuration errors collected for tests and debug surfaces.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>Number of successful snapshot trigger attempts.</summary>
    public int SnapshotTriggerCount { get; private set; }

    /// <summary>Whether feature-ready initialization has completed.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Injects the ResourcesManager purchase boundary.</summary>
    public void SetResourcesManager(ResourcesManager resources)
    {
        _resources = resources;
    }

    /// <summary>Injects a focus-target registration callback owned by InteractionRegistry or scene adapters.</summary>
    public void SetFocusTargetRegistration(Action<string, string, WorldVector2> registerFocusTarget)
    {
        _registerFocusTarget = registerFocusTarget;
    }

    /// <summary>Injects persistence callbacks for focused tests or app boot wiring.</summary>
    public void SetPersistenceDelegates(
        Func<IReadOnlyDictionary<string, object?>?> restoreSnapshot,
        Func<bool> captureSnapshot)
    {
        _restoreSnapshot = restoreSnapshot;
        _captureSnapshot = captureSnapshot;
    }

    /// <summary>Registers this domain with the persistence pipeline.</summary>
    public void RegisterPersistence(Persistence persistence)
    {
        persistence.RegisterDomainSerializer(ProgressDomainId, BuildSnapshotPackage);
        persistence.RegisterDomainDeserializer(ProgressDomainId, RestoreFromSnapshotPackage);
    }

    /// <summary>Connects upstream source events during feature-ready boot.</summary>
    public void ConnectSignals(WorldRepair? worldRepair = null, InteractionRegistry? interactionRegistry = null)
    {
        if (worldRepair is not null && _connectedSources.Add("world-repair"))
        {
            worldRepair.RepairCompleted += OnRepairCompleted;
        }

        if (interactionRegistry is not null && _connectedSources.Add("interaction"))
        {
            interactionRegistry.InteractionUseRequested += (targetId, _) => OnUseRequested(targetId);
        }
    }

    /// <summary>Runs feature-ready initialization: connect, restore or initialize, then register open stalls.</summary>
    public void OnFeatureReady(WorldRepair? worldRepair = null, InteractionRegistry? interactionRegistry = null)
    {
        ConnectSignals(worldRepair, interactionRegistry);
        var snapshot = _restoreSnapshot?.Invoke();
        if (snapshot is null || snapshot.Count == 0)
        {
            InitNewGameState();
            TriggerSnapshot();
        }
        else
        {
            DeserializeSettlement(snapshot);
        }

        RegisterOpenStalls();
        IsInitialized = true;
    }

    /// <summary>Initializes new-game settlement, stall, and NPC runtime state from Registry definitions.</summary>
    public void InitNewGameState()
    {
        _settlements.Clear();
        _stalls.Clear();
        _npcs.Clear();

        foreach (var entity in _registry.ListByKind("settlement"))
        {
            var id = ReadString(entity, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                _errors.Add("Settlement: settlement entry missing id");
                continue;
            }

            _settlements[id] = new SettlementRuntimeState(SettlementState.Dormant);
        }

        foreach (var entity in _registry.ListByKind("stall"))
        {
            var id = ReadString(entity, "id");
            var settlementId = ReadString(entity, "settlement_id");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(settlementId))
            {
                _errors.Add("Settlement: malformed stall entry skipped");
                continue;
            }

            _stalls[id] = new StallRuntimeState(
                ReadBool(entity, "is_default_open") ? StallState.OpenBasic : StallState.Closed,
                settlementId);
            if (ReadStringList(entity, "required_node_ids").Count == 0 && !ReadBool(entity, "is_default_open"))
            {
                _warnings.Add($"Settlement: stall '{id}' has empty required_node_ids");
            }
        }

        foreach (var entity in _registry.ListByKind("npc"))
        {
            var id = ReadString(entity, "id");
            var stallId = ReadString(entity, "stall_id");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(stallId))
            {
                _errors.Add("Settlement: malformed npc entry skipped");
                continue;
            }

            _npcs[id] = new NpcRuntimeState(
                ReadBool(entity, "is_default_present") ? NpcState.Idle : NpcState.Absent,
                stallId);
            if (!_stalls.ContainsKey(stallId))
            {
                _warnings.Add($"Settlement: NPC '{id}' references missing stall '{stallId}'");
            }
        }

        if (!_settlements.ContainsKey(MvpSettlementId))
        {
            _errors.Add($"Settlement: MVP settlement '{MvpSettlementId}' not found in Registry");
        }

        if (!_stalls.ContainsKey(DefaultStallId))
        {
            _errors.Add($"Settlement: MVP default stall '{DefaultStallId}' not found in Registry");
        }
    }

    /// <summary>Returns a settlement state, defaulting to Dormant for unknown IDs.</summary>
    public SettlementState GetSettlementState(string settlementId)
    {
        return _settlements.TryGetValue(settlementId, out var state) ? state.SettlementState : SettlementState.Dormant;
    }

    /// <summary>Returns a stall state, defaulting to Closed for unknown IDs.</summary>
    public StallState GetStallState(string stallId)
    {
        return _stalls.TryGetValue(stallId, out var state) ? state.StallState : StallState.Closed;
    }

    /// <summary>Returns an NPC state, defaulting to Absent for unknown IDs.</summary>
    public NpcState GetNpcState(string npcId)
    {
        return _npcs.TryGetValue(npcId, out var state) ? state.NpcState : NpcState.Absent;
    }

    /// <summary>Returns completed repair node IDs for a settlement.</summary>
    public IReadOnlyList<string> GetCompletedNodeIds(string settlementId)
    {
        return _settlements.TryGetValue(settlementId, out var state)
            ? state.CompletedNodeIds.ToArray()
            : Array.Empty<string>();
    }

    /// <summary>Returns a stable list of all stall IDs loaded for a settlement.</summary>
    public IReadOnlyList<string> GetSettlementStalls(string settlementId)
    {
        return _stalls
            .Where(pair => pair.Value.SettlementId == settlementId)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Key)
            .ToArray();
    }

    /// <summary>Attempts a stall state transition through the forward-only MVP lifecycle.</summary>
    public bool TransitionStallState(string stallId, StallState targetState)
    {
        if (!_stalls.TryGetValue(stallId, out var state))
        {
            _warnings.Add($"Settlement: unknown stall '{stallId}'");
            return false;
        }

        var current = state.StallState;
        if (targetState == StallState.OpenBasic && current == StallState.Closed)
        {
            state.StallState = StallState.OpenBasic;
            StallStateChanged?.Invoke(stallId, (int)current, (int)targetState);
            UnlockNpcForStall(stallId);
            return true;
        }

        if (targetState == StallState.OpenBasic && current >= StallState.OpenBasic)
        {
            return false;
        }

        _warnings.Add($"Settlement: invalid stall transition {current}->{targetState} for '{stallId}'");
        return false;
    }

    /// <summary>Attempts an NPC state transition through the forward-only MVP lifecycle.</summary>
    public bool TransitionNpcState(string npcId, NpcState targetState)
    {
        if (!_npcs.TryGetValue(npcId, out var state))
        {
            _warnings.Add($"Settlement: unknown NPC '{npcId}'");
            return false;
        }

        var current = state.NpcState;
        if (targetState == NpcState.Idle && current == NpcState.Absent)
        {
            state.NpcState = NpcState.Idle;
            NpcStateChanged?.Invoke(npcId, (int)current, (int)targetState);
            return true;
        }

        if (targetState == NpcState.Idle && current >= NpcState.Idle)
        {
            return false;
        }

        _warnings.Add($"Settlement: invalid NPC transition {current}->{targetState} for '{npcId}'");
        return false;
    }

    /// <summary>Attempts a settlement state transition through the forward-only lifecycle.</summary>
    public bool TransitionSettlementState(string settlementId, SettlementState targetState)
    {
        if (!_settlements.TryGetValue(settlementId, out var state))
        {
            _warnings.Add($"Settlement: unknown settlement '{settlementId}'");
            return false;
        }

        var current = state.SettlementState;
        if (targetState > current)
        {
            state.SettlementState = targetState;
            SettlementActivityChanged?.Invoke(settlementId, GetActiveStallCount(settlementId));
            return true;
        }

        if (targetState == current)
        {
            return false;
        }

        _warnings.Add($"Settlement: invalid settlement transition {current}->{targetState} for '{settlementId}'");
        return false;
    }

    /// <summary>Calculates F.1 total cost from Registry good.price.</summary>
    public int CalculateTotalCost(string goodId, int quantity)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        var good = QueryEntity(goodId);
        var price = good.Count == 0 ? 0 : ReadInt(good, "price", 0);
        if (price == 0)
        {
            _errors.Add($"Settlement: good '{goodId}' has price=0");
        }

        return checked(price * quantity);
    }

    /// <summary>Returns goods available at the stall's current unlock level.</summary>
    public IReadOnlyList<string> GetStallGoods(string stallId)
    {
        var stallState = GetStallState(stallId);
        if (stallState < StallState.OpenBasic)
        {
            return Array.Empty<string>();
        }

        return _registry.ListByKind("good")
            .Where(good => ReadStringList(good, "available_stall_ids").Contains(stallId, StringComparer.Ordinal))
            .Where(good => stallState >= (StallState)ReadInt(good, "required_stall_state", (int)StallState.OpenBasic))
            .OrderBy(good => ReadInt(good, "sort_order", int.MaxValue))
            .Select(good => ReadString(good, "id"))
            .Where(id => id.Length > 0)
            .ToArray();
    }

    /// <summary>Validates a purchase by checking stall state, availability, quantity, and ResourcesManager preflight.</summary>
    public PurchaseValidation ValidatePurchaseRequest(string stallId, string goodId, int quantity)
    {
        if (quantity <= 0)
        {
            return new PurchaseValidation(false, "invalid_quantity", 0);
        }

        if (GetStallState(stallId) < StallState.OpenBasic)
        {
            return new PurchaseValidation(false, "stall_closed", 0);
        }

        if (!GetStallGoods(stallId).Contains(goodId, StringComparer.Ordinal))
        {
            return new PurchaseValidation(false, "good_unavailable", 0);
        }

        var totalCost = CalculateTotalCost(goodId, quantity);
        if (_resources is null)
        {
            return new PurchaseValidation(false, "system_unavailable", totalCost);
        }

        var resourceValidation = _resources.ValidatePurchase(goodId, quantity);
        return resourceValidation.Valid
            ? new PurchaseValidation(true, string.Empty, totalCost)
            : new PurchaseValidation(false, resourceValidation.Reason, totalCost);
    }

    /// <summary>Executes a purchase after defensive re-validation and emits post-mutation signals.</summary>
    public PurchaseExecutionResult ExecutePurchase(string stallId, string goodId, int quantity)
    {
        var validation = ValidatePurchaseRequest(stallId, goodId, quantity);
        if (!validation.Valid)
        {
            PurchaseFailed?.Invoke(goodId, validation.Reason);
            return new PurchaseExecutionResult(false, goodId, quantity, validation.TotalCost, validation.Reason);
        }

        if (_resources is null)
        {
            PurchaseFailed?.Invoke(goodId, "system_unavailable");
            return new PurchaseExecutionResult(false, goodId, quantity, validation.TotalCost, "system_unavailable");
        }

        var result = _resources.ExecutePurchase(goodId, quantity);
        if (!result.Success)
        {
            var reason = ResourceFailureReason(result.Result);
            PurchaseFailed?.Invoke(goodId, reason);
            return new PurchaseExecutionResult(false, goodId, quantity, validation.TotalCost, reason);
        }

        PurchaseCompleted?.Invoke(goodId, quantity, validation.TotalCost);
        TriggerSnapshot();
        return new PurchaseExecutionResult(true, goodId, quantity, validation.TotalCost, string.Empty);
    }

    /// <summary>Computes UI max_affordable from ResourcesManager currency and storage capacity preflight.</summary>
    public int GetMaxAffordable(string goodId)
    {
        var price = ReadInt(QueryEntity(goodId), "price", 0);
        if (_resources is null || price <= 0)
        {
            return 0;
        }

        var byCurrency = _resources.GetPlayerCurrency() / price;
        var max = 0;
        for (var i = 1; i <= byCurrency; i++)
        {
            if (!_resources.ValidatePurchase(goodId, i).Valid)
            {
                break;
            }

            max = i;
        }

        return max;
    }

    /// <summary>Clamps abnormal UI quantity input into the [1, max_affordable] range.</summary>
    public int ClampPurchaseQuantity(string goodId, double requested)
    {
        var floored = (int)Math.Floor(requested);
        var lowerBounded = Math.Max(1, floored);
        var maxAffordable = GetMaxAffordable(goodId);
        return maxAffordable <= 0 ? 1 : Math.Min(lowerBounded, maxAffordable);
    }

    /// <summary>Captures a purchase-session goods list that UI may hold stable until the modal closes.</summary>
    public IReadOnlyList<string> CapturePurchaseSessionGoods(string stallId)
    {
        return GetStallGoods(stallId).ToArray();
    }

    /// <summary>Returns a safe NPC display name fallback when narrative data is unavailable.</summary>
    public string GetNpcDisplayName(string npcId)
    {
        var npc = QueryEntity(npcId);
        if (npc.Count == 0)
        {
            _warnings.Add($"Settlement: NPC '{npcId}' missing from Registry");
            return "摊主";
        }

        var narrativeKey = ReadString(npc, "narrative_key");
        if (string.IsNullOrWhiteSpace(narrativeKey))
        {
            _warnings.Add($"Settlement: NPC '{npcId}' has no narrative_key");
            return "摊主";
        }

        _warnings.Add("Settlement: narrative file missing or empty");
        return "摊主";
    }

    /// <summary>Returns true when F.2 unlock threshold is met by completed repair nodes.</summary>
    public bool IsStallUnlocked(string stallId, IEnumerable<string> completedNodeIds)
    {
        var stall = QueryEntity(stallId);
        var required = ReadStringList(stall, "required_node_ids");
        if (required.Count == 0)
        {
            return false;
        }

        var completed = completedNodeIds.ToHashSet(StringComparer.Ordinal);
        var threshold = Math.Max(1, ReadInt(stall, "unlock_threshold_basic", 1));
        return required.Count(completed.Contains) >= threshold;
    }

    /// <summary>Consumes a WorldRepair repair_completed node ID to unlock matching market stalls.</summary>
    public void OnRepairCompleted(string nodeId)
    {
        var repair = QueryEntity(nodeId);
        if (repair.Count == 0)
        {
            return;
        }

        var linkedLocationId = ReadString(repair, "linked_location_id");
        if (string.IsNullOrWhiteSpace(linkedLocationId))
        {
            return;
        }

        var settlementId = FindSettlementForLocation(linkedLocationId);
        if (string.IsNullOrWhiteSpace(settlementId) || !_settlements.TryGetValue(settlementId, out var settlement))
        {
            return;
        }

        if (!settlement.CompletedNodeIds.Add(nodeId))
        {
            return;
        }

        var changed = false;
        foreach (var stallId in GetSettlementStalls(settlementId))
        {
            if (GetStallState(stallId) == StallState.Closed && IsStallUnlocked(stallId, settlement.CompletedNodeIds))
            {
                changed |= TransitionStallState(stallId, StallState.OpenBasic);
                StallOpened?.Invoke(stallId, settlementId);
            }
        }

        RecalculateSettlementActivity(settlementId);
        if (changed || settlement.CompletedNodeIds.Contains(nodeId, StringComparer.Ordinal))
        {
            TriggerSnapshot();
        }
    }

    /// <summary>Handles Use from #4 and emits a stall-opened UI boundary for open stalls only.</summary>
    public void OnUseRequested(string targetId)
    {
        if (!_stalls.TryGetValue(targetId, out var stall) || stall.StallState < StallState.OpenBasic)
        {
            return;
        }

        StallOpened?.Invoke(targetId, stall.SettlementId);
    }

    /// <summary>Returns currently interactive open stalls for a settlement.</summary>
    public IReadOnlyList<string> GetInteractiveStalls(string settlementId)
    {
        return GetSettlementStalls(settlementId)
            .Where(stallId => GetStallState(stallId) >= StallState.OpenBasic)
            .ToArray();
    }

    /// <summary>Returns active open stall count for F.3 aggregation.</summary>
    public int GetActiveStallCount(string settlementId)
    {
        return GetSettlementStalls(settlementId).Count(stallId => GetStallState(stallId) >= StallState.OpenBasic);
    }

    /// <summary>Recalculates F.3 settlement activity and emits if state changes.</summary>
    public void RecalculateSettlementActivity(string settlementId)
    {
        if (!_settlements.TryGetValue(settlementId, out var state))
        {
            return;
        }

        var total = GetSettlementStalls(settlementId).Count;
        var active = GetActiveStallCount(settlementId);
        var next = active == 0 || active == 1
            ? SettlementState.Dormant
            : active < total ? SettlementState.Recovering : SettlementState.Active;
        if (next == state.SettlementState)
        {
            return;
        }

        if (next < state.SettlementState)
        {
            _warnings.Add($"Settlement: activity wanted regression {state.SettlementState}->{next} for '{settlementId}'");
            return;
        }

        state.SettlementState = next;
        SettlementActivityChanged?.Invoke(settlementId, active);
    }

    /// <summary>Serializes settlement runtime state into the ADR-0003 payload shape.</summary>
    public Dictionary<string, object?> SerializeSettlement()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["domain_id"] = "settlement-market",
            ["settlements"] = _settlements.ToDictionary(
                pair => pair.Key,
                pair => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["settlement_state"] = (int)pair.Value.SettlementState,
                    ["completed_node_ids"] = pair.Value.CompletedNodeIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                },
                StringComparer.Ordinal),
            ["stalls"] = _stalls.ToDictionary(
                pair => pair.Key,
                pair => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["stall_state"] = (int)pair.Value.StallState,
                    ["settlement_id"] = pair.Value.SettlementId,
                },
                StringComparer.Ordinal),
            ["npcs"] = _npcs.ToDictionary(
                pair => pair.Key,
                pair => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["npc_state"] = (int)pair.Value.NpcState,
                    ["stall_id"] = pair.Value.StallId,
                },
                StringComparer.Ordinal),
        };
    }

    /// <summary>Restores settlement runtime state from an ADR-0003 payload and reconciles derived invariants.</summary>
    public void DeserializeSettlement(IReadOnlyDictionary<string, object?> snapshot)
    {
        InitNewGameState();

        foreach (var (settlementId, rawState) in ReadObjectMap(snapshot, "settlements"))
        {
            if (QueryEntity(settlementId).Count == 0)
            {
                _warnings.Add($"Settlement: skipping unknown settlement '{settlementId}' in snapshot");
                continue;
            }

            var data = ToObjectMap(rawState);
            var state = new SettlementRuntimeState((SettlementState)ReadInt(data, "settlement_state", 0));
            foreach (var nodeId in ReadStringList(data, "completed_node_ids").Distinct(StringComparer.Ordinal))
            {
                if (QueryEntity(nodeId).Count == 0)
                {
                    _warnings.Add($"Settlement: snapshot completed_node_id '{nodeId}' not found in Registry");
                }

                state.CompletedNodeIds.Add(nodeId);
            }

            _settlements[settlementId] = state;
        }

        foreach (var (stallId, rawState) in ReadObjectMap(snapshot, "stalls"))
        {
            if (QueryEntity(stallId).Count == 0)
            {
                _warnings.Add($"Settlement: skipping unknown stall '{stallId}' in snapshot");
                continue;
            }

            var data = ToObjectMap(rawState);
            _stalls[stallId] = new StallRuntimeState(
                (StallState)ReadInt(data, "stall_state", 0),
                ReadString(data, "settlement_id"));
        }

        foreach (var (npcId, rawState) in ReadObjectMap(snapshot, "npcs"))
        {
            if (QueryEntity(npcId).Count == 0)
            {
                _warnings.Add($"Settlement: skipping unknown NPC '{npcId}' in snapshot");
                continue;
            }

            var data = ToObjectMap(rawState);
            _npcs[npcId] = new NpcRuntimeState(
                (NpcState)ReadInt(data, "npc_state", 0),
                ReadString(data, "stall_id"));
        }

        ReconcileSettlementState();
    }

    /// <summary>Builds a persistence package for progress.settlement-market.</summary>
    public SnapshotPackage BuildSnapshotPackage()
    {
        var package = new SnapshotPackage
        {
            DomainId = ProgressDomainId,
            SnapshotSchemaVersion = 1,
            DomainState = SnapshotDomainState.Ready,
        };
        package.ContentDomainVersions["settlement-market"] = "2026-05-14";
        package.StableIdRefs.AddRange(_settlements.Keys.Concat(_stalls.Keys).Concat(_npcs.Keys).OrderBy(id => id, StringComparer.Ordinal));
        foreach (var (key, value) in SerializeSettlement())
        {
            package.Payload[key] = value;
        }

        return package;
    }

    /// <summary>Restores state from a persistence package.</summary>
    public void RestoreFromSnapshotPackage(SnapshotPackage package)
    {
        if (package.DomainId == ProgressDomainId)
        {
            DeserializeSettlement(package.Payload);
        }
    }

    private void RegisterOpenStalls()
    {
        if (_registerFocusTarget is null)
        {
            return;
        }

        foreach (var stallId in _stalls.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (GetStallState(stallId) >= StallState.OpenBasic)
            {
                _registerFocusTarget(stallId, GetStallLabel(stallId), WorldVector2.Zero);
            }
        }
    }

    private void UnlockNpcForStall(string stallId)
    {
        foreach (var npcId in _npcs.Where(pair => pair.Value.StallId == stallId).Select(pair => pair.Key).ToArray())
        {
            TransitionNpcState(npcId, NpcState.Idle);
        }
    }

    private void TriggerSnapshot()
    {
        SnapshotTriggerCount++;
        if (_captureSnapshot is not null && !_captureSnapshot())
        {
            _errors.Add("Settlement: failed to persist snapshot");
        }
    }

    private string FindSettlementForLocation(string locationId)
    {
        foreach (var settlement in _registry.ListByKind("settlement"))
        {
            if (ReadStringList(settlement, "linked_location_ids").Contains(locationId, StringComparer.Ordinal))
            {
                return ReadString(settlement, "id");
            }
        }

        return string.Empty;
    }

    private void ReconcileSettlementState()
    {
        foreach (var npc in _npcs)
        {
            if (!_stalls.TryGetValue(npc.Value.StallId, out var stall))
            {
                continue;
            }

            if (stall.StallState >= StallState.OpenBasic && npc.Value.NpcState < NpcState.Idle)
            {
                _warnings.Add($"Settlement: NPC '{npc.Key}' inconsistent with stall '{npc.Value.StallId}' - auto-correcting");
                npc.Value.NpcState = NpcState.Idle;
            }
            else if (stall.StallState < StallState.OpenBasic && npc.Value.NpcState >= NpcState.Idle)
            {
                _warnings.Add($"Settlement: NPC '{npc.Key}' inconsistent with stall '{npc.Value.StallId}' - auto-correcting");
                npc.Value.NpcState = NpcState.Absent;
            }
        }

        foreach (var settlementId in _settlements.Keys.ToArray())
        {
            var previous = _settlements[settlementId].SettlementState;
            _settlements[settlementId].SettlementState = SettlementState.Dormant;
            RecalculateSettlementActivity(settlementId);
            if (_settlements[settlementId].SettlementState != previous)
            {
                _warnings.Add($"Settlement: corrected settlement_state for '{settlementId}' during restore");
            }
        }
    }

    private IReadOnlyDictionary<string, object?> QueryEntity(string id)
    {
        var query = _registry.QueryById(id);
        return query.Status == RegistryQueryStatus.Found && query.Entity is not null
            ? query.Entity
            : new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private string GetStallLabel(string stallId)
    {
        var entity = QueryEntity(stallId);
        return ReadString(entity, "display_name", stallId);
    }

    private static string ResourceFailureReason(ResourceResult result)
    {
        return result is ResourceResult.ErrTargetFull or ResourceResult.ErrStorageFull or ResourceResult.ErrCapacityZero
            ? PurchaseFailCapacity
            : result == ResourceResult.ErrNotInitialized ? "system_unavailable" : PurchaseFailFunds;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> data, string key, string fallback = "")
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() ?? fallback : fallback;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> data, string key, int fallback)
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
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => fallback,
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> data, string key)
    {
        return data.TryGetValue(key, out var value) && value is bool boolValue && boolValue;
    }

    private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text) ? Array.Empty<string>() : [text];
        }

        if (value is System.Collections.IEnumerable list)
        {
            return list.Cast<object?>()
                .Select(item => item?.ToString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
        }

        return Array.Empty<string>();
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

    private static IEnumerable<KeyValuePair<string, object?>> ReadObjectMap(IReadOnlyDictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        return value switch
        {
            IReadOnlyDictionary<string, object?> readOnly => readOnly,
            IDictionary<string, object?> mutable => new Dictionary<string, object?>(mutable, StringComparer.Ordinal),
            _ => [],
        };
    }

    private sealed class SettlementRuntimeState
    {
        public SettlementRuntimeState(SettlementState settlementState)
        {
            SettlementState = settlementState;
        }

        public SettlementState SettlementState { get; set; }
        public HashSet<string> CompletedNodeIds { get; } = new(StringComparer.Ordinal);
    }

    private sealed class StallRuntimeState
    {
        public StallRuntimeState(StallState stallState, string settlementId)
        {
            StallState = stallState;
            SettlementId = settlementId;
        }

        public StallState StallState { get; set; }
        public string SettlementId { get; }
    }

    private sealed class NpcRuntimeState
    {
        public NpcRuntimeState(NpcState npcState, string stallId)
        {
            NpcState = npcState;
            StallId = stallId;
        }

        public NpcState NpcState { get; set; }
        public string StallId { get; }
    }
}
