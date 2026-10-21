using System.Collections.Generic;
using Godot;

namespace Knight.Presentation;

public partial class ForegroundOccluder : Node2D
{
    [Export(PropertyHint.Range, "0,1,0.01")] public float RestoredOpacity { get; set; } = 0.82f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BehindOpacity { get; set; } = 0.24f;
    [Export] public float FadeSpeed { get; set; } = 4f;
    [Export] public Rect2 CoverageRect { get; set; } = new(-90f, -360f, 180f, 720f);

    public float CurrentOpacity { get; private set; } = 0.82f;
    public bool PlayerBehind { get; private set; }

    public override void _Ready()
    {
        CurrentOpacity = Mathf.Clamp(RestoredOpacity, 0f, 1f);
        ApplyOpacity();
    }

    public override void _Process(double delta) => AdvanceFade((float)delta);

    public void SetPlayerBehind(bool behind) => PlayerBehind = behind;

    public void SetProtectedRegions(IReadOnlyList<Rect2> protectedScreenRegions)
    {
        if (protectedScreenRegions == null || protectedScreenRegions.Count == 0)
        {
            PlayerBehind = false;
            return;
        }

        Rect2 screenBounds = ScreenBounds();
        PlayerBehind = false;
        foreach (Rect2 region in protectedScreenRegions)
        {
            if (!FinitePositive(region.Size.X) || !FinitePositive(region.Size.Y)) continue;
            if (!screenBounds.Intersects(region)) continue;
            PlayerBehind = true;
            return;
        }
    }

    public void AdvanceFade(float delta)
    {
        if (!float.IsFinite(delta) || delta <= 0f) return;
        float target = PlayerBehind ? BehindOpacity : RestoredOpacity;
        CurrentOpacity = Mathf.MoveToward(CurrentOpacity, Mathf.Clamp(target, 0f, 1f),
            Mathf.Max(0f, FadeSpeed) * delta);
        ApplyOpacity();
    }

    private void ApplyOpacity()
    {
        Color color = Modulate;
        color.A = CurrentOpacity;
        Modulate = color;
    }

    private Rect2 ScreenBounds()
    {
        Transform2D transform = IsInsideTree()
            ? GetGlobalTransformWithCanvas()
            : new Transform2D(0f, Position);
        Vector2 topLeft = transform * CoverageRect.Position;
        Vector2 topRight = transform * new Vector2(CoverageRect.End.X, CoverageRect.Position.Y);
        Vector2 bottomRight = transform * CoverageRect.End;
        Vector2 bottomLeft = transform * new Vector2(CoverageRect.Position.X, CoverageRect.End.Y);
        float minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X),
            Mathf.Min(bottomRight.X, bottomLeft.X));
        float minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y),
            Mathf.Min(bottomRight.Y, bottomLeft.Y));
        float maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X),
            Mathf.Max(bottomRight.X, bottomLeft.X));
        float maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y),
            Mathf.Max(bottomRight.Y, bottomLeft.Y));
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    private static bool FinitePositive(float value) => float.IsFinite(value) && value > 0f;
}
