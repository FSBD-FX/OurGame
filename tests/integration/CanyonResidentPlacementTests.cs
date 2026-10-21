using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Build.Prologue;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Interactions;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Presentation.Minimap;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Tools;

namespace Knight.Tests.Integration;

// Default phase is query-only; candidate phases explicitly prove limited native actor motion.
public static class CanyonResidentPlacementTests
{
    public static async Task RunCandidateParent(SceneTree tree,string storage)
    {
        string source=Path.Combine(storage,"candidate-parent-source"),moved=Path.Combine(storage,"candidate-parent-relocated");
        Directory.CreateDirectory(source);Directory.CreateDirectory(moved);
        var paths=new Dictionary<PrologueSectorId,string>();
        foreach(PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
        {
            string path=Path.Combine(source,id+".tscn");
            var sector=PrologueCanyonBuildParts.Build(id);
            try
            {
                TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(sector,path,out int before,out int after,out string failure),"candidate parent actual sector save: "+failure);
                TestAssert.Equal(before,after,"candidate parent leaf node preservation");paths.Add(id,path);
            }
            finally{sector.Free();}
        }
        var entry=typeof(BuildPrologueLevel).GetMethod("Build",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
        PrologueLevelController parent=null,loaded=null;SubViewport viewport=null;World2D nativeWorld=null;
        try
        {
            if(entry!=null)parent=(PrologueLevelController)entry.Invoke(null,new object[]{paths});
            else
            {
                // Baseline adapter exercises the real old parent graph and real canonical-only producer.
                parent=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
                var old=parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");old.Definitions.Clear();
                typeof(BuildPrologueLevel).GetMethod("PopulateCanyonDefinitions",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.Invoke(null,new object[]{old});
                parent.GetNode<PrologueMinimapController>("MinimapController").Catalog=MinimapTerrainCatalogBuilder.Build(old.Definitions.ToArray());
            }
            Check(parent,source);
            TestAssert.True(entry!=null,"parent authoring entry must be available after actual reference behavior is satisfied");
            var defaults=(PrologueLevelController)entry.Invoke(null,new object[]{null});
            try
            {
                var legacy=defaults.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
                TestAssert.True(legacy.Count==5&&legacy.All(d=>d.Scene.ResourcePath==PrologueCanyonBuildParts.ScenePath(d.Id))&&
                    defaults.GetNode<PrologueMinimapController>("MinimapController").Catalog.SolidCount==127,"default authoring preserves five canonical references and original127 catalog");
                GD.Print("RESIDENT_PARENT_DEFAULT_OK canonical_paths=5 original_solids=127 canonical_written=false");
            }
            finally{defaults.Free();}
            string parentPath=Path.Combine(source,"PrologueLevel.tscn");
            var snapshotSave=typeof(BuildPrologueLevel).GetMethod("SaveCandidate");
            Error error=snapshotSave==null?SceneBuildUtil.PackAndSave(parent,parentPath):
                (Error)snapshotSave.Invoke(null,new object[]{parent,parentPath});
            TestAssert.Equal(Error.Ok,error,"candidate parent actual pack with only five embedded scene snapshots");
            parent.Free();parent=null;
            foreach(string file in Directory.EnumerateFiles(source,"*.tscn"))
            {
                string destination=Path.Combine(moved,Path.GetFileName(file));File.Copy(file,destination,false);
                TestAssert.True(File.ReadAllBytes(file).AsSpan().SequenceEqual(File.ReadAllBytes(destination)),"relocation preserves exact saved bytes");
            }
            TestAssert.Equal(6,Directory.EnumerateFiles(moved,"*.tscn").Count(),"five sector leaves plus one real parent");
            loaded=ResourceLoader.Load<PackedScene>(Path.Combine(moved,"PrologueLevel.tscn"),cacheMode:ResourceLoader.CacheMode.Ignore).Instantiate<PrologueLevelController>();
            Check(loaded,moved,true);
            TestAssert.True(!File.ReadAllText(Path.Combine(moved,"PrologueLevel.tscn")).Contains(source.Replace('\\','/'),StringComparison.OrdinalIgnoreCase),"moved parent must not retain original absolute package dependencies");
            GD.Print("RESIDENT_PARENT_RELOCATED_OK files=6 bytes_unchanged=true exact_five_embedded_snapshots=true embedded_solids=132 frozen_rectangles=5 canonical_changed=false");
            IsolatedPrologueFixture.Configure(loaded,"relocated-candidate-parent");
            var flow=loaded.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
            nativeWorld=new World2D();
            viewport=new SubViewport{Name="ResidentCandidateParentWorld",Size=new(1280,720),World2D=nativeWorld};
            tree.Root.AddChild(viewport);
            var canaryRoot=new Node2D{Name="PlayerRoot"};
            var canary=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            canary.Name="Player";canary.AddToGroup("player");canaryRoot.AddChild(canary);
            canary.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
            canary.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            try
            {
                viewport.AddChild(canaryRoot);
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                TestAssert.True(ParentPlayerReady(canaryRoot,canary),"actual isolated canary starts as a live unique saved Player");
                var duplicate=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
                duplicate.Name="DuplicatePlayer";duplicate.AddToGroup("player");
                duplicate.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
                duplicate.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
                canaryRoot.AddChild(duplicate);
                TestAssert.True(duplicate.IsInsideTree()&&duplicate.GetNode<HealthComponent>("Health").CurrentHealth>0&&
                    !ParentPlayerReady(canaryRoot,canary),"parent proof rejects a second actual saved Player, not only an incorrect reference");
                duplicate.QueueFree();
                TestAssert.True(!ParentPlayerReady(canaryRoot,canary),"queued duplicate must not be filtered out of uniqueness");
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                TestAssert.True(ParentPlayerReady(canaryRoot,canary),"unique living canary is accepted again after duplicate actually leaves native tree");
                GD.Print("RESIDENT_PARENT_DUPLICATE_PLAYER_REJECTED actual_saved_players=2 queued_duplicate_rejected=true native_removed=true");
                var canaryHealth=canary.GetNode<HealthComponent>("Health");
                TestAssert.True(canaryHealth.ApplyDamage(new DamageRequest(canaryHealth.CurrentHealth,Vector2.Zero,0,1,canary.GetInstanceId()))&&canaryHealth.CurrentHealth==0,
                    "negative proof kills genuine saved Player through Health, without main-parent recovery");
                TestAssert.True(!ParentPlayerReady(canaryRoot,canary),"parent proof must reject an actual zero-HP Player despite a matching persistent reference");
                GD.Print("RESIDENT_PARENT_DEAD_PLAYER_REJECTED actual_saved_player=true actual_health_damage=true parent_recovery=false");
            }
            finally
            {
                canaryRoot.QueueFree();
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            }
            viewport.AddChild(loaded);
            for(int i=0;i<5;i++){await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            var controller=loaded.GetNode<PrologueMinimapController>("MinimapController");
            TestAssert.True(controller.Model!=null&&ReferenceEquals(controller.Model.Catalog,controller.Catalog),"runtime Model consumes actual moved parent embedded catalog");
            var world=loaded.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonDescent,out var descent),"real seamless Ready mounts candidate adjacent Descent");
            var floor=descent.GetNode<StaticBody2D>("Gameplay/Collision/FrozenLowerResidentShelf");
            var shape=floor.GetNode<CollisionShape2D>("Collision");
            TestAssert.True(shape.Shape is RectangleShape2D rectangle&&rectangle.Size==new Vector2(300,40)&&
                floor.GlobalPosition==new Vector2(1690,2900),"mounted native candidate lower shelf is actual80px-bypass geometry");
            using var ray=PhysicsRayQueryParameters2D.Create(new(1600,2870),new(1600,2890),CombatCollisionLayers.World);
            ray.CollideWithAreas=false;
            using var hit=descent.GetWorld2D().DirectSpaceState.IntersectRay(ray);
            TestAssert.True(hit.Count>0&&hit["collider"].AsGodotObject()==floor,"actual moved parent registers new shelf in native World2D");
            TestAssert.True(ParentPlayerReady(loaded.GetNode("PlayerRoot"),loaded.Player)&&flow.State==PrologueGameFlowState.Title,"real parent remains isolated at Title with one persistent player");
            GD.Print("RESIDENT_PARENT_NATIVE_OK actual_ready=true model_same_embedded_catalog=true mounted_new_shelf=true native_collision=true title=true living_unique_player=true normal_route=false publication=false");
        }
        finally
        {
            if(GodotObject.IsInstanceValid(parent))parent.Free();
            if(GodotObject.IsInstanceValid(loaded))
            {
                if(loaded.IsInsideTree())await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,loaded);
                else loaded.Free();
            }
            if(GodotObject.IsInstanceValid(viewport))viewport.Free();
            nativeWorld?.Dispose();
        }
        static bool ParentPlayerReady(Node playerRoot,CharacterBody2D player)
        {
            if(!GodotObject.IsInstanceValid(playerRoot)||!playerRoot.IsInsideTree()||playerRoot.IsQueuedForDeletion()||
                !GodotObject.IsInstanceValid(player)||!player.IsInsideTree()||player.IsQueuedForDeletion()||
                player!=playerRoot.GetNodeOrNull<CharacterBody2D>("Player"))return false;
            var health=player.GetNodeOrNull<HealthComponent>("Health");
            if(!GodotObject.IsInstanceValid(health)||!health.IsInsideTree()||health.IsQueuedForDeletion()||health.CurrentHealth<=0)return false;
            var players=playerRoot.GetTree().GetNodesInGroup("player");
            return players.Count==1&&players[0]==player;
        }
        static void Check(PrologueLevelController root,string package,bool embedded=false)
        {
            var defs=root.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
            TestAssert.Equal(5,defs.Count,"parent owns five authored definitions");
            foreach(var d in defs)
            {
                string expected=Path.GetFullPath(Path.Combine(package,d.Id+".tscn")).Replace('\\','/');
                string actual=ProjectSettings.GlobalizePath(d.Scene.ResourcePath).Replace('\\','/');
                GD.Print($"RESIDENT_PARENT_REFERENCE id={d.Id} actual={actual} expected={expected}");
                bool correct=embedded?actual.StartsWith(Path.GetFullPath(Path.Combine(package,"PrologueLevel.tscn")).Replace('\\','/')+"::",StringComparison.OrdinalIgnoreCase):
                    string.Equals(expected,actual,StringComparison.OrdinalIgnoreCase);
                TestAssert.True(correct,"candidate parent must consume supplied saved sector or its actual moved built-in snapshot, never old fallback: "+d.Id);
                var instance=d.Scene.Instantiate<PrologueSector>();
                try{TestAssert.True(instance.SectorId==d.Id&&instance.ValidateContract(d).Count==0,"actual embedded sector identity/contract survives reload");}
                finally{instance.Free();}
            }
            TestAssert.Equal(5,defs.Select(d=>d.Scene.GetInstanceId()).Distinct().Count(),"five distinct real scene resources, not one alias");
            var catalog=root.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            using var rebuilt=MinimapTerrainCatalogBuilder.Build(defs.ToArray());
            TestAssert.True(rebuilt.SerializedValues.AsSpan().SequenceEqual(catalog.SerializedValues),"reloaded embedded catalog exactly matches all five actual scene snapshots");
            TestAssert.Equal(132,catalog.SolidCount,"same-five embedded minimap includes five new Frozen solids");
            foreach(Rect2 wanted in new[]{new Rect2(1280,1800,36,280),new(1280,2080,8,140),new(1280,2220,36,1140),new(1316,2400,368,40),new(1540,2880,300,40)})
                TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>
                    catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds.Position==wanted.Position&&catalog.Solid(i).LocalBounds.Size==wanted.Size),"embedded minimap literal candidate solid "+wanted);
            var cp=root.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints;
            TestAssert.True(cp.Count==3&&cp["canyon.v2.start"]==new Vector2(160,600)&&cp["canyon.v2.basin-camp"]==new Vector2(1680,3960)&&cp["canyon.v2.temple-forecourt"]==new Vector2(5920,1080),"candidate preserves three actual checkpoint positions");
        }
    }

    public static async Task RunFrozenPlayerRoute(SceneTree tree,string storage)
    {
        string output=Path.Combine(storage,"player-candidate-CanyonDescent.tscn");
        var authored=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent);
        try
        {
            TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(authored,output,out int before,out int after,out string failure),"save exact Player candidate: "+failure);
            TestAssert.Equal(before,after,"Player candidate keeps authored nodes");
        }
        finally{authored.Free();}
        var paths=CanyonWorldLayout.SectorBounds.Keys.ToDictionary(id=>id,PrologueCanyonBuildParts.ScenePath);
        paths[PrologueSectorId.CanyonDescent]=output;
        var fixture=new NativeFixture(tree);
        CharacterBody2D player=null;
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();fixture.AssertNativePreconditions(132);
            player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position=new(1800,3244);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");
            var motor=player.GetNode<PlayerMotor>("Motor");
            var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
            var health=player.GetNode<HealthComponent>("Health");
            var exit=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<StaticBody2D>("Gameplay/Collision/FrozenExit");
            var shelf=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<StaticBody2D>("Gameplay/Collision/FrozenLowerResidentShelf");
            var upper=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<GrappleSurface2D>("Gameplay/Collision/FrozenRightCatch/GrappleSurfaceProfile");
            var wall=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<GrappleSurface2D>("Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile");
            ulong id=player.GetInstanceId();int healed=0;bool reeled=false;
            var casts=new HashSet<ulong>();var boostCasts=new HashSet<ulong>();
            var arrivals=new HashSet<(ulong Cast,ulong Target)>();ulong wallCast=0,upperCast=0;
            health.Healed+=(_,_)=>healed++;
            MechanicalClawState traceState=MechanicalClawState.Idle;MechanicalClawFailure traceFailure=MechanicalClawFailure.None;
            claw.ClawFrameChanged+=(frame,target)=>
            {
                if(frame.State==MechanicalClawState.Windup&&frame.CastId!=0)casts.Add(frame.CastId);reeled|=frame.State==MechanicalClawState.Reel;
                if(frame.ReleaseKind==MechanicalClawReleaseKind.Boost)boostCasts.Add(frame.CastId);
                if(frame.Arrived&&frame.State==MechanicalClawState.Swing&&frame.Failure==MechanicalClawFailure.None&&frame.CastId!=0&&
                    target!=null&&(target==wall||target==upper)&&frame.LockedTargetRuntimeId==target.GetInstanceId()&&
                    arrivals.Add((frame.CastId,frame.LockedTargetRuntimeId)))
                {
                    if(target==wall)wallCast=frame.CastId;else upperCast=frame.CastId;
                    GD.Print($"RESIDENT_FROZEN_PLAYER_ARRIVAL cast={frame.CastId} target={target.Name} actual_arrived=true player={player.GlobalPosition} input_seq={input.CurrentFrameSequence}");
                }
                if(frame.State!=traceState||frame.Failure!=traceFailure)
                    GD.Print($"RESIDENT_FROZEN_PLAYER_CLAW state={frame.State} failure={frame.Failure} player={player.GlobalPosition} velocity={player.Velocity} anchor={frame.AnchorPoint} dock={frame.DockPoint} target={frame.LockedTargetRuntimeId} input_seq={input.CurrentFrameSequence} jumps={motor.AcceptedJumpCount}");
                traceState=frame.State;traceFailure=frame.Failure;
            };
            int samples=0;ulong previous=input.CurrentFrameSequence;
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);samples++;
                TestAssert.True(input.CurrentFrameSequence>previous,"Frozen route observes real sampled keyboard and native motor");previous=input.CurrentFrameSequence;
                TestAssert.True(player.GetInstanceId()==id&&health.CurrentHealth>0&&healed==0&&player.GlobalPosition.X is >1250 and <1960&&player.GlobalPosition.Y is >2600 and <3380,
                    "Frozen route cannot recover/teleport/heal or escape local saved terrain: "+player.GlobalPosition);
            }
            bool On(StaticBody2D body)
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++)
                {using var hit=player.GetSlideCollision(i);if(hit.GetCollider()==body&&hit.GetNormal().Dot(Vector2.Up)>.99f)return true;}
                return false;
            }
            bool AtObservedArrival(ulong cast,GrappleSurface2D target,float maximumY)=>cast!=0&&
                arrivals.Contains((cast,target.GetInstanceId()))&&claw.CurrentFrame.CastId==cast&&
                claw.CurrentFrame.LockedTargetRuntimeId==target.GetInstanceId()&&claw.CurrentTarget==target&&
                claw.CurrentFrame.State==MechanicalClawState.Swing&&claw.CurrentFrame.Failure==MechanicalClawFailure.None&&player.GlobalPosition.Y<maximumY;
            async Task Until(Func<bool> reached,int bound,string stage)
            {
                int frame=0;for(;frame<bound&&!reached();frame++)await Step();
                GD.Print($"RESIDENT_FROZEN_PLAYER_STAGE stage={stage} frames={frame}/{bound} player={player.GlobalPosition} velocity={player.Velocity} claw={claw.CurrentFrame.State} preview={claw.KeyboardPreview.Surface?.GetPath()} hp={health.CurrentHealth} floor={player.IsOnFloor()} samples={samples}");
                TestAssert.True(reached(),"actual Frozen Player stage failed: "+stage);
            }
            await Until(()=>On(exit),12,"initial_saved_exit");int hp=health.CurrentHealth;
            ResidentPathGeometryResult dockCorridor=ResidentPlacementGeometry.CheckCorridor(player,new(0,new Vector2(1872,2760)),new(0,new Vector2(1872,3000)),
                fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
            GD.Print("RESIDENT_FROZEN_PLAYER_DOCK_CORRIDOR x=1872 full_capsule=true clear="+dockCorridor.CorridorClear+" issues="+string.Join(",",dockCorridor.Issues));
            TestAssert.True(dockCorridor.CorridorClear,"actual wall docking x1872 must fit the entire unchanged40px Player along new shelf, not only center1890 free movement");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            await Step();await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.W,true);for(int i=0;i<2;i++)await Step();
            TestAssert.Equal(wall,claw.KeyboardPreview.Surface,"NE first uses the nearby actual saved canyon wall rather than a distant ceiling longshot");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);for(int i=0;i<4;i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false);KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
            await Until(()=>AtObservedArrival(wallCast,wall,3160),120,"short_native_wall_pull");
            TestAssert.Equal(wall.GetInstanceId(),claw.CurrentFrame.LockedTargetRuntimeId,"first real attachment belongs to the shown existing wall");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
            await Until(()=>player.GlobalPosition.X>=1872,50,"boost_into_full_capsule_sidegap");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);KeyboardActionRuntimeTests.KeyEvent(Key.W,true);
            for(int i=0;i<3;i++)await Step();
            TestAssert.Equal(upper,claw.KeyboardPreview.Surface,"W must show actual saved upper support, not a synthetic anchor or a mouse override");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);for(int i=0;i<4;i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false);KeyboardActionRuntimeTests.KeyEvent(Key.W,false);
            await Until(()=>AtObservedArrival(upperCast,upper,2800),180,"native_claw_up_through80px");
            TestAssert.True(reeled&&claw.CurrentFrame.LockedTargetRuntimeId==upper.GetInstanceId()&&player.GlobalPosition.X is >=1860 and <=1901,
                "same saved Player auto-pulls through the real full-capsule sidegap to its shown support");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
            for(int i=0;i<4;i++)await Step();KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            await Until(()=>On(shelf)&&player.GlobalPosition.X<=1816,120,"land_on_new_supported_shelf");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(shelf)&&Mathf.Abs(player.GlobalPosition.Y-2844)<1,"new shelf catches the unchanged full Player through real collision");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            await Until(()=>player.GlobalPosition.X>=1888&&player.GlobalPosition.Y>2957,120,"walk_off_and_down_sidegap");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            await Until(()=>player.GlobalPosition.X<=1816,60,"aircorrect_over_original_exit");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
            await Until(()=>On(exit),120,"return_to_original_saved_exit");
            for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(exit)&&player.GetInstanceId()==id&&motor.AcceptedJumpCount==1&&motor.AcceptedDashCount==0&&
                casts.Count==2&&wallCast!=upperCast&&arrivals.Count==2&&boostCasts.Count==1&&health.CurrentHealth<=hp&&healed==0,"native keyboard roundtrip keeps identity, two unique real arrived casts, one real boost, original jump/dash/fall policy and no healing");
            GD.Print("RESIDENT_FROZEN_PLAYER_OBSERVER_OK unique_casts=2 unique_arrivals=2 exact_targets=true current_attached_pose=true event_handler_record_only=true");
            GD.Print($"RESIDENT_FROZEN_PLAYER_ROUTE_OK same_player=true saved_terrain=132 actual_keyboard=true actual_physics=true first_target=AbyssBrakeLeft4 up_gap=80 upper_target=FrozenRightCatch landed_new_shelf=true returned_original_exit=true accepted_jumps=1 accepted_boosts=1 accepted_dashes=0 healed=0 health={hp}->{health.CurrentHealth} samples={samples} mechanisms_active=false normal_world=false publication=false");
        }
        finally
        {
            foreach(Key key in new[]{Key.A,Key.D,Key.W,Key.K,Key.Space})KeyboardActionRuntimeTests.KeyEvent(key,false);
            if(GodotObject.IsInstanceValid(player))player.Free();
            await fixture.DisposeAsync();
        }
    }

    public static async Task RunFrozenCandidate(SceneTree tree,string storage)
    {
        string leaves=ResidentSceneBindingTests.BuildCandidatePackage(storage);
        string output=Path.Combine(storage,"candidate-CanyonDescent.tscn");
        var candidate=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent);
        try
        {
            TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(candidate,output,out int before,out int after,out string failure),"save candidate Descent through real builder: "+failure);
            TestAssert.Equal(before,after,"candidate saved without dropping nodes");
        }
        finally{candidate.Free();}
        var paths=CanyonWorldLayout.SectorBounds.Keys.ToDictionary(id=>id,PrologueCanyonBuildParts.ScenePath);
        paths[PrologueSectorId.CanyonDescent]=output;
        var fixture=new NativeFixture(tree);
        using var source=new CensusLeafSource(leaves);
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();
            foreach(ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)source.Raw(type);
            ResidentPopulationDefinition identity=CanyonResidentCatalog.All.First(x=>x.SubtypeId=="scrap-wanderer");
            CharacterBody2D old=(CharacterBody2D)GD.Load<PackedScene>(CanyonEncounterCatalog.RustShellScene).Instantiate();
            try
            {
                ResidentBodyGeometryResult oldClear=ResidentPlacementGeometry.CheckBody(old,new(0,new Vector2(1040,604)),fixture.Space,fixture.Scope,fixture.Bounds,true,Array.Empty<ResidentKeepOut>());
                TestAssert.True(oldClear.BodyGeometryClear&&oldClear.WorldBodyBounds==new Rect2(1022,572,36,64),"old probe counterexample starts at actual native-clear supported body");
                bool rejected=false;
                try{source.QueryBody(identity,old,new(0,new Vector2(1040,604)),fixture,fixture.Bounds,true,Array.Empty<ResidentKeepOut>());}
                catch(ArgumentException){rejected=true;}
                TestAssert.True(rejected&&source.BodyIdentityCount==0&&source.BodySubtypeCount==0,"actual body query must reject oldProbe without incrementing counts even after24raw-cache loads");
            }
            finally{old.Free();}
            GD.Print("RESIDENT_BODY_QUERY_IDENTITY_OK raw_cache=24 old_probe_native_clear=true alias_rejected_before_count=true same_actor_queried=true");
            ResidentPopulationDefinition[] ghosts=CanyonResidentCatalog.All.Where(x=>x.RouteNode==CanyonRouteNodeId.FrozenDetour&&x.SubtypeId=="lost-echo").ToArray();
            TestAssert.Equal(2,ghosts.Length,"both planned Frozen identities remain required");
            Vector2[] homes={new(1500,2368),new(1712,2848)};
            var geometryFailures=new List<string>();
            for(int i=0;i<ghosts.Length;i++)
            {
                ResidentSubtypeDefinition type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==ghosts[i].SubtypeId);
                ResidentBodyGeometryResult check=source.QueryBody(ghosts[i],source.Raw(type),new(0,homes[i]),fixture,
                    CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.FrozenDetour).WorldBounds,true,Array.Empty<ResidentKeepOut>());
                if(!check.BodyGeometryClear)geometryFailures.Add("full_feet:"+homes[i]+":"+string.Join(",",check.Issues));
            }
            (string Name,Rect2 Bounds)[] wanted=
            {
                ("FrozenOuterUpperWall",new(1280,1800,36,280)),
                ("FrozenSentryRecessWall",new(1280,2080,8,140)),
                ("FrozenOuterLowerWall",new(1280,2220,36,1140)),
                ("FrozenUpperResidentShelf",new(1316,2400,368,40)),
                ("FrozenLowerResidentShelf",new(1540,2880,300,40))
            };
            foreach(float y in new[]{2000f,2151f,2800f})
            {
                using var ray=PhysicsRayQueryParameters2D.Create(new(1240,y),new(1350,y),CombatCollisionLayers.World);
                ray.CollideWithAreas=false;
                using Godot.Collections.Dictionary hit=fixture.Space.IntersectRay(ray);
                if(hit.Count==0||hit["collider"].AsGodotObject() is not StaticBody2D body||
                    body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile")?.Material!=GrappleSurfaceMaterial.GripStone)
                    geometryFailures.Add("native_grip_wall:"+y);
            }
            foreach(var item in wanted)
            {
                StaticBody2D body=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNodeOrNull<StaticBody2D>("Gameplay/Collision/"+item.Name);
                CollisionShape2D collider=body?.GetNodeOrNull<CollisionShape2D>("Collision");
                if(collider==null||collider.GlobalTransform*collider.Shape.GetRect()!=item.Bounds)
                    geometryFailures.Add("saved_solid:"+item.Name);
                Knight.Presentation.CanyonPlatformVisual skin=fixture.Sectors[PrologueSectorId.CanyonDescent]
                    .GetNodeOrNull<Knight.Presentation.CanyonPlatformVisual>("VisualRoot/GameplayReadable/"+item.Name);
                if(skin==null||skin.SkinSize!=item.Bounds.Size||body==null||skin.GlobalPosition!=body.GlobalPosition)
                    geometryFailures.Add("matching_skin:"+item.Name);
            }
            var definitions=paths.Select(pair=>new PrologueSectorDefinition(pair.Key,GD.Load<PackedScene>(pair.Value),
                PrologueCanyonBuildParts.WorldOffsets[pair.Key],PrologueCanyonBuildParts.Bounds(pair.Key),null,null)).ToArray();
            using(var catalog=MinimapTerrainCatalogBuilder.Build(definitions))
            {
                foreach(var item in wanted)
                    if(!Enumerable.Range(0,catalog.SolidCount).Select(i=>catalog.Solid(i)).Any(s=>s.LocalToWorld*s.LocalBounds==item.Bounds))
                        geometryFailures.Add("candidate_minimap_missing:"+item.Name);
                string catalogPath=Path.Combine(storage,"candidate-minimap.tres");
                TestAssert.Equal(Error.Ok,ResourceSaver.Save(catalog,catalogPath),"save separate candidate catalog without changing canonical parent");
                MinimapTerrainCatalog loaded=GD.Load<MinimapTerrainCatalog>(catalogPath);
                TestAssert.True(catalog.SerializedValues.AsSpan().SequenceEqual(loaded.SerializedValues),"candidate map catalog survives actual save/load");
            }
            GD.Print("RESIDENT_FROZEN_GEOMETRY_RESULTS failures="+geometryFailures.Count+" details="+string.Join(";",geometryFailures));
            TestAssert.True(geometryFailures.Count==0,"saved candidate alcove native/minimap geometry: "+string.Join(";",geometryFailures));
            GD.Print("RESIDENT_FROZEN_CANDIDATE_SUPPORT_OK identities=2 actual_saved_sector=true native_full_feet=true publication=false");
            fixture.AssertNativePreconditions(132);
            string snapshot=fixture.CollisionSnapshot();
            ResidentEnvironmentSnapshot environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.Equal(0,environment.Issues.Count,"candidate keeps complete actual environment reservations");
            var oldBodies=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);
            try
            {
                CharacterBody2D Old(string path)
                {if(!oldBodies.TryGetValue(path,out CharacterBody2D actor)){actor=(CharacterBody2D)GD.Load<PackedScene>(path).Instantiate();oldBodies.Add(path,actor);}return actor;}
                IReadOnlyList<ResidentKeepOut> keep=environment.Regions.Concat(OriginalSpawnReservations(Old)).ToArray();
                int oldFrozen=0;
                foreach(CanyonEncounterDefinition encounter in CanyonEncounterCatalog.Normal.Where(x=>x.RouteNode==CanyonRouteNodeId.FrozenDetour))
                foreach(CanyonEnemyPlacement placement in encounter.Enemies)
                foreach(Vector2 point in placement.AlternateWorldPosition is Vector2 alternate?new[]{placement.WorldPosition,alternate}:new[]{placement.WorldPosition})
                {
                    ResidentBodyGeometryResult clear=ResidentPlacementGeometry.CheckBody(Old(placement.ScenePath),new(0,point),fixture.Space,
                        fixture.Scope,fixture.Bounds,placement.ScenePath==CanyonEncounterCatalog.SentryScene,Array.Empty<ResidentKeepOut>());
                    TestAssert.True(clear.BodyGeometryClear,"candidate cannot cut through old Frozen spawn/alternate: "+placement.ScenePath+" at "+point+":"+string.Join(",",clear.Issues));oldFrozen++;
                }
                TestAssert.Equal(5,oldFrozen,"preserve exact five existing Frozen primary/alternate records, not five actors");
                var residents=new List<RustShellSquire>();
                try
                {
                    for(int i=0;i<ghosts.Length;i++)
                    {
                        Vector2 home=homes[i];Vector2[] points={home,home+new Vector2(-64,0),home+new Vector2(64,0),home};
                        Rect2 territory=CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.FrozenDetour).WorldBounds;
                        var actor=(RustShellSquire)source.Configure(ghosts[i],points,territory);residents.Add(actor);
                        ResidentPathGeometryResult forward=ResidentPlacementGeometry.CheckCorridor(actor,new(0,points[1]),new(0,points[2]),fixture.Space,fixture.Scope,territory,true,keep);
                        ResidentPathGeometryResult reverse=ResidentPlacementGeometry.CheckCorridor(actor,new(0,points[2]),new(0,points[1]),fixture.Space,fixture.Scope,territory,true,keep);
                        TestAssert.True(forward.CorridorClear&&reverse.CorridorClear,"actual configured Ghost full patrol/body avoids all saved reservations: "+ghosts[i].StableId+":"+string.Join(",",forward.Issues.Concat(reverse.Issues)));
                        actor.Position=home-CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonDescent].Position;
                        fixture.MountResident(actor,PrologueSectorId.CanyonDescent);
                    }
                    await fixture.Sync();
                    float[] low=residents.Select(x=>x.GlobalPosition.X).ToArray(),high=(float[])low.Clone(),previous=(float[])low.Clone(),step=new float[2];
                    bool[] supported={true,true},returned={false,false};
                    for(int frame=0;frame<360;frame++)
                    {
                        await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                        for(int i=0;i<residents.Count;i++)
                        {
                            float x=residents[i].GlobalPosition.X;low[i]=MathF.Min(low[i],x);high[i]=MathF.Max(high[i],x);
                            step[i]=MathF.Max(step[i],MathF.Abs(x-previous[i]));previous[i]=x;supported[i]&=residents[i].IsOnFloor();
                            returned[i]|=low[i]<=homes[i].X-56&&high[i]>=homes[i].X+56&&MathF.Abs(x-homes[i].X)<=4;
                        }
                    }
                    for(int i=0;i<residents.Count;i++)
                    {
                        GD.Print($"RESIDENT_FROZEN_PATROL id={ghosts[i].StableId} min={low[i]} max={high[i]} max_step={step[i]} supported={supported[i]} returned_home={returned[i]}");
                        TestAssert.True(low[i]<=homes[i].X-56&&high[i]>=homes[i].X+56&&step[i]<2&&supported[i]&&returned[i]&&
                            residents[i].GetNode<HealthComponent>("Health").CurrentHealth==3,"actual saved Ghost must patrol both directions and home continuously without falling/healing "+ghosts[i].StableId);
                    }
                }
                finally{foreach(RustShellSquire actor in residents)if(GodotObject.IsInstanceValid(actor))actor.Free();await fixture.Sync();}
                GD.Print("RESIDENT_FROZEN_PATROL_OK identities=2 frames=360 actual_saved_actors=true actual_ready=true actual_physics=true old_spawn_records=5 formal_spawned=0");

                CharacterBody2D player=(CharacterBody2D)GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate();
                var shape=(RectangleShape2D)fixture.Sectors[PrologueSectorId.CanyonDescent]
                    .GetNode<CollisionShape2D>("Gameplay/Collision/FrozenLowerResidentShelf/Collision").Shape;
                Vector2 size=shape.Size;
                try
                {
                    ResidentPathGeometryResult Passage(Vector2 from,Vector2 to)=>ResidentPlacementGeometry.CheckCorridor(player,new(0,from),new(0,to),fixture.Space,fixture.Scope,fixture.Bounds,false,keep);
                    Vector2 top=new(1890,2760),bottom=new(1890,3000);
                    TestAssert.True(Passage(top,bottom).CorridorClear&&Passage(bottom,top).CorridorClear&&
                        Passage(new(1872,2760),new(1872,3000)).CorridorClear&&Passage(new(1872,3000),new(1872,2760)).CorridorClear,
                        "whole unchanged Player capsule fits free-movement and actual48px wall-dock lanes both directions in real80px bypass");
                    try
                    {
                        shape.Size=size+new Vector2(88,0);await fixture.Sync();
                        ResidentPathGeometryResult closed=Passage(top,bottom);
                        TestAssert.True(!closed.CorridorClear&&closed.Issues.Contains("corridor_native_world"),"visually plausible36px gap cannot pass40px Player capsule");
                    }
                    finally{shape.Size=size;await fixture.Sync();}
                    TestAssert.True(Passage(top,bottom).CorridorClear,"reopened candidate bypass returns to its real native clearance");
                }
                finally{player.Free();}
                GD.Print("RESIDENT_FROZEN_PLAYER_BYPASS_OK directions=4 closed36px_rejected=true restored80px=true actual_player_shape=true actual_player_motion=false claw_return_proved=false");
            }
            finally{foreach(CharacterBody2D actor in oldBodies.Values)actor.Free();}
            TestAssert.Equal(snapshot,fixture.CollisionSnapshot(),"candidate motion/control fixtures restore exact map collision state");
            RunBodyCandidateCensus(fixture,source);RunActivityCandidateCensus(fixture,source);
            source.WriteDiagnostics(Path.Combine(storage,"candidate-leaf-geometry-diagnostics.jsonl"));
            TestAssert.True(CanyonResidentCatalog.All.All(x=>x.Placement==null),"staged candidate does not silently publish formal positions");
            GD.Print("RESIDENT_FROZEN_CANDIDATE_DONE generated_terrain=132 actual_patrol_identities=2 census_identities=50 canonical_changed=false parent_updated=false publication=false");
        }
        finally{await fixture.DisposeAsync();}
    }

    public static async Task Run(SceneTree tree, string admittedStorage)
    {
        string leaves = ResidentSceneBindingTests.BuildCandidatePackage(admittedStorage);
        using var leafSource = new CensusLeafSource(leaves);
        var fixture = new NativeFixture(tree);
        var failed = new List<string>();
        int cases = 0;
        try
        {
            fixture.Initialize();
            fixture.MountSavedSectors();
            await fixture.Sync();
            fixture.StopCallbacks();
            fixture.AssertNativePreconditions();
            string snapshot = fixture.CollisionSnapshot();
            await fixture.Sync();
            TestAssert.Equal(snapshot, fixture.CollisionSnapshot(), "frozen geometry remains invariant after deferred Ready work");
            await RunEnvironment(fixture);
            await RunPlayerAccess(fixture);
            RunArchiveResidentSpace(fixture);
            RunBodyCandidateCensus(fixture, leafSource);
            RunActivityCandidateCensus(fixture, leafSource);
            fixture.PopulateControlledCounterexamples();
            await fixture.Sync();
            fixture.AssertControlledPreconditions();
            RunSavedLeafControls(fixture, leaves, admittedStorage);
            leafSource.WriteDiagnostics(Path.Combine(admittedStorage,"leaf-geometry-diagnostics.jsonl"));

            foreach ((string scene, Rect2 expected) in new[]
            {
                (CanyonEncounterCatalog.RustShellScene, new Rect2(1082,468,36,64)),
                (CanyonEncounterCatalog.LaborerScene, new Rect2(1072,458,56,84)),
                (CanyonEncounterCatalog.MothScene, new Rect2(1084,484,32,32)),
                (CanyonEncounterCatalog.SkitterScene, new Rect2(1086,477,28,22)),
                (CanyonEncounterCatalog.SentryScene, new Rect2(1075,461,50,68)),
                (CanyonEncounterCatalog.ScatterScene, new Rect2(1075,461,50,68)),
                (CanyonEncounterCatalog.BurstScene, new Rect2(1075,461,50,68)),
                (CanyonEncounterCatalog.MissileScene, new Rect2(1075,461,50,68)),
                (CanyonEncounterCatalog.LaserScene, new Rect2(1075,461,50,68))
            })
                Probe(scene, actor =>
                {
                    ResidentBodyGeometryResult r = Check(actor, new Vector2(1100,500), false);
                    Expect("actual_body_"+scene, r.BodyGeometryClear && r.WorldBodyBounds == expected && !r.IsCompletePlacementProof);
                });

            Probe(CanyonEncounterCatalog.RustShellScene, actor =>
            {
                ResidentBodyGeometryResult r = Check(actor, new Vector2(1040,604), true);
                Expect("saved_complete_floor", r.BodyGeometryClear && r.WorldBodyBounds == new Rect2(1022,572,36,64) && r.Support.Count > 0);
                Reject("center_gap_despite_edge_support", Check(actor,new Vector2(9000,604),true), "incomplete_support");
                Reject("wall_on_same_support_body",Check(actor,new Vector2(9700,604),true),"native_world");
                Reject("actual_world_overlap", Check(actor,new Vector2(10200,500),false), "native_world");
                Reject("actual_attack_area", Check(actor,new Vector2(9300,604),true), "native_hazard");
                Reject("query_capacity", Check(actor,new Vector2(10500,500),false), "query_truncated");
                CanyonShieldLaneHazard electric=fixture.Sectors[PrologueSectorId.CanyonBasin]
                    .GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
                CollisionShape2D electricShape=electric.GetNode<CollisionShape2D>("Collision");
                CollisionShape2D actorShape=actor.GetNode<CollisionShape2D>("BodyCollider");
                Transform2D electricSite=new(0,new Vector2(2230,3964));
                TestAssert.True(electric.CollisionLayer==0&&electric.Monitoring&&!electricShape.Disabled,
                    "saved live electric lane deliberately observes Player through mask with layer zero");
                TestAssert.True(actorShape.Shape.Collide(electricSite,electricShape.Shape,electricShape.GlobalTransform)&&
                    !fixture.HasNativeHit(actorShape.Shape,electricSite,electric.GetInstanceId()),
                    "real shape overlaps electric lane although even an all-layer native query cannot see it");
                Reject("actual_electric_lane", Check(actor,new Vector2(2230,3964),true), "native_hazard");
                Reject("body_not_center_rest", Check(actor,new Vector2(2090,3964),false,
                    new[] { new ResidentKeepOut("basin-camp", ResidentKeepOutKind.Protected, new Rect2(1280,3600,800,720)) }), "reserved.Protected:basin-camp");
                Transform2D singular = new(Vector2.Zero, Vector2.Down, new Vector2(1100,500));
                Reject("singular_pose", ResidentPlacementGeometry.CheckBody(actor,singular,fixture.Space,fixture.Scope,
                    fixture.Bounds,false,Array.Empty<ResidentKeepOut>()), "invalid_transform");
                actor.GetNode<CollisionShape2D>("BodyCollider").Disabled = true;
                Reject("disabled_body",Check(actor,new Vector2(1100,500),false),"disabled_body");
            });

            Probe(CanyonEncounterCatalog.MothScene, actor =>
            {
                CanyonSteamJet jet = fixture.Sectors[PrologueSectorId.CanyonBasin]
                    .GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
                Rect2 potentialJet = jet.Collision.GlobalTransform * jet.Collision.Shape.GetRect();
                TestAssert.Equal(new Rect2(3424,4035,72,150),potentialJet,"actual saved jet world envelope");
                TestAssert.True(jet.Collision.Disabled,"actual frozen jet is currently in safe phase");
                ResidentBodyGeometryResult before = Check(actor,new Vector2(3460,4080),false);
                Expect("current_safe_jet_space",before.BodyGeometryClear);
                Reject("disabled_jet_potential",Check(actor,new Vector2(3460,4080),false,
                    new[] { new ResidentKeepOut("saved-steam-full-phase",ResidentKeepOutKind.Hazard,potentialJet) }),
                    "reserved.Hazard:saved-steam-full-phase");
                var piston = fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<CanyonGearPiston>("Gameplay/Mechanisms/GearPiston");
                Rect2 endA = new Transform2D(0,piston.GetParent<Node2D>().ToGlobal(piston.RetractedPosition)) * piston.Collision.Shape.GetRect();
                Rect2 endB = new Transform2D(0,piston.GetParent<Node2D>().ToGlobal(piston.ExtendedPosition)) * piston.Collision.Shape.GetRect();
                Rect2 sweep = endA.Merge(endB);
                TestAssert.Equal(new Rect2(2534,3844,132,132),sweep,"actual saved full piston occupation");
                TestAssert.True(piston.Collision.Disabled,"piston current safe phase has no enabled body");
                Expect("current_safe_piston_space",Check(actor,new Vector2(2600,3890),false).BodyGeometryClear);
                Reject("piston_full_travel",Check(actor,new Vector2(2600,3890),false,
                    new[] { new ResidentKeepOut("saved-piston-travel",ResidentKeepOutKind.MovingOccupancy,sweep) }),
                    "reserved.MovingOccupancy:saved-piston-travel");
            });

            Probe(CanyonEncounterCatalog.RustShellScene, actor =>
            {
                CollisionShape2D collider=actor.GetNode<CollisionShape2D>("BodyCollider");
                collider.Owner=null;
                actor.RemoveChild(collider);
                var offset=new Node2D {Name="OffsetBodyParent",Position=new Vector2(100,0),Rotation=Mathf.Pi/2,Scale=new Vector2(2,2)};
                actor.AddChild(offset); offset.AddChild(collider);
                ResidentBodyGeometryResult r=Check(actor,new Vector2(900,500),false);
                Expect("full_ancestor_transform", r.WorldBodyBounds.Position.DistanceTo(new Vector2(936,464))<.01f &&
                    r.WorldBodyBounds.Size.DistanceTo(new Vector2(128,72))<.01f && r.Issues.Contains("native_world"));
            });
            Probe(CanyonEncounterCatalog.RustShellScene, actor =>
            {
                actor.GetNode<CollisionShape2D>("BodyCollider").TopLevel=true;
                Reject("top_level_body",Check(actor,new Vector2(1100,500),false),"unsupported_transform");
            });
            Probe(CanyonEncounterCatalog.RustShellScene, actor =>
            {
                CollisionShape2D body=actor.GetNode<CollisionShape2D>("BodyCollider");
                body.Owner=null;
                actor.RemoveChild(body);var separator=new Node {Name="NonCanvasSeparator"};actor.AddChild(separator);separator.AddChild(body);
                Reject("non_canvas_separator",Check(actor,new Vector2(1100,500),false),"unsupported_transform");
            });
            Probe(CanyonEncounterCatalog.RustShellScene, actor =>
            {
                actor.GetNode<CollisionShape2D>("BodyCollider").Shape=fixture.Own(new SegmentShape2D {A=new Vector2(-1,0),B=new Vector2(1,0)});
                Reject("unsupported_shape",Check(actor,new Vector2(1100,500),false),"unsupported_shape");
            });
            Probe(CanyonEncounterCatalog.RustShellScene, actor =>
            {
                actor.AddChild(new CollisionShape2D {Name="ExtraPhysicalBody",Shape=fixture.Own(new RectangleShape2D {Size=new Vector2(4,4)})});
                Reject("extra_body_shape",Check(actor,new Vector2(1100,500),false),"multiple_body_shapes");
            });
            TestAssert.True(failed.Count==0,"resident native geometry checks failed: "+string.Join("; ",failed));
            GD.Print($"RESIDENT_GEOMETRY_NATIVE_OK cases={cases} saved_sectors=5 probe_types=9 same_checker=true placement_certified=false");
            RunCorridors(fixture);
            RunCorridorSelectionControls(fixture);
        }
        finally { await fixture.DisposeAsync(); }

        ResidentBodyGeometryResult Check(CharacterBody2D actor,Vector2 p,bool ground,IReadOnlyList<ResidentKeepOut> keep=null) =>
            ResidentPlacementGeometry.CheckBody(actor,new Transform2D(0,p),fixture.Space,fixture.Scope,fixture.Bounds,ground,keep??Array.Empty<ResidentKeepOut>());
        void Expect(string name,bool accepted){cases++;if(!accepted)failed.Add(name);}
        void Reject(string name,ResidentBodyGeometryResult result,string reason) =>
            Expect(name,!result.BodyGeometryClear && result.Issues.Contains(reason) && !result.IsCompletePlacementProof);
        void Probe(string scene,Action<CharacterBody2D> action)
        {
            Node owned=null;
            try { owned=GD.Load<PackedScene>(scene).Instantiate(); action((CharacterBody2D)owned); }
            finally {if(GodotObject.IsInstanceValid(owned))owned.Free();}
        }
    }

    // The literal native-wall differential catches silently using a carrier
    // probe instead of the saved leaf whose body changed. No fake shape query.
    private static void RunSavedLeafControls(NativeFixture fixture, string leaves, string storage)
    {
        ResidentSubtypeDefinition type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id=="scrap-wanderer");
        string controls=Path.Combine(storage,"leaf-geometry-controls");Directory.CreateDirectory(controls);
        string path=Path.Combine(controls,type.Id+".tscn");
        Node root=GD.Load<PackedScene>(Path.Combine(leaves,type.Id+".tscn")).Instantiate();
        try
        {
            root.SceneFilePath="";
            root.GetNode<CollisionShape2D>("BodyCollider").Position+=new Vector2(9100,0);
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"save actual changed-body leaf counterexample");
        }
        finally{root.Free();}
        using var source=new CensusLeafSource(controls);
        CharacterBody2D actor=source.Raw(type);
        ResidentBodyGeometryResult changed=ResidentPlacementGeometry.CheckBody(actor,new(0,new Vector2(1100,500)),
            fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
        TestAssert.True(changed.WorldBodyBounds==new Rect2(10182,468,36,64)&&!changed.BodyGeometryClear&&changed.Issues.Contains("native_world"),
            "geometry census must query actual saved leaf body, not old carrier probe; changed="+changed.WorldBodyBounds+" issues="+string.Join(",",changed.Issues));
        GD.Print("RESIDENT_LEAF_GEOMETRY_DIFFERENTIAL_OK cases=1 changed_saved_body=true native_wall=true probe_fallback=false placement_certified=false");
        using var valid=new CensusLeafSource(leaves);
        CharacterBody2D unchanged=valid.Raw(type);
        ResidentBodyGeometryResult clear=ResidentPlacementGeometry.CheckBody(unchanged,new(0,new Vector2(1100,500)),
            fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
        TestAssert.True(clear.BodyGeometryClear&&clear.WorldBodyBounds==new Rect2(1082,468,36,64),"unchanged real leaf remains native-clear at differential point");

        var old=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);
        try
        {
            CharacterBody2D Old(string path)
            {if(!old.TryGetValue(path,out CharacterBody2D actor)){actor=(CharacterBody2D)GD.Load<PackedScene>(path).Instantiate();old.Add(path,actor);}return actor;}
            ResidentKeepOut[] before=OriginalSpawnReservations(Old).ToArray();
            source.Raw(type); // The new shifted leaf must never enter old26 reservations.
            TestAssert.True(before.SequenceEqual(OriginalSpawnReservations(Old)),"changed new leaf cannot rewrite old26/36 reservations");
        }
        finally{foreach(CharacterBody2D oldRoot in old.Values)oldRoot.Free();}

        ResidentSubtypeDefinition laser=CanyonResidentCatalog.Subtypes.Single(x=>x.Id=="precision-laser");
        ResidentPopulationDefinition slot=CanyonResidentCatalog.All.Single(x=>x.RouteNode==CanyonRouteNodeId.SealedCourtyard);
        Vector2 home=new(5153,1211);
        slot=slot with {Placement=new ResidentPlacementCandidate(home,
            CanyonWorldLayout.Nodes.Single(x=>x.Id==slot.RouteNode).WorldBounds,new[]{home})};
        CharacterBody2D first=valid.Configure(laser,slot),second=valid.Configure(laser,slot);
        try
        {
            TestAssert.True(first.GetInstanceId()!=second.GetInstanceId()&&!first.IsInsideTree()&&!second.IsInsideTree(),"real factory returns distinct once-configured roots, never raw cache");
            ResidentBodyGeometryResult body=ResidentPlacementGeometry.CheckBody(first,new(0,home),fixture.Space,fixture.Scope,
                slot.Placement.Territory,true,Array.Empty<ResidentKeepOut>());
            TestAssert.True(body.BodyGeometryClear&&body.WorldBodyBounds==new Rect2(5128,1172,50,68),"legal saved laser consumes real existing supported courtyard point");
        }
        finally{first.Free();second.Free();}
        int rejected=0;
        Reject(()=>valid.Configure(laser,slot with {Placement=null}),"null placement rejected, never fabricated");
        using(var borrowedSource=new CensusLeafSource(Path.Combine(storage,"invalid-missing-leaf")))
        {
            // Existing old scene saved under the exact expected leaf path;
            // absence of exported binding must be rejected with no fallback.
            string bare=Path.Combine(storage,"invalid-missing-leaf");Directory.CreateDirectory(bare);
            SaveVariant(type,CanyonEncounterCatalog.RustShellScene,bare,null);
            Reject(()=>borrowedSource.Raw(type),"old unbound carrier under leaf filename rejected");
        }
        string familyRoot=Path.Combine(storage,"invalid-family-leaf");Directory.CreateDirectory(familyRoot);
        SaveVariant(type,valid.PathFor(type),familyRoot,binding=>binding.Profile.Family=ResidentFamily.Ghost);
        using(var family=new CensusLeafSource(familyRoot))Reject(()=>family.Raw(type),"same saved body with wrong family rejected");
        ResidentSubtypeDefinition scatter=CanyonResidentCatalog.Subtypes.Single(x=>x.Id=="scrap-scatter");
        string gunRoot=Path.Combine(storage,"invalid-gun-leaf");Directory.CreateDirectory(gunRoot);
        SaveVariant(laser,valid.PathFor(scatter),gunRoot,binding=>
        {
            binding.Profile.SubtypeId=laser.Id;binding.Profile.Family=laser.Family;
            binding.Profile.Archetype=laser.Archetype;binding.Profile.Carrier=laser.Carrier;
        });
        using(var gun=new CensusLeafSource(gunRoot))
        {
            ResidentBodyGeometryResult same=ResidentPlacementGeometry.CheckBody(gun.Raw(laser),new(0,home),fixture.Space,fixture.Scope,
                slot.Placement.Territory,true,Array.Empty<ResidentKeepOut>());
            TestAssert.True(same.BodyGeometryClear&&same.WorldBodyBounds==new Rect2(5128,1172,50,68),"wrong actual gun has same native-clear literal body, so AABB cannot validate carrier");
            Reject(()=>gun.Configure(laser,slot),"correct laser profile/path on actual scatter gun rejected", "actual body/weapon carrier");
        }
        TestAssert.True(CanyonResidentCatalog.All.All(x=>x.Placement==null),"controls preserve all canonical unplaced identities");
        GD.Print($"RESIDENT_ACTUAL_LEAF_BOUNDARIES_OK rejections={rejected} native_body_differential=true same_aabb_wrong_gun=true distinct_configured_roots=true old_reservations=36 canonical_placements_authored=0");

        void Reject(Func<CharacterBody2D> action,string name,string message=null)
        {
            CharacterBody2D unexpected=null;bool caught=false;
            try{unexpected=action();}catch(ArgumentException ex){caught=message==null||ex.Message.Contains(message,StringComparison.Ordinal);}
            finally{if(GodotObject.IsInstanceValid(unexpected))unexpected.Free();}
            TestAssert.True(caught,name);rejected++;
        }
        static void SaveVariant(ResidentSubtypeDefinition expected,string donor,string folder,Action<ResidentSceneBinding> change)
        {
            Node actor=GD.Load<PackedScene>(donor).Instantiate();ResidentSubtypeProfile owned=null;
            try
            {
                actor.SceneFilePath="";
                if(change!=null)
                {
                    ResidentSceneBinding binding=actor.GetNode<ResidentSceneBinding>("ResidentBinding");
                    owned=(ResidentSubtypeProfile)binding.Profile.Duplicate();binding.Profile=owned;change(binding);
                }
                TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(actor,Path.Combine(folder,expected.Id+".tscn")),"save real isolated leaf negative control");
            }
            finally{actor.Free();owned?.Dispose();}
        }
    }

    // This source owns only instantiated roots, never borrowed saved resources.
    private sealed class CensusLeafSource : IDisposable
    {
        private readonly string _leaves;
        private readonly Dictionary<string,CharacterBody2D> _raw=new(StringComparer.Ordinal);
        public CensusLeafSource(string leaves){_leaves=leaves;}
        public CharacterBody2D Raw(ResidentSubtypeDefinition type)
        {
            if(!_raw.TryGetValue(type.Id,out CharacterBody2D actor))
            {
                string path=PathFor(type);
                Node root=GD.Load<PackedScene>(path)?.Instantiate();
                try
                {
                    ResidentSceneBinding binding=root?.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
                    if(root is not CharacterBody2D body || binding?.Profile==null || CountBindings(root)!=1 ||
                        binding.PresentationState!=ResidentSceneState.ExplicitPlaceholder ||
                        binding.Profile.SubtypeId!=type.Id || binding.Profile.Family!=type.Family ||
                        binding.Profile.Archetype!=type.Archetype || binding.Profile.Carrier!=type.Carrier ||
                        root.SceneFilePath.Replace('\\','/')!=path.Replace('\\','/'))
                        throw new ArgumentException("Actual saved leaf identity/body required; never fall back to a carrier probe.");
                    actor=body;_raw.Add(type.Id,actor);
                }
                catch{if(GodotObject.IsInstanceValid(root))root.Free();throw;}
            }
            return actor;
        }
        private readonly HashSet<string> _bodyIds=new(StringComparer.Ordinal);
        private readonly HashSet<string> _bodyTypes=new(StringComparer.Ordinal);
        private readonly HashSet<string> _configuredIds=new(StringComparer.Ordinal);
        private readonly HashSet<string> _configuredTypes=new(StringComparer.Ordinal);
        private readonly List<string> _diagnostics=new();
        public string PathFor(ResidentSubtypeDefinition type)=>Path.Combine(_leaves,type.Id+".tscn");
        public CharacterBody2D Configure(ResidentSubtypeDefinition type,ResidentPopulationDefinition slot)
        {
            string path=PathFor(type);
            CharacterBody2D actor=ResidentSceneBinding.Instantiate(GD.Load<PackedScene>(path),
                type with {SceneState=ResidentSceneState.ExplicitPlaceholder,ScenePath=path},slot,true);
            _configuredIds.Add(slot.StableId);_configuredTypes.Add(type.Id);return actor;
        }
        public CharacterBody2D Configure(ResidentPopulationDefinition slot,Vector2[] points,Rect2 territory)=>
            Configure(CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId),
                slot with {Placement=new ResidentPlacementCandidate(points[0],territory,points)});
        public int BodyIdentityCount=>_bodyIds.Count;
        public int BodySubtypeCount=>_bodyTypes.Count;
        public ResidentBodyGeometryResult QueryBody(ResidentPopulationDefinition slot,CharacterBody2D actor,Transform2D pose,
            NativeFixture fixture,Rect2 permitted,bool ground,IReadOnlyList<ResidentKeepOut> keepOut)
        {
            ResidentSubtypeDefinition type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId);
            ResidentSceneBinding binding=actor?.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
            if(!GodotObject.IsInstanceValid(actor)||actor.SceneFilePath.Replace('\\','/')!=PathFor(type).Replace('\\','/')||
                actor.SceneFilePath.Replace('\\','/')==type.ProbeScenePath.Replace('\\','/')||CountBindings(actor)!=1||
                binding?.Profile==null||binding.PresentationState!=ResidentSceneState.ExplicitPlaceholder||
                binding.Profile.SubtypeId!=type.Id||binding.Profile.Family!=type.Family||type.Family!=slot.Family||
                binding.Profile.Archetype!=type.Archetype||type.Archetype!=slot.Archetype||binding.Profile.Carrier!=type.Carrier)
                throw new ArgumentException("Query and count the actual saved subtype actor; reject aliases before native query or counters.");
            ResidentBodyGeometryResult result=ResidentPlacementGeometry.CheckBody(actor,pose,fixture.Space,fixture.Scope,permitted,ground,keepOut);
            _bodyIds.Add(slot.StableId);_bodyTypes.Add(slot.SubtypeId);return result;
        }
        public void Record(ResidentPopulationDefinition slot,string stage,string status,Vector2[] points,
            IReadOnlyList<string> issues=null,ResidentKeepOut witness=null,string stopReason=null)
        {
            _diagnostics.Add(System.Text.Json.JsonSerializer.Serialize(new{
                route=slot.RouteNode.ToString(),subtype=slot.SubtypeId,
                stable_ids=CanyonResidentCatalog.All.Where(x=>x.RouteNode==slot.RouteNode&&x.SubtypeId==slot.SubtypeId).Select(x=>x.StableId).ToArray(),
                stage,status,points=points.Select(x=>new[]{x.X,x.Y}).ToArray(),raw_issues=issues??Array.Empty<string>(),
                static_witness=witness==null?null:new{source=witness.SourceId,kind=witness.Kind.ToString(),bounds=witness.WorldBounds.ToString()},
                stop_reason=stopReason,placement_certified=false,motion_certified=false}));
        }
        public void WriteDiagnostics(string path)
        {
            TestAssert.Equal(50,_bodyIds.Count,"all real identities query actual saved subtype bodies");
            TestAssert.Equal(24,_bodyTypes.Count,"all real subtype bodies independently queried without activity-cache counts");
            File.WriteAllLines(path,_diagnostics);
            GD.Print($"RESIDENT_ACTUAL_LEAF_CENSUS_OK body_identities={_bodyIds.Count} body_subtypes={_bodyTypes.Count} configured_candidate_identities={_configuredIds.Count} configured_candidate_subtypes={_configuredTypes.Count} diagnostic_records={_diagnostics.Count} actual_saved_leaves=true actual_factory=true actual_ready=false placements_authored=0 motion_certified=false");
        }
        private static int CountBindings(Node root)=> (root is ResidentSceneBinding?1:0)+root.GetChildren().Sum(CountBindings);
        public void Dispose(){foreach(CharacterBody2D actor in _raw.Values)if(GodotObject.IsInstanceValid(actor))actor.Free();}
    }

    private static void RunCorridors(NativeFixture fixture)
    {
        var failures=new List<string>();int cases=0;
        string before=fixture.CollisionSnapshot();
        IReadOnlyList<ResidentKeepOut> environment=ResidentEnvironmentInventory.Capture(fixture.Scope).Regions;
        Probe(CanyonEncounterCatalog.RustShellScene, actor=>
        {
            Accept("saved_ground_forward",actor,new(800,604),new(928,604),true,new(782,572,164,64),environment);
            Accept("saved_ground_reverse",actor,new(928,604),new(800,604),true,new(782,572,164,64),environment);
            Accept("saved_descent_ground",actor,new(1800,824),new(1928,824),true,new(1782,792,164,64),environment);
            Accept("stationary_turret_geometry_only",actor,new(1040,604),new(1040,604),true,new(1022,572,36,64));
            Reject("intermediate_floor_gap",actor,new(8950,604),new(9050,604),true,"corridor_incomplete_support");
            Reject("intermediate_same_floor_body_wall",actor,new(9620,604),new(9780,604),true,"corridor_native_world");
            Reject("intermediate_thin_wall",actor,new(10100,500),new(10300,500),false,"corridor_native_world");
            Reject("intermediate_attack_area",actor,new(9200,600),new(9400,600),false,"corridor_native_hazard");
            Reject("intermediate_query_capacity",actor,new(10400,500),new(10600,500),false,"corridor_query_truncated");
            foreach(ResidentKeepOutKind kind in Enum.GetValues<ResidentKeepOutKind>())
                Reject("intermediate_reservation_"+kind,actor,new(11000,500),new(11100,500),false,
                    "corridor_reserved."+kind+":middle-control",
                    new[]{new ResidentKeepOut("middle-control",kind,new Rect2(11050,468,20,64))});
            Transform2D from=new(0,new Vector2(11000,500)),rotated=new(Mathf.Pi/2,new Vector2(11100,500));
            Expect("changed_basis_rejected",!Check(actor,from,rotated,false).CorridorClear&&
                Check(actor,from,rotated,false).Issues.Contains("unsupported_path_basis"));
            Expect("nonhorizontal_ground_rejected",Check(actor,new(0,new Vector2(800,604)),
                new(0,new Vector2(1800,824)),true).Issues.Contains("unsupported_ground_path"));
            Expect("missing_inventory_rejected",ResidentPlacementGeometry.CheckCorridor(actor,from,from,
                fixture.Space,fixture.Scope,fixture.Bounds,false,null).Issues.Contains("start.missing_keep_out_inventory"));
            Expect("invalid_endpoint_rejected",Check(actor,new(0,new Vector2(float.NaN,500)),from,false)
                .Issues.Contains("start.invalid_transform"));
            Expect("missing_actor_rejected",Check(null,from,from,false).Issues.Contains("start.missing_actor"));
        });
        Probe(CanyonEncounterCatalog.MothScene,actor=>
        {
            Accept("saved_air_horizontal",actor,new(800,450),new(928,450),false,new(784,434,160,32),environment);
            Accept("saved_air_vertical",actor,new(928,450),new(928,546),false,new(912,434,32,128),environment);
            Accept("saved_air_reverse",actor,new(928,546),new(928,450),false,new(912,434,32,128),environment);
            Reject("intermediate_layer_zero_electric",actor,new(2100,3930),new(2420,3930),false,"corridor_native_hazard");
        });
        Probe(CanyonEncounterCatalog.RustShellScene,actor=>
        {
            CollisionShape2D collider=actor.GetNode<CollisionShape2D>("BodyCollider");collider.Owner=null;actor.RemoveChild(collider);
            var offset=new Node2D {Name="CorridorAncestor",Position=new Vector2(100,0),Rotation=Mathf.Pi/2,Scale=new Vector2(2,2)};
            actor.AddChild(offset);offset.AddChild(collider);
            Accept("complete_collider_ancestor_path",actor,new(11000,1000),new(11128,1000),false,new(11036,964,256,72));
        });
        TestAssert.Equal(before,fixture.CollisionSnapshot(),"corridor checks do not alter actual saved native geometry");
        TestAssert.True(failures.Count==0,"resident corridor checks failed: "+string.Join("; ",failures));
        GD.Print($"RESIDENT_CORRIDOR_NATIVE_OK cases={cases} fixed_pose_translation=true motion_certified=false placement_certified=false");

        ResidentPathGeometryResult Check(CharacterBody2D actor,Transform2D from,Transform2D to,bool ground,
            IReadOnlyList<ResidentKeepOut> keep=null)=>ResidentPlacementGeometry.CheckCorridor(actor,from,to,
                fixture.Space,fixture.Scope,fixture.Bounds,ground,keep??Array.Empty<ResidentKeepOut>());
        void EndpointPreconditions(CharacterBody2D actor,Vector2 from,Vector2 to,bool ground,IReadOnlyList<ResidentKeepOut> keep)
        {
            foreach(Vector2 point in new[]{from,to})
            {
                ResidentBodyGeometryResult endpoint=ResidentPlacementGeometry.CheckBody(actor,new(0,point),fixture.Space,
                    fixture.Scope,fixture.Bounds,ground,keep??Array.Empty<ResidentKeepOut>());
                TestAssert.True(endpoint.BodyGeometryClear,"independent corridor endpoint must be clear at "+point+": "+string.Join(",",endpoint.Issues));
            }
        }
        void Accept(string name,CharacterBody2D actor,Vector2 from,Vector2 to,bool ground,Rect2 want,IReadOnlyList<ResidentKeepOut> keep=null)
        {
            EndpointPreconditions(actor,from,to,ground,keep);
            ResidentPathGeometryResult result=Check(actor,new(0,from),new(0,to),ground,keep);
            Expect(name,result.CorridorClear&&!result.IsCompletePlacementProof&&
                result.WorldCorridorBounds.Position.DistanceTo(want.Position)<.01f&&result.WorldCorridorBounds.Size.DistanceTo(want.Size)<.01f);
        }
        void Reject(string name,CharacterBody2D actor,Vector2 from,Vector2 to,bool ground,string reason,IReadOnlyList<ResidentKeepOut> keep=null)
        {
            EndpointPreconditions(actor,from,to,ground,keep);
            ResidentPathGeometryResult result=Check(actor,new(0,from),new(0,to),ground,keep);
            Expect(name,!result.CorridorClear&&!result.IsCompletePlacementProof&&result.Issues.Contains(reason));
        }
        void Expect(string name,bool accepted){cases++;if(!accepted)failures.Add(name);}
        void Probe(string scene,Action<CharacterBody2D> action)
        {
            Node owned=null;try{owned=GD.Load<PackedScene>(scene).Instantiate();action((CharacterBody2D)owned);}
            finally{if(GodotObject.IsInstanceValid(owned))owned.Free();}
        }
    }

    private static void RunArchiveResidentSpace(NativeFixture fixture)
    {
        var failed=new List<string>();int cases=0;
        var probes=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);Node parent=null;
        string before=fixture.CollisionSnapshot();
        try
        {
            CharacterBody2D Probe(string scene)
            {
                if(!probes.TryGetValue(scene,out CharacterBody2D body))
                {body=(CharacterBody2D)GD.Load<PackedScene>(scene).Instantiate();probes.Add(scene,body);}
                return body;
            }
            Rect2 wantShelf=new(1480,1420,240,40);
            CollisionShape2D shelf=fixture.Sectors[PrologueSectorId.CanyonDescent]
                .GetNode<CollisionShape2D>("Gameplay/Collision/ArchiveShelf/Collision");
            Rect2 actual=shelf.GlobalTransform*shelf.Shape.GetRect();
            Expect("archive_shelf_expands_left_without_roof_overlap",actual==wantShelf);
            ResidentEnvironmentSnapshot environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.Equal(0,environment.Issues.Count,"archive expansion requires actual interaction/hazard reservations");
            IReadOnlyList<ResidentKeepOut> keep=environment.Regions.Concat(OriginalSpawnReservations(Probe)).ToArray();
            ResidentPathGeometryResult charger=ResidentPlacementGeometry.CheckCorridor(Probe(CanyonEncounterCatalog.SkitterScene),
                new(0,new Vector2(1502,1421)),new(0,new Vector2(1694,1421)),fixture.Space,fixture.Scope,
                CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.ArchiveSideCave).WorldBounds,true,keep);
            Expect("archive_full192_charge_band_supported_and_reserved_clear",charger.CorridorClear&&!charger.IsCompletePlacementProof);
            CharacterBody2D player=(CharacterBody2D)GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate();
            try
            {
                ResidentPathGeometryResult passage=ResidentPlacementGeometry.CheckCorridor(player,
                    new(0,new Vector2(1404,1340)),new(0,new Vector2(1404,1548)),fixture.Space,fixture.Scope,fixture.Bounds,
                    false,Array.Empty<ResidentKeepOut>());
                Expect("archive_left_drop_keeps_full_player_body_clear",passage.CorridorClear&&!passage.IsCompletePlacementProof);
            }
            finally{player.Free();}
            parent=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate();
            MinimapTerrainCatalog catalog=parent.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            Expect("saved_parent_minimap_contains_expanded_shelf",Enumerable.Range(0,catalog.SolidCount)
                .Select(i=>catalog.Solid(i)).Any(solid=>solid.LocalToWorld*solid.LocalBounds==wantShelf));
            using MinimapTerrainCatalog rebuilt=MinimapTerrainCatalogBuilder.Build(
                parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray());
            Expect("saved_parent_minimap_matches_current_five_sector_geometry",catalog.Revision==rebuilt.Revision&&
                catalog.SerializedValues.AsSpan().SequenceEqual(rebuilt.SerializedValues));
        }
        finally
        {
            if(GodotObject.IsInstanceValid(parent))parent.Free();
            foreach(CharacterBody2D probe in probes.Values)if(GodotObject.IsInstanceValid(probe))probe.Free();
        }
        TestAssert.Equal(before,fixture.CollisionSnapshot(),"archive query proof does not modify native geometry");
        TestAssert.True(failed.Count==0,"resident archive space checks failed: "+string.Join("; ",failed));
        GD.Print($"RESIDENT_ARCHIVE_SPACE_OK cases={cases} charge_band=192 left_player_drop=true parent_minimap_current=true actual_patrol_certified=false");
        void Expect(string name,bool pass){cases++;if(!pass)failed.Add(name);}
    }

    private sealed record CorridorCandidate(Vector2 Home,IReadOnlyList<Vector2> Points,Rect2 Occupancy);

    private static Dictionary<string,CorridorCandidate> ChooseCompatible(
        IReadOnlyList<(string Id,IReadOnlyList<CorridorCandidate> Options)> slots,int nodeBudget,
        out int nodes,out bool exhausted)
    {
        int visited=0;bool capped=false;
        var chosen=new Dictionary<string,CorridorCandidate>(StringComparer.Ordinal);
        var best=new Dictionary<string,CorridorCandidate>(StringComparer.Ordinal);
        Search(slots.ToList());nodes=visited;exhausted=capped;return best;

        void Search(List<(string Id,IReadOnlyList<CorridorCandidate> Options)> remaining)
        {
            if(chosen.Count>best.Count) best=new(chosen,StringComparer.Ordinal);
            if(remaining.Count==0||chosen.Count+remaining.Count<=best.Count) return;
            if(visited>=nodeBudget){capped=true;return;}
            visited++;
            var next=remaining.Select(slot=>(Slot:slot,Options:slot.Options.Where(candidate=>
                chosen.Values.All(existing=>!existing.Occupancy.Intersects(candidate.Occupancy))).ToArray()))
                .OrderBy(x=>x.Options.Length).ThenByDescending(x=>x.Options.Select(c=>c.Occupancy.Area).DefaultIfEmpty(0).Max())
                .ThenBy(x=>x.Slot.Id,StringComparer.Ordinal).First();
            List<(string Id,IReadOnlyList<CorridorCandidate> Options)> rest=remaining.Where(x=>x.Id!=next.Slot.Id).ToList();
            // Preserve the deterministic native candidate preference order.
            // Backtracking, not a greedy commitment, gives long paths a second chance.
            foreach(CorridorCandidate candidate in next.Options)
            {
                chosen.Add(next.Slot.Id,candidate);Search(rest);chosen.Remove(next.Slot.Id);
                if(best.Count==slots.Count||capped) return;
            }
            // A missing/blocked slot remains explicitly missing; returning the
            // largest partial set never changes the required population count.
            Search(rest);
        }
    }

    private static void RunCorridorSelectionControls(NativeFixture fixture)
    {
        CharacterBody2D actor=null;
        try
        {
            actor=(CharacterBody2D)GD.Load<PackedScene>(CanyonEncounterCatalog.RustShellScene).Instantiate();
            var paths=new List<CorridorCandidate>();
            foreach(float x in new[]{11000f,11300f,11600f})
            {
                Vector2 a=new(x,1000),b=new(x+128,1000);
                ResidentPathGeometryResult proof=ResidentPlacementGeometry.CheckCorridor(actor,new(0,a),new(0,b),
                    fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
                TestAssert.True(proof.CorridorClear&&!proof.IsCompletePlacementProof,"solver controls consume real clear native corridors");
                paths.Add(new(a,new[]{a,b,a},proof.WorldCorridorBounds.Grow(24)));
            }
            var failed=new List<string>();int cases=0;
            var backtracking=new (string,IReadOnlyList<CorridorCandidate>)[]
            {("A",new[]{paths[0],paths[1]}),("B",new[]{paths[0],paths[2]}),("C",new[]{paths[0],paths[2]})};
            Dictionary<string,CorridorCandidate> selected=ChooseCompatible(backtracking,1000,out int visited,out bool capped);
            Expect("backtrack_early_path_to_fit_all",selected.Count==3&&selected["A"].Home==new Vector2(11300,1000)&&visited>0&&!capped);
            selected=ChooseCompatible(new (string,IReadOnlyList<CorridorCandidate>)[]{("A",new[]{paths[0]}),("B",new[]{paths[0]})},
                1000,out visited,out capped);
            Expect("do_not_assign_overlapping_corridors",selected.Count==1&&!capped);
            selected=ChooseCompatible(new (string,IReadOnlyList<CorridorCandidate>)[]{("A",new[]{paths[0]}),
                ("B",new[]{paths[1]}),("missing",Array.Empty<CorridorCandidate>())},1000,out visited,out capped);
            Expect("missing_slot_is_not_fake_completion",selected.Count==2&&!selected.ContainsKey("missing")&&!capped);
            selected=ChooseCompatible(backtracking,0,out visited,out capped);
            Expect("budget_is_unfinished_not_impossible",selected.Count==0&&visited==0&&capped);
            TestAssert.True(failed.Count==0,"resident corridor selection checks failed: "+string.Join("; ",failed));
            GD.Print($"RESIDENT_CORRIDOR_SOLVER_OK cases={cases} joint_occupancy=true search_not_placement_proof=true");
            void Expect(string name,bool accepted){cases++;if(!accepted)failed.Add(name);}
            RunTemplateDistributionControls(fixture,actor);
        }
        finally{if(GodotObject.IsInstanceValid(actor))actor.Free();}
    }

    private static IReadOnlyList<Vector2[]> SpreadActivityTemplates(IReadOnlyList<Vector2[]> templates,Rect2 route)
    {
        // Order DESCRIPTIONS only. Every accepted candidate still needs both
        // original native corridor checks; spatial distance is not clearance.
        var cells=templates.GroupBy(points=>
        {
            Vector2 relative=(points[0]-route.Position)/route.Size;
            int x=Math.Clamp((int)MathF.Floor(relative.X*4),0,3),y=Math.Clamp((int)MathF.Floor(relative.Y*4),0,3);
            return y*4+x;
        }).OrderBy(x=>x.Key).Select(cell=>new Queue<Vector2[]>(cell
            .OrderBy(points=>points[0].Y).ThenBy(points=>points[0].X)
            .ThenBy(points=>string.Join("/",points.Select(p=>string.Create(CultureInfo.InvariantCulture,$"{p.X},{p.Y}"))),StringComparer.Ordinal)))
            .ToArray();
        var ordered=new List<Vector2[]>(templates.Count);
        while(cells.Any(x=>x.Count>0))foreach(Queue<Vector2[]> cell in cells)if(cell.Count>0)ordered.Add(cell.Dequeue());
        return ordered.AsReadOnly();
    }

    private static void RunTemplateDistributionControls(NativeFixture fixture,CharacterBody2D actor)
    {
        var failed=new List<string>();int cases=0;
        var templates=Enumerable.Range(0,16).Select(i=>new[]{new Vector2(11000+4*i,1000),new Vector2(11128+4*i,1000)})
            .Concat(new[]{new[]{new Vector2(11300,1000),new Vector2(11428,1000)},
                new[]{new Vector2(11600,1000),new Vector2(11728,1000)}}).ToArray();
        Rect2 route=new(10900,900,1000,800);
        // These are actual native clear corridors, not fake compatibility boxes.
        foreach(Vector2[] points in templates)
            TestAssert.True(ResidentPlacementGeometry.CheckCorridor(actor,new(0,points[0]),new(0,points[1]),
                fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>()).CorridorClear,
                "distribution controls require independently clear native corridors");
        CorridorCandidate[] Options(IEnumerable<Vector2[]> values)=>values.Select(points=>new CorridorCandidate(points[0],
            Array.AsReadOnly(points),new Rect2(points[0]+new Vector2(-18,-32),new Vector2(164,64)).Grow(24))).ToArray();
        int Fit(IEnumerable<Vector2[]> values)
        {
            CorridorCandidate[] options=Options(values);
            return ChooseCompatible(new (string,IReadOnlyList<CorridorCandidate>)[]{("A",options),("B",options),("C",options)},
                1000,out _,out _).Count;
        }
        TestAssert.Equal(1,Fit(templates.Take(16)),"literal first16 are a real cluster, not three independent spaces");
        IReadOnlyList<Vector2[]> ordered=SpreadActivityTemplates(templates,route);
        Expect("early_spatial_representatives_fit_three",Fit(ordered.Take(16))==3);
        Vector2[][] layers=templates.Take(16).Concat(new[]{new[]{new Vector2(11000,1400),new Vector2(11128,1400)},
            new[]{new Vector2(11300,1400),new Vector2(11428,1400)}}).ToArray();
        Expect("later_height_not_starved_by_first_strip",SpreadActivityTemplates(layers,route).Take(16).Any(x=>x[0].Y==1400));
        string Signature(IEnumerable<Vector2[]> values)=>string.Join("|",values.Select(points=>string.Join("/",points.Select(p=>
            string.Create(CultureInfo.InvariantCulture,$"{p.X},{p.Y}")))));
        Expect("reversed_enumeration_same_order",Signature(ordered)==Signature(SpreadActivityTemplates(templates.Reverse().ToArray(),route)));
        Expect("all_templates_retained_without_native_claim",ordered.Count==18&&ordered.All(x=>templates.Contains(x))&&ordered.Distinct().Count()==18);
        Expect("empty_templates_stay_empty",SpreadActivityTemplates(Array.Empty<Vector2[]>(),route).Count==0);
        TestAssert.True(failed.Count==0,"resident template distribution checks failed: "+string.Join("; ",failed));
        GD.Print($"RESIDENT_TEMPLATE_DISTRIBUTION_OK cases={cases} native_templates=18 early_capacity=3 ordering_only=true placement_certified=false");
        void Expect(string name,bool pass){cases++;if(!pass)failed.Add(name);}
    }

    private static IReadOnlyList<ResidentKeepOut> OriginalSpawnReservations(Func<string,CharacterBody2D> probe)
    {
        var result=new List<ResidentKeepOut>();
        foreach(CanyonEncounterDefinition encounter in CanyonEncounterCatalog.Normal)
        for(int index=0;index<encounter.Enemies.Count;index++)
        {
            CanyonEnemyPlacement enemy=encounter.Enemies[index];
            CollisionShape2D shape=probe(enemy.ScenePath).GetNode<CollisionShape2D>("BodyCollider");
            foreach((string suffix,Vector2 point) in enemy.AlternateWorldPosition is Vector2 alternate
                ?new[]{("primary",enemy.WorldPosition),("alternate",alternate)}:new[]{("primary",enemy.WorldPosition)})
            {
                Rect2 body=new Transform2D(0,point)*shape.Transform*shape.Shape.GetRect();
                if(encounter.RouteNode==CanyonRouteNodeId.CounterweightLift&&enemy.ScenePath==CanyonEncounterCatalog.RustShellScene)
                    body=body.Merge(new Rect2(body.Position+new Vector2(0,4),body.Size));
                result.Add(new("old-spawn:"+encounter.StableId+":"+index+":"+suffix,ResidentKeepOutKind.OriginalEnemy,body));
            }
        }
        TestAssert.Equal(36,result.Count,"old26 identities reserve36 primary/alternate body records, not36 actors");
        return result.AsReadOnly();
    }

    // Full conservative activity-enclosure authoring investigation. Even a
    // successful joint candidate does not certify native AI, attacks or motion.
    private static void RunActivityCandidateCensus(NativeFixture fixture,CensusLeafSource leafSource)
    {
        var probes=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);
        var choices=new Dictionary<string,IReadOnlyList<CorridorCandidate>>(StringComparer.Ordinal);
        var cappedBuckets=new HashSet<string>(StringComparer.Ordinal);
        var selected=new Dictionary<string,CorridorCandidate>(StringComparer.Ordinal);
        var cappedRegions=new HashSet<CanyonRouteNodeId>();
        int checks=0,jointNodes=0;bool searchCapped=false;
        string before=fixture.CollisionSnapshot();
        try
        {
            CharacterBody2D Probe(string scene)
            {
                if(!probes.TryGetValue(scene,out CharacterBody2D actor))
                {actor=(CharacterBody2D)GD.Load<PackedScene>(scene).Instantiate();probes.Add(scene,actor);}
                return actor;
            }
            ResidentEnvironmentSnapshot environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.Equal(0,environment.Issues.Count,"activity search requires complete known saved-environment inventory");
            var keepOut=environment.Regions.Concat(OriginalSpawnReservations(Probe)).ToArray();
            // Reproduce cramped previous body-only samples on the actual map.
            CharacterBody2D charger=Probe(CanyonEncounterCatalog.SkitterScene);
            foreach((string name,Vector2 home,ResidentKeepOut[] additional) in new[]
            {
                ("entry",new Vector2(486,637),new[]{new ResidentKeepOut("new-body-spacing:entry-wanderer",ResidentKeepOutKind.OriginalEnemy,new Rect2(384,548,84,112))}),
                ("archive",new Vector2(1558,1261),new[]{new ResidentKeepOut("new-body-spacing:archive-wanderer",ResidentKeepOutKind.OriginalEnemy,new Rect2(1456,1172,84,112))}),
                ("gear",new Vector2(2102,3997),Array.Empty<ResidentKeepOut>())
            })
            {
                ResidentKeepOut[] reservations=keepOut.Concat(additional).ToArray();
                ResidentBodyGeometryResult body=ResidentPlacementGeometry.CheckBody(charger,new(0,home),fixture.Space,fixture.Scope,
                    fixture.Bounds,true,reservations);
                TestAssert.True(body.BodyGeometryClear,"previous sampled charger home itself remains clear: "+name);
                foreach(int direction in new[]{-1,1})
                {
                    ResidentPathGeometryResult path=ResidentPlacementGeometry.CheckCorridor(charger,new(0,home),
                        new(0,home+new Vector2(direction*64,0)),fixture.Space,fixture.Scope,fixture.Bounds,true,reservations);
                    TestAssert.True(!path.CorridorClear&&!path.IsCompletePlacementProof,"cramped previous home must not certify64px charger motion: "+name);
                    GD.Print($"RESIDENT_CRAMPED_SAMPLE route={name} direction={direction} body_clear=true corridor_clear=false reasons={string.Join(",",path.Issues)}");
                }
            }
            GD.Print("RESIDENT_CRAMPED_CANDIDATES_OK identities=3 directions=6 same_checker=true originals_preserved=true");

            foreach(var group in CanyonResidentCatalog.All.GroupBy(x=>(x.RouteNode,x.SubtypeId)))
            {
                ResidentPopulationDefinition slot=group.First();
                ResidentSubtypeDefinition type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId);
                CharacterBody2D actor=leafSource.Raw(type);
                CollisionShape2D collider=actor.GetNode<CollisionShape2D>("BodyCollider");
                Rect2 local=collider.Transform*collider.Shape.GetRect();
                Rect2 permitted=CanyonWorldLayout.Nodes.Single(x=>x.Id==slot.RouteNode).WorldBounds;
                bool ground=slot.Archetype is not(ResidentArchetype.Flyer or ResidentArchetype.Diver);
                var options=new List<CorridorCandidate>();int bucketChecks=0;
                Vector2[][] described=Templates(slot,local,permitted).Take(8193).ToArray();
                bool capped=described.Length>8192;
                if(capped)leafSource.Record(slot,"static_filter","budget_stopped",Array.Empty<Vector2>(),stopReason:"description_limit");
                Vector2[][] filtered=described.Take(8192).Where(points=>
                {
                    Vector2 low=new(points.Min(x=>x.X),points.Min(x=>x.Y)),high=new(points.Max(x=>x.X),points.Max(x=>x.Y));
                    Rect2 enclosure=(new Transform2D(0,low)*local).Merge(new Transform2D(0,high)*local);
                    if(!permitted.Encloses(enclosure))
                    {leafSource.Record(slot,"static_filter","rejected",points,new[]{"route_bounds"});return false;}
                    ResidentKeepOut witness=keepOut.FirstOrDefault(x=>x.WorldBounds.Intersects(enclosure));
                    if(witness!=null)
                    {leafSource.Record(slot,"static_filter","rejected",points,new[]{"reserved."+witness.Kind+":"+witness.SourceId},witness);return false;}
                    return true;
                }).ToArray();
                foreach(Vector2[] points in SpreadActivityTemplates(filtered,permitted))
                {
                    if(bucketChecks>=96||checks>=3000||options.Count>=16)
                    {capped=true;leafSource.Record(slot,"outward","budget_stopped",points,
                        stopReason:bucketChecks>=96?"bucket_limit":checks>=3000?"global_limit":"option_limit");break;}
                    Vector2 minimum=new(points.Min(x=>x.X),points.Min(x=>x.Y)),maximum=new(points.Max(x=>x.X),points.Max(x=>x.Y));
                    Rect2 enclosure=new Transform2D(0,minimum)*local;
                    enclosure=enclosure.Merge(new Transform2D(0,maximum)*local);
                    if(!permitted.Encloses(enclosure)||keepOut.Any(x=>x.WorldBounds.Intersects(enclosure))) continue;
                    CharacterBody2D configured=leafSource.Configure(slot,points,permitted);
                    try
                    {
                    bucketChecks++;checks++;
                    ResidentPathGeometryResult outward=ResidentPlacementGeometry.CheckCorridor(configured,new(0,minimum),new(0,maximum),
                        fixture.Space,fixture.Scope,permitted,ground,keepOut);
                    if(!outward.CorridorClear)
                    {leafSource.Record(slot,"outward","rejected",points,outward.Issues);leafSource.Record(slot,"backward","not_attempted",points);continue;}
                    bucketChecks++;checks++;
                    ResidentPathGeometryResult backward=ResidentPlacementGeometry.CheckCorridor(configured,new(0,maximum),new(0,minimum),
                        fixture.Space,fixture.Scope,permitted,ground,keepOut);
                    if(!backward.CorridorClear){leafSource.Record(slot,"backward","rejected",points,backward.Issues);continue;}
                    TestAssert.True(!outward.IsCompletePlacementProof&&!backward.IsCompletePlacementProof,"enclosure is never actualAI/placement proof");
                    // Conservative WHOLE-enclosure spacing: two Grow24 envelopes
                    // leave48px between separate activity enclosures, not24px.
                    options.Add(new(points[0],Array.AsReadOnly(points),outward.WorldCorridorBounds.Grow(24)));
                    leafSource.Record(slot,"backward","accepted",points);
                    }
                    finally{configured.Free();}
                }
                foreach(ResidentPopulationDefinition identity in group)
                {choices.Add(identity.StableId,options.AsReadOnly());if(capped)cappedBuckets.Add(identity.StableId);}
                searchCapped|=capped;
                GD.Print($"RESIDENT_ACTIVITY_OPTIONS route={slot.RouteNode} subtype={slot.SubtypeId} identities={group.Count()} candidates={options.Count} corridor_checks={bucketChecks} template_limit_reached={capped.ToString().ToLowerInvariant()} described={described.Length} static_filtered={filtered.Length} spatial_cells=4x4 envelope_only=true");
            }
            foreach(var region in CanyonResidentCatalog.All.GroupBy(x=>x.RouteNode))
            {
                var slots=region.Select(slot=>(Id:slot.StableId,Options:(IReadOnlyList<CorridorCandidate>)choices[slot.StableId]
                    .Where(candidate=>selected.Values.All(previous=>!candidate.Occupancy.Intersects(previous.Occupancy))).ToArray())).ToArray();
                Dictionary<string,CorridorCandidate> joint=ChooseCompatible(slots,20000,out int visited,out bool capped);
                jointNodes+=visited;searchCapped|=capped;
                if(capped)cappedRegions.Add(region.Key);
                foreach(var entry in joint)selected.Add(entry.Key,entry.Value);
                GD.Print($"RESIDENT_ACTIVITY_REGION route={region.Key} required={region.Count()} selected={joint.Count} missing={region.Count()-joint.Count} joint_nodes={visited} joint_budget_exhausted={capped.ToString().ToLowerInvariant()}");
            }
            foreach(ResidentPopulationDefinition slot in CanyonResidentCatalog.All)
                if(selected.TryGetValue(slot.StableId,out CorridorCandidate candidate))
                {
                    Rect2 territory=CanyonWorldLayout.Nodes.Single(x=>x.Id==slot.RouteNode).WorldBounds;
                    CharacterBody2D configured=leafSource.Configure(slot,candidate.Points.ToArray(),territory);
                    try
                    {
                        Vector2 low=new(candidate.Points.Min(x=>x.X),candidate.Points.Min(x=>x.Y)),high=new(candidate.Points.Max(x=>x.X),candidate.Points.Max(x=>x.Y));
                        bool ground=slot.Archetype is not(ResidentArchetype.Flyer or ResidentArchetype.Diver);
                        ResidentPathGeometryResult check=ResidentPlacementGeometry.CheckCorridor(configured,new(0,low),new(0,high),fixture.Space,fixture.Scope,territory,ground,keepOut);
                        TestAssert.True(check.CorridorClear,"individually configured selected real identity must retain native candidate clearance "+slot.StableId);
                    }
                    finally{configured.Free();}
                    GD.Print(string.Create(CultureInfo.InvariantCulture,$"RESIDENT_ACTIVITY_CANDIDATE id={slot.StableId} subtype={slot.SubtypeId} route={slot.RouteNode} home={candidate.Home} occupancy={candidate.Occupancy} points={string.Join("/",candidate.Points)} authored=false motion_certified=false"));
                }
                else
                {
                    string reason=cappedRegions.Contains(slot.RouteNode)?"joint_budget":choices[slot.StableId].Count>0?"candidate_set_unselected":cappedBuckets.Contains(slot.StableId)?"sampling_budget":"no_sampled_template";
                    leafSource.Record(slot,"joint",cappedRegions.Contains(slot.RouteNode)?"budget_stopped":"not_selected",Array.Empty<Vector2>(),stopReason:cappedRegions.Contains(slot.RouteNode)?"joint_node_limit":null);
                    GD.Print($"RESIDENT_ACTIVITY_UNRESOLVED id={slot.StableId} route={slot.RouteNode} reason={reason} map_impossibility_proved=false");
                }
            TestAssert.Equal(50,choices.Count,"all50 identities participate without silently reducing population");
            CorridorCandidate[] all=selected.Values.ToArray();
            TestAssert.True(all.SelectMany((a,i)=>all.Skip(i+1).Select(b=>a.Occupancy.Intersects(b.Occupancy))).All(x=>!x),"allselected fullactivity enclosures includingcrossregion spacing are disjoint");
            TestAssert.True(CanyonResidentCatalog.All.All(x=>x.Placement==null),"investigation does not silently author incomplete placements");
            TestAssert.Equal(before,fixture.CollisionSnapshot(),"activity search leaves saved world geometry unchanged");
            GD.Print($"RESIDENT_ACTIVITY_CENSUS_DONE identities=50 selected={selected.Count} unresolved={50-selected.Count} corridor_checks={checks} joint_nodes={jointNodes} search_capped={searchCapped.ToString().ToLowerInvariant()} placements_authored=0 motion_certified=false diagnostic_only=true");
        }
        finally{foreach(CharacterBody2D actor in probes.Values)if(GodotObject.IsInstanceValid(actor))actor.Free();}

        IEnumerable<Vector2[]> Templates(ResidentPopulationDefinition slot,Rect2 local,Rect2 route)
        {
            if(slot.Archetype is ResidentArchetype.Flyer or ResidentArchetype.Diver)
            {
                for(float y=route.Position.Y-local.Position.Y+8;y<route.End.Y-local.End.Y-8;y+=32)
                for(float x=route.Position.X-local.Position.X+8;x<route.End.X-local.End.X-8;x+=32)
                {
                    Vector2 home=new(x,y);
                    if(slot.Archetype==ResidentArchetype.Flyer)
                        yield return new[]{home,home+new Vector2(128,0),home+new Vector2(128,96),home+new Vector2(0,96),home};
                    else foreach(float dx in new[]{0f,-64f,64f})
                        yield return new[]{home,home+new Vector2(-32,0),home+new Vector2(32,0),home+new Vector2(dx,192),home};
                }
                yield break;
            }
            float half=slot.Archetype switch{ResidentArchetype.Wanderer=>64,ResidentArchetype.Charger=>96,ResidentArchetype.Heavy=>32,_=>0};
            var floors=Descendants(fixture.Sectors[slot.Sector].GetNode("Gameplay/Collision")).OfType<CollisionShape2D>()
                .Where(x=>!x.Disabled&&!x.OneWayCollision&&x.Shape is RectangleShape2D&&x.GetParent() is StaticBody2D body&&
                    (body.CollisionLayer&CombatCollisionLayers.World)!=0&&Mathf.Abs(x.GlobalTransform.X.Y)<.001f&&
                    Mathf.Abs(x.GlobalTransform.Y.X)<.001f&&x.GlobalTransform.X.X>0&&x.GlobalTransform.Y.Y>0)
                .Select(x=>x.GlobalTransform*x.Shape.GetRect()).GroupBy(x=>x.Position.Y).OrderBy(x=>x.Key);
            foreach(var level in floors)
            {
                var strips=new List<Rect2>();
                foreach(Rect2 floor in level.OrderBy(x=>x.Position.X))
                    if(strips.Count>0&&floor.Position.X<=strips[^1].End.X+.001f)strips[^1]=strips[^1].Merge(floor);
                    else strips.Add(floor);
                foreach(Rect2 floor in strips)
                {
                    float left=floor.Position.X-local.Position.X+half+8,right=floor.End.X-local.End.X-half-8;
                    if(left>right)continue;
                    var xs=new List<float>{(left+right)/2,left,right};for(float x=left;x<=right;x+=16)xs.Add(x);
                    foreach(float x in xs.Distinct())
                    {
                        Vector2 home=new(x,floor.Position.Y-local.End.Y);
                        if(slot.Archetype==ResidentArchetype.Turret)yield return new[]{home};
                        else if(slot.Archetype==ResidentArchetype.Charger)
                            yield return new[]{home,home+new Vector2(-32,0),home+new Vector2(32,0),home+new Vector2(96,0),home+new Vector2(-96,0),home};
                        else yield return new[]{home,home+new Vector2(-half,0),home+new Vector2(half,0),home};
                    }
                }
            }
        }
        static IEnumerable<Node> Descendants(Node root)
        {
            yield return root;using Godot.Collections.Array children=(Godot.Collections.Array)root.GetChildren();
            for(int i=0;i<children.Count;i++)foreach(Node child in Descendants(children[i].AsGodotObject() as Node))yield return child;
        }
    }

    // Native authoring investigation only. A sampled clear BODY is not an
    // authored placement, a patrol/attack corridor, or a completed family actor.
    private static void RunBodyCandidateCensus(NativeFixture fixture,CensusLeafSource leafSource)
    {
        ResidentEnvironmentSnapshot environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
        TestAssert.Equal(0,environment.Issues.Count,"census requires valid actual saved environment inventory");
        var keepOut=environment.Regions.ToList();
        int oldSites=0,found=0,attempted=0,queries=0;
        var probes=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);
        string before=fixture.CollisionSnapshot();
        try
        {
            CharacterBody2D Probe(string scene)
            {
                if (!probes.TryGetValue(scene,out CharacterBody2D actor))
                { actor=(CharacterBody2D)GD.Load<PackedScene>(scene).Instantiate();probes.Add(scene,actor); }
                return actor;
            }
            IReadOnlyList<ResidentKeepOut> old=OriginalSpawnReservations(Probe);keepOut.AddRange(old);oldSites=old.Count;
            TestAssert.Equal(36,oldSites,"26 old identities have36 primary/alternate candidate records, not36 actors");
            foreach (ResidentPopulationDefinition slot in CanyonResidentCatalog.All)
            {
                attempted++;
                ResidentSubtypeDefinition type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId);
                CharacterBody2D probe=leafSource.Raw(type);
                CollisionShape2D shape=probe.GetNode<CollisionShape2D>("BodyCollider");
                Rect2 bodyLocal=shape.Transform*shape.Shape.GetRect();
                bool ground=slot.Archetype is not (ResidentArchetype.Flyer or ResidentArchetype.Diver);
                Rect2 permitted=CanyonWorldLayout.SectorBounds[slot.Sector];
                int slotQueries=0;
                Vector2? selected=null;Rect2 selectedBounds=default;
                foreach (Vector2 candidate in Candidates(slot,bodyLocal,ground))
                {
                    if (slotQueries>=160) break;
                    if (!CanyonWorldLayout.TryResolveRouteNode(candidate,out CanyonRouteNodeId route)||route!=slot.RouteNode) continue;
                    slotQueries++;queries++;
                    ResidentBodyGeometryResult check=leafSource.QueryBody(slot,probe,new Transform2D(0,candidate),fixture,permitted,ground,keepOut);
                    if (!check.BodyGeometryClear) continue;
                    selected=candidate;selectedBounds=check.WorldBodyBounds;found++;break;
                }
                if (selected is Vector2 position)
                {
                    // A new conservative spacing policy for this DIAGNOSTIC
                    // sample only; not an old wave rule or final territory.
                    keepOut.Add(new("sampled-resident:"+slot.StableId,ResidentKeepOutKind.OriginalEnemy,selectedBounds.Grow(24)));
                    GD.Print(string.Create(CultureInfo.InvariantCulture,$"RESIDENT_BODY_CANDIDATE id={slot.StableId} subtype={slot.SubtypeId} route={slot.RouteNode} x={position.X:0.###} y={position.Y:0.###} ground={ground} queries={slotQueries} body_only=true authored=false"));
                }
                else GD.Print($"RESIDENT_BODY_CANDIDATE_MISSING id={slot.StableId} route={slot.RouteNode} queries={slotQueries} sampling_not_impossibility_proof=true");
            }
            TestAssert.Equal(50,attempted,"every allocated identity participates in native body census");
            TestAssert.True(found>0&&found<=50,"census has actual clear-body witnesses, no invented or duplicate identities");
            TestAssert.True(CanyonResidentCatalog.All.All(x=>x.Placement==null),"diagnostic census never silently authors uncertified placements");
            TestAssert.Equal(before,fixture.CollisionSnapshot(),"census changes no saved world collider/pose/state");
            GD.Print($"RESIDENT_BODY_CENSUS_OK identities=50 candidates={found} missing={50-found} queries={queries} old_spawn_candidates=36 placements_authored=0 patrol_certified=false diagnostic_only=true");
        }
        finally { foreach (CharacterBody2D probe in probes.Values) if (GodotObject.IsInstanceValid(probe)) probe.Free(); }

        IEnumerable<Vector2> Candidates(ResidentPopulationDefinition slot,Rect2 local,bool ground)
        {
            if (!ground)
            {
                Rect2 route=CanyonWorldLayout.Nodes.Single(x=>x.Id==slot.RouteNode).WorldBounds;
                for(float y=route.Position.Y+64;y<route.End.Y-32;y+=128)
                for(float x=route.Position.X+64;x<route.End.X-32;x+=128) yield return new Vector2(x,y);
                yield break;
            }
            Node terrain=fixture.Sectors[slot.Sector].GetNode("Gameplay/Collision");
            foreach (CollisionShape2D floor in Descendants(terrain).OfType<CollisionShape2D>())
            {
                if (floor.Disabled||floor.Shape is not RectangleShape2D||floor.GetParent() is not StaticBody2D body||
                    (body.CollisionLayer&CombatCollisionLayers.World)==0) continue;
                Transform2D pose=floor.GlobalTransform;
                if (Mathf.Abs(pose.X.Y)>.001f||Mathf.Abs(pose.Y.X)>.001f||pose.X.X<=0||pose.Y.Y<=0) continue;
                Rect2 top=pose*floor.Shape.GetRect();
                float start=top.Position.X-local.Position.X+8,end=top.End.X-local.End.X-8;
                for(float x=start;x<=end;x+=64) yield return new Vector2(x,top.Position.Y-local.End.Y);
            }
        }
        static IEnumerable<Node> Descendants(Node root)
        {
            yield return root;
            using Godot.Collections.Array children=(Godot.Collections.Array)root.GetChildren();
            for(int i=0;i<children.Count;i++) foreach(Node child in Descendants(children[i].AsGodotObject() as Node)) yield return child;
        }
    }

    private static async Task RunEnvironment(NativeFixture fixture)
    {
        var failures = new List<string>();
        int cases = 0;
        string before = fixture.CollisionSnapshot();
        ResidentEnvironmentSnapshot actual = ResidentEnvironmentInventory.Capture(fixture.Scope);
        Expect("saved_inventory_clean", actual.Issues.Count == 0);
        Expect("saved_noninteraction_inventory_count", actual.Regions.Count(x=>x.Kind!=ResidentKeepOutKind.PlayerInteraction) == 81);
        Expect("stable_source_unique", actual.Regions.Select(x=>x.SourceId).Distinct().Count()==actual.Regions.Count);
        Expect("inventory_is_not_placement_certificate", !actual.IsCompletePlacementProof);
        foreach ((string id,ResidentKeepOutKind kind,Rect2 bounds) in new[]
        {
            ("recovery:CanyonEntrance/Gameplay/FallRecovery/RecoveryVolume/Collision",ResidentKeepOutKind.Hazard,new Rect2(0,704,1280,16)),
            ("recovery:CanyonDescent/Gameplay/FallRecovery/RecoveryVolume/Collision",ResidentKeepOutKind.Hazard,new Rect2(1280,3584,336,16)),
            ("recovery:CanyonDescent/Gameplay/FallRecovery/RecoveryVolume2/Collision",ResidentKeepOutKind.Hazard,new Rect2(1996,3584,564,16)),
            ("recovery:CanyonBasin/Gameplay/FallRecovery/RecoveryVolume/Collision",ResidentKeepOutKind.Hazard,new Rect2(1280,4304,3840,16)),
            ("recovery:CanyonAscent/Gameplay/FallRecovery/RecoveryVolume/Collision",ResidentKeepOutKind.Hazard,new Rect2(4336,3584,464,16)),
            ("recovery:CanyonAscent/Gameplay/FallRecovery/RecoveryVolume2/Collision",ResidentKeepOutKind.Hazard,new Rect2(5088,3584,32,16)),
            ("recovery:LostTemple/Gameplay/FallRecovery/RecoveryVolume/Collision",ResidentKeepOutKind.Hazard,new Rect2(5120,1424,2560,16)),
            ("electric:CanyonBasin/Gameplay/ShieldLane/Collision",ResidentKeepOutKind.Hazard,new Rect2(2130,3870,260,120)),
            ("steam:CanyonBasin/Gameplay/Mechanisms/SteamJet/Collision",ResidentKeepOutKind.Hazard,new Rect2(3424,4035,72,150)),
            ("steam:CanyonBasin/Gameplay/Mechanisms/SteamDiverterTrench/LeftJet/Collision",ResidentKeepOutKind.Hazard,new Rect2(3858,4035,64,150)),
            ("steam:CanyonBasin/Gameplay/Mechanisms/SteamDiverterTrench/RightJet/Collision",ResidentKeepOutKind.Hazard,new Rect2(4078,4035,64,150)),
            ("steam:CanyonDescent/Gameplay/Mechanisms/SteamDiverterFrozen/LeftJet/Collision",ResidentKeepOutKind.Hazard,new Rect2(1632,2465,56,150)),
            ("steam:CanyonDescent/Gameplay/Mechanisms/SteamDiverterFrozen/RightJet/Collision",ResidentKeepOutKind.Hazard,new Rect2(1792,2465,56,150)),
            ("linear:CanyonBasin/Gameplay/Mechanisms/GearPiston/Collision",ResidentKeepOutKind.MovingOccupancy,new Rect2(2534,3844,132,132)),
            ("linear:CanyonBasin/Gameplay/Mechanisms/BasinCounterweightLift/Collision",ResidentKeepOutKind.MovingOccupancy,new Rect2(2700,3844,120,176)),
            ("linear:CanyonAscent/Gameplay/Mechanisms/CounterweightLift/Collision",ResidentKeepOutKind.MovingOccupancy,new Rect2(4800,3008,80,544)),
            ("pendulum:CanyonAscent/Gameplay/Mechanisms/PendulumHazard/Collision",ResidentKeepOutKind.MovingOccupancy,new Rect2(4726.01055f,2078.12733f,387.97890f,232.31768f)),
            ("conditional:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate/Door/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(1720,1424,40,216)),
            ("conditional:CanyonAscent/Gameplay/Mechanisms/UpperBasinShortcutWinch/Barrier/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(3840,1520,410,1930)),
            ("conditional:LostTemple/Gameplay/Mechanisms/CourtyardSeal/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(5500,720,36,500)),
            ("conditional:LostTemple/Gameplay/Mechanisms/BellChain/Door/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(5805,850,50,240)),
            ("conditional:LostTemple/Gameplay/Mechanisms/TemplePressureGate/Door/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(5930,760,30,200)),
            ("conditional:LostTemple/Gameplay/Doors/Entry/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(6142,936,36,180)),
            ("conditional:LostTemple/Gameplay/Doors/Exit/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(7282,936,36,180)),
            ("conditional:LostTemple/Gameplay/Doors/AdmissionBarrier/Collision",ResidentKeepOutKind.ConditionalSolid,new Rect2(6142,720,36,436)),
            ("conditional:LostTemple/Gameplay/Doors/AdmissionBarrier/TopCollision",ResidentKeepOutKind.ConditionalSolid,new Rect2(6142,720,1556,40)),
            ("conditional:LostTemple/Gameplay/Doors/AdmissionBarrier/RightCollision",ResidentKeepOutKind.ConditionalSolid,new Rect2(7662,720,36,436))
        })
            Expect(id,Has(actual,id,kind,bounds));
        Expect("traversal_18_launch_18_landing",actual.Regions.Count(x=>x.Kind==ResidentKeepOutKind.TraversalLaunch)==18 &&
            actual.Regions.Count(x=>x.Kind==ResidentKeepOutKind.TraversalLanding)==18);
        Expect("nine_fragment_approaches",actual.Regions.Count(x=>x.Kind==ResidentKeepOutKind.FragmentApproach)==9);
        Expect("two_supply_approaches",actual.Regions.Count(x=>x.Kind==ResidentKeepOutKind.SupplyApproach)==2);
        Expect("rest3_checkpoint3_boss1",actual.Regions.Count(x=>x.Kind==ResidentKeepOutKind.Protected)==7);
        Expect("explicit_start_protection",Has(actual,"checkpoint:canyon.v2.start",ResidentKeepOutKind.Protected,new Rect2(40,456,240,192)));
        Expect("pickup_full_approach",Has(actual,"fragment:CanyonEntrance/Gameplay/QuestFragments/entrance/Collision",ResidentKeepOutKind.FragmentApproach,new Rect2(222,542,116,116)));
        Expect("first_saved_launch_world",Has(actual,"launch:CanyonEntrance/Gameplay/TraversalContracts/canyonv2routebridge-to-rift",ResidentKeepOutKind.TraversalLaunch,new Rect2(1190,564,70,72)));
        Expect("null_world_fail_closed",ResidentEnvironmentInventory.Capture(null).Issues.Contains("invalid_world"));
        Node probe=GD.Load<PackedScene>(CanyonEncounterCatalog.MothScene).Instantiate();
        try
        {
            var actor=(CharacterBody2D)probe;
            foreach((string name,Vector2 p,string reason) in new[]
            {
                ("recovery_layer_zero",new Vector2(700,708),"reserved.Hazard:recovery:CanyonEntrance/Gameplay/FallRecovery/RecoveryVolume/Collision"),
                ("disabled_frozen_steam",new Vector2(1660,2520),"reserved.Hazard:steam:CanyonDescent/Gameplay/Mechanisms/SteamDiverterFrozen/LeftJet/Collision"),
                ("pendulum_middle_sweep",new Vector2(4920,2280),"reserved.MovingOccupancy:pendulum:CanyonAscent/Gameplay/Mechanisms/PendulumHazard/Collision"),
                ("admission_top_not_only_primary",new Vector2(6800,740),"reserved.ConditionalSolid:conditional:LostTemple/Gameplay/Doors/AdmissionBarrier/TopCollision"),
                ("start_checkpoint_body",new Vector2(140,540),"reserved.Protected:checkpoint:canyon.v2.start"),
                ("fragment_approach_not_contact_only",new Vector2(280,550),"reserved.FragmentApproach:fragment:CanyonEntrance/Gameplay/QuestFragments/entrance/Collision"),
                ("launch_access",new Vector2(1225,600),"reserved.TraversalLaunch:launch:CanyonEntrance/Gameplay/TraversalContracts/canyonv2routebridge-to-rift")
            })
            {
                ResidentBodyGeometryResult r=ResidentPlacementGeometry.CheckBody(actor,new Transform2D(0,p),fixture.Space,fixture.Scope,
                    fixture.Bounds,false,actual.Regions);
                Expect(name,!r.BodyGeometryClear&&r.Issues.Contains(reason));
            }
        }
        finally {probe.Free();}
        CanyonSteamJet jet=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
        Shape2D oldShape=jet.Collision.Shape;
        try
        {
            jet.Collision.Shape=null;
            Expect("missing_potential_shape_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Any(x=>x.StartsWith("invalid_shape:")));
        }
        finally {jet.Collision.Shape=oldShape;}
        CanyonPendulumHazard pendulum=fixture.Sectors[PrologueSectorId.CanyonAscent].GetNode<CanyonPendulumHazard>("Gameplay/Mechanisms/PendulumHazard");
        float oldAngle=pendulum.MaxAngleDegrees;
        try
        {
            pendulum.MaxAngleDegrees=float.NaN;
            Expect("invalid_future_motion_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Any(x=>x.StartsWith("invalid_motion:")));
        }
        finally {pendulum.MaxAngleDegrees=oldAngle;}
        Node2D basin=fixture.Sectors[PrologueSectorId.CanyonBasin];Transform2D oldTransform=basin.Transform;
        try
        {
            basin.Rotation=Mathf.Pi/2;basin.Scale=new Vector2(2,2);
            Expect("full_ancestor_future_motion",Has(ResidentEnvironmentInventory.Capture(fixture.Scope),
                "linear:CanyonBasin/Gameplay/Mechanisms/GearPiston/Collision",ResidentKeepOutKind.MovingOccupancy,new Rect2(528,6108,264,264)));
        }
        finally {basin.Transform=oldTransform;}
        // A separate owned transform control avoids changing a saved body's
        // SyncToPhysics deferred target. The original five sectors stay exact.
        var arcParent=new Node2D {Name="ArcTransformControl",Position=new Vector2(3840,720)};
        var arcBody=new CanyonPendulumHazard {Name="ScaledPendulum",Position=new Vector2(1080,1370),MaxAngleDegrees=58};
        CollisionShape2D savedArc=pendulum.GetNode<CollisionShape2D>("Collision");
        try
        {
            arcBody.AddChild(new CollisionShape2D {Name="Collision",Transform=savedArc.Transform,
                Shape=fixture.Own((Shape2D)savedArc.Shape.Duplicate())});
            arcParent.AddChild(arcBody);fixture.Scope.AddChild(arcParent);fixture.StopCallbacks();
            // This control tests geometry, not default synchronized movement.
            // Only the disposable extra body's sync is disabled, never saved ones.
            arcBody.SyncToPhysics=false;
            // Hand-derived from four local corners (+/-14,0)/(+/-14,220):
            // bodyScale(2,1), ancestor R90*Scale(3,2), pivot(1100,3960), +/-58deg.
            arcParent.Rotation=Mathf.Pi/2;arcParent.Scale=new Vector2(3,2);arcBody.Scale=new Vector2(2,1);
            Expect("full_ancestor_and_body_stretch_arc",Has(ResidentEnvironmentInventory.Capture(fixture.Scope),
                "pendulum:ArcTransformControl/ScaledPendulum/Collision",ResidentKeepOutKind.MovingOccupancy,
                new Rect2(656.450679f,3355.775038f,491.040014f,1208.449923f)));
        }
        finally
        {
            if(GodotObject.IsInstanceValid(arcBody)&&arcBody.GetParent()==null) arcBody.Free();
            if(GodotObject.IsInstanceValid(arcParent)) arcParent.Free();
        }
        await fixture.Sync();
        string after=fixture.CollisionSnapshot();
        if(before!=after) GD.Print("RESIDENT_ENVIRONMENT_RESTORE_DIFF "+string.Join(" | ",
            before.Split('|').Zip(after.Split('|')).Where(x=>x.First!=x.Second).Take(6).Select(x=>"before="+x.First+" after="+x.Second)));
        Expect("capture_and_controls_restore_saved_geometry",before==after);
        TestAssert.True(failures.Count==0,"resident environment checks failed: "+string.Join("; ",failures));
        GD.Print($"RESIDENT_ENVIRONMENT_NATIVE_OK cases={cases} regions=81 hazards=13 moving=4 conditional_shapes=10 launches=18 landings=18 fragments=9 supplies=2 protected=7 non_player_interaction=true placement_certified=false");
        void Expect(string name,bool accepted){cases++;if(!accepted)failures.Add(name);}
        static bool Has(ResidentEnvironmentSnapshot set,string id,ResidentKeepOutKind kind,Rect2 want)=>
            set.Regions.Any(x=>x.SourceId==id&&x.Kind==kind&&x.WorldBounds.Position.DistanceTo(want.Position)<.03f&&x.WorldBounds.Size.DistanceTo(want.Size)<.03f);
    }

    private static async Task RunPlayerAccess(NativeFixture fixture)
    {
        var failed=new List<string>();int cases=0;
        string before=fixture.CollisionSnapshot();
        ResidentEnvironmentSnapshot actual=ResidentEnvironmentInventory.Capture(fixture.Scope);
        Expect("player_access_total100",actual.Regions.Count==100);
        Expect("nineteen_actual_interaction_sources",actual.Regions.Count(x=>x.Kind==ResidentKeepOutKind.PlayerInteraction)==19);
        Expect("access_not_whole_placement_proof",!actual.IsCompletePlacementProof);
        var expected=new(string Id,Rect2 Bounds)[]
        {
            ("interaction-target:CanyonDescent/Gameplay/Mechanisms/HiddenPanel/ThreadTarget/TargetCollision/Collision",new(1412,1348,56,56)),
            ("interaction-target:CanyonDescent/Gameplay/GrappleSurfaces/SteamDiverterFrozenControl/TargetCollision/Collision",new(1718,2478,44,44)),
            ("interaction-target:CanyonBasin/Gameplay/GrappleSurfaces/SteamValveControl/TargetCollision/Collision",new(3138,4018,44,44)),
            ("interaction-target:CanyonBasin/Gameplay/GrappleSurfaces/SteamDiverterTrenchControl/TargetCollision/Collision",new(3968,4048,44,44)),
            ("interaction-target:CanyonBasin/Gameplay/GrappleSurfaces/BasinCounterweightControl/TargetCollision/Collision",new(2470,3794,44,44)),
            ("interaction-target:CanyonAscent/Gameplay/GrappleSurfaces/CounterweightLiftControl/TargetCollision/Collision",new(5078,3398,44,44)),
            ("interaction-target:CanyonAscent/Gameplay/GrappleSurfaces/UpperBasinShortcutControl/TargetCollision/Collision",new(4318,1398,44,44)),
            ("interaction-target:CanyonAscent/Gameplay/GrappleSurfaces/CourtyardSealLinkageControl/TargetCollision/Collision",new(4898,1238,44,44)),
            ("interaction-target:LostTemple/Gameplay/Mechanisms/BellChain/ThreadTarget/TargetCollision/Collision",new(5652,862,56,56)),
            ("interaction-target:LostTemple/Gameplay/Mechanisms/BellBody/ThreadTarget/TargetCollision/Collision",new(6032,868,56,56)),
            ("interaction-plate:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate",new(1620,1580,80,60)),
            ("interaction-interior:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate",new(1760,1424,120,216)),
            ("interaction-anti-crush:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate",new(1700,1424,80,216)),
            ("interaction-plate:LostTemple/Gameplay/Mechanisms/TemplePressureGate",new(5868,900,60,60)),
            ("interaction-interior:LostTemple/Gameplay/Mechanisms/TemplePressureGate",new(5960,760,100,200)),
            ("interaction-anti-crush:LostTemple/Gameplay/Mechanisms/TemplePressureGate",new(5910,760,70,200)),
            ("interaction-corridor:CanyonAscent/Gameplay/Mechanisms/UpperBasinShortcutWinch",new(3840,1500,410,2060)),
            ("interaction-manual:CanyonBasin/Gameplay/GrappleSurfaces/SteamValveControl",new(3080,3960,160,160)),
            // Ready spawns a real Boss even in this player-free fixture. Its
            // initially disabled chain target is not a missing/optional source.
            ("interaction-target:LostTemple/Gameplay/BossRoot/FallenBellkeeper/ChainTarget/TargetCollision/Collision",new(6758,1006,64,64))
        };
        foreach(var row in expected)Expect(row.Id,Has(actual,row.Id,row.Bounds));
        CharacterBody2D probe=null;
        try
        {
            probe=(CharacterBody2D)GD.Load<PackedScene>(CanyonEncounterCatalog.MothScene).Instantiate();
            foreach(var row in expected)
            {
                ResidentBodyGeometryResult body=ResidentPlacementGeometry.CheckBody(probe,new(0,row.Bounds.GetCenter()),
                    fixture.Space,fixture.Scope,fixture.Bounds,false,actual.Regions);
                Expect("actual_body_rejected:"+row.Id,!body.BodyGeometryClear&&body.Issues.Contains("reserved.PlayerInteraction:"+row.Id));
            }
        }
        finally{if(GodotObject.IsInstanceValid(probe))probe.Free();}

        ThreadTarget2D target=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<ThreadTarget2D>("Gameplay/Mechanisms/HiddenPanel/ThreadTarget");
        string targetId="CanyonDescent/Gameplay/Mechanisms/HiddenPanel/ThreadTarget";
        Area2D area=target.GetNode<Area2D>("TargetCollision");CollisionShape2D collision=area.GetNode<CollisionShape2D>("Collision");
        Node owner=area.Owner;
        try
        {
            area.Owner=null;target.RemoveChild(area);
            Expect("missing_target_area_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_interaction_shape:"+targetId));
        }
        finally{target.AddChild(area);area.Owner=owner;}
        Shape2D savedShape=collision.Shape;
        try
        {
            collision.Shape=null;
            Expect("missing_target_shape_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_interaction_shape:"+targetId));
        }
        finally{collision.Shape=savedShape;}
        var extra=new CollisionShape2D {Name="UnknownInteractionShape",Shape=fixture.Own(new RectangleShape2D {Size=new Vector2(4,4)})};
        try
        {
            area.AddChild(extra);
            Expect("unknown_target_shape_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_interaction_shape:"+targetId));
        }
        finally{extra.Free();}
        NodePath savedReceiver=target.PullReceiverPath;
        using(var missingReceiver=new NodePath("missing-receiver-control"))
        {
            try
            {
                target.PullReceiverPath=missingReceiver;
                Expect("missing_receiver_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_interaction_receiver:"+targetId));
            }
            finally{target.PullReceiverPath=savedReceiver;}
        }
        float savedRadius=target.AssistRadius;
        try
        {
            target.AssistRadius=float.NaN;
            Expect("invalid_target_config_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_interaction_target:"+targetId));
        }
        finally{target.AssistRadius=savedRadius;}
        ThreadTargetKind savedKind=target.Kind;
        try
        {
            target.Kind=(ThreadTargetKind)999;
            Expect("unknown_target_kind_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_interaction_target:"+targetId));
        }
        finally{target.Kind=savedKind;}
        bool savedDisabled=collision.Disabled;
        try
        {
            collision.Disabled=true;
            Expect("disabled_target_still_reserved",Has(ResidentEnvironmentInventory.Capture(fixture.Scope),expected[0].Id,expected[0].Bounds));
        }
        finally{collision.Disabled=savedDisabled;}
        CanyonPressurePlateGate gate=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/ArchivePressureGate");
        Rect2 savedPlate=gate.PlateWorldRect,savedInterior=gate.InteriorWorldRect,savedCrush=gate.AntiCrushWorldRect;
        try
        {
            gate.PlateWorldRect=new(float.NaN,1580,80,60);
            Expect("invalid_plate_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_bounds:interaction-plate:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate"));
            gate.PlateWorldRect=savedPlate;gate.InteriorWorldRect=new(1760,1424,0,216);
            Expect("invalid_interior_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_bounds:interaction-interior:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate"));
            gate.InteriorWorldRect=savedInterior;gate.AntiCrushWorldRect=new(1700,1424,80,0);
            Expect("invalid_anti_crush_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_bounds:interaction-anti-crush:CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate"));
        }
        finally{gate.PlateWorldRect=savedPlate;gate.InteriorWorldRect=savedInterior;gate.AntiCrushWorldRect=savedCrush;}
        CanyonShortcutWinch winch=fixture.Sectors[PrologueSectorId.CanyonAscent].GetNode<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
        Rect2 savedCorridor=winch.CorridorBounds;
        try
        {
            winch.CorridorBounds=new(0,780,0,2060);
            Expect("invalid_declared_corridor_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_bounds:interaction-corridor:CanyonAscent/Gameplay/Mechanisms/UpperBasinShortcutWinch"));
        }
        finally{winch.CorridorBounds=savedCorridor;}
        ThreadTarget2D manualTarget=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamValveControl");
        CanyonSteamValve valve=manualTarget.GetNode<CanyonSteamValve>(manualTarget.PullReceiverPath);NodePath savedJet=valve.PairedJetPath;
        using(var missingJet=new NodePath("missing-manual-jet-control"))
        {
            try
            {
                valve.PairedJetPath=missingJet;
                Expect("manual_missing_jet_fail_closed",ResidentEnvironmentInventory.Capture(fixture.Scope).Issues.Contains("invalid_manual_control:CanyonBasin/Gameplay/GrappleSurfaces/SteamValveControl"));
            }
            finally{valve.PairedJetPath=savedJet;}
        }
        CanyonSteamValve remoteLink=fixture.Sectors[PrologueSectorId.CanyonAscent].GetNode<CanyonSteamValve>("Gameplay/Mechanisms/CourtyardSealLinkage");
        Expect("remote_no_jet_linkage_remains_legal",string.IsNullOrEmpty(remoteLink.PairedJetPath.ToString())&&actual.Issues.Count==0&&Has(actual,expected[7].Id,expected[7].Bounds));
        Node2D basin=fixture.Sectors[PrologueSectorId.CanyonBasin];Transform2D savedBasin=basin.Transform;
        try
        {
            basin.Rotation=Mathf.Pi/2;basin.Scale=new Vector2(2,2);
            ResidentEnvironmentSnapshot transformed=ResidentEnvironmentInventory.Capture(fixture.Scope);
            Expect("target_full_ancestor_transform",Has(transformed,expected[2].Id,new Rect2(356,7316,88,88)));
            Expect("manual_radius_is_world_distance_not_scaled_shape",Has(transformed,expected[17].Id,new Rect2(320,7280,160,160)));
        }
        finally{basin.Transform=savedBasin;}
        await fixture.Sync();
        Expect("player_access_controls_restore_native_geometry",before==fixture.CollisionSnapshot());
        TestAssert.True(failed.Count==0,"resident player access checks failed: "+string.Join("; ",failed));
        GD.Print($"RESIDENT_PLAYER_ACCESS_NATIVE_OK cases={cases} sources=19 targets=11 pressure_rects=6 declared_corridors=1 manual_q=1 total_regions=100 placement_certified=false");
        void Expect(string name,bool pass){cases++;if(!pass)failed.Add(name);}
        static bool Has(ResidentEnvironmentSnapshot snapshot,string id,Rect2 bounds)=>snapshot.Regions.Any(x=>x.SourceId==id&&
            x.Kind==ResidentKeepOutKind.PlayerInteraction&&x.WorldBounds.Position.DistanceTo(bounds.Position)<.03f&&x.WorldBounds.Size.DistanceTo(bounds.Size)<.03f);
    }

    internal sealed class NativeFixture
    {
        private readonly SceneTree _tree;
        private World2D _world;
        private SubViewport _viewport;
        private readonly List<Shape2D> _ownedShapes=new();
        private readonly List<(Shape2D shape,Rect2 bounds)> _borrowedOriginals=new();
        private readonly List<Node> _detached=new();
        public Node2D Scope {get;private set;}
        public PhysicsDirectSpaceState2D Space=>_world.DirectSpaceState;
        public Rect2 Bounds=>new(0,0,12000,5000);
        public Dictionary<PrologueSectorId,Node2D> Sectors {get;}=new();
        public NativeFixture(SceneTree tree){_tree=tree;}
        public void Initialize()
        {
            TestAssert.Equal(0,PlayerCount(),"native geometry has no SceneTree-wide player");
            _world=new World2D();
            _viewport=new SubViewport {Name="ResidentGeometryNativeWorld",Size=new Vector2I(32,32),World2D=_world,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};
            Scope=new Node2D {Name="ExactSavedSectors"};_detached.Add(Scope);
            _viewport.AddChild(Scope);_detached.Remove(Scope);_tree.Root.AddChild(_viewport);
        }
        public void MountSavedSectors(IReadOnlyDictionary<PrologueSectorId,string> paths=null)
        {
            foreach((PrologueSectorId id,string file,Vector2 offset) in new[]
            {
                (PrologueSectorId.CanyonEntrance,"CanyonEntrance",new Vector2(0,0)),
                (PrologueSectorId.CanyonDescent,"CanyonDescent",new Vector2(1280,0)),
                (PrologueSectorId.CanyonBasin,"CanyonBasin",new Vector2(1280,3600)),
                (PrologueSectorId.CanyonAscent,"CanyonAscent",new Vector2(3840,720)),
                (PrologueSectorId.LostTemple,"LostTemple",new Vector2(5120,720))
            })
            {
                string scenePath=paths==null?$"res://scenes/prologue/sectors/{file}.tscn":paths[id];
                var sector=(Node2D)GD.Load<PackedScene>(scenePath).Instantiate();
                TestAssert.Equal(scenePath.Replace('\\','/'),sector.SceneFilePath.Replace('\\','/'),"mount the exact saved candidate/ordinary sector, never canonical fallback");
                _detached.Add(sector);
                TestAssert.Equal(Transform2D.Identity,sector.Transform,"saved sector root transform is identity before exact world offset");
                foreach(CollisionShape2D shape in Walk(sector).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))
                {
                    _borrowedOriginals.Add((shape.Shape,shape.Shape.GetRect()));
                    Shape2D copy=Own((Shape2D)shape.Shape.Duplicate());shape.Shape=copy;
                }
                sector.Position=offset;Scope.AddChild(sector);_detached.Remove(sector);Sectors.Add(id,sector);
                StopCallbacks();
            }
        }
        public void MountResident(RustShellSquire actor,PrologueSectorId sector)
        {
            _detached.Add(actor);
            Sectors[sector].GetNode("Gameplay/Actors").AddChild(actor);
            _detached.Remove(actor);
        }
        public void StopCallbacks()
        {
            foreach(Node node in Walk(Scope)){node.SetProcess(false);node.SetPhysicsProcess(false);}
        }
        public async Task Sync()
        {
            for(int i=0;i<3;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}
        }
        public void AssertNativePreconditions(int expectedTerrain=127)
        {
            TestAssert.Equal(0,PlayerCount(),"no global player can activate saved encounters or recovery");
            TestAssert.True(Sectors.Values.All(x=>x.IsInsideTree()&&x.GetWorld2D()==_world)&&_world!=_tree.Root.FindWorld2D(),"all saved sectors belong to owned independent world");
            TestAssert.Equal(expectedTerrain,Sectors.Values.Sum(x=>Walk(x.GetNode("Gameplay/Collision")).OfType<StaticBody2D>().Count()),"actual saved raw terrain count, not legal sites");
            CharacterBody2D[] bodies=Walk(Scope).OfType<CharacterBody2D>().ToArray();
            TestAssert.True(bodies.Length==1&&bodies[0] is FallenBellkeeper,"actual Ready-spawned boss owned, no ordinary waves born");
            TestAssert.True(_borrowedOriginals.All(x=>x.shape.GetRect()==x.bounds),"Ready mutations do not change cached source shape resources");
            using var ray=PhysicsRayQueryParameters2D.Create(new Vector2(1040,590),new Vector2(1040,680),CombatCollisionLayers.World);
            ray.CollideWithAreas=false;ray.CollideWithBodies=true;
            using Godot.Collections.Dictionary hit=Space.IntersectRay(ray);
            TestAssert.True(hit.Count>0&&hit["collider"].AsGodotObject() is StaticBody2D&&Mathf.Abs(hit["position"].AsVector2().Y-636)<.01f,"actual saved FarBank floor is natively registered");
            using Godot.Collections.Dictionary other=_tree.Root.FindWorld2D().DirectSpaceState.IntersectRay(ray);
            TestAssert.Equal(0,other.Count,"saved floor absent from unrelated main World2D");
            CanyonPendulumHazard pendulum=Sectors[PrologueSectorId.CanyonAscent].GetNode<CanyonPendulumHazard>("Gameplay/Mechanisms/PendulumHazard");
            CollisionShape2D collider=pendulum.GetNode<CollisionShape2D>("Collision");
            TestAssert.True(NativeHits(collider.Shape,collider.GlobalTransform,false).Contains(pendulum.GetInstanceId()),"enabled saved animated body remains native after callback stopping");
            GD.Print($"RESIDENT_GEOMETRY_FIXTURE_OK saved_sectors=5 raw_terrain={expectedTerrain} ready_boss=1 ordinary_spawned=0 independent_world=true cached_shapes_unchanged=true recovery_tested=false");
        }
        public string CollisionSnapshot()=>string.Join("|",Walk(Scope).OfType<CollisionShape2D>().Where(x=>x.Shape!=null).Select(x=>
            string.Create(CultureInfo.InvariantCulture,$"{x.GetInstanceId()}:{x.Disabled}:{x.GlobalTransform}:{x.Shape.GetRect()}")));
        public void PopulateControlledCounterexamples()
        {
            Floor("GapLeft",new Vector2(8948,656),new Vector2(96,40));Floor("GapRight",new Vector2(9052,656),new Vector2(96,40));
            Floor("AreaFloor",new Vector2(9300,656),new Vector2(200,40));
            var combined=new StaticBody2D {Name="SameBodyFloorAndWall",CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};_detached.Add(combined);
            combined.AddChild(new CollisionShape2D {Name="SupportShape",Position=new Vector2(9700,656),Shape=Own(new RectangleShape2D {Size=new Vector2(200,40)})});
            combined.AddChild(new CollisionShape2D {Name="BlockingWallShape",Position=new Vector2(9700,604),Shape=Own(new RectangleShape2D {Size=new Vector2(4,64)})});
            Scope.AddChild(combined);_detached.Remove(combined);
            Floor("FullTransformWall",new Vector2(1050,500),new Vector2(4,100));
            Floor("OverlapWall",new Vector2(10200,500),new Vector2(4,160));
            Area("RealAttackArea",new Vector2(9300,604));
            for(int i=0;i<65;i++)Area("CapacityArea"+i,new Vector2(10500,500));
        }
        public void AssertControlledPreconditions()
        {
            foreach(float x in new[]{8982.1f,9017.9f})
            {
                using var q=PhysicsRayQueryParameters2D.Create(new Vector2(x,630),new Vector2(x,642),CombatCollisionLayers.World);
                q.CollideWithAreas=false;using Godot.Collections.Dictionary h=Space.IntersectRay(q);
                TestAssert.True(h.Count>0,"both edge probes on real separated native floors are supported");
            }
        }
        public bool HasNativeHit(Shape2D shape,Transform2D transform,ulong id)=>NativeHits(shape,transform,true,uint.MaxValue).Contains(id);
        private List<ulong> NativeHits(Shape2D shape,Transform2D transform,bool areas,uint mask=CombatCollisionLayers.World)
        {
            using var q=new PhysicsShapeQueryParameters2D {Shape=shape,Transform=transform,CollisionMask=mask,CollideWithBodies=true,CollideWithAreas=areas};
            using Godot.Collections.Array hits=(Godot.Collections.Array)Space.IntersectShape(q,64);
            var ids=new List<ulong>();
            for(int i=0;i<hits.Count;i++){using Godot.Collections.Dictionary h=hits[i].AsGodotDictionary();ids.Add(h["collider_id"].AsUInt64());}
            return ids;
        }
        private void Floor(string name,Vector2 pos,Vector2 size)
        {
            var b=new StaticBody2D {Name=name,Position=pos,CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};_detached.Add(b);
            b.AddChild(new CollisionShape2D {Shape=Own(new RectangleShape2D {Size=size})});Scope.AddChild(b);_detached.Remove(b);
        }
        private void Area(string name,Vector2 pos)
        {
            var a=new Area2D {Name=name,Position=pos,CollisionLayer=CombatCollisionLayers.EnemyAttack,CollisionMask=0,Monitoring=false,Monitorable=true};_detached.Add(a);
            a.AddChild(new CollisionShape2D {Shape=Own(new RectangleShape2D {Size=new Vector2(100,64)})});Scope.AddChild(a);_detached.Remove(a);
        }
        public T Own<T>(T shape) where T:Shape2D {_ownedShapes.Add(shape);return shape;}
        private int PlayerCount()
        {
            using Godot.Collections.Array players=(Godot.Collections.Array)_tree.GetNodesInGroup("player");
            return players.Count;
        }
        public async Task DisposeAsync()
        {
            foreach(Node node in _detached)if(GodotObject.IsInstanceValid(node))node.Free();_detached.Clear();
            if(GodotObject.IsInstanceValid(_viewport)){if(_viewport.IsInsideTree())_viewport.QueueFree();else _viewport.Free();}
            await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            foreach(Shape2D shape in _ownedShapes)shape.Dispose();_ownedShapes.Clear();_borrowedOriginals.Clear();_world?.Dispose();
        }
        private static IEnumerable<Node> Walk(Node root)
        {
            yield return root;
            using Godot.Collections.Array children=(Godot.Collections.Array)root.GetChildren();
            for(int i=0;i<children.Count;i++)foreach(Node node in Walk(children[i].AsGodotObject() as Node))yield return node;
        }
    }
}
