using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #13 Story 002: Deposit Validation & Batch Commit ===");

var failed = 0;
var total = 0;

Run("AC-1: invalid node returns invalid_node", Ac1InvalidNode);
Run("AC-2: empty and zero offers return empty_offer", Ac2EmptyOffer);
Run("AC-3: invalid material returns invalid_material", Ac3InvalidMaterial);
Run("AC-4: quantity beyond gap returns excess_quantity", Ac4ExcessQuantity);
Run("AC-5: repaired node returns already_repaired", Ac5AlreadyRepaired);
Run("AC-6: mixed violations are all reported", Ac6MixedViolations);
Run("AC-7: partial deposit commits and keeps node known", Ac7PartialDeposit);
Run("AC-8: second batch completes repair", Ac8SecondBatchCompletes);
Run("AC-9: partially filled gap rejects excess", Ac9PartialGapExcess);
Run("AC-10: single-shot full commit completes repair", Ac10SingleShot);
Run("AC-11: validation failure does not call commit or mutate", Ac11ValidationFailIsAtomic);
Run("AC-12: resource commit failure leaves deposited unchanged", Ac12CommitFailIsAtomic);
Run("AC-13: already satisfied material rejects any more", Ac13SatisfiedMaterialRejects);
Run("AC-14: mixed valid and excess offer rejects whole batch", Ac14MixedValidAndExcessRejects);
Run("AC-15: legacy single commit uses batch validation", Ac15LegacyCommitUsesValidation);

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

static WorldRepair MakeRepair(Func<string, IReadOnlyDictionary<string, int>, ResourceOperationResult>? commit = null)
{
    var registry = new Registry();
    registry.InitializeContent();
    var repair = new WorldRepair(registry);
    repair.Initialize();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    repair.SetCommitDepositHandler(commit ?? ((_, offer) => ResourceOperationResult.Ok(offer.Values.Sum())));
    return repair;
}

static bool Has(DepositValidationResult result, RepairDepositViolation violation)
{
    return result.Violations.Contains(violation);
}

static bool Ac1InvalidNode()
{
    var repair = MakeRepair();

    var result = repair.ValidateDeposit("repair_node.invalid", new Dictionary<string, int> { ["resource.repair_kit"] = 1 });

    return !result.Valid && result.Violations.SequenceEqual([RepairDepositViolation.InvalidNode]);
}

static bool Ac2EmptyOffer()
{
    var repair = MakeRepair();

    var empty = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>());
    var zero = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 0 });

    return !empty.Valid
        && !zero.Valid
        && Has(empty, RepairDepositViolation.EmptyOffer)
        && Has(zero, RepairDepositViolation.EmptyOffer);
}

static bool Ac3InvalidMaterial()
{
    var repair = MakeRepair();

    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.invalid"] = 1 });

    return !result.Valid && Has(result, RepairDepositViolation.InvalidMaterial);
}

static bool Ac4ExcessQuantity()
{
    var repair = MakeRepair();

    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 5 });

    return !result.Valid && Has(result, RepairDepositViolation.ExcessQuantity);
}

static bool Ac5AlreadyRepaired()
{
    var repair = MakeRepair();
    repair.TryTransitionState(WorldRepair.MvpNodeId, RepairState.Repaired);

    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });

    return !result.Valid && Has(result, RepairDepositViolation.AlreadyRepaired);
}

static bool Ac6MixedViolations()
{
    var repair = MakeRepair();

    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.invalid"] = 1,
        ["resource.repair_kit"] = 5,
    });

    return !result.Valid
        && Has(result, RepairDepositViolation.InvalidMaterial)
        && Has(result, RepairDepositViolation.ExcessQuantity);
}

static bool Ac7PartialDeposit()
{
    var commitCalls = 0;
    var repair = MakeRepair((_, offer) =>
    {
        commitCalls++;
        return ResourceOperationResult.Ok(offer.Values.Sum());
    });
    var progressEvents = 0;
    repair.RepairProgressChanged += (_, _, _) => progressEvents++;

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    return result.Result == RepairSubmitResult.Success
        && commitCalls == 1
        && progressEvents == 1
        && result.Deposited["resource.repair_kit"] == 3
        && Math.Abs(result.Progress - 0.375d) < 0.000001d
        && !result.Completed
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known;
}

static bool Ac8SecondBatchCompletes()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 1,
        ["resource.basic_supply"] = 4,
    });

    return result.Result == RepairSubmitResult.Success
        && result.Completed
        && result.Deposited["resource.repair_kit"] == 4
        && result.Deposited["resource.basic_supply"] == 4
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac9PartialGapExcess()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 3,
        ["resource.basic_supply"] = 4,
    });

    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 2 });

    return !result.Valid && Has(result, RepairDepositViolation.ExcessQuantity);
}

static bool Ac10SingleShot()
{
    var repair = MakeRepair();

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });

    return result.Result == RepairSubmitResult.Success
        && result.Completed
        && Math.Abs(result.Progress - 1.0d) < 0.000001d
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac11ValidationFailIsAtomic()
{
    var commitCalls = 0;
    var repair = MakeRepair((_, offer) =>
    {
        commitCalls++;
        return ResourceOperationResult.Ok(offer.Values.Sum());
    });
    var progressEvents = 0;
    repair.RepairProgressChanged += (_, _, _) => progressEvents++;

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.invalid"] = 1 });

    return result.Result == RepairSubmitResult.ErrValidationFailed
        && commitCalls == 0
        && progressEvents == 0
        && repair.GetDeposited(WorldRepair.MvpNodeId).Count == 0
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known;
}

static bool Ac12CommitFailIsAtomic()
{
    var repair = MakeRepair((_, _) => ResourceOperationResult.Fail(ResourceResult.ErrSourceInsufficient));
    var progressEvents = 0;
    repair.RepairProgressChanged += (_, _, _) => progressEvents++;

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    return result.Result == RepairSubmitResult.ErrCommitFailed
        && progressEvents == 0
        && repair.GetDeposited(WorldRepair.MvpNodeId).Count == 0
        && Math.Abs(repair.GetRepairProgress(WorldRepair.MvpNodeId)) < 0.000001d;
}

static bool Ac13SatisfiedMaterialRejects()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 4 });

    var result = repair.ValidateDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 1 });

    return !result.Valid && Has(result, RepairDepositViolation.ExcessQuantity);
}

static bool Ac14MixedValidAndExcessRejects()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    var before = repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"];
    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 2,
        ["resource.basic_supply"] = 1,
    });

    return result.Result == RepairSubmitResult.ErrValidationFailed
        && result.Violations.Contains(RepairDepositViolation.ExcessQuantity)
        && repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == before
        && !repair.GetDeposited(WorldRepair.MvpNodeId).ContainsKey("resource.basic_supply");
}

static bool Ac15LegacyCommitUsesValidation()
{
    var repair = MakeRepair();
    var progressEvents = 0;
    repair.RepairProgressChanged += (_, _, _) => progressEvents++;

    var invalid = repair.CommitDeposit(WorldRepair.MvpNodeId, "resource.invalid", 1);
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 4 });
    var excess = repair.CommitDeposit(WorldRepair.MvpNodeId, "resource.repair_kit", 1);

    return !invalid
        && !excess
        && progressEvents == 1
        && !repair.GetDeposited(WorldRepair.MvpNodeId).ContainsKey("resource.invalid")
        && repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 4;
}
