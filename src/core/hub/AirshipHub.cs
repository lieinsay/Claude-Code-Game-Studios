using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Stable docking states owned by the airship hub.
/// </summary>
public enum HubDockingState
{
	Landed = 0,
	DepartureLocked = 1,
	InTransit = 2,
	Arrival = 3,
}

/// <summary>
/// Spawn reason used to derive the safe player position in the hub.
/// </summary>
public enum HubSpawnReason
{
	FirstLoad = 0,
	ReturnFromVoyage = 1,
	SaveLoad = 2,
}

/// <summary>
/// Mirrored visible module slot states from the module system.
/// </summary>
public enum HubModuleSlotState
{
	Empty = 0,
	Installed = 1,
	Damaged = 2,
	Unchecked = 3,
}

/// <summary>
/// Runtime state for one hub station. Busy is transient and is never persisted.
/// </summary>
public enum HubStationState
{
	Ready = 0,
	Busy = 1,
	Disabled = 2,
}

/// <summary>
/// High-level departure mode initiated from a hub station.
/// </summary>
public enum HubDepartureMode
{
	Chart = 0,
	Direct = 1,
}

/// <summary>
/// Data source hooks used by HubManager to query external domain systems.
/// The hub keeps these as read-only delegates so it does not own domain rules.
/// </summary>
public sealed class HubDomainQueries
{
	/// <summary>Known route count for chart note traces.</summary>
	public Func<int> KnownRouteCount { get; init; } = () => 0;

	/// <summary>Whether the route knowledge ability is unlocked.</summary>
	public Func<bool> RouteKnowledgeUnlocked { get; init; } = () => false;

	/// <summary>Route risk summary for chart departure confirmation.</summary>
	public Func<string> RouteRiskSummary { get; init; } = () => "未知";

	/// <summary>Storage used volume.</summary>
	public Func<int> StorageUsedVolume { get; init; } = () => 0;

	/// <summary>Storage total capacity.</summary>
	public Func<int> StorageTotalCapacity { get; init; } = () => 1000;

	/// <summary>Cargo bay used volume.</summary>
	public Func<int> CargoUsedVolume { get; init; } = () => 0;

	/// <summary>Cargo bay total capacity.</summary>
	public Func<int> CargoTotalCapacity { get; init; } = () => 0;

	/// <summary>Whether a partner is currently in the active crew.</summary>
	public Func<bool> PartnerInCrew { get; init; } = () => false;

	/// <summary>Partner nest tier from the partner system.</summary>
	public Func<int> PartnerNestState { get; init; } = () => 0;

	/// <summary>Completed world repair count.</summary>
	public Func<int> CompletedRepairCount { get; init; } = () => 0;

	/// <summary>Whether a repair completed since the last departure.</summary>
	public Func<bool> RepairSinceLastDeparture { get; init; } = () => false;

	/// <summary>Whether the chart system accepts the current chart departure request.</summary>
	public Func<HubDepartureContext, HubDepartureRequestResult> ChartDepartureRequest { get; init; } =
		_ => new HubDepartureRequestResult(true, string.Empty);

	/// <summary>Whether a module slot ID is still valid according to content data.</summary>
	public Func<string, bool> IsValidModuleSlotId { get; init; } = slotId =>
		slotId is HubIds.CargoModule or HubIds.ScoutModule;
}

/// <summary>
/// Result returned by an external departure receiver.
/// </summary>
public sealed record HubDepartureRequestResult(bool Accepted, string ReasonCode);

/// <summary>
/// Typed context passed to downstream voyage systems after departure is confirmed.
/// </summary>
public sealed record HubDepartureContext(
	string DepartureMode,
	bool KnownRoute,
	string DockedLocationId,
	string LastDepartureRoute,
	double EncounterRateBonus,
	double ModuleEfficiency);

/// <summary>
/// One row in the departure confirmation checklist.
/// </summary>
public sealed record HubChecklistItem(string StationId, string Label, bool Visited, string Warning);

/// <summary>
/// Data model used by UI to render the departure confirmation dialog.
/// </summary>
public sealed record HubDepartureConfirmation(
	string Mode,
	IReadOnlyList<HubChecklistItem> Checklist,
	string RouteRisk,
	string CargoCapacity,
	string ModuleSummary,
	bool ConfirmEnabled);

/// <summary>
/// Serializable trace anchor state.
/// </summary>
public sealed record HubTraceAnchor(
	string TraceId,
	int CurrentTier,
	int MaxTier,
	string DisplayName,
	string DataSource);

/// <summary>
/// Immutable spatial room definition for the hub layout.
/// </summary>
public sealed record HubRoomDefinition(
	string RoomId,
	bool BaseExists,
	string? RequiredModule,
	string Layer,
	string CollisionGroup);

/// <summary>
/// Summary of the hub layout used by tests and Godot scene wrappers.
/// </summary>
public sealed record HubLayoutSummary(
	IReadOnlyList<HubRoomDefinition> Rooms,
	bool HasCentralLadder,
	bool HasWalkableBounds,
	int LayerCount);

/// <summary>
/// A physical hub station registered through the existing InteractionRegistry.
/// </summary>
public sealed class HubStation : Interactable
{
	private readonly HubManager manager;
	private readonly Func<bool> conditions;
	private readonly Func<string> hintBuilder;
	private readonly Func<UseResult> useHandler;
	private bool pendingDisable;

