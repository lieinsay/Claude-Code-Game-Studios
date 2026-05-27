# Godot Asset Context: initial_island_scene

## 原始请求

继续 Godot asset workflow，处理 `production/scene-specs/initial-island-scene.md` 对应的 `initial_island_scene` 独立场景资产。

## 已读取文件

- `AGENTS.md`
- `.codex/README.md`
- `.agentlens/INDEX.md`
- `CLAUDE.md`
- `production/session-state/active.md`
- `production/scene-specs/initial-island-scene.md`
- `src/presentation/playable_slice_authored_content.json`
- `src/scenes/HubRuntime.cs`
- `src/scenes/ship/ShipInteriorLayeredScene.tscn`
- `src/scenes/ship/ShipInteriorLayeredScene.cs`
- `tests/smoke/session_shell_visual_probe.gd`
- `tests/integration/playable-slice/DomainAdapterProgram.cs`

## 已知事实

- `initial_island_scene` 创建适合性为 `APPROVED`。
- 当前规格状态仍为 `spec_drafted`，独立 Godot 场景字段为 `pending`。
- 运行时合同 ID 保持 `hub_island_dock`，但旧 `HubRuntime` 外部码头灰盒不能继续作为 production-ready 场景证据。
- `ship_interior_layered` 已有独立场景资产，并作为登船后的船内空间。
- 初始岛屿必须作为进入 `ship_interior_layered` 的前置世界空间，保留码头、停靠飞船和登船路径证据。
- 项目文档默认中文；commit message 必须遵守 Lore commit protocol。

## 约束

- 不删除或替换旧 Godot 节点，除非用户明确批准 exact path。
- 不把 HUD、按钮、标签、调试入口或旧 `HubRuntime` 灰盒当作 production-ready 场景证据。
- 不引入新依赖，不改变存档格式，不扩展成村镇、市场、NPC hub 或新经济系统。
- 运行时仍由 Hub / Player Movement / Chart / Persistence 等既有系统拥有全局状态。

## 开放问题

- 无阻塞问题。本轮采用项目已批准的最窄安全假设：以 production-traceable greybox 独立场景完成资产化，不声明最终美术 / 音频完成。
