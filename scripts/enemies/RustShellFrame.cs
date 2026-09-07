using Knight.Combat;

namespace Knight.Enemies;

public readonly record struct RustShellFrame(
    RustShellState State,
    float MoveDirection,
    bool AttackActive,
    bool EnteredState,
    AttackTimelineFrame AttackTimeline = default);
