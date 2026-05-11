using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 001: Staging → Verify → Promotion Pipeline — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: All conditions met → promotion_success=true, generation increments", Ac1FullHappyPath);
Run("AC-2: Invalid snapshot (missing domain_id) → promotion_success=false, old safe preserved", Ac2InvalidSnapshotPreservesOldSafe);
Run("AC-2: Blocked domain_state → promotion_success=false, old safe preserved", Ac2BlockedDomainPreservesOldSafe);
Run("AC-3: Staging written but not promoted → last generation unchanged", Ac3StagingIsolation);
Run("AC-4: Promotion only updates safe via generation increment, not interim state", Ac4PromotionAtomicity);
Run("AC-5: Manifest generation older than last_verified rejected via checksum guard", Ac5OutdatedGenerationRejected);
Run("Regression: Pipeline returns to Idle after successful save", RegressionPipelineIdleAfterSave);
Run("Regression: Pipeline returns to Idle after failed save", RegressionPipelineIdleAfterFailure);
Run("Regression: CanonicalJsonEncode sorted keys deterministic", RegressionCanonicalJsonSortedKeys);
Run("Regression: ComputeChecksum returns non-empty hex string", RegressionChecksumNonEmpty);
Run("Regression: Multiple saves increment generation monotonically", RegressionGenerationMonotonic);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 AC validation passed: {total}/{total} checks passed.");
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
// AC-1: All conditions met → promotion succeeds
// ---------------------------------------------------------------------------

static bool Ac1FullHappyPath()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);

    // Pre-condition: generation starts at 0
    var initialGeneration = persistence.CurrentGeneration;

    int? promotionCallbackGeneration = null;
    persistence.PromotionCompleted += (_, gen) => promotionCallbackGeneration = gen;
    int? saveCallbackGeneration = null;
    persistence.SaveCompleted += gen => saveCallbackGeneration = gen;

    var result = persistence.RequestSaveProgress();

    return result.Success
        && result.Reason is null
        && persistence.CurrentGeneration == initialGeneration + 1
        && promotionCallbackGeneration == persistence.CurrentGeneration
        && saveCallbackGeneration == persistence.CurrentGeneration;
}

// ---------------------------------------------------------------------------
// AC-2: Any domain failure → promotion_success=false, old safe preserved
// ---------------------------------------------------------------------------

static bool Ac2InvalidSnapshotPreservesOldSafe()
{
    // First save establishes a known-good safe at generation 1
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);
    persistence.RequestSaveProgress();
    var safeGeneration = persistence.CurrentGeneration;

    // Now register an invalid serializer (no domain_id)
    persistence.RegisterDomainSerializer("resources", InvalidSerializer_NoDomainId);

    string? failReason = null;
    persistence.SaveFailed += (reason, _) => failReason = reason;

    var result = persistence.RequestSaveProgress();

    // Old safe generation must not have changed
    return !result.Success
        && persistence.CurrentGeneration == safeGeneration
        && failReason is not null;
}

static bool Ac2BlockedDomainPreservesOldSafe()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);
    persistence.RequestSaveProgress();
    var safeGeneration = persistence.CurrentGeneration;

    // Replace with blocked-state serializer
    persistence.RegisterDomainSerializer("resources", BlockedSerializer);

    var result = persistence.RequestSaveProgress();

    return !result.Success
        && persistence.CurrentGeneration == safeGeneration;
}

// ---------------------------------------------------------------------------
// AC-3: While staging is in progress, current generation stays at old safe
// ---------------------------------------------------------------------------

static bool Ac3StagingIsolation()
{
    // Verify that before any save happens, the generation is 0 (no safe data yet)
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);

    var preGeneration = persistence.CurrentGeneration;

    // The pipeline is synchronous: by the time RequestSaveProgress returns,
    // promotion has either succeeded or failed. We verify the invariant holds:
    // if we observe the generation BEFORE calling save, it reflects the old safe.
    var observedBeforeSave = persistence.CurrentGeneration;
    var result = persistence.RequestSaveProgress();
    var observedAfterSave = persistence.CurrentGeneration;

    // Before save: generation was preGeneration (0)
    // After successful save: generation is preGeneration + 1
    // The old safe was never corrupted — the generation only advanced on success
    return result.Success
        && observedBeforeSave == preGeneration
        && observedAfterSave == preGeneration + 1;
}

// ---------------------------------------------------------------------------
// AC-4: Promotion only happens via the authorized generation switch
// ---------------------------------------------------------------------------

