using System;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Existing archive readability classification used by the capability detector.
/// </summary>
public enum ArchiveReadClass
{
    Readable = 0,
    Unreadable = 1,
    NotApplicable = 2,
}

/// <summary>
/// Full probe input for the persistence capability formula.
/// All fields correspond directly to ADR-0003 formula variables.
/// </summary>
public sealed record PersistenceCapabilityProbe(
    bool RawPersistentApiOk,
    bool StorageBackendProbeOk,
    ArchiveReadClass ExistingArchiveReadClass,
    bool QuotaOk,
    bool QuotaReserveOk,
    bool WriteRoundtripOk,
    bool PolicyForcesEphemeral,
    long AvailableWorkingSetBytes = 0,
    DateTimeOffset? ProbeTimestamp = null)
{
    public static readonly TimeSpan BootTtl = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ResumeTtl = TimeSpan.FromSeconds(10);
    public const long WorkingSetFallbackBytes = 16 * 1024 * 1024; // 16 MiB
}

/// <summary>
/// Persistence-side capability detection result with diagnostic context.
/// </summary>
public sealed record PersistenceCapabilityResult(
    StorageCapability Capability,
    bool WorkingSetFallbackApplied = false,
    bool ProbeExpired = false,
    string? DiagnosticNote = null);

/// <summary>
/// Implements the ADR-0003 storage capability detection formula with TTL probe management.
/// Persistence owns this calculation; the shell shell layer must not recompute it.
/// </summary>
public sealed class PersistenceCapabilityDetector : IStorageCapabilityEvaluator
{
    private PersistenceCapabilityProbe? _lastProbe;
    private DateTimeOffset _lastProbeTime;
    private TimeSpan _activeTtl = PersistenceCapabilityProbe.BootTtl;

    /// <summary>Most recent probe used for the last evaluation.</summary>
    public PersistenceCapabilityProbe? LastProbe => _lastProbe;

    /// <summary>Whether the last probe was applied with the working-set fallback.</summary>
    public bool LastWorkingSetFallback { get; private set; }

    /// <summary>Whether the last probe is now expired.</summary>
    public bool IsProbeExpired =>
        _lastProbe is not null
        && DateTimeOffset.UtcNow - _lastProbeTime > _activeTtl;

    /// <summary>
    /// Sets the active TTL category. Use BootTtl on boot, ResumeTtl after desktop suspend/resume.
    /// </summary>
    public void SetTtl(TimeSpan ttl)
    {
        _activeTtl = ttl;
    }

    /// <summary>
    /// Invalidates the current probe, forcing a re-evaluate on next query.
    /// Call on write failure, readback mismatch, quota failure, or policy change.
    /// </summary>
    public void InvalidateProbe()
    {
        _lastProbe = null;
    }

    /// <summary>
    /// Evaluates storage capability from a raw persistence probe.
    /// Called by the platform shell forwarding its persistence_probe signal.
    /// </summary>
    public StorageCapabilityEvaluation Evaluate(PersistenceProbe rawProbe)
    {
        // Translate from the session-shell probe format to the full capability probe
        var probe = new PersistenceCapabilityProbe(
            RawPersistentApiOk: rawProbe.IndexedDbAvailable,
            StorageBackendProbeOk: rawProbe.WriteTestPassed,
            ExistingArchiveReadClass: rawProbe.QuotaBytes > 0
                ? ArchiveReadClass.Readable
                : ArchiveReadClass.NotApplicable,
            QuotaOk: rawProbe.QuotaBytes > 0,
            QuotaReserveOk: rawProbe.QuotaBytes - rawProbe.UsedBytes > 1024 * 1024,
            WriteRoundtripOk: rawProbe.WriteTestPassed,
            PolicyForcesEphemeral: !rawProbe.IndexedDbAvailable,
            AvailableWorkingSetBytes: 0,
            ProbeTimestamp: DateTimeOffset.UtcNow);

        var result = EvaluateProbe(probe);
        var continuePoint = new ContinuePointValidation(
            Exists: result.Capability != StorageCapability.EphemeralOnly,
            IntegrityValid: result.Capability == StorageCapability.PersistentAvailable,
            ContentDomainMatches: result.Capability == StorageCapability.PersistentAvailable);

        return new StorageCapabilityEvaluation(result.Capability, continuePoint);
    }

    /// <summary>
    /// Evaluates storage capability using the full ADR-0003 capability formula.
    /// </summary>
    public PersistenceCapabilityResult EvaluateProbe(PersistenceCapabilityProbe probe)
    {
        _lastProbe = probe;
        _lastProbeTime = probe.ProbeTimestamp ?? DateTimeOffset.UtcNow;

        var workingSetFallback = false;
        var effectiveProbe = probe;

        // AC-9: working set bytes unavailable → apply 16 MiB fallback
        if (probe.AvailableWorkingSetBytes <= 0)
        {
            workingSetFallback = true;
            effectiveProbe = probe with
            {
                AvailableWorkingSetBytes = PersistenceCapabilityProbe.WorkingSetFallbackBytes,
            };
        }

        LastWorkingSetFallback = workingSetFallback;

        var capability = ComputeCapability(effectiveProbe);
        var note = workingSetFallback ? "WORKING_SET_BUDGET_FALLBACK" : null;

        return new PersistenceCapabilityResult(capability, workingSetFallback, DiagnosticNote: note);
    }

    /// <summary>
    /// Returns the capability for a previously-cached probe, or WriteLocked if the probe is expired.
    /// The shell must never recompute this value itself.
    /// </summary>
    public StorageCapability GetCurrentCapability()
    {
        if (_lastProbe is null)
        {
            return StorageCapability.EphemeralOnly;
        }

        // AC-8: expired probe must not be used as PersistentAvailable basis
        if (IsProbeExpired)
        {
            return StorageCapability.WriteLocked;
        }

        return ComputeCapability(_lastProbe);
    }

    /// <summary>
    /// ADR-0003 capability formula:
    ///   PersistentAvailable if A AND B AND L≠Unreadable AND Q AND H AND R AND NOT P
    ///   WriteLocked         if A AND B AND L≠Unreadable AND NOT P
    ///   EphemeralOnly       otherwise
    /// </summary>
    private static StorageCapability ComputeCapability(PersistenceCapabilityProbe p)
    {
        // AC-5: OS.is_userfs_persistent() hint alone is NOT sufficient —
        // WriteRoundtripOk must be true via actual write/flush/readback/checksum.
        // The probe's WriteRoundtripOk field must come from a real roundtrip, not just an API check.

        var apiAndBackendOk = p.RawPersistentApiOk && p.StorageBackendProbeOk;
        var archiveAccessible = p.ExistingArchiveReadClass != ArchiveReadClass.Unreadable;
        var notForcedEphemeral = !p.PolicyForcesEphemeral;

        if (!apiAndBackendOk || !archiveAccessible || !notForcedEphemeral)
        {
            return StorageCapability.EphemeralOnly;
        }

        if (p.QuotaOk && p.QuotaReserveOk && p.WriteRoundtripOk)
        {
            return StorageCapability.PersistentAvailable;
        }

        // AC-6: quota_reserve_ok=false + Readable → WriteLocked (not EphemeralOnly)
        // AC-7: quota_reserve_ok=false + Unreadable → already returned EphemeralOnly above
        return StorageCapability.WriteLocked;
    }
}
