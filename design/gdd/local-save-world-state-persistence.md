# 本地存档与世界状态持久化

> **Status**: In Design
> **Author**: User + Codex
> **Last Updated**: 2026-05-09
> **Implements Pillar**: 规划先于冒险; 世界会回应照料; 飞艇是家，不只是载具
> **System Index**: `design/gdd/systems-index.md`
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-04-28
> **Design Review**: APPROVED WITH CAVEATS 2026-04-28; revision passes address formula ownership, Snapshot Package contract, persistence invariants, SaveLocked / WriteLocked reachability, atomic promotion, lifecycle, UX/focus requirements, performance telemetry, and testability.

> **Platform Pivot Note**: ADR-0019 supersedes this GDD's original Web persistence assumptions. Active MVP persistence targets desktop Godot 4.6.2 .NET/C# using `user://` / local filesystem semantics behind a C# service boundary. Browser storage, IndexedDB, JavaScriptBridge, `pagehide`, and Web quota requirements below are historical unless restated here as desktop storage / focus / quit requirements.

## Overview

`本地存档与世界状态持久化` 是《云海织航》的连续性保障系统。它负责把资源、模块、航线状态、修复状态、村镇/市场状态、探索状态、设置和安全继续点序列化到本地存档，并在 Start / Continue / Suspend / Resume 等会话节点中验证、恢复或锁定这些数据。这个系统不拥有运行时世界状态，也不替代各领域系统的状态管理；它只负责保存领域系统声明的可持久化快照、恢复到最近安全继续点、处理版本迁移，并在内容 ID、Schema 或桌面存储能力不可靠时给出安全结果。玩家不会直接“玩”这个系统，但会通过它相信自己的飞艇、修复过的灯塔、已稳定的航线和带回的材料不会因为退出应用、崩溃重启或隔天回来而消失；世界会等他们回来，照料过的痕迹会继续存在。

## Player Fantasy

玩家不应该感觉自己在管理存档，而应该感觉这个空海世界会稳稳接住自己的离开与归来。退出应用、重启游戏、隔天回来，或从一次窗口恢复中继续时，玩家期待看到的不是一串技术状态，而是熟悉的飞艇仍然可靠，带回的材料仍被安放，已修复的灯塔仍亮着，已稳定的航线仍然成立。

这个系统服务的是一种克制的安全感：玩家相信自己的照料不会白费，世界不会因为现实中的短暂离开而散开。保存提示、继续入口、恢复提示和锁定原因可以被玩家直接看见，但它们的语气必须像可靠的港口记录，而不是冷冰冰的数据管理界面。理想体验是“返航不是重来，而是续上生活”：玩家重新进入时，飞艇像家一样完整接住他们，世界也保留他们曾经修补、连接和照料过的痕迹。

这个幻想必须保持边界清楚。`本地存档与世界状态持久化` 不承诺模拟一个离线运转的世界，也不拥有世界状态本身；它承诺的是可靠延续、可解释恢复和安全失败。当存档不可用、版本不兼容或继续点被锁定时，玩家也应该感觉进度被谨慎保护，而不是被系统随意丢弃或静默覆盖。

## Detailed Rules

### Core Rules

1. `本地存档与世界状态持久化` 只管理存档工件、继续点 metadata、完整性校验和迁移记录；它不拥有活运行时世界状态。
2. 领域系统拥有自己的状态真相。任何可保存状态必须由领域系统导出为显式 `Snapshot Package`，存档系统不能遍历场景树、全局单例或活对象图来抓取状态。
3. 存档工件分为三层：外壳 metadata、领域快照包、完整性信息。
4. 存档只保存稳定 ID、枚举、数值、状态标志、Schema/快照版本和迁移所需上下文；不得保存 Node 引用、对象句柄、场景路径、显示名、hover/selection、动画中段或表现层临时状态。
5. MVP 使用单一玩家档案、一个最近安全继续点和一个自动备份副本；多槽位不属于 MVP。
6. `Continue` 不是“存在文件即可进入”，而是继续点 metadata、完整快照、内容域版本、稳定 ID 引用和完整性校验全部通过后才输出 `Enabled`。
7. 所有持久写入必须走 `Staging -> Verify -> Promotion`。新快照完成写入、读回验证和内容兼容校验前，旧的 `Safe Continue Point` 必须保持不变。
8. 设置、游戏进度和继续点摘要必须逻辑分离。设置写入失败不得污染游戏进度；游戏进度损坏不得删除可用设置。
9. 存档系统只在稳定边界创建安全继续点：Hub 停靠点、航线选择提交后、探索/搜撤结算后、修复结果提交后、交易库存落定后、设置应用确认后。
10. `Suspend`、窗口失焦、最小化、退出请求或关闭前事件只能触发 best-effort 快照请求；只有写入完成并读回验证通过，才能升级为新的 `last_verified_checkpoint`。
11. 如果某个领域系统报告 `Blocked`、`Not Ready` 或处于未结算中间态，存档系统必须跳过本次 promotion，并继续保留上一个安全点。
12. 桌面存储能力的权威判定由本系统拥有。平台壳/平台适配层只提供原始 `persistence_probe` 信号和桌面生命周期信号；本系统计算 `storage_capability = PersistentAvailable / WriteLocked / EphemeralOnly`，并通过 `query_continue_state` 与保存状态查询返回给平台壳呈现。平台壳不得根据本地文件存在、单次 API 成功或旧探测结果重算该能力。
13. `EphemeralOnly` 是降级 fallback，不是正常战役模式。它只允许临时试航、构建检查、教程级低承诺操作和不会改变正式世界状态的演示动作；不得允许玩家提交世界修复、长期资源积累、关系/村镇变化、飞艇家园布置或任何会被玩家理解为“世界记住了”的正式照料结果。
14. 损坏与版本不兼容必须分开处理：损坏进入 `Quarantined`；可解析但当前不能恢复的版本/内容不兼容进入 `Locked` 或迁移流程。
15. 迁移必须显式、单向、可中止。迁移在 staging 副本上执行，成功并验证后 promotion；失败时原工件保持锁定，不被改写。
16. `SaveLocked`、`PreservedLocked`、`RecoveryRequired` 必须携带原因码，不能互相替代。
17. `continue_availability` 的最终判定由本系统拥有。`平台与会话壳` 只能通过 `query_continue_state` 读取并呈现 `Enabled` / `PreservedLocked` / `Hidden`，不得用本地公式重新计算或覆盖该结果。
18. 每个领域系统导出的 `Snapshot Package` 必须满足本 GDD 的最小契约；缺失契约字段、版本字段、稳定 ID 解析结果或领域错误码时，不得进入 promotion。
19. `Staging -> Verify -> Promotion` 是语义契约，不依赖最终存储后端。ADR-0019 的活动后端为 Godot `user://` / 本地文件系统封装；实现必须保证：旧 `Safe` 在新工件完成写入、读回、校验和兼容验证前保持不变。
20. 窗口失焦、最小化、退出请求、进程关闭和后台挂起永远不是正确性路径，只能触发 best-effort 请求；任何未完成或未读回验证的写入都不得成为 `last_verified_checkpoint`。
21. 自动备份是独立工件，不得与主继续点共用同一记录 ID。主档损坏且备份可验证时，备份提升必须走显式 `BackupPromoting -> Safe` 路径，并把旧主档标记为 `Quarantined`。
22. 最近安全继续点摘要必须是玩家可理解的世界事实摘要，至少包含最近安全地点/会话边界、一个关键世界变化或飞艇状态、保存时间；不得只显示内部版本、路径、checksum 或 slot metadata。
23. MVP 必须记录开发诊断指标：快照字节数、可用磁盘/预算余量、序列化耗时、写入耗时、读回耗时、checksum 耗时、promotion 结果、失败原因码、退出/挂起 best-effort 结果和备份提升结果。
24. 飞艇家园相关快照的最小可恢复范围包括：当前 Hub 停靠点/入口、舱室或生活空间稳定 ID、玩家可见的储物/模块/伙伴驻点状态、关键交互锚点状态，以及能表达“这仍是我的家”的最低生活痕迹。具体摆放、装饰和扩展字段由 `飞艇家园 Hub` GDD 细化。
25. 设置是独立 `settings` snapshot artifact，游戏进度是独立 `progress` snapshot artifact。二者必须有独立版本、完整性校验、最近已验证指针和恢复路径。
26. 会话中途进入 `SaveLocked` 时，存档系统必须立即打开写屏障：后续世界修复、长期资源积累、关系/村镇变化、飞艇家园布置和任何会被玩家理解为永久照料结果的提交都必须暂停、拒绝或转入明确临时模式。玩家必须看到 Return Title、Retry Save Capability 或 Enter Temporary Flight 之类的安全选择；旧 `Safe` 继续点不得被覆盖。
27. `settings` 与 `progress` 的 `storage_capability` 可以不同步计算，但对正式进度提交必须以 `progress.storage_capability` 为准。settings 可写而 progress 不可写时，不得把游戏进度显示为已保存；progress 可写而 settings 不可写时，设置回滚不得影响进度继续点。
28. `current_generation`、manifest pointer、`last_verified_checkpoint`、`checkpoint_summary`、backup promotion result 和所有 reason code 都是本系统拥有的 durable metadata。平台壳和 UI 只能读取这些字段，不得写入或推导替代值。
29. `settings` 与 `progress` 必须分别维护 artifact state、generation、manifest pointer、checksum、backup 和 reason code。`continue_availability` 只由 `progress` artifact 的恢复结果决定；settings artifact 损坏或不可写只能回退设置，不得隐藏、锁定或删除可恢复的 progress Continue。
30. `query_continue_state()` 是本系统对平台壳的唯一 Continue API，输出至少包含 `continue_availability`、`storage_capability`、`write_barrier_mode`、`reason_code`、`checkpoint_summary`、`last_verified_checkpoint`、`current_generation` 和 `artifact_kind=progress`。平台壳不得绕过该 API 读取 manifest 或 generation。

### Snapshot Package Contract

`Snapshot Package` 是领域系统与存档系统之间唯一合法的可持久化边界。它不是运行时对象副本，也不是任意 Dictionary dump；它是领域系统主动声明的、可校验的状态包。

每个 `Snapshot Package` 至少包含：

| Field | Required | Owner | Description |
|---|---|---|---|
| `domain_id` | Yes | Domain system | 领域稳定 ID。MVP 保存域使用 `settings`、`progress.resources`、`progress.intel`、`progress.airship`、`progress.routes`、`progress.exploration`、`progress.world-repair`、`progress.settlement-market`；这些保存域可以映射到注册表内容域，但不得假装等同于注册表 `owner_domain`。 |
| `snapshot_schema_version` | Yes | Domain system | 该领域快照 Schema 版本，用于兼容和迁移。 |
| `content_domain_versions` | Yes | Registry + domain | 本快照引用的内容域版本集合。 |
| `stable_id_refs` | Yes | Domain system | 本快照依赖的稳定 ID 列表；恢复前必须全部解析或进入迁移/锁定路径。 |
| `payload` | Yes | Domain system | 只包含稳定 ID、枚举、数值、状态标志和迁移上下文。 |
| `domain_state` | Yes | Domain system | `Ready` / `Blocked` / `NotReady` / `Settling`；非 `Ready` 不得 promotion。 |
| `domain_error_code` | When blocked | Domain system | 可诊断原因码，例如 `ERR_DOMAIN_SETTLING`、`ERR_REQUIRED_ID_MISSING`。 |
| `migration_hint` | Optional | Domain system / registry | 可选迁移提示；不等于执行迁移。 |

包级校验规则：

