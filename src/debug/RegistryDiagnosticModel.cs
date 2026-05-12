using System.Collections.Generic;

namespace CloudWeaverVoyage.Debug;

/// <summary>Filter state applied to the diagnostic Error List.</summary>
public sealed record DiagnosticFilterState(
    string? Severity,
    string? Kind,
    string? Domain,
    string? ErrorCode);

/// <summary>Flattened display row for the Error List panel.</summary>
public sealed record DiagnosticViewItem(
    string Severity,
    string ErrorCode,
    string ContentId,
    string Kind,
    string OwnerDomain,
    string BlockingScope,
    string SuggestedAction,
    string SourceRef,
    string FieldPath,
    string Status,
    string ContentPackage,
    string QueryContext,
    IReadOnlyList<string> ReferenceChain,
    IReadOnlyList<string> RelatedErrorCodes);

/// <summary>All 16 GDD-required fields for the Content Item Inspector panel.</summary>
public sealed record DiagnosticInspectorData(
    string EventId,
    string Timestamp,
    string Severity,
    string ErrorCode,
    string ContentId,
    string Kind,
    string Status,
    int SchemaVersion,
    string OwnerDomain,
    string ContentPackage,
    string SourceRef,
    string FieldPath,
    IReadOnlyList<string> ReferenceChain,
    string QueryContext,
    string BlockingScope,
    string SuggestedAction,
    IReadOnlyList<string> RelatedErrors);

/// <summary>Node in the reference graph display.</summary>
public sealed record ReferenceGraphNode(
    string ContentId,
    string Status,
    bool HasError,
    IReadOnlyList<string> RefTargets);

/// <summary>Complete reference graph for the graph panel.</summary>
public sealed record ReferenceGraph(
    IReadOnlyList<ReferenceGraphNode> Nodes,
    bool ErrorChainOnly);

/// <summary>Result of a Query Tester lookup.</summary>
public sealed record DiagnosticQueryResult(
    string QueryId,
    string Kind,
    string Domain,
    string Status,
    string Summary);