	/// <summary>Creates a station bound to a HubManager and domain query hooks.</summary>
	public HubStation(
		HubManager manager,
		string stableId,
		WorldVector2 anchorPosition,
		string interactionType,
		string label,
		Func<bool>? conditions = null,
		Func<string>? hintBuilder = null,
		Func<UseResult>? useHandler = null,
		double anchorRadius = 0.64,
		double priority = 0.5)
		: base(stableId, anchorPosition, anchorRadius, priority, interactionType, label)
	{
		this.manager = manager;
		Label = label;
		this.conditions = conditions ?? (() => true);
		this.hintBuilder = hintBuilder ?? (() => label);
		this.useHandler = useHandler ?? (() => UseResult.Accepted);
	}

	/// <summary>Localized station label for UI and diagnostics.</summary>
	public string Label { get; }

	/// <summary>Current station state.</summary>
	public HubStationState State { get; private set; } = HubStationState.Ready;

	/// <summary>Whether the station is usable at this moment.</summary>
	public override bool IsEnabled =>
		manager.DockingState == HubDockingState.Landed
		&& State == HubStationState.Ready
		&& conditions();

	/// <summary>Whether the station currently owns a domain use lock.</summary>
	public override bool IsBusy => State == HubStationState.Busy;

	/// <summary>Returns a station display hint with current domain summary.</summary>
	public string GetDisplayHint()
	{
		return hintBuilder();
	}

	/// <summary>Requests station use through HubManager routing.</summary>
	public override UseResult HandleUse(string playerId)
	{
		if (!IsEnabled)
		{
			return UseResult.Rejected;
		}

		State = HubStationState.Busy;
		manager.MarkStationVisited(InteractionId);
		manager.NotifyStationActivated(InteractionId, InteractionType);
		var result = useHandler();
		if (result == UseResult.Rejected)
		{
			Release();
		}

		return result == UseResult.Busy ? UseResult.Accepted : result;
	}

	/// <summary>Releases a station use lock, applying any pending disabled state.</summary>
	public void Release()
	{
		if (pendingDisable || !conditions())
		{
			State = HubStationState.Disabled;
			pendingDisable = false;
		}
		else
		{
			State = HubStationState.Ready;
		}

		manager.NotifyStationReleased(InteractionId);
	}

	/// <summary>Disables the station now, or after the current use lock releases.</summary>
	public void Disable()
	{
		if (State == HubStationState.Busy)
		{
			pendingDisable = true;
			return;
		}

		State = HubStationState.Disabled;
	}

	/// <summary>Enables the station if its conditions are currently satisfied.</summary>
	public void Enable()
	{
		if (conditions())
		{
			State = HubStationState.Ready;
			pendingDisable = false;
		}
	}

	/// <summary>Derives ready/disabled after load; busy is not restored.</summary>
	public void DeriveStateAfterLoad()
	{
		State = conditions() ? HubStationState.Ready : HubStationState.Disabled;
		pendingDisable = false;
	}
}

/// <summary>
/// Core Airship Hub manager for C# desktop implementation.
/// Godot node scripts should wrap this class instead of duplicating logic.
/// </summary>
public sealed class HubManager
{
	private const double DEFAULT_DEPARTURE_LOCK_SECONDS = 2.0;
	private const double MIN_VALID_LOCK_SECONDS = 1.0;
	private const double MAX_VALID_LOCK_SECONDS = 5.0;
	private const double MIN_RUNTIME_LOCK_SECONDS = 1.5;
	private const double MAX_RUNTIME_LOCK_SECONDS = 3.0;

	private readonly Dictionary<string, HubStation> stations = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HubModuleSlotState> moduleSlotState = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HubTraceAnchor> traceAnchors = new(StringComparer.Ordinal);
	private readonly HashSet<string> visitedStations = new(StringComparer.Ordinal);
	private readonly List<string> warnings = [];
	private readonly List<string> errors = [];
	private readonly HubDomainQueries queries;
	private double lockTimer;
	private double watchdogTimer;
	private bool departureReceiverCalled;

	/// <summary>Creates the hub manager and registers all MVP stations.</summary>
	public HubManager(
		InteractionRegistry? interactionRegistry = null,
		PlayerMovementController? movement = null,
		HubDomainQueries? queries = null,
		double departureLockDurationSeconds = DEFAULT_DEPARTURE_LOCK_SECONDS)
	{
		InteractionRegistry = interactionRegistry ?? new InteractionRegistry();
		Movement = movement ?? new PlayerMovementController();
		this.queries = queries ?? new HubDomainQueries();
		DepartureLockDurationSeconds = NormalizeDepartureLockDuration(departureLockDurationSeconds);
		InitializeDefaultState();
	}

	/// <summary>Raised after docking state has changed.</summary>
	public event Action<HubDockingState, HubDockingState>? DockingStateChanged;

	/// <summary>Raised after a station starts a use interaction.</summary>
	public event Action<string, string>? StationActivated;

	/// <summary>Raised after a station releases its use interaction.</summary>
	public event Action<string>? StationReleased;

	/// <summary>Raised after the hub enters in_transit from a confirmed departure.</summary>
	public event Action<string, bool, string>? DepartureInitiated;

	/// <summary>Raised after a departure request is rejected.</summary>
	public event Action<string, string>? DepartureRejected;

	/// <summary>Raised after arrival completes and the hub is landed.</summary>
	public event Action<string>? ArrivalCompleted;

	/// <summary>Raised after a module slot mirror changes.</summary>
	public event Action<string, int, int>? ModuleSlotChanged;

	/// <summary>Raised after a trace anchor tier changes.</summary>
	public event Action<string, int, int>? TraceAnchorChanged;

