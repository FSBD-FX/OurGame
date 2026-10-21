using Godot;
using Knight.Abilities;
using Knight.Config;
using Knight.Enemies;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class PlayerPresentationTests
{
    public static void RunProfileDiagnosticContract()
    {
        var resolve = System.Array.Find(typeof(PresentationProfile).GetMethods(), method =>
            method.Name == "Resolve" && method.GetParameters().Length == 2 && method.GetParameters()[1].IsOut);
        var cue = System.Array.Find(typeof(PresentationProfile).GetMethods(), method =>
            method.Name == "TryInstantiateCue" && method.GetParameters().Length == 3 && method.GetParameters()[2].IsOut);
        TestAssert.True(resolve != null && cue != null,
            "actual shared profile resolution/rejection diagnostic values are unavailable");
        void Check(object diagnostic, string code, string messageFragment)
        {
            TestAssert.True(diagnostic != null && diagnostic.GetType().GetProperty("Code") != null &&
                diagnostic.GetType().GetProperty("Message") != null, "profile diagnostic is an explicit value");
            TestAssert.Equal(code, diagnostic.GetType().GetProperty("Code").GetValue(diagnostic).ToString(),
                "actual profile diagnostic distinguishes accepted/missing and each rejected root kind");
            string message = (string)diagnostic.GetType().GetProperty("Message").GetValue(diagnostic);
            TestAssert.True(messageFragment == null ? string.IsNullOrEmpty(message) :
                message?.Contains(messageFragment, System.StringComparison.Ordinal) == true,
                "diagnostic retains rejection context without retaining native root/resource");
        }
        using var invalidScene = Pack(new Node2D { Name = "RejectedVisualRoot" });
        using var validCueScene = Pack(new Node2D { Name = "ValidCueRoot" });
        using var invalidCueScene = Pack(new Node { Name = "RejectedCueRoot" });
        using var profile = new PresentationProfile { VisualScene = invalidScene };
        var root = FallbackRoot(out RecordingEntityVisual fallback);
        bool previous = PrologueProductionMode.AllowDiagnosticFallbacks;
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            object[] visualArgs = { root, null };
            object resolved = resolve.Invoke(profile, visualArgs);
            TestAssert.True(ReferenceEquals(fallback, resolved) && fallback.Visible,
                "same real invalid visual resolution preserves original fallback identity and visibility");
            Check(visualArgs[1], "InvalidVisualRoot", "VisualRoot");
            profile.CueScenes[PresentationCue.AttackHit.ToString()] = validCueScene;
            profile.CueScenes[PresentationCue.Hurt.ToString()] = invalidCueScene;
            object[] validArgs = { PresentationCue.AttackHit, null, null };
            TestAssert.True((bool)cue.Invoke(profile, validArgs) && validArgs[1] is Node2D,
                "real valid Node2D cue remains caller-owned success");
            ((Node2D)validArgs[1]).Free(); Check(validArgs[2], "None", null);
            object[] invalidArgs = { PresentationCue.Hurt, null, null };
            TestAssert.True(!(bool)cue.Invoke(profile, invalidArgs) && invalidArgs[1] == null,
                "real ordinary Node cue is rejected and never handed to a caller");
            Check(invalidArgs[2], "InvalidCueRoot", "Hurt");
            object[] absentArgs = { PresentationCue.Died, null, null };
            TestAssert.True(!(bool)cue.Invoke(profile, absentArgs) && absentArgs[1] == null,
                "missing optional cue remains a quiet absence, not malformed data");
            Check(absentArgs[2], "None", null);
            PrologueProductionMode.AllowDiagnosticFallbacks = false;
            bool rejected = false;
            try { resolve.Invoke(profile, new object[] { root, null }); }
            catch (System.Reflection.TargetInvocationException exception) when
                (exception.InnerException is System.InvalidOperationException error &&
                 error.Message.Contains("production visual is missing or invalid", System.StringComparison.Ordinal))
            { rejected = true; }
            TestAssert.True(rejected && !fallback.Visible,
                "diagnostic result seam never swallows original production fallback rejection");
            PresentationDiagnostic productionDiagnostic = default;
            try { profile.Resolve(root, out productionDiagnostic); }
            catch (System.InvalidOperationException error) when
                (error.Message.Contains("production visual is missing or invalid", System.StringComparison.Ordinal)) { }
            Check(productionDiagnostic, "InvalidVisualRoot", "VisualRoot");
            var emptyRoot = new Node2D { Name = "NoVisualRoot" };
            try
            {
                PresentationDiagnostic emptyDiagnostic = default;
                bool emptyRejected = false;
                try { profile.Resolve(emptyRoot, out emptyDiagnostic); }
                catch (System.InvalidOperationException error) when
                    (error.Message.Contains("no IEntityVisual2D available under NoVisualRoot", System.StringComparison.Ordinal))
                { emptyRejected = true; }
                TestAssert.True(emptyRejected, "no-visual exception also remains an exception after rejected override");
                Check(emptyDiagnostic, "InvalidVisualRoot", "NoVisualRoot");
            }
            finally { emptyRoot.Free(); }
            GD.Print("THRUSTER_PROFILE_DIAGNOSTICS_OK same_production_core=true invalid_visual_fallback_identity=true invalid_cue_rejected=true valid_and_missing_quiet=true original_production_exception=true value_context=true native_resource_retained=false human=false");
        }
        finally { PrologueProductionMode.AllowDiagnosticFallbacks = previous; root.Free(); }
    }

    public static void Run()
    {
        PlayerStateMappingIsGameplayDriven();
        AuthoritativeTimelineCoversEveryLocomotionState();
        AttackTimelinePreservesPreparationContactAndCancellableRecovery();
        TypedActionAimNormalizesAndRejectsNonFiniteInput();
        AttackIdentityStaysBoundToItsStartContext();
        ThreadTimelineConsumesEveryAuthoritativeCastState();
        MechanicalBoostKeepsSpoolTension();
        HurtDeathAndCheckpointRecoveryRemainDistinct();
        SixtyFpsTimelineIsDeterministic();
        RustShellStateMappingIsGameplayDriven();
        CoordinatorDeliversFramesAndCuesOnce();
        ValidVisualOverrideReplacesFallback();
        bool previous = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        try
        {
            InvalidVisualOverrideKeepsFallback();
            MissingVisualOverrideKeepsFallback();
        }
        finally
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = previous;
        }
        ProductionModeRejectsFallback();
        CueScenesAcceptOnlyNode2DRoots();
    }

    private static void AuthoritativeTimelineCoversEveryLocomotionState()
    {
        const float step = 1f / 60f;
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput input = Input(Vector2.Zero, grounded: true);

        TestAssert.Equal(PlayerLocomotionState.Idle, timeline.Step(input, step).Locomotion.State,
            "authoritative timeline starts in Idle");
        input = input with { Velocity = new Vector2(180f, 0f) };
        TestAssert.Equal(PlayerLocomotionState.StartRun, timeline.Step(input, step).Locomotion.State,
            "ground acceleration enters StartRun before Run");
        PlayerPresentationFrame running = StepUntil(timeline, input, PlayerLocomotionState.Run);
        TestAssert.True(running.Locomotion.NormalizedTime >= 0f && running.Locomotion.NormalizedTime <= 1f,
            "Run exposes deterministic normalized progress");

        input = input with { Velocity = Vector2.Zero };
        TestAssert.Equal(PlayerLocomotionState.Brake, timeline.Step(input, step).Locomotion.State,
            "stopping from Run enters Brake before Idle");
        StepUntil(timeline, input, PlayerLocomotionState.Idle);

        input = input with { Grounded = false, Velocity = new Vector2(60f, -520f) };
        TestAssert.Equal(PlayerLocomotionState.JumpStart, timeline.Step(input, step).Locomotion.State,
            "leaving the floor enters JumpStart before Rise");
        StepUntil(timeline, input, PlayerLocomotionState.Rise);
        input = input with { Velocity = new Vector2(60f, 0f) };
        TestAssert.Equal(PlayerLocomotionState.Apex, timeline.Step(input, step).Locomotion.State,
            "near-zero airborne vertical speed resolves Apex");
        input = input with { Velocity = new Vector2(60f, 260f) };
        TestAssert.Equal(PlayerLocomotionState.Fall, timeline.Step(input, step).Locomotion.State,
            "positive airborne vertical speed resolves Fall");
        input = input with { Grounded = true, Velocity = new Vector2(60f, 0f) };
        TestAssert.Equal(PlayerLocomotionState.SoftLand, timeline.Step(input, step).Locomotion.State,
            "a moderate fall enters SoftLand");

        var hardLanding = new PlayerPresentationTimeline(new PlayerTuning());
        hardLanding.Step(Input(new Vector2(0f, 620f), grounded: false), step);
        TestAssert.Equal(PlayerLocomotionState.HardLand,
            hardLanding.Step(Input(Vector2.Zero, grounded: true), step).Locomotion.State,
            "a high-speed fall enters HardLand");
        TestAssert.Equal(PlayerLocomotionState.Dash,
            timeline.Step(input with { Dashing = true }, step).Locomotion.State,
            "Dash has an explicit locomotion state");
        TestAssert.Equal(PlayerLocomotionState.Recovery,
            timeline.Step(input with { CheckpointRecovering = true }, step).Locomotion.State,
            "checkpoint recovery has an explicit locomotion state");
        TestAssert.Equal(PlayerLocomotionState.Dead,
            timeline.Step(input with { Dead = true, CheckpointRecovering = false }, step).Locomotion.State,
            "Dead overrides every locomotion state");
    }

    private static void AttackTimelinePreservesPreparationContactAndCancellableRecovery()
    {
        AssertAttackTimeline(PlayerActionState.GroundAttack, grounded: true, AttackDirection.Horizontal);
        AssertAttackTimeline(PlayerActionState.AirAttack, grounded: false, AttackDirection.Up);
        AssertAttackTimeline(PlayerActionState.DownAttack, grounded: false, AttackDirection.Down);

        const float step = 1f / 60f;
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput attack = Input(new Vector2(180f, 0f), grounded: true) with
        {
            AttackId = 41,
            IsAttacking = true,
            AttackDirection = AttackDirection.Horizontal,
            MovementCancelRequested = true
        };
        PlayerPresentationFrame anticipation = timeline.Step(attack, step);
        TestAssert.Equal(PlayerActionPhase.Anticipation, anticipation.Action.Phase,
            "movement cannot skip attack preparation");
        PlayerPresentationFrame contact = StepUntil(timeline, attack,
            PlayerActionPhase.Contact);
        TestAssert.Equal(PlayerActionState.GroundAttack, contact.Action.State,
            "movement cannot skip attack contact confirmation");
        PlayerPresentationFrame recovery = StepUntil(timeline, attack with
        {
            MovementCancelRequested = false
        }, PlayerActionPhase.Recovery);
        TestAssert.True(recovery.Action.RecoveryCancellable,
            "only the declared attack Recovery is cancellable");
        PlayerPresentationFrame cancelled = timeline.Step(attack with
        {
            MovementCancelRequested = true
        }, step);
        TestAssert.Equal(PlayerActionFrame.None, cancelled.Action,
            "movement cancels the presentation layer during declared Recovery");

        var hit = new PlayerPresentationTimeline(new PlayerTuning());
        var whiff = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput hitInput = attack with
        {
            MovementCancelRequested = false,
            AttackId = 52
        };
        PlayerPresentationInput whiffInput = hitInput with { AttackId = 53 };
        PlayerPresentationFrame hitFrame = default;
        PlayerPresentationFrame whiffFrame = default;
        for (int index = 0; index < 22; index++)
        {
            hitFrame = hit.Step(hitInput with { AttackConnected = index == 7 }, step);
            whiffFrame = whiff.Step(whiffInput, step);
        }
        TestAssert.Equal(PlayerActionPhase.Recovery, hitFrame.Action.Phase,
            "a connected attack retains visible Recovery");
        TestAssert.Equal(PlayerActionPhase.Recovery, whiffFrame.Action.Phase,
            "a whiff retains visible Recovery");
        TestAssert.True(Mathf.Abs(hitFrame.Secondary.Lantern.Position.Y -
            whiffFrame.Secondary.Lantern.Position.Y) > 0.0001f,
            "hit confirmation adds a bounded secondary contact impulse without changing gameplay timing");
    }

    private static void TypedActionAimNormalizesAndRejectsNonFiniteInput()
    {
        var normalized = new PlayerActionFrame(PlayerActionState.ThreadThrow,
            PlayerActionPhase.Burst, .5f, false, new Vector2(3f, 4f));
        TestAssert.Near(.6f, normalized.Aim.X, .0001f,
            "typed action Aim normalizes finite X input");
        TestAssert.Near(.8f, normalized.Aim.Y, .0001f,
            "typed action Aim normalizes finite Y input");
        var huge = new PlayerActionFrame(PlayerActionState.ThreadThrow,
            PlayerActionPhase.Burst, .5f, false,
            new Vector2(float.MaxValue, -float.MaxValue));
        TestAssert.True(float.IsFinite(huge.Aim.X) && float.IsFinite(huge.Aim.Y),
            "extreme finite typed action Aim remains finite after normalization");
        TestAssert.Near(Mathf.Sqrt(.5f), huge.Aim.X, .0001f,
            "extreme finite typed action Aim preserves scaled X direction");
        TestAssert.Near(-Mathf.Sqrt(.5f), huge.Aim.Y, .0001f,
            "extreme finite typed action Aim preserves scaled Y direction");
        var none = new PlayerActionFrame(PlayerActionState.None, PlayerActionPhase.None,
            0f, false, Vector2.Right);
        TestAssert.Equal(Vector2.Zero, none.Aim, "None action always stores zero Aim");

        AssertAimRejected(new Vector2(float.NaN, 1f), "NaN");
        AssertAimRejected(new Vector2(float.PositiveInfinity, -1f), "Infinity");
    }

    private static void AssertAimRejected(Vector2 aim, string label)
    {
        try
        {
            _ = new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, .5f, false, aim);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            return;
        }
        throw new System.InvalidOperationException(
            $"Assertion failed: typed action Aim rejects explicit {label} input");
    }

    private static void ThreadTimelineConsumesEveryAuthoritativeCastState()
    {
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput input = Input(new Vector2(170f, -140f), grounded: false);
        (ThreadCastState State, PlayerActionState Action, PlayerActionPhase Phase)[] cases =
        [
            (ThreadCastState.Preview, PlayerActionState.ThreadPreview, PlayerActionPhase.Anticipation),
            (ThreadCastState.Windup, PlayerActionState.ThreadWindup, PlayerActionPhase.Anticipation),
            (ThreadCastState.Travel, PlayerActionState.ThreadThrow, PlayerActionPhase.Burst),
            (ThreadCastState.Latched, PlayerActionState.ThreadLatch, PlayerActionPhase.Contact),
            (ThreadCastState.Apply, PlayerActionState.ThreadApply, PlayerActionPhase.Contact),
            (ThreadCastState.Recovery, PlayerActionState.ThreadApply, PlayerActionPhase.Recovery),
            (ThreadCastState.Failed, PlayerActionState.ThreadFailed, PlayerActionPhase.Recovery)
        ];
        foreach ((ThreadCastState state, PlayerActionState action, PlayerActionPhase phase) in cases)
        {
            ThreadCastFrame cast = Cast(state, duration: .2f, remaining: .1f,
                aim: new Vector2(3f, -4f));
            PlayerPresentationFrame frame = timeline.Step(input with { Thread = cast }, 1f / 60f);
            TestAssert.Equal(action, frame.Action.State,
                $"authoritative thread {state} maps to typed {action}");
            TestAssert.Equal(phase, frame.Action.Phase,
                $"authoritative thread {state} preserves its declared action phase");
            TestAssert.Near(.5f, frame.Action.NormalizedTime, .0001f,
                $"authoritative thread {state} supplies normalized remaining-time progress");
            TestAssert.Near(.6f, frame.Action.Aim.X, .0001f,
                $"authoritative thread {state} normalizes and preserves cast Aim.X");
            TestAssert.Near(-.8f, frame.Action.Aim.Y, .0001f,
                $"authoritative thread {state} normalizes and preserves cast Aim.Y");
            if (state == ThreadCastState.Failed)
                TestAssert.True(!frame.Action.RecoveryCancellable,
                    "authoritative ThreadFailed window is not a cancellable recovery state");
            TestAssert.True(frame.Locomotion.State is PlayerLocomotionState.JumpStart or
                PlayerLocomotionState.Rise,
                $"thread {state} keeps the current airborne locomotion layer alive");
        }

        PlayerPresentationFrame idle = timeline.Step(input with { Thread = Cast(ThreadCastState.Idle) }, 0f);
        TestAssert.Equal(PlayerActionFrame.None, idle.Action,
            "authoritative thread Idle clears the typed action synchronously");
    }

    private static void AttackIdentityStaysBoundToItsStartContext()
    {
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput groundedStart = Input(new Vector2(120f, 0f), grounded: true) with
        {
            AttackId = 77,
            IsAttacking = true,
            AttackDirection = AttackDirection.Horizontal
        };
        TestAssert.Equal(PlayerActionState.GroundAttack,
            timeline.Step(groundedStart, 1f / 60f).Action.State,
            "a grounded attack binds its typed identity at attack start");
        TestAssert.Equal(PlayerActionState.GroundAttack,
            timeline.Step(groundedStart with
            {
                Grounded = false,
                Velocity = new Vector2(120f, -180f)
            }, 1f / 60f).Action.State,
            "one attack id cannot switch from GroundAttack to AirAttack after leaving the floor");
    }

    private static void HurtDeathAndCheckpointRecoveryRemainDistinct()
    {
        const float step = 1f / 60f;
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput input = Input(new Vector2(-240f, -180f), grounded: false) with
        {
            HurtTriggered = true,
            HurtImpulse = new Vector2(-280f, -120f)
        };
        PlayerPresentationFrame hurt = timeline.Step(input, step);
        TestAssert.Equal(PlayerActionState.Hurt, hurt.Action.State,
            "damage produces a typed Hurt action over knockback locomotion");
        TestAssert.Equal(PlayerActionPhase.Contact, hurt.Action.Phase,
            "hurt begins with visible contact confirmation");
        var oppositeTimeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationFrame oppositeHurt = oppositeTimeline.Step(input with
        {
            HurtImpulse = new Vector2(280f, -120f)
        }, step);
        TestAssert.True(hurt.Secondary.Mantle.Position.X < 0f &&
            oppositeHurt.Secondary.Mantle.Position.X > 0f,
            "left-facing health impulses drive opposite bounded facing-local hurt deformation immediately");
        PlayerPresentationFrame hurtRecovery = default;
        for (int index = 0; index < 8; index++)
            hurtRecovery = timeline.Step(input with { HurtTriggered = false }, step);
        TestAssert.Equal(PlayerActionPhase.Recovery, hurtRecovery.Action.Phase,
            "hurt has a visible recovery window");

        PlayerPresentationFrame checkpoint = timeline.Step(input with
        {
            HurtTriggered = false,
            CheckpointRecovering = true,
            Grounded = true,
            Velocity = Vector2.Zero
        }, step);
        TestAssert.Equal(PlayerLocomotionState.Recovery, checkpoint.Locomotion.State,
            "checkpoint recovery does not masquerade as hurt or death");
        PlayerPresentationFrame dead = timeline.Step(input with
        {
            HurtTriggered = false,
            CheckpointRecovering = false,
            Dead = true
        }, step);
        TestAssert.Equal(PlayerLocomotionState.Dead, dead.Locomotion.State,
            "death owns the base locomotion layer");
        TestAssert.Equal(PlayerActionFrame.None, dead.Action,
            "death clears transient upper-body actions");
    }

    private static void SixtyFpsTimelineIsDeterministic()
    {
        var first = new PlayerPresentationTimeline(new PlayerTuning());
        var second = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput input = Input(new Vector2(210f, -120f), grounded: false) with
        {
            AttackId = 91,
            IsAttacking = true,
            AttackDirection = AttackDirection.Up
        };
        PlayerPresentationFrame a = default;
        PlayerPresentationFrame b = default;
        for (int index = 0; index < 18; index++)
        {
            a = first.Step(input, 1f / 60f);
            b = second.Step(input, 1f / 60f);
            TestAssert.Equal(a, b,
                $"the same input sampled at 60 FPS produces the same frame at sample {index}");
        }
    }

    private static void AssertAttackTimeline(
        PlayerActionState expectedState,
        bool grounded,
        AttackDirection direction)
    {
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        PlayerPresentationInput input = Input(grounded ? new Vector2(160f, 0f) : new Vector2(80f, -160f),
            grounded) with
        {
            AttackId = 7,
            IsAttacking = true,
            AttackDirection = direction
        };
        var seen = new System.Collections.Generic.List<PlayerActionPhase>();
        Vector2 expectedAim = direction switch
        {
            AttackDirection.Up => Vector2.Up,
            AttackDirection.Down => Vector2.Down,
            _ => Vector2.Right
        };
        for (int index = 0; index < 20; index++)
        {
            PlayerPresentationFrame frame = timeline.Step(input, 1f / 60f);
            if (!seen.Contains(frame.Action.Phase)) seen.Add(frame.Action.Phase);
            TestAssert.Equal(expectedState, frame.Action.State,
                $"{expectedState} keeps one typed identity through its timeline");
            TestAssert.Equal(expectedAim, frame.Action.Aim,
                $"{expectedState} keeps its normalized attack aim through the timeline");
        }
        TestAssert.Equal(PlayerActionPhase.Anticipation, seen[0],
            $"{expectedState} starts with Anticipation");
        TestAssert.True(seen.Contains(PlayerActionPhase.Burst) && seen.Contains(PlayerActionPhase.Contact) &&
            seen.Contains(PlayerActionPhase.Recovery),
            $"{expectedState} exposes Burst, Contact, and Recovery without skipping them");
    }

    private static PlayerPresentationFrame StepUntil(
        PlayerPresentationTimeline timeline,
        PlayerPresentationInput input,
        PlayerLocomotionState expected)
    {
        for (int index = 0; index < 90; index++)
        {
            PlayerPresentationFrame frame = timeline.Step(input, 1f / 60f);
            if (frame.Locomotion.State == expected) return frame;
        }
        throw new System.InvalidOperationException($"Timed out waiting for locomotion {expected}");
    }

    private static PlayerPresentationFrame StepUntil(
        PlayerPresentationTimeline timeline,
        PlayerPresentationInput input,
        PlayerActionPhase expected)
    {
        for (int index = 0; index < 90; index++)
        {
            PlayerPresentationFrame frame = timeline.Step(input, 1f / 60f);
            if (frame.Action.Phase == expected) return frame;
        }
        throw new System.InvalidOperationException($"Timed out waiting for action phase {expected}");
    }

    public static void MechanicalBoostKeepsSpoolTension()
    {
        // Boost is still attached. Losing its Contact drive must not relax the
        // loaded spool for one frame, independently of any movement acceleration.
        const float step = 1f / 60f;
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        var claw = new MechanicalClawFrame(1, MechanicalClawState.Swing,
            MechanicalClawFailure.None, new Vector2(0f, -200f), 200f,
            Vector2.Zero, true, SurfaceNormal: Vector2.Down,
            DockPoint: new Vector2(0f, -152f), Arrived: false,
            LockedTargetRuntimeId: 2);
        PlayerPresentationInput input = Input(Vector2.Zero, grounded: false) with { Claw = claw };
        PlayerPresentationFrame loaded = default;
        for (int index = 0; index < 120; index++) loaded = timeline.Step(input, step);
        float before = loaded.Secondary.ShoulderSpool.RotationRadians;
        TestAssert.True(before < -.1f, "attached spool is visibly loaded before boost");
        PlayerPresentationFrame boosted = timeline.Step(input with
        {
            Claw = claw with { State = MechanicalClawState.Boost, BoostConsumed = true }
        }, step);
        float during = boosted.Secondary.ShoulderSpool.RotationRadians;
        GD.Print($"BOOST_SPOOL_TENSION before={before} during={during}");
        TestAssert.True(during <= before + .00001f,
            "accepted attached boost keeps spool tension instead of briefly relaxing it");
        PlayerPresentationFrame released = timeline.Step(input with
        {
            Claw = claw with { State = MechanicalClawState.Released,
                BoostConsumed = true, ReleaseKind = MechanicalClawReleaseKind.Boost }
        }, step);
        TestAssert.Equal(PlayerActionPhase.Contact, boosted.Action.Phase,
            "typed Boost remains a launch/tension contact action");
        TestAssert.Equal(PlayerActionPhase.Recovery, released.Action.Phase,
            "typed Released remains a distinct subsequent recovery action");
    }

    private static PlayerPresentationInput Input(Vector2 velocity, bool grounded) => new(
        velocity,
        grounded,
        Dashing: false,
        Dead: false,
        CheckpointRecovering: false,
        Facing: velocity.X < 0f ? -1 : 1);

    private static ThreadCastFrame Cast(
        ThreadCastState state,
        float duration = 0f,
        float remaining = 0f,
        Vector2? aim = null) => new(
        state == ThreadCastState.Idle ? 0UL : 1UL,
        state,
        ThreadAimMode.Tap,
        aim ?? Vector2.Right,
        state == ThreadCastState.Idle ? 0UL : 2UL,
        state is not ThreadCastState.Idle and not ThreadCastState.Failed,
        state == ThreadCastState.Failed ? ThreadFailureReason.NoTarget : ThreadFailureReason.None,
        duration,
        remaining);

    private static void PlayerStateMappingIsGameplayDriven()
    {
        TestAssert.Equal(EntityVisualState.Idle,
            PlayerPresentationCoordinator.ResolveFrame(Vector2.Zero, true, false, false, false, 1).State,
            "stationary grounded player is idle");
        TestAssert.Equal(EntityVisualState.Move,
            PlayerPresentationCoordinator.ResolveFrame(new Vector2(20f, 0f), true, false, false, false, 1).State,
            "grounded horizontal velocity is move");
        TestAssert.Equal(EntityVisualState.Rise,
            PlayerPresentationCoordinator.ResolveFrame(new Vector2(0f, -20f), false, false, false, false, 1).State,
            "negative airborne velocity is rise");
        TestAssert.Equal(EntityVisualState.Fall,
            PlayerPresentationCoordinator.ResolveFrame(new Vector2(0f, 20f), false, false, false, false, 1).State,
            "positive airborne velocity is fall");
        TestAssert.Equal(EntityVisualState.Dash,
            PlayerPresentationCoordinator.ResolveFrame(Vector2.Right, false, true, false, false, 1).State,
            "active dash takes precedence over airborne movement");
        TestAssert.Equal(EntityVisualState.Attack,
            PlayerPresentationCoordinator.ResolveFrame(Vector2.Zero, true, false, true, false, 1).State,
            "active attack takes precedence over locomotion");
        TestAssert.Equal(EntityVisualState.Dead,
            PlayerPresentationCoordinator.ResolveFrame(Vector2.Zero, true, true, true, true, 1).State,
            "death takes precedence over every player state");
        TestAssert.Equal(-1,
            PlayerPresentationCoordinator.ResolveFrame(Vector2.Zero, true, false, false, false, -1).Facing,
            "player facing is preserved");
    }

    private static void RustShellStateMappingIsGameplayDriven()
    {
        TestAssert.Equal(EntityVisualState.Idle,
            RustShellPresentationCoordinator.ResolveFrame(new RustShellFrame(RustShellState.Idle, 0f, false, false), 1).State,
            "idle squire maps to idle");
        TestAssert.Equal(EntityVisualState.Move,
            RustShellPresentationCoordinator.ResolveFrame(new RustShellFrame(RustShellState.Chase, 1f, false, false), 1).State,
            "chasing squire maps to move");
        TestAssert.Equal(EntityVisualState.Attack,
            RustShellPresentationCoordinator.ResolveFrame(new RustShellFrame(RustShellState.Attack, 0f, true, false), -1).State,
            "attacking squire maps to attack");
        TestAssert.Equal(EntityVisualState.Hurt,
            RustShellPresentationCoordinator.ResolveFrame(new RustShellFrame(RustShellState.Stunned, 0f, false, false), 1).State,
            "stunned squire maps to hurt");
        TestAssert.Equal(EntityVisualState.Dead,
            RustShellPresentationCoordinator.ResolveFrame(new RustShellFrame(RustShellState.Dead, 0f, false, false), 1).State,
            "dead squire maps to dead");
    }

    private static void CoordinatorDeliversFramesAndCuesOnce()
    {
        bool previous = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        var root = new Node2D { Name = "VisualRoot" };
        var visual = new RecordingEntityVisual { Name = "Placeholder" };
        root.AddChild(visual);
        var coordinator = new PlayerPresentationCoordinator { Profile = new PresentationProfile() };

        coordinator.BindVisualRoot(root);
        var frame = new EntityPresentationFrame(EntityVisualState.Move, -1, true);
        coordinator.Present(frame);
        coordinator.PlayCue(PresentationCue.AttackHit);

        TestAssert.Equal(frame, visual.LastFrame, "coordinator forwards the exact presentation frame");
        TestAssert.Equal(1, visual.CueCount, "coordinator forwards one cue exactly once");
        TestAssert.Equal(PresentationCue.AttackHit, visual.LastCue, "coordinator forwards the requested cue");
        coordinator.Free();
        root.Free();
        PrologueProductionMode.AllowDiagnosticFallbacks = previous;
    }

    private static void ValidVisualOverrideReplacesFallback()
    {
        var profile = new PresentationProfile { VisualScene = Pack(new RecordingEntityVisual { Name = "Override" }) };
        var root = FallbackRoot(out RecordingEntityVisual fallback);

        IEntityVisual2D resolved = profile.Resolve(root);

        TestAssert.True(resolved is RecordingEntityVisual && !ReferenceEquals(resolved, fallback),
            "valid packed visual is resolved instead of fallback");
        TestAssert.True(!fallback.Visible, "fallback hides when override is valid");
        root.Free();
        profile.Dispose();
    }

    private static void InvalidVisualOverrideKeepsFallback()
    {
        var profile = new PresentationProfile { VisualScene = Pack(new Node2D { Name = "InvalidOverride" }) };
        var root = FallbackRoot(out RecordingEntityVisual fallback);

        IEntityVisual2D resolved = profile.Resolve(root, out PresentationDiagnostic diagnostic);

        TestAssert.True(ReferenceEquals(fallback, resolved), "invalid packed visual falls back to procedural visual");
        TestAssert.True(fallback.Visible, "fallback stays visible after invalid override");
        TestAssert.Equal(PresentationDiagnosticCode.InvalidVisualRoot, diagnostic.Code,
            "invalid packed visual retains the real rejection diagnostic");
        TestAssert.True(diagnostic.Message.Contains("VisualRoot", System.StringComparison.Ordinal),
            "invalid packed visual identifies the resolution context");
        root.Free();
        profile.Dispose();
    }

    private static void MissingVisualOverrideKeepsFallback()
    {
        var profile = new PresentationProfile();
        var root = FallbackRoot(out RecordingEntityVisual fallback);

        IEntityVisual2D resolved = profile.Resolve(root);

        TestAssert.True(ReferenceEquals(fallback, resolved), "missing packed visual uses procedural fallback");
        TestAssert.True(fallback.Visible, "fallback stays visible without override");
        root.Free();
        profile.Dispose();
    }

    private static void ProductionModeRejectsFallback()
    {
        bool previous = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = false;
        var profile = new PresentationProfile();
        var root = FallbackRoot(out RecordingEntityVisual fallback);
        bool rejected = false;
        try { profile.Resolve(root); }
        catch (System.InvalidOperationException) { rejected = true; }
        TestAssert.True(rejected, "production mode fails fast when formal entity art is missing");
        TestAssert.True(!fallback.Visible, "production mode never reveals the entity placeholder");
        root.Free();
        profile.Dispose();
        PrologueProductionMode.AllowDiagnosticFallbacks = previous;
    }

    private static void CueScenesAcceptOnlyNode2DRoots()
    {
        var profile = new PresentationProfile();
        profile.CueScenes[PresentationCue.AttackHit.ToString()] = Pack(new Node2D { Name = "HitEffect" });
        profile.CueScenes[PresentationCue.Hurt.ToString()] = Pack(new Node { Name = "InvalidEffect" });

        bool valid = profile.TryInstantiateCue(PresentationCue.AttackHit, out Node2D hitEffect);
        bool invalid = profile.TryInstantiateCue(PresentationCue.Hurt, out Node2D invalidEffect,
            out PresentationDiagnostic diagnostic);

        TestAssert.True(valid && hitEffect != null, "Node2D cue scene is instantiated");
        TestAssert.True(!invalid && invalidEffect == null, "non-Node2D cue scene is rejected");
        TestAssert.Equal(PresentationDiagnosticCode.InvalidCueRoot, diagnostic.Code,
            "non-Node2D cue retains the real rejection diagnostic");
        TestAssert.True(diagnostic.Message.Contains("Hurt", System.StringComparison.Ordinal),
            "rejected cue diagnostic identifies the original cue");
        hitEffect.Free();
        profile.Dispose();
    }

    private static Node2D FallbackRoot(out RecordingEntityVisual fallback)
    {
        var root = new Node2D { Name = "VisualRoot" };
        fallback = new RecordingEntityVisual { Name = "Placeholder" };
        root.AddChild(fallback);
        return root;
    }

    private static PackedScene Pack(Node root)
    {
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(root), "test visual scene packs");
        root.Free();
        return packed;
    }
}

internal partial class RecordingEntityVisual : Node2D, IEntityVisual2D
{
    public EntityPresentationFrame LastFrame { get; private set; }
    public PresentationCue LastCue { get; private set; }
    public int CueCount { get; private set; }

    public void ApplyFrame(EntityPresentationFrame frame) => LastFrame = frame;

    public void PlayCue(PresentationCue cue)
    {
        LastCue = cue;
        CueCount++;
    }
}
