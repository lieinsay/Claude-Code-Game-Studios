using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 008: Player-Facing Boundary — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: GetDisplayInfo never exposes ERR_* codes or diagnostic fields", Ac1DisplayInfoNoInternalErrors);
Run("AC-1: GetPlayerSafeError(RegistryQueryStatus) never returns ERR_* strings", Ac1PlayerSafeErrorQueryStatus);
Run("AC-1: GetPlayerSafeError(NotFound/Deprecated/Retired) returns null — silent suppression", Ac1PlayerSafeErrorSilentStatuses);
Run("AC-2: Hub stable-ID lookup returns display info for home-space kind", Ac2HubStableIdLookup);
Run("AC-2: Scene-path query returns null display info (NOT_FOUND)", Ac2ScenePathQueryReturnsNull);
Run("AC-3: Deprecated/retired content suppressed from GetDisplayInfo", Ac3DeprecatedRetiredSuppressed);
Run("AC-4: Failed domain surfaces player-safe message not ERR_* code", Ac4FailedDomainSafeError);
Run("AC-4: VERSION_INCOMPATIBLE maps to player-safe update message", Ac4VersionIncompatibleSafeError);
Run("AC-4: Unloaded domain surfaces loading message", Ac4UnloadedDomainLoadingMessage);
Run("Regression: GetDisplayInfo returns null for unknown ID", RegressionUnknownIdReturnsNull);
Run("Regression: GetDisplayInfo tags are sorted deterministically", RegressionTagsSortedDeterministically);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 008 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 008 AC validation passed: {total}/{total} checks passed.");
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

// ---------------------------------------------------------------------------
// AC-1: Player UI never shows internal errors
// ---------------------------------------------------------------------------

static bool Ac1DisplayInfoNoInternalErrors()
{
    // Arrange: registry with one active location
    var registry = new Registry();
    registry.RegisterContent("location.glass-harbor", ValidLocation("location.glass-harbor", 1));

    // Act: retrieve player-facing display info
    var info = registry.GetDisplayInfo("location.glass-harbor");

    // Assert: fields must not contain any ERR_* prefix strings, reference chains,
    // or any diagnostic vocabulary that should never appear in player UI.
    if (info is null)
    {
        return false;
    }

    var allTextFields = new[] { info.NameKey, info.DescriptionKey, info.IconRef ?? string.Empty };
    foreach (var field in allTextFields)
    {
        if (field.Contains("ERR_", StringComparison.Ordinal)) return false;
        if (field.Contains("reference_chain", StringComparison.Ordinal)) return false;
        if (field.Contains("diagnostic", StringComparison.Ordinal)) return false;
        if (field.Contains("stack_trace", StringComparison.Ordinal)) return false;
    }

    // Tags must not carry internal error codes either
    foreach (var tag in info.Tags)
    {
        if (tag.Contains("ERR_", StringComparison.Ordinal)) return false;
    }

    return true;
}

static bool Ac1PlayerSafeErrorQueryStatus()
{
    // All RegistryQueryStatus values must produce either null or a player-safe
    // message — never an ERR_* error code string.
    var statuses = Enum.GetValues<RegistryQueryStatus>();
    foreach (var status in statuses)
    {
        var message = Registry.GetPlayerSafeError(status);
        if (message is not null && message.Contains("ERR_", StringComparison.Ordinal))
        {
            return false;
        }
    }

    return true;
}

static bool Ac1PlayerSafeErrorSilentStatuses()
{
    // NotFound should return null — logic layer handles it; player UI should not show an error.
    // Deprecated and Retired are also silent — content is suppressed, not surfaced as an error.
    return Registry.GetPlayerSafeError(RegistryQueryStatus.NotFound) is null
        && Registry.GetPlayerSafeError(RegistryQueryStatus.Deprecated) is null
        && Registry.GetPlayerSafeError(RegistryQueryStatus.Retired) is null
        && Registry.GetPlayerSafeError(RegistryQueryStatus.Found) is null;
}

// ---------------------------------------------------------------------------
// AC-2: Hub references must use stable IDs
// ---------------------------------------------------------------------------

