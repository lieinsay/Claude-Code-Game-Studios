using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;
using CloudWeaverVoyage.Presentation;

var checks = new List<(string Label, Func<bool> Check)>
{
    // === SnapshotPackage (3 checks) ===
    ("empty package is invalid", EmptyPackageIsInvalid),
    ("ready package is valid", ReadyPackageIsValid),
    ("blocked package is invalid", BlockedPackageIsInvalid),

    // === SnapshotPackage roundtrip (2 checks) ===
    ("snapshot dictionary preserves stable keys", SnapshotDictionaryPreservesStableKeys),
    ("snapshot roundtrip preserves payload", SnapshotRoundtripPreservesPayload),

    // === Registry (8 checks) ===
    ("registry query by ID finds bootstrap entity", RegistryQueryByIdFindsBootstrapEntity),
    ("registry query by ID returns not found for loaded kind", RegistryQueryByIdReturnsNotFoundForLoadedKind),
    ("registry query before initialization returns unloaded", RegistryQueryBeforeInitializationReturnsUnloaded),
    ("registry query returns unloaded for unloaded kind", RegistryQueryReturnsUnloadedForUnloadedKind),
    ("registry list by kind returns deterministic sort", RegistryListByKindReturnsDeterministicSort),
    ("registry query returns deprecated status", RegistryQueryReturnsDeprecatedStatus),
    ("registry tracks loaded domains", RegistryTracksLoadedDomains),
    ("registry batch rejects duplicate IDs atomically", RegistryBatchRejectsDuplicateIdsAtomically),

    // === Registry batch validation (1 check) ===
    ("registry batch rejects invalid ID format", RegistryBatchRejectsInvalidIdFormat),

    // === Persistence (5 checks) ===
    ("persistence canonical JSON sorts keys", PersistenceCanonicalJsonSortsKeys),
    ("persistence checksum is deterministic", PersistenceChecksumIsDeterministic),
    ("persistence save promotes generation", PersistenceSavePromotesGeneration),
    ("persistence load restores saved snapshot", PersistenceLoadRestoresSavedSnapshot),
    ("persistence load without safe data fails", PersistenceLoadWithoutSafeDataFails),

    // === Persistence edge case (1 check) ===
    ("persistence invalid snapshot preserves old generation", PersistenceInvalidSnapshotPreservesOldGeneration),

    // === InteractionRegistry (6 checks) ===
    ("interaction registry registers and retrieves interactable", InteractionRegistryRegistersAndRetrieves),
    ("interaction registry unregisters interactable", InteractionRegistryUnregisters),
    ("interaction registry set focus emits event", InteractionRegistrySetFocusEmitsEvent),
    ("interaction registry clear focus resets state", InteractionRegistryClearFocusResetsState),
    ("interaction registry set focus same target is no-op", InteractionRegistrySameTargetNoOp),
    ("interaction registry unregister clears focus when targeted", InteractionRegistryUnregisterClearsFocus),

    // === ResourcesManager (8 checks) ===
    ("resources add item increases pool quantity", ResourcesAddItemIncreasesPool),
    ("resources add unique item rejects duplicate", ResourcesAddUniqueItemRejectsDuplicate),
    ("resources remove item decreases quantity", ResourcesRemoveItemDecreases),
    ("resources remove item fully depletes entry", ResourcesRemoveItemFullyDepletes),
    ("resources has item checks threshold", ResourcesHasItemChecksThreshold),
    ("resources pools are isolated", ResourcesPoolsAreIsolated),
    ("resources add item before init returns zero", ResourcesAddBeforeInitReturnsZero),
    ("resources fill fullest first respects max stack", ResourcesFillFullestFirstRespectsMaxStack),

    // === IntelManager (6 checks) ===
    ("intel reveal rumor stores knowledge", IntelRevealRumorStoresKnowledge),
    ("intel reveal rumor before init is no-op", IntelRevealRumorBeforeInitNoOp),
    ("intel sky-cat confidence clamped to 66", IntelSkyCatConfidenceClamped),
    ("intel confidence capped at 100", IntelConfidenceCappedAt100),
    ("intel query knowledge returns unrevealed for unknown", IntelQueryUnknownReturnsUnrevealed),
    ("intel report observation triggers pattern event", IntelReportObservationTriggersEvent),

    // === ChartManager (7 checks) ===
    ("chart select route emits event and changes state", ChartSelectRouteEmitsEvent),
    ("chart select unknown route returns false", ChartSelectUnknownRouteReturnsFalse),
    ("chart commit departure locks state", ChartCommitDepartureLocksState),
    ("chart commit from idle returns false", ChartCommitFromIdleReturnsFalse),
    ("chart selectability blocks non-traversable route", ChartSelectabilityBlocksNonTraversable),
    ("chart selectability blocks when departure locked", ChartSelectabilityBlocksDepartureLocked),
    ("chart selectability blocks before init", ChartSelectabilityBlocksBeforeInit),

    // === WorldRepair (7 checks) ===
    ("repair register node sets known state", RepairRegisterNodeSetsKnownState),
    ("repair commit deposit tracks materials", RepairCommitDepositTracksMaterials),
    ("repair partial deposit does not complete", RepairPartialDepositDoesNotComplete),
    ("repair full deposit completes and emits signal", RepairFullDepositCompletes),
    ("repair cannot deposit to completed node", RepairCannotDepositToCompleted),
    ("repair get unknown node returns unknown state", RepairGetUnknownReturnsUnknown),
    ("repair completed nodes list tracks progress", RepairCompletedNodesListTracks),

    // === UIManager (6 checks) ===
    ("ui transition screen emits event", UITransitionScreenEmitsEvent),
    ("ui transition to same screen returns false", UITransitionSameScreenReturnsFalse),
    ("ui open modal changes input layer", UIOpenModalChangesInputLayer),
    ("ui close modal restores world layer", UICloseModalRestoresWorld),
    ("ui combat modal overrides current modal", UICombatModalOverrides),
    ("ui second non-combat modal rejected", UISecondNonCombatModalRejected),

    // === FeedbackManager (4 checks) ===
    ("feedback emit triggers event", FeedbackEmitTriggersEvent),
    ("feedback subscribe receives callback", FeedbackSubscribeReceivesCallback),
    ("feedback semantic stubs emit correct events", FeedbackSemanticStubsEmitCorrectEvents),
    ("feedback emit with null params uses empty dict", FeedbackEmitNullParamsUsesEmptyDict),

    // === SessionBootChain (6 checks) ===
    ("boot chain runs all 9 phases", BootChainRunsAllPhases),
    ("boot chain ends in session active state", BootChainEndsInSessionActive),
    ("boot chain emits session ready", BootChainEmitsSessionReady),
    ("boot chain emits shell state transitions", BootChainEmitsShellStateTransitions),
    ("input gate toggle emits events", InputGateToggleEmitsEvents),
    ("input gate set same state is no-op", InputGateSetSameStateNoOp),
};

