using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #13 Story 006: Edge Cases, MVP Visual/Audio & Defensive Handling ===");

var failed = 0;
var total = 0;

Run("AC-1: excess quantity is rejected and selector max floors at zero", Ac1ExcessQuantity);
Run("AC-2: repaired nodes hide interaction and reject direct deposits", Ac2AlreadyRepairedGuard);
Run("AC-3: repaired arrival is idempotent and does not emit visual again", Ac3RepairedArrivalNoRepeatVisual);
Run("AC-4: wrong location hides #11 repair interaction handoff", Ac4WrongPositionGatesInteraction);
Run("AC-5: physical arrival without intel reveals node but hides details", Ac5PhysicalArrivalWithoutIntel);
Run("AC-6: later intel refresh reveals material and unlock labels", Ac6IntelRefreshRevealsDetails);
Run("AC-7: repaired terminal state wins over knowledge regression", Ac7KnowledgeRegressionKeepsRepaired);
Run("AC-8: commit failure is atomic and returns player message", Ac8CommitFailureAtomic);
Run("AC-9: leaving mid-repair preserves deposited counters", Ac9LeaveAndReturnPreservesCounters);
Run("AC-10: mid-batch save/load preserves progress and remains continuable", Ac10MidBatchSaveLoad);
Run("AC-11: hazard reduction never goes below zero", Ac11HazardFloor);
Run("AC-12: new game resets nodes to unrevealed empty progress", Ac12NewGameReset);
Run("AC-13: final material commit triggers ceremony without extra inventory events", Ac13BagSlotEmptyIndependentCeremony);
Run("AC-14: ceremony advances only by supplied delta and completes cleanly", Ac14DeltaBasedCeremony);
Run("AC-15: known visual is broken/dim with no repaired effects", Ac15KnownVisualContract);
Run("AC-16: repaired visual exposes glow, breathing, beam, and particles", Ac16RepairedVisualContract);
Run("AC-17: ceremony duration settles at repaired visual while UI can close", Ac17CeremonyDurationAndUiClose);
Run("AC-18: each successful submit records short deposit confirm audio", Ac18DepositConfirmAudio);
Run("AC-19: final submit records 2-3s ceremony audio", Ac19CeremonyAudio);
Run("AC-20: empty node id validates as invalid_node", Ac20EmptyNodeInvalid);
Run("AC-21: untyped non-integer and negative quantities are defensive", Ac21UntypedQuantityDefensive);
Run("AC-22: malformed required_resources behaves as empty requirements", Ac22MalformedRequirements);
Run("AC-23: negative ceremony duration clamps to minimum", Ac23NegativeDurationClamp);

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

static WorldRepair MakeRepair(bool arrive = true)
{
    var registry = new Registry();
    registry.InitializeContent();
    var repair = new WorldRepair(registry);
    repair.Initialize();
    if (arrive)
    {
        repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    }

    repair.SetCommitDepositHandler((_, offer) => ResourceOperationResult.Ok(offer.Values.Sum()));
    return repair;
}

static RepairDepositResult Complete(WorldRepair repair)
{
    return repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });
}

static bool Ac1ExcessQuantity()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 4 });
    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    return result.Violations.Contains(RepairDepositViolation.ExcessQuantity)
        && repair.GetMaxOfferQuantity(WorldRepair.MvpNodeId, "resource.repair_kit") == 0
        && repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 4;
}

static bool Ac2AlreadyRepairedGuard()
{
    var repair = MakeRepair();
    Complete(repair);
    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    return !repair.IsRepairInteractionAvailable(WorldRepair.MvpNodeId)
        && result.Result == RepairSubmitResult.ErrValidationFailed
        && result.Violations.Contains(RepairDepositViolation.AlreadyRepaired);
}

static bool Ac3RepairedArrivalNoRepeatVisual()
{
    var repair = MakeRepair();
    var visualCount = 0;
    repair.VisualStateChanged += (_, _) => visualCount++;
    Complete(repair);
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired
        && visualCount == 1;
}

