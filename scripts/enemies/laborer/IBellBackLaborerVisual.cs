using Knight.Presentation;

namespace Knight.Enemies.Laborer;

// Typed visual adapters preserve guard/exposure/recovery instead of flattening to Idle.
public interface IBellBackLaborerVisual : IEntityVisual2D
{
    void ApplyLaborerFrame(BellBackLaborerFrame frame, int facing);
}
