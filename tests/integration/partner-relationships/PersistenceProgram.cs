using System.Text.Json;
using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #15 Story 005: Persistence & State Recovery ===");
var failed = 0;
var total = 0;

Run("AC-1: new game initializes all seven persisted fields", Ac1NewGameDefaults);
Run("AC-2: mid-state canonical JSON roundtrip preserves all seven fields", Ac2MidStateRoundtrip);
Run("AC-3: full end-state canonical JSON roundtrip preserves all seven fields", Ac3FullStateRoundtrip);
Run("AC-4: loaded save derives cat_state from Hub context instead of restoring SNIFFING", Ac4TransientCatStateDerived);
Run("AC-5: loaded save resets transient cooldown and lockout timers", Ac5TransientTimersReset);
Run("AC-6: sniff success flag without sniffed items is corrected with warning", Ac6SniffFlagCorrection);
Run("AC-7: naming_done is authoritative over inconsistent skip count", Ac7NamingDoneAuthoritative);
Run("AC-8: nest item index gaps warn while preserving raw data", Ac8NestGapWarnsAndPreserves);
Run("AC-9: successful scout_sniff triggers partner snapshot capture", Ac9ScoutSniffTriggersSnapshot);
Run("AC-10: submit_partner_name and skip_naming trigger snapshot capture", Ac10NamingMutationsTriggerSnapshot);
Run("AC-11: nest_state change is captured after successful sniff mutation", Ac11NestStateChangeCaptured);
Run("AC-12: prompted naming with two skips recovers as pending with one chance left", Ac12PromptedTwoSkipsRecoversPending);
Run("AC-13: prompted naming with three skips recovers as completed default name", Ac13PromptedThreeSkipsRecoversCompleted);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 005 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 005 validation passed: {total}/{total} checks passed.");
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

static bool Ac1NewGameDefaults()
{
	var partner = NewPartner();
	partner.InitNewGameState();
	var snapshot = partner.BuildProgressPartnerSnapshot();

	return StringField(snapshot, "name") == string.Empty
		&& !BoolField(snapshot, "naming_done")
		&& IntField(snapshot, "naming_skip_count") == 0
		&& !BoolField(snapshot, "sniff_success_occurred")
		&& IntField(snapshot, "nest_state") == (int)PartnerNestState.Empty
		&& IntList(snapshot, "nest_items").Count == 0
		&& StringList(snapshot, "sniffed_items").Count == 0;
}

static bool Ac2MidStateRoundtrip()
{
	var source = NewPartner();
	source.RestoreFromProgressPartner(Snapshot(
		name: "小云",
		namingDone: true,
		namingSkipCount: 0,
		sniffSuccessOccurred: true,
		nestState: PartnerNestState.Accumulating,
		nestItems: [0, 1],
		sniffedItems: ["item.a", "item.b", "item.c"]));

	var target = RoundtripViaCanonicalJson(source);
	return SameSevenFields(source.BuildProgressPartnerSnapshot(), target.BuildProgressPartnerSnapshot())
		&& source.BuildSnapshotPackage().ValidateContract().Valid;
}

static bool Ac3FullStateRoundtrip()
{
	var source = NewPartner();
	source.RestoreFromProgressPartner(Snapshot(
		name: "那只猫",
		namingDone: true,
		namingSkipCount: 3,
		sniffSuccessOccurred: true,
		nestState: PartnerNestState.Full,
		nestItems: [0, 1, 2, 3],
		sniffedItems: ["item.a", "item.b", "item.c", "item.d", "item.e", "item.f"]));

	var target = RoundtripViaCanonicalJson(source);
	return SameSevenFields(source.BuildProgressPartnerSnapshot(), target.BuildProgressPartnerSnapshot())
		&& SkyCat(target).NamingState == PartnerNamingState.Completed;
}

static bool Ac4TransientCatStateDerived()
{
	var source = PartnerWithSuccessfulSniff();
	var savedWhileSniffing = source.CatState == PartnerCatState.Sniffing;
	var snapshot = source.BuildProgressPartnerSnapshot();

	var target = NewPartner();
	target.RestoreFromProgressPartner(snapshot);

	return savedWhileSniffing
		&& target.CatState != PartnerCatState.Sniffing
		&& target.CatState == PartnerCatState.SleepingOnIntelStation;
}

