using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    private static readonly string[] Layers =
    [
        "Body", "NearArm", "FarArm", "NearLeg", "FarLeg", "Weapon", "Mantle",
        "ShoulderCore", "Lantern", "Tether"
    ];

    public static async Task Run(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
    }

    public static async Task RunGroundGaitOnly(SceneTree tree)
    {
        await RunGroundSeamNative(tree);
        await WithFormalFixture(tree,GroundGaitDrawControls);
        await RealCoordinatorPublishesMeasuredGroundGait(tree);
    }

    public static async Task RunDirectionalAirJetOnly(SceneTree tree)
    {
        await WithFormalFixture(tree,formal =>
        {
            int cases=0,slots=0,soles=0;
            foreach(int facing in new[]{1,-1})
            foreach(var action in new[]{PlayerActionFrame.None,
                new PlayerActionFrame(PlayerActionState.AirAttack,PlayerActionPhase.Burst,.4f,false,Vector2.Right),
                new PlayerActionFrame(PlayerActionState.AirAttack,PlayerActionPhase.Contact,1,false,Vector2.Right),
                new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right)})
            {
                for(int i=0;i<24;i++)formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle,0,PlayerActionFrame.None,facing:facing));
                var frame=Frame(PlayerLocomotionState.Dash,1,action,facing:facing) with
                {
                    Base=new EntityPresentationFrame(EntityVisualState.Dash,facing,false),
                    AirJet=new(true,true,Vector2.Right,0,0,0,0,0),PresentationTimeSeconds=0,
                    // A fading old support must not pin free-flight feet.
                    Gait=new(true,.5f,0,new(999,999),new(-999,999),true,true)
                };
                formal.ApplyPlayerFrame(frame);
                frame=frame with { PresentationTimeSeconds=.2 };
                formal.ApplyPlayerFrame(frame);
                formal.ApplyPlayerFrame(frame with { PresentationTimeSeconds=.4 });
                frame=frame with { PresentationTimeSeconds=.4 };
                var oldPose=formal.CurrentPose;
                var original=new Dictionary<string,(Vector2 Position,float Rotation,Vector2 RestPosition)>();
                for(int i=0;i<Layers.Length;i++)
                {
                    var node=formal.GetNode<Node2D>(Layers[i]);
                    original.Add(Layers[i],(node.Position,node.Rotation,node.Position-oldPose.Get((PlayerPoseSlot)i).Position));
                }
                Vector2 pivot=(original["NearLeg"].RestPosition+original["FarLeg"].RestPosition)*.5f;
                Vector2 root=formal.GlobalPosition;
                float angle=.1f*facing;
                formal.ApplyPlayerFrame(frame with { AirJet=new(true,true,Vector2.Right,angle,.5f,8,6,-3*facing) });
                for(int i=0;i<Layers.Length;i++)
                {
                    var node=formal.GetNode<Node2D>(Layers[i]);var old=original[Layers[i]];
                    Vector2 wanted=pivot+(old.Position-pivot).Rotated(angle)+new Vector2(0,.5f);
                    TestAssert.True(node.Position.DistanceTo(wanted)<.002f,
                        "AIR_JET_DRAW_RED: every actual attachment must share hip-pivot directional load, including "+Layers[i]);
                    TestAssert.Near(old.Rotation+angle,node.Rotation,.002f,"air thrust uses coherent rigid attachment rotation");
                    TestAssert.Equal(Vector2.One,node.Scale,"air thrust cannot stretch any image slot");slots++;
                }
                TestAssert.Equal(root,formal.GlobalPosition,"air pose never moves collision/root");
                TestAssert.Equal(new Vector2(facing,1),formal.Scale,"air pose only mirrors root scale");
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    var leg=formal.GetNode<PlayerFormalLayer>(name);
                    Vector2 wanted=leg.FreeLegSoleLocal+new Vector2(-3*facing,name=="NearLeg"?-8:-6);
                    TestAssert.True(leg.LegSoleLocal.DistanceTo(wanted)<.002f,
                        "AIR_JET_DRAW_RED: airborne fold/trail must reach actual rigid foot Draw, not old ground support");
                    TestAssert.Near(18,leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),.002f,"air upper link stays rigid");
                    TestAssert.Near(18,leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),.002f,"air lower link stays rigid");
                    TestAssert.True(!leg.LegReachClamped,"air folded foot cannot hide unreachable target by clamp");soles++;
                }
                foreach(var state in new[]{PlayerLocomotionState.Dead,PlayerLocomotionState.Recovery})
                {
                    var terminal=frame with { Locomotion=new(state,1) };
                    formal.ApplyPlayerFrame(terminal);var unloaded=formal.CurrentPose;
                    formal.ApplyPlayerFrame(terminal with { AirJet=new(true,true,Vector2.Right,angle,.5f,8,6,-3*facing) });
                    TestAssert.Equal(unloaded,formal.CurrentPose,"terminal pose cannot retain flight load");
                    foreach(string name in new[]{"NearLeg","FarLeg"})
                    {
                        var leg=formal.GetNode<PlayerFormalLayer>(name);
                        TestAssert.Equal(leg.FreeLegSoleLocal,leg.LegSoleLocal,"terminal legs release air targets");
                    }
                }
                var grounded=frame with { Base=new(EntityVisualState.Dash,facing,true),Gait=default };
                formal.ApplyPlayerFrame(grounded);var onGround=formal.CurrentPose;
                formal.ApplyPlayerFrame(grounded with { AirJet=new(true,true,Vector2.Right,angle,.5f,8,6,-3*facing) });
                TestAssert.Equal(onGround,formal.CurrentPose,"grounding revokes airborne load");
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    var leg=formal.GetNode<PlayerFormalLayer>(name);
                    TestAssert.Equal(leg.FreeLegSoleLocal,leg.LegSoleLocal,"grounded legs cannot retain air targets");
                }
                cases++;
            }
            GD.Print($"AIR_JET_DRAW_OK cases={cases} slots={slots} soles={soles} rigid_links=true action_attachments=true terminal_ground_clear=true headless_geometry=true human=false");
        });
    }

    public static async Task RunLandingLoadOnly(SceneTree tree)
    {
        await WithFormalFixture(tree, formal =>
        {
            int cases=0, soles=0;
            void Reset(int facing)
            {
                for(int i=0;i<24;i++)
                    formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle,0,PlayerActionFrame.None,facing:facing));
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle,0,PlayerActionFrame.None,facing:facing)
                    with { PresentationTimeSeconds=0 });
            }
            foreach(int facing in new[]{1,-1})
            foreach(var action in new[]{PlayerActionFrame.None,
                new PlayerActionFrame(PlayerActionState.GroundAttack,PlayerActionPhase.Contact,1,false,Vector2.Right),
                new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right)})
            {
                Reset(facing);
                formal.GlobalPosition=Vector2.Zero;
                var gait=new PlayerGroundGaitFrame(true,1,0,new(8*facing,36),new(-8*facing,36),true,true)
                    { GroundUp=Vector2.Up };
                var baseline=Frame(PlayerLocomotionState.Dash,1,action,facing:facing) with
                {
                    Base=new EntityPresentationFrame(EntityVisualState.Dash,facing,true),
                    Gait=gait, PresentationTimeSeconds=0
                };
                formal.ApplyPlayerFrame(baseline);
                var oldPose=formal.CurrentPose;
                Vector2 rootPosition=formal.GlobalPosition;
                var oldCore=formal.GetNode<Node2D>("ShoulderCore").Position;
                var oldWeapon=formal.GetNode<Node2D>("Weapon").Position;
                formal.ApplyPlayerFrame(baseline with { LandingLoad=new(true,3,.9f) });
                TestAssert.Near(3,formal.CurrentPose.Body.Position.Y-oldPose.Body.Position.Y,.001f,
                    "LANDING_LOAD_DRAW_RED: real supported Dash must compress body at contact, not after burn");
                TestAssert.Near(3,formal.GetNode<Node2D>("ShoulderCore").Position.Y-oldCore.Y,.001f,
                    "LANDING_LOAD_DRAW_RED: attached core follows common body absorption");
                TestAssert.Near(3,formal.GetNode<Node2D>("Weapon").Position.Y-oldWeapon.Y,.001f,
                    "LANDING_LOAD_DRAW_RED: held weapon preserves action-relative attachment");
                TestAssert.Equal(rootPosition,formal.GlobalPosition,"landing absorption never moves the collision/root");
                TestAssert.Equal(new Vector2(facing,1),formal.Scale,"landing absorption preserves facing without root image squash");
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    var leg=formal.GetNode<PlayerFormalLayer>(name);
                    Vector2 wanted=name=="NearLeg"?gait.NearSoleWorld:gait.FarSoleWorld;
                    TestAssert.True(leg.ToGlobal(leg.LegSoleLocal).DistanceTo(wanted)<.02f,
                        "LANDING_LOAD_DRAW_RED: supported foot must stay planted while hips absorb");
                    TestAssert.Near(18,leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),.002f,"landing upper leg remains rigid");
                    TestAssert.Near(18,leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),.002f,"landing lower leg remains rigid");
                    TestAssert.True(!leg.LegReachClamped,"landing cannot hide an unreachable foot with a clamp");
                    TestAssert.Equal(Vector2.One,leg.Scale,"landing cannot stretch a leg image");
                    soles++;
                }
                foreach(var forbidden in new[]{PlayerLocomotionState.Dead,PlayerLocomotionState.Recovery})
                {
                    var terminal=baseline with { Locomotion=new(forbidden,1),PresentationTimeSeconds=0 };
                    Reset(facing);
                    formal.ApplyPlayerFrame(terminal);
                    var unloaded=formal.CurrentPose;
                    formal.ApplyPlayerFrame(terminal with { LandingLoad=new(true,3,.9f) });
                    TestAssert.Equal(unloaded,formal.CurrentPose,"retained impact cannot deform dead/recovery pose");
                }
                foreach(var noSupport in new[]{baseline with { Gait=default },
                    baseline with { Base=new EntityPresentationFrame(EntityVisualState.Dash,facing,false) },
                    baseline with { Gait=gait with { Weight=.5f } }})
                {
                    Reset(facing);
                    formal.ApplyPlayerFrame(noSupport);
                    var unloaded=formal.CurrentPose;
                    formal.ApplyPlayerFrame(noSupport with { LandingLoad=new(true,3,.9f) });
                    TestAssert.Equal(unloaded,formal.CurrentPose,"air/missing/partial support cannot sink feet with a retained load");
                }
                foreach(var landing in new[]{PlayerLocomotionState.SoftLand,PlayerLocomotionState.HardLand})
                {
                    Reset(facing);
                    var supportedLanding=Frame(landing,.5f,PlayerActionFrame.None,facing:facing) with
                        { Gait=gait,LandingLoad=new(true,3,.9f),PresentationTimeSeconds=0 };
                    formal.ApplyPlayerFrame(supportedLanding);
                    formal.ApplyPlayerFrame(supportedLanding with { PresentationTimeSeconds=.1 });
                    formal.ApplyPlayerFrame(supportedLanding with { PresentationTimeSeconds=.2 });
                    TestAssert.Near(3,formal.CurrentPose.Body.Position.Y,.001f,
                        "measured landing replaces fixed vertical template rather than doubling compression");
                }
                cases++;
            }
            GD.Print($"LANDING_LOAD_DRAW_OK cases={cases} soles={soles} dash=true action_attachments=true rigid_links=true root_unchanged=true headless_geometry=true human=false");
        });
    }

    public static async Task RunBodyLoadOnly(SceneTree tree)
    {
        await WithFormalFixture(tree,formal =>
        {
            var failures=new List<Exception>();
            void Check(Action test) { try { test(); } catch(Exception error) { failures.Add(error); } }
            PlayerPresentationFrame Timed(PlayerLocomotionState state,PlayerActionFrame action,double time) =>
                Frame(state,state==PlayerLocomotionState.Idle?0:1,action) with { PresentationTimeSeconds=time };
            void Reset()
            {
                formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Idle,1,true));
                for(int i=0;i<16;i++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle,0,PlayerActionFrame.None));
                formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,PlayerActionFrame.None,0));
            }
            Vector2[] Geometry() => Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal,formal.ToGlobal(new Vector2(0,-20)));
            void Frozen(PlayerPresentationFrame frame,string label)
            {
                formal.ApplyPlayerFrame(frame);
                PlayerPose pose=formal.CurrentPose;
                var geometry=Geometry();
                for(int i=0;i<8;i++) formal.ApplyPlayerFrame(frame);
                TestAssert.Equal(pose,formal.CurrentPose,"BODY_CLOCK_ZERO_RED: "+label+" cannot consume time from repeated publications");
                var after=Geometry();
                for(int i=0;i<geometry.Length;i++) TestAssert.True(geometry[i].DistanceTo(after[i])<.001f,
                    "BODY_CLOCK_DRAW_RED: "+label+" actual Draw geometry cannot consume zero time");
            }
            Check(() => { Reset(); Frozen(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,0),"locomotion entry"); });
            Check(() => { Reset();
            formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,new(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right),0));
            formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,new(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right),.1));
            Frozen(Timed(PlayerLocomotionState.Idle,PlayerActionFrame.None,.1),"action release and secondary hand");
            });
            foreach(bool release in new[]{false,true}) Check(() =>
            {
                Vector2[] referenceGeometry=null;
                foreach(int hz in new[]{30,60,120})
                {
                    Reset();
                    var action=release ? new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right) :
                        new PlayerActionFrame(PlayerActionState.ThreadThrow,PlayerActionPhase.Burst,1,false,Vector2.Right);
                    formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,action,0));
                    double start=0;
                    if(release)
                    {
                        formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,action,.1));
                        start=.1;
                        formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,PlayerActionFrame.None,start));
                    }
                    for(int i=1;i<=hz/10;i++) formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,
                        release?PlayerActionFrame.None:action,start+(double)i/hz));
                    var actual=Geometry();
                    if(referenceGeometry!=null)
                        for(int i=0;i<actual.Length;i++) TestAssert.True(actual[i].DistanceTo(referenceGeometry[i])<.002f,
                            "BODY_CLOCK_HAND_RATE_RED: actual release/thread hand geometry agrees at "+hz+"Hz");
                    referenceGeometry ??=actual;
                }
            });
            Check(() => { Reset(); Frozen(Timed(PlayerLocomotionState.Idle,new(PlayerActionState.ThreadThrow,PlayerActionPhase.Burst,1,false,Vector2.Right),0),"thread entry"); });
            Check(() =>
            {
                int cases = 0;
                float maximumEntryStep = 0f;
                foreach (int facing in new[] { 1, -1 })
                foreach (var outgoing in new[] { PlayerActionState.GroundAttack, PlayerActionState.Shield })
                foreach (var incoming in new[] { PlayerActionState.GroundAttack, PlayerActionState.ThreadWindup })
                {
                    Reset();
                    Vector2 aim = new(facing, 0);
                    var idle = Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing) with
                    {
                        PresentationTimeSeconds = 0,
                        Secondary = new(PoseTransform2D.Identity with { Position = new(1, 2), RotationRadians = .03f },
                            PoseTransform2D.Identity with { Position = new(2, -1) },
                            PoseTransform2D.Identity with { RotationRadians = .04f }, PoseTransform2D.Identity)
                    };
                    formal.ApplyPlayerFrame(idle);
                    var baseline = ReadActual();
                    var action = idle with { Action = new(outgoing, PlayerActionPhase.Contact, 1, false, aim),
                        PresentationTimeSeconds = .1 };
                    formal.ApplyPlayerFrame(action);
                    formal.ApplyPlayerFrame(action with { PresentationTimeSeconds = .2 });
                    formal.ApplyPlayerFrame(idle with { PresentationTimeSeconds = .2 });
                    var release = idle with { PresentationTimeSeconds = .2 + 1d / 12 };
                    formal.ApplyPlayerFrame(release);
                    var visible = ReadActual();
                    TestAssert.True(MaxStep(baseline, visible) > 1f,
                        "fixture must observe a real non-neutral departing pose, not a release that already ended");
                    var root = formal.GlobalTransform;
                    var next = release with { Action = new(incoming, PlayerActionPhase.Anticipation,
                        incoming == PlayerActionState.ThreadWindup ? 1 : 0, false, aim) };
                    formal.ApplyPlayerFrame(next);
                    float step = MaxStep(visible, ReadActual());
                    maximumEntryStep = Mathf.Max(maximumEntryStep, step);
                    TestAssert.True(step < .003f,
                        $"ACTION_RELEASE_REENTRY_RED: {outgoing}→{incoming} facing={facing} must start from the displayed release at zero time; actual={step}px");
                    formal.ApplyPlayerFrame(next);
                    TestAssert.True(MaxStep(visible, ReadActual()) < .003f,
                        "same timestamp cannot advance the interrupted release or new action");
                    TestAssert.Equal(root, formal.GlobalTransform, "action capture must not move the visual root");
                    TestAssert.Equal(next, formal.CurrentPlayerFrame, "visual carry may not revive the old action state");
                    formal.ApplyPlayerFrame(next with { Action = new(incoming, PlayerActionPhase.Anticipation, 1, false, aim),
                        PresentationTimeSeconds = .5 });
                    TestAssert.True(MaxStep(visible, ReadActual()) > .5f,
                        "new action visibly progresses instead of freezing the old pose");
                    formal.ApplyPlayerFrame(idle with { PresentationTimeSeconds = .5 });
                    formal.ApplyPlayerFrame(idle with { PresentationTimeSeconds = .8 });
                    var fullyReleased = ReadActual();
                    formal.ApplyPlayerFrame(next with { PresentationTimeSeconds = .8 });
                    TestAssert.True(MaxStep(fullyReleased, ReadActual()) < .003f,
                        "finished release must not replay stale action geometry on the next entry");
                    cases++;
                }
                GD.Print($"ACTION_RELEASE_REENTRY_OK cases={cases} max_step={maximumEntryStep} same_clock=true actual_wrist_elbow=true partial_release=true finished_release=true bounded_entry=true gameplay_changed=false human=false");

                Vector2[] ReadActual()
                {
                    var points = new List<Vector2>(Geometry());
                    foreach (string name in Layers)
                    {
                        var node = formal.GetNode<Node2D>(name);
                        points.Add(node.GlobalPosition); points.Add(node.ToGlobal(Vector2.Right));
                    }
                    foreach (string name in new[] { "NearArm", "FarArm" })
                    {
                        var arm = formal.GetNode<PlayerFormalLayer>(name);
                        points.Add(arm.ToGlobal(arm.ArmWristLocal)); points.Add(arm.ToGlobal(arm.ArmElbowLocal));
                    }
                    return points.ToArray();
                }
                static float MaxStep(Vector2[] from, Vector2[] to)
                {
                    float step = 0;
                    for (int i = 0; i < from.Length; i++) step = Mathf.Max(step, from[i].DistanceTo(to[i]));
                    return step;
                }
            });
            Check(() => {
            PlayerPose? reference=null;
            foreach(int hz in new[]{30,60,120})
            {
                Reset(); formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,0));
                for(int i=1;i<=hz/10;i++) formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,(double)i/hz));
                TestAssert.True(formal.CurrentPose.Body.Position.Y>.2f,"timed handoff actually moves body, not an always-frozen false green");
                if(reference is {} expected)
                {
                    var a=SlotTransforms(expected); var b=SlotTransforms(formal.CurrentPose);
                    for(int i=0;i<a.Length;i++) AssertTransformNear(a[i],b[i],"BODY_CLOCK_RATE_RED: elapsed-rate invariant slot "+Layers[i]);
                }
                reference ??=formal.CurrentPose;
            }
            });
            Check(() => {
                Reset(); formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,0));
                formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,.05));
                PlayerPose before=formal.CurrentPose;
                foreach(double time in new[]{double.NaN,-1d,.02d})
                {
                    formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,time));
                    TestAssert.Equal(before,formal.CurrentPose,"BODY_CLOCK_INVALID_RED: invalid/backwards time freezes handoff");
                }
                formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Brake,PlayerActionFrame.None,.03));
                TestAssert.True(formal.CurrentPose.Body.Position.Y>before.Body.Position.Y,"valid new epoch time resumes progress");
            });
            Check(() => {
            Reset();
            var leg=formal.GetNode<PlayerFormalLayer>("NearLeg");
            var near=formal.GetNode<PlayerFormalLayer>("NearArm");
            var shoulder=formal.GetNode<Node2D>("ShoulderCore");
            Vector2 originalLeg=leg.Position, originalArm=near.Position, originalCore=shoulder.Position;
            float distance=originalArm.DistanceTo(originalCore);
            formal.ApplyPlayerFrame(Timed(PlayerLocomotionState.Idle,PlayerActionFrame.None,.1) with { BodyLoad=new(true,1,.08f) });
            TestAssert.True(formal.CurrentPose.Body.Position.X>.5f && leg.Position.Y-originalLeg.Y>.9f,
                "BODY_LOAD_DRAW_RED: real body and hip respond without translating whole character root");
            TestAssert.Near(distance,near.Position.DistanceTo(shoulder.Position),.001f,"torso group preserves authored attachment distances");
            TestAssert.Equal(Vector2.One,leg.Scale,"body load does not stretch legs");
            TestAssert.Equal(Vector2.One,formal.Scale,"body load does not stretch root");
            });
            Check(() =>
            {
                Reset();
                var attack=Timed(PlayerLocomotionState.Idle,new(PlayerActionState.GroundAttack,PlayerActionPhase.Contact,1,false,Vector2.Right),0);
                formal.ApplyPlayerFrame(attack);
                var body=formal.GetNode<Node2D>("Body");
                var owned=Geometry();
                for(int i=3;i<owned.Length;i++) owned[i]=body.ToLocal(owned[i]);
                Reset(); formal.ApplyPlayerFrame(attack with { BodyLoad=new(true,1,.08f) });
                var loaded=Geometry();
                for(int i=3;i<loaded.Length;i++) TestAssert.True(body.ToLocal(loaded[i]).DistanceTo(owned[i])<.002f,
                    "body load preserves actual authored hand/weapon action relative to torso");
                foreach(var state in new[]{PlayerLocomotionState.Dash,PlayerLocomotionState.Dead,PlayerLocomotionState.Recovery})
                {
                    Reset(); formal.ApplyPlayerFrame(Timed(state,PlayerActionFrame.None,0));
                    PlayerPose unloaded=formal.CurrentPose;
                    Reset(); formal.ApplyPlayerFrame(Timed(state,PlayerActionFrame.None,0) with { BodyLoad=new(true,1,.08f) });
                    TestAssert.Equal(unloaded,formal.CurrentPose,"retained body frame cannot deform dash/death/recovery");
                }
            });
            Check(() =>
            {
                int cases=0;
                var attachmentFaults=new List<string>();
                foreach(int facing in new[]{1,-1})
                foreach(var action in new[]{new PlayerActionFrame(PlayerActionState.GroundAttack,PlayerActionPhase.Contact,1,false,Vector2.Right),
                    new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Recovery,.5f,true,Vector2.Right)})
                {
                    var actionFrame=Timed(PlayerLocomotionState.Idle,action,.1) with { Base=new(EntityVisualState.Idle,facing,true) };
                    Vector2[] Anchors()
                    {
                        var body=formal.GetNode<Node2D>("Body");
                        var result=new List<Vector2>();
                        foreach(string name in new[]{"ShoulderCore","Mantle","Lantern","Tether"})
                        {
                            var node=formal.GetNode<Node2D>(name);
                            result.Add(body.ToLocal(node.GlobalPosition));
                            result.Add(body.ToLocal(node.ToGlobal(Vector2.Right)));
                        }
                        return result.ToArray();
                    }
                    Reset(); formal.ApplyPlayerFrame(actionFrame);
                    var expected=Anchors();
                    Reset(); formal.ApplyPlayerFrame(actionFrame with { BodyLoad=new(true,1,.08f) });
                    var actual=Anchors();
                    for(int i=0;i<actual.Length;i++) if(actual[i].DistanceTo(expected[i])>=.002f)
                        attachmentFaults.Add($"anchor{i}/face{facing}/{action.State}/{action.Phase}");
                    cases++;
                }
                TestAssert.True(attachmentFaults.Count==0,"BODY_LOAD_REVIEW_I1_RED: "+string.Join(",",attachmentFaults));
                GD.Print($"BODY_LOAD_ATTACHMENTS_OK cases={cases} anchors=4 axes=true full_partial=true facings=2 actual_nodes=true");
            });
            if(failures.Count>0) throw new AggregateException("BODY_LOAD_DRAW_RED",failures);
            GD.Print("BODY_LOAD_DRAW_OK timed_replay_frozen=true rates=30,60,120 actual_geometry=true torso_hip=true pixels=false human=false");
        });
        await GroundAttackWeightTransferControls(tree);
    }

    // A missing common hip translation must fail even though the old Body already rotates.
    // These are developer sample bounds, not an assertion of approved character art.
    private static async Task GroundAttackWeightTransferControls(SceneTree tree)
    {
        await WithFormalFixture(tree, formal =>
        {
            var faults = new List<Exception>();
            void Check(Action test) { try { test(); } catch (Exception error) { faults.Add(error); } }
            var leg = formal.GetNode<PlayerFormalLayer>("NearLeg");
            Vector2 Hip() => formal.ToLocal(leg.ToGlobal(leg.LegHipLocal));
            PlayerPresentationFrame Sample(int facing, PlayerActionPhase phase, float progress = 1,
                Vector2? aim = null) => Frame(PlayerLocomotionState.Idle, 0,
                    new(PlayerActionState.GroundAttack, phase, progress, false, aim ?? new Vector2(facing, 0)), facing: facing)
                with { Gait = new(true, 1, 0, new(8 * facing, 36), new(-8 * facing, 36), true, true), PresentationTimeSeconds = 0 };
            void Reset(int facing)
            {
                formal.PlayCue(PresentationCue.Died);
                for (int i = 0; i < 24; i++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing));
                formal.GlobalPosition = Vector2.Zero;
                formal.ApplyPlayerFrame(Sample(facing, PlayerActionPhase.Anticipation) with { Action = PlayerActionFrame.None });
            }
            void Hold(PlayerPresentationFrame frame, int hz = 60, double start = 0)
            {
                formal.ApplyPlayerFrame(frame with { PresentationTimeSeconds = start });
                for (int tick = 1; tick <= hz / 5; tick++)
                    formal.ApplyPlayerFrame(frame with { PresentationTimeSeconds = start + (double)tick / hz });
            }
            int phaseCases = 0;
            foreach (int facing in new[] { 1, -1 }) Check(() =>
            {
                Reset(facing);
                Vector2 restHip = Hip(), restBody = formal.GetNode<Node2D>("Body").Position;
                var offsets = new List<Vector2>();
                int phaseIndex = 0;
                foreach (var phase in new[] { PlayerActionPhase.Anticipation, PlayerActionPhase.Burst,
                    PlayerActionPhase.Contact, PlayerActionPhase.Recovery })
                {
                    var frame = Sample(facing, phase);
                    Hold(frame, start: phaseIndex++ * .2);
                    Vector2 offset = Hip() - restHip;
                    offsets.Add(offset);
                    TestAssert.True((formal.GetNode<Node2D>("Body").Position - restBody).DistanceTo(offset) < .002f,
                        "GROUND_ATTACK_WEIGHT_RED Body and actual hip share translation, not only old body rotation");
                    AssertGroundAttackWeightRig(formal, frame, Vector2.Zero);
                    phaseCases++;
                }
                GD.Print($"GROUND_ATTACK_WEIGHT_MEASURE facing={facing} anticipation={offsets[0]} burst={offsets[1]} contact={offsets[2]} recovery={offsets[3]} actual_hip=true old_rotation_excluded=true");
                TestAssert.True(offsets[0].X < -.25f && offsets[1].X > .25f &&
                    offsets[0].Y > .5f && offsets[1].Y > .5f && offsets[1].Y <= 2.2f &&
                    offsets[2].Y > .25f && offsets[3].Length() < .02f,
                    "GROUND_ATTACK_WEIGHT_RED actual supported hip must wind back, drive forward/down, pass and recover; old rotation is insufficient");
            });
            foreach (int facing in new[] { 1, -1 }) Check(() =>
            {
                Reset(facing); Vector2 rest = Hip();
                Hold(Sample(facing, PlayerActionPhase.Burst, aim: Vector2.Up));
                Vector2 up = Hip() - rest;
                TestAssert.True(Mathf.Abs(up.X) < .002f && up.Y > .5f && up.Y <= 2.2f,
                    "GROUND_ATTACK_WEIGHT_UP_RED vertical locked aim compresses without a horizontal drive");
                Reset(facing); rest = Hip();
                Hold(Sample(facing, PlayerActionPhase.Burst, aim: new Vector2(-facing, 0)));
                TestAssert.True((Hip() - rest).X < -.25f,
                    "GROUND_ATTACK_WEIGHT_AIM_RED locked backward action aim, not facing alone, owns the drive");
            });
            Check(() =>
            {
                Reset(1); Vector2 rest = Hip(); var frame = Sample(1, PlayerActionPhase.Burst); Hold(frame);
                Vector2 before = leg.ToGlobal(leg.LegHipLocal) - formal.ToGlobal(rest);
                var flipped = frame with { Base = frame.Base with { Facing = -1 }, PresentationTimeSeconds = .2 };
                formal.ApplyPlayerFrame(flipped);
                Vector2 after = leg.ToGlobal(leg.LegHipLocal) - formal.ToGlobal(rest);
                TestAssert.True(before.X > .25f && after.DistanceTo(before) < .002f,
                    "GROUND_ATTACK_WEIGHT_FACING_RED same-time facing mirror cannot reverse the existing world-directed hip load");
                for (int i = 1; i <= 12; i++) formal.ApplyPlayerFrame(flipped with { PresentationTimeSeconds = .2 + i / 60d });
                TestAssert.True((leg.ToGlobal(leg.LegHipLocal) - formal.ToGlobal(rest)).X > .25f,
                    "locked world aim keeps forward world hip drive after facing changes");
                AssertGroundAttackWeightRig(formal, formal.CurrentPlayerFrame, Vector2.Zero);
            });
            Check(() =>
            {
                Vector2? reference = null;
                foreach (int hz in new[] { 30, 60, 120 })
                {
                    Reset(1); Vector2 rest = Hip(); var frame = Sample(1, PlayerActionPhase.Burst);
                    Hold(frame, hz);
                    Vector2 offset = Hip() - rest;
                    TestAssert.True(offset.X > .25f && offset.Y > .5f, "GROUND_ATTACK_WEIGHT_RATE_RED held target is not vacuously frozen");
                    if (reference is { } expected) TestAssert.True(offset.DistanceTo(expected) < .002f,
                        "same fixed target and elapsed endpoint agree at 30/60/120Hz");
                    reference ??= offset;
                    Vector2 before = Hip();
                    for (int repeat = 0; repeat < 8; repeat++) formal.ApplyPlayerFrame(frame with
                        { Action = new(PlayerActionState.GroundAttack, PlayerActionPhase.Anticipation, 1, false, Vector2.Right), PresentationTimeSeconds = .2 });
                    TestAssert.True(Hip().DistanceTo(before) < .001f, "zero elapsed target changes cannot consume hip transition time");
                    formal.ApplyPlayerFrame(frame with { PresentationTimeSeconds = .1 });
                    TestAssert.True(Hip().DistanceTo(before) < .001f, "backward clock cannot advance hip tracking");
                    formal.ApplyPlayerFrame(frame with { PresentationTimeSeconds = .1 + 1d / hz });
                    AssertGroundAttackWeightRig(formal, formal.CurrentPlayerFrame, Vector2.Zero);
                }
            });
            foreach (var state in new[] { PlayerActionState.None, PlayerActionState.Hurt, PlayerActionState.Shield, PlayerActionState.ThreadThrow }) Check(() =>
            {
                Reset(1); Vector2 rest = Hip(); var frame = Sample(1, PlayerActionPhase.Burst); Hold(frame);
                Vector2 before = Hip();
                var cancel = frame with { Action = state == PlayerActionState.None ? PlayerActionFrame.None :
                    new(state, PlayerActionPhase.Contact, 1, false, Vector2.Right), PresentationTimeSeconds = .2 };
                formal.ApplyPlayerFrame(cancel);
                TestAssert.True(Hip().DistanceTo(before) < .001f, "reliable support zero-time cancellation keeps continuous hip position: " + state);
                formal.ApplyPlayerFrame(cancel with { PresentationTimeSeconds = .2 + 1d / 60 });
                TestAssert.True(Hip().DistanceTo(rest) > .1f && Hip().DistanceTo(rest) < before.DistanceTo(rest),
                    "GROUND_ATTACK_WEIGHT_CANCEL_RED supported cancellation must decay, not snap or retain load: " + state);
                for (int i = 2; i <= 30; i++) formal.ApplyPlayerFrame(cancel with { PresentationTimeSeconds = .2 + i / 60d });
                TestAssert.True(Hip().DistanceTo(rest) < .002f, "supported interruption settles the hip: " + state);
            });
            Check(() =>
            {
                var loaded = Sample(1, PlayerActionPhase.Burst) with { BodyLoad = new(true, 1.1f, .08f) };
                // Same phase/history/load, but no measured support: BodyLoad still
                // applies while the new ground-attack source must be ineligible.
                Reset(1); Hold(loaded with { Gait = default });
                Vector2 baselineHip = Hip();
                var baseline = new Dictionary<string, Vector2>();
                foreach (string slot in Layers) baseline.Add(slot, formal.GetNode<Node2D>(slot).Position);
                Reset(1); Hold(loaded);
                Vector2 hipDelta = Hip() - baselineHip;
                TestAssert.True(hipDelta.X > .25f && hipDelta.Y > .5f,
                    "GROUND_ATTACK_WEIGHT_BODY_LOAD_OVERLAP_RED actual supported hip must drive while existing measured body load remains active");
                foreach (string slot in Layers)
                    TestAssert.True((formal.GetNode<Node2D>(slot).Position - baseline[slot]).DistanceTo(hipDelta) < .002f,
                        "GROUND_ATTACK_WEIGHT_BODY_LOAD_OVERLAP_RED old BodyLoad must not rotate the new common translation away from the actual hip: " + slot);
                AssertGroundAttackWeightRig(formal, formal.CurrentPlayerFrame, Vector2.Zero);
            });
            Check(() =>
            {
                var frame = Sample(1, PlayerActionPhase.Burst);
                var invalid = new[] {
                    frame with { Gait = default }, frame with { Gait = frame.Gait with { Weight = .5f } },
                    frame with { Base = frame.Base with { Grounded = false } },
                    frame with { Gait = frame.Gait with { NearSoleWorld = new(float.NaN, 36) } },
                    frame with { Gait = frame.Gait with { FarSoleWorld = new(0, float.PositiveInfinity) } } };
                foreach (var rejected in invalid)
                {
                    Reset(1); Vector2 rest = Hip(); Hold(frame);
                    formal.ApplyPlayerFrame(rejected with { PresentationTimeSeconds = .2 });
                    TestAssert.True(Hip().IsFinite() && Hip().DistanceTo(rest) < .002f,
                        "GROUND_ATTACK_WEIGHT_SUPPORT_RED lost/partial/nonfinite support immediately clears ground hip load");
                }
                foreach (var terminal in new[] { PlayerLocomotionState.Dash, PlayerLocomotionState.Dead, PlayerLocomotionState.Recovery })
                {
                    Reset(1); Hold(frame);
                    var rejected = frame with { Locomotion = new(terminal, 1), PresentationTimeSeconds = .2 };
                    formal.ApplyPlayerFrame(rejected); Vector2 carried = Hip();
                    Reset(1); Hold(frame with { Gait = default });
                    formal.ApplyPlayerFrame(rejected); Vector2 unloaded = Hip();
                    TestAssert.True(carried.DistanceTo(unloaded) < .002f,
                        "terminal/dash entry has no carried attack translation versus same locomotion history without support: " + terminal);
                }
                Reset(1); Vector2 baseline = Hip(); Hold(frame);
                formal.PlayCue(PresentationCue.Hurt);
                TestAssert.True(Hip().DistanceTo(baseline) < .002f, "explicit reset clears attack hip source immediately");
            });
            if (faults.Count > 0) throw new AggregateException("GROUND_ATTACK_WEIGHT_DRAW_RED", faults);
            GD.Print($"GROUND_ATTACK_WEIGHT_DRAW_OK phase_cases={phaseCases} actual_hip=true body=true planted_soles=true fixed_links=true grips=true rates=30,60,120 zero_clock=true cancel=true invalid_support=true reset=true developer_sample=true human=false");
        });
    }

    private static void AssertGroundAttackWeightRig(PlayerFormalVisual formal, PlayerPresentationFrame frame, Vector2 root)
    {
        TestAssert.Equal(root, formal.GlobalPosition, "ground attack weight never moves formal/collision root");
        foreach (string slot in Layers) TestAssert.Equal(Vector2.One, formal.GetNode<Node2D>(slot).Scale, "weight transfer cannot stretch " + slot);
        foreach (string name in new[] { "NearLeg", "FarLeg" })
        {
            var leg = formal.GetNode<PlayerFormalLayer>(name);
            Vector2 wanted = name == "NearLeg" ? frame.Gait.NearSoleWorld : frame.Gait.FarSoleWorld;
            TestAssert.True(leg.ToGlobal(leg.LegSoleLocal).DistanceTo(wanted) < .01f, "ground attack actual Draw sole stays planted: " + name);
            TestAssert.Near(18, leg.LegHipLocal.DistanceTo(leg.LegKneeLocal), .002f, "attack upper leg remains rigid");
            TestAssert.Near(18, leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal), .002f, "attack lower leg remains rigid");
            TestAssert.True(!leg.LegReachClamped, "weight transfer cannot conceal unreachable soles");
        }
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        AssertArmChain(near); AssertArmChain(far);
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        Vector2 Grip(float x) => weapon.ToGlobal(weapon.WeaponDrawRect.Position +
            (new Vector2(x, 798) - weapon.WeaponSourceRect.Position) * (25f / 287f));
        TestAssert.True(Grip(835).DistanceTo(near.ToGlobal(near.ArmWristLocal)) < .002f &&
            Grip(548).DistanceTo(far.ToGlobal(far.ArmWristLocal)) < .002f && weapon.WeaponSecondaryGripContact,
            "ground hip drive retains actual primary and secondary weapon grips");
    }

    public static async Task RunGroundGaitReviewContracts(SceneTree tree)
    {
        var errors=new List<Exception>();
        async Task Check(Func<Task> test)
        { try { await test(); } catch(Exception error) { errors.Add(error); } }
        await Check(() =>
        {
            var model=new PlayerGroundGait();
            var sample=new PlayerGroundSample(true,Vector2.Zero,101,Transform2D.Identity,new(0,36),Vector2.Up)
                { HasBounds=true,SupportBounds=new(-20,36,40,10) };
            model.Step(sample,true,1f/60);
            var shrunk=model.Step(sample with { SupportBounds=new(-6,36,12,10) },true,1f/60);
            TestAssert.True((!shrunk.NearStance || Mathf.Abs(shrunk.NearSoleWorld.X)<=4.001f) &&
                (!shrunk.FarStance || Mathf.Abs(shrunk.FarSoleWorld.X)<=4.001f),
                "GROUND_GAIT_REVIEW_I3_RED: same support shrink revalidates already planted soles");
            return Task.CompletedTask;
        });
        await Check(() => WithFormalFixture(tree,formal =>
        {
            for(int i=0;i<12;i++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle,0,PlayerActionFrame.None));
            for(int legIndex=0;legIndex<2;legIndex++)
            {
                var leg=formal.GetNode<PlayerFormalLayer>(legIndex==0?"NearLeg":"FarLeg");
                Vector2 heading=formal.ToGlobal(new(0,-20));
                var before=Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal,heading);
                Vector2 original=leg.LegSoleLocal;
                Transform2D matrix=leg.Transform;
                PlayerPose pose=formal.CurrentPose;
                try
                {
                    leg.SetLegSoleLocal(new(6,25),Vector2.Up);
                    var after=Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal,heading);
                    TestAssert.True(after.Length==15 && after[legIndex].DistanceTo(leg.ToGlobal(new(6,25)))<.001f &&
                        after[legIndex].DistanceTo(before[legIndex])>1,
                        "GROUND_GAIT_REVIEW_I4_RED: actual sole Draw-only change reaches the old observer foot slot");
                    TestAssert.True(leg.Transform==matrix && formal.CurrentPose==pose,"foot Draw mutation leaves slot matrix and typed pose unchanged");
                    for(int i=0;i<15;i++) if(i!=legIndex) TestAssert.True(after[i].DistanceTo(before[i])<.001f,"other14 observer entries stay unchanged");
                }
                finally { leg.SetLegSoleLocal(original,Vector2.Up); }
            }
        }));
        await Check(() => GroundGaitZeroTimeClawReleaseAfterMotor(tree));
        await Check(() => GroundGaitRejectsCompoundPolygon(tree));
        if(errors.Count>0) throw new AggregateException("GROUND_GAIT_REVIEW_RED",errors);
        GD.Print("GROUND_GAIT_REVIEW_OK issues=4 real_claw_idle=true post_motor=true controlled_post_motor_publish=true compound_polygon=true changed_bounds=true draw_observer=true pixels=false");
    }

    private static async Task GroundGaitZeroTimeClawReleaseAfterMotor(SceneTree tree)
    {
        ReleaseInput();
        var root=new Node2D { Name="GroundGaitZeroEventFixture" };
        var floor=new StaticBody2D { Position=new(0,50),CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new(1600,20) } });
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        var probe=new BasinBoardingTickObserver { ProcessPhysicsPriority=100 };
        root.AddChild(floor); root.AddChild(player); root.AddChild(probe); tree.Root.AddChild(root);
        ClawHarness claw=null;
        try
        {
            await Frames(tree,30);
            claw=SuspendMechanicalClaw(player);
            await FailClawWithoutTarget(tree,claw);
            SetInputFrame(claw.Input,new PlayerInputFrame(1,Vector2.Zero,false,false,false,false,false));
            await Frames(tree,8); // Establish real free-motion momentum before Reset clears the sampled input.
            var coordinator=player.GetNode<PlayerPresentationCoordinator>("Presentation");
            FieldInfo cached=typeof(PlayerPresentationCoordinator).GetField("_groundSample",BindingFlags.Instance|BindingFlags.NonPublic);
            bool armed=false,captured=false;
            PlayerGroundSample observed=default;
            Vector2 actual=default;
            ulong capturedPhysics=0;
            Exception probeFault=null;
            probe.Sample=delta =>
            {
                if(!armed || captured) return;
                try
                {
                    coordinator._Process(delta); // Native post-Motor priority, production publication path.
                    observed=(PlayerGroundSample)cached.GetValue(coordinator);
                    actual=player.GlobalPosition; capturedPhysics=Engine.GetPhysicsFrames(); captured=true;
                }
                catch(Exception error) { probeFault=error; captured=true; }
            };
            await Frames(tree,1); // Real physics boundary before Motor, like priority -100 Claw.
            Vector2 before=player.GlobalPosition;
            Vector2 beforeVelocity=player.Velocity;
            ulong eventPhysics=Engine.GetPhysicsFrames();
            claw.Controller.ResetTransientState(); // Real Failed -> Idle -> PresentCurrent(0).
            armed=true;
            await Frames(tree,2);
            if(probeFault!=null) throw new InvalidOperationException("native post-Motor probe fault",probeFault);
            TestAssert.True(captured && capturedPhysics==eventPhysics && actual.X-before.X>.1f,
                $"GROUND_GAIT_REVIEW_I1_WITNESS_RED: actual Claw Idle and later Motor move share a physics tick captured={captured} " +
                $"event={eventPhysics} post={capturedPhysics} before={before} before_velocity={beforeVelocity} after={actual} input={claw.Input.CurrentFrame.MoveAxis:R} sample={observed.RootWorld}");
            TestAssert.True(observed.PhysicsTick==capturedPhysics && observed.RootWorld.DistanceTo(actual)<.001f,
                $"GROUND_GAIT_REVIEW_I1_RED: zero-time Claw event cannot freeze pre-Motor geometry observed={observed.RootWorld} actual={actual}");
        }
        finally
        {
            probe.SetPhysicsProcess(false); probe.Sample=null;
            if(claw!=null) SetInputFrame(claw.Input,ClawInput());
            ReleaseInput(); await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);
        }
    }

    private static async Task GroundGaitRejectsCompoundPolygon(SceneTree tree)
    {
        var root=new Node2D { Name="GroundGaitCompoundFixture" };
        var floor=new StaticBody2D { CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Position=new(500,50),Shape=new RectangleShape2D { Size=new(100,20) } });
        floor.AddChild(new CollisionPolygon2D { Polygon=[new(-100,40),new(100,40),new(100,60),new(-100,60)] });
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        root.AddChild(floor); root.AddChild(player); tree.Root.AddChild(root);
        try
        {
            await Frames(tree,35);
            TestAssert.True(player.IsOnFloor() && Mathf.Abs(player.GlobalPosition.X)<1,"compound fixture really stands on polygon, not far rectangle");
            var gait=player.GetNode<PlayerFormalVisual>("VisualRoot/Formal").CurrentPlayerFrame.Gait;
            TestAssert.True(gait.Weight==0 && !gait.NearStance && !gait.FarStance,
                "GROUND_GAIT_REVIEW_I2_RED: polygon contact is not paired with an unrelated rectangle footprint");
        }
        finally { ReleaseInput(); await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root); }
    }

    private static async Task RealCoordinatorPublishesMeasuredGroundGait(SceneTree tree)
    {
        ReleaseInput();
        var root=new Node2D { Name="GroundGaitNativeFixture" };
        var floor=new StaticBody2D { Position=new(0,50),CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new(1600,20) } });
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        root.AddChild(floor); root.AddChild(player); tree.Root.AddChild(root);
        try
        {
            await Frames(tree,30);
            var formal=player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var near=formal.GetNode<PlayerFormalLayer>("NearLeg");
            var far=formal.GetNode<PlayerFormalLayer>("FarLeg");
            int observed=0, supported=0, loads=0, forward=0, caught=0;
            Vector2 start=player.GlobalPosition;
            Input.ActionPress(InputActions.MoveRight);
            for(int i=0;i<60;i++)
            {
                await Frames(tree,1);
                var gait=formal.CurrentPlayerFrame.Gait;
                var load=formal.CurrentPlayerFrame.BodyLoad;
                if(load.Observed) { loads++; if(load.TorsoLeanRadians>.005f) forward++; }
                if(!gait.Observed || gait.Weight<.999f) continue;
                observed++;
                if(gait.NearStance)
                { TestAssert.True(near.ToGlobal(near.LegSoleLocal).DistanceTo(gait.NearSoleWorld)<.05f,"GROUND_GAIT_BINDING_RED: live near support reaches Draw"); supported++; }
                if(gait.FarStance)
                { TestAssert.True(far.ToGlobal(far.LegSoleLocal).DistanceTo(gait.FarSoleWorld)<.05f,"GROUND_GAIT_BINDING_RED: live far support reaches Draw"); supported++; }
            }
            TestAssert.True(observed>30 && supported>30 && player.GlobalPosition.X-start.X>100,
                "GROUND_GAIT_BINDING_RED: real Motor and Coordinator must publish measured support nonvacuously");
            Input.ActionRelease(InputActions.MoveRight);
            for(int i=0;i<24;i++)
            {
                await Frames(tree,1);
                if(formal.CurrentPlayerFrame.BodyLoad is { Observed:true,TorsoLeanRadians:<-.005f }) caught++;
            }
            TestAssert.True(loads>30 && forward>0 && caught>0,"BODY_LOAD_BINDING_RED: real native acceleration and passive catch reach the final renderer");
            GD.Print($"BODY_LOAD_BINDING_OK observed={loads} forward={forward} passive_catch={caught} actual_motor=true action_input=true keyboard_route=false human=false");
            GD.Print($"GROUND_GAIT_BINDING_OK observed={observed} supports={supported} native_motor=true action_input=true keyboard_route=false pixels=false human=false");
        }
        finally { ReleaseInput(); await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root); }
    }

    private static void GroundGaitDrawControls(PlayerFormalVisual formal)
    {
        var near=formal.GetNode<PlayerFormalLayer>("NearLeg");
        var far=formal.GetNode<PlayerFormalLayer>("FarLeg");
        var gait=new PlayerGroundGait();
        PlayerGroundGaitFrame last=default;
        int samples=0, supports=0;
        for(int i=0;i<120;i++)
        {
            formal.GlobalPosition=new(i,0);
            var frame=gait.Step(new PlayerGroundSample(true,formal.GlobalPosition,101,Transform2D.Identity,new(0,36),Vector2.Up)
                { HasBounds=true,SupportBounds=new(-10000,36,20000,20) },true,1f/60);
            var action=i is >=20 and <40 ? new PlayerActionFrame(PlayerActionState.GroundAttack,PlayerActionPhase.Contact,.55f,false) :
                i is >=40 and <60 ? new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,.55f,false) :
                i is >=60 and <80 ? new PlayerActionFrame(PlayerActionState.ThreadThrow,PlayerActionPhase.Burst,.55f,false,Vector2.Right) : PlayerActionFrame.None;
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run,.2f,action,facing:i<60?1:-1) with { Gait=frame,MobilityLeanRadians=.18f });
            last=frame;
            foreach(var leg in new[]{near,far})
            {
                TestAssert.Near(18,leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),.002f,"GROUND_GAIT_DRAW_RED: fixed upper leg");
                TestAssert.Near(18,leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),.002f,"GROUND_GAIT_DRAW_RED: fixed lower leg");
                TestAssert.True(!leg.LegReachClamped,"GROUND_GAIT_DRAW_RED: normal stance or swing must be reachable");
                TestAssert.Equal(Vector2.One,leg.Scale,"gait cannot stretch leg node");
            }
            if(frame.NearStance)
            { TestAssert.True(near.ToGlobal(near.LegSoleLocal).DistanceTo(frame.NearSoleWorld)<.01f,"GROUND_GAIT_DRAW_RED: actual near sole is planted"); supports++; }
            if(frame.FarStance)
            { TestAssert.True(far.ToGlobal(far.LegSoleLocal).DistanceTo(frame.FarSoleWorld)<.01f,"GROUND_GAIT_DRAW_RED: actual far sole is planted"); supports++; }
            samples++;
        }
        TestAssert.True(supports>100,"actual rendered support observations are nonvacuous");
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Dead,.65f,PlayerActionFrame.None) with { Gait=last });
        TestAssert.True(near.LegSoleLocal.DistanceTo(near.FreeLegSoleLocal)<.001f && far.LegSoleLocal.DistanceTo(far.FreeLegSoleLocal)<.001f,
            "GROUND_GAIT_DRAW_RED: death releases a retained grounded frame even through compatibility cues");
        foreach(int hz in new[]{30,60,120})
        {
            var rateModel=new PlayerGroundGait();
            int rateSupports=0;
            for(int i=0;i<hz*2;i++)
            {
                formal.GlobalPosition=new(i*260f/hz,0);
                var measured=rateModel.Step(new PlayerGroundSample(true,formal.GlobalPosition,101,Transform2D.Identity,new(0,36),Vector2.Up)
                    { HasBounds=true,SupportBounds=new(-10000,36,20000,20) },true,1f/hz);
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run,.2f,PlayerActionFrame.None,facing:i<hz?1:-1)
                    with { Gait=measured,MobilityLeanRadians=.18f });
                foreach(var leg in new[]{near,far})
                {
                    TestAssert.Near(18,leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),.002f,$"actual upper link at {hz}Hz");
                    TestAssert.Near(18,leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),.002f,$"actual lower link at {hz}Hz");
                    TestAssert.True(!leg.LegReachClamped,$"no hidden stance clamp at {hz}Hz");
                }
                if(measured.NearStance)
                { TestAssert.True(near.ToGlobal(near.LegSoleLocal).DistanceTo(measured.NearSoleWorld)<.02f,$"actual near planted sole at {hz}Hz"); rateSupports++; }
                if(measured.FarStance)
                { TestAssert.True(far.ToGlobal(far.LegSoleLocal).DistanceTo(measured.FarSoleWorld)<.02f,$"actual far planted sole at {hz}Hz"); rateSupports++; }
                samples++;
            }
            TestAssert.True(rateSupports>hz,$"nonvacuous actual rendered support at {hz}Hz");
            supports+=rateSupports;
        }
        GD.Print($"GROUND_GAIT_DRAW_OK samples={samples} supports={supports} articulated=true rates=30,60,120 pixels=false human=false");
    }

    public static async Task RunCurrentClawPhasesOnly(SceneTree tree)
    {
        await ApprovedWeaponFollowsActualHand(tree);
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(PlayerFormalVisualTests) + "." + nameof(RealCoordinatorKeepsFormalThreadCastThroughEveryPhase),
            nameof(PlayerFormalVisualTests) + "." + nameof(RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode),
            nameof(PlayerFormalVisualTests) + "." + nameof(RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase)
        };
        var selected = new List<Task7Case>();
        foreach (Task7Case item in Task7Cases(tree))
            if (names.Contains(item.Name)) selected.Add(item);
        TestAssert.Equal(3, selected.Count, "claw phase probe selects exactly the original registered cases");
        Task7CaseRun run = await Task7CaseRegistry.Execute(selected);
        TestAssert.Equal(3, run.Completed.Count, "original registered claw phase delegates complete once");
        GD.Print("THRUSTER_FORMAL_CLAW_PHASES_OK original_registry=true automatic_reel=true explicit_cancel=true human=false");
    }

    internal static IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Case(nameof(SetupAndBasicContract), () => SetupAndBasicContract(tree)),
        FormalCase(tree, nameof(TypedActionPhaseBoundaryBlendsWithoutHardCut),
            TypedActionPhaseBoundaryBlendsWithoutHardCut),
        FormalCase(tree, nameof(AirAndDownAttacksExposeFourDistinctRigStages),
            AirAndDownAttacksExposeFourDistinctRigStages),
        FormalCase(tree, nameof(ThreadTerminalPoseReturnsContinuouslyToIdle),
            ThreadTerminalPoseReturnsContinuouslyToIdle),
        FormalCase(tree, nameof(ThreadAimUsesFacingLocalMirroredRig),
            ThreadAimUsesFacingLocalMirroredRig),
        FormalCase(tree, nameof(ActionOverRunKeepsLegsAndSecondarySlotsContinuous),
            ActionOverRunKeepsLegsAndSecondarySlotsContinuous),
        FormalCase(tree, nameof(LoopWrapKeepsEveryLocomotionSlotContinuous),
            LoopWrapKeepsEveryLocomotionSlotContinuous),
        FormalCase(tree, nameof(TaskOneSecondaryOutputsReachTheirOwnedFormalSlots),
            TaskOneSecondaryOutputsReachTheirOwnedFormalSlots),
        FormalCase(tree, nameof(CompatibilityAdaptersRemainVisible),
            CompatibilityAdaptersRemainVisible),
        FormalCase(tree, nameof(SixtyFpsFramesKeepEverySlotBounded),
            SixtyFpsFramesKeepEverySlotBounded),
        Case(nameof(FacingLocalSecondaryMotionMirrorsWorldExactly),
            () => FacingLocalSecondaryMotionMirrorsWorldExactly(tree)),
        Case(nameof(RealAttackSignalsKeepTypedAnticipationAuthoritative),
            () => RealAttackSignalsKeepTypedAnticipationAuthoritative(tree)),
        Case(nameof(RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder),
            () => RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder(tree)),
        Case(nameof(ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers),
            () => ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers(tree)),
        Case(nameof(RealLethalDamageClearsAttackAcrossRecoveryLifecycle),
            () => RealLethalDamageClearsAttackAcrossRecoveryLifecycle(tree)),
        Case(nameof(RealCheckpointRecoveryClearsAttackPresentationLock),
            () => RealCheckpointRecoveryClearsAttackPresentationLock(tree)),
        Case(nameof(RealInputAttackUpdatesFormalFacingToMatchHitbox),
            () => RealInputAttackUpdatesFormalFacingToMatchHitbox(tree)),
        Case(nameof(RealMechanicalClawAimUpdatesFormalFacingToMatchCast),
            () => RealMechanicalClawAimUpdatesFormalFacingToMatchCast(tree)),
        Case(nameof(RealCompletedAttackIdDoesNotRelatchPresentation),
            () => RealCompletedAttackIdDoesNotRelatchPresentation(tree)),
        Case(nameof(RealCoordinatorKeepsFormalThreadCastThroughEveryPhase),
            () => RealCoordinatorKeepsFormalThreadCastThroughEveryPhase(tree)),
        Case(nameof(RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode),
            () => RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode(tree)),
        Case(nameof(RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase),
            () => RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase(tree)),
        Case(nameof(RealCoordinatorSustainsFormalAndPlaceholderFailedPoses),
            () => RealCoordinatorSustainsFormalAndPlaceholderFailedPoses(tree))
    ];

    private static Task7Case Case(string name, Func<Task> execute) =>
        Task7CaseRegistry.Case(nameof(PlayerFormalVisualTests), name, execute);

    private static Task7Case FormalCase(SceneTree tree, string name,
        Action<PlayerFormalVisual> execute) => Case(name, () => WithFormalFixture(tree, execute));

    private static async Task SetupAndBasicContract(SceneTree tree)
    {
        await ApprovedWeaponFollowsActualHand(tree);
        PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
        TestAssert.True(formalScene != null, "formal player scene loads");
        PlayerFormalVisual formal = formalScene.Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        foreach (string layer in Layers)
            TestAssert.True(formal.GetNodeOrNull<Node2D>(layer) != null, $"formal player has {layer} layer");
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Move, -1, true));
        TestAssert.Equal(PlayerAnimationClip.Move, formal.CurrentClip, "formal player enters move clip");
        TestAssert.Equal(new Vector2(-1, 1), formal.Scale, "facing mirrors uniformly on X only");
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Fall, 1, false));
        TestAssert.Equal(PlayerAnimationClip.Fall, formal.CurrentClip, "formal player has distinct fall pose");
        formal.PlayCue(PresentationCue.Hurt);
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Fall, 1, false));
        TestAssert.Equal(PlayerAnimationClip.Hurt, formal.CurrentClip,
            "ordinary locomotion frames do not clear unrelated Hurt cue overrides");
        formal.ClearCueOverride();
        TestAssert.Equal(PlayerAnimationClip.Fall, formal.CurrentClip,
            "explicit cue clear restores the stored locomotion base clip");
        formal.PlayCue(PresentationCue.ThreadFired);
        TestAssert.Equal(PlayerAnimationClip.ThreadCast, formal.CurrentClip, "thread cue enters cast clip");

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        Node player = playerScene.Instantiate();
        TestAssert.True(player.GetNodeOrNull<PlayerFormalVisual>("VisualRoot/Formal") != null,
            "generated Player mounts the formal visual");
        TestAssert.True(player.FindChildren("*", "PlayerPlaceholderVisual", true, false).Count == 0,
            "generated Player contains no placeholder visual");
        player.Free();
        formal.GetParent().RemoveChild(formal);
        formal.Free();
    }

    private static async Task ApprovedWeaponFollowsActualHand(SceneTree tree)
    {
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        int samples = 0;
        try
        {
            tree.Root.AddChild(player);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
            var hand = formal.GetNode<PlayerFormalLayer>("NearArm");
            // Reflection lets the first native RED exercise the saved old rig,
            // rather than fail to compile just because its bitmap hook is absent.
            var texture = typeof(PlayerFormalLayer).GetProperty("WeaponTexture")?.GetValue(weapon) as Texture2D;
            TestAssert.True(texture != null, "approved weapon bitmap is bound on the saved normal Player");
            TestAssert.Equal("res://assets/player/thread-spool-bearer/half-bell-cleaver.png",
                texture.ResourcePath, "real weapon uses the authenticated approved single layer");
            TestAssert.Equal("06e657bb4e3831473688e5cfe7b6bb4ff2fd2a80d77c4ee17514d4b2325c325a",
                FileAccess.GetSha256(texture.ResourcePath), "approved weapon bytes are unchanged");
            using Image image = texture.GetImage();
            TestAssert.True(image.GetWidth() == 1402 && image.GetHeight() == 1122 &&
                image.GetUsedRect() == new Rect2I(504, 756, 705, 215),
                "weapon bitmap has the independently measured transparent boundary");

            var gripTexture = typeof(PlayerFormalLayer).GetProperty("WeaponGripTexture")?.GetValue(weapon) as Texture2D;
            TestAssert.True(gripTexture != null,
                "approved near-hand grip overlay is bound on the saved normal Player");
            TestAssert.Equal("res://assets/player/thread-spool-bearer/right-grip-overlay.png",
                gripTexture.ResourcePath, "near hand uses the authenticated approved right-grip layer");
            TestAssert.Equal("aa83fadef3d3de26b3d004d7353d959fb4ed26cae10c76ccc68a807adac37bd4",
                FileAccess.GetSha256(gripTexture.ResourcePath), "approved grip overlay bytes are unchanged");
            using Image gripImage = gripTexture.GetImage();
            TestAssert.True(gripImage.GetWidth() == 1402 && gripImage.GetHeight() == 1122 &&
                gripImage.GetUsedRect() == new Rect2I(810, 777, 49, 45),
                "near-hand overlay retains its original canvas and measured alpha boundary");

            foreach (int facing in new[] { 1, -1 })
                foreach (PlayerActionState action in new[] { PlayerActionState.None,
                    PlayerActionState.GroundAttack, PlayerActionState.AirAttack,
                    PlayerActionState.DownAttack, PlayerActionState.ThreadThrow, PlayerActionState.Shield })
                    foreach (float progress in new[] { .1f, .5f, .9f })
                    {
                        var actionFrame = action == PlayerActionState.None ? PlayerActionFrame.None :
                            new PlayerActionFrame(action, PlayerActionPhase.Burst, progress, false);
                        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run, progress, actionFrame,
                            facing: facing));
                        Rect2 source = WeaponRect(weapon, "WeaponSourceRect");
                        Rect2 destination = WeaponRect(weapon, "WeaponDrawRect");
                        TestAssert.Equal(new Rect2(504, 756, 705, 215), source,
                            "Draw region excludes empty master-canvas padding without editing the original");
                        // Approved right/near grip (835,798); the 287px master hand
                        // spacing maps to the existing 25 world-unit horizontal spacing.
                        const float scale = 25f / 287f;
                        TestAssert.Near(705f * scale, destination.Size.X, .0001f,
                            "weapon has a bounded rig-relative width");
                        TestAssert.Near(215f * scale, destination.Size.Y, .0001f,
                            "weapon uses the same scale on both axes, never stretch-to-fit");
                        Vector2 gripLocal = destination.Position +
                            (new Vector2(835, 798) - source.Position) * scale;
                        // Active ground wrist articulation makes the old rest-only (0,21)
                        // literal wrong: the image must follow the actual solved wrist.
                        TestAssert.True(weapon.ToGlobal(gripLocal).DistanceTo(hand.ToGlobal(hand.ArmWristLocal)) < .001f,
                            $"approved weapon primary grip follows actual posed hand action={action} facing={facing}");
                        Rect2 gripSource = WeaponRect(weapon, "WeaponGripSourceRect");
                        Rect2 gripDestination = WeaponRect(weapon, "WeaponGripDrawRect");
                        TestAssert.Equal(new Rect2(810, 777, 49, 45), gripSource,
                            "near-hand Draw region excludes only original transparent padding");
                        TestAssert.Near(49f * scale, gripDestination.Size.X, .0001f,
                            "near-hand overlay shares actual weapon horizontal scale");
                        TestAssert.Near(45f * scale, gripDestination.Size.Y, .0001f,
                            "near-hand overlay shares actual weapon vertical scale without stretching");
                        Vector2 overlayGrip = gripDestination.Position +
                            (new Vector2(835, 798) - gripSource.Position) * scale;
                        TestAssert.True(weapon.ToGlobal(overlayGrip).DistanceTo(hand.ToGlobal(hand.ArmWristLocal)) < .001f,
                            $"overlay grip follows the same actual hand action={action} facing={facing}");
                        TestAssert.True(Math.Sign(formal.Scale.X) == facing && weapon.Scale.X > 0 && weapon.Scale.Y > 0,
                            "root facing mirrors the bound weapon once without a second local flip");
                        TestAssert.Near(formal.CurrentPose.Weapon.RotationRadians, weapon.Rotation, .0001f,
                            "existing typed weapon rotation remains authoritative");
                        samples++;
                    }
            AssertGroundAttackActiveArm(formal);
            AssertApprovedWeaponObserver(formal);
            AssertApprovedSecondaryGrip(formal);
            AssertArticulatedArmObserver(formal);
            AssertSecondaryGripRelease(formal);
            AssertSecondaryGripProducerTransitions(formal);
            AssertDrawArmMotionProbe(formal);
            AssertDrawArmNormalTimeline(formal);
            AssertDrawArmInterruptedDownAttack(formal);
            // This self-seeding stress case ends in a partially released
            // shield pose. Keep the earlier saved-player controls independent.
            AssertAirAttackReachTransitions(formal);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(player))
            {
                player.GetParent()?.RemoveChild(player);
                player.Free();
            }
        }
        GD.Print($"APPROVED_WEAPON_BINDING_OK samples={samples} saved_player=true original_bytes=true primary_grip=true uniform_scale=true typed_actions=true mirror=true pixels=false human=false");
        GD.Print($"APPROVED_GRIP_OVERLAY_OK samples={samples} saved_player=true original_bytes=true shared_grip=true uniform_scale=true mirror_once=true pixels=false human=false");
    }

    private static void AssertGroundAttackActiveArm(PlayerFormalVisual formal)
    {
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        var legs = new[] { formal.GetNode<PlayerFormalLayer>("NearLeg"), formal.GetNode<PlayerFormalLayer>("FarLeg") };
        var tuning = formal.GetNode<PlayerMotor>("../../Motor").Tuning;
        Vector2 rest = new(0, 21), root = formal.GlobalPosition;
        int samples = 0, cases = 0;
        void Reset(int facing = 1)
        {
            formal.PlayCue(PresentationCue.Died);
            for (int i = 0; i < 20; i++)
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing));
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing)
                with { PresentationTimeSeconds = 0 });
        }
        void Observe(PlayerPresentationFrame frame, bool held = true)
        {
            formal.ApplyPlayerFrame(frame);
            AssertArmChain(near);
            AssertArmChain(far);
            const float scale = 25f / 287f;
            Vector2 Grip(float x) => weapon.ToGlobal(weapon.WeaponDrawRect.Position +
                (new Vector2(x, 798) - weapon.WeaponSourceRect.Position) * scale);
            TestAssert.True(Grip(835).DistanceTo(near.ToGlobal(near.ArmWristLocal)) < .001f,
                "active wrist binds primary grip in the same published frame, never one frame late");
            if (held)
            {
                Vector2 secondaryGrip = Grip(548);
                float gap = secondaryGrip.DistanceTo(far.ToGlobal(far.ArmWristLocal));
                TestAssert.True(gap < .001f && weapon.WeaponSecondaryGripContact,
                    $"active arm keeps actual offhand contact without segment stretch: action={frame.Action.State} " +
                    $"phase={frame.Action.Phase} progress={frame.Action.NormalizedTime:R} time={frame.PresentationTimeSeconds} " +
                    $"facing={frame.Base.Facing} requested_reach={far.ToLocal(secondaryGrip).Length():R} " +
                    $"actual_reach={far.ArmWristLocal.Length():R} gap={gap:R} contact={weapon.WeaponSecondaryGripContact}");
            }
            foreach (string slot in Layers)
                TestAssert.Equal(Vector2.One, formal.GetNode<Node2D>(slot).Scale, "active wrist never stretches image slot " + slot);
            foreach (var leg in legs)
                TestAssert.True(leg.LegSoleLocal.DistanceTo(leg.FreeLegSoleLocal) < .001f,
                    "ground arm trajectory cannot change unsupported leg articulation");
            TestAssert.True(!formal.CurrentActionPose.Owns(PlayerPoseSlot.NearLeg) &&
                !formal.CurrentActionPose.Owns(PlayerPoseSlot.FarLeg), "ground arm action never takes locomotion leg slots");
            TestAssert.Equal(root, formal.GlobalPosition, "active wrist never moves the player root");
            samples++;
        }
        foreach (int facing in new[] { 1, -1 })
        foreach (int hz in new[] { 30, 60, 120 })
        {
            var legBaseline = new Transform2D[hz + 1, 2];
            var bladeBaseline = new Vector2[hz + 1];
            var wristBaseline = new Vector2[hz + 1];
            foreach (AttackDirection direction in new[] { AttackDirection.Horizontal, AttackDirection.Up })
            {
                Reset(facing);
                var timeline = new PlayerPresentationTimeline(tuning);
                var idle = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, facing);
                Observe(timeline.Step(idle, 0));
                Vector2 startElbow = near.ArmElbowLocal, previous = near.ArmWristLocal;
                float minimum = previous.Length(), maximum = minimum, elbowTravel = 0, maxStep = 0;
                float upWristRise = 0f, upBladeTurn = 0f;
                var phases = new HashSet<PlayerActionPhase>();
                int attackTicks = (int)MathF.Ceiling((tuning.AttackStartupSeconds + tuning.AttackActiveSeconds +
                    tuning.AttackRecoverySeconds) * hz);
                for (int tick = 1; tick <= hz; tick++)
                {
                    var frame = timeline.Step(tick <= attackTicks ? idle with
                        { AttackId = 9244, IsAttacking = true, AttackDirection = direction } : idle, 1f / hz);
                    Observe(frame);
                    if (frame.Action.State == PlayerActionState.GroundAttack)
                    {
                        phases.Add(frame.Action.Phase);
                        minimum = Mathf.Min(minimum, near.ArmWristLocal.Length());
                        maximum = Mathf.Max(maximum, near.ArmWristLocal.Length());
                        elbowTravel = Mathf.Max(elbowTravel, startElbow.DistanceTo(near.ArmElbowLocal));
                    }
                    maxStep = Mathf.Max(maxStep, previous.DistanceTo(near.ArmWristLocal));
                    previous = near.ArmWristLocal;
                    Vector2 blade = formal.ToLocal(weapon.ToGlobal(Vector2.Right)) - formal.ToLocal(weapon.GlobalPosition);
                    Vector2 wrist = formal.ToLocal(near.ToGlobal(near.ArmWristLocal));
                    if (direction == AttackDirection.Horizontal)
                    {
                        bladeBaseline[tick] = blade;
                        wristBaseline[tick] = wrist;
                    }
                    else if (frame.Action.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact)
                    {
                        upWristRise = Mathf.Max(upWristRise, wristBaseline[tick].Y - wrist.Y);
                        upBladeTurn = Mathf.Max(upBladeTurn, -bladeBaseline[tick].AngleTo(blade));
                    }
                    for (int leg = 0; leg < 2; leg++)
                        if (direction == AttackDirection.Horizontal) legBaseline[tick, leg] = legs[leg].Transform;
                        else TestAssert.True(legBaseline[tick, leg].IsEqualApprox(legs[leg].Transform),
                            "horizontal/Up arm trajectories leave identical actual locomotion legs");
                }
                GD.Print($"GROUND_ATTACK_ACTIVE_ARM_MEASURE facing={facing} direction={direction} hz={hz} min_reach={minimum} max_reach={maximum} elbow_travel={elbowTravel} max_local_step={maxStep} phases={phases.Count}");
                TestAssert.True(minimum < 20.75f && maximum > 21.5f && elbowTravel > 1f,
                    "GROUND_ATTACK_ACTIVE_ARM_RED actual timeline must contract then extend the shoulder-space wrist and deform elbow, not merely rotate a rigid arm");
                TestAssert.True(phases.Count == 4 && maxStep < 8f && near.ArmWristLocal.DistanceTo(rest) < .001f,
                    "all authoritative attack phases keep bounded internal motion and settle after release");
                if (direction == AttackDirection.Up)
                {
                    GD.Print($"GROUND_UP_DIRECTION_MEASURE facing={facing} hz={hz} wrist_rise={upWristRise:R} blade_turn={upBladeTurn:R}");
                    TestAssert.True(upWristRise > 6f && upBladeTurn > .4f,
                        $"GROUND_UP_DIRECTION_RED: Up must raise the actual wrist and turn the blade above its matched horizontal cut; facing={facing} hz={hz} rise={upWristRise:R} turn={upBladeTurn:R}");
                }
                cases++;
            }
        }
        // Exercise the production rig with typed air frames independently of the
        // live J route: wrong aim gating or a phase-boundary snap must fail here.
        var airSamples = new List<(Vector2 Wrist, Vector2 Elbow, Vector2 FarWrist)>();
        var airPhases = new[] { PlayerActionPhase.Anticipation, PlayerActionPhase.Burst,
            PlayerActionPhase.Contact, PlayerActionPhase.Recovery };
        foreach (int facing in new[] { 1, -1 })
        {
            Reset(facing);
            int tick = 0;
            PlayerPresentationFrame Air(PlayerActionFrame action) =>
                Frame(PlayerLocomotionState.Fall, 0, action, facing: facing)
                    with { PresentationTimeSeconds = tick / 60d };
            Observe(Air(PlayerActionFrame.None));
            Vector2 previousWrist = near.ArmWristLocal, previousElbow = near.ArmElbowLocal;
            float minimum = previousWrist.Length(), maximum = minimum;
            void Sample(PlayerActionFrame action)
            {
                tick++;
                Observe(Air(action));
                Vector2 wrist = near.ArmWristLocal, elbow = near.ArmElbowLocal;
                TestAssert.True(previousWrist.DistanceTo(wrist) < 8f && previousElbow.DistanceTo(elbow) < 8f,
                    "AIR_HORIZONTAL_J_TYPED_RED: near wrist and elbow remain continuous across phase boundaries and release");
                minimum = Mathf.Min(minimum, wrist.Length()); maximum = Mathf.Max(maximum, wrist.Length());
                previousWrist = wrist; previousElbow = elbow;
                Vector2 Canonical(Vector2 p) => (p - root) * new Vector2(facing, 1);
                var actual = (Wrist: Canonical(near.ToGlobal(wrist)), Elbow: Canonical(near.ToGlobal(elbow)),
                    FarWrist: Canonical(far.ToGlobal(far.ArmWristLocal)));
                if (facing == 1) airSamples.Add(actual);
                else
                {
                    var expected = airSamples[tick - 1];
                    TestAssert.True(expected.Wrist.DistanceTo(actual.Wrist) < .002f &&
                        expected.Elbow.DistanceTo(actual.Elbow) < .002f &&
                        expected.FarWrist.DistanceTo(actual.FarWrist) < .002f,
                        "typed air horizontal J mirrors actual wrist, elbow, and offhand positions");
                }
            }
            foreach (PlayerActionPhase phase in airPhases)
            for (int step = 0; step <= 4; step++)
                Sample(new PlayerActionFrame(PlayerActionState.AirAttack, phase, step / 4f, false, new(facing, 0)));
            for (int step = 0; step < 20; step++) Sample(PlayerActionFrame.None);
            TestAssert.True(minimum < 20.75f && maximum > 21.5f && near.ArmWristLocal.DistanceTo(rest) < .001f,
                "AIR_HORIZONTAL_J_TYPED_RED: phased air wrist compresses, extends and settles to rest");

            Reset(facing); tick = 0;
            Observe(Air(PlayerActionFrame.None));
            var horizontal = new PlayerActionFrame(PlayerActionState.AirAttack, PlayerActionPhase.Burst, 1, false, new(facing, 0));
            for (tick = 1; tick <= 6; tick++) Observe(Air(horizontal));
            tick--;
            Vector2 heldWrist = near.ArmWristLocal, heldElbow = near.ArmElbowLocal;
            Vector2 heldFarWrist = far.ArmWristLocal, heldFarElbow = far.ArmElbowLocal;
            Transform2D heldNearPose = near.GlobalTransform, heldFarPose = far.GlobalTransform,
                heldWeaponPose = weapon.GlobalTransform;
            TestAssert.True(heldWrist.DistanceTo(rest) > 1f, "zero-time air control starts with real wrist load");
            float heldFarReach = far.ArmWristLocal.Length();
            TestAssert.True(heldFarReach < 47.95f,
                "held air horizontal J retains a bent offhand rather than saturating the two 24 px links");
            GD.Print(FormattableString.Invariant($"AIR_HORIZONTAL_J_HELD_REACH_OK facing={facing} far_reach={heldFarReach:R} both_grips=true"));
            PlayerActionFrame AtAim(Vector2 aim) =>
                new(horizontal.State, horizontal.Phase, horizontal.NormalizedTime, false, aim);
            Observe(Air(AtAim(Vector2.Up)));
            Vector2 retargetDelta = weapon.ToGlobal(PlayerFormalLayer.WeaponSecondaryGripOffsetLocal) - weapon.GlobalPosition;
            GD.Print($"AIR_UP_ZERO_CLOCK_REACH facing={facing} wanted={far.ToLocal(near.ToGlobal(heldWrist) + retargetDelta)} near_pos={near.Position} far_pos={far.Position} near_angle={near.Rotation:R} weapon_angle={weapon.Rotation:R} held={heldWrist} actual={near.ArmWristLocal}");
            TestAssert.True(near.GlobalTransform.IsEqualApprox(heldNearPose) && far.GlobalTransform.IsEqualApprox(heldFarPose) &&
                weapon.GlobalTransform.IsEqualApprox(heldWeaponPose),
                "AIR_UP_RETARGET_CLOCK_RED: zero elapsed cannot switch the displayed shoulders/blade before the internal grip chain");
            TestAssert.Equal(heldWrist, near.ArmWristLocal, "zero elapsed cannot advance the air wrist on aim retarget");
            TestAssert.Equal(heldElbow, near.ArmElbowLocal, "zero elapsed cannot advance the solved air elbow on aim retarget");
            for (int refresh = 0; refresh < 2; refresh++) Observe(Air(AtAim(Vector2.Up)));
            TestAssert.True(near.GlobalTransform.IsEqualApprox(heldNearPose) && far.GlobalTransform.IsEqualApprox(heldFarPose) &&
                weapon.GlobalTransform.IsEqualApprox(heldWeaponPose) && near.ArmWristLocal == heldWrist && near.ArmElbowLocal == heldElbow &&
                far.ArmWristLocal == heldFarWrist && far.ArmElbowLocal == heldFarElbow,
                "repeated zero-clock aim refresh keeps pending pose and both grip chains unchanged");
            Vector2 heldWorldWrist = near.ToGlobal(heldWrist);
            tick++; Observe(Air(AtAim(Vector2.Up)));
            TestAssert.True(!weapon.GlobalTransform.IsEqualApprox(heldWeaponPose) &&
                near.ToGlobal(near.ArmWristLocal).DistanceTo(heldWorldWrist) < 32,
                "next positive presentation step advances pending Up direction without exceeding original joint budget");
            Transform2D reverseNear = near.GlobalTransform, reverseFar = far.GlobalTransform, reverseWeapon = weapon.GlobalTransform;
            Vector2 reverseWrist = near.ArmWristLocal, reverseElbow = near.ArmElbowLocal,
                reverseFarWrist = far.ArmWristLocal, reverseFarElbow = far.ArmElbowLocal;
            for (int refresh = 0; refresh < 2; refresh++) Observe(Air(horizontal));
            TestAssert.True(near.GlobalTransform.IsEqualApprox(reverseNear) && far.GlobalTransform.IsEqualApprox(reverseFar) &&
                weapon.GlobalTransform.IsEqualApprox(reverseWeapon) && near.ArmWristLocal == reverseWrist && near.ArmElbowLocal == reverseElbow &&
                far.ArmWristLocal == reverseFarWrist && far.ArmElbowLocal == reverseFarElbow,
                "reverse Up-to-horizontal retarget also waits for positive time with real grips");
            tick++; Observe(Air(horizontal));
            TestAssert.True(!weapon.GlobalTransform.IsEqualApprox(reverseWeapon), "positive step advances reverse direction too");
            GD.Print($"AIR_UP_RETARGET_CLOCK_OK facing={facing} repeated_zero=true shoulders_weapon=true both_grips=true forward_reverse=true positive_step=true independent_timer=false");

            // Up now has its own positively tested wrist curve. These remain
            // unsupported aims; neither directional curve may spill into them.
            foreach (Vector2 aim in new[] { Vector2.Down, new Vector2(facing, -1).Normalized() })
            {
                Reset(facing); tick = 0;
                Observe(Air(PlayerActionFrame.None));
                for (tick = 1; tick <= 6; tick++) Observe(Air(AtAim(aim)));
                TestAssert.Equal(rest, near.ArmWristLocal, "unsupported air aim cannot engage horizontal or Up wrist paths");
            }
        }
        GD.Print("AIR_HORIZONTAL_J_TYPED_OK facings=2 phases=4 mirrored=true zero_elapsed=true aim_isolated=true rigid_grips=true");
        Vector2? rateReference = null;
        var loaded = new PlayerActionFrame(PlayerActionState.GroundAttack, PlayerActionPhase.Burst, 1f, false, Vector2.Right);
        PlayerPresentationFrame Timed(PlayerActionFrame action, double time) =>
            Frame(PlayerLocomotionState.Idle, 0, action) with { PresentationTimeSeconds = time };
        void Load()
        {
            Reset();
            Observe(Timed(loaded, 0));
            for (int tick = 1; tick <= 6; tick++) Observe(Timed(loaded, tick / 60d));
            TestAssert.True(near.ArmWristLocal.DistanceTo(rest) > 3f, "control actually carries attack wrist load before interruption");
        }
        foreach (int hz in new[] { 30, 60, 120 })
        {
            Reset();
            Observe(Timed(loaded, 0));
            for (int tick = 1; tick <= hz / 10; tick++) Observe(Timed(loaded, (double)tick / hz));
            TestAssert.True(near.ArmWristLocal.DistanceTo(rest) > 3f, "timed tracking is not vacuously frozen");
            if (rateReference is { } expected)
                TestAssert.True(near.ArmWristLocal.DistanceTo(expected) < .002f,
                    "same held target and elapsed time agree at 30/60/120Hz, not fixed per-publication advancement");
            rateReference ??= near.ArmWristLocal;
            Vector2 wrist = near.ArmWristLocal, elbow = near.ArmElbowLocal;
            for (int repeat = 0; repeat < 12; repeat++)
                Observe(Timed(new PlayerActionFrame(PlayerActionState.GroundAttack,
                    PlayerActionPhase.Anticipation, 1f, false, Vector2.Right), .1));
            TestAssert.Equal(wrist, near.ArmWristLocal, "zero elapsed cannot advance wrist even when authoritative target changes");
            TestAssert.Equal(elbow, near.ArmElbowLocal, "zero elapsed cannot advance solved elbow");
        }
        foreach (PlayerActionState state in new[] { PlayerActionState.None, PlayerActionState.Hurt,
            PlayerActionState.Shield, PlayerActionState.ThreadThrow, PlayerActionState.AirAttack })
        {
            Load();
            Vector2 before = near.ArmWristLocal;
            var interruption = state == PlayerActionState.None ? PlayerActionFrame.None :
                new PlayerActionFrame(state, PlayerActionPhase.Burst, 1f, false,
                    state == PlayerActionState.AirAttack ? Vector2.Down : Vector2.Right);
            bool held = state is PlayerActionState.None or PlayerActionState.AirAttack;
            Observe(Timed(interruption, .1), held);
            TestAssert.Equal(before, near.ArmWristLocal, "zero-time takeover cannot snap old wrist load: " + state);
            for (int tick = 1; tick <= 20; tick++)
            {
                Observe(Timed(interruption, .1 + tick / 60d), held);
                if (tick == 1)
                    TestAssert.True(near.ArmWristLocal.DistanceTo(rest) > .1f &&
                        near.ArmWristLocal.DistanceTo(rest) < before.DistanceTo(rest),
                        "other action continuously releases rather than snaps or retains attack load: " + state);
            }
            TestAssert.True(near.ArmWristLocal.DistanceTo(rest) < .001f, "bounded takeover settles near wrist: " + state);
        }
        // Up is now an authored positive direction, not a release-only aim.
        // Keep its same-clock takeover contract, then require its own active
        // wrist load instead of demanding that it return to rest while held.
        Load();
        Vector2 beforeAirUp = near.ArmWristLocal;
        var airUp = new PlayerActionFrame(PlayerActionState.AirAttack, PlayerActionPhase.Burst, 1, false, Vector2.Up);
        Observe(Timed(airUp, .1));
        TestAssert.Equal(beforeAirUp, near.ArmWristLocal, "zero-time Ground-to-AirUp takeover preserves existing wrist load");
        for (int tick = 1; tick <= 20; tick++) Observe(Timed(airUp, .1 + tick / 60d));
        TestAssert.True(near.ArmWristLocal.Length() > 21.5f && weapon.WeaponSecondaryGripContact,
            "held positive AirUp settles into its own articulated reach with the original offhand");
        foreach (PlayerLocomotionState state in new[] { PlayerLocomotionState.Dead, PlayerLocomotionState.Recovery })
        {
            Load();
            Observe(Frame(state, 0, PlayerActionFrame.None) with { PresentationTimeSeconds = .1 }, false);
            TestAssert.True(near.ArmWristLocal.DistanceTo(rest) < .001f, "terminal locomotion immediately clears old attack wrist load: " + state);
        }
        Load();
        formal.PlayCue(PresentationCue.Hurt); // Existing explicit resetTransitions route, not ClearCueOverride.
        TestAssert.True(near.ArmWristLocal.DistanceTo(rest) < .001f, "explicit presentation reset clears old active wrist load");
        Reset();
        GD.Print($"GROUND_ATTACK_ACTIVE_ARM_OK cases={cases} samples={samples} actual_timeline=true both_grips=true fixed_links24=true immutable_scales=true legs_unchanged=true zero_elapsed=true rates=30,60,120 interruptions=true terminal_reset=true pixels=false human=false");
    }

    private static void AssertAirAttackReachTransitions(PlayerFormalVisual formal)
    {
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        const double heldTime = .1d;
        const double firstReleaseElapsed = .0005d;
        const float gripTolerance = .001f;
        const float zeroTimeTolerance = .002f;
        var failures = new List<string>();

        // Sample the actual drawn bitmap, using the independently measured
        // source grip landmarks (835,798) and (548,798).
        Vector2 WeaponPoint(float x) => weapon.ToGlobal(weapon.WeaponDrawRect.Position +
            (new Vector2(x, 798) - weapon.WeaponSourceRect.Position) * (25f / 287f));
        Vector2 FromPrimary(float x) => weapon.ToGlobal(new Vector2((x - 835f) * (25f / 287f), 0f)) -
            weapon.GlobalPosition;
        float NearGap() => WeaponPoint(835).DistanceTo(near.ToGlobal(near.ArmWristLocal));
        float FarGap() => WeaponPoint(548).DistanceTo(far.ToGlobal(far.ArmWristLocal));

        (Vector2 Near, Vector2 Far) LoadClampedAirBurst(int facing)
        {
            formal.PlayCue(PresentationCue.Died);
            for (int i = 0; i < 20; i++)
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing));
            var air = Frame(PlayerLocomotionState.Fall, 0, PlayerActionFrame.None, facing: facing);
            formal.ApplyPlayerFrame(air with { PresentationTimeSeconds = 0 });
            var burst = new PlayerActionFrame(PlayerActionState.AirAttack, PlayerActionPhase.Burst, 1, false, new(facing, 0));
            for (int tick = 1; tick <= 6; tick++)
                formal.ApplyPlayerFrame(air with { Action = burst, PresentationTimeSeconds = tick / 60d });

            // Pre-constraint v287 Burst/1 wrist at .1s. Reproject this fixed
            // fixture through the saved shoulders and bitmap, not the solver.
            Vector2 unconstrainedNear = new(1.900426f, 27.176384f);
            float rawReach = far.ToLocal(near.ToGlobal(unconstrainedNear) + FromPrimary(548)).Length();
            float actualReach = far.ArmWristLocal.Length();
            TestAssert.True(rawReach > 48.1f && MathF.Abs(actualReach - 47.75f) < .002f &&
                near.ArmWristLocal.DistanceTo(unconstrainedNear) > .3f &&
                NearGap() < gripTolerance && FarGap() < gripTolerance && weapon.WeaponSecondaryGripContact,
                $"AIR_REACH_TRANSITION_FIXTURE facing={facing} raw_reach={rawReach:R} " +
                $"actual_reach={actualReach:R} near_gap={NearGap():R} far_gap={FarGap():R}");
            AssertArmChain(near);
            AssertArmChain(far);
            GD.Print(FormattableString.Invariant($"AIR_REACH_TRANSITION_FIXTURE_OK facing={facing} raw_reach={rawReach:R} actual_reach={actualReach:R}"));
            return (near.ArmWristLocal, far.ArmWristLocal);
        }

        // Hold the previous wrist in the current shoulder's coordinate frame
        // and carry the bitmap at the current authored angle. Intentional slot
        // motion therefore does not hide or impersonate an IK correction jump.
        float WeaponStep(Vector2 priorNear)
        {
            Vector2 expectedPrimary = near.ToGlobal(priorNear);
            return Mathf.Max(WeaponPoint(835).DistanceTo(expectedPrimary),
                WeaponPoint(548).DistanceTo(expectedPrimary + FromPrimary(548)));
        }

        void GroundHandoffAtSameTime(int facing)
        {
            var before = LoadClampedAirBurst(facing);
            var ground = new PlayerActionFrame(PlayerActionState.GroundAttack, PlayerActionPhase.Anticipation, 0, false, new(facing, 0));
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, ground, facing: facing)
                with { PresentationTimeSeconds = heldTime });
            float nearStep = near.ArmWristLocal.DistanceTo(before.Near);
            float farStep = far.ArmWristLocal.DistanceTo(before.Far);
            float weaponStep = WeaponStep(before.Near);
            float nearGap = NearGap(), farGap = FarGap();
            bool contact = weapon.WeaponSecondaryGripContact;
            string result = FormattableString.Invariant($"AIR_REACH_GROUND_HANDOFF facing={facing} near_step={nearStep:R} far_step={farStep:R} weapon_step={weaponStep:R} near_gap={nearGap:R} far_gap={farGap:R} contact={contact}");
            GD.Print(result);
            if (!(nearStep < zeroTimeTolerance && farStep < zeroTimeTolerance && weaponStep < zeroTimeTolerance &&
                nearGap < gripTolerance && farGap < gripTolerance && contact))
                failures.Add(result);
            AssertArmChain(near);
            AssertArmChain(far);
        }

        void ShieldFirstPartialRelease(int facing)
        {
            var before = LoadClampedAirBurst(facing);
            var shield = Frame(PlayerLocomotionState.Fall, 0,
                new(PlayerActionState.Shield, PlayerActionPhase.Contact, 0, false, new(facing, 0)), facing: facing);
            formal.ApplyPlayerFrame(shield with { PresentationTimeSeconds = heldTime });
            bool entryHeld = near.ArmWristLocal.DistanceTo(before.Near) < zeroTimeTolerance &&
                far.ArmWristLocal.DistanceTo(before.Far) < zeroTimeTolerance && WeaponStep(before.Near) < zeroTimeTolerance &&
                NearGap() < gripTolerance && FarGap() < gripTolerance && weapon.WeaponSecondaryGripContact;
            formal.ApplyPlayerFrame(shield with
            {
                Action = new(PlayerActionState.Shield, PlayerActionPhase.Contact, .01f, false, new(facing, 0)),
                PresentationTimeSeconds = heldTime + firstReleaseElapsed
            });
            float nearStep = near.ArmWristLocal.DistanceTo(before.Near);
            float weaponStep = WeaponStep(before.Near);
            float nearGap = NearGap(), farGap = FarGap();
            bool contact = weapon.WeaponSecondaryGripContact;
            string result = FormattableString.Invariant($"AIR_REACH_SHIELD_FIRST_RELEASE facing={facing} elapsed={firstReleaseElapsed:R} entry_held={entryHeld} near_step={nearStep:R} weapon_step={weaponStep:R} near_gap={nearGap:R} far_gap={farGap:R} contact={contact}");
            GD.Print(result);
            // .5 ms admits natural wrist motion, but not the whole .42px
            // removed reach correction. The offhand must actually separate,
            // while remaining close to the handle (neither held nor fully free).
            if (!(entryHeld && nearStep < .15f && weaponStep < .15f && nearGap < gripTolerance &&
                farGap > .0015f && farGap < .02f && !contact))
                failures.Add(result);
            AssertArmChain(near);
            AssertArmChain(far);
        }

        foreach (int facing in new[] { 1, -1 })
        {
            GroundHandoffAtSameTime(facing);
            ShieldFirstPartialRelease(facing);
        }
        TestAssert.True(failures.Count == 0,
            "clamped airborne grip handoff must remain continuous: " + string.Join("; ", failures));
        GD.Print("AIR_REACH_TRANSITIONS_OK facings=2 zero_elapsed_ground=true first_partial_shield=true bitmap_grips=true rigid_links=true");
    }

    private static void AssertApprovedSecondaryGrip(PlayerFormalVisual formal)
    {
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var secondaryTexture = typeof(PlayerFormalLayer).GetProperty("WeaponSecondaryGripTexture")?.GetValue(weapon) as Texture2D;
        int samples = 0;
        float maxGap = 0f, maxReach = 0f;
        string worst = "none";
        TestAssert.True(secondaryTexture != null &&
            secondaryTexture.ResourcePath == "res://assets/player/thread-spool-bearer/left-grip-overlay.png",
            "saved Player binds the approved secondary grip overlay");
        TestAssert.Equal("058db41e707619c25d1416dd5eda4bd6bc00f8407de2ebdf18b373d8fa7a6ebe",
            FileAccess.GetSha256(secondaryTexture.ResourcePath), "secondary overlay source bytes unchanged");
        using (Image image = secondaryTexture.GetImage())
            TestAssert.True(image.GetWidth() == 1402 && image.GetHeight() == 1122 &&
                image.GetUsedRect() == new Rect2I(525, 777, 47, 45), "secondary alpha keeps its measured source bounds");
        void Observe(PlayerPresentationFrame frame, string label)
        {
            formal.ApplyPlayerFrame(frame);
            Rect2 source = WeaponRect(weapon, "WeaponSourceRect");
            Rect2 draw = WeaponRect(weapon, "WeaponDrawRect");
            const float scale = 25f / 287f;
            Vector2 primary = weapon.ToGlobal(draw.Position + (new Vector2(835, 798) - source.Position) * scale);
            Vector2 secondary = weapon.ToGlobal(draw.Position + (new Vector2(548, 798) - source.Position) * scale);
            TestAssert.True(primary.DistanceTo(near.ToGlobal(near.ArmWristLocal)) < .001f,
                "complete held phases preserve the original primary hand contract: " + label);
            object actual = typeof(PlayerFormalLayer).GetProperty("ArmWristLocal")?.GetValue(far);
            Vector2 wrist = actual is Vector2 point ? point : new Vector2(0, 19);
            float gap = far.ToGlobal(wrist).DistanceTo(secondary);
            float reach = far.ToLocal(secondary).Length();
            AssertArmChain(near);
            AssertArmChain(far);
            Vector2 heading = formal.GetNode<Node2D>("Body").ToGlobal(new Vector2(10, -19));
            Vector2[] geometry = Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal, heading);
            foreach ((PlayerFormalLayer arm, int offset) in new[] { (near, 7), (far, 11) })
            {
                Rect2 bounds = arm.ArmDrawRect;
                Vector2[] corners = [bounds.Position, new(bounds.End.X, bounds.Position.Y), bounds.End,
                    new(bounds.Position.X, bounds.End.Y)];
                for (int index = 0; index < 4; index++)
                    TestAssert.True(geometry[offset + index].DistanceTo(arm.ToGlobal(corners[index])) < .0001f,
                        "camera maps complete arm bounds under actual facing/parent/lean: " + label);
            }
            Rect2 overlay = weapon.WeaponSecondaryGripDrawRect;
            TestAssert.True(weapon.WeaponSecondaryGripSourceRect == new Rect2(525, 777, 47, 45) &&
                overlay.Size.DistanceTo(new Vector2(47, 45) * scale) < .0001f &&
                weapon.ToGlobal(overlay.Position + new Vector2(23, 21) * scale).DistanceTo(secondary) < .001f &&
                weapon.WeaponSecondaryGripContact, "secondary overlay meets actual held wrist without independent scale: " + label);
            TestAssert.True(float.IsFinite(gap) && float.IsFinite(reach), "actual secondary geometry finite");
            if (gap > maxGap) { maxGap = gap; worst = label; }
            maxReach = Mathf.Max(maxReach, reach);
            samples++;
        }
        foreach (int facing in new[] { 1, -1 })
            foreach (Vector2 aim in new[] { Vector2.Zero, new Vector2(facing, -1).Normalized(), new Vector2(facing, 1).Normalized() })
            {
                for (int reset = 0; reset < 12; reset++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing));
                foreach (float progress in new[] { .1f, .5f, .9f })
                    Observe(Frame(PlayerLocomotionState.Run, progress, PlayerActionFrame.None, facing: facing), $"idle/{facing}/{progress}");
                foreach (PlayerActionState action in new[] { PlayerActionState.GroundAttack, PlayerActionState.AirAttack, PlayerActionState.DownAttack })
                {
                    foreach (PlayerActionPhase phase in new[] { PlayerActionPhase.Anticipation, PlayerActionPhase.Burst, PlayerActionPhase.Contact, PlayerActionPhase.Recovery })
                        foreach (float progress in new[] { .1f, .5f, .9f })
                            Observe(Frame(PlayerLocomotionState.Run, progress,
                                new PlayerActionFrame(action, phase, progress, false, aim), facing: facing), $"{action}/{phase}/{facing}/{progress}/aim={aim}");
                    for (int release = 0; release < 8; release++)
                        Observe(Frame(PlayerLocomotionState.Run, release / 8f, PlayerActionFrame.None, facing: facing), $"release/{action}/{facing}/{release}");
                }
            }
        GD.Print($"APPROVED_SECONDARY_GRIP_MEASURE samples={samples} max_gap={maxGap} max_required_reach={maxReach} missing_texture={secondaryTexture == null} worst={worst} scripted_geometry=true human=false");
        TestAssert.True(maxGap < .001f && secondaryTexture != null,
            $"APPROVED_SECONDARY_GRIP_RED actual saved Player missing offhand contact/overlay gap={maxGap} reach={maxReach} samples={samples}");
        Node2D parent = (Node2D)formal.GetParent();
        Transform2D original = parent.Transform;
        try
        {
            parent.Transform = new Transform2D(.31f, new Vector2(1.3f, .85f), 0f, new Vector2(91, -47));
            foreach (int facing in new[] { -1, 1 })
                Observe(Frame(PlayerLocomotionState.Idle, .5f, PlayerActionFrame.None, facing: facing) with
                    { MobilityLeanRadians = .18f }, "parent-transform/rigid-lean");
        }
        finally { parent.Transform = original; }
        GD.Print("APPROVED_SECONDARY_GRIP_OK held_samples=378 transformed_samples=2 fixed_links=true original_bytes=true pixels=false human=false");
    }

    private static void AssertArmChain(PlayerFormalLayer arm)
    {
        Vector2 elbow = arm.ArmElbowLocal, wrist = arm.ArmWristLocal;
        TestAssert.True(float.IsFinite(elbow.X) && float.IsFinite(elbow.Y) &&
            float.IsFinite(wrist.X) && float.IsFinite(wrist.Y), "complete articulated arm remains finite");
        TestAssert.Near(24f, elbow.Length(), .002f, "upper link keeps fixed shoulder-to-elbow length");
        TestAssert.Near(24f, elbow.DistanceTo(wrist), .002f, "lower link keeps fixed elbow-to-wrist length");
        float inkRadius = arm.Kind == PlayerFormalLayerKind.NearArm ? 5.7f : 5.2f;
        foreach (Vector2 centre in new[] { Vector2.Zero, elbow, wrist })
            foreach (Vector2 axis in new[] { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right })
                TestAssert.True(arm.ArmDrawRect.HasPoint(centre + axis * (inkRadius + .5f)),
                    "Draw bounds contain independent stroke/joint thickness, not just centres");
    }

    // A Draw-only wrist change with centres still visible must nevertheless fail
    // when actual ink clips. A node-matrix/old seven-point observer misses it.
    private static void AssertArticulatedArmObserver(PlayerFormalVisual formal)
    {
        for (int reset = 0; reset < 12; reset++)
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0f, PlayerActionFrame.None));
        Rect2 safe = new(formal.ToGlobal(new Vector2(-60, -40)), new Vector2(160, 140));
        Vector2 heading = formal.GetNode<Node2D>("Body").ToGlobal(new Vector2(10, -19));
        Vector2[] before = Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal, heading);
        TestAssert.True(Array.TrueForAll(before, safe.HasPoint), "all current geometry visible before arm-only mutation");
        var transforms = new Dictionary<string, Transform2D>();
        foreach (string slot in Layers) transforms[slot] = formal.GetNode<Node2D>(slot).Transform;
        PlayerPose pose = formal.CurrentPose;
        Rect2 weapon = formal.GetNode<PlayerFormalLayer>("Weapon").WeaponDrawRect;
        foreach (string slot in new[] { "NearArm", "FarArm" })
        {
            var arm = formal.GetNode<PlayerFormalLayer>(slot);
            Vector2 original = arm.ArmWristLocal;
            try
            {
                arm.SetArmWristLocal(new Vector2(0, -36));
                AssertArmChain(arm);
                foreach (Vector2 centre in new[] { Vector2.Zero, arm.ArmElbowLocal, arm.ArmWristLocal })
                    TestAssert.True(safe.HasPoint(arm.ToGlobal(centre)), "arm centres alone remain deceptively visible");
                float radius = slot == "NearArm" ? 4.68f : 4.16f;
                TestAssert.True(!safe.HasPoint(arm.ToGlobal(new Vector2(0, -36 - radius))),
                    "independent actual wrist-ink edge clips, not just a padded bound");
                Vector2[] actual = Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal, heading);
                for (int index = 0; index < 7; index++)
                    TestAssert.True(before[index].IsEqualApprox(actual[index]), "old seven points unchanged by arm Draw mutation");
                foreach (string name in Layers)
                    TestAssert.True(transforms[name].IsEqualApprox(formal.GetNode<Node2D>(name).Transform), "typed slot matrix unchanged");
                TestAssert.True(pose == formal.CurrentPose && weapon == formal.GetNode<PlayerFormalLayer>("Weapon").WeaponDrawRect,
                    "arm consumer does not rewrite CurrentPose or primary bitmap grip");
                TestAssert.True(Array.Exists(actual, point => !safe.HasPoint(point)),
                    "ARM_DRAW_OBSERVER_RED real clipped arm ink must be rejected by camera consumer: " + slot);
            }
            finally { arm.SetArmWristLocal(original); }
        }
        GD.Print("APPROVED_ARM_OBSERVER_OK draw_only_negatives=2 unchanged_slots=true independent_ink=true pixels=false human=false");
    }

    private static void AssertSecondaryGripRelease(PlayerFormalVisual formal)
    {
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        foreach (Vector2 requested in new[] { Vector2.Zero, new Vector2(60, 80),
            new Vector2(float.MaxValue, float.MaxValue), new Vector2(float.NaN, 0), new Vector2(0, float.PositiveInfinity) })
        {
            far.SetArmWristLocal(requested);
            AssertArmChain(far);
            TestAssert.True(far.ArmWristLocal.Length() <= 48.001f, "invalid/over-reach cannot stretch links or create NaN");
            if (!float.IsFinite(requested.X) || !float.IsFinite(requested.Y))
                TestAssert.True(far.ArmWristLocal == new Vector2(0, 19), "invalid wrist falls back to free attached hand");
            if (requested == new Vector2(60, 80))
                TestAssert.True(far.ArmWristLocal.DistanceTo(new Vector2(28.8f, 38.4f)) < .001f, "overreach preserves direction at bounded reach");
        }
        int modes = 0;
        float maxJointStep = 0f;
        foreach (PlayerActionState action in new[] { PlayerActionState.ThreadPreview, PlayerActionState.ThreadWindup,
            PlayerActionState.ThreadThrow, PlayerActionState.ThreadLatch, PlayerActionState.ThreadApply,
            PlayerActionState.ThreadFailed, PlayerActionState.Shield, PlayerActionState.Hurt })
        {
            for (int reset = 0; reset < 12; reset++)
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
            Vector2 previousWrist = Vector2.Zero, previousElbow = Vector2.Zero;
            for (int sample = 0; sample <= 10; sample++)
            {
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0,
                    new PlayerActionFrame(action, PlayerActionPhase.Burst, sample / 10f, false, new Vector2(1, -1).Normalized())));
                AssertArmChain(far);
                Vector2 wrist = far.ToGlobal(far.ArmWristLocal), elbow = far.ToGlobal(far.ArmElbowLocal);
                if (sample > 0)
                {
                    float step = Mathf.Max(previousWrist.DistanceTo(wrist), previousElbow.DistanceTo(elbow));
                    maxJointStep = Mathf.Max(maxJointStep, step);
                    TestAssert.True(step < 12f, "free-hand phase has no discontinuous wrist/elbow snap: " + action);
                }
                previousWrist = wrist;
                previousElbow = elbow;
                if (sample == 0) TestAssert.True(weapon.WeaponSecondaryGripContact, "zero-phase release starts from visible held hand");
            }
            TestAssert.True(far.ArmWristLocal.DistanceTo(new Vector2(0, 19)) < .001f &&
                !weapon.WeaponSecondaryGripContact, "spell/shield/hurt ends with free connected arm and no floating finger overlay");
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
            TestAssert.True(far.ArmWristLocal.DistanceTo(new Vector2(0, 19)) < .001f &&
                !weapon.WeaponSecondaryGripContact, "first release frame does not snap offhand back to weapon");
            for (int release = 0; release < 12; release++)
            {
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
                AssertArmChain(far);
            }
            TestAssert.True(weapon.WeaponSecondaryGripContact, "existing release blend restores actual contact");
            modes++;
        }
        foreach (PlayerLocomotionState state in new[] { PlayerLocomotionState.Dead, PlayerLocomotionState.Recovery })
        {
            for (int sample = 0; sample < 12; sample++)
                formal.ApplyPlayerFrame(Frame(state, sample / 12f, PlayerActionFrame.None));
            TestAssert.True(!weapon.WeaponSecondaryGripContact && far.ArmWristLocal.DistanceTo(new Vector2(0, 19)) < .001f,
                "death/recovery releases offhand without hiding a limb");
        }
        for (int reset = 0; reset < 12; reset++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
        formal.PlayCue(PresentationCue.ThreadFired);
        AssertArmChain(far);
        formal.ClearCueOverride();
        for (int reset = 0; reset < 12; reset++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
        TestAssert.True(weapon.WeaponSecondaryGripContact, "cue release returns through typed frame to actual contact");
        GD.Print($"APPROVED_ARM_RELEASE_OK free_modes={modes} invalid_targets=5 max_joint_step={maxJointStep} death_recovery=true cue=true geometry_only=true human=false");
    }

    private static void AssertSecondaryGripProducerTransitions(PlayerFormalVisual formal)
    {
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        MethodInfo producer = typeof(PlayerPresentationTimeline).GetMethod("MechanicalClawAction",
            BindingFlags.Static | BindingFlags.NonPublic);
        TestAssert.True(producer != null, "real claw-to-presentation producer is available");
        PlayerActionFrame Produced(MechanicalClawState state) => (PlayerActionFrame)producer.Invoke(null,
            new object[] { MechanicalClawFrame.Idle() with { State = state, CastId = 1,
                EffectiveVisibility = true, AnchorPoint = new Vector2(160, -100) } });
        PlayerActionFrame windup = Produced(MechanicalClawState.Windup);
        TestAssert.True(windup.Phase == PlayerActionPhase.Anticipation && windup.NormalizedTime == 1f,
            "test exercises actual terminal-progress entry, not invented Burst ramp");
        void Reset()
        {
            for (int frame = 0; frame < 16; frame++)
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
        }
        float maxEntry = 0f, maxReturn = 0f, firstReturnGap = 0f, maxInterrupted = 0f;
        int cases = 0;
        foreach (PlayerActionFrame terminal in new[] { Produced(MechanicalClawState.Released),
            Produced(MechanicalClawState.Failed), new PlayerActionFrame(PlayerActionState.Shield,
                PlayerActionPhase.Recovery, 1f, false) })
        {
            Reset();
            for (int frame = 0; frame < 12; frame++)
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0,
                    new PlayerActionFrame(PlayerActionState.AirAttack, PlayerActionPhase.Burst, .9f, false)));
            Vector2 held = far.ToGlobal(far.ArmWristLocal);
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, windup));
            maxEntry = Mathf.Max(maxEntry, held.DistanceTo(far.ToGlobal(far.ArmWristLocal)));
            for (int frame = 0; frame < 12; frame++)
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, terminal));
            TestAssert.True(!weapon.WeaponSecondaryGripContact, "terminal free mode starts return without overlay");
            Vector2 previous = far.ToGlobal(far.ArmWristLocal);
            for (int frame = 0; frame < 14; frame++)
            {
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
                Vector2 wrist = far.ToGlobal(far.ArmWristLocal);
                maxReturn = Mathf.Max(maxReturn, previous.DistanceTo(wrist));
                if (frame == 0) firstReturnGap = Mathf.Max(firstReturnGap,
                    far.ArmWristLocal.DistanceTo(new Vector2(0, 19)));
                previous = wrist;
                AssertArmChain(far);
            }
            TestAssert.True(weapon.WeaponSecondaryGripContact, "terminal recovery eventually restores real secondary contact");
            // Interrupt a partially returning hand, then feed the same phase at
            // a lower progress. It must neither complete the return nor teleport.
            for (int frame = 0; frame < 12; frame++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, terminal));
            for (int frame = 0; frame < 3; frame++) formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
            previous = far.ToGlobal(far.ArmWristLocal);
            foreach (PlayerActionFrame action in new[] { windup,
                new PlayerActionFrame(windup.State, windup.Phase, .1f, windup.RecoveryCancellable, windup.Aim), windup })
            {
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, action));
                Vector2 wrist = far.ToGlobal(far.ArmWristLocal);
                maxInterrupted = Mathf.Max(maxInterrupted, previous.DistanceTo(wrist));
                previous = wrist;
                AssertArmChain(far);
            }
            cases++;
        }
        GD.Print($"APPROVED_ARM_PRODUCER_TRANSITIONS_MEASURE cases={cases} entry={maxEntry} return={maxReturn} first_return_gap={firstReturnGap} interrupted={maxInterrupted} actual_claw_producer=true human=false");
        TestAssert.True(maxEntry < 12f && maxReturn < 12f && firstReturnGap < .001f && maxInterrupted < 12f,
            "ARM_PRODUCER_PROGRESS_RED real terminal entry/return/interruption must preserve bounded displayed wrist");
        Reset();
        GD.Print("APPROVED_ARM_PRODUCER_TRANSITIONS_OK actual_claw_producer=true terminal_return=true interrupted_return=true phase_rollback=true human=false");
    }

    private static void AssertDrawArmMotionProbe(PlayerFormalVisual formal)
    {
        for (int sample = 0; sample < 16; sample++)
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None));
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        Vector2 original = far.ArmWristLocal;
        var transforms = new Dictionary<string, Transform2D>();
        foreach (string slot in Layers) transforms[slot] = formal.GetNode<Node2D>(slot).Transform;
        PlayerPose pose = formal.CurrentPose;
        var probe = new Knight.Tests.Presentation.PlayerArmMotionProbe();
        bool rejected = false;
        float actualJump = 0f;
        try
        {
            far.SetArmWristLocal(new Vector2(0, -40));
            Vector2 before = far.Transform * far.ArmWristLocal;
            probe.Observe(formal, 1);
            far.SetArmWristLocal(new Vector2(0, 40));
            actualJump = before.DistanceTo(far.Transform * far.ArmWristLocal);
            foreach (string slot in Layers)
                TestAssert.True(transforms[slot].IsEqualApprox(formal.GetNode<Node2D>(slot).Transform),
                    "Draw-only jitter keeps every old node motion sample unchanged");
            TestAssert.True(pose == formal.CurrentPose && actualJump > 79.9f,
                "independent physical wrist jump exceeds original32 while typed pose remains unchanged");
            try { probe.Observe(formal, 2); }
            catch (InvalidOperationException error) when (error.Message.Contains("arm Draw motion")) { rejected = true; }
        }
        finally { far.SetArmWristLocal(original); }
        GD.Print($"ARM_DRAW_MOTION_MEASURE actual_jump={actualJump} rejected={rejected} unchanged_nodes=true human=false");
        TestAssert.True(rejected, "ARM_DRAW_MOTION_RED actual motion consumer must reject articulation hidden from node matrices");
        // Exact original32 budget, no widened threshold. A skipped observation
        // may establish a new baseline but is not a qualified continuity pair.
        far.SetArmWristLocal(Vector2.Zero);
        var boundary = new Knight.Tests.Presentation.PlayerArmMotionProbe();
        boundary.Observe(formal, 1);
        far.SetArmWristLocal(new Vector2(0, 32));
        boundary.Observe(formal, 2);
        far.SetArmWristLocal(Vector2.Zero);
        boundary.Observe(formal, 3);
        far.SetArmWristLocal(new Vector2(0, 32.01f));
        bool over = false, duplicate = false, backwards = false;
        try { boundary.Observe(formal, 4); }
        catch (InvalidOperationException error) when (error.Message.Contains("arm Draw motion")) { over = true; }
        far.SetArmWristLocal(Vector2.Zero);
        try { boundary.Observe(formal, 3); }
        catch (InvalidOperationException error) when (error.Message.Contains("increasing sample frames")) { duplicate = true; }
        try { boundary.Observe(formal, 2); }
        catch (InvalidOperationException error) when (error.Message.Contains("increasing sample frames")) { backwards = true; }
        TestAssert.True(over && duplicate && backwards, "threshold/frame errors cannot silently replace prior accepted baseline");
        far.SetArmWristLocal(new Vector2(0, -40));
        boundary.Observe(formal, 5);
        far.SetArmWristLocal(new Vector2(0, 40));
        boundary.Observe(formal, 7); // No adjacent-frame continuity claim across the gap.
        far.SetArmWristLocal(original);
        GD.Print("ARM_DRAW_MOTION_OK hidden_jump_rejected=true boundary32=true frame_fail_closed=true human=false");
    }

    private static void AssertDrawArmNormalTimeline(PlayerFormalVisual formal)
    {
        var timeline = new PlayerPresentationTimeline(formal.GetNode<PlayerMotor>("../../Motor").Tuning);
        var probe = new Knight.Tests.Presentation.PlayerArmMotionProbe();
        var idle = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, 1);
        Knight.Tests.Presentation.PlayerArmMotionPoint[] previous = null;
        int frame = 0, pairs = 0;
        float largest = 0f;
        void Step(PlayerPresentationInput input, int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                formal.ApplyPlayerFrame(timeline.Step(input, 1f / 60f));
                var points = probe.Observe(formal, ++frame);
                TestAssert.Equal(4, points.Length, "continuous reader includes both actual elbows and wrists");
                if (previous != null)
                    for (int index = 0; index < 4; index++)
                    {
                        largest = Mathf.Max(largest, points[index].Position.DistanceTo(previous[index].Position));
                        pairs++;
                    }
                previous = points;
            }
        }
        Step(idle, 20);
        Step(idle with { Velocity = new Vector2(210, 0) }, 24);
        Step(idle with { Velocity = new Vector2(130, -180), Grounded = false }, 18);
        Step(idle with { Velocity = new Vector2(130, 0), Grounded = false }, 6);
        Step(idle with { Velocity = new Vector2(130, 220), Grounded = false }, 18);
        Step(idle, 16);
        Step(idle with { AttackId = 7001, IsAttacking = true }, 20);
        Step(idle, 16);
        Step(idle with { Grounded = false, AttackId = 7002, IsAttacking = true }, 20);
        Step(idle with { Grounded = false }, 16);
        Step(idle with { Grounded = false, AttackId = 7003, IsAttacking = true,
            AttackDirection = AttackDirection.Down }, 20);
        Step(idle, 16);
        foreach (MechanicalClawState state in new[] { MechanicalClawState.Aim, MechanicalClawState.Windup,
            MechanicalClawState.Travel, MechanicalClawState.Latched, MechanicalClawState.Reel,
            MechanicalClawState.Swing, MechanicalClawState.Boost, MechanicalClawState.Released,
            MechanicalClawState.Retract, MechanicalClawState.Failed })
            Step(idle with { Claw = MechanicalClawFrame.Idle() with { State = state, CastId = 1,
                EffectiveVisibility = true, AnchorPoint = new Vector2(160, -100), ShuttleDistance = 50 } }, 12);
        Step(idle, 16);
        Step(idle with { ShieldActive = true, ShieldAim = Vector2.Up }, 18);
        Step(idle, 18);
        Step(idle with { HurtTriggered = true, HurtImpulse = new Vector2(-160, -30) }, 1);
        Step(idle, 24);
        Step(idle with { Dead = true }, 16);
        Step(idle with { CheckpointRecovering = true }, 16);
        Step(idle, 16);
        Step(idle with { Facing = -1, Velocity = new Vector2(-210, 0) }, 16);
        // World travel/root mirror must not masquerade as arm deformation.
        Node2D parent = (Node2D)formal.GetParent();
        Transform2D parentTransform = parent.Transform;
        try
        {
            parent.Position += new Vector2(500, -250);
            Step(idle with { Facing = 1 }, 1);
        }
        finally { parent.Transform = parentTransform; }
        TestAssert.True(largest <= 32f && pairs > 1800, "actual timeline must not skip joint channels or exceed unchanged32 budget");
        GD.Print($"ARM_DRAW_TIMELINE_OK frames={frame} joint_pairs={pairs} max_step={largest} delta=0.016666668 actual_timeline=true pixels=false human=false");
    }

    private static void AssertDrawArmInterruptedDownAttack(PlayerFormalVisual formal)
    {
        var tuning = formal.GetNode<PlayerMotor>("../../Motor").Tuning;
        TestAssert.True(16f / 60f < tuning.AttackStartupSeconds + tuning.AttackActiveSeconds + tuning.AttackRecoverySeconds,
            "counterexample remains inside the saved real combat attack duration, not an invented held attack");
        int cases = 0, pairs = 0;
        float maximum = 0f;
        foreach (int facing in new[] { 1, -1 })
        foreach (AttackDirection direction in new[] { AttackDirection.Down, AttackDirection.Horizontal, AttackDirection.Up })
        foreach (bool hurt in new[] { true, false })
        {
            var timeline = new PlayerPresentationTimeline(tuning);
            var probe = new Knight.Tests.Presentation.PlayerArmMotionProbe();
            var apex = new PlayerPresentationInput(Vector2.Zero, false, false, false, false, facing);
            for (int tick = 0; tick < 16; tick++) formal.ApplyPlayerFrame(timeline.Step(apex, 1f / 60f));
            var previous = probe.Observe(formal, 0);
            bool sawHurt = false, sawResumed = false;
            PlayerPresentationFrame latest = default;
            void Step(PlayerPresentationInput input, int tick)
            {
                latest = timeline.Step(input, 1f / 60f);
                sawHurt |= latest.Action.State == PlayerActionState.Hurt;
                sawResumed |= sawHurt && latest.Action.State ==
                    (direction == AttackDirection.Down ? PlayerActionState.DownAttack : PlayerActionState.AirAttack);
                formal.ApplyPlayerFrame(latest);
                var current = probe.Observe(formal, tick);
                for (int index = 0; index < current.Length; index++)
                {
                    maximum = Mathf.Max(maximum, current[index].Position.DistanceTo(previous[index].Position)); pairs++;
                }
                previous = current;
                AssertArmChain(formal.GetNode<PlayerFormalLayer>("NearArm"));
                AssertArmChain(formal.GetNode<PlayerFormalLayer>("FarArm"));
                var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
                var near = formal.GetNode<PlayerFormalLayer>("NearArm");
                TestAssert.True(weapon.ToGlobal(Vector2.Zero + (new Vector2(835, 798) - weapon.WeaponSourceRect.Position) *
                    (25f / 287f) + weapon.WeaponDrawRect.Position).DistanceTo(near.ToGlobal(near.ArmWristLocal)) < .001f,
                    "interrupted attack never detaches the original primary grip");
            }
            for (int tick = 1; tick <= 16; tick++)
                Step(apex with { AttackId = 9151, IsAttacking = true, AttackDirection = direction,
                    HurtTriggered = hurt && tick == 3 }, tick);
            TestAssert.True(sawHurt == hurt && (!hurt || sawResumed),
                "real producer executes the requested actual overlap and resumed attack");
            for (int tick = 17; tick <= 56; tick++) Step(apex, tick);
            TestAssert.True(latest.Action.State == PlayerActionState.None &&
                formal.GetNode<PlayerFormalLayer>("Weapon").WeaponSecondaryGripContact,
                "normal release finishes with actual secondary contact, not a hidden or clamped hand");
            cases++;
        }
        foreach (PlayerFormalLayerKind kind in new[] { PlayerFormalLayerKind.NearArm, PlayerFormalLayerKind.FarArm })
        {
            var arm = new PlayerFormalLayer { Kind = kind };
            try
            {
                arm.SetArmWristLocal(new Vector2(0, .01f));
                TestAssert.True(kind == PlayerFormalLayerKind.NearArm ? arm.ArmElbowLocal.X < -23.9f : arm.ArmElbowLocal.X > 23.9f,
                    "initial authored bend side remains unchanged");
                Vector2 before = arm.ArmElbowLocal;
                foreach (float y in new[] { 0f, -.01f, 0f, .01f })
                {
                    arm.SetArmWristLocal(new Vector2(0, y));
                    Vector2 elbow = arm.ArmElbowLocal;
                    TestAssert.True(elbow.DistanceTo(before) < .02f, "shoulder crossing keeps the continuous elbow branch");
                    AssertArmChain(arm);
                    for (int read = 0; read < 32; read++)
                        TestAssert.Equal(elbow, arm.ArmElbowLocal, "Draw/bounds reads do not advance local solution history");
                    before = elbow;
                }
            }
            finally { arm.Free(); }
        }
        GD.Print($"ARM_INTERRUPTED_DOWN_OK actual_timeline=true cases={cases} joint_pairs={pairs} max_step={maximum} " +
            "hurt_tick=3 resumed=true original_budget32=true zero_crossing=true pure_reads=true pixels=false human=false");
    }

    private static Rect2 WeaponRect(PlayerFormalLayer layer, string name)
    {
        object result = typeof(PlayerFormalLayer).GetProperty(name)?.GetValue(layer);
        TestAssert.True(result is Rect2, "real weapon Draw geometry hook is available: " + name);
        return (Rect2)result;
    }

    private static void AssertApprovedWeaponObserver(PlayerFormalVisual formal)
    {
        for (int sample = 0; sample < 12; sample++)
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0f, PlayerActionFrame.None));
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        Transform2D originalTransform = weapon.Transform;
        Vector2 originalDrawGrip = weapon.WeaponDrawRect.Position -
            (new Vector2(504, 756) - new Vector2(835, 798)) * (25f / 287f);
        Vector2 before = Knight.Tests.Presentation.ProloguePresentation.PlayerWeaponMotionOrigin(formal);
        Rect2 baselineSafe = new(formal.ToGlobal(new Vector2(-20, -100)), new Vector2(120, 200));
        Vector2 baselineHeading = formal.GetNode<Node2D>("Body").ToGlobal(new Vector2(10, -19));
        TestAssert.True(Array.TrueForAll(Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal, baselineHeading),
            baselineSafe.HasPoint), "weapon negative starts with all actual geometry safely visible");
        try
        {
            // Only the Draw origin changes; old line endpoints and slot matrices
            // still lie inside this hand-checked camera-like rectangle.
            weapon.SetWeaponGripLocal(originalDrawGrip + new Vector2(-60, 0));
            Rect2 safe = new(formal.ToGlobal(new Vector2(-20, -100)), new Vector2(120, 200));
            Vector2 heading = formal.GetNode<Node2D>("Body").ToGlobal(new Vector2(10, -19));
            Vector2[] legacy = [formal.GetNode<Node2D>("NearLeg").ToGlobal(new Vector2(0, 22)),
                formal.GetNode<Node2D>("FarLeg").ToGlobal(new Vector2(0, 20)), heading,
                weapon.ToGlobal(new Vector2(-2, 0)), weapon.ToGlobal(new Vector2(45, -6))];
            TestAssert.True(Array.TrueForAll(legacy, point => safe.HasPoint(point)) &&
                weapon.Transform.IsEqualApprox(originalTransform),
                "draw-only visibility counterexample keeps all old points and node transforms acceptable");
            Vector2[] observed = Knight.Tests.Presentation.ProloguePresentation.PlayerMotionGeometry(formal, heading);
            bool rejected = Array.Exists(observed, point => !safe.HasPoint(point));
            Vector2 after = Knight.Tests.Presentation.ProloguePresentation.PlayerWeaponMotionOrigin(formal);
            bool motionObserved = before.DistanceTo(after) > 59f;
            TestAssert.True(rejected && motionObserved,
                $"actual weapon observer rejects draw-only clipping and samples bitmap motion rejected={rejected} motion={motionObserved}");
            TestAssert.Equal(15, observed.Length, "observer retains original feet/heading/bitmap plus both complete arm bounds");
            Rect2 rect = weapon.WeaponDrawRect;
            Vector2[] corners = [rect.Position, new(rect.End.X, rect.Position.Y), rect.End,
                new(rect.Position.X, rect.End.Y)];
            for (int index = 0; index < corners.Length; index++)
                TestAssert.True(observed[index + 3].DistanceTo(weapon.ToGlobal(corners[index])) < .0001f,
                    "actual visibility consumer maps each real Draw rectangle corner");
        }
        finally { weapon.SetWeaponGripLocal(originalDrawGrip); }
        GD.Print("APPROVED_WEAPON_OBSERVER_OK draw_only_negative_control=true corners=4 motion_origin=true pixels=false human=false");
    }

    private static async Task WithFormalFixture(SceneTree tree, Action<PlayerFormalVisual> execute)
    {
        PlayerFormalVisual formal = GD.Load<PackedScene>(
            "res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        try { execute(formal); }
        finally
        {
            formal.GetParent()?.RemoveChild(formal);
            formal.Free();
        }
    }

    public static async Task RunTask7RecoveryContracts(SceneTree tree)
    {
        await RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder(tree);
        await ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers(tree);
        await RealLethalDamageClearsAttackAcrossRecoveryLifecycle(tree);
        await RealCheckpointRecoveryClearsAttackPresentationLock(tree);
        GD.Print("TASK7_LEGACY_SUITE_CASES_EXECUTED " +
                 "suite=PlayerFormalVisualTests count=4");
    }

    private static void TypedActionPhaseBoundaryBlendsWithoutHardCut(PlayerFormalVisual formal)
    {
        PlayerPresentationFrame anticipation = Frame(
            PlayerLocomotionState.Run,
            .42f,
            new PlayerActionFrame(PlayerActionState.GroundAttack,
                PlayerActionPhase.Anticipation, .98f, false));
        formal.ApplyPlayerFrame(anticipation);
        PoseTransform2D before = formal.CurrentPose.NearArm;

        PlayerPresentationFrame burst = Frame(
            PlayerLocomotionState.Run,
            .46f,
            new PlayerActionFrame(PlayerActionState.GroundAttack,
                PlayerActionPhase.Burst, .04f, false));
        formal.ApplyPlayerFrame(burst);
        PoseTransform2D after = formal.CurrentPose.NearArm;
        float positionDelta = before.Position.DistanceTo(after.Position);
        float rotationDelta = Mathf.Abs(Mathf.AngleDifference(
            before.RotationRadians, after.RotationRadians));

        TestAssert.True(positionDelta > .0001f && positionDelta < 2.5f,
            "named per-slot phase-boundary midpoint advances without a destination-position hard cut");
        TestAssert.True(rotationDelta > .0001f && rotationDelta < .10f,
            "named per-slot phase-boundary delta advances without a destination-rotation hard cut");
        TestAssert.Equal(PlayerActionPhase.Burst, formal.CurrentPlayerFrame.Action.Phase,
            "formal visual retains the authoritative typed Burst phase");
    }

    private static void AirAndDownAttacksExposeFourDistinctRigStages(PlayerFormalVisual formal)
    {
        foreach (PlayerActionState state in new[]
                 {
                     PlayerActionState.AirAttack,
                     PlayerActionState.DownAttack
                 })
        {
            var stages = new List<PlayerPose>();
            foreach (PlayerActionPhase phase in new[]
                     {
                         PlayerActionPhase.Anticipation,
                         PlayerActionPhase.Burst,
                         PlayerActionPhase.Contact,
                         PlayerActionPhase.Recovery
                     })
            {
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
                    new PlayerActionFrame(state, phase, 1f,
                        phase == PlayerActionPhase.Recovery)));
                stages.Add(formal.CurrentActionPose);
            }

            for (int first = 0; first < stages.Count; first++)
            for (int second = first + 1; second < stages.Count; second++)
            {
                float silhouetteDelta =
                    Mathf.Abs(Mathf.AngleDifference(stages[first].Body.RotationRadians,
                        stages[second].Body.RotationRadians)) +
                    Mathf.Abs(Mathf.AngleDifference(stages[first].NearArm.RotationRadians,
                        stages[second].NearArm.RotationRadians)) +
                    Mathf.Abs(Mathf.AngleDifference(stages[first].Weapon.RotationRadians,
                        stages[second].Weapon.RotationRadians));
                TestAssert.True(silhouetteDelta > .08f,
                    $"{state} stage {first}->{second} has a distinct Body/Arm/Weapon silhouette");
            }
        }
    }

    private static void ThreadTerminalPoseReturnsContinuouslyToIdle(PlayerFormalVisual formal)
    {
        foreach (PlayerActionState state in new[]
                 {
                     PlayerActionState.ThreadApply,
                     PlayerActionState.ThreadFailed
                 })
        {
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                PlayerActionFrame.None));
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                new PlayerActionFrame(state, PlayerActionPhase.Recovery, .5f, false,
                    new Vector2(1f, -.25f))));
            TestAssert.True(Mathf.Abs(formal.CurrentActionPose.NearArm.RotationRadians) > .04f,
                $"{state} retains a visible middle Recovery window");

            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                new PlayerActionFrame(state, PlayerActionPhase.Recovery, .98f, false,
                    new Vector2(1f, -.25f))));
            PoseTransform2D terminalArm = formal.CurrentPose.NearArm;
            PoseTransform2D terminalWeapon = formal.CurrentPose.Weapon;
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                PlayerActionFrame.None));

            float armDelta = Mathf.Abs(Mathf.AngleDifference(
                terminalArm.RotationRadians, formal.CurrentPose.NearArm.RotationRadians));
            float weaponDelta = Mathf.Abs(Mathf.AngleDifference(
                terminalWeapon.RotationRadians, formal.CurrentPose.Weapon.RotationRadians));
            TestAssert.True(armDelta < .04f,
                $"{state} .98 Recovery to Idle keeps actual NearArm slot continuous (delta={armDelta})");
            TestAssert.True(weaponDelta < .04f,
                $"{state} .98 Recovery to Idle keeps actual Weapon slot continuous (delta={weaponDelta})");
        }
    }

    private static void ThreadAimUsesFacingLocalMirroredRig(PlayerFormalVisual formal)
    {
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Right), facing: 1));
        PlayerPose right = formal.CurrentActionPose;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Left), facing: -1));
        PlayerPose left = formal.CurrentActionPose;
        AssertMirroredAimUsesEquivalentLocalRig(right, left, "horizontal Right/Left");
        TestAssert.Near(-1f, formal.Scale.X, .0001f,
            "left world aim mirrors the formal root exactly once");

        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Up)));
        PlayerPose up = formal.CurrentActionPose;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Down)));
        PlayerPose down = formal.CurrentActionPose;

        TestAssert.True(Mathf.Abs(Mathf.AngleDifference(up.Body.RotationRadians,
                down.Body.RotationRadians)) > .10f,
            "typed ThreadCast Aim Up/Down produces distinct actual Body rotation");
        TestAssert.True(Mathf.Abs(Mathf.AngleDifference(up.NearArm.RotationRadians,
                down.NearArm.RotationRadians)) > .40f,
            "typed ThreadCast Aim Up/Down produces distinct actual Arm rotation");
        TestAssert.True(Mathf.Abs(Mathf.AngleDifference(up.Weapon.RotationRadians,
                down.Weapon.RotationRadians)) > .40f,
            "typed ThreadCast Aim Up/Down produces distinct actual Weapon rotation");

        Vector2 upRight = new Vector2(1f, -1f).Normalized();
        Vector2 upLeft = new Vector2(-1f, -1f).Normalized();
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: upRight), facing: 1));
        PlayerPose diagonalRight = formal.CurrentActionPose;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: upLeft), facing: -1));
        PlayerPose diagonalLeft = formal.CurrentActionPose;
        AssertMirroredAimUsesEquivalentLocalRig(diagonalRight, diagonalLeft,
            "diagonal UpRight/UpLeft");
    }

    private static void AssertMirroredAimUsesEquivalentLocalRig(
        PlayerPose right,
        PlayerPose left,
        string direction)
    {
        TestAssert.Near(right.Body.RotationRadians, left.Body.RotationRadians, .0001f,
            $"{direction} world aims produce equivalent facing-local Body rotation");
        TestAssert.Near(right.NearArm.RotationRadians, left.NearArm.RotationRadians, .0001f,
            $"{direction} world aims produce equivalent facing-local Arm rotation");
        TestAssert.Near(right.Weapon.RotationRadians, left.Weapon.RotationRadians, .0001f,
            $"{direction} world aims produce equivalent facing-local Weapon rotation");
    }

    private static void ActionOverRunKeepsLegsAndSecondarySlotsContinuous(PlayerFormalVisual formal)
    {
        var action = new PlayerActionFrame(PlayerActionState.GroundAttack,
            PlayerActionPhase.Contact, .55f, false);
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run, .20f, action));
        PoseTransform2D firstNearLeg = formal.CurrentPose.NearLeg;
        PoseTransform2D firstFarLeg = formal.CurrentPose.FarLeg;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run, .32f, action));
        PoseTransform2D secondNearLeg = formal.CurrentPose.NearLeg;
        PoseTransform2D secondFarLeg = formal.CurrentPose.FarLeg;

        TestAssert.True(Mathf.Abs(firstNearLeg.RotationRadians - secondNearLeg.RotationRadians) > .001f &&
            Mathf.Abs(firstFarLeg.RotationRadians - secondFarLeg.RotationRadians) > .001f,
            "named locomotion-under-action assertion keeps both run legs advancing under an upper-body attack");
        TestAssert.True(!formal.CurrentActionPose.Owns(PlayerPoseSlot.NearLeg) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.FarLeg),
            "upper-body action ownership excludes both locomotion legs");
        TestAssert.True(!formal.CurrentActionPose.Owns(PlayerPoseSlot.Mantle) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.Lantern) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.ShoulderCore) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.Tether),
            "upper-body action ownership excludes all Task 1 secondary slots");
    }

    private static void LoopWrapKeepsEveryLocomotionSlotContinuous(PlayerFormalVisual formal)
    {
        AssertLoopWrapKeepsEverySlotContinuous(formal, PlayerLocomotionState.Run, "Run");
        AssertLoopWrapKeepsEverySlotContinuous(formal, PlayerLocomotionState.Idle, "Idle");
    }

    private static void AssertLoopWrapKeepsEverySlotContinuous(
        PlayerFormalVisual formal,
        PlayerLocomotionState loop,
        string label)
    {
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Dead, .72f,
            PlayerActionFrame.None));
        formal.ApplyPlayerFrame(Frame(loop, .20f,
            PlayerActionFrame.None));
        formal.ApplyPlayerFrame(Frame(loop, .98f,
            PlayerActionFrame.None));
        PoseTransform2D[] beforeWrap = SlotTransforms(formal.CurrentPose);

        formal.ApplyPlayerFrame(Frame(loop, .02f,
            PlayerActionFrame.None));
        PoseTransform2D[] afterWrap = SlotTransforms(formal.CurrentPose);

        for (int slot = 0; slot < Layers.Length; slot++)
        {
            TestAssert.True(beforeWrap[slot].Position.DistanceTo(afterWrap[slot].Position) < 2f,
                $"{label} loop 0.98->0.02 keeps slot {Layers[slot]} position continuous instead of returning to entry pose");
            TestAssert.True(Mathf.Abs(Mathf.AngleDifference(beforeWrap[slot].RotationRadians,
                    afterWrap[slot].RotationRadians)) < .15f,
                $"{label} loop 0.98->0.02 keeps slot {Layers[slot]} rotation continuous instead of returning to entry pose");
        }
    }

    private static void TaskOneSecondaryOutputsReachTheirOwnedFormalSlots(PlayerFormalVisual formal)
    {
        var secondary = new SecondaryMotionFrame(
            Pose(3f, -2f, .11f),
            Pose(-2f, 4f, -.09f),
            Pose(1f, -1f, .17f),
            Pose(-3f, 2f, -.13f));
        PlayerPresentationFrame frame = Frame(PlayerLocomotionState.Idle, .25f,
            PlayerActionFrame.None, secondary);
        formal.ApplyPlayerFrame(frame);

        TestAssert.Near(3f, formal.CurrentPose.Mantle.Position.X, .0001f,
            "mantle continuously consumes the Task 1 mantle output");
        TestAssert.Near(4f, formal.CurrentPose.Lantern.Position.Y, .0001f,
            "lantern continuously consumes the Task 1 lantern output");
        TestAssert.Near(.17f, formal.CurrentPose.ShoulderCore.RotationRadians, .0001f,
            "shoulder spool continuously consumes the Task 1 spool output");
        TestAssert.Near(-.13f, formal.CurrentPose.Tether.RotationRadians, .0001f,
            "tether continuously consumes the Task 1 tether output");
    }

    private static void CompatibilityAdaptersRemainVisible(PlayerFormalVisual formal)
    {
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Rise, -1, false));
        TestAssert.Equal(PlayerAnimationClip.Rise, formal.CurrentClip,
            "legacy ApplyFrame remains a valid visible compatibility adapter");
        formal.PlayCue(PresentationCue.Hurt);
        TestAssert.Equal(PlayerAnimationClip.Hurt, formal.CurrentClip,
            "legacy PlayCue remains a valid visible compatibility adapter");
        formal.ClearCueOverride();
        TestAssert.Equal(PlayerAnimationClip.Rise, formal.CurrentClip,
            "legacy cue clear restores the compatibility locomotion base");
    }

    private static void SixtyFpsFramesKeepEverySlotBounded(PlayerFormalVisual formal)
    {
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        var input = new PlayerPresentationInput(
            new Vector2(210f, 0f),
            Grounded: true,
            Dashing: false,
            Dead: false,
            CheckpointRecovering: false,
            Facing: 1,
            AttackId: 611,
            IsAttacking: true,
            AttackDirection: AttackDirection.Horizontal);
        formal.ApplyPlayerFrame(timeline.Step(input, 1f / 60f));
        PoseTransform2D[] previous = SlotTransforms(formal.CurrentPose);
        for (int sample = 1; sample <= 30; sample++)
        {
            formal.ApplyPlayerFrame(timeline.Step(input, 1f / 60f));
            PoseTransform2D[] current = SlotTransforms(formal.CurrentPose);
            for (int slot = 0; slot < current.Length; slot++)
            {
                TestAssert.True(previous[slot].Position.DistanceTo(current[slot].Position) < 6f,
                    $"60 FPS sample {sample} keeps slot {Layers[slot]} displacement bounded");
                TestAssert.True(Mathf.Abs(Mathf.AngleDifference(previous[slot].RotationRadians,
                        current[slot].RotationRadians)) < .5f,
                    $"60 FPS sample {sample} keeps slot {Layers[slot]} rotation bounded");
            }
            previous = current;
        }
    }

    private static PoseTransform2D[] SlotTransforms(PlayerPose pose) =>
    [
        pose.Body, pose.NearArm, pose.FarArm, pose.NearLeg, pose.FarLeg,
        pose.Weapon, pose.Mantle, pose.ShoulderCore, pose.Lantern, pose.Tether
    ];

    private static async Task FacingLocalSecondaryMotionMirrorsWorldExactly(SceneTree tree)
    {
        var rightTimeline = new PlayerPresentationTimeline(new PlayerTuning());
        var leftTimeline = new PlayerPresentationTimeline(new PlayerTuning());
        rightTimeline.Step(new PlayerPresentationInput(
            Vector2.Zero, true, false, false, false, -1), 1f / 60f);
        leftTimeline.Step(new PlayerPresentationInput(
            Vector2.Zero, true, false, false, false, 1), 1f / 60f);

        PlayerPresentationFrame rightFrame = rightTimeline.Step(new PlayerPresentationInput(
            new Vector2(240f, -20f), true, false, false, false, 1,
            HurtTriggered: true, HurtImpulse: new Vector2(100f, 20f)), 1f / 60f);
        PlayerPresentationFrame leftFrame = leftTimeline.Step(new PlayerPresentationInput(
            new Vector2(-240f, -20f), true, false, false, false, -1,
            HurtTriggered: true, HurtImpulse: new Vector2(-100f, 20f)), 1f / 60f);

        AssertTransformNear(rightFrame.Secondary.Mantle, leftFrame.Secondary.Mantle,
            "mirrored velocity/turn/impact keeps Mantle facing-local");
        AssertTransformNear(rightFrame.Secondary.Lantern, leftFrame.Secondary.Lantern,
            "mirrored velocity/turn/impact keeps Lantern facing-local");
        AssertTransformNear(rightFrame.Secondary.ShoulderSpool, leftFrame.Secondary.ShoulderSpool,
            "mirrored velocity/turn/impact keeps shoulder spool facing-local");
        AssertTransformNear(rightFrame.Secondary.Tether, leftFrame.Secondary.Tether,
            "mirrored velocity/turn/impact keeps tether/charm facing-local");

        PackedScene scene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
        PlayerFormalVisual right = scene.Instantiate<PlayerFormalVisual>();
        PlayerFormalVisual left = scene.Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(right);
        tree.Root.AddChild(left);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        right.ApplyPlayerFrame(rightFrame);
        left.ApplyPlayerFrame(leftFrame);

        AssertTransformNear(right.CurrentPose.Body, left.CurrentPose.Body,
            "mirrored world motion keeps Body local pose equivalent");
        AssertTransformNear(right.CurrentPose.Mantle, left.CurrentPose.Mantle,
            "mirrored world motion keeps Mantle local pose equivalent");
        AssertTransformNear(right.CurrentPose.Lantern, left.CurrentPose.Lantern,
            "mirrored world motion keeps Lantern local pose equivalent");
        AssertTransformNear(right.CurrentPose.ShoulderCore, left.CurrentPose.ShoulderCore,
            "mirrored world motion keeps spool local pose equivalent");
        AssertTransformNear(right.CurrentPose.Tether, left.CurrentPose.Tether,
            "mirrored world motion keeps charm/tether local pose equivalent");
        TestAssert.Equal(new Vector2(1f, 1f), right.Scale,
            "right-facing formal root keeps positive world X scale");
        TestAssert.Equal(new Vector2(-1f, 1f), left.Scale,
            "left-facing formal root provides the sole world X mirror");

        right.QueueFree();
        left.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void AssertTransformNear(PoseTransform2D expected, PoseTransform2D actual,
        string message)
    {
        TestAssert.Near(expected.Position.X, actual.Position.X, .0001f, $"{message} position X");
        TestAssert.Near(expected.Position.Y, actual.Position.Y, .0001f, $"{message} position Y");
        TestAssert.Near(expected.RotationRadians, actual.RotationRadians, .0001f,
            $"{message} rotation");
    }

    private static async Task RealAttackSignalsKeepTypedAnticipationAuthoritative(SceneTree tree)
    {
        await AssertRealAttackSignalKeepsTypedAnticipationAuthoritative(
            tree, AttackDirection.Horizontal, "AttackStarted");
        await AssertRealAttackSignalKeepsTypedAnticipationAuthoritative(
            tree, AttackDirection.Down, "Downstrike");
    }

    private static async Task RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder));
        level.Name = "PlayerRecoveryPresentationFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        bool recoveryRequested = false;
        bool recoveryCompleted = false;
        var publishedFrames = new List<PlayerPresentationFrame>();
        level.RecoveryRequested += _ => recoveryRequested = true;
        level.RecoveryCompleted += _ => recoveryCompleted = true;
        coordinator.FramePublished += publishedFrames.Add;

        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        GD.Print($"FORMAL_RECOVERY_REQUEST_BOUNDARY before=true state={flow.State} " +
            $"pending={level.ResidentPreparationPending} health={health.CurrentHealth} " +
            $"reason={level.ResidentPreparationFailure}");
        bool lethalAccepted = health.ApplyDamage(
            new DamageRequest(health.CurrentHealth, Vector2.Right, 0f, 99101UL));
        GD.Print($"FORMAL_RECOVERY_REQUEST_BOUNDARY before=false state={flow.State} " +
            $"pending={level.ResidentPreparationPending} health={health.CurrentHealth} " +
            $"damage_accepted={lethalAccepted} requested={recoveryRequested} " +
            $"rejected={flow.LastRejectedTransition}");

        TestAssert.True(recoveryRequested,
            "real lethal damage enters the Prologue recovery lifecycle synchronously");
        await WaitForPublishedRecoverySequence(tree, publishedFrames);

        TestAssert.True(recoveryCompleted,
            "real generated Player death completes the Prologue checkpoint lifecycle");
        List<PlayerLocomotionState> sequence = DistinctLocomotionStates(publishedFrames);
        TestAssert.True(sequence.Count >= 3,
            "real typed-frame event publishes at least Dead, Recovery, and ordinary states");
        TestAssert.Equal(PlayerLocomotionState.Dead, sequence[0],
            "real typed-frame event publishes Dead first");
        TestAssert.Equal(PlayerLocomotionState.Recovery, sequence[1],
            "real typed-frame event publishes Recovery immediately after Dead");
        TestAssert.True(sequence[2] is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "real typed-frame event publishes ordinary locomotion immediately after Recovery");
        TestAssert.True(formal.CurrentPlayerFrame.Locomotion.State is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "real formal rig preserves Dead -> Recovery -> ordinary locomotion order");

        coordinator.FramePublished -= publishedFrames.Add;
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task WaitForPublishedRecoverySequence(
        SceneTree tree,
        List<PlayerPresentationFrame> publishedFrames)
    {
        // The frame budget is fail-closed timeout only. Ordering observations come
        // exclusively from the coordinator's natural typed-frame publication event.
        for (int timeoutFrame = 0; timeoutFrame < 120; timeoutFrame++)
        {
            if (DistinctLocomotionStates(publishedFrames).Count >= 3) return;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        throw new InvalidOperationException(
            "Assertion failed: timed out waiting for natural Dead -> Recovery -> ordinary typed-frame publications");
    }

    private static async Task ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers));
        level.Name = "ThrowingPlayerFrameObserverFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var downstreamFrames = new List<PlayerPresentationFrame>();
        void ThrowingObserver(PlayerPresentationFrame _) =>
            throw new InvalidOperationException("deliberate diagnostic observer failure");
        coordinator.FramePublished += ThrowingObserver;
        coordinator.FramePublished += downstreamFrames.Add;

        Exception leaked = null;
        try
        {
            health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0f, 99103UL));
        }
        catch (Exception exception)
        {
            leaked = exception;
        }
        TestAssert.True(leaked == null,
            "throwing frame observer is isolated from real lethal recovery dispatch");
        await WaitForPublishedRecoverySequence(tree, downstreamFrames);
        List<PlayerLocomotionState> sequence = DistinctLocomotionStates(downstreamFrames);
        TestAssert.Equal(PlayerLocomotionState.Dead, sequence[0],
            "later observer still receives Dead after an earlier observer throws");
        TestAssert.Equal(PlayerLocomotionState.Recovery, sequence[1],
            "throwing observer cannot freeze AdvanceRecoveryPresentation");
        TestAssert.True(sequence[2] is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "later observer receives ordinary locomotion after isolated observer failures");
        TestAssert.True(coordinator.FrameObserverFailureCount > 0,
            "coordinator exposes a read-only diagnostic observer failure summary");
        coordinator.FramePublished -= ThrowingObserver;
        coordinator.FramePublished -= downstreamFrames.Add;
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static List<PlayerLocomotionState> DistinctLocomotionStates(
        List<PlayerPresentationFrame> publishedFrames)
    {
        var result = new List<PlayerLocomotionState>();
        foreach (PlayerPresentationFrame frame in publishedFrames)
        {
            PlayerLocomotionState state = frame.Locomotion.State;
            if (result.Count == 0 || result[^1] != state) result.Add(state);
        }
        return result;
    }

    private static async Task RealLethalDamageClearsAttackAcrossRecoveryLifecycle(SceneTree tree)
    {
        ReleaseInput();
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealLethalDamageClearsAttackAcrossRecoveryLifecycle));
        level.Name = "PlayerAttackDeathPresentationFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");

        var attackStarted = tree.ToSignal(combat, PlayerCombat.SignalName.AttackStarted);
        await PressAttackPrimary(tree);
        await attackStarted;
        Input.ActionRelease(InputActions.PrimaryAction);
        TestAssert.True(combat.IsAttacking,
            "real Input.ActionPress starts PlayerCombat before lethal damage");

        var publishedFrames = new List<PlayerPresentationFrame>();
        coordinator.FramePublished += publishedFrames.Add;
        health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Left, 0f, 99102UL));
        await WaitForPublishedRecoverySequence(tree, publishedFrames);

        List<PlayerLocomotionState> sequence = DistinctLocomotionStates(publishedFrames);
        TestAssert.Equal(PlayerLocomotionState.Dead, sequence[0],
            "attack death lifecycle publishes Dead first");
        TestAssert.Equal(PlayerLocomotionState.Recovery, sequence[1],
            "attack death lifecycle publishes Recovery second");
        TestAssert.True(sequence[2] is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "attack death lifecycle publishes ordinary locomotion third");

        int ordinaryIndex = publishedFrames.FindIndex(frame =>
            frame.Locomotion.State is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery);
        TestAssert.True(ordinaryIndex >= 0,
            "attack death lifecycle publishes an ordinary frame after recovery");
        for (int index = 0; index <= ordinaryIndex; index++)
        {
            PlayerPresentationFrame frame = publishedFrames[index];
            TestAssert.Equal(PlayerActionState.None, frame.Action.State,
                $"natural {frame.Locomotion.State} publication clears the pre-death attack action");
        }

        coordinator.FramePublished -= publishedFrames.Add;
        ReleaseInput();
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealCheckpointRecoveryClearsAttackPresentationLock(SceneTree tree)
    {
        ReleaseInput();
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealCheckpointRecoveryClearsAttackPresentationLock));
        level.Name = "PlayerAttackCheckpointRecoveryFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");

        var attackStarted = tree.ToSignal(combat, PlayerCombat.SignalName.AttackStarted);
        await PressAttackPrimary(tree);
        await attackStarted;
        Input.ActionRelease(InputActions.PrimaryAction);

        var publishedFrames = new List<PlayerPresentationFrame>();
        coordinator.FramePublished += publishedFrames.Add;
        level.RequestRespawn("presentation-checkpoint-recovery");
        await WaitForCondition(tree, () =>
        {
            bool sawRecovery = false;
            bool sawOrdinaryAfterRecovery = false;
            foreach (PlayerPresentationFrame frame in publishedFrames)
            {
                if (frame.Locomotion.State == PlayerLocomotionState.Recovery) sawRecovery = true;
                else if (sawRecovery) sawOrdinaryAfterRecovery = true;
            }
            return sawOrdinaryAfterRecovery;
        }, "non-death checkpoint Recovery followed by ordinary typed publication");

        int recoveryIndex = publishedFrames.FindIndex(frame =>
            frame.Locomotion.State == PlayerLocomotionState.Recovery);
        int ordinaryIndex = publishedFrames.FindIndex(recoveryIndex + 1, frame =>
            frame.Locomotion.State != PlayerLocomotionState.Recovery);
        TestAssert.True(recoveryIndex >= 0 && ordinaryIndex > recoveryIndex,
            "real checkpoint recovery publishes Recovery before ordinary locomotion");
        for (int index = recoveryIndex; index <= ordinaryIndex; index++)
            TestAssert.Equal(PlayerActionState.None, publishedFrames[index].Action.State,
                "non-death checkpoint recovery clears the presentation-only attack lock");

        coordinator.FramePublished -= publishedFrames.Add;
        ReleaseInput();
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealInputAttackUpdatesFormalFacingToMatchHitbox(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "RealInputAttackFacingFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        Hitbox hitbox = combat.GetNode<Hitbox>("AttackHitbox");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");

        Input.ActionPress(InputActions.MoveLeft);
        await PressAttackPrimary(tree);
        await Frames(tree, 1);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        TestAssert.True(combat.IsAttacking,
            "real Input.ActionPress reaches PlayerInputReader and starts PlayerCombat");
        TestAssert.True(hitbox.Position.X < 0f,
            "real PlayerCombat horizontal hitbox follows the backward left aim");
        TestAssert.Equal(-1, coordinator.LastPlayerFrame.Base.Facing,
            "typed presentation root follows the same left aim as the real hitbox while stationary");
        TestAssert.Equal(Vector2.Left, coordinator.LastPlayerFrame.Action.Aim,
            "typed horizontal attack aim matches the real left-facing hitbox");
        TestAssert.Near(-1f, formal.Scale.X, .0001f,
            "formal root mirrors left for the real backward attack");

        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealMechanicalClawAimUpdatesFormalFacingToMatchCast(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "MechanicalClawFacingFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        GrappleSurface2D surface = GrappleSurface("FacingSurface", new Vector2(-180f, -120f));
        root.AddChild(surface);
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;
        ClawHarness claw = SuspendMechanicalClaw(player);
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));

        Vector2 castAim = claw.Controller.CurrentFrame.CasterWorldPosition.DirectionTo(
            claw.Controller.CurrentFrame.AnchorPoint);
        TestAssert.True(castAim.X < 0f && castAim.Y < 0f,
            "real mechanical-claw target produces an authoritative up-left aim");
        TestAssert.Equal(-1, coordinator.LastPlayerFrame.Base.Facing,
            "typed presentation root follows the real mechanical-claw horizontal intent");
        TestAssert.Near(castAim.X,
            coordinator.LastPlayerFrame.Action.Aim.X, .0001f,
            "typed claw action consumes the normalized real cast Aim.X");
        TestAssert.Near(castAim.Y,
            coordinator.LastPlayerFrame.Action.Aim.Y, .0001f,
            "typed claw action consumes the normalized real cast Aim.Y");
        TestAssert.Near(-1f, formal.Scale.X, .0001f,
            "formal root mirrors left for the real diagonal claw cast");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealCompletedAttackIdDoesNotRelatchPresentation(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "CompletedAttackPresentationFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var afterNaturalCompletion = new List<PlayerPresentationFrame>();
        bool observedAttackPose = false;
        bool observedFirstNone = false;
        void OnFrame(PlayerPresentationFrame frame)
        {
            if (frame.Action.State is PlayerActionState.GroundAttack or PlayerActionState.AirAttack or
                PlayerActionState.DownAttack)
            {
                observedAttackPose = true;
                return;
            }
            if (!observedAttackPose || combat.IsAttacking) return;
            if (!observedFirstNone)
            {
                observedFirstNone = frame.Action.State == PlayerActionState.None;
                return;
            }
            afterNaturalCompletion.Add(frame);
        }
        coordinator.FramePublished += OnFrame;

        var attackStarted = tree.ToSignal(combat, PlayerCombat.SignalName.AttackStarted);
        await PressAttackPrimary(tree);
        await attackStarted;
        Input.ActionRelease(InputActions.PrimaryAction);
        await WaitForCondition(tree, () => observedFirstNone && afterNaturalCompletion.Count >= 5,
            "natural attack completion plus five subsequent typed publications");

        foreach (PlayerPresentationFrame frame in afterNaturalCompletion)
            TestAssert.Equal(PlayerActionState.None, frame.Action.State,
                "completed external attack ID cannot re-latch the presentation timeline");

        coordinator.FramePublished -= OnFrame;
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task WaitForCondition(
        SceneTree tree,
        Func<bool> condition,
        string description)
    {
        for (int timeoutFrame = 0; timeoutFrame < 240; timeoutFrame++)
        {
            if (condition()) return;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        throw new InvalidOperationException($"Assertion failed: timed out waiting for {description}");
    }

    private static async Task AssertRealAttackSignalKeepsTypedAnticipationAuthoritative(
        SceneTree tree,
        AttackDirection direction,
        string cueName)
    {
        var root = new Node2D { Name = $"{cueName}TypedAuthorityFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        PoseTransform2D before = formal.CurrentActionPose.NearArm;

        combat.EmitSignal(PlayerCombat.SignalName.AttackStarted, 900UL + (ulong)direction,
            (int)direction);
        coordinator._Process(0d);

        TestAssert.Equal(PlayerActionPhase.Anticipation, coordinator.LastPlayerFrame.Action.Phase,
            $"real generated Player {cueName} enters typed Anticipation first");
        TestAssert.Near(before.Position.X, formal.CurrentActionPose.NearArm.Position.X, .0001f,
            $"real generated Player {cueName} legacy cue cannot inject Burst position before typed Anticipation");
        TestAssert.Near(before.Position.Y, formal.CurrentActionPose.NearArm.Position.Y, .0001f,
            $"real generated Player {cueName} legacy cue cannot inject Burst height before typed Anticipation");
        TestAssert.Near(before.RotationRadians,
            formal.CurrentActionPose.NearArm.RotationRadians, .0001f,
            $"real generated Player {cueName} legacy cue cannot inject Burst rotation before typed Anticipation");

        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static PlayerPresentationFrame Frame(
        PlayerLocomotionState locomotion,
        float progress,
        PlayerActionFrame action,
        SecondaryMotionFrame? secondary = null,
        int facing = 1) => new(
        new EntityPresentationFrame(locomotion switch
        {
            PlayerLocomotionState.Dead => EntityVisualState.Dead,
            PlayerLocomotionState.Dash => EntityVisualState.Dash,
            PlayerLocomotionState.Rise or PlayerLocomotionState.JumpStart => EntityVisualState.Rise,
            PlayerLocomotionState.Fall or PlayerLocomotionState.Apex => EntityVisualState.Fall,
            PlayerLocomotionState.Run or PlayerLocomotionState.StartRun or PlayerLocomotionState.Brake =>
                EntityVisualState.Move,
            PlayerLocomotionState.Recovery => EntityVisualState.Hurt,
            _ => EntityVisualState.Idle
        }, facing, locomotion is not PlayerLocomotionState.Rise and not PlayerLocomotionState.JumpStart and
            not PlayerLocomotionState.Fall and not PlayerLocomotionState.Apex),
        new PlayerLocomotionFrame(locomotion, progress),
        action,
        secondary ?? SecondaryMotionFrame.Identity);

    private static PoseTransform2D Pose(float x, float y, float rotation) => new(
        new Vector2(x, y), rotation, Vector2.One, Colors.White);

    private static async Task RealCoordinatorKeepsFormalThreadCastThroughEveryPhase(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "MechanicalClawPresentationCoordinatorFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        CharacterBody2D player = playerScene.Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        GrappleSurface2D surface = GrappleSurface("PresentationSurface", new Vector2(180f, 0f));
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(surface);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        try
        {
        PlayerFormalVisual liveFormal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        Vector2 castStart = player.GlobalPosition;
        var observedStates = new HashSet<MechanicalClawState>();
        var observedOrder = new List<MechanicalClawState>();
        var clipAtPhase = new Dictionary<MechanicalClawState, PlayerAnimationClip>();
        void CaptureCompletedStepClip()
        {
            MechanicalClawState state = claw.Controller.CurrentFrame.State;
            if (clipAtPhase.TryAdd(state, liveFormal.CurrentClip))
                GD.Print($"FORMAL_CLAW_CLIP_TRACE state={state} clip={liveFormal.CurrentClip.Name} post_step=true");
        }
        claw.Controller.ClawFrameChanged += (frame, target) =>
        {
            if (observedStates.Add(frame.State))
            {
                observedOrder.Add(frame.State);
                GD.Print($"FORMAL_CLAW_PHASE_TRACE state={frame.State} caster={frame.CasterWorldPosition} " +
                    $"body={player.GlobalPosition} velocity={frame.CasterVelocity} dock={frame.DockPoint} " +
                    $"arrived={frame.Arrived} failure={frame.Failure}");
            }
        };

        int captureAttempts = 0, captureCompleted = 0, captureFaults = 0;
        bool sampledActualPreview = false;
        Action<MechanicalClawFrame, GrappleSurface2D> faultCapture = (frame, target) =>
        {
            if (captureAttempts != 0 || frame.State != MechanicalClawState.Aim) return;
            captureAttempts++;
            bool complete = PrologueCanyonGeneratedMainRouteTests.Task10CapturePostHopObservation(() =>
            {
                sampledActualPreview = frame.CastId == claw.Controller.CurrentFrame.CastId &&
                    frame.AnchorPoint.DistanceTo(surface.WorldAnchor) < .001f;
                throw new InvalidOperationException("intentional posthop candidate observation fault");
            }, () => captureFaults++);
            if (complete) captureCompleted++;
        };
        claw.Controller.ClawFrameChanged += faultCapture;
        try
        {
        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));
        }
        finally { claw.Controller.ClawFrameChanged -= faultCapture; }
        CaptureCompletedStepClip();
        TestAssert.Equal(PlayerAnimationClip.ThreadPreview, liveFormal.CurrentClip,
            "real coordinator presents the typed ThreadPreview pose for mechanical-claw Aim");
        bool failedAdmission = false;
        try { PrologueCanyonGeneratedMainRouteTests.Task10RequirePostHopObservation(
            captureAttempts, captureCompleted, captureFaults, 0); }
        catch (InvalidOperationException error) when (error.Message.Contains("MIXED_POST_HOP_OBSERVER_RED"))
        { failedAdmission = true; }
        GD.Print($"POST_HOP_OBSERVER_FAULT_MEASURE actual_preview={sampledActualPreview} attempts={captureAttempts} completed={captureCompleted} faults={captureFaults} rejected={failedAdmission} event_isolated=true human=false");
        TestAssert.True(sampledActualPreview && captureAttempts == 1 && captureCompleted == 0 &&
            captureFaults == 1 && failedAdmission,
            "POST_HOP_OBSERVER_FAULT_RED failed actual production-event observation cannot qualify diagnostic acceptance");
        int admissionCases = 0;
        foreach (var row in new (int Attempts, int Completed, int Faults, int Dropped, bool Accepted)[]
        {
            (0, 0, 0, 0, true), (0, 0, 1, 5, true), (1, 1, 0, 0, true),
            (1, 0, 0, 0, false), (1, 0, 1, 0, false), (1, 1, 1, 0, false),
            (1, 1, 0, 1, false), (1, 2, 0, 0, false), (2, 2, 0, 0, false),
            (-1, 0, 0, 0, false), (1, 1, -1, 0, false), (1, 1, 0, -1, false),
            (0, 1, 0, 0, false), (0, -1, 0, 0, false)
        })
        {
            bool accepted = true;
            try { PrologueCanyonGeneratedMainRouteTests.Task10RequirePostHopObservation(
                row.Attempts, row.Completed, row.Faults, row.Dropped); }
            catch (InvalidOperationException error) when (error.Message.Contains("MIXED_POST_HOP_OBSERVER_RED")) { accepted = false; }
            TestAssert.Equal(row.Accepted, accepted, "posthop admission obeys literal complete/partial/fault/drop/scope contracts");
            admissionCases++;
        }
        GD.Print($"POST_HOP_OBSERVER_FAULT_OK actual_event=true original_preview_preserved=true admission_cases={admissionCases} human=false");

        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        CaptureCompletedStepClip();
        TestAssert.Equal(MechanicalClawState.Windup, claw.Controller.CurrentFrame.State,
            "releasing the initial cast press does not cancel its windup");
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Latched, holdPrimary: false,
            completedStep: CaptureCompletedStepClip);
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        CaptureCompletedStepClip();
        TestAssert.Equal(MechanicalClawState.Reel, claw.Controller.CurrentFrame.State,
            "latched mechanical claw automatically reels before reaching the dock without a held primary");
        TestAssert.True(!claw.Controller.CurrentFrame.Arrived &&
            claw.Controller.CurrentFrame.CasterWorldPosition.DistanceTo(claw.Controller.CurrentFrame.DockPoint) > 8f,
            "the distant caster cannot claim arrival or Swing at the start of automatic Reel");
        TestAssert.Equal(PlayerAnimationClip.ThreadApply, liveFormal.CurrentClip,
            "automatic Reel presents the typed ThreadApply pose while the real body approaches");
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Swing, holdPrimary: false,
            completedStep: CaptureCompletedStepClip);
        AssertRealClawArrival(player, claw, castStart, "formal coordinator");
        ulong arrivedCastId = claw.Controller.CurrentFrame.CastId;
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        CaptureCompletedStepClip();
        TestAssert.Equal(MechanicalClawState.Swing, claw.Controller.CurrentFrame.State,
            "primary release preserves the arrived Swing attachment");
        TestAssert.Equal(arrivedCastId, claw.Controller.CurrentFrame.CastId,
            "primary release retains the same authoritative cast rather than silently recasting");
        await StepClaw(tree, claw, ClawInput(primaryPressed: true,
            mouseWorld: surface.WorldAnchor));
        CaptureCompletedStepClip();
        TestAssert.Equal(MechanicalClawState.Released, claw.Controller.CurrentFrame.State,
            "a second explicit primary press publishes the typed claw release phase");
        TestAssert.Equal(MechanicalClawReleaseKind.Cancel, claw.Controller.CurrentFrame.ReleaseKind,
            "a second primary press cancels without an unrequested boost");
        TestAssert.Equal(PlayerAnimationClip.ThreadApply, liveFormal.CurrentClip,
            "released claw retains the typed apply/recovery pose while retracting");

        MechanicalClawState[] expectedOrder =
                 {
                     MechanicalClawState.Aim,
                     MechanicalClawState.Windup,
                     MechanicalClawState.Travel,
                     MechanicalClawState.Latched,
                     MechanicalClawState.Reel,
                     MechanicalClawState.Swing,
                     MechanicalClawState.Released
                 };
        TestAssert.Equal(string.Join(",", expectedOrder), string.Join(",", observedOrder),
            "the real coordinator observes automatic Reel then arrived Swing before explicit release");
        foreach (MechanicalClawState state in expectedOrder)
        {
            TestAssert.True(observedStates.Contains(state),
                $"real coordinator observes the authoritative {state} state in order");
            PlayerAnimationClip expectedClip = state switch
            {
                MechanicalClawState.Aim => PlayerAnimationClip.ThreadPreview,
                MechanicalClawState.Windup => PlayerAnimationClip.ThreadWindup,
                MechanicalClawState.Travel => PlayerAnimationClip.ThreadThrow,
                MechanicalClawState.Latched => PlayerAnimationClip.ThreadLatch,
                _ => PlayerAnimationClip.ThreadApply
            };
            TestAssert.Equal(expectedClip, clipAtPhase[state],
                $"real coordinator owns the typed formal pose for {state}");
        }
        await SettleClawToIdle(tree, claw, surface.WorldAnchor);
        AssertCurrentAirborneLocomotionAfterClaw(player, claw, liveFormal, "natural retract exit");
        }
        finally
        {
            ResumeMechanicalClaw(claw);
            ReleaseInput();
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await Frames(tree, 1);
        }
    }

    private static async Task RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode(SceneTree tree)
    {
        await AssertFormalClawPoseClearsOnDetach(tree, attackMode: false);
        await AssertFormalClawPoseClearsOnDetach(tree, attackMode: true);
    }

    private static async Task AssertFormalClawPoseClearsOnDetach(SceneTree tree, bool attackMode)
    {
        ReleaseInput();
        string source = attackMode ? "attack-mode cancellation" : "second explicit primary press";
        var root = new Node2D
        {
            Name = attackMode ? "FormalAttackModeRetractFixture" : "FormalExplicitCancelFixture"
        };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        GrappleSurface2D surface = GrappleSurface("DetachSurface", new Vector2(180f, 0f));
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(surface);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        try
        {
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        ThreadLineVisual line = player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        ThreadShuttleVisual shuttle = player.GetNode<ThreadShuttleVisual>("VisualRoot/MechanicalClaw");
        Vector2 castStart = player.GlobalPosition;

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Swing, holdPrimary: false);
        AssertRealClawArrival(player, claw, castStart, source);
        TestAssert.Equal(PlayerAnimationClip.ThreadApply, formal.CurrentClip,
            $"real {source} setup presents the typed ThreadApply swing pose before detach");

        PlayerInputFrame detach = attackMode
            ? ClawInput(attackModePressed: true, primaryPressed: true,
                mouseWorld: surface.WorldAnchor)
            : ClawInput(primaryPressed: true, mouseWorld: surface.WorldAnchor);
        await StepClaw(tree, claw, detach);
        TestAssert.Equal(MechanicalClawState.Released, claw.Controller.CurrentFrame.State,
            $"real {source} publishes the authoritative Released edge");
        TestAssert.Equal(MechanicalClawReleaseKind.Cancel, claw.Controller.CurrentFrame.ReleaseKind,
            $"real {source} cancels rather than applying boost-release velocity");
        if (attackMode)
            TestAssert.True(!claw.Input.CurrentFrame.PrimaryPressed,
                "attack-mode cancellation consumes the conflicting primary edge instead of recasting");
        await SettleClawToIdle(tree, claw, surface.WorldAnchor);

        AssertCurrentAirborneLocomotionAfterClaw(player, claw, formal, source);
        TestAssert.True(line.ActiveStrandCount == 0 && shuttle.IsClawClear,
            $"real {source} clears line and shuttle on the authoritative Idle edge");
        TestAssert.Equal(MechanicalClawState.Idle, claw.Controller.CurrentFrame.State,
            $"real {source} leaves no active claw or gameplay cooldown state");

        }
        finally
        {
            ResumeMechanicalClaw(claw);
            ReleaseInput();
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await Frames(tree, 1);
        }
    }

    private static async Task RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase(SceneTree tree)
    {
        ReleaseInput();
        bool priorDiagnosticFallbacks = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        try
        {
        var root = new Node2D { Name = "MechanicalClawPlaceholderCoordinatorFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        GrappleSurface2D surface = GrappleSurface("PlaceholderSurface", new Vector2(180f, 0f));
        var placeholderHost = new Node2D { Name = "PlaceholderHost" };
        var placeholder = new PlayerPlaceholderVisual { Name = "Placeholder" };
        placeholderHost.AddChild(placeholder);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(surface);
        root.AddChild(placeholderHost);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        try
        {
        player.GetNode<PlayerPresentationCoordinator>("Presentation").BindVisualRoot(placeholderHost);
        Vector2 castStart = player.GlobalPosition;
        var geometryAtPhase = new Dictionary<MechanicalClawState, PlaceholderGeometry>();
        claw.Controller.ClawFrameChanged += (frame, target) =>
        {
            if (frame.State != MechanicalClawState.Idle)
                geometryAtPhase[frame.State] = ReadPlaceholderGeometry(placeholder);
        };

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));
        AssertThreadPlaceholderGeometry(ReadPlaceholderGeometry(placeholder),
            "real-coordinator mechanical-claw Aim");
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Latched, holdPrimary: false);
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(MechanicalClawState.Reel, claw.Controller.CurrentFrame.State,
            "placeholder coordinator observes automatic Reel rather than an unearned immediate Swing");
        TestAssert.True(!claw.Controller.CurrentFrame.Arrived,
            "placeholder Reel has not fabricated arrival for the distant real body");
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Swing, holdPrimary: false);
        AssertRealClawArrival(player, claw, castStart, "placeholder coordinator");
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(MechanicalClawState.Swing, claw.Controller.CurrentFrame.State,
            "placeholder attachment survives primary release after real arrival");
        await StepClaw(tree, claw, ClawInput(primaryPressed: true,
            mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(MechanicalClawState.Released, claw.Controller.CurrentFrame.State,
            "placeholder attachment releases only on the explicit second primary press");
        TestAssert.Equal(MechanicalClawReleaseKind.Cancel, claw.Controller.CurrentFrame.ReleaseKind,
            "placeholder explicit cancellation does not borrow boost-release semantics");

        foreach (MechanicalClawState state in new[]
                 {
                     MechanicalClawState.Aim,
                     MechanicalClawState.Windup,
                     MechanicalClawState.Travel,
                     MechanicalClawState.Latched,
                     MechanicalClawState.Reel,
                     MechanicalClawState.Swing,
                     MechanicalClawState.Released
                 })
        {
            TestAssert.True(geometryAtPhase.TryGetValue(state, out PlaceholderGeometry geometry),
                $"real placeholder coordinator publishes the {state} phase pose");
            AssertThreadPlaceholderGeometry(geometry, $"real placeholder {state}");
        }

        await SettleClawToIdle(tree, claw, surface.WorldAnchor);
        PlaceholderGeometry afterCast = ReadPlaceholderGeometry(placeholder);
        TestAssert.True(!afterCast.ThreadCastPoseActive,
            "placeholder leaves the ThreadCast pose as soon as the coordinator returns to Idle");
        TestAssert.True(afterCast.BodySize != new Vector2(46f, 48f),
            "placeholder restores its movement-driven body silhouette after casting");
        TestAssert.Equal(new Vector2(46f, 7f), afterCast.WeaponEnd,
            "placeholder restores its default weapon geometry after casting");

        }
        finally
        {
            ResumeMechanicalClaw(claw);
            ReleaseInput();
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await Frames(tree, 1);
        }
        }
        finally
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = priorDiagnosticFallbacks;
        }
    }

    private static async Task RealCoordinatorSustainsFormalAndPlaceholderFailedPoses(SceneTree tree)
    {
        ReleaseInput();
        bool priorDiagnosticFallbacks = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        try
        {
        var root = new Node2D { Name = "MechanicalClawFailedPresentationFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        var placeholderHost = new Node2D { Name = "FailedPlaceholderHost" };
        var placeholder = new PlayerPlaceholderVisual { Name = "Placeholder" };
        placeholderHost.AddChild(placeholder);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(placeholderHost);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        PlayerPresentationCoordinator coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        PlayerAnimationClip locomotionClipBeforeFailure = formal.CurrentClip;

        await FailClawWithoutTarget(tree, claw);
        TestAssert.Equal(MechanicalClawState.Failed, claw.Controller.CurrentFrame.State,
            "formal failure fixture publishes the typed no-target failure frame");
        TestAssert.Equal(PlayerAnimationClip.ThreadFailed, formal.CurrentClip,
            "formal visual presents the distinct ThreadFailed pose instead of locomotion");
        await StepClaw(tree, claw, ClawInput());
        TestAssert.Equal(MechanicalClawState.Idle, claw.Controller.CurrentFrame.State,
            "no-target failure clears directly without allocating a cooldown cast");
        TestAssert.Equal(locomotionClipBeforeFailure, formal.CurrentClip,
            "natural Failed exit clears the formal failure override on the authoritative Idle edge");

        coordinator.BindVisualRoot(placeholderHost);
        await FailClawWithoutTarget(tree, claw);
        TestAssert.True(placeholder.ThreadFailedPoseActive,
            "placeholder presents a distinct ThreadFailed pose for the typed failure frame");
        TestAssert.True(!placeholder.ThreadCastPoseActive,
            "placeholder failure pose is not false-greened by the normal ThreadCast pose");
        TestAssert.Equal(new Vector2(48f, 44f), placeholder.CurrentBodySize,
            "placeholder failure uses its recoiled procedural body silhouette");
        TestAssert.Equal(new Vector2(9f, -3f), placeholder.CurrentWeaponStart,
            "placeholder failure pulls the procedural needle back toward the spool");
        TestAssert.Equal(new Vector2(43f, 6f), placeholder.CurrentWeaponEnd,
            "placeholder failure keeps a short visibly broken needle silhouette");
        await StepClaw(tree, claw, ClawInput());
        TestAssert.True(!placeholder.ThreadFailedPoseActive && !placeholder.ThreadGlowActive,
            "placeholder clears the Failed pose and glow when the model returns to Idle");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
        }
        finally
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = priorDiagnosticFallbacks;
        }
    }

    private static PlaceholderGeometry ReadPlaceholderGeometry(PlayerPlaceholderVisual placeholder)
        => new(placeholder.ThreadCastPoseActive, placeholder.CurrentBodySize,
            placeholder.CurrentWeaponStart, placeholder.CurrentWeaponEnd);

    private static void AssertThreadPlaceholderGeometry(PlaceholderGeometry geometry, string phase)
    {
        TestAssert.True(geometry.ThreadCastPoseActive,
            $"{phase} owns a real ThreadCast pose state rather than only a glow timer");
        TestAssert.Equal(new Vector2(46f, 48f), geometry.BodySize,
            $"{phase} uses the compact braced ThreadCast body silhouette");
        TestAssert.Equal(new Vector2(13f, -7f), geometry.WeaponStart,
            $"{phase} raises the procedural thread weapon from the shoulder");
        TestAssert.Equal(new Vector2(72f, -21f), geometry.WeaponEnd,
            $"{phase} extends the procedural needle beyond the default weapon geometry");
    }

    private readonly record struct PlaceholderGeometry(
        bool ThreadCastPoseActive,
        Vector2 BodySize,
        Vector2 WeaponStart,
        Vector2 WeaponEnd);

    private static ClawHarness SuspendMechanicalClaw(CharacterBody2D player)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController controller =
            player.GetNode<MechanicalClawController>("MechanicalClaw");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        controller.SetPhysicsProcess(false);
        controller.ResetTransientState();
        return new ClawHarness(input, modes, controller);
    }

    private static void ResumeMechanicalClaw(ClawHarness claw)
    {
        SetInputFrame(claw.Input, ClawInput());
        claw.Controller.ResetTransientState();
        claw.Input.SetPhysicsProcess(true);
        claw.Modes.SetPhysicsProcess(true);
        claw.Controller.SetPhysicsProcess(true);
    }

    private static async Task StepClaw(SceneTree tree, ClawHarness claw, PlayerInputFrame frame)
    {
        SetInputFrame(claw.Input, frame);
        claw.Modes._PhysicsProcess(0d);
        claw.Controller._PhysicsProcess(1d / 60d);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task AdvanceClawTo(SceneTree tree, ClawHarness claw,
        GrappleSurface2D surface, MechanicalClawState expected, bool holdPrimary = true,
        Action completedStep = null)
    {
        for (int index = 0; index < 180; index++)
        {
            if (claw.Controller.CurrentFrame.State == expected) return;
            if (claw.Controller.CurrentFrame.State == MechanicalClawState.Failed)
                throw new InvalidOperationException(
                    $"Mechanical claw failed before {expected}: {claw.Controller.CurrentFrame.Failure}");
            await StepClaw(tree, claw, ClawInput(primaryHeld: holdPrimary,
                mouseWorld: surface.WorldAnchor));
            completedStep?.Invoke();
        }
        throw new InvalidOperationException(
            $"Timed out waiting for mechanical-claw state {expected}; " +
            $"current={claw.Controller.CurrentFrame.State} caster={claw.Controller.CurrentFrame.CasterWorldPosition} " +
            $"dock={claw.Controller.CurrentFrame.DockPoint} arrived={claw.Controller.CurrentFrame.Arrived}");
    }

    private static void AssertRealClawArrival(CharacterBody2D player, ClawHarness claw,
        Vector2 castStart, string source)
    {
        MechanicalClawFrame frame = claw.Controller.CurrentFrame;
        GD.Print($"FORMAL_CLAW_ARRIVAL_TRACE source={source} state={frame.State} " +
            $"caster={frame.CasterWorldPosition} body={player.GlobalPosition} dock={frame.DockPoint} " +
            $"arrived={frame.Arrived} travel_x={player.GlobalPosition.X - castStart.X}");
        TestAssert.Equal(MechanicalClawState.Swing, frame.State,
            $"{source} reaches Swing using real motor movement without a held primary or reel input");
        TestAssert.True(frame.Arrived && frame.CasterWorldPosition.DistanceTo(frame.DockPoint) <= 8f,
            $"{source} only enters Swing after the measured caster reaches the eight-pixel dock envelope");
        TestAssert.True(player.GlobalPosition.X - castStart.X > 100f,
            $"{source} physically travels over one hundred pixels toward the saved dock before Swing");
        TestAssert.True(player.GlobalPosition.DistanceTo(frame.DockPoint) <= 8f,
            $"{source} keeps the actual CharacterBody inside the arrival envelope");
        TestAssert.True(claw.Controller.CurrentTarget != null,
            $"{source} keeps its real target attached on arrival");
    }

    private static void AssertCurrentAirborneLocomotionAfterClaw(CharacterBody2D player,
        ClawHarness claw, PlayerFormalVisual formal, string source)
    {
        PlayerPresentationFrame frame = player.GetNode<PlayerPresentationCoordinator>("Presentation").LastPlayerFrame;
        GD.Print($"FORMAL_CLAW_CLEAR_TRACE source={source} claw={claw.Controller.CurrentFrame.State} " +
            $"body={player.GlobalPosition} grounded={player.IsOnFloor()} velocity_y={player.Velocity.Y} " +
            $"action={frame.Action.State} locomotion={frame.Locomotion.State} clip={formal.CurrentClip.Name}");
        TestAssert.Equal(MechanicalClawState.Idle, claw.Controller.CurrentFrame.State,
            $"real {source} completes retraction on the authoritative Idle edge");
        TestAssert.True(!player.IsOnFloor(),
            $"real {source} leaves this unchanged fixture's body above its floor rather than fabricating landing");
        TestAssert.True(float.IsFinite(player.Velocity.Y) && Mathf.Abs(player.Velocity.Y) < 80f,
            $"real {source} has a measured finite vertical speed inside the airborne Apex band");
        TestAssert.True(!frame.Base.Grounded,
            $"real {source} publishes the actual body's airborne grounding state");
        TestAssert.Equal(PlayerActionState.None, frame.Action.State,
            $"real {source} clears the claw action rather than retaining its apply pose");
        TestAssert.Equal(PlayerActionPhase.None, frame.Action.Phase,
            $"real {source} leaves no stale claw action phase");
        TestAssert.Equal(PlayerLocomotionState.Apex, frame.Locomotion.State,
            $"real {source} restores the current airborne Apex locomotion, not the initialization sample");
        TestAssert.Equal(PlayerAnimationClip.Apex, formal.CurrentClip,
            $"real {source} visibly restores the literal Apex clip for the current physical state");
    }

    private static async Task SettleClawToIdle(SceneTree tree, ClawHarness claw,
        Vector2 mouseWorld)
    {
        for (int index = 0; index < 180; index++)
        {
            if (claw.Controller.CurrentFrame.State == MechanicalClawState.Idle) return;
            await StepClaw(tree, claw, ClawInput(mouseWorld: mouseWorld));
        }
        throw new InvalidOperationException(
            $"Timed out retracting mechanical claw; current={claw.Controller.CurrentFrame.State}");
    }

    private static async Task FailClawWithoutTarget(SceneTree tree, ClawHarness claw)
    {
        claw.Controller.ResetTransientState();
        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: new Vector2(420f, -420f)));
        await StepClaw(tree, claw, ClawInput(primaryHeld: true,
            mouseWorld: new Vector2(420f, -420f)));
        TestAssert.Equal(MechanicalClawFailure.NoTarget, claw.Controller.CurrentFrame.Failure,
            "mechanical-claw failure fixture reports the typed no-target reason");
    }

    private static PlayerInputFrame ClawInput(bool grappleModePressed = false,
        bool attackModePressed = false, bool primaryPressed = false, bool primaryHeld = false,
        bool primaryReleased = false, Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, false, false, false, false, false,
            MouseWorld: mouseWorld,
            AttackModePressed: attackModePressed,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld,
            PrimaryReleased: primaryReleased);

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame) =>
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);

    private static GrappleSurface2D GrappleSurface(string name, Vector2 position)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 10f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task PressAttackPrimary(SceneTree tree)
    {
        Input.ActionPress(InputActions.SelectAttackMode);
        await Frames(tree, 1);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionPress(InputActions.PrimaryAction);
    }

    private static void ReleaseInput()
    {
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(InputActions.Dash);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.Shield);
    }

    private sealed record ClawHarness(PlayerInputReader Input,
        PlayerActionModeController Modes, MechanicalClawController Controller);
}
