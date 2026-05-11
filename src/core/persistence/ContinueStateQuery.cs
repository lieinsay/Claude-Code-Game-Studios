namespace CloudWeaverVoyage.Core;

/// <summary>
/// Artifact health classification used by the restore-readiness formula.
/// </summary>
public enum ArtifactState
{
    /// <summary>Artifact is present, valid, and safe to restore.</summary>
    Safe = 0,

    /// <summary>Artifact is present but failed integrity or version checks and must not be used.</summary>
    Quarantined = 1,

    /// <summary>No artifact archive is present on disk.</summary>
    Missing = 2,
}

/// <summary>
/// Write barrier mode active for the current Continue session.
/// </summary>
public enum WriteBarrierMode
{
    /// <summary>Full read-write persistence is available.</summary>
    None = 0,

    /// <summary>Reads are possible but new saves cannot be committed (WriteLocked storage).</summary>
    SaveLocked = 1,

    /// <summary>No persistence at all; session is ephemeral.</summary>
    EphemeralOnly = 2,
}

/// <summary>
/// Readiness metadata for a single artifact kind, supplied by the persistence layer.
/// All fields correspond directly to ADR-0003 restore_readiness formula variables.
/// </summary>
/// <param name="State">Safe / Quarantined / Missing classification.</param>
/// <param name="IntegrityOk">True when checksum and structural validation pass.</param>
/// <param name="VersionCompatible">True when the saved schema version is supported by the current binary.</param>
/// <param name="StableIdsResolved">True when all stable-ID references in the artifact are present in the content registry.</param>
/// <param name="MigrationRequired">True when the artifact can only be loaded after an automatic schema migration.</param>
public sealed record ArtifactStatus(
    ArtifactState State,
    bool IntegrityOk,
    bool VersionCompatible,
    bool StableIdsResolved,
    bool MigrationRequired);

/// <summary>
/// Full output of <see cref="ContinueStateQuery.QueryContinueState"/>.
/// Contains all required fields per AC-8.
/// </summary>
/// <param name="Availability">Enabled / PreservedLocked / Hidden visibility decision.</param>
/// <param name="StorageCapability">Storage capability passed into the query (forwarded for tracing).</param>
/// <param name="WriteBarrier">Write constraint active for this Continue session.</param>
/// <param name="ReasonCode">Machine-readable reason when Availability is not Enabled; empty string when Enabled.</param>
/// <param name="CurrentGeneration">Save generation number passed into the query.</param>
/// <param name="ArtifactKind">Artifact kind this result was computed for; always "progress" for Continue.</param>
public sealed record ContinueStateResult(
    ContinueAvailability Availability,
    StorageCapability StorageCapability,
    WriteBarrierMode WriteBarrier,
    string ReasonCode,
    int CurrentGeneration,
    string ArtifactKind = "progress");

/// <summary>
/// Stateless query layer that computes Continue availability and restore readiness from artifact status
/// and storage capability, as defined by the ADR-0003 formula.
/// </summary>
/// <remarks>
/// ADR-0003 restore_readiness formula:
/// <code>
/// restore_readiness(K) =
///     archive_present[K]          // State != Missing
///     AND artifact_state[K] = Safe
///     AND integrity_ok[K]
///     AND version_compatible[K]
///     AND stable_ids_resolved[K]
///     AND NOT migration_required[K]
///     AND NOT quarantined[K]      // implied by State = Safe, but checked explicitly
/// </code>
///
/// Continue availability:
/// <code>
/// Enabled         if S IN {PersistentAvailable, WriteLocked} AND archive_present AND restore_readiness
/// PreservedLocked if S IN {PersistentAvailable, WriteLocked} AND archive_present AND NOT restore_readiness
/// Hidden          otherwise
/// </code>
///
/// Non-interference rule (AC-9 / AC-10):
/// The Continue result is always driven by progressStatus alone.
/// A Quarantined settings artifact does NOT prevent Continue (AC-9).
/// A Quarantined progress artifact prevents Continue regardless of settings (AC-10).
/// </remarks>
public static class ContinueStateQuery
{
    /// <summary>Reason code emitted when migration is required and restore is blocked.</summary>
    public const string ReasonMigrationRequired = "migration_required";

    /// <summary>Reason code emitted when the saved schema version is not supported.</summary>
    public const string ReasonVersionIncompatible = "version_incompatible";

    /// <summary>Reason code emitted when the integrity check failed.</summary>
    public const string ReasonIntegrityFailed = "integrity_failed";

    /// <summary>Reason code emitted when the artifact is quarantined.</summary>
    public const string ReasonQuarantined = "quarantined";

    /// <summary>Reason code emitted when storage is EphemeralOnly and no persistent archive can be offered.</summary>
    public const string ReasonStorageEphemeralOnly = "storage_ephemeral_only";

