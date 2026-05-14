using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #16 Story 004: Upstream Data Contracts & Domain Integration ===");
var failed = 0;
var total = 0;

Run("AC-1: _ready phase has no upstream calls", Ac1ReadyNoUpstreamCalls);
Run("AC-2: ui_ready connects HUD signals, preloads S7, and shows S1", Ac2UiReadySequence);
Run("AC-3: S4 binds chart, filter, visible routes, and selected-route data with snapshot semantics", Ac3ChartBindingSnapshot);
Run("AC-4: S7 binds combat threat context on open", Ac4CombatBinding);
Run("AC-5: S8 binds repair state with node context", Ac5RepairBinding);
Run("AC-6: S9 binds stall data with stall context", Ac6MarketBinding);
Run("AC-7: S10 binds partner name and naming eligibility", Ac7NamingBinding);
Run("AC-8: S11 filters sniff items by cat signature", Ac8SniffFiltering);
Run("AC-9: S11 shows empty state when no sniffable items exist", Ac9SniffEmptyState);
Run("AC-10: S12 shows empty state when storage is empty", Ac10StorageEmptyState);
Run("AC-11: S4 shows empty state when no routes are visible", Ac11ChartEmptyState);
Run("AC-12: S1 degrades gracefully when resources are unavailable", Ac12ResourcesUnavailable);
Run("AC-13: Registry display-name failure falls back to item id", Ac13RegistryFallback);
Run("AC-14: S11 degrades gracefully when partner system is unavailable", Ac14PartnerUnavailable);
Run("AC-15: capacity choice writes back through ResourcesManager command", Ac15CapacityWriteBack);
Run("AC-16: repair submission writes back through WorldRepair command", Ac16RepairWriteBack);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 004 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 004 validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
    total++;
    try
    {
        if (test())
        {
            Console.WriteLine($"[PASS] {label}");
            return;
        }
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

static bool Ac1ReadyNoUpstreamCalls()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);

    return ui.ReadyPhaseActions.SequenceEqual(new[] { "constant_init", "register_screen_registry", "declare_signals" })
        && upstream.Calls.Count == 0
        && ui.LastPanelBindingSnapshot is null;
}

static bool Ac2UiReadySequence()
{
    var ui = CreateUi(new FakeUpstreamDataSource());

    return ui.HudSignalConnectionCount == 11
        && ui.CombatPanelPreloaded
        && ui.HubHudVisible
        && !ui.ExplorationHudVisible
        && ui.UiReadyPhaseActions.Contains($"preload:{UIManager.CombatScreenId}")
        && ui.UiReadyPhaseActions.Contains($"show:{UIManager.HubHudScreenId}")
        && ui.UiReadyPhaseActions.Contains($"hide:{UIManager.ExplorationHudScreenId}");
}

static bool Ac3ChartBindingSnapshot()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);

    var opened = ui.PressMapKey();
    var first = Binding(ui);
    upstream.VisibleRoutes.Add(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["id"] = "route.later",
    });
    var firstAfterMutation = Binding(ui);
    var routeSelected = ui.SelectRoute("route.sky-reef");
    var selected = Binding(ui);
    ui.PressEscape();
    ui.PressMapKey();
    var second = Binding(ui);

    return opened == ScreenResult.Success
        && first.PanelId == UIManager.ChartScreenId
        && first.QueryNames.SequenceEqual(new[] { "get_chart_state", "get_visible_routes", "get_filter_state" })
        && first.Fields["filter_hide_rumored"] == "false"
        && first.Fields["visible_route_count"] == "1"
        && firstAfterMutation.Fields["visible_route_count"] == "1"
        && routeSelected == ScreenResult.Success
        && selected.QueryNames.SequenceEqual(new[] { "get_chart_state", "get_visible_routes", "get_filter_state", "get_selected_route" })
        && selected.Fields["selected_id"] == "route.sky-reef"
        && selected.Fields["selected_name"] == "裂云礁航线"
        && second.Fields["visible_route_count"] == "2";
}