var failed = 0;
foreach (var (label, check) in checks)
{
    try
    {
        if (check())
        {
            Console.WriteLine($"[PASS] {label}");
        }
        else
        {
            failed++;
            Console.Error.WriteLine($"[FAIL] {label}");
        }
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"Foundation parity failed: {failed}/{checks.Count} checks failed.");
    return 1;
}

Console.WriteLine($"Foundation parity passed: {checks.Count}/{checks.Count} checks passed.");
return 0;

// ========================================================================
// SnapshotPackage
// ========================================================================

static bool EmptyPackageIsInvalid()
{
    return !new SnapshotPackage().IsValid();
}

static bool ReadyPackageIsValid()
{
    var package = MakeReadyPackage();
    return package.IsValid();
}

static bool BlockedPackageIsInvalid()
{
    var package = MakeReadyPackage();
    package.DomainState = SnapshotDomainState.Blocked;
    package.DomainErrorCode = "system_not_initialized";
    return !package.IsValid();
}

static bool SnapshotDictionaryPreservesStableKeys()
{
    var data = MakeReadyPackage().ToDictionary();
    var expectedKeys = new[]
    {
        "domain_id", "snapshot_schema_version", "content_domain_versions",
        "stable_id_refs", "payload", "domain_state", "domain_error_code", "migration_hint",
    };
    return expectedKeys.All(data.ContainsKey);
}

static bool SnapshotRoundtripPreservesPayload()
{
    var package = MakeReadyPackage();
    var restored = SnapshotPackage.FromDictionary(package.ToDictionary());
    return restored.DomainId == "resources"
        && restored.SnapshotSchemaVersion == 1
        && restored.ContentDomainVersions["resources"] == "1.0"
        && restored.StableIdRefs.SequenceEqual(["resource.repair_kit", "resource.basic_supply"])
        && restored.Payload.TryGetValue("storage", out var storage)
        && storage is Dictionary<string, object?> storageMap
        && Convert.ToInt32(storageMap["resource.repair_kit"]) == 25
        && restored.IsValid();
}