	/// <summary>Existing movement/interaction registry from Epic #4.</summary>
	public InteractionRegistry InteractionRegistry { get; }

	/// <summary>Existing movement controller; Hub roots it during transition locks.</summary>
	public PlayerMovementController Movement { get; }

	/// <summary>Current hub docking state.</summary>
	public HubDockingState DockingState { get; private set; } = HubDockingState.Landed;

	/// <summary>Validated departure lock duration used by Story 001/004.</summary>
	public double DepartureLockDurationSeconds { get; private set; }

	/// <summary>Current spawn reason.</summary>
	public HubSpawnReason SpawnReason { get; private set; } = HubSpawnReason.FirstLoad;

	/// <summary>Whether deterministic ticks auto-complete the lock when its timer expires.</summary>
	public bool AutoCompleteDepartureLock { get; set; } = true;

	/// <summary>Current player spawn position.</summary>
	public WorldVector2 CurrentSpawnPosition { get; private set; } = HubIds.HelmSpawn;

	/// <summary>Last rejection reason shown to UI.</summary>
	public string LastRejectionReason { get; private set; } = string.Empty;

	/// <summary>Last departure mode stored for persistence and downstream context.</summary>
	public string LastDepartureMode { get; private set; } = string.Empty;

	/// <summary>Last departure route stored for persistence and downstream context.</summary>
	public string LastDepartureRoute { get; private set; } = string.Empty;

	/// <summary>Last departure context handed to downstream voyage systems.</summary>
	public HubDepartureContext LastDepartureContext { get; private set; } =
		new(string.Empty, false, string.Empty, string.Empty, 0, 1);

	/// <summary>Pre-departure state summary used for R5 continuity checks.</summary>
	public Dictionary<string, object?> DepartureSnapshot { get; private set; } =
		new(StringComparer.Ordinal);

	/// <summary>Non-fatal warnings collected for tests and UI diagnostics.</summary>
	public IReadOnlyList<string> Warnings => warnings;

	/// <summary>Error logs collected for tests and UI diagnostics.</summary>
	public IReadOnlyList<string> Errors => errors;

	/// <summary>All registered stations in stable ID order.</summary>
	public IReadOnlyList<HubStation> Stations => stations.Values.OrderBy(s => s.InteractionId, StringComparer.Ordinal).ToList();

	/// <summary>Current module slot state mirror.</summary>
	public IReadOnlyDictionary<string, HubModuleSlotState> ModuleSlotState => moduleSlotState;

	/// <summary>Current trace anchor mirror.</summary>
	public IReadOnlyDictionary<string, HubTraceAnchor> TraceAnchors => traceAnchors;

	/// <summary>Hub spatial layout used by the scene wrapper.</summary>
	public HubLayoutSummary Layout { get; private set; } = BuildDefaultLayout();

	/// <summary>Returns a registered station by stable ID.</summary>
	public HubStation GetStation(string stableId)
	{
		return stations[stableId];
	}

	/// <summary>Registers one station with the hub and InteractionRegistry.</summary>
	public void RegisterStation(HubStation station)
	{
		stations[station.InteractionId] = station;
		InteractionRegistry.Register(station);
	}

	/// <summary>Evaluates room existence from base existence and mirrored module state.</summary>
	public bool RoomExists(string roomId)
	{
		var room = Layout.Rooms.FirstOrDefault(r => r.RoomId == roomId);
		if (room is null)
		{
			return true;
		}

		if (room.BaseExists)
		{
			return true;
		}

		return room.RequiredModule is not null && IsModuleInstalled(room.RequiredModule);
	}

	/// <summary>Returns the module required by a room, or null for base rooms.</summary>
	public string? GetRoomRequiredModule(string roomId)
	{
		return Layout.Rooms.First(r => r.RoomId == roomId).RequiredModule;
	}

	/// <summary>Synchronizes a module slot mirror from the module system.</summary>
	public void SyncModuleSlotState(string slotId, HubModuleSlotState state)
	{
		moduleSlotState.TryGetValue(slotId, out var oldState);
		moduleSlotState[slotId] = state;

		if (slotId == HubIds.CargoModule)
		{
			if (!IsModuleInstalled(oldState) && IsModuleInstalled(state))
			{
				GetStation(HubIds.CargoBay).Enable();
			}
			else if (IsModuleInstalled(oldState) && !IsModuleInstalled(state))
			{
				GetStation(HubIds.CargoBay).Disable();
			}
		}

		ModuleSlotChanged?.Invoke(slotId, (int)oldState, (int)state);
	}

	/// <summary>Returns whether a module can be safely unequipped.</summary>
	public bool CanUnequipModule(string slotId)
	{
		return slotId != HubIds.CargoModule || queries.CargoUsedVolume() == 0;
	}

	/// <summary>Returns the localized block reason for an unequip attempt.</summary>
	public string GetUnequipBlockReason(string slotId)
	{
		return CanUnequipModule(slotId) ? string.Empty : "请先清空货舱";
	}

	/// <summary>Builds confirmation data for Mode A or B without blocking confirmation.</summary>
	public HubDepartureConfirmation BuildDepartureConfirmation(HubDepartureMode mode)
	{
		var modeText = mode == HubDepartureMode.Direct ? "direct" : "chart";
		var routeRisk = mode == HubDepartureMode.Direct
			? "未知 — 自主飞行"
			: visitedStations.Contains(HubIds.IntelDesk) ? queries.RouteRiskSummary() : "未知";
		return new HubDepartureConfirmation(
			modeText,
			BuildChecklist(),
			routeRisk,
			BuildCargoCapacityText(),
			BuildModuleSummary(),
			ConfirmEnabled: true);
	}