- `domain_id` 必须唯一；同一保存工件中同一领域不能提供两个互相竞争的包。
- `payload` 中不得包含 Node、Resource 实例、对象句柄、场景路径、显示名、翻译文本、hover/selection、动画中段或临时表现状态。
- `payload_allowed_types_only` 的设计层白名单为：bool、int、finite float、string、enum string、stable ID string、array、dictionary/null marker；array 与 dictionary 只能递归包含同一白名单类型。C# DTO / Godot `Variant` bridge 实现不得夹带 `Object`、`Node`、`Resource`、`Callable`、`Signal`、`RID`、`NodePath`、`PackedScene`、活引用或任何引擎句柄。最终 API 类型可由 ADR / Control Manifest 收窄，但不得放宽本白名单。
- dictionary key 必须是 string，key 必须使用 canonical bytewise ascending order 编码；array 保持领域系统声明顺序；dictionary/array 禁止循环引用和共享引用语义；float 禁止 `NaN`、`Infinity`、`-Infinity`；null 只能使用明确的 `null` marker，不能用缺字段暗示 null。
- 所有 string 必须先规范化为 Unicode NFC，再编码为 UTF-8；stable ID 与 dictionary key 必须额外满足 ASCII lowercase `kind.slug` / snake-case 风格约束。规范化后重复的 dictionary key 必须让 `snapshot_package_validity=false`。
- float 必须以 IEEE-754 binary64 语义进入 canonical codec，并以规范化十进制文本或等价固定宽度二进制编码写入；`-0.0` 必须规范化为 `0.0`。空 payload 允许存在，但必须显式编码为空 dictionary，不能省略 `payload` 字段。
- checksum 覆盖 canonical encoded payload、snapshot schema version、content domain versions、stable ID refs、artifact kind、artifact generation 和 manifest pointer target；不得只覆盖裸 payload。
- `stable_id_refs` 必须能在 `内容数据与状态注册表` 中解析为 `Active` 或可迁移的 `Deprecated`。`Retired`、`NOT_FOUND`、`UNLOADED`、`VERSION_INCOMPATIBLE` 都不是有效保存包；旧档恢复时遇到 `Retired` 只能进入 `PreservedLocked` 或迁移/人工修复路径，不得被判定为 `snapshot_package_validity=true`。
- 任何领域报告 `Blocked`、`NotReady` 或 `Settling` 时，本次保存可以保留为 `Dirty` 或失败的 `Staging`，但不得 promotion。
- 存档系统只校验、调度、迁移和恢复包；不解释资源容量、航线发现、修复条件、市场库存或 Hub 生活语义。

### Durable Metadata Contract

每个 artifact kind 分别维护 durable metadata。MVP artifact kind 为 `settings` 与 `progress`；以下字段都必须以 `artifact_kind` 作为 key 的一部分持久化，例如 `progress.current_generation` 与 `settings.current_generation` 不得共用同一记录。

| Field | Owner | Meaning |
|---|---|---|
| `artifact_kind` | Persistence | `settings` 或 `progress`。 |
| `current_generation` | Persistence | 当前被 `query_continue_state` 和恢复流程视为权威的 generation ID。 |
| `manifest_pointer` | Persistence | 指向 `current_generation` 的小型权威提交记录；逻辑提交面，不要求文件系统原子 rename。 |
| `last_verified_checkpoint` | Persistence | 最近一次完成写入、读回、checksum、Schema、稳定 ID 和领域 blocker 校验的安全继续点。 |
| `checkpoint_summary` | Persistence + domains | 从 `last_verified_checkpoint` 派生的玩家可理解摘要；领域系统提供世界事实，存档系统记录时间、边界和来源。 |
| `reason_code` | Persistence | `SaveLocked`、`PreservedLocked`、`RecoveryRequired`、`Quarantined` 等状态的机器可读原因。 |
| `probe_generation` | Platform adapter + Persistence | 最近一次 capability probe 的 ID、时间戳、触发来源和 TTL。 |
| `backup_generation` | Persistence | 最近可验证自动备份 generation；不等同于 current。 |

`current_generation` 只能在 `promotion_success=true` 后切换。`manifest_pointer` 更新失败时必须继续指向旧 generation。`last_verified_checkpoint` 只能指向已验证 generation，不能指向 `Staging`、未完成 `Verify`、shutdown-only marker 或失败迁移副本。

generation 必须单调递增或使用可比较的 commit sequence；启动恢复时若 manifest pointer 指向的 generation 低于已记录的 `last_verified_checkpoint.generation`、与 checksum/summary 不匹配，或看起来是旧 pointer replay / rollback，必须拒绝该 pointer，保留最近已验证 generation 或进入 `RecoveryRequired`。

`settings` 与 `progress` 的非干扰规则：

| Failure | Required Result |
|---|---|
| `settings` write / verify / promotion 失败，`progress` Safe 可用 | settings 回退到最近已验证设置；`progress.continue_availability` 不受影响。 |
| `progress` write / verify / promotion 失败，settings Safe 可用 | progress 回退到最近已验证进度；settings 不被删除或降级。 |
| `settings` Quarantined，`progress` Safe 可用 | 只重置或回退设置；Continue 仍按 progress 计算。 |
| `progress` Quarantined，settings Safe 可用 | Continue 不得 `Enabled`，但 settings 可保留。 |

### Desktop Persistence Contract

ADR-0019 的活动实现路径为桌面 Godot .NET/C#。本节原始 Web contract 中关于 custom HTML shell、JS shim、IndexedDB、BFCache、`visibilitychange` 和 `pagehide` 的要求不再约束 MVP；保留它们只用于解释旧评审语境。活动桌面要求如下：

| Contract Area | Requirement |
|---|---|
| Storage boundary | C# persistence service owns all snapshot write/read/verify/promotion operations behind a narrow Godot-facing API. |
| Desktop file boundary | Use `user://` / local filesystem paths through Godot .NET-compatible APIs or a wrapped C# file service. Every promotion still requires write -> flush/close -> reopen/readback -> checksum before pointer swap. |
| Lifecycle boundary | Focus loss, minimize, pause, and quit requests are best-effort triggers only. Correctness comes from previously verified safe checkpoints, not from last-moment shutdown saves. |
| Capability probe | The persistence system computes `PersistentAvailable` / `WriteLocked` / `EphemeralOnly` from real write/read/checksum probes and migration checks. |
| Atomicity | Existing `Safe` remains valid until the new generation has fully passed verify and promotion. |

#### Historical Web Contract (Superseded By ADR-0019)

最终持久化后端由 ADR 决定，但本 GDD 要求以下后端无关语义：

| Contract | Requirement |
|---|---|
| Bootstrap lifecycle | custom HTML shell / JS shim 必须在 Godot engine start 前注册 `visibilitychange`、`pagehide`、`pageshow`、focus/blur 监听，并缓存最早到达的事件。Godot 启动后，平台适配层必须创建并持有 `JavaScriptBridge.create_callback()` 返回引用直到页面结束；释放 callback 前必须注销 JS listener。壳层读取缓存事件并转换为幂等 lifecycle token，避免启动早期漏掉隐藏/关闭信号。 |
| Capability probe | 平台壳/平台适配层收集 `raw_persistent_api_ok`、`storage_backend_probe_ok`、`existing_archive_read_class`、`quota_ok`、`quota_reserve_ok`、`write_roundtrip_ok`、`working_set_budget_class`、`policy_forces_ephemeral`、`probe_timestamp`、`probe_trigger`；本系统计算 `storage_capability`。fresh install 没有旧 archive 时，`existing_archive_read_class` 必须是 `NotApplicable`，不得因此变成 `EphemeralOnly`。`OS.is_userfs_persistent()`、API 存在、IndexedDB 存在或 FileAccess 可打开只能作为 hints；只有当前页面/iframe/隐私策略上下文中的真实写入、flush/close、读回和 checksum roundtrip 可以让 `write_roundtrip_ok=true` 并支持 `PersistentAvailable`。 |
| Probe cache | probe 结果必须带 TTL。MVP TTL：boot probe 30s；resume / pageshow probe 10s；write failure、readback mismatch、quota failure、policy change、iframe visibility/cookie policy change、`pageshow` after any suspension 必须立即失效并重探。过期 probe 只能保守进入 `WriteLocked` 或重新探测，不得作为 `PersistentAvailable` 依据。 |
| Quota reserve | `quota_ok` 必须以 persisted artifact bytes 为持久存储口径，并覆盖主工件、staging 工件、自动备份、metadata、迁移临时副本、后端膨胀和最小安全余量。 |
| Peak working set | `quota_reserve_ok` 必须按最坏同时驻留估算保存峰值工作集：encoded in-memory artifact bytes + readback copy bytes + checksum buffer bytes + serialization transient bytes + migration temp bytes + backend inflation bytes。若估算超过安全余量，输出 `WriteLocked`；若底层存储或读取能力也不可靠，输出 `EphemeralOnly`。 |
| Working-set budget | `available_working_set_bytes` 必须来自平台适配层内存预算 probe、ADR 固定 conservative cap，或二者取较小值。若无法获得预算，MVP 固定 fallback 为 16 MiB；若估算峰值超过 fallback，`quota_reserve_ok=false`。 |
| Staging | 新工件先写入 staging 标识或 staging key；旧 `Safe` 不被覆盖。 |
| Readback verify | 写入后必须读回同一 staging 工件，校验结构、checksum、Schema、稳定 ID 和内容域版本。 |
| Promotion | 只有读回验证成功后，存档系统才更新 manifest / pointer / 最近安全继续点指针或主工件；promotion 失败时旧 `Safe` 保持不变。平台壳不得执行 pointer swap。 |
| Web file boundary | 若 ADR 使用 Godot `user://` / `FileAccess`，目录结构必须在 boot probe 阶段预创建并验证。每次写入必须显式完成 write -> flush/close -> reopen/readback；任一 `FileAccess.open`、目录创建、flush/close 或 readback 失败都使该 artifact 的本次 promotion 失败。 |
| Atomic commit surface | 后端 ADR 可以选择单 manifest pointer、事务式交换或后端原子写入，但设计合同固定为“单一 current pointer / generation 是权威提交面”。这是逻辑提交面，不依赖 `DirAccess.rename()`、copy 或底层文件系统事务。`current_generation` 切换前，新 staging 不得被 `query_continue_state` 看作 current；切换失败时必须继续读取旧 generation。 |
| Backup promotion | 备份提升顺序固定为：验证备份 -> 写入 promoted staging 或新 generation -> 读回 verify -> 切换 current pointer -> 标记旧主档 `Quarantined`。不得直接用备份覆盖损坏主档；任一步失败都保持旧主档隔离、备份保留、外部 Continue 不得显示为 `Enabled`。 |
| Durability caveat | `pagehide` / 关闭前事件只能提交 best-effort，不得承诺完成；下一次启动必须重新读回验证。 |
| Pagehide hard boundary | `visibilitychange hidden` 是 Web-first 的主要“会话可能结束”信号；`pagehide` 是 fallback；`beforeunload` / `unload` 不得作为正确性路径。上述路径不得启动新的完整序列化、迁移、备份提升、readback、checksum 或诊断文本生成；只能请求已经准备好的预编码 staging 轻量 flush / marker。超出 `pagehide_marker_budget_ms` 后必须放弃本次推进，保留旧 `last_verified_checkpoint`。 |
| Lifecycle wiring | MVP 需要 custom HTML shell 或等价 JS shim 通过 `JavaScriptBridge` / 平台适配层把 `visibilitychange`、`pagehide`、`pageshow`、focus/blur 和浏览器 capability 信号传给平台壳。平台壳拥有浏览器事件 token 与输入门禁；存档系统拥有保存、验证、promotion、备份提升、`storage_capability` 和恢复判定。 |
| Diagnostics | 每次保存、迁移、恢复、备份提升和 probe 都必须生成开发诊断摘要，不记录完整玩家存档内容。诊断摘要记录结构化指标；可复制文本报告可以延迟或按需生成，不能阻塞保存热路径。 |

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| `Absent` | 没有持久化继续点 | 创建新 staging 快照 | 对平台壳输出 `Hidden` |
| `EphemeralOnly` | 平台壳报告持久存储不可用，或旧档读取/写入 roundtrip 均不可靠，或平台策略强制临时会话 | 当前页面会话结束或存储能力恢复后重新验证 | 只允许临时试航和非正式操作，不生成持久 Continue，不提交正式世界照料结果 |
| `Dirty` | 领域系统已有可持久化变更但尚未保存 | 到达稳定保存边界 | 标记需要快照，不立即写盘 |
| `Staging` | 稳定边界触发保存，快照正在收集和写入 staging 工件 | 写入完成进入 `Verify`；写入失败、被阻塞或页面关闭 | 不覆盖旧安全点 |
| `Verify` | staging 工件已写入，正在读回并校验结构、checksum、Schema、稳定 ID、内容域版本和领域 blocker | 验证成功进入 `Safe` / promotion；验证失败回到旧 `Safe` 或 `Dirty`，问题工件可隔离 | 不覆盖旧安全点，不显示保存成功 |
| `Safe` | 新工件完成写入、读回、完整性和兼容校验 | 下一次保存、迁移需求或锁定条件出现 | 可作为最近安全继续点 |
| `Migrating` | 旧工件可解析但版本落后，且存在迁移链 | 迁移成功、失败或被中止 | 在副本上执行迁移，不改写原工件 |
| `Locked` | 工件存在但当前构建不能直接恢复 | 迁移成功或继续保持锁定 | 对平台壳输出 `PreservedLocked` 和原因码 |
| `Quarantined` | 工件损坏、不可信或解析失败 | 人工处理或保留诊断 | 不作为继续点，不静默修补 |
| `BackupPromoting` | 主工件损坏但自动备份通过完整性、版本和稳定 ID 校验 | 备份提升成功、失败或被阻塞 | 在不改写损坏主工件的前提下，把备份提升为新的 `Safe` |
| `SaveLocked` | 旧 `Safe` 可读或可保留，但当前不能可靠写入新持久进度 | 存储能力恢复、玩家返回标题，或玩家明确进入临时试航 | 打开写屏障，阻止新持久保存和正式世界变更，不代表旧存档损坏 |
| `RecoveryRequired` | 当前流程不能自动继续但存在恢复路径 | Retry、New Session、Return Title 或迁移完成 | 平台壳显示恢复选择，不开放玩法输入 |

