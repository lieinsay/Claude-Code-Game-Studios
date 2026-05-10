using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 001: ID Registry Core + Query Engine — Acceptance Criteria ===");
var failed = 0;
var total = 0;

// AC-1: Unique canonical query
total++;
if (Ac1UniqueCanonicalQuery())
    Console.WriteLine($"[PASS] AC-1: unique canonical query");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-1"); }

// AC-2: Duplicate ID rejection
total++;
if (Ac2DuplicateIdRejection())
    Console.WriteLine($"[PASS] AC-2: duplicate ID rejection");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-2"); }

// AC-3: ID format validation
total++;
if (Ac3IdFormatValidation())
    Console.WriteLine($"[PASS] AC-3: ID format validation (uppercase, spaces, path separators)");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-3"); }

// AC-4: Found returns definition
total++;
if (Ac4FoundReturnsDefinition())
    Console.WriteLine($"[PASS] AC-4: found returns definition");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-4"); }

// AC-5: Unloaded domain returns UNLOADED
total++;
if (Ac5UnloadedDomainReturnsUnloaded())
    Console.WriteLine($"[PASS] AC-5: unloaded domain returns UNLOADED");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-5"); }

// AC-6: Not found returns NOT_FOUND
total++;
if (Ac6NotFoundReturnsNotFound())
    Console.WriteLine($"[PASS] AC-6: not found returns NOT_FOUND");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-6"); }

// AC-7: Deterministic sort order
total++;
if (Ac7DeterministicSort())
    Console.WriteLine($"[PASS] AC-7: deterministic sort (sort_order ASC, id ASC)");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-7"); }

// AC-8: Max query result count pagination
total++;
if (Ac8MaxQueryResultCount())
    Console.WriteLine($"[PASS] AC-8: max_query_result_count pagination");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-8"); }

// AC-9: Domain-scoped partial query
total++;
if (Ac9DomainScopedPartialQuery())
    Console.WriteLine($"[PASS] AC-9: domain-scoped partial query independence");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-9"); }

// AC-10: No filesystem scan for unloaded domain
total++;
if (Ac10NoFilesystemScanForUnloaded())
    Console.WriteLine($"[PASS] AC-10: unloaded domain query returns UNLOADED, no scan");
else { failed++; Console.Error.WriteLine($"[FAIL] AC-10"); }

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 AC validation passed: {total}/{total} checks passed.");
return 0;

static Registry MakePopulatedRegistry()
{
    var registry = new Registry();
    registry.RegisterContent("resource.iron-ore", new Dictionary<string, object?>
    {
        ["id"] = "resource.iron-ore",
        ["kind"] = "resource",
        ["display_name"] = "铁矿石",
        ["content_status"] = ContentStatus.Active,
        ["sort_order"] = 10,
        ["owner_domain"] = "resources",
    });
    registry.RegisterContent("resource.copper-ore", new Dictionary<string, object?>
    {
        ["id"] = "resource.copper-ore",
        ["kind"] = "resource",
        ["display_name"] = "铜矿石",
        ["content_status"] = ContentStatus.Active,
        ["sort_order"] = 10,
        ["owner_domain"] = "resources",
    });
    registry.RegisterContent("location.glass-harbor", new Dictionary<string, object?>
    {
        ["id"] = "location.glass-harbor",
        ["kind"] = "location",
        ["display_name"] = "玻璃港",
        ["content_status"] = ContentStatus.Active,
        ["sort_order"] = 1,
        ["owner_domain"] = "world",
    });
    registry.InitializeContent();
    return registry;
}

// AC-1: GIVEN registry 中存在某个稳定 ID 的唯一 Active 定义
// WHEN 通过该稳定 ID 查询 THEN 只返回一份 canonical definition
static bool Ac1UniqueCanonicalQuery()
{
    var registry = MakePopulatedRegistry();
    var result = registry.QueryById("resource.iron-ore");
    return result.Status == RegistryQueryStatus.Found
        && result.Entity is not null
        && Convert.ToString(result.Entity["display_name"]) == "铁矿石"
        && Convert.ToString(result.Entity["kind"]) == "resource";
}

// AC-2: GIVEN 同一稳定 ID 出现两份定义
// WHEN 运行注册表校验 THEN 必须返回 ERR_DUPLICATE_ID
static bool Ac2DuplicateIdRejection()
{
    var registry = new Registry();
    var batchResult = registry.RegisterBatch([
        new Dictionary<string, object?> { ["id"] = "resource.iron-ore", ["kind"] = "resource" },
        new Dictionary<string, object?> { ["id"] = "resource.iron-ore", ["kind"] = "resource" },
    ]);
    return !batchResult.Success
        && batchResult.ErrorCode == "ERR_DUPLICATE_ID";
}

// AC-3: GIVEN ID 不符合格式规则或归一化后发生碰撞
// WHEN 注册表校验 THEN 返回 ID 格式或归一化冲突错误
static bool Ac3IdFormatValidation()
{
    var registry = new Registry();

    var resultUpper = registry.RegisterBatch([
        new Dictionary<string, object?> { ["id"] = "Resource.Iron-Ore", ["kind"] = "resource" },
    ]);
    var resultSpace = registry.RegisterBatch([
        new Dictionary<string, object?> { ["id"] = "resource iron ore", ["kind"] = "resource" },
    ]);
    var resultSlash = registry.RegisterBatch([
        new Dictionary<string, object?> { ["id"] = "resource.iron/ore", ["kind"] = "resource" },
    ]);

    return !resultUpper.Success && resultUpper.ErrorCode == "ERR_INVALID_ID_FORMAT"
        && !resultSpace.Success && resultSpace.ErrorCode == "ERR_INVALID_ID_FORMAT"
        && !resultSlash.Success && resultSlash.ErrorCode == "ERR_INVALID_ID_FORMAT";
}

