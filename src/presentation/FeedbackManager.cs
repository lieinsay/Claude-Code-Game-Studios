using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// 反馈请求的语义优先级；数值保持 ADR-0016 指定的公开合同。
/// </summary>
public enum FeedbackPriority
{
	Ambient = 0,
	Minor = 1,
	Major = 2,
	Critical = 3,
}

/// <summary>
/// 一次反馈路由或输出选择的诊断决策。
/// </summary>
public enum FeedbackOutputDecision
{
	Routed = 0,
	Coalesced = 1,
	OutputSelected = 2,
	SkippedUnsupported = 3,
	SkippedInvalidPayload = 4,
	Idle = 5,
}

/// <summary>
/// ADR-0016 规定的不可变反馈请求值。
/// </summary>
public sealed record FeedbackRequest
{
	/// <summary>创建不可变反馈请求，并复制载荷以隔离调用方可变字典。</summary>
	public FeedbackRequest(
		string eventId,
		string sourceSystem,
		string cueFamily,
		FeedbackPriority priority,
		string coalesceKey,
		string? visualCueId,
		string? audioCueId,
		string? captionText,
		string? statusText,
		IReadOnlyDictionary<string, object?>? payload)
	{
		EventId = RequireText(eventId, nameof(eventId));
		SourceSystem = RequireText(sourceSystem, nameof(sourceSystem));
		CueFamily = RequireText(cueFamily, nameof(cueFamily));
		Priority = priority;
		CoalesceKey = string.IsNullOrWhiteSpace(coalesceKey) ? EventId : coalesceKey;
		VisualCueId = string.IsNullOrWhiteSpace(visualCueId) ? null : visualCueId;
		AudioCueId = string.IsNullOrWhiteSpace(audioCueId) ? null : audioCueId;
		CaptionText = string.IsNullOrWhiteSpace(captionText) ? null : captionText;
		StatusText = string.IsNullOrWhiteSpace(statusText) ? null : statusText;
		Payload = CopyPayload(payload);
	}

	/// <summary>稳定语义事件 ID。</summary>
	public string EventId { get; init; }

	/// <summary>发出语义事件的系统标识。</summary>
	public string SourceSystem { get; init; }

	/// <summary>反馈提示所属的呈现族群。</summary>
	public string CueFamily { get; init; }

	/// <summary>反馈优先级。</summary>
	public FeedbackPriority Priority { get; init; }

	/// <summary>重复反馈合并所用的稳定键。</summary>
	public string CoalesceKey { get; init; }

	/// <summary>可选视觉提示 ID。</summary>
	public string? VisualCueId { get; init; }

	/// <summary>可选音频提示 ID。</summary>
	public string? AudioCueId { get; init; }

	/// <summary>可选字幕文本或本地化键。</summary>
	public string? CaptionText { get; init; }

	/// <summary>可选状态文本或本地化键。</summary>
	public string? StatusText { get; init; }

	/// <summary>只读上下文载荷；#17 不拥有也不写回领域状态。</summary>
	public IReadOnlyDictionary<string, object?> Payload { get; init; }

	private static string RequireText(string value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Value must not be empty.", parameterName);
		}

		return value;
	}

	private static IReadOnlyDictionary<string, object?> CopyPayload(IReadOnlyDictionary<string, object?>? payload)
	{
		var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
		if (payload is not null)
		{
			foreach (var (key, value) in payload)
			{
				copy[key] = value;
			}
		}

		return new ReadOnlyDictionary<string, object?>(copy);
	}
}

/// <summary>
/// 路由请求后的结果，包含请求值与同步写入的诊断快照。
/// </summary>
public sealed record FeedbackRouteResult(
	bool Accepted,
	FeedbackRequest? Request,
	FeedbackDiagnosticSnapshot Diagnostic);

