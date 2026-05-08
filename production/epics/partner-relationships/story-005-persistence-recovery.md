# Story 005: Persistence & State Recovery

> **Epic**: Partner & Relationships
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/partner-relationships.md`
**Requirement**: `TR-partner-003`

**ADR Governing Implementation**: ADR-0015 (§6 ADR-0003 序列化, §2 Dictionary 后端存储); ADR-0003 (Canonical JSON 快照包)
**ADR Decision Summary**: PartnerManager 的所有持久化状态通过 ADR-0003 Canonical JSON 快照包保存为 progress.partner_skycat。7 个持久化字段：name, naming_done, naming_skip_count, sniff_success_occurred, nest_state, nest_items[], sniffed_items[]。瞬态字段（cat_state, _cat_state_cooldown, _sniff_lockout_remaining）不持久化——读档后从 Hub 上下文重新派生（E.4.a）。快照时机：每次嗅辨完成后、命名提交/跳过时、nest_state 变更时。反序列化一致性修正：(1) sniff_success_occurred=true 但 sniffed_items 为空 → 修正为 false；(2) naming_state 从 naming_done + naming_skip_count 派生；(3) nest_items 顺序验证——必须匹配静态清单索引。快照原子性：sniff 结果 + nest 累积 + 命名写入在同一事务中——若快照写入失败，数据已在内存中正确，下次快照会包含完整状态。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 瞬态字段永不写入快照——读档后重派生；序列化前 .duplicate(true) 所有 Array 字段；反序列化时执行一致性修正
- Forbidden: 快照中包含 cat_state / cooldown 瞬态值；读档后以快照中的瞬态值覆盖派生值
- Guardrail: sniff_success_occurred 与 sniffed_items 不一致 → 自动修正 + warning

---

## Acceptance Criteria

### New Game Initialization

- [ ] **AC-1**: GIVEN 新游戏 + 无 progress.partner_skycat 快照，WHEN _init_new_game_state()，THEN name="" + naming_done=false + naming_skip_count=0 + sniff_success_occurred=false + nest_state=EMPTY + nest_items=[] + sniffed_items=[]

### Serialization Roundtrip

- [ ] **AC-2**: GIVEN 中间状态：name="小云" + naming_done=true + naming_skip_count=0 + sniff_success_occurred=true + sniffed_items=[A,B,C] + nest_state=ACCUMULATING + nest_items=[0,1]，WHEN _serialize_partner() → JSON → _deserialize_partner()，THEN 所有 7 字段一致无丢失
- [ ] **AC-3**: GIVEN 全部终态：name="那只猫" + naming_done=true + naming_skip_count=3 + sniff_success_occurred=true + sniffed_items=[A..F] + nest_state=FULL + nest_items=[0,1,2,3]，WHEN 序列化往返，THEN 所有字段完整恢复

### Transient Field Derivation (E.4.a)

- [ ] **AC-4**: GIVEN 存档时 cat_state=SNIFFING，WHEN 读档，THEN cat_state 由 Hub 上下文派生（非 SNIFFING）。嗅辨动画不恢复——数据已在嗅辨时提交
- [ ] **AC-5**: GIVEN 存档时 _cat_state_cooldown=0.3, _sniff_lockout_remaining=1.2，WHEN 读档，THEN cooldown=0.0, lockout=0.0。瞬态计时器重置

### Consistency Correction

- [ ] **AC-6**: GIVEN 快照中 sniff_success_occurred=true 但 sniffed_items=[]（数据损坏），WHEN _deserialize_partner()，THEN sniff_success_occurred 修正为 false + 记录 warning
- [ ] **AC-7**: GIVEN 快照中 naming_done=true + naming_skip_count=2（不一致），WHEN 反序列化，THEN naming_state 从 naming_done 派生为 COMPLETED。naming_done 是主权威字段
- [ ] **AC-8**: GIVEN 快照中 nest_items=[0,3]（跳过索引），WHEN 验证，THEN 记录 warning——但保留原始数据。不崩溃

### Snapshot Triggers

- [ ] **AC-9**: GIVEN scout_sniff() 成功，WHEN 返回前，THEN _trigger_snapshot() 被调用
- [ ] **AC-10**: GIVEN submit_partner_name() 或 skip_naming() 完成状态变更，WHEN 返回前，THEN _trigger_snapshot() 被调用
- [ ] **AC-11**: GIVEN nest_state 变更（空→first / accumulating→full 等），WHEN 变更完成，THEN _trigger_snapshot() 被调用

### Naming Recovery (E.1.e)

- [ ] **AC-12**: GIVEN 存档时 naming_state=PROMPTED + naming_skip_count=2，WHEN 读档，THEN naming_state 派生为 PENDING + skip_count=2。下次 player_returned_to_hub 触发命名 UI（还有 1 次机会）
- [ ] **AC-13**: GIVEN 存档时 naming_state=PROMPTED + naming_skip_count=3，WHEN 读档，THEN naming_state=COMPLETED + name="那只猫"。窗口已关闭

---

## Implementation Notes

### Serialization

```gdscript
func _serialize_partner() -> Dictionary:
    var p := partners[MVP_PARTNER_ID]
    return {
        "domain_id": "partner_skycat",
        "name": p["name"],
        "naming_done": p["naming_done"],
        "naming_skip_count": p["naming_skip_count"],
        "sniff_success_occurred": p["sniff_success_occurred"],
        "nest_state": p["nest_state"],
        "nest_items": p["nest_items"].duplicate(true),
        "sniffed_items": p["sniffed_items"].duplicate(true),
    }
