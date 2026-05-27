# Godot Asset Contract: scene_unit.prototype.banded_iron_ore

## Metadata

- Asset Type: fixed-world-unit
- Stable ID: scene_unit.prototype.banded_iron_ore
- Display Name: 条带状铁矿
- Source Requirement: `production/unit-specs/fixed-scene-objects/banded-iron-ore.md`
- Lifecycle State: review-ready

## Intent

- Player/User-facing purpose: 玩家在赭石岛看到一个明确的世界资源点，靠近并按 Use 采集基础矿物资源。
- Design role: 支撑赭石岛“资源岛”身份，不能由 UI 或地面纹理替代。
- In scope: 可复用固定单位原型、可见矿脉形状、soft-overlap 交互范围、available / harvested / blocked 状态、采集反馈、场景实例证据。
- Non-goals: 完整矿场、工具效率、矿脉再生、冶炼、市场交易、经济链、AI 或动态移动。

## Godot Outputs

- Scene paths: `src/scenes/units/BandedIronOre.tscn`
- Script paths: `src/scenes/units/BandedIronOre.cs`
- Resource paths: `src/presentation/playable_slice_authored_content.json` may gain prototype and instance records for `scene_unit.prototype.banded_iron_ore`.
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `production/qa/evidence/banded-iron-ore-godot-verification.md`; screenshot under `production/qa/evidence/`.

## Runtime Boundary

- Owns: Fixed unit visual state, local overlap/anchor, emitted harvest intent.
- Reads: Current harvested/blocked state, Resources capacity / accepted reward result, input focus state.
- Emits: `ore_harvest_requested`, `ore_harvested`, `ore_harvest_blocked`.
- Must not own: Global resource inventory authority, persistence authority, economy balancing, route/navigation state.

## Decision Boundaries

- AI may decide: Simple greybox geometry, stripe count, color values, child node names, local collision shape size.
- AI must ask before: Adding new resource IDs beyond existing/basic placeholder resources, adding regeneration, changing capacity rules, deleting or replacing existing unit nodes.

## Acceptance Evidence

- Node/resource evidence: Reusable `BandedIronOre` unit scene or data prototype exists and is instanced in `ochre_island_scene`.
- Visual evidence: Screenshot shows mineral stripes and a distinct available / harvested state.
- Runtime evidence: Proximity + Use changes state once, emits harvest event, and blocked state preserves visible world object.
- Log/test evidence: Smoke or verification log confirms prototype ID, instance ID, collision/overlap semantics, source layer `world_playable_scene`, and `ui_evidence_allowed == false`.

## Execution Readiness

- Blocking ambiguity: Godot AI MCP editor session availability is unknown.
- Required MCP/editor state: A Godot editor session for this repository, or an approved file-level asset execution path recorded by review.
- Safe to execute: true

## Asset-Type Specific Requirements

- Reusable scene/prefab boundary: `scene_unit.prototype.banded_iron_ore` must be reusable and not only a one-off texture.
- Visible form: Ochre/dark banded mineral body, readable as ore within 3 seconds.
- Collision or soft overlap: Body may be blocking/static or贴地; interaction range is `soft_overlap`.
- States: `available`, `harvested`, `blocked`.
- Interaction anchors: `banded_iron_ore_anchor`.
- Emitted events: `ore_harvest_requested`, `ore_harvested`, `ore_harvest_blocked`.
- Instance evidence: At least one placed instance in `ochre_island_scene`.

## Residual Ambiguity

- Non-blocking assumptions: Reward may use an existing/basic resource placeholder until a later economy pass names final iron ore resource.
- Blocking questions: None for contract review; editor session availability is checked during execution.

