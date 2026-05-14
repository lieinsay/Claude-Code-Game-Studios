using System.Reflection;
using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #15 Story 006: Edge Cases, R15 Guards & Defensive Handling ===");
var failed = 0;
var total = 0;

Run("AC-1: no successful sniff means repeated hub returns never prompt naming", Ac1NoSniffNeverPromptsNaming);
Run("AC-2: third naming skip writes the default name without reopening the prompt", Ac2ThirdSkipWritesDefaultName);
Run("AC-3: whitespace-only submitted name returns name_empty without skip mutation", Ac3WhitespaceNameRejected);
Run("AC-4: save/load during naming prompt recovers as pending with skip count preserved", Ac4PromptedSaveRecoversPending);
Run("AC-5: departure lock does not clear an active naming prompt state", Ac5NamingPromptSurvivesDepartureLock);
Run("AC-6: empty inventory returns no sniffable items", Ac6EmptyInventoryReturnsEmpty);
Run("AC-7: inventory with no sniff signatures returns no sniffable items", Ac7UnsignedInventoryReturnsEmpty);
Run("AC-8: second sniff of same item returns already smelled without Intel writes or inventory mutation", Ac8DuplicateItemShortCircuits);
Run("AC-9: confidence clamp caps raw 90 at 66", Ac9ConfidenceClampCapsAt66);
Run("AC-10: empty reveal target returns confused without Intel writes", Ac10EmptyRevealTargetConfused);
Run("AC-11: two items sharing reveal target each call reveal_rumor independently", Ac11SharedRevealTargetStillReportsTwice);
Run("AC-12: rapid scout_sniff spam allows only one success while sniffing gate is active", Ac12RapidSniffSpamIsGated);
Run("AC-13: departure during sniff animation preserves already committed sniff data", Ac13DepartureDuringSniffPreservesData);
Run("AC-14: departure after one sniff leaves remaining inventory available for future sniff", Ac14DepartureLeavesRemainingItems);
Run("AC-15: nest remains empty before any successful sniff", Ac15NoSniffKeepsNestEmpty);
Run("AC-16: fifth sniff after full nest leaves nest items unchanged", Ac16FullNestCapsSilently);
Run("AC-17: save/load restores accumulating nest items exactly", Ac17RestoreAccumulatingNestItems);
Run("AC-18: failed snapshot write does not corrupt next restored snapshot", Ac18FailedSnapshotWriteDoesNotCorruptRestore);
Run("AC-19: load never restores transient sniffing cat state", Ac19SniffingStateIsNotRestored);
Run("AC-20: rapid zone spam produces at most two state transitions within one second", Ac20ZoneSpamDebounced);
Run("AC-21: arrival forces cat out of nest into living quarters idle", Ac21ArrivalForcesLivingIdle);
Run("AC-22: entering living quarters pulls cat out of nest", Ac22LivingQuartersPullsCatFromNest);
Run("AC-23: reveal_rumor exception is caught while local sniff state commits", Ac23RevealRumorExceptionIsSafe);
Run("AC-24: nonexistent pattern id is passed through without Partner validation", Ac24NonexistentPatternPassesThrough);
Run("AC-25: unknown cat_sniff_signature fields are ignored", Ac25UnknownSignatureFieldsIgnored);
Run("AC-26: query_partner_present returns true in transit", Ac26PresentInTransit);
Run("AC-27: duplicate player_returned_to_hub emissions trigger naming only once", Ac27DuplicateReturnPromptsOnce);
Run("AC-28: sync_with_hub_state repairs missed pre-subscription Hub events", Ac28SyncRepairsInitRace);
Run("AC-29: API and data model expose no relationship meter fields", Ac29NoRelationshipMeterFields);
Run("AC-30: scout_sniff is the only item interaction path", Ac30ScoutSniffOnlyItemInteractionPath);
Run("AC-31: cat behavior exposes no event tree, story node, or dialogue branch API", Ac31NoEventTreeOrDialogueBranchApi);
Run("AC-32: cat state changes are not process-driven and expose no reward tick API", Ac32NoProcessDrivenRewards);
Run("AC-33: initialized partners dictionary contains only partner.sky-cat", Ac33OnlySkyCatPartner);
Run("AC-34: no recruit, dismiss, remove, or add partner API exists; join is queued once", Ac34NoRecruitDismissAndBootstrapJoinOnce);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 006 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 006 validation passed: {total}/{total} checks passed.");
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

