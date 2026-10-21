using Godot;
using Knight.Shared;
using Knight.Tools;

namespace Knight.Tests.Unit;

public static class ProjectFoundationTests
{
    public static void Run()
    {
        ProjectSettingsAreExplicitAndCorrect();
        VerifyScriptUsesPortableDotnetHosting();
        InputMapHasCompleteBindings();
        SceneBuildUtilPacksACompleteOwnedTree();
        SceneBuildUtilRejectsDroppedDescendantsBelowNestedScenes();
    }

    private static void ProjectSettingsAreExplicitAndCorrect()
    {
        TestAssert.Equal("Knight", (string)ProjectSettings.GetSetting("application/config/assembly_name"),
            "configured assembly name");
        TestAssert.Equal(60, (int)ProjectSettings.GetSetting("physics/common/physics_ticks_per_second"),
            "physics tick rate is 60 Hz");
        string projectText = FileAccess.GetFileAsString(ProjectSettings.GlobalizePath("res://project.godot"));
        TestAssert.True(projectText.Contains("common/physics_ticks_per_second=60"),
            "project explicitly configures the 60 Hz physics tick rate");
        TestAssert.True(!projectText.Contains("config/icon="),
            "project does not reference a missing icon resource");
    }

    private static void InputMapHasCompleteBindings()
    {
        AssertBindings(InputActions.MoveLeft, [Key.A, Key.Left], [JoyButton.DpadLeft], JoyAxis.LeftX, -1.0f);
        AssertBindings(InputActions.MoveRight, [Key.D, Key.Right], [JoyButton.DpadRight], JoyAxis.LeftX, 1.0f);
        AssertBindings(InputActions.MoveUp, [Key.W, Key.Up], [JoyButton.DpadUp], JoyAxis.LeftY, -1.0f);
        AssertBindings(InputActions.MoveDown, [Key.S, Key.Down], [JoyButton.DpadDown], JoyAxis.LeftY, 1.0f);
        AssertBindings(InputActions.Jump, [Key.Space], [JoyButton.A]);
        AssertBindings(InputActions.Dash, [Key.Shift], [JoyButton.B]);
        AssertBindings(InputActions.Attack, [Key.J], []);
        AssertBindings(InputActions.Claw, [Key.K], []);
        AssertBindings(InputActions.Interact, [Key.Q], []);
        AssertBindings(InputActions.KeyboardShield, [Key.L], []);
        TestAssert.True(!InputMap.HasAction("thread"), "old held-thread action remains inactive");
        AssertBindings(InputActions.Ember, [], [JoyButton.LeftShoulder]);
        TestAssert.True(!HasKey(InputMap.ActionGetEvents(InputActions.Ember), Key.L),
            "L belongs only to keyboard guard, not legacy ember");
        AssertBindings(InputActions.EchoRecord, [Key.U], [JoyButton.RightShoulder]);
        AssertBindings(InputActions.EchoReplay, [Key.I], [], JoyAxis.TriggerLeft, 1.0f);
        AssertBindings(InputActions.Pause, [Key.Escape], [JoyButton.Start]);
        AssertBindings(InputActions.SelectAttackMode, [], [JoyButton.X]);
        AssertBindings(InputActions.SelectGrappleMode, [], [JoyButton.Y]);
        TestAssert.True(!HasKey(InputMap.ActionGetEvents(InputActions.SelectAttackMode), Key.Q) &&
            !HasKey(InputMap.ActionGetEvents(InputActions.SelectGrappleMode), Key.E),
            "Q is interaction, with no keyboard Q/E mode prerequisite");
        RequireMouse(InputActions.PrimaryAction, MouseButton.Left);
        RequireButton(InputActions.PrimaryAction, JoyButton.RightShoulder);
        RequireMouse(InputActions.Shield, MouseButton.Right);
        RequireButton(InputActions.Shield, JoyButton.LeftShoulder);
        AssertBindings(InputActions.Slot1, [Key.Key1], [JoyButton.DpadLeft]);
        AssertBindings(InputActions.Slot2, [Key.Key2], [JoyButton.DpadUp]);
        AssertBindings(InputActions.Slot3, [Key.Key3], [JoyButton.DpadRight]);
        AssertBindings(InputActions.Slot4, [Key.Key4], [JoyButton.DpadDown]);
    }

    private static void VerifyScriptUsesPortableDotnetHosting()
    {
        string verifyScript = FileAccess.GetFileAsString(ProjectSettings.GlobalizePath("res://tools/verify.ps1"));
        TestAssert.True(verifyScript.Contains("'G:\\godot\\dotnet-sdk'"),
            "verifier prefers stable English .NET root");
        TestAssert.True(verifyScript.Contains("$env:DOTNET_ROOT = $dotnetRoot"),
            "verifier sets DOTNET_ROOT");
        TestAssert.True(verifyScript.Contains("$env:DOTNET_ROOT_X64 = $dotnetRoot"),
            "verifier sets DOTNET_ROOT_X64");
        TestAssert.True(verifyScript.Contains("$env:DOTNET_ROLL_FORWARD = 'LatestMajor'"),
            "verifier uses LatestMajor roll forward");
    }