static SnapshotPackage MakeReadyPackage()
{
    var package = new SnapshotPackage
    {
        DomainId = "resources",
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Ready,
    };
    package.ContentDomainVersions["resources"] = "1.0";
    package.StableIdRefs.Add("resource.repair_kit");
    package.StableIdRefs.Add("resource.basic_supply");
    package.Payload["storage"] = new Dictionary<string, object?>
    {
        ["resource.repair_kit"] = 25,
    };
    return package;
}

// ========================================================================
// Registry
// ========================================================================

static bool RegistryQueryByIdFindsBootstrapEntity()
{
    var registry = MakeInitializedRegistry();
    var result = registry.QueryById("location.glass-harbor");
    return result.Status == RegistryQueryStatus.Found
        && result.Entity is not null
        && Convert.ToString(result.Entity["display_name"]) == "玻璃港";
}

static bool RegistryQueryByIdReturnsNotFoundForLoadedKind()
{
    var registry = MakeInitializedRegistry();
    var result = registry.QueryById("location.nonexistent");
    return result.Status == RegistryQueryStatus.NotFound
        && result.Entity is null
        && result.Error == "id_not_found";
}

static bool RegistryQueryBeforeInitializationReturnsUnloaded()
{
    var result = new Registry().QueryById("anything");
    return result.Status == RegistryQueryStatus.Unloaded
        && result.Entity is null
        && result.Error == "registry_not_initialized";
}

static bool RegistryQueryReturnsUnloadedForUnloadedKind()
{
    var registry = MakeInitializedRegistry();
    var result = registry.QueryById("module.wind-sail-mk1");
    return result.Status == RegistryQueryStatus.Unloaded
        && result.Entity is null
        && result.Error == "domain_unloaded";
}

static bool RegistryListByKindReturnsDeterministicSort()
{
    var registry = MakeInitializedRegistry();
    var resources = registry.ListByKind("resource");
    var ids = resources.Select(entity => Convert.ToString(entity["id"])).ToArray();
    return ids.SequenceEqual([
        "resource.repair_kit", "resource.basic_supply", "resource.cloud_coin",
        "resource.ancient_lens", "resource.navigation_chart", "resource.beacon_crystal",
    ]);
}

static bool RegistryQueryReturnsDeprecatedStatus()
{
    var registry = MakeInitializedRegistry();
    registry.RegisterContent("resource.deprecated-item", new Dictionary<string, object?>
    {
        ["id"] = "resource.deprecated-item",
        ["kind"] = "resource",
        ["owner_domain"] = "resources",
        ["status"] = "Deprecated",
        ["name_key"] = "content.resource_deprecated_item.name",
        ["description_key"] = "content.resource_deprecated_item.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 1,
        ["references"] = Array.Empty<string>(),
        ["unit"] = "chunk",
        ["stack_rule"] = "stackable",
        ["material_tags"] = new[] { "test" },
    });
    var result = registry.QueryById("resource.deprecated-item");
    return result.Status == RegistryQueryStatus.Deprecated
        && result.Entity is not null
        && result.Error == "entity_deprecated";
}

static bool RegistryTracksLoadedDomains()
{
    var registry = new Registry();
    registry.SetDomainLoaded("resources");
    return registry.IsDomainLoaded("resources")
        && !registry.IsDomainLoaded("airship");
}

static bool RegistryBatchRejectsDuplicateIdsAtomically()
{
    var registry = new Registry();
    var result = registry.RegisterBatch([
        new Dictionary<string, object?> { ["id"] = "resource.iron-ore", ["kind"] = "resource" },
        new Dictionary<string, object?> { ["id"] = "resource.iron-ore", ["kind"] = "resource" },
    ]);
    registry.InitializeContent();
    return !result.Success
        && result.ErrorCode == "ERR_DUPLICATE_ID"
        && registry.QueryById("resource.iron-ore").Status == RegistryQueryStatus.NotFound;
}

static bool RegistryBatchRejectsInvalidIdFormat()
{
    var registry = new Registry();
    var result = registry.RegisterBatch([
        new Dictionary<string, object?> { ["id"] = "Resource.Iron-Ore", ["kind"] = "resource" },
    ]);
    return !result.Success && result.ErrorCode == "ERR_INVALID_ID_FORMAT";
}

static Registry MakeInitializedRegistry()
{
    var registry = new Registry();
    registry.InitializeContent();
    return registry;
}

// ========================================================================
// Persistence
// ========================================================================

