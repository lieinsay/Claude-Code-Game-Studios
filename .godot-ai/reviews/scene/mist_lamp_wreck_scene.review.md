# Godot Asset Review: mist_lamp_wreck_scene

## Verdict

`APPROVED_WITH_NOTES`

## Review Notes

- 合同满足独立 Godot 场景资产、脚本、作者化数据、运行时挂载和 smoke/debug 证据要求。
- 保留 `exploration_mist_island` 作为兼容 runtime ID 是合理折中，可避免破坏当前可玩探索流程。
- 旧 `HubRuntime` 探索灰盒仅可作为非破坏性兼容 scaffolding；后续不能引用它作为 production-ready 场景证据。
- 岛屿本体无威胁区的用户备注已反映在资产合同、scene spec、debug evidence 和 smoke 断言中。

## Required Execution Checks

- `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
- `git diff --check`

## Residual Risk

- 当前是生产可追踪灰盒，不是最终美术 / 音频。
- 非 headless 截图仍需后续补证。
