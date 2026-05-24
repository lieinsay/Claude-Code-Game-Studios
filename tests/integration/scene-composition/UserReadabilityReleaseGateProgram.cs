Console.WriteLine("=== Epic #19 Story 004: Post-Implementation Feedback Routing ===");

var failed = 0;
var total = 0;

var repoRoot = FindRepoRoot();
var checklist = Read("production/playtests/scene-composition-user-readability-checklist.md");
var handoff = Read("production/scene-specs/scene-release-gate-handoff.md");
var gate = Read("production/scene-specs/scene-completeness-gate.md");
var registry = Read("production/scene-specs/scene-coverage-registry.md");
var sceneTemplate = Read("production/scene-specs/scene-spec-template.md");
var uiTemplate = Read("production/ui-specs/ui-spec-template.md");
var unitTemplate = Read("production/unit-specs/unit-spec-template.md");

Run("AC-1: creation review remains the only human gate", test_creation_review_is_only_human_gate);
Run("AC-2: post-implementation feedback routes to directed modification", test_feedback_routes_to_directed_modification);
Run("AC-3: release handoff no longer waits for user verdict", test_release_handoff_no_user_verdict);
Run("AC-4: templates do not contain second-review verdict fields", test_templates_drop_second_review_verdicts);
Run("Release snapshot: current scenes are blocked by evidence, not second review", test_current_scene_snapshot_blocks_on_evidence);

if (failed > 0)
{
	Console.Error.WriteLine($"Post-implementation feedback routing validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Post-implementation feedback routing validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
	total++;
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
		failed++;
		Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
		return;
	}

	failed++;
	Console.Error.WriteLine($"[FAIL] {label}");
}

bool test_creation_review_is_only_human_gate()
{
	return ContainsText(gate, "创建适合性审查回答“是否应该创建这个场景”，是唯一人工前置硬门")
		&& ContainsText(sceneTemplate, "创建适合性是进入规格和实现的唯一人工前置硬门")
		&& ContainsText(uiTemplate, "创建适合性是进入规格和实现的唯一人工前置硬门")
		&& ContainsText(unitTemplate, "创建适合性是进入规格和实现的唯一人工前置硬门");
}

bool test_feedback_routes_to_directed_modification()
{
	string[] requiredDocs = [gate, handoff, registry, sceneTemplate, uiTemplate, unitTemplate];

	return requiredDocs.All(doc => ContainsText(doc, "directed-content-modification"))
		&& ContainsText(gate, "实现后反馈不是二次审核门")
		&& ContainsText(handoff, "用户实现后反馈只记录为后续修改需求")
		&& ContainsText(registry, "实现后反馈不进入登记门禁");
}

bool test_release_handoff_no_user_verdict()
{
	return ContainsText(handoff, "release_handoff_ready =")
		&& !ContainsText(handoff, "user_review_passed")
		&& !ContainsText(handoff, "用户可读性 verdict")
		&& !ContainsText(handoff, "用户审核状态")
		&& ContainsText(handoff, "后续反馈入口");
}

bool test_templates_drop_second_review_verdicts()
{
	string joinedTemplates = string.Join("\n", sceneTemplate, uiTemplate, unitTemplate);

	return !ContainsText(joinedTemplates, "体验验收结论")
		&& !ContainsText(joinedTemplates, "用户可读性审核")
		&& !ContainsText(joinedTemplates, "用户体验验收")
		&& !ContainsText(joinedTemplates, "用户审核")
		&& !ContainsText(joinedTemplates, "PASS_WITH_NOTES")
		&& ContainsText(joinedTemplates, "后续反馈");
}

bool test_current_scene_snapshot_blocks_on_evidence()
{
	string[] scenes =
	[
		"`initial_island_scene`",
		"`ship_interior_layered`",
		"`voyage_open_world_scene`",
		"`mist_lamp_wreck_scene`",
		"`ochre_island_scene`",
	];

	return scenes.All(term => ContainsText(handoff, term))
		&& ContainsText(handoff, "Scene Composition #19: `BLOCKED_FOR_RELEASE`")
		&& ContainsText(registry, "直到自动证据、截图证据、#20 合同、P0 缺口处理")
		&& ContainsText(handoff, "尚无 #20 合同、作者化单位和运行时证据")
		&& !ContainsText(registry, "直到用户可读性审核");
}

string Read(string relativePath)
{
	var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
	return File.ReadAllText(path);
}

static bool ContainsText(string haystack, string needle) =>
	haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

static string FindRepoRoot()
{
	var current = new DirectoryInfo(AppContext.BaseDirectory);
	while (current is not null)
	{
		if (File.Exists(Path.Combine(current.FullName, "project.godot")))
		{
			return current.FullName;
		}

		current = current.Parent;
	}

	throw new DirectoryNotFoundException("Could not find repository root containing project.godot.");
}
