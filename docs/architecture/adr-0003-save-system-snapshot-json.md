# ADR-0003: 存档系统 — 快照包与 JSON 序列化

## Status
Proposed

## Date
2026-05-04

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Persistence / Serialization |
| **Knowledge Risk** | HIGH — 此版本远超 LLM 训练截止日期 (May 2025) |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md` |
| **Post-Cutoff APIs Used** | `FileAccess.store_*` 返回 `bool` (4.4) — 用于写验证；`JavaScriptBridge` 生命周期回调 (4.x stable) — 用于 `visibilitychange`/`pagehide`；`duplicate_deep()` (4.5) — 用于快照深拷贝 |
| **Verification Required** | Web 导出下 `user://` 写入+读回 roundtrip 验证；`pagehide` 内 20ms budget flush 可行性；JSON 大字符串在 Web 单线程下的序列化耗时剖面；SHA-256 checksum 在 2MB 快照上的耗时 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload 清单 — Persistence 为 Autoload #2，Phase 2 初始化；启动信号链定义 `persistence_ready` 信号) |
| **Enables** | ADR-0005 (资源池快照 Schema), ADR-0006 (Web 平台约束 — 存储配额与生命周期), ADR-0007 (Intel 快照 Schema), ADR-0008 (飞艇模块快照 Schema), ADR-0013 (世界修复快照 Schema) |
| **Blocks** | 所有需要持久化状态的系统实现故事 — 必须先确定序列化格式和 Snapshot Package API 才能实现任何领域的 save/load |
| **Ordering Note** | 应在 ADR-0001 和 ADR-0002 之后、ADR-0005/0007/0008 之前 Accepted |

## Context

### Problem Statement

《云海织航》的 GDD #3 `local-save-world-state-persistence` 定义了完整的存档语义：Snapshot Package 契约、Staging→Verify→Promotion 工作流、`settings`/`progress` 双工件分离、备份故障转移、迁移和 Web 生命周期集成。但 GDD 将以下技术决策留给 ADR：序列化格式（JSON vs 二进制）、存储后端（`user://` vs IndexedDB via JS bridge）、Snapshot Package 的具体 GDScript API、checksum 算法、以及 canonical encoding 规则。本 ADR 做出这些决策，为所有领域系统提供统一的持久化基础设施。

### Constraints

- **Web-first**: 单线程执行，所有序列化/checksum 在主线程完成，不能阻塞帧渲染
- **Godot 4.6.2 Web 导出**: `FileAccess` 支持 `user://` 路径读写；`JavaScriptBridge` 支持 JS 互操作；单线程导出（无 `Thread`）
- **GDD 硬约束**: 禁止 Variant blob (`store_var`/`get_var`) 作为权威存档格式；必须分层 manifest + canonical encoded payload；必须 Staging→Verify→Promotion
- **浏览器存储限制**: IndexedDB / `user://` 配额因浏览器而异（通常源限额 30%-60% 磁盘），MVP 目标 ≤ 2MB 快照
- **Web 生命周期**: `pagehide`/`visibilitychange hidden` 只能触发 best-effort flush，不能承诺完整保存
- **ADR-0001**: Persistence 为 Autoload #2，Phase 2 初始化；`_ready()` 只能做常量初始化和信号声明
- **ADR-0002**: 所有跨系统信号使用 typed 参数；`save_completed`/`save_failed`/`load_completed`/`load_failed` 信号契约已定义
- **内存**: 序列化峰值工作集 ≤ 16MB（GDD fallback budget）

### Requirements

- 必须支持 `settings` 和 `progress` 独立工件，各自维护 generation、checksum、manifest pointer
- 领域系统通过 `SnapshotPackage` 导出可持久化状态；存档系统只校验、调度、迁移和恢复，不解释领域语义
- Payload 必须遵守 canonical encoding 规则：sorted keys、NFC normalization、finite IEEE 754 floats only
- 必须支持原子 promotion：旧 Safe 在新工件完成写入+读回+校验前保持不变
- 必须支持 Web `pagehide` best-effort flush（≤ 20ms budget）
- 必须支持备份故障转移：主工件损坏时自动从备份恢复

## Decision

### 1. 序列化格式: Canonical JSON

选择 JSON 作为权威存档格式，配合严格的 canonical encoding 规则确保确定性输出。

**Canonical Encoding 规则**:

