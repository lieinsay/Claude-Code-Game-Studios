namespace CloudWeaverVoyage.Core;

/// <summary>
/// Persistence-owned storage capability states consumed by the session shell.
/// </summary>
public enum StorageCapability
{
	PersistentAvailable = 0,
	WriteLocked = 1,
	EphemeralOnly = 2,
}

/// <summary>
/// Raw desktop persistence signal payload forwarded unchanged to persistence.
/// </summary>
public sealed record PersistenceProbe(
	bool IndexedDbAvailable,
	long QuotaBytes,
	long UsedBytes,
	bool WriteTestPassed);

/// <summary>
/// Persistence-owned continue point validation details consumed by the shell.
/// </summary>
public sealed record ContinuePointValidation(
	bool Exists,
	bool IntegrityValid,
	bool ContentDomainMatches,
	string? LockedReason = null);

/// <summary>
/// Combined persistence response for a raw storage probe.
/// </summary>
public sealed record StorageCapabilityEvaluation(
	StorageCapability StorageCapability,
	ContinuePointValidation ContinuePoint);

/// <summary>
/// Persistence capability evaluator. The shell forwards raw probes and does not calculate storage capability.
/// </summary>
public interface IStorageCapabilityEvaluator
{
	StorageCapabilityEvaluation Evaluate(PersistenceProbe rawProbe);
}

/// <summary>
/// Stable result code for Start entry decisions under the current storage capability.
/// </summary>
public enum StorageStartResultCode
{
	AcceptedPersistent = 0,
	RequiresEphemeralConfirmation = 1,
	AcceptedEphemeral = 2,
}

/// <summary>
/// Stable result code for Continue entry decisions under the current storage capability.
/// </summary>
public enum StorageContinueResultCode
{
	Accepted = 0,
	AcceptedWriteLocked = 1,
	Hidden = 2,
	PreservedLocked = 3,
	EphemeralUnavailable = 4,
}

/// <summary>
/// Shell-facing Start storage decision.
/// </summary>
public sealed record StorageStartResult(
	StorageStartResultCode Code,
	string? Warning = null,
	bool CreatesPersistentContinuePoint = false);

/// <summary>
/// Shell-facing Continue storage decision.
/// </summary>
public sealed record StorageContinueResult(
	StorageContinueResultCode Code,
	string? Warning = null,
	string? LockedReason = null);

/// <summary>
/// Coordinates shell storage decisions from persistence-provided capability and continue validation.
/// </summary>
public sealed class StorageCapabilityCoordinator
{
	public const string EphemeralSessionFlag = "EPHEMERAL";
	public const string EphemeralWarning = "ephemeral_session_no_save";
	public const string WriteLockedContinueWarning = "new_progress_currently_cannot_be_reliably_saved";

	private readonly IStorageCapabilityEvaluator evaluator;
	private readonly HashSet<string> sessionFlags = [];

	public StorageCapabilityCoordinator(IStorageCapabilityEvaluator evaluator)
	{
		this.evaluator = evaluator;
		ContinueState = new ContinueEntryState(ContinueAvailability.Hidden);
	}

	/// <summary>Current persistence-provided storage capability.</summary>
	public StorageCapability StorageCapability { get; private set; } = StorageCapability.EphemeralOnly;

	/// <summary>Current shell continue state derived from persistence-provided continue validation.</summary>
	public ContinueEntryState ContinueState { get; private set; }

	/// <summary>Current immutable session flags for the accepted session.</summary>
	public IReadOnlySet<string> SessionFlags => sessionFlags;

	/// <summary>Last raw probe passed into persistence.</summary>
	public PersistenceProbe? LastForwardedProbe { get; private set; }

	/// <summary>Whether the latest ephemeral Start has been confirmed by the player.</summary>
	public bool EphemeralStartConfirmed { get; private set; }

	/// <summary>Whether this coordinator generated a persistent continue point for the current Start.</summary>
	public bool PersistentContinuePointCreated { get; private set; }

	/// <summary>Raised after persistence returns a changed storage capability.</summary>
	public event Action<StorageCapability, StorageCapability>? StorageCapabilityChanged;

	/// <summary>
	/// Forwards the raw platform persistence probe to persistence and consumes its returned state.
	/// </summary>
	public StorageCapabilityEvaluation OnPersistenceProbe(PersistenceProbe rawProbe)
	{
		LastForwardedProbe = rawProbe;
		var previous = StorageCapability;
		var evaluation = evaluator.Evaluate(rawProbe);
		StorageCapability = evaluation.StorageCapability;
		ContinueState = ToContinueState(evaluation.ContinuePoint);

		if (previous != StorageCapability)
		{
			StorageCapabilityChanged?.Invoke(previous, StorageCapability);
		}

		return evaluation;
	}

	/// <summary>
	/// Checks whether Start can proceed or must first show a temporary-session confirmation.
	/// </summary>
	public StorageStartResult SelectStart()
	{
		if (StorageCapability == StorageCapability.PersistentAvailable)
		{
			PersistentContinuePointCreated = true;
			return new StorageStartResult(
				StorageStartResultCode.AcceptedPersistent,
				CreatesPersistentContinuePoint: true);
		}

		return new StorageStartResult(
			StorageStartResultCode.RequiresEphemeralConfirmation,
			Warning: EphemeralWarning);
	}

	/// <summary>
	/// Confirms a temporary Start after the shell has shown the no-save warning.
	/// </summary>
	public StorageStartResult ConfirmEphemeralStart()
	{
		EphemeralStartConfirmed = true;
		PersistentContinuePointCreated = false;
		sessionFlags.Add(EphemeralSessionFlag);
		return new StorageStartResult(
			StorageStartResultCode.AcceptedEphemeral,
			Warning: EphemeralWarning,
			CreatesPersistentContinuePoint: false);
	}

	/// <summary>
	/// Checks whether Continue can proceed under the current storage and continue state.
	/// </summary>
	public StorageContinueResult SelectContinue()
	{
		if (StorageCapability == StorageCapability.EphemeralOnly)
		{
			return new StorageContinueResult(
				StorageContinueResultCode.EphemeralUnavailable,
				LockedReason: ContinueState.LockedReason);
		}

		return ContinueState.Availability switch
		{
			ContinueAvailability.Hidden => new StorageContinueResult(StorageContinueResultCode.Hidden),
			ContinueAvailability.PreservedLocked => new StorageContinueResult(
				StorageContinueResultCode.PreservedLocked,
				LockedReason: ContinueState.LockedReason),
			ContinueAvailability.Enabled when StorageCapability == StorageCapability.WriteLocked =>
				new StorageContinueResult(
					StorageContinueResultCode.AcceptedWriteLocked,
					Warning: WriteLockedContinueWarning),
			ContinueAvailability.Enabled => new StorageContinueResult(StorageContinueResultCode.Accepted),
			_ => new StorageContinueResult(StorageContinueResultCode.Hidden),
		};
	}

	private static ContinueEntryState ToContinueState(ContinuePointValidation validation)
	{
		if (!validation.Exists)
		{
			return new ContinueEntryState(ContinueAvailability.Hidden);
		}

		if (validation.IntegrityValid && validation.ContentDomainMatches)
		{
			return new ContinueEntryState(ContinueAvailability.Enabled);
		}

		return new ContinueEntryState(
			ContinueAvailability.PreservedLocked,
			validation.LockedReason ?? "continue_point_validation_failed");
	}
}
