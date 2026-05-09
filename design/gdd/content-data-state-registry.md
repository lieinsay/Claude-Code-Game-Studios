# 内容数据与状态注册表

> **Status**: In Design
> **Author**: User + Codex
> **Last Updated**: 2026-05-09
> **Implements Pillar**: 规划先于冒险; 世界会回应照料; 飞艇是家，不只是载具
> **System Index**: `design/gdd/systems-index.md`
> **Platform Pivot Note**: ADR-0019 supersedes Web/GDScript implementation assumptions. Active registry implementation targets desktop Godot .NET/C#; browser-only diagnostics and GDScript API names are legacy notes until refreshed.

## Overview

`内容数据与状态注册表` 是《云海织航》的静态内容契约层。它定义所有跨系统共享内容的稳定 ID、分类、Schema 和查询边界，包括资源、货物、飞艇模块、飞艇家园空间、生活锚点、航线、地点、修复节点、摊位商品、伙伴、威胁与情报条目。这个系统不拥有玩家进度、运行时状态或解锁结果；它只保证后续系统引用同一套内容语言，避免资源、航线、商品、修复节点在不同 GDD 或实现中出现重复命名、冲突字段或双重真相源。

玩家不会直接操作这个系统，但会通过它间接感受到世界的一致性：航图上看到的风险、探索点带回的材料、空港摊位出售的商品、飞艇模块需要的零件、修复灯塔消耗的资源，都能稳定地指向同一套内容定义。没有这个系统，`规划先于冒险` 会失去可靠信息基础，`世界会回应照料` 会难以保存和追踪，`飞艇是家` 也会因为模块、货物和空间状态命名混乱而变得不可维护。

## Player Fantasy

玩家不会直接操作 `内容数据与状态注册表`，但应该通过它感到世界的信息是可靠的。材料、货物、飞艇模块、航线、地点、威胁、摊位商品和情报都有稳定身份，因此玩家在整备、规划航线、判断风险、购买补给或准备修复时，不是在和含糊或矛盾的数据搏斗，而是在阅读一套可信的空海世界秩序。

这个系统在幕后支撑一种间接幻想：玩家相信自己的准备有依据，带回的东西有明确用途，修复行为有清楚因果。它不负责保存进度或推动解锁，但它保证“带回什么、指向哪里、能修什么、属于谁”不会在不同系统中变成不同答案。玩家感受到的不是注册表本身，而是世界命名一致、关系清楚、照料结果可信的安心感。

## Detailed Design

### Core Rules

1. 本系统是全游戏静态内容的唯一目录和校验入口，负责定义 ID、分类、Schema、标签、引用关系和只读查询契约。
2. 本系统只拥有静态内容定义，不拥有任何玩家态、世界态、解锁态、库存态、可见性态、关系态、局内遭遇态或存档态。
3. 任何可被跨系统引用的内容，都必须先拥有一个稳定 ID。
4. 下游系统只能通过稳定 ID 引用内容定义，不能通过显示名、文件路径、数组顺序、资源路径、翻译文本或本地缓存副本作为真相源。
5. 静态内容定义在运行时视为只读。任何运行时变化必须由对应领域系统拥有。
6. 本系统输出的是 canonical definition，不是 runtime instance。
7. 本系统可以支持按内容域、类别或 ID 的局部可用；不要求启动时一次性加载全部内容。
8. 查询契约必须区分：存在、未加载、不存在、已废弃、版本不兼容。
9. 所有列表型查询必须返回确定性顺序，不能依赖字典遍历顺序、导出顺序或文件顺序。
10. Schema 必须保持明确类型、低嵌套、低异构。新增内容类别需要更新 GDD 或后续架构说明，不能随意塞进万能字段。
11. 内容 ID 一旦进入 `Active` 状态就不可复用。旧 ID 退役后可以保留兼容映射，但不能赋予新含义。
12. 错误必须可诊断，并能在桌面开发和运行环境中定位：重复 ID、缺字段、引用缺失、Schema 版本不兼容、非法查询条件、资源引用失效。

### States and Transitions

这里的状态只表示内容定义生命周期，不表示运行时状态。

| State | Meaning | Valid Transitions |
|---|---|---|
| `Draft` | 制作中，未进入可用内容集 | `Active` |
| `Active` | 当前权威定义，可被新内容引用和查询 | `Deprecated` |
| `Deprecated` | 仍可解析，用于兼容旧内容或旧存档，但新内容不应引用 | `Retired` |
| `Retired` | 保留兼容痕迹，不应被新内容引用 | None |

无效状态或数据错误：

- ID 重复。
- 引用不存在，且字段未明确标记为可选。
- `kind` 与 Schema 不匹配。
- 同一静态事实在多个系统中各自定义。
- 内容定义中出现运行时字段，例如当前数量、当前库存、当前价格、已解锁、已发现、已修复、当前耐久、当前关系值。
- 查询结果依赖未定义排序。
- 同一 ID 被退役后重新赋予新含义。

### Interactions with Other Systems

| System | Registry Provides | Explicitly Does Not Own |
|---|---|---|
| `资源、货物与容量` | 资源 ID、货物 ID、分类、单位、标签、静态容量/处理类别引用 | 当前数量、任务内掉落、携带状态、交易结果 |
| `飞艇家园 Hub` | 飞艇家园空间 ID、生活锚点 ID、空间功能标签、交互锚点标签 | 玩家当前位置、当前可交互状态、舱室占用、家具摆放、生活事件进度 |
| `飞艇模块与船体状态` | 模块 ID、槽位类型、兼容标签、静态效果类别 | 当前安装、耐久、损坏、维修状态 |
| `航图与航线规划` | 航线 ID、起终点地点 ID、距离带、风险标签、环境标签 | 是否已探索、是否开放、当前是否可通行 |
| `玩家知识与情报` | 情报条目 ID、情报类型、静态关联目标、来源标签 | 已知/未知、传闻真假、风险提示是否已揭示 |
| `世界修复与解锁` | 修复节点 ID、地点 ID、节点类型、修复主题、静态引用 | 可修复、已修复、解锁结果、世界状态变化 |
| `空港 / 村镇状态与集市交易` | 地点 ID、摊位商品 ID、商品分类、供给标签、地方性标签 | 当前库存、缺货、价格变化、摊位是否开放 |
| `伙伴功能与关系` | 伙伴 ID、角色标签、来源地点、功能定位标签 | 当前关系、是否同行、剧情进度、驻点状态 |
| `战斗与威胁处理` | 威胁 ID、威胁类型、遭遇标签、对抗标签、严重度层级 | 威胁是否生成、局内强度、当前行为、战斗结果 |
| `本地存档与世界状态持久化` | 可保存的稳定 ID 引用 | 注册表本身的运行时状态；存档系统不应序列化注册表定义为玩家状态 |
| `UI / HUD / 航图界面` | 显示名键、描述键、标签、排序键、静态图标/分类引用 | UI 当前选择、hover、筛选状态、运行时可见性 |

### Content Domain Contract