| 规则 | 要求 |
|------|------|
| Key 排序 | 所有 JSON object key 按 bytewise ascending (ASCII) 排序 |
| 字符串编码 | Unicode NFC 规范化 → UTF-8；stable ID 和 dictionary key 额外约束为 ASCII lowercase `kind.slug` / snake_case |
| 数字 | IEEE 754 binary64 语义；`NaN`/`Infinity`/`-Infinity` 禁止；`-0.0` 规范化为 `0.0` |
| null | 显式 `null` JSON token；禁止用缺字段暗示 null |
| 空 payload | 显式编码为空 JSON object `{}` |
| 禁止类型 | 不编码 `Object`/`Node`/`Resource`/`Callable`/`Signal`/`RID`/`NodePath`/`PackedScene` |
| 缩进 | 紧凑编码（无缩进），最小化字节数 |
| 尾随逗号 | 禁止 |

**Godot JSON 实现**:
- MVP 使用 Godot 内置 `JSON.stringify()` / `JSON.parse_string()` 作为编码器
- `JSON.stringify()` 输出的 key 顺序取决于 GDScript `Dictionary` 的插入/创建顺序
- **必须**使用 sorted-keys 辅助函数在 `stringify()` 前对 dictionary 递归排序
- 若未来性能需要，可替换为自定义 C++/GDExtension 编码器，但 canonical 规则不变

### 2. 存储后端: Godot FileAccess + user:// + JavaScriptBridge 生命周期

**文件 I/O 层**: Godot `FileAccess` + `user://` 路径
- Godot 4.6 Web 导出中 `user://` 映射到 IndexedDB 存储
- `FileAccess.store_*` 返回值自 4.4 起为 `bool`，可用于写验证
- 写入流程: `open(path, WRITE)` → `store_string(data)` → `flush()` → `close()` → `open(path, READ)` → `get_as_text()` → readback verify

**生命周期层**: Custom HTML shell + `JavaScriptBridge`
- Custom HTML shell 在 Godot 引擎启动前注册 `visibilitychange`、`pagehide`、`pageshow`、focus/blur 监听器
- 平台适配层通过 `JavaScriptBridge.create_callback()` 将事件传递给 Godot
- Persistence Autoload 在 Phase 2 初始化时读取缓存的生命周期事件
- Godot `FileAccess` 处理实际文件 I/O；JavaScript 层只传递生命周期信号和存储能力探测结果

**Capability Probe**: 分两阶段执行，归属明确

**Phase 0 — 平台壳轻量探测** (SessionShell):
- 仅检查 API 存在性: `raw_persistent_api_ok`、`storage_backend_probe_class` (粗略分类)、`policy_forces_ephemeral` (浏览器隐私模式)
- 不执行 write→readback roundtrip（太慢，阻塞启动）
- 发射 `platform_ready(raw_probe: Dictionary)` — payload 为原始探测字段，不是最终 `storage_capability`
- `OS.is_userfs_persistent()` 仅作为 hint

**Phase 2 — Persistence 权威探测** (本系统):
- 接收 Phase 0 的 `raw_probe`，补充完整 roundtrip 探测
- 执行 write→flush→close→reopen→readback→checksum roundtrip
- 补充: `existing_archive_read_class`、`quota_ok`、`quota_reserve_ok`、`write_roundtrip_ok`、`working_set_budget_class`
- 计算权威 `storage_capability: PersistentAvailable / WriteLocked / EphemeralOnly`
- 发射 `persistence_ready(continue_state, storage_capability)` — 此为权威值
- 平台壳只能读取此值，不得重算

> **ADR-0001 联动变更**: `platform_ready` signal payload 从 `storage_capability: int` 改为 `raw_probe: Dictionary`。权威 `storage_capability` 由 Persistence 在 `persistence_ready` 中首次输出。

### 3. 工件文件布局

```
user://
├── settings/
│   ├── manifest.json          # { current_generation, last_verified_checkpoint, ... }
│   ├── gen_0001.json           # settings 快照 payload
│   ├── gen_0001.checksum       # SHA-256 hex
│   └── backup/
│       └── gen_0001.json
├── progress/
│   ├── manifest.json
│   ├── gen_0001.json           # 完整 progress 快照 (所有领域)
│   ├── gen_0001.checksum
│   └── backup/
│       └── gen_0001.json
└── diagnostics/
    └── diag_0001.json          # 结构化诊断记录
```

**Generation 规则**:
- Generation ID 单调递增 (1, 2, 3, ...)
- Staging 写入为 `gen_NNNN.staging.json`，验证成功后 rename 为 `gen_NNNN.json`
- 旧 generation 在 promotion 成功后保留一定数量用于诊断，但 `manifest.json` 只指向 `current_generation`
- Backup 独立存储，不与主工件共用 generation

