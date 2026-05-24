# 完整场景构成与验收

> **Status**: Approved
> **Author**: User + Codex
> **Last Updated**: 2026-05-24
> **Implements Pillar**: 飞艇是家，不只是载具; 世界会回应照料; 规划先于冒险; 未知带来温和压力
> **Platform Pivot Note**: Active implementation targets desktop Godot 4.6.2 .NET/C#. This GDD governs scene design, runtime mounting, asset readiness, and QA evidence for authored playable scenes.

## Overview

完整场景构成与验收系统定义《云海织航》中每个“可进入场景”从设计到实现再到验收的统一标准。它不是新增一套玩法逻辑，也不是替代 Hub、航图、探索、修复、市场等已有系统；它是一层跨系统的场景设计契约，确保每个场景都同时具备空间结构、玩家行为、状态变化、视觉资产、音频反馈、技术接入和 QA 证据。

当前项目已经有系统 GDD、UX 规格、关卡模板、灰盒实现、资产缺口门禁和人工 QA 清单，但这些材料分散在不同目录中。最新 Polish 015 暴露的问题是：自动 smoke 能证明节点存在，却不能保证玩家在真实窗口里读出“岛屿/码头/船内/探索地”等场景身份。因此本系统把“场景是否完整”定义为一个可审查、可追踪、可阻断发布的标准。

本系统适用于所有当前和后续可进入场景。当前 demo 的 release-candidate 场景集合为：初始岛屿场景、雾灯残骸、旧集市边缘、连接初始岛屿与两个目的地的航行大场景、以及云织号船内分层水平场景。标题/声场确认、航图桌、修复点、未来村镇/空港扩展等可以作为后续场景或辅助界面继续纳入，但不得替代当前 demo 的真实场景边界。任何场景进入实现前必须拥有场景规格；任何场景进入 release gate 前必须通过双审：Codex 设计/实现审查 + 用户人工体验审查。

## Player Fantasy

玩家幻想不是“进入一个功能界面”，而是“站在一个能被看懂、能被使用、能回应状态变化的地方”。完整场景必须让玩家在不依赖开发者解释的情况下理解自己在哪里、为什么来、现在能做什么、做完后世界发生了什么。

**地点可信**：玻璃港不是背景文字，而是有浮岛、码头、船、停泊边界和登船路径的地方；云织号不是状态面板，而是有驾驶舱、货舱、轮机间、走廊和生活痕迹的家；雾海搜撤点不是空地加按钮，而是有残骸、线索、路径、危险和返航船位的探索地点。

**行动有身体感**：玩家应该通过移动、靠近、观察、选择、确认、撤离来完成场景行为。UI 可以辅助信息，但不能替代场景本身。场景中的主要交互必须拥有空间锚点，例如舵台打开航图、货箱承载容量状态、残骸承载搜索动作、灯塔承载修复提交、摊位承载交易。

**状态会留下痕迹**：场景应记住玩家行为。修复前后、搜刮前后、受损前后、返航前后、市场库存变化、伙伴巢穴变化，都应在空间、道具、光照、音频或 UI 摘要中留下可见证据。玩家不是在重置循环里切屏，而是在逐步修补一个世界。

**安全与未知都可读**：Hub 和船内强调归属、安全、整备；探索和高风险场景强调未知、轻压力和撤离判断；修复与市场强调世界回应和地方生活。完整场景必须把这些情绪目标写进规格，而不是只列功能点。

## Detailed Rules

### Core Rules

**R1 -- 场景规格先于实现。** 每个可进入场景在进入 runtime 实现前，必须有一份场景规格文档或等价章节，至少包含：场景目的、玩家入口/出口、空间结构、关键路径、交互锚点、状态变体、资产清单、音频/VFX 清单、技术接入、验收标准。短期 Polish story 可以先写在 `production/polish-backlog/`，但 release gate 前必须回写或链接到长期设计源。

**R1a -- 先定场景物理契约，再细化布局。** 每个 2D 可进入场景在布局、镜头、碰撞、交互、资产规格之前，必须按 `design/gdd/scene-physics-unit-system.md` 声明 Scene Physics Contract。未声明物理场景类型、移动平面、单位碰撞、遮挡、尺度和特殊行为的规格不得进入 `implementation_ready`。

