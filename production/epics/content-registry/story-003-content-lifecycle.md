# Story 003: Content Lifecycle

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-001`, `TR-registry-003`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order
**ADR Decision Summary**: Registry 管理内容定义生命周期——Draft→Active→Deprecated→Retired。Active ID 不可复用。Deprecated 内容仍可解析但新内容不应引用。Retired 仅用于旧存档兼容迁移。fantasy-critical ID（route/location/repair-node/home-space/home-anchor/companion）一旦 Active 不能改义。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 状态机为纯 GDScript 枚举+字典；生命周期变更通过信号通知下游（`content_status_changed`）。

**Control Manifest Rules (Foundation layer)**:
- Required: 状态转换遵循单向路径——Draft→Active→Deprecated→Retired，不可逆向
- Forbidden: `hardcoded_value` — 生命周期策略（deprecated_reference_policy 等）从 tuning knobs 配置读取
- Guardrail: 单次状态变更 <0.1ms（字典查找+信号 emit）

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN 内容经历 Draft → Active → Deprecated → Retired，WHEN 以稳定 ID 查询或引用它，THEN 状态必须被明确识别，不得与 NOT_FOUND 混淆
- [ ] **AC-2**: GIVEN 某个 Active ID 已退役 (Retired)，WHEN 新内容尝试复用该 ID，THEN 校验必须失败 (ERR_ID_REUSE)
- [ ] **AC-3**: GIVEN 某个 fantasy-critical ID（route/location/repair-node/home-space/home-anchor/companion）已进入 Active，WHEN 后续内容包试图把该 ID 改义为另一个地点/房间/伙伴/修复目标/航线，THEN 校验返回 ID 改义或复用错误
- [ ] **AC-4**: GIVEN `home-space`、`home-anchor` 或 `companion` 的运行时状态发生升级/模块替换/关系进展，WHEN 保存或恢复该状态，THEN 状态变化必须引用原稳定 ID，不能通过替换静态 ID 表达
- [ ] **AC-5**: GIVEN 旧存档引用 Deprecated 或 Retired ID，WHEN 注册表解析该 ID，THEN 返回生命周期状态和迁移提示（迁移表引用）——具体存档迁移由 Persistence 执行
- [ ] **AC-6**: GIVEN 新 Active 内容尝试新增对 Deprecated ID 的引用（非兼容路径），WHEN 运行引用校验，THEN 返回 `ERR_REFERENCE_TO_DEPRECATED`
- [ ] **AC-7**: GIVEN 新内容引用 Retired ID，WHEN 运行引用校验，THEN 返回 `ERR_REFERENCE_TO_RETIRED`——仅旧存档兼容解析允许

---

## Implementation Notes

*Derived from ADR-0001 + GDD Lifecycle rules:*

- 状态机使用枚举 `enum ContentStatus { DRAFT, ACTIVE, DEPRECATED, RETIRED }`
- 合法转换: DRAFT→ACTIVE, ACTIVE→DEPRECATED, DEPRECATED→RETIRED
- 非法转换（DRAFT→RETIRED 跳过 Active、ACTIVE→DRAFT 回退）→记录诊断 warning 并拒绝
- Retired ID 存储在独立字典 `retired_ids: Dictionary[StringName, RetiredRecord]`，包含 migration_target 和 retirement_reason
- fantasy-critical ID 改义检测：新内容注册时，若 ID 已存在于 retired_ids 或当前 active_entries 但 owner_domain/kind 不同→ERR_ID_REUSE
- 迁移提示结构：`{original_id, status, suggested_replacement_id, migration_note, retired_date}`
- 状态变更通过信号 `content_status_changed(id, old_status, new_status)` emit-after-mutation

---

## Out of Scope

- Story 004: 引用完整性校验（Deprecated/Retired 引用策略由 Story 004 的 reference_validity 执行）
- Story 005: 内容域加载状态（UNLOADED/LOADING/COMPLETE——独立于内容生命周期）
- Persistence (#3): 存档迁移执行——Registry 只返回迁移提示，实际存档迁移由 ADR-0003 管理

---

## QA Test Cases

- **AC-1**: Lifecycle states distinguishable
  - Given: 分别存在 Draft/Active/Deprecated/Retired 状态的内容
  - When: `query_entity(id)` 查询每种状态
  - Then: 每种状态返回明确的 status 字段——不得将 Deprecated 或 Retired 报告为 NOT_FOUND
  - Edge cases: Retired ID 在 retired_ids 中存在但不在 active_entries 中

- **AC-2**: Retired ID reuse prevention
  - Given: ID `route.old-passage` 已 Retired
  - When: 新内容尝试使用相同 ID 注册
  - Then: 返回 ERR_ID_REUSE，新内容注册失败
  - Edge cases: 大小写变体视为不同 ID（由 Story 001 格式校验处理）

- **AC-3**: Fantasy-critical ID redefinition blocked
  - Given: `location.glass-harbor` 为 Active（kind=location, region_tag=starter-sea）
  - When: 新内容包试图用同一 ID 注册为 kind=repair-node 或 region_tag=storm-belt
  - Then: 返回 ID 改义错误
  - Edge cases: 同 ID 相同 kind/owner_domain 但新增兼容字段→合法（非改义）

- **AC-5**: Deprecated/Retired ID resolution for old saves
  - Given: 旧存档引用 `resource.old-iron`（已 Deprecated→Retired）
  - When: `resolve_legacy_id("resource.old-iron")`
  - Then: 返回 `{status: "retired", migration_target: "resource.iron-ore", note: "..."}  `
  - Edge cases: Retired 且无迁移目标→返回 null migration_target + 说明

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/registry/content_lifecycle_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (ID Registry Core —— 生命周期管理依赖 ID 注册机制)
- Unlocks: Story 005 (Domain Loading 需区分 Active vs Draft 内容)
