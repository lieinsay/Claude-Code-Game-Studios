using System.Collections.ObjectModel;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// First-loop onboarding step state names from ADR-0017.
/// </summary>
public enum OnboardingStepState
{
	/// <summary>The step is known but cannot be prompted yet.</summary>
	NotStarted = 0,

	/// <summary>The step can show a non-modal hint.</summary>
	Eligible = 1,

	/// <summary>The step currently has a visible hint request.</summary>
	Visible = 2,

	/// <summary>The player completed the step.</summary>
	Completed = 3,

	/// <summary>The step is intentionally hidden without completing it.</summary>
	Suppressed = 4,
}

/// <summary>
/// Read-only snapshot of one onboarding step.
/// </summary>
public sealed record OnboardingStepProgress(
	string StepId,
	OnboardingStepState State,
	int CompletionGeneration,
	int RepeatCount);

/// <summary>
/// Immutable hint/highlight request emitted by the onboarding manager.
/// </summary>
public sealed record OnboardingHintRequest(
	string StepId,
	string HintTextKey,
	string? HighlightAnchorId,
	int Priority,
	double DurationSeconds);

/// <summary>
/// Candidate data used by deterministic hint scoring.
/// </summary>
public sealed record OnboardingHintCandidate(
	string StepId,
	string HintTextKey,
	string? HighlightAnchorId,
	int BaseStepPriority,
	int BlockerBonus = 0,
	double SecondsSinceEligible = 0.0d,
	int RepeatCount = 0,
	double DurationSeconds = OnboardingManager.DefaultHintDurationSeconds);

/// <summary>
/// Result of consuming a step completion signal.
/// </summary>
public sealed record OnboardingStepEventResult(
	bool Accepted,
	string? StepId,
	string? IgnoredReason,
	int CompletionGeneration);

/// <summary>
/// Headless first-loop guidance service for #18 onboarding.
/// </summary>
public sealed class OnboardingManager
{
	/// <summary>Stable GDD step: find the Hub HUD.</summary>
	public const string FindHubHudStepId = "find_hub_hud";

	/// <summary>Stable GDD step: open the chart.</summary>
	public const string OpenChartStepId = "open_chart";

	/// <summary>Stable GDD step: select a route.</summary>
	public const string SelectRouteStepId = "select_route";

	/// <summary>Stable GDD step: depart on the selected route.</summary>
	public const string DepartRouteStepId = "depart_route";

	/// <summary>Stable GDD step: advance pressure during exploration.</summary>
	public const string AdvancePressureStepId = "advance_pressure";

	/// <summary>Stable GDD step: notice save/load affordances.</summary>
	public const string NoticeSaveLoadStepId = "notice_save_load";

	/// <summary>Stable GDD step: return to the Hub.</summary>
	public const string ReturnHubStepId = "return_hub";

	/// <summary>Stable GDD step: notice the changed summary after return.</summary>
	public const string NoticeSummaryChangeStepId = "notice_summary_change";

	/// <summary>Default duration for first-loop hint requests.</summary>
	public const double DefaultHintDurationSeconds = 4.0d;

	/// <summary>Maximum score bonus from elapsed unseen time.</summary>
	public const int MaxTimeUnseenBonus = 20;

	/// <summary>Penalty applied to candidates for already completed steps.</summary>
	public const int CompletedStepPenalty = 10_000;

	/// <summary>Penalty applied for each repeated hint exposure.</summary>
	public const int RepeatHintPenalty = 20;

	private static readonly IReadOnlyList<StepDefinition> Definitions = new[]
	{
		new StepDefinition(FindHubHudStepId, "onboarding.hub.find_hud", "hub.hud.status", 80),
		new StepDefinition(OpenChartStepId, "onboarding.hub.open_chart", "hub.chart.console", 75),
		new StepDefinition(SelectRouteStepId, "onboarding.chart.select_route", "chart.route_list", 70),
		new StepDefinition(DepartRouteStepId, "onboarding.chart.depart_route", "chart.depart_button", 70),
		new StepDefinition(AdvancePressureStepId, "onboarding.exploration.advance_pressure", "exploration.pressure", 65),
		new StepDefinition(NoticeSaveLoadStepId, "onboarding.session.notice_save_load", "session.save_load", 55),
		new StepDefinition(ReturnHubStepId, "onboarding.exploration.return_hub", "exploration.return_hub", 65),
		new StepDefinition(NoticeSummaryChangeStepId, "onboarding.hub.notice_summary_change", "hub.summary", 60),
	};

