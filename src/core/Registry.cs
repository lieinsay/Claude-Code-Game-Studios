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
/// Static content catalog for stable IDs and deterministic read-only queries.
/// </summary>
public sealed class Registry
{
    private static readonly Regex StableIdPattern = new(
        "^[a-z][a-z0-9_-]*\\.[a-z0-9][a-z0-9_-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly Dictionary<string, Dictionary<string, object?>> content = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadedDomains = new(StringComparer.Ordinal);
    private readonly HashSet<string> loadedKinds = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets whether the registry has completed its initial content bootstrap.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Loads the prototype static content set into the registry.
    /// </summary>
    public void InitializeContent()
    {
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
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ListByKind(string kind)
    {
        return content.Values
            .Where(entity => string.Equals(ReadString(entity, "kind"), kind, StringComparison.Ordinal))
            .Where(entity => ReadContentStatus(entity) <= ContentStatus.Active)
            .OrderBy(entity => ReadInt(entity, "sort_order", int.MaxValue))
            .ThenBy(entity => ReadString(entity, "id"), StringComparer.Ordinal)
            .Select(CloneEntity)
            .ToList();
    }

    /// <summary>
    /// Lists active or draft content definitions for an owner domain in deterministic order.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ListByDomain(string domain)
    {
        return content.Values
            .Where(entity => string.Equals(ReadString(entity, "owner_domain"), domain, StringComparison.Ordinal))
            .Where(entity => ReadContentStatus(entity) <= ContentStatus.Active)
            .OrderBy(entity => ReadInt(entity, "sort_order", int.MaxValue))
            .ThenBy(entity => ReadString(entity, "id"), StringComparer.Ordinal)
            .Select(CloneEntity)
            .ToList();
    }

    /// <summary>
    /// Registers one content definition without schema validation.
    /// </summary>
    public void RegisterContent(string entityId, IReadOnlyDictionary<string, object?> definition)
    {
        var entity = CloneMutable(definition);
        entity["id"] = entityId;
        content[entityId] = entity;

        var kind = ReadString(entity, "kind");
        if (!string.IsNullOrWhiteSpace(kind))
        {
            loadedKinds.Add(kind);
        }
    }

    /// <summary>
    /// Registers a batch atomically after checking ID format, normalization collisions, and duplicates.
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
            RegisterContent(id, definition);
        }

        return new RegistryRegistrationResult(true, null, null);
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
        return entity.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private bool IsLoadedIdFamily(string entityId)
    {
        var separator = entityId.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 && loadedKinds.Contains(entityId[..separator]);
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
        foreach (var definition in CreatePrototypeDefinitions())
        {
            registry.RegisterContent((string)definition["id"]!, definition);
        }

        registry.SetDomainLoaded("core_content");
    }

    private static IEnumerable<IReadOnlyDictionary<string, object?>> CreatePrototypeDefinitions()
    {
        yield return Entity("location.glass-harbor", "location", "玻璃港", 1, new()
        {
            ["type"] = "settlement",
            ["description"] = "起始空港聚落——修复前的第一站",
        });
        yield return Entity("location.glass-harbor-outskirts", "location", "玻璃港近郊", 2, new()
        {
            ["type"] = "outskirts",
            ["description"] = "玻璃港附近郊区——灯塔修复节点所在地",
        });
        yield return Entity("location.sky-reef-outpost", "location", "空礁前哨", 3, new()
        {
            ["type"] = "outpost",
            ["description"] = "安全航线的目的地——小型探索前哨",
        });
        yield return Entity("location.cloudwatch-ruins", "location", "云观站废墟", 4, new()
        {
            ["type"] = "ruins",
            ["description"] = "高风险航线的目的地——探索搜撤场景",
        });

        yield return Entity("route.sky-reef-arc-01", "route", "空礁航线", 1, new()
        {
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
            ["destination_id"] = "location.cloudwatch-ruins",
            ["origin_id"] = "location.glass-harbor",
            ["traversable"] = true,
            ["hazard_tags"] = new[] { "mist", "low-visibility", "guard" },
            ["distance_band"] = "medium",
            ["encounter_check_count"] = 10,
        });

        yield return Entity("resource.repair_kit", "resource", "维修套件", 1, new()
        {
            ["stack_rule"] = "stackable",
            ["max_stack"] = 99,
            ["supply_class"] = "repair",
        });
        yield return Entity("resource.basic_supply", "resource", "基础补给品", 2, new()
        {
            ["stack_rule"] = "stackable",
            ["max_stack"] = 99,
            ["supply_class"] = "basic",
        });
        yield return Entity("resource.cloud_coin", "resource", "云海币", 3, new()
        {
            ["stack_rule"] = "stackable",
            ["max_stack"] = 9999,
            ["supply_class"] = "basic",
        });
        yield return Entity("resource.ancient_lens", "resource", "古代透镜", 4, new()
        {
            ["stack_rule"] = "unique",
            ["max_stack"] = 1,
            ["supply_class"] = "intel",
            ["cat_sniff_signature"] = "ancient_optics",
        });
        yield return Entity("resource.navigation_chart", "resource", "旧航海图", 5, new()
        {
            ["stack_rule"] = "stackable",
            ["max_stack"] = 20,
            ["supply_class"] = "navigation",
        });
        yield return Entity("resource.beacon_crystal", "resource", "信标水晶", 6, new()
        {
            ["stack_rule"] = "stackable",
            ["max_stack"] = 99,
            ["supply_class"] = "repair",
        });

        yield return Entity("repair_node.starlight_dock", "repair_node", "星光灯塔", 1, new()
        {
            ["location_id"] = "location.glass-harbor-outskirts",
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
            ["threat_type"] = "guard",
            ["trigger_radius"] = 120.0,
            ["trigger_probability"] = 0.70,
            ["hull_damage_min"] = 8,
            ["hull_damage_max"] = 12,
            ["module_damage_chance"] = 0.30,
            ["can_retreat"] = true,
        });

        yield return Entity("partner.sky-cat", "companion", "航海猫", 1, new()
        {
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
        var entity = new Dictionary<string, object?>(extra, StringComparer.Ordinal)
        {
            ["id"] = id,
            ["kind"] = kind,
            ["display_name"] = displayName,
            ["content_status"] = ContentStatus.Active,
            ["sort_order"] = sortOrder,
        };
        return entity;
    }
}
