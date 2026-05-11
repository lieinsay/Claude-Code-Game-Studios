# Story 002: Start / Continue Entry + Audio Activation

> **Epic**: Platform Session Shell
> **Status**: Complete — 2026-05-11
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-001`, `TR-platform-002`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: Start 永远表示新会话；Continue 只在验证存在可恢复会话时显示或启用。音频激活必须由明确用户手势触发——Start/Continue 的确认输入在同一手势中尝试音频解锁。audio_gate 有四种状态：Pass/SoftFail/HardFail/Muted。in-flight token 去重防止并行会话创建。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: audio device readiness 激活依赖 桌面平台的用户手势要求；SessionShell receives desktop window lifecycle notifications from Godot。

**Control Manifest Rules (Foundation layer)**:
- Required: Start/Continue 令牌去重；音频失败为软失败不锁死游戏
- Forbidden: `direct_cross_autoload_in_ready` — 不在 `_ready()` 中查询存档系统
- Guardrail: 音频解锁尝试 ≤1 次/用户手势

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN Ready 且 audio_gate=Pass 或 Muted，WHEN 玩家选择 Start，THEN 创建 Start in-flight token 进入 SessionStarting
- [x] **AC-2**: GIVEN Ready 且 audio_gate 需用户手势，WHEN 玩家选择 Start/Continue，THEN 在該手勢中嘗試音頻解鎖；若仍需确认→AwaitingAudioActivation
- [x] **AC-3**: GIVEN AwaitingAudioActivation，WHEN 音频解锁成功，THEN audio_gate=Pass→转入 SessionStarting
- [x] **AC-4**: GIVEN AwaitingAudioActivation，WHEN 音频解锁失败但玩家选无声继续，THEN audio_gate=Muted→转入 SessionStarting
- [x] **AC-5**: GIVEN 存在旧会话，WHEN 玩家选择 Start，THEN 创建新会话意图，不沿用旧会话上下文，不修改现有继续点
- [x] **AC-6**: GIVEN continue_availability=Enabled，WHEN 玩家选择 Continue，THEN 创建 Continue in-flight token 进入 SessionStarting
- [x] **AC-7**: GIVEN continue_availability=PreservedLocked，WHEN 玩家选择 Continue，THEN 不进入 SessionStarting——显示锁定原因+Return Title+New Session，不删除继续点
- [x] **AC-8**: GIVEN continue_availability=Hidden，WHEN 渲染入口，THEN Continue 不可见或不可选
- [x] **AC-9**: GIVEN 玩家连续重复点击 Start/Continue，WHEN 多次输入发生，THEN 只接受第一次，其余去重，不并行创建两个会话
- [x] **AC-10**: GIVEN Start/Continue in-flight token 已创建，WHEN 相同入口再次触发，THEN 不创建第二个 token

---

## Implementation Notes

- `continue_availability` 使用枚举 `{ ENABLED, PRESERVED_LOCKED, HIDDEN }`——由存档系统返回的 continue_point 状态决定
- In-flight token 使用 `Dictionary{intent: String, token_id: String, created_at: int}`——在 SessionStarting/ResumePending 期间有效
- 去重逻辑: `if _active_token != null and _active_token.intent == new_intent: return ERR_BUSY`
- Audio gate 枚举: `enum AudioGate { REQUIRES_GESTURE, PASS, SOFT_FAIL, HARD_FAIL, MUTED }`
- `_try_audio_unlock()` 在用户手势内调用 `AudioServer.set_bus_mute(master_bus, false)` + 播放短静音音频→检查 `AudioServer.get_bus_volume_db()` 确认激活
- audio_gate=HardFail 时→进入 `FatalBlocked`（仅当桌面音频设备不可用且游戏要求音频）
- PreservedLocked 原因字符串由存档系统提供——壳层直接渲染，不本地生成

---

## Out of Scope

- Story 001: 状态机核心（Ready/SessionStarting 状态定义）
- Story 005: continue_availability 的来源（storage_capability 判定）
- Story 007: Start/Continue UI 渲染

---

## QA Test Cases

- **AC-9**: Click-spam dedup
  - Given: Ready 状态，玩家快速点击 Start 3 次
  - When: 3 次点击在 token 完成前到达
  - Then: 仅创建 1 个 SessionStarting token；第 2/3 次点击返回 ERR_BUSY 且不产生副作用
  - Edge cases: 不同入口 (Start vs Continue) 同时触发→按先到顺序，后到返回 ERR_BUSY

- **AC-4**: Silent continue after audio fail
  - Given: 音频解锁失败，显示 "音频不可用——是否无声继续？"
  - When: 玩家选择 "无声继续"
  - Then: audio_gate=Muted，进入 SessionStarting，HUD 显示持久静音指示器
  - Edge cases: 玩家可在 Settings 中重新尝试启用音频

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/session/EntryAudioTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-11

---

## Dependencies

- Depends on: Story 001 (State Machine Core)
- Unlocks: Story 005 (Storage Capability 影响 continue_availability), Story 007 (Entry UI)

## Implementation Notes

**Implemented**: 2026-05-11
**Criteria**: 10/10 passing
**Test Evidence**: Integration test at `tests/integration/session/EntryAudioTest.csproj` — 10 acceptance checks passing.
**Code Review**: Local review complete — no blocking issues found.
