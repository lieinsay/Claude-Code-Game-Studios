using Godot;

public partial class BandedIronOre : Node2D
{
    [Signal]
    public delegate void OreHarvestRequestedEventHandler();

    [Export]
    public bool Harvested { get; set; }

    public void MarkHarvested()
    {
        Harvested = true;
        EmitSignal(SignalName.OreHarvestRequested);
    }
}
