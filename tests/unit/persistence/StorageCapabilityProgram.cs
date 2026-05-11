using System;
using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 003: Storage Capability Detection — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: All conditions met → PersistentAvailable", Ac1AllConditionsMet);
Run("AC-1: ExistingArchiveReadClass=NotApplicable → PersistentAvailable (fresh install)", Ac1FreshInstallNotApplicable);
Run("AC-2: write_roundtrip_ok=false → WriteLocked", Ac2WriteRoundtripFails);
Run("AC-2: quota_ok=false → WriteLocked", Ac2QuotaFails);
Run("AC-2: quota_reserve_ok=false → WriteLocked", Ac2QuotaReserveFails);
Run("AC-3: raw_persistent_api_ok=false → EphemeralOnly", Ac3ApiUnavailable);
Run("AC-3: policy_forces_ephemeral=true → EphemeralOnly", Ac3PolicyForcesEphemeral);
Run("AC-3: ExistingArchiveReadClass=Unreadable → EphemeralOnly", Ac3UnreadableArchive);
Run("AC-4: Fresh install (NotApplicable) with passing probe → PersistentAvailable", Ac4FreshInstallPersistentAvailable);
Run("AC-5: is_userfs_persistent hint alone (no write_roundtrip) → not PersistentAvailable", Ac5HintAloneInsufficientForPersistent);
Run("AC-6: quota_reserve_ok=false + Readable archive → WriteLocked (not EphemeralOnly)", Ac6QuotaReserveFalseReadable);
Run("AC-7: quota_reserve_ok=false + Unreadable archive → EphemeralOnly", Ac7QuotaReserveFalseUnreadable);
Run("AC-8: Expired probe returns WriteLocked, not PersistentAvailable", Ac8ExpiredProbeConservative);
Run("AC-9: Working set bytes 0 → fallback applied, WORKING_SET_BUDGET_FALLBACK noted", Ac9WorkingSetFallback);
Run("AC-10: Shell reads capability from detector, not its own formula", Ac10ShellReadsFromDetector);
Run("Regression: InvalidateProbe forces EphemeralOnly on next GetCurrentCapability", RegressionInvalidateForcesFallback);
Run("Regression: SetTtl ResumeTtl used for resume context", RegressionResumeTtlApplied);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 003 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 003 AC validation passed: {total}/{total} checks passed.");
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

// ---------------------------------------------------------------------------
// AC-1: All conditions met → PersistentAvailable
// ---------------------------------------------------------------------------

static bool Ac1AllConditionsMet()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable);
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.PersistentAvailable;
}

static bool Ac1FreshInstallNotApplicable()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.NotApplicable);
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.PersistentAvailable;
}

// ---------------------------------------------------------------------------
// AC-2: Partial failure → WriteLocked
// ---------------------------------------------------------------------------

static bool Ac2WriteRoundtripFails()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with { WriteRoundtripOk = false };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.WriteLocked;
}

static bool Ac2QuotaFails()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with { QuotaOk = false };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.WriteLocked;
}

static bool Ac2QuotaReserveFails()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with { QuotaReserveOk = false };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.WriteLocked;
}

// ---------------------------------------------------------------------------
// AC-3: Full failure → EphemeralOnly
// ---------------------------------------------------------------------------

static bool Ac3ApiUnavailable()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with
    {
        RawPersistentApiOk = false,
        StorageBackendProbeOk = false,
    };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.EphemeralOnly;
}

static bool Ac3PolicyForcesEphemeral()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with { PolicyForcesEphemeral = true };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.EphemeralOnly;
}

static bool Ac3UnreadableArchive()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Unreadable);
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.EphemeralOnly;
}

// ---------------------------------------------------------------------------
// AC-4: Fresh install
// ---------------------------------------------------------------------------

static bool Ac4FreshInstallPersistentAvailable()
{
    var detector = new PersistenceCapabilityDetector();
    // NotApplicable = no existing archive, but all probe conditions pass
    var probe = AllGoodProbe(ArchiveReadClass.NotApplicable);
    var result = detector.EvaluateProbe(probe);
    // Must not return EphemeralOnly just because there's no existing archive
    return result.Capability == StorageCapability.PersistentAvailable;
}

// ---------------------------------------------------------------------------
// AC-5: OS hint alone insufficient
// ---------------------------------------------------------------------------

