namespace CloudWeaverVoyage.Core;

/// <summary>
/// Shell-facing aggregate state for the required content domains used by a session entry path.
/// </summary>
public enum RequiredContentDomainStatus
{
	Waiting = 0,
	Complete = 1,
	Failed = 2,
	VersionIncompatible = 3,
}

/// <summary>
/// Per-domain status values consumed by the platform session shell.
/// </summary>
public enum RequiredContentDomainState
{
	Unloaded = 0,
	Loading = 1,
	Partial = 2,
	Complete = 3,
	Failed = 4,
	VersionIncompatible = 5,
}

/// <summary>
/// Failure class derived from required content domain aggregation.
/// </summary>
public enum ContentDomainFailureClass
{
	None = 0,
	Waiting = 1,
	Recoverable = 2,
	Fatal = 3,
}

/// <summary>
/// Entry operation whose failure gates are being evaluated.
/// </summary>
public enum SessionOperationKind
{
	Start = 0,
	Continue = 1,
	Resume = 2,
}

/// <summary>
/// Safe shell action exposed after a fail-closed result.
/// </summary>
public enum FailureRecoveryAction
{
	Retry = 0,
	NewSession = 1,
	ReturnTitle = 2,
	ViewError = 3,
}

/// <summary>
/// Minimal immutable continue-point marker used to prove failure handling is read-only.
/// </summary>
public sealed record ContinuePointSnapshot(
	string Id,
	ContinueAvailability Availability,
	long UpdatedAtMs);

/// <summary>
/// Inputs required to classify a shell failure path.
/// </summary>
public sealed record FailureRecoveryRequest(
	SessionOperationKind OperationKind,
	bool BaseLoaded,
	ContentDomainFailureClass ContentDomainFailureClass,
	AudioGate AudioGate,
	StorageCapability StorageCapability,
	ContinueAvailability ContinueAvailability,
	bool ResumeReady,
	ContinuePointSnapshot? ContinuePoint = null);

/// <summary>
/// Fail-closed result consumed by recovery/fatal shell UI.
/// </summary>
public sealed record FailureRecoveryResult(
	FailureSeverity Severity,
	ShellState TargetState,
	IReadOnlyList<FailureRecoveryAction> Actions,
	ContinuePointSnapshot? PreservedContinuePoint,
	string Reason)
{
	/// <summary>Whether gameplay may be entered immediately after classification.</summary>
	public bool AllowsGameplay => Severity is FailureSeverity.None or FailureSeverity.SoftFail;
}

/// <summary>
/// Computes required-content aggregation, failure severity, and safe recovery targets.
/// </summary>
public static class FailureRecoveryPolicy
{
	private static readonly FailureRecoveryAction[] SoftActions =
	[
		FailureRecoveryAction.ViewError,
		FailureRecoveryAction.ReturnTitle,
	];

	private static readonly FailureRecoveryAction[] RecoveryActions =
	[
		FailureRecoveryAction.Retry,
		FailureRecoveryAction.NewSession,
		FailureRecoveryAction.ReturnTitle,
		FailureRecoveryAction.ViewError,
	];

	private static readonly FailureRecoveryAction[] FatalActions =
	[
		FailureRecoveryAction.Retry,
		FailureRecoveryAction.ReturnTitle,
		FailureRecoveryAction.ViewError,
	];

	/// <summary>
	/// Aggregates the caller-supplied required domain set without owning or hardcoding domain names.
	/// </summary>
	public static RequiredContentDomainStatus AggregateRequiredContentDomains(
		IEnumerable<RequiredContentDomainState> requiredDomainStates)
	{
		var hasWaiting = false;
		var hasFailed = false;

		foreach (var state in requiredDomainStates)
		{
			switch (state)
			{
				case RequiredContentDomainState.VersionIncompatible:
					return RequiredContentDomainStatus.VersionIncompatible;
				case RequiredContentDomainState.Failed:
					hasFailed = true;
					break;
				case RequiredContentDomainState.Unloaded:
				case RequiredContentDomainState.Loading:
				case RequiredContentDomainState.Partial:
					hasWaiting = true;
					break;
			}
		}

		if (hasFailed)
		{
			return RequiredContentDomainStatus.Failed;
		}

		return hasWaiting
			? RequiredContentDomainStatus.Waiting
			: RequiredContentDomainStatus.Complete;
	}

	/// <summary>
	/// Maps aggregate required-content status into the shell failure class.
	/// </summary>
	public static ContentDomainFailureClass ClassifyContentDomainFailure(
		RequiredContentDomainStatus status)
	{
		return status switch
		{
			RequiredContentDomainStatus.VersionIncompatible => ContentDomainFailureClass.Fatal,
			RequiredContentDomainStatus.Failed => ContentDomainFailureClass.Recoverable,
			RequiredContentDomainStatus.Waiting => ContentDomainFailureClass.Waiting,
			_ => ContentDomainFailureClass.None,
		};
	}

	/// <summary>
	/// Calculates failure severity with hard failures taking precedence over recoverable and soft failures.
	/// </summary>
	public static FailureSeverity CalculateFailureSeverity(FailureRecoveryRequest request)
	{
		if (HasHardGateFailure(request))
		{
			return FailureSeverity.HardFail;
		}

		if (request.ContentDomainFailureClass == ContentDomainFailureClass.Recoverable)
		{
			return FailureSeverity.RecoverableFail;
		}

		if (HasSoftGateFailure(request))
		{
			return FailureSeverity.SoftFail;
		}

		return FailureSeverity.None;
	}

	/// <summary>
	/// Produces the fail-closed recovery or fatal target without mutating the supplied continue point.
	/// </summary>
	public static FailureRecoveryResult HandleFailure(FailureRecoveryRequest request)
	{
		var severity = CalculateFailureSeverity(request);
		return severity switch
		{
			FailureSeverity.HardFail => new FailureRecoveryResult(
				severity,
				ShellState.FatalBlocked,
				FatalActions,
				request.ContinuePoint,
				"hard_gate_failed"),
			FailureSeverity.RecoverableFail => new FailureRecoveryResult(
				severity,
				ShellState.RecoveryRequired,
				RecoveryActions,
				request.ContinuePoint,
				"recoverable_gate_failed"),
			FailureSeverity.SoftFail => new FailureRecoveryResult(
				severity,
				ShellState.SessionStarting,
				SoftActions,
				request.ContinuePoint,
				"soft_gate_failed"),
			_ => new FailureRecoveryResult(
				severity,
				ShellState.SessionStarting,
				Array.Empty<FailureRecoveryAction>(),
				request.ContinuePoint,
				"none"),
		};
	}

	private static bool HasHardGateFailure(FailureRecoveryRequest request)
	{
		return !request.BaseLoaded
			|| request.ContentDomainFailureClass == ContentDomainFailureClass.Fatal
			|| request.AudioGate == AudioGate.HardFail
			|| (request.OperationKind == SessionOperationKind.Continue
				&& request.ContinueAvailability == ContinueAvailability.Hidden)
			|| (request.OperationKind == SessionOperationKind.Resume && !request.ResumeReady);
	}

	private static bool HasSoftGateFailure(FailureRecoveryRequest request)
	{
		return request.AudioGate == AudioGate.SoftFail
			|| request.StorageCapability is StorageCapability.WriteLocked or StorageCapability.EphemeralOnly;
	}
}
