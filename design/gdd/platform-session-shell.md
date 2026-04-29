# 平台与会话壳

> **Status**: In Design
> **Author**: User + Codex
> **Last Updated**: 2026-04-28
> **Implements Pillar**: 规划先于冒险; 飞艇是家，不只是载具; 未知带来温和压力
> **System Index**: `design/gdd/systems-index.md`

## Overview

`平台与会话壳` 是《云海织航》的 Web-first 入口与会话生命周期系统。它负责把玩家从浏览器页面带入一个可恢复、可听见、可操作、可继续的游戏会话：加载游戏、展示开始/继续入口、处理首次音频激活、建立键鼠输入入口、在标签页失焦或恢复时保护会话状态，并在玩家返回时清楚地告诉他们当前能继续什么。它不是存档系统、UI/HUD 系统或具体玩法系统，但它为这些系统提供可靠的外层节奏，确保玩家在短体量 Web 正式版中不会因为浏览器限制、音频策略、切页暂停或恢复不清而失去“飞艇是家”的安全感。

技术约束在本 GDD 中只作为设计约束出现：Godot 4.6.2 Web 导出、桌面浏览器、键盘/鼠标、音频需用户手势激活、后台标签页可能暂停、首屏加载和恢复反馈必须可理解。MVP 必须有 custom HTML shell 或等价 JS shim 捕获浏览器生命周期信号；具体 Autoload、SceneTree、JavaScriptBridge 绑定、资源加载或存档 API 方案留给后续架构与 ADR。

## Player Fantasy

玩家不会把 `平台与会话壳` 当作一个可游玩的系统来感知，但它应该让玩家相信：即使暂时离开，云海中的家与航程也会安稳地留在原处，等他们回来继续。

这个系统支撑的是一种低调的安全感。玩家关闭标签页、刷新页面、切去别的窗口，或在浏览器恢复焦点后重新进入游戏时，不应该觉得自己被抛回一个冷冰冰的启动流程，也不应该焦虑进度、音频、输入或当前目标是否还可靠。理想体验是：点下“继续”后，飞艇、航线计划和上次的照料节奏被清楚接回，像重新踏回熟悉的甲板。

它的情绪目标不是奖励、刺激或高频反馈，而是“世界还替我留着灯”。开始、继续、音频激活和焦点恢复都应保持克制、清楚、温和：像可靠的航务员，也像始终有人值守的港口。玩家感受到的不是平台壳本身，而是游戏值得信任，飞艇像一个可以返回的家，航程不会因为现实中的短暂离开而散掉。

## Detailed Rules

### Core Rules

1. `平台与会话壳` 只负责 Web-first 的进入、继续、音频激活、焦点恢复与会话生命周期编排。MVP 必须通过 custom HTML shell 或等价 JS shim 接入浏览器生命周期事件，并经 Godot `JavaScriptBridge` / 平台适配层传入壳层；存档数据、存储介质、manifest pointer、promotion 和具体 codec 由 `本地存档与世界状态持久化` 与后续 ADR 决定。
2. 壳层是“进入和离开世界的门”，不是世界本身。它可以决定何时进入、暂停、恢复、退出或停在安全错误态，但不能决定任务结果、资源变化、航线结果、探索判定或设施修复结果。
3. `Start` 和 `Continue` 是会话意图，不是直接进入玩法。真正进入可操作世界前，必须完成基础加载、内容域可用性检查、会话可继续性检查、输入焦点准备，以及必要的音频激活处理。
4. `Start` 永远表示开始新会话，不复用旧会话上下文。`Continue` 只在已验证存在可恢复会话时显示或启用。
5. 首次音频激活必须由明确用户手势触发。`Start`、`Continue`、`ResumePending` 的确认输入都必须在同一个可信手势内尝试音频解锁或恢复；音频失败是软失败，游戏可以进入持久的无声会话，但必须给出清楚提示，不能把整个游戏锁死。
6. 标签页隐藏、窗口失焦或浏览器暂停时，壳层默认进入后台挂起或软暂停。后台状态下，输入不得进入玩法层，游戏不得假装仍在正常交互。
7. 页面恢复可见后，必须先确认页面可见与可交互，再做会话恢复检查，再恢复 UI focus。第一次返回输入只用于重新激活或确认，并在同一手势中尝试音频恢复；该输入不得同时触发普通玩法动作。
8. 壳层只做输入门禁。进入 `SessionActive` 后，普通玩法输入下放给玩家移动与交互系统；壳层只保留暂停、失焦、错误恢复等生命周期级拦截。
9. 内容加载、内容域校验、会话恢复或存储能力失败时，必须 fail closed：停在壳层安全界面，允许重试、返回标题、开始新会话或查看错误，不允许进入半初始化世界。
10. 任何失败都不得覆盖有效继续点、污染现有会话、生成损坏状态或自动清空失效续档。是否迁移、修复或清理存档由 `本地存档与世界状态持久化` 决定。
11. 壳层必须维护一个会话连续性不变量：玩家离开、刷新、页面被浏览器丢弃或重新进入后，只能恢复到存档系统确认的最近安全继续点，或回到清楚说明不可继续原因的壳层安全状态；不得恢复到未经验证的半初始化状态。
12. MVP 允许 `ephemeral_session_allowed = true`。当浏览器存储不可用或写入被阻止时，玩家可以开始临时会话，但壳层必须在进入前明确提示“本次进度不会保存”，并且不得显示或生成持久化 `Continue`。
13. `Continue = PreservedLocked` 在 MVP 中必须可见但不可进入玩法。它显示为锁定继续入口，主操作是查看原因 / 返回标题 / 开始新会话；不得只隐藏入口或只灰掉按钮。
14. 壳层 UI 是进入、加载、音频、恢复和错误提示的焦点所有者。只要壳层 overlay 可见，玩法 HUD 和玩法输入不得接收焦点或输入；进入 `SessionActive` 且无壳层 overlay 后，焦点才交给下游 UI / HUD。
15. 所有 `Start`、`Continue`、音频确认、恢复确认和重试操作必须使用单次 in-flight 意图 token 去重。token 完成、取消或失败前，后续同类输入只能被忽略或转为明确的取消/返回操作，不得并行创建第二个会话上下文。

### States and Transitions

主平台状态如下：

