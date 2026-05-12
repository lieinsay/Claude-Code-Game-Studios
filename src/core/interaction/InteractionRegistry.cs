using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Focus state for the legacy interactable registration center.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum FocusState
{
	Idle = 0,
	Focusing = 1,
	Focused = 2,
	Unfocusing = 3,
	Blocked = 4,
}

/// <summary>
/// Input gate state consumed by movement and world interaction.
/// </summary>
public enum MovementInputGateState
{
	InputClosed = 0,
	InputReacquire = 1,
	InputOpen = 2,
}

/// <summary>
/// Movement state exposed to UI and feedback systems.
/// </summary>
public enum MovementState
{
	Idle = 0,
	Moving = 1,
	Blocked = 2,
	Rooted = 3,
}

/// <summary>
/// World interaction focus state.
/// </summary>
public enum WorldFocusState
{
	NoFocus = 0,
	Candidate = 1,
	Focused = 2,
	UsePending = 3,
	UseLocked = 4,
	Frozen = 5,
}

/// <summary>
/// Result returned when an interactable is used.
/// </summary>
public enum UseResult
{
	Accepted = 0,
	Rejected = 1,
	Busy = 2,
}

/// <summary>
/// Deterministic 2D vector used by engine-independent movement and focus tests.
/// </summary>
public readonly record struct WorldVector2(double X, double Y)
{
	/// <summary>Zero vector.</summary>
	public static WorldVector2 Zero { get; } = new(0, 0);

	/// <summary>Vector length.</summary>
	public double Length => Math.Sqrt((X * X) + (Y * Y));

	/// <summary>Returns a normalized vector or zero for near-zero input.</summary>
	public WorldVector2 Normalized()
	{
		var length = Length;
		return length <= double.Epsilon ? Zero : new WorldVector2(X / length, Y / length);
	}

	/// <summary>Distance to another vector.</summary>
	public double DistanceTo(WorldVector2 other)
	{
		return (this - other).Length;
	}

	/// <summary>Clamps vector magnitude to a maximum length.</summary>
	public WorldVector2 ClampLength(double maxLength)
	{
		if (maxLength < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be non-negative.");
		}

		var length = Length;
		return length <= maxLength || length <= double.Epsilon
			? this
			: Normalized() * maxLength;
	}

	public static WorldVector2 operator +(WorldVector2 left, WorldVector2 right)
	{
		return new WorldVector2(left.X + right.X, left.Y + right.Y);
	}

	public static WorldVector2 operator -(WorldVector2 left, WorldVector2 right)
	{
		return new WorldVector2(left.X - right.X, left.Y - right.Y);
	}

	public static WorldVector2 operator *(WorldVector2 vector, double scalar)
	{
		return new WorldVector2(vector.X * scalar, vector.Y * scalar);
	}
}

/// <summary>
/// Movement tuning values normally sourced from scene/content configuration.
/// </summary>
public sealed record MovementConfig(
	double BaseMoveSpeed = 4.2,
	double MaxMoveSpeed = 4.2,
	double MovementBlockEventDelay = 0.15)
{
	/// <summary>Validates movement tuning before a controller uses it.</summary>
	public void Validate()
	{
		if (BaseMoveSpeed <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(BaseMoveSpeed), "Base move speed must be positive.");
		}

		if (MaxMoveSpeed < BaseMoveSpeed)
		{
			throw new ArgumentOutOfRangeException(nameof(MaxMoveSpeed), "Max move speed must be at least base speed.");
		}

		if (MovementBlockEventDelay <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(MovementBlockEventDelay), "Block event delay must be positive.");
		}
	}
}

/// <summary>
/// Result of one deterministic movement physics step.
/// </summary>
public sealed record MovementStepResult(
	WorldVector2 IntendedVelocity,
	WorldVector2 ActualVelocity,
	double MovementVelocity,
	double CollisionMultiplier,
	MovementState State,
	bool MovementBlockedEventEmitted);

