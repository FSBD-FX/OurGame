using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Integration;

public static partial class CanyonResidentPlacementTests
{
    private static Dictionary<PrologueSectorId,string> SaveGearMap(string storage)
    {
        var paths=CanyonWorldLayout.SectorBounds.Keys.ToDictionary(id=>id,PrologueCanyonBuildParts.ScenePath);
        foreach(var id in new[]{PrologueSectorId.CanyonDescent,PrologueSectorId.CanyonBasin,PrologueSectorId.CanyonAscent})
        {
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusBoundary("sector-build-begin:"+id);
#endif
            string path=Path.Combine(storage,"gear-candidate-"+id+".tscn");
            var sector=PrologueCanyonBuildParts.Build(id);
            try
            {
                TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(sector,path,out int before,out int after,out string failure),"save actual combined candidate: "+failure);
                TestAssert.Equal(before,after,"saved candidate preserves every node");paths[id]=path;
            }
            finally
            {
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusBoundary("sector-free-begin:"+id);
#endif
                sector.Free();
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusBoundary("sector-free-return:"+id);
#endif
            }
        }
        return paths;
    }

    public static async Task RunGearMapCensus(SceneTree tree,string storage)
    {
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.BeginCensus();
#endif
        string leaves=ResidentSceneBindingTests.BuildCandidatePackage(storage);
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusBoundary("package-build-return");
#endif
        var paths=SaveGearMap(storage);var fixture=new NativeFixture(tree);using var source=new CensusLeafSource(leaves);
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusBoundary("map-save-return");
#endif
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();fixture.AssertNativePreconditions(143);
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.Equal(0,environment.Issues.Count,"combined candidate keeps all environment inventory");
            RunBodyCandidateCensus(fixture,source);
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusBoundary("body-return-activity-begin");
#endif
#if !REFCOUNTED_FATAL_IDENTITY
            CensusJointSeal joint=RunActivityCandidateCensus(fixture,source,
                keep=>WriteJointContext(storage,paths,leaves,keep));
#else
            _=RunActivityCandidateCensus(fixture,source);
#endif
            source.WriteDiagnostics(Path.Combine(storage,"candidate-leaf-geometry-diagnostics.jsonl"));
#if !REFCOUNTED_FATAL_IDENTITY
            await RunJointFreezeContracts(tree,fixture,source,joint,storage);
#endif
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CensusSummary();
            GD.Print("RESIDENT_GEAR_MAP_CENSUS_DIAGNOSTIC_OK identities=50 subtypes=24 saved_sectors=5 native_terrain=143 canonical_changed=false placements_authored=0 motion_certified=false behavior_accepted=false");
#else
            GD.Print("RESIDENT_GEAR_MAP_CENSUS_OK identities=50 subtypes=24 saved_sectors=5 native_terrain=143 canonical_changed=false placements_authored=0 motion_certified=false");
#endif
        }
        finally{await fixture.DisposeAsync();}
    }

    public static async Task RunGearCandidate(SceneTree tree,string storage)
    {
        var baseline=await CaptureGearOriginalBaseline(tree);
        string leaves=Path.Combine(storage,"gear-leaves");Directory.CreateDirectory(leaves);
        var slots=new[]{"canyon.v2.resident.gear-pistons.06","canyon.v2.resident.gear-pistons.07"}
            .Select(id=>CanyonResidentCatalog.All.Single(x=>x.StableId==id)).ToArray();
        foreach(var slot in slots)
        {
            var type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId);
            using var profile=new ResidentSubtypeProfile {SubtypeId=type.Id,Family=type.Family,Archetype=type.Archetype,Carrier=type.Carrier};
            var author=GD.Load<PackedScene>(type.ProbeScenePath).Instantiate<CharacterBody2D>();
            try
            {
                author.SceneFilePath="";author.AddChild(new ResidentSceneBinding {Name="ResidentBinding",Profile=profile});
                TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(author,Path.Combine(leaves,type.Id+".tscn")),"save actual distinct Heavy placeholder leaf");
            }
            finally{author.Free();}
        }
        var paths=SaveGearMap(storage);var fixture=new NativeFixture(tree);using var source=new CensusLeafSource(leaves);
        var originals=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);var residents=new List<BellBackLaborer>();
        int[] healed={0,0};bool[] healthStable={true,true};
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();
            CharacterBody2D Original(string path)
            {if(!originals.TryGetValue(path,out var actor)){actor=GD.Load<PackedScene>(path).Instantiate<CharacterBody2D>();originals.Add(path,actor);}return actor;}
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);TestAssert.Equal(0,environment.Issues.Count,"all-phase environment inventory complete");
            TestAssert.Equal(baseline.Environment,GearEnvironmentKey(environment),"two supports do not rewrite existing all-phase hazards/route reservations");
            var old=OriginalSpawnReservations(Original);TestAssert.Equal(36,old.Count,"all global original primary/alternate records retained");
            Rect2 launchBounds=new(1932,3746,168,40);
            TestAssert.True(old.All(x=>!x.WorldBounds.Intersects(launchBounds)),"narrow western launch must not overlap any of36 actual original primary/alternate bodies");
            GD.Print("RESIDENT_GEAR_LAUNCH_OLD_BODIES_OK records=36 actual_saved_body_shapes=true launch_overlap=false protected_rest_overlap_retained=true checkpoint_save_proved=false");
            Rect2 sideBounds=new(2280,3700,24,116);
            TestAssert.True(old.All(x=>!x.WorldBounds.Intersects(sideBounds)),"new west climb wall does not overlap any original36 primary/alternate bodies");
            var sideBody=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/GearWestClimbWall");
            var sideShape=sideBody.GetNode<CollisionShape2D>("Collision");var sideSkin=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<Knight.Presentation.CanyonPlatformVisual>("VisualRoot/GameplayReadable/GearWestClimbWall");
            TestAssert.True(sideShape.GlobalTransform*sideShape.Shape.GetRect()==sideBounds&&sideSkin.SkinSize==sideBounds.Size&&sideSkin.GlobalPosition==sideBody.GlobalPosition,"new side entry has matching independent native collision and replaceable skin");
            var keep=environment.Regions.Concat(old).ToArray();
            Rect2 route=CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.GearPistons).WorldBounds;
            Vector2[] homes={new(2380,3750),new(2932,3678)};
            Rect2[] bodies={new(2320,3708,120,84),new(2872,3636,120,84)};
            var failures=new List<string>();
            for(int i=0;i<2;i++)
            {
                var type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slots[i].SubtypeId);var raw=source.Raw(type);
                Vector2 left=homes[i]+new Vector2(-32,0),right=homes[i]+new Vector2(32,0);
                var forward=ResidentPlacementGeometry.CheckCorridor(raw,new(0,left),new(0,right),fixture.Space,fixture.Scope,route,true,keep);
                var reverse=ResidentPlacementGeometry.CheckCorridor(raw,new(0,right),new(0,left),fixture.Space,fixture.Scope,route,true,keep);
                GD.Print($"RESIDENT_GEAR_CORRIDOR id={slots[i].StableId} forward={forward.CorridorClear} reverse={reverse.CorridorClear} issues={string.Join(",",forward.Issues.Concat(reverse.Issues))}");
                if(!forward.CorridorClear||!reverse.CorridorClear)failures.Add(slots[i].StableId+":"+string.Join(",",forward.Issues.Concat(reverse.Issues)));
                else TestAssert.Equal(bodies[i],forward.WorldCorridorBounds,"literal full120x84 activity envelope");
            }
            TestAssert.True(failures.Count==0,"new supports must really carry both full Heavy patrols: "+string.Join(";",failures));
            fixture.AssertNativePreconditions(143);
            TestAssert.True(!bodies[0].Grow(24).Intersects(bodies[1].Grow(24)),"both full activity occupancy margins are disjoint");
            string[] names={"GearResidentLeftShelf","GearResidentRightShelf"};
            Rect2[] floors={new(2304,3792,152,24),new(2856,3720,136,24)};
            for(int i=0;i<2;i++)
            {
                var sector=fixture.Sectors[PrologueSectorId.CanyonBasin];var body=sector.GetNode<StaticBody2D>("Gameplay/Collision/"+names[i]);
                var shape=body.GetNode<CollisionShape2D>("Collision");TestAssert.Equal(floors[i],shape.GlobalTransform*shape.Shape.GetRect(),"literal actual saved new floor");
                var skin=sector.GetNode<Knight.Presentation.CanyonPlatformVisual>("VisualRoot/GameplayReadable/"+names[i]);
                TestAssert.True(skin.SkinSize==floors[i].Size&&skin.GlobalPosition==body.GlobalPosition,"saved floor skin matches physical size/position");
            }
            var definitions=paths.Select(pair=>new PrologueSectorDefinition(pair.Key,GD.Load<PackedScene>(pair.Value),PrologueCanyonBuildParts.WorldOffsets[pair.Key],PrologueCanyonBuildParts.Bounds(pair.Key),null,null)).ToArray();
            using(var catalog=MinimapTerrainCatalogBuilder.Build(definitions))
            {
                TestAssert.Equal(143,catalog.SolidCount,"same-five minimap includes both extra structures");
                TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds==launchBounds),"same-five minimap includes exact narrowed launch");
                TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds==sideBounds),"same-five minimap includes exact independent west wall");
                foreach(var floor in floors)TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds==floor),"new floor included in real minimap");
                TestAssert.Equal(Error.Ok,ResourceSaver.Save(catalog,Path.Combine(storage,"gear-candidate-minimap.tres")),"save actual combined minimap");
            }
            var originalGear=CanyonEncounterCatalog.Normal.Where(x=>x.RouteNode==CanyonRouteNodeId.GearPistons).SelectMany(x=>x.Enemies).ToArray();
            TestAssert.Equal(2,originalGear.Length,"Gear has two original records, not five actors or global36");
            foreach(var placement in originalGear)
            {
                var result=ResidentPlacementGeometry.CheckBody(Original(placement.ScenePath),new(0,placement.WorldPosition),fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
                var before=baseline.Bodies[placement.ScenePath];
                TestAssert.True(before.Bounds==result.WorldBodyBounds&&before.Issues.SequenceEqual(result.Issues.OrderBy(x=>x,StringComparer.Ordinal))&&
                    floors.All(floor=>!floor.Intersects(result.WorldBodyBounds)),"new saved floor rectangles must not intersect old native bodies or change their baseline findings: "+string.Join(",",result.Issues));
                GD.Print($"RESIDENT_GEAR_ORIGINAL_COMPAT scene={Path.GetFileNameWithoutExtension(placement.ScenePath)} bounds={result.WorldBodyBounds} baseline_issues={string.Join(",",before.Issues)} candidate_issues={string.Join(",",result.Issues)} baseline_findings_preserved=true new_shelf_body_overlap=false full_feet_policy_applied=false");
            }
            var heavy=Original(CanyonEncounterCatalog.LaborerScene);
            var oldCorridor=ResidentPlacementGeometry.CheckCorridor(heavy,new(0,new Vector2(2880,3794)),new(0,new Vector2(3004,3794)),fixture.Space,fixture.Scope,fixture.Bounds,true,Array.Empty<ResidentKeepOut>());
            TestAssert.True(oldCorridor.CorridorClear,"old Heavy84px body retains supported corridor under new right shelf: "+string.Join(",",oldCorridor.Issues));
            var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            try
            {
                Vector2 top=new(2676,3800),bottom=new(2676,4070);
                var clear=ResidentPlacementGeometry.CheckCorridor(player,new(0,top),new(0,bottom),fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
                var reverse=ResidentPlacementGeometry.CheckCorridor(player,new(0,bottom),new(0,top),fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
                TestAssert.True(clear.CorridorClear&&reverse.CorridorClear,"new geometry preserves original finite static48px fall lane");
                var timed=ResidentPlacementGeometry.CheckCorridor(player,new(0,top),new(0,bottom),fixture.Space,fixture.Scope,fixture.Bounds,false,keep);
                TestAssert.True(!timed.CorridorClear&&timed.Issues.Any(x=>x.Contains("MovingOccupancy")&&x.Contains("GearPiston")),"original full potential piston occupancy remains a timed obstruction, not removed to fake safe passage");
                GD.Print("RESIDENT_GEAR_PLAYER_GAP_OK static_width=48 body_clearance_each=4 finite_segment=true directions=2 potential_piston_overlap_retained=true all_phase_safe=false");
            }
            finally{player.Free();}
            for(int i=0;i<2;i++)
            {
                Vector2 home=homes[i];var actor=(BellBackLaborer)source.Configure(slots[i],new[]{home,home+new Vector2(-32,0),home+new Vector2(32,0),home},route);residents.Add(actor);
                int identity=i;actor.GetNode<HealthComponent>("Health").Healed+=(_,_)=>healed[identity]++;
                foreach(var shape in GearDescendants(actor).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))shape.Shape=fixture.Own((Shape2D)shape.Shape.Duplicate());
                var parent=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<Node2D>("Gameplay/Actors");actor.Transform=parent.GlobalTransform.AffineInverse()*new Transform2D(0,home);parent.AddChild(actor);
            }
            await fixture.Sync();
            float[] low=homes.Select(x=>x.X).ToArray(),high=(float[])low.Clone(),step=new float[2];
            Vector2[] previous=residents.Select(x=>x.GlobalPosition).ToArray();
            bool[] supported={true,true},returned={false,false};
            for(int frame=0;frame<360;frame++)
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                for(int i=0;i<2;i++)
                {
                    var actor=residents[i];Vector2 position=actor.GlobalPosition;float x=position.X;low[i]=MathF.Min(low[i],x);high[i]=MathF.Max(high[i],x);step[i]=MathF.Max(step[i],position.DistanceTo(previous[i]));previous[i]=position;
                    var liveBody=actor.GetNode<CollisionShape2D>("BodyCollider");
                    TestAssert.True(liveBody.Shape is CapsuleShape2D capsule&&capsule.Radius==28&&capsule.Height==84&&
                        MathF.Abs(position.Y-homes[i].Y)<.5f&&bodies[i].Grow(.5f).Encloses(liveBody.GlobalTransform*liveBody.Shape.GetRect()),
                        "live84px Heavy remains on intended shelf within full checked activity bounds, without Ready-time shrink or vertical relocation");
                    supported[i]&=actor.IsOnFloor();returned[i]|=low[i]<=homes[i].X-26&&high[i]>=homes[i].X+26&&MathF.Abs(x-homes[i].X)<3;
                    healthStable[i]&=actor.GetNode<HealthComponent>("Health").CurrentHealth==6;
                }
            }
            for(int i=0;i<2;i++)
            {
                GD.Print($"RESIDENT_GEAR_PATROL id={slots[i].StableId} min={low[i]} max={high[i]} max_step={step[i]} supported={supported[i]} returned={returned[i]} hp_stable={healthStable[i]} healed={healed[i]} hp={residents[i].GetNode<HealthComponent>("Health").CurrentHealth}");
                TestAssert.True(low[i]<=homes[i].X-26&&high[i]>=homes[i].X+26&&step[i]<1.5f&&supported[i]&&returned[i]&&healthStable[i]&&healed[i]==0&&residents[i].GetNode<HealthComponent>("Health").CurrentHealth==6,"actual distinct saved Heavy patrols both directions and home on native floor without falling/healing");
            }
            foreach(var actor in residents)actor.Free();residents.Clear();await fixture.Sync();
            await RunGearPlayerVisit(tree,fixture);
            GD.Print("RESIDENT_GEAR_CANDIDATE_OK identities=2 saved_leaves=2 native_terrain=143 patrol_frames=360 old_gear_records=2 global_old_records=36 native_player_motion=true all_phase_passage=false enclosure_complete=false canonical_changed=false formal_spawned=0 publication=false");
        }
        finally
        {
            foreach(var actor in residents)if(GodotObject.IsInstanceValid(actor))actor.Free();
            foreach(var actor in originals.Values)actor.Free();await fixture.DisposeAsync();
        }
    }

    private static string GearEnvironmentKey(ResidentEnvironmentSnapshot environment)=>
        string.Join("\n",environment.Regions.OrderBy(x=>x.SourceId,StringComparer.Ordinal).ThenBy(x=>x.Kind)
            .Select(x=>$"{x.SourceId}|{x.Kind}|{x.WorldBounds}"));

    private static async Task<(string Environment,Dictionary<string,(Rect2 Bounds,string[] Issues)> Bodies)> CaptureGearOriginalBaseline(SceneTree tree,Action<NativeFixture> observe=null)
    {
        var fixture=new NativeFixture(tree);var bodies=new Dictionary<string,(Rect2 Bounds,string[] Issues)>(StringComparer.Ordinal);
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(ResidentTerrainSynchronizationTests.BaselinePaths);await fixture.Sync();fixture.StopCallbacks();fixture.AssertNativePreconditions(127);
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);TestAssert.Equal(0,environment.Issues.Count,"baseline actual canonical environment valid");
            foreach(var placement in CanyonEncounterCatalog.Normal.Where(x=>x.RouteNode==CanyonRouteNodeId.GearPistons).SelectMany(x=>x.Enemies))
            {
                var actor=GD.Load<PackedScene>(placement.ScenePath).Instantiate<CharacterBody2D>();
                try
                {
                    var result=ResidentPlacementGeometry.CheckBody(actor,new(0,placement.WorldPosition),fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>());
                    bodies.Add(placement.ScenePath,(result.WorldBodyBounds,result.Issues.OrderBy(x=>x,StringComparer.Ordinal).ToArray()));
                    GD.Print($"RESIDENT_GEAR_ORIGINAL_BASELINE scene={Path.GetFileNameWithoutExtension(placement.ScenePath)} issues={string.Join(",",result.Issues)} frozen_v059=true current_canonical=false");
                }
                finally{actor.Free();}
            }
            TestAssert.Equal(2,bodies.Count,"baseline two real original Gear records");
            observe?.Invoke(fixture);
            return(GearEnvironmentKey(environment),bodies);
        }
        finally{await fixture.DisposeAsync();}
    }

    private static async Task RunGearPlayerVisit(SceneTree tree,NativeFixture fixture)
    {
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        try
        {
            player.Position=new(3096,3720);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            var health=player.GetNode<HealthComponent>("Health");ulong identity=player.GetInstanceId();int hp=health.CurrentHealth,healed=0,samples=0;
            var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");bool clawUsed=false;ulong sequence=input.CurrentFrameSequence;
            claw.ClawFrameChanged+=(frame,_)=>{if(frame.State!=MechanicalClawState.Idle||frame.CastId!=0)clawUsed=true;};
            health.Healed+=(_,_)=>healed++;
            var sector=fixture.Sectors[PrologueSectorId.CanyonBasin];
            var start=sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
            var shelf=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearResidentRightShelf");
            bool On(StaticBody2D floor)
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++)
                {using var hit=player.GetSlideCollision(i);if(hit.GetCollider()==floor&&hit.GetNormal().Dot(Vector2.Up)>.99f)return true;}
                return false;
            }
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);samples++;
                TestAssert.True(input.CurrentFrameSequence>sequence&&!input.CurrentFrame.DashPressed&&!input.CurrentFrame.ClawPressed&&
                    !clawUsed&&claw.CurrentFrame.State==MechanicalClawState.Idle&&claw.CurrentFrame.CastId==0&&
                    float.IsFinite(player.GlobalPosition.X)&&float.IsFinite(player.GlobalPosition.Y)&&float.IsFinite(player.Velocity.X)&&float.IsFinite(player.Velocity.Y),
                    "native sampled route stays finite and never uses a dash/claw cast or hidden boost");sequence=input.CurrentFrameSequence;
                TestAssert.True(player.GetInstanceId()==identity&&health.CurrentHealth==hp&&healed==0&&
                    player.GlobalPosition.X is >2820 and <3330&&player.GlobalPosition.Y is >3520 and <3860,
                    "same real Player stays alive in local route without reset/recovery or injected pose: "+player.GlobalPosition);
            }
            for(int i=0;i<10;i++)await Step();
            TestAssert.True(On(start),"actual saved Player begins supported by original named gallery");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            for(int i=0;i<120&&!On(shelf);i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
            for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(shelf)&&player.GlobalPosition.X is >2876 and <2972&&MathF.Abs(player.GlobalPosition.Y-3684)<1&&motor.AcceptedJumpCount==1,
                "single actual keyboard jump lands and naturally brakes fully on new right shelf: "+player.GlobalPosition);
            GD.Print($"RESIDENT_GEAR_PLAYER_LANDED position={player.GlobalPosition} velocity={player.Velocity} samples={samples}");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            for(int i=0;i<120&&!(On(start)&&player.GlobalPosition.X>=3060);i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(start)&&player.GlobalPosition.X is >3060 and <3260&&MathF.Abs(player.GlobalPosition.Y-3720)<1&&
                motor.AcceptedJumpCount==1&&motor.AcceptedDashCount==0&&!clawUsed&&health.CurrentHealth==hp&&healed==0,
                "same Player walks back to original support without second jump/dash/boost/heal: "+player.GlobalPosition);
            GD.Print($"RESIDENT_GEAR_PLAYER_VISIT_OK same_player=true actual_keyboard=true actual_physics=true position_writes_after_mount=0 start=UpperGearGalleryOne landed=GearResidentRightShelf returned_original_support=true jumps=1 dashes=0 claw_boosts=0 healed=0 health={hp}->{health.CurrentHealth} samples={samples} mechanisms_active=false residents_present=false left_shelf_access=false normal_world=false");
        }
        finally
        {
            foreach(var key in new[]{Key.A,Key.D,Key.Space})KeyboardActionRuntimeTests.KeyEvent(key,false);
            if(GodotObject.IsInstanceValid(player))player.Free();
        }
    }

    public static async Task RunGearLeftAccess(SceneTree tree,string storage)
    {
        var paths=SaveGearMap(storage);var fixture=new NativeFixture(tree);CharacterBody2D player=null;
        BasinBoardingTickObserver observer=null;
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();fixture.AssertNativePreconditions(143);
            var sideEntry=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNodeOrNull<StaticBody2D>("Gameplay/Collision/GearWestClimbWall");
            TestAssert.True(sideEntry!=null&&sideEntry.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile")?.UseCollisionHitPoint==true,
                "approved left shelf needs an independent native side-wall grapple entrance, not only the running top-cast launch");
            var climbShape=sideEntry.GetNode<CollisionShape2D>("Collision");
            TestAssert.Equal(new Rect2(2280,3700,24,116),climbShape.GlobalTransform*climbShape.Shape.GetRect(),"real west-face entry joins the shelf without a synthetic anchor");
            string snapshot=fixture.CollisionSnapshot();
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.Equal(0,environment.Issues.Count,"left access preserves complete saved environment inventory");
            var danger=environment.Regions.Where(x=>x.Kind is ResidentKeepOutKind.Hazard or ResidentKeepOutKind.MovingOccupancy).ToArray();
            foreach(var expected in new[]{new Rect2(2130,3870,260,120),new(2534,3844,132,132),new(2700,3844,120,176)})
                TestAssert.True(danger.Any(x=>x.WorldBounds==expected),"full original electric/piston/lift potential region retained");
            await RunGearOriginalFrozenCampDrop(tree,fixture,danger);
            player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position=new(1680,3960);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");var health=player.GetNode<HealthComponent>("Health");
            var collider=player.GetNode<CollisionShape2D>("BodyCollider");ulong identity=player.GetInstanceId(),sequence=input.CurrentFrameSequence;
            var launchProof=ResidentPlacementGeometry.CheckBody(player,new(0,new Vector2(2000,3710)),fixture.Space,fixture.Scope,fixture.Bounds,true,danger);
            GD.Print($"RESIDENT_GEAR_LEFT_LAUNCH_BODY clear={launchProof.BodyGeometryClear} bounds={launchProof.WorldBodyBounds} issues={string.Join(",",launchProof.Issues)}");
            TestAssert.True(launchProof.BodyGeometryClear,"full40x72 Player requires real western launch support at3746 before aiming; no floating air pose: "+string.Join(",",launchProof.Issues));
            int hp=health.CurrentHealth,healed=0,samples=0,physicsSamples=0;health.Healed+=(_,_)=>healed++;
            var basin=fixture.Sectors[PrologueSectorId.CanyonBasin];var descent=fixture.Sectors[PrologueSectorId.CanyonDescent];
            var camp=basin.GetNode<StaticBody2D>("Gameplay/Collision/BasinCampFloor");
            var bottom=descent.GetNode<StaticBody2D>("Gameplay/Collision/AbyssSafeBottom");
            var shelf=basin.GetNode<StaticBody2D>("Gameplay/Collision/GearResidentLeftShelf");
            var launch=basin.GetNode<StaticBody2D>("Gameplay/Collision/GearWestLaunchShelf");
            var launchCollider=launch.GetNode<CollisionShape2D>("Collision");
            TestAssert.Equal(new Rect2(1932,3746,168,40),launchCollider.GlobalTransform*launchCollider.Shape.GetRect(),"literal actual western catch/launch plate preserving old Frozen drop");
            var piston=basin.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
            var wall=descent.GetNode<GrappleSurface2D>("Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile");
            var side=sideEntry.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            var casts=new HashSet<ulong>();var arrivals=new HashSet<(ulong Cast,ulong Target)>();var cancels=new HashSet<ulong>();var boostCasts=new HashSet<ulong>();ulong sideStart=0,lastCornerPhysics=0;bool climbed=false,edgeWitness=false,centerCrossSeen=false,wholeCrossSeen=false;int cornerPhysics=0;
            MechanicalClawState lastState=MechanicalClawState.Idle;MechanicalClawFailure lastFailure=MechanicalClawFailure.None;
            claw.ClawFrameChanged+=(frame,target)=>
            {
                if(frame.State!=lastState||frame.Failure!=lastFailure||frame.ReleaseKind!=MechanicalClawReleaseKind.None)
                    GD.Print($"RESIDENT_GEAR_LEFT_CLAW_STATE physics={Engine.GetPhysicsFrames()} state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} cast={frame.CastId} next={frame.NextCastId} target={target?.GetPath()} position={player.GlobalPosition} velocity={player.Velocity}");
                lastState=frame.State;lastFailure=frame.Failure;
                if(frame.State==MechanicalClawState.Windup&&frame.CastId!=0)casts.Add(frame.CastId);
                if(frame.State==MechanicalClawState.Released&&frame.ReleaseKind==MechanicalClawReleaseKind.Boost&&frame.CastId!=0)boostCasts.Add(frame.CastId);
                if(frame.ReleaseKind==MechanicalClawReleaseKind.Cancel&&frame.CastId!=0)cancels.Add(frame.CastId);
                if(frame.Arrived&&frame.State==MechanicalClawState.Swing&&frame.Failure==MechanicalClawFailure.None&&
                    frame.CastId!=0&&(target==wall||target==side)&&frame.LockedTargetRuntimeId==target.GetInstanceId()&&arrivals.Add((frame.CastId,frame.LockedTargetRuntimeId)))
                    GD.Print($"RESIDENT_GEAR_LEFT_ARRIVAL cast={frame.CastId} target={target.GetPath()} position={player.GlobalPosition} velocity={player.Velocity} input_sequence={input.CurrentFrameSequence}");
            };
            Rect2 PlayerBounds()=>collider.GlobalTransform*collider.Shape.GetRect();
            bool On(StaticBody2D floor)
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++)
                {using var hit=player.GetSlideCollision(i);if(hit.GetCollider()==floor&&hit.GetNormal().Dot(Vector2.Up)>.99f)return true;}
                return false;
            }
            bool FullOn(StaticBody2D floor)
            {
                if(!On(floor))return false;var c=floor.GetNode<CollisionShape2D>("Collision");Rect2 floorBounds=c.GlobalTransform*c.Shape.GetRect(),body=PlayerBounds();
                if(body.Position.X<floorBounds.Position.X||body.End.X>floorBounds.End.X||MathF.Abs(body.End.Y-floorBounds.Position.Y)>1)return false;
                foreach(float x in new[]{body.Position.X+1,body.GetCenter().X,body.End.X-1})
                {
                    using var q=PhysicsRayQueryParameters2D.Create(new(x,body.End.Y-.25f),new(x,body.End.Y+1),CombatCollisionLayers.World);q.CollideWithAreas=false;
                    using var hit=fixture.Space.IntersectRay(q);if(hit.Count==0||hit["collider"].AsGodotObject()!=floor||hit["normal"].AsVector2().Y>-.99f)return false;
                }
                return true;
            }
            string routeStage="initial";
            void Safety()
            {
                TestAssert.True(player.GetInstanceId()==identity&&health.CurrentHealth==hp&&healed==0&&
                    motor.AcceptedJumpCount==0&&motor.AcceptedDashCount==0&&boostCasts.Count<=1&&
                    player.GlobalPosition.X is >1500 and <2800&&player.GlobalPosition.Y is >3300 and <4020&&
                    float.IsFinite(player.Velocity.X)&&float.IsFinite(player.Velocity.Y),"left route keeps same living full Player, no ordinary jump/dash/recovery/pose injection, at most one real attached boost: "+player.GlobalPosition);
                if(sideStart!=0)TestAssert.True(Engine.GetPhysicsFrames()-sideStart<=300,"side cast, climb, full landing and brake share native300 cap");
                TestAssert.True(collider.Shape is CapsuleShape2D shape&&shape.Radius==20&&shape.Height==72,"left route retains full actual40x72 Player");
                Rect2 body=PlayerBounds();var overlap=danger.FirstOrDefault(x=>x.WorldBounds.Intersects(body));
                TestAssert.True(overlap==null,"whole Player avoids full authored hazard/potential occupancy, not a frozen safe phase: "+overlap?.SourceId+" body="+body);
                if(sideStart!=0&&boostCasts.Count==1&&Engine.GetPhysicsFrames()>lastCornerPhysics&&body.Intersects(new Rect2(2280,3700,24,116)))
                {
                    TestAssert.True(!collider.Shape.Collide(collider.GlobalTransform,climbShape.Shape,climbShape.GlobalTransform),"every actual corner-overlap physics sample keeps the full capsule out of the real wall");lastCornerPhysics=Engine.GetPhysicsFrames();cornerPhysics++;
                }
                void CrossingWitness(string kind)
                {
                    var query=ResidentPlacementGeometry.CheckBody(player,player.GlobalTransform,fixture.Space,fixture.Scope,fixture.Bounds,false,danger);
                    var contacts=new List<string>();for(int i=0;i<player.GetSlideCollisionCount();i++){using var hit=player.GetSlideCollision(i);contacts.Add((hit.GetCollider() as Node)?.GetPath()+":"+hit.GetNormal());}
                    GD.Print($"RESIDENT_GEAR_LEFT_CORNER_WITNESS kind={kind} physics={Engine.GetPhysicsFrames()} position={player.GlobalPosition} velocity={player.Velocity} body={body} native_clear={query.BodyGeometryClear} issues={string.Join(",",query.Issues)} aabb_above_top={body.End.Y<=3700} contacts={string.Join(";",contacts)}");
                    TestAssert.True(query.BodyGeometryClear&&player.GlobalPosition.Y<3700,"actual complete capsule crosses above the wall corner, never below the wall");
                }
                if(sideStart!=0&&!centerCrossSeen&&player.GlobalPosition.X>=2304){centerCrossSeen=true;CrossingWitness("center_east");}
                if(sideStart!=0&&!wholeCrossSeen&&body.Position.X>=2304)
                {wholeCrossSeen=true;CrossingWitness("whole_east");TestAssert.True(centerCrossSeen&&cornerPhysics>0,"real corner-overlap physics and prior center crossing were observed");climbed=true;GD.Print($"RESIDENT_GEAR_LEFT_CLIMB_CLEAR physics={Engine.GetPhysicsFrames()} body={body} corner_physics={cornerPhysics} native_capsule_clear=true wall_top=3700 wall_right=2304 same_player=true");}
                if(sideStart!=0&&!edgeWitness&&claw.CurrentTarget==side&&claw.CurrentFrame.State==MechanicalClawState.Reel)
                {
                    var actual=ResidentPlacementGeometry.CheckBody(player,player.GlobalTransform,fixture.Space,fixture.Scope,fixture.Bounds,false,danger);
                    GD.Print($"RESIDENT_GEAR_LEFT_EDGE_CAPSULE clear={actual.BodyGeometryClear} bounds={actual.WorldBodyBounds} issues={string.Join(",",actual.Issues)} actual_reel=true");
                    TestAssert.True(actual.BodyGeometryClear,"first native side Reel must preserve entire actual capsule, not an AABB-only escape");edgeWitness=true;
                }
            }
            Exception nativeSafetyFailure=null;
            observer=new BasinBoardingTickObserver {ProcessPhysicsPriority=100};
            observer.Sample=_=>{physicsSamples++;try{Safety();}catch(Exception error){nativeSafetyFailure??=error;}};
            fixture.Scope.AddChild(observer);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);samples++;
                TestAssert.True(nativeSafetyFailure==null,"post-Motor every-physics safety: "+nativeSafetyFailure?.Message);
                TestAssert.True(input.CurrentFrameSequence>sequence,"left route consumes fresh native keyboard sample");sequence=input.CurrentFrameSequence;Safety();
                if(routeStage=="original_bottom_support"&&samples%4==0)
                    GD.Print($"RESIDENT_GEAR_LEFT_DROP_TRACE player={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} move={input.CurrentFrame.MoveAxis} physics={Engine.GetPhysicsFrames()} input={sequence} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.ReleaseKind}");
            }
            async Task Until(Func<bool> reached,int bound,string stage)
            {
                routeStage=stage;
                int observed=0;for(;observed<bound&&!reached();observed++)await Step();
                GD.Print($"RESIDENT_GEAR_LEFT_STAGE stage={stage} observations={observed}/{bound} player={player.GlobalPosition} velocity={player.Velocity} claw={claw.CurrentFrame.State} target={claw.CurrentTarget?.GetPath()} preview={claw.KeyboardPreview.Surface?.GetPath()} hp={health.CurrentHealth} samples={samples}");
                TestAssert.True(reached(),"actual continuous left access failed at "+stage);
            }
            async Task K()
            {
                int oldCasts=casts.Count,oldCancels=cancels.Count;
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
                for(int i=0;i<8&&casts.Count==oldCasts&&cancels.Count==oldCancels;i++)await Step();
                KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
                TestAssert.True(casts.Count!=oldCasts||cancels.Count!=oldCancels,"one fresh sampled K actually casts or cancels before release");
            }
            bool Arrived(GrappleSurface2D target)=>claw.CurrentTarget==target&&claw.CurrentFrame.State==MechanicalClawState.Swing&&
                claw.CurrentFrame.Arrived&&claw.CurrentFrame.Failure==MechanicalClawFailure.None&&arrivals.Contains((claw.CurrentFrame.CastId,target.GetInstanceId()));
            await Until(()=>On(camp),12,"original_camp_support");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);await Until(()=>player.GlobalPosition.X>=1720,90,"camp_launch");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);for(int i=0;i<8;i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.W,true);for(int i=0;i<3;i++)await Step();
            var resolver=new MechanicalClawTargetResolver();var first=resolver.ResolveKeyboard(player,input.KeyboardAimDirection);
            GD.Print($"RESIDENT_GEAR_LEFT_PREFLIGHT target={first.Surface?.GetPath()} anchor={first.Snapshot.WorldAnchor} normal={first.Snapshot.SurfaceNormal} player={player.GlobalPosition}");
            TestAssert.True(On(camp)&&first.Surface==wall&&first.Snapshot.HasLineOfSight&&first.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.999f&&
                player.GlobalPosition.DistanceTo(first.Snapshot.WorldAnchor)<=620,"normal W selects exact original west face with full LOS/range");
            await K();await Until(()=>Arrived(wall),300,"original_wall_arrival");
            await K();KeyboardActionRuntimeTests.KeyEvent(Key.W,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            await Until(()=>On(bottom)&&player.GlobalPosition.X>=2240,180,"original_bottom_support");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);for(int i=0;i<8;i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);await Until(()=>player.GlobalPosition.X<=1896,120,"whole_body_past_bottom_west_edge");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            bool SideSelected()
            {
                var current=resolver.ResolveKeyboard(player,input.KeyboardAimDirection);
                return current.Surface==side&&current.Snapshot.HasLineOfSight&&current.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.999f&&
                    MathF.Abs(current.Snapshot.WorldAnchor.X-2280)<.1f&&current.Snapshot.WorldAnchor.Y is >=3700 and <=3816&&player.GlobalPosition.DistanceTo(current.Snapshot.WorldAnchor)<=620;
            }
            await Until(()=>FullOn(launch)&&player.Velocity.X>=0&&input.CurrentFrame.MoveAxis>.5f,90,"western_launch_landing");
            await Until(()=>FullOn(launch)&&player.GlobalPosition.X>=2064&&player.GlobalPosition.X<=2080&&player.Velocity.X>240&&SideSelected(),90,"grounded_side_cast_stance");
            var second=resolver.ResolveKeyboard(player,input.KeyboardAimDirection);
            GD.Print($"RESIDENT_GEAR_LEFT_PREFLIGHT target={second.Surface?.GetPath()} anchor={second.Snapshot.WorldAnchor} normal={second.Snapshot.SurfaceNormal} player={player.GlobalPosition}");
            TestAssert.True(second.Surface==side&&second.Snapshot.HasLineOfSight&&second.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.999f&&
                MathF.Abs(second.Snapshot.WorldAnchor.X-2280)<.1f&&MathF.Abs(second.Snapshot.WorldAnchor.Y-player.GlobalPosition.Y)<.1f&&player.GlobalPosition.DistanceTo(second.Snapshot.WorldAnchor)<=620,"actual horizontal D axis selects independent west wall, not old roof or shelf top");
            sideStart=Engine.GetPhysicsFrames();
            await K();
            await Until(()=>
            {
                TestAssert.True(claw.CurrentTarget==side&&claw.CurrentFrame.Failure==MechanicalClawFailure.None&&
                    claw.CurrentFrame.State is MechanicalClawState.Windup or MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel,
                    "same running cast remains genuinely locked to independent side while leaving launch support");
                if(claw.CurrentFrame.State==MechanicalClawState.Reel)return edgeWitness;
                return PlayerBounds().Position.X>=2100.5f;
            },40,"clear_launch_edge_same_cast");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
            await Until(()=>Arrived(side),180,"new_left_side_arrival");
            TestAssert.True(edgeWitness,"actual native first side-Reel capsule query cannot be skipped by clearing the launch edge early");
            ulong sideCast=claw.CurrentFrame.CastId;
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            await Until(()=>boostCasts.Contains(sideCast),8,"side_wall_boost");
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            await Until(()=>climbed&&input.CurrentFrame.MoveAxis>.5f&&player.GlobalPosition.X>=2360,120,"climb_into_shelf_airspace");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            await Until(()=>player.Velocity.X<=0&&input.CurrentFrame.MoveAxis<-.5f,30,"reverse_before_shelf_end");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
            await Until(()=>FullOn(shelf)&&player.GlobalPosition.X is >=2324 and <=2436,120,"full_shelf_landing");
            for(int i=0;i<8;i++)await Step();
            TestAssert.True(Engine.GetPhysicsFrames()-sideStart<=300,"whole independent wall transfer includes real landing/braking");sideStart=0;
            TestAssert.True(FullOn(shelf)&&player.GlobalPosition.X is >2324 and <2436&&MathF.Abs(player.GlobalPosition.Y-3756)<1,"same Player fully settles on exact new left support");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);await Until(()=>player.GlobalPosition.X>=2480,90,"leave_left_shelf_east");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            await Until(()=>player.Velocity.X<=0&&input.CurrentFrame.MoveAxis<-.5f,30,"brake_before_full_piston_sweep");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);await Until(()=>On(piston),90,"supported_original_egress");
            for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(piston)&&player.GlobalPosition.X is >2460 and <2514&&MathF.Abs(player.GlobalPosition.Y-3880)<1&&
                casts.Count==2&&arrivals.Count==2&&cancels.Count==1&&boostCasts.Count==1&&climbed&&health.CurrentHealth==hp&&healed==0,
                "two real casts, one attached Space climb and full living shelf landing retain supported original east egress");
            GD.Print($"RESIDENT_GEAR_LEFT_ROUTE_OK same_player=true start=BasinCampFloor via=AbyssSafeBottom grounded_launch=GearWestLaunchShelf side_entry=GearWestClimbWall climbed=true landed=GearResidentLeftShelf egress=GearPistonOne actual_keyboard=true actual_physics=true position_writes_after_mount=0 casts=2 arrivals=2 cancels=1 jumps=0 dashes=0 boosts=1 healed=0 health={hp}->{health.CurrentHealth} samples={samples} physics_samples={physicsSamples} whole_body_hazard_excluded=true mechanisms_active=false residents_present=false canonical_changed=false formal_spawned=0 normal_world=false publication=false");
            observer.Sample=null;observer.Free();observer=null;player.Free();player=null;await fixture.Sync();TestAssert.Equal(snapshot,fixture.CollisionSnapshot(),"actual traversal never mutates map collision for a passing route");
        }
        finally
        {
            foreach(var key in new[]{Key.A,Key.D,Key.W,Key.S,Key.K,Key.Space})KeyboardActionRuntimeTests.KeyEvent(key,false);
            if(GodotObject.IsInstanceValid(observer)){observer.Sample=null;observer.Free();}
            if(GodotObject.IsInstanceValid(player))player.Free();await fixture.DisposeAsync();
        }
    }

    private static async Task RunGearOriginalFrozenCampDrop(SceneTree tree,NativeFixture fixture,ResidentKeepOut[] danger)
    {
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        BasinBoardingTickObserver observer=null;
        try
        {
            player.Position=new(1800,3244);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            var health=player.GetNode<HealthComponent>("Health");var collider=player.GetNode<CollisionShape2D>("BodyCollider");
            var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
            var exit=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<StaticBody2D>("Gameplay/Collision/FrozenExit");
            var camp=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/BasinCampFloor");
            ulong id=player.GetInstanceId(),sequence=input.CurrentFrameSequence;int healed=0,physics=0,observations=0;
            health.Healed+=(_,_)=>healed++;
            bool On(StaticBody2D floor)
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++)
                {using var hit=player.GetSlideCollision(i);if(hit.GetCollider()==floor&&hit.GetNormal().Dot(Vector2.Up)>.99f)return true;}
                return false;
            }
            bool FullOn(StaticBody2D support)
            {
                if(!On(support))return false;var shape=support.GetNode<CollisionShape2D>("Collision");
                Rect2 floor=shape.GlobalTransform*shape.Shape.GetRect(),body=collider.GlobalTransform*collider.Shape.GetRect();
                if(body.Position.X<floor.Position.X||body.End.X>floor.End.X||MathF.Abs(body.End.Y-floor.Position.Y)>1)return false;
                foreach(float x in new[]{body.Position.X+1,body.GetCenter().X,body.End.X-1})
                {
                    using var q=PhysicsRayQueryParameters2D.Create(new(x,body.End.Y-.25f),new(x,body.End.Y+1),CombatCollisionLayers.World);q.CollideWithAreas=false;
                    using var hit=fixture.Space.IntersectRay(q);if(hit.Count==0||hit["collider"].AsGodotObject()!=support||hit["normal"].AsVector2().Y>-.99f)return false;
                }
                return true;
            }
            bool roundtrip=false;int roundtripHp=0;
            void Safety()
            {
                TestAssert.True(player.GetInstanceId()==id&&health.CurrentHealth is >=4 and <=5&&healed==0&&
                    motor.AcceptedJumpCount==0&&motor.AcceptedDashCount==0&&
                    (roundtrip?health.CurrentHealth==roundtripHp&&claw.CurrentFrame.Failure==MechanicalClawFailure.None&&claw.CurrentFrame.NextCastId<=2:
                        claw.CurrentFrame.State==MechanicalClawState.Idle&&claw.CurrentFrame.NextCastId==1)&&
                    player.GlobalPosition.X>1500&&player.GlobalPosition.X<(roundtrip?2600:2100)&&player.GlobalPosition.Y is >3200 and <4020&&
                    player.GlobalPosition.IsFinite()&&player.Velocity.IsFinite(),"original Frozen drop preserves same actual Player without recovery/ability/pose writes");
                TestAssert.True(collider.Shape is CapsuleShape2D capsule&&capsule.Radius==20&&capsule.Height==72,"original drop full actual40x72 capsule");
                Rect2 body=collider.GlobalTransform*collider.Shape.GetRect();
                TestAssert.True(!danger.Any(x=>x.WorldBounds.Intersects(body)),"original Frozen drop avoids complete all-phase hazard envelopes");
            }
            Exception failure=null;observer=new BasinBoardingTickObserver {ProcessPhysicsPriority=100};
            observer.Sample=_=>{physics++;try{Safety();}catch(Exception error){failure??=error;}};fixture.Scope.AddChild(observer);
            async Task Tick()
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);observations++;
                TestAssert.True(failure==null,"original Frozen drop every-physics safety: "+failure?.Message);
                TestAssert.True(input.CurrentFrameSequence>sequence,"original drop fresh sampled keyboard");sequence=input.CurrentFrameSequence;Safety();
            }
            int settle=0;for(;settle<12&&!On(exit);settle++)await Tick();
            TestAssert.True(On(exit),"old return begins on actual FrozenExit");
            int hp=health.CurrentHealth;ulong landings=motor.LandingCount;
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            int depart=0;for(;depart<120&&(On(exit)||player.IsOnFloor());depart++)await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
            TestAssert.True(!On(exit)&&!player.IsOnFloor(),"old D leaves original FrozenExit in unchanged120 bound");
            int fall=0;for(;fall<160&&!FullOn(camp);fall++)await Tick();
            GD.Print($"RESIDENT_GEAR_OLD_FROZEN_DROP actual_keyboard=true same_player=true position={player.GlobalPosition} velocity={player.Velocity} full_camp={FullOn(camp)} departure={depart}/120 fall={fall}/160 health={hp}->{health.CurrentHealth} landings={landings}->{motor.LandingCount} fall_height={motor.LastFallHeight:R} fall_damage={motor.LastFallDamage} damage_applied={motor.LastFallDamageApplied} observations={observations} physics={physics} pose_writes_after_mount=0 checkpoint_save_proved=false");
            TestAssert.True(FullOn(camp)&&motor.LandingCount==landings+1&&motor.LastFallDamage==1&&motor.LastFallDamageApplied&&health.CurrentHealth==hp-1,
                "new launch must preserve original Frozen-to-camp physical drop, one landing and original one-damage fall; do not waive interception");
            roundtrip=true;
            roundtripHp=health.CurrentHealth;
            var bottom=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<StaticBody2D>("Gameplay/Collision/AbyssSafeBottom");
            var wall=fixture.Sectors[PrologueSectorId.CanyonDescent].GetNode<GrappleSurface2D>("Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile");
            var casts=new HashSet<ulong>();var cancels=new HashSet<ulong>();var arrivals=new HashSet<ulong>();
            claw.ClawFrameChanged+=(frame,target)=>
            {
                if(frame.State==MechanicalClawState.Windup&&frame.CastId!=0)casts.Add(frame.CastId);
                if(frame.ReleaseKind==MechanicalClawReleaseKind.Cancel&&frame.CastId!=0)cancels.Add(frame.CastId);
                if(target==wall&&frame.State==MechanicalClawState.Swing&&frame.Arrived&&frame.Failure==MechanicalClawFailure.None&&frame.LockedTargetRuntimeId==wall.GetInstanceId())arrivals.Add(frame.CastId);
            };
            async Task NativeUntil(Func<bool> reached,int bound,string stage)
            {
                ulong began=Engine.GetPhysicsFrames();
                while(!reached()&&Engine.GetPhysicsFrames()-began<(ulong)bound)await Tick();
                ulong elapsed=Engine.GetPhysicsFrames()-began;
                GD.Print($"RESIDENT_GEAR_OLD_CAMP_STAGE stage={stage} native_ticks={elapsed}/{bound} position={player.GlobalPosition} health={health.CurrentHealth}");
                TestAssert.True(reached()&&elapsed<=(ulong)bound,"original camp roundtrip retains native budget at "+stage);
                TestAssert.Equal(roundtripHp,health.CurrentHealth,"old camp roundtrip no new damage or healing");
            }
            ulong stanceBegan=Engine.GetPhysicsFrames();
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            await NativeUntil(()=>player.GlobalPosition.X<=1720,90,"west_camp_stance");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);for(int i=0;i<8;i++)await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.W,true);for(int i=0;i<3;i++)await Tick();
            TestAssert.True(Engine.GetPhysicsFrames()-stanceBegan<=90,"west camp stance including neutral and W preparation retains shared90 native budget");
            var resolved=new MechanicalClawTargetResolver().ResolveKeyboard(player,input.KeyboardAimDirection);
            TestAssert.True(FullOn(camp)&&resolved.Surface==wall&&resolved.Snapshot.HasLineOfSight&&resolved.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.999f&&
                player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor)<=620,"old return requires actual west-face W preview from camp");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);await NativeUntil(()=>casts.Count==1,8,"one_original_cast");KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            await NativeUntil(()=>claw.CurrentTarget==wall&&claw.CurrentFrame.State==MechanicalClawState.Swing&&claw.CurrentFrame.Arrived&&arrivals.Contains(claw.CurrentFrame.CastId),300,"original_wall_arrival");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);await NativeUntil(()=>cancels.Count==1,8,"one_original_cancel");KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            KeyboardActionRuntimeTests.KeyEvent(Key.W,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            await NativeUntil(()=>FullOn(bottom)&&player.GlobalPosition.X>=2240,180,"original_abyss_support");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);for(int i=0;i<8;i++)await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            int stable=0;ulong stableSequence=input.CurrentFrameSequence;
            await NativeUntil(()=>
            {
                bool landed=FullOn(camp)&&player.GlobalPosition.X<=1820;
                if(landed)KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
                if(input.CurrentFrameSequence!=stableSequence)
                {
                    stableSequence=input.CurrentFrameSequence;
                    stable=landed&&input.CurrentFrame.MoveAxis==0&&MathF.Abs(player.Velocity.X)<=2?stable+1:0;
                }
                return stable>=12;
            },180,"original_camp_return_neutral12");
            TestAssert.True(FullOn(camp)&&casts.SetEquals(new ulong[]{1})&&arrivals.SetEquals(casts)&&cancels.SetEquals(casts)&&health.CurrentHealth==roundtripHp,
                "same Player physically returns to original camp through new geometry with one matched original cast/cancel");
            GD.Print($"RESIDENT_GEAR_OLD_CAMP_RETURN_OK same_player=true from=FrozenExit via=BasinCampFloor,AbyssSafeBottom returned=BasinCampFloor actual_keyboard=true position_writes_after_mount=0 full_body=40x72 casts=1 arrivals=1 cancels=1 jumps=0 dashes=0 healed=0 health={roundtripHp}->{health.CurrentHealth} checkpoint_save_proved=false normal_world=false");
        }
        finally
        {
            foreach(var key in new[]{Key.A,Key.D,Key.W,Key.K})KeyboardActionRuntimeTests.KeyEvent(key,false);
            if(GodotObject.IsInstanceValid(observer)){observer.Sample=null;observer.Free();}
            if(GodotObject.IsInstanceValid(player))player.Free();await fixture.Sync();
        }
    }

    private static IEnumerable<Node> GearDescendants(Node root)
    {
        yield return root;
        for(int i=0;i<root.GetChildCount();i++)
            foreach(var child in GearDescendants(root.GetChild(i)))yield return child;
    }
}
