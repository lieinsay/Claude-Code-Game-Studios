Console.WriteLine("=== Epic #19 Story 002: Scene Completeness Gate and Evidence Contract ===");

var failed = 0;
var total = 0;

var repoRoot = FindRepoRoot();
var gate = Read("production/scene-specs/scene-completeness-gate.md");
var template = Read("production/scene-specs/scene-spec-template.md");
var registry = Read("production/scene-specs/scene-coverage-registry.md");
var smoke = Read("tests/smoke/session_shell_visual_probe.gd");
var storyReadiness = Read(".claude/skills/story-readiness/SKILL.md");
var devStory = Read(".claude/skills/dev-story/SKILL.md");

Run("AC-1: gate checks every GDD #19 readiness dimension", test_gate_checks_every_readiness_dimension);
Run("AC-1 edge: any false/pending dimension blocks scene completion", test_any_false_dimension_blocks_completion);
Run("AC-2: smoke evidence covers identity viewport anchors focus and route behavior", test_smoke_evidence_contract_is_declared_and_backed_by_probe);
Run("AC-3: asset gate maps P0 assets to identity interaction state or feedback", test_asset_gate_requires_traceability);
Run("AC-4: unresolved P0 current-scene asset gaps block or record waiver", test_unresolved_p0_gaps_block_or_require_waiver);
Run("AC-5: scene layer cannot create gameplay authority or duplicate persistent state", test_domain_authority_boundary_is_explicit);
Run("Regression: implementation workflows enforce production specs before work starts", test_workflows_enforce_specs_before_implementation);

if (failed > 0)
{
	Console.Error.WriteLine($"Scene completeness gate validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Scene completeness gate validation passed: {total}/{total} checks passed.");
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

bool test_gate_checks_every_readiness_dimension()
{
	string[] dimensions =
	[
		"purpose_ready",
		"scene_physics_ready",
		"space_ready",
		"behavior_ready",
		"state_ready",
		"presentation_ready",
		"technical_ready",
		"qa_ready",
		"codex_review_passed",
		"user_review_passed",
	];
	string[] blockerLines =
	[
		"目的含糊",
		"#20 合同缺失",
		"只有节点存在或 HUD 文本作为空间证明",
		"主要动作只存在于 UI 按钮",
		"变体缺失",
		"当前场景存在未解决 P0 资产缺口",
		"场景层创建新玩法权威",
		"smoke 只证明节点存在",
		"任一 Codex blocker 未解决",
		"用户审核缺失",
	];

	return dimensions.All(gate.Contains)
		&& blockerLines.All(gate.Contains)
		&& gate.Contains("Codex 对目的、空间、行为、状态、表现、技术、QA 线无 blocker", StringComparison.Ordinal)
		&& gate.Contains("用户可读性审核无 blocker", StringComparison.Ordinal);
}

bool test_any_false_dimension_blocks_completion()
{
	return gate.Contains("任何 `fail`、`pending`、`tracked-gap` 或缺失证据都会阻塞完成", StringComparison.Ordinal)
		&& gate.Contains("scene_complete =")
		&& gate.Contains("AND scene_physics_ready")
		&& gate.Contains("AND user_review_passed")
		&& registry.Contains("tracked-gap")
		&& registry.Contains("BLOCKED_FOR_RELEASE");
}

bool test_smoke_evidence_contract_is_declared_and_backed_by_probe()
{
	string[] gateRequirements =
	[
		"可见场景身份节点",
		"主视口覆盖",
		"交互锚点",
		"焦点隔离",
		"核心路线行为",
		"物理合同证据",
		"节点存在本身不足",
	];
	string[] smokeProof =
	[
		"HubPlayableSkyBackdrop",
		"HubDockedShipHullSilhouette",
		"HubBoardingRamp",
		"HubInteriorCockpitBay",
		"ChartTableSurface",
		"ExplorationPlayableIslandBody",
		"SearchWreckProp",
		"ReturnBeaconProp",
		"_control_area",
		"_button_focus_mode",
		"DebugCurrentScenePhysicsContract",
		"OnDepartPressed",
	];

	return gateRequirements.All(term => gate.Contains(term, StringComparison.Ordinal))
		&& smokeProof.All(term => smoke.Contains(term, StringComparison.Ordinal));
}

bool test_asset_gate_requires_traceability()
{
	string[] traceTargets = ["identity", "interaction", "state_variant", "feedback"];

	return traceTargets.All(gate.Contains)
		&& gate.Contains("每个当前场景的 P0 资产行必须映射", StringComparison.Ordinal)
		&& template.Contains("| P0 |")
		&& template.Contains("P0 资产 / 音频需求可追溯到身份、交互、状态或反馈", StringComparison.Ordinal);
}

bool test_unresolved_p0_gaps_block_or_require_waiver()
{
	string[] waiverFields = ["waiver owner", "waiver date", "explicit risk accepted", "temporary greybox or fallback evidence"];

	return gate.Contains("未解决的 P0 缺口会阻塞 release readiness", StringComparison.Ordinal)
		&& waiverFields.All(gate.Contains)
		&& gate.Contains("灰盒可以支撑 `greybox` 或 `asset_gate` 生命周期状态", StringComparison.Ordinal)
		&& gate.Contains("不能让 release readiness 的 `scene_complete=true`", StringComparison.Ordinal);
}

bool test_domain_authority_boundary_is_explicit()
{
	string[] forbiddenAuthority =
	[
		"创建新的玩法权威",
		"复制持久化状态",
		"绕过所属领域去变更资源、航线、修复、市场、探索、反馈、新手引导、保存 / 读取或 UI 焦点状态",
		"仅凭美术推断玩法碰撞、可通行性或物理行为",
	];

	return forbiddenAuthority.All(gate.Contains)
		&& gate.Contains("每个可变玩法后果命名领域负责人", StringComparison.Ordinal)
		&& template.Contains("读取的领域管理器:")
		&& template.Contains("会变更的领域管理器:");
}

bool test_workflows_enforce_specs_before_implementation()
{
	string[] specPaths =
	[
		"production/scene-specs/scene-coverage-registry.md",
		"production/scene-specs/scene-completeness-gate.md",
		"production/scene-specs/scene-vs-ui-evidence-boundary.md",
		"production/ui-specs/README.md",
		"production/unit-specs/README.md",
	];
	string[] readinessGates =
	[
		"Scene specs required before scene work",
		"Unit specs required for reusable world units",
		"UI specs required before UI work",
		"UI cannot satisfy scene/unit readiness",
	];
	string[] devStoryGates =
	[
		"Production scene/UI/unit specifications",
		"stop before coding",
		"README/template references",
		"are not enough for implementation stories",
		"UI/HUD/buttons/labels/menus/modals/debug overlays cannot be used as scene",
	];

	return specPaths.All(path => storyReadiness.Contains(path, StringComparison.Ordinal) && devStory.Contains(path, StringComparison.Ordinal))
		&& readinessGates.All(term => storyReadiness.Contains(term, StringComparison.Ordinal))
		&& devStoryGates.All(term => devStory.Contains(term, StringComparison.Ordinal));
}

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