/// <summary>
/// Engine-independent movement core mirroring the Godot CharacterBody2D contract.
/// </summary>
public sealed class PlayerMovementController
{
	private double blockEventClock = double.NegativeInfinity;
	private bool suppressHeldInputAfterReacquire;

	/// <summary>Creates a movement controller with validated tuning.</summary>
	public PlayerMovementController(MovementConfig? config = null)
	{
		Config = config ?? new MovementConfig();
		Config.Validate();
	}

	/// <summary>Raised when movement remains blocked and the throttle allows feedback.</summary>
	public event Action<WorldVector2, string>? MovementBlocked;

	/// <summary>Movement tuning.</summary>
	public MovementConfig Config { get; }

	/// <summary>Current player position in world units.</summary>
	public WorldVector2 Position { get; private set; }

	/// <summary>Current movement state.</summary>
	public MovementState State { get; private set; } = MovementState.Idle;

	/// <summary>Whether interaction or domain logic has rooted the player.</summary>
	public bool IsRooted => State == MovementState.Rooted;

	/// <summary>Applies an external rooted lock.</summary>
	public void SetRooted(bool rooted)
	{
		State = rooted ? MovementState.Rooted : MovementState.Idle;
	}

	/// <summary>Places the player at a scene transition spawn point.</summary>
	public void SetPosition(WorldVector2 position)
	{
		Position = position;
	}

	/// <summary>Marks the next open-gate held movement vector as consumed by shell reactivation.</summary>
	public void MarkReacquireConsumed()
	{
		suppressHeldInputAfterReacquire = true;
	}

	/// <summary>
	/// Runs one fixed-physics movement step.
	/// The resolver represents Godot move_and_slide and returns engine-resolved velocity.
	/// </summary>
	public MovementStepResult PhysicsStep(
		WorldVector2 rawInputDirection,
		MovementInputGateState gateState,
		double deltaSeconds,
		double nowSeconds,
		Func<WorldVector2, WorldVector2>? actualVelocityResolver = null)
	{
		if (deltaSeconds < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta must be non-negative.");
		}

		if (gateState != MovementInputGateState.InputOpen)
		{
			if (gateState == MovementInputGateState.InputReacquire && rawInputDirection.Length > 0)
			{
				MarkReacquireConsumed();
			}

			State = State == MovementState.Rooted ? MovementState.Idle : MovementState.Idle;
			return new MovementStepResult(WorldVector2.Zero, WorldVector2.Zero, 0, 1, State, false);
		}

		if (suppressHeldInputAfterReacquire)
		{
			if (rawInputDirection.Length > 0)
			{
				State = State == MovementState.Rooted ? MovementState.Rooted : MovementState.Idle;
				return new MovementStepResult(WorldVector2.Zero, WorldVector2.Zero, 0, 1, State, false);
			}

			suppressHeldInputAfterReacquire = false;
		}

		if (State == MovementState.Rooted)
		{
			return new MovementStepResult(WorldVector2.Zero, WorldVector2.Zero, 0, 1, State, false);
		}

		var inputMagnitude = Math.Min(1.0, rawInputDirection.Length);
		var inputDirection = rawInputDirection.Normalized();
		var scalar = Math.Clamp(Config.BaseMoveSpeed * inputMagnitude, 0, Config.MaxMoveSpeed);
		var intendedVelocity = inputDirection * scalar;
		var actualVelocity = (actualVelocityResolver ?? (velocity => velocity))(intendedVelocity)
			.ClampLength(Config.MaxMoveSpeed);
		var collisionMultiplier = actualVelocity.Length == 0 && intendedVelocity.Length > 0 ? 0.0 : 1.0;
		var movementVelocity = actualVelocity.Length;
		var blocked = collisionMultiplier == 0.0;
		var eventEmitted = false;

		if (blocked)
		{
			State = MovementState.Blocked;
			if (nowSeconds - blockEventClock >= Config.MovementBlockEventDelay)
			{
				blockEventClock = nowSeconds;
				eventEmitted = true;
				MovementBlocked?.Invoke(inputDirection, "world_geometry");
			}
		}
		else
		{
			State = movementVelocity > 0 ? MovementState.Moving : MovementState.Idle;
		}

		Position += actualVelocity * deltaSeconds;
		return new MovementStepResult(
			intendedVelocity,
			actualVelocity,
			movementVelocity,
			collisionMultiplier,
			State,
			eventEmitted);
	}
}

