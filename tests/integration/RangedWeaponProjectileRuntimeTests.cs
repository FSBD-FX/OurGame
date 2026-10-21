using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class RangedWeaponProjectileRuntimeTests
{
    private static readonly Rect2 BroadArea = new(-2000f, -2000f, 4000f, 4000f);

    public static async Task Run(SceneTree tree)
    {
        await ContactDeliversOneRealDamageAndRetires(tree);
        await QueuedProjectileCannotContinueBlastDamage(tree);
        await ThinWorldStopsBeforeHurtbox(tree);
        await CoincidentWorldContactWinsOverHurtbox(tree);
        await EmbeddedLaunchCannotExplodeThroughWorld(tree);
        await SharedVolleyRemainsConsumedAfterReceiverReset(tree);
        await InvulnerableContactRemainsConsumedAfterInvulnerability(tree);
        await OwnerDeathAndExitRetireSynchronouslyOnce(tree);
        await UnlaunchedAndInvalidProjectilesRemainHarmless(tree);
        await PausedAndInvalidDeltasCannotAdvance(tree);
        await LaunchFreezesValuesWithoutKeepingTheResource(tree);
        await EveryTailProjectileKeepsItsOwnWorldTelegraph(tree);
        await MissileGuidanceHonorsRateTimeAndAbsoluteTurnBudget(tree);
        await GuidanceDurationStopsWithTurnBudgetRemaining(tree);
        await SmallerGuidanceRateAndBudgetAreHonored(tree);
        await MissileLosingSightOrTargetKeepsItsLastHeading(tree);
        await MissileAgeAndTravelEachBoundItsLifetime(tree);
        await MissileContactExplodesOnceWithoutDirectDamage(tree);
        await MissileExplosionRespectsRealWorldOcclusion(tree);
        await MissileBoundaryKeepsTheWholeBlastLegal(tree);
        GD.Print("RANGED_PROJECTILE_RUNTIME_OK real_hurtbox=true swept_world=true guidance_bounded=true explosion_once=true");
    }

    // Mutation caught: QueueFree during a real receiver callback is deferred,
    // so checking only IsRetired lets the same revoked leaf hit later receivers.
    private static async Task QueuedProjectileCannotContinueBlastDamage(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileQueuedDuringBlast");
        try
        {
            // Geometry orders genuine Hurtboxes by native instance ID. Create
            // the directly hit receiver first to make cancellation deterministic.
            Target direct = f.AddTarget(new Vector2(100f, 0f));
            Target nearby = f.AddTarget(new Vector2(85f, 25f), 5f);
            await f.Synchronize();
            using var settings = MissileSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 74);
            int directDamages = 0;
            bool cancellationWasDeferred = false;
            direct.Health.Damaged += (_, _) =>
            {
                directDamages++;
                p.QueueFree();
                cancellationWasDeferred = p.IsQueuedForDeletion() && !p.IsRetired;
            };
            p._PhysicsProcess(.5d);
            TestAssert.True(direct.Health.CurrentHealth == 9 && directDamages == 1 && cancellationWasDeferred,
                "the first real blast receiver queues the still-live projectile during its damage callback");
            TestAssert.Equal(10, nearby.Health.CurrentHealth,
                "RANGED_PROJECTILE_QUEUED_BLAST_RED a projectile queued by the first receiver cannot damage a later real receiver");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: omitting actual ledger delivery, doubling damage, leaving
    // contact projectiles active, or having Retire free a node owned by the actor.
    private static async Task ContactDeliversOneRealDamageAndRetires(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileContact");
        try
        {
            Target target = f.AddTarget(new Vector2(100f, 0f));
            await f.Synchronize();
            using var settings = ShotSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 1);
            int retirements = 0, damages = 0;
            p.Retired += _ => retirements++;
            target.Health.Damaged += (_, _) => damages++;
            p._PhysicsProcess(.4d);
            TestAssert.Equal(9, target.Health.CurrentHealth,
                "RANGED_PROJECTILE_CONTACT_RED actual Player-layer Hurtbox receives exactly one projectile damage");
            TestAssert.True(p.IsRetired && damages == 1 && retirements == 1,
                "a real contact retires after one delivery and one retirement notification");
            p._PhysicsProcess(1d); p.Retire(); p.Retire();
            TestAssert.True(target.Health.CurrentHealth == 9 && damages == 1 && retirements == 1 &&
                            GodotObject.IsInstanceValid(p) && !p.IsQueuedForDeletion(),
                "retirement is idempotent and leaves freeing to the owning actor");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: endpoint-only collision or checking targets before the earlier World hit.
    private static async Task ThinWorldStopsBeforeHurtbox(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileThinWall");
        try
        {
            Target target = f.AddTarget(new Vector2(120f, 0f));
            f.AddWall(new Vector2(70f, 0f), new Vector2(3f, 120f));
            await f.Synchronize();
            using var settings = ShotSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 2);
            p._PhysicsProcess(.5d);
            TestAssert.True(p.IsRetired && p.GlobalPosition.X < 65f && p.TravelledDistance < 65f &&
                            target.Health.CurrentHealth == 10,
                "a swept five-pixel projectile cannot tunnel through a three-pixel World wall to real HP");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: equal-time player contact wins over solid cover.
    private static async Task CoincidentWorldContactWinsOverHurtbox(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileWorldTie");
        try
        {
            Target target = f.AddTarget(new Vector2(100f, 0f), 1f);
            f.AddWall(new Vector2(100f, 0f), new Vector2(2f, 100f));
            await f.Synchronize();
            using var settings = ShotSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 3);
            p._PhysicsProcess(.5d);
            TestAssert.True(p.IsRetired && target.Health.CurrentHealth == 10,
                "coincident World and Hurtbox sweep contact resolves as solid World, not damage");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: an overlapping launch bypasses CastMotion and creates an inside-wall blast.
    private static async Task EmbeddedLaunchCannotExplodeThroughWorld(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileEmbedded");
        try
        {
            Target target = f.AddTarget(new Vector2(20f, 0f), 3f);
            f.AddWall(Vector2.Zero, new Vector2(12f, 120f));
            await f.Synchronize();
            using var settings = MissileSettings();
            int explosions = 0;
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 4,
                beforeLaunch: projectile => projectile.ExplosionResolved += (_, _) => explosions++);
            p._PhysicsProcess(.1d);
            TestAssert.True(p.IsRetired && target.Health.CurrentHealth == 10 && p.TravelledDistance == 0f && explosions == 0,
                "a missile starting embedded in real World is revoked without an explosion or travel");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: projectile-local bookkeeping, release on first pellet retirement,
    // or identity collapse that also rejects the next independent burst ordinal.
    private static async Task SharedVolleyRemainsConsumedAfterReceiverReset(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileSharedVolley");
        try
        {
            Target target = f.AddTarget(new Vector2(100f, 0f));
            await f.Synchronize();
            using var settings = ShotSettings();
            RangedWeaponProjectile first = f.Launch(settings, Vector2.Zero, Vector2.Right, 10);
            RangedWeaponProjectile tail = f.Launch(settings, Vector2.Zero, Vector2.Right, 10);
            first._PhysicsProcess(.4d);
            TestAssert.Equal(9, target.Health.CurrentHealth, "the first same-volley pellet really damages HP");
            target.Health.ResetFull();
            tail._PhysicsProcess(.4d);
            TestAssert.True(first.IsRetired && tail.IsRetired && target.Health.CurrentHealth == 10,
                "a surviving same-identity pellet cannot repeat damage after Health cache reset");
            RangedWeaponProjectile nextRound = f.Launch(settings, Vector2.Zero, Vector2.Right, 11);
            nextRound._PhysicsProcess(.4d);
            TestAssert.True(nextRound.IsRetired && target.Health.CurrentHealth == 9,
                "a different burst ordinal remains an independent actual damage attempt");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: only accepted damage consumes an identity, or rejected projectiles fly through.
    private static async Task InvulnerableContactRemainsConsumedAfterInvulnerability(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileRejectedVolley");
        try
        {
            Target target = f.AddTarget(new Vector2(100f, 0f));
            await f.Synchronize();
            target.Health.InvulnerabilitySeconds = 1f;
            TestAssert.True(target.Health.ReceiveDamage(new DamageRequest(1, Vector2.Left, 0f, 900, 901)),
                "the real receiver starts one second of invulnerability");
            using var settings = ShotSettings();
            RangedWeaponProjectile first = f.Launch(settings, Vector2.Zero, Vector2.Right, 12);
            RangedWeaponProjectile tail = f.Launch(settings, Vector2.Zero, Vector2.Right, 12);
            first._PhysicsProcess(.4d);
            TestAssert.True(first.IsRetired && target.Health.CurrentHealth == 9,
                "an invulnerable contact still consumes and retires its physical projectile");
            target.Health._PhysicsProcess(2d);
            tail._PhysicsProcess(.4d);
            TestAssert.True(tail.IsRetired && target.Health.CurrentHealth == 9,
                "the same volley cannot damage after its rejected contact's invulnerability expires");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: owner death only affects future shots, or removal leaves active attack leaves.
    private static async Task OwnerDeathAndExitRetireSynchronouslyOnce(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileOwnerLifetime");
        try
        {
            var ownerHealth = new HealthComponent { Name = "Health", MaxHealth = 2, InvulnerabilitySeconds = 0f };
            f.Owner.AddChild(ownerHealth);
            await f.Synchronize();
            using var settings = ShotSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 20);
            int retirements = 0;
            p.Retired += _ => retirements++;
            ownerHealth.ReceiveDamage(new DamageRequest(2, Vector2.Left, 0f, 800, 801));
            TestAssert.True(p.IsRetired && retirements == 1 && !p.Telegraph.ImpactVisible,
                "actual owning Health death synchronously withdraws the live projectile warning");
            ownerHealth.ResetFull();
            RangedWeaponProjectile tail = f.Launch(settings, Vector2.Zero, Vector2.Right, 21);
            int tailRetirements = 0;
            tail.Retired += _ => tailRetirements++;
            f.Root.RemoveChild(f.Owner);
            TestAssert.True(tail.IsRetired && tailRetirements == 1 && !tail.Telegraph.ImpactVisible,
                "owner exit withdraws its still-live projectile even before another physics frame");
            tail.Retire(); p.Retire();
            TestAssert.True(retirements == 1 && tailRetirements == 1,
                "death followed by exit and explicit cleanup does not notify retirement again");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: constructor/Ready draws or advances, malformed inputs authorize movement/damage.
    private static async Task UnlaunchedAndInvalidProjectilesRemainHarmless(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileInvalidLaunch");
        try
        {
            Target target = f.AddTarget(new Vector2(50f, 0f));
            var unlaunched = new RangedWeaponProjectile();
            f.Owner.AddChild(unlaunched);
            await f.Synchronize();
            unlaunched._PhysicsProcess(1d);
            TestAssert.True(unlaunched.GlobalPosition == Vector2.Zero && unlaunched.TravelledDistance == 0f &&
                            !unlaunched.Visible && (unlaunched.Telegraph == null || !unlaunched.Telegraph.ImpactVisible),
                "an unlaunched node has no movement or visible dangerous geometry");
            using var settings = ShotSettings();
            var launches = new (Vector2 Origin, Vector2 Direction, DamageAttackIdentity Identity, Rect2 Area)[]
            {
                (Vector2.Zero, Vector2.Zero, new(1, 1), BroadArea),
                (new(float.NaN, 0f), Vector2.Right, new(1, 1), BroadArea),
                (Vector2.Zero, new(float.PositiveInfinity, 1f), new(1, 1), BroadArea),
                (Vector2.Zero, Vector2.Right, new(0, 1), BroadArea),
                (Vector2.Zero, Vector2.Right, new(1, 0), BroadArea),
                (Vector2.Zero, Vector2.Right, new(1, 1), new Rect2(0f, 0f, 0f, 100f)),
                (new(3000f, 0f), Vector2.Right, new(1, 1), BroadArea)
            };
            foreach (var launch in launches)
            {
                var p = new RangedWeaponProjectile(); f.Owner.AddChild(p);
                p.Launch(f.Owner, launch.Origin, launch.Direction, launch.Identity, settings, f.Ledger, launch.Area);
                p._PhysicsProcess(1d);
                TestAssert.True(p.IsRetired && p.TravelledDistance == 0f && target.Health.CurrentHealth == 10,
                    "invalid vector, identity, or allowed area cannot authorize a projectile");
            }
            var noLedger = new RangedWeaponProjectile(); f.Owner.AddChild(noLedger);
            noLedger.Launch(f.Owner, Vector2.Zero, Vector2.Right, new(1, 30), settings, null, BroadArea);
            var noOwner = new RangedWeaponProjectile(); f.Owner.AddChild(noOwner);
            noOwner.Launch(null, Vector2.Zero, Vector2.Right, new(1, 31), settings, f.Ledger, BroadArea);
            settings.ProjectileRadius = float.NaN;
            RangedWeaponProjectile badSettings = f.Launch(settings, Vector2.Zero, Vector2.Right, 32);
            TestAssert.True(noLedger.IsRetired && noOwner.IsRetired && badSettings.IsRetired,
                "missing ownership/ledger and nonfinite launch settings are revoked safely");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: manual/Always process advances while paused, nonfinite dt contaminates state.
    private static async Task PausedAndInvalidDeltasCannotAdvance(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileDeltaGate");
        bool wasPaused = tree.Paused;
        try
        {
            await f.Synchronize();
            using var settings = ShotSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 40);
            p._PhysicsProcess(.05d);
            Vector2 before = p.GlobalPosition;
            float travelled = p.TravelledDistance;
            foreach (double dt in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity,
                         double.MaxValue, -1d, 0d }) p._PhysicsProcess(dt);
            tree.Paused = true;
            p._PhysicsProcess(.5d);
            TestAssert.True(p.GlobalPosition == before && p.TravelledDistance == travelled && !p.IsRetired,
                "paused and invalid deltas do not age, move, turn, or retire a valid projectile");
        }
        finally { tree.Paused = wasPaused; await f.Dispose(); }
    }

    // Mutation caught: retaining or duplicating a mutable settings Resource instead of numeric values.
    private static async Task LaunchFreezesValuesWithoutKeepingTheResource(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileFrozenSettings");
        try
        {
            Target target = f.AddTarget(new Vector2(80f, 0f));
            await f.Synchronize();
            var settings = ShotSettings();
            settings.ProjectileSpeed = 200f; settings.MaximumTravel = 100f;
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 41);
            settings.ProjectileSpeed = 1f; settings.ProjectileRadius = 100f;
            settings.MaximumTravel = 1000f; settings.Role = RangedWeaponRole.Missile; settings.Damage = 9;
            settings.Dispose();
            TestAssert.True(p.Telegraph.CurrentFrame.Shape.Extent == new Vector2(5f, 5f) &&
                            p.Telegraph.CurrentFrame.Timeline.Kind == EnemyAttackKind.BeltBurstShot,
                "launch fixes radius and typed role independently of later resource mutation/disposal");
            p._PhysicsProcess(.5d);
            TestAssert.True(p.IsRetired && target.Health.CurrentHealth == 9,
                "the launched projectile still uses its original speed and ordinary damage after resource disposal");
            target.Health.ResetFull();
            using var configured = ShotSettings(); configured.Damage = 2;
            RangedWeaponProjectile stronger = f.Launch(configured, Vector2.Zero, Vector2.Right, 42);
            stronger._PhysicsProcess(.5d);
            TestAssert.True(stronger.IsRetired && target.Health.CurrentHealth == 8,
                "an independently launched valid damage setting is consumed rather than silently ignored");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: actor Active controls all warning leaves, shared visual, or local-space origin drift.
    private static async Task EveryTailProjectileKeepsItsOwnWorldTelegraph(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedProjectileTailWarnings");
        try
        {
            await f.Synchronize();
            using var settings = ShotSettings(); settings.Role = RangedWeaponRole.Scatter;
            settings.ProjectileSpeed = 100f;
            RangedWeaponProjectile a = f.Launch(settings, Vector2.Zero, Vector2.Right, 50);
            RangedWeaponProjectile b = f.Launch(settings, Vector2.Zero, Vector2.Down, 50);
            a._PhysicsProcess(.2d); b._PhysicsProcess(.6d);
            f.Owner.GlobalPosition = new Vector2(400f, 400f);
            TestAssert.True(a.TopLevel && b.TopLevel && a.Telegraph != b.Telegraph &&
                            a.Telegraph.CurrentFrame.Shape.Origin == new Vector2(20f, 0f) &&
                            b.Telegraph.CurrentFrame.Shape.Origin == new Vector2(0f, 60f) &&
                            b.Telegraph.CurrentFrame.Shape.Extent == new Vector2(5f, 5f) &&
                            b.Telegraph.CurrentFrame.Timeline.AttackId == 50 &&
                            b.Telegraph.CurrentFrame.Timeline.Kind == EnemyAttackKind.RivetScatterShot &&
                            a.Telegraph.ImpactVisible && b.Telegraph.ImpactVisible,
                "each live tail owns an Active frame at its true independent world position and radius");
            a.Retire();
            TestAssert.True(!a.Telegraph.ImpactVisible && b.Telegraph.ImpactVisible && !b.IsRetired,
                "retiring one pellet clears only its own warning while the surviving tail remains dangerous");
            b._PhysicsProcess(.1d);
            TestAssert.Near(70f, b.Telegraph.CurrentFrame.Shape.Origin.Y, .001f,
                "a tail warning follows the projectile beyond the owner's short firing phase");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: unlimited homing, per-frame rather than per-second turns, signed budget refund.
    private static async Task MissileGuidanceHonorsRateTimeAndAbsoluteTurnBudget(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileGuidanceBudget");
        try
        {
            var target = new Node2D { Position = new Vector2(1000f, 1000f) }; f.Root.AddChild(target);
            await f.Synchronize();
            using var settings = MissileSettings();
            settings.ProjectileSpeed = 100f; settings.MaximumTravel = 1000f;
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 60, target);
            float previousAngle = 0f, absoluteTurn = 0f;
            for (int i = 0; i < 8; i++)
            {
                // Alternating target signs must consume, never refund, turn budget.
                target.GlobalPosition = new Vector2(1000f, (i & 1) == 0 ? 1000f : -1000f);
                Vector2 before = p.GlobalPosition; p._PhysicsProcess(.05d);
                float angle = (p.GlobalPosition - before).Angle();
                float turn = Mathf.Abs(Mathf.Wrap(angle - previousAngle, -Mathf.Pi, Mathf.Pi));
                TestAssert.True(Mathf.RadToDeg(turn) <= 3.76f,
                    "default missile heading turns at most 75 degrees per second");
                absoluteTurn += Mathf.RadToDeg(turn); previousAngle = angle;
            }
            TestAssert.True(absoluteTurn > 19.9f && absoluteTurn <= 20.02f,
                "alternating missile turns consume a maximum twenty-degree absolute budget");
            Vector2 last = p.GlobalPosition; target.GlobalPosition = new Vector2(-1000f, 1000f);
            p._PhysicsProcess(.2d);
            TestAssert.Near(previousAngle, (p.GlobalPosition - last).Angle(), .0001f,
                "after the four-tenths-second guidance window, a missile flies its final heading");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: guidance keeps reacquiring after its configured window when
    // the angle budget remains. The budget fixture above deliberately exhausts it.
    private static async Task GuidanceDurationStopsWithTurnBudgetRemaining(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileGuidanceTime");
        try
        {
            var target = new Node2D { Position = new Vector2(1000f, 100f) }; f.Root.AddChild(target);
            await f.Synchronize();
            foreach (float duration in new[] { .4f, .2f })
            {
                using var settings = MissileSettings(); settings.ProjectileSpeed = 100f;
                settings.MissileGuidanceSeconds = duration;
                RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right,
                    duration > .3f ? 64UL : 65UL, target);
                Vector2 prior = p.GlobalPosition; p._PhysicsProcess(duration);
                float finalGuidedAngle = (p.GlobalPosition - prior).Angle();
                TestAssert.True(finalGuidedAngle > .08f && finalGuidedAngle < .12f,
                    "the low-angle target consumes less than seven of the available twenty degrees");
                target.GlobalPosition = new Vector2(1000f, -1000f);
                prior = p.GlobalPosition; p._PhysicsProcess(.1d);
                TestAssert.Near(finalGuidedAngle, (p.GlobalPosition - prior).Angle(), .0001f,
                    "default or smaller configured guidance window stops despite remaining turn budget");
                p.Retire(); target.GlobalPosition = new Vector2(1000f, 100f);
            }
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: runtime hardcodes defaults instead of consuming the launch snapshot.
    private static async Task SmallerGuidanceRateAndBudgetAreHonored(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileConfiguredGuidance");
        try
        {
            var target = new Node2D { Position = new Vector2(1000f, 1000f) }; f.Root.AddChild(target);
            await f.Synchronize();
            using var settings = MissileSettings(); settings.ProjectileSpeed = 100f;
            settings.MissileTurnRateDegrees = 20f; settings.MissileMaximumTurnDegrees = 3f;
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 66, target);
            Vector2 prior = p.GlobalPosition; p._PhysicsProcess(.1d);
            TestAssert.Near(2f, Mathf.RadToDeg((p.GlobalPosition - prior).Angle()), .01f,
                "configured twenty-degree-per-second rate permits two degrees in one tenth second");
            prior = p.GlobalPosition; p._PhysicsProcess(.1d);
            TestAssert.Near(3f, Mathf.RadToDeg((p.GlobalPosition - prior).Angle()), .01f,
                "configured three-degree total budget stops a second turn after one remaining degree");
            prior = p.GlobalPosition; p._PhysicsProcess(.1d);
            TestAssert.Near(3f, Mathf.RadToDeg((p.GlobalPosition - prior).Angle()), .01f,
                "exhausted configured budget prevents further guidance");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: homing through World, re-acquiring LOS after lock is lost, dereferencing a freed target.
    private static async Task MissileLosingSightOrTargetKeepsItsLastHeading(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileTargetLoss");
        try
        {
            var target = new Node2D { Position = new Vector2(1000f, 500f) }; f.Root.AddChild(target);
            await f.Synchronize();
            using var settings = MissileSettings(); settings.ProjectileSpeed = 100f;
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 61, target);
            Vector2 prior = p.GlobalPosition; p._PhysicsProcess(.1d);
            float guidedAngle = (p.GlobalPosition - prior).Angle();
            TestAssert.True(guidedAngle > .12f, "a visible offset target first produces real guidance");
            StaticBody2D wall = f.AddWall(new Vector2(300f, 0f), new Vector2(3f, 1800f));
            await f.Synchronize();
            prior = p.GlobalPosition; p._PhysicsProcess(.1d);
            TestAssert.Near(guidedAngle, (p.GlobalPosition - prior).Angle(), .0001f,
                "real World LOS loss permanently stops missile guidance before contact");
            wall.QueueFree(); target.QueueFree(); await f.Synchronize();
            prior = p.GlobalPosition; p._PhysicsProcess(.1d);
            TestAssert.True(!p.IsRetired && Finite(p.GlobalPosition), "an invalid target leaves finite straight flight");
            TestAssert.Near(guidedAngle, (p.GlobalPosition - prior).Angle(), .0001f,
                "target deletion and restored sight cannot re-enable abandoned homing");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: unlimited age when maximum travel is large, or unlimited travel before maximum age.
    private static async Task MissileAgeAndTravelEachBoundItsLifetime(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileDualLifetimeBounds");
        try
        {
            await f.Synchronize();
            using var settings = MissileSettings(); settings.ProjectileSpeed = 10f;
            settings.MaximumTravel = 1000f;
            RangedWeaponProjectile age = f.Launch(settings, Vector2.Zero, Vector2.Right, 62);
            age._PhysicsProcess(2d); TestAssert.True(!age.IsRetired, "a two-second missile remains below the age cap");
            age._PhysicsProcess(1d);
            TestAssert.True(age.IsRetired && age.TravelledDistance <= 30.001f,
                "a default missile cannot outlive three seconds");
            settings.MissileMaximumAgeSeconds = .25f;
            RangedWeaponProjectile shortAge = f.Launch(settings, new Vector2(0f, 100f), Vector2.Right, 67);
            shortAge._PhysicsProcess(1d);
            TestAssert.True(shortAge.IsRetired && MathF.Abs(shortAge.TravelledDistance - 2.5f) < .001f,
                "a smaller configured maximum age limits travel within a large physics delta");
            settings.ProjectileSpeed = 100f; settings.MaximumTravel = 25f; settings.MissileMaximumAgeSeconds = 3f;
            RangedWeaponProjectile range = f.Launch(settings, new Vector2(0f, 200f), Vector2.Right, 63);
            range._PhysicsProcess(1d);
            TestAssert.True(range.IsRetired && MathF.Abs(range.TravelledDistance - 25f) < .001f,
                "maximum travel independently terminates a young missile at its exact range");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: contact delivers direct damage and then blast damage, or retirement re-explodes.
    private static async Task MissileContactExplodesOnceWithoutDirectDamage(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileSingleExplosion");
        try
        {
            Target direct = f.AddTarget(new Vector2(100f, 0f));
            Target nearby = f.AddTarget(new Vector2(85f, 25f), 5f);
            await f.Synchronize();
            using var settings = MissileSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 70);
            int directDamage = 0, nearbyDamage = 0;
            int explosions = 0;
            Vector2 explosionCenter = Vector2.Zero;
            float explosionRadius = 0f;
            bool warningDescribedBlast = false;
            Vector2 nearbyImpulse = Vector2.Zero;
            p.ExplosionResolved += (center, radius) =>
            {
                explosions++; explosionCenter = center; explosionRadius = radius;
                warningDescribedBlast = p.Telegraph.ImpactVisible &&
                    p.Telegraph.CurrentFrame.Shape.Origin == center &&
                    p.Telegraph.CurrentFrame.Shape.Extent == new Vector2(56f, 56f);
            };
            direct.Health.Damaged += (_, _) => directDamage++;
            nearby.Health.Damaged += (_, impulse) => { nearbyDamage++; nearbyImpulse = impulse; };
            p._PhysicsProcess(.5d); p._PhysicsProcess(.5d); p.Retire();
            TestAssert.True(p.IsRetired && direct.Health.CurrentHealth == 9 && nearby.Health.CurrentHealth == 9 &&
                            directDamage == 1 && nearbyDamage == 1,
                "a missile contact delivers one real explosion to contact and nearby targets with no direct-hit double damage");
            TestAssert.True(explosions == 1 && explosionCenter == p.GlobalPosition && explosionRadius == 56f &&
                            warningDescribedBlast,
                "RANGED_PROJECTILE_EXPLOSION_EVENT_RED one legal blast supplies the owner one actual center/radius cosmetic callback");
            // Native CastMotion returns a safe centre (observed x84.375), not
            // necessarily the ideal tangent x85. Check radial alignment with
            // that authoritative centre, not an invented exactly vertical ray.
            Vector2 away = nearby.Actor.GlobalPosition - explosionCenter;
            TestAssert.True(MathF.Abs(Vector2.Right.Cross(away)) > 20f,
                "the nearby real target is genuinely off the missile flight line");
            TestAssert.True(nearbyImpulse.Y > 119f && nearbyImpulse.Dot(away) > 0f &&
                            MathF.Abs(nearbyImpulse.Cross(away)) < .01f &&
                            MathF.Abs(nearbyImpulse.Length() - 120f) < .01f,
                "RANGED_MISSILE_RADIAL_IMPULSE_RED actual nearby Health receives radial blast impulse rather than flight-right impulse" +
                $"; center={explosionCenter}, impulse={nearbyImpulse}");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: blast targets use radius alone, center is placed inside cover, or no blast on World impact.
    private static async Task MissileExplosionRespectsRealWorldOcclusion(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileExplosionLOS");
        try
        {
            Target front = f.AddTarget(new Vector2(80f, 30f), 5f);
            Target behind = f.AddTarget(new Vector2(120f, 0f), 5f);
            f.AddWall(new Vector2(100f, 0f), new Vector2(2f, 150f));
            await f.Synchronize();
            using var settings = MissileSettings();
            RangedWeaponProjectile p = f.Launch(settings, Vector2.Zero, Vector2.Right, 71);
            p._PhysicsProcess(.5d);
            TestAssert.True(p.IsRetired && p.GlobalPosition.X <= 94.001f && front.Health.CurrentHealth == 9 &&
                            behind.Health.CurrentHealth == 10,
                "World impact explodes at the safe center: nearby exposed HP changes, covered HP does not");
        }
        finally { await f.Dispose(); }
    }

    // Mutation caught: only the projectile circle is clipped, an illegal origin explodes,
    // or area-edge travel authorizes a blast outside its encounter envelope.
    private static async Task MissileBoundaryKeepsTheWholeBlastLegal(SceneTree tree)
    {
        var f = new Fixture(tree, "RangedMissileAllowedBlastArea");
        try
        {
            Target legal = f.AddTarget(new Vector2(160f, 120f), 3f);
            Target outside = f.AddTarget(new Vector2(205f, 100f), 2f);
            Target illegalOriginNeighbor = f.AddTarget(new Vector2(40f, 100f), 3f);
            await f.Synchronize();
            using var settings = MissileSettings(); settings.ExplosionRadius = 30f;
            settings.ProjectileSpeed = 200f; settings.MaximumTravel = 400f;
            var area = new Rect2(0f, 0f, 200f, 200f);
            RangedWeaponProjectile p = f.Launch(settings, new Vector2(100f, 100f), Vector2.Right, 72, area: area);
            p._PhysicsProcess(1d);
            TestAssert.True(p.IsRetired && MathF.Abs(p.GlobalPosition.X - 170f) < .01f &&
                            legal.Health.CurrentHealth == 9 && outside.Health.CurrentHealth == 10,
                "boundary explosion uses the final center whose complete thirty-pixel radius stays inside the allowed area");
            RangedWeaponProjectile illegal = f.Launch(settings, new Vector2(5f, 100f), Vector2.Right, 73, area: area);
            illegal._PhysicsProcess(.5d);
            TestAssert.True(illegal.IsRetired && illegal.TravelledDistance == 0f &&
                            illegalOriginNeighbor.Health.CurrentHealth == 10,
                "a missile without a legal complete-radius origin retires without incidental area damage");
        }
        finally { await f.Dispose(); }
    }

    private static RangedWeaponSettings ShotSettings() => new()
    {
        Role = RangedWeaponRole.Burst, ProjectileSpeed = 400f, ProjectileRadius = 5f,
        MaximumTravel = 400f, ActiveSeconds = .42f, BurstEmissionCount = 3
    };

    private static RangedWeaponSettings MissileSettings() => new()
    {
        Role = RangedWeaponRole.Missile, ProjectileSpeed = 400f, ProjectileRadius = 5f,
        MaximumTravel = 500f, ExplosionRadius = 56f
    };

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private sealed record Target(Node2D Actor, HealthComponent Health, Hurtbox Hurtbox);

    // Test fixture owns all created collision Resources. Nodes are released first,
    // then their retained shapes; direct-space-state and tree references are borrowed.
    private sealed class Fixture
    {
        private readonly SceneTree _tree;
        private readonly List<Shape2D> _shapes = new();
        public Node2D Root { get; }
        public Node2D Owner { get; }
        public RangedVolleyDamageLedger Ledger { get; } = new();

        public Fixture(SceneTree tree, string name)
        {
            _tree = tree; Root = new Node2D { Name = name };
            Owner = new Node2D { Name = "ProjectileOwner" };
            Root.AddChild(Owner); tree.Root.AddChild(Root);
        }

        public Target AddTarget(Vector2 position, float radius = 10f)
        {
            var actor = new Node2D { Position = position };
            var health = new HealthComponent { Name = "Health", MaxHealth = 10, InvulnerabilitySeconds = 0f };
            var hurtbox = new Hurtbox
            {
                Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Player, CollisionMask = 0,
                Monitoring = false, Monitorable = true, DamageReceiverPath = new NodePath("../Health")
            };
            var shape = new CircleShape2D { Radius = radius }; _shapes.Add(shape);
            hurtbox.AddChild(new CollisionShape2D { Shape = shape });
            actor.AddChild(health); actor.AddChild(hurtbox); Root.AddChild(actor);
            health.SetPhysicsProcess(false);
            return new Target(actor, health, hurtbox);
        }

        public StaticBody2D AddWall(Vector2 position, Vector2 size)
        {
            var wall = new StaticBody2D
            {
                Position = position, CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
            };
            var shape = new RectangleShape2D { Size = size }; _shapes.Add(shape);
            wall.AddChild(new CollisionShape2D { Shape = shape }); Root.AddChild(wall);
            return wall;
        }

        public RangedWeaponProjectile Launch(RangedWeaponSettings settings, Vector2 origin,
            Vector2 direction, ulong ordinal, Node2D target = null, Rect2? area = null,
            Action<RangedWeaponProjectile> beforeLaunch = null)
        {
            var projectile = new RangedWeaponProjectile(); Owner.AddChild(projectile);
            beforeLaunch?.Invoke(projectile);
            projectile.Launch(Owner, origin, direction, new DamageAttackIdentity(Owner.GetInstanceId(), ordinal),
                settings, Ledger, area ?? BroadArea, target);
            projectile.SetPhysicsProcess(false); // Deterministic calls still use the real native physics space.
            return projectile;
        }

        public async Task Synchronize()
        {
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
        }

        public async Task Dispose()
        {
            if (GodotObject.IsInstanceValid(Owner) && Owner.GetParent() == null) Owner.Free();
            if (GodotObject.IsInstanceValid(Root)) Root.Free();
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            foreach (Shape2D shape in _shapes) shape.Dispose();
            Ledger.Clear();
        }
    }
}
