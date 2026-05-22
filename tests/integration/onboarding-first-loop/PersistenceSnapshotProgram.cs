using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #18 Story 003: Onboarding Persistence Snapshot ===");

var failed = 0;
var total = 0;

Run("AC-1: snapshot contains completed suppressed complete flag and schema", test_snapshot_contains_required_fields);
Run("AC-2: completed hints do not repeat after load", test_completed_hints_do_not_repeat_after_load);
Run("AC-3: mid-loop restore preserves next eligible hint", test_mid_loop_restore_preserves_next_eligible_hint);
Run("AC-4: malformed data is diagnosed without crashing", test_malformed_data_is_diagnosed_without_crashing);
Run("AC-5: preferences remain separate from progress.onboarding", test_preferences_remain_separate_from_progress);
Run("REG-1: canonical Persistence save/load restores progress.onboarding", test_canonical_persistence_restores_onboarding);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 003 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 003 validation passed: {total}/{total} checks passed.");
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
		failed++;
		Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
		return;
	}

	failed++;
	Console.Error.WriteLine($"[FAIL] {label}");
}

static bool test_snapshot_contains_required_fields()
{
	var onboarding = CompleteThrough(OnboardingManager.OpenChartStepId);
	onboarding.SuppressStep(OnboardingManager.AdvancePressureStepId);

	var package = onboarding.BuildSnapshotPackage();
	var completed = StringList(package.Payload["completed_step_ids"]);
	var suppressed = StringList(package.Payload["suppressed_step_ids"]);

	return package.DomainId == OnboardingManager.ProgressDomainId
		&& package.SnapshotSchemaVersion == OnboardingManager.SnapshotSchemaVersion
		&& package.DomainState == SnapshotDomainState.Ready
		&& Convert.ToInt32(package.Payload["schema_version"]) == OnboardingManager.SnapshotSchemaVersion
		&& completed.SequenceEqual(new[] { OnboardingManager.FindHubHudStepId, OnboardingManager.OpenChartStepId })
		&& suppressed.SequenceEqual(new[] { OnboardingManager.AdvancePressureStepId })
		&& package.Payload.ContainsKey("first_loop_complete")
		&& package.Payload["first_loop_complete"] is false
		&& package.ValidateContract().Valid;
}

static bool test_completed_hints_do_not_repeat_after_load()
{
	var source = CompleteThrough(OnboardingManager.OpenChartStepId);
	var package = source.BuildSnapshotPackage();
	var restored = new OnboardingManager();

	var result = restored.RestoreFromSnapshotPackage(package);
	var hint = restored.EvaluateNextHint();

	return result.Success
		&& restored.GetStepProgress(OnboardingManager.OpenChartStepId).State == OnboardingStepState.Completed
		&& hint is not null
		&& hint.StepId == OnboardingManager.SelectRouteStepId
		&& hint.StepId != OnboardingManager.OpenChartStepId;
}

static bool test_mid_loop_restore_preserves_next_eligible_hint()
{
	var source = CompleteThrough(OnboardingManager.DepartRouteStepId);
	var restored = new OnboardingManager();
	restored.RestoreFromSnapshotPackage(source.BuildSnapshotPackage());
	var hint = restored.EvaluateNextHint();

	return restored.GetStepProgress(OnboardingManager.FindHubHudStepId).State == OnboardingStepState.Completed
		&& restored.GetStepProgress(OnboardingManager.DepartRouteStepId).State == OnboardingStepState.Completed
		&& restored.GetStepProgress(OnboardingManager.AdvancePressureStepId).State == OnboardingStepState.Eligible
		&& hint is not null
		&& hint.StepId == OnboardingManager.AdvancePressureStepId;
}

static bool test_malformed_data_is_diagnosed_without_crashing()
{
	var package = new SnapshotPackage
	{
		DomainId = OnboardingManager.ProgressDomainId,
		SnapshotSchemaVersion = 99,
		DomainState = SnapshotDomainState.Ready,
	};
	package.ContentDomainVersions["onboarding-first-loop"] = "future";
	package.Payload["schema_version"] = 99;
	package.Payload["completed_step_ids"] = new object?[]
	{
		OnboardingManager.FindHubHudStepId,
		"unknown.future.step",
		null,
	};
	package.Payload["suppressed_step_ids"] = "not-a-list";

	var restored = new OnboardingManager();
	var result = restored.RestoreFromSnapshotPackage(package);

	return !result.Success
		&& result.Diagnostics.Any(item => item == "unsupported_schema_version")
		&& result.Diagnostics.Any(item => item == "unknown_step_id:unknown.future.step")
		&& result.Diagnostics.Any(item => item == "invalid_completed_step_ids_entry")
		&& result.Diagnostics.Any(item => item == "invalid_suppressed_step_ids_type")
		&& restored.GetStepProgress(OnboardingManager.FindHubHudStepId).State == OnboardingStepState.Completed;
}

static bool test_preferences_remain_separate_from_progress()
{
	var onboarding = CompleteThrough(OnboardingManager.FindHubHudStepId);
	var package = onboarding.BuildSnapshotPackage();

	return !package.Payload.ContainsKey("disabled")
		&& !package.Payload.ContainsKey("reset_requested")
		&& !package.Payload.ContainsKey("settings.onboarding")
		&& package.DomainId == OnboardingManager.ProgressDomainId;
}

static bool test_canonical_persistence_restores_onboarding()
{
	var persistence = new Persistence();
	var source = CompleteThrough(OnboardingManager.DepartRouteStepId);
	source.RegisterPersistence(persistence);
	var save = persistence.RequestSaveProgress();

	var restored = new OnboardingManager();
	restored.RegisterPersistence(persistence);
	var load = persistence.RequestLoadProgress();

	return save.Success
		&& load.Success
		&& restored.GetStepProgress(OnboardingManager.DepartRouteStepId).State == OnboardingStepState.Completed
		&& restored.EvaluateNextHint()?.StepId == OnboardingManager.AdvancePressureStepId
		&& restored.LastRestoreDiagnostics.Count == 0;
}

static OnboardingManager CompleteThrough(string stepId)
{
	var onboarding = new OnboardingManager();
	foreach (var id in onboarding.StepIds)
	{
		onboarding.CompleteStep(id);
		if (id == stepId)
		{
			break;
		}
	}

	return onboarding;
}

static IReadOnlyList<string> StringList(object? value)
{
	if (value is string[] array)
	{
		return array;
	}

	if (value is IEnumerable<string> typed)
	{
		return typed.ToArray();
	}

	if (value is System.Collections.IEnumerable list)
	{
		return list.Cast<object?>().Select(item => Convert.ToString(item) ?? string.Empty).ToArray();
	}

	return Array.Empty<string>();
}