/// <summary>
/// Base class for all world interactables.
/// Godot node subclasses wrap this contract in C# partial node scripts.
/// </summary>
public abstract class Interactable
{
	/// <summary>Creates an interactable with stable identity and authoring values.</summary>
	protected Interactable(
		string interactionId,
		WorldVector2 anchorPosition,
		double anchorRadius = 0.45,
		double priority = 0.5,
		string interactionType = "use",
		string displayHint = "")
	{
		if (string.IsNullOrWhiteSpace(interactionId))
		{
			throw new ArgumentException("Interaction ID must be stable and non-empty.", nameof(interactionId));
		}

		if (anchorRadius < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(anchorRadius), "Anchor radius must be non-negative.");
		}

		InteractionId = interactionId;
		AnchorPosition = anchorPosition;
		AnchorRadius = anchorRadius;
		Priority = Math.Clamp(priority, 0, 1);
		InteractionType = string.IsNullOrWhiteSpace(interactionType) ? "use" : interactionType;
		DisplayHint = displayHint;
		InstanceKey = Guid.NewGuid();
	}

	/// <summary>Stable content-backed interaction ID.</summary>
	public string InteractionId { get; }

	/// <summary>Runtime instance key. ID reuse receives a new key.</summary>
	public Guid InstanceKey { get; }

	/// <summary>Interaction anchor position in world units.</summary>
	public WorldVector2 AnchorPosition { get; set; }

	/// <summary>Anchor radius in world units.</summary>
	public double AnchorRadius { get; }

	/// <summary>Author priority, 0 to 1.</summary>
	public double Priority { get; }

	/// <summary>Interaction type used by feedback and UI prompts.</summary>
	public string InteractionType { get; }

	/// <summary>Short display hint for focus UI.</summary>
	public string DisplayHint { get; }

	/// <summary>Whether the target is currently usable.</summary>
	public virtual bool IsEnabled => true;

	/// <summary>Whether the target is currently processing another interaction.</summary>
	public virtual bool IsBusy => false;

	/// <summary>Handles a Use request. Domain consequences live in subclasses.</summary>
	public abstract UseResult HandleUse(string playerId);
}

/// <summary>
/// Focus tuning values from ADR-0004/GDD #4.
/// </summary>
public sealed record InteractionFocusConfig(
	double PlayerInteractionRadius = 0.25,
	double AcquireMargin = 0.05,
	double RetainMargin = 0.20,
	double MinFocusScore = 0.35,
	double FocusStickinessBonus = 0.08,
	int MaxFocusCandidatesPerQuery = 8,
	double UseLockTimeoutSeconds = 2.0,
	double InputBufferWindowSeconds = 0.10)
{
	/// <summary>Returns a validated copy with acquire margin lower than retain margin.</summary>
	public InteractionFocusConfig Normalized()
	{
		var retain = RetainMargin <= 0 ? 0.20 : RetainMargin;
		var acquire = AcquireMargin >= retain ? retain * 0.5 : Math.Max(0, AcquireMargin);
		return this with
		{
			PlayerInteractionRadius = Math.Max(0, PlayerInteractionRadius),
			AcquireMargin = acquire,
			RetainMargin = retain,
			MinFocusScore = Math.Clamp(MinFocusScore, 0, 1),
			FocusStickinessBonus = Math.Max(0, FocusStickinessBonus),
			MaxFocusCandidatesPerQuery = Math.Max(1, MaxFocusCandidatesPerQuery),
			UseLockTimeoutSeconds = Math.Max(0.01, UseLockTimeoutSeconds),
			InputBufferWindowSeconds = Math.Max(0.01, InputBufferWindowSeconds),
		};
	}
}

