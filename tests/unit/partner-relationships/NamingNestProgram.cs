using System.Reflection;
using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #15 Story 003: Naming System & Nest Accumulation ===");
var failed = 0;
var total = 0;

Run("AC-1: sniff success plus pending state is naming eligible", Ac1SniffSuccessPendingEligible);
Run("AC-2: no sniff success is never naming eligible", Ac2NoSniffSuccessIneligible);
Run("AC-3: prompted state is not eligible again", Ac3PromptedIneligible);
Run("AC-4: completed state is terminal and ineligible", Ac4CompletedIneligible);
Run("AC-5: skip count at three closes naming window", Ac5SkipCountThreeIneligible);
Run("AC-6: new game naming and nest defaults are empty", Ac6NewGameDefaults);
Run("AC-7: first return after sniff prompts naming once", Ac7ReturnPromptsNaming);
Run("AC-8: valid name completes naming and emits completion", Ac8ValidNameCompletes);
Run("AC-9: completed naming rejects rename attempts", Ac9CompletedRejectsRename);
Run("AC-10: empty name rejects without skip mutation", Ac10EmptyNameRejected);
Run("AC-11: skip returns naming to pending for next return", Ac11SkipReturnsPending);
Run("AC-12: third skip locks default name immediately", Ac12ThirdSkipLocksDefault);
Run("AC-13: default-locked name never opens UI again", Ac13DefaultLockedNeverPrompts);
Run("AC-14: long submitted name truncates to eight chars", Ac14LongNameTruncates);
Run("AC-15: saved name remains stable and no rename API exists", Ac15NameStableNoRenamePath);
Run("AC-16: first nest item moves empty to first", Ac16FirstNestItem);
Run("AC-17: second nest item moves to accumulating", Ac17SecondNestItem);
Run("AC-18: third nest item preserves accumulating order", Ac18ThirdNestItem);
Run("AC-19: fourth nest item fills the nest", Ac19FourthNestItem);
Run("AC-20: fifth nest accumulation silently caps", Ac20NestCapacityCaps);
Run("AC-21: nest item order always matches static index list", Ac21NestOrderInvariant);
Run("AC-22: nest item count is monotonic across operations", Ac22NestMonotonic);
Run("AC-23: public API exposes no nest deletion or reorder path", Ac23NoNestDeletionPath);

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

static bool Ac1SniffSuccessPendingEligible()
{
	var partner = PartnerWithSuccessfulSniff();
	var state = SkyCat(partner);

	return state.SniffSuccessOccurred
		&& state.NamingState == PartnerNamingState.Pending
		&& state.NamingSkipCount == 0
		&& partner.IsNamingEligible();
}

static bool Ac2NoSniffSuccessIneligible()
{
	var partner = NewPartner();
	var prompted = partner.OnPlayerReturnedToHub();
	var state = SkyCat(partner);

	return !state.SniffSuccessOccurred
		&& !prompted
		&& !partner.IsNamingEligible()
		&& state.NamingState == PartnerNamingState.Pending;
}

static bool Ac3PromptedIneligible()
{
	var partner = PartnerWithSuccessfulSniff();
	var promptCount = 0;
	partner.NamingPromptTriggered += () => promptCount++;

	var first = partner.OnPlayerReturnedToHub();
	var second = partner.OnPlayerReturnedToHub();
	var state = SkyCat(partner);

	return first
		&& !second
		&& promptCount == 1
		&& state.NamingState == PartnerNamingState.Prompted
		&& !partner.IsNamingEligible();
}

static bool Ac4CompletedIneligible()
{
	var partner = PromptedPartner();
	var result = partner.SubmitPartnerName("小云");

	return result.Accepted
		&& SkyCat(partner).NamingState == PartnerNamingState.Completed
		&& !partner.IsNamingEligible();
}

static bool Ac5SkipCountThreeIneligible()
{
	var partner = PromptedPartnerWithSkipCount(2);
	partner.SkipNaming();
	var state = SkyCat(partner);

	return state.NamingSkipCount == PartnerManager.NamingSkipMax
		&& state.NamingState == PartnerNamingState.Completed
		&& !partner.IsNamingEligible();
}

static bool Ac6NewGameDefaults()
{
	var partner = NewPartner();
	var state = SkyCat(partner);

	return state.NamingState == PartnerNamingState.Pending
		&& state.Name == string.Empty
		&& !state.NamingDone
		&& state.NamingSkipCount == 0
		&& !state.SniffSuccessOccurred
		&& state.NestState == PartnerNestState.Empty
		&& state.NestItems.Count == 0;
}

static bool Ac7ReturnPromptsNaming()
{
	var partner = PartnerWithSuccessfulSniff();
	var prompted = 0;
	partner.NamingPromptTriggered += () => prompted++;

	var result = partner.OnPlayerReturnedToHub();

	return result
		&& prompted == 1
		&& SkyCat(partner).NamingState == PartnerNamingState.Prompted;
}

