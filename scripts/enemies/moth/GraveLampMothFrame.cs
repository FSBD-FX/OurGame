using Knight.Combat;

namespace Knight.Enemies.Moth;

public readonly record struct GraveLampMothFrame(
    GraveLampMothState State,
    float VelocityX,
    float VelocityY,
    bool AttackActive,
    bool EnteredState,
    AttackTimelineFrame AttackTimeline = default);