**R1b -- 场景单位先定义，再验收场景。** 场景规格必须列出 world/playable scene layer 中的 `场景单位`：可移动角色、NPC、阻挡物、门、箱子、残骸、摊位、梯子、楼梯、平台、水面、玻璃、镜子、影子、高度标记、危险边界、可推动物、弹性物或可破坏物等。UI 控件、HUD 文本、按钮、菜单和调试标签不属于场景单位，不得作为场景构成或物理验收证据。

**R1b-1 -- 场景单位必须区分原型与实例。** 当一个场景进入 `implementation_ready` 或后续状态时，场景规格必须链接 `单位原型` 与 `摆放实例` 的数据来源：原型负责复用规则，实例负责本场景摆放。一个场景可以先用灰盒 Godot 节点摆放，但 release gate 前必须能追踪每个 gameplay-relevant 实例引用了哪个原型、位于哪个 floor/layer、来自哪个 scene spec，并且通过 #20 的 UI-evidence rejection。

**R1c -- 复杂水平场景必须先拆层。** 如果水平场景中出现楼房、屋顶、山体、桥、洞口、坡道、室内外、河岸、飞船、多层地貌或明显高低差，场景规格必须引用 #20 的 Layer / Height Model，标明每一层是可走层、视觉层、转移层、高度层还是阻挡层。未拆层的复杂水平场景不得进入 `implementation_ready`，因为玩家无法可靠判断哪里能走、哪里会挡、哪里只是背景。

**R1d -- 多层场景必须声明显隐剖切。** 如果玩家可以进入多层楼房、塔、山洞、地下空间、飞船舱段、树屋、桥下空间或任何上下层遮挡空间，场景规格必须引用 #20 的 Cutaway / Reveal Model。玩家到达第 N 层时，N 层以上如何隐藏、剖开、半透明或降亮，N 层以下如何保留参照，非当前层是否可交互，都必须写清楚。否则即使场景节点存在，也不得通过场景验收。

**R1e -- 大型前景遮挡必须声明后方显隐。** 如果玩家能走到楼房、大树、山石、船体、市场棚、桥墩、残骸或其他大型场景单位后方，场景规格必须说明 #20 的 `behind_object_reveal` 行为：遮挡物何时淡出、开洞、描边、降亮，玩家轮廓如何保留，碰撞和交互是否保持。不能因为玩家没进入楼内就忽略楼房遮挡；“从楼后经过”也是场景物理问题。

**R2 -- 场景是空间，不是文本仪表盘。** 主视觉区域必须首先传达地点身份。任何可进入场景不得依赖大段说明文字、HUD 文案或调试标签来证明“这里是哪里”。文字可以命名和补充，但不能承担主要空间识别职责。

**R2a -- 场景不是 UI。** 场景由 world/playable scene layer 中的地形、边界、单位、道具、入口、出口、遮挡、物理行为和状态痕迹构成；UI/HUD/按钮/文本面板只负责提示、确认、摘要和无障碍补充。验收时不得把 UI 面板存在、按钮可点击、标签文字正确当作“场景完整”的证据。若玩家关闭或忽略 HUD，仍必须能从场景本身读出地点、移动空间、关键锚点和危险边界。

**R3 -- 每个完整场景至少包含 7 条构成线。**

| 构成线 | 必须回答的问题 | 最低产物 |
| --- | --- | --- |
| 目的线 | 玩家为什么来到这里？本场景服务哪个核心循环节点？ | 场景目标 + 情绪目标 |
| 空间线 | 玩家从哪里进、往哪里走、从哪里离开？ | 布局说明或简图 |
| 行为线 | 玩家在这里做什么？如何失败、取消、完成？ | 关键路径 + 可选行为 |
| 状态线 | 初次、完成后、修复后、受损后如何变化？ | 状态变体表 |
| 表现线 | 不读文字能否识别场景身份和交互点？ | 美术/VFX/音频资产清单 |
| 技术线 | 哪些 Godot 节点、domain managers、存档字段和输入层参与？ | 技术接入契约 |
| 验收线 | 自动和人工分别证明什么？ | smoke 断言 + human QA 问题 |

