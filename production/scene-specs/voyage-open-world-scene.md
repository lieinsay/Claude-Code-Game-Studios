# 航行大场景规格

> **Scene ID**: `voyage_open_world_scene`
> **Player-facing scene name**: 航行大场景
> **Status**: `spec_drafted`
> **Last Updated**: 2026-05-24
> **Source GDDs**: `design/gdd/navigation-route-risk.md`, `design/gdd/scene-composition-system.md`, `design/gdd/scene-physics-unit-system.md`

## 1. Scene Identity

- Purpose: 让当前 demo 的两条航道成为可玩、可读、可验收的航行场景，而不是航图确认后的等待条。
- Emotional target: 空海中的判断、轻压力、孤航感；玩家觉得自己在读风、读云、读风险，而不是被随机事件扣血。
- Core fantasy / pillars served: 规划先于冒险；未知带来温和压力；飞艇是家，不只是载具。
- What the player should understand within 3 seconds: 我正在驾驶云织号穿过空海，前方有航道、云层、航标、目的地方向和可判断的风险。
- What this scene is not: 不是平面地图 UI，不是单纯进度条，不是主动开火射击战，也不是真实 3D 飞行模拟。

## 2. Scene Physics Contract

| Field | Value |
| --- | --- |
| Physics source | design spec |
| Contract scene ID | `voyage_open_world_scene` |
| `physics_contract_complete` status | pending |
| Scene physics type | `水平场景` with pseudo-3D presentation |
| Movement plane | 飞船在航道平面内前进、后退、转向；玩家视角与当前前进方向一致 |
| Layer / Height Model | 近景云雾 / 中景航标和残骸 / 远景目的地轮廓 / UI overlay 分层 |
| Cutaway / Reveal Model | N/A true；无建筑剖切，但云雾和大型生物遮挡必须保留航向与飞船可读性 |
| Unit catalog | 飞船前景轮廓、云层、航标、漂浮残骸、浓雾带、飞行大鸟、目的地轮廓；后续可扩展风暴边缘、空盗追击影、失控无人浮标 |
| Collision / occlusion / scale | pending；残骸、浓雾边界、大鸟靠近/离开路径、云层遮挡需要可读边界 |
| Special surfaces / dynamic behaviors / recovery | 云雾、残骸、飞行大鸟临时避险、返航点标记；卡死或迷航时必须能撤退或回到最近安全航向 |
| Exemption reason, if no gameplay-relevant physical units | N/A |

## 3. Entry / Exit

- Entry source: 初始岛屿或船内航行准备完成后进入。
- Spawn / arrival position: 飞船进入航道起点，船首/视角对准已选航线方向。
- Exit or return path: 抵达 `mist_lamp_wreck_scene` 或 `old_market_edge_scene`；也可撤退返回初始岛屿/船内。
- Cancel / failure path: 航行中可撤退；船体或模块状态过差时可被迫改道或迫降，具体结算由 #10/#8/#11 后续定义。
- Saved-state return behavior: 航行中断应恢复至最近安全航向、当前风险状态和已揭示信息；不能恢复到无上下文的 UI 面板。
- Scene transition cleanup expectations: 离开航行大场景时清理临时大鸟遭遇、扫描、云雾遮挡和航向反馈，不清除已写入的航线知识或船体后果。

## 4. Spatial Layout

- Main viewport composition: 前景保留少量船首/舱窗/仪表边缘；中景显示航道、航标、残骸、云层；远景显示目的地轮廓、大鸟剪影或旧集市边缘方向。
- Walkable area: 飞船可控航道平面，表现为前进方向上的可读通路，而不是角色步行区域。
- Boundaries: 浓雾核心、漂浮残骸密集区、大鸟靠近警戒区、航道外深云区；风暴边缘和扫描锁定区为后续边界。
- Landmarks: 初始岛屿远离轮廓、雾灯残骸灯影、旧集市边缘轮廓、航标链、远处生物剪影。
- Interaction anchors: 航向、速度、撤退点、扫描/观察、云层临时避险、残骸穿缝、大鸟避让窗口。
- Occlusion risks: 浓雾和云层不能长时间遮住航向、目的地提示或飞船状态；遮挡必须服务风险读法。
- Minimum greybox readability requirement: 不用文字说明也能看出正在航行、正在接近目的地、前方风险可规避或可处理。

