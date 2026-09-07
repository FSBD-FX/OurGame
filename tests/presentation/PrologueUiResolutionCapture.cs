using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Boss;
using Knight.Combat;
using Knight.Presentation;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;
using Knight.Vfx;

namespace Knight.Tests.Presentation;

public partial class PrologueUiResolutionCapture : SceneTree
{
    private const int ConsecutiveCaptureFrameCount = 3;
    private static readonly Vector2I LogicalResolution = new(1280, 720);
    private static readonly Vector2I[] Resolutions =
    [
        new(1280, 720), new(1920, 1080), new(2560, 1440), new(3840, 2160)
    ];
    private readonly HashSet<string> _capturedSensoryStates = new(StringComparer.Ordinal);
    private string _captureRoot = string.Empty;

    public override async void _Initialize()
    {
        try
        {
            _captureRoot = ResolveExternalCaptureRoot();
            foreach (Vector2I resolution in Resolutions) await AuditResolution(resolution);
            AssertSensoryCaptureCompleteness();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Frames(4);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GD.Print("PROLOGUE_UI_RESOLUTION_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            Paused = false;
            GD.PushError("PROLOGUE_UI_RESOLUTION_FAIL: " + exception);
            Quit(1);
        }
    }

    private async Task AuditResolution(Vector2I resolution)
    {
        var viewport = new SubViewport
        {
            Name = $"UiAudit{resolution.X}x{resolution.Y}",
            Size = resolution,
            Size2DOverride = LogicalResolution,
            Size2DOverrideStretch = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        level.GetNode<PrologueAudioDirector>("Audio").PlaybackEnabled = false;
        Root.AddChild(viewport);
        viewport.AddChild(level);
        await Frames(3);

        PrologueUiRoot ui = level.GetNode<PrologueUiRoot>("ProductUi");
        ui.ShowMainMenu(false);
        await Frames(1);
        AssertCenteredAndSafe(ui.GetNode<Control>("MainMenu/Panel"), LogicalResolution, "title panel");

        ui.ShowSettings(new GameSettingsData());
        await Frames(1);
        Control settingsPanel = ui.GetNode<Control>("SettingsMenu/Panel");
        AssertCenteredAndSafe(settingsPanel, LogicalResolution, "settings panel");
        Control settingsRows = settingsPanel.GetNode<Control>("Rows");
        Control settingsBack = settingsPanel.GetNode<Control>("Back");
        foreach (Node child in settingsRows.GetChildren())
        {
            if (child is not Control row) continue;
            AssertContainedAndSafe(settingsPanel, row, LogicalResolution, $"settings row {row.Name}");
            Control value = row.GetNode<Control>("Value");
            AssertContainedAndSafe(settingsPanel, value, LogicalResolution, $"settings value {row.Name}");
            if (settingsBack.GetGlobalRect().Intersects(row.GetGlobalRect()))
                throw new InvalidOperationException(
                    $"settings back button overlaps row {row.Name} at {resolution}");
        }
        AssertContainedAndSafe(settingsPanel, settingsBack, LogicalResolution, "settings back button");

        ui.ShowControls();
        await Frames(1);
        AssertCenteredAndSafe(ui.GetNode<Control>("ControlsView/Panel"), LogicalResolution, "controls panel");

        flow.ReturnToTitle();
        flow.StartNew();
        await Frames(4);
        PrologueHud hud = level.GetNode<PrologueHud>("Hud");
        PrologueFormalHud formal = hud.GetNode<PrologueFormalHud>("FormalHud");
        PrologueGuidanceController guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        WorldTargetMarkerVisual guidanceVisual = hud.GetNode<WorldTargetMarkerVisual>(
            "Guidance/VisualRoot");

        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.English,
            GuidanceEnabled = true,
            UiScale = 1f
        });
        for (int recovery = 0; recovery < 3; recovery++)
        {
            level.RequestRespawn("DEATH RESET");
            await Frames(2);
            await PhysicsFrames(2);
        }
        guidance.RefreshNow();
        await Frames(2);
        if (guidance.SameSectorFailureCount < 2 || !guidanceVisual.IsExpandedTeaching ||
            !guidanceVisual.RenderedTextContained || !guidanceVisual.Visible ||
            !guidanceVisual.HasVisiblePrompt ||
            !guidanceVisual.PromptRect.HasArea() || guidance.CurrentInstruction.Targets.Count == 0)
            throw new InvalidOperationException(
                $"real repeated-failure entrance guidance is not expanded and contained at {resolution}");
        AssertProtectedPrompt(level, guidanceVisual, LogicalResolution, "live entrance guidance prompt");
        string englishGuidance = level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        if (!englishGuidance.Contains("First-Level Guidance", StringComparison.Ordinal) ||
            englishGuidance.Contains("第一关引导", StringComparison.Ordinal) ||
            englishGuidance.Contains("提示：", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"mounted English expanded guidance retained non-English chrome at {resolution}");
        await Capture(viewport, resolution, "entrance-guidance-expanded-en");
        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.ChineseSimplified,
            GuidanceEnabled = true,
            UiScale = 1f
        });
        await Frames(2);
        string chineseGuidance = level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        if (!chineseGuidance.Contains("第一关引导", StringComparison.Ordinal) ||
            chineseGuidance.Contains("First-Level Guidance", StringComparison.Ordinal) ||
            !guidanceVisual.RenderedTextContained)
            throw new InvalidOperationException(
                $"mounted Chinese expanded guidance retained English chrome or overflowed at {resolution}");
        AssertProtectedPrompt(level, guidanceVisual, LogicalResolution,
            "localized Chinese entrance guidance prompt");
        await Capture(viewport, resolution, "entrance-guidance-expanded-zh");