| State | Meaning | Allowed Player Input | Exit Condition |
|---|---|---|---|
| `Booting` | 游戏刚启动，壳层尚未可交互 | None | 基础壳层资源可显示 |
| `Loading` | 正在加载基础资源、内容域状态、存储能力和会话元数据 | None or cancel if available | 加载完成、等待、可恢复失败或致命失败 |
| `Ready` | 主入口可交互，显示 Start / Continue 状态 | Start, Continue, settings-level shell actions | 用户选择入口或进入错误态 |
| `AwaitingAudioActivation` | 等待可信用户手势激活音频或确认无声继续 | Click/key confirm, mute/continue | 音频解锁成功、软失败转无声、或玩家选择无声继续 |
| `SessionStarting` | 已收到 Start / Continue 意图，正在建立单个 in-flight 会话 token | Cancel/back only if safe | 会话准备完成或失败 |
| `SessionActive` | 玩家可操作，玩法系统接收输入 | Gameplay input | 失焦、隐藏、退出、错误 |
| `BackgroundSuspended` | 标签页隐藏、窗口失焦、`visibilitychange hidden`、`pagehide` 或浏览器暂停，壳层冻结会话推进并保留挂起 token | None | 页面恢复可见/可交互，或必须回到标题/恢复错误态 |
| `ResumePending` | 页面已回来，等待恢复检查、焦点重获和明确重新激活 | Confirm/click/key reactivation, Return Title | 恢复成功、恢复失败或退出 |
| `RecoveryRequired` | 可恢复失败，壳层保留安全路径 | Retry, New Session, Return Title | 重试成功、放弃或进入 fatal |
| `FatalBlocked` | 核心能力缺失或启动不可继续 | Retry, refresh guidance, return where possible | 重试成功或用户离开 |

关键转移：

| Transition | Guard |
|---|---|
| `Booting -> Loading` | 壳层基础显示能力可用 |
| `Loading -> Ready` | 基础资源、内容域状态、存储能力和会话元数据检查完成；若只能临时会话则同时标记 `EphemeralOnly` |
| `Loading -> RecoveryRequired` | 内容域 `FAILED`、存储读取失败或会话元数据可重试失败 |
| `Loading -> FatalBlocked` | 核心资源缺失、内容域 `VERSION_INCOMPATIBLE`、构建不兼容或必需浏览器能力不足 |
| `Ready -> AwaitingAudioActivation` | 用户选择 Start / Continue 且音频仍需用户手势 |
| `Ready -> SessionStarting` | 用户选择 Start / Continue 且无需额外音频门禁 |
| `AwaitingAudioActivation -> SessionStarting` | 音频解锁成功，或玩家明确选择无声继续 |
| `SessionStarting -> SessionActive` | 会话上下文、内容域、可继续性和输入焦点检查通过 |
| `SessionStarting -> RecoveryRequired` | 继续会话失败、内容域失败、存储状态不支持继续或会话元数据损坏 |
| `SessionActive -> BackgroundSuspended` | 标签页隐藏、窗口失焦、`visibilitychange hidden`、`pagehide` 或浏览器暂停；壳层创建挂起 token，并只请求存档系统执行已预编码 marker / lightweight flush，不请求完整 safe checkpoint |
| `BackgroundSuspended -> ResumePending` | `pageshow` / 可见性恢复后，页面可见且可接收可信输入 |
| `ResumePending -> SessionActive` | 恢复检查通过，玩家完成明确重新激活 |
| `ResumePending -> RecoveryRequired` | 恢复检查失败 |
| `Any -> FatalBlocked` | 不可恢复平台错误或核心初始化失败 |

加载子阶段不升级为主状态，但必须用于诊断和重试分类：`BaseBoot`、`ContentDomainCheck`、`StorageCapabilityCheck`、`SessionMetadataCheck`、`EntryRenderReady`。每次失败报告必须包含失败子阶段、失败类型、是否可重试、浏览器可见性/焦点状态和是否存在有效继续点。

浏览器生命周期事件必须幂等处理：同一隐藏、`visibilitychange hidden`、`pagehide`、失焦或恢复序列只能生成一个挂起 token；重复 `pageshow`、focus 或可见性事件只能刷新现有 token 状态，不能创建第二个恢复流程。`visibilitychange` / focus / trusted input 到达顺序不可靠时，以挂起 token 的当前阶段为准：未进入 `ResumePending` 前丢弃玩法输入，进入 `ResumePending` 后第一下可信输入只确认恢复。

浏览器生命周期归一化表：

| Browser Signal | Shell Interpretation | Required Handling |
|---|---|---|
| `visibilitychange hidden` | 页面不可见，可能即将暂停 | 进入或保持 `BackgroundSuspended`，只请求已预编码 marker / lightweight flush，不开放玩法输入，不启动完整保存。 |
| `pagehide` with `persisted=false` | 页面正在离开或关闭 | 只请求已预编码 marker / lightweight flush；后续重新进入必须从 `Booting` / `Loading` 重新验证继续点。 |
| `pagehide` with `persisted=true` | 页面可能进入 BFCache | 进入 `BackgroundSuspended`，不得承诺保存完成；返回时仍需恢复检查和焦点重获。 |
| `pageshow` with `persisted=true` | BFCache 恢复 | 不直接回到 `SessionActive`；必须进入 `ResumePending`，重新确认页面可交互、内容域状态和存档系统的最近安全点。 |
| `pageshow` with `persisted=false` | 新页面实例或普通显示 | 走 `Booting` / `Loading` 或现有挂起 token 的恢复路径，不复用未验证旧内存状态。 |
| `beforeunload` / `unload` | 不可靠的关闭前信号 | 不启动新序列化、迁移、备份提升或阻塞式保存；只能记录已准备好的轻量 best-effort 请求。 |
| Browser discard / reload detected | 旧页面内存不可信 | 重新进入 `Booting` / `Loading`，由存档系统重新读回验证。 |

不纳入主状态的内容：设置菜单展开、新手提示弹窗、HUD 面板打开、加载条百分比、具体屏幕转场动画。这些属于状态内 UI 子模式，不能升级为壳层主状态。

### Interactions with Other Systems

