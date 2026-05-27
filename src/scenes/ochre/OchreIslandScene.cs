using Godot;

public partial class OchreIslandScene : Node2D
{
	[Signal]
	public delegate void OreHarvestedEventHandler();

	[Signal]
	public delegate void ReturnDepartureRequestedEventHandler();

	public void RequestOreHarvest() => EmitSignal(SignalName.OreHarvested);

	public void RequestReturnDeparture() => EmitSignal(SignalName.ReturnDepartureRequested);
}
