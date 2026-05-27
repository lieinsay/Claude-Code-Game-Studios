# Godot Asset Review: ship_interior_layered

## Review Verdict

- Verdict: pass
- Can Execute: true
- Execution Mode Allowed: reviewed-auto
- Blocking Issues: None
- Risks:
  - 现有 HubRuntime 中仍保留旧灰盒辅助节点；本轮不删除它们，因此 verification 必须明确 production-ready 证据来自 `ShipInteriorLayeredScene.tscn`。
  - 视觉仍为灰盒资产，不代表最终美术或音频完成。
- Required User Decisions: None
- Recommended Execution Plan: 新增 `ShipInteriorLayeredScene.tscn` / `.cs`，在 HubRuntime 中挂载独立船内场景，更新 authored content、smoke、规格和验证记录。

## Schema Review

- Asset type `scene` is supported.
- Stable ID `ship_interior_layered` is path/name safe.
- Godot outputs are concrete and non-destructive.
- Runtime authority is clear: scene owns visual/world layout only; HubRuntime and domain managers retain state.
- In-scope and non-goals prevent accidental repair/NPC/combat/system expansion.
- Decision boundaries require user approval before deletion, migration, new dependencies, or gameplay expansion.
- Acceptance evidence includes node/resource, visual, runtime, and log/test proof surfaces.

## Safety Review

- No destructive deletes or replacements are required.
- Existing ChartTable and S4_chart assets are reused rather than duplicated.
- UI evidence boundary is preserved: S4_chart is a referenced/opened UI target, not the scene proof itself.

## Execution Readiness

- Ready for `godot-asset-execute`.