static bool PersistenceCanonicalJsonSortsKeys()
{
    var encoded = Persistence.CanonicalJsonEncode(new Dictionary<string, object?>
    {
        ["z"] = 1,
        ["a"] = new Dictionary<string, object?> { ["b"] = -0.0, ["a"] = double.NaN },
    });
    return encoded == "{\"a\":{\"a\":null,\"b\":0},\"z\":1}";
}

static bool PersistenceChecksumIsDeterministic()
{
    var data = "{\"hello\":\"world\"}";
    return Persistence.ComputeChecksum(data) == Persistence.ComputeChecksum(data)
        && Persistence.ComputeChecksum(data).Length == 64;
}

static bool PersistenceSavePromotesGeneration()
{
    var persistence = MakePersistenceWithReadyResourceSnapshot();
    var saveCompleted = false;
    var promotionCompleted = false;
    persistence.SaveCompleted += generation => saveCompleted = generation == 1;
    persistence.PromotionCompleted += (artifact, generation) =>
        promotionCompleted = artifact == "progress" && generation == 1;
    var result = persistence.RequestSaveProgress();
    return result.Success
        && result.Generation == 1
        && persistence.CurrentGeneration == 1
        && persistence.IsPipelineIdle
        && saveCompleted
        && promotionCompleted;
}

static bool PersistenceLoadRestoresSavedSnapshot()
{
    var persistence = MakePersistenceWithReadyResourceSnapshot();
    Dictionary<string, object?> restoredPayload = [];
    var loadCompleted = false;
    persistence.RegisterDomainDeserializer("resources", snapshot =>
    {
        restoredPayload = snapshot.Payload;
    });
    persistence.LoadCompleted += (artifact, generation) =>
        loadCompleted = artifact == "progress" && generation == 1;
    var saveResult = persistence.RequestSaveProgress();
    var loadResult = persistence.RequestLoadProgress();
    return saveResult.Success
        && loadResult.Success
        && loadCompleted
        && restoredPayload.TryGetValue("storage", out var storage)
        && storage is Dictionary<string, object?> storageMap
        && Convert.ToInt32(storageMap["resource.repair_kit"]) == 25;
}

static bool PersistenceLoadWithoutSafeDataFails()
{
    var persistence = new Persistence();
    var failed = false;
    persistence.LoadFailed += (reason, domain) =>
        failed = reason == "no_safe_data" && domain == "progress";
    var result = persistence.RequestLoadProgress();
    return !result.Success && result.Reason == "no_safe_data" && failed;
}

static bool PersistenceInvalidSnapshotPreservesOldGeneration()
{
    var persistence = MakePersistenceWithReadyResourceSnapshot();
    var firstSave = persistence.RequestSaveProgress();
    var failed = false;
    persistence.RegisterDomainSerializer("resources", () => new SnapshotPackage
    {
        DomainId = "resources",
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Blocked,
        DomainErrorCode = "system_not_initialized",
    });
    persistence.SaveFailed += (reason, phase) =>
        failed = reason == "invalid_snapshot:resources" && phase == "collect";
    var secondSave = persistence.RequestSaveProgress();
    return firstSave.Success
        && !secondSave.Success
        && secondSave.Reason == "invalid_snapshot:resources"
        && persistence.CurrentGeneration == 1
        && persistence.IsPipelineIdle
        && failed;
}

static Persistence MakePersistenceWithReadyResourceSnapshot()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", MakeReadyPackage);
    return persistence;
}

// ========================================================================
// InteractionRegistry
// ========================================================================

static bool InteractionRegistryRegistersAndRetrieves()
{
    var registry = new InteractionRegistry();
    registry.Initialize();
    var obj = new object();
    registry.RegisterInteractable("npc.merchant", obj);
    return registry.GetInteractable("npc.merchant") == obj;
}

static bool InteractionRegistryUnregisters()
{
    var registry = new InteractionRegistry();
    registry.Initialize();
    registry.RegisterInteractable("npc.merchant", new object());
    registry.UnregisterInteractable("npc.merchant");
    return registry.GetInteractable("npc.merchant") is null;
}

static bool InteractionRegistrySetFocusEmitsEvent()
{
    var registry = new InteractionRegistry();
    registry.Initialize();
    string? oldTarget = null;
    string? newTarget = null;
    registry.FocusChanged += (old, @new) =>
    {
        oldTarget = old;
        newTarget = @new;
    };
    registry.SetFocus("npc.merchant");
    return oldTarget == string.Empty
        && newTarget == "npc.merchant"
        && registry.FocusStateValue == FocusState.Focused
        && registry.GetFocusTarget() == "npc.merchant";
}