static bool Ac1NoSniffNeverPromptsNaming()
{
	var partner = NewPartner();
	var prompts = 0;
	partner.NamingPromptTriggered += () => prompts++;

	var first = partner.OnPlayerReturnedToHub();
	var second = partner.OnPlayerReturnedToHub();
	var third = partner.OnPlayerReturnedToHub();

	return !first
		&& !second
		&& !third
		&& prompts == 0
		&& !SkyCat(partner).SniffSuccessOccurred
		&& SkyCat(partner).NamingState == PartnerNamingState.Pending;
}

static bool Ac2ThirdSkipWritesDefaultName()
{
	var sink = new RecordingSnapshotSink();
	var partner = PromptedPartnerWithSkipCount(2, sink: sink);
	var prompts = 0;
	var completedNames = new List<string>();
	partner.NamingPromptTriggered += () => prompts++;
	partner.NamingCompleted += completedNames.Add;
	sink.Clear();

	var skipped = partner.SkipNaming();
	var state = SkyCat(partner);
	var captured = sink.Captures.SingleOrDefault();

	return skipped
		&& prompts == 0
		&& completedNames.SequenceEqual([PartnerManager.DefaultPartnerName])
		&& state.NamingSkipCount == PartnerManager.NamingSkipMax
		&& state.Name == PartnerManager.DefaultPartnerName
		&& state.NamingDone
		&& state.NamingState == PartnerNamingState.Completed
		&& captured is not null
		&& StringField(captured.Payload, "name") == PartnerManager.DefaultPartnerName;
}

static bool Ac3WhitespaceNameRejected()
{
	var partner = PromptedPartner();
	var beforeSkipCount = SkyCat(partner).NamingSkipCount;

	var result = partner.SubmitPartnerName("   ");
	var state = SkyCat(partner);

	return !result.Accepted
		&& result.Error == "name_empty"
		&& state.NamingSkipCount == beforeSkipCount
		&& state.NamingState == PartnerNamingState.Prompted
		&& !state.NamingDone;
}

static bool Ac4PromptedSaveRecoversPending()
{
	var loaded = NewPartner();
	loaded.RestoreFromProgressPartner(Snapshot(
		namingDone: false,
		namingSkipCount: 2,
		sniffSuccessOccurred: true,
		namingState: PartnerNamingState.Prompted,
		nestItems: [0],
		sniffedItems: ["item.prior"]));

	var recoveredState = SkyCat(loaded).NamingState;
	var recoveredSkipCount = SkyCat(loaded).NamingSkipCount;
	var prompted = loaded.OnPlayerReturnedToHub();

	return recoveredState == PartnerNamingState.Pending
		&& recoveredSkipCount == 2
		&& prompted
		&& SkyCat(loaded).NamingState == PartnerNamingState.Prompted;
}

static bool Ac5NamingPromptSurvivesDepartureLock()
{
	var partner = PromptedPartner();
	partner.OnHubStateChanged(HubDockingState.DepartureLocked);

	return SkyCat(partner).NamingState == PartnerNamingState.Prompted
		&& partner.IsStateFrozen
		&& !partner.IsCatInteractable;
}

static bool Ac6EmptyInventoryReturnsEmpty()
{
	var partner = NewPartner(inventory: new FakeInventorySource());

	return partner.GetSniffableItems().Count == 0;
}

static bool Ac7UnsignedInventoryReturnsEmpty()
{
	var content = new FakeContentLookup()
		.Add("item.a", ItemWithoutSignature("item.a"))
		.Add("item.b", ItemWithoutSignature("item.b"));
	var inventory = new FakeInventorySource("item.a", "item.b");
	var partner = NewPartner(content, inventory);

	return partner.GetSniffableItems().Count == 0;
}

