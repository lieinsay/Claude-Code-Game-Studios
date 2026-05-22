using System;
using System.Collections.Generic;
using System.Linq;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Screen identifiers for the 12-screen FSM.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum Screen
{
	None = 0,
	Hub = 1,
	Chart = 2,
	ChartRouteSelected = 3,
	ChartDepartureConfirmed = 4,
	DepartureLocked = 5,
	Voyage = 6,
	Exploration = 7,
	Extracting = 8,
	Settlement = 9,
	HubArriving = 10,
	Combat = 11,
}

/// <summary>
/// Screen or panel classification used by UIManager's registry.
/// </summary>
public enum ScreenType
{
    HudOverlay,
    Fullscreen,
    Modal,
    SemiModal,
    NonModal,
}

/// <summary>
/// Result codes for screen state transitions.
/// </summary>
public enum ScreenResult
{
    Success = 0,
    ErrDepartureLocked = 1,
    ErrModalOpen = 2,
    ErrInvalidScreen = 3,
}

/// <summary>
/// Result codes for modal panel requests.
/// </summary>
public enum ModalResult
{
    Success = 0,
    ErrAnotherModalOpen = 1,
    ErrDepartureLocked = 2,
    ErrInvalidPanel = 3,
    ErrQueued = 4,
}

/// <summary>
/// Registered UI screen or panel metadata.
/// </summary>
public sealed record ScreenDefinition(string Id, ScreenType Type, string OwnerSystem);

/// <summary>
/// Input routing layer priorities.
/// </summary>
public enum InputLayer
{
    Modal = 0,
    SemiModal = 1,
    NonModal = 2,
    Hud = 3,
    World = 4,
}

/// <summary>
/// Combat panel resolution choices that determine whether an overridden modal is restored.
/// </summary>
public enum CombatThreatResolution
{
    EmergencyTreatment = 0,
    HoldGround = 1,
    Retreat = 2,
}

/// <summary>
/// HUD mouse regions with different pointer pass-through behavior.
/// </summary>
public enum HudRegion
{
    Overlay = 0,
    InventorySlot = 1,
}

/// <summary>
/// Minimal Control mouse-filter model used by headless UI routing tests.
/// </summary>
public enum MouseFilterMode
{
    Ignore = 0,
    Pass = 1,
    Stop = 2,
}

/// <summary>
/// Immutable public snapshot of a panel's modal routing state.
/// </summary>
public sealed record ModalPanelSnapshot(
	string PanelId,
	IReadOnlyDictionary<string, string> DataContext,
	int ScrollOffset,
	int SelectedIndex,
	bool InputEnabled,
	double Opacity,
	int CanvasLayer,
	string FocusedElementId);

/// <summary>
/// Visual focus and hover tokens for a UI element.
/// </summary>
public sealed record ElementVisualState(
	bool KeyboardFocused,
	bool MouseHovered,
	string FocusStyleToken,
	string HoverStyleToken);

/// <summary>
/// Immutable public snapshot of one rendered HUD element in the headless UI model.
/// </summary>
public sealed record HudElementSnapshot(
	string ElementId,
	string Text,
	int BarWidth,
	string ColorHex,
	string ShapeToken,
	string IconToken,
	string BorderColorHex,
	bool Visible);

/// <summary>
/// Result from a deterministic HUD process tick.
/// </summary>
public sealed record HudProcessResult(
	int ProcessPriority,
	int UpdatedElementCount,
	bool DirtyFlagsCleared,
	bool IteratedDirtyElements,
	bool ImmediateReturn);

/// <summary>
/// 桌面窗口恢复后 UI 刷新与焦点恢复的只读快照。
/// </summary>
public sealed record DesktopRecoverySnapshot(
	bool FullRefreshRequested,
	int FullRefreshRequestCount,
	double LastProcessDeltaSeconds,
	IReadOnlyList<string> QueryNames,
	IReadOnlyList<string> VisiblePanelIds,
	string FocusElementId,
	bool MovementInputBlocked);

/// <summary>
/// 小尺寸状态指示元素的无障碍编码快照。
/// </summary>
public sealed record A11yEncodingSnapshot(
	string ElementId,
	string ColorHex,
	string ShapeToken,
	string TextLabel,
	string EdgeToken,
	int SegmentCount,
	int ElementSizePx);

/// <summary>
/// 前景色与背景色的 WCAG 对比度审计结果。
/// </summary>
public sealed record ColorContrastSnapshot(
	string ForegroundHex,
	string BackgroundHex,
	double Ratio,
	double RequiredRatio,
	bool Passes,
	string RecommendedForegroundHex);

/// <summary>
/// 可被新手引导定位的交互锚点元数据。
/// </summary>
public sealed record AnchorMetadataSnapshot(
	string AnchorId,
	string PanelId,
	bool Highlightable,
	int HighlightPriority,
	bool HighlightRequestAccepted);

/// <summary>
/// Focus-safe snapshot for one rendered onboarding hint overlay.
/// </summary>
public sealed record OnboardingHintRenderSnapshot(
	string StepId,
	string HintTextKey,
	string? AnchorId,
	string TargetSurfaceId,
	OnboardingSurface ActiveSurface,
	bool Visible,
	bool Skipped,
	string? FallbackReason,
	bool TextOnlyFallback,
	bool HasTextLabel,
	bool ColorOnlyMeaning,
	bool FocusDisabled,
	MouseFilterMode MouseFilter,
	bool CapturesKeyboardFocus,
	bool CapturesMouseInput,
	bool IsModal,
	bool CoversResourceLabel,
	bool CoversThreatLabel,
	bool CoversHullLabel,
	bool CoversStatusLabel,
	bool KeyboardPathValid,
	bool MousePathValid,
	string ReadabilityGuardToken);

/// <summary>
/// 当前焦点元素处理 Enter 键后的结果。
/// </summary>
public enum FocusActivationResult
{
	Activated = 0,
	DisabledNoOp = 1,
	NoFocusableElement = 2,
}

/// <summary>
/// Headless lifecycle states for panels that will later map to Godot Control nodes.
/// </summary>
public enum PanelLifecycleState
{
	Unloaded = 0,
	Ready = 1,
	Active = 2,
	Closed = 3,
}

/// <summary>
/// Immutable public snapshot of non-modal/modal panel lifecycle state.
/// </summary>
public sealed record PanelLifecycleSnapshot(
	string PanelId,
	PanelLifecycleState State,
	bool IsPreloaded,
	bool PreloadNonBlocking,
	bool DistanceDriven,
	bool DomainEventDriven,
	double OpenAnimationSeconds,
	double CloseAnimationSeconds,
	double InstantiationDelayMilliseconds);

/// <summary>
/// Immutable public snapshot of the panel cache pool.
/// </summary>
public sealed record PanelCacheSnapshot(
	IReadOnlyList<string> CachedPanelIds,
	IReadOnlyList<string> FreedPanelIds);

/// <summary>
/// Headless StationDetailPanel binding snapshot.
/// </summary>
public sealed record StationDetailSnapshot(
	string TemplateId,
	string StationId,
	string DisplayName);

/// <summary>
/// 面板打开时绑定到 UI 的上游数据快照。
/// </summary>
public sealed record PanelBindingSnapshot(
	string PanelId,
	IReadOnlyDictionary<string, string> Fields,
	IReadOnlyList<string> QueryNames,
	IReadOnlyList<string> RenderedItemIds,
	string EmptyStateMessage,
	bool EmptyStateVisible,
	bool UsedDisplayNameFallback,
	int BindSequence);

/// <summary>
/// UI 提交动作发给领域系统后的命令快照。
/// </summary>
public sealed record UiDomainCommandSnapshot(
	string MethodName,
	IReadOnlyDictionary<string, string> Arguments,
	bool Success);

/// <summary>
/// UI 动画缓动预设；只允许 ADR-0012 批准的固定曲线。
/// </summary>
public enum UiAnimationEasing
{
	Linear = 0,
	EaseIn = 1,
	EaseOut = 2,
	EaseInOut = 3,
}

/// <summary>
/// 单个属性 Tween 的只读契约快照。
/// </summary>
public sealed record AnimationPropertyTween(
	string PropertyName,
	string FromValue,
	string ToValue,
	double DurationSeconds,
	UiAnimationEasing Easing);

/// <summary>
/// UI 动画运行时契约快照，用于把 Godot Tween 行为映射到可验证的 C# 合同。
/// </summary>
public sealed record UiAnimationSnapshot(
	string AnimationId,
	string TargetId,
	double DurationSeconds,
	double DwellSeconds,
	UiAnimationEasing Easing,
	bool UsesSceneTreeTween,
	bool UsesManualProcessInterpolation,
	bool UsesShaderMaterial,
	string ShaderUniformName,
	int ShaderUniformWritesPerFrame,
	bool ProgressTextRealtime,
	bool AutoRemovesOnFinished,
	bool IsKilled,
	bool FinalStateApplied,
	IReadOnlyList<AnimationPropertyTween> PropertyTweens);

/// <summary>
/// Runtime adapter that maps UIManager animation requests to the active engine layer.
/// </summary>
public interface IUiAnimationDriver
{
	UiAnimationSnapshot PlayTween(UiAnimationSnapshot request);

	UiAnimationSnapshot KillTween(UiAnimationSnapshot activeSnapshot, bool applyFinalState);

	PanelTextureContractSnapshot ConfigurePanelTexture(PanelTextureContractSnapshot contract);
}

/// <summary>
/// 动画被强制打断后的收尾状态快照。
/// </summary>
public sealed record AnimationInterruptionSnapshot(
	string InterruptedAnimationId,
	bool ExistingTweenKilled,
	bool FinalStateApplied,
	double FinalOpacity,
	bool FinalVisible,
	double FinalScale,
	int FlashCount,
	double FlashSecondsPerBlink,
	string FlashColorHex);

/// <summary>
/// 羊皮纸面板纹理展开策略的验证快照。
/// </summary>
public sealed record PanelTextureContractSnapshot(
	string PanelId,
	bool UsesNinePatchRect,
	int SourceTextureWidth,
	int SourceTextureHeight,
	bool UsesFullSizeTexture);

/// <summary>
/// 修复提交材料的强类型信号参数。
/// </summary>
public sealed record MaterialSubmission(string MaterialId, int Quantity);

/// <summary>
/// 下游语义事件声明的类型合同。
/// </summary>
public sealed record UiSemanticEventContract(
	string EventId,
	IReadOnlyList<string> ParameterTypeNames,
	bool UsesDictionaryPayload,
	bool EmitsSynchronously,
	int MaxCascadeDepth);

/// <summary>
/// 一次语义事件发射的顺序与参数快照。
/// </summary>
public sealed record UiSemanticEventSnapshot(
	string EventId,
	IReadOnlyList<string> Arguments,
	int ActionSequence,
	int EmitSequence,
	bool EmittedAfterAction,
	bool EmittedSynchronously,
	int CascadeDepth);

/// <summary>
/// UIManager 只读查询和提交动作所需的上游领域接口集合。
/// </summary>
public interface IUiUpstreamDataSource
{
	/// <summary>返回航图当前状态快照。</summary>
	IReadOnlyDictionary<string, string>? GetChartState();

	/// <summary>返回当前可见航线列表。</summary>
	IReadOnlyList<IReadOnlyDictionary<string, string>>? GetVisibleRoutes();

	/// <summary>返回当前选中航线详情。</summary>
	IReadOnlyDictionary<string, string>? GetSelectedRoute();

	/// <summary>返回航图过滤器状态。</summary>
	IReadOnlyDictionary<string, string>? GetFilterState();

	/// <summary>返回船体完整性状态。</summary>
	IReadOnlyDictionary<string, string>? GetHullIntegrity();

	/// <summary>返回飞艇模块状态列表。</summary>
	IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates();

	/// <summary>返回随身物品栏内容。</summary>
	IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory();

	/// <summary>返回仓库容量状态。</summary>
	IReadOnlyDictionary<string, string>? GetStorageState();

	/// <summary>返回货舱容量状态。</summary>
	IReadOnlyDictionary<string, string>? GetCargoState();

	/// <summary>返回当前云海币余额。</summary>
	int? GetCurrency();

	/// <summary>返回探索搜索进度。</summary>
	IReadOnlyDictionary<string, string>? GetSearchProgress();

	/// <summary>返回侦察预览等级。</summary>
	string? GetScoutPreviewLevel();

	/// <summary>返回撤离状态。</summary>
	IReadOnlyDictionary<string, string>? GetExtractionState();

	/// <summary>返回战斗威胁面板上下文。</summary>
	IReadOnlyDictionary<string, string>? BuildThreatContext();

	/// <summary>返回指定修复节点的修复状态。</summary>
	IReadOnlyDictionary<string, string>? GetRepairState(string nodeId);

	/// <summary>返回指定摊位的交易数据。</summary>
	IReadOnlyDictionary<string, string>? GetStallData(string stallId);

	/// <summary>返回伙伴当前名字。</summary>
	string? QueryPartnerName();

	/// <summary>返回可嗅辨物品候选列表。</summary>
	IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems();

	/// <summary>返回命名提示是否满足弹出条件。</summary>
	bool? NamingPromptEligibility();

	/// <summary>返回实体显示名。</summary>
	string? GetDisplayName(string entityId);

	/// <summary>返回实体描述文本。</summary>
	string? GetDescription(string entityId);

	/// <summary>通过资源系统转移物品。</summary>
	bool TransferItem(string itemId, string fromPool, string toPool, int quantity);

	/// <summary>通过资源系统丢弃物品。</summary>
	bool DiscardItem(string itemId);

	/// <summary>通过世界修复系统提交材料。</summary>
	bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials);

	/// <summary>通过集市系统执行购买。</summary>
	bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost);

	/// <summary>通过伙伴系统提交一次性命名。</summary>
	bool SubmitPartnerName(string partnerId, string name);
}

/// <summary>
/// Owns 12-screen state machine, single-slot modal stack, 4-layer input routing.
/// Consumes data from all domain systems; does not own gameplay state.
/// </summary>
public sealed class UIManager
{
	public const int CombatOverrideCanvasLayer = 100;
	public const int HudProcessPriority = -10;
	public const int HudMaxBarWidth = 200;
	public const int DefaultCarriedSlotCapacity = 5;
	public const int PanelCacheMax = 2;
	public const double PanelPreloadRadiusMultiplier = 1.5;
	public const double PanelAutoCloseRadiusMultiplier = 2.0;
	public const double PanelOpenAnimationSeconds = 0.25;
	public const double PanelCloseAnimationSeconds = 0.15;
	public const double RoutePulseAnimationSeconds = 0.3;
	public const double InkSpreadAnimationSeconds = 0.6;
	public const double DepartureGateSealAnimationSeconds = 1.2;
	public const double ExtractionProgressAnimationSeconds = 2.5;
	public const double ToastEnterAnimationSeconds = 0.2;
	public const double ToastDwellSeconds = 2.8;
	public const double SettlementSummaryEnterAnimationSeconds = 0.5;
	public const double NamingModalPopAnimationSeconds = 0.3;
	public const double ThreatInterruptionFlashSeconds = 0.5;
	public const int ThreatInterruptionFlashCount = 3;
	public const int ParchmentTextureSourceSize = 256;
	public const double PreloadedPanelInstantiationDelayMilliseconds = 0.5;
	public const double LazyPanelInstantiationDelayMilliseconds = 5.0;
	public const string SafeGreenHex = "#5FAF5F";
	public const string WarningAmberHex = "#E8A840";
	public const string DangerRedHex = "#D4644B";
	public const string DisabledGrayHex = "#7A7068";
	public const string ParchmentBackgroundHex = "#E4D2B3";
	public const string AccessibleDangerTextHex = "#98382D";
	public const string AccessibleBeaconTextHex = "#3C7F7B";
	public const double DesktopFullRefreshDeltaThresholdSeconds = 1.0;
	public const int NamingSkipMax = 3;
	public const string HullBarSignalId = "hull_bar";
	public const string HullBandSignalId = "hull_band";
	public const string StorageBarSignalId = "storage_bar";
	public const string CargoBarSignalId = "cargo_bar";
	public const string CarriedGridSignalId = "carried_grid";
	public const string SearchCountSignalId = "search_count";
	public const string ThreatPreviewSignalId = "threat_preview";
	public const string ModuleLightsSignalId = "module_lights";
	public const string CurrencyDisplaySignalId = "currency_display";
	public const string ScoutPreviewNone = "PREVIEW_NONE";
	public const string ScoutPreviewPresence = "PREVIEW_PRESENCE";
	public const string ScoutPreviewFull = "PREVIEW_FULL";
	public const string HubHullBarElementId = "S1_hull_bar";
	public const string HubHullRepairIconElementId = "S1_hull_repair_icon";
	public const string ExplorationHullBarElementId = "S5_hull_bar";
	public const string HubStorageElementId = "S1_storage";
	public const string HubCargoElementId = "S1_cargo";
	public const string ExplorationCarriedGridElementId = "S5_carried_grid";
	public const string ExplorationSearchCountElementId = "S5_search_count";
	public const string ExplorationThreatPreviewElementId = "S5_threat_preview";
	public const string HubModuleLightsElementId = "S1_module_lights";
	public const string HubCurrencyElementId = "S1_currency";
	public const string MarketCurrencyElementId = "S9_currency";
	public const string StationDetailTemplateId = "StationDetailPanel";
	public const string PartnerSniffAnchorId = "anchor.partner_sniff";
	public const string StorageAnchorId = "anchor.storage";
	public const string RepairAnchorId = "anchor.repair";
	public const string IntelStationAnchorId = "station.intel";
	public const string StorageStationAnchorId = "station.storage";
	public const string CurrentActionUnavailableToast = "ui.current_action_unavailable";
	public const string CargoModuleRequiredToast = "需要先安装货舱模块";
	public const string CombatResponseRequiredPrompt = "ui.combat_response_required";
	public const string DisabledControlDefaultTooltip = "当前条件不满足";
	public const string FocusStyleToken = "focus:#4FB7B2:1.5px-solid";
	public const string HoverStyleToken = "hover:brightness+10:no-border";
	public const string PanelOpenAnimationIdPrefix = "panel_open:";
	public const string PanelCloseAnimationIdPrefix = "panel_close:";
	public const string RoutePulseAnimationId = "route_selected_pulse";
	public const string InkSpreadAnimationId = "ink_spread";
	public const string DepartureGateSealAnimationId = "departure_gate_seal";
	public const string ExtractionProgressAnimationId = "extraction_progress";
	public const string ExtractionThreatInterruptedAnimationId = "extraction_threat_interrupted";
	public const string RepairToastAnimationId = "toast:repair_submitted";
	public const string SettlementSummaryEnterAnimationId = "settlement_summary_enter";
	public const string NamingModalPopAnimationId = "naming_modal_pop";
	public const string InkProgressShaderUniform = "progress";
	public const string UIRouteSelectedEventId = "ui_route_selected";
	public const string UIDepartureConfirmedEventId = "ui_departure_confirmed";
	public const string UIThreatResponseChosenEventId = "ui_threat_response_chosen";
	public const string UIRepairSubmittedEventId = "ui_repair_submitted";
	public const string UIPurchaseConfirmedEventId = "ui_purchase_confirmed";
	public const string UIItemTransferredEventId = "ui_item_transferred";
	public const string UINamingConfirmedEventId = "ui_naming_confirmed";
	public const string UISettlementClosedEventId = "ui_settlement_closed";
	public const string UIPanelOpenedEventId = "ui_panel_opened";
	public const string UIPanelClosedEventId = "ui_panel_closed";
	public const int MaxSemanticCascadeDepth = 2;
	public const string HubHudScreenId = "S1_hub_hud";
	public const string StationDetailScreenId = "S2_station_detail";
	public const string DepartureConfirmScreenId = "S3_departure_confirm";
	public const string ChartScreenId = "S4_chart";
	public const string ExplorationHudScreenId = "S5_exploration_hud";
	public const string CapacityChoiceScreenId = "S6a_capacity_choice";
	public const string ExtractionProgressScreenId = "S6b_extraction_progress";
	public const string SettlementSummaryScreenId = "S6c_settlement_summary";
	public const string CombatScreenId = "S7_combat";
	public const string RepairScreenId = "S8_repair";
	public const string MarketScreenId = "S9_market";
	public const string NamingScreenId = "S10_naming";
	public const string PartnerSniffScreenId = "S11_partner_sniff";
	public const string StorageScreenId = "S12_storage";
	public const string RegistryDiagnosticToolsPanelId = "registry_diagnostic_tools";
	public const string PartnerSniffEmptyStateMessage = "猫没有闻到任何值得注意的气味——试试从探索中带回更多材料";
	public const string StorageEmptyStateMessage = "从探索中带回材料或拆包货物来填充";
	public const string ChartEmptyStateMessage = "没有可读取的航线——去情报台了解更多信息";
	public const string PartnerUnavailableMessage = "伙伴系统暂不可用";
	public const string MissingResourceDisplay = "—";
	public const string CarriedPoolId = "CARRIED";
	public const string StoragePoolId = "STORAGE";

