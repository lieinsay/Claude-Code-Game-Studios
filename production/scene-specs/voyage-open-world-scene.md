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
| Cutaway / Reveal Model | N/A true；无建筑剖切，但云雾和风暴遮挡必须保留航向与飞船可读性 |
| Unit catalog | 飞船前景轮廓、云层、航标、漂浮残骸、风暴边缘、浓雾带、空盗追击影、失控无人浮标、目的地轮廓 |
| Collision / occlusion / scale | pending；残骸、风暴边缘、浮标扫描区、云层遮挡需要可读边界 |
| Special surfaces / dynamic behaviors / recovery | 云雾、风暴、横风、追击、扫描锁定、返航点标记；卡死或迷航时必须能撤退或回到最近安全航向 |
| Exemption reason, if no gameplay-relevant physical units | N/A |

## 3. Entry / Exit

- Entry source: 初始岛屿或船内航行准备完成后进入。
- Spawn / arrival position: 飞船进入航道起点，船首/视角对准已选航线方向。
- Exit or return path: 抵达 `mist_lamp_wreck_scene` 或 `old_market_edge_scene`；也可撤退返回初始岛屿/船内。
- Cancel / failure path: 航行中可撤退；船体或模块状态过差时可被迫改道或迫降，具体结算由 #10/#8/#11 后续定义。
- Saved-state return behavior: 航行中断应恢复至最近安全航向、当前风险状态和已揭示信息；不能恢复到无上下文的 UI 面板。
- Scene transition cleanup expectations: 离开航行大场景时清理临时追击、扫描、云雾遮挡和航向反馈，不清除已写入的航线知识或船体后果。

## 4. Spatial Layout

- Main viewport composition: 前景保留少量船首/舱窗/仪表边缘；中景显示航道、航标、残骸、云层；远景显示目的地轮廓、风暴墙或旧集市边缘方向。
- Walkable area: 飞船可控航道平面，表现为前进方向上的可读通路，而不是角色步行区域。
- Boundaries: 风暴边缘、浓雾核心、漂浮残骸密集区、扫描锁定区、航道外深云区。
- Landmarks: 初始岛屿远离轮廓、雾灯残骸灯影、旧集市边缘轮廓、航标链、风暴墙。
- Interaction anchors: 航向、速度、撤退点、扫描/校准、云层断锁、残骸穿缝、浮标扫描间隙。
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
| 第一问题窗口 | 15-40% | 低到中强度的航路判断 | 浓雾带、残骸群或失准航标进入中景 | 绕行、减速、穿缝、扫描或等待 |
| 中段喘息 | 40-55% | 让玩家读懂处理结果，恢复方向感 | 风险物远离，目的地轮廓短暂显现 | 重新校准航向，决定继续或撤退 |
| 第二问题窗口 | 55-80% | 主要压力点，可能出现对抗问题 | 空盗追击、无人浮标、风暴边缘或复合环境 | 甩脱、干扰、硬抗、绕行或撤退 |
| 抵达压缩 | 80-100% | 目的地接近，最后一次判断 | 目的地轮廓放大，航道收窄，返航窗口变弱 | 稳定进入目的地，或在高风险下撤退 |

每趟航行最多同时突出一个主问题，允许一个低强度次问题作为背景压力。玩家不应该在同一时间同时处理浓雾、残骸、追击、浮标和风暴，否则航行会变成噪声而不是判断。

## 5b. Demo 航线编排

| 航线 | 目标时长 | 问题数量 | 必出问题 | 可选问题 | 节奏目标 |
| --- | --- | --- | --- | --- | --- |
| 初始岛屿 → 雾灯残骸 | 60-75s | 1 个主问题 + 1 个轻背景问题 | 浓雾带 或 漂浮残骸群 | 失准航标轻提示 | 教玩家“看前方、调航向、减速/绕行、抵达目的地” |
| 初始岛屿 → 旧集市边缘 | 110-140s | 2 个主问题 + 1 个轻背景问题 | 风暴边缘 + 空盗拦截 或 失控无人浮标 | 浓雾带 / 失准航标 | 测试完整航行判断：绕行、甩脱/干扰、风险收益和撤退 |

第一版不要求每次航行都随机不同。为了可读性，demo 可以使用半固定编排：雾灯残骸线偏教学，旧集市边缘线偏完整压力。随机性只用于问题具体位置、持续时间或视觉变体，不改变玩家必须学习的核心动作。

## 5c. 问题处理窗口

| 问题 | 预警时间 | 主处理时间 | 成功判定 | 部分失败 | 彻底失败 |
| --- | --- | --- | --- | --- | --- |
| 浓雾带 | 4-6s | 8-12s | 降速/扫描/绕行后航标恢复，航向偏差可控 | 航程 +5-10s，短暂迷航 | 偏离航道，进入返航/重校准状态 |
| 漂浮残骸群 | 3-5s | 6-10s | 从缝隙穿过或后退重找角度 | 轻微擦碰，船体 1-2 损伤 | 正面碰撞，船体 3-5 损伤并失速 |
| 风暴边缘 | 5-8s | 10-18s | 贴边通过或绕开，风险不升级 | 模块短暂不稳，航程变长 | 模块损伤或强制撤退建议 |
| 失准航标 | 5-8s | 8-15s | 用多航标/扫描校准真方向 | 绕远，目的地出现延迟 | 进入错误航向，需要手动重校准 |
| 空盗拦截 | 4-6s | 12-20s | 进入云层断锁、急转甩脱或残骸阻挡 | 船体/资源轻损，追击延长 | 被迫改道或触发资源损失 |
| 失控无人浮标 | 4-6s | 10-16s | 等待扫描间隙、绕开或校准关闭 | 侦察短暂失效 | 模块短路，后续预警降低 |