static bool Ac8DuplicateItemShortCircuits()
{
	var content = ContentWithItems("item.a");
	var inventory = new FakeInventorySource("item.a");
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, inventory, intel);
	MoveToLivingIdle(partner);

	var before = inventory.GetInventoryItems();
	var first = partner.ScoutSniff("item.a");
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	var second = partner.ScoutSniff("item.a");
	var after = inventory.GetInventoryItems();

	return first.Success
		&& !second.Success
		&& second.ReactionId == (int)ScoutSniffReaction.AlreadySmelled
		&& second.Error == "already_sniffed"
		&& intel.RevealCalls.Count == 1
		&& intel.ReportCalls.Count == 1
		&& before.SequenceEqual(after)
		&& after.SequenceEqual(["item.a"]);
}

static bool Ac9ConfidenceClampCapsAt66()
{
	return PartnerManager.ClampConfidence(90) == PartnerManager.MvpConfidenceMax;
}

static bool Ac10EmptyRevealTargetConfused()
{
	var content = new FakeContentLookup()
		.Add("item.empty-target", ItemWithSignature("item.empty-target", revealTarget: string.Empty));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.empty-target");

	return !result.Success
		&& result.ReactionId == (int)ScoutSniffReaction.Confused
		&& result.Error == "empty_reveal_target"
		&& intel.RevealCalls.Count == 0
		&& intel.ReportCalls.Count == 0
		&& SkyCat(partner).SniffedItems.Count == 0;
}

static bool Ac11SharedRevealTargetStillReportsTwice()
{
	var content = new FakeContentLookup()
		.Add("item.low", ItemWithSignature("item.low", revealTarget: "location.shared", confidence: 30))
		.Add("item.high", ItemWithSignature("item.high", revealTarget: "location.shared", confidence: 90));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var first = partner.ScoutSniff("item.low");
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	var second = partner.ScoutSniff("item.high");

	return first.Success
		&& second.Success
		&& intel.RevealCalls.Count == 2
		&& intel.RevealCalls.All(call => call.RevealTarget == "location.shared")
		&& intel.RevealCalls.Select(call => call.Confidence).SequenceEqual([30, 66]);
}

static bool Ac12RapidSniffSpamIsGated()
{
	var content = ContentWithItems("item.0", "item.1", "item.2", "item.3", "item.4");
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var results = Enumerable.Range(0, 5)
		.Select(index =>
		{
			var result = partner.ScoutSniff($"item.{index}");
			partner.AdvanceTime(0.01d);
			return result;
		})
		.ToArray();

	return results.Count(result => result.Success) == 1
		&& results.Skip(1).All(result => result.Error == "cat_busy")
		&& SkyCat(partner).SniffedItems.SequenceEqual(["item.0"])
		&& intel.RevealCalls.Count == 1;
}

static bool Ac13DepartureDuringSniffPreservesData()
{
	var sink = new RecordingSnapshotSink();
	var partner = NewPartner(ContentWithItems("item.a"), sink: sink);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.a");
	partner.OnHubStateChanged(HubDockingState.DepartureLocked);

	return result.Success
		&& partner.CatState == PartnerCatState.Sniffing
		&& partner.IsStateFrozen
		&& SkyCat(partner).SniffedItems.SequenceEqual(["item.a"])
		&& partner.QueryNestItems().SequenceEqual([0])
		&& sink.Captures.Count == 1
		&& StringList(sink.Captures[0].Payload, "sniffed_items").SequenceEqual(["item.a"]);
}

static bool Ac14DepartureLeavesRemainingItems()
{
	var content = ContentWithItems("item.a", "item.b");
	var inventory = new FakeInventorySource("item.a", "item.b");
	var partner = NewPartner(content, inventory);
	MoveToLivingIdle(partner);

	var first = partner.ScoutSniff("item.a");
	partner.OnHubStateChanged(HubDockingState.DepartureLocked);
	var duringDepartureInventory = inventory.GetInventoryItems();
	partner.OnHubStateChanged(HubDockingState.Arrival);
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	var second = partner.ScoutSniff("item.b");

	return first.Success
		&& second.Success
		&& duringDepartureInventory.SequenceEqual(["item.a", "item.b"])
		&& SkyCat(partner).SniffedItems.SequenceEqual(["item.a", "item.b"]);
}

