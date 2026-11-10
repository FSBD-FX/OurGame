using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Ranged;
using Knight.Enemies.Residents;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    internal readonly record struct GhostForeignWindow(bool Danger, int Actors, int ExcludedResidents,
        int Projectiles, ulong BlockingActor, string Reason);

    // Test input planning only. Actual captured damage permissions, not a
    // distance/home/render-opacity heuristic. Unknown fields/types fail closed.
    internal static GhostForeignWindow ReadGhostForeignWindow(IEnumerable<Node> roots,
        CharacterBody2D player, Node2D selected, Rect2 envelope, float requiredSeconds)
    {
        bool danger=false;
        int actors=0, excluded=0, projectiles=0;
        ulong blocker=0;
        string reason="none";
        void Reject(Node node,string why)
        { danger=true; if(reason=="none") { blocker=node?.GetInstanceId()??0; reason=why; } }
        bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
        bool Bounds(Rect2 value) => value.Position.IsFinite() && value.Size.IsFinite() && value.End.IsFinite() &&
            value.Size.X>0 && value.Size.Y>0;
        if(!Live(player) || !Live(selected) || selected is not RustShellSquire ||
            selected.SceneFilePath!=GhostLandingGuardRuntimeTests.ScenePath || selected.GetWorld2D()!=player.GetWorld2D() ||
            !Bounds(envelope) || !float.IsFinite(requiredSeconds) || requiredSeconds<=0 || roots==null ||
            !ReadGhostPlayerBounds(player,out Rect2 body,out Rect2 hurt) || !envelope.Encloses(body) || !envelope.Encloses(hurt))
            return new(true,0,0,0,0,"invalid_scope_or_envelope");
        try
        {
            var seen=new HashSet<ulong>();
            int rootCount=0;
            foreach(Node root in roots)
            {
                rootCount++;
                if(!Live(root)) { Reject(root,"invalid_actor_root"); continue; }
                Visit(root,null,false,default);
            }
            if(rootCount==0) Reject(null,"empty_actor_scope");
            void Visit(Node node,Node2D owner,bool excludedOwner,AttackTimelineFrame ownerTimeline)
            {
                if(!Live(node)) { Reject(null,"changing_subtree"); return; }
                if(!seen.Add(node.GetInstanceId()) || node==player) return;
                // A projectile is checked BEFORE any owner's territory exclusion.
                if(node is RangedWeaponProjectile {IsRetired:false} or BellBoltProjectile {IsRetired:false})
                { projectiles++; Reject(node,"nonretired_projectile"); }
                if(node is Node2D actor && actor.GetNodeOrNull<HealthComponent>("Health") is {} health)
                {
                    owner=actor; excludedOwner=actor==selected;
                    ownerTimeline=default;
                    if(health.CurrentHealth>0 && !GhostProductionBindingsMatch(actor))
                        Reject(actor,"changed_or_unknown_production_binding");
                    if(health.CurrentHealth>0 && actor!=selected)
                    {
                        actors++;
                        if(actor.GetWorld2D()!=player.GetWorld2D()) Reject(actor,"foreign_world");
                        bool known=actor is RustShellSquire or BellBackLaborer or GraveLampMoth or CanyonSkitter or
                            BellBoltSentry or RangedWeaponEnemy;
                        if(!known) Reject(actor,"unknown_living_actor");
                        else if(TryGhostCapturedTerritory(actor,out Rect2 territory) && !territory.Intersects(envelope,true))
                        { excludedOwner=true; excluded++; }
                        else
                        {
                            // A saved resident with an unreadable capture may not
                            // silently borrow ordinary non-resident permission.
                            if(actor.HasNode("ResidentBinding") && !TryGhostCapturedTerritory(actor,out _))
                                Reject(actor,"unknown_resident_capture");
                            ownerTimeline=GhostActorTimeline(actor);
                            float allowance=ownerTimeline.Phase switch
                            {
                                AttackPhase.None => Task10PassiveRearmAllowance(actor),
                                AttackPhase.Recovery or AttackPhase.Telegraph => ownerTimeline.PhaseRemainingSeconds,
                                _ => 0f
                            };
                            if(ownerTimeline.Phase is AttackPhase.Recovery or AttackPhase.Telegraph)
                            {
                                var publication=GhostBoundPublication(actor);
                                if(ownerTimeline.AttackId==0 || !Live(publication) ||
                                    publication.CurrentFrame.Timeline.AttackId!=ownerTimeline.AttackId ||
                                    publication.CurrentFrame.Timeline.Phase!=ownerTimeline.Phase)
                                    Reject(actor,"unmatched_foreign_publication");
                            }
                            if(!float.IsFinite(allowance) || allowance<requiredSeconds)
                                Reject(actor,"foreign_complete_counter_window");
                        }
                    }
                }
                if(node is Hitbox {Monitoring:true} &&
                    (!excludedOwner || owner==null || !ReferenceEquals(node,GhostBoundHitbox(owner))))
                    Reject(owner??node,"foreign_or_unowned_active_hitbox");
                if(node is AttackTelegraphVisual leaf &&
                    leaf.CurrentFrame.Timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active)
                {
                    bool exactLeaf=owner!=null && ReferenceEquals(node,GhostBoundPublication(owner));
                    if(!exactLeaf || !excludedOwner &&
                        (leaf.CurrentFrame.Timeline.Phase==AttackPhase.Active ||
                         leaf.CurrentFrame.Timeline.AttackId!=ownerTimeline.AttackId ||
                         leaf.CurrentFrame.Timeline.Phase!=ownerTimeline.Phase))
                        Reject(owner??node,"unowned_or_active_publication");
                }
                for(int index=0;index<node.GetChildCount();index++) Visit(node.GetChild(index),owner,excludedOwner,ownerTimeline);
            }
        }
        catch(Exception error) { danger=true; reason="observation_"+error.GetType().Name; }
        return new(danger,actors,excluded,projectiles,blocker,reason);
    }

    private static Hitbox GhostBoundHitbox(Node2D actor)
    {
        // Preserve the existing conservative Skitter Rush rejection (review M1).
        // Do not invent an AttackHitbox binding from a same-named replacement.
        Type type=actor switch
        {
            RustShellSquire => typeof(RustShellSquire),
            BellBackLaborer => typeof(BellBackLaborer),
            GraveLampMoth => typeof(GraveLampMoth),
            _ => null
        };
        return type?.GetField("_attackHitbox",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(actor) as Hitbox;
    }

    private static AttackTelegraphVisual GhostBoundPublication(Node2D actor)
    {
        (Type type,string field)=actor switch
        {
            RustShellSquire => (typeof(RustShellSquire),"_attackTelegraph"),
            BellBackLaborer => (typeof(BellBackLaborer),"_attackTelegraph"),
            GraveLampMoth => (typeof(GraveLampMoth),"_attackTelegraph"),
            CanyonSkitter => (typeof(CanyonSkitter),"_telegraph"),
            BellBoltSentry => (typeof(BellBoltSentry),"_telegraph"),
            RangedWeaponEnemy => (typeof(RangedWeaponEnemy),"_telegraph"),
            _ => (null,null)
        };
        return type?.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(actor) as AttackTelegraphVisual;
    }

    private static bool GhostProductionBindingsMatch(Node2D actor)
    {
        bool Owned(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() &&
            !node.IsQueuedForDeletion() && actor.IsAncestorOf(node);
        AttackTelegraphVisual leaf=GhostBoundPublication(actor);
        if(!Owned(leaf) || !ReferenceEquals(leaf,actor.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph")))
            return false;
        if(actor is not (RustShellSquire or BellBackLaborer or GraveLampMoth)) return true;
        Hitbox hit=GhostBoundHitbox(actor);
        return Owned(hit) && ReferenceEquals(hit,actor.GetNodeOrNull<Hitbox>("AttackHitbox"));
    }

    private static AttackTimelineFrame GhostActorTimeline(Node2D actor) => actor switch
    {
        RustShellSquire rust => rust.CurrentFrame.AttackTimeline,
        BellBackLaborer bell => bell.CurrentFrame.AttackTimeline,
        GraveLampMoth moth => moth.CurrentFrame.AttackTimeline,
        CanyonSkitter skitter => skitter.CurrentFrame.AttackTimeline,
        BellBoltSentry sentry => sentry.CurrentFrame.AttackTimeline,
        RangedWeaponEnemy ranged => ranged.CurrentFrame.AttackTimeline,
        _ => default
    };

    private static bool TryGhostCapturedTerritory(Node2D actor,out Rect2 territory)
    {
        territory=default;
        (Type type,string field)=actor switch
        {
            RustShellSquire => (typeof(RustShellSquire),"_residence"),
            BellBackLaborer => (typeof(BellBackLaborer),"_heavyResidence"),
            GraveLampMoth => (typeof(GraveLampMoth),"_airResidence"),
            CanyonSkitter => (typeof(CanyonSkitter),"_residence"),
            BellBoltSentry => (typeof(BellBoltSentry),"_residence"),
            RangedWeaponEnemy => (typeof(RangedWeaponEnemy),"_residence"),
            _ => (null,null)
        };
        object capture=type?.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(actor);
        if(capture is ResidentPlacementCandidate placement) territory=placement.Territory;
        else if(capture is ResidentTurretResidence turret) territory=turret.Territory;
        else return false;
        return territory.Position.IsFinite() && territory.Size.IsFinite() && territory.End.IsFinite() &&
            territory.Size.X>0 && territory.Size.Y>0;
    }

    private static bool ReadGhostPlayerBounds(CharacterBody2D player,out Rect2 body,out Rect2 hurt)
    {
        body=hurt=default;
        var bodyShape=player.GetNodeOrNull<CollisionShape2D>("BodyCollider");
        var hurtShape=player.GetNodeOrNull<CollisionShape2D>("Hurtbox/HurtboxCollider");
        if(bodyShape?.Shape==null || hurtShape?.Shape==null || bodyShape.Disabled || hurtShape.Disabled) return false;
        body=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
        hurt=hurtShape.GlobalTransform*hurtShape.Shape.GetRect();
        return body.Position.IsFinite() && body.Size.IsFinite() && hurt.Position.IsFinite() && hurt.Size.IsFinite() &&
            body.Size.X>0 && body.Size.Y>0 && hurt.Size.X>0 && hurt.Size.Y>0;
    }

    private static async Task ClearTask10Grip8Ghost(SceneTree tree,PrologueLevelController level,
        SeamlessPrologueWorld world,CharacterBody2D player,RouteMonitor monitor,StaticBody2D support,
        Action observe,Func<Action,Action> swapPreObserverGuard)
    {
        var driver=KeyboardRouteInputDriver.Current ?? throw new InvalidOperationException("Grip8Combat needs original keyboard owner");
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonAscent,out var ascent),"Grip8Combat original sector mounted");
        Node actors=ascent.GetNode<Node>("Gameplay/Actors");
        var candidates=new List<RustShellSquire>();
        for(int index=0;index<actors.GetChildCount();index++)
            if(actors.GetChild(index) is RustShellSquire actor && actor.SceneFilePath==GhostLandingGuardRuntimeTests.ScenePath &&
                actor.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding")?.Profile?.SubtypeId=="lost-echo" &&
                actor.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is >0) candidates.Add(actor);
        TestAssert.Equal(1,candidates.Count,"Grip8Combat selects one current saved lost-echo, never an old runtime ID");
        RustShellSquire target=candidates[0];
        var targetHealth=target.GetNode<HealthComponent>("Health");
        var targetHurt=target.GetNode<Hurtbox>("Hurtbox");
        var playerHealth=player.GetNode<HealthComponent>("Health");
        var motor=player.GetNode<PlayerMotor>("Motor");
        var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
        var shield=player.GetNode<PlayerShieldController>("Shield");
        var combat=player.GetNode<PlayerCombat>("Combat");
        var input=player.GetNode<PlayerInputReader>("Input");
        var energy=player.GetNode<PlayerEnergyController>("Energy");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
        var floorShape=support.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(ReadGhostPlayerBounds(player,out Rect2 body,out Rect2 hurt),"Grip8Combat actual player shapes valid");
        Rect2 merged=body.Merge(hurt);
        Rect2 envelope=(floorShape.GlobalTransform*floorShape.Shape.GetRect()).GrowIndividual(
            merged.Size.X*2,merged.Size.Y*2,merged.Size.X*2,merged.Size.Y*2);
        ulong began=Engine.GetPhysicsFrames(), playerId=player.GetInstanceId(), targetId=target.GetInstanceId(),
            nextCast=claw.CurrentFrame.NextCastId, issueSequence=0;
        int hp=playerHealth.CurrentHealth, requests=monitor.RecoveryRequested,recoveries=monitor.RecoveryCompleted,
            jumps=motor.AcceptedJumpCount,dashes=motor.AcceptedDashCount,casts=driver.ClawEdges,
            attackEdges=driver.AttackEdges,shieldEdges=driver.ShieldEdges,originalTargetHp=targetHealth.CurrentHealth;
        int starts=0,connections=0,damage=0,hits=0;
        bool rawJ=false,rawKeyAtStart=false;
        ulong startedAttack=0,startSequence=0;
        PlayerCombat.AttackStartedEventHandler started=(id,_) =>
        {
            starts++; startedAttack=id; startSequence=input.CurrentFrameSequence;
            rawKeyAtStart=Input.IsPhysicalKeyPressed(Key.J);
            rawJ=rawKeyAtStart && startSequence>issueSequence;
        };
        PlayerCombat.AttackConnectedEventHandler connected=victim => { if(victim==targetHurt) connections++; };
        HealthComponent.DamagedEventHandler damaged=(_,_) => damage++;
        Action previous=null;
        bool guardInstalled=false;
        Exception primary=null;
        void Retained()
        {
            bool bounds=ReadGhostPlayerBounds(player,out Rect2 nowBody,out Rect2 nowHurt);
            bool floor=TryTask10FullFloorSupport(player,out ulong floorId) && floorId==support.GetInstanceId();
            TestAssert.True(Engine.GetPhysicsFrames()-began<=600 && level.Player==player && monitor.Player==player &&
                player.GetInstanceId()==playerId && player.GlobalPosition.IsFinite() && player.Velocity.IsFinite() &&
                GodotObject.IsInstanceValid(target) && target.IsInsideTree() && !target.IsQueuedForDeletion() &&
                target.GetInstanceId()==targetId && target.SceneFilePath==GhostLandingGuardRuntimeTests.ScenePath &&
                hp>0 && playerHealth.CurrentHealth==hp && requests==0 && recoveries==0 &&
                monitor.RecoveryRequested==requests && monitor.RecoveryCompleted==recoveries &&
                motor.AcceptedJumpCount==jumps && motor.AcceptedDashCount==dashes &&
                claw.CurrentFrame.State==MechanicalClawState.Idle && claw.CurrentFrame.NextCastId==nextCast &&
                driver.ClawEdges==casts && flow.State==PrologueGameFlowState.Playing && !tree.Paused &&
                floor && bounds && envelope.Encloses(nowBody) && envelope.Encloses(nowHurt),
                $"Grip8Combat retained actual floor/player/HP/recovery/envelope/no extra K/jump/jet " +
                $"physics={Engine.GetPhysicsFrames()-began}/600 hp={hp}->{playerHealth.CurrentHealth} floor={floor} " +
                $"player={player.GlobalPosition} velocity={player.Velocity} claw={claw.CurrentFrame.State}");
        }
        async Task Tick()
        {
            Retained();
            TestAssert.True(Engine.GetPhysicsFrames()-began<600,"Grip8Combat actual shared deadline before every nested wait");
            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
            observe();
            Retained();
        }
        GhostForeignWindow Foreign()
        {
            var roots=new List<Node>();
            foreach(var sectorId in world.MountedSectorIds)
            {
                if(!world.TryGetMountedSector(sectorId,out var sector) || sector.GetNodeOrNull<Node>("Gameplay/Actors") is not {} root)
                    return new(true,0,0,0,0,"missing_mounted_actor_root");
                roots.Add(root);
            }
            float required=combat.Tuning.AttackStartupSeconds+combat.Tuning.AttackActiveSeconds+
                combat.Tuning.AttackRecoverySeconds+8f/Engine.PhysicsTicksPerSecond;
            return ReadGhostForeignWindow(roots,player,target,envelope,required);
        }
        try
        {
            driver.ReleaseAll();
            Retained();
            previous=swapPreObserverGuard(() => { previous?.Invoke(); Retained(); });
            guardInstalled=true;
            combat.AttackStarted+=started; combat.AttackConnected+=connected; targetHealth.Damaged+=damaged;
            GD.Print($"GRIP8_COMBAT_BEGIN physics={began} target={targetId} hurt={targetHurt.GetInstanceId()} " +
                $"scene={target.SceneFilePath} subtype=lost-echo target_hp={originalTargetHp} player_hp={hp} " +
                $"envelope={envelope} transfer8_completed=true transfer9_started=false");
            while(targetHealth.CurrentHealth>0)
            {
                Retained();
                var timeline=target.CurrentFrame.AttackTimeline;
                bool guard=GhostLandingGuardRuntimeTests.TrySample(target,player,out Vector2 point,out _) &&
                    (timeline.Phase==AttackPhase.Active || timeline.Phase==AttackPhase.Telegraph &&
                     timeline.PhaseRemainingSeconds<=8f/Engine.PhysicsTicksPerSecond) && shield.CanBeginGuard;
                if(guard)
                {
                    Vector2 direction=BasinCounterweightRuntimeTests.StationaryGuardDirection(
                        player.GetNode<Hurtbox>("Hurtbox").GlobalPosition,point,[point],true);
                    driver.PointerButton(MouseButton.Right,true,player,player.GlobalPosition+direction*96);
                }
                else driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
                GhostForeignWindow foreign=Foreign();
                if(!GhostLandingGuardRuntimeTests.CanCounter(target,player,targetId,timeline.AttackId,foreign.Danger))
                {
                    if((Engine.GetPhysicsFrames()-began)%60==0)
                        GD.Print($"GRIP8_COMBAT_WAIT physics={Engine.GetPhysicsFrames()-began} target_phase={timeline.Phase} " +
                            $"remaining={timeline.PhaseRemainingSeconds} foreign={foreign.Danger} reason={foreign.Reason} " +
                            $"actors={foreign.Actors} excluded={foreign.ExcludedResidents} projectiles={foreign.Projectiles} blocker={foreign.BlockingActor}");
                    await Tick(); continue;
                }
                bool left=target.GlobalPosition.X<player.GlobalPosition.X;
                driver.Command(InputActions.MoveUp,false); driver.Command(InputActions.MoveDown,false);
                driver.Command(InputActions.MoveLeft,left); driver.Command(InputActions.MoveRight,!left);
                await Tick();
                driver.Command(InputActions.MoveLeft,false); driver.Command(InputActions.MoveRight,false);
                await Tick();
                timeline=target.CurrentFrame.AttackTimeline;
                foreign=Foreign(); // Fresh after releasing L and sampling the real facing.
                if(!GhostLandingGuardRuntimeTests.CanCounter(target,player,targetId,timeline.AttackId,foreign.Danger)) continue;
                int beforeHp=targetHealth.CurrentHealth,beforeStarts=starts,beforeConnections=connections,beforeDamage=damage;
                issueSequence=input.CurrentFrameSequence;
                driver.Command(InputActions.Attack,true);
                await Tick();
                // PhysicsFrame resumes before all node callbacks. Keep the ONE
                // original J edge held until its actual AttackStarted consumes
                // a fresh reader sample; never replay or synthesize that edge.
                for(int arm=0;arm<4 && starts==beforeStarts;arm++) await Tick();
                driver.Command(InputActions.Attack,false);
                for(int wait=0;wait<40 && (starts==beforeStarts || combat.IsAttacking);wait++) await Tick();
                GD.Print($"GRIP8_COMBAT_J physics={Engine.GetPhysicsFrames()-began} source_attack={timeline.AttackId} " +
                    $"player_attack={startedAttack} hp={beforeHp}->{targetHealth.CurrentHealth} starts={starts-beforeStarts} " +
                    $"connected={connections-beforeConnections} damage={damage-beforeDamage} raw_J={rawJ} " +
                    $"raw_key_J={rawKeyAtStart} issue_seq={issueSequence} start_seq={startSequence} " +
                    $"foreign_actors={foreign.Actors} excluded={foreign.ExcludedResidents} projectiles={foreign.Projectiles} " +
                    $"player={player.GlobalPosition} target={target.GlobalPosition} energy={energy.EnergyRatio}");
                TestAssert.True(starts==beforeStarts+1 && connections==beforeConnections+1 && damage==beforeDamage+1 &&
                    targetHealth.CurrentHealth==beforeHp-1 && rawJ,"Grip8Combat fresh J must connect to exact Hurtbox and reduce actual target Health once");
                hits++;
            }
            // Deferred corpse collision withdrawal is observed, not forced.
            await Tick(); await Tick();
            TestAssert.True(hits==originalTargetHp && starts==hits && connections==hits && damage==hits &&
                driver.AttackEdges-attackEdges==hits && !targetHurt.Monitorable && target.CollisionLayer==0,
                "Grip8Combat actual death withdraws corpse obstruction without deleting/resetting the enemy");
            GD.Print($"GRIP8_COMBAT_CLEAR_OK actual_hits={hits} target_hp={originalTargetHp}->0 player_hp={hp}->{playerHealth.CurrentHealth} " +
                $"physics={Engine.GetPhysicsFrames()-began}/600 J={driver.AttackEdges-attackEdges} K={driver.ClawEdges-casts} " +
                $"L={driver.ShieldEdges-shieldEdges} source=actual_Grip8 whole_route=false human=false");
        }
        catch(Exception error) { primary=error; throw; }
        finally
        {
            Exception cleanup=null;
            try { driver.ReleaseAll(); } catch(Exception error) { cleanup=error; }
            try { if(guardInstalled) swapPreObserverGuard(previous); } catch(Exception error) { cleanup??=error; }
            try { if(GodotObject.IsInstanceValid(combat)) { combat.AttackStarted-=started; combat.AttackConnected-=connected; } }
            catch(Exception error) { cleanup??=error; }
            try { if(GodotObject.IsInstanceValid(targetHealth)) targetHealth.Damaged-=damaged; }
            catch(Exception error) { cleanup??=error; }
            if(cleanup!=null) { if(primary==null) throw cleanup; GD.PushError("Grip8Combat cleanup after primary: "+cleanup); }
        }
    }

#if CLAW_OBSTRUCTION_IDENTITY
    // One passive snapshot after the ORIGINAL Grip8 transfer/guard has returned.
    // No await, input, ray-query, actor writes or permission is added here. This
    // inventory is deliberately NOT a claim that foreign attack sources are safe.
    private static void ObserveTask10Grip8CombatInventory(SeamlessPrologueWorld world,
        CharacterBody2D player, StaticBody2D support)
    {
        try
        {
            int actors = 0, liveProjectiles = 0, activeHitboxes = 0;
            bool full = TryTask10FullFloorSupport(player, out ulong floorId);
            GD.Print($"GRIP8_COMBAT_INVENTORY_BEGIN physics={Engine.GetPhysicsFrames()} " +
                $"player_id={player.GetInstanceId()} position={player.GlobalPosition} velocity={player.Velocity} " +
                $"hp={player.GetNode<HealthComponent>("Health").CurrentHealth} " +
                $"energy={player.GetNode<PlayerEnergyController>("Energy").EnergyRatio} " +
                $"claw={player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State} " +
                $"full_support={full} floor={floorId} expected_floor={support.GetInstanceId()} " +
                "source=post_original_grip8_return input_changes=0 safety_admission=false");
            foreach (var sectorId in world.MountedSectorIds)
                if (world.TryGetMountedSector(sectorId, out var sector) &&
                    sector.GetNodeOrNull<Node>("Gameplay/Actors") is { } root)
                    Visit(root, null);
            GD.Print($"GRIP8_COMBAT_INVENTORY_END actors={actors} live_projectiles={liveProjectiles} " +
                $"active_hitboxes={activeHitboxes} safety_admission=false");

            void Visit(Node node, Node2D actor)
            {
                if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree() || node.IsQueuedForDeletion()) return;
                if (node is Node2D source && source.GetNodeOrNull<HealthComponent>("Health") is { } health)
                {
                    actor = source;
                    actors++;
                    var binding = source.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
                    AttackTimelineFrame frame = source switch
                    {
                        RustShellSquire rust => rust.CurrentFrame.AttackTimeline,
                        BellBackLaborer bell => bell.CurrentFrame.AttackTimeline,
                        GraveLampMoth moth => moth.CurrentFrame.AttackTimeline,
                        CanyonSkitter skitter => skitter.CurrentFrame.AttackTimeline,
                        BellBoltSentry sentry => sentry.CurrentFrame.AttackTimeline,
                        RangedWeaponEnemy ranged => ranged.CurrentFrame.AttackTimeline,
                        _ => default
                    };
                    GD.Print($"GRIP8_COMBAT_ACTOR physics={Engine.GetPhysicsFrames()} id={source.GetInstanceId()} " +
                        $"path={source.GetPath()} class={source.GetType().Name} scene={source.SceneFilePath} " +
                        $"subtype={binding?.Profile?.SubtypeId ?? "none"} hp={health.CurrentHealth} " +
                        $"position={source.GlobalPosition} process={source.ProcessMode} visible={source.Visible} " +
                        $"attack={frame.AttackId} phase={frame.Phase} remaining={frame.PhaseRemainingSeconds} " +
                        $"thread_available={source.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.IsAvailable} " +
                        "stable_slot_binding=not_observed safe=false");
                }
                bool projectile = node is RangedWeaponProjectile { IsRetired: false } or BellBoltProjectile { IsRetired: false };
                bool activeHit = node is Hitbox { Monitoring: true };
                if (projectile) liveProjectiles++;
                if (activeHit) activeHitboxes++;
                if (projectile || activeHit)
                    GD.Print($"GRIP8_COMBAT_LEAF id={node.GetInstanceId()} path={node.GetPath()} " +
                        $"owner_actor={actor?.GetInstanceId() ?? 0UL} class={node.GetType().Name} " +
                        $"live_projectile={projectile} active_hitbox={activeHit} safe=false");
                for (int index = 0; index < node.GetChildCount(); index++) Visit(node.GetChild(index), actor);
            }
        }
        catch (Exception error)
        {
            // This diagnostic neither authorizes a counter nor changes the route.
            GD.Print($"GRIP8_COMBAT_INVENTORY_FAULT type={error.GetType().Name} message={error.Message} safety_admission=false");
        }
    }
#endif
}
