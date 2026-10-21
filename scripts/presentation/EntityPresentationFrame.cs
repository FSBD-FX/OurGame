namespace Knight.Presentation;

public readonly record struct EntityPresentationFrame(
    EntityVisualState State,
    int Facing,
    bool Grounded);
