namespace Knight.Player;

public readonly record struct AttackFrame(
    ulong AttackId,
    AttackDirection Direction,
    bool IsActive,
    bool Finished);