static bool Ac8ValidNameCompletes()
{
	var partner = PromptedPartner();
	var completedNames = new List<string>();
	partner.NamingCompleted += completedNames.Add;

	var result = partner.SubmitPartnerName("小云");
	var state = SkyCat(partner);
	var promptAfterComplete = partner.OnPlayerReturnedToHub();

	return result.Accepted
		&& result.Error == string.Empty
		&& state.Name == "小云"
		&& state.NamingDone
		&& state.NamingState == PartnerNamingState.Completed
		&& completedNames.SequenceEqual(["小云"])
		&& !promptAfterComplete;
}

static bool Ac9CompletedRejectsRename()
{
	var partner = PromptedPartner();
	partner.SubmitPartnerName("小云");

	var result = partner.SubmitPartnerName("新名");
	var state = SkyCat(partner);

	return !result.Accepted
		&& result.Error == "naming_completed"
		&& state.Name == "小云"
		&& partner.QueryPartnerName() == "小云";
}

static bool Ac10EmptyNameRejected()
{
	var partner = PromptedPartner();
	var empty = partner.SubmitPartnerName("");
	var whitespace = partner.SubmitPartnerName("   ");
	var state = SkyCat(partner);

	return !empty.Accepted
		&& empty.Error == "name_empty"
		&& !whitespace.Accepted
		&& whitespace.Error == "name_empty"
		&& state.NamingSkipCount == 0
		&& state.NamingState == PartnerNamingState.Prompted
		&& !state.NamingDone;
}

static bool Ac11SkipReturnsPending()
{
	var partner = PromptedPartner();
	var skipped = partner.SkipNaming();
	var skipCountAfterSkip = SkyCat(partner).NamingSkipCount;
	var namingStateAfterSkip = SkyCat(partner).NamingState;
	var promptedAgain = partner.OnPlayerReturnedToHub();

	return skipped
		&& skipCountAfterSkip == 1
		&& namingStateAfterSkip == PartnerNamingState.Pending
		&& promptedAgain
		&& SkyCat(partner).NamingState == PartnerNamingState.Prompted;
}

static bool Ac12ThirdSkipLocksDefault()
{
	var partner = PromptedPartnerWithSkipCount(2);
	var promptCount = 0;
	partner.NamingPromptTriggered += () => promptCount++;

	var skipped = partner.SkipNaming();
	var state = SkyCat(partner);

	return skipped
		&& promptCount == 0
		&& state.NamingSkipCount == 3
		&& state.Name == PartnerManager.DefaultPartnerName
		&& state.NamingDone
		&& state.NamingState == PartnerNamingState.Completed;
}

static bool Ac13DefaultLockedNeverPrompts()
{
	var partner = PromptedPartnerWithSkipCount(2);
	partner.SkipNaming();
	var prompted = partner.OnPlayerReturnedToHub();

	return !prompted
		&& SkyCat(partner).NamingState == PartnerNamingState.Completed
		&& partner.QueryPartnerName() == PartnerManager.DefaultPartnerName;
}

static bool Ac14LongNameTruncates()
{
	var partner = PromptedPartner();
	var result = partner.SubmitPartnerName("超长的猫咪名字测试一下");

	return result.Accepted
		&& SkyCat(partner).Name == "超长的猫咪名字测"
		&& SkyCat(partner).Name.Length == PartnerManager.PartnerNameLengthMax;
}

static bool Ac15NameStableNoRenamePath()
{
	var partner = PromptedPartner();
	partner.SubmitPartnerName("小云");
	var saved = partner.BuildProgressPartnerSnapshot();
	var loaded = NewPartner();
	var restored = loaded.RestoreFromProgressPartner(saved);
	var before = loaded.QueryPartnerName();
	var rejected = loaded.SubmitPartnerName("新名");
	var after = loaded.QueryPartnerName();

	var hasRenameApi = typeof(PartnerManager)
		.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		.Any(method => method.Name.Contains("Rename", StringComparison.OrdinalIgnoreCase));

	return restored
		&& before == "小云"
		&& after == "小云"
		&& !rejected.Accepted
		&& rejected.Error == "naming_completed"
		&& !hasRenameApi;
}

static bool Ac16FirstNestItem()
{
	var partner = NewPartner(ContentWithItems("item.0"));
	var events = new List<(PartnerNestState OldState, PartnerNestState NewState)>();
	partner.NestStateChanged += (oldState, newState) => events.Add((oldState, newState));

	MoveToLivingIdle(partner);
	var result = partner.ScoutSniff("item.0");
	var state = SkyCat(partner);

	return result.Success
		&& state.NestItems.SequenceEqual([0])
		&& state.NestState == PartnerNestState.First
		&& PartnerManager.NestItemNames[0] == "旧船帆碎布"
		&& events.SequenceEqual([(PartnerNestState.Empty, PartnerNestState.First)]);
}

static bool Ac17SecondNestItem()
{
	var partner = PartnerWithNestSniffs(2);
	var state = SkyCat(partner);

	return state.NestItems.SequenceEqual([0, 1])
		&& state.NestState == PartnerNestState.Accumulating
		&& PartnerManager.NestItemNames[1] == "锈蚀的测风链环";
}

