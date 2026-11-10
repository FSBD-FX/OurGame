using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Integration;

public static partial class CanyonResidentPlacementTests
{
    public static async Task RunSteamCandidate(SceneTree tree,string storage)
    {
        string originalEnvironment=(await CaptureGearOriginalBaseline(tree,fixture=>{TraceSteamClawCapsule(fixture,true);TraceSteamClawWindows(fixture,true);})).Environment;
        var slot=CanyonResidentCatalog.All.Single(x=>x.StableId=="canyon.v2.resident.steam-valves.02");
        var type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId);
        TestAssert.True(type.Archetype==ResidentArchetype.Wanderer&&type.Family==ResidentFamily.Scrap,"actual allocated scrap-wanderer, not Heavy substitution");
        string leaves=Path.Combine(storage,"steam-leaves");Directory.CreateDirectory(leaves);
        using(var profile=new ResidentSubtypeProfile {SubtypeId=type.Id,Family=type.Family,Archetype=type.Archetype,Carrier=type.Carrier})
        {
            var author=GD.Load<PackedScene>(type.ProbeScenePath).Instantiate<CharacterBody2D>();
            try
            {
                author.SceneFilePath="";author.AddChild(new ResidentSceneBinding {Name="ResidentBinding",Profile=profile});
                TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(author,Path.Combine(leaves,type.Id+".tscn")),"save actual scrap placeholder leaf");
            }
            finally{author.Free();}
        }
        var paths=SaveGearMap(storage);var fixture=new NativeFixture(tree);using var source=new CensusLeafSource(leaves);
        var originals=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);RustShellSquire resident=null;
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();
            CharacterBody2D Original(string path)
            {if(!originals.TryGetValue(path,out var actor)){actor=GD.Load<PackedScene>(path).Instantiate<CharacterBody2D>();originals.Add(path,actor);}return actor;}
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.Equal(0,environment.Issues.Count,"all original environment reservations remain valid");
            TestAssert.Equal(originalEnvironment,GearEnvironmentKey(environment),"new bay preserves complete actual canonical hazard/protection inventory");
            var old=OriginalSpawnReservations(Original);TestAssert.Equal(36,old.Count,"all original primary/alternate records, not36 actors");
            var keep=environment.Regions.Concat(old).ToArray();
            Rect2 route=CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.SteamValves).WorldBounds;
            Vector2 home=new(3780,3880),left=new(3716,3880),right=new(3844,3880);
            Rect2 activity=new(3698,3848,164,64),floorRect=new(3680,3912,200,24);
            TraceSteamClawCapsule(fixture,false);
            TraceSteamClawWindows(fixture,false);
            var oldPost=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3956Grip");
            using(var ray=PhysicsRayQueryParameters2D.Create(new Vector2(3970,4040),new Vector2(4232,3826),CombatCollisionLayers.World))
            {
                ray.CollideWithAreas=false;ray.CollideWithBodies=true;
                using var hit=fixture.Space.IntersectRay(ray);
                TestAssert.True(hit.Count>0&&hit["collider"].AsGodotObject()==oldPost,"new bay must not intercept original NE first-ascent post line");
                GD.Print("RESIDENT_STEAM_OLD_CLAW_LOS_OK actual_saved_world=true first_hit=ShortcutStep3956Grip native_ray=true full_route=false");
            }
            var raw=source.Raw(type);
            var forward=ResidentPlacementGeometry.CheckCorridor(raw,new(0,left),new(0,right),fixture.Space,fixture.Scope,route,true,keep);
            var reverse=ResidentPlacementGeometry.CheckCorridor(raw,new(0,right),new(0,left),fixture.Space,fixture.Scope,route,true,keep);
            GD.Print($"RESIDENT_STEAM_CORRIDOR forward={forward.CorridorClear} reverse={reverse.CorridorClear} issues={string.Join(",",forward.Issues.Concat(reverse.Issues))}");
            TestAssert.True(forward.CorridorClear&&reverse.CorridorClear,"real Steam bay must carry complete164x64 supported patrol in both directions: "+string.Join(",",forward.Issues.Concat(reverse.Issues)));
            TestAssert.Equal(activity,forward.WorldCorridorBounds,"literal full body plus128px travel, never reduced patrol");
            fixture.AssertNativePreconditions(143);
            var sector=fixture.Sectors[PrologueSectorId.CanyonBasin];
            var floor=sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamResidentBay");
            var collider=floor.GetNode<CollisionShape2D>("Collision");
            TestAssert.Equal(floorRect,collider.GlobalTransform*collider.Shape.GetRect(),"real saved24px structural plate");
            var skin=sector.GetNode<Knight.Presentation.CanyonPlatformVisual>("VisualRoot/GameplayReadable/SteamResidentBay");
            TestAssert.True(skin.SkinSize==floorRect.Size&&skin.GlobalPosition==floor.GlobalPosition,"saved skin and collision agree");
            TestAssert.True(keep.All(x=>!x.WorldBounds.Intersects(activity)),"complete activity avoids original protected/hazard/old actor reservations");
            var definitions=paths.Select(pair=>new PrologueSectorDefinition(pair.Key,GD.Load<PackedScene>(pair.Value),PrologueCanyonBuildParts.WorldOffsets[pair.Key],PrologueCanyonBuildParts.Bounds(pair.Key),null,null)).ToArray();
            ulong[] ownedDefinitionIds=definitions.Select(x=>x.GetInstanceId()).ToArray(),borrowedSceneIds=definitions.Select(x=>x.Scene.GetInstanceId()).ToArray();
            try
            {
                using var catalog=MinimapTerrainCatalogBuilder.Build(definitions);
                TestAssert.Equal(143,catalog.SolidCount,"same-five minimap includes real Steam bay");
                TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds==floorRect),"literal new bay in minimap");
                TestAssert.Equal(Error.Ok,ResourceSaver.Save(catalog,Path.Combine(storage,"steam-candidate-minimap.tres")),"save actual five-sector minimap");
            }
            finally{foreach(var definition in definitions)definition.Dispose();}
            TestAssert.True(ownedDefinitionIds.All(id=>!GodotObject.IsInstanceIdValid(id))&&borrowedSceneIds.All(GodotObject.IsInstanceIdValid),
                "five temporary owned definitions must be released after catalog build while loaded shared scenes remain valid");
            GD.Print("RESIDENT_STEAM_MINIMAP_OWNERS_OK definitions_released=5 borrowed_scenes_preserved=5 historical_resource_faults_closed=false");
            string savedEnvironment=GearEnvironmentKey(environment);
            int healed=0;resident=(RustShellSquire)source.Configure(slot,new[]{home,left,right,home},route);
            resident.GetNode<HealthComponent>("Health").Healed+=(_,_)=>healed++;
            foreach(var shape in GearDescendants(resident).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))shape.Shape=fixture.Own((Shape2D)shape.Shape.Duplicate());
            var parent=sector.GetNode<Node2D>("Gameplay/Actors");resident.Transform=parent.GlobalTransform.AffineInverse()*new Transform2D(0,home);parent.AddChild(resident);
            await fixture.Sync();float min=home.X,max=home.X,maxStep=0;Vector2 previous=resident.GlobalPosition;bool returned=false;
            for(int frame=0;frame<360;frame++)
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                Vector2 position=resident.GlobalPosition;min=MathF.Min(min,position.X);max=MathF.Max(max,position.X);maxStep=MathF.Max(maxStep,position.DistanceTo(previous));previous=position;
                var body=resident.GetNode<CollisionShape2D>("BodyCollider");
                TestAssert.True(body.Shape is CapsuleShape2D capsule&&capsule.Radius==18&&capsule.Height==64&&activity.Grow(.5f).Encloses(body.GlobalTransform*body.Shape.GetRect())&&
                    MathF.Abs(position.Y-3880)<.5f&&resident.IsOnFloor()&&resident.GetNode<HealthComponent>("Health").CurrentHealth==3&&healed==0,
                    "actual full64px saved resident stays supported, within original patrol and HP3, without shrink/heal/pose writes");
                returned|=min<=3722&&max>=3838&&MathF.Abs(position.X-home.X)<3;
            }
            TestAssert.True(min<=3722&&max>=3838&&maxStep<1.5f&&returned,"actual resident patrols both ends and home without teleport");
            GD.Print($"RESIDENT_STEAM_PATROL_OK min={min} max={max} max_step={maxStep} frames=360 full_body=true returned=true health=3 healed=0");
            resident.Free();resident=null;await fixture.Sync();
            await RunSteamPlayerVisit(tree,fixture,environment);
            TestAssert.Equal(savedEnvironment,GearEnvironmentKey(ResidentEnvironmentInventory.Capture(fixture.Scope)),"all potential hazards/protected reservations unchanged after native visits");
            GD.Print("RESIDENT_STEAM_CANDIDATE_OK identities=1 native_terrain=143 patrol_frames=360 global_old_records=36 physical_plate_thickness=24 original_hazards_preserved=true native_player_motion=true canonical_changed=false formal_spawned=0 enclosure_complete=false normal_world=false publication=false");
        }
        finally
        {
            if(resident!=null&&GodotObject.IsInstanceValid(resident))resident.Free();
            foreach(var actor in originals.Values)actor.Free();await fixture.DisposeAsync();
        }
    }

    private static void TraceSteamClawCapsule(NativeFixture fixture,bool canonical)
    {
        var probe=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        try
        {
            Vector2 start=new(3960,4052),end=new(4166,3822);
            foreach(var point in new[]{start,end})
                TestAssert.True(ResidentPlacementGeometry.CheckBody(probe,new(0,point),fixture.Space,fixture.Scope,fixture.Bounds,false,Array.Empty<ResidentKeepOut>()).BodyGeometryClear,
                    "capsule sweep endpoints must be independently clear, not rely on CastMotion ignoring initial overlap");
            using var query=new PhysicsShapeQueryParameters2D {Shape=probe.GetNode<CollisionShape2D>("BodyCollider").Shape,
                Transform=new Transform2D(0,start),Motion=end-start,CollisionMask=CombatCollisionLayers.World,CollideWithAreas=false,CollideWithBodies=true};
            float[] travel=fixture.Space.CastMotion(query);
            GD.Print($"RESIDENT_STEAM_OLD_CLAW_CAPSULE canonical={canonical} safe_fraction={(travel.Length>0?travel[0]:-1)} fixed_linear_probe=true actual_claw_motion=false");
            TestAssert.True(travel.Length==2&&float.IsFinite(travel[0])&&travel[0]>=.9999f,"new bay must preserve original full40x72 capsule ascent sweep, not just target ray");
        }
        finally{probe.Free();}
    }

    private static void TraceSteamClawWindows(NativeFixture fixture,bool canonical)
    {
        Vector2[] origins={new(3970,4040),new(3880,4040),new(3970,3986)};
        var oldPost=fixture.Sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3956Grip");
        for(int i=0;i<origins.Length;i++)
        {
            using var ray=PhysicsRayQueryParameters2D.Create(origins[i],new Vector2(4232,3826),CombatCollisionLayers.World);
            ray.CollideWithAreas=false;ray.CollideWithBodies=true;using var hit=fixture.Space.IntersectRay(ray);
            bool preserved=hit.Count>0&&hit["collider"].AsGodotObject()==oldPost;
            TestAssert.True(preserved,(canonical?"canonical control":"new resident bay")+" must preserve original first-ascent post for window "+i);
            string first=hit.Count>0?(hit["collider"].AsGodotObject() as Node)?.Name.ToString():"none";
            GD.Print($"RESIDENT_STEAM_OLD_CLAW_WINDOW canonical={canonical} window={i} origin={origins[i]} first_hit={first} original_target_preserved={preserved} full_route_certified=false");
        }
    }

    private static async Task RunSteamPlayerVisit(SceneTree tree,NativeFixture fixture,ResidentEnvironmentSnapshot environment)
    {
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();ResidentSteamPhysicsObserver observer=null;
        try
        {
            player.Position=new(3940,3760);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            var health=player.GetNode<HealthComponent>("Health");var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
            ulong identity=player.GetInstanceId(),sequence=input.CurrentFrameSequence;int hp=health.CurrentHealth,healed=0,samples=0,physics=0;bool clawUsed=false;Exception safetyFailure=null;
            health.Healed+=(_,_)=>healed++;claw.ClawFrameChanged+=(frame,_)=>{if(frame.State!=MechanicalClawState.Idle||frame.CastId!=0)clawUsed=true;};
            var sector=fixture.Sectors[PrologueSectorId.CanyonBasin];var start=sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3796");
            var egress=sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep4116");
            var floor=sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamResidentBay");
            var danger=environment.Regions.Where(x=>x.Kind is ResidentKeepOutKind.Hazard or ResidentKeepOutKind.MovingOccupancy).ToArray();
            bool On(StaticBody2D support)
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++){using var hit=player.GetSlideCollision(i);if(hit.GetCollider()==support&&hit.GetNormal().Dot(Vector2.Up)>.99f)return true;}
                return false;
            }
            void Safety()
            {
                physics++;var body=player.GetNode<CollisionShape2D>("BodyCollider");Rect2 bounds=body.GlobalTransform*body.Shape.GetRect();
                TestAssert.True(body.Shape is CapsuleShape2D capsule&&capsule.Radius==20&&capsule.Height==72&&
                    float.IsFinite(player.GlobalPosition.X)&&float.IsFinite(player.GlobalPosition.Y)&&float.IsFinite(player.Velocity.X)&&float.IsFinite(player.Velocity.Y)&&
                    player.GlobalPosition.X is >3640 and <4280&&player.GlobalPosition.Y is >3600 and <4180&&
                    player.GetInstanceId()==identity&&health.CurrentHealth==hp&&healed==0&&!clawUsed&&motor.AcceptedDashCount==0&&
                    danger.All(x=>!x.WorldBounds.Intersects(bounds)),"same Player full capsule remains alive/finite and outside all potential danger every native physics tick: "+player.GlobalPosition);
            }
            observer=new ResidentSteamPhysicsObserver {Name="SteamBodyObserver",ProcessPhysicsPriority=100,Sample=()=>{try{Safety();}catch(Exception e){safetyFailure??=e;}}};fixture.Scope.AddChild(observer);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);samples++;
                TestAssert.True(safetyFailure==null,"per-physics full capsule safety: "+safetyFailure);
                TestAssert.True(input.CurrentFrameSequence>sequence&&!input.CurrentFrame.DashPressed&&!input.CurrentFrame.ClawPressed,"actual fresh keyboard sample without hidden dash/claw");sequence=input.CurrentFrameSequence;
            }
            for(int i=0;i<10;i++)await Step();TestAssert.True(On(start),"initial actual Player supported by old named ShortcutStep3796");
            KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            for(int i=0;i<120&&player.GlobalPosition.X>3784;i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            int brake=0;do{await Step();brake++;}while(player.Velocity.X<0&&brake<30);
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
            for(int i=0;i<120&&!On(floor);i++)await Step();for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(floor)&&player.GlobalPosition.X is >3700 and <3860&&MathF.Abs(player.GlobalPosition.Y-3876)<1&&motor.AcceptedJumpCount==0,
                "real left departure/inertia braking lands completely on new bay: "+player.GlobalPosition);
            GD.Print($"RESIDENT_STEAM_PLAYER_LANDED position={player.GlobalPosition} velocity={player.Velocity}");
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            for(int i=0;i<120&&player.GlobalPosition.X<3942;i++)await Step();
            KeyboardActionRuntimeTests.KeyEvent(Key.D,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            brake=0;do{await Step();brake++;}while(player.Velocity.X>0&&brake<30);
            KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
            for(int i=0;i<120&&!On(egress);i++)await Step();for(int i=0;i<8;i++)await Step();
            TestAssert.True(On(egress)&&player.GlobalPosition.X is >3860 and <3970&&MathF.Abs(player.GlobalPosition.Y-4080)<1&&motor.AcceptedJumpCount==0&&
                motor.AcceptedDashCount==0&&!clawUsed&&healed==0&&health.CurrentHealth==hp,"same Player clears full jet occupancy and reaches original lower support without jump or pose injection: "+player.GlobalPosition);
            GD.Print($"RESIDENT_STEAM_PLAYER_VISIT_OK same_player=true actual_keyboard=true actual_physics=true position_writes_after_mount=0 start=ShortcutStep3796 landed=SteamResidentBay egress=ShortcutStep4116 jumps=0 dashes=0 claws=0 healed=0 health={hp}->{health.CurrentHealth} samples={samples} physics_samples={physics} whole_body_hazard_excluded=true mechanisms_active=false residents_present=false normal_world=false");
        }
        finally
        {
            foreach(var key in new[]{Key.A,Key.D,Key.Space})KeyboardActionRuntimeTests.KeyEvent(key,false);
            if(observer!=null&&GodotObject.IsInstanceValid(observer))observer.Free();
            if(GodotObject.IsInstanceValid(player))player.Free();
        }
    }
}

internal partial class ResidentSteamPhysicsObserver:Node
{
    internal Action Sample;
    public override void _PhysicsProcess(double delta)=>Sample?.Invoke();
}
