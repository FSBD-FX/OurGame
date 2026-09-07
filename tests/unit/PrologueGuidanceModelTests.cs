using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Knight.Localization;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;

namespace Knight.Tests.Unit;

public static class PrologueGuidanceModelTests
{
    private static readonly IReadOnlyDictionary<StringName, string> Glyphs =
        new Dictionary<StringName, string>
        {
            [InputActions.MoveLeft] = "A",
            [InputActions.MoveRight] = "D",
            [InputActions.MoveUp] = "W",
            [InputActions.MoveDown] = "S",
            [InputActions.Jump] = "SPACE",
            [InputActions.SelectAttackMode] = "Q",
            [InputActions.SelectGrappleMode] = "E",
            [InputActions.PrimaryAction] = "LMB",
            [InputActions.Shield] = "RMB",
            [InputActions.Slot1] = "1",
            [InputActions.Slot2] = "2",
            [InputActions.Slot3] = "3",
            [InputActions.Slot4] = "4"
        };

    public static void Run()
    {
        LoadStrings();
        FirstLevelGuidanceCoversEveryContinuousMechanicInBothLanguages();
        MechanicalClawGuidanceUsesApprovedBindingsAndExcludesInternalNames();
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
            foreach (string token in new[] { "Q", "E", "1", "2", "3", "4", "A", "D", "W", "S", "LMB", "RMB", "SPACE" })
                TestAssert.True(all.Contains(token, StringComparison.Ordinal),
                    $"{language} first-level guidance includes live glyph {token}");
            foreach (string concept in language == GameLanguage.English
                         ? new[] { "energy", "overheat", "rough", "ice", "release" }
                         : new[] { "能量", "过热", "粗糙", "冰面", "松开" })
                TestAssert.True(all.Contains(concept, StringComparison.OrdinalIgnoreCase),
                    $"{language} first-level guidance covers {concept}");
        }
    }

    private static void MechanicalClawGuidanceUsesApprovedBindingsAndExcludesInternalNames()
    {
        PrologueGuidanceInstruction chinese = Evaluate(PrologueObjective.LearnGrappleMode,
            PrologueSectorId.CanyonDescent, GameLanguage.ChineseSimplified);
        PrologueGuidanceInstruction english = Evaluate(PrologueObjective.LearnGrappleMode,
            PrologueSectorId.CanyonDescent, GameLanguage.English);
        foreach (string glyph in new[] { "E", "LMB", "A/D", "W/S", "SPACE" })
        {
            TestAssert.True(chinese.StepText.Contains(glyph, StringComparison.Ordinal),
                $"Chinese mechanical-claw instruction contains live glyph {glyph}");
            TestAssert.True(english.StepText.Contains(glyph, StringComparison.Ordinal),
                $"English mechanical-claw instruction contains live glyph {glyph}");
        }
        foreach (string phrase in new[] { "按住 LMB", "SPACE 跃升一次", "松开 LMB", "冰面", "封闭表面" })
            TestAssert.True(chinese.StepText.Contains(phrase, StringComparison.Ordinal),
                $"Chinese mechanical-claw instruction explains {phrase}");
        foreach (string phrase in new[] { "hold LMB", "one boost", "release LMB", "ice", "sealed" })
            TestAssert.True(english.StepText.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                $"English mechanical-claw instruction explains {phrase}");
        string all = chinese.StepText + "\n" + english.StepText;
        foreach (string forbidden in new[] { "GripStone", "二段跳", "double jump", "Space disconnect" })
            TestAssert.True(!all.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                $"mechanical-claw instruction excludes {forbidden}");
    }

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
                      "按 J", "按 K", " J ", " K ", "J/K", "Space disconnect", "double jump", "二段跳",
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
        Evaluate(PrologueObjective.ReachBasin, PrologueSectorId.CanyonDescent, language),
        Evaluate(PrologueObjective.LearnShield, PrologueSectorId.CanyonBasin, language),
        Evaluate(PrologueObjective.ClimbCanyon, PrologueSectorId.CanyonAscent, language),
        Evaluate(PrologueObjective.EnterTemple, PrologueSectorId.LostTemple, language),
        Evaluate(PrologueObjective.PullTempleMechanism, PrologueSectorId.LostTemple, language),
        Evaluate(PrologueObjective.DefeatBellkeeper, PrologueSectorId.LostTemple, language)
    ];

    private static PrologueGuidanceInstruction Evaluate(PrologueObjective objective,
        PrologueSectorId sector, GameLanguage language) => PrologueGuidanceModel.Evaluate(new(
            objective, sector, PrologueBossGuidanceState.None, 0, language, true), Glyphs);

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
