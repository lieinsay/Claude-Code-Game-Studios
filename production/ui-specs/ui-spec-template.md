# UI 规格模板

> **用途**: 复制本文件为每个新 UI / HUD / 面板 / 覆盖层建立规格。
> **规则**: 本文件只保留模板字段、填写要求和检查项，不包含任何具体 UI 案例。
> **权威 GDD**: `design/gdd/ui-hud-chart-interface.md`
> **语言规则**: 除路径、代码符号、稳定 ID、状态枚举、命令和引擎/API 名称外，正文使用中文。

## 使用方式

1. 复制本文件到 `production/ui-specs/<ui-slug>.md`。
2. 将所有 `<...>` 占位符替换为真实内容。
3. 不适用字段写 `N/A true`，并说明原因。
4. 创建适合性结论必须为 `APPROVED` 或 `APPROVED_WITH_NOTES`，且备注已写回，才能进入 `implementation_ready`。
5. UI 可以解释、选择、确认、反馈或补充无障碍信息；不能替代场景或单位本体。

## 0. 文件头

| 字段 | 填写内容 |
| --- | --- |
| UI ID | `<ui.stable_id>` |
| UI 名称 | `<中文名>` |
| UI 分类 | `persistent_hud` / `anchored_panel` / `modal_dialog` / `semi_modal_overlay` / `full_screen_surface` / `toast_or_hint` / `debug_only` |
| 显示优先级 | P0 / P1 / P2 / P3 / P4 |
| 生命周期状态 | concept_needed / spec_drafted / implementation_ready / greybox / accepted / blocked |
| 创建适合性人工审查 | PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED |
| 来源 GDD | `<design/gdd/...md>` |
| 最近更新日期 | `<YYYY-MM-DD>` |
| 负责人 | Codex / user / QA / `<role>` |

## 1. UI 身份

| 字段 | 填写内容 |
| --- | --- |
| UI 是什么 | `<identity>` |
| 玩家为什么需要它 | `<player need>` |
| 服务的系统 / 场景 / 单位 | `<system/scene/unit links>` |
| 它不是什么 | `<explicit exclusions>` |
| 禁止用途 | `<what this UI must not replace or prove>` |

## 2. 独立实现 / 资产边界

| 字段 | 填写内容 |
| --- | --- |
| 独立 UI 场景 / 组件 | `<src/.../<ui>.tscn>` / `<component>` / `N/A true` |
| 配套脚本 / presenter | `<src/.../<ui>.cs>` / `<src/.../<ui>.gd>` / `N/A true` |
| 注册表条目 | `<UIManager key or registry row>` / `N/A true` |
| 资产组 | `<asset folder or manifest section>` / `N/A true` |
| 装配入口 | `<mounting scene/system>`，只负责挂载或引用 |
| 禁止混入位置 | `<legacy container/script>` / `N/A true` |
| 独立性说明 | 说明本 UI 如何被单独追踪、替换和删除 |

## 3. 显示分类与打开方式

| 字段 | 填写内容 |
| --- | --- |
| UI 分类 | `<classification>` |
| 显示优先级 | `<P0-P4>` |
| 是否可常驻 | 是 / 否 / 条件性 |
| 自动显示条件 | `<condition>` / `N/A true` |
| 手动打开方式 | 世界锚点 / 快捷键 / 按钮 / 菜单 / 无 |
| 绑定对象 | 系统状态 / 世界锚点 / 场景单位 / 场景阶段 / 调试 flag |
| 关闭方式 | Esc / 按钮 / 离开锚点 / 系统事件 / 自动超时 |
| 是否阻断世界输入 | 是 / 否 / 部分 |
| 是否抢占焦点 | 是 / 否 |
| 创建审查记录 | 本文件第 12 节，或链接到记录位置 |

## 4. 显示优先级与覆盖规则

| 情况 | 规则 |
| --- | --- |
| 更高优先级 UI 打开 | `<rule>` |
| 同优先级 UI 同时请求 | `<rule>` |
| 更低优先级 UI 已打开 | `<rule>` |
| 关闭后恢复 | `<rule>` |
| 数据变更时刷新 | `<rule>` |
| 输入层切换 | `<rule>` |

## 5. 内容与作用

| 内容区域 | 显示什么 | 数据来源 | 玩家动作 | 禁止用途 |
| --- | --- | --- | --- | --- |
| `<region_id>` | `<content>` | `<domain/source>` | `<action>` / `N/A true` | `<forbidden use>` |

