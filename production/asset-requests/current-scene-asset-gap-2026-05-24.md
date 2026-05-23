# 当前场景美术与音频资产缺口门禁（2026-05-24）

**状态：BLOCKED。** 当前仓库没有 `assets/` 运行时美术或音频资源目录，现有可视化仍是 Godot 灰盒形状。以下资产未替换前，不允许进入下一阶段功能开发或系统扩展。

## 本次范围

- 当前可进入场景：标题/声场确认、玻璃港停泊浮岛、云织号船内、航图桌、雾海搜撤。
- 本次已补齐的缺失场景面：航图桌灰盒场景层，包含桌面、羊皮纸航图、玻璃港起点、雾海短程/旧集市航道、罗经和航线选择高亮。
- 仍缺失但必须补齐的场景：世界修复/解锁场景、旧集市交易场景。对应核心系统已存在，但当前可玩场景未呈现这些地点。

## P0 美术资产

| 资产 ID | 场景 | 缺失内容 | 最低验收 |
| --- | --- | --- | --- |
| `art.shell.title_background` | 标题/声场确认 | 《云海织航》标题背景、云海/飞艇轮廓、正式 Logo | 首屏不是纯 UI 面板；标题能读出游戏主题 |
| `art.shell.audio_prompt_panel` | 声场确认 | 船内声场确认面板、按钮底纹、静音继续状态 | 与航行日志/船内声场文案一致 |
| `art.hub.glass_harbor_dock_background` | 玻璃港停泊浮岛 | 浮岛主体、码头、云海地平线、玻璃港远景 | 1280x720 下不依赖文字也能看出“停泊浮岛+码头” |
| `art.hub.airship_exterior_cloudweaver` | 玻璃港停泊浮岛 | 云织号外观：船体、气囊、舱门、桅架、登船坡道 | 登船入口清晰，比例与玩家标记匹配 |
| `art.hub.airship_interior_background` | 云织号船内 | 船内剖面背景、驾驶舱、货舱、轮机间、走廊 | 三个功能舱室一眼可分辨 |
| `art.hub.helm_console` | 云织号船内 | 航图舵台/控制台 | 能作为打开航图的空间交互锚点 |
| `art.hub.storage_crates` | 云织号船内 | 仓储箱、货舱装载条、信标水晶箱 | 能表现空载、载货、收益锁定状态 |
| `art.hub.engine_bench` | 云织号船内 | 轮机间线圈、模块检修台、损伤覆盖层 | 能表现 100/100 与受损状态差异 |
| `art.chart.table_background` | 航图桌 | 木质航图桌、铜边、罗经、桌面阴影 | 航图模式不是空白文本面板 |
| `art.chart.parchment_map` | 航图桌 | 羊皮纸航图、海雾纹理、玻璃港起点 | 纸面可承载航线节点和风险标记 |
| `art.chart.route_nodes` | 航图桌 | 雾海短程、旧集市航道的路线线条、节点、选中高亮 | 选中航线有明确高亮状态 |
| `art.exploration.mist_island_background` | 雾海搜撤 | 雾海浮岛、路径、崖边、远景云海 | 玩家不看文字也能识别探索地点 |
| `art.exploration.return_airship` | 雾海搜撤 | 靠岸空艇、返航坡道、驾驶返航点 | 返航点与搜索点视觉上分离 |
| `art.exploration.search_wreck` | 雾海搜撤 | 雾灯残骸、桅杆、线索碎片、搜索高亮 | 三段扫描时有可见进度反馈 |
| `art.exploration.threat_zone` | 雾海搜撤 | 云影/剪云威胁区、警示覆盖层 | 中威胁出现时有非文字警示 |
| `art.exploration.return_beacon` | 雾海搜撤 | 返航信标、光束、收益货箱 | 收益锁定和撤离状态可视化 |
| `art.repair.lighthouse_scene` | 世界修复/解锁 | 星光灯塔/旧灯塔损坏与修复两态 | 修复前后地点变化可见 |
| `art.repair.material_handoff_fx` | 世界修复/解锁 | 修复材料提交、修复完成、航线解锁效果 | 与 #13 修复完成信号一致 |
| `art.market.old_market_scene` | 旧集市交易 | 旧集市背景、摊位、棚架、货架 | 能支撑 #14 集市交易场景 |
| `art.market.npc_and_stall_sprites` | 旧集市交易 | 阿图、韦师傅、云姨、岑测绘；杂货摊、透镜工坊、帆具铺、星图斋 | NPC 与摊位职责能被看出 |
| `art.items.goods_icons` | 货物/交易/奖励 | 基础物资包、修补帆布、透镜维护套件、抗风暴涂层、简易六分仪、航线手记、信标水晶 | 货舱、交易、奖励界面可复用 |
| `art.ui.feedback_icons` | 全局反馈 | 航线、威胁、修复、市场、库存、保存、读取图标 | 替代纯文字状态提示 |

