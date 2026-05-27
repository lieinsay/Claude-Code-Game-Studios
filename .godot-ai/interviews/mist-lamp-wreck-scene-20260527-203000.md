# Godot Asset Interview: mist_lamp_wreck_scene

## 结论

通过，进入资产合同起草。

## 访谈摘要

- 场景身份: 雾灯残骸浮岛，是安静搜索 / 打捞 / 返航闭环的目的地。
- 进入路径: 从 `voyage_open_world_scene` 抵达。
- 离开路径: 返航船舵点预热后起飞，返回 `initial_island_scene` / `hub_island_dock`。
- 明确非目标: 岛屿本体不含威胁区；航行危险由 `voyage_open_world_scene` 和后续 #10 live driving 负责。
- 证据边界: 世界 / 可玩场景节点和作者化单位是证据；HUD、按钮、标签、旧灰盒和调试入口不能替代。

## 必需资产

- `MistLampWorldLayer`
- `MistIslandMass`
- `MistIslandPath`
- `MistLampWreckBody`
- `MistSearchScanAnchor`
- `MistReturnShipHull`
- `MistReturnHelmAnchor`
- `MistReturnTakeoffTrail`
- `MistWaterBoundary`

## 风险记录

- 当前目标是生产可追踪灰盒，不声明最终美术、最终音频或 release-ready。
- 旧探索灰盒可暂留为兼容 scaffolding，但不得作为生产证据。
