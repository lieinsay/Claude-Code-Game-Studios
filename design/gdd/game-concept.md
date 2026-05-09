# Game Concept: 云海织航

*Created: 2026-04-26*
*Status: Draft*
*Last Updated: 2026-05-09*

> **Creative Director Review (CD-PILLARS)**: CONCERNS accepted 2026-04-26. Pillar 2 wording revised and Pillar 2 / Pillar 3 responsibility boundary clarified.
> **Art Director Review (AD-CONCEPT-VISUAL)**: CONCERNS accepted 2026-04-26. Visual identity anchor selected with platform and scope caveats.
> **Technical Director Review (TD-FEASIBILITY)**: CONCERNS accepted 2026-04-26. Web-first 2D technical boundaries added.
> **Producer Review (PR-SCOPE)**: OPTIMISTIC accepted 2026-04-26. Scope reduced to a 2D Web-first official lightweight product.
> **Platform Pivot Note (2026-05-09)**: ADR-0019 supersedes the original Web-first product target. Active MVP implementation is desktop Godot 4.6.2 .NET/C# with keyboard/mouse input; older browser-specific scope notes are historical constraints unless restated by refreshed architecture/design docs.

---

## Elevator Pitch

《云海织航》是一款桌面 2D 空海探索与移动家园游戏。玩家在只有空中陆地和无尽海面的世界里经营一艘可步行飞艇，通过整备、航线规划、探索搜撤和设施修复，把孤立的空港与村镇重新连接起来。

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 2D 开放式探索 / 移动家园 / 轻搜撤 / 世界修复 |
| **Platform** | Desktop-first：Godot 4.6.2 .NET/C#；Windows 桌面优先；其他桌面平台后续评估；Web 不再是 MVP 目标 |
| **Target Audience** | 喜欢创造、探索、低压规划、基地成长和世界反馈的中度玩家 |
| **Player Count** | 单人优先 |
| **Session Length** | 30-90 分钟；桌面 MVP 目标为 45-90 分钟短体量正式产品 |
| **Monetization** | 待定；桌面 MVP 可按免费试玩、付费轻量版或后续扩展再评估 |
| **Estimated Scope** | Medium (5-9 months, solo / small team, desktop-first 2D official lightweight version); broader full version is Large (1-2+ years, scope dependent) |
| **Comparable Titles** | 《方舟：生存进化》、Dark and Darker、《杀戮尖塔》、《永劫无间》 |

---

## Core Fantasy

玩家拥有一艘真正能住进去、能整理、能改造、能出航的飞艇。它不是菜单里的载具，而是玩家在空海世界里的家、工具箱和移动据点。

每一次出航都从准备开始：读航线情报，检查模块，安排货物和伙伴功能，再进入未知航线或小型探索点。玩家带回的不只是数值奖励，而是让世界发生变化的材料、情报和连接。灯塔被重新点亮，航线重新稳定，村镇重新有人往来，玩家逐渐感觉自己不是征服世界，而是在修补一个曾经破碎的空海社会。

---

## Unique Hook

它像《方舟：生存进化》的生存建造和驯养幻想，AND ALSO 结合低频高价值的搜撤冒险、可步行飞艇家园、航线网络和空港修复。撤出的资源不只用于让玩家变强，而是用于让飞艇、村镇、航线和世界状态一起变好。

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Expression** | 1 | 通过飞艇舱室、模块选择、航线规划、修复优先级和世界恢复路径，让玩家表达自己的生存与发展方式。 |
| **Discovery** | 2 | 通过未知航线、风流、风险标记、探索点、遗迹情报和村镇需求，让玩家逐步理解空海世界。 |
| **Submission** | 3 | 普通航线和飞艇整理保持低压，允许玩家慢慢整备、跑航线、看世界变好。 |
| **Fantasy** | 4 | 玩家扮演空海世界里的飞艇船长、修复者和航线开拓者。 |
| **Challenge** | 5 | 高风险航线和探索搜撤提供规划压力、资源取舍和撤离判断。 |
| **Narrative** | 6 | 世界状态、村镇变化、伙伴功能和设施修复形成轻叙事。 |
| **Sensation** | 7 | 通过 2D 横版飞艇剖面、航标地图语言、灯塔信号色、风暴和修复视觉反馈提供感官满足。 |
| **Fellowship** | N/A | 首发版本不做多人；相关性通过伙伴、村镇和 NPC 状态体现。 |

### Key Dynamics