```

### Deserialization with Consistency Correction

```gdscript
func _deserialize_partner(snapshot: Dictionary) -> void:
    var p := partners[MVP_PARTNER_ID]
    p["name"] = snapshot.get("name", "")
    p["naming_done"] = snapshot.get("naming_done", false)
    p["naming_skip_count"] = snapshot.get("naming_skip_count", 0)
    p["sniff_success_occurred"] = snapshot.get("sniff_success_occurred", false)
    p["nest_state"] = snapshot.get("nest_state", NEST_EMPTY)
    p["nest_items"] = snapshot.get("nest_items", [])
    p["sniffed_items"] = snapshot.get("sniffed_items", [])

    # 一致性修正 1: sniff flag vs items
    if p["sniff_success_occurred"] and p["sniffed_items"].size() == 0:
        push_warning("Partner: sniff_success_occurred=true but sniffed_items empty — correcting")
        p["sniff_success_occurred"] = false

    # 一致性修正 2: naming_state 派生
    if p["naming_done"]:
        p["naming_state"] = NAMING_COMPLETED
    elif p["naming_skip_count"] > 0:
        p["naming_state"] = NAMING_PENDING
    else:
        p["naming_state"] = NAMING_PENDING

    # 一致性修正 3: nest_state 从 nest_items 派生
    var size := p["nest_items"].size()
    var derived_nest: int
    match size:
        0: derived_nest = NEST_EMPTY
        1: derived_nest = NEST_FIRST
        2, 3: derived_nest = NEST_ACCUMULATING
        _: derived_nest = NEST_FULL
    if derived_nest != p["nest_state"]:
        push_warning("Partner: nest_state mismatch — correcting from %d to %d" % [p["nest_state"], derived_nest])
        p["nest_state"] = derived_nest

    # 瞬态字段重派生（E.4.a）
    cat_state = CAT_SLEEPING_ON_INTEL_STATION
    _cat_state_cooldown = 0.0
    _sniff_lockout_remaining = 0.0
```

### Snapshot Trigger

```gdscript
func _trigger_snapshot() -> void:
    var snapshot := _serialize_partner()
    Persistence.capture_snapshot("progress.partner_skycat", snapshot)
```

---

## Out of Scope

- Persistence.capture_snapshot() / restore_snapshot() 实现——属于 local-save-persistence Epic
- Canonical JSON 格式规范——属于 ADR-0003
- 读档后 cat_state 派生逻辑——属于 Story 001
- 快照失败 UI 警告——属于 #16 UIManager

---

## QA Test Cases

- **AC-1**: New game defaults all 7 fields
- **AC-2**: Mid-state roundtrip fidelity
- **AC-3**: Full end-state roundtrip fidelity
- **AC-4**: Transient SNIFFING not restored
- **AC-5**: Transient timers reset
- **AC-6**: sniff flag inconsistency auto-correct
- **AC-7**: naming_done inconsistency auto-correct
- **AC-8**: nest_items order gap → warning
- **AC-9/10/11**: Snapshot triggers on state change
- **AC-12**: Partial skip preserved across save/load
- **AC-13**: Completed skip locked after load

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/partner-relationships/persistence_test.gd` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (cat state derivation), Story 002 (scout_sniff snapshot trigger), Story 003 (naming/nest snapshot trigger), Story 004 (init sequence), local-save-persistence Epic (capture_snapshot, restore_snapshot)
- Unlocks: Story 006 (persistence edge cases)
