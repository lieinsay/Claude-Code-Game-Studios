# Story 004: Reference Integrity

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-001`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order
**ADR Decision Summary**: Registry 负责引用完整性校验——所有 `references` 字段必须解析到有效定义；引用状态必须符合生命周期规则；不能有自循环或闭环依赖。所有必需引用解析失败→条目不能进入 Active。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 引用图遍历为纯 GDScript——小规模图（每个内容最多 16 条引用），性能不受影响。

**Control Manifest Rules (Foundation layer)**:
- Required: 引用校验在内容注册前完成——引用不完整的条目不能进入可查询集合
- Forbidden: `bare_dictionary_payload` — 引用错误必须包含完整引用链（如 `route → location → repair-node`）
- Guardrail: 单条目引用校验 <0.5ms（含传递引用解析）

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN 所需引用都可解析、引用状态允许且不存在非法自循环，WHEN 计算 `reference_validity`，THEN 结果为 true
- [ ] **AC-2**: GIVEN 任一引用缺失、状态非法或形成循环，WHEN 运行引用校验，THEN 结果为 false，并定位到具体引用链
- [ ] **AC-3**: GIVEN 必需引用无法解析，WHEN 校验，THEN 返回 `ERR_MISSING_REFERENCE`——引用者不能进入 Active
- [ ] **AC-4**: GIVEN 引用目标处于 Draft，WHEN Active 内容引用它，THEN 返回 `ERR_REFERENCE_TO_DRAFT`——禁止 Active 依赖 Draft
- [ ] **AC-5**: GIVEN 引用形成自循环或闭环依赖，WHEN 校验，THEN 返回 `ERR_REFERENCE_CYCLE` 并报告完整循环链路
- [ ] **AC-6**: GIVEN 引用目标所属域 UNLOADED，WHEN 解析引用，THEN 返回 `UNLOADED_REFERENCE`——不得自动加载或隐式扫描
- [ ] **AC-7**: GIVEN 查询条件不足导致多个候选内容匹配，WHEN 查询，THEN 返回 `AMBIGUOUS_QUERY`——不得猜测第一个结果

---

## Implementation Notes

*Derived from ADR-0001 + GDD Reference rules:*

- `reference_validity = required_refs_resolve AND allowed_status_refs AND no_self_invalid_cycle`
- 引用图使用邻接表——遍历深度限制 ≤ `max_references_per_item` (default 16) 防止无限递归
- 循环检测：DFS with visited set + recursion stack → 发现回边时报告完整循环路径
- 引用状态规则矩阵:
  - Active → Active: ✅
  - Active → Draft: ❌ ERR_REFERENCE_TO_DRAFT
  - Active → Deprecated: ❌ (新引用); ⚠️ (已有兼容引用，warn)
  - Active → Retired: ❌ ERR_REFERENCE_TO_RETIRED
- `references` 字段中每个 ID 都要解析——optional 引用（标记为 `optional: true`）缺失时不阻断
- 引用链展示格式: `"{source_id} → {ref_id} → {ref_of_ref_id}"` ——每层标注 status

---

## Out of Scope

- Story 003: 引用目标生命周期状态定义（Draft/Active/Deprecated/Retired）——由 Story 003 实现
- Story 006: 引用错误的诊断优先级和错误报告格式

---

## QA Test Cases

- **AC-1**: All references valid
  - Given: route 定义引用 location.glass-harbor (Active) 和 location.starlight-dock (Active)
  - When: `validate_references(route_entry)`
  - Then: 返回 `{valid: true}`
  - Edge cases: optional 引用目标不存在→不阻断 validity

- **AC-2**: Broken reference chain
  - Given: route 引用 location.missing-port（不存在）
  - When: `validate_references(route_entry)`
  - Then: 返回 false + ERR_MISSING_REFERENCE + 引用链: `route.sky-reef-arc-01 → location.missing-port`
  - Edge cases: 多层引用链中的缺失→展示完整路径

- **AC-5**: Cycle detection
  - Given: A 引用 B, B 引用 C, C 引用 A
  - When: `validate_references()` 在注册 C 时
  - Then: 返回 ERR_REFERENCE_CYCLE + 完整循环: `A → B → C → A`
  - Edge cases: 自循环 (A → A) 同样检测

- **AC-7**: Ambiguous query
  - Given: 查询条件 `{tags: ["metal"]}` 匹配到 3 条内容
  - When: 查询未指定足够条件唯一命中
  - Then: 返回 AMBIGUOUS_QUERY，不返回第一条匹配
  - Edge cases: 条件匹配 0 条→NOT_FOUND（非 AMBIGUOUS_QUERY）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/registry/reference_integrity_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (ID Registry Core), Story 003 (Content Lifecycle —— 需要状态判定引用合法性)
- Unlocks: Story 005 (Domain Loading 需要引用完整性校验完成)
