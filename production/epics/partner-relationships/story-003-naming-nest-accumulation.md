# Story 003: Naming System & Nest Accumulation

> **Epic**: Partner & Relationships
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/partner-relationships.md`
**Requirement**: `TR-partner-003`

**ADR Governing Implementation**: ADR-0015 (§5b F.2 命名资格判定, §5d 小窝物件累积, §5e 命名处理)
**ADR Decision Summary**: 伙伴系统的两套持久化状态机——命名和小窝——构成"它会记得你"的核心证据。命名 3 态状态机（PENDING → PROMPTED → COMPLETED）：F.2 _is_naming_eligible() 要求 sniff_success_occurred=true + naming_state=PENDING + naming_skip_count < 3；首次成功嗅辨后下次归港触发命名 UI；valid submit 一次性完成、不可改名；skip 累积到 3 次后静默锁定默认名"那只猫"；空字符串/空白拒绝（不计入 skip）；名字长度 1-8 字符。小窝痕迹 4 阶段状态机（EMPTY → FIRST → ACCUMULATING → FULL）：每次成功嗅辨产出 nest_token=true → 按固定索引 [0,1,2,3] 顺序累积物件；超过 NEST_CAPACITY=4 静默跳过；所有阶段单向前进、不可逆——无删除/清理/重置操作。两套机均持久化至 progress.partner_skycat snapshot。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 命名一次性——naming_done=true 后拒绝所有后续命名请求；小窝不可逆——nest_items 只追加、不删除；默认名锁定在 3 次跳过发生的瞬间——不延迟到下次归港
- Forbidden: 命名发生在 sniff_success_occurred=false 时——R3 猫先证明自己；nest_items 元素被替换/删除/重排；任何 rename 函数或"清理小窝"操作
- Guardrail: 名字长度 >8 时安全截断——主验证在 UI 层

---

## Acceptance Criteria

### F.2 Naming Eligibility

- [ ] **AC-1**: GIVEN sniff_success_occurred=true + naming_state=PENDING + naming_skip_count=0 + player_returned_to_hub 触发，WHEN _is_naming_eligible()，THEN 返回 true
- [ ] **AC-2**: GIVEN sniff_success_occurred=false（所有条件相同），WHEN _is_naming_eligible()，THEN 返回 false。命名永不被触发——猫未证明自己
- [ ] **AC-3**: GIVEN naming_state=PROMPTED，WHEN _is_naming_eligible()，THEN 返回 false。不重复提示
- [ ] **AC-4**: GIVEN naming_state=COMPLETED，WHEN _is_naming_eligible()，THEN 返回 false。终态
- [ ] **AC-5**: GIVEN naming_skip_count=3，WHEN _is_naming_eligible()，THEN 返回 false。窗口已关闭

### Naming State Machine

- [ ] **AC-6**: GIVEN 新游戏 + sniff_success_occurred=false，WHEN 初始化，THEN naming_state=PENDING + name="" + naming_done=false + naming_skip_count=0
- [ ] **AC-7**: GIVEN 首次成功嗅辨后 + 下次 player_returned_to_hub，WHEN 触发，THEN naming_state→PROMPTED + naming_prompt_triggered 信号发射
- [ ] **AC-8**: GIVEN naming_state=PROMPTED + 玩家提交有效名字"小云"，WHEN submit_partner_name("小云")，THEN name="小云" + naming_done=true + naming_state→COMPLETED + naming_completed 信号发射。命名后不再触发
- [ ] **AC-9**: GIVEN naming_state=COMPLETED + 玩家尝试再次 submit_partner_name("新名")，WHEN 调用，THEN 拒绝——返回 {accepted: false, error: "naming_completed"}。不可改名
- [ ] **AC-10**: GIVEN naming_state=PROMPTED + 玩家提交 "" 或 "   "，WHEN submit_partner_name()，THEN 拒绝 {accepted: false, error: "name_empty"}。naming_skip_count 不增加。naming_state 保持 PROMPTED

### Naming Skip & Default Name

- [ ] **AC-11**: GIVEN naming_state=PROMPTED + 玩家点击"稍后"，WHEN skip_naming()，THEN naming_skip_count += 1 + naming_state→PENDING。下次归港再提示
- [ ] **AC-12**: GIVEN naming_skip_count=2 + 玩家再次 skip，WHEN skip_naming()，THEN naming_skip_count=3 + name="那只猫" + naming_done=true + naming_state→COMPLETED。静默锁定——无通知弹窗
- [ ] **AC-13**: GIVEN 默认名已锁定，WHEN 后续 player_returned_to_hub 触发，THEN naming UI 永不打开。COMPLETED 是终态

### Name Validation

- [ ] **AC-14**: GIVEN 玩家提交 12 字符的名字"超长的猫咪名字测试一下"，WHEN submit_partner_name()，THEN 安全截断为 8 字符。主验证在 UI 层——这里是安全网
- [ ] **AC-15**: GIVEN 名字已设置 + 存档→读档，WHEN 恢复，THEN 名字不变。无 rename 代码路径

### Nest Accumulation

- [ ] **AC-16**: GIVEN nest_state=EMPTY + nest_items=[] + 首次成功嗅辨，WHEN _accumulate_nest_item()，THEN nest_items=[0] + nest_state=FIRST。第 0 件物件：旧船帆碎布
- [ ] **AC-17**: GIVEN nest_items=[0] + 第 2 次嗅辨，WHEN _accumulate_nest_item()，THEN nest_items=[0,1] + nest_state=ACCUMULATING。第 1 件：锈蚀的测风链环
- [ ] **AC-18**: GIVEN nest_items=[0,1] + 第 3 次嗅辨，WHEN 累积，THEN nest_items=[0,1,2] + nest_state=ACCUMULATING。第 2 件：玩家绳头
- [ ] **AC-19**: GIVEN nest_items=[0,1,2] + 第 4 次嗅辨，WHEN 累积，THEN nest_items=[0,1,2,3] + nest_state=FULL。第 3 件：空港徽章残片
- [ ] **AC-20**: GIVEN nest_state=FULL + nest_items.size()=4 + 第 5 次嗅辨，WHEN _accumulate_nest_item()，THEN nest_items 保持 [0,1,2,3]。静默跳过——不追加、不报错

### Nest Invariants

- [ ] **AC-21**: GIVEN nest_items 已累积 N 件 (N>0)，WHEN 任何操作后检查，THEN nest_items 前 N 件永远匹配静态清单 [0..N-1]。顺序固定
- [ ] **AC-22**: GIVEN 任意操作（重复嗅辨、快照往返、Hub 状态转换），WHEN 检查，THEN nest_items.size() 永不低于之前的值。单调不可逆
- [ ] **AC-23**: GIVEN 代码库搜索，WHEN 查找，THEN 不存在 nest_items 的删除/清空/重排操作。G.2 不可逆硬约束

---

## Implementation Notes

### Naming Eligibility

```gdscript
func _is_naming_eligible() -> bool:
    var p := partners[MVP_PARTNER_ID]
    if p["naming_state"] != NAMING_PENDING:
        return false
    if not p["sniff_success_occurred"]:
        return false
    if p["naming_skip_count"] >= NAMING_SKIP_MAX:
        return false
    return true
