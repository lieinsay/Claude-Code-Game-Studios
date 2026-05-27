using Godot;

/// <summary>
/// Independent playable-world scene asset for the first voyage route between the island dock and the mist-lamp wreck.
/// </summary>
public partial class VoyageOpenWorldScene : Node2D
{
	/// <summary>Design-facing scene asset ID.</summary>
	[Export]
	public string SceneId { get; set; } = "voyage_open_world_scene";

	/// <summary>Runtime contract ID used for scene physics, smoke tests, and authored scene-unit evidence.</summary>
	[Export]
	public string RuntimeContractId { get; set; } = "voyage_open_world_scene";

	/// <summary>Route ID demonstrated by this first open-world voyage asset.</summary>
	[Export]
	public string RouteId { get; set; } = "route.mist";

	/// <summary>Destination location ID approached by the authored voyage composition.</summary>
	[Export]
	public string DestinationId { get; set; } = "location.mist-short";

	/// <summary>Scene asset ID reached after the current authored route arrives.</summary>
	[Export]
	public string ArrivalSceneId { get; set; } = "mist_lamp_wreck_scene";

	/// <summary>Runtime compatibility contract reached after the current authored route arrives.</summary>
	[Export]
	public string ArrivalRuntimeContractId { get; set; } = "exploration_mist_island";

	/// <summary>Returns concrete scene-asset evidence for smoke tests and QA documentation.</summary>
	public Godot.Collections.Dictionary DebugSceneAssetEvidence()
	{
		return new Godot.Collections.Dictionary
		{
			["scene_id"] = SceneId,
			["runtime_contract_id"] = RuntimeContractId,
			["route_id"] = RouteId,
			["destination_id"] = DestinationId,
			["arrival_scene_id"] = ArrivalSceneId,
			["arrival_runtime_contract_id"] = ArrivalRuntimeContractId,
			["world_layer_ready"] = GetNodeOrNull<Node2D>("VoyageWorldLayer") is not null,
			["takeoff_transition_ready"] = GetNodeOrNull<CanvasItem>("VoyageWorldLayer/VoyageTakeoffTrail") is not null,
			["active_driving_view_ready"] = GetNodeOrNull<CanvasItem>("VoyageWorldLayer/VoyageShipBowForeground") is not null
				&& GetNodeOrNull<CanvasItem>("VoyageWorldLayer/VoyageCockpitWindowFrame") is not null,
			["route_corridor_ready"] = GetNodeOrNull<Area2D>("VoyageWorldLayer/VoyageRouteCorridor") is not null,
			["fog_problem_ready"] = GetNodeOrNull<Area2D>("VoyageWorldLayer/VoyageFogBank") is not null,
			["wreckage_problem_ready"] = GetNodeOrNull<CanvasItem>("VoyageWorldLayer/VoyageWreckageField") is not null,
			["bird_evasion_ready"] = GetNodeOrNull<CanvasItem>("VoyageWorldLayer/VoyageBirdSilhouette") is not null,
			["destination_silhouette_ready"] = GetNodeOrNull<CanvasItem>("VoyageWorldLayer/VoyageDestinationMistLampSilhouette") is not null,
			["retreat_anchor_ready"] = GetNodeOrNull<Area2D>("VoyageWorldLayer/VoyageRetreatBeacon") is not null,
			["ui_evidence_allowed_for_scene"] = false,
		};
	}
}
