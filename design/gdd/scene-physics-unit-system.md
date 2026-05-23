# 场景单位物理设计

> **Status**: In Design -- awaiting Codex + User review
> **Author**: User + Codex
> **Last Updated**: 2026-05-24
> **Implements Pillar**: 飞艇是家，不只是载具; 规划先于冒险; 未知带来温和压力
> **Platform Pivot Note**: Active implementation targets desktop Godot 4.6.2 .NET/C#. This GDD governs scene unit scale, collision, occlusion, special surfaces, and dynamic physical behavior for authored 2D scenes.

## Overview

场景单位物理设计系统定义《云海织航》所有 2D 可进入场景中“单位如何占空间、如何遮挡、如何碰撞、如何移动、如何被推动、如何表达高度、如何与水/玻璃/镜子/弹性/滑动等特殊物理规则交互”。它不定义具体场景目的，也不替代 `玩家移动与交互` 的输入和焦点职责；它是 `完整场景构成与验收` 的物理契约依赖。

没有这个系统，场景设计会在实现阶段变成临场猜测：玩家能不能从这里通过、箱子能不能推、飞行单位的高度怎么看、玻璃后面的 NPC 会不会被挡住、弹性单位是反弹玩家还是道具、正式资产替换灰盒后碰撞是否仍然成立。这个系统把这些问题提前写成设计规范和验收点。

本系统适用于水平场景、垂直场景、Hub 船内、探索地、码头、市场、修复点、航图桌周边，以及未来任何带有可移动单位、阻挡物、可推动物、特殊表面或动态物理行为的 2D 场景。

### Terminology: Scene Unit

`场景单位` 指存在于 world/playable scene layer 中、会影响或表达场景空间规则的实体。它可以是可移动角色、NPC、阻挡物、门、箱子、残骸、摊位、梯子、楼梯、平台、水面、玻璃、镜子、影子、高度标记、危险边界、可推动物、弹性物或可破坏物。一个场景单位不一定有机械交互，但只要它影响移动、碰撞、遮挡、尺度、特殊表面、玩家读法或 QA 验收，就必须被纳入 Scene Physics Contract。

`场景单位` 不等于 UI 控件、HUD 文本、按钮、菜单或调试标签。UI 可以说明单位状态，但不能替代单位本身。若一个元素只存在于 UI 层，不存在于 world/playable scene layer，它不能被登记为场景单位，也不能作为场景物理验收证据。

## Player Fantasy

玩家应该感觉自己站在一个有真实空间规律的世界里。墙会挡路，箱子能推，水面和深渊能被看懂，玻璃既能透视也会阻挡，雾会遮蔽但不让人困惑，飞行和跳跃的高度能通过影子和落点读出。物理规则不是为了炫技，而是为了让场景可信、可预测、可规划。

在 Hub 中，单位物理支持“飞艇是家”：舱壁、门、货箱、楼梯、走廊、平台和前景遮挡都要让船内空间像能居住、能走动、能整理的地方。在探索中，单位物理支持“未知带来温和压力”：残骸、水流、雾、可推动货箱、边缘和危险物要让玩家判断路线与风险，而不是被看不懂的碰撞惩罚。在市场和修复点中，单位物理支持“世界会回应照料”：摊位、NPC、灯塔、材料提交点和修复后变化都必须稳定占据空间并能被玩家理解。

## Detailed Rules

### Core Rules

**R1 -- 场景必须先声明物理场景类型。** 每个 2D 可进入场景在布局、镜头、碰撞、交互、资产规格之前，必须先声明它是 `水平场景` 还是 `垂直场景`。未声明类型的场景不得进入 `implementation_ready`。

