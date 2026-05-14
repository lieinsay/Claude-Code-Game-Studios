using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #16 Story 003: HUD Update, Panel Lifecycle & Cache ===");
var failed = 0;
var total = 0;

Run("AC-1: HUD signal records dirty flag and payload", Ac1DirtyFlagPayload);
Run("AC-2: HUD process batches dirty elements and clears flags", Ac2BatchUpdateClearsFlags);
Run("AC-3: idle HUD process returns immediately without iteration", Ac3IdleProcessZeroCost);
Run("AC-4: repeated storage signals apply only the latest payload", Ac4LastPayloadWins);
Run("AC-5: hull band updates S1/S5 color and shape", Ac5HullBandMapping);
Run("AC-6: storage update sets text and 200px-scaled width", Ac6StorageMapping);
Run("AC-7: carried update fills slot and marks full inventory", Ac7CarriedGridMapping);
Run("AC-8: search progress updates S5 count text", Ac8SearchProgressMapping);
Run("AC-9: scout preview switches to full threat label", Ac9ScoutPreviewMapping);
Run("REG-1: scout preview supports none, presence, and full states", Reg1ScoutPreviewThreeStates);
Run("AC-10: module state updates S1 module light", Ac10ModuleStateMapping);
Run("AC-11: currency update reaches market and Hub HUD", Ac11CurrencyMapping);
Run("AC-12: HUB shows S1 and hides S5", Ac12HubVisibility);
Run("AC-13: EXPLORATION and EXTRACTING show S5", Ac13ExplorationVisibility);
Run("AC-14: VOYAGE hides S1 and S5", Ac14VoyageVisibility);
Run("AC-15: proximity enter preloads S11 data non-blocking", Ac15ProximityPreload);
Run("AC-16: Use opens preloaded S11 as active non-modal", Ac16UseOpensNonModal);
Run("AC-17: proximity exit auto-closes S11 with close timing", Ac17ProximityExitCloses);
Run("AC-18: Esc closes S11 immediately inside radius", Ac18EscClosesNonModal);
Run("AC-19: repair node Use opens S8 as event-driven modal", Ac19RepairModalEventDriven);
Run("AC-20: closing S8 restores world movement", Ac20RepairModalCloseRestoresMovement);
Run("AC-21: S7 is preloaded and instantiates under 1ms", Ac21CombatPreload);
Run("AC-22: LRU cache evicts the oldest panel at max 2", Ac22LruCacheEvicts);
Run("AC-23: voyage scene switch clears cached panels", Ac23VoyageClearsCache);
Run("AC-24: S2 station detail reuses one template with new data", Ac24StationTemplateReuse);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 003 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 003 validation passed: {total}/{total} checks passed.");
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

static bool Ac1DirtyFlagPayload()
{
    var ui = CreateUi();
    ui.OnHullIntegrityChanged(oldValue: 30, newValue: 25);
    var payload = ui.GetPendingHudPayload(UIManager.HullBarSignalId);

    return ui.IsHudElementDirty(UIManager.HullBarSignalId)
        && payload["old"] == "30"
        && payload["new"] == "25";
}

static bool Ac2BatchUpdateClearsFlags()
{
    var ui = CreateUi();
    ui.OnHullBandChanged("GREEN", "YELLOW");
    ui.OnStorageChanged(920, 1000);
    ui.OnSearchProgressChanged(3, 6);
    var result = ui.ProcessHudFrame();

    return result.ProcessPriority == UIManager.HudProcessPriority
        && result.UpdatedElementCount == 4
        && result.DirtyFlagsCleared
        && result.IteratedDirtyElements
        && !result.ImmediateReturn
        && !ui.IsHudElementDirty(UIManager.HullBandSignalId)
        && ui.GetHudElementSnapshot(UIManager.HubStorageElementId).Text == "920/1000"
        && ui.GetHudElementSnapshot(UIManager.ExplorationSearchCountElementId).Text == "3/6";
}

static bool Ac3IdleProcessZeroCost()
{
    var ui = CreateUi();
    var result = ui.ProcessHudFrame();

    return result.UpdatedElementCount == 0
        && result.ImmediateReturn
        && !result.IteratedDirtyElements
        && result.DirtyFlagsCleared;
}