对平台壳暴露的继续状态：

| External State | Meaning |
|---|---|
| `Enabled` | 存在 `Safe` 且兼容的最近安全继续点 |
| `PreservedLocked` | 继续点存在但当前不能进入；必须显示原因，不能删除或覆盖 |
| `Hidden` | 没有持久继续点，或当前只有临时会话；壳层不得显示可用 Continue，可按 UI 规范隐藏入口或显示非焦点占位 |

### Interactions with Other Systems

| System | Persistence Requests / Receives | Persistence Provides | Boundary |
|---|---|---|---|
| `平台与会话壳` | `start_new`、`continue_requested`、`suspend_requested`、`resume_requested`、`query_continue_state`、存储能力结果 | `Enabled` / `PreservedLocked` / `Hidden`、恢复结果、锁定原因、最近安全继续点摘要 | 壳层拥有入口和呈现，不拥有存档格式，也不得重新计算 Continue 可用性 |
| `内容数据与状态注册表` | 稳定 ID 解析、Schema 版本、Deprecated/Retired 生命周期、迁移提示 | 存档中使用的 ID 引用和版本需求 | 注册表不保存玩家状态；存档不复制静态定义 |
| `资源、货物与容量` | 资源/货物/容量快照、恢复确认、迁移结果 | 保存/恢复调度和快照版本记录 | 资源数量与容量语义由该领域拥有 |
| `玩家知识与情报` | 已知/未知、传闻、风险揭示快照 | 保存稳定 ID 引用和恢复顺序 | 存档不判断情报真假或发现规则 |
| `飞艇家园 Hub` | Hub 停靠点/入口、生活空间稳定 ID、储物/模块/伙伴驻点状态、关键交互锚点状态、最低生活痕迹快照 | 最近安全停靠点和可识别家园状态恢复 | 存档不保存动画、hover、临时 UI 状态；家园语义由 Hub 领域拥有 |
| `飞艇模块与船体状态` | 模块安装、船体损伤、维修状态快照 | 保存/恢复调度 | 模块效果语义由领域系统拥有 |
| `航图与航线规划` | 航线可用性、选择状态、路线解锁快照 | 继续点摘要和恢复调度 | 航图展示与选择逻辑不归存档拥有 |
| `探索 / 搜撤场景` | 已结算探索状态、撤离结果、战利品归属快照 | 只在稳定结算点保存 | 未结算中段不得 promotion |
| `世界修复与解锁` | 修复完成、解锁结果、世界状态标志快照 | 保存结果型状态 | 存档不决定修复条件或解锁语义 |
| `空港 / 村镇状态与集市交易` | 摊位库存、开放性、村镇活跃度快照 | 保存/恢复调度 | 市场价格/库存规则由该领域拥有 |

## Formulas

本系统不定义战斗、经济或成长数值公式；它定义的是保存、继续、恢复和迁移的判定规则。求值顺序为：

`persistence_probe -> quota_reserve_ok -> storage_capability -> snapshot_package_validity -> migration_required -> safe_checkpoint_eligibility -> promotion_success -> backup_failover_outcome -> restore_readiness -> continue_availability -> migration_outcome`

The `storage_capability` formula is defined as:

`storage_capability = PersistentAvailable if raw_persistent_api_ok AND storage_backend_probe_ok AND existing_archive_read_class IN {Readable, NotApplicable} AND quota_ok AND quota_reserve_ok AND write_roundtrip_ok AND NOT policy_forces_ephemeral; else WriteLocked if raw_persistent_api_ok AND storage_backend_probe_ok AND existing_archive_read_class IN {Readable, NotApplicable} AND NOT policy_forces_ephemeral; else EphemeralOnly`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `raw_persistent_api_ok` | `A` | bool | true/false | 平台壳从底层浏览器或存储 API 归一化出的可持久化 API 可用结果。 |
| `storage_backend_probe_ok` | `B` | bool | true/false | 后端最小读写入口是否可调用；不同于旧 archive 是否存在。 |
| `existing_archive_read_class` | `L` | enum | `Readable` / `Unreadable` / `NotApplicable` | 旧 manifest / current pointer / 最近安全继续点 metadata 是否可读；fresh install 或无旧档时为 `NotApplicable`。 |
| `quota_ok` | `Q` | bool | true/false | 当前可用配额足以容纳最小保存工件。 |
| `quota_reserve_ok` | `H` | bool | true/false | 当前可用配额足以容纳主工件、staging、自动备份、metadata、迁移临时副本和最小安全余量。 |
| `write_roundtrip_ok` | `R` | bool | true/false | 写入测试后读回校验成功。 |
| `policy_forces_ephemeral` | `P` | bool | true/false | 隐私模式、用户策略或平台限制强制进入临时会话。 |
| `storage_capability` | `S` | enum | `PersistentAvailable` / `WriteLocked` / `EphemeralOnly` | 平台壳输出给存档系统的归一化能力结果。 |

**Output Range:** `PersistentAvailable`、`WriteLocked` 或 `EphemeralOnly`。`WriteLocked` 不隐藏旧 `Safe`，但会阻止新持久保存并进入 `SaveLocked` 写屏障。
**Examples:** 如果 fresh install 没有旧档，`existing_archive_read_class=NotApplicable`、其他能力全部通过，则 `storage_capability = PersistentAvailable`。如果 `raw_persistent_api_ok=true`、`storage_backend_probe_ok=true`、`existing_archive_read_class=Readable`、`quota_ok=false` 且 `policy_forces_ephemeral=false`，则 `storage_capability = WriteLocked`。

The `quota_reserve_ok` formula is defined as:

`persisted_artifact_bytes = encoded_memory_artifact_bytes * backend_persistence_inflation_factor`

`required_bytes = persisted_artifact_bytes + staging_artifact_bytes + backup_artifact_bytes + metadata_bytes + migration_temp_bytes + safety_margin_bytes`

`peak_working_set_bytes = encoded_memory_artifact_bytes + readback_copy_bytes + checksum_buffer_bytes + serialization_transient_bytes + migration_temp_bytes + backend_working_set_inflation_bytes`

`safety_margin_bytes = max(encoded_memory_artifact_bytes * quota_reserve_multiplier, 512 KiB)`

`quota_reserve_ok = available_storage_bytes >= required_bytes AND available_working_set_bytes >= peak_working_set_bytes`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `encoded_memory_artifact_bytes` | `E` | int | >= 0 | 本次编码后内存中 canonical artifact 字节数，作为工作集估算基准。 |
| `persisted_artifact_bytes` | `P` | int | >= 0 | 后端实际持久化字节数估算，用于配额口径；可与内存编码大小不同。 |
| `main_artifact_bytes` | `M` | int | >= 0 | 当前主工件或预计主工件字节数。 |
| `staging_artifact_bytes` | `S` | int | >= 0 | staging 工件预计字节数。 |
| `backup_artifact_bytes` | `B` | int | >= 0 | 自动备份工件预计字节数。 |
| `metadata_bytes` | `D` | int | >= 0 | manifest、pointer、summary、reason code 和版本 metadata 字节数。 |
| `migration_temp_bytes` | `G` | int | >= 0 | 迁移临时副本最坏驻留字节数；不迁移时为 0。 |
| `readback_copy_bytes` | `R` | int | >= 0 | 读回 verify 时同时驻留的副本字节数。 |
| `checksum_buffer_bytes` | `C` | int | >= 0 | checksum 计算需要的缓冲字节数。 |
| `serialization_transient_bytes` | `T` | int | >= 0 | 序列化过程最坏瞬时分配字节数。 |
| `backend_persistence_inflation_factor` | `F` | float | >= 1.0 | manifest、backup、base64 或 metadata 包装等导致的持久化膨胀系数。 |
| `backend_working_set_inflation_bytes` | `K` | int | >= 0 | 后端适配层、JS bridge 或编码包装引入的额外工作集字节数。 |
| `quota_reserve_multiplier` | `X` | float | >= 0 | 追加安全余量倍数，MVP 默认为 0.5。 |
| `available_storage_bytes` | `A` | int | >= 0 | 平台壳/适配层估算出的可用持久存储字节数。 |
| `available_working_set_bytes` | `W` | int | >= 0 | 平台适配层或 ADR conservative cap 提供的工作集预算字节数；未知时使用 16 MiB fallback。 |

**Output Range:** true/false。false 时若 `existing_archive_read_class IN {Readable, NotApplicable}`，`storage_capability` 必须走 `WriteLocked`；若 `existing_archive_read_class=Unreadable` 或后端/API/策略不可用，走 `EphemeralOnly`。当 `available_storage_bytes = required_bytes` 且 `available_working_set_bytes = peak_working_set_bytes` 时结果为 true；任一值低 1 byte 时结果为 false。
**Example:** 如果 `encoded_memory_artifact_bytes=1 MiB`、`backend_persistence_inflation_factor=1.5`、`staging_artifact_bytes=1.5 MiB`、`backup_artifact_bytes=1.5 MiB`、`metadata_bytes=64 KiB`、`migration_temp_bytes=0`、`safety_margin_bytes=512 KiB`，则 `required_bytes=5.0625 MiB`；可用存储少于该值时 `quota_reserve_ok=false`。

The `snapshot_package_validity` formula is defined as:

