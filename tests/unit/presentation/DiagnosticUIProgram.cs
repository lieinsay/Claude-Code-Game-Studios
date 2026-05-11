using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Story 007: Registry Diagnostic UI — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: all diagnostic panels are visible and nonblank", Ac1PanelsVisible);
Run("AC-2: fatal and error diagnostics are first-viewport visible", Ac2HighSeverityOverview);
Run("AC-3: single diagnostic copy includes required fields", Ac3SingleDiagnosticCopyFields);
Run("AC-4: bulk copy emits Registry Diagnostic Summary table", Ac4BulkCopySummaryTable);
Run("AC-5: reference graph supports error-only mode", Ac5ReferenceGraphErrorOnlyMode);
Run("AC-6: keyboard navigation reaches every diagnostic work area", Ac6KeyboardFocusOrder);
Run("Debug gate: UIManager exposes tools only in debug builds", DebugGate);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 007 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 007 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1PanelsVisible()
{
    var tools = CreateTools();
    var expected = Enum.GetValues<RegistryDiagnosticPanelId>().ToHashSet();
    var actual = tools.Panels.Select(panel => panel.PanelId).ToHashSet();

    return expected.SetEquals(actual)
        && tools.Panels.All(panel => panel.Visible && panel.HasContent && panel.KeyboardReachable);
}

static bool Ac2HighSeverityOverview()
{
    var tools = CreateTools();
    var overview = tools.BuildOverview();

    return overview.FatalCount == 1
        && overview.ErrorCount == 1
        && overview.HighSeverityVisibleInFirstViewport
        && overview.FirstViewportIssues.Count >= 2
        && overview.FirstViewportIssues.Take(2).All(item => item.Severity is "fatal" or "error");
}

static bool Ac3SingleDiagnosticCopyFields()
{
    var tools = CreateTools();
    var diagnostic = tools.GetFilteredErrors(new RegistryDiagnosticFilter(Severity: "error")).Single();
    var copy = tools.CopyDiagnostic(diagnostic.EventId);
    var lines = copy.Split(Environment.NewLine);

    return lines.Length == 16
        && copy.Contains("severity: error", StringComparison.Ordinal)
        && copy.Contains("error_code: ERR_MISSING_REFERENCE", StringComparison.Ordinal)
        && copy.Contains("content_id: route.broken", StringComparison.Ordinal)
        && copy.Contains("source_ref: content/routes.yaml:12", StringComparison.Ordinal)
        && copy.Contains("blocking_scope: item", StringComparison.Ordinal)
        && copy.Contains("suggested_action:", StringComparison.Ordinal);
}

static bool Ac4BulkCopySummaryTable()
{
    var tools = CreateTools();
    var summary = tools.CopySummary();

    return summary.StartsWith("Registry Diagnostic Summary", StringComparison.Ordinal)
        && summary.Contains("| severity | error_code | content_id | kind | field_path | blocking_scope | suggested_action |", StringComparison.Ordinal)
        && summary.Contains("| error | ERR_MISSING_REFERENCE | route.broken | route | destination_id | item |", StringComparison.Ordinal)
        && summary.Contains("| fatal | ERR_CONTENT_PACKAGE_VERSION | route.future | route | schema_version | registry |", StringComparison.Ordinal);
}

static bool Ac5ReferenceGraphErrorOnlyMode()
{
    var tools = CreateTools();
    var all = tools.BuildReferenceGraph(errorOnly: false);
    var errorsOnly = tools.BuildReferenceGraph(errorOnly: true);

    return all.Nodes.Any(node => node.ContentId == "route.ok")
        && !errorsOnly.Nodes.Any(node => node.ContentId == "route.ok")
        && errorsOnly.Nodes.Any(node => node.ContentId == "route.broken" && node.HasError)
        && errorsOnly.Nodes.Any(node => node.ContentId == "location.missing")
        && errorsOnly.Edges.Any(edge => edge.FromContentId == "route.broken" && edge.ToContentId == "location.missing");
}

static bool Ac6KeyboardFocusOrder()
{
    var tools = CreateTools();
    var panels = tools.FocusTargets.Select(target => target.PanelId).ToHashSet();
    var requiredPanels = new[]
    {
        RegistryDiagnosticPanelId.ErrorList,
        RegistryDiagnosticPanelId.ContentItemInspector,
        RegistryDiagnosticPanelId.ReferenceGraph,
        RegistryDiagnosticPanelId.QueryTester,
        RegistryDiagnosticPanelId.CopyableReport,
    };

    var visited = new HashSet<string>(StringComparer.Ordinal) { tools.CurrentFocus.ElementId };
    for (var index = 0; index < tools.FocusTargets.Count * 2; index++)
    {
        visited.Add(tools.FocusNext().ElementId);
    }

    return requiredPanels.All(panels.Contains)
        && tools.FocusTargets.All(target => target.FocusVisible)
        && tools.FocusTargets.All(target => target.FocusRingToken == RegistryDiagnosticDevTools.KeyboardFocusRingToken)
        && visited.IsSupersetOf(tools.FocusTargets.Select(target => target.ElementId));
}

static bool DebugGate()
{
    var registry = new Registry();
    registry.InitializeContent();
    var diagnostics = BuildDiagnostics(registry);

    var releaseUi = new UIManager();
    releaseUi.Initialize();
    var blocked = releaseUi.OpenRegistryDiagnosticTools(registry, diagnostics, isDebugBuild: false);

    var debugUi = new UIManager();
    debugUi.Initialize();
    var opened = debugUi.OpenRegistryDiagnosticTools(registry, diagnostics, isDebugBuild: true);

    return blocked is null
        && !releaseUi.IsModalOpen()
        && opened is not null
        && debugUi.IsModalOpen()
        && debugUi.ActiveInputLayer == InputLayer.Modal;
}

static RegistryDiagnosticDevTools CreateTools()
{
    var registry = new Registry();
    registry.InitializeContent();
    return RegistryDiagnosticDevTools.TryOpen(registry, BuildDiagnostics(registry), isDebugBuild: true)
        ?? throw new InvalidOperationException("Diagnostic tools did not open in debug mode.");
}

static RegistryDiagnosticEvent[] BuildDiagnostics(Registry registry)
{
    var fatal = registry.GenerateDiagnostic(
        ValidRoute("route.future"),
        [Finding("ERR_CONTENT_PACKAGE_VERSION", "route.future", "schema_version")],
        contentPackage: "future-pack",
        sourceRef: "content/routes.yaml:7",
        queryContext: "validate_all");
    var error = registry.GenerateDiagnostic(
        ValidRoute("route.broken"),
        [Finding("ERR_MISSING_REFERENCE", "route.broken", "destination_id", ["route.broken", "location.missing"])],
        contentPackage: "routes-pack",
        sourceRef: "content/routes.yaml:12",
        queryContext: "validate_all");
    var ok = registry.GenerateDiagnostic(
        ValidRoute("route.ok"),
        Array.Empty<RegistryDiagnosticFinding>(),
        contentPackage: "routes-pack",
        sourceRef: "content/routes.yaml:20",
        queryContext: "validate_all");

    return [fatal, error, ok];
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