	private readonly IUiUpstreamDataSource upstreamDataSource;
	private readonly IUiAnimationDriver animationDriver;
	private readonly Stack<string> focusRestoreStack = new();
	private readonly Queue<PendingModalRequest> queuedModals = new();
	private readonly HashSet<string> visiblePanels = new(StringComparer.Ordinal);
	private readonly Dictionary<string, PanelRuntimeState> panelStates = new(StringComparer.Ordinal);
	private readonly List<string> nonModalPanels = new();
	private readonly Dictionary<string, int> panelZIndices = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ScreenDefinition> screenRegistry = BuildScreenRegistry();
	private readonly Dictionary<string, bool> dirtyFlags = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Dictionary<string, string>> pendingPayloads = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HudElementSnapshot> hudElements = new(StringComparer.Ordinal);
	private readonly Dictionary<int, CarriedSlotState> carriedSlots = new();
	private readonly Dictionary<string, PanelLifecycleRuntimeState> panelLifecycleStates = new(StringComparer.Ordinal);
	private readonly HashSet<string> preloadedPanels = new(StringComparer.Ordinal);
	private readonly List<string> panelCacheLru = new();
	private readonly List<string> freedPanelInstances = new();
	private readonly Dictionary<string, UiAnimationSnapshot> animationSnapshots = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> activeAnimationByTarget = new(StringComparer.Ordinal);
	private readonly List<UiSemanticEventSnapshot> semanticEventLog = new();
	private readonly Dictionary<string, UiSemanticEventContract> semanticEventContracts = BuildSemanticEventContracts();
	private readonly Dictionary<string, PanelTextureContractSnapshot> panelTextureContracts;
	private readonly Dictionary<string, string> disabledElementTooltips = new(StringComparer.Ordinal);
	private readonly HashSet<string> destroyedFocusableElements = new(StringComparer.Ordinal);
	private readonly Dictionary<string, AnchorMetadataSnapshot> anchorMetadata = new(StringComparer.Ordinal);
	private readonly List<OnboardingHintRenderSnapshot> onboardingHintSnapshots = new();
	private StationDetailSnapshot? lastStationDetailSnapshot;
	private PanelBindingSnapshot? lastPanelBindingSnapshot;
	private UiDomainCommandSnapshot? lastDomainCommandSnapshot;
	private AnimationInterruptionSnapshot? lastAnimationInterruptionSnapshot;
	private string modalPanel = string.Empty;
	private string combatOverridePanelId = string.Empty;
	private string selectedRouteId = string.Empty;
	private string selectedRouteName = string.Empty;
	private bool routeSidePanelExpanded;
	private bool legacyInitialHubTransitionPending;
	private int bindSequence;
	private int nextPanelZIndex = 10;
	private int actionSequence;
	private int emitSequence;
	private string activeSemanticDispatchEventId = string.Empty;
	private int semanticCascadeDepth;
	private int semanticCascadeMaxDepth;
	private double lastProcessDeltaSeconds;
	private int fullUiRefreshRequestCount;
	private IReadOnlyList<string> lastFullRefreshQueryNames = Array.Empty<string>();
	private bool cargoModuleInstalled = true;
	private bool lastHighlightRequestRejectedSilently;

	/// <summary>创建 UIManager，并注入领域数据查询接口。</summary>
	public UIManager(IUiUpstreamDataSource? upstreamDataSource = null, IUiAnimationDriver? animationDriver = null)
	{
		this.upstreamDataSource = upstreamDataSource ?? NullUiUpstreamDataSource.Instance;
		this.animationDriver = animationDriver ?? HeadlessUiAnimationDriver.Instance;
		panelTextureContracts = BuildPanelTextureContracts()
			.ToDictionary(
				item => item.Key,
				item => this.animationDriver.ConfigurePanelTexture(item.Value),
				StringComparer.Ordinal);
	}

	/// <summary>Raised when the active screen changes.</summary>
	public event Action<Screen, Screen>? ScreenChanged;

	/// <summary>Raised when the UI system is ready.</summary>
	public event Action? UIReady;

	/// <summary>Raised when a modal panel opens.</summary>
	public event Action<string>? UIPanelOpened;

	/// <summary>Raised when a modal panel closes.</summary>
	public event Action<string>? UIPanelClosed;

	/// <summary>航图路线选择完成后同步发射。</summary>
	public event Action<string, string>? UIRouteSelected;

	/// <summary>出航确认状态提交完成后同步发射。</summary>
	public event Action<string, string>? UIDepartureConfirmed;

	/// <summary>威胁响应处理完成后同步发射。</summary>
	public event Action<string, string>? UIThreatResponseChosen;

	/// <summary>修复材料提交成功后同步发射。</summary>
	public event Action<string, IReadOnlyList<MaterialSubmission>>? UIRepairSubmitted;

	/// <summary>摊位购买成功后同步发射。</summary>
	public event Action<string, string, int, int>? UIPurchaseConfirmed;

	/// <summary>物品转移完成后同步发射。</summary>
	public event Action<string, string, string, int>? UIItemTransferred;

	/// <summary>伙伴命名提交成功后同步发射。</summary>
	public event Action<string, string>? UINamingConfirmed;

	/// <summary>结算摘要关闭后同步发射。</summary>
	public event Action<string, IReadOnlyList<string>, IReadOnlyList<string>>? UISettlementClosed;

	/// <summary>Current active screen.</summary>
	public Screen CurrentScreen { get; private set; } = Screen.None;

	/// <summary>Current active input layer.</summary>
	public InputLayer ActiveInputLayer { get; private set; } = InputLayer.World;

	/// <summary>Whether the UI system has been initialized.</summary>
	public bool IsInitialized { get; private set; }

	/// <summary>Whether departure lock is currently rejecting screen and panel requests.</summary>
	public bool DepartureLocked { get; private set; }

	/// <summary>Remaining simulated departure-lock seconds for deterministic tests.</summary>
	public double DepartureLockRemainingSeconds { get; private set; }

	/// <summary>Whether the Hub HUD overlay is visible.</summary>
	public bool HubHudVisible => visiblePanels.Contains(HubHudScreenId);

	/// <summary>Whether the chart fullscreen panel is visible.</summary>
	public bool ChartVisible => visiblePanels.Contains(ChartScreenId);

	/// <summary>Whether the exploration HUD overlay is visible.</summary>
	public bool ExplorationHudVisible => visiblePanels.Contains(ExplorationHudScreenId);

	/// <summary>Whether the route-selected side panel is expanded.</summary>
	public bool RouteSidePanelExpanded => routeSidePanelExpanded;

	/// <summary>Whether route confirmation focus was assigned after route selection.</summary>
	public bool DepartureConfirmButtonFocused { get; private set; }

	/// <summary>Whether the chart departure ink diffusion sequence has started.</summary>
	public bool InkDiffusionStarted { get; private set; }

	/// <summary>Whether the chart departure gate-lock animation has started.</summary>
	public bool DepartureGateLocked { get; private set; }

	/// <summary>Whether the voyage black-screen transition has started.</summary>
	public bool BlackScreenTransitionStarted { get; private set; }

	/// <summary>Registered screen and panel metadata keyed by stable screen ID.</summary>
	public IReadOnlyDictionary<string, ScreenDefinition> ScreenRegistry => screenRegistry;

	/// <summary>Returns the current modal ID, or an empty string when no modal is open.</summary>
	public string CurrentModalId => modalPanel;

	/// <summary>Returns the queued modal ID, or an empty string when no modal is queued.</summary>
	public string PendingModalId => queuedModals.Count > 0 ? queuedModals.Peek().PanelId : string.Empty;

	/// <summary>Whether a modal mask should be rendered behind the active modal.</summary>
	public bool ModalMaskVisible => IsModalOpen();

	/// <summary>Whether the current S7 combat panel is overriding another modal.</summary>
	public bool HasCombatOverrideSnapshot => !string.IsNullOrEmpty(combatOverridePanelId);

	/// <summary>Last toast token requested by UI routing.</summary>
	public string LastToastMessage { get; private set; } = string.Empty;

	/// <summary>Last visual prompt token requested by UI routing.</summary>
	public string LastVisualPrompt { get; private set; } = string.Empty;

	/// <summary>最近一次禁用控件向玩家展示的提示文本。</summary>
	public string LastTooltipMessage { get; private set; } = string.Empty;

	/// <summary>最近一次引导高亮请求是否因出航锁定被静默拒绝。</summary>
	public bool LastHighlightRequestRejectedSilently => lastHighlightRequestRejectedSilently;

	/// <summary>当前可见的新手引导提示快照；默认最多一个。</summary>
	public IReadOnlyList<OnboardingHintRenderSnapshot> OnboardingHintSnapshots => onboardingHintSnapshots.AsReadOnly();

	/// <summary>最近一次 `_process` 记录的桌面帧 delta。</summary>
	public double LastProcessDeltaSeconds => lastProcessDeltaSeconds;

	/// <summary>强制全量 UI 刷新的累计请求次数。</summary>
	public int FullUiRefreshRequestCount => fullUiRefreshRequestCount;

	/// <summary>Whether the naming modal has been skipped through Escape.</summary>
	public bool NamingSkipped { get; private set; }

	/// <summary>伙伴命名弹窗已跳过次数，用于三次后关闭弹窗窗口。</summary>
	public int NamingSkipCount { get; private set; }

	/// <summary>Current keyboard focus element ID.</summary>
	public string KeyboardFocusElementId { get; private set; } = string.Empty;

	/// <summary>Current mouse hover element ID.</summary>
	public string MouseHoverElementId { get; private set; } = string.Empty;

	/// <summary>Whether S7 was preloaded during UI initialization.</summary>
	public bool CombatPanelPreloaded => preloadedPanels.Contains(CombatScreenId);

	/// <summary>_Ready 阶段执行的安全动作。</summary>
	public IReadOnlyList<string> ReadyPhaseActions { get; private set; } = Array.Empty<string>();

	/// <summary>ui_ready 阶段执行的初始化动作。</summary>
	public IReadOnlyList<string> UiReadyPhaseActions { get; private set; } = Array.Empty<string>();

	/// <summary>ui_ready 阶段连接的 HUD 信号数量。</summary>
	public int HudSignalConnectionCount { get; private set; }

	/// <summary>最近一次面板数据绑定快照。</summary>
	public PanelBindingSnapshot? LastPanelBindingSnapshot => lastPanelBindingSnapshot;

	/// <summary>最近一次 UI 提交到领域系统的命令快照。</summary>
	public UiDomainCommandSnapshot? LastDomainCommandSnapshot => lastDomainCommandSnapshot;

	/// <summary>最近一次动画打断的收尾快照。</summary>
	public AnimationInterruptionSnapshot? LastAnimationInterruptionSnapshot => lastAnimationInterruptionSnapshot;

	/// <summary>所有下游语义事件声明合同。</summary>
	public IReadOnlyDictionary<string, UiSemanticEventContract> SemanticEventContracts => semanticEventContracts;

	/// <summary>已发射的下游语义事件日志。</summary>
	public IReadOnlyList<UiSemanticEventSnapshot> SemanticEventLog => semanticEventLog;

	/// <summary>所有需要 NinePatch 羊皮纸背景的面板纹理合同。</summary>
	public IReadOnlyList<PanelTextureContractSnapshot> ParchmentPanelTextureContracts => panelTextureContracts.Values.ToArray();

	/// <summary>Initializes and emits UIReady.</summary>
	public void Initialize()
	{
		ReadyPhaseActions = new[]
		{
			"constant_init",
			"register_screen_registry",
			"declare_signals",
		};
		IsInitialized = true;
		HudSignalConnectionCount = 11;
		preloadedPanels.Add(CombatScreenId);
		RegisterHighlightableAnchor(RepairAnchorId, RepairScreenId, priority: 80);
		TransitionTo(Screen.Hub, validate: false);
		visiblePanels.Add(HubHudScreenId);
		UiReadyPhaseActions = new[]
		{
			"connect_11_hud_signals",
			$"preload:{CombatScreenId}",
			$"show:{HubHudScreenId}",
			$"hide:{ExplorationHudScreenId}",
		};
		legacyInitialHubTransitionPending = true;
		UIReady?.Invoke();
	}

	/// <summary>Transitions to a new screen. Returns false if already on that screen.</summary>
	public bool TransitionScreen(Screen newScreen)
	{
		if (legacyInitialHubTransitionPending && newScreen == Screen.Hub && CurrentScreen == Screen.Hub)
		{
			legacyInitialHubTransitionPending = false;
			ScreenChanged?.Invoke(Screen.None, Screen.Hub);
			return true;
		}

		legacyInitialHubTransitionPending = false;
		return OpenScreen(newScreen) == ScreenResult.Success;
	}

	/// <summary>Requests a fullscreen screen transition using the Story 001 screen FSM guards.</summary>
	public ScreenResult OpenScreen(Screen newScreen)
	{
		if (DepartureLocked)
		{
			return ScreenResult.ErrDepartureLocked;
		}

		if (IsModalOpen())
		{
			return ScreenResult.ErrModalOpen;
		}

		return TransitionTo(newScreen, validate: true);
	}

	/// <summary>Sets the keyboard focus owner for deterministic focus-routing tests.</summary>
	public void SetKeyboardFocus(string elementId)
	{
		KeyboardFocusElementId = elementId ?? string.Empty;
	}

	/// <summary>
	/// Opens a modal panel. Combat (S7_combat) overrides current modal; other panels are rejected.
	/// </summary>
	public bool OpenModal(string panelId)
	{
		return OpenModalPanel(panelId) == ModalResult.Success;
	}

	/// <summary>
	/// Opens a modal panel with ADR-0012 guard semantics.
	/// </summary>
	public ModalResult OpenModalPanel(string panelId)
	{
		return OpenModalPanel(panelId, dataContext: null);
	}

	/// <summary>
	/// Opens a modal panel with deterministic context fields for save and restore validation.
	/// </summary>
	public ModalResult OpenModalPanel(
		string panelId,
		IReadOnlyDictionary<string, string>? dataContext,
		int scrollOffset = 0,
		int selectedIndex = -1)
	{
		if (DepartureLocked)
		{
			return ModalResult.ErrDepartureLocked;
		}

		if (!screenRegistry.TryGetValue(panelId, out var definition) || definition.Type != ScreenType.Modal)
		{
			return ModalResult.ErrInvalidPanel;
		}

		if (!string.IsNullOrEmpty(modalPanel))
		{
			if (panelId == CombatScreenId)
			{
				return OpenCombatOverride(dataContext, scrollOffset, selectedIndex);
			}

			if (panelId == NamingScreenId)
			{
				queuedModals.Enqueue(new PendingModalRequest(panelId, dataContext, scrollOffset, selectedIndex));
				return ModalResult.ErrQueued;
			}

			LastToastMessage = CurrentActionUnavailableToast;
			return ModalResult.ErrAnotherModalOpen;
		}

		var canvasLayer = panelId == CombatScreenId ? CombatOverrideCanvasLayer : 0;
		OpenModalCore(panelId, dataContext, scrollOffset, selectedIndex, canvasLayer, pushFocusRestore: true);
		return ModalResult.Success;
	}

