using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #18 Story 001: First-Loop Step State and Hint Scoring ===");

var failed = 0;
var total = 0;

Run("AC-1: stable first-loop steps initialize in deterministic order", test_stable_steps_initialize_in_order);
Run("AC-2: step completion updates progress deterministically", test_step_completion_updates_progress);
Run("AC-3: hint scoring selects highest eligible hint and stable tie", test_hint_scoring_selects_highest_and_stable_tie);
Run("AC-4: completed steps do not repeat until reset", test_completed_steps_do_not_repeat_until_reset);
Run("AC-5: invalid duplicate and out-of-order events are isolated", test_invalid_duplicate_and_out_of_order_events_are_isolated);
Run("REG-1: hint score formula applies caps and penalties", test_hint_score_formula_applies_caps_and_penalties);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 001 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 001 validation passed: {total}/{total} checks passed.");
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

static bool test_stable_steps_initialize_in_order()
{
	var onboarding = new OnboardingManager();
	var expected = new[]
	{
		OnboardingManager.FindHubHudStepId,
		OnboardingManager.OpenChartStepId,
		OnboardingManager.SelectRouteStepId,
		OnboardingManager.DepartRouteStepId,
		OnboardingManager.AdvancePressureStepId,
		OnboardingManager.NoticeSaveLoadStepId,
		OnboardingManager.ReturnHubStepId,
		OnboardingManager.NoticeSummaryChangeStepId,
	};

	return onboarding.StepIds.SequenceEqual(expected)
		&& onboarding.SnapshotSteps().Count == expected.Length
		&& onboarding.GetStepProgress(OnboardingManager.FindHubHudStepId).State == OnboardingStepState.Eligible
		&& onboarding.GetStepProgress(OnboardingManager.OpenChartStepId).State == OnboardingStepState.NotStarted;
}

static bool test_step_completion_updates_progress()
{
	var onboarding = new OnboardingManager();
	var first = onboarding.CompleteStep(OnboardingManager.FindHubHudStepId);
	var second = onboarding.CompleteStep(OnboardingManager.OpenChartStepId);

	return first.Accepted
		&& second.Accepted
		&& onboarding.GetStepProgress(OnboardingManager.FindHubHudStepId).State == OnboardingStepState.Completed
		&& onboarding.GetStepProgress(OnboardingManager.OpenChartStepId).State == OnboardingStepState.Completed
		&& onboarding.GetStepProgress(OnboardingManager.SelectRouteStepId).State == OnboardingStepState.Eligible
		&& Math.Abs(onboarding.FirstLoopProgressPercent - 25.0d) < 0.001d
		&& onboarding.GetStepProgress(OnboardingManager.OpenChartStepId).CompletionGeneration == 2;
}

static bool test_hint_scoring_selects_highest_and_stable_tie()
{
	var onboarding = new OnboardingManager();
	var high = onboarding.SelectHighestScoringHint(new[]
	{
		new OnboardingHintCandidate(
			OnboardingManager.FindHubHudStepId,
			"hint.low",
			"hub.low",
			BaseStepPriority: 10,
			BlockerBonus: 0,
			SecondsSinceEligible: 0.0d),
		new OnboardingHintCandidate(
			OnboardingManager.OpenChartStepId,
			"hint.high",
			"hub.chart",
			BaseStepPriority: 10,
			BlockerBonus: 30,
			SecondsSinceEligible: 50.0d),
	});

	var tie = onboarding.SelectHighestScoringHint(new[]
	{
		new OnboardingHintCandidate(OnboardingManager.OpenChartStepId, "hint.second", "hub.chart", BaseStepPriority: 30),
		new OnboardingHintCandidate(OnboardingManager.FindHubHudStepId, "hint.first", "hub.hud", BaseStepPriority: 30),
	});

	return high is not null
		&& high.StepId == OnboardingManager.OpenChartStepId
		&& high.Priority == 50
		&& tie is not null
		&& tie.StepId == OnboardingManager.FindHubHudStepId;
}

static bool test_completed_steps_do_not_repeat_until_reset()
{
	var onboarding = new OnboardingManager();
	onboarding.CompleteStep(OnboardingManager.FindHubHudStepId);

	var next = onboarding.EvaluateNextHint();
	onboarding.RecordHintShown(OnboardingManager.OpenChartStepId);
	var completedCandidate = onboarding.SelectHighestScoringHint(new[]
	{
		new OnboardingHintCandidate(OnboardingManager.FindHubHudStepId, "hint.completed", "hub.hud", BaseStepPriority: 500),
		new OnboardingHintCandidate(OnboardingManager.OpenChartStepId, "hint.open_chart", "hub.chart", BaseStepPriority: 20),
	});
	var openChartRepeatCount = onboarding.GetStepProgress(OnboardingManager.OpenChartStepId).RepeatCount;

	onboarding.Reset();
	var resetHint = onboarding.EvaluateNextHint();

	return next is not null
		&& next.StepId == OnboardingManager.OpenChartStepId
		&& completedCandidate is not null
		&& completedCandidate.StepId == OnboardingManager.OpenChartStepId
		&& openChartRepeatCount == 1
		&& resetHint is not null
		&& resetHint.StepId == OnboardingManager.FindHubHudStepId;
}

static bool test_invalid_duplicate_and_out_of_order_events_are_isolated()
{
	var onboarding = new OnboardingManager();
	var outOfOrder = onboarding.CompleteStep(OnboardingManager.DepartRouteStepId);
	var unknown = onboarding.CompleteStep("unknown_step");
	var first = onboarding.CompleteStep(OnboardingManager.FindHubHudStepId);
	var duplicate = onboarding.CompleteStep(OnboardingManager.FindHubHudStepId);

	return !outOfOrder.Accepted
		&& outOfOrder.IgnoredReason == "out_of_order_completion"
		&& !unknown.Accepted
		&& unknown.IgnoredReason == "unknown_step_id"
		&& first.Accepted
		&& !duplicate.Accepted
		&& duplicate.IgnoredReason == "duplicate_completion"
		&& onboarding.GetStepProgress(OnboardingManager.DepartRouteStepId).State == OnboardingStepState.NotStarted
		&& onboarding.GetStepProgress(OnboardingManager.OpenChartStepId).State == OnboardingStepState.Eligible
		&& Math.Abs(onboarding.FirstLoopProgressPercent - 12.5d) < 0.001d;
}

static bool test_hint_score_formula_applies_caps_and_penalties()
{
	return OnboardingManager.CalculateHintPriorityScore(
			baseStepPriority: 40,
			blockerBonus: 10,
			secondsSinceEligible: 999.0d,
			repeatCount: 2) == 30
		&& OnboardingManager.CalculateHintPriorityScore(
			baseStepPriority: 500,
			completed: true) < 0;
}
