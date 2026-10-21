using Godot;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Presentation.Fog;
using Knight.Combat;
using Knight.Ui;
using Knight.Tests.Presentation;
using Knight.Player;
using Knight.Boss;
using Knight.Save;
using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Knight.Tests.Integration;

public static class MinimapFormalTests
{
    public static async Task Presentation(SceneTree tree)
    {
        TestAssert.True(DisplayServer.GetName()=="Windows","presentation needs actual hardware window");
        var level=ResourceLoader.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn",cacheMode:ResourceLoader.CacheMode.Ignore).Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"formal-presentation");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        tree.Root.AddChild(level);await Frames(tree,5);flow.StartNew();
        var map=level.GetNode<PrologueMinimapController>("MinimapController");var view=level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap");
        var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        try
        {
            GD.Print($"MINIMAP_PRESENTATION_CONTEXT renderer={RenderingServer.GetCurrentRenderingMethod()} device={RenderingServer.GetVideoAdapterName()} display={DisplayServer.GetName()} window={tree.Root.Size} fixed_rate=60 deterministic_presentation_only=True performance=False hardware_input=False controlled_checkpoint_staging=True");
            async Task Shot(string stage,int frames)
            {
                await Frames(tree,frames);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                tree.Root.GetTexture().GetImage().SavePng(Path.Combine(paths.CaseRoot,stage+".png"));
                GD.Print($"MINIMAP_PRESENTATION_STAGE stage={stage} actor={level.Player.GlobalPosition} alive={level.Player.GetNode<HealthComponent>("Health").CurrentHealth} generation={map.WorldGeneration} revision={map.Model.RevisionCounter} view={view.ViewWorldRect} visible={view.Visible} cache={view.CacheWorldRect} boundaryOverflow={view.BoundaryQueryOverflow}");
            }
            await Shot("01-entrance-unknown",60);
            string old=map.Model.CaptureSave().FreeCellsBase64;
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);await Shot("02-following-authorized-lines",35);KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
            TestAssert.True(level.Player.GlobalPosition.X>180,"presentation original D moves living actor before the initial ledge; no fall/reset substituted for following");
            await Shot("03-original-hud",60);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(1680,3960)),"presentation controlled authored Basin checkpoint");
            await Shot("04-controlled-basin",70);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonEntrance,new Vector2(160,600)),"presentation controlled old route return");
            await Shot("05-old-route-retained",70);Contains(old,map.Model.CaptureSave().FreeCellsBase64,"presentation return retains actual old explored bits");
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu");pause.Open();await Shot("06-pause-hidden",30);
            pause.Execute(ProloguePauseCommand.Resume);await Shot("07-resumed-map",30);
            TestAssert.True(view.Visible&&!view.BoundaryQueryOverflow,"actual saved presentation remains bounded and visible");
            GD.Print("MINIMAP_PRESENTATION_OK actual_saved_catalog=True same_player=True controlled_checkpoint_staging=True original_simulation_rate=True normal_time=False renderer_parity=False");
        }
        finally {KeyboardActionRuntimeTests.KeyEvent(Key.D,false);tree.Paused=false;await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
    }
    public static async Task Run(SceneTree tree, bool measurePerformance = false, bool profileOnly = false, bool oracle = false, string oraclePath = null)
    {
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn", cacheMode: ResourceLoader.CacheMode.Ignore);
        var level = scene.Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level,"formal-saved-map");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;
        try
        {
            var controller = level.GetNodeOrNull<PrologueMinimapController>("MinimapController");
            TestAssert.True(controller?.Catalog is { CellCount: 518400, BoundaryCount: > 0 },
                "SavedFormalSceneContainsMap: actual saved formal scene needs an attached configured controller");
            TestAssert.True(level.GetChildren().OfType<PrologueMinimapController>().Count()==1 &&
                level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints.Count==3,"one saved controller, actual HUD and fixed checkpoint descriptors");
            tree.Root.AddChild(level);await Frames(tree,3);
            TestAssert.True(controller.Model!=null && ReferenceEquals(controller.Model.Catalog,controller.Catalog),"actual model consumes the same saved Resource before deferred Flow load");
            flow.StartNew();await Frames(tree,5);
            var fog=level.GetNode<PrologueFogController>("FogController");
            void Observe()=>typeof(PrologueFogController).GetMethod("OnFramePreDraw",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(fog,null);
            Observe();
            TestAssert.True(flow.State==PrologueGameFlowState.Playing && controller.Model.RevisionCounter>1,"actual saved formal scene valid living observation reveals");
            var view=level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap");
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu");var model=controller.Model;
            pause.Open();TestAssert.True(tree.Paused && !view.Visible,"actual saved first-frame pause hides presentation synchronously");
            pause.Execute(ProloguePauseCommand.Resume);await Frames(tree,2);
            using(var sampler=new HeadlampOcclusionSampler(level.GetNode<Node2D>("WorldRoot")))
            {
                var frame=sampler.Capture();
                TestAssert.True(frame.IsComplete,"actual mounted native formal frame complete");
                int members=0;
                for(int i=0;i<controller.Catalog.SolidCount;i++)
                {
                    var s=controller.Catalog.Solid(i);
                    if(frame.Shapes.Any(row=>row.WorldToLocal==s.LocalToWorld.AffineInverse() && row.LocalBounds==s.LocalBounds))members++;
                }
                TestAssert.True(members>=11,"actual formal saved/mounted native shapes exactly match catalog");
                GD.Print($"MINIMAP_FORMAL_NATIVE members={members} blockers={frame.Shapes.Count} actor={level.Player.GlobalPosition} generation={controller.WorldGeneration}");
                var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                var loaded=world.Definitions.Where(d=>world.TryGetMountedSector(d.Id,out _)).Select(d=>d.WorldBounds).ToArray();
                if (oracle) MinimapPerformanceProbe.Oracle(controller.Catalog,frame,level.Player.GlobalPosition,loaded,paths.CaseRoot,oraclePath);
                else if (profileOnly) MinimapPerformanceProbe.Profile(controller.Catalog,frame,level.Player.GlobalPosition,loaded,paths.CaseRoot);
                else if (measurePerformance) MinimapPerformanceProbe.Run(controller.Catalog,frame,level.Player.GlobalPosition,loaded,paths.CaseRoot);
            }
            var health=level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(99,Vector2.Right,0,882)),"actual formal lethal hit");
            TestAssert.True(health.CurrentHealth==0 && flow.State==PrologueGameFlowState.Recovering,"actual formal health death enters recovery");
            for(int i=0;i<150 && flow.State==PrologueGameFlowState.Recovering;i++)await Frames(tree,1);
            TestAssert.True(health.CurrentHealth>0 && flow.State==PrologueGameFlowState.Playing && ReferenceEquals(model,controller.Model),"actual formal respawn keeps same explored model");
            await Inputs(tree,level);
            string originBits=model.CaptureSave().FreeCellsBase64;
            ulong playerId=level.Player.GetInstanceId(),generation=controller.WorldGeneration;
            var sectors=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(sectors.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(1680,3960)),"controlled authored Basin checkpoint relocation admitted");
            await Frames(tree,8);Observe();
            TestAssert.True(!sectors.TryGetMountedSector(PrologueSectorId.CanyonEntrance,out _) && controller.WorldGeneration>generation && level.Player.GetInstanceId()==playerId,"actual mount/unload uses persistent player and changes map generation");
            Contains(originBits,model.CaptureSave().FreeCellsBase64,"unload preserves old actual catalog free bits");
            TestAssert.True(sectors.RestoreCheckpoint(PrologueSectorId.CanyonEntrance,new Vector2(160,600)),"return to authored Entrance checkpoint admitted");
            await Frames(tree,8);Observe();
            TestAssert.True(sectors.TryGetMountedSector(PrologueSectorId.CanyonEntrance,out _) && ReferenceEquals(model,controller.Model),"old route remount keeps same actual map model");
            Contains(originBits,model.CaptureSave().FreeCellsBase64,"old explored route remains on return");
            TestAssert.True(flow.TrySaveBeforeQuit(out _),"formal actual catalog durable save A");
            string durable=model.CaptureSave().FreeCellsBase64;
            TestAssert.True(sectors.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(1680,3960)),"unsaved formal relocation B");
            await Frames(tree,8);Observe();
            TestAssert.True(model.CaptureSave().FreeCellsBase64!=durable,"formal actual catalog unsaved B differs");
            flow.ReturnToTitle();flow.Continue();
            TestAssert.Equal(durable,model.CaptureSave().FreeCellsBase64,"formal Continue restores durable A before any new observation, not unsaved B");
            await Frames(tree,4);
            var bossSave=PrologueSaveData.NewLayoutV2();bossSave.CheckpointId="canyon.v2.temple-forecourt";bossSave.HasActivatedCheckpoint=true;
            bossSave.CheckpointSector="LostTemple";bossSave.CheckpointWorldX=5920;bossSave.CheckpointWorldY=1080;
            foreach(string id in new[]{"entrance","rift","abyss-floor","gear-gallery","ascent-mid","courtyard"})bossSave.CollectedFragmentIds.Add("canyon.v2.fragment."+id);
            bossSave.ActivatedMechanisms.Add("canyon.v2.courtyard-seal-open");bossSave.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
            level.RestoreFromSave(bossSave.Normalize());await Frames(tree,8);
            var arena=level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            level.Player.GlobalPosition=new Vector2(6340,1080);level.Player.Velocity=Vector2.Zero;await Frames(tree,8);
            arena.BeginEncounter();await Frames(tree,2);Observe();
            TestAssert.True(arena.IsEncounterActive && view.Visible && ReferenceEquals(model,controller.Model),"actual Boss encounter retains living actor map presentation");
            ulong beforeBoss=model.RevisionCounter;
            level.Player.GlobalPosition=new Vector2(7200,1080);level.Player.Velocity=Vector2.Zero;await Frames(tree,8);Observe();
            TestAssert.True(model.RevisionCounter>beforeBoss,"actual Boss stage authorizes fresh reveal");
            var completion=level.GetNode<PrologueCompletionController>("Completion");completion.RestoreSession(PrologueObjective.DefeatBellkeeper);
            TestAssert.True(arena.CurrentBoss.GetNode<HealthComponent>("Health").ApplyDamage(new DamageRequest(999,Vector2.Right,0,883)),"controlled actual Boss health death accepted");
            await Frames(tree,8);
            TestAssert.True(level.Progress.BossDefeated,"actual Boss death event updates progression");
            var exit=level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision");
            var floor=level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Collision/ArenaFloor/Collision");
            var capsule=(CapsuleShape2D)level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            level.Player.GlobalPosition=new Vector2(exit.GlobalPosition.X+((RectangleShape2D)exit.Shape).Size.X*.5f+capsule.Radius+8,
                floor.GlobalPosition.Y-((RectangleShape2D)floor.Shape).Size.Y*.5f-capsule.Height*.5f-1);level.Player.Velocity=Vector2.Zero;
            for(int i=0;i<40 && flow.State!=PrologueGameFlowState.Completed;i++)await Frames(tree,1);
            TestAssert.True(flow.State==PrologueGameFlowState.Completed && completion.CompletionCount==1,"actual capsule settles beyond open Boss exit on original support and completes");
            await Frames(tree,1);TestAssert.True(!view.Visible,"actual settlement hides map without clearing model");
            Contains(durable,model.CaptureSave().FreeCellsBase64,"Boss and settlement preserve durable explored bits");
            GD.Print("MINIMAP_FORMAL_EXTENDED_PROOF physical_parse_keys=Q,J,K,L,Esc controlled_checkpoint_mount_unload_return=True same_model=True Continue_durable_only=True same_process=True controlled_prerequisites=True actual_Boss_health_death=True supported_exit=True settlement_hidden=True normal_route=False hardware_input=False");
            GD.Print("MINIMAP_FORMAL_CONTROLLED_PROOF living_reveal=True pause_first_frame=True real_health_death_respawn=True normal_route=False hardware_input=False");
        }
        finally {tree.Paused=false;await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
        GD.Print("MINIMAP_FORMAL_OK");
    }
    private static async Task Frames(SceneTree tree,int count){for(int i=0;i<count;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
    private static void Contains(string oldBits,string nextBits,string message)
    {var a=Convert.FromBase64String(oldBits);var b=Convert.FromBase64String(nextBits);TestAssert.True(a.Length==b.Length && a.Select((v,i)=>(b[i]&v)==v).All(v=>v),message);}
    private static async Task Inputs(SceneTree tree,PrologueLevelController level)
    {
        var input=level.Player.GetNode<PlayerInputReader>("Input");
        var observer=new MinimapFormalInputObserver{Reader=input,ProcessPhysicsPriority=-299};level.AddChild(observer);
        try
        {
            // Recovery intentionally suppresses its first input sample. Begin a fresh
            // press only after the original sampler has crossed that gameplay boundary.
            await KeyboardActionRuntimeTests.Frames(tree,input,8);
            TestAssert.Equal(-300,input.ProcessPhysicsPriority,"observer follows original Input sampler before consumers");
            foreach(Key key in new[]{Key.Q,Key.J,Key.K,Key.L})
            {
                observer.Seen=false;observer.Key=key;KeyboardActionRuntimeTests.KeyEvent(key,true);
                await KeyboardActionRuntimeTests.Frames(tree,input,2);
                KeyboardActionRuntimeTests.KeyEvent(key,false);
                TestAssert.True(observer.Seen,$"actual saved map leaves physical {key} input sample available to existing player consumer");
                await KeyboardActionRuntimeTests.Frames(tree,input,35);
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.Escape,true);await Frames(tree,1);KeyboardActionRuntimeTests.KeyEvent(Key.Escape,false);
            await Frames(tree,1); // Same complete menu pulse used by the existing formal boundary gate.
            TestAssert.True(tree.Paused && !level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").Visible,"actual physical Esc not intercepted by map");
            level.GetNode<ProloguePauseMenu>("PauseMenu").Execute(ProloguePauseCommand.Resume);await Frames(tree,2);
        }
        finally {foreach(Key key in new[]{Key.Q,Key.J,Key.K,Key.L,Key.Escape})KeyboardActionRuntimeTests.KeyEvent(key,false);observer.QueueFree();}
    }
}

public partial class MinimapFormalInputObserver : Node
{
    internal PlayerInputReader Reader;internal Key Key;internal bool Seen;
    public override void _PhysicsProcess(double delta)
    {var f=Reader.Current;Seen|=Key switch{Key.Q=>f.InteractPressed,Key.J=>f.AttackPressed,Key.K=>f.ClawPressed,Key.L=>f.ShieldHeld,_=>false};}
}