    /// <summary>
    /// Computes the ADR-0003 restore_readiness predicate for one artifact.
    /// Returns true only when all conditions are satisfied.
    /// </summary>
    /// <param name="status">Artifact readiness metadata to evaluate.</param>
    /// <returns>
    /// True when the artifact can be safely restored:
    /// archive present, state is Safe, integrity passes, version is compatible,
    /// all stable IDs are resolved, and no migration is required.
    /// </returns>
    /// <example>
    /// <code>
    /// var status = new ArtifactStatus(ArtifactState.Safe, true, true, true, false);
    /// bool ready = ContinueStateQuery.ComputeRestoreReadiness(status); // true
    /// </code>
    /// </example>
    public static bool ComputeRestoreReadiness(ArtifactStatus status)
    {
        // archive_present: State != Missing
        if (status.State == ArtifactState.Missing)
        {
            return false;
        }

        // artifact_state = Safe (covers NOT quarantined)
        if (status.State == ArtifactState.Quarantined)
        {
            return false;
        }

        // integrity_ok
        if (!status.IntegrityOk)
        {
            return false;
        }

        // version_compatible
        if (!status.VersionCompatible)
        {
            return false;
        }

        // stable_ids_resolved
        if (!status.StableIdsResolved)
        {
            return false;
        }

        // NOT migration_required
        if (status.MigrationRequired)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Computes Continue availability and write barrier from storage capability and progress artifact status.
    /// The settings artifact status is checked only to enforce the non-interference rule (AC-9 / AC-10);
    /// it does not change the continue result.
    /// </summary>
    /// <param name="storageCapability">Current storage capability from the persistence detector.</param>
    /// <param name="progressStatus">Readiness metadata for the "progress" artifact.</param>
    /// <param name="settingsStatus">
    /// Readiness metadata for the "settings" artifact, or null if not evaluated.
    /// Used only for non-interference validation; does not affect the returned availability.
    /// </param>
    /// <param name="currentGeneration">Current save generation number to embed in the result.</param>
    /// <returns>
    /// A <see cref="ContinueStateResult"/> containing all required AC-8 fields.
    /// </returns>
    /// <example>
    /// <code>
    /// var status = new ArtifactStatus(ArtifactState.Safe, true, true, true, false);
    /// var result = ContinueStateQuery.QueryContinueState(
    ///     StorageCapability.PersistentAvailable, status, null, generation: 5);
    /// // result.Availability == ContinueAvailability.Enabled
    /// // result.WriteBarrier == WriteBarrierMode.None
    /// // result.ReasonCode == ""
    /// </code>
    /// </example>
    public static ContinueStateResult QueryContinueState(
        StorageCapability storageCapability,
        ArtifactStatus progressStatus,
        ArtifactStatus? settingsStatus,
        int currentGeneration)
    {
        // EphemeralOnly: no persistent archive can be offered.
        // If archive is missing → Hidden; if archive exists → PreservedLocked (cannot become new save point).
        if (storageCapability == StorageCapability.EphemeralOnly)
        {
            if (progressStatus.State == ArtifactState.Missing)
            {
                return new ContinueStateResult(
                    Availability: ContinueAvailability.Hidden,
                    StorageCapability: storageCapability,
                    WriteBarrier: WriteBarrierMode.EphemeralOnly,
                    ReasonCode: ReasonStorageEphemeralOnly,
                    CurrentGeneration: currentGeneration);
            }

            // Archive exists but storage is ephemeral — offer PreservedLocked so the player
            // knows their prior save exists but cannot be continued into a new save point.
            return new ContinueStateResult(
                Availability: ContinueAvailability.PreservedLocked,
                StorageCapability: storageCapability,
                WriteBarrier: WriteBarrierMode.EphemeralOnly,
                ReasonCode: ReasonStorageEphemeralOnly,
                CurrentGeneration: currentGeneration);
        }

        // Storage is PersistentAvailable or WriteLocked from here.
        // Determine write barrier.
        var writeBarrier = storageCapability == StorageCapability.WriteLocked
            ? WriteBarrierMode.SaveLocked
            : WriteBarrierMode.None;

        // AC-6: no archive → Hidden.
        if (progressStatus.State == ArtifactState.Missing)
        {
            return new ContinueStateResult(
                Availability: ContinueAvailability.Hidden,
                StorageCapability: storageCapability,
                WriteBarrier: writeBarrier,
                ReasonCode: string.Empty,
                CurrentGeneration: currentGeneration);
        }

        // Archive is present. Evaluate restore readiness from progressStatus only (non-interference rule).
        var restoreReady = ComputeRestoreReadiness(progressStatus);

        if (restoreReady)
        {
            // AC-3: PersistentAvailable + archive + readiness → Enabled, WriteBarrier=None
            // AC-4: WriteLocked + archive + readiness → Enabled, WriteBarrier=SaveLocked
            return new ContinueStateResult(
                Availability: ContinueAvailability.Enabled,
                StorageCapability: storageCapability,
                WriteBarrier: writeBarrier,
                ReasonCode: string.Empty,
                CurrentGeneration: currentGeneration);
        }

        // AC-5 / AC-7: archive present but restore not ready → PreservedLocked with specific reason.
        var reasonCode = DetermineReasonCode(progressStatus);

        return new ContinueStateResult(
            Availability: ContinueAvailability.PreservedLocked,
            StorageCapability: storageCapability,
            WriteBarrier: writeBarrier,
            ReasonCode: reasonCode,
            CurrentGeneration: currentGeneration);
    }

    /// <summary>
    /// Determines the most specific reason code for a non-ready artifact.
    /// Priority: quarantined > migration_required > version_incompatible > integrity_failed.
    /// </summary>
    private static string DetermineReasonCode(ArtifactStatus status)
    {
        if (status.State == ArtifactState.Quarantined)
        {
            return ReasonQuarantined;
        }

        if (status.MigrationRequired)
        {
            return ReasonMigrationRequired;
        }

        if (!status.VersionCompatible)
        {
            return ReasonVersionIncompatible;
        }

        if (!status.IntegrityOk)
        {
            return ReasonIntegrityFailed;
        }

        // StableIdsResolved = false with no other cause — fall back to version_incompatible
        // to give a stable, actionable code (content registry not populated for this save).
        return ReasonVersionIncompatible;
    }
}
