using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Abilities;

public sealed class ThreadAbilityModel
{
    private const float ComparisonTolerance = .00001f;

    private readonly ThreadAbilitySettings _settings;
    private readonly HashSet<ulong> _consumedCastIds = new();
    private double _nextUseTime = double.NegativeInfinity;
    private ulong _nextCompatibilityCastId = 1;

    public ThreadAbilityModel(ThreadAbilitySettings settings = null)
    {
        _settings = settings ?? new ThreadAbilitySettings();
    }

    public ThreadCandidate? SelectTarget(Vector2 origin, Vector2 aim, ThreadAimMode mode,
        IReadOnlyList<ThreadCandidate> candidates)
    {
        if (aim.IsZeroApprox()) return null;

        float range = mode == ThreadAimMode.Tap ? _settings.TapAssistRange : _settings.HoldRange;
        float rangeSquared = range * range;
        float minimumDot = Mathf.Cos(Mathf.DegToRad(mode == ThreadAimMode.Tap
            ? _settings.TapConeHalfAngleDegrees
            : _settings.HoldConeHalfAngleDegrees));
        Vector2 normalizedAim = aim.Normalized();
        ThreadCandidate? best = null;
        ThreadTargetPriority bestPriority = default;
        float bestDot = float.NegativeInfinity;
        float bestDistanceSquared = float.PositiveInfinity;

        foreach (ThreadCandidate candidate in candidates)
        {
            if (!candidate.HasLineOfSight || !TryPriority(candidate.Kind, out ThreadTargetPriority priority)) continue;
            Vector2 offset = candidate.Position - origin;
            float distanceSquared = offset.LengthSquared();
            if (Mathf.IsZeroApprox(distanceSquared) || distanceSquared > rangeSquared) continue;

            float aimDot = normalizedAim.Dot(offset / Mathf.Sqrt(distanceSquared));
            if (aimDot < minimumDot) continue;
            if (!best.HasValue || IsBetter(priority, aimDot, distanceSquared, candidate,
                    bestPriority, bestDot, bestDistanceSquared, best.Value))
            {
                best = candidate;
                bestPriority = priority;
                bestDot = aimDot;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    // Temporary shim while ThreadAbility still drives the pre-mode immediate route.
    public ThreadCandidate? SelectTarget(Vector2 origin, Vector2 aim, IReadOnlyList<ThreadCandidate> candidates) =>
        SelectTarget(origin, aim.IsZeroApprox() ? Vector2.Right : aim,
            aim.IsZeroApprox() ? ThreadAimMode.Tap : ThreadAimMode.Hold, candidates);

    public bool CanConsume(double nowSeconds) => nowSeconds >= _nextUseTime;

    public bool Consume(ulong castId, double nowSeconds)
    {
        if (castId == 0) return false;
        if (_consumedCastIds.Contains(castId)) return true;
        if (!CanConsume(nowSeconds)) return false;

        _consumedCastIds.Add(castId);
        _nextUseTime = nowSeconds + _settings.CooldownSeconds;
        return true;
    }

    // Temporary shim while ThreadAbility has not yet been migrated to cast-edge consumption.
    public bool TryConsume(double nowSeconds) => Consume(_nextCompatibilityCastId++, nowSeconds);

    public void Reset()
    {
        _nextUseTime = double.NegativeInfinity;
        _nextCompatibilityCastId = 1;
        _consumedCastIds.Clear();
    }

    public float ReadyRatio(double nowSeconds)
    {
        if (double.IsNegativeInfinity(_nextUseTime) || CanConsume(nowSeconds)) return 1f;
        if (_settings.CooldownSeconds <= 0f) return 1f;
        return Mathf.Clamp(1f - (float)((_nextUseTime - nowSeconds) / _settings.CooldownSeconds), 0f, 1f);
    }

    private static bool TryPriority(ThreadTargetKind kind, out ThreadTargetPriority priority)
    {
        switch (kind)
        {
            case ThreadTargetKind.Mechanism:
            case ThreadTargetKind.HeavyEnemy:
                priority = ThreadTargetPriority.Mechanism;
                return true;
            case ThreadTargetKind.LightEnemy:
                priority = ThreadTargetPriority.PullableEnemy;
                return true;
            case ThreadTargetKind.Anchor:
                priority = ThreadTargetPriority.MovementAnchor;
                return true;
            default:
                priority = default;
                return false;
        }
    }

    private static bool IsBetter(ThreadTargetPriority candidatePriority, float candidateDot,
        float candidateDistanceSquared, ThreadCandidate candidate, ThreadTargetPriority bestPriority,
        float bestDot, float bestDistanceSquared, ThreadCandidate best)
    {
        if (candidatePriority < bestPriority) return true;
        if (candidatePriority > bestPriority) return false;
        if (candidateDot > bestDot + ComparisonTolerance) return true;
        if (candidateDot < bestDot - ComparisonTolerance) return false;
        if (candidateDistanceSquared < bestDistanceSquared - ComparisonTolerance) return true;
        if (candidateDistanceSquared > bestDistanceSquared + ComparisonTolerance) return false;
        return candidate.Id < best.Id;
    }
}