static bool Ac4LastPayloadWins()
{
    var ui = CreateUi();
    ui.OnStorageChanged(900, 1000);
    ui.OnStorageChanged(910, 1000);
    ui.OnStorageChanged(920, 1000);
    ui.ProcessHudFrame();
    var storage = ui.GetHudElementSnapshot(UIManager.HubStorageElementId);

    return storage.Text == "920/1000"
        && storage.BarWidth == 184;
}

static bool Ac5HullBandMapping()
{
    var ui = CreateUi();
    ui.OnHullBandChanged("GREEN", "YELLOW");
    ui.ProcessHudFrame();
    var hub = ui.GetHudElementSnapshot(UIManager.HubHullBarElementId);
    var exploration = ui.GetHudElementSnapshot(UIManager.ExplorationHullBarElementId);

    return hub.ColorHex == UIManager.WarningAmberHex
        && hub.ShapeToken == "shape.bolt"
        && exploration.ColorHex == UIManager.WarningAmberHex
        && exploration.ShapeToken == "shape.bolt";
}

static bool Ac6StorageMapping()
{
    var ui = CreateUi();
    ui.OnStorageChanged(920, 1000);
    ui.ProcessHudFrame();
    var storage = ui.GetHudElementSnapshot(UIManager.HubStorageElementId);

    return storage.Text == "920/1000"
        && storage.BarWidth == 184;
}

static bool Ac7CarriedGridMapping()
{
    var ui = CreateUi();
    ui.OnCarriedChanged(slot: 0, itemId: "scrap", quantity: 1);
    ui.OnCarriedChanged(slot: 1, itemId: "cloth", quantity: 1);
    ui.OnCarriedChanged(slot: 2, itemId: "wire", quantity: 1);
    ui.OnCarriedChanged(slot: 3, itemId: "cog", quantity: 1);
    ui.OnCarriedChanged(slot: 4, itemId: "lens_kit", quantity: 1);
    ui.ProcessHudFrame();
    var carried = ui.GetHudElementSnapshot(UIManager.ExplorationCarriedGridElementId);

    return carried.Text == "slot:4:lens_kit:x1"
        && carried.IconToken == "lens_kit"
        && carried.BorderColorHex == UIManager.WarningAmberHex;
}

static bool Ac8SearchProgressMapping()
{
    var ui = CreateUi();
    ui.OnSearchProgressChanged(3, 6);
    ui.ProcessHudFrame();

    return ui.GetHudElementSnapshot(UIManager.ExplorationSearchCountElementId).Text == "3/6";
}

static bool Ac9ScoutPreviewMapping()
{
    var ui = CreateUi();
    ui.OnScoutPreviewChanged(UIManager.ScoutPreviewFull);
    ui.ProcessHudFrame();
    var preview = ui.GetHudElementSnapshot(UIManager.ExplorationThreatPreviewElementId);

    return preview.Text == UIManager.ScoutPreviewFull
        && preview.IconToken == "threat.preview.full";
}

static bool Reg1ScoutPreviewThreeStates()
{
    var ui = CreateUi();

    ui.OnScoutPreviewChanged(UIManager.ScoutPreviewNone);
    ui.ProcessHudFrame();
    var none = ui.GetHudElementSnapshot(UIManager.ExplorationThreatPreviewElementId);

    ui.OnScoutPreviewChanged(UIManager.ScoutPreviewPresence);
    ui.ProcessHudFrame();
    var presence = ui.GetHudElementSnapshot(UIManager.ExplorationThreatPreviewElementId);

    ui.OnScoutPreviewChanged(UIManager.ScoutPreviewFull);
    ui.ProcessHudFrame();
    var full = ui.GetHudElementSnapshot(UIManager.ExplorationThreatPreviewElementId);

    return !none.Visible
        && none.IconToken == string.Empty
        && presence.Visible
        && presence.Text == "!"
        && presence.ColorHex == UIManager.DangerRedHex
        && presence.IconToken == "threat.preview.presence"
        && full.Visible
        && full.Text == UIManager.ScoutPreviewFull
        && full.ColorHex == UIManager.WarningAmberHex
        && full.IconToken == "threat.preview.full";
}

