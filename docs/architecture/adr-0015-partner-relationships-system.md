# ADR-0015: 伙伴功能与关系系统 — PartnerManager Autoload #15

## Status
Accepted

## Date
2026-05-08

## Summary
PartnerManager 作为 Autoload #15，管理 MVP 唯一伙伴 `partner.sky-cat`（航海猫——老飞艇船员猫族群的最后一只）。系统维护三层状态机：猫的 6 态运行时状态机（sleeping_on_intel_station / idle_living_quarters / following_player_to_bench / bench_adjacent / sniffing / in_nest）、命名 3 态状态机（pending → prompted → completed）、小窝痕迹 4 阶段状态机（empty → first → accumulating → full）。核心交互循环：玩家将带有 `cat_sniff_signature` 字段的物品递给猫嗅辨 → 猫消费物品的静态签名字段 → 通过 F.1 confidence_clamp 截断置信度（硬上限 66，永不达权威）→ 调用 IntelManager (#6) 的 reveal_rumor() 写入航图传闻 + report_observation_event() 记录观测规律 → 首次成功嗅辨触发小窝物件累积 + 下次归港触发命名。猫的存在性契约（R2）不可违反——query_partner_present() 在任何 Hub 状态下恒返回 true，猫不消失不死亡。系统有 6 条硬禁止（R15）：无好感度数值、无礼物菜单、无事件树、无定时器奖励、无第二只伙伴、无招募/解雇。所有状态以 Dictionary[StringName, Variant] 存储，通过 ADR-0003 Canonical JSON 快照包持久化为 `progress.partner_skycat`。嗅辨品签名 `cat_sniff_signature` 由 Content Registry (#1) 拥有——本系统只读不写。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Feature — World/Companion |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构、状态机、信号、查询接口；无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/partner-relationships.md`, `docs/architecture/architecture.md`, `design/gdd/player-knowledge-intel.md`, `design/gdd/airship-hub.md`, `design/gdd/resources-goods-capacity.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | scout_sniff 6 步算法正确性；F.1 置信度截断（confidence ≤ 66 恒成立）；F.2 命名资格判定；命名状态机 3 次跳过后锁定默认名；小窝单向不可逆累积；sniff state gate 防并发嗅辨；repair_completed→sniff 无耦合验证；7 字段 snapshot 往返序列化；6 条 R15 硬禁止无违反；所有 AC 组覆盖 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #15 启动顺序, Phase 5 feature_ready)；ADR-0002 (Signal 通信协议 — 消费 Hub 事件、向 #6 写入)；ADR-0003 (快照包持久化 — progress.partner_skycat)；ADR-0004 (InteractionHandler @abstract — partner_station 焦点注册与 use_requested 分发)；ADR-0005 (ResourcesManager — 物品 ID 查询 cat_sniff_signature)；ADR-0007 (IntelManager — reveal_rumor / report_observation_event / on_partner_joined)；ADR-0008 (Hub — hub_state_changed / player_returned_to_hub / player_entered_zone / query_nest_state)；ADR-0014 (Settlement — NPC 活跃度可能影响伙伴叙事，post-MVP) |
| **Enables** | ADR-0016 (Feedback — 猫动画/反应视觉反馈、小窝痕迹渲染)；ADR-0017 (Onboarding — 首次嗅辨可能作为新手引导触发器) |
| **Blocks** | N/A — Partner 为 Feature 层终端系统，消费上游 Hub 事件但不产出被其他系统依赖的新信号（嗅辨结果通过 #6 间接影响航图 #9） |
| **Ordering Note** | 应在 ADR-0007 (Intel), ADR-0008 (Hub), ADR-0005 (Resources) 之后 Accepted — 核心交互依赖 reveal_rumor() 写入、Hub 事件消费、物品签名查询 |

## Context

### Problem Statement

《云海织航》的 Pillar 5（少量深关系胜过大量收集）在 MVP 中需要一个可见证据——伙伴系统就是那个证据。它是一只猫，没有好感度数值，没有礼物菜单，没有事件树——但它会记得你给它取的名字，会在生活舱角落累积出一个小窝，会把每次嗅辨的结果变成航图上一条新传闻。没有这个系统，飞艇只是一个工具箱，Pillar 5 在 MVP 中无可见证据，CD 关于"令人难忘的身份节拍 + 持久关系记忆"的硬约束无法兑现。

