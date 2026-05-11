using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 004: Registry Reference Integrity — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: valid required and optional references pass", Ac1ValidReferencesPass);
Run("AC-2: missing, illegal status, and cycle diagnostics include chains", Ac2InvalidReferencesReportConcreteChains);
Run("AC-3: missing required references block Active registration", Ac3MissingRequiredReferenceBlocksActiveRegistration);
Run("AC-4: Active content cannot reference Draft content", Ac4ActiveToDraftReferenceRejected);
Run("AC-5: self-loop and closed cycles return complete cycle chain", Ac5CyclesReportCompleteChain);
Run("AC-6: unloaded reference does not trigger implicit loading", Ac6UnloadedReferenceReported);
Run("AC-7: insufficient query conditions return ambiguous query", Ac7AmbiguousQueryDoesNotChooseFirstMatch);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 004 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 004 AC validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
    total++;
    if (test())
    {
        Console.WriteLine($"[PASS] {label}");
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

static bool Ac1ValidReferencesPass()
{
    var registry = new Registry();
    registry.RegisterContent("location.glass-harbor", ValidLocation("location.glass-harbor", ContentStatus.Active));
    registry.RegisterContent("location.starlight-dock", ValidLocation("location.starlight-dock", ContentStatus.Active));

    var route = ValidRoute(
        "route.sky-reef-arc-01",
        [
            "location.glass-harbor",
            "location.starlight-dock",
            new Dictionary<string, object?> { ["id"] = "location.optional-cove", ["optional"] = true },
        ]);

    var validation = registry.ValidateReferences(route);

    return validation.Valid
        && validation.ReferenceValidity
        && validation.Diagnostics.Count == 0;
}

static bool Ac2InvalidReferencesReportConcreteChains()
{
    var registry = new Registry();
    registry.RegisterContent("location.glass-harbor", ValidLocation("location.glass-harbor", ContentStatus.Draft));

    var missingRoute = ValidRoute("route.missing-link", ["location.missing-port"]);
    var draftRoute = ValidRoute("route.draft-link", ["location.glass-harbor"]);
    var cycle = CycleRegistry();

    var missing = registry.ValidateReferences(missingRoute);
    var draft = registry.ValidateReferences(draftRoute);
    var cycleResult = cycle.ValidateReferences(cycle.QueryById("route.a").Entity!);

    return HasDiagnostic(missing, "ERR_MISSING_REFERENCE", "route.missing-link", "location.missing-port")
        && HasDiagnostic(draft, "ERR_REFERENCE_TO_DRAFT", "route.draft-link", "location.glass-harbor")
        && HasDiagnostic(cycleResult, "ERR_REFERENCE_CYCLE", "route.a", "route.a")
        && ChainContains(missing.Diagnostics.Single(), "route.missing-link", "location.missing-port")
        && ChainContains(draft.Diagnostics.Single(), "route.draft-link", "location.glass-harbor")
        && ChainContains(cycleResult.Diagnostics.Single(), "route.a", "route.b", "route.c", "route.a")
        && ChainStatusesContain(draft.Diagnostics.Single(), "route.draft-link(Active)", "location.glass-harbor(Draft)");
}

static bool Ac3MissingRequiredReferenceBlocksActiveRegistration()
{
    var registry = new Registry();
    registry.InitializeContent();

    var route = ValidRoute("route.sky-reef-arc-02", ["location.missing-port"]);
    var result = registry.RegisterBatch([route]);
    var query = registry.QueryById("route.sky-reef-arc-02");

    return !result.Success
        && result.ErrorCode == "ERR_MISSING_REFERENCE"
        && query.Status == RegistryQueryStatus.NotFound;
}

static bool Ac4ActiveToDraftReferenceRejected()
{
    var registry = new Registry();
    registry.RegisterContent("location.unpublished-cove", ValidLocation("location.unpublished-cove", ContentStatus.Draft));

    var route = ValidRoute("route.to-draft", ["location.unpublished-cove"]);
    var validation = registry.ValidateReferences(route);

    return !validation.Valid
        && validation.Diagnostics.Single().ErrorCode == "ERR_REFERENCE_TO_DRAFT"
        && ChainContains(validation.Diagnostics.Single(), "route.to-draft", "location.unpublished-cove")
        && ChainStatusesContain(validation.Diagnostics.Single(), "route.to-draft(Active)", "location.unpublished-cove(Draft)");
}

static bool Ac5CyclesReportCompleteChain()
{
    var self = new Registry();
    self.RegisterContent("route.self", ValidRoute("route.self", ["route.self"], ContentStatus.Draft));
    self.InitializeContent();

    var closed = CycleRegistry();
    var selfResult = self.ValidateReferences(self.QueryById("route.self").Entity!);
    var closedResult = closed.ValidateReferences(closed.QueryById("route.a").Entity!);

    return HasDiagnostic(selfResult, "ERR_REFERENCE_CYCLE", "route.self", "route.self")
        && ChainContains(selfResult.Diagnostics.Single(), "route.self", "route.self")
        && ChainStatusesContain(selfResult.Diagnostics.Single(), "route.self(Draft)", "route.self(Draft)")
        && HasDiagnostic(closedResult, "ERR_REFERENCE_CYCLE", "route.a", "route.a")
        && ChainContains(closedResult.Diagnostics.Single(), "route.a", "route.b", "route.c", "route.a")
        && ChainStatusesContain(
            closedResult.Diagnostics.Single(),
            "route.a(Draft)",
            "route.b(Draft)",
            "route.c(Draft)",
            "route.a(Draft)");
}

static bool Ac6UnloadedReferenceReported()
{
    var registry = new Registry();
    var route = ValidRoute("route.needs-unloaded-location", ["location.unloaded-harbor"]);

    var validation = registry.ValidateReferences(route);
    var queryAfter = registry.QueryById("location.unloaded-harbor");

    return !validation.Valid
        && validation.Diagnostics.Single().ErrorCode == "UNLOADED_REFERENCE"
        && queryAfter.Status == RegistryQueryStatus.Unloaded;
}

static bool Ac7AmbiguousQueryDoesNotChooseFirstMatch()
{
    var registry = new Registry();
    registry.RegisterContent("resource.scrap-a", ValidResource("resource.scrap-a", ["metal", "salvage"]));
    registry.RegisterContent("resource.scrap-b", ValidResource("resource.scrap-b", ["metal", "repair-material"]));
    registry.RegisterContent("resource.herb", ValidResource("resource.herb", ["organic"]));

    var ambiguous = registry.QueryUniqueByTags(["metal"], kind: "resource");
    var missing = registry.QueryUniqueByTags(["crystal"], kind: "resource");
    var unique = registry.QueryUniqueByTags(["organic"], kind: "resource");

    return ambiguous.Status == RegistryUniqueQueryStatus.AmbiguousQuery
        && ambiguous.ErrorCode == "AMBIGUOUS_QUERY"
        && ambiguous.Entity is null
        && ambiguous.MatchedIds.SequenceEqual(["resource.scrap-a", "resource.scrap-b"])
        && missing.Status == RegistryUniqueQueryStatus.NotFound
        && missing.ErrorCode == "NOT_FOUND"
        && unique.Status == RegistryUniqueQueryStatus.Found
        && Convert.ToString(unique.Entity?["id"]) == "resource.herb";
}

static Registry CycleRegistry()
{
    var registry = new Registry();
    registry.RegisterContent("route.a", ValidRoute("route.a", ["route.b"], ContentStatus.Draft));
    registry.RegisterContent("route.b", ValidRoute("route.b", ["route.c"], ContentStatus.Draft));
    registry.RegisterContent("route.c", ValidRoute("route.c", ["route.a"], ContentStatus.Draft));
    registry.InitializeContent();
    return registry;
}

static bool HasDiagnostic(
    RegistryReferenceValidationResult result,
    string errorCode,
    string sourceId,
    string targetId)
{
    return !result.Valid
        && result.Diagnostics.Any(diagnostic =>
            diagnostic.ErrorCode == errorCode
            && diagnostic.SourceId == sourceId
            && diagnostic.TargetId == targetId);
}

static bool ChainContains(RegistryReferenceDiagnostic diagnostic, params string[] ids)
{
    var chain = diagnostic.ReferenceChain.Select(item => item.Split('(')[0]).ToArray();
    if (chain.Length < ids.Length)
    {
        return false;
    }

    for (var index = 0; index < ids.Length; index++)
    {
        if (!string.Equals(chain[index], ids[index], StringComparison.Ordinal))
        {
            return false;
        }
    }

    return true;
}

static bool ChainStatusesContain(RegistryReferenceDiagnostic diagnostic, params string[] expected)
{
    return diagnostic.ReferenceChain.SequenceEqual(expected, StringComparer.Ordinal);
}

static Dictionary<string, object?> ValidRoute(string id, object[] references, ContentStatus status = ContentStatus.Active)
{
    var definition = BaseDefinition(id, "route", "routes", status);
    definition["origin_location_id"] = "location.glass-harbor";
    definition["destination_id"] = "location.starlight-dock";
    definition["distance_band"] = "short";
    definition["hazard_tags"] = new[] { "safe" };
    definition["references"] = references;
    return definition;
}

static Dictionary<string, object?> ValidLocation(string id, ContentStatus status)
{
    var definition = BaseDefinition(id, "location", "world", status);
    definition["region_tag"] = "starter-sea";
    definition["location_kind"] = "harbor";
    definition["service_tags"] = new[] { "market", "repair" };
    definition["local_identity_tags"] = new[] { "glass-buoys" };
    definition["settlement_need_tags"] = new[] { "navigation-aid", "trade-link" };
    return definition;
}

static Dictionary<string, object?> ValidResource(string id, string[] tags)
{
    var definition = BaseDefinition(id, "resource", "resources", ContentStatus.Active);
    definition["unit"] = "chunk";
    definition["stack_rule"] = "stackable";
    definition["material_tags"] = tags;
    definition["tags"] = tags;
    return definition;
}

static Dictionary<string, object?> BaseDefinition(string id, string kind, string ownerDomain, ContentStatus status)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = kind,
        ["owner_domain"] = ownerDomain,
        ["status"] = status.ToString(),
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 10,
        ["references"] = Array.Empty<string>(),
    };
}
