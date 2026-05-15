using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #16 Story 006: Edge Cases, Desktop Recovery & Accessibility ===");
var failed = 0;
var total = 0;

Run("AC-1: desktop resume preserves S12, focus, and movement", Ac1DesktopResumePreservesPanel);
Run("AC-2: large resume delta forces full UI refresh from domain", Ac2LargeDeltaFullRefresh);
Run("AC-2b: full UI refresh clears carried grid when domain inventory is empty", Ac2bFullRefreshClearsEmptyCarriedGrid);
Run("AC-3: normal resume delta keeps dirty path", Ac3NormalDeltaNoFullRefresh);
Run("AC-4: S8 repair panel keeps open-time snapshot", Ac4RepairSnapshotIsolation);
Run("AC-5: S6a capacity panel keeps decision snapshot", Ac5CapacitySnapshotIsolation);
Run("AC-6: S11 sniff empty state renders text", Ac6SniffEmptyState);
Run("AC-7: S12 storage empty state renders guide", Ac7StorageEmptyState);
Run("AC-8: S4 chart empty state renders text", Ac8ChartEmptyState);
Run("AC-9: naming modal opens at arrival tail when eligible", Ac9NamingArrivalTiming);
Run("AC-10: naming skip count closes prompt window", Ac10NamingSkipLockout);
Run("AC-11: hull zero renders red band and repair icon", Ac11HullZeroRepairSignal);
Run("AC-12: missing cargo module greys cargo and rejects S12", Ac12CargoMissing);
Run("AC-13: combat override restores or discards capacity state", Ac13CombatOverrideRaceDefense);
Run("AC-14: disabled button remains focusable but Enter no-ops", Ac14DisabledButtonFocusable);
Run("AC-15: no-focus modal traps focus on container", Ac15NoFocusablePanelTrap);
Run("AC-16: destroyed prior focus falls back to current screen", Ac16DestroyedFocusFallback);
Run("AC-17: S7 Esc is consumed with response prompt", Ac17CombatEscBlocked);
Run("AC-18: hull band has color, shape, and segment text", Ac18HullTripleEncoding);
Run("AC-19: repair materials have icon and text state", Ac19MaterialTripleEncoding);
Run("AC-20: all small status indicators pass triple encoding audit", Ac20SmallStatusAudit);
Run("AC-21: danger text resolves to an AA foreground on parchment", Ac21DangerContrastAudit);
Run("AC-22: beacon text resolves to an AA foreground on parchment", Ac22BeaconContrastAudit);
Run("AC-23: repair anchor exposes highlight metadata", Ac23HighlightableRepairAnchor);
Run("AC-24: departure lock silently rejects highlight override", Ac24DepartureLockHighlightReject);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 006 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 006 validation passed: {total}/{total} checks passed.");
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

static bool Ac1DesktopResumePreservesPanel()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OpenNonModal(UIManager.StorageScreenId);
	ui.SetKeyboardFocus("hub.helm");
	ui.RecordProcessDelta(1.2);
	var recovery = ui.OnApplicationResumed();

	return recovery.FullRefreshRequested
		&& recovery.VisiblePanelIds.Contains(UIManager.StorageScreenId)
		&& recovery.FocusElementId == "hub.helm"
		&& !recovery.MovementInputBlocked
		&& ui.IsPanelVisible(UIManager.StorageScreenId);
}

static bool Ac2LargeDeltaFullRefresh()
{
	var upstream = new FakeUpstreamDataSource { StorageCurrent = 7 };
	var ui = CreateUi(upstream);
	ui.OnStorageChanged(1, 10);
	ui.RecordProcessDelta(1.25);
	var recovery = ui.OnApplicationResumed();
	var storage = ui.GetHudElementSnapshot(UIManager.HubStorageElementId);

	return recovery.FullRefreshRequested
		&& recovery.FullRefreshRequestCount == 1
		&& recovery.QueryNames.Contains("get_storage_state")
		&& !ui.IsHudElementDirty(UIManager.StorageBarSignalId)
		&& storage.Text == "7/10";
}