/// <summary>
/// Per-candidate runtime values gathered from the scene query.
/// </summary>
public sealed record InteractionCandidateInput(
	string TargetId,
	double PointerScore = 0,
	bool PathClear = true);

/// <summary>
/// Candidate scoring snapshot used by tests, debug UI, and focus selection.
/// </summary>
public sealed record InteractionCandidateSnapshot(
	string TargetId,
	double Distance,
	double ReachLimit,
	bool Reachable,
	double PointerScore,
	double ProximityScore,
	double PriorityScore,
	double StickinessScore,
	double FocusScore);

/// <summary>
/// Focus state snapshot for UI polling.
/// </summary>
public sealed record InteractionFocusSnapshot(
	string WorldFocusId,
	string DisplayHint,
	WorldFocusState FocusState,
	string LastBlockReason,
	string LastBlockTargetId,
	MovementInputGateState InputGateState,
	MovementState MovementState,
	int CandidateCount,
	bool IsInputOpen);

/// <summary>
/// Use gate result.
/// </summary>
public sealed record UseGateResult(bool Allowed, string TargetId, string BlockReason, UseResult? DomainResult);

/// <summary>
/// Interactable registration center spanning all scenes.
/// Owns focus selection, input gate state, candidate pool, and Use dispatch.
/// </summary>
public sealed class InteractionRegistry
{
	private readonly Dictionary<string, object> legacyInteractables = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Interactable> interactables = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Guid> focusInstanceKeys = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> latestPathClear = new(StringComparer.Ordinal);
	private readonly InteractionFocusConfig config;
	private readonly List<InteractionCandidateSnapshot> latestCandidates = [];
	private string focusTarget = string.Empty;
	private string frozenFocusTarget = string.Empty;
	private string lastBlockReason = string.Empty;
	private string lastBlockTargetId = string.Empty;
	private string lockedTargetId = string.Empty;
	private double useLockStartSeconds;
	private bool bufferedUse;
	private double bufferedUseSeconds;
	private int keyboardCycleIndex = -1;
	private int currentEventPriority = int.MaxValue;
	private double eventFrame = double.NaN;

	/// <summary>Creates an interaction registry.</summary>
	public InteractionRegistry(InteractionFocusConfig? config = null)
	{
		this.config = (config ?? new InteractionFocusConfig()).Normalized();
	}

	/// <summary>Raised when an interactable is used. Legacy payload preserves parity tests.</summary>
	public event Action<string, UseResult>? InteractionUsed;

	/// <summary>Raised when focus changes between targets. Legacy payload preserves parity tests.</summary>
	public event Action<string, string>? FocusChanged;

	/// <summary>Raised when focus changes with a transition reason.</summary>
	public event Action<string, string, string>? InteractionFocusChanged;

	/// <summary>Raised when Use is successfully requested.</summary>
	public event Action<string, string>? InteractionUseRequested;

	/// <summary>Raised when Use is blocked.</summary>
	public event Action<string, string>? UseBlocked;

	/// <summary>Raised when input gate changes.</summary>
	public event Action<MovementInputGateState, MovementInputGateState>? InputGateChanged;

	/// <summary>Current legacy focus state.</summary>
	public FocusState FocusStateValue { get; private set; } = FocusState.Idle;

	/// <summary>Current world focus state.</summary>
	public WorldFocusState WorldFocusStateValue { get; private set; } = WorldFocusState.NoFocus;

	/// <summary>Current input gate state.</summary>
	public MovementInputGateState InputGateState { get; private set; } = MovementInputGateState.InputClosed;

	/// <summary>Whether a UI modal blocks world interaction while freezing focus data.</summary>
	public bool UiModalBlocked { get; private set; }

	/// <summary>Whether the registry has been initialized.</summary>
	public bool IsInitialized { get; private set; }

	/// <summary>Candidate pool size.</summary>
	public int CandidateCount => interactables.Count;

	/// <summary>Latest focus candidate scoring snapshot.</summary>
	public IReadOnlyList<InteractionCandidateSnapshot> LatestCandidates => latestCandidates;