### 4. 完整性校验: SHA-256

- Checksum 覆盖: canonical encoded payload + snapshot schema version + content domain versions + stable ID refs + artifact kind + artifact generation + manifest pointer target
- Checksum 存储为 hex string，与 payload 文件同目录
- 使用 Godot `HashingContext` (若 Web 导出支持) 或 fallback 到纯 GDScript SHA-256 实现

### 5. SnapshotPackage API

```gdscript
# === SnapshotPackage — 领域系统与存档系统之间的唯一持久化边界 ===
# 实现为纯 GDScript RefCounted 类（非 Godot Resource — Resource 类型在 payload 白名单中被禁止）
# 位置: Persistence Autoload 中定义为独立 RefCounted 类

class SnapshotPackage:
    var domain_id: String              # 领域稳定 ID
    var snapshot_schema_version: int   # 该领域快照 Schema 版本
    var content_domain_versions: Dictionary  # { "content_domain": version_int }
    var stable_id_refs: Array[String]  # 本快照依赖的稳定 ID 列表
    var payload: Dictionary            # 只包含 bool/int/float/string/Array/Dictionary
    var domain_state: int              # READY / BLOCKED / NOT_READY / SETTLING
    var domain_error_code: String      # 领域错误码 (domain_state != READY 时)
    var migration_hint: String         # 可选迁移提示

    # 验证方法
    func is_valid() -> bool:
        return (not domain_id.is_empty()
            and snapshot_schema_version > 0
            and not content_domain_versions.is_empty()
            and stable_id_refs != null
            and payload != null
            and domain_state == DOMAIN_READY
            and domain_error_code.is_empty())

    # 导出为 canonical JSON string
    func to_canonical_json() -> String:
        pass  # 递归 sorted-keys → JSON.stringify()

    # 从 canonical JSON string 恢复
    static func from_canonical_json(json_string: String) -> SnapshotPackage:
        pass
```

### 6. Persistence Autoload 公共 API

```gdscript
# === Persistence (Autoload #2) — 公共接口 ===
# 所有公共方法遵循 ADR-0002 的读查询 = 直接调用 / 状态变更 = signal 模式

# --- 读查询 (直接方法调用，返回 Result) ---
func query_continue_state() -> ContinueState:
    # 返回: { continue_availability, storage_capability, write_barrier_mode,
    #         reason_code, checkpoint_summary, last_verified_checkpoint,
    #         current_generation, artifact_kind }
    pass

func query_write_barrier() -> WriteBarrierState:
    pass

func get_snapshot(domain_id: String) -> SnapshotPackage:
    # 获取领域系统的当前快照 (用于测试/诊断)
    pass

func register_domain_serializer(domain_id: String, serializer: Callable) -> void:
    # 领域系统注册其快照序列化器
    # serializer: func() -> SnapshotPackage
    pass

func unregister_domain_serializer(domain_id: String) -> void:
    # Scene 系统 (Exploration, Settlement, VoyageManager) 在 queue_free() 前必须调用
    # 防止保存请求触发已销毁对象的序列化器
    # 注册/注销对称: 实例化时注册, 退出清理时注销
    pass

# --- 操作请求 (直接方法调用 → 返回 Result 类型) ---
# ADR-0002 要求操作请求返回 Result；signal 用于异步完成通知
func request_save(trigger: int) -> SaveRequestResult:
    # 同步返回 accepted / rejected (write barrier active) / deferred (scene transition)
    # 异步完成通过 save_completed / save_failed 信号通知
    pass

func request_load(slot: int) -> LoadRequestResult:
    # 同步返回 accepted / rejected (no safe checkpoint) / deferred
    # 异步完成通过 load_completed / load_failed 信号通知
    pass

# --- 信号 (fire-and-forget 通知) ---
signal save_completed(slot: int)
signal save_failed(slot: int, reason: String)
signal load_completed(slot: int)
signal load_failed(slot: int, reason: String)
signal promotion_completed(generation: int)
signal backup_promoted(generation: int)
```

**本 ADR 新增信号目录** (需同步到 ADR-0002):

| Signal | Producer | Consumers | Payload | Cascade |
|--------|----------|-----------|---------|---------|
| `promotion_completed` | Persistence | UIManager (保存成功反馈), SessionShell (更新 checkpoint summary) | `generation: int` | 深度 1 — 无下游 signal |
| `backup_promoted` | Persistence | UIManager (恢复提示), SessionShell (更新 checkpoint summary) | `generation: int` | 深度 1 — 无下游 signal |