| 场景类型 | 移动平面 | 高度表达 | 视觉层次 | 典型用途 |
| --- | --- | --- | --- | --- |
| `水平场景` | 可移动单位可上下左右移动，但默认都在地面平面上移动 | 跳跃或飞行可离开地面；用地面影子、落点、遮挡和高度标记让玩家知道单位高度变化 | 主要以地面平面、路径、障碍和可交互物为核心；可有背景，但不承担主要移动读法 | 探索点、码头外部、市场广场、修复设施周边 |
| `垂直场景` | 可移动单位主要左右移动；上下移动依赖跳跃、飞行、楼梯、梯子、平台或楼层连接 | 高度通过楼层、平台、梯子、楼梯、跳跃弧线或飞行轨迹表达 | 必须有可读景深，能区分前景、中景、背景和物体前后关系 | 飞艇船内剖面、室内舱室、多层设施、侧视平台场景 |

**R2 -- 场景单位必须有碰撞语义。** 场景中的可移动单位、阻挡物、可推动物、门、箱子、NPC、船体部件、残骸、摊位和设施都必须声明碰撞行为。不得让程序员从美术图、节点名或视觉轮廓猜测碰撞规则。

**R2a -- UI 控件不是场景单位。** HUD、按钮、文本标签、状态面板、菜单和调试 overlay 不得登记为场景物理单位，也不得承担阻挡、可推动、遮挡、特殊表面或高度表达职责。它们可以显示场景单位的名称、状态和操作提示，但 Scene Physics Contract 必须引用 world/playable scene layer 中真实存在的场景单位。

| 碰撞类型 | 行为 | 示例 |
| --- | --- | --- |
| `blocking_static` | 阻挡可移动单位，自己不移动 | 墙、舱壁、码头边、灯塔基座、摊位 |
| `blocking_dynamic` | 阻挡可移动单位，并可能按规则移动或改变状态 | 门、升降平台、活动桥、可开合舱门 |
| `pushable` | 可被可移动单位推动，推动规则由场景规格说明 | 货箱、小推车、可移动残骸 |
| `soft_overlap` | 可重叠但触发焦点、提示、收集或交互 | 搜索线索、修复提交点、NPC 对话范围 |
| `height_marker` | 不阻挡或只弱阻挡，用来表达高度/影子/落点 | 飞行单位影子、跳跃落点、高处平台投影 |

**R3 -- 场景单位必须有前后遮挡关系。** 所有会出现在玩家移动平面附近的单位必须声明遮挡层级和排序规则。场景不得只靠节点创建顺序决定谁挡住谁。

| 遮挡层 | 用途 | 排序规则 |
| --- | --- | --- |
| `background` | 远景、天空、远处建筑、不可交互轮廓 | 永远在可移动单位后方，不参与碰撞 |
| `midground_floor` | 地面、平台、甲板、可移动单位所在主层 | 可移动单位默认所在层；水平场景通常按 Y 坐标排序 |
| `midground_object` | 可交互物、阻挡物、可推动物、NPC、设施 | 根据场景类型使用 Y-sort、floor index 或手工 z-index |
| `foreground_occluder` | 前景栏杆、门框、树冠、舱壁边缘等短暂遮挡物 | 可遮挡单位，但不得长期隐藏核心交互点 |
| `height_shadow` | 跳跃/飞行单位在地面的影子或落点标记 | 影子留在地面排序层，飞行单位本体可在 higher layer |
| `ui_overlay` | HUD、提示、交互标签、模态面板 | 只呈现信息，不参与世界遮挡和碰撞 |

水平场景中，站在更“下方”的单位通常应显示在更前面；垂直场景中，楼层、平台和景深层级必须优先于单纯 Y-sort。任何例外都必须在场景规格中说明。

**R4 -- 场景单位必须有大小规范。** 场景规格必须定义当前场景的尺度基准，并说明玩家、NPC、可推动物、阻挡物、交互点、门/通道和大型地标的相对大小。正式资产不得在未经重审的情况下改变可走性、碰撞半径或交互可读性。

