using System;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Localization;
using Knight.Presentation;
using Knight.Settings;
using Knight.Ui;
using Knight.Vfx;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class PrologueAccessibilityTests
{
    public static async Task Task10ProductionEnglishFlowsToPause(SceneTree tree)
    {
        Vector2I priorViewportSize = tree.Root.Size;
        tree.Root.Size = new Vector2I(1280, 720);
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(Task10ProductionEnglishFlowsToPause));
        level.GetNode<PrologueAudioDirector>("Audio").PlaybackEnabled = false;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English });
        ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        pause.Open();
        pause.Execute(ProloguePauseCommand.ShowControls);
        string controls = level.GetNode<Label>("PauseMenu/Panel/Controls").Text;
        TestAssert.True(controls.Contains("ATTACK MODE", System.StringComparison.Ordinal) &&
                        !controls.Contains("攻击", System.StringComparison.Ordinal) &&
                        !controls.Contains("机械爪", System.StringComparison.Ordinal),
            "English selected through production GameFlow renders a coherent English pause overlay");

        TestAssert.True(level.GetNode<Label>("PauseMenu/Panel/Title").Text.Contains("PAUSED") &&
                        level.GetNode<Button>("PauseMenu/Panel/ReturnToCheckpoint").Text ==
                            "RETURN TO CHECKPOINT",
            "production English dispatch updates pause title and commands without mixed-language text");
        pause.Execute(ProloguePauseCommand.Resume);

        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.ChineseSimplified });
        pause.Open();
        pause.Execute(ProloguePauseCommand.ShowControls);
        TestAssert.True(level.GetNode<Label>("PauseMenu/Panel/Title").Text.Contains("暂停") &&
                        level.GetNode<Label>("PauseMenu/Panel/Controls").Text.Contains("攻击模式") &&
                        !level.GetNode<Label>("PauseMenu/Panel/Controls").Text.Contains("ATTACK MODE"),
            "the same production pause path renders coherent Chinese settings and controls");
        pause.Execute(ProloguePauseCommand.Resume);
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English });

        level.Player.GetNode<Knight.Player.PlayerMotor>("Motor").SetPhysicsProcess(false);
        PrologueSector sector = level.CurrentSectorRoot;
        var tutorial = new TutorialPromptTrigger
        {
            Name = "Task10LocaleTutorial", TutorialId = "attack", Text = "ATTACK",
            Position = sector.ToLocal(level.Player.GlobalPosition + Vector2.Right * 240f),
            CollisionLayer = 0,
            CollisionMask = 2
        };
        tutorial.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(120, 120) }
        });
        sector.AddChild(tutorial);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        level.Player.GlobalPosition = tutorial.GlobalPosition;
        await Frames(tree, 3);
        Label prompt = level.GetNode<Label>("Hud/FormalHud/Prompt/Text");
        TestAssert.Equal("SELECT ATTACK MODE AND STRIKE", prompt.Text,
            "a real continuous-sector BodyEntered prompt is English through GameFlow settings");

        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.ChineseSimplified });
        TestAssert.Equal("切换攻击模式并攻击", prompt.Text,
            "switching production settings re-renders the live prompt wholly in Chinese");
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English });

        var hidden = new HiddenAlcoveTrigger
        {
            Name = "Task10LocaleHiddenAlcove", DiscoveryId = "upper_hidden_alcove",
            Position = sector.ToLocal(level.Player.GlobalPosition + Vector2.Right * 260f),
            CollisionLayer = 0,
            CollisionMask = 2
        };
        hidden.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(120, 120) }
        });
        sector.AddChild(hidden);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        level.Player.GlobalPosition = hidden.GlobalPosition;
        await Frames(tree, 3);
        TestAssert.Equal("HIDDEN ALCOVE FOUND", prompt.Text,
            "a real hidden-alcove BodyEntered transition renders the English discovery overlay");
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.ChineseSimplified });
        TestAssert.Equal("隐藏壁龛已发现", prompt.Text,
            "the live hidden-alcove overlay re-renders wholly in Chinese through GameFlow settings");
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English });

        string recoveryTitle = string.Empty;
        string recoveryReason = string.Empty;
        void CaptureRecovery(string reason)
        {
            recoveryTitle = level.GetNode<Label>("Hud/FormalHud/Recovery/Title").Text;
            recoveryReason = level.GetNode<Label>("Hud/FormalHud/Recovery/Reason").Text;
        }
        level.RecoveryRequested += CaptureRecovery;
        level.RequestRespawn("DEATH RESET");
        level.RecoveryRequested -= CaptureRecovery;
        TestAssert.Equal("GRAVE LAMP RELIT", recoveryTitle,
            "real recovery request renders its English overlay title before relocation");
        TestAssert.Equal("FALLEN · CHECKPOINT RESTORED", recoveryReason,
            "real recovery request renders its English reason without Chinese fallback");
        await SettleRecovery(tree);

        PrologueGuidanceController guidance =
            level.GetNode<PrologueGuidanceController>("Guidance");
        WorldTargetMarkerVisual guidanceVisual =
            level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        for (int recovery = 0; recovery < 4 && guidance.SameSectorFailureCount < 2; recovery++)
        {
            level.RequestRespawn("DEATH RESET");
            await SettleRecovery(tree);
            guidance.RefreshNow();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        Rect2 logicalSafe = new(new Vector2(48, 40), new Vector2(1184, 640));
        if (guidance.SameSectorFailureCount < 2 || !guidanceVisual.IsExpandedTeaching)
            throw new InvalidOperationException(
                "two real same-sector death recoveries must expand production guidance without " +
                $"a harness bypass; failures={guidance.SameSectorFailureCount} " +
                $"expanded={guidanceVisual.IsExpandedTeaching} " +
                $"instructionVisible={guidance.CurrentInstruction.Visible}");
        TestAssert.True(guidanceVisual.RenderedTextContained &&
                        logicalSafe.Encloses(guidanceVisual.PromptRect),
            "real expanded English guidance text is measured and contained at exact 1280x720");
        TestAssert.True(guidanceVisual.GetNode<Label>("Text").Text
                            .Contains("FIRST-LEVEL GUIDANCE", StringComparison.OrdinalIgnoreCase),
            "real repeated-failure teaching remains wholly English through production settings");

        string chineseRecoveryTitle = string.Empty;
        string chineseRecoveryReason = string.Empty;
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.ChineseSimplified });
        void CaptureChineseRecovery(string reason)
        {
            chineseRecoveryTitle = level.GetNode<Label>("Hud/FormalHud/Recovery/Title").Text;
            chineseRecoveryReason = level.GetNode<Label>("Hud/FormalHud/Recovery/Reason").Text;
        }
        level.RecoveryRequested += CaptureChineseRecovery;
        level.RequestRespawn("DEATH RESET");
        level.RecoveryRequested -= CaptureChineseRecovery;
        TestAssert.Equal("墓灯重燃", chineseRecoveryTitle,
            "real recovery request renders its Chinese title through production settings");
        TestAssert.Equal("倒下 · 墓灯重燃", chineseRecoveryReason,
            "real recovery request renders its Chinese reason without English fallback");
        await SettleRecovery(tree);
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English });

        PrologueCompletionController completion = level.GetNode<PrologueCompletionController>("Completion");
        guidance.SetGuidanceEnabled(false);
        completion.RestoreSession(PrologueObjective.DefeatBellkeeper);
        level.RecordBossDefeated();
        await Frames(tree, 2);
        TestAssert.True(level.GetNode<Control>("Hud/FormalHud/Completion").Visible,
            "real objective completion shows the production completion overlay");
        TestAssert.True(level.GetNode<Label>("Hud/FormalHud/Completion/Panel/Title").Text
                            .Contains("PROLOGUE COMPLETE", StringComparison.Ordinal) &&
                        level.GetNode<Label>("Hud/FormalHud/Completion/Panel/Stats").Text
                            .Contains("HIDDEN ROUTE", StringComparison.Ordinal) &&
                        level.GetNode<Button>("Hud/FormalHud/Completion/Panel/Replay").Text == "PLAY AGAIN",
            "real completion remains wholly English through production settings dispatch");
        flow.ApplySettings(new GameSettingsData { Language = GameLanguage.ChineseSimplified });
        TestAssert.True(level.GetNode<Label>("Hud/FormalHud/Completion/Panel/Title").Text
                            .Contains("序章完成", StringComparison.Ordinal) &&
                        level.GetNode<Label>("Hud/FormalHud/Completion/Panel/Stats").Text
                            .Contains("隐藏路线", StringComparison.Ordinal),
            "visible completion re-renders wholly in Chinese through the same production dispatcher");

        tree.Paused = false;
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.Root.Size = priorViewportSize;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task SettleRecovery(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    public static async Task Run(SceneTree tree)
    {
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, nameof(PrologueAccessibilityTests));
        level.GetNode<PrologueAudioDirector>("Audio").PlaybackEnabled = false;
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        PrologueVfxDirector vfx = level.GetNode<PrologueVfxDirector>("Vfx");
        vfx.Configure(new GameSettingsData { ScreenShake = 0f, ReduceFlashes = true });
        VfxCueStyle reduced = vfx.ResolveStyle(PresentationCue.BossTransition);
        vfx.Play(PresentationCue.BossTransition, Vector2.Zero, Vector2.Right);
        CameraFeedbackController camera = vfx.GetNode<CameraFeedbackController>("CameraFeedback");
        TestAssert.Equal(0f, camera.LastApplied.Intensity,
            "zero screen-shake setting removes camera displacement");
        TestAssert.True(!reduced.FullScreenFlash && reduced.FlashOpacity == 0f,
            "reduced-flash setting removes full-screen flashes");

        PrologueUiRoot ui = level.GetNode<PrologueUiRoot>("ProductUi");
        ui.ShowMainMenu(false);
        TestAssert.Equal("开始新游戏", ui.GetNode<Button>("MainMenu/Panel/Buttons/Start").Text,
            "Chinese accessibility path is readable");
        ui.ShowSettings(new GameSettingsData { Language = GameLanguage.English, UiScale = 1.5f });
        TestAssert.Equal("Settings", ui.GetNode<Label>("SettingsMenu/Panel/Title").Text,
            "English accessibility path is readable");
        ui.ShowControls();
        ui.SetLastInputDevice(InputDeviceKind.Gamepad);
        TestAssert.True(ui.GetNode<Label>("ControlsView/Panel/Rows/Jump").Text.Contains("[A]"),
            "gamepad controls expose the active glyph");
        ui.SetLastInputDevice(InputDeviceKind.KeyboardMouse);
        TestAssert.True(ui.GetNode<Label>("ControlsView/Panel/Rows/Jump").Text.Contains("[SPACE]"),
            "keyboard controls expose the active glyph");
        TestAssert.True(ui.GetNode<Label>("ControlsView/Panel/Rows/AttackMode").Text.Contains("[Q / LMB]") &&
                        ui.GetNode<Label>("ControlsView/Panel/Rows/GrappleMode").Text.Contains("[E / LMB]") &&
                        ui.GetNode<Label>("ControlsView/Panel/Rows/Shield").Text.Contains("[RMB]") &&
                        ui.GetNode<Label>("ControlsView/Panel/Rows/Loadout").Text.Contains("[1 / 2 / 3 / 4]"),
            "Task10 English controls present coherent current keyboard and mouse bindings");
        TestAssert.True(!string.IsNullOrWhiteSpace(PrologueStrings.Get("controls.shield",
                            GameLanguage.ChineseSimplified)) &&
                        !string.IsNullOrWhiteSpace(PrologueStrings.Get("controls.shield", GameLanguage.English)) &&
                        !string.IsNullOrWhiteSpace(PrologueStrings.Get("controls.loadout",
                            GameLanguage.ChineseSimplified)) &&
                        !string.IsNullOrWhiteSpace(PrologueStrings.Get("controls.loadout", GameLanguage.English)),
            "Task10 shield and loadout rows have equal zh-CN/en localization coverage");

        PrologueFormalHud formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        PrologueHudViewModel bilingual = formal.CurrentModel.WithObjective("WAKE AND FOLLOW THE BELLS")
            .WithBoss("FALLEN BELLKEEPER", 12, 18, 2, armored: true, exposed: false);
        formal.SetLanguage(GameLanguage.English);
        formal.Present(bilingual);
        TestAssert.Equal("WAKE AND FOLLOW THE BELLS",
            formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "Task10 English HUD objective remains wholly English");
        TestAssert.Equal("PHASE II · ARMOR CLOSED", formal.GetNode<Label>("Boss/State").Text,
            "Task10 English HUD state remains wholly English");
        formal.SetLanguage(GameLanguage.ChineseSimplified);
        TestAssert.Equal("循钟声苏醒", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "Task10 Chinese HUD objective remains wholly Chinese");
        TestAssert.Equal("阶段 II · 护甲闭合", formal.GetNode<Label>("Boss/State").Text,
            "Task10 Chinese HUD state remains wholly Chinese");

        TestAssert.True(PrologueProductionAudit.ValidateUiScale(.8f) &&
                        PrologueProductionAudit.ValidateUiScale(1.5f),
            "supported UI-scale endpoints remain valid");

        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
