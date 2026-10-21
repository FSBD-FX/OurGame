using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Knight.Abilities;
using Knight.Localization;
using Knight.Prologue.Collections;
using Knight.Settings;
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
            PrologueObjective.ReachBasin when context.Sector == Prologue.Seamless.PrologueSectorId.CanyonEntrance =>
                ("guidance.exit_entrance", [PrologueGuidanceTargetKey.EntranceExit]),
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
            PrologueObjective.DefeatBellkeeper => Boss(context.BossState, context.BossAdmission),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.Objective,
                "Unknown first-level objective.")
        };

        if (context.AscentTransferOpportunity &&
            context.Objective is (PrologueObjective.ClimbCanyon or PrologueObjective.EnterTemple) &&
            context.Sector == Prologue.Seamless.PrologueSectorId.CanyonAscent &&
            context.BossState is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            context.ClawState == MechanicalClawState.Idle)
        {
            key = "guidance.ascent_transfer";
            targets = Array.Empty<PrologueGuidanceTargetKey>();
        }

        if (context.DescentClawOpportunity && context.Objective == PrologueObjective.ReachBasin &&
            context.Sector == Prologue.Seamless.PrologueSectorId.CanyonDescent &&
            context.BossState is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            context.ClawState is (MechanicalClawState.Idle or MechanicalClawState.Aim or
                MechanicalClawState.Windup or MechanicalClawState.Travel))
        {
            key = "guidance.descent_first_claw";
            targets = [PrologueGuidanceTargetKey.DescentClawOpportunity];
        }

        if (context.Objective is (PrologueObjective.LearnGrappleMode or PrologueObjective.ReachBasin or
                PrologueObjective.ClimbCanyon or PrologueObjective.EnterTemple) &&
            context.ClawState is (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost))
        {
            key = "guidance.grapple_attached";
            if (context.Objective == PrologueObjective.ReachBasin &&
                context.Sector == Prologue.Seamless.PrologueSectorId.CanyonDescent)
                targets = [PrologueGuidanceTargetKey.ClawAttachment];
        }

        // An optional Basin climb temporarily teaches the live claw, not the pending shield lesson.
        if (context.Objective == PrologueObjective.LearnShield &&
            context.Sector == Prologue.Seamless.PrologueSectorId.CanyonBasin &&
            context.BossState is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            context.ClawState is (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost))
        {
            key = "guidance.grapple_attached";
            targets = [PrologueGuidanceTargetKey.ClawAttachment];
        }

        // Temple objective ownership resumes as soon as its optional real-terrain lock ends.
        if (context.TerrainClawAttached &&
            context.Sector == Prologue.Seamless.PrologueSectorId.LostTemple &&
            context.Objective is (PrologueObjective.PullTempleMechanism or PrologueObjective.DefeatBellkeeper) &&
            context.BossState is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            context.ClawState is (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost))
        {
            key = "guidance.grapple_attached";
            targets = [PrologueGuidanceTargetKey.ClawAttachment];
        }

        bool firstCombat = context.FirstCombatNearby &&
            context.Sector == Prologue.Seamless.PrologueSectorId.CanyonEntrance &&
            context.Objective is (PrologueObjective.Wake or PrologueObjective.LearnAttackMode or
                PrologueObjective.LearnGrappleMode or PrologueObjective.ReachBasin) &&
            context.BossState is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            context.ClawState == MechanicalClawState.Idle;
        if (firstCombat)
        {
            key = "guidance.first_combat";
            targets = Array.Empty<PrologueGuidanceTargetKey>();
        }

        // Immediate visible danger temporarily outranks an unfinished traversal
        // lesson, without completing it or exposing a target beyond the fog.
        bool shieldLane = context.ShieldLaneNearby &&
            context.Sector == Prologue.Seamless.PrologueSectorId.CanyonBasin &&
            context.Objective is (PrologueObjective.Wake or PrologueObjective.LearnAttackMode or
                PrologueObjective.LearnGrappleMode or PrologueObjective.ReachBasin or
                PrologueObjective.LearnShield or PrologueObjective.ClimbCanyon or PrologueObjective.EnterTemple) &&
            context.BossState is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            context.ClawState == MechanicalClawState.Idle;
        if (shieldLane)
        {
            key = "guidance.shield_lane";
            targets = Array.Empty<PrologueGuidanceTargetKey>();
        }

        string template = key == "guidance.boss_blocked"
            ? BlockedBossText(context.BossAdmission.Value, context.Language)
            : PrologueStrings.Get(key, context.Language);
        string text = ResolveGlyphs(template, glyphs);
        string hint = !firstCombat && !shieldLane && context.SameSectorFailureCount >= 2
            ? ResolveGlyphs(PrologueStrings.Get(HintKey(context.Sector), context.Language), glyphs)
            : string.Empty;
        return new PrologueGuidanceInstruction(true, text, hint,
            PrologueStrings.Get("guidance.chrome.heading", context.Language),
            PrologueStrings.Get("guidance.chrome.hint", context.Language), targets,
            firstCombat ? PrologueStrings.Get("guidance.first_combat_title", context.Language) :
            shieldLane ? PrologueStrings.Get("guidance.shield_lane_title", context.Language) : string.Empty);
    }

    private static (string, PrologueGuidanceTargetKey[]) Boss(PrologueBossGuidanceState state,
        BossAdmissionResult? admission) => state switch
    {
        PrologueBossGuidanceState.ChainExposed => ("guidance.boss_chain",
            [PrologueGuidanceTargetKey.BossChain]),
        PrologueBossGuidanceState.Active => ("guidance.boss",
            [PrologueGuidanceTargetKey.BossBody]),
        _ when admission is { Allowed: false } => ("guidance.boss_blocked",
            Array.Empty<PrologueGuidanceTargetKey>()),
        _ => ("guidance.boss_enter", [PrologueGuidanceTargetKey.BossActivation])
    };

    private static string BlockedBossText(BossAdmissionResult admission, GameLanguage language)
    {
        var requirements = new List<string> { PrologueStrings.Get("guidance.boss_blocked", language) };
        if (admission.MissingFragments > 0)
            requirements.Add(PrologueStrings.Get("guidance.boss_need_fragments", language)
                .Replace("{missing_fragments}", admission.MissingFragments.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal));
        if (!admission.CourtyardReady)
            requirements.Add(PrologueStrings.Get("guidance.boss_need_courtyard", language));
        if (!admission.BellChainReady)
            requirements.Add(PrologueStrings.Get("guidance.boss_need_bell_chain", language));
        return string.Join(" ", requirements);
    }

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
        (InputActions.Jump, "jump"), (InputActions.Dash, "dash"),
        (InputActions.Attack, "attack"), (InputActions.Claw, "claw"),
        (InputActions.KeyboardShield, "shield"),
        (InputActions.Slot1, "slot_1"), (InputActions.Slot2, "slot_2"),
        (InputActions.Slot3, "slot_3"), (InputActions.Slot4, "slot_4")
    ];
}
