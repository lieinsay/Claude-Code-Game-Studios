# ADR-0006: Web 平台约束 — 浏览器作为一等目标平台的完整约束集

## Status
Proposed

## Date
2026-05-04

## Summary
将分散在 ADR-0001~0005 和引擎参考文档中的 Web 平台约束整合为单一权威决策记录。覆盖 8 个维度：语言与导出、渲染管线、线程模型、浏览器生命周期、音频激活、存储后端、输入模式、内存与启动预算。所有后续 ADR 和 GDD 可将此 ADR 作为 Web 约束的统一引用点，无需各自重复声明。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-04

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Platform — Web Export |
| **Knowledge Risk** | MEDIUM — Web 导出行为在 Godot 4.4+ 持续演变 (SharedArrayBuffer 要求、IndexedDB 映射、AudioContext 策略) |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/current-best-practices.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md`, `docs/engine-reference/godot/modules/input.md`, `docs/engine-reference/godot/modules/rendering.md`, `docs/engine-reference/godot/modules/ui.md` |
| **Post-Cutoff APIs Used** | Godot 4.6 D3D12 default on Windows (桌面开发期间 — 不影响 Web 导出)；Web 导出路径不变 (WebGL 2 + Compatibility) |
| **Verification Required** | Web 导出下 9 Autoload 启动时间剖面 (<2s 目标)；IndexedDB 存储配额限制测试 (浏览器通常 1-2GB 总配额，user:// 映射)；AudioContext 在无用户手势时的静默行为；`pagehide`/`pageshow` 事件在 Godot 4.6.2 的 JavaScriptBridge 中的可靠性 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload/Scene 架构 — Web 约束作为 Autoload 划分的决策输入)；ADR-0003 (存档系统 — IndexedDB 存储后端 + JavaScriptBridge 生命周期) |
| **Enables** | 所有后续 ADR — 作为 Web 约束的统一引用点；ADR-0007 (内容注册表)；ADR-0010~0016 (Feature 层系统) |
| **Blocks** | 任何假设多线程、原生文件系统、GPU 后处理、或非 Web 音频行为的 ADR |
| **Ordering Note** | 应在 Foundation ADR 阶段最后 Accepted — 在 ADR-0001~0005 之后，在 Feature 层 ADR 之前 |

## Context

### Problem Statement

《云海织航》的主目标平台是 Web 桌面浏览器。Godot 4.6.2 的 Web 导出引入了一组区别于原生平台的约束：GDScript only（C# 不支持 Web 导出）、Compatibility 渲染器/WebGL 2、单线程执行、浏览器生命周期事件（标签页冻结/恢复）、AudioContext 用户手势要求、IndexedDB 作为唯一持久化后端、以及浏览器内存/启动时间限制。

ADR-0001、0002、0003 已各自引用了部分 Web 约束（单线程、兼容渲染器、IndexedDB、浏览器生命周期），但引用分散且深度不一致。需要一个独立的平台约束 ADR 作为所有跨系统 Web 约束的单一权威源 — 后续 ADR 和 GDD 只需引用此 ADR 而非各自重新声明 Web 假设。

### Constraints

- **Godot 4.6.2**: Web 导出使用 Compatibility 渲染器 (WebGL 2)；C# 项目不可导出到 Web
- **浏览器环境**: 单线程、无原生文件系统、无 raw socket、AudioContext 需用户手势
- **MVP 输入**: 仅键盘+鼠标 — 无 gamepad、touch、指针锁
- **现有 ADR**: ADR-0001~0005 中已引用部分 Web 约束，本 ADR 整合并取代这些分散引用
- **引擎参考**: `docs/engine-reference/godot/` 已包含 VERSION.md 中 4 条 Web 约束声明，本 ADR 将其正式化为架构决策

### Requirements

- 明确声明 Web 作为一等目标平台 — 所有架构决策必须以 Web 可行性为前提
- 覆盖 8 个约束维度：语言/导出、渲染、线程、生命周期、音频、存储、输入、内存/启动
- 每个约束维度必须说明：约束内容、影响哪些系统、违反后果、缓解措施
- 所有后续 ADR 的 Engine Compatibility 表可通过引用本 ADR 而非重复声明 Web 约束
- 桌面原生构建（开发者日常）可暂时放松部分约束（如放宽启动时间），但 CI 必须以 Web 导出测试

## Decision

### 1. 语言与导出: GDScript Only for Web

**约束**: Godot 4 C# 项目不可导出到 Web。项目语言钉选为 GDScript。

**影响**: 所有游戏逻辑、Autoloads、Scenes 必须使用 GDScript 编写。不可使用 C#、GDExtension (C++/Rust) 编写的任何 Web 导出路径代码。

**桌面开发例外**: 桌面原生构建（Windows/Linux）可加载 GDExtension 用于开发工具（编辑器插件），但这些扩展不得进入 Web 导出路径。CI pipeline 的 Web 导出步骤在编译期排除 GDExtension 依赖。

**违反后果**: 项目无法导出到 Web — 阻塞性失败。

### 2. 渲染管线: Compatibility Renderer / WebGL 2

**约束**: Web 导出必须使用 Compatibility 渲染器 (OpenGL 3.3 / WebGL 2)。不可使用 Vulkan (Forward+/Mobile) 渲染器。

**影响**:
- **无 `Compositor` 后处理**: 无屏幕空间反射、SSAO、雾效体积、自定义后处理 pass
- **无 `FogVolume`**: Godot 4.x FogVolume 仅在 Forward+ 中可用
- **shader 限制**: GLSL ES 300 (WebGL 2)；不可使用 `textureSize()` 在 vertex shader 中
- **粒子**: `GPUParticles2D` 可用但功能受限 (无 `sub-emitter` 在 WebGL 2)；`CPUParticles2D` 作为 fallback
- **光照**: `CanvasItemMaterial` 支持 2D 法线贴图和光照；2D 点光源、方向光可用。3D 不在 MVP 范围

**缓解**:
- 所有视觉效果在 WebGL 2 上 profile 确认可接受帧率
- 避免大量 `CanvasGroup` 叠加（每个导致额外 draw call）
- 纹理尺寸 ≤ 2048×2048（移动浏览器 ≤ 1024）；使用 atlas 合并小纹理

### 3. 线程模型: 单线程执行

**约束**: Web 导出首选单线程 (`single-threaded` export flag)。不使用 `Thread` API、`Mutex`、`Semaphore`、`WorkerThreadPool`（底层使用线程 — 在单线程导出中不可用）。

**影响**:
- 所有代码在浏览器主线程执行 — 信号 emit 同步、资源操作同步、物理帧同步
- 不可将重度计算卸载到 worker thread
- `OS.delay_usec()` 阻塞主线程 → 冻结整个标签页 → **禁止使用**
- 所有 `await` 必须在当前帧或下一帧恢复 — 不可阻塞等待外部 I/O

**正面效应** (已利用):
- 无并发竞争 — 资源操作无需锁（ADR-0005 的 `ERR_BUSY` 是重入防护，非线程锁）
- 信号链执行顺序可预测 — 同步 emit 保证消费者在已知顺序执行（ADR-0002）

**违反后果**: SharedArrayBuffer 和 cross-origin isolation 是 `threaded` 导出的硬性要求 — 大多数静态托管不满足。使用 Thread API 在单线程导出中会导致运行时错误或静默失败。

### 4. 浏览器生命周期: 标签页可见性、冻结、恢复

**约束**: 浏览器可在任意时刻暂停/恢复标签页（`visibilitychange`）、在页面离开时序列化状态（`pagehide`）、在返回时恢复（`pageshow`）。Godot 引擎对这些事件的捕获不完整 — 需要自定义 HTML shell + `JavaScriptBridge`。

**影响系统**:

| 事件 | 浏览器行为 | 对本游戏的影响 | 处理 |
|------|-----------|---------------|------|
| `visibilitychange` (hidden) | 标签页不可见 — requestAnimationFrame 停止，定时器节流 | 游戏渲染暂停；`_physics_process` 停止 | SessionShell 通过 JS bridge 接收事件 → 进入 `paused` 状态 |
| `visibilitychange` (visible) | 标签页恢复可见 — rAF 恢复 | 游戏继续渲染 | SessionShell 恢复 → 从暂停状态退出 |
| `pagehide` | 页面即将被卸载/冻结 (bfcache) | 未持久化状态可能丢失 | Persistence 在收到 `pagehide` 时触发紧急存档（最后稳定边界后状态） |
| `pageshow` (persisted) | 页面从 bfcache 恢复 | 游戏状态可能过期（标签页冻结期间世界时间已推进） | SessionShell 检测 `persisted` → 显示"会话已恢复"提示；不尝试"追赶"冻结期间的游戏时间 |
| `beforeunload` | 用户关闭标签页 | 未持久化数据丢失 | 非可靠存档事件 — 不依赖此事件进行存档。`pagehide` 是更可靠的紧急存档触发点 |

**桌面开发例外**: 桌面原生构建不触发浏览器生命周期事件 — 使用窗口关闭事件 (`NOTIFICATION_WM_CLOSE_REQUEST`) 作为等效存档触发点。

### 5. 音频激活: AudioContext 用户手势要求

**约束**: 浏览器 `AudioContext` 在用户首次交互（点击、按键）前处于 `suspended` 状态。自动播放音频被浏览器静默阻止。

**影响**:
- 标题画面背景音乐在用户首次点击/按键前不会播放
- `FeedbackManager` 的初始音频反馈（如启动音效）可能被静默

**缓解**:
- 标题画面显示"点击开始"按钮 — 用户点击同时激活 AudioContext
- SessionShell 在收到首次用户输入后调用 `AudioServer.set_bus_mute(master, false)`
- 不使用 `AudioStreamPlayer.autoplay` — 所有音频通过 FeedbackManager 显式触发

### 6. 存储后端: IndexedDB via user://

**约束**: Godot 4.6 Web 导出将 `user://` 映射到浏览器 IndexedDB。无原生文件系统访问 (`res://` 只读，`user://` 读写)。