- 玩家会在出航前阅读航线风险，并根据目标调整模块、货物和伙伴功能。
- 玩家会把探索得到的情报转化为下一次更好的路线规划。
- 玩家会在“修复世界”和“优化飞艇”之间分配有限资源。
- 玩家会记住少量关键人物、伙伴或空港，而不是追逐大量同质收集。
- 玩家会把高风险探索视为偶尔进行的关键行动，而不是持续高压主玩法。

### Core Mechanics

1. **飞艇整备与可步行 Hub**：横版剖面飞艇内部提供行走、舱室交互、模块安装、货物整理和伙伴驻点。
2. **航线规划与风险读取**：俯视或地图式航线界面展示安全线、高风险/未知线、资源点、风险提示和撤离信息。
3. **2D 探索 / 搜撤场景**：短小探索点提供采集、风险判断、有限资源取舍和撤离。
4. **世界修复反馈**：带回材料或情报后修复一个设施，永久改变航线、NPC 状态或视觉状态。
5. **伙伴功能**：首发只做一个功能动词，例如侦察，用于揭示风险或资源。

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** | 玩家决定出航准备、航线选择、资源用途、修复顺序和飞艇改造方式。 | Core |
| **Competence** | 玩家通过理解风流、航线风险、探索点规则和资源循环，越来越会规划远征。 | Core |
| **Relatedness** | 玩家通过飞艇家园、少量伙伴、村镇变化和空港复苏获得归属感。 | Supporting |

### Player Type Appeal

- [x] **Achievers** — 通过航线稳定、设施修复、飞艇升级和世界阶段变化获得进度满足。
- [x] **Explorers** — 通过理解未知航线、探索点、风暴规则和世界节点关系获得主要乐趣。
- [x] **Socializers** — 首发不是多人社交，但通过伙伴、NPC 和村镇状态满足轻度关系需求。
- [ ] **Killers/Competitors** — 不服务高频 PvP、支配、排行榜或强竞技。

### Flow State Design

- **Onboarding curve**：先让玩家在飞艇里完成一次整备，再选择安全航线，最后进入一个短探索点并修复一个设施。
- **Difficulty scaling**：普通航线低压力；高风险/未知线增加情报不足、资源取舍和撤离判断。
- **Feedback clarity**：模块状态、航线风险、材料用途、设施修复和世界变化必须清楚可见。
- **Recovery from failure**：失败应以教育性损失为主，例如带回较少、船体受损、需要维修；不做恶劣惩罚和硬限时主压迫。

---

## Core Loop

### Moment-to-Moment (30 seconds)

玩家查看当前目标、航线情报、村镇需求或船只状态；在飞艇内部行走并调整模块、货物、补给和伙伴功能；选择普通航线或高风险/未知航线；在航行与探索中根据风险、资源点和撤离条件做小决策；回到飞艇或空港后继续整备和建设。

### Short-Term (5-15 minutes)

1. 从空港、村镇、天气图、旧航海日志或飞艇传感器获得未知线索。
2. 整备飞艇：选择模块、货物、维修件、伙伴功能和撤退预案。
3. 进入未知航线或探索点，侦查资源、风险和可撤离路线。
4. 判断继续深入、标记以后再来、采集少量资源或撤离。
5. 带着情报或材料回来，用于修复世界节点或优化飞艇下一次探索能力。

### Session-Level (30-120 minutes)

普通 Session 以探明一条未知航线、获得一次飞艇升级或优化下一次出航为自然句点。重要 Session 以修复关键空港设施、改变村镇状态，或完成一次高风险探索并成功带回关键资源为自然句点。

### Long-Term Progression

1. **玩家知识成长**：理解风流、天气、遗迹、资源、风险和航线规律。
2. **世界成长**：空港、村镇、航线网络、贸易关系和 NPC 迁徙逐渐恢复。
3. **飞艇成长**：移动家园更可靠、更舒适、更能支持远征。
4. **伙伴成长**：少量伙伴提供情感连接和功能补足，但不抢主轴。

### Retention Hooks

- **Curiosity**：未探明的航线、未修复的设施、未解释的风暴区和远方空港。
- **Investment**：飞艇内部变化、设施修复后的永久世界反馈、村镇状态和伙伴功能。
- **Social**：首发不做多人；后续可通过社区分享路线、修复选择和挑战结果补足。
- **Mastery**：更好的出航规划、更少损耗、更高价值的撤离、更有效的修复路径。

---

## Game Pillars

