using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Outcome of evaluating or executing a save artifact migration.
/// </summary>
public enum MigrationOutcome
{
    /// <summary>
    /// Save data is already at the current target version and is directly restorable.
    /// No migration was required.
    /// </summary>
    AlreadyCurrent = 0,

    /// <summary>
    /// Migration executed successfully on a staging copy, was verified, and was promoted.
    /// The original artifact is now replaced with the migrated version.
    /// </summary>
    Upgraded = 1,

    /// <summary>
    /// Migration was required but could not be completed (no chain available, staging
    /// failed, verify failed, or promotion failed). The original artifact is preserved
    /// unchanged and locked from direct restore.
    /// </summary>
    PreservedLocked = 2,

    /// <summary>
    /// The artifact failed to parse or failed integrity validation. It is quarantined
    /// regardless of version. No migration is attempted on corrupt artifacts.
    /// </summary>
    Quarantined = 3,
}

/// <summary>
/// Input context passed to <see cref="SaveMigrator.EvaluateArtifact"/> and
/// <see cref="SaveMigrator.ExecuteMigration"/>.
/// </summary>
public sealed class ArtifactContext
{
    /// <summary>Whether the artifact JSON parsed without error.</summary>
    public bool ParseOk { get; set; }

    /// <summary>Whether the artifact SHA-256 integrity check passed.</summary>
    public bool IntegrityOk { get; set; }

    /// <summary>Whether this artifact's schema/content versions require migration.</summary>
    public bool MigrationRequired { get; set; }

    /// <summary>
    /// Whether a complete migration chain is available from the artifact's version
    /// to the current target version.
    /// </summary>
    public bool MigrationChainAvailable { get; set; }

    /// <summary>
    /// Whether the artifact is directly restorable at its current version
    /// (version_compatible AND content_domain_versions_directly_compatible
    /// AND stable_id_resolution_class = AllActive).
    /// </summary>
    public bool DirectRestoreCompatible { get; set; }

    /// <summary>Current recorded schema version of the artifact.</summary>
    public int ArtifactSchemaVersion { get; set; } = 1;

    /// <summary>Target schema version for this build.</summary>
    public int TargetSchemaVersion { get; set; } = 1;

    /// <summary>Generation number recorded in the artifact manifest.</summary>
    public int OldGeneration { get; set; }

    /// <summary>Kind of artifact being evaluated.</summary>
    public PersistenceArtifactKind ArtifactKind { get; set; } = PersistenceArtifactKind.Progress;
}

/// <summary>
/// Represents one step in a migration chain: transforms a snapshot payload
/// from <see cref="FromVersion"/> to <see cref="ToVersion"/>.
/// </summary>
public sealed class MigrationStep
{
    /// <summary>Source schema version this step accepts.</summary>
    public int FromVersion { get; set; }

    /// <summary>Target schema version this step produces.</summary>
    public int ToVersion { get; set; }

    /// <summary>
    /// Pure transformation function: takes the artifact payload at
    /// <see cref="FromVersion"/> and returns the mutated payload at
    /// <see cref="ToVersion"/>. Must not throw on valid input.
    /// </summary>
    public required Func<Dictionary<string, object?>, Dictionary<string, object?>> MigrationFn { get; set; }
}

/// <summary>
/// Immutable record of a completed migration, written after a successful promotion.
/// </summary>
public sealed record MigrationRecord(
    PersistenceArtifactKind ArtifactKind,
    int OldGeneration,
    int NewGeneration,
    int OldVersion,
    int NewVersion,
    IReadOnlyList<string> ChainVersions,
    IReadOnlyList<long> StepDurationsMs,
    MigrationOutcome Outcome,
    DateTimeOffset Timestamp);

/// <summary>
/// Manages save artifact version migration for the persistence pipeline.
///
/// Implements ADR-0003 migration contract:
/// - Migration executes on a staging copy; original artifact is never modified
///   until promotion succeeds.
/// - Failures lock the original artifact as <see cref="MigrationOutcome.PreservedLocked"/>.
/// - Corrupt artifacts (parse or integrity failure) are <see cref="MigrationOutcome.Quarantined"/>.
/// - migration_retry_limit = 1 per launch; duplicate migration requests after a
///   failed attempt are rejected.
///
/// <example>
/// <code>
/// var migrator = new SaveMigrator();
/// migrator.RegisterMigrationStep(new MigrationStep
/// {
///     FromVersion = 1,
///     ToVersion = 2,
///     MigrationFn = payload =>
///     {
///         var upgraded = new Dictionary&lt;string, object?&gt;(payload);
///         upgraded["schema_version"] = 2;
///         return upgraded;
///     }
/// });
///
/// var ctx = new ArtifactContext
/// {
///     ParseOk = true, IntegrityOk = true,
///     MigrationRequired = true, MigrationChainAvailable = true,
///     ArtifactSchemaVersion = 1, TargetSchemaVersion = 2,
/// };
/// var result = migrator.ExecuteMigration(ctx, originalPayload);
/// // result.Outcome == MigrationOutcome.Upgraded
/// </code>
/// </example>
/// </summary>
public sealed class SaveMigrator
{
    private readonly List<MigrationStep> _steps = [];
    private readonly HashSet<int> _failedMigrationVersions = [];
    private readonly List<MigrationRecord> _migrationHistory = [];

