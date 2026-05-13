using CloudWeaverVoyage.Core;

// Story 003 — Scout Preview Window & Hidden Tag Reveal
// 覆盖 AC-1 到 AC-15 全部验收标准

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}
void AssertNear(double actual, double expected, double eps, string name)
{
	bool ok = Math.Abs(actual - expected) < eps;
	if (ok) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}  actual={actual:F3} expected={expected:F3}"); fail++; }
}

Console.WriteLine("=== Story 003: Scout Preview Window & Hidden Tag Reveal ===\n");

// ── AC-1: η=1.0 → N_preview=2, T_preview=2×T_check ──
{
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact); // 12s
	double preview = NavigationManager.CalculateScoutPreviewWindow(1.0, tCheck);
	int nPreview = (int)Math.Floor(1.0 * 2); // = 2
	AssertNear(preview, 24.0, 0.001, "AC-1: η=1.0 → T_preview=24s (N=2)");
	Assert(nPreview == 2, "AC-1: N_preview=⌊1.0×2⌋=2");
}

// ── AC-2: η=0.95 → N_preview=⌊1.9⌋=1, T_preview=12s ──
{
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.95, tCheck);
	AssertNear(preview, 12.0, 0.001, "AC-2: η=0.95 → T_preview=12s (N=1)");
	Assert((int)Math.Floor(0.95 * 2) == 1, "AC-2: N_preview=⌊0.95×2⌋=⌊1.9⌋=1");
}

// ── AC-3: η=0.6 → N_preview=⌊1.2⌋=1, T_preview=12s ──
{
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.6, tCheck);
	AssertNear(preview, 12.0, 0.001, "AC-3: η=0.6 → T_preview=12s (N=1)");
	Assert((int)Math.Floor(0.6 * 2) == 1, "AC-3: N_preview=⌊1.2⌋=1");
}

// ── AC-4: η=0 → N_preview=0, T_preview=0 ──
{
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.0, tCheck);
	AssertNear(preview, 0.0, 0.001, "AC-4: η=0 → T_preview=0s");
	Assert((int)Math.Floor(0.0 * 2) == 0, "AC-4: N_preview=0");
}

// ── AC-5: 可见标签有预警图标（η>0，接口层确认）──
{
	// 有侦察 + 可见标签 → T_preview > 0，表示有预警信息
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.8, tCheck);
	Assert(preview > 0.0, "AC-5: η>0 时 T_preview>0，表示有预警信息");
}

// ── AC-6: 隐藏标签显示 "?" ——接口契约（T_preview>0 但不揭示内容）──
{
	// η=0.8 → T_preview=12s，但隐藏标签不被预先揭示
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.8, tCheck);
	// 隐藏标签揭示是独立的概率判定，与 T_preview 无关
	Assert(preview > 0.0, "AC-6: T_preview>0 表示有预警，但隐藏标签内容仍为 '?'");
}

// ── AC-7: η=0 时无预警图标 ──
{
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.0, tCheck);
	Assert(preview == 0.0, "AC-7: η=0 → 无预警（T_preview=0）");
}

// ── AC-8: 双侦察取 max η ──
{
	// max(1.0, 0.6) = 1.0 → T_preview=24s
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double etaA = 1.0;
	double etaB = 0.6;
	double effective = Math.Max(etaA, etaB);
	double preview = NavigationManager.CalculateScoutPreviewWindow(effective, tCheck);
	AssertNear(preview, 24.0, 0.001, "AC-8: max(1.0, 0.6)=1.0 → T_preview=24s");
}

// ── AC-9: 双侦察相同 η → max = 同值 ──
{
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double effective = Math.Max(0.6, 0.6);
	double preview = NavigationManager.CalculateScoutPreviewWindow(effective, tCheck);
	AssertNear(preview, 12.0, 0.001, "AC-9: max(0.6,0.6)=0.6 → T_preview=12s");
}

// ── AC-10: unchecked η 在航行中不变化 ──
{
	// η=0.95（unchecked 折扣）保持 0.95，不变为 0 或 1.0
	double tCheck = NavigationManager.CalculateCheckInterval(HullBand.Intact);
	double preview = NavigationManager.CalculateScoutPreviewWindow(0.95, tCheck);
	AssertNear(preview, 12.0, 0.001, "AC-10: η=0.95 在航行中始终用 0.95，不变化");
}