**R4 -- 场景交互必须有空间锚点。** 核心动作不能只存在于 HUD 按钮上。航图从舵台/航图桌打开；搜索从残骸、线索、货箱或扫描点触发；返航从船、舵位或撤离锚点触发；修复从灯塔/设施/材料提交点触发；交易从 NPC 和摊位触发。UI 按钮可以作为辅助入口，但不得成为唯一入口，除非该场景本身就是全屏 UI 场景且规格明确说明。

**R4a -- 场景物理契约是场景完整性的一部分。** 每个包含可移动单位、阻挡物、可推动物、前后遮挡、特殊表面或动态物理行为的场景，都必须通过 `场景单位物理设计` 的 `physics_contract_complete` 门禁。#19 不重复定义物理细节；#20 是唯一的场景单位物理规范源。

**R5 -- UI 不得遮蔽世界身份。** HUD、状态摘要、按钮栏和调试信息必须让出主视口的地点识别空间。若人工 QA 无法在 3 秒内识别场景身份，视为场景完整性失败，即使自动节点断言通过。

**R6 -- 状态变体是完整场景的一部分。** 场景规格必须列出至少三个状态：进入前/初次状态、完成或推进后的状态、异常或阻断状态。对 Hub/船内可以是空载、返航带货、船体受损；对探索可以是未搜刮、已搜刮、危险变化；对修复可以是损坏、修复中、已修复；对市场可以是基础库存、修复后库存、无法购买/缺货。

**R7 -- 场景不得制造新领域权威。** 场景层拥有空间、锚点、表现和玩家可见状态同步；领域系统仍拥有规则、资源、修复、市场、探索、威胁、知识和存档真相。场景可以缓存 presentation state，但不能绕过 C# domain managers 或另建并行状态机。

**R8 -- 灰盒允许，但灰盒也必须可读。** 在 final art 缺失时，灰盒可以作为临时实现，但必须满足轮廓、尺度、分区、动线和交互点可读性。ColorRect/Polygon2D 可以证明布局，不得成为 release-ready 主视觉。

**R9 -- 资产门禁连接场景规格。** `production/asset-requests/` 中的资产缺口必须能追溯到场景规格中的资产清单。每个 P0 场景资产都应说明它解决哪一条场景识别、交互或状态变体需求。

**R10 -- 双审门禁。** 完整场景通过必须同时满足 Codex 审查与用户审查。Codex 审查关注文档完整性、跨系统一致性、技术接入、自动验证和回归风险；用户审查关注场景是否读得出来、是否符合想象、是否漏掉需求、是否值得继续实现。任一方给出 BLOCKED 时，场景不得进入 release gate。

**R11 -- 航行大场景是一等场景。** 当前 demo 中，从初始岛屿前往雾灯残骸、旧集市边缘的两条航道不得拆成两个孤立小 UI 流程；它们应合并到一个可独立设计、可独立验收的航行大场景中。该场景表现为伪 3D 航行：玩家视角始终与飞船前进方向保持一致，飞船可以拐弯、前进和后退，运动感主要由世界、云层、航标、远近地貌、风险物和目的地轮廓的变化来表达，而不是由固定镜头里一个小飞船图标移动来替代。航行大场景必须拥有自己的 Scene Physics Contract、镜头/运动读法、航道边界、风险可读性、返航/抵达规则和人工 readability 记录。

### States and Transitions

本系统不定义玩家可见 gameplay 状态机，而定义场景生命周期状态。

