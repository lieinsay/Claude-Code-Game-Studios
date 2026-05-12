using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 001: Movement System ===");
var failed = 0;
var total = 0;

Run("AC-1: input-open movement advances at configured speed", Ac1InputOpenMovement);
Run("AC-2: released input stops in one frame", Ac2ReleaseStops);
Run("AC-3: diagonal input is normalized and capped", Ac3DiagonalCapped);
Run("AC-4: input-closed blocks movement", Ac4GateClosedBlocks);
Run("AC-5: rooted blocks movement", Ac5RootedBlocks);
Run("AC-6: zero actual velocity from nonzero intent becomes blocked", Ac6CollisionBlocked);
Run("AC-7: blocked movement event is throttled", Ac7BlockedEventThrottle);

if (failed > 0)
{
	Console.Error.WriteLine($"Movement System failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Movement System passed: {total}/{total} checks passed.");
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

static bool Nearly(double actual, double expected, double tolerance = 0.0001)
{
	return Math.Abs(actual - expected) <= tolerance;
}

static bool Ac1InputOpenMovement()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4));
	var result = controller.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1, 0);
	return Nearly(result.MovementVelocity, 4)
		&& Nearly(controller.Position.X, 4)
		&& controller.State == MovementState.Moving;
}

static bool Ac2ReleaseStops()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4));
	controller.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1.0 / 60.0, 0);
	var result = controller.PhysicsStep(WorldVector2.Zero, MovementInputGateState.InputOpen, 1.0 / 60.0, 1.0 / 60.0);
	return result.MovementVelocity == 0 && controller.State == MovementState.Idle;
}

static bool Ac3DiagonalCapped()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4));
	var result = controller.PhysicsStep(new WorldVector2(1, 1), MovementInputGateState.InputOpen, 1.0 / 60.0, 0);
	return result.MovementVelocity <= 4.0001
		&& Nearly(result.IntendedVelocity.Length, 4);
}

static bool Ac4GateClosedBlocks()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4));
	var result = controller.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputClosed, 1, 0);
	return result.MovementVelocity == 0
		&& controller.Position == WorldVector2.Zero
		&& controller.State == MovementState.Idle;
}

static bool Ac5RootedBlocks()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4));
	controller.SetRooted(true);
	var result = controller.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1, 0);
	return result.MovementVelocity == 0
		&& controller.State == MovementState.Rooted
		&& controller.Position == WorldVector2.Zero;
}

static bool Ac6CollisionBlocked()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4));
	var result = controller.PhysicsStep(
		new WorldVector2(1, 0),
		MovementInputGateState.InputOpen,
		1,
		0,
		_ => WorldVector2.Zero);
	return result.CollisionMultiplier == 0
		&& result.MovementVelocity == 0
		&& controller.State == MovementState.Blocked
		&& controller.Position == WorldVector2.Zero;
}

static bool Ac7BlockedEventThrottle()
{
	var controller = new PlayerMovementController(new MovementConfig(BaseMoveSpeed: 4, MaxMoveSpeed: 4, MovementBlockEventDelay: 0.15));
	var events = 0;
	controller.MovementBlocked += (_, _) => events++;
	for (var i = 0; i <= 60; i++)
	{
		controller.PhysicsStep(
			new WorldVector2(1, 0),
			MovementInputGateState.InputOpen,
			1.0 / 60.0,
			i / 60.0,
			_ => WorldVector2.Zero);
	}

	return events <= (int)(1 / 0.15) + 1;
}
