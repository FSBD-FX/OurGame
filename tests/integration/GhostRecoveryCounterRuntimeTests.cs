using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue.Residents;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

// Saved actor/input feasibility only. This translated single-enemy world is NOT Grip8 or the full route.
internal static class GhostRecoveryCounterRuntimeTests
{
    internal static async Task Run(SceneTree tree)
    {
        bool rejected = false;
        try { await RunTrial(tree, forceNestedBudgetFault: true); }
        catch (CounterPhysicsBudgetExceeded error)
        {
            TestAssert.True(error.Elapsed == error.Limit && error.Limit < 600 && error.Stage == "J-recovery-tail",
                "controlled nested J wait must be rejected at its actual physics deadline before adding a frame");
            rejected = true;
            GD.Print($"GHOST_RECOVERY_COUNTER_BUDGET_OK actual_elapsed={error.Elapsed} limit={error.Limit} " +
                "rejected_before_extra_wait=true nested_J_tail=true");
        }
        TestAssert.True(rejected,
            "GHOST_COUNTER_NESTED_BUDGET_RED a real nested J tail cannot complete after its shared physics deadline");
        await RunTrial(tree, forceNestedBudgetFault: false);
    }

    private sealed class CounterPhysicsBudgetExceeded(ulong elapsed, ulong limit, string stage) : Exception
    {
        public ulong Elapsed { get; } = elapsed;
        public ulong Limit { get; } = limit;
        public string Stage { get; } = stage;
    }