static bool Ac5TransientTimersReset()
{
	var source = PartnerWithSuccessfulSniff();
	var sourceHadTimer = source.SniffLockoutRemaining > 0.0d;
	var snapshot = source.BuildProgressPartnerSnapshot();

	var target = NewPartner();
	MoveToLivingIdle(target);
	target.ScoutSniff("item.ignored");
	target.RestoreFromProgressPartner(snapshot);

	return sourceHadTimer
		&& target.CatStateCooldownRemaining == 0.0d
		&& target.SniffLockoutRemaining == 0.0d;
}

static bool Ac6SniffFlagCorrection()
{
	var partner = NewPartner();
	partner.RestoreFromProgressPartner(Snapshot(
		sniffSuccessOccurred: true,
		nestItems: [],
		sniffedItems: []));
	var state = SkyCat(partner);

	return !state.SniffSuccessOccurred
		&& partner.Warnings.Contains("partner_sniff_success_without_items_corrected");
}

static bool Ac7NamingDoneAuthoritative()
{
	var partner = NewPartner();
	partner.RestoreFromProgressPartner(Snapshot(
		name: "小云",
		namingDone: true,
		namingSkipCount: 2,
		sniffSuccessOccurred: true,
		nestItems: [0],
		sniffedItems: ["item.a"]));
	var state = SkyCat(partner);

	return state.NamingDone
		&& state.NamingSkipCount == 2
		&& state.NamingState == PartnerNamingState.Completed
		&& partner.QueryPartnerName() == "小云";
}

static bool Ac8NestGapWarnsAndPreserves()
{
	var partner = NewPartner();
	partner.RestoreFromProgressPartner(Snapshot(
		sniffSuccessOccurred: true,
		nestState: PartnerNestState.Accumulating,
		nestItems: [0, 3],
		sniffedItems: ["item.a"]));

	return partner.QueryNestItems().SequenceEqual([0, 3])
		&& partner.Warnings.Contains("partner_nest_items_order_gap_preserved");
}

static bool Ac9ScoutSniffTriggersSnapshot()
{
	var sink = new RecordingSnapshotSink();
	var partner = NewPartner(ContentWithItems("item.a"), sink: sink);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.a");
	var capture = sink.Captures.SingleOrDefault();

	return result.Success
		&& capture is not null
		&& capture.DomainId == PartnerManager.PartnerSnapshotDomainId
		&& StringList(capture.Payload, "sniffed_items").SequenceEqual(["item.a"])
		&& BoolField(capture.Payload, "sniff_success_occurred");
}

static bool Ac10NamingMutationsTriggerSnapshot()
{
	var submitSink = new RecordingSnapshotSink();
	var submitPartner = PromptedPartner(submitSink);
	submitSink.Clear();
	var submit = submitPartner.SubmitPartnerName("小云");

	var skipSink = new RecordingSnapshotSink();
	var skipPartner = PromptedPartner(skipSink);
	skipSink.Clear();
	var skipped = skipPartner.SkipNaming();

	return submit.Accepted
		&& submitSink.Captures.Count == 1
		&& StringField(submitSink.Captures[0].Payload, "name") == "小云"
		&& skipped
		&& skipSink.Captures.Count == 1
		&& IntField(skipSink.Captures[0].Payload, "naming_skip_count") == 1;
}

static bool Ac11NestStateChangeCaptured()
{
	var sink = new RecordingSnapshotSink();
	var partner = NewPartner(ContentWithItems("item.a"), sink: sink);
	var snapshotCountDuringNestEvent = -1;
	partner.NestStateChanged += (_, _) => snapshotCountDuringNestEvent = sink.Captures.Count;
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.a");

	return result.Success
		&& snapshotCountDuringNestEvent == 0
		&& sink.Captures.Count == 1
		&& IntField(sink.Captures[0].Payload, "nest_state") == (int)PartnerNestState.First
		&& IntList(sink.Captures[0].Payload, "nest_items").SequenceEqual([0]);
}

