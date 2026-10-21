using Godot;

namespace Knight.Player;

public sealed class PlayerMotionState
{
    public Vector2 Velocity { get; set; }
    public float CoyoteRemaining { get; set; }
    public float JumpBufferRemaining { get; set; }
    public float DashRemaining { get; set; }
    public bool PreservingGrappleReleaseMomentum { get; set; }
    public bool GrappleRiseProtected { get; set; }
    public bool ThrusterRiseProtected { get; set; }

    public void Reset()
    {
        Velocity = Vector2.Zero;
        CoyoteRemaining = 0f;
        JumpBufferRemaining = 0f;
        DashRemaining = 0f;
        PreservingGrappleReleaseMomentum = false;
        GrappleRiseProtected = false;
        ThrusterRiseProtected = false;
        DashDirection = Vector2.Right;
        AirDashAvailable = true;
        WasOnFloor = false;
    }
    public Vector2 DashDirection { get; set; } = Vector2.Right;
    public bool AirDashAvailable { get; set; } = true;
    public bool WasOnFloor { get; set; }
}