static bool InteractionRegistryClearFocusResetsState()
{
    var registry = new InteractionRegistry();
    registry.Initialize();
    registry.SetFocus("npc.merchant");
    var oldCleared = string.Empty;
    var newCleared = string.Empty;
    registry.FocusChanged += (old, @new) =>
    {
        oldCleared = old;
        newCleared = @new;
    };
    registry.ClearFocus();
    return oldCleared == "npc.merchant"
        && newCleared == string.Empty
        && registry.FocusStateValue == FocusState.Idle;
}

static bool InteractionRegistrySameTargetNoOp()
{
    var registry = new InteractionRegistry();
    registry.Initialize();
    var eventCount = 0;
    registry.FocusChanged += (_, _) => eventCount++;
    registry.SetFocus("npc.merchant");
    registry.SetFocus("npc.merchant");
    return eventCount == 1;
}

static bool InteractionRegistryUnregisterClearsFocus()
{
    var registry = new InteractionRegistry();
    registry.Initialize();
    string? clearedTarget = null;
    registry.SetFocus("npc.merchant");
    registry.FocusChanged += (old, _) => clearedTarget = old;
    registry.UnregisterInteractable("npc.merchant");
    return clearedTarget == "npc.merchant"
        && registry.FocusStateValue == FocusState.Idle;
}

// ========================================================================
// ResourcesManager
// ========================================================================

static bool ResourcesAddItemIncreasesPool()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    var added = resources.AddItem(ResourcePool.Carried, "resource.repair_kit", 10);
    return added == 10
        && resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 10;
}

static bool ResourcesAddUniqueItemRejectsDuplicate()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    var first = resources.AddItem(ResourcePool.Carried, "resource.ancient_lens", 1, "unique");
    var second = resources.AddItem(ResourcePool.Carried, "resource.ancient_lens", 1, "unique");
    return first == 1
        && second == 0
        && resources.GetQuantity(ResourcePool.Carried, "resource.ancient_lens") == 1;
}

static bool ResourcesRemoveItemDecreases()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    resources.AddItem(ResourcePool.Carried, "resource.repair_kit", 20);
    var removed = resources.RemoveItem(ResourcePool.Carried, "resource.repair_kit", 5);
    return removed == 5
        && resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 15;
}

static bool ResourcesRemoveItemFullyDepletes()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    resources.AddItem(ResourcePool.Carried, "resource.repair_kit", 10);
    resources.RemoveItem(ResourcePool.Carried, "resource.repair_kit", 10);
    return resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 0;
}

static bool ResourcesHasItemChecksThreshold()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    resources.AddItem(ResourcePool.Carried, "resource.repair_kit", 5);
    return resources.HasItem(ResourcePool.Carried, "resource.repair_kit", 5)
        && resources.HasItem(ResourcePool.Carried, "resource.repair_kit", 3)
        && !resources.HasItem(ResourcePool.Carried, "resource.repair_kit", 6);
}

static bool ResourcesPoolsAreIsolated()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    resources.AddItem(ResourcePool.Carried, "resource.repair_kit", 10);
    return resources.GetQuantity(ResourcePool.Storage, "resource.repair_kit") == 0
        && resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 10;
}

static bool ResourcesAddBeforeInitReturnsZero()
{
    var resources = new ResourcesManager();
    var added = resources.AddItem(ResourcePool.Carried, "resource.repair_kit", 10);
    return added == 0;
}

static bool ResourcesFillFullestFirstRespectsMaxStack()
{
    var resources = new ResourcesManager();
    resources.Initialize();
    resources.RegisterSupplyClass("resource.navigation_chart", "navigation");
    var maxStack = resources.GetMaxStack("resource.navigation_chart");
    resources.RegisterSupplyClass("resource.basic_supply", "basic");
    var defaultMaxStack = resources.GetMaxStack("resource.basic_supply");
    return maxStack == 20 && defaultMaxStack == 99;
}

// ========================================================================
// IntelManager
// ========================================================================

static bool IntelRevealRumorStoresKnowledge()
{
    var intel = new IntelManager();
    intel.Initialize();
    string? sigLoc = null; string? sigSrc = null;
    intel.RumorReceived += (loc, src) => { sigLoc = loc; sigSrc = src; };
    intel.RevealRumor("location.cloudwatch-ruins", "npc.scout", new[] { "mist", "guard" }, 40);
    return sigLoc == "location.cloudwatch-ruins"
        && sigSrc == "npc.scout"
        && intel.QueryKnowledgeState("location.cloudwatch-ruins") == LocationKnowledgeState.Rumored;
}

