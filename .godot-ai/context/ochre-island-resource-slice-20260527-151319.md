# Godot Asset Context: ochre-island-resource-slice

## Original Request

用户要求“开始下一步”。当前项目状态显示下一步应关闭 `ochre_island_scene` + `scene_unit.prototype.banded_iron_ore` 的最小 release blocker。

## Referenced Files

- `production/session-state/active.md`
- `production/scene-specs/scene-release-gate-handoff.md`
- `production/scene-specs/scene-coverage-registry.md`
- `production/content-creation-review-gate.md`
- `production/scene-specs/ochre-island-scene.md`
- `production/unit-specs/fixed-scene-objects/banded-iron-ore.md`
- `src/presentation/playable_slice_authored_content.json`
- `src/presentation/SceneUnitAuthoring.cs`
- `src/scenes/HubRuntime.cs`
- `tests/smoke/session_shell_visual_probe.gd`

## Known Facts

- 项目处于 `Polish` 阶段。
- `#19 Scene Composition` story 已完成，但 release handoff 仍是 `BLOCKED_FOR_RELEASE`。
- `ochre_island_scene` 已通过创建适合性人工审查，结论为 `APPROVED_WITH_NOTES`。
- `scene_unit.prototype.banded_iron_ore` 已通过创建适合性人工审查，结论为 `APPROVED_WITH_NOTES`。
- 当前 `.godot-ai/` 只有占位文件，尚无合同、审查、执行计划或验证记录。
- 项目门禁要求新 scene / unit 必须走 `godot-asset-interview -> godot-asset-review -> godot-asset-execute`。
- execute 阶段优先使用 `addons/godot_ai` / Godot AI MCP；若 editor session 不可用，必须记录 blocker，不能用散落手写节点绕过合同。

## Constraints

- 不得把 UI/HUD/按钮/标签/调试覆盖层当作场景或物理单位证据。
- 不得把赭石岛实现为旧市场、探索面板或无独立边界的临时节点。
- 不得把条带状铁矿只画成地面纹理、市场商品或 UI 按钮。
- 不得删除或替换旧 Godot 节点，除非用户明确批准 exact paths。
- 本轮范围不包含市场、NPC、完整经济链、复杂采矿工具、矿脉再生或冶炼。

## Open Questions

- Godot AI MCP editor session 是否可用。
- 最终独立实现会采用独立 `.tscn`，还是先采用允许的等价作者化数据边界。

