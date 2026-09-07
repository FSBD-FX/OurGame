using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Settings;

namespace Knight.Ui;

public partial class PrologueFormalHud : Control
{
    [Export] public NodePath ThreadAbilityPath { get; set; } = new("../../PlayerRoot/Player/ThreadAbility");
    [Export]
    public NodePath PlayerPresentationPath { get; set; } = new("../../PlayerRoot/Player/Presentation");
    [Export] public NodePath InputPath { get; set; } = new("../../PlayerRoot/Player/Input");
    [Export] public NodePath MechanicalClawPath { get; set; } = new("../../PlayerRoot/Player/MechanicalClaw");

    public PrologueHudViewModel CurrentModel { get; private set; } =
        PrologueHudViewModel.Gameplay(5, 5, 1f, string.Empty, string.Empty, string.Empty);

    private PlayerPresentationCoordinator _playerPresentation;
    private PlayerInputReader _input;
    private MechanicalClawController _mechanicalClaw;
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private InputDeviceKind _inputDevice = InputDeviceKind.KeyboardMouse;
    private float _uiScale = 1f;
    private float _contentScale = 1f;
    private float _collapsedObjectiveHeight = 34f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ApplyLayout();
        Present(CurrentModel);
    }

    public override void _Process(double delta)
    {
        if (CurrentModel.ObjectiveExpanded)
        {
            CurrentModel = CurrentModel.AdvanceObjectiveTransition((float)delta);
            ApplyObjectivePresentation();
        }
        if (!GodotObject.IsInstanceValid(_playerPresentation))
            _playerPresentation = GetNodeOrNull<PlayerPresentationCoordinator>(PlayerPresentationPath);
        if (!GodotObject.IsInstanceValid(_input)) _input = GetNodeOrNull<PlayerInputReader>(InputPath);
        if (!GodotObject.IsInstanceValid(_mechanicalClaw))
            _mechanicalClaw = GetNodeOrNull<MechanicalClawController>(MechanicalClawPath);
        PrologueHudViewModel next = CurrentModel;
        if (_playerPresentation != null)
        {
            PlayerHudActionState action = _playerPresentation.ActionHudState;
            next = next.WithActionState(action.Mode, action.ActiveSlot, action.EnergyRatio,
                action.HeatRatio, action.IsOverheated, action.Rejection);
        }
        if (next != CurrentModel) Present(next);
        PresentReticle();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
            SetInputDevice(InputDeviceKind.Gamepad);
        else if (@event is InputEventKey or InputEventMouseButton or InputEventMouseMotion)
            SetInputDevice(InputDeviceKind.KeyboardMouse);
    }

    public void SetInputDevice(InputDeviceKind device)
    {
        if (_inputDevice == device) return;
        _inputDevice = device;
        Label glyph = GetNodeOrNull<Label>("Prompt/Glyph");
        if (glyph != null) glyph.Text = ResolvePromptGlyph(CurrentModel.PromptGlyph);
    }

    public void SetUiScale(float uiScale)
    {
        _uiScale = Mathf.Clamp(uiScale, .8f, 1.5f);
        ApplyLayout();
    }

    public void SetLanguage(GameLanguage language)
    {
        _language = System.Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        GetNodeOrNull<PrologueActionModeVisual>("ModeSelector")?.SetLanguage(_language);
        GetNodeOrNull<PrologueEnergyHeatVisual>("ResourceCluster")?.SetLanguage(_language);
        GetNodeOrNull<PrologueRecoveryView>("Recovery")?.SetLanguage(_language);
        GetNodeOrNull<PrologueCompletionView>("Completion")?.SetLanguage(_language);
        Present(CurrentModel);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && IsInsideTree()) ApplyLayout();
    }

    public void Present(PrologueHudViewModel model)
    {
        if (model.Objective != CurrentModel.Objective)
            model = model.WithObjective(model.Objective, 0f);
        else if (model.ObjectiveTransitionSeconds < CurrentModel.ObjectiveTransitionSeconds)
            model = model with
            {
                ObjectiveTransitionSeconds = CurrentModel.ObjectiveTransitionSeconds
            };
        CurrentModel = model;
        Node shards = GetNodeOrNull("Health/Shards");
        if (shards != null)
        {
            for (int index = 0; index < shards.GetChildCount(); index++)
            {
                HealthShardState state = index < model.HealthSegments.Count ? model.HealthSegments[index] :
                    HealthShardState.Empty;
                (shards.GetChild(index) as PrologueHealthShardVisual)?.SetState(state);
            }
        }

        Label threadState = GetNodeOrNull<Label>("ThreadSpool/State");
        if (threadState != null) threadState.Text = string.Empty;

        PresentActionState(model);

        Label objective = GetNodeOrNull<Label>("ObjectiveRibbon/Text");
        if (objective != null) objective.Text = LocalizeFirstLevelObjective(model.Objective);
        Control ribbon = GetNodeOrNull<Control>("ObjectiveRibbon");
        if (ribbon != null) ribbon.Visible = !string.IsNullOrWhiteSpace(model.Objective);
        ApplyObjectivePresentation();

        Control prompt = GetNodeOrNull<Control>("Prompt");
        if (prompt != null) prompt.Visible = !string.IsNullOrWhiteSpace(model.PromptText);
        Label glyph = GetNodeOrNull<Label>("Prompt/Glyph");
        Label promptText = GetNodeOrNull<Label>("Prompt/Text");
        if (glyph != null) glyph.Text = ResolvePromptGlyph(model.PromptGlyph);
        if (promptText != null) promptText.Text = model.PromptText;

        Control boss = GetNodeOrNull<Control>("Boss");
        if (boss != null) boss.Visible = model.BossVisible;
        Label bossName = GetNodeOrNull<Label>("Boss/Name");
        ProgressBar bossHealth = GetNodeOrNull<ProgressBar>("Boss/Health");
        Label bossState = GetNodeOrNull<Label>("Boss/State");
        if (bossName != null) bossName.Text = model.BossName;
        if (bossHealth != null)
        {
            bossHealth.MaxValue = Mathf.Max(1, model.BossMax);
            bossHealth.Value = Mathf.Clamp(model.BossCurrent, 0, model.BossMax);
        }
        if (bossState != null)
        {
            bool english = _language == GameLanguage.English;
            string posture = model.BossExposed ? (english ? "CORE EXPOSED" : "核心暴露") :
                model.BossArmored ? (english ? "ARMOR CLOSED" : "护甲闭合") :
                (english ? "OPEN STANCE" : "攻势开放");
            bossState.Text = $"{(english ? "PHASE" : "阶段")} " +
                             $"{(model.BossPhase >= 2 ? "II" : "I")} · {posture}";
        }

        PrologueRecoveryView recovery = GetNodeOrNull<PrologueRecoveryView>("Recovery");
        if (recovery != null)
        {
            recovery.SetLanguage(_language);
            if (model.RecoveryVisible) recovery.Present(model.RecoveryReason);
            else recovery.Clear();
        }
        PrologueCompletionView completion = GetNodeOrNull<PrologueCompletionView>("Completion");
        if (completion != null)
        {
            completion.SetLanguage(_language);
            if (model.CompletionVisible) completion.Present(model.CompletionTime, model.Deaths,
                model.HiddenFound, model.ShortcutOpen);
            else completion.Clear();
        }
        PresentReticle();
    }

    private void ApplyLayout()
    {
        Vector2 viewport = Size;
        if (viewport.X < 1f || viewport.Y < 1f) viewport = GetViewportRect().Size;
        PrologueHudLayout layout = PrologueHudLayout.Calculate(viewport, _uiScale);
        ApplyRect("Health", layout.HealthRect);
        ApplyRect("ThreadSpool", layout.ThreadRect);
        ApplyRect("ObjectiveRibbon", layout.ObjectiveRect);
        _collapsedObjectiveHeight = layout.ObjectiveRect.Size.Y;
        ApplyRect("Prompt", layout.PromptRect);
        ApplyRect("Boss", layout.BossRect);
        ApplyRect("ModeSelector", layout.ModeSelectorRect);
        ApplyRect("Loadout", layout.LoadoutRect);
        ApplyRect("ResourceCluster", layout.ResourceClusterRect);
        _contentScale = Mathf.Max(.5f, layout.HealthRect.Size.Y / 72f);
        ApplyContentScale(_contentScale);
        ApplyObjectivePresentation();
    }

    private void PresentActionState(PrologueHudViewModel model)
    {
        GetNodeOrNull<PrologueActionModeVisual>("ModeSelector")?.Present(
            new PrologueActionModeFrame(model.ActionMode, model.ActionRejection));
        GetNodeOrNull<PrologueLoadoutVisual>("Loadout")?.Present(PrologueLoadoutFrame.Create(
            model.ActiveSlot, true, false, false, false, model.ActionRejection));
        GetNodeOrNull<PrologueEnergyHeatVisual>("ResourceCluster")?.Present(
            PrologueEnergyHeatFrame.Create(model.EnergyRatio, model.HeatRatio, model.IsOverheated));
        SetVisible("ModeSelector/Attack/SelectedMarker", model.ActionMode == PlayerActionMode.Attack);
        SetVisible("ModeSelector/Grapple/SelectedMarker", model.ActionMode == PlayerActionMode.Grapple);
        for (int slot = 1; slot <= 4; slot++)
            SetVisible($"Loadout/Slot{slot}/SelectedMarker", model.ActiveSlot == slot);
        SetVisible("ResourceCluster/Heat/OverheatMarker", model.IsOverheated);
        SetVisible("Loadout/RejectionMarker", model.ActionRejection != PlayerActionRejectionReason.None);
        Label rejection = GetNodeOrNull<Label>("Loadout/RejectionMarker");
        if (rejection != null) rejection.TooltipText = model.ActionRejection.ToString();
        ProgressBar energy = GetNodeOrNull<ProgressBar>("ResourceCluster/Energy");
        if (energy != null) energy.Value = model.EnergyRatio;
        ProgressBar heat = GetNodeOrNull<ProgressBar>("ResourceCluster/Heat");
        if (heat != null) heat.Value = model.HeatRatio;
    }

    private void PresentReticle()
    {
        PrologueAimReticle reticle = GetNodeOrNull<PrologueAimReticle>("AimReticle");
        if (reticle == null) return;
        bool visible = !CurrentModel.CompletionVisible && !CurrentModel.RecoveryVisible;
        if (_input == null)
        {
            reticle.Visible = visible;
            return;
        }
        PrologueAimReticleState state = CurrentModel.ActionMode == PlayerActionMode.Attack
            ? PrologueAimReticleState.Attack : ResolveGrappleReticle();
        Vector2 screen = GetViewport().GetCanvasTransform() * _input.Current.MouseWorld;
        Vector2 viewport = Size.X > 1f && Size.Y > 1f ? Size : GetViewportRect().Size;
        reticle.Present(new PrologueAimReticleFrame(state, screen, visible),
            new Rect2(new Vector2(48, 40), viewport - new Vector2(96, 80)),
            WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(this));
    }

    private PrologueAimReticleState ResolveGrappleReticle()
    {
        MechanicalClawFrame frame = _mechanicalClaw?.CurrentFrame ?? MechanicalClawFrame.Idle();
        if (frame.Failure == MechanicalClawFailure.IceSlip ||
            _mechanicalClaw?.CurrentTarget?.Material == GrappleSurfaceMaterial.Ice)
            return PrologueAimReticleState.Ice;
        if (frame.Failure == MechanicalClawFailure.Forbidden ||
            _mechanicalClaw?.CurrentTarget?.Material == GrappleSurfaceMaterial.Forbidden)
            return PrologueAimReticleState.Forbidden;
        if (frame.Failure == MechanicalClawFailure.Obstructed)
            return PrologueAimReticleState.Obstructed;
        if (frame.State is MechanicalClawState.Released or MechanicalClawState.Retract)
            return PrologueAimReticleState.OutOfRange;
        if (frame.Failure is MechanicalClawFailure.OutOfRange or MechanicalClawFailure.NoTarget)
            return PrologueAimReticleState.OutOfRange;
        return _mechanicalClaw?.CurrentTarget != null || frame.State is MechanicalClawState.Latched or
            MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost
            ? PrologueAimReticleState.Grippable
            : PrologueAimReticleState.OutOfRange;
    }

    private void ApplyContentScale(float scale)
    {
        HBoxContainer shards = GetNodeOrNull<HBoxContainer>("Health/Shards");
        if (shards != null)
        {
            shards.Position = new Vector2(0f, 8f) * scale;
            shards.Size = new Vector2(190f, 42f) * scale;
            shards.AddThemeConstantOverride("separation", Mathf.RoundToInt(5f * scale));
            foreach (Node child in shards.GetChildren())
                if (child is Control shard)
                    shard.CustomMinimumSize = new Vector2(34f, 42f) * scale;
        }

        ApplyRule("ObjectiveRibbon", scale);
        ApplyRule("Prompt", scale);
        ApplyRule("Boss", scale);
        ScaleActionRouterContents("ModeSelector", scale);
        ScaleActionRouterContents("Loadout", scale);
        ScaleActionRouterContents("ResourceCluster", scale);

        Label objective = GetNodeOrNull<Label>("ObjectiveRibbon/Text");
        objective?.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(15f * scale));

        Label glyph = GetNodeOrNull<Label>("Prompt/Glyph");
        if (glyph != null)
        {
            glyph.Position = new Vector2(12f, 8f) * scale;
            glyph.Size = new Vector2(112f, 40f) * scale;
            glyph.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(19f * scale));
        }
        Label promptText = GetNodeOrNull<Label>("Prompt/Text");
        if (promptText != null)
        {
            promptText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            promptText.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            promptText.ClipText = false;
            promptText.VerticalAlignment = VerticalAlignment.Center;
            promptText.Position = new Vector2(132f, 4f) * scale;
            Control prompt = GetNodeOrNull<Control>("Prompt");
            promptText.Size = new Vector2(
                Mathf.Max(1f, (prompt?.Size.X ?? 390f * scale) - 144f * scale),
                50f * scale);
            promptText.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(16f * scale));
        }

        Label bossName = GetNodeOrNull<Label>("Boss/Name");
        if (bossName != null)
        {
            bossName.Position = new Vector2(18f, 3f) * scale;
            bossName.Size = new Vector2(320f, 22f) * scale;
            bossName.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(17f * scale));
        }
        ProgressBar bossHealth = GetNodeOrNull<ProgressBar>("Boss/Health");
        if (bossHealth != null)
        {
            bossHealth.Position = new Vector2(18f, 31f) * scale;
            bossHealth.Size = new Vector2(604f, 15f) * scale;
        }
        Label bossState = GetNodeOrNull<Label>("Boss/State");
        if (bossState != null)
        {
            bossState.Position = new Vector2(340f, 3f) * scale;
            bossState.Size = new Vector2(282f, 22f) * scale;
            bossState.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(14f * scale));
        }
    }

    private void ScaleActionRouterContents(string path, float scale)
    {
        Control root = GetNodeOrNull<Control>(path);
        if (root == null) return;
        ScaleDescendants(root, Mathf.Max(.01f, scale));
        root.QueueRedraw();
    }

    private static void ScaleDescendants(Node parent, float scale)
    {
        foreach (Node node in parent.GetChildren())
        {
            if (node is not Control child) continue;
            if (!child.HasMeta("task10_base_position"))
            {
                child.SetMeta("task10_base_position", child.Position);
                child.SetMeta("task10_base_size", child.Size);
            }
            child.Position = child.GetMeta("task10_base_position").AsVector2() * scale;
            child.Size = child.GetMeta("task10_base_size").AsVector2() * scale;
            ScaleDescendants(child, scale);
        }
    }

    private string LocalizeFirstLevelObjective(string objective)
    {
        if (_language == GameLanguage.English) return objective ?? string.Empty;
        return objective switch
        {
            "WAKE IN THE CANYON" => "在峡谷中苏醒",
            "WAKE AND FOLLOW THE BELLS" => "循钟声苏醒",
            "REACH THE CORRIDOR GRAVE LAMP" => "到达走廊墓灯检查点",
            "USE THE MECHANICAL CLAW" => "使用机械爪",
            "CHECKPOINT LIT" => "墓灯已点亮",
            "CHECKPOINT RESTORED" => "墓灯重燃",
            "RETURNED TO CHECKPOINT" => "已返回墓灯",
            "DEATH RESET" => "倒下 · 墓灯重燃",
            "FALL RESET" => "坠落 · 墓灯重燃",
            "ASCEND THROUGH THE CANYON" => "穿越峡谷向上",
            "REACH THE CANYON BASIN" => "抵达峡谷盆地",
            "LEARN THE ENERGY SHIELD" => "学会使用能量盾",
            "CLIMB THE CANYON" => "攀上峡谷",
            "ENTER THE LOST TEMPLE" => "进入遗落神殿",
            "PULL THE TEMPLE MECHANISM" => "拉动神殿机关",
            "OPEN THE BELL-CHAIN SHORTCUT" => "开启钟链捷径",
            "DEFEAT THE FALLEN BELLKEEPER" => "击败坠落钟守",
            "SELECT ATTACK MODE" => "选择攻击模式",
            "SELECT GRAPPLE MODE" => "选择机械爪模式",
            "PROLOGUE COMPLETE — THE LAST GRAVEKEEPER REMEMBERS" =>
                "序章完成——最后的守墓人忆起往昔",
            _ => objective ?? string.Empty
        };
    }

    private void ApplyObjectivePresentation()
    {
        Control ribbon = GetNodeOrNull<Control>("ObjectiveRibbon");
        Label text = GetNodeOrNull<Label>("ObjectiveRibbon/Text");
        if (ribbon == null || text == null) return;
        float height = CurrentModel.ObjectiveExpanded
            ? _collapsedObjectiveHeight * 1.42f
            : _collapsedObjectiveHeight;
        ribbon.Size = new Vector2(ribbon.Size.X, height);
        text.Position = new Vector2(14f, 2f) * _contentScale;
        text.Size = new Vector2(Mathf.Max(1f, ribbon.Size.X - 28f * _contentScale),
            Mathf.Max(1f, height - 4f * _contentScale));
        text.AutowrapMode = CurrentModel.ObjectiveExpanded
            ? TextServer.AutowrapMode.WordSmart
            : TextServer.AutowrapMode.Off;
        text.TextOverrunBehavior = CurrentModel.ObjectiveExpanded
            ? TextServer.OverrunBehavior.TrimWordEllipsis
            : TextServer.OverrunBehavior.TrimEllipsis;
    }

    private string ResolvePromptGlyph(string authored)
    {
        string token = (authored ?? string.Empty).Trim().Trim('[', ']').ToUpperInvariant();
        string[] tokens = token.Split('+', System.StringSplitOptions.TrimEntries |
            System.StringSplitOptions.RemoveEmptyEntries);
        var glyphs = new System.Collections.Generic.List<string>(tokens.Length);
        foreach (string part in tokens)
        {
            StringName action = part switch
            {
                "SPACE" or "A" => InputActions.Jump,
                "SHIFT" or "B" => InputActions.Dash,
                "Q" or "X" => InputActions.SelectAttackMode,
                "E" or "Y" => InputActions.SelectGrappleMode,
                "LMB" or "RB" => InputActions.PrimaryAction,
                "RMB" or "LB" => InputActions.Shield,
                "1" or "D←" => InputActions.Slot1,
                "2" or "D↑" => InputActions.Slot2,
                "3" or "D→" => InputActions.Slot3,
                "4" or "D↓" => InputActions.Slot4,
                _ => default
            };
            if (action == default) return authored ?? string.Empty;
            glyphs.Add(InputGlyphResolver.Resolve(_inputDevice, action.ToString()).Text);
        }
        return glyphs.Count == 0 ? authored ?? string.Empty : $"[{string.Join(" + ", glyphs)}]";
    }

    private void ApplyRect(NodePath path, Rect2 rect)
    {
        Control control = GetNodeOrNull<Control>(path);
        if (control == null) return;
        control.Position = rect.Position;
        control.Size = rect.Size;
    }

    private void ApplyRule(NodePath panelPath, float scale)
    {
        Control panel = GetNodeOrNull<Control>(panelPath);
        Control rule = panel?.GetNodeOrNull<Control>("TopRule");
        if (panel == null || rule == null) return;
        rule.Position = Vector2.Zero;
        rule.Size = new Vector2(panel.Size.X, 2f * scale);
    }

    private void SetVisible(NodePath path, bool visible)
    {
        Control control = GetNodeOrNull<Control>(path);
        if (control != null) control.Visible = visible;
    }

}
