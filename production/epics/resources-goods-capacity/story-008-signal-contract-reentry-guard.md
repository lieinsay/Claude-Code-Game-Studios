# Story 008: Signal Contract & Reentry Guard

> **Epic**: Resources, Goods & Capacity
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-001`
**GDD Acceptance Criteria**: `AC-RES-012.1` through `AC-RES-012.12` (resource signal contract, emit-after-mutation, and reentry guard)

**ADR Governing Implementation**: ADR-0005 (7 typed signals), ADR-0002 (Signal Communication Protocol)
**ADR Decision Summary**: 7 个 typed signal（pool_changed、resource_added、resource_removed、transfer_completed、cargo_unpacked、deposit_committed、mass_changed）+ 1 个 pairing failed signal（deposit_failed）。所有信号在状态变更完成后触发（emit-after-mutation），不在操作中途触发。信号处理器不得在回调中调用变更方法——返回 `ERR_BUSY`；可安全调用查询方法。Godot 4.6 信号默认同步派发（`emit()` 立即执行回调后返回）。pool_changed 在所有其他操作信号之后 emit（聚合通知）。信号参数使用 typed params（禁止 Dictionary payload）。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Godot 4.6 signal 默认同步派发；typed signal 参数语法: `signal name(param: Type)`；`emit()` 立即执行所有已连接回调

**Control Manifest Rules (Foundation layer)**:
- Required: signal 使用 typed params；emit-after-mutation；pool_changed 最后 emit
- Forbidden: `dictionary_signal_payload`；`untyped_signal_param`；操作中途 emit 信号；信号回调中调用变更方法
- Guardrail: signal cascade depth ≤2；重入变更返回 `ERR_BUSY`

---

## Acceptance Criteria

### Signal Emission on Success

- [x] **AC-1**: GIVEN `add(in_storage, basic_id, 5)` 成功，WHEN 信号触发，THEN `resource_added(in_storage, basic_id, 5)` 和 `pool_changed(in_storage)` 各触发 1 次
- [x] **AC-2**: GIVEN `remove(in_storage, basic_id, 3)` 成功，WHEN 信号触发，THEN `resource_removed(in_storage, basic_id, 3)` 和 `pool_changed(in_storage)` 各触发 1 次
- [x] **AC-3**: GIVEN `transfer(in_storage, on_person, basic_id, 5)` 成功，WHEN 信号触发，THEN `transfer_completed(in_storage, on_person, basic_id, 5)` + `pool_changed(in_storage)` + `pool_changed(on_person)`
- [x] **AC-4**: GIVEN `unpack_cargo(cargo_slot)` 成功（cargo_id="cargo.iron", linked="resource.iron", Q=30），WHEN 信号触发，THEN `cargo_unpacked("cargo.iron", "resource.iron", 30)` + `pool_changed(loaded)` + `pool_changed(in_storage)`
- [x] **AC-5**: GIVEN `commit_deposit(repair_node_id, costs)` 成功，WHEN 信号触发，THEN `deposit_committed(repair_node_id)` 触发
- [x] **AC-6**: GIVEN 货舱 `add(loaded, heavy_cargo, 1)` 成功（weight=6），WHEN 信号触发，THEN `mass_changed(6)` 触发

### Signal NOT Emitted on Failure

- [x] **AC-7**: GIVEN `add(carry, unknown_id, 5)` 失败（随便满），WHEN 操作完成，THEN 所有 7 个信号触发 0 次
- [x] **AC-8**: GIVEN `transfer(...)` 因源不足失败，WHEN 操作完成，THEN `transfer_completed` 触发 0 次, `pool_changed` 触发 0 次

### Signal Emission Order

- [x] **AC-9**: GIVEN `transfer(in_storage, on_person, basic_id, 5)` 成功，WHEN 记录信号触发顺序，THEN `transfer_completed` 在 `pool_changed(in_storage)` 和 `pool_changed(on_person)` 之前触发

### Emit-After-Mutation

- [x] **AC-10**: GIVEN `pool_changed` 的回调中调用 `get_storage_summary()`，WHEN 查询返回数据，THEN 数据反映已完成的变更（新资源已出现——非操作前旧状态）
- [x] **AC-11**: GIVEN `resource_added` 的回调中调用 `get_storage_summary()`，WHEN 查询返回数据，THEN 新增资源已包含在摘要中

### Reentry Guard (ERR_BUSY)

- [x] **AC-12**: GIVEN `resource_added` 信号回调中调用 `add(in_storage, basic_id, 5)`，WHEN 回调执行，THEN 返回 `ERR_BUSY`，原操作不受影响
- [x] **AC-13**: GIVEN `pool_changed` 信号回调中调用 `get_storage_summary()`，WHEN 查询执行，THEN 正常返回数据（查询方法不受 BUSY 限制）

### deposit_failed Signal

- [x] **AC-14**: GIVEN `commit_deposit(node, costs)` 因资源不足失败，WHEN 操作完成，THEN `deposit_failed(node, "ERR_SOURCE_INSUFFICIENT")` 触发（非调用方消费者如 UIManager 可响应）

### Signal for discard()

- [x] **AC-15**: GIVEN `discard(carry, basic_id, 3)` 成功，WHEN 信号触发，THEN `resource_removed(carry, basic_id, 3)` 和 `pool_changed(carry)` 触发

---

## Implementation Notes

### Signal Definitions (from ADR-0005 Section 6)

```text
# 状态变更通知 — typed params only
signal pool_changed(pool_id: StringName)
signal resource_added(pool_id: StringName, resource_id: StringName, quantity: int)
signal resource_removed(pool_id: StringName, resource_id: StringName, quantity: int)
signal transfer_completed(from_pool: StringName, to_pool: StringName,
                          resource_id: StringName, quantity: int)
