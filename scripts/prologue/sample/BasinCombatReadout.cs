using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Ui;

namespace Knight.Prologue.Sample;

// Session-local presentation: the guide supplies the live actor each frame.
public partial class BasinCombatReadout : Control
{
    private static readonly Vector2 ReadoutSize = new(180f, 30f);
    private const float RearOffset = 110f;
    private const float TopOffset = -90f;
    private Label _state;
    private ProgressBar _health;
    private StyleBoxFlat _healthFill;
    private BellBackLaborer _actor;
    private bool _english;
    private Rect2 _reservedPanel;

    public BasinCombatReadout()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        FocusMode = FocusModeEnum.None;
        Visible = false;
        Size = ReadoutSize;
    }

    public override void _Ready()
    {
        var backdrop = new Panel
        {
            Name = "Backdrop", Size = ReadoutSize,
            MouseFilter = MouseFilterEnum.Ignore, FocusMode = FocusModeEnum.None
        };
        backdrop.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(PrologueUiPalette.Ink, .72f),
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        });
        AddChild(backdrop);
        _state = new Label
        {
            Name = "State", Position = new Vector2(4f, 0f),
            Size = new Vector2(ReadoutSize.X - 8f, 24f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore, FocusMode = FocusModeEnum.None
        };
        _state.AddThemeFontSizeOverride("font_size", 14);
        _state.AddThemeColorOverride("font_outline_color", PrologueUiPalette.Ink);
        _state.AddThemeConstantOverride("outline_size", 2);
        AddChild(_state);
        _health = new ProgressBar
        {
            Name = "Health", Position = new Vector2(12f, 24f),
            ShowPercentage = false, MinValue = 0, Step = 1,
            MouseFilter = MouseFilterEnum.Ignore, FocusMode = FocusModeEnum.None
        };
        _health.AddThemeStyleboxOverride("background", new StyleBoxFlat
        {
            BgColor = new Color(PrologueUiPalette.BoneMuted, .22f)
        });
        _healthFill = new StyleBoxFlat { BgColor = PrologueUiPalette.BoneMuted };
        _health.AddThemeStyleboxOverride("fill", _healthFill);
        AddChild(_health);
        // The default percentage/theme initially enforces a much taller minimum.
        // Set the strip size after installing its actual theme and joining the tree.
        _health.Size = new Vector2(ReadoutSize.X - 24f, 4f);
        Present(_actor, _english, _reservedPanel);
    }

    public void Present(BellBackLaborer actor, bool english, Rect2 reservedPanel)
    {
        _actor = actor;
        _english = english;
        _reservedPanel = reservedPanel;
        Visible = false;
        if (_state == null || !GodotObject.IsInstanceValid(actor) || !actor.IsInsideTree() ||
            !actor.IsVisibleInTree() || actor.CurrentState is BellBackLaborerState.Idle or BellBackLaborerState.Dead)
            return;
        HealthComponent health = actor.GetNodeOrNull<HealthComponent>(actor.HealthPath);
        if (!GodotObject.IsInstanceValid(health) || health.CurrentHealth <= 0) return;

        (string text, Color color) = actor.CurrentState switch
        {
            BellBackLaborerState.Telegraph =>
                (english ? "WINDING UP" : "横扫蓄力", PrologueUiPalette.Brass),
            BellBackLaborerState.Attack =>
                (english ? "SWEEP ACTIVE" : "横扫中", new Color("ef9256")),
            BellBackLaborerState.Exposed =>
                (english ? "ARMOR OPEN" : "开甲", PrologueUiPalette.Thread),
            BellBackLaborerState.Recover =>
                (english ? "RECOVERING · ARMORED" : "收招 · 仍有甲", PrologueUiPalette.BoneMuted),
            _ => (english ? "ARMORED" : "有甲", PrologueUiPalette.BoneMuted)
        };
        _state.Text = text;
        _state.AddThemeColorOverride("font_color", color);
        _health.MaxValue = Mathf.Max(1, health.MaxHealth);
        _health.Value = health.CurrentHealth;
        _healthFill.BgColor = color;

        // Keep the forward-facing swing and its real danger outline unobstructed.
        Vector2 anchor = actor.GetGlobalTransformWithCanvas() * new Vector2(-actor.Facing * RearOffset, TopOffset);
        Position = anchor - new Vector2(ReadoutSize.X * .5f, 0f);
        Rect2 bounds = new(Position, ReadoutSize);
        // Never clamp this enemy-owned readout onto a different screen-space subject.
        Visible = GetViewport().GetVisibleRect().Encloses(bounds) && !reservedPanel.Intersects(bounds);
    }
}
