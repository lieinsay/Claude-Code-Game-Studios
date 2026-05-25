# 航行大场景单位与灰盒运行时证据

> **日期**: 2026-05-25  
> **范围**: `voyage_open_world_scene`  
> **结果**: 自动证据 PASS；release 仍因截图 / P0 资产等缺口阻塞  
> **证据边界**: UI、HUD、按钮、标签和调试覆盖层只能辅助断言；场景单位证据必须来自世界 / 可玩场景层、作者化数据或 #20 运行时合同。

## 变更摘要

- `production/scene-specs/voyage-open-world-scene.md` 从 `spec_drafted` 更新为 `runtime_greybox_evidence_added`，记录本次定向修改、#20 合同、运行时边界和剩余 release 风险。
- `src/presentation/playable_slice_authored_content.json` 新增航行大场景 scene-unit 原型 / 实例，覆盖玩家标记、航道、船首前景、航标链、雾带、残骸、大鸟剪影、目的地轮廓和撤退信标。
- `src/scenes/HubRuntime.cs` 新增 `voyageSceneItems` 灰盒组、`DebugShowVoyageOpenWorldScene(route_id)`、`DebugVoyageSceneSnapshot()` 和 `DebugScenePhysicsContract("voyage_open_world_scene")`。
- `tests/smoke/session_shell_visual_probe.gd` 覆盖航行大场景 #20 合同、作者化链路、灰盒节点可见性、当前合同跟随 debug 预览、风险窗口和 UI-only 证据拒绝。
- `tests/integration/playable-slice/DomainAdapterProgram.cs` 校验 `voyage_open_world_scene` 作者化原型 / 实例、floor、scene spec 和 runtime catalog 单位覆盖。

## 验证命令

| 命令 | 结果 | 备注 |
| --- | --- | --- |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS | 0 警告 / 0 错误。 |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj --no-restore -p:UseSharedCompilation=false` | PASS | 覆盖作者化数据和 `voyage_open_world_scene` scene-unit 链路。 |
| `D:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | 覆盖航行大场景 #20 合同、debug 快照、灰盒节点和既有赭石岛闭环；headless 显示驱动截图限制仍需窗口化图证。 |
| `git diff --check` | PASS | 仅出现 Windows CRLF 转换提示，无空白错误。 |

## 验收映射

| 验收点 | 证据 |
| --- | --- |
| 航行大场景不是航图 UI 或等待条 | `HubRuntime` 使用独立 `voyageSceneItems` 灰盒组；smoke 通过 `DebugShowVoyageOpenWorldScene("route.ochre")` 进入 `currentScreen == "voyage"` 并断言当前合同为 `voyage_open_world_scene`。 |
| 场景单位来自作者化数据 | `DomainAdapterProgram` 和 Godot smoke 校验 `scene_unit.instance.voyage_open_world_scene.*` 的 prototype、scene spec、floor、Godot placement reference 和 UI evidence rejection。 |
| #20 物理合同完整 | Godot smoke 对 `DebugScenePhysicsContract("voyage_open_world_scene")` 执行合同形状、Layer / Height、Cutaway / Reveal、Floor State、碰撞 / 遮挡 / 比例、动态行为和恢复规则校验。 |
| 主动驾驶视角可读 | `VoyageShipProwForeground`、`VoyageAirLane`、`VoyageBeaconChain` 和 `VoyageDestinationSilhouette` 在世界层可见；`DebugVoyageSceneSnapshot()` 声明 `active_pilot_view_ready` 和 `destination_silhouette_ready`。 |
| 风险窗口可追踪 | `VoyageFogBank`、`VoyageDebrisField`、`VoyageBirdShadow` 分别对应雾带、残骸和大鸟临时避险窗口；debug 快照暴露 `risk_windows=fog_band,debris_field,bird_shadow`。 |
| UI-only 证据被拒绝 | 合同、作者化数据和 debug 快照均记录 `ui_evidence_allowed=false`；HUD / 标签只可辅助说明，不能替代世界层灰盒证据。 |

## 剩余风险

- 这是灰盒运行时证据，不是最终美术、动画、音频或独立 `.tscn` 资产替换完成声明。
- headless smoke 当前仍不能替代 release 视觉包；release packet 需要窗口化截图 / 人工视觉证明，覆盖起飞视角、雾带、残骸、大鸟剪影和目的地接近。
- P0 美术 / 音频资产仍需制作、接入或明确 waiver。
- 正式玩家流程仍沿用现有快速抵达探索目的地；本次补齐的是已批准航行大场景的 #20 合同、作者化数据和 debug 灰盒证据。

## 推荐下一步

1. 运行窗口化 Godot 截图或人工视觉检查，补航行大场景图证。
2. 将航行大场景的 P0 美术 / 音频缺口写入 release packet 或记录用户 waiver。
3. 统一刷新当前 demo 场景集的 release handoff 包。
