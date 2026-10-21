using System.Threading.Tasks;
using Godot;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Five actual saved mounted sources; controlled positioning/timing, not traversal or final fog visuals.</summary>
public static class FogSteamSavedControllerRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level,"fog-saved-steam");
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
            tree.Root.AddChild(level); await Frames(tree,5);
            var cues=level.GetNodeOrNull<FogSteamReadabilityOverlay>("ProtectedCues/Steam");
            TestAssert.True(cues!=null,"saved formal parent must contain native steam protection on layer2");
            var layer=level.GetNode<CanvasLayer>("ProtectedCues");
            TestAssert.True(layer.Layer==2 && layer.FollowViewportEnabled && layer.FollowViewportScale==1f &&
                cues.PresentedJetCount==0 && cues.LastOcclusionFrame==null,"Title has no protected steam commands or stale frame");
            flow.StartNew(); await Frames(tree,5);
            var controller=level.GetNode<PrologueFogController>("FogController");
            ulong playerId=level.Player.GetInstanceId();
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(3460,3990)),"real stream adapter mounts original Basin for this controlled source fixture");
            await Frames(tree,8);
            var jet=level.CurrentSectorRoot.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            var trench=level.CurrentSectorRoot.GetNode<CanyonSteamDiverter>("Gameplay/Mechanisms/SteamDiverterTrench");
            jet.SetPhysicsProcess(false); trench.SetPhysicsProcess(false); trench.ResetForCheckpoint(); jet.EnableForNewRun();
            TestAssert.Equal(new Vector2(3460,4110),jet.GlobalPosition,"original standalone source remains at its saved world position");
            TestAssert.Equal(new Vector2(72,150),((RectangleShape2D)jet.Collision.Shape).Size,"original native standalone dimensions are unchanged");
            Position(level,jet.GlobalPosition+Vector2.Up*120f); jet.AdvanceRuntime(.96f); await Frames(tree,3);
            AssertCurrent(level,cues,controller,playerId,"standalone actual early warning");
            jet.AdvanceRuntime(.8f); await Frames(tree,2);
            TestAssert.True(jet.Frame.HazardActive && jet.Monitoring && !jet.Collision.Disabled,"saved original standalone really enters damaging phase");
            AssertCurrent(level,cues,controller,playerId,"standalone actual damaging phase");
            level.CurrentSectorRoot.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve").TryActivate(); await Frames(tree,2);
            TestAssert.True(jet.DisabledForRun && cues.PresentedJetCount==0,"actual paired valve withdraws the sole nearby standalone source");
            await Pair(tree,level,cues,controller,trench,new Vector2(3890,4110),new Vector2(4110,4110),playerId);
            // Descent is deliberately NOT an allowed checkpoint destination.
            // This presentation fixture relocates only its controlled player;
            // the ordinary stream physics selects the already authored sector.
            Position(level,new Vector2(1820,2420));
            await Frames(tree,8);
            TestAssert.Equal(PrologueSectorId.CanyonDescent,world.CurrentSector,"real ordinary stream selects original Descent without granting it a new checkpoint");
            var frozen=level.CurrentSectorRoot.GetNode<CanyonSteamDiverter>("Gameplay/Mechanisms/SteamDiverterFrozen");
            frozen.SetPhysicsProcess(false); frozen.ResetForCheckpoint();
            await Pair(tree,level,cues,controller,frozen,new Vector2(1660,2540),new Vector2(1820,2540),playerId);
            flow.ReturnToTitle(); await Frames(tree,2);
            TestAssert.True(cues.PresentedJetCount==0 && cues.LastOcclusionFrame==null && !level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
                "actual ReturnToTitle clears all five saved steam sources without enabling unfinished formal fog");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_FIVE_STEAM_OK pixels=false fixture=true traversal=false formal_visible=false");
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
    private static async Task Pair(SceneTree tree,PrologueLevelController level,FogSteamReadabilityOverlay cues,
        PrologueFogController controller,CanyonSteamDiverter pair,Vector2 leftPosition,Vector2 rightPosition,ulong playerId)
    {
        CanyonSteamJet left=pair.GetNode<CanyonSteamJet>("LeftJet"),right=pair.GetNode<CanyonSteamJet>("RightJet");
        TestAssert.Equal(leftPosition,left.GlobalPosition,"original saved left jet geometry is retained");
        TestAssert.Equal(rightPosition,right.GlobalPosition,"original saved right jet geometry is retained");
        Position(level,rightPosition+Vector2.Up*120f); pair.Advance(.8d); await Frames(tree,3);
        TestAssert.True(right.Frame.Telegraphing && left.DisabledForRun,"actual original pair publishes only selected right warning");
        AssertCurrent(level,cues,controller,playerId,"original selected right warning");
        right.Hide(); await Frames(tree,1); TestAssert.Equal(0,cues.PresentedJetCount,"saved collector withdraws hidden selected source, not its cached prior shape");
        right.Show(); await Frames(tree,1); AssertCurrent(level,cues,controller,playerId,"original selected right restored");
        pair.Advance(.7d); await Frames(tree,2);
        // The original pair deliberately exposes at most ONE phase boundary
        // per callback and discards overshoot. .8 then .7 reaches main warning,
        // not active; preserve that contract rather than changing its clock.
        TestAssert.True(right.Frame.Telegraphing && !right.Frame.HazardActive && right.Collision.Disabled && !right.Monitoring,
            "original pair exposes the complete harmless main warning before damaging activation");
        AssertCurrent(level,cues,controller,playerId,"original selected right main warning");
        pair.Advance(.41d); await Frames(tree,2);
        TestAssert.True(right.Frame.HazardActive && !right.Collision.Disabled && right.Monitoring,"actual selected right Area is damaging before its protected cue");
        AssertCurrent(level,cues,controller,playerId,"original selected right active");
        TestAssert.True(pair.TryReceiveThread(default),"actual saved pair switches through its ordinary receiver");
        await Frames(tree,2); TestAssert.True(pair.IsHandingOff && cues.PresentedJetCount==0,"actual disarmed handoff withdraws outgoing protected danger");
        pair.Advance(.61d); Position(level,leftPosition+Vector2.Up*120f); pair.Advance(.8d); await Frames(tree,2);
        TestAssert.True(left.Frame.Telegraphing && right.DisabledForRun,"actual saved left clock, not a forged visual phase, enters warning");
        AssertCurrent(level,cues,controller,playerId,"original selected left warning");
        var pause=level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open(); await Frames(tree,2);
        TestAssert.True(cues.PresentedJetCount==0 && cues.LastOcclusionFrame==null,"real Pause withdraws all mounted jet commands and provenance");
        pause.Execute(ProloguePauseCommand.Resume); await Frames(tree,2); AssertCurrent(level,cues,controller,playerId,"original left resumes from fresh frame");
        pair.ResetForCheckpoint(); await Frames(tree,2); TestAssert.Equal(0,cues.PresentedJetCount,"original reset disarms left and does not invent a warning for safe right");
    }
    private static void Position(PrologueLevelController level,Vector2 position)
    {
        level.Player.GlobalPosition=position; level.Player.Velocity=Vector2.Zero;
        level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
    }
    private static void AssertCurrent(PrologueLevelController level,FogSteamReadabilityOverlay cues,PrologueFogController controller,ulong playerId,string label)
    {
        TestAssert.True(cues.PresentedJetCount>0 && ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame) &&
            level.Player.GetInstanceId()==playerId && level.Player.GetNode<PlayerPresentationCoordinator>("Presentation").FrameObserverFailureCount==0 &&
            level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
            $"{label}: real mounted collector shares the exact current frame, same player and visible formal fog; jets={cues.PresentedJetCount} player={level.Player.GlobalPosition}");
    }
    private static async Task Frames(SceneTree tree,int count)
    {
        for(int i=0;i<count;i++)
        {
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        }
    }
}
