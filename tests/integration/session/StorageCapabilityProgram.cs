using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 005: Storage Capability & Ephemeral Sessions — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: Write-blocked Start requires no-save confirmation and stays ephemeral", Ac1WriteBlockedStartRequiresConfirmation);
Run("AC-2: Shell forwards raw persistence probe and consumes returned capability", Ac2RawProbeForwardedToPersistence);
Run("AC-3: WriteLocked valid Continue is allowed with warning and preserved continue state", Ac3WriteLockedContinueAllowedWithWarning);
Run("AC-4: EphemeralOnly Start confirms temporary session without persistent continue point", Ac4EphemeralOnlyStartConfirmsTemporarySession);
Run("AC-5: Missing continue point maps to Hidden", Ac5MissingContinuePointHidden);
Run("AC-6: Valid continue point maps to Enabled", Ac6ValidContinuePointEnabled);
Run("AC-7: Invalid continue point maps to PreservedLocked and remains present", Ac7InvalidContinuePointPreservedLocked);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 005 AC validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 005 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1WriteBlockedStartRequiresConfirmation()
{
	var coordinator = NewCoordinator(
		StorageCapability.WriteLocked,
		new ContinuePointValidation(Exists: false, IntegrityValid: false, ContentDomainMatches: false));
	coordinator.OnPersistenceProbe(Probe(writeTestPassed: false));

	var start = coordinator.SelectStart();
	var confirmed = coordinator.ConfirmEphemeralStart();

	return start.Code == StorageStartResultCode.RequiresEphemeralConfirmation
		&& start.Warning == StorageCapabilityCoordinator.EphemeralWarning
		&& confirmed.Code == StorageStartResultCode.AcceptedEphemeral
		&& confirmed.CreatesPersistentContinuePoint == false
		&& coordinator.EphemeralStartConfirmed
		&& coordinator.PersistentContinuePointCreated == false
		&& coordinator.SessionFlags.Contains(StorageCapabilityCoordinator.EphemeralSessionFlag);
}

static bool Ac2RawProbeForwardedToPersistence()
{
	var rawProbe = new PersistenceProbe(
		IndexedDbAvailable: false,
		QuotaBytes: 4096,
		UsedBytes: 4095,
		WriteTestPassed: false);
	var evaluator = new ScriptedStorageEvaluator(
		StorageCapability.EphemeralOnly,
		new ContinuePointValidation(Exists: false, IntegrityValid: false, ContentDomainMatches: false));
	var coordinator = new StorageCapabilityCoordinator(evaluator);
	StorageCapability? oldCapability = null;
	StorageCapability? newCapability = null;
	coordinator.StorageCapabilityChanged += (oldValue, newValue) =>
	{
		oldCapability = oldValue;
		newCapability = newValue;
	};

	var evaluation = coordinator.OnPersistenceProbe(rawProbe);

	return evaluator.ReceivedProbes.Count == 1
		&& evaluator.ReceivedProbes[0] == rawProbe
		&& coordinator.LastForwardedProbe == rawProbe
		&& evaluation.StorageCapability == StorageCapability.EphemeralOnly
		&& coordinator.StorageCapability == StorageCapability.EphemeralOnly
		&& oldCapability is null
		&& newCapability is null;
}

static bool Ac3WriteLockedContinueAllowedWithWarning()
{
	var validation = new ContinuePointValidation(
		Exists: true,
		IntegrityValid: true,
		ContentDomainMatches: true);
	var coordinator = NewCoordinator(StorageCapability.WriteLocked, validation);
	coordinator.OnPersistenceProbe(Probe(writeTestPassed: false));

	var result = coordinator.SelectContinue();

	return coordinator.ContinueState.Availability == ContinueAvailability.Enabled
		&& result.Code == StorageContinueResultCode.AcceptedWriteLocked
		&& result.Warning == StorageCapabilityCoordinator.WriteLockedContinueWarning
		&& coordinator.ContinueState.Availability == ContinueAvailability.Enabled
		&& coordinator.PersistentContinuePointCreated == false;
}