GDD #15 定义了：唯一伙伴 `partner.sky-cat`（航海猫）、R2 存在性契约（猫永远在飞艇上）、R3 命名时刻（首次成功嗅辨后归港触发）、R5/R6 scout_sniff 6 步算法、R7 嗅辨反应符号集（5 种动画映射）、R8 置信度硬上限 66、R11 小窝物件 4 件累积、R12 Idle 行为契约、R13 归港行为、R15 6 条硬禁止。猫的 6 态运行时状态机、3 态命名状态机、4 阶段小窝痕迹状态机构成系统核心。但 Partner 的 Autoload 定位、信号/查询接口、与 #6/#7/#5 的 API 合同、R15 硬禁止的系统层验证、F.1 截断的不可绕过性未在 ADR 中形式化。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑，无引擎 API 风险
- **ADR-0002 信号协议**: typed params, sync emit, max depth 2——本系统主要消费 Hub 事件（不产出新信号链）
- **ADR-0003 持久化**: `progress.partner_skycat` snapshot package——7 字段：name, naming_done, naming_skip_count, sniff_success_occurred, nest_state, nest_items[], sniffed_items[]
- **ADR-0007 IntelManager**: reveal_rumor(location_id, source_tag, hazard_tags, confidence) + report_observation_event(pattern_id, "partner_sniff_success") + on_partner_joined("partner.sky-cat")——新游戏 bootstrap 调用 1 次
- **ADR-0008 Hub**: hub_state_changed(new_state), player_returned_to_hub(), player_entered_zone(zone_id)——猫状态机的所有非嗅辨驱动事件
- **ADR-0005 ResourcesManager**: 物品 item_id 查询——消费 cat_sniff_signature 静态字段但不修改
- **ADR-0001 启动顺序**: PartnerManager 在 Phase 5 (feature_ready) 初始化，必须在 #6/#7/#5 之后
- **R15 硬禁止**: 6 条系统层不可违反的约束——需在数据模型中可验证
- **MVP 边界**: 唯一伙伴 `partner.sky-cat`，1 个嗅辨动词，4 件小窝物件上限，命名一次不可改，MVP_CONFIDENCE_MAX=66

## Decision

### 1. PartnerManager 作为 Autoload #15

PartnerManager 在 Phase 5 (feature_ready) 中初始化，在 #6 (Intel), #7 (Hub), #5 (Resources) 之后就绪。`_ready()` 仅执行字段声明和常量定义；初始化序列在收到 `feature_ready` 后执行：加载快照 → 派生初始猫状态 → 订阅 Hub 事件 → 显式调用 sync_with_hub_state() → 新游戏时排队 on_partner_joined()。

```
Autoload 顺序 (Phase 5):
  #10 Navigation          ──┐
  #11 Exploration         ──┤
  #12 Combat              ──┤ 并行接收 feature_ready
  #13 WorldRepair         ──┤
  #14 Settlement          ──┤
  #15 Partner             ──┤
  #16 UI                  ──┘
```

### 2. Dictionary 后端存储

```gdscript
# === PartnerManager 状态结构 ===

# 伙伴元数据: StringName → PartnerState
# PartnerState = {
#   name: String,               # 玩家命名的名字（空字符串 = 未命名）
#   naming_done: bool,          # 命名是否已完成（终态锁）
#   naming_skip_count: int,     # 已跳过次数 (0-3)
#   naming_state: int,          # 0=PENDING, 1=PROMPTED, 2=COMPLETED
#   sniff_success_occurred: bool, # 生命周期内是否发生过至少一次成功嗅辨
#   sniffed_items: Array,       # Array[StringName] — 已嗅辨物品 ID 集合
#   nest_state: int,            # 0=EMPTY, 1=FIRST, 2=ACCUMULATING, 3=FULL
#   nest_items: Array,          # Array[int] — 已累积的物件索引 (0-3)
# }
var partners: Dictionary = {}  # Dictionary[StringName, Dictionary]

# 猫运行时状态（瞬态——不持久化）
# cat_state: int              # 0=SLEEPING_ON_INTEL, 1=IDLE_LIVING, 2=FOLLOWING, 3=BENCH, 4=SNIFFING, 5=IN_NEST
var cat_state: int = CAT_SLEEPING_ON_INTEL_STATION
var _cat_state_cooldown: float = 0.0
var _sniff_lockout_remaining: float = 0.0
```

**常量定义：**