| State | Meaning | Allowed Next |
| --- | --- | --- |
| `concept_needed` | 只有系统需求，尚无场景规格 | `spec_drafted` |
| `spec_drafted` | 已写场景规格，未审查或未补齐 | `codex_review`, `user_review`, `blocked` |
| `codex_review` | Codex 检查结构、依赖、技术与 QA | `user_review`, `blocked`, `implementation_ready` |
| `user_review` | 用户检查体验、想象、遗漏需求 | `codex_review`, `blocked`, `implementation_ready` |
| `implementation_ready` | 双审通过，允许进入实现 story | `greybox`, `blocked` |
| `greybox` | 灰盒可运行，空间和交互锚点存在 | `asset_gate`, `blocked` |
| `asset_gate` | 资产清单与缺口门禁已建立 | `playtest_ready`, `blocked` |
| `playtest_ready` | 自动 smoke 通过，具备人工 QA 清单 | `accepted`, `blocked` |
| `accepted` | 自动 + 人工验收通过 | downstream release / polish |
| `blocked` | 缺关键需求、表现、资产、技术或 QA 证据 | return to relevant prior state |

状态推进规则：

1. `implementation_ready` 之前不得把场景当作最终需求执行，只能做探索性原型。
2. `greybox` 通过只证明空间和锚点存在，不证明 release readiness。
3. `asset_gate` 不要求所有 final art 已完成，但必须列出 P0 缺口和替换路径。
4. `accepted` 必须有人工 QA 结论，且结论不能是 BLOCKED。

### Required Scene Specification Shape

每个具体场景规格必须至少包含下列小节：

1. **Scene Identity**: 场景名、所属循环节点、情绪目标、服务的 Pillars。
2. **Scene Physics Contract**: 链接或嵌入 `场景单位物理设计` 要求的物理契约，包含场景物理类型、移动平面、Layer / Height Model、Cutaway / Reveal Model、单位目录、碰撞、遮挡、尺度、特殊表面、动态行为和恢复规则。
3. **Entry / Exit**: 进入来源、生成位置、出口、失败/取消路径、返回路径。
4. **Spatial Layout**: 主视口构图、玩家可走区域、地标、交互锚点、遮挡风险。
5. **Critical Path**: 玩家完成本场景的最短路径。
6. **Optional / Readability Beats**: 可选观察点、生活痕迹、地方身份、状态提示。
7. **State Variants**: 初次、完成后、异常/阻断、世界变化后的表现。
8. **Interaction Contract**: 使用的交互模式、输入、焦点隔离、确认门、禁用反馈。
9. **Data / Runtime Contract**: 读取和写入的 domain state、存档字段、稳定 ID、Godot 场景/节点。
10. **Asset and Audio Needs**: P0/P1 美术、VFX、音频、临时灰盒替代。
11. **QA Evidence**: 自动测试、截图、人工问题、通过/阻断标准。

### Initial Scene Coverage

| Scene | Current Status | Required Next Standard |
| --- | --- | --- |
| 初始岛屿场景 | 当前 demo 起点，旧资料中曾以 `hub_island_dock` / 玻璃港停泊浮岛记录 | 规格必须定义岛屿、码头、云织号外观、登船路径、离岛入口，以及返回后状态变化 |
| 云织号船内分层水平场景 | 旧资料中曾按船内/Hub 处理，但需要改为水平场景分层设计 | 规格必须定义驾驶舱、货舱、轮机间、走廊、生活痕迹、状态变体、水平移动平面、层级显隐、behind-object reveal 和舱段切换 |
| 航行大场景 | 需要新增独立场景设计；合并初始岛屿到雾灯残骸、初始岛屿到旧集市边缘两条航道 | 规格必须定义伪 3D 视角、飞船前进朝向锁定、转向/前进/后退、世界变化式运动表现、航道边界、风险物、返航和抵达规则 |
| 雾灯残骸 | 当前 demo 的一个目的地岛屿，不能再被泛化为“雾海搜撤” | 规格必须定义残骸轮廓、搜索/打捞锚点、危险边界、返航读法、与航行大场景的抵达衔接 |
| 旧集市边缘 | 当前 demo 的另一个目的地岛屿，旧资料只作为 future `market_scene` tracked gap | 规格必须定义旧集市边缘地貌、摊位/建筑轮廓、NPC 或交易前置锚点、可达/不可达边界、返回航行入口 |
| 标题/声场确认 | UI 可运行，正式主题资产缺失 | 首屏必须传达《云海织航》主题，不只是壳层按钮；不属于本轮场景 readability release gate 的核心场景 |
| 航图桌 | 当前应视为船内航行准备锚点或 UI-assisted surface | 不作为独立 release-candidate scene，除非后续明确拆为可进入物理场景并补 #20 contract |
| 世界修复/解锁点 | 系统存在，可进入场景缺失 | 后续必须新增灯塔/设施修复场景规格和资产门禁 |