> **ADR-0002 联动变更**: `promotion_completed` 和 `backup_promoted` 两个 signal 需要追加到 ADR-0002 的 "Foundation → Core / Feature" Signal 目录表中。

### 7. 保存流程: Staging → Verify → Promotion

```
┌──────────────────────────────────────────────────────────────────────┐
│                     SAVE FLOW (Stable Boundary)                       │
│                                                                       │
│  1. Stable Boundary Trigger (Hub停靠/航线提交/探索结算/修复提交/交易落定) │
│       │                                                               │
│       ▼                                                               │
│  2. Snapshot Collection: 遍历已注册的领域序列化器                       │
│       │  每个领域 → serializer() → SnapshotPackage                    │
│       │  检查 domain_state == READY                                   │
│       │  任一领域 BLOCKED → 终止，旧 Safe 保留                         │
│       │                                                               │
│       ▼                                                               │
│  3. Encode: 每个 SnapshotPackage → to_canonical_json()                │
│       │  组装 Manifest: { generation, domain_packages[], ... }        │
│       │  Manifest → to_canonical_json()                               │
│       │  Checksum = SHA-256(canonical_manifest)                       │
│       │                                                               │
│       ▼                                                               │
│  4. Staging Write:                                                    │
│       │  FileAccess.open("gen_N.staging.json", WRITE)                 │
│       │  store_string(canonical_manifest)                             │
│       │  flush() → close()                                            │
│       │  FileAccess.open("gen_N.staging.checksum", WRITE)             │
│       │  store_string(checksum) → flush() → close()                   │
│       │                                                               │
│       ▼                                                               │
│  5. Readback Verify:                                                  │
│       │  FileAccess.open("gen_N.staging.json", READ)                  │
│       │  readback = get_as_text()                                     │
│       │  SHA-256(readback) == stored_checksum ?                       │
│       │  Re-parse JSON → validate schema/stable IDs/content domains   │
│       │  Fail → abort, old Safe preserved, quarantine staging         │
│       │                                                               │
│       ▼                                                               │
│  6. Promotion:                                                        │
│       │  验证通过 → rename staging files (remove .staging suffix)      │
│       │  Update manifest.json: current_generation = N                 │
│       │  Update last_verified_checkpoint                              │
│       │  Update backup (async, best-effort)                           │
│       │                                                               │
│       ▼                                                               │
│  7. Done: emit save_completed(slot)                                   │
│       │  emit promotion_completed(N)                                  │
│                                                                       │
│  AT ANY POINT failure → old Safe preserved, emit save_failed(slot, reason) │
└──────────────────────────────────────────────────────────────────────┘
```

### 8. 恢复流程

```
┌──────────────────────────────────────────────────────────────────────┐
│                     LOAD FLOW (Continue)                              │
│                                                                       │
│  1. Read manifest.json → get current_generation                       │
│       │                                                               │
│       ▼                                                               │
│  2. Read gen_N.json + gen_N.checksum                                  │
│       │  SHA-256(readback) == stored_checksum ?                       │
│       │  Fail → try backup → backup valid ? → BackupPromoting         │
│       │         → backup invalid ? → RecoveryRequired                 │
│       │                                                               │
│       ▼                                                               │
│  3. Parse Manifest → iterate domain packages                          │
│       │  For each domain:                                             │
│       │    - Resolve stable_id_refs against Registry                  │
│       │    - Check snapshot_schema_version compatibility              │
│       │    - Check content_domain_versions compatibility              │
│       │    - Any Retired/NOT_FOUND → PreservedLocked                  │
│       │    - Deprecated with migration path → Migrating               │
│       │                                                               │
│       ▼                                                               │
│  4. Dispatch: 每个领域接收其 SnapshotPackage 并恢复状态                │
│       │  Persistence 不解释 payload 内容                              │
│       │  领域系统验证自身快照的语义有效性                              │
│       │  任一领域拒绝 → abort, emit load_failed                       │
│       │                                                               │
│       ▼                                                               │
│  5. 全领域恢复完成 → emit load_completed(slot)                         │
│       │  load_completed 是"世界状态已就绪"通知:                         │
│       │  - SessionShell: 切换到 playing 状态, 隐藏 loading overlay     │
│       │  - UIManager: 刷新 HUD dirty-flag, 切换到游戏内屏幕            │
│       │  - AirshipHub: 如处于隐藏状态则 welcome_back()                 │
│       │  load_completed 不携带 domain payload (各领域已直接接收)       │
│       │  消费者通过直接方法调用查询各自领域的恢复后状态                 │
│       │                                                               │
│       ▼                                                               │
│  6. Done: 世界状态就绪, 玩家可操作                                     │
└──────────────────────────────────────────────────────────────────────┘
```

