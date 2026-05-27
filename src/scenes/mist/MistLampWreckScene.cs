using Godot;

/// <summary>
/// Independent playable-world scene asset for the quiet mist-lamp wreck destination after the first voyage.
/// </summary>
public partial class MistLampWreckScene : Node2D
{
	/// <summary>Design-facing scene asset ID.</summary>
	[Export]
	public string SceneId { get; set; } = "mist_lamp_wreck_scene";

	/// <summary>Runtime compatibility contract ID used by existing exploration flow, saves, and tests.</summary>
	[Export]
	public string RuntimeContractId { get; set; } = "exploration_mist_island";

	/// <summary>Scene asset ID reached by the return takeoff path.</summary>
	[Export]
	public string ReturnTargetSceneId { get; set; } = "initial_island_scene";

	/// <summary>Runtime contract reached by the return takeoff path.</summary>
	[Export]
	public string ReturnTargetRuntimeContractId { get; set; } = "hub_island_dock";

	/// <summary>Returns concrete scene-asset evidence for smoke tests and QA documentation.</summary>
	public Godot.Collections.Dictionary DebugSceneAssetEvidence()
	{
		return new Godot.Collections.Dictionary
		{
			["scene_id"] = SceneId,
			["runtime_contract_id"] = RuntimeContractId,
			["return_target_scene_id"] = ReturnTargetSceneId,
			["return_target_runtime_contract_id"] = ReturnTargetRuntimeContractId,
			["world_layer_ready"] = GetNodeOrNull<Node2D>("MistLampWorldLayer") is not null,
			["player_spawn_ready"] = GetNodeOrNull<Marker2D>("MistLampWorldLayer/MistLampPlayerStart") is not null,
			["island_mass_ready"] = GetNodeOrNull<CanvasItem>("MistLampWorldLayer/MistIslandMass") is not null,
			["search_wreck_ready"] = GetNodeOrNull<CanvasItem>("MistLampWorldLayer/MistLampWreckBody") is not null,
			["search_anchor_ready"] = GetNodeOrNull<Area2D>("MistLampWorldLayer/MistSearchScanAnchor") is not null,
			["return_ship_ready"] = GetNodeOrNull<CanvasItem>("MistLampWorldLayer/MistReturnShipHull") is not null,
			["return_helm_anchor_ready"] = GetNodeOrNull<Area2D>("MistLampWorldLayer/MistReturnHelmAnchor") is not null,
			["return_takeoff_ready"] = GetNodeOrNull<CanvasItem>("MistLampWorldLayer/MistReturnTakeoffTrail") is not null,
			["water_boundary_ready"] = GetNodeOrNull<CanvasItem>("MistLampWorldLayer/MistWaterBoundary") is not null,
			["island_has_threat_zone"] = GetNodeOrNull<CanvasItem>("MistLampWorldLayer/MistThreatZone") is not null,
			["ui_evidence_allowed_for_scene"] = false,
		};
	}
}
