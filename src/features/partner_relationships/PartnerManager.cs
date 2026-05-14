using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Feature;

/// <summary>
/// 天猫伙伴的运行时状态。数值与 ADR-0015 的 6 态表保持稳定。
/// </summary>
public enum PartnerCatState
{
	SleepingOnIntelStation = 0,
	IdleLivingQuarters = 1,
	FollowingPlayerToBench = 2,
	BenchAdjacent = 3,
	Sniffing = 4,
	InNest = 5,
}

/// <summary>
/// 伙伴嗅辨结果与 Feedback 钩子使用的稳定反应 ID。
/// </summary>
public enum ScoutSniffReaction
{
	EarsBackTailPoint = 0,
	CirclesTwice = 1,
	RubsFace = 2,
	Confused = 3,
	AlreadySmelled = 4,
}

/// <summary>
/// 天猫命名流程的三段式持久化状态。
/// </summary>
public enum PartnerNamingState
{
	Pending = 0,
	Prompted = 1,
	Completed = 2,
}

/// <summary>
/// 生活舱小窝痕迹的四阶段持久化状态。
/// </summary>
public enum PartnerNestState
{
	Empty = 0,
	First = 1,
	Accumulating = 2,
	Full = 3,
}

/// <summary>
/// <see cref="PartnerManager.ScoutSniff"/> 返回的结果契约。
/// </summary>
/// <param name="Success">嗅辨是否成功完成。</param>
/// <param name="ReactionId">选定的反应 ID；状态门控失败时为 -1。</param>
/// <param name="Error">失败原因；成功时为空字符串。</param>
public sealed record ScoutSniffResult(bool Success, int ReactionId, string Error);

/// <summary>
/// <see cref="PartnerManager.SubmitPartnerName"/> 的命名提交结果。
/// </summary>
/// <param name="Accepted">提交是否被接受。</param>
/// <param name="Error">拒绝原因；接受时为空字符串。</param>
public sealed record PartnerNameSubmissionResult(bool Accepted, string Error);

/// <summary>
/// 从内容 Registry 读取并解析后的猫嗅辨静态签名。
/// </summary>
/// <param name="RevealTarget">要写入传闻的地点或路线 ID。</param>
/// <param name="HazardHint">随传闻附带的风险提示标签。</param>
/// <param name="Confidence">原始置信度；调用 Intel 前会被截断。</param>
/// <param name="PatternId">成功嗅辨后要上报的规律 ID。</param>
public sealed record ScoutSniffSignature(
	string RevealTarget,
	string HazardHint,
	int Confidence,
	string PatternId);

/// <summary>
/// 静态内容定义的只读查询边界。
/// </summary>
public interface IPartnerContentLookup
{
	/// <summary>返回指定物品 ID 的静态定义；不可用时返回 null。</summary>
	IReadOnlyDictionary<string, object?>? QueryEntity(string itemId);
}

/// <summary>
/// 玩家当前背包物品 ID 的只读查询边界。
/// </summary>
public interface IPartnerInventorySource
{
	/// <summary>返回伙伴系统可检查的背包物品 ID 列表。</summary>
	IReadOnlyList<string> GetInventoryItems();
}

/// <summary>
/// PartnerManager 在 feature_ready 阶段读取自己持久化快照的边界。
/// </summary>
public interface IPartnerSnapshotStore
{
	/// <summary>返回指定 domain 的持久化载荷；新游戏或不可用时返回空字典。</summary>
	IReadOnlyDictionary<string, object?> RestoreSnapshot(string domainId);
}

/// <summary>
/// PartnerManager 在状态变更边界请求持久化捕获的写入边界。
/// </summary>
public interface IPartnerSnapshotSink
{
	/// <summary>捕获指定 domain 的领域载荷；具体落盘由 Persistence 系统拥有。</summary>
	void CaptureSnapshot(string domainId, IReadOnlyDictionary<string, object?> payload);
}

/// <summary>
/// Hub #7 提供给 PartnerManager 的事件与当前状态查询边界。
/// </summary>
public interface IPartnerHubSignalSource
{
	/// <summary>当前 Hub 停泊状态。</summary>
	HubDockingState CurrentHubState { get; }

	/// <summary>Hub 停泊状态变化事件。</summary>
	event Action<HubDockingState>? HubStateChanged;

	/// <summary>玩家完成归港流程事件。</summary>
	event Action? PlayerReturnedToHub;

	/// <summary>玩家进入 Hub 区域事件。</summary>
	event Action<string>? PlayerEnteredZone;

	/// <summary>返回玩家当前是否处于指定 Hub 区域。</summary>
	bool IsPlayerInZone(string zoneId);
}

/// <summary>
/// BootstrapSequencer 的最小排队边界，确保跨系统加入通知在系统就绪后分发。
/// </summary>
public interface IPartnerBootstrapSequencer
{
	/// <summary>排队一个在 bootstrap 就绪后执行的调用。</summary>
	void QueueCall(Action callback);
}

/// <summary>
/// PartnerManager 写入 Intel 系统的单向边界。
/// </summary>
public interface IPartnerIntelSink
{
	/// <summary>写入伙伴嗅辨发现的路线或地点传闻。</summary>
	void RevealRumor(string revealTarget, string sourceTag, IReadOnlyList<string> hazardTags, int confidence);

	/// <summary>上报嗅辨签名关联规律的观测事件。</summary>
	void ReportObservationEvent(string patternId, string eventId);

	/// <summary>通知 Intel 系统 MVP 伙伴已在新游戏中加入。</summary>
	void OnPartnerJoined(string partnerId);
}

