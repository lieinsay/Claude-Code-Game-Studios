# 赭石岛场景单位与灰盒运行时证据

> **日期**: 2026-05-25  
> **范围**: `ochre_island_scene`、`scene_unit.prototype.banded_iron_ore`、`route.ochre`  
> **结果**: 自动证据 PASS；release 仍因截图 / P0 资产等缺口阻塞  
> **证据边界**: UI、HUD、按钮、标签和调试覆盖层只能辅助断言；场景单位证据必须来自世界 / 可玩场景层、作者化数据或 #20 运行时合同。

## 变更摘要

- `src/presentation/playable_slice_authored_content.json` 新增赭石岛 scene-unit 原型 / 实例、`route.ochre`、`sp.ochre.1/2/3`，并为雾岛搜索点补齐 `route_id`。
- `src/core/content/Registry.cs` 注册 `resource.iron-ore`，供 ResourcesManager 校验和入库。
- `src/presentation/PlayableSliceDomainAdapter.cs` 按航线映射 `ExplorationSceneId`、搜索点和奖励资源；`route.ochre` 使用 `ochre_island_scene` 并发放 `resource.iron-ore`。
- `src/scenes/HubRuntime.cs` 新增赭石岛灰盒场景、航图选择、矿脉采集微交互、返航预热、采集后 overlay、#20 物理合同和当前场景 debug 快照。
- `tests/integration/playable-slice/DomainAdapterProgram.cs` 校验赭石岛作者化数据、资源注册、铁矿 carried / storage 流和 `ochre_island_scene` scene-unit 链路。
- `tests/smoke/session_shell_visual_probe.gd` 覆盖赭石岛航线选择、独立场景可见性、雾岛物件隐藏、#20 合同、三段采集、返航和铁矿入库。

## 验证命令

| 命令 | 结果 | 备注 |
| --- | --- | --- |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS | 0 警告 / 0 错误。 |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj --no-restore -p:UseSharedCompilation=false` | PASS | `RESULT 987/987 passing`。 |
| `D:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | 赭石岛路线、#20 合同、采集和返航断言通过；headless 显示驱动跳过截图保存。 |
| `git diff --check` | PASS | 仅出现 Windows CRLF 转换提示，无空白错误。 |

## 验收映射

| 验收点 | 证据 |
| --- | --- |
| 赭石岛不是 UI 或旧市场复用 | `HubRuntime` 使用独立 `ochreSceneItems`，smoke 断言 `OchreIslandMass` / `BandedIronOreVein` 可见且 `SearchWreckProp` / `ExplorationSkyField` 隐藏。 |
| 场景单位来自作者化数据 | `DomainAdapterProgram` 和 Godot smoke 校验 `scene_unit.instance.ochre_island_scene.*` 的 prototype、scene spec、floor、Godot placement reference 和 UI evidence rejection。 |
| #20 物理合同完整 | Godot smoke 对 `DebugScenePhysicsContract("ochre_island_scene")` 执行合同形状、Layer / Height、Cutaway / Reveal、Floor State、碰撞 / 遮挡 / 比例、动态行为和恢复规则校验。 |
| 条带状铁矿是固定场景单位 | 作者化原型 `scene_unit.prototype.banded_iron_ore` 链接 `production/unit-specs/fixed-scene-objects/banded-iron-ore.md`，实例放置于 `ochre_island_ground_01`。 |
| 铁矿资源能进入领域系统 | `Registry` 注册 `resource.iron-ore`；集成测试验证 carried 池可接受铁矿，赭石岛三次采集后 carried 为 3，返航后 storage 为 3。 |
| 搜索公式不会误判空采集 | `PlayableSliceDomainAdapter.SearchFormulaZone` 将赭石岛作者化区段 `ore_core` / `ore_return` 映射到已批准的 `A_core` 搜索公式，保留场景区段语义。 |
| 二次出航会刷新 Navigation 上下文 | `NavigationManager.ResetToIdleForNextVoyage()` 由 playable adapter 在下一段航程前调用，Godot smoke 验证赭石岛抵达目的地为 `location.ochre-island`。 |

## 剩余风险

- 这是灰盒运行时证据，不是最终美术、音频或资产替换完成声明。
- headless smoke 当前无法保存截图；release packet 仍需要窗口化截图 / 人工视觉证明，覆盖首屏、矿脉、返航点和采集后状态。
- `voyage_open_world_scene` 的独立 #20 合同和运行时证据仍缺失，整体 Scene Composition release handoff 继续 `BLOCKED_FOR_RELEASE`。
- 禁用 / 容量不足反馈只在通用流程中保留，赭石岛特定容量满场景还需要后续 QA 或 waiver。

## 推荐下一步

1. 运行窗口化 Godot smoke 或人工截图，补 `ochre_island_scene` 首屏、矿脉、采集后状态和返航点图证。
2. 为赭色岛体、条带状铁矿、采集反馈音效建立 P0 资产处理记录或用户 waiver。
3. 继续补 `voyage_open_world_scene` 的 #20 合同和运行时证据，降低整体 release handoff 阻塞。
