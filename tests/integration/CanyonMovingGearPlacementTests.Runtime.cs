using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class CanyonMovingGearPlacementTests
{
    // A saved parent and real stream/PlayerMotor/InputMap, with a declared
    // pre-tree controlled start. No live pose, velocity, HP or clock writes.
    private static async Task RunCandidateRuntime(SceneTree tree,string parentPath)
    {
        for(int variant=0;variant<2;variant++)
        {
            var f=new GalleryFixture(tree,parentPath,variant);
            try
            {
                await f.Start();
                if(variant==0)
                {
                    await f.Until(()=>f.Gear.CurrentMotion.Phase01 is >.47 and <.53,240,"actual lower turnaround");
                    f.Submit(); f.AssertCue(true);
                    using var wallShape=new RectangleShape2D {Size=new(8,66)};
                    var wall=new StaticBody2D {Name="CurrentCircleOnlyFogWall",Position=new(3360,3641),CollisionLayer=CombatCollisionLayers.World};
                    wall.AddChild(new CollisionShape2D {Shape=wallShape});
                    f.Level.GetNode<Node2D>("WorldRoot").AddChild(wall);
                    try
                    {
                        await f.Frames(2); f.Submit();
                        TestAssert.True(f.Fog.LastOcclusionFrame.CanSeeSegment(f.Player.GlobalPosition,
                            new(3422.9f,3550),new(3422,3556)),"negative wall still exposes a segment at the original A circle");
                        f.AssertCue(false);
                    }
                    finally { wall.QueueFree(); await f.Frames(2); }
                    f.Submit(); f.AssertCue(true);
                    f.Gear.Visible=false; f.Submit(); f.AssertCue(false);
                    f.Gear.Visible=true; f.Submit(); f.AssertCue(true);
                    var collision=f.Gear.GetNode<CollisionShape2D>("Collision");
                    collision.Disabled=true; await f.Frames(2); f.Submit(); f.AssertCue(false);
                    collision.Disabled=false; await f.Frames(2); f.Submit(); f.AssertCue(true);
                    tree.Paused=true;
                    try
                    {
                        var before=f.Gear.CurrentMotion;
                        f.Submit();
                        TestAssert.True(f.Overlay.PresentedSegmentCount==0 && f.Overlay.LastOcclusionFrame==null &&
                            !f.Level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible && f.Gear.CurrentMotion==before,
                            "paused actual controller clears all commands without advancing the source");
                    }
                    finally { tree.Paused=false; }
                    f.Submit(); f.AssertCue(true);
                    await f.WaitForOpening(); await f.Walk(3490); f.AssertSafe();
                    await f.Frames(12); f.AssertSupportAndCamera();
                    await f.WaitForOpening(); await f.Walk(3310); f.AssertSafe();
                    await f.Frames(12); f.AssertSupportAndCamera();
                    f.Gear.QueueFree(); f.Submit(); f.AssertCue(false);
                    await f.Frames(2);
                    TestAssert.True(!GodotObject.IsInstanceValid(f.Gear),"queued saved gear really leaves the native tree");
                    f.Submit(); f.AssertCue(false); f.AssertSafe();
                    GD.Print("MOVING_GEAR_SAVED_CONTROLLER_OK actual_saved_parent=true staged_ready=true current_circle_wall=true original_circle_exposed=true hidden_disabled=true pause_queue=true manual_predraw=true pixels=false human=false");
                    GD.Print("MOVING_GEAR_SAFE_PASSAGE_OK trips=2 real_keyboard=true actual_support=true full_capsule_camera=true damage=0 recoveries=0 jump=0 dash=0 claw=0 controlled_start=true natural_full_route=false human=false");
                }
                else
                {
                    await f.Until(()=>f.Gear.CurrentMotion.Phase01 is >.34 and <.37,240,"closed-window approach");
                    f.Keyboard.Command(InputActions.MoveRight,true);
                    await f.Until(()=>f.Hits.Count>0,90,"wrong timing must meet the real moving circle");
                    f.Keyboard.ReleaseAll();
                    TestAssert.True(f.Hits.Count==1 && f.Hits[0].SourceRuntimeId==f.Gear.GetInstanceId() &&
                        f.Hits[0].LocalAttackId>0 && f.Health.CurrentHealth==f.Health.MaxHealth-2 &&
                        f.Player.GlobalPosition.X is >3340 and <3445 && f.ObserverFailure==null,
                        "wrong timing produces one real saved-gear identity and exactly configured2 damage, not a recovery shortcut");
                    await f.Walk(3310);
                    TestAssert.True(f.Hits.Count==1 && f.Health.CurrentHealth==f.Health.MaxHealth-2,"retreat after one bad approach is recoverable without a second hit");
                    f.AssertSupportAndCamera();
                    GD.Print("MOVING_GEAR_BAD_TIMING_OK source=saved_gear accepted_hits=1 damage=2 recovery=0 retreat=true real_keyboard=true controlled_start=true human=false");
                }
            }
            finally { tree.Paused=false; await f.Dispose(); }
        }
    }

    private sealed class GalleryFixture
    {
        private readonly SceneTree _tree;
        internal readonly PrologueLevelController Level;
        internal readonly CharacterBody2D Player;
        internal readonly PlayerMotor Motor;
        internal readonly HealthComponent Health;
        internal readonly SeamlessPrologueWorld World;
        internal readonly KeyboardRouteInputDriver Keyboard;
        internal readonly PrologueFogController Fog;
        internal readonly FogRecyclingReadabilityOverlay Overlay;
        internal CanyonMovingGear Gear;
        internal readonly List<DamageAttackIdentity> Hits=new();
        internal string ObserverFailure;
        private readonly ulong _playerId;
        private int _recoveries;
        private PrologueSector _basin;
        private StaticBody2D _support;
        private readonly Queue<string> _recent=new();

        internal GalleryFixture(SceneTree tree,string path,int variant)
        {
            _tree=tree;
            Level=GD.Load<PackedScene>(path).Instantiate<PrologueLevelController>();
            Player=Level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            Player.Position=new(3310,3640); // The only position assignment, before AddChild.
            _playerId=Player.GetInstanceId();
            Motor=Player.GetNode<PlayerMotor>("Motor"); Health=Player.GetNode<HealthComponent>("Health");
            World=Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            World.InitialSector=PrologueSectorId.CanyonBasin;
            var flow=Level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart=false; flow.AutomationAutoStart=true;
            IsolatedPrologueFixture.Configure(Level,"moving-gallery-"+variant);
            Keyboard=KeyboardRouteInputDriver.Begin(Player);
            Fog=Level.GetNode<PrologueFogController>("FogController");
            Overlay=Level.GetNode<FogRecyclingReadabilityOverlay>("ProtectedCues/Recycling");
            Health.Damaged+=OnDamaged; Level.RecoveryRequested+=OnRecovery;
        }
        internal async Task Start()
        {
            _tree.Root.AddChild(Level); await Frames(8);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(_tree,Level);
            TestAssert.True(World.TryGetMountedSector(PrologueSectorId.CanyonBasin,out _basin),"candidate uses the real staged Basin mount");
            Gear=_basin.GetNode<CanyonMovingGear>(GearPath);
            TestAssert.True(Gear.HasValidMotionGeometry && Gear.IsDangerTelegraphActive &&
                Gear.GlobalPosition.DistanceTo(new Vector2(3400,3550)+Gear.CurrentMotion.Offset)<.01f,
                "disabled-before-Ready streaming activates the actual saved moving Area");
            var reservation=ResidentEnvironmentInventory.Capture(World).Regions.Single(r=>r.SourceId.StartsWith("moving-gear:",StringComparison.Ordinal));
            TestAssert.Equal(new Rect2(3376,3526,48,128),reservation.WorldBounds,"mounted inventory reserves the complete future rail");
            _support=BasinAscentCameraRuntimeTests.ResolveAuthoredSupport(World,PrologueSectorId.CanyonBasin,
                "UpperGearGalleryTwo",new(3280,3676,240,40));
            await Until(()=>Player.IsOnFloor(),90,"actual capsule settles on gallery");
            AssertSupportAndCamera(); AssertSafe(); Submit(); AssertCue(true);
        }
        internal async Task Frame()
        {
            await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);
            await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            Keyboard.ObserveFrame();
            _recent.Enqueue($"x={Player.GlobalPosition.X} y={Player.GlobalPosition.Y} v={Player.Velocity} hp={Health.CurrentHealth} floor={Player.IsOnFloor()} phase={(GodotObject.IsInstanceValid(Gear)?Gear.CurrentMotion.Phase01:-1)}");
            if(_recent.Count>12) _recent.Dequeue();
            TestAssert.True(Player.GlobalPosition.IsFinite() && Player.GetInstanceId()==_playerId &&
                Level.Player==Player && Health.CurrentHealth>0 && _recoveries==0 &&
                Motor.AcceptedJumpSequence==0 && Motor.AcceptedDashCount==0 && Keyboard.ClawEdges==0 &&
                World.CurrentSector==PrologueSectorId.CanyonBasin,
                "gallery test cannot use relocation, ordinary air jump, dash, claw or recovery: "+string.Join(";",_recent));
        }
        internal async Task Frames(int n) { for(int i=0;i<n;i++) await Frame(); }
        internal async Task Until(Func<bool> condition,int cap,string reason)
        { for(int i=0;i<cap && !condition();i++) await Frame(); TestAssert.True(condition(),reason+": "+string.Join(";",_recent)); }
        internal async Task WaitForOpening()=>await Until(()=>Gear.CurrentMotion.Phase01 is >.82 and <.86,240,"visible rising opening");
        internal async Task Walk(float target)
        {
            for(int i=0;i<180;i++)
            {
                float projected=Player.GlobalPosition.X+Player.Velocity.X*Mathf.Abs(Player.Velocity.X)/(2*2800f);
                Keyboard.Command(InputActions.MoveRight,projected<target-2);
                Keyboard.Command(InputActions.MoveLeft,projected>target+2);
                await Frame();
                if(Mathf.Abs(Player.GlobalPosition.X-target)<5 && Mathf.Abs(Player.Velocity.X)<15)
                { Keyboard.ReleaseAll(); return; }
            }
            Keyboard.ReleaseAll(); TestAssert.True(false,"bounded natural walk cannot reach "+target+": "+string.Join(";",_recent));
        }
        internal void Submit()=>RenderingServer.Singleton.EmitSignal(RenderingServer.SignalName.FramePreDraw);
        internal void AssertCue(bool expected)
        {
            TestAssert.True(Fog.LastOcclusionFrame is {IsComplete:true} &&
                ReferenceEquals(Overlay.LastOcclusionFrame,Fog.LastOcclusionFrame) &&
                Overlay.PresentedGearCount==(expected?1:0),"real controller must submit only the current mounted circle; expected="+expected+" count="+Overlay.PresentedGearCount);
            if(expected) TestAssert.True(Overlay.PresentedSegmentCount>0 && Level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
                "protected current circle commands accompany the enabled real fog canvas");
        }
        internal void AssertSafe()=>TestAssert.True(Health.CurrentHealth==Health.MaxHealth && Hits.Count==0 &&
            _recoveries==0 && ObserverFailure==null,"two-way safe window cannot spend HP or recover: "+string.Join(";",_recent));
        internal void AssertSupportAndCamera()
        {
            TestAssert.True(Player.IsOnFloor() && Player.GlobalPosition.X is >3300 and <3500,
                "the complete capsule remains over the original240px gallery");
            bool contact=false;
            for(int i=0;i<Player.GetSlideCollisionCount();i++)
            { using var c=Player.GetSlideCollision(i); contact|=c.GetCollider()==_support && c.GetNormal().Y<-.7f; }
            TestAssert.True(contact,"real upward native contact names the unchanged gallery");
            var body=Player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(body.Shape is CapsuleShape2D {Radius:20,Height:72},"unchanged full40x72 capsule");
            Transform2D canvas=body.GetGlobalTransformWithCanvas();
            Vector2 size=Player.GetViewportRect().Size;
            foreach(Vector2 corner in new[]{new Vector2(-20,-36),new(20,-36),new(20,36),new(-20,36)})
            {
                Vector2 p=canvas*corner;
                TestAssert.True(p.X>=16 && p.Y>=16 && p.X<=size.X-16 && p.Y<=size.Y-16,
                    "actual camera keeps complete capsule visible while gear has negative sector-local y");
            }
            var camera=Player.GetNode<Camera2D>("Camera2D");
            TestAssert.True(camera.Enabled && camera.LimitLeft==0 && camera.LimitTop==0 &&
                camera.LimitRight==7680 && camera.LimitBottom==4320,"do not enlarge camera bounds to rescue the new placement");
        }
        private void OnRecovery(string _) { _recoveries++; }
        private void OnDamaged(int _,Vector2 __)
        {
            try
            {
                var field=typeof(HealthComponent).GetField("_processedAttackOrder",BindingFlags.NonPublic|BindingFlags.Instance);
                Hits.Add(((Queue<DamageAttackIdentity>)field.GetValue(Health)).Last());
            }
            catch(Exception error) { ObserverFailure=error.ToString(); }
        }
        internal async Task Dispose()
        {
            try { Keyboard.Dispose(); Health.Damaged-=OnDamaged; Level.RecoveryRequested-=OnRecovery; }
            finally
            {
                if(GodotObject.IsInstanceValid(Level))
                { if(Level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree,Level); else Level.Free(); }
            }
        }
    }
}
