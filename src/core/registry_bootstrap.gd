## RegistryBootstrap — Static content definitions for P3 prototype
##
## Extracted from design/registry/entities.yaml.
## In production, this would be loaded from external data files.
## Placed here as a helper script for Registry initialization.

class_name RegistryBootstrap
extends RefCounted

# Bootstrap all static content into the Registry
static func bootstrap(registry: Registry) -> void:
	_load_locations(registry)
	_load_routes(registry)
	_load_resources(registry)
	_load_repair_nodes(registry)
	_load_threats(registry)
	_load_partners(registry)
	registry.set_domain_loaded(&"core_content")

static func _load_locations(registry: Registry) -> void:
	var locs: Array[Dictionary] = [
		{
			"id": "location.glass-harbor",
			"kind": "location",
			"display_name": "玻璃港",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 1,
			"type": "settlement",
			"description": "起始空港聚落——修复前的第一站",
		},
		{
			"id": "location.glass-harbor-outskirts",
			"kind": "location",
			"display_name": "玻璃港近郊",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 2,
			"type": "outskirts",
			"description": "玻璃港附近郊区——灯塔修复节点所在地",
		},
		{
			"id": "location.sky-reef-outpost",
			"kind": "location",
			"display_name": "空礁前哨",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 3,
			"type": "outpost",
			"description": "安全航线的目的地——小型探索前哨",
		},
		{
			"id": "location.cloudwatch-ruins",
			"kind": "location",
			"display_name": "云观站废墟",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 4,
			"type": "ruins",
			"description": "高风险航线的目的地——探索搜撤场景",
		},
	]
	for loc: Dictionary in locs:
		registry.register_content(loc.id, loc)

static func _load_routes(registry: Registry) -> void:
	var routes: Array[Dictionary] = [
		{
			"id": "route.sky-reef-arc-01",
			"kind": "route",
			"display_name": "空礁航线",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 1,
			"destination_id": "location.sky-reef-outpost",
			"origin_id": "location.glass-harbor",
			"traversable": false,
			"hazard_tags": ["safe"],
			"distance_band": "short",
			"encounter_check_count": 5,
			"required_repair_id": "repair_node.starlight_dock",
		},
		{
			"id": "route.storm-cut-01",
			"kind": "route",
			"display_name": "风暴捷径",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 2,
			"destination_id": "location.cloudwatch-ruins",
			"origin_id": "location.glass-harbor",
			"traversable": true,
			"hazard_tags": ["mist", "low-visibility", "guard"],
			"distance_band": "medium",
			"encounter_check_count": 10,
		},
	]
	for route: Dictionary in routes:
		registry.register_content(route.id, route)

static func _load_resources(registry: Registry) -> void:
	var resources: Array[Dictionary] = [
		{
			"id": "resource.repair_kit",
			"kind": "resource",
			"display_name": "维修套件",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 1,
			"stack_rule": "stackable",
			"max_stack": 99,
			"supply_class": "repair",
		},
		{
			"id": "resource.basic_supply",
			"kind": "resource",
			"display_name": "基础补给品",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 2,
			"stack_rule": "stackable",
			"max_stack": 99,
			"supply_class": "basic",
		},
		{
			"id": "resource.cloud_coin",
			"kind": "resource",
			"display_name": "云海币",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 3,
			"stack_rule": "stackable",
			"max_stack": 9999,
			"supply_class": "basic",
		},
		{
			"id": "resource.ancient_lens",
			"kind": "resource",
			"display_name": "古代透镜",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 4,
			"stack_rule": "unique",
			"max_stack": 1,
			"supply_class": "intel",
			"cat_sniff_signature": "ancient_optics",
		},
		{
			"id": "resource.navigation_chart",
			"kind": "resource",
			"display_name": "旧航海图",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 5,
			"stack_rule": "stackable",
			"max_stack": 20,
			"supply_class": "navigation",
		},
		{
			"id": "resource.beacon_crystal",
			"kind": "resource",
			"display_name": "信标水晶",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 6,
			"stack_rule": "stackable",
			"max_stack": 99,
			"supply_class": "repair",
		},
	]
	for res: Dictionary in resources:
		registry.register_content(res.id, res)

static func _load_repair_nodes(registry: Registry) -> void:
	var nodes: Array[Dictionary] = [
		{
			"id": "repair_node.starlight_dock",
			"kind": "repair_node",
			"display_name": "星光灯塔",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 1,
			"location_id": "location.glass-harbor-outskirts",
			"required_materials": {
				"resource.repair_kit": 4,
				"resource.beacon_crystal": 2,
			},
			"unlocks": {
				"routes": ["route.sky-reef-arc-01"],
				"stalls": ["stall.navigator_supply"],
				"abilities": ["ability.lighthouse_signal"],
			},
		},
	]
	for node: Dictionary in nodes:
		registry.register_content(node.id, node)

static func _load_threats(registry: Registry) -> void:
	var threats: Array[Dictionary] = [
		{
			"id": "threat.guard-sentinel",
			"kind": "threat",
			"display_name": "警戒哨兵",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 1,
			"threat_type": "guard",
			"trigger_radius": 120.0,
			"trigger_probability": 0.70,
			"hull_damage_min": 8,
			"hull_damage_max": 12,
			"module_damage_chance": 0.30,
			"can_retreat": true,
		},
	]
	for threat: Dictionary in threats:
		registry.register_content(threat.id, threat)

static func _load_partners(registry: Registry) -> void:
	var partners: Array[Dictionary] = [
		{
			"id": "partner.sky-cat",
			"kind": "companion",
			"display_name": "航海猫",
			"content_status": registry.ContentStatus.ACTIVE,
			"sort_order": 1,
			"companion_type": "cat",
			"abilities": ["scout_sniff"],
			"max_confidence": 66,
		},
	]
	for partner: Dictionary in partners:
		registry.register_content(partner.id, partner)