static bool Ac4WrongPositionGatesInteraction()
{
    var repair = MakeRepair();
    return !repair.IsRepairInteractionAvailableAtLocation(WorldRepair.MvpNodeId, "location.somewhere-else")
        && repair.IsRepairInteractionAvailableAtLocation(WorldRepair.MvpNodeId, "location.glass-harbor-outskirts");
}

static bool Ac5PhysicalArrivalWithoutIntel()
{
    var repair = MakeRepair(arrive: false);
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    var info = repair.GetRepairInteractionInfo(WorldRepair.MvpNodeId, intelIdentified: false);
    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known
        && info.InteractionAvailable
        && info.MaterialLabels.Values.All(label => label == "?")
        && info.UnlockPreview == "unknown_effect";
}

static bool Ac6IntelRefreshRevealsDetails()
{
    var repair = MakeRepair();
    var info = repair.GetRepairInteractionInfo(WorldRepair.MvpNodeId, intelIdentified: true);
    return info.MaterialLabels["resource.repair_kit"] == "resource.repair_kit:4"
        && info.UnlockPreview.Contains("route.sky-reef-arc-01", StringComparison.Ordinal);
}

static bool Ac7KnowledgeRegressionKeepsRepaired()
{
    var repair = MakeRepair();
    Complete(repair);
    repair.OnIntelRevealedRepairNode(WorldRepair.MvpNodeId);
    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac8CommitFailureAtomic()
{
    var repair = MakeRepair();
    var progressEvents = 0;
    repair.RepairProgressChanged += (_, _, _) => progressEvents++;
    repair.SetCommitDepositHandler((_, _) => ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient));
    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    return result.Result == RepairSubmitResult.ErrCommitFailed
        && repair.GetDeposited(WorldRepair.MvpNodeId).Count == 0
        && progressEvents == 0
        && WorldRepair.GetSubmitFailureMessage(result.Result) == WorldRepair.CommitFailedPlayerMessage;
}

static bool Ac9LeaveAndReturnPreservesCounters()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });
    var before = repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"];
    var after = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    return before == 3
        && after.Result == RepairSubmitResult.Success
        && repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 4;
}

static bool Ac10MidBatchSaveLoad()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 2 });
    var snapshot = repair.BuildSnapshotPackage();

    var restored = MakeRepair();
    restored.RestoreFromSnapshotPackage(snapshot);
    restored.SetCommitDepositHandler((_, offer) => ResourceOperationResult.Ok(offer.Values.Sum()));
    var continued = restored.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 2 });

    return restored.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 4
        && Math.Abs(restored.GetRepairProgress(WorldRepair.MvpNodeId) - 0.5d) < 0.000001d
        && restored.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known
        && continued.Result == RepairSubmitResult.Success;
}

static bool Ac11HazardFloor()
{
    return Math.Abs(WorldRepair.ApplyHazardReduction(0.1d, 0.3d) - 0.07d) < 0.000001d
        && WorldRepair.ApplyHazardReduction(0.0d, 0.3d) == 0.0d;
}

static bool Ac12NewGameReset()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    repair.InitNewGameState();
    var snapshot = repair.GetNodeSnapshot(WorldRepair.MvpNodeId);
    return snapshot is not null
        && snapshot.RepairState == RepairState.Unrevealed
        && snapshot.Deposited.Count == 0
        && snapshot.RepairProgress == 0.0d;
}

static bool Ac13BagSlotEmptyIndependentCeremony()
{
    var repair = MakeRepair();
    var depositEvents = 0;
    repair.DepositCommitted += (_, _, _) => depositEvents++;
    var result = Complete(repair);
    var visual = repair.GetVisualSnapshot(WorldRepair.MvpNodeId);
    return result.Completed
        && visual.CeremonyActive
        && depositEvents == 2;
}

static bool Ac14DeltaBasedCeremony()
{
    var repair = MakeRepair();
    Complete(repair);
    repair.TickCeremony(2.0d);
    var during = repair.GetVisualSnapshot(WorldRepair.MvpNodeId);
    repair.TickCeremony(0.0d);
    repair.TickCeremony(3.0d);
    var done = repair.GetVisualSnapshot(WorldRepair.MvpNodeId);
    return during.CeremonyActive
        && Math.Abs(during.CeremonyElapsedSec - 2.0d) < 0.000001d
        && !done.CeremonyActive
        && Math.Abs(done.CeremonyElapsedSec - 5.0d) < 0.000001d;
}