## Formulas

### F-19-01 Scene Completeness Gate

```
scene_complete =
    purpose_ready
    AND scene_physics_ready
    AND space_ready
    AND behavior_ready
    AND state_ready
    AND presentation_ready
    AND technical_ready
    AND qa_ready
    AND codex_review_passed
    AND user_review_passed
```

| Variable | Type | True When |
| --- | --- | --- |
| `purpose_ready` | bool | 场景目标、情绪目标、核心循环位置明确 |
| `scene_physics_ready` | bool | 场景通过 `design/gdd/scene-physics-unit-system.md` 的 `physics_contract_complete` 门禁，或明确证明该场景不含 gameplay-relevant 物理单位 |
| `space_ready` | bool | 入口、出口、关键路径、可走区域、地标和交互锚点已定义 |
| `behavior_ready` | bool | 玩家主要行为、取消/失败/完成路径已定义 |
| `state_ready` | bool | 至少 3 个状态变体已定义，且说明状态来源 |
| `presentation_ready` | bool | 美术/VFX/音频需求列出，灰盒或正式资产能支持可读性 |
| `technical_ready` | bool | Godot 节点、domain manager、稳定 ID、存档/输入/焦点契约明确 |
| `qa_ready` | bool | 自动 smoke 和人工 QA 问题存在，验收标准明确 |
| `codex_review_passed` | bool | Codex 审查无 blocker |
| `user_review_passed` | bool | 用户审查无 blocker |

**Output Range:** true/false。任一变量为 false，场景不能进入 release gate。

**Example:** 雾海搜撤场景已有目的、通过 #20 的物理契约、三段搜索行为、未搜刮/已搜刮/危险变化状态、灰盒表现、Godot 节点契约和 smoke，但用户人工审查仍无法读出“这是一个可探索地点”，则 `user_review_passed = false`，`scene_complete = false`。

### F-19-02 Readability Pass

```
readability_pass =
    identity_3s
    AND primary_action_visible
    AND exit_visible
    AND hud_not_dominant
```

| Variable | Type | True When |
| --- | --- | --- |
| `identity_3s` | bool | 人工 QA 在 3 秒内能说出“这里是什么地方” |
| `primary_action_visible` | bool | 不读任务文本也能猜到主要交互点 |
| `exit_visible` | bool | 返航、离开、取消或下一步路径可见 |
| `hud_not_dominant` | bool | UI/HUD 没有压过主场景身份 |

**Output Range:** true/false。该公式必须由人工 QA 判定；自动测试只能辅助证明节点面积、z-order 和截图存在。

**Example:** 玻璃港停泊浮岛在 3 秒内可被识别为“岛屿+码头+船”，登船坡道可见，返航/离开路径可见，HUD 没有盖住主视口，则 `readability_pass = true`。若 tester 只能看到文本面板和按钮，即使节点存在，`hud_not_dominant = false`，公式返回 false。

### F-19-03 Scene Asset Traceability

```
asset_traceability = referenced_assets / required_p0_assets
```

| Variable | Type | Range | Description |
| --- | --- | --- | --- |
| `referenced_assets` | int | 0..N | 已在 Godot 场景、资源路径、规格或资产门禁中可追踪的 P0 资产数量 |
| `required_p0_assets` | int | 1..N | 场景规格列出的 P0 美术、VFX、音频资产数量 |

**Output Range:** 0.0 to 1.0。进入 release gate 前目标为 1.0；进入 greybox 阶段可低于 1.0，但必须有替代灰盒说明和缺口门禁。

