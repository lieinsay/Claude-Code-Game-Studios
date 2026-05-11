using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Stable identifiers for the registry diagnostic developer panels.
/// </summary>
public enum RegistryDiagnosticPanelId
{
    RegistryOverview = 0,
    ErrorList = 1,
    ContentItemInspector = 2,
    ReferenceGraph = 3,
    QueryTester = 4,
    CopyableReport = 5,
}

/// <summary>
/// Runtime visibility and keyboard status for one diagnostic developer panel.
/// </summary>
public sealed record RegistryDiagnosticPanelState(
    RegistryDiagnosticPanelId PanelId,
    string Title,
    bool Visible,
    bool HasContent,
    bool KeyboardReachable);

/// <summary>
/// Filter criteria for the registry diagnostic error list.
/// </summary>
public sealed record RegistryDiagnosticFilter(
    string? Severity = null,
    string? ErrorCode = null,
    string? Kind = null,
    string? OwnerDomain = null,
    string? ContentPackage = null);

/// <summary>
/// First-screen overview metrics for registry health.
/// </summary>
public sealed record RegistryDiagnosticOverview(
    int TotalCount,
    int FatalCount,
    int ErrorCount,
    int WarningCount,
    IReadOnlyList<RegistryDiagnosticEvent> FirstViewportIssues,
    bool HighSeverityVisibleInFirstViewport);

/// <summary>
/// Inspector payload for the currently selected registry diagnostic event.
/// </summary>
public sealed record RegistryDiagnosticInspector(
    RegistryDiagnosticEvent? SelectedDiagnostic,
    IReadOnlyDictionary<string, string> Fields,
    string HighlightedFieldPath,
    string CopyText);

/// <summary>
/// Lightweight reference graph node for the diagnostic graph panel.
/// </summary>
public sealed record RegistryReferenceGraphNode(
    string ContentId,
    bool HasError,
    string VisualStateToken);

/// <summary>
/// Lightweight reference graph edge for the diagnostic graph panel.
/// </summary>
public sealed record RegistryReferenceGraphEdge(
    string FromContentId,
    string ToContentId,
    bool HasError);

/// <summary>
/// Reference graph payload consumed by a future Godot Control renderer.
/// </summary>
public sealed record RegistryReferenceGraphModel(
    bool ErrorOnlyMode,
    IReadOnlyList<RegistryReferenceGraphNode> Nodes,
    IReadOnlyList<RegistryReferenceGraphEdge> Edges,
    string EmptyMessage);

/// <summary>
/// Query tester input for read-only registry diagnostics.
/// </summary>
public sealed record RegistryDiagnosticQuery(
    string? Id = null,
    string? Kind = null,
    string? Domain = null,
    IReadOnlyList<string>? Tags = null);

/// <summary>
/// Query tester output with deterministic entity ordering.
/// </summary>
public sealed record RegistryDiagnosticQueryResult(
    string Status,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Entities,
    IReadOnlyList<string> MatchedIds,
    string? ErrorCode);

/// <summary>
/// Keyboard focus target exposed for complete non-mouse diagnostic navigation.
/// </summary>
public sealed record RegistryDiagnosticFocusTarget(
    RegistryDiagnosticPanelId PanelId,
    string ElementId,
    string Label,
    bool FocusVisible,
    string FocusRingToken);

/// <summary>
/// Debug-build registry diagnostic developer tools model. It consumes diagnostics
/// produced by the Registry and exposes panel, filtering, copy, graph, query, and
/// keyboard-focus state for a Godot Control UI to bind.
/// </summary>
public sealed class RegistryDiagnosticDevTools
{
    /// <summary>
    /// ADR-0012 focus ring token for keyboard navigation.
    /// </summary>
    public const string KeyboardFocusRingToken = "focus:#4FB7B2:1.5px-solid";

