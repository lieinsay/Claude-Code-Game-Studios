using System.Collections.ObjectModel;
using CloudWeaverVoyage.Core;

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
/// Active surface observed by onboarding integration.
/// </summary>
public enum OnboardingSurface
{
	/// <summary>No playable surface has been observed yet.</summary>
	Unknown = 0,

	/// <summary>The Hub surface is visible.</summary>
	Hub = 1,

	/// <summary>The Chart surface is visible.</summary>
	Chart = 2,

	/// <summary>The Exploration surface is visible.</summary>
	Exploration = 3,

	/// <summary>A session save/load surface or affordance is visible.</summary>
	Session = 4,
}

/// <summary>
/// Read-only diagnostic for one consumed onboarding integration event.
/// </summary>
public sealed record OnboardingObservedEvent(
	string EventId,
	OnboardingSurface Surface,
	string? StepId,
	bool Accepted,
	string? Reason);

/// <summary>
/// Result of restoring a progress.onboarding snapshot.
/// </summary>
public sealed record OnboardingSnapshotRestoreResult(
	bool Success,
	IReadOnlyList<string> Diagnostics);

/// <summary>
/// Headless first-loop guidance service for #18 onboarding.
/// </summary>
public sealed class OnboardingManager
{
	/// <summary>Stable GDD step: find the Hub HUD.</summary>
	public const string FindHubHudStepId = "find_hub_hud";

	/// <summary>Canonical persistence domain for onboarding progress.</summary>
	public const string ProgressDomainId = "progress.onboarding";

	/// <summary>Current progress.onboarding schema version.</summary>
	public const int SnapshotSchemaVersion = 1;

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
	private readonly List<OnboardingObservedEvent> observedEvents = new();
	private readonly HashSet<string> suppressedHintStepIds = new(StringComparer.Ordinal);
	private readonly List<string> lastRestoreDiagnostics = new();
	private int completionGeneration;
	private string? selectedRouteId;
	private PlayableSliceSnapshot? lastExplorationSnapshot;
	private PlayableSliceSnapshot? lastHubSnapshot;

	/// <summary>Creates a new manager with the first GDD step eligible.</summary>
	public OnboardingManager()
	{
		Reset();
	}

	/// <summary>Stable first-loop step IDs in GDD order.</summary>
	public IReadOnlyList<string> StepIds => Definitions.Select(item => item.StepId).ToArray();

	/// <summary>Most recent ignored completion reason, exposed for QA diagnostics.</summary>
	public string? LastIgnoredEventReason { get; private set; }

	/// <summary>Current active surface observed by integration handlers.</summary>
	public OnboardingSurface ActiveSurface { get; private set; } = OnboardingSurface.Unknown;

	/// <summary>Observed integration events in arrival order.</summary>
	public IReadOnlyList<OnboardingObservedEvent> ObservedEvents => observedEvents.AsReadOnly();

	/// <summary>Step IDs whose stale hints were suppressed by surface changes.</summary>
	public IReadOnlyCollection<string> SuppressedHintStepIds => suppressedHintStepIds.ToArray();

	/// <summary>Diagnostics from the most recent snapshot restore attempt.</summary>
	public IReadOnlyList<string> LastRestoreDiagnostics => lastRestoreDiagnostics.AsReadOnly();

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
		ActiveSurface = OnboardingSurface.Unknown;
		selectedRouteId = null;
		lastExplorationSnapshot = null;
		lastHubSnapshot = null;
		observedEvents.Clear();
		suppressedHintStepIds.Clear();
		lastRestoreDiagnostics.Clear();
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
		suppressedHintStepIds.Add(stepId);
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