static bool Ac15NoSniffKeepsNestEmpty()
{
	var partner = NewPartner();
	partner.OnHubStateChanged(HubDockingState.DepartureLocked);
	partner.OnHubStateChanged(HubDockingState.Arrival);
	partner.OnLivingQuartersIdleElapsed(PartnerManager.NestSettleSeconds + 1.0d);

	return !SkyCat(partner).SniffSuccessOccurred
		&& partner.QueryNestState() == (int)PartnerNestState.Empty
		&& partner.QueryNestItems().Count == 0;
}

static bool Ac16FullNestCapsSilently()
{
	var partner = PartnerWithNestSniffs(PartnerManager.NestCapacity);
	var before = partner.QueryNestItems();
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);

	var fifth = partner.ScoutSniff("item.4");
	var after = partner.QueryNestItems();

	return fifth.Success
		&& before.SequenceEqual([0, 1, 2, 3])
		&& after.SequenceEqual([0, 1, 2, 3])
		&& partner.QueryNestState() == (int)PartnerNestState.Full;
}

static bool Ac17RestoreAccumulatingNestItems()
{
	var partner = NewPartner();
	partner.RestoreFromProgressPartner(Snapshot(
		sniffSuccessOccurred: true,
		nestState: PartnerNestState.Accumulating,
		nestItems: [0, 1],
		sniffedItems: ["item.a", "item.b"]));

	return partner.QueryNestItems().SequenceEqual([0, 1])
		&& partner.QueryNestState() == (int)PartnerNestState.Accumulating;
}

static bool Ac18FailedSnapshotWriteDoesNotCorruptRestore()
{
	var oldSnapshot = Snapshot();
	var sink = new RecordingSnapshotSink { ThrowOnCapture = true };
	var source = NewPartner(ContentWithItems("item.a"), sink: sink);
	MoveToLivingIdle(source);

	var first = source.ScoutSniff("item.a");
	var loaded = NewPartner(ContentWithItems("item.a"));
	loaded.RestoreFromProgressPartner(oldSnapshot);
	MoveToLivingIdle(loaded);
	var retried = loaded.ScoutSniff("item.a");

	return first.Success
		&& source.QueryNestItems().SequenceEqual([0])
		&& source.Warnings.Any(warning => warning.StartsWith("partner_snapshot_capture_failed:", StringComparison.Ordinal))
		&& loaded.QueryNestItems().Count == 1
		&& retried.Success
		&& SkyCat(loaded).SniffedItems.SequenceEqual(["item.a"]);
}

static bool Ac19SniffingStateIsNotRestored()
{
	var source = NewPartner(ContentWithItems("item.a"));
	MoveToLivingIdle(source);
	var sniffed = source.ScoutSniff("item.a");
	var snapshot = source.BuildProgressPartnerSnapshot();

	var target = NewPartner();
	target.RestoreFromProgressPartner(snapshot);

	return sniffed.Success
		&& source.CatState == PartnerCatState.Sniffing
		&& target.CatState != PartnerCatState.Sniffing
		&& target.SniffLockoutRemaining == 0.0d;
}

static bool Ac20ZoneSpamDebounced()
{
	var partner = NewPartner();
	var transitions = 0;
	partner.CatStateChanged += (_, _) => transitions++;

	for (var i = 0; i < 5; i++)
	{
		var zone = i % 2 == 0 ? HubIds.LivingQuarters : "workbench";
		partner.OnPlayerEnteredZone(zone);
		partner.AdvanceTime(0.2d);
	}

	return transitions <= 2;
}