内容域是静态内容的加载、校验和查询边界，不是玩法系统边界，也不是运行时状态容器。一个内容项只能有一个 `owner_domain`，但可以通过 `references` 指向其他内容域中的稳定 ID。

MVP 内容域如下：

| Domain | Contains | Required Before Player-Facing Use |
|---|---|---|
| `resources` | `resource`, `cargo` | 资源、货物、修复和市场界面显示材料用途前必须 `COMPLETE` |
| `airship` | `module`, `home-space`, `home-anchor` | 飞艇家园、模块安装和整备界面打开前必须 `COMPLETE` |
| `world` | `location`, `repair-node`, `stall-good` | 修复、空港、村镇和市场界面打开前必须 `COMPLETE` |
| `routes` | `route` | 航图、航线选择和风险比较界面打开前必须 `COMPLETE` |
| `intel` | `intel` | 情报、传闻和风险提示界面打开前必须 `COMPLETE` |
| `companions` | `companion` | 伙伴驻点、侦察和关系反馈界面打开前必须 `COMPLETE` |
| `threats` | `threat` | 航行风险、探索和威胁处理需要展示威胁定义前必须 `COMPLETE` |

内容域加载状态：

| Domain State | Meaning | Query Behavior |
|---|---|---|
| `UNLOADED` | 该域尚未进入注册表加载流程 | 查询该域 ID 返回 `UNLOADED` |
| `LOADING` | 该域正在加载或校验，结果不可作为玩家决策依据 | 玩家界面不得使用该域结果；开发工具可显示加载中 |
| `PARTIAL` | 该域有部分内容可用于开发诊断，但完整性未确认 | 玩家界面不得把查询结果当作完整列表 |
| `COMPLETE` | 该域已加载、校验并确认没有阻断错误 | 玩家界面可以使用该域结果 |
| `FAILED` | 该域加载或校验失败 | 玩家界面不得进入依赖该域的决策流程 |

玩家决策界面必须使用完整内容域或冻结快照：

- 航图与航线规划界面至少需要 `routes`、`world`、`intel` 和相关 `threats` 为 `COMPLETE`，否则只能显示安全错误或等待状态，不能给出半完整航线选择。
- 飞艇家园与整备界面至少需要 `airship`、`resources` 和相关 `companions` 为 `COMPLETE`，否则不能展示可安装模块、舱室锚点或伙伴功能选择。
- 世界修复和市场界面至少需要 `world`、`resources` 和相关 `intel` 为 `COMPLETE`，否则不能展示修复需求、摊位商品或材料用途。
- 进入一个玩家决策界面后，该界面使用的内容定义集合视为 snapshot。界面打开期间不得因异步加载、缓存刷新或内容包重载改变可选项、排序或引用结果；需要刷新时必须关闭当前决策、重新校验并重建 snapshot。
- `PARTIAL` 只允许用于开发期诊断、内容作者工具和错误定位；正式玩家流程不得把 `PARTIAL` 结果显示成可行动选择。

### Naming and ID Rules

- ID 使用 `kind.slug` 形式。
- ID 全小写，slug 使用短横线。
- 字段名使用 `snake_case`。
- ID 不得使用中文显示名、翻译文本、文件名、资源路径或场景路径。
- 显示名和描述必须通过 `name_key` / `description_key` 这类字段引用。
- ID 只表达内容身份，不表达当前状态。
- 示例：
  - `resource.iron-ore`
  - `cargo.coastal-crate`
  - `module.wind-sail-mk1`
  - `home-space.map-room`
  - `home-anchor.chart-table`
  - `route.sky-reef-arc-01`
  - `location.glass-harbor`
  - `repair_node.starlight_dock`
  - `stall-good.fresh-rations`
  - `companion.tide-scout`
  - `threat.mist-raider`
  - `intel.lost-channel-rumor`

### Minimum Schema

公共字段：

| Field | Purpose |
|---|---|
| `id` | 稳定主键 |
| `kind` | 内容类型 |
| `name_key` | 显示名引用 |
| `description_key` | 描述文本引用 |
| `schema_version` | Schema 版本 |
| `tags` | 检索和分类标签 |
| `status` | `Draft` / `Active` / `Deprecated` / `Retired` |
| `sort_order` | 确定性排序键 |
| `owner_domain` | 内容归属领域 |
| `references` | 静态 ID 引用，不包含运行时结果 |
| `cat_sniff_signature` | (optional, on `resource` kind) 伙伴嗅探签名：`reveal_target` (location_id), `hazard_hint` (string), `confidence` (0-100), `pattern_id` (string)。#15 sky-cat 的 scout sniff 动词消费此字段。`reveal_target` 必须引用有效 `location_id`。 |

类型专属最小字段：

| Kind | Required Fields |
|---|---|
| `resource` | `unit`, `stack_rule`, `material_tags`, `cat_sniff_signature` (optional) |
| `cargo` | `linked_resource_id`, `mass_class`, `handling_class` |
| `module` | `slot_type`, `compatibility_tags`, `effect_tags` |
| `home-space` | `space_kind`, `home_function_tags`, `access_tags` |
| `home-anchor` | `home_space_id`, `anchor_kind`, `interaction_tags`, `home_feedback_tags` |
| `route` | `origin_location_id`, `destination_id`, `distance_band`, `hazard_tags` |
| `location` | `region_tag`, `location_kind`, `service_tags`, `local_identity_tags`, `settlement_need_tags` |
| `repair-node` | `location_id`, `node_kind`, `restoration_theme`, `settlement_need_tags`, `repair_visible_state_tags` |
| `stall-good` | `commodity_tags`, `vendor_tags`, `supply_class`, `local_identity_tags`, `settlement_need_tags`, `repair_visible_state_tags` |
| `companion` | `role_tags`, `origin_location_id`, `archetype_tags` |
| `threat` | `threat_class`, `encounter_tags`, `counter_tags`, `severity_tier` |
| `intel` | `entry_type`, `linked_content_ids`, `source_tags`, `presentation_tier` |

### Controlled Vocabularies

以下字段不是自由标签。新增值需要更新本 GDD 或后续架构说明，并提供至少一个使用场景和校验规则。

| Field | MVP Allowed Values | Notes |
|---|---|---|
| `owner_domain` | `resources`, `airship`, `world`, `routes`, `intel`, `companions`, `threats` | 必须与内容域契约一致 |
| `kind` | `resource`, `cargo`, `module`, `home-space`, `home-anchor`, `route`, `location`, `repair-node`, `stall-good`, `companion`, `threat`, `intel` | 必须与 ID 前缀一致 |
| `region_tag` | `starter-sea`, `sky-reef`, `storm-belt`, `old-harbor-chain` | 地理归属标签，不是稳定 ID 引用；MVP 只需要 `starter-sea` |
| `settlement_need_tags` | `food`, `repair-materials`, `navigation-aid`, `safety`, `trade-link`, `home-comfort` | 表示聚落或空港的静态需求主题，不表示当前是否已满足 |
| `repair_visible_state_tags` | `dark`, `damaged`, `patched`, `lit`, `connected`, `inhabited`, `stock-improved` | 表示修复前后可被下游系统呈现的静态状态语义，不表示玩家是否已经修复 |
| `home_function_tags` | `storage`, `planning`, `rest`, `module-access`, `companion-station`, `crafting-light` | 表示飞艇空间可承载的静态功能 |
| `hazard_tags` | `safe`, `mist`, `storm`, `raider`, `low-visibility`, `unstable-current` | 表示航线或遭遇的静态风险语义 |
| `severity_tier` | `minor`, `moderate`, `severe` | MVP 威胁严重度层级，不直接等同于伤害数值 |
| `supply_class` | `basic`, `repair`, `navigation`, `local-specialty`, `intel` | 摊位商品供给类别，不表示当前库存 |
| `presentation_tier` | `hint`, `clue`, `warning`, `lore` | 情报展示层级，不表示玩家是否已知 |

