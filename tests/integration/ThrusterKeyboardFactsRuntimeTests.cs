using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Integration;

// Real Godot physical-key map; the adapter is manually sampled. This is not
// the automatic saved-Player physics/ability/OS/human acceptance test.
public static class ThrusterKeyboardFactsRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        PropertyInfo rawProperty = typeof(PlayerInputReader).GetProperty("RawFrame");
        TestAssert.True(rawProperty != null, "Raw input facts are unavailable");
        TestAssert.True(rawProperty.PropertyType == typeof(PlayerInputFrame) &&
            rawProperty.GetMethod?.IsPublic == true && rawProperty.SetMethod?.IsPublic != true,
            "raw facts are a public read-only value snapshot");
        TestAssert.True(InputMap.HasAction(InputActions.Dash) && InputMap.HasAction(InputActions.Jump) &&
            InputMap.HasAction(InputActions.Claw), "input test uses the existing production mapping");
        var root = new Node2D { Name = "ThrusterInputFacts", ProcessMode = Node.ProcessModeEnum.Disabled };
        var input = new PlayerInputReader { Name = "Input" };
        root.AddChild(new HealthComponent { Name = "Health" });
        root.AddChild(input);
        tree.Root.AddChild(root);
        PlayerInputFrame Raw() => (PlayerInputFrame)rawProperty.GetValue(input);
        try
        {
            Release();
            await Advance(tree);
            KeyEvent(Key.Shift, true);
            KeyEvent(Key.Space, true);
            KeyEvent(Key.K, true);
            input._PhysicsProcess(1.0 / 60);
            TestAssert.True(Raw().DashPressed && Raw().JumpPressed && Raw().ClawPressed,
                "same native key sample retains all original Shift/Space/K facts");
            TestAssert.True(input.CurrentFrame.DashPressed && !input.CurrentFrame.JumpPressed &&
                !input.CurrentFrame.ClawPressed, "accepted command still has the original Shift priority");
            TestAssert.True(input.CurrentFrameSequence == 1UL && Raw().KeyboardActions,
                "raw and accepted frames belong to the same keyboard sample sequence");

            input.ConsumeOneShotEdgesForCurrentPhysicsFrame();
            AssertNoEdges(Raw(), "explicit raw edge consumption");
            AssertNoEdges(input.CurrentFrame, "explicit accepted edge consumption");
            TestAssert.True(Raw().JumpHeld, "consuming edges does not erase a native held key fact");

            input.ResetForGameplayBoundary();
            TestAssert.Equal(default(PlayerInputFrame), Raw(), "boundary reset clears the entire raw snapshot");
            input._PhysicsProcess(1.0 / 60);
            AssertNoEdges(Raw(), "boundary suppresses first raw sample even before native edge expires");
            AssertNoEdges(input.CurrentFrame, "boundary suppresses first accepted sample");
            await Advance(tree);
            input._PhysicsProcess(1.0 / 60);
            AssertNoEdges(Raw(), "held native keys do not create repeated raw edges after frame advance");
            TestAssert.True(input.CurrentFrameSequence == 3UL, "clear/reset never reuses the input sample sequence");

            input.ClearSampledFrame();
            TestAssert.Equal(default(PlayerInputFrame), Raw(), "clear removes raw held and one-shot state");
            TestAssert.Equal(default(PlayerInputFrame), input.CurrentFrame, "clear removes accepted state");
            Release();
            await Advance(tree);
            KeyEvent(Key.Shift, true);
            KeyEvent(Key.Space, true);
            KeyEvent(Key.K, true);
            input._PhysicsProcess(1.0 / 60);
            TestAssert.True(Raw().JumpPressed, "fresh native edge is observed before pause-notification control");
            input._Notification(checked((int)Node.NotificationPaused));
            AssertNoEdges(Raw(), "pause notification consumes raw one-shots");
            input._PhysicsProcess(1.0 / 60);
            AssertNoEdges(Raw(), "pause notification suppresses the next raw sample");
            input._Notification(checked((int)Node.NotificationUnpaused));
            input._PhysicsProcess(1.0 / 60);
            AssertNoEdges(Raw(), "resume notification suppresses next raw sample rather than replaying Shift/Space");
            AssertNoEdges(input.CurrentFrame, "resume suppresses accepted sample too");

            Release();
            await Advance(tree);
            KeyEvent(Key.J, true);
            KeyEvent(Key.K, true);
            KeyEvent(Key.Q, true);
            input._PhysicsProcess(1.0 / 60);
            TestAssert.True(Raw().AttackPressed && Raw().ClawPressed && Raw().InteractPressed,
                "raw J/K/Q facts remain available independently of attack priority");
            TestAssert.True(input.CurrentFrame.AttackPressed && !input.CurrentFrame.ClawPressed &&
                !input.CurrentFrame.InteractPressed, "J still wins over K and Q");
            Release();
            await Advance(tree);
            KeyEvent(Key.L, true);
            KeyEvent(Key.Shift, true);
            KeyEvent(Key.Space, true);
            input._PhysicsProcess(1.0 / 60);
            TestAssert.True(Raw().ShieldHeld && Raw().DashPressed && Raw().JumpPressed,
                "raw guard, Shift and Space remain separate facts");
            TestAssert.True(input.CurrentFrame.ShieldHeld && !input.CurrentFrame.DashPressed &&
                input.CurrentFrame.JumpPressed, "raw guard still blocks Shift and allows the existing unattached jump");
            GD.Print("THRUSTER_INPUT_FACTS_OK physical_key_map=true raw_space_preserved=true boundary_edges_cleared=true priority_preserved=true adapter_manually_sampled=true notifications_manually_called=true automatic_player=false human=false");
        }
        finally
        {
            Release();
            root.Free();
        }
    }

    private static void AssertNoEdges(PlayerInputFrame frame, string reason) =>
        TestAssert.True(!frame.JumpPressed && !frame.DashPressed && !frame.ClawPressed &&
            !frame.AttackPressed && !frame.InteractPressed && !frame.ThreadPressed &&
            !frame.ThreadReleased && !frame.PrimaryPressed && !frame.PrimaryReleased &&
            !frame.AttackModePressed && !frame.GrappleModePressed && frame.RequestedSlot == 0, reason);

    private static void KeyEvent(Key key, bool pressed)
    {
        using var native = new InputEventKey { PhysicalKeycode = key, Pressed = pressed, Echo = false };
        Input.ParseInputEvent(native);
        Input.FlushBufferedEvents();
    }

    private static void Release()
    {
        KeyEvent(Key.Shift, false);
        KeyEvent(Key.Space, false);
        KeyEvent(Key.K, false);
        KeyEvent(Key.J, false);
        KeyEvent(Key.Q, false);
        KeyEvent(Key.L, false);
    }

    private static async Task Advance(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