| System | Shell Sends / Requests | Shell Receives / Reads | Boundary |
|---|---|---|---|
| `内容数据与状态注册表` | 请求内容域可用性和构建兼容状态 | `UNLOADED` / `LOADING` / `PARTIAL` / `COMPLETE` / `FAILED` / `VERSION_INCOMPATIBLE` / diagnostics summary | 壳层不解析内容本体，不决定内容规则 |
| `本地存档与世界状态持久化` | `start_new`, `continue_requested`, `suspend_requested`, `resume_requested`, `safe_close_marker_requested`, raw `persistence_probe` signals, optional `working_set_budget_bytes` | continue availability, session metadata, restore result, authoritative `storage_capability` | 壳层不拥有存档格式、迁移、写入、pointer promotion、`storage_capability` 判定或清理策略；关闭/隐藏路径只请求 marker，不请求完整保存 |
| `玩家移动与交互` | gameplay input enabled/disabled, session active gate | interaction readiness or blocked state | 只有 `SessionActive` 才能把输入交给玩法层 |
| `UI / HUD / 航图界面` | shell-level entry state, loading/error/recovery prompts | none required for core MVP | 壳层拥有入口、加载、错误和恢复提示；HUD 属于下游 |
| `音频 / 反馈语义` | audio unlock requested, resume audio requested, mute fallback selected | unlock succeeded/failed, audio locked/unlocked | 壳层只处理首次激活和恢复门槛，不拥有世界音频规则 |
| Platform adapter / browser layer | none at design level | visibility changed, window focus changed, trusted input received, viewport resized, storage capability | 拥有浏览器生命周期和能力信号接入；具体 Godot API、JS bridge、HTML shell 绑定留给 ADR |

跨系统硬边界：

- 壳层不拥有世界模拟。
- 壳层不做数值判定。
- 壳层不持有存档格式。
- 壳层不管理常驻 HUD 数据绑定。
- 壳层不直接操作玩家实体。
- 壳层不决定航线、搜撤、修复、市场、伙伴或战斗逻辑。
- 壳层不承诺 gamepad、touch、PWA、离线缓存或 PC-later 技术路径。

## Formulas

本系统不定义战斗、经济或成长数值公式；它只定义会话入口、恢复、输入门禁和失败分级的判定规则。所有公式都必须保持只读判定：不得修改存档、覆盖继续点或改变玩法状态。

The `content_domain_ready` rule is defined as:

`required_content_domain_status = VERSION_INCOMPATIBLE if any required domain status = VERSION_INCOMPATIBLE; else FAILED if any required domain status = FAILED; else Waiting if any required domain status IN {UNLOADED, LOADING, PARTIAL}; else COMPLETE`

`content_domain_ready = true if required_content_domain_status = COMPLETE; false otherwise`

`content_domain_failure_class = Waiting if required_content_domain_status = Waiting; Recoverable if required_content_domain_status = FAILED; Fatal if required_content_domain_status = VERSION_INCOMPATIBLE`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `required_domain_statuses` | `D[]` | map/list | domain -> `UNLOADED` / `LOADING` / `PARTIAL` / `COMPLETE` / `FAILED` / `VERSION_INCOMPATIBLE` | 当前入口或恢复流程所需所有内容域的逐域状态。 |
| `required_content_domain_status` | `D` | enum | `Waiting` / `COMPLETE` / `FAILED` / `VERSION_INCOMPATIBLE` | 按优先级聚合后的内容域状态；`VERSION_INCOMPATIBLE` 高于 `FAILED`，`FAILED` 高于 `Waiting`，全部完成才是 `COMPLETE`。 |
| `content_domain_ready` | `C` | bool | true/false | 内容域是否可用于进入或恢复会话 |
| `content_domain_failure_class` | `FC` | enum | `Waiting` / `Recoverable` / `Fatal` | 内容域未就绪时应进入等待、可恢复错误还是不可恢复阻断 |

**Output Range:** true/false and `Waiting` / `Recoverable` / `Fatal`。
**Example:** 如果 `routes=COMPLETE`、`world=FAILED`、`intel=LOADING`、`threats=COMPLETE`，则聚合结果为 `FAILED`，壳层进入 `RecoveryRequired` 而不是停在等待；如果任一必需域为 `VERSION_INCOMPATIBLE`，则聚合结果为 `VERSION_INCOMPATIBLE`。

The `continue_availability` rule is delegated to `本地存档与世界状态持久化`.

`continue_availability = persistence.query_continue_state().continue_availability`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `persistence_continue_state` | `P` | struct | includes `continue_availability`, `reason_code`, `checkpoint_summary` | 存档系统返回的权威继续状态。 |
| `continue_availability` | `A` | enum | `Enabled` / `PreservedLocked` / `Hidden` | 壳层消费和呈现的 Continue 可用状态。 |

**Output Range:** `Enabled`、`PreservedLocked` 或 `Hidden`。
**Ownership:** 壳层不得根据 `continue_point_exists`、`continue_point_integrity_ok` 或 `content_domain_ready` 重新计算该 enum；这些条件由存档系统、内容注册表和恢复流程共同校验后统一返回。壳层只负责入口显示、焦点、去重和安全失败呈现。
**Example:** 如果存档系统返回 `PreservedLocked` 和版本不兼容原因码，壳层显示锁定 Continue 入口和原因详情，不进入 `SessionStarting`。

The `input_gate` formula is defined as:

`input_gate = Open if session_state = Active AND foreground_visible AND foreground_interactive AND input_focus_ok AND NOT modal_blocked; Reacquire if session_state = Suspended AND foreground_visible AND foreground_interactive; Closed otherwise`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `session_state` | `S` | enum | `Inactive` / `Suspended` / `Active` | 当前会话状态 |
| `foreground_visible` | `Fv` | bool | true/false | 页面是否可见 |
| `foreground_interactive` | `Fi` | bool | true/false | 页面是否可接收可信输入 |
| `input_focus_ok` | `K` | bool | true/false | 是否已获得输入焦点 |
| `modal_blocked` | `M` | bool | true/false | 是否被壳层模态提示、恢复提示或错误提示阻塞 |
| `input_gate` | `G` | enum | `Open` / `Reacquire` / `Closed` | 输入门状态 |

**Output Range:** `Open`、`Reacquire` 或 `Closed`。
**Example:** 如果会话处于 `Suspended` 且页面已回到前台，则输出 `Reacquire`，表示第一次输入只用于重新激活，不能触发普通玩法动作。

The `resume_readiness` formula is defined as:

`resume_readiness = session_state = Suspended AND suspended_session_valid AND foreground_visible AND foreground_interactive AND explicit_reactivate AND input_focus_ok AND content_domain_ready`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `session_state` | `S` | enum | `Inactive` / `Suspended` / `Active` | 当前会话状态 |
| `suspended_session_valid` | `V` | bool | true/false | 当前内存会话是否仍可恢复；由挂起 token、会话上下文和必要安全点共同决定 |
| `foreground_visible` | `Fv` | bool | true/false | 页面是否已回到可见状态 |
| `foreground_interactive` | `Fi` | bool | true/false | 页面是否已可接收可信输入 |
| `explicit_reactivate` | `R` | bool | true/false | 玩家是否完成明确的重新激活操作 |
| `input_focus_ok` | `K` | bool | true/false | 输入焦点是否已恢复 |
| `content_domain_ready` | `C` | bool | true/false | 当前恢复所需内容域是否可用 |

