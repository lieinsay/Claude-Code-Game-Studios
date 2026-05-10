# Story 007: Artifact Isolation (settings / progress)

> **Epic**: Local Save / World State Persistence
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-007`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization
**ADR Decision Summary**: 设置与游戏进度必须逻辑分离。settings 写入失败不得污染游戏进度；游戏进度损坏不得删除可用设置。settings 与 progress 分别维护 artifact state、generation、manifest pointer、checksum、backup 和 reason code。`continue_availability` 只由 progress artifact 的恢复结果决定；settings artifact 损坏或不可写只能回退设置，不得隐藏、锁定或删除可恢复的 progress Continue。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 两个 artifact kind 作为独立 key 前缀持久化；各自拥有 staging 目录和 backup 工件。

**Control Manifest Rules (Foundation layer)**:
- Required: settings/progress 独立 generation/pointer/checksum/reason_code；非干扰回退
- Forbidden: 一侧失败不得删除或覆盖另一侧
- Guardrail: 每个 artifact kind 必须满足 durable metadata contract 全部字段

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 仅 settings artifact 变化且 progress artifact 未变化，WHEN 设置保存与恢复完成，THEN settings 值恢复为新值，progress artifact 保持最近已验证版本不变
- [ ] **AC-2**: GIVEN 仅 progress artifact 变化且 settings artifact 未变化，WHEN 进度保存与恢复完成，THEN progress 值恢复为新值，settings artifact 保持最近已验证版本不变
- [ ] **AC-3**: GIVEN settings 写入失败但 progress promotion 成功，WHEN 恢复会话，THEN progress 使用最近已验证值，settings 回退到最近已验证设置值，二者不得互相删除或覆盖
- [ ] **AC-4**: GIVEN progress 写入失败但 settings promotion 成功，WHEN 恢复会话，THEN settings 使用最近已验证设置值，progress 回退到最近已验证进度值，二者不得互相删除或覆盖
- [ ] **AC-5**: GIVEN settings artifact 进入 Quarantined 且 progress artifact 仍为可恢复 Safe，WHEN 计算 `continue_availability`，THEN Continue 仍按 progress 输出，不得因 settings 损坏变为 Hidden 或 PreservedLocked
- [ ] **AC-6**: GIVEN progress artifact 进入 Quarantined 且 settings artifact 仍为 Safe，WHEN 计算 `continue_availability`，THEN Continue 不得为 Enabled，但 settings 不得被删除或覆盖
- [ ] **AC-7**: GIVEN `current_generation`、manifest pointer、`last_verified_checkpoint`、`checkpoint_summary`、reason code 和 backup promotion result，WHEN 持久化，THEN 所有 metadata 字段以 `artifact_kind` 作为 key 前缀分别存储；settings 和 progress 的 generation 不共用同一记录
- [ ] **AC-8**: GIVEN `storage_capability` 对 settings 和 progress 不同步计算，WHEN 计算正式进度提交的 capability，THEN 以 `progress.storage_capability` 为准；settings 可写而 progress 不可写时，不得把游戏进度显示为已保存；progress 可写而 settings 不可写时，设置回滚不得影响进度继续点

---

## Implementation Notes

- Artifact kind 枚举: `settings` / `progress`
- Durable metadata 以 `{artifact_kind}.` 为前缀持久化:
  - `progress.current_generation` vs `settings.current_generation`
  - `progress.manifest_pointer` vs `settings.manifest_pointer`
  - `progress.last_verified_checkpoint` vs `settings.last_verified_checkpoint`
  - `progress.checkpoint_summary` vs `settings.checkpoint_summary`
  - `progress.reason_code` vs `settings.reason_code`
  - `progress.backup_generation` vs `settings.backup_generation`
- 非干扰规则实现:
  | Failure | Result |
  |---|---|
  | settings write/verify/promotion 失败，progress Safe 可用 | settings 回退；progress.continue_availability 不受影响 |
  | progress write/verify/promotion 失败，settings Safe 可用 | progress 回退；settings 不被删除或降级 |
  | settings Quarantined，progress Safe 可用 | 只重置/回退 settings；Continue 仍按 progress 计算 |
  | progress Quarantined，settings Safe 可用 | Continue 不得 Enabled，但 settings 可保留 |
- 每个 artifact kind 独立走 staging/verify/promotion 流程
- 同一稳定边界可同时触发 settings 和 progress 保存——但各自独立判定 promotion_success

---

## Out of Scope

- Story 001: staging/verify/promotion 的单次流程实现（本 Story 关注两侧独立调度）
- Story 004: continue_availability 的完整计算（本 Story 只保证 progress 侧权威性）
- Story 006: 自动备份的创建和提升（settings 和 progress 各自拥有备份）
- settings snapshot 的具体字段定义（由 platform-session-shell Epic 定义）

---

## QA Test Cases

- **AC-3**: Settings write fails, progress succeeds → independent rollback
  - Given: 稳定边界触发保存，settings staging write 失败，progress staging→verify→promotion 成功
  - When: 恢复会话
  - Then: progress 使用新 Safe、settings 回退到最近已验证 settings 值；progress 的 Continue 不受影响
  - Edge cases: 两侧同时写入失败 → 各自回退到各自最近 Safe

- **AC-5**: Settings Quarantined, progress Safe → Continue unaffected
  - Given: settings 工件结构损坏进入 Quarantined，progress 完整可恢复
  - When: `query_continue_state()`
  - Then: `continue_availability=Enabled`（基于 progress）；settings 重置为默认值
  - Edge cases: 如果 restore_readiness(settings)=false 但 progress 可恢复 → 不阻塞 Continue

- **AC-8**: Divergent storage_capability between artifacts
  - Given: settings 可写（配额足够 settings 工件）但 progress 不可写（配额不足 progress 工件）
  - When: 稳定边界触发保存
  - Then: settings 可 promotion；progress 进入 SaveLocked；UI 不显示"进度已保存"
  - Edge cases: 相反场景 → progress 可写、settings 不可写时 settings 回退，progress Continue 正常

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/persistence/ArtifactIsolationTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (promotion 流程)；Story 002 (snapshot_package_validity)；Story 004 (continue_availability——本 Story 实现非干扰规则)
- Unlocks: Story 008 (Desktop Lifecycle——suspend_requested 时需判断两侧 artifact 各自是否需要 best-effort flush)
