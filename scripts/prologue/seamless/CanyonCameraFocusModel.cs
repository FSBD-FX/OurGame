using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue.Seamless;

public sealed record CanyonCameraFocusRegion(
    string Id, Rect2 WorldBounds, Vector2 FocusOffset,
    Vector2 VelocityLookAhead, float BlendSeconds);

public readonly record struct CanyonCameraFrame(
    Vector2 LocalPosition, Vector2 FocusOffset, string RegionId);

public sealed class CanyonCameraFocusModel
{
    private readonly IReadOnlyList<CanyonCameraFocusRegion> _regions;
    private readonly Dictionary<string, CanyonCameraFocusRegion> _regionsById;
    private readonly float _maxFallSpeed;

    public CanyonCameraFocusModel(IReadOnlyList<CanyonCameraFocusRegion> regions,
        float maxFallSpeed)
    {
        if (regions == null || regions.Count == 0)
            throw new ArgumentException("At least one camera focus region is required.",
                nameof(regions));
        if (!float.IsFinite(maxFallSpeed) || maxFallSpeed <= 0f)
            throw new ArgumentOutOfRangeException(nameof(maxFallSpeed), maxFallSpeed,
                "Maximum fall speed must be positive and finite.");

        _regionsById = new Dictionary<string, CanyonCameraFocusRegion>(StringComparer.Ordinal);
        foreach (CanyonCameraFocusRegion region in regions)
        {
            if (region == null || string.IsNullOrWhiteSpace(region.Id) ||
                !Finite(region.WorldBounds.Position) || !Finite(region.WorldBounds.Size) ||
                region.WorldBounds.Size.X <= 0f || region.WorldBounds.Size.Y <= 0f ||
                !Finite(region.FocusOffset) || !Finite(region.VelocityLookAhead) ||
                !float.IsFinite(region.BlendSeconds) || region.BlendSeconds <= 0f)
                throw new ArgumentException("Camera focus regions must be finite, positive and named.",
                    nameof(regions));
            if (!_regionsById.TryAdd(region.Id, region))
                throw new ArgumentException($"Duplicate camera focus region ID {region.Id}.",
                    nameof(regions));
        }
        _regions = regions;
        _maxFallSpeed = maxFallSpeed;
    }

    public CanyonCameraFrame ResolveTarget(Vector2 playerWorldPosition, Vector2 velocity,
        Vector2 authoredLocalPosition, Func<Vector2, Vector2> worldToLocal)
    {
        if (!Finite(playerWorldPosition) || !Finite(velocity) || !Finite(authoredLocalPosition))
            throw new ArgumentOutOfRangeException(nameof(playerWorldPosition),
                "Camera inputs must be finite.");
        ArgumentNullException.ThrowIfNull(worldToLocal);

        CanyonCameraFocusRegion region = SelectRegion(playerWorldPosition);
        Vector2 focusOffset = region.FocusOffset;
        if (!Mathf.IsZeroApprox(region.VelocityLookAhead.X))
        {
            float horizontalRatio = Mathf.Clamp(velocity.X / _maxFallSpeed, -1f, 1f);
            focusOffset.X += region.VelocityLookAhead.X * horizontalRatio;
        }
        if (!Mathf.IsZeroApprox(region.VelocityLookAhead.Y))
        {
            float fallRatio = Mathf.Clamp(Mathf.Max(0f, velocity.Y) / _maxFallSpeed, 0f, 1f);
            float downwardLookAhead = Mathf.Lerp(region.FocusOffset.Y,
                region.VelocityLookAhead.Y, fallRatio);
            focusOffset.Y = downwardLookAhead;
        }

        Vector2 worldFocus = playerWorldPosition + focusOffset;
        Vector2 localPosition = authoredLocalPosition + worldToLocal(worldFocus);
        return new CanyonCameraFrame(localPosition, focusOffset, region.Id);
    }

    public CanyonCameraFrame Blend(CanyonCameraFrame current, CanyonCameraFrame target,
        float delta)
    {
        if (!float.IsFinite(delta) || delta < 0f)
            throw new ArgumentOutOfRangeException(nameof(delta), delta,
                "Blend delta must be nonnegative and finite.");
        if (!_regionsById.TryGetValue(target.RegionId, out CanyonCameraFocusRegion region))
            throw new ArgumentException($"Unknown camera focus region {target.RegionId}.",
                nameof(target));
        float weight = 1f - Mathf.Exp(-delta / region.BlendSeconds);
        return new CanyonCameraFrame(
            current.LocalPosition.Lerp(target.LocalPosition, weight),
            current.FocusOffset.Lerp(target.FocusOffset, weight),
            target.RegionId);
    }

    private CanyonCameraFocusRegion SelectRegion(Vector2 position)
    {
        foreach (CanyonCameraFocusRegion region in _regions)
            if (Contains(region.WorldBounds, position)) return region;

        CanyonCameraFocusRegion closest = _regions[0];
        float closestDistance = DistanceSquared(closest.WorldBounds, position);
        for (int index = 1; index < _regions.Count; index++)
        {
            float distance = DistanceSquared(_regions[index].WorldBounds, position);
            if (distance >= closestDistance) continue;
            closest = _regions[index];
            closestDistance = distance;
        }
        return closest;
    }

    private static float DistanceSquared(Rect2 bounds, Vector2 point)
    {
        Vector2 nearest = new(
            Mathf.Clamp(point.X, bounds.Position.X, bounds.End.X),
            Mathf.Clamp(point.Y, bounds.Position.Y, bounds.End.Y));
        return nearest.DistanceSquaredTo(point);
    }

    private static bool Contains(Rect2 bounds, Vector2 point) =>
        point.X >= bounds.Position.X && point.Y >= bounds.Position.Y &&
        point.X <= bounds.End.X && point.Y <= bounds.End.Y;

    private static bool Finite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