/// <summary>
/// 单帧反馈处理结果，用于证明空队列帧不会做轮询工作。
/// </summary>
public sealed record FeedbackFrameResult(
	int ProcessedOutputCount,
	bool ImmediateReturn,
	bool IteratedQueue,
	FeedbackRequest? SelectedRequest);

/// <summary>
/// 面向测试和 QA 的路由诊断快照。
/// </summary>
public sealed record FeedbackDiagnosticSnapshot(
	int Sequence,
	string EventId,
	string SourceSystem,
	string CueFamily,
	FeedbackPriority Priority,
	string CoalesceKey,
	int PriorityScore,
	FeedbackOutputDecision Decision,
	bool Coalesced,
	string? StatusText);

/// <summary>
/// 语义反馈事件中心，负责生成视觉、音频、字幕、状态与 QA 诊断请求。
/// </summary>
public sealed class FeedbackManager
{
	public const double DefaultCoalesceWindowSeconds = 0.25d;
	public const int AmbientBasePriorityScore = 10;
	public const int MinorBasePriorityScore = 30;
	public const int MajorBasePriorityScore = 60;
	public const int CriticalBasePriorityScore = 100;
	public const int MaxUrgencyBonus = 25;
	public const int MaxNoveltyBonus = 10;
	public const int MaxCooldownPenalty = 50;

	private static readonly IReadOnlyDictionary<string, FeedbackEventDefinition> SupportedEvents = BuildSupportedEvents();

	private readonly Dictionary<string, Action<Dictionary<string, object?>>?> subscriptions = new(StringComparer.Ordinal);
	private readonly List<PendingFeedback> queue = new();
	private readonly Dictionary<string, PendingFeedback> latestByCoalesceKey = new(StringComparer.Ordinal);
	private readonly HashSet<UIManager> connectedUiManagers = new();
	private readonly HashSet<Persistence> connectedPersistenceSystems = new();
	private readonly List<FeedbackDiagnosticSnapshot> diagnostics = new();
	private readonly Func<double> clockSeconds;
	private int nextSequence;

	/// <summary>使用系统单调时钟创建反馈管理器。</summary>
	public FeedbackManager()
		: this(() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency)
	{
	}

	/// <summary>使用注入时钟创建反馈管理器，便于单元测试确定合并窗口。</summary>
	public FeedbackManager(Func<double> clockSeconds)
	{
		this.clockSeconds = clockSeconds ?? throw new ArgumentNullException(nameof(clockSeconds));
	}

	/// <summary>旧版反馈事件回调；保留给 FoundationParity 和迁移期调用者。</summary>
	public event Action<string, Dictionary<string, object?>>? FeedbackTriggered;

	/// <summary>旧版 UI 事件消费回调；新代码应读取 FeedbackRequestRouted。</summary>
	public event Action<string>? UIEventConsumed;

	/// <summary>反馈请求被接受或合并时触发。</summary>
	public event Action<FeedbackRequest>? FeedbackRequestRouted;

	/// <summary>反馈请求从冲突队列中被选中输出时触发。</summary>
	public event Action<FeedbackRequest>? FeedbackOutputSelected;

	/// <summary>是否已经初始化。</summary>
	public bool IsInitialized { get; private set; }

	/// <summary>已排队但尚未输出的反馈请求快照。</summary>
	public IReadOnlyList<FeedbackRequest> PendingRequests =>
		queue.OrderBy(item => item.EnqueueSequence).Select(item => item.Request).ToArray();

	/// <summary>路由、合并、跳过与输出选择的诊断快照。</summary>
	public IReadOnlyList<FeedbackDiagnosticSnapshot> Diagnostics => diagnostics.AsReadOnly();

	/// <summary>实际处理过非空反馈队列的帧次数。</summary>
	public int FrameWorkCount { get; private set; }

	/// <summary>标记反馈系统就绪。</summary>
	public void Initialize()
	{
		IsInitialized = true;
	}

