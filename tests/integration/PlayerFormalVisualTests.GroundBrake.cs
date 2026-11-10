using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    public static void RunGroundBrakeModel()
    {
        Type modelType=typeof(PlayerMotor).Assembly.GetType("Knight.Presentation.PlayerGroundBrakeModel");
        TestAssert.True(modelType!=null,"GROUND_BRAKE_MODEL_RED: real ordinary control facts need an independent measured brake consumer");
        int checks=0;
        void Check(bool valid,string label){checks++;TestAssert.True(valid,"GROUND_BRAKE_MODEL_RED: "+label);}
        object Fresh(){object model=Activator.CreateInstance(modelType);BrakeSample(model,1,260,260,axis:1,reverse:false);return model;}
        object reverse=Fresh();
        object force=BrakeSample(reverse,2,260,100);
        Check(BrakeRead<bool>(force,"Observed") && BrakeRead<bool>(force,"Braking") &&
            BrakeRead<bool>(force,"Reverse") && !BrakeRead<bool>(force,"CrossedZero"),"reverse facts are not a velocity-sign guess");
        Check(BrakeRead<float>(force,"Strength")>.6f && BrakeRead<float>(force,"Strength")<=1 &&
            BrakeRead<float>(force,"PelvisDrop")>1 && BrakeRead<float>(force,"TorsoLeanWorld")<0,
            "high speed produces bounded bracing against incoming motion");
        object duplicate=BrakeSample(reverse,2,-260,-100,axis:1);
        Check(force.Equals(duplicate),"same physics tick cannot replace accepted control facts");
        Check(force.Equals(BrakeSample(reverse,3,260,100,delta:0)),"zero-time publication cannot consume the next physics step");
        Check(BrakeRead<bool>(BrakeSample(reverse,3,260,100),"Braking"),"deferred fresh step is consumed on positive time");
        object passive=BrakeSample(Fresh(),2,260,140,axis:0,reverse:false);
        Check(BrakeRead<bool>(passive,"Braking") && !BrakeRead<bool>(passive,"Reverse") &&
            BrakeRead<float>(passive,"Strength")<BrakeRead<float>(force,"Strength"),"passive friction differs from stronger counter-thrust");
        object low=BrakeSample(Fresh(),2,26,10,step:.005f);
        Check(BrakeRead<float>(low,"PelvisDrop")<BrakeRead<float>(force,"PelvisDrop"),"slow control braking cannot look like full-speed catch");
        object left=BrakeSample(Fresh(),2,-260,-100,axis:1);
        Check(BrakeRead<float>(left,"TorsoLeanWorld")>0 &&
            Mathf.Abs(BrakeRead<float>(left,"Strength")-BrakeRead<float>(force,"Strength"))<.0001f,
            "world force mirrors while intensity remains symmetric");
        object crossing=BrakeSample(Fresh(),2,150,-100,step:.1f,zero:true);
        Check(BrakeRead<bool>(crossing,"CrossedZero") && BrakeRead<float>(crossing,"TorsoLeanWorld")<0,
            "same-step stop and reverse retains incoming positive direction");
        Check(!BrakeRead<bool>(BrakeSample(Fresh(),2,100,150,axis:1,reverse:false),"Braking"),"acceleration is not braking");
        Check(!BrakeRead<bool>(BrakeSample(Fresh(),2,100,0,axis:1,reverse:false,eligible:false),"Braking"),
            "collision or ownership rejection cannot manufacture control load");
        Check(!BrakeRead<bool>(BrakeSample(Fresh(),2,260,100,axis:-1,reverse:false),"Braking"),
            "opposed input with a contradictory no-reverse fact cannot be accepted as passive braking");
        foreach(string rejection in new[]{"owner","revision","rollback","gap","air","suppressed","eligible","ground","teleport","invalid"})
        {
            object model=Fresh();BrakeSample(model,2,260,100);
            object bad=rejection switch
            {
                "owner"=>BrakeSample(model,3,260,100,owner:2),
                "revision"=>BrakeSample(model,3,260,100,revision:2),
                "rollback"=>BrakeSample(model,1,260,100),
                "gap"=>BrakeSample(model,20,260,100),
                "air"=>BrakeSample(model,3,260,100,grounded:false),
                "suppressed"=>BrakeSample(model,3,260,100,suppressed:true),
                "eligible"=>BrakeSample(model,3,260,100,eligible:false),
                "ground"=>BrakeSample(model,3,260,100,groundObserved:false),
                "teleport"=>BrakeSample(model,3,260,100,rootX:1000),
                _=>BrakeSample(model,3,float.NaN,100)
            };
            Check(!BrakeRead<bool>(bad,"Braking") && BrakeRead<float>(bad,"Strength")==0,"clear/reset rule: "+rejection);
            if(rejection is "air" or "suppressed" or "eligible" or "ground" or "invalid")
                Check(!BrakeRead<bool>(BrakeSample(model,3,260,100),"Braking"),"same rejected tick cannot be revived: "+rejection);
        }
        // Break caught: cache/zero-time returns retain a nonzero brace even
        // after the supporting surface becomes invalid. Clearing consumes the
        // rejected stamp, so restoring that same stamp must not revive it.
        foreach(string rejection in new[]{"unobserved","id","no-bounds","bounds","normal","transform","stamp"})
        foreach(int timing in new[]{0,1,2})
        {
            object model=Fresh();BrakeSample(model,2,260,100);
            ulong tick=timing==2?3UL:2UL;
            var good=BrakeGround(tick,0);
            var bad=rejection switch
            {
                "unobserved"=>good with { Observed=false },
                "id"=>good with { SupportId=0 },
                "no-bounds"=>good with { HasBounds=false },
                "bounds"=>good with { SupportBounds=new(0,36,0,20) },
                "normal"=>good with { GroundNormal=Vector2.Down },
                "transform"=>good with { SupportTransform=new(Vector2.Zero,Vector2.Zero,Vector2.Zero) },
                _=>good with { PhysicsTick=tick+1 }
            };
            object cleared=BrakeSample(model,tick,260,100,delta:timing==0?.05f:0f,groundOverride:bad);
            Check(!BrakeRead<bool>(cleared,"Observed") && BrakeRead<float>(cleared,"Strength")==0,
                $"support invalidation precedes duplicate/zero-time cache: {rejection}/{timing}");
            Check(!BrakeRead<bool>(BrakeSample(model,tick,260,100),"Braking"),
                $"same rejected support stamp cannot revive: {rejection}/{timing}");
        }
        foreach(int rate in new[]{30,60,120})
        {
            object model=Activator.CreateInstance(modelType);
            BrakeSample(model,1,1000,1000,axis:1,reverse:false,step:1f/rate);
            object result=null;
            float position=0;
            for(int i=1;i<=rate/5;i++)
            {
                float before=1000-(i-1)*3200f/rate,after=1000-i*3200f/rate;
                position+=after/rate;
                result=BrakeSample(model,(ulong)i+1,before,after,step:1f/rate,rootX:position);
            }
            Check(Mathf.Abs(BrakeRead<float>(result,"Strength")-.9967015f)<.0001f,
                "hand-derived one-minus-exp envelope at 0.2 seconds, rate="+rate);
        }
        GD.Print($"GROUND_BRAKE_MODEL_OK checks={checks} real_step=true incoming_zero_cross=true stamped=true rates=30,60,120 no_replay=true human=false");
    }

    public static async Task RunGroundBrakeDrawOnly(SceneTree tree)
    {
        MethodInfo step=typeof(PlayerGroundGait).GetMethod("Step");
        TestAssert.True(step.GetParameters().Length==5,"GROUND_BRAKE_GAIT_RED: measured braking must reach the actual free-foot path");
        int lifts=0,stance=0;
        var normal=new PlayerGroundGait();var braked=new PlayerGroundGait();
        for(int i=0;i<150;i++)
        {
            var sample=BrakeGround((ulong)i+1,i*2);
            PlayerGroundGaitFrame a=BrakeGait(step,normal,sample,0),b=BrakeGait(step,braked,sample,1);
            TestAssert.Near(a.Phase,b.Phase,.0001f,"braking cannot change actual distance phase");
            if(a.NearStance){TestAssert.True(b.NearStance && a.NearSoleWorld.DistanceTo(b.NearSoleWorld)<.001f,"near planted foot is not dragged");stance++;}
            if(a.FarStance){TestAssert.True(b.FarStance && a.FarSoleWorld.DistanceTo(b.FarSoleWorld)<.001f,"far planted foot is not dragged");stance++;}
            if(i>10 && !a.NearStance && a.NearSoleWorld.Y<35){TestAssert.True(b.NearSoleWorld.Y>a.NearSoleWorld.Y+.1f,"measured brake reduces free near-foot lift");lifts++;}
            if(i>10 && !a.FarStance && a.FarSoleWorld.Y<35){TestAssert.True(b.FarSoleWorld.Y>a.FarSoleWorld.Y+.1f,"measured brake reduces free far-foot lift");lifts++;}
        }
        TestAssert.True(lifts>30 && stance>150,"real matched-path brace comparison is nonvacuous");
        Type frameType=typeof(PlayerMotor).Assembly.GetType("Knight.Presentation.PlayerGroundBrakeFrame");
        TestAssert.True(frameType!=null,"GROUND_BRAKE_DRAW_RED: missing output value contract");
        int drawn=0,supports=0;
        await WithFormalFixture(tree,formal=>
        {
            var near=formal.GetNode<PlayerFormalLayer>("NearLeg");var far=formal.GetNode<PlayerFormalLayer>("FarLeg");
            foreach(int facing in new[]{1,-1})
            {
                var model=new PlayerGroundGait();
                for(int i=0;i<100;i++)
                {
                    float x=i<50?i*1.2f:60-(i-50)*1.2f;
                    formal.GlobalPosition=new(x,0);
                    var sample=BrakeGround((ulong)i+1,x);
                    var gait=BrakeGait(step,model,sample,1);
                    var action=i is >=20 and <40?new PlayerActionFrame(PlayerActionState.GroundAttack,PlayerActionPhase.Contact,.55f,false):
                        i is >=40 and <60?new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,.55f,false):PlayerActionFrame.None;
                    object load=Activator.CreateInstance(frameType,new object[]{true,true,true,false,1f,2.2f,-.13f});
                    var frame=WithGroundBrake(Frame(PlayerLocomotionState.Brake,.3f,action,facing:facing) with
                        { Gait=gait,BodyLoad=new(true,1.5f,.1f),PresentationTimeSeconds=i/60d },load);
                    formal.ApplyPlayerFrame(frame);
                    var once=formal.CurrentPose;
                    formal.ApplyPlayerFrame(frame with { BodyLoad=new(true,0,0) });
                    TestAssert.Equal(once,formal.CurrentPose,"GROUND_BRAKE_DRAW_RED: measured brake replaces, not adds the old body load");
                    foreach(var leg in new[]{near,far})
                    {
                        TestAssert.Near(18,leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),.002f,"braking upper link stays rigid");
                        TestAssert.Near(18,leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),.002f,"braking lower link stays rigid");
                        TestAssert.True(!leg.LegReachClamped,"GROUND_BRAKE_DRAW_RED: lowered swing/support cannot hide unreachable feet");
                        TestAssert.Equal(Vector2.One,leg.Scale,"bracing cannot stretch a leg image");
                    }
                    if(gait.NearStance){TestAssert.True(near.ToGlobal(near.LegSoleLocal).DistanceTo(gait.NearSoleWorld)<.02f,"braced near support reaches actual Draw");supports++;}
                    if(gait.FarStance){TestAssert.True(far.ToGlobal(far.LegSoleLocal).DistanceTo(gait.FarSoleWorld)<.02f,"braced far support reaches actual Draw");supports++;}
                    drawn++;
                }
            }
        });
        TestAssert.True(drawn==200 && supports>200,"actual braced trajectory includes both facing/support chains");
        GD.Print($"GROUND_BRAKE_DRAW_OK samples={drawn} supports={supports} lower_swing={lifts} planted=true rigid=true no_clamp=true facings=2 actions=true human=false");
    }

    private static PlayerGroundGaitFrame BrakeGait(MethodInfo step,PlayerGroundGait model,PlayerGroundSample ground,float strength) =>
        (PlayerGroundGaitFrame)step.Invoke(model,new object[]{ground,true,1f/60,false,strength});

    private static PlayerGroundSample BrakeGround(ulong tick,float x,float seconds=.05f) =>
        new(true,new(x,0),101,Transform2D.Identity,new(0,36),Vector2.Up)
        { HasBounds=true,SupportBounds=new(-10000,36,20000,20),PhysicsTick=tick,PhysicsStepSeconds=seconds };

    private static object BrakeSample(object model,ulong tick,float before,float after,float axis=-1,bool reverse=true,
        bool zero=false,float step=.05f,float delta=.05f,ulong owner=1,ulong revision=1,
        bool grounded=true,bool eligible=true,bool suppressed=false,bool groundObserved=true,float rootX=0,
        PlayerGroundSample? groundOverride=null)
    {
        Type observationType=typeof(PlayerMotor).Assembly.GetType("Knight.Player.PlayerGroundMotionObservation");
        TestAssert.True(observationType!=null,"GROUND_BRAKE_MODEL_RED: completed Motor observation missing");
        object observation=Activator.CreateInstance(observationType,new object[]{owner,revision,tick,step,before,after,after,axis,
            grounded,eligible,reverse,zero});
        var ground=groundOverride??(BrakeGround(tick,rootX,step) with { Observed=groundObserved });
        return model.GetType().GetMethod("Step").Invoke(model,new object[]{observation,ground,grounded,suppressed,delta});
    }

    private static T BrakeRead<T>(object value,string name)=>(T)value.GetType().GetProperty(name).GetValue(value);

    private static PlayerPresentationFrame WithGroundBrake(PlayerPresentationFrame frame,object load)
    {
        var property=typeof(PlayerPresentationFrame).GetProperty("GroundBrake");
        TestAssert.True(property!=null,"GROUND_BRAKE_DRAW_RED: production output is not connected to final Draw");
        object boxed=frame;property.SetValue(boxed,load);return (PlayerPresentationFrame)boxed;
    }

    // Break caught: ordinary control friction is physically slowing a live
    // Player, but presentation still runs until speed has already reached zero.
    public static async Task RunGroundBrakeNative(SceneTree tree)
    {
        ReleaseInput();
        var root=new Node2D { Name="GroundBrakeNativeFixture" };
        var floor=new StaticBody2D { Position=new(0,50),CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new(10000,20) } });
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        var probe=new BasinBoardingTickObserver { ProcessPhysicsPriority=100 };
        root.AddChild(floor);root.AddChild(player);root.AddChild(probe);tree.Root.AddChild(root);
        try
        {
            await Frames(tree,30);
            var motor=player.GetNode<PlayerMotor>("Motor");
            var coordinator=player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var formal=player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            Input.ActionPress(InputActions.MoveRight);
            await Frames(tree,40);
            TestAssert.True(player.IsOnFloor() && player.Velocity.X>80,
                "ground brake fixture must establish actual forward motion without velocity injection");
            TestAssert.True(formal.CurrentPlayerFrame.Locomotion.State!=PlayerLocomotionState.Brake,
                "steady forward control cannot be falsely classified as braking");
            float previous=player.Velocity.X;
            int slowing=0,braced=0;
            Exception sampleFault=null;
            bool observing=true;
            probe.Sample=delta=>
            {
                if(!observing || sampleFault!=null) return;
                try
                {
                    coordinator._Process(delta); // Actual priority100, after priority0 Motor completion.
                    float speed=player.Velocity.X;
                    if(speed>8 && speed<previous-.01f)
                    {
                        slowing++;
                        if(formal.CurrentPlayerFrame.Locomotion.State==PlayerLocomotionState.Brake) braced++;
                        var observed=motor.GroundMotionObservation;
                        TestAssert.True(observed.OwnerId==motor.GetInstanceId() && observed.Revision==motor.MotionAuthorityRevision &&
                            observed.PhysicsTick==Engine.GetPhysicsFrames() && observed.Grounded && observed.Eligible &&
                            observed.MoveAxis==0 && observed.BeforeX>observed.IntegratedX &&
                            Mathf.Abs(observed.ActualX-player.Velocity.X)<.0001f,
                            "GROUND_BRAKE_NATIVE_RED: exact completed Motor control/owner/tick facts must reach the production reader "+
                            $"observation={observed} current_tick={Engine.GetPhysicsFrames()} owner={motor.GetInstanceId()} revision={motor.MotionAuthorityRevision} actual_velocity={player.Velocity}");
                    }
                    previous=speed;
                }
                catch(Exception error){sampleFault=error;}
            };
            Input.ActionRelease(InputActions.MoveRight);
            await Frames(tree,24);
            observing=false;
            if(sampleFault!=null) throw new InvalidOperationException("post-Motor brake observation fault",sampleFault);
            TestAssert.True(slowing>=2 && braced>0,
                $"GROUND_BRAKE_NATIVE_RED: actual control deceleration must brace before zero speed slowing={slowing} braced={braced}");
            TestAssert.True(motor.AcceptedJumpCount==0 && motor.AcceptedDashCount==0 &&
                Mathf.Abs(player.Velocity.X)<.01f,
                "ordinary braking preserves actual no-jump/no-thruster stop");
            int clears=0;
            async Task Reestablish()
            {
                await Frames(tree,2);
                TestAssert.True(motor.GroundMotionObservation.OwnerId==motor.GetInstanceId(),"live ordinary step reestablishes current observation");
            }
            void Cleared(string why)
            {
                TestAssert.True(motor.GroundMotionObservation.OwnerId==0,"GROUND_BRAKE_CLEAR_RED: immediate invalidation at "+why);
                coordinator._Process(0);
                TestAssert.True(!formal.CurrentPlayerFrame.GroundBrake.Braking && formal.CurrentPlayerFrame.GroundBrake.Strength==0,
                    "zero-time authority loss cannot retain an old measured brace");
                clears++;
            }
            await Reestablish();motor.ApplyGrappleConstraint(Vector2.Zero,1);Cleared("constraint");
            await Reestablish();motor.ClearGrappleConstraint();Cleared("clear constraint");
            await Reestablish();motor.DetachGrappleWithoutRelease();Cleared("detach");
            await Reestablish();motor.ApplyGrappleRelease(Vector2.Up,Vector2.Zero,1000,false);Cleared("release without added force");
            await Reestablish();motor.ExternalImpulse(Vector2.Zero);Cleared("external stop");
            await Reestablish();motor.ResetTransientState();Cleared("reset");
            await Reestablish();
            tree.Paused=true;
            try{Cleared("pause");}finally{tree.Paused=false;}
            Cleared("unpause before fresh physics");
            await Reestablish();motor.SetPhysicsProcess(false);
            try{Cleared("processing disabled");}finally{motor.SetPhysicsProcess(true);}
            TestAssert.Equal(9,clears,"all scoped lifecycle/admission reversals checked");
            await RunGroundBrakeReentrantClear(tree,false);
            await RunGroundBrakeReentrantClear(tree,true);
            GD.Print("GROUND_BRAKE_NATIVE_OK passive_before_zero=true completed_snapshot=true lifecycle_clears=9 reentrant_clears=2 saved_player=true actual_motor=true action_input=true controlled_post_motor=true ordinary_motion_unforced=true human=false");
        }
        finally
        {
            probe.SetPhysicsProcess(false);probe.Sample=null;
            ReleaseInput();
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);
        }
    }

    // Break caught: a synchronous landing-damage callback clears/transfers
    // claw observation authority, but the completing outer Motor republishes
    // its frozen pre-callback candidate with post-callback velocity.
    private static async Task RunGroundBrakeReentrantClear(SceneTree tree,bool release)
    {
        var root=new Node2D { Name=release?"GroundBrakeReentrantRelease":"GroundBrakeReentrantClear" };
        var floor=new StaticBody2D { Position=new(0,2020),CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new(1200,40) } });
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position=new(0,2000-36-800);
        player.GetNode<Camera2D>("Camera2D").Enabled=false;
        var motor=player.GetNode<PlayerMotor>("Motor");
        var health=player.GetNode<HealthComponent>("Health");
        var probe=new BasinBoardingTickObserver { ProcessPhysicsPriority=100 };
        root.AddChild(floor);root.AddChild(player);root.AddChild(probe);tree.Root.AddChild(root);
        int callbacks=0,completions=0;
        ulong callbackTick=0,beforeRevision=0,afterRevision=0;
        Exception fault=null;
        void OnDamage(int remaining,Vector2 impulse)
        {
            callbacks++;callbackTick=Engine.GetPhysicsFrames();beforeRevision=motor.MotionAuthorityRevision;
            if(release) motor.ApplyGrappleRelease(Vector2.Up,new(75,0),1000,false);
            else motor.ClearGrappleConstraint();
            afterRevision=motor.MotionAuthorityRevision;
        }
        health.Damaged+=OnDamage;
        probe.Sample=_=>
        {
            if(callbackTick!=Engine.GetPhysicsFrames() || completions!=0) return;
            completions++;
            try
            {
                TestAssert.True(beforeRevision==afterRevision && health.CurrentHealth>0,
                    "reentrant fixture changes observation authority, not motion revision/death");
                TestAssert.True(motor.GroundMotionObservation.OwnerId==0,
                    $"GROUND_BRAKE_REENTRANT_RED: completing step cannot overwrite callback invalidation release={release} observed={motor.GroundMotionObservation}");
                if(release) TestAssert.Near(75,player.Velocity.X,.001f,
                    "observation cancellation must not undo actual claw release velocity");
            }
            catch(Exception error){fault=error;}
        };
        try
        {
            for(int i=0;i<180 && completions==0;i++) await Frames(tree,1);
            if(fault!=null) throw new InvalidOperationException("post-Motor reentrant observation fault",fault);
            TestAssert.True(callbacks==1 && completions==1 && motor.LastFallDamageApplied,
                $"actual synchronous damaged landing must complete once callbacks={callbacks} completions={completions} height={motor.LastFallHeight} damage={motor.LastFallDamage}");
        }
        finally
        {
            health.Damaged-=OnDamage;probe.SetPhysicsProcess(false);probe.Sample=null;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);
        }
    }
}