	/// <summary>Begins a departure lock after UI confirmation.</summary>
	public bool BeginDeparture(HubDepartureMode mode, string routeId = "")
	{
		if (DockingState != HubDockingState.Landed)
		{
			return false;
		}

		LastDepartureMode = mode == HubDepartureMode.Direct ? "direct" : "chart";
		LastDepartureRoute = routeId;
		CaptureDepartureSnapshot();
		LastDepartureContext = BuildDepartureContext(mode, routeId);
		departureReceiverCalled = false;
		return TransitionDocking(HubDockingState.DepartureLocked);
	}

	/// <summary>Advances departure lock and watchdog timers deterministically.</summary>
	public void AdvanceTime(double deltaSeconds)
	{
		if (deltaSeconds < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta must be non-negative.");
		}

		if (DockingState != HubDockingState.DepartureLocked)
		{
			return;
		}

		lockTimer -= deltaSeconds;
		watchdogTimer -= deltaSeconds;

		if (AutoCompleteDepartureLock && lockTimer <= 0 && !departureReceiverCalled)
		{
			CompleteDepartureLock();
		}

		if (DockingState == HubDockingState.DepartureLocked && watchdogTimer <= 0)
		{
			errors.Add("departure_lock_watchdog_forced_landed");
			TransitionDocking(HubDockingState.Landed);
		}
	}

	/// <summary>Completes the departure lock and calls the mode receiver.</summary>
	public bool CompleteDepartureLock()
	{
		if (DockingState != HubDockingState.DepartureLocked)
		{
			return false;
		}

		departureReceiverCalled = true;
		if (LastDepartureMode == "chart")
		{
			var chartResult = queries.ChartDepartureRequest(LastDepartureContext);
			if (!chartResult.Accepted)
			{
				RejectDeparture(chartResult.ReasonCode);
				return false;
			}
		}

		TransitionDocking(HubDockingState.InTransit);
		DepartureInitiated?.Invoke(LastDepartureMode, LastDepartureContext.KnownRoute, LastDepartureRoute);
		return true;
	}

	/// <summary>Rejects a pending departure and restores landed movement.</summary>
	public void RejectDeparture(string reasonCode)
	{
		LastRejectionReason = string.IsNullOrWhiteSpace(reasonCode) ? "UNKNOWN" : reasonCode;
		TransitionDocking(HubDockingState.Landed);
		DepartureRejected?.Invoke(LastDepartureMode, LastRejectionReason);
	}

	/// <summary>Triggers the return arrival flow from in_transit.</summary>
	public bool TriggerArrival()
	{
		return DockingState == HubDockingState.InTransit && TransitionDocking(HubDockingState.Arrival);
	}

	/// <summary>Completes the arrival animation and restores landed control.</summary>
	public bool CompleteArrivalAnimation()
	{
		if (DockingState != HubDockingState.Arrival)
		{
			return false;
		}

		var transitioned = TransitionDocking(HubDockingState.Landed);
		ArrivalCompleted?.Invoke(LastDepartureMode);
		return transitioned;
	}

	/// <summary>Derives station states after loading or arrival.</summary>
	public void DeriveAllStationStates()
	{
		foreach (var station in stations.Values)
		{
			station.DeriveStateAfterLoad();
		}
	}

	/// <summary>Refreshes all trace anchors from external domain queries.</summary>
	public void RefreshTraceAnchors()
	{
		SetTraceAnchor(HubIds.TraceChartNotes, DeriveChartNotesTier(), 2, "情报台海图", "IntelManager.known_routes");
		SetTraceAnchor(HubIds.TraceStorageFullness, DeriveStorageFullnessTier(), 2, "货架占用", "ResourcesManager.storage_summary");
		SetTraceAnchor(HubIds.TraceNestAccumulation, DeriveNestTier(), 3, "伙伴巢穴", "PartnerSystem.query_nest_state");
		SetTraceAnchor(HubIds.TraceHullRepairs, DeriveHullRepairsTier(), 2, "船体修补", "WorldRepair.completed_repairs");
	}