static bool Ac2bFullRefreshClearsEmptyCarriedGrid()
{
	var upstream = new FakeUpstreamDataSource();
	var ui = ExplorationUi(upstream);
	ui.OnCarriedChanged(0, "item.saltcloth", 1);
	ui.ProcessHudFrame();
	var stale = ui.GetHudElementSnapshot(UIManager.ExplorationCarriedGridElementId);
	ui.RecordProcessDelta(1.25);
	var recovery = ui.OnApplicationResumed();
	var refreshed = ui.GetHudElementSnapshot(UIManager.ExplorationCarriedGridElementId);

	return stale.Visible
		&& recovery.FullRefreshRequested
		&& recovery.QueryNames.Contains("get_carried_inventory")
		&& !refreshed.Visible
		&& refreshed.Text == string.Empty;
}

static bool Ac3NormalDeltaNoFullRefresh()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OnStorageChanged(1, 10);
	ui.RecordProcessDelta(0.75);
	var recovery = ui.OnApplicationResumed();

	return !recovery.FullRefreshRequested
		&& recovery.FullRefreshRequestCount == 0
		&& ui.IsHudElementDirty(UIManager.StorageBarSignalId);
}

static bool Ac4RepairSnapshotIsolation()
{
	var upstream = new FakeUpstreamDataSource { RepairRequired = 3 };
	var ui = CreateUi(upstream);
	ui.OpenRepairPanelForNode("beacon_02");
	var opened = Binding(ui);
	upstream.RepairRequired = 10;
	var stillOpen = opened;
	ui.CloseModal();
	ui.OpenRepairPanelForNode("beacon_02");
	var reopened = Binding(ui);

	return opened.Fields["required"] == "3"
		&& stillOpen.Fields["required"] == "3"
		&& reopened.Fields["required"] == "10";
}

static bool Ac5CapacitySnapshotIsolation()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	var context = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["batch_id"] = "loot-01",
		["items"] = "old_item",
	};
	ui.OpenModalPanel(UIManager.CapacityChoiceScreenId, context, scrollOffset: 12, selectedIndex: 1);
	context["items"] = "new_item";
	var snapshot = Panel(ui, UIManager.CapacityChoiceScreenId);

	return snapshot.DataContext["items"] == "old_item"
		&& snapshot.ScrollOffset == 12
		&& snapshot.SelectedIndex == 1;
}

static bool Ac6SniffEmptyState()
{
	var upstream = new FakeUpstreamDataSource();
	upstream.SniffItems.Clear();
	var ui = CreateUi(upstream);
	ui.OpenNonModal(UIManager.PartnerSniffScreenId);

	return Binding(ui).EmptyStateVisible
		&& Binding(ui).EmptyStateMessage == UIManager.PartnerSniffEmptyStateMessage;
}

static bool Ac7StorageEmptyState()
{
	var ui = CreateUi(new FakeUpstreamDataSource { StorageCurrent = 0 });
	ui.OpenNonModal(UIManager.StorageScreenId);

	return Binding(ui).EmptyStateVisible
		&& Binding(ui).EmptyStateMessage == UIManager.StorageEmptyStateMessage;
}

static bool Ac8ChartEmptyState()
{
	var upstream = new FakeUpstreamDataSource();
	upstream.VisibleRoutes.Clear();
	var ui = CreateUi(upstream);
	ui.PressMapKey();

	return Binding(ui).EmptyStateVisible
		&& Binding(ui).EmptyStateMessage == UIManager.ChartEmptyStateMessage;
}

static bool Ac9NamingArrivalTiming()
{
	var ui = HubArrivingUi();
	var result = ui.ArrivalComplete(namingEligible: true);
	var animation = ui.GetAnimationSnapshot(UIManager.NamingModalPopAnimationId);

	return result == ScreenResult.Success
		&& ui.CurrentModalId == UIManager.NamingScreenId
		&& animation is not null
		&& Math.Abs(animation.DurationSeconds - UIManager.NamingModalPopAnimationSeconds) < 0.001
		&& ui.OpenScreen(Screen.Chart) == ScreenResult.ErrModalOpen;
}