    private static readonly RegistryDiagnosticPanelState[] VisiblePanels =
    [
        Panel(RegistryDiagnosticPanelId.RegistryOverview, "Registry Overview"),
        Panel(RegistryDiagnosticPanelId.ErrorList, "Error List"),
        Panel(RegistryDiagnosticPanelId.ContentItemInspector, "Content Item Inspector"),
        Panel(RegistryDiagnosticPanelId.ReferenceGraph, "Reference Graph"),
        Panel(RegistryDiagnosticPanelId.QueryTester, "Query Tester"),
        Panel(RegistryDiagnosticPanelId.CopyableReport, "Copyable Report Panel"),
    ];

    private readonly Registry _registry;
    private readonly List<RegistryDiagnosticEvent> _diagnostics;
    private int _focusIndex;
    private int _selectedErrorIndex;

    /// <summary>
    /// Creates a diagnostic tool model. Use <see cref="TryOpen"/> for production gating.
    /// </summary>
    public RegistryDiagnosticDevTools(
        Registry registry,
        IEnumerable<RegistryDiagnosticEvent> diagnostics,
        bool isDebugBuild)
    {
        if (!isDebugBuild)
        {
            throw new InvalidOperationException("Registry diagnostic tools are only available in debug builds.");
        }

        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _diagnostics = Registry.SortDiagnostics(diagnostics ?? Array.Empty<RegistryDiagnosticEvent>()).ToList();
    }

    /// <summary>
    /// Returns true when the current compilation is a debug build.
    /// </summary>
    public static bool IsCurrentBuildDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Opens diagnostic tools only when debug-build gating allows it.
    /// </summary>
    public static RegistryDiagnosticDevTools? TryOpen(
        Registry registry,
        IEnumerable<RegistryDiagnosticEvent> diagnostics,
        bool? isDebugBuild = null)
    {
        var debugBuild = isDebugBuild ?? IsCurrentBuildDebug;
        return debugBuild
            ? new RegistryDiagnosticDevTools(registry, diagnostics, isDebugBuild: true)
            : null;
    }

    /// <summary>
    /// Gets all required diagnostic panels as visible and keyboard reachable.
    /// </summary>
    public IReadOnlyList<RegistryDiagnosticPanelState> Panels => VisiblePanels;

    /// <summary>
    /// Gets the current keyboard focus target.
    /// </summary>
    public RegistryDiagnosticFocusTarget CurrentFocus => FocusTargets[_focusIndex];

    /// <summary>
    /// Gets the complete keyboard focus order for the diagnostic tools.
    /// </summary>
    public IReadOnlyList<RegistryDiagnosticFocusTarget> FocusTargets => BuildFocusTargets();

    /// <summary>
    /// Builds the high-level registry health overview.
    /// </summary>
    public RegistryDiagnosticOverview BuildOverview(int firstViewportIssueCapacity = 3)
    {
        var highSeverity = _diagnostics.Where(IsHighSeverity).ToArray();
        var firstViewport = _diagnostics
            .Where(diagnostic => IsHighSeverity(diagnostic) || IsActionableDiagnostic(diagnostic))
            .Take(Math.Max(1, firstViewportIssueCapacity))
            .ToArray();

        return new RegistryDiagnosticOverview(
            _diagnostics.Count,
            _diagnostics.Count(diagnostic => IsSeverity(diagnostic, "fatal")),
            _diagnostics.Count(diagnostic => IsSeverity(diagnostic, "error")),
            _diagnostics.Count(diagnostic => IsSeverity(diagnostic, "warning")),
            firstViewport,
            highSeverity.All(firstViewport.Contains));
    }