	/// <summary>Builds the progress.airship snapshot payload.</summary>
	public Dictionary<string, object?> BuildProgressAirshipSnapshot()
	{
		var stableDocking = DockingState switch
		{
			HubDockingState.DepartureLocked => HubDockingState.Landed,
			HubDockingState.Arrival => HubDockingState.InTransit,
			_ => DockingState,
		};

		return new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["docking_state"] = (int)stableDocking,
			["module_slot_state"] = moduleSlotState.ToDictionary(pair => pair.Key, pair => (object?)(int)pair.Value, StringComparer.Ordinal),
			["trace_anchors"] = BuildTraceAnchorSnapshot(),
			["last_departure_mode"] = LastDepartureMode,
			["last_departure_route"] = LastDepartureRoute,
			["departure_snapshot"] = new Dictionary<string, object?>(DepartureSnapshot, StringComparer.Ordinal),
			["spawn_reason"] = (int)SpawnReason,
		};
	}

	/// <summary>Builds the ADR-0003 SnapshotPackage for progress.airship.</summary>
	public SnapshotPackage BuildSnapshotPackage()
	{
		var package = new SnapshotPackage
		{
			DomainId = "progress.airship",
			SnapshotSchemaVersion = 1,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["airship-hub"] = "2026-05-09";
		package.StableIdRefs.AddRange(stations.Keys.Order(StringComparer.Ordinal));
		foreach (var (key, value) in BuildProgressAirshipSnapshot())
		{
			package.Payload[key] = value;
		}

		return package;
	}

	/// <summary>Restores from a progress.airship snapshot payload.</summary>
	public bool RestoreFromProgressAirship(IReadOnlyDictionary<string, object?> snapshot)
	{
		if (!ValidateSnapshotSchema(snapshot))
		{
			ApplySafeDefaults();
			return false;
		}

		RestoreModuleSlotState(ReadObjectMap(snapshot, "module_slot_state"));
		RestoreTraceAnchors(ReadObjectMap(snapshot, "trace_anchors"));
		LastDepartureMode = ReadString(snapshot, "last_departure_mode");
		LastDepartureRoute = ReadString(snapshot, "last_departure_route");
		DepartureSnapshot = ReadObjectMap(snapshot, "departure_snapshot");
		SpawnReason = (HubSpawnReason)ReadInt(snapshot, "spawn_reason", (int)HubSpawnReason.SaveLoad);

		var savedState = (HubDockingState)ReadInt(snapshot, "docking_state", (int)HubDockingState.Landed);
		if (savedState == HubDockingState.InTransit)
		{
			DockingState = HubDockingState.InTransit;
			TriggerArrival();
		}
		else if (savedState == HubDockingState.Landed)
		{
			DockingState = HubDockingState.Landed;
			Movement.SetRooted(false);
			SpawnPlayer(SpawnReason == HubSpawnReason.ReturnFromVoyage ? HubSpawnReason.ReturnFromVoyage : HubSpawnReason.SaveLoad);
			DeriveAllStationStates();
		}
		else
		{
			warnings.Add($"{savedState.ToString().ToLowerInvariant()} degraded to landed");
			DockingState = HubDockingState.Landed;
			Movement.SetRooted(false);
			SpawnPlayer(HubSpawnReason.SaveLoad);
			DeriveAllStationStates();
		}

		return true;
	}

	/// <summary>Returns true when the snapshot has the required independent fields.</summary>
	public static bool ValidateSnapshotSchema(IReadOnlyDictionary<string, object?> snapshot)
	{
		return snapshot.Count > 0
			&& snapshot.ContainsKey("docking_state")
			&& snapshot.ContainsKey("module_slot_state")
			&& snapshot.ContainsKey("trace_anchors")
			&& snapshot.ContainsKey("spawn_reason")
			&& snapshot["module_slot_state"] is IReadOnlyDictionary<string, object?>;
	}

	/// <summary>Returns true when a scene switch can keep the hub instance resident.</summary>
	public static bool CanKeepHubSceneResident(bool hubInstanceCached, bool targetSceneLoaded)
	{
		return hubInstanceCached && targetSceneLoaded;
	}

	/// <summary>Returns true when a cached return switch meets the 500ms transition budget.</summary>
	public static bool IsReturnTransitionWithinBudget(TimeSpan elapsed)
	{
		return elapsed <= TimeSpan.FromMilliseconds(500);
	}

	/// <summary>Returns whether the suspend flush fits the desktop 20ms budget.</summary>
	public static bool IsSuspendFlushWithinBudget(TimeSpan elapsed)
	{
		return elapsed <= TimeSpan.FromMilliseconds(20);
	}

	internal void MarkStationVisited(string stationId)
	{
		visitedStations.Add(stationId);
	}

	internal void NotifyStationActivated(string stationId, string interactionType)
	{
		StationActivated?.Invoke(stationId, interactionType);
	}

	internal void NotifyStationReleased(string stationId)
	{
		InteractionRegistry.ReleaseUseLock(stationId);
		StationReleased?.Invoke(stationId);
	}

	private void InitializeDefaultState()
	{
		moduleSlotState[HubIds.CargoModule] = HubModuleSlotState.Empty;
		moduleSlotState[HubIds.ScoutModule] = HubModuleSlotState.Empty;
		RegisterDefaultStations();
		RefreshTraceAnchors();
		SpawnPlayer(HubSpawnReason.FirstLoad);
	}

	private void RegisterDefaultStations()
	{
		RegisterStation(new HubStation(this, HubIds.IntelDesk, new WorldVector2(0, 1), "read", "情报台",
			hintBuilder: () => $"情报台 · {queries.KnownRouteCount()} 条航线",
			useHandler: () => UseResult.Busy,
			priority: 0.8));
		RegisterStation(new HubStation(this, HubIds.PartnerPost, new WorldVector2(3, 1), "talk", "伙伴驻点",
			conditions: queries.PartnerInCrew,
			hintBuilder: () => queries.PartnerInCrew() ? "伙伴驻点 · 可交谈" : "伙伴驻点 · 暂无伙伴",
			useHandler: () => UseResult.Busy,
			priority: 0.5));
		RegisterStation(new HubStation(this, HubIds.ModuleSlotA, new WorldVector2(0, -1), "use", "模块接口 A",
			hintBuilder: () => ModuleHint(HubIds.ScoutModule),
			useHandler: () => UseResult.Busy,
			priority: 0.7));
		RegisterStation(new HubStation(this, HubIds.ModuleSlotB, new WorldVector2(1, -1), "use", "模块接口 B",
			hintBuilder: () => ModuleHint(HubIds.CargoModule),
			useHandler: () => UseResult.Busy,
			priority: 0.7));
		RegisterStation(new HubStation(this, HubIds.StorageShelf, new WorldVector2(2, -1), "open", "仓库",
			hintBuilder: () => $"仓库 · {queries.StorageUsedVolume()}/{queries.StorageTotalCapacity()}",
			useHandler: () => UseResult.Busy,
			priority: 0.6));
		RegisterStation(new HubStation(this, HubIds.CargoBay, new WorldVector2(4, -1), "open", "货舱",
			conditions: () => RoomExists(HubIds.CargoHold),
			hintBuilder: () => RoomExists(HubIds.CargoHold)
				? $"货舱 · {queries.CargoUsedVolume()}/{queries.CargoTotalCapacity()}"
				: "货舱 · 未安装",
			useHandler: () => UseResult.Busy,
			priority: 0.6));
		RegisterStation(new HubStation(this, HubIds.Door, new WorldVector2(-1, 1), "use", "舱门",
			hintBuilder: () => "舱门 · 固定航线",
			useHandler: () => UseResult.Accepted,
			priority: 0.9));
		RegisterStation(new HubStation(this, HubIds.Helm, HubIds.HelmSpawn, "use", "舵轮",
			hintBuilder: () => "舵轮 · 自主飞行",
			useHandler: () => UseResult.Accepted,
			priority: 0.95));
		RegisterStation(new HubStation(this, HubIds.RestPoint, new WorldVector2(4, 1), "rest", "休息处",
			hintBuilder: () => "休息处 · 可休息",
			useHandler: () => UseResult.Accepted,
			priority: 0.3));
		RegisterStation(new HubStation(this, HubIds.RepairPoint, new WorldVector2(3, -1), "repair", "船体状态面板",
			hintBuilder: () => $"船体状态 · 修补 {queries.CompletedRepairCount()}",
			useHandler: () => UseResult.Busy,
			priority: 0.7));
	}

	private bool TransitionDocking(HubDockingState newState)
	{
		if (!CanTransition(DockingState, newState))
		{
			warnings.Add($"invalid_docking_transition:{DockingState}->{newState}");
			return false;
		}

		var oldState = DockingState;
		DockingState = newState;

		switch (newState)
		{
			case HubDockingState.Landed:
				Movement.SetRooted(false);
				if (oldState == HubDockingState.Arrival)
				{
					SpawnPlayer(HubSpawnReason.ReturnFromVoyage);
					DeriveAllStationStates();
					RefreshTraceAnchors();
				}

				break;
			case HubDockingState.DepartureLocked:
				Movement.SetRooted(true);
				lockTimer = Math.Clamp(DepartureLockDurationSeconds, MIN_RUNTIME_LOCK_SECONDS, MAX_RUNTIME_LOCK_SECONDS);
				watchdogTimer = DepartureLockDurationSeconds * 3.0;
				CloseHubPanelsRequested = true;
				break;
			case HubDockingState.InTransit:
				break;
			case HubDockingState.Arrival:
				Movement.SetRooted(true);
				break;
			default:
				throw new InvalidOperationException("Unsupported docking state.");
		}

		DockingStateChanged?.Invoke(oldState, newState);
		return true;
	}

	/// <summary>Whether UI panels should close because the hub entered departure_locked.</summary>
	public bool CloseHubPanelsRequested { get; private set; }

	private static bool CanTransition(HubDockingState fromState, HubDockingState toState)
	{
		return fromState switch
		{
			HubDockingState.Landed => toState == HubDockingState.DepartureLocked,
			HubDockingState.DepartureLocked => toState is HubDockingState.InTransit or HubDockingState.Landed,
			HubDockingState.InTransit => toState == HubDockingState.Arrival,
			HubDockingState.Arrival => toState == HubDockingState.Landed,
			_ => false,
		};
	}

	private HubDepartureContext BuildDepartureContext(HubDepartureMode mode, string routeId)
	{
		var penalties = GetDeparturePenalties();
		return new HubDepartureContext(
			mode == HubDepartureMode.Direct ? "direct" : "chart",
			KnownRoute: mode == HubDepartureMode.Chart,
			DockedLocationId: "hub.current_dock",
			LastDepartureRoute: routeId,
			EncounterRateBonus: penalties.EncounterRateBonus,
			ModuleEfficiency: penalties.ModuleEfficiency);
	}

	private (double EncounterRateBonus, double ModuleEfficiency) GetDeparturePenalties()
	{
		var encounter = visitedStations.Contains(HubIds.IntelDesk) ? 0.0 : 0.10;
		var moduleEfficiency = visitedStations.Contains(HubIds.ModuleSlotA)
			&& visitedStations.Contains(HubIds.ModuleSlotB)
			? 1.0
			: 0.95;
		return (encounter, moduleEfficiency);
	}

	private void CaptureDepartureSnapshot()
	{
		DepartureSnapshot = new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["modules"] = moduleSlotState.ToDictionary(pair => pair.Key, pair => (object?)(int)pair.Value, StringComparer.Ordinal),
			["storage_used"] = queries.StorageUsedVolume(),
			["cargo_used"] = queries.CargoUsedVolume(),
			["hull_repair_count"] = queries.CompletedRepairCount(),
		};
	}

	private List<HubChecklistItem> BuildChecklist()
	{
		return
		[
			new(HubIds.IntelDesk, "情报台", visitedStations.Contains(HubIds.IntelDesk), "航线风险未知 + 本次出航遭遇率 +10%"),
			new(HubIds.PartnerPost, "伙伴驻点", visitedStations.Contains(HubIds.PartnerPost), "无侦察简报"),
			new(HubIds.CargoBay, "货舱", visitedStations.Contains(HubIds.CargoBay), "容量显示未确认"),
			new(HubIds.ModuleSlotA, "模块接口 A", visitedStations.Contains(HubIds.ModuleSlotA), "该模块本次运行效率降至 95%"),
			new(HubIds.ModuleSlotB, "模块接口 B", visitedStations.Contains(HubIds.ModuleSlotB), "该模块本次运行效率降至 95%"),
		];
	}

	private string BuildCargoCapacityText()
	{
		if (!RoomExists(HubIds.CargoHold))
		{
			return "无货舱";
		}

		return visitedStations.Contains(HubIds.CargoBay)
			? $"{queries.CargoUsedVolume()}/{queries.CargoTotalCapacity()}"
			: "未确认";
	}

	private string BuildModuleSummary()
	{
		var summaries = new[]
		{
			$"A:{DescribeModuleState(moduleSlotState[HubIds.ScoutModule])}",
			$"B:{DescribeModuleState(moduleSlotState[HubIds.CargoModule])}",
		};
		return string.Join(", ", summaries);
	}

	private string ModuleHint(string moduleId)
	{
		return moduleSlotState[moduleId] switch
		{
			HubModuleSlotState.Empty => "模块接口 · 空槽",
			HubModuleSlotState.Installed => "模块接口 · 正常",
			HubModuleSlotState.Damaged => "模块接口 · 受损",
			HubModuleSlotState.Unchecked => "模块接口 · 未检查",
			_ => "模块接口 · 空槽",
		};
	}

	private static string DescribeModuleState(HubModuleSlotState state)
	{
		return state switch
		{
			HubModuleSlotState.Empty => "未安装",
			HubModuleSlotState.Installed => "正常",
			HubModuleSlotState.Damaged => "有损伤",
			HubModuleSlotState.Unchecked => "未检查",
			_ => "未安装",
		};
	}

	private void SpawnPlayer(HubSpawnReason reason)
	{
		SpawnReason = reason;
		CurrentSpawnPosition = reason == HubSpawnReason.FirstLoad ? HubIds.HelmSpawn : HubIds.DoorSpawn;
		Movement.SetPosition(CurrentSpawnPosition);
	}

	private void ApplySafeDefaults()
	{
		warnings.Add("progress.airship snapshot invalid; safe defaults applied");
		DockingState = HubDockingState.Landed;
		moduleSlotState.Clear();
		moduleSlotState[HubIds.CargoModule] = HubModuleSlotState.Empty;
		moduleSlotState[HubIds.ScoutModule] = HubModuleSlotState.Empty;
		traceAnchors.Clear();
		RefreshTraceAnchors();
		SpawnPlayer(HubSpawnReason.FirstLoad);
		DeriveAllStationStates();
	}

	private int DeriveChartNotesTier()
	{
		if (queries.KnownRouteCount() >= 4 || queries.RouteKnowledgeUnlocked())
		{
			return 2;
		}

		return queries.KnownRouteCount() >= 1 && visitedStations.Contains(HubIds.IntelDesk) ? 1 : 0;
	}

	private int DeriveStorageFullnessTier()
	{
		var capacity = queries.StorageTotalCapacity();
		if (capacity <= 0)
		{
			return 0;
		}

		var ratio = (double)queries.StorageUsedVolume() / capacity;
		return ratio > 0.75 ? 2 : ratio > 0.33 ? 1 : 0;
	}

	private int DeriveNestTier()
	{
		return queries.PartnerInCrew() ? Math.Clamp(queries.PartnerNestState(), 0, 3) : 0;
	}

	private int DeriveHullRepairsTier()
	{
		var repairs = queries.CompletedRepairCount();
		if (repairs >= 3 && queries.RepairSinceLastDeparture())
		{
			return 2;
		}

		return repairs >= 1 ? 1 : 0;
	}

	private void SetTraceAnchor(string traceId, int tier, int maxTier, string displayName, string dataSource)
	{
		traceAnchors.TryGetValue(traceId, out var old);
		var clampedTier = Math.Clamp(tier, 0, maxTier);
		traceAnchors[traceId] = new HubTraceAnchor(traceId, clampedTier, maxTier, displayName, dataSource);
		if (old is not null && old.CurrentTier != clampedTier)
		{
			TraceAnchorChanged?.Invoke(traceId, old.CurrentTier, clampedTier);
		}
	}

	private Dictionary<string, object?> BuildTraceAnchorSnapshot()
	{
		return traceAnchors.ToDictionary(
			pair => pair.Key,
			pair => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["tier"] = pair.Value.CurrentTier,
				["max_tier"] = pair.Value.MaxTier,
			},
			StringComparer.Ordinal);
	}

	private void RestoreTraceAnchors(IReadOnlyDictionary<string, object?> snapshot)
	{
		traceAnchors.Clear();
		foreach (var (traceId, value) in snapshot)
		{
			if (value is not IReadOnlyDictionary<string, object?> anchor)
			{
				continue;
			}

			var maxTier = ReadInt(anchor, "max_tier", traceId == HubIds.TraceNestAccumulation ? 3 : 2);
			SetTraceAnchor(traceId, ReadInt(anchor, "tier"), maxTier, TraceDisplayName(traceId), TraceDataSource(traceId));
		}

		foreach (var required in HubIds.TraceIds)
		{
			if (!traceAnchors.ContainsKey(required))
			{
				SetTraceAnchor(required, 0, required == HubIds.TraceNestAccumulation ? 3 : 2, TraceDisplayName(required), TraceDataSource(required));
			}
		}
	}

	private void RestoreModuleSlotState(IReadOnlyDictionary<string, object?> snapshot)
	{
		moduleSlotState.Clear();
		foreach (var (slotId, value) in snapshot)
		{
			if (!queries.IsValidModuleSlotId(slotId))
			{
				warnings.Add($"stale module slot skipped:{slotId}");
				continue;
			}

			moduleSlotState[slotId] = (HubModuleSlotState)ReadInt(snapshot, slotId);
		}

		moduleSlotState.TryAdd(HubIds.CargoModule, HubModuleSlotState.Empty);
		moduleSlotState.TryAdd(HubIds.ScoutModule, HubModuleSlotState.Empty);
	}

	private bool IsModuleInstalled(string moduleId)
	{
		return moduleSlotState.TryGetValue(moduleId, out var state) && IsModuleInstalled(state);
	}

	private static bool IsModuleInstalled(HubModuleSlotState state)
	{
		return state is HubModuleSlotState.Installed or HubModuleSlotState.Damaged or HubModuleSlotState.Unchecked;
	}

	private static double NormalizeDepartureLockDuration(double value)
	{
		return double.IsNaN(value) || value < MIN_VALID_LOCK_SECONDS || value > MAX_VALID_LOCK_SECONDS
			? DEFAULT_DEPARTURE_LOCK_SECONDS
			: value;
	}

	private static HubLayoutSummary BuildDefaultLayout()
	{
		return new HubLayoutSummary(
			[
				new(HubIds.Cockpit, true, null, "upper", "cockpit_bounds"),
				new(HubIds.LivingQuarters, true, null, "upper", "living_bounds"),
				new(HubIds.EngineeringBay, true, null, "lower", "engineering_bounds"),
				new(HubIds.CargoHold, false, HubIds.CargoModule, "lower", "cargo_bounds"),
			],
			HasCentralLadder: true,
			HasWalkableBounds: true,
			LayerCount: 2);
	}

	private static string TraceDisplayName(string traceId)
	{
		return traceId switch
		{
			HubIds.TraceChartNotes => "情报台海图",
			HubIds.TraceStorageFullness => "货架占用",
			HubIds.TraceNestAccumulation => "伙伴巢穴",
			HubIds.TraceHullRepairs => "船体修补",
			_ => traceId,
		};
	}

	private static string TraceDataSource(string traceId)
	{
		return traceId switch
		{
			HubIds.TraceChartNotes => "IntelManager.known_routes",
			HubIds.TraceStorageFullness => "ResourcesManager.storage_summary",
			HubIds.TraceNestAccumulation => "PartnerSystem.query_nest_state",
			HubIds.TraceHullRepairs => "WorldRepair.completed_repairs",
			_ => string.Empty,
		};
	}

	private static string ReadString(IReadOnlyDictionary<string, object?> data, string key)
	{
		return data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
	}

	private static int ReadInt(IReadOnlyDictionary<string, object?> data, string key, int fallback = 0)
	{
		if (!data.TryGetValue(key, out var value) || value is null)
		{
			return fallback;
		}

		return value switch
		{
			int i => i,
			long l => checked((int)l),
			double d => checked((int)d),
			float f => checked((int)f),
			string s when int.TryParse(s, out var parsed) => parsed,
			_ => fallback,
		};
	}

	private static Dictionary<string, object?> ReadObjectMap(IReadOnlyDictionary<string, object?> data, string key)
	{
		if (!data.TryGetValue(key, out var value) || value is null)
		{
			return new Dictionary<string, object?>(StringComparer.Ordinal);
		}

		return value is IReadOnlyDictionary<string, object?> map
			? new Dictionary<string, object?>(map, StringComparer.Ordinal)
			: new Dictionary<string, object?>(StringComparer.Ordinal);
	}
}

