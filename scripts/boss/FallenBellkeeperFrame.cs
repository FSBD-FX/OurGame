using Knight.Combat;

namespace Knight.Boss;

public readonly record struct FallenBellkeeperFrame(
    FallenBellkeeperState State,
    FallenBellkeeperAttack Attack,
    bool AttackActive,
    bool ArmorClosed,
    bool ChainTargetActive,
    bool EnteredState,
    AttackTimelineFrame AttackTimeline = default);
