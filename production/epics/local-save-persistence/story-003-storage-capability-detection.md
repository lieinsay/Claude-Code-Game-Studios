# Story 003: Storage Capability Detection

> **Epic**: Local Save / World State Persistence
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-003`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: 本地存储能力的权威判定由存档系统拥有。平台壳只提供原始 `persistence_probe` 信号；本系统计算 `storage_capability = PersistentAvailable / WriteLocked / EphemeralOnly`。probe 必须带 TTL——过期 probe 只能保守进入 WriteLocked 或重探，不得作为 PersistentAvailable 依据。`OS.is_userfs_persistent()` 只能作为 hint——只有当前页面上下文的真实 write/flush/readback/checksum roundtrip 可以让 `write_roundtrip_ok=true`。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: `user://` 在 桌面构建中映射到 user:// storage；`FileAccess` 写入/读回 roundtrip 验证；Persistence uses FileAccess write/readback probes to classify storage capability。

**Control Manifest Rules (Foundation layer)**:
- Required: probe TTL 机制；write roundtrip 验证；quota reserve 计算
- Forbidden: 不得仅凭 `OS.is_userfs_persistent()` 判定 PersistentAvailable
- Guardrail: boot probe TTL 30s；resume/resume_requested probe TTL 10s

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN `raw_persistent_api_ok=true`、`storage_backend_probe_ok=true`、`existing_archive_read_class IN {Readable, NotApplicable}`、`quota_ok=true`、`quota_reserve_ok=true`、`write_roundtrip_ok=true`、`policy_forces_ephemeral=false`，WHEN 计算 `storage_capability`，THEN 结果为 `PersistentAvailable`
- [x] **AC-2**: GIVEN `raw_persistent_api_ok=true`、`storage_backend_probe_ok=true`、`existing_archive_read_class IN {Readable, NotApplicable}`、`policy_forces_ephemeral=false`，但 `quota_ok=false`、`quota_reserve_ok=false` 或 `write_roundtrip_ok=false`，WHEN 计算 `storage_capability`，THEN 结果为 `WriteLocked`
- [x] **AC-3**: GIVEN `raw_persistent_api_ok=false`、`storage_backend_probe_ok=false`、`existing_archive_read_class=Unreadable` 或 `policy_forces_ephemeral=true`，WHEN 计算 `storage_capability`，THEN 结果为 `EphemeralOnly`
- [x] **AC-4**: GIVEN fresh install 无旧 manifest，`existing_archive_read_class=NotApplicable`、其他探测全部通过，WHEN 计算 `storage_capability`，THEN 结果为 `PersistentAvailable`（不得因无旧档输出 EphemeralOnly）
- [x] **AC-5**: GIVEN `OS.is_userfs_persistent()` 返回 true 但 write/flush/readback/checksum roundtrip 未完成，WHEN 计算 `storage_capability`，THEN 结果不得为 `PersistentAvailable`
- [x] **AC-6**: GIVEN `quota_reserve_ok=false` 且 `existing_archive_read_class=Readable`，WHEN 计算 `storage_capability`，THEN 结果为 `WriteLocked`，旧 Safe 继续点不被覆盖
- [x] **AC-7**: GIVEN `quota_reserve_ok=false` 且 `existing_archive_read_class=Unreadable`，WHEN 计算 `storage_capability`，THEN 结果为 `EphemeralOnly`，不显示可用持久 Continue
- [x] **AC-8**: GIVEN probe TTL 已过期、write failure、readback mismatch、quota failure 或 policy change 发生，WHEN 下次 capability 查询，THEN probe 失效并重探；过期 probe 不得作为 `PersistentAvailable` 依据
- [x] **AC-9**: GIVEN `available_working_set_bytes` 无法由平台适配层提供，WHEN 计算 `quota_reserve_ok`，THEN 使用 16 MiB fallback 并在诊断中标记 `WORKING_SET_BUDGET_FALLBACK`
- [x] **AC-10**: GIVEN 平台壳需要显示存储能力或 Continue 状态，WHEN 查询，THEN 壳层必须读取本系统返回的 `storage_capability` 和 `query_continue_state()`，不得根据桌面平台 API、文件存在或本地公式重算

---

## Implementation Notes

- `storage_capability` 公式: `PersistentAvailable if A AND B AND L≠Unreadable AND Q AND H AND R AND NOT P; else WriteLocked if A AND B AND L≠Unreadable AND NOT P; else EphemeralOnly`
- `quota_reserve_ok` 公式: `available_storage_bytes >= required_bytes AND available_working_set_bytes >= peak_working_set_bytes`
  - `required_bytes = persisted_artifact_bytes + staging_artifact_bytes + backup_artifact_bytes + metadata_bytes + migration_temp_bytes + safety_margin_bytes`
  - `safety_margin_bytes = max(encoded_memory_artifact_bytes * 0.5, 512 KiB)`
  - `peak_working_set_bytes = encoded_memory_artifact_bytes + readback_copy_bytes + checksum_buffer_bytes + serialization_transient_bytes + migration_temp_bytes + backend_working_set_inflation_bytes`
- `backend_persistence_inflation_factor`: MVP default 1.5；`backend_working_set_inflation_bytes`: MVP default 256 KiB
- Probe TTL: boot probe 30s；resume/resume_requested probe 10s
- Probe cache 失效触发: write failure、readback mismatch、quota failure、policy change、iframe/cookie policy change、`resume_requested` after any suspension
- `existing_archive_read_class` 枚举: `Readable` / `Unreadable` / `NotApplicable`
- `probe_generation` 必须记录: probe ID、timestamp、trigger source、TTL

---

## Out of Scope

- Story 001: promotion 流程本身（capability 是 promotion 的前置条件）
- Story 004: Continue Availability 的最终 UI 呈现
- Story 008: 桌面生命周期事件（suspend_requested/resume_requested）对 probe 的具体触发

---

## QA Test Cases

- **AC-1**: All conditions met → PersistentAvailable
  - Given: 所有 7 个 capability 条件均为 true/good，`existing_archive_read_class=Readable`
  - When: `detect_storage_capability(probe_data)`
  - Then: 返回 `PersistentAvailable`
  - Edge cases: `existing_archive_read_class=NotApplicable` 同样输出 PersistentAvailable

- **AC-8**: Expired probe → conservative fallback
  - Given: 上次 probe 结果 PersistentAvailable，TTL=30s 已过
  - When: `get_storage_capability()` 被调用
  - Then: probe 标记为 stale，触发重探；重探完成前保守返回 WriteLocked
  - Edge cases: resume probe TTL=10s → resume_requested 后必然过期

- **AC-9**: Working set budget fallback
  - Given: 平台适配层未提供 `available_working_set_bytes`
  - When: 计算 `quota_reserve_ok`
  - Then: 使用 16 MiB fallback，诊断记录 `WORKING_SET_BUDGET_FALLBACK`
  - Edge cases: fallback 导致 quota_reserve_ok=false → storage_capability 降级为 WriteLocked

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/persistence/StorageCapabilityTest.csproj` — must exist and pass
**Status**: [x] PASS 17/17 — 2026-05-11

---

## Dependencies

- Depends on: None — Story 003 是独立的 capability 判定模块；可与 Story 001/002 并行实现
- Unlocks: Story 004 (Continue Availability 依赖 storage_capability)；Story 008 (Desktop Lifecycle 触发 probe 重探)