signal cargo_unpacked(cargo_id: StringName, resource_id: StringName, quantity: int)
signal deposit_committed(repair_node_id: StringName)
signal deposit_failed(repair_node_id: StringName, reason: StringName)
signal mass_changed(new_mass: int)
```

### Emit-After-Mutation Pattern

```text
func add(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 1. 验证
    # 2. 执行变更（修改 _pools）
    # 3. 变更完成后 emit 信号（此时状态已完整）
    if result == SUCCESS:
        resource_added.emit(pool_id, resource_id, quantity)
        pool_changed.emit(pool_id)
    return result
```

### Reentry Guard

```text
var _busy: bool = false

func _guard_enter() -> bool:
    if _busy:
        return false  # ERR_BUSY
    _busy = true
    return true

func _guard_exit() -> void:
    _busy = false

func add(...) -> ResourceResult:
    if not _guard_enter():
        return ERR_BUSY
    # ... 执行操作 ...
    _guard_exit()
    return result
```

查询方法不检查 `_busy`——信号回调中可安全调用 `get_*`、`can_*`、`validate_*`。

### Signal Trigger Mapping

| Operation | Signals Emitted (Success) |
|-----------|--------------------------|
| `add()` | `resource_added` → `pool_changed` |
| `remove()` | `resource_removed` → `pool_changed` |
| `consume()` | `resource_removed` → `pool_changed` |
| `discard()` | `resource_removed` → `pool_changed` |
| `transfer()` | `transfer_completed` → `pool_changed`(from) → `pool_changed`(to) |
| `unpack_cargo()` | `cargo_unpacked` → `pool_changed`(loaded) → `pool_changed`(in_storage) → `mass_changed` |
| `commit_deposit()` | `deposit_committed` → `pool_changed` × N (per source pool) |
| `commit_deposit()` failure | `deposit_failed` |
| Any loaded change | `mass_changed` (if mass actually changed) |

### Signal Cascade Depth

本系统不订阅外部信号——只 emit。消费者（UIManager、FeedbackManager）监听本系统信号并更新自身状态。信号级联深度 ≤ 2（本系统 emit → consumer 处理 → consumer 可能 emit UI 更新信号）。

---

## Out of Scope

- FeedbackManager 消费信号的具体视觉/音频实现（Presentation 层）
- UIManager 消费信号的具体 UI 更新（UI/HUD Epic）
- `call_deferred()` 级联操作（下游系统自行处理）
- 信号消费者注册/连接（由各 Autoload 在 `_ready()` 中自行处理）

---

## QA Test Cases

- **AC-9**: Signal order on transfer
  - Given: 连接所有信号到记录器
  - When: `transfer(in_storage, on_person, basic_id, 5)` 成功
  - Then: 记录顺序 [transfer_completed, pool_changed(in_storage), pool_changed(on_person)]
  - Edge cases: transfer 失败 → 所有信号 0 次

- **AC-12**: Reentry returns ERR_BUSY
  - Given: `resource_added` 回调中调用 `add(in_storage, basic_id, 5)`
  - When: 触发 `add(in_storage, basic_id, 3)` → 回调执行
  - Then: 回调中的 `add()` 返回 `ERR_BUSY`, 外层 `add()` 返回 SUCCESS
  - Edge cases: 回调中 `transfer()` → 也返回 `ERR_BUSY`

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/resources/SignalContractTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-13 (15/15 checks)

---

## Completion Notes

**Completed**: 2026-05-13
**Criteria**: 15/15 passing
**Deviations**: None. Readiness metadata was corrected from nonexistent `TR-resources-008` to active `TR-resources-001`; the direct GDD anchor is `AC-RES-012.1` through `AC-RES-012.12`.
**Test Evidence**: Integration — `tests/integration/resources/SignalContractTest.csproj` passes 15/15 checks.
**Code Review**: Complete — APPROVED. Local review found no blocking ADR, architecture, standards, or testability issues; review-mode subagents were not spawned because Codex delegation requires an explicit user request.

---

## Dependencies

- Depends on: Story 005 (all core ops), Story 007 (specialized ops), ADR-0002 (Signal Protocol)
- Unlocks: FeedbackManager (消费所有信号), UIManager (消费 pool_changed)
