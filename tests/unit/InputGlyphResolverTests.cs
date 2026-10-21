using System;
using Godot;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Unit;

public static class InputGlyphResolverTests
{
    public static void Run()
    {
        ResolvesEveryImplementedKeyboardAction();
        ResolvesEveryImplementedGamepadAction();
        UnknownActionsAreRejected();
    }

    public static void Task10InputMapHasNoLegacyActions()
    {
        TestAssert.True(!InputMap.HasAction("attack"),
            "Task10 removes the legacy attack action from the live InputMap");
        TestAssert.True(!InputMap.HasAction("thread"),
            "Task10 removes the legacy thread action from the live InputMap");
    }

    private static void ResolvesEveryImplementedKeyboardAction()
    {
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.MoveLeft.ToString(), "A");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.MoveRight.ToString(), "D");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.MoveUp.ToString(), "W");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.MoveDown.ToString(), "S");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Jump.ToString(), "SPACE");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Dash.ToString(), "SHIFT");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Attack.ToString(), "J");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Claw.ToString(), "K");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Interact.ToString(), "Q");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.KeyboardShield.ToString(), "L");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.SelectAttackMode.ToString(), "Q");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.SelectGrappleMode.ToString(), "E");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.PrimaryAction.ToString(), "LMB");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Shield.ToString(), "RMB");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Slot1.ToString(), "1");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Slot2.ToString(), "2");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Slot3.ToString(), "3");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Slot4.ToString(), "4");
        AssertGlyph(InputDeviceKind.KeyboardMouse, InputActions.Pause.ToString(), "ESC");
    }

    private static void ResolvesEveryImplementedGamepadAction()
    {
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.MoveLeft.ToString(), "LS←");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.MoveRight.ToString(), "LS→");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.MoveUp.ToString(), "LS↑");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.MoveDown.ToString(), "LS↓");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Jump.ToString(), "A");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Dash.ToString(), "B");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Attack.ToString(), "X + RB");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Claw.ToString(), "Y + RB");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Interact.ToString(), "—");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.KeyboardShield.ToString(), "LB");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.SelectAttackMode.ToString(), "X");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.SelectGrappleMode.ToString(), "Y");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.PrimaryAction.ToString(), "RB");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Shield.ToString(), "LB");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Slot1.ToString(), "D←");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Slot2.ToString(), "D↑");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Slot3.ToString(), "D→");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Slot4.ToString(), "D↓");
        AssertGlyph(InputDeviceKind.Gamepad, InputActions.Pause.ToString(), "MENU");
    }

    private static void UnknownActionsAreRejected()
    {
        try
        {
            InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse, "unknown");
        }
        catch (ArgumentOutOfRangeException) { return; }
        throw new InvalidOperationException("Assertion failed: unknown input action is rejected");
    }

    private static void AssertGlyph(InputDeviceKind device, string action, string expected)
    {
        InputGlyph glyph = InputGlyphResolver.Resolve(device, action);
        TestAssert.Equal(expected, glyph.Text, $"{device} glyph for {action}");
        TestAssert.True(!string.IsNullOrWhiteSpace(glyph.IconKey), $"{device} icon key for {action}");
    }
}
