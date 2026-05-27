using Godot;

/// <summary>
/// Standalone Godot scene asset for the Ochre Island resource stop.
/// </summary>
public partial class OchreIslandScene : Node2D
{
	/// <summary>Raised when the local ore anchor asks the runtime owner to harvest ore.</summary>
	[Signal]
	public delegate void OreHarvestedEventHandler();

	/// <summary>Raised when the local return anchor asks the runtime owner to begin departure.</summary>
	[Signal]
	public delegate void ReturnDepartureRequestedEventHandler();

	/// <summary>Emits the local ore-harvest request signal.</summary>
	public void RequestOreHarvest() => EmitSignal(SignalName.OreHarvested);

	/// <summary>Emits the local return-departure request signal.</summary>
	public void RequestReturnDeparture() => EmitSignal(SignalName.ReturnDepartureRequested);
}
