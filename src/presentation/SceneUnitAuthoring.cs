using System.Text.Json;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Loads and validates scene unit prototype and placed-instance authoring data.
/// </summary>
public sealed class SceneUnitAuthoringFixture
{
	private static readonly HashSet<string> AllowedClassifications = new(StringComparer.Ordinal)
	{
		"dynamic_entity",
		"fixed_scene_object",
	};

	/// <summary>Reusable unit prototype definitions keyed by prototype id.</summary>
	public IReadOnlyDictionary<string, SceneUnitPrototype> Prototypes { get; init; } =
		new Dictionary<string, SceneUnitPrototype>(StringComparer.Ordinal);

	/// <summary>Placed scene-unit instances keyed by instance id.</summary>
	public IReadOnlyDictionary<string, SceneUnitInstance> Instances { get; init; } =
		new Dictionary<string, SceneUnitInstance>(StringComparer.Ordinal);

	/// <summary>Loads scene-unit authoring records from the shared playable-slice authored content file.</summary>
	public static SceneUnitAuthoringFixture Load(string path)
	{
		var fullPath = Path.GetFullPath(path);
		if (!File.Exists(fullPath))
		{
			return new SceneUnitAuthoringFixture();
		}

		using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
		var root = document.RootElement;
		var prototypes = ReadPrototypes(root)
			.ToDictionary(prototype => prototype.PrototypeId, StringComparer.Ordinal);
		var instances = ReadInstances(root)
			.ToDictionary(instance => instance.InstanceId, StringComparer.Ordinal);
		return new SceneUnitAuthoringFixture
		{
			Prototypes = prototypes,
			Instances = instances,
		};
	}

	/// <summary>Returns placed instances for one runtime scene id in stable instance-id order.</summary>
	public IReadOnlyList<SceneUnitInstance> InstancesForScene(string sceneId) =>
		Instances.Values
			.Where(instance => string.Equals(instance.SceneId, sceneId, StringComparison.Ordinal))
			.OrderBy(instance => instance.InstanceId, StringComparer.Ordinal)
			.ToArray();

	/// <summary>Validates prototype and placed-instance records for the requested scene.</summary>
	public IReadOnlyList<string> ValidateScene(string sceneId)
	{
		var diagnostics = new List<string>();
		foreach (var prototype in Prototypes.Values)
		{
			ValidatePrototype(prototype, diagnostics);
		}

		var seenUnitIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var instance in InstancesForScene(sceneId))
		{
			ValidateInstance(instance, diagnostics);
			if (!Prototypes.TryGetValue(instance.PrototypeId, out var prototype))
			{
				diagnostics.Add($"instance {instance.InstanceId} references missing prototype {instance.PrototypeId}");
				continue;
			}

			if (prototype.AllowedSceneIds.Count > 0 && !prototype.AllowedSceneIds.Contains(instance.SceneId))
			{
				diagnostics.Add($"prototype {prototype.PrototypeId} is not allowed in scene {instance.SceneId}");
			}

			if (!string.Equals(prototype.SourceLayer, "world_playable_scene", StringComparison.Ordinal))
			{
				diagnostics.Add($"prototype {prototype.PrototypeId} is not world/playable evidence");
			}

			if (!seenUnitIds.Add(instance.UnitId))
			{
				diagnostics.Add($"scene {sceneId} has duplicate placed unit id {instance.UnitId}");
			}
		}

