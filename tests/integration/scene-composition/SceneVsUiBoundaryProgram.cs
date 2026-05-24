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
		"目标 65%",
		"低于 55%",
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
		&& ContainsText(gate, "必须记录 `hud_not_dominant = true`")
		&& ContainsText(gate, "`primary_scene_viewport_share` 目标为 65%");
}

bool test_ui_evidence_is_rejected_for_scene_proof()
{
	string[] uiSurfaces =
	[
		"HUD 标签",
		"状态面板",
		"按钮",
		"菜单",
		"模态面板",
		"保存 / 读取 / 删除控件",
		"新手引导提示文本",
		"调试标签",
		"调试覆盖层",
	];
	string[] forbiddenUses =
	[
		"物理场景单位",
		"场景身份节点",
		"交互锚点",
		"物理合同证明",
		"视口身份",
		"人工可读性替代",
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
		&& ContainsText(template, "不是 UI/HUD/按钮/标签/调试覆盖层")
		&& ContainsText(registry, "不能满足场景单位、物理单位或可读性证据");
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
		&& ContainsText(boundary, "没有世界 / 可玩场景节点")
		&& ContainsText(boundary, "没有可见场景身份节点")
		&& ContainsText(boundary, "没有 helm、table、wreck、return ship、repair、stall 或 NPC 锚点")
		&& ContainsText(gate, "UI-only 证据包也会失败");
}

bool test_focus_isolation_preserves_world_evidence()
{
	string[] boundaryFocus =
	[
		"UIManager 拥有 UI 焦点",
		"世界移动 / 使用输入根据当前输入层被阻止或隔离",
		"离开焦点链",
		"底层世界 / 可玩场景证据必须保持挂载和可见",
		"不能被用来删除、隐藏或替代 #19 要求的场景证据",
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
	return ContainsText(boundary, "`voyage_open_world_scene`")
		&& ContainsText(boundary, "不是航线按钮 UI 或进度条")
		&& ContainsText(boundary, "UI / 进度条不能计入")
		&& ContainsText(gate, "`voyage_open_world_scene`")
		&& ContainsText(registry, "航行大场景")
		&& ContainsText(registry, "implementation readiness 前需要 #20 合同");
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
