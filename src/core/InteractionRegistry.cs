using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Focus state for the interactable registration center.
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
/// Result returned when an interactable is used.
/// </summary>
public enum UseResult
{
    Accepted = 0,
    Rejected = 1,
    Busy = 2,
}

/// <summary>
/// Interactable registration center spanning all scenes.
/// Owns 5-state focus machine and dual-channel dispatch.
/// </summary>
public sealed class InteractionRegistry
{
    private readonly Dictionary<string, object> interactables = new(StringComparer.Ordinal);
    private string focusTarget = string.Empty;

    /// <summary>
    /// Raised when an interactable is used.
    /// </summary>
    public event Action<string, UseResult>? InteractionUsed;

    /// <summary>
    /// Raised when focus changes between targets.
    /// </summary>
    public event Action<string, string>? FocusChanged;

    /// <summary>Current focus state.</summary>
    public FocusState FocusStateValue { get; private set; } = FocusState.Idle;

    /// <summary>Whether the registry has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Marks the registry as ready for use.</summary>
    public void Initialize()
    {
        IsInitialized = true;
    }

    /// <summary>Registers an interactable object by stable ID.</summary>
    public void RegisterInteractable(string targetId, object node)
    {
        interactables[targetId] = node;
    }

    /// <summary>Unregisters an interactable, clearing focus if currently targeted.</summary>
    public void UnregisterInteractable(string targetId)
    {
        if (focusTarget == targetId)
        {
            ClearFocus();
        }

        interactables.Remove(targetId);
    }

    /// <summary>Returns the interactable for a given ID, or null.</summary>
    public object? GetInteractable(string targetId)
    {
        return interactables.TryGetValue(targetId, out var node) ? node : null;
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
        FocusChanged?.Invoke(oldTarget, targetId);
    }

    /// <summary>Clears the current focus target.</summary>
    public void ClearFocus()
    {
        var oldTarget = focusTarget;
        focusTarget = string.Empty;
        FocusStateValue = FocusState.Idle;
        FocusChanged?.Invoke(oldTarget, string.Empty);
    }

    /// <summary>Returns the current focus target ID.</summary>
    public string GetFocusTarget()
    {
        return focusTarget;
    }
}