    /// <summary>
    /// Returns diagnostics filtered by severity, code, kind, domain, and package.
    /// </summary>
    public IReadOnlyList<RegistryDiagnosticEvent> GetFilteredErrors(RegistryDiagnosticFilter? filter = null)
    {
        filter ??= new RegistryDiagnosticFilter();
        return _diagnostics
            .Where(diagnostic => Matches(filter.Severity, diagnostic.Severity))
            .Where(diagnostic => Matches(filter.ErrorCode, diagnostic.ErrorCode))
            .Where(diagnostic => Matches(filter.Kind, diagnostic.Kind))
            .Where(diagnostic => Matches(filter.OwnerDomain, diagnostic.OwnerDomain))
            .Where(diagnostic => Matches(filter.ContentPackage, diagnostic.ContentPackage))
            .ToArray();
    }

    /// <summary>
    /// Builds an inspector payload for one diagnostic event.
    /// </summary>
    public RegistryDiagnosticInspector InspectDiagnostic(string eventId)
    {
        var diagnostic = _diagnostics.FirstOrDefault(item => item.EventId == eventId)
            ?? _diagnostics.FirstOrDefault();
        if (diagnostic is null)
        {
            return new RegistryDiagnosticInspector(
                null,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["empty"] = "No registry diagnostics.",
                },
                string.Empty,
                "No registry diagnostics.");
        }