/// <summary>
/// Stable IDs and spawn points for the airship hub.
/// </summary>
public static class HubIds
{
	public const string IntelDesk = "hub.interactable.intel-desk";
	public const string PartnerPost = "hub.interactable.partner-post";
	public const string ModuleSlotA = "hub.interactable.module-slot-a";
	public const string ModuleSlotB = "hub.interactable.module-slot-b";
	public const string StorageShelf = "hub.interactable.storage-shelf";
	public const string CargoBay = "hub.interactable.cargo-bay";
	public const string Door = "hub.interactable.door";
	public const string Helm = "hub.interactable.helm";
	public const string RestPoint = "hub.interactable.rest-point";
	public const string RepairPoint = "hub.interactable.repair-point";

	public const string Cockpit = "cockpit";
	public const string LivingQuarters = "living_quarters";
	public const string EngineeringBay = "engineering_bay";
	public const string CargoHold = "cargo_hold";

	public const string CargoModule = "cargo_module";
	public const string ScoutModule = "scout_module";

	public const string TraceChartNotes = "chart_notes";
	public const string TraceStorageFullness = "storage_fullness";
	public const string TraceNestAccumulation = "nest_accumulation";
	public const string TraceHullRepairs = "hull_repairs";

	public static readonly WorldVector2 HelmSpawn = new(0.25, 1.0);
	public static readonly WorldVector2 DoorSpawn = new(-1.0, 1.0);

	public static readonly string[] StationIds =
	[
		IntelDesk,
		PartnerPost,
		ModuleSlotA,
		ModuleSlotB,
		StorageShelf,
		CargoBay,
		Door,
		Helm,
		RestPoint,
		RepairPoint,
	];

	public static readonly string[] TraceIds =
	[
		TraceChartNotes,
		TraceStorageFullness,
		TraceNestAccumulation,
		TraceHullRepairs,
	];
}
