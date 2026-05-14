using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Lifecycle state for static content definitions owned by the content registry.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum ContentStatus
{
    Draft = 0,
    Active = 1,
    Deprecated = 2,
    Retired = 3,
}

/// <summary>
/// Discriminated status for registry entity lookup results.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum RegistryQueryStatus
{
    Found = 0,
    NotFound = 1,
    Unloaded = 2,
    Deprecated = 3,
    VersionIncompatible = 4,
    Retired = 5,
}

/// <summary>
/// Loading state for player-facing static content domains.
/// </summary>
public enum DomainStatus
{
    Unloaded = 0,
    Loading = 1,
    Partial = 2,
    Complete = 3,
    Failed = 4,
}

/// <summary>
/// Player-facing decision surfaces guarded by content domain completeness.
/// </summary>
public enum DecisionSurface
{
    Chart = 0,
    Hub = 1,
    RepairMarket = 2,
}

/// <summary>
/// Result status for content package load attempts.
/// </summary>
public enum ContentPackageLoadStatus
{
    Loaded = 0,
    VersionIncompatible = 1,
    ValidationFailed = 2,
}

/// <summary>
/// Result returned by a single registry entity lookup.
/// </summary>
public sealed record RegistryQueryResult(
    RegistryQueryStatus Status,
    IReadOnlyDictionary<string, object?>? Entity,
    string? Error);

/// <summary>
/// Player-facing display fields extracted from a static content definition.
/// Contains only fields safe to show directly in player UI: no internal error codes,
/// no diagnostic fields, and no runtime state. All display text is provided as
/// localization keys, not raw strings.
/// </summary>
/// <param name="NameKey">Localization key for the content's display name (e.g. "content.location_glass_harbor.name").</param>
/// <param name="DescriptionKey">Localization key for the content's description (e.g. "content.location_glass_harbor.desc").</param>
/// <param name="IconRef">Optional icon reference string for asset lookup; null when the definition has no icon.</param>
/// <param name="Tags">Read-only tag set used for UI filtering and classification.</param>
/// <param name="SortOrder">Stable sort position for deterministic list ordering in UI.</param>
public sealed record PlayerDisplayInfo(
    string NameKey,
    string DescriptionKey,
    string? IconRef,
    IReadOnlyList<string> Tags,
    int SortOrder);

/// <summary>
/// Result returned when registering content in bulk.
/// </summary>
public sealed record RegistryRegistrationResult(
    bool Success,
    string? ErrorCode,
    string? EntityId);

/// <summary>
/// Result returned when checking whether domains can safely back a decision UI.
/// </summary>
public sealed record DomainReadinessResult(
    bool Ready,
    IReadOnlyList<string> BlockedDomains,
    IReadOnlyDictionary<string, DomainStatus> DomainStatuses);

/// <summary>
/// Opaque handle for a frozen content snapshot owned by the registry.
/// </summary>
public readonly record struct RegistrySnapshotHandle(Guid Id);

/// <summary>
/// Static content package submitted to the registry boundary.
/// </summary>
public sealed record RegistryContentPackage(
    string Domain,
    int SchemaVersion,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Definitions);

/// <summary>
/// Result returned by content package loading with copyable diagnostics for shell error surfaces.
/// </summary>
public sealed record ContentPackageLoadResult(
    ContentPackageLoadStatus Status,
    bool Success,
    string? ErrorCode,
    string? Domain,
    string Diagnostic);

/// <summary>
/// Structured migration hint for deprecated or retired content IDs.
/// </summary>
public sealed record ContentMigrationHint(
    string OriginalId,
    ContentStatus Status,
    string? SuggestedReplacementId,
    string? MigrationNote,
    string? RetiredDate);

/// <summary>
/// Current lifecycle resolution for a stable content ID.
/// </summary>
public sealed record ContentLifecycleResolution(
    string ContentId,
    ContentStatus? Status,
    ContentMigrationHint? MigrationHint,
    bool IsKnown);

/// <summary>
/// Result returned by lifecycle state mutations.
/// </summary>
public sealed record ContentLifecycleChangeResult(
    bool Success,
    string? ErrorCode,
    string ContentId,
    ContentStatus? PreviousStatus,
    ContentStatus? NewStatus);

/// <summary>
/// Event payload emitted after a content lifecycle mutation is committed.
/// </summary>
public sealed record ContentStatusChangedEvent(
    string ContentId,
    ContentStatus OldStatus,
    ContentStatus NewStatus);

/// <summary>
/// Structured diagnostic emitted by static content definition validation.
/// </summary>
public sealed record RegistryDiagnostic(
    string EventId,
    string Severity,
    string ErrorCode,
    string ContentId,
    string Field,
    string Message,
    IReadOnlyDictionary<string, object?> Details);

/// <summary>
/// Minimal normalized finding consumed by the diagnostic event aggregator.
/// </summary>
public sealed record RegistryDiagnosticFinding(
    string ErrorCode,
    string ContentId,
    string FieldPath,
    string Message,
    IReadOnlyList<string> ReferenceChain,
    IReadOnlyDictionary<string, object?> Details);

/// <summary>
/// Secondary error attached to a primary registry diagnostic event.
/// </summary>
public sealed record RegistryRelatedDiagnostic(
    string Severity,
    string ErrorCode,
    string ContentId,
    string FieldPath,
    IReadOnlyList<string> ReferenceChain,
    string BlockingScope,
    string SuggestedAction);

/// <summary>
/// Copyable registry diagnostic event with the full GDD-required field set.
/// </summary>
public sealed record RegistryDiagnosticEvent(
    string EventId,
    DateTimeOffset Timestamp,
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
    IReadOnlyList<RegistryRelatedDiagnostic> RelatedErrors);

/// <summary>
/// Definition validity result using the GDD U/K/R/S terms.
/// </summary>
public sealed record RegistryDefinitionValidationResult(
    bool Valid,
    bool HasUniqueId,
    bool MatchesKindSchema,
    bool RequiredFieldsPresent,
    bool HasNoRuntimeFields,
    IReadOnlyList<RegistryDiagnostic> Diagnostics);

/// <summary>
/// Structured reference chain diagnostic emitted by content reference integrity validation.
/// </summary>
public sealed record RegistryReferenceDiagnostic(
    string ErrorCode,
    string SourceId,
    string TargetId,
    IReadOnlyList<string> ReferenceChain,
    string Message);

/// <summary>
/// Result returned by reference graph validation for one static content definition.
/// </summary>
public sealed record RegistryReferenceValidationResult(
    bool Valid,
    bool ReferenceValidity,
    IReadOnlyList<RegistryReferenceDiagnostic> Diagnostics);

/// <summary>
/// Status for queries that require exactly one deterministic content match.
/// </summary>
public enum RegistryUniqueQueryStatus
{
    Found = 0,
    NotFound = 1,
    AmbiguousQuery = 2,
}

/// <summary>
/// Result returned by query APIs that must never silently choose among multiple matches.
/// </summary>
public sealed record RegistryUniqueQueryResult(
    RegistryUniqueQueryStatus Status,
    IReadOnlyDictionary<string, object?>? Entity,
    string? ErrorCode,
    IReadOnlyList<string> MatchedIds);

internal readonly record struct RegistryReferenceSpec(string Id, bool Optional);

