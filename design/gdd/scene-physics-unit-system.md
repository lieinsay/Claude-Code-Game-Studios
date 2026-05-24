# 场景单位物理设计

> **Status**: Approved
> **Author**: User + Codex
> **Last Updated**: 2026-05-24
> **Implements Pillar**: 飞艇是家，不只是载具; 规划先于冒险; 未知带来温和压力
> **Platform Pivot Note**: Active implementation targets desktop Godot 4.6.2 .NET/C#. This GDD governs scene unit scale, collision, occlusion, special surfaces, and dynamic physical behavior for authored 2D scenes.

## Overview

场景单位物理设计系统定义《云海织航》所有 2D 可进入场景中“单位如何占空间、如何遮挡、如何碰撞、如何移动、如何被推动、如何表达高度、如何与水/玻璃/镜子/弹性/滑动等特殊物理规则交互”。它不定义具体场景目的，也不替代 `玩家移动与交互` 的输入和焦点职责；它是 `完整场景构成与验收` 的物理契约依赖。

没有这个系统，场景设计会在实现阶段变成临场猜测：玩家能不能从这里通过、箱子能不能推、飞行单位的高度怎么看、玻璃后面的 NPC 会不会被挡住、弹性单位是反弹玩家还是道具、正式资产替换灰盒后碰撞是否仍然成立。这个系统把这些问题提前写成设计规范和验收点。

本系统适用于水平场景、垂直场景、Hub 船内、探索地、码头、市场、修复点、航图桌周边，以及未来任何带有可移动单位、阻挡物、可推动物、特殊表面或动态物理行为的 2D 场景。当前 demo 的航行大场景虽然表现为伪 3D，但实现和验收仍按 2D 场景契约处理：通过层级、缩放、视差、风险边界、特殊表面和恢复规则表达航行空间，而不是引入真实 3D 物理模拟。

### Terminology: Scene Unit

`场景单位` 指存在于 world/playable scene layer 中、会影响或表达场景空间规则的实体。它可以是可移动角色、NPC、阻挡物、门、箱子、残骸、摊位、梯子、楼梯、平台、水面、玻璃、镜子、影子、高度标记、危险边界、可推动物、弹性物或可破坏物。一个场景单位不一定有机械交互，但只要它影响移动、碰撞、遮挡、尺度、特殊表面、玩家读法或 QA 验收，就必须被纳入 Scene Physics Contract。

`场景单位` 不等于 UI 控件、HUD 文本、按钮、菜单或调试标签。UI 可以说明单位状态，但不能替代单位本身。若一个元素只存在于 UI 层，不存在于 world/playable scene layer，它不能被登记为场景单位，也不能作为场景物理验收证据。

### Terminology: Prototype and Placed Instance

`单位原型` 是可复用的场景单位定义。它声明单位身份、分类、碰撞、遮挡层、尺度规则、特殊表面或物理行为、允许出现的场景、来源 GDD、是否允许 UI 证据等稳定规则。原型做好后，后续场景可以复用它，而不需要重新解释同一种墙、门、舵台、货箱、玻璃或角色的物理读法。单位原型的本体设计放在 `production/unit-specs/`，并且必须按 `fixed-scene-objects/` 与 `dynamic-entities/` 分开存放。

`摆放实例` 是某个具体场景中对单位原型的一次放置。它必须声明稳定实例 ID、引用的 `单位原型`、所在场景、Godot 可视化摆放引用、位置或 transform、floor/layer、场景规格来源，以及可选的状态 hook 或交互锚点。实例可以覆盖摆放位置和场景状态连接，但不能静默改变原型的碰撞 footprint、遮挡行为、交互半径或玩家相对尺度；需要改变时必须新建原型或重新审查物理契约。

单位原型至少分为两类，且两类不能混放：