static bool Ac12PromptedTwoSkipsRecoversPending()
{
	var partner = NewPartner();
	partner.RestoreFromProgressPartner(Snapshot(
		namingDone: false,
		namingSkipCount: 2,
		sniffSuccessOccurred: true,
		namingState: PartnerNamingState.Prompted,
		nestItems: [0],
		sniffedItems: ["item.a"]));
	var state = SkyCat(partner);
	var recoveredState = state.NamingState;
	var recoveredSkipCount = state.NamingSkipCount;
	var prompted = partner.OnPlayerReturnedToHub();

	return recoveredState == PartnerNamingState.Pending
		&& recoveredSkipCount == 2
		&& prompted
		&& SkyCat(partner).NamingState == PartnerNamingState.Prompted;
}

static bool Ac13PromptedThreeSkipsRecoversCompleted()
{
	var partner = NewPartner();
	partner.RestoreFromProgressPartner(Snapshot(
		namingDone: false,
		namingSkipCount: 3,
		sniffSuccessOccurred: true,
		namingState: PartnerNamingState.Prompted,
		nestItems: [0],
		sniffedItems: ["item.a"]));
	var state = SkyCat(partner);

	return state.NamingState == PartnerNamingState.Completed
		&& state.NamingDone
		&& state.Name == PartnerManager.DefaultPartnerName
		&& !partner.OnPlayerReturnedToHub();
}

static PartnerManager RoundtripViaCanonicalJson(PartnerManager source)
{
	var json = Persistence.CanonicalJsonEncode(source.BuildProgressPartnerSnapshot());
	var parsed = ParseObject(json);
	var target = NewPartner();
	target.RestoreFromProgressPartner(parsed);
	return target;
}

static bool SameSevenFields(
	IReadOnlyDictionary<string, object?> left,
	IReadOnlyDictionary<string, object?> right)
{
	return StringField(left, "name") == StringField(right, "name")
		&& BoolField(left, "naming_done") == BoolField(right, "naming_done")
		&& IntField(left, "naming_skip_count") == IntField(right, "naming_skip_count")
		&& BoolField(left, "sniff_success_occurred") == BoolField(right, "sniff_success_occurred")
		&& IntField(left, "nest_state") == IntField(right, "nest_state")
		&& IntList(left, "nest_items").SequenceEqual(IntList(right, "nest_items"))
		&& StringList(left, "sniffed_items").SequenceEqual(StringList(right, "sniffed_items"));
}

static PartnerManager PartnerWithSuccessfulSniff()
{
	var partner = NewPartner(ContentWithItems("item.a"));
	MoveToLivingIdle(partner);
	partner.ScoutSniff("item.a");
	return partner;
}

static PartnerManager PromptedPartner(RecordingSnapshotSink sink)
{
	var partner = NewPartner(ContentWithItems("item.a"), sink: sink);
	MoveToLivingIdle(partner);
	partner.ScoutSniff("item.a");
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	partner.OnPlayerReturnedToHub();
	return partner;
}

static PartnerManager NewPartner(
	FakeContentLookup? content = null,
	RecordingSnapshotSink? sink = null,
	FakeHubSignalSource? hub = null)
{
	var partner = new PartnerManager(
		content ?? new FakeContentLookup(),
		new FakeInventorySource(),
		new RecordingIntelSink(),
		new FakeSnapshotStore(),
		hub ?? new FakeHubSignalSource { Current = HubDockingState.Landed },
		new QueuedBootstrapSequencer(),
		sink ?? new RecordingSnapshotSink());
	partner.Initialize(HubDockingState.Landed);
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

static Dictionary<string, object?> Snapshot(
	string name = "",
	bool namingDone = false,
	int namingSkipCount = 0,
	bool sniffSuccessOccurred = false,
	PartnerNestState nestState = PartnerNestState.Empty,
	int[]? nestItems = null,
	string[]? sniffedItems = null,
	PartnerNamingState? namingState = null)
{
	var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["domain_id"] = "partner_skycat",
		["name"] = name,
		["naming_done"] = namingDone,
		["naming_skip_count"] = namingSkipCount,
		["sniff_success_occurred"] = sniffSuccessOccurred,
		["nest_state"] = (int)nestState,
		["nest_items"] = (nestItems ?? []).Cast<object?>().ToList(),
		["sniffed_items"] = (sniffedItems ?? []).Cast<object?>().ToList(),
	};
	if (namingState is not null)
	{
		snapshot["naming_state"] = (int)namingState.Value;
	}

	return snapshot;
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

static Dictionary<string, object?> ItemWithSignature(string revealTarget)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = revealTarget,
		["kind"] = "resource",
		["cat_sniff_signature"] = new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["reveal_target"] = revealTarget,
			["hazard_hint"] = "old-harbor-chain",
			["confidence"] = 65,
			["pattern_id"] = "pattern.ancient-optics",
		},
	};
}