/// <summary>
/// Static content catalog for stable IDs and deterministic read-only queries.
/// </summary>
public sealed class Registry
{
    private static readonly Regex StableIdPattern = new(
        "^[a-z][a-z0-9_-]*\\.[a-z0-9][a-z0-9_-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const int CurrentSchemaVersion = 1;

    private static readonly string[] CommonRequiredFields =
    [
        "id",
        "kind",
        "name_key",
        "description_key",
        "schema_version",
        "tags",
        "sort_order",
        "owner_domain",
        "references",
    ];

    private static readonly Dictionary<string, string[]> KindRequiredFields = new(StringComparer.Ordinal)
    {
        ["resource"] = ["unit", "stack_rule", "material_tags"],
        ["cargo"] = ["linked_resource_id", "mass_class", "handling_class"],
        ["module"] = ["slot_type", "compatibility_tags", "effect_tags"],
        ["home-space"] = ["space_kind", "home_function_tags", "access_tags"],
        ["home-anchor"] = ["home_space_id", "anchor_kind", "interaction_tags", "home_feedback_tags"],
        ["route"] = ["origin_location_id", "destination_id", "distance_band", "hazard_tags"],
        ["location"] = ["region_tag", "location_kind", "service_tags", "local_identity_tags", "settlement_need_tags"],
        ["repair-node"] = ["location_id", "node_kind", "restoration_theme", "settlement_need_tags", "repair_visible_state_tags"],
        ["stall-good"] = ["commodity_tags", "vendor_tags", "supply_class", "local_identity_tags", "settlement_need_tags", "repair_visible_state_tags"],
        ["companion"] = ["role_tags", "origin_location_id", "archetype_tags"],
        ["threat"] = ["threat_class", "encounter_tags", "counter_tags", "severity_tier"],
        ["intel"] = ["entry_type", "linked_content_ids", "source_tags", "presentation_tier"],
    };

    private static readonly Dictionary<string, string[]> ControlledVocabularies = new(StringComparer.Ordinal)
    {
        ["owner_domain"] = ["resources", "airship", "world", "routes", "intel", "companions", "threats"],
        ["kind"] = ["resource", "cargo", "module", "home-space", "home-anchor", "route", "location", "repair-node", "stall-good", "companion", "threat", "intel"],
        ["region_tag"] = ["starter-sea", "sky-reef", "storm-belt", "old-harbor-chain"],
        ["settlement_need_tags"] = ["food", "repair-materials", "navigation-aid", "safety", "trade-link", "home-comfort"],
        ["repair_visible_state_tags"] = ["dark", "damaged", "patched", "lit", "connected", "inhabited", "stock-improved"],
        ["home_function_tags"] = ["storage", "planning", "rest", "module-access", "companion-station", "crafting-light"],
        ["hazard_tags"] = ["safe", "mist", "storm", "raider", "low-visibility", "unstable-current"],
        ["severity_tier"] = ["minor", "moderate", "severe"],
        ["supply_class"] = ["basic", "repair", "navigation", "local-specialty", "intel"],
        ["presentation_tier"] = ["hint", "clue", "warning", "lore"],
    };

    private static readonly string[] RuntimeFieldDenylist =
    [
        "quantity",
        "inventory",
        "unlocked",
        "discovered",
        "durability",
        "current_price",
        "relationship",
        "installed",
        "repaired",
    ];

    private static readonly string[] FantasyCriticalKinds =
    [
        "route",
        "location",
        "repair-node",
        "home-space",
        "home-anchor",
        "companion",
    ];

    private static readonly string[] SemanticIdentityFields =
    [
        "kind",
        "owner_domain",
        "region_tag",
        "location_kind",
        "home_space_id",
        "anchor_kind",
        "origin_location_id",
        "destination_id",
        "location_id",
        "role_tags",
    ];

    private readonly Dictionary<string, Dictionary<string, object?>> content = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContentMigrationHint> retiredIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadedDomains = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadedKinds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> domainKindMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DomainStatus> domainStatuses = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, RegistrySnapshot> snapshots = new();

    private static readonly Dictionary<string, int> DiagnosticPriorities = new(StringComparer.Ordinal)
    {
        ["ERR_CONTENT_PACKAGE_VERSION"] = 1,
        ["VERSION_INCOMPATIBLE"] = 1,
        ["ERR_DEFINITION_VALIDITY_U"] = 2,
        ["ERR_DUPLICATE_ID"] = 2,
        ["ERR_ID_REUSE"] = 2,
        ["ERR_INVALID_ID_FORMAT"] = 2,
        ["ERR_ID_NORMALIZATION_COLLISION"] = 2,
        ["ERR_SCHEMA_INVALID"] = 3,
        ["ERR_SCHEMA_MISSING_REQUIRED_FIELD"] = 3,
        ["ERR_RUNTIME_FIELD_IN_STATIC_DATA"] = 4,
        ["ERR_READONLY_REGISTRY"] = 4,
        ["ERR_MISSING_REFERENCE"] = 5,
        ["UNLOADED_REFERENCE"] = 5,
        ["ERR_REFERENCE_TO_DRAFT"] = 6,
        ["ERR_REFERENCE_TO_DEPRECATED"] = 6,
        ["ERR_REFERENCE_TO_RETIRED"] = 6,
        ["ERR_REFERENCE_CYCLE"] = 7,
        ["ERR_REFERENCE_DEPTH_EXCEEDED"] = 7,
        ["ERR_INVALID_SORT_KEY"] = 8,
        ["AMBIGUOUS_QUERY"] = 8,
        ["ERR_UNSTABLE_IDENTIFIER"] = 8,
    };

    private static readonly IReadOnlyDictionary<DecisionSurface, string[]> DecisionSurfaceDomains =
        new Dictionary<DecisionSurface, string[]>
        {
            [DecisionSurface.Chart] = ["routes", "world", "intel", "threats"],
            [DecisionSurface.Hub] = ["airship", "resources", "companions"],
            [DecisionSurface.RepairMarket] = ["world", "resources", "intel"],
        };

    /// <summary>
    /// Fires after a content ID changes lifecycle status and the registry state is already mutated.
    /// </summary>
    public event Action<ContentStatusChangedEvent>? ContentStatusChanged;

    /// <summary>
    /// Fires synchronously after a content domain changes loading state.
    /// </summary>
    public event Action<string, string>? DomainReady;

    /// <summary>
    /// Gets whether the registry has completed its initial content bootstrap.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Maximum number of results returned by list queries before pagination.
    /// Default 200 per performance budget. Set to 0 for unlimited (dev tools only).
    /// </summary>
    public int MaxQueryResultCount { get; set; } = 200;

    /// <summary>
    /// Maximum transitive reference depth inspected per root item.
    /// Defaults to the GDD guardrail of 16 references per item.
    /// </summary>
    public int MaxReferencesPerItem { get; set; } = 16;

    /// <summary>
    /// Loads the prototype static content set into the registry.
    /// </summary>
    public void InitializeContent()
    {
        if (IsInitialized)
        {
            return;
        }

        RegistryBootstrap.Bootstrap(this);
        IsInitialized = true;
    }

    /// <summary>
    /// Looks up a static content definition by stable ID.
    /// </summary>
    public RegistryQueryResult QueryById(string entityId)
    {
        if (!IsInitialized)
        {
            return new RegistryQueryResult(RegistryQueryStatus.Unloaded, null, "registry_not_initialized");
        }

        if (content.TryGetValue(entityId, out var entity))
        {
            var status = ReadContentStatus(entity);
            return status switch
            {
                ContentStatus.Deprecated => new RegistryQueryResult(
                    RegistryQueryStatus.Deprecated,
                    CloneEntity(entity),
                    "entity_deprecated"),
                ContentStatus.Retired => new RegistryQueryResult(
                    RegistryQueryStatus.Retired,
                    CloneEntity(entity),
                    "entity_retired"),
                _ => new RegistryQueryResult(RegistryQueryStatus.Found, CloneEntity(entity), null),
            };
        }

        if (retiredIds.TryGetValue(entityId, out var retiredRecord))
        {
            return new RegistryQueryResult(
                RegistryQueryStatus.Retired,
                MigrationHintToEntity(retiredRecord),
                "entity_retired");
        }

        return IsLoadedIdFamily(entityId)
            ? new RegistryQueryResult(RegistryQueryStatus.NotFound, null, "id_not_found")
            : new RegistryQueryResult(RegistryQueryStatus.Unloaded, null, "domain_unloaded");
    }

    /// <summary>
    /// Lists active or draft content definitions for a kind in deterministic order.
    /// Results are capped at MaxQueryResultCount when set; 0 = unlimited.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ListByKind(string kind)
    {
        return ApplyResultLimit(
            content.Values
                .Where(entity => string.Equals(ReadString(entity, "kind"), kind, StringComparison.Ordinal))
                .Where(entity => ReadContentStatus(entity) <= ContentStatus.Active)
                .OrderBy(entity => ReadInt(entity, "sort_order", int.MaxValue))
                .ThenBy(entity => ReadString(entity, "id"), StringComparer.Ordinal)
                .Select(CloneEntity));
    }

    /// <summary>
    /// Returns a threat configuration by threat type, or null when the type is unknown.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetThreatConfig(string threatType)
    {
        if (string.IsNullOrWhiteSpace(threatType))
        {
            return null;
        }

        return ListByKind("threat")
            .FirstOrDefault(entity =>
                string.Equals(ReadString(entity, "threat_type"), threatType, StringComparison.Ordinal)
                || string.Equals(ReadString(entity, "threat_class"), threatType, StringComparison.Ordinal));
    }

    /// <summary>
    /// Lists active or draft content definitions for an owner domain in deterministic order.
    /// Results are capped at MaxQueryResultCount when set; 0 = unlimited.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ListByDomain(string domain)
    {
        return ApplyResultLimit(
            content.Values
                .Where(entity => string.Equals(ReadString(entity, "owner_domain"), domain, StringComparison.Ordinal))
                .Where(entity => ReadContentStatus(entity) <= ContentStatus.Active)
                .OrderBy(entity => ReadInt(entity, "sort_order", int.MaxValue))
                .ThenBy(entity => ReadString(entity, "id"), StringComparer.Ordinal)
                .Select(CloneEntity));
    }

    /// <summary>
    /// Lists active or draft content definitions for an owner domain from a frozen snapshot.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ListByDomain(
        string domain,
        RegistrySnapshotHandle handle)
    {
        if (!snapshots.TryGetValue(handle.Id, out var snapshot))
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        return ApplyResultLimit(
            snapshot.Content.Values
                .Where(entity => string.Equals(ReadString(entity, "owner_domain"), domain, StringComparison.Ordinal))
                .Where(entity => ReadContentStatus(entity) <= ContentStatus.Active)
                .OrderBy(entity => ReadInt(entity, "sort_order", int.MaxValue))
                .ThenBy(entity => ReadString(entity, "id"), StringComparer.Ordinal)
                .Select(CloneEntity));
    }