    private static async Task RunTrial(SceneTree tree, bool forceNestedBudgetFault)
    {
        var fixture = new ResidentPatrolRuntimeTests.Fixture(tree);
        KeyboardRouteInputDriver driver = null;
        PlayerCombat combat = null;
        HealthComponent enemyHealth = null;
        PlayerCombat.AttackStartedEventHandler onStarted = null;
        PlayerCombat.AttackConnectedEventHandler onConnected = null;
        HealthComponent.DamagedEventHandler onDamaged = null;
        Hitbox.ConnectedEventHandler onIncoming = null;
        Exception primary = null;
        try
        {
            fixture.Initialize(true,GhostLandingGuardRuntimeTests.ScenePath);
            ResidentPopulationDefinition original = ResidentAuthoredLayout.Load()
                .Single(x => x.Definition.StableId == "canyon.v2.resident.split-level.02").Definition;
            Vector2 offset = new Vector2(320,96) - original.Placement.WorldSpawn;
            fixture.Actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile.ConfigureActor(fixture.Actor,
                CanyonResidentCatalog.Subtypes.Single(x => x.Id == original.SubtypeId),original with
                {
                    Placement = new ResidentPlacementCandidate(new Vector2(320,96),
                        new Rect2(original.Placement.Territory.Position+offset,original.Placement.Territory.Size),
                        original.Placement.PatrolPoints.Select(p => p+offset))
                });
            fixture.SavedPlayer(280);
            driver = KeyboardRouteInputDriver.Begin(fixture.Target);
            await fixture.Mount(false);
            if (!forceNestedBudgetFault)
            {
                // This second saved resident is configured BEFORE mounting.
                // Its captured territory is disjoint from the player's complete
                // combat envelope; no phase, pose or Health is rewritten live.
                var far = GD.Load<PackedScene>(GhostLandingGuardRuntimeTests.ScenePath).Instantiate<RustShellSquire>();
                fixture.Floor(7800,1040);
                Vector2 farOffset = new Vector2(8320,96) - original.Placement.WorldSpawn;
                far.GetNode<ResidentSceneBinding>("ResidentBinding").Profile.ConfigureActor(far,
                    CanyonResidentCatalog.Subtypes.Single(x => x.Id == original.SubtypeId), original with
                    {
                        StableId = original.StableId + ".foreign-control",
                        Placement = new ResidentPlacementCandidate(new Vector2(8320,96),
                            new Rect2(original.Placement.Territory.Position+farOffset,original.Placement.Territory.Size),
                            original.Placement.PatrolPoints.Select(p => p+farOffset))
                    });
                far.Position = new Vector2(8320,96);
                fixture.Scope.AddChild(far);
                TestForeignWindow(fixture,far);
            }
            CharacterBody2D player = fixture.Target;
            RustShellSquire actor = fixture.Actor;
            enemyHealth = fixture.Health;
            combat = player.GetNode<PlayerCombat>("Combat");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var input = player.GetNode<PlayerInputReader>("Input");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var actorHurt = actor.GetNode<Hurtbox>("Hurtbox");
            var incoming = actor.GetNode<Hitbox>("AttackHitbox");
            ulong actorId = actor.GetInstanceId(), hurtId = actorHurt.GetInstanceId();
            int playerHp = fixture.PlayerHealth.CurrentHealth, lastHp = enemyHealth.CurrentHealth;
            int starts = 0, connections = 0, damageEvents = 0, guardedContacts = 0, hits = 0;
            int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
            ulong issueSequence = 0;
            bool rawJAtStart = false, checkedControls = false, checkedShield = false;
            onStarted = (_,_) =>
            {
                starts++;
                rawJAtStart = Input.IsPhysicalKeyPressed(Key.J) && input.CurrentFrameSequence > issueSequence;
            };
            onConnected = victim =>
            {
                if (ReferenceEquals(victim,actorHurt) || GodotObject.IsInstanceValid(victim) && victim.GetInstanceId() == hurtId) connections++;
            };
            onDamaged = (hp,_) => { lastHp = hp; damageEvents++; };
            onIncoming = victim => { if (victim == player.GetNode<Hurtbox>("Hurtbox") && shield.IsActive) guardedContacts++; };
            combat.AttackStarted += onStarted;
            combat.AttackConnected += onConnected;
            enemyHealth.Damaged += onDamaged;
            incoming.Connected += onIncoming;

            bool Counter(ulong expectedActor,ulong expectedAttack,bool foreign)
            {
                MethodInfo method = typeof(GhostLandingGuardRuntimeTests).GetMethod("CanCounter",BindingFlags.Static|BindingFlags.NonPublic);
                TestAssert.True(method != null,"GHOST_RECOVERY_COUNTER_POLICY_RED actual Recovery needs explicit same-source counter admission");
                return (bool)method.Invoke(null,[actor,player,expectedActor,expectedAttack,foreign]);
            }
            ulong began = Engine.GetPhysicsFrames();
            ulong physicsBudget = 600;
            async Task Step(string stage = "counter-loop")
            {
                ulong elapsed = Engine.GetPhysicsFrames() - began;
                if (elapsed >= physicsBudget) throw new CounterPhysicsBudgetExceeded(elapsed, physicsBudget, stage);
                await fixture.Frames(1);
                driver.ObserveFrame();
                elapsed = Engine.GetPhysicsFrames() - began;
                if (elapsed > physicsBudget) throw new CounterPhysicsBudgetExceeded(elapsed, physicsBudget, stage);
            }
            bool hasCounterApi = typeof(GhostLandingGuardRuntimeTests).GetMethod("CanCounter",BindingFlags.Static|BindingFlags.NonPublic) != null;
            bool checkedHidden = false, checkedOtherPhase = false;
            for (int frame = 0; frame < 600 && lastHp > 0; frame++)
            {
                TestAssert.True(fixture.PlayerHealth.CurrentHealth == playerHp && fixture.RecoveryRequests == 0 &&
                    motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes && driver.ClawEdges == 0,
                    "counter feasibility cannot spend HP, recover, jump, dash or add a K shortcut");
                AttackTimelineFrame timeline = actor.CurrentFrame.AttackTimeline;
                if (hasCounterApi && timeline.Phase != AttackPhase.Recovery)
                {
                    TestAssert.True(!Counter(actorId,timeline.AttackId,false),"hidden/Telegraph/Active/None cannot authorize a Recovery counter");
                    checkedHidden |= !actor.GetNode<Knight.Abilities.ThreadTarget2D>("ThreadTarget").IsAvailable;
                    checkedOtherPhase |= timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active;
                }
                bool guard = GhostLandingGuardRuntimeTests.TrySample(actor,player,out Vector2 point,out _) &&
                    (timeline.Phase == AttackPhase.Active || timeline.Phase == AttackPhase.Telegraph && timeline.PhaseRemainingSeconds <= 8f/60f) && shield.CanBeginGuard;
                if (guard)
                {
                    Vector2 direction = BasinCounterweightRuntimeTests.StationaryGuardDirection(
                        player.GetNode<Hurtbox>("Hurtbox").GlobalPosition,point,[point],true);
                    driver.PointerButton(MouseButton.Right,true,player,player.GlobalPosition+direction*96);
                }
                else driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
                if (timeline.Phase != AttackPhase.Recovery)
                { await Step(); continue; }
                if (shield.IsActive)
                {
                    TestAssert.True(!Counter(actorId,timeline.AttackId,false),"actual Recovery while shield remains active cannot start counter");
                    checkedShield = true;
                    await Step(); continue;
                }
                if (!Counter(actorId,timeline.AttackId,false))
                { await Step(); continue; }
                if (!checkedControls)
                {
                    TestAssert.True(!Counter(actorId+1,timeline.AttackId,false) &&
                        !Counter(actorId,timeline.AttackId+1,false) && !Counter(actorId,timeline.AttackId,true),
                        "another actor/attack or foreign danger cannot lend Recovery permission");
                    checkedControls = true;
                }
                // Real horizontal face sample, then release. No pose/velocity writes.
                bool left = actor.GlobalPosition.X < player.GlobalPosition.X;
                driver.Command(InputActions.MoveUp,false); driver.Command(InputActions.MoveDown,false);
                driver.Command(InputActions.MoveLeft,left); driver.Command(InputActions.MoveRight,!left);
                await Step("face-input");
                driver.Command(InputActions.MoveLeft,false); driver.Command(InputActions.MoveRight,false);
                await Step("face-release");
                timeline = actor.CurrentFrame.AttackTimeline;
                if (!Counter(actorId,timeline.AttackId,false)) continue;
                int beforeHp=lastHp, beforeStarts=starts, beforeConnections=connections, beforeDamage=damageEvents;
                issueSequence=input.CurrentFrameSequence;
                float energyBefore=energy.EnergyRatio;
                // Controlled negative only: put the deadline inside the upcoming
                // real J tail. No actor, clock, health or energy is rewritten.
                if (forceNestedBudgetFault) physicsBudget = Math.Min(physicsBudget, Engine.GetPhysicsFrames() - began + 2UL);
                driver.Command(InputActions.Attack,true);
                await Step("J-start");
                driver.Command(InputActions.Attack,false);
                for(int wait=0;wait<40 && (starts==beforeStarts || combat.IsAttacking);wait++)
                { await Step("J-recovery-tail"); }
                GD.Print($"GHOST_RECOVERY_COUNTER_SAMPLE hit={hits+1} attack={timeline.AttackId} hp={beforeHp}->{lastHp} starts={starts-beforeStarts} connected={connections-beforeConnections} damage={damageEvents-beforeDamage} raw_J={rawJAtStart} player={player.GlobalPosition} energy={energyBefore}->{energy.EnergyRatio}");
                TestAssert.True(starts==beforeStarts+1 && connections==beforeConnections+1 && damageEvents==beforeDamage+1 &&
                    lastHp==beforeHp-1 && rawJAtStart && fixture.PlayerHealth.CurrentHealth==playerHp,
                    "one fresh real J must connect to exact saved Hurtbox and reduce real target Health without player damage");
                hits++;
            }
            TestAssert.True(Engine.GetPhysicsFrames()-began<=physicsBudget && physicsBudget==600 &&
                lastHp==0 && hits==3 && starts==3 && connections==3 && damageEvents==3 && guardedContacts>0 &&
                checkedControls && checkedShield && checkedHidden && checkedOtherPhase && player.IsOnFloor() && driver.AttackEdges==3 && driver.ClawEdges==0,
                "saved Ghost clearance needs three actual independent J hits/death, native guarded contact and explicit admission negatives");
            GD.Print($"GHOST_RECOVERY_COUNTER_OK actual_hits=3 target_hp=3->0 player_hp={playerHp}->{fixture.PlayerHealth.CurrentHealth} physics={Engine.GetPhysicsFrames()-began}/600 raw_J=true raw_L=true extra_K=0 fixture_only=true whole_route=false human=false");
        }
        catch(Exception error) { primary=error; throw; }
        finally
        {
            Exception cleanup=null;
            try { if(GodotObject.IsInstanceValid(combat)) { if(onStarted!=null) combat.AttackStarted-=onStarted; if(onConnected!=null) combat.AttackConnected-=onConnected; } }
            catch(Exception error) { cleanup=error; }
            try { if(GodotObject.IsInstanceValid(enemyHealth) && onDamaged!=null) enemyHealth.Damaged-=onDamaged; }
            catch(Exception error) { cleanup??=error; }
            try { if(GodotObject.IsInstanceValid(fixture.Actor) && onIncoming!=null) fixture.Actor.GetNode<Hitbox>("AttackHitbox").Connected-=onIncoming; }
            catch(Exception error) { cleanup??=error; }
            try { driver?.Dispose(); } catch(Exception error) { cleanup??=error; }
            try { await fixture.DisposeAsync(); } catch(Exception error) { cleanup??=error; }
            if(cleanup!=null) { if(primary==null) throw cleanup; GD.PushError("Ghost counter cleanup after primary: "+cleanup); }
        }
    }

