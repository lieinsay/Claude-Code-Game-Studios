# Story 006: Diagnostic System

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-002`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order
**ADR Decision Summary**: Registry 提供完整诊断系统——所有校验错误按 8 级优先级确定性排序；每个诊断事件包含 event_id/severity/error_code/content_id/field_path/reference_chain/blocking_scope/suggested_action；severity 分 info/warning/error/fatal 四级。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 诊断事件为纯字典结构；错误码为 StringName 常量；日志输出通过 `push_warning`/`push_error` (仅在调试构建中)。

**Control Manifest Rules (Foundation layer)**:
- Required: 诊断结果按 Validation Diagnostic Precedence 确定主错误；其余作为 related_errors
- Forbidden: 不得将不同错误混为同一 generic 错误
- Guardrail: 诊断生成 <1ms/条目

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN 同一内容项同时存在多个定义或引用错误，WHEN 生成诊断，THEN 按 Validation Diagnostic Precedence 选择主错误，并把其他错误列入 `related_errors`
- [ ] **AC-2**: GIVEN 所有诊断事件，WHEN 输出，THEN 必须包含: event_id, timestamp, severity, error_code, content_id, kind, status, schema_version, owner_domain, content_package, source_ref, field_path, reference_chain, query_context, blocking_scope, suggested_action
- [ ] **AC-3**: GIVEN severity 为 info/warning/error/fatal，WHEN 诊断，THEN info=不影响注册查询；warning=兼容允许但不应继续依赖；error=单项/单包不能进入可查询集合；fatal=整个注册表不可用
- [ ] **AC-4**: GIVEN 同 severity 同 priority 内的多条错误，WHEN 排序，THEN 按 `error_code ASC, content_id ASC, field_path ASC` 稳定排序
- [ ] **AC-5**: GIVEN 诊断生成完成，WHEN 返回结果，THEN 主错误按 8 级优先级选择：(1) 内容包不可用→(2) ID 身份错误→(3) Schema 错误→(4) 静态/运行时边界破坏→(5) 引用缺失→(6) 引用生命周期非法→(7) 引用图结构非法→(8) 查询不稳定

---

## Implementation Notes

*Derived from GDD Diagnostic Precedence:*

- 诊断事件结构为 typed Dictionary——所有 16 个字段在事件创建时填充
- 8 级优先级定义（从高到低）:
  1. ERR_CONTENT_PACKAGE_VERSION
  2. ERR_DUPLICATE_ID, ERR_ID_REUSE, ERR_INVALID_ID_FORMAT, ERR_ID_NORMALIZATION_COLLISION
  3. ERR_SCHEMA_INVALID, missing required field, invalid controlled vocabulary
  4. ERR_RUNTIME_FIELD_IN_STATIC_DATA, ERR_READONLY_REGISTRY
  5. ERR_MISSING_REFERENCE, UNLOADED_REFERENCE
  6. ERR_REFERENCE_TO_DRAFT, ERR_REFERENCE_TO_DEPRECATED, ERR_REFERENCE_TO_RETIRED
  7. ERR_REFERENCE_CYCLE
  8. ERR_INVALID_SORT_KEY, AMBIGUOUS_QUERY, ERR_UNSTABLE_IDENTIFIER
- `generate_diagnostic()` 收集所有错误→按优先级选主错误→其余进入 `related_errors`
- Severity 映射:
  - `info`: 内容包扫描完成、域加载完成
  - `warning`: 引用 Deprecated 内容（兼容路径）、sort_order 缺失
  - `error`: 单项/单包不能进入可查询集合——大多数 ERR_* 错误
  - `fatal`: 内容包版本不兼容、构建不兼容——整个 Registry 不可用
- 事件 ID 格式: `{timestamp_ms}-{content_id}-{error_code}` 保证唯一性

---

## Out of Scope

- Story 007: 诊断 UI（Registry Overview、Error List、Reference Graph、Copyable Report）
- Story 005: 域加载失败后的 UI 处理——诊断系统提供数据，域加载/壳层消费

---

## QA Test Cases

- **AC-1**: Multiple errors → main error precedence
  - Given: 一个 route 同时有 ERR_MISSING_REFERENCE (dest 缺失，priority 5) 和 ERR_REFERENCE_CYCLE (priority 7)
  - When: `generate_diagnostic(route_entry)`
  - Then: 主错误=ERR_MISSING_REFERENCE，related_errors=[ERR_REFERENCE_CYCLE]
  - Edge cases: 同 priority 的多个错误→按 error_code ASC 选第一个为主错误

- **AC-3**: Severity classification
  - Given: 引用 Deprecated 内容（兼容路径已有引用）
  - When: 生成诊断
  - Then: severity=warning（非 error），不阻断内容进入可查询集合
  - Edge cases: 新 Active 内容新引用 Deprecated→应升级为 error

- **AC-5**: 8-level precedence
  - Given: 内容包版本不兼容 (priority 1) + 同时有 Schema 错误 (priority 3)
  - When: 诊断
  - Then: 主错误=ERR_CONTENT_PACKAGE_VERSION，blocking_scope=registry；Schema 错误进入 related_errors
  - Edge cases: same-priority → error_code ASC 稳定排序

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/registry/diagnostic_system_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-004 (诊断系统消费所有校验器的输出)
- Unlocks: Story 007 (Diagnostic UI 消费诊断事件)