| 原型分类 | 含义 | 示例 | 额外要求 |
| --- | --- | --- | --- |
| `dynamic_entity` | 会移动、被推动、弹开、飞行、巡逻、改变运行时位置 / 速度 / AI / 状态，或需要同步领域 / 存档状态的实体单位 | 玩家标记、NPC、敌人、可推动箱、移动平台、放在地上的物理球 | 必须放在 `production/unit-specs/dynamic-entities/`；必须声明运动来源、运行时状态、碰撞响应、恢复规则和领域 owner |
| `fixed_scene_object` | 默认不自行移动，主要作为稳定空间结构、地标、门、通道、遮挡、特殊表面、资源点或交互锚点存在的固定单位；它仍然可以被破坏、采集、开关、再生或改变状态 | 舱壁、舵台、货箱、门槛、玻璃、残骸、摊位、前景墙、可砍伐再生树 | 必须放在 `production/unit-specs/fixed-scene-objects/`；必须声明碰撞/遮挡/尺度、是否可交互、状态生命周期、资产替换约束 |

分类判断规则：

| 判断问题 | 若答案为是 | 分类倾向 |
| --- | --- | --- |
| 单位运行时位置、速度、路径、AI 或物理冲量会变化吗？ | 是 | `dynamic_entity` |
| 单位会被玩家推动、弹开、携带、击退、跟随路径或受环境力移动吗？ | 是 | `dynamic_entity` |
| 单位位置基本固定，但会被砍伐、开关、修复、采集、再生或切换状态吗？ | 是 | `fixed_scene_object` |
| 单位主要定义边界、门、坡道、树、残骸、摊位、灯塔、特殊表面或遮挡关系吗？ | 是 | `fixed_scene_object` |
| 单位只是 UI 图标、按钮、文本、HUD 标记或调试标签吗？ | 是 | 不是场景单位 |

若一个单位既有固定载体又会生成移动实体，必须拆成两个规格。例如“树”是 `fixed_scene_object`，砍下来的滚木若能被推动或滚动，则另建 `dynamic_entity` 原型；“炮台底座”是固定单位，炮弹是实体单位。不得用一个混合规格同时承担固定结构和运行时实体职责。

项目采用混合 authoring 规则：Godot 负责可视化摆放和空间读法，JSON/YAML/registry 风格数据负责稳定 ID、原型分类、物理契约、测试和跨文档追踪。若 Godot 节点和数据记录冲突，场景不得进入 `implementation_ready`，直到二者重新对齐。

## Player Fantasy

玩家应该感觉自己站在一个有真实空间规律的世界里。墙会挡路，箱子能推，水面和深渊能被看懂，玻璃既能透视也会阻挡，雾会遮蔽但不让人困惑，飞行和跳跃的高度能通过影子和落点读出。物理规则不是为了炫技，而是为了让场景可信、可预测、可规划。

在 Hub 中，单位物理支持“飞艇是家”：舱壁、门、货箱、走廊、舱段、设备层、货架和前景遮挡都要让船内水平分层空间像能居住、能走动、能整理的地方。在航行中，单位物理支持“规划先于冒险”：云雾、航标、漂浮残骸、风暴边缘、目的地轮廓和航道边界必须让玩家读出方向、风险、接近和撤退。在探索中，单位物理支持“未知带来温和压力”：残骸、水流、雾、可推动货箱、边缘和危险物要让玩家判断路线与风险，而不是被看不懂的碰撞惩罚。在市场和修复点中，单位物理支持“世界会回应照料”：摊位、NPC、灯塔、材料提交点和修复后变化都必须稳定占据空间并能被玩家理解。

## Detailed Rules

### Core Rules

**R1 -- 场景必须先声明物理场景类型。** 每个 2D 可进入场景在布局、镜头、碰撞、交互、资产规格之前，必须先声明它是 `水平场景` 还是 `垂直场景`。未声明类型的场景不得进入 `implementation_ready`。