static bool Ac10NamingSkipLockout()
{
	var ui = HubArrivingUi();
	ui.SetNamingSkipCount(UIManager.NamingSkipMax);

	return !ui.IsNamingPromptEligible(domainEligible: true)
		&& ui.ArrivalComplete(namingEligible: true) == ScreenResult.Success
		&& !ui.IsModalOpen();
}

static bool Ac11HullZeroRepairSignal()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OnHullIntegrityChanged(1, 0);
	ui.ProcessHudFrame();
	var hull = ui.GetHudElementSnapshot(UIManager.HubHullBarElementId);
	var repair = ui.GetHudElementSnapshot(UIManager.HubHullRepairIconElementId);

	return hull.ColorHex == UIManager.DangerRedHex
		&& hull.ShapeToken == "shape.circle"
		&& repair.Visible
		&& repair.Text == "需要维修"
		&& repair.IconToken == "wrench.blink";
}

static bool Ac12CargoMissing()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OnCargoChanged(current: 0, max: 0, hasModule: false);
	ui.ProcessHudFrame();
	var cargo = ui.GetHudElementSnapshot(UIManager.HubCargoElementId);

	return cargo.Visible
		&& cargo.Text == "无货舱"
		&& cargo.ColorHex == UIManager.DisabledGrayHex
		&& ui.UsePanelAnchor(UIManager.StorageAnchorId) == ScreenResult.ErrInvalidScreen
		&& ui.LastToastMessage == UIManager.CargoModuleRequiredToast;
}

static bool Ac13CombatOverrideRaceDefense()
{
	var restore = CapacityChoiceWithCombat();
	var retreat = CapacityChoiceWithCombat();

	return restore.ResolveCombatThreat(CombatThreatResolution.HoldGround) == ModalResult.Success
		&& restore.CurrentModalId == UIManager.CapacityChoiceScreenId
		&& Panel(restore, UIManager.CapacityChoiceScreenId).DataContext["decision"] == "keep"
		&& retreat.ResolveCombatThreat(CombatThreatResolution.Retreat) == ModalResult.Success
		&& !retreat.IsPanelVisible(UIManager.CapacityChoiceScreenId);
}

static bool Ac14DisabledButtonFocusable()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OpenModalPanel(UIManager.RepairScreenId);
	ui.SetElementDisabled("repair.confirm", "材料不足，无法提交");

	return ui.PressTab()
		&& ui.KeyboardFocusElementId == "repair.confirm"
		&& ui.PressEnterOnFocusedElement() == FocusActivationResult.DisabledNoOp
		&& ui.LastTooltipMessage == "材料不足，无法提交";
}

static bool Ac15NoFocusablePanelTrap()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.DestroyFocusableElement("repair.plus_one");
	ui.DestroyFocusableElement("repair.confirm");
	ui.DestroyFocusableElement("repair.cancel");
	ui.OpenModalPanel(UIManager.RepairScreenId);
	var initialFocus = $"panel:{UIManager.RepairScreenId}";

	return ui.CurrentModalId == UIManager.RepairScreenId
		&& ui.KeyboardFocusElementId == initialFocus
		&& ui.PressTab()
		&& ui.KeyboardFocusElementId == initialFocus
		&& ui.IsMovementInputBlocked();
}

static bool Ac16DestroyedFocusFallback()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.SetKeyboardFocus("hub.helm");
	ui.OpenModalPanel(UIManager.RepairScreenId);
	ui.DestroyFocusableElement("hub.helm");
	ui.CloseModal();

	return ui.KeyboardFocusElementId == "hub.gangway";
}

static bool Ac17CombatEscBlocked()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OpenModalPanel(UIManager.CombatScreenId);

	return ui.PressEscape() == ScreenResult.Success
		&& ui.CurrentModalId == UIManager.CombatScreenId
		&& ui.LastVisualPrompt == UIManager.CombatResponseRequiredPrompt;
}

