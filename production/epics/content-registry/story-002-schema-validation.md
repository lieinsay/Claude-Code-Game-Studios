# Story 002: Schema Validation

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
**ADR Decision Summary**: Registry 负责所有静态内容定义的 Schema 校验——12 种内容 kind 各有最小必填字段；受控词表字段值必须来自允许列表；定义不能包含运行时字段（库存、价格、解锁、耐久、关系值）。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 校验为纯 GDScript 字典/数组遍历——无引擎 API 依赖。校验在内容导入时执行（非每帧）。

**Control Manifest Rules (Foundation layer)**:
- Required: Schema 校验在内容注册前完成——不合规定义不得进入可查询集合
- Forbidden: `bare_dictionary_payload` — 校验错误必须使用结构化诊断事件（event_id, severity, error_code, content_id 等字段）
- Guardrail: 单次 `validate_all()` 调用 <5ms（批量校验可能涉及数百条目）

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN 内容定义满足唯一 ID、kind/schema 匹配、必填字段齐全且不含运行时字段，WHEN 计算 `definition_validity`，THEN 结果为 true
- [ ] **AC-2**: GIVEN 内容定义缺少任一 `definition_validity` 条件，WHEN 运行校验，THEN 结果为 false，并指出具体失败项（U/K/R/S）
- [ ] **AC-3**: GIVEN 定义混入库存、价格、解锁、修复、耐久、关系等运行时字段，WHEN 运行定义校验，THEN 返回 `ERR_RUNTIME_FIELD_IN_STATIC_DATA`
- [ ] **AC-4**: GIVEN 内容定义使用受控词表字段（owner_domain, kind, region_tag, settlement_need_tags 等），WHEN 运行 Schema 校验，THEN 字段值必须来自受控词表，未知值返回可诊断错误
- [ ] **AC-5**: GIVEN `location` 内容进入注册表，WHEN 运行 Schema 校验，THEN 验证 region_tag、local_identity_tags、settlement_need_tags 字段完整，不能只用宽泛 tags 兜底
- [ ] **AC-6**: GIVEN `repair-node` 或 `stall-good` 内容进入注册表，WHEN 运行 Schema 校验，THEN 验证 settlement_need_tags 和 repair_visible_state_tags 字段完整
- [ ] **AC-7**: GIVEN 下游系统尝试写回玩家态/世界态/库存态/解锁态，WHEN 调用注册表接口，THEN 操作必须被拒绝，且不产生任何状态变更
- [ ] **AC-8**: GIVEN 正常只读查询，WHEN 注册表返回内容，THEN 只能返回静态内容定义，不返回可写句柄或 runtime instance

---

## Implementation Notes

*Derived from ADR-0001 + GDD Schema rules:*

- `definition_validity = has_unique_id AND matches_kind_schema AND required_fields_present AND has_no_runtime_fields`
- 12 种 kind 各有独立的最小 Schema 校验函数——使用 kind→validator 字典映射，不由单一 mega-function 处理
- 运行时字段检测使用 denylist：`["quantity", "inventory", "unlocked", "discovered", "durability", "current_price", "relationship", "installed", "repaired"]` ——含任一即拒绝
- 受控词表通过常量字典定义——不在允许值列表内的值触发 `ERR_SCHEMA_INVALID` 并列出允许值
- 校验在 `register_batch()` 内部执行——不合规条目不进入可查询集合
- `definition_validity` 的四项条件必须全部为 true——任一 false 则整条结果为 false

---

## Out of Scope

- Story 001: ID 唯一性和格式校验（由 `has_unique_id` 依赖）
- Story 003: ID 生命周期状态校验（Active/Draft/Deprecated/Retired —— 独立于 Schema 校验）
- Story 004: 引用完整性校验（reference_validity 独立于 definition_validity）
- Story 006: 诊断优先级和错误报告格式

---

## QA Test Cases

- **AC-1**: Complete valid definition passes
  - Given: 包含唯一 ID、正确 kind/schema、所有必填字段、无运行时字段的定义
  - When: `validate_definition(entry)`
  - Then: 返回 `{valid: true}`
  - Edge cases: optional 字段缺失不影响 validity

- **AC-3**: Runtime field contamination detected
  - Given: resource 定义中包含 `current_quantity: 5` 字段
  - When: `validate_definition(entry)`
  - Then: 返回 ERR_RUNTIME_FIELD_IN_STATIC_DATA，拒绝注册
  - Edge cases: 深层嵌套的运行时字段也需要检测

- **AC-4**: Controlled vocabulary enforcement
  - Given: 定义使用 `owner_domain: "gameplay"`（不在受控词表）
  - When: Schema 校验
  - Then: 返回可诊断错误，列出允许值: `resources, airship, world, routes, intel, companions, threats`
  - Edge cases: 大小写敏感的受控词表校验

- **AC-7**: Read-only enforcement
  - Given: 下游系统调用 `registry.set_entity(...)` 或尝试修改返回的 entity 引用
  - When: 调用到达 Registry
  - Then: 返回 ERR_READONLY_REGISTRY，内部数据不变
  - Edge cases: 返回的 entity 必须是 deep copy 或 frozen dict，防止调用方修改影响注册表内部状态

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/registry/schema_validation_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (ID Registry Core —— Schema 校验依赖已注册的 ID)
- Unlocks: Story 005 (Domain Loading 需要 Schema 校验通过的内容才能 COMPLETE)
