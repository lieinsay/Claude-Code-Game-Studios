using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 005: Registry Domain Loading & Decision UI Gating — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: chart decisions are blocked by incomplete domains", Ac1ChartBlockedByIncompleteDomain);
Run("AC-2: hub decisions are blocked by incomplete domains", Ac2HubBlockedByIncompleteDomain);
Run("AC-3: repair and market decisions are blocked by incomplete domains", Ac3RepairMarketBlockedByIncompleteDomain);
Run("AC-4: decision snapshots isolate status and content ordering", Ac4SnapshotIsolation);
Run("AC-5: incompatible content packages return VERSION_INCOMPATIBLE", Ac5VersionIncompatible);
Run("AC-6: desktop boundary receives copyable fatal diagnostics", Ac6FatalBoundaryDiagnostic);
Run("Signal: domain_ready fires synchronously with typed values", DomainReadySignalFiresSynchronously);
Run("Regression: stale loaded-domain flag is cleared on reload failure", StaleLoadedDomainFlagCleared);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 005 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 005 AC validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
    total++;
    try
    {
        if (test())
        {
            Console.WriteLine($"[PASS] {label}");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
        failed++;
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

static bool Ac1ChartBlockedByIncompleteDomain()
{
    var registry = new Registry();
    registry.SetDomainStatus("routes", DomainStatus.Complete);
    registry.SetDomainStatus("world", DomainStatus.Complete);
    registry.SetDomainStatus("intel", DomainStatus.Complete);
    registry.SetDomainStatus("threats", DomainStatus.Loading);

    var blocked = registry.CheckDecisionSurfaceReady(DecisionSurface.Chart);
    registry.SetDomainStatus("threats", DomainStatus.Complete);
    var ready = registry.CheckDecisionSurfaceReady(DecisionSurface.Chart);

    return !blocked.Ready
        && blocked.BlockedDomains.SequenceEqual(["threats"])
        && blocked.DomainStatuses["threats"] == DomainStatus.Loading
        && ready.Ready
        && ready.BlockedDomains.Count == 0;
}

static bool Ac2HubBlockedByIncompleteDomain()
{
    var registry = new Registry();
    registry.SetDomainStatus("airship", DomainStatus.Complete);
    registry.SetDomainStatus("resources", DomainStatus.Complete);
    registry.SetDomainStatus("companions", DomainStatus.Partial);

    var blocked = registry.CheckDecisionSurfaceReady(DecisionSurface.Hub);

    return !blocked.Ready
        && blocked.BlockedDomains.SequenceEqual(["companions"])
        && blocked.DomainStatuses["companions"] == DomainStatus.Partial;
}

static bool Ac3RepairMarketBlockedByIncompleteDomain()
{
    var registry = new Registry();
    registry.SetDomainStatus("world", DomainStatus.Complete);
    registry.SetDomainStatus("resources", DomainStatus.Failed);
    registry.SetDomainStatus("intel", DomainStatus.Complete);

    var blocked = registry.CheckDecisionSurfaceReady(DecisionSurface.RepairMarket);

    return !blocked.Ready
        && blocked.BlockedDomains.SequenceEqual(["resources"])
        && blocked.DomainStatuses["resources"] == DomainStatus.Failed;
}

static bool Ac4SnapshotIsolation()
{
    var registry = new Registry();
    registry.RegisterContent("threat.alpha", ValidThreat("threat.alpha", 10));
    registry.SetDomainStatus("threats", DomainStatus.Loading);

    var handle = registry.TakeSnapshot(["threats"]);
    registry.SetDomainStatus("threats", DomainStatus.Complete);
    registry.RegisterContent("threat.beta", ValidThreat("threat.beta", 5));

    var snapshotThreats = registry.ListByDomain("threats", handle);
    var snapshotThreatStatus = registry.GetDomainStatus("threats", handle);
    var liveThreats = registry.ListByDomain("threats");
    var released = registry.ReleaseSnapshot(handle);
    var afterRelease = registry.ListByDomain("threats", handle);

    return snapshotThreatStatus == DomainStatus.Loading
        && registry.GetDomainStatus("threats") == DomainStatus.Complete
        && snapshotThreats.Select(Id).SequenceEqual(["threat.alpha"])
        && liveThreats.Select(Id).SequenceEqual(["threat.beta", "threat.alpha"])
        && released
        && afterRelease.Count == 0;
}

static bool Ac5VersionIncompatible()
{
    var registry = new Registry();
    registry.SetDomainStatus("routes", DomainStatus.Complete);

    var result = registry.LoadContentPackage(new RegistryContentPackage(
        "threats",
        SchemaVersion: 3,
        Definitions: [ValidThreat("threat.incompatible", 1)]));

    return !result.Success
        && result.Status == ContentPackageLoadStatus.VersionIncompatible
        && result.ErrorCode == "VERSION_INCOMPATIBLE"
        && registry.GetDomainStatus("threats") == DomainStatus.Failed
        && registry.GetDomainStatus("intel") == DomainStatus.Unloaded
        && registry.GetDomainStatus("routes") == DomainStatus.Complete
        && registry.QueryById("threat.incompatible").Status == RegistryQueryStatus.Unloaded;
}

static bool Ac6FatalBoundaryDiagnostic()
{
    var registry = new Registry();
    var result = registry.LoadContentPackage(new RegistryContentPackage(
        "resources",
        SchemaVersion: 3,
        Definitions: [ValidResource("resource.future-ore")]));

    var shellCanStopAtContentBoundary = !result.Success
        && registry.GetDomainStatus("resources") == DomainStatus.Failed
        && result.Diagnostic.Contains("VERSION_INCOMPATIBLE", StringComparison.Ordinal)
        && result.Diagnostic.Contains("domain=resources", StringComparison.Ordinal)
        && result.Diagnostic.Contains("package_schema_version=3", StringComparison.Ordinal)
        && result.Diagnostic.Contains("supported_schema_version=1", StringComparison.Ordinal);

    return shellCanStopAtContentBoundary;
}

static bool DomainReadySignalFiresSynchronously()
{
    var registry = new Registry();
    var observed = new List<(string Domain, string Status, DomainStatus StateAtEmit)>();
    registry.DomainReady += (domain, status) =>
        observed.Add((domain, status, registry.GetDomainStatus(domain)));

    registry.SetDomainStatus("intel", DomainStatus.Loading);
    registry.SetDomainStatus("intel", DomainStatus.Complete);

    return observed.SequenceEqual([
        ("intel", "LOADING", DomainStatus.Loading),
        ("intel", "COMPLETE", DomainStatus.Complete),
    ]);
}

static bool StaleLoadedDomainFlagCleared()
{
    var registry = new Registry();
    registry.SetDomainLoaded("resources");
    var beforeReload = registry.IsDomainLoaded("resources");

    registry.SetDomainStatus("resources", DomainStatus.Loading);
    var duringReload = registry.IsDomainLoaded("resources");

    registry.SetDomainStatus("resources", DomainStatus.Failed);
    var afterFailure = registry.IsDomainLoaded("resources");

    return beforeReload && !duringReload && !afterFailure;
}

static string Id(IReadOnlyDictionary<string, object?> entity)
{
    return Convert.ToString(entity["id"]) ?? string.Empty;
}

static Dictionary<string, object?> ValidThreat(string id, int sortOrder)
{
    var definition = BaseDefinition(id, "threat", "threats", sortOrder);
    definition["threat_class"] = "guard";
    definition["encounter_tags"] = new[] { "raider" };
    definition["counter_tags"] = new[] { "evade", "retreat" };
    definition["severity_tier"] = "moderate";
    return definition;
}

static Dictionary<string, object?> ValidResource(string id)
{
    var definition = BaseDefinition(id, "resource", "resources", 10);
    definition["unit"] = "chunk";
    definition["stack_rule"] = "stackable";
    definition["material_tags"] = new[] { "metal", "repair-material" };
    return definition;
}

static Dictionary<string, object?> BaseDefinition(string id, string kind, string ownerDomain, int sortOrder)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = kind,
        ["owner_domain"] = ownerDomain,
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = sortOrder,
        ["references"] = Array.Empty<string>(),
    };
}
