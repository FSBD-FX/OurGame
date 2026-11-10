using System;
using System.Collections;
using System.Linq;
using Godot;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class ThrusterPresentationModelTests
{
    private readonly record struct AirPoseProbe(bool Active,bool Force,Vector2 Acceleration,
        float Lean,float Lag,float Near,float Far,float Trail);

    public static void RunDirectionalAirPose()
    {
        var assembly=typeof(PlayerMotor).Assembly;
        Type type=assembly.GetType("Knight.Presentation.PlayerAirJetPoseModel");
        Type inputType=assembly.GetType("Knight.Presentation.PlayerAirJetInput");
        TestAssert.True(type!=null && inputType!=null,
            "AIR_JET_MODEL_RED: actual air-direction/physics-stamped articulated model is missing");
        var step=type.GetMethod("Step");
        object New()=>Activator.CreateInstance(type);
        ThrusterPresentationFrame Source(Vector2 direction,ulong burn=1,ulong life=1,ulong epoch=1,
            bool coast=false,bool brake=false,float remaining=.45f,ulong owner=71) =>
            new(new(owner,life,epoch,burn),direction,!coast,coast,brake,coast?remaining:0);
        AirPoseProbe Sample(object model,ThrusterPresentationFrame source,Vector2 velocity,ulong tick,
            float delta=1f/60,int facing=1,bool grounded=false,bool suppressed=false,ulong owner=71,
            Vector2 root=default,float physicsStep=1f/60)
        {
            var motion=new PlayerGroundSample(true,root,0,Transform2D.Identity,Vector2.Zero,Vector2.Up)
                { PhysicsTick=tick,PhysicsStepSeconds=physicsStep };
            object input=Activator.CreateInstance(inputType,new object[]{owner,velocity,motion,facing,grounded,suppressed,source});
            object result=step.Invoke(model,new[]{input,(object)delta});
            T Get<T>(string name)=>(T)result.GetType().GetProperty(name).GetValue(result);
            return new(Get<bool>("Active"),Get<bool>("ForceObserved"),Get<Vector2>("NormalizedAccelerationWorld"),
                Get<float>("TorsoLeanRadians"),Get<float>("PelvisLag"),Get<float>("NearFold"),Get<float>("FarFold"),Get<float>("FootTrail"));
        }
        int checks=0;
        void Check(bool condition,string label){checks++;TestAssert.True(condition,"AIR_JET_MODEL_RED: "+label);}
        var probes=new System.Collections.Generic.List<AirPoseProbe>();
        foreach(Vector2 direction in new[]{Vector2.Up,Vector2.Down,Vector2.Left,Vector2.Right,
            new Vector2(-1,-1).Normalized(),new Vector2(1,-1).Normalized(),
            new Vector2(-1,1).Normalized(),new Vector2(1,1).Normalized()})
        {
            var model=New(); var source=Source(direction);int face=direction.X<0?-1:1;
            Check(!Sample(model,source,Vector2.Zero,1,delta:0,facing:face).Active,"zero-time first snapshot only primes, never poses");
            var pose=Sample(model,source,direction*75,2,facing:face);
            Check(pose.Active && pose.Force && pose.Acceleration.DistanceTo(direction)<.001f,
                "real stamped velocity difference supplies normalized world force");
            Check(float.IsFinite(pose.Lean) && Mathf.Abs(pose.Lean)<=.16f && Mathf.Abs(pose.Lag)<=.8f &&
                pose.Near is >=0 and <=11 && pose.Far is >=0 and <=10 && Mathf.Abs(pose.Trail)<=4,
                "all eight directions produce bounded finite rigid-joint targets");
            probes.Add(pose);
        }
        Check(probes[0].Near>probes[1].Near+1 && probes[0].Lag>0 && probes[1].Lag<0,
            "upward tuck/load is distinct from downward extension/opposite load");
        Check(Mathf.Abs(probes[0].Lean)<.001f && probes[3].Lean>.005f && probes[5].Lean>.005f,
            "vertical, horizontal and diagonal thrust do not share a fixed Dash lean");
        Check(Mathf.Abs(probes[2].Lean-probes[3].Lean)<.001f && Mathf.Abs(probes[2].Trail-probes[3].Trail)<.001f,
            "mirrored world thrust produces equivalent facing-local pose");
        var measured=New();var right=Source(Vector2.Right);
        Sample(measured,right,Vector2.Zero,1,delta:0);
        var forward=Sample(measured,right,new(75,0),2);
        var duplicate=Sample(measured,right,new(-99999,0),2);
        Check(duplicate.Acceleration==forward.Acceleration,"same physical tick cannot manufacture another force");
        Check(Sample(measured,right,new(-99999,0),3,delta:0)==duplicate,"zero elapsed cannot consume next physics sample or advance pose");
        var next=Sample(measured,right,new(150,0),3);
        Check(next.Acceleration.DistanceTo(Vector2.Right)<.001f,"positive next tick measures from last real sample, not zero-time payload");
        var flipped=Sample(measured,right,new(150,0),3,delta:0,facing:-1);
        Check(Mathf.Abs(next.Lean+flipped.Lean)<.001f && Mathf.Abs(next.Trail+flipped.Trail)<.001f &&
            next.Acceleration==flipped.Acceleration,"zero-time facing remaps local axes without reversing world load");
        var constant=New();Sample(constant,right,new(75,0),1,delta:0);
        var constantPose=Sample(constant,right,new(75,0),2);
        Check(constantPose.Force && constantPose.Acceleration==Vector2.Zero && forward.Lean>constantPose.Lean+.003f,
            "accepted direction alone is not a substitute for actual acceleration");
        foreach(var bad in new[]{default(ThrusterPresentationFrame),Source(Vector2.Zero),Source(new(2,0)),
            Source(new(float.NaN,0)),Source(Vector2.Right,owner:72),Source(Vector2.Right,burn:0),
            Source(Vector2.Right,life:0),Source(Vector2.Right) with { Coasting=true }})
        {
            var model=New();Sample(model,right,Vector2.Zero,1,delta:0);Sample(model,right,new(75,0),2);
            Check(!Sample(model,bad,new(100,0),3).Active,"invalid/default/foreign/contradictory source cannot advertise a real jet pose");
        }
        var retired=New();Sample(retired,right,Vector2.Zero,1,delta:0);Sample(retired,right,new(75,0),2);
        Check(!Sample(retired,default,new(100,0),3).Active && !Sample(retired,right,new(125,0),4).Active,
            "expired identity cannot replay merely because an old frame reappears");
        Check(Sample(retired,Source(Vector2.Up,burn:2),new(125,-75),5).Active,"a genuinely newer burn may be observed");
        var epoch=New();Sample(epoch,right,Vector2.Zero,1,delta:0);Sample(epoch,right,new(75,0),2);
        var fresh=Sample(epoch,Source(Vector2.Up,life:2,epoch:2),new(75,-75),3);
        Check(fresh.Active && !fresh.Force,"new lifecycle/epoch re-primes instead of creating fake velocity impulse");
        Check(Sample(epoch,Source(Vector2.Left,burn:99),new(-999,0),4)==fresh,
            "stale lifecycle cannot mutate newer pose/history");
        foreach(var invalidMotion in new[]{(Tick:0UL,Step:1f/60,Root:Vector2.Zero,Velocity:Vector2.Zero),
            (Tick:3UL,Step:0f,Root:Vector2.Zero,Velocity:Vector2.Zero),
            (Tick:3UL,Step:1f/60,Root:new Vector2(float.NaN,0),Velocity:Vector2.Zero),
            (Tick:3UL,Step:1f/60,Root:Vector2.Zero,Velocity:new Vector2(float.PositiveInfinity,0))})
        {
            var model=New();Sample(model,right,Vector2.Zero,1,delta:0);Sample(model,right,new(75,0),2);
            Check(!Sample(model,right,invalidMotion.Velocity,invalidMotion.Tick,root:invalidMotion.Root,physicsStep:invalidMotion.Step).Active,
                "unobserved/invalid physical sample cannot supply an air-force pose");
        }
        foreach(var gap in new[]{(Tick:1UL,Root:Vector2.Zero),(Tick:100UL,Root:Vector2.Zero),(Tick:3UL,Root:new Vector2(1000,0))})
        {
            var model=New();Sample(model,right,Vector2.Zero,1,delta:0);Sample(model,right,new(75,0),2);
            var reprime=Sample(model,right,new(999,999),gap.Tick,root:gap.Root);
            Check(reprime.Active && !reprime.Force && reprime.Acceleration==Vector2.Zero,"rollback/gap/teleport re-primes force rather than inventing it");
        }
        foreach(bool ground in new[]{false,true})
        {
            var model=New();Sample(model,right,Vector2.Zero,1,delta:0);Sample(model,right,new(75,0),2);
            Check(!Sample(model,right,new(75,0),3,grounded:ground,suppressed:!ground).Active,
                "ground/death/recovery clears flight posture immediately");
        }
        var brake=New();Sample(brake,right,new(150,0),1,delta:0);
        AirPoseProbe braking=default;
        for(ulong tick=2;tick<=5;tick++)braking=Sample(brake,Source(Vector2.Right,coast:true,brake:true),new(150-(tick-1)*20,0),tick);
        Check(braking.Active && braking.Lean<0,"actual reverse brake catches back while still moving forward");
        var duplicateBrake=Sample(brake,Source(Vector2.Right,coast:true,brake:true),new(-99999,0),5);
        Check(duplicateBrake.Lean<=braking.Lean && duplicateBrake.Acceleration==braking.Acceleration,
            "same-tick unmeasured velocity cannot reverse the cached braking target");
        var locked=New();Sample(locked,right,Vector2.Zero,1,delta:0);Sample(locked,right,new(75,0),2);
        Check(!Sample(locked,Source(Vector2.Up),new(75,-75),3).Active,
            "one accepted identity cannot silently change its locked thrust direction");
        foreach(bool suppress in new[]{false,true})
        {
            var ownerGap=New();Sample(ownerGap,right,Vector2.Zero,1,delta:0);Sample(ownerGap,right,new(75,0),2);
            Sample(ownerGap,suppress?right:default,new(75,0),3,suppressed:suppress);
            Sample(ownerGap,default,new(75,0),4,owner:0);
            Check(!Sample(ownerGap,right,new(100,0),5).Active,
                "invalid owner gap cannot erase expired/suppressed identity retirement");
        }
        foreach(var replay in new[]{(Burn:true,SameTick:true),(Burn:false,SameTick:true),
            (Burn:true,SameTick:false),(Burn:false,SameTick:false)})
        {
            var phase=New();var up=Source(Vector2.Up);
            Sample(phase,up,Vector2.Zero,1,delta:0);
            for(ulong tick=2;tick<=5;tick++)Sample(phase,up,new(0,-75),tick);
            AirPoseProbe settled=default;
            for(ulong tick=6;tick<=20;tick++)settled=Sample(phase,Source(Vector2.Up,coast:true,remaining:.045f),new(0,-75),tick);
            var stale=replay.Burn?up:Source(Vector2.Up,coast:true,remaining:.4f);
            var after=Sample(phase,stale,new(0,-75),replay.SameTick?20UL:21UL);
            Check(after.Active && after.Near<=settled.Near+.00001f && after.Far<=settled.Far+.00001f,
                "same/later physical tick cannot restore old burn or earlier coast energy within one identity");
        }
        var extreme=New();Sample(extreme,right,new(float.MaxValue,0),1,delta:0);
        var bounded=Sample(extreme,right,new(-float.MaxValue,float.MaxValue),2,delta:float.MaxValue,physicsStep:float.Epsilon);
        Check(bounded.Active && bounded.Acceleration.IsFinite() && float.IsFinite(bounded.Lean) &&
            Mathf.Abs(bounded.Lean)<=.16f && bounded.Near<=11 && bounded.Far<=10,"extreme finite arithmetic remains bounded before float publication");
        AirPoseProbe? reference=null;
        foreach(int hz in new[]{30,60,120})
        {
            var model=New();Sample(model,right,Vector2.Zero,1,delta:0,physicsStep:1f/hz);
            AirPoseProbe result=default;
            for(int i=1;i<=hz/5;i++)result=Sample(model,right,new(4500f*i/hz,0),(ulong)(i+1),delta:1f/hz,physicsStep:1f/hz);
            Check(result.Active && result.Lean>.05f && result.Near>1,"nonvacuous equal elapsed directional pose");
            if(reference is {} expected)Check(Mathf.Abs(result.Lean-expected.Lean)<.001f && Mathf.Abs(result.Near-expected.Near)<.002f,
                "equal elapsed30/60/120 samples agree, not hardware FPS qualification");
            reference ??=result;
        }
        GD.Print($"AIR_JET_MODEL_OK checks={checks} directions=8 real_force=true physics_dedup=true source_history=true elapsed_rates=true no_native_ownership=true human=false");
    }

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
        RunDirectionalAirPose();
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
