using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Player knowledge state for discovered information.
/// </summary>
public enum KnowledgeState
{
    Unrevealed = 0,
    Revealed = 1,
    Rumored = 2,
    Verified = 3,
}

/// <summary>
/// Domain category for knowledge entries.
/// </summary>
public enum KnowledgeDomain
{
    Location = 0,
    Route = 1,
    Hazard = 2,
    Pattern = 3,
    Ability = 4,
}

/// <summary>
/// Owns player knowledge state: known/unknown route information, rumors,
/// risk clues, discoveries. Source of truth for discovered information.
/// </summary>
public sealed class IntelManager
{
    private readonly Dictionary<string, Dictionary<string, object?>> knowledge = new(StringComparer.Ordinal);

    /// <summary>Raised when knowledge state changes for a subject.</summary>
    public event Action<KnowledgeDomain, string, KnowledgeState>? KnowledgeRevealed;

    /// <summary>Raised when a rumor is revealed for a location.</summary>
    public event Action<string, string, int>? RumorRevealed;

    /// <summary>Raised when a pattern is observed.</summary>
    public event Action<string>? PatternObserved;

    /// <summary>Whether the manager has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Marks the manager as ready.</summary>
    public void Initialize()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// Reveals a rumor for a location with confidence clamping.
    /// Confidence is clamped to 100, and to 66 for partner.sky-cat per ADR-0015.
    /// </summary>
    public void RevealRumor(string locationId, string sourceTag, string[] hazardHints, int confidence)
    {
        if (!IsInitialized)
        {
            return;
        }

        var finalConfidence = Math.Min(confidence, 100);
        if (sourceTag == "partner.sky-cat")
        {
            finalConfidence = Math.Min(finalConfidence, 66);
        }

        knowledge[locationId] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["state"] = KnowledgeState.Rumored,
            ["source"] = sourceTag,
            ["confidence"] = finalConfidence,
            ["hazards"] = hazardHints,
        };

        RumorRevealed?.Invoke(locationId, sourceTag, finalConfidence);
    }

    /// <summary>Queries the knowledge state for a subject.</summary>
    public KnowledgeState QueryKnowledge(string subjectId, KnowledgeDomain domain = KnowledgeDomain.Location)
    {
        if (knowledge.TryGetValue(subjectId, out var entry)
            && entry.TryGetValue("state", out var stateValue)
            && stateValue is KnowledgeState state)
        {
            return state;
        }

        return KnowledgeState.Unrevealed;
    }

    /// <summary>Reports an observation event, triggering PatternObserved.</summary>
    public void ReportObservationEvent(string patternId, string eventType)
    {
        PatternObserved?.Invoke(patternId);
    }

    /// <summary>Returns a copy of all knowledge entries.</summary>
    public Dictionary<string, Dictionary<string, object?>> GetAllKnowledge()
    {
        var copy = new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal);
        foreach (var (key, value) in knowledge)
        {
            copy[key] = new Dictionary<string, object?>(value, StringComparer.Ordinal);
        }

        return copy;
    }
}
