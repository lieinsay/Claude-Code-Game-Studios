using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #15 Story 004: Hub Event & Intel API Integration ===");
var failed = 0;
var total = 0;

Run("AC-1: feature_ready restores snapshot, subscribes Hub events, then syncs current Hub state", Ac1FeatureReadySequence);
Run("AC-2: LANDED Hub event unfreezes the cat state machine", Ac2LandedUnfreezesCat);
Run("AC-3: player_returned_to_hub emits naming prompt when eligible", Ac3ReturnToHubPromptsNaming);
Run("AC-4: new game queues on_partner_joined and dispatches it once", Ac4NewGameQueuesPartnerJoinedOnce);
Run("AC-5: loaded save does not queue on_partner_joined", Ac5LoadedSaveSkipsPartnerJoined);
Run("AC-6: successful scout_sniff writes correct reveal_rumor parameters", Ac6RevealRumorParameters);
Run("AC-7: reveal_rumor failure preserves local sniff and nest state", Ac7RevealRumorFailureGraceful);
Run("AC-8: missing Intel sink skips writes without crashing", Ac8MissingIntelSkipsWrites);
Run("AC-9: query_partner_present returns true for Hub", Ac9QueryPartnerPresent);
Run("AC-10: named partner name is exposed to Hub", Ac10QueryPartnerNameNamed);
Run("AC-11: unnamed partner name returns empty string", Ac11QueryPartnerNameUnnamed);
Run("AC-12: full nest exposes NEST_FULL tier 3", Ac12QueryNestFull);
Run("AC-13: get_sniffable_items filters inventory by cat_sniff_signature", Ac13SniffableItemsFilter);
Run("AC-14: unavailable Resources inventory degrades to empty list", Ac14ResourcesUnavailable);
Run("AC-15: sync_with_hub_state repairs missed pre-subscription Hub events", Ac15SyncRepairsInitRace);
Run("AC-16: landed Hub plus living-quarters player derives idle living cat state", Ac16SyncUsesCurrentPlayerZone);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 004 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 004 validation passed: {total}/{total} checks passed.");
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

static bool Ac1FeatureReadySequence()
{
	var log = new List<string>();
	var snapshot = new FakeSnapshotStore(log);
	var hub = new FakeHubSignalSource(log) { Current = HubDockingState.Landed };
	var bootstrap = new QueuedBootstrapSequencer(log);
	var partner = NewPartner(
		content: new FakeContentLookup(),
		snapshotStore: snapshot,
		hub: hub,
		bootstrap: bootstrap);

	var isNewGame = partner.OnFeatureReady();

	return isNewGame
		&& snapshot.RequestedDomains.SequenceEqual([PartnerManager.PartnerSnapshotDomainId])
		&& hub.HubStateChangedSubscriptions == 1
		&& hub.PlayerReturnedSubscriptions == 1
		&& hub.PlayerEnteredZoneSubscriptions == 1
		&& partner.CatState == PartnerCatState.SleepingOnIntelStation
		&& log.IndexOf("snapshot:restore") < log.IndexOf("hub:connect:state")
		&& log.IndexOf("hub:connect:zone") < log.IndexOf("hub:is_player_in_zone:living_quarters")
		&& bootstrap.QueuedCount == 1;
}

static bool Ac2LandedUnfreezesCat()
{
	var partner = NewPartner();
	partner.Initialize(HubDockingState.DepartureLocked);
	partner.OnHubStateChanged(HubDockingState.Landed);

	return !partner.IsStateFrozen
		&& partner.IsCatInteractable
		&& partner.IsCatRendered;
}

static bool Ac3ReturnToHubPromptsNaming()
{
	var hub = new FakeHubSignalSource { Current = HubDockingState.Landed };
	var partner = NewPartner(ContentWithItems("item.valid"), hub: hub);
	partner.OnFeatureReady();
	MoveToLivingIdle(partner);
	partner.ScoutSniff("item.valid");
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);

	var prompts = 0;
	partner.NamingPromptTriggered += () => prompts++;
	hub.EmitPlayerReturnedToHub();

	return prompts == 1
		&& SkyCat(partner).NamingState == PartnerNamingState.Prompted;
}

static bool Ac4NewGameQueuesPartnerJoinedOnce()
{
	var intel = new RecordingIntelSink();
	var bootstrap = new QueuedBootstrapSequencer();
	var partner = NewPartner(
		content: new FakeContentLookup(),
		intel: intel,
		bootstrap: bootstrap);

	var isNewGame = partner.OnFeatureReady();
	var queuedBeforeDispatch = bootstrap.QueuedCount;
	bootstrap.DispatchAll();
	bootstrap.DispatchAll();

	return isNewGame
		&& queuedBeforeDispatch == 1
		&& intel.PartnerJoinedCalls.SequenceEqual([PartnerManager.MvpPartnerId]);
}