    /// <summary>
    /// Gets all completed migration records produced during this launch,
    /// ordered chronologically.
    /// </summary>
    public IReadOnlyList<MigrationRecord> MigrationHistory => _migrationHistory;

    /// <summary>
    /// Registers a migration step. Steps are applied in ascending
    /// <see cref="MigrationStep.FromVersion"/> order.
    /// </summary>
    public void RegisterMigrationStep(MigrationStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
    }

    /// <summary>
    /// Evaluates an artifact's context and returns the appropriate
    /// <see cref="MigrationOutcome"/> without executing any migration.
    /// Use this to determine whether to call <see cref="ExecuteMigration"/>.
    ///
    /// <example>
    /// <code>
    /// var outcome = migrator.EvaluateArtifact(ctx);
    /// if (outcome == MigrationOutcome.Upgraded) { /* never — evaluation doesn't migrate */ }
    /// </code>
    /// </example>
    /// </summary>
    public MigrationOutcome EvaluateArtifact(ArtifactContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ComputeOutcome(ctx, migrationExecuted: false, stagingOk: false, verifyOk: false, promotionSuccess: false);
    }

    /// <summary>
    /// Executes a migration for the given artifact context, applying the registered
    /// migration chain on a staging copy. Returns the outcome and, on success, updates
    /// the payload reference with the migrated result.
    ///
    /// ADR-0003 invariants enforced:
    /// - Original payload is never mutated until promotion succeeds.
    /// - retry_limit = 1 per artifact version per launch.
    /// - Migration record is written only on <see cref="MigrationOutcome.Upgraded"/>.
    ///
    /// <example>
    /// <code>
    /// var payload = new Dictionary&lt;string, object?&gt; { ["data"] = 42 };
    /// var result = migrator.ExecuteMigration(ctx, payload);
    /// if (result.Outcome == MigrationOutcome.Upgraded)
    ///     RestoreFromPayload(result.MigratedPayload!);
    /// </code>
    /// </example>
    /// </summary>
    public MigrationExecutionResult ExecuteMigration(
        ArtifactContext ctx,
        Dictionary<string, object?> originalPayload)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(originalPayload);

        // Corrupt artifacts are quarantined immediately — no migration attempted.
        if (!ctx.ParseOk || !ctx.IntegrityOk)
        {
            return new MigrationExecutionResult(MigrationOutcome.Quarantined, null);
        }

        // No migration needed — evaluate direct restore compatibility.
        if (!ctx.MigrationRequired)
        {
            var noMigrationOutcome = ctx.DirectRestoreCompatible
                ? MigrationOutcome.AlreadyCurrent
                : MigrationOutcome.PreservedLocked;
            return new MigrationExecutionResult(noMigrationOutcome, null);
        }

        // Migration required but no chain available.
        if (!ctx.MigrationChainAvailable)
        {
            return new MigrationExecutionResult(MigrationOutcome.PreservedLocked, null);
        }

        // Enforce migration_retry_limit = 1 per launch.
        if (_failedMigrationVersions.Contains(ctx.ArtifactSchemaVersion))
        {
            return new MigrationExecutionResult(MigrationOutcome.PreservedLocked, null);
        }

        // Resolve ordered chain from artifact version to target version.
        var chain = BuildChain(ctx.ArtifactSchemaVersion, ctx.TargetSchemaVersion);
        if (chain is null || chain.Count == 0)
        {
            _failedMigrationVersions.Add(ctx.ArtifactSchemaVersion);
            return new MigrationExecutionResult(MigrationOutcome.PreservedLocked, null);
        }

        // Apply migration steps on a staging copy — original is never mutated.
        var stagingPayload = DeepCopyPayload(originalPayload);
        var stepDurations = new List<long>(chain.Count);
        var chainVersions = new List<string>(chain.Count + 1) { ctx.ArtifactSchemaVersion.ToString(CultureInfo.InvariantCulture) };

