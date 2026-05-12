// Godot node script — compiled by Godot editor project only.
// Excluded from CloudWeaverVoyage.csproj via <Compile Remove>.
using Godot;
using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Debug;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Godot-side wiring for the session shell scene.
/// Owns the Registry lifecycle and wires it to the diagnostic panel.
/// Boot chain execution is deferred to avoid cross-system calls in _Ready().
/// </summary>
public partial class SessionShell : Node2D
{
    private Registry? _registry;
    private RegistryDiagnosticPanel? _diagnosticPanel;

    public override void _Ready()
    {
        _diagnosticPanel = GetNodeOrNull<RegistryDiagnosticPanel>("RegistryDiagnosticPanel");

        _registry = new Registry();
        _registry.InitializeContent();

        _diagnosticPanel?.Initialize(_registry);

        // Defer boot chain to avoid calling other systems in _Ready()
        CallDeferred(MethodName.RunBootChain);
    }

    private void RunBootChain()
    {
        // SessionBootChain wiring connects here once session lifecycle is integrated.
    }
}
