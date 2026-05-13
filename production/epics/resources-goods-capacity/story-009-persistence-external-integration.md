# Story 009: Persistence & External Integration

> **Epic**: Resources, Goods & Capacity
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-001`, `TR-resources-003`
**GDD Acceptance Criteria**: `AC-RES-008.1` through `AC-RES-008.3`, `AC-RES-009.3`, `AC-RES-010.1` through `AC-RES-010.3`, and the MVP Starting State table

**ADR Governing Implementation**: ADR-0005 (持久化集成), ADR-0003 (Save System / JSON Serialization), ADR-0001 (Autoload Boot Order)
**ADR Decision Summary**: 池 1-3（on_person、in_storage、loaded）通过 Canonical JSON 快照包持久化为 `progress.resources`。ResourcesManager 注册 domain serializer/de-serializer 到 Persistence。起始状态通过 `reset_for_new_game(starting_snapshot)` 注入。容量加成（carry_slot_bonus 等）也持久化。模块/货舱交互（EC-05 战斗摧毁货物损失）、供给类别查询（get_carried_intel、get_carried_contents_by_tag）、版本间迁移边界（EC-08/EC-09 mass_class/max_stack 变更）均由本系统处理。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `JSON.stringify()` 对 Dictionary[StringName] 自动转换 StringName → String；反序列化需显式 `StringName(str)`

**Control Manifest Rules (Foundation layer)**:
- Required: 快照仅含稳定 ID + 数量 + 池归属 + 容量加成；`reset_for_new_game()` 注入起始状态
- Forbidden: `bare_dictionary_payload`（快照中无 Object/Node/Resource 引用）；持久化显示名/模型引用
- Guardrail: 探索中非稳定边界不产生存档（carried 不持久化）

---

## Acceptance Criteria

### Snapshot Serialization (Save)

- [x] **AC-1**: GIVEN 仓库 basic × 10, 货舱 cargo.iron × 1 (linked=resource.iron, Q=30, mass_class=medium), 随身空，WHEN `_serialize_resources()`，THEN 返回 Dictionary 含 `domain="resources"`, pools 含 on_person/in_storage/loaded 完整堆结构
- [x] **AC-2**: GIVEN 快照 payload，WHEN 检查内容，THEN 不含显示名、文件路径、Object/Node/Resource 引用——仅有 resource_id (StringName/string)、quantity (int)、槽位索引

### Snapshot Deserialization (Load)

- [x] **AC-3**: GIVEN 有效快照（仓库 basic × 10, 货舱有 1 货物），WHEN `_deserialize_resources(snapshot)`，THEN `_pools` 恢复到保存时状态——所有 resource_id、quantity、槽位索引一致
- [x] **AC-4**: GIVEN 快照含容量加成（carry_slot_bonus=2），WHEN 反序列化，THEN `get_carry_capacity()` 返回 5+2=7

### Reset for New Game

- [x] **AC-5**: GIVEN `reset_for_new_game(starting_snapshot)`，WHEN 起始快照含 in_storage: basic×10 + repair×4，THEN 仓库初始化 basic×10 (1 堆) + repair×4 (1 堆), 随身空, 货舱空, 容量加成归零
- [x] **AC-6**: GIVEN `reset_for_new_game()` 被调用，WHEN 调用前有旧数据，THEN 所有旧池数据被清除（全新起始状态）

### Module & Cargo Bay Interaction

- [x] **AC-7**: GIVEN 货舱有货物（used_volume > 0），WHEN 模块系统查询 `get_cargo_bay_usage()`，THEN 返回 `{used_volume: N, stacks: [...]}`，模块系统据此阻止模块移除
- [x] **AC-8**: GIVEN 模块被战斗摧毁（货舱中有 5 堆不同货物），WHEN 摧毁处理执行，THEN 约 40% 货物进入 destroyed（EC-05 公式：loss = min(Q-1, max(1, ceil(Q×0.4)))）, 保留部分生成 recoverable_crate 临时状态, 货舱容积归零, 玩家收到损失通知
- [x] **AC-9**: GIVEN 货舱清空（used_volume=0），WHEN 模块系统调用移除模块，THEN `get_cargo_bay_usage()` 返回 used_volume=0, 模块移除允许

### Supply Class & Tag Queries

- [x] **AC-10**: GIVEN `get_carried_intel()`，WHEN 随身有 basic×5 + intel×1 + repair×3，THEN 仅返回 intel 物品（supply_class=intel）
- [x] **AC-11**: GIVEN `get_carried_contents_by_tag("repair-material")`，WHEN 随身有 repair_kit (material_tags=["repair-material"]) × 3 和 basic (material_tags=["basic-supply"]) × 5，THEN 仅返回 repair_kit 条目
- [x] **AC-12**: GIVEN `get_carried_contents_by_tag("nonexistent")`，WHEN 查询不存在的 tag，THEN 返回空 Dictionary（非错误）

### Starting State Bootstrap

- [x] **AC-13**: GIVEN 新游戏开始，WHEN 起始状态注入完成，THEN 仓库 basic×10 + repair×4, 货舱容积 500（模块预装）, 其他池空

### Version Migration Boundaries

- [x] **AC-14**: GIVEN 存档中 max_stack 旧值=99, 新版本 basic max_stack=50，WHEN 加载时 basic 堆 E=80（超出新上限），THEN 拆分为 50+30（容量允许时），若无容量则余量进入 destroyed + 通知玩家
- [x] **AC-15**: GIVEN 存档中资源 mass_class=medium, 新版本 mass_class=heavy（volume=200），WHEN 加载时重新计算容积，THEN 若超出池容量则部分进入 destroyed + 迁移日志条目

---

## Implementation Notes

### Domain Serializer Registration

```text
func _ready() -> void:
    # Phase 5 foundation_ready — Persistence 已 ready
    Persistence.register_domain_serializer("resources", _serialize_resources, _deserialize_resources)