## P0 音频资产

| 资产 ID | 场景 | 缺失内容 | 最低验收 |
| --- | --- | --- | --- |
| `audio.ambience.title` | 标题 | 标题云海氛围 | 可循环，进入声场确认前不刺耳 |
| `audio.ambience.glass_harbor` | 玻璃港停泊浮岛 | 港口风、云海、远处机械声 | 20-40 秒可循环 |
| `audio.ambience.airship_interior` | 云织号船内 | 低频引擎、木板/舱内环境 | 不遮盖 UI 点击 |
| `audio.ambience.chart_table` | 航图桌 | 舱内安静底噪、纸张/罗经轻响 | 打开航图后可明显切换 |
| `audio.ambience.mist_island` | 雾海搜撤 | 雾海风、岛屿环境、远处残骸声 | 搜撤开始后循环 |
| `audio.ambience.repair_site` | 世界修复/解锁 | 灯塔/修复点环境声 | 修复场景可用 |
| `audio.ambience.old_market` | 旧集市交易 | 市集人声、棚布、器械声 | 交易场景可用 |
| `audio.ui.button_click` | 全局 UI | 按钮点击 | 所有按钮复用 |
| `audio.ui.panel_open_close` | 全局 UI | 面板打开/关闭 | 航图、搜撤记录、日志面板复用 |
| `audio.chart.route_selected` | 航图桌 | 航线选中 | 对应 `audio.chart.route_selected` |
| `audio.chart.departure_confirmed` | 航图桌 | 确认离港 | 对应 `audio.chart.departure_confirmed` |
| `audio.exploration.scan_stage` | 雾海搜撤 | 扫描 1/3、2/3、打捞完成 | 三段搜索必须可听辨 |
| `audio.exploration.loot_pickup` | 雾海搜撤 | 信标水晶入货 | 与货舱反馈同步 |
| `audio.threat.response` | 雾海搜撤 | 威胁出现/回应 | 对应 `audio.threat.response` |
| `audio.hull.damage_warning` | 雾海搜撤/船内 | 船体擦伤、轮机受压 | 与船体完整度下降同步 |
| `audio.repair.submitted` | 世界修复/解锁 | 提交修复材料/修复完成 | 对应 `audio.repair.submitted` |
| `audio.market.purchase` | 旧集市交易 | 购买/售出/无法购买 | 对应 `audio.market.purchase` |
| `audio.inventory.transfer` | 货舱/集市 | 货物转移 | 对应 `audio.inventory.transfer` |
| `audio.session.save` | 航行日志 | 记录完成 | 对应 `audio.session.save` |
| `audio.session.load` | 航行日志 | 读取完成 | 对应 `audio.session.load` |

## P0 场景补齐要求

1. 航图桌：已补灰盒场景面，下一步必须用 `art.chart.*` 和 `audio.chart.*` 替换。
2. 玻璃港停泊浮岛：必须替换灰盒浮岛、码头、云织号外观和登船坡道。
3. 云织号船内：必须替换灰盒驾驶舱、货舱、轮机间、舵台、货箱和损伤覆盖层。
4. 雾海搜撤：必须替换灰盒浮岛、残骸、搜索线索、威胁区、返航信标和返航空艇。
5. 世界修复/解锁：当前缺少可进入场景。必须补 `星光灯塔/旧灯塔` 修复前后场景，接入修复完成反馈后才能关闭 #13 的可视化缺口。
6. 旧集市交易：当前缺少可进入场景。必须补旧集市、摊位、NPC、商品图标与购买反馈后才能关闭 #14 的可视化缺口。

## 门禁验收

- `assets/` 下存在正式图片、VFX、音频文件，并由 Godot 场景或资源加载路径引用。
- 当前可进入场景不得再以 ColorRect/Polygon2D 灰盒作为主视觉。
- 玩家可见文案不得出现 `HUD`、`MVP`、`会话壳`、`启用音频`、`返回 Hub` 这类实现阶段措辞。
- `tests/smoke/session_shell_visual_probe.gd` 通过，并保留航图桌、船内、搜撤场景的可见节点断言。
- 人工 QA 截图必须能在不读文字的情况下识别：停泊浮岛、云织号船内、航图桌、雾海搜撤点、返航点。
- 音频日志不得只走静默 fallback；上表中语义 cue 至少有一版临时正式音频。