**影响**:
- ADR-0003: Persistence 使用 `FileAccess` + `user://` 路径 — 自动映射到 IndexedDB
- 存储配额由浏览器管理 (通常每域名 1-2GB 总配额，跨 IndexedDB + Cache API + LocalStorage 共享)
- LocalStorage (5-10MB) 不用于游戏存档 — 仅用于存储会话 token/设置 hash (~100 bytes)
- 不可在 `user://` 中创建深层目录结构 — IndexedDB 对路径深度不敏感但文件操作延迟高于原生 FS

**配额监控**:
- Persistence 在每次存档后检查 `FileAccess.get_open_error()` — 若为 `QuotaExceededError`，警告玩家并阻止后续存档
- 存档管理界面显示已用/可用空间（通过 `JavaScriptBridge` 查询 `navigator.storage.estimate()`）

### 7. 输入模式: 键盘+鼠标 only (MVP)

**约束**: MVP 仅支持键盘+鼠标输入。Gamepad API、触摸事件、指针锁 API 不在 MVP 范围。全屏和鼠标捕获需要用户手势触发。

**影响**:
- `InteractionRegistry` 的焦点选择公式已优化为鼠标指针 + 键盘 Tab 双路径（ADR-0004）
- 键盘焦点循环确保纯键盘可达性（无鼠标场景）
- 不使用 `Input.set_mouse_mode(MOUSE_MODE_CAPTURED)` — 需要用户手势且在 Web 中不稳定

