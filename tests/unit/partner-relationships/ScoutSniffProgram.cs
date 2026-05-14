using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #15 Story 002: Scout Sniff Algorithm & Confidence Clamp ===");
var failed = 0;
var total = 0;

Run("AC-1: sniffing state gate returns cat_busy without mutation", Ac1SniffingGateReturnsBusy);
Run("AC-2: idle and bench states pass the sniffing gate", Ac2AllowedStatesPassGate);
Run("AC-3: already sniffed item returns already smelled without side effects", Ac3AlreadySniffedShortCircuits);
Run("AC-4: unsniffed item proceeds to signature lookup", Ac4UnsniffedItemQueriesSignature);
Run("AC-5: null signature returns confused without Intel calls", Ac5NullSignatureConfused);
Run("AC-6: empty reveal target returns confused without Intel calls", Ac6EmptyRevealTargetConfused);
Run("AC-7: confidence clamp enforces min(raw, 66)", Ac7ConfidenceClampCases);
Run("AC-8: missing confidence defaults to zero", Ac8MissingConfidenceDefaultsZero);
Run("AC-9: successful sniff reveals rumor with clamped parameters", Ac9RevealRumorCalledOnce);
Run("AC-10: empty pattern id skips observation report", Ac10EmptyPatternSkipsObservation);
Run("AC-11: nonempty pattern id reports observation once", Ac11PatternReportsObservation);
Run("AC-12: successful sniff records item and success flag", Ac12SuccessMutatesSniffState);
Run("AC-13: success flag remains true after later successes", Ac13SuccessFlagRemainsTrue);
Run("AC-14: successful sniff does not consume inventory", Ac14InventoryNotConsumed);
Run("AC-15: confidence at least 50 selects circles reaction", Ac15StrongSignalReaction);
Run("AC-16: confidence below 50 selects rubs-face reaction", Ac16WeakSignalReaction);
Run("AC-17: sniffable filter includes only signed inventory items", Ac17SniffableFilter);
Run("AC-18: empty inventory returns no sniffable items", Ac18EmptyInventory);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 002 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 002 validation passed: {total}/{total} checks passed.");
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

static bool Ac1SniffingGateReturnsBusy()
{
	var content = new FakeContentLookup()
		.Add("item.first", ItemWithSignature("item.first", confidence: 65))
		.Add("item.second", ItemWithSignature("item.second", confidence: 65));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var first = partner.ScoutSniff("item.first");
	var second = partner.ScoutSniff("item.second");
	var state = partner.Partners[PartnerManager.MvpPartnerId];

	return first.Success
		&& !second.Success
		&& second.ReactionId == -1
		&& second.Error == "cat_busy"
		&& !state.SniffedItems.Contains("item.second", StringComparer.Ordinal)
		&& intel.RevealCalls.Count == 1
		&& intel.ReportCalls.Count == 1;
}

static bool Ac2AllowedStatesPassGate()
{
	var idleContent = new FakeContentLookup()
		.Add("item.none", ItemWithoutSignature("item.none"));
	var idlePartner = NewPartner(idleContent);
	MoveToLivingIdle(idlePartner);
	var idleResult = idlePartner.ScoutSniff("item.none");

	var benchContent = new FakeContentLookup()
		.Add("item.none", ItemWithoutSignature("item.none"));
	var benchPartner = NewPartner(benchContent);
	MoveToBenchAdjacent(benchPartner);
	var benchResult = benchPartner.ScoutSniff("item.none");

	return idleResult.Error == "no_signature"
		&& benchResult.Error == "no_signature"
		&& idleContent.QueriedIds.Count == 1
		&& benchContent.QueriedIds.Count == 1;
}

static bool Ac3AlreadySniffedShortCircuits()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature("item.valid", confidence: 65));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var first = partner.ScoutSniff("item.valid");
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	var second = partner.ScoutSniff("item.valid");
	var state = partner.Partners[PartnerManager.MvpPartnerId];

	return first.Success
		&& !second.Success
		&& second.ReactionId == (int)ScoutSniffReaction.AlreadySmelled
		&& second.Error == "already_sniffed"
		&& state.SniffedItems.Count == 1
		&& intel.RevealCalls.Count == 1
		&& intel.ReportCalls.Count == 1;
}

static bool Ac4UnsniffedItemQueriesSignature()
{
	var content = new FakeContentLookup()
		.Add("item.unknown", ItemWithoutSignature("item.unknown"));
	var partner = NewPartner(content);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.unknown");

	return !result.Success
		&& result.Error == "no_signature"
		&& content.QueriedIds.SequenceEqual(["item.unknown"]);
}