    private static void AssertBindings(StringName action, Key[] keys, JoyButton[] buttons,
        JoyAxis? expectedAxis = null, float expectedAxisValue = 0.0f)
    {
        TestAssert.True(InputMap.HasAction(action), $"missing action {action}");
        TestAssert.Near(0.2f, InputMap.ActionGetDeadzone(action), 0.001f, $"deadzone for {action}");

        Godot.Collections.Array<InputEvent> events = InputMap.ActionGetEvents(action);
        foreach (Key key in keys)
            TestAssert.True(HasKey(events, key), $"missing keyboard binding {key} for {action}");
        foreach (JoyButton button in buttons)
            TestAssert.True(HasButton(events, button), $"missing controller binding {button} for {action}");
        if (expectedAxis.HasValue)
            TestAssert.True(HasMotion(events, expectedAxis.Value, expectedAxisValue),
                $"missing controller axis {expectedAxis}={expectedAxisValue} for {action}");
    }

    private static bool HasKey(Godot.Collections.Array<InputEvent> events, Key key)
    {
        foreach (InputEvent inputEvent in events)
            if (inputEvent is InputEventKey keyEvent && keyEvent.PhysicalKeycode == key) return true;
        return false;
    }

    private static void RequireKey(StringName action, Key key)
    {
        TestAssert.True(InputMap.HasAction(action), $"missing action {action}");
        TestAssert.True(HasKey(InputMap.ActionGetEvents(action), key),
            $"missing keyboard binding {key} for {action}");
    }

    private static void RequireMouse(StringName action, MouseButton button)
    {
        TestAssert.True(InputMap.HasAction(action), $"missing action {action}");
        TestAssert.True(HasMouse(InputMap.ActionGetEvents(action), button),
            $"missing mouse binding {button} for {action}");
    }

    private static void RequireButton(StringName action, JoyButton button)
    {
        TestAssert.True(InputMap.HasAction(action), $"missing action {action}");
        TestAssert.True(HasButton(InputMap.ActionGetEvents(action), button),
            $"missing controller binding {button} for {action}");
    }

    private static bool HasMouse(Godot.Collections.Array<InputEvent> events, MouseButton button)
    {
        foreach (InputEvent inputEvent in events)
            if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == button) return true;
        return false;
    }

    private static bool HasButton(Godot.Collections.Array<InputEvent> events, JoyButton button)
    {
        foreach (InputEvent inputEvent in events)
            if (inputEvent is InputEventJoypadButton buttonEvent && buttonEvent.ButtonIndex == button) return true;
        return false;
    }

    private static bool HasMotion(Godot.Collections.Array<InputEvent> events, JoyAxis axis, float value)
    {
        foreach (InputEvent inputEvent in events)
            if (inputEvent is InputEventJoypadMotion motionEvent && motionEvent.Axis == axis &&
                Mathf.IsEqualApprox(motionEvent.AxisValue, value)) return true;
        return false;
    }

    private static void SceneBuildUtilPacksACompleteOwnedTree()
    {
        var root = new Node { Name = "SceneBuildRoot" };
        var child = new Node { Name = "Child" };
        child.AddChild(new Node { Name = "Grandchild" });
        root.AddChild(child);

        Error result = SceneBuildUtil.PackAndSave(root, "user://scene_build_util_test.tscn");
        root.Free();

        TestAssert.Equal(Error.Ok, result, "scene pack and save succeeds for an owned local tree");
        TestAssert.True(FileAccess.FileExists("user://scene_build_util_test.tscn"),
            "packed scene was saved");
    }

    private static void SceneBuildUtilRejectsDroppedDescendantsBelowNestedScenes()
    {
        const string nestedPath = "user://scene_build_util_nested_source.tscn";
        const string rejectedPath = "user://scene_build_util_rejected.tscn";
        DeleteUserFile(nestedPath);
        DeleteUserFile(rejectedPath);

        var nestedRoot = new Node { Name = "NestedRoot" };
        var nestedChild = new Node { Name = "NestedChild" };
        nestedRoot.AddChild(nestedChild);
        nestedChild.Owner = nestedRoot;
        var nestedPacked = new PackedScene();
        TestAssert.Equal(Error.Ok, nestedPacked.Pack(nestedRoot), "nested scene source packs");
        TestAssert.Equal(Error.Ok, ResourceSaver.Save(nestedPacked, nestedPath), "nested scene source saves");
        nestedRoot.Free();

        PackedScene nestedResource = ResourceLoader.Load<PackedScene>(nestedPath);
        Node nestedInstance = nestedResource.Instantiate();
        TestAssert.True(!string.IsNullOrEmpty(nestedInstance.SceneFilePath), "nested instance retains source path");
        nestedInstance.AddChild(new Node { Name = "UnownedLocalDescendant" });

        var root = new Node { Name = "OuterRoot" };
        root.AddChild(nestedInstance);
        Error result = SceneBuildUtil.PackAndSave(root, rejectedPath);
        root.Free();

        TestAssert.Equal(Error.InvalidData, result, "dropped nested-scene descendant is rejected");
        TestAssert.True(!FileAccess.FileExists(rejectedPath), "rejected scene is not saved");
        DeleteUserFile(nestedPath);
    }

    private static void DeleteUserFile(string resourcePath)
    {
        string absolutePath = ProjectSettings.GlobalizePath(resourcePath);
        if (FileAccess.FileExists(resourcePath)) DirAccess.RemoveAbsolute(absolutePath);
    }
}