受控词表的值只能表达静态语义。它们不能表达库存数量、当前价格、修复完成、是否解锁、关系值、当前风险强度或 UI 可见性。

### Content Validation Rules

以下校验规则在内容导入时执行，违规条目标记为 `ERR_SCHEMA_INVALID`：

| # | 规则 | 适用 kind | 错误码 |
|---|---|---|---|
| CV-01 | `cat_sniff_signature.reveal_target` 必须引用注册表中存在的有效 `location_id`（含 `kind: location`） | `resource` (含 `cat_sniff_signature` 的条目) | `ERR_SNIFF_TARGET_INVALID` |
| CV-02 | `cat_sniff_signature.confidence` 必须在 0-100 范围内 | `resource` (含 `cat_sniff_signature` 的条目) | `ERR_SNIFF_CONFIDENCE_RANGE` |

### Stable Identity and Migration Rules

以下内容类型承载核心幻想或存档连续性，属于 fantasy-critical ID：`route`、`location`、`repair-node`、`home-space`、`home-anchor`、`companion`。

- fantasy-critical ID 一旦进入 `Active`，后续只能追加兼容字段或进入 `Deprecated` / `Retired`，不能改义为另一个地点、房间、伙伴、修复目标或航线。
- 舱室升级、模块替换、修复结果和伙伴关系进展不能通过替换 `home-space`、`home-anchor` 或 `companion` ID 来表达；这些变化必须由对应运行时系统或存档系统拥有。
- 如果内容重命名但身份不变，必须保留原 ID，仅更新显示文本键或描述文本键。
- 如果内容被合并、拆分或移除，必须保留旧 ID 的 `Deprecated` 或 `Retired` 记录，并在迁移表中指向替代 ID、拒绝原因或旧存档兼容处理。
- 新内容不能引用 `Retired` ID；新 `Active` 内容也不能新增对 `Deprecated` ID 的引用，除非该引用明确标记为兼容路径。
- 存档系统可以保存稳定 ID 引用，但不能保存注册表定义副本作为玩家状态。读取旧存档时，注册表只负责解析旧 ID 的生命周期和迁移提示；具体存档迁移由 `本地存档与世界状态持久化` 拥有。

### Minimum Schema Examples

以下示例只表达字段语义，不指定最终存储格式。

```yaml
id: resource.iron-ore
kind: resource
owner_domain: resources
status: Active
name_key: content.resource.iron_ore.name
description_key: content.resource.iron_ore.desc
schema_version: 1
tags: [metal]
sort_order: 10
references: []
unit: chunk
stack_rule: stackable
material_tags: [metal, repair-material]
```

```yaml
id: cargo.coastal-crate
kind: cargo
owner_domain: resources
status: Active
name_key: content.cargo.coastal_crate.name
description_key: content.cargo.coastal_crate.desc
schema_version: 1
tags: [cargo]
sort_order: 20
references: [resource.iron-ore]
linked_resource_id: resource.iron-ore
mass_class: medium
handling_class: crate
```

```yaml
id: module.wind-sail-mk1
kind: module
owner_domain: airship
status: Active
name_key: content.module.wind_sail_mk1.name
description_key: content.module.wind_sail_mk1.desc
schema_version: 1
tags: [starter]
sort_order: 30
references: []
slot_type: sail
compatibility_tags: [starter-hull]
effect_tags: [route-stability]
```

```yaml
id: home-space.map-room
kind: home-space
owner_domain: airship
status: Active
name_key: content.home_space.map_room.name
description_key: content.home_space.map_room.desc
schema_version: 1
tags: [home]
sort_order: 40
references: []
space_kind: room
home_function_tags: [planning]
access_tags: [walkable, starter]
```

```yaml
id: home-anchor.chart-table
kind: home-anchor
owner_domain: airship
status: Active
name_key: content.home_anchor.chart_table.name
description_key: content.home_anchor.chart_table.desc
schema_version: 1
tags: [interaction]
sort_order: 50
references: [home-space.map-room]
home_space_id: home-space.map-room
anchor_kind: station
interaction_tags: [route-planning]
home_feedback_tags: [safe-preparation]
```

```yaml
id: route.sky-reef-arc-01
kind: route
owner_domain: routes
status: Active
name_key: content.route.sky_reef_arc_01.name
description_key: content.route.sky_reef_arc_01.desc
schema_version: 1
tags: [starter-route]
sort_order: 60
references: [location.glass-harbor, location.sky-reef-outpost]
origin_location_id: location.glass-harbor
destination_id: location.sky-reef-outpost
distance_band: short
hazard_tags: [safe]  # post-repair: originally [mist, low-visibility], reduced 30% by #13 repair_node.starlight_dock
```

```yaml
id: location.glass-harbor
kind: location
owner_domain: world
status: Active
name_key: content.location.glass_harbor.name
description_key: content.location.glass_harbor.desc
schema_version: 1
tags: [harbor]
sort_order: 70
references: []
region_tag: starter-sea
location_kind: harbor
service_tags: [market, repair]
local_identity_tags: [glass-buoys]
settlement_need_tags: [navigation-aid, trade-link]
```

```yaml
id: location.glass-harbor
kind: location
owner_domain: world
status: Active
name_key: content.location.glass_harbor.name
description_key: content.location.glass_harbor.desc
schema_version: 1
tags: [starting-port]
sort_order: 70
references: []
region_tag: starter-sea
location_kind: port
service_tags: [market, repair, chart, general]
local_identity_tags: [glass-buoys]
settlement_need_tags: [navigation-aid, trade-link]
```

```yaml
id: repair_node.starlight_dock
kind: repair-node
owner_domain: world
status: Active
name_key: content.repair_node.starlight_dock.name
description_key: content.repair_node.starlight_dock.desc
schema_version: 1
tags: [repair]
sort_order: 80
references: [location.glass-harbor]
location_id: location.glass-harbor
node_kind: beacon
restoration_theme: lighthouse
settlement_need_tags: [navigation-aid, safety]
repair_visible_state_tags: [dark, lit, connected]
```