static bool Ac5NullSignatureConfused()
{
	var content = new FakeContentLookup()
		.Add("item.null", ItemWithRawSignature("item.null", null));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.null");
	var state = partner.Partners[PartnerManager.MvpPartnerId];

	return !result.Success
		&& result.ReactionId == (int)ScoutSniffReaction.Confused
		&& intel.RevealCalls.Count == 0
		&& intel.ReportCalls.Count == 0
		&& !state.SniffedItems.Contains("item.null", StringComparer.Ordinal);
}

static bool Ac6EmptyRevealTargetConfused()
{
	var content = new FakeContentLookup()
		.Add("item.empty", ItemWithSignature("item.empty", revealTarget: string.Empty))
		.Add("item.null-target", ItemWithSignature("item.null-target", revealTarget: null));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var empty = partner.ScoutSniff("item.empty");
	var nullTarget = partner.ScoutSniff("item.null-target");
	var state = partner.Partners[PartnerManager.MvpPartnerId];

	return !empty.Success
		&& empty.ReactionId == (int)ScoutSniffReaction.Confused
		&& !nullTarget.Success
		&& nullTarget.ReactionId == (int)ScoutSniffReaction.Confused
		&& intel.RevealCalls.Count == 0
		&& intel.ReportCalls.Count == 0
		&& state.SniffedItems.Count == 0;
}

static bool Ac7ConfidenceClampCases()
{
	return PartnerManager.ClampConfidence(0) == 0
		&& PartnerManager.ClampConfidence(30) == 30
		&& PartnerManager.ClampConfidence(66) == 66
		&& PartnerManager.ClampConfidence(67) == 66
		&& PartnerManager.ClampConfidence(90) == 66
		&& PartnerManager.ClampConfidence(100) == 66;
}

static bool Ac8MissingConfidenceDefaultsZero()
{
	var content = new FakeContentLookup()
		.Add("item.no-confidence", ItemWithSignature("item.no-confidence", includeConfidence: false));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.no-confidence");

	return result.Success
		&& result.ReactionId == (int)ScoutSniffReaction.RubsFace
		&& intel.RevealCalls.Count == 1
		&& intel.RevealCalls[0].Confidence == 0;
}

static bool Ac9RevealRumorCalledOnce()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature(
			"item.valid",
			revealTarget: "location.glass-harbor",
			hazardHint: "mist",
			confidence: 90,
			patternId: "pattern.ancient-optics"));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");
	var call = intel.RevealCalls.SingleOrDefault();

	return result.Success
		&& call is not null
		&& call.RevealTarget == "location.glass-harbor"
		&& call.SourceTag == PartnerManager.MvpPartnerId
		&& call.HazardTags.SequenceEqual(["mist"])
		&& call.Confidence == 66;
}

static bool Ac10EmptyPatternSkipsObservation()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature("item.valid", patternId: string.Empty));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");

	return result.Success
		&& intel.RevealCalls.Count == 1
		&& intel.ReportCalls.Count == 0;
}

static bool Ac11PatternReportsObservation()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature("item.valid", patternId: "pattern.ancient-optics"));
	var intel = new RecordingIntelSink();
	var partner = NewPartner(content, new FakeInventorySource(), intel);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");
	var call = intel.ReportCalls.SingleOrDefault();

	return result.Success
		&& call is not null
		&& call.PatternId == "pattern.ancient-optics"
		&& call.EventId == PartnerManager.PartnerSniffSuccessEventId;
}

static bool Ac12SuccessMutatesSniffState()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature("item.valid"));
	var partner = NewPartner(content);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.valid");
	var state = partner.Partners[PartnerManager.MvpPartnerId];

	return result.Success
		&& state.SniffedItems.SequenceEqual(["item.valid"])
		&& state.SniffSuccessOccurred;
}

static bool Ac13SuccessFlagRemainsTrue()
{
	var content = new FakeContentLookup()
		.Add("item.first", ItemWithSignature("item.first"))
		.Add("item.second", ItemWithSignature("item.second"));
	var partner = NewPartner(content);
	MoveToLivingIdle(partner);

	var first = partner.ScoutSniff("item.first");
	var wasTrue = partner.Partners[PartnerManager.MvpPartnerId].SniffSuccessOccurred;
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	var second = partner.ScoutSniff("item.second");
	var state = partner.Partners[PartnerManager.MvpPartnerId];

	return first.Success
		&& second.Success
		&& wasTrue
		&& state.SniffSuccessOccurred
		&& state.SniffedItems.Count == 2;
}

static bool Ac14InventoryNotConsumed()
{
	var content = new FakeContentLookup()
		.Add("item.valid", ItemWithSignature("item.valid"));
	var inventory = new FakeInventorySource("item.valid");
	var partner = NewPartner(content, inventory);
	MoveToLivingIdle(partner);

	var before = inventory.GetInventoryItems();
	var result = partner.ScoutSniff("item.valid");
	var after = inventory.GetInventoryItems();

	return result.Success
		&& before.SequenceEqual(after)
		&& after.SequenceEqual(["item.valid"]);
}

