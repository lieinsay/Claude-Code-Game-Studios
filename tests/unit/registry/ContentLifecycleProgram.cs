using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 003: Registry Content Lifecycle — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: lifecycle states are distinguishable from missing IDs", Ac1LifecycleStatesAreDistinguishable);
Run("AC-2: retired stable IDs cannot be reused", Ac2RetiredIdReuseRejected);
Run("AC-3: fantasy-critical ID redefinition is blocked", Ac3FantasyCriticalRedefinitionBlocked);
Run("AC-4: runtime state references stable IDs without replacing static definitions", Ac4RuntimeStateUsesStableReferences);
Run("AC-5: deprecated and retired IDs resolve migration hints", Ac5LegacyIdsResolveMigrationHints);
Run("AC-6: lifecycle status and migration hint are available for reference validation", Ac6LifecycleInfoSupportsReferenceValidation);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 003 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 003 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1LifecycleStatesAreDistinguishable()
{
    var registry = new Registry();
    registry.InitializeContent();
    registry.RegisterContent("resource.draft-cloth", ValidResource("resource.draft-cloth", ContentStatus.Draft));
    registry.RegisterContent("resource.active-iron", ValidResource("resource.active-iron", ContentStatus.Active));
    registry.RegisterContent("resource.old-copper", ValidResource("resource.old-copper", ContentStatus.Active));
    registry.RegisterContent("resource.old-tin", ValidResource("resource.old-tin", ContentStatus.Active));
    registry.SetDomainLoaded("resources");

    var events = new List<(string Id, ContentStatus OldStatus, ContentStatus NewStatus, ContentStatus? ObservedStatus)>();
    registry.ContentStatusChanged += change =>
    {
        var observed = registry.ResolveContentLifecycle(change.ContentId).Status;
        events.Add((change.ContentId, change.OldStatus, change.NewStatus, observed));
    };

    var deprecated = registry.ChangeContentStatus(
        "resource.old-copper",
        ContentStatus.Deprecated,
        migrationTargetId: "resource.active-iron",
        migrationNote: "Use the active iron resource.");
    var deprecatedAgain = registry.QueryById("resource.old-copper");

    registry.ChangeContentStatus("resource.old-tin", ContentStatus.Deprecated);
    var retired = registry.ChangeContentStatus(
        "resource.old-tin",
        ContentStatus.Retired,
        migrationTargetId: "resource.active-iron",
        migrationNote: "Tin merged into iron.",
        retiredDate: "2026-05-10");
    var retiredAgain = registry.QueryById("resource.old-tin");
    var missing = registry.QueryById("resource.missing");

    return deprecated.Success
        && retired.Success
        && registry.QueryById("resource.draft-cloth").Status == RegistryQueryStatus.Found
        && registry.QueryById("resource.active-iron").Status == RegistryQueryStatus.Found
        && deprecatedAgain.Status == RegistryQueryStatus.Deprecated
        && retiredAgain.Status == RegistryQueryStatus.Retired
        && missing.Status == RegistryQueryStatus.NotFound
        && events.SequenceEqual([
            ("resource.old-copper", ContentStatus.Active, ContentStatus.Deprecated, ContentStatus.Deprecated),
            ("resource.old-tin", ContentStatus.Active, ContentStatus.Deprecated, ContentStatus.Deprecated),
            ("resource.old-tin", ContentStatus.Deprecated, ContentStatus.Retired, ContentStatus.Retired),
        ]);
}

static bool Ac2RetiredIdReuseRejected()
{
    var registry = new Registry();
    registry.InitializeContent();
    registry.RegisterContent("route.old-passage", ValidRoute("route.old-passage"));
    registry.ChangeContentStatus("route.old-passage", ContentStatus.Deprecated);
    registry.ChangeContentStatus("route.old-passage", ContentStatus.Retired, migrationTargetId: "route.new-passage");

    var reuse = registry.RegisterBatch([ValidRoute("route.old-passage")]);

    return !reuse.Success
        && reuse.ErrorCode == "ERR_ID_REUSE"
        && registry.QueryById("route.old-passage").Status == RegistryQueryStatus.Retired;
}

static bool Ac3FantasyCriticalRedefinitionBlocked()
{
    var registry = new Registry();
    registry.RegisterContent("location.glass-harbor", ValidLocation("location.glass-harbor", "starter-sea"));

    var changedKind = ValidRepairNode("location.glass-harbor");
    changedKind["kind"] = "repair-node";

    var changedRegion = ValidLocation("location.glass-harbor", "storm-belt");

    var kindResult = registry.RegisterBatch([changedKind]);
    var regionResult = registry.RegisterBatch([changedRegion]);

    return !kindResult.Success
        && kindResult.ErrorCode == "ERR_ID_REDEFINITION"
        && !regionResult.Success
        && regionResult.ErrorCode == "ERR_ID_REDEFINITION";
}