/// <summary>
/// MVP 伙伴的最小状态记录。后续 story 会在同一对象上扩展命名、嗅辨与小窝字段。
/// </summary>
public sealed class PartnerState
{
	private readonly List<string> _sniffedItems = new();
	private readonly List<int> _nestItems = new();

	/// <summary>创建一个固定 ID 的伙伴状态。</summary>
	public PartnerState(string partnerId)
	{
		PartnerId = partnerId;
	}

	/// <summary>伙伴稳定 ID。</summary>
	public string PartnerId { get; }

	/// <summary>玩家为伙伴取的一次性名字；未命名前为空字符串。</summary>
	public string Name { get; private set; } = string.Empty;

	/// <summary>命名是否已经进入终态；为 true 后不可改名。</summary>
	public bool NamingDone { get; private set; }

	/// <summary>玩家已经跳过命名提示的次数。</summary>
	public int NamingSkipCount { get; private set; }

	/// <summary>当前命名状态。</summary>
	public PartnerNamingState NamingState { get; private set; } = PartnerNamingState.Pending;

	/// <summary>已经完成嗅辨的物品 ID 集合，按首次成功嗅辨顺序保存。</summary>
	public IReadOnlyList<string> SniffedItems => _sniffedItems;

	/// <summary>生命周期内是否至少发生过一次成功嗅辨。</summary>
	public bool SniffSuccessOccurred { get; private set; }

	/// <summary>按静态清单索引顺序累积的小窝物件。</summary>
	public IReadOnlyList<int> NestItems => _nestItems.ToArray();

	/// <summary>当前小窝痕迹阶段。</summary>
	public PartnerNestState NestState { get; private set; } = PartnerNestState.Empty;

	internal bool HasSniffed(string itemId)
	{
		return _sniffedItems.Contains(itemId, StringComparer.Ordinal);
	}

	internal void MarkSniffed(string itemId)
	{
		if (!HasSniffed(itemId))
		{
			_sniffedItems.Add(itemId);
		}

		SniffSuccessOccurred = true;
	}

	internal bool IsNamingEligible(int namingSkipMax)
	{
		return NamingState == PartnerNamingState.Pending
			&& SniffSuccessOccurred
			&& NamingSkipCount < namingSkipMax;
	}

	internal void MarkNamingPrompted()
	{
		NamingState = PartnerNamingState.Prompted;
	}

	internal void CompleteNaming(string name)
	{
		Name = name;
		NamingDone = true;
		NamingState = PartnerNamingState.Completed;
	}

	internal void ReturnNamingToPending()
	{
		NamingState = PartnerNamingState.Pending;
	}

	internal void IncrementNamingSkip(int namingSkipMax)
	{
		NamingSkipCount = Math.Min(namingSkipMax, NamingSkipCount + 1);
	}

	internal bool TryAccumulateNestItem(int nestCapacity, out PartnerNestState oldState, out PartnerNestState newState)
	{
		oldState = NestState;
		newState = NestState;
		if (_nestItems.Count >= nestCapacity)
		{
			return false;
		}

		_nestItems.Add(_nestItems.Count);
		newState = DeriveNestState(_nestItems.Count);
		NestState = newState;
		return true;
	}

	internal static PartnerState FromPersistentState(
		string partnerId,
		string name,
		bool namingDone,
		int namingSkipCount,
		bool sniffSuccessOccurred,
		IReadOnlyList<string> sniffedItems,
		IReadOnlyList<int> nestItems,
		int namingSkipMax)
	{
		var state = new PartnerState(partnerId)
		{
			Name = name,
			NamingDone = namingDone,
			NamingSkipCount = Math.Clamp(namingSkipCount, 0, namingSkipMax),
			SniffSuccessOccurred = sniffSuccessOccurred,
		};

		state.NamingState = state.NamingDone
			? PartnerNamingState.Completed
			: PartnerNamingState.Pending;

		foreach (var itemId in sniffedItems)
		{
			if (!string.IsNullOrWhiteSpace(itemId) && !state.HasSniffed(itemId))
			{
				state._sniffedItems.Add(itemId);
			}
		}

		if (state.SniffSuccessOccurred && state._sniffedItems.Count == 0)
		{
			state.SniffSuccessOccurred = false;
		}

		foreach (var nestItem in nestItems)
		{
			state._nestItems.Add(nestItem);
		}

		state.NestState = DeriveNestState(state._nestItems.Count);
		if (!state.NamingDone && state.NamingSkipCount >= namingSkipMax)
		{
			state.CompleteNaming(PartnerManager.DefaultPartnerName);
		}

		return state;
	}

	internal void ClearSniffState()
	{
		_sniffedItems.Clear();
		SniffSuccessOccurred = false;
	}

	private static PartnerNestState DeriveNestState(int nestItemCount)
	{
		return nestItemCount switch
		{
			0 => PartnerNestState.Empty,
			1 => PartnerNestState.First,
			2 or 3 => PartnerNestState.Accumulating,
			_ => PartnerNestState.Full,
		};
	}
}

/// <summary>
/// PartnerManager Autoload #15 的 C# 领域逻辑，拥有 MVP 唯一天猫的存在性契约与运行时状态机。
/// </summary>
public sealed class PartnerManager
{
	/// <summary>MVP 唯一伙伴 ID。</summary>
	public const string MvpPartnerId = "partner.sky-cat";

	/// <summary>ADR-0003 持久化 domain ID。</summary>
	public const string PartnerSnapshotDomainId = "progress.partner_skycat";

