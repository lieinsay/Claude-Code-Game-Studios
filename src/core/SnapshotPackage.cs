using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Readiness state for a domain snapshot collected by the persistence pipeline.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum SnapshotDomainState
{
    Ready = 0,
    Blocked = 1,
    NotReady = 2,
    Settling = 3,
}

/// <summary>
/// Typed container for one domain's save payload.
/// </summary>
public sealed class SnapshotPackage
{
    /// <summary>Stable domain ID such as "resources" or "repair".</summary>
    public string DomainId { get; set; } = string.Empty;

    /// <summary>Schema version for this domain snapshot payload.</summary>
    public int SnapshotSchemaVersion { get; set; } = 1;

    /// <summary>Content domain versions used when this snapshot was captured.</summary>
    public Dictionary<string, string> ContentDomainVersions { get; } = new(StringComparer.Ordinal);

    /// <summary>Stable IDs referenced by this snapshot.</summary>
    public List<string> StableIdRefs { get; } = [];

    /// <summary>JSON-compatible domain-owned payload.</summary>
    public Dictionary<string, object?> Payload { get; } = new(StringComparer.Ordinal);

    /// <summary>Snapshot readiness state.</summary>
    public SnapshotDomainState DomainState { get; set; } = SnapshotDomainState.NotReady;

    /// <summary>Error code provided when a domain cannot provide a valid snapshot.</summary>
    public string DomainErrorCode { get; set; } = string.Empty;

    /// <summary>Optional migration hint for future schema changes.</summary>
    public string MigrationHint { get; set; } = string.Empty;

    /// <summary>
    /// Returns true only when the package is ready for save promotion.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(DomainId)
            && SnapshotSchemaVersion > 0
            && ContentDomainVersions.Count > 0
            && DomainState == SnapshotDomainState.Ready
            && string.IsNullOrEmpty(DomainErrorCode);
    }

    /// <summary>
    /// Converts this package to the stable dictionary shape used by the legacy prototype.
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["domain_id"] = DomainId,
            ["snapshot_schema_version"] = SnapshotSchemaVersion,
            ["content_domain_versions"] = new Dictionary<string, string>(ContentDomainVersions, StringComparer.Ordinal),
            ["stable_id_refs"] = StableIdRefs.ToList(),
            ["payload"] = new Dictionary<string, object?>(Payload, StringComparer.Ordinal),
            ["domain_state"] = (int)DomainState,
            ["domain_error_code"] = DomainErrorCode,
            ["migration_hint"] = MigrationHint,
        };
    }

    /// <summary>
    /// Restores a package from the stable dictionary shape used by save data.
    /// </summary>
    public static SnapshotPackage FromDictionary(IReadOnlyDictionary<string, object?> data)
    {
        var package = new SnapshotPackage
        {
            DomainId = ReadString(data, "domain_id"),
            SnapshotSchemaVersion = ReadInt(data, "snapshot_schema_version"),
            DomainState = (SnapshotDomainState)ReadInt(data, "domain_state", (int)SnapshotDomainState.NotReady),
            DomainErrorCode = ReadString(data, "domain_error_code"),
            MigrationHint = ReadString(data, "migration_hint"),
        };

        foreach (var (key, value) in ReadStringMap(data, "content_domain_versions"))
        {
            package.ContentDomainVersions[key] = value;
        }

        package.StableIdRefs.AddRange(ReadStringList(data, "stable_id_refs"));

        foreach (var (key, value) in ReadObjectMap(data, "payload"))
        {
            package.Payload[key] = value;
        }

        return package;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> data, string key, int fallback = 0)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            double doubleValue => checked((int)doubleValue),
            float floatValue => checked((int)floatValue),
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            _ => fallback,
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadStringMap(
        IReadOnlyDictionary<string, object?> data,
        string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is IReadOnlyDictionary<string, string> typedMap)
        {
            return typedMap;
        }

        if (value is IReadOnlyDictionary<string, object?> objectMap)
        {
            return objectMap.Select(pair => new KeyValuePair<string, string>(
                pair.Key,
                pair.Value?.ToString() ?? string.Empty));
        }

        return [];
    }

    private static IEnumerable<string> ReadStringList(IReadOnlyDictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is IEnumerable<string> typedList)
        {
            return typedList;
        }

        if (value is IEnumerable<object?> objectList)
        {
            return objectList.Select(item => item?.ToString() ?? string.Empty);
        }

        return [];
    }

    private static IEnumerable<KeyValuePair<string, object?>> ReadObjectMap(
        IReadOnlyDictionary<string, object?> data,
        string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        return value is IReadOnlyDictionary<string, object?> objectMap
            ? objectMap
            : [];
    }
}
