using Godot;
using Knight.Localization;
using Knight.Settings;
using Knight.Shared;

namespace Knight.Ui;

public partial class PrologueControlsView : Control
{
    private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;
    private GameLanguage _language = GameLanguage.ChineseSimplified;

    public void Present(InputDeviceKind device, GameLanguage language)
    {
        _device = device;
        _language = language;
        Visible = true;
        Refresh();
    }

    public void SetDevice(InputDeviceKind device)
    {
        _device = device;
        if (IsInsideTree()) Refresh();
    }

    private void Refresh()
    {
        GetNode<Label>("Panel/Title").Text = PrologueStrings.Get("menu.controls", _language);
        SetRow("Move", "controls.move", InputActions.MoveLeft.ToString(), InputActions.MoveRight.ToString());
        SetRow("Aim", "controls.aim", InputActions.MoveUp.ToString(), InputActions.MoveLeft.ToString(),
            InputActions.MoveDown.ToString(), InputActions.MoveRight.ToString());
        SetRow("Jump", "controls.jump", InputActions.Jump.ToString());
        SetRow("Dash", "controls.dash", InputActions.Dash.ToString());
        string reverse = InputGlyphResolver.Resolve(_device, InputActions.MoveLeft.ToString()).Text + " / " +
            InputGlyphResolver.Resolve(_device, InputActions.MoveRight.ToString()).Text;
        GetNode<Label>("Panel/Rows/Dash").Text += " · " +
            string.Format(PrologueStrings.Get("controls.reverse_brake", _language), reverse);
        SetRow("AttackMode", "controls.attack", InputActions.Attack.ToString());
        SetRow("GrappleMode", "controls.thread", InputActions.Claw.ToString());
        Label grapple = GetNode<Label>("Panel/Rows/GrappleMode");
        grapple.AddThemeFontSizeOverride("font_size",
            _device == InputDeviceKind.KeyboardMouse ? 16 : 20);
        if (_device == InputDeviceKind.KeyboardMouse)
            grapple.Text += "\n" + PrologueStrings.Get("controls.grapple_keyboard_help", _language);
        SetRow("Shield", "controls.shield", InputActions.KeyboardShield.ToString());
        SetRow("Interact", "controls.interact", InputActions.Interact.ToString());
        SetRow("Loadout", "controls.loadout", InputActions.Slot1.ToString(), InputActions.Slot2.ToString(),
            InputActions.Slot3.ToString(), InputActions.Slot4.ToString());
        SetRow("Pause", "controls.pause", InputActions.Pause.ToString());
        GetNode<Button>("Panel/Back").Text = PrologueStrings.Get("settings.back", _language);
    }

    private void SetRow(string row, string key, params string[] actions)
    {
        string glyphs = string.Join(" / ", System.Array.ConvertAll(actions,
            action => InputGlyphResolver.Resolve(_device, action).Text));
        GetNode<Label>($"Panel/Rows/{row}").Text =
            $"{PrologueStrings.Get(key, _language)}    [{glyphs}]";
    }
}