static bool Ac18ThirdNestItem()
{
	var partner = PartnerWithNestSniffs(3);
	var state = SkyCat(partner);

	return state.NestItems.SequenceEqual([0, 1, 2])
		&& state.NestState == PartnerNestState.Accumulating
		&& PartnerManager.NestItemNames[2] == "玩家绳头";
}

static bool Ac19FourthNestItem()
{
	var partner = PartnerWithNestSniffs(4);
	var state = SkyCat(partner);

	return state.NestItems.SequenceEqual([0, 1, 2, 3])
		&& state.NestState == PartnerNestState.Full
		&& PartnerManager.NestItemNames[3] == "空港徽章残片";
}

static bool Ac20NestCapacityCaps()
{
	var partner = PartnerWithNestSniffs(PartnerManager.NestCapacity);
	var before = partner.QueryNestItems();
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	var result = partner.ScoutSniff("item.4");
	var after = partner.QueryNestItems();

	return result.Success
		&& before.SequenceEqual([0, 1, 2, 3])
		&& after.SequenceEqual([0, 1, 2, 3])
		&& SkyCat(partner).NestState == PartnerNestState.Full;
}

static bool Ac21NestOrderInvariant()
{
	var partner = NewPartner(ContentWithItems("item.0", "item.1", "item.2", "item.3"));
	MoveToLivingIdle(partner);
	for (var i = 1; i <= PartnerManager.NestCapacity; i++)
	{
		var result = partner.ScoutSniff($"item.{i - 1}");
		if (!result.Success)
		{
			return false;
		}

		if (!partner.QueryNestItems().SequenceEqual(Enumerable.Range(0, i)))
		{
			return false;
		}

		partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	}

	return PartnerManager.NestItemNames.SequenceEqual([
		"旧船帆碎布",
		"锈蚀的测风链环",
		"玩家绳头",
		"空港徽章残片",
	]);
}

static bool Ac22NestMonotonic()
{
	var partner = NewPartner(ContentWithItems("item.a", "item.b", "item.c"));
	var counts = new List<int> { partner.QueryNestItems().Count };

	MoveToLivingIdle(partner);
	partner.ScoutSniff("item.a");
	counts.Add(partner.QueryNestItems().Count);
	partner.AdvanceTime(PartnerManager.SniffLockoutSeconds);
	partner.OnPlayerReturnedToHub();
	partner.SkipNaming();
	counts.Add(partner.QueryNestItems().Count);
	partner.OnHubStateChanged(HubDockingState.DepartureLocked);
	partner.OnHubStateChanged(HubDockingState.Arrival);
	counts.Add(partner.QueryNestItems().Count);
	partner.ScoutSniff("item.b");
	counts.Add(partner.QueryNestItems().Count);

	return counts.Zip(counts.Skip(1), (left, right) => right >= left).All(BooleanIdentity)
		&& partner.QueryNestItems().SequenceEqual([0, 1]);
}

static bool Ac23NoNestDeletionPath()
{
	var partner = PartnerWithNestSniffs(1);
	var copy = partner.QueryNestItems();
	if (copy is int[] mutableCopy)
	{
		mutableCopy[0] = 99;
	}

	var forbiddenApi = typeof(PartnerManager)
		.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		.Any(method =>
			method.Name.Contains("ClearNest", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("RemoveNest", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("ResetNest", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("SortNest", StringComparison.OrdinalIgnoreCase)
			|| method.Name.Contains("ReverseNest", StringComparison.OrdinalIgnoreCase)
			|| (method.Name.Contains("AccumulateNest", StringComparison.OrdinalIgnoreCase)
				&& method.IsPublic));

	return !forbiddenApi
		&& partner.QueryNestItems().SequenceEqual([0])
		&& SkyCat(partner).NestItems.SequenceEqual([0]);
}

static bool BooleanIdentity(bool value)
{
	return value;
}

static PartnerManager NewPartner(FakeContentLookup? content = null)
{
	var partner = new PartnerManager(
		content ?? new FakeContentLookup(),
		new FakeInventorySource(),
		new RecordingIntelSink());
	partner.Initialize(HubDockingState.Landed);
	return partner;
}

static PartnerManager PartnerWithSuccessfulSniff(string itemId = "item.valid")
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

static PartnerManager PromptedPartnerWithSkipCount(int skipCount)
{
	var partner = PromptedPartner();
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

static PartnerState SkyCat(PartnerManager partner)
{
	return partner.Partners[PartnerManager.MvpPartnerId];
}

static void MoveToLivingIdle(PartnerManager partner)
{
	partner.OnPlayerEnteredZone("living_quarters");
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

static Dictionary<string, object?> ItemWithSignature(string id)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["id"] = id,
		["kind"] = "resource",
		["cat_sniff_signature"] = new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["reveal_target"] = "location.glass-harbor",
			["hazard_hint"] = "old-harbor-chain",
			["confidence"] = 65,
			["pattern_id"] = "pattern.ancient-optics",
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
	public IReadOnlyList<string> GetInventoryItems()
	{
		return Array.Empty<string>();
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
