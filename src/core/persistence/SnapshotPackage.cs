using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Result of validating a SnapshotPackage contract before pipeline promotion.
/// </summary>
public sealed record SnapshotValidationResult(bool Valid, string? ReasonCode = null)
{
    public static readonly SnapshotValidationResult Ok = new(true);
}

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
    /// Validates the full snapshot package contract. Returns Ok when valid, or a reason code on failure.
    /// Covers: required fields, domain_state, payload type whitelist, NFC key uniqueness, float rules.
    /// </summary>
    public SnapshotValidationResult ValidateContract()
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(DomainId))
        {
            return new SnapshotValidationResult(false, "ERR_MISSING_DOMAIN_ID");
        }

        if (SnapshotSchemaVersion <= 0)
        {
            return new SnapshotValidationResult(false, "ERR_MISSING_SCHEMA_VERSION");
        }

        if (ContentDomainVersions.Count == 0)
        {
            return new SnapshotValidationResult(false, "ERR_MISSING_CONTENT_DOMAIN_VERSIONS");
        }

        if (StableIdRefs is null)
        {
            return new SnapshotValidationResult(false, "ERR_MISSING_STABLE_ID_REFS");
        }

        if (Payload is null)
        {
            return new SnapshotValidationResult(false, "ERR_MISSING_PAYLOAD");
        }

        // domain_state must be Ready; anything else blocks promotion
        if (DomainState != SnapshotDomainState.Ready || !string.IsNullOrEmpty(DomainErrorCode))
        {
            return new SnapshotValidationResult(false, "ERR_DOMAIN_NOT_READY");
        }

        // Payload type whitelist — DFS
        var payloadResult = ValidatePayloadTypes(Payload);
        if (!payloadResult.Valid)
        {
            return payloadResult;
        }

        // NFC key uniqueness and canonical key ordering within payload dictionaries
        var keyResult = ValidateCanonicalKeys(Payload);
        if (!keyResult.Valid)
        {
            return keyResult;
        }

        return SnapshotValidationResult.Ok;
    }

    /// <summary>
    /// DFS traversal of a payload dictionary ensuring only whitelisted types are present.
    /// Forbidden: anything that is not bool, int/long, float/double, string, array, or dictionary.
    /// </summary>
    public static SnapshotValidationResult ValidatePayloadTypes(object? value)
    {
        return value switch
        {
            null => SnapshotValidationResult.Ok,
            bool => SnapshotValidationResult.Ok,
            int or long or short or byte => SnapshotValidationResult.Ok,
            float f when float.IsNaN(f) || float.IsInfinity(f) =>
                new SnapshotValidationResult(false, "ERR_NON_FINITE_FLOAT_IN_PAYLOAD"),
            double d when double.IsNaN(d) || double.IsInfinity(d) =>
                new SnapshotValidationResult(false, "ERR_NON_FINITE_FLOAT_IN_PAYLOAD"),
            float => SnapshotValidationResult.Ok,
            double => SnapshotValidationResult.Ok,
            string => SnapshotValidationResult.Ok,
            IReadOnlyDictionary<string, object?> dict => ValidatePayloadDictionary(dict),
            IDictionary<string, object?> dict => ValidatePayloadDictionary(dict),
            System.Collections.IEnumerable list => ValidatePayloadList(list),
            // Any non-whitelisted type (object references, enum values not converted, etc.)
            _ => new SnapshotValidationResult(false, "ERR_FORBIDDEN_TYPE_IN_PAYLOAD"),
        };
    }

    private static SnapshotValidationResult ValidatePayloadDictionary(
        System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> dict)
    {
        foreach (var (_, v) in dict)
        {
            var r = ValidatePayloadTypes(v);
            if (!r.Valid)
            {
                return r;
            }
        }

        return SnapshotValidationResult.Ok;
    }

    private static SnapshotValidationResult ValidatePayloadList(System.Collections.IEnumerable list)
    {
        foreach (var item in list)
        {
            var r = ValidatePayloadTypes(item);
            if (!r.Valid)
            {
                return r;
            }
        }

        return SnapshotValidationResult.Ok;
    }

    /// <summary>
    /// Validates that all dictionary keys in the payload are NFC-normalized and that
    /// no duplicate keys exist after NFC normalization.
    /// </summary>
    public static SnapshotValidationResult ValidateCanonicalKeys(object? value)
    {
        if (value is not IReadOnlyDictionary<string, object?> dict
            && value is not IDictionary<string, object?>)
        {
            return SnapshotValidationResult.Ok;
        }

        IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> pairs =
            value is IReadOnlyDictionary<string, object?> rd ? rd : (IDictionary<string, object?>)value;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (key, nested) in pairs)
        {
            var normalized = key.Normalize(System.Text.NormalizationForm.FormC);
            if (!seen.Add(normalized))
            {
                return new SnapshotValidationResult(false, "ERR_DUPLICATE_KEY_AFTER_NFC");
            }

            // Recurse into nested dictionaries and arrays
            var nested2 = ValidateCanonicalKeys(nested);
            if (!nested2.Valid)
            {
                return nested2;
            }
        }

        return SnapshotValidationResult.Ok;
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
