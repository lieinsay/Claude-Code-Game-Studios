// Godot node script — compiled by Godot editor project only.
// Excluded from CloudWeaverVoyage.csproj via <Compile Remove>.
using Godot;
using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Debug;

namespace CloudWeaverVoyage.Debug;

/// <summary>
/// Dev-only diagnostic panel overlay for the content registry.
/// Toggle with F12. Only functional in debug builds (OS.IsDebugBuild()).
/// </summary>
public partial class RegistryDiagnosticPanel : CanvasLayer
{
    private RegistryDiagnosticPresenter? _presenter;
    private DiagnosticFilterState _filter = new(null, null, null, null);
    private DiagnosticViewItem? _selectedItem;
    private bool _errorChainOnly;

    // Cached node references — populated in _Ready()
    private ItemList _errorList = null!;
    private RichTextLabel _overviewLabel = null!;
    private RichTextLabel _inspectorLabel = null!;
    private RichTextLabel _graphLabel = null!;
    private LineEdit _queryInput = null!;
    private RichTextLabel _queryResultLabel = null!;
    private Label _statusLabel = null!;
    private OptionButton _severityFilter = null!;
    private OptionButton _kindFilter = null!;
    private OptionButton _domainFilter = null!;

    private static readonly string[] Severities = ["(All)", "fatal", "error", "warning", "info"];
    private static readonly string[] Kinds =
    [
        "(All)", "resource", "cargo", "module", "home-space", "home-anchor",
        "route", "location", "repair-node", "stall-good", "companion", "threat", "intel",
    ];
    private static readonly string[] Domains =
    [
        "(All)", "resources", "airship", "world", "routes", "intel", "companions", "threats",
    ];

