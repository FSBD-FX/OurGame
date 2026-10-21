using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Residents;
using Knight.Enemies.Skitter;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.World;
using Knight.Mechanisms;
using Knight.Player;

namespace Knight.Tests.Integration;

// Actual saved Skitter, real physics/health/contacts. This develops one adapter,
// not seven final Chargers, family identities or fifty certified spawn sites.
public static partial class ResidentChargerRuntimeTests
{
    // Differential lifecycle control: same fixture/scene/cleanup, no opt-in
    // adapter. This is not a configured-behavior or final population receipt.
    public static async Task RunDefaultFixture(SceneTree tree)
    {
        for(int i=0;i<20;i++)
        {
            var f=new Fixture(tree,"unconfigured-lifecycle-"+i);
            try
            {
                f.Initialize(true);if(i%2==1)f.Player(410);await f.Mount(false);
                if(i%2==0)
                {
                    float start=f.Actor.Position.X;await f.Frames(45);
                    TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Idle&&MathF.Abs(f.Actor.Position.X-start)<.01f,
                        "same fixture preserves unconfigured no-target idle");
                }
                else
                {
                    await f.Until(()=>f.PlayerHealth.CurrentHealth<9,120);
                    TestAssert.True(f.PlayerHealth.CurrentHealth<9,"same fixture preserves real unconfigured contact damage");
                }
            }
            finally{await f.DisposeAsync();}
        }
        GD.Print("RESIDENT_CHARGER_FIXTURE_RESULT cases=20 configured=false saved_actor=true native_gone=true owned_disposed=true formal_spawned=0");
    }

    public static async Task Run(SceneTree tree)
    {
        int cases=0;var failures=new List<string>();
        await Check("default_without_target_unchanged",async f=>
        {
            await f.Mount(false);float x=f.Actor.Position.X;await f.Frames(45);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Idle&&MathF.Abs(f.Actor.Position.X-x)<.01f,
                "unconfigured no-player Skitter stays idle");
        });
        await Check("real_two_way_patrol",async f=>
        {
            await f.Mount();var t=await f.Trace(330);
            TestAssert.True(t.min<260&&t.max>378&&t.maxStep<2&&t.supported,$"real continuous native patrol {t}");
        });
        await Check("same_body_wall_blocks_whole_body",async f=>
        {
            f.Wall(368);await f.Mount();var t=await f.Trace(210);
            TestAssert.True(t.max<350.1f&&t.min<270&&t.supported,$"same floor RID wall blocks author motion {t}");
        });
        await Check("middle_gap_not_just_two_rays",async f=>
        {
            f.Floor(200,119.5f);f.Floor(320.5f,119.5f);await f.Mount(home:300,left:284,right:356);
            var t=await f.Trace(180);
            TestAssert.True(t.max<=305.6f&&t.min<290&&t.supported,$"complete foot strip rejects middle gap {t}");
        },false);
        await Check("outside_target_does_not_replace_patrol",async f=>
        {
            f.Player(470);await f.Mount();int attacks=0;
            f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.Phase==AttackPhase.Active)attacks++;};
            var t=await f.Trace(100);
            TestAssert.True(attacks==0&&t.max<404&&t.max-t.min>30,"real outside player cannot steal resident patrol");
        });
        await Check("locked_warning_cancelled_after_departure",async f=>
        {
            f.Player(410);await f.Mount();await f.Until(()=>f.Actor.CurrentFrame.AttackTimeline.HasLockedTarget,75);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Telegraph&&f.Actor.CurrentFrame.AttackTimeline.HasLockedTarget,
                "real warning locks before active movement");
            int active=0;f.Actor.AttackFrameChanged+=x=>{if(x.Timeline.Phase==AttackPhase.Active)active++;};
            f.Target.MoveAndCollide(new(130,0));await f.Frames(80);
            TestAssert.True(active==0&&f.PlayerHealth.CurrentHealth==9,"lost territory permission cancels already locked attack");
        });
        await Check("patrol_then_retreat_uses_combat_origin",async f=>
        {
            await f.Mount();await f.Until(()=>f.Actor.GlobalPosition.X<274,100);
            TestAssert.True(f.Actor.GlobalPosition.X<274,"actor actually patrolled away from initial spawn");
            f.Player(f.Actor.GlobalPosition.X+100);
            await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,100);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,"native rush begins after full warning");
            float max=f.Actor.GlobalPosition.X,min=float.PositiveInfinity;bool retreat=false;
            for(int i=0;i<55;i++)
            {
                await f.Frames(1);max=MathF.Max(max,f.Actor.GlobalPosition.X);
                if(f.Actor.CurrentFrame.State==CanyonSkitterState.Retreat){retreat=true;min=MathF.Min(min,f.Actor.GlobalPosition.X);}
                if(retreat&&f.Actor.CurrentFrame.State==CanyonSkitterState.Recover)break;
            }
            TestAssert.True(retreat&&max-min>=30,$"retreat cannot be capped by obsolete spawn x320: max={max} min={min}");
        });
        await Check("bounded_claw_force_and_real_return",async f=>
        {
            await f.Mount(home:400,left:360,right:440,territory:new(336,80,128,96));
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Zero,0,1,900)),"real damage before pull");
            f.Actor.ApplyThreadPull(new(1500,0));
            TestAssert.True(f.Actor.Velocity.Length()<=600.01f,"existing 600 maximum pull is preserved");
            var t=await f.Trace(210);
            TestAssert.True(t.max>450&&t.returned&&t.maxStep<11&&t.supported&&f.Health.CurrentHealth==1,
                $"force crosses territory, real grounded return crosses home without heal {t}");
        });
        await Check("sleep_withdraws_direct_contact_then_fresh_warning",async f=>
        {
            f.Player(410);await f.Mount();await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,100);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,"sleep test has actual active rush");
            ulong old=f.Actor.CurrentFrame.AttackTimeline.AttackId;int hp=f.PlayerHealth.CurrentHealth;
            f.Agent.SetResidentDormant(true);
            TestAssert.True(!f.Actor.GetNode<Hitbox>("RushHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),"immediate stale outgoing contact rejected");
            TestAssert.True(!f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,600,2,900)),"direct sleeping hurtbox rejected");
            await f.Frames(1);Vector2 pose=f.Actor.GlobalPosition;f.Actor.ApplyThreadPull(new(1500,0));await f.Frames(60);
            TestAssert.True(f.Actor.GlobalPosition.DistanceTo(pose)<.1f&&f.PlayerHealth.CurrentHealth==hp&&f.Health.CurrentHealth==2,
                "sleep preserves health and withdraws pull/attack authority");
            f.Agent.SetResidentDormant(false);bool warning=false;
            for(int i=0;i<130;i++){await f.Frames(1);warning|=f.Actor.CurrentFrame.State==CanyonSkitterState.Telegraph;if(f.Actor.CurrentFrame.State==CanyonSkitterState.Rush)break;}
            TestAssert.True(warning&&f.Actor.CurrentFrame.State==CanyonSkitterState.Rush&&f.Actor.CurrentFrame.AttackTimeline.AttackId>old,
                "wake starts a fresh warning/attack identity, not old active resume");
        });
        await Check("ordinary_lower_floor_is_not_home_height_death",async f=>
        {
            f.Floor(250,100);f.Floor(32,640,520);await f.Mount(home:330,left:280,right:332);
            f.Actor.ApplyThreadPull(new(600,0));await f.Frames(140);
            TestAssert.True(f.Health.CurrentHealth==2&&f.Actor.GlobalPosition.Y>500&&f.Actor.IsOnFloor(),
                "real ordinary 392px fall survives rather than home+240 artificial death");
        },false);
        await Check("actual_void_health_death_once",async f=>
        {
            f.Floor(250,100);f.Void(new(350,300,500,16));await f.Mount(home:330,left:280,right:332);
            int deaths=0;f.Actor.Defeated+=()=>deaths++;f.Actor.ApplyThreadPull(new(600,0));await f.Frames(140);
            TestAssert.True(f.Health.CurrentHealth==0&&deaths==1&&f.Recoveries==0&&f.Actor.CurrentFrame.State==CanyonSkitterState.Dead,
                "actual void is one Health death, never player recovery");
        },false);
        await Check("pending_void_withdraws_motion_and_survives_source_removal",async f=>
        {
            f.Player(410);await f.Mount();await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,100);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,"pending test begins during actual native rush");
            f.Health.InvulnerabilitySeconds=2;TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Zero,0,3,900)),"actual invulnerability starts before void contact");
            int deaths=0;f.Actor.Defeated+=()=>deaths++;
            KillZone source=f.Void(new(192,96,256,48));await f.Frames(1);Vector2 contact=f.Actor.GlobalPosition;
            TestAssert.True(f.Health.CurrentHealth==1&&deaths==0,"real overlapping source cannot bypass Health invulnerability");
            TestAssert.True(!f.Actor.GetNode("ThreadTarget").IsInGroup(Knight.Abilities.ThreadTarget2D.TargetGroup)&&
                f.Actor.GetNode<Area2D>("ThreadTarget/TargetCollision").CollisionLayer==0&&f.Actor.CurrentFrame.AttackTimeline.Phase==AttackPhase.None,
                "pending fatality withdraws target discovery and attack presentation immediately");
            await f.Frames(8);
            bool motionWithdrawn=f.Actor.GlobalPosition.DistanceTo(contact)<.1f;
            f.Agent.SetResidentDormant(true);f.Agent.SetResidentDormant(false);
            TestAssert.True(!f.Actor.GetNode("ThreadTarget").IsInGroup(Knight.Abilities.ThreadTarget2D.TargetGroup),"wake cannot restore a pending fatal target");
            f.Agent.SetResidentDormant(true);source.QueueFree();await f.Frames(125);
            TestAssert.True(motionWithdrawn,"pending fatal contact must stop autonomous retreat without freezing external physics");
            TestAssert.True(f.Health.CurrentHealth==0&&deaths==1,"pending source identity survives sleep and source destruction until one accepted Health death");
        });
        await Check("default_real_contact_attack_unchanged",async f=>
        {
            f.Player(410);await f.Mount(false);await f.Until(()=>f.PlayerHealth.CurrentHealth<9,120);
            TestAssert.True(f.PlayerHealth.CurrentHealth<9,"unconfigured old Skitter still warns/rushes and damages actual native hurtbox");
        });
        await Check("narrow_center_floor_is_not_whole_foot_support",async f=>
        {
            f.Floor(308,24);await f.Mount();var t=await f.Trace(60);
            TestAssert.True(t.supported&&t.max-t.min<.1f,"24px support cannot certify 28px full-body autonomous movement");
        },false);
        await Check("postmount_configuration_rejected_without_reset",async f=>
        {
            await f.Mount();bool rejected=false;try{f.Agent.ConfigureResidence(null);}catch(InvalidOperationException){rejected=true;}
            TestAssert.True(rejected,"a live resident cannot be reconfigured/reset through this API");
            var t=await f.Trace(90);TestAssert.True(t.max-t.min>30,"rejected configuration leaves real patrol working");
        });
        await Check("ordinary_damage_death_withdraws_native_authority",async f=>
        {
            f.Player(410);await f.Mount();await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,100);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            var area=target.GetNode<Area2D>("TargetCollision");var hurt=f.Actor.GetNode<Hurtbox>("Hurtbox");
            TestAssert.True(target.IsAvailable&&target.IsInGroup(GrappleSurface2D.TargetGroup)&&area.CollisionLayer==32,
                "actual living saved target has both discovery registrations and native area before death");
            int died=0,defeated=0;f.Health.Died+=()=>died++;f.Actor.Defeated+=()=>defeated++;
            TestAssert.True(hurt.Receive(new(2,Vector2.Zero,0,4,900)),"ordinary real Hurtbox lethal damage, not void or forced callback");
            bool withdrawn=!target.IsInGroup(ThreadTarget2D.TargetGroup)&&!target.IsInGroup(GrappleSurface2D.TargetGroup)&&
                area.CollisionLayer==0&&hurt.CollisionLayer==0&&!hurt.Monitoring;
            var locked=new MechanicalClawTargetResolver().SnapshotLocked(f.Target,target,target.WorldAnchor,Vector2.Left);
            TestAssert.True(!locked.Exists&&!target.TryGetEligibleReceiver(f.Target,out _),"existing dead/locked receiver rejection is preserved");
            TestAssert.True(!f.Actor.GetNode<Hitbox>("RushHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox"))&&
                !hurt.Receive(new(1,Vector2.Right,600,5,900)),"dead direct damage authority withdrawn before native deferred End");
            Vector2 dead=f.Actor.GlobalPosition;await f.Frames(20);
            TestAssert.True(f.Health.CurrentHealth==0&&died==1&&defeated==1&&f.Actor.IsInsideTree()&&
                f.Actor.GlobalPosition.DistanceTo(dead)<.1f,"one absorbing death while corpse remains mounted");
            TestAssert.True(withdrawn,"ordinary death must remove both target registrations/native target layer/hurtbox authority without sleep cleanup");
            TestAssert.True(!hurt.Monitorable,"monitorability is withdrawn at the legal deferred boundary, not inside native in/out emission");
        });
        await Check("dead_target_area_does_not_occlude_far_anchor",async f=>
        {
            await f.Mount();float y=f.Actor.GlobalPosition.Y-32;
            Node2D caster=f.Caster(new(220,y));GrappleSurface2D anchor=f.Anchor(new(430,y));await f.Frames(2);
            Area2D targetArea=f.Actor.GetNode<Area2D>("ThreadTarget/TargetCollision");
            using(var ray=PhysicsRayQueryParameters2D.Create(caster.GlobalPosition,anchor.WorldAnchor,
                CombatCollisionLayers.World|CombatCollisionLayers.Enemy|CombatCollisionLayers.ThreadTarget))
            {
                ray.CollideWithAreas=true;ray.CollideWithBodies=true;
                using Godot.Collections.Dictionary hit=f.Space.IntersectRay(ray);
                TestAssert.True(hit.Count>0&&ReferenceEquals(hit["collider"].AsGodotObject(),targetArea),
                    "literal horizontal ray hits live target circle above physical body, not corpse solid terrain");
            }
            var resolver=new MechanicalClawTargetResolver();
            TestAssert.True(!ReferenceEquals(resolver.ResolveKeyboard(caster,Vector2.Right).Surface,anchor),"living native enemy area actually blocks farther anchor before death");
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(2,Vector2.Zero,0,6,900)),"ordinary real death without removing actor or target node");
            await f.Frames(2);
            var after=resolver.ResolveKeyboard(caster,Vector2.Right);
            TestAssert.True(f.Health.CurrentHealth==0&&f.Actor.IsInsideTree()&&ReferenceEquals(after.Surface,anchor)&&after.Snapshot.HasLineOfSight,
                "actual keyboard resolver must select farther valid anchor after dead native target area withdrawal");
        });
        await Check("configured_checkpoint_reset_restores_one_life",async f=>
        {
            await f.Mount();int died=0,defeated=0;f.Health.Died+=()=>died++;f.Actor.Defeated+=()=>defeated++;
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(2,Vector2.Zero,0,7,900)),"first actual configured life dies");
            f.Actor.ResetForCheckpoint();await f.Frames(8);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(f.Health.CurrentHealth==2&&target.IsAvailable&&target.IsInGroup(GrappleSurface2D.TargetGroup)&&
                target.GetNode<Area2D>("TargetCollision").CollisionLayer==32,"explicit existing checkpoint reset restores authored life and target permission");
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(2,Vector2.Zero,0,8,900)),"reset life can receive actual damage again");
            await f.Frames(3);TestAssert.True(died==2&&defeated==2&&f.Health.CurrentHealth==0,"exactly one defeat per explicit checkpoint life, never wake healing");
        });
        await Check("native_area_damage_death_withdraws_target",async f=>
        {
            await f.Mount();int connections=0,defeated=0;f.Actor.Defeated+=()=>defeated++;
            Hitbox attack=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(0,-12));attack.Connected+=_=>connections++;attack.Begin(99);
            await f.Until(()=>f.Health.CurrentHealth==0,12);
            TestAssert.True(f.Health.CurrentHealth==0&&connections==1&&defeated==1,
                "actual AreaEntered→Hitbox→Hurtbox→Health→Died callback, no direct TryConnect/Receive injection");
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(!target.IsInGroup(GrappleSurface2D.TargetGroup)&&target.GetNode<Area2D>("TargetCollision").CollisionLayer==0,
                "native incoming lethal contact withdraws target physics without requiring actor removal");
            await f.Frames(1);TestAssert.True(!f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "native callback completes legal monitorability withdrawal after its deferred boundary");
        });
        await Check("native_death_reset_ignores_stale_deferred_withdrawal",async f=>
        {
            await f.Mount();int connections=0,defeated=0;f.Actor.Defeated+=()=>defeated++;
            Hitbox attack=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(0,-12));
            attack.Connected+=_=>{connections++;if(connections==1)f.Actor.ResetForCheckpoint();};attack.Begin(100);
            await f.Until(()=>connections==1,12);await f.Frames(2);
            TestAssert.True(connections==1&&defeated==1&&f.Health.CurrentHealth==2&&f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "explicit checkpoint reset inside actual lethal contact supersedes queued dead-monitorability update");
            attack.End();await f.Frames(2);attack.Begin(101);await f.Until(()=>connections==2,12);
            TestAssert.True(connections==2&&defeated==2&&f.Health.CurrentHealth==0,
                "the revived hurtbox remains natively hittable, exactly one new death in its second life");
            await f.Frames(1);TestAssert.True(!f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "the second death also commits unavailable, not a stale true from the intervening reset");
        });
        GD.Print($"RESIDENT_CHARGER_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));

        async Task Check(string name,Func<Fixture,Task> test,bool floor=true)
        {
            cases++;var f=new Fixture(tree,name);
            try{f.Initialize(floor);await test(f);GD.Print("RESIDENT_CHARGER_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_CHARGER_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
    }

    public static async Task RunBone(SceneTree tree)
    {
        int cases=0;var failures=new List<string>();
        await Check("guarded_J_real_K_window_and_expiry",async f=>
        {
            await MountBone(f,100);await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Idle,60);
            TestAssert.Equal(CanyonSkitterState.Idle,f.Actor.CurrentFrame.State,"initial real landing recovery ends before guarded contact");
            await Strike(f,1,false,1010);TestAssert.Equal(2,f.Health.CurrentHealth,"guarded native J never consumes Health");
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(target.IsAvailable&&target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new(EnemyPullSpeed:1500)),
                "guarded Bone still accepts original one-shot claw interaction from outside AI territory");
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Stunned&&f.Actor.Velocity.Length()<=600.01f,
                "real K synchronously enters original Stunned pose and preserves 600 pull bound");
            await Strike(f,1,true,1011);TestAssert.Equal(1,f.Health.CurrentHealth,"native J exploits K opening without modifying saved HP2");
            TestAssert.True(f.Actor.GlobalPosition.X<before.X-5,"same body actually receives external K movement");
            await f.Frames(65);await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Idle,60);
            TestAssert.Equal(CanyonSkitterState.Idle,f.Actor.CurrentFrame.State,"K and any real landing recovery have ended");
            await Strike(f,1,false,1012);TestAssert.Equal(1,f.Health.CurrentHealth,"expired opening closes without healing");
        });
        await Check("real_Retreat_is_not_Recover_despite_same_timeline_phase",async f=>
        {
            await MountBone(f,410);await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Rush,150);
            TestAssert.Equal(CanyonSkitterState.Rush,f.Actor.CurrentFrame.State,"real body completes warning and begins actual charge");
            await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Retreat,45);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Retreat&&
                f.Actor.CurrentFrame.AttackTimeline.Phase==AttackPhase.Recovery,
                "actual moving Retreat has Recovery timeline; this is the wrong-branch negative control");
            await Strike(f,1,false,1020);TestAssert.Equal(2,f.Health.CurrentHealth,"moving Retreat remains guarded");
            await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Recover,40);
            TestAssert.Equal(CanyonSkitterState.Recover,f.Actor.CurrentFrame.State,"same actor physically finishes retreat into static recovery");
            await Strike(f,1,true,1021);TestAssert.Equal(1,f.Health.CurrentHealth,"actual Recover is the ordinary punish window");
            await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Telegraph,90);
            TestAssert.Equal(CanyonSkitterState.Telegraph,f.Actor.CurrentFrame.State,"next real warning closes the natural opening");
            await Strike(f,1,false,1022);TestAssert.Equal(1,f.Health.CurrentHealth,"new windup is guarded, no reset/heal");
        });
        await Check("sleep_clears_K_source_and_generic_cancel_stun_stays_guarded",async f=>
        {
            await MountBone(f,100);var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            var receiver=f.Actor as IThreadInteractionReceiver;
            TestAssert.True(receiver!=null,"actual saved Skitter exposes a final boolean K receiver");
            var interaction=new ThreadInteraction(f.Target,f.Target.GlobalPosition,target.WorldAnchor,new());
            TestAssert.True(receiver.TryReceiveThread(interaction),"initial captured K is really accepted");
            await Strike(f,1,true,1030);TestAssert.Equal(1,f.Health.CurrentHealth,"native K opening positive before sleep");
            f.Agent.SetResidentDormant(true);await f.Frames(5);
            TestAssert.True(!target.IsAvailable&&!receiver.TryReceiveThread(interaction)&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,0,1031,900))&&f.Health.CurrentHealth==1,
                "sleep immediately rejects captured K/J without spending or healing");
            f.Agent.SetResidentDormant(false);await f.Frames(1);
            TestAssert.Equal(CanyonSkitterState.Stunned,f.Actor.CurrentFrame.State,"sleep's real CancelResidentCombat left a generic Stunned pose");
            await Strike(f,1,false,1032);TestAssert.Equal(1,f.Health.CurrentHealth,"generic cancel stun is not a K-source opening");
            TestAssert.True(receiver.TryReceiveThread(interaction),"new real K can open the same awake body");
            await Strike(f,1,true,1033);TestAssert.Equal(0,f.Health.CurrentHealth,"new K window permits a fresh native lethal hit");
        });
        await Check("native_death_reentrant_Reset_and_second_K_life",async f=>
        {
            await MountBone(f,100);await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Idle,60);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");var receiver=f.Actor as IThreadInteractionReceiver;
            TestAssert.True(receiver!=null,"captured final receiver exists before native death");
            var interaction=new ThreadInteraction(f.Target,f.Target.GlobalPosition,target.WorldAnchor,new());
            int defeated=0,connected=0;f.Actor.Defeated+=()=>defeated++;
            TestAssert.True(receiver.TryReceiveThread(interaction),"first real K opens native lethal contact");
            Hitbox first=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(0,-12));
            first.Connected+=_=>{connected++;if(connected==1)f.Actor.ResetForCheckpoint();};first.Begin(1040);
            await f.Until(()=>connected==1,12);first.End();await f.Frames(2);
            TestAssert.True(connected==1&&defeated==1&&f.Health.CurrentHealth==2&&target.IsAvailable&&
                f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "explicit Reset inside actual lethal Area signal supersedes old-life deferred withdrawal");
            await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Idle,60);
            await Strike(f,1,false,1041);TestAssert.Equal(2,f.Health.CurrentHealth,"new guarded life does not inherit old K opening");
            TestAssert.True(receiver.TryReceiveThread(interaction),"same captured receiver authorizes a genuinely new K on the reset life");
            await Strike(f,2,true,1042);await f.Frames(2);
            TestAssert.True(f.Health.CurrentHealth==0&&defeated==2&&!target.IsAvailable&&!receiver.TryReceiveThread(interaction)&&
                !target.IsInGroup(GrappleSurface2D.TargetGroup),"second native death withdraws discovery and direct captured transaction exactly once");
        });
        await Check("real_K_off_ledge_void_death_bypasses_pose_guard",async f=>
        {
            f.Floor(250,100);f.Void(new(350,300,500,16));await MountBone(f,430,330,280,332);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");int defeated=0;f.Actor.Defeated+=()=>defeated++;
            TestAssert.True(target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new()),"actual K pulls Bone from authored support toward cliff");
            await f.Frames(140);
            TestAssert.True(f.Health.CurrentHealth==0&&defeated==1&&f.Recoveries==0&&
                f.Actor.CurrentFrame.State==CanyonSkitterState.Dead&&!target.IsAvailable,
                "real unsupported fall crosses actual void once, never receives pose immunity or player recovery");
        },false);
        await Check("invalid_profiles_reject_atomically_before_residence_commit",async f=>
        {
            var valid=Bone();
            foreach(var invalid in new[]{valid with {Family=ResidentFamily.Native},valid with {Family=ResidentFamily.Ghost},
                valid with {CombatProfile=null},valid with {CombatProfile=new()},
                valid with {CombatProfile=new(ResidentBoneWeakness.RearOrExposed)},valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadStunned)},
                valid with {CombatProfile=new((ResidentBoneWeakness)99)},
                valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadExposed,.6f,.8f)},
                valid with {CombatProfile=new(ResidentBoneWeakness.RecoveryOrThreadExposed,float.NaN,0)}})
                Reject(()=>f.Agent.ConfigureResidence(invalid));
            f.Agent.ConfigureResidence(valid);f.Player(100);await f.Mount(false);await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Idle,60);
            Reject(()=>f.Agent.ConfigureResidence(valid));
            await Strike(f,1,false,1060);TestAssert.Equal(2,f.Health.CurrentHealth,"all rejected configurations leave one later valid guarded Bone policy");
            TestAssert.True(f.Actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable,"Bone pose guard never hides the K target");
        });
        await Check("captured_K_rejects_bad_settings_foreign_world_and_queued_chains",async f=>
        {
            await MountBone(f,100);var receiver=f.Actor as IThreadInteractionReceiver;
            TestAssert.True(receiver!=null,"boolean transaction is a real saved actor interface, not void fallback success");
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            foreach(ThreadAbilitySettings settings in new ThreadAbilitySettings[]{null,new(EnemyPullSpeed:0),new(EnemyPullSpeed:float.NaN),new(EnemyPullSpeed:float.PositiveInfinity)})
                TestAssert.True(!Try(new(f.Target,f.Target.GlobalPosition,target.WorldAnchor,settings)),"malformed final K rejects without native side effect or exception");
            using(var world=new World2D())
            {
                var viewport=new SubViewport{Size=new(16,16),World2D=world};f.Scope.AddChild(viewport);
                var foreign=new Node2D();viewport.AddChild(foreign);await f.Frames(1);
                TestAssert.True(foreign.GetTree()==f.Actor.GetTree()&&foreign.GetWorld2D()!=f.Actor.GetWorld2D(),"real same-tree foreign-world negative control");
                TestAssert.True(!Try(new(foreign,foreign.GlobalPosition,target.WorldAnchor,new()))&&
                    !target.TryApplyLegacyInteraction(foreign,foreign.GlobalPosition,new()),"direct captured receiver and saved target reject foreign world");
                viewport.QueueFree();await f.Frames(2);
            }
            Node2D caster=f.Caster(new(100,116));
            TestAssert.True(caster.GetNodeOrNull<HealthComponent>("Health")==null&&
                Try(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),
                "same-world healthless Node2D outside AI territory retains legitimate old external pull authority");
            var parent=new Node2D();var queuedSource=new Node2D();parent.AddChild(queuedSource);f.Scope.AddChild(parent);parent.QueueFree();
            TestAssert.True(!Try(new(queuedSource,queuedSource.GlobalPosition,target.WorldAnchor,new())),"queued ancestor is rejected even before child is freed");
            f.Actor.QueueFree();TestAssert.True(!Try(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),"queued actor rejects captured final receiver before any native reads");
            bool Try(ThreadInteraction interaction)
            {
                try{return receiver.TryReceiveThread(interaction);}
                catch(Exception error){throw new InvalidOperationException("malformed/captured final K must fail closed without throwing",error);}
            }
        });
        await Check("saved_Player_physical_K_then_J_hits_without_timing_override",async f=>
        {
            // Skitter's saved anchor is at its feet (36px below the saved
            // player centre), unlike Rust. Start inside the real 22.5deg cone.
            f.Agent.ConfigureResidence(Bone());f.SavedPlayer(220);await f.Mount(false);
            var combat=f.Target.GetNode<PlayerCombat>("Combat");
            var claw=f.Target.GetNode<MechanicalClawController>("MechanicalClaw");
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
            var nativeAttack=combat.GetNode<Hitbox>("AttackHitbox");
            int nativeEntries=0;CanyonSkitterState firstContactPose=default;
            nativeAttack.AreaEntered+=area=>
            {
                if(area!=f.Actor.GetNode<Hurtbox>("Hurtbox"))return;
                if(nativeEntries++==0)firstContactPose=f.Actor.CurrentFrame.State;
            };
            int attacks=0,connections=0;combat.AttackStarted+=(_,_)=>attacks++;
            combat.AttackConnected+=hurt=>{if(hurt==f.Actor.GetNode<Hurtbox>("Hurtbox"))connections++;};
            try
            {
                TestAssert.True(f.Target.IsOnFloor()&&f.Target.GetWorld2D()==f.Actor.GetWorld2D(),"saved player and Bone charge body share actual support/world");
                TestAssert.True(claw.KeyboardPreview.Surface==f.Actor.GetNode<ThreadTarget2D>("ThreadTarget"),
                    $"real keyboard preview must see the saved anchor before K; player={f.Target.GlobalPosition} enemy={f.Actor.GlobalPosition}");
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
                for(int i=0;i<45&&adapter.ApplicationCount==0;i++)await f.Frames(1);
                KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
                TestAssert.Equal(1,adapter.ApplicationCount,$"actual player claw delivers one K; state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure}");
                KeyboardActionRuntimeTests.KeyEvent(Key.J,true);
                for(int i=0;i<16&&connections==0;i++)await f.Frames(1);
                KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
                GD.Print($"RESIDENT_BONE_CHARGER_PLAYER_CONTACT K={adapter.ApplicationCount} J={attacks} native_entries={nativeEntries} first_pose={firstContactPose} connected={connections} hp={f.Health.CurrentHealth} final_pose={f.Actor.CurrentFrame.State}");
                TestAssert.True(attacks==1&&connections==1&&f.Health.CurrentHealth==1,
                    $"real J startup/native contact must fit K counterplay; attacks={attacks} connections={connections} hp={f.Health.CurrentHealth} pose={f.Actor.CurrentFrame.State}");
                GD.Print($"RESIDENT_BONE_CHARGER_KEYBOARD actual_saved_player=true K={adapter.ApplicationCount} J={attacks} connected={connections} hp={f.Health.CurrentHealth} OS_origin=false");
            }
            finally{KeyboardActionRuntimeTests.KeyEvent(Key.J,false);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);}
        });
        await Check("airborne_K_exposure_expires_before_actual_landing",async f=>
        {
            await MountBone(f,100);await f.Until(()=>f.Actor.CurrentFrame.State==CanyonSkitterState.Idle,60);
            Node2D caster=f.Caster(new(320,-1000));var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new(EnemyPullSpeed:1500)),"real upward K accepts healthless source and preserves 600 cap");
            await f.Frames(5);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Falling&&!f.Actor.IsOnFloor()&&f.Actor.GlobalPosition.Y<100,
                "original physical K actually lifts body and original model enters Falling");
            await Strike(f,1,true,1090);TestAssert.Equal(1,f.Health.CurrentHealth,"native airborne J exploits the bounded K-source opening");
            await f.Frames(42);
            TestAssert.True(f.Actor.CurrentFrame.State==CanyonSkitterState.Falling&&!f.Actor.IsOnFloor(),"body is still in real air after original .60 second K time");
            await Strike(f,1,false,1091);TestAssert.Equal(1,f.Health.CurrentHealth,"expired airborne opening closes before landing, not permanent Falling vulnerability");
            GD.Print("RESIDENT_BONE_CHARGER_AIR_WINDOW original_stun_seconds=0.6 physical_falling=true expired_before_landing=true");
        });
        GD.Print($"RESIDENT_BONE_CHARGER_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));

        ResidentPopulationDefinition Bone(float home=320,float left=256,float right=384)=>new(
            "bone.native-test.charger.01","bone.native-test.charger",CanyonRouteNodeId.LowCeilingBridge,
            PrologueSectorId.CanyonEntrance,ResidentFamily.Bone,ResidentArchetype.Charger,
            new(new(home,128),new(192,80,256,96),new[]{new Vector2(left,128),new Vector2(right,128)}),
            new(ResidentBoneWeakness.RecoveryOrThreadExposed));
        async Task MountBone(Fixture f,float playerX,float home=320,float left=256,float right=384)
        {f.Agent.ConfigureResidence(Bone(home,left,right));f.Player(playerX);await f.Mount(false,home,left,right);}
        async Task Check(string name,Func<Fixture,Task> test,bool floor=true)
        {
            cases++;var f=new Fixture(tree,"bone-"+name);
            try{f.Initialize(floor);await test(f);GD.Print("RESIDENT_BONE_CHARGER_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_BONE_CHARGER_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static void Reject(Action action)
        {
            bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            TestAssert.True(rejected,"invalid/repeated/postmount Bone configuration fails before partial commit");
        }
        static async Task Strike(Fixture f,int damage,bool accepted,ulong id)
        {
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(0,-12));hit.Damage=damage;int connected=0;
            hit.Connected+=_=>connected++;hit.Begin(id);await f.Frames(3);
            TestAssert.True(connected==1||hit.GetOverlappingAreas().Contains(f.Actor.GetNode<Hurtbox>("Hurtbox")),"rejected native J actually overlaps, not empty contact");
            hit.End();await f.Frames(2);
            GD.Print($"RESIDENT_BONE_CHARGER_STRIKE id={id} connected={connected} hp={f.Health.CurrentHealth} pose={f.Actor.CurrentFrame.State}");
            TestAssert.Equal(accepted?1:0,connected,"actual native Area J resolves Bone charge pose permission");
        }
    }

    private sealed class Fixture
    {
        private readonly SceneTree _tree;private readonly string _caseName;private World2D _world;private SubViewport _view;private RecoveryScope _scope;
        private StaticBody2D _floor;private readonly List<Shape2D> _owned=new();private bool _mounted;private float _home;
        public CanyonSkitter Actor {get;private set;}
        public CharacterBody2D Target {get;private set;}
        public HealthComponent Health=>Actor.GetNode<HealthComponent>("Health");
        public HealthComponent PlayerHealth=>Target.GetNode<HealthComponent>("Health");
        public IResidentEnemyAgent Agent=>Actor as IResidentEnemyAgent??throw new InvalidOperationException("Saved Skitter has no resident adapter");
        public int Recoveries=>_scope.Requests;
        public PhysicsDirectSpaceState2D Space=>_world.DirectSpaceState;
        public Node2D Scope=>_scope;
        public Fixture(SceneTree tree,string caseName){_tree=tree;_caseName=caseName;}
        public void Initialize(bool floor)
        {
            using Godot.Collections.Array players=(Godot.Collections.Array)_tree.GetNodesInGroup("player");TestAssert.Equal(0,players.Count,"fixture has no unrelated global player");
            _world=new World2D();_view=new SubViewport{Size=new(32,32),World2D=_world};_scope=new RecoveryScope();_view.AddChild(_scope);_tree.Root.AddChild(_view);
            Actor=GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn").Instantiate<CanyonSkitter>();
            if(floor)Floor(32,640);
        }
        public void Floor(float x,float width,float top=128)
        {
            var body=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            body.AddChild(new CollisionShape2D{Position=new(x+width/2,top+20),Shape=Own(new RectangleShape2D{Size=new(width,40)})});_scope.AddChild(body);_floor??=body;
        }
        public void Wall(float x)=>_floor.AddChild(new CollisionShape2D{Position=new(x,96),Shape=Own(new RectangleShape2D{Size=new(8,64)})});
        public KillZone Void(Rect2 bounds)
        {
            var area=new KillZone{CollisionLayer=0,CollisionMask=CombatCollisionLayers.Player,RoomControllerPath=new NodePath("..")};
            area.AddChild(new CollisionShape2D{Position=bounds.GetCenter(),Shape=Own(new RectangleShape2D{Size=bounds.Size})});_scope.AddChild(area);return area;
        }
        public void Player(float x)
        {
            Target=new CharacterBody2D{Position=new(x,116),CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};
            Target.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(20,24)})});Target.AddChild(new HealthComponent{Name="Health",MaxHealth=9});
            var hurt=new Hurtbox{Name="Hurtbox",CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};hurt.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(20,24)})});
            Target.AddChild(hurt);Target.AddToGroup("player");_scope.AddChild(Target);
        }
        public Node2D Caster(Vector2 position){var caster=new Node2D{Position=position};_scope.AddChild(caster);return caster;}
        public void SavedPlayer(float x)
        {
            Target=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Target.Position=new(x,92);Target.AddToGroup("player");_scope.AddChild(Target);
        }
        public Hitbox IncomingAttack(Vector2 position)
        {
            var attack=new Hitbox{Position=position,Damage=2,Knockback=0,CollisionLayer=CombatCollisionLayers.PlayerAttack,
                CollisionMask=CombatCollisionLayers.Enemy,Monitorable=false};
            attack.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(60,40)})});_scope.AddChild(attack);return attack;
        }
        public GrappleSurface2D Anchor(Vector2 position)
        {
            var anchor=new GrappleSurface2D{Position=position,AssistRadius=26};
            var area=new Area2D{Name="TargetCollision",CollisionLayer=CombatCollisionLayers.ThreadTarget,CollisionMask=0,Monitoring=false};
            area.AddChild(new CollisionShape2D{Shape=Own(new CircleShape2D{Radius=4})});anchor.AddChild(area);_scope.AddChild(anchor);return anchor;
        }
        public async Task Mount(bool configured=true,float home=320,float left=256,float right=384,Rect2? territory=null)
        {
            _home=home;Actor.Position=new(home,128);
            if(configured)Agent.ConfigureResidence(new("native-test.skitter.01","native-test.charger",CanyonRouteNodeId.LowCeilingBridge,
                PrologueSectorId.CanyonEntrance,ResidentFamily.Native,ResidentArchetype.Charger,
                new(new(home,128),territory??new Rect2(192,80,256,96),new[]{new Vector2(left,128),new Vector2(right,128)})));
            _scope.AddChild(Actor);_mounted=true;await Frames(6);
            TestAssert.True(Actor.IsOnFloor()&&Actor.GetWorld2D()==_world&&_world!=_tree.Root.FindWorld2D(),"actual saved body rests in owned native world");
        }
        public async Task Frames(int count){for(int i=0;i<count;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}}
        public async Task Until(Func<bool> predicate,int count){for(int i=0;i<count&&!predicate();i++)await Frames(1);}
        public async Task<(float min,float max,float maxStep,bool supported,bool returned)> Trace(int count)
        {
            float min=Actor.Position.X,max=min,last=min,step=0;bool supported=true,outside=false,returned=false;
            for(int i=0;i<count;i++){await Frames(1);float x=Actor.Position.X;min=MathF.Min(min,x);max=MathF.Max(max,x);step=MathF.Max(step,MathF.Abs(x-last));last=x;supported&=Actor.IsOnFloor();outside|=x>450;returned|=outside&&MathF.Abs(x-_home)<3;}
            GD.Print($"RESIDENT_CHARGER_TRACE min={min} max={max} max_step={step} supported={supported} returned={returned} final={Actor.Position}");return(min,max,step,supported,returned);
        }
        private T Own<T>(T shape)where T:Shape2D{_owned.Add(shape);return shape;}
        public async Task DisposeAsync()
        {
            ulong actorId=GodotObject.IsInstanceValid(Actor)?Actor.GetInstanceId():0;
            var physicsBefore=Engine.GetPhysicsFrames();var processBefore=Engine.GetProcessFrames();
            if(!_mounted&&GodotObject.IsInstanceValid(Actor))Actor.Free();
            if(GodotObject.IsInstanceValid(_view))_view.QueueFree();await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            bool nativeGone=!GodotObject.IsInstanceValid(Actor)&&!GodotObject.IsInstanceValid(Target)&&!GodotObject.IsInstanceValid(_floor)&&
                !GodotObject.IsInstanceValid(_scope)&&!GodotObject.IsInstanceValid(_view);
            int allocated=_owned.Count,disposed=0;
            foreach(Shape2D shape in _owned){shape.Dispose();disposed++;}_owned.Clear();_world?.Dispose();
            Actor=null;Target=null;_floor=null;_scope=null;_view=null;_world=null;
            GD.Print($"RESIDENT_CHARGER_TEARDOWN case={_caseName} actor_id={actorId} native_gone={nativeGone} owned_allocated={allocated} owned_disposed={disposed} world_disposed=true managed_fields_cleared=true physics_boundaries={Engine.GetPhysicsFrames()-physicsBefore} process_boundaries={Engine.GetProcessFrames()-processBefore}");
            TestAssert.True(nativeGone&&allocated==disposed,"fixture native graph is gone and every fixture-owned shape was disposed before any managed reference release");
        }
    }
    private partial class RecoveryScope:Node2D,ILevelRecovery
    {
        public int Requests {get;private set;}
        public void RequestRespawn(string reason)=>Requests++;
    }
}