static bool Ac4CombatBinding()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);

    return ui.OpenModalPanel(UIManager.CombatScreenId) == ModalResult.Success
        && Binding(ui).QueryNames.SequenceEqual(new[] { "build_threat_context" })
        && Binding(ui).Fields["threat_name"] == "裂帆影";
}

static bool Ac5RepairBinding()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);

    return ui.OpenRepairPanelForNode("beacon_02") == ModalResult.Success
        && Binding(ui).QueryNames.SequenceEqual(new[] { "get_repair_state" })
        && Binding(ui).Fields["node_id"] == "beacon_02"
        && Binding(ui).Fields["node_name"] == "二号雾灯";
}

static bool Ac6MarketBinding()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);

    var result = ui.OpenModalPanel(
        UIManager.MarketScreenId,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stall_id"] = "stall_weaver",
        });

    return result == ModalResult.Success
        && Binding(ui).QueryNames.SequenceEqual(new[] { "get_stall_data" })
        && Binding(ui).Fields["stall_id"] == "stall_weaver"
        && Binding(ui).Fields["npc_name"] == "织绳婆婆";
}

static bool Ac7NamingBinding()
{
    var ui = CreateUi(new FakeUpstreamDataSource());

    return ui.OpenModalPanel(UIManager.NamingScreenId) == ModalResult.Success
        && Binding(ui).QueryNames.SequenceEqual(new[] { "query_partner_name", "naming_prompt_eligibility" })
        && Binding(ui).Fields["partner_name"] == "灰白猫"
        && Binding(ui).Fields["naming_eligible"] == "True";
}

static bool Ac8SniffFiltering()
{
    var ui = CreateUi(new FakeUpstreamDataSource());

    return ui.OpenNonModal(UIManager.PartnerSniffScreenId) == ScreenResult.Success
        && Binding(ui).QueryNames.SequenceEqual(new[] { "get_sniff_items", "get_display_name" })
        && Binding(ui).RenderedItemIds.SequenceEqual(new[] { "item.saltcloth" })
        && Binding(ui).Fields["item_count"] == "1";
}

static bool Ac9SniffEmptyState()
{
    var upstream = new FakeUpstreamDataSource();
    upstream.SniffItems.Clear();
    var ui = CreateUi(upstream);

    return ui.OpenNonModal(UIManager.PartnerSniffScreenId) == ScreenResult.Success
        && Binding(ui).EmptyStateVisible
        && Binding(ui).EmptyStateMessage == UIManager.PartnerSniffEmptyStateMessage;
}

static bool Ac10StorageEmptyState()
{
    var upstream = new FakeUpstreamDataSource { StorageCurrent = 0 };
    var ui = CreateUi(upstream);

    return ui.OpenNonModal(UIManager.StorageScreenId) == ScreenResult.Success
        && Binding(ui).QueryNames.SequenceEqual(new[] { "get_storage_state", "get_cargo_state" })
        && Binding(ui).Fields["cargo_current"] == "1"
        && Binding(ui).EmptyStateMessage == UIManager.StorageEmptyStateMessage;
}

static bool Ac11ChartEmptyState()
{
    var upstream = new FakeUpstreamDataSource();
    upstream.VisibleRoutes.Clear();
    var ui = CreateUi(upstream);

    return ui.PressMapKey() == ScreenResult.Success
        && Binding(ui).EmptyStateVisible
        && Binding(ui).EmptyStateMessage == UIManager.ChartEmptyStateMessage;
}

static bool Ac12ResourcesUnavailable()
{
    var upstream = new FakeUpstreamDataSource { ResourcesUnavailable = true };
    var ui = CreateUi(upstream);
    var snapshot = ui.BindScreenData(UIManager.HubHudScreenId);

    return snapshot.QueryNames.Contains("get_storage_state")
        && snapshot.QueryNames.Contains("get_cargo_state")
        && snapshot.Fields["storage_display"] == UIManager.MissingResourceDisplay
        && snapshot.Fields["cargo_display"] == UIManager.MissingResourceDisplay;
}