		return diagnostics;
	}

	private static void ValidatePrototype(SceneUnitPrototype prototype, List<string> diagnostics)
	{
		Require(prototype.PrototypeId, $"prototype has stable id", diagnostics);
		Require(prototype.PrototypeClassification, $"prototype {prototype.PrototypeId} has classification", diagnostics);
		if (!AllowedClassifications.Contains(prototype.PrototypeClassification))
		{
			diagnostics.Add($"prototype {prototype.PrototypeId} uses unsupported classification {prototype.PrototypeClassification}");
		}
		Require(prototype.UnitType, $"prototype {prototype.PrototypeId} has unit type", diagnostics);
		Require(prototype.Collision, $"prototype {prototype.PrototypeId} has collision", diagnostics);
		Require(prototype.OcclusionLayer, $"prototype {prototype.PrototypeId} has occlusion layer", diagnostics);
		Require(prototype.ScaleRule, $"prototype {prototype.PrototypeId} has scale rule", diagnostics);
		Require(prototype.SourceLayer, $"prototype {prototype.PrototypeId} has source layer", diagnostics);
		Require(prototype.SourceGdd, $"prototype {prototype.PrototypeId} has source GDD", diagnostics);
		if (!string.Equals(prototype.SourceLayer, "world_playable_scene", StringComparison.Ordinal))
		{
			diagnostics.Add($"prototype {prototype.PrototypeId} source_layer must be world_playable_scene");
		}
		if (prototype.UiEvidenceAllowed)
		{
			diagnostics.Add($"prototype {prototype.PrototypeId} cannot allow UI evidence");
		}
	}

	private static void ValidateInstance(SceneUnitInstance instance, List<string> diagnostics)
	{
		Require(instance.InstanceId, "instance has stable id", diagnostics);
		Require(instance.PrototypeId, $"instance {instance.InstanceId} has prototype reference", diagnostics);
		Require(instance.SceneId, $"instance {instance.InstanceId} has scene id", diagnostics);
		Require(instance.UnitId, $"instance {instance.InstanceId} has unit id", diagnostics);
		Require(instance.GodotNodePath, $"instance {instance.InstanceId} has Godot placement reference", diagnostics);
		Require(instance.FloorId, $"instance {instance.InstanceId} has floor id", diagnostics);
		Require(instance.Layer, $"instance {instance.InstanceId} has layer", diagnostics);
		Require(instance.SceneSpec, $"instance {instance.InstanceId} has scene spec traceability", diagnostics);
	}

	private static void Require(string value, string message, List<string> diagnostics)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			diagnostics.Add(message);
		}
	}

	private static IReadOnlyList<SceneUnitPrototype> ReadPrototypes(JsonElement root)
	{
		if (!root.TryGetProperty("scene_unit_prototypes", out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return Array.Empty<SceneUnitPrototype>();
		}

		return array.EnumerateArray()
			.Select(item => new SceneUnitPrototype(
				ReadString(item, "prototype_id"),
				ReadString(item, "display_name"),
				ReadString(item, "prototype_classification"),
				ReadString(item, "unit_type"),
				ReadString(item, "collision"),
				ReadString(item, "occlusion_layer"),
				ReadString(item, "scale_rule"),
				ReadString(item, "source_layer"),
				ReadBool(item, "ui_evidence_allowed"),
				ReadString(item, "source_gdd"),
				ReadStringArray(item, "allowed_scene_ids").ToHashSet(StringComparer.Ordinal),
				ReadStringArray(item, "behavior_tags"),
				ReadString(item, "domain_owner")))
			.Where(prototype => !string.IsNullOrWhiteSpace(prototype.PrototypeId))
			.ToArray();
	}

	private static IReadOnlyList<SceneUnitInstance> ReadInstances(JsonElement root)
	{
		if (!root.TryGetProperty("scene_unit_instances", out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return Array.Empty<SceneUnitInstance>();
		}

		return array.EnumerateArray()
			.Select(item => new SceneUnitInstance(
				ReadString(item, "instance_id"),
				ReadString(item, "prototype_id"),
				ReadString(item, "scene_id"),
				ReadString(item, "unit_id"),
				ReadString(item, "godot_node_path"),
				ReadFloat(item, "position", "x"),
				ReadFloat(item, "position", "y"),
				ReadString(item, "floor_id"),
				ReadInt(item, "floor_index"),
				ReadString(item, "layer"),
				ReadString(item, "scene_spec"),
				ReadString(item, "state_hook"),
				ReadString(item, "interaction_anchor_id")))
			.Where(instance => !string.IsNullOrWhiteSpace(instance.InstanceId))
			.ToArray();
	}

	private static string ReadString(JsonElement item, string propertyName) =>
		item.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
			? property.GetString() ?? string.Empty
			: string.Empty;

	private static bool ReadBool(JsonElement item, string propertyName) =>
		item.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True;

	private static int ReadInt(JsonElement item, string propertyName) =>
		item.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
			? value
			: 0;

	private static float ReadFloat(JsonElement item, string objectName, string propertyName)
	{
		if (!item.TryGetProperty(objectName, out var nested)
			|| nested.ValueKind != JsonValueKind.Object
			|| !nested.TryGetProperty(propertyName, out var property)
			|| !property.TryGetSingle(out var value))
		{
			return 0.0f;
		}

		return value;
	}

	private static IReadOnlyList<string> ReadStringArray(JsonElement item, string propertyName)
	{
		if (!item.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return Array.Empty<string>();
		}

		return array.EnumerateArray()
			.Select(value => value.GetString() ?? string.Empty)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToArray();
	}
}

/// <summary>Reusable scene-unit definition shared by placed instances.</summary>
public sealed record SceneUnitPrototype(
	string PrototypeId,
	string DisplayName,
	string PrototypeClassification,
	string UnitType,
	string Collision,
	string OcclusionLayer,
	string ScaleRule,
	string SourceLayer,
	bool UiEvidenceAllowed,
	string SourceGdd,
	IReadOnlySet<string> AllowedSceneIds,
	IReadOnlyList<string> BehaviorTags,
	string DomainOwner);

/// <summary>Scene-specific placement of a reusable scene-unit prototype.</summary>
public sealed record SceneUnitInstance(
	string InstanceId,
	string PrototypeId,
	string SceneId,
	string UnitId,
	string GodotNodePath,
	float X,
	float Y,
	string FloorId,
	int FloorIndex,
	string Layer,
	string SceneSpec,
	string StateHook,
	string InteractionAnchorId);
