using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class CanyonResidentPlacementTests
{
    // Space/motion stage, not publication, occupied combat, or a Player-access certificate.
    public static async Task RunArchiveCandidate(SceneTree tree,string storage)
    {
        string baseline=(await CaptureGearOriginalBaseline(tree)).Environment;
        string leaves=ResidentSceneBindingTests.BuildCandidatePackage(storage);
        var paths=SaveGearMap(storage);var fixture=new NativeFixture(tree);using var source=new CensusLeafSource(leaves);
        var originals=new Dictionary<string,CharacterBody2D>(StringComparer.Ordinal);
        var mounted=new List<CharacterBody2D>();
        try
        {
            fixture.Initialize();fixture.MountSavedSectors(paths);await fixture.Sync();fixture.StopCallbacks();
            var environment=ResidentEnvironmentInventory.Capture(fixture.Scope);
            TestAssert.True(environment.Issues.Count==0&&GearEnvironmentKey(environment)==baseline,"Archive preserves all original potential hazards/protection");
            CharacterBody2D Original(string path){if(!originals.TryGetValue(path,out var actor)){actor=GD.Load<PackedScene>(path).Instantiate<CharacterBody2D>();originals.Add(path,actor);}return actor;}
            var old=OriginalSpawnReservations(Original);TestAssert.Equal(36,old.Count,"complete original primary/alternate body records");
            var keep=environment.Regions.Concat(old).ToArray();
            var route=CanyonWorldLayout.Nodes.Single(x=>x.Id==CanyonRouteNodeId.ArchiveSideCave).WorldBounds;
            var slots=Enumerable.Range(1,5).Select(i=>CanyonResidentCatalog.All.Single(x=>x.StableId=="canyon.v2.resident.archive-side-cave."+i.ToString("00"))).ToArray();
            Vector2[][] points={
                new[]{new Vector2(1802,1744),new(1738,1744),new(1866,1744),new(1802,1744)},
                new[]{new Vector2(1856,1216),new(1792,1216),new(1920,1216),new(1856,1216)},
                new[]{new Vector2(1558,1777),new(1526,1777),new(1590,1777),new(1654,1777),new(1462,1777),new(1558,1777)},
                new[]{new Vector2(1656,1200),new(1624,1200),new(1688,1200),new(1656,1392),new(1656,1200)},
                new[]{new Vector2(1505,1231)}
            };
            Rect2[] wantedOccupancy={new(1696,1688,212,112),new(1750,1160,212,112),new(1424,1730,268,70),new(1584,1160,144,272),new(1456,1168,98,116)};
            var failures=new List<string>();
            for(int i=0;i<5;i++)
            {
                var raw=source.Raw(CanyonResidentCatalog.Subtypes.Single(x=>x.Id==slots[i].SubtypeId));Rect2 envelope=default;
                for(int j=0;j<Math.Max(1,points[i].Length-1);j++)
                {
                    var a=points[i][j];var b=points[i][Math.Min(j+1,points[i].Length-1)];
                    var result=ResidentPlacementGeometry.CheckCorridor(raw,new(0,a),new(0,b),fixture.Space,fixture.Scope,route,slots[i].Archetype is ResidentArchetype.Wanderer or ResidentArchetype.Charger or ResidentArchetype.Turret,keep);
                    if(!result.CorridorClear)failures.Add(slots[i].StableId+"/segment"+j+":"+string.Join(",",result.Issues));
                    else envelope=j==0?result.WorldCorridorBounds:envelope.Merge(result.WorldCorridorBounds);
                }
                GD.Print($"RESIDENT_ARCHIVE_CORRIDOR id={slots[i].StableId} subtype={slots[i].SubtypeId} home={points[i][0]} occupancy={envelope.Grow(24)} issues={string.Join(";",failures.Where(x=>x.StartsWith(slots[i].StableId+"/")))}");
                if(!failures.Any(x=>x.StartsWith(slots[i].StableId+"/")))TestAssert.Equal(wantedOccupancy[i],envelope.Grow(24),"literal full actual carrier activity plus24, no shrink");
            }
            TestAssert.True(failures.Count==0,"five actual Archive activity paths need real finite support/clearance and explicit territory: "+string.Join(";",failures));
            for(int i=0;i<5;i++)for(int j=i+1;j<5;j++)TestAssert.True(!wantedOccupancy[i].Intersects(wantedOccupancy[j]),"all five complete activity occupations fit jointly, not individual counts");
            TestAssert.Equal(new Rect2(1280,1176,704,624),route,"explicit finite east alcove territory, not an unbounded world exemption");
            fixture.AssertNativePreconditions(143);
            var descent=fixture.Sectors[PrologueSectorId.CanyonDescent];
            var wanted=new Dictionary<string,Rect2>{{"ArchiveResidentLowerFloor",new(1424,1776,496,24)},{"ArchiveResidentUpperFloor",new(1736,1248,232,24)},{"ArchiveResidentUpperBackWall",new(1944,1100,24,172)}};
            foreach(var pair in wanted)
            {
                var body=descent.GetNode<StaticBody2D>("Gameplay/Collision/"+pair.Key);var shape=body.GetNode<CollisionShape2D>("Collision");
                TestAssert.Equal(pair.Value,shape.GlobalTransform*shape.Shape.GetRect(),"literal native saved Archive structure");
                var skin=descent.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/"+pair.Key);
                TestAssert.True(skin.SkinSize==pair.Value.Size&&skin.GlobalPosition==body.GlobalPosition,"independently replaceable skin aligned to native collision");
                TestAssert.True(old.All(x=>!x.WorldBounds.Intersects(pair.Value)),"new structure does not occupy any original36 bodies");
            }
            var definitions=paths.Select(x=>new PrologueSectorDefinition(x.Key,GD.Load<PackedScene>(x.Value),PrologueCanyonBuildParts.WorldOffsets[x.Key],PrologueCanyonBuildParts.Bounds(x.Key),null,null)).ToArray();
            try
            {
                using var catalog=MinimapTerrainCatalogBuilder.Build(definitions);TestAssert.Equal(143,catalog.SolidCount,"same-five minimap includes exactly three Archive structures");
                foreach(var rectangle in wanted.Values)TestAssert.True(Enumerable.Range(0,catalog.SolidCount).Any(i=>catalog.Solid(i).LocalToWorld*catalog.Solid(i).LocalBounds==rectangle),"literal Archive structure in actual minimap");
            }
            finally{foreach(var definition in definitions)definition.Dispose();}
            int[] indices={0,1,2};float[] low=new float[3],high=new float[3],previous=new float[3],maxStep=new float[3];int[] hp={3,3,2},healed=new int[3];bool[] supported={true,true,true};
            for(int k=0;k<3;k++)
            {
                int i=indices[k],capture=k;var actor=source.Configure(slots[i],points[i],route);TestAssert.Equal(hp[k],actor.GetNode<HealthComponent>("Health").MaxHealth,"actual saved Rust3/Skitter2 before ordinary native Ready");
                actor.GetNode<HealthComponent>("Health").Healed+=(_,_)=>healed[capture]++;
                foreach(var shape in GearDescendants(actor).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))shape.Shape=fixture.Own((Shape2D)shape.Shape.Duplicate());
                var parent=descent.GetNode<Node2D>("Gameplay/Actors");actor.Transform=parent.GlobalTransform.AffineInverse()*new Transform2D(0,points[i][0]);parent.AddChild(actor);mounted.Add(actor);
                low[k]=high[k]=previous[k]=points[i][0].X;
            }
            await fixture.Sync();
            for(int k=0;k<3;k++)previous[k]=mounted[k].GlobalPosition.X;
            for(int tick=0;tick<360;tick++)
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                for(int k=0;k<3;k++)
                {
                    var actor=mounted[k];float x=actor.GlobalPosition.X;low[k]=MathF.Min(low[k],x);high[k]=MathF.Max(high[k],x);maxStep[k]=MathF.Max(maxStep[k],MathF.Abs(x-previous[k]));previous[k]=x;
                    var body=actor.GetNode<CollisionShape2D>("BodyCollider");Rect2 actualBody=body.GlobalTransform*body.Shape.GetRect();
                    if(k==1)TestAssert.True(actualBody.Position.Y>=route.Position.Y+7,"actual native upper64px body has real vertical territory margin, not only nominal support: "+actualBody);
                    supported[k]&=actor.IsOnFloor();TestAssert.True(actor.GetNode<HealthComponent>("Health").CurrentHealth==hp[k]&&healed[k]==0&&MathF.Abs(actor.GlobalPosition.Y-points[indices[k]][0].Y)<.6f,"real Archive ground residents preserve supported pose/HP, no injected repair: "+slots[indices[k]].StableId+" position="+actor.GlobalPosition+" hp="+actor.GetNode<HealthComponent>("Health").CurrentHealth+" healed="+healed[k]);
                }
            }
            for(int k=0;k<3;k++)
            {
                float home=points[indices[k]][0].X,minimum=k==2?26:56;
                GD.Print($"RESIDENT_ARCHIVE_PATROL_OBSERVED id={slots[indices[k]].StableId} observations=360 min={low[k]} max={high[k]} max_step={maxStep[k]} hp={hp[k]} healed={healed[k]} full_floor={supported[k]} final={mounted[k].GlobalPosition} target_present=false attack_tested=false");
            }
            for(int k=0;k<3;k++)
            {
                float home=points[indices[k]][0].X,minimum=k==2?26:56;
                TestAssert.True(supported[k]&&low[k]<=home-minimum&&high[k]>=home+minimum&&maxStep[k]<2&&hp[k]>0&&healed[k]==0,"actual native Archive patrol reaches both directions with continuous full floor support: "+slots[indices[k]].StableId+" min="+low[k]+" max="+high[k]+" supported="+supported[k]);
                GD.Print($"RESIDENT_ARCHIVE_PATROL_OK id={slots[indices[k]].StableId} observations=360 min={low[k]} max={high[k]} max_step={maxStep[k]} hp={hp[k]} healed=0 full_floor=true target_present=false attack_tested=false");
            }
            foreach(var actor in mounted)actor.Free();mounted.Clear();await fixture.Sync();
            await RunArchivePlayer(tree,fixture,environment);
            TestAssert.Equal(baseline,GearEnvironmentKey(ResidentEnvironmentInventory.Capture(fixture.Scope)),"all original environment survives actual motion");
            GD.Print("RESIDENT_ARCHIVE_CANDIDATE_OK native_terrain=143 saved_sectors=5 joint_archive_slots=5 actual_ground_patrols=3 observations_each=360 upper_player_visit=true lower_player_access=true attack_tested=false canonical_changed=false formal_spawned=0 normal_world=false publication=false");
        }
        finally{foreach(var actor in mounted)if(GodotObject.IsInstanceValid(actor))actor.Free();foreach(var actor in originals.Values)actor.Free();await fixture.DisposeAsync();}
    }

    private static async Task RunArchivePlayer(SceneTree tree,NativeFixture fixture,ResidentEnvironmentSnapshot environment)
    {
        ((SubViewport)fixture.Scope.GetViewport()).Size=new(1280,720);
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();ResidentSteamPhysicsObserver observer=null;
        try
        {
            player.Position=new(1556,1224);player.AddToGroup("player");fixture.Scope.AddChild(player);
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");var health=player.GetNode<HealthComponent>("Health");
            var collider=player.GetNode<CollisionShape2D>("BodyCollider");ulong identity=player.GetInstanceId(),sequence=input.CurrentFrameSequence,transferStart=0,lastInput=0;int samples=0,physics=0,edges=0,healed=0;bool allowBoost=false,neutralDrop=false;Exception failure=null;
            var arrivals=new HashSet<(ulong Cast,ulong Target)>();var cancels=new HashSet<ulong>();var boosts=new HashSet<ulong>();health.Healed+=(_,_)=>healed++;
            MechanicalClawState lastState=MechanicalClawState.Idle;MechanicalClawFailure lastFailure=MechanicalClawFailure.None;
            MechanicalClawFrame diagnosticPrevious=claw.CurrentFrame;GrappleSurface2D diagnosticTarget=null;
            claw.ClawFrameChanged+=(frame,target)=>
            {
                if(frame.State!=lastState||frame.Failure!=lastFailure||frame.ReleaseKind!=MechanicalClawReleaseKind.None)GD.Print($"RESIDENT_ARCHIVE_PLAYER_CLAW physics={Engine.GetPhysicsFrames()} state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} cast={frame.CastId} target={target?.GetPath()} anchor={frame.AnchorPoint} dock={frame.DockPoint} position={player.GlobalPosition} velocity={player.Velocity} correction={frame.VelocityCorrection}");
                if(frame.Failure==MechanicalClawFailure.Obstructed&&diagnosticPrevious.State==MechanicalClawState.Reel&&diagnosticTarget!=null)
                {
                    try
                    {
                        var prior=diagnosticPrevious with {CasterWorldPosition=player.GlobalPosition,CasterVelocity=player.Velocity};
                        var snapshot=new MechanicalClawTargetResolver().SnapshotLocked(player,diagnosticTarget,prior.AnchorPoint,prior.SurfaceNormal);
                        bool near=MechanicalClawArrivalGeometry.IsArrived(player.GlobalPosition,prior.DockPoint,MechanicalClawSettings.Default.ArrivalTolerance);
                        float seconds=(float)(Engine.TimeScale/Engine.PhysicsTicksPerSecond);
                        var predicted=new MechanicalClawModel().Step(prior,new(MechanicalClawCommand.None,input.CurrentFrame.MoveAxis,near,prior.DockPoint),snapshot,seconds);
                        Vector2 motion=(player.Velocity+predicted.VelocityCorrection-prior.VelocityCorrection)*seconds;
                        using var hit=new KinematicCollision2D();bool blocked=player.TestMove(player.GlobalTransform,motion,hit,player.SafeMargin,false);
                        var contacts=new List<string>();for(int i=0;i<player.GetSlideCollisionCount();i++){var contact=player.GetSlideCollision(i);contacts.Add((contact.GetCollider() as Node)?.GetPath()+":"+contact.GetNormal());}
                        GD.Print($"RESIDENT_ARCHIVE_OBSTRUCTION_WITNESS physics={Engine.GetPhysicsFrames()} diagnostic_reconstruction=true controller_internal_capture=false los={snapshot.HasLineOfSight} near_dock={near} predicted_state={predicted.State} motion={motion} test_move_blocked={blocked} collider={(blocked?(hit.GetCollider() as Node)?.GetPath():null)} normal={(blocked?hit.GetNormal():Vector2.Zero)} travel={(blocked?hit.GetTravel():Vector2.Zero)} target={diagnosticTarget.GetPath()} saved_normal={prior.SurfaceNormal} position={player.GlobalPosition} velocity={player.Velocity} previous_contacts={string.Join(";",contacts)}");
                    }
                    catch(Exception error){failure??=error;}
                }
                diagnosticPrevious=frame;diagnosticTarget=target;
                lastState=frame.State;lastFailure=frame.Failure;if(frame.ReleaseKind==MechanicalClawReleaseKind.Boost&&frame.CastId!=0)boosts.Add(frame.CastId);
                if(frame.ReleaseKind==MechanicalClawReleaseKind.Cancel&&frame.CastId!=0)cancels.Add(frame.CastId);if(frame.Arrived&&frame.State==MechanicalClawState.Swing&&frame.Failure==MechanicalClawFailure.None&&target!=null&&frame.LockedTargetRuntimeId==target.GetInstanceId())arrivals.Add((frame.CastId,target.GetInstanceId()));
            };
            var danger=environment.Regions.Where(x=>x.Kind is ResidentKeepOutKind.Hazard or ResidentKeepOutKind.MovingOccupancy).ToArray();
            var sector=fixture.Sectors[PrologueSectorId.CanyonDescent];StaticBody2D Solid(string name)=>sector.GetNode<StaticBody2D>("Gameplay/Collision/"+name);
            Rect2 Body()=>collider.GlobalTransform*collider.Shape.GetRect();
            bool On(StaticBody2D floor){if(!player.IsOnFloor())return false;for(int i=0;i<player.GetSlideCollisionCount();i++){using var h=player.GetSlideCollision(i);if(h.GetCollider()==floor&&h.GetNormal().Dot(Vector2.Up)>.99f)return true;}return false;}
            bool FullOn(StaticBody2D floor)
            {
                if(!On(floor))return false;var c=floor.GetNode<CollisionShape2D>("Collision");Rect2 plate=c.GlobalTransform*c.Shape.GetRect(),body=Body();
                if(body.Position.X<plate.Position.X||body.End.X>plate.End.X||MathF.Abs(body.End.Y-plate.Position.Y)>1)return false;
                foreach(float x in new[]{body.Position.X+1,body.GetCenter().X,body.End.X-1}){using var q=PhysicsRayQueryParameters2D.Create(new(x,body.End.Y-.25f),new(x,body.End.Y+1),CombatCollisionLayers.World);q.CollideWithAreas=false;using var hit=fixture.Space.IntersectRay(q);if(hit.Count==0||hit["collider"].AsGodotObject()!=floor||hit["normal"].AsVector2().Y>-.99f)return false;}return true;
            }
            void Safety()
            {
                physics++;if(input.CurrentFrameSequence>lastInput){lastInput=input.CurrentFrameSequence;if(input.CurrentFrame.ClawPressed)edges++;}
                TestAssert.True(!neutralDrop||input.CurrentFrame.MoveAxis==0&&!input.CurrentFrame.JumpPressed&&!input.CurrentFrame.ClawPressed&&!input.CurrentFrame.DashPressed,"every actual neutral drop physics sample has zero movement/new ability input");
                TestAssert.True(player.GetInstanceId()==identity&&health.CurrentHealth==5&&healed==0&&boosts.Count<=(allowBoost?1:0)&&motor.AcceptedJumpCount<=1&&motor.AcceptedDashCount==0&&collider.Shape is CapsuleShape2D capsule&&capsule.Radius==20&&capsule.Height==72&&player.GlobalPosition.IsFinite()&&player.Velocity.IsFinite()&&player.GlobalPosition.X is >1328 and <1984&&player.GlobalPosition.Y is >1100 and <2200&&danger.All(x=>!x.WorldBounds.Intersects(Body())),"actual Archive Player keeps full body/HP/one ordinary jump/only the designated lower boost/all potential dangers: "+player.GlobalPosition);
                if(transferStart!=0)TestAssert.True(Engine.GetPhysicsFrames()-transferStart<=300,"whole actual claw transfer retains native300 cap");
            }
            observer=new ResidentSteamPhysicsObserver{ProcessPhysicsPriority=100,Sample=()=>{try{Safety();}catch(Exception e){failure??=e;}}};fixture.Scope.AddChild(observer);
            async Task Step(){await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);samples++;TestAssert.True(failure==null,"every-physics upper Archive safety: "+failure);TestAssert.True(input.CurrentFrameSequence>sequence,"fresh actual upper Archive keyboard sample");sequence=input.CurrentFrameSequence;}
            async Task Until(Func<bool> done,int cap,string stage)
            {
                ulong start=Engine.GetPhysicsFrames();int i=0;for(;i<cap&&!done();i++){await Step();TestAssert.True(Engine.GetPhysicsFrames()-start<=(ulong)cap,"actual native stage cap: "+stage);}
                GD.Print($"RESIDENT_ARCHIVE_PLAYER_STAGE stage={stage} samples={i}/{cap} physics={Engine.GetPhysicsFrames()-start} position={player.GlobalPosition} velocity={player.Velocity} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure} target={claw.CurrentTarget?.GetPath()}");TestAssert.True(done(),"actual upper Archive Player stage failed: "+stage);
            }
            async Task Transfer(StaticBody2D wall,StaticBody2D landing,string stage)
            {
                var target=wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");Vector2 direction=Vector2.Right;
                var resolution=new MechanicalClawTargetResolver().ResolveKeyboard(player,direction);GD.Print($"RESIDENT_ARCHIVE_PLAYER_PREFLIGHT stage={stage} player={player.GlobalPosition} velocity={player.Velocity} target={resolution.Surface?.GetPath()} anchor={resolution.Snapshot.WorldAnchor} normal={resolution.Snapshot.SurfaceNormal}");
                TestAssert.True(resolution.Surface==target&&resolution.Snapshot.HasLineOfSight&&resolution.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.999f&&resolution.Snapshot.WorldAnchor.Y is >=1148 and <=1196,"actual resolver selects existing upper back-wall left face at a full-body safe height");
                var settings=MechanicalClawSettings.Default;Vector2 dock=MechanicalClawArrivalGeometry.DockPoint(resolution.Snapshot.WorldAnchor,resolution.Snapshot.SurfaceNormal,(CapsuleShape2D)collider.Shape,settings.MinimumRopeLength,settings.ArrivalMargin);
                var atDock=collider.GlobalTransform;atDock.Origin+=dock-player.GlobalPosition;
                using(var query=new PhysicsShapeQueryParameters2D {Shape=collider.Shape,Transform=atDock,CollisionMask=CombatCollisionLayers.World,CollideWithBodies=true,CollideWithAreas=false})
                {using Godot.Collections.Array hits=(Godot.Collections.Array)fixture.Space.IntersectShape(query,64);TestAssert.True(hits.Count==0,"actual complete unchanged capsule is clear at the selected wall dock before K");}
                ulong cast=claw.CurrentFrame.NextCastId;int before=edges;
                KeyboardActionRuntimeTests.KeyEvent(Key.D,true);KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
                await Until(()=>edges==before+1&&claw.CurrentTarget==target&&claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel,8,stage+"_admit");
                TestAssert.True(claw.CurrentFrame.State==MechanicalClawState.Aim?claw.CurrentFrame.CastId==0&&claw.CurrentFrame.NextCastId==cast:claw.CurrentFrame.CastId==cast&&claw.CurrentFrame.NextCastId==cast+1,"fresh sampled K has one real next-cast transition");
                foreach(var key in new[]{Key.D,Key.W,Key.S,Key.K})KeyboardActionRuntimeTests.KeyEvent(key,false);
                await Until(()=>arrivals.Contains((cast,target.GetInstanceId()))&&claw.CurrentTarget==target&&claw.CurrentFrame.CastId==cast&&claw.CurrentFrame.State==MechanicalClawState.Swing,150,stage+"_arrive");
                KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);await Until(()=>cancels.Contains(cast)&&claw.CurrentFrame.State==MechanicalClawState.Idle,8,stage+"_cancel");KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
                await Until(()=>FullOn(landing),120,stage+"_full_landing");
                TestAssert.True(Engine.GetPhysicsFrames()-transferStart<=300,"shared original native cap includes ordinary jump/admission/arrival/cancel/landing");transferStart=0;
            }
            async Task WalkHome(StaticBody2D floor,float x,int walkCap=80)
            {
                ulong start=Engine.GetPhysicsFrames();int axis=Math.Sign(x-player.GlobalPosition.X);Key key=axis<0?Key.A:Key.D;KeyboardActionRuntimeTests.KeyEvent(key,true);int i=0;
                for(;i<walkCap;i++){var forecast=FreeMotionForecast.Neutral(player.Velocity.X,true,motor.MotionSettings,motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,12,2);TestAssert.True(forecast.SpeedThresholdReached&&FullOn(floor),"interior walk uses physical full support/stopping forecast");if(axis*(x-player.GlobalPosition.X)<=MathF.Max(0,axis*forecast.NetDistance))break;await Step();}
                KeyboardActionRuntimeTests.KeyEvent(key,false);ulong released=input.CurrentFrameSequence;await Until(()=>input.CurrentFrameSequence>released&&input.CurrentFrame.MoveAxis==0&&MathF.Abs(player.Velocity.X)<=2,12,"upper_interior_brake");
                TestAssert.True(FullOn(floor)&&MathF.Abs(player.GlobalPosition.X-x)<=4&&Engine.GetPhysicsFrames()-start<=(ulong)(walkCap+12),"same Player really enters complete named interior within native walk/brake cap");
            }
            async Task DropLeft(StaticBody2D from,StaticBody2D to,bool iceExit)
            {
                var a=from.GetNode<CollisionShape2D>("Collision");var b=to.GetNode<CollisionShape2D>("Collision");Rect2 source=a.GlobalTransform*a.Shape.GetRect(),target=b.GlobalTransform*b.Shape.GetRect();
                float minimum=target.Position.X+20,maximum=MathF.Min(target.End.X-20,iceExit?1380:float.PositiveInfinity),standing=target.Position.Y-36;double dt=1d/Engine.PhysicsTicksPerSecond;
                float StopX(Vector2 p,Vector2 v){for(int i=0;i<120;i++){v=ThrusterDynamics.Step(new(v,0,false,Vector2.Zero,0),motor.ThrusterState.Module,motor.MotionSettings,dt).Velocity;p+=v*(float)dt;if(v.Y>=0&&p.Y>=standing){var tail=FreeMotionForecast.Neutral(v.X,true,motor.MotionSettings,motor.ThrusterState.Module,dt,60,.5f);return tail.SpeedThresholdReached?p.X+tail.NetDistance:float.NaN;}}return float.NaN;}
                TestAssert.True(FullOn(from),"drop begins on real full source support");int departure=0;ulong departureStart=Engine.GetPhysicsFrames();
                while(departure<120&&player.IsOnFloor())
                {
                    Vector2 p=player.GlobalPosition,v=player.Velocity;for(int n=0;n<2;n++){v=ThrusterDynamics.Step(new(v,-1,true,Vector2.Zero,0),motor.ThrusterState.Module,motor.MotionSettings,dt).Velocity;p+=v*(float)dt;}
                    float stop=StopX(p,v);bool left=float.IsFinite(stop)&&stop>=minimum&&(stop<=maximum||p.X>=source.Position.X);
                    KeyboardActionRuntimeTests.KeyEvent(Key.A,left);await Step();departure++;TestAssert.True(Engine.GetPhysicsFrames()-departureStart<=120,"drop departure retains native120 cap, not only observation count");
                }
                KeyboardActionRuntimeTests.KeyEvent(Key.A,false);ulong released=input.CurrentFrameSequence;await Until(()=>input.CurrentFrameSequence>released&&input.CurrentFrame.MoveAxis==0,3,"drop_sampled_release");neutralDrop=true;
                float expected=StopX(player.GlobalPosition,player.Velocity);TestAssert.True(!player.IsOnFloor()&&expected>=minimum&&expected<=maximum,"physical release forecasts complete neutral support: "+from.Name+" -> "+to.Name+" stop="+expected);
                await Until(()=>On(to),120,"drop_"+to.Name);await Until(()=>FullOn(to)&&MathF.Abs(player.Velocity.X)<=.5f,60,"stop_"+to.Name);
                if(from.Name=="ArchiveShelf"&&to.Name=="ArchiveFloor")TestAssert.Near(expected,player.GlobalPosition.X,.03f,"unchanged unobstructed Archive drop prediction");
                if(iceExit)TestAssert.True(Body().End.X<=1400,"old left Frozen slot keeps full capsule outside ice/new lower slab");
                neutralDrop=false;
            }
            async Task LowerVisit()
            {
                var lower=Solid("ArchiveResidentLowerFloor");var target=lower.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
                TestAssert.True(FullOn(Solid("ArchiveFloor"))&&boosts.Count==0&&motor.AcceptedJumpCount==1,"lower starts on same Player's actually reached original floor, not an air spawn");
                transferStart=Engine.GetPhysicsFrames();var resolver=new MechanicalClawTargetResolver();Vector2 aim=new Vector2(1,1).Normalized();
                bool Selected()
                {
                    var r=resolver.ResolveKeyboard(player,aim);
                    return r.Surface==target&&r.Snapshot.HasLineOfSight&&r.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.999f&&MathF.Abs(r.Snapshot.WorldAnchor.X-1424)<.1f&&r.Snapshot.WorldAnchor.Y is >=1778 and <=1798&&player.GlobalPosition.X is >=1348 and <=1356&&MathF.Abs(player.Velocity.X)<=80;
                }
                int steps=0;ulong approach=Engine.GetPhysicsFrames();
                while(steps<90&&!Selected())
                {
                    float x=player.GlobalPosition.X,vx=player.Velocity.X;int axis=0;
                    if(x>1356||vx< -2)
                    {var stop=FreeMotionForecast.Reverse(vx,player.IsOnFloor(),motor.MotionSettings,motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,60,.5f);TestAssert.True(stop.SpeedThresholdReached||stop.BrakeReachedZero,"finite existing reverse forecast selects actual keys only");axis=x+stop.NetDistance>1352?-1:1;}
                    else if(x<1348)axis=1;else if(vx>2)axis=-1;
                    KeyboardActionRuntimeTests.KeyEvent(Key.A,axis<0);KeyboardActionRuntimeTests.KeyEvent(Key.D,axis>0);await Step();steps++;
                    if(steps%4==0){var r=resolver.ResolveKeyboard(player,aim);GD.Print($"RESIDENT_ARCHIVE_LOWER_APPROACH physics={Engine.GetPhysicsFrames()} position={player.GlobalPosition} velocity={player.Velocity} axis={input.CurrentFrame.MoveAxis} floor={player.IsOnFloor()} target={r.Surface?.GetPath()} anchor={r.Snapshot.WorldAnchor} normal={r.Snapshot.SurfaceNormal}");}
                    TestAssert.True(Engine.GetPhysicsFrames()-approach<=90,"lower departure/preview uses native90 cap");
                }
                var preview=resolver.ResolveKeyboard(player,aim);GD.Print($"RESIDENT_ARCHIVE_LOWER_PREFLIGHT position={player.GlobalPosition} velocity={player.Velocity} target={preview.Surface?.GetPath()} anchor={preview.Snapshot.WorldAnchor} normal={preview.Snapshot.SurfaceNormal}");
                TestAssert.True(Selected(),"actual lower-left face is selectable from physically reached narrow slot before K");
                Vector2 dock=MechanicalClawArrivalGeometry.DockPoint(preview.Snapshot.WorldAnchor,preview.Snapshot.SurfaceNormal,(CapsuleShape2D)collider.Shape,48,4);
                var atDock=collider.GlobalTransform;atDock.Origin+=dock-player.GlobalPosition;
                using(var q=new PhysicsShapeQueryParameters2D{Shape=collider.Shape,Transform=atDock,CollisionMask=CombatCollisionLayers.World,CollideWithBodies=true,CollideWithAreas=false})
                {using Godot.Collections.Array hits=(Godot.Collections.Array)fixture.Space.IntersectShape(q,64);TestAssert.True(hits.Count==0&&dock.X==1376,"complete actual lower dock capsule has native clearance, including old ice");}
                ulong cast=claw.CurrentFrame.NextCastId;int before=edges;
                KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);KeyboardActionRuntimeTests.KeyEvent(Key.S,true);KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
                await Until(()=>edges==before+1&&claw.CurrentTarget==target&&claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel,8,"lower_side_admit");
                TestAssert.True(claw.CurrentFrame.SurfaceNormal.Dot(Vector2.Left)>.999f&&claw.CurrentFrame.AnchorPoint.Y is >=1778 and <=1798,"sampled K really locks the preflight left face, not another edge");
                foreach(var key in new[]{Key.D,Key.S,Key.K})KeyboardActionRuntimeTests.KeyEvent(key,false);
                KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
                await Until(()=>input.CurrentFrame.MoveAxis<-.5f&&player.Velocity.X<=0,8,"lower_cast_reverse_drift");
                KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
                await Until(()=>arrivals.Contains((cast,target.GetInstanceId()))&&claw.CurrentTarget==target&&claw.CurrentFrame.CastId==cast&&claw.CurrentFrame.State==MechanicalClawState.Swing,150,"lower_side_arrived");
                TestAssert.True(Body().End.X<=1400&&motor.AcceptedJumpCount==1,"same actual lower arrival keeps entire body west of Ice, no extra ordinary jump");
                allowBoost=true;KeyboardActionRuntimeTests.KeyEvent(Key.A,true);KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
                await Until(()=>boosts.Contains(cast),8,"lower_side_attached_boost");
                KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
                await Until(()=>FullOn(lower),120,"lower_full_landing");KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
                TestAssert.True(Engine.GetPhysicsFrames()-transferStart<=300&&boosts.SetEquals(new[]{cast}),"one real matched lower boost lands full body within original sharednative300");transferStart=0;
                await WalkHome(lower,1802,120);
                GD.Print($"RESIDENT_ARCHIVE_LOWER_INTERIOR_OK same_player=true position={player.GlobalPosition} full_floor=true lower_cast={cast} lower_boosts=1 ordinary_jumps_total=1 pose_writes_after_mount=0");
                await DropLeft(lower,Solid("FrozenLeftCatch"),true);
            }
            for(int i=0;i<10;i++)await Step();TestAssert.True(FullOn(Solid("ArchiveEntry")),"controlled original Entry initial pose settles on full native support");
            transferStart=Engine.GetPhysicsFrames();KeyboardActionRuntimeTests.KeyEvent(Key.D,true);KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            for(int i=0;i<3;i++)await Step();
            TestAssert.True(motor.AcceptedJumpCount==1&&player.Velocity.Y<0&&Body().End.Y<=1232&&Engine.GetPhysicsFrames()-transferStart<=8,"three actual fresh jump samples establish safe airborne wall access, no post-mount pose writes");
            await Transfer(Solid("ArchiveResidentUpperBackWall"),Solid("ArchiveResidentUpperFloor"),"jump_to_existing_backwall");
            await WalkHome(Solid("ArchiveResidentUpperFloor"),1856);
            GD.Print($"RESIDENT_ARCHIVE_UPPER_INTERIOR_OK same_player=true position={player.GlobalPosition} full_floor=true residents_removed=true pose_writes_after_mount=0");
            await DropLeft(Solid("ArchiveResidentUpperFloor"),Solid("ArchiveShelf"),false);
            await DropLeft(Solid("ArchiveShelf"),Solid("ArchiveFloor"),false);
            await LowerVisit();
            TestAssert.True(arrivals.Count==2&&cancels.Count==1&&edges==3&&boosts.Count==1&&motor.AcceptedJumpCount==1&&FullOn(Solid("FrozenLeftCatch")),"same living Player actually visits both Archive layers and reaches original supported Frozen exit with exact existing actions");
            GD.Print($"RESIDENT_ARCHIVE_PLAYER_ROUTE_OK same_player=true start=ArchiveEntry upper_interior=true lower_interior=true egress=FrozenLeftCatch casts=2 arrivals=2 cancels=1 jumps=1 dashes=0 boosts=1 healed=0 health=5->5 samples={samples} physics_samples={physics} position_writes_after_mount=0 whole_body_hazards_excluded=true normal_world=false");
        }
        finally{foreach(var key in new[]{Key.A,Key.D,Key.W,Key.S,Key.K,Key.Space})KeyboardActionRuntimeTests.KeyEvent(key,false);if(GodotObject.IsInstanceValid(observer))observer.Free();if(GodotObject.IsInstanceValid(player))player.Free();}
    }
}
