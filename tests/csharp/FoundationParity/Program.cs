using CloudWeaverVoyage.Core;

var checks = new List<(string Label, Func<bool> Check)>
{
    ("empty package is invalid", EmptyPackageIsInvalid),
    ("ready package is valid", ReadyPackageIsValid),
    ("blocked package is invalid", BlockedPackageIsInvalid),
    ("snapshot dictionary preserves stable keys", SnapshotDictionaryPreservesStableKeys),
    ("snapshot roundtrip preserves payload", SnapshotRoundtripPreservesPayload),
    ("registry query by ID finds bootstrap entity", RegistryQueryByIdFindsBootstrapEntity),
    ("registry query by ID returns not found for loaded kind", RegistryQueryByIdReturnsNotFoundForLoadedKind),
    ("registry query before initialization returns unloaded", RegistryQueryBeforeInitializationReturnsUnloaded),
    ("registry query returns unloaded for unloaded kind", RegistryQueryReturnsUnloadedForUnloadedKind),
    ("registry list by kind returns deterministic sort", RegistryListByKindReturnsDeterministicSort),
    ("registry query returns deprecated status", RegistryQueryReturnsDeprecatedStatus),
    ("registry tracks loaded domains", RegistryTracksLoadedDomains),
    ("registry batch rejects duplicate IDs atomically", RegistryBatchRejectsDuplicateIdsAtomically),
    ("registry batch rejects invalid ID format", RegistryBatchRejectsInvalidIdFormat),
    ("persistence canonical JSON sorts keys", PersistenceCanonicalJsonSortsKeys),
    ("persistence checksum is deterministic", PersistenceChecksumIsDeterministic),
    ("persistence save promotes generation", PersistenceSavePromotesGeneration),
    ("persistence load restores saved snapshot", PersistenceLoadRestoresSavedSnapshot),
    ("persistence load without safe data fails", PersistenceLoadWithoutSafeDataFails),
    ("persistence invalid snapshot preserves old generation", PersistenceInvalidSnapshotPreservesOldGeneration),
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
        "domain_id",
        "snapshot_schema_version",
        "content_domain_versions",
        "stable_id_refs",
        "payload",
        "domain_state",
        "domain_error_code",
        "migration_hint",
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
        "resource.repair_kit",
        "resource.basic_supply",
        "resource.cloud_coin",
        "resource.ancient_lens",
        "resource.navigation_chart",
        "resource.beacon_crystal",
    ]);
}

static bool RegistryQueryReturnsDeprecatedStatus()
{
    var registry = MakeInitializedRegistry();
    registry.RegisterContent("test.deprecated_item", new Dictionary<string, object?>
    {
        ["id"] = "test.deprecated_item",
        ["kind"] = "test",
        ["content_status"] = ContentStatus.Deprecated,
        ["sort_order"] = 1,
    });

    var result = registry.QueryById("test.deprecated_item");
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
        new Dictionary<string, object?>
        {
            ["id"] = "resource.iron-ore",
            ["kind"] = "resource",
        },
        new Dictionary<string, object?>
        {
            ["id"] = "resource.iron-ore",
            ["kind"] = "resource",
        },
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
        new Dictionary<string, object?>
        {
            ["id"] = "Resource.Iron-Ore",
            ["kind"] = "resource",
        },
    ]);

    return !result.Success
        && result.ErrorCode == "ERR_INVALID_ID_FORMAT";
}

static Registry MakeInitializedRegistry()
{
    var registry = new Registry();
    registry.InitializeContent();
    return registry;
}

static bool PersistenceCanonicalJsonSortsKeys()
{
    var encoded = Persistence.CanonicalJsonEncode(new Dictionary<string, object?>
    {
        ["z"] = 1,
        ["a"] = new Dictionary<string, object?>
        {
            ["b"] = -0.0,
            ["a"] = double.NaN,
        },
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

    return !result.Success
        && result.Reason == "no_safe_data"
        && failed;
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