**Example:** 航图桌规格列出 6 个 P0 资产，其中 4 个已经在资源路径或资产缺口门禁中可追踪，则 `asset_traceability = 4 / 6 = 0.67`。该场景可以继续灰盒验证，但不能通过 release gate。

## Edge Cases

1. **自动测试通过但人工读不出场景。** 以人工读不出为 blocker。更新 smoke 只能作为辅助，不能覆盖人工体验判断。
2. **场景有正式美术但玩法锚点不清。** 不通过完整场景验收。美术质量不能替代交互和状态契约。
3. **场景玩法可用但资产缺失。** 可进入 greybox 或 asset_gate，不得宣称 release-ready。
4. **UI 为了可用性临时承载动作。** 必须在场景规格中标记为 runtime bridge，并定义未来空间锚点替换路径。
5. **同一场景承担多个系统。** 场景规格必须声明每个系统的边界，避免场景层成为规则权威。
6. **状态变体来自多个 domain managers。** 场景层只读并组合显示，不能复制或推断不可验证状态。
7. **用户审查发现新增需求。** 不视为返工失败；将需求写回规格，状态退回 `spec_drafted` 或 `blocked`。
8. **Codex 审查与用户审查意见冲突。** 以用户体验目标为优先，但必须记录技术风险和验证补偿。
9. **场景被拆成多个 Godot 场景或同场景层切换。** 规格按玩家感知的可进入场景组织，不按文件边界组织。
10. **文案暴露实现阶段术语。** 玩家可见文案不得出现 `HUD`、`MVP`、`会话壳`、`返回 Hub` 等实现措辞；发现即阻断 release gate。
11. **截图看起来正确但交互焦点穿透。** 以 ADR-0012 和 `design/ux/interaction-patterns.md` 为准，必须修复焦点隔离。
12. **灰盒尺度误导后续资产。** 灰盒必须记录目标资产比例、交互半径和主视口占比，避免正式资产导入后推翻动线。物理细节按 `场景单位物理设计` 处理。

## Dependencies

Dependency note: this is a production/design gate, not a gameplay runtime system. The references below are authoritative inputs that this GDD must respect. Bidirectional tracking is maintained in `design/gdd/systems-index.md`; upstream gameplay GDDs do not need to add #19 as a gameplay dependent unless they are revised for scene-gate integration.

### Upstream Dependencies

| System / Doc | Dependency |
| --- | --- |
| `design/gdd/game-concept.md` | Core fantasy, pillars, visual identity anchor |
| `design/art/art-bible.md` | 航路修复主义、修补痕迹、航标化信息层、家园感 |
| `design/gdd/scene-physics-unit-system.md` | Scene Physics Contract: scene type, movement plane, Layer / Height Model, Cutaway / Reveal Model, behind-object reveal, collision, occlusion, unit scale, special surfaces, physical behaviors |
| `design/gdd/airship-hub.md` | Hub/ship interior spatial promise and home fantasy |
| `design/gdd/exploration-scavenge-scenario.md` | Exploration template, search, threat, extraction state |
| `design/gdd/world-repair-unlock.md` | Repair outcomes and visible world response |
| `design/gdd/port-village-market.md` | Settlement/market identity, NPC and stall interactions |
| `design/gdd/ui-hud-chart-interface.md` | Screen flow, modal stack, HUD and interaction constraints |
| `design/gdd/feedback-fx-audio.md` | Semantic VFX/audio ownership |
| `design/gdd/onboarding-first-loop.md` | First-loop clarity and player guidance |
| `design/ux/interaction-patterns.md` | Approach + E, confirmation gates, focus isolation, keyboard-first rules |
| `docs/architecture/adr-0001-autoload-scene-boot-order.md` | Scene lifecycle and transition budget |
| `docs/architecture/adr-0012-ui-input-routing-dual-focus.md` | UI/input routing and focus isolation |
| `docs/architecture/adr-0016-feedback-vfx-audio-semantics.md` | Feedback semantic event constraints |
| `docs/architecture/adr-0019-desktop-csharp-platform-pivot.md` | Desktop Godot .NET/C# implementation target |

### Downstream Consumers

