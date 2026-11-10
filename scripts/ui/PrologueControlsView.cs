using Godot;
using Knight.Localization;
using Knight.Settings;
using Knight.Shared;

namespace Knight.Ui;

public partial class PrologueControlsView : Control
{
    private Panel _panel;
    private ScrollContainer _scroll;
    private VBoxContainer _rows;
    private Button _back;
    private Label _scrollHint;
    private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;
    private GameLanguage _language = GameLanguage.ChineseSimplified;

    public override void _Ready()
    {
        _panel = GetNode<Panel>("Panel");
        _scroll = _panel.GetNode<ScrollContainer>("Scroll");
        _rows = _scroll.GetNode<VBoxContainer>("Rows");
        _back = _panel.GetNode<Button>("Back");
        _scrollHint = _panel.GetNode<Label>("ScrollHint");
        Resized += FitPanel;
        _back.GuiInput += OnReferenceKey;
        _scroll.GetVScrollBar().Changed += UpdateScrollHint;
        FitPanel();
    }

    public void Present(InputDeviceKind device, GameLanguage language)
    {
        _device = device;
        _language = language;
        Visible = true;
        Refresh();
        _scroll.ScrollVertical = 0;
    }

    public void SetDevice(InputDeviceKind device)
    {
        if (_device == device) return;
        _device = device;
        if (IsInsideTree() && IsVisibleInTree()) Refresh();
    }

    private void Refresh()
    {
        bool keyboard = _device == InputDeviceKind.KeyboardMouse;
        string left = KeyText(InputActions.MoveLeft), right = KeyText(InputActions.MoveRight);
        string horizontal = $"{left}/{right}";
        GetNode<Label>("Panel/Title").Text = PrologueStrings.Get("menu.controls", _language);
        SetRow("Move", "controls.move", InputActions.MoveLeft, InputActions.MoveRight);
        string aim = keyboard
            ? string.Format(PrologueStrings.Get("controls.aim_keyboard", _language), horizontal)
            : PrologueStrings.Get("controls.aim", _language);
        _rows.GetNode<Label>("Aim").Text =
            $"{aim}    [{KeyText(InputActions.MoveUp)} / {left} / {KeyText(InputActions.MoveDown)} / {right}]";
        SetRow("Jump", "controls.jump", InputActions.Jump);
        SetRow("Dash", "controls.dash", InputActions.Dash);
        string reverse = $"{left} / {right}";
        _rows.GetNode<Label>("Dash").Text += " · " +
            string.Format(PrologueStrings.Get("controls.reverse_brake", _language), reverse);
        SetRow("AttackMode", "controls.attack", InputActions.Attack);
        SetRow("GrappleMode", "controls.thread", InputActions.Claw);
        Label grapple = _rows.GetNode<Label>("GrappleMode");
        grapple.AddThemeFontSizeOverride("font_size", keyboard ? 16 : 20);
        if (keyboard)
            grapple.Text += "\n" + string.Format(PrologueStrings.Get("controls.grapple_keyboard_help", _language),
                InputGlyphResolver.KeyboardAimText(KeyText(InputActions.MoveUp), left, KeyText(InputActions.MoveDown), right),
                horizontal, KeyText(InputActions.Jump), $"{KeyText(InputActions.Attack)}/{KeyText(InputActions.KeyboardShield)}");
        SetRow("Shield", "controls.shield", InputActions.KeyboardShield);
        SetRow("Interact", "controls.interact", InputActions.Interact);
        SetRow("Loadout", "controls.loadout", InputActions.Slot1, InputActions.Slot2,
            InputActions.Slot3, InputActions.Slot4);
        SetRow("Pause", "controls.pause", InputActions.Pause);
        _back.Text = PrologueStrings.Get("settings.back", _language);
        _scrollHint.Text = PrologueStrings.Get("controls.scroll_hint", _language);
        UpdateScrollHint();
    }

    private void FitPanel()
    {
        Vector2 available = GetViewportRect().Size;
        float halfWidth = Mathf.Clamp((available.X - 32f) * .5f, 1f, 410f);
        float halfHeight = Mathf.Clamp((available.Y - 32f) * .5f, 1f, 312f);
        _panel.OffsetLeft = -halfWidth; _panel.OffsetRight = halfWidth;
        _panel.OffsetTop = -halfHeight; _panel.OffsetBottom = halfHeight;
        float contentWidth = Mathf.Max(1f, 2f * halfWidth - 120f - _scroll.GetVScrollBar().GetCombinedMinimumSize().X);
        foreach (Node child in _rows.GetChildren())
            if (child is Label row) row.CustomMaximumSize = new Vector2(contentWidth, -1f);
    }

    private bool HasVerticalOverflow()
    {
        VScrollBar bar = _scroll.GetVScrollBar();
        return bar.MaxValue - bar.Page > bar.MinValue + .5;
    }

    private void UpdateScrollHint() => _scrollHint.Visible = HasVerticalOverflow();

    private void OnReferenceKey(InputEvent input)
    {
        if (!IsVisibleInTree() || !_back.HasFocus() || !HasVerticalOverflow() ||
            input is not InputEventKey { Pressed: true, AltPressed: false, CtrlPressed: false,
                ShiftPressed: false, MetaPressed: false } key) return;
        int page = Mathf.Max(32, Mathf.RoundToInt((float)_scroll.GetVScrollBar().Page) - 32);
        int step = (key.Keycode == Key.None ? key.PhysicalKeycode : key.Keycode) switch
        {
            Key.Up => -32,
            Key.Down => 32,
            Key.Pageup => -page,
            Key.Pagedown => page,
            _ => 0
        };
        if (step == 0) return;
        _scroll.ScrollVertical += step;
        _back.AcceptEvent();
    }

    private string KeyText(StringName action) => _device == InputDeviceKind.KeyboardMouse
        ? InputGlyphResolver.LiveKeyboardText(action, _language)
        : InputGlyphResolver.Resolve(_device, action.ToString()).Text;

    private void SetRow(string row, string key, params StringName[] actions)
    {
        string glyphs = string.Join(" / ", System.Array.ConvertAll(actions, KeyText));
        _rows.GetNode<Label>(row).Text =
            $"{PrologueStrings.Get(key, _language)}    [{glyphs}]";
    }
}
