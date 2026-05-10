using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Route planning state machine.
/// </summary>
public enum ChartState
{
    Idle = 0,
    Browsing = 1,
    RouteSelected = 2,
    DepartureConfirmed = 3,
    DepartureLocked = 4,
}

/// <summary>
/// Result returned by route selectability check.
/// </summary>
public sealed record RouteSelectabilityResult(bool Selectable, string Reason);

/// <summary>
/// Owns route state, route selection, and departure commitment.
/// Consumes knowledge from Intel, static data from Registry.
/// </summary>
public sealed class ChartManager
{
    private readonly Dictionary<string, Dictionary<string, object?>> routes = new(StringComparer.Ordinal);
    private string selectedRoute = string.Empty;
    private string selectedDestination = string.Empty;

    /// <summary>Raised when a route is selected.</summary>
    public event Action<string, string>? RouteSelected;

    /// <summary>Raised when departure is committed with hazard tags.</summary>
    public event Action<string, string, string[]>? RouteCommitted;

    /// <summary>Raised when departure is locked in.</summary>
    public event Action<string>? DepartureLocked;

    /// <summary>Current chart state machine state.</summary>
    public ChartState CurrentState { get; private set; } = ChartState.Idle;

    /// <summary>Whether the manager has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Marks the chart manager as ready.</summary>
    public void Initialize()
    {
        IsInitialized = true;
    }

    /// <summary>Registers a route with its data.</summary>
    public void RegisterRoute(string routeId, Dictionary<string, object?> routeData)
    {
        routes[routeId] = routeData;
    }

    /// <summary>Selects a route and transitions to RouteSelected state.</summary>
    public bool SelectRoute(string routeId)
    {
        if (!routes.ContainsKey(routeId))
        {
            return false;
        }

        selectedRoute = routeId;
        selectedDestination = ReadString(routes[routeId], "destination_id");
        CurrentState = ChartState.RouteSelected;
        RouteSelected?.Invoke(routeId, selectedDestination);
        return true;
    }

    /// <summary>Commits departure, locking the route and emitting signals.</summary>
    public bool CommitDeparture()
    {
        if (CurrentState != ChartState.RouteSelected)
        {
            return false;
        }

        var routeData = routes.GetValueOrDefault(selectedRoute, new Dictionary<string, object?>());
        var hazardTags = ReadStringArray(routeData, "hazard_tags");
        RouteCommitted?.Invoke(selectedRoute, selectedDestination, hazardTags);
        CurrentState = ChartState.DepartureLocked;
        DepartureLocked?.Invoke(selectedRoute);
        return true;
    }

    /// <summary>Checks whether a route is selectable and returns the reason.</summary>
    public RouteSelectabilityResult CheckRouteSelectability(string routeId)
    {
        if (!IsInitialized)
        {
            return new RouteSelectabilityResult(false, "chart_not_initialized");
        }

        if (!routes.ContainsKey(routeId))
        {
            return new RouteSelectabilityResult(false, "route_not_found");
        }

        var route = routes[routeId];
        if (!ReadBool(route, "traversable"))
        {
            return new RouteSelectabilityResult(false, "route_not_traversable");
        }

        if (CurrentState == ChartState.DepartureLocked)
        {
            return new RouteSelectabilityResult(false, "departure_locked");
        }

        return new RouteSelectabilityResult(true, string.Empty);
    }

    /// <summary>Returns the currently selected route ID.</summary>
    public string GetSelectedRoute()
    {
        return selectedRoute;
    }

    private static string ReadString(Dictionary<string, object?> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static bool ReadBool(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            _ => false,
        };
    }

    private static string[] ReadStringArray(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is string[] arr)
        {
            return arr;
        }

        return [];
    }
}
