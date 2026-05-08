# Story 004: Hub Event & Intel API Integration

> **Epic**: Partner & Relationships
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/partner-relationships.md`
**Requirement**: `TR-partner-001`, `TR-partner-002`

**ADR Governing Implementation**: ADR-0015 (§1 Autoload #15 启动顺序, §3 查询接口, §4 信号接口, §5c scout_sniff→#6, §5f Hub 事件处理)
**ADR Decision Summary**: PartnerManager 的集成边界涉及 3 条上游依赖和 2 条下游接口。上游：Hub #7 的 hub_state_changed / player_returned_to_hub / player_entered_zone 事件驱动猫状态机；#5 的 get_inventory_items() 供嗅辨面板过滤；#1 的 query_entity() 读取 cat_sniff_signature。下游：向 #6 单向写入——reveal_rumor(location_id, "partner.sky-cat", hazard_tags, confidence) + report_observation_event(pattern_id, "partner_sniff_success")——不读取 #6 的知识状态；新游戏 bootstrap 时排队 on_partner_joined("partner.sky-cat") 在 #6 就绪后分发。向 #7 提供 query_partner_present() / query_partner_name() / query_nest_state() / query_nest_items() 查询接口。初始化竞态处理：订阅 Hub 事件后显式调用 sync_with_hub_state(current_hub_state)（E.6.c）；on_partner_joined 由 bootstrap sequencer 排队（E.6.d）；#5 不可用时 get_sniffable_items() 返回空列表（优雅降级）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 初始化顺序——先加载快照 → 派生 cat_state → 订阅 Hub 事件 → sync_with_hub_state() → 排队 on_partner_joined；向 #6 只写不读——不缓存知识状态
- Forbidden: 在 _ready() 中调用其他 Autoload 方法；在 Hub 事件回调中同步等待 #6 API 返回
- Guardrail: #5 不可用时嗅辨面板空——不崩溃；#6 不可用时 reveal_rumor 调用静默失败（E.5.a）

---

## Acceptance Criteria

### Hub Event Integration

- [ ] **AC-1**: GIVEN PartnerManager 收到 feature_ready，WHEN _on_feature_ready()，THEN 依次: (1) 加载/初始化快照 (2) 派生 cat_state (3) Hub.hub_state_changed.connect(on_hub_state_changed) + Hub.player_returned_to_hub.connect(on_player_returned_to_hub) + Hub.player_entered_zone.connect(on_player_entered_zone) (4) sync_with_hub_state(Hub.current_state)
- [ ] **AC-2**: GIVEN Hub 发射 hub_state_changed(LANDED)，WHEN PartnerManager.on_hub_state_changed()，THEN _state_frozen=false。猫状态机恢复运转
- [ ] **AC-3**: GIVEN Hub 发射 player_returned_to_hub() + F.2 命名资格满足，WHEN on_player_returned_to_hub()，THEN naming_prompt_triggered 信号发射。命名 UI 打开

### Bootstrap & on_partner_joined

- [ ] **AC-4**: GIVEN 新游戏 + 所有系统就绪，WHEN bootstrap sequencer 分发排队事件，THEN IntelManager.on_partner_joined("partner.sky-cat") 被调用 1 次。一个会话仅 1 次
- [ ] **AC-5**: GIVEN 读档（非新游戏），WHEN 初始化，THEN on_partner_joined() 不被调用。猫已在之前的会话中加入

### Intel API Contract

- [ ] **AC-6**: GIVEN scout_sniff() 成功，WHEN 调用 IntelManager.reveal_rumor()，THEN 参数正确——location_id=reveal_target, source_tag="partner.sky-cat", hazard_tags=[hazard_hint], confidence=clamped(min(raw,66))
- [ ] **AC-7**: GIVEN IntelManager.reveal_rumor() 抛出异常或返回错误，WHEN 处理，THEN PartnerManager 捕获异常不传播。本地状态（sniffed_items, nest）仍正确提交。记录 warning
- [ ] **AC-8**: GIVEN IntelManager 不可用（null），WHEN scout_sniff() 调用，THEN 不崩溃——跳过 reveal_rumor 和 report_observation_event 调用

### Query Interface for Hub #7

- [ ] **AC-9**: GIVEN Hub 查询猫状态，WHEN query_partner_present()，THEN 返回 true。见 Story 001 AC-1
- [ ] **AC-10**: GIVEN 命名完成 name="小云"，WHEN Hub 调用 query_partner_name()，THEN 返回 "小云"
- [ ] **AC-11**: GIVEN 未命名，WHEN Hub 调用 query_partner_name()，THEN 返回 ""。Hub 应回退显示"那只灰白猫"
- [ ] **AC-12**: GIVEN nest_state=FULL，WHEN Hub 调用 query_nest_state()，THEN 返回 NEST_FULL(3)。Hub 依据此值渲染全部 4 件痕迹锚点

### #5 Resources Integration

- [ ] **AC-13**: GIVEN ResourcesManager 可用 + 背包有物品，WHEN get_sniffable_items()，THEN 正确过滤并返回有 cat_sniff_signature 的物品
- [ ] **AC-14**: GIVEN ResourcesManager 不可用，WHEN get_sniffable_items()，THEN 返回 []。优雅降级——不崩溃

### Initialization Race Handling (E.6.c)

- [ ] **AC-15**: GIVEN Hub 事件在 PartnerManager 订阅之前已发射，WHEN 订阅后调用 sync_with_hub_state()，THEN cat_state 依据当前 Hub 状态正确派生。不出现猫状态错误
- [ ] **AC-16**: GIVEN Hub 当前为 LANDED + 玩家在生活舱，WHEN sync_with_hub_state()，THEN cat_state=IDLE_LIVING_QUARTERS。不是默认的 SLEEPING_ON_INTEL_STATION

---

## Implementation Notes

### Feature Ready Sequence

```gdscript
func _on_feature_ready() -> void:
    # 1. 加载/初始化快照
    var snapshot := Persistence.restore_snapshot("progress.partner_skycat")
    if snapshot.is_empty():
        _init_new_game_state()
    else:
        _deserialize_partner(snapshot)

    # 2. 派生初始猫状态
    _derive_initial_cat_state()

    # 3. 订阅 Hub 事件
    Hub.hub_state_changed.connect(on_hub_state_changed)
    Hub.player_returned_to_hub.connect(on_player_returned_to_hub)
    Hub.player_entered_zone.connect(on_player_entered_zone)

    # 4. 显式同步——处理订阅前已发射的事件
    sync_with_hub_state(Hub.current_state)

    # 5. 新游戏 → 排队 on_partner_joined
    if snapshot.is_empty():
        BootstrapSequencer.queue_call(_dispatch_on_partner_joined)

