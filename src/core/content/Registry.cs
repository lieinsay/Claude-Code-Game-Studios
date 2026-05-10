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
}

/// <summary>
/// Result returned by a single registry entity lookup.
/// </summary>
public sealed record RegistryQueryResult(
    RegistryQueryStatus Status,
    IReadOnlyDictionary<string, object?>? Entity,
    string? Error);

/// <summary>
/// Result returned when registering content in bulk.
/// </summary>
public sealed record RegistryRegistrationResult(
    bool Success,
    string? ErrorCode,
    string? EntityId);

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

    private readonly Dictionary<string, Dictionary<string, object?>> content = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadedDomains = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadedKinds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> domainKindMap = new(StringComparer.Ordinal);

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
                    RegistryQueryStatus.NotFound,
                    null,
                    "entity_retired"),
                _ => new RegistryQueryResult(RegistryQueryStatus.Found, CloneEntity(entity), null),
            };
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

            if (!seenIds.Add(id) || content.ContainsKey(id))
            {
                return new RegistryRegistrationResult(false, "ERR_DUPLICATE_ID", id);
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
        return loadedDomains.Contains(domain);
    }

    /// <summary>
    /// Marks a content domain as loaded for downstream readiness checks.
    /// </summary>
    public void SetDomainLoaded(string domain)
    {
        loadedDomains.Add(domain);
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

    private RegistryDefinitionValidationResult ValidateDefinition(
        IReadOnlyDictionary<string, object?> definition,
        bool requireGloballyUniqueId)
    {
        var diagnostics = new List<RegistryDiagnostic>();
        var id = ReadString(definition, "id");
        var kind = ReadString(definition, "kind");

        var hasUniqueId = StableIdPattern.IsMatch(id)
            && (!requireGloballyUniqueId || !content.ContainsKey(id));
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

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> ApplyResultLimit(
        IEnumerable<IReadOnlyDictionary<string, object?>> results)
    {
        if (MaxQueryResultCount > 0)
        {
            return results.Take(MaxQueryResultCount).ToList();
        }

        return results.ToList();
    }
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
            ["location_id"] = "location.glass-harbor-outskirts",
            ["node_kind"] = "beacon",
            ["restoration_theme"] = "lighthouse",
            ["settlement_need_tags"] = new[] { "navigation-aid", "safety" },
            ["repair_visible_state_tags"] = new[] { "dark", "lit", "connected" },
            ["required_materials"] = new Dictionary<string, int>
            {
                ["resource.repair_kit"] = 4,
                ["resource.beacon_crystal"] = 2,
            },
            ["unlocks"] = new Dictionary<string, string[]>
            {
                ["routes"] = ["route.sky-reef-arc-01"],
                ["stalls"] = ["stall.navigator_supply"],
                ["abilities"] = ["ability.lighthouse_signal"],
            },
        });

        yield return Entity("threat.guard-sentinel", "threat", "警戒哨兵", 1, new()
        {
            ["threat_class"] = "guard",
            ["encounter_tags"] = new[] { "raider" },
            ["counter_tags"] = new[] { "evade", "retreat" },
            ["severity_tier"] = "moderate",
            ["threat_type"] = "guard",
            ["trigger_radius"] = 120.0,
            ["trigger_probability"] = 0.70,
            ["hull_damage_min"] = 8,
            ["hull_damage_max"] = 12,
            ["module_damage_chance"] = 0.30,
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
