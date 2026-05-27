using Godot;

/// <summary>
/// Independent world scene asset for the initial island dock before boarding the Cloudweaver.
/// </summary>
public partial class InitialIslandScene : Node2D
{
	/// <summary>Design-facing scene asset ID.</summary>
	[Export]
	public string SceneId { get; set; } = "initial_island_scene";

	/// <summary>Runtime compatibility contract ID used by HubRuntime and existing saves/tests.</summary>
	[Export]
	public string RuntimeContractId { get; set; } = "hub_island_dock";

	/// <summary>Scene asset ID reached by the boarding ramp.</summary>
	[Export]
	public string BoardingTargetSceneId { get; set; } = "ship_interior_layered";

	/// <summary>Runtime contract reached by the boarding ramp.</summary>
	[Export]
	public string BoardingTargetRuntimeContractId { get; set; } = "hub_ship_interior";

	/// <summary>Emitted when a future direct scene-local boarding handler requests ship entry.</summary>
	[Signal]
	public delegate void BoardingRequestedEventHandler();

	/// <summary>Requests boarding from the local scene anchor without owning the Hub state transition.</summary>
	public void RequestBoarding() => EmitSignal(SignalName.BoardingRequested);

	/// <summary>Returns concrete scene-asset evidence for smoke tests and QA documentation.</summary>
	public Godot.Collections.Dictionary DebugSceneAssetEvidence()
	{
		return new Godot.Collections.Dictionary
		{
			["scene_id"] = SceneId,
			["runtime_contract_id"] = RuntimeContractId,
			["boarding_target_scene_id"] = BoardingTargetSceneId,
			["boarding_target_runtime_contract_id"] = BoardingTargetRuntimeContractId,
			["world_layer_ready"] = GetNodeOrNull<Node2D>("InitialIslandWorldLayer") is not null,
			["player_spawn_ready"] = GetNodeOrNull<Marker2D>("InitialIslandWorldLayer/InitialIslandPlayerStart") is not null,
			["boarding_anchor_ready"] = GetNodeOrNull<Area2D>("InitialIslandWorldLayer/BoardingRampSoftOverlap") is not null,
			["ship_exterior_ready"] = GetNodeOrNull<CanvasItem>("InitialIslandWorldLayer/HubDockedShipExterior") is not null,
			["waterline_boundary_ready"] = GetNodeOrNull<CanvasItem>("InitialIslandWorldLayer/HubWaterlineBoundary") is not null,
			["ui_evidence_allowed_for_scene"] = false,
		};
	}
}