    private static void TestForeignWindow(ResidentPatrolRuntimeTests.Fixture fixture,RustShellSquire far)
    {
        MethodInfo method = typeof(PrologueCanyonGeneratedMainRouteTests).GetMethod("ReadGhostForeignWindow",
            BindingFlags.Static|BindingFlags.NonPublic);
        TestAssert.True(method != null,
            "GHOST_FOREIGN_WINDOW_RED real mounted actors need fail-closed foreign-source admission before route J");
        Rect2 body = fixture.Target.GetNode<CollisionShape2D>("BodyCollider").GlobalTransform *
            fixture.Target.GetNode<CollisionShape2D>("BodyCollider").Shape.GetRect();
        Rect2 envelope = body.Grow(8);
        bool Danger()
        {
            object result = method.Invoke(null, [new Node[]{fixture.Scope},fixture.Target,fixture.Actor,envelope,.44f]);
            if ((bool)result.GetType().GetProperty("Danger").GetValue(result))
                GD.Print($"GHOST_FOREIGN_REJECTION_CONTROL reason={result.GetType().GetProperty("Reason").GetValue(result)} " +
                    $"blocker={result.GetType().GetProperty("BlockingActor").GetValue(result)}");
            return (bool)result.GetType().GetProperty("Danger").GetValue(result);
        }
        object premise=method.Invoke(null,[new Node[]{fixture.Scope},fixture.Target,fixture.Actor,envelope,.44f]);
        int excluded=(int)premise.GetType().GetProperty("ExcludedResidents").GetValue(premise);
        TestAssert.True(!Danger() && excluded==1,
            "GHOST_DISJOINT_PREMISE_RED the real captured territory must actually be disjoint, not merely the actor far away");
        GD.Print($"GHOST_FOREIGN_DISJOINT_PREMISE excluded={excluded} far={far.GlobalPosition} envelope={envelope}");
        var unknown = new Node2D { Name="UnknownForeignSource" };
        unknown.AddChild(new HealthComponent { Name="Health" });
        fixture.Scope.AddChild(unknown);
        try { TestAssert.True(Danger(), "unknown living foreign actor rejects a counter even with no current leaf"); }
        finally { unknown.Free(); }
        var projectile = new RangedWeaponProjectile { Name="UnregisteredForeignLeaf" };
        fixture.Scope.AddChild(projectile);
        try { TestAssert.True(Danger(), "unregistered non-retired projectile cannot be forgiven by its owner's territory"); }
        finally { projectile.Free(); }
        var unownedHit = new Hitbox { Name="UnownedActiveHitbox" };
        fixture.Scope.AddChild(unownedHit);
        unownedHit.Begin(9001);
        try { TestAssert.True(Danger(), "unowned active Hitbox cannot lend a false safe counter window"); }
        finally { unownedHit.End(); unownedHit.Free(); }
        var escapedHit = new Hitbox { Name="UnknownHitboxUnderExcludedResident" };
        far.AddChild(escapedHit);
        escapedHit.Begin(9002);
        try { TestAssert.True(Danger(), "territory only excuses the actor's exact bound hitbox, never arbitrary descendants"); }
        finally { escapedHit.End(); escapedHit.Free(); }
        // Controlled identity negatives: a node at the old path does NOT
        // inherit the production actor's cached damage/publication binding.
        bool replacementHitRejected=false,replacementLeafRejected=false;
        var boundHit=far.GetNode<Hitbox>("AttackHitbox");
        boundHit.Name="OriginalBoundHitbox";
        var replacementHit=new Hitbox {Name="AttackHitbox"};
        far.AddChild(replacementHit); replacementHit.Begin(9003);
        try
        {
            GD.Print($"GHOST_REPLACEMENT_HIT_BINDING path_id={far.GetNodeOrNull<Hitbox>("AttackHitbox")?.GetInstanceId()} " +
                $"original_id={boundHit.GetInstanceId()} replacement_id={replacementHit.GetInstanceId()} " +
                $"original_name={boundHit.Name} replacement_name={replacementHit.Name}");
            replacementHitRejected=Danger();
        }
        finally { replacementHit.End(); replacementHit.Free(); boundHit.Name="AttackHitbox"; }
        var visualRoot=far.GetNode<Node2D>("VisualRoot");
        var boundLeaf=visualRoot.GetNode<AttackTelegraphVisual>("AttackTelegraph");
        boundLeaf.Name="OriginalBoundTelegraph";
        var replacementLeaf=new AttackTelegraphVisual {Name="AttackTelegraph"};
        visualRoot.AddChild(replacementLeaf);
        try
        {
            replacementLeaf.Present(new AttackPresentationFrame(
                new AttackTimelineFrame(9004,EnemyAttackKind.RustShellThrust,AttackPhase.Active,1,1,fixture.Target.GlobalPosition,true),
                new AttackShapeFrame(AttackShapeKind.Rectangle,fixture.Target.GlobalPosition,new Vector2(40,40),0,Array.Empty<Vector2>())));
            GD.Print($"GHOST_REPLACEMENT_LEAF_BINDING path_id={far.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph")?.GetInstanceId()} " +
                $"original_id={boundLeaf.GetInstanceId()} replacement_id={replacementLeaf.GetInstanceId()} " +
                $"original_name={boundLeaf.Name} replacement_name={replacementLeaf.Name}");
            replacementLeafRejected=Danger();
        }
        finally { replacementLeaf.Free(); boundLeaf.Name="AttackTelegraph"; }
        GD.Print($"GHOST_BOUND_LEAF_IDENTITY_SAMPLE replacement_hit_rejected={replacementHitRejected} replacement_publication_rejected={replacementLeafRejected}");
        TestAssert.True(replacementHitRejected && replacementLeafRejected,
            "GHOST_BOUND_LEAF_IDENTITY_RED same-path replacements cannot inherit captured territory permission");
        TestAssert.True(!Danger(), "foreign rejection controls leave no false admission or retained unknown leaf");
        GD.Print("GHOST_FOREIGN_WINDOW_CONTROLS_OK captured_disjoint_resident=true unknown_actor_rejected=true unregistered_leaf_rejected=true unowned_hitbox_rejected=true launched_flight_proved=false pose_hp_clock_writes=0 structural_negative_controls=true");
    }
}