| 场景类型 | 移动平面 | 高度表达 | 视觉层次 | 典型用途 |
| --- | --- | --- | --- | --- |
| `水平场景` | 可移动单位可上下左右移动，但默认都在地面平面上移动 | 跳跃或飞行可离开地面；用地面影子、落点、遮挡和高度标记让玩家知道单位高度变化 | 主要以地面平面、路径、障碍和可交互物为核心；可有背景，但不承担主要移动读法 | 探索点、码头外部、市场广场、修复设施周边 |
| `垂直场景` | 可移动单位主要左右移动；上下移动依赖跳跃、飞行、楼梯、梯子、平台或楼层连接 | 高度通过楼层、平台、梯子、楼梯、跳跃弧线或飞行轨迹表达 | 必须有可读景深，能区分前景、中景、背景和物体前后关系 | 飞艇船内剖面、室内舱室、多层设施、侧视平台场景 |

**R1a -- 复杂水平场景必须声明 Layer / Height Model。** `水平场景` 可以表现楼房、山体、高台、桥、洞口、室内外、地下入口、前景树冠、远景建筑和多层地貌，但它的默认移动仍发生在一个或多个明确的可走平面上。只要视觉上出现“多层”或“高低差”，场景规格必须说明这些层是 `visual_layer`、`walkable_layer`、`transition_layer`、`height_only_layer` 还是 `blocked_layer`。

| 层类型 | 含义 | 允许行为 | 示例 |
| --- | --- | --- | --- |
| `visual_layer` | 只表达远近、高度或地点身份，不可到达 | 不参与移动、碰撞或交互；可参与遮挡 | 远处楼房、山峰、云中塔影、背景桥 |
| `walkable_layer` | 玩家或可移动单位实际可走的水平平面 | 可移动、碰撞、交互、Y-sort 或 layer-sort | 地面街道、山腰平台、屋顶平台、码头甲板 |
| `transition_layer` | 连接两个可走层的路径或装置 | 楼梯、坡道、梯子、门洞、升降台、跳跃落点 | 上山坡道、进楼门、通往屋顶的梯子 |
| `height_only_layer` | 单位可短暂离地但不形成独立可走区域 | 跳跃、飞行、投影、影子、落点提示 | 飞行敌人、跳过裂缝、被弹起的货箱 |
| `blocked_layer` | 看起来有空间，但设计上不可通行 | 必须给出阻挡读法和反馈 | 楼房外墙、山崖、封闭门、河对岸远景 |

复杂水平场景不能只说“这是水平场景”。它必须声明 `primary_walkable_layer`，并列出所有可到达层、不可到达层和层间转移规则。若某个建筑或山体只作为背景，必须标记为 `visual_layer` 或 `blocked_layer`；若玩家可以进入楼房、爬上屋顶、绕到山后、走上桥面或穿过洞口，则必须为这些空间建立独立的 `walkable_layer` 或切换到新的场景。

**R1b -- 水平场景中的多层表达优先选择清晰切换。** 当多层水平空间会让移动、遮挡或碰撞读法变复杂时，优先把它拆成明确的场景状态或子场景，而不是在同一平面里塞入含糊的“假 3D”。例如：市场街道外部是一个水平场景；进入楼房内部可以切换到室内水平场景或垂直剖面场景；爬山可以用坡道连接山脚与山腰两个 `walkable_layer`；真正多楼层建筑应使用垂直场景或独立楼层场景。

**R1c -- 多层建筑必须声明 Cutaway / Reveal Model。** 当玩家进入多层楼房、飞船舱段、塔、山洞、地下设施、树屋、立体市场或任何有上下层遮挡的空间时，场景必须说明玩家所在层如何被展示。默认规则：玩家位于第 N 层时，N 层以上的结构必须隐藏、剖开、半透明或降低遮挡强度；N 层本身必须完整可读；N 层以下可以保留轮廓、阴影或支撑结构作为空间参照，但不得遮挡玩家和核心交互点。