| 单位类型 | 相对大小规范 | 设计要求 |
| --- | --- | --- |
| `player_unit` | 场景主尺度基准，记为 1.0 unit height | 所有门、通道、交互半径和遮挡关系以玩家为基准 |
| `small_unit` | 0.35-0.7x player height | 小道具、小箱子、线索碎片；不得误读为可通行门或主交互点 |
| `npc_unit` | 0.8-1.3x player height | 允许角色差异，但不能破坏交互提示和遮挡读法 |
| `pushable_unit` | 0.4-1.2x player height; width 可大于玩家 | 必须能看出可推动方向、占地和碰撞边界 |
| `blocking_unit` | 任意大小，但占地边界必须明确 | 墙、摊位、残骸、舱壁等必须让玩家看出不能直接穿过 |
| `door_or_passage` | clear height >= 1.2x player height; clear width >= 1.1x player width | 通行入口必须比玩家明显可通过；若不可通过必须有阻挡反馈 |
| `landmark_unit` | >= 2.0x player height or dominates local viewport | 灯塔、船体、市场棚架等用于地点识别，不应与小交互物混淆 |

**R5 -- 特殊表面必须先定义行为，再定义效果。** 水、镜子、玻璃、雾、云、半透明帘布、反光金属、破碎地面、可坠落边缘等特殊表面不能只作为美术装饰。

| 特殊表面 | 默认行为 | 必须声明 |
| --- | --- | --- |
| `water_shallow` | 可进入或可涉水，降低移动或改变脚步反馈 | 是否阻挡、是否减速、是否能推动物体进入、是否有涟漪/脚步声 |
| `water_deep` | 默认阻挡地面单位，可被飞行单位跨越 | 边界提示、落水/不可通行反馈、影子是否投射在水面 |
| `mirror` | 反射单位或场景局部，不应改变碰撞 | 反射范围、是否反射玩家/NPC/道具、反射延迟或简化方案、是否可交互 |
| `glass_clear` | 可透视，默认阻挡移动和推动 | 是否可破坏、是否遮挡交互、反射强度、背后单位可读性 |
| `glass_broken` | 可透视，可能阻挡、伤害或允许通过 | 碰撞剩余边界、危险提示、碎片是否可推动或收集 |
| `fog_or_cloud` | 弱遮挡，降低可见度但不一定阻挡移动 | 可见度影响、单位轮廓保留、交互提示是否穿透、音频低通或风声 |
| `transparent_fabric` | 半遮挡，通常不阻挡或只弱阻挡 | 前后层级、是否可穿过、是否遮挡提示、摆动是否影响碰撞 |
| `reflective_metal` | 反光但不镜像完整场景 | 高光规则、是否会误导成可交互/可通行 |
| `breakable_floor` | 初始可站立，触发后变阻挡/空洞/危险 | 触发条件、坠落/绕行规则、修复或重置状态 |
| `ledge_or_void` | 地面单位不可直接越过，跳跃/飞行可跨越 | 边缘读法、影子落点、失败反馈、是否有护栏/前景遮挡 |

特殊表面如果会改变核心路径或玩家安全感，必须进入 Critical Path 或 Edge Cases；如果只是氛围装饰，则必须标记为 `visual_only`，并明确不影响碰撞、交互和状态。

**R6 -- 动态物理行为必须显式标记。** 除了“是否阻挡/可推动”之外，场景单位还可以具有弹性、滑动、黏附、移动、破坏、变形、伤害、吸附、传送等物理行为。