**Output Range:** true/false；true 表示可从 `ResumePending` 回到 `SessionActive`。
**Example:** 页面回到前台后，如果玩家尚未明确点击或按键重新激活，则输出 false。

The `entry_readiness` formula is defined as:

`entry_readiness = base_loaded AND content_domain_ready AND entry_focus_ready AND audio_gate IN {Pass, Muted} AND (session_intent = Start OR (session_intent = Continue AND continue_availability = Enabled))`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `base_loaded` | `B` | bool | true/false | 基础加载是否完成 |
| `content_domain_ready` | `C` | bool | true/false | 内容域是否可用于进入会话 |
| `entry_focus_ready` | `K` | bool | true/false | 壳层入口 UI 已拥有焦点，且首个玩法输入不会泄漏到玩法层 |
| `audio_gate` | `U` | enum | `Pass` / `SoftFail` / `Muted` / `HardFail` | 音频门状态；`Muted` 表示玩家已选择持久无声 |
| `session_intent` | `T` | enum | `Start` / `Continue` | 会话意图 |
| `continue_availability` | `A` | enum | `Enabled` / `PreservedLocked` / `Hidden` | Continue 的可用状态 |

**Output Range:** true/false；true 表示可进入 `SessionActive`。
**Example:** `Start` 意图下，如果基础加载完成、内容域可用、入口焦点已准备好且音频为 `Pass`，则输出 true，即使 `input_gate` 仍未对玩法开放也可以进入 `SessionStarting`。`Continue` 意图下，如果音频为 `Muted` 且 `continue_availability = Enabled`，则输出 true，允许持久无声继续。

The `failure_severity` formula is defined as:

`failure_severity = HardFail if hard_gate_failed; SoftFail if soft_gate_failed; RecoverableFail if recoverable_gate_failed; None otherwise`

`hard_gate_failed = NOT base_loaded OR content_domain_failure_class = Fatal OR audio_gate = HardFail OR (operation_kind = Continue AND continue_availability = Hidden) OR (operation_kind = Resume AND NOT resume_readiness)`

`soft_gate_failed = audio_gate = SoftFail OR storage_capability IN {WriteLocked, EphemeralOnly}`