	/// <summary>订阅旧版语义事件 ID。</summary>
	public void Subscribe(string eventId, Action<Dictionary<string, object?>>? callback)
	{
		subscriptions[eventId] = callback;
	}

	/// <summary>触发旧版反馈事件并调用旧版订阅者。</summary>
	public void EmitFeedback(string eventId, Dictionary<string, object?>? parameters = null)
	{
		parameters ??= new Dictionary<string, object?>();
		FeedbackTriggered?.Invoke(eventId, parameters);

		if (subscriptions.TryGetValue(eventId, out var callback) && callback is not null)
		{
			callback(parameters);
		}
	}

	/// <summary>通知反馈系统消费一个 UI 语义事件。</summary>
	public void ConsumeUiEvent(string eventId, IReadOnlyDictionary<string, object?>? payload = null)
	{
		UIEventConsumed?.Invoke(eventId);
		RouteSemanticEvent(eventId, payload);
	}

	/// <summary>一次性连接 UIManager 的首批 #16 语义事件到 #17 路由器。</summary>
	public void ConnectUiSemanticEvents(UIManager uiManager)
	{
		ArgumentNullException.ThrowIfNull(uiManager);
		if (!connectedUiManagers.Add(uiManager))
		{
			return;
		}

		uiManager.UIPanelOpened += panelId => RouteUiPanelOpened(uiManager, panelId);
		uiManager.UIPanelClosed += panelId => RouteUiPanelClosed(uiManager, panelId);
		uiManager.UIRouteSelected += (routeId, routeName) => RouteUiRouteSelected(uiManager, routeId, routeName);
		uiManager.UIDepartureConfirmed += (routeId, departureMode) =>
			RouteUiDepartureConfirmed(uiManager, routeId, departureMode);
		uiManager.UIThreatResponseChosen += (threatId, resultId) =>
			RouteUiThreatResponseChosen(uiManager, threatId, resultId);
		uiManager.UIRepairSubmitted += (nodeId, materials) => RouteUiRepairSubmitted(uiManager, nodeId, materials);
		uiManager.UIPurchaseConfirmed += (stallId, goodId, quantity, totalCost) =>
			RouteUiPurchaseConfirmed(uiManager, stallId, goodId, quantity, totalCost);
		uiManager.UIItemTransferred += (itemId, fromPool, toPool, quantity) =>
			RouteUiItemTransferred(uiManager, itemId, fromPool, toPool, quantity);
	}

	/// <summary>一次性连接 #3 Persistence 的保存和加载完成事件到 #17 路由器。</summary>
	public void ConnectPersistenceEvents(Persistence persistence)
	{
		ArgumentNullException.ThrowIfNull(persistence);
		if (!connectedPersistenceSystems.Add(persistence))
		{
			return;
		}

		persistence.PromotionCompleted += (artifactKind, generation) =>
			RoutePersistenceSaveCompleted(artifactKind, generation);
		persistence.LoadCompleted += (artifactKind, generation) => RoutePersistenceLoadCompleted(artifactKind, generation);
	}