| Consumer | Uses This System For |
| --- | --- |
| Polish backlog stories | Deciding whether a scene story is design-ready, implementation-ready, or blocked |
| Asset requests | Tracing P0 assets to scene identity, interaction and state needs |
| Smoke tests | Knowing which visible nodes, z-order, interaction anchors and screenshots matter |
| Human playtest checklists | Asking the right subjective readability and missing-requirement questions |
| Release checklist / gate-check | Blocking release readiness when scene completeness is false |
| Future scene specs | Reusing a consistent structure for repair, market, settlement and route scenes |
| `design/gdd/scene-physics-unit-system.md` | Supplies the required physics sub-contract for scene completeness |

## Tuning Knobs

| Knob | Default | Range | Owner | Notes |
| --- | --- | --- | --- | --- |
| `identity_read_seconds` | 3s | 2-5s | UX / QA | Time for human QA to identify scene without explanation |
| `minimum_state_variants` | 3 | 3-6 | Design | Initial, progressed, abnormal is the minimum |
| `primary_scene_viewport_share` | 65% | 55-85% | UX / Presentation | Main world identity should dominate over dashboard/UI |
| `required_p0_asset_traceability` | 1.0 | 0.8-1.0 | Producer / Art | Release gate target is 1.0 |
| `greybox_release_allowed` | false | false only | Producer | Greybox can unblock design/implementation, not release readiness |
| `codex_review_required` | true | true only | Production | Cannot be waived silently |
| `user_review_required` | true | true only | User / Production | User review is required to catch missing fantasy and requirements |
| `smoke_required_before_human_qa` | true | true/false | QA | Can be waived only for pure paper design, not runtime candidate |
| `screenshot_evidence_required` | true | true/false | QA | Required for visual scenes and release candidates |

## Acceptance Criteria

- [ ] GIVEN a new可进入场景 is proposed, WHEN design starts, THEN a scene specification exists before implementation work is treated as production-ready.
- [ ] GIVEN a 2D scene specification is drafted, WHEN review begins, THEN it includes or links a Scene Physics Contract that passes `design/gdd/scene-physics-unit-system.md`.
- [ ] GIVEN a scene has no gameplay-relevant physical units, WHEN review begins, THEN the spec explicitly states why #20 does not apply.
- [ ] GIVEN a scene specification exists, WHEN Codex reviews it, THEN purpose, space, behavior, state, presentation, technical and QA lines are all checked for blockers.
- [ ] GIVEN Codex review passes, WHEN the user reviews the same scene, THEN missing fantasy, missing requirements, unclear identity, or undesirable player flow can still block the scene.
- [ ] GIVEN either Codex or user review reports BLOCKED, WHEN production planning runs, THEN the scene cannot enter release gate until the blocker is resolved or explicitly waived by the user.
- [ ] GIVEN a scene reaches greybox, WHEN automated smoke runs, THEN tests verify visible scene identity nodes, main viewport coverage, interaction anchors, focus isolation and core route behavior relevant to that scene.
- [ ] GIVEN a scene reaches asset_gate, WHEN asset requests are audited, THEN every P0 asset maps back to a scene identity, interaction, state variant or feedback requirement.
- [ ] GIVEN a scene reaches playtest_ready, WHEN human QA evaluates it, THEN the tester can answer where they are, what they can do, how to leave, and what changed without developer guidance.
- [ ] GIVEN UI or HUD exists in a scene, WHEN visual QA checks the screen, THEN UI does not dominate or hide the world identity.
- [ ] GIVEN a scene depends on domain systems, WHEN implementation occurs, THEN the scene layer does not create a new gameplay authority or duplicate persistent state.
- [ ] GIVEN release readiness is discussed, WHEN any P0 current-scene asset gap remains unresolved, THEN the release gate stays blocked or explicitly records the waiver.
- [ ] GIVEN repair or market systems are implemented, WHEN this system is applied, THEN their可进入场景 specs must be created before they are considered visually complete.
- [ ] GIVEN this GDD itself is reviewed, WHEN the user completes review, THEN any missing demand is added here before status can move from `In Design` to `Approved`.
