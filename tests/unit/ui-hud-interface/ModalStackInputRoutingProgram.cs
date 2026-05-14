using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #16 Story 002: Modal Stack, Combat Override & Input Routing ===");
var failed = 0;
var total = 0;

Run("AC-1: S8 opens as single modal and blocks movement", Ac1RepairModalBlocksMovement);
Run("AC-2: second non-combat modal is rejected with toast", Ac2SecondModalRejected);
Run("AC-3: Esc closes modal and restores previous focus", Ac3CloseModalRestoresFocus);
Run("AC-4: S7 overrides S6a and preserves context", Ac4CombatOverrideSavesContext);
Run("AC-5: resolved combat restores overridden modal", Ac5CombatResolutionRestoresModal);
Run("AC-6: retreat discards overridden modal", Ac6RetreatDiscardsModal);
Run("AC-7: S7 opens normally without override state", Ac7CombatOpensWithoutOverride);
Run("AC-8: S10 opens normally when no modal exists", Ac8NamingOpensNormally);
Run("AC-9: S10 queues and opens after S6c closes", Ac9NamingQueuesAfterSettlement);
Run("AC-10: S11 and S12 coexist with increasing z-index", Ac10NonModalCoexistence);
Run("AC-11: Esc closes non-modal panels in LIFO order", Ac11NonModalEscLifo);
Run("AC-12: modal layer consumes movement", Ac12ModalConsumesMovement);
Run("AC-13: semi-modal layer consumes movement", Ac13SemiModalConsumesMovement);
Run("AC-14: non-modal layer allows movement", Ac14NonModalAllowsMovement);
Run("AC-15: HUD overlay click passes through except inventory slots", Ac15HudMouseFilters);
Run("AC-16: world layer allows movement and Use", Ac16WorldAllowsMovementAndUse);
Run("AC-17: Esc closes normal modal", Ac17EscClosesNormalModal);
Run("AC-18: Esc on S7 is consumed with prompt", Ac18EscOnCombatRequiresChoice);
Run("AC-19: Esc on S10 skips naming", Ac19EscOnNamingSkips);
Run("AC-20: M opens chart only from unblocked Hub world layer", Ac20MapKeyLayerGate);
Run("AC-21: Tab cycles inside the active modal", Ac21TabCyclesInsideModal);
Run("AC-22: mouse press synchronizes keyboard focus", Ac22MousePressGrabsFocus);
Run("AC-23: focus and hover visual styles are distinct", Ac23FocusHoverStylesDistinct);
Run("AC-24: read-only label is skipped by Tab traversal", Ac24ReadonlySkipped);
Run("AC-25: opening modal focuses first interactable", Ac25ModalAutoFocus);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 002 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 002 validation passed: {total}/{total} checks passed.");
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

static bool Ac1RepairModalBlocksMovement()
{
    var ui = CreateUi();

    return ui.OpenModalPanel(UIManager.RepairScreenId) == ModalResult.Success
        && ui.CurrentModalId == UIManager.RepairScreenId
        && ui.ModalMaskVisible
        && ui.ActiveInputLayer == InputLayer.Modal
        && ui.IsMovementInputBlocked();
}

static bool Ac2SecondModalRejected()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.RepairScreenId);

    return ui.OpenModalPanel(UIManager.MarketScreenId) == ModalResult.ErrAnotherModalOpen
        && ui.CurrentModalId == UIManager.RepairScreenId
        && !ui.IsPanelVisible(UIManager.MarketScreenId)
        && ui.LastToastMessage == UIManager.CurrentActionUnavailableToast;
}

static bool Ac3CloseModalRestoresFocus()
{
    var ui = CreateUi();
    ui.SetKeyboardFocus("hub.helm");
    ui.OpenModalPanel(UIManager.RepairScreenId);

    return ui.PressEscape() == ScreenResult.Success
        && !ui.IsModalOpen()
        && ui.CurrentModalId == string.Empty
        && ui.KeyboardFocusElementId == "hub.helm"
        && !ui.IsMovementInputBlocked();
}