### Pillar 1: 规划先于冒险

每次出航前的路线、模块、货物和风险判断，都应该让玩家感觉自己在做有意义的准备。

*Design test*: 如果在“即时反应更爽”和“准备与判断更有回报”之间取舍，优先选择后者。

### Pillar 2: 世界会回应照料

玩家带回的资源、情报和连接，必须让村镇、空港、航线或 NPC 生活产生可见变化。

*Design test*: 如果一个高价值奖励长期只让玩家个人数值变强，却不推动村镇、空港、航线或 NPC 生活的任何可见变化，优先把其中一部分转化为世界状态的推进。

*Responsibility boundary*: 这个支柱管外部世界是否被修复、连通、居住化。

### Pillar 3: 飞艇是家，不只是载具

飞艇要承载居住、整备、存储、个性化、旅途安全感和玩家身份。

*Design test*: 如果某个飞艇系统只是交通效率提升，但不能强化“这是我的家”，就要重新设计。

*Responsibility boundary*: 这个支柱管内部飞艇是否可栖居、可依赖、可认同。当一个设计同时服务 Pillar 2 和 Pillar 3 时，以主要情感落点决定主归属。

### Pillar 4: 未知带来温和压力

普通探索应允许放松和调整；只有未知航线、风暴区和小型探索点才提高风险与准备要求。

*Design test*: 如果压力来自硬限时或频繁惩罚，而不是情报不足和准备取舍，就削弱它。

### Pillar 5: 少量深关系胜过大量收集

伙伴、船员、村镇和空港不追求数量堆叠，而追求记忆点、功能差异和长期关系。

*Design test*: 如果一个伙伴只是第 N 个可收集单位，优先删减或合并成更有身份的角色。

### Anti-Pillars

- **NOT 官方服务器式恶劣 PvP**：它会破坏放松、居住和长期建设感。
- **NOT 强限时压力主玩法**：核心体验是规划、探索和流动，不是持续倒计时。
- **NOT 大规模无限驯养收集**：它会挤压飞艇、航线和世界反哺主轴。
- **NOT 纯跑腿贸易**：世界变化必须可见，航线连接要带来新状态。
- **NOT 只加数值的成长**：主要成长应是知识、世界和生活空间的变化。

---

## Visual Identity Anchor

### Selected Direction

**主锚点：航路修复主义**

One-line visual rule: 每个画面都要像一个正在被修补、扩建、重新连通的世界与飞艇。

### Supporting Visual Principles

1. **修补痕迹可见**
   - 视觉原则：飞艇、空港、设施和航线节点都应带有拼接、维修、改造或再利用的痕迹。
   - 设计测试：如果一个场景或物件看不出磨损、修补、拼接或功能改造，它就不属于核心视觉方向。

2. **航标地图诗学作为系统语言**
   - 视觉原则：航线、风险、资源、补给和已连通状态应像一张活着的航海图。
   - 设计测试：玩家一眼应能判断去哪、哪里危险、哪里可补给、哪里已连通。

3. **乡土空海民俗作为文化层**
   - 视觉原则：每个岛屿、村镇和伙伴都应像有真实生活痕迹的地方文化，而不是泛用奇幻背景板。
   - 设计测试：不看文字说明，也能从轮廓、旗帜、纹样或颜色判断这个聚落或伙伴的地区身份。

### Color Philosophy

基础色使用低饱和海蓝、雾灰、旧木色、氧化金属色和帆布米色。信号色只用于灯塔、危险提示、可交互节点和关键资源。航线与系统语言可使用路线金、航标青和雾白；村镇和伙伴使用地方性强调色，避免所有地区看起来同质。

### 2D Presentation

首发版本采用混合 2D 表现：飞艇内部为横版剖面可步行空间，用来表达“家”和舱室生活；航线与探索采用俯视或地图式表现，用来表达规划、未知路线和撤离判断。

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| 《方舟：生存进化》 | 第一次驯服恐龙、大型生存建造、拥有自己的基地和伙伴 | 不做官方服务器式恶劣 PvP，不做大规模无限驯养收集 | 验证“拥有、建设、长期成长”的吸引力 |
| Dark and Darker | 搜打撤的风险、带回、取舍和撤离满足感 | 搜撤是低频高价值活动，不是高压主玩法 | 提供关键资源和未知风险，而不压倒家园与世界修复 |
| 《永劫无间》 | 与朋友一起在吃鸡热潮里体验紧张对抗和协作 | 首发不做多人竞技；只保留风险判断和局内张力 | 说明适度紧张与共同经历能增强记忆点 |
| 《杀戮尖塔》 | 可重复游玩、消磨时间、短中期目标清楚 | 不做卡牌核心；借鉴清晰循环、可读选择和“再来一局”的节奏 | 支持 Web 轻量产品的短 Session 结构 |