```gdscript
# 猫运行时状态枚举
const CAT_SLEEPING_ON_INTEL_STATION: int = 0
const CAT_IDLE_LIVING_QUARTERS: int = 1
const CAT_FOLLOWING_PLAYER_TO_BENCH: int = 2
const CAT_BENCH_ADJACENT: int = 3
const CAT_SNIFFING: int = 4
const CAT_IN_NEST: int = 5

# 命名状态枚举
const NAMING_PENDING: int = 0
const NAMING_PROMPTED: int = 1
const NAMING_COMPLETED: int = 2

# 小窝状态枚举
const NEST_EMPTY: int = 0
const NEST_FIRST: int = 1
const NEST_ACCUMULATING: int = 2
const NEST_FULL: int = 3

# 硬约束常量
const MVP_CONFIDENCE_MAX: int = 66
const NAMING_SKIP_MAX: int = 3
const NEST_CAPACITY: int = 4
const PARTNER_NAME_LEN_MAX: int = 8
const PARTNER_NAME_LEN_MIN: int = 1
const T_CAT_STATE_COOLDOWN: float = 0.5
const T_SNIFF_LOCKOUT: float = 2.5
const T_NEST_SETTLE: float = 20.0

# MVP 唯一伙伴 ID
const MVP_PARTNER_ID: StringName = &"partner.sky-cat"
```

### 3. 查询接口（本系统提供——被 Hub/UI 消费）

```gdscript
# === 伙伴存在性（R2 硬契约） ===
func query_partner_present() -> bool:
    return true  # 恒 true — 猫永远在飞艇上

func query_partner_name() -> String:
    var p := partners[MVP_PARTNER_ID]
    if p["naming_done"]:
        return p["name"]
    return ""  # 未命名——UI 层回退到 "那只灰白猫"

func query_nest_state() -> int:
    return partners[MVP_PARTNER_ID]["nest_state"]

func query_nest_items() -> Array:
    return partners[MVP_PARTNER_ID]["nest_items"].duplicate()

# === 嗅辨面板过滤 ===
func get_sniffable_items() -> Array:
    # 返回玩家背包中具有 cat_sniff_signature 字段的物品列表
    var inventory: Array = ResourcesManager.get_inventory_items()
    var candidates: Array = []
    for item_id in inventory:
        var sig := _get_sniff_signature(item_id)
        if not sig.is_empty():
            candidates.append(item_id)
    return candidates
```

### 4. 信号接口

PartnerManager 不发射自有业务信号——嗅辨结果通过 #6 的 reveal_rumor() 写入（属于 #6 的信号域），猫状态变更通过 Hub 的 query 接口暴露。但声明以下信号供 UI/FX 消费：

```gdscript
# === 猫状态变更（供 Feedback #17 动画切换） ===
signal cat_state_changed(old_state: int, new_state: int)

# === 命名事件（供 UI #16） ===
signal naming_prompt_triggered()
signal naming_completed(cat_name: String)

# === 小窝变更（供 Hub #7 痕迹锚点） ===
signal nest_state_changed(old_state: int, new_state: int)

# === 嗅辨事件（供 Feedback #17 动画播放） ===
signal sniff_reaction_triggered(reaction_id: int, item_id: StringName)
```

### 5. 核心算法

#### 5a. F.1 置信度截断

```gdscript
func _clamp_confidence(raw_confidence: int) -> int:
    return mini(raw_confidence, MVP_CONFIDENCE_MAX)
    # 永不达 67（#6 的权威门槛）——R8 硬约束
    # min() 不可跳过——即使原始置信度为 100，汇报值仍为 66
```

#### 5b. F.2 命名资格判定

```gdscript
func _is_naming_eligible() -> bool:
    var p := partners[MVP_PARTNER_ID]
    if p["naming_state"] != NAMING_PENDING:
        return false
    if not p["sniff_success_occurred"]:
        return false  # R3: 猫先证明自己
    if p["naming_skip_count"] >= NAMING_SKIP_MAX:
        return false
    # player_returned_to_hub 由调用上下文保证——不在本函数内检查
    return true
```

#### 5c. R6 scout_sniff() 6 步算法