static bool Ac10ModuleStateMapping()
{
    var ui = CreateUi();
    ui.OnModuleStateChanged(slot: 0, state: "INSTALLED");
    ui.ProcessHudFrame();
    var module = ui.GetHudElementSnapshot(UIManager.HubModuleLightsElementId);

    return module.Text == "INSTALLED"
        && module.ShapeToken == "shape.check"
        && module.ColorHex == UIManager.SafeGreenHex;
}

static bool Ac11CurrencyMapping()
{
    var ui = CreateUi();
    ui.OnCurrencyChanged(150);
    ui.ProcessHudFrame();

    return ui.GetHudElementSnapshot(UIManager.HubCurrencyElementId).Text == "150"
        && ui.GetHudElementSnapshot(UIManager.MarketCurrencyElementId).Text == "150";
}

static bool Ac12HubVisibility()
{
    var ui = CreateUi();

    return ui.CurrentScreen == Screen.Hub
        && ui.HubHudVisible
        && !ui.ExplorationHudVisible;
}

static bool Ac13ExplorationVisibility()
{
    var exploration = ExplorationUi();
    var extracting = ExplorationUi();
    extracting.ExtractionStarted();

    return exploration.CurrentScreen == Screen.Exploration
        && !exploration.HubHudVisible
        && exploration.ExplorationHudVisible
        && extracting.CurrentScreen == Screen.Extracting
        && !extracting.HubHudVisible
        && extracting.ExplorationHudVisible;
}

static bool Ac14VoyageVisibility()
{
    var ui = ChartDepartureConfirmedUi();
    ui.CompleteChartLock();

    return ui.CurrentScreen == Screen.Voyage
        && !ui.HubHudVisible
        && !ui.ExplorationHudVisible;
}

static bool Ac15ProximityPreload()
{
    var ui = CreateUi();
    var result = ui.ProximityEnter(UIManager.PartnerSniffAnchorId, UIManager.PanelPreloadRadiusMultiplier);
    var lifecycle = ui.GetPanelLifecycleSnapshot(UIManager.PartnerSniffScreenId);

    return result == ScreenResult.Success
        && lifecycle.State == PanelLifecycleState.Ready
        && lifecycle.IsPreloaded
        && lifecycle.PreloadNonBlocking
        && lifecycle.DistanceDriven;
}

static bool Ac16UseOpensNonModal()
{
    var ui = CreateUi();
    ui.ProximityEnter(UIManager.PartnerSniffAnchorId, UIManager.PanelPreloadRadiusMultiplier);
    var result = ui.UsePanelAnchor(UIManager.PartnerSniffAnchorId);
    var lifecycle = ui.GetPanelLifecycleSnapshot(UIManager.PartnerSniffScreenId);

    return result == ScreenResult.Success
        && ui.IsPanelVisible(UIManager.PartnerSniffScreenId)
        && lifecycle.State == PanelLifecycleState.Active
        && Math.Abs(lifecycle.OpenAnimationSeconds - UIManager.PanelOpenAnimationSeconds) < 0.001
        && !ui.IsMovementInputBlocked();
}

static bool Ac17ProximityExitCloses()
{
    var ui = ActiveSniffPanelUi();
    var result = ui.ProximityExit(UIManager.PartnerSniffAnchorId, UIManager.PanelAutoCloseRadiusMultiplier);
    var lifecycle = ui.GetPanelLifecycleSnapshot(UIManager.PartnerSniffScreenId);

    return result == ScreenResult.Success
        && !ui.IsPanelVisible(UIManager.PartnerSniffScreenId)
        && lifecycle.State == PanelLifecycleState.Closed
        && Math.Abs(lifecycle.CloseAnimationSeconds - UIManager.PanelCloseAnimationSeconds) < 0.001;
}

static bool Ac18EscClosesNonModal()
{
    var ui = ActiveSniffPanelUi();
    var result = ui.PressEscape();
    var lifecycle = ui.GetPanelLifecycleSnapshot(UIManager.PartnerSniffScreenId);

    return result == ScreenResult.Success
        && !ui.IsPanelVisible(UIManager.PartnerSniffScreenId)
        && lifecycle.State == PanelLifecycleState.Closed;
}

