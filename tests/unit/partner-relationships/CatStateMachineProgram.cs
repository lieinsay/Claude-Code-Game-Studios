using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #15 Story 001: Cat State Machine & Presence Contract ===");
var failed = 0;
var total = 0;

Run("AC-1: all Hub states report partner present", Ac1AllHubStatesReportPresence);
Run("AC-2: no absent state or absent partner path exists", Ac2NoAbsentStatePath);
Run("AC-3: landed new game initializes sleeping at intel station", Ac3LandedInitializesSleeping);
Run("AC-4: living quarters zone wakes cat into living quarters idle", Ac4LivingQuartersTransition);
Run("AC-5: workbench path follows then becomes bench adjacent", Ac5WorkbenchPath);
Run("AC-6: leaving bench reach limit returns to living quarters idle", Ac6BenchReachLimitReturnsIdle);
Run("AC-7: idle over nest settle threshold enters nest", Ac7IdleSettleEntersNest);
Run("AC-8: living quarters trigger pulls cat out of nest", Ac8LivingTriggerLeavesNest);
Run("AC-9: departure_locked freezes state and blocks zone events", Ac9DepartureLockedFreezesState);
Run("AC-10: in_transit hides and disables cat while preserving presence", Ac10InTransitSimplifiesCat);
Run("AC-11: arrival forces idle living quarters", Ac11ArrivalForcesLivingIdle);
Run("AC-12: zone cooldown blocks rapid transition", Ac12CooldownBlocksRapidTransition);
Run("AC-13: cooldown expiry allows next transition", Ac13CooldownExpiryAllowsTransition);
Run("AC-14: feature init derives state from Hub context", Ac14InitDerivesFromHub);
Run("AC-15: MVP partner dictionary only contains sky-cat", Ac15OnlySkyCatRegistered);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 001 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 001 validation passed: {total}/{total} checks passed.");
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

static bool Ac1AllHubStatesReportPresence()
{
	return Enum.GetValues<HubDockingState>()
		.All(state =>
		{
			var partner = new PartnerManager();
			partner.Initialize(state);
			return partner.QueryPartnerPresent();
		});
}

static bool Ac2NoAbsentStatePath()
{
	var partner = new PartnerManager();
	partner.Initialize(HubDockingState.DepartureLocked);

	return partner.QueryPartnerPresent()
		&& !Enum.GetNames<PartnerCatState>().Any(name => name.Contains("Absent", StringComparison.OrdinalIgnoreCase))
		&& partner.Partners.Count == 1
		&& partner.Partners.ContainsKey(PartnerManager.MvpPartnerId);
}

static bool Ac3LandedInitializesSleeping()
{
	var partner = NewPartner(HubDockingState.Landed);

	return partner.CatState == PartnerCatState.SleepingOnIntelStation
		&& partner.QueryPartnerPresent();
}

static bool Ac4LivingQuartersTransition()
{
	var partner = NewPartner();

	return partner.OnPlayerEnteredZone("living_quarters")
		&& partner.CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac5WorkbenchPath()
{
	var partner = NewPartner();
	partner.OnPlayerEnteredZone("living_quarters");
	partner.AdvanceTime(PartnerManager.CatStateCooldownSeconds);
	var follows = partner.OnPlayerEnteredZone("workbench");
	var followingState = partner.CatState;
	var arrived = partner.OnCatReachedBench();

	return follows
		&& followingState == PartnerCatState.FollowingPlayerToBench
		&& arrived
		&& partner.CatState == PartnerCatState.BenchAdjacent;
}

static bool Ac6BenchReachLimitReturnsIdle()
{
	var partner = BenchAdjacentPartner();

	return partner.OnPlayerLeftBenchReachLimit()
		&& partner.CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac7IdleSettleEntersNest()
{
	var partner = LivingIdlePartner();

	return partner.OnLivingQuartersIdleElapsed(PartnerManager.NestSettleSeconds + 0.1d)
		&& partner.CatState == PartnerCatState.InNest;
}

static bool Ac8LivingTriggerLeavesNest()
{
	var partner = LivingIdlePartner();
	partner.OnLivingQuartersIdleElapsed(PartnerManager.NestSettleSeconds + 0.1d);

	return partner.OnPlayerEnteredZone("living_quarters")
		&& partner.CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac9DepartureLockedFreezesState()
{
	var partner = LivingIdlePartner();
	partner.OnHubStateChanged(HubDockingState.DepartureLocked);
	var before = partner.CatState;

	return partner.IsStateFrozen
		&& !partner.IsCatInteractable
		&& !partner.OnPlayerEnteredZone("workbench")
		&& partner.CatState == before;
}

static bool Ac10InTransitSimplifiesCat()
{
	var partner = BenchAdjacentPartner();
	partner.OnHubStateChanged(HubDockingState.InTransit);

	return partner.CatState == PartnerCatState.IdleLivingQuarters
		&& !partner.IsCatRendered
		&& !partner.IsCatInteractable
		&& partner.QueryPartnerPresent();
}

static bool Ac11ArrivalForcesLivingIdle()
{
	var partner = LivingIdlePartner();
	partner.OnLivingQuartersIdleElapsed(PartnerManager.NestSettleSeconds + 0.1d);
	partner.OnHubStateChanged(HubDockingState.Arrival);

	return partner.CatState == PartnerCatState.IdleLivingQuarters
		&& partner.IsCatRendered
		&& partner.QueryPartnerPresent();
}

static bool Ac12CooldownBlocksRapidTransition()
{
	var partner = NewPartner();
	partner.OnPlayerEnteredZone("living_quarters");

	return partner.CatState == PartnerCatState.IdleLivingQuarters
		&& partner.CatStateCooldownRemaining > 0.0d
		&& !partner.OnPlayerEnteredZone("workbench")
		&& partner.CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac13CooldownExpiryAllowsTransition()
{
	var partner = NewPartner();
	partner.OnPlayerEnteredZone("living_quarters");
	partner.AdvanceTime(PartnerManager.CatStateCooldownSeconds);

	return partner.CatStateCooldownRemaining == 0.0d
		&& partner.OnPlayerEnteredZone("workbench")
		&& partner.CatState == PartnerCatState.FollowingPlayerToBench;
}

static bool Ac14InitDerivesFromHub()
{
	return NewPartner(HubDockingState.Landed).CatState == PartnerCatState.SleepingOnIntelStation
		&& NewPartner(HubDockingState.InTransit).CatState == PartnerCatState.IdleLivingQuarters
		&& NewPartner(HubDockingState.Arrival).CatState == PartnerCatState.IdleLivingQuarters;
}

static bool Ac15OnlySkyCatRegistered()
{
	var partner = NewPartner();

	return partner.Partners.Count == 1
		&& partner.Partners.Keys.Single() == PartnerManager.MvpPartnerId
		&& partner.Partners[PartnerManager.MvpPartnerId].PartnerId == PartnerManager.MvpPartnerId;
}

static PartnerManager NewPartner(HubDockingState state = HubDockingState.Landed)
{
	var partner = new PartnerManager();
	partner.Initialize(state);
	return partner;
}

static PartnerManager LivingIdlePartner()
{
	var partner = NewPartner();
	partner.OnPlayerEnteredZone("living_quarters");
	partner.AdvanceTime(PartnerManager.CatStateCooldownSeconds);
	return partner;
}

static PartnerManager BenchAdjacentPartner()
{
	var partner = LivingIdlePartner();
	partner.OnPlayerEnteredZone("workbench");
	partner.OnCatReachedBench();
	return partner;
}