static bool Ac4CombatOverrideSavesContext()
{
    var ui = CreateUi();
    var context = new Dictionary<string, string>
    {
        ["batch_id"] = "loot-choice-01",
        ["item_id"] = "goods.scrap"
    };
    ui.OpenModalPanel(UIManager.CapacityChoiceScreenId, context, scrollOffset: 42, selectedIndex: 3);

    return ui.OpenModalPanel(UIManager.CombatScreenId) == ModalResult.Success
        && ui.CurrentModalId == UIManager.CombatScreenId
        && ui.HasCombatOverrideSnapshot
        && ui.IsPanelVisible(UIManager.CapacityChoiceScreenId)
        && ui.IsPanelVisible(UIManager.CombatScreenId)
        && Snapshot(ui, UIManager.CapacityChoiceScreenId).DataContext["batch_id"] == "loot-choice-01"
        && Snapshot(ui, UIManager.CapacityChoiceScreenId).ScrollOffset == 42
        && Snapshot(ui, UIManager.CapacityChoiceScreenId).SelectedIndex == 3
        && !Snapshot(ui, UIManager.CapacityChoiceScreenId).InputEnabled
        && Math.Abs(Snapshot(ui, UIManager.CapacityChoiceScreenId).Opacity - 0.2) < 0.001
        && Snapshot(ui, UIManager.CombatScreenId).CanvasLayer == UIManager.CombatOverrideCanvasLayer;
}

static bool Ac5CombatResolutionRestoresModal()
{
    var emergency = CapacityChoiceWithCombat();
    var holdGround = CapacityChoiceWithCombat();

    return emergency.ResolveCombatThreat(CombatThreatResolution.EmergencyTreatment) == ModalResult.Success
        && emergency.CurrentModalId == UIManager.CapacityChoiceScreenId
        && Snapshot(emergency, UIManager.CapacityChoiceScreenId).InputEnabled
        && Math.Abs(Snapshot(emergency, UIManager.CapacityChoiceScreenId).Opacity - 1.0) < 0.001
        && Snapshot(emergency, UIManager.CapacityChoiceScreenId).SelectedIndex == 3
        && !emergency.HasCombatOverrideSnapshot
        && holdGround.ResolveCombatThreat(CombatThreatResolution.HoldGround) == ModalResult.Success
        && holdGround.CurrentModalId == UIManager.CapacityChoiceScreenId
        && Snapshot(holdGround, UIManager.CapacityChoiceScreenId).DataContext["batch_id"] == "loot-choice-01";
}

static bool Ac6RetreatDiscardsModal()
{
    var ui = CapacityChoiceWithCombat();

    return ui.ResolveCombatThreat(CombatThreatResolution.Retreat) == ModalResult.Success
        && !ui.IsModalOpen()
        && !ui.IsPanelVisible(UIManager.CapacityChoiceScreenId)
        && !ui.IsPanelVisible(UIManager.CombatScreenId)
        && !ui.HasCombatOverrideSnapshot;
}

static bool Ac7CombatOpensWithoutOverride()
{
    var ui = CreateUi();

    return ui.OpenModalPanel(UIManager.CombatScreenId) == ModalResult.Success
        && ui.CurrentModalId == UIManager.CombatScreenId
        && !ui.HasCombatOverrideSnapshot
        && Snapshot(ui, UIManager.CombatScreenId).CanvasLayer == UIManager.CombatOverrideCanvasLayer
        && ui.ResolveCombatThreat(CombatThreatResolution.HoldGround) == ModalResult.Success
        && !ui.IsModalOpen();
}

static bool Ac8NamingOpensNormally()
{
    var ui = CreateUi();

    return ui.OpenModalPanel(UIManager.NamingScreenId) == ModalResult.Success
        && ui.CurrentModalId == UIManager.NamingScreenId
        && ui.PendingModalId == string.Empty;
}