`recoverable_gate_failed = content_domain_failure_class = Recoverable OR (operation_kind = Continue AND continue_availability = PreservedLocked)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `base_loaded` | `B` | bool | true/false | 基础加载是否完成 |
| `content_domain_failure_class` | `FC` | enum | `Waiting` / `Recoverable` / `Fatal` | 内容域失败类型 |
| `continue_availability` | `A` | enum | `Enabled` / `PreservedLocked` / `Hidden` | Continue 的可用状态 |
| `resume_readiness` | `R` | bool | true/false | 恢复是否已就绪 |
| `operation_kind` | `O` | enum | `Start` / `Continue` / `Resume` | 当前尝试的操作类型 |
| `audio_gate` | `U` | enum | `Pass` / `SoftFail` / `Muted` / `HardFail` | 音频门状态 |
| `storage_capability` | `P` | enum | `PersistentAvailable` / `WriteLocked` / `EphemeralOnly` | 当前浏览器存储能力；旧档可读但不能可靠写入时为 `WriteLocked`，旧档也不可读或策略强制临时时为 `EphemeralOnly`。 |

**Output Range:** `None`、`SoftFail`、`RecoverableFail` 或 `HardFail`。
**Example:** 如果只有音频解锁失败，则输出 `SoftFail` 并允许无声继续；如果内容域加载失败但可重试，或 Continue 为 `PreservedLocked`，则输出 `RecoverableFail` 并进入可解释的恢复/锁定详情路径；如果内容域版本不兼容，则输出 `HardFail`，必须停在壳层安全界面。普通 `Start` 进入前 `input_gate = Closed` 不属于硬失败，因为玩法输入尚未开放是预期状态。

## Edge Cases

- **If 首次加载壳层失败且无法进入可交互状态**: 进入 `FatalBlocked`，只显示不可继续的错误态与重试入口；不得生成、修改或覆盖任何继续点。
- **If 壳层基础加载成功但内容域初始化失败**: 进入 `RecoveryRequired` 或安全错误态；`Start` 只有在不依赖失败内容域时才可保留，`Continue` 必须禁用，已有继续点状态不变。
- **If `Continue` 对应的继续点缺失**: `Continue` 立即隐藏或禁用，系统只能走 `Start` 路径；不得回填默认会话冒充可继续进度。
- **If 继续点存在但结构损坏或校验失败**: 输出 `PreservedLocked`，禁用 `Continue` 并显示可理解提示；不得自动删除该继续点，也不得影响其他有效继续点。
- **If 继续点版本与当前内容域版本不匹配**: 禁用 `Continue` 并进入版本不兼容提示态；只允许重新开始或进入由存档系统定义的显式迁移流程。
- **If 继续点的内容域标识不匹配当前构建目标域**: 禁用 `Continue` 并隔离该记录；不得覆盖当前域的可继续状态。
- **If `Continue = PreservedLocked` 被玩家选中**: 不进入 `SessionStarting`；打开原因详情或恢复问题提示，提供 Return Title、New Session 和由存档系统定义的显式迁移/修复入口（若存在）。
- **If 音频因缺少用户手势而未解锁**: 按 `SoftFail` 处理，允许玩家选择持久无声运行，并保留“点击或按键启用音频”的非阻断提示。
- **If 玩家选择持久无声运行**: 设置 `audio_gate = Muted`，本次会话和后续恢复流程不得反复弹出阻断式音频提示；只能显示非阻断的启用音频入口。
- **If 音频在恢复后再次无法解锁**: 保持已恢复会话可用，维持 `Muted` 或待激活状态；不得弹出重复阻断式错误。
- **If 标签页隐藏、窗口失焦、`visibilitychange hidden`、`pagehide` 或浏览器暂停**: 立即进入 `BackgroundSuspended`，冻结玩法输入和会话推进，创建或复用挂起 token，并只请求存档系统执行已预编码 marker / lightweight flush；不得触发关卡推进、资源变化、完整序列化、readback、checksum、迁移、备份提升或会话切换。
- **If 页面从隐藏/失焦恢复但玩家尚未明确重新激活**: 保持 `ResumePending`，不接受玩法输入、不消耗资源、不播放推进音效。
- **If 恢复期间发生键盘或鼠标输入**: 该输入只可用于重新激活或被丢弃；在 `input_gate = Open` 前不得传入玩法层。
- **If 用户在 `SessionStarting` 或 `ResumePending` 中重复点击 `Start` / `Continue` / 恢复确认**: 只接受当前 in-flight token 绑定的第一次有效意图，其余点击去重；不得并行创建两个会话上下文。
- **If 页面刷新、标签页关闭后重新打开、浏览器丢弃页面或 BFCache 恢复**: 不得复用未验证的旧内存状态。壳层必须重新进入 `Booting` / `Loading`，读取存档系统确认的最近安全继续点；若无法确认，则显示 `RecoveryRequired` 或 `PreservedLocked`，不得直接进入玩法。
- **If 浏览器存储不可用或写入被阻止**: 视为软失败，允许当前临时会话继续，但必须在进入前显示无保存提示，禁用持久化继续点写入，并确保 `Continue` 不会伪装成可用。
- **If 浏览器存储能力检测显示旧档可读但新写入被阻止**: MVP 中归一化为 `storage_capability = WriteLocked`；壳层可呈现已验证旧 Continue，但必须显示新进度无法可靠保存的受控路径。
- **If 浏览器存储能力检测显示旧档读取也不可靠、底层 API 不可用或平台策略强制临时会话**: MVP 中归一化为 `storage_capability = EphemeralOnly`；不得保留一个未处理的 `Unavailable` 分支。
- **If 存储读取失败导致无法验证继续点完整性**: `Continue` 必须禁用；不得使用缓存旧值替代验证结果。
- **If 窗口尺寸变化发生在壳层初始化或恢复过程中**: 先完成布局重算并恢复默认 focus anchor，再开放输入；中间帧不得触发继续点刷新或会话切换。
- **If 错误重试被连续触发或短时间反复失败**: 重试必须节流，并在每次重试前重新校验内容域、版本和继续点；错误不得升级为覆盖或清理继续点的行为。
- **If `FatalBlocked` 后用户选择重试**: 重试从 `Booting` 或 `Loading` 重新开始，只复查平台与内容条件；不得假设上一次半初始化状态仍然有效。
- **If `RecoveryRequired` 后用户选择 New Session**: 壳层只发出新会话意图；是否保留、迁移或清理旧继续点由 `本地存档与世界状态持久化` 决定。

## Dependencies

`平台与会话壳` 是 Foundation 层系统。它没有更早的玩法依赖，但它依赖浏览器/Godot Web 运行环境提供基础页面可见性、输入焦点、音频激活和存储能力信号。它同时是多个后续系统的入口门禁：后续系统不能绕过壳层直接进入可操作玩法。

| Dependency | Type | Direction | Interface / Contract |
|---|---|---|---|
| Godot 4.6.2 Web runtime | Hard | Platform -> Shell | 提供 Web 导出运行、基础渲染、键鼠输入、窗口/焦点/可见性变化、音频上下文和浏览器存储能力信号。 |
| Desktop browser environment | Hard | Platform -> Shell | Chrome / Edge / Firefox 桌面浏览器；必须按 Web autoplay、后台标签页暂停、焦点恢复和存储可用性限制设计。 |
| `内容数据与状态注册表` | Hard before entering session | Registry -> Shell | 提供内容域加载状态、版本兼容状态和诊断摘要。壳层只读取 `UNLOADED` / `LOADING` / `PARTIAL` / `COMPLETE` / `FAILED` / `VERSION_INCOMPATIBLE` 等可用性状态，不解析内容定义。 |
| `本地存档与世界状态持久化` | Hard for Continue; Soft for Start | Shell <-> Persistence | 壳层请求开始、继续、挂起、恢复、安全关闭；存档系统返回继续点是否存在、是否完整、是否兼容、是否可恢复，以及最近安全继续点摘要。 |
| `玩家移动与交互` | Downstream hard gate | Shell -> Interaction | 壳层只在 `SessionActive` 且 `input_gate = Open` 时放行玩法输入。 |
| `UI / HUD / 航图界面` | Downstream / soft | Shell -> UI | 壳层拥有启动、加载、错误、恢复提示和壳层 overlay 焦点；HUD 和玩法界面在会话激活且壳层 overlay 关闭后接管。 |
| `反馈、特效与音频语义` | Soft for MVP; hard for polished release | Shell <-> Audio/Feedback | 壳层请求首次音频激活、恢复音频和持久静音模式；音频失败为软失败，允许无声继续。 |
| `新手引导与首轮闭环` | Downstream / vertical slice | Shell -> Onboarding | 壳层提供 Start / Continue / Resume 入口，但不拥有新手引导步骤或首轮任务编排。 |
| Platform adapter / browser layer | Hard | Platform -> Shell | 提供 `visibilitychange`、`pagehide`、`pageshow`、focus/blur、可信输入、viewport resize、storage capability 和 browser restore/discard 线索；生命周期和能力信号由壳层/平台适配层归一化，具体接入方式留给 ADR。 |

硬依赖：

- Godot Web runtime 与桌面浏览器能力必须存在，否则进入 `FatalBlocked`。
- 进入 `SessionActive` 前，必须确认需要的内容域状态没有阻断错误。
- `Continue` 必须依赖存档系统验证继续点；壳层不能自行推断继续点有效。
- 玩法输入必须依赖壳层门禁；后台、恢复中、错误态不得绕过壳层输入门。

软依赖：

- 音频激活失败不阻断会话，只降低为持久无声运行和提示状态。
- 持久化不可用时，MVP 允许临时会话；但 `Continue` 和持久化继续点必须禁用或明确标记不可用。
- UI/HUD 可以在 MVP 先使用壳层基础提示，但生产期需要交给 UI 系统规范化。

## Tuning Knobs

| Knob | Default / MVP Intent | Safe Range | Too Low / Too Strict | Too High / Too Loose |
|---|---|---|---|---|
| `initial_loading_timeout_seconds` | 15s before showing slow-load feedback | 8-30s | 过早报错，让正常 Web 加载显得失败 | 玩家长时间看空白或无反馈加载 |
| `fatal_load_retry_limit` | 3 attempts before stronger refresh guidance | 1-5 | 短暂网络/加载抖动没有恢复机会 | 反复重试造成挫败，像卡死 |
| `retry_backoff_seconds` | 1s, then 3s, then 5s | 0.5-10s | 过快重试刷屏或重复初始化 | 等待太久，玩家以为不可用 |
| `resume_requires_explicit_confirm` | true | true/false | false 会增加恢复后误触玩法动作风险 | true 会多一次确认，但更安全 |
| `first_resume_input_consumed` | true | true | 设为 false 会让恢复点击同时触发玩法动作 | 不适用；该值应固定为 true |
| `audio_soft_fail_allows_play` | true | true | false 会因为浏览器音频策略阻断游戏 | 不适用；该值应固定为 true |
| `continue_preserved_locked_visible` | true | true | false 会让玩家以为存档消失 | 不适用；MVP 固定为 true |
| `ephemeral_session_allowed` | true with explicit no-save warning | true | false 会在隐私模式/存储不可用时阻断试玩 | 不适用；MVP 固定为 true，但必须禁止持久 Continue |
| `shell_error_detail_level` | Player-safe summary + optional diagnostics copy | low / medium / developer | 太低无法排查 Web 问题 | 太高会暴露内部代码和破坏沉浸 |
| `focus_fallback_target` | Primary visible shell action | fixed semantic target | 没有 fallback 会出现键盘死区 | 过度跳焦会让玩家迷失 |
| `continue_validation_strictness` | strict | strict | 不严格会进入损坏或不兼容会话 | 过度严格可能锁住可迁移旧存档 |
| `background_suspend_policy` | immediate suspend | immediate / delayed <= 1s | 延迟过长会产生后台误推进 | 立即暂停可能打断过场，但 MVP 更安全 |
| `safe_close_request_window_ms` | best effort only, no guarantee | best effort | 假设一定保存会造成误导 | 过度等待可能被浏览器直接中断 |
| `resume_event_order_policy` | token-driven idempotent resume | token-driven | 直接按事件顺序处理会误触玩法输入 | 过度等待会让恢复显得卡住 |
| `audio_muted_persistence` | session-scoped, prompt non-blocking | session-scoped / persistent setting later | 太短会反复打扰玩家 | 太长可能隐藏玩家想恢复音频的入口 |

固定设计值：

- `first_resume_input_consumed` 必须为 true。
- `audio_soft_fail_allows_play` 必须为 true。
- `continue_preserved_locked_visible` MVP 必须为 true。
- `ephemeral_session_allowed` MVP 必须为 true，且进入前必须提示不会保存。
- `continue_validation_strictness` MVP 必须为 strict。
- `background_suspend_policy` MVP 使用 immediate suspend。
- `resume_event_order_policy` MVP 必须使用 token-driven idempotent resume。

需要后续产品决策的值：

- `shell_error_detail_level`: 面向玩家的 Web 发布版要显示多少可复制诊断信息。

## Visual/Audio Requirements

`平台与会话壳` 不要求独立资产规格，但它必须遵守《云海织航》的“航路修复主义”视觉语言：入口、加载、继续、恢复和错误状态应像可靠的航务/港口界面，而不是通用网页启动器。

最低视觉要求：

- `Ready` 入口必须让玩家一眼分清 `Start`、`Continue`、继续点锁定/不可用、临时会话提示。
- `Loading` 不能是空白屏；必须有轻量进度或等待反馈，让玩家知道壳层仍在工作。
- `ResumePending` 必须以克制、清楚的提示表达“点击或按键继续”，避免像错误弹窗。
- `RecoveryRequired` 与 `FatalBlocked` 必须视觉上区分：前者是可恢复，后者是不可继续。
- `PreservedLocked` 继续点应表现为“保留但不可进入”，不能让玩家误以为存档已消失。
- 错误和诊断信息不得直接显示内部代码、堆栈或 `ERR_*` 给普通玩家；开发版可以提供复制诊断入口。

最低音频要求：

- 首次音频激活成功后，可以播放极短、柔和的确认反馈。
- 音频未解锁或失败时，不得使用强警告音或阻断式音效。
- 标签页恢复后，环境声或音乐恢复应平滑，不应突然爆响。
- 无声继续必须是可接受体验；核心入口反馈不能只依赖声音。

音频状态语义：

| Audio State | Meaning | Player-Facing Behavior |
|---|---|---|
| `Pass` | 音频已解锁并可播放 | 允许极短柔和确认反馈，进入或恢复会话 |
| `SoftFail` | 本次可信手势未能解锁音频 | 显示非阻断提示，可选择无声继续 |
| `Muted` | 玩家已选择持久无声会话 | 不再阻断进入/恢复，只保留可手动启用音频入口 |
| `HardFail` | 音频系统初始化破坏核心运行或浏览器能力异常 | 进入安全错误态；不得用刺耳音效提示 |

## UI Requirements

壳层 UI 只覆盖入口、加载、音频提示、恢复提示和安全错误路径。常驻 HUD、航图、库存、修复、市场和玩法内提示属于后续 UI 系统。

必须包含的 UI 状态：

| UI State | Required Elements |
|---|---|
| `Loading` | 游戏名或壳层标识、加载/检查中反馈、必要时的慢加载提示 |
| `Ready` | `Start`、可用或不可用的 `Continue`、基础设置入口（如音量/语言后续可接入） |
| `AwaitingAudioActivation` | “点击或按键启用音频/继续”的非阻断提示，无声继续路径 |
| `SessionStarting` | 正在进入/恢复会话的反馈，防重复点击状态 |
| `ResumePending` | 明确的重新激活提示，第一下输入只恢复不触发玩法 |
| `RecoveryRequired` | 可理解错误摘要、Retry、New Session、Return Title |
| `FatalBlocked` | 不可继续说明、Retry 或刷新/返回建议、可复制诊断（开发/中高错误详情级别） |

交互规则：

- 键盘/鼠标必须可完成所有壳层流程。
- 每个壳层 UI 状态必须有默认 focus anchor；focus 丢失时必须回退到当前状态的主操作。
- Hover 只能用于补充说明，不能承载关键操作。
- `Start` / `Continue` 在处理中必须去重或禁用，防止并行会话。
- `Continue = PreservedLocked` 时必须显示原因摘要或可进入详情，不得只灰掉按钮。
- 存储不可用但允许临时会话时，必须在进入前给出明确提示。

壳层 focus / overlay 所有权：

| UI State | Focus Owner | Default Focus Anchor | Escape / Back |
|---|---|---|---|
| `Loading` | Shell | none; status is passive | none unless cancel is safe |
| `Ready` | Shell | `Continue` if `Enabled`; `PreservedLocked` detail if locked; otherwise `Start` | settings close returns to primary action |
| `AwaitingAudioActivation` | Shell audio prompt | primary confirm / enable audio action | 无声继续或 Return Title |
| `SessionStarting` | Shell progress overlay | cancel/back only if safe | cancel returns to `Ready` only before session token commits |
| `ResumePending` | Shell resume overlay | Resume / Reactivate | Return Title |
| `RecoveryRequired` | Shell recovery overlay | Retry | New Session or Return Title |
| `FatalBlocked` | Shell fatal overlay | Retry if available, otherwise diagnostics/refresh guidance | Return/refresh guidance |

壳层 overlay 可见时，所有玩法输入、HUD 快捷键和下游 UI hover/click 都必须被拦截。`modal_blocked = true` 的来源只能是壳层 overlay 或壳层确认流程；下游 HUD 不得自行把壳层主状态切成恢复、错误或启动状态。

## Acceptance Criteria

- **GIVEN** 游戏处于 `Ready` 且 `audio_gate = Pass` 或 `Muted`，**WHEN** 玩家选择 `Start`，**THEN** 壳层创建一个 `Start` in-flight token 并进入 `SessionStarting`，在进入 `SessionActive` 前不接收任何玩法输入。
- **GIVEN** 游戏处于 `Ready` 且 `audio_gate` 需要可信用户手势，**WHEN** 玩家选择 `Start` 或 `Continue`，**THEN** 壳层必须在该手势中尝试音频解锁；若仍需确认，则进入 `AwaitingAudioActivation`，不得直接开放玩法输入。
- **GIVEN** 游戏处于 `AwaitingAudioActivation`，**WHEN** 音频解锁成功，**THEN** `audio_gate` 变为 `Pass`，状态转入 `SessionStarting`。
- **GIVEN** 游戏处于 `AwaitingAudioActivation`，**WHEN** 音频解锁失败但玩家选择无声继续，**THEN** `audio_gate` 变为 `Muted`，状态转入 `SessionStarting`，并保留非阻断启用音频入口。
- **GIVEN** 当前存在旧会话或旧继续点，**WHEN** 玩家选择 `Start`，**THEN** 系统创建新的会话意图，不沿用旧会话上下文，且不修改现有继续点。
- **GIVEN** 玩家选择 `Continue`，**WHEN** `continue_availability = Enabled` 且入口条件通过，**THEN** 壳层创建一个 `Continue` in-flight token 并进入 `SessionStarting`。
- **GIVEN** 玩家选择 `Continue`，**WHEN** `continue_availability = PreservedLocked`，**THEN** 壳层不得进入 `SessionStarting`，必须显示锁定原因、Return Title 和 New Session，且不得删除或覆盖继续点。
- **GIVEN** 玩家选择 `Continue`，**WHEN** `continue_availability = Hidden`，**THEN** 入口不得显示可用 Continue，也不得生成默认会话冒充可继续进度。
- **GIVEN** 游戏处于 `SessionActive`，**WHEN** 标签页隐藏、窗口失焦、`visibilitychange hidden` 或浏览器暂停，**THEN** 游戏进入 `BackgroundSuspended`，玩法输入停止，世界推进停止。
- **GIVEN** 游戏处于 `SessionActive`，**WHEN** `visibilitychange hidden`、`pagehide`、浏览器丢弃页面或刷新前事件发生，**THEN** 壳层只请求已预编码 marker / lightweight flush，不得请求完整 safe checkpoint，并且后续重新进入时必须从 `Booting` / `Loading` 重新验证继续点。
- **GIVEN** `pageshow.persisted=true` 表示 BFCache 恢复，**WHEN** 页面回到前台，**THEN** 壳层必须进入 `ResumePending` 并重新执行恢复检查，不得直接回到 `SessionActive`。
- **GIVEN** `beforeunload` 或 `unload` 触发，**WHEN** 壳层处理关闭前信号，**THEN** 不得启动新序列化、迁移、备份提升或阻塞式保存。
- **GIVEN** 游戏从后台返回并进入 `ResumePending`，**WHEN** 页面已可见但玩家尚未显式重新激活，**THEN** 玩法输入仍然被阻断，第一下输入只用于重新激活，不得触发普通玩法动作。
- **GIVEN** 游戏处于 `ResumePending`，**WHEN** 玩家按下键盘或鼠标作为恢复操作，**THEN** 该输入只能用于重新激活和同手势音频恢复，不能同时触发任何普通玩法动作。
- **GIVEN** 游戏处于 `ResumePending`，**WHEN** 页面已回前台、挂起 token 有效、玩家完成明确重新激活、焦点恢复且内容域可用，**THEN** 状态转入 `SessionActive`。
- **GIVEN** 游戏处于 `ResumePending`，**WHEN** 挂起 token 无效、内容域不可恢复或恢复检查失败，**THEN** 状态转入 `RecoveryRequired`，不得开放玩法输入。
- **GIVEN** 游戏处于 `ResumePending`，**WHEN** 玩家选择 Return Title，**THEN** 壳层返回 `Ready` 或安全标题状态，且不把返回输入传给玩法层。
- **GIVEN** 内容域状态为 `UNLOADED`、`LOADING` 或 `PARTIAL`，**WHEN** 玩家尝试进入或恢复会话，**THEN** 壳层保持加载/等待反馈，不得显示半完整可行动入口。
- **GIVEN** 内容域状态为 `FAILED`，**WHEN** 玩家尝试进入或恢复会话，**THEN** 壳层进入 `RecoveryRequired`，允许 Retry / New Session / Return Title。
- **GIVEN** 内容域状态为 `VERSION_INCOMPATIBLE`，**WHEN** 玩家尝试进入或恢复会话，**THEN** 壳层进入 `FatalBlocked` 或版本不兼容安全态，不得进入玩法。
- **GIVEN** 必需内容域集合中同时存在 `FAILED` 和 `LOADING`，**WHEN** 计算聚合 `required_content_domain_status`，**THEN** 结果必须为 `FAILED`。
- **GIVEN** 必需内容域集合中任一域为 `VERSION_INCOMPATIBLE`，**WHEN** 计算聚合 `required_content_domain_status`，**THEN** 结果必须为 `VERSION_INCOMPATIBLE`。
- **GIVEN** 任一失败路径被触发，**WHEN** 失败被处理，**THEN** 已存在的有效继续点必须保持原样，不得被删除、覆盖、降级或自动清空。
- **GIVEN** 基础资源、内容域状态和会话元数据检查完成，**WHEN** 检查全部通过，**THEN** 状态转入 `Ready`，并显示可交互的 `Start` / `Continue` 入口。
- **GIVEN** 核心资源缺失、构建不兼容、缺少必需 Web runtime、缺少可用渲染上下文或内容域版本不兼容，**WHEN** 加载失败，**THEN** 状态转入 `FatalBlocked`，只保留安全错误态与重试/返回类入口，不得进入玩法。
- **GIVEN** 游戏处于 `SessionStarting`，**WHEN** 会话上下文、内容域、可继续性和输入焦点全部通过，**THEN** 状态转入 `SessionActive`。
- **GIVEN** 游戏处于 `SessionStarting`，**WHEN** 继续会话失败、内容域失败、存储状态不支持继续或会话元数据损坏，**THEN** 状态转入 `RecoveryRequired`，不得进入玩法。
- **GIVEN** 游戏处于 `BackgroundSuspended`，**WHEN** 页面恢复可见且可交互，**THEN** 状态转入 `ResumePending`，但仍不得接受普通玩法输入。
- **GIVEN** 不存在继续点，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `Hidden`。
- **GIVEN** 继续点存在且完整性、内容域都通过，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `Enabled`。
- **GIVEN** 继续点存在但完整性失败或内容域不匹配，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `PreservedLocked`，且继续点仍保持存在。
- **GIVEN** 会话状态为 `Active`、页面在前台、输入焦点正常且无模态阻挡，**WHEN** 计算 `input_gate`，**THEN** 结果必须是 `Open`。
- **GIVEN** 会话状态为 `Suspended` 且页面已回前台，**WHEN** 计算 `input_gate`，**THEN** 结果必须是 `Reacquire`。
- **GIVEN** `resume_readiness` 所需任一条件缺失，**WHEN** 计算恢复就绪，**THEN** 结果必须为 false。
- **GIVEN** 基础加载完成、内容域通过、入口焦点已准备、音频门为 `Pass` 或 `Muted`，且会话意图为 `Start`，**WHEN** 计算 `entry_readiness`，**THEN** 结果必须为 true。
- **GIVEN** 会话意图为 `Continue` 且 `continue_availability != Enabled`，**WHEN** 计算 `entry_readiness`，**THEN** 结果必须为 false。
- **GIVEN** 没有硬门槛失败但音频门为 `SoftFail` 或存储能力为 `EphemeralOnly`，**WHEN** 计算 `failure_severity`，**THEN** 结果必须为 `SoftFail`。
- **GIVEN** 存储能力为 `WriteLocked` 且没有其他硬门槛失败，**WHEN** 计算 `failure_severity`，**THEN** 结果必须为 `SoftFail`，并且壳层必须显示新进度无法可靠保存的受控路径。
- **GIVEN** 内容域失败类型为 `Recoverable`，**WHEN** 计算 `failure_severity`，**THEN** 结果必须为 `RecoverableFail`，并且壳层必须进入 `RecoveryRequired` 而不是 `FatalBlocked`。
- **GIVEN** 玩家选择 `Continue` 且 `continue_availability = PreservedLocked`，**WHEN** 计算 `failure_severity`，**THEN** 结果必须为 `RecoverableFail`，并显示锁定原因路径。
- **GIVEN** 任一硬门槛失败，包括基础加载失败、内容域致命失败、`Continue = Hidden`、Resume 不就绪或音频 `HardFail`，**WHEN** 计算 `failure_severity`，**THEN** 结果必须为 `HardFail`。
- **GIVEN** 继续点不存在，**WHEN** 渲染入口，**THEN** `Continue` 必须隐藏或不可选，且不得出现伪装成可继续的默认会话。
- **GIVEN** 继续点存在但结构损坏、校验失败、版本不匹配或内容域标识不匹配，**WHEN** 渲染入口，**THEN** `Continue` 必须保持为锁定/禁用态并给出可理解提示，且继续点不得被自动删除或覆盖。
- **GIVEN** 浏览器存储不可用或写入被阻止，**WHEN** 玩家选择 `Start`，**THEN** 壳层必须先显示临时会话无保存提示；玩家确认后可进入临时会话，但不得生成持久化继续点。
- **GIVEN** 浏览器存储 API、后端 probe、配额、写入 roundtrip、策略或旧档读取状态发生变化，**WHEN** 壳层收到浏览器/JS 侧信号，**THEN** 壳层只把 raw `persistence_probe` 传给存档系统；`storage_capability` 必须使用存档系统返回的 `PersistentAvailable` / `WriteLocked` / `EphemeralOnly`，壳层不得本地计算。
- **GIVEN** 存档系统返回 `storage_capability=WriteLocked` 且 `continue_availability=Enabled`，**WHEN** 玩家检查 `Continue`，**THEN** 壳层必须允许进入已验证旧 Continue，同时显示“新进度当前无法可靠保存”的受控路径，不得隐藏或覆盖旧继续点。
- **GIVEN** 存档系统返回 `storage_capability=EphemeralOnly`，**WHEN** 玩家选择 `Start`，**THEN** 壳层必须先显示临时会话无保存提示；玩家确认后可进入临时会话，但不得生成持久化继续点。
- **GIVEN** 玩家在 `SessionStarting` 或 `ResumePending` 中连续重复点击 `Start` / `Continue` / 重新激活，**WHEN** 多次输入发生，**THEN** 系统只接受第一次有效意图，其余输入必须去重，不得并行创建两个会话。
- **GIVEN** 壳层 overlay 可见，**WHEN** 玩家使用鼠标、键盘或快捷键操作，**THEN** 焦点和输入必须停留在壳层 overlay，不得传入 HUD 或玩法层。
- **GIVEN** `Start` / `Continue` / `Resume` in-flight token 已创建，**WHEN** 相同入口再次被触发，**THEN** 壳层不得创建第二个 token 或第二个会话上下文。
- **GIVEN** `Ready`、`AwaitingAudioActivation`、`ResumePending`、`RecoveryRequired` 或 `FatalBlocked` 任一 UI 状态显示，**WHEN** 只使用键盘操作，**THEN** 玩家必须能到达主操作、返回/退出操作和可用的错误详情/无声继续入口。

## Open Questions

- **Owner: Technical Direction**; **Target: ADR**; 已定设计约束为 custom HTML shell 或等价 JS shim 捕获浏览器生命周期信号，并经 Godot `JavaScriptBridge` / 平台适配层传入壳层；ADR 只决定具体绑定、初始化顺序和测试桩实现。
- **Owner: Technical Direction**; **Target: ADR**; 后台暂停使用全局暂停还是局部进程模式控制？
- **Resolved by 本地存档与世界状态持久化 GDD**; 继续点损坏、版本不兼容、可迁移旧存档、`PreservedLocked` / `WriteLocked` / 临时试航文案归属已由 Persistence GDD 固定；后续 UI GDD 只负责布局与组件化呈现。
- **Owner: Audio / UX**; **Target: feedback/audio semantic GDD**; 音频未解锁时的非阻断提示应使用什么视觉/文案语言？
- **Owner: UX / Accessibility**; **Target: UI GDD / UX spec**; 壳层状态变化、错误和恢复提示的 screen reader / live region 行为如何定义？
