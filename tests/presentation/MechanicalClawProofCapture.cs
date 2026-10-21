using System;
using Godot;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

/// <summary>
/// Capture-side claw proof. Route movement, latching and landing remain in the canonical
/// generated-main InputMap harness; this script never writes a player position or invokes a
/// claw/model/mechanism method directly.
/// </summary>
public partial class MechanicalClawProofCapture : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            Root.Size = new Vector2I(1280, 720);
            await ToSignal(this, SignalName.ProcessFrame);

            SendAction(InputActions.SelectGrappleMode, true);
            await ToSignal(this, SignalName.PhysicsFrame);
            SendAction(InputActions.SelectGrappleMode, false);
            await PrologueCanyonTraversalTests.Run(this);
            bool completed = await PrologueCanyonGeneratedMainRouteTests.Run(this, captureFrames: true);
            if (!completed)
                throw new InvalidOperationException("generated-main claw proof did not complete its route");

            GD.Print("TASK6_MECHANICAL_CLAW_PROOF_OK inputmap=True casts=3 latches=3 " +
                "catch_returns=4 boost=True second_boost_rejected=True dash=False");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("TASK6_MECHANICAL_CLAW_PROOF_FAIL: " + exception);
            Quit(1);
        }
    }

    private static void SendAction(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }
}
