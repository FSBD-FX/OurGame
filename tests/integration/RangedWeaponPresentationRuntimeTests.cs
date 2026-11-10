using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue.Guidance;

namespace Knight.Tests.Integration;

public static class RangedWeaponPresentationRuntimeTests
{
    private const string TelegraphPath = "res://assets/audio/prologue/sfx/enemies/laborer-telegraph.wav";
    private const string ShotPath = "res://assets/audio/prologue/sfx/enemies/rust-attack.wav";
    private const string RecoveryPath = "res://assets/audio/prologue/sfx/enemies/laborer-recovery.wav";
    private const string HurtPath = "res://assets/audio/prologue/sfx/enemies/laborer-hurt.wav";
    private const string DeathPath = "res://assets/audio/prologue/sfx/enemies/laborer-death.wav";
    private static readonly Rect2 Arena = new(80f, 120f, 1000f, 500f);

    public static async Task Run(SceneTree tree)
    {
        CompleteBodyRigHasRealArtAndMotionContracts();
        TestAssert.True(typeof(RangedWeaponVisual).Assembly.GetType("Knight.Enemies.Ranged.IRangedWeaponVisual2D") is Type contract &&
                contract.IsAssignableFrom(typeof(RangedWeaponVisual)),
            "RANGED_BODY_REPLACEMENT_CONTRACT_RED: real presentation must support an independent complete-body replacement, not require the default concrete visual");
        CurrentAttackReplayAndOverheadContinuity();
        await RealBodiesAndReplacementFollowNativeLifecycle(tree);
        await ImmediateThreadCancellationRejectsRecordedConsumerInput(tree);
        foreach ((RangedWeaponRole role, int shots) in new[]
        {
            (RangedWeaponRole.Scatter, 1), (RangedWeaponRole.Burst, 3),
            (RangedWeaponRole.Missile, 1), (RangedWeaponRole.Laser, 1)
        }) await RealActorWarningAndEmissionAudio(tree, role, shots);
        await ThreadAndCheckpointReleaseRealSource(tree);
        await RealHurtDeathAndAncestryNeverUsePlayerCues(tree);
        await DeathDuringRealBurstSuppressesTailAudio(tree);
        await PausedAndDisabledConsumersDoNotEmitOrConsumeNewPhase(tree);
        await QueuedAncestorsAndDetachedSourcesFailClosed(tree);
        GD.Print("RANGED_PRESENTATION_RUNTIME_OK saved_roles=4 actual_shots=1/3/1/1 manifest=true cancellation=true human_listening=false whole_goal=false");
    }

