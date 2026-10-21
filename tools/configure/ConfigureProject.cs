using Godot;
using Knight.Shared;

namespace Knight.Tools.Configure;

public partial class ConfigureProject : SceneTree
{
    public override void _Initialize()
    {
        AddAction(InputActions.MoveLeft,
            KeyEvent(Key.A), KeyEvent(Key.Left), Motion(JoyAxis.LeftX, -1.0f), Button(JoyButton.DpadLeft));
        AddAction(InputActions.MoveRight,
            KeyEvent(Key.D), KeyEvent(Key.Right), Motion(JoyAxis.LeftX, 1.0f), Button(JoyButton.DpadRight));
        AddAction(InputActions.MoveUp,
            KeyEvent(Key.W), KeyEvent(Key.Up), Motion(JoyAxis.LeftY, -1.0f), Button(JoyButton.DpadUp));
        AddAction(InputActions.MoveDown,
            KeyEvent(Key.S), KeyEvent(Key.Down), Motion(JoyAxis.LeftY, 1.0f), Button(JoyButton.DpadDown));
        AddAction(InputActions.Jump, KeyEvent(Key.Space), Button(JoyButton.A));
        AddAction(InputActions.Dash, KeyEvent(Key.Shift), Button(JoyButton.B));
        AddAction(InputActions.Attack, KeyEvent(Key.J));
        AddAction(InputActions.Claw, KeyEvent(Key.K));
        AddAction(InputActions.Interact, KeyEvent(Key.Q));
        AddAction(InputActions.KeyboardShield, KeyEvent(Key.L));
        RemoveAction("thread");
        AddAction(InputActions.SelectAttackMode, Button(JoyButton.X));
        AddAction(InputActions.SelectGrappleMode, Button(JoyButton.Y));
        AddAction(InputActions.PrimaryAction, MouseButtonEvent(MouseButton.Left), Button(JoyButton.RightShoulder));
        AddAction(InputActions.Shield, MouseButtonEvent(MouseButton.Right), Button(JoyButton.LeftShoulder));
        AddAction(InputActions.Slot1, KeyEvent(Key.Key1), Button(JoyButton.DpadLeft));
        AddAction(InputActions.Slot2, KeyEvent(Key.Key2), Button(JoyButton.DpadUp));
        AddAction(InputActions.Slot3, KeyEvent(Key.Key3), Button(JoyButton.DpadRight));
        AddAction(InputActions.Slot4, KeyEvent(Key.Key4), Button(JoyButton.DpadDown));
        AddAction(InputActions.Ember, Button(JoyButton.LeftShoulder));
        AddAction(InputActions.EchoRecord, KeyEvent(Key.U), Button(JoyButton.RightShoulder));
        AddAction(InputActions.EchoReplay, KeyEvent(Key.I), Motion(JoyAxis.TriggerLeft, 1.0f));
        AddAction(InputActions.Pause, KeyEvent(Key.Escape), Button(JoyButton.Start));
        ProjectSettings.SetSetting("physics/common/physics_ticks_per_second", 60);

        Error saveError = ProjectSettings.Save();
        if (saveError != Error.Ok)
        {
            GD.PushError($"Unable to save project settings: {saveError}");
            Quit(1);
            return;
        }

        var projectConfig = new ConfigFile();
        Error configLoadError = projectConfig.Load("res://project.godot");
        if (configLoadError != Error.Ok)
        {
            GD.PushError($"Unable to load project configuration: {configLoadError}");
            Quit(1);
            return;
        }

        projectConfig.SetValue("physics", "common/physics_ticks_per_second", 60);
        Error configSaveError = projectConfig.Save("res://project.godot");
        if (configSaveError != Error.Ok)
        {
            GD.PushError($"Unable to persist physics tick rate: {configSaveError}");
            Quit(1);
            return;
        }

        Quit(0);
    }

    private static void AddAction(StringName action, params InputEvent[] events)
    {
        if (InputMap.HasAction(action)) InputMap.EraseAction(action);
        InputMap.AddAction(action, 0.2f);
        foreach (InputEvent inputEvent in events) InputMap.ActionAddEvent(action, inputEvent);

        var serializedEvents = new Godot.Collections.Array<InputEvent>();
        foreach (InputEvent inputEvent in events) serializedEvents.Add(inputEvent);
        var setting = new Godot.Collections.Dictionary
        {
            ["deadzone"] = 0.2f,
            ["events"] = serializedEvents,
        };
        ProjectSettings.SetSetting($"input/{action}", setting);
    }

    private static void RemoveAction(StringName action)
    {
        if (InputMap.HasAction(action)) InputMap.EraseAction(action);
        ProjectSettings.SetSetting($"input/{action}", default(Variant));
    }

    private static InputEventKey KeyEvent(Key key) => new() { PhysicalKeycode = key };

    private static InputEventMouseButton MouseButtonEvent(MouseButton button) => new() { ButtonIndex = button };

    private static InputEventJoypadButton Button(JoyButton button) => new() { ButtonIndex = button };

    private static InputEventJoypadMotion Motion(JoyAxis axis, float value) => new() { Axis = axis, AxisValue = value };
}
