namespace Knight.Player;

// Completed native step, never a command or physical velocity authority.
public readonly record struct PlayerGroundMotionObservation(
    ulong OwnerId, ulong Revision, ulong PhysicsTick, float StepSeconds,
    float BeforeX, float IntegratedX, float ActualX, float MoveAxis,
    bool Grounded, bool Eligible, bool ReverseBraking, bool BrakeReachedZero);