## 5. Critical Path

1. 玩家从初始岛屿/船内确认航线，进入航行大场景。
2. 玩家观察前方航道和风险信号，选择绕行、减速、转向、后退、扫描、等待或撤退。
3. 玩家处理至少一个航行问题后继续前进。
4. 玩家抵达雾灯残骸或旧集市边缘，或主动撤退返回。

## 5a. 单次航行节奏

航行大场景使用“阶段编排 + 问题窗口”，而不是把每次遭遇检查都表现成同等强度的随机事件。#10 的遭遇检查仍可作为后台风险解析，但玩家可见节奏应遵循下表。

| 阶段 | 航程百分比 | 目标感受 | 世界表现 | 玩家决策 |
| --- | --- | --- | --- | --- |
| 离港校准 | 0-15% | 从安全地带进入空海，确认航向 | 初始岛屿远离，航标链变清晰，云层速度稳定 | 调整航向、确认速度、选择是否扫描 |
| 第一问题窗口 | 15-40% | 低到中强度的航路判断 | 浓雾带或残骸群进入中景 | 绕行、减速、穿缝、扫描或等待 |
| 中段喘息 | 40-55% | 让玩家读懂处理结果，恢复方向感 | 风险物远离，目的地轮廓短暂显现 | 重新校准航向，决定继续或撤退 |
| 第二问题窗口 | 55-80% | 主要压力点，可能出现生态避险问题 | 飞行大鸟靠近、浓雾遮蔽或残骸阴影形成临时避险窗口 | 改航向、降速、借云雾/残骸避险或撤退 |
| 抵达压缩 | 80-100% | 目的地接近，最后一次判断 | 目的地轮廓放大，航道收窄，返航窗口变弱 | 稳定进入目的地，或在高风险下撤退 |

每趟航行最多同时突出一个主问题，允许一个低强度次问题作为背景压力。第一版玩家不应该在同一时间同时处理浓雾、残骸和大鸟靠近，否则航行会变成噪声而不是判断。

## 5b. Demo 航线编排

| 航线 | 目标时长 | 问题数量 | 必出问题 | 可选问题 | 节奏目标 |
| --- | --- | --- | --- | --- | --- |
| 初始岛屿 → 雾灯残骸 | 60-75s | 1 个主问题 + 1 个轻背景问题 | 浓雾带 或 漂浮残骸群 | 大鸟远景剪影/轻提示 | 教玩家“看前方、调航向、减速/绕行、抵达目的地” |
| 初始岛屿 → 旧集市边缘 | 110-140s | 2 个主问题 + 1 个轻背景问题 | 漂浮残骸群 + 飞行大鸟临时避险 | 浓雾带 | 测试完整航行判断：穿缝、借遮蔽避险、判断继续或撤退 |

第一版不要求每次航行都随机不同。为了可读性，demo 可以使用半固定编排：雾灯残骸线偏教学，旧集市边缘线偏完整压力。随机性只用于问题具体位置、持续时间或视觉变体，不改变玩家必须学习的核心动作。空盗、风暴边缘、失准航标和失控无人浮标不属于第一版硬范围。

## 5c. 问题处理窗口

| 问题 | 预警时间 | 主处理时间 | 成功判定 | 部分失败 | 彻底失败 |
| --- | --- | --- | --- | --- | --- |
| 浓雾带 | 4-6s | 8-12s | 降速/扫描/绕行后航标恢复，航向偏差可控 | 航程 +5-10s，短暂迷航 | 偏离航道，进入返航/重校准状态 |
| 漂浮残骸群 | 3-5s | 6-10s | 从缝隙穿过或后退重找角度 | 轻微擦碰，船体 1-2 损伤 | 正面碰撞，船体 3-5 损伤并失速 |
| 飞行大鸟临时避险 | 4-6s | 8-14s | 借云雾/残骸阴影避开靠近，或改航向等它离开 | 航程变长，船体轻微不稳 | 被迫改道、船体受损或进入撤退建议 |