| 行为标签 | 默认含义 | 必须声明 |
| --- | --- | --- |
| `elastic` | 可反弹、弹开或压缩回弹 | 反弹方向、最大弹力、是否影响玩家/道具/NPC、是否可连续触发 |
| `slippery` | 降低摩擦，单位会滑行或难以停下 | 摩擦倍率、是否影响推箱、是否影响转向/刹停 |
| `sticky` | 增加摩擦或黏附单位 | 是否减速、是否可挣脱、是否影响跳跃/飞行/推动 |
| `conveyor` | 持续推动单位朝某方向移动 | 方向、速度、是否影响可推动物、是否可逆转 |
| `moving_platform` | 自身按路径移动并承载单位 | 路径、速度、等待点、是否夹伤/挤压、单位如何跟随 |
| `one_way_platform` | 只从某些方向阻挡或允许通过 | 可通过方向、掉落操作、飞行/跳跃例外 |
| `climbable` | 可攀爬或上下通行 | 进入/离开条件、爬行速度、能否携带/推动物体 |
| `breakable` | 可被交互、碰撞或状态变化破坏 | 触发条件、破坏后碰撞、碎片行为、是否可修复 |
| `deformable` | 形状或碰撞边界会改变 | 变形范围、是否影响路径、是否持久化 |
| `hazardous` | 接触造成伤害、状态变化或失败反馈 | 伤害/后果、预警、免疫条件、恢复路径 |
| `magnetic_or_attracting` | 吸引、排斥或牵引单位 | 作用范围、目标类型、强度、是否可关闭 |
| `teleport_or_warp` | 将单位移动到另一位置 | 入口/出口、冷却、朝向保持、是否影响携带物 |
| `current_or_wind` | 水流、风、气流推动单位或投射物 | 方向、强度、是否影响地面/飞行单位、可视化提示 |
| `trigger_only` | 无实体碰撞，只触发事件或状态 | 触发条件、一次性/重复、是否有可见反馈 |

物理行为标签可以组合，例如 `pushable + slippery` 的湿货箱、`blocking_dynamic + elastic` 的缓冲气囊、`water_shallow + current_or_wind` 的浅水流、`moving_platform + one_way_platform` 的升降甲板。组合标签必须说明优先级：当行为冲突时，以哪个规则为准。

### Scene Physics Contract

每个具体场景必须在场景规格中提供一份 Scene Physics Contract，至少包含：

1. **Scene Physics Type**: `水平场景` 或 `垂直场景`。
2. **Movement Plane**: 可移动单位的默认移动平面、跳跃/飞行/楼梯/梯子的处理。
3. **Unit Catalog**: 玩家、NPC、道具、阻挡物、可推动物、特殊单位列表。
4. **Collision Table**: 每个 gameplay-relevant 单位的碰撞类型。
5. **Occlusion Table**: 背景、中景、前景、影子、UI 的排序规则。
6. **Scale Table**: 相对玩家单位的大小规范。
7. **Special Surfaces**: 特殊表面及其行为或 `visual_only` 标记。
8. **Physical Behaviors**: 动态行为标签、参数、冲突优先级。
9. **Recovery Rules**: 卡死、掉落、挤压、弹出场景、动态物体异常时的恢复路径。

## Formulas

### F-20-01 Physics Contract Completeness

```
physics_contract_complete =
    scene_physics_type_ready
    AND movement_plane_ready
    AND unit_catalog_ready
    AND collision_ready
    AND occlusion_ready
    AND scale_ready
    AND special_surface_ready
    AND physical_behavior_ready
    AND recovery_ready
```

| Variable | Type | True When |
| --- | --- | --- |
| `scene_physics_type_ready` | bool | 场景声明为水平场景或垂直场景 |
| `movement_plane_ready` | bool | 默认移动平面和高度/垂直移动表达已定义 |
| `unit_catalog_ready` | bool | 所有 gameplay-relevant 单位已列出 |
| `collision_ready` | bool | 单位碰撞语义已定义 |
| `occlusion_ready` | bool | 遮挡层级和排序规则已定义 |
| `scale_ready` | bool | 单位大小基准和相对比例已定义 |
| `special_surface_ready` | bool | 特殊表面已声明为 gameplay-affecting 或 `visual_only` |
| `physical_behavior_ready` | bool | 动态物理标签、参数和优先级已定义或明确不存在 |
| `recovery_ready` | bool | 卡死、挤压、掉落、异常移动等恢复路径已定义 |

**Output Range:** true/false。任一变量为 false，则场景不能通过 `完整场景构成与验收` 的物理契约门禁。

**Example:** 云织号船内声明为垂直场景，定义左右移动、楼梯上下、舱壁阻挡、货箱可推动、前景门框遮挡、玩家/门/货箱比例、无特殊表面、无动态弹性行为、卡死时回到最近安全地面，则 `physics_contract_complete = true`。

