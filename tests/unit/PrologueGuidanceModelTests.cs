using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Knight.Abilities;
using Knight.Localization;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;

namespace Knight.Tests.Unit;

public static class PrologueGuidanceModelTests
{
    public static void RunRecyclingTeaching()
    {
        LoadStrings();
        var glyphs=new Dictionary<StringName,string>(Glyphs)
        { [InputActions.Jump]="HOP_BINDING", [InputActions.MoveRight]="BRAKE_BINDING" };
        foreach(GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var context=new PrologueGuidanceContext(PrologueObjective.Wake,PrologueSectorId.CanyonEntrance,
                PrologueBossGuidanceState.Inactive,3,language,true,FirstCombatNearby:true,
                RecyclingLesson:PrologueRecyclingLesson.Combination);
            var card=PrologueGuidanceModel.Evaluate(context,glyphs);
            TestAssert.True(card.Visible && card.ContextTitle.Contains("ECHO") && card.Targets.Count==0 &&
                card.HintText==string.Empty && card.StepText.Contains("HOP_BINDING") &&
                card.StepText.Contains("BRAKE_BINDING") && !card.StepText.Contains('{') &&
                card.StepText.Contains(language==GameLanguage.English?"UP-LEFT":"左上") &&
                card.StepText.Contains(language==GameLanguage.English?"NO SHIELD":"盾无效"),
                "live combination outranks competing early combat, fully explains functional danger and respects bindings without world boxes");
            foreach(var lesson in new[]{PrologueRecyclingLesson.CombinationLaunched,PrologueRecyclingLesson.Springboard,PrologueRecyclingLesson.FixedGear})
            {
                var next=PrologueGuidanceModel.Evaluate(context with{RecyclingLesson=lesson,FirstCombatNearby=false},glyphs);
                TestAssert.True(next.Visible && next.ContextTitle.Contains("ECHO") && next.Targets.Count==0 &&
                    next.HintText==string.Empty && !next.StepText.Contains('{'),"all semantic device cards are complete in both languages");
            }
            var practiceDuringCombat=PrologueGuidanceModel.Evaluate(context with{
                RecyclingLesson=PrologueRecyclingLesson.Springboard},glyphs);
            TestAssert.True(!practiceDuringCombat.ContextTitle.Contains("ECHO") && practiceDuringCombat.StepText.Contains("J") &&
                practiceDuringCombat.StepText.Contains("L"),
                "RECYCLING_PRACTICE_PRIORITY_RED: a nonlethal practice pad must not hide the live enemy attack/shield lesson");
            TestAssert.True(!PrologueGuidanceModel.Evaluate(context with{GuidanceEnabled=false},glyphs).Visible &&
                !PrologueGuidanceModel.Evaluate(context with{Objective=PrologueObjective.Complete},glyphs).Visible,
                "settings and completed flow remain the authority for visibility");
            TestAssert.True(PrologueGuidanceModel.Evaluate(context with{Sector=PrologueSectorId.CanyonBasin},glyphs).ContextTitle==string.Empty &&
                !PrologueGuidanceModel.Evaluate(context with{RecyclingLesson=(PrologueRecyclingLesson)999},glyphs).ContextTitle.Contains("ECHO"),
                "wrong sector and unknown device state cannot invent a local mechanism card");
            var attached=PrologueGuidanceModel.Evaluate(context with{Objective=PrologueObjective.LearnGrappleMode,
                ClawState=MechanicalClawState.Reel},glyphs);
            TestAssert.True(!attached.ContextTitle.Contains("ECHO"),"actual attached-claw controls are not hidden by an old idle device lesson");
        }
        GD.Print("RECYCLING_GUIDANCE_MODEL_OK languages=2 custom_bindings=true semantic_cards=4 danger_priority=true " +
            "no_world_targets=true disabled_complete_wrong_sector=true pixels=false");
    }