```yaml
id: stall-good.fresh-rations
kind: stall-good
owner_domain: world
status: Active
name_key: content.stall_good.fresh_rations.name
description_key: content.stall_good.fresh_rations.desc
schema_version: 1
tags: [market]
sort_order: 90
references: [location.glass-harbor]
commodity_tags: [food]
vendor_tags: [harbor-stall]
supply_class: basic
local_identity_tags: [glass-harbor]
settlement_need_tags: [food]
repair_visible_state_tags: [stock-improved]
```

```yaml
id: companion.tide-scout
kind: companion
owner_domain: companions
status: Active
name_key: content.companion.tide_scout.name
description_key: content.companion.tide_scout.desc
schema_version: 1
tags: [scout]
sort_order: 100
references: [location.glass-harbor]
role_tags: [scout]
origin_location_id: location.glass-harbor
archetype_tags: [careful, local-guide]
```

```yaml
id: threat.mist-raider
kind: threat
owner_domain: threats
status: Active
name_key: content.threat.mist_raider.name
description_key: content.threat.mist_raider.desc
schema_version: 1
tags: [threat]
sort_order: 110
references: []
threat_class: raider
encounter_tags: [ambush, mist]
counter_tags: [scout-warning, retreat]
severity_tier: moderate
```

```yaml
id: intel.lost-channel-rumor
kind: intel
owner_domain: intel
status: Active
name_key: content.intel.lost_channel_rumor.name
description_key: content.intel.lost_channel_rumor.desc
schema_version: 1
tags: [rumor]
sort_order: 120
references: [route.sky-reef-arc-01]
entry_type: rumor
linked_content_ids: [route.sky-reef-arc-01]
source_tags: [harbor-gossip]
presentation_tier: clue
```

### Prohibited Patterns

- 不得存放玩家进度、解锁状态、库存状态、当前价格、已知/未知、修复进度、当前耐久、当前关系值。
- 不得把本系统变成全局状态管理器。
- 不得把查询接口设计成可写接口。
- 不得手工维护派生列表作为第二份真相源。
- 不得使用显示名、路径、数组下标、字典顺序或导出顺序作为引用依据。
- 不得用单一 mega-schema 堆满大量可空字段，让每个类型靠约定解释。
- 不得要求运行时扫描任意文件系统路径。
- 不得把具体存储格式、加载器、Godot Resource/JSON/Autoload 方案写死在 GDD 中；这些属于后续架构和 ADR。

## Formulas

本系统不定义战斗、经济、成长或概率公式；它只定义内容校验与查询确定性规则。

The `definition_validity` formula is defined as:

`definition_validity = has_unique_id AND matches_kind_schema AND required_fields_present AND has_no_runtime_fields`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `has_unique_id` | `U` | bool | true/false | ID 在注册表中唯一 |
| `matches_kind_schema` | `K` | bool | true/false | `kind` 与对应 Schema 匹配 |
| `required_fields_present` | `R` | bool | true/false | 必填字段完整 |
| `has_no_runtime_fields` | `S` | bool | true/false | 未包含库存、解锁、价格、耐久等运行时字段 |

**Output Range:** true/false；false 时内容不能进入 `Active`。
**Example:** `resource.iron-ore` ID 唯一、字段完整、Schema 正确且没有运行时字段，则结果为 true。

The `reference_validity` formula is defined as:

`reference_validity = required_refs_resolve AND allowed_status_refs AND no_self_invalid_cycle`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `required_refs_resolve` | `R` | bool | true/false | 所有必需引用都能解析到内容定义 |
| `allowed_status_refs` | `A` | bool | true/false | 新内容不引用 `Retired` 内容 |
| `no_self_invalid_cycle` | `C` | bool | true/false | 引用关系没有非法自循环或闭环依赖 |

**Output Range:** true/false；false 时记录具体缺失或非法引用。
**Example:** `route.sky-reef-arc-01` 的起点和终点地点 ID 都存在，且未引用退役地点，则结果为 true。

The `query_order_key` formula is defined as:

`query_order_key = sort_order ASC, id ASC`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `sort_order` | `O` | int | >= 0 | 设计指定排序键 |
| `id` | `I` | string | stable content ID | 相同排序键时的稳定补充排序 |

**Output Range:** 确定性列表顺序；不允许依赖文件顺序、导出顺序或字典遍历顺序。
**Example:** 两个商品 `sort_order = 10` 时，按 `id` 字典序稳定排序。

### Validation Diagnostic Precedence

`definition_validity` 和 `reference_validity` 只回答“能否进入可查询集合”。当多个错误同时存在时，诊断系统必须按确定性顺序输出错误，不能由实现任意选择第一条错误。

诊断输出规则：

1. 必须收集同一内容项或内容包上的所有可检测错误。
2. 主错误按下方优先级选择，用于列表排序、阻断范围和复制报告标题。
3. 其余错误作为 `related_errors` 附在同一诊断事件或同一内容项报告中。
4. `fatal` / `error` / `warning` 严重度优先于同级内部顺序；同级多项按 `error_code ASC, content_id ASC, field_path ASC` 稳定排序。

主错误优先级：

| Priority | Error Family | Examples | Blocking Scope |
|---:|---|---|---|
| 1 | 内容包不可用 | `ERR_CONTENT_PACKAGE_VERSION` | `package` / `registry` |
| 2 | ID 无法建立稳定身份 | `ERR_DUPLICATE_ID`, `ERR_ID_REUSE`, `ERR_INVALID_ID_FORMAT`, `ERR_ID_NORMALIZATION_COLLISION` | `registry` / `package` |
| 3 | Schema 无法解释 | `ERR_SCHEMA_INVALID`, missing required field, invalid controlled vocabulary value | `item` |
| 4 | 静态/运行时边界破坏 | `ERR_RUNTIME_FIELD_IN_STATIC_DATA`, `ERR_READONLY_REGISTRY` | `item` / `runtime-query` |
| 5 | 引用目标缺失或未加载 | `ERR_MISSING_REFERENCE`, `UNLOADED_REFERENCE` | `item` |
| 6 | 引用生命周期非法 | `ERR_REFERENCE_TO_DRAFT`, `ERR_REFERENCE_TO_DEPRECATED`, `ERR_REFERENCE_TO_RETIRED` | `item` |
| 7 | 引用图结构非法 | `ERR_REFERENCE_CYCLE` | `item` / `package` |
| 8 | 查询不稳定 | `ERR_INVALID_SORT_KEY`, `AMBIGUOUS_QUERY`, `ERR_UNSTABLE_IDENTIFIER` | `runtime-query` |

示例：如果一个 `route` 同时引用缺失地点、引用 `Retired` 威胁，并形成循环，主错误为 `ERR_MISSING_REFERENCE` 或 `UNLOADED_REFERENCE`，生命周期和循环错误进入 `related_errors`；诊断详情仍必须展示完整引用链。

## Edge Cases

