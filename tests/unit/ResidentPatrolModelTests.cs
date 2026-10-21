using System;
using System.Collections.Generic;
using Godot;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;

namespace Knight.Tests.Unit;

public static class ResidentPatrolModelTests
{
    public static IReadOnlyList<string> Run()
    {
        var failures = new List<string>();
        int cases = 0;
        Check("autonomous_patrol_and_endpoint_turn", () =>
        {
            var model = Model();
            TestAssert.Near(-1, model.Step(Input(320)).MoveDirection, 0, "home heads to actual first waypoint, no fake target");
            TestAssert.Near(1, model.Step(Input(256)).MoveDirection, 0, "left waypoint reverses toward right waypoint");
            TestAssert.Near(-1, model.Step(Input(384)).MoveDirection, 0, "right waypoint reverses toward left waypoint");
        });
        Check("blocked_side_turns_without_pose_output", () =>
        {
            ResidentPatrolFrame f = Model().Step(Input(320) with { LeftSafe = false });
            TestAssert.Near(1, f.MoveDirection, 0, "blocked left turns right");
            f = Model().Step(Input(320) with { LeftSafe = false, RightSafe = false });
            TestAssert.Near(0, f.MoveDirection, 0, "no legal direction stops only autonomous movement");
        });
        Check("outside_full_body_returns_and_disallows_combat", () =>
        {
            var model=Model();
            ResidentPatrolFrame f = model.Step(Input(440) with { TargetPermitted = true });
            TestAssert.Equal(ResidentPatrolState.Returning, f.State, "right shoulder outside territory returns");
            TestAssert.Near(-1, f.MoveDirection, 0, "return toward home, no position clamp");
            TestAssert.True(!f.CombatAllowed, "no attack permission outside complete body bounds");
            TestAssert.Equal(ResidentPatrolState.Returning,model.Step(Input(400)).State,"re-entering territory is not yet reaching home");
            TestAssert.Equal(ResidentPatrolState.Patrolling,model.Step(Input(320)).State,"actual home arrival resumes patrol");
        });
        Check("real_permitted_target_engages_only_inside", () =>
        {
            ResidentPatrolFrame f = Model().Step(Input(320) with { TargetPermitted = true });
            TestAssert.Equal(ResidentPatrolState.Engaged, f.State, "real sensing may engage");
            TestAssert.True(f.CombatAllowed && f.MoveDirection == 0, "combat adapter owns chase, patrol does not double-apply");
        });
        Check("suppressed_dormant_dead_have_no_autonomous_authority", () =>
        {
            foreach ((ResidentPatrolInput i, ResidentPatrolState state) in new[]
            {
                (Input(320) with { Suppressed = true, TargetPermitted = true }, ResidentPatrolState.Suppressed),
                (Input(320) with { Dormant = true, TargetPermitted = true }, ResidentPatrolState.Dormant),
                (Input(320) with { Dead = true, TargetPermitted = true }, ResidentPatrolState.Dead)
            })
            {
                ResidentPatrolFrame f = Model().Step(i);
                TestAssert.Equal(state, f.State, "explicit authority state");
                TestAssert.True(f.MoveDirection == 0 && !f.CombatAllowed, "no autonomous attack/move in suppressed state");
            }
        });
        Check("airborne_return_does_not_author_ground_motion", () =>
        {
            ResidentPatrolFrame f = Model().Step(Input(480) with { Supported = false });
            TestAssert.Equal(ResidentPatrolState.Returning, f.State, "airborne external displacement still needs return");
            TestAssert.Near(0, f.MoveDirection, 0, "airborne physics/impulse retained, no pretend ground motion");
        });
        Check("malformed_settings_and_paths_rejected", () =>
        {
            foreach (var settings in new[] { new ResidentPatrolSettings(0), new ResidentPatrolSettings(float.NaN), new ResidentPatrolSettings(48, -1) })
                TestAssert.True(settings.Validate().Count > 0, "invalid tuning rejected");
            Reject(() => new ResidentPatrolModel(null));
            Reject(() => new ResidentPatrolModel(new ResidentPlacementCandidate(new(320,96), new(192,32,256,128), new[] { new Vector2(256,96), new Vector2(500,96) })));
            Reject(() => new ResidentPatrolModel(new ResidentPlacementCandidate(new(320,96), new(192,32,256,128), new[] { new Vector2(256,96), new Vector2(384,97) })));
            Reject(() => new ResidentPatrolModel(new ResidentPlacementCandidate(new(320,96), new(192,32,256,128), Array.Empty<Vector2>())));
        });
        GD.Print($"RESIDENT_PATROL_MODEL_RESULT cases={cases} failures={failures.Count}");
        return failures;

        void Check(string name, Action test)
        {
            cases++;
            try { test(); } catch (Exception ex) { failures.Add(name + ": " + ex.Message); }
        }
    }

    private static ResidentPatrolModel Model() => new(new ResidentPlacementCandidate(new(320,96), new(192,32,256,128), new[] { new Vector2(256,96), new Vector2(384,96) }));
    private static ResidentPatrolInput Input(float x) => new(new(x,96), new(x-18,64,36,64), false, true, true, true);
    private static void Reject(Action action)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        TestAssert.True(rejected, "malformed residence fails before use");
    }
}