static bool Ac4EphemeralOnlyStartConfirmsTemporarySession()
{
	var coordinator = NewCoordinator(
		StorageCapability.EphemeralOnly,
		new ContinuePointValidation(Exists: false, IntegrityValid: false, ContentDomainMatches: false));
	coordinator.OnPersistenceProbe(Probe(indexedDbAvailable: false, writeTestPassed: false));

	var start = coordinator.SelectStart();
	var confirmed = coordinator.ConfirmEphemeralStart();

	return start.Code == StorageStartResultCode.RequiresEphemeralConfirmation
		&& confirmed.Code == StorageStartResultCode.AcceptedEphemeral
		&& coordinator.StorageCapability == StorageCapability.EphemeralOnly
		&& coordinator.ContinueState.Availability == ContinueAvailability.Hidden
		&& coordinator.SessionFlags.Contains(StorageCapabilityCoordinator.EphemeralSessionFlag)
		&& !coordinator.PersistentContinuePointCreated;
}

static bool Ac5MissingContinuePointHidden()
{
	var coordinator = NewCoordinator(
		StorageCapability.PersistentAvailable,
		new ContinuePointValidation(Exists: false, IntegrityValid: true, ContentDomainMatches: true));
	coordinator.OnPersistenceProbe(Probe());

	return coordinator.ContinueState.Availability == ContinueAvailability.Hidden
		&& coordinator.SelectContinue().Code == StorageContinueResultCode.Hidden;
}

static bool Ac6ValidContinuePointEnabled()
{
	var coordinator = NewCoordinator(
		StorageCapability.PersistentAvailable,
		new ContinuePointValidation(Exists: true, IntegrityValid: true, ContentDomainMatches: true));
	coordinator.OnPersistenceProbe(Probe());

	return coordinator.ContinueState.Availability == ContinueAvailability.Enabled
		&& coordinator.SelectContinue().Code == StorageContinueResultCode.Accepted;
}

static bool Ac7InvalidContinuePointPreservedLocked()
{
	var coordinator = NewCoordinator(
		StorageCapability.PersistentAvailable,
		new ContinuePointValidation(
			Exists: true,
			IntegrityValid: false,
			ContentDomainMatches: true,
			LockedReason: "integrity_failed"));
	coordinator.OnPersistenceProbe(Probe());

	var result = coordinator.SelectContinue();

	return coordinator.ContinueState.Availability == ContinueAvailability.PreservedLocked
		&& coordinator.ContinueState.LockedReason == "integrity_failed"
		&& result.Code == StorageContinueResultCode.PreservedLocked
		&& result.LockedReason == "integrity_failed";
}

static StorageCapabilityCoordinator NewCoordinator(
	StorageCapability capability,
	ContinuePointValidation validation)
{
	return new StorageCapabilityCoordinator(new ScriptedStorageEvaluator(capability, validation));
}

static PersistenceProbe Probe(
	bool indexedDbAvailable = true,
	long quotaBytes = 1048576,
	long usedBytes = 1024,
	bool writeTestPassed = true)
{
	return new PersistenceProbe(indexedDbAvailable, quotaBytes, usedBytes, writeTestPassed);
}

sealed class ScriptedStorageEvaluator : IStorageCapabilityEvaluator
{
	private readonly StorageCapabilityEvaluation evaluation;

	public ScriptedStorageEvaluator(StorageCapability capability, ContinuePointValidation validation)
	{
		evaluation = new StorageCapabilityEvaluation(capability, validation);
	}

	public List<PersistenceProbe> ReceivedProbes { get; } = [];

	public StorageCapabilityEvaluation Evaluate(PersistenceProbe rawProbe)
	{
		ReceivedProbes.Add(rawProbe);
		return evaluation;
	}
}
