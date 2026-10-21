namespace Knight.Enemies.Moth;

public readonly record struct GraveLampMothInput(
    bool HasPlayer,
    float SignedPlayerX,
    float SignedPlayerY,
    float AlignmentErrorX,
    float AlignmentErrorY,
    bool WorldContact,
    bool ThreadRequested,
    bool Dead);
