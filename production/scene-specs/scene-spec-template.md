# 场景规格模板

> **用途**: 复制本文件为每个新可进入场景建立规格。
> **规则**: 本文件只保留模板字段、填写要求和检查项，不包含任何具体场景案例。
> **依赖**: `design/gdd/scene-composition-system.md` 和 `design/gdd/scene-physics-unit-system.md`
> **语言规则**: 除路径、代码符号、稳定 ID、状态枚举、命令和引擎/API 名称外，正文使用中文。

## 使用方式

1. 复制本文件到 `production/scene-specs/<scene-slug>.md`。
2. 将所有 `<...>` 占位符替换为真实内容。
3. 不适用字段写 `N/A true`，并说明原因。
4. 创建适合性结论必须为 `APPROVED` 或 `APPROVED_WITH_NOTES`，且备注已写回，才能进入 `implementation_ready`。
5. 规格二次人工审核不是实现硬门；实现后反馈只记录为后续定向修改需求。

## 0. 文件头

| 字段 | 填写内容 |
| --- | --- |
| Scene ID | `<scene.stable_id>` |
| 玩家可见场景名 | `<中文名>` |
| 所属循环节点 | Hub / Chart / Exploration / Repair / Market / Settlement / Other |
| 当前生命周期状态 | concept_needed / spec_drafted / implementation_ready / greybox / asset_gate / playtest_ready / accepted / blocked |
| 来源 GDD | `<design/gdd/...md>` |
| 来源 story 或设计说明 | `<production/epics/...>` / `N/A true` |
| 创建适合性人工审查 | PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED |
| 创建审查记录 | 本文件第 13 节，或链接到记录位置 |
| 最近更新日期 | `<YYYY-MM-DD>` |
| 负责人 | Codex / user / QA / `<role>` |

## 1. 独立实现 / 资产边界

| 字段 | 填写内容 |
| --- | --- |
| 独立 Godot 场景 | `<src/.../<scene>.tscn>` / `N/A true` |
| 配套脚本 / runtime | `<src/.../<scene>.cs>` / `<src/.../<scene>.gd>` / `N/A true` |
| 作者化数据 | `<data file or registry path>` / `N/A true` |
| 资产组 | `<asset folder or manifest section>` / `N/A true` |
| 装配入口 | `<mounting scene/system>`，只负责挂载或引用 |
| 禁止混入位置 | `<legacy scene/script path>` / `N/A true` |
| 独立性说明 | 说明本场景如何被单独追踪、替换和删除 |

## 2. 场景身份

| 字段 | 填写内容 |
| --- | --- |
| 场景目的 | `<玩家为什么来到这里>` |
| 情绪目标 | `<安全 / 未知 / 压力 / 修复 / 生活感等>` |
| 服务的核心幻想 / 支柱 | `<对应支柱>` |
| 玩家 3 秒内应理解 | `<地点身份和主要可做事项>` |
| 本场景不是什么 | `<明确排除的范围>` |

## 3. 场景物理合同

本节只链接或摘要 #20 物理合同，不在这里重新定义全套物理规则。

| 字段 | 填写内容 |
| --- | --- |
| 物理来源 | Runtime contract / design spec / evidence doc / exemption |
| 合同场景 ID | `<physics_contract_id>` |
| `physics_contract_complete` 状态 | pass / fail / pending / exempt |
| 场景物理类型 | `水平场景` / `垂直场景` / `N/A true` |
| 移动平面 | `<链接或摘要>` |
| Layer / Height Model | `<链接或摘要>` |
| Cutaway / Reveal Model | `<链接或摘要>` |
| 单位目录 | `<链接或摘要>` |
| 固定单位原型 | `<production/unit-specs/fixed-scene-objects/...>` / `N/A true` |
| 实体单位原型 | `<production/unit-specs/dynamic-entities/...>` / `N/A true` |
| 摆放实例 | `<数据源或实例表>` / `N/A true` |
| 碰撞 / 遮挡 / 比例 | `<链接或摘要>` |
| 特殊表面 / 动态行为 / 恢复规则 | `<链接或摘要>` / `N/A true` |
| 无玩法相关物理单位时的豁免原因 | `<reason>` / `N/A true` |

## 4. 进入 / 离开

| 字段 | 填写内容 |
| --- | --- |
| 进入来源 | `<previous scene/system>` |
| 出生 / 抵达位置 | `<spawn point or anchor>` |
| 离开或返回路径 | `<exit path>` |
| 取消 / 失败路径 | `<cancel/fail behavior>` |
| 存档状态返回行为 | `<restore behavior>` |
| 场景切换清理预期 | `<cleanup requirements>` |

## 5. 空间布局

| 字段 | 填写内容 |
| --- | --- |
| 主视口构图 | `<composition>` |
| 可行走区域 | `<walkable bounds>` |
| 边界 | `<blocking / visual / hazard boundaries>` |
| 地标 | `<landmarks>` |
| 交互锚点 | `<world anchors>` |
| 遮挡风险 | `<occlusion risk and treatment>` / `N/A true` |
| 最低灰盒可读性要求 | `<greybox readability requirements>` |

## 6. 关键路径

| 步骤 | 场景动作 | 世界锚点 | 预期结果 |
| --- | --- | --- | --- |
| 1 | `<action>` | `<anchor>` | `<result>` |
| 2 | `<action>` | `<anchor>` | `<result>` |
| 3 | `<action>` | `<anchor>` | `<result>` |

