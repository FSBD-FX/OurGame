using System.Threading.Tasks;
using Godot;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Saved parent, actual streaming and original current native bodies; controlled poses, not traversal.</summary>
public static class FogMovingGeometrySavedControllerRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level,"fog-saved-moving-geometry");
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow"); flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
            tree.Root.AddChild(level); await Frames(tree,5);
            var cues=level.GetNodeOrNull<FogMovingGeometryReadabilityOverlay>("ProtectedCues/MovingBodies");
            TestAssert.True(cues!=null,"saved formal parent needs current lift, piston and rotating pendulum native boundaries");
            TestAssert.True(cues.PresentedBodyCount==0 && cues.PresentedWarningCount==0 && cues.LastOcclusionFrame==null,"Title has no retained moving body or advance warning provenance");
            flow.StartNew(); await Frames(tree,5);
            ulong playerId=level.Player.GetInstanceId();
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var controller=level.GetNode<PrologueFogController>("FogController");
            Position(level,new Vector2(4840,3450)); await Frames(tree,8);
            TestAssert.Equal(PrologueSectorId.CanyonAscent,world.CurrentSector,"actual ordinary stream mounts original Ascent");
            var lift=level.CurrentSectorRoot.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/CounterweightLift");
            Current(level,cues,controller,playerId,"original inactive lift remains solid");
            ulong retired=lift.GetInstanceId(); Vector2 before=lift.GlobalPosition;
            TestAssert.True(lift.TryReceiveThread(default),"original receiver activates saved lift without a substitute clock");
            await Frames(tree,12);
            TestAssert.True(lift.GlobalPosition.Y<before.Y-30,"saved lift naturally advances native position");
            Current(level,cues,controller,playerId,"actual moving saved lift");
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open(); await Frames(tree,2);
            TestAssert.True(cues.PresentedBodyCount==0 && cues.LastOcclusionFrame==null,"actual pause revokes moving body commands");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree,2);
            Current(level,cues,controller,playerId,"actual resume reads new native frame");
            Position(level,new Vector2(4700,2190)); await Frames(tree,8);
            var pendulum=level.CurrentSectorRoot.GetNode<CanyonPendulumHazard>("Gameplay/Mechanisms/PendulumHazard");
            Transform2D old=pendulum.GetNode<CollisionShape2D>("Collision").GlobalTransform;
            Current(level,cues,controller,playerId,"original saved pendulum with other terrain present");
            await Frames(tree,20);
            TestAssert.True(old.X.DistanceTo(pendulum.GetNode<CollisionShape2D>("Collision").GlobalTransform.X)>.1f,
                "actual saved pendulum rotates with its original physics callback");
            Current(level,cues,controller,playerId,"new rotated saved pendulum");
            for(int i=0;i<180 && !pendulum.Frame.Telegraphing;i++) await Frames(tree,1);
            Warning(level,cues,controller,playerId,pendulum,"saved original pendulum advance warning");
            pause.Open(); await Frames(tree,2);
            TestAssert.True(cues.PresentedWarningCount==0 && cues.LastOcclusionFrame==null,"saved pause revokes original advance warning");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree,2);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(2600,3760)),
                "original legal Basin checkpoint path mounts the saved piston"); await Frames(tree,8);
            var piston=level.CurrentSectorRoot.GetNode<CanyonGearPiston>("Gameplay/Mechanisms/GearPiston");
            for(int i=0;i<180 && !piston.Frame.Telegraphing;i++) await Frames(tree,1);
            Warning(level,cues,controller,playerId,piston,"saved original piston advance warning");
            TestAssert.True(piston.Collision.Disabled && cues.PresentedBodyCount==0,"saved warning is not a solid landing promise");
            for(int i=0;i<210 && !piston.Frame.HazardActive;i++) await Frames(tree,1);
            TestAssert.True(piston.Frame.HazardActive && !piston.Collision.Disabled,"actual native piston enters active collision naturally");
            TestAssert.Equal(0,cues.PresentedWarningCount,"original active piston no longer presents advance warning");
            Current(level,cues,controller,playerId,"original saved active piston");
            for(int i=0;i<80 && piston.Frame.HazardActive;i++) await Frames(tree,1);
            TestAssert.True(!piston.Frame.HazardActive && cues.PresentedBodyCount==0,"actual native recovery leaves no ghost solid piston");
            // Return through original streaming, not a retained cross-sector source list.
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,new Vector2(5898,924)),"actual Temple stream retains its legal neighbor"); await Frames(tree,5);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonAscent,out _) && cues.PresentedBodyCount==0,
                "legally mounted but distant Ascent neighbor does not paint the current Temple");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonEntrance,new Vector2(180,260)),"actual remote Entrance checkpoint retires Ascent"); await Frames(tree,5);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonAscent,out _) && cues.PresentedBodyCount==0,
                "actually retired moving bodies cannot paint the Entrance");
            Position(level,new Vector2(4840,3450)); await Frames(tree,8);
            lift=level.CurrentSectorRoot.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/CounterweightLift");
            TestAssert.True(lift.GetInstanceId()!=retired,"remount creates a new original lift identity");
            Current(level,cues,controller,playerId,"fresh Ascent identity after remount");
            flow.ReturnToTitle(); await Frames(tree,2);
            TestAssert.True(cues.PresentedBodyCount==0 && cues.PresentedWarningCount==0 && cues.LastOcclusionFrame==null && !level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
                "ReturnToTitle clears all moving native provenance and formal fog stays hidden");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_MOVING_GEOMETRY_OK pixels=false fixture=true traversal=false advance_warnings=true formal_playing_visible=true final_title_visible=false");
        }
        finally
        {
            tree.Paused=false;
            if(GodotObject.IsInstanceValid(level))
            { if(level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level); else level.Free(); }
        }
    }
    private static void Warning(PrologueLevelController level,FogMovingGeometryReadabilityOverlay cues,
        PrologueFogController controller,ulong playerId,AnimatableBody2D body,string message)
    {
        NodePath path=body is CanyonGearPiston p ? p.TelegraphPath : ((CanyonPendulumHazard)body).TelegraphPath;
        Polygon2D source=body.GetNode<Polygon2D>(path);
        TestAssert.True(((ICanyonTelegraphSource)body).IsDangerTelegraphActive && source.IsVisibleInTree() &&
            cues.PresentedWarningCount==1 && cues.PresentedWarningSegmentCount>0 &&
            ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame) && level.Player.GetInstanceId()==playerId &&
            level.GetNode<PrologueFogCanvas>("Fog/Canvas").IsVisibleInTree(),
            $"{message}: same original source/player/current native frame; count={cues.PresentedWarningCount}");
    }
    private static void Current(PrologueLevelController level,FogMovingGeometryReadabilityOverlay cues,
        PrologueFogController controller,ulong playerId,string message)=>
        TestAssert.True(cues.PresentedBodyCount==1 && ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame) &&
            level.Player.GetInstanceId()==playerId && level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
            $"{message}: exact same current Player/frame, one native source; count={cues.PresentedBodyCount} actor={level.Player.GlobalPosition}");
    private static void Position(PrologueLevelController level,Vector2 position)
    { level.Player.GlobalPosition=position; level.Player.Velocity=Vector2.Zero; level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing(); }
    private static async Task Frames(SceneTree tree,int count)
    { for(int i=0;i<count;i++) { await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame); await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw); } }
}
