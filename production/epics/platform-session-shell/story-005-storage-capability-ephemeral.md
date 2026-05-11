# Story 005: Storage Capability & Ephemeral Sessions

> **Epic**: Platform Session Shell
> **Status**: Complete — 2026-05-11
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-001`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: 壳层通过存档系统返回的 `storage_capability` 判定存储能力——不本地计算。三态：PersistentAvailable（正常保存）、WriteLocked（可读不可写——允许 Continue 但不允许新保存）、EphemeralOnly（完全不持久化——临时会话）。WriteLocked 时壳层允许进入已验证旧 Continue，同时显示"新进度当前无法可靠保存"。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: `user://` storage 可用性由 Persistence 通过 FileAccess write/readback probe 判定；SessionShell 只展示返回的 storage_capability。

**Control Manifest Rules (Foundation layer)**:
- Required: storage_capability 由存档系统返回——壳层不得本地计算或缓存判定
- Forbidden: `hardcoded_value` — 存储策略阈值从配置读取
- Guardrail: persistence_probe 处理 ≤50ms

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN 本地存储不可用或写入被阻止，WHEN 玩家选择 Start，THEN 壳层显示临时会话无保存提示；玩家确认后可进入临时会话，但不生成持久化继续点
- [x] **AC-2**: GIVEN 本地存储 API/后端 probe/配额/写入 roundtrip/策略/旧档读取状态变化，WHEN 壳层收到桌面平台侧信号，THEN 壳层只把 raw persistence_probe 传给存档系统——storage_capability 由存档系统返回（PersistentAvailable/WriteLocked/EphemeralOnly）
- [x] **AC-3**: GIVEN 存档系统返回 storage_capability=WriteLocked 且 continue_availability=Enabled，WHEN 玩家检查 Continue，THEN 壳层允许进入已验证旧 Continue，同时显示"新进度当前无法可靠保存"——不隐藏或覆盖旧继续点
- [x] **AC-4**: GIVEN 存档系统返回 storage_capability=EphemeralOnly，WHEN 玩家选择 Start，THEN 壳层显示临时会话无保存提示；玩家确认后可进入临时会话，但不生成持久化继续点
- [x] **AC-5**: GIVEN 不存在继续点，WHEN 计算 continue_availability，THEN 结果为 Hidden
- [x] **AC-6**: GIVEN 继续点存在且完整性、内容域都通过，WHEN 计算 continue_availability，THEN 结果为 Enabled
- [x] **AC-7**: GIVEN 继续点存在但完整性失败或内容域不匹配，WHEN 计算 continue_availability，THEN 结果为 PreservedLocked——继续点仍保持存在

---

## Implementation Notes

- `persistence_probe` 为 raw signal payload: `{indexed_db_available: bool, quota_bytes: int, used_bytes: int, write_test_passed: bool}`
- 壳层将此 probe 发送给 Persistence (#3) 并接收 `storage_capability` 响应
- `continue_availability` 由存档系统的 `check_continue_point()` 返回——壳层只消费结果
- Ephemeral 会话标记: `_session_flags.append("EPHEMERAL")`——存档系统跳过所有序列化
- WriteLocked 路径: 允许读取旧 continue_point 但不允许 `commit_snapshot()` 提升新快照
- 存储能力变更信号: Persistence `storage_capability_changed(old, new)`——壳层监听并更新 UI 警告

---

## Out of Scope

- Persistence (#3): 存档系统的 storage_capability 判定逻辑和 continue_point 验证
- Story 002: continue_availability 在入口 UI 中的渲染
- Story 004: WriteLocked/EphemeralOnly 在 failure_severity 中的 SoftFail 判定

---

## QA Test Cases

- **AC-3**: WriteLocked + valid Continue
  - Given: storage_capability=WriteLocked, continue_point 有效且 Enabled
  - When: 玩家选择 Continue
  - Then: 允许进入 SessionStarting + 显示持久黄色警告 "新进度无法保存"
  - Edge cases: 会话中触发保存→保存静默失败 + 玩家收到通知（非崩溃）

- **AC-7**: PreservedLocked continue point
  - Given: continue_point 存在但内容域版本不匹配
  - When: 计算 continue_availability
  - Then: PreservedLocked——Continue 按钮灰显+锁图标+tooltip "游戏内容已更新，此进度不兼容——开始新会话以继续"
  - Edge cases: 用户不得手动删除 PreservedLocked 继续点（存档系统管理）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/session/StorageCapabilityTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-11

---

## Dependencies

- Depends on: Story 001 (State Machine), Persistence (#3) — storage_capability 判定
- Unlocks: Story 002 (Entry UI 消费 continue_availability)

## Completion Notes

**Completed**: 2026-05-11
**Criteria**: 7/7 passing
**Test Evidence**: Integration test at `tests/integration/session/StorageCapabilityTest.csproj`.
**Code Review**: Local review complete — no blocking issues found.
