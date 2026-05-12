using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 004: Use Gate & Dispatch ===");
var failed = 0;
var total = 0;

Run("AC-1: allowed use emits event and calls HandleUse", Ac1AllowedUse);
Run("AC-2: range failure blocks as too_far", Ac2TooFar);
Run("AC-3: path failure blocks as blocked", Ac3Blocked);
Run("AC-4: busy target blocks as target_busy", Ac4Busy);
Run("AC-5: no focus blocks without interaction_used", Ac5NoFocus);
Run("AC-6: UI modal blocks use", Ac6Modal);
Run("AC-7: UseLocked prevents repeat dispatch", Ac7UseLockedNoRepeat);
Run("AC-8: UseLocked timeout restores focus", Ac8Timeout);
Run("AC-9: disabled target at use time blocks safely", Ac9DisabledRace);
Run("AC-10: buffered use dispatches after release", Ac10BufferedUse);
Run("AC-11: UseLocked can root movement externally", Ac11RootedMovement);

if (failed > 0)
{
	Console.Error.WriteLine($"Use Gate failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Use Gate passed: {total}/{total} checks passed.");
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

static bool Ac1AllowedUse()
{
	var (registry, target) = ReadyTarget();
	var used = false;
	registry.InteractionUseRequested += (id, type) => used = id == "target" && type == "use";
	var result = registry.TryUse("player", WorldVector2.Zero, 0);
	return result.Allowed
		&& result.DomainResult == UseResult.Accepted
		&& used
		&& target.UseCalls == 1
		&& registry.QueryFocusState().FocusState == WorldFocusState.UseLocked;
}

static bool Ac2TooFar()
{
	var registry = OpenRegistry();
	registry.Register(new StubInteractable("target", new WorldVector2(2, 0)));
	registry.SetFocus("target");
	var result = registry.TryUse("player", WorldVector2.Zero);
	return !result.Allowed && result.BlockReason == "too_far";
}

static bool Ac3Blocked()
{
	var registry = OpenRegistry();
	registry.Register(new StubInteractable("target", new WorldVector2(0.5, 0)));
	registry.EvaluateFocus(WorldVector2.Zero, new[] { new InteractionCandidateInput("target", PathClear: false) });
	registry.SetFocus("target");
	var disabledPath = registry.TryUse("player", WorldVector2.Zero);
	return !disabledPath.Allowed && disabledPath.BlockReason == "blocked";
}

static bool Ac4Busy()
{
	var (registry, target) = ReadyTarget();
	target.Busy = true;
	var result = registry.TryUse("player", WorldVector2.Zero);
	return !result.Allowed && result.BlockReason == "target_busy" && target.UseCalls == 0;
}

static bool Ac5NoFocus()
{
	var registry = OpenRegistry();
	var used = false;
	registry.InteractionUseRequested += (_, _) => used = true;
	var result = registry.TryUse("player", WorldVector2.Zero);
	return !result.Allowed && result.BlockReason == "no_focus" && !used;
}

static bool Ac6Modal()
{
	var (registry, _) = ReadyTarget();
	registry.SetUiModalBlocked(true);
	var result = registry.TryUse("player", WorldVector2.Zero);
	return !result.Allowed && result.BlockReason == "ui_modal_blocked";
}

static bool Ac7UseLockedNoRepeat()
{
	var (registry, target) = ReadyTarget();
	registry.TryUse("player", WorldVector2.Zero, 0);
	registry.TryUse("player", WorldVector2.Zero, 0.01);
	registry.TryUse("player", WorldVector2.Zero, 0.02);
	return target.UseCalls == 1;
}

static bool Ac8Timeout()
{
	var (registry, _) = ReadyTarget(new InteractionFocusConfig(UseLockTimeoutSeconds: 0.1));
	registry.TryUse("player", WorldVector2.Zero, 0);
	registry.Tick(0.11);
	return registry.QueryFocusState().FocusState == WorldFocusState.Focused;
}

static bool Ac9DisabledRace()
{
	var (registry, target) = ReadyTarget();
	target.Enabled = false;
	var result = registry.TryUse("player", WorldVector2.Zero);
	return !result.Allowed && result.BlockReason == "target_disabled" && target.UseCalls == 0;
}

static bool Ac10BufferedUse()
{
	var (registry, target) = ReadyTarget(new InteractionFocusConfig(InputBufferWindowSeconds: 0.1));
	registry.TryUse("player", WorldVector2.Zero, 0);
	registry.TryUse("player", WorldVector2.Zero, 0.05);
	registry.ReleaseUseLock("target", WorldVector2.Zero, 0.08);
	return target.UseCalls == 2;
}

static bool Ac11RootedMovement()
{
	var (registry, _) = ReadyTarget();
	var movement = new PlayerMovementController();
	registry.TryUse("player", WorldVector2.Zero, 0);
	movement.SetRooted(registry.QueryFocusState().FocusState == WorldFocusState.UseLocked);
	var result = movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1, 0);
	return result.MovementVelocity == 0 && movement.State == MovementState.Rooted;
}

static (InteractionRegistry Registry, StubInteractable Target) ReadyTarget(InteractionFocusConfig? config = null)
{
	var registry = OpenRegistry(config);
	var target = new StubInteractable("target", new WorldVector2(0.5, 0));
	registry.Register(target);
	registry.SetFocus("target");
	return (registry, target);
}

static InteractionRegistry OpenRegistry(InteractionFocusConfig? config = null)
{
	var registry = new InteractionRegistry(config);
	registry.SetInputGate(MovementInputGateState.InputOpen);
	return registry;
}

sealed class StubInteractable : Interactable
{
	public StubInteractable(string id, WorldVector2 position)
		: base(id, position, interactionType: "use", displayHint: id)
	{
	}

	public int UseCalls { get; private set; }
	public bool Enabled { get; set; } = true;
	public bool Busy { get; set; }
	public override bool IsEnabled => Enabled;
	public override bool IsBusy => Busy;
	public override UseResult HandleUse(string playerId)
	{
		UseCalls++;
		return UseResult.Accepted;
	}
}