```

### Serialize Resources (ADR-0005 Section 7)

```text
func _serialize_resources() -> Dictionary:
    return {
        "domain": "resources",
        "version": 1,
        "pools": {
            "on_person": _serialize_pool(&"on_person"),
            "in_storage": _serialize_pool(&"in_storage"),
            "loaded": _serialize_pool(&"loaded"),
        },
        "bonuses": {
            "carry_slot_bonus": _carry_slot_bonus,
            "carry_volume_bonus": _carry_volume_bonus,
            "storage_volume_bonus": _storage_volume_bonus,
            "cargo_module_volume_bonus": _cargo_module_volume_bonus,
        },
    }

func _serialize_pool(pool_id: StringName) -> Dictionary:
    # 序列化每个堆: [{resource_id: StringName, quantity: int}, ...]
    # StringName → JSON string (Godot 自动转换)
```

### Deserialize Resources

```text
func _deserialize_resources(snapshot: Dictionary) -> void:
    # 清空所有池
    _pools.clear()
    _init_pools()
    # 恢复池 1-3
    for pool_id in [&"on_person", &"in_storage", &"loaded"]:
        _restore_pool(pool_id, snapshot["pools"][pool_id])
    # 恢复容量加成
    var bonuses = snapshot.get("bonuses", {})
    _carry_slot_bonus = bonuses.get("carry_slot_bonus", 0)
    # ...
```

### Reset for New Game

```text
func reset_for_new_game(starting_snapshot: Dictionary) -> void:
    _pools.clear()
    _init_pools()
    _bonuses.clear()
    # 注入起始状态（由 Persistence 在 new_game() 时调用）
    _deserialize_resources(starting_snapshot)
```

### EC-05: Module Destroyed — Cargo Loss

```text
func handle_cargo_bay_module_destroyed() -> Dictionary:
    # 1. 对货舱中每个货物堆计算损失：loss = min(Q-1, max(1, ceil(Q×0.4)))
    # 2. retention = Q - loss
    # 3. 损失部分 → destroyed（emit resource_removed）
    # 4. 保留部分 → recoverable_crate 临时状态（世界空间放置）
    # 5. 货舱容积归零: _cargo_module_volume_bonus = 0
    # 6. 返回 {losses: [{cargo_id, resource_id, loss_qty, retention_qty}, ...], crates: [...]}
```

### Supply Class Queries

```text
func get_carried_intel() -> Dictionary:
    # 过滤 carried 池中 supply_class=intel 的资源
    # 返回 {resource_id: quantity, ...}

func get_carried_contents_by_tag(material_tag: StringName) -> Dictionary:
    # 过滤 carried 池中 material_tags 包含给定标签的资源
    # 返回 {resource_id: quantity, ...}
```

### Version Migration (EC-08/EC-09)

加载时在 `_deserialize_resources()` 中:
1. 对每堆检查 max_stack → 若超出则拆分
2. 对每堆检查 mass_class → 若变更则用新 mapping 重新计算
3. 重算容积 → 若超出池容量则超出部分进入 destroyed
4. 写入迁移日志

---

## Out of Scope

- Persistence 的保存调度和触发时机（local-save-persistence Epic 拥有）
- 探索中 `carried` 池的持久化排除（carried 不持久化——本 Story 的序列化仅含池 1-3）
- 模块移除 UI（飞艇模块 UI）
- `recoverable_crate` 的世界空间放置和拾取（Story 005/006 + Exploration）
- 版本迁移日志的持久化格式（Persistence Epic）

---

## QA Test Cases

- **AC-3**: Snapshot round-trip fidelity
  - Given: 仓库 basic×10 (1 堆), 货舱 cargo.iron × 1, 随身 repair×4
  - When: serialized → _pools cleared → deserialized → get_storage_summary()
  - Then: 仓库返回 basic×10, 货舱返回 cargo.iron×1, 随身返回 repair×4
  - Edge cases: 空池 → serialized/deserialized 后仍为空

- **AC-8**: Module destroyed cargo loss
  - Given: 货舱 5 堆货物 (Q values: 10, 5, 1, 20, 3)
  - When: `handle_cargo_bay_module_destroyed()`
  - Then: Q=1 堆 loss=0 (EC-05 单堆保护); 其他堆按公式 loss ≥ 1; crates 生成; 货舱容积归零
  - Edge cases: 货舱为空 → 无损失, 无 crates, 容积归零

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/resources/PersistenceIntegrationTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-13 (15/15 checks)

---

## Completion Notes

**Completed**: 2026-05-13
**Criteria**: 15/15 passing
**Deviations**: None. Readiness metadata was corrected from nonexistent `TR-resources-009` to active `TR-resources-001` and `TR-resources-003`; direct GDD anchors are `AC-RES-008`, `AC-RES-009.3`, `AC-RES-010`, and the MVP Starting State table.
**Test Evidence**: Integration — `tests/integration/resources/PersistenceIntegrationTest.csproj` passes 15/15 checks.
**Code Review**: Complete — APPROVED. Local review found no blocking ADR, architecture, standards, or testability issues; review-mode subagents were not spawned because Codex delegation requires an explicit user request.

---

## Dependencies

- Depends on: Story 001-008 (all resources logic), local-save-persistence Epic (snapshot serialization framework)
- Unlocks: modules-hull-state Epic (module/cargo interaction), Exploration Epic (carried→storage settlement), Settlement Epic (market inventory persistence)