```

### Name Submission

```gdscript
func submit_partner_name(submitted_name: String) -> Dictionary:
    var p := partners[MVP_PARTNER_ID]
    if p["naming_done"]:
        return {"accepted": false, "error": &"naming_completed"}

    var trimmed := submitted_name.strip_edges()
    if trimmed.is_empty():
        return {"accepted": false, "error": &"name_empty"}

    if trimmed.length() > PARTNER_NAME_LEN_MAX:
        trimmed = trimmed.substr(0, PARTNER_NAME_LEN_MAX)

    p["name"] = trimmed
    p["naming_done"] = true
    p["naming_state"] = NAMING_COMPLETED
    naming_completed.emit(trimmed)
    _trigger_snapshot()
    return {"accepted": true, "error": &""}
```

### Name Skip

```gdscript
func skip_naming() -> void:
    var p := partners[MVP_PARTNER_ID]
    p["naming_skip_count"] += 1

    if p["naming_skip_count"] >= NAMING_SKIP_MAX:
        p["name"] = "那只猫"
        p["naming_done"] = true
        p["naming_state"] = NAMING_COMPLETED
        naming_completed.emit("那只猫")
    else:
        p["naming_state"] = NAMING_PENDING

    _trigger_snapshot()
```

### Nest Accumulation

```gdscript
const NEST_ITEMS: Array = ["旧船帆碎布", "锈蚀的测风链环", "玩家绳头", "空港徽章残片"]

func _accumulate_nest_item() -> void:
    var p := partners[MVP_PARTNER_ID]
    var size: int = p["nest_items"].size()
    if size >= NEST_CAPACITY:
        return  # E.3.b

    p["nest_items"].append(size)
    var new_state: int = NEST_EMPTY
    match p["nest_items"].size():
        0: new_state = NEST_EMPTY
        1: new_state = NEST_FIRST
        2, 3: new_state = NEST_ACCUMULATING
        4: new_state = NEST_FULL

    var old := p["nest_state"]
    if new_state != old:
        p["nest_state"] = new_state
        nest_state_changed.emit(old, new_state)
```

---

## Out of Scope

- player_returned_to_hub 事件触发——属于 Story 004 (Hub 集成)
- naming_prompt_triggered / naming_completed 信号的 UI 消费——属于 #16 UIManager
- 命名 UI 文本框和验证——属于 #16 UIManager
- nest_state_changed 信号的 Hub 痕迹锚点渲染——属于 Hub #7
- _trigger_snapshot() 实现——属于 Story 005

---

## QA Test Cases

- **AC-1-5**: F.2 eligibility all 5 conditions
- **AC-6**: New game defaults
- **AC-7/8**: naming_state transitions
- **AC-9**: Completed → reject rename
- **AC-10**: Empty name → reject without skip
- **AC-11/12**: Skip 3 times → default name locked
- **AC-13**: COMPLETED → UI never reopens
- **AC-14**: Long name truncation
- **AC-16-20**: Nest 4-stage progression + cap
- **AC-21**: Fixed item order preserved
- **AC-22**: Monotonicity across all operations
- **AC-23**: No delete/clear/reset functions

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/partner-relationships/naming_nest_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (partner Dictionary), Story 002 (scout_sniff → _accumulate_nest_item trigger), platform-session-shell Epic (Phase 5 init)
- Unlocks: Story 004 (naming during hub events), Story 005 (persistence of naming/nest state)
