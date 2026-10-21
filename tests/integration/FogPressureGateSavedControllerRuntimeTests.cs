using System.Threading.Tasks;
using Godot;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Saved parent and both original live doors; controlled player poses, not traversal.</summary>
public static class FogPressureGateSavedControllerRuntimeTests
{
    // Catches a missing saved leaf/collector, a retained retired source, and
    // stale paint after real pause, plate reentry, reset or title transition.
    public static async Task Run(SceneTree tree)
    {
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level,"fog-saved-pressure-gates");
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
            tree.Root.AddChild(level); await Frames(tree,5);
            var cues=level.GetNodeOrNull<FogPressureGateReadabilityOverlay>("ProtectedCues/PressureGates");
            TestAssert.True(cues!=null,"saved formal parent needs the original closing pressure gate countdown above fog");
            var layer=level.GetNode<CanvasLayer>("ProtectedCues");
            TestAssert.True(layer.Layer==2 && layer.FollowViewportEnabled && layer.FollowViewportScale==1f &&
                cues.PresentedGateCount==0 && cues.LastOcclusionFrame==null,"Title has no closing warning or retained native frame");
            flow.StartNew(); await Frames(tree,5);
            ulong playerId=level.Player.GetInstanceId();
            var motor=level.Player.GetNode<PlayerMotor>("Motor"); motor.SetPhysicsProcess(false);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var controller=level.GetNode<PrologueFogController>("FogController");
            Position(level,new Vector2(1660,1604)); await Frames(tree,8);
            TestAssert.Equal(PrologueSectorId.CanyonDescent,world.CurrentSector,
                "ordinary stream selects original Descent without adding a checkpoint destination");
            var archive=level.CurrentSectorRoot.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/ArchivePressureGate");
            TestAssert.Equal(new Rect2(1720,1424,40,216),archive.GetNode<CanyonPressureGateVisual>("ReadableGate").ClosedSlabWorldRect,
                "saved Archive door remains the original native footprint");
            await Warning(tree,level,archive,cues,controller,playerId);
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open();
            float pausedPhase=archive.Frame.Phase01; await Frames(tree,2);
            TestAssert.True(cues.PresentedGateCount==0 && cues.LastOcclusionFrame==null && archive.Frame.Phase01==pausedPhase,
                "actual pause clears warning provenance and freezes the original clock");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree,2);
            Current(level,cues,controller,playerId,"actual Archive resume");
            archive.ResetForCheckpoint(); await Frames(tree,2);
            TestAssert.Equal(0,cues.PresentedGateCount,"actual checkpoint reset withdraws the mounted warning");
            ulong retired=archive.GetInstanceId();
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,new Vector2(5898,924)),
                "original legal Temple checkpoint stream mounts the other actual door");
            await Frames(tree,8);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonDescent,out _),
                "Temple transition retires the original Archive source");
            var temple=level.CurrentSectorRoot.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/TemplePressureGate");
            TestAssert.Equal(new Rect2(5930,760,30,200),temple.GetNode<CanyonPressureGateVisual>("ReadableGate").ClosedSlabWorldRect,
                "saved Temple door remains its original native footprint");
            var chain=level.CurrentSectorRoot.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
            TestAssert.True(!chain.IsOpen && !chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath).Disabled,
                "saved Temple starts with the original solid BellChain door between fixture actor and warning");
            await Warning(tree,level,temple,cues,controller,playerId,false);
            TestAssert.True(!controller.LastOcclusionFrame.CanSee(level.Player.GlobalPosition,new Vector2(5945,860)),
                "current native BellChain really blocks the warning centerline, not just its renderer");
            TestAssert.True(chain.TryReceiveThread(default),"original receiver, not a forced collision flag, opens the BellChain fixture");
            await Frames(tree,2);
            TestAssert.True(chain.IsOpen && chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath).Disabled,
                "the actual BellChain deferred native opening commits before the cue becomes visible");
            Current(level,cues,controller,playerId,"actual Temple warning after original chain opening");
            Position(level,new Vector2(5898,924)); motor.SetPhysicsProcess(true); await Frames(tree,8); motor.SetPhysicsProcess(false);
            TestAssert.True(level.Player.IsOnFloor() && temple.IsOpen && !temple.IsDangerTelegraphActive && cues.PresentedGateCount==0,
                "grounded actual plate reentry cancels warning without closing the real door");
            Position(level,new Vector2(1660,1604)); await Frames(tree,8);
            archive=level.CurrentSectorRoot.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/ArchivePressureGate");
            TestAssert.True(archive.GetInstanceId()!=retired,"remount uses a fresh actual Archive identity");
            await Warning(tree,level,archive,cues,controller,playerId);
            flow.ReturnToTitle(); await Frames(tree,2);
            TestAssert.True(cues.PresentedGateCount==0 && cues.LastOcclusionFrame==null && !level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
                "ReturnToTitle revokes all countdown paint and leaves unfinished formal fog hidden");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_TWO_PRESSURE_GATES_OK pixels=false fixture=true traversal=false formal_visible=false");
        }
        finally
        {
            tree.Paused=false;
            if(GodotObject.IsInstanceValid(level))
            {
                if(level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
                else level.Free();
            }
        }
    }
    private static async Task Warning(SceneTree tree,PrologueLevelController level,CanyonPressurePlateGate gate,
        FogPressureGateReadabilityOverlay cues,PrologueFogController controller,ulong playerId,bool visible=true)
    {
        var motor=level.Player.GetNode<PlayerMotor>("Motor");
        Vector2 plate=new(gate.PlateWorldRect.GetCenter().X,gate.PlateWorldRect.End.Y-36f);
        Position(level,plate); motor.SetPhysicsProcess(true); await Frames(tree,8); motor.SetPhysicsProcess(false);
        TestAssert.True(level.Player.IsOnFloor() && gate.IsOpen,"same original Player really grounds on the saved plate");
        Position(level,plate-Vector2.Right*150f); await Frames(tree,90);
        TestAssert.True(gate.IsDangerTelegraphActive && gate.DoorCollision.Disabled && !gate.Frame.HazardActive,
            "actual unmodified grace naturally enters harmless closing warning");
        if(visible) Current(level,cues,controller,playerId,"actual natural closing warning");
        else TestAssert.True(cues.PresentedGateCount==0 && ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame),
            "actual closed chain occludes all original Temple closing paint in the current shared frame");
    }
    private static void Current(PrologueLevelController level,FogPressureGateReadabilityOverlay cues,
        PrologueFogController controller,ulong playerId,string label)
    {
        TestAssert.True(cues.PresentedGateCount==1 && ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame) &&
            level.Player.GetInstanceId()==playerId && level.Player.GetNode<PlayerPresentationCoordinator>("Presentation").FrameObserverFailureCount==0 &&
            level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
            $"{label}: one actual current door, exact shared frame, same player; count={cues.PresentedGateCount} player={level.Player.GlobalPosition}");
    }
    private static void Position(PrologueLevelController level,Vector2 position)
    { level.Player.GlobalPosition=position; level.Player.Velocity=Vector2.Zero; level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing(); }
    private static async Task Frames(SceneTree tree,int count)
    { for(int i=0;i<count;i++) { await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame); await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw); } }
}
