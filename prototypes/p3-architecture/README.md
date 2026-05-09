## P3 架构验证原型 — README

### 目标
验证 Autoload 架构 + 信号协议 + 持久化管道在 Godot 4.6.2 环境下可运行。

### 验证清单

- [ ] **场景 A**：9 Autoload 引导完成 → 终端输出 "Architecture boot: PASS"
- [ ] **场景 B**：存档往返 — 模拟状态 → save → load → 状态一致，SHA-256 校验通过
- [ ] **场景 C**：信号协议 — repair_completed 扇出到 4 消费者，cascade depth ≤ 2

### 运行

```bash
# 运行自动化测试
godot --headless --script tests/gdunit4_runner.gd

# 手动冒烟测试
godot --editor  # 打开编辑器，按 F5 运行主场景
```

### 文件清单

| 文件 | 说明 |
|------|------|
| `project.godot` | Godot 4.6.2 项目配置（9 Autoloads、输入映射、Compatibility 渲染器）|
| `src/core/registry.gd` | Registry Autoload — 静态内容目录 + 查询引擎 |
| `src/core/registry_bootstrap.gd` | 引导数据 — 4 地点、2 航线、6 资源、1 修复节点、1 威胁、1 伙伴 |
| `src/core/persistence.gd` | Persistence Autoload — 完整 staging→verify→promotion 管道 + Canonical JSON + SHA-256 |
| `src/core/snapshot_package.gd` | SnapshotPackage RefCounted — 域保存数据容器 |
| `src/core/interaction_registry.gd` | InteractionRegistry Autoload — 可交互对象注册中心 + 5 状态焦点机 |
| `src/core/interactable.gd` | Interactable @abstract — 所有可交互对象的基类 |
| `src/core/resources_manager.gd` | Resources Autoload — 6 资源池 + stack/unique 合并规则 |
| `src/core/intel_manager.gd` | Intel Autoload — 知识状态机 + reveal_rumor 接口 |
| `src/core/chart_manager.gd` | Chart Autoload — 航线状态机 + 出航确认 |
| `src/feature/world_repair.gd` | WorldRepair Autoload — 修复状态机 + commit_deposit |
| `src/presentation/ui_manager.gd` | UIManager Autoload — 12 屏幕 FSM + 模态栈 |
| `src/presentation/feedback_manager.gd` | FeedbackManager Autoload — 语义事件中心 (VS stub) |
| `src/session_shell.gd` | SessionShell 主场景 — 引导链 Phase 0→7 |
| `src/session_shell.tscn` | 主场景文件 |
| `tests/unit/test_registry_query.gd` | Registry 查询引擎测试 |
| `tests/unit/test_resources_merge.gd` | Resources stack merge 算法测试 |
| `tests/unit/test_persistence_roundtrip.gd` | Persistence 往返测试 |
| `tests/integration/test_boot_chain.gd` | 引导链集成测试 |