| 情况 | 处理 |
|---|---|
| 同一 ID 在任何内容包、分片、导入产物或热更新包中重复出现 | `ERR_DUPLICATE_ID`，整批注册失败；禁止覆盖、合并或按加载顺序取最后值。 |
| `Active` ID 被删除、复用或改义 | `ERR_ID_REUSE`；必须保留 `Deprecated` / `Retired` 记录或显式迁移映射。 |
| 查询 ID 所属内容域未加载 | 返回 `UNLOADED`；不得返回 `NOT_FOUND`，不得触发隐式文件扫描。 |
| 查询 ID 不存在，且所属域已确认加载完成 | 返回 `NOT_FOUND`，并记录调用方、ID、kind、域。 |
| 查询目标版本与当前注册表 Schema 不兼容 | 返回 `VERSION_INCOMPATIBLE`；不得自动降级、自动补字段或静默转换。 |
| 内容缺少必填字段、字段类型错误、`kind` 与 Schema 不匹配 | `ERR_SCHEMA_INVALID`；该内容不得进入可查询集合。 |
| 内容含有运行时字段，例如库存、价格、已解锁、已修复、耐久、关系值 | `ERR_RUNTIME_FIELD_IN_STATIC_DATA`；拒绝注册。 |
| 必需引用无法解析 | `ERR_MISSING_REFERENCE`；引用者不能进入 `Active`。 |
| 引用目标处于 `Draft` | `ERR_REFERENCE_TO_DRAFT`；禁止 Active 内容依赖 Draft 内容。 |
| 引用目标处于 `Deprecated` | 允许兼容读取，但新 `Active` 内容不得新增此引用；新增时返回 `ERR_REFERENCE_TO_DEPRECATED`。 |
| 引用目标处于 `Retired` | 仅允许迁移/旧存档解析；正常内容加载返回 `ERR_REFERENCE_TO_RETIRED`。 |
| 引用形成自循环或闭环依赖 | `ERR_REFERENCE_CYCLE`；报告完整循环链路，阻止相关内容进入 `Active`。 |
| `sort_order` 缺失、非整数或小于 0 | `ERR_INVALID_SORT_KEY`；列表查询不得退回文件顺序。 |
| 多项内容 `sort_order` 相同 | 合法；必须使用 `id ASC` 作为稳定 tie-breaker。 |
| ID 含大写、空白、非法字符、路径分隔符或 Unicode 归一化冲突 | `ERR_INVALID_ID_FORMAT` 或 `ERR_ID_NORMALIZATION_COLLISION`；拒绝注册。 |
| 查询请求使用显示名、翻译文本、文件路径、数组下标或资源路径作为目标 | `ERR_UNSTABLE_IDENTIFIER`；调用方必须改用稳定 ID。 |
| 查询条件不足导致多个候选内容匹配 | 返回 `AMBIGUOUS_QUERY`；不得猜测第一个结果。 |
| 浏览器缓存或 Godot 导入产物指向旧注册表版本 | 启动校验失败，返回 `ERR_CONTENT_PACKAGE_VERSION`；要求刷新或重拉内容包。 |
| 局部加载中父内容已加载、引用目标未加载 | 父内容可查询，但引用解析返回 `UNLOADED_REFERENCE`；不得自动加载任意路径。 |
| 同一静态事实在两个系统各自定义 | 标记为 `ERR_DUPLICATE_FACT_SOURCE`；必须指定唯一 owning system。 |
| 查询接口被运行时代码尝试写入或修改定义 | `ERR_READONLY_REGISTRY`；拒绝操作并记录调用方。 |

## Dependencies

`内容数据与状态注册表` 是 Foundation 层系统。根据 `systems-index.md`，它没有上游设计依赖；它的主要责任是向下游系统提供稳定 ID、Schema、标签、生命周期状态和只读查询结果。所有下游接口均为只读接口，不允许下游系统写入或修改注册表定义。

### Upstream Dependencies

| System | Type | Interface | Notes |
|---|---|---|---|
| None | Hard | None | 本系统是静态内容契约层，不依赖其他玩法系统才能定义内容身份。 |

实现阶段可能需要平台加载能力，但这属于后续架构/ADR，不在本 GDD 中把 `平台与会话壳` 设为设计依赖。

### Direct Downstream Dependents

| System | Type | Registry Provides | Dependent Owns |
|---|---|---|---|
| `本地存档与世界状态持久化` | Hard | 可序列化的稳定 ID、Schema 版本、兼容/退役状态 | 存档数据、迁移、序列化、反序列化 |
| `资源、货物与容量` | Hard | 资源 ID、货物 ID、单位、分类、标签、静态处理类别 | 当前数量、容量占用、提取结果、消耗结果 |
| `玩家知识与情报` | Hard | 情报 ID、来源标签、关联内容 ID、展示层级 | 已知/未知、传闻真假、风险揭示状态 |
| `飞艇家园 Hub` | Hard | 飞艇家园空间 ID、生活锚点 ID、模块 ID、空间功能标签、交互锚点标签 | 当前安装、空间状态、玩家可交互状态、居住事件状态 |
| `航图与航线规划` | Hard | 航线 ID、地点 ID、风险标签、环境标签、排序键 | 航线可见性、已探索状态、路线选择、筛选状态 |

### Indirect Downstream Dependents

| System | Type | Registry Relationship |
|---|---|---|
| `飞艇模块与船体状态` | Hard via `飞艇家园 Hub` / `资源、货物与容量` | 消费模块 ID、槽位类型、兼容标签。 |
| `航行与路线风险` | Hard via `航图与航线规划` | 消费航线、威胁、风险标签和 EncounterContext 所需静态引用。 |
| `探索 / 搜撤场景` | Hard via `资源、货物与容量` / `航行与路线风险` | 消费资源、货物、地点、威胁、探索点静态引用。 |
| `战斗与威胁处理` | Hard via `探索 / 搜撤场景` | 消费威胁 ID、威胁类型、对抗标签。 |
| `世界修复与解锁` | Hard via `资源、货物与容量` / `玩家知识与情报` / `航图与航线规划` | 消费修复节点、地点、资源、情报、航线引用。 |
| `空港 / 村镇状态与集市交易` | Hard via `世界修复与解锁` / `资源、货物与容量` | 消费地点、摊位商品、商品标签、供给类别。 |
| `伙伴功能与关系` | Soft/Hard mixed | 伙伴 ID 是硬依赖；关系状态和剧情进度不属于注册表。 |
| `UI / HUD / 航图界面` | Soft/Hard mixed | 显示名键、描述键、标签、排序键是硬依赖；当前 UI 状态不属于注册表。 |
| `反馈、特效与音频语义` | Soft | 可使用内容标签选择反馈语义，但不能依赖注册表拥有表现状态。 |
| `新手引导与首轮闭环` | Soft | 可引用稳定内容 ID 作为教学目标，但教学进度不属于注册表。 |

### Interface Contract

- 输入：静态内容定义、Schema、稳定 ID、引用关系、状态字段。
- 输出：只读 canonical definition、查询状态、错误码、确定性排序结果。
- 禁止输出：runtime instance、玩家状态、世界状态、库存状态、价格状态、修复状态、关系状态。
- 禁止输入：下游系统写回的运行时状态或局内结果。