    /// <summary>Wires the presenter to the registry. Call after registry is initialized.</summary>
    public void Initialize(Registry registry)
    {
        if (registry is null)
        {
            return;
        }

        _presenter = new RegistryDiagnosticPresenter(registry);
    }

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            QueueFree();
            return;
        }

        _errorList = GetNode<ItemList>("%ErrorList");
        _overviewLabel = GetNode<RichTextLabel>("%OverviewLabel");
        _inspectorLabel = GetNode<RichTextLabel>("%InspectorLabel");
        _graphLabel = GetNode<RichTextLabel>("%GraphLabel");
        _queryInput = GetNode<LineEdit>("%QueryInput");
        _queryResultLabel = GetNode<RichTextLabel>("%QueryResultLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _severityFilter = GetNode<OptionButton>("%SeverityFilter");
        _kindFilter = GetNode<OptionButton>("%KindFilter");
        _domainFilter = GetNode<OptionButton>("%DomainFilter");

        PopulateFilterOptions();
        WireSignals();

        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Keycode != Key.F12)
        {
            return;
        }

        Visible = !Visible;
        if (Visible)
        {
            RefreshAll();
        }

        GetViewport().SetInputAsHandled();
    }

    // -------------------------------------------------------------------------
    // Signal handlers
    // -------------------------------------------------------------------------

    private void OnClosePressed()
    {
        Visible = false;
    }

    private void OnFilterChanged(long _index)
    {
        ApplyFilters();
        RefreshErrorList();
    }

    private void OnErrorListItemSelected(long index)
    {
        var items = _presenter?.BuildErrorList(_filter);
        if (items is null || index < 0 || index >= items.Count)
        {
            return;
        }

        _selectedItem = items[(int)index];
        RefreshInspector();
    }

    private void OnErrorChainToggled(bool pressed)
    {
        _errorChainOnly = pressed;
        RefreshGraph();
    }

    private void OnRunQueryPressed()
    {
        if (_presenter is null)
        {
            return;
        }

        var result = _presenter.ExecuteQuery(_queryInput.Text, null, null);
        _queryResultLabel.Text = $"[{result.Status}] {result.Summary}";
    }

    private void OnCopySinglePressed()
    {
        if (_selectedItem is null || _presenter is null)
        {
            _statusLabel.Text = "No error selected";
            return;
        }

        DisplayServer.ClipboardSet(_presenter.FormatSingleErrorReport(_selectedItem));
        _statusLabel.Text = "Copied to clipboard";
    }

    private void OnBatchCopyPressed()
    {
        if (_presenter is null)
        {
            return;
        }

        var items = _presenter.BuildErrorList(_filter);
        DisplayServer.ClipboardSet(_presenter.FormatBatchReport(items));
        _statusLabel.Text = $"Copied {items.Count} errors to clipboard";
    }

    // -------------------------------------------------------------------------
    // Panel refresh
    // -------------------------------------------------------------------------

    private void RefreshAll()
    {
        if (_presenter is null)
        {
            _overviewLabel.Text = "[Not initialized — call Initialize(registry) first]";
            return;
        }

        _overviewLabel.Text = _presenter.BuildOverviewText();
        ApplyFilters();
        RefreshErrorList();
        RefreshGraph();
    }

    private void RefreshErrorList()
    {
        _errorList.Clear();
        if (_presenter is null)
        {
            return;
        }

        var items = _presenter.BuildErrorList(_filter);
        if (items.Count == 0)
        {
            _errorList.AddItem("(No errors)");
            return;
        }

        foreach (var item in items)
        {
            _errorList.AddItem($"[{item.Severity.ToUpperInvariant()}] {item.ContentId} — {item.ErrorCode}");
        }
    }

    private void RefreshInspector()
    {
        if (_selectedItem is null || _presenter is null)
        {
            _inspectorLabel.Text = "(Select an error to inspect)";
            return;
        }

        var data = _presenter.BuildInspectorData(_selectedItem.ContentId);
        if (data is null)
        {
            _inspectorLabel.Text = $"No inspector data for {_selectedItem.ContentId}";
            return;
        }

        _inspectorLabel.Text = string.Join('\n',
            $"event_id:     {data.EventId}",
            $"severity:     {data.Severity}",
            $"error_code:   {data.ErrorCode}",
            $"content_id:   {data.ContentId}",
            $"kind:         {data.Kind}",
            $"status:       {data.Status}",
            $"domain:       {data.OwnerDomain}",
            $"source_ref:   {data.SourceRef}",
            $"field_path:   {data.FieldPath}",
            $"blocking:     {data.BlockingScope}",
            $"suggestion:   {data.SuggestedAction}");
    }

    private void RefreshGraph()
    {
        if (_presenter is null)
        {
            return;
        }

        var graph = _presenter.BuildReferenceGraph(_errorChainOnly);
        if (graph.Nodes.Count == 0)
        {
            _graphLabel.Text = _errorChainOnly
                ? "(No error chains — registry is clean)"
                : "(No content loaded)";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var node in graph.Nodes)
        {
            var marker = node.HasError ? "[ERR] " : "      ";
            var refs = node.RefTargets.Count > 0 ? $" → {string.Join(", ", node.RefTargets)}" : string.Empty;
            sb.AppendLine($"{marker}{node.ContentId} [{node.Status}]{refs}");
        }

        _graphLabel.Text = sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------------------------

    private void PopulateFilterOptions()
    {
        foreach (var s in Severities)
        {
            _severityFilter.AddItem(s);
        }

        foreach (var k in Kinds)
        {
            _kindFilter.AddItem(k);
        }

        foreach (var d in Domains)
        {
            _domainFilter.AddItem(d);
        }
    }

    private void ApplyFilters()
    {
        var severity = _severityFilter.GetItemText(_severityFilter.Selected);
        var kind = _kindFilter.GetItemText(_kindFilter.Selected);
        var domain = _domainFilter.GetItemText(_domainFilter.Selected);

        _filter = new DiagnosticFilterState(
            Severity: severity.StartsWith('(') ? null : severity,
            Kind: kind.StartsWith('(') ? null : kind,
            Domain: domain.StartsWith('(') ? null : domain,
            ErrorCode: null);
    }

    private void WireSignals()
    {
        GetNode<Button>("%CloseButton").Pressed += OnClosePressed;
        _severityFilter.ItemSelected += OnFilterChanged;
        _kindFilter.ItemSelected += OnFilterChanged;
        _domainFilter.ItemSelected += OnFilterChanged;
        _errorList.ItemSelected += OnErrorListItemSelected;
        GetNode<CheckButton>("%ErrorChainToggle").Toggled += OnErrorChainToggled;
        GetNode<Button>("%RunQueryButton").Pressed += OnRunQueryPressed;
        GetNode<Button>("%CopySingleButton").Pressed += OnCopySinglePressed;
        GetNode<Button>("%BatchCopyButton").Pressed += OnBatchCopyPressed;
    }
}
