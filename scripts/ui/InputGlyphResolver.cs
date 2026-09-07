using System;
using System.Collections.Generic;
using Knight.Shared;

namespace Knight.Ui;

public enum InputDeviceKind
{
    KeyboardMouse,
    Gamepad
}

public sealed record InputGlyph(string Text, string IconKey);

public static class InputGlyphResolver
{
    private static readonly IReadOnlyDictionary<(InputDeviceKind Device, string Action), InputGlyph> Glyphs =
        new Dictionary<(InputDeviceKind, string), InputGlyph>
        {
            [(InputDeviceKind.KeyboardMouse, InputActions.MoveLeft.ToString())] = new("A", "key-a"),
            [(InputDeviceKind.KeyboardMouse, InputActions.MoveRight.ToString())] = new("D", "key-d"),
            [(InputDeviceKind.KeyboardMouse, InputActions.MoveUp.ToString())] = new("W", "key-w"),
            [(InputDeviceKind.KeyboardMouse, InputActions.MoveDown.ToString())] = new("S", "key-s"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Jump.ToString())] = new("SPACE", "key-space"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Dash.ToString())] = new("SHIFT", "key-shift"),
            [(InputDeviceKind.KeyboardMouse, InputActions.SelectAttackMode.ToString())] = new("Q", "key-q"),
            [(InputDeviceKind.KeyboardMouse, InputActions.SelectGrappleMode.ToString())] = new("E", "key-e"),
            [(InputDeviceKind.KeyboardMouse, InputActions.PrimaryAction.ToString())] = new("LMB", "mouse-left"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Shield.ToString())] = new("RMB", "mouse-right"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Slot1.ToString())] = new("1", "key-1"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Slot2.ToString())] = new("2", "key-2"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Slot3.ToString())] = new("3", "key-3"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Slot4.ToString())] = new("4", "key-4"),
            [(InputDeviceKind.KeyboardMouse, InputActions.Pause.ToString())] = new("ESC", "key-escape"),
            [(InputDeviceKind.Gamepad, InputActions.MoveLeft.ToString())] = new("LS←", "pad-left"),
            [(InputDeviceKind.Gamepad, InputActions.MoveRight.ToString())] = new("LS→", "pad-right"),
            [(InputDeviceKind.Gamepad, InputActions.MoveUp.ToString())] = new("LS↑", "pad-up"),
            [(InputDeviceKind.Gamepad, InputActions.MoveDown.ToString())] = new("LS↓", "pad-down"),
            [(InputDeviceKind.Gamepad, InputActions.Jump.ToString())] = new("A", "pad-south"),
            [(InputDeviceKind.Gamepad, InputActions.Dash.ToString())] = new("B", "pad-east"),
            [(InputDeviceKind.Gamepad, InputActions.SelectAttackMode.ToString())] = new("X", "pad-west"),
            [(InputDeviceKind.Gamepad, InputActions.SelectGrappleMode.ToString())] = new("Y", "pad-north"),
            [(InputDeviceKind.Gamepad, InputActions.PrimaryAction.ToString())] = new("RB", "pad-rb"),
            [(InputDeviceKind.Gamepad, InputActions.Shield.ToString())] = new("LB", "pad-lb"),
            [(InputDeviceKind.Gamepad, InputActions.Slot1.ToString())] = new("D←", "pad-left"),
            [(InputDeviceKind.Gamepad, InputActions.Slot2.ToString())] = new("D↑", "pad-up"),
            [(InputDeviceKind.Gamepad, InputActions.Slot3.ToString())] = new("D→", "pad-right"),
            [(InputDeviceKind.Gamepad, InputActions.Slot4.ToString())] = new("D↓", "pad-down"),
            [(InputDeviceKind.Gamepad, InputActions.Pause.ToString())] = new("MENU", "pad-menu")
        };

    public static InputGlyph Resolve(InputDeviceKind device, string action)
    {
        if (action != null && Glyphs.TryGetValue((device, action), out InputGlyph glyph)) return glyph;
        throw new ArgumentOutOfRangeException(nameof(action), action,
            $"No input glyph is declared for {device} action '{action}'.");
    }
}
