using Godot;
using Knight.Abilities;
using Knight.Config;
using Knight.Enemies;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class PlayerPresentationTests
{
    public static void RunLandingSupport()
    {
        PlayerPresentationInput Air(bool measured=true) => new(new(0,717.5f),false,true,false,false,1)
        {
            Landing=measured?new(1,0,0,false):default,
            Ground=new(true,new(0,-40),0,Transform2D.Identity,Vector2.Zero,Vector2.Up)
                { PhysicsTick=1,PhysicsStepSeconds=1f/60 }
        };
        PlayerPresentationInput Contact() => new(Vector2.Zero,true,true,false,false,1)
        {
            Landing=new(1,1,717.5f,true),
            Ground=new(true,Vector2.Zero,101,Transform2D.Identity,new(0,36),Vector2.Up)
                { HasBounds=true,SupportBounds=new(-1000,36,2000,20),PhysicsTick=2,PhysicsStepSeconds=1f/60 }
        };
        var timeline=new PlayerPresentationTimeline(new PlayerTuning());
        timeline.Step(Air(),1f/60);
        var landed=timeline.Step(Contact(),1f/60);
        TestAssert.True(landed.LandingLoad.Active && landed.Gait.Weight>=.999f &&
            landed.Gait.NearStance && landed.Gait.FarStance,
            "LANDING_SUPPORT_HANDOFF_RED: a measured real contact must establish both planted feet before compression");
        TestAssert.True(Mathf.Abs(landed.Gait.NearSoleWorld.Y-36)<.001f &&
            Mathf.Abs(landed.Gait.FarSoleWorld.Y-36)<.001f,"measured contact uses current actual support plane");
        var duplicate=timeline.Step(Contact() with { Ground=Contact().Ground with { RootWorld=new(400,0) } },1f/60);
        TestAssert.Equal(landed.Gait,duplicate.Gait,"same physics tick cannot manufacture a different planted foot");
        var leftGround=timeline.Step(Air() with { Landing=new(1,1,717.5f,true),
            Ground=Air().Ground with { PhysicsTick=3 } },1f/60);
        TestAssert.True(!leftGround.LandingLoad.Active && !leftGround.Gait.NearStance && !leftGround.Gait.FarStance,
            "leaving actual support immediately releases the landing envelope and planted stance");
        int rejected=0;
        foreach(var invalid in new[]{Contact().Ground with { SupportId=0 },Contact().Ground with { HasBounds=false },
            Contact().Ground with { GroundNormal=Vector2.Right },Contact().Ground with { RootWorld=new(float.NaN,0) }})
        {
            var bad=new PlayerPresentationTimeline(new PlayerTuning()); bad.Step(Air(),1f/60);
            var frame=bad.Step(Contact() with { Ground=invalid },1f/60);
            TestAssert.True(frame.Gait.Weight<.999f && !frame.Gait.NearStance && !frame.Gait.FarStance,
                "measured impact cannot override invalid or unsupported geometry");
            rejected++;
        }
        var legacy=new PlayerPresentationTimeline(new PlayerTuning());
        legacy.Step(Air(false) with { Dashing=false },1f/60);
        var legacyLand=legacy.Step(Contact() with { Dashing=false,Landing=default },1f/60);
        TestAssert.True(legacyLand.Gait.Weight>0 && legacyLand.Gait.Weight<.999f,
            "ordinary unmeasured reentry retains its original support blend");
        GD.Print($"LANDING_SUPPORT_MODEL_OK current_plane=true same_tick=true invalid_rejections={rejected} legacy_blend=true human=false");
    }

    public static void RunLandingLoad()
    {
        var assembly=typeof(PlayerPresentationTimeline).Assembly;
        var modelType=assembly.GetType("Knight.Presentation.PlayerLandingLoadModel");
        var observationType=assembly.GetType("Knight.Presentation.PlayerLandingObservation");
        TestAssert.True(modelType!=null && observationType!=null,
            "LANDING_LOAD_MODEL_RED: measured landing needs an independent one-shot value model");
        var step=modelType.GetMethod("Step");
        TestAssert.True(step!=null,"LANDING_LOAD_MODEL_RED: missing Step contract");
        object New() => System.Activator.CreateInstance(modelType);
        (bool Active,float Compression,float Strength) Sample(object model,ulong owner,ulong count,float speed,
            bool reliable=true,bool grounded=true,bool suppressed=false,float delta=.05f)
        {
            object observation=System.Activator.CreateInstance(observationType,new object[]{owner,count,speed,reliable});
            object result=step.Invoke(model,new[]{observation,(object)grounded,suppressed,delta});
            T Read<T>(string name) => (T)result.GetType().GetProperty(name).GetValue(result);
            return (Read<bool>("Active"),Read<float>("Compression"),Read<float>("Strength"));
        }
        int checks=0;
        void Check(bool condition,string label) { checks++; TestAssert.True(condition,"LANDING_LOAD_MODEL_RED: "+label); }
        var model=New();
        Check(!Sample(model,1,0,0).Active,"initial standing is not an impact");
        var hit=Sample(model,1,1,650);
        Check(hit.Active && hit.Compression>.1f && hit.Compression<=4 && hit.Strength>.99f,
            "a new real count creates finite supported absorption");
        Check(Sample(model,1,2,650,delta:0)==hit,"zero time cannot consume a new count or advance recovery");
        var next=Sample(model,1,2,650);
        Check(next==hit,"a zero-time deferred count is consumed exactly on its first positive step");
        for(int i=0;i<8;i++) Sample(model,1,2,650);
        Check(!Sample(model,1,2,650).Active,"same historical count cannot replay after settling");
        var light=New(); Sample(light,1,0,0);
        var lightHit=Sample(light,1,1,325);
        Check(lightHit.Active && lightHit.Compression>0 && lightHit.Compression<hit.Compression &&
            lightHit.Strength<hit.Strength,"lower measured normal speed produces less absorption");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f})
        {
            var bad=New(); Sample(bad,1,0,0);
            Check(!Sample(bad,1,1,invalid).Active,"invalid measurement never becomes load");
            Check(!Sample(bad,1,1,650).Active,"rejected count is quarantined, not revived by changing its payload");
        }
        foreach(var rejection in new[]{(Reliable:false,Grounded:true,Suppressed:false),
            (Reliable:true,Grounded:false,Suppressed:false),(Reliable:true,Grounded:true,Suppressed:true)})
        {
            var rejected=New(); Sample(rejected,1,0,0);
            Check(!Sample(rejected,1,1,650,rejection.Reliable,rejection.Grounded,rejection.Suppressed).Active,
                "unreliable/air/recovery cannot emit physical support load");
            Check(!Sample(rejected,1,1,650).Active,"returning to eligible state cannot replay a suppressed count");
        }
        foreach(var clear in new[]{(Owner:2UL,Count:8UL,Grounded:true,Suppressed:false),
            (Owner:1UL,Count:0UL,Grounded:true,Suppressed:false),
            (Owner:1UL,Count:1UL,Grounded:false,Suppressed:false),
            (Owner:1UL,Count:1UL,Grounded:true,Suppressed:true),
            (Owner:0UL,Count:1UL,Grounded:true,Suppressed:false)})
        {
            var cleared=New(); Sample(cleared,1,0,0); Sample(cleared,1,1,650);
            Check(!Sample(cleared,clear.Owner,clear.Count,650,grounded:clear.Grounded,suppressed:clear.Suppressed).Active,
                "owner/counter/air/death reset clears retained compression");
            Check(!Sample(cleared,clear.Owner,clear.Count,650).Active,"clear cannot defer a historical impact");
        }
        var history=New();
        Check(!Sample(history,1,77,650).Active,"first binding primes current history without replay");
        var extreme=New(); Sample(extreme,1,0,0);
        var bounded=Sample(extreme,1,ulong.MaxValue,float.MaxValue);
        Check(bounded.Active && float.IsFinite(bounded.Compression) && bounded.Compression<=4 && bounded.Strength==1,
            "finite extreme speed/count stays bounded without overflow");
        Check(!Sample(extreme,1,0,650).Active,"counter rollover is a new baseline, not a fake event");
        float? rateReference=null;
        foreach(int hz in new[]{30,60,120})
        {
            var rate=New(); Sample(rate,1,0,0);
            (bool Active,float Compression,float Strength) result=default;
            for(int i=0;i<hz/10;i++) result=Sample(rate,1,1,650,delta:1f/hz);
            Check(result.Active && result.Compression>1,"elapsed-rate sample has a real nonzero load");
            if(rateReference.HasValue) Check(System.Math.Abs(result.Compression-rateReference.Value)<.001f,
                "same elapsed recovery agrees at30/60/120 samples, not hardware FPS proof");
            rateReference ??=result.Compression;
        }
        GD.Print($"LANDING_LOAD_MODEL_OK checks={checks} one_shot=true real_count=true elapsed_rates=true no_native_ownership=true human=false");
    }

    public static void RunBodyLoad()
    {
        var faults=new System.Collections.Generic.List<string>();
        int checks=0;
        void Check(bool condition,string name) { checks++; if(!condition) faults.Add(name); }
        PlayerPresentationInput Sample(float velocity,ulong tick,float step=1f/60,int facing=1) =>
            new(new Vector2(velocity,0),true,false,false,false,facing)
            { Ground=new(true,new Vector2((float)tick,0),101,Transform2D.Identity,new Vector2(0,40),Vector2.Up)
                { PhysicsTick=tick,PhysicsStepSeconds=step } };
        var model=new PlayerPresentationTimeline(new PlayerTuning());
        model.Step(Sample(0,1),1f/60);
        var accelerating=model.Step(Sample(36.666668f,2),1f/60);
        Check(accelerating.BodyLoad.Observed && accelerating.BodyLoad.TorsoLeanRadians>.005f &&
            accelerating.BodyLoad.PelvisDrop>.05f,"actual acceleration produces bounded forward load and pelvis drop");
        var duplicate=model.Step(Sample(-180,2),1f/60);
        Check(duplicate.BodyLoad==accelerating.BodyLoad,"same physics tick cannot manufacture reversed force");
        var zero=model.Step(Sample(-180,3),0);
        Check(zero.BodyLoad==accelerating.BodyLoad,"zero-time event cannot resample body force");
        model.Step(Sample(180,3),1f/60);
        PlayerPresentationFrame slowing=default;
        for(ulong i=4;i<=9;i++) slowing=model.Step(Sample(180-(float)(i-3)*20,i),1f/60);
        Check(slowing.BodyLoad.Observed && slowing.BodyLoad.TorsoLeanRadians<-.005f && slowing.BodyLoad.PelvisDrop>.1f,
            "passive velocity loss produces backward catch while still moving");
        Check(slowing.Locomotion.State is PlayerLocomotionState.StartRun or PlayerLocomotionState.Run,
            "load describes deceleration independently of coarse run template");
        var left=new PlayerPresentationTimeline(new PlayerTuning());
        left.Step(Sample(0,1,facing:-1),1f/60);
        var leftLoad=left.Step(Sample(-36.666668f,2,facing:-1),1f/60).BodyLoad;
        Check(leftLoad.Observed && System.Math.Abs(leftLoad.TorsoLeanRadians-accelerating.BodyLoad.TorsoLeanRadians)<.0001f &&
            System.Math.Abs(leftLoad.PelvisDrop-accelerating.BodyLoad.PelvisDrop)<.0001f,"facing-local mirrored acceleration is equivalent");
        foreach(var ineligible in new[]{Sample(60,10) with { Grounded=false },Sample(60,10) with { Dashing=true },
            Sample(60,10) with { Dead=true },Sample(60,10) with { CheckpointRecovering=true }})
        {
            var isolated=new PlayerPresentationTimeline(new PlayerTuning());
            isolated.Step(Sample(0,1),1f/60); isolated.Step(Sample(40,2),1f/60);
            Check(!isolated.Step(ineligible,1f/60).BodyLoad.Observed,"air/dash/death/recovery releases measured body load");
        }
        Check(!model.Step(Sample(0,11) with { Ground=default },1f/60).BodyLoad.Observed,"unobserved legacy input does not claim measured force");
        var gap=new PlayerPresentationTimeline(new PlayerTuning());
        gap.Step(Sample(0,1),1f/60); gap.Step(Sample(40,2),1f/60);
        var reset=gap.Step(Sample(260,100),1f/60).BodyLoad;
        Check(reset.Observed && System.Math.Abs(reset.TorsoLeanRadians)<.0001f && reset.PelvisDrop<.0001f,"long sample gap primes without fake acceleration");
        Check(!gap.Step(Sample(float.NaN,101),1f/60).BodyLoad.Observed,"invalid raw velocity is not a real braking observation");
        PlayerBodyLoadFrame? rateReference=null;
        foreach(int hz in new[]{30,60,120})
        {
            var rate=new PlayerPresentationTimeline(new PlayerTuning());
            rate.Step(Sample(0,1,1f/hz),1f/hz);
            PlayerBodyLoadFrame result=default;
            for(int i=1;i<=hz/5;i++) result=rate.Step(Sample(1000f*i/hz,(ulong)(i+1),1f/hz),1f/hz).BodyLoad;
            Check(result.Observed && result.TorsoLeanRadians>.01f && result.PelvisDrop>.2f,"nonvacuous rate force response");
            if(rateReference is {} reference) Check(System.Math.Abs(result.TorsoLeanRadians-reference.TorsoLeanRadians)<.001f &&
                System.Math.Abs(result.PelvisDrop-reference.PelvisDrop)<.025f,"30/60/120 same elapsed load agrees");
            rateReference ??=result;
        }
        var clock=new PlayerPresentationTimeline(new PlayerTuning());
        clock.Step(Sample(0,1),.1f);
        var second=clock.Step(Sample(0,2),.1f);
        var repeated=clock.Step(Sample(0,2),0);
        Check(second.PresentationTimeSeconds is double t && System.Math.Abs(t-.2)<.000001 &&
            repeated.PresentationTimeSeconds==second.PresentationTimeSeconds,"production clock advances elapsed time, not event count");
        TestAssert.True(faults.Count==0,"BODY_LOAD_MODEL_RED: "+string.Join("; ",faults));
        GD.Print($"BODY_LOAD_MODEL_OK checks={checks} actual_velocity_difference=true physics_stamped=true no_motor_changes=true human=false");
    }

    public static void RunGroundGait()
    {
        var faults=new System.Collections.Generic.List<string>();
        int checks=0, planted=0, lifted=0;
        void Check(bool actual,string name) { checks++; if(!actual) faults.Add(name); }
        PlayerGroundSample Ground(float x,Transform2D? support=null) => new(true,new(x,0),101,
            support??Transform2D.Identity,(support??Transform2D.Identity)*new Vector2(0,36),
            (support??Transform2D.Identity).BasisXform(Vector2.Up).Normalized())
            { HasBounds=true,SupportBounds=new(-10000,36,20000,20) };
        var gait=new PlayerGroundGait();
        var first=gait.Step(Ground(0),true,1f/60);
        Check(first.Observed && first.Weight==1 && first.NearStance && first.FarStance,"observed standing support");
        Check(first.NearSoleWorld.DistanceTo(new(8,36))<.001f && first.FarSoleWorld.DistanceTo(new(-8,36))<.001f,
            "neutral sole points lie on literal ground");
        var previous=first;
        for(int i=0;i<60;i++) previous=gait.Step(Ground(0),true,1f/60);
        Check(previous.Phase==first.Phase && previous.NearSoleWorld==first.NearSoleWorld,"zero displacement does not walk");
        float worstPlant=0, worstStep=0;
        for(int i=1;i<=180;i++)
        {
            var current=gait.Step(Ground(i),true,1f/60);
            if(previous.NearStance && current.NearStance)
            { worstPlant=Mathf.Max(worstPlant,current.NearSoleWorld.DistanceTo(previous.NearSoleWorld)); planted++; }
            if(previous.FarStance && current.FarStance)
            { worstPlant=Mathf.Max(worstPlant,current.FarSoleWorld.DistanceTo(previous.FarSoleWorld)); planted++; }
            if(current.NearSoleWorld.Y<35 || current.FarSoleWorld.Y<35) lifted++;
            worstStep=Mathf.Max(worstStep,Mathf.Max(current.NearSoleWorld.DistanceTo(previous.NearSoleWorld),
                current.FarSoleWorld.DistanceTo(previous.FarSoleWorld)));
            previous=current;
        }
        Check(planted>100 && lifted>20 && worstPlant<.002f,"real stance is stable and swing leaves ground");
        Check(worstStep<10,"walking soles do not jump between frames");
        var beforeZero=previous;
        var zero=gait.Step(Ground(1000),true,0);
        Check(zero==beforeZero,"zero delta does not consume a teleport observation");
        var teleported=gait.Step(Ground(1000),true,1f/60);
        Check(teleported.Observed && teleported.NearSoleWorld.X>950 && teleported.FarSoleWorld.X>950,"teleport releases old terrain contacts");
        var moved=new PlayerGroundGait();
        moved.Step(Ground(0),true,1f/60);
        var carried=moved.Step(Ground(10,new Transform2D(0,new Vector2(10,0))),true,1f/60);
        Check(carried.NearSoleWorld.DistanceTo(new(18,36))<.002f && carried.Phase==0,"platform motion carries feet but is not walking");
        var rotated=new PlayerGroundGait();
        rotated.Step(Ground(0),true,1f/60);
        var rotation=new Transform2D(.1f,Vector2.Zero);
        var turned=rotated.Step(Ground(0,rotation),true,1f/60);
        Check(turned.NearSoleWorld.DistanceTo(rotation*new Vector2(8,36))<.002f && turned.Phase==0,"rotating support keeps local planted point");
        var air=moved.Step(Ground(10),false,.1f);
        Check(!air.NearStance && !air.FarStance && air.Weight==0,"airborne or disabled gait has no planted feet");
        var invalid=moved.Step(Ground(float.NaN),true,1f/60);
        Check(!invalid.NearStance && !invalid.FarStance && invalid.NearSoleWorld.IsFinite() && invalid.FarSoleWorld.IsFinite(),
            "invalid observation cannot publish fake support or NaN");
        PlayerGroundGaitFrame Finish(int hz)
        {
            var model=new PlayerGroundGait(); model.Step(Ground(0),true,1f/hz);
            PlayerGroundGaitFrame result=default;
            for(int i=1;i<=hz*2;i++) result=model.Step(Ground(i*60f/hz),true,1f/hz);
            return result;
        }
        var slow=Finish(30); var medium=Finish(60); var fast=Finish(120);
        Check(slow.NearSoleWorld.DistanceTo(medium.NearSoleWorld)<.02f && medium.NearSoleWorld.DistanceTo(fast.NearSoleWorld)<.02f &&
            slow.FarSoleWorld.DistanceTo(fast.FarSoleWorld)<.02f && Mathf.Abs(slow.Phase-fast.Phase)<.001f,"30 60 120 Hz displacement agreement");
        var stamped=new PlayerGroundGait();
        stamped.Step(Ground(0) with { PhysicsTick=1,PhysicsStepSeconds=1f/60 },true,1f/120);
        var physicsStep=stamped.Step(Ground(14) with { PhysicsTick=2,PhysicsStepSeconds=1f/60 },true,1f/120);
        var renderOnly=stamped.Step(Ground(14) with { PhysicsTick=2,PhysicsStepSeconds=1f/60 },true,1f/120);
        Check(physicsStep==renderOnly,"repeated render observation cannot settle a moving physics foot");
        var left=new PlayerGroundGait().Step(Ground(0) with { Facing=-1 },true,1f/60);
        Check(left.NearSoleWorld.DistanceTo(new(-8,36))<.001f && left.FarSoleWorld.DistanceTo(new(8,36))<.001f,
            "fresh left-facing stance mirrors its neutral foot offsets");
        var bounded=new PlayerGroundGait().Step(Ground(0) with { SupportBounds=new(-6,36,12,10) },true,1f/60);
        Check(bounded.NearSoleWorld.DistanceTo(new(4,36))<.001f && bounded.FarSoleWorld.DistanceTo(new(-4,36))<.001f,
            "finite support does not invent ground beyond its edge");
        var unknown=new PlayerGroundGait().Step(Ground(0) with { HasBounds=false },true,1f/60);
        Check(!unknown.NearStance && !unknown.FarStance && unknown.Weight==0,"unknown terrain extent cannot be planted");
        var singular=new PlayerGroundGait().Step(Ground(0) with { SupportTransform=default },true,1f/60);
        Check(singular.NearSoleWorld.IsFinite() && !singular.NearStance && !singular.FarStance,"singular support fails closed");
        var reverseModel=new PlayerGroundGait(); reverseModel.Step(Ground(0),true,1f/60);
        PlayerGroundGaitFrame reversePrevious=default;
        float reverseJump=0;
        for(int i=1;i<=160;i++)
        {
            float x=i<=80?i:160-i;
            var result=reverseModel.Step(Ground(x),true,1f/60);
            if(i>1) reverseJump=Mathf.Max(reverseJump,Mathf.Max(result.NearSoleWorld.DistanceTo(reversePrevious.NearSoleWorld),
                result.FarSoleWorld.DistanceTo(reversePrevious.FarSoleWorld)));
            reversePrevious=result;
        }
        Check(reverseJump<10,"inertial reversal starts from the current foot rather than teleporting");
        float stoppedPhase=reversePrevious.Phase;
        for(int i=0;i<20;i++) reversePrevious=reverseModel.Step(Ground(0),true,1f/60);
        Check(reversePrevious.Phase==stoppedPhase && reversePrevious.NearStance && reversePrevious.FarStance &&
            Mathf.Abs(reversePrevious.NearSoleWorld.Y-36)<.001f && Mathf.Abs(reversePrevious.FarSoleWorld.Y-36)<.001f,
            "stopping settles the raised foot without advancing walking phase");
        foreach (int facing in new[]{1,-1})
        foreach (bool resume in new[]{false,true})
        {
            var retarget = new PlayerGroundGait();
            PlayerGroundSample At(float x) => Ground(x*facing) with { Facing=facing };
            var plantedFoot = retarget.Step(At(0),true,1f/60).NearSoleWorld;
            var raised = retarget.Step(At(18),true,1f/60);
            TestAssert.Near(28,raised.FarSoleWorld.Y,.002f,"fixture reaches the original 8px swing apex");
            if(resume) raised = retarget.Step(At(18),true,1f/60);
            float target = resume ? 22 : 10;
            var caught = retarget.Step(At(target),true,1f/60);
            TestAssert.True(caught.FarSoleWorld.Y>=27.998f && caught.FarSoleWorld.Y<36 &&
                !caught.FarStance && caught.NearStance && caught.NearSoleWorld.DistanceTo(plantedFoot)<.002f,
                $"GROUND_SWING_RETARGET_RED: facing={facing} resume={resume} old_y={raised.FarSoleWorld.Y} new_y={caught.FarSoleWorld.Y}");
            for(int i=0;i<12;i++) caught=retarget.Step(At(target),true,1f/60);
            TestAssert.True(caught.FarStance && caught.NearStance && Mathf.Abs(caught.FarSoleWorld.Y-36)<.002f,
                "retargeted swing still settles on the real support plane");
        }
        GD.Print("GROUND_SWING_RETARGET_OK mirrored=2 reversal=true resume=true planted_other=true settles=true");
        TestAssert.True(faults.Count==0,"GROUND_GAIT_MODEL_RED: "+string.Join("; ",faults));
        GD.Print($"GROUND_GAIT_MODEL_OK checks={checks} planted={planted} lifted={lifted} actual_displacement=true motor_changed=false pixels=false");
    }

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
        OrdinaryAirMotionTests.Run();
        ThrusterPresentationModelTests.RunDirectionalAirPose();
        RunLandingLoad();
        RunLandingSupport();
        RunGroundGait();
        PlayerStateMappingIsGameplayDriven();
        AuthoritativeTimelineCoversEveryLocomotionState();
        ZeroTimeRefreshPreservesSecondaryLandingImpulse();
        ZeroTimeRefreshPreservesSecondaryAcceleration();
        ZeroTimeRefreshPreservesSecondaryTurn();
        ZeroTimeTurnRoundTripPreservesBasisWithoutInventingTurn();
        AttackTimelinePreservesPreparationContactAndCancellableRecovery();
        GroundAttackDrivesContinuousSecondaryMotion();
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

    private static void ZeroTimeRefreshPreservesSecondaryAcceleration()
    {
        using var tuning = new PlayerTuning();
        foreach (float refreshDelta in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            var uninterrupted = new PlayerPresentationTimeline(tuning);
            var refreshed = new PlayerPresentationTimeline(tuning);
            var rest = Input(Vector2.Zero, grounded: true) with { Facing = 1 };
            uninterrupted.Step(rest, .02f);
            var before = refreshed.Step(rest, .02f);
            var accelerated = rest with { Velocity = new Vector2(90f, 0f) };
            var instant = refreshed.Step(accelerated, refreshDelta);
            TestAssert.Equal(before.Secondary, instant.Secondary,
                "a non-advancing refresh cannot integrate secondary motion");
            var expected = uninterrupted.Step(accelerated, .02f);
            var actual = refreshed.Step(accelerated, .02f);
            TestAssert.True(expected.Secondary.Lantern.Position.X < -.01f &&
                actual.Secondary.Lantern.Position.X < -.01f,
                "SECONDARY_REFRESH_ACCELERATION_RED: zero-time refresh must not consume the next physical acceleration drive");
            TestAssert.Equal(expected.Secondary, actual.Secondary,
                "the next positive step retains all four acceleration-driven secondary channels");
        }
    }

    private static void ZeroTimeRefreshPreservesSecondaryTurn()
    {
        using var tuning = new PlayerTuning();
        int cases = 0;
        foreach (int facing in new[] { 1, -1 })
        foreach (float refreshDelta in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            var uninterrupted = new PlayerPresentationTimeline(tuning);
            var refreshed = new PlayerPresentationTimeline(tuning);
            var rest = Input(Vector2.Zero, grounded: true) with { Facing = facing };
            uninterrupted.Step(rest, .02f);
            var before = refreshed.Step(rest, .02f);
            var turned = rest with { Facing = -facing };
            var instant = refreshed.Step(turned, refreshDelta);
            TestAssert.Equal(before.Secondary, instant.Secondary,
                "a quiet turn refresh changes basis but cannot integrate new spring motion");
            // Repeated publication of that same facing must not reflect twice
            // or consume the deferred source before integrating time.
            refreshed.Step(turned, 0f);
            var expected = uninterrupted.Step(turned, .02f);
            var actual = refreshed.Step(turned, .02f);
            GD.Print($"SECONDARY_TURN_REFRESH_OBS start_facing={facing} refresh_delta={refreshDelta} expected_mantle_y={expected.Secondary.Mantle.Position.Y} actual_mantle_y={actual.Secondary.Mantle.Position.Y}");
            TestAssert.True(actual.Secondary.Mantle.Position.Y > .01f,
                "SECONDARY_REFRESH_TURN_RED: a non-advancing facing refresh must not consume the next turn drive");
            TestAssert.Near(.028783012f, expected.Secondary.Mantle.Position.Y, .000001f,
                "turn response matches the hand-derived 2.5*(1-1.16*exp(-.16)) spring step");
            TestAssert.Equal(expected.Secondary, actual.Secondary,
                "turn refresh preserves all four channels at the next positive step");
            for (int tick = 0; tick < 30; tick++)
            {
                var expectedCoast = uninterrupted.Step(turned, .02f);
                var actualCoast = refreshed.Step(turned, .02f);
                if (tick == 0)
                    TestAssert.Near(.074925166f, actualCoast.Secondary.Mantle.Position.Y, .000001f,
                        "one .02s turn pulse follows step-response(.04)-step-response(.02), not a repeated drive");
                TestAssert.Equal(expectedCoast.Secondary, actualCoast.Secondary,
                    "one deferred turn is consumed once, without a replay in its inertial tail");
            }
            cases++;
        }
        GD.Print($"SECONDARY_TURN_REFRESH_MODEL_OK cases={cases} repeated_refresh=true one_shot=true gameplay_changed=false human=false");
    }

    private static void ZeroTimeTurnRoundTripPreservesBasisWithoutInventingTurn()
    {
        using var tuning = new PlayerTuning();
        foreach (int facing in new[] { 1, -1 })
        {
            var reference = new PlayerPresentationTimeline(tuning);
            var refreshed = new PlayerPresentationTimeline(tuning);
            var input = Input(new(180f, -40f), grounded: false) with { Facing = facing };
            PlayerPresentationFrame before = default;
            for (int tick = 0; tick < 6; tick++)
            {
                reference.Step(input, .02f);
                before = refreshed.Step(input, .02f);
            }
            var flipped = refreshed.Step(input with { Facing = -facing }, 0f);
            TestAssert.Near(-before.Secondary.Mantle.Position.X, flipped.Secondary.Mantle.Position.X, .000001f,
                "the parent basis must still reflect existing motion immediately, without elapsed time");
            TestAssert.Near(before.Secondary.Mantle.Position.Y, flipped.Secondary.Mantle.Position.Y, .000001f,
                "an immediate basis change leaves vertical motion unchanged");
            TestAssert.Equal(before.Secondary, refreshed.Step(input, 0f).Secondary,
                "a zero-time facing round trip restores the original basis exactly");
            for (int tick = 0; tick < 30; tick++)
                TestAssert.Equal(reference.Step(input, .02f).Secondary, refreshed.Step(input, .02f).Secondary,
                    "returning to the sampled facing before time advances cannot invent a turn impulse");
        }
        GD.Print("SECONDARY_TURN_ROUNDTRIP_MODEL_OK cases=2 immediate_basis=true no_invented_turn=true human=false");
    }

    private static void ZeroTimeRefreshPreservesSecondaryLandingImpulse()
    {
        using var tuning = new PlayerTuning();
        var uninterrupted = new PlayerPresentationTimeline(tuning);
        var refreshed = new PlayerPresentationTimeline(tuning);
        // A reliable landing record is authoritative even when the visual sample
        // immediately before contact has no downward speed of its own.
        var airborne = Input(Vector2.Zero, grounded: false) with
        { Facing = 1, Landing = new(1, 0, 0, false) };
        uninterrupted.Step(airborne, .02f);
        var before = refreshed.Step(airborne, .02f);
        var contact = airborne with { Grounded = true, Landing = new(1, 1, 650f, true) };
        TestAssert.Equal(before.Secondary, refreshed.Step(contact, 0f).Secondary,
            "instant contact publication does not advance the secondary springs");
        var expected = uninterrupted.Step(contact, .02f);
        var actual = refreshed.Step(contact, .02f);
        TestAssert.True(expected.LandingLoad.Active && actual.LandingLoad.Active &&
            expected.Secondary.Lantern.Position.Y > .01f && actual.Secondary.Lantern.Position.Y > .01f,
            "SECONDARY_REFRESH_LANDING_RED: zero-time contact must not consume the landing impulse before positive time");
        TestAssert.Equal(expected.Secondary, actual.Secondary,
            "a deferred landing retains the same secondary response as uninterrupted positive time");
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

    private static void GroundAttackDrivesContinuousSecondaryMotion()
    {
        // Missing the authoritative GroundAttack source must fail even with no
        // movement, hit confirmation, thread, landing or facing-turn impulse.
        const float step = 1f / 120f;
        var right = new PlayerPresentationTimeline(new PlayerTuning());
        var left = new PlayerPresentationTimeline(new PlayerTuning());
        var hit = new PlayerPresentationTimeline(new PlayerTuning());
        var idle = new PlayerPresentationTimeline(new PlayerTuning());
        var air = new PlayerPresentationTimeline(new PlayerTuning());
        var still = Input(Vector2.Zero, grounded: true);
        right.Step(still, step);
        left.Step(still with { Facing = -1 }, step);
        hit.Step(still, step);
        var attack = still with { AttackId = 243, IsAttacking = true };
        var seen = new System.Collections.Generic.HashSet<PlayerActionPhase>();
        var moving = new System.Collections.Generic.HashSet<PlayerActionPhase>();
        PlayerPresentationFrame previous = default;
        float peak = 0f;
        bool extraContact = false;
        for (int index = 0; index < 48; index++)
        {
            var frame = right.Step(attack, step);
            var mirrored = left.Step(attack with { Facing = -1 }, step);
            var connected = hit.Step(attack with { AttackConnected = index == 15 }, step);
            var quiet = idle.Step(still, step);
            var airborne = air.Step(attack with { Grounded = false }, step);
            seen.Add(frame.Action.Phase);
            float lantern = frame.Secondary.Lantern.Position.Length();
            float tether = frame.Secondary.Tether.Position.Length();
            if (lantern > .001f && tether > .001f) moving.Add(frame.Action.Phase);
            peak = Mathf.Max(peak, lantern);
            TestAssert.True(lantern < 15f && tether < 13f,
                "GROUND_ATTACK_SECONDARY_SOURCE_RED: stationary attack secondary motion stays bounded");
            TestAssert.True(frame.Secondary.Lantern.Position.DistanceTo(mirrored.Secondary.Lantern.Position) < .001f &&
                frame.Secondary.Tether.Position.DistanceTo(mirrored.Secondary.Tether.Position) < .001f &&
                Mathf.Abs(frame.Secondary.Lantern.RotationRadians - mirrored.Secondary.Lantern.RotationRadians) < .001f,
                "GROUND_ATTACK_SECONDARY_SOURCE_RED: opposite world aims agree in each reflected facing-parent local basis");
            TestAssert.Equal(PoseTransform2D.Identity, frame.Secondary.ShoulderSpool,
                "a sword attack does not wind the mechanical shoulder spool");
            TestAssert.Equal(PoseTransform2D.Identity, quiet.Secondary.Lantern,
                "idle has no ground attack source");
            TestAssert.Equal(PoseTransform2D.Identity, airborne.Secondary.Lantern,
                "an air attack cannot borrow the ground attack source");
            if (index > 0)
                TestAssert.True(frame.Secondary.Lantern.Position.DistanceTo(previous.Secondary.Lantern.Position) < 2f &&
                    frame.Secondary.Tether.Position.DistanceTo(previous.Secondary.Tether.Position) < 2f,
                    "GROUND_ATTACK_SECONDARY_SOURCE_RED: phase boundaries retain continuous inertial motion");
            if (index > 15 && Mathf.Abs(connected.Secondary.Lantern.Position.Y -
                frame.Secondary.Lantern.Position.Y) > .001f) extraContact = true;
            previous = frame;
        }
        foreach (var phase in new[] { PlayerActionPhase.Anticipation, PlayerActionPhase.Burst,
            PlayerActionPhase.Contact, PlayerActionPhase.Recovery })
        {
            TestAssert.True(seen.Contains(phase), $"ground attack actually visits {phase}");
            TestAssert.True(moving.Contains(phase),
                $"GROUND_ATTACK_SECONDARY_SOURCE_RED: stationary whiff drives lantern and tether during {phase}");
        }
        TestAssert.True(peak > .05f && extraContact,
            "GROUND_ATTACK_SECONDARY_SOURCE_RED: visible whiff inertia retains additional real-hit response");

        // Natural completion, declared recovery cancellation, and a higher-priority
        // checkpoint override all remove the drive, without resetting the springs.
        foreach (int exit in new[] { 0, 1, 2 })
        {
            var timeline = new PlayerPresentationTimeline(new PlayerTuning());
            timeline.Step(still, step);
            StepUntil(timeline, attack, PlayerActionPhase.Recovery);
            var released = attack with { IsAttacking = false,
                MovementCancelRequested = exit == 1, CheckpointRecovering = exit == 2 };
            PlayerPresentationFrame settled = default;
            for (int index = 0; index < 360; index++) settled = timeline.Step(released, step);
            TestAssert.Equal(PlayerActionFrame.None, settled.Action,
                $"ground attack exit {exit} relinquishes action authority");
            TestAssert.True(settled.Secondary.Lantern.Position.Length() < .001f &&
                settled.Secondary.Tether.Position.Length() < .001f &&
                Mathf.Abs(settled.Secondary.Lantern.RotationRadians) < .001f &&
                Mathf.Abs(settled.Secondary.Tether.RotationRadians) < .001f,
                $"GROUND_ATTACK_SECONDARY_SOURCE_RED: exit {exit} settles to rest within three seconds");
        }
        GD.Print("GROUND_ATTACK_SECONDARY_SOURCE_OK phases=4 facing_local=true continuous=true hit_extra=true exits=3 human=false");
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
