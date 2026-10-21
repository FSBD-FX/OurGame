using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Tests.Integration;

/// <summary>Saved parent and original door state machines; controlled poses/metadata, not natural traversal.</summary>
public static class FogBarrierSavedControllerRuntimeTests
{
    private static readonly string[] Fragments=["canyon.v2.fragment.entrance","canyon.v2.fragment.rift",
        "canyon.v2.fragment.abyss-floor","canyon.v2.fragment.gear-gallery","canyon.v2.fragment.ascent-mid","canyon.v2.fragment.courtyard"];
    public static async Task Run(SceneTree tree)
    {
        Stage("before-load-instantiate");
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        Stage("after-load-instantiate");
        try
        {
            IsolatedPrologueFixture.Configure(level,"fog-saved-barriers");
            Stage("after-isolation");
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow"); flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
            Stage("before-mount");
            tree.Root.AddChild(level);
            Stage("after-mount");
            await Frames(tree,5);
            Stage("title-rendered");
            var cues=level.GetNodeOrNull<FogBarrierReadabilityOverlay>("ProtectedCues/Barriers");
            TestAssert.True(cues!=null,"saved formal parent needs original current per-shape door and admission boundaries");
            TestAssert.True(cues.PresentedBodyCount==0 && cues.LastOcclusionFrame==null,"Title has no retained barrier provenance");
            flow.StartNew(); Stage("after-start-new"); await Frames(tree,5); Stage("playing-rendered"); ulong playerId=level.Player.GetInstanceId();
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var controller=level.GetNode<PrologueFogController>("FogController");
            Position(level,new Vector2(4340,1600)); await Frames(tree,8);
            TestAssert.Equal(PrologueSectorId.CanyonAscent,world.CurrentSector,"original ordinary streaming mounts actual long shortcut barrier");
            var shortcut=level.CurrentSectorRoot.GetNode<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
            var shortBody=shortcut.GetNode<StaticBody2D>("Barrier"); ulong retired=shortBody.GetInstanceId();
            await Included(tree,level,cues,controller,playerId,shortBody,"actual long shortcut");
            TestAssert.True(shortcut.TryReceiveThread(default),"original thread receiver opens the real shortcut"); await Frames(tree,2);
            TestAssert.True(shortcut.BarrierCollision.Disabled && !HasBody(controller,shortBody),"actual shortcut removal is committed in the shared native frame");
            TestAssert.Equal(0,cues.PresentedBodyCount,"open original shortcut has no ghost barrier paint");
            level.RestoreFromSave(Snapshot(0,false,false)); await Frames(tree,8);
            Position(level,new Vector2(5400,950)); await Frames(tree,5);
            var seal=level.CurrentSectorRoot.GetNode<CanyonSealGate>("Gameplay/Mechanisms/CourtyardSeal");
            await Included(tree,level,cues,controller,playerId,seal,"actual closed courtyard seal");
            level.Progress.ActivateMechanism(seal.RequiredMechanismId); level.Progress.ActivateMechanism(seal.RequiredEncounterId); await Frames(tree,2);
            TestAssert.True(seal.IsOpen && seal.Collision.Disabled && !HasBody(controller,seal),"original authored seal prerequisites remove its native boundary");
            Position(level,new Vector2(5748,924)); await Frames(tree,5);
            var chain=level.CurrentSectorRoot.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
            var bellBody=chain.GetNode<StaticBody2D>("Door");
            await Included(tree,level,cues,controller,playerId,bellBody,"actual closed bell chain door");
            TestAssert.True(chain.TryReceiveThread(default),"original bell receiver opens actual saved door"); await Frames(tree,2);
            TestAssert.True(chain.IsOpen && chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath).Disabled && !HasBody(controller,bellBody),
                "deferred chain collision commits before disappearance in fresh native frame");
            Position(level,new Vector2(5898,830)); await Frames(tree,5);
            var templePressure=level.CurrentSectorRoot.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/TemplePressureGate");
            TestAssert.True(!templePressure.IsOpen && !templePressure.DoorCollision.Disabled,"unpressed original Temple pressure door is actually solid");
            await Included(tree,level,cues,controller,playerId,templePressure.GetNode<StaticBody2D>("Door"),"actual closed Temple pressure door");
            Position(level,new Vector2(6060,880)); await Frames(tree,5);
            var admission=level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Doors/AdmissionBarrier");
            TestAssert.True(HasBody(controller,admission) && cues.PresentedBodyCount==0,
                "actor on the original supply right-wall slab cannot authorize a see-through admission cue");
            Position(level,new Vector2(6110,880)); await Frames(tree,5);
            await Included(tree,level,cues,controller,playerId,admission,"actual three-shape quota boundary");
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open(); await Frames(tree,2);
            TestAssert.True(cues.PresentedBodyCount==0 && cues.LastOcclusionFrame==null,"actual pause revokes current native door paint");
            pause.Execute(ProloguePauseCommand.Resume);
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false); await Frames(tree,2);
            await Included(tree,level,cues,controller,playerId,admission,"actual resume rebuilds quota boundary");
            // Valid isolated metadata, explicitly not a natural six-piece collection proof.
            level.RestoreFromSave(Snapshot(6,true,true)); await Frames(tree,8);
            var arena=level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            admission=level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Doors/AdmissionBarrier");
            TestAssert.True(!HasBody(controller,admission),"all admission native shapes disappear for actual restored six-piece prerequisites");
            arena.BeginEncounter(); arena.CurrentBoss.SetPhysicsProcess(false); await Frames(tree,2);
            Position(level,new Vector2(6060,1020)); await Frames(tree,5);
            await Included(tree,level,cues,controller,playerId,level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Doors/Entry"),"independent actual combat entry");
            Position(level,new Vector2(7250,1020)); await Frames(tree,5);
            var exit=level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Doors/Exit");
            await Included(tree,level,cues,controller,playerId,exit,"independent actual combat exit");
            level.Progress.MarkBossDefeated(); await Frames(tree,3);
            TestAssert.True(!HasBody(controller,admission) && !HasBody(controller,exit) && cues.PresentedBodyCount==0,
                "actual defeated-state restoration removes all three admission shapes and combat exit without ghost paint");
            Position(level,new Vector2(1660,1530)); await Frames(tree,8);
            var archive=level.CurrentSectorRoot.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/ArchivePressureGate");
            TestAssert.True(!archive.IsOpen && !archive.DoorCollision.Disabled,"unpressed original Archive pressure door is actually solid");
            await Included(tree,level,cues,controller,playerId,archive.GetNode<StaticBody2D>("Door"),"actual closed Archive pressure door");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonEntrance,new Vector2(180,260)),"legal remote checkpoint retires Ascent"); await Frames(tree,5);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonAscent,out _) && cues.PresentedBodyCount==0,
                "actually retired distant native barrier cannot paint Entrance");
            Position(level,new Vector2(4340,1600)); await Frames(tree,8);
            shortBody=level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Mechanisms/UpperBasinShortcutWinch/Barrier");
            TestAssert.True(shortBody.GetInstanceId()!=retired,"remount uses a new original shortcut body identity");
            await Included(tree,level,cues,controller,playerId,shortBody,"new original closed shortcut identity");
            flow.ReturnToTitle(); await Frames(tree,2);
            TestAssert.True(cues.PresentedBodyCount==0 && cues.LastOcclusionFrame==null,"ReturnToTitle clears all current barrier provenance");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_BARRIERS_OK pixels=false fixture=true traversal=false formal_visible=false");
        }
        finally
        {
            tree.Paused=false;
            if(GodotObject.IsInstanceValid(level)) { if(level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level); else level.Free(); }
        }
    }
    private static void Stage(string stage)=>GD.Print($"FOG_BARRIER_STAGE stage={stage} usec={Time.GetTicksUsec()} process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()}");
    private static bool HasBody(PrologueFogController controller,StaticBody2D body)
    { foreach(var shape in controller.LastOcclusionFrame.Shapes) if(shape.BodyInstanceId==body.GetInstanceId()) return true; return false; }
    private static async Task Included(SceneTree tree,PrologueLevelController level,FogBarrierReadabilityOverlay cues,
        PrologueFogController controller,ulong playerId,StaticBody2D body,string label)
    {
        Vector2 actor=level.Player.GlobalPosition;
        int count=cues.PresentedBodyCount;
        TestAssert.True(count>0 && HasBody(controller,body) && ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame) &&
            level.Player.GetInstanceId()==playerId && level.Player.GetNode<PlayerPresentationCoordinator>("Presentation").FrameObserverFailureCount==0 &&
            level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,$"{label}: current original body/frame; count={count} actor={level.Player.GlobalPosition}");
        // Native geometry stays untouched: withdrawing only this canonical source
        // must remove exactly one presented body, even among other visible doors.
        string name=body.Name;
        var pressure=body.GetParent() as CanyonPressurePlateGate;
        var bell=body.GetParent() as BellChainMechanism;
        NodePath pressurePath=pressure?.DoorPath, bellPath=bell?.DoorCollisionPath;
        // These original mechanisms resolve their door on every callback. Keep
        // their binding valid while withdrawing only the collector's canonical path.
        try
        {
            body.Name="UnboundBarrier";
            if(pressure!=null) pressure.DoorPath=new("UnboundBarrier");
            if(bell!=null) bell.DoorCollisionPath=new("UnboundBarrier/Collision");
            await Frames(tree,2);
            TestAssert.Equal(count-1,cues.PresentedBodyCount,label+": exact source withdrawal, not a retained/global body list");
        }
        finally
        {
            body.Name=name;
            if(pressure!=null) pressure.DoorPath=pressurePath;
            if(bell!=null) bell.DoorCollisionPath=bellPath;
        }
        await Frames(tree,2);
        TestAssert.Equal(actor,level.Player.GlobalPosition,label+": controlled pose stays fixed throughout canonical-path negative control");
        int hp=level.Player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth;
        var state=level.GetNode<PrologueGameFlowController>("GameFlow").State;
        TestAssert.Equal(count,cues.PresentedBodyCount,$"{label}: current canonical source resumes; native={HasBody(controller,body)} " +
            $"pressure_open={pressure?.IsOpen} disabled={pressure?.DoorCollision.Disabled} actor={level.Player.GlobalPosition} " +
            $"hp={hp} flow={state} body={body.GetPath()}");
    }
    private static PrologueSaveData Snapshot(int count,bool courtyard,bool bell)
    {
        var save=PrologueSaveData.NewLayoutV2(); save.CheckpointId="canyon.v2.temple-forecourt"; save.HasActivatedCheckpoint=true;
        save.CheckpointSector="LostTemple"; save.CheckpointWorldX=5920; save.CheckpointWorldY=1080;
        for(int index=0;index<count;index++) save.CollectedFragmentIds.Add(Fragments[index]);
        if(courtyard) save.ActivatedMechanisms.Add("canyon.v2.courtyard-seal-open"); if(bell) save.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
        return save.Normalize();
    }
    private static void Position(PrologueLevelController level,Vector2 position)
    {
        // RestoreFromSave and pause resume legitimately re-enable gameplay processing.
        level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        level.Player.GlobalPosition=position; level.Player.Velocity=Vector2.Zero;
        level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
    }
    private static async Task Frames(SceneTree tree,int count)
    { for(int i=0;i<count;i++) { await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame); await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw); } }
}