        var stagingOk = true;
        foreach (var step in chain)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                stagingPayload = step.MigrationFn(stagingPayload);
                sw.Stop();
                stepDurations.Add(sw.ElapsedMilliseconds);
                chainVersions.Add(step.ToVersion.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
                sw.Stop();
                stepDurations.Add(sw.ElapsedMilliseconds);
                stagingOk = false;
                break;
            }
        }

        if (!stagingOk)
        {
            _failedMigrationVersions.Add(ctx.ArtifactSchemaVersion);
            return new MigrationExecutionResult(MigrationOutcome.PreservedLocked, null);
        }

        // Verify: re-encode the staging payload and confirm it is non-empty.
        var verifyOk = VerifyMigratedPayload(stagingPayload);
        if (!verifyOk)
        {
            _failedMigrationVersions.Add(ctx.ArtifactSchemaVersion);
            return new MigrationExecutionResult(MigrationOutcome.PreservedLocked, null);
        }

        // Promotion: staging copy becomes the authoritative result.
        var newGeneration = ctx.OldGeneration + 1;
        var record = new MigrationRecord(
            ArtifactKind: ctx.ArtifactKind,
            OldGeneration: ctx.OldGeneration,
            NewGeneration: newGeneration,
            OldVersion: ctx.ArtifactSchemaVersion,
            NewVersion: ctx.TargetSchemaVersion,
            ChainVersions: chainVersions.AsReadOnly(),
            StepDurationsMs: stepDurations.AsReadOnly(),
            Outcome: MigrationOutcome.Upgraded,
            Timestamp: DateTimeOffset.UtcNow);

        _migrationHistory.Add(record);

        return new MigrationExecutionResult(MigrationOutcome.Upgraded, stagingPayload, record);
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static MigrationOutcome ComputeOutcome(
        ArtifactContext ctx,
        bool migrationExecuted,
        bool stagingOk,
        bool verifyOk,
        bool promotionSuccess)
    {
        // Quarantined: corrupt artifact — takes priority over all other conditions.
        if (!ctx.ParseOk || !ctx.IntegrityOk)
        {
            return MigrationOutcome.Quarantined;
        }

        if (ctx.MigrationRequired)
        {
            if (!ctx.MigrationChainAvailable)
            {
                return MigrationOutcome.PreservedLocked;
            }

            if (migrationExecuted && stagingOk && verifyOk && promotionSuccess)
            {
                return MigrationOutcome.Upgraded;
            }

            return MigrationOutcome.PreservedLocked;
        }

        // No migration required.
        return ctx.DirectRestoreCompatible
            ? MigrationOutcome.AlreadyCurrent
            : MigrationOutcome.PreservedLocked;
    }

    /// <summary>
    /// Builds an ordered migration chain from <paramref name="fromVersion"/> to
    /// <paramref name="toVersion"/> using registered steps.
    /// Returns null when no complete chain can be constructed.
    /// </summary>
    private List<MigrationStep>? BuildChain(int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            return null;
        }

        // Sort steps ascending by FromVersion for deterministic traversal.
        var sorted = _steps.OrderBy(s => s.FromVersion).ToList();
        var chain = new List<MigrationStep>();
        var current = fromVersion;

        while (current < toVersion)
        {
            var next = sorted.FirstOrDefault(s => s.FromVersion == current);
            if (next is null)
            {
                return null;
            }

            chain.Add(next);
            current = next.ToVersion;
        }

        return current == toVersion ? chain : null;
    }

    /// <summary>
    /// Verifies a migrated payload is structurally valid: non-null, non-empty,
    /// and re-encodeable via the canonical JSON encoder.
    /// </summary>
    private static bool VerifyMigratedPayload(Dictionary<string, object?> payload)
    {
        if (payload is null || payload.Count == 0)
        {
            return false;
        }

        try
        {
            var encoded = Persistence.CanonicalJsonEncode(payload);
            return !string.IsNullOrEmpty(encoded) && encoded.Length >= 2; // at minimum "{}"
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Produces a shallow copy of a payload dictionary for use as a staging copy.
    /// Nested object references are not deep-copied — migration functions are
    /// responsible for producing fresh dictionaries when mutating nested data.
    /// </summary>
    private static Dictionary<string, object?> DeepCopyPayload(Dictionary<string, object?> payload)
    {
        return new Dictionary<string, object?>(payload, StringComparer.Ordinal);
    }
}

/// <summary>
/// Result returned by <see cref="SaveMigrator.ExecuteMigration"/>.
/// </summary>
public sealed class MigrationExecutionResult
{
    /// <summary>The computed migration outcome.</summary>
    public MigrationOutcome Outcome { get; }

    /// <summary>
    /// The promoted payload after a successful migration.
    /// Non-null only when <see cref="Outcome"/> is <see cref="MigrationOutcome.Upgraded"/>.
    /// </summary>
    public Dictionary<string, object?>? MigratedPayload { get; }

    /// <summary>
    /// The migration record written on success.
    /// Non-null only when <see cref="Outcome"/> is <see cref="MigrationOutcome.Upgraded"/>.
    /// </summary>
    public MigrationRecord? Record { get; }

    internal MigrationExecutionResult(
        MigrationOutcome outcome,
        Dictionary<string, object?>? migratedPayload,
        MigrationRecord? record = null)
    {
        Outcome = outcome;
        MigratedPayload = migratedPayload;
        Record = record;
    }
}
