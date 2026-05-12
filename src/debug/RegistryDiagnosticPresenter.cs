using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Debug;

/// <summary>
/// Pure C# presenter that converts Registry state into diagnostic display data.
/// All Godot UI interaction is handled by RegistryDiagnosticPanel; this class
/// contains no Godot dependency and is independently testable.
/// </summary>
public sealed class RegistryDiagnosticPresenter
{
    private static readonly string[] AllKinds =
    [
        "resource", "cargo", "module", "home-space", "home-anchor",
        "route", "location", "repair-node", "stall-good", "companion", "threat", "intel",
    ];

    private readonly Registry _registry;

    /// <param name="registry">Initialized registry instance to inspect.</param>
    public RegistryDiagnosticPresenter(Registry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Scans all registered content, runs validators, and returns a filtered error list
    /// sorted by diagnostic precedence.
    /// </summary>
    public IReadOnlyList<DiagnosticViewItem> BuildErrorList(DiagnosticFilterState filter)
    {
        var events = CollectAllDiagnostics();
        var sorted = Registry.SortDiagnostics(events);

        return sorted
            .Where(e => MatchesFilter(e, filter))
            .Select(ToDiagnosticViewItem)
            .ToList();
    }

    /// <summary>
    /// Returns a plain-text overview summary: total items, breakdown by status, error counts.
    /// </summary>
    public string BuildOverviewText()
    {
        var allEvents = CollectAllDiagnostics();
        var totalItems = CountAllContent();
        var fatal = allEvents.Count(e => string.Equals(e.Severity, "fatal", StringComparison.OrdinalIgnoreCase));
        var errors = allEvents.Count(e => string.Equals(e.Severity, "error", StringComparison.OrdinalIgnoreCase));
        var warnings = allEvents.Count(e => string.Equals(e.Severity, "warning", StringComparison.OrdinalIgnoreCase));

        var sb = new StringBuilder();
        sb.AppendLine($"Total content items: {totalItems}");
        sb.AppendLine($"Diagnostics — Fatal: {fatal}  Error: {errors}  Warning: {warnings}");
        if (fatal > 0)
        {
            sb.AppendLine("[!] Fatal issues detected — registry may be unavailable");
        }
        else if (errors > 0)
        {
            sb.AppendLine("[!] Errors detected — some content cannot be queried");
        }
        else if (warnings > 0)
        {
            sb.AppendLine("[~] Warnings — review deprecated references");
        }
        else
        {
            sb.AppendLine("[OK] No issues found");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds full inspector data for the given content_id, or null if not found.
    /// </summary>
    public DiagnosticInspectorData? BuildInspectorData(string contentId)
    {
        var events = CollectAllDiagnostics();
        var match = events.FirstOrDefault(e =>
            string.Equals(e.ContentId, contentId, StringComparison.Ordinal));

        if (match is null)
        {
            return null;
        }

        return new DiagnosticInspectorData(
            EventId: match.EventId,
            Timestamp: match.Timestamp.ToString("o"),
            Severity: match.Severity,
            ErrorCode: match.ErrorCode,
            ContentId: match.ContentId,
            Kind: match.Kind,
            Status: match.Status,
            SchemaVersion: match.SchemaVersion,
            OwnerDomain: match.OwnerDomain,
            ContentPackage: match.ContentPackage,
            SourceRef: match.SourceRef,
            FieldPath: match.FieldPath,
            ReferenceChain: match.ReferenceChain,
            QueryContext: match.QueryContext,
            BlockingScope: match.BlockingScope,
            SuggestedAction: match.SuggestedAction,
            RelatedErrors: match.RelatedErrors
                .Select(r => $"{r.Severity}:{r.ErrorCode}:{r.ContentId}")
                .ToList());
    }

    /// <summary>
    /// Builds the reference graph, optionally filtered to error chains only.
    /// </summary>
    public ReferenceGraph BuildReferenceGraph(bool errorChainOnly)
    {
        var errorContentIds = new HashSet<string>(StringComparer.Ordinal);
        if (errorChainOnly)
        {
            var events = CollectAllDiagnostics();
            foreach (var e in events)
            {
                if (!string.Equals(e.Severity, "info", StringComparison.OrdinalIgnoreCase))
                {
                    errorContentIds.Add(e.ContentId);
                    foreach (var chainId in e.ReferenceChain)
                    {
                        errorContentIds.Add(chainId);
                    }
                }
            }
        }

        var nodes = new List<ReferenceGraphNode>();
        foreach (var kind in AllKinds)
        {
            var items = _registry.ListByKind(kind);
            foreach (var item in items)
            {
                var id = GetString(item, "id");
                if (errorChainOnly && !errorContentIds.Contains(id))
                {
                    continue;
                }

                var refs = GetReferences(item);
                var hasError = errorContentIds.Contains(id);
                nodes.Add(new ReferenceGraphNode(id, GetString(item, "status"), hasError, refs));
            }
        }

        return new ReferenceGraph(nodes, errorChainOnly);
    }

    /// <summary>
    /// Executes a read-only query by id, kind, or domain and returns a summary result.
    /// </summary>
    public DiagnosticQueryResult ExecuteQuery(string queryId, string? kind, string? domain)
    {
        if (!string.IsNullOrWhiteSpace(queryId))
        {
            var result = _registry.QueryById(queryId.Trim());
            var summary = result.Entity is not null
                ? $"kind={GetString(result.Entity, "kind")} domain={GetString(result.Entity, "owner_domain")}"
                : result.Error ?? "no details";

            return new DiagnosticQueryResult(
                QueryId: queryId,
                Kind: kind ?? string.Empty,
                Domain: domain ?? string.Empty,
                Status: result.Status.ToString(),
                Summary: summary);
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            var items = _registry.ListByKind(kind.Trim());
            return new DiagnosticQueryResult(
                QueryId: string.Empty,
                Kind: kind,
                Domain: domain ?? string.Empty,
                Status: "Found",
                Summary: $"{items.Count} items of kind '{kind}'");
        }

        if (!string.IsNullOrWhiteSpace(domain))
        {
            var items = _registry.ListByDomain(domain.Trim());
            return new DiagnosticQueryResult(
                QueryId: string.Empty,
                Kind: string.Empty,
                Domain: domain,
                Status: "Found",
                Summary: $"{items.Count} items in domain '{domain}'");
        }

        return new DiagnosticQueryResult(string.Empty, string.Empty, string.Empty, "Empty", "No query provided");
    }

    /// <summary>
    /// Formats a single error as a plain-text 16-field report for single-item copy.
    /// </summary>
    public string FormatSingleErrorReport(DiagnosticViewItem item)
    {
        return string.Join('\n',
            $"event_id:         (scan-time)",
            $"severity:         {item.Severity}",
            $"error_code:       {item.ErrorCode}",
            $"content_id:       {item.ContentId}",
            $"kind:             {item.Kind}",
            $"status:           {item.Status}",
            $"owner_domain:     {item.OwnerDomain}",
            $"content_package:  {item.ContentPackage}",
            $"source_ref:       {item.SourceRef}",
            $"field_path:       {item.FieldPath}",
            $"reference_chain:  {string.Join(" → ", item.ReferenceChain)}",
            $"query_context:    {item.QueryContext}",
            $"blocking_scope:   {item.BlockingScope}",
            $"suggested_action: {item.SuggestedAction}",
            $"related_errors:   {string.Join(", ", item.RelatedErrorCodes)}");
    }

    /// <summary>
    /// Formats all filtered errors as a Registry Diagnostic Summary markdown table for batch copy.
    /// </summary>
    public string FormatBatchReport(IReadOnlyList<DiagnosticViewItem> items)
    {
        if (items.Count == 0)
        {
            return "Registry Diagnostic Summary — No errors found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Registry Diagnostic Summary");
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:o}  Items: {items.Count}");
        sb.AppendLine();
        sb.AppendLine("| severity | error_code | content_id | kind | field_path | blocking_scope | suggested_action |");
        sb.AppendLine("|----------|------------|------------|------|------------|----------------|-----------------|");

        foreach (var item in items)
        {
            sb.AppendLine(
                $"| {item.Severity} | {item.ErrorCode} | {item.ContentId} | {item.Kind} | {item.FieldPath} | {item.BlockingScope} | {item.SuggestedAction} |");
        }

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private List<RegistryDiagnosticEvent> CollectAllDiagnostics()
    {
        var allEvents = new List<RegistryDiagnosticEvent>();

        foreach (var kind in AllKinds)
        {
            var items = _registry.ListByKind(kind);
            foreach (var item in items)
            {
                var defResult = _registry.ValidateDefinition(item);
                var refResult = _registry.ValidateReferences(item);

                if (!defResult.Valid || !refResult.Valid)
                {
                    var evt = _registry.GenerateDiagnostic(
                        item,
                        defResult.Diagnostics,
                        refResult.Diagnostics,
                        queryContext: "diagnostic_scan");
                    allEvents.Add(evt);
                }
            }
        }

        return allEvents;
    }

    private int CountAllContent()
    {
        return AllKinds.Sum(k => _registry.ListByKind(k).Count);
    }

    private static bool MatchesFilter(RegistryDiagnosticEvent e, DiagnosticFilterState filter)
    {
        if (filter.Severity is not null
            && !string.Equals(e.Severity, filter.Severity, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.Kind is not null
            && !string.Equals(e.Kind, filter.Kind, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.Domain is not null
            && !string.Equals(e.OwnerDomain, filter.Domain, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.ErrorCode is not null
            && !string.Equals(e.ErrorCode, filter.ErrorCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static DiagnosticViewItem ToDiagnosticViewItem(RegistryDiagnosticEvent e)
    {
        return new DiagnosticViewItem(
            Severity: e.Severity,
            ErrorCode: e.ErrorCode,
            ContentId: e.ContentId,
            Kind: e.Kind,
            OwnerDomain: e.OwnerDomain,
            BlockingScope: e.BlockingScope,
            SuggestedAction: e.SuggestedAction,
            SourceRef: e.SourceRef,
            FieldPath: e.FieldPath,
            Status: e.Status,
            ContentPackage: e.ContentPackage,
            QueryContext: e.QueryContext,
            ReferenceChain: e.ReferenceChain,
            RelatedErrorCodes: e.RelatedErrors.Select(r => r.ErrorCode).ToList());
    }

    private static IReadOnlyList<string> GetReferences(IReadOnlyDictionary<string, object?> item)
    {
        if (!item.TryGetValue("references", out var raw) || raw is not IEnumerable<object?> list)
        {
            return Array.Empty<string>();
        }

        return list
            .OfType<string>()
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    private static string GetString(IReadOnlyDictionary<string, object?> item, string key)
    {
        return item.TryGetValue(key, out var val) && val is string s ? s : string.Empty;
    }
}