### Provisional Assumptions

当前除 `systems-index.md` 和本 GDD 外，其他系统 GDD 尚未创建。因此上述依赖接口来自系统索引和本 GDD 的边界定义。后续下游 GDD 若要求注册表提供运行时状态，必须视为冲突并回退到对应领域系统拥有。

## Tuning Knobs

本系统没有玩法平衡参数。这里的可调项只面向内容制作、校验严格度、Web 加载表现和后续兼容维护；不得通过这些参数改变玩家库存、价格、修复进度、探索收益或战斗结果。

### Tunable Values

| Knob | Default | Safe Range | Purpose | Too Low / Too Strict | Too High / Too Loose |
|---|---:|---:|---|---|---|
| `supported_schema_versions` | current only | 1-3 active versions | 控制当前构建可接受的内容 Schema 版本 | 旧存档/旧内容包更容易失效 | 兼容路径膨胀，错误更难定位 |
| `max_tags_per_item` | 12 | 4-24 | 限制单个内容定义的标签数量 | 内容检索表达力不足 | 标签泛滥，查询语义变弱 |
| `max_references_per_item` | 16 | 4-32 | 限制单个定义可引用的其他内容数量 | 复杂地点/修复节点难表达 | 引用图过密，循环和加载风险上升 |
| `max_query_result_count` | 200 | 50-500 | 限制一次列表查询返回量，保护 Web 性能 | UI/工具需要分页过多 | 浏览器端列表处理和渲染压力上升 |
| `deprecated_reference_policy` | warn in compatibility, error in new Active content | warn/error only | 控制对 `Deprecated` 内容的引用策略 | 迁移旧内容困难 | 新内容继续依赖旧定义 |
| `content_package_version_policy` | fail on incompatible | fail/warn in editor only | 控制内容包版本不兼容时的处理 | 迭代期更容易被阻断 | 静默兼容会掩盖结构错误 |
| `diagnostic_detail_level` | standard | minimal/standard/verbose | 控制错误报告的信息量 | 内容作者难定位问题 | 日志噪声过多，Web 控制台难读 |

### Non-Tunable Constraints

以下规则不是调参项，不能在后续系统中放宽：

- 稳定 ID 格式。
- Active ID 不可复用。
- 注册表只读。
- 禁止运行时字段进入静态定义。
- 禁止显示名、路径、数组下标、字典顺序作为引用依据。
- 查询必须区分 `UNLOADED`、`NOT_FOUND`、`VERSION_INCOMPATIBLE`、`Deprecated`、`Retired`。
- 列表查询必须使用 `sort_order ASC, id ASC`。

## Visual/Audio Requirements

`内容数据与状态注册表` 不直接产出玩家场景画面、角色资产、特效或音乐。本节约束的是开发期内容工具、调试面板、错误报告页、Web 运行时诊断界面和诊断日志。目标是让内容作者和程序能快速看出：哪个 ID 出错、哪条引用断了、哪个内容包或 Schema 版本导致失败。

### Visual Requirements

- 诊断界面必须以“航图/修补记录”的方式表达内容关系：内容项是节点，引用是连线，错误是断点，内容包是分区。
- 每个内容项至少显示：`id`、`kind`、`status`、`schema_version`、`owner_domain`。
- 每个错误至少显示：错误码、严重度、问题字段、来源文件或内容包、引用链、建议处理方向。
- 引用链必须可视化为路径，例如 `route -> location -> repair-node`，不能只给出孤立错误文本。
- 内容状态视觉语义：
  - `Active`：实线节点、确认标记。
  - `Draft`：虚线节点、草稿标记。
  - `Deprecated`：褪色实线、旧标签。
  - `Retired`：断线或封存章。
  - `UNLOADED`：半透明虚线节点。
  - `VERSION_INCOMPATIBLE`：断裂边框加版本标记。
  - Error：警示锈红加断裂三角或缺口边。
- 状态图标不得只靠颜色区分；必须同时具备形状、线型或边缘特征。
- 错误列表必须支持按错误码、内容类型、内容包、owner domain、严重度排序。
- 高严重度错误必须能在首屏看到，不能淹没在 verbose 日志中。
- Web 调试界面不得依赖大量半透明层、持续粒子、复杂动效或超大图表。
- 诊断界面可以使用项目视觉语言，但不能牺牲密度和可读性；它首先是工具，不是展示页。

### Diagnostic Log Fields

每条注册表诊断事件必须包含以下字段：

| Field | Required | Purpose |
|---|---|---|
| `event_id` | Yes | 单条诊断事件的唯一 ID，便于复制和追踪。 |
| `timestamp` | Yes | 事件发生时间。 |
| `severity` | Yes | `info` / `warning` / `error` / `fatal`。 |
| `error_code` | Yes for warning+ | 例如 `ERR_DUPLICATE_ID`、`ERR_SCHEMA_INVALID`。 |
| `content_id` | Yes when available | 出错内容的稳定 ID。 |
| `kind` | Yes when available | 内容类型。 |
| `status` | Yes when available | 内容生命周期状态。 |
| `schema_version` | Yes when available | 内容 Schema 版本。 |
| `owner_domain` | Yes when available | 内容归属领域。 |
| `content_package` | Yes | 来源内容包、分片或构建批次。 |
| `source_ref` | Yes when available | 来源文件、导入产物或资源引用。 |
| `field_path` | Yes when field-specific | 具体出错字段路径。 |
| `reference_chain` | Yes when reference-related | 触发错误的引用链。 |
| `query_context` | Yes when query-related | 查询调用方、查询参数、作用域。 |
| `blocking_scope` | Yes | `item` / `package` / `registry` / `runtime-query`。 |
| `suggested_action` | Yes | 最短可行动修复建议。 |

### Severity Rules

- `info`：不影响注册和查询，例如工具打开、包扫描完成。
- `warning`：允许兼容读取，但新内容不应继续依赖，例如引用 `Deprecated` 内容。
- `error`：单项内容或内容包不能进入可查询集合。
- `fatal`：整个注册表不可用，或当前构建无法继续启动内容层。

### Copyable Report Format

诊断界面必须支持复制单条错误为纯文本，格式至少包含：

```text
[severity] error_code
content_id:
kind:
owner_domain:
schema_version:
content_package:
source_ref:
field_path:
reference_chain:
blocking_scope:
suggested_action:
```

多条错误复制格式必须保留稳定排序和摘要信息：

```text
Registry Diagnostic Summary
content_package:
generated_at:
filters:
total_errors:
total_warnings:

| severity | error_code | content_id | kind | field_path | blocking_scope | suggested_action |
|---|---|---|---|---|---|---|
| error | ERR_MISSING_REFERENCE | route.sky-reef-arc-01 | route | destination_id | item | Add or load referenced location. |
```

批量复制必须使用当前错误列表的排序与筛选结果；每行只放最短可行动信息，完整引用链仍通过单条错误复制格式提供。若同一内容项有 `related_errors`，摘要行使用主错误，单条详情必须列出 `related_errors`。

### Audio Requirements