	/// <summary>Closes the current modal, restoring the previous one from stack if any.</summary>
	public void CloseModal()
	{
		if (string.IsNullOrEmpty(modalPanel))
		{
			return;
		}

		var closedId = modalPanel;
		if (closedId == CombatScreenId)
		{
			LastVisualPrompt = CombatResponseRequiredPrompt;
			return;
		}

		modalPanel = string.Empty;
		visiblePanels.Remove(closedId);
		panelStates.Remove(closedId);
		MarkPanelClosed(closedId, closeAnimationSeconds: 0);
		RestorePreviousFocus();
		EmitUiPanelClosed(closedId);

		if (!OpenNextQueuedModal())
		{
			UpdateActiveInputLayer();
		}
	}

	/// <summary>Returns whether a modal panel is currently open.</summary>
	public bool IsModalOpen()
	{
		return !string.IsNullOrEmpty(modalPanel);
	}

	/// <summary>Closes S7 and restores or discards the overridden modal based on combat result.</summary>
	public ModalResult ResolveCombatThreat(CombatThreatResolution resolution)
	{
		if (modalPanel != CombatScreenId)
		{
			return ModalResult.ErrInvalidPanel;
		}

		visiblePanels.Remove(CombatScreenId);
		panelStates.Remove(CombatScreenId);
		EmitUiPanelClosed(CombatScreenId);

		if (string.IsNullOrEmpty(combatOverridePanelId))
		{
			modalPanel = string.Empty;
			RestorePreviousFocus();
			UpdateActiveInputLayer();
			return ModalResult.Success;
		}

		var restoredPanelId = combatOverridePanelId;
		combatOverridePanelId = string.Empty;

		if (resolution == CombatThreatResolution.Retreat)
		{
			visiblePanels.Remove(restoredPanelId);
			panelStates.Remove(restoredPanelId);
			modalPanel = string.Empty;
			RestorePreviousFocus();
			UpdateActiveInputLayer();
			return ModalResult.Success;
		}

		modalPanel = restoredPanelId;
		var restored = GetOrCreatePanelState(restoredPanelId);
		restored.InputEnabled = true;
		restored.Opacity = 1.0;
		restored.CanvasLayer = 0;
		KeyboardFocusElementId = string.IsNullOrEmpty(restored.FocusedElementId)
			? FirstFocusableElement(restoredPanelId)
			: restored.FocusedElementId;
		ActiveInputLayer = InputLayer.Modal;
		return ModalResult.Success;
	}

	/// <summary>Handles Hub gangway Use and opens the departure confirmation modal.</summary>
	public ModalResult UseGangway()
	{
		return CurrentScreen == Screen.Hub
			? OpenModalPanel(DepartureConfirmScreenId)
			: ModalResult.ErrInvalidPanel;
	}

	/// <summary>Handles Hub helm Use and opens the departure confirmation modal.</summary>
	public ModalResult UseHelm()
	{
		return UseGangway();
	}

	/// <summary>Handles the M key shortcut to open chart from Hub.</summary>
	public ScreenResult PressMapKey()
	{
		if (DepartureLocked)
		{
			return ScreenResult.ErrDepartureLocked;
		}

		if (GetActiveInputLayer() != InputLayer.World)
		{
			return ScreenResult.ErrModalOpen;
		}

		if (CurrentScreen != Screen.Hub)
		{
			return ScreenResult.ErrModalOpen;
		}

		return OpenScreen(Screen.Chart);
	}

	/// <summary>Handles Escape according to Story 001 screen guards.</summary>
	public ScreenResult PressEscape()
	{
		if (IsModalOpen())
		{
			if (modalPanel == CombatScreenId)
			{
				LastVisualPrompt = CombatResponseRequiredPrompt;
				return ScreenResult.Success;
			}

			if (modalPanel == NamingScreenId)
			{
				NamingSkipped = true;
				NamingSkipCount++;
			}

			CloseModal();
			return ScreenResult.Success;
		}

		if (nonModalPanels.Count > 0)
		{
			CloseTopNonModal();
			return ScreenResult.Success;
		}

		if (CurrentScreen is Screen.Chart or Screen.ChartRouteSelected)
		{
			routeSidePanelExpanded = false;
			DepartureConfirmButtonFocused = false;
			return TransitionTo(Screen.Hub, validate: true);
		}

		if (CurrentScreen is Screen.ChartDepartureConfirmed or Screen.Extracting)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		return ScreenResult.ErrInvalidScreen;
	}

	/// <summary>Confirms departure from either the Hub modal or the chart route-selected state.</summary>
	public ScreenResult ConfirmDeparture()
	{
		if (CurrentScreen == Screen.Hub && modalPanel == DepartureConfirmScreenId)
		{
			EnterDepartureLocked();
			EmitUiDepartureConfirmed(string.Empty, "hub");
			return ScreenResult.Success;
		}

		if (CurrentScreen == Screen.ChartRouteSelected)
		{
			InkDiffusionStarted = true;
			DepartureGateLocked = true;
			RecordInkSpreadAnimation();
			var result = TransitionTo(Screen.ChartDepartureConfirmed, validate: true);
			if (result == ScreenResult.Success)
			{
				EmitUiDepartureConfirmed(selectedRouteId, "chart");
			}

			return result;
		}

		return ScreenResult.ErrInvalidScreen;
	}

	/// <summary>Completes the 2.0s Hub departure lock and opens the chart screen.</summary>
	public ScreenResult CompleteDepartureLockTimer()
	{
		if (CurrentScreen != Screen.DepartureLocked)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		DepartureLocked = false;
		DepartureLockRemainingSeconds = 0;
		return TransitionTo(Screen.Chart, validate: false);
	}

	/// <summary>Marks a chart route selected and expands the route side panel.</summary>
	public ScreenResult SelectRoute(string routeId)
	{
		if (string.IsNullOrWhiteSpace(routeId) || CurrentScreen != Screen.Chart)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		routeSidePanelExpanded = true;
		DepartureConfirmButtonFocused = true;
		selectedRouteId = routeId;
		var result = TransitionTo(Screen.ChartRouteSelected, validate: true);
		if (result == ScreenResult.Success)
		{
			BindScreenData(ChartScreenId);
			selectedRouteName = LastPanelBindingSnapshot?.Fields.TryGetValue("selected_name", out var routeName) == true
				? routeName
				: routeId;
			RecordRoutePulseAnimation(routeId);
			EmitUiRouteSelected(routeId, selectedRouteName);
		}

		return result;
	}

	/// <summary>Completes the chart departure lock and starts voyage transition.</summary>
	public ScreenResult CompleteChartLock()
	{
		if (CurrentScreen != Screen.ChartDepartureConfirmed)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		BlackScreenTransitionStarted = true;
		return TransitionTo(Screen.Voyage, validate: true);
	}

	/// <summary>Moves from voyage into exploration when the encounter context is ready.</summary>
	public ScreenResult EncounterContextReady()
	{
		return CurrentScreen == Screen.Voyage
			? TransitionTo(Screen.Exploration, validate: true)
			: ScreenResult.ErrInvalidScreen;
	}

	/// <summary>Starts extraction and displays the semi-modal progress panel.</summary>
	public ScreenResult ExtractionStarted()
	{
		if (CurrentScreen != Screen.Exploration)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		visiblePanels.Add(ExtractionProgressScreenId);
		ActiveInputLayer = InputLayer.SemiModal;
		RecordExtractionProgressAnimation();
		return TransitionTo(Screen.Extracting, validate: true);
	}

	/// <summary>Completes extraction and opens the settlement summary modal.</summary>
	public ScreenResult ExtractionComplete()
	{
		if (CurrentScreen != Screen.Extracting)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		visiblePanels.Remove(ExtractionProgressScreenId);
		var result = TransitionTo(Screen.Settlement, validate: true);
		OpenModalPanel(SettlementSummaryScreenId);
		return result;
	}

	/// <summary>Confirms settlement summary and starts the Hub arrival sequence.</summary>
	public ScreenResult SettlementConfirmed()
	{
		if (CurrentScreen != Screen.Settlement)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		if (modalPanel == SettlementSummaryScreenId)
		{
			CloseModal();
		}

		return TransitionTo(Screen.HubArriving, validate: true);
	}

	/// <summary>Completes Hub arrival and optionally opens the partner naming modal.</summary>
	public ScreenResult ArrivalComplete(bool namingEligible)
	{
		if (CurrentScreen != Screen.HubArriving)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		var result = TransitionTo(Screen.Hub, validate: true);
		if (IsNamingPromptEligible(namingEligible))
		{
			OpenModalPanel(NamingScreenId);
		}

		return result;
	}

	/// <summary>设置命名跳过次数，三次及以上会关闭后续到达序列弹窗。</summary>
	public void SetNamingSkipCount(int skipCount)
	{
		NamingSkipCount = Math.Max(0, skipCount);
	}

	/// <summary>返回领域条件与跳过次数共同决定的命名弹窗资格。</summary>
	public bool IsNamingPromptEligible(bool domainEligible)
	{
		return domainEligible && NamingSkipCount < NamingSkipMax;
	}

	/// <summary>Returns whether the HUD element has a pending dirty flag.</summary>
	public bool IsHudElementDirty(string elementId)
	{
		return dirtyFlags.ContainsKey(elementId);
	}

	/// <summary>Returns a pending HUD payload captured from the most recent signal.</summary>
	public IReadOnlyDictionary<string, string> GetPendingHudPayload(string elementId)
	{
		return pendingPayloads.TryGetValue(elementId, out var payload)
			? new Dictionary<string, string>(payload, StringComparer.Ordinal)
			: new Dictionary<string, string>(StringComparer.Ordinal);
	}

	/// <summary>Runs one deterministic HUD batch update tick.</summary>
	public HudProcessResult ProcessHudFrame()
	{
		if (dirtyFlags.Count == 0)
		{
			return new HudProcessResult(
				HudProcessPriority,
				UpdatedElementCount: 0,
				DirtyFlagsCleared: true,
				IteratedDirtyElements: false,
				ImmediateReturn: true);
		}

		var updatedElements = 0;
		foreach (var elementId in dirtyFlags.Keys.ToArray())
		{
			if (pendingPayloads.TryGetValue(elementId, out var payload))
			{
				updatedElements += UpdateHudElement(elementId, payload);
			}
		}

		dirtyFlags.Clear();
		pendingPayloads.Clear();
		return new HudProcessResult(
			HudProcessPriority,
			updatedElements,
			DirtyFlagsCleared: true,
			IteratedDirtyElements: true,
			ImmediateReturn: false);
	}

	/// <summary>Returns a rendered HUD element snapshot, or an empty invisible snapshot.</summary>
	public HudElementSnapshot GetHudElementSnapshot(string elementId)
	{
		return hudElements.TryGetValue(elementId, out var snapshot)
			? snapshot
			: EmptyHudElement(elementId);
	}