| 显隐模式 | 用途 | 规则 |
| --- | --- | --- |
| `floor_cutaway` | 多层楼房、飞艇内部、塔楼 | 隐藏或剖开玩家所在层以上的墙、地板和屋顶；保留当前层墙体低截面和门洞 |
| `roof_fade` | 玩家进入房屋、棚屋、车厢、舱室 | 屋顶/树冠/上层遮挡淡出到 20-40% 或完全隐藏；离开后恢复 |
| `front_wall_removed` | 侧视或剖面室内 | 面向镜头的前墙不渲染或只保留低墙；后墙和侧墙保留 |
| `active_floor_focus` | 多楼层同时可见但只操作一层 | 当前层全亮并可交互；其他层降亮、锁交互或只显示轮廓 |
| `vertical_slice_window` | 高塔、山洞、电梯井、深井 | 只显示玩家附近若干层/高度段，远离部分淡出或裁剪 |
| `occluder_peek` | 前景树冠、门框、桥下空间 | 遮挡物在玩家接近时局部透明或开洞，不整块消失 |
| `behind_object_reveal` | 玩家走到楼房、大树、巨石、船体、山体前景后方 | 遮挡玩家的物体局部透明、描边、开洞或降亮；不改变该物体的碰撞和空间身份 |
| `interior_instance` | 进入独立室内/地下/洞穴 | 外部场景保持为入口外观，内部切换为独立场景或子场景 |

显隐模式必须按场景状态区分，不能互相替代：

| 玩家状态 | 正确分类 | 设计含义 |
| --- | --- | --- |
| 玩家进入建筑、舱室、洞穴或地下空间 | `floor_cutaway` / `roof_fade` / `front_wall_removed` / `interior_instance` | 玩家已经从外部场景进入内部空间；重点是显示当前房间、楼层或子场景 |
| 玩家在多层空间中切换楼层 | `active_floor_focus` / `vertical_slice_window` | 玩家仍在同一多层结构内；重点是当前层可读、非当前层降噪 |
| 玩家仍在室外或同一地面层，只是走到大型物体后面 | `behind_object_reveal` | 玩家没有进入该物体；重点是遮挡物局部让位，同时保留碰撞和身份 |
| 玩家短暂经过树冠、屋檐、桥边、门框等前景遮挡 | `occluder_peek` | 遮挡是短时局部问题；重点是避免核心单位被长时间盖住 |

多层显隐不能只靠 UI 小地图或文字说明。玩家必须能从场景本身看出“我在第几层、上层为什么不挡视线、出口/楼梯/梯子在哪里、上下层是否还能交互”。如果上层隐藏会影响剧情、危险或敌人读法，必须用影子、声音、轮廓、透明楼板、楼层编号或边缘提示保留必要信息。

**R1d -- 多层空间必须声明 Floor State。** 每个可到达楼层或高度段至少声明：`floor_id`、`floor_index`、`is_active_floor`、`visibility_mode`、`walkable_bounds`、`vertical_connectors`、`occluders_hidden_above`、`interactions_enabled`。楼层切换时必须原子更新这些状态，避免玩家被隐藏墙、失效碰撞或不可见交互点卡住。

**R1e -- 走到遮挡物后方必须声明 Behind-Object Reveal。** 如果玩家能从楼房、大树、山石、船体、市场棚、塔、残骸、桥墩、前景墙或其他大型单位后方经过，该单位必须声明 `behind_object_reveal`。这不是“玩家进入建筑”，也不是“楼房消失”；这是当前地面层上的前景遮挡处理。默认规则：遮挡物只在玩家被遮住的局部区域淡出、描边、开洞或降亮；遮挡物的碰撞、入口、可交互状态和场景身份不随透明度改变。