	/// <summary>边界区域防抖秒数。</summary>
	public const double CatStateCooldownSeconds = 0.5d;

	/// <summary>生活舱闲置后进入小窝的秒数。</summary>
	public const double NestSettleSeconds = 20.0d;

	/// <summary>成功嗅辨后猫处于 SNIFFING 状态的锁定秒数。</summary>
	public const double SniffLockoutSeconds = 2.5d;

	/// <summary>伙伴嗅辨传给 Intel 的最大置信度，永不达到权威阈值 67。</summary>
	public const int MvpConfidenceMax = 66;

	/// <summary>玩家可跳过命名的最大次数，达到后立即锁定默认名。</summary>
	public const int NamingSkipMax = 3;

	/// <summary>伙伴名字的系统级安全长度上限。</summary>
	public const int PartnerNameLengthMax = 8;

	/// <summary>小窝可累积物件数量上限。</summary>
	public const int NestCapacity = 4;

	/// <summary>三次跳过后锁定的默认名。</summary>
	public const string DefaultPartnerName = "那只猫";

	/// <summary>伙伴嗅辨成功事件 ID。</summary>
	public const string PartnerSniffSuccessEventId = "partner_sniff_success";

	private static readonly IReadOnlyList<string> NestItemNamesSource =
	[
		"旧船帆碎布",
		"锈蚀的测风链环",
		"玩家绳头",
		"空港徽章残片",
	];

	private readonly Dictionary<string, PartnerState> _partners = new(StringComparer.Ordinal);
	private readonly List<string> _warnings = new();
	private readonly IPartnerContentLookup _contentLookup;
	private readonly IPartnerInventorySource _inventorySource;
	private readonly IPartnerIntelSink _intelSink;
	private readonly IPartnerSnapshotStore _snapshotStore;
	private readonly IPartnerSnapshotSink _snapshotSink;
	private readonly IPartnerHubSignalSource _hubSignalSource;
	private readonly IPartnerBootstrapSequencer _bootstrapSequencer;
	private double _catStateCooldownRemaining;
	private double _sniffLockoutRemaining;
	private PartnerCatState _preSniffState = PartnerCatState.IdleLivingQuarters;
	private bool _stateFrozen;
	private bool _catRendered = true;
	private bool _catInteractable = true;
	private bool _hubEventsSubscribed;
	private bool _partnerJoinedQueuedThisSession;

	/// <summary>创建伙伴系统并注册 MVP 唯一伙伴。</summary>
	public PartnerManager(
		IPartnerContentLookup? contentLookup = null,
		IPartnerInventorySource? inventorySource = null,
		IPartnerIntelSink? intelSink = null,
		IPartnerSnapshotStore? snapshotStore = null,
		IPartnerHubSignalSource? hubSignalSource = null,
		IPartnerBootstrapSequencer? bootstrapSequencer = null,
		IPartnerSnapshotSink? snapshotSink = null)
	{
		_contentLookup = contentLookup ?? EmptyPartnerContentLookup.Instance;
		_inventorySource = inventorySource ?? EmptyPartnerInventorySource.Instance;
		_intelSink = intelSink ?? NoOpPartnerIntelSink.Instance;
		_snapshotStore = snapshotStore ?? EmptyPartnerSnapshotStore.Instance;
		_snapshotSink = snapshotSink ?? NoOpPartnerSnapshotSink.Instance;
		_hubSignalSource = hubSignalSource ?? EmptyPartnerHubSignalSource.Instance;
		_bootstrapSequencer = bootstrapSequencer ?? ImmediatePartnerBootstrapSequencer.Instance;
		ResetPartners();
	}

	/// <summary>猫状态变更后发出，供后续 Feedback/UI 包装层消费。</summary>
	public event Action<PartnerCatState, PartnerCatState>? CatStateChanged;

	/// <summary>嗅辨反应被选定后发出，供后续 Feedback 系统播放动画。</summary>
	public event Action<int, string>? SniffReactionTriggered;

	/// <summary>命名提示打开时发出，供 UI 层显示命名模态。</summary>
	public event Action? NamingPromptTriggered;

	/// <summary>命名进入终态后发出，参数为最终名字。</summary>
	public event Action<string>? NamingCompleted;

	/// <summary>小窝阶段变化后发出，供 Hub/Feedback 层刷新痕迹锚点。</summary>
	public event Action<PartnerNestState, PartnerNestState>? NestStateChanged;

	/// <summary>当前猫运行时状态。该字段是瞬态状态，不进入持久化快照。</summary>
	public PartnerCatState CatState { get; private set; } = PartnerCatState.SleepingOnIntelStation;

	/// <summary>只读伙伴字典，MVP 中只允许包含 partner.sky-cat。</summary>
	public IReadOnlyDictionary<string, PartnerState> Partners => _partners;

	/// <summary>当前防抖剩余时间，供测试与调试面板读取。</summary>
	public double CatStateCooldownRemaining => _catStateCooldownRemaining;

	/// <summary>成功嗅辨后 SNIFFING 状态的剩余锁定时间。</summary>
	public double SniffLockoutRemaining => _sniffLockoutRemaining;

	/// <summary>猫状态是否因 Hub 处于 departure_locked 或 in_transit 而冻结。</summary>
	public bool IsStateFrozen => _stateFrozen;

	/// <summary>猫当前是否应被渲染。in_transit 简化模拟时为 false。</summary>
	public bool IsCatRendered => _catRendered;

	/// <summary>猫当前是否可交互。departure_locked 与 in_transit 期间为 false。</summary>
	public bool IsCatInteractable => _catInteractable;