- 默认不播放任何音频。
- 开发工具若启用音频提示，必须可关闭。
- 允许的提示音只有三类：
  - 校验通过：短促确认音。
  - 警告：低侵入标记音。
  - 阻断错误：清晰但不刺耳的失败提示音。
- 禁止循环提示音、持续警报音、战斗化警报音。
- Web 浏览器自动播放限制下，不得依赖音频作为唯一反馈。

### Diagnostic Presentation Contract

- 每个诊断结果必须能回答：
  - 哪个内容项出错？
  - 错误属于 ID、Schema、引用、状态、版本、排序还是只读性问题？
  - 这个错误阻止单项内容、整批内容包，还是只影响兼容读取？
  - 下游哪个系统可能被影响？
- verbose 模式可以展开完整引用链和原始字段；standard 模式只显示最短可行动信息。
- 玩家正式流程中不显示内部错误码，除非进入开发/调试构建。
- 诊断日志不得包含玩家个人数据、存档完整内容或运行时库存/进度快照；本系统只记录静态内容问题。

## UI Requirements

`内容数据与状态注册表` 的 UI 只面向开发期、内容制作期和调试构建。正式玩家流程不应暴露内部注册表界面；玩家只会通过航图、集市、修复、资源、飞艇模块等下游系统间接看到内容定义结果。

### Required Screens / Panels

| UI Surface | Purpose | Required Elements |
|---|---|---|
| Registry Overview | 查看当前内容包和注册表健康状态 | 内容包列表、Schema 版本、总条目数、错误/警告计数、fatal 状态 |
| Content Item Inspector | 查看单个内容定义 | `id`、`kind`、`status`、`owner_domain`、字段表、引用列表、被引用列表 |
| Reference Graph | 诊断引用关系 | 节点、连线、循环标记、未加载引用、缺失引用、Deprecated/Retired 引用 |
| Error List | 批量处理内容错误 | 错误码、严重度、内容 ID、字段路径、阻断范围、建议动作 |
| Query Tester | 验证只读查询契约 | 查询输入、作用域、返回状态、结果列表、排序结果、错误码 |
| Copyable Report Panel | 提交问题或协作排查 | 单条错误复制、多条错误摘要复制、纯文本格式预览 |

### Interaction Requirements

- 所有列表必须支持键鼠操作：点击选择、滚轮滚动、文本复制、基础搜索。
- 诊断工具必须支持纯键盘操作；Tab 顺序为全局筛选、Registry Overview、Error List、Content Item Inspector、Reference Graph、Query Tester、Copyable Report Panel。
- 当前焦点必须有可见 focus ring，不能只依赖颜色变化；焦点状态不得被图表、滚动容器或自定义控件吞掉。
- `Error List` 必须支持方向键移动、Enter 打开详情、Ctrl+C 复制当前错误、Shift+Ctrl+C 复制当前筛选后的错误摘要。
- `Content Item Inspector` 必须支持键盘展开/折叠字段组，并允许复制 `id`、字段路径和引用 ID。
- `Reference Graph` 必须提供键盘遍历模式：方向键或 Tab 在错误链路节点间移动，Enter 打开节点 Inspector，Esc 返回 Error List。
- 所有面板必须定义 loading、empty、error、no-selection 和 partial-domain 状态；这些状态必须使用文字加图标/线型表达，不能只靠颜色。
- 错误列表必须支持按 `severity`、`error_code`、`kind`、`owner_domain`、`content_package` 过滤。
- 点击错误项必须能跳转到对应内容项和字段路径。
- 点击引用链中的节点必须能打开该内容项 Inspector。
- `Reference Graph` 必须提供“只看错误链路”模式，避免大图淹没关键问题。
- `Query Tester` 只能执行只读查询，不能修改内容定义。
- 所有内部错误码必须可复制，不能只显示为图标或颜色。
- UI 不得把 `UNLOADED`、`NOT_FOUND`、`VERSION_INCOMPATIBLE` 混成同一种“失败”。

### Layout Requirements

- 优先使用高密度工具布局：左侧列表，中部详情，右侧诊断/引用。
- 不使用营销式大卡片、装饰 hero 或大面积插画。
- 错误和警告区域必须在首屏可见。
- 所有长 ID、字段路径和引用链必须支持换行或横向滚动，不得截断到无法复制。
- 24px 以下状态图标必须配合文字标签或 tooltip。
- Web 调试界面必须适配常见桌面浏览器窗口，最小目标宽度为 1280px；低于该宽度时允许折叠右侧诊断栏。

### Player-Facing Boundary

- 正式玩家 UI 不显示 Registry Overview、Query Tester 或内部引用图。
- 正式玩家 UI 可以显示内容定义派生出的名称、描述、图标、标签和排序结果。
- 正式玩家 UI 不显示 `ERR_*` 内部错误码；若内容包损坏，只显示面向玩家的安全错误提示。
- 玩家界面不得提供任何写入或修改注册表定义的入口。

## Acceptance Criteria