**未来扩展**: 若后续添加 gamepad 支持，需在 Web 导出中处理 Gamepad API 的 `navigator.getGamepads()` 轮询模型（非事件驱动）。触摸支持需要处理 `touchstart`/`touchend` 的 300ms 延迟消除。

### 8. 内存与启动预算

**约束**:
- **启动时间**: Web 导出下 9 Autoload 启动 + 首个场景加载 < 2 秒（目标）；< 5 秒（硬上限）
- **运行时内存**: 浏览器标签页通常在 2-4GB 地址空间内；实际可用取决于设备。游戏内存目标 < 256MB
- **包体大小**: Web 导出 `.pck` + `.wasm` < 50MB（初始加载）；资源按需流式加载

**影响 ADR-0001**:
- 9 Autoload 的 `_ready()` 必须仅执行常量初始化（信号声明、null 检查）— 禁止文件 I/O、场景实例化、音频播放
- 实际初始化延迟到 Phase 2-5 的信号链中
- Feature Scene (Exploration 4-zone radial) 按需实例化 — 不在启动时加载

**Profile 要求**:
- CI pipeline 的 Web 导出构建在每次 PR 时测量启动时间和运行时内存
- 启动时间回归 > 20% 触发阻塞性 CI 失败
- 运行时内存回归 > 20% 触发警告

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│              WEB PLATFORM CONSTRAINT LAYER                            │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    Godot 4.6.2 Engine                          │   │
│  │                                                                │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │   │
│  │  │ Rendering│  │  Audio   │  │  Input   │  │  File I/O │     │   │
│  │  │ WebGL 2  │  │ AudioCtx │  │ Kbd/Mouse│  │ IndexedDB │     │   │
│  │  │ Compat.  │  │ gesture  │  │ only     │  │ via user://│    │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘     │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              Custom HTML Shell + JavaScriptBridge               │  │
│  │                                                                │  │
│  │  ┌──────────────────────────────────────────────────────┐     │  │
│  │  │ Browser Lifecycle Events                              │     │  │
│  │  │ visibilitychange → SessionShell._on_visibility(hidden)│     │  │
│  │  │ pagehide        → Persistence._emergency_save()       │     │  │
│  │  │ pageshow        → SessionShell._on_resume(persisted)  │     │  │
│  │  │ beforeunload    → (unreliable — pagehide preferred)   │     │  │
│  │  └──────────────────────────────────────────────────────┘     │  │
│  │                                                                │  │
│  │  ┌──────────────────────────────────────────────────────┐     │  │
│  │  │ Storage Quota Monitoring                              │     │  │
│  │  │ navigator.storage.estimate() → quota_bytes / usage    │     │  │
│  │  │ FileAccess QuotaExceededError → block future saves    │     │  │
│  │  └──────────────────────────────────────────────────────┘     │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              Game Systems (受约束层)                            │  │
│  │                                                                │  │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐ │  │
│  │  │ ADR-0001   │ │ ADR-0003   │ │ ADR-0004   │ │ ADR-0005   │ │  │
│  │  │ Autoloads  │ │ Persistence│ │ Interaction│ │ Resources  │ │  │
│  │  │ <2s start  │ │ IndexedDB  │ │ Kbd/Mouse  │ │ Sync ops   │ │  │
│  │  │ Single-    │ │ lifecycle  │ │ only       │ │ No locks   │ │  │
│  │  │ threaded   │ │ JS bridge  │ │            │ │            │ │  │
│  │  └────────────┘ └────────────┘ └────────────┘ └────────────┘ │  │
│  │                                                                │  │
│  │  ┌──────────────────────────────────────────────────────────┐ │  │
│  │  │ 所有系统共用约束:                                          │ │  │
│  │  │ • GDScript only (C#/GDExtension 不可用于 Web 导出)        │ │  │
│  │  │ • 单线程 (Thread/Mutex/Semaphore 不可用)                  │ │  │
│  │  │ • WebGL 2 / Compatibility 渲染器 (无 Compositor/Vulkan)   │ │  │
│  │  │ • AudioContext 需用户手势激活                              │ │  │
│  │  │ • 启动内存 < 256MB / .pck+.wasm < 50MB                    │ │  │
│  │  └──────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```gdscript
# === SessionShell — 浏览器生命周期接口 ===

# JavaScriptBridge 回调 (注册在 custom HTML shell 中)
func _on_visibility_changed(state: String) -> void:
    # "visible" → 恢复渲染和 physics_process
    # "hidden" → 进入 paused 状态，停止非关键 CPU 工作
    pass

func _on_page_resume(persisted: bool) -> void:
    # pageshow 事件回调
    # persisted=true → 从 bfcache 恢复，显示"会话已恢复"
    # persisted=false → 正常加载
    pass

# === Persistence — 紧急存档接口 ===

func _emergency_save() -> void:
    # pagehide 事件触发
    # 仅在最后稳定边界后状态已变更时执行存档
    # 不保证完成（浏览器可能在存档中途终止进程）
    pass

# === Audio — 音频激活接口 ===

func _on_first_user_interaction() -> void:
    # 首次用户点击/按键后调用
    # AudioServer.set_bus_mute(AudioServer.get_bus_index("Master"), false)
    # FeedbackManager 恢复所有挂起的音频播放
    pass
```

### Custom HTML Shell 要求

```html
<!-- Godot 4.6 Web 导出自定义 shell 的最小生命周期钩子 -->
<script>
  // 必须: visibilitychange → Godot 引擎
  document.addEventListener('visibilitychange', () => {
    const state = document.visibilityState; // 'visible' | 'hidden'
    // 通过 JavaScriptBridge 传递给 SessionShell
  });

  // 必须: pagehide → 紧急存档触发
  window.addEventListener('pagehide', (event) => {
    if (event.persisted) {
      // 页面进入 bfcache — 非卸载
    }
    // 触发 Persistence._emergency_save()
  });

  // 必须: pageshow → 恢复检测
  window.addEventListener('pageshow', (event) => {
    // event.persisted: true = 从 bfcache 恢复, false = 正常加载
  });

  // 可选: 存储配额查询
  navigator.storage?.estimate().then(({quota, usage}) => {
    // quota: 总可用字节, usage: 已用字节
  });
</script>
```

## Alternatives Considered

### Alternative A: 不创建独立 ADR — 将 Web 约束分散到各系统 ADR

- **Description**: 每个受影响的 ADR 自行声明其 Web 约束。ADR-0001 声明启动约束，ADR-0003 声明存储约束，ADR-0004 声明输入约束，引擎参考文档保持为唯一集中参考源
- **Pros**: 无新 ADR 开销；Web 约束紧邻被约束的系统，便于上下文理解
- **Cons**: 约束跨 5+ ADR 分散 → 无单一位置列出完整 Web 约束集；新系统设计者需阅读多个 ADR 才能了解完整约束；Web 约束变更时需更新多个文档；`/architecture-review` 无法集中检查 Web 约束合规性
- **Rejection Reason**: 集中式 Web 约束 ADR 是 Foundation 层的正确抽象 — 它描述的是"平台能提供什么"，而非"系统需要什么"。后续所有 Feature ADR 在 Engine Compatibility 表中引用本 ADR 即可，无需各自研究 Web 约束

### Alternative B: Threaded Web Export (SharedArrayBuffer + COOP/COEP)

- **Description**: 使用 `threaded` Web 导出，利用 SharedArrayBuffer 启用 AudioWorklet 和多线程
- **Pros**: AudioWorklet 提供更低延迟音频处理；多线程可卸载重度计算
- **Cons**: 要求设置 COOP (`Cross-Origin-Opener-Policy: same-origin`) 和 COEP (`Cross-Origin-Embedder-Policy: require-corp`) HTTP 头 — 大多数静态托管 (GitHub Pages, itch.io, Netlify) **不支持**自定义 HTTP 头或需要企业计划
- **Rejection Reason**: 部署约束压倒性能优势。`single-threaded` 导出可部署到任意静态托管。若未来确定托管方案支持 COOP/COEP，可重新评估

### Alternative C: 桌面原生优先 — Web 是发布后才考虑的降级目标

- **Description**: 以桌面原生平台（Windows/Linux）为主要开发目标，Web 导出作为后处理适配层
- **Pros**: 开发期间无 Web 约束 — 可用 Vulkan 渲染器、多线程、原生文件系统
- **Cons**: Web 导出时发现架构不兼容风险极高 — 可能在开发后期被迫大幅重构。C# 或 GDExtension 编写的系统在 Web 导出中完全不可用。这不是"降级"问题，而是"不可导出"的阻断性问题
- **Rejection Reason**: 项目已明确 Web 桌面浏览器为主要目标平台。Web-first 意味着 Web 约束是架构设计的**输入**而非事后验证 — 设计与约束共同演进，而非代码完成后适配约束

## Consequences

### Positive

- **单一 Web 约束源**: 所有后续 ADR 和 GDD 在 Web 约束上引用 ADR-0006 — 无需各自重新研究 Web 行为
- **架构一致性**: 8 个约束维度显式声明 — `/architecture-review` 可集中检查新 ADR 是否违反 Web 约束
- **部署简化**: `single-threaded` 导出 + 仅键盘/鼠标 → 可部署到任意静态托管（GitHub Pages、itch.io、Netlify 免费层）
- **CI 可验证**: Web 约束可转化为 CI 检查 — "无 Thread API 调用"、"无 C# 文件"、"启动 < 2s"
- **未来升级路径**: 若平台约束变更（如 COOP/COEP 支持普及），只需修订此 ADR，而非 5+ 个系统 ADR

### Negative

- **开发/目标差异**: 桌面原生开发构建不受部分约束（启动超时、AudioContext 手势），开发者需在 Web 导出中定期测试以确保兼容
- **渲染能力受限**: Compatibility 渲染器排除了 Vulkan 的现代后处理效果 — 视觉效果必须在 WebGL 2 能力范围内设计
- **无原生扩展**: GDExtension (C++/Rust) 在 Web 导出中不可用 — 所有游戏逻辑必须用 GDScript 实现

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| IndexedDB 配额在长时间游戏中耗尽 | Medium — 取决于浏览器和设备的配额策略（通常 1-2GB/域名） | High — 存档失败，进度丢失 | 存档管理 UI 显示配额使用；到达 80% 时警告玩家清理旧存档；Persisting 在 QuotaExceededError 时优雅降级（保留最近 N 个存档） |
| AudioContext 在无用户手势时静默 — 玩家首次体验无音频 | High — 所有浏览器强制此策略 | Medium — 第一印象受损，玩家以为音频损坏 | 标题画面"点击开始"按钮同时激活音频；按钮文案暗示"点击以开始"而非"点击以启用音频" |
| `pagehide` 不可靠 — 浏览器可能在存档完成前终止进程 | Medium — `pagehide` 执行时间有限（通常 < 1s） | High — 进度丢失 | 存档操作必须在 < 500ms 内完成；正常存档仅在稳定边界触发（非 pagehide）；pagehide 存档是尽力而为的备份 |
| WebGL 2 在旧设备/移动浏览器上不可用或性能不达标 | Low — WebGL 2 在 95%+ 桌面浏览器中可用 | High — 游戏完全不可渲染 | 启动时通过 `JavaScriptBridge` 调用 `canvas.getContext('webgl2')` 检测 WebGL 2 支持；不支持时显示自定义 HTML 错误提示（"请使用支持 WebGL 2 的现代浏览器"）。备选：依赖 Godot 引擎初始化失败的内置错误页面（不够优雅但有效） |
| `single-threaded` 导出下长时间运算阻塞 UI | Low — 游戏逻辑在每帧 ≤ 16ms 内完成 | Low — 偶尔掉帧 | 重度计算拆分到多帧（分批处理）；不使用 `OS.delay_usec()`；不使用阻塞 await |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| #3 local-save-world-state-persistence | Web 生命周期事件捕获 (visibilitychange, pagehide, pageshow) | 约束维度 4 — 自定义 HTML shell + JavaScriptBridge 生命周期钩子 |
| #3 local-save-world-state-persistence | IndexedDB 作为存储后端（user:// 映射） | 约束维度 6 — user:// → IndexedDB 存储后端 + 配额监控 |
| #3 local-save-world-state-persistence | 浏览器标签页挂起时存档边界 | 约束维度 4 — pagehide 紧急存档 + pageshow 恢复检测 |
| #4 player-movement-interaction | MVP 仅键盘+鼠标输入，无 gamepad/touch | 约束维度 7 — 键盘+鼠标 only；未来扩展路径 |
| #4 player-movement-interaction | Web 环境下的输入焦点恢复 | 约束维度 4 — visibilitychange 恢复时输入门重新打开 |
| ADR-0001 | Web 启动时间 < 2s | 约束维度 8 — 启动预算 + Autoload _ready() 限制 |
| ADR-0001 | 单线程执行 — Autoload _ready() 串行 | 约束维度 3 — 单线程执行模型 |
| ADR-0001 | Compatibility 渲染器 — 无 Compositor | 约束维度 2 — WebGL 2 / Compatibility 渲染器 |
| ADR-0002 | 同步 signal emit（单线程保证） | 约束维度 3 — 单线程执行 → 可预测的信号顺序 |
| ADR-0003 | FileAccess + user:// → IndexedDB | 约束维度 6 — user:// → IndexedDB 存储后端 |
| ADR-0004 | 焦点评分公式的 Web 输入约束 | 约束维度 7 — 仅键盘+鼠标 |
| ADR-0005 | 同步资源操作（单线程保证无竞争） | 约束维度 3 — 单线程 → 无需锁 |

## Performance Implications

- **CPU**: `single-threaded` — 所有游戏逻辑、物理、渲染共享同一个浏览器主线程。`_physics_process` 和 `_process` 在 60Hz 下每帧 16.6ms 总预算。信号同步 emit 不增加额外线程切换开销
- **Memory**: 浏览器标签页典型可用堆 2-4GB；游戏目标 ≤ 256MB (well within limit)。IndexedDB 不计算在 JS 堆内 — 存储配额独立
- **Load Time**: Web 导出 ≤ 2s 启动目标。`.pck` + `.wasm` 初始加载 < 50MB (gzip 后 ~15-20MB)。后续资源按需流式加载
- **Network**: 无 — 单机游戏。Web 导出的 `.pck` / `.wasm` / asset 文件由 HTTP 服务器静态提供（CDN）；不使用 WebSocket 或 WebRTC

## Migration Plan

无需迁移 — 项目尚无代码。此 ADR 作为 Foundation 层的平台约束声明，在后续所有实现中作为前提条件。

实现检查清单:
1. CI pipeline 添加 Web 导出构建步骤 (Godot 4.6 headless)
2. CI 添加约束验证: "no Thread/Mutex/Semaphore usage" grep check、"no C# files" check
3. 自定义 HTML shell 实现生命周期钩子 (visibilitychange, pagehide, pageshow)
4. 存储配额查询 JavaScriptBridge 接口
5. 标题画面"点击开始"按钮 + AudioContext 激活逻辑
6. WebGL 2 支持检测 + fallback 错误页

## Validation Criteria

- Web 导出构建成功 — `godot --headless --export-release "Web"` 无错误
- 9 Autoload 启动 + 首个场景加载 < 2s（在 Web 导出中测量）
- 标签页隐藏/恢复周期后游戏状态保持一致（paused → resumed）
- `pagehide` 事件触发 Persistence 保存尝试（验证: 在存档后修改状态 → 模拟 pagehide → 重新加载验证最后一次存档反映修改前状态）
- AudioContext 在首次用户点击前静默；点击后音频正常播放
- 在 Chrome/Firefox/Edge 中 IndexedDB 读写正常（user:// 路径）
- 无 Thread/Mutex/Semaphore API 使用（grep 检查）
- 无 C# (.cs) 文件
- 无 GDExtension (.gdextension) 配置文件

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — Web 约束是 Autoload 划分和 Scene 管理的决策输入
- **ADR-0002**: Signal 通信协议 — 单线程保证信号同步执行，深链限制 (max depth 2)
- **ADR-0003**: 存档系统 — IndexedDB 存储后端 + JavaScriptBridge 生命周期集成
- **ADR-0004**: InteractionHandler — 仅键盘+鼠标输入模式
- **ADR-0005**: 资源池系统 — 单线程保证同步操作无需锁
- **Engine Reference**: `docs/engine-reference/godot/VERSION.md` — 4 条 Web 约束声明
- **Engine Reference**: `docs/engine-reference/godot/current-best-practices.md` — Web Export 节
- **Engine Reference**: `docs/engine-reference/godot/modules/input.md` — Web 输入约束
- **Engine Reference**: `docs/engine-reference/godot/modules/rendering.md` — Compatibility 渲染器约束
