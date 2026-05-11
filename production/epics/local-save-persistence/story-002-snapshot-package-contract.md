# Story 002: Snapshot Package Contract

> **Epic**: Local Save / World State Persistence
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-002`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization
**ADR Decision Summary**: Snapshot Package 是领域系统与存档系统之间唯一合法的可持久化边界。不是运行时对象副本，不是任意 Dictionary dump——是领域系统主动声明的、可校验的状态包。包级校验覆盖必填字段、payload 类型白名单、canonical 编码规则、stable ID 可解析性和 domain_state 准入。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 纯 C#；payload_allowed_types_only 白名单为 bool/int/float/string/enum string/stable ID string/array/dictionary；禁止 Object/Node/Resource/Callable/Signal/RID/NodePath/PackedScene 进入 payload。

**Control Manifest Rules (Foundation layer)**:
- Required: `snapshot_package_validity` 全字段校验；payload 类型白名单 enforced at codec level
- Forbidden: `store_var_save` — 禁止 Variant blob；禁止 Node/Resource/Object 引用进入 payload
- Guardrail: encode 耗时计入 save_hot_path_budget_ms；单包 payload ≤2MB ceiling

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN 领域系统导出包含 `domain_id`、`snapshot_schema_version`、`content_domain_versions`、`stable_id_refs`、`payload`、`domain_state=Ready` 的完整包，且 `schema_version_known=true`、`domain_error_blocking=false`、payload 只含允许类型、内容域版本兼容、稳定 ID 全解析为 Active/Deprecated，WHEN 计算 `snapshot_package_validity`，THEN 结果为 true
- [x] **AC-2**: GIVEN 包缺少 `domain_id`、`snapshot_schema_version`、`content_domain_versions`、`stable_id_refs`、`payload` 或 `domain_state` 任一必填字段，WHEN 计算 validity，THEN 结果为 false 并输出缺字段 reason code
- [x] **AC-3**: GIVEN `schema_version_known=false`，WHEN 计算 validity，THEN 结果为 false，不得进入 migration 以外的 promotion 路径
- [x] **AC-4**: GIVEN `domain_state=Blocked`、`NotReady` 或 `Settling`，或 `domain_error_blocking=true`，WHEN 计算 validity，THEN 结果为 false，保留旧 Safe
- [x] **AC-5**: GIVEN 任一 stable ID 解析为 `Retired`、`NOT_FOUND`、`UNLOADED` 或 `VERSION_INCOMPATIBLE`，WHEN 计算 validity，THEN 结果为 false；Retired 不得被当作可保存成功
- [x] **AC-6**: GIVEN payload 中包含 Object/Node/Resource/Callable/Signal/RID/NodePath/PackedScene 引用，WHEN 计算 validity，THEN 结果为 false 并输出 `ERR_FORBIDDEN_TYPE_IN_PAYLOAD`
- [x] **AC-7**: GIVEN payload dictionary key 在 NFC 规范化后重复、key 不是 string、key 未 canonical bytewise ascending 排序、float 为 NaN/Inf/-Inf、或 `-0.0` 未规范化为 `0.0`，WHEN 计算 validity，THEN 结果为 false
- [x] **AC-8**: GIVEN checksum 只覆盖裸 payload 而未覆盖 snapshot_schema_version、content_domain_versions、stable_id_refs、artifact_kind、artifact_generation 和 manifest_pointer_target，WHEN 计算 validity，THEN 结果为 false（checksum 范围不足）
- [x] **AC-9**: GIVEN 同一保存工件中同一 `domain_id` 出现两次，WHEN 收集 Snapshot Package，THEN 拒绝重复包，输出 `ERR_DUPLICATE_DOMAIN_PACKAGE`

---

## Implementation Notes

- `snapshot_package_validity` 公式: `P AND F AND V AND C AND I AND R AND T AND (D=Ready) AND (NOT B)`，其中各变量对应 GDD §Formulas 定义
- `payload_allowed_types_only` 检查：DFS 遍历 payload 树，遇到非白名单类型返回 false
- Canonical key 排序：NFC 规范化 → bytewise ascending order；重复 key 检测在规范化后执行
- Float 规范化：`-0.0 → 0.0`；NaN → null + warning；Inf/-Inf → null + warning
- Checksum 覆盖范围: `canonical_encoded_payload + snapshot_schema_version + content_domain_versions + stable_id_refs + artifact_kind + artifact_generation + manifest_pointer_target`
- `domain_id` 必须唯一——同一 artifact_kind 内同一 domain_id 只允许一个包
- `domain_state` 枚举: `Ready=0` / `Blocked=1` / `NotReady=2` / `Settling=3`

---

## Out of Scope

- Story 001: Staging → Verify → Promotion 三段式流水线（本 Story 只判定包级 validity）
- Story 003: storage_capability 探测和计算
- Story 005: 版本迁移（migration_required / migration_chain）
- 领域系统如何构造 Snapshot Package——由各领域 GDD 定义

---

## QA Test Cases

- **AC-2**: Missing required field returns false
  - Given: Snapshot Package 缺少 `domain_id` 字段
  - When: `validate_snapshot_package(pkg)`
  - Then: 返回 `{valid: false, reason: "ERR_MISSING_DOMAIN_ID"}`
  - Edge cases: 所有 6 个必填字段逐一缺失 → 各自返回对应 reason code

- **AC-6**: Forbidden type in payload rejected
  - Given: payload 包含 `{"node_ref": Node}` 或 `{"res": load("res://...")}`
  - When: `validate_payload_types(payload)`
  - Then: 返回 false，reason `ERR_FORBIDDEN_TYPE_IN_PAYLOAD`
  - Edge cases: 嵌套 array/dictionary 中包含禁止类型 → DFS 捕获；空 payload → 需显式编码为空 dictionary

- **AC-9**: Duplicate domain_id rejected
  - Given: 两个包都声明 `domain_id="progress.resources"`
  - When: `collect_snapshot_packages(packages)`
  - Then: 返回 `ERR_DUPLICATE_DOMAIN_PACKAGE`
  - Edge cases: 不同 artifact_kind 可共用 domain_id（settings vs progress 各自独立）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/persistence/SnapshotPackageTest.csproj` — must exist and pass
**Status**: [x] PASS 18/18 — 2026-05-11

---

## Dependencies

- Depends on: Story 001 (Staging → Verify → Promotion Pipeline) — promotion 依赖 validity 判定
- Unlocks: Story 004 (Continue Availability — 恢复时需重新判定 snapshot_package_validity)
