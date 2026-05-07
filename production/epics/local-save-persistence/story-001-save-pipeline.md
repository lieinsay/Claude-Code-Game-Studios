# Story 001: Staging → Verify → Promotion Pipeline

> **Epic**: Local Save / World State Persistence
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-001`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization
**ADR Decision Summary**: 三段式保存工作流：Staging（写入暂存区→不修改 manifest pointer）→ Verify（readback+SHA-256 checksum+Schema 兼容性+稳定 ID 解析）→ Promotion（原子切换 current pointer/generation）。任何阶段失败→旧 Safe 保持不变。禁止 `store_var()`/`get_var()` Variant blob——所有快照使用 Canonical JSON。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 纯 GDScript + JSON；`user://` 映射到 IndexedDB；SHA-256 通过 `Crypto` 单例（Godot 4.x built-in）。

**Control Manifest Rules (Foundation layer)**:
- Required: Canonical JSON 序列化；SHA-256 校验；原子三阶段 promotion
- Forbidden: `store_var_save` — 禁止 Variant blob
- Guardrail: p95 编码+SHA-256 <50ms；快照 2MB ceiling

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN staging_written+readback_verified+checksum_ok+schema_compatible+stable_id_resolved+no_domain_blockers，WHEN 计算 promotion_success，THEN 结果为 true
- [ ] **AC-2**: GIVEN staging 缺字段/部分写入/checksum 不一致/Schema 不兼容/稳定 ID 不可解析/领域 blocker，WHEN promotion，THEN promotion_success=false，旧 Safe 不变
- [ ] **AC-3**: GIVEN 新保存从 Staging 开始，WHEN staging 已写入但尚未进入 Verify，THEN current_generation、manifest pointer、last_verified_checkpoint 仍指向旧 Safe
- [ ] **AC-4**: GIVEN promotion_success=true，WHEN promotion 提交发生，THEN 只能通过权威 current pointer/generation 切换让新工件成为当前继续点
- [ ] **AC-5**: GIVEN manifest pointer 指向的 generation 低于 last_verified_checkpoint.generation 或 checksum 不匹配，WHEN 启动恢复检查，THEN 该 pointer 必须被拒绝

---

## Implementation Notes

- Save pipeline: `write_staging(snapshot)` → `verify_staging(staging_id)` → `promote_safe(staging_id)` 或 `abort_staging(staging_id)`
- Canonical JSON 规则: key 按 NFC 规范化+字典序排序；float 去除 `-0.0`→`0.0`；NaN/Inf→null 并记录 warning
- SHA-256 校验: `Crypto.sha256_buffer(packed_json).hex_encode()` 比对 manifest 中记录的 checksum
- Manifest pointer: `{current_generation: int, safe_pointer: String, last_verified: {generation: int, checksum: String, timestamp: int}}`
- Generation monotonic counter: 每次成功 promotion `current_generation += 1`
- Staging 目录: `user://staging/`——promotion 后 move 到 `user://safe/`；abort 后清理

---

## Out of Scope

- Story 002: Snapshot Package 的 validity 判定
- Story 003: storage_capability 判定（PersistentAvailable/WriteLocked/EphemeralOnly）
- Story 008: pagehide/beforeunload 期间的 best-effort flush

---

## QA Test Cases

- **AC-2**: Failed promotion preserves old Safe
  - Given: Safe 存档 generation=3，staging 写入但 checksum 不匹配
  - When: `promote_safe(staging_id)`
  - Then: 返回 false；manifest pointer 仍指向 generation=3；不显示保存成功
  - Edge cases: staging 文件不存在→直接返回 false

- **AC-3**: Staging isolation
  - Given: 保存进行中，staging 已写入
  - When: 在 promotion 前查询 `last_verified_checkpoint`
  - Then: 仍指向旧 Safe generation，非 staging 数据
  - Edge cases: staging 期间游戏崩溃→重启后只加载旧 Safe（staging 自动清理）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/persistence/save_pipeline_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None — Story 001 是 persistence 的前置
- Unlocks: Story 004 (Continue Availability), Story 005 (Migration)