“彻底失败”不应立刻结束游戏。它应把玩家推入一个更差但可继续判断的状态：失速、迷航、模块短路、返航窗口变差或资源损失。只有船体状态已接近极限时，才进入迫降或强制撤退。

## 5d. 撤退与损伤阈值

| 状态 | 触发 | 玩家可见反馈 | 允许行动 |
| --- | --- | --- | --- |
| 安全航行 | 无未解决主问题 | 航标清晰、目的地可读 | 继续、调整航向、扫描 |
| 压力航行 | 主问题处理中或连续部分失败 | 仪表警告、云层压近、航标不稳 | 继续处理、减速、撤退 |
| 危险航行 | 船体进入 damaged / 模块短路 / 大鸟避险失败 | 船体抖动、警报、返航提示亮起 | 撤退、硬抗、尝试最后处理 |
| 临界航行 | 船体 critical 或连续彻底失败 | 画面不稳、速度下降、目的地/返航二选一 | 强烈建议撤退；继续会有迫降风险 |

撤退不是失败。撤退应保留已揭示风险、已学习航标、部分航程日志和合理损伤；玩家回到船内后能理解“我知道了什么，下次要如何准备”。

## 5e. 第一版问题玩法规格

### 浓雾带

- 设计目的: 教玩家“看不清不等于立刻失败”，需要通过减速、扫描或绕行恢复方向感。
- 出现位置: 雾灯残骸短线可作为教学主问题；旧集市边缘长线可作为背景压力。
- 可读信号: 远景目的地轮廓淡出，航标光变弱，近景云雾速度变慢但面积变大；风声变闷。
- 玩家解法:
  - 降速穿越: 安全但航程变长。
  - 侦察扫描: 若侦察模块可用，短暂显示隐藏航标和安全通道。
  - 绕行: 转向跟随雾边缘，保持航标可见。
  - 硬闯: 保持高速穿过，可能迷航或错过目的地入口。
- 成功反馈: 航标重新亮起，目的地轮廓回到前方，航向偏差归零或可控。
- 失败反馈: 进入 `压力航行`；航标短暂互相矛盾，航程增加，返航提示更亮。
- 系统联动: #8 侦察模块影响扫描清晰度；#6 已知情报可提前标出雾带边缘；#10 记录航程延长或迷航。
- Smoke / QA 关注点: 关闭 UI 仍能看出雾带边界和安全方向；玩家不应只靠文字提示判断。

### 漂浮残骸群

- 设计目的: 让驾驶手感成立，要求玩家转向、减速、后退重找角度，而不是只点确认。
- 出现位置: 雾灯残骸短线可作为教学主问题；旧集市边缘长线可作为主问题，也可成为大鸟避险时的遮蔽工具。
- 可读信号: 中景出现大小不同的残骸，近景残骸按透视放大接近，缝隙清晰可见。
- 玩家解法:
  - 穿缝: 对准残骸间隙低速通过。
  - 后退重找角度: 避免正面碰撞，代价是航程时间。
  - 贴边绕行: 安全但可能拉长航程或进入浓雾边缘。
  - 硬抗: 船体承受轻到中等损伤，通过残骸群。
- 成功反馈: 残骸从两侧掠过，船体无明显抖动，速度恢复。
- 失败反馈: 船体前景震动、木金属碰撞声、航向短暂偏移。
- 系统联动: #8 船体状态决定硬抗可承受程度；#5 货物过载可降低转向/刹停余量；#17 播放碰撞/擦碰反馈。
- Smoke / QA 关注点: 残骸边界、可穿缝隙和碰撞结果必须在世界层可见，不依赖 debug hitbox。

### 飞行大鸟临时避险

