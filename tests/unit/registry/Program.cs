using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 002: Registry Schema Validation — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: complete valid definition passes", Ac1CompleteValidDefinitionPasses);
Run("AC-2: validation reports U/K/R/S failure terms", Ac2ValidationReportsValidityTerms);
Run("AC-3: runtime field contamination is rejected", Ac3RuntimeFieldContaminationDetected);
Run("AC-4: controlled vocabularies are enforced", Ac4ControlledVocabularyEnforced);
Run("AC-5: location requires dedicated region/local/settlement fields", Ac5LocationRequiresDedicatedFields);
Run("AC-6: repair-node and stall-good require settlement and visible-state tags", Ac6RepairAndStallSchemasRequireTags);
Run("AC-7: registry rejects runtime write attempts without mutation", Ac7ReadonlyWriteRejectedWithoutMutation);
Run("AC-8: read queries return static deep copies", Ac8QueriesReturnStaticDeepCopies);
Run("Batch: invalid schema does not enter queryable collection", RegisterBatchRejectsInvalidSchemaAtomically);
Run("Direct register: invalid schema does not enter queryable collection", DirectRegisterRejectsInvalidSchema);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 002 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 002 AC validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
    total++;
    if (test())
    {
        Console.WriteLine($"[PASS] {label}");
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

static bool Ac1CompleteValidDefinitionPasses()
{
    var registry = new Registry();
    var definition = ValidResource("resource.iron-ore");
    var validation = registry.ValidateDefinition(definition);
    var registration = registry.RegisterBatch([definition]);

    return validation.Valid
        && validation.HasUniqueId
        && validation.MatchesKindSchema
        && validation.RequiredFieldsPresent
        && validation.HasNoRuntimeFields
        && validation.Diagnostics.Count == 0
        && registration.Success;
}

static bool Ac2ValidationReportsValidityTerms()
{
    var registry = new Registry();
    registry.RegisterContent("resource.existing", ValidResource("resource.existing"));

    var invalid = ValidResource("resource.existing");
    invalid["kind"] = "cargo";
    invalid.Remove("linked_resource_id");
    invalid.Remove("unit");
    invalid["durability"] = 10;

    var validation = registry.ValidateDefinition(invalid);
    var terms = validation.Diagnostics
        .Select(diagnostic => Convert.ToString(diagnostic.Details["validity_term"]))
        .ToHashSet(StringComparer.Ordinal);

    return !validation.Valid
        && !validation.HasUniqueId
        && !validation.MatchesKindSchema
        && !validation.RequiredFieldsPresent
        && !validation.HasNoRuntimeFields
        && terms.SetEquals(["U", "K", "R", "S"]);
}

static bool Ac3RuntimeFieldContaminationDetected()
{
    var registry = new Registry();
    var contaminated = ValidResource("resource.current-quantity-test");
    contaminated["storage"] = new Dictionary<string, object?>
    {
        ["current_quantity"] = 5,
    };

    var validation = registry.ValidateDefinition(contaminated);
    var batch = registry.RegisterBatch([contaminated]);

    return !validation.Valid
        && !validation.HasNoRuntimeFields
        && validation.Diagnostics.Any(diagnostic =>
            diagnostic.ErrorCode == "ERR_RUNTIME_FIELD_IN_STATIC_DATA"
            && diagnostic.Field == "storage.current_quantity")
        && !batch.Success
        && batch.ErrorCode == "ERR_RUNTIME_FIELD_IN_STATIC_DATA";
}

static bool Ac4ControlledVocabularyEnforced()
{
    var registry = new Registry();
    var invalid = ValidResource("resource.bad-domain");
    invalid["owner_domain"] = "gameplay";

    var validation = registry.ValidateDefinition(invalid);
    var diagnostic = validation.Diagnostics.SingleOrDefault(diagnostic =>
        diagnostic.ErrorCode == "ERR_SCHEMA_INVALID"
        && diagnostic.Field == "owner_domain");

    return !validation.Valid
        && !validation.MatchesKindSchema
        && diagnostic is not null
        && ((string[])diagnostic.Details["allowed_values"]!).SequenceEqual([
            "resources", "airship", "world", "routes", "intel", "companions", "threats",
        ]);
}

static bool Ac5LocationRequiresDedicatedFields()
{
    var registry = new Registry();
    var broadTagsOnly = ValidLocation("location.glass-harbor");
    broadTagsOnly.Remove("region_tag");
    broadTagsOnly.Remove("local_identity_tags");
    broadTagsOnly.Remove("settlement_need_tags");
    broadTagsOnly["tags"] = new[] { "starter-sea", "glass-buoys", "navigation-aid" };

    var validation = registry.ValidateDefinition(broadTagsOnly);
    var missingFields = validation.Diagnostics
        .Where(diagnostic => diagnostic.ErrorCode == "ERR_SCHEMA_MISSING_REQUIRED_FIELD")
        .Select(diagnostic => diagnostic.Field)
        .ToHashSet(StringComparer.Ordinal);

    return !validation.Valid
        && !validation.RequiredFieldsPresent
        && missingFields.SetEquals(["region_tag", "local_identity_tags", "settlement_need_tags"]);
}

static bool Ac6RepairAndStallSchemasRequireTags()
{
    var registry = new Registry();

    var repairNode = ValidRepairNode("repair-node.starlight-dock");
    repairNode.Remove("settlement_need_tags");
    repairNode.Remove("repair_visible_state_tags");

    var stallGood = ValidStallGood("stall-good.fresh-rations");
    stallGood.Remove("settlement_need_tags");
    stallGood.Remove("repair_visible_state_tags");

    var repairValidation = registry.ValidateDefinition(repairNode);
    var stallValidation = registry.ValidateDefinition(stallGood);

    return MissingRequiredFields(repairValidation).SetEquals(["settlement_need_tags", "repair_visible_state_tags"])
        && MissingRequiredFields(stallValidation).SetEquals(["settlement_need_tags", "repair_visible_state_tags"]);
}

static bool Ac7ReadonlyWriteRejectedWithoutMutation()
{
    var registry = new Registry();
    registry.RegisterContent("resource.iron-ore", ValidResource("resource.iron-ore"));
    registry.InitializeContent();

    var before = registry.QueryById("resource.iron-ore");
    var write = registry.SetEntity("resource.iron-ore", new Dictionary<string, object?>
    {
        ["id"] = "resource.iron-ore",
        ["kind"] = "resource",
        ["name_key"] = "content.resource.changed.name",
    });
    var after = registry.QueryById("resource.iron-ore");

    return !write.Success
        && write.ErrorCode == "ERR_READONLY_REGISTRY"
        && before.Entity is not null
        && after.Entity is not null
        && Convert.ToString(before.Entity["name_key"]) == Convert.ToString(after.Entity["name_key"])
        && Convert.ToString(after.Entity["name_key"]) == "content.resource_iron_ore.name";
}

static bool Ac8QueriesReturnStaticDeepCopies()
{
    var registry = new Registry();
    registry.RegisterContent("resource.iron-ore", ValidResource("resource.iron-ore"));
    registry.InitializeContent();

    var first = registry.QueryById("resource.iron-ore").Entity;
    if (first is null || first["cat_sniff_signature"] is not Dictionary<string, object?> signature)
    {
        return false;
    }

    signature["confidence"] = 0;
    signature["unlocked"] = true;

    var second = registry.QueryById("resource.iron-ore").Entity;
    return second is not null
        && second["cat_sniff_signature"] is Dictionary<string, object?> secondSignature
        && Convert.ToInt32(secondSignature["confidence"]) == 65
        && !secondSignature.ContainsKey("unlocked");
}

static bool RegisterBatchRejectsInvalidSchemaAtomically()
{
    var registry = new Registry();
    var invalid = ValidLocation("location.invalid-vocab");
    invalid["region_tag"] = "unknown-region";

    var result = registry.RegisterBatch([invalid]);
    registry.InitializeContent();
    var query = registry.QueryById("location.invalid-vocab");

    return !result.Success
        && result.ErrorCode == "ERR_SCHEMA_INVALID"
        && query.Status == RegistryQueryStatus.NotFound;
}

static bool DirectRegisterRejectsInvalidSchema()
{
    var registry = new Registry();
    var invalid = ValidLocation("location.direct-invalid");
    invalid["region_tag"] = "unknown-region";

    try
    {
        registry.RegisterContent("location.direct-invalid", invalid);
        return false;
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("ERR_SCHEMA_INVALID", StringComparison.Ordinal))
    {
    }

    registry.InitializeContent();
    var query = registry.QueryById("location.direct-invalid");
    return query.Status == RegistryQueryStatus.NotFound;
}

static HashSet<string> MissingRequiredFields(RegistryDefinitionValidationResult validation)
{
    return validation.Diagnostics
        .Where(diagnostic => diagnostic.ErrorCode == "ERR_SCHEMA_MISSING_REQUIRED_FIELD")
        .Select(diagnostic => diagnostic.Field)
        .ToHashSet(StringComparer.Ordinal);
}

static Dictionary<string, object?> ValidResource(string id)
{
    var definition = BaseDefinition(id, "resource", "resources");
    definition["unit"] = "chunk";
    definition["stack_rule"] = "stackable";
    definition["material_tags"] = new[] { "metal", "repair-material" };
    definition["cat_sniff_signature"] = new Dictionary<string, object?>
    {
        ["reveal_target"] = "location.glass-harbor",
        ["hazard_hint"] = "old-harbor-chain",
        ["confidence"] = 65,
        ["pattern_id"] = "pattern.ancient-optics",
    };
    return definition;
}

static Dictionary<string, object?> ValidLocation(string id)
{
    var definition = BaseDefinition(id, "location", "world");
    definition["region_tag"] = "starter-sea";
    definition["location_kind"] = "harbor";
    definition["service_tags"] = new[] { "market", "repair" };
    definition["local_identity_tags"] = new[] { "glass-buoys" };
    definition["settlement_need_tags"] = new[] { "navigation-aid", "trade-link" };
    return definition;
}

static Dictionary<string, object?> ValidRepairNode(string id)
{
    var definition = BaseDefinition(id, "repair-node", "world");
    definition["location_id"] = "location.glass-harbor";
    definition["node_kind"] = "beacon";
    definition["restoration_theme"] = "lighthouse";
    definition["settlement_need_tags"] = new[] { "navigation-aid", "safety" };
    definition["repair_visible_state_tags"] = new[] { "dark", "lit", "connected" };
    return definition;
}

static Dictionary<string, object?> ValidStallGood(string id)
{
    var definition = BaseDefinition(id, "stall-good", "world");
    definition["commodity_tags"] = new[] { "food" };
    definition["vendor_tags"] = new[] { "harbor-stall" };
    definition["supply_class"] = "basic";
    definition["local_identity_tags"] = new[] { "glass-harbor" };
    definition["settlement_need_tags"] = new[] { "food" };
    definition["repair_visible_state_tags"] = new[] { "stock-improved" };
    return definition;
}

static Dictionary<string, object?> BaseDefinition(string id, string kind, string ownerDomain)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = kind,
        ["owner_domain"] = ownerDomain,
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 10,
        ["references"] = Array.Empty<string>(),
    };
}
