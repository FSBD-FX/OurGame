using Godot;
using System.Linq;
using Knight.Abilities;
using Knight.Player;
using Knight.Ui;

namespace Knight.Tests.Unit;

public static class PrologueHudViewModelTests
{
    public static void Run()
    {
        Task10ImmutableVisualFramesClampAtBoundary();
        Task10ReticleStatesHaveIndependentGeometryAndColor();
        HealthAndLegacyAdapterStateIsNonPresentational();
        ActionModeAndResourceStateIsExplicit();
        LegacyThreadCastStateNeverProducesPlayerFacingText();
        BossAndCompletionStateIsExact();
        HealthOwnsTheTopLeftInstrumentCluster();
        ActionRouterLayoutAvoidsExistingRibbonsAt1280x720();
        ObjectiveDefaultsToOneCollapsedLine();
        ObjectiveTransitionTimeControlsBriefExpansion();
        BossBarLayoutAuditRejectsCombatFloorOverlap();
        BossBarUsesTheUpperSafeArea();
        LayoutHonorsSupportedUiScale();
        ProtectedPromptPlacerTests.Run();
        FourResolutionLayoutStaysSafe();
    }

    public static void Task10ImmutableVisualFramesClampAtBoundary()
    {
        PrologueEnergyHeatFrame resources = PrologueEnergyHeatFrame.Create(-.25f, 1.42f, true);
        TestAssert.Near(0f, resources.EnergyRatio, .0001f,
            "Task10 immutable energy frame clamps ratios below zero");
        TestAssert.Near(1f, resources.HeatRatio, .0001f,
            "Task10 immutable heat frame clamps heat above one hundred percent");
        PrologueLoadoutFrame loadout = PrologueLoadoutFrame.Create(8, true, false, false, false,
            PlayerActionRejectionReason.EmptySlot);
        TestAssert.Equal(4, loadout.ActiveSlot,
            "Task10 immutable loadout frame clamps active slot to the four-slot boundary");
    }

    public static void Task10ReticleStatesHaveIndependentGeometryAndColor()
    {
        var geometries = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        var colors = new System.Collections.Generic.HashSet<uint>();
        var sounds = new System.Collections.Generic.HashSet<Knight.Audio.PrologueAudioCue>();
        foreach (PrologueAimReticleState state in System.Enum.GetValues<PrologueAimReticleState>().Distinct())
        {
            PrologueAimReticleStyle style = PrologueAimReticle.StyleFor(state);
            TestAssert.True(geometries.Add(style.GeometrySignature),
                $"Task10 reticle state {state} has a unique geometry/line-style signature");
            TestAssert.True(colors.Add(style.Color.ToRgba32()),
                $"Task10 reticle state {state} has a unique color channel");
            TestAssert.True(sounds.Add(style.AudioCue),
                $"Task4 reticle state {state} has a unique deterministic sound cue");
        }
        TestAssert.Equal(6, geometries.Count,
            "Task10 reticle exposes exactly six shape-plus-color states");
    }

    private static void HealthOwnsTheTopLeftInstrumentCluster()
    {
        PrologueHudLayout layout = PrologueHudLayout.Calculate(new Vector2(1280, 720));
        TestAssert.Equal(Vector2.Zero, layout.ThreadRect.Size,
            "Task10 legacy thread adapter owns no player-facing HUD region");
        TestAssert.True(layout.HealthRect.Size.X <= 220f && layout.HealthRect.Size.Y <= 72f,
            "health alone owns the compact top-left instrument cluster");
    }

    private static void ActionRouterLayoutAvoidsExistingRibbonsAt1280x720()
    {
        PrologueHudLayout layout = PrologueHudLayout.Calculate(new Vector2(1280, 720));
        TestAssert.True(!layout.ModeSelectorRect.Intersects(layout.HealthRect) &&
                        !layout.ModeSelectorRect.Intersects(layout.ThreadRect) &&
                        !layout.ModeSelectorRect.Intersects(layout.ObjectiveRect),
            "mode selector has a dedicated 1280-by-720 region without health or objective overlap");
        TestAssert.True(!layout.LoadoutRect.Intersects(layout.HealthRect) &&
                        !layout.LoadoutRect.Intersects(layout.ObjectiveRect),
            "four-slot loadout has a dedicated 1280-by-720 region without health or objective overlap");
        TestAssert.True(!layout.ResourceClusterRect.Intersects(layout.ModeSelectorRect) &&
                        !layout.ResourceClusterRect.Intersects(layout.PromptRect),
            "energy and heat meters do not overlap the mode selector or contextual prompt at 1280 by 720");
    }

    private static void ObjectiveDefaultsToOneCollapsedLine()
    {
        PrologueHudLayout layout = PrologueHudLayout.Calculate(new Vector2(1280, 720));
        TestAssert.True(layout.ObjectiveRect.Position.X == layout.HealthRect.Position.X &&
                        layout.ObjectiveRect.Position.Y >= layout.HealthRect.End.Y + 6f,
            "collapsed objective sits directly below the compact instrument cluster");
        TestAssert.True(layout.ObjectiveRect.Size.X <= 426f && layout.ObjectiveRect.Size.Y <= 36f,
            "ordinary objective occupies one compact line instead of a large fixed block");
    }