`snapshot_package_validity = package_present AND required_fields_present AND schema_version_known AND content_domain_versions_compatible AND stable_id_refs_listed AND stable_id_resolution_ok AND payload_allowed_types_only AND domain_state = Ready AND NOT domain_error_blocking`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `package_present` | `P` | bool | true/false | 领域系统是否提供快照包。 |
| `required_fields_present` | `F` | bool | true/false | `domain_id`、`snapshot_schema_version`、`content_domain_versions`、`stable_id_refs`、`payload`、`domain_state` 是否存在。 |
| `schema_version_known` | `V` | bool | true/false | 该领域快照版本是否被当前构建或迁移链识别。 |
| `content_domain_versions_compatible` | `C` | bool | true/false | 快照引用的内容域版本与当前注册表直接兼容，或存在明确迁移路径。 |
| `stable_id_refs_listed` | `I` | bool | true/false | 快照依赖的稳定 ID 是否显式列出。 |
| `stable_id_resolution_ok` | `R` | bool | true/false | 所有稳定 ID 解析为 `Active`，或解析为带迁移路径的 `Deprecated`；`Retired`、`NOT_FOUND`、`UNLOADED`、`VERSION_INCOMPATIBLE` 均为 false。 |
| `payload_allowed_types_only` | `T` | bool | true/false | payload 是否只包含允许持久化的稳定数据类型。 |
| `domain_state` | `D` | enum | `Ready` / `Blocked` / `NotReady` / `Settling` | 领域系统导出的保存状态。 |
| `domain_error_blocking` | `B` | bool | true/false | 领域错误码是否阻止保存或恢复。 |
| `snapshot_package_validity` | `E` | bool | true/false | 快照包是否可进入保存/恢复流程。 |

**Output Range:** true/false。任一领域包为 false 时，本次 promotion 失败，旧 `Safe` 保持不变。

The `migration_required` formula is defined as:

`migration_required = snapshot_schema_version_older OR content_domain_versions_require_migration OR stable_id_resolution_requires_migration`

`direct_restore_compatible = version_compatible AND content_domain_versions_directly_compatible AND stable_id_resolution_class = AllActive`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `snapshot_schema_version_older` | `S` | bool | true/false | 快照 Schema 版本低于当前直接恢复版本。 |
| `version_compatible` | `V` | bool | true/false | 快照 Schema 与当前构建可直接恢复，不需要迁移。 |
| `content_domain_versions_require_migration` | `C` | bool | true/false | 任一内容域版本需要迁移才能恢复。 |
| `stable_id_resolution_requires_migration` | `I` | bool | true/false | 任一稳定 ID 解析为可迁移的 `Deprecated`。 |
| `content_domain_versions_directly_compatible` | `D` | bool | true/false | 内容域版本可被当前构建直接恢复。 |
| `stable_id_resolution_class` | `R` | enum | `AllActive` / `DeprecatedMigratable` / `RetiredLocked` / `MissingOrIncompatible` | 恢复所需稳定 ID 的聚合解析结果。 |

**Output Range:** true/false。任何 `RetiredLocked`、`MissingOrIncompatible` 或版本不兼容但无迁移路径的组合不得落到 `AlreadyCurrent`；必须进入 `PreservedLocked` 或 `Quarantined`。

The `safe_checkpoint_eligibility` formula is defined as:

`safe_checkpoint_eligibility = storage_capability = PersistentAvailable AND trigger_class = StableBoundary AND domain_ready AND snapshot_package_validity AND NOT write_barrier_active`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `storage_capability` | `S` | enum | `PersistentAvailable` / `WriteLocked` / `EphemeralOnly` | 平台壳归一化后的存储能力。 |
| `trigger_class` | `T` | enum | `StableBoundary` / `Suspend` / `Pagehide` / `Resume` / `Recovery` / `Migration` | 本次保存触发来源。 |
| `domain_ready` | `D` | bool | true/false | 所有可持久化领域系统都已结算，无 `Blocked` / `Not Ready` / 中间态。 |
| `snapshot_package_validity` | `E` | bool | true/false | 所有参与保存的领域快照包是否满足最小契约。 |
| `write_barrier_active` | `B` | bool | true/false | 当前是否存在写屏障，例如 `Migrating`、`Locked`、`Quarantined`、`SaveLocked`、`RecoveryRequired`。 |
| `safe_checkpoint_eligibility` | `E` | bool | true/false | 是否允许生成安全继续点。 |

**Output Range:** true/false。`Pagehide` 不满足 `StableBoundary`，只能触发 best-effort 保存请求，不应直接晋升为安全继续点。
**Example:** 如果 `storage_capability=PersistentAvailable`、`trigger_class=Pagehide`、`domain_ready=true` 且 `write_barrier_active=false`，则 `safe_checkpoint_eligibility = false`。

The `promotion_success` formula is defined as:

`promotion_success = staging_written AND readback_verified AND checksum_ok AND schema_compatible AND stable_id_resolved AND no_domain_blockers`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `staging_written` | `W` | bool | true/false | staging 工件已经完成写入。 |
| `readback_verified` | `R` | bool | true/false | 读回内容与 staging 写入结果一致。 |
| `checksum_ok` | `C` | bool | true/false | 完整性校验通过。 |
| `schema_compatible` | `V` | bool | true/false | 快照版本 / Schema 与当前构建兼容。 |
| `stable_id_resolved` | `I` | bool | true/false | 所有必需稳定 ID 都能在注册表中解析。 |
| `no_domain_blockers` | `D` | bool | true/false | 没有领域系统阻塞 promotion。 |
| `promotion_success` | `P` | bool | true/false | `Staging -> Verify -> Promotion` 是否成功。 |

**Output Range:** true/false。任何一项失败都必须保留旧的 `Safe` 工件不变。
**Example:** 如果所有输入均为 true，则 `promotion_success = true`，新快照可以晋升为最近安全继续点。

The `restore_readiness` formula is scoped to a single `artifact_kind`. For `continue_availability`, use `artifact_kind=progress`.

`restore_readiness(artifact_kind) = archive_present[artifact_kind] AND artifact_state[artifact_kind] = Safe AND integrity_ok[artifact_kind] AND version_compatible[artifact_kind] AND stable_ids_resolved[artifact_kind] AND NOT migration_required[artifact_kind] AND NOT quarantined[artifact_kind]`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `artifact_kind` | `K` | enum | `settings` / `progress` | 当前被评估的 artifact。 |
| `archive_present[artifact_kind]` | `A` | bool | true/false | 是否存在可读取的持久存档工件。 |
| `artifact_state[artifact_kind]` | `S` | enum | `Absent` / `EphemeralOnly` / `Dirty` / `Staging` / `Safe` / `Migrating` / `Locked` / `Quarantined` / `SaveLocked` / `RecoveryRequired` | 当前工件状态。 |
| `integrity_ok` | `I` | bool | true/false | 校验和、结构和 metadata 都未损坏。 |
| `version_compatible` | `V` | bool | true/false | 当前构建可直接恢复该工件。 |
| `stable_ids_resolved` | `R` | bool | true/false | 恢复所需稳定 ID 可解析。 |
| `migration_required` | `M` | bool | true/false | 当前工件需要迁移才能进入。 |
| `quarantined` | `Q` | bool | true/false | 工件是否已被隔离。 |
| `restore_readiness` | `E` | bool | true/false | 是否满足进入世界的恢复条件。 |

**Output Range:** true/false。
**Example:** 如果存在 `Safe` 工件、完整性通过、版本兼容、稳定 ID 可解析，且不需要迁移也未隔离，则 `restore_readiness = true`。

The `continue_availability` formula is defined as:

`continue_availability = Enabled if progress.storage_capability IN {PersistentAvailable, WriteLocked} AND progress.archive_present AND restore_readiness(progress); else PreservedLocked if progress.storage_capability IN {PersistentAvailable, WriteLocked} AND progress.archive_present AND NOT restore_readiness(progress); else Hidden`

Ownership note: this formula is authoritative for the external Continue state. `平台与会话壳` consumes the result and renders it; it does not recompute this enum from its own partial view of content or integrity state.

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `progress.storage_capability` | `S` | enum | `PersistentAvailable` / `WriteLocked` / `EphemeralOnly` | progress artifact 的存储能力。 |
| `progress.archive_present` | `A` | bool | true/false | 是否存在持久 progress 继续点工件。 |
| `restore_readiness(progress)` | `R` | bool | true/false | progress 是否可以直接恢复进入。 |
| `continue_availability` | `C` | enum | `Enabled` / `PreservedLocked` / `Hidden` | 对平台壳输出的继续入口状态。 |

**Output Range:** `Enabled`、`PreservedLocked` 或 `Hidden`。`PreservedLocked` 只表示工件存在但暂时不能进入，不是可继续状态。`WriteLocked` 可以继续显示和进入已验证旧档，但必须同时阻止新持久提交并显示保存受限原因。`EphemeralOnly` 不生成可靠持久 Continue，因此在没有可读旧档时输出 `Hidden`，并应被玩家界面表达为临时试航或无保存模式，而不是普通继续入口。
**Example:** 如果 `storage_capability=WriteLocked`、`archive_present=true` 且 `restore_readiness=true`，则 `continue_availability = Enabled`，但新持久保存必须进入 `SaveLocked` 写屏障。

The `migration_outcome` formula is defined as:

`migration_outcome = Quarantined if NOT parse_ok OR NOT integrity_ok; else Upgraded if migration_required AND migration_chain_available AND staging_ok AND verify_ok AND promotion_success; else PreservedLocked if migration_required AND (NOT migration_chain_available OR NOT staging_ok OR NOT verify_ok OR NOT promotion_success); else AlreadyCurrent if NOT migration_required AND direct_restore_compatible; else PreservedLocked`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `migration_required` | `M` | bool | true/false | 当前工件是否必须迁移才能恢复。 |
| `migration_chain_available` | `C` | bool | true/false | 是否存在从旧版本到当前版本的完整迁移链。 |
| `staging_ok` | `S` | bool | true/false | 迁移是否成功在 staging 副本上完成写入。 |
| `verify_ok` | `V` | bool | true/false | 迁移后的副本是否读回验证通过。 |
| `promotion_success` | `P` | bool | true/false | 迁移后的副本是否成功 promotion。 |
| `parse_ok` | `A` | bool | true/false | 旧工件是否可解析。 |
| `integrity_ok` | `I` | bool | true/false | 旧工件是否未损坏。 |
| `direct_restore_compatible` | `D` | bool | true/false | 当前构建可不迁移直接恢复该工件，且内容域版本与稳定 ID 解析均直接兼容。 |
| `migration_outcome` | `O` | enum | `Upgraded` / `PreservedLocked` / `Quarantined` / `AlreadyCurrent` | 迁移判定结果。 |

**Output Range:** `Upgraded`、`PreservedLocked`、`Quarantined` 或 `AlreadyCurrent`。解析失败或完整性失败必须进入 `Quarantined`；需要迁移但迁移链缺失必须进入 `PreservedLocked`；不需要迁移但不能直接恢复的组合也必须进入 `PreservedLocked`，不得落到 `AlreadyCurrent`。
**Example:** 如果 `migration_required=true`、`migration_chain_available=false`、`parse_ok=true` 且 `integrity_ok=true`，则 `migration_outcome = PreservedLocked`。

The `backup_failover_outcome` formula is scoped to `artifact_kind=progress` for Continue recovery. settings backup recovery can run independently and must not affect Continue.

`backup_direct_restore_ok = backup_parse_ok AND backup_integrity_ok AND backup_structure_ok AND backup_version_compatible AND backup_stable_ids_resolved AND NOT backup_migration_required`