	/// <summary>Connects typed UIManager events to first-loop onboarding steps.</summary>
	public void ConnectUiEvents(UIManager uiManager)
	{
		ArgumentNullException.ThrowIfNull(uiManager);
		uiManager.ScreenChanged += (_, next) =>
		{
			if (next == Screen.Hub)
			{
				ObserveHubVisible(inputReachable: true, ownerStateAlreadyMutated: true);
			}
			else if (next is Screen.Chart or Screen.ChartRouteSelected or Screen.ChartDepartureConfirmed)
			{
				ObserveChartActive(ownerStateAlreadyMutated: true);
			}
			else if (next == Screen.Exploration)
			{
				ObserveExplorationActive(ownerStateAlreadyMutated: true);
			}
		};
		uiManager.UIRouteSelected += (routeId, _) => ObserveRouteSelected(routeId, ownerStateAlreadyMutated: true);
		uiManager.UIDepartureConfirmed += (routeId, _) => ObserveDepartureConfirmed(routeId, ownerStateAlreadyMutated: true);
	}

	/// <summary>Connects typed playable-slice adapter events to first-loop onboarding steps.</summary>
	public void ConnectPlayableSliceEvents(PlayableSliceDomainAdapter adapter)
	{
		ArgumentNullException.ThrowIfNull(adapter);
		adapter.ChartOpened += () => ObserveChartActive(ownerStateAlreadyMutated: true);
		adapter.RouteSelected += routeId => ObserveRouteSelected(routeId, ownerStateAlreadyMutated: true);
		adapter.DepartureConfirmed += routeId => ObserveDepartureConfirmed(routeId, ownerStateAlreadyMutated: true);
		adapter.ExplorationPressureChanged += (_, after) => ObserveExplorationPressureChanged(after, ownerStateAlreadyMutated: true);
		adapter.SaveLoadUsed += (_, success) => ObserveSaveLoadAwareness(visibleOrUsed: success, ownerStateAlreadyMutated: true);
		adapter.ReturnedToHub += (before, after) => ObserveReturnedToHub(before, after, ownerStateAlreadyMutated: true);
	}

	/// <summary>Consumes a Hub-visible event after the owning UI surface is reachable.</summary>
	public OnboardingStepEventResult ObserveHubVisible(bool inputReachable, bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Hub;
		if (!ownerStateAlreadyMutated || !inputReachable)
		{
			return RecordIgnoredEvent("hub_visible", OnboardingSurface.Hub, FindHubHudStepId, "hub_not_reachable");
		}

		return RecordEvent("hub_visible", OnboardingSurface.Hub, CompleteStep(FindHubHudStepId));
	}

	/// <summary>Consumes a Chart-active event and suppresses stale Hub hints.</summary>
	public OnboardingStepEventResult ObserveChartActive(bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Chart;
		suppressedHintStepIds.Add(FindHubHudStepId);
		if (!ownerStateAlreadyMutated)
		{
			return RecordIgnoredEvent("chart_active", OnboardingSurface.Chart, OpenChartStepId, "owner_state_not_mutated");
		}

		return RecordEvent("chart_active", OnboardingSurface.Chart, CompleteStep(OpenChartStepId));
	}

	/// <summary>Consumes a route-selected event after Chart state has mutated.</summary>
	public OnboardingStepEventResult ObserveRouteSelected(string routeId, bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Chart;
		if (!ownerStateAlreadyMutated || string.IsNullOrWhiteSpace(routeId))
		{
			return RecordIgnoredEvent("route_selected", OnboardingSurface.Chart, SelectRouteStepId, "route_not_committed");
		}

		selectedRouteId = routeId;
		return RecordEvent("route_selected", OnboardingSurface.Chart, CompleteStep(SelectRouteStepId));
	}

	/// <summary>Consumes a departure-confirmed event after Chart/Hub state has mutated.</summary>
	public OnboardingStepEventResult ObserveDepartureConfirmed(string routeId, bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Exploration;
		if (!ownerStateAlreadyMutated
			|| string.IsNullOrWhiteSpace(routeId)
			|| !string.Equals(selectedRouteId, routeId, StringComparison.Ordinal))
		{
			return RecordIgnoredEvent("departure_confirmed", OnboardingSurface.Exploration, DepartRouteStepId, "departure_not_committed");
		}

		return RecordEvent("departure_confirmed", OnboardingSurface.Exploration, CompleteStep(DepartRouteStepId));
	}

