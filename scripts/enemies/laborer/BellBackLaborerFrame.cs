using Knight.Combat;

namespace Knight.Enemies.Laborer;

public readonly record struct BellBackLaborerFrame(
    BellBackLaborerState State,
    float MoveDirection,
    bool AttackActive,
    bool Guarded,
    bool EnteredState,
    AttackTimelineFrame AttackTimeline = default);
