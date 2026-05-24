# 作者化可玩切片固定单位规格

> **范围**: `src/presentation/playable_slice_authored_content.json` 中 `prototype_classification = fixed_scene_object` 的真实 `scene_unit.prototype.*`。
> **权威 GDD**: `design/gdd/scene-physics-unit-system.md`
> **边界**: 固定单位证明世界对象本体、碰撞、遮挡、尺度和复用规则；UI 面板只能引用这些对象，不能替代它们。

## 通用规则

- 每个原型必须在 `playable_slice_authored_content.json` 的 `unit_spec` 字段回指本文件。
- 固定单位的位置由 `scene_unit_instances` 决定；本文件定义可复用本体。
- `source_layer` 必须保持为 `world_playable_scene`。
- `ui_evidence_allowed` 必须为 `false` 或缺省为 false 语义。
- 新增固定单位原型时，必须补本表并通过 authored content 合同测试。

## 固定单位原型

| Prototype ID | 中文名称 | 可出现的场景 | 单位类型 | 碰撞 | 遮挡 | 尺度 / 复用规则 | UI 边界 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `scene_unit.prototype.hub_island_main_mass` | Hub 岛屿主地形 | `hub_island_dock` | 地形基底 | 阻挡 / 承载 | 地形层 | 定义玩家可站立和码头接地读法 | UI 不能证明岛屿存在 |
| `scene_unit.prototype.hub_dock_plank_walkway` | 码头木板通道 | `hub_island_dock` | 通行结构 | 可通行 / 边缘阻挡 | 地面结构 | 连接岛屿和登船动线 | 可触发提示，但不能替代通道 |
| `scene_unit.prototype.hub_docked_ship_hull` | 停靠船体 | `hub_island_dock` | 船体结构 | 阻挡 | 前景 / 中景 | 提供出航幻想和船体边界 | 出航 UI 必须绑定真实船体或坡道 |
| `scene_unit.prototype.hub_boarding_ramp` | 登船坡道 | `hub_island_dock` | 交互通道 | 可通行 / 交互 | 通道层 | 连接码头和船体入口 | 可打开 `S3_departure_confirm` |
| `scene_unit.prototype.hub_airship_envelope` | 飞艇气囊 | `hub_island_dock` | 大型装饰 / 轮廓 | 阻挡或高层不可达 | 高层轮廓 | 强化飞行船识别 | HUD 不能替代轮廓读法 |
| `scene_unit.prototype.hub_waterline` | 水线边界 | `hub_island_dock` | 场景边界 | 阻挡 / 危险边界 | 地表边缘 | 标记不可通行外缘 | UI 只能提示，不能替代边界 |
| `scene_unit.prototype.ship_hull_outline` | 船舱外轮廓 | `hub_ship_interior` | 室内边界 | 阻挡 | 墙体 / 壳体 | 定义船内空间边界 | 航图 UI 不能证明船舱存在 |
| `scene_unit.prototype.ship_room_bay` | 船舱功能舱位 | `hub_ship_interior` | 房间区块 | 区域边界 | 房间层 | 可复用于 cockpit / cargo / engine bay | 面板只说明功能，不替代舱位 |
| `scene_unit.prototype.helm_console` | 舵台 / helm 控制台 | `hub_ship_interior` | 交互控制台 | 阻挡 / 交互 | 前景物件 | 打开航图和出航确认的世界锚点 | 可打开 `S3_departure_confirm`、`S4_chart` |
| `scene_unit.prototype.storage_crate` | 仓库箱 / 货舱箱 | `hub_ship_interior` | 资源锚点 | 阻挡 / 交互 | 前景物件 | 绑定资源和仓库访问 | 可打开 `S12_storage` |
| `scene_unit.prototype.ship_exit_threshold` | 船舱出口阈值 | `hub_ship_interior` | 通道阈值 | 触发 / 通行 | 门槛层 | 标记室内外切换 | UI 只能提示离开，不能替代出口 |
| `scene_unit.prototype.cockpit_window_glass` | 驾驶舱窗玻璃 | `hub_ship_interior` | 透明结构 | 阻挡 | 透明前景 | 提供外部方向和驾驶舱读法 | 不承载交互 UI |
| `scene_unit.prototype.upper_hull_front_wall` | 上层船首墙体 | `hub_ship_interior` | 墙体结构 | 阻挡 | 高层墙体 | 约束船内视线和动线 | 不承载交互 UI |
| `scene_unit.prototype.exploration_island_mass` | 探索岛屿主地形 | `exploration_mist_island` | 地形基底 | 阻挡 / 承载 | 地形层 | 定义雾岛可玩区域 | 探索 HUD 不能证明地形存在 |
| `scene_unit.prototype.exploration_path` | 探索路径 | `exploration_mist_island` | 通行动线 | 可通行 | 地面路径 | 串联搜索点和返航点 | UI 只能提示方向 |
| `scene_unit.prototype.exploration_cliff_edge` | 探索悬崖边界 | `exploration_mist_island` | 危险 / 视觉边界 | 阻挡 | 边缘层 | 限制玩家越界和读法 | UI 只能提示危险 |
| `scene_unit.prototype.search_wreck` | 搜索残骸主体 | `exploration_mist_island` | 搜索锚点 | 阻挡 / 交互 | 前景残骸 | 搜索奖励和叙事读法来源 | 搜索 UI 必须绑定真实残骸 |
| `scene_unit.prototype.search_wreck_mast` | 残骸桅杆 | `exploration_mist_island` | 残骸部件 | 阻挡或视觉遮挡 | 高层残骸 | 强化残骸轮廓和方向性 | 不独立替代搜索点 |
| `scene_unit.prototype.return_ship_hull` | 返航船体 | `exploration_mist_island` | 返航锚点 | 阻挡 / 交互 | 船体层 | 标记返回船只位置 | 撤离 UI 必须绑定真实返航船体 |
| `scene_unit.prototype.return_helm_anchor` | 返航 helm 锚点 | `exploration_mist_island` | 交互锚点 | 交互 / 触发 | 前景物件 | 触发返航或撤离读条 | 可触发 `S6b_extraction_progress` |
| `scene_unit.prototype.mist_sea_boundary` | 雾海边界 | `exploration_mist_island` | 场景边界 | 阻挡 / 危险边界 | 远景边界 | 限制外海不可达区域 | UI 不能替代边界 |
| `scene_unit.prototype.mist_horizon_fog` | 雾海远景雾 | `exploration_mist_island` | 远景层 | 无直接交互 | 远景遮挡 | 建立雾海氛围和可视距离 | 只允许作为视觉证据 |
| `scene_unit.prototype.return_beacon_beam` | 返航信标光束 | `exploration_mist_island` | 导航标识 | 触发或视觉引导 | 特效层 | 指示返航方向，不替代返航船体 | UI 可引用方向，但不能替代光束 |

## 验收

- `tests/integration/playable-slice/DomainAdapterProgram.cs` 必须验证每个固定单位原型有 `unit_spec`、规格文件存在，且文件包含对应 `Prototype ID`。
- 具体摆放、楼层、层级和 scene spec 追踪仍由 `scene_unit_instances` 负责。