        guidance.SetProcess(false);
        try
        {
            await CaptureClawGuidance(viewport, level, guidanceVisual, resolution,
                GameLanguage.English, "claw-guidance-expanded-en");
            await CaptureClawGuidance(viewport, level, guidanceVisual, resolution,
                GameLanguage.ChineseSimplified, "claw-guidance-expanded-zh");
        }
        finally
        {
            guidance.SetProcess(true);
            guidance.RefreshNow();
        }

        CharacterBody2D player = level.Player;
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        PrologueSectorId priorSector = level.CurrentSector!.Value;
        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.ChineseSimplified,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        player.GlobalPosition = new Vector2(1500f, 1600f);
        await WaitFor(() => level.CurrentSector.HasValue && level.CurrentSector.Value != priorSector,
            90, $"real sector transition resets expanded teaching at {resolution}");
        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.ChineseSimplified,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        guidance.RefreshNow();
        await Frames(2);
        if (guidanceVisual.IsExpandedTeaching || guidance.SameSectorFailureCount != 0)
            throw new InvalidOperationException(
                $"real sector entry failed to collapse repeated-failure guidance at {resolution}");

        formal.Present(formal.CurrentModel.WithObjective(formal.CurrentModel.Objective,
            PrologueHudViewModel.ObjectiveExpandDurationSeconds + .1f));
        string grappleGlyph = ResolveLiveGlyph(InputActions.SelectGrappleMode);
        string primaryGlyph = ResolveLiveGlyph(InputActions.PrimaryAction);
        string ordinaryGlyph = $"[{grappleGlyph} + {primaryGlyph}]";
        PrologueSector sector = level.CurrentSectorRoot;
        var tutorial = new TutorialPromptTrigger
        {
            Name = "Task10CaptureTutorial",
            TutorialId = "thread_anchor",
            Text = "GRAPPLE",
            Position = sector.ToLocal(player.GlobalPosition + Vector2.Right * 220f),
            CollisionLayer = 0,
            CollisionMask = 2
        };
        tutorial.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(120, 120) }
        });
        sector.AddChild(tutorial);
        await PhysicsFrames(2);
        player.GlobalPosition = tutorial.GlobalPosition;
        await PhysicsFrames(3);
        await Frames(2);
        formal.SetInputDevice(InputDeviceKind.KeyboardMouse);
        hud.HideBoss();
        Label productionPrompt = formal.GetNode<Label>("Prompt/Text");
        if (productionPrompt.Text != "切换机械爪并瞄准锚点发射")
            throw new InvalidOperationException(
                $"real Chinese tutorial prompt did not reach FormalHud at {resolution}");
        if (guidanceVisual.IsExpandedTeaching || guidanceVisual.PromptRect.Size.Y > 50f)
            throw new InvalidOperationException(
                $"ordinary guidance failed to auto-collapse at {resolution}");
        AssertOrdinaryHudCompact(formal, LogicalResolution);
        AssertSafe(formal.GetNode<Control>("Health"), LogicalResolution, "health HUD");
        AssertSafe(formal.GetNode<Control>("ThreadSpool"), LogicalResolution, "thread HUD");
        AssertSafe(formal.GetNode<Control>("ObjectiveRibbon"), LogicalResolution, "objective HUD");
        AssertSafe(formal.GetNode<Control>("Prompt"), LogicalResolution, "prompt HUD");
        if (formal.GetNode<Label>("Prompt/Glyph").Text != ordinaryGlyph)
            throw new InvalidOperationException($"keyboard glyph is stale at {resolution}");
        AssertTask10ActionHud(formal, LogicalResolution);
        await Capture(viewport, resolution, "ordinary-keyboard");

        await Tap(InputActions.SelectAttackMode);
        await Tap(InputActions.Slot4);
        await Tap(InputActions.PrimaryAction);
        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.English,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        await Frames(2);
        if (formal.CurrentModel.ActionMode != PlayerActionMode.Attack ||
            formal.CurrentModel.ActiveSlot != 4 ||
            formal.CurrentModel.ActionRejection != PlayerActionRejectionReason.EmptySlot ||
            productionPrompt.Text != "SELECT GRAPPLE AND FIRE AT THE ANCHOR")
            throw new InvalidOperationException(
                $"real InputMap mode/loadout/rejection or production English prompt is stale at {resolution}");
        AssertRenderedPromptContained(productionPrompt, resolution);
        // Task6's InputMap-first teaching contract makes the Q/E bindings visible in the
        // production HUD.  Keep this display-backed audit bound to those exact labels.
        AssertTask10Language(formal, "Q ATTACK", "E GRAPPLE", "ENERGY", "HEAT", resolution);
        await Capture(viewport, resolution, "action-hud-en");
        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.ChineseSimplified,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        await Frames(2);
        AssertTask10Language(formal, "Q 攻击", "E 机械爪", "能量", "热量", resolution);
        await Capture(viewport, resolution, "action-hud-zh");

        PrologueAimReticle reticle = formal.GetNode<PrologueAimReticle>("AimReticle");
        var reticleColors = new HashSet<string>(StringComparer.Ordinal);
        var reticleShapes = new HashSet<string>(StringComparer.Ordinal);
        foreach (PrologueAimReticleState state in Enum.GetValues<PrologueAimReticleState>().Distinct())
        {
            PrologueAimReticleStyle style = PrologueAimReticle.StyleFor(state);
            if (!reticleColors.Add(style.Color.ToHtml()) || !reticleShapes.Add(style.GeometrySignature))
                throw new InvalidOperationException($"reticle state {state} lacks unique shape and color");
        }

        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.English,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        await Tap(InputActions.SelectAttackMode);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.Attack, resolution);
        await Capture(viewport, resolution, "reticle-attack");

        await Tap(InputActions.SelectGrappleMode);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.OutOfRange, resolution);
        await Capture(viewport, resolution, "reticle-outofrange");

        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        await Frames(2);
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        Vector2 cursor = input.Current.MouseWorld;
        player.GlobalPosition = cursor + Vector2.Left * 180f;
        player.Velocity = Vector2.Zero;
        GrappleSurface2D surface = Surface("Task10CaptureSurface", cursor,
            GrappleSurfaceMaterial.DedicatedAnchor);
        level.AddChild(surface);
        await PhysicsFrames(2);

        Input.ActionPress(InputActions.PrimaryAction);
        await WaitFor(() => claw.CurrentFrame.State is MechanicalClawState.Latched or
            MechanicalClawState.Reel or MechanicalClawState.Swing, 180,
            $"real claw reaches a grippable transition at {resolution}");
        claw.SetPhysicsProcess(false);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.Grippable, resolution);
        await Capture(viewport, resolution, "reticle-grippable");

        claw.SetPhysicsProcess(true);
        Input.ActionRelease(InputActions.PrimaryAction);
        await WaitFor(() => claw.CurrentFrame.State is MechanicalClawState.Released or
            MechanicalClawState.Retract, 30,
            $"real claw reaches retracting transition at {resolution}");
        claw.SetPhysicsProcess(false);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.OutOfRange, resolution);
        claw.SetPhysicsProcess(true);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Idle, 180,
            $"real claw returns to Idle after capture at {resolution}");

        surface.Material = GrappleSurfaceMaterial.Ice;
        Input.ActionPress(InputActions.PrimaryAction);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Failed &&
            claw.CurrentFrame.Failure == MechanicalClawFailure.IceSlip, 30,
            $"real ice surface reaches its typed failure at {resolution}");
        claw.SetPhysicsProcess(false);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.Ice, resolution);
        await Capture(viewport, resolution, "reticle-ice");
        Input.ActionRelease(InputActions.PrimaryAction);
        claw.SetPhysicsProcess(true);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Idle, 60,
            $"ice failure cleans up at {resolution}");

        surface.Material = GrappleSurfaceMaterial.Forbidden;
        Input.ActionPress(InputActions.PrimaryAction);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Failed &&
            claw.CurrentFrame.Failure == MechanicalClawFailure.Forbidden, 30,
            $"real forbidden surface reaches its typed failure at {resolution}");
        claw.SetPhysicsProcess(false);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.Forbidden, resolution);
        await Capture(viewport, resolution, "reticle-forbidden");
        Input.ActionRelease(InputActions.PrimaryAction);
        claw.SetPhysicsProcess(true);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Idle, 60,
            $"forbidden failure cleans up at {resolution}");

        surface.Material = GrappleSurfaceMaterial.DedicatedAnchor;
        StaticBody2D obstruction = WorldWall("Task10CaptureObstruction",
            player.GlobalPosition.Lerp(surface.GlobalPosition, .5f));
        level.AddChild(obstruction);
        await PhysicsFrames(2);
        Input.ActionPress(InputActions.PrimaryAction);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Failed &&
            claw.CurrentFrame.Failure == MechanicalClawFailure.Obstructed, 30,
            $"real blocked line of sight reaches typed obstruction at {resolution}");
        claw.SetPhysicsProcess(false);
        await Frames(2);
        AssertRuntimeReticle(reticle, PrologueAimReticleState.Obstructed, resolution);
        await Capture(viewport, resolution, "reticle-obstructed");
        Input.ActionRelease(InputActions.PrimaryAction);
        claw.SetPhysicsProcess(true);
        await WaitFor(() => claw.CurrentFrame.State == MechanicalClawState.Idle, 60,
            $"obstructed failure cleans up at {resolution}");
        obstruction.QueueFree();
        await PhysicsFrames(2);

        if (resolution == new Vector2I(1280, 720))
        {
            formal.SetUiScale(1.5f);
            PrologueHudLayout largeLayout = PrologueHudLayout.Calculate(LogicalResolution, 1.5f);
            if (formal.GetNode<Control>("Health").Size != largeLayout.HealthRect.Size)
                throw new InvalidOperationException("mounted HUD did not apply UI scale 1.5");
            AssertSafe(formal.GetNode<Control>("Health"), LogicalResolution, "large-scale health HUD");
            AssertSafe(formal.GetNode<Control>("Boss"), LogicalResolution, "large-scale Boss HUD");
            await Capture(viewport, resolution, "ui-scale-150");
            formal.SetUiScale(1f);

            PrologueVfxDirector vfx = level.GetNode<PrologueVfxDirector>("Vfx");
            vfx.Configure(new GameSettingsData { ScreenShake = 0f, ReduceFlashes = true });
            vfx.Play(PresentationCue.BossTransition, Vector2.Zero, Vector2.Right);
            if (vfx.LastScreenFlashOpacity != 0f ||
                vfx.ResolveStyle(PresentationCue.BossTransition).FullScreenFlash)
                throw new InvalidOperationException("reduced-flash capture retained a screen flash");
            await Capture(viewport, resolution, "reduced-flash-two-channel");
        }

        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.English,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        hud.ShowPrompt(string.Empty, string.Empty);
        if (formal.GetNode<Control>("Prompt").Visible)
            throw new InvalidOperationException($"stale formal prompt was not cleared at {resolution}");
        hud.ShowBoss("FALLEN BELLKEEPER", 72, 100, 2, exposed: false);
        await Frames(2);
        AssertContinuousBossLayout(level, formal, LogicalResolution);
        await Capture(viewport, resolution, "boss-layout-safe");

        ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        pause.Open();
        AssertCenteredAndSafe(pause.GetNode<Control>("Panel"), LogicalResolution, "pause panel");
        pause.Execute(ProloguePauseCommand.ShowControls);
        if (!pause.GetNode<Label>("Panel/Title").Text.Contains("PAUSED", StringComparison.Ordinal) ||
            !pause.GetNode<Label>("Panel/Controls").Text.Contains("ATTACK MODE", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"production English pause overlay is incoherent at {resolution}");
        await Capture(viewport, resolution, "pause-en");
        pause.Execute(ProloguePauseCommand.Resume);

        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.ChineseSimplified,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        pause.Open();
        pause.Execute(ProloguePauseCommand.ShowControls);
        if (!pause.GetNode<Label>("Panel/Title").Text.Contains("暂停", StringComparison.Ordinal) ||
            !pause.GetNode<Label>("Panel/Controls").Text.Contains("攻击模式", StringComparison.Ordinal) ||
            pause.GetNode<Label>("Panel/Controls").Text.Contains("ATTACK MODE", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"production Chinese pause overlay is incoherent at {resolution}");
        await Capture(viewport, resolution, "pause-zh");
        pause.Execute(ProloguePauseCommand.Resume);

        string recoveryTitle = string.Empty;
        string recoveryReason = string.Empty;
        void CaptureRecovery(string reason)
        {
            recoveryTitle = formal.GetNode<Label>("Recovery/Title").Text;
            recoveryReason = formal.GetNode<Label>("Recovery/Reason").Text;
        }
        level.RecoveryRequested += CaptureRecovery;
        level.RequestRespawn("DEATH RESET");
        level.RecoveryRequested -= CaptureRecovery;
        await Frames(1);
        await PhysicsFrames(2);
        if (recoveryTitle != "墓灯重燃" || recoveryReason != "倒下 · 墓灯重燃")
            throw new InvalidOperationException(
                $"real Chinese recovery overlay bypassed production language at {resolution}");
        AssertHorizontallyCenteredAndSafe(formal.GetNode<Control>("Recovery/Title"),
            LogicalResolution, "recovery title");
        AssertHorizontallyCenteredAndSafe(formal.GetNode<Control>("Recovery/Reason"),
            LogicalResolution, "recovery reason");

        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.English,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        PrologueCompletionController completion =
            level.GetNode<PrologueCompletionController>("Completion");
        completion.RestoreSession(PrologueObjective.DefeatBellkeeper);
        level.RecordBossDefeated();
        await PhysicsFrames(2);
        await Frames(2);
        if (!formal.GetNode<Control>("Completion").Visible ||
            !formal.GetNode<Label>("Completion/Panel/Title").Text
                .Contains("PROLOGUE COMPLETE", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"real English completion overlay did not reach production HUD at {resolution}");
        AssertCenteredAndSafe(formal.GetNode<Control>("Completion/Panel"),
            LogicalResolution, "completion panel");
        AssertVisibleTextContained(formal.GetNode<Control>("Completion/Panel"), resolution,
            "English completion panel");
        await Capture(viewport, resolution, "completion-clear-en");
        flow.ApplySettings(new GameSettingsData
        {
            Language = GameLanguage.ChineseSimplified,
            GuidanceEnabled = false,
            UiScale = 1f
        });
        await Frames(2);
        if (!formal.GetNode<Label>("Completion/Panel/Title").Text
                .Contains("序章完成", StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"visible completion failed live Chinese production redispatch at {resolution}");
        AssertVisibleTextContained(formal.GetNode<Control>("Completion/Panel"), resolution,
            "Chinese completion panel");
        await Capture(viewport, resolution, "completion-clear-zh");

        foreach (StringName action in new[]
        {
            InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
            InputActions.PrimaryAction, InputActions.Slot4
        }) Input.ActionRelease(action);
        Paused = false;
        level.Free();
        viewport.Free();
        level = null;
        viewport = null;
        await Frames(4);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GD.Print($"PROLOGUE_UI_RESOLUTION={resolution.X}x{resolution.Y}");
    }

    private static void AssertRuntimeReticle(PrologueAimReticle reticle,
        PrologueAimReticleState expected, Vector2I resolution)
    {
        if (reticle.CurrentFrame.State != expected || !reticle.Visible)
            throw new InvalidOperationException(
                $"real runtime reticle mismatch at {resolution}: expected={expected} " +
                $"actual={reticle.CurrentFrame.State} visible={reticle.Visible}");
        Rect2 safe = new(new Vector2(48, 40), LogicalResolution - new Vector2I(96, 80));
        if (!safe.Encloses(reticle.GetGlobalRect()))
            throw new InvalidOperationException(
                $"real runtime reticle {expected} leaves safe bounds at {resolution}");
        Control formalHud = reticle.GetParent<Control>();
        foreach (Rect2 protectedRegion in
                 WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formalHud))
        {
            if (reticle.GetGlobalRect().Intersects(protectedRegion.Grow(8f)))
                throw new InvalidOperationException(
                    $"real runtime reticle {expected} overlaps visible FormalHud region " +
                    $"{protectedRegion} at {resolution}");
        }
    }

    private static void AssertVisibleTextContained(Control panel, Vector2I resolution, string label)
    {
        foreach (Control control in DescendantControls(panel))
        {
            if (!control.Visible || string.IsNullOrWhiteSpace(ReadText(control))) continue;
            if (!panel.GetGlobalRect().Encloses(control.GetGlobalRect()))
                throw new InvalidOperationException(
                    $"{label} child {control.Name} leaves its panel at {resolution}: " +
                    $"panel={panel.GetGlobalRect()} child={control.GetGlobalRect()}");
            Font font = control.GetThemeFont("font");
            int fontSize = control.GetThemeFontSize("font_size");
            if (control is Label text)
            {
                float height = Math.Max(1, text.GetLineCount()) * font.GetHeight(fontSize);
                float width = text.AutowrapMode == TextServer.AutowrapMode.Off
                    ? MaximumLineWidth(font, fontSize, text.Text)
                    : 0f;
                if (height > text.Size.Y + .5f || width > text.Size.X + .5f)
                    throw new InvalidOperationException(
                        $"{label} text {text.Name} overflows at {resolution}: " +
                        $"required={width}x{height} available={text.Size}");
            }
            else if (control is Button button)
            {
                float width = font.GetStringSize(button.Text, HorizontalAlignment.Left, -1f,
                    fontSize).X;
                if (width > button.Size.X + .5f)
                    throw new InvalidOperationException(
                        $"{label} button {button.Name} clips at {resolution}");
            }
        }
    }

    private static IEnumerable<Control> DescendantControls(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Control control) yield return control;
            foreach (Control descendant in DescendantControls(child)) yield return descendant;
        }
    }

    private static string ReadText(Control control) => control switch
    {
        Label label => label.Text,
        Button button => button.Text,
        _ => string.Empty
    };

    private static float MaximumLineWidth(Font font, int fontSize, string text)
    {
        float width = 0f;
        foreach (string line in (text ?? string.Empty).Split('\n'))
            width = Math.Max(width,
                font.GetStringSize(line, HorizontalAlignment.Left, -1f, fontSize).X);
        return width;
    }

    private async Task Tap(StringName action)
    {
        Input.ActionPress(action);
        await PhysicsFrames(1);
        Input.ActionRelease(action);
        await PhysicsFrames(1);
    }

    private async Task WaitFor(Func<bool> condition, int frames, string message)
    {
        for (int index = 0; index < frames && !condition(); index++) await PhysicsFrames(1);
        if (!condition()) throw new InvalidOperationException(message);
    }

    private static GrappleSurface2D Surface(string name, Vector2 position,
        GrappleSurfaceMaterial material)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = material,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 10f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static StaticBody2D WorldWall(string name, Vector2 position)
    {
        var wall = new StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        wall.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(12f, 96f) }
        });
        return wall;
    }

    private static void AssertTask10ActionHud(PrologueFormalHud formal, Vector2I resolution)
    {
        Control mode = formal.GetNode<Control>("ModeSelector");
        Control loadout = formal.GetNode<Control>("Loadout");
        Control resources = formal.GetNode<Control>("ResourceCluster");
        Control[] actionControls = [mode, loadout, resources];
        foreach (Control control in actionControls)
        {
            if (!control.Visible) throw new InvalidOperationException($"{control.Name} is hidden");
            AssertSafe(control, resolution, $"Task10 {control.Name}");
            foreach (Node child in control.GetChildren())
                if (child is Control visibleChild && visibleChild.Visible)
                    AssertContainedAndSafe(control, visibleChild, resolution,
                        $"Task10 {control.Name}/{visibleChild.Name}");
        }

        for (int first = 0; first < actionControls.Length; first++)
        for (int second = first + 1; second < actionControls.Length; second++)
            if (actionControls[first].GetGlobalRect().Intersects(
                    actionControls[second].GetGlobalRect()))
                throw new InvalidOperationException(
                    $"Task10 action HUD overlap: {actionControls[first].Name}/" +
                    $"{actionControls[second].Name} at {resolution}");

        foreach (string protectedPath in new[] { "Health", "ObjectiveRibbon", "Prompt", "Boss" })
        {
            Control protectedControl = formal.GetNode<Control>(protectedPath);
            if (!protectedControl.Visible) continue;
            foreach (Control action in actionControls)
                if (action.GetGlobalRect().Intersects(protectedControl.GetGlobalRect()))
                    throw new InvalidOperationException(
                        $"Task10 {action.Name} overlaps {protectedPath} at {resolution}");
        }

        Control legacySpool = formal.GetNode<Control>("ThreadSpool");
        if (legacySpool.Visible ||
            formal.GetNodeOrNull<ProgressBar>("ThreadSpool/Cooldown") != null ||
            !string.IsNullOrEmpty(formal.GetNode<Label>("ThreadSpool/State").Text))
            throw new InvalidOperationException(
                $"Task10 legacy thread cooldown presentation returned at {resolution}");
    }

    private static void AssertTask10Language(PrologueFormalHud formal, string attack,
        string grapple, string energy, string heat, Vector2I resolution)
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ModeSelector/Attack/Label"] = attack,
            ["ModeSelector/Grapple/Label"] = grapple,
            ["ResourceCluster/EnergyLabel"] = energy,
            ["ResourceCluster/HeatLabel"] = heat
        };
        foreach ((string path, string text) in expected)
            if (formal.GetNode<Label>(path).Text != text)
                throw new InvalidOperationException(
                    $"Task10 language mismatch at {resolution}/{path}: " +
                    $"expected={text} actual={formal.GetNode<Label>(path).Text}");
        Control glyph = formal.GetNode<Control>("Prompt/Glyph");
        Control prompt = formal.GetNode<Control>("Prompt/Text");
        if (glyph.GetGlobalRect().Intersects(prompt.GetGlobalRect()))
            throw new InvalidOperationException(
                $"Task10 prompt glyph overlaps localized copy at {resolution}");
    }

    private static void AssertRenderedPromptContained(Label prompt, Vector2I resolution)
    {
        Font font = prompt.GetThemeFont("font");
        int fontSize = prompt.GetThemeFontSize("font_size");
        Vector2 measured = MeasureRenderedPrompt(prompt, font, fontSize, out int expectedLines);
        int actualLines = Math.Max(1, prompt.GetLineCount());
        Control panel = prompt.GetParent<Control>();
        bool longEnglishPromptNeedsWrap = prompt.Text ==
            "SELECT GRAPPLE AND FIRE AT THE ANCHOR" && actualLines < 2;
        if (longEnglishPromptNeedsWrap || actualLines != expectedLines ||
            measured.X > prompt.Size.X + .5f ||
            measured.Y > prompt.Size.Y + .5f ||
            !panel.GetGlobalRect().Encloses(prompt.GetGlobalRect()))
            throw new InvalidOperationException(
                "TASK10_FIX2_PROMPT_CONTAINMENT_RED " +
                $"resolution={resolution} wrap={prompt.AutowrapMode} " +
                $"actual_lines={actualLines} measured_lines={expectedLines} " +
                $"rendered={measured} available={prompt.Size} " +
                $"panel={panel.GetGlobalRect()} label={prompt.GetGlobalRect()}");
        GD.Print("TASK10_FIX2_PROMPT_BOUNDS_OK " +
            $"resolution={resolution.X}x{resolution.Y} lines={actualLines} " +
            $"rendered={measured} available={prompt.Size}");
    }

    private static Vector2 MeasureRenderedPrompt(Label prompt, Font font, int fontSize,
        out int lineCount)
    {
        string[] paragraphs = (prompt.Text ?? string.Empty).Split('\n');
        if (prompt.AutowrapMode == TextServer.AutowrapMode.Off)
        {
            lineCount = Math.Max(1, paragraphs.Length);
            return new Vector2(MaximumLineWidth(font, fontSize, prompt.Text),
                lineCount * font.GetHeight(fontSize));
        }

        float maximumWidth = 0f;
        lineCount = 0;
        foreach (string paragraph in paragraphs)
        {
            string current = string.Empty;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
                float candidateWidth = font.GetStringSize(candidate, HorizontalAlignment.Left,
                    -1f, fontSize).X;
                if (!string.IsNullOrEmpty(current) && candidateWidth > prompt.Size.X + .5f)
                {
                    maximumWidth = Math.Max(maximumWidth, font.GetStringSize(current,
                        HorizontalAlignment.Left, -1f, fontSize).X);
                    lineCount++;
                    current = word;
                }
                else current = candidate;
            }
            maximumWidth = Math.Max(maximumWidth, font.GetStringSize(current,
                HorizontalAlignment.Left, -1f, fontSize).X);
            lineCount++;
        }
        lineCount = Math.Max(1, lineCount);
        return new Vector2(maximumWidth, lineCount * font.GetHeight(fontSize));
    }

    private static void AssertOrdinaryHudCompact(PrologueFormalHud formal, Vector2I resolution)
    {
        Control health = formal.GetNode<Control>("Health");
        Control thread = formal.GetNode<Control>("ThreadSpool");
        Control objective = formal.GetNode<Control>("ObjectiveRibbon");
        Rect2 cluster = health.GetGlobalRect().Merge(thread.GetGlobalRect());
        PrologueHudLayout expected = PrologueHudLayout.Calculate(resolution);
        if (cluster.Size.Y > expected.HealthRect.Size.Y + 1f ||
            cluster.Size.X > expected.HealthRect.Size.X + expected.ThreadRect.Size.X +
            (expected.ThreadRect.Position.X - expected.HealthRect.End.X) + 1f)
            throw new InvalidOperationException($"ordinary HUD cluster is oversized at {resolution}");
        if (objective.Size.Y > expected.ObjectiveRect.Size.Y + 1f)
            throw new InvalidOperationException($"ordinary objective is not one line at {resolution}");
    }

    private static void AssertProtectedPrompt(PrologueLevelController level,
        WorldTargetMarkerVisual visual, Vector2I resolution, string label)
    {
        Rect2 safe = new(new Vector2(48, 40),
            new Vector2(resolution.X - 96, resolution.Y - 80));
        if (!safe.Encloses(visual.PromptRect))
            throw new InvalidOperationException($"{label} leaves safe bounds at {resolution}");
        if (!level.CurrentSector.HasValue || level.CurrentSectorRoot == null ||
            !level.CurrentSectorRoot.IsInsideTree())
            throw new InvalidOperationException(
                $"{label} is not backed by a live mounted continuous sector at {resolution}");
        Control formalHud = level.GetNode<Control>("Hud/FormalHud");
        foreach (Rect2 protectedRegion in
                 WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formalHud))
        {
            if (visual.PromptRect.Intersects(protectedRegion.Grow(8f)))
                throw new InvalidOperationException(
                    $"{label} overlaps visible FormalHud region {protectedRegion} at {resolution}");
        }
    }

    private static void AssertContinuousBossLayout(PrologueLevelController level,
        PrologueFormalHud formal, Vector2I resolution)
    {
        if (!level.CurrentSector.HasValue || level.CurrentSectorRoot == null ||
            !level.CurrentSectorRoot.IsInsideTree())
            throw new InvalidOperationException(
                "Boss layout audit is not mounted in the continuous generated main");
        Control boss = formal.GetNode<Control>("Boss");
        if (!boss.Visible) throw new InvalidOperationException("Boss layout state did not show the HUD");
        AssertSafe(boss, resolution, "real Boss HUD");
        foreach (Node child in boss.GetChildren())
        {
            if (child is not Control control || !control.Visible) continue;
            AssertContainedAndSafe(boss, control, resolution, $"real Boss HUD child {control.Name}");
        }

        Rect2 playerProtection = new(
            level.Player.GetGlobalTransformWithCanvas().Origin - new Vector2(80, 120),
            new Vector2(160, 240));
        if (boss.GetGlobalRect().Grow(8f).Intersects(playerProtection))
            throw new InvalidOperationException(
                $"Boss HUD overlaps the live continuous player protection region at {resolution}");
    }

    private static string ResolveLiveGlyph(StringName action)
    {
        foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
        {
            if (inputEvent is InputEventKey key)
                return OS.GetKeycodeString(key.PhysicalKeycode != Key.None
                    ? key.PhysicalKeycode : key.Keycode).ToUpperInvariant();
            if (inputEvent is InputEventMouseButton mouse)
                return mouse.ButtonIndex switch
                {
                    MouseButton.Left => "LMB",
                    MouseButton.Right => "RMB",
                    _ => $"MOUSE {mouse.ButtonIndex}"
                };
        }
        throw new InvalidOperationException($"live InputMap action '{action}' has no glyph");
    }

    private async Task Capture(SubViewport viewport, Vector2I resolution, string state)
    {
        string directory = Path.Combine(_captureRoot, $"{resolution.X}x{resolution.Y}");
        Error directoryError = DirAccess.MakeDirRecursiveAbsolute(directory);
        if (directoryError != Error.Ok && directoryError != Error.AlreadyExists)
            throw new InvalidOperationException(
                $"failed to create capture directory {directory}: {directoryError}");
        string captureKey = $"{resolution.X}x{resolution.Y}/{state}";
        if (!_capturedSensoryStates.Add(captureKey))
            throw new InvalidOperationException($"duplicate UI sensory capture {captureKey}");
        GD.Print($"PROLOGUE_UI_SENSORY_CAPTURE={captureKey}");
        for (int index = 0; index < ConsecutiveCaptureFrameCount; index++)
        {
            await Frames(1);
            Image image = viewport.GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.GetSize() != resolution)
                throw new InvalidOperationException(
                    $"running capture is empty or wrong-sized for {resolution}/{state} frame {index}");
            string path = Path.Combine(directory, $"{state}-frame{index:D2}.png");
            Error saveError = image.SavePng(path);
            if (saveError != Error.Ok)
                throw new InvalidOperationException($"failed to write {path}: {saveError}");
            GD.Print($"PROLOGUE_UI_CAPTURE={path}");
            GD.Print($"PROLOGUE_UI_SENSORY_CAPTURE_FRAME resolution={resolution.X}x{resolution.Y} " +
                $"state={state} index={index}");
        }
    }

    private static string ResolveExternalCaptureRoot()
    {
        string captureRoot = RequireAbsolutePath(
            System.Environment.GetEnvironmentVariable("KNIGHT_UI_CAPTURE_ROOT"),
            "KNIGHT_UI_CAPTURE_ROOT", "PROLOGUE_UI_CAPTURE_ROOT_REQUIRED");
        string evidenceRoot = RequireAbsolutePath(
            System.Environment.GetEnvironmentVariable("KNIGHT_UI_CAPTURE_EVIDENCE_ROOT"),
            "KNIGHT_UI_CAPTURE_EVIDENCE_ROOT", "PROLOGUE_UI_CAPTURE_EVIDENCE_ROOT_REQUIRED");
        string projectRoot = NormalizeAbsolutePath(ProjectSettings.GlobalizePath("res://"));
        if (IsSameOrDescendant(captureRoot, projectRoot))
            throw new InvalidOperationException(
                $"PROLOGUE_UI_CAPTURE_ROOT_INSIDE_PROJECT root={captureRoot} project={projectRoot}");
        if (IsSameOrDescendant(evidenceRoot, projectRoot))
            throw new InvalidOperationException(
                $"PROLOGUE_UI_CAPTURE_EVIDENCE_ROOT_INSIDE_PROJECT root={evidenceRoot} project={projectRoot}");
        if (captureRoot.Equals(evidenceRoot, StringComparison.OrdinalIgnoreCase) ||
            !IsSameOrDescendant(captureRoot, evidenceRoot))
            throw new InvalidOperationException(
                $"PROLOGUE_UI_CAPTURE_ROOT_OUTSIDE_EVIDENCE root={captureRoot} evidence={evidenceRoot}");
        return captureRoot;
    }

    private static string RequireAbsolutePath(string value, string variable, string missingMarker)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(missingMarker);
        if (!Path.IsPathRooted(value))
            throw new InvalidOperationException($"PROLOGUE_UI_CAPTURE_PATH_NOT_ABSOLUTE variable={variable} value={value}");
        return NormalizeAbsolutePath(value);
    }

    private static string NormalizeAbsolutePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static bool IsSameOrDescendant(string candidate, string root)
    {
        if (candidate.Equals(root, StringComparison.OrdinalIgnoreCase)) return true;
        string prefix = root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private async Task CaptureClawGuidance(SubViewport viewport, PrologueLevelController level,
        WorldTargetMarkerVisual visual, Vector2I resolution, GameLanguage language, string state)
    {
        PrologueGuidanceInstruction instruction = PrologueGuidanceModel.Evaluate(new(
            PrologueObjective.LearnGrappleMode, PrologueSectorId.CanyonDescent,
            PrologueBossGuidanceState.Inactive, 2, language, true), ResolveGuidanceGlyphs());
        visual.SetExpandedTeaching(true);
        visual.Present(instruction,
        [
            new PrologueGuidanceTargetView(new Vector2(LogicalResolution.X * .68f,
                    LogicalResolution.Y * .56f), Vector2.Zero, 0f, true)
        ]);
        await Frames(2);
        string text = level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        string[] expected = language == GameLanguage.English
            ? ["hold LMB", "A/D", "W/S", "one boost", "release LMB", "ice", "sealed"]
            : ["按住 LMB", "A/D", "W/S", "SPACE 跃升一次", "松开 LMB", "冰面", "封闭表面"];
        foreach (string token in expected)
            if (!text.Contains(token, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"mounted {language} claw guidance is missing '{token}' at {resolution}");
        if (!visual.RenderedTextContained || !visual.HasVisiblePrompt || !visual.PromptRect.HasArea())
            throw new InvalidOperationException(
                $"mounted {language} claw guidance did not fully wrap at {resolution}");
        AssertProtectedPrompt(level, visual, LogicalResolution,
            $"mounted {language} claw guidance prompt");
        await Capture(viewport, resolution, state);
    }

    private static IReadOnlyDictionary<StringName, string> ResolveGuidanceGlyphs()
    {
        var glyphs = new Dictionary<StringName, string>();
        foreach (StringName action in new[]
                 {
                     InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
                     InputActions.MoveDown, InputActions.Jump, InputActions.SelectAttackMode,
                     InputActions.SelectGrappleMode, InputActions.PrimaryAction, InputActions.Shield,
                     InputActions.Slot1, InputActions.Slot2, InputActions.Slot3, InputActions.Slot4
                 })
            glyphs[action] = ResolveLiveGlyph(action);
        return glyphs;
    }

    private void AssertSensoryCaptureCompleteness()
    {
        foreach (Vector2I resolution in Resolutions)
        {
            foreach (string state in new[]
                      { "entrance-guidance-expanded-en", "entrance-guidance-expanded-zh",
                          "claw-guidance-expanded-en", "claw-guidance-expanded-zh",
                          "ordinary-keyboard", "action-hud-en", "action-hud-zh",
                         "boss-layout-safe", "pause-en", "pause-zh",
                         "completion-clear-en", "completion-clear-zh" })
                AssertSensoryCapture(resolution, state);
            foreach (string reticle in new[]
                     { "attack", "grippable", "ice", "forbidden", "obstructed", "outofrange" })
                AssertSensoryCapture(resolution, $"reticle-{reticle}");
        }
        foreach (string state in new[]
                 { "ui-scale-150", "reduced-flash-two-channel" })
            AssertSensoryCapture(LogicalResolution, state);
    }

    private void AssertSensoryCapture(Vector2I resolution, string state)
    {
        string key = $"{resolution.X}x{resolution.Y}/{state}";
        if (!_capturedSensoryStates.Contains(key))
            throw new InvalidOperationException($"missing UI sensory capture {key}");
    }

    private static void AssertCenteredAndSafe(Control control, Vector2I resolution, string label)
    {
        AssertSafe(control, resolution, label);
        Vector2 expected = new(resolution.X / 2f, resolution.Y / 2f);
        if (control.GetGlobalRect().GetCenter().DistanceTo(expected) > 1.5f)
            throw new InvalidOperationException($"{label} is not centered at {resolution}");
    }

    private static void AssertHorizontallyCenteredAndSafe(Control control, Vector2I resolution, string label)
    {
        AssertSafe(control, resolution, label);
        if (Mathf.Abs(control.GetGlobalRect().GetCenter().X - resolution.X / 2f) > 1.5f)
            throw new InvalidOperationException($"{label} is not horizontally centered at {resolution}");
    }

    private static void AssertSafe(Control control, Vector2I resolution, string label)
    {
        Rect2 safe = new(new Vector2(48, 40), new Vector2(resolution.X - 96, resolution.Y - 80));
        Rect2 rect = control.GetGlobalRect();
        if (!safe.Encloses(rect))
            throw new InvalidOperationException($"{label} leaves safe bounds at {resolution}: {rect}");
    }

    private static void AssertContainedAndSafe(Control parent, Control child, Vector2I resolution, string label)
    {
        AssertSafe(child, resolution, label);
        if (!parent.GetGlobalRect().Encloses(child.GetGlobalRect()))
            throw new InvalidOperationException($"{label} leaves its panel at {resolution}: {child.GetGlobalRect()}");
    }

    private async Task Frames(int count)
    {
        for (int index = 0; index < count; index++)
            await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task PhysicsFrames(int count)
    {
        for (int index = 0; index < count; index++)
            await ToSignal(this, SignalName.PhysicsFrame);
    }
}