“彻底失败”不应立刻结束游戏。它应把玩家推入一个更差但可继续判断的状态：失速、迷航、模块短路、返航窗口变差或资源损失。只有船体状态已接近极限时，才进入迫降或强制撤退。

## 5d. 撤退与损伤阈值

| 状态 | 触发 | 玩家可见反馈 | 允许行动 |
| --- | --- | --- | --- |
| 安全航行 | 无未解决主问题 | 航标清晰、目的地可读 | 继续、调整航向、扫描 |
| 压力航行 | 主问题处理中或连续部分失败 | 仪表警告、云层压近、航标不稳 | 继续处理、减速、撤退 |
| 危险航行 | 船体进入 damaged / 模块短路 / 追击未甩脱 | 船体抖动、警报、返航提示亮起 | 撤退、硬抗、尝试最后处理 |
| 临界航行 | 船体 critical 或连续彻底失败 | 画面不稳、速度下降、目的地/返航二选一 | 强烈建议撤退；继续会有迫降风险 |

撤退不是失败。撤退应保留已揭示风险、已学习航标、部分航程日志和合理损伤；玩家回到船内后能理解“我知道了什么，下次要如何准备”。

## 6. Optional / Readability Beats

- Optional observation points: 远处目的地轮廓、风暴墙边缘、旧航标、漂浮小地标。
- Local identity details: 雾灯残骸方向有灯影和雾；旧集市边缘方向有破旧棚架/灯笼/码头残影。
- Life / repair / damage traces: 船体受损时前景结构抖动或仪表报警；模块异常时显示在船内/前景反馈。
- Player guidance embedded in the world: 航标链、风向云纹、残骸缝隙、云层断锁区域。
- UI assistance, if any: 可显示简短航向、船体、模块、风险提示；不得替代世界风险物和目的地轮廓。

## 7. State Variants

| Variant | Trigger / source state | World/playable scene evidence | UI assistance allowed |
| --- | --- | --- | --- |
| Initial | 正常出航 | 清晰航标链、低密度云层、目的地方向轮廓 | 航向/距离简条 |
| Progressed / completed | 接近目的地或解决一个航行问题 | 目的地轮廓变大，风险物远离或被甩脱，航标更清晰 | 航程摘要 |
| Blocked / abnormal | 迷航、追击、风暴边缘、浮标锁定、船体/模块异常 | 云层压近、追击影、扫描线、风暴光、船体反馈 | 警报和处理提示 |

## 8. Interaction Contract

| Anchor ID | Player action | Input / focus rule | Domain owner | Disabled / failure feedback | World evidence |
| --- | --- | --- | --- | --- | --- |
| `voyage.heading` | 转向 | 航行中持续输入 | #10 / movement implementation TBD | 航向受风切或锁定干扰时反馈偏移 | 世界层级围绕新航向重排 |
| `voyage.throttle` | 前进 / 后退 / 减速 | 航行中持续输入 | #10 | 风暴/残骸密度过高时速度受限 | 云层和残骸运动方向变化 |
| `voyage.scan` | 扫描 / 校准 | 有侦察或航图能力时可用 | #6 / #8 / #10 | 模块故障时扫描失败 | 航标恢复、雾中轮廓显现 |
| `voyage.evade` | 规避 / 甩脱 | 遭遇对抗或环境问题时可用 | #10 | 时机错误导致损伤或偏航 | 追击影被云层/残骸隔开 |
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
| P0 | 航标链 / 失准航标 | 导航判断、校准玩法 | tracked gap | Art / Design |
| P0 | 漂浮残骸群 | 驾驶规避、穿缝、损伤风险 | tracked gap | Art / Design |
| P0 | 风暴边缘 | 风险收益判断 | tracked gap | Art / VFX |
| P0 | 空盗追击影 | 非射击对抗遭遇 | tracked gap | Art / Design |
| P0 | 失控无人浮标 | 扫描间隙和干扰问题 | tracked gap | Art / Design |
| P0 | 雾灯残骸 / 旧集市边缘远景轮廓 | 目的地接近和场景身份 | tracked gap | Art |
| P1 | 船首/舱窗/仪表前景 | 飞船存在感、船长视角 | tracked gap | Art / UI |
| P1 | 航行环境音、风暴、追击、浮标扫描音 | 氛围和风险反馈 | tracked gap | Audio |

## 11. QA Evidence

| Evidence type | Required artifact | Status |
| --- | --- | --- |
| Automated smoke | 航行大场景 runtime contract / debug hook | pending |
| Screenshot / visual proof | 初始航行、浓雾/残骸/风暴/对抗问题、抵达目的地 | pending |
| Codex review | Scene spec + #19/#20 gate review | pending |
| User readability review | `production/playtests/scene-readability-voyage-open-world.md` | pending |

Human QA must answer:

- 我是否能在 3 秒内看出自己正在空海航行？
- 我是否能从世界变化读出前进、转向、后退？
- 我是否能看懂前方问题是环境问题还是对抗问题？
- 我是否知道可以规避、甩脱、干扰、硬抗或撤退？
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
