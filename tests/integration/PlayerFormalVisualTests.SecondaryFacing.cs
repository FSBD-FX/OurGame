using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Config;
using Knight.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    // Break caught: changing the root mirror without rebasing stored local spring
    // position AND velocity teleports or reverses an existing world-space tail.
    public static void RunSecondaryFacingModel()
    {
        int cases=0,channels=0,tails=0;
        foreach(int rate in new[]{30,60,120})
        foreach(int facing in new[]{1,-1})
        {
            using var tuning=new PlayerTuning();
            var turned=new PlayerPresentationTimeline(tuning);
            var reference=new PlayerPresentationTimeline(tuning);
            PlayerPresentationFrame before=default;
            for(int i=0;i<6;i++)
            {
                var input=SecondaryFacingInput(facing,i<2?new(80,30):new(220,-60),true);
                before=turned.Step(input,1f/rate);
                reference.Step(input,1f/rate);
            }
            var last=SecondaryFacingInput(-facing,new(220,-60),true);
            PlayerPresentationFrame flipped=turned.Step(last,0);
            TestAssert.Equal(before.PresentationTimeSeconds,flipped.PresentationTimeSeconds,
                "basis change cannot advance the presentation clock");
            foreach(var pair in SecondaryFacingChannels(before.Secondary))
            {
                TestAssert.True(Mathf.Abs(pair.Value.Position.X)>.00001f ||
                    Mathf.Abs(pair.Value.RotationRadians)>.00001f,
                    "all four facing controls must carry a nonzero preexisting tail");
                AssertSecondaryWorldNear(pair.Value,facing,SecondaryFacingChannels(flipped.Secondary)[pair.Key],-facing,
                    "zero-time turn "+pair.Key);
                channels++;
            }
            for(int i=0;i<8;i++)
            {
                int current=i%2==0?facing:-facing;
                var roundTrip=turned.Step(last with { Facing=current },0);
                foreach(var pair in SecondaryFacingChannels(before.Secondary))
                    AssertSecondaryWorldNear(pair.Value,facing,SecondaryFacingChannels(roundTrip.Secondary)[pair.Key],current,
                        "repeated basis round trip "+pair.Key);
            }
            // Both histories now perform the same logical turn: one published
            // its basis at zero time, the other first observes it while stepping.
            // Do not compare a driven turn against a history that never turns.
            for(int i=0;i<rate/2;i++)
            {
                var actual=turned.Step(SecondaryFacingInput(-facing,Vector2.Zero,false),1f/rate);
                var expected=reference.Step(SecondaryFacingInput(-facing,Vector2.Zero,false),1f/rate);
                foreach(var pair in SecondaryFacingChannels(expected.Secondary))
                {
                    AssertSecondaryWorldNear(pair.Value,-facing,SecondaryFacingChannels(actual.Secondary)[pair.Key],-facing,
                        "zero refresh preserves the same turn drive and inertial tail "+pair.Key);
                    tails++;
                }
            }
            cases++;
        }
        TestAssert.Equal(6,cases,"both initial orientations at three sampling rates");
        TestAssert.Equal(24,channels,"four nonvacuous channels per turn");
        TestAssert.Equal(840,tails,"complete half-second tails at all sampling rates");
        UnforcedSecondaryReflectionPreservesWorldMomentum();
        SecondaryMotionModelTests.Run();
        GD.Print("SECONDARY_FACING_MODEL_OK cases=6 channels=24 tails=840 zero_time=true repeated_basis=true stored_velocity=true original_spring_cases=true human=false");
    }

    private static void UnforcedSecondaryReflectionPreservesWorldMomentum()
    {
        int channels=0,tails=0;
        foreach(int rate in new[]{30,60,120})
        foreach(int facing in new[]{1,-1})
        {
            var reference=new SecondaryMotionModel();
            var reflected=new SecondaryMotionModel();
            var drive=new SecondaryMotionInput(new(120,-40),new(300,-200),.5f,.3f,.2f,
                PlayerActionPhase.Burst,.8f);
            SecondaryMotionFrame before=default;
            for(int tick=0;tick<6;tick++)
            {
                before=reference.Step(drive,1f/rate);
                reflected.Step(drive,1f/rate);
            }
            reflected.ReflectHorizontalBasis();
            var instant=reflected.Step(default,0);
            foreach(var pair in SecondaryFacingChannels(before))
            {
                TestAssert.True(Mathf.Abs(pair.Value.Position.X)>.00001f &&
                    Mathf.Abs(pair.Value.RotationRadians)>.00001f,
                    "unforced reflection starts with nonzero position and rotation: "+pair.Key);
                AssertSecondaryWorldNear(pair.Value,facing,SecondaryFacingChannels(instant)[pair.Key],-facing,
                    "bare spring instantaneous reflection "+pair.Key);
                channels++;
            }
            for(int tick=0;tick<rate/2;tick++)
            {
                var expected=reference.Step(default,1f/rate);
                var actual=reflected.Step(default,1f/rate);
                foreach(var pair in SecondaryFacingChannels(expected))
                {
                    AssertSecondaryWorldNear(pair.Value,facing,SecondaryFacingChannels(actual)[pair.Key],-facing,
                        "genuinely unforced spring preserves stored world momentum "+pair.Key);
                    tails++;
                }
            }
        }
        TestAssert.Equal(24,channels,"four nonvacuous bare springs for six facing/rate controls");
        TestAssert.Equal(840,tails,"bare spring half-second tails remain independent of turn input");
        GD.Print("SECONDARY_UNFORCED_REFLECTION_MODEL_OK channels=24 tails=840 zero_drive=true stored_momentum=true human=false");
    }

    public static async Task RunSecondaryFacingDrawOnly(SceneTree tree)
    {
        int cases=0,channels=0;
        foreach(int facing in new[]{1,-1})
        foreach(var action in new[]{PlayerActionFrame.None,
            new PlayerActionFrame(PlayerActionState.GroundAttack,PlayerActionPhase.Contact,.55f,false,Vector2.Right),
            new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right)})
        {
            await WithFormalFixture(tree,formal =>
            {
                using var tuning=new PlayerTuning();
                var timeline=new PlayerPresentationTimeline(tuning);
                PlayerPresentationFrame before=default;
                for(int i=0;i<6;i++) before=timeline.Step(
                    SecondaryFacingInput(facing,i<2?new(80,30):new(220,-60),true),1f/60);
                var after=timeline.Step(SecondaryFacingInput(-facing,new(220,-60),true),0);
                // Preserve production-provided coordinate provenance; only replace
                // the locomotion/action used to exercise the real attachment.
                var basis=before with { Base=new(EntityVisualState.Idle,facing,true),
                    Locomotion=new(PlayerLocomotionState.Idle,0),Action=action,
                    Secondary=SecondaryMotionFrame.Identity,PresentationTimeSeconds=0 };
                formal.ApplyPlayerFrame(basis);
                formal.ApplyPlayerFrame(basis with { PresentationTimeSeconds=.2 });
                basis=basis with { PresentationTimeSeconds=.4 };
                formal.ApplyPlayerFrame(basis);
                var old=ObserveSecondaryDrawIncrement(formal,basis,before.Secondary);
                var turnedBasis=basis with { Base=new(EntityVisualState.Idle,-facing,true) };
                var current=ObserveSecondaryDrawIncrement(formal,turnedBasis,after.Secondary);
                foreach(var pair in old)
                {
                    TestAssert.True(pair.Value.Offset.DistanceTo(current[pair.Key].Offset)<.002f &&
                        Mathf.Abs(Mathf.AngleDifference(pair.Value.Angle,current[pair.Key].Angle))<.002f,
                        "SECONDARY_FACING_DRAW_RED: actual rest-relative world inertia must not teleport on turn: "+pair.Key+
                        " action="+action.State+" facing="+facing+" old="+pair.Value+" current="+current[pair.Key]+
                        " mantle_local_rotation="+formal.GetNode<Node2D>("Mantle").Rotation);
                    TestAssert.True(pair.Value.Offset.Length()>.00001f || Mathf.Abs(pair.Value.Angle)>.00001f,
                        "actual Draw observer must see the live delayed channel");
                    channels++;
                }
                TestAssert.Equal(new Vector2(-facing,1),formal.Scale,"only the formal root mirrors");
                foreach(string name in new[]{"Mantle","Lantern","ShoulderCore","Tether"})
                    TestAssert.Equal(Vector2.One,formal.GetNode<Node2D>(name).Scale,
                        "basis continuity must not stretch the attachment: "+name);
                cases++;
            });
        }
        TestAssert.Equal(6,cases,"draw covers both facings with none, attack and shield");
        TestAssert.Equal(24,channels,"actual four-channel consumer per draw case");
        await PlayerMantleMotionTests.Run(tree);
        GD.Print("SECONDARY_FACING_DRAW_OK cases=6 channels=24 mantle_upper_pinned=true actual_rest_relative=true one_root_mirror=true no_scale=true original_mantle_cases=true headless_geometry=true human=false");
    }

    private readonly record struct SecondaryFacingDrawSample(Vector2 Offset,float Angle);

    private static Dictionary<string,SecondaryFacingDrawSample> ObserveSecondaryDrawIncrement(
        PlayerFormalVisual formal,PlayerPresentationFrame basis,SecondaryMotionFrame secondary)
    {
        formal.ApplyPlayerFrame(basis with { Secondary=SecondaryMotionFrame.Identity });
        var rest=new Dictionary<string,(Vector2 Position,float Angle)>();
        foreach(string name in new[]{"Lantern","ShoulderCore","Tether"})
        {
            var node=formal.GetNode<Node2D>(name);
            rest.Add(name,(node.ToGlobal(Vector2.Zero),(node.ToGlobal(Vector2.Right)-node.ToGlobal(Vector2.Zero)).Angle()));
        }
        var cloth=formal.GetNode<PlayerFormalLayer>("Mantle");
        Vector2 restHem=cloth.ToGlobal(new(0,31));
        Vector2 restUpper=cloth.ToGlobal(new(0,-3));
        formal.ApplyPlayerFrame(basis with { Secondary=secondary });
        var result=new Dictionary<string,SecondaryFacingDrawSample>();
        foreach(string name in new[]{"Lantern","ShoulderCore","Tether"})
        {
            var node=formal.GetNode<Node2D>(name);
            float angle=(node.ToGlobal(Vector2.Right)-node.ToGlobal(Vector2.Zero)).Angle();
            result.Add(name,new(node.ToGlobal(Vector2.Zero)-rest[name].Position,
                Mathf.AngleDifference(rest[name].Angle,angle)));
        }
        result.Add("Mantle",new(cloth.ToGlobal(cloth.MapMantlePoint(new(0,31)))-restHem,0));
        TestAssert.True(cloth.ToGlobal(cloth.MapMantlePoint(new(0,-3))).DistanceTo(restUpper)<.0001f,
            "facing continuity cannot unpin the actual upper cloth band");
        // Negative control: clearing the secondary really clears this same observer.
        formal.ApplyPlayerFrame(basis with { Secondary=SecondaryMotionFrame.Identity });
        TestAssert.True(cloth.ToGlobal(cloth.MapMantlePoint(new(0,31))).DistanceTo(restHem)<.0001f,
            "Draw observer cannot manufacture cloth inertia from identity input");
        foreach(string name in new[]{"Lantern","ShoulderCore","Tether"})
            TestAssert.True(formal.GetNode<Node2D>(name).ToGlobal(Vector2.Zero).DistanceTo(rest[name].Position)<.0001f,
                "actual identity removes channel offset rather than accumulating it");
        return result;
    }

    private static PlayerPresentationInput SecondaryFacingInput(int facing,Vector2 velocity,bool casting) =>
        new(velocity,false,false,false,false,facing)
        {
            Thread=casting?new ThreadCastFrame(1,ThreadCastState.Travel,ThreadAimMode.Tap,
                Vector2.Right*facing,2,true,ThreadFailureReason.None,.4f,.3f):default
        };

    private static Dictionary<string,PoseTransform2D> SecondaryFacingChannels(SecondaryMotionFrame frame) => new()
    {
        ["Mantle"]=frame.Mantle,["Lantern"]=frame.Lantern,
        ["ShoulderCore"]=frame.ShoulderSpool,["Tether"]=frame.Tether
    };

    private static void AssertSecondaryWorldNear(PoseTransform2D expected,int expectedFacing,
        PoseTransform2D actual,int actualFacing,string context)
    {
        Vector2 wanted=new(expected.Position.X*expectedFacing,expected.Position.Y);
        Vector2 observed=new(actual.Position.X*actualFacing,actual.Position.Y);
        TestAssert.True(wanted.DistanceTo(observed)<.0001f &&
            Mathf.Abs(expected.RotationRadians*expectedFacing-actual.RotationRadians*actualFacing)<.0001f,
            "SECONDARY_FACING_CONTINUITY_RED: "+context+" world_wanted="+wanted+" world_actual="+observed);
        TestAssert.Equal(expected.Scale,actual.Scale,"basis re-expression does not alter scale");
        TestAssert.Equal(expected.Modulate,actual.Modulate,"basis re-expression does not alter color");
    }
}
