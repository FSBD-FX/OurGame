using System;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    public static void RunGroundSeamHandoffControls() => Task10GroundSeamEvidence.RunPublishedPairControls();

    private sealed partial class Task10GroundSeamEvidence
    {
        private readonly record struct SeamJointProof(bool Complete, int Comparisons, float MaximumStep,
            ulong NativeBefore, ulong NativeAfter, string Reason);

        private static SeamJointProof ValidatePublishedSeamHandoff(SeamSample old, SeamSample now,
            ReadOnlySpan<SeamPhysicsStep> steps, ulong motor, ulong revision, ulong left, ulong right)
        {
            if (!old.Walking || !now.Walking || old.Owner != left || now.Owner != right ||
                old.Publication == 0 || old.Publication == ulong.MaxValue || now.Publication != old.Publication + 1 ||
                now.Process < old.Process || old.Motor != motor || now.Motor != motor ||
                old.Revision != revision || now.Revision != revision ||
                !old.Root.IsFinite() || !now.Root.IsFinite() ||
                !old.Feet.Near.IsFinite() || !old.Feet.Far.IsFinite() ||
                !now.Feet.Near.IsFinite() || !now.Feet.Far.IsFinite() ||
                !old.PresentationTime.HasValue || !now.PresentationTime.HasValue ||
                !double.IsFinite(old.PresentationTime.Value) || !double.IsFinite(now.PresentationTime.Value) ||
                now.PresentationTime.Value < old.PresentationTime.Value)
                return new(false, 0, 0, 0, 0, "invalid-or-missing-actual-publication");
            var native = ValidateGroundSeamPhysics(steps, old.Tick, now.Tick, motor, revision, left, right);
            if (!native.Complete)
                return new(false, 0, 0, 0, 0, "native-" + native.Reason);
            if (steps[0].Root.DistanceTo(old.Root) > .001f || steps[^1].Root.DistanceTo(now.Root) > .001f)
                return new(false, 0, 0, native.Before, native.After, "publication-native-root-disagreement");
            var measured = MeasurePair(old.Feet, now.Feet);
            if (measured.Comparisons == 0)
                return new(false, 0, 0, native.Before, native.After, "no-common-stance-foot");
            return new(measured.MaximumStep <= .15f, measured.Comparisons, measured.MaximumStep,
                native.Before, native.After, measured.MaximumStep <= .15f
                    ? "complete-native-and-actual-published-pair" : "planted-world-sole-jump");
        }

        public static void RunPublishedPairControls()
        {
            var old = new SeamSample(450, 11, new(1268, 600), PrologueSectorId.CanyonEntrance, .5f,
                new(new(1260, 636), new(1258, 636), true, true), true, 20, 100, false, 7.5, 3, 2);
            var now = new SeamSample(452, 12, new(1276, 600), PrologueSectorId.CanyonDescent, .61f,
                new(new(1260, 636), new(1258, 636.0001f), true, true), true, 21, 101, false, 7.535, 3, 2);
            SeamPhysicsStep[] native = {
                new(450, 3, 2, 11, new(1268, 600), true),
                new(451, 3, 2, 12, new(1272, 600), true),
                new(452, 3, 2, 12, new(1276, 600), true)
            };
            var proof = ValidatePublishedSeamHandoff(old, now, native, 3, 2, 11, 12);
            TestAssert.True(proof.Complete && proof.Comparisons == 2 && proof.MaximumStep < .001f &&
                proof.NativeBefore == 450 && proof.NativeAfter == 451,
                "WORLD_GROUND_SEAM_JOINT_CONTROL_RED: adjacent actual publications with three complete native steps require measured same-stance proof, never a fabricated middle pose");
            var oneTick = now with { Tick = 451, Root = new(1272, 600) };
            TestAssert.True(ValidatePublishedSeamHandoff(old, oneTick, native.AsSpan(0, 2), 3, 2, 11, 12).Complete,
                "WORLD_GROUND_SEAM_JOINT_CONTROL: original one-tick handoff remains admitted");
            int rejected = 0;
            void Reject(SeamSample before, SeamSample after, SeamPhysicsStep[] input, string why)
            {
                TestAssert.True(!ValidatePublishedSeamHandoff(before, after, input, 3, 2, 11, 12).Complete,
                    $"WORLD_GROUND_SEAM_JOINT_CONTROL: {why} must not become visual/native continuity");
                rejected++;
            }
            Reject(old, now, new[] { native[0], native[2] }, "missing physical step");
            var stale = (SeamPhysicsStep[])native.Clone(); stale[1] = stale[1] with { Revision = 9 };
            Reject(old, now, stale, "revoked middle step");
            Reject(old, now with { Publication = 22 }, native, "omitted actual publication");
            Reject(old, now with { Publication = 20 }, native, "duplicate publication ordinal");
            Reject(old with { Motor = 4 }, now, native, "different endpoint motor");
            Reject(old with { Revision = 1 }, now, native, "stale endpoint revision");
            Reject(old, now with { Owner = 99 }, native, "unrelated endpoint shape");
            Reject(old, now with { Walking = false }, native, "nonwalking endpoint");
            Reject(old, now with { Root = new(1276.1f, 600) }, native, "pose/native root disagreement");
            Reject(old, now with { PresentationTime = null }, native, "missing presentation clock");
            Reject(old, now with { PresentationTime = double.NaN }, native, "nonfinite presentation clock");
            Reject(old, now with { PresentationTime = 7.4 }, native, "backward presentation clock");
            Reject(old, now with { Process = 99 }, native, "backward process clock");
            Reject(old, now with { Tick = 450 }, native, "same-tick fabricated contact handoff");
            Reject(old, now with { Feet = new(new(1260, 636), new(1258, 636), false, false) }, native, "no comparable stance");
            Reject(old, now with { Feet = now.Feet with { Far = new(1262, 636) } }, native, "4px planted-foot teleport");
            Reject(old, now with { Feet = now.Feet with { Far = new(float.NaN, 636) } }, native, "nonfinite sole");
            GD.Print($"WORLD_GROUND_SEAM_JOINT_CONTROLS_OK positive=2 negative={rejected} " +
                "complete_native=true adjacent_publications=true sole_limit=0.15 interpolated_visuals=false native=false");
        }
    }
}