- 设计目的: 提供第一版生态压迫遭遇。大鸟不是敌兵，也不是射击目标，而是航道里的大型野生风险。
- 出现位置: 旧集市边缘长线的主要压力点；雾灯残骸短线只可作为远景剪影或轻提示。
- 可读信号: 远景出现大鸟剪影，随后从侧前方或上方接近；风声和翼振变重，航标光被短暂遮住，云层或残骸阴影形成可读避险空间。
- 玩家解法:
  - 云雾避险: 进入浓雾或厚云边缘，降低被大鸟接近的风险。
  - 残骸遮蔽: 穿过或贴近残骸阴影，让大鸟改变飞行路径。
  - 改航向/降速等待: 暂时偏离最短路线，等大鸟掠过。
  - 硬抗继续: 承受船体震动、货物晃动或轻微损伤，保持路线。
  - 撤退: 如果船体 damaged/critical，撤退是有效解法。
- 成功反馈: 大鸟掠过远离，翼振声减弱，航标重新可读，目的地方向恢复。
- 失败反馈: 船体前景剧烈晃动、速度短暂下降、航向偏移或进入 `压力航行` / `危险航行`。
- 系统联动: #8 船体状态决定硬抗风险；#5 货物固定程度可影响晃动/损失；#10 记录避险、改道、损伤或撤退；#17 提供翼振、风压和船体应力反馈。
- Smoke / QA 关注点: 玩家必须能在世界层看出大鸟接近、可用遮蔽位置和风险结束；不得要求主动开火。

## 5f. 后续问题池

以下问题不属于 demo 第一版硬范围，但保留为后续扩展方向。#10 或 #20 后续实现不得把它们提前塞入 release gate，除非重新更新本场景规格和 readability checklist。

| 后续问题 | 保留目的 | 延后原因 |
| --- | --- | --- |
| 风暴边缘 | 风险收益判断：贴边抢时间和绕开保安全 | 第一版先证明浓雾/残骸/生态避险，避免环境压力过多 |
| 失准航标 | 情报和观察驱动的导航判断 | 需要 #6/#9 航图与情报系统更成熟 |
| 空盗拦截 | 非射击对抗遭遇：云层断锁、急转甩脱、残骸阻挡 | 用户确认第一版空盗可以先没有；主动开火也不在第一版 |
| 失控无人浮标 | 谜题型扫描节奏和校准关闭 | 需要更明确的模块/扫描/航图联动 |

## 6. Optional / Readability Beats

- Optional observation points: 远处目的地轮廓、旧航标、漂浮小地标、大鸟远景剪影。
- Local identity details: 雾灯残骸方向有灯影和雾；旧集市边缘方向有破旧棚架/灯笼/码头残影。
- Life / repair / damage traces: 船体受损时前景结构抖动或仪表报警；模块异常时显示在船内/前景反馈。
- Player guidance embedded in the world: 航标链、风向云纹、残骸缝隙、云层/残骸遮蔽区域。
- UI assistance, if any: 可显示简短航向、船体、模块、风险提示；不得替代世界风险物和目的地轮廓。

## 7. State Variants

| Variant | Trigger / source state | World/playable scene evidence | UI assistance allowed |
| --- | --- | --- | --- |
| Initial | 正常出航 | 清晰航标链、低密度云层、目的地方向轮廓 | 航向/距离简条 |
| Progressed / completed | 接近目的地或解决一个航行问题 | 目的地轮廓变大，风险物远离或被甩脱，航标更清晰 | 航程摘要 |
| Blocked / abnormal | 迷航、大鸟靠近、残骸碰撞、船体/模块异常 | 云层压近、大鸟剪影压近、残骸擦碰、船体反馈 | 警报和处理提示 |

## 8. Interaction Contract

| Anchor ID | Player action | Input / focus rule | Domain owner | Disabled / failure feedback | World evidence |
| --- | --- | --- | --- | --- | --- |
| `voyage.heading` | 转向 | 航行中持续输入 | #10 / movement implementation TBD | 航向受浓雾、残骸碰撞或大鸟风压干扰时反馈偏移 | 世界层级围绕新航向重排 |
| `voyage.throttle` | 前进 / 后退 / 减速 | 航行中持续输入 | #10 | 残骸密度过高或大鸟靠近时速度受限 | 云层和残骸运动方向变化 |
| `voyage.scan` | 扫描 / 校准 | 有侦察或航图能力时可用 | #6 / #8 / #10 | 模块故障时扫描失败 | 航标恢复、雾中轮廓显现 |
| `voyage.evade` | 规避 / 避险 | 遭遇生态压迫或环境问题时可用 | #10 | 时机错误导致损伤或偏航 | 大鸟被云层/残骸遮蔽隔开 |
| `voyage.retreat` | 撤退 | 航行中可用 | #10 / #3 | 返航窗口差时撤退更慢或代价更高 | 返航航标亮起，世界运动反向 |