func _dispatch_on_partner_joined() -> void:
    IntelManager.on_partner_joined(MVP_PARTNER_ID)
```

### Sync with Hub State

```gdscript
func sync_with_hub_state(hub_state: int) -> void:
    match hub_state:
        HUB_LANDED:
            _state_frozen = false
            if Hub.is_player_in_zone(&"living_quarters"):
                _force_cat_state(CAT_IDLE_LIVING_QUARTERS)
            else:
                _force_cat_state(CAT_SLEEPING_ON_INTEL_STATION)
        HUB_IN_TRANSIT:
            _state_frozen = true
            _force_cat_state(CAT_IDLE_LIVING_QUARTERS)  # 逻辑态
        HUB_ARRIVAL:
            _state_frozen = false
            _force_cat_state(CAT_IDLE_LIVING_QUARTERS)
        HUB_DEPARTURE_LOCKED:
            _state_frozen = true
```

### Intel API Safety

```gdscript
func _safe_reveal_rumor(reveal_target: StringName, hazard_hint: StringName, confidence: int) -> void:
    if IntelManager == null or not IntelManager.has_method("reveal_rumor"):
        push_warning("Partner: IntelManager unavailable — rumor not delivered")
        return
    IntelManager.reveal_rumor(reveal_target, &"partner.sky-cat", [hazard_hint], confidence)
```

---

## Out of Scope

- Hub.hub_state_changed / player_returned_to_hub / player_entered_zone 信号发射——属于 airship-hub Epic
- IntelManager.reveal_rumor / report_observation_event / on_partner_joined 实现——属于 intel-knowledge Epic
- BootstrapSequencer.queue_call 实现——属于 platform-session-shell Epic
- ResourcesManager.get_inventory_items 实现——属于 resources-goods-capacity Epic
- Partner station 的 focus_target 注册——属于 player-movement-interaction Epic

---

## QA Test Cases

- **AC-1**: feature_ready init sequence correct
- **AC-2**: LANDED → cat unfrozen
- **AC-3**: Return to hub + naming eligible → prompt fired
- **AC-4**: New game → on_partner_joined called once
- **AC-5**: Load game → on_partner_joined NOT called
- **AC-6**: reveal_rumor parameters correct
- **AC-7**: reveal_rumor failure → graceful degradation
- **AC-8**: IntelManager null → no crash
- **AC-10/11**: query_partner_name named/unnamed
- **AC-12**: query_nest_state FULL
- **AC-13/14**: get_sniffable_items with/without #5
- **AC-15/16**: sync_with_hub_state correct after race

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/partner-relationships/hub_intel_integration_test.gd` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (cat state machine), Story 002 (scout_sniff), Story 003 (naming/nest), airship-hub Epic (Hub events, query interface), intel-knowledge Epic (reveal_rumor, report_observation_event, on_partner_joined), platform-session-shell Epic (BootstrapSequencer)
- Unlocks: Story 006 (edge cases involving cross-system state)