    private static void BossBarUsesTheUpperSafeArea()
    {
        Vector2 viewport = new(1280, 720);
        PrologueHudLayout layout = PrologueHudLayout.Calculate(viewport);
        TestAssert.True(layout.BossRect.Size.X <= 680f && layout.BossRect.Size.Y <= 64f,
            "Boss HUD remains a compact bar instead of covering the combat floor");
        TestAssert.True(layout.BossRect.Position.Y >= layout.ObjectiveRect.End.Y + 8f &&
                        layout.BossRect.End.Y <= viewport.Y * .36f,
            "Boss HUD occupies the upper safe area below the objective instead of hiding lower combat telegraphs");
        TestAssert.True(layout.BossRect.GetCenter().X == viewport.X * .5f,
            "Boss HUD stays centered in the upper safe area");
    }

    private static void BossBarLayoutAuditRejectsCombatFloorOverlap()
    {
        PrologueHudLayout layout = PrologueHudLayout.Calculate(new Vector2(1280, 720));
        var visibleArenaFloor = new Rect2(0, 630, 1280, 40);
        var lowerCombatTelegraphs = new Rect2(250, 420, 780, 210);
        TestAssert.True(layout.BossAvoidsProtectedRegions([visibleArenaFloor, lowerCombatTelegraphs]),
            "Boss safe-area audit keeps the production bar clear of the floor and the lower attack-telegraph band");
        PrologueHudLayout expandedIntoFloor = layout with
        {
            BossRect = new Rect2(layout.BossRect.Position,
                new Vector2(layout.BossRect.Size.X,
                    visibleArenaFloor.Position.Y - layout.BossRect.Position.Y + 1f))
        };
        TestAssert.True(!expandedIntoFloor.BossAvoidsProtectedRegions([visibleArenaFloor]),
            "Boss safe-area audit rejects a downward expansion into the arena floor");
    }

    private static void ObjectiveTransitionTimeControlsBriefExpansion()
    {
        PrologueHudViewModel initial = PrologueHudViewModel.Gameplay(5, 5, 1f,
            "点亮墓灯", string.Empty, string.Empty);
        PrologueHudViewModel changed = initial.WithObjective("打开钟链", 0f);
        TestAssert.True(changed.ObjectiveExpanded && changed.ObjectiveTransitionSeconds == 0f,
            "objective change starts the brief expanded transition state");
        PrologueHudViewModel settled = changed.AdvanceObjectiveTransition(2.5f);
        TestAssert.True(!settled.ObjectiveExpanded && settled.ObjectiveTransitionSeconds == 2.5f,
            "objective automatically collapses to one line after the transition window");
    }

    private static void LayoutHonorsSupportedUiScale()
    {
        Vector2 viewport = new(1280, 720);
        PrologueHudLayout normal = PrologueHudLayout.Calculate(viewport, 1f);
        PrologueHudLayout large = PrologueHudLayout.Calculate(viewport, 1.5f);
        Rect2 safe = new(new Vector2(48, 40), viewport - new Vector2(96, 80));
        TestAssert.Equal(normal.HealthRect.Size * 1.5f, large.HealthRect.Size,
            "supported UI scale proportionally enlarges the instrument cluster");
        TestAssert.True(safe.Encloses(large.HealthRect) && safe.Encloses(large.BossRect),
            "supported UI scale keeps critical HUD elements inside the safe area");
    }

    private static void HealthAndLegacyAdapterStateIsNonPresentational()
    {
        PrologueHudViewModel model = PrologueHudViewModel.Gameplay(3, 5, 1f,
            "点亮墓灯", "[E + LMB]", "牵动机关");
        TestAssert.Equal(5, model.HealthSegments.Count, "five formal health segments");
        TestAssert.Equal(HealthShardState.Filled, model.HealthSegments[2], "remaining health is filled");
        TestAssert.Equal(HealthShardState.Broken, model.HealthSegments[3], "newly lost health is broken");
        TestAssert.Equal(HealthShardState.Empty, model.HealthSegments[4], "older lost health is empty");
        TestAssert.Equal(1f, model.ThreadReadyRatio, "ready thread spool is full");
        TestAssert.Equal(ThreadCastState.Idle, model.ThreadState, "ready thread state is explicit");
        TestAssert.Equal(ThreadFailureReason.None, model.ThreadFailure,
            "ready thread failure is explicitly clear");
        TestAssert.Equal(string.Empty, model.ThreadStateText,
            "legacy thread adapter state never creates player-facing cooldown text");
        TestAssert.Equal("[E + LMB]", model.PromptGlyph,
            "current grapple-mode and primary glyphs remain separate from prompt text");
    }