`backup_failover_outcome = BackupPromoted if main_usable=false AND backup_present AND backup_direct_restore_ok; else BackupPreservedLocked if main_usable=false AND backup_present AND backup_parse_ok AND backup_integrity_ok AND backup_structure_ok AND (backup_migration_required OR NOT backup_version_compatible OR NOT backup_stable_ids_resolved); else NoUsableBackup if main_usable=false; else NotNeeded`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `main_usable` | `M` | bool | true/false | 主继续点是否 parse、structure、integrity、version 和 stable ID 检查全部可用。 |
| `backup_present` | `B` | bool | true/false | 自动备份工件是否存在。 |
| `backup_parse_ok` | `A` | bool | true/false | 自动备份是否可解析。 |
| `backup_integrity_ok` | `I` | bool | true/false | 自动备份是否通过结构与 checksum 校验。 |
| `backup_structure_ok` | `S` | bool | true/false | 自动备份必需 metadata、manifest pointer、generation 和 payload 结构是否完整。 |
| `backup_version_compatible` | `V` | bool | true/false | 当前构建是否可直接恢复备份。 |
| `backup_stable_ids_resolved` | `R` | bool | true/false | 备份依赖的稳定 ID 是否可解析。 |
| `backup_migration_required` | `G` | bool | true/false | 备份是否需要迁移才能恢复。 |
| `backup_failover_outcome` | `O` | enum | `BackupPromoted` / `BackupPreservedLocked` / `NoUsableBackup` / `NotNeeded` | 备份回退判定结果。 |

**Output Range:** `BackupPromoted`、`BackupPreservedLocked`、`NoUsableBackup` 或 `NotNeeded`。`BackupPromoted` 后，旧主工件进入 `Quarantined`，备份副本必须先写入 promoted staging / new generation，读回验证成功并切换 `current_generation` 后，才成为唯一可用 `Safe`。`BackupPreservedLocked` 表示备份存在且可信但不能直接恢复，例如需要迁移或稳定 ID 需要迁移；此时 Continue 必须是 `PreservedLocked`，不得显示为 `Enabled`。任一步失败时 `continue_availability != Enabled`。
**Example:** 如果主档 parse fail，备份完整但 `backup_migration_required=true`，则 `backup_failover_outcome=BackupPreservedLocked`，旧主档隔离，备份保留但不自动进入。

### Degraded Mode and Write Barrier Contract

| Mode | Entry | Allowed | Forbidden | Exit |
|---|---|---|---|---|
| `WriteLocked` | `storage_capability=WriteLocked`，旧 `Safe` 可读但新写入、配额或 reserve 不可靠 | 读取/进入已验证旧 Continue；查看标题摘要；修改只影响当前内存的临时预览 | 新持久 progress promotion；世界修复提交；长期资源落定；关系/村镇变化；飞艇家园布置提交 | capability probe 恢复为 `PersistentAvailable`，或玩家 Return Title / Enter Temporary Flight |
| `SaveLocked` | 会话中途从可写降为不可可靠写入，或 promotion/write/readback 出现能力型失败 | Retry Save Capability；Return Title；Enter Temporary Flight；继续查看当前内存状态 | 任何会让玩家理解为正式世界记住的提交；覆盖旧 `Safe`；显示保存成功 | 重试成功回到正常保存；返回标题；进入 `EphemeralOnly` 临时试航 |
| `EphemeralOnly` | API/后端不可用、旧档不可读、策略强制临时，或玩家从 `SaveLocked` 二次确认进入 Temporary Flight | 临时试航、教程级非正式动作、构建检查、不会写入正式世界状态的预览 | 持久 Continue；正式 progress promotion；世界修复、长期资源、关系/村镇、飞艇家园布置提交 | 页面结束，或重新启动后 capability probe 恢复 |

写屏障 API 最低合同：

| Query / Command | Owner | Result |
|---|---|---|
| `query_write_barrier()` | Persistence | 返回 `barrier_active`、`mode`、`reason_code`、`allowed_actions`、`forbidden_commit_classes`。 |
| `request_retry_save_capability()` | Platform shell -> Persistence | 触发 probe TTL 失效和重探；成功后清除 `SaveLocked`，失败保持 barrier。 |
| `enter_temporary_flight()` | Platform shell -> Persistence | 二次确认后进入 `EphemeralOnly`，所有正式 commit class 被拒绝。 |
| `return_title_preserve_safe()` | Platform shell -> Persistence | 返回标题并保留旧 `Safe`、旧 `current_generation` 和 `last_verified_checkpoint`。 |

正式 commit class 至少包括：`WorldRepairCommit`、`LongTermResourceCommit`、`RelationshipCommit`、`SettlementMarketCommit`、`AirshipHomeLayoutCommit`、`RouteUnlockCommit`、`ExplorationSettlementCommit`。写屏障激活时，领域系统必须在提交前查询 barrier；若无法查询，默认拒绝正式提交。

## Edge Cases

- **If 底层桌面存储 API 看起来可用，但新写入 reserve 或 write roundtrip probe 失败，且旧档 read probe 仍通过**: `storage_capability = WriteLocked`。旧 `Safe` 可继续验证和呈现，但新持久保存进入 `SaveLocked` 写屏障。
- **If fresh install 没有旧档，但存储后端、配额、reserve 和 write roundtrip 全部通过**: `existing_archive_read_class=NotApplicable`，`storage_capability = PersistentAvailable`。不得因为没有旧 manifest 就进入 `EphemeralOnly`。
- **If 平台壳策略、底层 API 缺失、存储路径不可调用、权限不足或旧档存在但 read probe 失败**: `storage_capability = EphemeralOnly`。当前会话只能进入临时试航或无保存模式，`continue_availability = Hidden`，且不得生成新的持久 Continue。
- **If 退出请求、关闭、失焦或隐藏发生时，任何可持久化领域系统仍处于 `Blocked`、`Not Ready` 或未结算中间态**: 只允许 best-effort 保存请求，不得 promotion；最近一次已验证的 `Safe` 继续点保持不变。
- **If 退出/挂起触发时 staging 已开始写入，但 Verify 尚未完成**: 本次 staging 作废，不得升级为 `last_verified_checkpoint`；系统回落到原来的 `Safe` 或 `Dirty` 状态。
- **If 写入成功但读回校验失败**: 该次 promotion 失败，旧 `Safe` 保持不变；若写入后的主工件无法确认与读回一致，则该工件进入 `Quarantined`，不得静默修补。
- **If 启动恢复时，已有档案的读回失败、checksum 不通过、结构解析失败或 manifest pointer 不可信**: 该工件进入 `Quarantined` 并触发 backup failover；只有备份提升完成后才能重新输出 `Enabled`，否则 `continue_availability=Hidden` 或 `PreservedLocked`，不能当作可恢复进度。
- **If 旧档需要迁移，且迁移链完整，staging / verify / promotion 全部成功**: `migration_outcome = Upgraded`；旧工件不被直接覆盖，升级后的副本成为新的 `Safe`。
- **If 旧档需要迁移，但迁移链缺失，或迁移过程中任一步失败**: `migration_outcome = PreservedLocked`；原工件保留不改写，`Continue` 不能直接进入，且不得输出 `AlreadyCurrent`。
- **If 恢复所需稳定 ID 被标记为 `Deprecated`，且注册表提供唯一替代映射**: 允许按替代 ID 进入显式迁移流程；迁移成功前不得把该档案当作直接可继续进度。
- **If 恢复所需稳定 ID 被标记为 `Retired`，或已从注册表移除且没有替代映射**: `snapshot_package_validity=false`，`restore_readiness = false`；外部状态必须是 `PreservedLocked`，并要求玩家走新游戏或显式修复/迁移入口。
- **If 设置写入失败但进度写入成功，或进度写入失败但设置写入成功**: 失败的一侧必须独立回滚，另一侧保持最近已验证值，二者不得互相删除、覆盖或连带进入 `Quarantined`。
- **If 主档损坏，但自动备份满足完整性、版本兼容和稳定 ID 解析**: 进入 `BackupPromoting`，备份提升为唯一可用 `Safe`，主档进入 `Quarantined`，继续入口按备份状态重新计算；提升结果必须写入诊断摘要。
- **If 主档损坏，且自动备份不存在或备份也不满足恢复条件**: 主档进入 `Quarantined`，`backup_failover_outcome = NoUsableBackup`，外部 Continue 不得显示为 `Enabled`。
- **If 会话处于 `EphemeralOnly` 临时模式**: 允许玩家在当前桌面进程内临时试航、检查构建或体验非正式流程；不得提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置。退出或关闭后进度视为不可恢复，且 `Continue` 必须保持 `Hidden`。
- **If 玩家在已有 `Safe` 继续点存在时选择新游戏**: 必须创建新的会话上下文；旧继续点在新会话第一次成功 promotion 前保持原样。若新会话失败、取消或返回标题，旧档不得被删除或覆盖。

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| `平台与会话壳` | This depends on Platform Shell | 提供 `start_new`、`continue_requested`、`suspend_requested`、`resume_requested`、`safe_close_marker_requested`、raw `persistence_probe` signals、optional `working_set_budget_bytes` 和壳层恢复/错误入口；`storage_capability` 由本系统计算。 |
| `内容数据与状态注册表` | This depends on Registry | 提供稳定 ID、内容域版本、Schema 版本、Deprecated/Retired 生命周期、迁移提示和内容兼容性查询。 |
| `资源、货物与容量` | Resources/Cargo depends on this; this depends on its snapshots | 提供资源、货物、容量和携带状态的领域快照；存档系统负责保存/恢复调度，不解释容量或资源规则。 |
| `玩家知识与情报` | Intelligence depends on this; this depends on its snapshots | 提供已知/未知、传闻、风险揭示和情报条目的领域快照；存档系统只保存稳定 ID 和状态标志。 |
| `飞艇家园 Hub` | Hub depends on this; this depends on its snapshots | 提供玩家可恢复的 Hub 停靠/位置/交互锚点状态；存档系统不保存表现层状态。 |
| `飞艇模块与船体状态` | Ship state depends on this; this depends on its snapshots | 提供模块安装、船体损伤、维修状态快照；模块效果和损伤语义仍由领域系统拥有。 |
| `航图与航线规划` | Route map depends on this; this depends on its snapshots | 提供航线可用性、选择状态和路线解锁快照；存档系统提供最近安全继续点摘要。 |
| `探索 / 搜撤场景` | Exploration depends on this; this depends on its settled snapshots | 只在探索结算点保存撤离结果、战利品归属和场景状态；未结算中段不得成为安全继续点。 |
| `世界修复与解锁` | World repair depends on this; this depends on its snapshots | 提供修复完成、解锁结果和世界状态标志，确保照料痕迹可恢复。 |
| `空港 / 村镇状态与集市交易` | Settlement/market depends on this; this depends on its snapshots | 提供摊位库存、开放性、村镇活跃度和市场状态快照；交易/库存规则不归存档拥有。 |
| `UI / HUD / 航图界面` | UI consumes this indirectly | 显示保存提示、Continue 状态、`PreservedLocked` 原因和恢复选择；不直接修改存档工件。 |

依赖说明：本 GDD 已固定持久化边界、artifact 合约、Continue 判定和失败状态；下游系统 GDD 可以在各自设计中补充领域快照字段，但不得重新定义 promotion、manifest pointer、`storage_capability` 或 `query_continue_state()` 的权威来源。

## Tuning Knobs