### F-20-02 Unit Scale Ratio

```
unit_scale_ratio = unit_height / player_unit_height
```

| Variable | Type | Range | Description |
| --- | --- | --- | --- |
| `unit_height` | float | > 0 | 当前单位在场景尺度中的高度 |
| `player_unit_height` | float | > 0 | 玩家单位高度，作为 1.0 基准 |

**Output Range:** > 0。结果必须落入该单位类型的大小规范区间，除非场景规格写明例外并通过用户审查。

**Example:** 一个 NPC 高度为 1.1 倍玩家高度，`unit_scale_ratio = 1.1`，落在 `npc_unit` 的 0.8-1.3 范围内。

### F-20-03 Behavior Conflict Resolution

```
effective_behavior = highest_priority(applicable_behavior_tags)
```

| Variable | Type | Range | Description |
| --- | --- | --- | --- |
| `applicable_behavior_tags` | set | 0..N tags | 当前单位或表面同时满足的行为标签 |
| `highest_priority` | ordered rule | defined per scene | 场景规格中的行为优先级排序 |

**Output Range:** one behavior or ordered composite behavior。组合标签必须有确定顺序。

**Example:** 若单位为 `hazardous + elastic`，规格声明 `hazardous` 先结算，玩家先受到危险反馈，再被弹开；若声明 `elastic` 先结算，则玩家被弹开且不触发危险。未声明优先级时不得实现。

## Edge Cases

1. **多个物理行为叠加产生冲突。** 场景规格必须定义优先级，例如 `hazardous` 是否先于 `elastic` 结算，`conveyor` 是否推动 `pushable`，`sticky` 是否取消 `slippery`。
2. **动态物体把玩家卡死。** 移动平台、可推动物、弹性物、可破坏物必须定义逃脱或复位路径。
3. **物理效果误导可读性。** 弹性、滑动、水流、风、吸附等必须有足够的视觉/音频提示，让玩家在接触前或第一次接触时理解规则。
4. **正式资产改变碰撞。** 资产替换灰盒时，不得改变已批准的碰撞 footprint、交互半径、遮挡行为和尺寸读法，除非重新审查。
5. **前景遮挡核心交互点。** `foreground_occluder` 可短暂遮挡玩家，但不得长期隐藏出口、主要交互点或危险提示。
6. **飞行单位和影子不同步。** 飞行本体与 `height_shadow` 必须同步移动和落点反馈，否则高度表达无效。
7. **特殊表面只是视觉但看起来可交互。** `visual_only` 表面不得误导玩家关于通行、危险、互动、碰撞或高度。
8. **推箱进入不可恢复位置。** 可推动物必须定义边界、复位或绕行方案。
9. **垂直场景滥用 Y-sort。** 多层平台和楼层必须优先使用 floor/layer 规则，不能只用 Y 坐标排序。
10. **水平场景高度不清。** 跳跃或飞行必须用影子、落点、遮挡或高度标记表达。

## Dependencies

| System / Doc | Dependency |
| --- | --- |
| `design/gdd/scene-composition-system.md` | Consumes this system as its scene physics contract gate |
| `design/gdd/player-movement-interaction.md` | Movement input, reachability, interaction focus, Use entry point |
| `design/gdd/airship-hub.md` | Ship interior vertical scene and room traversal expectations |
| `design/gdd/exploration-scavenge-scenario.md` | Horizontal exploration spaces, search points, threats, extraction anchors |
| `design/gdd/port-village-market.md` | Market stalls, NPCs, passability and interaction anchors |
| `design/gdd/world-repair-unlock.md` | Repair sites, facilities, material handoff points and state changes |
| `design/ux/interaction-patterns.md` | Approach + E, focus, keyboard-first, confirmation gates |
| `design/art/art-bible.md` | Shape language, readability and visual identity |
| `docs/architecture/adr-0019-desktop-csharp-platform-pivot.md` | Godot 4.6.2 .NET/C# target |

## Tuning Knobs