	/// <summary>非致命集成失败记录，例如 Intel/Resources 不可用。</summary>
	public IReadOnlyList<string> Warnings => _warnings;

	/// <summary>小窝物件静态清单，索引顺序不可变。</summary>
	public static IReadOnlyList<string> NestItemNames => NestItemNamesSource;

	/// <summary>
	/// 在 feature_ready 阶段初始化伙伴系统，并从当前 Hub 状态派生猫的瞬态状态。
	/// </summary>
	public void Initialize(HubDockingState currentHubState = HubDockingState.Landed)
	{
		InitNewGameState(currentHubState);
	}

	/// <summary>
	/// 初始化新游戏的伙伴持久化字段，并从 Hub 状态重新派生瞬态猫状态。
	/// </summary>
	public void InitNewGameState(HubDockingState currentHubState = HubDockingState.Landed)
	{
		ResetPartners();
		_partnerJoinedQueuedThisSession = false;
		DeriveTransientState(currentHubState);
	}

	/// <summary>
	/// 处理 feature_ready：恢复快照、派生瞬态猫状态、订阅 Hub 事件、同步当前 Hub 状态并为新游戏排队伙伴加入通知。
	/// </summary>
	public bool OnFeatureReady()
	{
		var snapshot = _snapshotStore.RestoreSnapshot(PartnerSnapshotDomainId);
		var isNewGame = snapshot.Count == 0;
		if (isNewGame)
		{
			ResetPartners();
		}
		else
		{
			RestoreFromProgressPartner(snapshot);
		}

		DeriveTransientState(_hubSignalSource.CurrentHubState);
		SubscribeHubEvents();
		SyncWithHubState(
			_hubSignalSource.CurrentHubState,
			_hubSignalSource.IsPlayerInZone(HubIds.LivingQuarters));

		if (isNewGame && !_partnerJoinedQueuedThisSession)
		{
			_partnerJoinedQueuedThisSession = true;
			_bootstrapSequencer.QueueCall(DispatchOnPartnerJoined);
		}

		return isNewGame;
	}

	/// <summary>
	/// 查询伙伴是否存在。R2 是硬约束：猫永远在飞艇上，没有 absent 分支。
	/// </summary>
	public bool QueryPartnerPresent()
	{
		return true;
	}

	/// <summary>
	/// 返回玩家已确认的伙伴名字；未命名时返回空字符串，由 UI 层提供灰白猫文案。
	/// </summary>
	public string QueryPartnerName()
	{
		var partner = _partners[MvpPartnerId];
		return partner.NamingDone ? partner.Name : string.Empty;
	}

	/// <summary>
	/// 返回当前小窝阶段，供 Hub 痕迹锚点按整数层级读取。
	/// </summary>
	public int QueryNestState()
	{
		return (int)_partners[MvpPartnerId].NestState;
	}

	/// <summary>
	/// 返回已累积小窝物件索引的只读副本。
	/// </summary>
	public IReadOnlyList<int> QueryNestItems()
	{
		return _partners[MvpPartnerId].NestItems.ToArray();
	}

	/// <summary>
	/// 执行 F.2 命名资格判定。调用上下文负责保证这是归港事件。
	/// </summary>
	public bool IsNamingEligible()
	{
		return _partners[MvpPartnerId].IsNamingEligible(NamingSkipMax);
	}

	/// <summary>
	/// 处理玩家归港后的命名提示机会；只有首次成功嗅辨后才会打开命名。
	/// </summary>
	public bool OnPlayerReturnedToHub()
	{
		if (!IsNamingEligible())
		{
			return false;
		}

		_partners[MvpPartnerId].MarkNamingPrompted();
		NamingPromptTriggered?.Invoke();
		return true;
	}

	/// <summary>
	/// 提交伙伴名字。命名完成后不可再次改名。
	/// </summary>
	public PartnerNameSubmissionResult SubmitPartnerName(string? submittedName)
	{
		var partner = _partners[MvpPartnerId];
		if (partner.NamingDone || partner.NamingState == PartnerNamingState.Completed)
		{
			return new PartnerNameSubmissionResult(false, "naming_completed");
		}

		var trimmed = (submittedName ?? string.Empty).Trim();
		if (trimmed.Length == 0)
		{
			return new PartnerNameSubmissionResult(false, "name_empty");
		}

		if (partner.NamingState != PartnerNamingState.Prompted || !partner.SniffSuccessOccurred)
		{
			return new PartnerNameSubmissionResult(false, "naming_not_prompted");
		}

		if (trimmed.Length > PartnerNameLengthMax)
		{
			trimmed = trimmed[..PartnerNameLengthMax];
		}

		partner.CompleteNaming(trimmed);
		NamingCompleted?.Invoke(trimmed);
		TriggerSnapshot();
		return new PartnerNameSubmissionResult(true, string.Empty);
	}

	/// <summary>
	/// 跳过当前命名提示；第三次跳过会立即锁定默认名。
	/// </summary>
	public bool SkipNaming()
	{
		var partner = _partners[MvpPartnerId];
		if (partner.NamingDone || partner.NamingState != PartnerNamingState.Prompted)
		{
			return false;
		}

		partner.IncrementNamingSkip(NamingSkipMax);
		if (partner.NamingSkipCount >= NamingSkipMax)
		{
			partner.CompleteNaming(DefaultPartnerName);
			NamingCompleted?.Invoke(DefaultPartnerName);
			TriggerSnapshot();
			return true;
		}

		partner.ReturnNamingToPending();
		TriggerSnapshot();
		return true;
	}