| Knob | Default / MVP Intent | Safe Range | Too Low / Too Strict | Too High / Too Loose |
|---|---|---|---|---|
| `autosave_stable_boundary_only` | true | true | false 会允许中间态保存，增加坏档风险 | 不适用；MVP 固定为 true |
| `backup_artifact_count` | 1 automatic backup | 1-2 | 无备份时主档损坏后无法回退 | 备份过多增加磁盘写入和恢复复杂度 |
| `quota_reserve_multiplier` | 0.5x encoded memory artifact bytes, minimum 512 KiB | 0.25-1.0 | staging、备份、读回副本或迁移副本可能挤爆配额/内存 | 过高会过早进入 `WriteLocked` |
| `backend_persistence_inflation_factor` | 1.0 | 1.0-2.0 | 低估 manifest / metadata / backup wrapper 开销 | 过高会过早进入 `WriteLocked` |
| `backend_working_set_inflation_bytes` | 256 KiB | 0-2 MiB | 低估编码包装临时分配 | 过高会过早阻止保存 |
| `staging_write_timeout_seconds` | 3s | 1-8s | 正常桌面写入可能被过早判失败 | 玩家离开/恢复时等待过久 |
| `snapshot_encode_budget_ms` | 16ms target, 50ms warning | 8-100ms | 预算过严会误报正常低端机 | 预算过宽会让保存卡顿难定位 |
| `checksum_budget_ms` | 8ms target, 30ms warning | 4-60ms | 大快照容易误报 | checksum 成为主线程卡顿源 |
| `readback_verify_budget_ms` | 20ms target, 100ms warning | 10-150ms | 桌面磁盘抖动下误报 | 恢复/保存反馈显得迟钝 |
| `diagnostic_report_max_kb` | 32 KB copyable report | 8-128 KB | 太小会截断有用诊断 | 太大会增加复制与字符串构建成本 |
| `readback_verify_required` | true | true | false 会让“写入请求”伪装成“保存成功” | 不适用；MVP 固定为 true |
| `minimum_save_interval_seconds` | 5s between committed saves | 2-15s | 频繁写入造成磁盘与备份压力 | 太久会让稳定边界后的进度迟迟不落盘 |
| `shutdown_marker_budget_ms` | 20ms marker/flush only, no success promise | 0-50ms | 完全不尝试会错过已准备 marker | 过度依赖会错误承诺关闭前强保存 |
| `continue_validation_strictness` | strict | strict | 不严格会进入损坏或不兼容会话 | 不适用；MVP 固定为 strict |
| `migration_retry_limit` | 1 explicit retry per launch | 0-3 | 迁移抖动没有重试机会 | 反复迁移可能增加损坏和误导 |
| `diagnostic_detail_level` | player-safe summary + developer detail copy | low / medium / developer | 太低无法解释锁定原因 | 太高会把内部字段暴露给普通玩家 |
| `ephemeral_warning_frequency` | before session start and on exit attempt | start-only / start+exit | 玩家可能误以为进度会保存 | 反复弹窗会破坏进入节奏 |
| `max_snapshot_size_mb` | 2 MB MVP target | 1-8 MB | 太小会限制后续状态增长 | 太大增加磁盘写入、读回和加载压力 |
| `diagnostic_metrics_required` | true | true | false 会让存档问题难复现 | 不适用；MVP 固定为 true |
| `save_success_feedback_delay_ms` | 300ms minimum visible feedback | 150-800ms | 反馈闪过，玩家不信保存成功 | 停留太久显得打扰 |

固定设计值：

- `autosave_stable_boundary_only = true`
- `readback_verify_required = true`
- `continue_validation_strictness = strict`
- shutdown / suspend / focus-loss best-effort 永远不能承诺强保存完成
- `EphemeralOnly` 永远不能生成持久 Continue，也不能被文案包装成完整可恢复的正常游玩
- `diagnostic_metrics_required = true`

预算超限行为：

- encode、checksum 或 readback 超过 target 但低于 warning 时，保存仍可继续，但必须记录开发诊断 warning。
- 任一阶段超过 warning 时，本次仍以正确性优先完成或失败，但下一次自动保存必须延后至少 `minimum_save_interval_seconds`，并在开发诊断中标记 `PERF_SAVE_BUDGET_EXCEEDED`。
- 连续两次超过 warning 时，系统必须降低自动保存频率或提示开发诊断；不得把超预算写入包装成无问题的正常保存。
- shutdown / suspend best-effort 路径不得同步构建完整可复制诊断报告，只记录固定大小结构化指标，报告文本按需生成。

保存热路径统计口径：

- `save_hot_path_budget_ms` 由 encode + write + readback + checksum + promotion pointer update + structured diagnostics append 组成；MVP target 为 60ms，warning 为 180ms。
- 预算按单次保存的 wall-clock duration 记录，同时保留各阶段耗时；性能验收使用开发构建最近 20 次稳定边界保存的 p95。
- structured diagnostics 只能追加固定大小的标量、enum、reason code、generation ID、duration 和小型 metadata；不得同步字符串化完整报告，不得复制完整玩家存档 payload，不得在保存热路径分配超过 4 KiB 的诊断记录。`diagnostic_report_max_kb` 只限制按需复制报告，不属于保存热路径预算。
- shutdown / suspend best-effort 不纳入稳定保存成功率，只记录是否发出请求、是否已有预编码 staging、是否在预算内完成轻量 flush。

## Visual/Audio Requirements

`本地存档与世界状态持久化` 不需要独立场景资产，但需要一套克制、可靠、低打扰的保存/恢复反馈语言。视觉和音频反馈必须强化“世界被妥善保留”，不能制造焦虑，也不能把普通保存提示做成高危警报。

| Event | Visual Feedback | Audio Feedback | Priority |
|---|---|---|---|
| Stable autosave committed | 小型状态标记或短文本提示，确认“已保存”或等价语义；不遮挡操作 | 可选极短柔和确认音；必须可关闭 | High |
| Staging started | 轻量保存中状态，不承诺已完成 | None | Medium |
| Shutdown / suspend best-effort requested | 不显示“已保存成功”；若界面仍可见，只显示“正在保护最近进度”类语义 | None | High |
| EphemeralOnly session | 入口前显示清楚但不恐吓的“临时试航 / 本次不会留下正式进度”提示 | None or soft marker | High |
| Continue Enabled | Continue 入口显示最近安全继续点摘要 | Optional soft confirm on selection | High |
| Continue PreservedLocked | 锁定图标 + 原因摘要 + 可行动选项；强调“数据仍被保留” | No alarm; optional low warning tick | High |
| Quarantined artifact | 开发/诊断界面显示隔离状态、原因码和备份回退结果；普通玩家只看到安全错误摘要 | None | High |
| Backup promoted | 显示“已恢复到最近可用保存点”语义 | Optional soft recovery cue | Medium |
| Migration Upgraded | 显示“存档已更新，可继续”语义 | Optional soft confirm | Medium |
| Migration PreservedLocked | 显示“旧存档已保留，但需要修复/更新后继续” | None or low warning tick | High |
| SaveLocked | 显示“当前无法可靠保存”的原因和临时会话选择 | None | High |

视觉原则：

- 保存成功反馈必须短、清楚、可信，不要频繁抢焦点。
- `PreservedLocked` 必须表现为“保留但不可进入”，不能像删除、丢失或崩溃。
- 临时会话提示必须在进入前可见，不能只藏在设置或错误日志里。
- 普通玩家界面不显示 checksum、内部路径、栈信息或完整快照内容。
- 诊断界面可以显示 reason code、工件状态、迁移结果、备份回退结果和可复制摘要。
- 状态不能只靠颜色区分；必须结合图标、文字或线型。
- 音频永远不能作为唯一保存/失败反馈。

### Player-Facing Translation Layer

内部状态名不得直接作为普通玩家文案露出。UI 可以在开发构建显示 reason code，但玩家界面必须把技术状态翻译成符合“可靠港口记录”的温和、可行动语言。

| Internal State | Player-Facing Meaning | Copy Direction |
|---|---|---|
| `WriteLocked` | 旧航程记录可以继续，但新的正式进度当前不能可靠记录 | “可以从最近安全记录继续，但新的正式进度现在可能无法保存。建议先重试保存能力，或进入临时试航。” |
| `EphemeralOnly` | 当前只能临时试航，世界不会正式记住本次进度 | “当前无法可靠保存。你可以临时试航，但这次照料不会留下正式记录。” |
| `PreservedLocked` | 进度仍被保留，但现在不能安全进入 | “你的航程记录还在。现在需要检查后才能继续，你可以重试、返回标题，或开始一段不会覆盖旧记录的新航程。” |
| `Quarantined` | 某份记录不可信，已隔离保护 | “有一份记录需要检查；系统不会用它覆盖可用进度。” |
| `RecoveryRequired` | 需要玩家选择恢复路径 | “需要选择如何接回航程。可用记录会被保留，系统不会自动覆盖它。” |
| `SaveLocked` | 当前不能可靠写入新进度 | “现在无法可靠记录新的进度。你可以返回标题，或进入临时试航。” |
| `Migrating` | 正在谨慎更新旧记录 | “正在检查并更新航程记录。旧记录会保留到更新完成。” |
| `Upgraded` | 旧记录已更新，可继续 | “航程记录已更新，可以继续。” |
| `MigrationPreservedLocked` | 旧记录仍保留，但当前不能完成更新 | “旧航程记录还在。现在还不能安全更新，请稍后重试或返回标题。” |
| `TemporaryFlightConfirmed` | 已进入临时试航，旧安全记录仍保留，本次不会写入正式进度 | “已进入临时试航。旧航程记录仍被保留；本次操作不会留下正式进度。” |

文案禁止事项：

- 不向普通玩家显示 `EphemeralOnly`、`PreservedLocked`、`Quarantined`、`RecoveryRequired`、`Migrating`、`Upgraded` 等内部状态名。
- 不用“损坏”“失败”“fatal”“checksum”等词作为普通玩家主文案。
- 不承诺临时会话会被世界记住。
- 不把锁定状态写成存档已丢失。

## UI Requirements

| UI Surface | Purpose | Required Elements |
|---|---|---|
| Title / Ready Continue Entry | 告诉玩家是否能继续 | Continue Enabled / Hidden / PreservedLocked 状态、最近安全继续点摘要、锁定原因入口；`Hidden` 不可作为可聚焦 Continue |
| Save Feedback Toast | 确认稳定边界保存完成 | 保存成功、保存中、保存失败/临时会话提示；不显示未验证保存为成功 |
| PreservedLocked Detail | 解释为什么档案保留但不能进入 | 原因摘要、Retry、New Session、Return Title、可复制诊断（开发/中高详情级别） |
| Ephemeral Session Warning | 进入临时会话前确认 | “临时试航 / 本次不会留下正式进度”提示、继续临时试航、返回标题 |
| RecoveryRequired Screen | 当前流程不能自动恢复时给玩家选择 | Retry、New Session、Return Title、备份恢复结果、迁移状态；默认焦点在 Retry，若 Retry 不可用则在 Return Title |
| Migration Status | 显示旧档迁移进度和结果 | 使用 Player-Facing Translation Layer 文案显示检查中、已更新、已保留但不可继续、记录需检查；普通玩家界面不得直接显示 `Migrating`、`Upgraded`、`PreservedLocked`、`Quarantined` 内部状态名 |
| Developer Save Diagnostics | 开发期排查存档问题 | 工件状态、reason code、metadata 摘要、迁移步骤、备份状态、快照字节数、存储余量、encode/write/readback/checksum 耗时、shutdown/suspend 结果、复制报告 |

最近安全继续点摘要必须至少包含：

- 最近安全边界：例如停靠点、航线提交后、探索结算后、修复提交后或交易落定后。
- 一个玩家可识别的世界事实：例如灯塔已点亮、某航线已稳定、某模块已安装、某批材料已入库。
- 保存时间或相对时间。
- 如果来自备份提升，必须说明“已恢复到最近可用记录”，但不得使用内部 `BackupPromoted` 名称。

交互规则：

