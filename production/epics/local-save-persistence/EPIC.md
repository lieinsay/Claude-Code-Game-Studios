# Epic: Local Save / World State Persistence

> **Layer**: Foundation
> **GDD**: design/gdd/local-save-world-state-persistence.md
> **Architecture Module**: Autoload #3 — Persistence
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories local-save-persistence`

## Overview

实现《云海织航》的连续性保障系统——负责将资源、模块、航线状态、修复状态、村镇/市场状态、探索状态、设置和安全继续点序列化到本地存档，并在 Start / Continue / Suspend / Resume 等会话节点中验证、恢复或锁定这些数据。该系统采用 Staging → Verify → Promotion 三段式保存工作流（写入暂存区→SHA-256 完整性校验→原子替换正式存档），定义 8 个快照包 (progress.hub, progress.exploration, progress.world_repair, progress.settlement, state.resources, state.modules_hull, state.intel, settings.*)，并维护版本迁移路径 (save_version + migration registry)。持久化介质为 IndexedDB（通过 Godot `user://` 映射），快照上限 2MB，p95 编码+校验 <50ms。系统不拥有运行时世界状态——它只负责保存领域系统通过 Signal 声明的可持久化快照，恢复到最近安全继续点。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Save System / JSON Serialization | Canonical JSON 快照包；Staging→Verify→Promotion 工作流；8 个快照包定义；版本迁移路径；禁止 store_var/get_var Variant blob | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-persistence-001 | Staging → Verify → Promotion save workflow | ADR-0003 ✅ |
| TR-persistence-002 | 8 snapshot packages: progress.*, state.*, settings.* | ADR-0003 ✅ |
| TR-persistence-003 | Version migration: save_version field + migration path registry | ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/local-save-world-state-persistence.md` are verified
- All Logic and Integration stories have passing test files in `tests/integration/persistence/`
- Save/load roundtrip tests pass: write snapshot → SHA-256 verify → promote → load → restore → state match
- Canonical JSON format enforced (no Variant blobs, no `store_var()`)
- Version migration tested: old save_version → migration → new save_version roundtrip
- Web platform: IndexedDB unavailable → ephemeral session fallback works
- Snapshot 2MB budget respected; encode+hash p95 <50ms
- Atomic promotion: power loss / tab close during save never corrupts existing save

## Next Step

Run `/create-stories local-save-persistence` to break this epic into implementable stories.