	/// <summary>路由稳定语义事件，并生成不可变反馈请求或不支持事件的诊断。</summary>
	public FeedbackRouteResult RouteSemanticEvent(
		string eventId,
		IReadOnlyDictionary<string, object?>? payload = null,
		double? nowSeconds = null,
		int urgencyBonus = 0,
		int noveltyBonus = 0,
		int cooldownPenalty = 0,
		string? sourceSystem = null)
	{
		if (!SupportedEvents.TryGetValue(eventId, out var definition))
		{
			var skipped = RecordDiagnostic(
				eventId,
				sourceSystem ?? string.Empty,
				"Unknown",
				FeedbackPriority.Ambient,
				eventId,
				0,
				FeedbackOutputDecision.SkippedUnsupported,
				coalesced: false,
				statusText: null);
			return new FeedbackRouteResult(false, null, skipped);
		}

		var payloadSnapshot = CopyPayload(payload);
		if (!ValidatePayload(definition, payloadSnapshot))
		{
			var invalid = RecordDiagnostic(
				eventId,
				sourceSystem ?? definition.SourceSystem,
				definition.CueFamily,
				definition.Priority,
				eventId,
				0,
				FeedbackOutputDecision.SkippedInvalidPayload,
				coalesced: false,
				statusText: null);
			return new FeedbackRouteResult(false, null, invalid);
		}

		var routeTimeSeconds = nowSeconds ?? clockSeconds();
		var request = BuildRequest(definition, payloadSnapshot, sourceSystem);
		var priorityScore = CalculatePriorityScore(request.Priority, urgencyBonus, noveltyBonus, cooldownPenalty);

		if (latestByCoalesceKey.TryGetValue(request.CoalesceKey, out var existing)
			&& routeTimeSeconds - existing.LastUpdatedAtSeconds <= DefaultCoalesceWindowSeconds)
		{
			existing.Request = request;
			existing.PriorityScore = priorityScore;
			existing.LastUpdatedAtSeconds = routeTimeSeconds;
			FeedbackRequestRouted?.Invoke(request);

			var coalescedDiagnostic = RecordDiagnostic(
				request.EventId,
				request.SourceSystem,
				request.CueFamily,
				request.Priority,
				request.CoalesceKey,
				priorityScore,
				FeedbackOutputDecision.Coalesced,
				coalesced: true,
				request.StatusText);
			return new FeedbackRouteResult(true, request, coalescedDiagnostic);
		}

		var pending = new PendingFeedback(request, priorityScore, NextSequence(), routeTimeSeconds);
		queue.Add(pending);
		latestByCoalesceKey[request.CoalesceKey] = pending;
		FeedbackRequestRouted?.Invoke(request);

		var diagnostic = RecordDiagnostic(
			request.EventId,
			request.SourceSystem,
			request.CueFamily,
			request.Priority,
			request.CoalesceKey,
			priorityScore,
			FeedbackOutputDecision.Routed,
			coalesced: false,
			request.StatusText);
		return new FeedbackRouteResult(true, request, diagnostic);
	}

	/// <summary>处理一帧反馈输出；空队列会立即返回且不增加工作计数。</summary>
	public FeedbackFrameResult ProcessFrame()
	{
		if (queue.Count == 0)
		{
			return new FeedbackFrameResult(0, ImmediateReturn: true, IteratedQueue: false, SelectedRequest: null);
		}

		FrameWorkCount++;
		var selectedIndex = SelectNextPendingIndex();
		var selected = queue[selectedIndex];
		queue.RemoveAt(selectedIndex);

		if (latestByCoalesceKey.TryGetValue(selected.Request.CoalesceKey, out var latest)
			&& ReferenceEquals(latest, selected))
		{
			latestByCoalesceKey.Remove(selected.Request.CoalesceKey);
		}

		FeedbackOutputSelected?.Invoke(selected.Request);
		RecordDiagnostic(
			selected.Request.EventId,
			selected.Request.SourceSystem,
			selected.Request.CueFamily,
			selected.Request.Priority,
			selected.Request.CoalesceKey,
			selected.PriorityScore,
			FeedbackOutputDecision.OutputSelected,
			coalesced: false,
			selected.Request.StatusText);

		return new FeedbackFrameResult(1, ImmediateReturn: false, IteratedQueue: true, selected.Request);
	}

	/// <summary>按照 GDD 公式计算反馈优先级分数。</summary>
	public static int CalculatePriorityScore(
		FeedbackPriority priority,
		int urgencyBonus = 0,
		int noveltyBonus = 0,
		int cooldownPenalty = 0)
	{
		return BasePriorityScore(priority)
			+ Math.Clamp(urgencyBonus, 0, MaxUrgencyBonus)
			+ Math.Clamp(noveltyBonus, 0, MaxNoveltyBonus)
			- Math.Clamp(cooldownPenalty, 0, MaxCooldownPenalty);
	}

