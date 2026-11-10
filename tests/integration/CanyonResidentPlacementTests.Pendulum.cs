using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tools;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class CanyonResidentPlacementTests
{
    public static async Task RunPendulumCandidate(SceneTree tree,string storage)
    {
        string originalEnvironment;
        var baseline=new NativeFixture(tree);
        try
        {
            baseline.Initialize();baseline.MountSavedSectors(ResidentTerrainSynchronizationTests.BaselinePaths);await baseline.Sync();baseline.StopCallbacks();baseline.AssertNativePreconditions(127);
            var environment=ResidentEnvironmentInventory.Capture(baseline.Scope);originalEnvironment=GearEnvironmentKey(environment);
            await RunPendulumPlayer(tree,baseline,environment,false);
        }
        finally {await baseline.DisposeAsync();}

        string leaves=Path.Combine(storage,"pendulum-leaves");Directory.CreateDirectory(leaves);
        var slot=CanyonResidentCatalog.All.Single(x=>x.StableId=="canyon.v2.resident.pendulum-gallery.04");
        var type=CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slot.SubtypeId);
        TestAssert.True(type.Archetype==ResidentArchetype.Turret&&type.Family==ResidentFamily.Precision,"actual allocated precision fixed laser");
        using(var profile=new ResidentSubtypeProfile {SubtypeId=type.Id,Family=type.Family,Archetype=type.Archetype,Carrier=type.Carrier})
        {
            var author=GD.Load<PackedScene>(type.ProbeScenePath).Instantiate<CharacterBody2D>();
            try {author.SceneFilePath="";author.AddChild(new ResidentSceneBinding{Name="ResidentBinding",Profile=profile});TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(author,Path.Combine(leaves,type.Id+".tscn")),"save real laser placeholder leaf");}
            finally {author.Free();}
        }
        var paths=SaveGearMap(storage);
        var fixture=new NativeFixture(tree);using var source=new CensusLeafSource(leaves);
        var originals=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);PrismLaserGunner laser=null;
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.True(environment.Issues.Count==0&&GearEnvironmentKey(environment)==originalEnvironment,"all original hazards/protection/controls preserved");
            CharacterBody2D Original(string path) {if(!originals.TryGetValue(path,out var actor)){actor=GD.Load<PackedScene>(path).Instantiate<CharacterBody2D>();originals.Add(path,actor);}return actor;}
            var old=OriginalSpawnReservations(Original);TestAssert.Equal(36,old.Count,"complete old primary/alternate records remain");
            var keep=environment.Regions.Concat(old).ToArray();var route=CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.PendulumGallery).WorldBounds;
            Vector2 home=new(5028,2491);Rect2 body=new(5003,2452,50,68);var raw=source.Raw(type);
            var admission=ResidentPlacementGeometry.CheckBody(raw,new(0,home),fixture.Space,fixture.Scope,route,true,keep);
            GD.Print($"RESIDENT_PENDULUM_BODY clear={admission.BodyGeometryClear} bounds={admission.WorldBodyBounds} issues={string.Join(",",admission.Issues)}");
            TestAssert.Equal(body,admission.WorldBodyBounds,"real50x68 laser including collider localY-5");
            TestAssert.True(admission.BodyGeometryClear,"actual finite east alcove must support full laser with every reservation: "+string.Join(",",admission.Issues));
            fixture.AssertNativePreconditions(143);
            var sector=fixture.Sectors[PrologueSectorId.CanyonAscent];
            var wanted=new Dictionary<string,Rect2>{{"PendulumResidentFloor",new(4936,2520,184,40)},{"PendulumResidentRoof",new(4992,2344,128,40)},{"PendulumResidentBackWall",new(5096,2384,24,176)}};
            foreach(var pair in wanted)
            {
                var solid=sector.GetNode<StaticBody2D>("Gameplay/Collision/"+pair.Key);var shape=solid.GetNode<CollisionShape2D>("Collision");
                TestAssert.Equal(pair.Value,shape.GlobalTransform*shape.Shape.GetRect(),"literal real40px-or24px structural solid");
                var skin=sector.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/"+pair.Key);
                TestAssert.True(skin.GlobalPosition==solid.GlobalPosition&&skin.SkinSize==pair.Value.Size,"replaceable skin agrees with separate collision");
            }
            var definitions=paths.Select(x=>new PrologueSectorDefinition(x.Key,GD.Load<PackedScene>(x.Value),PrologueCanyonBuildParts.WorldOffsets[x.Key],PrologueCanyonBuildParts.Bounds(x.Key),null,null)).ToArray();
            try
            {
                using var catalog=MinimapTerrainCatalogBuilder.Build(definitions);TestAssert.Equal(143,catalog.SolidCount,"same-five map includes three actual Ascent solids");
                foreach(var rectangle in wanted.Values)TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds==rectangle),"literal new structure in minimap");
            }
            finally {foreach(var definition in definitions)definition.Dispose();}
            int healed=0;laser=(PrismLaserGunner)source.Configure(slot,new[]{home},route);laser.GetNode<HealthComponent>("Health").Healed+=(_,_)=>healed++;
            foreach(var shape in GearDescendants(laser).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))shape.Shape=fixture.Own((Shape2D)shape.Shape.Duplicate());
            var parent=sector.GetNode<Node2D>("Gameplay/Actors");laser.Transform=parent.GlobalTransform.AffineInverse()*new Transform2D(0,home);parent.AddChild(laser);await fixture.Sync();
            for(int i=0;i<360;i++)
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                var actual=laser.GetNode<CollisionShape2D>("BodyCollider");
                TestAssert.True(actual.Shape is CapsuleShape2D capsule&&capsule.Radius==25&&capsule.Height==68&&
                    (actual.GlobalTransform*actual.Shape.GetRect()).Position.DistanceTo(body.Position)<.5f&&laser.GlobalPosition.DistanceTo(home)<.5f&&
                    laser.GetNode<HealthComponent>("Health").CurrentHealth==2&&healed==0,"actual full laser remains on real floor at fixed home with HP2, no movement/health injection");
            }
            GD.Print("RESIDENT_PENDULUM_LASER_OK observations=360 body=50x68 local_y=-5 health=2 healed=0 full_keepouts=true stationary=true attack_tested=false");
            laser.Free();laser=null;await fixture.Sync();
            await RunPendulumPlayer(tree,fixture,environment,true);
            TestAssert.Equal(originalEnvironment,GearEnvironmentKey(ResidentEnvironmentInventory.Capture(fixture.Scope)),"all environment reservations remain unchanged after motion");
            GD.Print("RESIDENT_PENDULUM_CANDIDATE_OK native_terrain=143 saved_sectors=5 solids_added=3 full_laser_body=true original_claw_transfers=2 actual_player_visit=true canonical_changed=false formal_spawned=0 normal_world=false publication=false");
        }
        finally {if(laser!=null&&GodotObject.IsInstanceValid(laser))laser.Free();foreach(var actor in originals.Values)actor.Free();await fixture.DisposeAsync();}
    }

    private static async Task RunPendulumPlayer(SceneTree tree,NativeFixture fixture,ResidentEnvironmentSnapshot environment,bool candidate)
    {
        ((SubViewport)fixture.Scope.GetViewport()).Size=new(1280,720);
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();ResidentSteamPhysicsObserver observer=null;
        try
        {
            player.Position=new(4392,2804);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");var health=player.GetNode<HealthComponent>("Health");
            ulong identity=player.GetInstanceId(),sequence=input.CurrentFrameSequence,transferStart=0,edgeSequence=0;int hp=health.CurrentHealth,healed=0,samples=0,physics=0,clawEdges=0;bool boost=false;Exception failure=null;
            var arrivals=new HashSet<(ulong Cast,ulong Target)>();var cancels=new HashSet<ulong>();
            health.Healed+=(_,_)=>healed++;
            claw.ClawFrameChanged+=(frame,target)=> {boost|=frame.ReleaseKind==MechanicalClawReleaseKind.Boost;if(frame.ReleaseKind==MechanicalClawReleaseKind.Cancel&&frame.CastId!=0)cancels.Add(frame.CastId);if(frame.Arrived&&frame.State==MechanicalClawState.Swing&&frame.Failure==MechanicalClawFailure.None&&target!=null)arrivals.Add((frame.CastId,target.GetInstanceId()));};
            var ascent=fixture.Sectors[PrologueSectorId.CanyonAscent];StaticBody2D Solid(string name)=>ascent.GetNode<StaticBody2D>("Gameplay/Collision/"+name);
            var danger=environment.Regions.Where(x=>x.Kind is ResidentKeepOutKind.Hazard or ResidentKeepOutKind.MovingOccupancy).ToArray();
            bool On(StaticBody2D floor)
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++){using var hit=player.GetSlideCollision(i);if(hit.GetCollider()==floor&&hit.GetNormal().Dot(Vector2.Up)>.99f)return true;}return false;
            }
            bool FullOn(StaticBody2D floor)
            {
                if(!On(floor))return false;
                var feet=player.GetNode<CollisionShape2D>("BodyCollider");var plate=floor.GetNode<CollisionShape2D>("Collision");
                Rect2 body=feet.GlobalTransform*feet.Shape.GetRect(),support=plate.GlobalTransform*plate.Shape.GetRect();
                if(body.Position.X<support.Position.X||body.End.X>support.End.X||MathF.Abs(body.End.Y-support.Position.Y)>1)return false;
                foreach(float x in new[]{body.Position.X+1,body.GetCenter().X,body.End.X-1})
                {
                    using var query=PhysicsRayQueryParameters2D.Create(new(x,body.End.Y-.25f),new(x,body.End.Y+1),CombatCollisionLayers.World);
                    query.CollideWithAreas=false;using var hit=fixture.Space.IntersectRay(query);
                    if(hit.Count==0||hit["collider"].AsGodotObject()!=floor||hit["normal"].AsVector2().Y>-.99f)return false;
                }
                return true;
            }
            void Safety()
            {
                physics++;if(input.CurrentFrameSequence>edgeSequence){edgeSequence=input.CurrentFrameSequence;if(input.CurrentFrame.ClawPressed)clawEdges++;}
                var collider=player.GetNode<CollisionShape2D>("BodyCollider");Rect2 body=collider.GlobalTransform*collider.Shape.GetRect();
                TestAssert.True(player.GetInstanceId()==identity&&health.CurrentHealth==hp&&healed==0&&!boost&&motor.AcceptedDashCount==0&&
                    player.GlobalPosition.IsFinite()&&player.Velocity.IsFinite()&&player.GlobalPosition.X is >4220 and <5100&&player.GlobalPosition.Y is >2100 and <2880&&
                    collider.Shape is CapsuleShape2D capsule&&capsule.Radius==20&&capsule.Height==72&&danger.All(x=>!x.WorldBounds.Intersects(body)),"same full Player avoids all potential hazards without heal/dash/boost/pose injection: "+player.GlobalPosition);
                if(transferStart!=0)TestAssert.True(Engine.GetPhysicsFrames()-transferStart<=300,"original entire claw transfer cap300 includes prep/cast/fall");
            }
            observer=new ResidentSteamPhysicsObserver {ProcessPhysicsPriority=100,Sample=()=>{try{Safety();}catch(Exception e){failure??=e;}}};fixture.Scope.AddChild(observer);
            async Task Step()
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);samples++;
                TestAssert.True(failure==null,"every-physics safety: "+failure);TestAssert.True(input.CurrentFrameSequence>sequence&&!input.CurrentFrame.DashPressed,"fresh actual keyboard/motor sample");sequence=input.CurrentFrameSequence;
            }
            async Task Until(Func<bool> done,int cap,string stage)
            {
                int i=0;for(;i<cap&&!done();i++)await Step();GD.Print($"RESIDENT_PENDULUM_PLAYER_STAGE canonical={!candidate} stage={stage} samples={i}/{cap} position={player.GlobalPosition} velocity={player.Velocity} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure} target={claw.CurrentTarget?.GetPath()}");TestAssert.True(done(),"actual native stage failed: "+stage);
            }
            async Task Stance(float x,int preparationCap,StaticBody2D support,string stage)
            {
                ulong start=Engine.GetPhysicsFrames(),neutralSequence=input.CurrentFrameSequence;int prepare=0,settle=0;
                KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
                async Task PrepareStep(){TestAssert.True(FullOn(support),"stance retains complete original floor support");await Step();TestAssert.True(FullOn(support),"stance retains complete original floor support after physics");}
                while(prepare<12&&(MathF.Abs(player.Velocity.X)>2||input.CurrentFrameSequence<=neutralSequence||input.CurrentFrame.MoveAxis!=0)){await PrepareStep();prepare++;}
                TestAssert.True(MathF.Abs(player.Velocity.X)<=2&&input.CurrentFrame.MoveAxis==0,"stance begins fresh neutral");
                int direction=Math.Sign(x-player.GlobalPosition.X);KeyboardActionRuntimeTests.KeyEvent(Key.A,direction<0);KeyboardActionRuntimeTests.KeyEvent(Key.D,direction>0);
                while(prepare<preparationCap)
                {
                    var stop=FreeMotionForecast.Neutral(player.Velocity.X,true,motor.MotionSettings,motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,12,2f);
                    TestAssert.True(stop.SpeedThresholdReached,"real neutral stopping forecast fits original settling cap");
                    if(direction==0||(x-player.GlobalPosition.X)*direction<=Mathf.Max(0,direction*stop.NetDistance))break;
                    await PrepareStep();prepare++;TestAssert.True(Engine.GetPhysicsFrames()-start<=(ulong)preparationCap,"native preparation cap retained");
                }
                KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,false);ulong stopSequence=input.CurrentFrameSequence,settleStart=Engine.GetPhysicsFrames();
                while(settle<12&&(MathF.Abs(player.Velocity.X)>2||input.CurrentFrameSequence<=stopSequence||input.CurrentFrame.MoveAxis!=0)){await PrepareStep();settle++;}
                TestAssert.True(FullOn(support)&&MathF.Abs(player.GlobalPosition.X-x)<=4&&MathF.Abs(player.Velocity.X)<=2&&input.CurrentFrame.MoveAxis==0&&Engine.GetPhysicsFrames()-settleStart<=12&&Engine.GetPhysicsFrames()-start<=(ulong)(preparationCap+12),"bounded physical stance "+stage+" at "+player.GlobalPosition);
                GD.Print($"RESIDENT_PENDULUM_STANCE canonical={!candidate} stage={stage} position={player.GlobalPosition} velocity={player.Velocity} prepare={prepare}/{preparationCap} settle={settle}/12 native_ticks={Engine.GetPhysicsFrames()-start}");
            }
            async Task ClawTransfer(int index,Key horizontal,float clearX)
            {
                transferStart=Engine.GetPhysicsFrames();int jumps=motor.AcceptedJumpCount;var from=Solid("AscentGrip"+(index-1));var to=Solid("AscentGrip"+index);var target=Solid("AscentGrip"+index+"Grip").GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
                if(index==5)
                {
                    await Stance(4768,40,from,"original_stance5");
                }
                TestAssert.True(On(from)&&claw.CurrentFrame.State==MechanicalClawState.Idle,"real claw transfer starts on old support/Idle");
                ulong next=claw.CurrentFrame.NextCastId,issueTick=Engine.GetPhysicsFrames();int oldEdges=clawEdges;
                KeyboardActionRuntimeTests.KeyEvent(Key.W,true);KeyboardActionRuntimeTests.KeyEvent(horizontal,true);KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
                await Until(()=>clawEdges==oldEdges+1&&claw.CurrentTarget==target&&claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel,8,"original_K"+index+"_admission");
                TestAssert.True(Engine.GetPhysicsFrames()-issueTick<=8&&(claw.CurrentFrame.State==MechanicalClawState.Aim?claw.CurrentFrame.CastId==0&&claw.CurrentFrame.NextCastId==next:claw.CurrentFrame.CastId==next&&claw.CurrentFrame.NextCastId==next+1),"fresh K and subsequent cast are distinct real production stages within original admission cap");
                KeyboardActionRuntimeTests.KeyEvent(Key.K,false);KeyboardActionRuntimeTests.KeyEvent(Key.W,false);
                if(claw.CurrentFrame.State==MechanicalClawState.Aim)await Step();
                TestAssert.True(claw.CurrentFrame.CastId==next&&claw.CurrentFrame.NextCastId==next+1&&claw.CurrentTarget==target,"one accepted K advances to the same real cast without retry");
                int clearance=0;
                while((horizontal==Key.D?player.GlobalPosition.X<clearX:player.GlobalPosition.X>clearX)&&clearance<40)
                {
                    TestAssert.True(On(from)&&claw.CurrentFrame.State is MechanicalClawState.Windup or MechanicalClawState.Travel or MechanicalClawState.Latched,"original post clearance walk must precede Reel and stay on original support");await Step();clearance++;
                }
                KeyboardActionRuntimeTests.KeyEvent(horizontal,false);
                TestAssert.True(horizontal==Key.D?player.GlobalPosition.X>=clearX:player.GlobalPosition.X<=clearX,"actual whole body clears original source post");
                await Until(()=>arrivals.Contains((next,target.GetInstanceId()))&&claw.CurrentFrame.CastId==next&&claw.CurrentTarget==target&&claw.CurrentFrame.State==MechanicalClawState.Swing,150,"original_K"+index+"_arrived");
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);await Until(()=>cancels.Contains(next)&&claw.CurrentFrame.State==MechanicalClawState.Idle,8,"original_K"+index+"_cancel");KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
                await Until(()=>FullOn(to),120,"original_Grip"+index+"_landing");for(int i=0;i<5;i++)await Step();
                TestAssert.True(FullOn(to)&&motor.AcceptedJumpCount==jumps&&Engine.GetPhysicsFrames()-transferStart<=300,"one real original cast/cancel lands fully without jump under original shared cap");transferStart=0;
            }
            for(int i=0;i<10;i++)await Step();TestAssert.True(On(Solid("AscentGrip3")),"controlled pre-mount pose settles on actual original Grip3");
            await ClawTransfer(4,Key.D,4436);
            if(candidate)
            {
                var floor=Solid("PendulumResidentFloor");await Stance(4868,80,Solid("AscentGrip4"),"bay_jump_stance");
                KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);await Until(()=>FullOn(floor),120,"new_bay_single_jump");
                KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,false);for(int i=0;i<8;i++)await Step();
                TestAssert.True(FullOn(floor)&&motor.AcceptedJumpCount==1,"new bay reached by one ordinary jump with full foot support");
                await Stance(5028,40,floor,"bay_interior_home");
                TestAssert.True(player.GlobalPosition.X>=5012&&player.GlobalPosition.X<=5076,"same full Player must actually enter beneath the alcove roof, not only land on its outer lip: "+player.GlobalPosition);
                GD.Print($"RESIDENT_PENDULUM_INTERIOR_OK same_player=true position={player.GlobalPosition} fully_under_roof=true full_floor=true resident_removed=true pose_writes_after_mount=0");
                KeyboardActionRuntimeTests.KeyEvent(Key.A,true);await Until(()=>player.GlobalPosition.X<=4840,120,"bay_return_left");KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
                int brake=0;do{await Step();brake++;}while(player.Velocity.X<0&&brake<30);KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
                await Until(()=>FullOn(Solid("AscentGrip4")),120,"original_Grip4_return");for(int i=0;i<8;i++)await Step();
            }
            await ClawTransfer(5,Key.A,4724);
            TestAssert.True(arrivals.Count==2&&cancels.Count==2&&motor.AcceptedJumpCount==(candidate?1:0)&&motor.AcceptedDashCount==0&&!boost&&health.CurrentHealth==hp&&healed==0,"same Player two original real arrivals/cancels and exact ordinary jump accounting");
            GD.Print($"RESIDENT_PENDULUM_PLAYER_OK canonical={!candidate} same_player=true casts=2 arrivals=2 cancels=2 jumps={motor.AcceptedJumpCount} dashes=0 boosts=0 health={hp}->{health.CurrentHealth} healed=0 samples={samples} physics_samples={physics} position_writes_after_mount=0 whole_body_hazards_excluded=true original_transfer_cap=300 normal_world=false");
        }
        finally {foreach(var key in new[]{Key.A,Key.D,Key.W,Key.K,Key.Space})KeyboardActionRuntimeTests.KeyEvent(key,false);if(observer!=null&&GodotObject.IsInstanceValid(observer))observer.Free();if(GodotObject.IsInstanceValid(player))player.Free();}
    }
}