**Non-game inspirations**: 漂浮群岛、航海图、灯塔、旧港口、修补后的船舱、风暴后的海面、地方织物与旗帜。

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 16-35 |
| **Gaming experience** | 中度玩家；喜欢生存、建造、探索、轻经营或短循环策略 |
| **Time availability** | 平日 30-60 分钟，周末可进行 90 分钟左右完整 Session |
| **Platform preference** | Windows 桌面优先；Godot 4.6.2 .NET/C#；其他桌面平台后续评估 |
| **Current games they play** | 生存建造、轻搜撤、基地经营、探索类独立游戏、短循环策略游戏 |
| **What they're looking for** | 能创造和居住的移动家园、低压但有取舍的探索、能看见世界变好的反馈 |
| **What would turn them away** | 高频 PvP、强限时、恶劣惩罚、大量同质刷子内容、无反馈跑腿任务 |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Godot 4.6.2 .NET / C#。ADR-0019 是当前权威平台决策；Web-first 引擎选择问题已关闭。 |
| **Key Technical Challenges** | Godot .NET/C# 工程与构建、2D 横版飞艇 Hub 与地图式探索切换、桌面本地存档和恢复、窗口焦点/暂停/退出恢复、世界状态持久化。 |
| **Art Style** | 2D 风格化；横版飞艇剖面 + 俯视/地图式航线与探索。 |
| **Art Pipeline Complexity** | Medium：自定义 2D 场景、舱室、地图符号、角色/伙伴和状态变化；首发必须服从 Compatibility renderer 桌面性能预算。 |
| **Audio Needs** | Moderate：飞艇环境声、航线风声、危险提示、修复反馈、轻量音乐层次。 |
| **Networking** | None for launch. |
| **Content Volume** | 桌面 MVP：1 个 Hub、2 个模块、1 个起始据点、2 条航线、1 个探索点、1 个伙伴功能、1 个永久修复反馈。 |
| **Procedural Systems** | 首发不做重度程序生成。未知感来自手工设计的风险标记、航线状态和探索点取舍。 |

### Technical Boundaries

- 桌面 MVP 是正式轻量产品，不是纯 Demo。
- Windows 桌面优先；Linux / macOS 或 Web 仅作为后续独立评估目标。
- 不承诺未来 Web 与桌面 C# MVP 共用同一代码库或同一工程。
- 不做连续开放世界、无缝空海航行或大型动态载具模拟。
- 不做持续 NPC 生态、复杂贸易网络、实时多人或高频网络同步。
- 桌面窗口失焦、暂停/退出请求、本地存档、首屏加载和重新聚焦行为是设计约束，不是后期实现细节。
- 首发前必须验证：加载、性能、存档恢复、音频、窗口焦点/暂停恢复、整备到修复的完整闭环。

---

## Risks and Open Questions

### Design Risks

- **整备可能变繁琐**：如果准备环节只是重复菜单劳动，会把“规划先于冒险”误实现成负担。
- **世界反馈可能太弱**：如果修复只改变数值，玩家不会相信世界真的被照料。
- **搜撤可能抢主轴**：如果高风险探索太频繁或太刺激，会把游戏拉向标准搜打撤。

### Technical Risks

- **桌面交付稳定性**：加载、帧率、内存、存档、音频和窗口焦点/暂停恢复必须在早期验证。
- **2D 视角切换一致性**：横版飞艇与俯视/地图式探索需要清晰交互和叙事衔接。
- **世界状态持久化**：设施修复、航线变化和 NPC/视觉状态必须可靠保存。

### Market Risks

- **定位需要解释**：它不是标准生存、不是标准搜撤、也不是纯经营，宣传必须抓住“飞艇家园 + 世界复连”。
- **桌面玩家耐心有限**：首屏加载、引导和第一轮闭环必须足够快。
- **轻量版体量较小**：如果完成感不足，可能被当成 Demo 而非正式产品。

### Scope Risks