	/// <summary>Marks the registry as ready for use.</summary>
	public void Initialize()
	{
		IsInitialized = true;
	}

	/// <summary>Registers an interactable object by stable ID for legacy callers.</summary>
	public void RegisterInteractable(string targetId, object node)
	{
		legacyInteractables[targetId] = node;
	}

	/// <summary>Registers an interactable object for focus and Use dispatch.</summary>
	public void Register(Interactable target)
	{
		interactables[target.InteractionId] = target;
		focusInstanceKeys[target.InteractionId] = target.InstanceKey;
	}

	/// <summary>Unregisters an interactable, clearing state tied to this runtime instance.</summary>
	public void Unregister(Interactable target)
	{
		if (!interactables.TryGetValue(target.InteractionId, out var current)
			|| current.InstanceKey != target.InstanceKey)
		{
			return;
		}

		if (focusTarget == target.InteractionId || lockedTargetId == target.InteractionId)
		{
			ForceReleaseUseLock();
			ClearFocus("target_unregistered");
		}

		interactables.Remove(target.InteractionId);
		focusInstanceKeys.Remove(target.InteractionId);
	}

	/// <summary>Unregisters a legacy interactable, clearing focus if currently targeted.</summary>
	public void UnregisterInteractable(string targetId)
	{
		if (focusTarget == targetId)
		{
			ClearFocus();
		}

		legacyInteractables.Remove(targetId);
		if (interactables.TryGetValue(targetId, out var target))
		{
			Unregister(target);
		}
	}

	/// <summary>Returns the interactable for a given ID, or null.</summary>
	public object? GetInteractable(string targetId)
	{
		if (interactables.TryGetValue(targetId, out var target))
		{
			return target;
		}

		return legacyInteractables.TryGetValue(targetId, out var node) ? node : null;
	}

	/// <summary>Sets focus to a target and emits FocusChanged.</summary>
	public void SetFocus(string targetId)
	{
		if (targetId == focusTarget)
		{
			return;
		}

		var oldTarget = focusTarget;
		focusTarget = targetId;
		FocusStateValue = FocusState.Focused;
		WorldFocusStateValue = string.IsNullOrEmpty(targetId) ? WorldFocusState.NoFocus : WorldFocusState.Focused;
		EmitFocusChanged(oldTarget, targetId, "manual");
	}

	/// <summary>Clears the current focus target.</summary>
	public void ClearFocus()
	{
		ClearFocus("manual");
	}

	/// <summary>Clears the current focus target.</summary>
	public void ClearFocus(string reason)
	{
		if (string.IsNullOrEmpty(focusTarget) && FocusStateValue == FocusState.Idle)
		{
			return;
		}

		var oldTarget = focusTarget;
		focusTarget = string.Empty;
		FocusStateValue = FocusState.Idle;
		WorldFocusStateValue = WorldFocusState.NoFocus;
		keyboardCycleIndex = -1;
		EmitFocusChanged(oldTarget, string.Empty, reason);
	}

	/// <summary>Clears focus and candidates for a scene transition boundary.</summary>
	public void BeginSceneTransition()
	{
		ForceReleaseUseLock();
		ClearFocus("scene_transition");
		latestCandidates.Clear();
	}

	/// <summary>Returns the current focus target ID.</summary>
	public string GetFocusTarget()
	{
		return UiModalBlocked ? frozenFocusTarget : focusTarget;
	}

	/// <summary>Sets the world input gate state from shell-normalized signals.</summary>
	public void SetInputGate(MovementInputGateState newState)
	{
		if (newState == InputGateState)
		{
			return;
		}

		var oldState = InputGateState;
		InputGateState = newState;
		if (newState == MovementInputGateState.InputClosed)
		{
			ForceReleaseUseLock();
			ClearFocus("input_closed");
		}

		EmitFrameEvent(4, () => InputGateChanged?.Invoke(oldState, newState));
	}

	/// <summary>Connects to the existing shell input gate model.</summary>
	public void ApplyShellGate(ShellInputGateState shellGate)
	{
		SetInputGate(shellGate switch
		{
			ShellInputGateState.Open => MovementInputGateState.InputOpen,
			ShellInputGateState.Reacquire => MovementInputGateState.InputReacquire,
			_ => MovementInputGateState.InputClosed,
		});
	}

