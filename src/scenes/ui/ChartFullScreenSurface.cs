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
	private Button? confirmButton;
	private Button? returnButton;
	private Label? routeStateLabel;
	private Label? riskSummaryLabel;
	private ColorRect? selectionFrame;
	private string selectedRoute = "";

	public override void _Ready()
	{
		routeMistButton = GetNodeOrNull<Button>("Frame/Layout/BodyRow/RouteList/RouteMistButton");
		confirmButton = GetNodeOrNull<Button>("Frame/Layout/ActionRow/ConfirmDepartureButton");
		returnButton = GetNodeOrNull<Button>("Frame/Layout/ActionRow/ReturnShipButton");
		routeStateLabel = GetNodeOrNull<Label>("Frame/Layout/RouteStateLabel");
		riskSummaryLabel = GetNodeOrNull<Label>("Frame/Layout/BodyRow/RiskSummaryPanel/RiskSummaryLabel");
		selectionFrame = GetNodeOrNull<ColorRect>("Frame/Layout/BodyRow/MapPanel/RouteMistSelectionFrame");

		if (routeMistButton is not null)
		{
			routeMistButton.Pressed += () => SelectRoute("route.mist");
			routeMistButton.GrabFocus();
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
		var hasSelection = selectedRoute == "route.mist";
		if (selectionFrame is not null)
		{
			selectionFrame.Visible = hasSelection;
		}
		if (routeStateLabel is not null)
		{
			routeStateLabel.Text = hasSelection
				? "已选择：雾海短程。确认后进入雾海搜撤。"
				: "未选择航线：请选择一条可用目的地。";
		}
		if (riskSummaryLabel is not null)
		{
			riskSummaryLabel.Text = hasSelection
				? "风险摘要：低威胁雾带，短程，适合首轮搜索。确认后不会跳过航行/探索场景。"
				: "风险摘要：等待航线选择。不可用路线必须显示原因。";
		}
	}

	private void SelectRoute(string routeId)
	{
		SetSelectedRoute(routeId);
		EmitSignal(SignalName.RouteSelected, routeId);
	}
}
