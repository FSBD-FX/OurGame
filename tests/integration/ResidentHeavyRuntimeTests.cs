using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Enemies.Residents;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.World;

namespace Knight.Tests.Integration;

// Saved heavy body/armor/sweep in native physics; not final family/placement proof.
public static partial class ResidentHeavyRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("unconfigured_idle_remains_stationary",async f=>
        {
            await f.Mount(false);Vector2 before=f.Actor.GlobalPosition;await f.Frames(60);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Idle&&f.Actor.GlobalPosition.DistanceTo(before)<.1f,
                "saved old heavy without a player stays Idle, not opted into patrol");
        });
        await Check("actual_heavy_patrol_travels_both_directions",async f=>
        {
            await f.Mount();var t=await f.Trace(360);
            TestAssert.True(t.min<262&&t.max>378&&t.step<1.5f&&f.Actor.IsOnFloor()&&f.Health.CurrentHealth==6,
                $"real saved radius28/height84 heavy patrols continuously with gravity and support {t}");
        });
        await Check("eighty_pixel_floor_turns_complete_capsule_before_ledge",async f=>
        {
            f.FloorBounds=new(280,128,80,40);await f.Mount();var t=await f.Trace(240);
            TestAssert.True(t.min>=308&&t.max<=332&&t.min<315&&t.max>325&&f.Actor.IsOnFloor(),
                $"full width56 support turns visibly inside the narrow real platform {t}");
        });
        await Check("center_only_support_does_not_authorize_autonomous_step",async f=>
        {
            f.FloorBounds=new(304,128,32,40);await f.Mount();Vector2 before=f.Actor.GlobalPosition;await f.Frames(90);
            TestAssert.True(f.Actor.IsOnFloor()&&MathF.Abs(f.Actor.GlobalPosition.X-before.X)<.1f,
                "native capsule center contact is not a full-width autonomous support certificate");
        });
        await Check("real_approach_then_target_departure_returns_home_before_patrol",async f=>
        {
            f.Player(new(430,86));await f.Mount();await f.Until(()=>f.Actor.GlobalPosition.X>334,45);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Approach&&f.Actor.GlobalPosition.X>334,
                "actual qualified target causes native approach away from home");
            f.Target.MoveAndCollide(new(140,0));bool stoppedAtHome=false;float step=0;Vector2 last=f.Actor.GlobalPosition;
            for(int i=0;i<180;i++){await f.Frames(1);Vector2 now=f.Actor.GlobalPosition;step=MathF.Max(step,now.DistanceTo(last));last=now;
                if(MathF.Abs(now.X-320)<3&&MathF.Abs(f.Actor.Velocity.X)<.1f){stoppedAtHome=true;break;}
                TestAssert.True(now.X>300,"return must visit home before continuing toward the left patrol endpoint");}
            TestAssert.True(stoppedAtHome&&step<1.5f&&f.Health.CurrentHealth==6,"return is physical, stops at actual home, and never resets HP");
        });
        await Check("locked_target_leaves_territory_and_old_sweep_is_revoked",async f=>
        {
            f.Player(new(390,86));await f.Mount();await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Telegraph,"real heavy earns a locked warning");
            ulong old=f.Presentation.Timeline.AttackId;int active=0;f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)active++;};
            f.Target.MoveAndCollide(new(180,0));await f.Frames(90);
            TestAssert.True(active==0&&f.Presentation.Timeline.Phase==AttackPhase.None&&f.PlayerHealth.CurrentHealth==9&&
                !f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),
                "opt-in heavy withdraws locked territorial authority, unlike unconfigured old committed-sweep policy");
        });
        await Check("armor_remainder_survives_sleep_and_real_heavy_claw_opens_only_armor",async f=>
        {
            await f.Mount();Hurtbox hurt=f.Actor.GetNode<Hurtbox>("Hurtbox");
            TestAssert.True(hurt.Receive(new(1,Vector2.Zero,0,401,902))&&hurt.Receive(new(1,Vector2.Zero,0,402,902))&&f.Health.CurrentHealth==6,
                "two actual guarded one-point hits retain an armor remainder");
            f.Agent.SetResidentDormant(true);TestAssert.True(!hurt.Receive(new(1,Vector2.Zero,0,403,902)),"sleep rejects damage before armor accumulator consumption");
            f.Agent.SetResidentDormant(false);TestAssert.True(hurt.Receive(new(1,Vector2.Zero,0,404,902))&&f.Health.CurrentHealth==5,
                "first legal wake hit completes exactly the original three-hit armor accumulation");
            Vector2 before=f.Actor.GlobalPosition;Node2D caster=f.Caster(before+new Vector2(-100,0));
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
            TestAssert.True(adapter.TryApply(caster,caster.GlobalPosition),"actual saved Heavy legacy adapter accepts the claw interaction");await f.Frames(1);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Exposed&&adapter.ApplicationCount==1&&MathF.Abs(f.Actor.GlobalPosition.X-before.X)<.1f,
                "heavy claw opens armor without moving/pulling the heavy body");
            TestAssert.True(hurt.Receive(new(1,Vector2.Zero,0,405,902))&&f.Health.CurrentHealth==4,"real Exposed hit bypasses armor without replacing its accumulator");
            await f.Frames(80);TestAssert.True(f.Actor.CurrentState!=BellBackLaborerState.Exposed&&f.Health.CurrentHealth==4,"existing1.20s exposed window expires without healing");
        });
        await Check("native_active_sleep_withdraws_all_direct_authority_and_fresh_wake",async f=>
        {
            f.Player(new(390,86));await f.Mount();await f.Until(()=>f.Actor.CurrentState==BellBackLaborerState.Attack,110);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Attack,"sleep begins in a real convex sweep Active");
            ulong old=f.Presentation.Timeline.AttackId;f.Agent.SetResidentDormant(true);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox"))&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,600,406,902))&&
                !target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings()),"sleep rejects direct outgoing/incoming/Heavy-claw authority");
            TestAssert.True(!target.IsInGroup(ThreadTarget2D.TargetGroup)&&!target.IsInGroup(GrappleSurface2D.TargetGroup)&&target.GetNode<Area2D>("TargetCollision").CollisionLayer==0&&
                f.Presentation.Timeline.Phase==AttackPhase.None,"sleep withdraws discovery and presentation immediately");
            await f.Frames(10);f.Agent.SetResidentDormant(false);await f.Until(()=>f.Actor.CurrentState==BellBackLaborerState.Telegraph,180);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Telegraph&&f.Presentation.Timeline.AttackId>old&&
                f.Presentation.Timeline.PhaseRemainingSeconds>.70f&&f.Health.CurrentHealth==6,"wake preserves HP and needs a fresh full warning/identity");
        });
        await Check("real_armored_health_damage_impulse_then_physical_return",async f=>
        {
            await f.Mount();Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,600,407,902))&&f.Health.CurrentHealth==5,
                "actual armor reduction reaches Health with a real impulse");await f.Frames(3);
            TestAssert.True(f.Actor.GlobalPosition.X>before.X+15,"autonomous heavy intent cannot overwrite the actual Health impulse");
            bool returned=false;float step=0;Vector2 last=f.Actor.GlobalPosition;
            for(int i=0;i<240;i++){await f.Frames(1);Vector2 now=f.Actor.GlobalPosition;step=MathF.Max(step,now.DistanceTo(last));last=now;
                if(MathF.Abs(now.X-320)<3&&MathF.Abs(f.Actor.Velocity.X)<.1f){returned=true;break;}}
            TestAssert.True(returned&&step<11&&f.Health.CurrentHealth==5,"hurt recovery physically visits home without hard clamp or healing");
        });
        await Check("native_lethal_callback_reset_then_second_life_dies_once",async f=>
        {
            await f.Mount();int defeats=0,connections=0;bool reset=false;f.Actor.Defeated+=()=>defeats++;
            f.Actor.FrameChanged+=(frame,_)=>{if(frame.State==BellBackLaborerState.Dead&&!reset){reset=true;f.Actor.ResetForCheckpoint();}};
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition);hit.Connected+=_=>connections++;hit.Begin(501);await f.Until(()=>connections==1,12);await f.Frames(2);
            TestAssert.True(reset&&defeats==1&&connections==1&&f.Health.CurrentHealth==6,"native armored lethal callback can explicitly reset exactly its first life");
            hit.End();await f.Frames(2);hit.Begin(502);await f.Until(()=>connections==2,12);await f.Frames(1);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");f.Agent.SetResidentDormant(true);f.Agent.SetResidentDormant(false);await f.Frames(2);
            TestAssert.True(connections==2&&defeats==2&&f.Health.CurrentHealth==0&&f.Actor.CurrentState==BellBackLaborerState.Dead&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable&&!target.IsAvailable&&target.GetNode<Area2D>("TargetCollision").CollisionLayer==0,
                "old death publication cannot poison new life, and sleep/wake cannot resurrect a corpse");
        });
        GD.Print($"RESIDENT_HEAVY_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_HEAVY_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_HEAVY_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
    }
    public static async Task RunBoundaries(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("real_impulse_leaves_whole_territory_then_returns_without_reset",async f=>
        {
            await f.Mount();int defeats=0;f.Actor.Defeated+=()=>defeats++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,1800,601,903))&&f.Health.CurrentHealth==5,
                "actual Armor to Health applies the controlled1800 impulse, not a Heavy-claw pull");
            float max=f.Actor.GlobalPosition.X,step=0;bool outside=false,returned=false;Vector2 last=f.Actor.GlobalPosition;
            for(int i=0;i<420;i++){await f.Frames(1);Vector2 p=f.Actor.GlobalPosition;max=MathF.Max(max,p.X);step=MathF.Max(step,p.DistanceTo(last));last=p;
                outside|=p.X>420;if(outside&&MathF.Abs(p.X-320)<3&&MathF.Abs(f.Actor.Velocity.X)<.1f){returned=true;break;}}
            GD.Print($"RESIDENT_HEAVY_FORCE max_x={max} max_step={step} outside={outside} returned={returned} hp={f.Health.CurrentHealth}");
            TestAssert.True(outside&&returned&&step<=30.1f&&defeats==0&&f.Health.CurrentHealth==5&&f.Actor.IsOnFloor(),
                "real full body leaves x448 territory and physically returns over supported floor without clamp/teleport/heal");
        });
        await Check("vertical_health_impulse_is_applied_once_then_gravity_continues",async f=>
        {
            await f.Mount();Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Up,600,602,903)),"real armored Health receives upward impulse");
            TestAssert.True(f.Actor.Velocity.Y< -590&&f.Actor.Velocity.Y> -610,"single accepted vertical impulse is immediately observable");
            var rising=await f.Trace(10);TestAssert.True(f.Actor.Velocity.Y> -450&&f.Actor.Velocity.Y< -420&&f.Actor.GlobalPosition.Y<before.Y-70,
                "ten real ticks add980 gravity, not the same upward impulse repeatedly");
            var falling=await f.Trace(80);TestAssert.True(f.Actor.IsOnFloor()&&f.Health.CurrentHealth==5&&MathF.Max(rising.step,falling.step)<=10.1f,
                "every observed600 vertical-impulse tick stays continuous and the real body lands without resetting pose or Health");
        });
        await Check("sleep_retains_accepted_health_impulse_but_rejects_new_damage",async f=>
        {
            await f.Mount();Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,600,603,903)),"real pre-sleep Health impulse accepted");
            f.Agent.SetResidentDormant(true);var asleep=await f.Trace(10);
            TestAssert.True(f.Actor.GlobalPosition.X>before.X+30&&f.Health.CurrentHealth==5&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,600,604,903)),"sleep preserves carried physical force but rejects new incoming authority");
            f.Agent.SetResidentDormant(false);bool home=false;float maxStep=asleep.step;Vector2 last=f.Actor.GlobalPosition;
            for(int i=0;i<240;i++){await f.Frames(1);Vector2 now=f.Actor.GlobalPosition;maxStep=MathF.Max(maxStep,now.DistanceTo(last));last=now;
                if(MathF.Abs(now.X-320)<3&&MathF.Abs(f.Actor.Velocity.X)<.1f){home=true;break;}}
            TestAssert.True(home&&f.Health.CurrentHealth==5&&maxStep<=10.1f,"sleep and wake keep every observed600 impulse tick continuous and physically return without Health reset");
        });
        await Check("native_knockoff_enters_zero_layer_void_and_dies_once",async f=>
        {
            f.FloorBounds=new(280,128,80,40);KillZone zone=f.Void(new(360,208,256,16));await f.Mount();int deaths=0,defeats=0;
            f.Health.Died+=()=>deaths++;f.Actor.Defeated+=()=>defeats++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,1800,605,903)),"real knockoff starts from complete supported heavy body");
            bool contact=false,airborne=false;
            for(int i=0;i<100&&f.Health.CurrentHealth>0;i++){Transform2D before=f.BodyTransform;await f.Frames(1);airborne|=!f.Actor.IsOnFloor();contact|=f.BodyTouchesDuring(before,zone);}
            TestAssert.True(zone.CollisionLayer==0&&airborne&&contact&&f.Actor.GlobalPosition.Y>150&&f.Health.CurrentHealth==0&&deaths==1&&defeats==1&&f.RecoveryCalls==0,
                "actual shape contact, not a home-height threshold, kills the displaced Heavy exactly once without player recovery");
            await f.Frames(10);TestAssert.True(deaths==1&&defeats==1,"continued overlap cannot replay an environmental death");
        });
        await Check("real_void_pending_survives_health_invulnerability_sleep_and_source_removal",async f=>
        {
            f.FloorBounds=new(280,128,80,40);KillZone zone=f.Void(new(360,208,256,16));await f.Mount();f.Health.InvulnerabilitySeconds=2;
            int deaths=0,defeats=0;f.Health.Died+=()=>deaths++;f.Actor.Defeated+=()=>defeats++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,1800,606,903)),"actual Health damage starts invulnerability and physical knockoff");
            bool contact=false;ThreadTarget2D target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            for(int i=0;i<100&&target.IsAvailable;i++){Transform2D before=f.BodyTransform;await f.Frames(1);contact|=f.BodyTouchesDuring(before,zone);}
            TestAssert.True(contact&&f.Health.CurrentHealth==5&&deaths==0&&defeats==0&&!target.IsAvailable&&
                target.GetNode<Area2D>("TargetCollision").CollisionLayer==0&&f.Presentation.Timeline.Phase==AttackPhase.None,
                "true void contact withdraws authority without bypassing real Health invulnerability");
            zone.QueueFree();f.Agent.SetResidentDormant(true);await f.Until(()=>f.Health.CurrentHealth==0,150);
            TestAssert.True(f.Health.CurrentHealth==0&&deaths==1&&defeats==1&&f.Actor.CurrentState==BellBackLaborerState.Dead&&f.RecoveryCalls==0,
                "latched environment death survives source destruction and sleep until one actual accepted Health death");
        });
        await Check("pending_actor_unload_does_not_kill_fresh_same_definition_instance",async f=>
        {
            f.FloorBounds=new(280,128,80,40);KillZone zone=f.Void(new(360,208,256,16));await f.Mount();f.Health.InvulnerabilitySeconds=2;
            int oldDefeats=0;f.Actor.Defeated+=()=>oldDefeats++;BellBackLaborer old=f.Actor;ulong floor=f.FloorInstanceId;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(3,Vector2.Right,1800,607,903)),"old instance receives real pending-void knockoff");
            bool contact=false;var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            for(int i=0;i<100&&target.IsAvailable;i++){Transform2D before=f.BodyTransform;await f.Frames(1);contact|=f.BodyTouchesDuring(before,zone);}
            TestAssert.True(contact&&!target.IsAvailable&&f.Health.CurrentHealth==5&&oldDefeats==0,"old pending instance is alive before unload");
            old.QueueFree();await f.Frames(2);TestAssert.True(!GodotObject.IsInstanceValid(old)&&oldDefeats==0,"unload is not a Defeated event");
            await f.RemountFreshActor();int newDefeats=0;f.Actor.Defeated+=()=>newDefeats++;await f.Frames(180);
            TestAssert.True(f.FloorInstanceId==floor&&f.Health.CurrentHealth==6&&oldDefeats==0&&newDefeats==0&&
                f.Actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable,"old instance callbacks cannot kill a fresh same-definition actor on the same real floor");
        });
        await Check("invalid_complete_body_and_postmount_reconfiguration_are_atomic",async f=>
        {
            bool invalid=false;try{f.Agent.ConfigureResidence(new("invalid-heavy","scrap-heavy.laborer",CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,
                ResidentFamily.Scrap,ResidentArchetype.Heavy,new(new(320,86),new(320,32,128,144),new[]{new Vector2(352,86),new Vector2(400,86)})));}catch(ArgumentException){invalid=true;}
            TestAssert.True(invalid,"full radius28 body, not center-only placement, must fit before mounting");await f.Mount();bool mounted=false;
            try{f.Agent.ConfigureResidence(null);}catch(InvalidOperationException){mounted=true;}
            var t=await f.Trace(120);TestAssert.True(mounted&&t.min<290&&f.Health.CurrentHealth==6,"failed configuration does not latch partial state or permit later reconfiguration/reset");
        });
        await Check("foreign_world_target_rejected_then_real_local_target_attacked",async f=>
        {
            var foreign=new Fixture(tree);
            try
            {
                foreign.Initialize();foreign.Player(new(390,86));await f.Mount();
                TestAssert.True(foreign.Target.GetWorld2D()!=f.Actor.GetWorld2D()&&foreign.PlayerHealth.CurrentHealth==9,"same-coordinate living decoy is natively in a different World2D");
                bool combat=Combat(f.Actor.CurrentState);f.Actor.FrameChanged+=(frame,_)=>combat|=Combat(frame.State);await f.Frames(90);
                TestAssert.True(!combat&&foreign.PlayerHealth.CurrentHealth==9,"foreign target cannot authorize Guard/warning/sweep");
                f.Player(new(390,86));await f.Until(()=>f.PlayerHealth.CurrentHealth<9,180);
                TestAssert.True(f.PlayerHealth.CurrentHealth<9&&foreign.PlayerHealth.CurrentHealth==9,"same-world positive control reaches native sweep damage without touching foreign body");
            }
            finally{await foreign.DisposeAsync();}
        });
        await Check("dead_target_keeps_native_body_and_group_but_cannot_authorize_combat",async f=>
        {
            f.Player(new(390,86));TestAssert.True(f.PlayerHealth.ApplyDamage(new(9,Vector2.Zero,0,608,903)),"target Health really dies before perception");await f.Mount();
            bool combat=Combat(f.Actor.CurrentState);f.Actor.FrameChanged+=(frame,_)=>combat|=Combat(frame.State);await f.Frames(90);
            TestAssert.True(f.Target.IsInsideTree()&&f.Target.IsInGroup("player")&&f.PlayerHealth.CurrentHealth==0&&!combat,"corpse retains native body/group without authorizing combat");
        });
        await Check("real_world_obstruction_prevents_initial_combat",async f=>
        {
            f.Player(new(390,86));StaticBody2D wall=f.Barrier(new(352,32,8,104));await f.Mount();f.AssertRayHits(wall);
            bool combat=Combat(f.Actor.CurrentState);f.Actor.FrameChanged+=(frame,_)=>combat|=Combat(frame.State);await f.Frames(90);
            TestAssert.True(!combat&&f.PlayerHealth.CurrentHealth==9,"exact native World obstruction blocks initial Guard/warning/Active, not only damage");
        });
        await Check("locked_world_sight_loss_revokes_old_sweep_before_active",async f=>
        {
            f.Player(new(390,86));await f.Mount();await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Telegraph,"real warning is locked before adding a wall");
            ulong old=f.Presentation.Timeline.AttackId;int active=0;f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)active++;};
            StaticBody2D wall=f.Barrier(new(352,32,8,104));await f.Frames(2);f.AssertRayHits(wall);await f.Frames(90);
            TestAssert.True(active==0&&f.PlayerHealth.CurrentHealth==9&&f.Presentation.Timeline.Phase==AttackPhase.None&&
                !f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),"opt-in locked LOS loss cancels the actual attack rather than merely colliding with a wall");
        });
        await Check("locked_target_death_revokes_old_sweep_with_body_and_group_retained",async f=>
        {
            f.Player(new(390,86));await f.Mount();await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Telegraph,"native locked warning exists");
            ulong old=f.Presentation.Timeline.AttackId;int active=0,deaths=0;f.PlayerHealth.Died+=()=>deaths++;f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)active++;};
            TestAssert.True(f.PlayerHealth.ApplyDamage(new(9,Vector2.Zero,0,609,903)),"real Health death after lock");await f.Frames(90);
            TestAssert.True(f.Target.IsInsideTree()&&f.Target.IsInGroup("player")&&deaths==1&&active==0&&f.Presentation.Timeline.Phase==AttackPhase.None&&
                !f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),"dead locked target cannot retain sweep authority");
        });
        await Check("another_live_player_cannot_authorize_the_departed_locked_target",async f=>
        {
            f.Player(new(390,86));CharacterBody2D original=f.Target;await f.Mount();await f.Until(()=>f.Presentation.Timeline.HasLockedTarget,100);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Telegraph,"original target has locked warning");
            ulong old=f.Presentation.Timeline.AttackId;int active=0,newActive=0;HealthComponent originalHealth=f.PlayerHealth;
            f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.Phase==AttackPhase.Active){if(x.Timeline.AttackId==old)active++;else if(x.Timeline.AttackId!=0)newActive++;}};
            f.Player(new(360,86));original.MoveAndCollide(new(180,0));await f.Frames(1);
            TestAssert.True(active==0&&f.Presentation.Timeline.Phase==AttackPhase.None&&f.Actor.CurrentState!=BellBackLaborerState.Attack,
                "a different eligible living body cannot keep the departed target's old lock alive");
            await f.Frames(90);TestAssert.True(active==0&&newActive>0&&originalHealth.CurrentHealth==9,
                "the old identity never reactivates through its full window while a genuinely fresh attack may target the new living player");
        });
        GD.Print($"RESIDENT_HEAVY_BOUNDARY_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_HEAVY_BOUNDARY_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_HEAVY_BOUNDARY_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static bool Combat(BellBackLaborerState state)=>state is BellBackLaborerState.Guard or BellBackLaborerState.Telegraph or BellBackLaborerState.Attack or BellBackLaborerState.Recover;
    }

    public static async Task RunBone(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("protected_front_rejects_before_rear_armor_accumulation",async f=>
        {
            await f.Mount(profile:new(ResidentBoneWeakness.RearOrExposed));
            for(int i=0;i<3;i++)await Strike(f,true,false,(ulong)(710+i));
            TestAssert.Equal(6,f.Health.CurrentHealth,"three rejected native front hits cannot consume armor remainder or HP");
            await Strike(f,false,true,720);await Strike(f,false,true,721);
            TestAssert.Equal(6,f.Health.CurrentHealth,"only two permitted rear hits have reached the actual divisor-three armor");
            await Strike(f,false,true,722);TestAssert.Equal(5,f.Health.CurrentHealth,"third actual rear hit deals exactly one point, not contaminated by rejected fronts");
        });
        await Check("actual_heavy_claw_opens_front_then_pose_window_expires",async f=>
        {
            await f.Mount(profile:new(ResidentBoneWeakness.RearOrExposed));
            Vector2 before=f.Actor.GlobalPosition;Node2D caster=f.Caster(before+new Vector2(-100,0));
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
            TestAssert.True(adapter.TryApply(caster,caster.GlobalPosition),"real Heavy K transaction accepts the pose interruption");await f.Frames(1);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Exposed&&MathF.Abs(f.Actor.GlobalPosition.X-before.X)<.1f,
                "actual model opens Exposed without pulling the Heavy body");
            await Strike(f,true,true,730);TestAssert.Equal(5,f.Health.CurrentHealth,"native front J deals full one-point damage only in the actual exposed pose");
            await f.Frames(80);TestAssert.True(f.Actor.CurrentState!=BellBackLaborerState.Exposed,"real1.20s pose window expires without model injection");
            await Strike(f,true,false,731);TestAssert.Equal(5,f.Health.CurrentHealth,"closed pose protects the front again without healing");
        });
        await Check("sleep_overrides_exposed_K_and_preserves_rear_remainder",async f=>
        {
            await f.Mount(profile:new(ResidentBoneWeakness.RearOrExposed));
            await Strike(f,false,true,740);await Strike(f,false,true,741);TestAssert.Equal(6,f.Health.CurrentHealth,"two legal native rear hits retain only the armor remainder");
            Node2D caster=f.Caster(f.Actor.GlobalPosition+new Vector2(-100,0));
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");f.Agent.SetResidentDormant(true);
            await Strike(f,false,false,742);TestAssert.True(!target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new ThreadAbilitySettings()),
                "captured real K target is rejected while asleep");
            f.Agent.SetResidentDormant(false);await f.Frames(1);
            await Strike(f,false,true,743);TestAssert.Equal(5,f.Health.CurrentHealth,"wake keeps the exact original rear remainder and reduced health");
            TestAssert.True(target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new ThreadAbilitySettings()),"awake K still opens the pose");await f.Frames(1);
            TestAssert.True(f.Actor.CurrentState==BellBackLaborerState.Exposed,"actual exposed pose precedes sleep override");
            f.Agent.SetResidentDormant(true);await Strike(f,true,false,744);
            TestAssert.True(!target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new ThreadAbilitySettings())&&f.Health.CurrentHealth==5,
                "even an exposed pose cannot bypass sleeping J/K authority");
        });
        await Check("invalid_family_profile_rejects_before_any_residence_commit",async f=>
        {
            var placement=new ResidentPlacementCandidate(new(320,86),new(192,32,256,144),new[]{new Vector2(256,86),new Vector2(384,86)});
            var valid=new ResidentPopulationDefinition("bone-heavy.01","bone.guard",CanyonRouteNodeId.LowCeilingBridge,
                PrologueSectorId.CanyonEntrance,ResidentFamily.Bone,ResidentArchetype.Heavy,placement,new(ResidentBoneWeakness.RearOrExposed));
            foreach(var invalid in new[]{valid with {Family=ResidentFamily.Native},valid with {CombatProfile=new()},
                valid with {CombatProfile=new((ResidentBoneWeakness)99)},valid with {CombatProfile=null},
                valid with {CombatProfile=new(ResidentBoneWeakness.RearOrExposed,.6f,.8f)}})
            {
                bool rejected=false;try{f.Agent.ConfigureResidence(invalid);}catch(ArgumentException){rejected=true;}
                TestAssert.True(rejected,"incompatible, empty, unknown or missing Bone profile cannot partially commit residence");
            }
            await f.Mount(profile:new(ResidentBoneWeakness.RearOrExposed));await Strike(f,true,false,750);
            TestAssert.Equal(6,f.Health.CurrentHealth,"same actor accepts a later valid immutable profile and enforces it in native damage");
        });
        GD.Print($"RESIDENT_BONE_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_BONE_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_BONE_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static async Task Strike(Fixture f,bool front,bool accepted,ulong id)
        {
            int facing=f.Actor.Facing;Vector2 position=f.Actor.GlobalPosition+new Vector2((front?1:-1)*facing*20,0);
            Hitbox hit=f.IncomingAttack(position,1);int connections=0;hit.Connected+=_=>connections++;
            hit.AreaEntered+=area=>{if(ReferenceEquals(area,f.Actor.GetNode<Hurtbox>("Hurtbox")))
                GD.Print($"RESIDENT_BONE_NATIVE_CONTACT id={id} initial_facing={facing} actual_facing={f.Actor.Facing} direction={hit.GlobalPosition.DirectionTo(area.GlobalPosition)} pose={f.Actor.CurrentState}");};
            hit.Begin(id);await f.Frames(3);hit.End();await f.Frames(2);
            GD.Print($"RESIDENT_BONE_STRIKE id={id} front={front} facing={facing} connections={connections} hp={f.Health.CurrentHealth} pose={f.Actor.CurrentState}");
            TestAssert.Equal(accepted?1:0,connections,"actual AreaEntered→Hitbox→Hurtbox→actor permission, not a direct fake Health call");
        }
    }

    private sealed class Fixture
    {
        private readonly SceneTree _tree;private World2D _world;private SubViewport _view;private Node2D _scope;
        private readonly List<Shape2D> _owned=new();private bool _mounted;private StaticBody2D _floor;
        public BellBackLaborer Actor {get;private set;}public CharacterBody2D Target {get;private set;}
        public Rect2 FloorBounds=new(32,128,640,40);public AttackPresentationFrame Presentation {get;private set;}
        public HealthComponent Health=>Actor.GetNode<HealthComponent>("Health");public HealthComponent PlayerHealth=>Target.GetNode<HealthComponent>("Health");
        public IResidentEnemyAgent Agent=>Actor as IResidentEnemyAgent??throw new InvalidOperationException("Saved Heavy has no residence adapter");
        public Transform2D BodyTransform=>Actor.GetNode<CollisionShape2D>("BodyCollider").GlobalTransform;
        public int RecoveryCalls=>((RecoveryScope)_scope).Calls;
        public ulong FloorInstanceId=>_floor.GetInstanceId();
        public Fixture(SceneTree tree){_tree=tree;}
        public void Initialize()
        {
            using Godot.Collections.Array players=(Godot.Collections.Array)_tree.GetNodesInGroup("player");TestAssert.Equal(0,players.Count,"no unrelated player in heavy fixture");
            _world=new World2D();_view=new SubViewport{Size=new(32,32),World2D=_world};_scope=new RecoveryScope();_view.AddChild(_scope);_tree.Root.AddChild(_view);
            Actor=GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn").Instantiate<BellBackLaborer>();Actor.AttackFrameChanged+=x=>Presentation=x;
        }
        public async Task Mount(bool configured=true,ResidentCombatProfile profile=null)
        {
            if(!GodotObject.IsInstanceValid(_floor))_floor=Barrier(FloorBounds);
            Actor.Position=new(320,86);
            if(configured)Agent.ConfigureResidence(new("scrap-heavy.01","scrap-heavy.laborer",CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,
                profile==null?ResidentFamily.Scrap:ResidentFamily.Bone,ResidentArchetype.Heavy,new(Actor.Position,new(192,32,256,144),new[]{new Vector2(256,86),new Vector2(384,86)}),profile));
            _scope.AddChild(Actor);_mounted=true;await Frames(6);
            TestAssert.True(Actor.IsOnFloor()&&Actor.GetWorld2D()==_world&&_world!=_tree.Root.FindWorld2D(),"saved full heavy capsule actually stands in independent World2D");
        }
        public void Player(Vector2 position)
        {
            Target=new CharacterBody2D{Position=position,CollisionLayer=CombatCollisionLayers.Player,CollisionMask=CombatCollisionLayers.World};
            Target.AddChild(new CollisionShape2D{Shape=Own(new CapsuleShape2D{Radius=14,Height=84})});Target.AddChild(new HealthComponent{Name="Health",MaxHealth=9});
            var hurt=new Hurtbox{Name="Hurtbox",CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};hurt.AddChild(new CollisionShape2D{Shape=Own(new CapsuleShape2D{Radius=14,Height=84})});
            Target.AddChild(hurt);Target.AddToGroup("player");_scope.AddChild(Target);
        }
        public Node2D Caster(Vector2 position){var caster=new Node2D{Position=position};_scope.AddChild(caster);return caster;}
        public StaticBody2D Barrier(Rect2 bounds)
        {
            var body=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            body.AddChild(new CollisionShape2D{Position=bounds.GetCenter(),Shape=Own(new RectangleShape2D{Size=bounds.Size})});_scope.AddChild(body);return body;
        }
        public KillZone Void(Rect2 bounds)
        {
            var zone=new KillZone{CollisionLayer=0,CollisionMask=CombatCollisionLayers.Player,Monitoring=true,RoomControllerPath=new NodePath("..")};
            zone.AddChild(new CollisionShape2D{Name="Collision",Position=bounds.GetCenter(),Shape=Own(new RectangleShape2D{Size=bounds.Size})});_scope.AddChild(zone);return zone;
        }
        public bool BodyTouchesDuring(Transform2D before,KillZone zone)
        {
            CollisionShape2D body=Actor.GetNode<CollisionShape2D>("BodyCollider"),source=zone.GetNode<CollisionShape2D>("Collision");Transform2D after=body.GlobalTransform;
            return body.Shape.Collide(before,source.Shape,source.GlobalTransform)||body.Shape.CollideWithMotion(before,after.Origin-before.Origin,source.Shape,source.GlobalTransform,Vector2.Zero)||body.Shape.Collide(after,source.Shape,source.GlobalTransform);
        }
        public void AssertRayHits(StaticBody2D expected)
        {
            using var query=PhysicsRayQueryParameters2D.Create(Actor.GlobalPosition,Target.GlobalPosition,CombatCollisionLayers.World);query.CollideWithAreas=false;query.CollideWithBodies=true;
            using Godot.Collections.Dictionary hit=Actor.GetWorld2D().DirectSpaceState.IntersectRay(query);
            TestAssert.True(hit.Count>0&&ReferenceEquals(hit["collider"].AsGodotObject(),expected),"native target LOS hits the exact introduced World body");
        }
        public async Task RemountFreshActor()
        {
            TestAssert.True(!GodotObject.IsInstanceValid(Actor),"original native actor must be destroyed before replacement");
            Actor=GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn").Instantiate<BellBackLaborer>();Actor.AttackFrameChanged+=x=>Presentation=x;_mounted=false;await Mount();
        }
        public Hitbox IncomingAttack(Vector2 position,int damage=18)
        {
            var hit=new Hitbox{Position=position,Damage=damage,Knockback=0,CollisionLayer=CombatCollisionLayers.PlayerAttack,CollisionMask=CombatCollisionLayers.Enemy,Monitorable=false};
            hit.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(64,88)})});_scope.AddChild(hit);return hit;
        }
        public async Task Frames(int count){for(int i=0;i<count;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}}
        public async Task Until(Func<bool> predicate,int count){for(int i=0;i<count&&!predicate();i++)await Frames(1);}
        public async Task<(float min,float max,float step)> Trace(int count)
        {
            float min=Actor.GlobalPosition.X,max=min,step=0;Vector2 last=Actor.GlobalPosition;
            for(int i=0;i<count;i++){await Frames(1);Vector2 now=Actor.GlobalPosition;min=MathF.Min(min,now.X);max=MathF.Max(max,now.X);step=MathF.Max(step,last.DistanceTo(now));last=now;}
            GD.Print($"RESIDENT_HEAVY_TRACE min_x={min} max_x={max} max_step={step} final={Actor.GlobalPosition}");return(min,max,step);
        }
        private T Own<T>(T shape)where T:Shape2D{_owned.Add(shape);return shape;}
        public async Task DisposeAsync()
        {
            if(!_mounted&&GodotObject.IsInstanceValid(Actor))Actor.Free();if(GodotObject.IsInstanceValid(_view))_view.QueueFree();
            await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            bool gone=!GodotObject.IsInstanceValid(Actor)&&!GodotObject.IsInstanceValid(Target)&&!GodotObject.IsInstanceValid(_scope)&&!GodotObject.IsInstanceValid(_view);
            int allocated=_owned.Count,disposed=0;foreach(Shape2D shape in _owned){shape.Dispose();disposed++;}_owned.Clear();_world?.Dispose();
            Actor=null;Target=null;_scope=null;_view=null;_world=null;_floor=null;
            GD.Print($"RESIDENT_HEAVY_TEARDOWN native_gone={gone} owned_allocated={allocated} owned_disposed={disposed} borrowed_disposed=0");
            TestAssert.True(gone&&allocated==disposed,"owned heavy graph is gone before own shapes/world disposal");
        }
    }
    private partial class RecoveryScope:Node2D,ILevelRecovery{public int Calls {get;private set;}public void RequestRespawn(string reason)=>Calls++;}
}