	/// <summary>Sets whether UI modal routing blocks world Use and freezes focus data.</summary>
	public void SetUiModalBlocked(bool blocked)
	{
		if (blocked == UiModalBlocked)
		{
			return;
		}

		UiModalBlocked = blocked;
		if (blocked)
		{
			frozenFocusTarget = focusTarget;
			WorldFocusStateValue = WorldFocusState.Frozen;
		}
		else
		{
			frozenFocusTarget = string.Empty;
			WorldFocusStateValue = string.IsNullOrEmpty(focusTarget)
				? WorldFocusState.NoFocus
				: WorldFocusState.Focused;
		}
	}

	/// <summary>Evaluates focus candidates and returns the chosen target ID, if any.</summary>
	public string EvaluateFocus(
		WorldVector2 playerPosition,
		IEnumerable<InteractionCandidateInput>? candidateInputs = null,
		bool keyboardCycleNext = false,
		double nowSeconds = 0)
	{
		if (InputGateState != MovementInputGateState.InputOpen || UiModalBlocked)
		{
			return GetFocusTarget();
		}

		BeginFrame(nowSeconds);
		var inputMap = (candidateInputs ?? [])
			.GroupBy(input => input.TargetId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
		var candidates = interactables.Values
			.Take(config.MaxFocusCandidatesPerQuery)
			.Select(target => BuildCandidate(playerPosition, target, inputMap))
			.Where(candidate => candidate.Reachable && candidate.FocusScore >= config.MinFocusScore)
			.OrderByDescending(candidate => candidate.FocusScore)
			.ThenByDescending(candidate => interactables[candidate.TargetId].Priority)
			.ThenBy(candidate => candidate.Distance)
			.ThenBy(candidate => candidate.TargetId, StringComparer.Ordinal)
			.ToList();

		latestCandidates.Clear();
		latestCandidates.AddRange(candidates);

		if (candidates.Count == 0)
		{
			ClearFocus("no_candidates");
			return string.Empty;
		}

		var pointerCandidate = candidates.FirstOrDefault(candidate => candidate.PointerScore > 0);
		if (pointerCandidate is not null)
		{
			keyboardCycleIndex = candidates.IndexOf(pointerCandidate);
			SetFocusFromSelection(pointerCandidate.TargetId, "pointer");
			return pointerCandidate.TargetId;
		}

		if (keyboardCycleNext)
		{
			var keyboardCandidates = candidates
				.OrderByDescending(candidate => candidate.FocusScore - (0.15 * candidate.StickinessScore))
				.ThenByDescending(candidate => interactables[candidate.TargetId].Priority)
				.ThenBy(candidate => candidate.Distance)
				.ThenBy(candidate => candidate.TargetId, StringComparer.Ordinal)
				.ToList();
			var currentIndex = keyboardCandidates.FindIndex(candidate => candidate.TargetId == focusTarget);
			keyboardCycleIndex = (currentIndex + 1 + keyboardCandidates.Count) % keyboardCandidates.Count;
			var cycled = keyboardCandidates[keyboardCycleIndex].TargetId;
			SetFocusFromSelection(cycled, "keyboard_cycle");
			return cycled;
		}

		var selected = candidates
			.OrderByDescending(candidate => candidate.FocusScore + (candidate.TargetId == focusTarget ? config.FocusStickinessBonus : 0))
			.ThenByDescending(candidate => interactables[candidate.TargetId].Priority)
			.ThenBy(candidate => candidate.Distance)
			.ThenBy(candidate => candidate.TargetId, StringComparer.Ordinal)
			.First();
		keyboardCycleIndex = Math.Max(0, candidates.FindIndex(candidate => candidate.TargetId == selected.TargetId));
		SetFocusFromSelection(selected.TargetId, "selection");
		return selected.TargetId;
	}

	/// <summary>Attempts a Use dispatch against the current focus target.</summary>
	public UseGateResult TryUse(string playerId, WorldVector2 playerPosition, double nowSeconds = 0)
	{
		BeginFrame(nowSeconds);
		if (WorldFocusStateValue == WorldFocusState.UseLocked)
		{
			bufferedUse = true;
			bufferedUseSeconds = nowSeconds;
			return BlockUse(lockedTargetId, "use_locked");
		}

		var gate = EvaluateUseGate(playerPosition);
		if (!gate.Allowed)
		{
			return gate;
		}

		var target = interactables[gate.TargetId];
		WorldFocusStateValue = WorldFocusState.UsePending;
		EmitFrameEvent(1, () =>
		{
			InteractionUseRequested?.Invoke(target.InteractionId, target.InteractionType);
			InteractionUsed?.Invoke(target.InteractionId, UseResult.Accepted);
		});

		var domainResult = target.HandleUse(playerId);
		if (domainResult == UseResult.Accepted)
		{
			WorldFocusStateValue = WorldFocusState.UseLocked;
			FocusStateValue = FocusState.Blocked;
			lockedTargetId = target.InteractionId;
			useLockStartSeconds = nowSeconds;
		}
		else
		{
			WorldFocusStateValue = WorldFocusState.Focused;
			FocusStateValue = FocusState.Focused;
			if (domainResult == UseResult.Busy)
			{
				return BlockUse(target.InteractionId, "target_busy");
			}
		}

		return gate with { DomainResult = domainResult };
	}

	/// <summary>Releases a domain-owned Use lock.</summary>
	public bool ReleaseUseLock(string targetId, WorldVector2 playerPosition = default, double nowSeconds = 0)
	{
		if (WorldFocusStateValue != WorldFocusState.UseLocked || lockedTargetId != targetId)
		{
			return false;
		}

		ForceReleaseUseLock();
		if (bufferedUse && nowSeconds - bufferedUseSeconds <= config.InputBufferWindowSeconds)
		{
			bufferedUse = false;
			TryUse("player", playerPosition, nowSeconds);
		}

		return true;
	}

	/// <summary>Advances timeout handling for UseLocked.</summary>
	public void Tick(double nowSeconds)
	{
		BeginFrame(nowSeconds);
		if (WorldFocusStateValue == WorldFocusState.UseLocked
			&& nowSeconds - useLockStartSeconds >= config.UseLockTimeoutSeconds)
		{
			ForceReleaseUseLock();
			lastBlockReason = "use_lock_timeout";
			lastBlockTargetId = lockedTargetId;
		}
	}

	/// <summary>Returns a UI polling snapshot.</summary>
	public InteractionFocusSnapshot QueryFocusState(MovementState movementState = MovementState.Idle)
	{
		var targetId = GetFocusTarget();
		var displayHint = targetId.Length > 0 && interactables.TryGetValue(targetId, out var target)
			? target.DisplayHint
			: string.Empty;
		return new InteractionFocusSnapshot(
			targetId,
			displayHint,
			WorldFocusStateValue,
			lastBlockReason,
			lastBlockTargetId,
			InputGateState,
			movementState,
			interactables.Count,
			InputGateState == MovementInputGateState.InputOpen && !UiModalBlocked);
	}

	private InteractionCandidateSnapshot BuildCandidate(
		WorldVector2 playerPosition,
		Interactable target,
		IReadOnlyDictionary<string, InteractionCandidateInput> inputMap)
	{
		inputMap.TryGetValue(target.InteractionId, out var input);
		var isCurrent = target.InteractionId == focusTarget;
		var margin = isCurrent ? config.RetainMargin : config.AcquireMargin;
		var reachLimit = target.AnchorRadius + config.PlayerInteractionRadius + margin;
		var distance = playerPosition.DistanceTo(target.AnchorPosition);
		var pathClear = input?.PathClear ?? true;
		latestPathClear[target.InteractionId] = pathClear;
		var pointerScore = Math.Clamp(input?.PointerScore ?? 0, 0, 1);
		var reachable = target.IsEnabled && pathClear && distance <= reachLimit;
		var proximityScore = reachLimit <= 0 ? 0 : 1 - Math.Clamp(distance / reachLimit, 0, 1);
		var stickinessScore = isCurrent && reachable ? 1 : 0;
		var focusScore = Math.Clamp(
			(0.45 * pointerScore)
			+ (0.25 * proximityScore)
			+ (0.15 * target.Priority)
			+ (0.15 * stickinessScore),
			0,
			1);

		return new InteractionCandidateSnapshot(
			target.InteractionId,
			distance,
			reachLimit,
			reachable,
			pointerScore,
			proximityScore,
			target.Priority,
			stickinessScore,
			focusScore);
	}

	private UseGateResult EvaluateUseGate(WorldVector2 playerPosition)
	{
		if (InputGateState != MovementInputGateState.InputOpen)
		{
			return BlockUse(focusTarget, "input_closed");
		}

		if (UiModalBlocked)
		{
			return BlockUse(GetFocusTarget(), "ui_modal_blocked");
		}

		if (string.IsNullOrEmpty(focusTarget) || !interactables.TryGetValue(focusTarget, out var target))
		{
			return BlockUse(string.Empty, "no_focus");
		}

		if (!target.IsEnabled)
		{
			ClearFocus("target_disabled");
			return BlockUse(target.InteractionId, "target_disabled");
		}

		var candidate = BuildCandidate(playerPosition, target, new Dictionary<string, InteractionCandidateInput>
		{
			[target.InteractionId] = new InteractionCandidateInput(
				target.InteractionId,
				PathClear: !latestPathClear.TryGetValue(target.InteractionId, out var pathClear) || pathClear),
		});
		if (!candidate.Reachable)
		{
			return BlockUse(target.InteractionId, candidate.Distance > candidate.ReachLimit ? "too_far" : "blocked");
		}

		if (target.IsBusy)
		{
			return BlockUse(target.InteractionId, "target_busy");
		}

		return new UseGateResult(true, target.InteractionId, string.Empty, null);
	}

	private UseGateResult BlockUse(string targetId, string reason)
	{
		lastBlockReason = reason;
		lastBlockTargetId = targetId;
		EmitFrameEvent(2, () => UseBlocked?.Invoke(targetId, reason));
		return new UseGateResult(false, targetId, reason, null);
	}

	private void SetFocusFromSelection(string targetId, string reason)
	{
		if (targetId == focusTarget)
		{
			WorldFocusStateValue = WorldFocusState.Focused;
			FocusStateValue = FocusState.Focused;
			return;
		}

		var oldTarget = focusTarget;
		focusTarget = targetId;
		FocusStateValue = FocusState.Focused;
		WorldFocusStateValue = WorldFocusState.Focused;
		EmitFocusChanged(oldTarget, targetId, reason);
	}

	private void EmitFocusChanged(string oldTarget, string newTarget, string reason)
	{
		FocusChanged?.Invoke(oldTarget, newTarget);
		EmitFrameEvent(3, () => InteractionFocusChanged?.Invoke(oldTarget, newTarget, reason));
	}

	private void BeginFrame(double nowSeconds)
	{
		if (Math.Abs(nowSeconds - eventFrame) > double.Epsilon)
		{
			eventFrame = nowSeconds;
			currentEventPriority = int.MaxValue;
		}
	}

	private void EmitFrameEvent(int priority, Action emit)
	{
		if (priority > currentEventPriority)
		{
			return;
		}

		currentEventPriority = priority;
		emit();
	}

	private void ForceReleaseUseLock()
	{
		if (WorldFocusStateValue == WorldFocusState.UseLocked)
		{
			WorldFocusStateValue = string.IsNullOrEmpty(focusTarget)
				? WorldFocusState.NoFocus
				: WorldFocusState.Focused;
			FocusStateValue = string.IsNullOrEmpty(focusTarget) ? FocusState.Idle : FocusState.Focused;
		}

		lockedTargetId = string.Empty;
		useLockStartSeconds = 0;
	}
}
