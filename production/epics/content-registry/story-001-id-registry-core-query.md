# Story 001: ID Registry Core + Query Engine

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-001`, `TR-registry-002`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order
**ADR Decision Summary**: Registry 在 Phase 2 (foundation_start) 启动为 Autoload #1；提供全游戏共享的只读 `query_entity(id)` / `list_by_kind(kind)` / `list_by_domain(domain)` 查询接口；所有列表查询按 `sort_order ASC, id ASC` 确定性排序；ID 一旦注册不可变。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 纯 GDScript 数据结构和字典查询，无引擎特定 API 依赖。

**Control Manifest Rules (Foundation layer)**:
- Required: Registry 在 Phase 2 初始化，提供全游戏共享查询
- Forbidden: `hardcoded_value` — 所有 Registry 值从内容定义加载，代码中不得硬编码
- Guardrail: Autoload `_ready()` ≤5ms（仅信号声明和内部常量）

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN registry 中存在某个稳定 ID 的唯一 Active 定义，WHEN 通过该稳定 ID 查询，THEN 只返回一份 canonical definition
- [ ] **AC-2**: GIVEN 同一稳定 ID 出现两份定义，WHEN 运行注册表校验，THEN 必须返回 `ERR_DUPLICATE_ID`，不能任意选一份通过
- [ ] **AC-3**: GIVEN ID 不符合格式规则或归一化后发生碰撞，WHEN 注册表校验，THEN 返回 ID 格式或归一化冲突错误
- [ ] **AC-4**: GIVEN 目标已加载且存在，WHEN 查询，THEN 返回定义本体
- [ ] **AC-5**: GIVEN 目标所属域未加载，WHEN 查询，THEN 返回 `UNLOADED`，不得返回 `NOT_FOUND`
- [ ] **AC-6**: GIVEN 目标不存在且所属域已加载完成，WHEN 查询，THEN 返回 `NOT_FOUND`
- [ ] **AC-7**: GIVEN 列表查询返回多条内容，WHEN 执行查询，THEN 结果按 `sort_order ASC, id ASC` 排序，且多次查询顺序一致
- [ ] **AC-8**: GIVEN 查询结果超过 `max_query_result_count`，WHEN 执行列表查询，THEN 返回受控分页或截断，不得一次性无界返回
- [ ] **AC-9**: GIVEN registry 只加载部分内容域，WHEN 查询已加载域的内容，THEN 不需要等待未加载域完成即可返回结果
- [ ] **AC-10**: GIVEN registry 只加载部分内容域，WHEN 查询未加载域的内容，THEN 返回 `UNLOADED`，且不得触发任意文件系统扫描

---

## Implementation Notes

*Derived from ADR-0001:*

- Registry 使用 `Dictionary[StringName, Dictionary]` 作为核心存储——key 为稳定 ID，value 为 canonical definition
- `query_entity(id: StringName) -> Dictionary` 返回 `{status: String, entity: Dictionary/null, error: String/null}`
- 确定性排序：先按 `sort_order` 升序，相同 `sort_order` 按 `id` 字典序升序
- ID 格式校验：`kind.slug` —— 全小写，slug 使用短横线，不得含大写/空白/非法字符
- ID 归一化：Unicode NFKC 归一化后比对，碰撞返回 `ERR_ID_NORMALIZATION_COLLISION`
- 不在 `_process()` 或 `_physics_process()` 中执行查询——纯事件驱动
- 不调用其他 Autoload 的 `_ready()`

---

## Out of Scope

- Story 002: Schema 校验（definition_validity、受控词表、运行时字段检测）
- Story 003: 内容生命周期状态机（Draft→Active→Deprecated→Retired）
- Story 004: 引用完整性校验
- Story 005: 内容域加载状态机和玩家决策 UI 门控

---

## QA Test Cases

- **AC-1**: Unique canonical query
  - Given: Registry 中存在 `resource.iron-ore` 的唯一 Active 定义
  - When: `query_entity("resource.iron-ore")`
  - Then: 返回 status="FOUND"，entity 包含 id/kind/name_key/schema_version/owner_domain 等字段
  - Edge cases: 未加载域返回 UNLOADED；不存在的 ID 返回 NOT_FOUND

- **AC-2**: Duplicate ID rejection
  - Given: 两份内容定义均使用 ID `resource.iron-ore`
  - When: `validate_all()` 或 `register_batch()`
  - Then: 返回 ERR_DUPLICATE_ID，整批注册失败，不覆盖已有定义
  - Edge cases: 大小写不同的 ID 视为不同 ID（但格式校验拒绝大写）

- **AC-3**: ID format validation
  - Given: ID `Resource.Iron-Ore` (含大写)、`resource iron ore` (含空格)、`resource.iron/ore` (含路径分隔符)
  - When: 校验 ID 格式
  - Then: 返回 ERR_INVALID_ID_FORMAT
  - Edge cases: Unicode 归一化碰撞（如全角/半角拉丁字母）

- **AC-7**: Deterministic sort order
  - Given: 5 条 resource 定义，其中 2 条 sort_order=10（id=resource.b, resource.a），3 条 sort_order=20
  - When: `list_by_kind("resource")`
  - Then: 结果顺序为 resource.a, resource.b, (sort_order=20 的 3 条按 id 排序)
  - Edge cases: sort_order 缺失→视为最大排序值排在最后；多次查询结果一致

- **AC-10**: Partial domain query independence
  - Given: domain `resources` COMPLETE, domain `airship` UNLOADED
  - When: `query_entity("module.wind-sail-mk1")`
  - Then: 返回 UNLOADED，不触发 airship 域的文件扫描
  - Edge cases: 不应因为未加载域阻塞已加载域的查询

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/registry/id_registry_core_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None — Story 001 是所有其他 Registry Story 的前置
- Unlocks: Story 002 (Schema Validation), Story 003 (Content Lifecycle), Story 004 (Reference Integrity)