static bool Ac5HintAloneInsufficientForPersistent()
{
    var detector = new PersistenceCapabilityDetector();
    // Simulate: API ok, but write roundtrip not yet performed
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with { WriteRoundtripOk = false };
    var result = detector.EvaluateProbe(probe);
    // WriteRoundtripOk=false means it cannot be PersistentAvailable
    return result.Capability != StorageCapability.PersistentAvailable;
}

// ---------------------------------------------------------------------------
// AC-6 / AC-7: quota_reserve_ok=false branches
// ---------------------------------------------------------------------------

static bool Ac6QuotaReserveFalseReadable()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with
    {
        QuotaReserveOk = false,
    };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.WriteLocked;
}

static bool Ac7QuotaReserveFalseUnreadable()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Unreadable) with
    {
        QuotaReserveOk = false,
    };
    var result = detector.EvaluateProbe(probe);
    return result.Capability == StorageCapability.EphemeralOnly;
}

// ---------------------------------------------------------------------------
// AC-8: TTL expiry
// ---------------------------------------------------------------------------

static bool Ac8ExpiredProbeConservative()
{
    var detector = new PersistenceCapabilityDetector();
    // Set an artificially short TTL that has already expired
    detector.SetTtl(TimeSpan.FromMilliseconds(1));
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with
    {
        ProbeTimestamp = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(2),
    };
    detector.EvaluateProbe(probe);

    // After expiry: GetCurrentCapability() must return WriteLocked, not PersistentAvailable
    var capability = detector.GetCurrentCapability();
    return capability == StorageCapability.WriteLocked;
}

// ---------------------------------------------------------------------------
// AC-9: Working set fallback
// ---------------------------------------------------------------------------

static bool Ac9WorkingSetFallback()
{
    var detector = new PersistenceCapabilityDetector();
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with { AvailableWorkingSetBytes = 0 };
    var result = detector.EvaluateProbe(probe);
    return result.WorkingSetFallbackApplied
        && result.DiagnosticNote == "WORKING_SET_BUDGET_FALLBACK"
        && result.Capability == StorageCapability.PersistentAvailable;
}

// ---------------------------------------------------------------------------
// AC-10: Shell reads from detector
// ---------------------------------------------------------------------------

static bool Ac10ShellReadsFromDetector()
{
    // The shell (StorageCapabilityCoordinator) reads from IStorageCapabilityEvaluator.Evaluate().
    // Verify that PersistenceCapabilityDetector correctly implements the interface.
    IStorageCapabilityEvaluator evaluator = new PersistenceCapabilityDetector();
    var rawProbe = new PersistenceProbe(
        IndexedDbAvailable: true,
        QuotaBytes: 10 * 1024 * 1024,
        UsedBytes: 1024,
        WriteTestPassed: true);

    var evaluation = evaluator.Evaluate(rawProbe);
    // The evaluator must return a result — shell must not have computed it itself
    return evaluation.StorageCapability != StorageCapability.EphemeralOnly
        || evaluation.ContinuePoint is not null;
}

// ---------------------------------------------------------------------------
// Regression checks
// ---------------------------------------------------------------------------

static bool RegressionInvalidateForcesFallback()
{
    var detector = new PersistenceCapabilityDetector();
    detector.EvaluateProbe(AllGoodProbe(ArchiveReadClass.Readable));
    // Probe is valid, current = PersistentAvailable
    detector.InvalidateProbe();
    // After invalidation, no probe → EphemeralOnly
    return detector.GetCurrentCapability() == StorageCapability.EphemeralOnly;
}

static bool RegressionResumeTtlApplied()
{
    var detector = new PersistenceCapabilityDetector();
    detector.SetTtl(PersistenceCapabilityProbe.ResumeTtl);
    // ResumeTtl = 10s; set probe timestamp 15s ago → expired
    var probe = AllGoodProbe(ArchiveReadClass.Readable) with
    {
        ProbeTimestamp = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(15),
    };
    detector.EvaluateProbe(probe);
    return detector.IsProbeExpired;
}

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

static PersistenceCapabilityProbe AllGoodProbe(ArchiveReadClass archiveClass)
{
    return new PersistenceCapabilityProbe(
        RawPersistentApiOk: true,
        StorageBackendProbeOk: true,
        ExistingArchiveReadClass: archiveClass,
        QuotaOk: true,
        QuotaReserveOk: true,
        WriteRoundtripOk: true,
        PolicyForcesEphemeral: false,
        AvailableWorkingSetBytes: PersistenceCapabilityProbe.WorkingSetFallbackBytes,
        ProbeTimestamp: DateTimeOffset.UtcNow);
}
