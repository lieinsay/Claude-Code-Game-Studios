Console.WriteLine("=== Epic #19 Story 003: Scene Versus UI Evidence Boundary ===");

var failed = 0;
var total = 0;

var repoRoot = FindRepoRoot();
var boundary = Read("production/scene-specs/scene-vs-ui-evidence-boundary.md");
var gate = Read("production/scene-specs/scene-completeness-gate.md");
var template = Read("production/scene-specs/scene-spec-template.md");
var registry = Read("production/scene-specs/scene-coverage-registry.md");
var smoke = Read("tests/smoke/session_shell_visual_probe.gd");
var uiFocusTest = Read("tests/integration/ui-hud-interface/EdgeCasesDesktopA11yProgram.cs");

Run("AC-1: UI/HUD cannot dominate or hide world identity", test_ui_dominance_gate_is_explicit_and_backed_by_smoke);
Run("AC-2: UI evidence cannot count as scene unit identity anchor or physics proof", test_ui_evidence_is_rejected_for_scene_proof);
Run("AC-3: UI-only evidence packages fail scene readiness", test_ui_only_evidence_fails_readiness);
Run("AC-4: modal and semi-modal focus isolate world input without deleting scene evidence", test_focus_isolation_preserves_world_evidence);
Run("Regression: Chart table is classified as UI-assisted world surface, not standalone physics proof", test_chart_table_classification_is_explicit);

if (failed > 0)
{
	Console.Error.WriteLine($"Scene versus UI boundary validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Scene versus UI boundary validation passed: {total}/{total} checks passed.");
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

bool test_ui_dominance_gate_is_explicit_and_backed_by_smoke()
{
	string[] boundaryTerms =
	[
		"hud_not_dominant = true",
		"primary_scene_viewport_share",
		"Target 65%",
		"below 55%",
		"world_identity_visible_with_hud",
		"core_anchor_visible_with_hud",
	];
	string[] smokeProof =
	[
		"_control_area(session, \"HubPlayableSkyBackdrop\") > 500000.0",
		"Hub scene occupies the main viewport instead of a text-only strip",
		"_control_area(session, \"ExplorationPlayableSkyBackdrop\") > 500000.0",
		"Exploration scene occupies the main viewport instead of only HUD text",
		"HubPlayableSkyBackdrop",
		"ExplorationPlayableIslandBody",
	];

	return boundaryTerms.All(term => ContainsText(boundary, term))
		&& smokeProof.All(term => ContainsText(smoke, term))
		&& ContainsText(gate, "`hud_not_dominant = true` must be recorded")
		&& ContainsText(gate, "`primary_scene_viewport_share` targets 65%");
}

bool test_ui_evidence_is_rejected_for_scene_proof()
{
	string[] uiSurfaces =
	[
		"HUD labels",
		"status panels",
		"buttons",
		"menus",
		"modal panels",
		"save/load/delete controls",
		"onboarding hint text",
		"debug labels",
		"debug overlays",
	];
	string[] forbiddenUses =
	[
		"Physical scene units",
		"scene identity nodes",
		"interaction anchors",
		"physical contract proof",
		"viewport identity",
		"human readability replacement",
	];
	string[] runtimeRejection =
	[
		"physical_unit_source_layer",
		"refuses UI-only scene unit evidence",
		"cannot be satisfied by UI",
		"ui_overlay: not physical evidence",
	];

	return uiSurfaces.All(term => ContainsText(gate, term))
		&& forbiddenUses.All(term => ContainsText(boundary, term))
		&& runtimeRejection.All(term => ContainsText(smoke, term))
		&& ContainsText(template, "not UI/HUD/buttons/labels/debug overlays")
		&& ContainsText(registry, "cannot be counted as scene units or physical acceptance evidence");
}

bool test_ui_only_evidence_fails_readiness()
{
	string[] rejectionCases =
	[
		"`ui_only_surface`",
		"`debug_overlay_only`",
		"`button_only_interaction`",
		"`ui_physics_contract`",
		"`scene_readiness = fail`",
	];

	return rejectionCases.All(term => ContainsText(boundary, term))
		&& ContainsText(boundary, "no world/playable scene nodes")
		&& ContainsText(boundary, "no visible scene identity node")
		&& ContainsText(boundary, "no helm/table/wreck/return-ship/repair/stall/NPC anchor")
		&& ContainsText(gate, "A UI-only evidence package fails readiness");
}

bool test_focus_isolation_preserves_world_evidence()
{
	string[] boundaryFocus =
	[
		"UIManager owns UI focus",
		"World movement/use input is blocked or isolated",
		"leave the focus chain",
		"underlying world/playable scene evidence must remain mounted",
		"cannot be used to delete, hide, or replace the scene evidence",
	];
	string[] smokeFocus =
	[
		"Chart entry leaves focus chain while Chart panel is open",
		"Save entry leaves focus chain while Chart panel is open",
		"Load entry leaves focus chain while Chart panel is open",
		"Chart mode has a visible chart table scene",
	];
	string[] uiRegression =
	[
		"large resume delta forces full UI refresh from domain",
		"disabled button remains focusable but Enter no-ops",
		"no-focus modal traps focus on container",
		"destroyed prior focus falls back to current screen",
	];

	return boundaryFocus.All(term => ContainsText(boundary, term))
		&& smokeFocus.All(term => ContainsText(smoke, term))
		&& uiRegression.All(term => ContainsText(uiFocusTest, term));
}

bool test_chart_table_classification_is_explicit()
{
	return ContainsText(boundary, "`chart_table_scene`")
		&& ContainsText(boundary, "Authored chart table surface anchored inside ship interior")
		&& ContainsText(boundary, "Chart buttons/route UI do not count")
		&& ContainsText(gate, "`blocked-ui-assisted-surface`")
		&& ContainsText(registry, "authored UI-assisted surface anchored inside `hub_ship_interior`")
		&& ContainsText(registry, "Do not count Chart buttons/route UI as scene evidence");
}

static bool ContainsText(string haystack, string needle) =>
	haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

string Read(string relativePath)
{
	var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
	return File.ReadAllText(path);
}

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