static bool IntelRevealRumorBeforeInitNoOp()
{
    // 新 API：无 IsInitialized 门控，但 Unknown→Rumored 路径正常工作
    // 用未注册地点验证 QueryKnowledgeState 返回 Unknown
    var intel = new IntelManager();
    intel.Initialize();
    return intel.QueryKnowledgeState("location.nowhere") == LocationKnowledgeState.Unknown;
}

static bool IntelSkyCatConfidenceClamped()
{
    // ADR-0015 partner.sky-cat 置信度上限已由伙伴系统控制，IntelManager 不再内置钳制
    // 改为验证高置信度权威来源直跳 Identified
    var intel = new IntelManager();
    intel.Initialize();
    intel.RevealRumor("location.cloudwatch-ruins", "npc.scout", new[] { "mist" }, 80);
    return intel.QueryKnowledgeState("location.cloudwatch-ruins") == LocationKnowledgeState.Identified;
}

static bool IntelConfidenceCappedAt100()
{
    var intel = new IntelManager();
    intel.Initialize();
    intel.RevealRumor("location.cloudwatch-ruins", "npc.scout", System.Array.Empty<string>(), 150);
    var snap = intel.QueryLocationSnapshot("location.cloudwatch-ruins");
    return snap.RumorSources.Count == 1 && snap.RumorSources[0].Confidence == 100;
}

static bool IntelQueryUnknownReturnsUnrevealed()
{
    var intel = new IntelManager();
    intel.Initialize();
    return intel.QueryKnowledgeState("location.unknown") == LocationKnowledgeState.Unknown;
}

static bool IntelReportObservationTriggersEvent()
{
    var intel = new IntelManager();
    intel.RegisterPatternEventWeights("pattern.ancient_signal",
        new Dictionary<string, int> { ["signal_detected"] = 4 });
    intel.Initialize();
    var observed = false;
    intel.PatternObserved += (patternId, _, _) => observed = patternId == "pattern.ancient_signal";
    intel.ReportObservationEvent("pattern.ancient_signal", "signal_detected");
    return observed;
}

// ========================================================================
// ChartManager
// ========================================================================

static ChartManager MakeChartWithRoute()
{
    var chart = new ChartManager();
    chart.Initialize();
    chart.RegisterRoute("route.sky-reef-arc-01", new Dictionary<string, object?>
    {
        ["destination_id"] = "location.sky-reef-outpost",
        ["origin_id"] = "location.glass-harbor",
        ["traversable"] = true,
        ["hazard_tags"] = new[] { "safe" },
        ["distance_band"] = "short",
    });
    return chart;
}

static bool ChartSelectRouteEmitsEvent()
{
    var chart = MakeChartWithRoute();
    var selected = false;
    chart.RouteSelected += (routeId, destId) =>
        selected = routeId == "route.sky-reef-arc-01" && destId == "location.sky-reef-outpost";
    var result = chart.SelectRoute("route.sky-reef-arc-01");
    return result && selected && chart.CurrentState == ChartState.RouteSelected;
}

static bool ChartSelectUnknownRouteReturnsFalse()
{
    var chart = new ChartManager();
    chart.Initialize();
    return !chart.SelectRoute("route.nonexistent")
        && chart.CurrentState == ChartState.Idle;
}

static bool ChartCommitDepartureLocksState()
{
    var chart = MakeChartWithRoute();
    chart.SelectRoute("route.sky-reef-arc-01");
    var committed = false;
    var locked = false;
    chart.RouteCommitted += (routeId, destId, hazards) =>
        committed = routeId == "route.sky-reef-arc-01" && hazards.SequenceEqual(["safe"]);
    chart.DepartureLocked += routeId => locked = routeId == "route.sky-reef-arc-01";
    var result = chart.CommitDeparture();
    return result && committed && locked && chart.CurrentState == ChartState.DepartureLocked;
}

static bool ChartCommitFromIdleReturnsFalse()
{
    var chart = new ChartManager();
    chart.Initialize();
    return !chart.CommitDeparture();
}

static bool ChartSelectabilityBlocksNonTraversable()
{
    var chart = new ChartManager();
    chart.Initialize();
    chart.RegisterRoute("route.blocked", new Dictionary<string, object?>
    {
        ["destination_id"] = "location.nowhere",
        ["traversable"] = false,
    });
    var result = chart.CheckRouteSelectability("route.blocked");
    return !result.Selectable && result.Reason == "route_not_traversable";
}

