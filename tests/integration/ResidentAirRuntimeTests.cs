using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Moth;
using Knight.Enemies.Residents;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.World;

namespace Knight.Tests.Integration;

// Actual saved actor in an owned physical airspace, not formal family/spawn proof.
public static partial class ResidentAirRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("unconfigured_hover_unchanged",async f=>
        {
            await f.Mount(false);Vector2 start=f.Actor.GlobalPosition;await f.Frames(60);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Hover&&f.Actor.GlobalPosition.DistanceTo(start)<.01f,
                "actual old no-player Hover remains stationary");
        });
        await Check("actual_two_axis_air_patrol",async f=>
        {
            await f.Mount();var t=await f.Trace(300);
            TestAssert.True(t.min.X<292&&t.max.X>348&&t.min.Y<100&&t.max.Y>156&&t.step<1.5f,
                $"real air cruise travels both axes continuously {t}");
        });
        await Check("complete_circle_rejected_and_same_body_wall_blocks",async f=>
        {
            bool rejected=false;
            try{f.Agent.ConfigureResidence(new("invalid-air","native-air.moth",CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,
                ResidentFamily.Native,ResidentArchetype.Flyer,new(new(320,128),new(320,64,128,144),new[]{new Vector2(336,96),new Vector2(368,160)})));}
            catch(ArgumentException){rejected=true;}
            TestAssert.True(rejected,"center-inside is insufficient: the full saved radius16 circle must fit before mount");
            f.Wall();await f.Mount();var t=await f.Trace(240);
            TestAssert.True(t.max.X<=328.1f&&t.min.X<292&&t.step<1.5f,
                $"real complete circle stops at wall x344 while continuing legal air patrol {t}");
        });
        await Check("locked_dive_contact_then_physical_home_return",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true);Vector2 home=new(320,96);
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Telegraph&&f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph,"actual complete warning precedes dive");
            AttackPresentationFrame warning=f.Presentation;f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.HasLockedTarget&&x.Timeline.Phase==AttackPhase.Telegraph)warning=x;};
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Recover,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Recover&&f.Actor.GlobalPosition.Y>230&&
                f.Actor.GlobalPosition.Y<=240.1f&&f.PlayerHealth.CurrentHealth<9&&warning.Shape.Kind==AttackShapeKind.Capsule,
                "saved locked capsule leads to real world first-contact recovery and native contact damage");
            TestAssert.True(f.Actor.GlobalPosition.DistanceTo(warning.Timeline.LockedWorldTarget)<.01f,
                "actual body reaches the advertised native-clamped endpoint");
            f.Target.RemoveFromGroup("player");var t=await f.Trace(360,home);
            TestAssert.True(t.returned&&t.step<8.2f&&f.Health.CurrentHealth==2,
                $"real recovery returns upward to home without reset, teleport or healing {t}");
        });
        await Check("locked_target_departure_withdraws_attack",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true);
            await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph&&f.Presentation.Timeline.HasLockedTarget,"actual warning is already locked");
            int active=0;f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.Phase==AttackPhase.Active)active++;};
            f.Target.MoveAndCollide(new(240,0));await f.Frames(90);
            TestAssert.True(active==0&&f.PlayerHealth.CurrentHealth==9&&f.Actor.CurrentFrame.AttackTimeline.Phase==AttackPhase.None,
                "locked target exiting territory cannot produce later dive damage or presentation");
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),
                "direct stale contact is rejected even without native Monitoring");
        });
        await Check("real_claw_external_motion_then_air_return",async f=>
        {
            f.Floor(432);await f.Mount();Vector2 home=new(320,128);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(100,0));
            TestAssert.True(target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new ThreadAbilitySettings(EnemyPullSpeed:360)),
                "real existing LightEnemy receiver dispatch accepts pull");
            var t=await f.Trace(420,home);
            // .75s of gravity980 reaches735px/s; with decaying horizontal pull
            // a legitimate 60Hz step is about12.33px, not initial-pull/60.
            TestAssert.True(t.max.X>368&&t.max.Y>260&&t.returned&&t.step<13f&&f.Health.CurrentHealth==2,
                $"real claw crosses air territory, gravity falls, then returns without clamp/heal {t}");
        });
        await Check("active_sleep_rejects_direct_authority_and_fresh_wake",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true);
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Dive,"sleep begins during real active dive");
            ulong old=f.Actor.CurrentFrame.AttackTimeline.AttackId;int hp=f.PlayerHealth.CurrentHealth;
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");f.Agent.SetResidentDormant(true);
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox"))&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,600,1,900)),"sleep rejects stale direct outgoing/incoming damage");
            TestAssert.True(!target.IsInGroup(ThreadTarget2D.TargetGroup)&&!target.IsInGroup(GrappleSurface2D.TargetGroup)&&
                target.GetNode<Area2D>("TargetCollision").CollisionLayer==0,"sleep withdraws all native target discovery");
            Vector2 pose=f.Actor.GlobalPosition;target.ApplyPull(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings(EnemyPullSpeed:600));await f.Frames(10);
            TestAssert.True(f.Actor.GlobalPosition.DistanceTo(pose)<.1f&&f.Health.CurrentHealth==2&&f.PlayerHealth.CurrentHealth==hp,
                "captured target cannot pull sleeping actor or heal it");
            f.Agent.SetResidentDormant(false);bool warning=false;ulong next=0;
            for(int i=0;i<240;i++){await f.Frames(1);warning|=f.Actor.CurrentState==GraveLampMothState.Telegraph;if(f.Actor.CurrentState==GraveLampMothState.Dive){next=f.Actor.CurrentFrame.AttackTimeline.AttackId;break;}}
            TestAssert.True(warning&&next>old&&f.Health.CurrentHealth==2,"wake needs a new full warning/ID and preserves health");
        });
        await Check("native_damage_interrupts_dive_and_preserves_force",async f=>
        {
            f.Floor(432);f.Player(new(320,236));await f.Mount(dive:true);
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Dive,"damage occurs during true locked dive");
            Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,600,2,900)),"actual Health applies hurt impulse");
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),"hurt immediately interrupts direct active damage");
            f.Target.RemoveFromGroup("player");await f.Frames(3);
            TestAssert.True(f.Actor.GlobalPosition.X>before.X+15&&f.Actor.CurrentState==GraveLampMothState.ThreadStunned,
                "Dive does not overwrite the actual horizontal hurt impulse");
            var t=await f.Trace(420,new(320,96));
            TestAssert.True(t.returned&&f.Health.CurrentHealth==1,"real air return after hurt preserves reduced HP");
        });
        await Check("death_is_absorbing_and_configuration_is_atomic",async f=>
        {
            await f.Mount();bool rejected=false;try{f.Agent.ConfigureResidence(null);}catch(InvalidOperationException){rejected=true;}
            TestAssert.True(rejected,"mounted residence cannot be reconfigured/reset");
            int deaths=0;f.Actor.Defeated+=()=>deaths++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(2,Vector2.Zero,0,3,900)),"actual ordinary Health death");
            f.Agent.SetResidentDormant(true);f.Agent.SetResidentDormant(false);Vector2 pose=f.Actor.GlobalPosition;await f.Frames(20);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(f.Health.CurrentHealth==0&&deaths==1&&f.Actor.CurrentState==GraveLampMothState.Dead&&
                pose.DistanceTo(f.Actor.GlobalPosition)<.01f&&!target.IsAvailable&&target.GetNode<Area2D>("TargetCollision").CollisionLayer==0,
                "sleep/wake cannot resurrect, move or reauthorize corpse before unload");
        });
        await Check("zero_knockback_hurt_revokes_active_dive",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true);await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Dive,"zero-impulse case starts in native Active");
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Zero,0,4,900)),"real nonlethal damage has no knockback");
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox"))&&
                f.Presentation.Timeline.Phase==AttackPhase.None&&f.Health.CurrentHealth==1,"zero-impulse damage still withdraws outgoing attack immediately");
        });
        await Check("locked_target_cannot_be_replaced_by_other_live_player",async f=>
        {
            f.Floor(256);CharacterBody2D original=f.Player(new(320,236));await f.Mount(dive:true);
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Dive,"original target has actual locked Active");
            f.Player(new(360,236));original.MoveAndCollide(new(240,0));await f.Frames(1);
            TestAssert.True(f.Presentation.Timeline.Phase==AttackPhase.None&&f.Actor.CurrentState!=GraveLampMothState.Dive,
                "another eligible same-world player cannot authorize continuation of the departed target's locked attack");
        });
        await Check("native_death_publication_reset_preserves_second_life",async f=>
        {
            await f.Mount();int defeats=0,connections=0;bool reset=false;
            f.Actor.Defeated+=()=>defeats++;
            f.Actor.FrameChanged+=(frame,_,_)=>{if(frame.State==GraveLampMothState.Dead&&!reset){reset=true;f.Actor.ResetForCheckpoint();}};
            Hitbox attack=f.IncomingAttack(f.Actor.GlobalPosition);attack.Connected+=_=>connections++;attack.Begin(201);
            await f.Until(()=>connections==1,12);await f.Frames(2);
            TestAssert.True(reset&&connections==1&&defeats==1&&f.Health.CurrentHealth==2,"actual native first death's presentation subscriber resets the actor");
            attack.End();await f.Frames(2);attack.Begin(202);await f.Until(()=>connections==2,12);await f.Frames(1);
            TestAssert.True(connections==2&&defeats==2&&f.Health.CurrentHealth==0&&!f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "old death publication cannot poison the next life's defeat guard or deferred hurtbox state");
        });
        await Check("sleep_keeps_existing_external_physics_but_rejects_new_authority",async f=>
        {
            f.Floor(432);await f.Mount();Vector2 before=f.Actor.GlobalPosition;
            f.Actor.ApplyThreadPull(new(360,0));f.Agent.SetResidentDormant(true);await f.Frames(10);
            TestAssert.True(f.Actor.GlobalPosition.X>before.X+20&&f.Actor.GlobalPosition.Y>before.Y+10&&f.Health.CurrentHealth==2,
                "sleep must not erase already accepted real pull/gravity or clamp body to its territory");
            TestAssert.True(!f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,600,5,900)),"new sleeping damage remains rejected during carried physics");
        });
        await Check("real_void_pending_survives_sleep_and_source_removal",async f=>
        {
            KillZone source=f.Void(new(0,256,896,16));await f.Mount();int defeats=0;f.Actor.Defeated+=()=>defeats++;
            f.Health.InvulnerabilitySeconds=1.2f;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Zero,0,6,900)),"real Health invulnerability precedes void");
            f.Actor.ApplyThreadPull(new(0,600));var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            await f.Until(()=>!target.IsAvailable,30);
            TestAssert.True(!target.IsAvailable&&f.Health.CurrentHealth==1&&defeats==0,"actual body sweep contacts layer-zero KillZone without bypassing Health invulnerability");
            source.QueueFree();f.Agent.SetResidentDormant(true);await f.Until(()=>f.Health.CurrentHealth==0,100);
            TestAssert.True(f.Health.CurrentHealth==0&&defeats==1&&f.Actor.CurrentState==GraveLampMothState.Dead,"pending real fatal contact survives source destruction and sleep until one accepted death");
        });
        GD.Print($"RESIDENT_AIR_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_AIR_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_AIR_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
    }

    // Separate bounded phase: the original 14-case motion suite keeps its cap
    // and exact receipt; these checks exercise previously unproved boundaries.
    public static async Task RunBoundaries(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("foreign_world_player_rejected_then_local_player_attacked",async f=>
        {
            var foreign=new Fixture(tree);
            try
            {
                foreign.Initialize();foreign.Floor(256);CharacterBody2D decoy=foreign.Player(new(320,236));
                f.Floor(256);await f.Mount(dive:true);
                TestAssert.True(decoy.GetWorld2D()!=f.Actor.GetWorld2D()&&decoy.IsInGroup("player")&&foreign.PlayerHealth.CurrentHealth==9,
                    "live same-coordinate decoy really belongs to a distinct physical world");
                bool engaged=Engaging(f.Actor.CurrentState);f.Actor.FrameChanged+=(frame,_,_)=>engaged|=Engaging(frame.State);
                await f.Frames(90);
                TestAssert.True(!engaged&&foreign.PlayerHealth.CurrentHealth==9,"foreign-world player cannot authorize Align, warning or Dive");
                f.Player(new(320,236));await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,160);
                TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Dive,"same-world positive control reaches real Active after complete warning");
                await f.Until(()=>f.PlayerHealth.CurrentHealth<9,30);
                TestAssert.True(f.PlayerHealth.CurrentHealth<9&&foreign.PlayerHealth.CurrentHealth==9,
                    "native same-world contact damages only the local player, not an overlapping foreign player");
            }
            finally{await foreign.DisposeAsync();}
        });
        await Check("dead_player_keeps_body_and_group_but_cannot_authorize_attack",async f=>
        {
            f.Floor(256);f.Player(new(320,236));int deaths=0;f.PlayerHealth.Died+=()=>deaths++;
            TestAssert.True(f.PlayerHealth.ApplyDamage(new(9,Vector2.Zero,0,301,901)),"real target Health accepts lethal damage before perception");
            await f.Mount(dive:true);bool engaged=Engaging(f.Actor.CurrentState);f.Actor.FrameChanged+=(frame,_,_)=>engaged|=Engaging(frame.State);
            await f.Frames(90);
            TestAssert.True(f.Target.IsInsideTree()&&f.Target.IsInGroup("player")&&f.PlayerHealth.CurrentHealth==0&&deaths==1&&!engaged,
                "dead in-tree target retaining its group/body cannot authorize combat");
        });
        await Check("world_occlusion_prevents_initial_attack",async f=>
        {
            f.Floor(256);f.Player(new(320,236));StaticBody2D wall=f.Barrier(new(256,164,128,8));await f.Mount(dive:true);
            f.AssertRayHits(wall);bool engaged=Engaging(f.Actor.CurrentState);f.Actor.FrameChanged+=(frame,_,_)=>engaged|=Engaging(frame.State);
            await f.Frames(90);
            TestAssert.True(!engaged&&f.PlayerHealth.CurrentHealth==9&&f.Presentation.Timeline.Phase==AttackPhase.None,
                "actual world obstruction prevents warning/Active, not merely contact damage");
        });
        await Check("complete_circle_patrol_stays_below_real_ceiling",async f=>
        {
            f.Barrier(new(256,80,128,4));await f.Mount();var t=await f.Trace(240);
            // Roof bottom84 + saved physical radius16 = center100. The route
            // point96 is below the roof by center alone but its body penetrates.
            TestAssert.True(t.min.Y>=99.9f&&t.min.Y<103&&t.max.Y>155&&t.min.X<292&&t.step<1.5f,
                $"native full circle stops at ceiling while continuing its other legal air leg {t}");
        });
        await Check("locked_warning_loses_world_sight_and_cancels_same_attack",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true);await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph&&f.Presentation.Timeline.HasLockedTarget,"actual warning is locked before LOS changes");
            ulong old=f.Presentation.Timeline.AttackId;int active=0;
            f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)active++;};
            StaticBody2D wall=f.Barrier(new(256,164,128,8));await f.Frames(2);f.AssertRayHits(wall);await f.Frames(90);
            TestAssert.True(active==0&&f.PlayerHealth.CurrentHealth==9&&f.Presentation.Timeline.Phase==AttackPhase.None&&
                !f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),
                "lost locked LOS withdraws Active and direct authority, not just blocks the dive body");
        });
        await Check("locked_warning_target_dies_without_removing_group_or_body",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true);await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph&&f.Presentation.Timeline.HasLockedTarget,"real target is alive through locked warning");
            ulong old=f.Presentation.Timeline.AttackId;int active=0,deaths=0;f.PlayerHealth.Died+=()=>deaths++;
            f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)active++;};
            TestAssert.True(f.PlayerHealth.ApplyDamage(new(9,Vector2.Zero,0,302,901)),"real Health death occurs after the lock");await f.Frames(90);
            TestAssert.True(f.Target.IsInsideTree()&&f.Target.IsInGroup("player")&&f.PlayerHealth.CurrentHealth==0&&deaths==1&&active==0&&
                f.Presentation.Timeline.Phase==AttackPhase.None&&!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),
                "target death cancels the old locked attack while its native body/group remain");
        });
        GD.Print($"RESIDENT_AIR_BOUNDARY_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_AIR_BOUNDARY_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_AIR_BOUNDARY_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static bool Engaging(GraveLampMothState state)=>state is GraveLampMothState.Align or GraveLampMothState.Telegraph or GraveLampMothState.Dive;
    }

    // Same saved body, target and airspace; role differences must be physical,
    // not merely the shared model state's legacy name (Dive).
    public static async Task RunRoles(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("flyer_short_side_attack",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(roleSpace:true);
            var attack=await Observe(f);
            Vector2 direction=attack.end-attack.start;
            TestAssert.True(attack.start.Y>=200&&MathF.Abs(direction.X)>MathF.Abs(direction.Y)*2&&
                attack.travel>90&&attack.travel<=96.1f&&f.PlayerHealth.CurrentHealth==8,
                $"Flyer approaches from the side, crosses a short physical path and damages the real target: {attack}");
        });
        await Check("diver_keeps_high_downward_first_contact",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(dive:true,roleSpace:true);
            var attack=await Observe(f);Vector2 direction=attack.end-attack.start;
            TestAssert.True(attack.start.Y<130&&direction.Y>MathF.Abs(direction.X)*2&&attack.end.Y>=239&&
                attack.end.Y<=240.1f&&f.PlayerHealth.CurrentHealth==8,
                $"Diver still commits from above and stops its actual circle at floor256: {attack}");
        });
        await Check("flyer_can_attack_from_the_other_side",async f=>
        {
            f.Floor(256);f.Player(new(288,236));await f.Mount(roleSpace:true);
            var attack=await Observe(f);Vector2 direction=attack.end-attack.start;
            TestAssert.True(attack.start.X>360&&attack.start.Y>=200&&direction.X<0&&
                MathF.Abs(direction.X)>MathF.Abs(direction.Y)*2&&attack.travel>90&&attack.travel<=96.1f&&f.PlayerHealth.CurrentHealth==8,
                "same Flyer chooses the right approach when initially right of its real target, not a hard-coded rightward dash");
        });
        await Check("flyer_locked_endpoint_does_not_home_after_target_moves",async f=>
        {
            f.Floor(256);CharacterBody2D same=f.Player(new(320,236));await f.Mount(roleSpace:true);
            await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,160);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph,"move happens after native warning lock, before Active");
            Vector2 locked=f.Presentation.Timeline.LockedWorldTarget;same.MoveAndCollide(new(104,-100));
            TestAssert.True(ReferenceEquals(same,f.Target)&&same.GlobalPosition.DistanceTo(new(424,136))<.2f,
                $"same live physical target moves within the original territory, allowing native contact skin, without replacing identity: {same.GlobalPosition}");
            var attack=await Observe(f);
            TestAssert.True(attack.end.DistanceTo(locked)<.1f&&f.PlayerHealth.CurrentHealth==9&&attack.travel<=96.1f,
                "Flyer follows its advertised old endpoint rather than homing or damaging the dodged target");
        });
        await Check("flyer_checkpoint_reset_keeps_role",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await f.Mount(roleSpace:true);
            var first=await Observe(f);f.Actor.ResetForCheckpoint();
            var second=await Observe(f);
            TestAssert.True(first.start.Y>=200&&second.start.Y>=200&&second.travel>90&&second.travel<=96.1f&&
                MathF.Abs(second.end.X-second.start.X)>MathF.Abs(second.end.Y-second.start.Y)*2&&f.Health.CurrentHealth==2,
                "reset reconstructs the configured side-attack model, not the legacy downward model");
        });
        await Check("flyer_whole_body_wall_forecast_matches_first_contact",async f=>
        {
            f.Floor(256);f.Player(new(320,236));f.Barrier(new(344,180,8,76));await f.Mount(roleSpace:true);
            var attack=await Observe(f);
            TestAssert.True(attack.end.X>327&&attack.end.X<=328.1f&&attack.travel<96&&f.PlayerHealth.CurrentHealth==8,
                "side attack's full circle forecast and native first contact stop before wall344, not at its centerline");
        });
        await Check("real_claw_interrupts_flyer_warning_and_preserves_physical_return",async f=>
        {
            f.Floor(432);f.Player(new(320,236));await f.Mount(roleSpace:true);
            await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,160);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph,"real side warning is locked before existing claw dispatch");
            ulong old=f.Presentation.Timeline.AttackId;int oldActive=0;
            f.Actor.AttackFrameChanged+=frame=>{if(frame.Timeline.AttackId==old&&frame.Timeline.Phase==AttackPhase.Active)oldActive++;};
            Vector2 before=f.Actor.GlobalPosition;Node2D caster=f.Caster(before+new Vector2(100,0));
            TestAssert.True(f.Actor.GetNode<ThreadTarget2D>("ThreadTarget").TryApplyLegacyInteraction(caster,caster.GlobalPosition,
                new ThreadAbilitySettings(EnemyPullSpeed:360)),"actual existing claw receiver accepts the side attack interruption");
            TestAssert.True(f.Presentation.Timeline.Phase==AttackPhase.None&&
                !f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),"captured old side attack loses both warning and direct damage authority");
            f.Target.RemoveFromGroup("player");await f.Frames(10);
            TestAssert.True(f.Actor.GlobalPosition.X>before.X+20&&f.Actor.GlobalPosition.Y>before.Y+10,
                "claw uses accepted native pull and gravity instead of replacing the body pose");
            var trace=await f.Trace(420,new(320,96));
            TestAssert.True(trace.returned&&trace.step<13&&oldActive==0&&f.Health.CurrentHealth==2&&f.PlayerHealth.CurrentHealth==9,
                "interrupted side attack returns physically to high home, without damage, teleport or healing");
        });
        GD.Print($"RESIDENT_AIR_ROLE_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_AIR_ROLE_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_AIR_ROLE_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static async Task<(Vector2 start,Vector2 end,float travel)> Observe(Fixture f)
        {
            await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,160);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Telegraph&&f.Presentation.Shape.Kind==AttackShapeKind.Capsule,
                "actual native attack has a visible locked capsule before movement");
            AttackPresentationFrame warning=f.Presentation;Vector2 start=f.Actor.GlobalPosition,last=start;float travel=0,maxStep=0;
            ulong id=warning.Timeline.AttackId;bool active=false,recovered=false;
            for(int i=0;i<100;i++)
            {
                await f.Frames(1);Vector2 next=f.Actor.GlobalPosition;float step=next.DistanceTo(last);travel+=step;maxStep=MathF.Max(maxStep,step);last=next;
                if(f.Presentation.Timeline.AttackId==id&&f.Presentation.Timeline.Phase==AttackPhase.Active)active=true;
                if(active&&f.Actor.CurrentState==GraveLampMothState.Recover){recovered=true;break;}
            }
            GD.Print($"RESIDENT_AIR_ROLE_TRACE id={id} start={start} end={last} warning={warning.Timeline.LockedWorldTarget} travel={travel} max_step={maxStep} player_hp={f.PlayerHealth.CurrentHealth}");
            TestAssert.True(active&&recovered&&f.WarningTicksBeforeActive>=42&&last.DistanceTo(warning.Timeline.LockedWorldTarget)<.1f&&maxStep<8.2f,
                $"real fixed-direction Active follows at least42 native warning ticks, reaches its locked endpoint continuously then recovers; warning_ticks={f.WarningTicksBeforeActive}");
            return(start,last,travel);
        }
    }

    public static async Task RunBone(SceneTree tree)
    {
        int cases=0;var failures=new List<string>();
        await Check("guarded_hover_real_K_then_expiry_and_physical_return",async f=>
        {
            f.Floor(432);await MountBone(f);await Strike(f,1,false,1100);
            TestAssert.Equal(2,f.Health.CurrentHealth,"guarded Hover never consumes Health");
            Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(100,0));
            await Claw(f,caster);await Strike(f,1,true,1101);
            TestAssert.Equal(1,f.Health.CurrentHealth,"accepted real K opens native J without changing HP2");
            await f.Frames(50);
            TestAssert.True(f.Actor.CurrentState!=GraveLampMothState.ThreadStunned,"original .75 second K pose expires");
            await Strike(f,1,false,1102);TestAssert.Equal(1,f.Health.CurrentHealth,"expired window is closed, not healed");
            var trace=await f.Trace(260,new(320,96));
            TestAssert.True(trace.returned&&f.Health.CurrentHealth==1,"same damaged body physically returns without reset or heal");
        });
        await Check("real_dive_recovery_and_hurt_stun_is_not_K",async f=>
        {
            f.Floor(256);f.Player(new(320,236));await MountBone(f);
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,160);
            TestAssert.Equal(GraveLampMothState.Dive,f.Actor.CurrentState,"same body actually commits a warned downward attack");
            await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Recover,60);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Recover&&f.PlayerHealth.CurrentHealth==8,
                "real floor contact/attack completes into Recover after native player damage");
            Vector2 before=f.Actor.GlobalPosition;await Strike(f,1,true,1110,260);
            await f.Frames(22);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.ThreadStunned&&f.Actor.GlobalPosition.X>before.X+5,
                "real nonzero hurt impulse enters generic ThreadStunned; .35s Health invulnerability has ended");
            await Strike(f,1,false,1111);TestAssert.Equal(1,f.Health.CurrentHealth,"generic Hurt stun is not a K-source opening");
            int defeated=0;f.Actor.Defeated+=()=>defeated++;
            await Claw(f,f.Caster(f.Actor.GlobalPosition+new Vector2(-100,0)));await Strike(f,2,true,1112);
            TestAssert.True(f.Health.CurrentHealth==0&&defeated==1,"genuine K can authorize lethal J in the remaining original stun time");
            GD.Print("RESIDENT_BONE_AIR_HURT_SOURCE generic_hurt_stunned=true health_invulnerability_elapsed=true guarded=true real_K_positive=true");
        });
        await Check("sleep_clears_K_source_without_cancelling_accepted_physics",async f=>
        {
            f.Floor(432);await MountBone(f);Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(100,0));
            await Claw(f,caster);await Strike(f,1,true,1120);
            var receiver=f.Actor as IThreadInteractionReceiver;var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(receiver!=null,"saved Moth exposes a final boolean transaction");
            var interaction=new ThreadInteraction(caster,caster.GlobalPosition,target.WorldAnchor,new());
            Vector2 before=f.Actor.GlobalPosition;f.Agent.SetResidentDormant(true);
            TestAssert.True(!receiver.TryReceiveThread(interaction)&&!target.IsAvailable&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,0,1121,900)),"sleep synchronously rejects captured K and J");
            await f.Frames(5);f.Agent.SetResidentDormant(false);await f.Frames(18);
            TestAssert.True(f.Actor.CurrentState==GraveLampMothState.ThreadStunned&&f.Actor.GlobalPosition.DistanceTo(before)>10,
                "original carried force/gravity and remaining pose persist; .35s invulnerability is over");
            await Strike(f,1,false,1122);TestAssert.Equal(1,f.Health.CurrentHealth,"wake cannot restore the old K-source weakness");
            await Claw(f,caster);await Strike(f,2,true,1123);TestAssert.Equal(0,f.Health.CurrentHealth,"new accepted K reauthorizes native J, not wake healing");
        });
        await Check("native_lethal_signal_Reset_and_second_K_life",async f=>
        {
            f.Floor(432);await MountBone(f);Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(100,0));
            await Claw(f,caster);int defeats=0,connections=0;f.Actor.Defeated+=()=>defeats++;
            Hitbox attack=f.IncomingAttack(f.Actor.GlobalPosition);
            attack.Connected+=_=>{connections++;if(connections==1)f.Actor.ResetForCheckpoint();};attack.Begin(1130);
            await f.Until(()=>connections==1,12);attack.End();await f.Frames(2);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(connections==1&&defeats==1&&f.Health.CurrentHealth==2&&target.IsAvailable&&f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "native lethal Area callback reset supersedes old-life deferred withdrawal");
            await Strike(f,1,false,1131);TestAssert.Equal(2,f.Health.CurrentHealth,"reset life cannot inherit old K opening");
            await Claw(f,caster);await Strike(f,2,true,1132);await f.Frames(1);
            var receiver=f.Actor as IThreadInteractionReceiver;
            TestAssert.True(receiver!=null&&defeats==2&&f.Health.CurrentHealth==0&&!target.IsAvailable&&
                !receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),"second native death rejects captured receiver and discovery exactly once");
        });
        await Check("K_down_real_void_pending_survives_sleep_and_source_removal",async f=>
        {
            KillZone zone=f.Void(new(0,256,896,16));await MountBone(f);f.Health.InvulnerabilitySeconds=1.2f;
            await Claw(f,f.Caster(f.Actor.GlobalPosition+new Vector2(100,0)));await Strike(f,1,true,1140);
            Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(0,800));var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"actual downward K is accepted, no injected body pose");
            int defeats=0;f.Actor.Defeated+=()=>defeats++;await f.Until(()=>!target.IsAvailable,40);
            var receiver=f.Actor as IThreadInteractionReceiver;
            TestAssert.True(receiver!=null&&!target.IsAvailable&&f.Health.CurrentHealth==1&&defeats==0&&
                !receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new()))&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,0,1141,900)),
                "actual layer-zero void contact withdraws J/K while genuine Health invulnerability delays death");
            zone.QueueFree();f.Agent.SetResidentDormant(true);await f.Until(()=>f.Health.CurrentHealth==0,100);
            TestAssert.True(f.Health.CurrentHealth==0&&defeats==1&&f.Actor.CurrentState==GraveLampMothState.Dead,
                "pending environmental Health death bypasses Bone pose gate and survives sleep/deleted source");
        });
        await Check("invalid_profiles_reject_atomically_before_commit",async f=>
        {
            var valid=Bone();
            foreach(var invalid in new[]{valid with {Family=ResidentFamily.Native},valid with {Family=ResidentFamily.Ghost},
                valid with {Archetype=ResidentArchetype.Flyer},valid with {CombatProfile=null},valid with {CombatProfile=new()},
                valid with {CombatProfile=new(ResidentBoneWeakness.RearOrExposed)},valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadExposed)},
                valid with {CombatProfile=new((ResidentBoneWeakness)99)},valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadStunned,.6f,.8f)},
                valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadStunned,float.NaN,0)}})
                Reject(()=>f.Agent.ConfigureResidence(invalid));
            f.Agent.ConfigureResidence(valid);await f.Mount(false,dive:true);Reject(()=>f.Agent.ConfigureResidence(valid));
            await Strike(f,1,false,1150);TestAssert.True(f.Health.CurrentHealth==2&&f.Actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable,
                "all invalid profiles leave one later valid guarded Bone configuration and visible K target");
        });
        await Check("captured_K_rejects_bad_settings_foreign_world_and_queued_chains",async f=>
        {
            await MountBone(f);var receiver=f.Actor as IThreadInteractionReceiver;
            TestAssert.True(receiver!=null,"boolean receiver is actual saved actor, not void fallback success");
            Node2D caster=f.Caster(new(100,96));var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(!Try(new(null,Vector2.Zero,target.WorldAnchor,new())),"null source fails closed");
            foreach(var settings in new ThreadAbilitySettings[]{null,new(EnemyPullSpeed:0),new(EnemyPullSpeed:float.NaN),new(EnemyPullSpeed:float.PositiveInfinity)})
                TestAssert.True(!Try(new(caster,caster.GlobalPosition,target.WorldAnchor,settings)),"malformed speed/settings reject without throwing");
            using(var world=new World2D())
            {
                var view=new SubViewport{Size=new(16,16),World2D=world};f.Scope.AddChild(view);var foreign=new Node2D();view.AddChild(foreign);await f.Frames(1);
                TestAssert.True(foreign.GetTree()==f.Actor.GetTree()&&foreign.GetWorld2D()!=f.Actor.GetWorld2D()&&
                    !Try(new(foreign,foreign.GlobalPosition,target.WorldAnchor,new()))&&!target.TryApplyLegacyInteraction(foreign,foreign.GlobalPosition,new()),
                    "direct and transactional K reject a real same-tree foreign World");view.QueueFree();await f.Frames(2);
            }
            TestAssert.True(caster.GetNodeOrNull<HealthComponent>("Health")==null&&
                Try(new(caster,caster.GlobalPosition,target.WorldAnchor,new(EnemyPullSpeed:1500)))&&f.Actor.Velocity.Length()<=600.01f,
                "live healthless source outside AI territory retains legitimate bounded external pull");
            var parent=new Node2D();var child=new Node2D();parent.AddChild(child);f.Scope.AddChild(parent);
            var stale=new ThreadInteraction(child,child.GlobalPosition,target.WorldAnchor,new());parent.QueueFree();
            TestAssert.True(!Try(stale),"queued ancestor is rejected before child native destruction");await f.Frames(2);
            TestAssert.True(!Try(stale),"captured freed source rejects before native properties are read");
            f.Actor.QueueFree();TestAssert.True(!Try(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),"queued actor rejects captured K before any native reads");
            bool Try(ThreadInteraction interaction){try{return receiver.TryReceiveThread(interaction);}catch(Exception error){throw new InvalidOperationException("captured/malformed K must fail closed without exception",error);}}
        });
        await Check("saved_Player_physical_up_K_then_J_native_contact",async f=>
        {
            f.Floor(256);f.Agent.ConfigureResidence(Bone());f.SavedPlayer(new(300,220));await f.Mount(false,dive:true);
            var combat=f.Target.GetNode<PlayerCombat>("Combat");var claw=f.Target.GetNode<MechanicalClawController>("MechanicalClaw");
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
            int attacks=0,connections=0,entries=0,direction=-1;GraveLampMothState firstPose=default;
            combat.AttackStarted+=(_,d)=>{attacks++;direction=d;};combat.AttackConnected+=hurt=>{if(hurt==f.Actor.GetNode<Hurtbox>("Hurtbox"))connections++;};
            combat.GetNode<Hitbox>("AttackHitbox").AreaEntered+=area=>{if(area==f.Actor.GetNode<Hurtbox>("Hurtbox")){if(entries++==0)firstPose=f.Actor.CurrentState;}};
            try
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.W,true);await f.Frames(2);
                TestAssert.True(f.Target.IsOnFloor()&&claw.KeyboardPreview.Surface==f.Actor.GetNode<ThreadTarget2D>("ThreadTarget"),
                    $"actual saved Player up-preview sees saved Moth before K; player={f.Target.GlobalPosition} moth={f.Actor.GlobalPosition}");
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);for(int i=0;i<45&&adapter.ApplicationCount==0;i++)await f.Frames(1);
                KeyboardActionRuntimeTests.KeyEvent(Key.K,false);TestAssert.Equal(1,adapter.ApplicationCount,"physical K reaches actual saved legacy receiver");
                KeyboardActionRuntimeTests.KeyEvent(Key.J,true);for(int i=0;i<20&&connections==0;i++)await f.Frames(1);
                KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
                GD.Print($"RESIDENT_BONE_AIR_PLAYER_CONTACT K={adapter.ApplicationCount} J={attacks} native_entries={entries} first_pose={firstPose} connected={connections} hp={f.Health.CurrentHealth} direction={direction}");
                TestAssert.True(attacks==1&&direction==(int)AttackDirection.Up&&entries==1&&connections==1&&f.Health.CurrentHealth==1,
                    "original .06s startup/native Up J must actually reach the K-pulled body without timing/HP/pose override");
                GD.Print("RESIDENT_BONE_AIR_KEYBOARD actual_saved_player=true K=1 J=1 connected=1 hp=1 OS_origin=false");
            }
            finally{KeyboardActionRuntimeTests.KeyEvent(Key.W,false);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);KeyboardActionRuntimeTests.KeyEvent(Key.J,false);}
        });
        GD.Print($"RESIDENT_BONE_AIR_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));

        ResidentPopulationDefinition Bone()=>new("bone.native-test.diver.01","bone.native-test.diver",CanyonRouteNodeId.LowCeilingBridge,
            PrologueSectorId.CanyonEntrance,ResidentFamily.Bone,ResidentArchetype.Diver,
            new(new(320,96),new(224,64,256,240),new[]{new Vector2(288,96),new Vector2(352,128)}),new(ResidentBoneWeakness.RecoveryOrThreadStunned));
        async Task MountBone(Fixture f){f.Agent.ConfigureResidence(Bone());await f.Mount(false,dive:true);}
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);try{f.Initialize();await test(f);GD.Print("RESIDENT_BONE_AIR_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_BONE_AIR_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static void Reject(Action action){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}TestAssert.True(rejected,"invalid/repeated Bone air profile must reject before commit");}
        static async Task Claw(Fixture f,Node2D caster)
        {
            TestAssert.True(f.Actor.GetNode<ThreadTarget2D>("ThreadTarget").TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"real saved K accepts living source");
            await f.Frames(1);TestAssert.Equal(GraveLampMothState.ThreadStunned,f.Actor.CurrentState,"original model consumes real K request in native physics");
            TestAssert.True(f.Actor.Velocity.Length()<620,"original 600 pull bound plus one gravity tick, not force enlargement");
        }
        static async Task Strike(Fixture f,int damage,bool accepted,ulong id,float knockback=0)
        {
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(-12,0));hit.Damage=damage;hit.Knockback=knockback;int connections=0;hit.Connected+=_=>connections++;
            hit.Begin(id);await f.Frames(3);
            using Godot.Collections.Array overlaps=(Godot.Collections.Array)hit.GetOverlappingAreas();
            TestAssert.True(connections==1||overlaps.Contains(f.Actor.GetNode<Hurtbox>("Hurtbox")),"rejected J must actually overlap the native hurtbox");
            hit.End();await f.Frames(2);GD.Print($"RESIDENT_BONE_AIR_STRIKE id={id} connected={connections} hp={f.Health.CurrentHealth} pose={f.Actor.CurrentState}");
            TestAssert.Equal(accepted?1:0,connections,"actual Area J obeys Bone pose and source permission");
        }
    }

    // Real saved actors/physics; opacity is deliberately not the permission oracle.
    public static async Task RunGhost(SceneTree tree,bool boundaries=false)
    {
        var failures=new List<string>();int cases=0;
        string suite=boundaries?"BOUNDARY":"CORE";
        foreach(ResidentArchetype role in new[]{ResidentArchetype.Flyer,ResidentArchetype.Diver})
        {
            if(!boundaries)
            {
                await Check(role+"_hidden_native_contact_and_manifest_permissions",async f=>
                {
                    f.Floor(432);f.Wall();await Mount(f,role,.6f,3);
                    var target=Target(f);var hurt=f.Actor.GetNode<Hurtbox>("Hurtbox");var body=f.Actor.GetNode<CollisionShape2D>("BodyCollider");
                    Node2D caster=f.Caster(new(100,96));uint layer=f.Actor.CollisionLayer,mask=f.Actor.CollisionMask;
                    TestAssert.True(!target.IsAvailable&&target.GetNode<Area2D>("TargetCollision").CollisionLayer==0&&
                        !target.IsInGroup(ThreadTarget2D.TargetGroup)&&!target.IsInGroup(GrappleSurface2D.TargetGroup),"Hidden withdraws both discovery groups and native target layer");
                    TestAssert.True(hurt.Monitorable&&hurt.CollisionLayer!=0&&!body.Disabled&&f.Actor.TestMove(f.Actor.GlobalTransform,new(100,0)),
                        "Hidden retains actual J contact surface and circle/world collision");
                    await Strike(f,1,false,1200);Vector2 before=f.Actor.Velocity;
                    TestAssert.True(!Receiver(f).TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new()))&&
                        !target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"captured and transactional Hidden K reject");
                    f.Actor.ApplyThreadPull(new(600,0));TestAssert.Equal(before,f.Actor.Velocity,"legacy void pull cannot bypass Hidden");
                    await Manifest(f);await Strike(f,1,true,1201);
                    TestAssert.True(f.Health.CurrentHealth==1&&Receiver(f).TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new()))&&
                        f.Actor.CollisionLayer==layer&&f.Actor.CollisionMask==mask&&!body.Disabled,"Manifest permits original native J and real K without replacing physical collision");
                });
                await Check(role+"_original_warning_and_physical_attack",async f=>
                {
                    f.Floor(256);f.Player(new(320,236));await Mount(f,role,.2f,4);await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,200);
                    TestAssert.Equal(GraveLampMothState.Telegraph,f.Actor.CurrentState,"original lock occurs during visible warning");
                    Vector2 start=f.Actor.GlobalPosition,end=start;bool active=false,recovered=false;float travel=0;Vector2 last=start;
                    for(int i=0;i<100;i++){await f.Frames(1);Vector2 next=f.Actor.GlobalPosition;travel+=next.DistanceTo(last);last=next;
                        if(f.Actor.CurrentState==GraveLampMothState.Dive)active=true;
                        else if(active){end=next;recovered=f.Actor.CurrentState==GraveLampMothState.Recover;break;}}
                    Vector2 direction=end-start;
                    TestAssert.True(active&&recovered&&f.WarningTicksBeforeActive>=42&&f.PlayerHealth.CurrentHealth==8,
                        $"original .70 warning and actual contact remain usable: role={role} ticks={f.WarningTicksBeforeActive} playerHP={f.PlayerHealth.CurrentHealth}");
                    TestAssert.True(role==ResidentArchetype.Flyer?MathF.Abs(direction.X)>MathF.Abs(direction.Y)*2&&travel>90&&travel<=96.1f:
                        start.Y<130&&direction.Y>MathF.Abs(direction.X)*2&&end.Y>=239&&end.Y<=240.1f,"Ghost phase must not flatten distinct native role trajectories");
                });
                await Check(role+"_hide_revokes_active_old_identity",async f=>
                {
                    f.Floor(256);f.Player(new(320,236));await Mount(f,role,.2f,role==ResidentArchetype.Flyer?1.9f:.85f);
                    await f.Until(()=>f.Actor.CurrentState==GraveLampMothState.Dive,200);
                    TestAssert.True(f.Actor.CurrentState==GraveLampMothState.Dive&&Target(f).IsAvailable,"finite phase actually contains original Active, not a compressed model");
                    ulong old=f.Presentation.Timeline.AttackId;await f.Until(()=>!Target(f).IsAvailable,100);
                    TestAssert.True(!Target(f).IsAvailable&&f.Presentation.Timeline.Phase==AttackPhase.None&&
                        !f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),"hide edge revokes captured Active synchronously");
                    int stale=0;f.Actor.AttackFrameChanged+=frame=>{if(frame.Timeline.AttackId==old&&frame.Timeline.Phase==AttackPhase.Active)stale++;};
                    await f.Until(()=>f.Presentation.Timeline.Phase==AttackPhase.Telegraph,260);
                    TestAssert.True(stale==0&&f.Presentation.Timeline.Phase==AttackPhase.Telegraph&&f.Presentation.Timeline.AttackId!=old&&
                        f.Presentation.Timeline.PhaseRemainingSeconds>=.69f,"next Manifest starts a new complete warning, never resumes old Active");
                });
            }
            else
            {
                await Check(role+"_accepted_K_survives_hide_and_sleep_pauses_phase",async f=>
                {
                    f.Floor(432);await Mount(f,role,.6f,.5f);await Manifest(f);await f.Frames(14);
                    Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(100,0));Vector2 before=f.Actor.GlobalPosition;
                    TestAssert.True(Target(f).TryApplyLegacyInteraction(caster,caster.GlobalPosition,new(EnemyPullSpeed:360)),"real Manifest K accepted before hide");
                    await f.Until(()=>!Target(f).IsAvailable,30);await f.Frames(3);
                    TestAssert.True(f.Actor.CurrentState==GraveLampMothState.ThreadStunned&&f.Actor.GlobalPosition.X>before.X+20&&f.Actor.GlobalPosition.Y>before.Y+10,
                        "hide preserves pending/stunned K velocity, gravity and native movement");
                    await Strike(f,1,false,1210);f.Agent.SetResidentDormant(true);await f.Frames(45);f.Agent.SetResidentDormant(false);await f.Frames(12);
                    TestAssert.True(!Target(f).IsAvailable&&f.Health.CurrentHealth==2,"sleep longer than Hidden duration pauses, rather than resets or advances, the remaining phase");
                    await Manifest(f);var trace=await f.Trace(240,new(320,96));
                    TestAssert.True(trace.returned&&trace.step<13&&f.Health.CurrentHealth==2,"carried force ends in real bounded physical return, not teleport/heal");
                });
                await Check(role+"_native_lethal_reset_starts_new_hidden_life",async f=>
                {
                    f.Floor(432);await Mount(f,role,.2f,3);await Manifest(f);int deaths=0,connections=0;bool reset=false;
                    f.Actor.Defeated+=()=>deaths++;f.Actor.FrameChanged+=(frame,_,_)=>{if(frame.State==GraveLampMothState.Dead&&!reset){reset=true;f.Actor.ResetForCheckpoint();}};
                    Hitbox attack=f.IncomingAttack(f.Actor.GlobalPosition);attack.Connected+=_=>connections++;attack.Begin(1220);await f.Frames(3);attack.End();
                    TestAssert.True(reset&&deaths==1&&connections==1&&f.Health.CurrentHealth==2&&!Target(f).IsAvailable&&
                        f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,"same-stack native death reset is Hidden yet contactable, old deferred authority cannot reopen it");
                    await Manifest(f);await Strike(f,2,true,1221);await f.Frames(80);
                    TestAssert.True(deaths==2&&f.Health.CurrentHealth==0&&f.Actor.CurrentState==GraveLampMothState.Dead&&!Target(f).IsAvailable,"natural phase cannot revive corpse");
                });
                await Check(role+"_saved_Player_physical_K_J",async f=>
                {
                    f.Floor(256);f.SavedPlayer(new(300,220));await Mount(f,role,.2f,4);await Manifest(f);
                    var combat=f.Target.GetNode<PlayerCombat>("Combat");var claw=f.Target.GetNode<MechanicalClawController>("MechanicalClaw");
                    var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");int attacks=0,connected=0,entries=0,direction=-1;
                    combat.AttackStarted+=(_,d)=>{attacks++;direction=d;};combat.AttackConnected+=hurt=>{if(hurt==f.Actor.GetNode<Hurtbox>("Hurtbox"))connected++;};
                    combat.GetNode<Hitbox>("AttackHitbox").AreaEntered+=area=>{if(area==f.Actor.GetNode<Hurtbox>("Hurtbox"))entries++;};
                    try
                    {
                        KeyboardActionRuntimeTests.KeyEvent(Key.W,true);await f.Frames(2);
                        TestAssert.True(f.Target.IsOnFloor()&&claw.KeyboardPreview.Surface==Target(f),$"real Up preview before K role={role} player={f.Target.GlobalPosition} actor={f.Actor.GlobalPosition}");
                        KeyboardActionRuntimeTests.KeyEvent(Key.K,true);await f.Until(()=>adapter.ApplicationCount==1,45);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
                        TestAssert.Equal(1,adapter.ApplicationCount,"physical-keycode K dispatch reaches actual adapter");
                        KeyboardActionRuntimeTests.KeyEvent(Key.J,true);await f.Until(()=>connected==1,20);KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
                        GD.Print($"RESIDENT_GHOST_AIR_PLAYER role={role} K={adapter.ApplicationCount} J={attacks} entries={entries} connected={connected} hp={f.Health.CurrentHealth} direction={direction} OS_origin=false");
                        TestAssert.True(attacks==1&&entries==1&&connected==1&&direction==(int)AttackDirection.Up&&f.Health.CurrentHealth==1,"original saved Player K→J has actual native Up contact without pose/HP/timing overrides");
                    }
                    finally{KeyboardActionRuntimeTests.KeyEvent(Key.W,false);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);KeyboardActionRuntimeTests.KeyEvent(Key.J,false);}
                });
            }
        }
        if(boundaries)
        {
            await Check("invalid_profiles_reject_atomically",async f=>
            {
                var valid=Ghost(ResidentArchetype.Flyer,.2f,3);
                foreach(var invalid in new[]{valid with {CombatProfile=null},valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadStunned,.2f,3)},
                    valid with {CombatProfile=new(ResidentBoneWeakness.None,0,3)},valid with {CombatProfile=new(ResidentBoneWeakness.None,float.NaN,3)},
                    valid with {CombatProfile=new(ResidentBoneWeakness.None,.2f,float.PositiveInfinity)},valid with {CombatProfile=new(ResidentBoneWeakness.None,.2f,61)},
                    valid with {Family=ResidentFamily.Native},valid with {Archetype=ResidentArchetype.Wanderer}})
                {bool rejected=false;try{f.Agent.ConfigureResidence(invalid);}catch(ArgumentException){rejected=true;}TestAssert.True(rejected,"invalid Ghost profile rejects before commit");}
                f.Agent.ConfigureResidence(valid);await f.Mount(false,roleSpace:true);TestAssert.True(!Target(f).IsAvailable,"one valid mount remains possible and starts Hidden after all rejects");await Manifest(f);
            });
            await Check("manifest_captured_K_guards_and_reentrant_reset",async f=>
            {
                await Mount(f,ResidentArchetype.Diver,.2f,3);await Manifest(f);var receiver=Receiver(f);Node2D caster=f.Caster(new(100,96));var target=Target(f);
                foreach(var settings in new ThreadAbilitySettings[]{null,new(EnemyPullSpeed:0),new(EnemyPullSpeed:float.NaN),new(EnemyPullSpeed:float.PositiveInfinity)})
                    TestAssert.True(!receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,settings)),"Manifest still rejects bad settings without exception");
                using(var world=new World2D())
                {
                    var view=new SubViewport{Size=new(16,16),World2D=world};f.Scope.AddChild(view);var foreign=new Node2D();view.AddChild(foreign);await f.Frames(1);
                    TestAssert.True(!receiver.TryReceiveThread(new(foreign,foreign.GlobalPosition,target.WorldAnchor,new())),"Manifest rejects real foreign World");view.QueueFree();await f.Frames(2);
                }
                var parent=new Node2D();var child=new Node2D();parent.AddChild(child);f.Scope.AddChild(parent);var stale=new ThreadInteraction(child,child.GlobalPosition,target.WorldAnchor,new());parent.QueueFree();
                TestAssert.True(!receiver.TryReceiveThread(stale),"Manifest rejects queued source ancestry");await f.Frames(2);TestAssert.True(!receiver.TryReceiveThread(stale),"freed captured source rejects");
                bool reset=false;f.Actor.FrameChanged+=(_,_,_)=>{if(!reset){reset=true;f.Actor.ResetForCheckpoint();}};
                TestAssert.True(!receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),"old K transaction cannot report acceptance after callback replaces life");
                await f.Frames(2);TestAssert.True(reset&&!Target(f).IsAvailable&&f.Actor.Velocity.Length()<150&&f.Actor.CurrentState!=GraveLampMothState.ThreadStunned,"old pull tail cannot write force/request into new Hidden life");
            });
            await Check("real_void_pending_survives_hidden_sleep_deleted_source",async f=>
            {
                KillZone zone=f.Void(new(0,256,896,16));await Mount(f,ResidentArchetype.Diver,.2f,3);await Manifest(f);f.Health.InvulnerabilitySeconds=1.2f;int deaths=0;f.Actor.Defeated+=()=>deaths++;
                await Strike(f,1,true,1230);Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(0,100));TestAssert.True(Target(f).TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"Manifest down-K accepted");
                await f.Until(()=>f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer==0,40);
                TestAssert.True(f.Health.CurrentHealth==1&&deaths==0&&!Target(f).IsAvailable,"actual swept void latches before Health invulnerability expires, distinct from Hidden");
                zone.QueueFree();f.Agent.SetResidentDormant(true);await f.Until(()=>f.Health.CurrentHealth==0,100);
                TestAssert.True(f.Health.CurrentHealth==0&&deaths==1&&f.Actor.CurrentState==GraveLampMothState.Dead,"pending environmental death bypasses ghost J permission and survives source removal/sleep");
            });
            await Check("pending_void_callback_reset_cannot_kill_new_life",async f=>
            {
                KillZone zone=f.Void(new(0,256,896,16));await Mount(f,ResidentArchetype.Diver,.2f,3);await Manifest(f);f.Health.InvulnerabilitySeconds=1.2f;int deaths=0;f.Actor.Defeated+=()=>deaths++;
                await Strike(f,1,true,1231);Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(0,100));TestAssert.True(Target(f).TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"real down-K creates pending void");
                await f.Until(()=>f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer==0,40);TestAssert.True(f.Health.CurrentHealth==1&&deaths==0,"pending source not yet accepted fatality");
                zone.QueueFree();bool reset=false;f.Actor.FrameChanged+=(_,_,_)=>{if(!reset){reset=true;f.Actor.ResetForCheckpoint();}};await f.Frames(2);
                TestAssert.True(reset&&deaths==0&&f.Health.CurrentHealth==2&&!Target(f).IsAvailable&&f.Actor.CurrentState!=GraveLampMothState.Dead,"old pending cancel callback must not apply lethal damage to explicit new Hidden life");
            });
        }
        GD.Print($"RESIDENT_GHOST_AIR_{suite}_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);try{f.Initialize();await test(f);GD.Print($"RESIDENT_GHOST_AIR_{suite}_CASE_OK {name}");}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print($"RESIDENT_GHOST_AIR_{suite}_CASE_FAIL {name}: {error.Message}");}
            finally{await f.DisposeAsync();}
        }
        static ThreadTarget2D Target(Fixture f)=>f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
        static IThreadInteractionReceiver Receiver(Fixture f)=>f.Actor as IThreadInteractionReceiver??throw new InvalidOperationException("real bool receiver missing");
        static ResidentPopulationDefinition Ghost(ResidentArchetype role,float hidden,float manifest)=>new("ghost.native-test.air.01","ghost.native-test.air",CanyonRouteNodeId.LowCeilingBridge,
            PrologueSectorId.CanyonEntrance,ResidentFamily.Ghost,role,new(new(320,96),new(224,64,320,240),new[]{new Vector2(288,96),new Vector2(352,128)}),new(ResidentBoneWeakness.None,hidden,manifest));
        static async Task Mount(Fixture f,ResidentArchetype role,float hidden,float manifest){f.Agent.ConfigureResidence(Ghost(role,hidden,manifest));await f.Mount(false,roleSpace:true);}
        static async Task Manifest(Fixture f){await f.Until(()=>Target(f).IsAvailable,90);TestAssert.True(Target(f).IsAvailable,"natural finite Manifest opens original target");}
        static async Task Strike(Fixture f,int damage,bool accepted,ulong id)
        {
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(-12,0));hit.Damage=damage;int connections=0;hit.Connected+=_=>connections++;hit.Begin(id);await f.Frames(3);
            using Godot.Collections.Array overlaps=(Godot.Collections.Array)hit.GetOverlappingAreas();TestAssert.True(connections==1||overlaps.Contains(f.Actor.GetNode<Hurtbox>("Hurtbox")),"rejected J actually overlaps native hurtbox");
            hit.End();await f.Frames(2);TestAssert.Equal(accepted?1:0,connections,"native J obeys phase before Health");
        }
    }

    private sealed class Fixture
    {
        private readonly SceneTree _tree;private World2D _world;private SubViewport _view;private RecoveryScope _scope;
        private bool _mounted;private readonly List<Shape2D> _owned=new();
        public GraveLampMoth Actor {get;private set;}public CharacterBody2D Target {get;private set;}
        public AttackPresentationFrame Presentation {get;private set;}
        public int WarningTicksBeforeActive {get;private set;}
        private int _warningTicks;
        public HealthComponent Health=>Actor.GetNode<HealthComponent>("Health");public HealthComponent PlayerHealth=>Target.GetNode<HealthComponent>("Health");
        public Node2D Scope=>_scope;
        public IResidentEnemyAgent Agent=>Actor as IResidentEnemyAgent??throw new InvalidOperationException("Saved Moth has no air residence adapter");
        public Fixture(SceneTree tree){_tree=tree;}
        public void Initialize()
        {
            using Godot.Collections.Array players=(Godot.Collections.Array)_tree.GetNodesInGroup("player");TestAssert.Equal(0,players.Count,"no unrelated player in air fixture");
            _world=new World2D();_view=new SubViewport{Size=new(32,32),World2D=_world};_scope=new RecoveryScope();_view.AddChild(_scope);_tree.Root.AddChild(_view);
            Actor=GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn").Instantiate<GraveLampMoth>();
            Actor.AttackFrameChanged+=frame=>
            {
                if(frame.Timeline.Phase==AttackPhase.Telegraph)
                {
                    if(Presentation.Timeline.Phase!=AttackPhase.Telegraph)_warningTicks=0;
                    _warningTicks++;
                }
                if(frame.Timeline.Phase==AttackPhase.Active&&Presentation.Timeline.Phase!=AttackPhase.Active)WarningTicksBeforeActive=_warningTicks;
                Presentation=frame;
            };
        }
        public void Floor(float top)
        {
            var body=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            body.AddChild(new CollisionShape2D{Position=new(448,top+16),Shape=Own(new RectangleShape2D{Size=new(896,32)})});_scope.AddChild(body);
        }
        public void Wall()
        {
            var body=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            body.AddChild(new CollisionShape2D{Position=new(348,136),Shape=Own(new RectangleShape2D{Size=new(8,144)})});_scope.AddChild(body);
        }
        public StaticBody2D Barrier(Rect2 bounds)
        {
            var body=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            body.AddChild(new CollisionShape2D{Position=bounds.GetCenter(),Shape=Own(new RectangleShape2D{Size=bounds.Size})});_scope.AddChild(body);return body;
        }
        public void AssertRayHits(StaticBody2D expected)
        {
            using var ray=PhysicsRayQueryParameters2D.Create(Actor.GlobalPosition,Target.GlobalPosition,CombatCollisionLayers.World);
            ray.CollideWithAreas=false;ray.CollideWithBodies=true;
            using Godot.Collections.Dictionary hit=Actor.GetWorld2D().DirectSpaceState.IntersectRay(ray);
            TestAssert.True(hit.Count>0&&ReferenceEquals(hit["collider"].AsGodotObject(),expected),"native LOS actually hits the exact introduced World obstruction");
        }
        public CharacterBody2D Player(Vector2 position)
        {
            Target=new CharacterBody2D{Position=position,CollisionLayer=CombatCollisionLayers.Player,CollisionMask=CombatCollisionLayers.World};
            Target.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(20,40)})});Target.AddChild(new HealthComponent{Name="Health",MaxHealth=9});
            var hurt=new Hurtbox{Name="Hurtbox",CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};hurt.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(20,40)})});
            Target.AddChild(hurt);Target.AddToGroup("player");_scope.AddChild(Target);return Target;
        }
        public Node2D Caster(Vector2 position){var caster=new Node2D{Position=position};_scope.AddChild(caster);return caster;}
        public void SavedPlayer(Vector2 position)
        {
            Target=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Target.Position=position;Target.AddToGroup("player");_scope.AddChild(Target);
        }
        public Hitbox IncomingAttack(Vector2 position)
        {
            var hit=new Hitbox{Position=position,Damage=2,Knockback=0,CollisionLayer=CombatCollisionLayers.PlayerAttack,CollisionMask=CombatCollisionLayers.Enemy,Monitorable=false};
            hit.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(60,40)})});_scope.AddChild(hit);return hit;
        }
        public KillZone Void(Rect2 bounds)
        {
            var zone=new KillZone{CollisionLayer=0,CollisionMask=CombatCollisionLayers.Player,RoomControllerPath=new NodePath("..")};
            zone.AddChild(new CollisionShape2D{Position=bounds.GetCenter(),Shape=Own(new RectangleShape2D{Size=bounds.Size})});_scope.AddChild(zone);return zone;
        }
        public async Task Mount(bool configured=true,bool dive=false,bool roleSpace=false)
        {
            Actor.Position=dive||roleSpace?new(320,96):new(320,128);
            if(configured)Agent.ConfigureResidence(new("native-air.01","native-air.moth",CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,
                ResidentFamily.Native,dive?ResidentArchetype.Diver:ResidentArchetype.Flyer,
                new(Actor.Position,roleSpace?new(224,64,320,240):dive?new(224,64,256,240):new(256,64,128,144),
                    dive||roleSpace?new[]{new Vector2(288,96),new Vector2(352,128)}:new[]{new Vector2(288,96),new Vector2(352,160)})));
            _scope.AddChild(Actor);_mounted=true;await Frames(6);
            TestAssert.True(Actor.GetWorld2D()==_world&&_world!=_tree.Root.FindWorld2D(),"actual saved body in independently owned World2D");
        }
        public async Task Frames(int count){for(int i=0;i<count;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}}
        public async Task Until(Func<bool> predicate,int count){for(int i=0;i<count&&!predicate();i++)await Frames(1);}
        public async Task<(Vector2 min,Vector2 max,float step,bool returned)> Trace(int count,Vector2? home=null)
        {
            Vector2 min=Actor.GlobalPosition,max=min,last=min;float step=0;bool returned=false,departed=false;
            for(int i=0;i<count;i++){await Frames(1);Vector2 p=Actor.GlobalPosition;min=min.Min(p);max=max.Max(p);step=MathF.Max(step,p.DistanceTo(last));last=p;
                if(home.HasValue){departed|=p.DistanceTo(home.Value)>32;returned|=departed&&p.DistanceTo(home.Value)<3;}}
            GD.Print($"RESIDENT_AIR_TRACE min={min} max={max} max_step={step} returned={returned} final={Actor.GlobalPosition}");return(min,max,step,returned);
        }
        private T Own<T>(T shape)where T:Shape2D{_owned.Add(shape);return shape;}
        public async Task DisposeAsync()
        {
            if(!_mounted&&GodotObject.IsInstanceValid(Actor))Actor.Free();
            if(GodotObject.IsInstanceValid(_view))_view.QueueFree();await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            bool gone=!GodotObject.IsInstanceValid(Actor)&&!GodotObject.IsInstanceValid(Target)&&!GodotObject.IsInstanceValid(_scope)&&!GodotObject.IsInstanceValid(_view);
            int allocated=_owned.Count,disposed=0;foreach(Shape2D shape in _owned){shape.Dispose();disposed++;}_owned.Clear();_world?.Dispose();
            Actor=null;Target=null;_scope=null;_view=null;_world=null;
            GD.Print($"RESIDENT_AIR_TEARDOWN native_gone={gone} owned_allocated={allocated} owned_disposed={disposed} borrowed_disposed=0");
            TestAssert.True(gone&&allocated==disposed,"owned air fixture releases native graph before its own shapes/world");
        }
    }
    private partial class RecoveryScope:Node2D,ILevelRecovery{public void RequestRespawn(string reason)=>throw new InvalidOperationException("NPC must not request player recovery: "+reason);}
}
