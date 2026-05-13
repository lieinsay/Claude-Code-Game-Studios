using CloudWeaverVoyage.Core;

internal static class ModuleHullTestSupport
{
    public static int Failed { get; private set; }

    public static int Total { get; private set; }

    public static void Run(string label, Func<bool> test)
    {
        Total++;
        try
        {
            if (test())
            {
                Console.WriteLine($"[PASS] {label}");
                return;
            }
        }
        catch (Exception ex)
        {
            Failed++;
            Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        Failed++;
        Console.Error.WriteLine($"[FAIL] {label}");
    }

    public static int Finish(string name)
    {
        if (Failed > 0)
        {
            Console.Error.WriteLine($"{name} validation failed: {Failed}/{Total} checks failed.");
            return 1;
        }

        Console.WriteLine($"{name} validation passed: {Total}/{Total} checks passed.");
        return 0;
    }

    public static (Registry Registry, ResourcesManager Resources) MakeResources(int basic = 50, int repair = 50)
    {
        var registry = new Registry();
        registry.InitializeContent();
        var resources = new ResourcesManager(registry);
        resources.Initialize();
        if (basic > 0)
        {
            resources.Add(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId, basic);
        }

        if (repair > 0)
        {
            resources.Add(ResourcePool.InStorage, ModuleHullManager.RepairKitId, repair);
        }

        return (registry, resources);
    }

    public static ModuleHullManager MakeManager(int basic = 50, int repair = 50)
    {
        return new ModuleHullManager(MakeResources(basic, repair).Resources);
    }

    public static void RegisterCargo(Registry registry, string id, string linkedResourceId = ModuleHullManager.BasicSupplyId, string massClass = "medium")
    {
        var key = id.Replace('.', '_').Replace('-', '_');
        registry.RegisterContent(id, new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = id,
            ["kind"] = "cargo",
            ["owner_domain"] = "resources",
            ["status"] = "Active",
            ["name_key"] = $"content.{key}.name",
            ["description_key"] = $"content.{key}.desc",
            ["schema_version"] = 1,
            ["tags"] = new[] { "test" },
            ["sort_order"] = 999,
            ["references"] = Array.Empty<string>(),
            ["linked_resource_id"] = linkedResourceId,
            ["mass_class"] = massClass,
            ["handling_class"] = "crate",
            ["stack_rule"] = "unique",
            ["max_stack"] = 1,
        });
    }

    public static string EventLog(IReadOnlyList<string> events)
    {
        return string.Join(">", events);
    }
}