- `Continue = PreservedLocked` 时，入口必须可见但不可直接进入；玩家必须能打开原因详情。
- `Continue = Hidden` 时，不显示可操作 Continue。若布局需要占位，占位不得进入键盘焦点顺序，也不得读作可用按钮。
- 玩家选择 `New Session` 时，不得立即覆盖旧 `Safe` 继续点；UI 必须等新会话第一次成功 promotion 后才可把新档视为最近安全点。
- `EphemeralOnly` 进入前必须有明确确认；确认按钮文案必须说明“临时试航 / 无保存继续”语义，不能写成普通 Start 或 Continue。
- 保存成功提示只能在 `promotion_success = true` 后显示。
- shutdown marker 或 `suspend_requested` 不能显示“保存成功”，除非读回验证已经完成。
- `SaveLocked` overlay 可见时，正式世界变更提交按钮、修复确认、长期资源落定、关系/村镇变化和飞艇家园布置提交必须禁用或转为临时预览；默认操作为 Retry Save Capability 或 Return Title，临时试航必须二次确认。
- `SaveLocked` overlay 默认焦点为 Retry Save Capability；若当前 probe TTL 未过且重试不可用，默认焦点为 Return Title。Escape / Back 返回最近安全标题态，不关闭写屏障；live region 必须说明“旧安全记录仍被保留，新的正式进度现在不能可靠记录”。
- 玩家从 `SaveLocked` 选择 Enter Temporary Flight 时，必须二次确认并调用 `enter_temporary_flight()`；进入后 `mode=EphemeralOnly`，不得继续显示普通保存成功或普通 Continue 语义。
- 只用键盘必须能操作 Continue、锁定详情、Retry、New Session、Return Title 和诊断复制。
- 普通玩家界面使用温和、可行动文案，并通过 Player-Facing Translation Layer 翻译内部状态；开发构建可以显示详细 reason code。
- UI/HUD 系统最终拥有布局、焦点和视觉组件；本系统只规定状态语义和必须呈现的信息。

焦点与辅助功能要求：

| UI Surface | Default Focus | Escape / Back | Announcement |
|---|---|---|---|
| `Title / Ready Continue Entry` | `Continue` if `Enabled`; `PreservedLocked` detail button if locked; `Start` if `Hidden` | Back closes secondary panels; Escape returns focus to primary title action | 礼貌 live region 在 Continue 状态变化时说明“可继续 / 记录保留但需检查 / 暂无可继续记录”；`Hidden` 不朗读为按钮 |
| `PreservedLocked Detail` | Retry；若不可用则 Return Title | 返回 Title / Ready Continue Entry | 礼貌 live region 说明记录仍保留、当前不可进入、可选操作 |
| `Ephemeral Session Warning` | Return Title；玩家主动选择后才到临时试航确认 | Return Title | 礼貌 live region 说明本次不会留下正式进度；只有即将离开已有已验证进度时才允许 assertive |
| `SaveLocked Overlay` | Retry Save Capability；若重试暂不可用则 Return Title | Return Title，写屏障保持有效 | 礼貌 live region 说明旧安全记录仍保留、新正式进度当前不能可靠记录；禁止朗读为保存成功 |
| `Temporary Flight Confirmed` | Return Title；若继续临时玩法则聚焦临时模式主操作 | Return Title | 礼貌 live region 说明已进入临时试航、旧安全记录仍保留、本次不会写入正式进度 |
| `RecoveryRequired Screen` | Retry；若不可用则 Return Title | Return Title | 礼貌 live region 说明需要选择恢复路径，旧记录不会被覆盖 |
| `Migration Status` | 被动状态无焦点；完成后聚焦 Continue 或 Return Title | 迁移中不可返回，除非明确可中止 | 礼貌 live region 更新迁移中、已更新、已保留但不可继续 |
| `Developer Save Diagnostics` | Copy Report | 关闭诊断返回来源界面 | 不向普通玩家朗读内部错误码；开发构建可朗读 reason code |

标题入口 Tab 顺序：

1. `Continue` 或 `PreservedLocked` 详情入口；当 `Hidden` 时跳过此项。
2. `Start` / `New Session`。
3. Settings / language / audio 等壳层设置入口。
4. Diagnostics copy 入口仅在开发构建或中高详情级别可见时加入。

## Acceptance Criteria

### Storage Capability

- **GIVEN** `raw_persistent_api_ok=true`、`storage_backend_probe_ok=true`、`existing_archive_read_class=Readable` 或 `NotApplicable`、`quota_ok=true`、`quota_reserve_ok=true`、`write_roundtrip_ok=true` 且 `policy_forces_ephemeral=false`，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果必须是 `PersistentAvailable`。
- **GIVEN** `raw_persistent_api_ok=true`、`storage_backend_probe_ok=true`、`existing_archive_read_class=Readable` 或 `NotApplicable`、`policy_forces_ephemeral=false`，但 `quota_ok=false`、`quota_reserve_ok=false` 或 `write_roundtrip_ok=false`，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果必须是 `WriteLocked`。
- **GIVEN** `raw_persistent_api_ok=false`、`storage_backend_probe_ok=false`、`existing_archive_read_class=Unreadable` 或 `policy_forces_ephemeral=true`，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果必须是 `EphemeralOnly`。
- **GIVEN** fresh install 没有旧 manifest 或 continue artifact，且 `existing_archive_read_class=NotApplicable`、其他持久化探测全部通过，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果必须是 `PersistentAvailable`，不得因为无旧档输出 `EphemeralOnly`。
- **GIVEN** 平台壳需要显示存储能力或 Continue 状态，**WHEN** 状态被呈现，**THEN** 壳层必须读取本系统返回的 `storage_capability` 和 `query_continue_state()`，不得根据桌面 API、文件存在、旧 probe 或本地公式重新计算。
- **GIVEN** `quota_reserve_ok=false` 且 `existing_archive_read_class=Readable`，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果必须是 `WriteLocked`，旧 `Safe` 继续点不得被覆盖或隐藏。
- **GIVEN** `quota_reserve_ok=false` 且 `existing_archive_read_class=Unreadable`，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果必须是 `EphemeralOnly`，不得显示可用持久 Continue。
- **GIVEN** `user://` 路径、API presence 或文件存在返回可用，但真实 write/flush/readback/checksum roundtrip 未完成，**WHEN** 本系统计算 `storage_capability`，**THEN** 结果不得是 `PersistentAvailable`。

### Snapshot Package Validity

- **GIVEN** 领域系统导出包含 `domain_id`、`snapshot_schema_version`、`content_domain_versions`、`stable_id_refs`、`payload`、`domain_state=Ready` 的 `Snapshot Package`，`schema_version_known=true`、`domain_error_blocking=false`、payload 只包含允许类型且满足 canonical codec 规则，内容域版本兼容，且所有稳定 ID 解析为 `Active` 或可迁移 `Deprecated`，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 true。
- **GIVEN** `Snapshot Package` 缺少任一必填字段、`schema_version_known=false`、`domain_state != Ready`、存在阻塞错误码、内容域版本不兼容、稳定 ID 解析为 `Retired` / `NOT_FOUND` / `UNLOADED` / `VERSION_INCOMPATIBLE`，payload 包含非法 Godot/Variant 类型，dictionary key 不是 string，key 未 canonical 排序，容器存在 cycle，float 为 `NaN` / `Infinity` / `-Infinity`，或 checksum 未覆盖 metadata，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 false，且本次 promotion 不发生。
- **GIVEN** `Snapshot Package` 缺少 `domain_id`、`snapshot_schema_version`、`content_domain_versions`、`stable_id_refs`、`payload` 或 `domain_state` 任一必填字段，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 false，并输出缺字段 reason code。
- **GIVEN** `schema_version_known=false`，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 false，并不得进入 migration 以外的 promotion 路径。
- **GIVEN** `domain_state=Blocked`、`NotReady` 或 `Settling`，或 `domain_error_blocking=true`，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 false，并保留旧 `Safe`。
- **GIVEN** 任一 stable ID 解析为 `Retired`、`NOT_FOUND`、`UNLOADED` 或 `VERSION_INCOMPATIBLE`，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 false；`Retired` 不得被当作可保存成功。
- **GIVEN** payload dictionary key 在 Unicode NFC 规范化后重复，key 不是 string，key 未 canonical 排序，float 为非 finite，或 `-0.0` 未规范化为 `0.0`，**WHEN** 计算 `snapshot_package_validity`，**THEN** 结果必须为 false。

### Safe Checkpoint And Promotion

- **GIVEN** `storage_capability=PersistentAvailable`、`trigger_class=StableBoundary`、`domain_ready=true`、`snapshot_package_validity=true` 且 `write_barrier_active=false`，**WHEN** 计算 `safe_checkpoint_eligibility`，**THEN** 结果必须为 true。
- **GIVEN** `trigger_class != StableBoundary`，**WHEN** 计算 `safe_checkpoint_eligibility`，**THEN** 结果必须为 false，即使领域快照已 ready。
- **GIVEN** `staging_written=true`、`readback_verified=true`、`checksum_ok=true`、`schema_compatible=true`、`stable_id_resolved=true` 且 `no_domain_blockers=true`，**WHEN** 计算 `promotion_success`，**THEN** 结果必须为 true，并更新最近安全继续点。
- **GIVEN** staging 缺字段、部分写入、checksum 不一致、Schema 不兼容、稳定 ID 不可解析或领域 blocker 存在，**WHEN** 运行 promotion，**THEN** `promotion_success=false`，旧 `Safe` 保持不变，且 UI 不显示保存成功。
- **GIVEN** 新保存从 `Staging` 开始，**WHEN** staging 已写入但尚未进入 `Verify`，**THEN** `current_generation`、manifest pointer 和 `last_verified_checkpoint` 必须仍指向旧 `Safe`。
- **GIVEN** 工件处于 `Verify`，**WHEN** readback、checksum、Schema、稳定 ID、内容域版本或领域 blocker 任一检查未完成，**THEN** 工件不得进入 `Safe`，UI 不得显示保存成功。
- **GIVEN** `promotion_success=true`，**WHEN** promotion 提交发生，**THEN** 只能通过权威 current pointer / generation 切换让新工件成为当前继续点。
- **GIVEN** manifest pointer 指向的 generation 低于已记录的 `last_verified_checkpoint.generation` 或 checksum/summary 不匹配，**WHEN** 启动恢复检查发生，**THEN** 该 pointer 必须被拒绝，不得成为 current。

### Artifact Split

- **GIVEN** 仅 settings artifact 变化且 progress artifact 未变化，**WHEN** 设置保存与恢复完成，**THEN** settings 值恢复为新值，progress artifact 保持最近已验证版本不变。
- **GIVEN** 仅 progress artifact 变化且 settings artifact 未变化，**WHEN** 进度保存与恢复完成，**THEN** progress 值恢复为新值，settings artifact 保持最近已验证版本不变。
- **GIVEN** settings 写入失败但 progress promotion 成功，**WHEN** 恢复会话，**THEN** progress 使用最近已验证值，settings 回退到最近已验证设置值，二者不得互相删除或覆盖。
- **GIVEN** progress 写入失败但 settings promotion 成功，**WHEN** 恢复会话，**THEN** settings 使用最近已验证设置值，progress 回退到最近已验证进度值，二者不得互相删除或覆盖。
- **GIVEN** settings artifact 进入 `Quarantined` 且 progress artifact 仍为可恢复 `Safe`，**WHEN** 计算 `continue_availability`，**THEN** Continue 必须仍按 progress 输出，不得因 settings 损坏变为 `Hidden` 或 `PreservedLocked`。
- **GIVEN** progress artifact 进入 `Quarantined` 且 settings artifact 仍为 `Safe`，**WHEN** 计算 `continue_availability`，**THEN** Continue 不得是 `Enabled`，但 settings 不得被删除或覆盖。

### Continue And Migration

