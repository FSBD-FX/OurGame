using System;

namespace Knight.Player;

[Flags]
public enum AcceptedPlayerActions
{
    None = 0, Jump = 1, Attack = 2, Claw = 4, Interact = 8, Dash = 16, Shield = 32
}

public static class PlayerActionCommandModel
{
    public static AcceptedPlayerActions Resolve(PlayerInputFrame frame, bool attached, bool controlsBlocked,
        bool guardAvailable = true)
    {
        if (controlsBlocked) return AcceptedPlayerActions.None;
        // Raw guard still cancels claw and suppresses attached jump even when overheated.
        // A closed, unavailable guard must not swallow a fresh affordable attack.
        if (frame.ShieldHeld)
            return AcceptedPlayerActions.Shield | (!guardAvailable && frame.AttackPressed
                ? AcceptedPlayerActions.Attack : AcceptedPlayerActions.None) |
                (!attached && frame.JumpPressed ? AcceptedPlayerActions.Jump : AcceptedPlayerActions.None);
        if (frame.DashPressed) return AcceptedPlayerActions.Dash;
        if (attached && frame.JumpPressed) return AcceptedPlayerActions.Jump;
        AcceptedPlayerActions result = frame.JumpPressed ? AcceptedPlayerActions.Jump : AcceptedPlayerActions.None;
        if (frame.AttackPressed) return result | AcceptedPlayerActions.Attack;
        if (frame.ClawPressed) return result | AcceptedPlayerActions.Claw;
        if (frame.InteractPressed) return result | AcceptedPlayerActions.Interact;
        return result;
    }
}
