using Godot;
using Knight.Enemies.Laborer;
using Knight.Ui;

namespace Knight.Prologue.Sample;

public partial class BasinSampleGuideView : CanvasLayer
{
    private const float PanelHeight = 168f;
    private const float PreferredPanelWidth = 410f;
    private const float WideViewportThreshold = 1100f;

    private readonly Label[] _progressSteps = new Label[4];

    private Panel _panel;
    private Label _title;
    private Label _keys;
    private Label _body;
    private BasinCombatReadout _combatReadout;
    private bool _compactCombat;
    private Control _worldFocus;
    private Label _focusLabel;
    private ColorRect _focusTop;
    private ColorRect _focusRight;
    private ColorRect _focusBottom;
    private ColorRect _focusLeft;
    private ColorRect _focusCenter;

    private int _step;
    private string _titleText = string.Empty;
    private string _keysText = string.Empty;
    private string _bodyText = string.Empty;
    private bool _completed;
    private Node2D _focusTarget;
    private Vector2 _focusLocalOffset;
    private string _focusText = string.Empty;

    public override void _Ready()
    {
        Layer = 6;
        BuildPanel();
        BuildWorldFocus();
        PrologueUiPalette.ApplyBorderlessPanels(this);
        _combatReadout = new BasinCombatReadout { Name = "CombatReadout", Visible = false };
        AddChild(_combatReadout);
        GetViewport().SizeChanged += ApplyLayout;
        ApplyLayout();
        ApplyPresentation();
        ApplyWorldFocusState();
    }

    public override void _ExitTree()
    {
        if (GetViewport() != null) GetViewport().SizeChanged -= ApplyLayout;
    }

    public override void _Process(double delta)
    {
        if (!Visible || _focusTarget == null ||
            !GodotObject.IsInstanceValid(_focusTarget))
        {
            if (_worldFocus != null) _worldFocus.Visible = false;
            if (_focusTarget != null && !GodotObject.IsInstanceValid(_focusTarget))
            {
                _focusTarget = null;
                SetProcess(false);
            }
            return;
        }

        UpdateWorldFocusLayout();
    }

    public void Present(int step, string title, string keys, string body, bool completed)
    {
        int normalizedStep = Mathf.Clamp(step, 0, 4);
        string normalizedTitle = title ?? string.Empty;
        string normalizedKeys = keys ?? string.Empty;
        string normalizedBody = body ?? string.Empty;
        if (_step == normalizedStep && _titleText == normalizedTitle &&
            _keysText == normalizedKeys && _bodyText == normalizedBody &&
            _completed == completed)
            return;

        _step = normalizedStep;
        _titleText = normalizedTitle;
        _keysText = normalizedKeys;
        _bodyText = normalizedBody;
        _completed = completed;
        ApplyPresentation();
    }

    public void SetWorldFocus(Node2D target, Vector2 localOffset, string label)
    {
        string normalizedLabel = label ?? string.Empty;
        if (_focusTarget == target && _focusLocalOffset == localOffset &&
            _focusText == normalizedLabel)
            return;

        _focusTarget = target;
        _focusLocalOffset = localOffset;
        _focusText = normalizedLabel;
        ApplyWorldFocusState();
    }

    public void PresentCombat(BellBackLaborer enemy, bool english)
    {
        if (_combatReadout == null) return;
        _combatReadout.Present(enemy, english, _panel.GetGlobalRect());
        bool compact = _combatReadout.Visible && GodotObject.IsInstanceValid(enemy) &&
            enemy.CurrentState is BellBackLaborerState.Telegraph or BellBackLaborerState.Attack or
                BellBackLaborerState.Exposed;
        if (_compactCombat == compact) return;
        _compactCombat = compact;
        ApplyLayout();
    }

    private void BuildPanel()
    {
        _panel = new Panel
        {
            Name = "Panel",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(PrologueUiPalette.Glass, .94f),
            BorderColor = new Color(PrologueUiPalette.Brass, .78f),
            BorderWidthLeft = 1,
            BorderWidthTop = 2,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        });
        AddChild(_panel);