- **GIVEN** `archive_present=true`、`artifact_state=Safe`、`integrity_ok=true`、`version_compatible=true`、`stable_ids_resolved=true`、`migration_required=false` 且 `quarantined=false`，**WHEN** 计算 `restore_readiness`，**THEN** 结果必须为 true。
- **GIVEN** `archive_present=false`，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `Hidden`。
- **GIVEN** `storage_capability=PersistentAvailable`、`archive_present=true` 且 `restore_readiness=true`，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `Enabled`。
- **GIVEN** `storage_capability=WriteLocked`、`archive_present=true` 且 `restore_readiness=true`，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `Enabled`，并且新持久保存必须进入 `SaveLocked` 写屏障。
- **GIVEN** `storage_capability=PersistentAvailable`、`archive_present=true` 且 `restore_readiness=false` 是因为 `migration_required=true`、版本不兼容、内容域不兼容或稳定 ID 需要迁移，**WHEN** 计算 `continue_availability`，**THEN** 结果必须是 `PreservedLocked`，并带原因码。
- **GIVEN** Title / Ready Continue Entry 需要呈现 Continue，**WHEN** 壳层读取状态，**THEN** 必须消费 `query_continue_state().continue_availability`，不得根据文件存在、slot metadata、settings 或本地内容域状态重新计算 `Enabled` / `PreservedLocked` / `Hidden`。
- **GIVEN** 存档工件解析失败、结构损坏或完整性校验失败，**WHEN** 启动恢复前检查，**THEN** 该工件状态必须变为 `Quarantined`，不得作为 `Enabled` 继续点。
- **GIVEN** `migration_required=true`、`migration_chain_available=false`、`parse_ok=true` 且 `integrity_ok=true`，**WHEN** 计算 `migration_outcome`，**THEN** 结果必须是 `PreservedLocked`，不得是 `AlreadyCurrent`。
- **GIVEN** `migration_required=true`、`migration_chain_available=true`、`staging_ok=true`、`verify_ok=true` 且 `promotion_success=true`，**WHEN** 计算 `migration_outcome`，**THEN** 结果必须是 `Upgraded`，并写入迁移记录。
- **GIVEN** `migration_required=true` 且迁移过程中 `staging_ok=false`、`verify_ok=false` 或 `promotion_success=false`，**WHEN** 计算 `migration_outcome`，**THEN** 结果必须是 `PreservedLocked`，原工件保持不改写。
- **GIVEN** `migration_required=false`、`parse_ok=true` 且 `integrity_ok=true`，**WHEN** 计算 `migration_outcome`，**THEN** 结果必须是 `AlreadyCurrent`。
- **GIVEN** `migration_required=false`、`parse_ok=true`、`integrity_ok=true` 但 `direct_restore_compatible=false`，**WHEN** 计算 `migration_outcome`，**THEN** 结果必须是 `PreservedLocked`，不得是 `AlreadyCurrent`。

### Backup Failover

- **GIVEN** 主继续点 parse、structure、integrity、version 或 stable ID 任一检查失败，且 `backup_present=true`、`backup_parse_ok=true`、`backup_integrity_ok=true`、`backup_structure_ok=true`、`backup_version_compatible=true`、`backup_stable_ids_resolved=true`，**WHEN** 计算 `backup_failover_outcome`，**THEN** 结果必须是 `BackupPromoted`，旧主档进入 `Quarantined`，备份经 `BackupPromoting -> Verify -> Safe` 后才可成为唯一可用 `Safe`。
- **GIVEN** 主继续点不可用，备份 parse/structure/integrity 通过但 `backup_migration_required=true` 或备份版本/稳定 ID 不能直接恢复，**WHEN** 计算 `backup_failover_outcome`，**THEN** 结果必须是 `BackupPreservedLocked`，Continue 必须是 `PreservedLocked` 而不是 `Enabled`。
- **GIVEN** 主继续点 parse、structure、integrity、version 或 stable ID 任一检查失败，且没有备份或备份 parse、structure、integrity、version、stable ID 任一检查失败，**WHEN** 计算 `backup_failover_outcome`，**THEN** 结果必须是 `NoUsableBackup`，Continue 不得显示为 `Enabled`。
- **GIVEN** `main_usable=true`，**WHEN** 计算 `backup_failover_outcome`，**THEN** 结果必须是 `NotNeeded`，且现有主 `Safe` 继续作为当前可用继续点。

### Desktop Lifecycle And Degraded Modes

- **GIVEN** 桌面退出请求或 `suspend_requested` 发生且 staging 正在进行但 verify 尚未完成，**WHEN** 应用关闭继续推进，**THEN** 系统可以发出 best-effort flush，但不得设置 `promotion_success=true`，不得替换 `last_verified_checkpoint`，不得阻塞关闭。
- **GIVEN** 窗口失焦、最小化、系统暂停或退出请求触发，**WHEN** 尚无预编码 staging 可轻量 flush，**THEN** 系统不得启动新的完整序列化、迁移、备份提升或诊断文本生成。
- **GIVEN** 窗口失焦、最小化、系统暂停或退出请求触发，**WHEN** 处理 lifecycle marker，**THEN** 系统不得启动 readback、checksum、full serialization、migration、backup promotion 或 diagnostics text formatting；只能使用已预编码 marker，且必须在 `shutdown_marker_budget_ms` 内放弃。
- **GIVEN** Godot .NET 平台适配层在壳层完成初始化前收到 pause / quit / focus-lost 信号，**WHEN** 壳层完成初始化，**THEN** 该缓存事件必须转为壳层 lifecycle token，不得丢弃。
- **GIVEN** 任意 suspend 后第一次 focus/resume，**WHEN** 窗口恢复，**THEN** capability probe 必须失效并重新探测，即使 TTL 尚未过期。
- **GIVEN** 工件处于 `Staging` 或 `Verify`，**WHEN** UI 轮询保存状态，**THEN** UI 可以显示保存中或正在保护最近进度，但不得显示保存成功。
- **GIVEN** `EphemeralOnly` 会话已确认进入，**WHEN** 玩家尝试提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置，**THEN** 系统必须拒绝正式提交，且 `continue_availability=Hidden`；任何临时预览都必须显式标记为非持久化，并且不得创建或修改正式世界状态快照。
- **GIVEN** 会话中途进入 `SaveLocked`，**WHEN** 玩家尝试提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置，**THEN** 系统必须阻止正式提交并显示 Retry Save Capability / Return Title / Enter Temporary Flight 选择。
- **GIVEN** `SaveLocked Overlay` 显示，**WHEN** 只使用键盘或屏幕阅读器操作，**THEN** 默认焦点必须在 Retry Save Capability；若 Retry 暂不可用则在 Return Title；Escape / Back 必须返回安全标题态但不得关闭写屏障；live region 必须说明旧安全记录仍保留且新正式进度当前不能可靠记录。
- **GIVEN** 玩家在 `SaveLocked` 中选择 Enter Temporary Flight，**WHEN** 二次确认完成，**THEN** 本系统必须进入 `EphemeralOnly`，调用结果必须禁止所有正式 commit class，且 UI 不得显示普通保存成功或普通 Continue 语义。
- **GIVEN** 玩家在 `SaveLocked` 中完成 Enter Temporary Flight 二次确认，**WHEN** `EphemeralOnly` 生效，**THEN** `continue_availability` 必须是 `Hidden`，焦点必须移动到临时模式主操作或 Return Title，live region 必须说明旧安全记录仍保留且本次不会写入正式进度。

### UI And Diagnostics

- **GIVEN** `Continue = Hidden`，**WHEN** 渲染标题入口，**THEN** 不得显示可操作 Continue；若保留布局占位，占位不得进入键盘焦点顺序。
- **GIVEN** `Continue = PreservedLocked`，**WHEN** 渲染标题入口，**THEN** 入口必须可见但不可直接进入玩法，必须提供原因详情、Retry、New Session 和 Return Title。
- **GIVEN** `PreservedLocked Detail`、`Ephemeral Session Warning`、`RecoveryRequired Screen` 或 `Migration Status` 显示，**WHEN** 只使用键盘和屏幕阅读器操作，**THEN** 默认焦点、Escape/Back 行为和 live region 公告必须符合本 GDD 的焦点与辅助功能表。
- **GIVEN** `Title / Ready Continue Entry` 显示，**WHEN** `continue_availability` 在 `Enabled`、`PreservedLocked` 或 `Hidden` 之间变化，**THEN** 默认焦点、Tab 顺序、Escape/Back 行为和礼貌 live region 公告必须符合本 GDD 的标题入口规则。
- **GIVEN** 最近安全继续点可用，**WHEN** Title / Ready Continue Entry 显示摘要，**THEN** 摘要必须包含最近安全边界、一个玩家可识别的世界事实、保存时间或相对时间，且不得只显示内部版本、路径、checksum 或 slot metadata。
- **GIVEN** `quota_reserve_ok` 被计算，**WHEN** 系统估算持久存储和峰值工作集，**THEN** 必须输出 `required_bytes`、`peak_working_set_bytes`、`safety_margin_bytes`、`backend_persistence_inflation_factor`、`backend_working_set_inflation_bytes`、`available_storage_bytes` 和 `available_working_set_bytes`；任一必需项超出安全余量时不得返回 `PersistentAvailable`。
- **GIVEN** `available_working_set_bytes` 无法由平台适配层提供，**WHEN** 计算 `quota_reserve_ok`，**THEN** 必须使用 16 MiB fallback，并在诊断中标记 `WORKING_SET_BUDGET_FALLBACK`。
- **GIVEN** capability probe 已完成，**WHEN** probe TTL 过期、write failure、readback mismatch、quota failure、policy change 或平台存储策略变更（磁盘配额/权限/文件系统变更）发生，**THEN** 本系统必须让 probe 失效并重新探测；过期 probe 不得作为 `PersistentAvailable` 依据。
- **GIVEN** 每次保存、迁移、恢复、备份提升或 capability probe 完成，**WHEN** 开发诊断摘要生成，**THEN** 摘要必须包含快照字节数、存储余量、encode/write/readback/checksum 耗时、promotion 结果、失败原因码、shutdown/suspend 结果和备份提升结果；保存热路径只能追加预分配、固定大小、allocation-free 的结构化记录，不得同步生成完整可复制文本报告。

## Open Questions

| Question | Owner | Target | Resolution |
|---|---|---|---|
| 桌面 C# 持久化最终通过哪一层实现：Godot `FileAccess` / `user://`、C# `System.IO` wrapper，还是二者分层？ | Technical Direction | ADR / Control Manifest | Resolved for GDD: MVP 使用桌面 Godot .NET/C# 路径；存档服务拥有 write/read/verify/promotion，平台壳只传递 focus/pause/quit/capability 信号，不直接执行 promotion。 |
| 存档工件的具体格式采用 JSON、二进制、Resource、自定义容器，还是分层 manifest + payload？ | Technical Direction / Persistence Implementation | ADR | Resolved for GDD: 必须是分层 manifest + canonical encoded payload，自定义确定性 codec；禁止 raw `store_var` / `get_var` Variant blob 作为权威存档格式。ADR 可决定 JSON-like 或二进制编码，但必须满足本 GDD 的 canonical key、finite float、null、checksum 和 metadata 覆盖规则。 |
| `Snapshot Package` 的最终 C# API 名称和具体类型如何命名？ | Architecture | Control Manifest / ADR | Open；最小字段、版本字段、状态字段和阻塞错误码契约已由本 GDD 定义。 |
| 存档迁移表由注册表、存档系统还是独立 migration module 提供具体数据？ | Technical Direction | ADR | Open；但本 GDD 已固定迁移所有权：本系统调度迁移、staging、verify、promotion 和 outcome；注册表只提供稳定 ID 生命周期、内容域版本和迁移提示。 |
| Player-Facing Translation Layer 的最终中文文案、英文/本地化版本和界面布局由 UI/HUD GDD 还是 UX spec 最终定稿？ | UX / UI | UI GDD / UX spec | Open；但本 GDD 已固定普通玩家不得看到内部状态名，且 `SaveLocked`、`PreservedLocked`、`EphemeralOnly`、`Quarantined` 必须有可行动、非恐吓的玩家语义。 |
| 自动备份是否允许玩家手动恢复，还是只作为系统内部回退？ | Product / UX | UX spec | Open；MVP 必须支持系统内部自动提升，手动恢复入口可由 UX spec 决定。 |
