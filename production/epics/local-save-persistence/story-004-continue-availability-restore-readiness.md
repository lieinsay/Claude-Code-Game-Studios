# Story 004: Continue Availability & Restore Readiness

> **Epic**: Local Save / World State Persistence
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-004`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization
**ADR Decision Summary**: `continue_availability` 的最终判定由存档系统拥有。平台壳只能通过 `query_continue_state()` 读取并呈现 `Enabled` / `PreservedLocked` / `Hidden`，不得用本地公式重新计算。Continue 不是"存在文件即可进入"——必须 metadata、完整快照、内容域版本、稳定 ID 引用和完整性校验全部通过后才输出 Enabled。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `query_continue_state()` 以 `artifact_kind=progress` 调用；settings 与 progress 独立判定。

**Control Manifest Rules (Foundation layer)**:
- Required: `query_continue_state()` 是唯一 Continue API；settings/progress 独立 artifact 判定
- Forbidden: 平台壳不得绕过 `query_continue_state()` 直接读 manifest；不得根据文件存在或本地公式重算 Continue
- Guardrail: `continue_validation_strictness = strict`（固定）

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN `archive_present=true`、`artifact_state=Safe`、`integrity_ok=true`、`version_compatible=true`、`stable_ids_resolved=true`、`migration_required=false`、`quarantined=false`，WHEN 计算 `restore_readiness(progress)`，THEN 结果为 true
- [x] **AC-2**: GIVEN `archive_present=false`，WHEN 计算 `continue_availability`，THEN 结果为 `Hidden`
- [x] **AC-3**: GIVEN `progress.storage_capability=PersistentAvailable`、`archive_present=true`、`restore_readiness(progress)=true`，WHEN 计算 `continue_availability`，THEN 结果为 `Enabled`
- [x] **AC-4**: GIVEN `progress.storage_capability=WriteLocked`、`archive_present=true`、`restore_readiness(progress)=true`，WHEN 计算 `continue_availability`，THEN 结果为 `Enabled`，且新持久保存进入 `SaveLocked` 写屏障
- [x] **AC-5**: GIVEN `archive_present=true` 且 `restore_readiness(progress)=false` 因为 `migration_required=true`、版本不兼容、内容域不兼容或稳定 ID 需要迁移，WHEN 计算 `continue_availability`，THEN 结果为 `PreservedLocked` 并带 reason_code
- [x] **AC-6**: GIVEN Title / Ready Continue Entry 需要呈现 Continue，WHEN 壳层读取状态，THEN 必须消费 `query_continue_state().continue_availability`，不得根据文件存在、slot metadata、settings 或本地内容域状态重算
- [x] **AC-7**: GIVEN 存档工件解析失败、结构损坏或完整性校验失败，WHEN 启动恢复前检查，THEN 工件状态变为 `Quarantined`，不得作为 `Enabled` 继续点
- [x] **AC-8**: GIVEN `query_continue_state()` 被调用，WHEN 返回结果，THEN 输出至少包含 `continue_availability`、`storage_capability`、`write_barrier_mode`、`reason_code`、`checkpoint_summary`、`last_verified_checkpoint`、`current_generation` 和 `artifact_kind=progress`
- [x] **AC-9**: GIVEN settings artifact 进入 Quarantined 且 progress artifact 仍为 Safe，WHEN 计算 `continue_availability`，THEN Continue 仍按 progress 输出，不得因 settings 损坏变为 Hidden 或 PreservedLocked
- [x] **AC-10**: GIVEN progress artifact 进入 Quarantined 且 settings artifact 仍为 Safe，WHEN 计算 `continue_availability`，THEN Continue 不得为 Enabled，但 settings 不得被删除或覆盖

---

## Implementation Notes

- `restore_readiness(artifact_kind)` 公式: `archive_present[K] AND artifact_state[K]=Safe AND integrity_ok[K] AND version_compatible[K] AND stable_ids_resolved[K] AND NOT migration_required[K] AND NOT quarantined[K]`
- `continue_availability` 公式:
  - `Enabled if S IN {PersistentAvailable, WriteLocked} AND A AND R`
  - `PreservedLocked if S IN {PersistentAvailable, WriteLocked} AND A AND NOT R`
  - `Hidden otherwise`
- `query_continue_state()` 输出结构:
  ```
  {
    continue_availability: String,    # "Enabled" | "PreservedLocked" | "Hidden"
    storage_capability: String,       # "PersistentAvailable" | "WriteLocked" | "EphemeralOnly"
    write_barrier_mode: String,       # "" | "SaveLocked" | "EphemeralOnly"
    reason_code: String,              # "" or specific reason when not Enabled
    checkpoint_summary: Dictionary,   # {location, world_fact, save_time}
    last_verified_checkpoint: Dictionary,
    current_generation: int,
    artifact_kind: String             # "progress"
  }
  ```
- `WriteLocked` + `restore_readiness=true` → `Enabled`，但 `write_barrier_mode=SaveLocked`
- `EphemeralOnly` 不生成可靠持久 Continue → 无旧档时 `Hidden`，有旧档但不可验证时 `Hidden`
- settings/progress 非干扰规则：settings 损坏不隐藏 progress Continue

---

## Out of Scope

- Story 003: storage_capability 的具体探测和计算
- Story 005: migration_required 的迁移链执行
- Story 006: 主档损坏时的 backup failover
- Story 007: settings/progress artifact 独立写入和回退的完整实现
- UI 呈现（由 platform-session-shell Story 007 和本 Epic Story 008 的 UI 部分覆盖）

---

## QA Test Cases

- **AC-4**: WriteLocked + valid archive → Enabled with SaveLocked
  - Given: `storage_capability=WriteLocked`、旧 Safe 可恢复
  - When: `query_continue_state()`
  - Then: `continue_availability=Enabled`、`write_barrier_mode=SaveLocked`
  - Edge cases: 新保存请求被写屏障拦截 → promotion 拒绝但旧档继续可用

- **AC-5**: Archive exists but migration required → PreservedLocked
  - Given: 旧档存在、`migration_required=true`、`migration_chain_available=false`
  - When: `query_continue_state()`
  - Then: `continue_availability=PreservedLocked`、`reason_code` 包含迁移相关原因
  - Edge cases: 版本不兼容但无迁移路径 → 同样 PreservedLocked

- **AC-10**: Progress Quarantined, settings Safe → Continue not Enabled
  - Given: progress 损坏隔离、settings 完好
  - When: `query_continue_state()`
  - Then: `continue_availability=PreservedLocked` 或 `Hidden`，settings 不受影响
  - Edge cases: 反向场景（settings Quarantined, progress Safe）→ Continue 仍按 progress 输出 Enabled

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/persistence/ContinueAvailabilityTest.csproj` — must exist and pass
**Status**: [x] PASS 10/10 — 2026-05-11

---

## Dependencies

- Depends on: Story 001 (Promotion 生成 Safe checkpoint)；Story 002 (snapshot_package_validity 是 restore_readiness 的前置)；Story 003 (storage_capability 是 continue_availability 的输入)
- Unlocks: Story 005 (Migration — PreservedLocked 触发迁移入口)；Story 008 (Desktop Lifecycle — 恢复时 Continue 判定)