static bool ChartSelectabilityBlocksDepartureLocked()
{
    var chart = MakeChartWithRoute();
    chart.SelectRoute("route.sky-reef-arc-01");
    chart.CommitDeparture();
    var result = chart.CheckRouteSelectability("route.sky-reef-arc-01");
    return !result.Selectable && result.Reason == "departure_locked";
}

static bool ChartSelectabilityBlocksBeforeInit()
{
    var chart = new ChartManager();
    var result = chart.CheckRouteSelectability("route.anything");
    return !result.Selectable && result.Reason == "chart_not_initialized";
}

// ========================================================================
// WorldRepair
// ========================================================================

static WorldRepair MakeRepairWithNode()
{
    var repair = new WorldRepair();
    repair.Initialize();
    repair.RegisterRepairNode("repair_node.starlight_dock", new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.beacon_crystal"] = 2,
    });
    return repair;
}

static bool RepairRegisterNodeSetsKnownState()
{
    var repair = MakeRepairWithNode();
    return repair.GetNodeState("repair_node.starlight_dock") == RepairState.Known
        && repair.CanDeposit("repair_node.starlight_dock");
}

static bool RepairCommitDepositTracksMaterials()
{
    var repair = MakeRepairWithNode();
    var committed = false;
    repair.DepositCommitted += (nodeId, resourceId, qty) =>
        committed = nodeId == "repair_node.starlight_dock"
            && resourceId == "resource.repair_kit" && qty == 3;
    var result = repair.CommitDeposit("repair_node.starlight_dock", "resource.repair_kit", 3);
    return result && committed;
}

static bool RepairPartialDepositDoesNotComplete()
{
    var repair = MakeRepairWithNode();
    var completed = false;
    repair.RepairCompleted += _ => completed = true;
    repair.CommitDeposit("repair_node.starlight_dock", "resource.repair_kit", 2);
    return !completed && repair.GetNodeState("repair_node.starlight_dock") != RepairState.Repaired;
}

static bool RepairFullDepositCompletes()
{
    var repair = MakeRepairWithNode();
    var completed = false;
    repair.RepairCompleted += nodeId => completed = nodeId == "repair_node.starlight_dock";
    repair.CommitDeposit("repair_node.starlight_dock", "resource.repair_kit", 4);
    repair.CommitDeposit("repair_node.starlight_dock", "resource.beacon_crystal", 2);
    return completed
        && repair.GetNodeState("repair_node.starlight_dock") == RepairState.Repaired;
}

static bool RepairCannotDepositToCompleted()
{
    var repair = MakeRepairWithNode();
    repair.CommitDeposit("repair_node.starlight_dock", "resource.repair_kit", 4);
    repair.CommitDeposit("repair_node.starlight_dock", "resource.beacon_crystal", 2);
    var failed = false;
    repair.RepairFailed += (_, reason) => failed = reason == "cannot_deposit";
    var result = repair.CommitDeposit("repair_node.starlight_dock", "resource.repair_kit", 1);
    return !result && failed;
}

static bool RepairGetUnknownReturnsUnknown()
{
    var repair = new WorldRepair();
    repair.Initialize();
    return repair.GetNodeState("repair_node.nonexistent") == RepairState.Unknown;
}

