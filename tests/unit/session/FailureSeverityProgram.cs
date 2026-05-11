using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 004: Failure Severity & Recovery Paths - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: content domain FAILED enters RecoveryRequired", Ac1ContentFailedRecovers);
Run("AC-2: VERSION_INCOMPATIBLE enters FatalBlocked", Ac2VersionIncompatibleFatal);
Run("AC-3: FAILED beats LOADING in required content aggregation", Ac3FailedBeatsLoading);
Run("AC-4: VERSION_INCOMPATIBLE beats FAILED in required content aggregation", Ac4VersionIncompatiblePrecedence);
Run("AC-5: failure handling preserves existing continue point", Ac5ContinuePointPreserved);
Run("AC-6: audio SoftFail or EphemeralOnly produces SoftFail", Ac6SoftFailureConditions);
Run("AC-7: WriteLocked produces SoftFail", Ac7WriteLockedSoftFail);
Run("AC-8: recoverable content failure produces RecoverableFail", Ac8RecoverableContentFailure);
Run("AC-9: hard gate failures produce HardFail", Ac9HardGateFailures);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 004 AC validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 004 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1ContentFailedRecovers()
{
	var aggregate = FailureRecoveryPolicy.AggregateRequiredContentDomains(
	[
		RequiredContentDomainState.Complete,
		RequiredContentDomainState.Failed,
	]);
	var request = BaselineRequest() with
	{
		ContentDomainFailureClass = FailureRecoveryPolicy.ClassifyContentDomainFailure(aggregate),
	};
	var result = FailureRecoveryPolicy.HandleFailure(request);

	return aggregate == RequiredContentDomainStatus.Failed
		&& result.Severity == FailureSeverity.RecoverableFail
		&& result.TargetState == ShellState.RecoveryRequired
		&& result.Actions.Contains(FailureRecoveryAction.Retry)
		&& result.Actions.Contains(FailureRecoveryAction.NewSession)
		&& result.Actions.Contains(FailureRecoveryAction.ReturnTitle);
}

static bool Ac2VersionIncompatibleFatal()
{
	var aggregate = FailureRecoveryPolicy.AggregateRequiredContentDomains(
	[
		RequiredContentDomainState.Complete,
		RequiredContentDomainState.VersionIncompatible,
	]);
	var request = BaselineRequest() with
	{
		ContentDomainFailureClass = FailureRecoveryPolicy.ClassifyContentDomainFailure(aggregate),
	};
	var result = FailureRecoveryPolicy.HandleFailure(request);

	return aggregate == RequiredContentDomainStatus.VersionIncompatible
		&& result.Severity == FailureSeverity.HardFail
		&& result.TargetState == ShellState.FatalBlocked
		&& !result.AllowsGameplay;
}

static bool Ac3FailedBeatsLoading()
{
	var aggregate = FailureRecoveryPolicy.AggregateRequiredContentDomains(
	[
		RequiredContentDomainState.Complete,
		RequiredContentDomainState.Failed,
		RequiredContentDomainState.Loading,
	]);

	return aggregate == RequiredContentDomainStatus.Failed;
}

static bool Ac4VersionIncompatiblePrecedence()
{
	var aggregate = FailureRecoveryPolicy.AggregateRequiredContentDomains(
	[
		RequiredContentDomainState.Failed,
		RequiredContentDomainState.Loading,
		RequiredContentDomainState.VersionIncompatible,
	]);

	return aggregate == RequiredContentDomainStatus.VersionIncompatible
		&& FailureRecoveryPolicy.ClassifyContentDomainFailure(aggregate) == ContentDomainFailureClass.Fatal;
}

static bool Ac5ContinuePointPreserved()
{
	var continuePoint = new ContinuePointSnapshot("safe-continue-001", ContinueAvailability.Enabled, 42);
	var first = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		ContentDomainFailureClass = ContentDomainFailureClass.Recoverable,
		ContinuePoint = continuePoint,
	});
	var third = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		ContentDomainFailureClass = ContentDomainFailureClass.Recoverable,
		ContinuePoint = first.PreservedContinuePoint,
	});

	return ReferenceEquals(continuePoint, first.PreservedContinuePoint)
		&& ReferenceEquals(continuePoint, third.PreservedContinuePoint)
		&& third.PreservedContinuePoint == continuePoint
		&& third.PreservedContinuePoint.Availability == ContinueAvailability.Enabled;
}

static bool Ac6SoftFailureConditions()
{
	var audio = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		AudioGate = AudioGate.SoftFail,
	});
	var storage = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		StorageCapability = StorageCapability.EphemeralOnly,
	});
	var recoverableBeatsSoft = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		ContentDomainFailureClass = ContentDomainFailureClass.Recoverable,
		StorageCapability = StorageCapability.EphemeralOnly,
	});

	return audio.Severity == FailureSeverity.SoftFail
		&& storage.Severity == FailureSeverity.SoftFail
		&& audio.AllowsGameplay
		&& storage.AllowsGameplay
		&& recoverableBeatsSoft.Severity == FailureSeverity.RecoverableFail
		&& recoverableBeatsSoft.TargetState == ShellState.RecoveryRequired;
}

static bool Ac7WriteLockedSoftFail()
{
	var result = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		StorageCapability = StorageCapability.WriteLocked,
	});

	return result.Severity == FailureSeverity.SoftFail
		&& result.TargetState == ShellState.SessionStarting
		&& result.AllowsGameplay
		&& result.Reason == "soft_gate_failed";
}

static bool Ac8RecoverableContentFailure()
{
	var result = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		ContentDomainFailureClass = ContentDomainFailureClass.Recoverable,
	});

	return result.Severity == FailureSeverity.RecoverableFail
		&& result.TargetState == ShellState.RecoveryRequired
		&& result.Actions.Contains(FailureRecoveryAction.Retry)
		&& !result.AllowsGameplay;
}

static bool Ac9HardGateFailures()
{
	var baseLoad = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with { BaseLoaded = false });
	var fatalContent = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		ContentDomainFailureClass = ContentDomainFailureClass.Fatal,
	});
	var continueHidden = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		OperationKind = SessionOperationKind.Continue,
		ContinueAvailability = ContinueAvailability.Hidden,
	});
	var resumeNotReady = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		OperationKind = SessionOperationKind.Resume,
		ResumeReady = false,
	});
	var audioHard = FailureRecoveryPolicy.HandleFailure(BaselineRequest() with
	{
		AudioGate = AudioGate.HardFail,
	});

	var results = new[] { baseLoad, fatalContent, continueHidden, resumeNotReady, audioHard };
	return results.All(result =>
		result.Severity == FailureSeverity.HardFail
		&& result.TargetState == ShellState.FatalBlocked
		&& !result.AllowsGameplay);
}

static FailureRecoveryRequest BaselineRequest()
{
	return new FailureRecoveryRequest(
		SessionOperationKind.Start,
		BaseLoaded: true,
		ContentDomainFailureClass.None,
		AudioGate.Pass,
		StorageCapability.PersistentAvailable,
		ContinueAvailability.Enabled,
		ResumeReady: true);
}
