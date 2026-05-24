# 运行时 UI 表面规格总表

> **范围**: 已在 `src/presentation/UIManager.cs` 的 `BuildScreenRegistry()` 注册的 UI / HUD / 面板 / 覆盖层。
> **权威 GDD**: `design/gdd/ui-hud-chart-interface.md`
> **边界**: 本文件只证明 UI 本体、打开规则、输入层和系统归属；不能替代 `production/scene-specs/` 或 `production/unit-specs/` 的世界对象证据。

## 规格规则

- 表中每一行都是一个真实 UI 表面规格入口。后续如果某个 UI 需要更细的布局、文案、无障碍或截图验收，可以拆成独立文件，但必须保留本表索引。
- `debug_only` 表面只能用于开发 / QA 证据，不能作为玩家体验、场景完成度或单位存在性的证据。
- 非模态面板必须声明是否允许移动；模态、半模态和全屏表面必须声明焦点和输入阻断。
- 新增运行时 UI ID 时，必须同步更新本表，并通过 UI 状态机测试。

## 运行时表面

| UI ID | 中文名称 | 分类 | 优先级 | 打开 / 显示门槛 | 输入与焦点 | 归属系统 | 绑定对象或场景锚点 | 验证 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `S1_hub_hud` | Hub 常驻 HUD | `persistent_hud` | P2 | `Hub` / `HubArriving` 可玩状态自动显示 | 默认不抢焦点，不阻断移动；高优先级模态覆盖时隐藏或降级 | `hub` | 初始岛屿、停靠船体、码头读法 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S2_station_detail` | 站点详情面板 | `anchored_panel` | P2 | 玩家对 hub 站点锚点执行 Use | 非模态；允许移动；焦点只在主动导航面板时进入 | `hub` | hub station / 设施锚点 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S3_departure_confirm` | 出航确认弹窗 | `modal_dialog` | P0 | 登船坡道或 helm 锚点触发出航确认 | 模态；阻断世界输入；焦点锁在确认 / 取消 | `hub` | `scene_unit.prototype.hub_boarding_ramp`、`scene_unit.prototype.helm_console` | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S4_chart` | 航图全屏表面 | `full_screen_surface` | P1 | `M` 键、出航锁完成或航图入口打开 | 全屏接管输入；世界层隔离；Esc 在可逆状态返回 Hub | `chart` | 船内航图 / helm 操作语义 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S5_exploration_hud` | 探索常驻 HUD | `persistent_hud` | P2 | `Exploration` / `Extracting` 阶段自动显示 | 默认不抢焦点；半模态读条可覆盖部分输入 | `exploration` | 探索岛屿、搜索点、威胁反馈 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S6a_capacity_choice` | 容量取舍弹窗 | `modal_dialog` | P1 | 搜索奖励或货物压力超过可携带容量 | 模态；阻断移动和继续搜索；必须选择丢弃或保留 | `resources` | 资源 / 货舱状态 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S6b_extraction_progress` | 撤离 / 提取进度覆盖层 | `semi_modal_overlay` | P1 | 撤离或提取动作开始 | 半模态；限制取消和移动规则；进度完成前不可静默关闭 | `exploration` | 搜索点完成、返航动作 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S6c_settlement_summary` | 结算摘要弹窗 | `modal_dialog` | P1 | 探索结算完成 | 模态；确认前阻断新输入；确认后进入返航 / Hub 到达 | `exploration` | 探索奖励、损伤、携带物结算 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S7_combat` | 战斗 / 威胁弹窗 | `modal_dialog` | P0 | 威胁进入战斗或强制处理状态 | 模态；危机状态 Esc 不得绕过；结果写回领域状态 | `combat` | `scene_unit.prototype.exploration_threat_zone` | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S8_repair` | 修复面板 | `modal_dialog` | P1 | 修复锚点、船体状态或世界修复请求打开 | 模态；提交会改变修复 / 资源状态；关闭不应提交 | `world-repair` | 船体、修复锚点、材料状态 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S9_market` | 市场交易面板 | `modal_dialog` | P1 | settlement / 市场摊位锚点打开 | 模态；购买 / 出售确认写回资源状态 | `settlement` | 市场摊位、港口交易语义 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S10_naming` | 命名弹窗 | `modal_dialog` | P0 | 到达后存在命名资格或强制命名事件 | 模态；命名前阻断离开关键流 | `partner` | 伙伴 / 船名 / 到达事件 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S11_partner_sniff` | 伙伴嗅辨面板 | `anchored_panel` | P2 | 伙伴嗅辨锚点或系统事件打开 | 非模态；允许移动；只在主动聚焦时占用导航 | `partner` | 伙伴实体或嗅辨提示锚点 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `S12_storage` | 仓库 / 货舱面板 | `anchored_panel` | P2 | 仓库、货舱或资源锚点打开 | 非模态；允许移动；提交操作写回资源状态 | `resources` | `scene_unit.prototype.storage_crate`、船舱货舱 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |
| `registry_diagnostic_tools` | 注册表诊断工具 | `debug_only` | P4 | debug / QA 入口显式打开 | 不进入玩家输入优先级；不得出现在玩家验收截图中 | `registry` | 开发诊断 | `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` |

## 追踪要求

运行时新增或删除 `UIManager` 屏幕 ID 时，必须同步：

- 本文件的表格行。
- `tests/unit/ui-hud-interface/ScreenStateMachineProgram.cs` 中的注册表规格检查。
- 如 UI 绑定具体世界锚点，相关单位规格或场景规格也必须引用该 UI ID。