static bool Ac5LoadedSaveSkipsPartnerJoined()
{
	var snapshot = new FakeSnapshotStore(snapshot: SnapshotWithName("小云"));
	var intel = new RecordingIntelSink();
	var bootstrap = new QueuedBootstrapSequencer();
	var partner = NewPartner(
		content: new FakeContentLookup(),
		intel: intel,
		snapshotStore: snapshot,
		bootstrap: bootstrap);

	var isNewGame = partner.OnFeatureReady();
	bootstrap.DispatchAll();

	return !isNewGame
		&& bootstrap.QueuedCount == 0
		&& intel.PartnerJoinedCalls.Count == 0
		&& partner.QueryPartnerName() == "小云";
}

static bool Ac6RevealRumorParameters()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature(
			revealTarget: "location.kestrel-rock-01",
			hazardHint: "low-visibility",
			confidence: 90,
			patternId: "pattern.bird-flight-direction"));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");
	var call = intel.RevealCalls.SingleOrDefault();

	return result.Success
		&& call is not null
		&& call.RevealTarget == "location.kestrel-rock-01"
		&& call.SourceTag == PartnerManager.MvpPartnerId
		&& call.HazardTags.SequenceEqual(["low-visibility"])
		&& call.Confidence == 66
		&& intel.ReportCalls.SequenceEqual([new ReportObservationCall("pattern.bird-flight-direction", PartnerManager.PartnerSniffSuccessEventId)]);
}

static bool Ac7RevealRumorFailureGraceful()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature(revealTarget: "location.failed", confidence: 90));
	var intel = new RecordingIntelSink { ThrowOnReveal = true };
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");
	var state = SkyCat(partner);

	return result.Success
		&& state.SniffedItems.SequenceEqual(["item.valid"])
		&& state.NestItems.SequenceEqual([0])
		&& partner.Warnings.Any(warning => warning.StartsWith("intel_reveal_rumor_failed:location.failed:", StringComparison.Ordinal))
		&& intel.ReportCalls.Count == 1;
}

static bool Ac8MissingIntelSkipsWrites()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature("location.safe"));
	var partner = new PartnerManager(content, new FakeInventorySource());
	partner.Initialize(HubDockingState.Landed);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");

	return result.Success
		&& SkyCat(partner).SniffedItems.SequenceEqual(["item.valid"])
		&& partner.QueryNestItems().SequenceEqual([0]);
}

static bool Ac9QueryPartnerPresent()
{
	var partner = NewPartner();
	partner.Initialize(HubDockingState.InTransit);

	return partner.QueryPartnerPresent();
}

static bool Ac10QueryPartnerNameNamed()
{
	var partner = PromptedPartner();
	var result = partner.SubmitPartnerName("小云");

	return result.Accepted
		&& partner.QueryPartnerName() == "小云";
}

static bool Ac11QueryPartnerNameUnnamed()
{
	return NewPartner().QueryPartnerName() == string.Empty;
}

static bool Ac12QueryNestFull()
{
	var partner = PartnerWithNestSniffs(PartnerManager.NestCapacity);

	return partner.QueryNestState() == (int)PartnerNestState.Full
		&& partner.QueryNestItems().SequenceEqual([0, 1, 2, 3]);
}

static bool Ac13SniffableItemsFilter()
{
	var content = new FakeContentLookup()
		.Add("item.signed-a", ItemWithSignature("location.a"))
		.Add("item.unsigned", ItemWithoutSignature("item.unsigned"))
		.Add("item.signed-b", ItemWithSignature("location.b"));
	var inventory = new FakeInventorySource("item.signed-a", "item.unsigned", "item.signed-b");
	var partner = NewPartner(content, inventory);

	return partner.GetSniffableItems().SequenceEqual(["item.signed-a", "item.signed-b"]);
}

static bool Ac14ResourcesUnavailable()
{
	var partner = NewPartner(inventory: new FakeInventorySource { ThrowOnRead = true });
	var sniffable = partner.GetSniffableItems();

	return sniffable.Count == 0
		&& partner.Warnings.Any(warning => warning.StartsWith("resources_inventory_unavailable:", StringComparison.Ordinal));
}

static bool Ac15SyncRepairsInitRace()
{
	var hub = new FakeHubSignalSource { Current = HubDockingState.Landed };
	hub.EmitHubStateChanged(HubDockingState.Landed);
	var partner = NewPartner(hub: hub);

	partner.OnFeatureReady();

	return partner.CatState == PartnerCatState.SleepingOnIntelStation
		&& !partner.IsStateFrozen
		&& partner.IsCatInteractable;
}