	private static readonly IReadOnlyDictionary<string, StepDefinition> DefinitionsById =
		new ReadOnlyDictionary<string, StepDefinition>(Definitions.ToDictionary(item => item.StepId, StringComparer.Ordinal));

	private readonly Dictionary<string, StepRuntimeState> steps = new(StringComparer.Ordinal);
	private int completionGeneration;

	/// <summary>Creates a new manager with the first GDD step eligible.</summary>
	public OnboardingManager()
	{
		Reset();
	}

	/// <summary>Stable first-loop step IDs in GDD order.</summary>
	public IReadOnlyList<string> StepIds => Definitions.Select(item => item.StepId).ToArray();

	/// <summary>Most recent ignored completion reason, exposed for QA diagnostics.</summary>
	public string? LastIgnoredEventReason { get; private set; }

	/// <summary>Percentage of first-loop steps completed, from 0 to 100.</summary>
	public double FirstLoopProgressPercent =>
		steps.Count == 0 ? 0.0d : steps.Values.Count(item => item.State == OnboardingStepState.Completed) * 100.0d / steps.Count;

	/// <summary>True when every stable first-loop step is completed.</summary>
	public bool IsFirstLoopComplete => steps.Values.All(item => item.State == OnboardingStepState.Completed);

	/// <summary>Returns a read-only snapshot of all tracked steps.</summary>
	public IReadOnlyDictionary<string, OnboardingStepProgress> SnapshotSteps()
	{
		return new ReadOnlyDictionary<string, OnboardingStepProgress>(
			Definitions.ToDictionary(item => item.StepId, item => GetStepProgress(item.StepId), StringComparer.Ordinal));
	}

	/// <summary>Returns the current progress snapshot for a known step.</summary>
	public OnboardingStepProgress GetStepProgress(string stepId)
	{
		var state = GetRuntimeState(stepId);
		return new OnboardingStepProgress(stepId, state.State, state.CompletionGeneration, state.RepeatCount);
	}

	/// <summary>Resets all first-loop state to a fresh session.</summary>
	public void Reset()
	{
		steps.Clear();
		foreach (var definition in Definitions)
		{
			steps[definition.StepId] = new StepRuntimeState();
		}

		steps[FindHubHudStepId].State = OnboardingStepState.Eligible;
		completionGeneration = 0;
		LastIgnoredEventReason = null;
	}

	/// <summary>Consumes a deterministic step completion signal.</summary>
	public OnboardingStepEventResult CompleteStep(string? stepId)
	{
		if (string.IsNullOrWhiteSpace(stepId))
		{
			return Ignore(null, "empty_step_id");
		}

		if (!steps.TryGetValue(stepId, out var state))
		{
			return Ignore(stepId, "unknown_step_id");
		}

		if (state.State == OnboardingStepState.Completed)
		{
			return Ignore(stepId, "duplicate_completion");
		}

		if (!ArePriorStepsCompleted(stepId))
		{
			return Ignore(stepId, "out_of_order_completion");
		}

		completionGeneration++;
		state.State = OnboardingStepState.Completed;
		state.CompletionGeneration = completionGeneration;
		MarkNextStepEligible(stepId);
		LastIgnoredEventReason = null;
		return new OnboardingStepEventResult(true, stepId, null, completionGeneration);
	}

	/// <summary>Marks an incomplete step as suppressed so it does not emit hints.</summary>
	public bool SuppressStep(string stepId)
	{
		var state = GetRuntimeState(stepId);
		if (state.State == OnboardingStepState.Completed)
		{
			return false;
		}

		state.State = OnboardingStepState.Suppressed;
		return true;
	}

	/// <summary>Records that a hint was shown and marks the step visible.</summary>
	public bool RecordHintShown(string stepId)
	{
		var state = GetRuntimeState(stepId);
		if (state.State is OnboardingStepState.Completed or OnboardingStepState.Suppressed)
		{
			return false;
		}

		state.State = OnboardingStepState.Visible;
		state.RepeatCount++;
		return true;
	}

	/// <summary>Evaluates the current stable first-loop steps and returns the best eligible hint.</summary>
	public OnboardingHintRequest? EvaluateNextHint(
		double secondsSinceEligible = 0.0d,
		int blockerBonus = 0,
		int repeatCountOverride = 0)
	{
		var candidates = Definitions.Select(definition =>
		{
			var state = steps[definition.StepId];
			return new OnboardingHintCandidate(
				definition.StepId,
				definition.HintTextKey,
				definition.HighlightAnchorId,
				definition.BaseStepPriority,
				blockerBonus,
				secondsSinceEligible,
				repeatCountOverride + state.RepeatCount,
				DefaultHintDurationSeconds);
		});
		return SelectHighestScoringHint(candidates);
	}