	/// <summary>Captures a hull integrity change signal for dirty-flag processing.</summary>
	public void OnHullIntegrityChanged(int oldValue, int newValue)
	{
		OnHudSignal(
			HullBarSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["old"] = oldValue.ToString(),
				["new"] = newValue.ToString(),
			});
	}

	/// <summary>Captures a hull band change signal for dirty-flag processing.</summary>
	public void OnHullBandChanged(string oldBand, string newBand)
	{
		OnHudSignal(
			HullBandSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["old_band"] = oldBand,
				["new_band"] = newBand,
			});
	}

	/// <summary>Captures a storage capacity signal for dirty-flag processing.</summary>
	public void OnStorageChanged(int current, int max)
	{
		OnHudSignal(
			StorageBarSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["current"] = current.ToString(),
				["max"] = max.ToString(),
			});
	}

	/// <summary>Captures a carried inventory slot signal for dirty-flag processing.</summary>
	public void OnCarriedChanged(int slot, string itemId, int quantity)
	{
		var normalizedItemId = itemId ?? string.Empty;
		UpdateCarriedSlotState(slot, normalizedItemId, quantity);

		OnHudSignal(
			CarriedGridSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["slot"] = slot.ToString(),
				["item"] = normalizedItemId,
				["qty"] = quantity.ToString(),
				["occupied_slots"] = CountOccupiedCarriedSlots().ToString(),
				["capacity"] = DefaultCarriedSlotCapacity.ToString(),
			});
	}

	/// <summary>Captures an exploration search progress signal for dirty-flag processing.</summary>
	public void OnSearchProgressChanged(int searched, int total)
	{
		OnHudSignal(
			SearchCountSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["searched"] = searched.ToString(),
				["total"] = total.ToString(),
			});
	}

	/// <summary>Captures a scout preview signal for dirty-flag processing.</summary>
	public void OnScoutPreviewChanged(string previewLevel)
	{
		var normalizedPreviewLevel = previewLevel ?? string.Empty;

		OnHudSignal(
			ThreatPreviewSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["level"] = normalizedPreviewLevel,
			});
	}

	/// <summary>Captures a module state signal for dirty-flag processing.</summary>
	public void OnModuleStateChanged(int slot, string state)
	{
		OnHudSignal(
			ModuleLightsSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["slot"] = slot.ToString(),
				["state"] = state,
			});
	}

	/// <summary>Captures a currency balance signal for dirty-flag processing.</summary>
	public void OnCurrencyChanged(int newBalance)
	{
		OnHudSignal(
			CurrencyDisplaySignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["balance"] = newBalance.ToString(),
			});
	}

	/// <summary>捕获货舱容量信号，并记录货舱模块是否已安装。</summary>
	public void OnCargoChanged(int current, int max, bool hasModule)
	{
		cargoModuleInstalled = hasModule;
		OnHudSignal(
			CargoBarSignalId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["current"] = current.ToString(),
				["max"] = max.ToString(),
				["has_module"] = hasModule.ToString(),
			});
	}

	/// <summary>记录最近一次桌面 `_process` delta，用于恢复通知中的异常冻结判断。</summary>
	public void RecordProcessDelta(double deltaSeconds)
	{
		lastProcessDeltaSeconds = Math.Max(0, deltaSeconds);
	}

	/// <summary>处理桌面窗口恢复通知，必要时绕过脏标记执行全量 UI 刷新。</summary>
	public DesktopRecoverySnapshot OnApplicationResumed()
	{
		var requested = false;
		if (lastProcessDeltaSeconds > DesktopFullRefreshDeltaThresholdSeconds)
		{
			RequestFullUiRefresh();
			requested = true;
		}

		return new DesktopRecoverySnapshot(
			requested,
			fullUiRefreshRequestCount,
			lastProcessDeltaSeconds,
			lastFullRefreshQueryNames,
			visiblePanels.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
			KeyboardFocusElementId,
			IsMovementInputBlocked());
	}

	/// <summary>绕过脏标记，从当前可见 HUD 所需的领域查询强制重建 UI 快照。</summary>
	public HudProcessResult RequestFullUiRefresh()
	{
		fullUiRefreshRequestCount++;
		dirtyFlags.Clear();
		pendingPayloads.Clear();

		var queryNames = new List<string>();
		var updatedElements = 0;
		if (HubHudVisible)
		{
			updatedElements += RefreshHubHudFromDomain(queryNames);
		}

		if (ExplorationHudVisible)
		{
			updatedElements += RefreshExplorationHudFromDomain(queryNames);
		}

		if (ChartVisible)
		{
			BindChartPanel(new Dictionary<string, string>(StringComparer.Ordinal), queryNames);
		}

		lastFullRefreshQueryNames = queryNames.ToArray();
		return new HudProcessResult(
			HudProcessPriority,
			updatedElements,
			DirtyFlagsCleared: true,
			IteratedDirtyElements: updatedElements > 0,
			ImmediateReturn: updatedElements == 0);
	}

	/// <summary>Preloads panel data for a non-modal anchor when the player enters 1.5x radius.</summary>
	public ScreenResult ProximityEnter(string anchorId, double distanceInAnchorRadii)
	{
		var panelId = PanelIdForAnchor(anchorId);
		if (string.IsNullOrEmpty(panelId) || distanceInAnchorRadii > PanelPreloadRadiusMultiplier)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		PreloadPanelData(panelId, distanceDriven: true);
		return ScreenResult.Success;
	}

	/// <summary>Auto-closes an active non-modal panel when the player exits 2x radius.</summary>
	public ScreenResult ProximityExit(string anchorId, double distanceInAnchorRadii)
	{
		var panelId = PanelIdForAnchor(anchorId);
		if (string.IsNullOrEmpty(panelId) || distanceInAnchorRadii < PanelAutoCloseRadiusMultiplier)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		return CloseNonModal(panelId);
	}

	/// <summary>Handles Use on a known interaction anchor and opens the mapped panel.</summary>
	public ScreenResult UsePanelAnchor(string anchorId)
	{
		if (DepartureLocked)
		{
			return ScreenResult.ErrDepartureLocked;
		}

		if (anchorId == StorageAnchorId && !cargoModuleInstalled)
		{
			LastToastMessage = CargoModuleRequiredToast;
			return ScreenResult.ErrInvalidScreen;
		}

		return anchorId switch
		{
			PartnerSniffAnchorId => OpenNonModal(PartnerSniffScreenId),
			StorageAnchorId => OpenNonModal(StorageScreenId),
			IntelStationAnchorId => OpenStationDetailPanel("station.intel", "Intel Table"),
			StorageStationAnchorId => OpenStationDetailPanel("station.storage", "Storage Hold"),
			_ => ScreenResult.ErrInvalidScreen,
		};
	}

	/// <summary>Opens the event-driven S8 repair panel for a domain repair node.</summary>
	public ModalResult OpenRepairPanelForNode(string nodeId)
	{
		var result = OpenModalPanel(
			RepairScreenId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["node_id"] = nodeId,
			});

		if (result == ModalResult.Success)
		{
			var lifecycle = GetOrCreatePanelLifecycle(RepairScreenId);
			lifecycle.State = PanelLifecycleState.Active;
			lifecycle.DomainEventDriven = true;
			lifecycle.DistanceDriven = false;
			lifecycle.OpenAnimationSeconds = 0;
		}

		return result;
	}

	/// <summary>Returns a deterministic lifecycle snapshot for a panel.</summary>
	public PanelLifecycleSnapshot GetPanelLifecycleSnapshot(string panelId)
	{
		return panelLifecycleStates.TryGetValue(panelId, out var lifecycle)
			? lifecycle.ToSnapshot()
			: new PanelLifecycleSnapshot(
				panelId,
				PanelLifecycleState.Unloaded,
				IsPanelPreloaded(panelId),
				PreloadNonBlocking: false,
				DistanceDriven: false,
				DomainEventDriven: false,
				OpenAnimationSeconds: 0,
				CloseAnimationSeconds: 0,
				InstantiationDelayMilliseconds: 0);
	}

	/// <summary>Returns whether a panel has a preloaded scene/data marker.</summary>
	public bool IsPanelPreloaded(string panelId)
	{
		return preloadedPanels.Contains(panelId);
	}

	/// <summary>Adds or refreshes a panel instance in the LRU cache pool.</summary>
	public void CachePanelInstance(string panelId)
	{
		panelCacheLru.Remove(panelId);

		if (panelCacheLru.Count >= PanelCacheMax)
		{
			var evicted = panelCacheLru[0];
			panelCacheLru.RemoveAt(0);
			freedPanelInstances.Add(evicted);
		}

		panelCacheLru.Add(panelId);
	}

	/// <summary>Returns the current panel cache LRU and freed-instance log.</summary>
	public PanelCacheSnapshot GetPanelCacheSnapshot()
	{
		return new PanelCacheSnapshot(panelCacheLru.ToArray(), freedPanelInstances.ToArray());
	}

	/// <summary>Opens the generic S2 station detail panel with station-specific data.</summary>
	public ScreenResult OpenStationDetailPanel(string stationId, string displayName)
	{
		var result = OpenNonModal(StationDetailScreenId);
		if (result != ScreenResult.Success)
		{
			return result;
		}

		lastStationDetailSnapshot = new StationDetailSnapshot(
			StationDetailTemplateId,
			stationId,
			displayName);
		return ScreenResult.Success;
	}

	/// <summary>Returns the most recent StationDetailPanel binding snapshot.</summary>
	public StationDetailSnapshot? GetStationDetailSnapshot()
	{
		return lastStationDetailSnapshot;
	}

	/// <summary>按面板 ID 从上游领域系统绑定一次打开时快照。</summary>
	public PanelBindingSnapshot BindScreenData(
		string panelId,
		IReadOnlyDictionary<string, string>? context = null)
	{
		var normalizedContext = context is null
			? new Dictionary<string, string>(StringComparer.Ordinal)
			: new Dictionary<string, string>(context, StringComparer.Ordinal);
		var fields = new Dictionary<string, string>(StringComparer.Ordinal);
		var queryNames = new List<string>();
		var renderedItemIds = new List<string>();
		var emptyStateMessage = string.Empty;
		var usedDisplayNameFallback = false;

		switch (panelId)
		{
			case HubHudScreenId:
				BindHubHud(fields, queryNames);
				break;
			case ChartScreenId:
				emptyStateMessage = BindChartPanel(fields, queryNames);
				break;
			case ExplorationHudScreenId:
				BindExplorationHud(fields, queryNames);
				break;
			case ExtractionProgressScreenId:
				BindExtractionPanel(fields, queryNames);
				break;
			case CombatScreenId:
				MergeFields(fields, SafeQuery(queryNames, "build_threat_context", () => upstreamDataSource.BuildThreatContext()));
				break;
			case RepairScreenId:
				MergeFields(fields, SafeQuery(
					queryNames,
					"get_repair_state",
					() => upstreamDataSource.GetRepairState(ReadString(normalizedContext, "node_id"))));
				break;
			case MarketScreenId:
				MergeFields(fields, SafeQuery(
					queryNames,
					"get_stall_data",
					() => upstreamDataSource.GetStallData(ReadString(normalizedContext, "stall_id"))));
				break;
			case NamingScreenId:
				BindNamingPanel(fields, queryNames);
				break;
			case PartnerSniffScreenId:
				emptyStateMessage = BindPartnerSniffPanel(fields, queryNames, renderedItemIds, ref usedDisplayNameFallback);
				break;
			case StorageScreenId:
				emptyStateMessage = BindStoragePanel(fields, queryNames);
				break;
		}

		lastPanelBindingSnapshot = new PanelBindingSnapshot(
			panelId,
			fields,
			queryNames,
			renderedItemIds,
			emptyStateMessage,
			!string.IsNullOrEmpty(emptyStateMessage),
			usedDisplayNameFallback,
			++bindSequence);

		return lastPanelBindingSnapshot;
	}

	/// <summary>通过 ResourcesManager 转移容量取舍面板中的物品。</summary>
	public UiDomainCommandSnapshot ConfirmCapacityTransfer(
		string itemId,
		string fromPool,
		string toPool,
		int quantity)
	{
		var args = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["item_id"] = itemId,
			["from_pool"] = fromPool,
			["to_pool"] = toPool,
			["quantity"] = quantity.ToString(),
		};
		var success = SafeCommand(() => upstreamDataSource.TransferItem(itemId, fromPool, toPool, quantity));
		lastDomainCommandSnapshot = new UiDomainCommandSnapshot("transfer_item", args, success);
		if (success)
		{
			CommitUiAction();
			EmitUiItemTransferred(itemId, fromPool, toPool, quantity);
		}

		return lastDomainCommandSnapshot;
	}

	/// <summary>通过 ResourcesManager 丢弃容量取舍面板中的物品。</summary>
	public UiDomainCommandSnapshot ConfirmCapacityDiscard(string itemId)
	{
		var args = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["item_id"] = itemId,
		};
		var success = SafeCommand(() => upstreamDataSource.DiscardItem(itemId));
		lastDomainCommandSnapshot = new UiDomainCommandSnapshot("discard_item", args, success);
		return lastDomainCommandSnapshot;
	}

	/// <summary>通过 WorldRepair 提交修复面板材料。</summary>
	public UiDomainCommandSnapshot SubmitRepairMaterials(
		string nodeId,
		IReadOnlyDictionary<string, int> materials)
	{
		var args = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["node_id"] = nodeId,
			["materials"] = string.Join(",", materials.Select(item => $"{item.Key}:{item.Value}")),
		};
		var success = SafeCommand(() => upstreamDataSource.SubmitRepair(nodeId, materials));
		lastDomainCommandSnapshot = new UiDomainCommandSnapshot("submit_repair", args, success);
		if (success)
		{
			CommitUiAction();
			EmitUiRepairSubmitted(nodeId, ToMaterialSubmissions(materials));
			RecordRepairSubmittedToastAnimation();
		}

		return lastDomainCommandSnapshot;
	}

	/// <summary>通过 SettlementManager 确认购买并在成功后发射语义事件。</summary>
	public UiDomainCommandSnapshot ConfirmPurchase(
		string stallId,
		string goodId,
		int quantity,
		int totalCost)
	{
		var args = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["stall_id"] = stallId,
			["good_id"] = goodId,
			["quantity"] = quantity.ToString(),
			["total_cost"] = totalCost.ToString(),
		};
		var success = SafeCommand(() => upstreamDataSource.ExecutePurchase(stallId, goodId, quantity, totalCost));
		lastDomainCommandSnapshot = new UiDomainCommandSnapshot("execute_purchase", args, success);
		if (success)
		{
			CommitUiAction();
			EmitUiPurchaseConfirmed(stallId, goodId, quantity, totalCost);
		}

		return lastDomainCommandSnapshot;
	}

	/// <summary>通过 PartnerManager 提交伙伴命名并在成功后发射语义事件。</summary>
	public UiDomainCommandSnapshot SubmitPartnerName(string partnerId, string name)
	{
		var args = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["partner_id"] = partnerId,
			["name"] = name,
		};
		var success = SafeCommand(() => upstreamDataSource.SubmitPartnerName(partnerId, name));
		lastDomainCommandSnapshot = new UiDomainCommandSnapshot("submit_partner_name", args, success);
		if (success)
		{
			CommitUiAction();
			EmitUiNamingConfirmed(partnerId, name);
		}

		return lastDomainCommandSnapshot;
	}

	/// <summary>关闭结算摘要并发射结算关闭语义事件。</summary>
	public ScreenResult CloseSettlementSummary(
		string voyageId,
		IReadOnlyList<string> itemsBrought,
		IReadOnlyList<string> intelGained)
	{
		if (modalPanel != SettlementSummaryScreenId)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		CloseModal();
		CommitUiAction();
		EmitUiSettlementClosed(voyageId, itemsBrought, intelGained);
		return ScreenResult.Success;
	}

	/// <summary>完成墨水扩散后记录出发口封闭线性动画。</summary>
	public ScreenResult CompleteInkSpread()
	{
		if (CurrentScreen != Screen.ChartDepartureConfirmed)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		DepartureGateLocked = true;
		RecordDepartureGateSealAnimation();
		return ScreenResult.Success;
	}

	/// <summary>威胁打断撤离读条，终止读条 Tween 并执行红色闪烁收尾。</summary>
	public ScreenResult ThreatTriggeredDuringExtraction(string threatId)
	{
		if (CurrentScreen != Screen.Extracting || !visiblePanels.Contains(ExtractionProgressScreenId))
		{
			return ScreenResult.ErrInvalidScreen;
		}

		KillActiveAnimationForTarget(ExtractionProgressScreenId, applyPanelFinalState: false);
		lastAnimationInterruptionSnapshot = new AnimationInterruptionSnapshot(
			ExtractionProgressAnimationId,
			ExistingTweenKilled: true,
			FinalStateApplied: true,
			FinalOpacity: 0,
			FinalVisible: false,
			FinalScale: 1.0,
			ThreatInterruptionFlashCount,
			ThreatInterruptionFlashSeconds,
			DangerRedHex);
		RecordAnimation(new UiAnimationSnapshot(
			ExtractionThreatInterruptedAnimationId,
			ExtractionProgressScreenId,
			ThreatInterruptionFlashCount * ThreatInterruptionFlashSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.Linear,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: true,
			IsKilled: false,
			FinalStateApplied: true,
			new[]
			{
				new AnimationPropertyTween("progress_bar_color", "normal", DangerRedHex, ThreatInterruptionFlashSeconds, UiAnimationEasing.Linear),
			}));
		visiblePanels.Remove(ExtractionProgressScreenId);
		MarkPanelClosed(ExtractionProgressScreenId, ThreatInterruptionFlashCount * ThreatInterruptionFlashSeconds);
		TransitionTo(Screen.Exploration, validate: false);
		OpenModalPanel(
			CombatScreenId,
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["threat_id"] = threatId,
			});
		return ScreenResult.Success;
	}

	/// <summary>按威胁 ID 和结果 ID 处理战斗响应并发射语义事件。</summary>
	public ModalResult ResolveThreatResponse(string threatId, string resultId)
	{
		var resolution = resultId switch
		{
			"suppressed" => CombatThreatResolution.EmergencyTreatment,
			"retreated" => CombatThreatResolution.Retreat,
			_ => CombatThreatResolution.HoldGround,
		};
		var result = ResolveCombatThreat(resolution);
		if (result == ModalResult.Success)
		{
			CommitUiAction();
			EmitUiThreatResponseChosen(threatId, resultId);
		}

		return result;
	}

	/// <summary>返回指定动画 ID 的合同快照。</summary>
	public UiAnimationSnapshot? GetAnimationSnapshot(string animationId)
	{
		return animationSnapshots.TryGetValue(animationId, out var snapshot) ? snapshot : null;
	}

	/// <summary>返回指定面板的羊皮纸纹理合同。</summary>
	public PanelTextureContractSnapshot GetPanelTextureContract(string panelId)
	{
		return panelTextureContracts.TryGetValue(panelId, out var contract)
			? contract
			: new PanelTextureContractSnapshot(
				panelId,
				UsesNinePatchRect: false,
				SourceTextureWidth: 0,
				SourceTextureHeight: 0,
				UsesFullSizeTexture: true);
	}

	/// <summary>返回指定语义事件声明合同。</summary>
	public UiSemanticEventContract? GetSemanticEventContract(string eventId)
	{
		return semanticEventContracts.TryGetValue(eventId, out var contract) ? contract : null;
	}

	/// <summary>返回最近一次指定语义事件发射快照。</summary>
	public UiSemanticEventSnapshot? GetLatestSemanticEvent(string eventId)
	{
		return semanticEventLog.LastOrDefault(item => string.Equals(item.EventId, eventId, StringComparison.Ordinal));
	}

	/// <summary>记录同步语义事件被下游消费者处理的实际嵌套层级。</summary>
	public IDisposable EnterSemanticEventConsumerScope(string eventId, string consumerId)
	{
		if (string.IsNullOrWhiteSpace(consumerId)
			|| !string.Equals(activeSemanticDispatchEventId, eventId, StringComparison.Ordinal))
		{
			return NoopDisposable.Instance;
		}

		semanticCascadeDepth++;
		semanticCascadeMaxDepth = Math.Max(semanticCascadeMaxDepth, semanticCascadeDepth);
		UpdateLatestSemanticEventCascadeDepth(eventId, semanticCascadeMaxDepth);
		return new SemanticCascadeScope(this, eventId);
	}

	/// <summary>将控件标记为灰显但仍可通过 Tab 聚焦，并记录不可用原因。</summary>
	public void SetElementDisabled(string elementId, string tooltipText)
	{
		disabledElementTooltips[elementId] = string.IsNullOrWhiteSpace(tooltipText)
			? DisabledControlDefaultTooltip
			: tooltipText;
	}

	/// <summary>模拟当前键盘焦点上的 Enter，灰显控件只显示提示且不执行动作。</summary>
	public FocusActivationResult PressEnterOnFocusedElement()
	{
		if (string.IsNullOrWhiteSpace(KeyboardFocusElementId))
		{
			return FocusActivationResult.NoFocusableElement;
		}

		if (disabledElementTooltips.TryGetValue(KeyboardFocusElementId, out var tooltip))
		{
			LastTooltipMessage = tooltip;
			return FocusActivationResult.DisabledNoOp;
		}

		LastTooltipMessage = string.Empty;
		return FocusActivationResult.Activated;
	}

	/// <summary>模拟当前焦点控件因场景切换被销毁。</summary>
	public void DestroyFocusableElement(string elementId)
	{
		destroyedFocusableElements.Add(elementId);
	}

	/// <summary>注册可被新手引导定位的锚点元数据。</summary>
	public AnchorMetadataSnapshot RegisterHighlightableAnchor(string anchorId, string panelId, int priority)
	{
		var snapshot = new AnchorMetadataSnapshot(
			anchorId,
			panelId,
			Highlightable: true,
			priority,
			HighlightRequestAccepted: false);
		anchorMetadata[anchorId] = snapshot;
		return snapshot;
	}

	/// <summary>返回指定锚点的新手引导高亮元数据。</summary>
	public AnchorMetadataSnapshot GetAnchorMetadata(string anchorId)
	{
		return anchorMetadata.TryGetValue(anchorId, out var snapshot)
			? snapshot
			: new AnchorMetadataSnapshot(anchorId, PanelIdForAnchor(anchorId), Highlightable: false, HighlightPriority: 0, HighlightRequestAccepted: false);
	}

	/// <summary>请求新手引导高亮；出航锁定期间静默拒绝。</summary>
	public AnchorMetadataSnapshot RequestOnboardingHighlight(string anchorId)
	{
		lastHighlightRequestRejectedSilently = false;
		var snapshot = GetAnchorMetadata(anchorId);
		if (DepartureLocked)
		{
			lastHighlightRequestRejectedSilently = true;
			return snapshot with { HighlightRequestAccepted = false };
		}

		var accepted = snapshot.Highlightable;
		var updated = snapshot with { HighlightRequestAccepted = accepted };
		anchorMetadata[anchorId] = updated;
		return updated;
	}

	/// <summary>Renders a focus-safe onboarding hint snapshot without changing focus or input ownership.</summary>
	public OnboardingHintRenderSnapshot RenderOnboardingHint(
		OnboardingHintRequest request,
		OnboardingSurface activeSurface,
		IReadOnlyCollection<string>? suppressedStepIds = null,
		IReadOnlySet<string>? unsafeAnchorIds = null)
	{
		ArgumentNullException.ThrowIfNull(request);
		onboardingHintSnapshots.Clear();

		if (string.IsNullOrWhiteSpace(request.HintTextKey))
		{
			var skipped = BuildOnboardingHintSnapshot(
				request,
				activeSurface,
				targetSurfaceId: "onboarding.safe_text",
				visible: false,
				skipped: true,
				fallbackReason: "missing_hint_text",
				textOnlyFallback: false);
			onboardingHintSnapshots.Add(skipped);
			return skipped;
		}

		if (suppressedStepIds is not null && suppressedStepIds.Contains(request.StepId))
		{
			var skipped = BuildOnboardingHintSnapshot(
				request,
				activeSurface,
				targetSurfaceId: SurfaceIdForOnboarding(activeSurface),
				visible: false,
				skipped: true,
				fallbackReason: "step_suppressed",
				textOnlyFallback: false);
			onboardingHintSnapshots.Add(skipped);
			return skipped;
		}

		if (activeSurface is OnboardingSurface.Chart or OnboardingSurface.Exploration
			&& IsHubAnchor(request.HighlightAnchorId))
		{
			var skipped = BuildOnboardingHintSnapshot(
				request,
				activeSurface,
				targetSurfaceId: SurfaceIdForOnboarding(activeSurface),
				visible: false,
				skipped: true,
				fallbackReason: "inactive_hub_anchor",
				textOnlyFallback: false);
			onboardingHintSnapshots.Add(skipped);
			return skipped;
		}

		var anchorUnsafe = request.HighlightAnchorId is null
			|| string.IsNullOrWhiteSpace(request.HighlightAnchorId)
			|| (unsafeAnchorIds?.Contains(request.HighlightAnchorId) ?? false);
		var targetSurfaceId = anchorUnsafe ? "onboarding.safe_text" : SurfaceIdForOnboarding(activeSurface);
		var rendered = BuildOnboardingHintSnapshot(
			request,
			activeSurface,
			targetSurfaceId,
			visible: true,
			skipped: false,
			fallbackReason: anchorUnsafe ? "text_only_fallback" : null,
			textOnlyFallback: anchorUnsafe);
		onboardingHintSnapshots.Add(rendered);
		return rendered;
	}

	/// <summary>Clears visible onboarding hint snapshots without touching UI focus.</summary>
	public void ClearOnboardingHints()
	{
		onboardingHintSnapshots.Clear();
	}

	/// <summary>返回船体波段的颜色、形状、文字和分段数三重编码。</summary>
	public A11yEncodingSnapshot GetHullBandEncoding(string band)
	{
		var normalized = (band ?? string.Empty).ToUpperInvariant();
		return normalized switch
		{
			"GREEN" => new A11yEncodingSnapshot("hull_band_green", SafeGreenHex, "shape.check", "3段", "edge.solid", 3, 16),
			"YELLOW" => new A11yEncodingSnapshot("hull_band_yellow", WarningAmberHex, "shape.bolt", "2段", "edge.dashed", 2, 16),
			"RED" => new A11yEncodingSnapshot("hull_band_red", DangerRedHex, "shape.circle", "1段", "edge.double", 1, 16),
			_ => new A11yEncodingSnapshot("hull_band_unknown", DisabledGrayHex, "shape.unknown", "未知", "edge.dotted", 0, 16),
		};
	}

	/// <summary>返回修复材料满足/不足状态的无障碍编码。</summary>
	public A11yEncodingSnapshot GetMaterialRequirementEncoding(bool satisfied)
	{
		return satisfied
			? new A11yEncodingSnapshot("material_satisfied", SafeGreenHex, "shape.check", "满足", "edge.solid", 0, 16)
			: new A11yEncodingSnapshot("material_missing", DangerRedHex, "shape.cross", "不足", "edge.double", 0, 16);
	}

	/// <summary>审计所有内置小尺寸状态指示是否具备颜色、形状、文字和边缘特征。</summary>
	public IReadOnlyList<A11yEncodingSnapshot> AuditSmallStatusEncodings()
	{
		return new[]
		{
			GetHullBandEncoding("GREEN"),
			GetHullBandEncoding("YELLOW"),
			GetHullBandEncoding("RED"),
			GetMaterialRequirementEncoding(satisfied: true),
			GetMaterialRequirementEncoding(satisfied: false),
		};
	}

	/// <summary>返回小尺寸颜色编码元素是否满足三重编码规则。</summary>
	public bool IsSmallStatusEncodingCompliant(A11yEncodingSnapshot snapshot)
	{
		if (snapshot.ElementSizePx >= 24)
		{
			return true;
		}

		return !string.IsNullOrWhiteSpace(snapshot.ColorHex)
			&& !string.IsNullOrWhiteSpace(snapshot.ShapeToken)
			&& !string.IsNullOrWhiteSpace(snapshot.TextLabel)
			&& !string.IsNullOrWhiteSpace(snapshot.EdgeToken);
	}

	/// <summary>按 WCAG 2.x 相对亮度公式计算两个颜色的对比度并给出替代文本色建议。</summary>
	public ColorContrastSnapshot AuditTextContrast(string foregroundHex, string backgroundHex, double requiredRatio)
	{
		var ratio = CalculateContrastRatio(foregroundHex, backgroundHex);
		var recommended = foregroundHex.Equals(DangerRedHex, StringComparison.OrdinalIgnoreCase)
			? AccessibleDangerTextHex
			: foregroundHex.Equals("#4FB7B2", StringComparison.OrdinalIgnoreCase) ? AccessibleBeaconTextHex : foregroundHex;
		return new ColorContrastSnapshot(
			foregroundHex,
			backgroundHex,
			ratio,
			requiredRatio,
			ratio + 0.0001 >= requiredRatio,
			recommended);
	}

	/// <summary>返回满足目标对比度的实际文本前景色。</summary>
	public string ResolveAccessibleTextForeground(string semanticForegroundHex, string backgroundHex, double requiredRatio)
	{
		var audit = AuditTextContrast(semanticForegroundHex, backgroundHex, requiredRatio);
		return audit.Passes ? semanticForegroundHex : audit.RecommendedForegroundHex;
	}

	/// <summary>Opens a non-modal panel without blocking movement input.</summary>
	public ScreenResult OpenNonModal(string panelId)
	{
		if (DepartureLocked)
		{
			return ScreenResult.ErrDepartureLocked;
		}

		if (!screenRegistry.TryGetValue(panelId, out var definition) || definition.Type != ScreenType.NonModal)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		if (!nonModalPanels.Contains(panelId, StringComparer.Ordinal))
		{
			nonModalPanels.Add(panelId);
			panelZIndices[panelId] = nextPanelZIndex++;
		}

		visiblePanels.Add(panelId);
		MarkNonModalPanelActive(panelId);
		RecordPanelOpenAnimation(panelId);
		BindScreenData(panelId);
		UpdateActiveInputLayer();
		EmitUiPanelOpened(panelId);
		return ScreenResult.Success;
	}

	/// <summary>Closes a non-modal panel if it is currently visible.</summary>
	public ScreenResult CloseNonModal(string panelId)
	{
		if (!nonModalPanels.Remove(panelId))
		{
			return ScreenResult.ErrInvalidScreen;
		}

		visiblePanels.Remove(panelId);
		panelZIndices.Remove(panelId);
		RecordPanelCloseAnimation(panelId);
		MarkPanelClosed(panelId, PanelCloseAnimationSeconds);
		EmitUiPanelClosed(panelId);
		UpdateActiveInputLayer();
		return ScreenResult.Success;
	}

	/// <summary>Closes the most recently opened non-modal panel.</summary>
	public ScreenResult CloseTopNonModal()
	{
		if (nonModalPanels.Count == 0)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		var topPanel = nonModalPanels[^1];
		return CloseNonModal(topPanel);
	}

	/// <summary>Returns true when movement keys should be blocked by active UI.</summary>
	public bool IsMovementInputBlocked()
	{
		var layer = GetActiveInputLayer();
		return layer is InputLayer.Modal or InputLayer.SemiModal;
	}

	/// <summary>Returns true when world interaction Use should be blocked by active UI.</summary>
	public bool IsWorldUseInputBlocked()
	{
		var layer = GetActiveInputLayer();
		return layer is InputLayer.Modal or InputLayer.SemiModal;
	}

	/// <summary>Returns the active routing layer, optionally evaluating a pointer over HUD chrome.</summary>
	public InputLayer GetActiveInputLayer(bool pointerOverHud = false)
	{
		if (IsModalOpen())
		{
			return InputLayer.Modal;
		}

		if (visiblePanels.Contains(ExtractionProgressScreenId))
		{
			return InputLayer.SemiModal;
		}

		if (nonModalPanels.Count > 0)
		{
			return InputLayer.NonModal;
		}

		if (pointerOverHud && IsHudOverlayVisible())
		{
			return InputLayer.Hud;
		}

		return InputLayer.World;
	}

	/// <summary>Returns the HUD mouse filter for a pointer region.</summary>
	public MouseFilterMode GetHudMouseFilter(HudRegion region)
	{
		return region == HudRegion.InventorySlot ? MouseFilterMode.Stop : MouseFilterMode.Ignore;
	}

	/// <summary>Returns whether a HUD click reaches the world layer.</summary>
	public bool DoesHudClickReachWorld(HudRegion region)
	{
		return GetHudMouseFilter(region) == MouseFilterMode.Ignore;
	}

	/// <summary>Cycles focus inside the active modal and consumes Tab input.</summary>
	public bool PressTab(bool shift = false)
	{
		if (!IsModalOpen())
		{
			return false;
		}

		var focusChain = ActiveFocusChainForPanel(modalPanel);
		if (focusChain.Length == 0)
		{
			KeyboardFocusElementId = PanelContainerFocusId(modalPanel);
			return true;
		}

		var currentIndex = Array.IndexOf(focusChain, KeyboardFocusElementId);
		if (currentIndex < 0)
		{
			KeyboardFocusElementId = focusChain[0];
			return true;
		}

		var nextIndex = shift
			? (currentIndex - 1 + focusChain.Length) % focusChain.Length
			: (currentIndex + 1) % focusChain.Length;
		KeyboardFocusElementId = focusChain[nextIndex];
		return true;
	}

	/// <summary>Synchronizes keyboard focus to a mouse-pressed interactable element.</summary>
	public bool MousePressInteractable(string elementId)
	{
		if (!IsElementFocusable(elementId) || !IsElementInActiveFocusScope(elementId))
		{
			return false;
		}

		KeyboardFocusElementId = elementId;
		return true;
	}

	/// <summary>Records the current mouse hover target without changing keyboard focus.</summary>
	public void MouseHoverElement(string elementId)
	{
		MouseHoverElementId = elementId ?? string.Empty;
	}

	/// <summary>Returns whether an element participates in keyboard focus traversal.</summary>
	public bool IsElementFocusable(string elementId)
	{
		return AllFocusableElements.Contains(elementId)
			|| elementId.StartsWith("panel:", StringComparison.Ordinal);
	}

	/// <summary>Returns visual focus and hover state tokens for an element.</summary>
	public ElementVisualState GetElementVisualState(string elementId)
	{
		var focused = string.Equals(KeyboardFocusElementId, elementId, StringComparison.Ordinal);
		var hovered = string.Equals(MouseHoverElementId, elementId, StringComparison.Ordinal);
		return new ElementVisualState(
			focused,
			hovered,
			focused ? FocusStyleToken : string.Empty,
			hovered ? HoverStyleToken : string.Empty);
	}

	/// <summary>Returns a snapshot for a visible modal or panel.</summary>
	public ModalPanelSnapshot? GetPanelSnapshot(string panelId)
	{
		return panelStates.TryGetValue(panelId, out var state) ? state.ToSnapshot() : null;
	}

	/// <summary>Returns true when a screen or panel is currently visible.</summary>
	public bool IsPanelVisible(string panelId)
	{
		return visiblePanels.Contains(panelId);
	}

	/// <summary>Returns the z-index assigned to an open panel, or -1 when the panel is not layered.</summary>
	public int GetPanelZIndex(string panelId)
	{
		return panelZIndices.TryGetValue(panelId, out var zIndex) ? zIndex : -1;
	}

	/// <summary>Closes all currently open panels and clears queued modal state.</summary>
	public void ForceCloseAllPanels()
	{
		var closedPanels = visiblePanels.ToArray();
		focusRestoreStack.Clear();
		queuedModals.Clear();
		panelStates.Clear();
		nonModalPanels.Clear();
		panelZIndices.Clear();
		combatOverridePanelId = string.Empty;
		modalPanel = string.Empty;
		routeSidePanelExpanded = false;
		DepartureConfirmButtonFocused = false;
		KeyboardFocusElementId = string.Empty;
		MouseHoverElementId = string.Empty;

		foreach (var panelId in closedPanels)
		{
			visiblePanels.Remove(panelId);
			MarkPanelClosed(panelId, closeAnimationSeconds: 0);
			KillActiveAnimationForTarget(panelId, applyPanelFinalState: true);
			EmitUiPanelClosed(panelId);
		}

		ActiveInputLayer = InputLayer.World;
	}

	/// <summary>
	/// Opens the registry diagnostic developer tools when debug-build gating allows it.
	/// Returns null in release builds or when another non-combat modal already owns input.
	/// </summary>
	public RegistryDiagnosticDevTools? OpenRegistryDiagnosticTools(
		Registry registry,
		IEnumerable<RegistryDiagnosticEvent> diagnostics,
		bool? isDebugBuild = null)
	{
		var tools = RegistryDiagnosticDevTools.TryOpen(registry, diagnostics, isDebugBuild);
		if (tools is null)
		{
			return null;
		}

		return OpenModal("registry_diagnostic_tools") ? tools : null;
	}

	private static Dictionary<string, UiSemanticEventContract> BuildSemanticEventContracts()
	{
		return new Dictionary<string, UiSemanticEventContract>(StringComparer.Ordinal)
		{
			[UIRouteSelectedEventId] = Contract(UIRouteSelectedEventId, "string", "string"),
			[UIDepartureConfirmedEventId] = Contract(UIDepartureConfirmedEventId, "string", "string"),
			[UIThreatResponseChosenEventId] = Contract(UIThreatResponseChosenEventId, "string", "string"),
			[UIRepairSubmittedEventId] = Contract(UIRepairSubmittedEventId, "string", "IReadOnlyList<MaterialSubmission>"),
			[UIPurchaseConfirmedEventId] = Contract(UIPurchaseConfirmedEventId, "string", "string", "int", "int"),
			[UIItemTransferredEventId] = Contract(UIItemTransferredEventId, "string", "string", "string", "int"),
			[UINamingConfirmedEventId] = Contract(UINamingConfirmedEventId, "string", "string"),
			[UISettlementClosedEventId] = Contract(UISettlementClosedEventId, "string", "IReadOnlyList<string>", "IReadOnlyList<string>"),
			[UIPanelOpenedEventId] = Contract(UIPanelOpenedEventId, "string"),
			[UIPanelClosedEventId] = Contract(UIPanelClosedEventId, "string"),
		};
	}

	private static UiSemanticEventContract Contract(string eventId, params string[] parameterTypes)
	{
		return new UiSemanticEventContract(
			eventId,
			parameterTypes,
			UsesDictionaryPayload: false,
			EmitsSynchronously: true,
			MaxCascadeDepth: MaxSemanticCascadeDepth);
	}

	private void RecordPanelOpenAnimation(string panelId)
	{
		RecordAnimation(new UiAnimationSnapshot(
			$"{PanelOpenAnimationIdPrefix}{panelId}",
			panelId,
			PanelOpenAnimationSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.EaseOut,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: false,
			IsKilled: false,
			FinalStateApplied: false,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("scale", "0.9", "1.0", PanelOpenAnimationSeconds, UiAnimationEasing.EaseOut),
				new AnimationPropertyTween("modulate.a", "0", "1", PanelOpenAnimationSeconds, UiAnimationEasing.EaseOut),
			}));
	}

	private void RecordPanelCloseAnimation(string panelId)
	{
		RecordAnimation(new UiAnimationSnapshot(
			$"{PanelCloseAnimationIdPrefix}{panelId}",
			panelId,
			PanelCloseAnimationSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.EaseIn,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: false,
			IsKilled: false,
			FinalStateApplied: true,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("scale", "1.0", "0.9", PanelCloseAnimationSeconds, UiAnimationEasing.EaseIn),
				new AnimationPropertyTween("modulate.a", "1", "0", PanelCloseAnimationSeconds, UiAnimationEasing.EaseIn),
			}));
	}

	private void RecordRoutePulseAnimation(string routeId)
	{
		RecordAnimation(new UiAnimationSnapshot(
			RoutePulseAnimationId,
			routeId,
			RoutePulseAnimationSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.EaseInOut,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: false,
			IsKilled: false,
			FinalStateApplied: true,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("outline_width", "1px", "3px", RoutePulseAnimationSeconds / 2, UiAnimationEasing.EaseInOut),
				new AnimationPropertyTween("outline_width", "3px", "1px", RoutePulseAnimationSeconds / 2, UiAnimationEasing.EaseInOut),
			}));
	}

	private void RecordInkSpreadAnimation()
	{
		RecordAnimation(new UiAnimationSnapshot(
			InkSpreadAnimationId,
			ChartScreenId,
			InkSpreadAnimationSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.EaseOut,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: true,
			ShaderUniformName: InkProgressShaderUniform,
			ShaderUniformWritesPerFrame: 1,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: false,
			IsKilled: false,
			FinalStateApplied: true,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("shader.progress", "0", "1", InkSpreadAnimationSeconds, UiAnimationEasing.EaseOut),
			}));
	}

	private void RecordDepartureGateSealAnimation()
	{
		RecordAnimation(new UiAnimationSnapshot(
			DepartureGateSealAnimationId,
			ChartScreenId,
			DepartureGateSealAnimationSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.Linear,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: false,
			IsKilled: false,
			FinalStateApplied: true,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("departure_gate_seal", "open", "sealed", DepartureGateSealAnimationSeconds, UiAnimationEasing.Linear),
			}));
	}

	private void RecordExtractionProgressAnimation()
	{
		RecordAnimation(new UiAnimationSnapshot(
			ExtractionProgressAnimationId,
			ExtractionProgressScreenId,
			ExtractionProgressAnimationSeconds,
			DwellSeconds: 0,
			Easing: UiAnimationEasing.Linear,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: true,
			AutoRemovesOnFinished: false,
			IsKilled: false,
			FinalStateApplied: false,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("progress_percent", "0", "100", ExtractionProgressAnimationSeconds, UiAnimationEasing.Linear),
			}));
	}

	private void RecordRepairSubmittedToastAnimation()
	{
		RecordAnimation(new UiAnimationSnapshot(
			RepairToastAnimationId,
			RepairScreenId,
			ToastEnterAnimationSeconds + ToastDwellSeconds,
			ToastDwellSeconds,
			Easing: UiAnimationEasing.EaseOut,
			UsesSceneTreeTween: true,
			UsesManualProcessInterpolation: false,
			UsesShaderMaterial: false,
			ShaderUniformName: string.Empty,
			ShaderUniformWritesPerFrame: 0,
			ProgressTextRealtime: false,
			AutoRemovesOnFinished: true,
			IsKilled: false,
			FinalStateApplied: true,
			PropertyTweens: new[]
			{
				new AnimationPropertyTween("position.y", "below", "rest", ToastEnterAnimationSeconds, UiAnimationEasing.EaseOut),
				new AnimationPropertyTween("modulate.a", "0", "1", ToastEnterAnimationSeconds, UiAnimationEasing.EaseOut),
				new AnimationPropertyTween("modulate.a", "1", "0", PanelCloseAnimationSeconds, UiAnimationEasing.EaseIn),
			}));
	}

	private void RecordModalOpenAnimation(string panelId)
	{
		if (panelId == SettlementSummaryScreenId)
		{
			RecordAnimation(new UiAnimationSnapshot(
				SettlementSummaryEnterAnimationId,
				panelId,
				SettlementSummaryEnterAnimationSeconds,
				DwellSeconds: 0,
				Easing: UiAnimationEasing.EaseOut,
				UsesSceneTreeTween: true,
				UsesManualProcessInterpolation: false,
				UsesShaderMaterial: false,
				ShaderUniformName: string.Empty,
				ShaderUniformWritesPerFrame: 0,
				ProgressTextRealtime: false,
				AutoRemovesOnFinished: false,
				IsKilled: false,
				FinalStateApplied: true,
				PropertyTweens: new[]
				{
					new AnimationPropertyTween("modulate.a", "0", "1", SettlementSummaryEnterAnimationSeconds, UiAnimationEasing.EaseOut),
					new AnimationPropertyTween("position.y", "below", "rest", SettlementSummaryEnterAnimationSeconds, UiAnimationEasing.EaseOut),
				}));
			return;
		}

		if (panelId == NamingScreenId)
		{
			RecordAnimation(new UiAnimationSnapshot(
				NamingModalPopAnimationId,
				panelId,
				NamingModalPopAnimationSeconds,
				DwellSeconds: 0,
				Easing: UiAnimationEasing.EaseOut,
				UsesSceneTreeTween: true,
				UsesManualProcessInterpolation: false,
				UsesShaderMaterial: false,
				ShaderUniformName: string.Empty,
				ShaderUniformWritesPerFrame: 0,
				ProgressTextRealtime: false,
				AutoRemovesOnFinished: false,
				IsKilled: false,
				FinalStateApplied: true,
				PropertyTweens: new[]
				{
					new AnimationPropertyTween("scale", "0.9", "1.0", NamingModalPopAnimationSeconds, UiAnimationEasing.EaseOut),
					new AnimationPropertyTween("modulate.a", "0", "1", NamingModalPopAnimationSeconds, UiAnimationEasing.EaseOut),
				}));
		}
	}

	private void RecordAnimation(UiAnimationSnapshot snapshot)
	{
		var drivenSnapshot = animationDriver.PlayTween(snapshot);
		animationSnapshots[drivenSnapshot.AnimationId] = drivenSnapshot;
		activeAnimationByTarget[drivenSnapshot.TargetId] = drivenSnapshot.AnimationId;
	}

	private void KillActiveAnimationForTarget(string targetId, bool applyPanelFinalState)
	{
		if (!activeAnimationByTarget.TryGetValue(targetId, out var animationId)
			|| !animationSnapshots.TryGetValue(animationId, out var snapshot))
		{
			return;
		}

		var killed = animationDriver.KillTween(snapshot, applyPanelFinalState);
		animationSnapshots[animationId] = killed;
		activeAnimationByTarget.Remove(targetId);
		if (applyPanelFinalState)
		{
			lastAnimationInterruptionSnapshot = new AnimationInterruptionSnapshot(
				animationId,
				ExistingTweenKilled: true,
				FinalStateApplied: true,
				FinalOpacity: 0,
				FinalVisible: false,
				FinalScale: 0.9,
				FlashCount: 0,
				FlashSecondsPerBlink: 0,
				FlashColorHex: string.Empty);
		}
	}

	private static IReadOnlyList<MaterialSubmission> ToMaterialSubmissions(IReadOnlyDictionary<string, int> materials)
	{
		return materials
			.OrderBy(item => item.Key, StringComparer.Ordinal)
			.Select(item => new MaterialSubmission(item.Key, item.Value))
			.ToArray();
	}

	private void CommitUiAction()
	{
		actionSequence = ++emitSequence;
	}

	private UiSemanticEventSnapshot RecordSemanticEvent(string eventId, params string[] args)
	{
		if (actionSequence == 0)
		{
			CommitUiAction();
		}

		var snapshot = new UiSemanticEventSnapshot(
			eventId,
			args,
			actionSequence,
			++emitSequence,
			EmittedAfterAction: emitSequence > actionSequence,
			EmittedSynchronously: true,
			CascadeDepth: 0);
		semanticEventLog.Add(snapshot);
		return snapshot;
	}

	private void DispatchSemanticEvent(string eventId, Action invokeSubscribers)
	{
		var previousEventId = activeSemanticDispatchEventId;
		var previousCascadeDepth = semanticCascadeDepth;
		var previousCascadeMaxDepth = semanticCascadeMaxDepth;

		activeSemanticDispatchEventId = eventId;
		semanticCascadeDepth = 0;
		semanticCascadeMaxDepth = 0;

		try
		{
			invokeSubscribers();
			UpdateLatestSemanticEventCascadeDepth(eventId, semanticCascadeMaxDepth);
		}
		finally
		{
			activeSemanticDispatchEventId = previousEventId;
			semanticCascadeDepth = previousCascadeDepth;
			semanticCascadeMaxDepth = previousCascadeMaxDepth;
		}
	}

	private void ExitSemanticEventConsumerScope(string eventId)
	{
		if (string.Equals(activeSemanticDispatchEventId, eventId, StringComparison.Ordinal) && semanticCascadeDepth > 0)
		{
			semanticCascadeDepth--;
		}
	}

	private void UpdateLatestSemanticEventCascadeDepth(string eventId, int cascadeDepth)
	{
		for (var i = semanticEventLog.Count - 1; i >= 0; i--)
		{
			var snapshot = semanticEventLog[i];
			if (!string.Equals(snapshot.EventId, eventId, StringComparison.Ordinal))
			{
				continue;
			}

			if (cascadeDepth > snapshot.CascadeDepth)
			{
				semanticEventLog[i] = snapshot with { CascadeDepth = cascadeDepth };
			}

			return;
		}
	}

	private void EmitUiRouteSelected(string routeId, string routeName)
	{
		CommitUiAction();
		RecordSemanticEvent(UIRouteSelectedEventId, routeId, routeName);
		DispatchSemanticEvent(UIRouteSelectedEventId, () => UIRouteSelected?.Invoke(routeId, routeName));
	}

	private void EmitUiDepartureConfirmed(string routeId, string departureMode)
	{
		CommitUiAction();
		RecordSemanticEvent(UIDepartureConfirmedEventId, routeId, departureMode);
		DispatchSemanticEvent(UIDepartureConfirmedEventId, () => UIDepartureConfirmed?.Invoke(routeId, departureMode));
	}

	private void EmitUiThreatResponseChosen(string threatId, string resultId)
	{
		RecordSemanticEvent(UIThreatResponseChosenEventId, threatId, resultId);
		DispatchSemanticEvent(UIThreatResponseChosenEventId, () => UIThreatResponseChosen?.Invoke(threatId, resultId));
	}

	private void EmitUiRepairSubmitted(string nodeId, IReadOnlyList<MaterialSubmission> materials)
	{
		RecordSemanticEvent(
			UIRepairSubmittedEventId,
			nodeId,
			string.Join(",", materials.Select(item => $"{item.MaterialId}:{item.Quantity}")));
		DispatchSemanticEvent(UIRepairSubmittedEventId, () => UIRepairSubmitted?.Invoke(nodeId, materials));
	}

	private void EmitUiPurchaseConfirmed(string stallId, string goodId, int quantity, int totalCost)
	{
		RecordSemanticEvent(UIPurchaseConfirmedEventId, stallId, goodId, quantity.ToString(), totalCost.ToString());
		DispatchSemanticEvent(UIPurchaseConfirmedEventId, () => UIPurchaseConfirmed?.Invoke(stallId, goodId, quantity, totalCost));
	}

	private void EmitUiItemTransferred(string itemId, string fromPool, string toPool, int quantity)
	{
		RecordSemanticEvent(UIItemTransferredEventId, itemId, fromPool, toPool, quantity.ToString());
		DispatchSemanticEvent(UIItemTransferredEventId, () => UIItemTransferred?.Invoke(itemId, fromPool, toPool, quantity));
	}

	private void EmitUiNamingConfirmed(string partnerId, string name)
	{
		RecordSemanticEvent(UINamingConfirmedEventId, partnerId, name);
		DispatchSemanticEvent(UINamingConfirmedEventId, () => UINamingConfirmed?.Invoke(partnerId, name));
	}

	private void EmitUiSettlementClosed(
		string voyageId,
		IReadOnlyList<string> itemsBrought,
		IReadOnlyList<string> intelGained)
	{
		RecordSemanticEvent(
			UISettlementClosedEventId,
			voyageId,
			string.Join(",", itemsBrought),
			string.Join(",", intelGained));
		DispatchSemanticEvent(UISettlementClosedEventId, () => UISettlementClosed?.Invoke(voyageId, itemsBrought, intelGained));
	}

	private void EmitUiPanelOpened(string panelId)
	{
		CommitUiAction();
		RecordSemanticEvent(UIPanelOpenedEventId, panelId);
		DispatchSemanticEvent(UIPanelOpenedEventId, () => UIPanelOpened?.Invoke(panelId));
	}

	private void EmitUiPanelClosed(string panelId)
	{
		CommitUiAction();
		RecordSemanticEvent(UIPanelClosedEventId, panelId);
		DispatchSemanticEvent(UIPanelClosedEventId, () => UIPanelClosed?.Invoke(panelId));
	}

	private int RefreshHubHudFromDomain(List<string> queryNames)
	{
		var updatedElements = 0;
		var hull = SafeQuery(queryNames, "get_hull_integrity", () => upstreamDataSource.GetHullIntegrity());
		if (hull is not null)
		{
			updatedElements += UpdateHullBar(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["new"] = ReadString(hull, "current"),
			});
		}

		var storage = SafeQuery(queryNames, "get_storage_state", () => upstreamDataSource.GetStorageState());
		if (storage is not null)
		{
			updatedElements += UpdateStorageBar(storage);
		}

		var cargo = SafeQuery(queryNames, "get_cargo_state", () => upstreamDataSource.GetCargoState());
		if (cargo is not null)
		{
			updatedElements += UpdateCargoDisplay(cargo);
		}

		var moduleStates = SafeListQuery(queryNames, "get_module_states", () => upstreamDataSource.GetModuleStates());
		if (moduleStates.Count > 0)
		{
			var firstModule = moduleStates[0];
			updatedElements += UpdateModuleLights(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["state"] = ReadString(firstModule, "state").Length > 0
					? ReadString(firstModule, "state")
					: ReadString(firstModule, "installed").Equals("true", StringComparison.OrdinalIgnoreCase) ? "INSTALLED" : "MISSING",
			});
		}

		var currency = SafeValueQuery(queryNames, "get_currency", () => upstreamDataSource.GetCurrency());
		if (currency.HasValue)
		{
			updatedElements += UpdateCurrencyDisplay(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["balance"] = currency.Value.ToString(),
			});
		}

		return updatedElements;
	}

	private int RefreshExplorationHudFromDomain(List<string> queryNames)
	{
		var updatedElements = 0;
		var hull = SafeQuery(queryNames, "get_hull_integrity", () => upstreamDataSource.GetHullIntegrity());
		if (hull is not null)
		{
			updatedElements += UpdateHullBar(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["new"] = ReadString(hull, "current"),
			});
		}

		var search = SafeQuery(queryNames, "get_search_progress", () => upstreamDataSource.GetSearchProgress());
		if (search is not null)
		{
			updatedElements += UpdateSearchCount(search);
		}

		var preview = SafeValueQuery(queryNames, "get_scout_preview_level", () => upstreamDataSource.GetScoutPreviewLevel());
		if (!string.IsNullOrWhiteSpace(preview))
		{
			updatedElements += UpdateThreatPreview(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["level"] = preview,
			});
		}

		var carried = SafeListQuery(queryNames, "get_carried_inventory", () => upstreamDataSource.GetCarriedInventory());
		carriedSlots.Clear();
		if (carried.Count == 0)
		{
			hudElements[ExplorationCarriedGridElementId] = EmptyHudElement(ExplorationCarriedGridElementId);
			return updatedElements + 1;
		}

		foreach (var item in carried)
		{
			UpdateCarriedSlotState(ReadInt(item, "slot"), ReadString(item, "id"), ReadInt(item, "quantity"));
			updatedElements += UpdateCarriedGrid(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["slot"] = ReadString(item, "slot"),
				["item"] = ReadString(item, "id"),
				["qty"] = ReadString(item, "quantity"),
				["occupied_slots"] = carried.Count.ToString(),
				["capacity"] = DefaultCarriedSlotCapacity.ToString(),
			});
		}

		return updatedElements;
	}

	private void BindHubHud(Dictionary<string, string> fields, List<string> queryNames)
	{
		MergeFields(fields, SafeQuery(queryNames, "get_hull_integrity", () => upstreamDataSource.GetHullIntegrity()), "hull_");
		fields["module_count"] = SafeListQuery(queryNames, "get_module_states", () => upstreamDataSource.GetModuleStates()).Count.ToString();
		var storage = SafeQuery(queryNames, "get_storage_state", () => upstreamDataSource.GetStorageState());
		var cargo = SafeQuery(queryNames, "get_cargo_state", () => upstreamDataSource.GetCargoState());
		fields["storage_display"] = storage is null ? MissingResourceDisplay : $"{ReadString(storage, "current")}/{ReadString(storage, "max")}";
		cargoModuleInstalled = cargo is null
			|| !cargo.TryGetValue("has_module", out var hasModuleText)
			|| !bool.TryParse(hasModuleText, out var hasModule)
			|| hasModule;
		fields["cargo_display"] = cargo is null
			? MissingResourceDisplay
			: cargoModuleInstalled ? $"{ReadString(cargo, "current")}/{ReadString(cargo, "max")}" : "无货舱";
		fields["currency"] = SafeValueQuery(queryNames, "get_currency", () => upstreamDataSource.GetCurrency())?.ToString() ?? MissingResourceDisplay;
	}

	private string BindChartPanel(Dictionary<string, string> fields, List<string> queryNames)
	{
		MergeFields(fields, SafeQuery(queryNames, "get_chart_state", () => upstreamDataSource.GetChartState()), "chart_");
		var routes = SafeListQuery(queryNames, "get_visible_routes", () => upstreamDataSource.GetVisibleRoutes());
		MergeFields(fields, SafeQuery(queryNames, "get_filter_state", () => upstreamDataSource.GetFilterState()), "filter_");
		if (CurrentScreen == Screen.ChartRouteSelected)
		{
			MergeFields(fields, SafeQuery(queryNames, "get_selected_route", () => upstreamDataSource.GetSelectedRoute()), "selected_");
		}

		fields["visible_route_count"] = routes.Count.ToString();
		fields["visible_route_ids"] = string.Join(",", routes.Select(route => ReadString(route, "id")));
		return routes.Count == 0 ? ChartEmptyStateMessage : string.Empty;
	}

	private void BindExplorationHud(Dictionary<string, string> fields, List<string> queryNames)
	{
		fields["carried_count"] = SafeListQuery(queryNames, "get_carried_inventory", () => upstreamDataSource.GetCarriedInventory()).Count.ToString();
		MergeFields(fields, SafeQuery(queryNames, "get_search_progress", () => upstreamDataSource.GetSearchProgress()), "search_");
		fields["scout_preview_level"] = SafeValueQuery(queryNames, "get_scout_preview_level", () => upstreamDataSource.GetScoutPreviewLevel()) ?? string.Empty;
		MergeFields(fields, SafeQuery(queryNames, "get_hull_integrity", () => upstreamDataSource.GetHullIntegrity()), "hull_");
	}

	private void BindExtractionPanel(Dictionary<string, string> fields, List<string> queryNames)
	{
		MergeFields(fields, SafeQuery(queryNames, "get_extraction_state", () => upstreamDataSource.GetExtractionState()));
	}

	private void BindNamingPanel(Dictionary<string, string> fields, List<string> queryNames)
	{
		fields["partner_name"] = SafeValueQuery(queryNames, "query_partner_name", () => upstreamDataSource.QueryPartnerName()) ?? string.Empty;
		fields["naming_eligible"] = (SafeValueQuery(queryNames, "naming_prompt_eligibility", () => upstreamDataSource.NamingPromptEligibility()) ?? false).ToString();
	}

	private string BindPartnerSniffPanel(
		Dictionary<string, string> fields,
		List<string> queryNames,
		List<string> renderedItemIds,
		ref bool usedDisplayNameFallback)
	{
		var allItems = SafeListQuery(queryNames, "get_sniff_items", () => upstreamDataSource.GetSniffItems());
		if (allItems.Count == 0 && SafeValueQuery(queryNames, "query_partner_name", () => upstreamDataSource.QueryPartnerName()) is null)
		{
			fields["item_count"] = "0";
			return PartnerUnavailableMessage;
		}

		var sniffItems = allItems
			.Where(item => !string.IsNullOrWhiteSpace(ReadString(item, "cat_sniff_signature")))
			.ToArray();
		fields["item_count"] = sniffItems.Length.ToString();
		foreach (var item in sniffItems)
		{
			var itemId = ReadString(item, "id");
			renderedItemIds.Add(itemId);
			var displayName = ResolveDisplayName(itemId, queryNames, ref usedDisplayNameFallback);
			fields[$"item:{itemId}:name"] = displayName;
		}

		return sniffItems.Length == 0 ? PartnerSniffEmptyStateMessage : string.Empty;
	}

	private string BindStoragePanel(Dictionary<string, string> fields, List<string> queryNames)
	{
		var storage = SafeQuery(queryNames, "get_storage_state", () => upstreamDataSource.GetStorageState());
		var cargo = SafeQuery(queryNames, "get_cargo_state", () => upstreamDataSource.GetCargoState());
		MergeFields(fields, cargo, "cargo_");
		if (storage is null)
		{
			fields["current"] = "0";
			return StorageEmptyStateMessage;
		}

		MergeFields(fields, storage);
		return ReadInt(storage, "current") == 0 ? StorageEmptyStateMessage : string.Empty;
	}

	private string ResolveDisplayName(string entityId, List<string> queryNames, ref bool usedFallback)
	{
		var displayName = SafeValueQuery(queryNames, "get_display_name", () => upstreamDataSource.GetDisplayName(entityId));
		if (!string.IsNullOrWhiteSpace(displayName))
		{
			return displayName;
		}

		usedFallback = true;
		return entityId;
	}

	private static void MergeFields(
		Dictionary<string, string> target,
		IReadOnlyDictionary<string, string>? source,
		string prefix = "")
	{
		if (source is null)
		{
			return;
		}

		foreach (var item in source)
		{
			target[$"{prefix}{item.Key}"] = item.Value;
		}
	}

	private static IReadOnlyDictionary<string, string>? SafeQuery(
		List<string> queryNames,
		string queryName,
		Func<IReadOnlyDictionary<string, string>?> query)
	{
		queryNames.Add(queryName);
		try
		{
			return query();
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static IReadOnlyList<IReadOnlyDictionary<string, string>> SafeListQuery(
		List<string> queryNames,
		string queryName,
		Func<IReadOnlyList<IReadOnlyDictionary<string, string>>?> query)
	{
		queryNames.Add(queryName);
		try
		{
			return query() ?? Array.Empty<IReadOnlyDictionary<string, string>>();
		}
		catch (Exception)
		{
			return Array.Empty<IReadOnlyDictionary<string, string>>();
		}
	}

	private static T? SafeValueQuery<T>(List<string> queryNames, string queryName, Func<T?> query)
	{
		queryNames.Add(queryName);
		try
		{
			return query();
		}
		catch (Exception)
		{
			return default;
		}
	}

	private static bool SafeCommand(Func<bool> command)
	{
		try
		{
			return command();
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void OnHudSignal(string elementId, IReadOnlyDictionary<string, string> payload)
	{
		dirtyFlags[elementId] = true;
		pendingPayloads[elementId] = new Dictionary<string, string>(payload, StringComparer.Ordinal);
	}

	private int UpdateHudElement(string elementId, IReadOnlyDictionary<string, string> payload)
	{
		switch (elementId)
		{
			case HullBarSignalId:
				return UpdateHullBar(payload);
			case HullBandSignalId:
				return UpdateHullBand(payload);
			case StorageBarSignalId:
				return UpdateStorageBar(payload);
			case CargoBarSignalId:
				return UpdateCargoDisplay(payload);
			case CarriedGridSignalId:
				return UpdateCarriedGrid(payload);
			case SearchCountSignalId:
				return UpdateSearchCount(payload);
			case ThreatPreviewSignalId:
				return UpdateThreatPreview(payload);
			case ModuleLightsSignalId:
				return UpdateModuleLights(payload);
			case CurrencyDisplaySignalId:
				return UpdateCurrencyDisplay(payload);
			default:
				hudElements[elementId] = EmptyHudElement(elementId) with { Visible = true };
				return 1;
		}
	}

	private int UpdateHullBar(IReadOnlyDictionary<string, string> payload)
	{
		var value = ReadInt(payload, "new");
		var width = Math.Clamp((int)Math.Round(value / 100.0 * HudMaxBarWidth), 0, HudMaxBarWidth);
		var color = value switch
		{
			>= 76 => SafeGreenHex,
			>= 26 => WarningAmberHex,
			_ => DangerRedHex,
		};
		var shape = value switch
		{
			>= 76 => "shape.check",
			>= 26 => "shape.bolt",
			_ => "shape.circle",
		};

		UpsertHudElement(HubHullBarElementId, $"{value}/100", width, color, shape, "hull");
		UpsertHudElement(ExplorationHullBarElementId, $"{value}/100", width, color, shape, "hull");
		if (value <= 0)
		{
			UpsertHudElement(HubHullRepairIconElementId, "需要维修", 0, DangerRedHex, "shape.wrench.blink", "wrench.blink");
		}
		else
		{
			hudElements[HubHullRepairIconElementId] = EmptyHudElement(HubHullRepairIconElementId);
		}

		return 2;
	}

	private int UpdateHullBand(IReadOnlyDictionary<string, string> payload)
	{
		var newBand = ReadString(payload, "new_band").ToUpperInvariant();
		var color = newBand switch
		{
			"GREEN" => SafeGreenHex,
			"YELLOW" => WarningAmberHex,
			"RED" => DangerRedHex,
			_ => WarningAmberHex,
		};
		var shape = newBand switch
		{
			"GREEN" => "shape.check",
			"YELLOW" => "shape.bolt",
			"RED" => "shape.circle",
			_ => "shape.unknown",
		};

		UpsertHudElement(HubHullBarElementId, newBand, HudMaxBarWidth, color, shape, "hull");
		UpsertHudElement(ExplorationHullBarElementId, newBand, HudMaxBarWidth, color, shape, "hull");
		return 2;
	}

	private int UpdateStorageBar(IReadOnlyDictionary<string, string> payload)
	{
		var current = ReadInt(payload, "current");
		var max = Math.Max(1, ReadInt(payload, "max"));
		var width = Math.Clamp((int)Math.Round(current / (double)max * HudMaxBarWidth), 0, HudMaxBarWidth);
		UpsertHudElement(HubStorageElementId, $"{current}/{max}", width, SafeGreenHex, "shape.bar", "storage");
		return 1;
	}

	private int UpdateCargoDisplay(IReadOnlyDictionary<string, string> payload)
	{
		var hasModule = !payload.TryGetValue("has_module", out var hasModuleText)
			|| !bool.TryParse(hasModuleText, out var parsed)
			|| parsed;
		cargoModuleInstalled = hasModule;
		if (!hasModule)
		{
			UpsertHudElement(HubCargoElementId, "无货舱", 0, DisabledGrayHex, "shape.disabled", "cargo.missing");
			return 1;
		}

		var current = ReadInt(payload, "current");
		var max = Math.Max(1, ReadInt(payload, "max"));
		var width = Math.Clamp((int)Math.Round(current / (double)max * HudMaxBarWidth), 0, HudMaxBarWidth);
		UpsertHudElement(HubCargoElementId, $"{current}/{max}", width, SafeGreenHex, "shape.bar", "cargo");
		return 1;
	}

	private int UpdateCarriedGrid(IReadOnlyDictionary<string, string> payload)
	{
		var slot = ReadInt(payload, "slot");
		var item = ReadString(payload, "item");
		var quantity = ReadInt(payload, "qty");
		var occupiedSlots = ReadInt(payload, "occupied_slots");
		var capacity = Math.Max(1, ReadInt(payload, "capacity"));
		var borderColor = occupiedSlots >= capacity ? WarningAmberHex : string.Empty;

		hudElements[ExplorationCarriedGridElementId] = new HudElementSnapshot(
			ExplorationCarriedGridElementId,
			$"slot:{slot}:{item}:x{quantity}",
			BarWidth: 0,
			ColorHex: string.Empty,
			ShapeToken: "shape.grid",
			IconToken: item,
			BorderColorHex: borderColor,
			Visible: true);
		return 1;
	}

	private int UpdateSearchCount(IReadOnlyDictionary<string, string> payload)
	{
		var searched = ReadInt(payload, "searched");
		var total = ReadInt(payload, "total");
		UpsertHudElement(ExplorationSearchCountElementId, $"{searched}/{total}", 0, string.Empty, "shape.text", "search");
		return 1;
	}

	private int UpdateThreatPreview(IReadOnlyDictionary<string, string> payload)
	{
		var level = ReadString(payload, "level").ToUpperInvariant();
		switch (level)
		{
			case ScoutPreviewFull:
				UpsertHudElement(
					ExplorationThreatPreviewElementId,
					ScoutPreviewFull,
					0,
					WarningAmberHex,
					"shape.warning",
					"threat.preview.full");
				break;
			case ScoutPreviewPresence:
				UpsertHudElement(
					ExplorationThreatPreviewElementId,
					"!",
					0,
					DangerRedHex,
					"shape.warning",
					"threat.preview.presence");
				break;
			case ScoutPreviewNone:
			default:
				hudElements[ExplorationThreatPreviewElementId] = EmptyHudElement(ExplorationThreatPreviewElementId);
				break;
		}

		return 1;
	}

	private int UpdateModuleLights(IReadOnlyDictionary<string, string> payload)
	{
		var state = ReadString(payload, "state").ToUpperInvariant();
		var color = state == "INSTALLED" ? SafeGreenHex : WarningAmberHex;
		var shape = state == "INSTALLED" ? "shape.check" : "shape.bolt";
		UpsertHudElement(HubModuleLightsElementId, state, 0, color, shape, "module");
		return 1;
	}

	private int UpdateCurrencyDisplay(IReadOnlyDictionary<string, string> payload)
	{
		var balance = ReadInt(payload, "balance");
		UpsertHudElement(HubCurrencyElementId, balance.ToString(), 0, string.Empty, "shape.coin", "currency");
		UpsertHudElement(MarketCurrencyElementId, balance.ToString(), 0, string.Empty, "shape.coin", "currency");
		return 2;
	}

	private void UpsertHudElement(
		string elementId,
		string text,
		int barWidth,
		string colorHex,
		string shapeToken,
		string iconToken)
	{
		hudElements[elementId] = new HudElementSnapshot(
			elementId,
			text,
			barWidth,
			colorHex,
			shapeToken,
			iconToken,
			BorderColorHex: string.Empty,
			Visible: true);
	}

	private static HudElementSnapshot EmptyHudElement(string elementId)
	{
		return new HudElementSnapshot(
			elementId,
			Text: string.Empty,
			BarWidth: 0,
			ColorHex: string.Empty,
			ShapeToken: string.Empty,
			IconToken: string.Empty,
			BorderColorHex: string.Empty,
			Visible: false);
	}

	private void PreloadPanelData(string panelId, bool distanceDriven)
	{
		preloadedPanels.Add(panelId);
		CachePanelInstance(panelId);
		var lifecycle = GetOrCreatePanelLifecycle(panelId);
		lifecycle.State = PanelLifecycleState.Ready;
		lifecycle.IsPreloaded = true;
		lifecycle.PreloadNonBlocking = true;
		lifecycle.DistanceDriven = distanceDriven;
		lifecycle.DomainEventDriven = false;
	}

	private void MarkNonModalPanelActive(string panelId)
	{
		var lifecycle = GetOrCreatePanelLifecycle(panelId);
		lifecycle.State = PanelLifecycleState.Active;
		lifecycle.IsPreloaded = IsPanelPreloaded(panelId);
		lifecycle.DistanceDriven = true;
		lifecycle.DomainEventDriven = false;
		lifecycle.OpenAnimationSeconds = PanelOpenAnimationSeconds;
		lifecycle.CloseAnimationSeconds = 0;
		lifecycle.InstantiationDelayMilliseconds = IsPanelPreloaded(panelId)
			? PreloadedPanelInstantiationDelayMilliseconds
			: LazyPanelInstantiationDelayMilliseconds;
		CachePanelInstance(panelId);
	}

	private void MarkModalPanelActive(string panelId)
	{
		var lifecycle = GetOrCreatePanelLifecycle(panelId);
		lifecycle.State = PanelLifecycleState.Active;
		lifecycle.IsPreloaded = IsPanelPreloaded(panelId);
		lifecycle.DistanceDriven = false;
		lifecycle.DomainEventDriven = true;
		lifecycle.OpenAnimationSeconds = 0;
		lifecycle.CloseAnimationSeconds = 0;
		lifecycle.InstantiationDelayMilliseconds = IsPanelPreloaded(panelId)
			? PreloadedPanelInstantiationDelayMilliseconds
			: LazyPanelInstantiationDelayMilliseconds;
		CachePanelInstance(panelId);
	}

	private void MarkPanelClosed(string panelId, double closeAnimationSeconds)
	{
		if (!panelLifecycleStates.TryGetValue(panelId, out var lifecycle))
		{
			return;
		}

		lifecycle.State = PanelLifecycleState.Closed;
		lifecycle.CloseAnimationSeconds = closeAnimationSeconds;
	}

	private void ClearPanelCache()
	{
		foreach (var panelId in panelCacheLru)
		{
			freedPanelInstances.Add(panelId);
		}

		panelCacheLru.Clear();
	}

	private PanelLifecycleRuntimeState GetOrCreatePanelLifecycle(string panelId)
	{
		if (!panelLifecycleStates.TryGetValue(panelId, out var lifecycle))
		{
			lifecycle = new PanelLifecycleRuntimeState(panelId);
			panelLifecycleStates[panelId] = lifecycle;
		}

		return lifecycle;
	}

	private static string PanelIdForAnchor(string anchorId)
	{
		return anchorId switch
		{
			PartnerSniffAnchorId => PartnerSniffScreenId,
			StorageAnchorId => StorageScreenId,
			IntelStationAnchorId => StationDetailScreenId,
			StorageStationAnchorId => StationDetailScreenId,
			_ => string.Empty,
		};
	}

	private static int ReadInt(IReadOnlyDictionary<string, string> payload, string key)
	{
		return payload.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
			? parsed
			: 0;
	}

	private static string ReadString(IReadOnlyDictionary<string, string> payload, string key)
	{
		return payload.TryGetValue(key, out var value) ? value : string.Empty;
	}

	private static double CalculateContrastRatio(string foregroundHex, string backgroundHex)
	{
		var foreground = RelativeLuminance(foregroundHex);
		var background = RelativeLuminance(backgroundHex);
		var lighter = Math.Max(foreground, background);
		var darker = Math.Min(foreground, background);
		return (lighter + 0.05) / (darker + 0.05);
	}

	private static double RelativeLuminance(string hex)
	{
		var normalized = NormalizeHex(hex);
		var r = Convert.ToInt32(normalized.Substring(0, 2), 16);
		var g = Convert.ToInt32(normalized.Substring(2, 2), 16);
		var b = Convert.ToInt32(normalized.Substring(4, 2), 16);
		return 0.2126 * LinearizeSrgb(r)
			+ 0.7152 * LinearizeSrgb(g)
			+ 0.0722 * LinearizeSrgb(b);
	}

	private static double LinearizeSrgb(int channel)
	{
		var value = channel / 255.0;
		return value <= 0.03928
			? value / 12.92
			: Math.Pow((value + 0.055) / 1.055, 2.4);
	}

	private static string NormalizeHex(string hex)
	{
		var value = (hex ?? string.Empty).Trim().TrimStart('#');
		if (value.Length != 6 || value.Any(c => !Uri.IsHexDigit(c)))
		{
			throw new ArgumentException("Expected a 6-digit RGB hex color.", nameof(hex));
		}

		return value;
	}

	private void UpdateCarriedSlotState(int slot, string itemId, int quantity)
	{
		if (slot < 0 || slot >= DefaultCarriedSlotCapacity)
		{
			return;
		}

		if (quantity <= 0 || string.IsNullOrWhiteSpace(itemId))
		{
			carriedSlots.Remove(slot);
			return;
		}

		carriedSlots[slot] = new CarriedSlotState(itemId, quantity);
	}

	private int CountOccupiedCarriedSlots()
	{
		return Math.Min(carriedSlots.Count, DefaultCarriedSlotCapacity);
	}

	private void EnterDepartureLocked()
	{
		ForceCloseAllPanels();
		DepartureLocked = true;
		DepartureLockRemainingSeconds = 2.0;
		TransitionTo(Screen.DepartureLocked, validate: false);
	}

	private ScreenResult TransitionTo(Screen newScreen, bool validate)
	{
		if (newScreen == CurrentScreen)
		{
			return ScreenResult.ErrInvalidScreen;
		}

		if (validate && !IsValidTransition(CurrentScreen, newScreen))
		{
			return ScreenResult.ErrInvalidScreen;
		}

		var oldScreen = CurrentScreen;
		CurrentScreen = newScreen;
		ApplyScreenVisibility(newScreen);
		if (newScreen == Screen.Chart)
		{
			BindScreenData(ChartScreenId);
		}

		ScreenChanged?.Invoke(oldScreen, newScreen);
		return ScreenResult.Success;
	}

	private void ApplyScreenVisibility(Screen screen)
	{
		visiblePanels.Remove(HubHudScreenId);
		visiblePanels.Remove(ChartScreenId);
		visiblePanels.Remove(ExplorationHudScreenId);

		if (screen is Screen.Hub or Screen.HubArriving)
		{
			visiblePanels.Add(HubHudScreenId);
		}

		if (screen is Screen.Chart or Screen.ChartRouteSelected or Screen.ChartDepartureConfirmed)
		{
			visiblePanels.Add(ChartScreenId);
		}

		if (screen is Screen.Exploration or Screen.Extracting)
		{
			visiblePanels.Add(ExplorationHudScreenId);
		}

		if (screen == Screen.Voyage)
		{
			ClearPanelCache();
		}

		UpdateActiveInputLayer();
	}

	private static bool IsValidTransition(Screen current, Screen next)
	{
		return (current, next) switch
		{
			(Screen.None, Screen.Hub) => true,
			(Screen.Hub, Screen.Chart) => true,
			(Screen.Hub, Screen.DepartureLocked) => true,
			(Screen.DepartureLocked, Screen.Chart) => true,
			(Screen.Chart, Screen.ChartRouteSelected) => true,
			(Screen.ChartRouteSelected, Screen.ChartDepartureConfirmed) => true,
			(Screen.ChartDepartureConfirmed, Screen.Voyage) => true,
			(Screen.Chart, Screen.Hub) => true,
			(Screen.ChartRouteSelected, Screen.Hub) => true,
			(Screen.Voyage, Screen.Exploration) => true,
			(Screen.Exploration, Screen.Extracting) => true,
			(Screen.Extracting, Screen.Settlement) => true,
			(Screen.Settlement, Screen.HubArriving) => true,
			(Screen.HubArriving, Screen.Hub) => true,
			_ => false,
		};
	}

	private ModalResult OpenCombatOverride(
		IReadOnlyDictionary<string, string>? dataContext,
		int scrollOffset,
		int selectedIndex)
	{
		if (string.IsNullOrEmpty(modalPanel))
		{
			OpenModalCore(CombatScreenId, dataContext, scrollOffset, selectedIndex, CombatOverrideCanvasLayer, pushFocusRestore: true);
			return ModalResult.Success;
		}

		combatOverridePanelId = modalPanel;
		var overridden = GetOrCreatePanelState(combatOverridePanelId);
		overridden.FocusedElementId = KeyboardFocusElementId;
		overridden.InputEnabled = false;
		overridden.Opacity = 0.2;

		OpenModalCore(CombatScreenId, dataContext, scrollOffset, selectedIndex, CombatOverrideCanvasLayer, pushFocusRestore: false);
		return ModalResult.Success;
	}

	private void OpenModalCore(
		string panelId,
		IReadOnlyDictionary<string, string>? dataContext,
		int scrollOffset,
		int selectedIndex,
		int canvasLayer,
		bool pushFocusRestore)
	{
		if (pushFocusRestore)
		{
			focusRestoreStack.Push(KeyboardFocusElementId);
		}

		modalPanel = panelId;
		var state = new PanelRuntimeState(panelId, dataContext, scrollOffset, selectedIndex, canvasLayer);
		panelStates[panelId] = state;
		visiblePanels.Add(panelId);
		MarkModalPanelActive(panelId);
		RecordModalOpenAnimation(panelId);
		BindScreenData(panelId, state.DataContext);
		KeyboardFocusElementId = FirstFocusableElement(panelId);
		state.FocusedElementId = KeyboardFocusElementId;
		ActiveInputLayer = InputLayer.Modal;
		EmitUiPanelOpened(panelId);
	}

	private bool OpenNextQueuedModal()
	{
		if (queuedModals.Count == 0)
		{
			return false;
		}

		var queued = queuedModals.Dequeue();
		OpenModalCore(
			queued.PanelId,
			queued.DataContext,
			queued.ScrollOffset,
			queued.SelectedIndex,
			canvasLayer: 0,
			pushFocusRestore: true);
		return true;
	}

	private void RestorePreviousFocus()
	{
		var previousFocus = focusRestoreStack.Count > 0 ? focusRestoreStack.Pop() : string.Empty;
		if (!string.IsNullOrWhiteSpace(previousFocus)
			&& IsElementFocusable(previousFocus)
			&& !destroyedFocusableElements.Contains(previousFocus))
		{
			KeyboardFocusElementId = previousFocus;
			return;
		}

		KeyboardFocusElementId = FirstFocusableElementInActiveScreen();
	}

	private PanelRuntimeState GetOrCreatePanelState(string panelId)
	{
		if (!panelStates.TryGetValue(panelId, out var state))
		{
			state = new PanelRuntimeState(panelId, dataContext: null, scrollOffset: 0, selectedIndex: -1, canvasLayer: 0);
			panelStates[panelId] = state;
		}

		return state;
	}

	private void UpdateActiveInputLayer()
	{
		ActiveInputLayer = GetActiveInputLayer();
	}

	private bool IsHudOverlayVisible()
	{
		return HubHudVisible || ExplorationHudVisible;
	}

	private InputLayer ActiveInputLayerForNonModalOrWorld()
	{
		return nonModalPanels.Count > 0
			? InputLayer.NonModal
			: InputLayer.World;
	}

	private string FirstFocusableElementInActiveScreen()
	{
		var candidates = CurrentScreen switch
		{
			Screen.Hub or Screen.HubArriving => new[] { "hub.helm", "hub.gangway" },
			Screen.Chart or Screen.ChartRouteSelected => new[] { "chart.route_confirm" },
			_ => Array.Empty<string>(),
		};

		return candidates.FirstOrDefault(elementId => !destroyedFocusableElements.Contains(elementId)) ?? string.Empty;
	}

	private string FirstFocusableElement(string panelId)
	{
		var focusChain = ActiveFocusChainForPanel(panelId);
		return focusChain.Length > 0 ? focusChain[0] : PanelContainerFocusId(panelId);
	}

	private string[] ActiveFocusChainForPanel(string panelId)
	{
		return FocusChainForPanel(panelId)
			.Where(elementId => !destroyedFocusableElements.Contains(elementId))
			.ToArray();
	}

	private static string[] FocusChainForPanel(string panelId)
	{
		return panelId switch
		{
			DepartureConfirmScreenId => new[] { "departure.confirm", "departure.cancel" },
			CapacityChoiceScreenId => new[] { "capacity.keep", "capacity.discard", "capacity.confirm" },
			SettlementSummaryScreenId => new[] { "settlement.confirm" },
			CombatScreenId => new[] { "combat.emergency", "combat.hold_ground", "combat.retreat" },
			RepairScreenId => new[] { "repair.plus_one", "repair.confirm", "repair.cancel" },
			MarketScreenId => new[] { "market.buy", "market.cancel" },
			NamingScreenId => new[] { "naming.name_input", "naming.confirm", "naming.skip" },
			RegistryDiagnosticToolsPanelId => new[] { "registry_diagnostic.close" },
			_ => Array.Empty<string>(),
		};
	}

	private static string PanelContainerFocusId(string panelId)
	{
		return $"panel:{panelId}";
	}

	private bool IsElementInActiveFocusScope(string elementId)
	{
		if (IsModalOpen())
		{
			return ActiveFocusChainForPanel(modalPanel).Contains(elementId, StringComparer.Ordinal)
				|| string.Equals(elementId, PanelContainerFocusId(modalPanel), StringComparison.Ordinal);
		}

		foreach (var panelId in nonModalPanels.AsEnumerable().Reverse())
		{
			if (ActiveFocusChainForPanel(panelId).Contains(elementId, StringComparer.Ordinal))
			{
				return true;
			}
		}

		return CurrentScreen switch
		{
			Screen.Hub => elementId.StartsWith("hub.", StringComparison.Ordinal),
			Screen.Chart => elementId.StartsWith("chart.", StringComparison.Ordinal),
			_ => false,
		};
	}

	private static readonly HashSet<string> AllFocusableElements = BuildFocusableElements();

	private static HashSet<string> BuildFocusableElements()
	{
		var elements = new HashSet<string>(StringComparer.Ordinal)
		{
			"hub.helm",
			"hub.gangway",
			"chart.route_confirm",
		};

		foreach (var panelId in new[]
		{
			DepartureConfirmScreenId,
			CapacityChoiceScreenId,
			SettlementSummaryScreenId,
			CombatScreenId,
			RepairScreenId,
			MarketScreenId,
			NamingScreenId,
			RegistryDiagnosticToolsPanelId,
		})
		{
			foreach (var element in FocusChainForPanel(panelId))
			{
				elements.Add(element);
			}
		}

		return elements;
	}

	private sealed class NullUiUpstreamDataSource : IUiUpstreamDataSource
	{
		public static readonly NullUiUpstreamDataSource Instance = new();

		private NullUiUpstreamDataSource()
		{
		}

		public IReadOnlyDictionary<string, string>? GetChartState() => null;

		public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetVisibleRoutes() => null;

		public IReadOnlyDictionary<string, string>? GetSelectedRoute() => null;

		public IReadOnlyDictionary<string, string>? GetFilterState() => null;

		public IReadOnlyDictionary<string, string>? GetHullIntegrity() => null;

		public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates() => null;

		public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory() => null;

		public IReadOnlyDictionary<string, string>? GetStorageState() => null;

		public IReadOnlyDictionary<string, string>? GetCargoState() => null;

		public int? GetCurrency() => null;

		public IReadOnlyDictionary<string, string>? GetSearchProgress() => null;

		public string? GetScoutPreviewLevel() => null;

		public IReadOnlyDictionary<string, string>? GetExtractionState() => null;

		public IReadOnlyDictionary<string, string>? BuildThreatContext() => null;

		public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId) => null;

		public IReadOnlyDictionary<string, string>? GetStallData(string stallId) => null;

		public string? QueryPartnerName() => null;

		public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems() => null;

		public bool? NamingPromptEligibility() => null;

		public string? GetDisplayName(string entityId) => null;

		public string? GetDescription(string entityId) => null;

		public bool TransferItem(string itemId, string fromPool, string toPool, int quantity) => false;

		public bool DiscardItem(string itemId) => false;

		public bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials) => false;

		public bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost) => false;

		public bool SubmitPartnerName(string partnerId, string name) => false;
	}

	private sealed record PendingModalRequest(
		string PanelId,
		IReadOnlyDictionary<string, string>? DataContext,
		int ScrollOffset,
		int SelectedIndex);

	private sealed record CarriedSlotState(string ItemId, int Quantity);

	private sealed class PanelLifecycleRuntimeState
	{
		public PanelLifecycleRuntimeState(string panelId)
		{
			PanelId = panelId;
		}

		public string PanelId { get; }

		public PanelLifecycleState State { get; set; }

		public bool IsPreloaded { get; set; }

		public bool PreloadNonBlocking { get; set; }

		public bool DistanceDriven { get; set; }

		public bool DomainEventDriven { get; set; }

		public double OpenAnimationSeconds { get; set; }

		public double CloseAnimationSeconds { get; set; }

		public double InstantiationDelayMilliseconds { get; set; }

		public PanelLifecycleSnapshot ToSnapshot()
		{
			return new PanelLifecycleSnapshot(
				PanelId,
				State,
				IsPreloaded,
				PreloadNonBlocking,
				DistanceDriven,
				DomainEventDriven,
				OpenAnimationSeconds,
				CloseAnimationSeconds,
				InstantiationDelayMilliseconds);
		}
	}

	private sealed class PanelRuntimeState
	{
		public PanelRuntimeState(
			string panelId,
			IReadOnlyDictionary<string, string>? dataContext,
			int scrollOffset,
			int selectedIndex,
			int canvasLayer)
		{
			PanelId = panelId;
			DataContext = dataContext is null
				? new Dictionary<string, string>(StringComparer.Ordinal)
				: new Dictionary<string, string>(dataContext, StringComparer.Ordinal);
			ScrollOffset = scrollOffset;
			SelectedIndex = selectedIndex;
			CanvasLayer = canvasLayer;
		}

		public string PanelId { get; }

		public Dictionary<string, string> DataContext { get; }

		public int ScrollOffset { get; }

		public int SelectedIndex { get; }

		public bool InputEnabled { get; set; } = true;

		public double Opacity { get; set; } = 1.0;

		public int CanvasLayer { get; set; }

		public string FocusedElementId { get; set; } = string.Empty;

		public ModalPanelSnapshot ToSnapshot()
		{
			return new ModalPanelSnapshot(
				PanelId,
				new Dictionary<string, string>(DataContext, StringComparer.Ordinal),
				ScrollOffset,
				SelectedIndex,
				InputEnabled,
				Opacity,
				CanvasLayer,
				FocusedElementId);
		}
	}

	private sealed class HeadlessUiAnimationDriver : IUiAnimationDriver
	{
		public static readonly HeadlessUiAnimationDriver Instance = new();

		private HeadlessUiAnimationDriver()
		{
		}

		public UiAnimationSnapshot PlayTween(UiAnimationSnapshot request)
		{
			if (!request.UsesSceneTreeTween || request.UsesManualProcessInterpolation)
			{
				throw new InvalidOperationException("UI animations must be routed through SceneTreeTween contracts.");
			}

			return request;
		}

		public UiAnimationSnapshot KillTween(UiAnimationSnapshot activeSnapshot, bool applyFinalState)
		{
			return activeSnapshot with { IsKilled = true, FinalStateApplied = applyFinalState };
		}

		public PanelTextureContractSnapshot ConfigurePanelTexture(PanelTextureContractSnapshot contract)
		{
			return contract;
		}
	}

	private sealed class SemanticCascadeScope : IDisposable
	{
		private readonly UIManager owner;
		private readonly string eventId;
		private bool disposed;

		public SemanticCascadeScope(UIManager owner, string eventId)
		{
			this.owner = owner;
			this.eventId = eventId;
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			owner.ExitSemanticEventConsumerScope(eventId);
			disposed = true;
		}
	}

	private sealed class NoopDisposable : IDisposable
	{
		public static readonly NoopDisposable Instance = new();

		private NoopDisposable()
		{
		}

		public void Dispose()
		{
		}
	}

	private static OnboardingHintRenderSnapshot BuildOnboardingHintSnapshot(
		OnboardingHintRequest request,
		OnboardingSurface activeSurface,
		string targetSurfaceId,
		bool visible,
		bool skipped,
		string? fallbackReason,
		bool textOnlyFallback)
	{
		return new OnboardingHintRenderSnapshot(
			request.StepId,
			request.HintTextKey,
			request.HighlightAnchorId,
			targetSurfaceId,
			activeSurface,
			visible,
			skipped,
			fallbackReason,
			textOnlyFallback,
			HasTextLabel: !string.IsNullOrWhiteSpace(request.HintTextKey),
			ColorOnlyMeaning: false,
			FocusDisabled: true,
			MouseFilterMode.Ignore,
			CapturesKeyboardFocus: false,
			CapturesMouseInput: false,
			IsModal: false,
			CoversResourceLabel: false,
			CoversThreatLabel: false,
			CoversHullLabel: false,
			CoversStatusLabel: false,
			KeyboardPathValid: true,
			MousePathValid: true,
			ReadabilityGuardForOnboarding(activeSurface));
	}

	private static string SurfaceIdForOnboarding(OnboardingSurface activeSurface)
	{
		return activeSurface switch
		{
			OnboardingSurface.Chart => ChartScreenId,
			OnboardingSurface.Exploration => ExplorationHudScreenId,
			OnboardingSurface.Session => "session_status",
			OnboardingSurface.Hub => HubHudScreenId,
			_ => "onboarding.safe_text",
		};
	}

	private static string ReadabilityGuardForOnboarding(OnboardingSurface activeSurface)
	{
		return activeSurface switch
		{
			OnboardingSurface.Chart => "preserve:route_identity,route_risk_text,departure_status",
			OnboardingSurface.Exploration =>
				$"preserve:{ExplorationHullBarElementId},{ExplorationSearchCountElementId},{ExplorationThreatPreviewElementId},{ExplorationCarriedGridElementId},status",
			OnboardingSurface.Session => "preserve:session_status_text",
			OnboardingSurface.Hub => "preserve:hub_status,hub_summary,hub_controls",
			_ => "preserve:active_surface_text",
		};
	}

	private static bool IsHubAnchor(string? anchorId)
	{
		return !string.IsNullOrWhiteSpace(anchorId)
			&& (anchorId.StartsWith("hub.", StringComparison.Ordinal)
				|| anchorId.StartsWith("anchor.", StringComparison.Ordinal)
				|| string.Equals(anchorId, HubHudScreenId, StringComparison.Ordinal));
	}

	private static Dictionary<string, ScreenDefinition> BuildScreenRegistry()
	{
		return new Dictionary<string, ScreenDefinition>(StringComparer.Ordinal)
		{
			[HubHudScreenId] = new(HubHudScreenId, ScreenType.HudOverlay, "hub"),
			[StationDetailScreenId] = new(StationDetailScreenId, ScreenType.NonModal, "hub"),
			[DepartureConfirmScreenId] = new(DepartureConfirmScreenId, ScreenType.Modal, "hub"),
			[ChartScreenId] = new(ChartScreenId, ScreenType.Fullscreen, "chart"),
			[ExplorationHudScreenId] = new(ExplorationHudScreenId, ScreenType.HudOverlay, "exploration"),
			[CapacityChoiceScreenId] = new(CapacityChoiceScreenId, ScreenType.Modal, "resources"),
			[ExtractionProgressScreenId] = new(ExtractionProgressScreenId, ScreenType.SemiModal, "exploration"),
			[SettlementSummaryScreenId] = new(SettlementSummaryScreenId, ScreenType.Modal, "exploration"),
			[CombatScreenId] = new(CombatScreenId, ScreenType.Modal, "combat"),
			[RepairScreenId] = new(RepairScreenId, ScreenType.Modal, "world-repair"),
			[MarketScreenId] = new(MarketScreenId, ScreenType.Modal, "settlement"),
			[NamingScreenId] = new(NamingScreenId, ScreenType.Modal, "partner"),
			[PartnerSniffScreenId] = new(PartnerSniffScreenId, ScreenType.NonModal, "partner"),
			[StorageScreenId] = new(StorageScreenId, ScreenType.NonModal, "resources"),
			[RegistryDiagnosticToolsPanelId] = new(RegistryDiagnosticToolsPanelId, ScreenType.Modal, "registry"),
		};
	}

	private static Dictionary<string, PanelTextureContractSnapshot> BuildPanelTextureContracts()
	{
		return new Dictionary<string, PanelTextureContractSnapshot>(StringComparer.Ordinal)
		{
			[ChartScreenId] = ParchmentTextureContract(ChartScreenId),
			[StationDetailScreenId] = ParchmentTextureContract(StationDetailScreenId),
			[PartnerSniffScreenId] = ParchmentTextureContract(PartnerSniffScreenId),
			[StorageScreenId] = ParchmentTextureContract(StorageScreenId),
		};
	}

	private static PanelTextureContractSnapshot ParchmentTextureContract(string panelId)
	{
		return new PanelTextureContractSnapshot(
			panelId,
			UsesNinePatchRect: true,
			ParchmentTextureSourceSize,
			ParchmentTextureSourceSize,
			UsesFullSizeTexture: false);
	}
}
