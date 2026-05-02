# 伙伴功能与关系

> **Status**: In Review
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-02
> **Implements Pillar**: 少量深关系胜过大量收集 (P5 主); 未知带来温和压力 (P4 辅); 飞艇是家，不只是载具 (P3 辅)
> **MVP Partner**: `partner.sky-cat` — 航海猫，老飞艇船员猫族群的最后一只
> **Cross-GDD Impact**: 修订 #6 玩家知识与情报 Part 8（三个人类伙伴退到 Post-MVP，待 `/consistency-check` 同步）

## Overview

`伙伴功能与关系` 是《云海织航》中将 Pillar 5（少量深关系胜过大量收集）落地为 MVP 可见证据的系统。它在数据层面追踪一只非人类伙伴——`partner.sky-cat`，老飞艇船员猫族群的最后一只航海猫——的存在状态、关系记忆条目、嗅辨任务记录，并通过 `玩家知识与情报` (#6) 暴露的 `reveal_rumor()` 接口将猫的嗅辨产出写入玩家的航图情报；在玩家层面，它是出航前蹲在情报台上睡觉的那只灰白猫，是返航后会蹭你的腿、嗅你带回物品并以耳朵姿态/尾巴方向暗示气味来源的伴侣，是飞艇生活舱角落随时间累积出小窝、它叼回的小纪念品和睡过的旧布料的"它住在这艘船上"的可见证据。

玩家与系统的交互是双重的：**主动**——把带回的物品交给猫嗅辨，把它的反应转化为航图上的一条新传闻（`reveal_rumor()` 写入）；**被动**——猫不会消失，它会自己在飞艇里走动、找暖光、在你工作时蹲在旁边，留下不需要你管它的"在场感"。它没有好感度数值，没有礼物菜单，没有事件树——但它会记得：你给它取的名字会写进存档，第一次成功嗅辨后它的小窝会出现在生活舱并持续累积痕迹，这些痕迹在 MVP 内不可逆——它们证明"这只猫和这艘船一起在生活"。

没有这个系统，飞艇就只是一个工具箱——再精致的整备和归位流也无法证明有谁在这里活着；玩家会在低压规划循环中感到孤独，Pillar 5 在 MVP 中将无可见证据，CD 关于"令人难忘的身份节拍 + 持久关系记忆"的硬约束将无法兑现。

## Player Fantasy

`伙伴功能与关系` 服务的幻想是三层交织的：**归港时有人在**、**群体记忆的最后一块**、**它会记得你**。

**归港时有人在**：每一次航行结束、停泊在 Hub，引擎杂音降下来后，飞艇里就不再只有你一个人。猫从生活舱暖光管旁起身，慢慢踱过来，在你整理货物的工作台边蹲下。你不需要走过去摸它、不需要用任何指令——只是在分拣物品，余光里它在那里。但你知道：如果今天你没回来，没人会在那盏灯下蹲着了。这种"我有人等"的低语感，是把飞艇从交通工具变成"家"的最直接证据，是 P3（飞艇是家）和 P5（深关系）在 MVP 中最朴素的兑现。

**群体记忆的最后一块**：这只猫不是被随机抓来的萌物——它是老飞艇船员猫族群的最后一只。在世界破碎之前，每一艘长途飞艇都至少养着一只这样的猫，它们能嗅出风暴前的气压变化、辨别空港和异域的气息差别，在船员之间睡来睡去。碎裂事件之后，它们随着旧航线一起断了消息，只剩这一只孤本带着族群的本能登上了你的飞艇。当你递给它一片从未知岛屿带回来的金属碎屑，它绕了两圈，耳朵后转，尾巴指向西南舷窗——你不知道它指的是当年它族群飞过的某条老航线，还是它在风里读到的什么别的；游戏不会告诉你答案。但你会带着这个问号继续航行，把它的反应在航图上写成一条新传闻，去验证。它的嗅辨不是冰冷的提示——是一段比你更古老的群体记忆，正在借你的航程被一点点重新激活，这同时兑现 P4（未知带来温和压力——未知被染上了历史的重量，而非纯粹危险）。

**它会记得你**：这只猫没有好感度数值，没有经验值，没有礼物菜单，没有事件树。但它会记得——你给它取的名字写进了存档，下一次开档它仍然叫这个名；第一次成功嗅辨之后，生活舱角落悄悄出现了它的小窝，由它自己叼来的小布片、你某次返航留下的旧绳头、它从工程舱角落拖来的一颗螺帽组成；之后每次出航归来，窝里可能多一样它新挑出来的东西。这些痕迹在 MVP 内不可逆——它们不是你"投喂"出来的，是它自己在这艘船上活下来留下的证据。它记得你，不是因为系统在数你们见面的次数，是因为你们共享了同一艘飞艇、同一段航程、同一片云海。

参考游戏的情感基准：接近《风之旅人》中沉默伙伴的空间性陪伴，和《Stray》里环境对猫的细微反应——你们各自有事要做，但碰巧选择了同一条航线，并且都没有走开。这与《方舟》的驯服征服和《最后生还者》的强情感剧不同——这只猫的关系强度来自日常和留白，不是情节高峰。

## Detailed Design

### Core Rules

**R1 — 系统范畴与边界。** 本系统拥有：(a) MVP 唯一伙伴 `partner.sky-cat` 的运行时状态、(b) 玩家给猫取的名字、(c) 已嗅辨物品集合、(d) 小窝物件清单、(e) 命名状态、(f) 嗅辨成功发生标志 `sniff_success_occurred`（生命周期持久化标志；亦可从 `sniffed_items.size() > 0` 派生，显式存储为双重保险）。本系统不拥有：(a) 玩家知识状态（属于 #6）、(b) 物品 schema（属于 #1）、(c) Hub 空间布局（属于 #7）、(d) 航图渲染（属于 #9）。

**R2 — 猫的存在性契约。** 猫永远在飞艇上。`query_partner_present()` 在任何 Hub 状态下恒返回 `true`。猫不离开、不消失、不死亡，即使飞艇受损或货舱模块被摧毁。这条契约是硬约束——不存在"猫离开"的状态转换路径。猫从新游戏开始就在飞艇上（`on_partner_joined("partner.sky-cat")` 在初始化时调用 1 次），不存在加入或解雇流程。

**R3 — 命名时刻触发。** 命名提示在玩家完成第一次成功嗅辨后下一次 `player_returned_to_hub()` 事件触发，在归港动画完成后、猫蹭腿动画结束前插入。设计意图：猫先"证明了自己"（嗅辨成功，写出第一条传闻），玩家才给它取名——避免命名发生在双方还没有共同航程时。命名一次性、不可改名；名字写入存档 `progress.partner_skycat.name`，所有涉猫 UI/提示只显示这个名字。

**R4 — 跳过与默认名。** 玩家可关闭命名提示或选择"稍后再取"。每次后续 `player_returned_to_hub()` 时再次提示，最多提示 3 次。第 3 次跳过后 `naming_done = true`，`name = "那只猫"`，不再提示。"那只猫"作为默认名是一种克制的情感表达——暗示玩家还没准备好靠近，同时允许叙事在后续保持一致。名字长度约束 1–8 字符，禁止空字符串；不允许后续改名。

**R5 — 斥候动词触发。** 玩家走到伙伴驻点（`hub.interactable.partner_station` in 生活舱，由 #7 拥有，interaction_type: `talk`）的 anchor_radius 内，按 Use 打开"物品递给猫"面板。面板只显示玩家背包中具有 `cat_sniff_signature` 字段的物品（无签名物品不出现在列表中）。

**R6 — 嗅辨判定逻辑（`scout_sniff(item_id)` 算法）。**
1. 检查 `sniffed_items` 集合——若 `item_id` 已存在 → 猫做"已闻过"短动画（耳朵放松下压），返回 `Null`，不调用 `reveal_rumor()`，不产出 `nest_token`
2. 否则读取该物品的 `cat_sniff_signature` 静态字段（由 #1 拥有）：
   - 若签名为 `null` → 猫做"困惑"动画（耳朵前转、左右探头），返回 `Null`（异域物品兜底——通常面板已过滤）
   - 否则 `confidence = min(item.cat_sniff_signature.confidence, 66)`（MVP 硬上限），随后调用 `reveal_rumor(location_id, "partner.sky-cat", [hazard_hint], confidence)`
3. 同时调用 `report_observation_event(pattern_id, "partner_sniff_success")`（pattern_id 来自物品签名）
4. 把 `item_id` 加入 `sniffed_items`
5. 设置 `nest_token = true`（首次嗅辨成功时产出小窝物件），调用 R11 累积逻辑
6. 播放嗅辨反应动画（见 R7）

**R7 — 嗅辨反应的语言。** 全部以猫的动画表达，无诊断字幕，无"它在想…"提示文本。具体符号集：

| 动画 | 含义 | 触发条件 |
|---|---|---|
| 耳朵后转 + 尾巴指向某方位 | 气味陌生但有效线索；尾巴方向 = 气味来源方向 | 嗅辨产出有效 reveal_target |
| 绕圈两圈后离开 | 信号强、置信度高 | confidence ≥ 50 |
| 蹭脸后原地坐下 | 信号弱、存疑 | confidence < 50 |
| 耳朵前转 + 左右探头 | 困惑（异域物品） | 物品 cat_sniff_signature 为 null |
| 耳朵放松下压 | 已闻过（重复物品） | item_id ∈ sniffed_items |

**R8 — 嗅辨置信度硬上限（MVP）。** `partner.sky-cat` 的 `reveal_rumor()` 调用永远不达 `权威`（confidence ≤ 66）——它产出 `rumored` 状态，玩家必须自行验证。物品 schema 可设定任意值，但本系统在调用前强制执行 `min(item.cat_sniff_signature.confidence, 66)`。这条规则保护 P1（玩家必须自行验证）和 P4（保留温和压力）——猫的知识是古老的直觉，不是现代地图。

**R9 — 嗅辨可揭示什么 / 不可揭示什么。** 猫的嗅辨产出 `(reveal_target: location_id, hazard_hint: hazard_tag, confidence: int)`。猫永远不产出"明确负面评估"（如"此地必死请绕行"）——hazard_tags 可被写入但显示文案由 #6 决定，猫本身不"警告"。猫只指向"有什么值得去看"，不告诉玩家"那里是什么"。

**R10 — 物品来源数据契约。** 嗅辨签名 `cat_sniff_signature` 是物品 schema 的静态字段，由 #1 拥有并在内容管线中定义。本系统不动态生成嗅辨结果——只读字段、执行副作用。Schema 形式（参考）：

```yaml
cat_sniff_signature:
  reveal_target: location.kestrel-rock-01  # 揭示的地点 ID
  hazard_hint: low-visibility               # 单条 hazard_tag
  confidence: 50                            # 0-100 (raw; clamp to ≤66 happens in #15, not in content data)
  pattern_id: pattern.bird-flight-direction # 触发的观测事件规律 ID
```

**R11 — 小窝物件累积规则。** 每次嗅辨产出 `nest_token = true` 时（即首次嗅辨某物品成功），追加一件物件到小窝。MVP 上限 4 件——超过 4 件不再追加，但已追加的物件永久保留、不可逆。物件出现顺序固定（按列表索引追加）：

| 索引 | 物件 | 出现叙事 |
|---|---|---|
| 0 | 旧船帆碎布 | 初始铺底——猫自己铺的，暗示它在这里住了一段时间但你之前没注意到 |
| 1 | 锈蚀的测风链环 | 旧航线飞艇用于悬挂气压指示旗的配件，猫从工程舱拖来 |
| 2 | 玩家某次返航无意间落在工作台上的绳头 | 猫叼走了，你不一定记得那根绳子 |
| 3 | 空港徽章的残片 | 来源不明——但旧世界各空港曾发给常驻船猫身份牌。这只猫从哪里找来？游戏不解释 |

第 0 件物件的出现锚定 Player Fantasy 第二层"群体记忆的最后一块"；第 3 件物件的出现锚定 CD"30 小时后仍记得"的难忘瞬间——它出现时游戏什么都不说明，玩家自己去查或不查都构成一种体验。

**R12 — Idle 行为契约（被动陪伴层）。** 整备中（玩家在 Hub 操作各工作站期间），猫在飞艇内自由移动——轮流在情报台、工程舱旁蹲坐，偶尔跳上窗台观察外面，不主动打断玩家任何操作。玩家不需要管它，它也不需要被管。具体 idle 行为由状态机驱动（见 States and Transitions），动作流畅自然，不弹任何状态文本。

**R13 — 长航行后归港行为。** 玩家从 `in_transit` 返回 `landed` 时，猫不在入口等待——它在生活舱暖光旁，是玩家走过去发现它，而不是它跑过来欢迎玩家。这条规则保护避免"主人/宠物"等级语言——猫不依赖玩家，它只是选择留在这艘船上。

**R14 — "无反应"叙事呈现。** 物品来源完全超出猫族群历史认知范围（异域物品）时，猫嗅一下、停顿、走开去做别的事（比如跳上窗台看外面），动作流畅自然，与 idle 行为在视觉语言上连贯——不弹"此物品无法被嗅辨"提示。这种沉默本身是叙事信息：它不认识的东西才是真正的未知（反向 P4 提示）。通常面板层已过滤无签名物品，R14 是兜底契约。

**R15 — PR-Scope 边界守护（硬禁止）。** 本系统在系统层显式声明：

1. **无好感度数值** — 不持有任何 `affection / friendship / bond` 字段，不暴露此类查询接口
2. **无礼物菜单** — `scout_sniff` 是唯一物品交互入口，物品只能"给猫嗅辨"，不可"赠予猫"
3. **无事件树** — 猫的行为由状态机 + Hub 状态驱动，不存在剧情节点触发或对话分支解锁
4. **无定时器/出航等待** — 猫在 `in_transit` 期间简化模拟，不触发任何基于时间的事件或奖励
5. **无第二只伙伴** — `partner.sky-cat` 是 MVP 唯一伙伴实体，初始化路径硬编码此 partner_id
6. **无招募/解雇** — 猫从游戏开始就在飞艇上，无加入/离开流程

### States and Transitions

**猫的运行时状态机：**

| State | 显示位置 | 进入条件 | 有效转出 |
|---|---|---|---|
| `sleeping_on_intel_station` | 驾驶舱情报台 | Hub 进入 `landed` 时（默认态）；玩家加载存档后初始化 | → `idle_living_quarters`（玩家进入生活舱）→ `sniffing`（玩家在驻点递物品） |
| `idle_living_quarters` | 生活舱暖光区 | 玩家进入生活舱 OR 整备流程开始（玩家移动到驾驶舱以外区域） | → `following_player_to_bench`（玩家走向工作台）→ `in_nest`（自然闲置超过 `T_nest_settle`） |
| `following_player_to_bench` | 过渡动画 | Hub = `landed` 且玩家目标区域为工作台 | → `bench_adjacent`（抵达工作台旁） |
| `bench_adjacent` | 工作台边蹲伏 | `following_player_to_bench` 完成 | → `idle_living_quarters`（玩家离开工作台 reach_limit）→ `sniffing`（玩家在驻点 anchor_radius 内递物品） |
| `sniffing` | 伙伴驻点（生活舱）或工作台旁 | 玩家触发 `scout_verb_initiated` 事件 | → `idle_living_quarters`（动画播完后常规）OR `in_nest`（嗅辨产出 nest_token 且小窝当前为 `empty`） |
| `in_nest` | 生活舱角落小窝 | 自然闲置 OR 嗅辨产出首件物件 | → `idle_living_quarters`（玩家进入生活舱触发半径） |

**与 Hub 状态机的关系：**

| Hub 状态 | 猫状态行为 |
|---|---|
| `landed` | 状态机正常运转 |
| `departure_locked` | 状态冻结在当前态，不响应玩家输入 |
| `in_transit` | **简化模拟**——不渲染、不参与玩家可见行为；逻辑态保持为 `idle_living_quarters` |
| `arrival` | 强制转为 `idle_living_quarters`，0.5s 后按正常触发规则做首次转态判断 |

**小窝痕迹状态机（与 Hub R7 痕迹锚点系统对接）：**

| Stage | 进入条件 | 视觉锚点 | 不可逆 |
|---|---|---|---|
| `empty` | 初始态 | 生活舱角落无特殊陈设 | — |
| `first` | 首次 `scout_sniff` 产出 nest_token（`nest_items.size() == 1`） | 一片旧船帆碎布出现 | 是 |
| `accumulating` | `nest_items.size() ∈ {2, 3}` | 旧船帆 + 1-2 件叼来的小物件 | 是 |
| `full` | `nest_items.size() == 4` | 完整的小窝（4 件物件上限） | 是 |

无效转换：所有阶段单向前进——一旦累积就不可逆，不存在"玩家清理小窝"操作。

**命名状态机：**

| Stage | 进入条件 | 玩家可见 | 转出 |
|---|---|---|---|
| `pending` | 初始态——`naming_done == false` 且 `sniffed_items.size() == 0` | 命名提示尚未触发 | → `prompted`（首次成功嗅辨后下一次 player_returned_to_hub） |
| `prompted` | R3 触发条件满足 | 弹出命名 UI | → `completed`（玩家提交名字 OR 第 3 次跳过）→ `pending`（玩家关闭，已用次数 < 3） |
| `completed` | 玩家提交名字 OR 第 3 次跳过 | 名字写入存档；UI 不再弹出 | 无（终态） |

### Interactions with Other Systems

| System | 本系统提供 | 本系统接收 | 边界 |
|---|---|---|---|
| `内容数据与状态注册表` (#1) | — | 物品的 `cat_sniff_signature` 静态字段（只读） | 签名 schema 由 #1 拥有；本系统不修改物品 schema |
| `本地存档与世界状态持久化` (#3) | `progress.partner_skycat` 快照包：`{name, naming_done, naming_skip_count, sniff_success_occurred, nest_state, nest_items[], sniffed_items[]}` | 加载存档时初始化（`load_partner_state()`） | 本系统拥有自己的 save domain；不读其他系统的 domain。`sniff_success_occurred` 可从 `sniffed_items.size() > 0` 派生，显式存储为双重保险。瞬态字段（当前状态机状态、地图坐标、动画进度）不持久化——加载时重新派生 |
| `资源、货物与容量` (#5) | — | 物品 `item_id` + `source_location_id` 元数据（经玩家在驻点面板递入） | 物品归 #1，本系统只消费 `item_id` 查签名 |
| `玩家知识与情报` (#6) | 调用 `reveal_rumor(location_id, "partner.sky-cat", hazard_tags, confidence)`、`report_observation_event(pattern_id, "partner_sniff_success")`、`on_partner_joined("partner.sky-cat")`（新游戏初始化 1 次） | — | 单向写入；本系统不缓存知识状态，#6 为唯一真相源。confidence 强制 ≤ 66（MVP 硬上限） |
| `飞艇家园 Hub` (#7) | `query_partner_present() -> bool`（恒 true）、`query_partner_name() -> String`、`query_nest_state() -> NestStage` | `hub_state_changed(new_state)`、`player_returned_to_hub()`、`player_entered_zone(zone_id)` | Hub 提供物理位置和互动点；本系统提供伙伴逻辑态。Hub 痕迹锚点 R7 监听 `query_nest_state()` 变更并渲染对应阶段 |
| `航图与航线规划` (#9) | 无直接接口（经由 #6 间接影响） | 无 | 本系统写 #6，#9 读 #6；不建立直接依赖 |
| `UI / HUD / 航图界面` (#16) | `query_partner_name()`（用于伙伴驻点站点 hint 文本） | UI 渲染嗅辨面板（呈现持有 `cat_sniff_signature` 的物品列表） | 本系统提供数据；UI 拥有面板布局和模态管理 |

## Formulas

### Partner-Owned Formulas

The `confidence_clamp` formula is defined as:

`confidence_final(item) = min(item.cat_sniff_signature.confidence, MVP_CONFIDENCE_MAX)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `item.cat_sniff_signature.confidence` | — | int | 0–100 | 原始物品嗅辨置信度，由 #1 内容数据注册表定义 |
| `MVP_CONFIDENCE_MAX` | — | int | 0–100（常量；MVP = 66） | 本系统在 MVP 阶段允许向 #6 汇报的置信度上限 |
| `confidence_final` | — | int | 0–66 | 实际传递给 `reveal_rumor()` 的置信度值 |

**Output Range:** 0–66（MVP 强制上限）。永不达 67——这是 #6 的"权威"门槛——由 R8 约束。`min()` 不可跳过——即使物品原始置信度为 100，汇报值仍为 66。

**Example:** 某香料物品原始 `confidence = 90` → `confidence_final = min(90, 66) = 66`（被截断）。某残骸碎片原始 `confidence = 30` → `confidence_final = min(30, 66) = 30`（未截断）。

---

The `naming_prompt_eligibility` formula is defined as:

`naming_eligible(s) = (naming_state == pending) AND (sniff_success_occurred == true) AND (player_returned_to_hub == true) AND (naming_skip_count < NAMING_SKIP_MAX)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `naming_state` | — | enum | {pending, prompted, completed} | 当前命名状态机阶段（见 Section C 命名状态机） |
| `sniff_success_occurred` | — | bool | true/false | 生命周期内是否发生过至少一次嗅辨成功（持久化标志，不重置） |
| `player_returned_to_hub` | — | bool | true/false | 本次触发来自 `player_returned_to_hub()` 事件（瞬态，仅在事件回调内为 true） |
| `naming_skip_count` | — | int | 0–3 | 玩家已跳过命名 UI 的次数 |
| `NAMING_SKIP_MAX` | — | int | 常量 = 3 | 允许跳过的最大次数；第 3 次跳过后锁定默认名"那只猫" |
| `naming_eligible` | — | bool | true/false | 是否在此次 `player_returned_to_hub` 事件触发命名 UI |

**Output Range:** true/false。当 `naming_skip_count == NAMING_SKIP_MAX` 时 `naming_eligible` 永远为 false（命名窗口已关闭）；此时 `naming_state` 应已由状态机强制转为 `completed`（默认名），故该条件实为双重保险。

**Example:** 玩家首次嗅辨成功（`sniff_success_occurred = true`），返航后 `player_returned_to_hub` 触发，`naming_state = pending`，`naming_skip_count = 0` → `naming_eligible = true` → 弹出命名 UI。第三次跳过后 `naming_skip_count = 3` → `naming_eligible = false` → 写入默认名"那只猫"。

### External Formulas Referenced

以下计算 / 数据由其他系统拥有，本系统仅引用：

| 公式 / Schema | 拥有系统 | 本系统如何使用 |
|------|---------|-------------|
| 置信度区间映射（0–33 不确定 / 34–66 可靠 / 67–100 权威） | `玩家知识与情报` #6 | 调用 `reveal_rumor()` 时传入 `confidence_final`；#6 据此映射 `knowledge_state`。本系统不拥有区间边界定义 |
| `state_transition` 触发条件（`rumored → identified` 等） | `玩家知识与情报` #6 | 本系统仅负责推送 `reveal_rumor()`；何时升级 knowledge_state 完全由 #6 拥有 |
| `cat_sniff_signature` schema（`reveal_target / hazard_hint / confidence / pattern_id`） | `内容数据与状态注册表` #1 | 读取 `item.cat_sniff_signature.confidence` 作为 F1 输入；schema 定义归 #1 |
| `item_id / source_location_id` 元数据 | `资源、货物与容量` #5 | 嗅辨时通过 `item_id` 定位物品的 `cat_sniff_signature`；元数据归 #5 |

### Sections That Need No Formula

以下设计元素已在 Detailed Design 中以状态表和规则形式完整定义，不需要额外公式：

- **猫的 6 态运行时状态机**（`sleeping_on_intel_station / idle_living_quarters / following_player_to_bench / bench_adjacent / sniffing / in_nest`）——状态/转换/触发已在状态表中枚举，无计算成分，只有离散迁移规则
- **小窝痕迹 4 阶段状态机**（`empty / first / accumulating / full`）——由 `nest_items.size()` 分段派生的状态标签，等价于一个查表（0→empty, 1→first, 2-3→accumulating, 4→full）；无连续数学，写成表格比公式更清晰
- **命名 3 阶段状态机**（`pending / prompted / completed`）——状态迁移触发条件由 F2 `naming_prompt_eligibility` 捕获；状态机本身是枚举转换表
- **R7 嗅辨反应符号集**（耳朵后转 / 尾巴方向 / 绕圈 / 蹭脸 / 困惑 / 已闻过）——纯查表，从嗅辨结果类型映射到动画反应 ID，无数学
- **R11 小窝物件清单**（4 件静态枚举，索引 0-3 顺序固定）——静态有序枚举，大小由常量 `NEST_CAPACITY` 约束，不需要公式
- **`scout_sniff_outcome`（R5-R6 算法全流程）**——6 步判定算法中每一步均为离散条件判定（签名查找 → clamp → hazard 映射 → 接口调用），无连续函数；其中唯一的计算成分已由 F1 `confidence_clamp` 覆盖。将整个算法公式化会引入伪精确性而非可测试性

## Edge Cases

### E.1 Naming System

**E.1.a — Player never completes a successful sniff.** The player returns to hub multiple times but never gives the cat a sniffable item, or all sniff attempts land on null-signature items. `sniff_success_occurred` stays `false` permanently. `naming_state` stays `pending` permanently. The naming UI never triggers. Any UI that would display the partner name shows a generic descriptor (e.g., "那只灰白猫" from hint text) since `partner_skycat.name` is unset. This is a valid permanent state — naming is gated behind the cat proving itself first (R3).

**E.1.b — Player skips naming exactly 3 times.** On the 3rd prompted return (the 3rd time the naming UI opens and the player skips), `naming_skip_count` reaches 3 (= `NAMING_SKIP_MAX`). The system immediately writes `name = "那只猫"`, sets `naming_done = true`, and transitions `naming_state` to `completed`. The naming UI closes and never reopens. All future UI displays "那只猫". The transition is silent — no notification popup informs the player that the default name has been locked in. This matches R4: "第 3 次跳过后" the default name is locked at the moment of the 3rd skip, not deferred to a subsequent return.

**E.1.c — Player submits an empty string or whitespace-only name.** The naming UI client-side (owned by system #16) rejects the submission with a validation hint. This rejection does NOT count as a skip — `naming_skip_count` is not incremented, `naming_state` stays `prompted`, and the UI stays open. The player must either enter a valid name or close/skip the prompt.

**E.1.d — Player submits a name exceeding the 8-character limit.** The naming UI client-side (system #16) enforces the limit — either truncating to 8 characters before submission or rejecting with validation hint. The partner system also clamps to 8 characters as a safety net on write, but the primary enforcement is at the UI layer. The stored `partner_skycat.name` never exceeds 8 characters.

**E.1.e — Game saved/loaded while naming UI is open (`naming_state == prompted`).** The naming UI is a transient modal and is not persisted. On save, the system persists `naming_state = prompted` and the current `naming_skip_count`. On load, the system checks: if `naming_state == prompted` and `naming_skip_count < NAMING_SKIP_MAX`, wait for the next `player_returned_to_hub()` event, then re-trigger the naming UI. The skip count at time of load is preserved — if the player had skipped 2 times before saving, they get 1 more opportunity after loading.

**E.1.f — Player quits or crashes after submitting a name but before the save transaction commits.** The name and `naming_done` flag are written atomically as part of the naming completion transaction. If the save fails or the game crashes mid-transaction, on next load `naming_state` may be `prompted` (transaction didn't commit) or `completed` (it did). If `prompted`, the system re-triggers per E.1.e — the player gets another chance to name with the same remaining skip count. No special recovery logic is needed; the idempotent re-trigger handles both cases.

**E.1.g — Naming UI is open and Hub state transitions to `departure_locked`.** The naming UI is a modal owned by system #16. `player_returned_to_hub()` fires during the arrival sequence, BEFORE the player regains control and before departure can be initiated. The naming modal must be resolved (named or skipped) before any other UI interaction — including departure controls — becomes available. If a bug or race condition causes `departure_locked` to fire while the naming modal is open, the modal remains open; the departure animation is deferred until the modal is resolved.

### E.2 Sniffing System

**E.2.a — Player returns to hub with zero items in inventory.** The sniff interact point (partner station) remains active. Opening the sniff panel shows an empty item list — all inventory items are 0, so the filter yields zero candidates. System #16 renders an empty-state view (text or visual equivalent). The cat does not react to the panel being opened empty. No partner system state changes occur.

**E.2.b — Player opens sniff panel; all inventory items lack `cat_sniff_signature`.** Same behavior as E.2.a — the filter yields zero eligible items, empty list displayed. This is the expected case for generic resources (fuel, repair parts, trade goods not tied to specific locations). The filter operates on the field's existence: only items where `cat_sniff_signature != null` appear in the list. Items with a signature whose fields are all zero/default values still appear in the list (they passed existence check) and are handled by E.2.e or E.2.f.

**E.2.c — Same `item_id` given to cat twice.** On the second attempt, `item_id` is already in `sniffed_items`. `scout_sniff()` returns `Null` at step 1 (R6 algorithm). The cat plays the "already smelled" animation: ears relax and flatten down, a short gesture, then returns to idle. No `reveal_rumor()` call is made. No `nest_token` is produced. The item remains in the player's inventory — sniffing never consumes items. `sniffed_items` is a set; duplicate insertion is a no-op.

**E.2.d — Item's `cat_sniff_signature` field is `null`.** Step 2 of R6 detects the null signature. The cat plays the "confused" animation (ears forward, head tilts left and right, then walks away to do something else — per R14). No `reveal_rumor()` call is made. No `nest_token` is produced. The `item_id` is NOT added to `sniffed_items` (only successful sniffs are recorded, per R6 steps 3-5 only executing in the success path). The player CAN re-offer the same null-signature item with the same "confused" result — this is intentional narrative: the cat genuinely does not recognize this item, and consistent confusion is the information. The panel filter should normally exclude null-signature items (E.2.b), so this path serves as a defensive fallback.

**E.2.e — Item's `cat_sniff_signature.confidence > 66`.** `confidence_final = min(raw_confidence, MVP_CONFIDENCE_MAX) = min(raw_confidence, 66)`. The item's `reveal_target` and `hazard_hint` are used normally; the clamped confidence (max 66) is passed to `reveal_rumor()`. The cat's reaction animation is chosen based on `confidence_final` (R7: `>= 50` → circles twice then leaves; `< 50` → rubs face then sits). A raw confidence of 90 produces `confidence_final = 66`, which triggers the high-confidence animation. The knowledge state written to system #6 is `rumored`, never `authoritative` (which requires confidence >= 67, per #6's confidence tier mapping).

**E.2.f — Item has `cat_sniff_signature` with valid structure but `reveal_target` is null or empty.** This is a content data defect — the signature schema exists but its core field is empty. The partner system performs a defensive validity check before calling `reveal_rumor()`: if `cat_sniff_signature.reveal_target` is null, empty string, or whitespace-only, treat identically to a null signature (E.2.d) — confused animation, no `reveal_rumor()` call, no `nest_token`, item NOT added to `sniffed_items`. This prevents garbage data from propagating to system #6. A warning is logged in development builds.

**E.2.g — Multiple items with the same `reveal_target` but different `confidence` values.** Two separate `reveal_rumor()` calls are made for the same `location_id` with potentially different confidence values (e.g., 30 from item A, 66 from item B). The partner system does not check system #6's current knowledge state before calling — it always pushes. System #6 owns the conflict resolution: it may update to the higher confidence, merge hazard hints, or keep the first-received value. From the partner system's perspective, both sniffs are independently successful: both items are added to `sniffed_items`, both may produce `nest_token` (if first-time sniffs and nest not yet full). Multiple independent clues pointing to the same location is narratively coherent and reinforces the location's importance.

**E.2.h — Player rapidly spams the sniff interaction (clicks "sniff" multiple times before animation completes).** `scout_sniff()` is gated by cat state. On the first call, the cat transitions to `sniffing` state. While in `sniffing`, all subsequent `scout_sniff()` calls for any item are rejected — the cat is busy. The UI (system #16) should debounce the confirm button and/or close the panel after selection, but the state gate is the authoritative lock. Once the sniff animation completes and the cat transitions out of `sniffing` (to `idle_living_quarters`, `bench_adjacent`, or `in_nest`), the next sniff can be initiated.

**E.2.i — Hub state transitions to `departure_locked` during sniffing animation.** Per the Hub state table, `departure_locked` freezes cat state transitions. The sniff outcome (data) was already computed synchronously at the start of `scout_sniff()`: `reveal_rumor()` was called, `item_id` was added to `sniffed_items`, `nest_token` was computed and nest accumulation executed. The animation is presentation-only. When `departure_locked` fires mid-animation, the data is already committed — no data loss occurs. The animation may be interrupted or truncated visually (cat jumps to departure position), or may complete naturally before the state freeze takes effect. Either behavior is acceptable; the data integrity is the invariant.

**E.2.j — Player returns to hub with multiple new sniffable items, sniffs one, then immediately departs without sniffing the rest.** Unsniffed items stay in the player's inventory. They can be sniffed on any future return to hub. There is no penalty, no decay, no missed opportunity window. The cat does not "notice" or "react to" items going unsniffed.

### E.3 Nest System

**E.3.a — Player never triggers a successful sniff.** `nest_state` stays `empty` indefinitely. The living quarters corner has no special visual anchor rendered by system #7's trace anchor system. The nest system stays entirely dormant. This is a valid permanent state — the nest is earned through the cat's demonstrated activity, not a guaranteed unlock.

**E.3.b — All 4 nest items accumulated (nest full); player continues to sniff new items successfully.** `nest_items.size() == 4 == NEST_CAPACITY`. The `scout_sniff()` algorithm still computes `nest_token = true` for first-time successful sniffs (step 5 of R6), but the nest accumulation logic (R11) checks `if nest_items.size() < NEST_CAPACITY` before appending. The append is silently skipped. All other sniff outcomes proceed normally: `reveal_rumor()` is called, `item_id` is added to `sniffed_items`, the reaction animation plays. The nest permanently stays at 4 items. The irreversibility contract holds — items already in the nest are never removed.

**E.3.c — Game saved with partially accumulated nest (e.g., `nest_items.size() == 2`), then loaded.** The `progress.partner_skycat` save block includes `nest_state` (= `accumulating`) and `nest_items[]` (= `[0, 1]`). On load, the system restores these exact values. System #7's trace anchor system renders the visual anchors for indices 0 and 1 (旧船帆碎布 + 锈蚀的测风链环). The nest is restored to its exact pre-save state. The irreversibility contract is preserved across save/load: no loaded nest ever has fewer items than it had when saved.

**E.3.d — Player triggers first successful sniff (nest transitions `empty` → `first`), then quits or crashes before the save commits.** The sniff result and nest update are part of the same atomic unit. If the save transaction did not commit: on load, `sniffed_items` does not contain the item, `nest_state = empty`, `nest_items = []`. The player can re-sniff the item, which triggers the nest again normally. If the transaction partially committed (corrupted save), the snapshot package validity formula (owned by system #3) detects the violation and rejects the snapshot; the game falls back to the last valid save. No special reconciliation logic is needed within the partner system.

### E.4 State Machine

**E.4.a — Game saved while cat is in `sniffing` state, then loaded.** The cat's current state machine state (`sniffing`) is a transient field and is NOT persisted (per interactions table: "瞬态字段不持久化——加载时重新派生"). On load, the system derives the cat's initial state from Hub state and player position:
- Hub `landed` + player not in 生活舱 → `sleeping_on_intel_station`
- Hub `landed` + player in 生活舱 → `idle_living_quarters`
- Hub `in_transit` → `idle_living_quarters` (logical, not rendered)
- Hub `arrival` → force `idle_living_quarters` per Hub state table

The in-progress sniffing animation is not resumed. The sniff data was committed synchronously before the animation began, so no data is lost. If the save happened after the data commit but during the animation: on load, the sniff result is persisted in `sniffed_items` and system #6's knowledge store, but the cat is in a derived idle state — the animation never completed visually. This is acceptable; the implementation complexity of resuming mid-animation state on load is not justified.

**E.4.b — Player rapidly transitions between hub zones (entering and leaving 生活舱 repeatedly).** Each `player_entered_zone("living_quarters")` event triggers a state evaluation: if cat is in `sleeping_on_intel_station`, transition to `idle_living_quarters`. Leaving 生活舱 could trigger the reverse. If the player spams zone boundaries, the cat could visually jitter between states. To prevent this, a minimum dwell time (`T_cat_state_cooldown`, default 0.5s) is applied: once the cat transitions to a new state, no further non-forced state transition (i.e., excluding `sniffing`, `departure_locked`, and `arrival` forced transitions) is processed until the cooldown expires. This is a visual polish parameter; the state machine logic remains correct regardless of cooldown value.

**E.4.c — Player returns to hub while the cat's pre-departure state was `in_nest`.** Per the Hub state table: `arrival` forces the cat to `idle_living_quarters`. After 0.5s, the normal transition evaluation runs. The cat is in the living quarters when the player arrives, not hiding in the nest. If the player enters 生活舱 and then idles, the cat may transition back to `in_nest` after `T_nest_settle` of idle time. This sequence serves R13: the cat is not waiting at the entrance, but it's not permanently hiding either.

**E.4.d — Cat is in `in_nest` state; player enters 生活舱 trigger radius.** Per the state machine: `in_nest → idle_living_quarters` fires immediately when the player enters the 生活舱 trigger radius. The cat leaves the nest and moves to the warm-light area. This transition fires even if the player immediately walks past and leaves 生活舱 — the cat has already left the nest for this "visit" and will not re-enter until `T_nest_settle` of idle time elapses while the player remains in the living quarters area.

### E.5 Interface and Data Contracts

**E.5.a — `reveal_rumor()` call to system #6 fails (system #6 is unavailable, or returns an error).** The partner system logs the error and does NOT retry. The sniff is considered successful from the partner system's perspective: `item_id` is added to `sniffed_items`, `nest_token` is computed and nest accumulation executed, and the reaction animation plays (it was selected based on `confidence_final` before the `reveal_rumor()` call). The failed rumor is silently lost — the player sees the cat react but no new entry appears on the chart. The player cannot distinguish this from a cat reaction that produced information too weak to register. In development builds, a warning is logged with `item_id` and `reveal_target` for content debugging.

**E.5.b — `report_observation_event(pattern_id, "partner_sniff_success")` called with a `pattern_id` that does not exist in system #6's observation pattern registry.** The partner system passes `pattern_id` as-read from the item's `cat_sniff_signature`. System #6 owns `pattern_id` validation. If the pattern does not exist, system #6 may log a warning, ignore the event, or create a dangling reference — the partner system has no visibility into this outcome. The sniff otherwise proceeds normally. This is a content pipeline contract: every `pattern_id` in `cat_sniff_signature` must correspond to a valid entry in #6's `observation_patterns` table.

**E.5.c — `cat_sniff_signature.reveal_target` references a `location_id` that does not exist in the world registry (system #1) or is marked as unreleased/inactive.** The partner system passes the `location_id` as-read. System #6 (`reveal_rumor()`) owns location existence validation. The outcome is system #6's responsibility — it may create an orphaned rumor (revealing a location that can never be visited in the current build), reject the call, or log an error. The partner system does not validate location existence. This is a content pipeline contract.

**E.5.d — `reveal_target` references a location the player already knows at `identified` or `verified` confidence (higher than the cat can ever provide, since cat max is 66 < 67 authority threshold).** The partner system calls `reveal_rumor()` regardless — it does not query system #6's current knowledge state before calling. System #6 owns the conflict: it may ignore the lower-confidence input, update only if new confidence exceeds current, or merge hazard hints. The partner system's local state updates (`sniffed_items`, nest) proceed independently of whether #6 accepts the knowledge contribution. The player gets a cat reaction without new chart information — narratively coherent with "the cat's nose isn't always useful for things you already know."

**E.5.e — Item's `cat_sniff_signature` schema contains fields unknown to the partner system's deserializer (forward compatibility with content updates).** The partner system reads only the fields it knows: `reveal_target`, `hazard_hint`, `confidence`, `pattern_id`. Unknown fields are silently ignored. The deserializer is designed for forward compatibility — a newer schema version with additional fields (e.g., a future `cultural_context` tag) does not break existing sniff logic.

### E.6 Presence and Initialization

**E.6.a — `query_partner_present()` called during `in_transit`.** Returns `true` unconditionally — per R2, the contract is absolute and does not depend on Hub state. The caller (system #7 or any consumer) may cache or ignore the result based on its own Hub state awareness. The partner system does not condition its answer on context.

**E.6.b — System #7 fires `player_returned_to_hub()` multiple times due to a bug or race condition.** The partner system's handler is idempotent with respect to key side effects. For naming: `naming_prompt_eligibility` (F2) requires `naming_state == pending` AND `sniff_success_occurred == true` AND `naming_skip_count < NAMING_SKIP_MAX`. Once `naming_state` transitions to `prompted` or `completed`, subsequent firings are no-ops. For cat state: multiple `player_returned_to_hub()` events when Hub is already `landed` trigger redundant `arrival`-style state derivations, but since the cat is already in the correct derived state, the redundant transition to the same state is a no-op.

**E.6.c — Game initialization race: Hub (#7) loads and fires `hub_state_changed(landed)` before the partner system's save data (#3) has been loaded.** The partner system's initialization sequence: (1) load `progress.partner_skycat` from save data, (2) derive initial cat state from loaded data + current Hub state, (3) subscribe to Hub events. If events fire before step 3, the event system queues them for delivery after subscription. If the engine does not provide event queuing, the partner system explicitly calls `sync_with_hub_state(current_hub_state)` after step 2 and before step 3, ensuring the cat's state is correct regardless of initialization order.

**E.6.d — New game: `on_partner_joined("partner.sky-cat")` fires before system #6 has completed initialization.** `on_partner_joined()` is dispatched during the post-initialization bootstrap phase, after all systems have signaled readiness, not during individual `_ready()` callbacks. The game bootstrap sequencer ensures system #6 signals readiness before partner initialization events are dispatched. If system #6 has not signaled readiness when the partner system attempts the call, the call is queued and delivered when #6 is ready.

## Dependencies

This section classifies dependencies by their operational criticality, specifies initialization order, maps save/load data ownership, and flags existing GDDs that need revision because of this system. The Section C "Interactions with Other Systems" table already documents the bidirectional API contracts for systems #1, #3, #5, #6, #7, #9, and #16. This section does NOT duplicate that table; it answers how the system degrades when a dependency is absent, what order things must start in, who owns what data at the persistence boundary, and which existing GDDs carry stale assumptions.

### F.1 Hard Dependencies

A hard dependency is one whose absence makes the partner system fail to deliver its core design purpose (the scout function) or violates a Creative Director constraint (persistent relationship memory). Degradation behaviour is specified for each.

| # | System | Depended-On API / Feature | Degradation If Unavailable |
|---|--------|--------------------------|---------------------------|
| 6 | `玩家知识与情报` | `reveal_rumor(location_id, source_tag, hazard_tags, confidence)` — write sniff results into the player's chart knowledge. `report_observation_event(pattern_id, "partner_sniff_success")` — notify the observation pattern system. `on_partner_joined("partner.sky-cat")` — register the partner during new-game bootstrap. | Sniff animations play but no knowledge is created. The cat reacts (R7), but no rumored location appears on the chart, no observation event is logged, and no partner-based ability unlock conditions in #6 can fire. The partner degrades to cosmetic presence only — the "scout" in "scout partner" is severed. |
| 7 | `飞艇家园 Hub` | `hub.interactable.partner_station` (interaction_type: `talk`) — physical anchor for the sniff verb. Living quarters warm-light zone — spatial trigger for idle state transitions and nest area. Hub state events (`hub_state_changed`, `player_returned_to_hub`, `player_entered_zone`) — inputs driving the cat's runtime state machine. Trace anchor system (Hub R7) — renders nest visual staging. | The cat exists as abstract data only. There is no physical space for the cat, no interaction point to trigger `scout_sniff()`, no zone events to drive idle behavior transitions, and no surface to render the nest. The entire embodied-presence layer (Player Fantasy layers 1 and 2) is non-functional. |
| 3 | `本地存档与世界状态持久化` | Save/load domain for `progress.partner_skycat` snapshot package: `{name, naming_done, naming_skip_count, nest_state, nest_items[], sniffed_items[]}`. | All relationship memory is session-volatile. On every session start the cat resets to initial state: unnamed (`naming_state = pending`), empty nest, empty sniff history. The CD constraint "persistent relationship memory" fails. The Player Fantasy layer 3 ("它会记得你") is contradicted — the cat forgets everything every time the game restarts. |
| 1 | `内容数据与状态注册表` | Content pipeline: `partner.sky-cat` companion ID registration. `cat_sniff_signature` schema field on items (`reveal_target`, `hazard_hint`, `confidence`, `pattern_id`). Content domain `companions` marked `COMPLETE`. | **Runtime:** zero items possess `cat_sniff_signature` — the sniff panel is permanently empty (E.2.a/b behaviour). The cat exists, idle behaviours run, but the core scout loop never starts: no successful sniff, naming never triggers (gated behind first sniff success per R3), nest never appears. The system is technically alive but gameplay-inert. **Content-authoring:** this is a blocking content pipeline dependency — items must be authored with sniff signatures before the system produces player-visible output. This is a hard content pipeline dependency with graceful runtime degradation. |

### F.2 Soft Dependencies

A soft dependency enhances the partner system's value or accessibility but is not required for it to function. The system operates in a degraded-but-valid state when a soft dependency is absent.

| # | System | What It Provides | Degradation If Unavailable |
|---|--------|-----------------|---------------------------|
| 5 | `资源、货物与容量` | Player inventory items (with `item_id` and `source_location_id` metadata) are the input to `scout_sniff()`. The sniff panel filters inventory for items having `cat_sniff_signature`. | Sniff panel is always empty (same as E.2.a — zero items in inventory). The partner system functions but has no input to process. This is a valid permanent state; the cat's presence and idle behaviours are unaffected. The scout function is dormant rather than broken. |
| 16 | `UI / HUD / 航图界面` | Sniff panel UI (item list filtered by `cat_sniff_signature` existence, confirm button, debounce). Naming UI (text input, 1-8 character limit, skip button, validation). Partner name hint text rendering at partner station. Modal management for both panels. | Partner logic works correctly (all APIs, state transitions, data commits) but the player has no interface to trigger `scout_sniff()` or respond to the naming prompt. The system is functionally alive but player-inaccessible. If a hypothetical debug command or automated test calls the APIs directly, all outcomes are correct. |
| 9 | `航图与航线规划` | Chart displays rumored locations. The partner writes knowledge via #6; the chart reads from #6. This is an indirect dependency — no direct API between #15 and #9. | Rumored locations are written to #6 but never displayed on any chart UI. The player receives no visual confirmation of the cat's sniff results on the navigation surface. The knowledge exists in the data model but is invisible to the player's primary planning tool. |
| 10 | `航行与路线风险` | Departure checklist (Hub R9) references partner station visit status: if "伙伴驻点未交互", the departure confirmation shows "无侦察简报" and may apply an encounter-rate penalty (e.g., `uninformed_departure_penalty`). | The departure checklist loses the partner station line item. The scout function still works (sniff results are written to #6 regardless), but the departure flow cannot remind the player that they have not consulted the cat. No impact on the partner system itself. |
| 2 | `平台与会话壳` | Game lifecycle (start, continue, tab-focus recovery, session management). All gameplay systems depend on this implicitly. | Standard platform-layer failure: game cannot launch or maintain session. Not specific to the partner system; captured here for completeness. |

### F.3 Initialization Order

The partner system must initialize AFTER all hard dependencies have signalled readiness. The following sequence is load-bearing:

| Order | System | What Must Be Ready | Partner System Action |
|-------|--------|--------------------|-----------------------|
| 1 | #1 `内容数据与状态注册表` | Item schemas loaded; `cat_sniff_signature` fields queryable; companion ID `partner.sky-cat` registered; content domain `companions` status is at least `STUB`. | (None — prerequisite only) |
| 2 | #3 `本地存档与世界状态持久化` | Save/load infrastructure ready; `progress.partner_skycat` domain accepted. | Load `progress.partner_skycat` snapshot from save data (or initialise defaults for new game). |
| 3 | #6 `玩家知识与情报` | `reveal_rumor()`, `report_observation_event()`, and `on_partner_joined()` APIs available for subscription/call. | If new game: queue `on_partner_joined("partner.sky-cat")` for delivery after bootstrap. |
| 4 | #7 `飞艇家园 Hub` | Hub scene loaded; interaction registry populated (including `partner_station`); zone triggers active; state events (`hub_state_changed`, `player_returned_to_hub`, `player_entered_zone`) ready. | Derive initial cat state from loaded data + current Hub state. Subscribe to Hub events. Call `sync_with_hub_state(current_hub_state)` to handle any events that fired before subscription (per E.6.c). |
| 5 | #15 `伙伴功能与关系` | (This system) | Signal readiness. Bootstrap sequencer dispatches queued `on_partner_joined()` to #6. Normal operation begins. |

**Bootstrap race condition handling** (formalised from E.6.c and E.6.d):

- If Hub events fire before the partner system subscribes: the partner system explicitly calls `sync_with_hub_state(current_hub_state)` after step 4 and before step 5. If the engine does not provide event queuing, this explicit sync is mandatory.
- If `on_partner_joined()` is dispatched before #6 signals readiness: the game bootstrap sequencer queues the call and delivers it when #6 is ready. The partner system does NOT call `on_partner_joined()` during its own `_ready()` callback; it defers to the post-initialization bootstrap phase.

### F.4 Save/Load Data Ownership

The partner system owns its own persistence domain. The following matrix clarifies who reads and writes what at the save/load boundary:

| Data Package | Owner | Persisted By | Read On Load | Written On Save | Notes |
|-------------|-------|-------------|-------------|----------------|-------|
| `progress.partner_skycat` | #15 (本系统) | #3 infrastructure | #15 via `load_partner_state()` | #15 via save snapshot export | Full snapshot: `name`, `naming_done`, `naming_skip_count`, `sniff_success_occurred`, `nest_state`, `nest_items[]`, `sniffed_items[]`. `sniff_success_occurred` is derivable from `sniffed_items.size() > 0` but stored explicitly as defense-in-depth. Transient fields (current state machine state, animation progress, map coordinates) are NOT persisted — re-derived on load per E.4.a. |
| `cat_sniff_signature` on items | #1 | N/A (static content) | #15 reads during `scout_sniff()` | Never | Read-only. Defined in content pipeline, not in save data. |
| `knowledge_state` per location | #6 | #3 infrastructure | Never (partner only writes) | Via `reveal_rumor()` calls | #6 is the sole source of truth for knowledge. Partner never caches or reads knowledge state. |
| `observation_events` per pattern | #6 | #3 infrastructure | Never (partner only writes) | Via `report_observation_event()` calls | Partner reports events; #6 owns event storage and pattern progression. |
| Hub trace anchor `nest_visual_stage` | #7 | #3 infrastructure (within `progress.airship`) | Derived by #7 from `query_nest_state()` | #7 snapshots visual stage | Partner provides `query_nest_state()`; #7 owns the visual rendering state. Partner never writes to `progress.airship`. |

**Snapshot atomicity**: The sniff result (`sniffed_items` update, `nest_token` computation), the `reveal_rumor()` call to #6, and the nest accumulation are part of the same logical transaction. If the save transaction does not commit (crash, error), the partner system's state and #6's knowledge state may be inconsistent on next load. E.3.d and E.5.a address these cases: the partner system treats its own local state as authoritative for replay safety, and #6's state may lag behind (sniffed item re-offered, nest re-triggered). No cross-system two-phase commit is required in MVP.

### F.5 Cross-GDD Revision Flags

The following existing GDDs contain assumptions, placeholders, or open questions that this GDD resolves or contradicts. Each flag identifies the specific section/location that needs updating and the nature of the change.

#### Flag 1: #6 `玩家知识与情报` — Dependencies entry is stale

**Location**: Section "Dependencies", entry `#10 伙伴功能与关系 — 尚未设计`.

**Current text**: States the partner system GDD is not yet created. Lists `reveal_rumor()` as the sole upstream API.

**Change needed**:
- Mark the entry as designed and add bidirectional reference to this GDD.
- Add `report_observation_event(pattern_id, "partner_sniff_success")` to the upstream API list — the partner system calls this after every successful sniff (R6 step 3).
- Add `on_partner_joined("partner.sky-cat")` to the upstream API list — called once during new-game bootstrap.
- Note that MVP `reveal_rumor()` calls from the partner system are clamped to `confidence ≤ 66` (never reach the `权威` tier requiring `confidence ≥ 67`).

#### Flag 2: #6 `玩家知识与情报` — Part 8 human partners need Post-MVP flag

**Location**: Part 8 "知识伙伴身份锚点".

**Current text**: Describes three human partners (`partner.old-sailor`, `partner.lighthouse-keeper-descendant`, `partner.cartographer`) with identity anchors and observation event types. No indication that these are outside MVP scope.

**Change needed**:
- Add a Post-MVP scope marker at the top of Part 8, cross-referencing this GDD's R15.5 ("无第二只伙伴 — `partner.sky-cat` 是 MVP 唯一伙伴实体").
- The three human partners' identity anchors, observation events (`bird-partner-sailor`, `lh-partner-keeper`, `fog-partner-cartographer`), and ability unlock conditions (Path C/D partner requirements) remain valid design but are explicitly Post-MVP.
- MVP knowledge advancement via partner is exclusively through `partner.sky-cat`'s `reveal_rumor()` calls, not through `partner_comment` observation events (which require human partners).

#### Flag 3: #7 `飞艇家园 Hub` — Trace anchor R7 "生活舱个人物品" clarified

**Location**: R7 "最低生活痕迹".

**Current text**: Lists "生活舱个人物品存在（无 → 有）" as a binary trace anchor. OQ-7 asks "生活舱'私人物品'痕迹锚点的具体内容".

**Change needed**:
- The binary "无 → 有" anchor is now a 4-stage visual driven by `query_nest_state()` from #15: `empty` (no anchor rendered), `first` (旧船帆碎布), `accumulating` (2-3 items), `full` (4 items). See R11 and the nest trace state machine for the full staging.
- OQ-7 is answered by this GDD: the "私人物品" are the 4 nest objects (旧船帆碎布, 锈蚀的测风链环, 玩家绳头, 空港徽章残片). These are not generic decorations; they are narrative objects accumulated by the cat's own activity.
- The trace anchor rendering contract: #7 listens to `query_nest_state()` changes and renders the corresponding visual stage in the living quarters corner. The partner system does not own visual rendering.

#### Flag 4: #1 `内容数据与状态注册表` — New item schema field

**Location**: Items schema definition; content domain `companions`.

**Current state**: No `cat_sniff_signature` field exists in the items schema. The `companions` content domain is listed but has no partner ID entries yet.

**Change needed**:
- Add `cat_sniff_signature` as an optional static field on item schema entries, with sub-fields: `reveal_target` (location_id, required if signature exists), `hazard_hint` (single hazard_tag, optional), `confidence` (int 0-100, required if signature exists), `pattern_id` (observation pattern ID, optional).
- Register `partner.sky-cat` as the MVP companion ID with role tag `scout`.
- Content validation rule: for any item with `cat_sniff_signature` present, `reveal_target` must reference a valid `location_id` in the registry (otherwise the defensive check in E.2.f triggers and the signature is treated as null).
- Items without `cat_sniff_signature` are valid — they simply never appear in the sniff panel (E.2.b).

#### Flag 5: #16 `UI / HUD / 航图界面` — Future GDD must include partner UI

**Location**: Not yet authored (status: Not Started in systems index).

**Change needed** (for when #16 is authored):
- Sniff panel: item list filtered to entries where `cat_sniff_signature != null`; confirm button debounced; panel closes on item selection; empty-state view when zero eligible items (E.2.a).
- Naming UI: text input with 1-8 character limit enforced client-side; skip/close button; "稍后再取" option; validation hint for empty/whitespace-only input (E.1.c).
- Naming modal must block all other UI interaction including departure controls (E.1.g). It fires during the arrival sequence, before the player regains control.
- Partner name hint text at the partner station: displays `query_partner_name()` result, or a fallback descriptor ("那只灰白猫") when name is unset.

---

**Summary of revision impact**:

| GDD | Severity | Action |
|-----|----------|--------|
| #6 `玩家知识与情报` | **Medium** | Update Dependencies entry; add Post-MVP flag to Part 8; add `report_observation_event` and `on_partner_joined` to API list. |
| #7 `飞艇家园 Hub` | **Low** | Update R7 trace anchor spec from binary to 4-stage; close OQ-7. Dependencies already list #15; InteractionRegistry already includes partner station. |
| #1 `内容数据与状态注册表` | **Medium** | Add `cat_sniff_signature` to item schema; register `partner.sky-cat` companion ID; add content validation rule for `reveal_target` references. |
| #16 `UI / HUD / 航图界面` | **Low** (informational) | Flag for future authoring: sniff panel, naming UI, modal blocking behaviour. |

No revision is needed for #3 (persistence), #5 (resources), #9 (chart), or #2 (platform) — the partner system's relationship with these systems is consumption-only (read or write without requiring schema changes on their side) and all API contracts are already defined on the partner side.

## Tuning Knobs

This section catalogs every configurable value in the partner system, drawn from Sections C (Detailed Rules), D (Formulas), and E (Edge Cases). Each knob is presented with its MVP default, the safe range within which it can be adjusted without breaking system contracts, the player-facing gameplay aspect it affects, the formula or rule it drives, and tuning guidance for what happens when the value is increased or decreased.

### G.1 Master Knobs Table

| # | Knob Name | MVP Default | Safe Range | What It Affects | Linked Formula/Rule | Tuning Guidance |
|---|-----------|-------------|------------|-----------------|---------------------|-----------------|
| K1 | `MVP_CONFIDENCE_MAX` | 66 | 20–66 | How reliable the cat's sniff results are relative to the player's own verification. Controls whether the cat can ever produce "authoritative" knowledge. | F1 `confidence_clamp`, R6 step 2, R8 | **Increase toward 66**: Cat's clues feel more trustworthy; more locations start at `可靠` tier. **Decrease toward 20**: Cat's clues feel increasingly vague; more locations start at `不确定`, requiring more player verification trips. **Never set above 66 in MVP** — this would allow the cat to bypass P1 (player must verify) and P4 (mild pressure from unknown). Raising this knob is the primary Post-MVP lever if human partners are introduced who should be more authoritative than the cat. |
| K2 | `NAMING_SKIP_MAX` | 3 | 1–5 | How many times the player can postpone naming the cat before the system locks in the default name "那只猫". Controls the tension between player agency and narrative commitment. | F2 `naming_prompt_eligibility`, R4, E.1.b | **Increase**: More opportunities to postpone; naming prompt becomes a recurring popup that may feel like nagging. **Decrease**: Fewer chances; player may feel rushed into naming before they are emotionally ready. At 1, the player gets one "maybe later" and then the default locks in — this raises the stakes of each skip. At 0, the naming prompt cannot be skipped — this violates R4's intent of player opt-out. |
| K3 | `NEST_CAPACITY` | 4 | 2–8 | How many objects accumulate in the cat's nest before it is "full." Controls the visible duration of the nest progression arc and the number of successful sniffs needed to see the complete nest. | R11, E.3.b, nest trace state machine | **Increase**: Longer nest progression arc; more successful sniffs needed to reach `full` stage. If set above the total number of unique sniff-signature items in the game, the nest can never reach `full` — check against content pipeline item count. **Decrease**: Shorter progression; nest fills quickly and may lose the sense of gradual accumulation. At 1, the nest is binary (exists / doesn't exist) and the `accumulating` stage is skipped entirely. |
| K4 | `PARTNER_NAME_LEN_MAX` | 8 | 4–12 | Maximum characters in the cat's name. Affects UI layout (name must fit in hint text, panel headers, and potential future dialogue lines) and player expression. | R4, E.1.d | **Increase**: More expressive names possible; risks UI overflow in narrow HUD elements. **Decrease**: Tighter UI fit; may frustrate players who want longer names. Below 4 becomes severely restrictive. The 8-character limit was chosen to accommodate most Chinese given names + single-character suffix (e.g., "小灰灰") and short English names. |
| K5 | `PARTNER_NAME_LEN_MIN` | 1 | 1–2 | Minimum characters in the cat's name. Prevents empty-string names (already blocked by validation at E.1.c) and, at higher values, blocks single-character names. | R4, E.1.c | **At 1**: Single-character names allowed (e.g., "云", "M"). **At 2**: Single-character names blocked — player must enter at least two characters. Setting this above 2 would block legitimate two-character names; not recommended. This knob is primarily a defense against the empty-string edge case (E.1.c already catches that); its secondary purpose is cultural — some writing systems have meaningful single-character names. |
| K6 | `T_sniff_lockout` | 2.5 | 1.5–4.0 | Minimum duration (seconds) the cat stays in `sniffing` state, preventing the player from initiating another sniff. Controls the felt weight and deliberateness of each sniff interaction. | R6, R7 sniff reaction animations, E.2.h state gate | **Increase**: Each sniff feels more weighty and ceremonial; repeated sniffing of multiple items becomes slower and may frustrate players who want to process inventory quickly. **Decrease**: Sniff interactions feel snappier; risk of the cat's reactions feeling perfunctory or glitchy if too short (below 1.5s, animations may not complete before the next sniff is available). This knob should be tuned alongside actual animation durations — the lockout must be at least as long as the longest R7 reaction animation. |
| K7 | `T_cat_state_cooldown` | 0.5 | 0.2–1.5 | Minimum dwell time (seconds) before the cat can transition to a new non-forced state. Prevents visual jitter when the player rapidly crosses zone boundaries. Also governs the post-arrival settle delay before normal state evaluation resumes (Hub state table, E.4.c). | E.4.b, E.4.c, Hub state table (arrival row) | **Increase**: Cat feels more deliberate and unhurried; transitions are visibly smoother. At values above 1.5s, the cat feels slow to respond to player movement — the companion illusion weakens. **Decrease**: Cat reacts faster to player movement; below 0.2s, zone-boundary spam causes visible jitter (cat flickering between states). This is a visual polish knob; the state machine correctness is unaffected by its value. |
| K8 | `T_nest_settle` | 20.0 | 10.0–60.0 | Idle time (seconds) the cat waits in `idle_living_quarters` before autonomously moving to `in_nest`. Controls how often the player sees the cat retreat to its nest vs. remaining in the shared living quarters space. | State machine: `idle_living_quarters → in_nest` transition, E.4.c, E.4.d | **Increase**: Cat stays in the shared space longer; nest behavior becomes rarer. At very high values (120s+), playtesters may never observe the nest transition and report it as missing content. **Decrease**: Cat retreats to nest more readily. Below 10s, the cat may enter the nest while the player is still actively moving around the living quarters — this reads as the cat "hiding from" rather than "coexisting with" the player, contradicting Player Fantasy layer 1 (归港时有人在). |

### G.2 Values Not Exposed as Knobs

The following values appear in the GDD but are intentionally fixed — they are design invariants, not tuning parameters:

| Value | Why Fixed |
|-------|-----------|
| Nest item list (4 static objects, indices 0–3) | Content identity — the specific objects carry narrative meaning (群体记忆). Changing them requires content authoring, not numeric tuning. |
| R7 reaction-to-animation mapping (5 entries) | The mapping from sniff outcome to animation is a qualitative design decision. Changing which animation plays for which condition is a design change, not a tuning adjustment. |
| `partner.sky-cat` as sole MVP partner | Hard constraint per R15.5 and CD direction. Adding partners requires system redesign, not tuning. |
| R2 presence contract (cat always on ship) | Invariant. Making this tunable would create a state where the cat is absent — a different system design entirely. |
| `query_partner_present()` always returns `true` | Derives from R2. Not a tunable behavior. |
| Nest irreversibility (stages never regress) | Design invariant from Player Fantasy layer 3 (它会记得你). Making this reversible would contradict the "traces are permanent" contract. |
| Naming irreversibility (name never changes) | Design invariant. Allowing renames would undermine the narrative weight of the naming moment. |

### G.3 Tuning Philosophy

The partner system's tuning philosophy follows three principles derived from its Pillar alignment:

**1. The cat's unreliability is a feature, not a calibratable flaw.** The `MVP_CONFIDENCE_MAX = 66` hard cap is the system's most load-bearing knob because it enforces P1 (player must verify) and P4 (mild pressure from unknown). Raising it would convert the cat from a scout companion into a spoiler dispenser. This knob should be the last thing anyone touches during tuning — and likely never in MVP.

**2. Timing knobs serve presence, not efficiency.** `T_sniff_lockout`, `T_cat_state_cooldown`, and `T_nest_settle` all govern how the cat occupies time and space on the airship. Their tuning target is not "fastest possible interaction" but "the cat feels like it lives here, at its own pace, not at yours." When playtesters say the cat feels "too slow," first check whether they mean the animations drag (adjust `T_sniff_lockout`) or the cat feels unresponsive to their presence (adjust `T_cat_state_cooldown`). These are different complaints with different knobs.

**3. The nest is a slow burn, not a progression bar.** `NEST_CAPACITY = 4` was chosen to span roughly the first third of the MVP content — enough successful sniffs that the player has formed a relationship before the nest completes, but few enough that reaching `full` is achievable in a normal play session. If content pipeline expands significantly Post-MVP, `NEST_CAPACITY` should be the first knob re-evaluated — but the nest item list (indices 0–3) carries specific narrative load and cannot simply be padded with generic objects.

**General tuning workflow**: Start from the MVP defaults. Run 3–5 playtest sessions observing: (a) how many successful sniffs before the player names the cat, (b) whether the nest reaches `accumulating` or `full` in a typical session, (c) whether players attempt to sniff a second item before the first animation completes (indicating `T_sniff_lockout` is too long), and (d) whether players report the cat "disappearing" (indicating `T_nest_settle` is too short — the cat retreated while they were still present). Adjust one knob at a time; re-test before touching another.

## Acceptance Criteria

### H.1 Core Scout Loop

**AC-SNIFF-01: Successful sniff calls reveal_rumor() with clamped confidence**

- **Assertion**: When `scout_sniff(item_id)` is called with an item that has a valid `cat_sniff_signature` and has not been previously sniffed, then `reveal_rumor()` is called exactly once with: `location_id = item.cat_sniff_signature.reveal_target`, `source_tag = "partner.sky-cat"`, `hazard_tags = [item.cat_sniff_signature.hazard_hint]`, `confidence = min(item.cat_sniff_signature.confidence, 66)`.
- **Verification**: Unit Test (mock #6 interface, assert call parameters)
- **References**: R6 steps 1-3, F1 confidence_clamp, R8
- **Pass**: `reveal_rumor()` called with clamped confidence. **Fail**: `reveal_rumor()` not called, called with raw confidence > 66, or called multiple times.

**AC-SNIFF-02: Duplicate sniff rejected with no side effects**

- **Assertion**: When `scout_sniff(item_id)` is called where `item_id` already exists in `sniffed_items`, then `scout_sniff()` returns `Null`, the "already smelled" animation (ears relax and flatten down) is selected, and neither `reveal_rumor()` nor `report_observation_event()` is called. No `nest_token` is produced. `item_id` is not re-added to `sniffed_items` (set no-op).
- **Verification**: Unit Test (pre-populate `sniffed_items`, call `scout_sniff`, verify no external API calls, no state mutation)
- **References**: R6 step 1, R7 "already smelled" row, E.2.c
- **Pass**: function returns `Null`, zero external API calls, zero nest update. **Fail**: `reveal_rumor()` called on duplicate, or `item_id` added twice.

**AC-SNIFF-03: Confidence clamped at MVP_CONFIDENCE_MAX = 66 (Formula F1)**

- **Assertion**: For any item `cat_sniff_signature.confidence` value, the confidence passed to `reveal_rumor()` is `min(raw_confidence, 66)`. Parameterized test cases: raw 0 yields 0, raw 30 yields 30, raw 66 yields 66, raw 67 yields 66, raw 90 yields 66, raw 100 yields 66.
- **Verification**: Unit Test (parameterized: 6 inputs, 6 expected outputs)
- **References**: F1 `confidence_clamp`, R8, Section D
- **Pass**: all 6 parameterized cases produce correct clamped output. **Fail**: any raw value > 66 passed through unclamped.

**AC-SNIFF-04: Null or invalid signature triggers confused animation only**

- **Assertion**: When `scout_sniff(item_id)` is called and `item.cat_sniff_signature` is `null`, OR `cat_sniff_signature.reveal_target` is null, empty, or whitespace-only, then the confused animation plays (ears forward, head tilts left-right, cat walks away -- per R14). `scout_sniff()` returns `Null`. No `reveal_rumor()` call. No `report_observation_event()` call. Item is NOT added to `sniffed_items`. No `nest_token` produced.
- **Verification**: Unit Test (two test cases: null signature item, valid structure with empty reveal_target; assert zero side effects for both)
- **References**: R6 step 2, R7 "confused" row, R14, E.2.d, E.2.f
- **Pass**: function returns `Null` with no external calls, no state mutation, for both cases. **Fail**: item added to `sniffed_items`, or `reveal_rumor()` called.

**AC-SNIFF-05: Successful sniff adds item_id to sniffed_items**

- **Assertion**: When `scout_sniff(item_id)` completes successfully (valid signature, not previously sniffed), then `item_id` is added to the `sniffed_items` persistent set. A subsequent `scout_sniff()` for the same `item_id` triggers duplicate rejection (AC-SNIFF-02).
- **Verification**: Unit Test (sniff item A once, assert A in `sniffed_items`; sniff item A again, assert duplicate rejection)
- **References**: R6 step 4, E.2.c
- **Pass**: first call adds to set, second call rejected. **Fail**: item not added on success, or added on duplicate call.

**AC-SNIFF-06: report_observation_event() called on successful sniff**

- **Assertion**: When `scout_sniff(item_id)` completes successfully, then `report_observation_event(item.cat_sniff_signature.pattern_id, "partner_sniff_success")` is called exactly once.
- **Verification**: Unit Test (mock #6 interface, verify call with correct `pattern_id` and `event_type`)
- **References**: R6 step 3, Interactions table row #6
- **Pass**: `report_observation_event()` called with correct parameters exactly once. **Fail**: not called, called with wrong `event_type`, or called on duplicate/null-signature sniff.

**AC-SNIFF-07: Sniff panel filters to items with cat_sniff_signature only**

- **Assertion**: When player opens the sniff panel (Use on `partner_station` interactable), the displayed item list contains only inventory items where `cat_sniff_signature != null`. Items lacking the field are excluded. Empty-state view displayed if zero eligible items.
- **Verification**: Integration Test (inventory with mixed items: 2 with signature, 3 without; open panel; assert exactly 2 items displayed)
- **References**: R5, E.2.a, E.2.b, Interactions table row #16
- **Pass**: only signature-carrying items shown; empty-state renders for zero-eligible case. **Fail**: unsigned items appear, or panel crashes with empty inventory.

**AC-SNIFF-08: Sniff state gate prevents concurrent sniffing attempts**

- **Assertion**: While the cat is in `sniffing` state (previous sniff animation in progress), any subsequent `scout_sniff()` call for any item is rejected. No data mutation occurs. No second `reveal_rumor()` call is made.
- **Verification**: Unit Test (call `scout_sniff(item_A)`, assert cat state = `sniffing`; call `scout_sniff(item_B)` before animation completes, assert rejection)
- **References**: E.2.h state gate, State machine `sniffing` row, K6 `T_sniff_lockout`
- **Pass**: second call rejected while in `sniffing`. **Fail**: second call processes normally, or data mutated.

### H.2 Naming System

**AC-NAME-01: Naming prompt triggers on first return-to-hub after first successful sniff**

- **Assertion**: When `sniff_success_occurred = true` AND `naming_state = pending` AND `player_returned_to_hub()` fires AND `naming_skip_count < NAMING_SKIP_MAX` (3), then naming UI opens and `naming_state` transitions to `prompted`.
- **Verification**: Unit Test (simulate successful sniff, set `naming_state = pending`, fire `player_returned_to_hub`, assert `naming_state = prompted`, naming UI triggered)
- **References**: F2 `naming_prompt_eligibility`, R3, Naming state machine
- **Pass**: naming UI opens exactly once on the qualifying return. **Fail**: UI does not open, opens without sniff success, or opens multiple times.

**AC-NAME-02: Naming never triggers without a successful sniff**

- **Assertion**: When the player returns to hub multiple times but `sniff_success_occurred` remains `false` (no successful sniff ever occurred), `naming_state` stays `pending` permanently. Naming UI never opens. No default name is assigned.
- **Verification**: Unit Test (fire `player_returned_to_hub` 10 times with `sniff_success_occurred = false`, assert `naming_state` still = `pending`, `name` unset, no UI trigger)
- **References**: R3, F2, E.1.a
- **Pass**: `naming_state` stays `pending` across all returns. **Fail**: naming triggers without sniff.

**AC-NAME-03: Three skips lock default name "那只猫" silently**

- **Assertion**: When `naming_skip_count` reaches 3 (= `NAMING_SKIP_MAX`), on the next `player_returned_to_hub()` the system writes `name = "那只猫"`, sets `naming_done = true`, and transitions `naming_state` to `completed`. No naming UI opens. The transition is silent (no notification). All subsequent UI displays "那只猫".
- **Verification**: Unit Test (simulate 3 skips, fire `player_returned_to_hub`, assert `name = "那只猫"`, `naming_done = true`, `naming_state = completed`, no UI open)
- **References**: R4, F2, E.1.b, Naming state machine
- **Pass**: default name locked silently; UI never reopens. **Fail**: UI opens on 4th trigger, or name not written, or notification popup appears.

**AC-NAME-04: Valid name submission completes naming irreversibly**

- **Assertion**: When player submits a name of 1-8 non-whitespace characters, then `partner_skycat.name = submitted_name` (trimmed), `naming_done = true`, `naming_state = completed`. Naming UI never reopens. Name persists across save/load without any rename path.
- **Verification**: Unit Test (submit valid name, assert state = `completed`; fire `player_returned_to_hub`, assert no naming trigger; save/load, assert name unchanged; search codebase, assert no rename function exists)
- **References**: R3, R4, Naming state machine, G.2 naming irreversibility
- **Pass**: name locked, state terminal, no rename path. **Fail**: naming retriggers, name changes on reload, or rename function found.

**AC-NAME-05: Empty or whitespace-only name rejected without consuming a skip**

- **Assertion**: When player submits `""` or `"   "` (whitespace-only), the naming UI (owned by #16) rejects the submission with a validation hint. `naming_skip_count` is NOT incremented. `naming_state` stays `prompted`. UI remains open. Player must enter a valid name or explicitly skip.
- **Verification**: Integration Test (open naming UI, submit empty string, assert reject hint visible, skip_count unchanged, UI still open)
- **References**: R4, E.1.c, K4, K5
- **Pass**: rejection shown, skip count unchanged, UI responsive. **Fail**: skip count increments, empty name stored, or UI closes.

**AC-NAME-06: Stored name never exceeds 8 characters**

- **Assertion**: After any name write operation, `partner_skycat.name.length <= 8`. The system safety-net clamps to 8 characters even if UI validation (primary enforcement) is bypassed.
- **Verification**: Unit Test (attempt to write a 12-character name via internal API, assert stored value length <= 8)
- **References**: R4, E.1.d, K4 `PARTNER_NAME_LEN_MAX`
- **Pass**: stored name always <= 8 chars. **Fail**: 9+ character name persisted.

### H.3 Nest Accumulation

**AC-NEST-01: First successful sniff transitions nest from empty to first**

- **Assertion**: When `nest_state = empty` and `scout_sniff(item_id)` succeeds producing `nest_token = true`, then `nest_items = [0]` (the 旧船帆碎布), `nest_state = first`. Hub `query_nest_state()` returns `first`.
- **Verification**: Unit Test (start empty nest, execute successful sniff, assert nest_state = `first`, nest_items contains exactly index 0)
- **References**: R11 nest items table index 0, Nest trace state machine, E.3.a
- **Pass**: nest transitions to `first` with exactly one item (index 0). **Fail**: nest stays `empty`, or wrong item index stored.

**AC-NEST-02: Second and third successful sniffs accumulate progressively**

- **Assertion**: Each subsequent successful sniff appends the next fixed-index item until cap. After 2nd sniff: `nest_items = [0, 1]`, `nest_state = accumulating`. After 3rd sniff: `nest_items = [0, 1, 2]`, `nest_state = accumulating`.
- **Verification**: Unit Test (execute 3 successful sniffs sequentially, assert after each: size is index+1, item indices match static list, state is `accumulating` for sizes 2-3)
- **References**: R11 nest items table (indices 1, 2), Nest trace state machine `accumulating` row
- **Pass**: items accumulate in fixed order, state stays `accumulating`. **Fail**: wrong item index appended, or state doesn't transition to `accumulating`.

**AC-NEST-03: Fourth successful sniff fills nest; further sniffs add nothing**

- **Assertion**: After 4th successful sniff: `nest_items = [0, 1, 2, 3]`, size = 4 = `NEST_CAPACITY`, `nest_state = full`. Subsequent successful sniffs: size remains 4, state remains `full`, no items appended.
- **Verification**: Unit Test (execute 5 successful sniffs, assert size = 4 after 4th, size = 4 after 5th, state = `full`)
- **References**: R11, Nest trace state machine `full` row, E.3.b, K3 `NEST_CAPACITY`
- **Pass**: cap at 4, no overflow. **Fail**: 5th+ item appended, state not `full` after 4 items.

**AC-NEST-04: Nest items are irreversible across all operations**

- **Assertion**: Once nest has N items (N > 0), `nest_items.size()` never decreases for the lifetime of the save. No operation removes, rearranges, or replaces items. The first N items always match indices [0..N-1] from the static list (旧船帆碎布, 锈蚀的测风链环, 玩家绳头, 空港徽章残片). Tested across: save/load, duplicate sniffs (no-op), sniffs beyond cap (no-op), Hub state transitions.
- **Verification**: Unit Test (accumulate 3 items; save/load, assert size = 3 and indices [0,1,2]; sniff duplicate, assert size still = 3; sniff beyond cap, assert size still = 3 or = 4 if cap reached; transition all Hub states, assert size never decreases)
- **References**: R11 irreversibility clause, Nest trace state machine "无效转换" rule, E.3.c, E.3.d, G.2 nest irreversibility
- **Pass**: size monotonic non-decreasing, indices preserved across all operations. **Fail**: any item removed, index shifted, or size decreased.

**AC-NEST-05: Zero successful sniffs means nest stays empty permanently**

- **Assertion**: When `sniff_success_occurred` remains `false` for the entire session, `nest_state = empty`, `nest_items = []`, and Hub renders no nest visual anchors.
- **Verification**: Unit Test (initialize system, play through session without any `scout_sniff()` call or with only null-signature items, assert nest_state = `empty`, nest_items empty array)
- **References**: E.3.a, Nest trace state machine
- **Pass**: nest stays dormant. **Fail**: nest items appear without successful sniff trigger.

### H.4 Cat Presence and State Machine

**AC-CAT-01: query_partner_present() returns true in all Hub states**

- **Assertion**: `query_partner_present()` returns `true` unconditionally for all Hub states: `landed`, `departure_locked`, `in_transit`, `arrival`. This is an invariant (R2); no state path produces `false`.
- **Verification**: Unit Test (call `query_partner_present()` for each Hub state enum value, assert all return `true`)
- **References**: R2, Hub state table, E.6.a
- **Pass**: all 4 Hub states return `true`. **Fail**: any state returns `false`.

**AC-CAT-02: New game initialization places cat at intel station**

- **Assertion**: On new game, when Hub = `landed` and the player has not yet moved to another zone, cat state = `sleeping_on_intel_station`, visual position = 驾驶舱情报台.
- **Verification**: Integration Test (start new game, verify cat spawned at intel station in sleeping pose animation)
- **References**: State machine `sleeping_on_intel_station` row, R2
- **Pass**: cat visible at correct location with sleeping pose. **Fail**: cat absent, at wrong location, or in wrong state.

**AC-CAT-03: Zone-based state transitions follow state machine table**

- **Assertion**: Cat state transitions match the state machine table exactly:
  - `sleeping_on_intel_station` + player enters 生活舱 -> `idle_living_quarters`
  - `idle_living_quarters` + player moves toward workbench -> `following_player_to_bench` -> `bench_adjacent` (on arrival)
  - `bench_adjacent` + player leaves workbench reach_limit -> `idle_living_quarters`
  - `idle_living_quarters` + idle > `T_nest_settle` -> `in_nest`
  - `in_nest` + player enters 生活舱 trigger radius -> `idle_living_quarters`
- **Verification**: Integration Test (simulate each trigger condition in sequence, assert cat state updates to correct target per table, assert no invalid transitions occur)
- **References**: Cat runtime state machine table, E.4.b, E.4.c, E.4.d, K7 `T_cat_state_cooldown`, K8 `T_nest_settle`
- **Pass**: all tested transitions match the state machine table. **Fail**: any transition targets wrong state, or invalid transition fires.

**AC-CAT-04: departure_locked freezes cat state**

- **Assertion**: When Hub fires `hub_state_changed(departure_locked)` while cat is in any non-forced state, cat state freezes. No further state transitions are processed. Player input to partner station is ignored.
- **Verification**: Integration Test (place cat in `bench_adjacent`, fire `departure_locked`, trigger zone transition event, assert cat stays `bench_adjacent`; attempt to interact with partner station, assert rejected)
- **References**: Hub state table `departure_locked` row
- **Pass**: cat motionless during lockout, interaction rejected. **Fail**: cat moves or accepts sniff during departure lock.

**AC-CAT-05: arrival forces cat to idle_living_quarters**

- **Assertion**: When Hub transitions to `arrival` state (post-transit), cat state is forced to `idle_living_quarters` regardless of pre-departure state. After `T_cat_state_cooldown` (0.5s), normal transition evaluation resumes. Cat is NOT at the entrance (R13).
- **Verification**: Integration Test (set pre-departure cat state to `in_nest`, simulate transit, fire arrival, assert cat state = `idle_living_quarters`, assert cat visual position = 生活舱暖光区)
- **References**: Hub state table `arrival` row, R13, E.4.c, K7 `T_cat_state_cooldown`
- **Pass**: cat in living quarters after arrival. **Fail**: cat at wrong location, or state not forced to `idle_living_quarters`.

**AC-CAT-06: in_transit uses simplified simulation**

- **Assertion**: During `in_transit`, cat is not rendered and not interactable. Logical state stored as `idle_living_quarters` (for save purposes). `query_partner_present()` still returns `true`.
- **Verification**: Integration Test (trigger departure, enter `in_transit`; assert cat not visible in scene, partner station not interactable, `query_partner_present()` still returns `true`)
- **References**: Hub state table `in_transit` row, R2, E.6.a
- **Pass**: cat invisible and non-interactable during transit, presence still `true`. **Fail**: cat rendered mid-flight, station interactable, or presence reports `false`.

### H.5 Boundary Guards (R15 — 6 Hard Prohibitions)

**AC-GUARD-01: No affection/friendship/bond field exists in data model**

- **Assertion**: The partner system's data model (`progress.partner_skycat` snapshot and all runtime data structures) contains zero fields named `affection`, `friendship`, `bond`, `relationship_level`, or any semantically equivalent key.
- **Verification**: Unit Test (inspect serialized save snapshot schema and runtime data object; assert no forbidden field keys exist)
- **References**: R15.1, G.2 "Values Not Exposed as Knobs" (no affection)
- **Pass**: zero affection-like fields found. **Fail**: any affection-like field name detected.

**AC-GUARD-02: No gift-giving entry point exists**

- **Assertion**: `scout_sniff` is the sole item interaction pathway for the cat. There is no "give gift," "offer item," "present to cat," or any non-sniff item handover interface. Items given to the cat for sniffing are never consumed or removed from inventory.
- **Verification**: Integration Test (exhaustively test all cat interaction points, verify only sniff panel opens; verify inventory unchanged after sniff)
- **References**: R15.2, R5, E.2.c (items never consumed)
- **Pass**: `scout_sniff` is the only item interaction; items never consumed. **Fail**: any gift/donate/consume path found; inventory item removed after sniff.

**AC-GUARD-03: No event tree or dialogue branch data structures**

- **Assertion**: Cat behavior is driven exclusively by the runtime state machine (Hub state + player zone events) and sniff interactions. No event trigger condition, story flag, dialogue tree node, or branching narrative structure references the partner system to alter cat behavior.
- **Verification**: Unit Test (inspect partner system code/data, assert zero event tree structures, zero dialogue node references, zero story flag conditions)
- **References**: R15.3
- **Pass**: zero event tree or dialogue references in partner system. **Fail**: any story node, conversation branch, or plot flag condition found.

**AC-GUARD-04: No timer-based partner events or rewards**

- **Assertion**: In any Hub state, no elapsed-time trigger produces a partner-specific reward, event, state mutation, or content unlock. Cat behavior is purely event-driven: zone entry/exit, Hub state changes, sniff initiation -- never `delta_time > threshold`.
- **Verification**: Unit Test (inspect partner system for any `_process(delta)` or timer callback that mutates partner state or produces rewards; assert none exist beyond animation playback)
- **References**: R15.4, State machine (all transitions are event-triggered)
- **Pass**: zero delta-time-based partner events or rewards. **Fail**: any timer-driven state change or item production.

**AC-GUARD-05: partner.sky-cat is the sole MVP partner entity**

- **Assertion**: After game initialization (new game or loaded save), exactly one partner entity exists with `partner_id = "partner.sky-cat"`. No other partner entities can be created, and the initialization path is hardcoded to this id. Attempting to instantiate any other partner id is an invalid operation.
- **Verification**: Unit Test (query all active partners after init, assert count = 1, assert id = "partner.sky-cat"; verify no partner factory or spawner accepts other ids)
- **References**: R15.5, R1 "partner.sky-cat is sole MVP partner"
- **Pass**: exactly one partner, correct id. **Fail**: multiple partners, or wrong/non-existent id.

**AC-GUARD-06: No recruit or dismiss mechanic**

- **Assertion**: From new game to session end, `on_partner_joined("partner.sky-cat")` fires exactly once (bootstrap). There is no function to recruit a new partner, dismiss the cat, send the cat away, or re-recruit it. The cat cannot be removed, replaced, or re-added. R2's presence contract is the invariant: the cat is always there.
- **Verification**: Unit Test (search partner system API for any join/leave/recruit/dismiss/remove/add function beyond the one-time bootstrap; assert none exist; assert `on_partner_joined` fires exactly once per session)
- **References**: R15.6, R2, E.6.d
- **Pass**: zero recruit/dismiss API, bootstrap call fires once. **Fail**: any remove, re-add, or multi-call join function found.

### H.6 Persistence

**AC-SAVE-01: All seven owned data fields survive save/load roundtrip**

- **Assertion**: After save/load, all seven persisted fields of `progress.partner_skycat` match pre-save values exactly: `name`, `naming_done`, `naming_skip_count`, `sniff_success_occurred`, `nest_state`, `nest_items[]` (order and content), `sniffed_items[]` (set content, order-independent).
- **Verification**: Integration Test (perform multiple sniffs, name cat, accumulate 2 nest items; save; force full reload; assert all six fields equal pre-save values)
- **References**: Interactions table row #3, F.4 save/load data ownership table, E.3.c
- **Pass**: all seven fields identical post-load. **Fail**: any field value diverges from pre-save state.

**AC-SAVE-02: Transient fields NOT persisted -- re-derived on load**

- **Assertion**: When the game is saved while cat is in `sniffing`, `bench_adjacent`, or any non-default state, and then loaded, the cat's state is derived from Hub context (per E.4.a rules), NOT restored to the transient saved state. The sniff animation does not resume mid-sequence.
- **Verification**: Integration Test (save during `sniffing` animation; load; assert cat state is one of {`sleeping_on_intel_station`, `idle_living_quarters`} per E.4.a derivation rules, NOT `sniffing`)
- **References**: E.4.a, Interactions table transient fields note, F.4 save/load notes
- **Pass**: cat in derived initial state on load, not transient state. **Fail**: cat loads in exact pre-save transient state.

**AC-SAVE-03: Naming state with partial skips survives save/load correctly**

- **Assertion**: When saved with `naming_state = prompted`, `naming_skip_count = 2`, on load the system preserves both values. The next `player_returned_to_hub()` triggers the naming UI (one remaining chance). Skip count is not reset.
- **Verification**: Integration Test (skip naming twice; save; load; fire `player_returned_to_hub`; assert naming UI opens; complete skip; assert skip_count = 3, naming_state = completed, name = "那只猫")
- **References**: E.1.e, F.4 save/load table
- **Pass**: skip count preserved across load, remaining chances correct. **Fail**: skip count reset to 0, or naming UI never retriggers.

**AC-SAVE-04: Partial nest state restored exactly on load**

- **Assertion**: When saved with `nest_items = [0, 1]` (size = 2, state = `accumulating`), on load `nest_items = [0, 1]` (same indices, same order), `nest_state = accumulating`. Hub renders visual anchors for indices 0 and 1 only.
- **Verification**: Integration Test (accumulate 2 nest items; save; load; assert nest_items = [0, 1], nest_state = accumulating; verify Hub renders 2 visual anchors)
- **References**: E.3.c, Nest trace state machine, Interactions table row #7
- **Pass**: exact restoration of partial nest with correct items and visual anchors. **Fail**: items missing, reordered, wrong indices, or wrong state.

**AC-SAVE-05: sniffed_items set survives roundtrip with intact duplicate protection**

- **Assertion**: After sniffing items A, B, C; saving; and loading -- `sniffed_items` contains exactly {A, B, C}. Sniffing A, B, or C again triggers duplicate rejection (AC-SNIFF-02). Previously sniffed items are not forgotten across sessions.
- **Verification**: Integration Test (sniff items A, B, C; save; load; attempt to sniff A, B, C again; assert all three rejected as duplicates; sniff new item D; assert D succeeds)
- **References**: Interactions table row #3, E.2.c, AC-SNIFF-02
- **Pass**: duplicate protection preserved across sessions; new items still sniffable. **Fail**: previously sniffed items accepted as new, or new items rejected.

### H.7 Edge Case Coverage

**AC-EDGE-01: Rapid sniff spam rejected by state gate**

- **Assertion**: When the player rapidly triggers `scout_sniff()` multiple times (e.g., UI spam or debug command) before the first animation completes, only the first call processes. Subsequent calls while `cat.state = sniffing` are rejected. Exactly one `reveal_rumor()` call occurs. Exactly one `item_id` is added to `sniffed_items`.
- **Verification**: Integration Test (rapid-fire 5 `scout_sniff()` calls with different valid items in < 1 second; assert exactly 1 successful sniff, 4 rejections)
- **References**: E.2.h, K6 `T_sniff_lockout`, State machine `sniffing` row
- **Pass**: exactly 1 successful sniff, all duplicates rejected. **Fail**: multiple successful sniffs from spam, or data corruption.

**AC-EDGE-02: reveal_rumor() failure degrades gracefully**

- **Assertion**: When `reveal_rumor()` call to system #6 fails (error, timeout, or #6 unavailable), the partner system logs a warning (dev build only), treats the sniff as locally successful, and does NOT retry. `item_id` is added to `sniffed_items`. `nest_token` is processed normally. Reaction animation plays. No crash.
- **Verification**: Unit Test (mock #6 `reveal_rumor()` to throw error; call `scout_sniff()`; assert no exception propagates; assert `item_id` in `sniffed_items`; assert nest accumulated if applicable)
- **References**: E.5.a
- **Pass**: local state correct despite #6 failure, no unhandled exception. **Fail**: system crash, local state not updated, or infinite retry loop.

**AC-EDGE-03: Zone boundary spam prevented by state cooldown**

- **Assertion**: When the player rapidly enters and leaves 生活舱 zone boundary 5 times in 1 second, the cat transitions at most 2 times (cooldown period = `T_cat_state_cooldown` = 0.5s). No visual jitter (state flickering) occurs.
- **Verification**: Integration Test (spam zone enter/leave events 5 times in 1 second; count cat state transitions; assert transitions <= 2)
- **References**: E.4.b, K7 `T_cat_state_cooldown`
- **Pass**: transitions rate-limited to cooldown. **Fail**: cat flickers through all 5 transitions visually.

**AC-EDGE-04: Multiple items to same reveal_target -- both independently processed**

- **Assertion**: When item A (`reveal_target = location.X`, `confidence = 30`) and item B (`reveal_target = location.X`, `confidence = 66`) are both sniffed, two separate `reveal_rumor()` calls are made to #6. Both item_ids are added to `sniffed_items`. The partner system does NOT query #6's current knowledge state before calling -- it always pushes. If both are first-time sniffs and nest not full, both may produce `nest_token`.
- **Verification**: Unit Test (sniff two items sharing same `reveal_target`; assert 2 `reveal_rumor()` calls, both in `sniffed_items`, both nest items accumulated if applicable)
- **References**: E.2.g
- **Pass**: both items processed independently without cross-check. **Fail**: second item rejected as "already known location."

**AC-EDGE-05: departure_locked during sniff animation -- data safe, animation may truncate**

- **Assertion**: When `scout_sniff()` data commit completes, then `departure_locked` fires during the animation playback: sniff data is ALREADY committed (`sniffed_items` updated, `reveal_rumor()` called, nest accumulated). Data integrity is preserved. The animation may truncate visually or complete naturally before the freeze takes effect -- either is acceptable.
- **Verification**: Integration Test (sniff item; at 0.5s into animation, fire `departure_locked`; assert `sniffed_items` contains item, nest updated if applicable, cat enters frozen state)
- **References**: E.2.i, Hub state table `departure_locked`
- **Pass**: data committed, no loss, cat frozen. **Fail**: sniff data lost, or system crash on state conflict.

**AC-EDGE-06: Save during sniffing animation -- data persisted, animation not resumed**

- **Assertion**: When sniff data is committed, then the game saves during animation playback, then loads: the sniff result is persisted in `sniffed_items` (and nest if applicable); system #6's knowledge store has the `reveal_rumor()` result. On load, cat state is re-derived per E.4.a rules -- animation does NOT resume mid-sequence. The in-progress animation is lost visually; this is acceptable.
- **Verification**: Integration Test (save mid-sniffing-animation; load; assert `sniffed_items` contains item; assert cat state is derived initial state per E.4.a, NOT `sniffing`)
- **References**: E.4.a
- **Pass**: data persisted, cat in derived state on load, no crash. **Fail**: cat stuck in `sniffing` on load, or sniff data lost.

**AC-EDGE-07: Initialization race -- Hub events fire before partner subscription**

- **Assertion**: When Hub (#7) loads and fires `hub_state_changed(landed)` before the partner system (#15) has subscribed, the partner system explicitly calls `sync_with_hub_state(current_hub_state)` after subscription. The cat's state is correct regardless of event delivery order.
- **Verification**: Integration Test (manipulate init order: Hub fires events first, then partner subscribes; after full init, assert cat state = `sleeping_on_intel_station` for `landed` Hub)
- **References**: E.6.c, F.3 initialization order, F.3 bootstrap race condition handling
- **Pass**: cat state correct regardless of init event ordering. **Fail**: cat in wrong/uninitialized state due to race condition.

**AC-EDGE-08: Naming modal blocks departure when open**

- **Assertion**: When the naming UI (modal) is open (`naming_state = prompted`), and the game attempts to fire `departure_locked` (player initiates departure), the departure animation is deferred. The naming modal remains open and functional. Departure proceeds only after the modal is resolved (named or skipped).
- **Verification**: Integration Test (open naming UI; trigger departure; assert departure deferred; close naming UI by naming; assert departure proceeds)
- **References**: E.1.g
- **Pass**: departure blocked by naming modal, naming still functional. **Fail**: departure proceeds with naming modal open, or naming modal becomes unusable.

### H.8 Pillar Alignment

**AC-PILLAR-01: Memorable identity beat -- naming is one-time, irreversible, player-initiated (CD constraint)**

- **Assertion**: The naming moment occurs exactly once in the game's lifetime: gated behind the cat proving itself (first successful sniff), triggered by the player's return to hub, and permanently stored. No rename option, no undo, no re-trigger. The name appears in all cat-related UI (hint text, sniff panel header) and persists across all sessions.
- **Verification**: Integration Test (complete full naming sequence: sniff once -> return -> name cat "小云"; verify name displayed in hint text at partner station; verify no rename UI path exists; save/load; verify name still "小云"; search all UI for rename affordance)
- **References**: R3, R4, Naming state machine, G.2 naming irreversibility, CD "memorable identity beat"
- **Pass**: name permanent, displayed consistently, no rename path, no re-trigger. **Fail**: rename UI found, name not displayed, or naming re-triggers.

**AC-PILLAR-02: Persistent relationship memory -- nest traces permanent across sessions (CD constraint)**

- **Assertion**: The cat's nest is a permanent, irreversible, cumulative record of the player and cat's shared history. Items accumulated in the nest are never lost, never regress, and never reset -- not across save/load, not across sessions, not through any player action. The nest IS the persistent relationship memory.
- **Verification**: Integration Test (accumulate 3 nest items over multiple sniffs; save; quit; restart; load; play 2 hours; save; quit; restart; load -- assert nest always has at least those 3 items in correct order; assert no "clear nest" or "reset nest" function exists in any system)
- **References**: R11 irreversibility, E.3.c, E.3.d, Nest trace state machine irreversibility, CD "persistent relationship memory", G.2 nest irreversibility
- **Pass**: nest items permanent across sessions, no regression, no reset path. **Fail**: any item missing after load, or any clear/reset function found.

**AC-PILLAR-03: Mild pressure from unknown -- cat knowledge never authoritative (Pillar 4)**

- **Assertion**: Every `reveal_rumor()` call from the partner system passes `confidence <= 66`. Since #6's authoritative threshold is `confidence >= 67`, no partner-originated knowledge can ever be `authoritative`. The cat gives clues, never certainties. The player must always verify.
- **Verification**: Unit Test (test all code paths that call `reveal_rumor()`; assert every call has `confidence <= 66`; verify with parameterized inputs covering raw confidence 0-100)
- **References**: F1 `confidence_clamp`, R8, P4 pillar, P1 verification, Section D confidence tier mapping (0-33, 34-66, 67-100)
- **Pass**: all `reveal_rumor()` calls clamped to <= 66. **Fail**: any call with confidence >= 67 discovered.

**AC-PILLAR-04: Passive presence -- cat never demands attention or interrupts player (Pillar 3 + Pillar 5)**

- **Assertion**: During any hub activity (workbench operation, chart planning, inventory management, departure prep), the cat exhibits idle behavior (R12) that is ambient and non-interruptive. The cat does NOT: pop up notification text, trigger quest prompts, request items unsolicited, block player movement paths, produce timed alerts, or penalize the player for ignoring it.
- **Verification**: Manual Playtest (play a 30-minute hub session across varied activities; record every cat-initiated interaction; rubric: zero interruptions = pass; any popup, forced prompt, or path blockage = fail)
- **References**: R12 idle behavior contract, R15.1 (no affection), R15.3 (no event tree), Player Fantasy layer 1 (归港时有人在), P3 pillar (飞艇是家)
- **Pass**: zero cat-initiated interruptions in >= 30 minute hub session. **Fail**: any cat-initiated popup, prompt, or movement block observed.

**AC-PILLAR-05: Airship is home -- cat presence makes hub feel lived-in (Pillar 3)**

- **Assertion**: Across multiple departure/arrival cycles, the cat is consistently present and animate during hub stays. The cat is not at the entrance on arrival (R13 -- in living quarters). The nest progresses if the player sniffs. The cat's idle behavior (moving between warm-light zone, intel station, nest) is observable. The airship demonstrably houses a living creature with its own rhythms, independent of player attention.
- **Verification**: Manual Playtest (play 3 full departure/arrival cycles; after each arrival, observe: (a) cat visible somewhere on ship, (b) cat not at entrance, (c) cat performs at least one idle behavior transition within 2 minutes of arrival, (d) nest reflects accumulated sniffs. Rubric: all 4 conditions met in >= 2 of 3 cycles = pass)
- **References**: R2 presence contract, R13 arrival behavior, R12 idle contract, Nest trace state machine, P3 pillar, Player Fantasy layers 1 and 2
- **Pass**: cat present and animate in >= 2 of 3 cycles, nest progression visible. **Fail**: cat missing, static, or nest broken in >= 2 cycles.

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Open Questions

[To be designed]