	/// <summary>Scores provided candidates and returns the highest eligible hint deterministically.</summary>
	public OnboardingHintRequest? SelectHighestScoringHint(IEnumerable<OnboardingHintCandidate> candidates)
	{
		ArgumentNullException.ThrowIfNull(candidates);

		OnboardingHintRequest? best = null;
		var bestOrder = int.MaxValue;
		foreach (var candidate in candidates)
		{
			if (!DefinitionsById.ContainsKey(candidate.StepId)
				|| !steps.TryGetValue(candidate.StepId, out var state)
				|| state.State is OnboardingStepState.Completed or OnboardingStepState.Suppressed
				|| !ArePriorStepsCompletedOrEligible(candidate.StepId))
			{
				continue;
			}

			var score = CalculateHintPriorityScore(
				candidate.BaseStepPriority,
				candidate.BlockerBonus,
				candidate.SecondsSinceEligible,
				candidate.RepeatCount + state.RepeatCount,
				completed: false);
			var order = StepOrder(candidate.StepId);
			if (best is null || score > best.Priority || (score == best.Priority && order < bestOrder))
			{
				best = new OnboardingHintRequest(
					candidate.StepId,
					candidate.HintTextKey,
					candidate.HighlightAnchorId,
					score,
					candidate.DurationSeconds);
				bestOrder = order;
			}
		}

		return best;
	}

	/// <summary>Calculates ADR-0017 hint priority score.</summary>
	public static int CalculateHintPriorityScore(
		int baseStepPriority,
		int blockerBonus = 0,
		double secondsSinceEligible = 0.0d,
		int repeatCount = 0,
		bool completed = false)
	{
		var timeUnseenBonus = Math.Clamp((int)Math.Floor(Math.Max(0.0d, secondsSinceEligible) / 5.0d), 0, MaxTimeUnseenBonus);
		var repeatPenalty = Math.Max(0, repeatCount) * RepeatHintPenalty;
		var completedPenalty = completed ? CompletedStepPenalty : 0;
		return baseStepPriority + Math.Max(0, blockerBonus) + timeUnseenBonus - completedPenalty - repeatPenalty;
	}

	private OnboardingStepEventResult Ignore(string? stepId, string reason)
	{
		LastIgnoredEventReason = reason;
		return new OnboardingStepEventResult(false, stepId, reason, completionGeneration);
	}

	private StepRuntimeState GetRuntimeState(string stepId)
	{
		if (!steps.TryGetValue(stepId, out var state))
		{
			throw new ArgumentException("Unknown onboarding step.", nameof(stepId));
		}

		return state;
	}

	private bool ArePriorStepsCompleted(string stepId)
	{
		foreach (var definition in Definitions)
		{
			if (definition.StepId == stepId)
			{
				return true;
			}

			if (steps[definition.StepId].State != OnboardingStepState.Completed)
			{
				return false;
			}
		}

		return false;
	}

	private bool ArePriorStepsCompletedOrEligible(string stepId)
	{
		foreach (var definition in Definitions)
		{
			if (definition.StepId == stepId)
			{
				return true;
			}

			var state = steps[definition.StepId].State;
			if (state is not OnboardingStepState.Completed and not OnboardingStepState.Eligible and not OnboardingStepState.Visible)
			{
				return false;
			}
		}

		return false;
	}

	private void MarkNextStepEligible(string completedStepId)
	{
		var nextIndex = StepOrder(completedStepId) + 1;
		if (nextIndex >= Definitions.Count)
		{
			return;
		}

		var nextState = steps[Definitions[nextIndex].StepId];
		if (nextState.State == OnboardingStepState.NotStarted)
		{
			nextState.State = OnboardingStepState.Eligible;
		}
	}

	private static int StepOrder(string stepId)
	{
		for (var index = 0; index < Definitions.Count; index++)
		{
			if (Definitions[index].StepId == stepId)
			{
				return index;
			}
		}

		return int.MaxValue;
	}

	private sealed record StepDefinition(
		string StepId,
		string HintTextKey,
		string HighlightAnchorId,
		int BaseStepPriority);

	private sealed class StepRuntimeState
	{
		public OnboardingStepState State { get; set; } = OnboardingStepState.NotStarted;

		public int CompletionGeneration { get; set; }

		public int RepeatCount { get; set; }
	}
}