- **可步行飞艇空间膨胀**：舱室数量、装修、角色行为和交互容易扩张。
- **伙伴系统膨胀**：必须只保留一个功能动词，避免同行 AI、关系树和收集系统过早进入。
- **美术野心膨胀**：2D 风格化仍可能变贵，首发要优先强轮廓、低复杂度和清晰状态反馈。

### Open Questions

- **Godot .NET 桌面工程验证是否完整？** 通过 Sprint 001 验证 `.csproj` / `.sln`、`dotnet build`、桌面/headless 启动和 C# Foundation spike。
- **飞艇内部横版行走手感是否成立？** 通过技术验证原型验证舱室移动、交互、模块切换和状态显示。
- **地图式探索是否足够有张力？** 通过灰盒测试验证安全线与高风险线的对照。
- **修复反馈是否足够强？** 通过 1 个设施修复后的永久航线/NPC/视觉变化验证。

---

## MVP Definition

**Core hypothesis**: 玩家会喜欢“在可步行飞艇中整备 -> 选择航线 -> 进入短探索/搜撤 -> 带回资源或情报 -> 修复设施 -> 看见世界永久变化”的低压规划循环。

**Required for MVP**:
1. 1 个横版剖面可步行飞艇 Hub，包含 2-4 个小舱室或区域。
2. 2 个核心模块，建议从侦察模块 + 货仓/维修模块开始。
3. 1 个起始空港/村镇。
4. 2 条航线：安全线 + 未知/高风险线。
5. 1 个俯视或地图式探索点，包含搜集、风险、撤离。
6. 1 个伙伴功能，建议先做侦察，用于揭示风险或资源。
7. 1 个永久世界反馈：修复灯塔/航标后，解锁或稳定一条航线，并改变视觉或 NPC 状态。
8. 本地存档与恢复，至少可靠保存设施修复、资源、模块状态和航线状态。

**Explicitly NOT in MVP**:
- 无缝开放世界。
- 真实动态飞艇载具模拟。
- 复杂经济、贸易价格联动或大量 NPC 持续模拟。
- 多人、PvP、排行榜。
- 大规模伙伴/驯养/船员收集。
- 多个探索点、多种副本类型、多设施修复分支。
- 移动浏览器正式支持。
- PC 完整版技术栈承诺。

### Scope Tiers

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **Technical Prototype** | 1 个简化 Hub、1 个简化探索点 | 加载、性能、输入、音频、存档、窗口焦点/暂停恢复、最短闭环验证 | 2-4 weeks |
| **Greybox Vertical Slice** | 1 个 Hub、1 条航线、1 个探索点、1 个材料、1 个修复结果 | 整备 -> 出航 -> 搜撤 -> 带回 -> 修复 | 4-8 weeks after prototype |
| **Web Official Lightweight Version** | 1 个 Hub、2 个模块、1 个起始据点、2 条航线、1 个探索点、1 个伙伴功能、1 个永久修复反馈 | 可交付短体量产品、完整 Session、存档恢复、基础引导、Web 发布包装 | 5-9 months total, solo / small team |
| **PC-Later Full Vision** | 1 个区域群、4-6 个空港/村镇、10-15 条航线、3-5 个探索/副本类型、3-6 个深关系伙伴 | 更完整的飞艇成长、世界阶段、伙伴关系、视觉表现和内容体量 | Large (1-2+ years, team and tech dependent) |

---

## Recommended Next Steps

1. Run `$setup-engine` to configure the engine and populate version-aware reference docs.
2. Run `$art-bible` to create the visual identity specification — do this BEFORE writing GDDs. The art bible gates asset production and shapes technical architecture decisions (rendering, VFX, UI systems).
3. Use `$design-review design/gdd/game-concept.md` to validate concept completeness before going downstream.
4. Discuss vision with the `creative-director` agent for pillar refinement.
5. Decompose the concept into individual systems with `$map-systems` — maps dependencies, assigns priorities, and creates the systems index.
6. Author per-system GDDs with `$design-system` — guided, section-by-section GDD writing for each system identified in step 5.
7. Plan the technical architecture with `$create-architecture` — produces the master architecture blueprint and Required ADR list.
8. Record key architectural decisions with `$architecture-decision (×N)` — write one ADR per decision in the Required ADR list from `$create-architecture`.
9. Validate readiness to advance with `$gate-check` — phase gate before committing to production.
10. Prototype the riskiest system with `$prototype core-loop` — validate the core loop before full implementation.
11. Run `$playtest-report` after the prototype to validate the core hypothesis.
12. If validated, plan the first sprint with `$sprint-plan new`.
