using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Enemies.Residents;
using Knight.Enemies.Sentry;
using Knight.Presentation;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Prologue;
using Knight.World;
using Knight.Player;
using Knight.Mechanisms;

namespace Knight.Tests.Integration;

// Saved actors/native worlds. These are opt-in contracts, not nine final turrets
// or family scenes certified in the canyon. All mounted poses stay actor-owned.
public static partial class ResidentTurretRuntimeTests
{
    private static readonly string[] Scenes={"RivetScatterGunner","BeltBurstGunner","FurnaceMissileGunner","PrismLaserGunner","BellBoltSentry"};
    public static Task Run(SceneTree tree)=>RunCases(tree,false);
    public static Task RunSleep(SceneTree tree)=>RunCases(tree,true);
    // Observe the original fifteen cases without fetching borrowed Shape2D
    // wrappers, extra loads/frames, or changing resource ownership/cache policy.
    public static Task RunIdentity(SceneTree tree)=>RunCases(tree,false,true);
    public static Task RunPerception(SceneTree tree)=>RunTargetCases(tree,false);
    public static Task RunTargetDeath(SceneTree tree)=>RunTargetCases(tree,true);
    public static async Task RunConfiguration(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes)
        {
            cases++;var f=new Fixture(tree,scene);
            try
            {
                f.Initialize();
                Reject<ArgumentException>(()=>f.Agent.ConfigureResidence(Definition(new(600,300),f.Territory,new[]{new Vector2(600,300)})));
                Reject<ArgumentException>(()=>f.Agent.ConfigureResidence(Definition(new(320,300),f.Territory,new[]{new Vector2(352,300)})));
                if(f.Actor is RangedWeaponEnemy ranged)
                {
                    ranged.ConfigureEncounterEnvelope(new(800,224,100,144));
                    Reject<ArgumentException>(()=>f.Agent.ConfigureResidence(Definition(new(320,300),f.Territory,new[]{new Vector2(320,300)})));
                    ranged.ConfigureEncounterEnvelope(f.Territory);
                }
                f.Agent.ConfigureResidence(Definition(new(320,300),f.Territory,new[]{new Vector2(320,300)}));
                ResidentPopulationDefinition alternative=Definition(new(320,300),new(256,224,200,144),new[]{new Vector2(320,300)});
                Reject<InvalidOperationException>(()=>f.Agent.ConfigureResidence(alternative));
                await f.Mount(prepared:true);
                Reject<InvalidOperationException>(()=>f.Agent.ConfigureResidence(alternative));
                f.Player(new(560,293));ulong ticks=await AssertFreshAttack(f);
                TestAssert.True(f.Actor.IsOnFloor()&&f.Health.CurrentHealth==2,
                    "failed and duplicate configurations preserve the original physical home, Health and full territory, not a partial narrower replacement");
                f.Agent.SetResidentDormant(true);
                GD.Print($"RESIDENT_TURRET_CONFIGURATION_CASE_OK scene={scene} warning_ticks={ticks} real_receiver_hp={f.PlayerHealth.CurrentHealth} failed_config_atomic=true");
            }
            catch(Exception error){failures.Add(scene+": "+error.Message);GD.Print($"RESIDENT_TURRET_CONFIGURATION_CASE_FAIL scene={scene}: {error.Message}");}
            finally{await f.DisposeAsync();}
        }
        GD.Print($"RESIDENT_TURRET_CONFIGURATION_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        static ResidentPopulationDefinition Definition(Vector2 home,Rect2 area,Vector2[] points)=>new("scrap-turret.01","scrap-turret.fixture",
            CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,ResidentFamily.Scrap,ResidentArchetype.Turret,new(home,area,points));
        static void Reject<T>(Action action)where T:Exception
        {
            bool rejected=false;try{action();}catch(T){rejected=true;}
            TestAssert.True(rejected,"invalid or duplicate residence is rejected before changing the original contract");
        }
    }
    public static async Task RunLockedWall(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes)
        {
            cases++;var f=new Fixture(tree,scene);
            try
            {
                f.Initialize();f.Player(new(560,293));await f.Mount();await f.Until(()=>f.Timeline.HasLockedTarget,100);
                TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.HasLockedTarget,"a real living target first earns its committed warning");
                ulong old=f.Timeline.AttackId;int oldActive=0;
                f.OnPresentation=frame=>{if(frame.Timeline.AttackId==old&&frame.Timeline.Phase==AttackPhase.Active)oldActive++;};
                StaticBody2D wall=f.Wall();await f.Frames(2);
                TestAssert.True(f.LiteralSightHit(wall),"the newly registered actual World wall now occludes the committed target");
                for(int i=0;i<90;i++)
                {
                    await f.Frames(1);
                    TestAssert.True(f.Timeline.Phase==AttackPhase.None&&f.LiveRounds().Length==0&&f.AuthorizedLeaves()==0&&oldActive==0&&f.Shots==0&&f.PlayerHealth.CurrentHealth==9,
                        "an already locked attack is revoked when real World sight is lost, not allowed to finish its old model warning");
                }
                wall.QueueFree();await f.Frames(2);TestAssert.True(f.LiteralSightHit(null),"the exact dynamic obstruction is actually gone");
                ulong ticks=await AssertFreshAttack(f,old);
                TestAssert.True(oldActive==0&&f.Timeline.AttackId>old,"new legal sight cannot resume the cancelled attack identity");
                f.Agent.SetResidentDormant(true);
                GD.Print($"RESIDENT_TURRET_LOCKED_WALL_CASE_OK scene={scene} old_id={old} old_active={oldActive} fresh_warning_ticks={ticks} hp={f.PlayerHealth.CurrentHealth}");
            }
            catch(Exception error){failures.Add(scene+": "+error.Message);GD.Print($"RESIDENT_TURRET_LOCKED_WALL_CASE_FAIL scene={scene}: {error.Message}");}
            finally{await f.DisposeAsync();}
        }
        GD.Print($"RESIDENT_TURRET_LOCKED_WALL_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
    }
    public static async Task RunWallWarning(SceneTree tree)
    {
        var f=new Fixture(tree,"BellBoltSentry");bool passed=false;
        try
        {
            f.Initialize();f.Territory=new(256,96,368,400);f.Player(new(560,293));f.Wall(new(610,285));await f.Mount();
            await f.Until(()=>f.Timeline.HasLockedTarget,100);AttackShapeFrame warning=f.Shape;
            TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&warning.Kind==AttackShapeKind.Capsule&&MathF.Abs(warning.Extent.X-7)<.001f&&f.LiteralSightHit(null),
                "the actual radius7 warning is locked on a clear living target; wall left601 is behind, not between gun and player");
            float warningRight=warning.Origin.X+MathF.Abs(Vector2.Down.Rotated(warning.RotationRadians).X)*(warning.Extent.Y-warning.Extent.X)+warning.Extent.X;
            await f.Until(()=>f.LiveRounds().Length>0,140);BellBoltProjectile bolt=f.LiveRounds().OfType<BellBoltProjectile>().Single();
            bool retired=false;float maxRight=bolt.GlobalPosition.X+7;
            bolt.Retired+=_=>{retired=true;maxRight=MathF.Max(maxRight,bolt.GlobalPosition.X+7);};
            f.Target.MoveAndCollide(new(0,-120));TestAssert.True(f.Target.GlobalPosition.Y<174,"the living target really dodges the already launched native bolt");
            for(int i=0;i<120&&!retired;i++){await f.Frames(1);if(GodotObject.IsInstanceValid(bolt))maxRight=MathF.Max(maxRight,bolt.GlobalPosition.X+7);}
            GD.Print($"RESIDENT_SENTRY_WALL_WARNING warning_full_right={warningRight} actual_bolt_full_right={maxRight} wall_left=601 retired={retired} hp={f.PlayerHealth.CurrentHealth}");
            TestAssert.True(retired&&maxRight>=600&&maxRight<=601.1f&&f.PlayerHealth.CurrentHealth==9,
                "the actual full radius7 bolt stops against the literal wall, not the farther territory boundary, without hitting the dodged target");
            TestAssert.True(warningRight>=600&&warningRight<=601.1f,
                "the complete radius7 warning ends at the same real World wall instead of extending seven pixels into it");
            passed=true;
        }
        finally{await f.DisposeAsync();GD.Print($"RESIDENT_TURRET_WALL_WARNING_RESULT cases=1 failures={(passed?0:1)} saved_actor=true actual_physics=true formal_spawned=0");}
    }
    private static async Task<ulong> AssertFreshAttack(Fixture f,ulong previous=0)
    {
        await f.Until(()=>f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.AttackId>previous,20);
        ulong id=f.Timeline.AttackId,begin=Engine.GetPhysicsFrames();
        TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&id>previous&&f.Timeline.PhaseRemainingSeconds>1.0f,"a new legal target gets a full fresh warning");
        await f.Until(()=>f.Laser?f.Timeline.Phase==AttackPhase.Active:f.LiveRounds().Length>0,100);
        ulong ticks=Engine.GetPhysicsFrames()-begin;
        TestAssert.True(ticks>=64&&f.Timeline.AttackId==id&&(f.Laser?f.Timeline.Phase==AttackPhase.Active:f.LiveRounds().Length>0),"native beam or projectile is emitted only after the full shortest1.10-second warning");
        await f.Until(()=>f.PlayerHealth.CurrentHealth<9,90);TestAssert.Equal(8,f.PlayerHealth.CurrentHealth,"the same native attack really damages the legal receiver once");return ticks;
    }
    public static async Task RunVoid(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes)foreach(bool disabled in new[]{false,true})
        {
            cases++;var f=new Fixture(tree,scene);
            try
            {
                f.Initialize();KillZone zone=f.Void(disabled);int died=0,defeated=0;
                await f.Mount();f.Health.Died+=()=>died++;
                if(f.Actor is RangedWeaponEnemy ranged)ranged.Defeated+=()=>defeated++;else ((BellBoltSentry)f.Actor).Defeated+=()=>defeated++;
                f.RemoveFloor();bool touched=false;
                for(int i=0;i<80&&!touched;i++){Transform2D before=f.BodyPose();await f.Frames(1);touched=f.BodyTouchesDuring(before,zone);}
                TestAssert.True(touched,"the actual saved body naturally falls across the literal layer0 void shape, not a home-height threshold");
                await f.Frames(30);
                TestAssert.True(disabled?f.Health.CurrentHealth==2&&died==0&&defeated==0&&f.Actor.GlobalPosition.Y>420:
                    f.Health.CurrentHealth==0&&died==1&&defeated==1&&f.Timeline.Phase==AttackPhase.None,
                    "enabled real void kills once through Health; disabled geometry cannot kill an ordinary natural fall");
                TestAssert.Equal(0,f.RecoveryRequests,"NPC void contact never calls the player's recovery controller");
                if(!disabled)
                {
                    f.Agent.SetResidentDormant(true);f.Agent.SetResidentDormant(false);await f.Frames(4);
                    TestAssert.True(f.Health.CurrentHealth==0&&died==1&&defeated==1&&!f.Actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable,
                        "sleep/wake cannot revive or reauthorize the environment-defeated turret");
                }
                GD.Print($"RESIDENT_TURRET_VOID_CASE_OK scene={scene} disabled={disabled} actual_contact={touched} hp={f.Health.CurrentHealth} deaths={died} defeats={defeated}");
            }
            catch(Exception error){failures.Add(scene+"/disabled="+disabled+": "+error.Message);GD.Print($"RESIDENT_TURRET_VOID_CASE_FAIL scene={scene} disabled={disabled}: {error.Message}");}
            finally{await f.DisposeAsync();}
        }
        GD.Print($"RESIDENT_TURRET_VOID_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
    }
    public static async Task RunVoidPending(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes)
        {
            cases++;var f=new Fixture(tree,scene);
            try
            {
                f.Initialize();f.Health.InvulnerabilitySeconds=2;KillZone zone=f.Void();await f.Mount();int died=0,defeated=0,connections=0;
                f.Health.Died+=()=>died++;if(f.Actor is RangedWeaponEnemy ranged)ranged.Defeated+=()=>defeated++;else ((BellBoltSentry)f.Actor).Defeated+=()=>defeated++;
                Hitbox incoming=f.IncomingAttack(new(320,295),1);incoming.Connected+=_=>connections++;
                await f.Frames(2);incoming.Begin(712);await f.Until(()=>connections==1,12);incoming.End();
                TestAssert.True(connections==1&&f.Health.CurrentHealth==1,"actual native nonlethal AreaEntered establishes the real two-second protection window");
                f.RemoveFloor();bool touched=false;
                for(int i=0;i<80&&!touched;i++){Transform2D before=f.BodyPose();await f.Frames(1);touched=f.BodyTouchesDuring(before,zone);}
                var thread=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
                TestAssert.True(touched&&f.Health.CurrentHealth==1&&died==0&&!thread.IsAvailable&&
                    !thread.IsInGroup(ThreadTarget2D.TargetGroup)&&!thread.IsInGroup(GrappleSurface2D.TargetGroup)&&
                    thread.GetNode<Area2D>("TargetCollision").CollisionLayer==0&&f.Timeline.Phase==AttackPhase.None,
                    "real invulnerable void contact latches fatality and immediately withdraws interaction/attack authority without bypassing Health");
                float contactY=f.Actor.GlobalPosition.Y;f.Agent.SetResidentDormant(true);zone.QueueFree();f.Agent.SetResidentDormant(false);await f.Frames(4);
                TestAssert.True(f.Health.CurrentHealth==1&&!thread.IsAvailable&&!GodotObject.IsInstanceValid(zone)&&f.Actor.GlobalPosition.Y>contactY,
                    "sleep/wake and deleting the source cannot clear pending fatality or freeze accepted gravity");
                await f.Until(()=>f.Health.CurrentHealth==0,150);await f.Frames(2);
                TestAssert.True(f.Health.CurrentHealth==0&&died==1&&defeated==1&&f.RecoveryRequests==0,
                    "after real Health protection expires, latched NPC death resolves once despite source removal");
                f.RestoreFloor();await f.Frames(2);f.Reset();await f.Frames(6);
                TestAssert.True(f.Health.CurrentHealth==2&&f.Actor.IsOnFloor()&&thread.IsAvailable&&died==1&&defeated==1,
                    "explicit production checkpoint reset owns a clean second life, not the old pending fatality");
                KillZone fresh=f.Void();f.RemoveFloor();await f.Until(()=>f.Health.CurrentHealth==0,80);await f.Frames(2);
                TestAssert.True(f.Health.CurrentHealth==0&&died==2&&defeated==2&&GodotObject.IsInstanceValid(fresh)&&f.RecoveryRequests==0,
                    "the same actor's second real fall resolves its own environment death without replaying the first");
                GD.Print($"RESIDENT_TURRET_VOID_PENDING_CASE_OK scene={scene} actual_contact={touched} connections={connections} deaths={died} defeats={defeated} hp={f.Health.CurrentHealth}");
            }
            catch(Exception error){failures.Add(scene+": "+error.Message);GD.Print($"RESIDENT_TURRET_VOID_PENDING_CASE_FAIL scene={scene}: {error.Message}");}
            finally{await f.DisposeAsync();}
        }
        GD.Print($"RESIDENT_TURRET_VOID_PENDING_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
    }
    private static async Task RunTargetCases(SceneTree tree,bool death)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes)
        {
            if(!death)
            {
            await Check(scene,scene+"_foreign_world_is_not_a_target_then_local_real_attack",async f=>
            {
                CharacterBody2D foreign=f.ForeignPlayer(new(460,293));await f.Mount();
                TestAssert.True(foreign.IsInGroup("player")&&foreign.GetNode<HealthComponent>("Health").CurrentHealth==9&&
                    foreign.GetWorld2D()!=f.Actor.GetWorld2D()&&foreign.GlobalPosition==new Vector2(460,293),
                    "a real living grouped player occupies the literal legal coordinates, but in another native world");
                await Quiet(f,90);
                f.Player(new(560,293));ulong ticks=await FreshAttack(f);
                GD.Print($"RESIDENT_TURRET_PERCEPTION scene={scene} case=foreign local_warning_ticks={ticks} foreign_hp={foreign.GetNode<HealthComponent>("Health").CurrentHealth}");
                TestAssert.Equal(9,foreign.GetNode<HealthComponent>("Health").CurrentHealth,"local native attack cannot damage the foreign-world receiver");
                f.Agent.SetResidentDormant(true);
            });
            await Check(scene,scene+"_world_wall_blocks_then_removed_wall_allows_actual_attack",async f=>
            {
                f.Player(new(560,293));StaticBody2D wall=f.Wall();await f.Mount();
                TestAssert.True(f.LiteralSightHit(wall),"independent native ray from320292 to560293 hits the exact World-layer wall");
                await Quiet(f,90);wall.QueueFree();await f.Frames(2);
                TestAssert.True(!GodotObject.IsInstanceValid(wall)&&f.LiteralSightHit(null),"the exact wall is gone and the literal native ray is clear");
                ulong ticks=await FreshAttack(f);
                GD.Print($"RESIDENT_TURRET_PERCEPTION scene={scene} case=world_wall fresh_warning_ticks={ticks}");
                f.Agent.SetResidentDormant(true);
            });
            }
            if(death)await Check(scene,scene+"_locked_dead_target_cancels_old_identity_before_replacement",async f=>
            {
                f.Player(new(560,293));await f.Mount();await f.Until(()=>f.Timeline.HasLockedTarget,100);
                TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.HasLockedTarget,"real living player has earned the original locked native warning");
                ulong old=f.Timeline.AttackId;int oldActive=0;
                f.OnPresentation=frame=>{if(frame.Timeline.AttackId==old&&frame.Timeline.Phase==AttackPhase.Active)oldActive++;};
                CharacterBody2D replacement=f.ExtraPlayer(new(500,293));int deaths=0,connections=0;
                f.PlayerHealth.Died+=()=>{deaths++;replacement.AddToGroup("player");};
                Hitbox incoming=f.PlayerLethalAttack();incoming.Connected+=_=>connections++;
                await f.Frames(2);incoming.Begin(611);await f.Until(()=>deaths==1,12);incoming.End();await f.Frames(2);
                TestAssert.True(deaths==1&&connections==1&&f.PlayerHealth.CurrentHealth==0&&f.Target.IsInGroup("player")&&replacement.IsInGroup("player"),
                    "real EnemyAttack AreaEntered kills the old grouped target once and only then groups the pre-existing replacement");
                ulong ticks=await FreshAttack(f,old,replacement.GetNode<HealthComponent>("Health"));
                TestAssert.True(oldActive==0&&f.Timeline.AttackId>old&&f.PlayerHealth.CurrentHealth==0,
                    "a new living target cannot keep the dead target's committed attack identity active");
                f.Agent.SetResidentDormant(true);await f.Frames(90);
                TestAssert.True(oldActive==0&&f.Timeline.Phase==AttackPhase.None&&f.LiveRounds().Length==0,
                    "the cancelled old target identity never reappears during the entire additional90 ticks");
                GD.Print($"RESIDENT_TURRET_PERCEPTION scene={scene} case=locked_death old_id={old} warning_ticks={ticks} old_active={oldActive}");
            });
        }
        string label=death?"TARGET_DEATH":"PERCEPTION";
        GD.Print($"RESIDENT_TURRET_{label}_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string scene,string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree,scene);
            try{f.Initialize();await test(f);GD.Print($"RESIDENT_TURRET_{(death?"TARGET_DEATH":"PERCEPTION")}_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print($"RESIDENT_TURRET_{(death?"TARGET_DEATH":"PERCEPTION")}_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static async Task Quiet(Fixture f,int frames)
        {
            for(int i=0;i<frames;i++)
            {
                await f.Frames(1);
                TestAssert.True(f.Timeline.Phase==AttackPhase.None&&f.LiveRounds().Length==0&&f.Shots==0&&f.AuthorizedLeaves()==0,
                    "the actual illegal target never starts a warning, beam or native projectile");
            }
        }
        static async Task<ulong> FreshAttack(Fixture f,ulong previous=0,HealthComponent receiver=null)
        {
            await f.Until(()=>f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.AttackId>previous,20);
            ulong id=f.Timeline.AttackId,begin=Engine.GetPhysicsFrames();
            TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&id>previous&&f.Timeline.PhaseRemainingSeconds>1.0f,
                "a legal living same-world target starts a new complete warning rather than reusing cancelled time");
            await f.Until(()=>f.Laser?f.Timeline.Phase==AttackPhase.Active:f.LiveRounds().Length>0,100);
            ulong ticks=Engine.GetPhysicsFrames()-begin;
            TestAssert.True(ticks>=64&&f.Timeline.AttackId==id&&(f.Laser?f.Timeline.Phase==AttackPhase.Active:f.LiveRounds().Length>0),
                "the positive control reaches actual beam or projectile emission only after the full shortest1.10-second warning");
            receiver??=f.PlayerHealth;await f.Until(()=>receiver.CurrentHealth<9,90);
            TestAssert.Equal(8,receiver.CurrentHealth,"the same native positive control actually damages its legal receiver once");
            return ticks;
        }
    }
    // These callbacks run inside actual actor/projectile native physics calls,
    // before first beam damage or between real blast recipients. No clock Step.
    public static async Task RunReentry(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("PrismLaserGunner","laser_first_emission_sleep_prevents_same_stack_first_damage",async f=>
        {
            f.Player(new(560,293));var actor=(RangedWeaponEnemy)f.Actor;ulong cancelled=0;int damage=0,oldActive=0;
            f.PlayerHealth.Damaged+=(_,_)=>damage++;
            actor.ShotEmitted+=(id,_)=>{if(cancelled==0){cancelled=id;f.Agent.SetResidentDormant(true);}};
            f.OnPresentation=frame=>{if(cancelled!=0&&frame.Timeline.AttackId==cancelled&&frame.Timeline.Phase==AttackPhase.Active)oldActive++;};
            await f.Mount();await f.Until(()=>cancelled!=0,140);
            TestAssert.True(cancelled!=0&&f.Shots==1&&damage==0&&f.PlayerHealth.CurrentHealth==9&&f.AuthorizedLeaves()==0&&
                f.Timeline.Phase==AttackPhase.None&&oldActive==0,"actual first ShotEmitted sleep clears the beam before the same native call can deliver first damage");
            await f.Frames(90);
            TestAssert.True(damage==0&&f.Shots==1&&f.PlayerHealth.CurrentHealth==9&&oldActive==0,"cancelled first beam has no deferred damage or stale Active publication");
            f.Agent.SetResidentDormant(false);await f.Until(()=>f.Timeline.Phase==AttackPhase.Telegraph,12);
            ulong warning=Engine.GetPhysicsFrames(),fresh=f.Timeline.AttackId;
            TestAssert.True(fresh>cancelled&&f.Timeline.PhaseRemainingSeconds>1.0f,"wake starts a fresh complete laser warning");
            await f.Until(()=>damage!=0,100);ulong elapsed=Engine.GetPhysicsFrames()-warning;
            TestAssert.True(damage==1&&f.PlayerHealth.CurrentHealth==8&&f.Shots==2&&elapsed>=73&&oldActive==0,
                "the same real target is hit after the fresh full1.25-second warning, proving first cancellation was not a missing beam");
            f.Agent.SetResidentDormant(true);await f.Frames(20);
            TestAssert.True(damage==1&&f.PlayerHealth.CurrentHealth==8,"fresh beam cancellation cannot redeliver a consumed identity");
            GD.Print($"RESIDENT_LASER_REENTRY cancelled_id={cancelled} fresh_id={fresh} warning_ticks={elapsed} damage={damage} hp={f.PlayerHealth.CurrentHealth}");
        });
        foreach(int mode in new[]{0,1,2})await Check("FurnaceMissileGunner",mode switch
        {0=>"missile_native_two_receiver_positive",1=>"missile_explosion_callback_sleep_prevents_first_damage",_=>"missile_first_damage_callback_sleep_prevents_second_receiver"},async f=>
        {
            f.Player(new(560,293));CharacterBody2D second=f.ExtraPlayer(new(584,293));HealthComponent secondHealth=second.GetNode<HealthComponent>("Health");
            Hurtbox firstHurt=f.Target.GetNode<Hurtbox>("Hurtbox"),secondHurt=second.GetNode<Hurtbox>("Hurtbox");
            int firstDamage=0,secondDamage=0,explosions=0;bool bothCandidates=false;Vector2 blast=Vector2.Zero;RangedWeaponProjectile round=null;
            void Damaged(bool first)
            {
                if(first)firstDamage++;else secondDamage++;
                if(mode==2&&firstDamage+secondDamage==1)f.Agent.SetResidentDormant(true);
            }
            f.PlayerHealth.Damaged+=(_,_)=>Damaged(true);secondHealth.Damaged+=(_,_)=>Damaged(false);
            ((RangedWeaponEnemy)f.Actor).ShotEmitted+=(_,_)=>
            {
                if(round!=null)return;round=f.LiveRounds().OfType<RangedWeaponProjectile>().Single();
                round.ExplosionResolved+=(center,radius)=>
                {
                    explosions++;blast=center;
                    // A real native candidate snapshot, not an expectation
                    // derived from it: the literal required IDs are both bodies.
                    Hurtbox[] candidates=RangedAttackGeometry.ExplosionTargets(round.GetWorld2D().DirectSpaceState,center,radius);
                    bothCandidates=candidates.Any(x=>ReferenceEquals(x,firstHurt))&&candidates.Any(x=>ReferenceEquals(x,secondHurt));
                    if(mode==1)f.Agent.SetResidentDormant(true);
                };
            };
            await f.Mount();await f.Until(()=>explosions!=0,180);
            TestAssert.True(explosions==1&&bothCandidates&&f.Shots==1&&blast.X>=312&&blast.X<=568&&blast.Y>=280&&blast.Y<=312,
                "the actual first blast contains both exact receivers and its fullradius56 remains inside territory");
            int total=firstDamage+secondDamage;
            TestAssert.True(mode switch
            {
                0=>firstDamage==1&&secondDamage==1&&f.PlayerHealth.CurrentHealth==8&&secondHealth.CurrentHealth==8,
                1=>total==0&&f.PlayerHealth.CurrentHealth==9&&secondHealth.CurrentHealth==9,
                _=>total==1&&Math.Min(f.PlayerHealth.CurrentHealth,secondHealth.CurrentHealth)==8&&Math.Max(f.PlayerHealth.CurrentHealth,secondHealth.CurrentHealth)==9
            },"native blast positive delivers once per receiver; sleep before damage delivers zero, sleep after first damage delivers exactly one");
            if(mode==0)f.Agent.SetResidentDormant(true);
            TestAssert.True(f.LiveRounds().Length==0&&f.AuthorizedLeaves()==0&&f.LiveImpacts()==0&&f.Timeline.Phase==AttackPhase.None,
                "same-stack withdrawal retires real blast/projectile authority and queues existing cosmetic impact out");
            await f.Frames(90);
            TestAssert.True(firstDamage+secondDamage==total&&explosions==1&&f.Shots==1&&f.LiveImpacts()==0,
                "no retired explosion callback, second recipient or tail repeats during sleep");
            GD.Print($"RESIDENT_MISSILE_REENTRY mode={mode} actual_two_candidates={bothCandidates} blast={blast} first_damage={firstDamage} second_damage={secondDamage} hp={f.PlayerHealth.CurrentHealth},{secondHealth.CurrentHealth}");
        });
        GD.Print($"RESIDENT_TURRET_REENTRY_RESULT cases={cases} failures={failures.Count} saved_roles=2 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string scene,string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree,scene);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_TURRET_REENTRY_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_TURRET_REENTRY_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
    }
    public static async Task RunSentryDeath(SceneTree tree)
    {
        var f=new Fixture(tree,"BellBoltSentry");bool passed=false;
        try
        {
            f.Initialize();f.Player(new(560,293));await f.Mount();var actor=(BellBoltSentry)f.Actor;
            int resets=0,deaths=0,defeats=0,connections=0;ulong warning=0;
            f.Health.Died+=()=>deaths++;actor.Defeated+=()=>defeats++;
            f.OnPresentation=frame=>
            {
                if(resets==0&&frame.Timeline.Phase==AttackPhase.None&&f.Health.CurrentHealth==0){resets++;actor.ResetForCheckpoint();}
                if(resets==1&&warning==0&&frame.Timeline.Phase==AttackPhase.Telegraph)warning=Engine.GetPhysicsFrames();
            };
            await f.Until(()=>f.LiveRounds().Length>0,140);var old=f.LiveRounds().OfType<BellBoltProjectile>().Single();
            Shape2D oldBody=old.GetNode<CollisionShape2D>("BodyCollider").Shape,oldHit=old.GetNode<CollisionShape2D>("Hitbox/HitCollider").Shape;
            Shape2D savedBody=actor.GetNode<CollisionShape2D>("BodyCollider").Shape;
            ulong oldBoltId=old.GetInstanceId(),oldBodyId=oldBody.GetInstanceId(),oldHitId=oldHit.GetInstanceId();
            Hitbox incoming=f.IncomingAttack(new(320,295));incoming.Connected+=_=>connections++;await f.Frames(2);incoming.Begin(401);
            await f.Until(()=>connections==1,12);incoming.End();await f.Frames(2);
            TestAssert.True(resets==1&&connections==1&&deaths==1&&defeats==1&&f.Health.CurrentHealth==2,
                "actual AreaEntered death clears the old attack and its None subscriber explicitly resets the first life");
            TestAssert.True(!GodotObject.IsInstanceValid(old)&&!GodotObject.IsInstanceValid(oldBody)&&!GodotObject.IsInstanceValid(oldHit)&&GodotObject.IsInstanceValid(savedBody),
                "native old bolt exit releases only its own two shapes, not the borrowed saved actor shape");
            await f.Until(()=>f.LiveRounds().Length>0,140);var fresh=f.LiveRounds().OfType<BellBoltProjectile>().Single();
            Shape2D freshBody=fresh.GetNode<CollisionShape2D>("BodyCollider").Shape,freshHit=fresh.GetNode<CollisionShape2D>("Hitbox/HitCollider").Shape;
            ulong elapsed=Engine.GetPhysicsFrames()-warning;
            TestAssert.True(warning!=0&&elapsed>=64&&fresh.GetInstanceId()!=oldBoltId&&freshBody.GetInstanceId()!=oldBodyId&&freshHit.GetInstanceId()!=oldHitId&&f.Health.CurrentHealth==2,
                "reset owns a second life with complete fresh warning and distinct actual bolt/owned shapes");
            incoming.Begin(402);await f.Until(()=>connections==2,12);incoming.End();await f.Frames(2);
            GD.Print($"RESIDENT_SENTRY_SECOND_LIFE resets={resets} connections={connections} deaths={deaths} defeats={defeats} hp={f.Health.CurrentHealth} warning_ticks={elapsed} old_bolt_gone={!GodotObject.IsInstanceValid(old)} fresh_bolt_gone={!GodotObject.IsInstanceValid(fresh)} borrowed_disposed_by_test=0");
            TestAssert.True(connections==2&&deaths==2&&defeats==2&&f.Health.CurrentHealth==0&&actor.CurrentFrame.State==BellBoltSentryState.Dead,
                "old death publication cannot set the next life's defeated guard: two natural deaths each emit their own Defeated");
            TestAssert.True(!GodotObject.IsInstanceValid(fresh)&&!GodotObject.IsInstanceValid(freshBody)&&!GodotObject.IsInstanceValid(freshHit)&&GodotObject.IsInstanceValid(savedBody)&&
                f.Timeline.Phase==AttackPhase.None&&!actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                "second native death also terminates only that life's actual bolt/owned shapes and withdraws outgoing/incoming authority");
            f.Agent.SetResidentDormant(true);f.Agent.SetResidentDormant(false);await f.Frames(4);
            TestAssert.True(f.Health.CurrentHealth==0&&defeats==2&&f.LiveRounds().Length==0&&!actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable,
                "sleep/wake cannot resurrect or reauthorize the twice-defeated resident");passed=true;
        }
        finally{await f.DisposeAsync();}
        GD.Print($"RESIDENT_SENTRY_DEATH_RESULT cases=1 failures={(passed?0:1)} saved_actor=true actual_physics=true formal_spawned=0");
    }
    public static async Task RunRangedDeath(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes.Where(x=>x!="BellBoltSentry"))
        {
            cases++;var f=new Fixture(tree,scene);
            try
            {
                f.Initialize();f.Player(new(560,293));await f.Mount();var actor=(RangedWeaponEnemy)f.Actor;
                int resets=0,deaths=0,defeats=0,connections=0;ulong warning=0;
                f.Health.Died+=()=>deaths++;actor.Defeated+=()=>defeats++;
                f.OnPresentation=frame=>
                {
                    if(resets==0&&frame.Timeline.Phase==AttackPhase.None&&f.Health.CurrentHealth==0){resets++;actor.ResetForCheckpoint();}
                    if(resets==1&&warning==0&&frame.Timeline.Phase==AttackPhase.Telegraph)warning=Engine.GetPhysicsFrames();
                };
                await f.Until(()=>f.Shots==1,140);TestAssert.True(f.Shots==1,"first real native emission exists before death/reset");
                Node[] oldLeaves=f.LiveRounds();Hitbox incoming=f.IncomingAttack(new(320,295));incoming.Connected+=_=>connections++;
                await f.Frames(2);incoming.Begin(501);await f.Until(()=>connections==1,12);incoming.End();await f.Frames(2);
                TestAssert.True(resets==1&&connections==1&&deaths==1&&defeats==1&&f.Health.CurrentHealth==2&&oldLeaves.All(x=>!GodotObject.IsInstanceValid(x)),
                    "natural first death resets once and terminates every real old projectile sibling");
                GD.Print($"RESIDENT_RANGED_RESET scene={scene} hp={f.Health.CurrentHealth} state={actor.CurrentFrame.State} shots={f.Shots} deaths={deaths} defeats={defeats}");
                await f.Until(()=>f.Shots==2,140);ulong elapsed=warning==0?0:Engine.GetPhysicsFrames()-warning;
                TestAssert.True(warning!=0&&elapsed>=64&&f.Shots==2&&f.Health.CurrentHealth==2,
                    "old death callback cannot overwrite the reset model or withdraw the new life's complete warning and actual emission");
                incoming.Begin(502);await f.Until(()=>connections==2,12);incoming.End();await f.Frames(2);
                GD.Print($"RESIDENT_RANGED_SECOND_LIFE scene={scene} resets={resets} connections={connections} deaths={deaths} defeats={defeats} hp={f.Health.CurrentHealth} warning_ticks={elapsed}");
                TestAssert.True(connections==2&&deaths==2&&defeats==2&&f.Health.CurrentHealth==0&&actor.CurrentFrame.State==RangedWeaponState.Dead&&
                    f.LiveRounds().Length==0&&f.AuthorizedLeaves()==0&&f.Timeline.Phase==AttackPhase.None&&!actor.GetNode<Hurtbox>("Hurtbox").Monitorable,
                    "both real lives independently die and withdraw projectile/beam/incoming authority");
                f.Agent.SetResidentDormant(true);f.Agent.SetResidentDormant(false);await f.Frames(4);
                TestAssert.True(f.Health.CurrentHealth==0&&defeats==2&&!actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable,
                    "sleep/wake cannot revive a defeated ranged resident");
                GD.Print("RESIDENT_RANGED_DEATH_CASE_OK "+scene);
            }
            catch(Exception error){failures.Add(scene+": "+error.Message);GD.Print("RESIDENT_RANGED_DEATH_CASE_FAIL "+scene+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        GD.Print($"RESIDENT_RANGED_DEATH_RESULT cases={cases} failures={failures.Count} saved_roles=4 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
    }
    public static async Task RunBoundaries(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        foreach(string scene in Scenes)await Check(scene,scene+"_actual_attack_stays_in_full_territory",async f=>
        {
            f.Territory=new(256,96,368,400);CharacterBody2D outside=null;
            if(scene=="PrismLaserGunner")outside=f.ExtraPlayer(new(660,293));
            f.Player(new(560,293));await f.Mount();
            if(f.Laser)
            {
                await f.Until(()=>f.Timeline.Phase==AttackPhase.Active,140);AttackShapeFrame shape=f.Shape;
                float beamRight=CapsuleRight(shape);await f.Frames(30);
                TestAssert.True(shape.Kind==AttackShapeKind.Capsule&&MathF.Abs(shape.Extent.X-5)<.001f&&beamRight<=624.01f&&
                    outside.GetNode<HealthComponent>("Health").CurrentHealth==9&&f.PlayerHealth.CurrentHealth==8,
                    "real width5 laser hits the inside receiver once but its full capsule cannot reach outside receiver at660");return;
            }
            await f.Until(()=>f.LiveRounds().Length>0,140);Node round=f.LiveRounds().OrderBy(x=>MathF.Abs(((Node2D)x).GlobalPosition.Y-292)).FirstOrDefault();
            TestAssert.True(round!=null,"actual launched central projectile exists before native dodge");
            float inset=scene switch{"RivetScatterGunner"=>6,"BeltBurstGunner"=>5,"FurnaceMissileGunner"=>56,_=>7};
            bool retired=false;float max=((Node2D)round).GlobalPosition.X+inset;
            void Terminal(){retired=true;max=MathF.Max(max,((Node2D)round).GlobalPosition.X+inset);}
            if(round is RangedWeaponProjectile ranged)ranged.Retired+=_=>Terminal();else ((BellBoltProjectile)round).Retired+=_=>Terminal();
            f.Target.MoveAndCollide(new(0,-120));TestAssert.True(f.Target.GlobalPosition.Y<174&&f.Target.GlobalPosition.Y-36>=96,
                "real living target dodges above the old locked path while its whole body remains in territory");
            for(int i=0;i<120&&!retired&&max<=624.01f;i++){await f.Frames(1);if(GodotObject.IsInstanceValid(round))max=MathF.Max(max,((Node2D)round).GlobalPosition.X+inset);}
            GD.Print($"RESIDENT_TURRET_BOUND scene={scene} max_full_x={max} retired={retired} inset={inset}");
            TestAssert.True(retired&&max<=624.01f,"the actual missed projectile/full missile blast retires at the complete territory boundary, not its farther default MaximumTravel");
        });
        await Check("BellBoltSentry","sentry_warning_matches_real_radius_and_territory",async f=>
        {
            f.Player(new(560,293));await f.Mount();await f.Until(()=>f.Timeline.HasLockedTarget,100);AttackShapeFrame shape=f.Shape;float right=CapsuleRight(shape);
            GD.Print($"RESIDENT_SENTRY_WARNING radius={shape.Extent.X} full_right={right}");
            TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&shape.Kind==AttackShapeKind.Capsule&&MathF.Abs(shape.Extent.X-7)<.001f&&right<=624.01f,
                "opt-in warning covers the actual radius7 bolt and ends inside the same right624 territory");
        });
        await Check("BellBoltSentry","sentry_creator_owned_collision_shapes_end_with_native_bolt",async f=>
        {
            f.Player(new(560,293));await f.Mount();await f.Until(()=>f.LiveRounds().Length>0,140);var bolt=f.LiveRounds().OfType<BellBoltProjectile>().Single();
            // Read-only borrowed observations of shapes allocated by the actual
            // Sentry producer; this test must never Dispose either reference.
            Shape2D body=bolt.GetNode<CollisionShape2D>("BodyCollider").Shape,hit=bolt.GetNode<CollisionShape2D>("Hitbox/HitCollider").Shape;
            f.Agent.SetResidentDormant(true);await f.Frames(2);
            bool gone=!GodotObject.IsInstanceValid(bolt),bodyGone=!GodotObject.IsInstanceValid(body),hitGone=!GodotObject.IsInstanceValid(hit);
            GD.Print($"RESIDENT_SENTRY_OWNED_SHAPES bolt_gone={gone} body_disposed={bodyGone} hit_disposed={hitGone} borrowed_disposed_by_test=0");
            TestAssert.True(gone&&bodyGone&&hitGone,"the producer, not borrowed observers or GC, releases both of its dynamically allocated bolt shapes on native exit");
        });
        GD.Print($"RESIDENT_TURRET_BOUNDARY_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string scene,string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree,scene);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_TURRET_BOUNDARY_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_TURRET_BOUNDARY_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static float CapsuleRight(AttackShapeFrame shape)=>shape.Origin.X+MathF.Abs(Vector2.Down.Rotated(shape.RotationRadians).X)*(shape.Extent.Y-shape.Extent.X)+shape.Extent.X;
    }
    private static async Task RunCases(SceneTree tree,bool sleep,bool identifyShapes=false)
    {
        var failures=new List<string>();int cases=0,snapshots=0;
        foreach(string scene in Scenes)
        {
            if(sleep){await Check(scene,scene+"_real_tail_or_beam_sleep_and_fresh_wake",async f=>
            {
                f.Player(new(560,293));await f.Mount();
                TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Zero,0,701,904))&&f.Health.CurrentHealth==1,
                    "actual incoming hit reduces mounted turret HP before the tested sleep");
                await f.Until(()=>f.Laser?f.Timeline.Phase==AttackPhase.Active:f.Recovering&&f.LiveRounds().Length>0,140);
                Node[] captured=f.LiveRounds();
                TestAssert.True(f.Laser?f.Timeline.Phase==AttackPhase.Active:f.Recovering&&captured.Length>0,
                    "sleep starts with real laser Active or actual projectile still flying after owner recovery");
                ulong old=f.Timeline.AttackId;int oldActive=0;f.OnPresentation=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)oldActive++;};
                int playerHp=f.PlayerHealth.CurrentHealth,shots=f.Shots;Vector2 before=f.Actor.GlobalPosition;
                f.Agent.SetResidentDormant(true);
                TestAssert.True(f.Timeline.Phase==AttackPhase.None&&f.LiveRounds().Length==0&&f.AuthorizedLeaves()==0&&
                    f.Actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph").CurrentFrame.Timeline.Phase==AttackPhase.None,
                    "sleep synchronously withdraws owner warning, actual projectile leaves and laser authority");
                foreach(Node round in captured)
                {
                    TestAssert.True(f.Retired(round),"captured actual leaf is retired before deferred deletion");
                    if(round is BellBoltProjectile bolt)
                        TestAssert.True(!bolt.GetNode<Hitbox>("Hitbox").TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox")),
                            "held old Sentry hitbox loses direct logical damage permission immediately");
                }
                var thread=f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
                TestAssert.True(!f.Actor.GetNode<Hurtbox>("Hurtbox").Receive(new(1,Vector2.Zero,0,702,904))&&
                    !thread.IsAvailable&&!thread.IsInGroup(ThreadTarget2D.TargetGroup)&&!thread.IsInGroup(GrappleSurface2D.TargetGroup)&&
                    thread.GetNode<Area2D>("TargetCollision").CollisionLayer==0&&
                    !thread.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter").TryApply(f.Target,f.Target.GlobalPosition),
                    "sleep rejects direct incoming damage and saved legacy claw discovery/interaction without healing");
                ((IThreadPullReceiver)f.Actor).ApplyThreadPull(Vector2.Right*600);
                for(int i=0;i<90;i++){await f.Frames(1);TestAssert.True(f.Timeline.Phase==AttackPhase.None&&!f.Stunned&&f.LiveRounds().Length==0,
                    "sleep cannot retain attacks, create new Sentry/projectile leaves, or accept a new claw stun");}
                TestAssert.True(f.Health.CurrentHealth==1&&f.PlayerHealth.CurrentHealth==playerHp&&f.Shots==shots&&oldActive==0&&
                    f.Actor.GlobalPosition.DistanceTo(before)<.1f&&f.Actor.IsOnFloor(),"no tail emission, late impact, HP reset or fixed-body teleport during sleep");
                f.Agent.SetResidentDormant(false);await f.Until(()=>f.Timeline.Phase==AttackPhase.Telegraph,12);
                TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.AttackId>old&&f.Timeline.PhaseRemainingSeconds>1.0f&&
                    thread.IsAvailable&&f.Health.CurrentHealth==1&&oldActive==0,"wake preserves HP and starts a genuinely new full warning/identity");
                ulong warningFrame=Engine.GetPhysicsFrames(),fresh=f.Timeline.AttackId;
                await f.Until(()=>f.Timeline.Phase==AttackPhase.Active,90);ulong elapsed=Engine.GetPhysicsFrames()-warningFrame;
                TestAssert.True(f.Timeline.Phase==AttackPhase.Active&&f.Timeline.AttackId==fresh&&elapsed>=64&&oldActive==0&&
                    (f.Laser||f.LiveRounds().Length>0),"new wake attack reaches actual beam/flight only after the complete shortest1.10-second warning, never the old identity");
                GD.Print($"RESIDENT_TURRET_WAKE scene={scene} warning_ticks={elapsed} old_id={old} fresh_id={fresh} hp={f.Health.CurrentHealth}");
            });continue;}
            await Check(scene,scene+"_unconfigured_fixed_idle_keeps_native_gravity",async f=>
            {
                await f.Mount(false);Vector2 before=f.Actor.GlobalPosition;await f.Frames(60);
                TestAssert.True(f.Timeline.Phase==AttackPhase.None&&f.Actor.IsOnFloor()&&f.Actor.GlobalPosition.DistanceTo(before)<.1f,
                    "old saved actor without a target stays fixed and still resolves the native floor");
            });
            await Check(scene,scene+"_configured_fixed_home_keeps_native_gravity",async f=>
            {
                await f.Mount();Vector2 before=f.Actor.GlobalPosition;await f.Frames(90);
                TestAssert.True(f.Timeline.Phase==AttackPhase.None&&f.Actor.IsOnFloor()&&f.Actor.GlobalPosition.DistanceTo(before)<.1f&&f.Health.CurrentHealth==2,
                    "one-point turret residence is fixed, not a ground patrol or disabled physical body");
            });
            await Check(scene,scene+"_locked_complete_target_leaves_territory",async f=>
            {
                f.Player(new(560,293));await f.Mount();await f.Until(()=>f.Timeline.HasLockedTarget,100);
                TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.HasLockedTarget,"actual native full-body target earns a locked warning");
                ulong old=f.Timeline.AttackId;int active=0;f.OnPresentation=x=>{if(x.Timeline.AttackId==old&&x.Timeline.Phase==AttackPhase.Active)active++;};
                f.Target.MoveAndCollide(new(50,0));TestAssert.True(f.Target.GlobalPosition.X>609&&f.Target.GlobalPosition.X<624,
                    "actual target center remains inside x624 while complete radius20 body overhangs");await f.Frames(90);
                TestAssert.True(active==0&&f.Timeline.Phase==AttackPhase.None&&f.LiveRounds().Length==0&&f.PlayerHealth.CurrentHealth==9,
                    "opt-in complete-target departure revokes the locked attack rather than retaining the old default committed shot");
            });
        }
        GD.Print($"RESIDENT_TURRET{(sleep?"_SLEEP":"")}_RESULT cases={cases} failures={failures.Count} saved_roles=5 actual_physics=true formal_spawned=0");
        if(identifyShapes)
        {
            TestAssert.Equal(15,snapshots,"all original saved-actor mounts have a scalar native-shape snapshot");
            GD.Print("RESIDENT_TURRET_IDENTITY_RESULT snapshots=15 phase_cases=15 borrowed_shape_wrappers=false resource_policy_changed=false formal_spawned=0");
        }
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string scene,string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree,scene,identifyShapes);
            try{f.Initialize();await test(f);GD.Print("RESIDENT_TURRET_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_TURRET_CASE_FAIL "+name+": "+error.Message);}
            finally{if(f.ShapeIdentityLogged)snapshots++;await f.DisposeAsync();}
        }
    }
    public static async Task RunGhostSentry(SceneTree tree)
    {
        var failures=new List<string>();int cases=0;
        await Check("hidden_native_J_K_and_manifest_same_body",async f=>
        {
            await Mount(f,.6f,3);var target=Target(f);var body=f.Actor.GetNode<CollisionShape2D>("BodyCollider");
            Node2D caster=f.Caster(new(200,300));uint layer=f.Actor.CollisionLayer,mask=f.Actor.CollisionMask;f.Wall();await f.Frames(1);
            Hidden(f);TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable&&!body.Disabled&&f.Actor.TestMove(f.Actor.GlobalTransform,new(200,0)),"Hidden keeps real incoming contact and whole capsule World collision");
            await Strike(f,1,false,1300);TestAssert.True(!Receiver(f).TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new()))&&!target.TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"captured and transactional Hidden K reject");
            ((BellBoltSentry)f.Actor).ApplyThreadPull(new(600,0));await f.Frames(1);TestAssert.True(!f.Stunned,"void legacy fallback cannot stun Hidden");
            await Manifest(f);await Strike(f,1,true,1301);Vector2 before=f.Actor.GlobalPosition;
            TestAssert.True(Receiver(f).TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new()))&&f.Health.CurrentHealth==1,"Manifest accepts same-Health J and real healthless K source");
            await f.Frames(2);TestAssert.True(f.Stunned&&f.Actor.GlobalPosition.DistanceTo(before)<.1f&&f.Actor.CollisionLayer==layer&&f.Actor.CollisionMask==mask,"original Sentry K stuns, never pulls body or changes saved collision");
        });
        await Check("original_full_warning_real_bolt_contact",async f=>
        {
            f.Player(new(560,293));await Mount(f,.2f,4);await Manifest(f);ulong ticks=await AssertFreshAttack(f);
            TestAssert.True(ticks>=64&&f.PlayerHealth.CurrentHealth==8,"Ghost Manifest leaves original1.10 warning and one physical bolt contact usable");
        });
        await Check("hide_locked_warning_new_identity",async f=>
        {
            f.Player(new(560,293));await Mount(f,.6f,.8f);await f.Until(()=>f.Timeline.HasLockedTarget,120);
            TestAssert.True(f.Timeline.Phase==AttackPhase.Telegraph&&f.Timeline.HasLockedTarget,"actual original direction lock precedes hide");ulong old=f.Timeline.AttackId;
            await f.Until(()=>!Target(f).IsAvailable,60);Hidden(f);TestAssert.True(f.Timeline.Phase==AttackPhase.None&&f.LiveRounds().Length==0&&f.PlayerHealth.CurrentHealth==9,"hide cancels pre-fire warning without invisible damage");
            await f.Until(()=>f.Timeline.Phase==AttackPhase.Telegraph,100);TestAssert.True(f.Timeline.AttackId>old&&f.Timeline.PhaseRemainingSeconds>1.08f,"new Manifest creates fresh complete warning, not old locked tail");
        });
        await Check("hide_retires_real_inflight_bolt_synchronously",async f=>
        {
            f.Player(new(560,293));await Mount(f,.6f,1.25f);await f.Until(()=>f.LiveRounds().Length==1,160);
            BellBoltProjectile bolt=f.LiveRounds().OfType<BellBoltProjectile>().Single();Hitbox captured=bolt.GetNode<Hitbox>("Hitbox");bool retired=false,denied=false;
            bolt.Retired+=_=>{retired=true;denied=!captured.TryConnect(f.Target.GetNode<Hurtbox>("Hurtbox"));};
            await f.Until(()=>f.Recovering,20);
            TestAssert.True(f.Recovering&&GodotObject.IsInstanceValid(bolt)&&!bolt.IsRetired,"actual live bolt persists into Recover BEFORE hide cancellation changes the model to Idle");
            await f.Until(()=>!Target(f).IsAvailable,60);
            GD.Print($"RESIDENT_GHOST_SENTRY_RETIRE retired={retired} captured_denied={denied} bolt_gone={!GodotObject.IsInstanceValid(bolt)} live={f.LiveRounds().Length} phase={f.Timeline.Phase} hp={f.PlayerHealth.CurrentHealth} recover_observed_before_cancel=true");
            TestAssert.True(retired&&denied&&!GodotObject.IsInstanceValid(bolt)&&f.LiveRounds().Length==0&&f.Timeline.Phase==AttackPhase.None&&f.PlayerHealth.CurrentHealth==9,"actual launched bolt crosses Recover and is retired before hit, captured direct damage rejects inside retirement callback");
        });
        await Check("accepted_pending_K_crosses_hide_and_sleep_clock_pauses",async f=>
        {
            await Mount(f,.6f,.05f);await Manifest(f);await f.Frames(2);Node2D caster=f.Caster(new(200,300));
            TestAssert.True(Target(f).IsAvailable&&Target(f).TryApplyLegacyInteraction(caster,caster.GlobalPosition,new()),"last visible tick accepts real K before native next tick hides");
            await f.Frames(2);TestAssert.True(!Target(f).IsAvailable&&f.Stunned,"hide must preserve accepted pending K rather than clearing its request");
            f.Agent.SetResidentDormant(true);await f.Frames(45);f.Agent.SetResidentDormant(false);await f.Frames(12);
            TestAssert.True(!Target(f).IsAvailable&&f.Health.CurrentHealth==2&&f.Actor.IsOnFloor(),"sleep longer than Hidden duration pauses remaining phase and never heals/moves fixed turret");await Manifest(f);
        });
        await Check("native_lethal_reset_hidden_dual_groups_second_death",async f=>
        {
            await Mount(f,.6f,3);await Manifest(f);int resets=0,deaths=0,defeats=0,connections=0;var actor=(BellBoltSentry)f.Actor;
            f.Health.Died+=()=>deaths++;actor.Defeated+=()=>defeats++;
            f.OnPresentation=frame=>{if(resets==0&&frame.Timeline.Phase==AttackPhase.None&&f.Health.CurrentHealth==0){resets++;actor.ResetForCheckpoint();}};
            Hitbox attack=f.IncomingAttack(f.Actor.GlobalPosition);attack.Connected+=_=>connections++;attack.Begin(1310);await f.Frames(3);attack.End();
            TestAssert.True(resets==1&&connections==1&&deaths==1&&defeats==1&&f.Health.CurrentHealth==2,"actual native first death's None callback resets same actor");Hidden(f);
            TestAssert.True(f.Actor.GetNode<Hurtbox>("Hurtbox").Monitorable,"deferred incoming monitorability reflects new alive Hidden life, not old corpse");
            await Manifest(f);attack.Begin(1311);await f.Frames(3);attack.End();await f.Frames(80);
            TestAssert.True(deaths==2&&defeats==2&&f.Health.CurrentHealth==0&&!Target(f).IsAvailable&&f.LiveRounds().Length==0,"phase/sleep cannot resurrect the second corpse");
        });
        await Check("profiles_reject_before_commit",async f=>
        {
            var valid=Ghost(f,.2f,3);
            foreach(var invalid in new[]{valid with {CombatProfile=null},valid with {CombatProfile=new(ResidentBoneWeakness.RearOrExposed,.2f,3)},valid with {CombatProfile=new(ResidentBoneWeakness.None,0,3)},
                valid with {CombatProfile=new(ResidentBoneWeakness.None,float.NaN,3)},valid with {CombatProfile=new(ResidentBoneWeakness.None,.2f,float.PositiveInfinity)},
                valid with {CombatProfile=new(ResidentBoneWeakness.None,.2f,61)},valid with {Family=ResidentFamily.Scrap},valid with {Archetype=ResidentArchetype.Flyer}})
            {bool rejected=false;try{f.Agent.ConfigureResidence(invalid);}catch(ArgumentException){rejected=true;}TestAssert.True(rejected,"invalid Ghost/mixed-family profile must reject atomically");}
            f.Agent.ConfigureResidence(valid);await f.Mount(prepared:true);Hidden(f);await Manifest(f);TestAssert.True(f.Health.CurrentHealth==2,"invalid attempts preserve one later valid exact saved mount");
        });
        await Check("manifest_final_K_guards_and_healthless_positive",async f=>
        {
            await Mount(f,.2f,4);await Manifest(f);var receiver=Receiver(f);Node2D caster=f.Caster(new(100,300));var target=Target(f);
            TestAssert.True(!receiver.TryReceiveThread(new(null,Vector2.Zero,target.WorldAnchor,new())),"null source rejects without property reads");
            foreach(var settings in new ThreadAbilitySettings[]{null,new(EnemyPullSpeed:0),new(EnemyPullSpeed:float.NaN),new(EnemyPullSpeed:float.PositiveInfinity)})
                TestAssert.True(!receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,settings)),"Manifest rejects malformed settings without throw");
            CharacterBody2D foreign=f.ForeignPlayer(new(350,293));await f.Frames(1);TestAssert.True(!receiver.TryReceiveThread(new(foreign,foreign.GlobalPosition,target.WorldAnchor,new())),"same-tree foreign World source rejects");
            TestAssert.True(caster.GetNodeOrNull<HealthComponent>("Health")==null&&receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),"sameWorld living healthless source outside AI territory remains valid");
            Node2D parent=f.Caster(new(100,300));var child=new Node2D();parent.AddChild(child);var stale=new ThreadInteraction(child,child.GlobalPosition,target.WorldAnchor,new());parent.QueueFree();
            TestAssert.True(!receiver.TryReceiveThread(stale),"queued ancestor rejects");await f.Frames(2);TestAssert.True(!receiver.TryReceiveThread(stale),"captured freed source rejects");
            f.Actor.QueueFree();TestAssert.True(!receiver.TryReceiveThread(new(caster,caster.GlobalPosition,target.WorldAnchor,new())),"queued actor rejects direct captured K");
        });
        await Check("real_void_pending_hidden_sleep_deleted_source",async f=>
        {
            KillZone zone=f.Void();await Mount(f,.2f,.3f);await Manifest(f);f.Health.InvulnerabilitySeconds=1.2f;int deaths=0;f.Health.Died+=()=>deaths++;
            await Strike(f,1,true,1320);f.RemoveFloor();bool touched=false,hiddenBeforeContact=false;
            for(int i=0;i<80&&!touched;i++)
            {
                hiddenBeforeContact|=!Target(f).IsAvailable&&f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer!=0;
                Transform2D before=f.BodyPose();await f.Frames(1);touched=f.BodyTouchesDuring(before,zone);
            }
            TestAssert.True(touched&&hiddenBeforeContact&&f.Health.CurrentHealth==1&&deaths==0&&f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer==0,"actual gravity crosses independently observed Hidden then saved void while protected; life layer differs from Hidden alone");
            zone.QueueFree();f.Agent.SetResidentDormant(true);await f.Until(()=>f.Health.CurrentHealth==0,100);TestAssert.True(f.Health.CurrentHealth==0&&deaths==1&&f.RecoveryRequests==0,"pending native environment death survives Hidden/sleep/deleted source, never requests player recovery");
        });
        await Check("actual_saved_Player_K_then_horizontal_J",async f=>
        {
            f.SavedPlayer(new(380,293));await Mount(f,.6f,4);await Manifest(f);var combat=f.Target.GetNode<PlayerCombat>("Combat");var claw=f.Target.GetNode<MechanicalClawController>("MechanicalClaw");
            var adapter=f.Actor.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");int attacks=0,entries=0,connected=0,direction=-1;
            combat.AttackStarted+=(_,d)=>{attacks++;direction=d;};combat.AttackConnected+=hurt=>{if(hurt==f.Actor.GetNode<Hurtbox>("Hurtbox"))connected++;};
            combat.GetNode<Hitbox>("AttackHitbox").AreaEntered+=area=>{if(area==f.Actor.GetNode<Hurtbox>("Hurtbox"))entries++;};
            try
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.A,true);await f.Frames(2);KeyboardActionRuntimeTests.KeyEvent(Key.A,false);await f.Frames(2);
                TestAssert.True(f.Target.IsOnFloor()&&claw.KeyboardPreview.Surface==Target(f),$"real saved Player facing-left preview targets near turret: player={f.Target.GlobalPosition} turret={f.Actor.GlobalPosition}");
                KeyboardActionRuntimeTests.KeyEvent(Key.K,true);await f.Until(()=>adapter.ApplicationCount==1,45);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);TestAssert.Equal(1,adapter.ApplicationCount,"physical K reaches original adapter");
                KeyboardActionRuntimeTests.KeyEvent(Key.J,true);await f.Until(()=>connected==1,20);KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
                GD.Print($"RESIDENT_GHOST_SENTRY_PLAYER K={adapter.ApplicationCount} J={attacks} entries={entries} connected={connected} hp={f.Health.CurrentHealth} direction={direction} OS_origin=false");
                TestAssert.True(attacks==1&&entries==1&&connected==1&&direction==(int)AttackDirection.Horizontal&&f.Health.CurrentHealth==1,"saved Player original near-range K→J has real Area contact, not future ranged weapon or increased reach");
            }
            finally{KeyboardActionRuntimeTests.KeyEvent(Key.A,false);KeyboardActionRuntimeTests.KeyEvent(Key.K,false);KeyboardActionRuntimeTests.KeyEvent(Key.J,false);}
        });
        await Check("pending_cancel_Reset_does_not_kill_new_hidden_life",async f=>
        {
            KillZone zone=f.Void();await Mount(f,.2f,3);await Manifest(f);f.Health.InvulnerabilitySeconds=1.2f;int deaths=0;f.Health.Died+=()=>deaths++;
            await Strike(f,1,true,1321);f.RemoveFloor();await f.Until(()=>f.Actor.GetNode<Hurtbox>("Hurtbox").CollisionLayer==0,80);
            TestAssert.True(f.Health.CurrentHealth==1&&deaths==0,"real void latched before protected Health accepts fatality");zone.QueueFree();f.RestoreFloor();bool reset=false;
            f.OnPresentation=frame=>{if(!reset&&frame.Timeline.Phase==AttackPhase.None){reset=true;f.Reset();}};await f.Frames(2);
            TestAssert.True(reset&&deaths==0&&f.Health.CurrentHealth==2,"old pending call rechecks VoidPending after synchronous None callback Reset");Hidden(f);
        });
        await Check("hide_None_callback_Reset_preserves_full_new_hidden_phase",async f=>
        {
            f.Player(new(560,293));await Mount(f,.6f,.8f);await f.Until(()=>f.Timeline.HasLockedTarget,120);TestAssert.True(f.Timeline.HasLockedTarget,"real old locked warning before callback is armed");
            bool reset=false;f.OnPresentation=frame=>{if(!reset&&frame.Timeline.Phase==AttackPhase.None){reset=true;f.Reset();}};await f.Until(()=>reset,90);
            TestAssert.True(reset&&f.Health.CurrentHealth==2,"actual hide cancellation invokes observer Reset");Hidden(f);await f.Frames(20);Hidden(f);
            TestAssert.True(f.LiveRounds().Length==0&&f.PlayerHealth.CurrentHealth==9,"old phase/attack stack cannot overwrite new full Hidden phase or emit stale bolt");
        });
        GD.Print($"RESIDENT_GHOST_SENTRY_RESULT cases={cases} failures={failures.Count} saved_actor=true native_area=true formal_spawned=0");
        TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        async Task Check(string name,Func<Fixture,Task> test)
        {
            cases++;var f=new Fixture(tree,"BellBoltSentry");try{f.Initialize();await test(f);GD.Print("RESIDENT_GHOST_SENTRY_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_GHOST_SENTRY_CASE_FAIL "+name+": "+error.Message);}
            finally{await f.DisposeAsync();}
        }
        static ThreadTarget2D Target(Fixture f)=>f.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
        static IThreadInteractionReceiver Receiver(Fixture f)=>f.Actor as IThreadInteractionReceiver??throw new InvalidOperationException("real final bool Sentry receiver missing");
        static ResidentPopulationDefinition Ghost(Fixture f,float hidden,float manifest)=>new("ghost-tower.native.01","ghost-tower.native",CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,
            ResidentFamily.Ghost,ResidentArchetype.Turret,new(new(320,300),f.Territory,new[]{new Vector2(320,300)}),new(ResidentBoneWeakness.None,hidden,manifest));
        static async Task Mount(Fixture f,float hidden,float manifest){f.Agent.ConfigureResidence(Ghost(f,hidden,manifest));await f.Mount(prepared:true);}
        static async Task Manifest(Fixture f){await f.Until(()=>Target(f).IsAvailable,90);TestAssert.True(Target(f).IsAvailable,"natural finite phase manifests original target");}
        static void Hidden(Fixture f){var t=Target(f);TestAssert.True(!t.IsAvailable&&!t.IsInGroup(ThreadTarget2D.TargetGroup)&&!t.IsInGroup(GrappleSurface2D.TargetGroup)&&t.GetNode<Area2D>("TargetCollision").CollisionLayer==0,"Hidden withdraws both groups and native target, not only IsAvailable");}
        static async Task Strike(Fixture f,int damage,bool accepted,ulong id)
        {
            Hitbox hit=f.IncomingAttack(f.Actor.GlobalPosition,damage);int connected=0;hit.Connected+=_=>connected++;hit.Begin(id);await f.Frames(3);
            using Godot.Collections.Array overlaps=(Godot.Collections.Array)hit.GetOverlappingAreas();TestAssert.True(connected==1||overlaps.Contains(f.Actor.GetNode<Hurtbox>("Hurtbox")),"rejected J actually overlaps saved native hurtbox");
            hit.End();await f.Frames(2);TestAssert.Equal(accepted?1:0,connected,"native contact obeys Ghost damage gate before Health");
        }
    }

    private sealed class Fixture
    {
        private readonly SceneTree _tree;private readonly string _scene;private World2D _world,_foreignWorld;private SubViewport _view,_foreignView;private RecoveryScope _scope;private StaticBody2D _floor;
        private readonly List<Shape2D> _owned=new();private bool _mounted;private readonly bool _identifyShapes;
        public bool ShapeIdentityLogged {get;private set;}
        public int RecoveryRequests=>_scope.Calls;
        public CharacterBody2D Actor {get;private set;}public CharacterBody2D Target {get;private set;}
        public HealthComponent Health=>Actor.GetNode<HealthComponent>("Health");public HealthComponent PlayerHealth=>Target.GetNode<HealthComponent>("Health");
        public IResidentEnemyAgent Agent=>Actor as IResidentEnemyAgent??throw new InvalidOperationException("Saved turret has no residence adapter: "+_scene);
        public AttackTimelineFrame Timeline {get;private set;}public Action<AttackPresentationFrame> OnPresentation;public int Shots {get;private set;}
        public AttackShapeFrame Shape {get;private set;}public Rect2 Territory=new(256,224,368,144);
        public bool Laser=>Actor is PrismLaserGunner;
        public bool Recovering=>Actor is RangedWeaponEnemy ranged?ranged.CurrentFrame.State==RangedWeaponState.Recovery:((BellBoltSentry)Actor).CurrentFrame.State==BellBoltSentryState.Recover;
        public bool Stunned=>Actor is RangedWeaponEnemy ranged?ranged.CurrentFrame.State==RangedWeaponState.Stunned:((BellBoltSentry)Actor).CurrentFrame.State==BellBoltSentryState.Stunned;
        public Fixture(SceneTree tree,string scene,bool identifyShapes=false){_tree=tree;_scene=scene;_identifyShapes=identifyShapes;}
        public void Initialize()
        {
            using Godot.Collections.Array players=(Godot.Collections.Array)_tree.GetNodesInGroup("player");TestAssert.Equal(0,players.Count,"no unrelated player in turret fixture");
            _world=new World2D();_view=new SubViewport{Size=new(1024,640),World2D=_world};_scope=new RecoveryScope();_view.AddChild(_scope);_tree.Root.AddChild(_view);
            var floor=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            floor.AddChild(new CollisionShape2D{Position=new(532,349),Shape=Own(new RectangleShape2D{Size=new(1000,40)})});_scope.AddChild(floor);_floor=floor;
            Actor=GD.Load<PackedScene>("res://scenes/enemies/"+_scene+".tscn").Instantiate<CharacterBody2D>();
            if(Actor is RangedWeaponEnemy ranged){ranged.AttackFrameChanged+=Present;ranged.ShotEmitted+=(_,_)=>Shots++;}
            else ((BellBoltSentry)Actor).AttackFrameChanged+=Present;
        }
        private void Present(AttackPresentationFrame frame){Timeline=frame.Timeline;Shape=frame.Shape;OnPresentation?.Invoke(frame);}
        public async Task Mount(bool configured=true,bool prepared=false)
        {
            Actor.Position=new(320,300);
            if(!prepared)
            {
                if(Actor is RangedWeaponEnemy ranged)ranged.ConfigureEncounterEnvelope(configured?Territory:new(32,32,960,512));
                if(configured)Agent.ConfigureResidence(new("scrap-turret.01","scrap-turret.fixture",CanyonRouteNodeId.LowCeilingBridge,PrologueSectorId.CanyonEntrance,
                    ResidentFamily.Scrap,ResidentArchetype.Turret,new(new(320,300),Territory,new[]{new Vector2(320,300)})));
            }
            _scope.AddChild(Actor);_mounted=true;await Frames(6);
            TestAssert.True(Actor.IsOnFloor()&&Actor.GetWorld2D()==_world&&_world!=_tree.Root.FindWorld2D(),"saved turret reaches real floor in its own World2D");
            if(_identifyShapes)LogNativeShapeIdentity(configured);
        }
        private void LogNativeShapeIdentity(bool configured)
        {
            // Only query native RIDs while their collision objects are mounted.
            // The ranged private beam query shape is NOT mounted and is not
            // covered by this observation. Never query freed RIDs for liveness.
            Rid body=Actor.GetRid(),hurt=Actor.GetNode<Area2D>("Hurtbox").GetRid(),
                thread=Actor.GetNode<Area2D>("ThreadTarget/TargetCollision").GetRid();
            TestAssert.True(PhysicsServer2D.BodyGetShapeCount(body)==1&&PhysicsServer2D.AreaGetShapeCount(hurt)==1&&
                PhysicsServer2D.AreaGetShapeCount(thread)==1,"saved body/hurt/thread each mount their one known native shape");
            ulong bodyShape=PhysicsServer2D.BodyGetShape(body,0).Id,hurtShape=PhysicsServer2D.AreaGetShape(hurt,0).Id,
                threadShape=PhysicsServer2D.AreaGetShape(thread,0).Id;
            string owned=string.Join(",",_owned.Select(shape=>shape.GetRid().Id));
            GD.Print($"RESIDENT_TURRET_SHAPE_IDENTITY scene={_scene} configured={configured} actor={Actor.GetInstanceId()} world={_world.GetInstanceId()} body={bodyShape} hurt={hurtShape} thread={threadShape} fixture_owned={owned} private_query_covered=false");
            ShapeIdentityLogged=true;
        }
        public void Player(Vector2 position)
        {
            Target=CreatePlayer(position,true);
        }
        public Node2D Caster(Vector2 position){var node=new Node2D{Position=position};_scope.AddChild(node);return node;}
        public void SavedPlayer(Vector2 position)
        {
            Target=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();Target.Position=position;Target.AddToGroup("player");_scope.AddChild(Target);
        }
        public CharacterBody2D ExtraPlayer(Vector2 position)=>CreatePlayer(position,false);
        public CharacterBody2D ForeignPlayer(Vector2 position)
        {
            _foreignWorld=new World2D();_foreignView=new SubViewport{Size=new(1024,640),World2D=_foreignWorld};
            _view.AddChild(_foreignView);return CreatePlayer(position,true,_foreignView);
        }
        public StaticBody2D Wall(Vector2? position=null)
        {
            var wall=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            wall.AddChild(new CollisionShape2D{Position=position??new(440,285),Shape=Own(new RectangleShape2D{Size=new(18,88)})});_scope.AddChild(wall);return wall;
        }
        public bool LiteralSightHit(StaticBody2D expected)
        {
            using var ray=PhysicsRayQueryParameters2D.Create(new(320,292),new(560,293),CombatCollisionLayers.World);
            ray.CollideWithBodies=true;ray.CollideWithAreas=false;ray.HitFromInside=true;
            using Godot.Collections.Dictionary hit=_world.DirectSpaceState.IntersectRay(ray);
            return expected==null?hit.Count==0:hit.TryGetValue("collider",out Variant collider)&&ReferenceEquals(collider.AsGodotObject(),expected);
        }
        private CharacterBody2D CreatePlayer(Vector2 position,bool grouped,Node parent=null)
        {
            var player=new CharacterBody2D{Position=position,CollisionLayer=CombatCollisionLayers.Player,CollisionMask=CombatCollisionLayers.World};
            player.AddChild(new CollisionShape2D{Shape=Own(new CapsuleShape2D{Radius=20,Height=72})});player.AddChild(new HealthComponent{Name="Health",MaxHealth=9,InvulnerabilitySeconds=0});
            var hurt=new Hurtbox{Name="Hurtbox",CollisionLayer=CombatCollisionLayers.Player,CollisionMask=0};
            hurt.AddChild(new CollisionShape2D{Shape=Own(new CapsuleShape2D{Radius=20,Height=72})});player.AddChild(hurt);if(grouped)player.AddToGroup("player");(parent??_scope).AddChild(player);return player;
        }
        public Node[] LiveRounds()
        {
            Godot.Collections.Array<Node> children=Actor.GetChildren();using Godot.Collections.Array backing=(Godot.Collections.Array)children;
            return children.Where(x=>GodotObject.IsInstanceValid(x)&&!x.IsQueuedForDeletion()&&
                (x is RangedWeaponProjectile p&&!p.IsRetired||x is BellBoltProjectile b&&!b.IsRetired)).ToArray();
        }
        public bool Retired(Node round)=>round is RangedWeaponProjectile p?p.IsRetired:((BellBoltProjectile)round).IsRetired;
        public int AuthorizedLeaves()
        {
            if(Actor is not RangedWeaponEnemy ranged)return LiveRounds().Length;
            var leaves=new List<AttackTelegraphVisual>();ranged.AppendAuthorizedAttackLeaves(leaves);return leaves.Count;
        }
        public int LiveImpacts()
        {
            Node effects=Actor.GetNode<Node>("VisualRoot/Effects");Godot.Collections.Array<Node> children=effects.GetChildren();
            using Godot.Collections.Array backing=(Godot.Collections.Array)children;
            return children.Count(x=>x is RangedWeaponImpactRing&&!x.IsQueuedForDeletion());
        }
        public Hitbox IncomingAttack(Vector2 position,int damage=2)
        {
            var attack=new Hitbox{Position=position,Damage=damage,Knockback=0,CollisionLayer=CombatCollisionLayers.PlayerAttack,
                CollisionMask=CombatCollisionLayers.Enemy,Monitorable=false};
            attack.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(64,68)})});_scope.AddChild(attack);return attack;
        }
        public Hitbox PlayerLethalAttack()
        {
            Hurtbox old=Target.GetNode<Hurtbox>("Hurtbox");
            var attack=new Hitbox{Position=new(560,293),Damage=9,Knockback=0,CollisionLayer=CombatCollisionLayers.EnemyAttack,
                CollisionMask=CombatCollisionLayers.Player,Monitorable=false,TargetFilter=hurt=>ReferenceEquals(hurt,old)};
            attack.AddChild(new CollisionShape2D{Shape=Own(new RectangleShape2D{Size=new(64,72)})});_scope.AddChild(attack);return attack;
        }
        public KillZone Void(bool disabled=false)
        {
            var zone=new KillZone{CollisionLayer=0,CollisionMask=CombatCollisionLayers.Player,Monitoring=true,RoomControllerPath=new NodePath("..")};
            zone.AddChild(new CollisionShape2D{Name="Collision",Disabled=disabled,Position=new(440,408),Shape=Own(new RectangleShape2D{Size=new(368,16)})});_scope.AddChild(zone);return zone;
        }
        public void RemoveFloor()=>_floor.QueueFree();
        public void RestoreFloor()
        {
            _floor=new StaticBody2D{CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
            _floor.AddChild(new CollisionShape2D{Position=new(532,349),Shape=Own(new RectangleShape2D{Size=new(1000,40)})});_scope.AddChild(_floor);
        }
        public void Reset(){if(Actor is RangedWeaponEnemy ranged)ranged.ResetForCheckpoint();else ((BellBoltSentry)Actor).ResetForCheckpoint();}
        public Transform2D BodyPose()=>Actor.GetNode<CollisionShape2D>("BodyCollider").GlobalTransform;
        public bool BodyTouchesDuring(Transform2D before,KillZone zone)
        {
            CollisionShape2D body=Actor.GetNode<CollisionShape2D>("BodyCollider"),source=zone.GetNode<CollisionShape2D>("Collision");Transform2D after=body.GlobalTransform;
            return body.Shape.Collide(before,source.Shape,source.GlobalTransform)||
                body.Shape.CollideWithMotion(before,after.Origin-before.Origin,source.Shape,source.GlobalTransform,Vector2.Zero)||
                body.Shape.Collide(after,source.Shape,source.GlobalTransform);
        }
        public async Task Frames(int count){for(int i=0;i<count;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}}
        public async Task Until(Func<bool> predicate,int count){for(int i=0;i<count&&!predicate();i++)await Frames(1);}
        private T Own<T>(T shape)where T:Shape2D{_owned.Add(shape);return shape;}
        public async Task DisposeAsync()
        {
            if(!_mounted&&GodotObject.IsInstanceValid(Actor))Actor.Free();if(GodotObject.IsInstanceValid(_view))_view.QueueFree();
            await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            bool gone=!GodotObject.IsInstanceValid(Actor)&&!GodotObject.IsInstanceValid(Target)&&!GodotObject.IsInstanceValid(_scope)&&!GodotObject.IsInstanceValid(_view)&&!GodotObject.IsInstanceValid(_foreignView);
            int allocated=_owned.Count,disposed=0;foreach(Shape2D shape in _owned){shape.Dispose();disposed++;}_owned.Clear();_world?.Dispose();
            _foreignWorld?.Dispose();_foreignWorld=null;_foreignView=null;
            Actor=null;Target=null;_scope=null;_floor=null;_view=null;_world=null;OnPresentation=null;
            GD.Print($"RESIDENT_TURRET_TEARDOWN native_gone={gone} owned_allocated={allocated} owned_disposed={disposed} borrowed_disposed=0");
            TestAssert.True(gone&&allocated==disposed,"owned turret graph is gone before disposing only owned shapes/world");
        }
    }
    private partial class RecoveryScope:Node2D,ILevelRecovery{public int Calls {get;private set;}public void RequestRespawn(string reason)=>Calls++;}
}
