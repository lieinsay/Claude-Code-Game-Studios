using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 006: Life Trace Anchors ===");
var failed = 0;
var total = 0;

Run("AC-1/2: hub exposes four trace anchors with metadata", AcDefinitions);
Run("AC-3..6: chart notes tier follows intel routes and emits changes", AcChartNotes);
Run("AC-7..9: storage fullness tier follows capacity ratio", AcStorage);
Run("AC-10..14: nest accumulation follows partner query only when crewed", AcNest);
Run("AC-15..19: hull repairs derive from repair source and snapshot tiers restore as authority", AcHullAndPersistence);

if (failed > 0)
{
	Console.Error.WriteLine($"LifeTraceAnchors failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"LifeTraceAnchors passed: {total}/{total} checks passed.");
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
		Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
		failed++;
		return;
	}

	Console.Error.WriteLine($"[FAIL] {label}");
	failed++;
}

static bool AcDefinitions()
{
	var hub = new HubManager();
	return hub.TraceAnchors.Count == 4
		&& HubIds.TraceIds.All(id => hub.TraceAnchors.ContainsKey(id))
		&& hub.TraceAnchors.Values.All(anchor => anchor.TraceId.Length > 0
			&& anchor.CurrentTier >= 0
			&& anchor.MaxTier >= anchor.CurrentTier
			&& anchor.DisplayName.Length > 0
			&& anchor.DataSource.Length > 0);
}

static bool AcChartNotes()
{
	var routes = 0;
	var hub = new HubManager(queries: new HubDomainQueries { KnownRouteCount = () => routes });
	hub.GetStation(HubIds.IntelDesk).HandleUse("player");
	hub.GetStation(HubIds.IntelDesk).Release();
	var changed = false;
	hub.TraceAnchorChanged += (id, oldTier, newTier) => changed |= id == HubIds.TraceChartNotes && oldTier == 0 && newTier == 1;
	routes = 2;
	hub.RefreshTraceAnchors();
	var tier1 = hub.TraceAnchors[HubIds.TraceChartNotes].CurrentTier;
	routes = 4;
	hub.RefreshTraceAnchors();
	return changed && tier1 == 1 && hub.TraceAnchors[HubIds.TraceChartNotes].CurrentTier == 2;
}

static bool AcStorage()
{
	var used = 0;
	var hub = new HubManager(queries: new HubDomainQueries
	{
		StorageUsedVolume = () => used,
		StorageTotalCapacity = () => 100,
	});
	used = 33;
	hub.RefreshTraceAnchors();
	var tier0 = hub.TraceAnchors[HubIds.TraceStorageFullness].CurrentTier;
	used = 50;
	hub.RefreshTraceAnchors();
	var tier1 = hub.TraceAnchors[HubIds.TraceStorageFullness].CurrentTier;
	used = 76;
	hub.RefreshTraceAnchors();
	return tier0 == 0 && tier1 == 1 && hub.TraceAnchors[HubIds.TraceStorageFullness].CurrentTier == 2;
}

static bool AcNest()
{
	var crewed = false;
	var nest = 3;
	var hub = new HubManager(queries: new HubDomainQueries
	{
		PartnerInCrew = () => crewed,
		PartnerNestState = () => nest,
	});
	hub.RefreshTraceAnchors();
	var noCrew = hub.TraceAnchors[HubIds.TraceNestAccumulation].CurrentTier;
	crewed = true;
	hub.RefreshTraceAnchors();
	return noCrew == 0 && hub.TraceAnchors[HubIds.TraceNestAccumulation].CurrentTier == 3;
}

static bool AcHullAndPersistence()
{
	var repairs = 0;
	var recent = false;
	var hub = new HubManager(queries: new HubDomainQueries
	{
		CompletedRepairCount = () => repairs,
		RepairSinceLastDeparture = () => recent,
	});
	repairs = 2;
	hub.RefreshTraceAnchors();
	var tier1 = hub.TraceAnchors[HubIds.TraceHullRepairs].CurrentTier;
	repairs = 3;
	recent = true;
	hub.RefreshTraceAnchors();
	var snapshot = hub.BuildProgressAirshipSnapshot();
	var restored = new HubManager(queries: new HubDomainQueries { CompletedRepairCount = () => 0 });
	restored.RestoreFromProgressAirship(snapshot);
	return tier1 == 1
		&& hub.TraceAnchors[HubIds.TraceHullRepairs].CurrentTier == 2
		&& restored.TraceAnchors[HubIds.TraceHullRepairs].CurrentTier == 2;
}