- **GIVEN** registry 中存在某个稳定 ID 的唯一 `Active` 定义，**WHEN** 通过该稳定 ID 查询，**THEN** 只返回一份 canonical definition，且内容与静态目录定义一致。
- **GIVEN** 同一稳定 ID 出现两份定义，**WHEN** 运行注册表校验，**THEN** 必须返回 `ERR_DUPLICATE_ID`，不能任意选一份通过。
- **GIVEN** 内容定义满足唯一 ID、`kind/schema` 匹配、必填字段齐全且不含运行时字段，**WHEN** 计算 `definition_validity`，**THEN** 结果为 true。
- **GIVEN** 内容定义缺少任一 `definition_validity` 条件，**WHEN** 运行校验，**THEN** 结果为 false，并指出具体失败项。
- **GIVEN** `飞艇家园 Hub` 需要引用舱室、驻点或生活性交互对象，**WHEN** 这些内容进入注册表，**THEN** 必须使用 `home-space` 或 `home-anchor` 稳定 ID，不得用场景路径、显示文本或 UI 文案替代。
- **GIVEN** `location` 内容进入注册表，**WHEN** 运行 Schema 校验，**THEN** 必须验证 `region_tag`、地方身份和村镇需求相关字段完整，不能只用宽泛 `tags` 兜底。
- **GIVEN** `repair-node` 或 `stall-good` 内容进入注册表，**WHEN** 运行 Schema 校验，**THEN** 必须验证村镇需求和修复可见状态相关字段完整，不能只用宽泛 `tags` 兜底。
- **GIVEN** 所需引用都可解析、引用状态允许且不存在非法自循环，**WHEN** 计算 `reference_validity`，**THEN** 结果为 true。
- **GIVEN** 任一引用缺失、状态非法或形成循环，**WHEN** 运行引用校验，**THEN** 结果为 false，并定位到具体引用链。
- **GIVEN** 同一内容项同时存在多个定义或引用错误，**WHEN** 生成诊断，**THEN** 必须按 Validation Diagnostic Precedence 选择主错误，并把其他错误列入 `related_errors`。
- **GIVEN** 列表查询返回多条内容，**WHEN** 执行查询，**THEN** 结果按 `sort_order ASC, id ASC` 排序，且多次查询顺序一致。
- **GIVEN** 内容经历 `Draft -> Active -> Deprecated -> Retired`，**WHEN** 以稳定 ID 查询或引用它，**THEN** 状态必须被明确识别，不得与 `NOT_FOUND` 混淆。
- **GIVEN** 某个 `Active` ID 已退役，**WHEN** 新内容尝试复用该 ID，**THEN** 校验必须失败。
- **GIVEN** ID 不符合格式规则或归一化后发生碰撞，**WHEN** 注册表校验，**THEN** 必须返回 ID 格式或归一化冲突错误。
- **GIVEN** 目标已加载且存在，**WHEN** 查询，**THEN** 返回定义本体。
- **GIVEN** 目标所属域未加载，**WHEN** 查询，**THEN** 返回 `UNLOADED`，不得返回 `NOT_FOUND`。
- **GIVEN** 目标不存在且所属域已加载完成，**WHEN** 查询，**THEN** 返回 `NOT_FOUND`。
- **GIVEN** 内容包或 Schema 版本不兼容，**WHEN** 查询，**THEN** 返回 `VERSION_INCOMPATIBLE`。
- **GIVEN** 查询条件无法唯一命中，**WHEN** 查询，**THEN** 返回 `AMBIGUOUS_QUERY`。
- **GIVEN** 定义混入库存、价格、解锁、修复、耐久、关系等运行时字段，**WHEN** 运行定义校验，**THEN** 返回 `ERR_RUNTIME_FIELD_IN_STATIC_DATA`。
- **GIVEN** 下游系统尝试写回玩家态、世界态、库存态或解锁态，**WHEN** 调用注册表接口，**THEN** 操作必须被拒绝，且不产生任何状态变更。
- **GIVEN** 正常只读查询，**WHEN** 注册表返回内容，**THEN** 只能返回静态内容定义，不返回可写句柄或 runtime instance。
- **GIVEN** registry 只加载部分内容域，**WHEN** 查询已加载域的内容，**THEN** 不需要等待未加载域完成即可返回结果。
- **GIVEN** registry 只加载部分内容域，**WHEN** 查询未加载域的内容，**THEN** 返回 `UNLOADED`，且不得触发任意文件系统扫描。
- **GIVEN** 玩家打开航图与航线规划界面，**WHEN** `routes`、`world`、`intel` 或相关 `threats` 内容域不是 `COMPLETE`，**THEN** 界面不得展示半完整航线选择，必须显示等待状态或安全错误。
- **GIVEN** 玩家打开飞艇家园或整备界面，**WHEN** `airship`、`resources` 或相关 `companions` 内容域不是 `COMPLETE`，**THEN** 界面不得展示可安装模块、舱室锚点或伙伴功能选择。
- **GIVEN** 玩家打开世界修复或市场界面，**WHEN** `world`、`resources` 或相关 `intel` 内容域不是 `COMPLETE`，**THEN** 界面不得展示可执行修复、摊位商品或材料用途。
- **GIVEN** 玩家决策界面已经基于一组内容定义打开，**WHEN** 异步加载、缓存刷新或内容包重载发生，**THEN** 当前界面的可选项、排序和引用结果不得变化，除非界面关闭并用新的完整 snapshot 重建。
- **GIVEN** 内容定义使用 `owner_domain`、`kind`、`region_tag`、`settlement_need_tags`、`repair_visible_state_tags`、`home_function_tags`、`hazard_tags`、`severity_tier`、`supply_class` 或 `presentation_tier`，**WHEN** 运行 Schema 校验，**THEN** 字段值必须来自受控词表，未知值必须返回可诊断错误。
- **GIVEN** 某个 fantasy-critical ID 已进入 `Active`，**WHEN** 后续内容包试图把该 ID 改义为另一个地点、房间、伙伴、修复目标或航线，**THEN** 校验必须返回 ID 改义或复用错误。
- **GIVEN** `home-space`、`home-anchor` 或 `companion` 的运行时状态发生升级、模块替换或关系进展，**WHEN** 保存或恢复该状态，**THEN** 状态变化必须引用原稳定 ID，不能通过替换静态 ID 表达。
- **GIVEN** 旧存档引用 `Deprecated` 或 `Retired` ID，**WHEN** 注册表解析该 ID，**THEN** 必须返回生命周期状态和迁移提示；具体存档迁移由 `本地存档与世界状态持久化` 执行。
- **GIVEN** 查询结果超过 `max_query_result_count`，**WHEN** 执行列表查询，**THEN** 必须返回受控分页、截断或明确错误，不得一次性无界返回。
- **GIVEN** Web 调试界面打开 Registry Overview，**WHEN** registry 存在 `fatal` 或 `error` 诊断，**THEN** 高严重度问题必须在首屏可见。
- **GIVEN** Web 调试界面打开 Reference Graph，**WHEN** 引用图较大，**THEN** 必须提供“只看错误链路”模式，避免整图阻塞排查。
- **GIVEN** 内容包版本不兼容，**WHEN** Web 构建启动内容层，**THEN** 必须失败在内容层边界并给出可复制诊断，不得进入半可用状态。
- **GIVEN** registry 存在任一错误，**WHEN** 打开开发期诊断工具，**THEN** 必须能看到 Registry Overview、Content Item Inspector、Reference Graph、Error List、Query Tester。
- **GIVEN** 某条错误显示在诊断 UI 中，**WHEN** 查看或复制错误，**THEN** 必须包含 severity、error_code、content_id、source_ref、blocking_scope 和 suggested_action。
- **GIVEN** 错误列表存在多条错误，**WHEN** 使用批量复制，**THEN** 必须输出 Registry Diagnostic Summary，并按当前筛选和排序包含 severity、error_code、content_id、kind、field_path、blocking_scope 和 suggested_action。
- **GIVEN** 用户不使用鼠标操作开发期诊断工具，**WHEN** 通过键盘导航，**THEN** 必须能访问筛选、错误列表、Inspector、Reference Graph、Query Tester 和复制面板，并能看到当前焦点。
- **GIVEN** 正式玩家 UI 打开相关内容页面，**WHEN** 页面渲染，**THEN** 不得显示内部注册表、`ERR_*`、诊断代码或开发期调试字段。

## Open Questions

当前没有阻塞本 GDD 进入后续设计评审的问题。以下问题不改变本系统边界，留待架构、ADR 或下游系统 GDD 决定：

- 静态内容最终采用 Godot Resource、JSON、YAML、CSV 或混合格式，由架构阶段决定。
- 内容包的构建、导入、校验和热更新流程，由后续内容管线/平台架构决定。
- 注册表查询接口的具体 GDScript API 命名，由架构文档和控制清单决定。
- 开发期诊断工具是否内置在 Godot 调试构建中，或作为独立 Web 工具提供，由工具链设计决定。
- 旧存档迁移表的具体格式，由 `本地存档与世界状态持久化` GDD 决定。