        var fields = BuildSingleDiagnosticFields(diagnostic);
        return new RegistryDiagnosticInspector(
            diagnostic,
            fields,
            diagnostic.FieldPath,
            FormatSingleDiagnostic(fields));
    }

    /// <summary>
    /// Copies one diagnostic as the story-required 16-field plain-text block.
    /// </summary>
    public string CopyDiagnostic(string eventId)
    {
        return InspectDiagnostic(eventId).CopyText;
    }

    /// <summary>
    /// Copies the filtered diagnostic list as a Registry Diagnostic Summary table.
    /// </summary>
    public string CopySummary(RegistryDiagnosticFilter? filter = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Registry Diagnostic Summary");
        builder.AppendLine("| severity | error_code | content_id | kind | field_path | blocking_scope | suggested_action |");
        builder.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var diagnostic in GetFilteredErrors(filter))
        {
            builder
                .Append("| ")
                .Append(EscapeTable(diagnostic.Severity))
                .Append(" | ")
                .Append(EscapeTable(diagnostic.ErrorCode))
                .Append(" | ")
                .Append(EscapeTable(diagnostic.ContentId))
                .Append(" | ")
                .Append(EscapeTable(diagnostic.Kind))
                .Append(" | ")
                .Append(EscapeTable(diagnostic.FieldPath))
                .Append(" | ")
                .Append(EscapeTable(diagnostic.BlockingScope))
                .Append(" | ")
                .Append(EscapeTable(diagnostic.SuggestedAction))
                .AppendLine(" |");
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds a reference graph, optionally limited to diagnostic error chains only.
    /// </summary>
    public RegistryReferenceGraphModel BuildReferenceGraph(bool errorOnly)
    {
        var diagnostics = errorOnly
            ? _diagnostics.Where(IsActionableDiagnostic).ToArray()
            : _diagnostics.ToArray();
        var nodes = new Dictionary<string, RegistryReferenceGraphNode>(StringComparer.Ordinal);
        var edges = new HashSet<string>(StringComparer.Ordinal);
        var edgeModels = new List<RegistryReferenceGraphEdge>();

        foreach (var diagnostic in diagnostics)
        {
            AddNode(nodes, diagnostic.ContentId, IsActionableDiagnostic(diagnostic), VisualTokenFor(diagnostic));
            AddReferenceChain(nodes, edges, edgeModels, diagnostic.ReferenceChain, IsActionableDiagnostic(diagnostic));

            foreach (var related in diagnostic.RelatedErrors)
            {
                AddNode(nodes, related.ContentId, true, VisualTokenFor(related));
                AddReferenceChain(nodes, edges, edgeModels, related.ReferenceChain, true);
            }
        }

        var emptyMessage = errorOnly
            ? "No diagnostic error chains."
            : "No registry reference graph nodes.";
        return new RegistryReferenceGraphModel(
            errorOnly,
            nodes.Values.OrderBy(node => node.ContentId, StringComparer.Ordinal).ToArray(),
            edgeModels.OrderBy(edge => edge.FromContentId, StringComparer.Ordinal)
                .ThenBy(edge => edge.ToContentId, StringComparer.Ordinal)
                .ToArray(),
            nodes.Count == 0 ? emptyMessage : string.Empty);
    }

    /// <summary>
    /// Executes a read-only registry query for the Query Tester panel.
    /// </summary>
    public RegistryDiagnosticQueryResult ExecuteQuery(RegistryDiagnosticQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Id))
        {
            var byId = _registry.QueryById(query.Id);
            return new RegistryDiagnosticQueryResult(
                byId.Status.ToString(),
                byId.Entity is null ? Array.Empty<IReadOnlyDictionary<string, object?>>() : [byId.Entity],
                byId.Entity is null ? Array.Empty<string>() : [ReadEntityId(byId.Entity)],
                byId.Error);
        }

        var tags = (query.Tags ?? Array.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToArray();
        if (!string.IsNullOrWhiteSpace(query.Domain))
        {
            var results = _registry.ListByDomain(query.Domain)
                .Where(entity => string.IsNullOrWhiteSpace(query.Kind)
                    || string.Equals(ReadString(entity, "kind"), query.Kind, StringComparison.Ordinal))
                .Where(entity => TagsMatch(entity, tags))
                .ToArray();
            return BuildListQueryResult(results);
        }

        if (tags.Length > 0)
        {
            var byTags = _registry.QueryUniqueByTags(tags, query.Kind);
            return new RegistryDiagnosticQueryResult(
                byTags.Status.ToString(),
                byTags.Entity is null ? Array.Empty<IReadOnlyDictionary<string, object?>>() : [byTags.Entity],
                byTags.MatchedIds,
                byTags.ErrorCode);
        }

        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            return BuildListQueryResult(_registry.ListByKind(query.Kind));
        }

        return new RegistryDiagnosticQueryResult(
            "EmptyQuery",
            Array.Empty<IReadOnlyDictionary<string, object?>>(),
            Array.Empty<string>(),
            "EMPTY_QUERY");
    }

    /// <summary>
    /// Advances keyboard focus to the next diagnostic tool target.
    /// </summary>
    public RegistryDiagnosticFocusTarget FocusNext()
    {
        _focusIndex = (_focusIndex + 1) % FocusTargets.Count;
        return CurrentFocus;
    }

    /// <summary>
    /// Moves keyboard focus to the previous diagnostic tool target.
    /// </summary>
    public RegistryDiagnosticFocusTarget FocusPrevious()
    {
        _focusIndex = (_focusIndex - 1 + FocusTargets.Count) % FocusTargets.Count;
        return CurrentFocus;
    }

    /// <summary>
    /// Moves the selected error row by keyboard delta and clamps at list bounds.
    /// </summary>
    public RegistryDiagnosticEvent? MoveErrorSelection(int delta, RegistryDiagnosticFilter? filter = null)
    {
        var errors = GetFilteredErrors(filter);
        if (errors.Count == 0)
        {
            _selectedErrorIndex = 0;
            return null;
        }

        _selectedErrorIndex = Math.Clamp(_selectedErrorIndex + delta, 0, errors.Count - 1);
        return errors[_selectedErrorIndex];
    }

    private static RegistryDiagnosticPanelState Panel(RegistryDiagnosticPanelId panelId, string title)
    {
        return new RegistryDiagnosticPanelState(panelId, title, Visible: true, HasContent: true, KeyboardReachable: true);
    }

    private static IReadOnlyDictionary<string, string> BuildSingleDiagnosticFields(RegistryDiagnosticEvent diagnostic)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["event_id"] = diagnostic.EventId,
            ["timestamp"] = diagnostic.Timestamp.ToString("O"),
            ["severity"] = diagnostic.Severity,
            ["error_code"] = diagnostic.ErrorCode,
            ["content_id"] = diagnostic.ContentId,
            ["kind"] = diagnostic.Kind,
            ["status"] = diagnostic.Status,
            ["schema_version"] = diagnostic.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["owner_domain"] = diagnostic.OwnerDomain,
            ["content_package"] = diagnostic.ContentPackage,
            ["source_ref"] = diagnostic.SourceRef,
            ["field_path"] = diagnostic.FieldPath,
            ["reference_chain"] = string.Join(" -> ", diagnostic.ReferenceChain),
            ["query_context"] = diagnostic.QueryContext,
            ["blocking_scope"] = diagnostic.BlockingScope,
            ["suggested_action"] = diagnostic.SuggestedAction,
        };
    }

    private static string FormatSingleDiagnostic(IReadOnlyDictionary<string, string> fields)
    {
        return string.Join(Environment.NewLine, fields.Select(pair => $"{pair.Key}: {pair.Value}"));
    }

    private static void AddReferenceChain(
        IDictionary<string, RegistryReferenceGraphNode> nodes,
        ISet<string> edgeKeys,
        ICollection<RegistryReferenceGraphEdge> edges,
        IReadOnlyList<string> referenceChain,
        bool hasError)
    {
        for (var index = 0; index < referenceChain.Count; index++)
        {
            var nodeId = NormalizeChainNode(referenceChain[index]);
            AddNode(nodes, nodeId, hasError, hasError ? "error-alert-triangle" : "active-solid-check");

            if (index == 0)
            {
                continue;
            }

            var from = NormalizeChainNode(referenceChain[index - 1]);
            var key = $"{from}->{nodeId}";
            if (edgeKeys.Add(key))
            {
                edges.Add(new RegistryReferenceGraphEdge(from, nodeId, hasError));
            }
        }
    }

    private static void AddNode(
        IDictionary<string, RegistryReferenceGraphNode> nodes,
        string contentId,
        bool hasError,
        string visualStateToken)
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return;
        }

        if (!nodes.TryGetValue(contentId, out var existing) || hasError && !existing.HasError)
        {
            nodes[contentId] = new RegistryReferenceGraphNode(contentId, hasError, visualStateToken);
        }
    }

    private static IReadOnlyList<RegistryDiagnosticFocusTarget> BuildFocusTargets()
    {
        return
        [
            Focus(RegistryDiagnosticPanelId.ErrorList, "filter.severity", "Severity filter"),
            Focus(RegistryDiagnosticPanelId.ErrorList, "filter.error_code", "Error code filter"),
            Focus(RegistryDiagnosticPanelId.ErrorList, "filter.kind", "Kind filter"),
            Focus(RegistryDiagnosticPanelId.ErrorList, "filter.owner_domain", "Owner domain filter"),
            Focus(RegistryDiagnosticPanelId.ErrorList, "filter.content_package", "Content package filter"),
            Focus(RegistryDiagnosticPanelId.ErrorList, "error_list", "Error List"),
            Focus(RegistryDiagnosticPanelId.ContentItemInspector, "inspector", "Content Item Inspector"),
            Focus(RegistryDiagnosticPanelId.ReferenceGraph, "reference_graph", "Reference Graph"),
            Focus(RegistryDiagnosticPanelId.QueryTester, "query.id", "Query ID input"),
            Focus(RegistryDiagnosticPanelId.QueryTester, "query.kind", "Query kind input"),
            Focus(RegistryDiagnosticPanelId.QueryTester, "query.domain", "Query domain input"),
            Focus(RegistryDiagnosticPanelId.QueryTester, "query.tags", "Query tags input"),
            Focus(RegistryDiagnosticPanelId.CopyableReport, "copy.single", "Copy current diagnostic"),
            Focus(RegistryDiagnosticPanelId.CopyableReport, "copy.summary", "Copy Registry Diagnostic Summary"),
        ];
    }

    private static RegistryDiagnosticFocusTarget Focus(
        RegistryDiagnosticPanelId panelId,
        string elementId,
        string label)
    {
        return new RegistryDiagnosticFocusTarget(panelId, elementId, label, FocusVisible: true, KeyboardFocusRingToken);
    }

    private static RegistryDiagnosticQueryResult BuildListQueryResult(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> entities)
    {
        return new RegistryDiagnosticQueryResult(
            entities.Count > 0 ? "Found" : "NotFound",
            entities,
            entities.Select(ReadEntityId).ToArray(),
            entities.Count > 0 ? null : "NOT_FOUND");
    }

    private static bool TagsMatch(IReadOnlyDictionary<string, object?> entity, IReadOnlyList<string> requiredTags)
    {
        return requiredTags.Count == 0 || requiredTags.All(ReadStringSet(entity, "tags").Contains);
    }

    private static bool Matches(string? filterValue, string actual)
    {
        return string.IsNullOrWhiteSpace(filterValue)
            || string.Equals(filterValue, actual, StringComparison.Ordinal);
    }

    private static bool IsSeverity(RegistryDiagnosticEvent diagnostic, string severity)
    {
        return string.Equals(diagnostic.Severity, severity, StringComparison.Ordinal);
    }

    private static bool IsHighSeverity(RegistryDiagnosticEvent diagnostic)
    {
        return IsSeverity(diagnostic, "fatal") || IsSeverity(diagnostic, "error");
    }

    private static bool IsActionableDiagnostic(RegistryDiagnosticEvent diagnostic)
    {
        return !string.Equals(diagnostic.ErrorCode, "REGISTRY_DIAGNOSTIC_OK", StringComparison.Ordinal)
            && !IsSeverity(diagnostic, "info");
    }

    private static string VisualTokenFor(RegistryDiagnosticEvent diagnostic)
    {
        if (IsActionableDiagnostic(diagnostic))
        {
            if (diagnostic.ErrorCode == "VERSION_INCOMPATIBLE"
                || diagnostic.ErrorCode == "ERR_CONTENT_PACKAGE_VERSION")
            {
                return "version-incompatible-broken-border-version-label";
            }

            if (diagnostic.ErrorCode == "UNLOADED_REFERENCE")
            {
                return "unloaded-translucent-dashed";
            }

            return "error-alert-triangle-rust-border";
        }

        return diagnostic.Status switch
        {
            "Draft" => "draft-dashed-draft-label",
            "Deprecated" => "deprecated-faded-old-label",
            "Retired" => "retired-broken-seal",
            _ => "active-solid-check",
        };
    }

    private static string VisualTokenFor(RegistryRelatedDiagnostic diagnostic)
    {
        return diagnostic.ErrorCode == "UNLOADED_REFERENCE"
            ? "unloaded-translucent-dashed"
            : "error-alert-triangle-rust-border";
    }

    private static string NormalizeChainNode(string node)
    {
        var marker = node.IndexOf('(', StringComparison.Ordinal);
        return marker > 0 ? node[..marker] : node;
    }

    private static string EscapeTable(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");
    }

    private static string ReadEntityId(IReadOnlyDictionary<string, object?> entity)
    {
        return ReadString(entity, "id");
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> entity, string field)
    {
        return entity.TryGetValue(field, out var value) && value is not null
            ? value.ToString() ?? string.Empty
            : string.Empty;
    }

    private static HashSet<string> ReadStringSet(IReadOnlyDictionary<string, object?> entity, string field)
    {
        if (!entity.TryGetValue(field, out var value) || value is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (value is string stringValue)
        {
            return new HashSet<string>([stringValue], StringComparer.Ordinal);
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable
                .Cast<object?>()
                .Where(item => item is not null)
                .Select(item => item!.ToString() ?? string.Empty)
                .ToHashSet(StringComparer.Ordinal);
        }

        return new HashSet<string>(StringComparer.Ordinal);
    }
}
