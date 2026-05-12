using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 005: Interactable Base Class & Registry ===");
var failed = 0;
var total = 0;

Run("AC-1: Interactable enforces HandleUse through abstract contract", Ac1AbstractContract);
Run("AC-2: register adds target to candidate pool", Ac2Register);
Run("AC-3: unregister removes before scene free", Ac3Unregister);
Run("AC-4: scene transition clears focus before unregister", Ac4TransitionCleanup);
Run("AC-5: ID reuse is treated as a new target instance", Ac5IdReuse);
Run("AC-6: identity comes from interaction_id", Ac6StableId);
Run("AC-7: Initialize does no side-effectful work", Ac7InitializeLight);

if (failed > 0)
{
	Console.Error.WriteLine($"Interactable Registry failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Interactable Registry passed: {total}/{total} checks passed.");
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

static bool Ac1AbstractContract()
{
	return typeof(Interactable).IsAbstract
		&& typeof(Interactable).GetMethod(nameof(Interactable.HandleUse))?.IsAbstract == true;
}

static bool Ac2Register()
{
	var registry = new InteractionRegistry();
	var target = new StubInteractable("hub.helm", WorldVector2.Zero);
	registry.Register(target);
	return registry.CandidateCount == 1 && ReferenceEquals(registry.GetInteractable("hub.helm"), target);
}

static bool Ac3Unregister()
{
	var registry = new InteractionRegistry();
	var targets = new[]
	{
		new StubInteractable("a", WorldVector2.Zero),
		new StubInteractable("b", WorldVector2.Zero),
		new StubInteractable("c", WorldVector2.Zero),
	};
	foreach (var target in targets)
	{
		registry.Register(target);
	}

	foreach (var target in targets)
	{
		registry.Unregister(target);
	}

	return registry.CandidateCount == 0;
}

static bool Ac4TransitionCleanup()
{
	var registry = new InteractionRegistry();
	var target = new StubInteractable("a", WorldVector2.Zero);
	registry.Register(target);
	registry.SetFocus("a");
	registry.BeginSceneTransition();
	registry.Unregister(target);
	return registry.GetFocusTarget() == string.Empty
		&& registry.QueryFocusState().FocusState == WorldFocusState.NoFocus
		&& registry.CandidateCount == 0;
}

static bool Ac5IdReuse()
{
	var registry = new InteractionRegistry();
	var first = new StubInteractable("hub.helm", new WorldVector2(0.5, 0));
	registry.Register(first);
	registry.SetInputGate(MovementInputGateState.InputOpen);
	registry.EvaluateFocus(WorldVector2.Zero);
	registry.TryUse("player", WorldVector2.Zero);
	registry.Unregister(first);
	var second = new StubInteractable("hub.helm", new WorldVector2(0.5, 0));
	registry.Register(second);
	var snapshot = registry.QueryFocusState();
	return first.InstanceKey != second.InstanceKey
		&& snapshot.WorldFocusId == string.Empty
		&& snapshot.FocusState == WorldFocusState.NoFocus;
}

static bool Ac6StableId()
{
	var target = new StubInteractable("stable.content.id", WorldVector2.Zero, displayHint: "Pretty");
	return target.InteractionId == "stable.content.id"
		&& target.DisplayHint == "Pretty";
}

static bool Ac7InitializeLight()
{
	var registry = new InteractionRegistry();
	registry.Initialize();
	return registry.IsInitialized && registry.CandidateCount == 0 && registry.GetFocusTarget() == string.Empty;
}

sealed class StubInteractable : Interactable
{
	public StubInteractable(string id, WorldVector2 position, string displayHint = "")
		: base(id, position, displayHint: displayHint)
	{
	}

	public override UseResult HandleUse(string playerId) => UseResult.Accepted;
}
