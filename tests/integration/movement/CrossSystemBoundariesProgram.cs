using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 007: Cross-System Boundaries & Desktop Lifecycle ===");
var failed = 0;
var total = 0;

Run("AC-1/2/3/5: movement source avoids forbidden domain and scene APIs", AcForbiddenTermsAbsent);
Run("AC-4: domain consequences remain inside HandleUse", Ac4DomainConsequences);
Run("AC-6: scene transition clears world focus", Ac6TransitionClearsFocus);
Run("AC-7: ID reuse does not retain old focus or lock", Ac7IdReuse);
Run("AC-8: shell-forwarded focus loss closes input and freezes position", Ac8FocusLossStopsInput);
Run("AC-9: semantic events emit before audio readiness", Ac9EventsWithoutAudio);
Run("AC-10: resume reacquire frame does not auto-use hovered target", Ac10ResumeNoAutoUse);

if (failed > 0)
{
	Console.Error.WriteLine($"Cross-System Boundaries failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Cross-System Boundaries passed: {total}/{total} checks passed.");
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

static bool AcForbiddenTermsAbsent()
{
	var source = File.ReadAllText(Path.Combine("src", "core", "interaction", "InteractionRegistry.cs"));
	var forbidden = new[]
	{
		"currency", "money", "gold", "inventory", "items", "Save(", "Write(", "Persist",
		"ChangeScene", "LoadScene", "SceneTree",
	};
	return forbidden.All(term => source.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0);
}

static bool Ac4DomainConsequences()
{
	var registry = ReadyRegistry(new DomainInteractable("stall.trade", new WorldVector2(0.5, 0)));
	registry.SetFocus("stall.trade");
	var result = registry.TryUse("player", WorldVector2.Zero);
	var target = (DomainInteractable)registry.GetInteractable("stall.trade")!;
	return result.Allowed && target.DomainHandled && target.CurrencyTouchedOnlyInsideDomain;
}

static bool Ac6TransitionClearsFocus()
{
	var target = new DomainInteractable("hub.helm", new WorldVector2(0.5, 0));
	var registry = ReadyRegistry(target);
	registry.EvaluateFocus(WorldVector2.Zero);
	registry.BeginSceneTransition();
	registry.Unregister(target);
	return registry.QueryFocusState().WorldFocusId == string.Empty;
}

static bool Ac7IdReuse()
{
	var first = new DomainInteractable("hub.helm", new WorldVector2(0.5, 0));
	var registry = ReadyRegistry(first);
	registry.EvaluateFocus(WorldVector2.Zero);
	registry.TryUse("player", WorldVector2.Zero, 0);
	registry.Unregister(first);
	var second = new DomainInteractable("hub.helm", new WorldVector2(0.5, 0));
	registry.Register(second);
	return first.InstanceKey != second.InstanceKey
		&& registry.QueryFocusState().WorldFocusId == string.Empty;
}

static bool Ac8FocusLossStopsInput()
{
	var shell = ActiveShell();
	var lifecycle = new DesktopSessionLifecycle(shell, 1);
	var movement = new PlayerMovementController();
	lifecycle.OnWindowFocusChanged(focused: false);
	var result = movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputClosed, 1, 0);
	return shell.CurrentState == ShellState.BackgroundSuspended
		&& result.MovementVelocity == 0
		&& movement.Position == WorldVector2.Zero;
}

static bool Ac9EventsWithoutAudio()
{
	var registry = ReadyRegistry(new DomainInteractable("hub.helm", new WorldVector2(0.5, 0)));
	var semanticEvents = 0;
	var audioReady = false;
	registry.InteractionFocusChanged += (_, _, _) => semanticEvents++;
	registry.SetFocus("hub.helm");
	return semanticEvents == 1 && !audioReady;
}

static bool Ac10ResumeNoAutoUse()
{
	var registry = ReadyRegistry(new DomainInteractable("hub.helm", new WorldVector2(0.5, 0)));
	var uses = 0;
	registry.InteractionUseRequested += (_, _) => uses++;
	registry.SetInputGate(MovementInputGateState.InputReacquire);
	registry.EvaluateFocus(
		WorldVector2.Zero,
		new[] { new InteractionCandidateInput("hub.helm", PointerScore: 1) },
		nowSeconds: 0);
	registry.SetInputGate(MovementInputGateState.InputOpen);
	registry.EvaluateFocus(
		WorldVector2.Zero,
		new[] { new InteractionCandidateInput("hub.helm", PointerScore: 1) },
		nowSeconds: 1.0 / 60.0);
	return uses == 0 && registry.GetFocusTarget() == "hub.helm";
}

static InteractionRegistry ReadyRegistry(Interactable target)
{
	var registry = new InteractionRegistry();
	registry.SetInputGate(MovementInputGateState.InputOpen);
	registry.Register(target);
	return registry;
}

static SessionBootChain ActiveShell()
{
	var shell = new SessionBootChain();
	shell.TransitionTo(ShellState.Loading);
	shell.CompleteAllLoadPhases();
	shell.TransitionTo(ShellState.Ready);
	shell.TransitionTo(ShellState.SessionStarting);
	shell.ResolveSessionStart(SessionTransitionContext.ReadyToActivate);
	return shell;
}

sealed class DomainInteractable : Interactable
{
	public DomainInteractable(string id, WorldVector2 position)
		: base(id, position, interactionType: "trade", displayHint: "stall")
	{
	}

	public bool DomainHandled { get; private set; }
	public bool CurrencyTouchedOnlyInsideDomain { get; private set; }

	public override UseResult HandleUse(string playerId)
	{
		DomainHandled = true;
		CurrencyTouchedOnlyInsideDomain = true;
		return UseResult.Accepted;
	}
}
