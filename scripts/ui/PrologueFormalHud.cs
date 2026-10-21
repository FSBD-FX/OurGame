using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Settings;
using Knight.Prologue;
using Knight.Localization;

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
    private string _contextualObjective = string.Empty;
    private string _coveredEarlyLessonObjective = string.Empty;
    private bool _fragmentTextLocalized;
    private bool _checkpointSaveFailed;
    public bool EarlyLessonRibbonSuppressed { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        PrologueUiPalette.ApplyBorderlessPanels(this);
        ApplyLayout();
        Present(CurrentModel);
    }

    public override void _ExitTree()
    {
        _coveredEarlyLessonObjective = string.Empty;
        EarlyLessonRibbonSuppressed = false;
        _playerPresentation = null;
        _input = null;
        _mechanicalClaw = null;
    }

    public override void _Process(double delta)
    {
        if (CurrentModel.ObjectiveExpanded)
        {
            CurrentModel = CurrentModel.AdvanceObjectiveTransition((float)delta);
            ApplyObjectivePresentation();
        }
        if (!Live(_playerPresentation))
            _playerPresentation = GetNodeOrNull<PlayerPresentationCoordinator>(PlayerPresentationPath);
        if (!Live(_input)) _input = GetNodeOrNull<PlayerInputReader>(InputPath);
        if (!Live(_mechanicalClaw))
            _mechanicalClaw = GetNodeOrNull<MechanicalClawController>(MechanicalClawPath);
        PrologueHudViewModel next = CurrentModel;
        if (Live(_playerPresentation))
        {
            PlayerHudActionState action = _playerPresentation.ActionHudState;
            next = next.WithActionState(action.Mode, action.ActiveSlot, action.EnergyRatio,
                action.HeatRatio, action.IsOverheated, action.Rejection);
        }
        if (next != CurrentModel) Present(next);
        if (!_fragmentTextLocalized && PrologueStrings.KeyCount(_language) > 0)
            PresentFragments(CurrentModel);
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
        ApplyResourceLabelLayout();
        GetNodeOrNull<PrologueRecoveryView>("Recovery")?.SetLanguage(_language);
        GetNodeOrNull<PrologueCompletionView>("Completion")?.SetLanguage(_language);
        Present(CurrentModel);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && IsInsideTree()) ApplyLayout();
    }

    // Presentation only: clearing this must reveal the latest objective, not restore an old one.
    public void SetContextualObjective(string title)
    {
        title ??= string.Empty;
        if (_contextualObjective == title) return;
        _contextualObjective = title;
        if (!string.IsNullOrEmpty(title)) _coveredEarlyLessonObjective = string.Empty;
        Present(CurrentModel);
    }

    // Only guidance owns coverage; the immutable canonical objective and label stay intact.
    public void SetEarlyLessonCoverage(PrologueObjective? objective)
    {
        string covered = objective is PrologueObjective.LearnAttackMode or PrologueObjective.LearnGrappleMode
            ? PrologueObjectiveModel.TextFor(objective.Value) : string.Empty;
        if (_coveredEarlyLessonObjective == covered) return;
        _coveredEarlyLessonObjective = covered;
        Present(CurrentModel);
    }

    public void SetCheckpointSaveFailure(bool failed)
    {
        if (_checkpointSaveFailed == failed) return;
        _checkpointSaveFailed = failed;
        Present(CurrentModel);
    }

    private bool ShowCheckpointSaveFailure => _checkpointSaveFailed &&
        !CurrentModel.RecoveryVisible && !CurrentModel.CompletionVisible;

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
        if (_coveredEarlyLessonObjective != model.Objective || model.RecoveryVisible ||
            model.CompletionVisible || model.BossVisible || !string.IsNullOrEmpty(_contextualObjective))
            _coveredEarlyLessonObjective = string.Empty;
        EarlyLessonRibbonSuppressed = !ShowCheckpointSaveFailure && !string.IsNullOrEmpty(_coveredEarlyLessonObjective);
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

        PresentFragments(model);

        Label threadState = GetNodeOrNull<Label>("ThreadSpool/State");
        if (threadState != null) threadState.Text = string.Empty;

        PresentActionState(model);

        Label objective = GetNodeOrNull<Label>("ObjectiveRibbon/Text");
        if (objective != null) objective.Text = ShowCheckpointSaveFailure
            ? (_language == GameLanguage.English ? "SAVE FAILED · RE-ENTER LAMP" : "墓灯未保存 · 离开后重新接触")
            : !string.IsNullOrEmpty(_contextualObjective) &&
            !model.RecoveryVisible && !model.CompletionVisible && !model.BossVisible
                ? _contextualObjective : LocalizeFirstLevelObjective(model.Objective);
        Control ribbon = GetNodeOrNull<Control>("ObjectiveRibbon");
        if (ribbon != null) ribbon.Visible = (ShowCheckpointSaveFailure || !string.IsNullOrWhiteSpace(model.Objective)) &&
            !EarlyLessonRibbonSuppressed;
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
        ApplyRect("Minimap", layout.MinimapRect);
        _contentScale = Mathf.Max(.5f, layout.HealthRect.Size.Y / 72f);
        ApplyContentScale(_contentScale);
        PresentFragments(CurrentModel);
        ApplyResourceLabelLayout();
        ApplyObjectivePresentation();
    }

    // A card may have been placed while the minimap was hidden. Resolve that
    // first-frame conflict before revealing the map. This only reduces/moves the
    // current rectangle; future guidance protects it, so the two never chase.
    public void AvoidActiveGuidanceForMinimap(Rect2 guidance)
    {
        Control map = GetNodeOrNull<Control>("Minimap");
        if (map == null || !map.GetRect().HasArea() || !guidance.HasArea() || !map.GetGlobalRect().Intersects(guidance.Grow(8))) return;
        Rect2 current = map.GetGlobalRect();
        float rightWidth = Mathf.Max(0, current.End.X - guidance.End.X - 12);
        float bottom = Mathf.Min(GetNode<Control>("ModeSelector").GetGlobalRect().Position.Y,
            GetNode<Control>("Loadout").GetGlobalRect().Position.Y) - 12;
        float belowHeight = Mathf.Max(0, bottom - guidance.End.Y - 12);
        float aboveHeight = Mathf.Max(0, guidance.Position.Y - 12 - current.Position.Y);
        float leftWidth = Mathf.Max(0, guidance.Position.X - 12 - current.Position.X);
        float rightRatio = Mathf.Min(1, rightWidth / current.Size.X);
        float belowRatio = Mathf.Min(1, belowHeight / current.Size.Y);
        float aboveRatio = Mathf.Min(1, aboveHeight / current.Size.Y);
        float leftRatio = Mathf.Min(1, leftWidth / current.Size.X);
        float ratio = Mathf.Max(Mathf.Max(rightRatio, belowRatio), Mathf.Max(aboveRatio, leftRatio));
        Vector2 size = current.Size * ratio;
        Vector2 position = ratio == leftRatio ? current.Position :
            new Vector2(current.End.X - size.X, ratio == belowRatio ? guidance.End.Y + 12 : current.Position.Y);
        map.Position = GetGlobalTransform().AffineInverse() * position;
        map.Size = size;
    }

    private void PresentFragments(PrologueHudViewModel model)
    {
        Control health = GetNodeOrNull<Control>("Health");
        if (health == null) return;
        Label counter = health.GetNodeOrNull<Label>("FragmentCounter");
        if (counter == null)
        {
            counter = new Label
            {
                Name = "FragmentCounter",
                MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.Off,
                TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
                ClipText = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            health.AddChild(counter);
        }

        int missing = Mathf.Max(0, model.FragmentRequired - model.FragmentCount);
        string key = model.CollectionQuotaExempt ? "hud.fragments_legacy" :
            missing > 0 ? "hud.fragments_needed" : "hud.fragments_ready";
        // Child _Ready can precede the UI root's localization load.
        _fragmentTextLocalized = PrologueStrings.KeyCount(_language) > 0;
        counter.Text = _fragmentTextLocalized
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                PrologueStrings.Get(key, _language), model.FragmentCount, model.FragmentRequired, missing)
            : $"{model.FragmentCount}/{model.FragmentRequired}";
        counter.Visible = true;

        // Shards end at y=50; keep all languages inside the reserved y=52..72 strip.
        Vector2 available = new Vector2(220f, 20f) * _contentScale;
        Font font = counter.GetThemeFont("font");
        int fontSize = Mathf.Max(1, Mathf.RoundToInt(13f * _contentScale));
        while (fontSize > 1 &&
            (font.GetStringSize(counter.Text, HorizontalAlignment.Left, -1, fontSize).X > available.X ||
             font.GetHeight(fontSize) > available.Y))
            fontSize--;
        counter.AddThemeFontSizeOverride("font_size", fontSize);
        counter.Position = new Vector2(0f, 52f) * _contentScale;
        counter.Size = available;
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
        if (!Live(_input))
        {
            // A detached source may remain instance-valid with a locked frame.
            // Clear both the draw flag and frame; never present that retired cue.
            Vector2 emptyBounds = Size.X > 1f && Size.Y > 1f ? Size : GetViewportRect().Size;
            reticle.Present(reticle.CurrentFrame with { Visible = false },
                new Rect2(new Vector2(48, 40), emptyBounds - new Vector2(96, 80)));
            return;
        }
        if (!Live(_mechanicalClaw)) _mechanicalClaw = null;
        if (_input.Current.KeyboardActions)
        {
            MechanicalClawFrame claw = _mechanicalClaw?.CurrentFrame ?? MechanicalClawFrame.Idle();
            MechanicalClawTargetResolution preview = _mechanicalClaw?.KeyboardPreview ?? MechanicalClawTargetResolution.None;
            bool locked = claw.LockedTargetRuntimeId != 0UL && claw.EffectiveVisibility &&
                claw.State is MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Boost;
            bool aiming = GodotObject.IsInstanceValid(preview.Surface) && preview.Surface.IsAvailable && preview.Snapshot.Exists;
            Vector2 anchor = locked ? claw.AnchorPoint : preview.Snapshot.WorldAnchor;
            Vector2 bounds = Size.X > 1f && Size.Y > 1f ? Size : GetViewportRect().Size;
            reticle.Present(new PrologueAimReticleFrame(PrologueAimReticleState.Grippable,
                GetViewport().GetCanvasTransform() * anchor, visible && (locked || aiming)),
                new Rect2(new Vector2(48,40), bounds - new Vector2(96,80)),
                WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(this));
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

    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) &&
        node.IsInsideTree() && !node.IsQueuedForDeletion();

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

    private void ApplyResourceLabelLayout()
    {
        Control resources = GetNodeOrNull<Control>("ResourceCluster");
        if (resources == null) return;
        float labelEnd = 0f;
        foreach (string name in new[] { "Energy", "Heat" })
        {
            Label label = resources.GetNodeOrNull<Label>(name + "Label");
            if (label == null || !label.HasMeta("task10_base_size")) return;
            Vector2 authoredSize = label.GetMeta("task10_base_size").AsVector2() * _contentScale;
            Vector2 textSize = label.GetThemeFont("font").GetStringSize(label.Text,
                HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size"));
            // Explicitly resize after language changes: Control grows for ENERGY but
            // does not automatically shrink its old minimum-width rectangle for Chinese.
            label.Size = new Vector2(Mathf.Max(authoredSize.X, textSize.X),
                Mathf.Max(authoredSize.Y, label.GetMinimumSize().Y));
            labelEnd = Mathf.Max(labelEnd, label.Position.X + label.Size.X);
        }
        foreach (string name in new[] { "Energy", "Heat" })
        {
            ProgressBar meter = resources.GetNodeOrNull<ProgressBar>(name);
            if (meter == null || !meter.HasMeta("task10_base_position")) continue;
            Vector2 authoredPosition = meter.GetMeta("task10_base_position").AsVector2() * _contentScale;
            Vector2 authoredSize = meter.GetMeta("task10_base_size").AsVector2() * _contentScale;
            float start = Mathf.Max(authoredPosition.X, labelEnd + 2f * _contentScale);
            // Keep the cluster and meter right edge fixed; only trade a little meter
            // length for the complete caption, without changing text size or resource values.
            meter.Position = new Vector2(start, authoredPosition.Y);
            meter.Size = new Vector2(Mathf.Max(1f, authoredPosition.X + authoredSize.X - start), authoredSize.Y);
            if (meter.GetNodeOrNull<Control>("OverheatMarker") is { } marker)
                marker.Position = new Vector2(Mathf.Max(0f, meter.Size.X - marker.Size.X - 4f * _contentScale),
                    marker.Position.Y);
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
            "STRIKE WITH YOUR WEAPON" => "挥动武器攻击",
            "GRAB A VALID SURFACE" => "抓住有效目标",
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
                "J" => InputActions.Attack,
                "K" => InputActions.Claw,
                "L" => InputActions.KeyboardShield,
                "Q" => InputActions.Interact,
                "X" => InputActions.SelectAttackMode,
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
        rule.Visible = false;
    }

    private void SetVisible(NodePath path, bool visible)
    {
        Control control = GetNodeOrNull<Control>(path);
        if (control != null) control.Visible = visible;
    }

}
