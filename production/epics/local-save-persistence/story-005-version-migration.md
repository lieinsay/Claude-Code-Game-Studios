# Story 005: Version Migration

> **Epic**: Local Save / World State Persistence
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-005`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization
**ADR Decision Summary**: 迁移必须显式、单向、可中止。迁移在 staging 副本上执行，成功并验证后 promotion；失败时原工件保持 Locked，不被改写。损坏与版本不兼容必须分开处理：损坏进入 Quarantined；可解析但当前不能恢复的版本/内容不兼容进入 Locked 或迁移流程。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: 迁移链由 migration module 管理；注册表提供稳定 ID 生命周期、内容域版本和迁移提示。

**Control Manifest Rules (Foundation layer)**:
- Required: staging 副本迁移 → verify → promotion；原工件在迁移成功前保持不改写
- Forbidden: 不得直接覆写原工件；不得跳过 verify 直接 promotion
- Guardrail: migration_retry_limit = 1 per launch

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN `migration_required=true`、`migration_chain_available=false`、`parse_ok=true`、`integrity_ok=true`，WHEN 计算 `migration_outcome`，THEN 结果为 `PreservedLocked`，不得为 `AlreadyCurrent`
- [x] **AC-2**: GIVEN `migration_required=true`、`migration_chain_available=true`、`staging_ok=true`、`verify_ok=true`、`promotion_success=true`，WHEN 计算 `migration_outcome`，THEN 结果为 `Upgraded`，写入迁移记录
- [x] **AC-3**: GIVEN `migration_required=true` 且迁移过程中 `staging_ok=false`、`verify_ok=false` 或 `promotion_success=false`，WHEN 计算 `migration_outcome`，THEN 结果为 `PreservedLocked`，原工件保持不改写
- [x] **AC-4**: GIVEN `migration_required=false`、`parse_ok=true`、`integrity_ok=true`、`direct_restore_compatible=true`，WHEN 计算 `migration_outcome`，THEN 结果为 `AlreadyCurrent`
- [x] **AC-5**: GIVEN `migration_required=false`、`parse_ok=true`、`integrity_ok=true` 但 `direct_restore_compatible=false`，WHEN 计算 `migration_outcome`，THEN 结果为 `PreservedLocked`，不得为 `AlreadyCurrent`
- [x] **AC-6**: GIVEN `parse_ok=false` 或 `integrity_ok=false`，WHEN 计算 `migration_outcome`，THEN 结果为 `Quarantined`（损坏工件不管版本直接隔离）
- [x] **AC-7**: GIVEN `migration_required=true` 且迁移链可用，WHEN 执行迁移，THEN 迁移在 staging 副本上执行；原工件在 promotion 成功前不被修改
- [x] **AC-8**: GIVEN 迁移成功 promotion 后，WHEN 记录迁移结果，THEN 迁移记录包含: 旧版本号、新版本号、迁移链版本列表、各步骤耗时、最终 outcome
- [x] **AC-9**: GIVEN 同一 launch 中迁移已失败过一次（`migration_retry_limit=1`），WHEN 再次请求迁移，THEN 拒绝重试并保持 `PreservedLocked`

---

## Implementation Notes

- `migration_required` 公式: `snapshot_schema_version_older OR content_domain_versions_require_migration OR stable_id_resolution_requires_migration`
- `direct_restore_compatible` 公式: `version_compatible AND content_domain_versions_directly_compatible AND stable_id_resolution_class = AllActive`
- `migration_outcome` 公式:
  - `Quarantined if NOT parse_ok OR NOT integrity_ok`
  - `Upgraded if M AND C AND S AND V AND P`
  - `PreservedLocked if M AND (NOT C OR NOT S OR NOT V OR NOT P)`
  - `AlreadyCurrent if NOT M AND D`
  - `PreservedLocked otherwise`
- 迁移流程: `read_original → copy_to_staging → apply_migration_chain → verify_staging → promote_staging → update_migration_record`
- `migration_chain`: 从当前快照版本到目标版本的有序迁移步骤列表
- 迁移步骤格式: `{from_version, to_version, migration_fn}` — 每步独立可测试
- 迁移记录: `{artifact_kind, old_generation, new_generation, old_version, new_version, chain_versions, step_durations_ms, outcome, timestamp}`

---

## Out of Scope

- Story 002: snapshot_package_validity（迁移前需判定原包 validity）
- Story 006: 损坏工件的主备份 failover（迁移只处理可解析工件）
- 迁移链数据的具体来源——由 Registry（content-registry Epic）提供迁移提示；本 Story 只实现迁移调度和 staging/verify/promotion 流程

---

## QA Test Cases

- **AC-2**: Full migration chain succeeds → Upgraded
  - Given: 旧工件 version=1、当前 build version=3、迁移链 [1→2, 2→3] 全部可用
  - When: `execute_migration(artifact)`
  - Then: staging 副本迁移成功 → verify 通过 → promotion 成功 → `migration_outcome=Upgraded`
  - Edge cases: 迁移链中某步骤转换后的 payload 需通过 snapshot_package_validity 再验证

- **AC-3**: Migration step fails mid-chain → PreservedLocked
  - Given: 旧工件 version=1、迁移链 [1→2, 2→3]，步骤 2→3 的 verify 失败
  - When: `execute_migration(artifact)`
  - Then: staging 副本作废，原工件保持 version=1 不变，`migration_outcome=PreservedLocked`
  - Edge cases: 迁移到一半应用关闭请求 → 下次启动重读原工件，staging 残留被清理

- **AC-6**: Corrupt artifact → Quarantined (not migration)
  - Given: 旧工件 checksum 不匹配
  - When: `evaluate_artifact(artifact)`
  - Then: 直接进入 Quarantined，不尝试迁移
  - Edge cases: 结构解析失败（缺 manifest pointer）→ 同样 Quarantined

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/persistence/MigrationTest.csproj` — must exist and pass
**Status**: [x] PASS 12/12 — 2026-05-11

---

## Dependencies

- Depends on: Story 001 (staging/verify/promotion 流程——迁移复用此流程)；Story 002 (validity 判定)；Story 004 (restore_readiness 在迁移完成后重新评估)
- Unlocks: Story 006 (Backup Failover——备份也可能需要迁移)
