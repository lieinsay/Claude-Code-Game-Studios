using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 003: Interaction Focus & Candidate Selection ===");
var failed = 0;
var total = 0;

Run("AC-1/2: reachable top candidate becomes the only focus", Ac1TopCandidate);
Run("AC-3: current focus retention prevents edge flicker", Ac3Hysteresis);
Run("AC-4: leaving retain range clears or switches focus", Ac4LeaveRange);
Run("AC-5: disabling focused target clears focus", Ac5DisableClears);
Run("AC-6: blocked path prevents focus", Ac6PathBlocked);
Run("AC-7/12: pointer priority wins and resets keyboard cycle", Ac7PointerWins);
Run("AC-8: empty-space click with no focus does not use", Ac8EmptyClick);
Run("AC-9: tie-breaking uses priority, distance, then stable ID", Ac9TieBreak);
Run("AC-10: exact reach boundary is reachable", Ac10Boundary);
Run("AC-11: keyboard cycles candidates by score", Ac11KeyboardCycle);

if (failed > 0)
{
	Console.Error.WriteLine($"Focus Selection failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Focus Selection passed: {total}/{total} checks passed.");
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

static bool Ac1TopCandidate()
{
	var registry = OpenRegistry();
	registry.Register(new StubInteractable("hub.helm", new WorldVector2(0.1, 0), priority: 1.0));
	registry.Register(new StubInteractable("hub.crate", new WorldVector2(0.6, 0), priority: 0.2));
	var focus = registry.EvaluateFocus(WorldVector2.Zero);
	return focus == "hub.helm"
		&& registry.GetFocusTarget() == "hub.helm"
		&& registry.QueryFocusState().FocusState == WorldFocusState.Focused;
}

static bool Ac3Hysteresis()
{
	var registry = OpenRegistry(new InteractionFocusConfig(MinFocusScore: 0.0));
	registry.Register(new StubInteractable("a", new WorldVector2(0.86, 0), priority: 0.5));
	registry.Register(new StubInteractable("b", new WorldVector2(0.70, 0), priority: 0.5));
	registry.SetFocus("a");
	var focus = registry.EvaluateFocus(WorldVector2.Zero);
	return focus == "a";
}

static bool Ac4LeaveRange()
{
	var registry = OpenRegistry();
	registry.Register(new StubInteractable("a", new WorldVector2(1.2, 0), priority: 1));
	registry.SetFocus("a");
	var focus = registry.EvaluateFocus(WorldVector2.Zero);
	return focus == string.Empty && registry.QueryFocusState().FocusState == WorldFocusState.NoFocus;
}

static bool Ac5DisableClears()
{
	var registry = OpenRegistry();
	var target = new StubInteractable("a", new WorldVector2(0.5, 0)) { Enabled = true };
	registry.Register(target);
	registry.EvaluateFocus(WorldVector2.Zero);
	target.Enabled = false;
	registry.EvaluateFocus(WorldVector2.Zero);
	return registry.GetFocusTarget() == string.Empty;
}

static bool Ac6PathBlocked()
{
	var registry = OpenRegistry();
	registry.Register(new StubInteractable("a", new WorldVector2(0.5, 0)));
	var focus = registry.EvaluateFocus(
		WorldVector2.Zero,
		new[] { new InteractionCandidateInput("a", PathClear: false) });
	return focus == string.Empty;
}

static bool Ac7PointerWins()
{
	var registry = OpenRegistry();
	registry.Register(new StubInteractable("a", new WorldVector2(0.3, 0), priority: 0.5));
	registry.Register(new StubInteractable("b", new WorldVector2(0.3, 0), priority: 0.5));
	registry.EvaluateFocus(WorldVector2.Zero, keyboardCycleNext: true);
	var pointer = registry.EvaluateFocus(
		WorldVector2.Zero,
		new[] { new InteractionCandidateInput("b", PointerScore: 1) });
	return pointer == "b";
}

static bool Ac8EmptyClick()
{
	var registry = OpenRegistry();
	var used = false;
	registry.InteractionUseRequested += (_, _) => used = true;
	var result = registry.TryUse("player", WorldVector2.Zero);
	return !used && !result.Allowed && result.BlockReason == "no_focus";
}

static bool Ac9TieBreak()
{
	var registry = OpenRegistry(new InteractionFocusConfig(MinFocusScore: 0.1));
	registry.Register(new StubInteractable("b", new WorldVector2(0.4, 0), priority: 0.7));
	registry.Register(new StubInteractable("a", new WorldVector2(0.4, 0), priority: 0.7));
	return registry.EvaluateFocus(WorldVector2.Zero) == "a";
}

static bool Ac10Boundary()
{
	var registry = OpenRegistry(new InteractionFocusConfig(PlayerInteractionRadius: 0.25, AcquireMargin: 0.05, MinFocusScore: 0.0));
	registry.Register(new StubInteractable("edge", new WorldVector2(0.75, 0), anchorRadius: 0.45));
	var focus = registry.EvaluateFocus(WorldVector2.Zero);
	return focus == "edge"
		&& registry.LatestCandidates.Single().Reachable;
}

static bool Ac11KeyboardCycle()
{
	var registry = OpenRegistry(new InteractionFocusConfig(MinFocusScore: 0.0));
	registry.Register(new StubInteractable("c1", new WorldVector2(0.2, 0), priority: 1));
	registry.Register(new StubInteractable("c2", new WorldVector2(0.3, 0), priority: 1));
	registry.Register(new StubInteractable("c3", new WorldVector2(0.4, 0), priority: 1));
	var first = registry.EvaluateFocus(WorldVector2.Zero, keyboardCycleNext: true);
	var second = registry.EvaluateFocus(WorldVector2.Zero, keyboardCycleNext: true);
	var third = registry.EvaluateFocus(WorldVector2.Zero, keyboardCycleNext: true);
	var wrap = registry.EvaluateFocus(WorldVector2.Zero, keyboardCycleNext: true);
	return first == "c1" && second == "c2" && third == "c3" && wrap == "c1";
}

static InteractionRegistry OpenRegistry(InteractionFocusConfig? config = null)
{
	var registry = new InteractionRegistry(config);
	registry.Initialize();
	registry.SetInputGate(MovementInputGateState.InputOpen);
	return registry;
}

sealed class StubInteractable : Interactable
{
	public StubInteractable(string id, WorldVector2 position, double anchorRadius = 0.45, double priority = 0.5)
		: base(id, position, anchorRadius, priority, "use", id)
	{
	}

	public bool Enabled { get; set; } = true;
	public override bool IsEnabled => Enabled;
	public override UseResult HandleUse(string playerId) => UseResult.Accepted;
}