static bool Ac9NamingQueuesAfterSettlement()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.SettlementSummaryScreenId);

    return ui.OpenModalPanel(UIManager.NamingScreenId) == ModalResult.ErrQueued
        && ui.CurrentModalId == UIManager.SettlementSummaryScreenId
        && ui.PendingModalId == UIManager.NamingScreenId
        && ui.PressEscape() == ScreenResult.Success
        && ui.CurrentModalId == UIManager.NamingScreenId
        && ui.PendingModalId == string.Empty;
}

static bool Ac10NonModalCoexistence()
{
    var ui = CreateUi();

    return ui.OpenNonModal(UIManager.PartnerSniffScreenId) == ScreenResult.Success
        && ui.OpenNonModal(UIManager.StorageScreenId) == ScreenResult.Success
        && ui.IsPanelVisible(UIManager.PartnerSniffScreenId)
        && ui.IsPanelVisible(UIManager.StorageScreenId)
        && ui.GetPanelZIndex(UIManager.StorageScreenId) > ui.GetPanelZIndex(UIManager.PartnerSniffScreenId)
        && !ui.IsMovementInputBlocked();
}

static bool Ac11NonModalEscLifo()
{
    var ui = CreateUi();
    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    ui.OpenNonModal(UIManager.StorageScreenId);

    return ui.PressEscape() == ScreenResult.Success
        && ui.IsPanelVisible(UIManager.PartnerSniffScreenId)
        && !ui.IsPanelVisible(UIManager.StorageScreenId)
        && ui.PressEscape() == ScreenResult.Success
        && !ui.IsPanelVisible(UIManager.PartnerSniffScreenId);
}

static bool Ac12ModalConsumesMovement()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.RepairScreenId);

    return ui.GetActiveInputLayer() == InputLayer.Modal
        && ui.IsMovementInputBlocked();
}

static bool Ac13SemiModalConsumesMovement()
{
    var ui = ExplorationUi();
    ui.ExtractionStarted();

    return ui.GetActiveInputLayer() == InputLayer.SemiModal
        && ui.IsMovementInputBlocked();
}

static bool Ac14NonModalAllowsMovement()
{
    var ui = CreateUi();
    ui.OpenNonModal(UIManager.StationDetailScreenId);

    return ui.GetActiveInputLayer() == InputLayer.NonModal
        && !ui.IsMovementInputBlocked();
}

static bool Ac15HudMouseFilters()
{
    var ui = CreateUi();

    return ui.GetActiveInputLayer(pointerOverHud: true) == InputLayer.Hud
        && ui.GetHudMouseFilter(HudRegion.Overlay) == MouseFilterMode.Ignore
        && ui.DoesHudClickReachWorld(HudRegion.Overlay)
        && ui.GetHudMouseFilter(HudRegion.InventorySlot) == MouseFilterMode.Stop
        && !ui.DoesHudClickReachWorld(HudRegion.InventorySlot);
}

static bool Ac16WorldAllowsMovementAndUse()
{
    var ui = CreateUi();

    return ui.GetActiveInputLayer() == InputLayer.World
        && !ui.IsMovementInputBlocked()
        && !ui.IsWorldUseInputBlocked();
}

static bool Ac17EscClosesNormalModal()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.MarketScreenId);

    return ui.PressEscape() == ScreenResult.Success
        && !ui.IsModalOpen()
        && !ui.IsPanelVisible(UIManager.MarketScreenId);
}

static bool Ac18EscOnCombatRequiresChoice()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.CombatScreenId);
    var escResult = ui.PressEscape();
    ui.CloseModal();

    return escResult == ScreenResult.Success
        && ui.CurrentModalId == UIManager.CombatScreenId
        && ui.IsPanelVisible(UIManager.CombatScreenId)
        && ui.LastVisualPrompt == UIManager.CombatResponseRequiredPrompt;
}

