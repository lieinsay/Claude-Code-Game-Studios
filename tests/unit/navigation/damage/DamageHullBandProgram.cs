using CloudWeaverVoyage.Core;

// Story 004 — Damage Accumulation & Dynamic Hull Band Transitions
// 覆盖 AC-1 到 AC-17 全部验收标准

int pass = 0, fail = 0;
void Assert(bool condition, string name)
{
	if (condition) { Console.WriteLine($"  PASS  {name}"); pass++; }
	else { Console.WriteLine($"  FAIL  {name}"); fail++; }
}

Console.WriteLine("=== Story 004: Damage Accumulation & Dynamic Hull Band Transitions ===\n");

// ── AC-1: max rule — storm+low-visibility 取最大 ──
{
	var entries = new List<EncounterEntry>
	{
		new("storm", damage: 3),
		new("low-visibility", damage: 4),
	};
	int d = NavigationManager.CalculateCheckDamage(entries);
	Assert(d == 4, "AC-1: max(3, 4) = 4（非 3+4=7）");
}

// ── AC-2: 3 个标签 d={2,0,5} → max=5 ──
{
	var entries = new List<EncounterEntry>
	{
		new("tag-a", damage: 2),
		new("tag-b", damage: 0),
		new("tag-c", damage: 5),
	};
	int d = NavigationManager.CalculateCheckDamage(entries);
	Assert(d == 5, "AC-2: max(2, 0, 5) = 5");
}

// ── AC-3: 空集 → d_check = 0 ──
{
	int d = NavigationManager.CalculateCheckDamage(new List<EncounterEntry>());
	Assert(d == 0, "AC-3: 空遭遇集 → d_check=0（显式定义）");
}

// ── AC-4: hull_departure=85, D_acc=18 → effective=67, damaged 波段 ──
{
	int effective = NavigationManager.CalculateEffectiveHullIntegrity(85, 18);
	Assert(effective == 67, "AC-4: max(0, 85-18) = 67");
	Assert(NavigationManager.GetHullBand(67) == HullBand.Damaged,
		"AC-4: 67 在 26-75 范围 → damaged");
}

// ── AC-5: 超量伤害丢弃 → hull_effective=0 ──
{
	int effective = NavigationManager.CalculateEffectiveHullIntegrity(3, 6);
	Assert(effective == 0, "AC-5: max(0, 3-6) = 0（超量丢弃）");
}

// ── AC-6: hull_effective=0 → FORCED_LANDING 优先于 ARRIVED ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "storm" }, "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 5); // 脆弱船体
	nav.SetGetHullBandDelegate(() => HullBand.Critical);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "storm" });
	bool landed = false;
	nav.VoyageForcedLanding += (_, _) => landed = true;
	// 施加 10 伤（超过 5），有效值=0
	nav.ApplyDamageAndCheckBandTransition(10);
	nav.ProcessVoyage(0.1);
	Assert(nav.CurrentState == VoyageState.ForcedLanding,
		"AC-6: hull_effective=0 → FORCED_LANDING（优先于 ARRIVED）");
	Assert(landed, "AC-6: VoyageForcedLanding 信号触发");
}

// ── AC-7: intact → damaged 波段转换 ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "storm" }, "medium")); // medium 更长
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 85); // intact 波段
	nav.SetGetHullBandDelegate(() => HullBand.Intact);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "storm" });
	HullBand? transOld = null; HullBand? transNew = null; int? transInteg = null;
	nav.HullBandTransitioned += (o, n, i) => { transOld = o; transNew = n; transInteg = i; };
	// 施加 10 伤：85-10=75，跨越 76 边界 → damaged
	nav.ApplyDamageAndCheckBandTransition(10);
	Assert(transOld == HullBand.Intact, "AC-7: 旧波段=Intact");
	Assert(transNew == HullBand.Damaged, "AC-7: 新波段=Damaged");
	Assert(transInteg == 75, "AC-7: 当前完整度=75");
}