static bool Ac15StrongSignalReaction()
{
	var content = new FakeContentLookup()
		.Add("item.strong", ItemWithSignature("item.strong", confidence: 50));
	var partner = NewPartner(content);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.strong");

	return result.Success
		&& result.ReactionId == (int)ScoutSniffReaction.CirclesTwice;
}

static bool Ac16WeakSignalReaction()
{
	var content = new FakeContentLookup()
		.Add("item.weak", ItemWithSignature("item.weak", confidence: 30));
	var partner = NewPartner(content);
	MoveToLivingIdle(partner);

	var result = partner.ScoutSniff("item.weak");

	return result.Success
		&& result.ReactionId == (int)ScoutSniffReaction.RubsFace;
}

static bool Ac17SniffableFilter()
{
	var content = new FakeContentLookup()
		.Add("item.a", ItemWithSignature("item.a"))
		.Add("item.b", ItemWithoutSignature("item.b"))
		.Add("item.c", ItemWithSignature("item.c"));
	var inventory = new FakeInventorySource("item.a", "item.b", "item.c");
	var partner = NewPartner(content, inventory);

	var sniffable = partner.GetSniffableItems();

	return sniffable.SequenceEqual(["item.a", "item.c"])
		&& inventory.GetInventoryItems().SequenceEqual(["item.a", "item.b", "item.c"]);
}

static bool Ac18EmptyInventory()
{
	var partner = NewPartner(new FakeContentLookup(), new FakeInventorySource());

	return partner.GetSniffableItems().Count == 0;
}

static PartnerManager NewPartner(
	FakeContentLookup content,
	FakeInventorySource? inventory = null,
	RecordingIntelSink? intel = null)
{
	var partner = new PartnerManager(
		content,
		inventory ?? new FakeInventorySource(),
		intel ?? new RecordingIntelSink());
	partner.Initialize(HubDockingState.Landed);
	return partner;
}

static void MoveToLivingIdle(PartnerManager partner)
{
	partner.OnPlayerEnteredZone("living_quarters");
	partner.AdvanceTime(PartnerManager.CatStateCooldownSeconds);
}

static void MoveToBenchAdjacent(PartnerManager partner)
{
	MoveToLivingIdle(partner);
	partner.OnPlayerEnteredZone("workbench");
	partner.OnCatReachedBench();
}

static Dictionary<string, object?> ItemWithoutSignature(string id)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = id,
		["kind"] = "resource",
	};
}

static Dictionary<string, object?> ItemWithRawSignature(string id, object? signature)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = id,
		["kind"] = "resource",
		["cat_sniff_signature"] = signature,
	};
}

static Dictionary<string, object?> ItemWithSignature(
	string id,
	string? revealTarget = "location.glass-harbor",
	string hazardHint = "old-harbor-chain",
	int confidence = 65,
	string patternId = "pattern.ancient-optics",
	bool includeConfidence = true)
{
	var signature = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["reveal_target"] = revealTarget,
		["hazard_hint"] = hazardHint,
		["pattern_id"] = patternId,
	};

	if (includeConfidence)
	{
		signature["confidence"] = confidence;
	}

	return ItemWithRawSignature(id, signature);
}

sealed class FakeContentLookup : IPartnerContentLookup
{
	private readonly Dictionary<string, IReadOnlyDictionary<string, object?>> _entities = new(StringComparer.Ordinal);

	public List<string> QueriedIds { get; } = new();

	public FakeContentLookup Add(string itemId, IReadOnlyDictionary<string, object?> entity)
	{
		_entities[itemId] = entity;
		return this;
	}

	public IReadOnlyDictionary<string, object?>? QueryEntity(string itemId)
	{
		QueriedIds.Add(itemId);
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
	public List<RevealRumorCall> RevealCalls { get; } = new();

	public List<ReportObservationCall> ReportCalls { get; } = new();

	public void RevealRumor(string revealTarget, string sourceTag, IReadOnlyList<string> hazardTags, int confidence)
	{
		RevealCalls.Add(new RevealRumorCall(revealTarget, sourceTag, hazardTags.ToArray(), confidence));
	}

	public void ReportObservationEvent(string patternId, string eventId)
	{
		ReportCalls.Add(new ReportObservationCall(patternId, eventId));
	}

	public void OnPartnerJoined(string partnerId)
	{
	}
}

sealed record RevealRumorCall(
	string RevealTarget,
	string SourceTag,
	IReadOnlyList<string> HazardTags,
	int Confidence);

sealed record ReportObservationCall(string PatternId, string EventId);
