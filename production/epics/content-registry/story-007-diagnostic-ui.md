# Story 007: Diagnostic UI — Dev Tools

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-002`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0012: UI / Input Routing
**ADR Decision Summary**: Registry 的诊断 UI 是开发期工具面板集（非玩家界面）——提供 Registry Overview、Content Item Inspector、Reference Graph、Error List、Query Tester 和 Copyable Report Panel。Reference Graph 必须提供"只看错误链路"模式。Error List 支持按 severity/error_code/kind/domain 过滤。UI 仅在调试构建中可用。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: UI 使用 Godot Control 节点；图表使用 `draw_*` 方法或轻量 2D 节点；桌面调试工具不得依赖大量半透明层或持续粒子。

**Control Manifest Rules (Foundation layer)**:
- Required: 键盘优先（模式 #10）——所有诊断工具必须支持完整键盘操作
- Forbidden: 不得将 UNLOADED/NOT_FOUND/VERSION_INCOMPATIBLE 混为同一种"失败"
- Guardrail: UI 密度优先——工具布局（非营销卡片）

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN registry 存在任一错误，WHEN 打开开发期诊断工具，THEN 必须能看到 Registry Overview、Content Item Inspector、Reference Graph、Error List、Query Tester 五个面板
- [ ] **AC-2**: GIVEN 桌面调试工具打开 Registry Overview，WHEN registry 存在 fatal 或 error 诊断，THEN 高严重度问题必须在首屏可见
- [ ] **AC-3**: GIVEN 某条错误显示在诊断 UI，WHEN 查看或复制错误，THEN 必须包含 severity、error_code、content_id、source_ref、blocking_scope 和 suggested_action
- [ ] **AC-4**: GIVEN 错误列表存在多条错误，WHEN 使用批量复制，THEN 输出 Registry Diagnostic Summary 表格（含 severity/error_code/content_id/kind/field_path/blocking_scope/suggested_action）
- [ ] **AC-5**: GIVEN 桌面调试工具打开 Reference Graph，WHEN 引用图较大，THEN 必须提供"只看错误链路"模式，避免整图阻塞排查
- [ ] **AC-6**: GIVEN 用户不使用鼠标操作开发期诊断工具，WHEN 通过键盘导航，THEN 必须能访问筛选、Error List、Inspector、Reference Graph、Query Tester 和 Copyable Report Panel，并能看到当前焦点

---

## Implementation Notes

*Derived from ADR-0012 + GDD UI Requirements:*

- 5 面板布局: 左侧列表（Error List ← 可折叠）→ 中部详情（Content Item Inspector）→ 右侧诊断/引用（Reference Graph + Query Tester）
- Reference Graph 渲染: 节点=内容项（矩形+id），连线=引用（带箭头），错误节点=锯齿边框+锈红色
- 状态视觉编码（非颜色唯一）: Active=实线+✓、Draft=虚线+Draft标记、Deprecated=褪色+旧标签、Retired=断线+封存章、UNLOADED=半透明虚线、VERSION_INCOMPATIBLE=断裂边框+版本标记、Error=警示锈红+断裂三角
- Error List 过滤控件: 下拉选择 severity/error_code/kind/owner_domain/content_package
- 点击 Error 行→跳转 Content Item Inspector（高亮错误字段）
- 点击引用链节点→打开该节点 Inspector
- Query Tester: 输入 ID / kind / domain / tags → 执行只读查询 → 显示结果状态+实体+排序
- Copyable Report: 单条复制（纯文本 16 字段格式）+ 批量复制（Registry Diagnostic Summary 表格）
- 仅在 `OS.is_debug_build()` 时暴露诊断面板入口

---

## Out of Scope

- Story 006: 诊断数据生成——本 Story 只消费诊断事件，不产生
- Story 008: 玩家正式 UI 边界（ERR_* 不可见）——本 Story 仅在调试构建中可用
- UI/HUD System (#16): 正式玩家 UI 渲染框架——诊断 UI 是独立开发工具

---

## QA Test Cases

- **AC-1**: Five panels visible
  - Setup: 启动调试构建，注册表存在至少 1 个 error
  - Verify: 打开诊断工具→Registry Overview（内容包列表+健康状态）、Error List（错误表）、Content Item Inspector（选中项的字段）、Reference Graph（节点+连线）、Query Tester（输入框+执行按钮）
  - Pass condition: 五个面板均可见且有内容/有 placeholder（非空白）
  - Edge cases: registry 无错误→Error List 显示 "No errors"

- **AC-5**: Reference graph error-only mode
  - Setup: 注册表有 50+ 条目，其中 3 条有引用错误
  - Verify: 切换到"只看错误链路"→仅显示 3 个错误节点+它们的引用链
  - Pass condition: 非错误节点不在图中；切换回全部模式后所有节点恢复
  - Edge cases: 无错误→error-only 模式显示空白提示

- **AC-6**: Full keyboard navigation
  - Setup: 打开诊断工具，双手不碰鼠标
  - Verify: Tab 依次移动焦点→筛选→Error List→Inspector→Reference Graph→Query Tester→Copyable Panel；方向键在 Error List 中移动；Enter 打开详情；Ctrl+C 复制当前错误；Esc 返回 Error List
  - Pass condition: 所有面板可通过键盘到达并有可见 focus ring
  - Edge cases: 空列表时方向键不崩溃

---

## Test Evidence

**Story Type**: UI
**Required evidence**: `production/qa/evidence/diagnostic-ui-evidence.md` — manual walkthrough with screenshots + keyboard navigation verification
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 (Diagnostic System —— UI 消费诊断事件数据)
- Depends on: ADR-0012 (UI/Input Routing —— 面板模态管理和 input routing)
- Unlocks: None — Story 007 是 Registry Epic 的最终 UI 层
