using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class AbyssWindLandingTests
{
    // The real long wind landing must use its actual impact, not lethal height
    // fallback because MoveAndSlide stopped its remaining slope-slide motion.
    public static async Task Run(SceneTree tree,string leaf)
    {
        await RunNativeContracts(tree);
        using var terrainScene=GD.Load<PackedScene>(leaf);
        foreach(bool stopOnSlope in new[]{true,false})
        {
            var root=new Node2D {Name=stopOnSlope?"AbyssStoppedLanding":"AbyssSlidingLanding"};
            root.AddChild(terrainScene.Instantiate());
            var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position=new(1601.1881f,2203.924f); // Sole pre-Ready fixture spawn, not a main-route proof.
            player.FloorStopOnSlope=stopOnSlope;player.GetNode<Camera2D>("Camera2D").Enabled=false;root.AddChild(player);
            var health=player.GetNode<HealthComponent>("Health");var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            tree.Root.AddChild(root);
            try
            {
                int frames=0;while(motor.LandingCount==0 && frames++<420)await KeyboardActionRuntimeTests.Frames(tree,input,1);
                GD.Print($"ABYSS_WIND_IMPACT stop={stopOnSlope} frames={frames} pos={player.Position} hp={health.CurrentHealth} impact={motor.LastLandingImpact.Speed} reason={motor.LastLandingImpact.Reason} height={motor.LastFallHeight} damage={motor.LastFallDamage}");
                TestAssert.True(motor.LandingCount==1 && player.Position.Y is >4470 and <4485,"real isolated wind landing, no pose writes or recovery");
                if(stopOnSlope)
                {
                    TestAssert.True(motor.LastLandingImpact.IsReliable,"ABYSS_WIND_IMPACT_RED: actual stopped landing must be measured under its native motion branch");
                    TestAssert.True(motor.LastLandingImpact.Speed is >498 and <502 && motor.LastFallHeight>2200 && motor.LastFallDamage==1 && health.CurrentHealth==4,
                        "real500px/s long landing mitigates by measured impact but still hurts once");
                }
                else
                    TestAssert.True(!motor.LastLandingImpact.IsReliable && motor.LastLandingImpact.Reason==LandingImpactFallbackReason.MultipleContacts &&
                        player.GetSlideCollisionCount()>1 && motor.LastFallDamage==5 && health.CurrentHealth==0,
                        "unsupported multiple native contacts retain original conservative height fallback, not the new stopped branch");
            }
            finally{await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);}
        }
        GD.Print("ABYSS_WIND_IMPACT_OK cases=2 stopped_reliable=true multiple_contact_rejected=true original_tolerance=0.002 physics_changed=false natural_main=false");
    }

    private static async Task RunNativeContracts(SceneTree tree)
    {
        // These isolated kernel stimuli are not keyboard/main-route evidence.
        // Prepare, native MoveAndSlide and Complete run in the same physics callback.
        foreach (string mode in new[]{"rollback","retained","continued","mutate-stop","mutate-up","mutate-margin","continued-multiple"})
        {
            bool rollback=mode=="rollback", stopped=mode is "rollback" or "retained";
            float gap=rollback?.0805f:.4f;
            using var floorShape=new RectangleShape2D {Size=new(400,40)};
            using var actorShape=new RectangleShape2D {Size=new(20,40)};
            using var sampler=new LandingImpactSampler();
            var root=new Node2D {Name="NativeImpact_"+mode};
            var floor=new StaticBody2D {Position=new(0,120),CollisionLayer=CombatCollisionLayers.World};
            floor.AddChild(new CollisionShape2D {Shape=floorShape});root.AddChild(floor);
            var body=new CharacterBody2D {Position=new(0,80-gap),FloorStopOnSlope=stopped,
                SafeMargin=.08f,CollisionLayer=CombatCollisionLayers.Player,CollisionMask=CombatCollisionLayers.World};
            body.AddChild(new CollisionShape2D {Shape=actorShape});root.AddChild(body);
            var observer=new BasinBoardingTickObserver();root.AddChild(observer);
            Exception failure=null;bool done=false;
            observer.Sample=delta=>
            {
                if(done)return;
                try
                {
                    Vector2 incoming=new(mode=="continued-multiple"?100:0,500);
                    var before=sampler.Prepare(body,incoming,false,delta);
                    body.Velocity=incoming;body.MoveAndSlide();
                    var support=sampler.InspectFloorSupport(body);
                    Vector2 actual=body.GlobalPosition-before.Origin;
                    int slides=body.GetSlideCollisionCount();
                    GD.Print($"ABYSS_IMPACT_CONTRACT_TRACE mode={mode} gap={gap} dt={delta} prepare={before.Reason} travel={before.Travel} actual={actual} slides={slides} support={support.Complete}/{support.Unique} velocity={body.Velocity}");
                    if(mode=="continued-multiple")
                    {
                        var rejected=sampler.Complete(body,before,support);
                        int fallback=PlayerFallDamageModel.CalculateImpactDamage(2272,rejected.Speed,rejected.IsReliable,600,400,300,780,5);
                        TestAssert.True(before.Reason==LandingImpactFallbackReason.Reliable && slides>1 &&
                            rejected.Reason==LandingImpactFallbackReason.MultipleContacts && !rejected.IsReliable && fallback==5,
                            "nonzero horizontal native remainder produces unsupported multiple contacts, never a fabricated single-slide positive");
                        GD.Print($"ABYSS_IMPACT_CONTRACT_OK mode={mode} reason={rejected.Reason} damage={fallback} real_native=true controlled_kernel=true normal_route=false");
                        return;
                    }
                    TestAssert.True(before.Reason==LandingImpactFallbackReason.Reliable && body.IsOnFloor() &&
                        slides==1 && support.Complete && support.Unique,"directed native single-contact eligibility before property mutation: "+mode);
                    if(stopped)
                    {
                        TestAssert.Equal(Vector2.Zero,body.Velocity,"native stopped branch cancels velocity");
                        TestAssert.True(rollback?before.Travel.Length()<=(double)before.SafeMargin+.00001d:
                            before.Travel.Length()>(double)before.SafeMargin+.00001d,"actual travel straddles native margin threshold");
                        TestAssert.True((actual-(rollback?Vector2.Zero:before.Travel)).Length()<=.002f,
                            "actual native stopped net travel: "+mode);
                    }
                    else TestAssert.True(!before.FloorStopOnSlope &&
                        (actual-(before.Travel+before.Remainder.Slide(before.Normal))).Length()<=.002f,
                        "verified single-slide continued-mode displacement (vertical remaining projection is zero)");
                    if(mode=="mutate-stop")body.FloorStopOnSlope=!before.FloorStopOnSlope;
                    if(mode=="mutate-up")body.UpDirection=new Vector2(.01f,-1).Normalized();
                    if(mode=="mutate-margin")body.SafeMargin=before.SafeMargin+.01f;
                    var sample=sampler.Complete(body,before,support);
                    bool mutated=mode.StartsWith("mutate-",StringComparison.Ordinal);
                    int damage=PlayerFallDamageModel.CalculateImpactDamage(2272,sample.Speed,sample.IsReliable,600,400,300,780,5);
                    TestAssert.True(mutated?!sample.IsReliable && sample.Reason==LandingImpactFallbackReason.MotionMismatch:
                        sample.IsReliable && Math.Abs(sample.Speed-500)<.002f,"directed measurement/rejection: "+mode);
                    TestAssert.Equal(mutated?5:1,damage,"unchanged damage consumer uses actual eligibility: "+mode);
                    GD.Print($"ABYSS_IMPACT_CONTRACT_OK mode={mode} reason={sample.Reason} damage={damage} real_native=true controlled_kernel=true normal_route=false");
                }
                catch(Exception error){failure=error;}
                finally{done=true;observer.Sample=null;}
            };
            tree.Root.AddChild(root);
            try
            {
                for(int wait=0;!done && wait<120;wait++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                TestAssert.True(done,"directed kernel callback completes");
                if(failure!=null)throw failure;
            }
            finally{observer.Sample=null;root.Free();}
        }
        // Native real_t length is float, but the C++ literal 0.01 is double.
        TestAssert.True(LandingImpactSampler.NativeSlopeStopDirection(.01f),
            "NATIVE_SLOPE_STOP_BOUNDARY_RED: float 0.01 lies below native double 0.01");
        TestAssert.True(!LandingImpactSampler.NativeSlopeStopDirection(MathF.BitIncrement(.01f)) &&
            LandingImpactSampler.NativeSlopeStopDirection(MathF.BitDecrement(.01f)) &&
            !LandingImpactSampler.NativeSlopeStopDirection(float.NaN) &&
            !LandingImpactSampler.NativeSlopeStopDirection(float.PositiveInfinity),"native threshold adjacent and invalid values");
        GD.Print("ABYSS_IMPACT_CONTRACTS_OK native_cases=7 mutation_rejections=3 direction_boundary=true nonzero_slide_rejected=true original_tolerance=0.002 physics_changed=false normal_route=false");
    }
}
