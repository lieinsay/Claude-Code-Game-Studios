using Godot;

public partial class ChartTable : Node2D
{
	[Signal]
	public delegate void ChartOpenRequestedEventHandler();

	[Export]
	public bool ChartAvailable { get; set; } = true;

	private CanvasItem? focusHighlight;
	private CanvasItem? disabledOverlay;
	private CanvasItem? projectionGlow;
	private Label? stateLabel;

	public override void _Ready()
	{
		focusHighlight = GetNodeOrNull<CanvasItem>("FocusHighlight");
		disabledOverlay = GetNodeOrNull<CanvasItem>("DisabledOverlay");
		projectionGlow = GetNodeOrNull<CanvasItem>("ProjectionGlow");
		stateLabel = GetNodeOrNull<Label>("StateLabel");
		SetIdle();
	}

	public void SetIdle()
	{
		SetVisualState("idle");
	}

	public void SetFocused()
	{
		SetVisualState(ChartAvailable ? "focused" : "disabled");
	}

	public void SetChartOpen()
	{
		SetVisualState("chart_open");
	}

	public void SetDisabled()
	{
		ChartAvailable = false;
		SetVisualState("disabled");
	}

	public void RequestOpenChart()
	{
		if (!ChartAvailable)
		{
			SetDisabled();
			return;
		}

		SetChartOpen();
		EmitSignal(SignalName.ChartOpenRequested);
	}

	private void SetVisualState(string state)
	{
		if (focusHighlight is not null)
		{
			focusHighlight.Visible = state == "focused" || state == "chart_open";
		}
		if (disabledOverlay is not null)
		{
			disabledOverlay.Visible = state == "disabled";
		}
		if (projectionGlow is not null)
		{
			projectionGlow.Visible = state == "focused" || state == "chart_open";
		}
		if (stateLabel is not null)
		{
			stateLabel.Text = state switch
			{
				"focused" => "航图台：可打开",
				"chart_open" => "航图台：航图展开中",
				"disabled" => "航图台：航线系统不可用",
				_ => "航图台：待规划",
			};
		}
	}
}