## 7. 可选内容 / 可读性节拍

| 类型 | 填写内容 |
| --- | --- |
| 可选观察点 | `<optional beats>` / `N/A true` |
| 本地身份细节 | `<local identity details>` |
| 生活 / 修复 / 损伤痕迹 | `<stateful traces>` / `N/A true` |
| 嵌入世界中的玩家引导 | `<world guidance>` / `N/A true` |
| UI 辅助 | `<assistive UI only>` / `N/A true` |

## 8. 状态变体

除非明确豁免，否则至少记录三个状态变体。

| 变体 | 触发 / 来源状态 | 世界 / 可玩场景证据 | 允许的 UI 辅助 |
| --- | --- | --- | --- |
| 初始 | `<trigger>` | `<world evidence>` | `<assistive UI>` / `N/A true` |
| 进展 / 完成 | `<trigger>` | `<world evidence>` | `<assistive UI>` / `N/A true` |
| 阻塞 / 异常 | `<trigger>` | `<world evidence>` | `<assistive UI>` / `N/A true` |

## 9. 交互合同

| 锚点 ID | 玩家动作 | 输入 / 焦点规则 | 领域负责人 | 禁用 / 失败反馈 | 世界证据 |
| --- | --- | --- | --- | --- | --- |
| `<anchor_id>` | `<action>` | `<input/focus>` | `<domain owner>` | `<feedback>` | `<world evidence>` |

## 10. 数据 / 运行时合同

- Godot 场景或运行时表面: `<path or node>`
- 稳定 ID: `<stable ids>`
- 读取的领域管理器: `<managers>` / `N/A true`
- 会变更的领域管理器: `<managers>` / `N/A true`
- 持久化字段: `<fields>` / `N/A true`
- 信号 / 语义事件: `<events>` / `N/A true`
- 焦点和模态边界: `<rules>` / `N/A true`
- 运行时 debug / smoke hook: `<hooks>` / `N/A true`
- 不允许写入的状态: `<forbidden state writes>`

## 11. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | `<required asset/audio>` | identity / interaction / state_variant / feedback | `<source>` / missing | `<owner>` |
| P1 | `<optional asset/audio>` | identity / interaction / state_variant / feedback | `<source>` / missing | `<owner>` |

## 12. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 自动 smoke | `<command or test path>` | pending / pass / fail / N/A true |
| 截图 / 视觉证明 | `<evidence path>` | pending / pass / fail / N/A true |
| Codex 审核 | `<review path or note>` | pending / pass / blocked |
| 后续反馈记录 | `<notes or task link>` / `N/A true` | pending / recorded / N/A true |

实现后自检问题:

- 我在哪里？
- 不看开发说明，我能在这里做什么？
- 我如何离开或继续？
- 相关动作之后发生了什么变化？
- UI/HUD 是否只是辅助场景，而不是主导或替代场景？

## 13. 创建适合性记录

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | scene |
| 稳定 ID 或拟定 ID | `<scene.stable_id>` |
| 人工审查人 | `<user/reviewer>` |
| 审查日期 | `<YYYY-MM-DD>` |
| 结论 | PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED |
| 必须回写的备注 | `<notes>` / None |

审查问题摘要:

- 适合当前项目 / 阶段的原因: `<reason>`
- 不复用已有场景 / UI / 单位的原因: `<reason>`
- 主要范围风险: `<risk>`
- 必须写回规格的调整: `<required changes>` / None

## 14. 后续反馈与定向修改

创建适合性是进入规格和实现的唯一人工前置硬门。规格不再要求二次人工审核，也不要求实现后再做一次人工判定；实现后若用户提出反馈，只作为后续定向修改需求记录。

| 字段 | 内容 |
| --- | --- |
| 创建适合性结论 | PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED |
| 保持可修改状态 | `true` |
| 定向修改入口 | `directed-content-modification` |
| 用户反馈 / 后续定向修改需求 | `<notes>` / None |

后续反馈记录规则:

- [ ] 创建适合性人工审查已记录，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`。
- [ ] 创建适合性人工审查已通过；未通过时不能进入 `implementation_ready`。
- [ ] 人工备注已写回本规格、story 或后续任务。
- [ ] 独立实现 / 资产边界已明确，不能只散落在旧 Godot 节点、大脚本或临时灰盒中。
- [ ] 实现后的反馈不得阻塞本规格既有一审结论。
- [ ] 新需求或调整通过 `directed-content-modification` 修改对应文档和实现。

## 15. 就绪检查清单

- [ ] 场景目的、循环角色和情绪目标明确。
- [ ] 进入、离开、失败和返回路径明确。
- [ ] 空间布局列出可行走区域、边界、地标和交互锚点。
- [ ] Scene Physics Contract 已链接并通过，或 #20 豁免明确。
- [ ] 固定单位与实体单位已分开引用；单位本体规则不只散落在本场景规格中。
- [ ] 场景单位来自世界 / 可玩场景层，而不是 UI/HUD/按钮/标签/调试覆盖层。
- [ ] 关键路径和可选可读性节拍已记录。
- [ ] 至少三个状态变体已记录，或明确豁免。
- [ ] 交互锚点说明输入 / 焦点行为和领域负责人。
- [ ] 运行时 / 状态合同没有创建新的玩法权威。
- [ ] P0 资产 / 音频需求可追溯到身份、交互、状态或反馈。
- [ ] 自动证据、截图证据和规格一致性检查路径已命名。