static bool Ac13RegistryFallback()
{
    var upstream = new FakeUpstreamDataSource { DisplayNameThrows = true };
    var ui = CreateUi(upstream);

    return ui.OpenNonModal(UIManager.PartnerSniffScreenId) == ScreenResult.Success
        && Binding(ui).UsedDisplayNameFallback
        && Binding(ui).Fields["item:item.saltcloth:name"] == "item.saltcloth";
}

static bool Ac14PartnerUnavailable()
{
    var upstream = new FakeUpstreamDataSource { PartnerUnavailable = true };
    var ui = CreateUi(upstream);

    return ui.OpenNonModal(UIManager.PartnerSniffScreenId) == ScreenResult.Success
        && Binding(ui).EmptyStateVisible
        && Binding(ui).EmptyStateMessage == UIManager.PartnerUnavailableMessage;
}

static bool Ac15CapacityWriteBack()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);
    ui.OpenModalPanel(
        UIManager.CapacityChoiceScreenId,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["item_id"] = "item_A",
            ["decision"] = "pending",
        });
    var before = ui.GetPanelSnapshot(UIManager.CapacityChoiceScreenId);
    var command = ui.ConfirmCapacityTransfer("item_A", UIManager.CarriedPoolId, UIManager.StoragePoolId, 1);
    var after = ui.GetPanelSnapshot(UIManager.CapacityChoiceScreenId);

    return command.Success
        && command.MethodName == "transfer_item"
        && upstream.Commands.SequenceEqual(new[] { "transfer_item:item_A:CARRIED:STORAGE:1" })
        && before is not null
        && after is not null
        && before.DataContext["decision"] == after.DataContext["decision"];
}

static bool Ac16RepairWriteBack()
{
    var upstream = new FakeUpstreamDataSource();
    var ui = CreateUi(upstream);
    ui.OpenRepairPanelForNode("beacon_02");
    var command = ui.SubmitRepairMaterials(
        "beacon_02",
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["item.saltcloth"] = 2,
        });

    return command.Success
        && command.MethodName == "submit_repair"
        && command.Arguments["node_id"] == "beacon_02"
        && upstream.Commands.SequenceEqual(new[] { "submit_repair:beacon_02:item.saltcloth=2" });
}

static UIManager CreateUi(FakeUpstreamDataSource upstream)
{
    var ui = new UIManager(upstream);
    ui.Initialize();
    return ui;
}

static PanelBindingSnapshot Binding(UIManager ui)
{
    return ui.LastPanelBindingSnapshot
        ?? throw new InvalidOperationException("Expected a panel binding snapshot.");
}

sealed class FakeUpstreamDataSource : IUiUpstreamDataSource
{
    public List<string> Calls { get; } = new();

    public List<string> Commands { get; } = new();

