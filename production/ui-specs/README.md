# UI 规格目录

> **用途**: 存放 UI/HUD/面板/覆盖层的本体规格。
> **权威 GDD**: `design/gdd/ui-hud-chart-interface.md`
> **边界规则**: UI 负责解释、选择、确认、反馈和无障碍补充；不能替代场景单位、场景身份、世界交互锚点或物理证据。
> **语言规则**: 除路径、代码符号、稳定 ID、状态枚举、命令和引擎/API 名称外，本目录文档使用中文。

## 这个目录解决什么

`production/ui-specs/` 回答“这个 UI 本体是什么、什么时候能显示、如何打开、显示优先级是什么、会不会阻断输入、绑定哪个系统或场景对象”。
`production/scene-specs/` 回答“某个场景需要哪些 UI 辅助，但 UI 不能证明场景本体”。
`production/unit-specs/` 回答“世界对象本体是什么，UI 只能说明它，不能替代它”。

## UI 显示分类

| 分类 | 是否可常驻 | 打开方式 | 输入影响 | 示例 |
| --- | --- | --- | --- | --- |
| `persistent_hud` | 可以，但必须有场景 / 状态门控 | 场景或阶段激活自动显示 | 不阻挡世界交互，除非用户主动聚焦 HUD 子控件 | Hub HUD、探索 HUD |
| `anchored_panel` | 不常驻 | 玩家靠近世界锚点并按 Use，或绑定快捷键 | 非模态时允许移动；模态时阻断移动 | 仓库、站点、伙伴嗅辨 |
| `modal_dialog` | 不常驻 | 关键决策、系统事件或世界锚点触发 | 阻断世界输入，焦点锁在面板内 | 出航确认、修复提交、购买、命名 |
| `semi_modal_overlay` | 短时显示 | 世界动作进行中自动显示 | 部分阻断输入，规则必须写清楚 | 撤离读条、过渡遮罩 |
| `full_screen_surface` | 不与世界同时常驻 | 明确进入某个 UI 表面 | 接管输入，世界层暂停或隔离 | 航图屏幕 |
| `toast_or_hint` | 短时 | 系统反馈或失败原因 | 不抢焦点，不阻挡输入 | 禁用原因、短提示 |
| `debug_only` | 只在开发 / QA | debug flag 或 smoke hook | 不计入玩家体验证据 | 调试标签 |

## 显示优先级

| 优先级 | 层级 | 说明 |
| --- | --- | --- |
| P0 | 危机 / 阻断级 | 威胁、致命错误、不可逆确认、命名等必须立刻处理的内容。 |
| P1 | 关键决策级 | 出航、修复提交、购买确认、容量取舍等会改变领域状态的内容。 |
| P2 | 当前任务级 | 当前场景 HUD、探索进度、船体 / 货物 / 搜索状态。 |
| P3 | 辅助信息级 | 工具提示、短提示、禁用原因、可访问性补充。 |
| P4 | 调试 / 开发级 | smoke、diagnostic、debug overlay；不能作为玩家体验证据。 |

同一时间出现多个 UI 请求时，必须按优先级裁决。高优先级可以覆盖或排队，低优先级必须让位、折叠或延后。任何 UI 规格都必须写明自己属于哪个优先级，以及被更高优先级覆盖时如何恢复。

## 什么时候必须写 UI 规格

下列任一条件成立，就需要在本目录写独立 UI 规格：

- UI 会常驻屏幕，或在多个场景 / 状态中复用。
- UI 会打开、关闭、排队、覆盖、阻断输入或改变焦点。
- UI 会提交领域状态、触发不可逆操作或显示关键失败原因。
- UI 绑定世界锚点、场景单位、快捷键或某个系统事件。
- UI 有显示优先级、遮挡风险、无障碍要求或截图 / QA 证据要求。

## 创建适合性人工审查

任何新 UI 表面在进入实现或 `implementation_ready` 前，必须先按 `production/content-creation-review-gate.md` 记录人工适合性审查。结论只有 `APPROVED` 或 `APPROVED_WITH_NOTES` 时才允许继续；`PENDING`、`REVISE`、`REJECTED` 都会阻塞 story-readiness 和 `/dev-story`。

人工审查重点不是布局细节，而是判断是否应该创建这个 UI：能否复用现有表面、是否会替代世界对象、是否适合当前输入 / 焦点流程、是否造成玩家只点 UI 而不理解场景。

## 与场景和单位的关系

- 场景规格可以引用 UI 规格，但不能把 UI 当作场景本体。
- 单位规格可以引用 UI 规格作为状态说明或操作反馈，但不能让 UI 成为唯一交互实体。
- UI 规格必须声明绑定对象：系统状态、世界锚点、场景单位、快捷键、自动事件或开发调试。
- UI 常驻不代表 UI 永远可见；常驻 HUD 也必须有场景 / 状态 / 优先级门控。

## 当前规格文件

| 文件 | 覆盖范围 | 状态 |
| --- | --- | --- |
| `runtime-ui-surface-registry.md` | `src/presentation/UIManager.cs` 当前注册的 `S1`-`S12` 与诊断 UI | 已补真实规格 |
| `chart-full-screen-surface.md` | `S4_chart` 航图全屏表面 | 规格草案，待用户二审 |
| `ui-spec-template.md` | 新 UI 规格模板 | 模板 |

## 后续可拆分规格文件

| UI | 建议文件 | 分类 |
| --- | --- | --- |
| Hub 常驻 HUD | `hub-persistent-hud.md` | `persistent_hud` |
| 探索常驻 HUD | `exploration-persistent-hud.md` | `persistent_hud` |
| 出航确认 | `departure-confirmation-modal.md` | `modal_dialog` |
| 修复面板 | `repair-anchored-modal.md` | `modal_dialog` |
| 仓库 / 货舱整理 | `storage-anchored-panel.md` | `anchored_panel` |

当前运行时已有 UI 先由 `runtime-ui-surface-registry.md` 兜底约束。后续某个面板进入详细 UX / 视觉 / 无障碍实现时，再拆成独立文件，并从总表保留追踪链接。