static bool Ac4RuntimeStateUsesStableReferences()
{
    var registry = new Registry();
    registry.InitializeContent();
    registry.RegisterContent("home-space.cargo-bay", ValidHomeSpace("home-space.cargo-bay"));

    var runtimeState = new Dictionary<string, object?>
    {
        ["id"] = "home-space.cargo-bay",
        ["upgrade_level"] = 2,
        ["module_state"] = "expanded",
    };

    var write = registry.SetEntity("home-space.cargo-bay", runtimeState);
    var query = registry.QueryById("home-space.cargo-bay");

    return registry.IsStableRuntimeReference("home-space.cargo-bay", "home-space")
        && !registry.IsStableRuntimeReference("home-space.renamed-cargo-bay", "home-space")
        && !write.Success
        && write.ErrorCode == "ERR_READONLY_REGISTRY"
        && query.Entity is not null
        && !query.Entity.ContainsKey("upgrade_level")
        && Convert.ToString(query.Entity["id"]) == "home-space.cargo-bay";
}

static bool Ac5LegacyIdsResolveMigrationHints()
{
    var registry = new Registry();
    registry.RegisterContent("resource.old-iron", ValidResource("resource.old-iron", ContentStatus.Active));
    registry.RegisterContent("resource.old-copper", ValidResource("resource.old-copper", ContentStatus.Active));

    registry.ChangeContentStatus(
        "resource.old-iron",
        ContentStatus.Deprecated,
        migrationTargetId: "resource.iron-ore",
        migrationNote: "Renamed during content cleanup.");

    registry.ChangeContentStatus("resource.old-copper", ContentStatus.Deprecated);
    registry.ChangeContentStatus(
        "resource.old-copper",
        ContentStatus.Retired,
        migrationTargetId: null,
        migrationNote: "No direct replacement.",
        retiredDate: "2026-05-10");

    var deprecated = registry.ResolveLegacyId("resource.old-iron");
    var retired = registry.ResolveLegacyId("resource.old-copper");

    return deprecated is not null
        && deprecated.Status == ContentStatus.Deprecated
        && deprecated.SuggestedReplacementId == "resource.iron-ore"
        && retired is not null
        && retired.Status == ContentStatus.Retired
        && retired.SuggestedReplacementId is null
        && retired.MigrationNote == "No direct replacement.";
}

static bool Ac6LifecycleInfoSupportsReferenceValidation()
{
    var registry = new Registry();
    registry.RegisterContent("resource.deprecated-thread", ValidResource("resource.deprecated-thread", ContentStatus.Active));
    registry.RegisterContent("resource.retired-thread", ValidResource("resource.retired-thread", ContentStatus.Active));

    registry.ChangeContentStatus(
        "resource.deprecated-thread",
        ContentStatus.Deprecated,
        migrationTargetId: "resource.thread",
        migrationNote: "Use the normalized thread ID.");
    registry.ChangeContentStatus("resource.retired-thread", ContentStatus.Deprecated);
    registry.ChangeContentStatus("resource.retired-thread", ContentStatus.Retired, migrationTargetId: "resource.thread");

    var deprecated = registry.ResolveContentLifecycle("resource.deprecated-thread");
    var retired = registry.ResolveContentLifecycle("resource.retired-thread");

    return deprecated.IsKnown
        && deprecated.Status == ContentStatus.Deprecated
        && deprecated.MigrationHint?.SuggestedReplacementId == "resource.thread"
        && retired.IsKnown
        && retired.Status == ContentStatus.Retired
        && retired.MigrationHint?.SuggestedReplacementId == "resource.thread";
}

static Dictionary<string, object?> ValidResource(string id, ContentStatus status)
{
    var definition = BaseDefinition(id, "resource", "resources", status);
    definition["unit"] = "chunk";
    definition["stack_rule"] = "stackable";
    definition["material_tags"] = new[] { "metal", "repair-material" };
    return definition;
}

static Dictionary<string, object?> ValidRoute(string id)
{
    var definition = BaseDefinition(id, "route", "routes", ContentStatus.Active);
    definition["origin_location_id"] = "location.glass-harbor";
    definition["destination_id"] = "location.sky-reef-outpost";
    definition["distance_band"] = "short";
    definition["hazard_tags"] = new[] { "safe" };
    return definition;
}

static Dictionary<string, object?> ValidLocation(string id, string regionTag)
{
    var definition = BaseDefinition(id, "location", "world", ContentStatus.Active);
    definition["region_tag"] = regionTag;
    definition["location_kind"] = "harbor";
    definition["service_tags"] = new[] { "market", "repair" };
    definition["local_identity_tags"] = new[] { "glass-buoys" };
    definition["settlement_need_tags"] = new[] { "navigation-aid", "trade-link" };
    return definition;
}

static Dictionary<string, object?> ValidRepairNode(string id)
{
    var definition = BaseDefinition(id, "repair-node", "world", ContentStatus.Active);
    definition["location_id"] = "location.glass-harbor";
    definition["node_kind"] = "beacon";
    definition["restoration_theme"] = "lighthouse";
    definition["settlement_need_tags"] = new[] { "navigation-aid", "safety" };
    definition["repair_visible_state_tags"] = new[] { "dark", "lit", "connected" };
    return definition;
}

static Dictionary<string, object?> ValidHomeSpace(string id)
{
    var definition = BaseDefinition(id, "home-space", "airship", ContentStatus.Active);
    definition["space_kind"] = "cargo";
    definition["home_function_tags"] = new[] { "storage" };
    definition["access_tags"] = new[] { "default" };
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