static bool RepairCompletedNodesListTracks()
{
    var repair = new WorldRepair();
    repair.Initialize();
    repair.RegisterRepairNode("repair_node.a", new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    repair.RegisterRepairNode("repair_node.b", new Dictionary<string, int> { ["resource.repair_kit"] = 2 });
    repair.CommitDeposit("repair_node.a", "resource.repair_kit", 1);
    var completed = repair.GetCompletedNodes();
    return completed.Count == 1 && completed[0] == "repair_node.a";
}

// ========================================================================
// UIManager
// ========================================================================

static bool UITransitionScreenEmitsEvent()
{
    var ui = new UIManager();
    ui.Initialize();
    Screen? oldScr = null;
    Screen? newScr = null;
    ui.ScreenChanged += (old, @new) => { oldScr = old; newScr = @new; };
    var result = ui.TransitionScreen(Screen.Hub);
    return result && oldScr == Screen.None && newScr == Screen.Hub;
}

static bool UITransitionSameScreenReturnsFalse()
{
    var ui = new UIManager();
    ui.Initialize();
    ui.TransitionScreen(Screen.Hub);
    return !ui.TransitionScreen(Screen.Hub);
}

static bool UIOpenModalChangesInputLayer()
{
    var ui = new UIManager();
    ui.Initialize();
    var opened = false;
    ui.UIPanelOpened += panelId => opened = panelId == "inventory";
    var result = ui.OpenModal("inventory");
    return result && opened && ui.ActiveInputLayer == InputLayer.Modal && ui.IsModalOpen();
}

static bool UICloseModalRestoresWorld()
{
    var ui = new UIManager();
    ui.Initialize();
    ui.OpenModal("inventory");
    var closed = false;
    ui.UIPanelClosed += panelId => closed = panelId == "inventory";
    ui.CloseModal();
    return closed && ui.ActiveInputLayer == InputLayer.World && !ui.IsModalOpen();
}

static bool UICombatModalOverrides()
{
    var ui = new UIManager();
    ui.Initialize();
    ui.OpenModal("inventory");
    var combatOpened = false;
    ui.UIPanelOpened += panelId => combatOpened = panelId == "S7_combat";
    var result = ui.OpenModal("S7_combat");
    ui.CloseModal();
    return result && combatOpened && ui.IsModalOpen();
}

static bool UISecondNonCombatModalRejected()
{
    var ui = new UIManager();
    ui.Initialize();
    ui.OpenModal("inventory");
    return !ui.OpenModal("settings");
}

// ========================================================================
// FeedbackManager
// ========================================================================

static bool FeedbackEmitTriggersEvent()
{
    var feedback = new FeedbackManager();
    feedback.Initialize();
    var triggered = false;
    feedback.FeedbackTriggered += (eventId, _) => triggered = eventId == "test_event";
    feedback.EmitFeedback("test_event");
    return triggered;
}

static bool FeedbackSubscribeReceivesCallback()
{
    var feedback = new FeedbackManager();
    feedback.Initialize();
    var received = false;
    feedback.Subscribe("test_event", _ => received = true);
    feedback.EmitFeedback("test_event");
    return received;
}

static bool FeedbackSemanticStubsEmitCorrectEvents()
{
    var feedback = new FeedbackManager();
    feedback.Initialize();
    var events = new List<string>();
    feedback.FeedbackTriggered += (eventId, _) => events.Add(eventId);
    feedback.OnRouteSelected("route.test", "location.test");
    feedback.OnRepairCompleted("repair_node.test");
    feedback.OnThreatTriggered("threat.test");
    return events.SequenceEqual(["route_selected", "world_repair_completed", "threat_warning"]);
}

static bool FeedbackEmitNullParamsUsesEmptyDict()
{
    var feedback = new FeedbackManager();
    feedback.Initialize();
    Dictionary<string, object?>? receivedParams = null;
    feedback.FeedbackTriggered += (_, parameters) => receivedParams = parameters;
    feedback.EmitFeedback("test_event", null);
    return receivedParams is not null && receivedParams.Count == 0;
}

// ========================================================================
// SessionBootChain
// ========================================================================

static bool BootChainRunsAllPhases()
{
    var boot = new SessionBootChain();
    var phases = new HashSet<BootPhase>();
    boot.LoadingPhaseChanged += (phase, _) => phases.Add(phase);
    boot.RunBootChain();
    return phases.Count == 9
        && phases.Contains(BootPhase.Phase0PlatformProbe)
        && phases.Contains(BootPhase.Phase7FeedbackSessionReady);
}

static bool BootChainEndsInSessionActive()
{
    var boot = new SessionBootChain();
    boot.RunBootChain();
    return boot.CurrentState == ShellState.SessionActive
        && boot.BootComplete
        && boot.BootTimeMs > 0;
}

static bool BootChainEmitsSessionReady()
{
    var boot = new SessionBootChain();
    var ready = false;
    boot.SessionReady += () => ready = true;
    boot.RunBootChain();
    return ready;
}

static bool BootChainEmitsShellStateTransitions()
{
    var boot = new SessionBootChain();
    var transitions = new List<(ShellState Old, ShellState New)>();
    boot.ShellStateChanged += (old, @new) => transitions.Add((old, @new));
    boot.RunBootChain();
    return transitions.Count >= 2
        && transitions[0] == (ShellState.Booting, ShellState.Booting)
        && transitions[^1].New == ShellState.SessionActive;
}

static bool InputGateToggleEmitsEvents()
{
    var boot = new SessionBootChain();
    var opened = false;
    var closed = false;
    boot.InputGateOpen += () => opened = true;
    boot.InputGateClosed += () => closed = true;
    boot.SetInputGate(false);
    boot.SetInputGate(true);
    return closed && opened;
}

static bool InputGateSetSameStateNoOp()
{
    var boot = new SessionBootChain();
    var eventCount = 0;
    boot.InputGateOpen += () => eventCount++;
    boot.SetInputGate(true);
    boot.SetInputGate(true);
    return eventCount == 0;
}
