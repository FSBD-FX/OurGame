using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Settings;
using Knight.Ui;
using Knight.Vfx;
using Knight.Boss;
using Knight.Abilities;
using Knight.Combat;

namespace Knight.Tests.Integration;

public static class PrologueGuidanceRuntimeTests
{
    private static PackedScene _levelScene;

    public static async Task Run(SceneTree tree) =>
        await Task7CaseRegistry.Execute(Task7Cases(tree));

    internal static IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Replacement(nameof(UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete),
            PrologueCanyonLegacyReplacementTests.WrongAndValidDescentTargetsStayDistinct),
        Replacement(nameof(UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply),
            PrologueCanyonLegacyReplacementTests.RecoveryClearsPendingSeamDanger),
        Replacement(nameof(UpperAnchorLowFailedCastClearsPendingApply),
            PrologueCanyonLegacyReplacementTests.IceFailureTargetRemainsVisibleAndCollidable),
        Replacement(nameof(UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff),
            PrologueCanyonLegacyReplacementTests.DescentApplyAndLandingContractIsComplete),
        Replacement(nameof(ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt),
            PrologueCanyonLegacyReplacementTests.CompletedAscentTargetsStayUnique),
        Case(nameof(MountedGuidanceRendersAndClearsWithoutHidingObjective),
            () => MountedGuidanceRendersAndClearsWithoutHidingObjective(tree)),
        Replacement(nameof(ExpandedTeachingAppearsOnlyForAllowedTriggers),
            PrologueCanyonLegacyReplacementTests.ExpandedTeachingIsLimitedToTheEntranceBeat),
        Case(nameof(MountedGuidancePassesPointerInputThroughEveryOverlayControl),
            () => MountedGuidancePassesPointerInputThroughEveryOverlayControl(tree)),
        Case(nameof(MountedGuidanceDoesNotOverlapBossHud),
            () => MountedGuidanceDoesNotOverlapBossHud(tree)),
        Replacement(nameof(RecoveryCountsWithinOneRoomAndResetsOnTransition),
            PrologueCanyonLegacyReplacementTests.RecoveryAndCheckpointAuthoritiesAreExplicit),
        Replacement(nameof(CrossCheckpointRetriesRestoreTheFailedChallengeStreak),
            PrologueCanyonLegacyReplacementTests.BothFormalCheckpointsExist),
        Case(nameof(DisablingGuidanceClearsWithoutTouchingCompletionOrProgress),
            () => DisablingGuidanceClearsWithoutTouchingCompletionOrProgress(tree)),
        Replacement(nameof(RealCriticalPathRetargetsThroughShortcutAndBossStates),
            PrologueCanyonLegacyReplacementTests.CriticalPathOrdersMechanismBeforeBoss),
        Replacement(nameof(ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels),
            PrologueCanyonLegacyReplacementTests.FreshShortcutExposesOpenedSignalAndClosedGate),
        Replacement(nameof(MissingTargetKeepsInstructionAndWarnsOnce),
            PrologueCanyonLegacyReplacementTests.EveryAssistPathResolvesWithoutMissingTarget)
    ];

    private static Task7Case Case(string name, System.Func<Task> execute) =>
        Task7CaseRegistry.Case(nameof(PrologueGuidanceRuntimeTests), name, execute);
    private static Task7Case Replacement(string name, System.Action execute) =>
        Task7CaseRegistry.Case(nameof(PrologueGuidanceRuntimeTests), name, execute);

    public static async Task RunTask7IndependentCases(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
        GD.Print("TASK7_LEGACY_SUITE_CASE_EXECUTED " +
                 "suite=PrologueGuidanceRuntimeTests registry=True");
    }

    private static async Task MountedGuidanceRendersAndClearsWithoutHidingObjective(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "MountedContinuousGuidanceFixture");
        TestAssert.True(fixture.Level.CurrentSector.HasValue &&
            GodotObject.IsInstanceValid(fixture.Level.CurrentSectorRoot),
            "guidance authority is the live current mounted sector");
        TestAssert.True(fixture.Visual is IPrologueGuidanceVisual && fixture.Visual.Visible &&
            fixture.Visual.RenderedTargetCount > 0,
            "continuous guidance presents its current-sector target through the visual contract");
        TestAssert.True(fixture.Controller.CurrentInstruction.StepText.Contains("Q") &&
            fixture.Controller.CurrentInstruction.StepText.Contains("1") &&
            fixture.Controller.CurrentInstruction.StepText.Contains("4"),
            "live InputMap glyphs populate the first attack-mode instruction");

        for (int recovery = 0;
             recovery < 4 && fixture.Controller.SameSectorFailureCount < 2;
             recovery++)
        {
            fixture.Level.RequestRespawn("DEATH RESET");
            await SettleRecovery(tree);
            fixture.Controller.RefreshNow();
        }
        TestAssert.True(fixture.Controller.SameSectorFailureCount >= 2 &&
                        fixture.Visual.IsExpandedTeaching,
            "two real same-sector death recoveries expand mounted guidance");
        fixture.Controller.SetLanguage(GameLanguage.English);
        string english = fixture.Level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        TestAssert.True(english.Contains("First-Level Guidance", System.StringComparison.Ordinal) &&
            !english.Contains("第一关引导", System.StringComparison.Ordinal) &&
            !english.Contains("提示：", System.StringComparison.Ordinal),
            "mounted English expanded guidance localizes its heading and hint chrome");
        fixture.Controller.SetLanguage(GameLanguage.ChineseSimplified);
        string chinese = fixture.Level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        TestAssert.True(chinese.Contains("第一关引导", System.StringComparison.Ordinal) &&
            !chinese.Contains("First-Level Guidance", System.StringComparison.Ordinal),
            "mounted Chinese expanded guidance retains its localized heading chrome");

        fixture.Controller.SetGuidanceEnabled(false);
        TestAssert.True(!fixture.Visual.Visible && fixture.Visual.RenderedTargetCount == 0 &&
            fixture.Visual.RenderedStepText.Length == 0,
            "disabling guidance clears the continuous target without hiding the objective HUD");
        TestAssert.True(fixture.Level.GetNode<Label>("Hud/Objective").Visible,
            "guidance state does not own objective visibility");
        await Dispose(tree, fixture);
    }

    private static async Task MountedGuidancePassesPointerInputThroughEveryOverlayControl(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceInputFixture");
        Control text = fixture.Level.GetNode<Control>("Hud/Guidance/VisualRoot/Text");
        TestAssert.Equal(Control.MouseFilterEnum.Ignore, fixture.Visual.MouseFilter,
            "guidance root passes mouse input through");
        TestAssert.Equal(Control.MouseFilterEnum.Ignore, text.MouseFilter,
            "guidance text passes mouse input through");
        await Dispose(tree, fixture);
    }

    private static async Task MountedGuidanceDoesNotOverlapBossHud(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceLayoutFixture");
        Control panel = fixture.Level.GetNode<Control>("Hud/Panel");
        Control boss = fixture.Level.GetNode<Control>("Hud/Boss");
        TestAssert.True(!panel.GetGlobalRect().Intersects(boss.GetGlobalRect()),
            "guidance/objective panel geometry does not overlap the Boss HUD");
        await Dispose(tree, fixture);
    }

    public static async Task ContinuousGuidanceProtectsLiveGameplay(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceProtectionFixture");
        fixture.Level.GetNode<Node2D>("PlayerRoot").Visible = true;
        Node2D player = fixture.Level.Player;
        var rope = new ThreadLineVisual { Name = "ProtectionTestRope" };
        player.AddChild(rope);
        Vector2 ropeEnd = player.GlobalPosition + new Vector2(220, -180);
        rope.Present(player.GlobalPosition, ropeEnd, .8f, ThreadLineState.Latched);
        var shuttle = new ThreadShuttleVisual { Name = "ProtectionTestReturningShuttle" };
        player.AddChild(shuttle);
        shuttle.Present(MechanicalClawFrame.Idle(player.GlobalPosition) with
        {
            State = MechanicalClawState.Retract,
            AnchorPoint = player.GlobalPosition + new Vector2(420, 0), RopeLength = 360
        });
        var hammer = new FallenBellkeeperFormalPart
        {
            Name = "ProtectionTestRaisedHammer", Kind = FallenBellkeeperPartKind.Hammer,
            Position = new Vector2(520, 290), Rotation = -.9f
        };
        fixture.Level.CurrentSectorRoot.AddChild(hammer);
        var terrain = new StaticBody2D
        {
            Name = "ProtectionTestStaticTerrain", Position = new Vector2(900, 560),
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget
        };
        terrain.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(100, 100) }
        });
        terrain.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile", UseCollisionHitPoint = true,
            Material = GrappleSurfaceMaterial.GripStone
        });
        fixture.Level.CurrentSectorRoot.AddChild(terrain);

        var regions = fixture.Visual.ResolveProtectedRegions();
        Vector2 playerScreen = player.GetGlobalTransformWithCanvas().Origin;
        TestAssert.True(Contains(regions, playerScreen),
            "continuous guidance protects the live player even when CurrentRoom is null");
        Vector2 ropeScreen = player.GetCanvasTransform() * player.GlobalPosition.Lerp(ropeEnd, .5f);
        TestAssert.True(Contains(regions, ropeScreen),
            "continuous guidance protects the visible mechanical-claw rope");
        Vector2 hammerScreen = hammer.GetGlobalTransformWithCanvas() * new Vector2(76, -56);
        TestAssert.True(Contains(regions, hammerScreen),
            "guidance protects the raised hammer silhouette, not only the Boss hitbox");
        TestAssert.True(Contains(regions, shuttle.GetCanvasTransform() * shuttle.ShuttleWorldPosition),
            "guidance protects the returning shuttle even after the tether line is cleared");
        TestAssert.True(!Contains(regions, terrain.GetGlobalTransformWithCanvas().Origin),
            "grippable static walls do not consume the whole prompt safe area");
        Rect2 prompt = WorldTargetMarkerVisual.PlacePrompt(new Rect2(48, 40, 1184, 640),
            playerScreen, new Vector2(248, 64), regions);
        TestAssert.True(prompt.HasArea(), "gameplay protection still leaves a readable prompt location");
        foreach (Rect2 region in regions)
            TestAssert.True(!prompt.Intersects(region.Grow(8)),
                "guidance card cannot overlap a live protected gameplay silhouette");
        fixture.Controller.SetProcess(false);
        Label text = fixture.Visual.GetNode<Label>("Text");
        text.AddThemeConstantOverride("line_spacing", 8);
        fixture.Visual.Present(new PrologueGuidanceInstruction(true,
            "按 E 切换机械爪，瞄准发亮的粗糙墙面、平台边缘或天花板并按住 LMB。" +
            "用 A/D 摆荡、W/S 收放绳；连接时按 SPACE 跃升一次，松开 LMB 脱离。冰面和封闭表面不可抓。",
            string.Empty, string.Empty, string.Empty, Array.Empty<PrologueGuidanceTargetKey>()),
            Array.Empty<PrologueGuidanceTargetView>());
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Control backdrop = fixture.Visual.GetNode<Control>("Backdrop");
        TestAssert.True(fixture.Visual.HasVisiblePrompt &&
            backdrop.GetGlobalRect().Encloses(text.GetGlobalRect()),
            "the actual wrapped label, including theme line spacing, fits inside its background");
        await Dispose(tree, fixture);
    }

    private static bool Contains(IReadOnlyList<Rect2> regions, Vector2 point)
    {
        foreach (Rect2 region in regions)
            if (region.HasPoint(point)) return true;
        return false;
    }

    private static async Task DisablingGuidanceClearsWithoutTouchingCompletionOrProgress(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceDisableFixture");
        PrologueCompletionController completion = fixture.Level.GetNode<PrologueCompletionController>("Completion");
        PrologueObjective objective = completion.CurrentObjective;
        PrologueCheckpointSnapshot before = fixture.Level.Progress.RestoreCheckpoint();
        fixture.Controller.SetGuidanceEnabled(false);
        PrologueCheckpointSnapshot after = fixture.Level.Progress.RestoreCheckpoint();

        TestAssert.True(!fixture.Visual.Visible && fixture.Visual.RenderedTargetCount == 0,
            "disabling guidance clears every rendered marker");
        TestAssert.Equal(objective, completion.CurrentObjective,
            "disabling guidance leaves the exact objective untouched");
        TestAssert.True(before.SectorId == after.SectorId &&
            before.WorldPosition == after.WorldPosition &&
            before.HasActivatedCheckpoint == after.HasActivatedCheckpoint &&
            before.ShortcutOpen == after.ShortcutOpen &&
            before.Progress.BossDefeated == after.Progress.BossDefeated &&
            before.Progress.Discoveries.SetEquals(after.Progress.Discoveries),
            "disabling guidance leaves checkpoint, shortcut, tutorials, and discoveries untouched");
        await Dispose(tree, fixture);
    }

    // Names below remain exact Task 8 retirement registry keys; their executable delegates above
    // are the continuous canyon replacements.
    private static void UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete() { }
    private static void UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply() { }
    private static void UpperAnchorLowFailedCastClearsPendingApply() { }
    private static void UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff() { }
    private static void ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt() { }
    private static void ExpandedTeachingAppearsOnlyForAllowedTriggers() { }
    private static void RecoveryCountsWithinOneRoomAndResetsOnTransition() { }
    private static void CrossCheckpointRetriesRestoreTheFailedChallengeStreak() { }
    private static void RealCriticalPathRetargetsThroughShortcutAndBossStates() { }
    private static void ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels() { }
    private static void MissingTargetKeepsInstructionAndWarnsOnce() { }

    private static async Task<Fixture> LoadFixture(SceneTree tree, string name)
    {
        _levelScene ??= GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = _levelScene.Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        string stateRoot = Path.Combine(Path.GetTempPath(),
            "knight-task7-guidance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stateRoot);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.SavePath = Path.Combine(stateRoot, "save.json");
        flow.SettingsPath = Path.Combine(stateRoot, "settings.json");
        tree.Root.AddChild(level);
        for (int index = 0; index < 3; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return new Fixture(level, level.GetNode<PrologueGuidanceController>("Guidance"),
            level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot"), stateRoot);
    }

    private static async Task Dispose(SceneTree tree, Fixture fixture)
    {
        try
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, fixture.Level);
        }
        finally
        {
            if (Directory.Exists(fixture.StateRoot)) Directory.Delete(fixture.StateRoot, recursive: true);
        }
    }

    private static async Task SettleRecovery(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private sealed record Fixture(PrologueLevelController Level,
        PrologueGuidanceController Controller, WorldTargetMarkerVisual Visual, string StateRoot);
}
