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
		"missing fantasy",
		"missing requirements",
		"unclear identity",
		"undesirable player flow",
	];

	return ContainsText(checklist, "A user review can still block a scene even when Codex review passes")
		&& ContainsText(handoff, "`Codex PASS` is necessary but not sufficient")
		&& blockers.All(term => ContainsText(handoff, term))
		&& ContainsText(gate, "Codex review is necessary but not sufficient")
		&& ContainsText(story, "missing fantasy, missing requirements, unclear identity, or undesirable player flow can still block");
}

bool test_blocked_verdict_prevents_release_handoff()
{
	string[] waiverFields =
	[
		"waiver owner",
		"waiver date",
		"exact blocker waived",
		"accepted player-facing risk",
		"fallback evidence",
		"follow-up owner",
	];

	return ContainsText(checklist, "`BLOCKED` prevents release gate handoff")
		&& ContainsText(handoff, "release_handoff_ready =")
		&& ContainsText(handoff, "user_review_passed OR user_waiver_recorded")
		&& ContainsText(handoff, "no_unresolved_p0_scene_blockers")
		&& waiverFields.All(term => ContainsText(handoff, term))
		&& ContainsText(gate, "prevents release handoff until the blocker is resolved or explicitly waived by the user");
}

bool test_readability_questions_are_concrete()
{
	string[] questions =
	[
		"Where am I?",
		"What can I do here?",
		"How do I leave or continue?",
		"What changed?",
		"Does UI/HUD support rather than dominate?",
		"Does the scene match the intended fantasy?",
	];

	return questions.All(term => ContainsText(checklist, term))
		&& questions.All(term => ContainsText(gate, term))
		&& ContainsText(checklist, "without developer explanation")
		&& ContainsText(checklist, "PASS_WITH_CONDITIONS")
		&& ContainsText(checklist, "BLOCKED");
}

bool test_missing_demands_write_back_to_spec()
{
	return ContainsText(checklist, "new demand is recorded")
		&& ContainsText(handoff, "new demands that must be written back into the scene spec")
		&& ContainsText(story, "any missing demand is added here before status can move")
		&& ContainsText(handoff, "scene spec or equivalent source note");
}

bool test_current_scene_snapshot_blocks_release()
{
	string[] scenes =
	[
		"`hub_island_dock`",
		"`hub_ship_interior`",
		"`chart_table_scene`",
		"`exploration_mist_island`",
		"`repair_node_scene`",
		"`market_scene`",
	];

	return scenes.All(term => ContainsText(checklist, term))
		&& scenes.All(term => ContainsText(handoff, term))
		&& ContainsText(handoff, "Scene Composition #19: `BLOCKED_FOR_RELEASE`")
		&& ContainsText(registry, "`BLOCKED_FOR_RELEASE` until user readability reviews are recorded or explicitly waived")
		&& ContainsText(handoff, "No scene spec or #20 contract yet");
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
