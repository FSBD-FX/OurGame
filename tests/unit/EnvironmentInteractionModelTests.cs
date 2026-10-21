using Knight.Interactions;

namespace Knight.Tests.Unit;

public static class EnvironmentInteractionModelTests
{
    public static void Run()
    {
        // Mutations caught: accepting a hidden/disabled target; sorting by discovery order;
        // widening reach; allowing an airborne, paused, dead or invalid candidate.
        var candidates = new[]
        {
            new EnvironmentInteractionCandidate("hidden", 1f, false, true),
            new EnvironmentInteractionCandidate("disabled", 4f, true, false),
            new EnvironmentInteractionCandidate("zeta", 1600f, true, true),
            new EnvironmentInteractionCandidate("Alpha", 1600f, true, true),
            new EnvironmentInteractionCandidate("alpha", 1600f, true, true)
        };
        TestAssert.Equal(3, EnvironmentInteractionModel.Resolve(candidates, true, false),
            "NearestVisibleIdTieBreak filters first and breaks equal distance with Ordinal stable ID");
        TestAssert.Equal(-1, EnvironmentInteractionModel.Resolve(candidates, false, false), "AirborneRejects");
        TestAssert.Equal(-1, EnvironmentInteractionModel.Resolve(candidates, true, true), "BlockedRejects");
        TestAssert.Equal(-1, EnvironmentInteractionModel.Resolve(new[]
        {
            new EnvironmentInteractionCandidate("too-far", 6400.1f, true, true)
        }, true, false), "Beyond80Rejects");
        TestAssert.Equal(0, EnvironmentInteractionModel.Resolve(new[]
        {
            new EnvironmentInteractionCandidate("edge", 6400f, true, true)
        }, true, false), "Exactly80IsReachable");
        TestAssert.Equal(1, EnvironmentInteractionModel.Resolve(new[]
        {
            new EnvironmentInteractionCandidate("a-farther", 400f, true, true),
            new EnvironmentInteractionCandidate("z-nearer", 100f, true, true)
        }, true, false), "DistancePrecedesStableId");
        TestAssert.Equal(-1, EnvironmentInteractionModel.Resolve(new[]
        {
            new EnvironmentInteractionCandidate("", 0f, true, true),
            new EnvironmentInteractionCandidate("nan", float.NaN, true, true),
            new EnvironmentInteractionCandidate("negative", -1f, true, true)
        }, true, false), "MalformedCandidatesReject");
    }
}