static bool Ac2HubStableIdLookup()
{
    // Arrange: register a home-space item — the kind that Hub panels render.
    // Hub DecisionSurface maps to domains ["airship", "resources", "companions"].
    // home-space kind belongs to the "airship" domain (see OwnerDomainForKind).
    var registry = new Registry();
    registry.RegisterContent("home-space.map-room", ValidHomeSpace("home-space.map-room", 1));

    // Act: look up the Hub item by its stable content ID
    var info = registry.GetDisplayInfo("home-space.map-room");

    // Assert: info exists and exposes only localization keys — never scene paths or display text
    return info is not null
        && !string.IsNullOrWhiteSpace(info.NameKey)
        && info.NameKey.StartsWith("content.", StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(info.DescriptionKey)
        && info.DescriptionKey.StartsWith("content.", StringComparison.Ordinal)
        && info.SortOrder >= 0;
}

static bool Ac2ScenePathQueryReturnsNull()
{
    // Arrange: registry with prototype content
    var registry = new Registry();
    registry.InitializeContent();

    // Act: attempt lookup using a scene path — must never resolve to content
    var infoByPath = registry.GetDisplayInfo("res://scenes/map_room.tscn");
    var infoByDisplayText = registry.GetDisplayInfo("Glass Harbor");
    var infoByLineIndex = registry.GetDisplayInfo("0");

    // Assert: none of the non-stable-ID lookups may return display info
    return infoByPath is null
        && infoByDisplayText is null
        && infoByLineIndex is null;
}

// ---------------------------------------------------------------------------
// AC-3: Deprecated/retired IDs are suppressed in GetDisplayInfo
// ---------------------------------------------------------------------------

static bool Ac3DeprecatedRetiredSuppressed()
{
    // Arrange
    var registry = new Registry();
    registry.RegisterContent("location.old-port", ValidLocation("location.old-port", 10));
    registry.RegisterContent("location.new-port", ValidLocation("location.new-port", 11));

    // Move old-port through lifecycle to deprecated
    var toDeprecated = registry.ChangeContentStatus(
        "location.old-port",
        ContentStatus.Deprecated,
        migrationTargetId: "location.new-port",
        migrationNote: "Renamed");

    if (!toDeprecated.Success) return false;

    // Act: GetDisplayInfo for deprecated ID
    var deprecatedInfo = registry.GetDisplayInfo("location.old-port");

    // Retire new-port via deprecated first
    registry.RegisterContent("location.final-port", ValidLocation("location.final-port", 12));
    var toRetiredDeprecated = registry.ChangeContentStatus(
        "location.final-port",
        ContentStatus.Deprecated,
        migrationTargetId: null);
    var toRetired = registry.ChangeContentStatus(
        "location.final-port",
        ContentStatus.Retired);

    if (!toRetiredDeprecated.Success || !toRetired.Success) return false;
    var retiredInfo = registry.GetDisplayInfo("location.final-port");

    // Assert: both suppressed — player UI must not render deprecated/retired content
    return deprecatedInfo is null && retiredInfo is null;
}

// ---------------------------------------------------------------------------
// AC-4: Player-safe error messages for corrupted/failed content
// ---------------------------------------------------------------------------

static bool Ac4FailedDomainSafeError()
{
    // Arrange: domain loading fails
    var registry = new Registry();
    registry.SetDomainStatus("world", DomainStatus.Failed);

    // Act
    var message = Registry.GetPlayerSafeError(DomainStatus.Failed);
    var domainStatus = registry.GetDomainStatus("world");

    // Assert: player message must not contain ERR_* codes and domain status is Failed
    return domainStatus == DomainStatus.Failed
        && message is not null
        && !message.Contains("ERR_", StringComparison.Ordinal)
        && !message.Contains("VERSION_INCOMPATIBLE", StringComparison.Ordinal)
        && message.Length > 0;
}

static bool Ac4VersionIncompatibleSafeError()
{
    // Arrange: load an incompatible content package
    var registry = new Registry();
    var result = registry.LoadContentPackage(new RegistryContentPackage(
        "world",
        SchemaVersion: 99,
        Definitions: [ValidLocation("location.future-port", 1)]));

    // Act: map query status
    var queryResult = registry.QueryById("location.future-port");
    var playerMessage = Registry.GetPlayerSafeError(RegistryQueryStatus.VersionIncompatible);

    // Assert: internal diagnostic stays internal; player message is safe
    return !result.Success
        && result.ErrorCode == "VERSION_INCOMPATIBLE"
        && result.Diagnostic.Contains("VERSION_INCOMPATIBLE", StringComparison.Ordinal)
        && playerMessage is not null
        && !playerMessage.Contains("VERSION_INCOMPATIBLE", StringComparison.Ordinal)
        && !playerMessage.Contains("ERR_", StringComparison.Ordinal);
}

static bool Ac4UnloadedDomainLoadingMessage()
{
    // Arrange: domain not yet loaded
    var registry = new Registry();

    // Act
    var unloadedMessage = Registry.GetPlayerSafeError(DomainStatus.Unloaded);
    var loadingMessage = Registry.GetPlayerSafeError(DomainStatus.Loading);
    var partialMessage = Registry.GetPlayerSafeError(DomainStatus.Partial);
    var completeMessage = Registry.GetPlayerSafeError(DomainStatus.Complete);

    // Assert: transient states yield "loading" message; complete yields null (no error)
    return unloadedMessage is not null
        && loadingMessage is not null
        && partialMessage is not null
        && completeMessage is null
        && !unloadedMessage.Contains("ERR_", StringComparison.Ordinal)
        && !loadingMessage.Contains("ERR_", StringComparison.Ordinal);
}

// ---------------------------------------------------------------------------
// Regression checks
// ---------------------------------------------------------------------------

static bool RegressionUnknownIdReturnsNull()
{
    var registry = new Registry();
    registry.InitializeContent();

    // Unknown IDs must not throw — they must return null gracefully
    return registry.GetDisplayInfo("location.nonexistent-place") is null
        && registry.GetDisplayInfo(string.Empty) is null
        && registry.GetDisplayInfo("not-a-valid-id-at-all") is null;
}

static bool RegressionTagsSortedDeterministically()
{
    // Arrange: two registries with same content registered in different order
    var registryA = new Registry();
    var registryB = new Registry();
    var defA = ValidLocationWithTags("location.test-alpha", 1, ["zebra", "apple", "mango"]);
    var defB = ValidLocationWithTags("location.test-alpha", 1, ["mango", "zebra", "apple"]);

    registryA.RegisterContent("location.test-alpha", defA);
    registryB.RegisterContent("location.test-alpha", defB);

    var infoA = registryA.GetDisplayInfo("location.test-alpha");
    var infoB = registryB.GetDisplayInfo("location.test-alpha");

    if (infoA is null || infoB is null) return false;

    // Assert: tag order must be identical regardless of insertion order
    return infoA.Tags.SequenceEqual(infoB.Tags, StringComparer.Ordinal);
}

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

static Dictionary<string, object?> ValidLocation(string id, int sortOrder)
{
    return ValidLocationWithTags(id, sortOrder, ["location"]);
}

static Dictionary<string, object?> ValidLocationWithTags(string id, int sortOrder, string[] tags)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "location",
        ["owner_domain"] = "world",
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = tags,
        ["sort_order"] = sortOrder,
        ["references"] = Array.Empty<string>(),
        ["region_tag"] = "starter-sea",
        ["location_kind"] = "settlement",
        ["service_tags"] = new[] { "market" },
        ["local_identity_tags"] = new[] { "test" },
        ["settlement_need_tags"] = new[] { "trade-link" },
    };
}

static Dictionary<string, object?> ValidHomeSpace(string id, int sortOrder)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "home-space",
        ["owner_domain"] = "airship",
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "home-space" },
        ["sort_order"] = sortOrder,
        ["references"] = Array.Empty<string>(),
        ["space_kind"] = "room",
        ["home_function_tags"] = new[] { "planning" },
        ["access_tags"] = new[] { "open" },
    };
}
