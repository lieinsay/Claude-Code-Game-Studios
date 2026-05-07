# Story 006: Backup Failover

> **Epic**: Local Save / World State Persistence
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-006`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization
**ADR Decision Summary**: 自动备份是独立工件，不得与主继续点共用同一记录 ID。主档损坏且备份可验证时，备份提升必须走显式 BackuPromoting → Safe 路径，并把旧主档标记为 Quarantined。不得直接用备份覆盖损坏主档；任一步失败都保持旧主档隔离、备份保留、外部 Continue 不得显示为 Enabled。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: MVP 使用单一自动备份（`backup_artifact_count=1`）；备份工件存储在 `user://backup/` 独立于 `user://safe/`。

**Control Manifest Rules (Foundation layer)**:
- Required: 备份独立工件 ID；backup promotion 走 staging → verify → promotion；旧主档 Quarantined
- Forbidden: 不得直接用备份覆盖损坏主档；不得跳过 verify 直接 promotion
- Guardrail: backup_artifact_count = 1（MVP）

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 主继续点 parse/structure/integrity/version/stable ID 任一检查失败，且 `backup_present=true`、`backup_parse_ok=true`、`backup_integrity_ok=true`、`backup_structure_ok=true`、`backup_version_compatible=true`、`backup_stable_ids_resolved=true`，WHEN 计算 `backup_failover_outcome`，THEN 结果为 `BackupPromoted`，旧主档进入 Quarantined
- [ ] **AC-2**: GIVEN 主继续点不可用，备份 parse/structure/integrity 通过但 `backup_migration_required=true` 或备份版本/稳定 ID 不能直接恢复，WHEN 计算 `backup_failover_outcome`，THEN 结果为 `BackupPreservedLocked`，Continue 必须是 `PreservedLocked` 而非 Enabled
- [ ] **AC-3**: GIVEN 主继续点不可用，且没有备份或备份 parse/structure/integrity/version/stable ID 任一检查失败，WHEN 计算 `backup_failover_outcome`，THEN 结果为 `NoUsableBackup`，Continue 不得为 Enabled
- [ ] **AC-4**: GIVEN `main_usable=true`，WHEN 计算 `backup_failover_outcome`，THEN 结果为 `NotNeeded`，现有主 Safe 继续作为当前可用继续点
- [ ] **AC-5**: GIVEN `backup_failover_outcome=BackupPromoted`，WHEN 执行备份提升，THEN 顺序为: 验证备份 → 写入 promoted staging/new generation → 读回 verify → 切换 current pointer → 标记旧主档 Quarantined；任一步失败保持旧主档隔离、备份保留
- [ ] **AC-6**: GIVEN 备份提升成功，WHEN 成为唯一可用 Safe，THEN `continue_availability` 重新计算为 Enabled，checkpoint_summary 标注"已恢复到最近可用记录"
- [ ] **AC-7**: GIVEN 备份提升失败，WHEN 恢复失败，THEN 备份保留不被删除，旧主档保持 Quarantined，Continue 不得为 Enabled

---

## Implementation Notes

- `backup_direct_restore_ok` 公式: `backup_parse_ok AND backup_integrity_ok AND backup_structure_ok AND backup_version_compatible AND backup_stable_ids_resolved AND NOT backup_migration_required`
- `backup_failover_outcome` 公式:
  - `BackupPromoted if NOT main_usable AND backup_present AND backup_direct_restore_ok`
  - `BackupPreservedLocked if NOT main_usable AND backup_present AND parse_ok AND integrity_ok AND structure_ok AND (migration_required OR NOT version_compatible OR NOT stable_ids_resolved)`
  - `NoUsableBackup if NOT main_usable`
  - `NotNeeded if main_usable`
- `main_usable` 判定: `parse_ok AND structure_ok AND integrity_ok AND version_compatible AND stable_ids_resolved`
- 备份提升流程: `validate_backup → copy_to_staging_as_new_generation → readback_verify → promote_to_safe → quarantine_original_main`
- 备份工件 key 独立: `backup_{artifact_kind}_generation` vs `{artifact_kind}_current_generation`
- 备份在每次成功 promotion 后自动创建: promotion 成功 → 旧 Safe 成为备份（覆盖旧备份）
- 备份提升记录写入诊断摘要

---

## Out of Scope

- Story 001: staging/verify/promotion 的具体实现（备份提升复用）
- Story 005: 备份的版本迁移（迁移在备份提升的 staging 阶段执行）
- Story 007: settings vs progress 备份的独立管理

---

## QA Test Cases

- **AC-1**: Main corrupt, backup valid → BackupPromoted
  - Given: 主档 checksum fail、备份完整且可直接恢复
  - When: `evaluate_backup_failover(progress)`
  - Then: `backup_failover_outcome=BackupPromoted` → 备份提升为 Safe → 旧主档 Quarantined → Continue=Enabled
  - Edge cases: 备份提升过程中 promotion 失败 → 保持旧主档 Quarantined、备份保留、Continue≠Enabled

- **AC-2**: Main corrupt, backup needs migration → BackupPreservedLocked
  - Given: 主档不可用、备份完整但 `backup_migration_required=true`
  - When: `evaluate_backup_failover(progress)`
  - Then: `backup_failover_outcome=BackupPreservedLocked` → Continue=PreservedLocked
  - Edge cases: 备份版本不兼容且无迁移链 → 同样 PreservedLocked

- **AC-3**: Main corrupt, no backup → NoUsableBackup
  - Given: 主档 parse fail、`backup_present=false`
  - When: `evaluate_backup_failover(progress)`
  - Then: `backup_failover_outcome=NoUsableBackup` → Continue=Hidden 或 PreservedLocked
  - Edge cases: 备份存在但 integrity fail → 同样 NoUsableBackup

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/persistence/backup_failover_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (promotion 流程——备份提升复用)；Story 004 (continue_availability 在备份提升后重算)；Story 005 (备份可能需要迁移)
- Unlocks: Story 007 (artifact isolation——主档 Quarantined 触发备份 failover)