static bool Ac21ArrivalForcesLivingIdle()
{
	var partner = NestingPartner();
	partner.OnHubStateChanged(HubDockingState.Arrival);

	return partner.CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac22LivingQuartersPullsCatFromNest()
{
	var partner = NestingPartner();

	return partner.OnPlayerEnteredZone(HubIds.LivingQuarters)
		&& partner.CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac23RevealRumorExceptionIsSafe()
{
	var intel = new RecordingIntelSink { ThrowOnReveal = true };
	var partner = NewPartner(ContentWithItems("item.a"), intel: intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.a");

	return result.Success
		&& SkyCat(partner).SniffedItems.SequenceEqual(["item.a"])
		&& partner.QueryNestItems().SequenceEqual([0])
		&& partner.Warnings.Any(warning => warning.StartsWith("intel_reveal_rumor_failed:", StringComparison.Ordinal));
}

static bool Ac24NonexistentPatternPassesThrough()
{
	var content = new FakeContentLookup()
		.Add("item.a", ItemWithSignature("item.a", patternId: "pattern.nonexistent"));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.a");

	return result.Success
		&& intel.ReportCalls.SequenceEqual([
			new ReportObservationCall("pattern.nonexistent", PartnerManager.PartnerSniffSuccessEventId)
		]);
}

static bool Ac25UnknownSignatureFieldsIgnored()
{
	var content = new FakeContentLookup()
		.Add("item.a", ItemWithSignature(
			"item.a",
			revealTarget: "location.known",
			hazardHint: "mist",
			confidence: 65,
			patternId: "pattern.known",
			extraFields: new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["future_payload"] = new { Value = "ignored" },
				["confidence_bonus"] = 999,
			}));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, intel: intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.a");
	var call = intel.RevealCalls.SingleOrDefault();

	return result.Success
		&& call is not null
		&& call.RevealTarget == "location.known"
		&& call.HazardTags.SequenceEqual(["mist"])
		&& call.Confidence == 65
		&& intel.ReportCalls.SequenceEqual([
			new ReportObservationCall("pattern.known", PartnerManager.PartnerSniffSuccessEventId)
		]);
}

static bool Ac26PresentInTransit()
{
	var partner = NewPartner();
	partner.OnHubStateChanged(HubDockingState.InTransit);

	return partner.QueryPartnerPresent();
}

static bool Ac27DuplicateReturnPromptsOnce()
{
	var partner = PartnerWithSuccessfulSniff();
	var prompts = 0;
	partner.NamingPromptTriggered += () => prompts++;

	var first = partner.OnPlayerReturnedToHub();
	var second = partner.OnPlayerReturnedToHub();
	var third = partner.OnPlayerReturnedToHub();

	return first
		&& !second
		&& !third
		&& prompts == 1
		&& SkyCat(partner).NamingState == PartnerNamingState.Prompted;
}

static bool Ac28SyncRepairsInitRace()
{
	var hub = new FakeHubSignalSource { CurrentHubState = HubDockingState.Landed };
	hub.SetPlayerInZone(HubIds.LivingQuarters, true);
	hub.EmitHubStateChanged(HubDockingState.Landed);
	var partner = NewPartner(hub: hub);

	partner.OnFeatureReady();

	return partner.CatState == PartnerCatState.IdleLivingQuarters
		&& !partner.IsStateFrozen
		&& partner.IsCatInteractable;
}

static bool Ac29NoRelationshipMeterFields()
{
	var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"affection",
		"friendship",
		"bond",
		"relationship_level",
		"relationshiplevel",
	};

	return !TypeMemberNames(typeof(PartnerState))
			.Concat(TypeMemberNames(typeof(PartnerManager)))
			.Any(name => forbidden.Contains(name));
}

static bool Ac30ScoutSniffOnlyItemInteractionPath()
{
	var methodNames = TypeMethods(typeof(PartnerManager))
		.Select(method => method.Name)
		.ToArray();

	return methodNames.Count(name => name == nameof(PartnerManager.ScoutSniff)) == 1
		&& !methodNames.Any(name =>
			name.Contains("Gift", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("Donate", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("PresentItem", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("GiveItem", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("UseItemOnPartner", StringComparison.OrdinalIgnoreCase));
}

static bool Ac31NoEventTreeOrDialogueBranchApi()
{
	return !TypeMemberNames(typeof(PartnerManager))
		.Concat(TypeMemberNames(typeof(PartnerState)))
		.Any(name =>
			name.Contains("EventTree", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("StoryNode", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("DialogueBranch", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("DialogueTree", StringComparison.OrdinalIgnoreCase));
}

static bool Ac32NoProcessDrivenRewards()
{
	return !TypeMethods(typeof(PartnerManager))
		.Any(method =>
			method.Name.Equals("_Process", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Equals("Process", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Equals("_PhysicsProcess", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("TimerReward", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("TickReward", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("GrantReward", StringComparison.OrdinalIgnoreCase));
}

static bool Ac33OnlySkyCatPartner()
{
	var partner = NewPartner();

	return partner.Partners.Count == 1
		&& partner.Partners.Keys.SequenceEqual([PartnerManager.MvpPartnerId])
		&& partner.Partners[PartnerManager.MvpPartnerId].PartnerId == PartnerManager.MvpPartnerId;
}

static bool Ac34NoRecruitDismissAndBootstrapJoinOnce()
{
	var methodNames = TypeMethods(typeof(PartnerManager))
		.Select(method => method.Name)
		.ToArray();
	var forbiddenApi = methodNames.Any(name =>
		name.Contains("Recruit", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("Dismiss", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("RemovePartner", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("AddPartner", StringComparison.OrdinalIgnoreCase));

	var intel = new RecordingIntelSink();
	var bootstrap = new QueuedBootstrapSequencer();
	var partner = NewPartner(intel: intel, bootstrap: bootstrap);
	var first = partner.OnFeatureReady();
	var second = partner.OnFeatureReady();
	bootstrap.DispatchAll();

	return !forbiddenApi
		&& first
		&& second
		&& intel.PartnerJoinedCalls.SequenceEqual([PartnerManager.MvpPartnerId]);
}

static PartnerManager NewPartner(
	FakeContentLookup? content = null,
	FakeInventorySource? inventory = null,
	RecordingIntelSink? intel = null,
	FakeSnapshotStore? snapshotStore = null,
	FakeHubSignalSource? hub = null,
	QueuedBootstrapSequencer? bootstrap = null,
	RecordingSnapshotSink? sink = null)
{
	var partner = new PartnerManager(
		content ?? new FakeContentLookup(),
		inventory ?? new FakeInventorySource(),
		intel ?? new RecordingIntelSink(),
		snapshotStore ?? new FakeSnapshotStore(),
		hub ?? new FakeHubSignalSource { CurrentHubState = HubDockingState.Landed },
		bootstrap ?? new QueuedBootstrapSequencer(),
		sink ?? new RecordingSnapshotSink());
	partner.Initialize(HubDockingState.Landed);
	return partner;
}

static PartnerManager PartnerWithSuccessfulSniff(string itemId = "item.a")
{
	var partner = NewPartner(ContentWithItems(itemId));
	MoveToLivingIdle(partner);
	partner.ScoutSniff(itemId);
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	return partner;
}

static PartnerManager PromptedPartner()
{
	var partner = PartnerWithSuccessfulSniff();
	partner.OnPlayerReturnedToHub();
	return partner;
}

static PartnerManager PromptedPartnerWithSkipCount(int skipCount, RecordingSnapshotSink? sink = null)
{
	var itemIds = Enumerable.Range(0, Math.Max(1, skipCount + 1))
		.Select(index => $"item.{index}")
		.ToArray();
	var partner = NewPartner(ContentWithItems(itemIds), sink: sink);
	MoveToLivingIdle(partner);
	partner.ScoutSniff(itemIds[0]);
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	partner.OnPlayerReturnedToHub();

	for (var i = 0; i < skipCount; i++)
	{
		partner.SkipNaming();
		if (i < skipCount - 1)
		{
			partner.OnPlayerReturnedToHub();
		}
	}

	if (SkyCat(partner).NamingState == PartnerNamingState.Pending)
	{
		partner.OnPlayerReturnedToHub();
	}

	return partner;
}

static PartnerManager PartnerWithNestSniffs(int count)
{
	var itemIds = Enumerable.Range(0, count + 1)
		.Select(index => $"item.{index}")
		.ToArray();
	var partner = NewPartner(ContentWithItems(itemIds));
	MoveToLivingIdle(partner);
	for (var i = 0; i < count; i++)
	{
		partner.ScoutSniff(itemIds[i]);
		partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	}

	return partner;
}

static PartnerManager NestingPartner()
{
	var partner = NewPartner();
	MoveToLivingIdle(partner);
	partner.OnLivingQuartersIdleElapsed(PartnerManager.NestSettleSeconds + 0.1d);
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
		content.Add(itemId, ItemWithSignature(itemId));
	}

	return content;
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

static Dictionary<string, object?> ItemWithoutSignature(string id)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = id,
		["kind"] = "resource",
	};
}

static Dictionary<string, object?> ItemWithSignature(
	string id,
	string? revealTarget = null,
	string hazardHint = "old-harbor-chain",
	int confidence = 65,
	string patternId = "pattern.ancient-optics",
	IReadOnlyDictionary<string, object?>? extraFields = null)
{
	var signature = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["reveal_target"] = revealTarget ?? $"location.{id}",
		["hazard_hint"] = hazardHint,
		["confidence"] = confidence,
		["pattern_id"] = patternId,
	};

	if (extraFields is not null)
	{
		foreach (var (key, value) in extraFields)
		{
			signature[key] = value;
		}
	}

	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = id,
		["kind"] = "resource",
		["cat_sniff_signature"] = signature,
	};
}

static string StringField(IReadOnlyDictionary<string, object?> data, string key)
{
	return data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
}

static IReadOnlyList<string> StringList(IReadOnlyDictionary<string, object?> data, string key)
{
	if (!data.TryGetValue(key, out var value) || value is string || value is not System.Collections.IEnumerable values)
	{
		return [];
	}

	return values.Cast<object?>().Select(item => item?.ToString() ?? string.Empty).ToArray();
}

static IEnumerable<string> TypeMemberNames(Type type)
{
	return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
		.Select(field => field.Name)
		.Concat(type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			.Select(property => property.Name));
}

static IEnumerable<MethodInfo> TypeMethods(Type type)
{
	return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
		.Where(method => !method.IsSpecialName);
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

	public IReadOnlyList<string> GetInventoryItems()
	{
		return _items.ToArray();
	}
}

sealed class RecordingIntelSink : IPartnerIntelSink
{
	public List<RevealRumorCall> RevealCalls { get; } = [];

	public List<ReportObservationCall> ReportCalls { get; } = [];

	public List<string> PartnerJoinedCalls { get; } = [];

	public bool ThrowOnReveal { get; init; }

	public bool ThrowOnReport { get; init; }

	public bool ThrowOnPartnerJoined { get; init; }

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
		if (ThrowOnReport)
		{
			throw new InvalidOperationException("report failed");
		}

		ReportCalls.Add(new ReportObservationCall(patternId, eventId));
	}

	public void OnPartnerJoined(string partnerId)
	{
		if (ThrowOnPartnerJoined)
		{
			throw new InvalidOperationException("join failed");
		}

		PartnerJoinedCalls.Add(partnerId);
	}
}

sealed class FakeSnapshotStore : IPartnerSnapshotStore
{
	private readonly IReadOnlyDictionary<string, object?> _snapshot;

	public FakeSnapshotStore(IReadOnlyDictionary<string, object?>? snapshot = null)
	{
		_snapshot = snapshot ?? new Dictionary<string, object?>(StringComparer.Ordinal);
	}

	public IReadOnlyDictionary<string, object?> RestoreSnapshot(string domainId)
	{
		return _snapshot;
	}
}

sealed class RecordingSnapshotSink : IPartnerSnapshotSink
{
	public List<SnapshotCapture> Captures { get; } = [];

	public bool ThrowOnCapture { get; init; }

	public void CaptureSnapshot(string domainId, IReadOnlyDictionary<string, object?> payload)
	{
		if (ThrowOnCapture)
		{
			throw new InvalidOperationException("capture failed");
		}

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
	private readonly HashSet<string> _zones = new(StringComparer.Ordinal);
	private Action<HubDockingState>? _hubStateChanged;
	private Action? _playerReturnedToHub;
	private Action<string>? _playerEnteredZone;

	public HubDockingState CurrentHubState { get; set; } = HubDockingState.Landed;

	public event Action<HubDockingState>? HubStateChanged
	{
		add => _hubStateChanged += value;
		remove => _hubStateChanged -= value;
	}

	public event Action? PlayerReturnedToHub
	{
		add => _playerReturnedToHub += value;
		remove => _playerReturnedToHub -= value;
	}

	public event Action<string>? PlayerEnteredZone
	{
		add => _playerEnteredZone += value;
		remove => _playerEnteredZone -= value;
	}

	public bool IsPlayerInZone(string zoneId)
	{
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
		CurrentHubState = state;
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

	public int QueuedCount => _callbacks.Count;

	public void QueueCall(Action callback)
	{
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

sealed record SnapshotCapture(
	string DomainId,
	IReadOnlyDictionary<string, object?> Payload);