static bool Ac18HullTripleEncoding()
{
	var green = new UIManager().GetHullBandEncoding("GREEN");
	var yellow = new UIManager().GetHullBandEncoding("YELLOW");
	var red = new UIManager().GetHullBandEncoding("RED");

	return green.ColorHex == UIManager.SafeGreenHex
		&& green.ShapeToken == "shape.check"
		&& green.SegmentCount == 3
		&& yellow.ShapeToken == "shape.bolt"
		&& yellow.SegmentCount == 2
		&& red.ColorHex == UIManager.DangerRedHex
		&& red.ShapeToken == "shape.circle"
		&& red.SegmentCount == 1;
}

static bool Ac19MaterialTripleEncoding()
{
	var ui = new UIManager();
	var satisfied = ui.GetMaterialRequirementEncoding(satisfied: true);
	var missing = ui.GetMaterialRequirementEncoding(satisfied: false);

	return satisfied.ColorHex == UIManager.SafeGreenHex
		&& satisfied.ShapeToken == "shape.check"
		&& satisfied.TextLabel == "满足"
		&& missing.ColorHex == UIManager.DangerRedHex
		&& missing.ShapeToken == "shape.cross"
		&& missing.TextLabel == "不足";
}

static bool Ac20SmallStatusAudit()
{
	var ui = new UIManager();
	var audit = ui.AuditSmallStatusEncodings();

	return audit.Count >= 5
		&& audit.All(ui.IsSmallStatusEncodingCompliant);
}

static bool Ac21DangerContrastAudit()
{
	var ui = new UIManager();
	var canonical = ui.AuditTextContrast(UIManager.DangerRedHex, UIManager.ParchmentBackgroundHex, requiredRatio: 4.52);
	var foreground = ui.ResolveAccessibleTextForeground(UIManager.DangerRedHex, UIManager.ParchmentBackgroundHex, requiredRatio: 4.52);
	var rendered = ui.AuditTextContrast(foreground, UIManager.ParchmentBackgroundHex, requiredRatio: 4.52);

	return !canonical.Passes
		&& canonical.Ratio < 4.52
		&& canonical.RecommendedForegroundHex == UIManager.AccessibleDangerTextHex
		&& foreground == UIManager.AccessibleDangerTextHex
		&& rendered.Passes;
}

static bool Ac22BeaconContrastAudit()
{
	var ui = new UIManager();
	var canonical = ui.AuditTextContrast("#4FB7B2", UIManager.ParchmentBackgroundHex, requiredRatio: 3.0);
	var foreground = ui.ResolveAccessibleTextForeground("#4FB7B2", UIManager.ParchmentBackgroundHex, requiredRatio: 3.0);
	var rendered = ui.AuditTextContrast(foreground, UIManager.ParchmentBackgroundHex, requiredRatio: 3.0);

	return !canonical.Passes
		&& canonical.Ratio < 3.0
		&& canonical.RecommendedForegroundHex == UIManager.AccessibleBeaconTextHex
		&& foreground == UIManager.AccessibleBeaconTextHex
		&& rendered.Passes;
}

static bool Ac23HighlightableRepairAnchor()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	var anchor = ui.GetAnchorMetadata(UIManager.RepairAnchorId);

	return anchor.Highlightable
		&& anchor.PanelId == UIManager.RepairScreenId
		&& anchor.HighlightPriority > 0;
}

static bool Ac24DepartureLockHighlightReject()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.UseGangway();
	ui.ConfirmDeparture();
	var beforeToast = ui.LastToastMessage;
	var result = ui.RequestOnboardingHighlight(UIManager.RepairAnchorId);

	return ui.DepartureLocked
		&& !result.HighlightRequestAccepted
		&& ui.LastHighlightRequestRejectedSilently
		&& ui.LastToastMessage == beforeToast;
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
		?? throw new InvalidOperationException("Expected binding snapshot.");
}

