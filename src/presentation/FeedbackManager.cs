using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Semantic event hub for VFX, audio, and sensory feedback.
/// Vertical Slice system — MVP stub. Full implementation deferred (ADR-0016).
/// </summary>
public sealed class FeedbackManager
{
	private readonly Dictionary<string, Action<Dictionary<string, object?>>?> subscriptions = new(StringComparer.Ordinal);

	/// <summary>Raised when a feedback event is triggered.</summary>
	public event Action<string, Dictionary<string, object?>>? FeedbackTriggered;

	/// <summary>Raised when a UI event is consumed.</summary>
	public event Action<string>? UIEventConsumed;

	/// <summary>Whether the manager has been initialized.</summary>
	public bool IsInitialized { get; private set; }

	/// <summary>Marks the feedback system as ready.</summary>
	public void Initialize()
	{
		IsInitialized = true;
	}

	/// <summary>Subscribes a callback to a semantic event ID.</summary>
	public void Subscribe(string eventId, Action<Dictionary<string, object?>>? callback)
	{
		subscriptions[eventId] = callback;
	}

	/// <summary>Emits a feedback event and invokes subscribers.</summary>
	public void EmitFeedback(string eventId, Dictionary<string, object?>? parameters = null)
	{
		parameters ??= new Dictionary<string, object?>();
		FeedbackTriggered?.Invoke(eventId, parameters);

		if (subscriptions.TryGetValue(eventId, out var callback) && callback is not null)
		{
			callback(parameters);
		}
	}

	/// <summary>Semantic event stub: route selected.</summary>
	public void OnRouteSelected(string routeId, string destinationId)
	{
		EmitFeedback("route_selected");
	}

	/// <summary>Semantic event stub: repair completed.</summary>
	public void OnRepairCompleted(string nodeId)
	{
		EmitFeedback("world_repair_completed");
	}

	/// <summary>Semantic event stub: threat triggered.</summary>
	public void OnThreatTriggered(string threatId)
	{
		EmitFeedback("threat_warning");
	}
}