    // I1: re-presenting an old phase/progress must not restore charge/heat.
    // I2: a continuous aim crossing vertical must not flip the whole body or
    // switch the two disjoint IK elbow branches. Literal .75px/tick bound is
    // deliberately much wider than these .004rad/1ms target transitions need.
    private static void CurrentAttackReplayAndOverheadContinuity()
    {
        var failures = new List<string>();
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            var rig = new RangedCharacterRig(); rig.Reset(role);
            RangedWeaponFrame warning = RigFrame(role, RangedWeaponState.Telegraph, 1);
            rig.Apply(warning with { AttackTimeline = warning.AttackTimeline with { PhaseRemainingSeconds = .25f } });
            rig.Advance(.1d);
            rig.Apply(warning with { AttackTimeline = warning.AttackTimeline with { PhaseRemainingSeconds = .75f } });
            if (MathF.Abs(rig.Pose.Charge - .75f) > .0001f) failures.Add("I1_older_progress_" + role);
            rig.Apply(RigFrame(role, RangedWeaponState.Active, 1, 0, 1));
            RangedWeaponFrame recovery = RigFrame(role, RangedWeaponState.Recovery, 1) with
                { AttackTimeline = RigFrame(role, RangedWeaponState.Recovery, 1).AttackTimeline with { PhaseRemainingSeconds = .25f } };
            rig.Apply(recovery);
            rig.Apply(warning);
            if (rig.Pose.State != RangedWeaponState.Recovery || rig.Pose.Charge != 0f || MathF.Abs(rig.Pose.Heat - .25f) > .0001f)
                failures.Add("I1_old_warning_" + role);
            rig.Apply(recovery);
            rig.Apply(RigFrame(role, RangedWeaponState.Active, 1, 0, 1));
            if (rig.Pose.State != RangedWeaponState.Recovery || MathF.Abs(rig.Pose.Heat - .25f) > .0001f)
                failures.Add("I1_old_active_" + role);
            // New unseen true interval still animates while phase stays forward.
            if (role == RangedWeaponRole.Burst)
            {
                rig.ClearAttack(); rig.Reset(role);
                rig.Apply(RigFrame(role, RangedWeaponState.Active, 1, 0, 1));
                rig.Apply(recovery with { FirstEmissionIndex = 1, EmissionsDue = 2 });
                rig.Advance(1d / 60d);
                TestAssert.True(rig.Pose.Recoil > 0f && rig.Pose.State == RangedWeaponState.Recovery,
                    "I1 legitimate Recovery tail is not discarded by stale-phase protection");
            }
            foreach (float boundary in new[] { Mathf.Pi * .5f, -Mathf.Pi * .5f })
            foreach (RangedWeaponState phase in new[] { RangedWeaponState.Telegraph, RangedWeaponState.Active, RangedWeaponState.Dead })
            {
                rig.Reset(role);
                float before = boundary - .002f, after = boundary + .002f;
                rig.Apply(warning with { AimDirection = new Vector2(MathF.Cos(before), MathF.Sin(before)) });
                rig.Advance(.5d); rig.Advance(.5d);
                RangedWeaponFrame target = RigFrame(role, phase == RangedWeaponState.Dead ? RangedWeaponState.Telegraph : phase,
                    1, phase == RangedWeaponState.Active ? 0 : -1, phase == RangedWeaponState.Active ? 1 : 0) with
                    { AimDirection = new Vector2(MathF.Cos(after), MathF.Sin(after)) };
                rig.Apply(target);
                if (phase == RangedWeaponState.Dead) rig.Apply(RigFrame(role, RangedWeaponState.Dead));
                RangedRigPose previous = rig.Pose;
                bool discontinuity = false;
                for (int tick = 0; tick < 100; tick++)
                {
                    rig.Advance(.001d);
                    RangedRigPose pose = rig.Pose;
                    float elbow = MathF.Max(previous.FarArm.Joint.DistanceTo(pose.FarArm.Joint), previous.NearArm.Joint.DistanceTo(pose.NearArm.Joint));
                    float head = previous.HeadTransform.Origin.DistanceTo(pose.HeadTransform.Origin);
                    if (!float.IsFinite(elbow) || !float.IsFinite(head) || elbow >= .75f || head >= .75f)
                    { discontinuity = true; break; }
                    AssertRigidLimbs(pose, "overhead continuous " + role + "/" + phase);
                    previous = pose;
                }
                if (discontinuity) failures.Add("I2_overhead_" + role + "_" + phase + "_" + (boundary > 0f ? "up" : "down"));
            }
        }
        if (failures.Count > 0) GD.Print("RANGED_RIG_REVIEW_FAILURES " + string.Join(",", failures));
        TestAssert.True(failures.Count == 0, "RANGED_RIG_REVIEW_RED: current-ID replay and continuous overhead geometry must remain forward/continuous");
        GD.Print("RANGED_RIG_REVIEW_OK replay_roles=4 overhead_transitions=24 rigid_lengths=true recovery_tail=true");
    }

    // Breaks caught: identical complete bodies, rigid legs translated as a unit,
    // event-count rather than time motion, replayed emission kicks, cancellation
    // resurrection and texture stretching. Geometry is the renderer's production
    // part layout, NOT a pixel/normal-speed art acceptance claim.
    private static void CompleteBodyRigHasRealArtAndMotionContracts()
    {
        Type rigType = typeof(RangedWeaponVisual).Assembly.GetType("Knight.Enemies.Ranged.RangedCharacterRig");
        TestAssert.True(rigType != null,
            "RANGED_COMPLETE_BODY_RIG_RED: four saved ranged roles need an articulated complete-body rig, not one shared torso with different guns");
        string[] slots = { "TorsoTexture", "HeadTexture", "UpperArmTexture", "ForearmTexture", "ThighTexture", "ShinTexture", "FootTexture", "WeaponTexture" };
        foreach (string slot in slots)
            TestAssert.True(typeof(RangedWeaponVisual).GetProperty(slot)?.PropertyType == typeof(Texture2D),
                "RANGED_REPLACEABLE_ART_RED: borrowed per-part Texture2D slot " + slot);
        var silhouettes = new HashSet<string>();
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            object rig = Activator.CreateInstance(rigType);
            RigInvoke(rig, "Reset", role);
            object rest = RigPose(rig);
            AssertRigidLimbs(rest, "rest " + role);
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Telegraph, 1));
            TestAssert.Equal(RigValue<Transform2D>(rest, "TorsoTransform"), RigValue<Transform2D>(RigPose(rig), "TorsoTransform"),
                "publishing a warning cannot jump body geometry before elapsed time");
            RigInvoke(rig, "Advance", 1d / 60d);
            object braced = RigPose(rig);
            TestAssert.True(RigValue<float>(braced, "Brace") > 0f, "real elapsed time starts shoulder/body brace");
            TestAssert.True(RigValue<Transform2D>(rest, "TorsoTransform") != RigValue<Transform2D>(braced, "TorsoTransform"),
                "warning drives actual body transform, not just a charge label");
            AssertRigidLimbs(braced, "brace " + role);
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Active, 1, 0, 1));
            RigInvoke(rig, "Advance", 1d / 60d);
            object fired = RigPose(rig);
            TestAssert.True(RigValue<float>(fired, "Recoil") > 0f, "unique native emission produces physical recoil displacement");
            AssertRigidLimbs(fired, "fire " + role);
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Active, 1, 0, 1));
            RigInvoke(rig, "Advance", 0d);
            TestAssert.Equal(fired, RigPose(rig), "same emission publication/zero delta cannot pump the pose");
            RigInvoke(rig, "ClearAttack");
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Active, 1, 0, 3));
            RigInvoke(rig, "Advance", .05d);
            object cancelled = RigPose(rig);
            TestAssert.Near(0f, RigValue<float>(cancelled, "Recoil"), .0001f, "revoked same attack cannot replay kick");
            TestAssert.Near(0f, RigValue<float>(cancelled, "Charge"), .0001f, "revoked same attack cannot restore warning");
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Telegraph, 2));
            RigInvoke(rig, "Advance", .05d);
            TestAssert.True(RigValue<float>(RigPose(rig), "Charge") > 0f, "new attack generation rearms naturally");
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Dead));
            RigInvoke(rig, "Advance", .05d);
            object dead = RigPose(rig);
            RigInvoke(rig, "ClearAttack");
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Idle));
            TestAssert.Equal(RangedWeaponState.Dead, RigValue<RangedWeaponState>(RigPose(rig), "State"),
                "Idle/release does not resurrect a dead body; checkpoint reset is explicit");
            TestAssert.True(RigValue<float>(dead, "Collapse") > 0f && RigValue<float>(dead, "Collapse") < 1f,
                "death collapses over time instead of one-frame translation");
            RigInvoke(rig, "Reset", role);
            RigInvoke(rig, "Apply", RigFrame(role, RangedWeaponState.Active, 1, 0, 1));
            RigInvoke(rig, "Advance", 1d / 60d);
            TestAssert.True(RigValue<float>(RigPose(rig), "Recoil") > 0f, "checkpoint reset admits a restarted AttackId1");

            var visual = new RangedWeaponVisual { Role = role };
            try
            {
                object geometry = PartGeometry(visual, "torso");
                var contour = RigValue<IReadOnlyList<Vector2>>(geometry, "Contour");
                silhouettes.Add(string.Join(";", contour.Select(point => point.ToString())));
            }
            finally { visual.Free(); }
        }
        TestAssert.Equal(4, silhouettes.Count, "four complete torso contours are distinguishable before color or weapon");
        foreach (int hz in new[] { 30, 60, 120 })
        {
            object rig = Activator.CreateInstance(rigType);
            RigInvoke(rig, "Reset", RangedWeaponRole.Burst);
            RigInvoke(rig, "Apply", RigFrame(RangedWeaponRole.Burst, RangedWeaponState.Telegraph, 1));
            for (int i = 0; i < hz; i++) RigInvoke(rig, "Advance", 1d / hz);
            TestAssert.Near(1f, RigValue<float>(RigPose(rig), "Brace"), .001f, "one second brace converges at " + hz + "Hz");
            object pose = RigPose(rig);
            foreach (double invalid in new[] { 0d, -1d, double.NaN, double.PositiveInfinity, 2d }) RigInvoke(rig, "Advance", invalid);
            TestAssert.Equal(pose, RigPose(rig), "invalid elapsed time freezes rather than corrupts pose");
            RigInvoke(rig, "Apply", RigFrame(RangedWeaponRole.Burst, RangedWeaponState.Telegraph, 1) with { AimDirection = new Vector2(float.NaN, float.PositiveInfinity) });
            RigInvoke(rig, "Advance", .01d);
            TestAssert.True(float.IsFinite(RigValue<float>(RigPose(rig), "AimAngle")), "malformed aim never reaches Draw transform");
        }
        // Equal total emission intervals, including overlap/hitch, must leave
        // exactly equal spring state. A replay-induced extra impulse breaks it.
        object batched = Activator.CreateInstance(rigType), split = Activator.CreateInstance(rigType);
        foreach (object rig in new[] { batched, split }) RigInvoke(rig, "Reset", RangedWeaponRole.Burst);
        RigInvoke(batched, "Apply", RigFrame(RangedWeaponRole.Burst, RangedWeaponState.Recovery, 1, 0, 3));
        RigInvoke(split, "Apply", RigFrame(RangedWeaponRole.Burst, RangedWeaponState.Active, 1, 0, 1));
        RigInvoke(split, "Apply", RigFrame(RangedWeaponRole.Burst, RangedWeaponState.Recovery, 1, 0, 3));
        foreach (object rig in new[] { batched, split }) RigInvoke(rig, "Advance", 1d / 60d);
        TestAssert.Equal(RigPose(batched), RigPose(split), "overlap only consumes new tail emissions, including Recovery hitch");

        var textureVisual = new RangedWeaponVisual();
        using Image wideImage = Image.CreateEmpty(80, 20, false, Image.Format.Rgba8);
        using Image tallImage = Image.CreateEmpty(20, 80, false, Image.Format.Rgba8);
        using ImageTexture wide = ImageTexture.CreateFromImage(wideImage), tall = ImageTexture.CreateFromImage(tallImage);
        try
        {
            PropertyInfo slot = typeof(RangedWeaponVisual).GetProperty("TorsoTexture");
            slot.SetValue(textureVisual, wide);
            object first = PartGeometry(textureVisual, "torso");
            Rect2 fit = RigValue<Rect2>(first, "DrawRect"), canvas = RigValue<Rect2>(first, "Canvas");
            TestAssert.True(RigValue<bool>(first, "UsesTexture"), "renderer geometry actually selects the borrowed texture slot");
            TestAssert.Near(4f, fit.Size.X / fit.Size.Y, .0001f, "wide art is uniformly fitted, never stretched to torso canvas");
            TestAssert.True(canvas.Encloses(fit), "fitted art remains inside its documented part canvas");
            slot.SetValue(textureVisual, tall);
            object second = PartGeometry(textureVisual, "torso");
            Rect2 secondFit = RigValue<Rect2>(second, "DrawRect");
            TestAssert.Near(.25f, secondFit.Size.X / secondFit.Size.Y, .0001f, "negative control observes tall slot change at actual Draw geometry");
            TestAssert.True(fit != secondFit && RigValue<Transform2D>(first, "Transform") == RigValue<Transform2D>(second, "Transform"),
                "slot change alters true fitted rectangle without pumping or rebasing bones");
        }
        finally { textureVisual.Free(); }
        TestAssert.True(GodotObject.IsInstanceValid(wide) && GodotObject.IsInstanceValid(tall), "freeing visual never disposes caller-owned textures");
        FootArtFitsAtAnkleWithoutChangingOtherParts();
        object eight = Activator.CreateInstance(rigType);
        RigInvoke(eight, "Reset", RangedWeaponRole.Burst);
        RigInvoke(eight, "Apply", RigFrame(RangedWeaponRole.Burst, RangedWeaponState.Recovery, 1, 0, 8));
        RigInvoke(eight, "Advance", 1d / 60d);
        TestAssert.True(RigValue<float>(RigPose(eight), "Recoil") > 0f,
            "RANGED_CONFIGURED_BURST_TAIL_RED: existing validated 1..8 Burst configuration must animate its real batched tail, not hardcode default3");
        GD.Print("RANGED_CHARACTER_RIG_OK roles=4 rigid_limbs=true recoil_dedup=true cancellation=true checkpoint_generation=true rates=30/60/120 slots=8 fit_negative=true pixels=false human_art=false");
    }

    private static async Task RealBodiesAndReplacementFollowNativeLifecycle(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            Fixture f = Create(tree, role, "RealBody" + role);
            try
            {
                var body = f.Actor.GetNode<RangedWeaponVisual>("VisualRoot/WeaponVisual");
                await Until(tree, () => body.CurrentRig.Brace > .01f, 14, "saved " + role + " body braces from actual actor event");
                TestAssert.Equal(RangedWeaponState.Telegraph, body.CurrentRig.State, "real saved warning reaches complete body");
                tree.Paused = true;
                RangedRigPose frozen = body.CurrentRig;
                for (int i = 0; i < 3; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.Equal(frozen, body.CurrentRig, "native tree pause freezes bones and body spring");
                tree.Paused = false;
                await Until(tree, () => f.Shots.Count > 0 && body.CurrentRig.Recoil > .01f, 110, "real " + role + " firing drives recoil");
                RangedWeaponFrame shotFrame = f.Actor.CurrentFrame;
                f.Actor.ApplyThreadPull(Vector2.Left * 400f);
                TestAssert.Near(0f, body.CurrentRig.Recoil, .0001f, "same-stack real thread revocation clears body kick");
                body.ApplyWeaponFrame(shotFrame);
                TestAssert.Near(0f, body.CurrentRig.Recoil, .0001f, "cancelled actual frame cannot resurrect visual recoil");
                TestAssert.Near(0f, body.CurrentRig.Heat, .0001f, "cancelled actual frame cannot resurrect weapon heat");
                f.Actor.ResetForCheckpoint();
                TestAssert.Equal(RangedWeaponState.Idle, body.CurrentRig.State, "real checkpoint reset returns body to live Idle");
                await Until(tree, () => body.CurrentRig.Charge > 0f, 14, "checkpoint restarted AttackId1 rearms real body");
                HealthComponent health = f.Actor.GetNode<HealthComponent>("Health");
                TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Left, 0f, 2701UL, 92701UL)), "real owner accepts lethal body lifecycle probe");
                await Until(tree, () => body.CurrentRig.State == RangedWeaponState.Dead && body.CurrentRig.Collapse > .01f, 14, "actual death transitions body collapse");
                TestAssert.Near(0f, body.CurrentRig.Charge, .0001f, "dead owner has no old charge");
                f.Actor.ResetForCheckpoint();
                TestAssert.Near(0f, body.CurrentRig.Collapse, .0001f, "real checkpoint resets dead collapse, not only health");
            }
            finally { tree.Paused = false; await Dispose(tree, f); }
        }

        var replacement = new RangedReplacementBodyProbe();
        Fixture custom = Create(tree, RangedWeaponRole.Laser, "IndependentBodyContract", replacement);
        try
        {
            await Until(tree, () => replacement.LastFrame.State == RangedWeaponState.Telegraph, 14, "independent body receives actual saved Laser warning");
            TestAssert.Equal(RangedWeaponRole.Laser, replacement.ActualRole, "interface supplies real role, absent from RangedWeaponFrame");
            TestAssert.Equal(1, replacement.ResetCount, "saved presentation initializes independent body once");
            int resets = replacement.ResetCount, clears = replacement.ClearCount;
            custom.Actor.ResetForCheckpoint();
            TestAssert.Equal(resets + 1, replacement.ResetCount, "checkpoint crosses independent reset contract exactly once");
            TestAssert.True(replacement.ClearCount > clears, "checkpoint cancels independent old attack before reset");
            clears = replacement.ClearCount;
            custom.Branch.RemoveChild(custom.Actor);
            TestAssert.True(replacement.ClearCount > clears, "source exit reaches independent body cleanup");
            custom.Actor.Free();
        }
        finally { if (GodotObject.IsInstanceValid(custom.Actor) && !custom.Actor.IsInsideTree()) custom.Actor.Free(); await Dispose(tree, custom); }

        var rotatedRoot = new Node2D { Rotation = 1.1f, Scale = new Vector2(1.4f, .6f) };
        var rotated = new RangedWeaponVisual { Rotation = .3f };
        rotatedRoot.AddChild(rotated);
        tree.Root.AddChild(rotatedRoot);
        try
        {
            rotated.SetProcess(false);
            rotated.PresentWeaponFrame(RangedWeaponRole.Scatter, RigFrame(RangedWeaponRole.Scatter, RangedWeaponState.Telegraph, 1));
            rotated._Process(.5d); rotated._Process(.5d);
            float angle = rotated.CurrentRig.AimAngle;
            Vector2 world = rotated.GlobalTransform.BasisXform(new Vector2(MathF.Cos(angle), MathF.Sin(angle))).Normalized();
            TestAssert.Near(0f, world.DistanceTo(Vector2.Right), .001f, "rotated/nonuniform ancestor converts world aim into actual local weapon basis");
        }
        finally { rotatedRoot.QueueFree(); await Frames(tree, 3); }
        GD.Print("RANGED_CHARACTER_LIFECYCLE_OK saved_roles=4 pause=true real_recoil=true revoke=true checkpoint=true death=true independent_body=true rotated_aim=true pixels=false");
    }

    private static RangedWeaponFrame RigFrame(RangedWeaponRole role, RangedWeaponState state, ulong id = 0, int first = -1, int due = 0)
    {
        AttackPhase phase = state switch { RangedWeaponState.Telegraph => AttackPhase.Telegraph,
            RangedWeaponState.Active => AttackPhase.Active, RangedWeaponState.Recovery => AttackPhase.Recovery, _ => AttackPhase.None };
        EnemyAttackKind kind = role switch { RangedWeaponRole.Scatter => EnemyAttackKind.RivetScatterShot,
            RangedWeaponRole.Burst => EnemyAttackKind.BeltBurstShot, RangedWeaponRole.Missile => EnemyAttackKind.FurnaceMissileShot, _ => EnemyAttackKind.PrismLaserBeam };
        return new(state, Vector2.Right, phase == AttackPhase.None ? default :
            new AttackTimelineFrame(id, kind, phase, 1f, .5f, Vector2.Zero, false), first, due);
    }
    private static object RigPose(object rig) => rig.GetType().GetProperty("Pose").GetValue(rig);
    private static T RigValue<T>(object value, string name) => (T)value.GetType().GetProperty(name).GetValue(value);
    private static void RigInvoke(object rig, string name, params object[] args) => rig.GetType().GetMethod(name).Invoke(rig, args);
    // Independent literal fits catch the old centered wide-foot gap, stretched
    // aspect ratios, and accidentally applying foot anchoring to every part.
    private static void FootArtFitsAtAnkleWithoutChangingOtherParts()
    {
        using Image wideImage = Image.CreateEmpty(80, 10, false, Image.Format.Rgba8);
        using Image tallImage = Image.CreateEmpty(10, 80, false, Image.Format.Rgba8);
        using Image torsoWideImage = Image.CreateEmpty(80, 20, false, Image.Format.Rgba8);
        using Image torsoTallImage = Image.CreateEmpty(20, 80, false, Image.Format.Rgba8);
        using ImageTexture wide = ImageTexture.CreateFromImage(wideImage), tall = ImageTexture.CreateFromImage(tallImage);
        using ImageTexture torsoWide = ImageTexture.CreateFromImage(torsoWideImage), torsoTall = ImageTexture.CreateFromImage(torsoTallImage);
        foreach ((RangedWeaponRole role, float ankleX) in new[]
        {
            (RangedWeaponRole.Scatter, 20f), (RangedWeaponRole.Burst, 15f),
            (RangedWeaponRole.Missile, 22f), (RangedWeaponRole.Laser, 12f)
        })
        foreach (float scaleX in new[] { 1f, -1f })
        foreach (Vector2 aim in new[] { Vector2.Right, Vector2.Left })
        {
            var visual = new RangedWeaponVisual { Scale = new Vector2(scaleX, 1f) };
            try
            {
                visual.ResetWeaponPresentation(role);
                // Leave the rig neutral: publishing aim/warning must not advance
                // elapsed time. Geometry queries must not do so either.
                visual.PresentWeaponFrame(role, RigFrame(role, RangedWeaponState.Telegraph, 1) with { AimDirection = aim });
                RangedRigPose before = visual.CurrentRig;
                foreach (bool far in new[] { false, true })
                {
                    Vector2 ankle = new(far ? -ankleX : ankleX, 34f);
                    TestAssert.Equal(ankle, far ? before.FarLeg.End : before.NearLeg.End,
                        "neutral role fixture has literal anatomical ankle " + role);
                    foreach ((Texture2D texture, Rect2 expected) in new (Texture2D, Rect2)[]
                    {
                        (null, new Rect2(-8f, -4f, 16f, 4f)),
                        (wide, new Rect2(-8f, -2f, 16f, 2f)),
                        (tall, new Rect2(-.25f, -4f, .5f, 4f))
                    })
                    {
                        visual.FootTexture = texture;
                        object geometry = PartGeometry(visual, "foot", far);
                        string context = "RANGED_FOOT_FIT_RED " + role + "/far=" + far + "/scale=" + scaleX + "/aim=" + aim + "/texture=" + (texture?.GetSize().ToString() ?? "null");
                        TestAssert.Equal(new Rect2(-8f, -4f, 16f, 4f), RigValue<Rect2>(geometry, "Canvas"), context + " documented foot canvas");
                        TestAssert.Equal(texture != null, RigValue<bool>(geometry, "UsesTexture"), context + " actual borrowed slot selection");
                        TestAssert.Equal(expected, RigValue<Rect2>(geometry, "DrawRect"), context + " aspect-fit X-centered bottom-anchored at ankle Y0");
                        Transform2D transform = RigValue<Transform2D>(geometry, "Transform");
                        TestAssert.Equal(new Transform2D(0f, ankle), transform, context + " actual near/far anatomical transform");
                        Vector2 bottomCenter = new(expected.GetCenter().X, expected.End.Y);
                        TestAssert.Equal(new Vector2(ankle.X * scaleX, 34f), visual.Transform * (transform * bottomCenter),
                            context + " supported horizontal mirror preserves ankle contact in parent space");
                        TestAssert.Equal(before, visual.CurrentRig, context + " geometry queries cannot animate the pending warning or aim");
                    }
                }
                foreach ((Texture2D texture, Rect2 expected) in new (Texture2D, Rect2)[]
                {
                    (torsoWide, new Rect2(-24f, -9.5f, 48f, 12f)),
                    (torsoTall, new Rect2(-5.375f, -25f, 10.75f, 43f))
                })
                {
                    visual.TorsoTexture = texture;
                    TestAssert.Equal(expected, RigValue<Rect2>(PartGeometry(visual, "torso"), "DrawRect"),
                        "foot-only anchoring must leave real torso art centered");
                }
                TestAssert.Equal(before, visual.CurrentRig, "foot/torso slot changes cannot advance rig time");
                visual._Process(.5d); visual._Process(.5d);
                Vector2 parentAim = visual.Transform.BasisXform(visual.CurrentRig.WeaponTransform.X).Normalized();
                TestAssert.Near(0f, parentAim.DistanceTo(aim), .001f,
                    "real settled left/right aim respects supported horizontal visual mirror");
                RangedRigPose settled = visual.CurrentRig;
                visual.FootTexture = wide;
                foreach (bool far in new[] { false, true })
                {
                    object geometry = PartGeometry(visual, "foot", far);
                    TestAssert.Equal(new Rect2(-8f, -2f, 16f, 2f), RigValue<Rect2>(geometry, "DrawRect"),
                        "settled aim keeps wide art ankle-anchored");
                    TestAssert.Equal(new Transform2D(0f, new Vector2(far ? -ankleX : ankleX, 34f)),
                        RigValue<Transform2D>(geometry, "Transform"), "settled aim preserves anatomical near/far feet");
                }
                TestAssert.Equal(settled, visual.CurrentRig, "settled geometry queries cannot advance motion");
                TestAssert.True(new[] { wide, tall, torsoWide, torsoTall }.All(GodotObject.IsInstanceValid),
                    "geometry use leaves all test-owned borrowed textures valid");
            }
            finally { visual.Free(); }
            TestAssert.True(new[] { wide, tall, torsoWide, torsoTall }.All(GodotObject.IsInstanceValid),
                "visual.Free never disposes borrowed foot/torso textures; test owns disposal");
        }
        GD.Print("RANGED_FOOT_FIT_OK roles=4 near_far=true aims=2 mirror=true wide_tall_null=true torso_centered=true query_pure=true borrowed=true");
    }

    private static object PartGeometry(RangedWeaponVisual visual, string part, bool far = false)
    {
        MethodInfo method = typeof(RangedWeaponVisual).GetMethod("GetPartGeometry", new[] { typeof(string), typeof(bool) });
        TestAssert.True(method != null, "RANGED_DRAW_PART_GEOMETRY_RED: actual renderer needs replaceable rigid part draw geometry");
        return method.Invoke(visual, new object[] { part, far });
    }
    private static void AssertRigidLimbs(object pose, string context)
    {
        foreach (string name in new[] { "FarArm", "NearArm", "FarLeg", "NearLeg" })
        {
            object limb = pose.GetType().GetProperty(name).GetValue(pose);
            Vector2 start = RigValue<Vector2>(limb, "Start"), joint = RigValue<Vector2>(limb, "Joint"), end = RigValue<Vector2>(limb, "End");
            float length = name.EndsWith("Arm", StringComparison.Ordinal) ? 18f : 13f;
            TestAssert.Near(length, start.DistanceTo(joint), .002f, context + " fixed upper " + name);
            TestAssert.Near(length, joint.DistanceTo(end), .002f, context + " fixed lower " + name);
            if (name.EndsWith("Leg", StringComparison.Ordinal)) TestAssert.Near(34f, end.Y, .001f, context + " planted foot baseline");
        }
    }

    // Break caught: ApplyThreadPull synchronously revokes attacks and publishes
    // None, releasing audio dedup, while CurrentFrame still says Active. The
    // public consumer must reject a recorded real event in that same stack,
    // not merely rely on the actor suppressing future natural emissions.
    private static async Task ImmediateThreadCancellationRejectsRecordedConsumerInput(SceneTree tree)
    {
        Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedImmediateThreadConsumer");
        try
        {
            await Until(tree, () => f.Shots.Count == 1, 110, "actual first saved Burst event before synchronous thread cancellation");
            TestAssert.True(f.Actor.CurrentFrame.AttackTimeline.Phase is AttackPhase.Active or AttackPhase.Recovery,
                "real Burst is in a shot-owning phase immediately before thread cancellation");
            TestAssert.Equal(1, f.Shots.Count, "exactly the first actual Burst event is recorded before cancellation");
            TestAssert.True(f.Actor.GetChildren().OfType<RangedWeaponProjectile>().Any(projectile => !projectile.IsRetired),
                "actual first Burst event has a live owned projectile to revoke");
            ShotObservation shot = f.Shots[0];
            AttackPresentationFrame oldWarning = f.Frames.First(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
            int before = f.Audio.FramePlaybackCount;

            // No await/manual step between cancellation and either replay.
            f.Actor.ApplyThreadPull(Vector2.Left * 400f);
            var leaves = new List<AttackTelegraphVisual>();
            f.Actor.AppendAuthorizedAttackLeaves(leaves);
            TestAssert.Equal(0, leaves.Count, "real thread request synchronously revokes every authorized attack leaf");
            TestAssert.True(!f.Actor.GetChildren().OfType<RangedWeaponProjectile>().Any(projectile => !projectile.IsRetired),
                "real thread request synchronously retires its actual owned Burst projectile");
            TestAssert.Equal(AttackPhase.None, f.Frames.Last().Timeline.Phase,
                "actual cancellation publishes None to the serialized presentation before physics advances");
            TestAssert.True(!TryShot(ShotApi(), f, shot),
                "RANGED_AUDIO_IMMEDIATE_THREAD_CANCEL_RED: same-stack cancelled owner must reject the exact recorded real ShotEmitted ordinal");
            TestAssert.Equal(before, f.Audio.FramePlaybackCount,
                "same-stack cancelled real event cannot increment playback after dedup release");
            f.Audio.PlayAttackFrame(f.Actor, oldWarning, oldWarning.Shape.Origin);
            TestAssert.Equal(before, f.Audio.FramePlaybackCount,
                "RANGED_AUDIO_IMMEDIATE_THREAD_PHASE_RED: same-stack cancelled owner must reject its old actual typed warning frame");
            TestAssert.Equal(1, f.Shots.Count,
                "consumer replay probes do not fabricate another natural Burst emission");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: saved actors publish a real warning, but the shared guidance
    // adapter ignores their base class; presentation drops audio or sounds once
    // per Active frame/pellet instead of once per real ShotEmitted event.
    private static async Task RealActorWarningAndEmissionAudio(SceneTree tree, RangedWeaponRole role, int expectedShots)
    {
        Fixture f = Create(tree, role, "RangedConsumer" + role);
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph, 12,
                "actual saved " + role + " actor naturally enters Telegraph");
            AssertRealAttackPreconditions(f);
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor),
                "RANGED_GUIDANCE_REAL_TELEGRAPH_RED: actual saved " + role + " warning must be recognized by the shared guidance consumer");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor.GetNode("Presentation")),
                "an arbitrary descendant is not itself a telegraph source");
            TestAssert.Equal(1, f.Audio.FramePlaybackCount,
                "RANGED_AUDIO_REAL_TELEGRAPH_RED: saved presentation must route the actor's first typed warning exactly once");
            AssertOutput(f, PrologueAudioCue.LaborerTelegraph, TelegraphPath, "real typed warning");

            AttackPresentationFrame warning = f.Frames.Last(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
            int warningCount = f.Audio.FramePlaybackCount;
            f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
            f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
            TestAssert.Equal(warningCount, f.Audio.FramePlaybackCount,
                "re-presenting the real typed warning cannot replay its sound");
            await Frames(tree, 5);
            TestAssert.Equal(warningCount, f.Audio.FramePlaybackCount,
                "natural warning geometry refreshes do not produce polling chatter");

            await Until(tree, () => f.Actor.CurrentFrame.AttackTimeline.Phase == AttackPhase.Recovery, 150,
                "actual saved " + role + " actor finishes its first native attack");
            f.Actor.SetPhysicsProcess(false);
            TestAssert.Equal(expectedShots, f.Shots.Count,
                role + " naturally publishes the independently specified emission count");
            TestAssert.Equal(expectedShots, f.Shots.Select(shot => shot.Ordinal).Distinct().Count(),
                "real emissions keep distinct owner-local damage ordinals");
            for (int index = 0; index < f.Shots.Count; index++)
            {
                ShotObservation shot = f.Shots[index];
                TestAssert.Equal(warningCount + index + 1, shot.PlaybackCount,
                    "RANGED_AUDIO_REAL_SHOT_RED: " + role + " emission " + (index + 1) + " produces exactly one actual shot onset, with no Active-phase extra");
                TestAssert.Equal(PrologueAudioCue.RustShellAttack, shot.Cue,
                    "hostile real shot uses an existing enemy onset, never PlayerAttack");
                TestAssert.Equal(ShotPath, shot.Path, "real emission selects the exact existing manifest shot path");
                TestAssert.Near(0f, shot.Origin.DistanceTo(shot.AudioOrigin), .001f,
                    "actual shot audio retains the emission world origin");
            }
            TestAssert.Equal(expectedShots + 2, f.Audio.FramePlaybackCount,
                "one warning, actual emission count and one recovery are the entire first attack audio");
            AssertOutput(f, PrologueAudioCue.LaborerRecovery, RecoveryPath, "real typed recovery");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor),
                "guidance stops advertising a warning during Recovery");

            int count = f.Audio.FramePlaybackCount;
            AttackPresentationFrame active = f.Frames.First(frame => frame.Timeline.Phase == AttackPhase.Active);
            f.Audio.PlayAttackFrame(f.Actor, active, active.Shape.Origin);
            f.Audio.PlayAttackFrame(f.Actor, active, active.Shape.Origin);
            TestAssert.Equal(count, f.Audio.FramePlaybackCount,
                "replayed actual Active frames cannot add a fourth burst or a second volley/beam onset");
            MethodInfo shotApi = ShotApi();
            foreach (ShotObservation shot in f.Shots)
                TestAssert.True(!TryShot(shotApi, f, shot), "an already audible actual event ordinal cannot sound twice");
            TestAssert.Equal(count, f.Audio.FramePlaybackCount, "actual-shot ordinal dedup preserves the total count");
            await Frames(tree, 180);
            TestAssert.Equal(count, f.Audio.FramePlaybackCount,
                "projectile retirement and beam recovery never create late shot onsets");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: thread/reset clears visible geometry but leaves consumer
    // identities alive, or resets damage ordinals and suppresses the next shot.
    private static async Task ThreadAndCheckpointReleaseRealSource(SceneTree tree)
    {
        Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedThreadResetConsumer");
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before thread cancellation");
            int beforeThread = f.Audio.FramePlaybackCount;
            f.Actor.ApplyThreadPull(Vector2.Left * 400f);
            await Frames(tree, 25);
            TestAssert.Equal(RangedWeaponState.Stunned, f.Actor.CurrentFrame.State, "real thread receiver enters its native stun");
            TestAssert.Equal(0, f.Shots.Count, "thread cancellation before firing produces no shot event");
            TestAssert.Equal(beforeThread, f.Audio.FramePlaybackCount, "cancelled warning produces no late recovery or shot sound");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor), "stunned actor is not a guidance warning");

            f.Actor.ResetForCheckpoint();
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning after checkpoint reset");
            TestAssert.Equal(beforeThread + 1, f.Audio.FramePlaybackCount,
                "reset releases the cancelled source so restarted local AttackId=1 warns audibly again");
            await Until(tree, () => f.Shots.Count == 1, 100, "first real burst event after reset");
            ShotObservation first = f.Shots[0];
            TestAssert.Equal(PrologueAudioCue.RustShellAttack, first.Cue, "reset's fresh event reaches the actual shot consumer");
            f.Actor.ResetForCheckpoint();
            int afterReset = f.Audio.FramePlaybackCount;
            await Until(tree, () => f.Shots.Count == 2, 110, "first real burst event after a second reset");
            f.Actor.SetPhysicsProcess(false);
            ShotObservation second = f.Shots[1];
            TestAssert.True(second.Ordinal > first.Ordinal, "checkpoint restart preserves real damage-ordinal monotonicity");
            TestAssert.Equal(afterReset + 2, f.Audio.FramePlaybackCount,
                "post-reset repeated local timeline emits one new warning and one fresh monotonic shot");
            TestAssert.Equal(PrologueAudioCue.RustShellAttack, second.Cue, "new monotonic ordinal is not suppressed by stale source dedup");
            f.Actor.ApplyThreadPull(Vector2.Left * 400f);
            f.Actor.SetPhysicsProcess(true);
            int cancelled = f.Audio.FramePlaybackCount;
            await Frames(tree, 25);
            TestAssert.Equal(2, f.Shots.Count, "thread cancellation removes both un-emitted burst tails");
            TestAssert.Equal(cancelled, f.Audio.FramePlaybackCount, "thread-retired rounds produce no audio after cancellation");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: after an actual first shot, death lets either remaining
    // burst event through, or the public consumer accepts an abandoned event.
    private static async Task DeathDuringRealBurstSuppressesTailAudio(SceneTree tree)
    {
        Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedDeathMidBurstConsumer");
        try
        {
            await Until(tree, () => f.Shots.Count == 1, 110, "real first event before burst-owner death");
            ShotObservation shot = f.Shots[0];
            HealthComponent health = f.Actor.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Left, 0f, 601UL, 9601UL)),
                "actual burst owner accepts lethal damage after the first shot");
            int count = f.Audio.FramePlaybackCount;
            f.Audio.ReleaseAttackSource(f.Actor);
            TestAssert.True(!TryShot(ShotApi(), f, shot), "dead real owner rejects even a released actual-shot ordinal");
            await Frames(tree, 40);
            TestAssert.Equal(1, f.Shots.Count, "death after first burst event suppresses both un-emitted real tails");
            TestAssert.Equal(count, f.Audio.FramePlaybackCount, "dead burst and retired rounds cannot emit late shot audio");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: Hurt/Death from a new actor or its descendant takes the
    // generic player fallback; a dead owner continues a pending burst.
    private static async Task RealHurtDeathAndAncestryNeverUsePlayerCues(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            Fixture f = Create(tree, role, "RangedLifeConsumer" + role);
            try
            {
                await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before damage");
                HealthComponent health = f.Actor.GetNode<HealthComponent>("Health");
                TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 501UL, 9501UL)),
                    "real saved ranged Health accepts the nonlethal request");
                AssertOutput(f, PrologueAudioCue.LaborerHurt, HurtPath, "actual ranged Hurt publication");
                PrologueAudioDirector.PlayPresentationCueFromTree(f.Actor.GetNode("Presentation"), PresentationCue.Hurt, f.Actor.GlobalPosition);
                AssertOutput(f, PrologueAudioCue.LaborerHurt, HurtPath, "ranged descendant ancestry mapping");
                TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 502UL, 9501UL)),
                    "real saved ranged Health accepts the lethal request");
                AssertOutput(f, PrologueAudioCue.LaborerDeath, DeathPath, "actual ranged Death publication");
                PrologueAudioDirector.PlayPresentationCueFromTree(f.Actor.GetNode("Presentation"), PresentationCue.Died, f.Actor.GlobalPosition);
                AssertOutput(f, PrologueAudioCue.LaborerDeath, DeathPath, "ranged descendant death ancestry mapping");
                int deadCount = f.Audio.FramePlaybackCount;
                await Frames(tree, 110);
                TestAssert.Equal(RangedWeaponState.Dead, f.Actor.CurrentFrame.State, "actual actor remains dead");
                TestAssert.Equal(0, f.Shots.Count, "death during warning cancels every real pending emission");
                TestAssert.Equal(deadCount, f.Audio.FramePlaybackCount, "dead actor cannot sound its abandoned shot or recovery");
                TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor), "dead owner releases its guidance warning");
            }
            finally { await Dispose(tree, f); }
        }
    }

    // Break caught: new phase/shot audio bypasses pause or disabled playback,
    // or a rejected attempt consumes dedup and suppresses a later accepted one.
    private static async Task PausedAndDisabledConsumersDoNotEmitOrConsumeNewPhase(SceneTree tree)
    {
        foreach (string gate in new[] { "audio-pause", "tree-pause", "disabled" })
        {
            Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedGate" + gate);
            try
            {
                await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before gate");
                AttackPresentationFrame warning = f.Frames.Last(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
                f.Actor.SetPhysicsProcess(false);
                f.Audio.ReleaseAttackSource(f.Actor);
                if (gate == "audio-pause") f.Audio.SetPaused(true);
                else if (gate == "tree-pause") tree.Paused = true;
                else f.Audio.PlaybackEnabled = false;
                int before = f.Audio.FramePlaybackCount;
                string beforePath = f.Audio.LastPlayedPath;
                f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
                TestAssert.Equal(before, f.Audio.FramePlaybackCount, gate + " rejects actual typed warning without counting nonexistent playback");
                TestAssert.Equal(beforePath, f.Audio.LastPlayedPath, gate + " cannot select a new sound path");
                tree.Paused = false;
                f.Audio.SetPaused(false);
                f.Audio.PlaybackEnabled = true;
                f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
                TestAssert.Equal(before + 1, f.Audio.FramePlaybackCount, "rejected " + gate + " phase does not consume the warning identity");
                f.Actor.SetPhysicsProcess(true);

                if (gate == "audio-pause") f.Audio.SetPaused(true);
                else f.Audio.PlaybackEnabled = false;
                int muted = f.Audio.FramePlaybackCount;
                await Until(tree, () => f.Shots.Count == 1, 100, "real emission while shot audio is gated");
                f.Actor.SetPhysicsProcess(false);
                ShotObservation shot = f.Shots[0];
                TestAssert.Equal(muted, shot.PlaybackCount, gate + " cannot count a muted actual shot as playback");
                MethodInfo api = ShotApi();
                TestAssert.True(!TryShot(api, f, shot), "gated actual-shot boundary rejects the real emitted ordinal");
                tree.Paused = true;
                f.Audio.SetPaused(false);
                f.Audio.PlaybackEnabled = true;
                TestAssert.True(!TryShot(api, f, shot), "raw tree pause gates the real shot boundary even though the director always processes");
                tree.Paused = false;
                TestAssert.True(!TryShot(api, f, shot with { Origin = new Vector2(float.NaN, 0f) }),
                    "actual ordinal with malformed origin fails closed without consuming its sound identity");
                TestAssert.True(TryShot(api, f, shot), "rejected real ordinal is accepted once after playback resumes");
                TestAssert.Equal(muted + 1, f.Audio.FramePlaybackCount, "accepted actual shot increments the established playback counter exactly once");
                AssertOutput(f, PrologueAudioCue.RustShellAttack, ShotPath, "unconsumed real emission after gate");
                TestAssert.True(!TryShot(api, f, shot), "accepted actual event is deduplicated after resume");
            }
            finally { tree.Paused = false; await Dispose(tree, f); }
        }
    }

    // Break caught: only source.IsQueuedForDeletion is checked; a queued parent
    // or grandparent still leaks guidance/audio before deferred deletion runs.
    private static async Task QueuedAncestorsAndDetachedSourcesFailClosed(SceneTree tree)
    {
        foreach (int queuedDepth in new[] { 0, 1, 2 })
        {
            Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedQueuedConsumer" + queuedDepth);
            try
            {
                await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before queue gate");
                AttackPresentationFrame warning = f.Frames.Last(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
                f.Actor.SetPhysicsProcess(false);
                Node queued = queuedDepth == 0 ? f.Actor : queuedDepth == 1 ? f.Branch : f.Envelope;
                queued.QueueFree();
                TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor),
                    "RANGED_GUIDANCE_QUEUED_ANCESTOR_RED: real warning rejects queued ancestor depth " + queuedDepth);
                f.Audio.ReleaseAttackSource(f.Actor);
                int count = f.Audio.FramePlaybackCount;
                f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
                TestAssert.Equal(count, f.Audio.FramePlaybackCount, "queued ancestry cannot replay a released real warning");
            }
            finally { await Dispose(tree, f); }

            Fixture emitted = Create(tree, RangedWeaponRole.Burst, "RangedQueuedShotConsumer" + queuedDepth);
            try
            {
                await Until(tree, () => emitted.Shots.Count == 1, 110, "real emitted ordinal before queued-source gate");
                emitted.Actor.SetPhysicsProcess(false);
                ShotObservation shot = emitted.Shots[0];
                Node queued = queuedDepth == 0 ? emitted.Actor : queuedDepth == 1 ? emitted.Branch : emitted.Envelope;
                emitted.Audio.ReleaseAttackSource(emitted.Actor);
                queued.QueueFree();
                int count = emitted.Audio.FramePlaybackCount;
                TestAssert.True(!TryShot(ShotApi(), emitted, shot), "actual-shot boundary rejects queued ancestry depth " + queuedDepth);
                TestAssert.Equal(count, emitted.Audio.FramePlaybackCount, "queued actual ordinal cannot count or replay a late shot");
                await Frames(tree, 3);
                TestAssert.Equal(1, emitted.Shots.Count, "queued actual owner cannot emit either remaining burst tail");
            }
            finally { await Dispose(tree, emitted); }
        }

        Fixture detached = Create(tree, RangedWeaponRole.Burst, "RangedDetachedConsumer");
        try
        {
            await Until(tree, () => detached.Shots.Count == 1, 110, "real emission before source detachment");
            detached.Actor.SetPhysicsProcess(false);
            ShotObservation shot = detached.Shots[0];
            AttackPresentationFrame warning = detached.Frames.First(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
            detached.Branch.RemoveChild(detached.Actor);
            int count = detached.Audio.FramePlaybackCount;
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(detached.Actor), "detached actual actor has no guidance warning");
            detached.Audio.PlayAttackFrame(detached.Actor, warning, warning.Shape.Origin);
            TestAssert.True(!TryShot(ShotApi(), detached, shot), "detached actual shot source fails closed");
            TestAssert.Equal(count, detached.Audio.FramePlaybackCount, "source exit releases audio state and blocks late typed/shot playback");
            detached.Actor.Free();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(detached.Actor) && !detached.Actor.IsInsideTree()) detached.Actor.Free();
            await Dispose(tree, detached);
        }
    }

    private static Fixture Create(SceneTree tree, RangedWeaponRole role, string name, Node2D replacement = null)
    {
        string actorName = role switch
        {
            RangedWeaponRole.Scatter => nameof(RivetScatterGunner),
            RangedWeaponRole.Burst => nameof(BeltBurstGunner),
            RangedWeaponRole.Missile => nameof(FurnaceMissileGunner),
            _ => nameof(PrismLaserGunner)
        };
        string path = "res://scenes/enemies/" + actorName + ".tscn";
        PackedScene scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        TestAssert.True(scene != null, "consumer fixture loads the actual saved leaf: " + path);
        RangedWeaponEnemy actor = scene.Instantiate<RangedWeaponEnemy>();
        TestAssert.Equal(path, actor.SceneFilePath, "consumer fixture retains the saved leaf identity");
        var resources = new HashSet<Resource> { actor.Settings };
        CollectShapes(actor, resources);
        if (replacement != null)
        {
            Node2D visualRoot = actor.GetNode<Node2D>("VisualRoot");
            Node original = visualRoot.GetNode("WeaponVisual");
            visualRoot.RemoveChild(original); original.Free();
            replacement.Name = "WeaponVisual"; visualRoot.AddChild(replacement);
        }
        var root = new Node2D { Name = name };
        var envelope = new Node2D { Name = "EncounterEnvelope" };
        var branch = new Node2D { Name = "ActorBranch" };
        var floorShape = new RectangleShape2D { Size = new Vector2(1100f, 20f) };
        resources.Add(floorShape);
        var floor = new StaticBody2D { Name = "NativeFloor", Position = new Vector2(580f, 339f),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
        floor.AddChild(new CollisionShape2D { Shape = floorShape });
        // The cached Player scene and its imported resources are borrowed, never disposed here.
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "ActualPlayer";
        player.Position = new Vector2(440f, 300f);
        player.AddToGroup("player");
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        PrologueAudioDirector audio = PrologueAudioDirector.CreateRuntimeTree();
        audio.PlaybackEnabled = false;
        root.AddChild(audio);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(envelope);
        envelope.AddChild(branch);
        actor.Position = new Vector2(200f, 300f);
        actor.ConfigureEncounterEnvelope(Arena);
        branch.AddChild(actor);
        tree.Root.AddChild(root);
        audio.PlaybackEnabled = true;
        HealthComponent playerHealth = player.GetNode<HealthComponent>("Health");
        playerHealth.MaxHealth = 100;
        playerHealth.ResetFull();
        var f = new Fixture(root, envelope, branch, actor, player, audio, scene, resources);
        // Mount first: the serialized Presentation subscribes in _Ready before
        // observations, so snapshots measure its real consumer side effects.
        actor.AttackFrameChanged += frame => f.Frames.Add(frame);
        actor.ShotEmitted += (ordinal, origin) => f.Shots.Add(new ShotObservation(ordinal, origin,
            audio.FramePlaybackCount, audio.LastCue, audio.LastPlayedPath, audio.LastWorldPosition));
        return f;
    }

    private static void AssertRealAttackPreconditions(Fixture f)
    {
        Vector2 muzzle = f.Actor.GetNode<Node2D>("Muzzle").GlobalPosition;
        TestAssert.True(f.Player is CharacterBody2D && f.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
            "warning precondition uses the actual living saved Player/Health");
        TestAssert.True(Arena.HasPoint(muzzle) && muzzle.DistanceTo(f.Player.GlobalPosition) < 360f,
            "real muzzle is admitted and player is within the shortest weapon detection range");
        TestAssert.True(f.Actor.GetViewport().GetVisibleRect().HasPoint(f.Actor.GetNode<Node2D>("Muzzle").GetGlobalTransformWithCanvas().Origin),
            "native viewport contains the actual muzzle");
        TestAssert.True(RangedAttackGeometry.HasClearWorldPath(f.Actor.GetWorld2D().DirectSpaceState, muzzle, f.Player.GlobalPosition),
            "native world physics confirms actual player LOS before consumer RED");
    }

    private static void AssertOutput(Fixture f, PrologueAudioCue cue, string path, string context)
    {
        TestAssert.Equal(cue, f.Audio.LastCue, context + " selects the agreed existing enemy cue");
        TestAssert.Equal(path, f.Audio.LastPlayedPath, context + " selects its real manifest path");
        PrologueAudioManifest manifest = PrologueAudioManifest.Load(PrologueAudioDirector.ManifestPath);
        TestAssert.True(manifest.Cues[cue.ToString()].Contains(path), context + " path is declared in the real sound bank");
        TestAssert.True(f.Audio.GetChildren().OfType<AudioStreamPlayer>().Any(player =>
                player.Bus == "SFX" && player.Playing && player.Stream?.ResourcePath == path),
            context + " reaches an actual playing SFX stream, not only LastCue bookkeeping");
        TestAssert.True(cue is not PrologueAudioCue.PlayerAttack and not PrologueAudioCue.PlayerHurt,
            context + " is never a hostile-to-player fallback");
    }

    private static MethodInfo ShotApi()
    {
        MethodInfo method = typeof(PrologueAudioDirector).GetMethod("TryPlayRangedShot",
            BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(RangedWeaponEnemy), typeof(ulong), typeof(Vector2) }, null);
        TestAssert.True(method != null && method.ReturnType == typeof(bool),
            "RANGED_AUDIO_ACTUAL_SHOT_API_RED: guarded bool TryPlayRangedShot(RangedWeaponEnemy, ulong, Vector2) boundary is required");
        return method;
    }

    private static bool TryShot(MethodInfo api, Fixture f, ShotObservation shot) =>
        (bool)api.Invoke(f.Audio, new object[] { f.Actor, shot.Ordinal, shot.Origin });

    private static async Task Until(SceneTree tree, Func<bool> condition, int budget, string context)
    {
        for (int index = 0; index < budget && !condition(); index++) await Frames(tree, 1);
        TestAssert.True(condition(), "fixture/native timing precondition: " + context);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static void CollectShapes(Node node, HashSet<Resource> resources)
    {
        if (node is CollisionShape2D collider && collider.Shape != null) resources.Add(collider.Shape);
        foreach (Node child in node.GetChildren()) CollectShapes(child, resources);
    }

    private static async Task Dispose(SceneTree tree, Fixture f)
    {
        tree.Paused = false;
        if (GodotObject.IsInstanceValid(f.Root) && !f.Root.IsQueuedForDeletion()) f.Root.QueueFree();
        await Frames(tree, 3);
        foreach (Resource resource in f.Resources) if (GodotObject.IsInstanceValid(resource)) resource.Dispose();
        f.Scene.Dispose();
    }

    private sealed record ShotObservation(ulong Ordinal, Vector2 Origin, int PlaybackCount,
        PrologueAudioCue Cue, string Path, Vector2 AudioOrigin);
    private sealed record Fixture(Node2D Root, Node2D Envelope, Node2D Branch,
        RangedWeaponEnemy Actor, CharacterBody2D Player, PrologueAudioDirector Audio,
        PackedScene Scene, HashSet<Resource> Resources)
    {
        public List<AttackPresentationFrame> Frames { get; } = new();
        public List<ShotObservation> Shots { get; } = new();
    }
}

// Test-owned replacement, no concrete default renderer inheritance. Observes
// real actor wiring; it is never used to simulate production attacks or damage.
public partial class RangedReplacementBodyProbe : Node2D, IRangedWeaponVisual2D
{
    public RangedWeaponFrame LastFrame { get; private set; }
    public RangedWeaponRole ActualRole { get; private set; }
    public int ResetCount { get; private set; }
    public int ClearCount { get; private set; }
    public void PresentWeaponFrame(RangedWeaponRole role, RangedWeaponFrame frame) { ActualRole = role; LastFrame = frame; }
    public void ClearAttackPresentation() => ClearCount++;
    public void ResetWeaponPresentation(RangedWeaponRole role) { ActualRole = role; ResetCount++; }
    public void ApplyFrame(EntityPresentationFrame frame) { }
    public void PlayCue(PresentationCue cue) { }
}
