# Epic: Partner & Relationships

> **Layer**: Feature
> **GDD**: design/gdd/partner-relationships.md
> **Architecture Module**: Autoload #15 — PartnerManager
> **Status**: Complete — 2026-05-14
> **Stories**: 6 (001-006)

## Overview

实现《云海织航》Pillar 5（少量深关系胜过大量收集）的 MVP 可见证据——PartnerManager Autoload #15，管理 MVP 唯一伙伴 `partner.sky-cat`（航海猫——老飞艇船员猫族群的最后一只）。系统维护三层状态机：猫的 6 态运行时状态机（睡觉→闲置→跟随→蹲坐→嗅辨→窝中）、命名 3 态（pending→prompted→completed）、小窝痕迹 4 阶段（empty→first→accumulating→full，不可逆）。核心交互循环：玩家将带有 `cat_sniff_signature` 的物品递给猫 → scout_sniff() 6 步算法 → F.1 置信度截断（≤66，永不达权威）→ 调用 IntelManager (#6) 的 reveal_rumor() 写入航图传闻。首次成功嗅辨后归港触发命名时刻（一次性、不可改名），之后每次成功嗅辨在小窝堆积不可逆的生活痕迹。系统有 6 条硬禁止（R15）：无好感度数值、无礼物菜单、无事件树、无定时器奖励、无第二只伙伴、无招募/解雇。猫永远在飞艇上（R2 存在性契约），不消失不死亡。核心差异化点：猫不是可收集的数值——命名的一次性、痕迹的不可逆性、嗅觉反应的留白共同构成"它记得你"的证据，不需要好感度来量化。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0015: Partner & Relationships System | PartnerManager Autoload #15；3-tier state machines (cat runtime, naming, nest)；scout_sniff 6 步算法 + F.1 confidence_clamp (≤66)；F.2 naming eligibility；R15 6 条硬禁止；Dictionary 后端存储；reveal_rumor() → #6 写入；Hub 事件消费；ADR-0003 progress.partner_skycat snapshot；query_partner_* 查询接口 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-partner-001 | Cat 6-state FSM + R2 presence contract + R12 idle + R13 arrival behavior | ADR-0015 |
| TR-partner-002 | scout_sniff 6-step algorithm + F.1 clamp + R7 reactions + R10 schema contract | ADR-0015 |
| TR-partner-003 | Naming 3-stage FSM + nest 4-stage irreversible accumulation + R15 guards + persistence | ADR-0015 |

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Cat State Machine & Presence Contract](story-001-cat-state-machine-presence.md) | Logic | TR-partner-001 | ADR-0015 |
| 002 | [Scout Sniff Algorithm & Confidence Clamp](story-002-scout-sniff-confidence.md) | Logic | TR-partner-002 | ADR-0015 |
| 003 | [Naming System & Nest Accumulation](story-003-naming-nest-accumulation.md) | Logic | TR-partner-003 | ADR-0015 |
| 004 | [Hub Event & Intel API Integration](story-004-hub-intel-integration.md) | Integration | TR-partner-001, TR-partner-002 | ADR-0015 |
| 005 | [Persistence & State Recovery](story-005-persistence-recovery.md) | Integration | TR-partner-003 | ADR-0015 |
| 006 | [Edge Cases, R15 Guards & Defensive Handling](story-006-edge-cases-r15-defensive.md) | Integration | TR-partner-001, TR-partner-002, TR-partner-003 | ADR-0015 |

**Summary**: 3 Logic + 3 Integration stories

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/partner-relationships.md` are verified (8 AC groups, 40+ ACs)
- All Logic and Integration stories have passing test files in `tests/unit/partner-relationships/` and `tests/integration/partner-relationships/`
- Cat 6-state FSM transitions correctly under all Hub events; cooldown prevents zone spam jitter
- scout_sniff() 6-step algorithm produces correct results including all 5 reaction types
- F.1 confidence_clamp enforces ≤66 for all raw inputs 0-100
- F.2 naming eligibility correctly gates on sniff_success + skip_count < 3 + player_returned_to_hub
- Naming state machine: 3 skips → default name "那只猫"; valid submit → completed irreversibly
- Nest 4-stage FSM: EMPTY→FIRST→ACCUMULATING→FULL, monotonic non-decreasing
- query_partner_present() returns true unconditionally
- sniffing state gate prevents concurrent scout_sniff() calls
- All 6 R15 hard prohibitions verified in data model and API surface
- reveal_rumor() failure degrades gracefully (local state committed, no retry)
- ADR-0003 progress.partner_skycat snapshot correctly serializes/deserializes all 7 fields
- Transient fields (cat_state, cooldown) correctly re-derived on load per E.4.a
- Bootstrap race conditions handled: sync_with_hub_state() + on_partner_joined queuing
- PartnerManager is independently testable with mock #6/#7/#5 injection

## Next Step

All stories complete and closed. Feature Layer 5/5 unblocked — all Feature Layer epics complete. Next: Presentation Layer — ui-hud-interface #16 (ADR-0012 ready, needs story decomposition).