```gdscript
func scout_sniff(item_id: StringName) -> Dictionary:
    # 返回 {success: bool, reaction_id: int, error: StringName}
    
    # Step 0: 状态门控——猫必须在可嗅辨状态
    if cat_state == CAT_SNIFFING:
        return {"success": false, "reaction_id": -1, "error": &"cat_busy"}
    
    var p := partners[MVP_PARTNER_ID]
    
    # Step 1: 检查已嗅辨集合
    if item_id in p["sniffed_items"]:
        _play_reaction(REACTION_ALREADY_SMELLED)  # 耳朵放松下压
        return {"success": false, "reaction_id": REACTION_ALREADY_SMELLED, "error": &"already_sniffed"}
    
    # Step 2: 读取物品嗅辨签名
    var sig := _get_sniff_signature(item_id)
    if sig.is_empty():
        _play_reaction(REACTION_CONFUSED)  # 困惑动画
        return {"success": false, "reaction_id": REACTION_CONFUSED, "error": &"no_signature"}
    
    var reveal_target: StringName = sig.get("reveal_target", &"")
    if reveal_target == &"":
        _play_reaction(REACTION_CONFUSED)
        return {"success": false, "reaction_id": REACTION_CONFUSED, "error": &"empty_reveal_target"}
    
    # Step 3: 截断置信度 + 调用 #6
    var raw_confidence: int = sig.get("confidence", 0)
    var confidence: int = _clamp_confidence(raw_confidence)
    var hazard_hint: StringName = sig.get("hazard_hint", &"")
    
    # 调用 #6 reveal_rumor（E.5.a: 失败不重试）
    var rumor_ok := IntelManager.reveal_rumor(reveal_target, &"partner.sky-cat", [hazard_hint], confidence)
    if not rumor_ok:
        push_warning("Partner: reveal_rumor() failed for item '%s' → target '%s'" % [item_id, reveal_target])
    
    # Step 4: 调用 #6 report_observation_event
    var pattern_id: StringName = sig.get("pattern_id", &"")
    if pattern_id != &"":
        IntelManager.report_observation_event(pattern_id, &"partner_sniff_success")
    
    # Step 5: 加入已嗅辨集合 + 标记 sniff_success_occurred
    p["sniffed_items"].append(item_id)
    if not p["sniff_success_occurred"]:
        p["sniff_success_occurred"] = true
    
    # Step 6: 小窝物件累积（R11）
    var nest_token := true  # 首次嗅辨成功 → 产出物件
    _accumulate_nest_item()
    
    # Step 7: 选择反应动画（R7 符号集）
    var reaction_id: int
    if confidence >= 50:
        reaction_id = REACTION_CIRCLES_TWICE   # 强信号 → 绕圈两圈
    else:
        reaction_id = REACTION_RUBS_FACE       # 弱信号 → 蹭脸
    
    # Step 8: 播放动画 + 进入 sniffing 状态
    cat_state = CAT_SNIFFING
    _sniff_lockout_remaining = T_SNIFF_LOCKOUT
    sniff_reaction_triggered.emit(reaction_id, item_id)
    
    _trigger_snapshot()
    return {"success": true, "reaction_id": reaction_id, "error": &""}
```

#### 5d. 小窝物件累积

```gdscript
# 静态物件清单（索引 0-3，顺序固定）
const NEST_ITEMS: Array = [
    "旧船帆碎布",       # 索引 0 — 初始铺底
    "锈蚀的测风链环",   # 索引 1 — 猫从工程舱拖来
    "玩家绳头",         # 索引 2 — 玩家无意间落下
    "空港徽章残片",     # 索引 3 — 来源不明，游戏不解释
]

func _accumulate_nest_item() -> void:
    var p := partners[MVP_PARTNER_ID]
    var size: int = p["nest_items"].size()
    if size >= NEST_CAPACITY:
        return  # E.3.b: 满额后静默跳过
    
    p["nest_items"].append(size)  # 按索引追加——size 即下一件物件的索引
    
    # 更新 nest_state
    var new_state: int
    match p["nest_items"].size():
        0: new_state = NEST_EMPTY
        1: new_state = NEST_FIRST
        2, 3: new_state = NEST_ACCUMULATING
        4: new_state = NEST_FULL
    var old_state: int = p["nest_state"]
    if new_state != old_state:
        p["nest_state"] = new_state
        nest_state_changed.emit(old_state, new_state)
```

#### 5e. 命名处理

```gdscript
func on_player_returned_to_hub() -> void:
    # R3: 首次成功嗅辨后归港 → 触发命名
    var p := partners[MVP_PARTNER_ID]
    
    if p["naming_state"] == NAMING_PROMPTED:
        # 仍在上次提示中（存档后读档恢复）——重新触发
        naming_prompt_triggered.emit()
        return
    
    if not _is_naming_eligible():
        return
    
    p["naming_state"] = NAMING_PROMPTED
    naming_prompt_triggered.emit()

func submit_partner_name(submitted_name: String) -> Dictionary:
    # 返回 {accepted: bool, error: String}
    var trimmed := submitted_name.strip_edges()
    if trimmed.is_empty():
        return {"accepted": false, "error": &"name_empty"}
    if trimmed.length() > PARTNER_NAME_LEN_MAX:
        trimmed = trimmed.substr(0, PARTNER_NAME_LEN_MAX)  # 安全网截断
    
    var p := partners[MVP_PARTNER_ID]
    p["name"] = trimmed
    p["naming_done"] = true
    p["naming_state"] = NAMING_COMPLETED
    naming_completed.emit(trimmed)
    _trigger_snapshot()
    return {"accepted": true, "error": &""}

func skip_naming() -> void:
    var p := partners[MVP_PARTNER_ID]
    p["naming_skip_count"] += 1
    
    if p["naming_skip_count"] >= NAMING_SKIP_MAX:
        # E.1.b: 3 次跳过后锁定默认名
        p["name"] = "那只猫"
        p["naming_done"] = true
        p["naming_state"] = NAMING_COMPLETED
        naming_completed.emit("那只猫")
    else:
        p["naming_state"] = NAMING_PENDING  # 下次归港再提示
    
    _trigger_snapshot()
```