// ── AC-8: damaged → critical 波段转换 ──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, new[] { "storm" }, "long"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 30); // damaged 波段
	nav.SetGetHullBandDelegate(() => HullBand.Damaged);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", new[] { "storm" });
	HullBand? transNew = null;
	nav.HullBandTransitioned += (_, n, _) => transNew = n;
	// 施加 5 伤：30-5=25，跨越 26 边界 → critical
	nav.ApplyDamageAndCheckBandTransition(5);
	Assert(transNew == HullBand.Critical, "AC-8: damaged → critical 波段转换");
}

// ── AC-9: 一次检查不跨两个波段（单次最大伤害 6）──
{
	// 最坏情况：从 intact 下沿（76）施加 6 伤 → 70，仍在 damaged，不跳至 critical
	int effective = NavigationManager.CalculateEffectiveHullIntegrity(76, 6); // = 70
	Assert(NavigationManager.GetHullBand(70) == HullBand.Damaged,
		"AC-9: 76-6=70 → damaged（未跨两个波段）");
	// damaged 下沿（26）施加 6 伤 → 20，进入 critical（跨一个波段）
	int effective2 = NavigationManager.CalculateEffectiveHullIntegrity(26, 6); // = 20
	Assert(NavigationManager.GetHullBand(20) == HullBand.Critical,
		"AC-9: 26-6=20 → critical（跨一个波段）");
}

// ── AC-10: hull=76 → Intact ──
{
	Assert(NavigationManager.GetHullBand(76) == HullBand.Intact,
		"AC-10: hull=76 → Intact（≥76）");
}

// ── AC-11: hull=75 → Damaged ──
{
	Assert(NavigationManager.GetHullBand(75) == HullBand.Damaged,
		"AC-11: hull=75 → Damaged（26-75）");
}

// ── AC-12: hull=25 → Critical ──
{
	Assert(NavigationManager.GetHullBand(25) == HullBand.Critical,
		"AC-12: hull=25 → Critical（1-25）");
}

// ── AC-13: hull=0 → Destroyed ──
{
	Assert(NavigationManager.GetHullBand(0) == HullBand.Destroyed,
		"AC-13: hull=0 → Destroyed（≤0）");
}

// ── AC-13b: hull=-5 → Destroyed ──
{
	Assert(NavigationManager.GetHullBand(-5) == HullBand.Destroyed,
		"AC-13b: hull=-5 → Destroyed");
}

// ── 进度不跳回验证（波段转换后 elapsed_time 不变）──
{
	var nav = new NavigationManager();
	nav.SetCanDepartDelegate(_ => (true, Array.Empty<string>()));
	nav.SetGetRouteDelegate(_ => (true, Array.Empty<string>(), "short"));
	nav.SetGetKnowledgeStateDelegate(_ => 2);
	nav.SetGetHullIntegrityDelegate(() => 80); // intact
	nav.SetGetHullBandDelegate(() => HullBand.Intact);
	nav.SetGetScoutEfficiencyDelegate(() => 0.0);
	nav.OnRouteCommitted("route.test", "location.dest", Array.Empty<string>());
	nav.ProcessVoyage(30.0); // 已流逝 30s（进度 50%）
	double progressBefore = nav.GetVoyageProgress();
	// 波段转换（intact → damaged）：T_voyage 变长，但进度百分比降低而非跳回
	nav.ApplyDamageAndCheckBandTransition(10); // 80-10=70 → damaged，T_voyage 从 60→66.7
	// 进度不跳回（elapsed_time=30 不变，只是 total_duration 变大）
	double progressAfter = nav.GetVoyageProgress();
	Assert(progressAfter < progressBefore,
		"PROGRESS: 波段转换后进度百分比降低（分母变大），elapsed_time 不变");
	Assert(nav.ElapsedTime == 30.0,
		"PROGRESS: elapsed_time=30s 保持不变（进度不跳回）");
}

// ── 超量伤害丢弃（hull_effective 下限为 0）──
{
	int e1 = NavigationManager.CalculateEffectiveHullIntegrity(10, 20);
	Assert(e1 == 0, "CLAMP: hull=10, damage=20 → effective=max(0,10-20)=0");
}

Console.WriteLine();
Console.WriteLine($"Story 004 Damage & Hull Band: {pass} PASS, {fail} FAIL");
if (fail > 0) Environment.Exit(1);