static bool Ac15KnownVisualContract()
{
    var visual = MakeRepair().GetVisualSnapshot(WorldRepair.MvpNodeId);
    return visual.SpriteState == WorldRepair.VisualStateKnown
        && !visual.HaloVisible
        && !visual.BeamVisible
        && visual.ParticleCount == 0;
}

static bool Ac16RepairedVisualContract()
{
    var repair = MakeRepair();
    Complete(repair);
    repair.TickCeremony(0.75d);
    var visual = repair.GetVisualSnapshot(WorldRepair.MvpNodeId);
    return visual.SpriteState == WorldRepair.VisualStateRepaired
        && visual.HaloVisible
        && visual.BeamVisible
        && visual.BeamColorRgba == WorldRepair.RepairedBeamColorRgba
        && visual.ParticleCount is >= 6 and <= 8
        && visual.ParticleSpawnRadiusPx == 48.0d
        && visual.ParticleMinLifetimeSec == 2.0d
        && visual.ParticleMaxLifetimeSec == 4.0d
        && visual.ModulateAlpha is >= 0.9d and <= 1.0d;
}

static bool Ac17CeremonyDurationAndUiClose()
{
    var repair = MakeRepair();
    Complete(repair);
    repair.TickCeremony(WorldRepair.DefaultRepairCeremonyDurationSec);
    var visual = repair.GetVisualSnapshot(WorldRepair.MvpNodeId);
    return !visual.CeremonyActive
        && Math.Abs(visual.CeremonyDurationSec - 5.0d) < 0.000001d
        && visual.SpriteState == WorldRepair.VisualStateRepaired
        && visual.UiCloseInteractable;
}

static bool Ac18DepositConfirmAudio()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    var confirms = repair.AudioCues.Where(cue => cue.CueId == "repair.deposit_confirm").ToArray();
    return confirms.Length == 2 && confirms.All(cue => cue.DurationSec < 0.5d);
}

static bool Ac19CeremonyAudio()
{
    var repair = MakeRepair();
    Complete(repair);
    var cue = repair.AudioCues.Single(cue => cue.CueId == "repair.ceremony_hum_chime");
    return cue.DurationSec is >= 2.0d and <= 3.0d
        && cue.Description.Contains("chime", StringComparison.Ordinal);
}

static bool Ac20EmptyNodeInvalid()
{
    var result = MakeRepair().ValidateDeposit("", new Dictionary<string, int> { ["resource.repair_kit"] = 1 });
    return result.Violations.Contains(RepairDepositViolation.InvalidNode);
}

static bool Ac21UntypedQuantityDefensive()
{
    var repair = MakeRepair();
    var negative = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, object?>
    {
        ["resource.repair_kit"] = -0.2d,
    });
    var fractional = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, object?>
    {
        ["resource.repair_kit"] = 1.8d,
    });
    return negative.Violations.Contains(RepairDepositViolation.EmptyOffer)
        && fractional.Valid;
}

static bool Ac22MalformedRequirements()
{
    var repair = new WorldRepair();
    repair.RegisterRepairNodeDefinition(new RepairNodeDefinition(
        "repair_node.malformed",
        "Malformed",
        "location.test",
        new Dictionary<string, int>(),
        Array.Empty<string>(),
        "",
        0.0d,
        true,
        ""));
    return repair.RepairProgress("repair_node.malformed") == 0.0d
        && !repair.RepairCompletion("repair_node.malformed");
}

static bool Ac23NegativeDurationClamp()
{
    var repair = MakeRepair();
    repair.SetRepairCeremonyDurationSec(-10.0d);
    Complete(repair);
    repair.TickCeremony(0.5d);
    var visual = repair.GetVisualSnapshot(WorldRepair.MvpNodeId);
    return Math.Abs(visual.CeremonyDurationSec - WorldRepair.MinRepairCeremonyDurationSec) < 0.000001d
        && !visual.CeremonyActive;
}