static bool Ac16SyncUsesCurrentPlayerZone()
{
	var hub = new FakeHubSignalSource { Current = HubDockingState.Landed };
	hub.SetPlayerInZone(HubIds.LivingQuarters, true);
	var partner = NewPartner(hub: hub);

	partner.SyncWithHubState(HubDockingState.Landed);

	return partner.CatState == PartnerCatState.IdleLivingQuarters
		&& !partner.IsStateFrozen;
}

static PartnerManager NewPartner(
	FakeContentLookup? content = null,
	FakeInventorySource? inventory = null,
	RecordingIntelSink? intel = null,
	FakeSnapshotStore? snapshotStore = null,
	FakeHubSignalSource? hub = null,
	QueuedBootstrapSequencer? bootstrap = null)
{
	var partner = new PartnerManager(
		content ?? new FakeContentLookup(),
		inventory ?? new FakeInventorySource(),
		intel ?? new RecordingIntelSink(),
		snapshotStore ?? new FakeSnapshotStore(),
		hub ?? new FakeHubSignalSource { Current = HubDockingState.Landed },
		bootstrap ?? new QueuedBootstrapSequencer());
	partner.Initialize(HubDockingState.Landed);
	return partner;
}

static PartnerManager PromptedPartner()
{
	var partner = NewPartner(ContentWithItems("item.valid"));
	MoveToLivingIdle(partner);
	partner.ScoutSniff("item.valid");
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	partner.OnPlayerReturnedToHub();
	return partner;
}

static PartnerManager PartnerWithNestSniffs(int count)
{
	var itemIds = Enumerable.Range(0, count)
		.Select(index => $"item.{index}")
		.ToArray();
	var partner = NewPartner(ContentWithItems(itemIds));
	MoveToLivingIdle(partner);
	foreach (var itemId in itemIds)
	{
		partner.ScoutSniff(itemId);
		partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	}

	return partner;
}

static PartnerState SkyCat(PartnerManager partner)
{
	return partner.Partners[PartnerManager.MvpPartnerId];
}

static void MoveToLivingIdle(PartnerManager partner)
{
	partner.OnPlayerEnteredZone(HubIds.LivingQuarters);
	partner.AdvanceTime(PartnerManager.CatStateCooldownSeconds);
}

static FakeContentLookup ContentWithItems(params string[] itemIds)
{
	var content = new FakeContentLookup();
	foreach (var itemId in itemIds)
	{
		content.Add(itemId, ItemWithSignature($"location.{itemId}"));
	}

	return content;
}

static Dictionary<string, object?> SnapshotWithName(string name)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["name"] = name,
		["naming_done"] = true,
		["naming_skip_count"] = 0,
		["sniff_success_occurred"] = true,
		["sniffed_items"] = new[] { "item.previous" },
		["nest_items"] = new object[] { 0 },
	};
}

static Dictionary<string, object?> ItemWithoutSignature(string id)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = id,
		["kind"] = "resource",
	};
}

static Dictionary<string, object?> ItemWithSignature(
	string revealTarget,
	string hazardHint = "old-harbor-chain",
	int confidence = 65,
	string patternId = "pattern.ancient-optics")
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = revealTarget,
		["kind"] = "resource",
		["cat_sniff_signature"] = new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["reveal_target"] = revealTarget,
			["hazard_hint"] = hazardHint,
			["confidence"] = confidence,
			["pattern_id"] = patternId,
		},
	};
}

sealed class FakeContentLookup : IPartnerContentLookup
{
	private readonly Dictionary<string, IReadOnlyDictionary<string, object?>> _entities = new(StringComparer.Ordinal);

	public FakeContentLookup Add(string itemId, IReadOnlyDictionary<string, object?> entity)
	{
		_entities[itemId] = entity;
		return this;
	}

	public IReadOnlyDictionary<string, object?>? QueryEntity(string itemId)
	{
		return _entities.TryGetValue(itemId, out var entity) ? entity : null;
	}
}

sealed class FakeInventorySource : IPartnerInventorySource
{
	private readonly List<string> _items;

	public FakeInventorySource(params string[] items)
	{
		_items = items.ToList();
	}

	public bool ThrowOnRead { get; init; }

	public IReadOnlyList<string> GetInventoryItems()
	{
		if (ThrowOnRead)
		{
			throw new InvalidOperationException("inventory unavailable");
		}

		return _items.ToArray();
	}
}

sealed class RecordingIntelSink : IPartnerIntelSink
{
	public List<RevealRumorCall> RevealCalls { get; } = new();

	public List<ReportObservationCall> ReportCalls { get; } = new();

