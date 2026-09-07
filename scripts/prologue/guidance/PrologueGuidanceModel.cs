using System;
using System.Collections.Generic;
using Godot;
using Knight.Localization;
using Knight.Shared;

namespace Knight.Prologue.Guidance;

public static class PrologueGuidanceModel
{
    public static PrologueGuidanceInstruction Evaluate(PrologueGuidanceContext context,
        IReadOnlyDictionary<StringName, string> glyphs)
    {
        if (!context.GuidanceEnabled || context.Objective == PrologueObjective.Complete)
            return PrologueGuidanceInstruction.Hidden;
        if (glyphs == null) throw new ArgumentNullException(nameof(glyphs));

        (string key, PrologueGuidanceTargetKey[] targets) = context.Objective switch
        {
            PrologueObjective.Wake => ("guidance.wake", [PrologueGuidanceTargetKey.EntranceCombat]),
            PrologueObjective.LearnAttackMode => ("guidance.attack",
                [PrologueGuidanceTargetKey.EntranceCombat]),
            PrologueObjective.LearnGrappleMode => ("guidance.grapple",
                context.Sector == Prologue.Seamless.PrologueSectorId.CanyonEntrance
                    ? [PrologueGuidanceTargetKey.EntranceCombat]
                    : [PrologueGuidanceTargetKey.DescentGripStone,
                        PrologueGuidanceTargetKey.DescentIce]),
            PrologueObjective.ReachBasin => ("guidance.reach_basin",
                [PrologueGuidanceTargetKey.DescentIce]),
            PrologueObjective.LearnShield => ("guidance.shield",
                [PrologueGuidanceTargetKey.BasinShieldLane, PrologueGuidanceTargetKey.BasinCheckpoint]),
            PrologueObjective.ClimbCanyon => ("guidance.climb",
                context.Sector == Prologue.Seamless.PrologueSectorId.CanyonBasin
                    ? [PrologueGuidanceTargetKey.BasinCheckpoint]
                    : [PrologueGuidanceTargetKey.AscentAnchorOne]),
            PrologueObjective.EnterTemple => ("guidance.enter_temple",
                context.Sector == Prologue.Seamless.PrologueSectorId.CanyonAscent
                    ? [PrologueGuidanceTargetKey.AscentAnchorOne]
                    : [PrologueGuidanceTargetKey.TempleEntrance]),
            PrologueObjective.PullTempleMechanism => ("guidance.mechanism",
                [PrologueGuidanceTargetKey.TempleMechanism, PrologueGuidanceTargetKey.TempleCheckpoint]),
            PrologueObjective.DefeatBellkeeper => Boss(context.BossState),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.Objective,
                "Unknown first-level objective.")
        };

        string text = ResolveGlyphs(PrologueStrings.Get(key, context.Language), glyphs);
        string hint = context.SameSectorFailureCount >= 2
            ? ResolveGlyphs(PrologueStrings.Get(HintKey(context.Sector), context.Language), glyphs)
            : string.Empty;
        return new PrologueGuidanceInstruction(true, text, hint,
            PrologueStrings.Get("guidance.chrome.heading", context.Language),
            PrologueStrings.Get("guidance.chrome.hint", context.Language), targets);
    }

    private static (string, PrologueGuidanceTargetKey[]) Boss(PrologueBossGuidanceState state) => state switch
    {
        PrologueBossGuidanceState.ChainExposed => ("guidance.boss_chain",
            [PrologueGuidanceTargetKey.BossChain]),
        PrologueBossGuidanceState.Active => ("guidance.boss",
            [PrologueGuidanceTargetKey.BossBody]),
        _ => ("guidance.boss_enter", [PrologueGuidanceTargetKey.BossActivation])
    };

    private static string HintKey(Prologue.Seamless.PrologueSectorId sector) => sector switch
    {
        Prologue.Seamless.PrologueSectorId.CanyonEntrance => "guidance.hint.entrance",
        Prologue.Seamless.PrologueSectorId.CanyonDescent => "guidance.hint.descent",
        Prologue.Seamless.PrologueSectorId.CanyonBasin => "guidance.hint.basin",
        Prologue.Seamless.PrologueSectorId.CanyonAscent => "guidance.hint.ascent",
        Prologue.Seamless.PrologueSectorId.LostTemple => "guidance.hint.temple",
        _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, null)
    };

    private static string ResolveGlyphs(string template, IReadOnlyDictionary<StringName, string> glyphs)
    {
        string resolved = template;
        foreach ((StringName action, string token) in Tokens)
        {
            if (!glyphs.TryGetValue(action, out string glyph) || string.IsNullOrWhiteSpace(glyph))
                throw new InvalidOperationException($"Guidance glyph for action '{action}' is unavailable.");
            resolved = resolved.Replace("{" + token + "}", glyph, StringComparison.Ordinal);
        }
        return resolved;
    }

    private static readonly (StringName Action, string Token)[] Tokens =
    [
        (InputActions.MoveLeft, "move_left"), (InputActions.MoveRight, "move_right"),
        (InputActions.MoveUp, "move_up"), (InputActions.MoveDown, "move_down"),
        (InputActions.Jump, "jump"),
        (InputActions.SelectAttackMode, "attack_mode"),
        (InputActions.SelectGrappleMode, "grapple_mode"),
        (InputActions.PrimaryAction, "primary"), (InputActions.Shield, "shield"),
        (InputActions.Slot1, "slot_1"), (InputActions.Slot2, "slot_2"),
        (InputActions.Slot3, "slot_3"), (InputActions.Slot4, "slot_4")
    ];
}