    private static void ActionModeAndResourceStateIsExplicit()
    {
        PrologueHudViewModel model = PrologueHudViewModel.Gameplay(5, 5, 1f, "", "", "")
            .WithActionState(PlayerActionMode.Grapple, 4, .42f, .86f, true,
                PlayerActionRejectionReason.EmptySlot);
        TestAssert.Equal(PlayerActionMode.Grapple, model.ActionMode,
            "HUD view model carries the sticky selected action mode");
        TestAssert.Equal(4, model.ActiveSlot,
            "HUD view model carries the selected empty loadout slot");
        TestAssert.Near(.42f, model.EnergyRatio, .0001f,
            "HUD view model preserves the authoritative energy ratio");
        TestAssert.Near(.86f, model.HeatRatio, .0001f,
            "HUD view model preserves the authoritative heat ratio");
        TestAssert.True(model.IsOverheated, "HUD view model carries explicit overheat state");
        TestAssert.Equal(PlayerActionRejectionReason.EmptySlot, model.ActionRejection,
            "HUD view model carries the named primary rejection state");
    }

    private static void LegacyThreadCastStateNeverProducesPlayerFacingText()
    {
        PrologueHudViewModel ready = PrologueHudViewModel.Gameplay(5, 5, 1f, "", "", "");
        (ThreadCastState State, ThreadFailureReason Failure, float Ratio)[] cases =
        [
            (ThreadCastState.Idle, ThreadFailureReason.None, .4f),
            (ThreadCastState.Preview, ThreadFailureReason.None, 1f),
            (ThreadCastState.Travel, ThreadFailureReason.None, .7f),
            (ThreadCastState.Recovery, ThreadFailureReason.None, .3f),
            (ThreadCastState.Failed, ThreadFailureReason.Cooldown, .2f),
            (ThreadCastState.Failed, ThreadFailureReason.Obstructed, 1f)
        ];

        foreach ((ThreadCastState state, ThreadFailureReason failure, float ratio) in cases)
        {
            PrologueHudViewModel actual = ready.WithThread(ratio).WithThreadCast(state, failure);
            TestAssert.Equal(state, actual.ThreadState, $"HUD retains {state} state");
            TestAssert.Equal(failure, actual.ThreadFailure, $"HUD retains {failure} failure");
            TestAssert.Equal(string.Empty, actual.ThreadStateText,
                $"HUD keeps adapter-only {state}/{failure} out of player-facing text");
        }
    }

    private static void BossAndCompletionStateIsExact()
    {
        PrologueHudViewModel boss = PrologueHudViewModel.Gameplay(5, 5, .35f, "击败钟守", "", "")
            .WithBoss("坠落钟守", 9, 18, 2, armored: true, exposed: false);
        TestAssert.True(boss.BossVisible && boss.BossArmored, "phase-two armored Boss is explicit");
        TestAssert.True(!boss.BossExposed, "armored Boss is not exposed");

        PrologueHudViewModel complete = boss.WithCompletion("00:12:34", 4, hiddenFound: true,
            shortcutOpen: true);
        TestAssert.True(complete.CompletionVisible, "completion card is visible");
        TestAssert.Equal("00:12:34", complete.CompletionTime, "completion time is exact");
        TestAssert.Equal(4, complete.Deaths, "completion deaths are exact");
        TestAssert.True(complete.HiddenFound && complete.ShortcutOpen,
            "completion discovery and shortcut statistics are retained");
    }

    private static void FourResolutionLayoutStaysSafe()
    {
        Vector2[] resolutions =
        [
            new(1280, 720), new(1920, 1080), new(2560, 1440), new(3840, 2160)
        ];
        foreach (Vector2 resolution in resolutions)
        {
            PrologueHudLayout layout = PrologueHudLayout.Calculate(resolution);
            Rect2 safe = new(new Vector2(48, 40), resolution - new Vector2(96, 80));
            TestAssert.True(safe.Encloses(layout.HealthRect), $"health stays safe at {resolution}");
            TestAssert.True(safe.Encloses(layout.GuidanceRect), $"guidance stays safe at {resolution}");
            TestAssert.True(safe.Encloses(layout.BossRect), $"Boss stays safe at {resolution}");
            TestAssert.True(!layout.GuidanceRect.Intersects(layout.BossRect),
                $"guidance does not overlap Boss HUD at {resolution}");
            TestAssert.True(!layout.PromptRect.Intersects(layout.BossRect),
                $"prompt does not overlap Boss HUD at {resolution}");
            Rect2[] protectedRegions =
            [
                layout.HealthRect, layout.ObjectiveRect, layout.PromptRect, layout.ModeSelectorRect,
                layout.LoadoutRect, layout.ResourceClusterRect, layout.BossRect
            ];
            foreach (Rect2 region in protectedRegions)
                TestAssert.True(!layout.GuidanceRect.Intersects(region),
                    $"Task10 guidance protected region avoids {region} at {resolution}");
        }
    }
}