        var progress = new HBoxContainer
        {
            Name = "Progress",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        progress.AddThemeConstantOverride("separation", 14);
        _panel.AddChild(progress);
        for (int index = 0; index < _progressSteps.Length; index++)
        {
            var step = new Label
            {
                Name = $"Step{index + 1}",
                Text = $"· 0{index + 1}",
                CustomMinimumSize = new Vector2(72f, 18f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                FocusMode = Control.FocusModeEnum.None
            };
            step.AddThemeFontSizeOverride("font_size", 12);
            progress.AddChild(step);
            _progressSteps[index] = step;
        }

        _title = BuildLabel("Title", 19, PrologueUiPalette.Bone);
        _title.VerticalAlignment = VerticalAlignment.Center;
        _title.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _title.ClipText = false;
        _panel.AddChild(_title);

        var keysBackdrop = new ColorRect
        {
            Name = "KeysBackdrop",
            Color = new Color(PrologueUiPalette.GlassRaised, .96f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        _panel.AddChild(keysBackdrop);

        _keys = BuildLabel("Keys", 13, PrologueUiPalette.Thread);
        _keys.HorizontalAlignment = HorizontalAlignment.Center;
        _keys.VerticalAlignment = VerticalAlignment.Center;
        _keys.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _keys.ClipText = false;
        _panel.AddChild(_keys);

        _body = BuildLabel("Body", 14, PrologueUiPalette.BoneMuted);
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _body.ClipText = false;
        _body.VerticalAlignment = VerticalAlignment.Top;
        _body.AddThemeConstantOverride("line_spacing", 2);
        _panel.AddChild(_body);
    }

    private void BuildWorldFocus()
    {
        _worldFocus = new Control
        {
            Name = "WorldFocus",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None,
            Visible = false
        };
        _worldFocus.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_worldFocus);

        _focusTop = FocusLine("Top");
        _focusRight = FocusLine("Right");
        _focusBottom = FocusLine("Bottom");
        _focusLeft = FocusLine("Left");
        _focusCenter = new ColorRect
        {
            Name = "Center",
            Color = PrologueUiPalette.Thread,
            Size = new Vector2(4f, 4f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        _worldFocus.AddChild(_focusTop);
        _worldFocus.AddChild(_focusRight);
        _worldFocus.AddChild(_focusBottom);
        _worldFocus.AddChild(_focusLeft);
        _worldFocus.AddChild(_focusCenter);

        _focusLabel = BuildLabel("Label", 13, PrologueUiPalette.Bone);
        _focusLabel.AddThemeColorOverride("font_outline_color", PrologueUiPalette.Ink);
        _focusLabel.AddThemeConstantOverride("outline_size", 4);
        _focusLabel.VerticalAlignment = VerticalAlignment.Center;
        _focusLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimWordEllipsis;
        _worldFocus.AddChild(_focusLabel);
    }

    private void ApplyLayout()
    {
        if (_panel == null) return;
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float margin = viewport.X >= WideViewportThreshold ? 48f : 32f;
        float panelWidth = Mathf.Min(PreferredPanelWidth,
            Mathf.Max(280f, viewport.X - margin * 2f));
        _panel.Position = new Vector2(margin, 185f);
        _panel.Size = new Vector2(panelWidth, _compactCombat ? 100f : PanelHeight);

        Control progress = _panel.GetNode<Control>("Progress");
        progress.Position = new Vector2(16f, 8f);
        progress.Size = new Vector2(panelWidth - 32f, 20f);

        _title.Position = new Vector2(16f, 34f);
        _title.Size = new Vector2(panelWidth - 32f, 28f);
        Control keysBackdrop = _panel.GetNode<Control>("KeysBackdrop");
        keysBackdrop.Position = new Vector2(16f, 66f);
        keysBackdrop.Size = new Vector2(panelWidth - 32f, 26f);
        _keys.Position = keysBackdrop.Position + new Vector2(8f, 0f);
        _keys.Size = keysBackdrop.Size - new Vector2(16f, 0f);
        _body.Position = new Vector2(16f, 101f);
        _body.Size = new Vector2(panelWidth - 32f, 56f);
        _body.Visible = !_compactCombat;

        if (_worldFocus != null)
            _worldFocus.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        if (Visible && _focusTarget != null && GodotObject.IsInstanceValid(_focusTarget))
            UpdateWorldFocusLayout();
    }

    private void ApplyPresentation()
    {
        if (_title == null) return;
        _title.Text = _titleText;
        _keys.Text = _keysText;
        _body.Text = _bodyText;

        bool allComplete = _completed || _step >= _progressSteps.Length;
        int activeStep = Mathf.Clamp(_step, 0, _progressSteps.Length - 1);
        for (int index = 0; index < _progressSteps.Length; index++)
        {
            bool reached = allComplete || index < activeStep;
            bool active = !allComplete && index == activeStep;
            _progressSteps[index].Text = $"{(reached ? "●" : active ? "◆" : "·")} 0{index + 1}";
            _progressSteps[index].AddThemeColorOverride("font_color",
                reached ? PrologueUiPalette.Brass :
                active ? PrologueUiPalette.Thread : new Color(PrologueUiPalette.BoneMuted, .55f));
        }
    }

    private void ApplyWorldFocusState()
    {
        if (_worldFocus == null) return;
        _focusLabel.Text = _focusText;
        bool hasTarget = _focusTarget != null && GodotObject.IsInstanceValid(_focusTarget);
        _worldFocus.Visible = hasTarget && Visible;
        SetProcess(hasTarget);
        if (hasTarget && Visible) UpdateWorldFocusLayout();
    }

    private void UpdateWorldFocusLayout()
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 point = _focusTarget.GetGlobalTransformWithCanvas() * _focusLocalOffset;
        Rect2 visibleArea = new(new Vector2(24f, 24f),
            viewport - new Vector2(48f, 48f));
        if (!visibleArea.HasPoint(point))
        {
            _worldFocus.Visible = false;
            return;
        }

        _worldFocus.Visible = true;
        const float radius = 12f;
        const float arm = 9f;
        const float thickness = 2f;
        _focusTop.Position = point + new Vector2(-arm * .5f, -radius);
        _focusTop.Size = new Vector2(arm, thickness);
        _focusBottom.Position = point + new Vector2(-arm * .5f, radius - thickness);
        _focusBottom.Size = new Vector2(arm, thickness);
        _focusLeft.Position = point + new Vector2(-radius, -arm * .5f);
        _focusLeft.Size = new Vector2(thickness, arm);
        _focusRight.Position = point + new Vector2(radius - thickness, -arm * .5f);
        _focusRight.Size = new Vector2(thickness, arm);
        _focusCenter.Position = point - _focusCenter.Size * .5f;

        const float labelWidth = 150f;
        const float labelHeight = 24f;
        float labelX = point.X + 17f;
        if (labelX + labelWidth > viewport.X - 18f) labelX = point.X - labelWidth - 17f;
        labelX = Mathf.Clamp(labelX, 18f, Mathf.Max(18f, viewport.X - labelWidth - 18f));
        float labelY = Mathf.Clamp(point.Y - labelHeight * .5f, 18f,
            Mathf.Max(18f, viewport.Y - labelHeight - 18f));
        _focusLabel.Position = new Vector2(labelX, labelY);
        _focusLabel.Size = new Vector2(labelWidth, labelHeight);
    }

    private static Label BuildLabel(string name, int fontSize, Color color)
    {
        var label = new Label
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static ColorRect FocusLine(string name) => new()
    {
        Name = name,
        Color = new Color(PrologueUiPalette.Brass, .92f),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        FocusMode = Control.FocusModeEnum.None
    };
}
