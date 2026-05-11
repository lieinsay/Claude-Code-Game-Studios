using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 006: Registry Diagnostic System — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: multiple errors select main by precedence and attach related errors", Ac1MainErrorPrecedence);
Run("AC-2: diagnostic event includes all required fields", Ac2RequiredFieldsPresent);
Run("AC-3: severity classification maps warning/error/fatal semantics", Ac3SeverityClassification);
Run("AC-3 edge: new Active references to Deprecated targets are errors", Ac3NewActiveDeprecatedReferenceIsError);
Run("AC-4: same severity and priority sort by code, content, field", Ac4StableOrdering);
Run("AC-5: 8-level precedence selects package version before schema errors", Ac5EightLevelPrecedence);
Run("Integration: validator diagnostics aggregate into one copyable event", ValidatorDiagnosticsAggregate);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 006 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 006 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1MainErrorPrecedence()
{
    var registry = new Registry();
    var route = ValidRoute("route.broken");
    var diagnostic = registry.GenerateDiagnostic(route, [
        Finding("ERR_REFERENCE_CYCLE", "route.broken", "references", ["route.broken", "route.loop", "route.broken"]),
        Finding("ERR_MISSING_REFERENCE", "route.broken", "destination_id", ["route.broken", "location.missing"]),
    ]);

    return diagnostic.ErrorCode == "ERR_MISSING_REFERENCE"
        && diagnostic.BlockingScope == "item"
        && diagnostic.RelatedErrors.Count == 1
        && diagnostic.RelatedErrors[0].ErrorCode == "ERR_REFERENCE_CYCLE"
        && diagnostic.RelatedErrors[0].ReferenceChain.SequenceEqual(["route.broken", "route.loop", "route.broken"]);
}

static bool Ac2RequiredFieldsPresent()
{
    var registry = new Registry();
    var route = ValidRoute("route.required-fields");
    var diagnostic = registry.GenerateDiagnostic(
        route,
        [Finding("ERR_MISSING_REFERENCE", "route.required-fields", "destination_id", ["route.required-fields", "location.missing"])],
        contentPackage: "test-pack",
        sourceRef: "content/routes.yaml:12",
        queryContext: "validate_all");

    return !string.IsNullOrWhiteSpace(diagnostic.EventId)
        && diagnostic.Timestamp > DateTimeOffset.UnixEpoch
        && diagnostic.Severity == "error"
        && diagnostic.ErrorCode == "ERR_MISSING_REFERENCE"
        && diagnostic.ContentId == "route.required-fields"
        && diagnostic.Kind == "route"
        && diagnostic.Status == "Active"
        && diagnostic.SchemaVersion == 1
        && diagnostic.OwnerDomain == "routes"
        && diagnostic.ContentPackage == "test-pack"
        && diagnostic.SourceRef == "content/routes.yaml:12"
        && diagnostic.FieldPath == "destination_id"
        && diagnostic.ReferenceChain.SequenceEqual(["route.required-fields", "location.missing"])
        && diagnostic.QueryContext == "validate_all"
        && diagnostic.BlockingScope == "item"
        && diagnostic.SuggestedAction.Contains("referenced content", StringComparison.Ordinal)
        && diagnostic.RelatedErrors.Count == 0;
}

static bool Ac3SeverityClassification()
{
    var registry = new Registry();
    var warning = registry.GenerateDiagnostic(
        ValidRoute("route.deprecated-compatible"),
        [Finding("ERR_REFERENCE_TO_DEPRECATED", "route.deprecated-compatible", "references", ["route.deprecated-compatible", "location.old"])]);
    var error = registry.GenerateDiagnostic(
        ValidRoute("route.bad"),
        [Finding("ERR_SCHEMA_INVALID", "route.bad", "kind")]);
    var fatal = registry.GenerateDiagnostic(
        ValidRoute("route.future"),
        [Finding("ERR_CONTENT_PACKAGE_VERSION", "route.future", "schema_version")]);

    return warning.Severity == "warning"
        && warning.BlockingScope == "item"
        && error.Severity == "error"
        && fatal.Severity == "fatal"
        && fatal.BlockingScope == "registry";
}

static bool Ac3NewActiveDeprecatedReferenceIsError()
{
    var registry = new Registry();
    registry.RegisterContent("location.old", ValidLocation("location.old", ContentStatus.Active));
    registry.ChangeContentStatus("location.old", ContentStatus.Deprecated, migrationTargetId: "location.new");

    var route = ValidRoute("route.new-active");
    route["references"] = new[] { "location.old" };
    var referenceValidation = registry.ValidateReferences(route);
    var diagnostic = registry.GenerateDiagnostic(
        route,
        Array.Empty<RegistryDiagnostic>(),
        referenceValidation.Diagnostics);

    return referenceValidation.Diagnostics.Any(error => error.ErrorCode == "ERR_REFERENCE_TO_DEPRECATED")
        && diagnostic.ErrorCode == "ERR_REFERENCE_TO_DEPRECATED"
        && diagnostic.Severity == "error";
}