## 9. Data / Runtime Contract

- Godot scene or runtime surface: TBD；应为独立航行大场景，而非 Chart UI 子面板。
- Stable IDs: `voyage_open_world_scene`, `route.sky-reef-arc-01`, `route.storm-cut-01`, `mist_lamp_wreck_scene`, `old_market_edge_scene`。
- Domain managers read: #9 航图、#10 航行风险、#8 船体/模块、#6 情报、#3 存档。
- Domain managers mutated: #10 航程状态；#8 船体/模块后果；#6 航线知识；#3 航程快照。
- Persistence fields: 航线 ID、航向、进度、已解决问题、已揭示风险、船体/模块后果、撤退状态。
- Signals / semantic events: voyage_state_changed, encounter_triggered, hull_band_changed, route_travel_completed。
- Focus and modal boundaries: 航行操控不应被常驻 UI 抢焦点；警报和提示为辅助。
- Runtime debug/smoke hooks: TBD；至少暴露当前航线、航向、问题类型、解决状态、UI 是否主导、目的地接近状态。

## 10. Asset And Audio Needs

| Priority | Need | Supports identity / interaction / state / feedback | Current source | Gap owner |
| --- | --- | --- | --- | --- |
| P0 | 近景云雾与视差层 | 航行运动感、浓雾问题 | tracked gap | Art |
| P0 | 航标链 | 导航判断、目的地方向 | tracked gap | Art / Design |
| P0 | 漂浮残骸群 | 驾驶规避、穿缝、损伤风险 | tracked gap | Art / Design |
| P0 | 飞行大鸟剪影 / 靠近动画 | 生态压迫、临时避险、世界身份 | tracked gap | Art / Animation / Audio |
| P0 | 雾灯残骸 / 旧集市边缘远景轮廓 | 目的地接近和场景身份 | tracked gap | Art |
| P1 | 船首/舱窗/仪表前景 | 飞船存在感、船长视角 | tracked gap | Art / UI |
| P1 | 航行环境音、雾中风声、残骸擦碰、大鸟翼振 | 氛围和风险反馈 | tracked gap | Audio |

## 11. QA Evidence

| Evidence type | Required artifact | Status |
| --- | --- | --- |
| Automated smoke | 航行大场景 runtime contract / debug hook | pending |
| Screenshot / visual proof | 初始航行、浓雾/残骸/大鸟临时避险、抵达目的地 | pending |
| Codex review | Scene spec + #19/#20 gate review | pending |
| User readability review | `production/playtests/scene-readability-voyage-open-world.md` | pending |

Human QA must answer:

- 我是否能在 3 秒内看出自己正在空海航行？
- 我是否能从世界变化读出前进、转向、后退？
- 我是否能看懂前方问题是环境问题还是生态压迫问题？
- 我是否知道可以规避、借遮蔽避险、减速、硬抗或撤退？
- UI 是否只是辅助，而不是把航行变成进度条？

## Readiness Checklist

- [x] Scene purpose, loop role, and emotional target are explicit.
- [x] Entry, exit, failure, and return paths are explicit at design level.
- [x] Spatial layout names航道、云层、航标、残骸、风险物、目的地轮廓。
- [ ] Scene Physics Contract is linked and passing.
- [x] Scene units come from world/playable scene layer, not UI/HUD/buttons/labels/debug overlays.
- [x] Critical path and optional readability beats are documented.
- [x] At least three state variants are documented.
- [x] Interaction anchors name input/focus behavior and domain owner.
- [x] Runtime/state contract does not create a new gameplay authority.
- [x] P0 asset/audio needs are traceable to identity, interaction, state, or feedback.
- [ ] Automated evidence, screenshot evidence, Codex review, and user review paths are complete.
