using Godot;

/// <summary>
/// Standalone Godot scene asset for the old market edge future market stop.
/// </summary>
public partial class OldMarketEdgeScene : Node2D
{
	/// <summary>Design-facing scene asset ID.</summary>
	[Export]
	public string SceneId { get; set; } = "old_market_edge_scene";

	/// <summary>Returns concrete scene-asset evidence for smoke tests and QA documentation.</summary>
	public Godot.Collections.Dictionary DebugSceneAssetEvidence()
	{
		return new Godot.Collections.Dictionary
		{
			["scene_id"] = SceneId,
			["world_layer_ready"] = GetNodeOrNull<Node2D>("WorldLayer") is not null,
			["player_spawn_ready"] = GetNodeOrNull<Marker2D>("WorldLayer/PlayerSpawn") is not null,
			["plaza_ready"] = GetNodeOrNull<CanvasItem>("WorldLayer/MarketPlazaGround") is not null,
			["walk_path_ready"] = GetNodeOrNull<CanvasItem>("WorldLayer/MarketWalkPath") is not null,
			["open_stall_ready"] = GetNodeOrNull<CanvasItem>("WorldLayer/GeneralStallBody") is not null,
			["closed_stall_ready"] = GetNodeOrNull<CanvasItem>("WorldLayer/ClosedStallBody") is not null,
			["stall_anchor_ready"] = GetNodeOrNull<Area2D>("WorldLayer/GeneralStallAnchor") is not null,
			["notice_board_ready"] = GetNodeOrNull<CanvasItem>("WorldLayer/MarketNoticeBoard") is not null,
			["cloudsea_boundary_ready"] = GetNodeOrNull<CanvasItem>("WorldLayer/MarketCloudSeaBoundary") is not null,
			["ui_evidence_allowed_for_scene"] = false,
		};
	}
}