static ModalPanelSnapshot Panel(UIManager ui, string panelId)
{
	return ui.GetPanelSnapshot(panelId)
		?? throw new InvalidOperationException($"Expected panel snapshot: {panelId}");
}

static UIManager HubArrivingUi()
{
	var ui = ExplorationUi(new FakeUpstreamDataSource());
	ui.ExtractionStarted();
	ui.ExtractionComplete();
	ui.SettlementConfirmed();
	return ui;
}

static UIManager ExplorationUi(FakeUpstreamDataSource upstream)
{
	var ui = CreateUi(upstream);
	ui.PressMapKey();
	ui.SelectRoute("route.sky-reef");
	ui.ConfirmDeparture();
	ui.CompleteChartLock();
	ui.EncounterContextReady();
	return ui;
}

static UIManager CapacityChoiceWithCombat()
{
	var ui = CreateUi(new FakeUpstreamDataSource());
	ui.OpenModalPanel(
		UIManager.CapacityChoiceScreenId,
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["batch_id"] = "loot-choice-01",
			["decision"] = "keep",
		},
		scrollOffset: 42,
		selectedIndex: 2);
	ui.OpenModalPanel(UIManager.CombatScreenId);
	return ui;
}

sealed class FakeUpstreamDataSource : IUiUpstreamDataSource
{
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
			["cat_sniff_signature"] = "rain",
		},
	};

	public int StorageCurrent { get; set; } = 3;

	public int RepairRequired { get; set; } = 3;

	public bool CargoHasModule { get; set; } = true;

	public IReadOnlyDictionary<string, string>? GetChartState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["state"] = "BROWSING",
		};
	}

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetVisibleRoutes()
	{
		return VisibleRoutes.ToArray();
	}

	public IReadOnlyDictionary<string, string>? GetSelectedRoute()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["id"] = "route.sky-reef",
			["name"] = "裂云礁航线",
		};
	}

	public IReadOnlyDictionary<string, string>? GetFilterState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["hide_rumored"] = "false",
		};
	}

	public IReadOnlyDictionary<string, string>? GetHullIntegrity()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["current"] = "82",
			["max"] = "100",
		};
	}

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates()
	{
		return new[]
		{
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["state"] = "INSTALLED",
			},
		};
	}

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory()
	{
		return Array.Empty<IReadOnlyDictionary<string, string>>();
	}

	public IReadOnlyDictionary<string, string>? GetStorageState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["current"] = StorageCurrent.ToString(),
			["max"] = "10",
		};
	}

	public IReadOnlyDictionary<string, string>? GetCargoState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["current"] = CargoHasModule ? "1" : "0",
			["max"] = CargoHasModule ? "5" : "0",
			["has_module"] = CargoHasModule.ToString(),
		};
	}

	public int? GetCurrency() => 12;

	public IReadOnlyDictionary<string, string>? GetSearchProgress()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["searched"] = "1",
			["total"] = "6",
		};
	}

	public string? GetScoutPreviewLevel() => UIManager.ScoutPreviewPresence;

	public IReadOnlyDictionary<string, string>? GetExtractionState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["extraction_progress"] = "0",
		};
	}

	public IReadOnlyDictionary<string, string>? BuildThreatContext()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["threat_id"] = "threat.riftshade",
		};
	}

	public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId)
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["node_id"] = nodeId,
			["required"] = RepairRequired.ToString(),
		};
	}

	public IReadOnlyDictionary<string, string>? GetStallData(string stallId)
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["stall_id"] = stallId,
		};
	}

	public string? QueryPartnerName() => "灰白猫";

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems() => SniffItems.ToArray();

	public bool? NamingPromptEligibility() => true;

	public string? GetDisplayName(string entityId) => entityId;

	public string? GetDescription(string entityId) => $"description:{entityId}";

	public bool TransferItem(string itemId, string fromPool, string toPool, int quantity) => true;

	public bool DiscardItem(string itemId) => true;

	public bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials) => true;

	public bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost) => true;

	public bool SubmitPartnerName(string partnerId, string name) => true;
}
