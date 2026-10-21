using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerActionCommandModelTests
{
    private static PlayerInputFrame Edges => new(1f, Vector2.Right, true, true, true, true, false,
        ShieldHeld: true, ClawPressed: true, InteractPressed: true, KeyboardActions: true);

    public static void Run()
    {
        // These catch missing/wrong arbitration branches, not literal config text changes.
        TestAssert.Equal(AcceptedPlayerActions.None,
            PlayerActionCommandModel.Resolve(Edges, true, true), "BlockedConsumesAllEdges");
        TestAssert.Equal(AcceptedPlayerActions.Shield,
            PlayerActionCommandModel.Resolve(Edges, true, false), "ShieldConsumesDashAndAttachedSpace");
        TestAssert.Equal(AcceptedPlayerActions.Dash,
            PlayerActionCommandModel.Resolve(Edges with { ShieldHeld = false }, true, false),
            "DashConsumesAttachedSpaceAndLowerPriority");
        TestAssert.Equal(AcceptedPlayerActions.Jump,
            PlayerActionCommandModel.Resolve(Edges with { ShieldHeld = false, DashPressed = false }, true, false),
            "AttachedSpaceConsumesLowerPriority");
        var ordinary = Edges with { ShieldHeld = false, DashPressed = false };
        TestAssert.Equal(AcceptedPlayerActions.Jump | AcceptedPlayerActions.Attack,
            PlayerActionCommandModel.Resolve(ordinary, false, false), "AttackWinsOverClawAndQ");
        TestAssert.Equal(AcceptedPlayerActions.Jump | AcceptedPlayerActions.Claw,
            PlayerActionCommandModel.Resolve(ordinary with { AttackPressed = false }, false, false),
            "OrdinaryJumpCombinesWithClaw");
        TestAssert.Equal(AcceptedPlayerActions.Interact,
            PlayerActionCommandModel.Resolve(ordinary with { AttackPressed = false, ClawPressed = false,
                JumpPressed = false }, false, false), "QIsLowestIndependentAction");
        TestAssert.Equal(1f, ordinary.MoveAxis, "arbitration does not mutate movement input");
        TestAssert.Equal(AcceptedPlayerActions.Shield | AcceptedPlayerActions.Attack,
            PlayerActionCommandModel.Resolve(Edges, true, false, guardAvailable: false),
            "overheated closed guard permits fresh attack but consumes attached Space and dash");
        TestAssert.Equal(AcceptedPlayerActions.Shield,
            PlayerActionCommandModel.Resolve(Edges with { AttackPressed = false }, true, false, guardAvailable: false),
            "raw held guard suppresses claw and attached Space even when unavailable");
        TestAssert.Equal(AcceptedPlayerActions.Shield | AcceptedPlayerActions.Jump,
            PlayerActionCommandModel.Resolve(Edges, false, false),
            "OrdinaryGuardAllowsJump: held L does not disable a normal grounded jump");
    }
}
