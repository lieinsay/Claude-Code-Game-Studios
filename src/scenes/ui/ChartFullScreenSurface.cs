using Godot;

public partial class ChartFullScreenSurface : Control
{
	[Signal]
	public delegate void RouteSelectedEventHandler(string routeId);

	[Signal]
	public delegate void DepartureConfirmedEventHandler();

	[Signal]
	public delegate void ChartClosedEventHandler();

	[Export]
	public string BoundChartTablePrototypeId { get; set; } = "scene_unit.prototype.chart_table";

	private Button? routeMistButton;
	private Button? routeOchreButton;
	private Button? confirmButton;
	private Button? returnButton;
	private Label? routeStateLabel;
	private Label? riskSummaryLabel;
	private ColorRect? mistSelectionFrame;
	private ColorRect? ochreSelectionFrame;
	private string selectedRoute = "";

	public override void _Ready()
	{
		routeMistButton = GetNodeOrNull<Button>("Frame/Layout/BodyRow/RouteList/RouteMistButton");
		routeOchreButton = GetNodeOrNull<Button>("Frame/Layout/BodyRow/RouteList/RouteOchreButton");
		confirmButton = GetNodeOrNull<Button>("Frame/Layout/ActionRow/ConfirmDepartureButton");
		returnButton = GetNodeOrNull<Button>("Frame/Layout/ActionRow/ReturnShipButton");
		routeStateLabel = GetNodeOrNull<Label>("Frame/Layout/RouteStateLabel");
		riskSummaryLabel = GetNodeOrNull<Label>("Frame/Layout/BodyRow/RiskSummaryPanel/RiskSummaryLabel");
		mistSelectionFrame = GetNodeOrNull<ColorRect>("Frame/Layout/BodyRow/MapPanel/RouteMistSelectionFrame");
		ochreSelectionFrame = GetNodeOrNull<ColorRect>("Frame/Layout/BodyRow/MapPanel/RouteOchreSelectionFrame");

		if (routeMistButton is not null)
		{
			routeMistButton.Pressed += () => SelectRoute("route.mist");
			routeMistButton.GrabFocus();
		}
		if (routeOchreButton is not null)
		{
			routeOchreButton.Pressed += () => SelectRoute("route.ochre");
		}
		if (confirmButton is not null)
		{
			confirmButton.Pressed += () => EmitSignal(SignalName.DepartureConfirmed);
		}
		if (returnButton is not null)
		{
			returnButton.Pressed += () => EmitSignal(SignalName.ChartClosed);
		}

		SetSelectedRoute("");
	}

	public void SetSelectedRoute(string routeId)
	{
		selectedRoute = routeId;
		var hasMistSelection = selectedRoute == "route.mist";
		var hasOchreSelection = selectedRoute == "route.ochre";
		if (mistSelectionFrame is not null)
		{
			mistSelectionFrame.Visible = hasMistSelection;
		}
		if (ochreSelectionFrame is not null)
		{
			ochreSelectionFrame.Visible = hasOchreSelection;
		}
		if (routeStateLabel is not null)
		{
			routeStateLabel.Text = selectedRoute switch
			{
				"route.mist" => "已选择：雾海短程。确认后进入雾海搜撤。",
				"route.ochre" => "已选择：赭石岛航线。确认后进入赭石岛资源场景。",
				_ => "未选择航线：请选择一条可用目的地。",
			};
		}
		if (riskSummaryLabel is not null)
		{
			riskSummaryLabel.Text = selectedRoute switch
			{
				"route.mist" => "风险摘要：低威胁雾带，短程，适合首轮搜索。确认后不会跳过航行/探索场景。",
				"route.ochre" => "风险摘要：中程资源航线，确认后进入赭石岛采矿与返航闭环。",
				_ => "风险摘要：等待航线选择。不可用路线必须显示原因。",
			};
		}
	}

	private void SelectRoute(string routeId)
	{
		SetSelectedRoute(routeId);
		EmitSignal(SignalName.RouteSelected, routeId);
	}
}
