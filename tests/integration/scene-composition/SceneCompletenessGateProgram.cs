Console.WriteLine("=== Epic #19 Story 002: Scene Completeness Gate and Evidence Contract ===");

var failed = 0;
var total = 0;

var repoRoot = FindRepoRoot();
var gate = Read("production/scene-specs/scene-completeness-gate.md");
var template = Read("production/scene-specs/scene-spec-template.md");
var registry = Read("production/scene-specs/scene-coverage-registry.md");
var smoke = Read("tests/smoke/session_shell_visual_probe.gd");

Run("AC-1: gate checks every GDD #19 readiness dimension", test_gate_checks_every_readiness_dimension);
Run("AC-1 edge: any false/pending dimension blocks scene completion", test_any_false_dimension_blocks_completion);
Run("AC-2: smoke evidence covers identity viewport anchors focus and route behavior", test_smoke_evidence_contract_is_declared_and_backed_by_probe);
Run("AC-3: asset gate maps P0 assets to identity interaction state or feedback", test_asset_gate_requires_traceability);
Run("AC-4: unresolved P0 current-scene asset gaps block or record waiver", test_unresolved_p0_gaps_block_or_require_waiver);
Run("AC-5: scene layer cannot create gameplay authority or duplicate persistent state", test_domain_authority_boundary_is_explicit);

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
		"Purpose is vague",
		"#20 contract is missing",
		"Node existence or HUD text is the only spatial proof",
		"primary action exists only as a UI button",
		"Variants are missing",
		"Any P0 current-scene asset gap is unresolved",
		"Scene layer creates new gameplay authority",
		"Smoke only proves node existence",
		"Any Codex blocker remains open",
		"User review is missing",
	];

	return dimensions.All(gate.Contains)
		&& blockerLines.All(gate.Contains)
		&& gate.Contains("Codex review has no blocker", StringComparison.Ordinal)
		&& gate.Contains("User readability review has no blocker", StringComparison.Ordinal);
}

bool test_any_false_dimension_blocks_completion()
{
	return gate.Contains("Any `fail`, `pending`, `tracked-gap`, or missing evidence blocks completion", StringComparison.Ordinal)
		&& gate.Contains("scene_complete =")
		&& gate.Contains("AND scene_physics_ready")
		&& gate.Contains("AND user_review_passed")
		&& registry.Contains("tracked-gap")
		&& gate.Contains("blocked-for-release");
}

bool test_smoke_evidence_contract_is_declared_and_backed_by_probe()
{
	string[] gateRequirements =
	[
		"Visible scene identity nodes",
		"Main viewport coverage",
		"Interaction anchors",
		"Focus isolation",
		"Core route behavior",
		"Physical contract evidence",
		"Node existence alone is insufficient",
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

	return gateRequirements.All(gate.Contains)
		&& smokeProof.All(smoke.Contains);
}

bool test_asset_gate_requires_traceability()
{
	string[] traceTargets = ["identity", "interaction", "state_variant", "feedback"];

	return traceTargets.All(gate.Contains)
		&& gate.Contains("Every P0 current-scene asset row must map", StringComparison.Ordinal)
		&& template.Contains("| P0 |")
		&& template.Contains("Supports identity / interaction / state / feedback", StringComparison.Ordinal);
}

bool test_unresolved_p0_gaps_block_or_require_waiver()
{
	string[] waiverFields = ["waiver owner", "waiver date", "explicit risk accepted", "temporary greybox or fallback evidence"];

	return gate.Contains("Unresolved P0 gaps block release readiness", StringComparison.Ordinal)
		&& waiverFields.All(gate.Contains)
		&& gate.Contains("Greybox can support `greybox` or `asset_gate` lifecycle states", StringComparison.Ordinal)
		&& gate.Contains("cannot by itself make `scene_complete=true`", StringComparison.Ordinal);
}

bool test_domain_authority_boundary_is_explicit()
{
	string[] forbiddenAuthority =
	[
		"create a new gameplay authority",
		"duplicate persistent state",
		"mutate resources, route, repair, market, exploration, feedback, onboarding, save/load, or UI focus state outside the owning domain",
		"infer gameplay collision, passability, or physical behavior from art alone",
	];

	return forbiddenAuthority.All(gate.Contains)
		&& gate.Contains("domain owner for every mutable gameplay consequence", StringComparison.Ordinal)
		&& template.Contains("Domain managers read:")
		&& template.Contains("Domain managers mutated:");
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