	/// <summary>
	/// 构建 progress.partner_skycat 的领域载荷。
	/// </summary>
	public Dictionary<string, object?> BuildProgressPartnerSnapshot()
	{
		var partner = _partners[MvpPartnerId];
		return new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["domain_id"] = "partner_skycat",
			["name"] = partner.Name,
			["naming_done"] = partner.NamingDone,
			["naming_skip_count"] = partner.NamingSkipCount,
			["sniff_success_occurred"] = partner.SniffSuccessOccurred,
			["nest_state"] = (int)partner.NestState,
			["nest_items"] = partner.NestItems.Cast<object?>().ToList(),
			["sniffed_items"] = partner.SniffedItems.Cast<object?>().ToList(),
		};
	}

	/// <summary>
	/// 构建 ADR-0003 使用的 progress.partner_skycat SnapshotPackage。
	/// </summary>
	public SnapshotPackage BuildSnapshotPackage()
	{
		var package = new SnapshotPackage
		{
			DomainId = PartnerSnapshotDomainId,
			SnapshotSchemaVersion = 1,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["partner-relationships"] = "2026-05-09";
		package.StableIdRefs.Add(MvpPartnerId);
		foreach (var itemId in _partners[MvpPartnerId].SniffedItems)
		{
			package.StableIdRefs.Add(itemId);
		}

		foreach (var (key, value) in BuildProgressPartnerSnapshot())
		{
			package.Payload[key] = value;
		}

		return package;
	}

	/// <summary>
	/// 将伙伴系统注册到 Persistence 的 progress 领域序列化边界。
	/// </summary>
	public void RegisterPersistence(Persistence persistence)
	{
		persistence.RegisterDomainSerializer(PartnerSnapshotDomainId, BuildSnapshotPackage);
		persistence.RegisterDomainDeserializer(PartnerSnapshotDomainId, package => TryRestoreFromSnapshotPackage(package));
	}

	/// <summary>
	/// 从 ADR-0003 SnapshotPackage 恢复伙伴持久化状态。
	/// </summary>
	public bool TryRestoreFromSnapshotPackage(SnapshotPackage package)
	{
		return package.DomainId == PartnerSnapshotDomainId
			&& RestoreFromProgressPartner(package.Payload);
	}

	/// <summary>
	/// 从 progress.partner_skycat 领域载荷恢复持久化状态，并重新派生瞬态字段。
	/// </summary>
	public bool RestoreFromProgressPartner(IReadOnlyDictionary<string, object?> snapshot)
	{
		if (snapshot.Count == 0)
		{
			return false;
		}

		var sniffSuccessOccurred = ReadBool(snapshot, "sniff_success_occurred");
		var sniffedItems = ReadStringList(snapshot, "sniffed_items");
		var nestItems = ReadIntList(snapshot, "nest_items");
		var savedNestState = (PartnerNestState)ReadInt(snapshot, "nest_state", (int)PartnerNestState.Empty);
		var derivedNestState = DeriveNestState(nestItems.Count);
		if (sniffSuccessOccurred && sniffedItems.Count == 0)
		{
			AddWarning("partner_sniff_success_without_items_corrected");
		}

		if (!NestItemsFollowStaticManifest(nestItems))
		{
			AddWarning("partner_nest_items_order_gap_preserved");
		}

		if (savedNestState != derivedNestState)
		{
			AddWarning($"partner_nest_state_mismatch_corrected:{(int)savedNestState}:{(int)derivedNestState}");
		}

		_partners[MvpPartnerId] = PartnerState.FromPersistentState(
			MvpPartnerId,
			ReadString(snapshot, "name"),
			ReadBool(snapshot, "naming_done"),
			ReadInt(snapshot, "naming_skip_count", 0),
			sniffSuccessOccurred,
			sniffedItems,
			nestItems,
			NamingSkipMax);
		DeriveTransientState(_hubSignalSource.CurrentHubState);
		return true;
	}

	/// <summary>
	/// 执行 ADR-0015 与 Story 002 定义的六步伙伴嗅辨算法。
	/// </summary>
	public ScoutSniffResult ScoutSniff(string itemId)
	{
		if (CatState == PartnerCatState.Sniffing)
		{
			return new ScoutSniffResult(false, -1, "cat_busy");
		}

		var partner = _partners[MvpPartnerId];
		if (partner.HasSniffed(itemId))
		{
			return new ScoutSniffResult(false, (int)ScoutSniffReaction.AlreadySmelled, "already_sniffed");
		}

		var signature = GetSniffSignature(itemId);
		if (signature is null)
		{
			return new ScoutSniffResult(false, (int)ScoutSniffReaction.Confused, "no_signature");
		}

		if (string.IsNullOrWhiteSpace(signature.RevealTarget))
		{
			return new ScoutSniffResult(false, (int)ScoutSniffReaction.Confused, "empty_reveal_target");
		}

		var confidence = ClampConfidence(signature.Confidence);
		SafeRevealRumor(signature.RevealTarget, signature.HazardHint, confidence);

		if (!string.IsNullOrWhiteSpace(signature.PatternId))
		{
			SafeReportObservationEvent(signature.PatternId);
		}

		partner.MarkSniffed(itemId);
		AccumulateNestItem();

		var reaction = confidence >= 50
			? ScoutSniffReaction.CirclesTwice
			: ScoutSniffReaction.RubsFace;

		_preSniffState = CatState;
		ChangeCatState(PartnerCatState.Sniffing, TransitionMode.Force);
		_sniffLockoutRemaining = SniffLockoutSeconds;
		SniffReactionTriggered?.Invoke((int)reaction, itemId);

		TriggerSnapshot();
		return new ScoutSniffResult(true, (int)reaction, string.Empty);
	}

	/// <summary>
	/// 返回背包中在内容 Registry 内具有非空猫嗅辨签名的物品。
	/// </summary>
	public IReadOnlyList<string> GetSniffableItems()
	{
		try
		{
			return _inventorySource.GetInventoryItems()
				.Where(itemId => GetSniffSignature(itemId) is not null)
				.ToArray();
		}
		catch (Exception ex)
		{
			AddWarning($"resources_inventory_unavailable:{ex.GetType().Name}");
			return Array.Empty<string>();
		}
	}

	/// <summary>
	/// 执行 F.1 置信度截断；该规则固定为 min(raw, 66)，不会触及权威阈值。
	/// </summary>
	public static int ClampConfidence(int rawConfidence)
	{
		return Math.Min(rawConfidence, MvpConfidenceMax);
	}

	/// <summary>
	/// 接收 Hub 状态变化并应用 story 001 的猫状态耦合规则。
	/// </summary>
	public void OnHubStateChanged(HubDockingState newState)
	{
		switch (newState)
		{
			case HubDockingState.Landed:
				_stateFrozen = false;
				_catRendered = true;
				_catInteractable = true;
				break;
			case HubDockingState.DepartureLocked:
				_stateFrozen = true;
				_catRendered = true;
				_catInteractable = false;
				break;
			case HubDockingState.InTransit:
				_stateFrozen = true;
				_catRendered = false;
				_catInteractable = false;
				ChangeCatState(PartnerCatState.IdleLivingQuarters, TransitionMode.Force);
				break;
			case HubDockingState.Arrival:
				_stateFrozen = false;
				_catRendered = true;
				_catInteractable = true;
				ChangeCatState(PartnerCatState.IdleLivingQuarters, TransitionMode.Force);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(newState), "Unsupported hub docking state.");
		}
	}

	/// <summary>
	/// 显式同步当前 Hub 状态，用于订阅 Hub 信号之后修正事件先后顺序。
	/// </summary>
	public void SyncWithHubState(HubDockingState currentHubState)
	{
		SyncWithHubState(currentHubState, _hubSignalSource.IsPlayerInZone(HubIds.LivingQuarters));
	}

	/// <summary>
	/// 显式同步当前 Hub 状态，并用玩家当前区域修正已错过的 Hub 事件。
	/// </summary>
	public void SyncWithHubState(HubDockingState currentHubState, bool playerInLivingQuarters)
	{
		switch (currentHubState)
		{
			case HubDockingState.Landed:
				_stateFrozen = false;
				_catRendered = true;
				_catInteractable = true;
				ChangeCatState(
					playerInLivingQuarters ? PartnerCatState.IdleLivingQuarters : PartnerCatState.SleepingOnIntelStation,
					TransitionMode.Force);
				break;
			case HubDockingState.DepartureLocked:
				_stateFrozen = true;
				_catRendered = true;
				_catInteractable = false;
				break;
			case HubDockingState.InTransit:
				_stateFrozen = true;
				_catRendered = false;
				_catInteractable = false;
				ChangeCatState(PartnerCatState.IdleLivingQuarters, TransitionMode.Force);
				break;
			case HubDockingState.Arrival:
				_stateFrozen = false;
				_catRendered = true;
				_catInteractable = true;
				ChangeCatState(PartnerCatState.IdleLivingQuarters, TransitionMode.Force);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(currentHubState), "Unsupported hub docking state.");
		}
	}

	/// <summary>
	/// 接收玩家进入区域事件。只有 zone 事件受 0.5 秒边界防抖影响。
	/// </summary>
	public bool OnPlayerEnteredZone(string zoneId)
	{
		if (_stateFrozen)
		{
			return false;
		}

		return zoneId switch
		{
			"living_quarters" when CatState is PartnerCatState.SleepingOnIntelStation or PartnerCatState.InNest =>
				ChangeCatState(PartnerCatState.IdleLivingQuarters, TransitionMode.RespectCooldown),
			"workbench" when CatState == PartnerCatState.IdleLivingQuarters =>
				ChangeCatState(PartnerCatState.FollowingPlayerToBench, TransitionMode.RespectCooldown),
			_ => false,
		};
	}

	/// <summary>
	/// 接收猫抵达工作台旁的事件，完成 following_player_to_bench 到 bench_adjacent 的转换。
	/// </summary>
	public bool OnCatReachedBench()
	{
		if (_stateFrozen || CatState != PartnerCatState.FollowingPlayerToBench)
		{
			return false;
		}

		return ChangeCatState(PartnerCatState.BenchAdjacent, TransitionMode.IgnoreCooldown);
	}

	/// <summary>
	/// 接收玩家离开工作台触发半径事件，让猫回到生活舱待机。
	/// </summary>
	public bool OnPlayerLeftBenchReachLimit()
	{
		if (_stateFrozen || CatState != PartnerCatState.BenchAdjacent)
		{
			return false;
		}

		return ChangeCatState(PartnerCatState.IdleLivingQuarters, TransitionMode.IgnoreCooldown);
	}

	/// <summary>
	/// 接收生活舱闲置计时完成事件；计时来源属于 Hub/场景层，本类只处理显式事件。
	/// </summary>
	public bool OnLivingQuartersIdleElapsed(double idleSeconds)
	{
		if (_stateFrozen || CatState != PartnerCatState.IdleLivingQuarters || idleSeconds <= NestSettleSeconds)
		{
			return false;
		}

		return ChangeCatState(PartnerCatState.InNest, TransitionMode.IgnoreCooldown);
	}

	/// <summary>
	/// 推进内部防抖倒计时；该方法不会自行触发猫状态变化。
	/// </summary>
	public void AdvanceTime(double deltaSeconds)
	{
		if (deltaSeconds < 0.0d)
		{
			throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta must be non-negative.");
		}

		_catStateCooldownRemaining = Math.Max(0.0d, _catStateCooldownRemaining - deltaSeconds);
		if (_sniffLockoutRemaining <= 0.0d)
		{
			return;
		}

		_sniffLockoutRemaining = Math.Max(0.0d, _sniffLockoutRemaining - deltaSeconds);
		if (_sniffLockoutRemaining == 0.0d && CatState == PartnerCatState.Sniffing)
		{
			ChangeCatState(_preSniffState, TransitionMode.Force);
		}
	}

	private void SubscribeHubEvents()
	{
		if (_hubEventsSubscribed)
		{
			return;
		}

		_hubSignalSource.HubStateChanged += OnHubStateChanged;
		_hubSignalSource.PlayerReturnedToHub += HandlePlayerReturnedToHubSignal;
		_hubSignalSource.PlayerEnteredZone += HandlePlayerEnteredZoneSignal;
		_hubEventsSubscribed = true;
	}

	private void HandlePlayerReturnedToHubSignal()
	{
		OnPlayerReturnedToHub();
	}

	private void HandlePlayerEnteredZoneSignal(string zoneId)
	{
		OnPlayerEnteredZone(zoneId);
	}

	private void DispatchOnPartnerJoined()
	{
		try
		{
			_intelSink.OnPartnerJoined(MvpPartnerId);
		}
		catch (Exception ex)
		{
			AddWarning($"intel_on_partner_joined_failed:{MvpPartnerId}:{ex.GetType().Name}");
		}
	}

	private void SafeRevealRumor(string revealTarget, string hazardHint, int confidence)
	{
		try
		{
			_intelSink.RevealRumor(
				revealTarget,
				MvpPartnerId,
				[hazardHint],
				confidence);
		}
		catch (Exception ex)
		{
			AddWarning($"intel_reveal_rumor_failed:{revealTarget}:{ex.GetType().Name}");
		}
	}

	private void SafeReportObservationEvent(string patternId)
	{
		try
		{
			_intelSink.ReportObservationEvent(patternId, PartnerSniffSuccessEventId);
		}
		catch (Exception ex)
		{
			AddWarning($"intel_report_observation_failed:{patternId}:{ex.GetType().Name}");
		}
	}

	private bool AccumulateNestItem()
	{
		var partner = _partners[MvpPartnerId];
		if (!partner.TryAccumulateNestItem(NestCapacity, out var oldState, out var newState))
		{
			return false;
		}

		if (newState != oldState)
		{
			NestStateChanged?.Invoke(oldState, newState);
		}

		return true;
	}

	private void TriggerSnapshot()
	{
		try
		{
			_snapshotSink.CaptureSnapshot(PartnerSnapshotDomainId, BuildProgressPartnerSnapshot());
		}
		catch (Exception ex)
		{
			AddWarning($"partner_snapshot_capture_failed:{ex.GetType().Name}");
		}
	}

	private void ResetPartners()
	{
		_partners.Clear();
		_partners[MvpPartnerId] = new PartnerState(MvpPartnerId);
	}

	private void DeriveTransientState(HubDockingState currentHubState)
	{
		_catStateCooldownRemaining = 0.0d;
		_sniffLockoutRemaining = 0.0d;
		_preSniffState = PartnerCatState.IdleLivingQuarters;
		_stateFrozen = currentHubState is HubDockingState.DepartureLocked or HubDockingState.InTransit;
		_catRendered = currentHubState != HubDockingState.InTransit;
		_catInteractable = currentHubState == HubDockingState.Landed || currentHubState == HubDockingState.Arrival;
		CatState = DeriveInitialCatState(currentHubState);
	}

	private bool ChangeCatState(PartnerCatState targetState, TransitionMode mode)
	{
		if (mode != TransitionMode.Force && _stateFrozen)
		{
			return false;
		}

		if (targetState == CatState)
		{
			return false;
		}

		if (mode == TransitionMode.RespectCooldown && _catStateCooldownRemaining > 0.0d)
		{
			return false;
		}

		var oldState = CatState;
		CatState = targetState;
		if (mode == TransitionMode.RespectCooldown)
		{
			_catStateCooldownRemaining = CatStateCooldownSeconds;
		}

		CatStateChanged?.Invoke(oldState, targetState);
		return true;
	}

	private ScoutSniffSignature? GetSniffSignature(string itemId)
	{
		IReadOnlyDictionary<string, object?>? entity;
		try
		{
			entity = _contentLookup.QueryEntity(itemId);
		}
		catch (Exception ex)
		{
			AddWarning($"content_query_failed:{itemId}:{ex.GetType().Name}");
			return null;
		}

		if (entity is null || !entity.TryGetValue("cat_sniff_signature", out var rawSignature))
		{
			return null;
		}

		if (rawSignature is not IReadOnlyDictionary<string, object?> signature || signature.Count == 0)
		{
			return null;
		}

		return new ScoutSniffSignature(
			ReadString(signature, "reveal_target"),
			ReadString(signature, "hazard_hint"),
			ReadInt(signature, "confidence", 0),
			ReadString(signature, "pattern_id"));
	}

	private void AddWarning(string warning)
	{
		_warnings.Add(warning);
	}

	private static string ReadString(IReadOnlyDictionary<string, object?> data, string field)
	{
		return data.TryGetValue(field, out var value) && value is not null
			? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
			: string.Empty;
	}

	private static int ReadInt(IReadOnlyDictionary<string, object?> data, string field, int fallback)
	{
		if (!data.TryGetValue(field, out var value) || value is null)
		{
			return fallback;
		}

		try
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}
		catch (FormatException)
		{
			return fallback;
		}
		catch (InvalidCastException)
		{
			return fallback;
		}
		catch (OverflowException)
		{
			return fallback;
		}
	}

	private static bool ReadBool(IReadOnlyDictionary<string, object?> data, string field)
	{
		if (!data.TryGetValue(field, out var value) || value is null)
		{
			return false;
		}

		try
		{
			return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
		}
		catch (FormatException)
		{
			return false;
		}
		catch (InvalidCastException)
		{
			return false;
		}
	}

	private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, object?> data, string field)
	{
		if (!data.TryGetValue(field, out var value)
			|| value is string
			|| value is not System.Collections.IEnumerable values)
		{
			return Array.Empty<string>();
		}

		var result = new List<string>();
		foreach (var item in values)
		{
			var text = Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text))
			{
				result.Add(text);
			}
		}

		return result;
	}

	private static IReadOnlyList<int> ReadIntList(IReadOnlyDictionary<string, object?> data, string field)
	{
		if (!data.TryGetValue(field, out var value)
			|| value is string
			|| value is not System.Collections.IEnumerable values)
		{
			return Array.Empty<int>();
		}

		var result = new List<int>();
		foreach (var item in values)
		{
			try
			{
				var converted = Convert.ToInt32(item, CultureInfo.InvariantCulture);
				if (converted >= 0)
				{
					result.Add(converted);
				}
			}
			catch (FormatException)
			{
			}
			catch (InvalidCastException)
			{
			}
			catch (OverflowException)
			{
			}
		}

		return result;
	}

	private static PartnerCatState DeriveInitialCatState(HubDockingState hubState)
	{
		return hubState switch
		{
			HubDockingState.Landed => PartnerCatState.SleepingOnIntelStation,
			HubDockingState.DepartureLocked => PartnerCatState.SleepingOnIntelStation,
			HubDockingState.InTransit => PartnerCatState.IdleLivingQuarters,
			HubDockingState.Arrival => PartnerCatState.IdleLivingQuarters,
			_ => PartnerCatState.SleepingOnIntelStation,
		};
	}

	private static PartnerNestState DeriveNestState(int nestItemCount)
	{
		return nestItemCount switch
		{
			0 => PartnerNestState.Empty,
			1 => PartnerNestState.First,
			2 or 3 => PartnerNestState.Accumulating,
			_ => PartnerNestState.Full,
		};
	}

	private static bool NestItemsFollowStaticManifest(IReadOnlyList<int> nestItems)
	{
		for (var i = 0; i < nestItems.Count; i++)
		{
			if (nestItems[i] != i)
			{
				return false;
			}
		}

		return true;
	}

	private enum TransitionMode
	{
		RespectCooldown,
		IgnoreCooldown,
		Force,
	}

	private sealed class EmptyPartnerContentLookup : IPartnerContentLookup
	{
		internal static readonly EmptyPartnerContentLookup Instance = new();

		public IReadOnlyDictionary<string, object?>? QueryEntity(string itemId)
		{
			return null;
		}
	}

	private sealed class EmptyPartnerInventorySource : IPartnerInventorySource
	{
		internal static readonly EmptyPartnerInventorySource Instance = new();

		public IReadOnlyList<string> GetInventoryItems()
		{
			return Array.Empty<string>();
		}
	}

	private sealed class EmptyPartnerSnapshotStore : IPartnerSnapshotStore
	{
		internal static readonly EmptyPartnerSnapshotStore Instance = new();

		public IReadOnlyDictionary<string, object?> RestoreSnapshot(string domainId)
		{
			return new Dictionary<string, object?>(StringComparer.Ordinal);
		}
	}

	private sealed class NoOpPartnerSnapshotSink : IPartnerSnapshotSink
	{
		internal static readonly NoOpPartnerSnapshotSink Instance = new();

		public void CaptureSnapshot(string domainId, IReadOnlyDictionary<string, object?> payload)
		{
		}
	}

	private sealed class EmptyPartnerHubSignalSource : IPartnerHubSignalSource
	{
		internal static readonly EmptyPartnerHubSignalSource Instance = new();

		public HubDockingState CurrentHubState => HubDockingState.Landed;

		public event Action<HubDockingState>? HubStateChanged
		{
			add { }
			remove { }
		}

		public event Action? PlayerReturnedToHub
		{
			add { }
			remove { }
		}

		public event Action<string>? PlayerEnteredZone
		{
			add { }
			remove { }
		}

		public bool IsPlayerInZone(string zoneId)
		{
			return false;
		}
	}

	private sealed class ImmediatePartnerBootstrapSequencer : IPartnerBootstrapSequencer
	{
		internal static readonly ImmediatePartnerBootstrapSequencer Instance = new();

		public void QueueCall(Action callback)
		{
			callback();
		}
	}

	private sealed class NoOpPartnerIntelSink : IPartnerIntelSink
	{
		internal static readonly NoOpPartnerIntelSink Instance = new();

		public void RevealRumor(string revealTarget, string sourceTag, IReadOnlyList<string> hazardTags, int confidence)
		{
		}

		public void ReportObservationEvent(string patternId, string eventId)
		{
		}

		public void OnPartnerJoined(string partnerId)
		{
		}
	}
}