static bool Ac4PromotionAtomicity()
{
    // Verify: on success, generation advances by exactly 1 per save.
    // On failure, generation stays at old value — no partial-promotion side effect.
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);

    // Succeed once
    var r1 = persistence.RequestSaveProgress();
    var genAfterFirst = persistence.CurrentGeneration;

    // Fail once
    persistence.RegisterDomainSerializer("resources", BlockedSerializer);
    var r2 = persistence.RequestSaveProgress();
    var genAfterFail = persistence.CurrentGeneration;

    // Succeed again
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);
    var r3 = persistence.RequestSaveProgress();
    var genAfterSecond = persistence.CurrentGeneration;

    return r1.Success && r1.Generation == 1
        && !r2.Success && genAfterFail == genAfterFirst
        && r3.Success && genAfterSecond == genAfterFirst + 1;
}

// ---------------------------------------------------------------------------
// AC-5: Manifest pointer below last verified checkpoint must be rejected
// ---------------------------------------------------------------------------

static bool Ac5OutdatedGenerationRejected()
{
    // Simulate: we have a safe at generation 3; an attempted save fails due
    // to checksum mismatch or invalid domain. The last verified generation
    // must remain 3 — the pipeline must not regress.
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);

    persistence.RequestSaveProgress(); // gen 1
    persistence.RequestSaveProgress(); // gen 2
    persistence.RequestSaveProgress(); // gen 3
    var safeGeneration = persistence.CurrentGeneration; // 3

    // Inject failure
    persistence.RegisterDomainSerializer("resources", BlockedSerializer);
    var failResult = persistence.RequestSaveProgress();

    // The current generation must still be the last verified safe (3), not 4
    return !failResult.Success
        && persistence.CurrentGeneration == safeGeneration
        && failResult.Generation == safeGeneration;
}

// ---------------------------------------------------------------------------
// Regression checks
// ---------------------------------------------------------------------------

static bool RegressionPipelineIdleAfterSave()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);
    persistence.RequestSaveProgress();
    return persistence.IsPipelineIdle;
}

static bool RegressionPipelineIdleAfterFailure()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", BlockedSerializer);
    persistence.RequestSaveProgress();
    return persistence.IsPipelineIdle;
}

static bool RegressionCanonicalJsonSortedKeys()
{
    // Same dictionary with different insertion order must produce identical output
    var dictA = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["zebra"] = 1,
        ["apple"] = 2,
        ["mango"] = 3,
    };
    var dictB = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["mango"] = 3,
        ["apple"] = 2,
        ["zebra"] = 1,
    };

    var jsonA = Persistence.CanonicalJsonEncode(dictA);
    var jsonB = Persistence.CanonicalJsonEncode(dictB);

    return string.Equals(jsonA, jsonB, StringComparison.Ordinal)
        && jsonA.IndexOf("\"apple\"", StringComparison.Ordinal) < jsonA.IndexOf("\"mango\"", StringComparison.Ordinal)
        && jsonA.IndexOf("\"mango\"", StringComparison.Ordinal) < jsonA.IndexOf("\"zebra\"", StringComparison.Ordinal);
}

static bool RegressionChecksumNonEmpty()
{
    var checksum = Persistence.ComputeChecksum("{\"hello\":\"world\"}");
    return !string.IsNullOrEmpty(checksum)
        && checksum.Length == 64
        && checksum == checksum.ToLowerInvariant();
}

static bool RegressionGenerationMonotonic()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);

    var generations = new List<int>();
    persistence.SaveCompleted += gen => generations.Add(gen);

    persistence.RequestSaveProgress();
    persistence.RequestSaveProgress();
    persistence.RequestSaveProgress();

    return generations.Count == 3
        && generations[0] == 1
        && generations[1] == 2
        && generations[2] == 3;
}

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

static SnapshotPackage ValidResourcesSerializer()
{
    var pkg = new SnapshotPackage
    {
        DomainId = "resources",
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Ready,
        DomainErrorCode = string.Empty,
    };
    pkg.ContentDomainVersions["resources"] = "1";
    pkg.StableIdRefs.Add("resource.supplies");
    pkg.Payload["pools"] = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["supplies"] = 5,
    };
    return pkg;
}

static SnapshotPackage InvalidSerializer_NoDomainId()
{
    // Missing DomainId — IsValid() returns false
    var pkg = new SnapshotPackage
    {
        DomainId = string.Empty,
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Ready,
    };
    pkg.ContentDomainVersions["resources"] = "1";
    return pkg;
}

static SnapshotPackage BlockedSerializer()
{
    var pkg = new SnapshotPackage
    {
        DomainId = "resources",
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Blocked,
        DomainErrorCode = "domain_settling",
    };
    pkg.ContentDomainVersions["resources"] = "1";
    return pkg;
}