static string StringField(IReadOnlyDictionary<string, object?> data, string key)
{
	return data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
}

static bool BoolField(IReadOnlyDictionary<string, object?> data, string key)
{
	return data.TryGetValue(key, out var value) && Convert.ToBoolean(value);
}

static int IntField(IReadOnlyDictionary<string, object?> data, string key)
{
	return data.TryGetValue(key, out var value) ? Convert.ToInt32(value) : 0;
}

static IReadOnlyList<int> IntList(IReadOnlyDictionary<string, object?> data, string key)
{
	if (!data.TryGetValue(key, out var value) || value is not System.Collections.IEnumerable values)
	{
		return [];
	}

	return values.Cast<object?>().Select(Convert.ToInt32).ToArray();
}

static IReadOnlyList<string> StringList(IReadOnlyDictionary<string, object?> data, string key)
{
	if (!data.TryGetValue(key, out var value) || value is not System.Collections.IEnumerable values)
	{
		return [];
	}

	return values.Cast<object?>().Select(item => item?.ToString() ?? string.Empty).ToArray();
}

static Dictionary<string, object?> ParseObject(string json)
{
	using var document = JsonDocument.Parse(json);
	return (Dictionary<string, object?>)ConvertJson(document.RootElement)!;
}

static object? ConvertJson(JsonElement element)
{
	return element.ValueKind switch
	{
		JsonValueKind.Object => element.EnumerateObject().ToDictionary(
			property => property.Name,
			property => ConvertJson(property.Value),
			StringComparer.Ordinal),
		JsonValueKind.Array => element.EnumerateArray().Select(ConvertJson).ToList(),
		JsonValueKind.String => element.GetString(),
		JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
		JsonValueKind.Number => element.GetDouble(),
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		_ => null,
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
	public IReadOnlyList<string> GetInventoryItems()
	{
		return [];
	}
}

sealed class FakeSnapshotStore : IPartnerSnapshotStore
{
	public IReadOnlyDictionary<string, object?> RestoreSnapshot(string domainId)
	{
		return new Dictionary<string, object?>(StringComparer.Ordinal);
	}
}

sealed class RecordingSnapshotSink : IPartnerSnapshotSink
{
	public List<SnapshotCapture> Captures { get; } = [];

	public void CaptureSnapshot(string domainId, IReadOnlyDictionary<string, object?> payload)
	{
		Captures.Add(new SnapshotCapture(
			domainId,
			new Dictionary<string, object?>(payload, StringComparer.Ordinal)));
	}

	public void Clear()
	{
		Captures.Clear();
	}
}

sealed class FakeHubSignalSource : IPartnerHubSignalSource
{
	public HubDockingState Current { get; init; } = HubDockingState.Landed;

	public HubDockingState CurrentHubState => Current;

	public event Action<HubDockingState>? HubStateChanged
	{
		add { }
		remove { }
	}

	public event Action? PlayerReturnedToHub
	{
		add { }
		remove { }
	}

	public event Action<string>? PlayerEnteredZone
	{
		add { }
		remove { }
	}

	public bool IsPlayerInZone(string zoneId)
	{
		return false;
	}
}

sealed class QueuedBootstrapSequencer : IPartnerBootstrapSequencer
{
	public void QueueCall(Action callback)
	{
		callback();
	}
}

sealed class RecordingIntelSink : IPartnerIntelSink
{
	public void RevealRumor(string revealTarget, string sourceTag, IReadOnlyList<string> hazardTags, int confidence)
	{
	}

	public void ReportObservationEvent(string patternId, string eventId)
	{
	}

	public void OnPartnerJoined(string partnerId)
	{
	}
}

sealed record SnapshotCapture(
	string DomainId,
	IReadOnlyDictionary<string, object?> Payload);