	public List<string> PartnerJoinedCalls { get; } = new();

	public bool ThrowOnReveal { get; init; }

	public void RevealRumor(string revealTarget, string sourceTag, IReadOnlyList<string> hazardTags, int confidence)
	{
		if (ThrowOnReveal)
		{
			throw new InvalidOperationException("reveal failed");
		}

		RevealCalls.Add(new RevealRumorCall(revealTarget, sourceTag, hazardTags.ToArray(), confidence));
	}

	public void ReportObservationEvent(string patternId, string eventId)
	{
		ReportCalls.Add(new ReportObservationCall(patternId, eventId));
	}

	public void OnPartnerJoined(string partnerId)
	{
		PartnerJoinedCalls.Add(partnerId);
	}
}

sealed class FakeSnapshotStore : IPartnerSnapshotStore
{
	private readonly IReadOnlyDictionary<string, object?> _snapshot;
	private readonly List<string>? _log;

	public FakeSnapshotStore(List<string>? log = null, IReadOnlyDictionary<string, object?>? snapshot = null)
	{
		_log = log;
		_snapshot = snapshot ?? new Dictionary<string, object?>(StringComparer.Ordinal);
	}

	public List<string> RequestedDomains { get; } = new();

	public IReadOnlyDictionary<string, object?> RestoreSnapshot(string domainId)
	{
		_log?.Add("snapshot:restore");
		RequestedDomains.Add(domainId);
		return _snapshot;
	}
}

sealed class FakeHubSignalSource : IPartnerHubSignalSource
{
	private readonly HashSet<string> _zones = new(StringComparer.Ordinal);
	private readonly List<string>? _log;
	private Action<HubDockingState>? _hubStateChanged;
	private Action? _playerReturnedToHub;
	private Action<string>? _playerEnteredZone;

	public FakeHubSignalSource(List<string>? log = null)
	{
		_log = log;
	}

	public HubDockingState Current { get; init; } = HubDockingState.Landed;

	public int HubStateChangedSubscriptions { get; private set; }

	public int PlayerReturnedSubscriptions { get; private set; }

	public int PlayerEnteredZoneSubscriptions { get; private set; }

	public HubDockingState CurrentHubState
	{
		get
		{
			_log?.Add("hub:current_state");
			return Current;
		}
	}

	public event Action<HubDockingState>? HubStateChanged
	{
		add
		{
			HubStateChangedSubscriptions++;
			_log?.Add("hub:connect:state");
			_hubStateChanged += value;
		}
		remove => _hubStateChanged -= value;
	}

	public event Action? PlayerReturnedToHub
	{
		add
		{
			PlayerReturnedSubscriptions++;
			_log?.Add("hub:connect:return");
			_playerReturnedToHub += value;
		}
		remove => _playerReturnedToHub -= value;
	}

	public event Action<string>? PlayerEnteredZone
	{
		add
		{
			PlayerEnteredZoneSubscriptions++;
			_log?.Add("hub:connect:zone");
			_playerEnteredZone += value;
		}
		remove => _playerEnteredZone -= value;
	}

	public bool IsPlayerInZone(string zoneId)
	{
		_log?.Add($"hub:is_player_in_zone:{zoneId}");
		return _zones.Contains(zoneId);
	}

	public void SetPlayerInZone(string zoneId, bool inZone)
	{
		if (inZone)
		{
			_zones.Add(zoneId);
			return;
		}

		_zones.Remove(zoneId);
	}

	public void EmitHubStateChanged(HubDockingState state)
	{
		_hubStateChanged?.Invoke(state);
	}

	public void EmitPlayerReturnedToHub()
	{
		_playerReturnedToHub?.Invoke();
	}

	public void EmitPlayerEnteredZone(string zoneId)
	{
		_playerEnteredZone?.Invoke(zoneId);
	}
}

sealed class QueuedBootstrapSequencer : IPartnerBootstrapSequencer
{
	private readonly Queue<Action> _callbacks = new();
	private readonly List<string>? _log;

	public QueuedBootstrapSequencer(List<string>? log = null)
	{
		_log = log;
	}

	public int QueuedCount => _callbacks.Count;

	public void QueueCall(Action callback)
	{
		_log?.Add("bootstrap:queue_call");
		_callbacks.Enqueue(callback);
	}

	public void DispatchAll()
	{
		while (_callbacks.Count > 0)
		{
			_callbacks.Dequeue().Invoke();
		}
	}
}

sealed record RevealRumorCall(
	string RevealTarget,
	string SourceTag,
	IReadOnlyList<string> HazardTags,
	int Confidence);

sealed record ReportObservationCall(string PatternId, string EventId);