### 9. Web 生命周期集成

| 事件 | 来源 | 行为 |
|------|------|------|
| `visibilitychange` → hidden | JS shim → 平台适配层 → Persistence | 触发 best-effort 保存请求。仅 flush 已有预编码 staging 数据 |
| `pagehide` | JS shim → 平台适配层 → Persistence | 同 hidden，但 budget 更严格 (≤ 20ms)。不启动新序列化 |
| `pageshow` | JS shim → 平台适配层 → Persistence | Probe TTL 失效，重新探测存储能力 |
| `pageshow.persisted=true` | JS shim → 平台适配层 → Persistence | BFCache 恢复 — 必须重探 capability，即使 probe TTL 未过期 |

**Custom HTML Shell 最低要求**:
- 在 `<script>` 中注册 `visibilitychange`、`pagehide`、`pageshow`、focus/blur 监听器
- 缓存最早到达的事件（Godot 启动前可能已有事件）
- 通过 `JavaScriptBridge` 接口暴露给平台适配层
- 平台适配层持有 `JavaScriptBridge.create_callback()` 引用直到页面生命周期结束

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                     PERSISTENCE ARCHITECTURE                          │
│                                                                       │
│  ┌─────────────────────────────────────────┐                         │
│  │         Custom HTML Shell (JS)           │                         │
│  │  visibilitychange / pagehide / pageshow  │                         │
│  │  focus / blur / capability probe         │                         │
│  └──────────────┬──────────────────────────┘                         │
│                 │ JavaScriptBridge                                    │
│  ┌──────────────▼──────────────────────────┐                         │
│  │         Platform Adapter                │                         │
│  │  事件缓存 → lifecycle tokens            │                         │
│  │  raw capability probe results           │                         │
│  └──────────────┬──────────────────────────┘                         │
│                 │ signal / direct call                                │
│  ┌──────────────▼──────────────────────────────────────────┐        │
│  │              Persistence Autoload (#2)                   │        │
│  │                                                          │        │
│  │  ┌──────────────────────────────────────────────────┐   │        │
│  │  │  PersistenceManager                               │   │        │
│  │  │  - save orchestration (Staging→Verify→Promotion)  │   │        │
│  │  │  - load orchestration (Manifest→Dispatch→Restore) │   │        │
│  │  │  - migration scheduling                           │   │        │
│  │  │  - backup failover                                │   │        │
│  │  │  - storage_capability computation                 │   │        │
│  │  │  - write barrier management                       │   │        │
│  │  └──────────────────────────────────────────────────┘   │        │
│  │                                                          │        │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │        │
│  │  │ CanonicalJSON │  │  SHA256Sum   │  │  DomainReg    │   │        │
│  │  │ sorted keys   │  │  HashingCtx  │  │  serializers  │   │        │
│  │  │ NFC normalize │  │  hex output  │  │  by domain_id │   │        │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │        │
│  │                                                          │        │
│  │  ┌──────────────────────────────────────────────────┐   │        │
│  │  │  FileAccess Layer                                 │   │        │
│  │  │  user://settings/   user://progress/              │   │        │
│  │  │  manifest.json  gen_N.json  gen_N.checksum       │   │        │
│  │  │  backup/        diagnostics/                     │   │        │
│  │  └──────────────────────────────────────────────────┘   │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │              Domain Systems (registered serializers)     │        │
│  │                                                          │        │
│  │  Resources ──── serializer() → SnapshotPackage            │        │
│  │  Intel ──────── serializer() → SnapshotPackage            │        │
│  │  Modules ────── serializer() → SnapshotPackage            │        │
│  │  Chart ──────── serializer() → SnapshotPackage            │        │
│  │  WorldRepair ── serializer() → SnapshotPackage            │        │
│  │  Settlement ─── serializer() → SnapshotPackage            │        │
│  │  Exploration ── serializer() → SnapshotPackage            │        │
│  │  Partner ────── serializer() → SnapshotPackage            │        │
│  │  AirshipHub ─── serializer() → SnapshotPackage            │        │
│  │  Settings ───── serializer() → SnapshotPackage            │        │
│  └──────────────────────────────────────────────────────────┘        │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────┐        │
│  │              Registry Autoload (#1)                       │        │
│  │  stable ID resolution / content domain versions          │        │
│  │  Deprecated→Active migration hints                       │        │
│  └──────────────────────────────────────────────────────────┘        │
└──────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```gdscript
# === Persistence Autoload 完整信号声明 ===
# Phase 2 连接到启动链，Phase 3a 初始化域序列化器

# 保存相关信号 (通知消费者)
signal save_completed(slot: int)
signal save_failed(slot: int, reason: String)
signal load_completed(slot: int)
signal load_failed(slot: int, reason: String)
signal promotion_completed(generation: int)
signal backup_promoted(generation: int)

# 启动链信号 (接收)
# 在 Phase 2: Persistence.on_registry_ready() → 检查 save slots → 发射
# 在 Phase 3a: 其他系统监听 persistence_ready 后初始化序列化器

# === 领域序列化器注册 ===
# 每个需要持久化的领域系统在 Phase 3a 后注册其序列化器

# 注册示例 (Resources Autoload):
# Persistence.register_domain_serializer("progress.resources", _serialize_resources)
#
# func _serialize_resources() -> SnapshotPackage:
#     var pkg = SnapshotPackage.new()
#     pkg.domain_id = "progress.resources"
#     pkg.snapshot_schema_version = 1
#     pkg.content_domain_versions = { "resources": 1 }
#     pkg.stable_id_refs = _collect_resource_ids()
#     pkg.payload = {
#         "pools": _pools_serialize(),
#         "cargo": _cargo_serialize()
#     }
#     pkg.domain_state = SnapshotPackage.DOMAIN_READY
#     return pkg

# === Canonical JSON 编码 ===
# static func to_canonical_json(data: Dictionary) -> String:
#     1. 递归对每个 nested Dictionary 按 key 排序
#     2. 验证所有值类型在白名单中 (bool/int/float/string/Array/Dictionary/null)
#     3. 验证所有 string 为 NFC 规范化
#     4. 验证 float 为 finite，-0.0 → 0.0
#     5. JSON.stringify(sorted_data)
#     6. 返回紧凑 JSON string (无缩进)

# === write_barrier API ===
# query_write_barrier() → WriteBarrierState:
#     barrier_active: bool
#     mode: int              # NONE / WRITE_LOCKED / SAVE_LOCKED / EPHEMERAL_ONLY
#     reason_code: String
#     allowed_actions: Array[String]
#     forbidden_commit_classes: Array[String]
```

## Alternatives Considered

### Alternative A: 纯 Godot FileAccess + user:// (无 JS Bridge)

- **Description**: 完全使用 Godot 原生 API — `FileAccess` + `user://` — 不依赖 custom HTML shell 或 JavaScriptBridge 进行生命周期管理
- **Pros**: 实现最简单；无需维护 JS shim；所有代码在 GDScript 中
- **Cons**: 无法可靠捕获 `pagehide`/`visibilitychange` 事件 — Godot Web 导出的引擎主循环在这些事件触发时可能已暂停；BFCache 恢复时无法重新探测存储能力；缺少浏览器策略/配额变化的主动通知
- **Rejection Reason**: GDD 明确要求 "不得回退为纯 GDScript user:// lifecycle blind path"。Web 平台的特殊生命周期要求必须有 JS 层配合才能满足 `pagehide` best-effort flush 和 capability re-probe 需求

### Alternative B: 纯 IndexedDB + JavaScript Bridge

- **Description**: 绕过 Godot `FileAccess`，通过 JavaScriptBridge 直接操作 IndexedDB
- **Pros**: 完全控制存储语义；可利用 IndexedDB 事务；绕过 `user://` 的不确定性
- **Cons**: 显著增加 JS bridge 复杂度；每次读写需要跨 GDScript/JS 边界序列化；JSON 字符串在 GDScript ↔ JS 之间传递时有额外编码成本；Godot `FileAccess` 在 Web 导出时已映射到 IndexedDB，直接使用更简单
- **Rejection Reason**: 复杂度收益比不高。Godot `FileAccess` + `user://` 在 4.6 Web 导出中已映射到 IndexedDB，额外的一层 JS bridge 只增加了代码路径而不增加能力。混合方案在需要 JS 的地方（生命周期）使用 JS，在不需要的地方使用原生 API

### Alternative C: Godot Resource 序列化 (.tres/.res)

- **Description**: 使用 Godot 内置 `ResourceSaver`/`ResourceLoader` 将快照保存为 `.tres` 或 `.res` 文件
- **Pros**: Godot 原生序列化；类型信息自动保留；支持 `Resource` 嵌套
- **Cons**: Variant 序列化语义不确定 — key 顺序、float 编码、字符串规范化不受控制；`Resource` 格式是 Godot 内部格式，向后兼容性由引擎版本决定；payload 中禁止的 `Resource`/`Node`/`Object` 引用可能被意外包含；不满足 GDD 的 canonical encoding 要求
- **Rejection Reason**: GDD 明确禁止 Variant blob (`store_var`/`get_var`) 作为权威存档格式。Resource 序列化与 Variant blob 有相同的不确定性问题和兼容性风险。JSON 提供跨版本的人类可读性和确定性的编码规则

## Consequences

### Positive

- **确定性编码**: Canonical JSON 规则确保相同快照在任何时间、任何平台产生字节级一致的输出，使 checksum 验证有效
- **人类可读**: JSON 格式允许开发者直接检查存档内容进行调试，无需专用工具
- **Web 生命周期安全**: JS bridge 确保 `pagehide`/BFCache 恢复正确处理，不丢失数据也不做出虚假的保存承诺
- **类型安全边界**: `SnapshotPackage` 强类型 API 防止领域系统意外泄露 `Node`/`Resource` 引用到持久化层
- **原子 Promotion**: Staging → Verify → Promotion 保证旧 Safe 永远不会被部分写入覆盖
- **独立工件**: `settings`/`progress` 完全分离，一个损坏不影响另一个
- **可测试**: 序列化/反序列化完全独立于 Godot 场景树，可在单元测试中验证

### Negative

- **主线程序列化**: JSON 编码和 SHA-256 计算在主线程执行，2MB 快照可能消耗 16-50ms
- **JSON 类型限制**: 不支持二进制数据、`Vector2`/`Vector2i`/`Vector3` 需拆分为数值数组
- **无增量保存**: 每次保存是完整快照，不追踪变更。对 2MB MVP 目标可接受，未来可能需要增量
- **维护两套代码**: Canonical JSON 编码器和 JS lifecycle shim 都需要维护和测试

### Risks

- **Risk**: 2MB JSON 快照的序列化 + checksum 在 Web 低端设备上超过 50ms frame budget
  - **Mitigation**: 在保存热路径中 profile encode + checksum 耗时；如果超过 warning threshold (50ms)，引入分帧序列化或异步 Web Worker (若支持线程导出)
- **Risk**: `user://` 在特定浏览器隐私模式下行为不一致
  - **Mitigation**: Capability probe 执行真实 write→flush→readback roundtrip；失败则进入 `WriteLocked` 或 `EphemeralOnly`
- **Risk**: `pagehide` 20ms budget 不足以完成 flush
  - **Mitigation**: 稳定边界保存时预先序列化 staging payload；`pagehide` 只执行轻量 flush marker；如果 20ms 超时则放弃，下一个 session 回退到上一个已验证 Safe
- **Risk**: `JSON.stringify()` 输出的 key 顺序在 Godot 不同版本间变化
  - **Mitigation**: sorted-keys 辅助函数在 `stringify()` 前递归排序所有 dictionary，消除对 Godot 内部顺序的依赖
- **Risk**: SHA-256 在纯 GDScript fallback 中性能不足
  - **Mitigation**: 优先使用 Godot `HashingContext` (C++ backend)；若 Web 导出不支持，使用 Godot `Crypto` 类；纯 GDScript fallback 仅用于离线测试

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| #3 local-save-world-state-persistence | Rule 2: 领域系统导出显式 Snapshot Package，存档系统不得遍历场景树 | `SnapshotPackage` 类 + `register_domain_serializer()` — 领域主动导出，Persistence 不遍历 |
| #3 local-save-world-state-persistence | Rule 7: Staging → Verify → Promotion 工作流 | 文件级 staging (`gen_N.staging.json`) + readback verify + `manifest.json` pointer swap |
| #3 local-save-world-state-persistence | Rule 8: settings/progress 逻辑分离，互不污染 | `user://settings/` 和 `user://progress/` 独立目录，各自 manifest + generation + checksum |
| #3 local-save-world-state-persistence | Rule 12: 存储能力权威判定由本系统拥有 | `storage_capability` 计算在 Persistence；`capability_probe` 结果从平台适配层输入 |
| #3 local-save-world-state-persistence | Rule 20: pagehide best-effort 不是正确性路径 | JS lifecycle bridge 只触发预编码 staging flush；完整序列化只在稳定边界执行 |
| #3 local-save-world-state-persistence | Rule 21: 自动备份独立工件 | `backup/` 子目录独立于主 generation |
| #3 local-save-world-state-persistence | Snapshot Package Contract: 必填字段 + 类型白名单 | `SnapshotPackage` 类强制所有必填字段；`to_canonical_json()` 验证类型白名单 |
| #3 local-save-world-state-persistence | Payload canonical encoding: sorted keys, NFC, finite float | Canonical JSON 规则详述 sorted keys、NFC normalization、IEEE 754 finite only、-0.0→0.0 |
| #3 local-save-world-state-persistence | Checksum 覆盖 metadata + payload | SHA-256 覆盖 canonical encoded payload + schema version + content domain versions + stable ID refs + artifact metadata |
| #3 local-save-world-state-persistence | Open Question: 存档工件具体格式 | 答案: Canonical JSON (sorted keys, compact, deterministic) |
| #3 local-save-world-state-persistence | Open Question: 存储后端 | 答案: Godot `FileAccess` + `user://` 文件 I/O + custom HTML shell / `JavaScriptBridge` 生命周期 |

## Performance Implications

- **CPU**: JSON 序列化 O(N) where N = snapshot bytes。2MB → 估计 8-16ms encode + 4-8ms SHA-256 = 12-24ms 总计 (现代桌面)。Web 低端设备可能 30-50ms。目标: p95 < 50ms
- **Memory**: 峰值工作集 = encoded artifact + readback copy + checksum buffer + serialization transient ≈ 3× artifact size。2MB artifact → ~6MB peak。远低于 16MB fallback budget
- **Load Time**: JSON 解析 O(N)。2MB → 估计 5-10ms parse + 4-8ms checksum verify = 9-18ms。恢复分派到各领域并行初始化（各系统在收到 SnapshotPackage 后独立恢复）
- **Network**: 无 — 所有存储为本地操作。Web 导出 `.pck` 包内

## Migration Plan

项目尚无代码，此为初始实现标准：

1. 创建 `Persistence` Autoload 骨架（最小 `_ready()` + 信号声明）
2. 实现 `CanonicalJSON` 工具类 — sorted keys + 类型验证 + NFC normalization + JSON 编解码
3. 实现 `SnapshotPackage` 类 — 字段定义 + `to_canonical_json()` + `from_canonical_json()`
4. 实现 `PersistenceManager` — save/load 编排 + Staging→Verify→Promotion + backup failover
5. 实现 capability probe 和 `storage_capability` 计算
6. 创建 custom HTML shell + 平台适配层 — JS lifecycle events → `JavaScriptBridge` → Persistence
7. 实现 `pagehide` best-effort flush
8. 实现 `manifest.json` 读写 + generation 管理
9. 每个领域系统逐一注册其序列化器

## Validation Criteria

- `SnapshotPackage.to_canonical_json()` 输出确定 — 相同输入 → 字节级一致的输出
- Canonical JSON 规则全部通过: sorted keys, NFC, finite float, -0.0→0.0, 显式 null, 空 payload 为 `{}`
- `staging_written + readback_verified + checksum_ok` → `promotion_success=true`, `current_generation` 递增
- Readback mismatch → `promotion_success=false`, 旧 `Safe` generation 不变
- `pagehide` 触发时只 flush 预编码 staging，不启动新序列化
- `pageshow.persisted=true` 后 probe TTL 失效并重探
- `settings` 写入失败 + `progress` 可恢复 → `continue_availability=Enabled` (按 progress 判定)
- `progress` 写入失败 + `settings` 可恢复 → `continue_availability != Enabled` (但 settings 保留)
- Web 导出下 2MB 快照的 save+verify 总耗时 < 100ms (p95)
- 备份提升: 主档损坏 + 备份可验证 → 备份经 Staging→Verify→Promotion 后成为 `Safe`，旧主档 `Quarantined`

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构与启动顺序 — Persistence 的 Autoload 位置和初始化时机
- **ADR-0002**: Signal 通信协议 — `save_completed`/`save_failed`/`load_completed`/`load_failed` 信号契约
- **ADR-0005** (待创建): 资源池架构 — Resources 领域快照 Schema 定义
- **ADR-0007** (待创建): Intel 快照 Schema — 知识状态序列化格式
- **ADR-0008** (待创建): 飞艇模块快照 Schema — 模块状态序列化格式
- **GDD #3**: `design/gdd/local-save-world-state-persistence.md` — 完整持久化语义定义