static bool Ac4StableOrdering()
{
    var registry = new Registry();
    var events = new[]
    {
        registry.GenerateDiagnostic(ValidRoute("route.b"), [Finding("ERR_MISSING_REFERENCE", "route.b", "z_field")]),
        registry.GenerateDiagnostic(ValidRoute("route.a"), [Finding("UNLOADED_REFERENCE", "route.a", "a_field")]),
        registry.GenerateDiagnostic(ValidRoute("route.a"), [Finding("ERR_MISSING_REFERENCE", "route.a", "b_field")]),
        registry.GenerateDiagnostic(ValidRoute("route.a"), [Finding("ERR_MISSING_REFERENCE", "route.a", "a_field")]),
    };

    var sorted = Registry.SortDiagnostics(events)
        .Select(diagnostic => $"{diagnostic.ErrorCode}|{diagnostic.ContentId}|{diagnostic.FieldPath}")
        .ToArray();

    return sorted.SequenceEqual([
        "ERR_MISSING_REFERENCE|route.a|a_field",
        "ERR_MISSING_REFERENCE|route.a|b_field",
        "ERR_MISSING_REFERENCE|route.b|z_field",
        "UNLOADED_REFERENCE|route.a|a_field",
    ]);
}

static bool Ac5EightLevelPrecedence()
{
    var registry = new Registry();
    var diagnostic = registry.GenerateDiagnostic(ValidRoute("route.future"), [
        Finding("ERR_SCHEMA_INVALID", "route.future", "kind"),
        Finding("ERR_CONTENT_PACKAGE_VERSION", "route.future", "schema_version"),
    ]);

    return diagnostic.ErrorCode == "ERR_CONTENT_PACKAGE_VERSION"
        && diagnostic.Severity == "fatal"
        && diagnostic.RelatedErrors.Count == 1
        && diagnostic.RelatedErrors[0].ErrorCode == "ERR_SCHEMA_INVALID";
}

static bool ValidatorDiagnosticsAggregate()
{
    var registry = new Registry();
    var route = ValidRoute("route.aggregate");
    route.Remove("distance_band");
    route["durability"] = 10;
    route["references"] = new object[] { "location.missing", "route.aggregate" };

    var definition = registry.ValidateDefinition(route);
    var references = registry.ValidateReferences(route);
    var diagnostic = registry.GenerateDiagnostic(
        route,
        definition.Diagnostics,
        references.Diagnostics,
        contentPackage: "aggregate-pack");

    var relatedCodes = diagnostic.RelatedErrors
        .Select(error => error.ErrorCode)
        .ToHashSet(StringComparer.Ordinal);

    return diagnostic.ErrorCode == "ERR_SCHEMA_MISSING_REQUIRED_FIELD"
        && relatedCodes.Contains("ERR_RUNTIME_FIELD_IN_STATIC_DATA")
        && relatedCodes.Contains("UNLOADED_REFERENCE")
        && relatedCodes.Contains("ERR_REFERENCE_CYCLE")
        && diagnostic.ContentPackage == "aggregate-pack";
}

static RegistryDiagnosticFinding Finding(
    string errorCode,
    string contentId,
    string fieldPath,
    IReadOnlyList<string>? chain = null)
{
    return new RegistryDiagnosticFinding(
        errorCode,
        contentId,
        fieldPath,
        $"Test finding for {errorCode}.",
        chain ?? Array.Empty<string>(),
        new Dictionary<string, object?>());
}

static Dictionary<string, object?> ValidRoute(string id)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "route",
        ["owner_domain"] = "routes",
        ["status"] = "Active",
        ["schema_version"] = 1,
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["tags"] = new[] { "test" },
        ["sort_order"] = 10,
        ["origin_location_id"] = "location.glass-harbor",
        ["destination_id"] = "location.sky-reef-outpost",
        ["distance_band"] = "short",
        ["hazard_tags"] = new[] { "safe" },
        ["references"] = Array.Empty<string>(),
    };
}

static Dictionary<string, object?> ValidLocation(string id, ContentStatus status)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "location",
        ["owner_domain"] = "world",
        ["status"] = status.ToString(),
        ["schema_version"] = 1,
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["tags"] = new[] { "test" },
        ["sort_order"] = 10,
        ["region_tag"] = "starter-sea",
        ["location_kind"] = "harbor",
        ["service_tags"] = new[] { "market" },
        ["local_identity_tags"] = new[] { "glass" },
        ["settlement_need_tags"] = new[] { "trade-link" },
        ["references"] = Array.Empty<string>(),
    };
}
