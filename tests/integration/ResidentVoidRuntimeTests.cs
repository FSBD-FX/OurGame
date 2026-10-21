using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.World;

namespace Knight.Tests.Integration;

// Missing actual void contact, home+y shortcuts, endpoint-only tests and
// losing a rejected fatal request must fail against real saved Rust physics.
public static class ResidentVoidRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("default_wave_does_not_gain_npc_void_policy",async f=>
        {
            f.Void(new(400,300,260,16));await f.Mount(false);
            f.Actor.ApplyThreadPull(new(1500,0));await f.Frames(90);
            TestAssert.True(f.Actor.GlobalPosition.Y>700&&f.Health.CurrentHealth==3,
                "unconfigured old Rust falls without new resident policy");
        });
        await Check("far_lower_ordinary_floor_survives",async f=>
        {
            f.Floor(400,260,520);await f.Mount();f.Actor.ApplyThreadPull(new(1500,0));
            await f.Until(()=>f.Actor.IsOnFloor()&&f.Actor.GlobalPosition.Y>450,100);
            TestAssert.True(f.Actor.GlobalPosition.X>400&&f.Actor.GlobalPosition.Y>480&&f.Actor.IsOnFloor(),
                "actual force leaves ledge and lands392px lower, no position write/return teleport");
            TestAssert.Equal(3,f.Health.CurrentHealth,"ordinary >240px fall is not a fabricated void");
        });
        await Check("real_pull_fall_through_void_dies_once",async f=>
        {
            f.Void(new(400,300,260,16));await f.Mount();int died=0,defeated=0;
            f.Health.Died+=()=>died++;f.Actor.Defeated+=()=>defeated++;
            f.Actor.ApplyThreadPull(new(1500,0));await f.Until(()=>f.Health.CurrentHealth==0,90);
            TestAssert.True(f.Actor.GlobalPosition.X>400&&f.Actor.GlobalPosition.Y>200,
                "real external force/gravity reaches actual boundary, not territory-only kill");
            TestAssert.Equal(0,f.Health.CurrentHealth,"actual whole-body void contact reaches real Health death");
            await f.Frames(30);
            TestAssert.True(died==1&&defeated==1&&f.Actor.CurrentState==RustShellState.Dead,
                "one real Health Died/Defeated, no parallel death notifications");
            TestAssert.Equal(0,f.RecoveryRequests,"NPC does not call player recovery");
        });
        await Check("disabled_void_shape_does_not_kill",async f=>
        {
            f.Void(new(400,300,260,16),true);f.Floor(400,260,520);await f.Mount();
            f.Actor.ApplyThreadPull(new(1500,0));await f.Frames(100);
            TestAssert.True(f.Actor.GlobalPosition.Y>480&&f.Actor.IsOnFloor()&&f.Health.CurrentHealth==3,
                "disabled real shape is not a fatal potential reservation");
        });
        await Check("same_coordinates_in_other_world_do_not_kill",async f=>
        {
            var other=new ResidentPatrolRuntimeTests.Fixture(tree);
            try
            {
                other.Initialize(false);other.Void(new(400,300,260,16));f.Floor(400,260,520);
                await f.Mount();f.Actor.ApplyThreadPull(new(1500,0));await f.Frames(100);
                TestAssert.True(f.Actor.GetWorld2D()!=other.Scope.GetWorld2D()&&f.Actor.IsOnFloor()&&f.Health.CurrentHealth==3,
                    "foreign physical world cannot supply a fatal boundary");
            }
            finally {await other.DisposeAsync();}
        });
        await Check("initial_whole_body_overlap_is_fatal",async f=>
        {
            await f.Mount();f.Void(new(300,80,40,16));await f.Frames(2);
            TestAssert.Equal(0,f.Health.CurrentHealth,"actual initial body overlap is detected even without falling motion");
        });
        await Check("thin_boundary_between_clear_endpoints_is_fatal",async f=>
        {
            await f.Mount();f.PrimaryFloor.QueueFree();await f.Frames(2);
            f.Void(new(0,200,640,16));var body=f.Actor.GetNode<CollisionShape2D>("BodyCollider");
            Rect2 before=body.GlobalTransform*body.Shape.GetRect();
            TestAssert.True(before.End.Y<200,"actual start body is above16px boundary");
            f.Actor.ApplyThreadPull(new(0,10000));await f.Frames(1);
            Rect2 after=body.GlobalTransform*body.Shape.GetRect();
            TestAssert.True(after.Position.Y>216,"actual end body is below16px boundary, endpoint sampling cannot pass");
            TestAssert.Equal(0,f.Health.CurrentHealth,"actual whole-body motion sweep catches intermediate boundary");
        });
        await Check("invulnerable_contact_latches_across_sleep_and_source_removal",async f=>
        {
            await f.Mount();int died=0,defeated=0;
            f.Health.Died+=()=>died++;f.Actor.Defeated+=()=>defeated++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1,Vector2.Zero,0,5001,9001)),
                "real nonlethal damage opens real invulnerability window");
            KillZone zone=f.Void(new(280,64,80,64));await f.Frames(1);
            TestAssert.True(f.Health.CurrentHealth==2&&died==0,"fatal contact does not bypass actual invulnerability");
            f.Actor.SetResidentDormant(true);zone.QueueFree();await f.Frames(30);
            TestAssert.True(f.Health.CurrentHealth==0&&died==1&&defeated==1,
                "accepted contact persists after source removal/sleep until actual Health death exactly once");
            TestAssert.Equal(0,f.RecoveryRequests,"latched NPC fatality never restores player");
        });
        await Check("existing_player_void_still_requests_recovery",async f=>
        {
            f.Void(new(0,300,640,16));f.Player(100);await f.Mount();
            TestAssert.Equal(0,f.RecoveryRequests,"no premature player recovery");
            f.Target.MoveAndCollide(new(0,210));await f.Frames(3);
            TestAssert.True(f.RecoveryRequests==1&&f.PlayerHealth.CurrentHealth==9&&f.Health.CurrentHealth==3,
                "actual player Area entry keeps existing one recovery callback, no NPC death or HP cost");
        });
        await Check("queued_source_before_contact_has_no_fatal_authority",async f=>
        {
            await f.Mount();KillZone zone=f.Void(new(280,64,80,64));zone.QueueFree();await f.Frames(40);
            TestAssert.True(f.Health.CurrentHealth==3&&!GodotObject.IsInstanceValid(zone),
                "queued/removed source cannot start a fatal request merely because a wrapper exists");
        });
        await Check("sleeping_airborne_resident_still_obeys_actual_void",async f=>
        {
            f.Void(new(400,300,260,16));await f.Mount();f.Actor.ApplyThreadPull(new(1500,0));
            await f.Until(()=>f.Actor.GlobalPosition.X>405,20);
            TestAssert.True(f.Actor.GlobalPosition.X>405&&f.Health.CurrentHealth==3,"real pull leaves ledge before void contact");
            f.Actor.SetResidentDormant(true);await f.Until(()=>f.Health.CurrentHealth==0,90);
            TestAssert.Equal(0,f.Health.CurrentHealth,"sleep withdraws player damage authority, not environmental fatal contact");
        });
        await Check("pending_actor_unload_is_not_defeat_or_replacement_fatality",async f=>
        {
            await f.Mount();int defeated=0;f.Actor.Defeated+=()=>defeated++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1,Vector2.Zero,0,5002,9001)),"real damage before pending unload");
            KillZone zone=f.Void(new(280,64,80,64));await f.Frames(1);
            TestAssert.Equal(2,f.Health.CurrentHealth,"fatal contact is waiting through real invulnerability");
            zone.QueueFree();f.DetachActor();
            var replacement=GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
            replacement.Name=f.Actor.Name;replacement.Position=new(320,96);
            replacement.ConfigureResidence(ResidentPatrolRuntimeTests.Definition(320,256,384));
            f.Scope.AddChild(replacement);await f.Frames(40);
            TestAssert.True(!f.Actor.IsInsideTree()&&f.Health.CurrentHealth==2&&defeated==0&&
                replacement.GetNode<HealthComponent>("Health").CurrentHealth==3&&replacement.IsOnFloor(),
                "old pending instance unload is not a kill and cannot kill same-name native replacement");
        });
        await SavedFall(1860,false,"saved_descent_seam_hole_lands_in_basin_alive");
        await SavedFall(1450,true,"saved_descent_real_recovery_strip_is_npc_fatal");
        GD.Print($"RESIDENT_VOID_NATIVE_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));

        async Task Check(string name,Func<ResidentPatrolRuntimeTests.Fixture,Task> test)
        {
            cases++;var f=new ResidentPatrolRuntimeTests.Fixture(tree);
            try {f.Initialize(false);f.Floor(280,80);await test(f);GD.Print("RESIDENT_VOID_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_VOID_CASE_FAIL "+name+": "+error.Message);}
            finally {await f.DisposeAsync();}
        }
        async Task SavedFall(float x,bool fatal,string name)
        {
            cases++;var f=new CanyonResidentPlacementTests.NativeFixture(tree);
            try
            {
                f.Initialize();f.MountSavedSectors();await f.Sync();f.AssertNativePreconditions();
                var actor=GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
                var home=fatal?new Vector2(x,3520):new Vector2(1820,3248);
                actor.Position=home-new Vector2(1280,0);
                actor.ConfigureResidence(new ResidentPopulationDefinition("native-test.saved-void","native-test.rust",
                    CanyonRouteNodeId.ControlledAbyss,PrologueSectorId.CanyonDescent,ResidentFamily.Native,ResidentArchetype.Wanderer,
                    new ResidentPlacementCandidate(home,new(home.X-96,home.Y-64,192,160),new[]{home-new Vector2(48,0),home+new Vector2(48,0)})));
                int died=0,defeated=0;actor.Defeated+=()=>defeated++;
                f.MountResident(actor,PrologueSectorId.CanyonDescent);
                HealthComponent health=actor.GetNode<HealthComponent>("Health");health.Died+=()=>died++;
                var body=actor.GetNode<CollisionShape2D>("BodyCollider");
                if(!fatal)
                {
                    await f.Sync();
                    CollisionShape2D exit=f.Sectors[PrologueSectorId.CanyonDescent].GetNode<CollisionShape2D>("Gameplay/Collision/FrozenExit/Collision");
                    Rect2 exitFloor=exit.GlobalTransform*exit.Shape.GetRect();
                    Rect2 supported=body.GlobalTransform*body.Shape.GetRect();
                    TestAssert.True(actor.IsOnFloor()&&supported.Position.X>=exitFloor.Position.X&&supported.End.X<=exitFloor.End.X&&
                        Mathf.Abs(supported.End.Y-exitFloor.Position.Y)<.2f,"actual whole-body support on saved FrozenExit before real pull");
                    actor.ApplyThreadPull(new(600,0));
                }
                Rect2 start=body.GlobalTransform*body.Shape.GetRect();
                TestAssert.True(start.End.Y<3584,"actual initial whole body is above saved16px strip");
                Vector2 previous=actor.GlobalPosition;float maxStep=0;
                for(int frame=0;frame<120&&health.CurrentHealth>0&&!(actor.IsOnFloor()&&actor.GlobalPosition.Y>3900);frame++)
                {
                    await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                    maxStep=Mathf.Max(maxStep,actor.GlobalPosition.DistanceTo(previous));previous=actor.GlobalPosition;
                }
                if(fatal) TestAssert.True(health.CurrentHealth==0&&died==1&&defeated==1&&actor.GlobalPosition.Y>3500,
                    "saved strip actual whole-body fall emits one real death, not only a synthetic Area");
                else
                {
                    CollisionShape2D camp=f.Sectors[PrologueSectorId.CanyonBasin].GetNode<CollisionShape2D>("Gameplay/Collision/BasinCampFloor/Collision");
                    Rect2 floor=camp.GlobalTransform*camp.Shape.GetRect();
                    TestAssert.True(actor.IsOnFloor()&&actor.GlobalPosition.Y>3960&&actor.GlobalPosition.Y<3965&&
                        Mathf.Abs((body.GlobalTransform*body.Shape.GetRect()).End.Y-floor.Position.Y)<.2f&&
                        health.CurrentHealth==3&&died==0&&defeated==0,
                        $"saved380pxhole passes wholebody into actualCampfloor, no mergedvoid/home threshold: {actor.GlobalPosition}");
                }
                TestAssert.True(maxStep<30,"native gravity fall without pose injection or teleport");
                GD.Print($"RESIDENT_VOID_SAVED_FALL requested_x={x} home={home} pull_x={(fatal?0:600)} fatal={fatal} hp={health.CurrentHealth} died={died} defeated={defeated} final={actor.GlobalPosition} max_step={maxStep} streamed_registry_tested=false");
                GD.Print("RESIDENT_VOID_CASE_OK "+name);
            }
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_VOID_CASE_FAIL "+name+": "+error.Message);}
            finally {await f.DisposeAsync();}
        }
    }
}
