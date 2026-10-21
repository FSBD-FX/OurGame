using System;
using System.Collections;
using System.Linq;
using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class ThrusterPresentationModelTests
{
    public static void RunBrakeCrossing()
    {
        var property = typeof(ThrusterDynamicsResult).GetProperty("BrakeReachedZero");
        TestAssert.True(property != null, "actual reverse-brake zero-crossing fact is unavailable");
        using var tuning = new Knight.Config.PlayerTuning();
        var motion = tuning.ToMotionSettings();
        foreach (var row in new (float speed, double seconds, bool floor, bool zero, float want)[] { (100f, .02, true, false, 20f),
            (100f, .025, true, true, 0f), (100f, .03, true, true, -11f),
            (100f, .02, false, false, 40f), (100f, .05, false, true, -25f) })
        {
            var result = ThrusterDynamics.Step(new(new Vector2(row.speed, 0f), -1f, row.floor,
                Vector2.Zero, 0f), MobilityModuleSettings.Standard, motion, row.seconds);
            TestAssert.Equal(row.zero, (bool)property.GetValue(result), "actual control branch reports reaching or crossing zero");
            TestAssert.Near(row.want, result.Velocity.X, .003f, "reporting zero crossing leaves independent piecewise physics unchanged");
            var model = new PlayerMotionModel(motion);
            var state = new PlayerMotionState { Velocity = new Vector2(row.speed, 0f) };
            var frame = new PlayerInputFrame { MoveAxis = -1f };
            PlayerMotionResult actual = model.StepFree(state, frame, row.floor, row.seconds,
                MobilityModuleSettings.Standard, Vector2.Zero, 0f, false, false);
            var motorFact = typeof(PlayerMotionResult).GetProperty("BrakeReachedZero");
            TestAssert.True(motorFact != null && (bool)motorFact.GetValue(actual) == row.zero,
                "real free-motion consumer forwards the same branch fact, not post-contact speed guessing");
        }
        GD.Print("THRUSTER_BRAKE_CROSSING_OK actual_control_branch=true stopped_and_crossed=true independent_piecewise_values=true free_motion_forwarded=true presentation_no_second_velocity=true");
    }

    public static void Run()
    {
        Type type = typeof(PlayerMotor).Assembly.GetType("Knight.Presentation.ThrusterPresentationModel");
        TestAssert.True(type != null, "typed thruster presentation observation is unavailable");
        StartAndEndSurviveOneRenderInterval(type);
        CoastingHasFiniteTimeAndIndependentBraking(type);
        AllEarlyExitsClearOnlyObservation(type);
        NoThrustCannotCreateCoasting(type);
        SupersededIdentityCannotAct(type);
        JournalIsBoundedAndReportsItsFloor(type);
        ActualEndReasonsAndEpoch(type);
        GD.Print("THRUSTER_PRESENTATION_MODEL_OK real_pulse_identities=true unique_start_end=true finite_coast=true orthogonal_braking=true early_exits=true stale_identity_rejected=true bounded_journal=true player_wired=false");
    }

    private static object New(Type type) => Activator.CreateInstance(type, new object[] { 71UL });
    private static void ActualEndReasonsAndEpoch(Type type)
    {
        foreach (ThrusterEndReason reason in Enum.GetValues<ThrusterEndReason>())
        {
            if (reason == ThrusterEndReason.None) continue;
            object model = New(type); var pulse = Pulse(); var accepted = Start(pulse);
            Call(model, "Start", accepted, Vector2.Right);
            Call(model, "RecordThrust", accepted.Identity, new Vector2(10f, 0f));
            var end = reason is ThrusterEndReason.Exhausted or ThrusterEndReason.ReverseCancelled
                ? pulse.ApplyDynamics(accepted.Identity, 0f, reason == ThrusterEndReason.ReverseCancelled)
                : reason is ThrusterEndReason.Reset or ThrusterEndReason.Death
                    ? pulse.ResetLifecycle(reason) : pulse.Interrupt(reason);
            TestAssert.True((bool)Call(model, "End", end), "real active end is recorded before any context synchronization");
            TestAssert.Equal(reason, Fact<ThrusterEndReason>(Read(model, 0UL)[1], "Reason"), "real end reason remains exact");
            TestAssert.Equal(reason is ThrusterEndReason.Exhausted or ThrusterEndReason.ReverseCancelled,
                Fact<bool>(Frame(model), "Coasting"), "only a natural finite end or reverse cancellation allows short coast");
            Call(model, "Synchronize", pulse.Snapshot);
            TestAssert.Equal(2, Read(model, 0UL).Length, "synchronization doesn't drop or duplicate the old active end");
        }
        object changed = Coast(type, out var oldPulse, out var oldIdentity);
        TestAssert.True(oldPulse.TryReplaceIdleModule(MobilityModuleSettings.Standard), "actual idle config replacement increments epoch");
        Call(changed, "Synchronize", oldPulse.Snapshot);
        TestAssert.True(!Fact<bool>(Frame(changed), "Coasting") && Fact<ThrusterIdentity>(Frame(changed), "Identity") == default,
            "actual new config epoch retires old coast");
        TestAssert.True(!(bool)Call(changed, "RecordThrust", oldIdentity, new Vector2(50f, 0f)),
            "old epoch force cannot revive observation");
    }
    private static object Call(object model, string name, params object[] values) =>
        model.GetType().GetMethod(name).Invoke(model, values);
    private static T Value<T>(object model, string name) => (T)model.GetType().GetProperty(name).GetValue(model);
    private static object Frame(object model) => Value<object>(model, "Frame");
    private static T Fact<T>(object frame, string name) => (T)frame.GetType().GetProperty(name).GetValue(frame);
    private static object Exit(object model, string name) => Enum.Parse(model.GetType().Assembly.GetType("Knight.Presentation.ThrusterCoastExit"), name);
    private static void Step(object model, float delta, bool braking = false, string exit = "None") =>
        Call(model, "Step", delta, braking, Exit(model, exit));
    private static object[] Read(object model, ulong after) => ((IEnumerable)Call(model, "ReadTransitionsAfter", after)).Cast<object>().ToArray();
    private static ThrusterPulseModel Pulse() => new(MobilityModuleSettings.Standard, 71UL);
    private static ThrusterPulseCommitResult Start(ThrusterPulseModel pulse, ulong sample = 1UL) => pulse.TryCommit(pulse.Preview(
        new(sample, true, false, true, true, false, false, Vector2.Right, pulse.Lifecycle, pulse.ConfigurationEpoch)));

    private static void StartAndEndSurviveOneRenderInterval(Type type)
    {
        object model = New(type); var pulse = Pulse(); var accepted = Start(pulse);
        TestAssert.True((bool)Call(model, "Start", accepted, Vector2.Right), "real accepted commit starts observation");
        TestAssert.True(!(bool)Call(model, "Start", accepted, Vector2.Right), "same accepted identity cannot emit twice");
        TestAssert.True(Fact<bool>(Frame(model), "Burning"), "actual start is burning");
        TestAssert.True((bool)Call(model, "RecordThrust", accepted.Identity, new Vector2(10f, 0f)), "matching actual nonzero thrust is observed");
        ThrusterPulseEnd end = pulse.ApplyDynamics(accepted.Identity, 0f, false);
        TestAssert.True((bool)Call(model, "End", end), "actual finite pulse end is observed");
        TestAssert.True(!(bool)Call(model, "End", end), "actual end emits once");
        object[] facts = Read(model, 0UL);
        TestAssert.Equal(2, facts.Length, "start and end both survive without an intervening render read");
        TestAssert.Equal("Started", Fact<object>(facts[0], "Kind").ToString(), "first fact is accepted start");
        TestAssert.Equal("Ended", Fact<object>(facts[1], "Kind").ToString(), "second fact is real end");
        TestAssert.Equal(accepted.Identity, Fact<ThrusterIdentity>(facts[0], "Identity"), "start retains actual owner/life/config/burn");
        TestAssert.Equal(accepted.Identity, Fact<ThrusterIdentity>(facts[1], "Identity"), "end retains the same identity after pulse cleared it");
        TestAssert.Equal(ThrusterEndReason.Exhausted, Fact<ThrusterEndReason>(facts[1], "Reason"), "end keeps actual reason");
        TestAssert.Equal(1UL, Fact<ulong>(facts[0], "Sequence"), "first monotonic presentation sequence");
        TestAssert.Equal(2UL, Fact<ulong>(facts[1], "Sequence"), "second monotonic presentation sequence");
        TestAssert.Equal(0, Read(model, 2UL).Length, "consumer cursor does not replay facts");
        TestAssert.True(!Fact<bool>(Frame(model), "Burning") && Fact<bool>(Frame(model), "Coasting"), "finite end stops flame while real short coast observation remains");
    }

    private static object Coast(Type type, out ThrusterPulseModel pulse, out ThrusterIdentity identity)
    {
        object model = New(type); pulse = Pulse(); var commit = Start(pulse); identity = commit.Identity;
        Call(model, "Start", commit, Vector2.Right);
        Call(model, "RecordThrust", identity, new Vector2(10f, 0f));
        Call(model, "End", pulse.ApplyDynamics(identity, 0f, false));
        return model;
    }

    private static void CoastingHasFiniteTimeAndIndependentBraking(Type type)
    {
        object model = Coast(type, out var pulse, out var identity);
        ThrusterPulseSnapshot gameplay = pulse.Snapshot;
        TestAssert.Near(.45f, Fact<float>(Frame(model), "CoastRemaining"), .000001f, "new coast is at most .45 seconds");
        Step(model, .1f, true);
        TestAssert.True(Fact<bool>(Frame(model), "Coasting") && Fact<bool>(Frame(model), "Braking"), "actual braking may coexist with coast");
        TestAssert.Equal(identity, Fact<ThrusterIdentity>(Frame(model), "Identity"), "coast keeps its ended burn identity");
        TestAssert.Near(.35f, Fact<float>(Frame(model), "CoastRemaining"), .000001f, "coast advances by gameplay delta");
        object frozen = Frame(model); Step(model, 0f, true);
        TestAssert.Equal(frozen, Frame(model), "zero-time paused observation is unchanged");
        Step(model, .2f, true); Step(model, .149f, true);
        TestAssert.True(Fact<bool>(Frame(model), "Coasting"), "coast remains before independently summed .45 boundary");
        Step(model, .0011f, true);
        TestAssert.True(!Fact<bool>(Frame(model), "Coasting") && Fact<bool>(Frame(model), "Braking"), "timeout clears coast without suppressing ordinary braking");
        TestAssert.Equal(default(ThrusterIdentity), Fact<ThrusterIdentity>(Frame(model), "Identity"), "ordinary brake after timeout has no old BurnId");
        TestAssert.Equal(gameplay, pulse.Snapshot, "presentation timing never mutates physical pulse/budget");
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            object before = Frame(model); bool rejected = false;
            try { Step(model, invalid); } catch (System.Reflection.TargetInvocationException e) when (e.InnerException is ArgumentOutOfRangeException) { rejected = true; }
            TestAssert.True(rejected, "invalid observation delta rejects before mutation");
            TestAssert.Equal(before, Frame(model), "invalid observation preserves state");
        }
    }

    private static void AllEarlyExitsClearOnlyObservation(Type type)
    {
        foreach (string exit in new[] { "Jump", "ClawAttachment", "ExternalReplacement", "Landing", "CollisionBlocked", "BrakedToZero", "Reset", "Death", "Superseded", "ConfigurationChange" })
        {
            object model = Coast(type, out var pulse, out _); var physical = pulse.Snapshot;
            Step(model, .01f, false, exit);
            TestAssert.True(!Fact<bool>(Frame(model), "Coasting"), "early exit retires short observation: " + exit);
            TestAssert.Equal(default(ThrusterIdentity), Fact<ThrusterIdentity>(Frame(model), "Identity"), "early exit loses stale observation identity: " + exit);
            TestAssert.Equal(physical, pulse.Snapshot, "early exit never changes actual pulse/budget: " + exit);
            Step(model, .01f);
            TestAssert.True(!Fact<bool>(Frame(model), "Coasting"), "ordinary later step cannot resurrect coast: " + exit);
        }
    }

    private static void NoThrustCannotCreateCoasting(Type type)
    {
        object model = New(type); var pulse = Pulse(); var commit = Start(pulse);
        Call(model, "Start", commit, Vector2.Right);
        TestAssert.True(!(bool)Call(model, "RecordThrust", commit.Identity, Vector2.Zero), "zero thrust does not claim jet motion");
        Call(model, "End", pulse.ApplyDynamics(commit.Identity, 0f, false));
        TestAssert.True(!Fact<bool>(Frame(model), "Coasting"), "accepted but unpowered end does not invent coast");
        Step(model, .1f, true);
        TestAssert.True(Fact<bool>(Frame(model), "Braking"), "ordinary braking can be observed without jet");
        TestAssert.Equal(default(ThrusterIdentity), Fact<ThrusterIdentity>(Frame(model), "Identity"), "ordinary brake has no fabricated burn");
        TestAssert.True(!(bool)Call(model, "Start", new ThrusterPulseCommitResult(false, ThrusterRejectReason.Burning, default), Vector2.Right), "rejected gameplay request is not a successful event");
        TestAssert.Equal(2UL, Value<ulong>(model, "LatestSequence"), "rejection does not append success facts");
    }

    private static void SupersededIdentityCannotAct(Type type)
    {
        object model = Coast(type, out var pulse, out var oldIdentity);
        ThrusterPulseEnd staleEnd = new(true, oldIdentity, ThrusterEndReason.Exhausted);
        pulse.ResetLifecycle(ThrusterEndReason.Reset);
        Call(model, "Synchronize", pulse.Snapshot);
        var next = Start(pulse, 2UL);
        TestAssert.True((bool)Call(model, "Start", next, Vector2.Up), "new actual lifecycle may start");
        object before = Frame(model); ulong sequence = Value<ulong>(model, "LatestSequence");
        TestAssert.True(!(bool)Call(model, "End", staleEnd), "old end cannot retire newer burn");
        TestAssert.True(!(bool)Call(model, "RecordThrust", oldIdentity, Vector2.Right), "old force cannot authorize newer coast");
        TestAssert.True(!(bool)Call(model, "Start", new ThrusterPulseCommitResult(true, ThrusterRejectReason.None, oldIdentity), Vector2.Right), "old accepted event cannot replay after reset");
        TestAssert.Equal(before, Frame(model), "old observations preserve newer frame");
        TestAssert.Equal(sequence, Value<ulong>(model, "LatestSequence"), "old observations append nothing");
    }

    private static void JournalIsBoundedAndReportsItsFloor(Type type)
    {
        object model = New(type); var pulse = Pulse();
        for (ulong sample = 1; sample <= 70; sample++)
        {
            var commit = Start(pulse, sample); Call(model, "Start", commit, Vector2.Right);
            Call(model, "End", pulse.ApplyDynamics(commit.Identity, 0f, false));
        }
        object[] facts = Read(model, 0UL);
        TestAssert.Equal(64, facts.Length, "long absent consumer cannot grow unbounded journal");
        TestAssert.Equal(77UL, Value<ulong>(model, "FirstAvailableSequence"), "lagging consumer is told exactly what prefix expired");
        TestAssert.Equal(140UL, Value<ulong>(model, "LatestSequence"), "bounded retention preserves monotonic sequence");
        TestAssert.Equal(77UL, Fact<ulong>(facts[0], "Sequence"), "retained first fact matches reported floor");
        TestAssert.Equal(140UL, Fact<ulong>(facts[^1], "Sequence"), "retained last fact is actual latest");
    }
}