| 后方遮挡情况 | 推荐处理 | 必须避免 |
| --- | --- | --- |
| 玩家走到楼房后面 | 楼体作为 `foreground_occluder + blocked_layer`；玩家被遮住时启用 `behind_object_reveal`，楼体局部透明或显示玩家轮廓 | 整栋楼瞬间消失，或楼仍完全挡住玩家 |
| 玩家走到树冠/大树后面 | 树冠淡出，树干保留碰撞；玩家描边可见 | 树冠遮住交互点，或树干碰撞跟着透明消失 |
| 玩家走到山石/巨石后面 | 石体前景部分局部透明，底部碰撞边界保留 | 玩家看不到自己，也不知道巨石是否可绕行 |
| 玩家走到船体/大型机器后面 | 船体前景降亮，入口/门/梯子仍高亮可读 | 船体透明后误以为可以穿过船身 |
| 玩家走到桥/高架下方 | 桥面保留为上层 `walkable_layer`，桥下玩家描边或桥面半透明 | 桥上/桥下单位混成同一层 |
| NPC 或敌人走到遮挡物后面 | 按同一 reveal 规则显示轮廓或影子；若不可见是设计目标，必须声明 stealth/hidden | 敌人攻击玩家但完全不可见，且没有声音/轮廓提示 |
| 可推动物被推到遮挡物后面 | 物体轮廓显示，推动方向和碰撞边界仍可读 | 箱子消失导致玩家不知道是否卡住 |
| 关键交互点被前景遮挡 | 交互点所在区域开洞、淡出或移动提示锚点到可见位置 | 用 HUD 文本替代场景锚点 |

| 复杂情况 | 推荐处理 | 必须避免 |
| --- | --- | --- |
| 楼房外观在水平场景中出现 | 外墙为 `blocked_layer`，门为 `transition_layer`，内部切换到室内场景 | 玩家以为能从窗/墙穿过，或外墙没有碰撞读法 |
| 可进入楼房 | 门口触发场景切换，或室内作为独立 `walkable_layer` 并有清晰遮挡 | UI 按钮直接“进入”，场景中没有门/入口单位 |
| 屋顶可走 | 屋顶为 `walkable_layer`，楼梯/梯子为 `transition_layer`，地面影子或边缘说明高度 | 屋顶和地面在同一 Y-sort 中互相穿插 |
| 多层楼房 | 使用 `floor_cutaway` 或 `active_floor_focus`；玩家在 3 层时隐藏/淡化 3 层以上并保留 3 层可读 | 上层楼板/墙体盖住玩家，或隐藏后玩家不知道自己在几层 |
| 玩家走到楼房后面 | 使用 `behind_object_reveal`；楼房仍是阻挡/遮挡单位，但遮住玩家的局部区域透明、开洞或显示玩家轮廓 | 把它误判为进入楼内的 cutaway，或让楼房完全遮住玩家 |
| 山体/坡地 | 山脚、坡道、山腰平台分层；不可攀爬山壁标记为 `blocked_layer` | 用一张山图让玩家猜哪里能走 |
| 桥/栈道 | 桥面是 `walkable_layer`，桥下水/空洞为 `blocked_layer` 或 `height_only_layer` | 玩家和桥下单位遮挡关系不明 |
| 洞口/山洞 | 洞口是 `transition_layer`，内部切新场景或子层 | 洞口只是背景画，却看起来可进入 |
| 前景树冠/屋檐 | 作为 `foreground_occluder`，遮挡时间和范围受限 | 长时间盖住玩家或核心交互点 |
| 大型机器/飞船 | 轮廓可为 `blocked_layer`，登船口/舱门为 `transition_layer` | 船体只是装饰，玩家找不到真实入口 |
| 水岸/河流 | 水面声明 `water_shallow` / `water_deep`，桥/浅滩声明通行规则 | 水和地面边界靠颜色猜测 |
| 斜坡 | 若可走，必须说明移动速度、投影和碰撞；若不可走，按山壁阻挡 | 看起来能走但被隐形墙挡住 |
| 地下/洞穴 | 入口切换到 `interior_instance` 或 `vertical_slice_window`；外部保留入口状态 | 地表和地下同时显示但没有清晰剖切 |
| 电梯/升降台 | 作为 `transition_layer + moving_platform`，切换 active floor 后更新显隐和碰撞 | 平台移动了，旧楼层碰撞还在挡路 |
| 透明楼板/玻璃地面 | 声明 `glass_clear` 与楼层可见性；下层可见但不可直接交互 | 玩家以为能点/拿下层物体 |
| 上下层敌人或 NPC | 非当前层单位降亮/轮廓化；只有同层或明确连通时可交互/碰撞 | 不同层单位看起来贴在一起，造成误判 |
| 前后重叠房间 | 用房间编号、门洞、墙体剖切或 active-room focus | 玩家不知道自己是在前屋、后屋还是走廊 |

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
3. **Layer / Height Model**: 水平场景中的可走层、视觉层、转移层、高度层、阻挡层；垂直场景中的楼层、平台和景深层级。
4. **Cutaway / Reveal Model**: 多层建筑、室内、山洞、飞船、前景遮挡、behind-object reveal 的显隐、剖切、半透明、active floor / active room 规则。
5. **Unit Catalog**: 玩家、NPC、道具、阻挡物、可推动物、特殊单位列表，并区分 `fixed_scene_object` 与 `dynamic_entity` 来源目录。
6. **Collision Table**: 每个 gameplay-relevant 单位的碰撞类型。
7. **Occlusion Table**: 背景、中景、前景、影子、UI 的排序规则。
8. **Scale Table**: 相对玩家单位的大小规范。
9. **Special Surfaces**: 特殊表面及其行为或 `visual_only` 标记。
10. **Physical Behaviors**: 动态行为标签、参数、冲突优先级。
11. **Recovery Rules**: 卡死、掉落、挤压、弹出场景、动态物体异常时的恢复路径。

