using System;
using Godot;

namespace Knight.Presentation;

public enum PlayerLocomotionState
{
    Idle,
    StartRun,
    Run,
    Brake,
    JumpStart,
    Rise,
    Apex,
    Fall,
    SoftLand,
    HardLand,
    Dash,
    Dead,
    Recovery
}

public enum PlayerActionState
{
    None,
    GroundAttack,
    AirAttack,
    DownAttack,
    ThreadPreview,
    ThreadWindup,
    ThreadThrow,
    ThreadLatch,
    ThreadApply,
    ThreadFailed,
    Hurt
}

public readonly record struct PlayerLocomotionFrame
{
    public PlayerLocomotionState State { get; }
    public float NormalizedTime { get; }

    public PlayerLocomotionFrame(PlayerLocomotionState State, float NormalizedTime)
    {
        if (!Enum.IsDefined(State)) throw new ArgumentOutOfRangeException(nameof(State));
        if (float.IsNaN(NormalizedTime) || float.IsInfinity(NormalizedTime))
            throw new ArgumentOutOfRangeException(nameof(NormalizedTime));

        this.State = State;
        this.NormalizedTime = Math.Clamp(NormalizedTime, 0f, 1f);
    }

    internal static PlayerLocomotionFrame FromLegacy(EntityPresentationFrame frame) => new(
        frame.State switch
        {
            EntityVisualState.Idle => PlayerLocomotionState.Idle,
            EntityVisualState.Move => PlayerLocomotionState.Run,
            EntityVisualState.Rise => PlayerLocomotionState.Rise,
            EntityVisualState.Fall => PlayerLocomotionState.Fall,
            EntityVisualState.Dash => PlayerLocomotionState.Dash,
            EntityVisualState.Dead => PlayerLocomotionState.Dead,
            _ => frame.Grounded ? PlayerLocomotionState.Idle : PlayerLocomotionState.Fall
        },
        0f);
}

public enum PlayerActionPhase
{
    None,
    Anticipation,
    Burst,
    Contact,
    Recovery
}

public readonly record struct PlayerActionFrame
{
    public PlayerActionState State { get; }
    public PlayerActionPhase Phase { get; }
    public float NormalizedTime { get; }
    public bool RecoveryCancellable { get; }
    public Vector2 Aim { get; }

    public static PlayerActionFrame None { get; } = new(
        PlayerActionState.None,
        PlayerActionPhase.None,
        0f,
        false);

    public PlayerActionFrame(
        PlayerActionState State,
        PlayerActionPhase Phase,
        float NormalizedTime,
        bool RecoveryCancellable,
        Vector2 Aim = default)
    {
        if (!Enum.IsDefined(State)) throw new ArgumentOutOfRangeException(nameof(State));
        if (!Enum.IsDefined(Phase)) throw new ArgumentOutOfRangeException(nameof(Phase));
        if (float.IsNaN(NormalizedTime) || float.IsInfinity(NormalizedTime))
            throw new ArgumentOutOfRangeException(nameof(NormalizedTime));
        if ((State == PlayerActionState.None) != (Phase == PlayerActionPhase.None))
            throw new ArgumentOutOfRangeException(nameof(Phase),
                "None action state and phase must be used together.");
        if (RecoveryCancellable && Phase != PlayerActionPhase.Recovery)
            throw new ArgumentOutOfRangeException(nameof(RecoveryCancellable),
                "Only Recovery may be movement/jump cancellable.");
        if (!float.IsFinite(Aim.X) || !float.IsFinite(Aim.Y))
            throw new ArgumentOutOfRangeException(nameof(Aim));

        this.State = State;
        this.Phase = Phase;
        this.NormalizedTime = Math.Clamp(NormalizedTime, 0f, 1f);
        this.RecoveryCancellable = RecoveryCancellable;
        this.Aim = State == PlayerActionState.None ? Vector2.Zero : NormalizeAim(Aim);
    }

    private static Vector2 NormalizeAim(Vector2 aim)
    {
        double maximum = Math.Max(Math.Abs((double)aim.X), Math.Abs((double)aim.Y));
        if (maximum <= 0d) return Vector2.Zero;

        double scaledX = aim.X / maximum;
        double scaledY = aim.Y / maximum;
        double scaledLength = Math.Sqrt(scaledX * scaledX + scaledY * scaledY);
        if (maximum * scaledLength <= .001d) return Vector2.Zero;
        return new Vector2(
            (float)(scaledX / scaledLength),
            (float)(scaledY / scaledLength));
    }
}

public readonly record struct PlayerPresentationFrame(
    EntityPresentationFrame Base,
    PlayerLocomotionFrame Locomotion,
    PlayerActionFrame Action,
    SecondaryMotionFrame Secondary)
{
    public PlayerPresentationFrame(
        EntityPresentationFrame Base,
        PlayerActionFrame Action,
        SecondaryMotionFrame Secondary)
        : this(Base, PlayerLocomotionFrame.FromLegacy(Base), Action, Secondary)
    {
    }
}
