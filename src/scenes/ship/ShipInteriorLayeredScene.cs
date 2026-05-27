using Godot;

public partial class ShipInteriorLayeredScene : Node2D
{
	[Export]
	public string SceneId { get; set; } = "ship_interior_layered";

	[Export]
	public string RuntimeContractId { get; set; } = "hub_ship_interior";

	[Export]
	public string BoundChartTablePrototypeId { get; set; } = "scene_unit.prototype.chart_table";

	[Export]
	public string BoundChartUiId { get; set; } = "S4_chart";

	[Export]
	public PackedScene? S4ChartScene { get; set; }

	public ChartTable? ChartTableInstance => GetNodeOrNull<ChartTable>("ShipInteriorWorldLayer/ShipInteriorChartTableSocket/ChartTableRuntimeInstance");

	public override void _Ready()
	{
		ChartTableInstance?.SetIdle();
	}

	public Godot.Collections.Dictionary DebugSceneAssetEvidence()
	{
		return new Godot.Collections.Dictionary
		{
			["scene_id"] = SceneId,
			["runtime_contract_id"] = RuntimeContractId,
			["chart_table_instance_ready"] = ChartTableInstance is not null,
			["chart_table_anchor_ready"] = ChartTableInstance?.GetNodeOrNull<Area2D>("ChartTableAnchor") is not null,
			["bound_chart_table_prototype_id"] = BoundChartTablePrototypeId,
			["s4_chart_reference_ready"] = S4ChartScene is not null && GetNodeOrNull<Node>("SceneReferences/S4ChartSceneReference") is not null,
			["bound_chart_ui_id"] = BoundChartUiId,
			["ui_evidence_allowed_for_scene"] = false,
		};
	}
}