    /// <summary>
    /// Registers one content definition after schema validation.
    /// </summary>
    public void RegisterContent(string entityId, IReadOnlyDictionary<string, object?> definition)
    {
        var entity = CloneMutable(definition);
        entity["id"] = entityId;

        var result = RegisterBatch([entity]);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Registry content registration failed for '{entityId}': {result.ErrorCode}");
        }
    }

    private void InsertContentUnchecked(string entityId, IReadOnlyDictionary<string, object?> definition)
    {
        var entity = CloneMutable(definition);
        entity["id"] = entityId;
        content[entityId] = entity;

        var kind = ReadString(entity, "kind");
        if (!string.IsNullOrWhiteSpace(kind))
        {
            loadedKinds.Add(kind);
            var ownerDomain = ReadString(entity, "owner_domain");
            if (!string.IsNullOrWhiteSpace(ownerDomain))
            {
                if (!domainKindMap.TryGetValue(ownerDomain, out var kinds))
                {
                    kinds = new HashSet<string>(StringComparer.Ordinal);
                    domainKindMap[ownerDomain] = kinds;
                }

                kinds.Add(kind);
            }
        }
    }

    /// <summary>
    /// Registers a batch atomically after checking ID and definition validity.
    /// </summary>
    public RegistryRegistrationResult RegisterBatch(IEnumerable<IReadOnlyDictionary<string, object?>> definitions)
    {
        var pending = new List<(string Id, IReadOnlyDictionary<string, object?> Definition)>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenNormalizedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            var id = ReadString(definition, "id");
            if (!StableIdPattern.IsMatch(id))
            {
                return new RegistryRegistrationResult(false, "ERR_INVALID_ID_FORMAT", id);
            }

            if (retiredIds.ContainsKey(id))
            {
                return new RegistryRegistrationResult(false, "ERR_ID_REUSE", id);
            }

            if (!seenIds.Add(id))
            {
                return new RegistryRegistrationResult(false, "ERR_DUPLICATE_ID", id);
            }

            if (content.TryGetValue(id, out var existing))
            {
                return IsFantasyCriticalRedefinition(existing, definition)
                    ? new RegistryRegistrationResult(false, "ERR_ID_REDEFINITION", id)
                    : new RegistryRegistrationResult(false, "ERR_DUPLICATE_ID", id);
            }

            var normalizedId = id.Normalize(NormalizationForm.FormKC);
            if (!seenNormalizedIds.Add(normalizedId)
                || content.Keys.Any(existing => string.Equals(
                    existing.Normalize(NormalizationForm.FormKC),
                    normalizedId,
                    StringComparison.Ordinal)))
            {
                return new RegistryRegistrationResult(false, "ERR_ID_NORMALIZATION_COLLISION", id);
            }

            pending.Add((id, definition));
        }

        foreach (var (id, definition) in pending)
        {
            var validation = ValidateDefinition(definition, requireGloballyUniqueId: false);
            if (!validation.Valid)
            {
                var diagnostic = validation.Diagnostics.First();
                return new RegistryRegistrationResult(false, diagnostic.ErrorCode, id);
            }
        }

        var pendingLookup = pending.ToDictionary(pair => pair.Id, pair => pair.Definition, StringComparer.Ordinal);
        foreach (var (id, definition) in pending)
        {
            if (ReadContentStatus(definition) != ContentStatus.Active)
            {
                continue;
            }

            var referenceValidation = ValidateReferences(definition, pendingLookup);
            if (!referenceValidation.Valid)
            {
                return new RegistryRegistrationResult(
                    false,
                    referenceValidation.Diagnostics.First().ErrorCode,
                    id);
            }
        }

        foreach (var (id, definition) in pending)
        {
            InsertContentUnchecked(id, definition);
        }

        return new RegistryRegistrationResult(true, null, null);
    }

    /// <summary>
    /// Validates a static content definition against ID, kind schema, required fields, controlled vocabularies,
    /// and static/runtime separation rules.
    /// </summary>
    public RegistryDefinitionValidationResult ValidateDefinition(IReadOnlyDictionary<string, object?> definition)
    {
        return ValidateDefinition(definition, requireGloballyUniqueId: true);
    }

    /// <summary>
    /// Validates that every required reference resolves to an allowed lifecycle state and that
    /// the transitive reference graph has no self-loop or closed dependency cycle.
    /// </summary>
    public RegistryReferenceValidationResult ValidateReferences(IReadOnlyDictionary<string, object?> definition)
    {
        var rootId = ReadString(definition, "id");
        var additionalDefinitions = string.IsNullOrWhiteSpace(rootId)
            ? new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal)
            : new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal)
            {
                [rootId] = definition,
            };

        return ValidateReferences(definition, additionalDefinitions);
    }

    /// <summary>
    /// Finds exactly one active content definition by tag set, returning AMBIGUOUS_QUERY when
    /// the supplied criteria are not specific enough to identify a single canonical item.
    /// </summary>
    public RegistryUniqueQueryResult QueryUniqueByTags(IEnumerable<string> requiredTags, string? kind = null)
    {
        var required = requiredTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToHashSet(StringComparer.Ordinal);

        var matches = content.Values
            .Where(entity => ReadContentStatus(entity) == ContentStatus.Active)
            .Where(entity => kind is null || string.Equals(ReadString(entity, "kind"), kind, StringComparison.Ordinal))
            .Where(entity => required.IsSubsetOf(ReadStringSet(entity, "tags")))
            .OrderBy(entity => ReadInt(entity, "sort_order", int.MaxValue))
            .ThenBy(entity => ReadString(entity, "id"), StringComparer.Ordinal)
            .ToArray();

        if (matches.Length == 0)
        {
            return new RegistryUniqueQueryResult(
                RegistryUniqueQueryStatus.NotFound,
                null,
                "NOT_FOUND",
                Array.Empty<string>());
        }

        var matchedIds = matches.Select(entity => ReadString(entity, "id")).ToArray();
        if (matches.Length > 1)
        {
            return new RegistryUniqueQueryResult(
                RegistryUniqueQueryStatus.AmbiguousQuery,
                null,
                "AMBIGUOUS_QUERY",
                matchedIds);
        }

        return new RegistryUniqueQueryResult(
            RegistryUniqueQueryStatus.Found,
            CloneEntity(matches[0]),
            null,
            matchedIds);
    }

    /// <summary>
    /// Changes content lifecycle status along the one-way Draft -> Active -> Deprecated -> Retired path.
    /// </summary>
    public ContentLifecycleChangeResult ChangeContentStatus(
        string entityId,
        ContentStatus newStatus,
        string? migrationTargetId = null,
        string? migrationNote = null,
        string? retiredDate = null)
    {
        if (!content.TryGetValue(entityId, out var entity))
        {
            if (retiredIds.TryGetValue(entityId, out var retiredRecord))
            {
                return new ContentLifecycleChangeResult(
                    false,
                    "ERR_ALREADY_RETIRED",
                    entityId,
                    retiredRecord.Status,
                    newStatus);
            }

            return new ContentLifecycleChangeResult(false, "ERR_CONTENT_NOT_FOUND", entityId, null, newStatus);
        }

        var oldStatus = ReadContentStatus(entity);
        if (!IsValidLifecycleTransition(oldStatus, newStatus))
        {
            return new ContentLifecycleChangeResult(false, "ERR_INVALID_STATUS_TRANSITION", entityId, oldStatus, newStatus);
        }

        if (newStatus == ContentStatus.Retired)
        {
            var hint = new ContentMigrationHint(
                entityId,
                ContentStatus.Retired,
                migrationTargetId,
                migrationNote,
                retiredDate);

            content.Remove(entityId);
            retiredIds[entityId] = hint;
        }
        else
        {
            entity["status"] = newStatus.ToString();
            if (newStatus == ContentStatus.Deprecated)
            {
                entity["migration_target"] = migrationTargetId;
                entity["migration_note"] = migrationNote;
            }
        }

        ContentStatusChanged?.Invoke(new ContentStatusChangedEvent(entityId, oldStatus, newStatus));
        return new ContentLifecycleChangeResult(true, null, entityId, oldStatus, newStatus);
    }

    /// <summary>
    /// Resolves lifecycle status and migration guidance without conflating retired IDs with missing IDs.
    /// </summary>
    public ContentLifecycleResolution ResolveContentLifecycle(string entityId)
    {
        if (content.TryGetValue(entityId, out var entity))
        {
            var status = ReadContentStatus(entity);
            var hint = status == ContentStatus.Deprecated
                ? new ContentMigrationHint(
                    entityId,
                    status,
                    ReadNullableString(entity, "migration_target"),
                    ReadNullableString(entity, "migration_note"),
                    ReadNullableString(entity, "retired_date"))
                : null;

            return new ContentLifecycleResolution(entityId, status, hint, IsKnown: true);
        }

        if (retiredIds.TryGetValue(entityId, out var retiredRecord))
        {
            return new ContentLifecycleResolution(entityId, ContentStatus.Retired, retiredRecord, IsKnown: true);
        }

        return new ContentLifecycleResolution(entityId, null, null, IsKnown: false);
    }

    /// <summary>
    /// Resolves old save references to deprecated or retired IDs and returns any available migration hint.
    /// </summary>
    public ContentMigrationHint? ResolveLegacyId(string entityId)
    {
        return ResolveContentLifecycle(entityId).MigrationHint;
    }

    /// <summary>
    /// Returns whether runtime state is referencing a known stable content ID instead of replacing static content.
    /// </summary>
    public bool IsStableRuntimeReference(string entityId, string? expectedKind = null)
    {
        if (content.TryGetValue(entityId, out var entity))
        {
            return expectedKind is null || string.Equals(ReadString(entity, "kind"), expectedKind, StringComparison.Ordinal);
        }

        return retiredIds.ContainsKey(entityId) && expectedKind is null;
    }

    /// <summary>
    /// Rejects attempts to write runtime or player state through the static registry API.
    /// </summary>
    public RegistryRegistrationResult SetEntity(string entityId, IReadOnlyDictionary<string, object?> _)
    {
        return new RegistryRegistrationResult(false, "ERR_READONLY_REGISTRY", entityId);
    }

    /// <summary>
    /// Returns true when the content domain has been marked loaded.
    /// </summary>
    public bool IsDomainLoaded(string domain)
    {
        return loadedDomains.Contains(domain) || GetDomainStatus(domain) == DomainStatus.Complete;
    }

    /// <summary>
    /// Marks a content domain as loaded for downstream readiness checks.
    /// </summary>
    public void SetDomainLoaded(string domain)
    {
        loadedDomains.Add(domain);
        SetDomainStatus(domain, DomainStatus.Complete);
    }

    /// <summary>
    /// Returns the latest loading state for a content domain.
    /// </summary>
    public DomainStatus GetDomainStatus(string domain)
    {
        return domainStatuses.TryGetValue(domain, out var status)
            ? status
            : DomainStatus.Unloaded;
    }

    /// <summary>
    /// Returns the frozen loading state for a content domain captured by a snapshot.
    /// </summary>
    public DomainStatus GetDomainStatus(string domain, RegistrySnapshotHandle handle)
    {
        return snapshots.TryGetValue(handle.Id, out var snapshot)
            && snapshot.DomainStatuses.TryGetValue(domain, out var status)
                ? status
                : DomainStatus.Unloaded;
    }

    /// <summary>
    /// Updates a domain loading state and synchronously notifies listeners.
    /// </summary>
    public void SetDomainStatus(string domain, DomainStatus status)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("Domain must be a stable non-empty ID.", nameof(domain));
        }

        domainStatuses[domain] = status;
        if (status == DomainStatus.Complete)
        {
            loadedDomains.Add(domain);
        }
        else
        {
            loadedDomains.Remove(domain);
        }

        DomainReady?.Invoke(domain, status.ToString().ToUpperInvariant());
    }

    /// <summary>
    /// Checks whether all requested domains are COMPLETE for a decision surface.
    /// </summary>
    public DomainReadinessResult CheckDomainsReady(IEnumerable<string> domains)
    {
        return BuildReadiness(domains, domain => GetDomainStatus(domain));
    }

    /// <summary>
    /// Checks whether all domains for a known decision surface are COMPLETE.
    /// </summary>
    public DomainReadinessResult CheckDecisionSurfaceReady(DecisionSurface surface)
    {
        return CheckDomainsReady(DecisionSurfaceDomains[surface]);
    }

    /// <summary>
    /// Returns the player-safe display fields for a content definition by stable ID.
    /// Only exposes name_key, description_key, icon_ref, tags, and sort_order — never
    /// internal error codes, diagnostic fields, or runtime state.
    /// Returns null when the ID is not found, unloaded, deprecated, or retired.
    /// Callers that need the full entity for logic should use <see cref="QueryById"/> instead.
    /// </summary>
    /// <param name="entityId">Stable content ID in <c>kind.identifier</c> format.</param>
    /// <returns>
    /// Player-safe display fields, or null when the entity is unavailable to player UI
    /// (not found, deprecated, retired, or ID is null/empty).
    /// </returns>
    /// <example>
    /// var info = registry.GetDisplayInfo("location.glass-harbor");
    /// if (info is not null)
    ///     label.Text = Tr(info.NameKey);
    /// </example>
    public PlayerDisplayInfo? GetDisplayInfo(string entityId)
    {
        if (string.IsNullOrEmpty(entityId))
        {
            return null;
        }

        if (!content.TryGetValue(entityId, out var entity))
        {
            return null;
        }

        var status = ReadContentStatus(entity);
        // Depends on Deprecated(2) < Retired(3) in enum ordinal order — keep suppressed statuses adjacent.
        if (status >= ContentStatus.Deprecated)
        {
            return null;
        }

        var tags = ReadStringSet(entity, "tags")
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();

        return new PlayerDisplayInfo(
            NameKey: ReadString(entity, "name_key"),
            DescriptionKey: ReadString(entity, "description_key"),
            IconRef: ReadNullableString(entity, "icon_ref"),
            Tags: tags,
            SortOrder: ReadInt(entity, "sort_order", int.MaxValue));
    }

    /// <summary>
    /// Maps an internal registry query status or domain failure state to a player-safe
    /// error string that contains no ERR_* codes, reference chains, or diagnostic details.
    /// Suitable for direct display in player-facing UI error surfaces.
    /// </summary>
    /// <param name="queryStatus">The <see cref="RegistryQueryStatus"/> returned by a failed query.</param>
    /// <returns>
    /// A short, player-readable message string. Possible values:
    /// <list type="bullet">
    ///   <item><description>"正在加载..." — domain is unloaded or still loading (transient)</description></item>
    ///   <item><description>"无法加载游戏数据——请重试或联系支持" — domain failed to load (persistent)</description></item>
    ///   <item><description>null — no player-visible error (Found, or caller should handle silently)</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// var result = registry.QueryById("location.glass-harbor");
    /// var msg = Registry.GetPlayerSafeError(result.Status);
    /// if (msg is not null) ShowErrorBanner(msg);
    /// </example>
    public static string? GetPlayerSafeError(RegistryQueryStatus queryStatus)
    {
        return queryStatus switch
        {
            RegistryQueryStatus.Found => null,
            // NotFound: logic layer handles missing content; player UI should not show an error
            RegistryQueryStatus.NotFound => null,
            RegistryQueryStatus.Unloaded => "正在加载...",
            RegistryQueryStatus.VersionIncompatible => "游戏内容需要更新——请重启客户端",
            // Deprecated/Retired: content is silently suppressed, not surfaced as an error
            RegistryQueryStatus.Deprecated or RegistryQueryStatus.Retired => null,
            _ => "无法加载游戏数据——请重试或联系支持",
        };
    }

    /// <summary>
    /// Maps a domain loading failure state to a player-safe error string for decision-surface
    /// UI fallback. Returns null when the domain is complete (no error to surface).
    /// </summary>
    /// <param name="domainStatus">The <see cref="DomainStatus"/> of a content domain.</param>
    /// <returns>
    /// A short, player-readable message string. Possible values:
    /// <list type="bullet">
    ///   <item><description>"正在加载..." — domain is loading or partial (transient)</description></item>
    ///   <item><description>"无法加载游戏数据——请重试或联系支持" — domain failed</description></item>
    ///   <item><description>null — domain is complete; no error to surface</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// var status = registry.GetDomainStatus("world");
    /// var msg = Registry.GetPlayerSafeError(status);
    /// if (msg is not null) ShowRetryDialog(msg);
    /// </example>
    public static string? GetPlayerSafeError(DomainStatus domainStatus)
    {
        return domainStatus switch
        {
            DomainStatus.Complete => null,
            DomainStatus.Unloaded or DomainStatus.Loading or DomainStatus.Partial => "正在加载...",
            _ => "无法加载游戏数据——请重试或联系支持",
        };
    }

    /// <summary>
    /// Captures a frozen content and domain-state snapshot for an open decision UI.
    /// </summary>
    public RegistrySnapshotHandle TakeSnapshot(IEnumerable<string> domains)
    {
        var requestedDomains = domains
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var domainSet = requestedDomains.ToHashSet(StringComparer.Ordinal);
        var snapshotContent = content
            .Where(pair => domainSet.Contains(ReadString(pair.Value, "owner_domain")))
            .ToDictionary(
                pair => pair.Key,
                pair => CloneMutable(pair.Value),
                StringComparer.Ordinal);
        var snapshotStatuses = requestedDomains.ToDictionary(
            domain => domain,
            GetDomainStatus,
            StringComparer.Ordinal);
        var handle = new RegistrySnapshotHandle(Guid.NewGuid());

        snapshots[handle.Id] = new RegistrySnapshot(snapshotContent, snapshotStatuses);
        return handle;
    }

    /// <summary>
    /// Releases a previously captured decision UI snapshot.
    /// </summary>
    public bool ReleaseSnapshot(RegistrySnapshotHandle handle)
    {
        return snapshots.Remove(handle.Id);
    }

    /// <summary>
    /// Loads a domain content package, failing at the registry boundary for incompatible schema versions.
    /// </summary>
    public ContentPackageLoadResult LoadContentPackage(RegistryContentPackage package)
    {
        if (package.SchemaVersion != CurrentSchemaVersion)
        {
            SetDomainStatus(package.Domain, DomainStatus.Failed);
            var diagnostic = string.Join(
                Environment.NewLine,
                "VERSION_INCOMPATIBLE",
                $"domain={package.Domain}",
                $"package_schema_version={package.SchemaVersion}",
                $"supported_schema_version={CurrentSchemaVersion}",
                "content layer stopped before partial registration");
            return new ContentPackageLoadResult(
                ContentPackageLoadStatus.VersionIncompatible,
                Success: false,
                ErrorCode: "VERSION_INCOMPATIBLE",
                package.Domain,
                diagnostic);
        }

        SetDomainStatus(package.Domain, DomainStatus.Loading);
        var result = RegisterBatch(package.Definitions);
        if (!result.Success)
        {
            SetDomainStatus(package.Domain, DomainStatus.Failed);
            return new ContentPackageLoadResult(
                ContentPackageLoadStatus.ValidationFailed,
                Success: false,
                result.ErrorCode,
                package.Domain,
                $"VALIDATION_FAILED domain={package.Domain} entity={result.EntityId} error={result.ErrorCode}");
        }

        SetDomainLoaded(package.Domain);
        return new ContentPackageLoadResult(
            ContentPackageLoadStatus.Loaded,
            Success: true,
            ErrorCode: null,
            package.Domain,
            $"LOADED domain={package.Domain} schema_version={package.SchemaVersion}");
    }

    /// <summary>
    /// Generates one primary diagnostic event from normalized findings, with lower-precedence errors attached.
    /// </summary>
    public RegistryDiagnosticEvent GenerateDiagnostic(
        IReadOnlyDictionary<string, object?> definition,
        IEnumerable<RegistryDiagnosticFinding> findings,
        string contentPackage = "runtime",
        string sourceRef = "",
        string queryContext = "")
    {
        var orderedFindings = findings
            .Where(finding => !string.IsNullOrWhiteSpace(finding.ErrorCode))
            .OrderBy(DiagnosticPriority)
            .ThenBy(finding => SeverityRank(SeverityFor(finding)))
            .ThenBy(finding => finding.ErrorCode, StringComparer.Ordinal)
            .ThenBy(finding => EffectiveContentId(finding, definition), StringComparer.Ordinal)
            .ThenBy(finding => finding.FieldPath, StringComparer.Ordinal)
            .ToArray();

        if (orderedFindings.Length == 0)
        {
            orderedFindings = [new RegistryDiagnosticFinding(
                "REGISTRY_DIAGNOSTIC_OK",
                ReadString(definition, "id"),
                string.Empty,
                "Registry diagnostic completed without errors.",
                Array.Empty<string>(),
                new Dictionary<string, object?>())];
        }

        var primary = orderedFindings[0];
        var primaryContentId = EffectiveContentId(primary, definition);
        var timestamp = DateTimeOffset.UtcNow;
        var related = orderedFindings
            .Skip(1)
            .Select(ToRelatedDiagnostic)
            .ToArray();

        return new RegistryDiagnosticEvent(
            $"{timestamp.ToUnixTimeMilliseconds()}-{SanitizeEventIdPart(primaryContentId)}-{primary.ErrorCode}",
            timestamp,
            SeverityFor(primary),
            primary.ErrorCode,
            primaryContentId,
            ReadString(definition, "kind"),
            ReadString(definition, "status"),
            ReadInt(definition, "schema_version", 0),
            ReadString(definition, "owner_domain"),
            contentPackage,
            sourceRef,
            primary.FieldPath,
            primary.ReferenceChain,
            queryContext,
            BlockingScopeFor(primary.ErrorCode),
            SuggestedActionFor(primary.ErrorCode),
            related);
    }

    /// <summary>
    /// Generates one primary diagnostic event from schema and reference validators.
    /// </summary>
    public RegistryDiagnosticEvent GenerateDiagnostic(
        IReadOnlyDictionary<string, object?> definition,
        IEnumerable<RegistryDiagnostic> definitionDiagnostics,
        IEnumerable<RegistryReferenceDiagnostic> referenceDiagnostics,
        string contentPackage = "runtime",
        string sourceRef = "",
        string queryContext = "")
    {
        var findings = definitionDiagnostics
            .Select(diagnostic => new RegistryDiagnosticFinding(
                diagnostic.ErrorCode,
                diagnostic.ContentId,
                diagnostic.Field,
                diagnostic.Message,
                Array.Empty<string>(),
                diagnostic.Details))
            .Concat(referenceDiagnostics.Select(diagnostic => new RegistryDiagnosticFinding(
                diagnostic.ErrorCode,
                diagnostic.SourceId,
                "references",
                diagnostic.Message,
                diagnostic.ReferenceChain,
                new Dictionary<string, object?>
                {
                    ["target_id"] = diagnostic.TargetId,
                    ["new_active_reference"] = diagnostic.ErrorCode == "ERR_REFERENCE_TO_DEPRECATED",
                })));

        return GenerateDiagnostic(definition, findings, contentPackage, sourceRef, queryContext);
    }

    /// <summary>
    /// Sorts diagnostic events by severity, precedence, and stable identity fields.
    /// </summary>
    public static IReadOnlyList<RegistryDiagnosticEvent> SortDiagnostics(
        IEnumerable<RegistryDiagnosticEvent> diagnostics)
    {
        return diagnostics
            .OrderBy(diagnostic => SeverityRank(diagnostic.Severity))
            .ThenBy(diagnostic => DiagnosticPriority(diagnostic.ErrorCode))
            .ThenBy(diagnostic => diagnostic.ErrorCode, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.ContentId, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.FieldPath, StringComparer.Ordinal)
            .ToArray();
    }

    private static DomainReadinessResult BuildReadiness(
        IEnumerable<string> domains,
        Func<string, DomainStatus> statusForDomain)
    {
        var statuses = domains
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(domain => domain, statusForDomain, StringComparer.Ordinal);
        var blocked = statuses
            .Where(pair => pair.Value != DomainStatus.Complete)
            .Select(pair => pair.Key)
            .ToArray();

        return new DomainReadinessResult(
            blocked.Length == 0,
            blocked,
            statuses);
    }

    private static RegistryRelatedDiagnostic ToRelatedDiagnostic(RegistryDiagnosticFinding finding)
    {
        return new RegistryRelatedDiagnostic(
            SeverityFor(finding),
            finding.ErrorCode,
            finding.ContentId,
            finding.FieldPath,
            finding.ReferenceChain,
            BlockingScopeFor(finding.ErrorCode),
            SuggestedActionFor(finding.ErrorCode));
    }

    private static int DiagnosticPriority(RegistryDiagnosticFinding finding)
    {
        return DiagnosticPriority(finding.ErrorCode);
    }

    private static int DiagnosticPriority(string errorCode)
    {
        return DiagnosticPriorities.TryGetValue(errorCode, out var priority)
            ? priority
            : 99;
    }

    private static int SeverityRank(string severity)
    {
        return severity switch
        {
            "fatal" => 0,
            "error" => 1,
            "warning" => 2,
            "info" => 3,
            _ => 4,
        };
    }

    private static string SeverityFor(string errorCode)
    {
        return errorCode switch
        {
            "REGISTRY_DIAGNOSTIC_OK" => "info",
            "ERR_CONTENT_PACKAGE_VERSION" or "VERSION_INCOMPATIBLE" => "fatal",
            "ERR_REFERENCE_TO_DEPRECATED" => "warning",
            _ when DiagnosticPriorities.ContainsKey(errorCode) => "error",
            _ => "warning",
        };
    }

    private static string SeverityFor(RegistryDiagnosticFinding finding)
    {
        if (finding.ErrorCode == "ERR_REFERENCE_TO_DEPRECATED"
            && finding.Details.TryGetValue("new_active_reference", out var value)
            && value is bool newActiveReference
            && newActiveReference)
        {
            return "error";
        }

        return SeverityFor(finding.ErrorCode);
    }

    private static string BlockingScopeFor(string errorCode)
    {
        return DiagnosticPriority(errorCode) switch
        {
            1 => "registry",
            2 => "package",
            3 or 4 or 5 or 6 or 7 => "item",
            8 => "runtime-query",
            _ => "item",
        };
    }

    private static string SuggestedActionFor(string errorCode)
    {
        return errorCode switch
        {
            "ERR_CONTENT_PACKAGE_VERSION" or "VERSION_INCOMPATIBLE" => "Install a compatible content package for this build.",
            "ERR_DUPLICATE_ID" => "Rename or remove the duplicate stable ID.",
            "ERR_ID_REUSE" => "Use a new stable ID or restore the retired migration record.",
            "ERR_INVALID_ID_FORMAT" or "ERR_DEFINITION_VALIDITY_U" => "Replace the value with a valid stable content ID.",
            "ERR_ID_NORMALIZATION_COLLISION" => "Rename one colliding ID after Unicode normalization.",
            "ERR_SCHEMA_INVALID" or "ERR_SCHEMA_MISSING_REQUIRED_FIELD" => "Update the definition to match its kind schema.",
            "ERR_RUNTIME_FIELD_IN_STATIC_DATA" or "ERR_READONLY_REGISTRY" => "Move runtime state to the owning domain system.",
            "ERR_MISSING_REFERENCE" => "Add the referenced content or correct the stable ID.",
            "UNLOADED_REFERENCE" => "Load the referenced domain before resolving this content.",
            "ERR_REFERENCE_TO_DRAFT" => "Promote the target content to Active or remove the reference.",
            "ERR_REFERENCE_TO_DEPRECATED" => "Migrate the reference to an Active replacement.",
            "ERR_REFERENCE_TO_RETIRED" => "Replace the retired reference with its migration target.",
            "ERR_REFERENCE_CYCLE" or "ERR_REFERENCE_DEPTH_EXCEEDED" => "Break the reference graph cycle or reduce reference depth.",
            "ERR_INVALID_SORT_KEY" => "Provide a non-negative integer sort_order.",
            "AMBIGUOUS_QUERY" => "Add query constraints until exactly one content item matches.",
            "ERR_UNSTABLE_IDENTIFIER" => "Use a stable content ID instead of display text, path, or index.",
            _ => "Inspect the content definition and correct the reported field.",
        };
    }

    private static string EffectiveContentId(
        RegistryDiagnosticFinding finding,
        IReadOnlyDictionary<string, object?> definition)
    {
        return string.IsNullOrWhiteSpace(finding.ContentId)
            ? ReadString(definition, "id")
            : finding.ContentId;
    }

    private static string SanitizeEventIdPart(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "registry"
            : Regex.Replace(value, "[^a-zA-Z0-9_.-]", "_");
    }

    private RegistryReferenceValidationResult ValidateReferences(
        IReadOnlyDictionary<string, object?> definition,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> additionalDefinitions)
    {
        var diagnostics = new List<RegistryReferenceDiagnostic>();
        var rootId = ReadString(definition, "id");
        if (string.IsNullOrWhiteSpace(rootId))
        {
            return new RegistryReferenceValidationResult(false, false, [
                new RegistryReferenceDiagnostic(
                    "ERR_INVALID_ID_FORMAT",
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    "Reference validation requires a stable source ID."),
            ]);
        }

        VisitReferences(
            definition,
            additionalDefinitions,
            new List<string> { rootId },
            diagnostics);

        return new RegistryReferenceValidationResult(
            diagnostics.Count == 0,
            diagnostics.Count == 0,
            diagnostics);
    }

    private void VisitReferences(
        IReadOnlyDictionary<string, object?> entity,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> additionalDefinitions,
        List<string> path,
        List<RegistryReferenceDiagnostic> diagnostics)
    {
        if (path.Count > MaxReferencesPerItem + 1)
        {
            diagnostics.Add(new RegistryReferenceDiagnostic(
                "ERR_REFERENCE_DEPTH_EXCEEDED",
                path[0],
                path[^1],
                ChainWithStatuses(path, additionalDefinitions),
                "Reference graph exceeded max_references_per_item."));
            return;
        }

        var sourceStatus = ReadContentStatus(entity);
        foreach (var reference in ExtractReferenceSpecs(entity))
        {
            if (string.IsNullOrWhiteSpace(reference.Id))
            {
                continue;
            }

            var cycleStart = path.IndexOf(reference.Id);
            if (cycleStart >= 0)
            {
                var cycle = path.Skip(cycleStart).Concat([reference.Id]).ToArray();
                diagnostics.Add(new RegistryReferenceDiagnostic(
                    "ERR_REFERENCE_CYCLE",
                    path[0],
                    reference.Id,
                    ChainWithStatuses(cycle, additionalDefinitions),
                    "Reference graph contains a self-loop or closed dependency cycle."));
                continue;
            }

            var resolution = ResolveReferenceTarget(reference.Id, additionalDefinitions);
            if (!resolution.IsKnown)
            {
                if (reference.Optional)
                {
                    continue;
                }

                diagnostics.Add(new RegistryReferenceDiagnostic(
                    resolution.IsUnloaded ? "UNLOADED_REFERENCE" : "ERR_MISSING_REFERENCE",
                    path[0],
                    reference.Id,
                    ChainWithStatuses(path.Concat([reference.Id]), additionalDefinitions),
                    resolution.IsUnloaded
                        ? "Reference target belongs to an unloaded domain or kind."
                        : "Required reference target could not be resolved."));
                continue;
            }

            if (sourceStatus == ContentStatus.Active)
            {
                var statusError = resolution.Status switch
                {
                    ContentStatus.Draft => "ERR_REFERENCE_TO_DRAFT",
                    ContentStatus.Deprecated => "ERR_REFERENCE_TO_DEPRECATED",
                    ContentStatus.Retired => "ERR_REFERENCE_TO_RETIRED",
                    _ => null,
                };

                if (statusError is not null)
                {
                    diagnostics.Add(new RegistryReferenceDiagnostic(
                        statusError,
                        path[0],
                        reference.Id,
                        ChainWithStatuses(path.Concat([reference.Id]), additionalDefinitions),
                        "Active content cannot depend on a target with this lifecycle status."));
                }
            }

            if (resolution.Entity is null)
            {
                continue;
            }

            path.Add(reference.Id);
            VisitReferences(resolution.Entity, additionalDefinitions, path, diagnostics);
            path.RemoveAt(path.Count - 1);
        }
    }

    private ReferenceTargetResolution ResolveReferenceTarget(
        string entityId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> additionalDefinitions)
    {
        if (additionalDefinitions.TryGetValue(entityId, out var additional))
        {
            return ReferenceTargetResolution.Found(additional, ReadContentStatus(additional));
        }

        if (content.TryGetValue(entityId, out var entity))
        {
            return ReferenceTargetResolution.Found(entity, ReadContentStatus(entity));
        }

        if (retiredIds.TryGetValue(entityId, out var retiredRecord))
        {
            return ReferenceTargetResolution.Found(null, retiredRecord.Status);
        }

        return IsLoadedIdFamily(entityId)
            ? ReferenceTargetResolution.Missing()
            : ReferenceTargetResolution.Unloaded();
    }

    private IReadOnlyList<string> ChainWithStatuses(
        IEnumerable<string> ids,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> additionalDefinitions)
    {
        return ids.Select(id =>
        {
            var status = ResolveReferenceTarget(id, additionalDefinitions).Status;
            return status is null ? id : $"{id}({status})";
        }).ToArray();
    }

    private static IEnumerable<RegistryReferenceSpec> ExtractReferenceSpecs(IReadOnlyDictionary<string, object?> entity)
    {
        if (!entity.TryGetValue("references", out var references) || references is null)
        {
            yield break;
        }

        if (references is string singleReference)
        {
            yield return new RegistryReferenceSpec(singleReference, Optional: false);
            yield break;
        }

        if (references is not System.Collections.IEnumerable enumerable)
        {
            yield break;
        }

        foreach (var item in enumerable)
        {
            switch (item)
            {
                case null:
                    continue;
                case string id:
                    yield return new RegistryReferenceSpec(id, Optional: false);
                    continue;
                case IReadOnlyDictionary<string, object?> typedReference:
                    yield return ReferenceSpecFromDictionary(typedReference);
                    continue;
                case System.Collections.IDictionary dictionaryReference:
                    yield return ReferenceSpecFromDictionary(DictionaryToObjectMap(dictionaryReference));
                    continue;
            }
        }
    }

    private static RegistryReferenceSpec ReferenceSpecFromDictionary(IReadOnlyDictionary<string, object?> reference)
    {
        var id = ReadFirstString(reference, ["id", "target_id", "ref_id", "content_id"]);
        return new RegistryReferenceSpec(id, ReadBool(reference, "optional"));
    }

    private static HashSet<string> ReadStringSet(IReadOnlyDictionary<string, object?> entity, string key)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (value is string text)
        {
            return new HashSet<string>([text], StringComparer.Ordinal);
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable
                .Cast<object?>()
                .Where(item => item is not null)
                .Select(item => item?.ToString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.Ordinal);
        }

        return new HashSet<string>(StringComparer.Ordinal);
    }

    private static string ReadFirstString(IReadOnlyDictionary<string, object?> entity, IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var value = ReadString(entity, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> entity, string key)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };
    }

    private static ContentStatus ReadContentStatus(IReadOnlyDictionary<string, object?> entity)
    {
        if (entity.TryGetValue("status", out var statusValue)
            && statusValue is not null
            && Enum.TryParse<ContentStatus>(statusValue.ToString(), ignoreCase: true, out var status))
        {
            return status;
        }

        return (ContentStatus)ReadInt(entity, "content_status", (int)ContentStatus.Draft);
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> entity, string key)
    {
        return entity.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static string? ReadNullableString(IReadOnlyDictionary<string, object?> entity, string key)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> entity, string key, int fallback = 0)
    {
        if (!entity.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            double doubleValue => checked((int)doubleValue),
            float floatValue => checked((int)floatValue),
            string stringValue when int.TryParse(stringValue, CultureInfo.InvariantCulture, out var parsed) => parsed,
            ContentStatus status => (int)status,
            _ => fallback,
        };
    }

    private static Dictionary<string, object?> CloneEntity(IReadOnlyDictionary<string, object?> entity)
    {
        return CloneMutable(entity);
    }

    private static Dictionary<string, object?> CloneMutable(IReadOnlyDictionary<string, object?> entity)
    {
        return entity.ToDictionary(pair => pair.Key, pair => CloneValue(pair.Value), StringComparer.Ordinal);
    }

    private static Dictionary<string, object?> MigrationHintToEntity(ContentMigrationHint hint)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = hint.OriginalId,
            ["status"] = hint.Status.ToString(),
            ["migration_target"] = hint.SuggestedReplacementId,
            ["migration_note"] = hint.MigrationNote,
            ["retired_date"] = hint.RetiredDate,
        };
    }

    private static bool IsValidLifecycleTransition(ContentStatus oldStatus, ContentStatus newStatus)
    {
        return (oldStatus, newStatus) is
            (ContentStatus.Draft, ContentStatus.Active) or
            (ContentStatus.Active, ContentStatus.Deprecated) or
            (ContentStatus.Deprecated, ContentStatus.Retired);
    }

    private static bool IsFantasyCriticalRedefinition(
        IReadOnlyDictionary<string, object?> existing,
        IReadOnlyDictionary<string, object?> replacement)
    {
        var existingKind = ReadString(existing, "kind");
        var replacementKind = ReadString(replacement, "kind");
        if (!FantasyCriticalKinds.Contains(existingKind, StringComparer.Ordinal)
            && !FantasyCriticalKinds.Contains(replacementKind, StringComparer.Ordinal))
        {
            return false;
        }

        foreach (var field in SemanticIdentityFields)
        {
            if (!existing.TryGetValue(field, out var existingValue)
                || !replacement.TryGetValue(field, out var replacementValue))
            {
                continue;
            }

            if (!ValuesEquivalent(existingValue, replacementValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ValuesEquivalent(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left is string || right is string)
        {
            return string.Equals(left.ToString(), right.ToString(), StringComparison.Ordinal);
        }

        if (left is System.Collections.IEnumerable leftEnumerable
            && right is System.Collections.IEnumerable rightEnumerable)
        {
            return leftEnumerable
                .Cast<object?>()
                .Select(value => value?.ToString() ?? string.Empty)
                .SequenceEqual(
                    rightEnumerable.Cast<object?>().Select(value => value?.ToString() ?? string.Empty),
                    StringComparer.Ordinal);
        }

        return Equals(left, right);
    }

    private RegistryDefinitionValidationResult ValidateDefinition(
        IReadOnlyDictionary<string, object?> definition,
        bool requireGloballyUniqueId)
    {
        var diagnostics = new List<RegistryDiagnostic>();
        var id = ReadString(definition, "id");
        var kind = ReadString(definition, "kind");

        var hasUniqueId = StableIdPattern.IsMatch(id)
            && (!requireGloballyUniqueId || (!content.ContainsKey(id) && !retiredIds.ContainsKey(id)));
        if (!hasUniqueId)
        {
            diagnostics.Add(CreateDiagnostic(
                "ERR_DEFINITION_VALIDITY_U",
                id,
                "id",
                "U",
                "Content ID must be a unique stable ID before schema validation."));
        }

        var matchesKindSchema = true;
        if (!KindRequiredFields.ContainsKey(kind))
        {
            matchesKindSchema = false;
            diagnostics.Add(CreateDiagnostic(
                "ERR_SCHEMA_INVALID",
                id,
                "kind",
                "K",
                "Content kind is not in the controlled vocabulary.",
                new Dictionary<string, object?> { ["allowed_values"] = ControlledVocabularies["kind"] }));
        }

        if (!string.IsNullOrWhiteSpace(id)
            && !string.IsNullOrWhiteSpace(kind)
            && !IdPrefixMatchesKind(id, kind))
        {
            matchesKindSchema = false;
            diagnostics.Add(CreateDiagnostic(
                "ERR_SCHEMA_INVALID",
                id,
                "kind",
                "K",
                "Content kind must match the stable ID prefix."));
        }

        if (ReadInt(definition, "schema_version", 0) != CurrentSchemaVersion)
        {
            matchesKindSchema = false;
            diagnostics.Add(CreateDiagnostic(
                "ERR_SCHEMA_INVALID",
                id,
                "schema_version",
                "K",
                $"Content schema_version must be {CurrentSchemaVersion}.",
                new Dictionary<string, object?> { ["allowed_values"] = new[] { CurrentSchemaVersion } }));
        }

        foreach (var diagnostic in ValidateControlledVocabularies(definition, id))
        {
            matchesKindSchema = false;
            diagnostics.Add(diagnostic);
        }

        var requiredFieldsPresent = true;
        foreach (var field in RequiredFieldsForKind(kind))
        {
            if (HasRequiredValue(definition, field))
            {
                continue;
            }

            requiredFieldsPresent = false;
            diagnostics.Add(CreateDiagnostic(
                "ERR_SCHEMA_MISSING_REQUIRED_FIELD",
                id,
                field,
                "R",
                "Definition is missing a required schema field."));
        }

        var runtimeFields = FindRuntimeFields(definition).ToArray();
        var hasNoRuntimeFields = runtimeFields.Length == 0;
        foreach (var field in runtimeFields)
        {
            diagnostics.Add(CreateDiagnostic(
                "ERR_RUNTIME_FIELD_IN_STATIC_DATA",
                id,
                field,
                "S",
                "Static content definitions cannot include runtime state fields."));
        }

        return new RegistryDefinitionValidationResult(
            hasUniqueId && matchesKindSchema && requiredFieldsPresent && hasNoRuntimeFields,
            hasUniqueId,
            matchesKindSchema,
            requiredFieldsPresent,
            hasNoRuntimeFields,
            diagnostics);
    }

    private static IEnumerable<string> RequiredFieldsForKind(string kind)
    {
        foreach (var field in CommonRequiredFields)
        {
            yield return field;
        }

        if (!HasStatusFieldRequirement(kind))
        {
            yield return "status";
        }

        if (!KindRequiredFields.TryGetValue(kind, out var requiredFields))
        {
            yield break;
        }

        foreach (var field in requiredFields)
        {
            yield return field;
        }
    }

    private static bool HasStatusFieldRequirement(string kind)
    {
        return string.IsNullOrWhiteSpace(kind);
    }

    private static bool HasRequiredValue(IReadOnlyDictionary<string, object?> definition, string field)
    {
        if (field == "status" && !definition.ContainsKey("status") && definition.ContainsKey("content_status"))
        {
            return definition["content_status"] is not null;
        }

        if (!definition.TryGetValue(field, out var value) || value is null)
        {
            return false;
        }

        if (value is string stringValue)
        {
            return !string.IsNullOrWhiteSpace(stringValue);
        }

        if (field == "references")
        {
            return true;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object?>().Any();
        }

        return true;
    }

    private static bool IdPrefixMatchesKind(string id, string kind)
    {
        var separator = id.IndexOf('.', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return false;
        }

        var idPrefix = id[..separator].Replace('_', '-');
        return string.Equals(idPrefix, kind, StringComparison.Ordinal);
    }

    private static IEnumerable<RegistryDiagnostic> ValidateControlledVocabularies(
        IReadOnlyDictionary<string, object?> definition,
        string id)
    {
        foreach (var (field, allowedValues) in ControlledVocabularies.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!definition.TryGetValue(field, out var value) || value is null)
            {
                continue;
            }

            var invalidValues = ValuesForVocabularyCheck(value)
                .Where(fieldValue => !allowedValues.Contains(fieldValue, StringComparer.Ordinal))
                .ToArray();
            if (invalidValues.Length == 0)
            {
                continue;
            }

            yield return CreateDiagnostic(
                "ERR_SCHEMA_INVALID",
                id,
                field,
                "K",
                "Controlled vocabulary value is not allowed.",
                new Dictionary<string, object?>
                {
                    ["invalid_values"] = invalidValues,
                    ["allowed_values"] = allowedValues,
                });
        }
    }

    private static IEnumerable<string> ValuesForVocabularyCheck(object value)
    {
        if (value is string stringValue)
        {
            yield return stringValue;
            yield break;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    yield return item.ToString() ?? string.Empty;
                }
            }
        }
    }

    private static IEnumerable<string> FindRuntimeFields(IReadOnlyDictionary<string, object?> definition)
    {
        foreach (var (key, value) in definition)
        {
            if (RuntimeFieldDenylist.Any(denied => IsRuntimeFieldName(key, denied)))
            {
                yield return key;
            }

            if (value is IReadOnlyDictionary<string, object?> nested)
            {
                foreach (var nestedField in FindRuntimeFields(nested))
                {
                    yield return $"{key}.{nestedField}";
                }
            }
            else if (value is System.Collections.IDictionary nestedDictionary)
            {
                foreach (var nestedField in FindRuntimeFields(DictionaryToObjectMap(nestedDictionary)))
                {
                    yield return $"{key}.{nestedField}";
                }
            }
            else if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    if (item is IReadOnlyDictionary<string, object?> nestedItem)
                    {
                        foreach (var nestedField in FindRuntimeFields(nestedItem))
                        {
                            yield return $"{key}[{index}].{nestedField}";
                        }
                    }
                    else if (item is System.Collections.IDictionary nestedDictionaryItem)
                    {
                        foreach (var nestedField in FindRuntimeFields(DictionaryToObjectMap(nestedDictionaryItem)))
                        {
                            yield return $"{key}[{index}].{nestedField}";
                        }
                    }

                    index++;
                }
            }
        }
    }

    private static bool IsRuntimeFieldName(string key, string denied)
    {
        return string.Equals(key, denied, StringComparison.Ordinal)
            || string.Equals(key, $"current_{denied}", StringComparison.Ordinal)
            || key.EndsWith($"_{denied}", StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> DictionaryToObjectMap(System.Collections.IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var key in dictionary.Keys)
        {
            if (key is null)
            {
                continue;
            }

            result[key.ToString() ?? string.Empty] = dictionary[key];
        }

        return result;
    }

    private static RegistryDiagnostic CreateDiagnostic(
        string errorCode,
        string contentId,
        string field,
        string validityTerm,
        string message,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        var diagnosticDetails = new Dictionary<string, object?>(details ?? new Dictionary<string, object?>(), StringComparer.Ordinal)
        {
            ["validity_term"] = validityTerm,
        };

        return new RegistryDiagnostic(
            "registry_definition_validation_failed",
            "error",
            errorCode,
            contentId,
            field,
            message,
            diagnosticDetails);
    }

    private static object? CloneValue(object? value)
    {
        if (value is null || value is string || value.GetType().IsValueType)
        {
            return value;
        }

        if (value is IReadOnlyDictionary<string, object?> dictionary)
        {
            return CloneMutable(dictionary);
        }

        if (value is System.Collections.IDictionary nonGenericDictionary)
        {
            var clone = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var key in nonGenericDictionary.Keys)
            {
                if (key is null)
                {
                    continue;
                }

                clone[key.ToString() ?? string.Empty] = CloneValue(nonGenericDictionary[key]);
            }

            return clone;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object?>().Select(CloneValue).ToArray();
        }

        return value;
    }

    private bool IsLoadedIdFamily(string entityId)
    {
        var separator = entityId.IndexOf('.', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return false;
        }

        var kind = entityId[..separator];
        return loadedKinds.Contains(kind) || domainKindMap.Values.Any(kinds => kinds.Contains(kind));
    }

    private sealed record ReferenceTargetResolution(
        bool IsKnown,
        bool IsUnloaded,
        IReadOnlyDictionary<string, object?>? Entity,
        ContentStatus? Status)
    {
        public static ReferenceTargetResolution Found(
            IReadOnlyDictionary<string, object?>? entity,
            ContentStatus status)
        {
            return new ReferenceTargetResolution(true, false, entity, status);
        }

        public static ReferenceTargetResolution Missing()
        {
            return new ReferenceTargetResolution(false, false, null, null);
        }

        public static ReferenceTargetResolution Unloaded()
        {
            return new ReferenceTargetResolution(false, true, null, null);
        }
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> ApplyResultLimit(
        IEnumerable<IReadOnlyDictionary<string, object?>> results)
    {
        if (MaxQueryResultCount > 0)
        {
            return results.Take(MaxQueryResultCount).ToList();
        }

        return results.ToList();
    }

    private sealed record RegistrySnapshot(
        Dictionary<string, Dictionary<string, object?>> Content,
        Dictionary<string, DomainStatus> DomainStatuses);
}