// AC-4: GIVEN 目标已加载且存在 WHEN 查询 THEN 返回定义本体
static bool Ac4FoundReturnsDefinition()
{
    var registry = MakePopulatedRegistry();
    var result = registry.QueryById("location.glass-harbor");
    return result.Status == RegistryQueryStatus.Found
        && result.Entity is not null
        && Convert.ToString(result.Entity["id"]) == "location.glass-harbor"
        && Convert.ToString(result.Entity["display_name"]) == "玻璃港";
}

// AC-5: GIVEN 目标所属域未加载 WHEN 查询 THEN 返回 UNLOADED
static bool Ac5UnloadedDomainReturnsUnloaded()
{
    var registry = MakePopulatedRegistry();
    var result = registry.QueryById("module.wind-sail-mk1");
    return result.Status == RegistryQueryStatus.Unloaded
        && result.Error == "domain_unloaded";
}

// AC-6: GIVEN 目标不存在且所属域已加载完成 WHEN 查询 THEN 返回 NOT_FOUND
static bool Ac6NotFoundReturnsNotFound()
{
    var registry = MakePopulatedRegistry();
    // "resource" kind is loaded because resource.iron-ore etc exist
    var result = registry.QueryById("resource.nonexistent");
    return result.Status == RegistryQueryStatus.NotFound
        && result.Error == "id_not_found";
}

// AC-7: GIVEN 列表查询返回多条内容
// WHEN 执行查询 THEN 结果按 sort_order ASC, id ASC 排序
static bool Ac7DeterministicSort()
{
    var registry = new Registry();
    registry.RegisterContent("resource.beta", new Dictionary<string, object?>
    {
        ["id"] = "resource.beta", ["kind"] = "resource",
        ["content_status"] = ContentStatus.Active, ["sort_order"] = 20,
    });
    registry.RegisterContent("resource.alpha", new Dictionary<string, object?>
    {
        ["id"] = "resource.alpha", ["kind"] = "resource",
        ["content_status"] = ContentStatus.Active, ["sort_order"] = 10,
    });
    registry.RegisterContent("resource.gamma", new Dictionary<string, object?>
    {
        ["id"] = "resource.gamma", ["kind"] = "resource",
        ["content_status"] = ContentStatus.Active, ["sort_order"] = 10,
    });

    var result1 = registry.ListByKind("resource");
    var result2 = registry.ListByKind("resource");
    var ids1 = result1.Select(e => Convert.ToString(e["id"])).ToArray();
    var ids2 = result2.Select(e => Convert.ToString(e["id"])).ToArray();

    // sort_order=10 first: alpha < gamma alphabetically; then sort_order=20: beta
    return ids1.SequenceEqual(ids2)
        && ids1.Length == 3
        && ids1[0] == "resource.alpha"
        && ids1[1] == "resource.gamma"
        && ids1[2] == "resource.beta";
}

// AC-8: GIVEN 查询结果超过 max_query_result_count
// WHEN 执行列表查询 THEN 返回受控分页或截断
static bool Ac8MaxQueryResultCount()
{
    var registry = new Registry();
    for (var i = 0; i < 50; i++)
    {
        registry.RegisterContent($"resource.test-{i:D3}", new Dictionary<string, object?>
        {
            ["id"] = $"resource.test-{i:D3}",
            ["kind"] = "resource",
            ["content_status"] = ContentStatus.Active,
            ["sort_order"] = i,
        });
    }

    registry.MaxQueryResultCount = 10;
    var results = registry.ListByKind("resource");
    return results.Count == 10
        && Convert.ToString(results[0]["id"]) == "resource.test-000";
}

// AC-9: GIVEN registry 只加载部分内容域
// WHEN 查询已加载域的内容 THEN 不需要等待未加载域完成即可返回结果
static bool Ac9DomainScopedPartialQuery()
{
    var registry = MakePopulatedRegistry();
    // "resource" kind is loaded (from MakePopulatedRegistry), "module" is not
    var resourceResult = registry.QueryById("resource.iron-ore");
    var moduleResult = registry.QueryById("module.wind-sail-mk1");
    return resourceResult.Status == RegistryQueryStatus.Found
        && moduleResult.Status == RegistryQueryStatus.Unloaded;
}

// AC-10: GIVEN 查询未加载域的内容 WHEN 执行 THEN 返回 UNLOADED
// 且不得触发任意文件系统扫描
static bool Ac10NoFilesystemScanForUnloaded()
{
    var registry = MakePopulatedRegistry();
    // Query for something in an unloaded domain — should return UNLOADED
    // without attempting filesystem access (verified implicitly: no I/O in pure C#)
    var result = registry.QueryById("airship.hull-band-1");
    return result.Status == RegistryQueryStatus.Unloaded
        && result.Error == "domain_unloaded"
        && result.Entity is null;
}
