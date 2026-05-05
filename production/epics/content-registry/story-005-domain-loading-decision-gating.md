# Story 005: Domain Loading & Decision UI Gating

> **Epic**: Content Registry
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/content-data-state-registry.md`
**Requirement**: `TR-registry-002`, `TR-registry-003`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0002: Signal Communication Protocol
**ADR Decision Summary**: Registry 的 7 个内容域分别加载——每个域有独立加载状态 (UNLOADED→LOADING→PARTIAL→COMPLETE→FAILED)。Player-facing 决策 UI 必须等待相关域全部 COMPLETE 后才能展示可操作选项。`domain_ready` 信号遵循 ADR-0002 typed params + sync emit 协议。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 信号 emit 为同步 (`sender.signal_name.emit(params)`)；`domain_ready(domain: StringName, status: StringName)` 遵循 ADR-0002。

**Control Manifest Rules (Foundation layer)**:
- Required: `domain_ready` 信号使用 typed params，sync emit
- Forbidden: `deferred_emit` —— 不使用 `.emit.call_deferred()`
- Guardrail: 信号 cascade depth ≤ 2

---

## Acceptance Criteria

*From GDD `design/gdd/content-data-state-registry.md`:*

- [ ] **AC-1**: GIVEN 玩家打开航图与航线规划界面，WHEN `routes`、`world`、`intel` 或相关 `threats` 内容域不是 COMPLETE，THEN 界面不得展示半完整航线选择，必须显示等待状态或安全错误
- [ ] **AC-2**: GIVEN 玩家打开飞艇家园或整备界面，WHEN `airship`、`resources` 或相关 `companions` 内容域不是 COMPLETE，THEN 界面不得展示可安装模块、舱室锚点或伙伴功能选择
- [ ] **AC-3**: GIVEN 玩家打开世界修复或市场界面，WHEN `world`、`resources` 或相关 `intel` 内容域不是 COMPLETE，THEN 界面不得展示可执行修复、摊位商品或材料用途
- [ ] **AC-4**: GIVEN 玩家决策界面已经基于一组内容定义打开，WHEN 异步加载/缓存刷新/内容包重载发生，THEN 当前界面的可选项、排序和引用结果不得变化，除非界面关闭并用新的完整 snapshot 重建
- [ ] **AC-5**: GIVEN 内容包或 Schema 版本不兼容，WHEN 查询/启动内容层，THEN 返回 `VERSION_INCOMPATIBLE`——不得静默降级或自动补字段
- [ ] **AC-6**: GIVEN 内容包版本不兼容，WHEN Web 构建启动内容层，THEN 必须失败在内容层边界并给出可复制诊断，不得进入半可用状态

---

## Implementation Notes

*Derived from ADR-0001 + ADR-0002 + GDD Domain Contract:*

- 7 个 MVP 域: `resources`, `airship`, `world`, `routes`, `intel`, `companions`, `threats`
- 域加载状态枚举: `enum DomainStatus { UNLOADED, LOADING, PARTIAL, COMPLETE, FAILED }`
- `PARTIAL` 只用于开发期诊断——玩家流程不得展示 PARTIAL 结果
- Snapshot 机制：决策 UI 打开时 `take_snapshot(domains: Array[StringName]) → SnapshotHandle`；UI 关闭时 `release_snapshot(handle)`；snapshot 有效期间内容变更被隔离
- 域完整性需求矩阵硬编码为配置表（非代码逻辑）:
  - Chart UI: needs `[routes, world, intel, threats]` COMPLETE
  - Hub UI: needs `[airship, resources, companions]` COMPLETE
  - Repair/Market UI: needs `[world, resources, intel]` COMPLETE
- `domain_ready(domain: StringName, status: StringName)` 信号在域状态变更后 emit

---

## Out of Scope

- Story 001-004: 域内的内容注册、校验、生命周期和引用完整性（域的 COMPLETE 状态依赖这些全部通过）
- Platform Session Shell (#2): HTML shell 加载页面和错误展示——Registry 只提供状态，壳层渲染
- UI/HUD (#16): 航图/Hub/市场 UI 具体渲染——Registry 只提供域状态信号和 snapshot 机制

---

## QA Test Cases

- **AC-1**: Chart UI blocked on incomplete domain
  - Given: `routes` COMPLETE, `world` COMPLETE, `intel` COMPLETE, `threats` LOADING
  - When: 航图界面请求 `check_domains_ready(["routes", "world", "intel", "threats"])`
  - Then: 返回 `{ready: false, blocked_domains: ["threats"]}`，UI 显示等待状态
  - Edge cases: 所有域 COMPLETE → `{ready: true}`

- **AC-4**: Snapshot isolation during UI open
  - Given: 航图 UI 已打开并持有 snapshot，`threats` 域之后变为 COMPLETE
  - When: 航图 UI 查询威胁列表
  - Then: 返回 snapshot 创建时的状态（threats 在 snapshot 时为 LOADING）
  - Edge cases: snapshot release 后重新查询→获取最新 COMPLETE 数据

- **AC-5**: Version incompatibility
  - Given: 内容包 schema_version=3，当前 Registry 仅支持 schema_version=1
  - When: `load_content_package(package)`
  - Then: 返回 VERSION_INCOMPATIBLE，域状态设为 FAILED，所有依赖域保持 UNLOADED
  - Edge cases: schema_version 在 supported_versions 范围内→正常加载

- **AC-6**: Fatal at content layer boundary
  - Given: Web 构建内容包 VERSION_INCOMPATIBLE
  - When: SessionShell 请求进入 Ready 状态
  - Then: Registry 返回 FAILED 状态+可复制诊断，壳层显示安全错误界面
  - Edge cases: 不是所有域 FAILED——已 COMPLETE 的域保持可用

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/registry/domain_loading_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-004 (域加载 COMPLETE 需要 ID/Schema/Lifecycle/Reference 全部校验通过)
- Depends on: ADR-0002 (Signal 协议——`domain_ready` 信号定义)
- Unlocks: Story 008 (Player-Facing Boundary——snapshot 和域门控保护玩家 UI)