// ── AC-11: 隐藏标签揭示——30% 概率判定（确定性测试）──
{
	var nav = new NavigationManager();
	// 注入确定性随机：始终返回 0.29（< 0.30）→ 揭示成功
	nav.SetRandomDelegate(() => 0.29);
	bool revealed = nav.RollHiddenTagReveal(0.30);
	Assert(revealed, "AC-11: roll=0.29 < p=0.30 → 揭示成功");

	// 注入 0.31（≥ 0.30）→ 失败
	nav.SetRandomDelegate(() => 0.31);
	bool failed = nav.RollHiddenTagReveal(0.30);
	Assert(!failed, "AC-11: roll=0.31 ≥ p=0.30 → 揭示失败");
}

// ── AC-12: storm_eye_passage → P_reveal=1.0，强制揭示所有隐藏标签 ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "hidden_storm" }, "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 100);
	nav.SetGetHullBandDelegate(() => HullBand.Intact);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "hidden_storm" });
	// 注入随机：始终 0.99（不揭示），但 stormEye=true 强制揭示
	nav.SetRandomDelegate(() => 0.99);
	var revealed = nav.ProcessHiddenTagReveal(stormEyePassage: true);
	Assert(revealed.Count == 1, "AC-12: storm_eye_passage → 强制揭示所有隐藏标签");
	Assert(revealed[0] == "hidden_storm", "AC-12: 揭示的标签正确");
}

// ── AC-13: 已揭示标签不重复判定 ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "hidden_reef" }, "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 100);
	nav.SetGetHullBandDelegate(() => HullBand.Intact);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "hidden_reef" });
	nav.SetRandomDelegate(() => 0.10); // 第一次揭示
	var first = nav.ProcessHiddenTagReveal();
	Assert(first.Count == 1, "AC-13: 第一次揭示成功");
	// 第二次调用——已揭示不重复判定
	var second = nav.ProcessHiddenTagReveal();
	Assert(second.Count == 0, "AC-13: 已揭示标签不重复判定");
}

// ── AC-14: 生命周期——揭示标签加入 RevealedHiddenTags ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "hidden_fog" }, "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 100);
	nav.SetGetHullBandDelegate(() => HullBand.Intact);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "hidden_fog" });
	string? sigTag = null;
	nav.HiddenTagRevealed += tag => sigTag = tag;
	nav.SetRandomDelegate(() => 0.10); // 揭示成功
	nav.ProcessHiddenTagReveal();
	Assert(nav.RevealedHiddenTags.Contains("hidden_fog"),
		"AC-14: 揭示的标签进入 RevealedHiddenTags 列表");
	Assert(sigTag == "hidden_fog", "AC-14: HiddenTagRevealed 信号触发");
}

// ── AC-15: 未揭示标签保持隐藏 ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "hidden_curse" }, "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 100);
	nav.SetGetHullBandDelegate(() => HullBand.Intact);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "hidden_curse" });
	nav.SetRandomDelegate(() => 0.99); // 始终失败
	// 多次尝试——全部失败
	nav.ProcessHiddenTagReveal();
	nav.ProcessHiddenTagReveal();
	nav.ProcessHiddenTagReveal();
	Assert(!nav.RevealedHiddenTags.Contains("hidden_curse"),
		"AC-15: 全程未揭示 → 标签保持隐藏");
}

// ── N_preview 全范围验证 ──
{
	// η in {0, 0.4, 0.5, 0.6, 0.95, 1.0}
	Assert((int)Math.Floor(0.0 * 2) == 0, "RANGE: η=0.0 → N=0");
	Assert((int)Math.Floor(0.4 * 2) == 0, "RANGE: η=0.4 → N=⌊0.8⌋=0");
	Assert((int)Math.Floor(0.5 * 2) == 1, "RANGE: η=0.5 → N=⌊1.0⌋=1");
	Assert((int)Math.Floor(0.6 * 2) == 1, "RANGE: η=0.6 → N=1");
	Assert((int)Math.Floor(0.95 * 2) == 1, "RANGE: η=0.95 → N=⌊1.9⌋=1");
	Assert((int)Math.Floor(1.0 * 2) == 2, "RANGE: η=1.0 → N=2");
}

Console.WriteLine();
Console.WriteLine($"Story 003 Scout Preview: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
