Console.WriteLine("=== Epic #19 Story 004: User Readability Review and Release Gate Handoff ===");

var failed = 0;
var total = 0;

var repoRoot = FindRepoRoot();
var story = Read("production/epics/scene-composition-system/story-004-user-readability-release-gate.md");
var checklist = Read("production/playtests/scene-composition-user-readability-checklist.md");
var handoff = Read("production/scene-specs/scene-release-gate-handoff.md");
var gate = Read("production/scene-specs/scene-completeness-gate.md");
var registry = Read("production/scene-specs/scene-coverage-registry.md");

Run("AC-1: user review can block even after Codex review passes", test_user_review_can_block_after_codex_pass);
Run("AC-2: blocked Codex or user review prevents release gate unless waived", test_blocked_verdict_prevents_release_handoff);
Run("AC-3: human QA questions force concrete readability answers", test_readability_questions_are_concrete);
Run("AC-4: missing user demands are written back before approval", test_missing_demands_write_back_to_spec);
Run("Release snapshot: current scenes remain blocked until user review or waiver", test_current_scene_snapshot_blocks_release);

if (failed > 0)
{
	Console.Error.WriteLine($"User readability release gate validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"User readability release gate validation passed: {total}/{total} checks passed.");
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

bool test_user_review_can_block_after_codex_pass()
{
	string[] blockers =
	[
		"幻想缺失",
		"需求缺失",
		"身份不清",
		"玩家流程不理想",
	];

	return ContainsText(checklist, "用户 review 仍然可以阻塞该场景")
		&& ContainsText(handoff, "`Codex PASS` 是必要条件，但不足以单独通过")
		&& blockers.All(term => ContainsText(handoff, term))
		&& ContainsText(gate, "Codex 审核是必要条件，但不足以单独通过")
		&& ContainsText(story, "missing fantasy, missing requirements, unclear identity, or undesirable player flow");
}

bool test_blocked_verdict_prevents_release_handoff()
{
	string[] waiverFields =
	[
		"waiver owner",
		"waiver date",
		"被豁免的具体 blocker",
		"接受的玩家可见风险",
		"fallback 证据",
		"follow-up owner",
	];

	return ContainsText(checklist, "`BLOCKED` 会阻止 release gate handoff")
		&& ContainsText(handoff, "release_handoff_ready =")
		&& ContainsText(handoff, "user_review_passed OR user_waiver_recorded")
		&& ContainsText(handoff, "no_unresolved_p0_scene_blockers")
		&& waiverFields.All(term => ContainsText(handoff, term))
		&& ContainsText(gate, "直到 blocker 解决或用户明确 waiver");
}

bool test_readability_questions_are_concrete()
{
	string[] checklistQuestions =
	[
		"我在哪里？",
		"我在这里能做什么？",
		"我如何离开或继续？",
		"什么发生了变化？",
		"UI/HUD 是辅助而不是主导吗？",
		"场景是否符合预期幻想？",
	];
	string[] gateQuestions =
	[
		"我在哪里？",
		"我能在这里做什么？",
		"我如何离开或继续？",
		"发生了什么变化？",
		"UI/HUD 是否只是辅助，而不是主导？",
		"场景是否符合预期幻想？",
	];

	return checklistQuestions.All(term => ContainsText(checklist, term))
		&& gateQuestions.All(term => ContainsText(gate, term))
		&& ContainsText(checklist, "没有开发者解释")
		&& ContainsText(checklist, "PASS_WITH_CONDITIONS")
		&& ContainsText(checklist, "BLOCKED");
}

bool test_missing_demands_write_back_to_spec()
{
	return ContainsText(checklist, "新增诉求都被记录")
		&& ContainsText(handoff, "新需求需要写回场景规格")
		&& ContainsText(story, "any missing demand is added here before status can move")
		&& ContainsText(handoff, "场景规格或等价来源说明");
}

bool test_current_scene_snapshot_blocks_release()
{
	string[] scenes =
	[
		"`initial_island_scene`",
		"`ship_interior_layered`",
		"`voyage_open_world_scene`",
		"`mist_lamp_wreck_scene`",
		"`old_market_edge_scene`",
	];

	return scenes.All(term => ContainsText(checklist, term))
		&& scenes.All(term => ContainsText(handoff, term))
		&& ContainsText(handoff, "Scene Composition #19: `BLOCKED_FOR_RELEASE`")
		&& ContainsText(registry, "`BLOCKED_FOR_RELEASE`，直到用户可读性审核被记录或明确豁免")
		&& ContainsText(handoff, "尚无独立场景规格、#20 合同或用户可读性 verdict");
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