    public static void RunThrusterTeaching()
    {
        LoadStrings();
        var glyphs = new Dictionary<StringName, string>(Glyphs)
        {
            [InputActions.Dash] = "JET_CUSTOM", [InputActions.MoveLeft] = "LEFT_CUSTOM",
            [InputActions.MoveRight] = "RIGHT_CUSTOM"
        };
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var context = new PrologueGuidanceContext(PrologueObjective.LearnAttackMode,
                PrologueSectorId.CanyonEntrance, PrologueBossGuidanceState.Inactive, 0, language, true);
            var instruction = PrologueGuidanceModel.Evaluate(context, glyphs);
            TestAssert.True(instruction.Visible && instruction.StepText.Contains("JET_CUSTOM") &&
                instruction.StepText.Contains("LEFT_CUSTOM") && instruction.StepText.Contains("RIGHT_CUSTOM") &&
                instruction.StepText.Contains(language == GameLanguage.English ? "SHORT JET" : "短喷") &&
                !instruction.StepText.Contains('{') && instruction.HintText == string.Empty,
                "first-encounter short-jet and reverse-brake teaching uses live glyphs without requiring failure");
            var combat = PrologueGuidanceModel.Evaluate(context with { FirstCombatNearby = true }, glyphs);
            TestAssert.True(combat.ContextTitle != string.Empty && combat.StepText.Contains("J") &&
                combat.StepText.Contains("L") && !combat.StepText.Contains("JET_CUSTOM"),
                "immediate first-combat danger retains its existing attack/shield teaching priority");

            // Break caught: unused real first-jet opportunity must select its
            // own bounded lesson, not merely repeat the ordinary attack card.
            var opportunity = context with { ThrusterOpportunity = true, SameSectorFailureCount = 3 };
            var jet = PrologueGuidanceModel.Evaluate(opportunity, glyphs);
            string title = language == GameLanguage.English ? "ECHO · THRUSTER" : "ECHO · 推进器";
            TestAssert.True(jet.Visible && jet.ContextTitle == title && jet.Targets.Count == 0 &&
                jet.HintText == string.Empty && jet.StepText.Contains("JET_CUSTOM") &&
                jet.StepText.Contains("LEFT_CUSTOM") && jet.StepText.Contains("RIGHT_CUSTOM") &&
                !jet.StepText.Contains('{'),
                "first unused jet opportunity has its own localized fixed card and live bindings without failure clutter");
            var pendingClaw = PrologueGuidanceModel.Evaluate(opportunity with
                { Objective = PrologueObjective.LearnGrappleMode }, glyphs);
            TestAssert.True(pendingClaw.ContextTitle == string.Empty && pendingClaw.StepText.Contains("K") &&
                !pendingClaw.StepText.Contains("JET_CUSTOM"),
                "mandatory first claw instruction outranks an unused optional short jet");
            var onwardJet = PrologueGuidanceModel.Evaluate(opportunity with
                { Objective = PrologueObjective.ReachBasin }, glyphs);
            TestAssert.True(onwardJet.ContextTitle == title && onwardJet.Targets.Count == 1 &&
                onwardJet.Targets[0] == PrologueGuidanceTargetKey.EntranceExit &&
                onwardJet.StepText.Contains(language == GameLanguage.English ? "DESCENT ENTRANCE" : "下降区入口"),
                "after first claw the optional jet preserves the mandatory forward target and route instruction");
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
            foreach (MechanicalClawState claw in Enum.GetValues<MechanicalClawState>())
            {
                var candidate = PrologueGuidanceModel.Evaluate(opportunity with { Sector = sector, ClawState = claw }, glyphs);
                TestAssert.Equal(sector == PrologueSectorId.CanyonEntrance && claw == MechanicalClawState.Idle,
                    candidate.ContextTitle == title, "first-jet opportunity belongs only to idle entrance traversal");
            }
            foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
            {
                var candidate = PrologueGuidanceModel.Evaluate(opportunity with { Objective = objective }, glyphs);
                bool early = objective is PrologueObjective.Wake or PrologueObjective.LearnAttackMode or
                    PrologueObjective.ReachBasin;
                TestAssert.Equal(early, candidate.ContextTitle == title,
                    "first-jet card never takes ownership of later objectives or completion");
            }
            TestAssert.True(!PrologueGuidanceModel.Evaluate(opportunity with { GuidanceEnabled = false }, glyphs).Visible &&
                PrologueGuidanceModel.Evaluate(opportunity with { ThrusterOpportunity = false }, glyphs).ContextTitle != title &&
                PrologueGuidanceModel.Evaluate(opportunity with { BossState = PrologueBossGuidanceState.Active }, glyphs).ContextTitle != title,
                "disabled guidance, consumed opportunity and active boss revoke the optional jet lesson");
            foreach (PrologueRecyclingLesson lesson in new[] { PrologueRecyclingLesson.Combination,
                PrologueRecyclingLesson.CombinationLaunched, PrologueRecyclingLesson.Springboard, PrologueRecyclingLesson.FixedGear })
                TestAssert.True(PrologueGuidanceModel.Evaluate(opportunity with { RecyclingLesson = lesson }, glyphs).ContextTitle != title,
                    "real entrance mechanism instruction outranks optional first jet");
            TestAssert.True(PrologueGuidanceModel.Evaluate(opportunity with { FirstCombatNearby = true }, glyphs).StepText == combat.StepText &&
                PrologueGuidanceModel.Evaluate(opportunity with { VisibleLaser = Knight.Presentation.Fog.VisibleLaserWarning.Locked }, glyphs).ContextTitle != title,
                "immediate combat and visible locked beam retain danger priority over first jet");
        }
        GD.Print("THRUSTER_TEACHING_GLYPHS_OK initial_objective=true localized=true custom_dash_left_right=true danger_priority_preserved=true");
    }

    public static void RunShieldLaneGuidance()
    {
        LoadStrings();
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var glyphs = new Dictionary<StringName, string>(Glyphs)
            { [InputActions.MoveRight] = "RIGHT_CUSTOM", [InputActions.KeyboardShield] = "GUARD_CUSTOM" };
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
            foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
            foreach (MechanicalClawState claw in Enum.GetValues<MechanicalClawState>())
            foreach (PrologueBossGuidanceState boss in Enum.GetValues<PrologueBossGuidanceState>())
            {
                var context = new PrologueGuidanceContext(objective, sector, boss, 3, language, true,
                    claw, ShieldLaneNearby: true);
                var actual = PrologueGuidanceModel.Evaluate(context, glyphs);
                bool eligible = sector == PrologueSectorId.CanyonBasin &&
                    objective is (PrologueObjective.Wake or PrologueObjective.LearnAttackMode or
                        PrologueObjective.LearnGrappleMode or PrologueObjective.ReachBasin or
                        PrologueObjective.LearnShield or PrologueObjective.ClimbCanyon or PrologueObjective.EnterTemple) &&
                    boss is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
                    claw == MechanicalClawState.Idle;
                if (!eligible)
                {
                    AssertSameGuidance(PrologueGuidanceModel.Evaluate(context with
                        { ShieldLaneNearby = false }, glyphs), actual,
                        "lane context never displaces another sector, Boss, completed objective or active claw");
                    continue;
                }
                TestAssert.True(actual.Visible && actual.Targets.Count == 0 && actual.HintText == "" &&
                    actual.ContextTitle == PrologueStrings.Get("guidance.shield_lane_title", language) &&
                    actual.StepText.Contains("RIGHT_CUSTOM") && actual.StepText.Contains("GUARD_CUSTOM") &&
                    !actual.StepText.Contains('{') && !actual.StepText.Contains("LMB") && !actual.StepText.Contains("RMB"),
                    "current danger uses localized live glyphs, no distant marker or unrelated retry text");
                AssertSameGuidance(PrologueGuidanceInstruction.Hidden,
                    PrologueGuidanceModel.Evaluate(context with { GuidanceEnabled = false }, glyphs),
                    "lane context never reopens disabled first-level guidance");
            }
        }
        Run();
        RunTempleTerrainGuidance();
        RunAscentTransferGuidance();
        GD.Print("SHIELD_LANE_GUIDANCE_MODEL_OK localized=true live_glyphs=true boundary_matrix=true");
    }

    public static void RunAscentTransferGuidance()
    {
        LoadStrings();
        var context = new PrologueGuidanceContext(PrologueObjective.EnterTemple,
            PrologueSectorId.CanyonAscent, PrologueBossGuidanceState.Inactive, 0,
            GameLanguage.English, true, AscentTransferOpportunity: true);
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var eligible = context with { Language = language };
            var remapped = new Dictionary<StringName, string>(Glyphs)
            {
                [InputActions.Jump] = "HOP_CUSTOM", [InputActions.MoveUp] = "UP_CUSTOM",
                [InputActions.MoveRight] = "RIGHT_CUSTOM", [InputActions.Claw] = "CAST_CUSTOM"
            };
            PrologueGuidanceInstruction instruction = PrologueGuidanceModel.Evaluate(eligible, remapped);
            TestAssert.True(instruction.Visible && instruction.StepText.Contains("HOP_CUSTOM") &&
                instruction.StepText.Contains("UP_CUSTOM") && instruction.StepText.Contains("RIGHT_CUSTOM") &&
                instruction.StepText.Contains("CAST_CUSTOM"),
                "AscentTransferGuidance: the supported local transfer teaches an ordinary jump then up-right claw using live bindings");
            TestAssert.True(instruction.StepText.IndexOf("HOP_CUSTOM", StringComparison.Ordinal) <
                instruction.StepText.IndexOf("CAST_CUSTOM", StringComparison.Ordinal) && instruction.Targets.Count == 0,
                "local transfer teaches the jump-before-cast sequence without marking an undiscovered platform");
            TestAssert.True(!instruction.StepText.Contains("Grip1") && !instruction.StepText.Contains("Grip2") &&
                !instruction.StepText.Contains("LMB") && !instruction.StepText.Contains("double jump") &&
                !instruction.StepText.Contains("二段跳") && !instruction.StepText.Contains('{'),
                "local transfer never exposes internal platform IDs, retired mouse controls or an extra jump");
            AssertSameGuidance(instruction, PrologueGuidanceModel.Evaluate(eligible with
                { Objective = PrologueObjective.ClimbCanyon, BossState = PrologueBossGuidanceState.None }, remapped),
                "the earlier climb objective and absent arena use the same local transfer help");
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
            foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
            foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
            {
                var boundary = eligible with { Sector = sector, Objective = objective, ClawState = state };
                if (sector == PrologueSectorId.CanyonAscent &&
                    objective is (PrologueObjective.ClimbCanyon or PrologueObjective.EnterTemple) &&
                    state == MechanicalClawState.Idle) continue;
                AssertSameGuidance(PrologueGuidanceModel.Evaluate(boundary with
                    { AscentTransferOpportunity = false }, remapped),
                    PrologueGuidanceModel.Evaluate(boundary, remapped),
                    "local transfer cannot replace another sector, objective or non-idle claw instruction");
            }
            foreach (PrologueBossGuidanceState boss in new[]
                { PrologueBossGuidanceState.Active, PrologueBossGuidanceState.ChainExposed })
            {
                var boundary = eligible with { BossState = boss };
                AssertSameGuidance(PrologueGuidanceModel.Evaluate(boundary with
                    { AscentTransferOpportunity = false }, remapped),
                    PrologueGuidanceModel.Evaluate(boundary, remapped),
                    "local transfer never displaces an active Boss instruction");
            }
            AssertSameGuidance(PrologueGuidanceInstruction.Hidden,
                PrologueGuidanceModel.Evaluate(eligible with { GuidanceEnabled = false }, remapped),
                "local transfer cannot reopen disabled guidance");
            var retry = eligible with { SameSectorFailureCount = 2 };
            TestAssert.Equal(PrologueGuidanceModel.Evaluate(retry with
                { AscentTransferOpportunity = false }, remapped).HintText,
                PrologueGuidanceModel.Evaluate(retry, remapped).HintText,
                "local transfer preserves the existing optional retry help");
        }
        RunTempleTerrainGuidance();
        GD.Print("ASCENT_TRANSFER_GUIDANCE_MODEL_OK localized=true live_glyphs=true other_contexts_preserved=true");
    }

    private static readonly IReadOnlyDictionary<StringName, string> Glyphs =
        new Dictionary<StringName, string>
        {
            [InputActions.MoveLeft] = "A",
            [InputActions.MoveRight] = "D",
            [InputActions.MoveUp] = "W",
            [InputActions.MoveDown] = "S",
            [InputActions.Jump] = "SPACE",
            [InputActions.Dash] = "SHIFT",
            [InputActions.SelectAttackMode] = "Q",
            [InputActions.SelectGrappleMode] = "E",
            [InputActions.PrimaryAction] = "LMB",
            [InputActions.Shield] = "RMB",
            [InputActions.Attack] = "J",
            [InputActions.Claw] = "K",
            [InputActions.KeyboardShield] = "L",
            [InputActions.Slot1] = "1",
            [InputActions.Slot2] = "2",
            [InputActions.Slot3] = "3",
            [InputActions.Slot4] = "4"
        };

    public static void RunTempleTerrainGuidance()
    {
        LoadStrings();
        var first = new PrologueGuidanceContext(PrologueObjective.DefeatBellkeeper,
            PrologueSectorId.LostTemple, PrologueBossGuidanceState.Inactive, 0,
            GameLanguage.ChineseSimplified, true, ClawState: MechanicalClawState.Swing,
            TerrainClawAttached: true);
        PrologueGuidanceInstruction attached = PrologueGuidanceModel.Evaluate(first, Glyphs);
        TestAssert.True(attached.Visible && attached.Targets.Count == 1 &&
            attached.Targets[0] == PrologueGuidanceTargetKey.ClawAttachment,
            "Temple attached terrain offers the live claw exit instruction");

        // Wrong phase, Boss ownership, or absent real-terrain evidence must restore the objective.
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueObjective objective in new[]
            { PrologueObjective.PullTempleMechanism, PrologueObjective.DefeatBellkeeper })
        foreach (PrologueBossGuidanceState boss in Enum.GetValues<PrologueBossGuidanceState>())
        foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
        {
            var context = first with { Objective = objective, Language = language, BossState = boss,
                ClawState = state, SameSectorFailureCount = 2 };
            PrologueGuidanceInstruction ordinary = PrologueGuidanceModel.Evaluate(
                context with { TerrainClawAttached = false }, Glyphs);
            PrologueGuidanceInstruction baseline = PrologueGuidanceModel.Evaluate(
                context with { TerrainClawAttached = false, ClawState = MechanicalClawState.Idle }, Glyphs);
            string label = $"{language}/{objective}/{boss}/{state}";
            AssertSameGuidance(baseline, ordinary, $"{label} absent terrain evidence retains prior objective teaching");
            AssertTempleObjectiveTargets(ordinary, objective, boss, label);
            PrologueGuidanceInstruction actual = PrologueGuidanceModel.Evaluate(context, Glyphs);
            bool eligible = boss is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
                state is (MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Boost);
            if (!eligible)
            {
                AssertSameGuidance(ordinary, actual, $"{label} restores ordinary objective outside attached terrain scope");
                continue;
            }
            TestAssert.True(actual.Visible && actual.Targets.Count == 1 &&
                actual.Targets[0] == PrologueGuidanceTargetKey.ClawAttachment,
                $"{label} points only at the live terrain attachment");
            foreach (string required in language == GameLanguage.English
                ? new[] { "auto-pull", "A/D", "optional swing", "SPACE", "leap-release", "J", "K", "L", "cancel" }
                : new[] { "自动牵引", "A/D", "可选摆荡", "SPACE", "跃出", "松爪", "J", "K", "L", "取消" })
                TestAssert.True(actual.StepText.Contains(required, StringComparison.OrdinalIgnoreCase),
                    $"{label} teaches attached control {required}");
            AssertNoRetiredAttachedControls(actual.StepText, label);
            TestAssert.Equal(ordinary.HintText, actual.HintText, $"{label} keeps sector retry help");
            TestAssert.Equal(ordinary.HeadingText, actual.HeadingText, $"{label} keeps localized heading");
            TestAssert.Equal(ordinary.HintPrefix, actual.HintPrefix, $"{label} keeps localized hint prefix");
            TestAssert.Equal(ordinary.ContextTitle, actual.ContextTitle, $"{label} keeps objective context title");
            AssertSameGuidance(ordinary, PrologueGuidanceModel.Evaluate(
                context with { TerrainClawAttached = false }, Glyphs),
                $"{label} losing terrain evidence immediately restores the complete objective instruction");
        }

        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
        foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
        foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
        {
            if (sector == PrologueSectorId.LostTemple &&
                objective is (PrologueObjective.PullTempleMechanism or PrologueObjective.DefeatBellkeeper)) continue;
            var context = first with { Objective = objective, Sector = sector, Language = language, ClawState = state };
            AssertSameGuidance(PrologueGuidanceModel.Evaluate(context with { TerrainClawAttached = false }, Glyphs),
                PrologueGuidanceModel.Evaluate(context, Glyphs),
                $"{language}/{objective}/{sector}/{state} terrain evidence cannot change another objective or sector");
        }
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
        foreach (var hidden in new[] { first with { GuidanceEnabled = false },
            first with { Objective = PrologueObjective.Complete } })
            AssertSameGuidance(PrologueGuidanceInstruction.Hidden, PrologueGuidanceModel.Evaluate(
                hidden with { Language = language, ClawState = state }, Glyphs),
                $"{language}/{state} terrain attachment never reopens disabled or completed teaching");

        var remapped = new Dictionary<StringName, string>(Glyphs)
        {
            [InputActions.MoveLeft] = "SWING_LEFT", [InputActions.MoveRight] = "SWING_RIGHT",
            [InputActions.MoveUp] = "REEL_IN", [InputActions.MoveDown] = "REEL_OUT",
            [InputActions.Jump] = "LEAP_KEY", [InputActions.Attack] = "STRIKE_CANCEL",
            [InputActions.KeyboardShield] = "CANCEL_BUTTON", [InputActions.Claw] = "CANCEL_KEY"
        };
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueObjective objective in new[]
            { PrologueObjective.PullTempleMechanism, PrologueObjective.DefeatBellkeeper })
        {
            string text = PrologueGuidanceModel.Evaluate(
                first with { Language = language, Objective = objective }, remapped).StepText;
            foreach (string glyph in new[] { "SWING_LEFT", "SWING_RIGHT", "LEAP_KEY", "CANCEL_KEY", "STRIKE_CANCEL", "CANCEL_BUTTON" })
                TestAssert.True(text.Contains(glyph, StringComparison.Ordinal),
                    $"{language}/{objective} attached teaching uses supplied glyph {glyph}");
            TestAssert.True(!System.Text.RegularExpressions.Regex.IsMatch(text,
                @"(?<![A-Za-z0-9_])(?:A|D|W|S|SPACE|J|K|L|Q|RMB|LMB)(?![A-Za-z0-9_])") &&
                !text.Contains("REEL_IN") && !text.Contains("REEL_OUT") && !text.Contains("CLICK_KEY") &&
                !text.Contains('{') && !text.Contains('}'),
                $"{language}/{objective} attached teaching leaks neither default controls, manual reeling nor cast input");
            AssertNoRetiredAttachedControls(text, $"{language}/{objective}/remapped");
        }
        // Preserve the complete existing guidance-model suite and actual localization checks.
        Run();
    }

    private static void AssertTempleObjectiveTargets(PrologueGuidanceInstruction instruction,
        PrologueObjective objective, PrologueBossGuidanceState boss, string label)
    {
        if (objective == PrologueObjective.PullTempleMechanism)
            TestAssert.True(instruction.Visible && instruction.Targets.Count == 2 &&
                instruction.Targets[0] == PrologueGuidanceTargetKey.TempleMechanism &&
                instruction.Targets[1] == PrologueGuidanceTargetKey.TempleCheckpoint,
                $"{label} ordinary mechanism teaching keeps mechanism and checkpoint targets");
        else
            TestAssert.True(instruction.Visible && instruction.Targets.Count == 1 &&
                instruction.Targets[0] == (boss == PrologueBossGuidanceState.Active
                    ? PrologueGuidanceTargetKey.BossBody : boss == PrologueBossGuidanceState.ChainExposed
                    ? PrologueGuidanceTargetKey.BossChain : PrologueGuidanceTargetKey.BossActivation),
                $"{label} ordinary Boss teaching keeps its own activation, body or chain target");
    }

    private static void AssertNoRetiredAttachedControls(string text, string label)
    {
        foreach (string obsolete in new[] { "W/S", "hold LMB", "按住 LMB", "release LMB", "松开 LMB",
            "double jump", "二段跳", "Space disconnect" })
            TestAssert.True(!text.Contains(obsolete, StringComparison.OrdinalIgnoreCase),
                $"{label} attached teaching rejects obsolete operation {obsolete}");
    }

    public static void Run()
    {
        LoadStrings();
        RunRecyclingTeaching();
        BasinShieldObjectiveTemporarilyTeachesTheLiveClaw();
        OptionalDescentClawRespectsObjectiveAndPhase();
        MechanicalClawGuidanceUsesApprovedBindingsAndExcludesInternalNames();
        DirectionalTeachingUsesConfiguredBindings();
        ClawPhaseBoundariesPreserveObjectiveOwnership();
        AttachedTeachingPreservesExpandedFailureContext();
        StagedClawGuidanceUsesSuppliedGlyphs();
        FirstCombatGuidanceRespectsEveryContextBoundary();
        FirstCombatGuidanceNeverReopensHiddenTeaching();
        FirstCombatGuidanceExplainsControlsThroughSuppliedGlyphs();
        FirstLevelGuidanceCoversEveryContinuousMechanicInBothLanguages();
        EveryInstructionUsesLiveSectorTargetsAndSuppliedGlyphs();
        ProductTextContainsNoLegacyControlsRoutesOrRooms();
        CompleteAndDisabledGuidanceStayHidden();
    }

    private static void FirstLevelGuidanceCoversEveryContinuousMechanicInBothLanguages()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            IReadOnlyList<PrologueGuidanceInstruction> instructions = BuildSequence(language);
            string all = string.Join("\n", CollectText(instructions));
            foreach (string token in new[] { "J", "K", "L", "1", "2", "3", "4", "A", "D", "WASD", "SPACE" })
                TestAssert.True(all.Contains(token, StringComparison.Ordinal),
                    $"{language} first-level guidance includes live glyph {token}");
            foreach (string concept in language == GameLanguage.English
                         ? new[] { "energy", "overheat", "rough", "ice", "auto-pull", "leap-release" }
                         : new[] { "耗能", "过热", "粗糙", "冰面", "自动牵引", "跃出" })
                TestAssert.True(all.Contains(concept, StringComparison.OrdinalIgnoreCase),
                    $"{language} first-level guidance covers {concept}");
        }
    }

    private static void DirectionalTeachingUsesConfiguredBindings()
    {
        var remapped = new Dictionary<StringName, string>(Glyphs)
        {
            [InputActions.MoveUp] = "UP", [InputActions.MoveLeft] = "LEFT",
            [InputActions.MoveDown] = "DOWN", [InputActions.MoveRight] = "RIGHT"
        };
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var entrance = new PrologueGuidanceContext(PrologueObjective.Wake, PrologueSectorId.CanyonEntrance,
                PrologueBossGuidanceState.Inactive, 0, language, true);
            var basin = entrance with { Objective = PrologueObjective.LearnShield, Sector = PrologueSectorId.CanyonBasin };
            var temple = entrance with { Objective = PrologueObjective.PullTempleMechanism, Sector = PrologueSectorId.LostTemple };
            var boss = temple with { Objective = PrologueObjective.DefeatBellkeeper, BossState = PrologueBossGuidanceState.Active };
            var cases = new (PrologueGuidanceContext Context, bool Hint)[]
            {
                (entrance with { FirstCombatNearby = true }, false),
                (entrance with { Objective = PrologueObjective.LearnGrappleMode }, false),
                (basin, false), (basin with { Objective = PrologueObjective.ClimbCanyon }, false),
                (temple, false), (boss, false),
                (boss with { BossState = PrologueBossGuidanceState.ChainExposed }, false),
                (basin with { SameSectorFailureCount = 2 }, true),
                (temple with { SameSectorFailureCount = 2 }, true)
            };
            foreach (var sample in cases)
            {
                var original = PrologueGuidanceModel.Evaluate(sample.Context, Glyphs);
                var custom = PrologueGuidanceModel.Evaluate(sample.Context, remapped);
                string defaultText = sample.Hint ? original.HintText : original.StepText;
                string customText = sample.Hint ? custom.HintText : custom.StepText;
                TestAssert.True(defaultText.Contains("WASD", StringComparison.Ordinal) &&
                    customText.Contains("UP/LEFT/DOWN/RIGHT", StringComparison.Ordinal) &&
                    !customText.Contains("WASD", StringComparison.Ordinal) && !customText.Contains('{'),
                    $"{language} {sample.Context.Objective} {sample.Context.BossState} hint={sample.Hint} " +
                    $"must preserve default WASD and show all configured direction keys in order: '{customText}'");
            }
        }
    }

    private static void MechanicalClawGuidanceUsesApprovedBindingsAndExcludesInternalNames()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            string ready = InstructionText(Evaluate(PrologueObjective.LearnGrappleMode,
                PrologueSectorId.CanyonDescent, language));
            foreach (string premature in new[] { "W/S", "SPACE", "A/D" })
                TestAssert.True(!ready.Contains(premature, StringComparison.OrdinalIgnoreCase),
                    $"{language} unattached claw card defers attached-only operation {premature}");
            foreach (string required in language == GameLanguage.English
                         ? new[] { "WASD", "K", "auto-pull", "rough", "diamond" }
                         : new[] { "WASD", "K", "自动牵引", "粗糙", "菱形" })
                TestAssert.True(ready.Contains(required, StringComparison.OrdinalIgnoreCase),
                    $"{language} unattached card explains the next cast action {required}");
            string attached = InstructionText(Evaluate(PrologueObjective.LearnGrappleMode,
                PrologueSectorId.CanyonDescent, language, MechanicalClawState.Latched));
            foreach (string required in language == GameLanguage.English
                         ? new[] { "A/D", "optional swing", "SPACE", "leap-release", "J", "K", "L" }
                         : new[] { "A/D", "可选摆荡", "SPACE", "跃出", "J", "K", "L" })
                TestAssert.True(attached.Contains(required, StringComparison.OrdinalIgnoreCase),
                    $"{language} attached card explains available operation {required}");
            foreach (string obsolete in new[] { "W/S", "hold LMB", "按住 LMB", "release LMB", "松开 LMB" })
                TestAssert.True(!attached.Contains(obsolete, StringComparison.OrdinalIgnoreCase),
                    $"{language} attached card rejects obsolete operation {obsolete}");
            string descent = InstructionText(Evaluate(PrologueObjective.ReachBasin,
                PrologueSectorId.CanyonDescent, language));
            foreach (string material in language == GameLanguage.English
                         ? new[] { "ice", "sealed" } : new[] { "冰面", "封闭" })
                TestAssert.True(descent.Contains(material, StringComparison.OrdinalIgnoreCase),
                    $"{language} descent stage retains the material warning {material}");
            foreach (string forbidden in new[] { "GripStone", "二段跳", "double jump", "Space disconnect" })
                TestAssert.True(!(ready + attached + descent).Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"staged mechanical-claw instruction excludes {forbidden}");
        }
    }

    private static void OptionalDescentClawRespectsObjectiveAndPhase()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var context = new PrologueGuidanceContext(PrologueObjective.ReachBasin,
                PrologueSectorId.CanyonDescent, PrologueBossGuidanceState.Inactive, 0, language, true,
                DescentClawOpportunity: true);
            foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
            {
                var actual = PrologueGuidanceModel.Evaluate(context with { ClawState = state }, Glyphs);
                bool attached = state is MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Boost;
                bool preparing = state is MechanicalClawState.Idle or MechanicalClawState.Aim or
                    MechanicalClawState.Windup or MechanicalClawState.Travel;
                TestAssert.Equal(attached ? PrologueGuidanceTargetKey.ClawAttachment : preparing
                    ? PrologueGuidanceTargetKey.DescentClawOpportunity : PrologueGuidanceTargetKey.DescentIce,
                    actual.Targets[0], $"{language}/{state} offers a local cast, actual attachment, or ordinary descent");
                if (preparing)
                    TestAssert.True(actual.StepText.Contains("W") && actual.StepText.Contains("K") &&
                        actual.StepText.Contains("A/D") && !actual.StepText.Contains("SPACE"),
                        "optional cast teaches the next action and preserves walking without early Boost overload");
            }
            foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
            {
                if (objective == PrologueObjective.ReachBasin && sector == PrologueSectorId.CanyonDescent) continue;
                var ordinary = context with { Objective = objective, Sector = sector, DescentClawOpportunity = false };
                var actual = PrologueGuidanceModel.Evaluate(ordinary with { DescentClawOpportunity = true }, Glyphs);
                var expected = PrologueGuidanceModel.Evaluate(ordinary, Glyphs);
                TestAssert.Equal(expected.StepText, actual.StepText, "local opportunity cannot replace another objective/sector");
                TestAssert.Equal(string.Join("|", expected.Targets), string.Join("|", actual.Targets),
                    "local opportunity does not redirect unrelated objectives");
            }
            var hidden = PrologueGuidanceModel.Evaluate(context with { GuidanceEnabled = false }, Glyphs);
            TestAssert.True(!hidden.Visible && hidden.Targets.Count == 0, "opportunity cannot reopen disabled guidance");
            var retry = PrologueGuidanceModel.Evaluate(context with { SameSectorFailureCount = 2 }, Glyphs);
            TestAssert.True(!string.IsNullOrEmpty(retry.HintText), "optional opportunity retains material/safe-platform retry help");
            var remapped = new Dictionary<StringName, string>(Glyphs)
            { [InputActions.Claw] = "CAST_CUSTOM", [InputActions.MoveUp] = "UP_CUSTOM" };
            var custom = PrologueGuidanceModel.Evaluate(context, remapped);
            TestAssert.True(custom.StepText.Contains("CAST_CUSTOM") && !custom.StepText.Contains("LMB"),
                "the optional opportunity uses the supplied independent claw binding");
            string upPrefix = language == GameLanguage.English ? "UP_CUSTOM: AIM UP" : "UP_CUSTOM 瞄准";
            TestAssert.True(custom.StepText.Contains(upPrefix, StringComparison.Ordinal),
                $"DESCENT_MOVE_UP_GLYPH: {language} optional opportunity must contain literal '{upPrefix}'");
            TestAssert.True(!custom.StepText.Contains("W 瞄准", StringComparison.Ordinal) &&
                !custom.StepText.Contains("W: AIM UP", StringComparison.Ordinal) &&
                !custom.StepText.Contains('{') && !custom.StepText.Contains('}'),
                $"DESCENT_MOVE_UP_GLYPH: {language} optional opportunity leaks neither old W aim prefixes nor unresolved tokens");
        }
    }

    private static void ClawPhaseBoundariesPreserveObjectiveOwnership()
    {
        MechanicalClawState[] attachedStates =
            [MechanicalClawState.Latched, MechanicalClawState.Reel, MechanicalClawState.Swing, MechanicalClawState.Boost];
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueObjective objective in new[] { PrologueObjective.LearnGrappleMode,
            PrologueObjective.ReachBasin, PrologueObjective.ClimbCanyon, PrologueObjective.EnterTemple })
        {
            PrologueGuidanceInstruction baseline = Evaluate(objective, PrologueSectorId.CanyonAscent, language);
            foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
            {
                PrologueGuidanceInstruction actual = Evaluate(objective, PrologueSectorId.CanyonAscent, language, state);
                TestAssert.True(actual.Visible && actual.Targets.Count > 0,
                    $"{objective}/{state} keeps a visible live-target instruction");
                TestAssert.Equal(string.Join("|", baseline.Targets), string.Join("|", actual.Targets),
                    $"{objective}/{state} changes teaching phase without replacing objective targets");
                if (Array.IndexOf(attachedStates, state) >= 0)
                    TestAssert.True(actual.StepText.Contains("A/D") && actual.StepText.Contains("SPACE") &&
                        actual.StepText.Contains(language == GameLanguage.English ? "optional swing" : "可选摆荡",
                            StringComparison.OrdinalIgnoreCase) &&
                        actual.StepText.Contains(language == GameLanguage.English ? "leap-release" : "跃出",
                            StringComparison.OrdinalIgnoreCase),
                        $"{objective}/{state} exposes attached controls");
                else
                    TestAssert.Equal(baseline.StepText, actual.StepText,
                        $"{objective}/{state} restores its own unattached objective instruction");
            }
        }
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (MechanicalClawState state in attachedStates)
        {
            foreach (PrologueObjective objective in new[] { PrologueObjective.Wake, PrologueObjective.LearnAttackMode,
                PrologueObjective.LearnShield, PrologueObjective.PullTempleMechanism, PrologueObjective.DefeatBellkeeper })
            {
                PrologueGuidanceInstruction baseline = Evaluate(objective, PrologueSectorId.LostTemple, language);
                PrologueGuidanceInstruction actual = Evaluate(objective, PrologueSectorId.LostTemple, language, state);
                TestAssert.Equal(baseline.StepText, actual.StepText,
                    $"{objective} is not replaced by attached teaching in {state}");
            }
            foreach (bool enabled in new[] { false, true })
            {
                PrologueGuidanceInstruction hidden = PrologueGuidanceModel.Evaluate(new(
                    enabled ? PrologueObjective.Complete : PrologueObjective.LearnGrappleMode,
                    PrologueSectorId.CanyonAscent, PrologueBossGuidanceState.None, 0, language,
                    enabled, ClawState: state), Glyphs);
                TestAssert.True(!hidden.Visible && hidden.Targets.Count == 0,
                    $"{state} cannot reopen completed or disabled guidance");
            }
        }
    }

    private static void BasinShieldObjectiveTemporarilyTeachesTheLiveClaw()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var context = new PrologueGuidanceContext(PrologueObjective.LearnShield,
                PrologueSectorId.CanyonBasin, PrologueBossGuidanceState.None, 0, language, true);
            var shield = PrologueGuidanceModel.Evaluate(context, Glyphs);
            TestAssert.True(shield.StepText.Contains("L") && shield.Targets.Count == 2 &&
                shield.Targets[0] == PrologueGuidanceTargetKey.BasinShieldLane &&
                shield.Targets[1] == PrologueGuidanceTargetKey.BasinCheckpoint,
                "ordinary Basin LearnShield still teaches shielding and points at its lane/checkpoint");
            foreach (PrologueBossGuidanceState boss in Enum.GetValues<PrologueBossGuidanceState>())
            foreach (MechanicalClawState state in Enum.GetValues<MechanicalClawState>())
            {
                var actual = PrologueGuidanceModel.Evaluate(context with { BossState = boss, ClawState = state }, Glyphs);
                bool attached = state is MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Boost;
                if (attached && boss is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive))
                {
                    foreach (string control in new[] { "A/D", "SPACE", "J", "K", "L" })
                        TestAssert.True(actual.StepText.Contains(control),
                            $"{language}/Basin LearnShield/{boss}/{state} teaches the live claw control {control}");
                    TestAssert.True(actual.StepText.Contains(language == GameLanguage.English ? "leap-release" : "跃出"),
                        "attached action copy explains the Space release transaction");
                    TestAssert.Equal(1, actual.Targets.Count, "attached Basin teaching has one actual attachment target");
                    TestAssert.Equal(PrologueGuidanceTargetKey.ClawAttachment, actual.Targets[0],
                        "attached Basin teaching points at the live claw, not the shield lane/checkpoint");
                }
                else AssertSameGuidance(shield, actual,
                    $"{language}/{boss}/{state} restores ordinary shield guidance outside the allowed attachment phase");
            }
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
            {
                if (sector == PrologueSectorId.CanyonBasin) continue;
                var ordinary = context with { Sector = sector };
                AssertSameGuidance(PrologueGuidanceModel.Evaluate(ordinary, Glyphs),
                    PrologueGuidanceModel.Evaluate(ordinary with { ClawState = MechanicalClawState.Latched }, Glyphs),
                    "the new LearnShield override is limited to Basin, not a global objective rewrite");
            }
            foreach (PrologueObjective objective in new[] { PrologueObjective.PullTempleMechanism, PrologueObjective.DefeatBellkeeper })
            {
                var ordinary = context with { Objective = objective, Sector = PrologueSectorId.LostTemple };
                AssertSameGuidance(PrologueGuidanceModel.Evaluate(ordinary, Glyphs),
                    PrologueGuidanceModel.Evaluate(ordinary with { ClawState = MechanicalClawState.Reel }, Glyphs),
                    "live claw teaching does not replace temple mechanism or Boss objectives");
            }
            foreach (var hidden in new[] { context with { GuidanceEnabled = false },
                         context with { Objective = PrologueObjective.Complete } })
                AssertSameGuidance(PrologueGuidanceInstruction.Hidden,
                    PrologueGuidanceModel.Evaluate(hidden with { ClawState = MechanicalClawState.Reel }, Glyphs),
                    "Basin attachment cannot reopen disabled or completed guidance");
        }
    }

    private static void AttachedTeachingPreservesExpandedFailureContext()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var context = new PrologueGuidanceContext(PrologueObjective.LearnGrappleMode,
                PrologueSectorId.CanyonDescent, PrologueBossGuidanceState.None, 2, language, true);
            PrologueGuidanceInstruction ready = PrologueGuidanceModel.Evaluate(context, Glyphs);
            PrologueGuidanceInstruction attached = PrologueGuidanceModel.Evaluate(
                context with { ClawState = MechanicalClawState.Latched }, Glyphs);
            TestAssert.True(!string.IsNullOrWhiteSpace(ready.HintText) &&
                !string.IsNullOrWhiteSpace(ready.HeadingText) && !string.IsNullOrWhiteSpace(ready.HintPrefix),
                $"{language} two-failure fixture has real contextual help and localized chrome");
            TestAssert.True(attached.StepText != ready.StepText && attached.StepText.Contains("A/D") &&
                attached.StepText.Contains("SPACE") && !attached.StepText.Contains("W/S"),
                $"{language} attaching replaces only the immediate action instruction after two failures");
            TestAssert.Equal(ready.HintText, attached.HintText,
                $"{language} attaching preserves the expanded sector-specific failure hint");
            TestAssert.Equal(ready.HeadingText, attached.HeadingText,
                $"{language} attaching preserves the localized teaching heading");
            TestAssert.Equal(ready.HintPrefix, attached.HintPrefix,
                $"{language} attaching preserves the localized hint prefix");
            TestAssert.Equal(string.Join("|", ready.Targets), string.Join("|", attached.Targets),
                $"{language} attaching preserves objective targets after two failures");

            var shieldContext = context with { Objective = PrologueObjective.LearnShield,
                Sector = PrologueSectorId.CanyonBasin };
            PrologueGuidanceInstruction shield = PrologueGuidanceModel.Evaluate(shieldContext, Glyphs);
            PrologueGuidanceInstruction attachedShield = PrologueGuidanceModel.Evaluate(
                shieldContext with { ClawState = MechanicalClawState.Latched }, Glyphs);
            // Deliberate scope revision: Basin's pending shield lesson yields its immediate
            // card/marker to a live claw, but does not discard retry context or lesson ownership.
            TestAssert.True(attachedShield.StepText != shield.StepText && attachedShield.StepText.Contains("A/D") &&
                attachedShield.StepText.Contains("SPACE") && attachedShield.StepText.Contains("L") &&
                !attachedShield.StepText.Contains("W/S"),
                $"{language} expanded Basin teaching presents the currently usable attached controls");
            TestAssert.Equal(1, attachedShield.Targets.Count, "expanded Basin attachment has one live target");
            TestAssert.Equal(PrologueGuidanceTargetKey.ClawAttachment, attachedShield.Targets[0],
                "expanded Basin attachment follows the current claw instead of shield objectives");
            TestAssert.Equal(shield.HintText, attachedShield.HintText,
                $"{language} expanded shield teaching retains its own sector hint");
            TestAssert.Equal(shield.HeadingText, attachedShield.HeadingText, "Basin attachment preserves localized heading");
            TestAssert.Equal(shield.HintPrefix, attachedShield.HintPrefix, "Basin attachment preserves localized hint prefix");
            AssertSameGuidance(shield, PrologueGuidanceModel.Evaluate(
                shieldContext with { ClawState = MechanicalClawState.Released }, Glyphs),
                "detaching restores the complete pending shield instruction and retry targets");
            foreach (PrologueGuidanceContext hiddenContext in new[]
                { context with { GuidanceEnabled = false }, context with { Objective = PrologueObjective.Complete } })
            {
                PrologueGuidanceInstruction hidden = PrologueGuidanceModel.Evaluate(
                    hiddenContext with { ClawState = MechanicalClawState.Latched }, Glyphs);
                TestAssert.True(!hidden.Visible && hidden.Targets.Count == 0 && hidden.StepText.Length == 0 &&
                    hidden.HintText.Length == 0 && hidden.HeadingText.Length == 0 && hidden.HintPrefix.Length == 0,
                    $"{language} two failures and attachment do not leak disabled or completed teaching");
            }
        }
    }

    private static void StagedClawGuidanceUsesSuppliedGlyphs()
    {
        var custom = new Dictionary<StringName, string>();
        foreach (var pair in Glyphs) custom[pair.Key] = pair.Value;
        custom[InputActions.Claw] = "CAST_KEY";
        custom[InputActions.MoveLeft] = "SWING_LEFT";
        custom[InputActions.MoveRight] = "SWING_RIGHT";
        custom[InputActions.MoveUp] = "REEL_IN";
        custom[InputActions.MoveDown] = "REEL_OUT";
        custom[InputActions.Jump] = "BOOST_KEY";
        custom[InputActions.KeyboardShield] = "CANCEL_BUTTON";
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (MechanicalClawState state in new[] { MechanicalClawState.Idle, MechanicalClawState.Latched })
        {
            string text = InstructionText(PrologueGuidanceModel.Evaluate(new(
                PrologueObjective.LearnGrappleMode, PrologueSectorId.CanyonDescent,
                PrologueBossGuidanceState.None, 0, language, true, ClawState: state), custom));
            foreach (string required in state == MechanicalClawState.Idle
                         ? new[] { "CAST_KEY" }
                         : new[] { "SWING_LEFT", "SWING_RIGHT", "BOOST_KEY", "CANCEL_BUTTON" })
                TestAssert.True(text.Contains(required), $"{language}/{state} substitutes supplied glyph {required}");
            TestAssert.True(!text.Contains("LMB") && !text.Contains("SPACE") &&
                !text.Contains("W/S") && !text.Contains("A/D"),
                $"{language}/{state} does not leak hardcoded default action glyphs");
        }
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            string text = PrologueGuidanceModel.Evaluate(new(PrologueObjective.LearnShield,
                PrologueSectorId.CanyonBasin, PrologueBossGuidanceState.Inactive, 0, language, true,
                ClawState: MechanicalClawState.Swing), custom).StepText;
            foreach (string required in new[] { "SWING_LEFT", "SWING_RIGHT", "BOOST_KEY", "CANCEL_BUTTON" })
                TestAssert.True(text.Contains(required), $"{language} Basin attachment substitutes supplied glyph {required}");
            TestAssert.True(!text.Contains("W/S") && !text.Contains("SPACE") && !text.Contains("LMB") &&
                !text.Contains("A/D") && !text.Contains("RMB"),
                "Basin attachment contains neither hardcoded claw bindings nor the old shield instruction");
        }
    }

    private static void FirstCombatGuidanceRespectsEveryContextBoundary()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var ready = new PrologueGuidanceContext(PrologueObjective.LearnAttackMode,
                PrologueSectorId.CanyonEntrance, PrologueBossGuidanceState.None, 2, language, true,
                FirstCombatNearby: true);
            PrologueGuidanceInstruction combat = PrologueGuidanceModel.Evaluate(ready, Glyphs);
            TestAssert.True(combat.Visible && !string.IsNullOrWhiteSpace(combat.StepText) &&
                !string.IsNullOrWhiteSpace(combat.ContextTitle) && combat.Targets.Count == 0 &&
                combat.HintText.Length == 0,
                $"{language} nearby combat supplies a title and stable card without route markers or old failure hints");
            TestAssert.True(PrologueGuidanceModel.Evaluate(ready with { FirstCombatNearby = false }, Glyphs)
                .HintText.Length > 0, "the two-failure baseline really has a sector hint to suppress");

            foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
            foreach (PrologueBossGuidanceState boss in Enum.GetValues<PrologueBossGuidanceState>())
            foreach (MechanicalClawState claw in Enum.GetValues<MechanicalClawState>())
            {
                var context = ready with { Objective = objective, Sector = sector, BossState = boss,
                    ClawState = claw };
                PrologueGuidanceInstruction actual = PrologueGuidanceModel.Evaluate(context, Glyphs);
                bool eligible = sector == PrologueSectorId.CanyonEntrance &&
                    objective is (PrologueObjective.Wake or PrologueObjective.LearnAttackMode or
                        PrologueObjective.LearnGrappleMode or PrologueObjective.ReachBasin) &&
                    boss is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
                    claw == MechanicalClawState.Idle;
                string label = $"{language}/{objective}/{sector}/{boss}/{claw}";
                PrologueGuidanceInstruction ordinary = PrologueGuidanceModel.Evaluate(
                    context with { FirstCombatNearby = false }, Glyphs);
                TestAssert.Equal(string.Empty, ordinary.ContextTitle,
                    $"{label} ordinary teaching never inherits a combat title");
                if (eligible)
                {
                    TestAssert.True(actual.Visible && actual.Targets.Count == 0 && actual.HintText.Length == 0,
                        $"{label} combat suppresses old route targets and failure hints");
                    TestAssert.Equal(combat.StepText, actual.StepText,
                        $"{label} early objective changes do not replace the current combat instruction");
                    TestAssert.Equal(combat.ContextTitle, actual.ContextTitle,
                        $"{label} early objectives share the same contextual title");
                    TestAssert.Equal(ordinary.HeadingText, actual.HeadingText,
                        $"{label} combat retains localized teaching chrome");
                    TestAssert.Equal(ordinary.HintPrefix, actual.HintPrefix,
                        $"{label} combat does not change localized hint chrome");
                }
                else AssertSameGuidance(ordinary, actual, $"{label} keeps its own instruction outside combat scope");

                PrologueGuidanceInstruction restored = PrologueGuidanceModel.Evaluate(
                    context with { FirstCombatNearby = false }, Glyphs);
                AssertSameGuidance(ordinary, restored, $"{label} leaving combat restores the ordinary step and targets");
            }
        }
    }

    private static void FirstCombatGuidanceNeverReopensHiddenTeaching()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
        foreach (MechanicalClawState claw in Enum.GetValues<MechanicalClawState>())
        foreach (bool enabled in new[] { false, true })
        {
            if (enabled && objective != PrologueObjective.Complete) continue;
            PrologueGuidanceInstruction hidden = PrologueGuidanceModel.Evaluate(new(objective,
                PrologueSectorId.CanyonEntrance, PrologueBossGuidanceState.None, 2, language, enabled,
                ClawState: claw, FirstCombatNearby: true), Glyphs);
            TestAssert.True(!hidden.Visible && hidden.Targets.Count == 0 && hidden.ContextTitle.Length == 0 &&
                hidden.StepText.Length == 0 && hidden.HintText.Length == 0 &&
                hidden.HeadingText.Length == 0 && hidden.HintPrefix.Length == 0,
                $"{language}/{objective}/{claw}/enabled={enabled} cannot leak nearby combat teaching or title");
        }
    }

    private static void FirstCombatGuidanceExplainsControlsThroughSuppliedGlyphs()
    {
        var custom = new Dictionary<StringName, string>();
        foreach (var pair in Glyphs) custom[pair.Key] = pair.Value;
        custom[InputActions.Slot1] = "WEAPON_ONE";
        custom[InputActions.KeyboardShield] = "GUARD_BUTTON";
        custom[InputActions.Attack] = "STRIKE_BUTTON";
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var context = new PrologueGuidanceContext(PrologueObjective.LearnGrappleMode,
                PrologueSectorId.CanyonEntrance, PrologueBossGuidanceState.None, 0, language, true,
                FirstCombatNearby: true);
            string text = PrologueGuidanceModel.Evaluate(context, Glyphs).StepText;
            foreach (string glyph in new[] { "J", "1", "L", "WASD" })
                TestAssert.True(text.Contains(glyph, StringComparison.Ordinal),
                    $"{language} combat teaches required action glyph {glyph}");
            string[][] concepts = language == GameLanguage.English
                ? [["WASD", "aim"], ["hold"], ["release"], ["press"], ["again", "new", "fresh"],
                    ["wind", "draw", "retract"], ["thrust", "strike", "stab"]]
                : [["WASD", "朝向"], ["按住"], ["松"], ["按"], ["再", "重新"], ["蓄力", "收针"], ["刺"]];
            foreach (string[] alternatives in concepts)
                TestAssert.True(Array.Exists(alternatives,
                    concept => text.Contains(concept, StringComparison.OrdinalIgnoreCase)),
                    $"{language} combat explains {string.Join("/", alternatives)} rather than only listing buttons");
            string rebound = PrologueGuidanceModel.Evaluate(context, custom).StepText;
            foreach (string glyph in new[] { "WEAPON_ONE", "GUARD_BUTTON", "STRIKE_BUTTON" })
                TestAssert.True(rebound.Contains(glyph, StringComparison.Ordinal),
                    $"{language} combat resolves supplied action glyph {glyph}");
            TestAssert.True(!System.Text.RegularExpressions.Regex.IsMatch(rebound,
                @"(?<![A-Za-z0-9_])(?:J|L|Q|1|RMB|LMB)(?![A-Za-z0-9_])") &&
                !rebound.Contains('{') && !rebound.Contains('}'),
                $"{language} rebound combat teaching leaks neither default controls nor unresolved tokens");
        }
    }

    private static void AssertSameGuidance(PrologueGuidanceInstruction expected,
        PrologueGuidanceInstruction actual, string message)
    {
        TestAssert.True(expected.Visible == actual.Visible && expected.StepText == actual.StepText &&
            expected.HintText == actual.HintText && expected.HeadingText == actual.HeadingText &&
            expected.HintPrefix == actual.HintPrefix && expected.ContextTitle == actual.ContextTitle &&
            string.Join("|", expected.Targets) == string.Join("|", actual.Targets), message);
    }

    private static string InstructionText(PrologueGuidanceInstruction instruction) =>
        instruction.StepText + "\n" + instruction.HintText;

    private static void EveryInstructionUsesLiveSectorTargetsAndSuppliedGlyphs()
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueGuidanceInstruction instruction in BuildSequence(language))
        {
            TestAssert.True(instruction.Visible, $"{language} sequence instruction is visible");
            TestAssert.True(instruction.Targets.Count > 0,
                $"{language} visible instruction exposes a stable live-sector target");
            TestAssert.True(!instruction.StepText.Contains('{') && !instruction.StepText.Contains('}'),
                $"{language} instruction resolves every glyph token before presentation");
        }
    }

    private static void ProductTextContainsNoLegacyControlsRoutesOrRooms()
    {
        string all = string.Join("\n", CollectText(BuildSequence(GameLanguage.ChineseSimplified))) + "\n" +
                     string.Join("\n", CollectText(BuildSequence(GameLanguage.English)));
        foreach (string forbidden in new[]
                 {
                     "LMB", "RMB", "点按左键", "右键", "Space disconnect", "double jump", "二段跳",
                      "hold LMB", "按住 LMB", "release LMB", "松开 LMB", "W/S",
                      "换房", "room swap", "GripStone", "上下路线任选其一", "AwakeningCourtyard",
                     "BrokenToothCorridor", "UpperPendulum", "LowerBuriedMachinery",
                     "BellChainMechanism", "FallenBellkeeperArena", "断齿走廊", "上层摆锤", "下层机械区"
                 })
            TestAssert.True(!all.Contains(forbidden, StringComparison.Ordinal),
                $"guidance excludes retired product text '{forbidden}'");
    }

    private static void CompleteAndDisabledGuidanceStayHidden()
    {
        PrologueGuidanceInstruction complete = PrologueGuidanceModel.Evaluate(new(
            PrologueObjective.Complete, PrologueSectorId.LostTemple,
            PrologueBossGuidanceState.Active, 0, GameLanguage.English, true), Glyphs);
        PrologueGuidanceInstruction disabled = PrologueGuidanceModel.Evaluate(new(
            PrologueObjective.LearnAttackMode, PrologueSectorId.CanyonEntrance,
            PrologueBossGuidanceState.None, 0, GameLanguage.English, false), Glyphs);
        TestAssert.True(!complete.Visible && complete.Targets.Count == 0,
            "completed first level hides guidance");
        TestAssert.True(!disabled.Visible && disabled.Targets.Count == 0,
            "first-level guidance setting hides guidance without changing progress");
    }

    private static IReadOnlyList<PrologueGuidanceInstruction> BuildSequence(GameLanguage language) =>
    [
        Evaluate(PrologueObjective.Wake, PrologueSectorId.CanyonEntrance, language),
        Evaluate(PrologueObjective.LearnAttackMode, PrologueSectorId.CanyonEntrance, language),
        Evaluate(PrologueObjective.LearnGrappleMode, PrologueSectorId.CanyonDescent, language),
        Evaluate(PrologueObjective.LearnGrappleMode, PrologueSectorId.CanyonDescent, language, MechanicalClawState.Latched),
        Evaluate(PrologueObjective.ReachBasin, PrologueSectorId.CanyonDescent, language),
        Evaluate(PrologueObjective.LearnShield, PrologueSectorId.CanyonBasin, language),
        Evaluate(PrologueObjective.ClimbCanyon, PrologueSectorId.CanyonAscent, language),
        Evaluate(PrologueObjective.EnterTemple, PrologueSectorId.LostTemple, language),
        Evaluate(PrologueObjective.PullTempleMechanism, PrologueSectorId.LostTemple, language),
        Evaluate(PrologueObjective.DefeatBellkeeper, PrologueSectorId.LostTemple, language)
    ];

    private static PrologueGuidanceInstruction Evaluate(PrologueObjective objective,
        PrologueSectorId sector, GameLanguage language, MechanicalClawState state = MechanicalClawState.Idle) =>
        PrologueGuidanceModel.Evaluate(new(objective, sector, PrologueBossGuidanceState.None, 0,
            language, true, ClawState: state), Glyphs);

    private static IEnumerable<string> CollectText(IEnumerable<PrologueGuidanceInstruction> values)
    {
        foreach (PrologueGuidanceInstruction value in values)
        {
            yield return value.StepText;
            yield return value.HintText;
        }
    }

    private static void LoadStrings()
    {
        string path = ProjectSettings.GlobalizePath("res://assets/localization/prologue-ui.json");
        PrologueStrings.Load(File.ReadAllText(path));
    }
}
