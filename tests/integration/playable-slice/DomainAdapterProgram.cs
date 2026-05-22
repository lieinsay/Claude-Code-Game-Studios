using CloudWeaverVoyage.Presentation;

var total = 0;
var failed = 0;

void Check(bool condition, string label)
{
	total++;
	if (condition)
	{
		Console.WriteLine($"PASS {label}");
		return;
	}

	failed++;
	Console.Error.WriteLine($"FAIL {label}");
}

var adapter = new PlayableSliceDomainAdapter();
adapter.OpenChart();
var opened = adapter.Snapshot;
Check(opened.ChartState == "Browsing", "adapter opens ChartManager into Browsing");
Check(opened.VisibleRouteCount >= 2, "adapter exposes seeded visible routes");

Check(adapter.SelectRoute("route.mist"), "adapter selects mist route through ChartManager");
var selected = adapter.Snapshot;
Check(selected.ChartState == "RouteSelected", "ChartManager state is RouteSelected");
Check(selected.SelectedRouteId == "route.mist", "selected route comes from ChartManager");
Check(selected.SelectedRouteName == "雾海短程", "selected route display name is mapped for Godot UI");

Check(adapter.ConfirmDeparture(), "adapter confirms departure through ChartManager and HubManager");
var departed = adapter.Snapshot;
Check(departed.CommittedRouteId == "route.mist", "ChartManager committed route is recorded");
Check(departed.HubLastRoute == "route.mist", "HubManager records chart departure route");
Check(departed.HubDockingState == "InTransit", "HubManager enters InTransit");
Check(departed.NavigationState == "Arrived", "NavigationManager completes the playable route");
Check(departed.EncounterDestinationId == "location.mist-short", "NavigationManager produces EncounterContext destination");
Check(departed.EncounterResult == "arrived", "NavigationManager produces arrived EncounterContext");
Check(departed.ExplorationPhase == "Exploring", "ExplorationManager consumes EncounterContext into Exploring");
Check(departed.ExplorationPointId == "location.mist-short", "ExplorationManager owns the active exploration point");

adapter.AdvanceExploration();
var searched = adapter.Snapshot;
Check(searched.ExplorationStep == 1, "adapter advances hardened exploration contract to step 1");
Check(searched.LastSearchPointId == "sp.playable.1", "ExplorationManager records the first runtime search point");
Check(searched.BasicSupplyInStorage == 9, "ResourcesManager consumes basic supply on first search");
Check(searched.RewardCarried == 1, "ResourcesManager carries first search reward");
Check(searched.CargoUsed == 80, "adapter exposes cargo pressure for step 1");
Check(searched.ThreatText == "低威胁", "adapter exposes threat text for step 1");

adapter.AdvanceExploration();
var damaged = adapter.Snapshot;
Check(damaged.ExplorationStep == 2, "adapter advances hardened exploration contract to step 2");
Check(damaged.LastSearchPointId == "sp.playable.2", "ExplorationManager records the second runtime search point");
Check(damaged.BasicSupplyInStorage == 8, "ResourcesManager consumes second basic supply");
Check(damaged.RewardCarried == 2, "ResourcesManager carries second search reward");
Check(damaged.HullIntegrity == 94, "ModuleHullManager applies hull pressure for step 2");
Check(damaged.ExplorationSubstate == "Threatened", "ExplorationManager owns the runtime threat substate");

var save = adapter.SaveSceneState(new PlayableSliceSceneState("exploration", "route.mist", 2, 592, 594, "saved from test"));
var saved = adapter.Snapshot;
Check(save.Success, "Persistence saves playable slice progress");
Check(saved.PersistenceGeneration == 1, "Persistence records generation after save");

adapter.ReturnToHub();
var returned = adapter.Snapshot;
Check(returned.HubDockingState == "Landed", "HubManager returns to Landed after arrival");
Check(returned.RewardCarried == 0, "ResourcesManager clears carried rewards on Hub return");
Check(returned.RewardInStorage == 2, "ResourcesManager extracts carried rewards to storage on Hub return");

var load = adapter.LoadSceneState();
var loaded = adapter.Snapshot;
Check(load.Result.Success, "Persistence loads playable slice progress");
Check(load.State.Screen == "exploration", "Persistence restores playable slice screen");
Check(loaded.ExplorationStep == 2, "Persistence restores playable slice exploration step");
Check(loaded.ExplorationPhase == "Exploring", "Persistence restores ExplorationManager active session");
Check(loaded.LastSearchPointId == "sp.playable.2", "Persistence restores last ExplorationManager search point");
Check(loaded.RewardCarried == 2, "Persistence restores ResourcesManager carried rewards");
Check(loaded.RewardInStorage == 0, "Persistence restores ResourcesManager storage before Hub return");
Check(loaded.HullIntegrity == 94, "Persistence restores ModuleHullManager hull damage");

Console.WriteLine($"RESULT {total - failed}/{total} passing");
return failed == 0 ? 0 : 1;
