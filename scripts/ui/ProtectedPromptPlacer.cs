using System.Collections.Generic;
using Godot;

namespace Knight.Ui;

public static class ProtectedPromptPlacer
{
    private const float TargetGap = 16f;
    private const float ProtectionPadding = 8f;

    public static Rect2 Place(Rect2 safeArea, Vector2 target, Vector2 requestedSize,
        IReadOnlyList<Rect2> protectedRegions)
    {
        Vector2 size = new(
            Mathf.Clamp(requestedSize.X, 1f, Mathf.Max(1f, safeArea.Size.X)),
            Mathf.Clamp(requestedSize.Y, 1f, Mathf.Max(1f, safeArea.Size.Y)));
        Vector2 half = size * .5f;
        Vector2[] centers =
        [
            target + Vector2.Up * (half.Y + TargetGap),
            target + Vector2.Right * (half.X + TargetGap),
            target + Vector2.Down * (half.Y + TargetGap),
            target + Vector2.Left * (half.X + TargetGap),
            target + new Vector2(half.X + TargetGap, half.Y + TargetGap * 4f),
            target + new Vector2(-half.X - TargetGap, half.Y + TargetGap * 4f),
            target + new Vector2(half.X + TargetGap, -half.Y - TargetGap * 4f),
            target + new Vector2(-half.X - TargetGap, -half.Y - TargetGap * 4f)
        ];

        foreach (Vector2 center in centers)
        {
            Rect2 candidate = ClampToSafe(new Rect2(center - half, size), safeArea);
            if (!IntersectsProtected(candidate, protectedRegions)) return candidate;
        }

        Vector2[] safeEdgeCenters =
        [
            new(safeArea.End.X - half.X, safeArea.Position.Y + half.Y),
            new(safeArea.Position.X + half.X, safeArea.Position.Y + half.Y),
            new(safeArea.End.X - half.X, safeArea.End.Y - half.Y),
            new(safeArea.Position.X + half.X, safeArea.End.Y - half.Y),
            new(safeArea.GetCenter().X, safeArea.Position.Y + half.Y),
            new(safeArea.GetCenter().X, safeArea.End.Y - half.Y),
            new(safeArea.Position.X + half.X, safeArea.GetCenter().Y),
            new(safeArea.End.X - half.X, safeArea.GetCenter().Y)
        ];
        foreach (Vector2 center in safeEdgeCenters)
        {
            Rect2 candidate = ClampToSafe(new Rect2(center - half, size), safeArea);
            if (!IntersectsProtected(candidate, protectedRegions)) return candidate;
        }

        // A default rect is an explicit no-placement result. Callers must suppress the
        // card rather than reusing stale content or accepting unsafe combat geometry.
        return default;
    }

    private static Rect2 ClampToSafe(Rect2 rect, Rect2 safeArea)
    {
        Vector2 position = new(
            Mathf.Clamp(rect.Position.X, safeArea.Position.X, safeArea.End.X - rect.Size.X),
            Mathf.Clamp(rect.Position.Y, safeArea.Position.Y, safeArea.End.Y - rect.Size.Y));
        return new Rect2(position, rect.Size);
    }

    private static bool IntersectsProtected(Rect2 candidate,
        IReadOnlyList<Rect2> protectedRegions)
    {
        if (protectedRegions == null) return false;
        foreach (Rect2 region in protectedRegions)
            if (candidate.Intersects(region.Grow(ProtectionPadding))) return true;
        return false;
    }
}
