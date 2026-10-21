using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Enemies;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Prologue;
using Knight.World;
using Knight.Player;

namespace Knight.Tests.Integration;

public static partial class ResidentPatrolRuntimeTests
{
    public static async Task RunFixtureOwnership(SceneTree tree)
    {
        foreach(int mode in new[] { 0,1,2 })
        {
            var fixture=new Fixture(tree);
            try
            {
                fixture.Initialize(true);
                if(mode==2) fixture.Player(374);
                if(mode>0) await fixture.Mount(false);
            }
            finally { await fixture.DisposeAsync(); }
            GD.Print($"RESIDENT_FIXTURE_OWNERSHIP_CASE_OK mode={mode}");
        }
        GD.Print("RESIDENT_FIXTURE_OWNERSHIP_OK cases=3 motion_proved=false");
    }

    // Missing ConfigureResidence behavior, center-only support, hard clamps,
    // cleared external forces and sleep-only Monitoring changes must all fail.
    public static async Task<IReadOnlyList<string>> Run(SceneTree tree)
    {
        var failures = new List<string>(); int cases = 0;
        await Check("default_no_player_unchanged", async f =>
        {
            await f.Mount(false); float x = f.Actor.GlobalPosition.X;
            await f.Frames(60);
            TestAssert.Equal(RustShellState.Idle, f.Actor.CurrentState, "unconfigured no-player wave stays Idle");
            TestAssert.Near(x, f.Actor.GlobalPosition.X, .01f, "no default patrol regression");
        });
        await Check("actual_both_way_patrol", async f =>
        {
            await f.Mount(); var trace = await f.Trace(360);
            TestAssert.True(trace.max - trace.min >= 120 && trace.min < 262 && trace.max > 378,
                $"actual saved Rust travels both ways, not data-only: {trace}");
            TestAssert.True(trace.maxStep < 2f && trace.supported, "continuous native grounded movement, no pose writes");
        });
        await Check("actual_ledge_turn", async f =>
        {
            f.Floor(280,80); await f.Mount(); var trace = await f.Trace(180);
            TestAssert.True(trace.min >= 297.9f && trace.max <= 342.1f && trace.supported,
                $"full-body edge safety, no fall: {trace}");
            TestAssert.True(trace.max - trace.min > 20, "not merely frozen on ledge floor");
        }, false);
        await Check("same_floor_body_wall", async f =>
        {
            f.Wall(368); await f.Mount(); var trace = await f.Trace(330);
            TestAssert.True(trace.max <= 346.1f && trace.min < 262 && trace.supported,
                $"same native floor RID wall blocks authored movement: {trace}");
        });
        await Check("center_narrow_support_is_not_full_width", async f =>
        {
            f.Floor(304,32); await f.Mount();
            TestAssert.True(f.Actor.IsOnFloor(), "actual capsule physically rests on narrow bridge before residence policy check");
            var trace = await f.Trace(60);
            TestAssert.True(trace.max - trace.min < .1f && trace.supported, "32px center support cannot certify36px whole-foot movement");
        }, false);
        await Check("intermediate_one_pixel_gap", async f =>
        {
            f.Floor(200,119.5f); f.Floor(320.5f,119.5f); await f.Mount(homeX:300, left:284, right:356);
            var trace = await f.Trace(180);
            TestAssert.True(trace.max <= 301.5f && trace.min < 290 && trace.supported,
                $"native middle gap is rejected, not just two rays: {trace}");
        }, false);
        await Check("outside_player_not_chased", async f =>
        {
            f.Player(470); await f.Mount(); int attacks = 0;
            f.Actor.AttackFrameChanged += frame => { if (frame.Timeline.Phase == AttackPhase.Active) attacks++; };
            var trace = await f.Trace(120);
            TestAssert.True(attacks == 0 && trace.max < 404 && trace.max - trace.min > 30,
                "visible player inside old360 detection but outside territory cannot start pursuit/attack");
        });
        await Check("inside_player_real_attack", async f =>
        {
            f.Player(374); await f.Mount(); int hp = f.PlayerHealth.CurrentHealth;
            await f.Until(() => f.PlayerHealth.CurrentHealth < hp, 150);
            TestAssert.True(f.PlayerHealth.CurrentHealth < hp, "inside real sensed target receives native Area attack");
        });
        await Check("wall_occludes_inside_player", async f =>
        {
            f.Wall(350); f.Player(390); await f.Mount(); int hp = f.PlayerHealth.CurrentHealth;
            await f.Frames(90);
            TestAssert.True(f.PlayerHealth.CurrentHealth == hp && !f.Actor.HasLineOfSightTo(f.Target), "actual wall continues to occlude target");
            TestAssert.True(!f.Actor.CurrentFrame.AttackActive, "no attacking through wall");
        });
        await Check("locked_attack_withdraws_when_target_leaves", async f =>
        {
            f.Player(388); bool locked=false;int active=0;
            f.Actor.AttackFrameChanged+=frame=>
            {
                locked |= frame.Timeline.Phase==AttackPhase.Telegraph&&frame.Timeline.HasLockedTarget;
                if(frame.Timeline.Phase==AttackPhase.Active) active++;
            };
            await f.Mount(); await f.Until(()=>locked,90);
            TestAssert.True(locked&&active==0,"actual presentation locked during telegraph before target departure");
            f.Target.MoveAndCollide(new Vector2(120,0));
            TestAssert.True(f.Target.GlobalPosition.X>448,"real target movement leaves territory, not fake model distance");
            await f.Frames(90);
            TestAssert.True(active==0&&f.PlayerHealth.CurrentHealth==9&&!f.Actor.CurrentFrame.AttackActive,
                "already locked attack is canceled, not merely prevented from starting anew");
        });
        await Check("thread_force_crosses_boundary_and_returns", async f =>
        {
            await f.Mount(); f.Actor.ApplyThreadPull(new Vector2(1500,0));
            var trace = await f.Trace(300);
            TestAssert.True(trace.max > 435 && trace.returning && trace.returnedHome && f.Actor.GlobalPosition.X < 403,
                $"pull kept outside territory, real legal return: {trace} final={f.Actor.GlobalPosition}");
            TestAssert.True(trace.maxStep < 27 && trace.supported, "no teleport or erased external pull");
        });
        await Check("damage_impulse_and_hp_not_erased", async f =>
        {
            await f.Mount();
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1,Vector2.Right,1800,81,901)), "real hurtbox routes actual damage");
            var trace = await f.Trace(300);
            TestAssert.True(trace.max > 435 && trace.returning && trace.returnedHome && f.Actor.GlobalPosition.X < 403,
                $"real damage impulse not clamped: {trace}");
            TestAssert.Equal(2, f.Health.CurrentHealth, "return does not heal resident");
        });
        await Check("active_sleep_withdraws_authority_and_fresh_wake", async f =>
        {
            f.Player(388); await f.Mount();
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1,Vector2.Zero,0,82,901)), "damage before sleep");
            await f.Until(() => f.Actor.CurrentFrame.AttackActive,150);
            TestAssert.True(f.Actor.CurrentFrame.AttackActive, "actual active attack before sleep, not idle-only test");
            ulong oldId = f.Actor.CurrentFrame.AttackTimeline.AttackId;
            Vector2 pose = f.Actor.GlobalPosition; int targetHp = f.PlayerHealth.CurrentHealth;
            f.Actor.SetResidentDormant(true);
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").Monitoring, "sleep ends native attack monitoring");
            TestAssert.True(!f.Actor.GetNode<Hitbox>("AttackHitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")), "uncached stale direct outgoing contact rejected");
            TestAssert.True(!f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1,Vector2.Right,600,83,901)), "direct incoming contact rejected while sleeping");
            await f.Frames(1);
            f.Actor.ApplyThreadPull(new(1500,0)); await f.Frames(90);
            TestAssert.True(f.Actor.GlobalPosition.DistanceTo(pose) < .1f && f.PlayerHealth.CurrentHealth == targetHp, "sleep thread/attack authority withdrawn, floor pose stable");
            TestAssert.Equal(2,f.Health.CurrentHealth,"no sleep heal");
            f.Actor.SetResidentDormant(false); bool telegraph = false;
            for(int i=0;i<150;i++)
            {
                await f.Frames(1); telegraph |= f.Actor.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph;
                if(f.Actor.CurrentFrame.AttackActive) break;
            }
            TestAssert.True(telegraph && f.Actor.CurrentFrame.AttackActive && f.Actor.CurrentFrame.AttackTimeline.AttackId > oldId,
                "wake starts fresh full telegraph with new monotonic attack, not old active resume");
            TestAssert.Equal(2,f.Health.CurrentHealth,"wake retains exact health");
        });
        await Check("actual_death_withdraws_autonomous_motion", async f =>
        {
            await f.Mount();
            for(ulong hit=91;hit<=93;hit++)
            {
                TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1,Vector2.Zero,0,hit,901)),"actual lethal health path");
                if(hit<93) await f.Frames(25);
            }
            await f.Frames(2); Vector2 dead=f.Actor.GlobalPosition;
            await f.Frames(60);
            TestAssert.True(f.Health.CurrentHealth==0&&f.Actor.CurrentState==RustShellState.Dead&&
                f.Actor.ResidenceFrame.State==ResidentPatrolState.Dead&&f.Actor.GlobalPosition.DistanceTo(dead)<.01f&&
                !f.Actor.GetNode<Hitbox>("AttackHitbox").Monitoring,"dead resident cannot patrol, attack or heal");
        });
        await Check("invalid_and_postmount_configuration", async f =>
        {
            var valid = Definition(320,256,384);
            Reject(() => f.Actor.ConfigureResidence(valid with { Placement = null }));
            Reject(() => f.Actor.ConfigureResidence(valid with { Archetype = ResidentArchetype.Flyer }));
            await f.Mount();
            Reject(() => f.Actor.ConfigureResidence(valid));
            var trace = await f.Trace(60);
            TestAssert.True(trace.max - trace.min > 30, "rejected reconfiguration keeps prior working residence");
        });
        GD.Print($"RESIDENT_PATROL_NATIVE_RESULT cases={cases} failures={failures.Count} saved_actor=true actual_physics=true formal_spawned=0");
        return failures;

        async Task Check(string name, Func<Fixture,Task> test, bool floor = true)
        {
            cases++; var f = new Fixture(tree);
            try { f.Initialize(floor); await test(f); GD.Print("RESIDENT_PATROL_CASE_OK " + name); }
            catch(Exception ex) { failures.Add(name+": "+ex.Message); GD.Print("RESIDENT_PATROL_CASE_FAIL "+name+": "+ex.Message); }
            finally { await f.DisposeAsync(); }
        }
    }

    public static async Task RunBoneWanderer(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        // Literal new-policy boundary allows the pre-feature build to compile:
        // enum value 2 is RecoveryOrThreadStunned, not Heavy's RearOrExposed.
        ResidentPopulationDefinition Bone()=>Definition(320,256,384) with {
            Family=ResidentFamily.Bone,SubtypeId="bone.wanderer.native-test",
            CombatProfile=new((ResidentBoneWeakness)2)};
        await Check("guarded_J_then_real_K_stun_opens_and_expires",async f=>
        {
            await MountBone(f,100);await Strike(f,1,false,910);
            TestAssert.Equal(3,f.Health.CurrentHealth,"guarded native J does not reach Health");
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(target.IsAvailable&&target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings()),
                "Bone J protection does not revoke actual mechanical-claw permission");
            await f.Frames(1);
            TestAssert.True(f.Actor.CurrentState==RustShellState.Stunned&&f.Actor.GlobalPosition.X<before.X-5,
                "real K both moves the native body and reaches actual Stunned pose");
            await Strike(f,1,true,911);TestAssert.Equal(2,f.Health.CurrentHealth,"K-created native pose accepts real J");
            await f.Frames(26);
            TestAssert.True(f.Actor.CurrentState!=RustShellState.Stunned&&f.Actor.CurrentState!=RustShellState.Recover,
                "no target in territory: actual K opening has expired, not another recovery window");
            await Strike(f,1,false,912);TestAssert.Equal(2,f.Health.CurrentHealth,"expired opening rejects J after real invulnerability has expired");
        });
        await Check("natural_attack_recovery_opens_but_new_telegraph_closes",async f=>
        {
            await MountBone(f,374);
            TestAssert.Equal(RustShellState.Telegraph,f.Actor.CurrentState,"real sensed player starts ordinary windup");
            await Strike(f,1,false,920);
            await f.Until(()=>f.Actor.CurrentState==RustShellState.Recover,90);
            TestAssert.True(f.Actor.CurrentState==RustShellState.Recover&&f.PlayerHealth.CurrentHealth==8,
                "actual native enemy thrust hit once before its natural recovery, no model pose injection");
            await Strike(f,1,true,921);TestAssert.Equal(2,f.Health.CurrentHealth,"actual ordinary recovery permits native J without K");
            await f.Until(()=>f.Actor.CurrentState==RustShellState.Telegraph,60);
            TestAssert.Equal(RustShellState.Telegraph,f.Actor.CurrentState,"same actor naturally starts its next full windup");
            await Strike(f,1,false,922);TestAssert.Equal(2,f.Health.CurrentHealth,"next windup closes the opening without healing");
        });
        await Check("sleep_cancels_K_opening_and_generic_stun_is_not_a_weakness",async f=>
        {
            await MountBone(f,100);var receiver=(IThreadInteractionReceiver)f.Actor;
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            var interaction=new ThreadInteraction(f.Target,f.Target.GlobalPosition,target.GlobalPosition,new ThreadAbilitySettings());
            TestAssert.True(receiver.TryReceiveThread(interaction),"captured current receiver accepts initial real K");await f.Frames(1);
            await Strike(f,1,true,930);TestAssert.Equal(2,f.Health.CurrentHealth,"real opening is positive before sleep");
            f.Actor.SetResidentDormant(true);await f.Frames(90);
            TestAssert.True(!target.IsAvailable&&!receiver.TryReceiveThread(interaction)&&
                !f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Right,0,931,901))&&f.Health.CurrentHealth==2,
                "sleep withdraws captured K and J authority and never heals");
            f.Actor.SetResidentDormant(false);await f.Frames(1);
            TestAssert.Equal(RustShellState.Stunned,f.Actor.CurrentState,"sleep's existing generic cancellation stun is real");
            await Strike(f,1,false,932);TestAssert.Equal(2,f.Health.CurrentHealth,"waking generic Stunned does not resurrect the old K weakness");
            await f.Until(()=>f.Actor.CurrentState!=RustShellState.Stunned,30);
            TestAssert.True(receiver.TryReceiveThread(interaction),"same captured K accepts a genuinely new live pull after wake");await f.Frames(1);
            await Strike(f,1,true,933);TestAssert.Equal(1,f.Health.CurrentHealth,"new K alone creates the new opening, no reset or heal");
        });
        await Check("native_death_withdraws_targets_and_reset_restores_guarded_life",async f=>
        {
            await MountBone(f,100);var receiver=(IThreadInteractionReceiver)f.Actor;
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            var interaction=new ThreadInteraction(f.Target,f.Target.GlobalPosition,target.GlobalPosition,new ThreadAbilitySettings());
            uint bodyLayer=f.Actor.CollisionLayer,bodyMask=f.Actor.CollisionMask;
            TestAssert.True(receiver.TryReceiveThread(interaction),"real K opens before native lethal J");await f.Frames(1);
            await Strike(f,3,true,940);await f.Frames(2);
            TestAssert.True(f.Health.CurrentHealth==0&&!target.IsAvailable&&
                !target.IsInGroup(ThreadTarget2D.TargetGroup)&&!receiver.TryReceiveThread(interaction)&&
                f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer==0,
                "native death withdraws discovery and captured K, not only its damage window");
            f.Actor.ResetForCheckpoint();await f.Frames(2);
            TestAssert.True(f.Health.CurrentHealth==3&&target.IsAvailable&&target.IsInGroup(ThreadTarget2D.TargetGroup)&&
                f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer==CombatCollisionLayers.Enemy&&
                f.Actor.CollisionLayer==bodyLayer&&f.Actor.CollisionMask==bodyMask,
                "explicit checkpoint reset restores same body's live permissions without Ghost phasing");
            await Strike(f,1,false,941);TestAssert.Equal(3,f.Health.CurrentHealth,"new guarded life does not retain old K weakness");
        });
        await Check("actual_void_bypasses_pose_guard_and_withdraws_targets",async f=>
        {
            await MountBone(f,100);var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            f.Void(new Rect2(192,180,256,8));f.PrimaryFloor.QueueFree();
            await f.Until(()=>f.Health.CurrentHealth==0,120);await f.Frames(2);
            TestAssert.True(f.Health.CurrentHealth==0&&f.Actor.CurrentState==RustShellState.Dead&&
                !target.IsAvailable&&!target.IsInGroup(ThreadTarget2D.TargetGroup)&&f.RecoveryRequests==0,
                "real unsupported fall into zero-layer void kills Bone through Health and withdraws claw, without player recovery");
        });
        await Check("invalid_profiles_reject_atomically_and_Bone_never_runs_Ghost_clock",async f=>
        {
            var valid=Bone();
            foreach(var invalid in new[]{valid with {Family=ResidentFamily.Native},valid with {Family=ResidentFamily.Ghost},
                valid with {CombatProfile=null},valid with {CombatProfile=new()},
                valid with {CombatProfile=new(ResidentBoneWeakness.RearOrExposed)},
                valid with {CombatProfile=new((ResidentBoneWeakness)99)},
                valid with {CombatProfile=new((ResidentBoneWeakness)2,.6f,.8f)},
                valid with {CombatProfile=new((ResidentBoneWeakness)2,float.NaN,0)}})
                Reject(()=>f.Actor.ConfigureResidence(invalid));
            f.Actor.ConfigureResidence(valid);f.Player(100);await f.Mount(false);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");float alpha=f.Actor.GetNode<Node2D>("VisualRoot").Modulate.A;
            TestAssert.Near(1,alpha,.001f,"Bone does not initialize hidden Ghost opacity");
            for(int i=0;i<90;i++)
            {
                await f.Frames(1);
                TestAssert.True(target.IsAvailable&&f.Actor.GetNode<Node2D>("VisualRoot").Modulate.A==alpha&&f.Health.CurrentHealth==3,
                    "Bone never enters zero-duration Ghost clock, hidden target permission or heal");
            }
            Reject(()=>f.Actor.ConfigureResidence(valid));
            bool rejected;try{rejected=!((IThreadInteractionReceiver)f.Actor).TryReceiveThread(
                new(f.Target,f.Target.GlobalPosition,target.GlobalPosition,null));}
            catch(Exception error){throw new InvalidOperationException("Bone captured K missing settings fails closed, not throws",error);}
            TestAssert.True(rejected&&target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings()),
                "all rejected profiles/transactions preserve the later valid Bone claw configuration");
        });
        await Check("saved_player_physical_K_then_J_fits_the_real_opening",async f=>
        {
            f.Actor.ConfigureResidence(Bone());f.SavedPlayer(260);await f.Mount(false);
            var input=f.Target.GetNode<PlayerInputReader>("Input");
            var combat=f.Target.GetNode<PlayerCombat>("Combat");
            var claw=f.Target.GetNode<MechanicalClawController>("MechanicalClaw");
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
            int attacks=0,connected=0;
            combat.AttackStarted+=(_,_)=>attacks++;
            combat.AttackConnected+=hurt=>{if(hurt==f.Actor.GetNode<Hurtbox>("Hurtbox"))connected++;};
            try
            {
                TestAssert.True(f.Target.IsOnFloor()&&f.Target.GetWorld2D()==f.Actor.GetWorld2D(),
                    "actual saved player and Bone body share real grounded world");
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
                for(int i=0;i<40&&adapter.ApplicationCount==0;i++)await f.Frames(1);
                KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
                TestAssert.True(adapter.ApplicationCount==1&&f.Actor.CurrentState==RustShellState.Stunned,
                    $"physical K reaches actual player claw then saved enemy adapter/pose; state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} sampled={input.CurrentFrameSequence}");
                KeyboardActionRuntimeTests.KeyEvent(Key.J,true);
                for(int i=0;i<12&&connected==0;i++)await f.Frames(1);
                KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
                TestAssert.True(attacks==1&&connected==1&&f.Health.CurrentHealth==2,
                    $"real saved-player J startup/contact fits K opening without timing overrides; attacks={attacks} connected={connected} hp={f.Health.CurrentHealth} pose={f.Actor.CurrentState}");
                GD.Print($"RESIDENT_BONE_WANDERER_KEYBOARD actual_saved_player=true physical_K={adapter.ApplicationCount} physical_J={attacks} native_connected={connected} hp={f.Health.CurrentHealth} OS_origin=false");
            }
            finally
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.J,false);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            }
        });
        GD.Print($"RESIDENT_BONE_WANDERER_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));

        async Task MountBone(Fixture f,float playerX)
        {
            f.Actor.ConfigureResidence(Bone());f.Player(playerX);await f.Mount(false);
        }
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize(true);await test(f);GD.Print("RESIDENT_BONE_WANDERER_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_BONE_WANDERER_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static async Task Strike(Fixture f,int damage,bool accepted,ulong id)
        {
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(16,0),damage);int connected=0;
            hit.Connected+=_=>connected++;hit.Begin(id);await f.Frames(3);
            TestAssert.True(connected==1||hit.GetOverlappingAreas().Contains(f.Actor.GetNode<Hurtbox>("Hurtbox")),
                "native attack really overlaps Bone: a rejected contact is not a missed Area");
            hit.End();await f.Frames(2);
            GD.Print($"RESIDENT_BONE_WANDERER_STRIKE id={id} connected={connected} hp={f.Health.CurrentHealth} state={f.Actor.CurrentState}");
            TestAssert.Equal(accepted?1:0,connected,"actual native Area J resolves Bone pose permission");
        }
    }

    public static async Task RunGhost(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("hidden_native_J_K_rejected_then_manifest_accepts_without_body_change",async f=>
        {
            await MountGhost(f);ThreadTarget2D target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            uint layer=f.Actor.CollisionLayer,mask=f.Actor.CollisionMask;HealthComponent same=f.Health;
            TestAssert.True(!target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings())&&!target.IsAvailable,
                "actual hidden Ghost transaction is rejected, not a void pull that reports success");
            await Strike(f,1,false,810);TestAssert.Equal(3,f.Health.CurrentHealth,"native hidden J reaches the actor but cannot change actual Health");
            await f.Until(()=>target.IsAvailable,60);TestAssert.True(target.IsAvailable,"real physics phase becomes manifested within the finite hidden interval");
            await Strike(f,1,true,811);TestAssert.Equal(2,f.Health.CurrentHealth,"manifested native J damages the same real receiver");
            Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter").TryApply(f.Target,f.Target.GlobalPosition),
                "saved current K adapter accepts a manifested LightEnemy pull");await f.Frames(2);
            TestAssert.True(f.Actor.GlobalPosition.X<before.X-5&&ReferenceEquals(same,f.Health)&&f.Health.CurrentHealth==2&&
                f.Actor.CollisionLayer==layer&&f.Actor.CollisionMask==mask&&!f.Actor.GetNode<CollisionShape2D>("BodyCollider").Disabled&&f.Actor.IsOnFloor(),
                "native pull is real; manifestation neither replaces/heals Health nor removes World support/collision");
        });
        await Check("captured_K_rechecks_hidden_sleep_and_native_death",async f=>
        {
            await MountGhost(f);var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
            await f.Until(()=>target.IsAvailable,60);TestAssert.True(target.IsAvailable,"capture starts in actual manifested phase");
            await f.Until(()=>!target.IsAvailable,90);TestAssert.True(!target.IsAvailable,"finite manifestation expires for this same native target");
            var receiver=f.Actor as IThreadInteractionReceiver;
            var interaction=new ThreadInteraction(f.Target,f.Target.GlobalPosition,target.GlobalPosition,new ThreadAbilitySettings());
            TestAssert.True(receiver!=null&&!receiver.TryReceiveThread(interaction)&&!adapter.TryApply(f.Target,f.Target.GlobalPosition)&&
                !target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings())&&adapter.ApplicationCount==0,
                "already captured receiver and adapter both reject hidden phase at final bool transaction boundary");
            await f.Until(()=>target.IsAvailable,60);TestAssert.True(target.IsAvailable,"same target naturally manifests again without reset or healing");
            f.Actor.SetResidentDormant(true);await f.Frames(12);
            TestAssert.True(!receiver.TryReceiveThread(interaction)&&!target.IsAvailable&&f.Health.CurrentHealth==3,"sleep overrides manifested phase and preserves HP");
            f.Actor.SetResidentDormant(false);TestAssert.True(target.IsAvailable&&receiver.TryReceiveThread(interaction),"wake restores existing manifested phase and real accepted pull");
            await f.Frames(2);await Strike(f,3,true,820);TestAssert.Equal(0,f.Health.CurrentHealth,"real native Area attack kills the manifested actor");
            await f.Frames(100);
            TestAssert.True(!receiver.TryReceiveThread(interaction)&&!target.IsAvailable&&f.Health.CurrentHealth==0,
                "later phase time cannot resurrect or reauthorize the captured corpse");
            f.Actor.ResetForCheckpoint();TestAssert.True(f.Health.CurrentHealth==3&&!target.IsAvailable,"explicit reset alone starts a new hidden life");
            await f.Until(()=>target.IsAvailable,60);TestAssert.True(target.IsAvailable&&f.Health.CurrentHealth==3,"reset preserves the configured finite phase without wake healing");
        });
        await Check("profile_rejection_is_atomic_and_missing_K_settings_fail_closed",async f=>
        {
            var valid=Definition(320,256,384) with {Family=ResidentFamily.Ghost,SubtypeId="ghost.native-test",
                CombatProfile=new(GhostHiddenSeconds:.6f,GhostManifestSeconds:.8f)};
            foreach(var invalid in new[]{valid with {Family=ResidentFamily.Native},valid with {CombatProfile=null},
                valid with {CombatProfile=new()},valid with {CombatProfile=new(ResidentBoneWeakness.RearOrExposed,.6f,.8f)},
                valid with {CombatProfile=new(GhostHiddenSeconds:float.NaN,GhostManifestSeconds:.8f)},
                valid with {CombatProfile=new(GhostHiddenSeconds:.001f,GhostManifestSeconds:.8f)}})
                Reject(()=>f.Actor.ConfigureResidence(invalid));
            f.Actor.ConfigureResidence(valid);f.Player(100);await f.Mount(false);
            var target=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");await f.Until(()=>target.IsAvailable,60);
            var receiver=f.Actor as IThreadInteractionReceiver;TestAssert.True(receiver!=null&&target.IsAvailable,"same actor accepts valid configuration after all rejections");
            bool rejected;try{rejected=!receiver.TryReceiveThread(new(f.Target,f.Target.GlobalPosition,target.GlobalPosition,null));}
            catch(Exception error){throw new InvalidOperationException("captured K receiver must reject absent settings without throwing",error);}
            TestAssert.True(rejected,"malformed captured transaction must fail closed");
            Vector2 before=f.Actor.GlobalPosition;TestAssert.True(target.TryApplyLegacyInteraction(f.Target,f.Target.GlobalPosition,new ThreadAbilitySettings()),
                "rejected transaction does not consume later real K permission");await f.Frames(2);
            TestAssert.True(f.Actor.GlobalPosition.X<before.X-5&&f.Health.CurrentHealth==3,"valid K still performs native pull without heal/reset");
        });
        GD.Print($"RESIDENT_GHOST_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree);
            try{f.Initialize(true);await test(f);GD.Print("RESIDENT_GHOST_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_GHOST_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static async Task MountGhost(Fixture f)
        {
            f.Actor.ConfigureResidence(Definition(320,256,384) with {Family=ResidentFamily.Ghost,SubtypeId="ghost.native-test",
                CombatProfile=new(GhostHiddenSeconds:.6f,GhostManifestSeconds:.8f)});
            f.Player(100);await f.Mount(false);
        }
        static async Task Strike(Fixture f,int damage,bool accepted,ulong id)
        {
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition+new Vector2(16,0),damage);int connected=0;
            hit.Connected+=_=>connected++;hit.Begin(id);await f.Frames(3);hit.End();await f.Frames(2);
            GD.Print($"RESIDENT_GHOST_STRIKE id={id} connected={connected} hp={f.Health.CurrentHealth}");
            TestAssert.Equal(accepted?1:0,connected,"actual native Area J resolves the phase permission rather than sprite alpha");
        }
    }

    internal static ResidentPopulationDefinition Definition(float home, float left, float right) => new(
        "native-test.resident.rust.01", "native-test.rust", CanyonRouteNodeId.LowCeilingBridge,
        PrologueSectorId.CanyonEntrance, ResidentFamily.Native, ResidentArchetype.Wanderer,
        new ResidentPlacementCandidate(new(home,96),new(192,32,256,128),new[] { new Vector2(left,96),new Vector2(right,96) }));
    private static void Reject(Action action)
    {
        bool rejected=false; try { action(); } catch(ArgumentException) { rejected=true; } catch(InvalidOperationException) { rejected=true; }
        TestAssert.True(rejected,"invalid/repeated/postmount residence must fail before partial state");
    }

    internal sealed class Fixture
    {
        private readonly SceneTree _tree;
        private World2D _world;
        private SubViewport _viewport;
        private RecoveryScope _scope;
        private StaticBody2D _floor;
        private readonly List<Shape2D> _owned = new();
        private readonly List<Node> _detached = new();
        public RustShellSquire Actor {get;private set;}
        public CharacterBody2D Target {get;private set;}
        public HealthComponent Health => Actor.GetNode<HealthComponent>("Health");
        public HealthComponent PlayerHealth => Target.GetNode<HealthComponent>("Health");
        public int RecoveryRequests => _scope.RecoveryRequests;
        public StaticBody2D PrimaryFloor => _floor;
        public Node2D Scope => _scope;
        public void DetachActor(){_detached.Add(Actor);_scope.RemoveChild(Actor);}
        public Fixture(SceneTree tree) { _tree=tree; }
        public void Initialize(bool floor)
        {
            using Godot.Collections.Array players=(Godot.Collections.Array)_tree.GetNodesInGroup("player");
            TestAssert.Equal(0,players.Count,"no unrelated global target in native resident fixture");
            _world=new World2D();
            _viewport=new SubViewport{Name="RealResidentMotionWorld",Size=new(32,32),World2D=_world};
            _scope=new RecoveryScope(); _viewport.AddChild(_scope); _tree.Root.AddChild(_viewport);
            Actor=GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
            _detached.Add(Actor);
            // Rust changes collider poses/layers, not Shape2D resources. Borrow
            // saved shapes unchanged; only fixture-created shapes are ours.
            GD.Print("RESIDENT_FIXTURE_BORROWED_SHAPES "+string.Join("/",Walk(Actor).OfType<CollisionShape2D>().Select(x=>$"{x.Name}:{x.Shape.GetInstanceId()}:{x.Shape.GetRid()}")));
            if(floor) Floor(32,640);
        }
        public void Floor(float x,float width,float top=128)
        {
            var body=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            body.AddChild(new CollisionShape2D{Position=new(x+width/2,top+20),Shape=Own(new RectangleShape2D{Size=new(width,40)})});
            _scope.AddChild(body); _floor ??= body;
        }
        public void Wall(float x) => _floor.AddChild(new CollisionShape2D{Position=new(x,96),Shape=Own(new RectangleShape2D{Size=new(8,64)})});
        public KillZone Void(Rect2 bounds,bool disabled=false)
        {
            var zone=new KillZone{Name="ActualVoidBoundary",CollisionLayer=0,CollisionMask=CombatCollisionLayers.Player,
                RoomControllerPath=new NodePath("..")};
            zone.AddChild(new CollisionShape2D{Name="Collision",Position=bounds.GetCenter(),
                Shape=Own(new RectangleShape2D{Size=bounds.Size}),Disabled=disabled});
            _scope.AddChild(zone);return zone;
        }
        public void Player(float x)
        {
            Target=new CharacterBody2D{Name="RealSensedTarget",Position=new(x,96),CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};
            Target.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(20,40)})});
            Target.AddChild(new HealthComponent{Name="Health",MaxHealth=9});
            var hurtbox=new Hurtbox{Name="Hurtbox",CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};
            hurtbox.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(20,40)})});
            Target.AddChild(hurtbox); Target.AddToGroup("player"); _scope.AddChild(Target);
        }
        public void SavedPlayer(float x)
        {
            Target=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Target.Position=new(x,92);Target.AddToGroup("player");_scope.AddChild(Target);
        }
        public Hitbox IncomingAttack(Vector2 position,int damage)
        {
            var hit=new Hitbox{Position=position,Damage=damage,Knockback=0,CollisionLayer=CombatCollisionLayers.PlayerAttack,
                CollisionMask=CombatCollisionLayers.Enemy,Monitorable=false};
            hit.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(64,80)})});_scope.AddChild(hit);return hit;
        }
        public async Task Mount(bool configured=true,float homeX=320,float left=256,float right=384)
        {
            Actor.Position=new(homeX,96);
            if(configured) Actor.ConfigureResidence(Definition(homeX,left,right));
            _scope.AddChild(Actor); _detached.Remove(Actor); await Frames(6);
            TestAssert.True(Actor.IsOnFloor() && Actor.GetWorld2D()==_world && _world!=_tree.Root.FindWorld2D(),"native saved actor grounded in independent world");
        }
        public async Task Frames(int frames)
        {
            for(int i=0;i<frames;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}
        }
        public async Task Until(Func<bool> predicate,int frames)
        {
            for(int i=0;i<frames&&!predicate();i++) await Frames(1);
        }
        public async Task<(float min,float max,float maxStep,bool supported,bool returning,bool returnedHome)> Trace(int frames)
        {
            float min=Actor.GlobalPosition.X,max=min,previous=min,maxStep=0; bool supported=true,returning=false,returnedHome=false;
            for(int i=0;i<frames;i++)
            {
                await Frames(1); float x=Actor.GlobalPosition.X; min=Mathf.Min(min,x);max=Mathf.Max(max,x);
                maxStep=Mathf.Max(maxStep,Mathf.Abs(x-previous));previous=x;
                supported &= Actor.IsOnFloor();returning |= Actor.ResidenceFrame.State==ResidentPatrolState.Returning;
                returnedHome |= returning&&max>435&&Mathf.Abs(x-320)<=3;
            }
            GD.Print($"RESIDENT_PATROL_TRACE min={min} max={max} max_step={maxStep} supported={supported} returning={returning} returned_home={returnedHome} final={Actor.GlobalPosition}");
            return(min,max,maxStep,supported,returning,returnedHome);
        }
        private T Own<T>(T resource) where T:Shape2D { _owned.Add(resource);return resource; }
        public async Task DisposeAsync()
        {
            foreach(Node node in _detached) if(GodotObject.IsInstanceValid(node)) node.Free();
            if(GodotObject.IsInstanceValid(_viewport)) _viewport.QueueFree();
            await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            foreach(Shape2D shape in _owned)
            {
                ulong identity=shape.GetInstanceId();Rid rid=shape.GetRid();shape.Dispose();
                TestAssert.True(!GodotObject.IsInstanceValid(shape),"owned fixture shape disposed exactly once");
                GD.Print($"RESIDENT_FIXTURE_OWNED_SHAPE_DISPOSED id={identity} rid={rid}");
            }
            TestAssert.True(!GodotObject.IsInstanceValid(Actor),"owned mounted/unmounted actor actually freed");
            _world?.Dispose();
        }
        private static IEnumerable<Node> Walk(Node node)
        {
            yield return node;
            using Godot.Collections.Array children=(Godot.Collections.Array)node.GetChildren();
            for(int i=0;i<children.Count;i++) foreach(Node child in Walk(children[i].AsGodotObject() as Node)) yield return child;
        }
    }

    private partial class RecoveryScope : Node2D,ILevelRecovery
    {
        public int RecoveryRequests {get;private set;}
        public void RequestRespawn(string reason) => RecoveryRequests++;
    }
}