    public List<IReadOnlyDictionary<string, string>> VisibleRoutes { get; } = new()
    {
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "route.sky-reef",
        },
    };

    public List<IReadOnlyDictionary<string, string>> SniffItems { get; } = new()
    {
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "item.saltcloth",
            ["cat_sniff_signature"] = "rain,old-rope",
        },
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "item.blank",
            ["cat_sniff_signature"] = "",
        },
    };

    public int StorageCurrent { get; set; } = 3;

    public bool ResourcesUnavailable { get; set; }

    public bool PartnerUnavailable { get; set; }

    public bool DisplayNameThrows { get; set; }

    public IReadOnlyDictionary<string, string>? GetChartState()
    {
        Calls.Add("get_chart_state");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["state"] = "BROWSING",
        };
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetVisibleRoutes()
    {
        Calls.Add("get_visible_routes");
        return VisibleRoutes.ToArray();
    }

    public IReadOnlyDictionary<string, string>? GetSelectedRoute()
    {
        Calls.Add("get_selected_route");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "route.sky-reef",
            ["name"] = "裂云礁航线",
            ["known_risks"] = "2",
            ["source_count"] = "1",
        };
    }

    public IReadOnlyDictionary<string, string>? GetFilterState()
    {
        Calls.Add("get_filter_state");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["hide_rumored"] = "false",
        };
    }

    public IReadOnlyDictionary<string, string>? GetHullIntegrity()
    {
        Calls.Add("get_hull_integrity");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["current"] = "82",
            ["max"] = "100",
        };
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates()
    {
        Calls.Add("get_module_states");
        return new[]
        {
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["slot_id"] = "0",
                ["installed"] = "true",
            },
        };
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory()
    {
        Calls.Add("get_carried_inventory");
        return Array.Empty<IReadOnlyDictionary<string, string>>();
    }

    public IReadOnlyDictionary<string, string>? GetStorageState()
    {
        Calls.Add("get_storage_state");
        if (ResourcesUnavailable)
        {
            throw new NullReferenceException("Resources unavailable");
        }

        return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["current"] = StorageCurrent.ToString(),
                ["max"] = "10",
            };
    }

    public IReadOnlyDictionary<string, string>? GetCargoState()
    {
        Calls.Add("get_cargo_state");
        if (ResourcesUnavailable)
        {
            throw new NullReferenceException("Resources unavailable");
        }

        return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["current"] = "1",
                ["max"] = "5",
            };
    }

    public int? GetCurrency()
    {
        Calls.Add("get_currency");
        return ResourcesUnavailable ? null : 12;
    }

    public IReadOnlyDictionary<string, string>? GetSearchProgress()
    {
        Calls.Add("get_search_progress");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["searched"] = "1",
            ["total"] = "6",
        };
    }

    public string? GetScoutPreviewLevel()
    {
        Calls.Add("get_scout_preview_level");
        return UIManager.ScoutPreviewPresence;
    }

    public IReadOnlyDictionary<string, string>? GetExtractionState()
    {
        Calls.Add("get_extraction_state");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["extraction_progress"] = "0.5",
            ["is_interrupted"] = "false",
        };
    }

    public IReadOnlyDictionary<string, string>? BuildThreatContext()
    {
        Calls.Add("build_threat_context");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["threat_name"] = "裂帆影",
            ["description"] = "从云层边缘扑来",
        };
    }

    public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId)
    {
        Calls.Add($"get_repair_state:{nodeId}");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["node_id"] = nodeId,
            ["node_name"] = "二号雾灯",
        };
    }

    public IReadOnlyDictionary<string, string>? GetStallData(string stallId)
    {
        Calls.Add($"get_stall_data:{stallId}");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stall_id"] = stallId,
            ["npc_name"] = "织绳婆婆",
        };
    }

    public string? QueryPartnerName()
    {
        Calls.Add("query_partner_name");
        return PartnerUnavailable ? null : "灰白猫";
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems()
    {
        Calls.Add("get_sniff_items");
        return PartnerUnavailable ? null : SniffItems.ToArray();
    }

    public bool? NamingPromptEligibility()
    {
        Calls.Add("naming_prompt_eligibility");
        return PartnerUnavailable ? null : true;
    }

    public string? GetDisplayName(string entityId)
    {
        Calls.Add($"get_display_name:{entityId}");
        if (DisplayNameThrows)
        {
            throw new NullReferenceException("Registry unavailable");
        }

        return entityId == "item.saltcloth" ? "盐帆布" : "";
    }

    public string? GetDescription(string entityId)
    {
        Calls.Add($"get_description:{entityId}");
        return $"description:{entityId}";
    }

    public bool TransferItem(string itemId, string fromPool, string toPool, int quantity)
    {
        Commands.Add($"transfer_item:{itemId}:{fromPool}:{toPool}:{quantity}");
        return true;
    }

    public bool DiscardItem(string itemId)
    {
        Commands.Add($"discard_item:{itemId}");
        return true;
    }

    public bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials)
    {
        Commands.Add($"submit_repair:{nodeId}:{string.Join(",", materials.Select(item => $"{item.Key}={item.Value}"))}");
        return true;
    }

    public bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost)
    {
        Commands.Add($"execute_purchase:{stallId}:{goodId}:{quantity}:{totalCost}");
        return true;
    }

    public bool SubmitPartnerName(string partnerId, string name)
    {
        Commands.Add($"submit_partner_name:{partnerId}:{name}");
        return true;
    }
}
