using System;
using Godot;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class OrdinaryAirMotionTests
{
    private readonly record struct Probe(bool Active,bool Started,bool Arc,float Entry,
        float Lean,float Pelvis,float Near,float Far,float Trail);

    public static void Run()
    {
        var assembly=typeof(PlayerMotor).Assembly;
        Type modelType=assembly.GetType("Knight.Presentation.PlayerOrdinaryAirMotion");
        Type inputType=assembly.GetType("Knight.Presentation.PlayerOrdinaryAirInput");
        Type jumpType=assembly.GetType("Knight.Presentation.PlayerJumpObservation");
        TestAssert.True(modelType!=null && inputType!=null && jumpType!=null,
            "ORDINARY_AIR_MODEL_RED: real jump-cause/stamped ordinary-air model missing");
        object New()=>Activator.CreateInstance(modelType);
        object Jump(ulong sequence,ulong owner=71,bool observed=true)=>Activator.CreateInstance(jumpType,new object[]{owner,sequence,observed});
        Probe Sample(object model,ulong sequence,Vector2 velocity,ulong tick,float delta=1f/60,
            bool grounded=false,bool suppressed=false,bool replaced=false,int facing=1,ulong owner=71,
            Vector2 root=default,float physicsStep=1f/60,bool observed=true)
        {
            var motion=new PlayerGroundSample(true,root,0,Transform2D.Identity,Vector2.Zero,Vector2.Up)
                {PhysicsTick=tick,PhysicsStepSeconds=physicsStep};
            object input=Activator.CreateInstance(inputType,new object[]{Jump(sequence,owner,observed),motion,velocity,facing,grounded,suppressed,replaced});
            object result=modelType.GetMethod("Step").Invoke(model,new[]{input,(object)delta});
            T Get<T>(string name)=>(T)result.GetType().GetProperty(name).GetValue(result);
            return new(Get<bool>("Active"),Get<bool>("JumpStarted"),Get<bool>("JumpArc"),Get<float>("EntryWeight"),
                Get<float>("TorsoLeanRadians"),Get<float>("PelvisOffset"),Get<float>("NearLift"),Get<float>("FarLift"),Get<float>("FootTrail"));
        }
        int checks=0;
        void Check(bool value,string label){checks++;TestAssert.True(value,"ORDINARY_AIR_MODEL_RED: "+label);}
        object Primed(){var model=New();Sample(model,0,Vector2.Zero,1,delta:0,grounded:true);return model;}
        var jump=Primed();var launch=Sample(jump,1,new(100,-600),2);
        Check(launch.Active && launch.Started && launch.Arc && launch.Near>0 && launch.Far>0,"fresh accepted jump creates one real launch and articulated tuck");
        var duplicate=Sample(jump,1,new(-99999,99999),2);
        Check(!duplicate.Started && duplicate.Arc && duplicate.Near>launch.Near && duplicate.Trail<launch.Trail,
            "same physical sample eases cached rise targets, not raw replacement velocity");
        var zero=Sample(jump,2,new(0,600),3,delta:0);
        Check(zero==duplicate,"zero time consumes neither a new jump nor new kinematics");
        Check(Sample(jump,2,new(0,-600),3).Started,"fresh positive sample can consume previously zero-time event once");
        var walk=Primed();var walkOff=Sample(walk,0,new(100,100),2);
        Check(walkOff.Active && !walkOff.Started && !walkOff.Arc && walkOff.Near<=0 && walkOff.Far<=0,
            "unsupported departure is a fall reach, not a fabricated launch tuck");
        Check(Sample(walk,1,new(100,-600),3).Started,"actual coyote acceptance works after an already airborne sample");
        var late=New();var first=Sample(late,5,new(100,-600),10);
        Check(first.Active && !first.Started && !first.Arc,"first late observer primes known sequence without replaying an unseen jump");
        Check(Sample(late,6,new(100,-600),11).Started,"a genuinely newer sequence remains observable");
        var staleGround=Primed();Sample(staleGround,2,new(0,-600),2);
        var groundedStale=Sample(staleGround,1,Vector2.Zero,3,grounded:true);
        Check(!groundedStale.Active && !groundedStale.Arc,"real grounding must revoke air pose even when cause observation is stale");
        Check(!Sample(staleGround,2,new(0,-600),4).Arc,"stale-cause landing cannot preserve the old arc for next departure");
        foreach(var boundary in new[]{(Tick:2UL,Delta:0f),(Tick:1UL,Delta:1f/60)})
        {
            var groundBoundary=Primed();
            Sample(groundBoundary,1,Vector2.Zero,boundary.Tick,delta:boundary.Delta,grounded:true);
            var departure=Sample(groundBoundary,1,new(100,30),3);
            Check(!departure.Started && !departure.Arc,
                "grounded zero-time or duplicate physical sample consumes pending cause so later floor loss cannot replay launch");
        }
        foreach(var clear in new[]{"ground","death","replacement","owner0","missing"})
        {
            var model=Primed();Sample(model,1,new(0,-600),2);
            var ended=Sample(model,1,new(0,-600),3,grounded:clear=="ground",suppressed:clear=="death",
                replaced:clear=="replacement",owner:clear=="owner0"?0UL:71UL,observed:clear!="missing");
            Check(!ended.Active && !ended.Started,"ground/death/takeover/missing binding immediately clears visible ordinary arc");
            var again=Sample(model,1,new(0,-600),4);
            Check(!again.Started && !again.Arc,"cleared ordinary jump cannot replay through a missing-owner/observation gap");
        }
        foreach(var gap in new[]{(Tick:1UL,Root:Vector2.Zero),(Tick:100UL,Root:Vector2.Zero),(Tick:3UL,Root:new Vector2(1000,0))})
        {
            var model=Primed();Sample(model,1,new(0,-600),2);
            var discontinuity=Sample(model,2,new(100,-600),gap.Tick,root:gap.Root);
            Check(!discontinuity.Started && !discontinuity.Arc,"rollback/gap/teleport primes without forging a new accepted jump");
        }
        foreach(var bad in new[]{(Tick:0UL,Step:1f/60,Velocity:Vector2.Zero),
            (Tick:3UL,Step:0f,Velocity:Vector2.Zero),(Tick:3UL,Step:1f/60,Velocity:new Vector2(float.NaN,0))})
        {
            var model=Primed();Sample(model,1,new(0,-600),2);
            Check(!Sample(model,2,bad.Velocity,bad.Tick,physicsStep:bad.Step).Active,"invalid physical data cannot drive ordinary air");
        }
        foreach(float delta in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
        {
            var model=Primed();var old=Sample(model,1,new(100,-600),2);
            var paused=Sample(model,1,new(-100,850),3,delta:delta);
            Check(paused with {Started=false}==old with {Started=false},"invalid/zero elapsed cannot advance displayed load");
        }
        var mirrored=Primed();var right=Sample(mirrored,1,new(100,-600),2);
        var left=Sample(mirrored,1,new(100,-600),2,delta:0,facing:-1);
        Check(Mathf.Abs(right.Lean+left.Lean)<.00001f && Mathf.Abs(right.Trail+left.Trail)<.00001f && right.Near==left.Near,
            "facing remaps world horizontal load without reversing jump arc");
        var extreme=Primed();var bounded=Sample(extreme,1,new(float.MaxValue,-float.MaxValue),2,delta:float.MaxValue);
        Check(bounded.Active && float.IsFinite(bounded.Lean) && Mathf.Abs(bounded.Lean)<=.05f &&
            bounded.Near is >=-2 and <=12 && bounded.Far is >=-1 and <=10 && Mathf.Abs(bounded.Trail)<=3,
            "finite extremes stay bounded before float publication");
        Probe? reference=null;
        foreach(int hz in new[]{30,60,120})
        {
            var model=New();Sample(model,0,Vector2.Zero,1,delta:0,grounded:true,physicsStep:1f/hz);
            Probe result=default;
            for(int i=0;i<hz*3/10;i++)result=Sample(model,1,new(100,-600),(ulong)i+2,delta:1f/hz,physicsStep:1f/hz);
            Check(result.Active && result.Arc && result.Near>8 && result.Entry==1,"elapsed-rate check is nonvacuous");
            if(reference is {} expected)Check(Mathf.Abs(result.Near-expected.Near)<.001f && Mathf.Abs(result.Trail-expected.Trail)<.001f,
                "equal elapsed30/60/120 samples agree, not hardware FPS certification");
            reference??=result;
        }
        using var tuning=new PlayerTuning();
        PlayerPresentationInput WithJump(PlayerPresentationInput input,ulong sequence)
        {
            var property=typeof(PlayerPresentationInput).GetProperty("Jump");
            Check(property!=null,"production Timeline input has an actual jump observation, not floor-loss guessing");
            object boxed=input;property.SetValue(boxed,Jump(sequence));return (PlayerPresentationInput)boxed;
        }
        var timeline=new PlayerPresentationTimeline(tuning);
        PlayerPresentationInput state=WithJump(new(Vector2.Zero,true,false,false,false,1),0) with
        {
            Ground=new(true,Vector2.Zero,0,Transform2D.Identity,Vector2.Zero,Vector2.Up){PhysicsTick=1,PhysicsStepSeconds=1f/60}
        };
        timeline.Step(state,1f/60);
        state=state with {Grounded=false,Velocity=new(100,30),Ground=state.Ground with {PhysicsTick=2}};
        Check(timeline.Step(state,1f/60).Locomotion.State==PlayerLocomotionState.Fall,"real observed walk-off directly falls without launch crouch");
        state=state with {Velocity=new(100,60),Ground=state.Ground with {PhysicsTick=3}};
        Check(timeline.Step(state,1f/60).Locomotion.State==PlayerLocomotionState.Fall,"continued walk-off descent cannot flicker back to Apex in the slow-speed band");
        state=WithJump(state with {Velocity=new(100,-600),Ground=state.Ground with {PhysicsTick=4}},1);
        Check(timeline.Step(state,1f/60).Locomotion.State==PlayerLocomotionState.JumpStart,"real coyote/buffered accepted event starts launch from air");
        foreach(float vertical in new[]{0f,.0010886595f,60f,-60f})
        {
            var hovering=new PlayerPresentationTimeline(tuning);
            var stationary=state with {Velocity=new(0,vertical),Jump=new(71,0,true),Ground=state.Ground with {PhysicsTick=5}};
            Check(hovering.Step(stationary,1f/60).Locomotion.State==PlayerLocomotionState.Apex,
                "near-stationary nonjump air after takeover keeps the existing Apex band, not a fabricated downward fall");
        }
        GD.Print($"ORDINARY_AIR_MODEL_OK checks={checks} real_cause=true cached_physics=true no_replay=true elapsed_rates=true bounded=true human=false");
        VerifySupportedCoastRefresh();
    }

    private static void VerifySupportedCoastRefresh()
    {
        PlayerOrdinaryAirInput Coast(ulong tick, ulong owner = 71, ulong sequence = 0, Vector2 root = default) =>
            new(new(owner, sequence, true), new PlayerGroundSample(true, root, 0, Transform2D.Identity,
                Vector2.Zero, Vector2.Up) { PhysicsTick = tick, PhysicsStepSeconds = 1f / 60 },
                new(500, 0), 1, true, false, true) { SupportedJetCoast = true };
        PlayerOrdinaryAirInput Launch(ulong tick, ulong owner = 71) => Coast(tick, owner, 1) with
            { Grounded = false, Replaced = false, SupportedJetCoast = false, Velocity = new(480, -590) };

        var model = new PlayerOrdinaryAirMotion();
        model.Step(Coast(2), 1f / 60);
        var refresh = model.Step(Coast(2), 0);
        var launch = model.Step(Launch(3), 1f / 60);
        TestAssert.True(!refresh.Active && launch.JumpStarted && launch.JumpArc &&
            !model.Step(Launch(3), 1f / 60).JumpStarted,
            "SUPPORTED_COAST_ZERO_REFRESH: same stamped ground refresh must preserve one subsequent launch");
        foreach (var invalid in new[] { Coast(1), Coast(3), Coast(2, owner: 72), Coast(2, root: new(1, 0)),
            Coast(2) with { Suppressed = true }, Coast(2) with { Grounded = false }, Coast(2, sequence: 1) })
        {
            model = new PlayerOrdinaryAirMotion();
            model.Step(Coast(2), 1f / 60);
            model.Step(invalid, 0);
            var after = model.Step(Launch(4, invalid.Jump.OwnerId), 1f / 60);
            TestAssert.True(!after.JumpStarted && !after.JumpArc,
                "incoherent, suppressed, airborne or already consumed coast observation cannot replay launch");
        }
        model = new PlayerOrdinaryAirMotion();
        model.Step(Coast(2), 0);
        TestAssert.True(!model.Step(Launch(3), 1f / 60).JumpStarted, "zero refresh cannot create a ground baseline");
        GD.Print("SUPPORTED_COAST_ZERO_REFRESH_OK same_stamp=true no_replay=true no_zero_prime=true");
    }
}
