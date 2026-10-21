using System;
using System.Collections.Generic;

namespace Knight.Interactions;

public readonly record struct EnvironmentInteractionCandidate(
    string StableId, float DistanceSquared, bool HasLineOfSight, bool CanInteract);

public static class EnvironmentInteractionModel
{
    public const float DefaultRadius = 80f;

    public static int Resolve(IReadOnlyList<EnvironmentInteractionCandidate> candidates,
        bool grounded, bool controlsBlocked, float radius = DefaultRadius)
    {
        if (!grounded || controlsBlocked || candidates == null || !float.IsFinite(radius) || radius < 0f)
            return -1;
        float maximumDistance = radius * radius;
        int selected = -1;
        for (int index = 0; index < candidates.Count; index++)
        {
            EnvironmentInteractionCandidate candidate = candidates[index];
            if (string.IsNullOrWhiteSpace(candidate.StableId) || !candidate.CanInteract ||
                !candidate.HasLineOfSight || !float.IsFinite(candidate.DistanceSquared) ||
                candidate.DistanceSquared < 0f || candidate.DistanceSquared > maximumDistance)
                continue;
            if (selected < 0 || candidate.DistanceSquared < candidates[selected].DistanceSquared ||
                candidate.DistanceSquared == candidates[selected].DistanceSquared &&
                string.Compare(candidate.StableId, candidates[selected].StableId, StringComparison.Ordinal) < 0)
                selected = index;
        }
        return selected;
    }
}