## Formulas

### F-20-01 Physics Contract Completeness

```
physics_contract_complete =
    scene_physics_type_ready
    AND movement_plane_ready
    AND layer_height_model_ready
    AND cutaway_reveal_ready
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
| `layer_height_model_ready` | bool | 复杂水平场景的可走层/视觉层/转移层/高度层/阻挡层已定义；垂直场景楼层/平台/景深已定义 |
| `cutaway_reveal_ready` | bool | 多层建筑、室内/地下/洞穴/前景遮挡等场景的显隐、剖切、active floor / active room 规则已定义；无多层遮挡时明确为 N/A |
| `unit_catalog_ready` | bool | 所有 gameplay-relevant 单位已列出 |
| `collision_ready` | bool | 单位碰撞语义已定义 |
| `occlusion_ready` | bool | 遮挡层级和排序规则已定义 |
| `scale_ready` | bool | 单位大小基准和相对比例已定义 |
| `special_surface_ready` | bool | 特殊表面已声明为 gameplay-affecting 或 `visual_only` |
| `physical_behavior_ready` | bool | 动态物理标签、参数和优先级已定义或明确不存在 |
| `recovery_ready` | bool | 卡死、挤压、掉落、异常移动等恢复路径已定义 |

**Output Range:** true/false。任一变量为 false，则场景不能通过 `完整场景构成与验收` 的物理契约门禁。

**Example:** 云织号船内在当前 demo 中声明为水平分层场景，定义上下左右移动的主走廊/地面平面、舱段间门洞或短梯作为 `transition_layer`、设备和货架作为 `foreground_occluder` / `blocked_layer`、局部 `front_wall_removed` 或 `active_room_focus` 的舱室显隐、舱壁阻挡、货箱可推动、玩家/门/货箱比例、无特殊表面或显式特殊表面、无动态弹性行为、卡死时回到最近安全地面，则 `layer_height_model_ready = true`、`cutaway_reveal_ready = true` 且 `physics_contract_complete = true`。若一个简单码头水平场景没有多层建筑和大型前景遮挡，也必须把 `cutaway_reveal_ready` 记录为 `N/A true`，而不是省略该项。

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
- [ ] GIVEN a `水平场景` contains buildings, mountains, bridges, caves, roofs, slopes, interiors, foreground canopies or visible height differences, WHEN implementation readiness is reviewed, THEN it declares a Layer / Height Model with `primary_walkable_layer` plus every reachable, unreachable, transition, height-only and blocked layer.
- [ ] GIVEN a scene is `垂直场景`, WHEN movement readability is reviewed, THEN left/right movement, depth layering, foreground/background separation, and vertical traversal methods such as jump, flight, ladders or stairs are specified.
- [ ] GIVEN a scene contains multi-floor buildings, ship compartments, towers, caves, underground spaces, tree houses, elevators or stacked rooms, WHEN design review begins, THEN it declares a Cutaway / Reveal Model that says how the active floor/room is shown and how non-active layers are hidden, faded, outlined or locked.
- [ ] GIVEN a reachable floor or height band exists, WHEN floor switching or vertical traversal is reviewed, THEN `floor_id`, `floor_index`, `is_active_floor`, `visibility_mode`, `walkable_bounds`, `vertical_connectors`, `occluders_hidden_above` and `interactions_enabled` are specified.
- [ ] GIVEN a scene contains gameplay-relevant units, WHEN implementation readiness is reviewed, THEN every unit declares collision behavior.
- [ ] GIVEN units overlap visually, WHEN readability is reviewed, THEN foreground, midground, background, Y-sort/floor sorting, temporary occluders, and flying-unit shadow/body layering are specified.
- [ ] GIVEN the player, NPCs, enemies, pushables or key interaction points can pass behind a building, tree, rock, ship body, bridge, market stall, wall or other large occluder, WHEN readability is reviewed, THEN that unit declares `behind_object_reveal` and preserves collision, interaction identity and spatial meaning while revealing the hidden subject.
- [ ] GIVEN a foreground occluder can cover the player or a core interaction, WHEN QA reviews the scene, THEN `occluder_peek`, fade, cutout, outline or equivalent reveal keeps identity readable within `identity_occlusion_max_seconds`.
- [ ] GIVEN a scene contains player, NPC, props, doors, passages or landmarks, WHEN asset readiness is reviewed, THEN each category has a relative size rule tied to `player_unit`.
- [ ] GIVEN a formal asset replaces a greybox unit, WHEN QA reviews the scene, THEN the replacement preserves collision footprint, occlusion behavior, interaction radius and size readability unless the physics contract is re-reviewed.
- [ ] GIVEN a scene contains water, mirror, glass, fog, cloud, transparent fabric, reflective metal, breakable floor, ledge or void, WHEN design review begins, THEN each special surface is marked as gameplay-affecting or `visual_only`.
- [ ] GIVEN a special surface is gameplay-affecting, WHEN implementation readiness is reviewed, THEN movement, collision, occlusion, reflection/refraction, interaction, audio, state and performance behavior are specified.
- [ ] GIVEN a special surface is `visual_only`, WHEN QA reviews it, THEN it does not mislead the player about passability, interactivity, danger, height or collision.
- [ ] GIVEN a scene unit has elastic, slippery, sticky, conveyor, moving-platform, one-way, climbable, breakable, deformable, hazardous, attracting, teleport, current/wind or trigger-only behavior, WHEN implementation readiness is reviewed, THEN behavior label, parameters, feedback, affected unit types and conflict priority are specified.
- [ ] GIVEN multiple physical behavior tags are combined on one unit or surface, WHEN design review runs, THEN priority and fallback rules are defined so collision, damage, movement and state updates cannot contradict each other.
- [ ] GIVEN a dynamic object can move, push, bounce, break, carry or attract the player, WHEN QA reviews the scene, THEN there is a visible escape, reset or recovery path from stuck states.
- [ ] GIVEN this GDD itself is reviewed, WHEN the user completes review, THEN missing physical unit requirements are added before status can move from `In Design` to `Approved`.
