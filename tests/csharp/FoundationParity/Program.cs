using CloudWeaverVoyage.Core;

var checks = new List<(string Label, Func<bool> Check)>
{
    ("empty package is invalid", EmptyPackageIsInvalid),
    ("ready package is valid", ReadyPackageIsValid),
    ("blocked package is invalid", BlockedPackageIsInvalid),
    ("snapshot dictionary preserves stable keys", SnapshotDictionaryPreservesStableKeys),
    ("snapshot roundtrip preserves payload", SnapshotRoundtripPreservesPayload),
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
