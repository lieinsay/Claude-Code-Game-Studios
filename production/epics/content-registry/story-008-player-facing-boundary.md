# Story 008: Player-Facing Boundary

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-003`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0002: Signal Communication Protocol
**ADR Decision Summary**: Registry 是纯后端系统——正式玩家 UI 不得暴露内部 ERR_* 错误码、诊断信息或开发期调试字段。玩家界面通过显示名键/描述键/标签/排序键消费内容定义。所有下游系统引用必须使用稳定 ID（非路径、显示文本或 UI 文案）。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 纯契约层——无引擎特定实现。依赖下游系统（UI/HUD #16、SessionShell #2）正确消费 Registry 查询结果。

**Control Manifest Rules (Foundation layer)**:
- Required: Registry 查询接口只返回静态内容定义——不返回 runtime instance 或可写句柄
- Forbidden: `hardcoded_value` — Hub/World/Exploration 引用必须使用稳定 ID
- Guardrail: 查询响应 <0.1ms（字典查找）

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN 正式玩家 UI 打开相关内容页面，WHEN 页面渲染，THEN 不得显示内部注册表、ERR_* 错误码、诊断代码或开发期调试字段
- [ ] **AC-2**: GIVEN `飞艇家园 Hub` 需要引用舱室/驻点或生活性交互对象，WHEN 这些内容进入注册表，THEN 必须使用 `home-space` 或 `home-anchor` 稳定 ID，不得用场景路径、显示文本或 UI 文案替代
- [ ] **AC-3**: GIVEN `home-space`、`home-anchor` 或 `companion` 的运行时状态发生升级/模块替换/关系进展，WHEN 保存或恢复该状态，THEN 状态变化必须引用原稳定 ID，不能通过替换静态 ID 表达
- [ ] **AC-4**: GIVEN 内容包损坏或域 FAILED 导致无法展示决策 UI，WHEN 玩家在正式流程中遇到，THEN 显示面向玩家的安全错误提示（非内部 ERR_* 代码）并提供可行动选项（重试、返回标题）

---

## Implementation Notes

*Derived from ADR-0001 + GDD Player-Facing Boundary:*

- Registry 提供两层查询接口:
  - `query_entity(id)` → 返回完整定义（供逻辑层使用）
  - `get_display_info(id)` → 返回 `{name_key, description_key, icon_ref, tags, sort_order}` 仅面向玩家的字段
- Player-facing 错误提示映射（非内部错误码）:
  - `VERSION_INCOMPATIBLE` → "游戏内容需要更新——请刷新页面"
  - `FAILED` → "无法加载游戏数据——请重试或联系支持"
  - `UNLOADED` → "正在加载..."（等待状态）
  - `NOT_FOUND` → 不应在玩家 UI 中出现（由逻辑层处理）
- 下游系统引用契约：
  - Hub 引用舱室: `references: [home-space.map-room]` ✓；`scene_path: "res://scenes/map_room.tscn"` ✗
  - Exploration 引用地点: `location_id: location.glass-harbor` ✓；`display_name: "Glass Harbor"` ✗
  - Chart 引用航线: `route_id: route.sky-reef-arc-01` ✓；`line_index: 0` ✗
- 所有玩家可见文本通过 `name_key` / `description_key` 引用本地化表——不直接在 Registry 中存储显示文本

---

## Out of Scope

- UI/HUD (#16): 玩家 UI 的具体渲染——Registry 只提供数据和接口约束
- Persistence (#3): 存档中的 ID 引用——Registry 提供 ID 稳定性和迁移提示，存档系统负责序列化
- Story 005: 域门控逻辑——本 Story 消费域状态信号并确保玩家 UI 的 fallback 行为
- Story 007: 开发期诊断 UI——本 Story 确保开发工具和玩家界面的分离

---

## QA Test Cases

- **AC-1**: Player UI never shows internal errors
  - Given: 正式构建（非 debug），registry 存在错误
  - When: 玩家打开航图/Hub/市场界面
  - Then: 不出现 ERR_* 前缀文本、internal error code、stack trace、reference_chain
  - Edge cases: 开发构建中可以展示 ERR_* 代码（用于调试）

- **AC-2**: Stable IDs in Hub references
  - Given: Hub GDD 定义舱室需要 `home-space.map-room`
  - When: 在 Registry 中查找该舱室的定义
  - Then: `id: home-space.map-room`，kind=home-space，通过稳定 ID 查询
  - Edge cases: 用场景路径 `res://scenes/map_room.tscn` 查询→NOT_FOUND

- **AC-4**: Safe error for corrupted content package
  - Given: 正式构建，`world` 域 FAILED
  - When: 玩家打开市场界面（依赖 world COMPLETE）
  - Then: UI 显示 "无法加载市场数据——请重试" + 重试按钮 + 返回标题按钮（不显示 ERR_CONTENT_PACKAGE_VERSION）
  - Edge cases: 重试成功后自动刷新 UI

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/registry/PlayerBoundaryTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 005 (Domain Loading & Decision Gating —— 玩家 UI 错误 fallback 依赖域状态)
- Depends on: ADR-0012 (UI/Input Routing —— 错误提示的 UI 渲染由 UIManager 提供)
- Unlocks: None — Story 008 是 Registry Epic 的最终集成边界
