using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

public readonly record struct PrologueFogField(Rect2 Bounds, PrologueFogProfile Profile, float BlendWidth);

/// <summary>
/// Read-only opacity model. This does not perform occlusion or render fog;
/// the eventual renderer must constrain both near-clear and beam light by walls.
/// </summary>
public static class PrologueFogModel
{
    // Inward-only candidate transition widths. They do not enlarge authored regions.
    private const double RegionBlendWidth = 64d;
    private const double RestBlendWidth = 32d;
    private static readonly Rect2 BossAndExitBounds = FindBossAndExitBounds();
    // One ordered field definition feeds both CPU evaluation and the display
    // texture. The shader must not carry a second hardcoded copy of the map.
    public static IReadOnlyList<PrologueFogField> Fields { get; } = CreateFields();

    /// <summary>
    /// Resolve/blend density at the world point being shaded, never at the actor's
    /// region. Authored local checkpoint rest bounds also belong in this lookup.
    /// </summary>
    public static PrologueFogProfile ResolveProfile(Vector2 worldPoint)
    {
        PrologueFogProfile profile = PrologueFogProfile.Exploration;
        if (!IsFinite(worldPoint)) return profile;

        foreach (PrologueFogField field in Fields)
            profile = Blend(profile, field.Profile,
                InwardWeight(field.Bounds, worldPoint, field.BlendWidth));
        return profile;
    }

    private static IReadOnlyList<PrologueFogField> CreateFields()
    {
        var fields = new List<PrologueFogField>();
        foreach (CanyonRouteNodeDefinition node in CanyonWorldLayout.Nodes)
        {
            if (node.RestArea || node.Id == CanyonRouteNodeId.BossHall) continue;
            fields.Add(new(node.WorldBounds, PrologueFogProfile.ForRegion(node.Id), (float)RegionBlendWidth));
        }
        // Exact adjacent union: no Boss density leaks into the courtyard or gap.
        fields.Add(new(BossAndExitBounds, PrologueFogProfile.BossHall, (float)RegionBlendWidth));
        foreach (Rect2 rest in CanyonWorldLayout.EncounterRestBounds)
            fields.Add(new(rest, PrologueFogProfile.RestArea, (float)RestBlendWidth));
        return Array.AsReadOnly(fields.ToArray());
    }

    public static float EvaluateOpacity(Vector2 worldPoint, Vector2 actorPosition,
        Vector2 lampPosition, Vector2 lampDirection, PrologueFogProfile profile)
    {
        if (!IsValid(profile)) profile = PrologueFogProfile.Exploration;
        if (!IsFinite(worldPoint) || !IsFinite(actorPosition)) return profile.MaxOpacity;

        double actorDistance = Distance(worldPoint, actorPosition);
        double distanceWeight = Smooth01((actorDistance - profile.ClearRadius) /
            ((double)profile.OuterRadius - profile.ClearRadius));
        double opacity = profile.MaxOpacity * distanceWeight;
        if (opacity == 0d || profile.BeamRange == 0f ||
            !IsFinite(lampPosition) || !IsFinite(lampDirection)) return (float)opacity;

        // Double intermediates keep finite float world coordinates/directions from
        // overflowing during subtraction, squared length, and normalization.
        double directionLength = Math.Sqrt((double)lampDirection.X * lampDirection.X +
            (double)lampDirection.Y * lampDirection.Y);
        if (directionLength == 0d) return (float)opacity;
        double lightX = (double)worldPoint.X - lampPosition.X;
        double lightY = (double)worldPoint.Y - lampPosition.Y;
        double lampDistance = Math.Sqrt(lightX * lightX + lightY * lightY);
        if (lampDistance >= profile.BeamRange) return (float)opacity;

        double angleWeight = 1d;
        if (lampDistance > 0d)
        {
            // atan2(cross, dot) preserves an exact diagonal axis even for a very
            // narrow legal cone. Normalizing before acos amplifies dot rounding.
            // Finite float inputs keep these double products safely in range.
            double cross = lightX * lampDirection.Y - lightY * lampDirection.X;
            double dot = lightX * lampDirection.X + lightY * lampDirection.Y;
            double angle = Math.Atan2(Math.Abs(cross), dot);
            double halfAngle = profile.BeamHalfAngleDegrees * Math.PI / 180d;
            angleWeight = 1d - Smooth01(angle / halfAngle);
        }
        double rangeWeight = 1d - Smooth01(lampDistance / profile.BeamRange);
        return (float)Math.Clamp(opacity * (1d - angleWeight * rangeWeight), 0d, profile.MaxOpacity);
    }

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static bool IsValid(PrologueFogProfile profile) =>
        float.IsFinite(profile.ClearRadius) && profile.ClearRadius >= 0f &&
        float.IsFinite(profile.OuterRadius) && profile.OuterRadius > profile.ClearRadius &&
        float.IsFinite(profile.MaxOpacity) && profile.MaxOpacity >= 0f && profile.MaxOpacity <= 1f &&
        float.IsFinite(profile.BeamRange) && profile.BeamRange >= 0f &&
        float.IsFinite(profile.BeamHalfAngleDegrees) &&
        profile.BeamHalfAngleDegrees > 0f && profile.BeamHalfAngleDegrees <= 90f;

    private static double Distance(Vector2 first, Vector2 second)
    {
        double x = (double)first.X - second.X;
        double y = (double)first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static double Smooth01(double value)
    {
        double t = Math.Clamp(value, 0d, 1d);
        return t * t * (3d - 2d * t);
    }

    private static double InwardWeight(Rect2 bounds, Vector2 point, double width)
    {
        if (!bounds.HasPoint(point)) return 0d;
        double edgeDistance = Math.Min(
            Math.Min((double)point.X - bounds.Position.X, (double)bounds.End.X - point.X),
            Math.Min((double)point.Y - bounds.Position.Y, (double)bounds.End.Y - point.Y));
        return Smooth01(edgeDistance / width);
    }

    private static PrologueFogProfile Blend(PrologueFogProfile from, PrologueFogProfile to, double weight)
    {
        if (weight <= 0d) return from;
        if (weight >= 1d) return to;
        return new PrologueFogProfile(
            Lerp(from.ClearRadius, to.ClearRadius, weight), Lerp(from.OuterRadius, to.OuterRadius, weight),
            Lerp(from.MaxOpacity, to.MaxOpacity, weight), Lerp(from.BeamRange, to.BeamRange, weight),
            Lerp(from.BeamHalfAngleDegrees, to.BeamHalfAngleDegrees, weight));
    }

    private static float Lerp(float from, float to, double weight) =>
        (float)(from + ((double)to - from) * weight);

    private static Rect2 FindBossAndExitBounds()
    {
        Rect2 boss = default;
        Rect2 exit = default;
        foreach (CanyonRouteNodeDefinition node in CanyonWorldLayout.Nodes)
        {
            if (node.Id == CanyonRouteNodeId.BossHall) boss = node.WorldBounds;
            else if (node.Id == CanyonRouteNodeId.SafeExit) exit = node.WorldBounds;
        }
        // Never illuminate a hypothetical gap if a future layout stops sharing
        // this full edge. Unknown geometry retains the exploration fallback.
        if (boss.Size.X <= 0f || boss.Size.Y <= 0f || exit.Size.X <= 0f ||
            boss.End.X != exit.Position.X || boss.Position.Y != exit.Position.Y ||
            boss.Size.Y != exit.Size.Y) return default;
        return new Rect2(boss.Position, new Vector2(boss.Size.X + exit.Size.X, boss.Size.Y));
    }
}