/// <summary>
/// Prototype content definitions used to validate the C# Foundation migration.
/// </summary>
public static class RegistryBootstrap
{
    /// <summary>
    /// Registers the current prototype static content set.
    /// </summary>
    public static void Bootstrap(Registry registry)
    {
        var result = registry.RegisterBatch(CreatePrototypeDefinitions());
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Registry bootstrap failed for '{result.EntityId}': {result.ErrorCode}");
        }

        registry.SetDomainLoaded("core_content");
    }

    private static IEnumerable<IReadOnlyDictionary<string, object?>> CreatePrototypeDefinitions()
    {
        yield return Entity("location.glass-harbor", "location", "玻璃港", 1, new()
        {
            ["region_tag"] = "starter-sea",
            ["location_kind"] = "settlement",
            ["service_tags"] = new[] { "market", "repair" },
            ["local_identity_tags"] = new[] { "glass-buoys" },
            ["settlement_need_tags"] = new[] { "navigation-aid", "trade-link" },
            ["type"] = "settlement",
            ["description"] = "起始空港聚落——修复前的第一站",
        });
        yield return Entity("location.glass-harbor-outskirts", "location", "玻璃港近郊", 2, new()
        {
            ["region_tag"] = "starter-sea",
            ["location_kind"] = "outskirts",
            ["service_tags"] = new[] { "repair" },
            ["local_identity_tags"] = new[] { "glass-buoys" },
            ["settlement_need_tags"] = new[] { "safety", "navigation-aid" },
            ["type"] = "outskirts",
            ["description"] = "玻璃港附近郊区——灯塔修复节点所在地",
        });
        yield return Entity("location.sky-reef-outpost", "location", "空礁前哨", 3, new()
        {
            ["region_tag"] = "sky-reef",
            ["location_kind"] = "outpost",
            ["service_tags"] = new[] { "navigation" },
            ["local_identity_tags"] = new[] { "reef-beacons" },
            ["settlement_need_tags"] = new[] { "navigation-aid", "safety" },
            ["type"] = "outpost",
            ["description"] = "安全航线的目的地——小型探索前哨",
        });
        yield return Entity("location.cloudwatch-ruins", "location", "云观站废墟", 4, new()
        {
            ["region_tag"] = "storm-belt",
            ["location_kind"] = "ruins",
            ["service_tags"] = new[] { "exploration" },
            ["local_identity_tags"] = new[] { "cloudwatch" },
            ["settlement_need_tags"] = new[] { "repair-materials", "safety" },
            ["type"] = "ruins",
            ["description"] = "高风险航线的目的地——探索搜撤场景",
        });

        yield return Entity("route.sky-reef-arc-01", "route", "空礁航线", 1, new()
        {
            ["origin_location_id"] = "location.glass-harbor",
            ["destination_id"] = "location.sky-reef-outpost",
            ["origin_id"] = "location.glass-harbor",
            ["traversable"] = false,
            ["hazard_tags"] = new[] { "safe" },
            ["distance_band"] = "short",
            ["encounter_check_count"] = 5,
            ["required_repair_id"] = "repair_node.starlight_dock",
        });
        yield return Entity("route.storm-cut-01", "route", "风暴捷径", 2, new()
        {
            ["origin_location_id"] = "location.glass-harbor",
            ["destination_id"] = "location.cloudwatch-ruins",
            ["origin_id"] = "location.glass-harbor",
            ["traversable"] = true,
            ["hazard_tags"] = new[] { "mist", "low-visibility", "raider" },
            ["distance_band"] = "medium",
            ["encounter_check_count"] = 10,
        });

        yield return Entity("resource.repair_kit", "resource", "维修套件", 1, new()
        {
            ["unit"] = "kit",
            ["stack_rule"] = "stackable",
            ["material_tags"] = new[] { "repair-material" },
            ["max_stack"] = 99,
            ["supply_class"] = "repair",
        });
        yield return Entity("resource.basic_supply", "resource", "基础补给品", 2, new()
        {
            ["unit"] = "crate",
            ["stack_rule"] = "stackable",
            ["material_tags"] = new[] { "food" },
            ["max_stack"] = 99,
            ["supply_class"] = "basic",
        });
        yield return Entity("resource.cloud_coin", "resource", "云海币", 3, new()
        {
            ["unit"] = "coin",
            ["stack_rule"] = "stackable",
            ["material_tags"] = new[] { "currency" },
            ["max_stack"] = 9999,
            ["supply_class"] = "basic",
        });
        yield return Entity("resource.ancient_lens", "resource", "古代透镜", 4, new()
        {
            ["unit"] = "piece",
            ["stack_rule"] = "unique",
            ["material_tags"] = new[] { "intel" },
            ["max_stack"] = 1,
            ["supply_class"] = "intel",
            ["cat_sniff_signature"] = "ancient_optics",
        });
        yield return Entity("resource.navigation_chart", "resource", "旧航海图", 5, new()
        {
            ["unit"] = "chart",
            ["stack_rule"] = "stackable",
            ["material_tags"] = new[] { "navigation-aid" },
            ["max_stack"] = 20,
            ["supply_class"] = "navigation",
        });
        yield return Entity("resource.beacon_crystal", "resource", "信标水晶", 6, new()
        {
            ["unit"] = "crystal",
            ["stack_rule"] = "stackable",
            ["material_tags"] = new[] { "repair-material" },
            ["max_stack"] = 99,
            ["supply_class"] = "repair",
        });

        yield return Entity("repair_node.starlight_dock", "repair-node", "星光灯塔", 1, new()
        {
            ["node_id"] = "repair_node.starlight_dock",
            ["location_id"] = "location.glass-harbor-outskirts",
            ["linked_location_id"] = "location.glass-harbor-outskirts",
            ["node_kind"] = "beacon",
            ["restoration_theme"] = "lighthouse",
            ["settlement_need_tags"] = new[] { "navigation-aid", "safety" },
            ["repair_visible_state_tags"] = new[] { "dark", "lit", "connected" },
            ["required_materials"] = new Dictionary<string, int>
            {
                ["resource.repair_kit"] = 4,
                ["resource.basic_supply"] = 4,
            },
            ["required_resources"] = new Dictionary<string, int>
            {
                ["resource.repair_kit"] = 4,
                ["resource.basic_supply"] = 4,
            },
            ["unlocked_routes"] = new[] { "route.sky-reef-arc-01" },
            ["route_enhancement"] = new Dictionary<string, object?>
            {
                ["effect"] = "hazard_reduction",
                ["magnitude"] = 0.3,
            },
            ["pre_repair_route_state"] = new Dictionary<string, object?>
            {
                ["traversable"] = false,
            },
            ["visual_state_anchor"] = "anchor.starlight_dock_beacon",
            ["unlocks"] = new Dictionary<string, string[]>
            {
                ["routes"] = ["route.sky-reef-arc-01"],
                ["stalls"] = ["stall.navigator_supply"],
                ["abilities"] = ["ability.lighthouse-signal-interpretation"],
            },
        });

        yield return Entity("threat.guard-sentinel", "threat", "警戒哨兵", 1, new()
        {
            ["threat_class"] = "guard",
            ["encounter_tags"] = new[] { "raider" },
            ["counter_tags"] = new[] { "evade", "retreat" },
            ["severity_tier"] = "moderate",
            ["threat_type"] = "guard",
            ["threat_category"] = "guard",
            ["trigger_radius"] = 6.0,
            ["trigger_radius_min"] = 4.0,
            ["trigger_radius_max"] = 6.0,
            ["trigger_probability"] = 0.70,
            ["full_damage_min"] = 8,
            ["full_damage_max"] = 12,
            ["hull_damage_min"] = 8,
            ["hull_damage_max"] = 12,
            ["module_damage_chance"] = 0.30,
            ["emergency_cost_repair_kit"] = 1,
            ["knockback_distance_tanked"] = 8.0,
            ["knockback_distance_retreat"] = 10.0,
            ["can_be_suppressed"] = true,
            ["can_retreat"] = true,
        });

        yield return Entity("companion.sky-cat", "companion", "航海猫", 1, new()
        {
            ["role_tags"] = new[] { "scout" },
            ["origin_location_id"] = "location.glass-harbor",
            ["archetype_tags"] = new[] { "sky-cat" },
            ["companion_type"] = "cat",
            ["abilities"] = new[] { "scout_sniff" },
            ["max_confidence"] = 66,
        });
    }

    private static Dictionary<string, object?> Entity(
        string id,
        string kind,
        string displayName,
        int sortOrder,
        Dictionary<string, object?> extra)
    {
        var key = id.Replace('.', '_').Replace('-', '_');
        var entity = new Dictionary<string, object?>(extra, StringComparer.Ordinal)
        {
            ["id"] = id,
            ["kind"] = kind,
            ["display_name"] = displayName,
            ["name_key"] = $"content.{key}.name",
            ["description_key"] = $"content.{key}.desc",
            ["schema_version"] = 1,
            ["tags"] = new[] { kind },
            ["owner_domain"] = OwnerDomainForKind(kind),
            ["references"] = Array.Empty<string>(),
            ["status"] = "Active",
            ["content_status"] = ContentStatus.Active,
            ["sort_order"] = sortOrder,
        };
        return entity;
    }

    private static string OwnerDomainForKind(string kind)
    {
        return kind switch
        {
            "resource" or "cargo" => "resources",
            "module" or "home-space" or "home-anchor" => "airship",
            "route" => "routes",
            "location" or "repair-node" or "stall-good" => "world",
            "companion" => "companions",
            "threat" => "threats",
            "intel" => "intel",
            _ => "world",
        };
    }
}