static bool Ac19RepairModalEventDriven()
{
    var ui = CreateUi();
    var result = ui.OpenRepairPanelForNode("lighthouse_01");
    var lifecycle = ui.GetPanelLifecycleSnapshot(UIManager.RepairScreenId);
    var snapshot = ui.GetPanelSnapshot(UIManager.RepairScreenId);

    return result == ModalResult.Success
        && ui.CurrentModalId == UIManager.RepairScreenId
        && ui.IsPanelVisible(UIManager.RepairScreenId)
        && lifecycle.State == PanelLifecycleState.Active
        && lifecycle.DomainEventDriven
        && !lifecycle.DistanceDriven
        && snapshot is not null
        && snapshot.DataContext["node_id"] == "lighthouse_01";
}

static bool Ac20RepairModalCloseRestoresMovement()
{
    var ui = CreateUi();
    ui.OpenRepairPanelForNode("lighthouse_01");

    return ui.PressEscape() == ScreenResult.Success
        && !ui.IsModalOpen()
        && !ui.IsPanelVisible(UIManager.RepairScreenId)
        && !ui.IsMovementInputBlocked()
        && ui.GetPanelLifecycleSnapshot(UIManager.RepairScreenId).State == PanelLifecycleState.Closed;
}

static bool Ac21CombatPreload()
{
    var ui = CreateUi();
    var result = ui.OpenModalPanel(UIManager.CombatScreenId);
    var lifecycle = ui.GetPanelLifecycleSnapshot(UIManager.CombatScreenId);

    return ui.CombatPanelPreloaded
        && result == ModalResult.Success
        && lifecycle.IsPreloaded
        && lifecycle.InstantiationDelayMilliseconds < 1.0;
}

static bool Ac22LruCacheEvicts()
{
    var ui = CreateUi();
    ui.CachePanelInstance(UIManager.PartnerSniffScreenId);
    ui.CachePanelInstance(UIManager.StorageScreenId);
    ui.CachePanelInstance(UIManager.RepairScreenId);
    var cache = ui.GetPanelCacheSnapshot();

    return cache.CachedPanelIds.SequenceEqual(new[] { UIManager.StorageScreenId, UIManager.RepairScreenId })
        && cache.FreedPanelIds.Contains(UIManager.PartnerSniffScreenId);
}

static bool Ac23VoyageClearsCache()
{
    var ui = CreateUi();
    ui.CachePanelInstance(UIManager.PartnerSniffScreenId);
    ui.CachePanelInstance(UIManager.StorageScreenId);
    ui.PressMapKey();
    ui.SelectRoute("route.sky-reef");
    ui.ConfirmDeparture();
    ui.CompleteChartLock();
    var cache = ui.GetPanelCacheSnapshot();

    return ui.CurrentScreen == Screen.Voyage
        && cache.CachedPanelIds.Count == 0
        && cache.FreedPanelIds.Contains(UIManager.PartnerSniffScreenId)
        && cache.FreedPanelIds.Contains(UIManager.StorageScreenId);
}

static bool Ac24StationTemplateReuse()
{
    var ui = CreateUi();
    ui.OpenStationDetailPanel("station.intel", "Intel Table");
    var intel = ui.GetStationDetailSnapshot();
    ui.CloseNonModal(UIManager.StationDetailScreenId);
    ui.OpenStationDetailPanel("station.storage", "Storage Hold");
    var storage = ui.GetStationDetailSnapshot();

    return intel is not null
        && storage is not null
        && intel.TemplateId == UIManager.StationDetailTemplateId
        && storage.TemplateId == UIManager.StationDetailTemplateId
        && intel.TemplateId == storage.TemplateId
        && intel.StationId != storage.StationId
        && intel.DisplayName != storage.DisplayName;
}

static UIManager CreateUi()
{
    var ui = new UIManager();
    ui.Initialize();
    return ui;
}

static UIManager ActiveSniffPanelUi()
{
    var ui = CreateUi();
    ui.ProximityEnter(UIManager.PartnerSniffAnchorId, UIManager.PanelPreloadRadiusMultiplier);
    ui.UsePanelAnchor(UIManager.PartnerSniffAnchorId);
    return ui;
}

static UIManager ChartDepartureConfirmedUi()
{
    var ui = CreateUi();
    ui.PressMapKey();
    ui.SelectRoute("route.sky-reef");
    ui.ConfirmDeparture();
    return ui;
}

static UIManager ExplorationUi()
{
    var ui = ChartDepartureConfirmedUi();
    ui.CompleteChartLock();
    ui.EncounterContextReady();
    return ui;
}