	/// <summary>转接 UI 面板打开事件，不读取或修改 UI 焦点状态。</summary>
	private void RouteUiPanelOpened(UIManager uiManager, string panelId)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(UIManager.UIPanelOpenedEventId, nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIPanelOpenedEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["panel_id"] = panelId,
				["status_text"] = "feedback.panel_opened",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接 UI 面板关闭事件，不读取或修改 UI 焦点状态。</summary>
	private void RouteUiPanelClosed(UIManager uiManager, string panelId)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(UIManager.UIPanelClosedEventId, nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIPanelClosedEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["panel_id"] = panelId,
				["status_text"] = "feedback.panel_closed",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接航线选择事件，保留可见状态文本与音频字幕路径。</summary>
	private void RouteUiRouteSelected(UIManager uiManager, string routeId, string routeName)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(UIManager.UIRouteSelectedEventId, nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIRouteSelectedEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["route_id"] = routeId,
				["route_name"] = routeName,
				["coalesce_key"] = $"route:{routeId}",
				["status_text"] = "feedback.route_selected",
				["caption_text"] = "feedback.route_selected.caption",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接出航确认事件，不阻塞 UIManager 已完成的状态推进。</summary>
	private void RouteUiDepartureConfirmed(UIManager uiManager, string routeId, string departureMode)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(
			UIManager.UIDepartureConfirmedEventId,
			nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIDepartureConfirmedEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["route_id"] = routeId,
				["departure_mode"] = departureMode,
				["status_text"] = "feedback.departure_confirmed",
				["caption_text"] = "feedback.departure_confirmed.caption",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接威胁响应选择事件到 Exploration HUD 反馈族群。</summary>
	private void RouteUiThreatResponseChosen(UIManager uiManager, string threatId, string resultId)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(
			UIManager.UIThreatResponseChosenEventId,
			nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIThreatResponseChosenEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["threat_id"] = threatId,
				["result_id"] = resultId,
				["status_text"] = "feedback.threat_response",
				["caption_text"] = "feedback.threat_response.caption",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接修复提交事件到 Repair 反馈族群。</summary>
	private void RouteUiRepairSubmitted(
		UIManager uiManager,
		string nodeId,
		IReadOnlyList<MaterialSubmission> materials)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(UIManager.UIRepairSubmittedEventId, nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIRepairSubmittedEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["node_id"] = nodeId,
				["materials"] = materials,
				["status_text"] = "feedback.repair_submitted",
				["caption_text"] = "feedback.repair_submitted.caption",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接购买确认事件到 Market/Inventory 反馈族群。</summary>
	private void RouteUiPurchaseConfirmed(
		UIManager uiManager,
		string stallId,
		string goodId,
		int quantity,
		int totalCost)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(
			UIManager.UIPurchaseConfirmedEventId,
			nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIPurchaseConfirmedEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["stall_id"] = stallId,
				["good_id"] = goodId,
				["quantity"] = quantity,
				["total_cost"] = totalCost,
				["status_text"] = "feedback.purchase_confirmed",
				["caption_text"] = "feedback.purchase_confirmed.caption",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接物品转移事件到 Market/Inventory 反馈族群。</summary>
	private void RouteUiItemTransferred(
		UIManager uiManager,
		string itemId,
		string fromPool,
		string toPool,
		int quantity)
	{
		using var scope = uiManager.EnterSemanticEventConsumerScope(UIManager.UIItemTransferredEventId, nameof(FeedbackManager));
		RouteSemanticEvent(
			UIManager.UIItemTransferredEventId,
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["item_id"] = itemId,
				["from_pool"] = fromPool,
				["to_pool"] = toPool,
				["quantity"] = quantity,
				["status_text"] = "feedback.item_transferred",
				["caption_text"] = "feedback.item_transferred.caption",
			},
			sourceSystem: "UIManager");
	}

	/// <summary>转接保存提升完成事件，并保持来源为 Persistence。</summary>
	private void RoutePersistenceSaveCompleted(string artifactKind, int generation)
	{
		var normalizedArtifact = NormalizePersistenceArtifact(artifactKind);
		RouteSemanticEvent(
			"ui_save_completed",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["artifact_kind"] = normalizedArtifact,
				["generation"] = generation,
				["coalesce_key"] = $"save:{normalizedArtifact}:{generation}",
				["status_text"] = "feedback.save_completed",
				["caption_text"] = "feedback.save_completed.caption",
			},
			sourceSystem: "Persistence");
	}

	/// <summary>转接加载完成事件，并保持来源为 Persistence。</summary>
	private void RoutePersistenceLoadCompleted(string artifactKind, int generation)
	{
		var normalizedArtifact = NormalizePersistenceArtifact(artifactKind);
		RouteSemanticEvent(
			"ui_load_completed",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["artifact_kind"] = normalizedArtifact,
				["generation"] = generation,
				["coalesce_key"] = $"load:{normalizedArtifact}:{generation}",
				["status_text"] = "feedback.load_completed",
				["caption_text"] = "feedback.load_completed.caption",
			},
			sourceSystem: "Persistence");
	}

	private static string NormalizePersistenceArtifact(string artifactKind)
	{
		return string.IsNullOrWhiteSpace(artifactKind) ? "progress" : artifactKind;
	}

	/// <summary>语义事件存根：航线已选择。</summary>
	public void OnRouteSelected(string routeId, string destinationId)
	{
		EmitFeedback("route_selected");
		RouteSemanticEvent(
			"ui_route_selected",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["route_id"] = routeId,
				["destination_id"] = destinationId,
				["coalesce_key"] = $"route:{routeId}",
				["status_text"] = "feedback.route_selected",
			});
	}

	/// <summary>语义事件存根：世界修复已完成。</summary>
	public void OnRepairCompleted(string nodeId)
	{
		EmitFeedback("world_repair_completed");
	}

	/// <summary>语义事件存根：威胁已触发。</summary>
	public void OnThreatTriggered(string threatId)
	{
		EmitFeedback("threat_warning");
	}

	private static int BasePriorityScore(FeedbackPriority priority)
	{
		return priority switch
		{
			FeedbackPriority.Critical => CriticalBasePriorityScore,
			FeedbackPriority.Major => MajorBasePriorityScore,
			FeedbackPriority.Minor => MinorBasePriorityScore,
			_ => AmbientBasePriorityScore,
		};
	}

	private static IReadOnlyDictionary<string, object?> CopyPayload(IReadOnlyDictionary<string, object?>? payload)
	{
		var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
		if (payload is not null)
		{
			foreach (var (key, value) in payload)
			{
				copy[key] = value;
			}
		}

		return new ReadOnlyDictionary<string, object?>(copy);
	}

	private static FeedbackRequest BuildRequest(
		FeedbackEventDefinition definition,
		IReadOnlyDictionary<string, object?> payload,
		string? sourceSystem)
	{
		return new FeedbackRequest(
			definition.EventId,
			sourceSystem ?? definition.SourceSystem,
			definition.CueFamily,
			definition.Priority,
			FindString(payload, "coalesce_key") ?? BuildDefaultCoalesceKey(definition.EventId, payload),
			FindString(payload, "visual_cue_id") ?? definition.VisualCueId,
			FindString(payload, "audio_cue_id") ?? definition.AudioCueId,
			FindString(payload, "caption_text") ?? definition.CaptionText,
			FindString(payload, "status_text") ?? definition.StatusText,
			payload);
	}

	private static bool ValidatePayload(FeedbackEventDefinition definition, IReadOnlyDictionary<string, object?> payload)
	{
		foreach (var requirement in definition.RequiredPayload)
		{
			if (!payload.TryGetValue(requirement.Key, out var value) || !requirement.Accepts(value))
			{
				return false;
			}
		}

		return true;
	}

	private static string BuildDefaultCoalesceKey(string eventId, IReadOnlyDictionary<string, object?> payload)
	{
		return eventId switch
		{
			"ui_panel_opened" or "ui_panel_closed" => WithPayloadKey(eventId, payload, "panel_id"),
			"ui_route_selected" or "ui_departure_confirmed" => WithPayloadKey(eventId, payload, "route_id"),
			"ui_threat_response_chosen" => WithPayloadKey(eventId, payload, "threat_id"),
			"ui_repair_submitted" => WithPayloadKey(eventId, payload, "node_id"),
			"ui_purchase_confirmed" => WithPayloadKey(eventId, payload, "good_id"),
			"ui_item_transferred" => WithPayloadKey(eventId, payload, "item_id"),
			_ => eventId,
		};
	}

	private static string WithPayloadKey(string eventId, IReadOnlyDictionary<string, object?> payload, string key)
	{
		var value = FindString(payload, key);
		return string.IsNullOrWhiteSpace(value) ? eventId : $"{eventId}:{value}";
	}

	private static string? FindString(IReadOnlyDictionary<string, object?> payload, string key)
	{
		if (!payload.TryGetValue(key, out var value) || value is null)
		{
			return null;
		}

		return Convert.ToString(value);
	}

	private int SelectNextPendingIndex()
	{
		var selectedIndex = 0;
		for (var index = 1; index < queue.Count; index++)
		{
			var candidate = queue[index];
			var selected = queue[selectedIndex];
			if (candidate.PriorityScore > selected.PriorityScore
				|| (candidate.PriorityScore == selected.PriorityScore
					&& candidate.EnqueueSequence < selected.EnqueueSequence))
			{
				selectedIndex = index;
			}
		}

		return selectedIndex;
	}

	private FeedbackDiagnosticSnapshot RecordDiagnostic(
		string eventId,
		string sourceSystem,
		string cueFamily,
		FeedbackPriority priority,
		string coalesceKey,
		int priorityScore,
		FeedbackOutputDecision decision,
		bool coalesced,
		string? statusText)
	{
		var diagnostic = new FeedbackDiagnosticSnapshot(
			NextSequence(),
			eventId,
			sourceSystem,
			cueFamily,
			priority,
			coalesceKey,
			priorityScore,
			decision,
			coalesced,
			statusText);
		diagnostics.Add(diagnostic);
		return diagnostic;
	}

	private int NextSequence()
	{
		nextSequence++;
		return nextSequence;
	}

	private static IReadOnlyDictionary<string, FeedbackEventDefinition> BuildSupportedEvents()
	{
		return new Dictionary<string, FeedbackEventDefinition>(StringComparer.Ordinal)
		{
			["ui_panel_opened"] = new(
				"ui_panel_opened",
				"ui-hud-chart-interface",
				"UI",
				FeedbackPriority.Minor,
				"visual.panel_context",
				null,
				null,
				"feedback.panel_opened",
				RequiredString("panel_id")),
			["ui_panel_closed"] = new(
				"ui_panel_closed",
				"ui-hud-chart-interface",
				"UI",
				FeedbackPriority.Minor,
				"visual.panel_return",
				null,
				null,
				"feedback.panel_closed",
				RequiredString("panel_id")),
			["ui_route_selected"] = new(
				"ui_route_selected",
				"ui-hud-chart-interface",
				"Chart",
				FeedbackPriority.Minor,
				"visual.chart.route_selected",
				"audio.chart.route_selected",
				"feedback.route_selected.caption",
				"feedback.route_selected",
				RequiredString("route_id")),
			["ui_departure_confirmed"] = new(
				"ui_departure_confirmed",
				"ui-hud-chart-interface",
				"Chart",
				FeedbackPriority.Critical,
				"visual.chart.departure_confirmed",
				"audio.chart.departure_confirmed",
				"feedback.departure_confirmed.caption",
				"feedback.departure_confirmed",
				RequiredString("route_id")),
			["ui_threat_response_chosen"] = new(
				"ui_threat_response_chosen",
				"ui-hud-chart-interface",
				"Exploration HUD",
				FeedbackPriority.Major,
				"visual.threat.response",
				"audio.threat.response",
				"feedback.threat_response.caption",
				"feedback.threat_response",
				RequiredString("threat_id"),
				RequiredString("result_id")),
			["ui_repair_submitted"] = new(
				"ui_repair_submitted",
				"ui-hud-chart-interface",
				"Repair",
				FeedbackPriority.Major,
				"visual.repair.submitted",
				"audio.repair.submitted",
				"feedback.repair_submitted.caption",
				"feedback.repair_submitted",
				RequiredString("node_id"),
				RequiredMaterialSubmissions("materials")),
			["ui_purchase_confirmed"] = new(
				"ui_purchase_confirmed",
				"ui-hud-chart-interface",
				"Market/Inventory",
				FeedbackPriority.Major,
				"visual.market.purchase",
				"audio.market.purchase",
				"feedback.purchase_confirmed.caption",
				"feedback.purchase_confirmed",
				RequiredString("stall_id"),
				RequiredString("good_id"),
				RequiredInt("quantity"),
				RequiredInt("total_cost")),
			["ui_item_transferred"] = new(
				"ui_item_transferred",
				"ui-hud-chart-interface",
				"Market/Inventory",
				FeedbackPriority.Minor,
				"visual.inventory.transfer",
				"audio.inventory.transfer",
				"feedback.item_transferred.caption",
				"feedback.item_transferred",
				RequiredString("item_id"),
				RequiredString("from_pool"),
				RequiredString("to_pool"),
				RequiredInt("quantity")),
			["ui_save_completed"] = new(
				"ui_save_completed",
				"session-persistence",
				"Session",
				FeedbackPriority.Minor,
				"visual.session.save",
				"audio.session.save",
				"feedback.save_completed.caption",
				"feedback.save_completed"),
			["ui_load_completed"] = new(
				"ui_load_completed",
				"session-persistence",
				"Session",
				FeedbackPriority.Minor,
				"visual.session.load",
				"audio.session.load",
				"feedback.load_completed.caption",
				"feedback.load_completed"),
		};
	}

	private static FeedbackPayloadRequirement RequiredString(string key)
	{
		return new FeedbackPayloadRequirement(
			key,
			value => value is string text && !string.IsNullOrWhiteSpace(text));
	}

	private static FeedbackPayloadRequirement RequiredInt(string key)
	{
		return new FeedbackPayloadRequirement(key, value => value is int);
	}

	private static FeedbackPayloadRequirement RequiredMaterialSubmissions(string key)
	{
		return new FeedbackPayloadRequirement(
			key,
			value => value is IReadOnlyList<MaterialSubmission>);
	}

	private sealed record FeedbackEventDefinition(
		string EventId,
		string SourceSystem,
		string CueFamily,
		FeedbackPriority Priority,
		string? VisualCueId,
		string? AudioCueId,
		string? CaptionText,
		string? StatusText,
		params FeedbackPayloadRequirement[] RequiredPayload);

	private sealed record FeedbackPayloadRequirement(string Key, Func<object?, bool> Accepts);

	private sealed class PendingFeedback
	{
		public PendingFeedback(
			FeedbackRequest request,
			int priorityScore,
			int enqueueSequence,
			double lastUpdatedAtSeconds)
		{
			Request = request;
			PriorityScore = priorityScore;
			EnqueueSequence = enqueueSequence;
			LastUpdatedAtSeconds = lastUpdatedAtSeconds;
		}

		public FeedbackRequest Request { get; set; }

		public int PriorityScore { get; set; }

		public int EnqueueSequence { get; }

		public double LastUpdatedAtSeconds { get; set; }
	}
}
