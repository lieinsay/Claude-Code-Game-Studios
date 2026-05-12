using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 006: Semantic Events & UI Data Contract ===");
var failed = 0;
var total = 0;

Run("AC-1: focus change emits typed semantic event", Ac1FocusEvent);
Run("AC-2: successful Use emits typed interaction_used event", Ac2UseEvent);
Run("AC-3: blocked Use emits typed use_blocked event", Ac3BlockedEvent);
Run("AC-4: movement blocked emits throttled semantic event", Ac4MovementBlockedEvent);
Run("AC-5: input gate changed emits typed event", Ac5GateEvent);
Run("AC-6: same-frame priority keeps use_blocked over focus", Ac6Priority);
Run("AC-7: UI query returns focus data", Ac7FocusData);
Run("AC-8: UI query returns last block data", Ac8BlockData);
Run("AC-9: UI query returns gate state", Ac9GateData);
Run("AC-10: modal freezes focus data and blocks Use", Ac10ModalFreeze);

if (failed > 0)
{
	Console.Error.WriteLine($"Semantic Events failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Semantic Events passed: {total}/{total} checks passed.");
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

static bool Ac1FocusEvent()
{
	var registry = ReadyRegistry();
	var seen = false;
	registry.InteractionFocusChanged += (oldId, newId, reason) =>
		seen = oldId == string.Empty && newId == "target" && reason.Length > 0;
	registry.SetFocus("target");
	return seen;
}

static bool Ac2UseEvent()
{
	var registry = ReadyRegistry();
	registry.SetFocus("target");
	var seen = false;
	registry.InteractionUseRequested += (id, type) => seen = id == "target" && type == "use";
	registry.TryUse("player", WorldVector2.Zero, 1);
	return seen;
}

static bool Ac3BlockedEvent()
{
	var registry = ReadyRegistry();
	var seen = false;
	registry.UseBlocked += (id, reason) => seen = id == string.Empty && reason == "no_focus";
	registry.TryUse("player", WorldVector2.Zero, 2);
	return seen;
}

static bool Ac4MovementBlockedEvent()
{
	var movement = new PlayerMovementController();
	var seen = false;
	movement.MovementBlocked += (direction, type) => seen = direction.X > 0 && type == "world_geometry";
	movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1.0 / 60.0, 0, _ => WorldVector2.Zero);
	return seen;
}

static bool Ac5GateEvent()
{
	var registry = new InteractionRegistry();
	var seen = false;
	registry.InputGateChanged += (oldState, newState) =>
		seen = oldState == MovementInputGateState.InputClosed && newState == MovementInputGateState.InputOpen;
	registry.SetInputGate(MovementInputGateState.InputOpen);
	return seen;
}

static bool Ac6Priority()
{
	var registry = ReadyRegistry();
	var focusEvents = 0;
	var blockEvents = 0;
	registry.InteractionFocusChanged += (_, _, _) => focusEvents++;
	registry.UseBlocked += (_, _) => blockEvents++;
	registry.SetFocus("target");
	focusEvents = 0;
	registry.TryUse("player", new WorldVector2(5, 0), 10);
	registry.ClearFocus("same_frame");
	return blockEvents == 1 && focusEvents == 0;
}

static bool Ac7FocusData()
{
	var registry = ReadyRegistry();
	registry.SetFocus("target");
	var data = registry.QueryFocusState(MovementState.Idle);
	return data.WorldFocusId == "target"
		&& data.DisplayHint == "target"
		&& data.FocusState == WorldFocusState.Focused;
}

static bool Ac8BlockData()
{
	var registry = ReadyRegistry();
	registry.TryUse("player", WorldVector2.Zero);
	var data = registry.QueryFocusState();
	return data.LastBlockReason == "no_focus" && data.LastBlockTargetId == string.Empty;
}

static bool Ac9GateData()
{
	var registry = new InteractionRegistry();
	registry.SetInputGate(MovementInputGateState.InputOpen);
	var data = registry.QueryFocusState();
	return data.InputGateState == MovementInputGateState.InputOpen && data.IsInputOpen;
}

static bool Ac10ModalFreeze()
{
	var registry = ReadyRegistry();
	registry.SetFocus("target");
	registry.SetUiModalBlocked(true);
	var data = registry.QueryFocusState();
	var use = registry.TryUse("player", WorldVector2.Zero, 20);
	return data.WorldFocusId == "target"
		&& data.FocusState == WorldFocusState.Frozen
		&& !use.Allowed
		&& use.BlockReason == "ui_modal_blocked";
}

static InteractionRegistry ReadyRegistry()
{
	var registry = new InteractionRegistry();
	registry.SetInputGate(MovementInputGateState.InputOpen);
	registry.Register(new StubInteractable("target", new WorldVector2(0.5, 0), "use", "target"));
	return registry;
}

sealed class StubInteractable : Interactable
{
	public StubInteractable(string id, WorldVector2 position, string type, string hint)
		: base(id, position, interactionType: type, displayHint: hint)
	{
	}

	public override UseResult HandleUse(string playerId) => UseResult.Accepted;
}