#### 5f. 猫运行时状态机（Hub 事件驱动）

```gdscript
func on_hub_state_changed(new_state: int) -> void:
    match new_state:
        HUB_LANDED:
            pass  # 正常运转
        HUB_DEPARTURE_LOCKED:
            _freeze_cat_state()  # E.2.i
        HUB_IN_TRANSIT:
            _simplify_cat()      # 不渲染，逻辑态保持 idle
        HUB_ARRIVAL:
            _force_cat_state(CAT_IDLE_LIVING_QUARTERS)  # R13: 不在入口

func on_player_entered_zone(zone_id: StringName) -> void:
    if _cat_state_cooldown > 0.0:
        return  # E.4.b: 防抖
    
    match zone_id:
        &"living_quarters":
            if cat_state == CAT_SLEEPING_ON_INTEL_STATION or cat_state == CAT_IN_NEST:
                _transition_cat_state(CAT_IDLE_LIVING_QUARTERS)
        &"workbench":
            if cat_state == CAT_IDLE_LIVING_QUARTERS:
                _transition_cat_state(CAT_FOLLOWING_PLAYER_TO_BENCH)
```

### 6. ADR-0003 序列化

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

func _deserialize_partner(snapshot: Dictionary) -> void:
    var p := partners[MVP_PARTNER_ID]
    p["name"] = snapshot.get("name", "")
    p["naming_done"] = snapshot.get("naming_done", false)
    p["naming_skip_count"] = snapshot.get("naming_skip_count", 0)
    p["sniff_success_occurred"] = snapshot.get("sniff_success_occurred", false)
    p["nest_state"] = snapshot.get("nest_state", NEST_EMPTY)
    p["nest_items"] = snapshot.get("nest_items", [])
    p["sniffed_items"] = snapshot.get("sniffed_items", [])
    
    # 派生 naming_state
    if p["naming_done"]:
        p["naming_state"] = NAMING_COMPLETED
    elif p["naming_skip_count"] > 0:
        p["naming_state"] = NAMING_PENDING  # 之前跳过但未完成
    else:
        p["naming_state"] = NAMING_PENDING
    
    # 一致性修正
    if p["sniff_success_occurred"] and p["sniffed_items"].size() == 0:
        push_warning("Partner: sniff_success_occurred=true but sniffed_items is empty — auto-correcting")
        p["sniff_success_occurred"] = false
    
    # 派生瞬态字段（E.4.a）
    cat_state = CAT_SLEEPING_ON_INTEL_STATION
    _cat_state_cooldown = 0.0
    _sniff_lockout_remaining = 0.0
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    PartnerManager (Autoload #15)                           │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │              STATE STORAGE (Dictionary)                        │       │
│  │  partners: Dict[StringName, PartnerState]                      │       │
│  │    partner.sky-cat: {name, naming_done, naming_skip_count,     │       │
│  │      naming_state, sniff_success_occurred, sniffed_items[],    │       │
│  │      nest_state, nest_items[]}                                 │       │
│  │  cat_state: int (瞬态——不持久化)                               │       │
│  └──────────────────────────────────────────────────────────────┘       │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          UPSTREAM (consumes)                                       │   │
│  │  Intel (#6)   ←── reveal_rumor() + report_observation_event()     │   │
│  │  Hub (#7)     ←── hub_state_changed, player_returned_to_hub,      │   │
│  │                    player_entered_zone, partner_station            │   │
│  │  Resources(#5)←── get_inventory_items() — 物品 cat_sniff_signature│   │
│  │  Registry(#1) ←── query_entity() — cat_sniff_signature 静态字段   │   │
│  │  Interaction(#4)←── use_requested(partner_station) → 嗅辨面板     │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          DOWNSTREAM (provides)                                      │   │
│  │  Intel (#6)  → on_partner_joined("partner.sky-cat") — bootstrap    │   │
│  │  Hub (#7)    → query_partner_present(), query_partner_name(),       │   │
│  │                  query_nest_state(), query_nest_items()             │   │
│  │  UI (#16)    → naming_prompt_triggered, naming_completed,           │   │
│  │                  get_sniffable_items()                              │   │
│  │  Feedback(#17)→ cat_state_changed, sniff_reaction_triggered,       │   │
│  │                  nest_state_changed                                 │   │
│  │  Persist(#3) → progress.partner_skycat snapshot                     │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │          3-TIER STATE + R15 HARD PROHIBITIONS                   │       │
│  │  Cat 6-state FSM: sleeping → idle → following → bench →        │       │
│  │                   sniffing → in_nest                            │       │
│  │  Naming 3-stage: PENDING → PROMPTED → COMPLETED                │       │
│  │  Nest 4-stage: EMPTY → FIRST → ACCUMULATING → FULL (irrev)     │       │
│  │  R15 Guards: 无好感度/无礼物/无事件树/无定时器/无第二只/无招募  │       │
│  └──────────────────────────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────────────────────────┘
```

## Alternatives Considered

### Alternative A: 伙伴系统由 Hub #7 直接管理

- **Description**: 猫的状态和行为作为 Hub 场景的一部分，不设独立 Autoload
- **Pros**: 减少 Autoload 数量；猫的视觉锚点与场景节点在同一处管理
- **Cons**: 状态与场景生命周期绑定——in_transit 时场景卸载导致猫状态丢失；跨会话持久化需要 Hub 快照包含伙伴数据（违反数据归属边界）；命名/小窝的累积逻辑与 Hub 的整备/出航逻辑耦合
- **Rejection Reason**: GDD 定义伙伴为独立系统——猫的状态需要在 in_transit 期间保持逻辑存在（R2 存在性契约），需要独立的 persistence domain (progress.partner_skycat)，需要与 #6 的知识系统有独立 API 合同。Hub 拥有空间和交互点，伙伴拥有数据和逻辑——分离符合 ADR-0001

### Alternative B: 好感度数值 + 事件树驱动（传统社交系统）

- **Description**: 猫拥有 affection 值、事件触发条件、对话分支、好感等级
- **Pros**: 玩家行为有明确的反馈梯度；熟悉的社交系统范式
- **Cons**: 违反 CD 方向（R15 硬禁止列表直接来自 CD）；数值化关系与"少量深关系"的 Pillar 5 矛盾——好感度鼓励"刷"而非"见证"；事件树引入状态组合爆炸
- **Rejection Reason**: Pillar 5 要求的是"持久关系记忆"而非"可量化的好感进度条"。猫的关系深度来自命名的一次性、小窝的不可逆性、以及嗅辨结果的留白——不是来自数值累积

### Alternative C: 多只伙伴（猫 + 人类伙伴）

- **Description**: MVP 同时包含 `partner.sky-cat` 和 3 个人类伙伴（老水手、灯塔看守后裔、制图师）
- **Pros**: 更多内容量；不同伙伴提供不同嗅辨专长
- **Cons**: 人类伙伴的 dialogue tree / event trigger 系统远超 MVP 范围；GDD #6 Part 8 已明确三人退到 Post-MVP；增加命名系统复杂度（多名伙伴需要多次命名）
- **Rejection Reason**: MVP 锁定唯一伙伴 per R15.5 和 CD 方向。单只猫足以锚定 Pillar 5 的核心证据——更多伙伴是 Post-MVP 的内容扩展，不是架构变更

## Consequences

### Positive

- **单一伙伴权威**: PartnerManager 是所有伙伴状态的唯一 owner——不分散在场景或 Hub 中
- **R15 可验证**: 6 条硬禁止可在代码审查中直接验证——无好感度字段、无礼物函数、无事件树结构、无 delta_time 奖励、无工厂模式、无 join/leave API
- **存在性契约简单**: query_partner_present() 恒返回 true——零状态分支，零 bug 面
- **关系记忆持久**: 命名、小窝、已嗅辨集合全部持久化——CD "30 小时后仍记得" 可验证
- **#6 写入单向**: 只写不读——不缓存知识状态，不引入 #15 → #6 回读的数据耦合

### Negative

- **Autoload #15**: 增加了 Phase 5 启动约束和初始化顺序依赖
- **瞬态状态不持久化**: 猫的精确动画状态在存档后丢失——读档后猫在情报台而非读档前的位置。这是有意的设计权衡（E.4.a），但可能被玩家感知为"猫瞬移了"
- **嗅辨签名内容管线依赖**: 若 #1 未配置 cat_sniff_signature 字段，嗅辨面板始终为空——系统在逻辑上正确但无玩家可感知内容。需在内容管线完成前通过 mock 数据测试

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| reveal_rumor() 调用失败导致知识丢失 | Low | Low — 嗅辨本地状态正确，仅传闻未写入 | E.5.a: 不重试，记录 warning。玩家无法区分"猫反应太弱"和"系统错误"——UI 层沉默 |
| 初始化竞态：#7 事件先于 #15 订阅 | Medium | Medium — 猫状态错误 | E.6.c: 订阅后显式调用 sync_with_hub_state()；bootstrap sequencer 确保 #6 就绪后才分发 on_partner_joined |
| 命名 UI 打开期间 departure_locked 触发 | Low | Low — 命名模态阻塞出航 | E.1.g: departure 推迟到命名解决后；命名 UI 在 arrival 序列中先于玩家恢复控制 |
| sniffed_items 集合膨胀（长期存档） | Low | Low — Array 而非 Set | MVP 预期嗅辨物品数 <20——Array 线性查找 <0.1ms。Post-MVP 可迁移为 Set 优化 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| partner-relationships.md | R1: 系统范畴与边界 | §2 Dictionary 存储 + §3 查询接口——拥有 partner.sky-cat 状态，不拥有知识/物品 schema/Hub 空间 |
| partner-relationships.md | R2: 猫的存在性契约 | query_partner_present() 恒返回 true——零状态分支 |
| partner-relationships.md | R3: 命名时刻触发 | F.2 命名资格判定 + on_player_returned_to_hub() 处理 |
| partner-relationships.md | R4: 跳过与默认名 | submit_partner_name() + skip_naming()——3 次跳过后锁"那只猫" |
| partner-relationships.md | R5/R6: 嗅辨动词 + 6 步算法 | §5c scout_sniff() 完整算法——状态门控→已嗅辨检查→签名读取→截断→#6 写入→集合加入→小窝累积→动画 |
| partner-relationships.md | R7: 嗅辨反应符号集 (5 种动画) | sniff_reaction_triggered 信号 + reaction_id 映射 |
| partner-relationships.md | R8: MVP_CONFIDENCE_MAX=66 | F.1 _clamp_confidence() — min() 不可绕过 |
| partner-relationships.md | R9: 嗅辨揭示/不揭示边界 | reveal_target + hazard_hint 透传——猫不产出"负面评估" |
| partner-relationships.md | R10: 物品来源数据契约 | cat_sniff_signature 从 Registry 只读——不动态生成 |
| partner-relationships.md | R11: 小窝物件累积 | _accumulate_nest_item()——固定索引顺序、CAP=4、不可逆 |
| partner-relationships.md | R12: Idle 行为契约 | 猫状态机全部事件驱动——无 _process 奖励 |
| partner-relationships.md | R13: 归港行为 | on_hub_state_changed(ARRIVAL) → force CAT_IDLE_LIVING_QUARTERS |
| partner-relationships.md | R14: "无反应"叙事 | REACTION_CONFUSED 静默——猫走开，不弹提示 |
| partner-relationships.md | R15: 6 条硬禁止 | 数据模型可验证——无 affection 字段/无 gift 函数/无事件树/无 delta_time 奖励/无其他 partner_id/无 recruit/dismiss |
| partner-relationships.md | F.1 confidence_clamp | mini(raw, 66)——6 参数化用例 |
| partner-relationships.md | F.2 naming_prompt_eligibility | _is_naming_eligible() 4 条件判定 |
| partner-relationships.md | E.1(a-g) 命名边缘情况 | submit/skip 守卫 + 3 次跳过锁 + 空字符串拒绝 + 存档恢复 |
| partner-relationships.md | E.2(a-j) 嗅辨边缘情况 | 状态门控 + 已嗅辨去重 + null 签名兜底 + departure 中断 |
| partner-relationships.md | E.3(a-d) 小窝边缘情况 | CAP 守卫 + 终态静默跳过 + 快照精确恢复 |
| partner-relationships.md | E.4(a-d) 状态机边缘情况 | 瞬态不持久化 + cooldown 防抖 + T_nest_settle |
| partner-relationships.md | E.5(a-e) 接口契约边缘情况 | reveal_rumor 失败不重试 + pattern_id 透传 + forward compat |
| partner-relationships.md | E.6(a-d) 存在性与初始化 | query_partner_present 恒 true + sync_with_hub_state + bootstrap sequencer |
| partner-relationships.md | AC-SNIFF-01～08 | scout_sniff 算法覆盖全部 8 条嗅辨 AC |
| partner-relationships.md | AC-NAME-01～06 | submit/skip/is_eligible 覆盖全部 6 条命名 AC |
| partner-relationships.md | AC-NEST-01～05 | _accumulate_nest_item 覆盖全部 5 条小窝 AC |
| partner-relationships.md | AC-CAT-01～06 | 状态机 + Hub 事件处理覆盖全部 6 条猫存在性 AC |
| partner-relationships.md | AC-GUARD-01～06 | R15 数据模型可验证性覆盖全部 6 条硬禁止 AC |
| partner-relationships.md | AC-SAVE-01～05 | ADR-0003 serializer/deserializer 覆盖全部 5 条持久化 AC |
| partner-relationships.md | AC-EDGE-01～08 | 状态门控/cooldown/defensive 守卫覆盖全部 8 条边缘情况 AC |
| partner-relationships.md | AC-PILLAR-01～05 | 命名不可逆/小窝永久/置信度上限/无中断/在场感——全部 5 条 Pillar 验证 AC |

## Performance Implications

- **CPU**: scout_sniff(): O(S) 其中 S=sniffed_items.size() (MVP <20, Array 线性查找 <0.001ms)。F.1 clamp: O(1) min()。F.2 eligibility: O(1) 4 条件判定。猫状态机转换: O(1) 查表。嗅辨面板过滤: O(I) 其中 I=背包物品数 (<50) — <0.01ms
- **Memory**: 1 partner × ~500 bytes（含 sniffed_items Array 和 nest_items Array）。猫状态机瞬态字段 <100 bytes。总计 <1KB
- **Load Time**: 启动时从 Persistence snapshot 恢复——反序列化 <0.5ms
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. 在 project.godot 中注册 PartnerManager 为 Autoload #15
2. 实现 3 层状态机枚举 + Dictionary 状态结构
3. 实现 scout_sniff() 6 步算法 + F.1 置信度截断
4. 实现 F.2 命名资格判定 + submit_partner_name() + skip_naming()
5. 实现 _accumulate_nest_item() 小窝物件累积
6. 实现猫运行时状态机（Hub 事件驱动）+ cooldown 防抖
7. 实现 query_partner_* 查询接口（presence/name/nest_state/nest_items）
8. 实现 get_sniffable_items() 嗅辨面板过滤
9. 实现 ADR-0003 serializer/deserializer + 一致性修正
10. 实现 on_partner_joined() bootstrap 排队 + sync_with_hub_state()
11. 单元测试: scout_sniff 全算法/F.1 截断/F.2 资格判定/命名状态机/小窝累积/R15 字段可验证性/状态门控
12. 集成测试: Hub 事件→猫状态转换/嗅辨→#6 写入/快照往返/初始化竞态/命名 UI 阻塞 departure

## Validation Criteria

- scout_sniff() 对已嗅辨物品返回 {success: false, error: "already_sniffed"}
- scout_sniff() 对 null/empty reveal_target 物品返回 REACTION_CONFUSED——不写入 #6
- F.1: raw 0→0, 30→30, 66→66, 67→66, 90→66, 100→66（6 参数化用例全通过）
- F.2: sniff_success_occurred=false → naming 永远不触发
- 3 次跳过后 name="那只猫", naming_done=true, naming_state=COMPLETED
- nest_items 单调递增——无删除、无倒退、无重排
- query_partner_present() 在所有 Hub 状态下返回 true
- sniffing 状态期间并发 scout_sniff() 调用被拒绝
- 7 字段 snapshot 往返一致——name/naming_done/naming_skip_count/sniff_success_occurred/nest_state/nest_items[]/sniffed_items[]
- 数据模型中无 affection/friendship/bond/relationship_level 字段
- 无 gift/donate/recruit/dismiss 函数
- departure_locked 期间 sniff 交互被拒绝

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — PartnerManager 为 Autoload #15，Phase 5 启动
- **ADR-0002**: Signal 通信协议 — 5 signals typed params, sync emit
- **ADR-0003**: 存档系统 — `progress.partner_skycat` snapshot package
- **ADR-0004**: InteractionHandler — partner_station 焦点注册与 use_requested 分发
- **ADR-0005**: 资源池系统 — 物品 item_id 查询 cat_sniff_signature
- **ADR-0007**: Intel 系统 — reveal_rumor / report_observation_event / on_partner_joined
- **ADR-0008**: Hub 系统 — hub_state_changed / player_returned_to_hub / player_entered_zone / query_nest_state
- **ADR-0014**: Settlement 系统 — NPC 活跃度可能影响伙伴叙事 (post-MVP)
- **GDD #15**: partner-relationships.md — 完整伙伴设计