static bool Ac19EscOnNamingSkips()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.NamingScreenId);

    return ui.PressEscape() == ScreenResult.Success
        && ui.NamingSkipped
        && !ui.IsModalOpen();
}

static bool Ac20MapKeyLayerGate()
{
    var modal = CreateUi();
    modal.OpenModalPanel(UIManager.RepairScreenId);
    var nonModal = CreateUi();
    nonModal.OpenNonModal(UIManager.StationDetailScreenId);
    var world = CreateUi();

    return modal.PressMapKey() == ScreenResult.ErrModalOpen
        && modal.CurrentScreen == Screen.Hub
        && nonModal.PressMapKey() == ScreenResult.ErrModalOpen
        && nonModal.CurrentScreen == Screen.Hub
        && world.PressMapKey() == ScreenResult.Success
        && world.CurrentScreen == Screen.Chart;
}

static bool Ac21TabCyclesInsideModal()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.RepairScreenId);

    return ui.KeyboardFocusElementId == "repair.plus_one"
        && ui.PressTab()
        && ui.KeyboardFocusElementId == "repair.confirm"
        && ui.PressTab()
        && ui.KeyboardFocusElementId == "repair.cancel"
        && ui.PressTab()
        && ui.KeyboardFocusElementId == "repair.plus_one";
}

static bool Ac22MousePressGrabsFocus()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.RepairScreenId);

    return !ui.MousePressInteractable("naming.confirm")
        && ui.KeyboardFocusElementId == "repair.plus_one"
        && ui.MousePressInteractable("repair.confirm")
        && ui.KeyboardFocusElementId == "repair.confirm"
        && ui.PressTab()
        && ui.KeyboardFocusElementId == "repair.cancel";
}

static bool Ac23FocusHoverStylesDistinct()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.RepairScreenId);
    ui.SetKeyboardFocus("repair.confirm");
    ui.MouseHoverElement("repair.cancel");
    var confirm = ui.GetElementVisualState("repair.confirm");
    var cancel = ui.GetElementVisualState("repair.cancel");

    return confirm.KeyboardFocused
        && confirm.FocusStyleToken == UIManager.FocusStyleToken
        && !confirm.MouseHovered
        && cancel.MouseHovered
        && cancel.HoverStyleToken == UIManager.HoverStyleToken
        && !cancel.KeyboardFocused
        && UIManager.FocusStyleToken != UIManager.HoverStyleToken;
}

static bool Ac24ReadonlySkipped()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.RepairScreenId);
    ui.SetKeyboardFocus("repair.cancel");

    return !ui.IsElementFocusable("repair.node_name")
        && ui.PressTab()
        && ui.KeyboardFocusElementId == "repair.plus_one";
}

static bool Ac25ModalAutoFocus()
{
    var ui = CreateUi();

    return ui.OpenModalPanel(UIManager.RepairScreenId) == ModalResult.Success
        && ui.KeyboardFocusElementId == "repair.plus_one";
}

static ModalPanelSnapshot Snapshot(UIManager ui, string panelId)
{
    return ui.GetPanelSnapshot(panelId)
        ?? throw new InvalidOperationException($"Missing panel snapshot for {panelId}");
}

static UIManager CreateUi()
{
    var ui = new UIManager();
    ui.Initialize();
    return ui;
}

static UIManager CapacityChoiceWithCombat()
{
    var ui = CreateUi();
    var context = new Dictionary<string, string>
    {
        ["batch_id"] = "loot-choice-01",
        ["item_id"] = "goods.scrap"
    };
    ui.OpenModalPanel(UIManager.CapacityChoiceScreenId, context, scrollOffset: 42, selectedIndex: 3);
    ui.OpenModalPanel(UIManager.CombatScreenId);
    return ui;
}

static UIManager ExplorationUi()
{
    var ui = CreateUi();
    ui.PressMapKey();
    ui.SelectRoute("route.sky-reef");
    ui.ConfirmDeparture();
    ui.CompleteChartLock();
    ui.EncounterContextReady();
    return ui;
}