| Knob | Default | Range | Owner | Notes |
| --- | --- | --- | --- | --- |
| `player_unit_height` | scene-defined | > 0 | Scene design / implementation | Base unit for all scale rules |
| `npc_scale_min` | 0.8x player | 0.6-1.0x | Design | Lower bound for readable NPC scale |
| `npc_scale_max` | 1.3x player | 1.0-1.6x | Design | Upper bound before NPC reads as landmark or large obstacle |
| `pushable_scale_min` | 0.4x player | 0.25-0.8x | Design / physics | Too small may read as pickup rather than pushable |
| `pushable_scale_max` | 1.2x player | 0.8-2.0x | Design / physics | Large pushables need clear mass and footprint feedback |
| `door_clear_height_min` | 1.2x player | 1.1-1.5x | Level design | Must clearly read as passable |
| `door_clear_width_min` | 1.1x player | 1.0-1.5x | Level design | Wider if carrying/pushing is allowed |
| `identity_occlusion_max_seconds` | 1s | 0-2s | UX / QA | Max time foreground can hide the player or key interaction |
| `elastic_max_repeats` | 3 | 1-5 | Gameplay / QA | Prevents infinite bounce loops |
| `stuck_recovery_seconds` | 2s | 1-5s | QA / implementation | Time before recovery rule should be available |

## Acceptance Criteria

- [ ] GIVEN a 2D scene physics contract is drafted, WHEN review begins, THEN it declares either `水平场景` or `垂直场景`.
- [ ] GIVEN a scene is `水平场景`, WHEN movement readability is reviewed, THEN ground-plane up/down/left/right movement, jump/fly height changes, and ground-shadow or landing-height cues are specified.
- [ ] GIVEN a scene is `垂直场景`, WHEN movement readability is reviewed, THEN left/right movement, depth layering, foreground/background separation, and vertical traversal methods such as jump, flight, ladders or stairs are specified.
- [ ] GIVEN a scene contains gameplay-relevant units, WHEN implementation readiness is reviewed, THEN every unit declares collision behavior.
- [ ] GIVEN units overlap visually, WHEN readability is reviewed, THEN foreground, midground, background, Y-sort/floor sorting, temporary occluders, and flying-unit shadow/body layering are specified.
- [ ] GIVEN a scene contains player, NPC, props, doors, passages or landmarks, WHEN asset readiness is reviewed, THEN each category has a relative size rule tied to `player_unit`.
- [ ] GIVEN a formal asset replaces a greybox unit, WHEN QA reviews the scene, THEN the replacement preserves collision footprint, occlusion behavior, interaction radius and size readability unless the physics contract is re-reviewed.
- [ ] GIVEN a scene contains water, mirror, glass, fog, cloud, transparent fabric, reflective metal, breakable floor, ledge or void, WHEN design review begins, THEN each special surface is marked as gameplay-affecting or `visual_only`.
- [ ] GIVEN a special surface is gameplay-affecting, WHEN implementation readiness is reviewed, THEN movement, collision, occlusion, reflection/refraction, interaction, audio, state and performance behavior are specified.
- [ ] GIVEN a special surface is `visual_only`, WHEN QA reviews it, THEN it does not mislead the player about passability, interactivity, danger, height or collision.
- [ ] GIVEN a scene unit has elastic, slippery, sticky, conveyor, moving-platform, one-way, climbable, breakable, deformable, hazardous, attracting, teleport, current/wind or trigger-only behavior, WHEN implementation readiness is reviewed, THEN behavior label, parameters, feedback, affected unit types and conflict priority are specified.
- [ ] GIVEN multiple physical behavior tags are combined on one unit or surface, WHEN design review runs, THEN priority and fallback rules are defined so collision, damage, movement and state updates cannot contradict each other.
- [ ] GIVEN a dynamic object can move, push, bounce, break, carry or attract the player, WHEN QA reviews the scene, THEN there is a visible escape, reset or recovery path from stuck states.
- [ ] GIVEN this GDD itself is reviewed, WHEN the user completes review, THEN missing physical unit requirements are added before status can move from `In Design` to `Approved`.