## 6. 输入与焦点

| 输入 | 行为 | 可用条件 | 失败 / 禁用反馈 |
| --- | --- | --- | --- |
| `<input>` | `<behavior>` | `<condition>` | `<feedback>` |

- 初始焦点: `<control or N/A true>`
- Tab 顺序: `<order or N/A true>`
- Esc 行为: `<behavior>`
- 鼠标 / 键盘焦点同步: `<rule>`
- 模态时焦点是否锁定: 是 / 否 / N/A true
- 禁用控件是否可聚焦并显示原因: 是 / 否 / N/A true

## 7. 布局与遮挡边界

| 字段 | 填写内容 |
| --- | --- |
| 默认位置 | `<layout position>` |
| 最大屏幕占比 | `<percent or rule>` |
| 是否允许遮挡世界身份 | 是 / 否 / 条件性 |
| 是否允许遮挡核心交互锚点 | 是 / 否 / 条件性 |
| 小屏 / 大字体 / 长文本处理 | `<responsive/accessibility rule>` |
| 与 HUD / 模态 / toast 的层级关系 | `<z/layer rule>` |
| UI 不替代场景证明 | 说明本 UI 不能作为场景身份、物理单位或交互锚点证据 |

## 8. 状态与生命周期

| 状态 | 进入条件 | 显示 | 输入规则 | 退出条件 |
| --- | --- | --- | --- | --- |
| hidden | `<condition>` | `<visible content>` / none | `<input rule>` | `<exit>` |
| opening | `<condition>` | `<visible content>` | `<input rule>` | `<exit>` |
| active | `<condition>` | `<visible content>` | `<input rule>` | `<exit>` |
| disabled | `<condition>` | `<visible content>` | `<input rule>` | `<exit>` |
| closing | `<condition>` | `<visible content>` | `<input rule>` | `<exit>` |

## 9. 数据与运行时合同

- 读取的领域管理器: `<managers>` / `N/A true`
- 会调用的领域 API: `<apis>` / `N/A true`
- 持久化字段: `<fields>` / `N/A true`
- 信号 / 语义事件: `<events>` / `N/A true`
- 运行时 debug / smoke hook: `<hooks>` / `N/A true`
- 不允许写入的状态: `<forbidden state writes>`

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 操作 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | `<required asset/audio>` | identity / operation / feedback / accessibility | `<source>` / missing | `<owner>` |
| P1 | `<optional asset/audio>` | identity / operation / feedback / accessibility | `<source>` / missing | `<owner>` |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 输入 / 焦点测试 | `<test path or command>` | pending / pass / fail / N/A true |
| 显示优先级测试 | `<test path or command>` | pending / pass / fail / N/A true |
| 截图 / 视觉证明 | `<evidence path>` | pending / pass / fail / N/A true |
| 后续反馈记录 | `<notes or task link>` / `N/A true` | pending / recorded / N/A true |

## 12. 创建适合性记录

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | ui |
| 稳定 ID 或拟定 ID | `<ui.stable_id>` |
| 人工审查人 | `<user/reviewer>` |
| 审查日期 | `<YYYY-MM-DD>` |
| 结论 | PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED |
| 必须回写的备注 | `<notes>` / None |

审查问题摘要:

- 适合当前项目 / 阶段的原因: `<reason>`
- 不复用已有场景 / UI / 单位的原因: `<reason>`
- 主要范围风险: `<risk>`
- 必须写回规格的调整: `<required changes>` / None

## 13. 后续反馈与定向修改

创建适合性是进入规格和实现的唯一人工前置硬门。规格不再要求二次人工审核，也不要求实现后再做一次人工判定；实现后若用户提出反馈，只作为后续定向修改需求记录。

| 字段 | 内容 |
| --- | --- |
| 创建适合性结论 | PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED |
| 保持可修改状态 | `true` |
| 定向修改入口 | `directed-content-modification` |
| 用户反馈 / 后续定向修改需求 | `<notes>` / None |

后续反馈记录规则:

- [ ] 创建适合性人工审查已记录，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`。
- [ ] 人工备注已写回本规格、story 或后续任务。
- [ ] 独立实现 / 资产边界已明确，不能只混入旧 UI 容器、大脚本或临时节点。
- [ ] 实现后的反馈不得阻塞本规格既有一审结论。
- [ ] 新需求或调整通过 `directed-content-modification` 修改对应文档和实现。
