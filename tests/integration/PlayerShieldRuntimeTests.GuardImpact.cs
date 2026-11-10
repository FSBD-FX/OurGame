using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static partial class PlayerShieldRuntimeTests
{
    // Mutation target: disconnect ShieldResolved from the formal articulated wrists.
    // This is a controlled saved-player integration test, NOT a normal-input route
    // or a pixel/normal-speed acceptance test. No new production API precedes RED.
    public static async Task RunGuardImpact(SceneTree tree)
    {
        var rateSamples = new Dictionary<int, Vector2>();
        int cases = 0;
        foreach (int facing in new[] { 1, -1 })
        foreach (int rate in new[] { 30, 60, 120 })
        {
            Input.ActionRelease(InputActions.KeyboardShield);
            Fixture hit = await CreateFixture(tree);
            FreezeGuardImpactFixture(hit.Player);
            Fixture control = null;
            try
            {
                control = await CreateFixture(tree);
                FreezeGuardImpactFixture(control.Player);
                var actual = new GuardImpactRig(hit);
                var baseline = new GuardImpactRig(control);
                ActivateGuardImpactShield(hit, facing);
                ActivateGuardImpactShield(control, facing);

                // Both saved visuals consume the exact same producer frame/clock.
                // The control coordinator's native timeline is deliberately not run.
                void Step(double dt)
                {
                    actual.Coordinator._Process(dt);
                    baseline.Coordinator.Present(actual.Coordinator.LastPlayerFrame);
                }
                for (int i = 0; i < 30; i++) Step(.01d);
                TestAssert.True(actual.Coordinator.LastPlayerFrame.Action.State == PlayerActionState.Shield,
                    "guard impact starts from a settled real shield action after .30 presentation seconds");
                TestAssert.True((actual.NearVector - baseline.NearVector).Length() < .002f,
                    "paired saved-player no-hit controls have the same shoulder-relative wrist before impact");
                Vector2 rootBefore = hit.Player.GlobalPosition, velocityBefore = hit.Player.Velocity;
                int healthBefore = hit.Health.CurrentHealth;
                int accepted = 0;
                void Resolved(Knight.Player.PlayerShieldResult result, ulong transition)
                {
                    TestAssert.True(result.Guarded && result.DamageAmount == 0 &&
                        !result.SpendHeavyEnergy && !result.BrokeGuard && transition > 0,
                        "the real shield event is a normal zero-damage resolution, not Hurt or heavy guard");
                    accepted++;
                }
                hit.Shield.ShieldResolved += Resolved;
                var request = new DamageRequest(1, -hit.Shield.GuardDirection, 100f,
                    31901UL, 31900UL, DamageGuardClass.Normal);
                TestAssert.True(hit.Hurtbox.Receive(request), "normal frontal request is accepted by the real Hurtbox");
                TestAssert.Equal(healthBefore, hit.Health.CurrentHealth, "successful normal guard does not lose HP");
                TestAssert.Equal(1, accepted, "one real successful shield resolution is published");
                TestAssert.True(!hit.Hurtbox.Receive(request), "duplicate attack identity is rejected");
                TestAssert.Equal(1, accepted, "duplicate identity publishes no second shield resolution");

                // No positive time has passed since contact: an impulse is not a teleport.
                Step(0d);
                TestAssert.True((actual.NearVector - baseline.NearVector).Length() < .002f,
                    "zero elapsed contact does not teleport the wrist");
                float peak = 0f;
                Vector2 atTenth = Vector2.Zero;
                for (int tick = 1; tick <= rate * 3 / 5; tick++)
                {
                    Step(1d / rate);
                    Vector2 difference = actual.NearVector - baseline.NearVector;
                    peak = Mathf.Max(peak, difference.Length());
                    if (tick == rate / 10)
                    {
                        // FIRST missing-feature assertion: real rendered chain geometry,
                        // not a reflection/missing-field/compile failure.
                        TestAssert.True(difference.Length() > .2f,
                            $"GUARD_IMPACT_MOTION_RED successful zero-damage block needs actual wrist recoil; facing={facing} rate={rate} delta={difference}");
                        TestAssert.True(difference.Dot(-hit.Shield.GuardDirection) > .2f,
                            "guard wrist impact is opposite world aim, including mirrored facing");
                        TestAssert.True((actual.FarVector - baseline.FarVector).Length() > .05f,
                            "the free far arm also absorbs the actual guard impact");
                        TestAssert.True((actual.NearElbowVector - baseline.NearElbowVector).Length() > .02f,
                            "the actual near elbow articulates with the recoiling wrist");
                        atTenth = difference;
                        Vector2 nearPaused = actual.NearVector, farPaused = actual.FarVector;
                        for (int repeat = 0; repeat < 8; repeat++) Step(0d);
                        TestAssert.True(actual.NearVector.DistanceTo(nearPaused) < .002f &&
                            actual.FarVector.DistanceTo(farPaused) < .002f,
                            "repeated same-clock publication neither advances nor replays the impact");
                    }
                    AssertGuardImpactGeometry(actual);
                    TestAssert.Equal(healthBefore, hit.Health.CurrentHealth, "presentation recoil does not alter HP");
                    TestAssert.True(hit.Player.GlobalPosition == rootBefore && hit.Player.Velocity == velocityBefore,
                        "presentation impact has no root displacement or gameplay velocity");
                }
                TestAssert.True(peak > 1f && peak <= 3.1f,
                    $"isolated normal guard has a bounded nontrivial pulse, not sustained shake: {peak}");
                TestAssert.True((actual.NearVector - baseline.NearVector).Length() < .03f &&
                    (actual.FarVector - baseline.FarVector).Length() < .03f,
                    "both actual wrists return to matched no-hit pose within .60 presentation seconds");
                if (rateSamples.TryGetValue(facing, out Vector2 reference))
                    TestAssert.True(atTenth.DistanceTo(reference) < .025f,
                        "30/60/120 Hz yield the same real wrist offset at .10 seconds");
                else rateSamples.Add(facing, atTenth);

                // Distinct attacker identity remains accepted after the first pulse,
                // and a same-frame burst cannot grow the visible recoil unbounded.
                for (ulong source = 31902UL; source < 31918UL; source++)
                    TestAssert.True(hit.Hurtbox.Receive(request with { SourceRuntimeId = source }),
                        "a new runtime source with the same local attack ordinal is independently accepted");
                TestAssert.Equal(17, accepted, "all distinct guarded sources publish exactly once");
                Step(1d / 30d);
                TestAssert.True((actual.NearVector - baseline.NearVector).Length() is > .2f and <= 6.01f,
                    "dense same-clock valid contacts produce a bounded actual wrist pulse");

                Vector2 beforeRelease = actual.NearVector;
                Input.ActionRelease(InputActions.KeyboardShield);
                hit.Input._PhysicsProcess(0d); hit.Shield._PhysicsProcess(0d);
                Step(0d);
                TestAssert.True(actual.NearVector.DistanceTo(beforeRelease) < .002f,
                    "shield release keeps the already displayed wrist offset at the same clock");
                for (int i = 0; i < 36; i++) Step(1d / 60d);
                TestAssert.True((actual.NearVector - baseline.NearVector).Length() < .03f,
                    "release/regrip retires the impact instead of leaving stale recoil");
                AssertGuardImpactGeometry(actual);
                hit.Shield.ShieldResolved -= Resolved;
                cases++;
            }
            finally
            {
                Input.ActionRelease(InputActions.KeyboardShield);
                Input.ActionRelease(InputActions.Shield);
                Input.ActionRelease(InputActions.MoveLeft);
                Input.ActionRelease(InputActions.MoveRight);
                hit.Root.QueueFree();
                control?.Root.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        TestAssert.True(rateSamples[1].X < -.2f && rateSamples[-1].X > .2f &&
            Mathf.Abs(rateSamples[1].X + rateSamples[-1].X) < .025f,
            "opposite guard aims mirror world recoil rather than negating the wrong local axis");
        await GuardImpactLifecycleAndTypedTakeover(tree);
        await GuardImpactReviewRecoveryAndShieldLease(tree);
        GD.Print($"GUARD_IMPACT_CONTROLLED_OK cases={cases} actual_saved_player=true real_zero_damage_event=true paired_no_hit=true wrist_elbow=true rigid_24=true primary_grip=true mirror=true rates=30/60/120 zero_time=true bounded=true rest=true release=true controlled_timing=true natural_route=false pixels=false");
    }

    // Review regressions deliberately aggregate all behavioral failures. A live
    // public recovery reset and the Shield node's own exit/enter are independent
    // revocation routes; a first failure must not hide either remaining route.
    private static async Task GuardImpactReviewRecoveryAndShieldLease(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (int scenario in new[] { 0, 1, 2 })
        {
            Input.ActionRelease(InputActions.KeyboardShield);
            Fixture hit = await CreateFixture(tree);
            FreezeGuardImpactFixture(hit.Player);
            Fixture control = null;
            PlayerPresentationCoordinator coordinator = null;
            try
            {
                control = await CreateFixture(tree);
                FreezeGuardImpactFixture(control.Player);
                var actual = new GuardImpactRig(hit);
                var baseline = new GuardImpactRig(control);
                coordinator = actual.Coordinator;
                ActivateGuardImpactShield(hit, 1);
                ActivateGuardImpactShield(control, 1);
                ulong attack = 32100UL;
                void Step(double dt)
                {
                    coordinator._Process(dt);
                    baseline.Coordinator.Present(coordinator.LastPlayerFrame);
                }
                void ReceiveFresh()
                {
                    int hp = hit.Health.CurrentHealth;
                    TestAssert.True(hit.Hurtbox.Receive(new DamageRequest(1, -hit.Shield.GuardDirection,
                        100f, ++attack, 32100UL, DamageGuardClass.Normal)),
                        "review regression precondition accepts a fresh real normal guarded request");
                    TestAssert.Equal(hp, hit.Health.CurrentHealth,
                        "review regression contact is zero damage, not Hurt/death");
                }
                void RecordNoRecoil(string id)
                {
                    float near = actual.NearVector.DistanceTo(baseline.NearVector);
                    float far = actual.FarVector.DistanceTo(baseline.FarVector);
                    bool passed = near < .002f && far < .002f;
                    if (!passed) failures.Add($"{id}(near={near:F4},far={far:F4})");
                    GD.Print($"GUARD_IMPACT_REVIEW_CASE id={id} passed={passed.ToString().ToLowerInvariant()} near={near:F4} far={far:F4}");
                    AssertGuardImpactGeometry(actual);
                }
                for (int i = 0; i < 30; i++) Step(.01d);
                TestAssert.True(coordinator.LastPlayerFrame.Action.State == PlayerActionState.Shield &&
                    actual.NearVector.DistanceTo(baseline.NearVector) < .002f,
                    "review regression begins with matched settled saved-player shield poses");
                ReceiveFresh();
                Step(.04d);
                TestAssert.True(hit.Health.CurrentHealth > 0 &&
                    actual.NearVector.DistanceTo(baseline.NearVector) > .2f,
                    "review regression starts alive with actual nonzero zero-damage block recoil");

                if (scenario == 0)
                {
                    // No preceding death is allowed: it would mask a missing
                    // clear in this independent public reset entry point.
                    coordinator.ResetRecoveryPresentation();
                    TestAssert.True(hit.Health.CurrentHealth > 0 && hit.Shield.IsActive,
                        "public presentation reset regression never kills or lowers the player's shield");
                    RecordNoRecoil("I1_LIVE_PUBLIC_RESET_IMMEDIATE");
                    Step(.1d);
                    RecordNoRecoil("I1_LIVE_PUBLIC_RESET_NO_REPLAY");
                }
                else if (scenario == 1)
                {
                    // Only the source Shield node leaves; coordinator and formal
                    // stay live. No intervening presentation frame may do cleanup.
                    hit.Player.RemoveChild(hit.Shield);
                    TestAssert.True(!hit.Shield.IsInsideTree() && coordinator.IsInsideTree(),
                        "ordinary source withdrawal removes Shield itself, not its coordinator");
                    RecordNoRecoil("I2_SHIELD_EXIT_IMMEDIATE");
                    hit.Player.AddChild(hit.Shield);
                    Step(.1d);
                    RecordNoRecoil("I2_SHIELD_REENTER_NO_REPLAY");
                }
                else
                {
                    // Force observer-before-production ordering without private
                    // event inspection. The existing coordinator detach control
                    // clears the setup pulse before this distinct callback test.
                    hit.Player.RemoveChild(coordinator);
                    hit.Player.RemoveChild(hit.Shield);
                    bool replacedInsideCallback = false;
                    void ReplaceShieldLease(Knight.Player.PlayerShieldResult result, ulong transition)
                    {
                        if (replacedInsideCallback) return;
                        replacedInsideCallback = true;
                        hit.Player.RemoveChild(hit.Shield);
                        hit.Player.AddChild(hit.Shield);
                    }
                    hit.Shield.ShieldResolved += ReplaceShieldLease;
                    hit.Player.AddChild(hit.Shield);
                    hit.Player.AddChild(coordinator);
                    try
                    {
                        ReceiveFresh();
                        TestAssert.True(replacedInsideCallback && hit.Shield.IsInsideTree() &&
                            coordinator.IsInsideTree(),
                            "real ShieldResolved observer replaces Shield itself within the captured event invocation");
                        Step(.1d);
                        RecordNoRecoil("I2_SHIELD_CALLBACK_EXPIRED_LEASE");
                    }
                    finally { hit.Shield.ShieldResolved -= ReplaceShieldLease; }
                }

                // Independently prove that revocation is not implemented as
                // permanent suppression. First let any captured failure decay,
                // then require a newly accepted real contact to move the wrist.
                for (int i = 0; i < 60; i++) Step(.01d);
                ReceiveFresh();
                Step(.04d);
                bool fresh = actual.NearVector.DistanceTo(baseline.NearVector) > .2f;
                if (!fresh) failures.Add($"SCENARIO_{scenario}_FRESH_CONTACT_SUPPRESSED");
                GD.Print($"GUARD_IMPACT_REVIEW_CASE id=SCENARIO_{scenario}_FRESH_CONTACT passed={fresh.ToString().ToLowerInvariant()}");
                AssertGuardImpactGeometry(actual);
            }
            finally
            {
                Input.ActionRelease(InputActions.KeyboardShield);
                Input.ActionRelease(InputActions.Shield);
                Input.ActionRelease(InputActions.MoveLeft); Input.ActionRelease(InputActions.MoveRight);
                // These nodes belong to this fixture. Reattach after a failed
                // strict precondition so QueueFree still owns all test nodes.
                if (hit.Shield.GetParent() == null) hit.Player.AddChild(hit.Shield);
                if (coordinator != null && coordinator.GetParent() == null) hit.Player.AddChild(coordinator);
                hit.Root.QueueFree(); control?.Root.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        TestAssert.True(failures.Count == 0,
            $"GUARD_IMPACT_REVIEW_RED all three review routes executed; failures={string.Join(";", failures)}");
        GD.Print("GUARD_IMPACT_REVIEW_BOUNDARIES_OK live_public_reset=true shield_exit_redraw=true shield_reenter_fresh=true shield_callback_lease=true aggregated_cases=3 controlled_timing=true natural_route=false pixels=false");
    }

    // Mutation targets: leaving compressed wrists on source withdrawal; accepting
    // an already-captured event delegate after its lease was replaced mid-callback;
    // preserving old impulse across death/reset; snapping at a same-clock takeover.
    private static async Task GuardImpactLifecycleAndTypedTakeover(SceneTree tree)
    {
        Input.ActionRelease(InputActions.KeyboardShield);
        Fixture hit = await CreateFixture(tree);
        FreezeGuardImpactFixture(hit.Player);
        Fixture control = null;
        try
        {
            control = await CreateFixture(tree);
            FreezeGuardImpactFixture(control.Player);
            var actual = new GuardImpactRig(hit);
            var baseline = new GuardImpactRig(control);
            ActivateGuardImpactShield(hit, 1);
            ActivateGuardImpactShield(control, 1);
            ulong attack = 32000UL;
            void Step(double dt)
            {
                actual.Coordinator._Process(dt);
                baseline.Coordinator.Present(actual.Coordinator.LastPlayerFrame);
            }
            void SettleShield()
            {
                for (int i = 0; i < 30; i++) Step(.01d);
                TestAssert.True(actual.Coordinator.LastPlayerFrame.Action.State == PlayerActionState.Shield,
                    "lifecycle control starts from a real settled shield frame");
            }
            void ReceiveFresh()
            {
                int hp = hit.Health.CurrentHealth;
                TestAssert.True(hit.Hurtbox.Receive(new DamageRequest(1, -hit.Shield.GuardDirection,
                    100f, ++attack, 32000UL, DamageGuardClass.Normal)),
                    "lifecycle fixture consumes a fresh real normal block");
                TestAssert.Equal(hp, hit.Health.CurrentHealth, "lifecycle normal block remains zero damage");
            }
            void AssertNoRecoil(string message)
            {
                TestAssert.True(actual.NearVector.DistanceTo(baseline.NearVector) < .002f &&
                    actual.FarVector.DistanceTo(baseline.FarVector) < .002f, message);
                AssertGuardImpactGeometry(actual);
            }
            void AssertLivePulse()
            {
                Step(.04d);
                TestAssert.True(actual.NearVector.DistanceTo(baseline.NearVector) > .2f,
                    "lifecycle precondition has a visible nonzero real-contact pulse");
                AssertGuardImpactGeometry(actual);
            }
            SettleShield();
            ReceiveFresh(); AssertLivePulse();
            hit.Player.RemoveChild(actual.Coordinator);
            AssertNoRecoil("source withdrawal immediately redraws both wrists without their old compression");
            hit.Player.AddChild(actual.Coordinator);
            Step(.1d);
            AssertNoRecoil("reattaching the coordinator does not replay its old impact");
            ReceiveFresh(); AssertLivePulse();

            // Install the observer FIRST in the next event invocation list.
            // Removing/readding does not alter a C# event's captured old invocation
            // list: its trailing production handler must reject the expired lease.
            hit.Player.RemoveChild(actual.Coordinator);
            bool replacedInsideCallback = false;
            void ReplaceLease(Knight.Player.PlayerShieldResult result, ulong transition)
            {
                if (replacedInsideCallback) return;
                replacedInsideCallback = true;
                hit.Player.RemoveChild(actual.Coordinator);
                hit.Player.AddChild(actual.Coordinator);
            }
            hit.Shield.ShieldResolved += ReplaceLease;
            hit.Player.AddChild(actual.Coordinator);
            try
            {
                ReceiveFresh();
                TestAssert.True(replacedInsideCallback,
                    "real ShieldResolved callback actually withdraws and replaces the coordinator lease");
                Step(.1d);
                AssertNoRecoil("GUARD_IMPACT_LEASE_RED an old captured shield callback must not pulse a replacement source lease");
            }
            finally { hit.Shield.ShieldResolved -= ReplaceLease; }
            ReceiveFresh(); AssertLivePulse();

            // Death goes through actual Health signals, not a hand-authored Dead
            // frame. ResetFull + ResetRecoveryPresentation is the public fixture
            // reset route; this is not a claim of checkpoint/level recovery input.
            var fatal = new DamageRequest(99, Vector2.Right, 0f, ++attack, 32000UL);
            TestAssert.True(hit.Health.ApplyDamage(fatal), "actual health accepts the lifecycle fatal request");
            TestAssert.True(control.Health.ApplyDamage(fatal),
                "no-hit control receives the same real death signals so cue/pose history stays matched");
            TestAssert.Equal(0, hit.Health.CurrentHealth, "lifecycle death is real zero HP");
            baseline.Coordinator.Present(actual.Coordinator.LastPlayerFrame);
            TestAssert.True(actual.Coordinator.LastPlayerFrame.Locomotion.State == PlayerLocomotionState.Dead,
                "actual death signal publishes the formal dead frame immediately");
            AssertNoRecoil("death immediately clears actual wrist recoil rather than retaining the last live offset");
            hit.Health.ResetFull();
            control.Health.ResetFull();
            actual.Coordinator.ResetRecoveryPresentation();
            baseline.Coordinator.ResetRecoveryPresentation();
            ActivateGuardImpactShield(hit, 1);
            ActivateGuardImpactShield(control, 1);
            SettleShield();
            AssertNoRecoil("health/presentation reset does not resurrect a previous life's impact");
            ReceiveFresh(); AssertLivePulse();

            // Presentation ownership only: direct typed GroundAttack is expressly
            // NOT actual J, combat arbitration, or normal-speed attack acceptance.
            Vector2 nearBefore = actual.NearVector, farBefore = actual.FarVector;
            PlayerPresentationFrame shieldFrame = actual.Coordinator.LastPlayerFrame;
            PlayerPresentationFrame takeover = shieldFrame with
            {
                Action = new PlayerActionFrame(PlayerActionState.GroundAttack,
                    PlayerActionPhase.Anticipation, 0f, false, Vector2.Right)
            };
            actual.Coordinator.Present(takeover);
            baseline.Coordinator.Present(takeover);
            TestAssert.True(actual.NearVector.DistanceTo(nearBefore) < .002f &&
                actual.FarVector.DistanceTo(farBefore) < .002f,
                "same-clock typed attack takeover preserves the already displayed wrist offsets");
            AssertGuardImpactGeometry(actual);
            for (int i = 0; i < 8; i++) actual.Coordinator.Present(takeover);
            TestAssert.True(actual.NearVector.DistanceTo(nearBefore) < .002f &&
                actual.FarVector.DistanceTo(farBefore) < .002f,
                "repeating a zero-time typed attack takeover does not pump or erase recoil");
            double start = takeover.PresentationTimeSeconds.Value;
            for (int i = 1; i <= 36; i++)
            {
                PlayerPresentationFrame advancing = takeover with { PresentationTimeSeconds = start + i / 60d };
                actual.Coordinator.Present(advancing);
                baseline.Coordinator.Present(advancing);
                AssertGuardImpactGeometry(actual);
            }
            AssertNoRecoil("typed attack takeover retires its inherited world offset within .60 seconds");
            GD.Print("GUARD_IMPACT_LIFECYCLE_OK detach_redraw=true reattach_fresh=true callback_lease=true death_signals=true health_reset=true typed_attack_same_clock=true controlled_timing=true checkpoint_route=false natural_route=false pixels=false");
        }
        finally
        {
            Input.ActionRelease(InputActions.KeyboardShield);
            Input.ActionRelease(InputActions.Shield);
            Input.ActionRelease(InputActions.MoveLeft); Input.ActionRelease(InputActions.MoveRight);
            hit.Root.QueueFree(); control?.Root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void FreezeGuardImpactFixture(Node node)
    {
        node.SetProcess(false);
        node.SetPhysicsProcess(false);
        foreach (Node child in node.GetChildren()) FreezeGuardImpactFixture(child);
    }

    private static void ActivateGuardImpactShield(Fixture fixture, int facing)
    {
        fixture.Health.ResetFull(); fixture.Energy.ResetFull(); fixture.Shield.ResetFull();
        Input.ActionRelease(InputActions.Shield);
        Input.ActionRelease(InputActions.MoveLeft); Input.ActionRelease(InputActions.MoveRight);
        Input.ActionPress(facing < 0 ? InputActions.MoveLeft : InputActions.MoveRight);
        Input.ActionPress(InputActions.KeyboardShield); // Logical L action, no OS event.
        fixture.Input._PhysicsProcess(0d);
        Input.ActionRelease(InputActions.MoveLeft); Input.ActionRelease(InputActions.MoveRight);
        fixture.Input._PhysicsProcess(0d);
        fixture.Shield._PhysicsProcess(0d);
        fixture.Energy._PhysicsProcess(0d);
        fixture.Shield._PhysicsProcess(0d);
        TestAssert.True(fixture.Shield.IsActive && fixture.Shield.GuardDirection.X * facing > .99f,
            "controlled logical L holds a real shield with the requested horizontal keyboard aim");
    }

    private static void AssertGuardImpactGeometry(GuardImpactRig rig)
    {
        foreach (PlayerFormalLayer arm in new[] { rig.Near, rig.Far })
        {
            TestAssert.True(arm.ArmWristLocal.IsFinite() && arm.ArmElbowLocal.IsFinite(),
                "impact wrists and elbows remain finite");
            TestAssert.Near(24f, arm.ArmElbowLocal.Length(), .002f, "impact upper arm remains 24px");
            TestAssert.Near(24f, arm.ArmElbowLocal.DistanceTo(arm.ArmWristLocal), .002f,
                "impact forearm remains 24px");
        }
        // Independent approved master-canvas primary grip, not an invented helper point.
        Vector2 primary = rig.Weapon.ToGlobal(rig.Weapon.WeaponDrawRect.Position +
            (new Vector2(835f, 798f) - rig.Weapon.WeaponSourceRect.Position) * (25f / 287f));
        TestAssert.True(primary.DistanceTo(rig.Near.ToGlobal(rig.Near.ArmWristLocal)) < .002f,
            "the original weapon bitmap primary grip follows the actual impact wrist without one-frame lag");
    }

    private sealed class GuardImpactRig
    {
        public readonly PlayerPresentationCoordinator Coordinator;
        public readonly PlayerFormalLayer Near, Far, Weapon;
        public GuardImpactRig(Fixture fixture)
        {
            Coordinator = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            TestAssert.True(Coordinator.ActiveVisual is PlayerFormalVisual,
                "guard impact fixture uses the actual saved player's formal visual");
            var formal = (PlayerFormalVisual)Coordinator.ActiveVisual;
            Near = formal.GetNode<PlayerFormalLayer>("NearArm");
            Far = formal.GetNode<PlayerFormalLayer>("FarArm");
            Weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        }
        public Vector2 NearVector => Near.ToGlobal(Near.ArmWristLocal) - Near.GlobalPosition;
        public Vector2 FarVector => Far.ToGlobal(Far.ArmWristLocal) - Far.GlobalPosition;
        public Vector2 NearElbowVector => Near.ToGlobal(Near.ArmElbowLocal) - Near.GlobalPosition;
    }
}