	/// <summary>Consumes an Exploration-active event without completing pressure by itself.</summary>
	public void ObserveExplorationActive(bool ownerStateAlreadyMutated)
	{
		if (ownerStateAlreadyMutated)
		{
			ActiveSurface = OnboardingSurface.Exploration;
			observedEvents.Add(new OnboardingObservedEvent("exploration_active", ActiveSurface, null, Accepted: true, null));
		}
	}

	/// <summary>Registers progress.onboarding with the canonical persistence pipeline.</summary>
	public void RegisterPersistence(Persistence persistence)
	{
		ArgumentNullException.ThrowIfNull(persistence);
		persistence.RegisterDomainSerializer(ProgressDomainId, BuildSnapshotPackage);
		persistence.RegisterDomainDeserializer(ProgressDomainId, package => RestoreFromSnapshotPackage(package));
	}

	/// <summary>Builds a pure-data progress.onboarding snapshot package.</summary>
	public SnapshotPackage BuildSnapshotPackage()
	{
		var completed = Definitions
			.Where(definition => steps[definition.StepId].State == OnboardingStepState.Completed)
			.Select(definition => definition.StepId)
			.ToArray();
		var suppressed = Definitions
			.Where(definition => steps[definition.StepId].State == OnboardingStepState.Suppressed)
			.Select(definition => definition.StepId)
			.ToArray();

		var package = new SnapshotPackage
		{
			DomainId = ProgressDomainId,
			SnapshotSchemaVersion = SnapshotSchemaVersion,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["onboarding-first-loop"] = "2026-05-22";
		foreach (var stepId in completed.Concat(suppressed).Distinct(StringComparer.Ordinal))
		{
			package.StableIdRefs.Add(stepId);
		}

		package.Payload["schema_version"] = SnapshotSchemaVersion;
		package.Payload["completed_step_ids"] = completed;
		package.Payload["suppressed_step_ids"] = suppressed;
		package.Payload["first_loop_complete"] = IsFirstLoopComplete;
		package.Payload["completion_generation"] = completionGeneration;
		return package;
	}

	/// <summary>Restores progress.onboarding from a snapshot package without throwing on malformed data.</summary>
	public OnboardingSnapshotRestoreResult RestoreFromSnapshotPackage(SnapshotPackage package)
	{
		ArgumentNullException.ThrowIfNull(package);
		lastRestoreDiagnostics.Clear();
		if (!string.Equals(package.DomainId, ProgressDomainId, StringComparison.Ordinal))
		{
			lastRestoreDiagnostics.Add("unexpected_domain_id");
		}

		var payloadSchema = ReadInt(package.Payload, "schema_version", package.SnapshotSchemaVersion);
		if (package.SnapshotSchemaVersion != SnapshotSchemaVersion || payloadSchema != SnapshotSchemaVersion)
		{
			lastRestoreDiagnostics.Add("unsupported_schema_version");
		}

		var completed = ReadStepIdSet(package.Payload, "completed_step_ids", lastRestoreDiagnostics);
		var suppressed = ReadStepIdSet(package.Payload, "suppressed_step_ids", lastRestoreDiagnostics);

		RestoreKnownStepSets(completed, suppressed);
		return new OnboardingSnapshotRestoreResult(lastRestoreDiagnostics.Count == 0, lastRestoreDiagnostics.ToArray());
	}

	/// <summary>Consumes visible resource/threat/hull pressure after the adapter changed state.</summary>
	public OnboardingStepEventResult ObserveExplorationPressureChanged(
		PlayableSliceSnapshot snapshot,
		bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Exploration;
		if (!ownerStateAlreadyMutated || !PressureChanged(snapshot))
		{
			lastExplorationSnapshot = snapshot;
			return RecordIgnoredEvent(
				"exploration_pressure_changed",
				OnboardingSurface.Exploration,
				AdvancePressureStepId,
				"pressure_unchanged");
		}

		lastExplorationSnapshot = snapshot;
		return RecordEvent("exploration_pressure_changed", OnboardingSurface.Exploration, CompleteStep(AdvancePressureStepId));
	}

	/// <summary>Consumes save/load visibility or successful use as onboarding awareness.</summary>
	public OnboardingStepEventResult ObserveSaveLoadAwareness(bool visibleOrUsed, bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Session;
		if (!ownerStateAlreadyMutated || !visibleOrUsed)
		{
			return RecordIgnoredEvent("save_load_awareness", OnboardingSurface.Session, NoticeSaveLoadStepId, "save_load_not_visible");
		}

		return RecordEvent("save_load_awareness", OnboardingSurface.Session, CompleteStep(NoticeSaveLoadStepId));
	}

	/// <summary>Consumes return-Hub and summary-change facts after Hub/domain state has mutated.</summary>
	public IReadOnlyList<OnboardingStepEventResult> ObserveReturnedToHub(
		PlayableSliceSnapshot before,
		PlayableSliceSnapshot after,
		bool ownerStateAlreadyMutated)
	{
		ActiveSurface = OnboardingSurface.Hub;
		var results = new List<OnboardingStepEventResult>();
		if (!ownerStateAlreadyMutated || !string.Equals(after.HubDockingState, "Landed", StringComparison.Ordinal))
		{
			results.Add(RecordIgnoredEvent("returned_to_hub", OnboardingSurface.Hub, ReturnHubStepId, "hub_not_landed"));
			return results;
		}

		results.Add(RecordEvent("returned_to_hub", OnboardingSurface.Hub, CompleteStep(ReturnHubStepId)));
		if (SummaryChanged(before, after))
		{
			results.Add(RecordEvent(
				"hub_summary_changed",
				OnboardingSurface.Hub,
				CompleteStep(NoticeSummaryChangeStepId)));
		}
		else
		{
			results.Add(RecordIgnoredEvent(
				"hub_summary_changed",
				OnboardingSurface.Hub,
				NoticeSummaryChangeStepId,
				"summary_unchanged"));
		}

		lastHubSnapshot = after;
		return results;
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

	private OnboardingStepEventResult RecordEvent(
		string eventId,
		OnboardingSurface surface,
		OnboardingStepEventResult result)
	{
		observedEvents.Add(new OnboardingObservedEvent(eventId, surface, result.StepId, result.Accepted, result.IgnoredReason));
		return result;
	}

	private OnboardingStepEventResult RecordIgnoredEvent(
		string eventId,
		OnboardingSurface surface,
		string stepId,
		string reason)
	{
		var result = Ignore(stepId, reason);
		observedEvents.Add(new OnboardingObservedEvent(eventId, surface, stepId, Accepted: false, reason));
		return result;
	}

	private bool PressureChanged(PlayableSliceSnapshot snapshot)
	{
		if (ActiveSurface != OnboardingSurface.Exploration)
		{
			return false;
		}

		if (lastExplorationSnapshot is null)
		{
			return snapshot.ExplorationStep > 0
				|| snapshot.CargoUsed > 0
				|| snapshot.RewardCarried > 0
				|| snapshot.HullIntegrity < 100
				|| !string.Equals(snapshot.ThreatText, "暂无遭遇", StringComparison.Ordinal);
		}

		return snapshot.ExplorationStep != lastExplorationSnapshot.ExplorationStep
			|| snapshot.CargoUsed != lastExplorationSnapshot.CargoUsed
			|| snapshot.RewardCarried != lastExplorationSnapshot.RewardCarried
			|| snapshot.HullIntegrity != lastExplorationSnapshot.HullIntegrity
			|| !string.Equals(snapshot.ThreatText, lastExplorationSnapshot.ThreatText, StringComparison.Ordinal);
	}

	private static bool SummaryChanged(PlayableSliceSnapshot before, PlayableSliceSnapshot after)
	{
		return before.RewardInStorage != after.RewardInStorage
			|| before.RewardCarried != after.RewardCarried
			|| before.CargoUsed != after.CargoUsed
			|| before.HullIntegrity != after.HullIntegrity
			|| !string.Equals(before.StorageText, after.StorageText, StringComparison.Ordinal)
			|| !string.Equals(before.HubDockingState, after.HubDockingState, StringComparison.Ordinal);
	}

	private void RestoreKnownStepSets(IReadOnlySet<string> completed, IReadOnlySet<string> suppressed)
	{
		steps.Clear();
		suppressedHintStepIds.Clear();
		foreach (var definition in Definitions)
		{
			steps[definition.StepId] = new StepRuntimeState();
		}

		completionGeneration = 0;
		foreach (var definition in Definitions)
		{
			if (!completed.Contains(definition.StepId))
			{
				continue;
			}

			completionGeneration++;
			steps[definition.StepId].State = OnboardingStepState.Completed;
			steps[definition.StepId].CompletionGeneration = completionGeneration;
		}

		foreach (var definition in Definitions)
		{
			if (completed.Contains(definition.StepId) || !suppressed.Contains(definition.StepId))
			{
				continue;
			}

			steps[definition.StepId].State = OnboardingStepState.Suppressed;
			suppressedHintStepIds.Add(definition.StepId);
		}

		if (!IsFirstLoopComplete)
		{
			foreach (var definition in Definitions)
			{
				var state = steps[definition.StepId];
				if (state.State == OnboardingStepState.Completed)
				{
					continue;
				}

				if (state.State == OnboardingStepState.NotStarted)
				{
					state.State = OnboardingStepState.Eligible;
				}

				break;
			}
		}

		LastIgnoredEventReason = null;
		ActiveSurface = OnboardingSurface.Unknown;
		observedEvents.Clear();
		selectedRouteId = null;
		lastExplorationSnapshot = null;
		lastHubSnapshot = null;
	}

	private static IReadOnlySet<string> ReadStepIdSet(
		IReadOnlyDictionary<string, object?> payload,
		string key,
		ICollection<string> diagnostics)
	{
		var result = new HashSet<string>(StringComparer.Ordinal);
		if (!payload.TryGetValue(key, out var value) || value is null)
		{
			diagnostics.Add($"missing_{key}");
			return result;
		}

		if (value is string)
		{
			diagnostics.Add($"invalid_{key}_type");
			return result;
		}

		if (value is not System.Collections.IEnumerable items)
		{
			diagnostics.Add($"invalid_{key}_type");
			return result;
		}

		foreach (var item in items)
		{
			var stepId = Convert.ToString(item);
			if (string.IsNullOrWhiteSpace(stepId))
			{
				diagnostics.Add($"invalid_{key}_entry");
				continue;
			}

			if (!DefinitionsById.ContainsKey(stepId))
			{
				diagnostics.Add($"unknown_step_id:{stepId}");
				continue;
			}

			result.Add(stepId);
		}

		return result;
	}

	private static int ReadInt(IReadOnlyDictionary<string, object?> payload, string key, int fallback)
	{
		if (!payload.TryGetValue(key, out var value) || value is null)
		{
			return fallback;
		}

		return value switch
		{
			int intValue => intValue,
			long longValue => checked((int)longValue),
			double doubleValue => checked((int)doubleValue),
			float floatValue => checked((int)floatValue),
			string text when int.TryParse(text, out var parsed) => parsed,
			_ => fallback,
		};
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
