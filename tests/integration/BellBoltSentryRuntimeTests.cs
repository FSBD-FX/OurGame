using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class BellBoltSentryRuntimeTests
{
    // A legitimate old-policy kill before any real bolt retirement must never
    // transfer ownership to the still-unarmed recovery helper.
    public static async Task RunSteamRecoveryPreArmBaseline(SceneTree tree)
    {
        var root = new Node2D { Name = "SteamSentryPreArmBaseline" };
        var resources = new HashSet<Resource>();
        CharacterBody2D player = null;
        BellBoltSentry sentry = null;
        PlayerCombat combat = null;
        HealthComponent health = null, enemyHealth = null;
        KeyboardRouteInputDriver driver = null;
        SteamSentryRecoveryDriver recovery = null;
        var attacks = new List<(ulong Id, ulong Frame, bool PhysicalJ, int Direction)>();
        var connections = new List<(ulong AttackId, ulong HurtboxId, int Hp)>();
        var hp = new List<int>();
        int enemyHp = 2, defeated = 0, damaged = 0, died = 0, healed = 0, postDefeatChecks = 0;
        bool typedWarning = false, observedBolt = false, loggedDefeat = false;
        ulong deadline = 0;
        void OnAttack(ulong id, int direction) => attacks.Add((id, Engine.GetPhysicsFrames(), Input.IsPhysicalKeyPressed(Key.J), direction));
        void OnConnected(Hurtbox target) => connections.Add((attacks[^1].Id, target.GetInstanceId(), enemyHp));
        void OnEnemyDamaged(int remaining, Vector2 impulse) { enemyHp = remaining; hp.Add(remaining); }
        void OnDefeated() { defeated++; if (defeated == 1) sentry.QueueFree(); }
        void OnWarning(AttackPresentationFrame frame)
        {
            if (frame.Timeline.Kind == EnemyAttackKind.BellBoltShot && frame.Timeline.Phase == AttackPhase.Telegraph)
                typedWarning = true;
            if (frame.Timeline.Kind == EnemyAttackKind.BellBoltShot && frame.Timeline.Phase == AttackPhase.Active)
                observedBolt = true;
        }
        void OnDamaged(int remaining, Vector2 impulse) => damaged++;
        void OnDied() => died++;
        void OnHealed(int restored, int remaining) => healed++;
        void CollectOwnedShapes(Node node)
        {
            if (node is CollisionShape2D collider && collider.Shape != null) resources.Add(collider.Shape);
            Godot.Collections.Array<Node> children = node.GetChildren();
            using Godot.Collections.Array backing = (Godot.Collections.Array)children;
            foreach (Node child in children) CollectOwnedShapes(child);
        }
        try
        {
            PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn", cacheMode: ResourceLoader.CacheMode.Ignore);
            resources.Add(playerScene);
            PackedScene sentryScene = ResourceLoader.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn", cacheMode: ResourceLoader.CacheMode.Ignore);
            resources.Add(sentryScene);
            player = playerScene.Instantiate<CharacterBody2D>();
            player.Position = new Vector2(362f, 293f);
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.AddToGroup("player");
            sentry = sentryScene.Instantiate<BellBoltSentry>();
            sentry.Position = new Vector2(300f, 300f);
            var motor = player.GetNode<PlayerMotor>("Motor");
            var input = player.GetNode<PlayerInputReader>("Input");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            combat = player.GetNode<PlayerCombat>("Combat");
            health = player.GetNode<HealthComponent>("Health");
            enemyHealth = sentry.GetNode<HealthComponent>("Health");
            ulong playerId = player.GetInstanceId(), hurtboxId = sentry.GetNode<Hurtbox>("Hurtbox").GetInstanceId();
            resources.Add(motor.Tuning); resources.Add(energy.Tuning); resources.Add(sentry.Settings);
            resources.Add(player.GetNode<PlayerPresentationCoordinator>("Presentation").Profile);
            var floor = new StaticBody2D { Name = "Floor", Position = new Vector2(580f, 339f),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
            floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1100f, 20f) } });
            root.AddChild(floor); root.AddChild(player); root.AddChild(sentry);
            CollectOwnedShapes(root);
            combat.AttackStarted += OnAttack; combat.AttackConnected += OnConnected;
            enemyHealth.Damaged += OnEnemyDamaged; sentry.Defeated += OnDefeated; sentry.AttackFrameChanged += OnWarning;
            health.Damaged += OnDamaged; health.Died += OnDied; health.Healed += OnHealed;
            int hz = Engine.PhysicsTicksPerSecond;
            float fullJ = combat.Tuning.AttackStartupSeconds + combat.Tuning.AttackActiveSeconds + combat.Tuning.AttackRecoverySeconds;
            ulong entry = Engine.GetPhysicsFrames();
            deadline = entry + (ulong)Math.Ceiling(2f * fullJ * hz) + 26UL + 6UL + 8UL;
            tree.Root.AddChild(root);
            driver = KeyboardRouteInputDriver.Begin(player);
            await Frames(tree, 1); // Let native _Ready initialize saved health before constructing the observer.
            recovery = new SteamSentryRecoveryDriver(player, sentry, driver);
            ulong lastSequence = 0;
            string Snapshot() => $"physics={Engine.GetPhysicsFrames()}/{deadline} sample={input.CurrentFrameSequence} " +
                $"player={player.GlobalPosition} velocity={player.Velocity} J={attacks.Count} hp={health.CurrentHealth} " +
                $"enemy_hp={enemyHp} defeated={defeated} helper={recovery.Stage}/{recovery.CastCommands}/{recovery.AttackCommands} " +
                $"post_defeat={postDefeatChecks}";
            void Check(bool condition, string claim) => TestAssert.True(condition, claim + "; " + Snapshot());
            bool Supported()
            {
                if (!player.IsOnFloor() || MathF.Abs(player.GlobalPosition.Y + 36f - 329f) > .2f) return false;
                for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                    if (player.GetSlideCollision(index).GetCollider() == floor) return true;
                return false;
            }
            async Task Tick(bool support = true)
            {
                int beforeK = driver.ClawEdges, beforeJ = driver.AttackEdges, beforeDowns = driver.KeyDownCount;
                if (defeated == 0)
                    Check(!recovery.TryDrive(false, checked((int)(deadline - Engine.GetPhysicsFrames()))),
                        "unarmed helper must decline each actual old-policy tick without a retirement claim");
                else
                {
                    Check(!recovery.TryFinishRetiredCounter(),
                        "STEAM_SENTRY_PREARM_RETIREMENT_RED legitimate prearm defeat cannot grant recovery-tail ownership");
                    postDefeatChecks++;
                }
                Check(recovery.CastCommands == 0 && recovery.AttackCommands == 0 && recovery.Applications == 0 && !recovery.Completed &&
                    driver.ClawEdges == beforeK && driver.AttackEdges == beforeJ && driver.KeyDownCount == beforeDowns,
                    "unarmed helper emits no physical commands and owns no baseline J");
                await Frames(tree, 1);
                Check(Engine.GetPhysicsFrames() <= deadline && (driver.ObservedFrames == 0 || input.CurrentFrameSequence > lastSequence),
                    "prearm baseline keeps its frozen two-J/spacing/six-sample bound and fresh native input");
                lastSequence = input.CurrentFrameSequence;
                driver.ObserveFrame();
                if (GodotObject.IsInstanceValid(sentry))
                {
                    Godot.Collections.Array<Node> children = sentry.GetChildren();
                    using Godot.Collections.Array backing = (Godot.Collections.Array)children;
                    observedBolt |= children.Any(child => child is BellBoltProjectile);
                }
                Check(player.GetInstanceId() == playerId && player.SceneFilePath == "res://scenes/player/Player.tscn" &&
                    health.MaxHealth == 5 && health.CurrentHealth == 5 && damaged == 0 && died == 0 && healed == 0 &&
                    motor.IsPhysicsProcessing() && input.IsPhysicsProcessing() && combat.IsPhysicsProcessing() &&
                    energy.IsPhysicsProcessing() && shield.IsPhysicsProcessing() && claw.IsPhysicsProcessing() &&
                    motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0 && driver.ClawEdges == 0 &&
                    driver.ShieldEdges == 0 && driver.ObservedMouseMotionSamples == 0 && !observedBolt && (!support || Supported()),
                    "ordinary HP5 prearm baseline retains real motor/input/energy/shield/claw, native floor and no incoming bolt/rescue");
                if (defeated > 0 && !loggedDefeat)
                {
                    loggedDefeat = true;
                    GD.Print("STEAM_SENTRY_PREARM_ACTUAL_DEFEAT actual_J=2 immediate_QueueFree=true real_retirement_claim=false " + Snapshot());
                }
            }
            await Tick(support: false); await Tick();
            Check(enemyHealth.CurrentHealth == 2 && enemyHealth.MaxHealth == 2 &&
                sentry.SceneFilePath == "res://scenes/enemies/BellBoltSentry.tscn" && sentry.IsPhysicsProcessing(),
                "saved Sentry starts with actual two HP and its real AI running before baseline J");
            driver.Command(InputActions.MoveLeft, true);
            await Tick();
            driver.Command(InputActions.MoveLeft, false);
            do { await Tick(); } while (MathF.Abs(player.Velocity.X) > 2f || input.Current.MoveAxis != 0f);
            Check(input.KeyboardAimDirection.X < -.99f && !Input.IsPhysicalKeyPressed(Key.A) &&
                player.GlobalPosition.X - 300f is >= 55f and <= 64f,
                "one real A approach followed by neutral braking establishes the ordinary left J stance");
            ulong firstJ = 0;
            for (int attack = 0; attack < 2; attack++)
            {
                if (attack == 0) firstJ = Engine.GetPhysicsFrames();
                else while (Engine.GetPhysicsFrames() - firstJ < 26UL) await Tick();
                int before = attacks.Count;
                driver.Command(InputActions.PrimaryAction, true);
                do { await Tick(); } while (attacks.Count == before);
                driver.Command(InputActions.PrimaryAction, false);
                Check(attacks.Count == before + 1 && attacks[^1].PhysicalJ &&
                    attacks[^1].Direction == (int)AttackDirection.Horizontal && combat.LastAttackFacing == -1,
                    "old-policy J is one accepted fresh physical left-facing attack, not a helper counter");
                while (combat.IsAttacking) await Tick();
            }
            for (int sample = 0; sample < 6; sample++) await Tick();
            Check(typedWarning && !observedBolt && hp.SequenceEqual(new[] { 1, 0 }) && defeated == 1 &&
                attacks.Count == 2 && attacks[0].Id != attacks[1].Id && attacks[1].Frame - attacks[0].Frame >= 26UL &&
                connections.Count == 2 && connections.All(connection => connection.HurtboxId == hurtboxId) &&
                connections.Select(connection => connection.AttackId).SequenceEqual(attacks.Select(attack => attack.Id)) &&
                connections.Select(connection => connection.Hp).SequenceEqual(new[] { 1, 0 }) &&
                postDefeatChecks >= 6 && !GodotObject.IsInstanceValid(sentry) && !GodotObject.IsInstanceValid(enemyHealth) &&
                !combat.IsAttacking && !Input.IsPhysicalKeyPressed(Key.J),
                "prearm native J2 defeats/removes the saved Sentry while six fresh post-J samples retain zero helper ownership");
            GD.Print("STEAM_SENTRY_PREARM_BASELINE_OK actual_J=2 hp=5 enemy_hp=2,1,0 defeated=1 helper_ownership=0 real_retirement_claim=false " + Snapshot());
        }
        finally
        {
            if (combat != null) { combat.AttackStarted -= OnAttack; combat.AttackConnected -= OnConnected; }
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; health.Healed -= OnHealed; }
            if (GodotObject.IsInstanceValid(enemyHealth)) enemyHealth.Damaged -= OnEnemyDamaged;
            if (GodotObject.IsInstanceValid(sentry)) { sentry.Defeated -= OnDefeated; sentry.AttackFrameChanged -= OnWarning; }
            try { recovery?.Dispose(); }
            finally
            {
                try { driver?.Dispose(); }
                finally
                {
                    if (GodotObject.IsInstanceValid(sentry) && sentry.GetParent() == null) sentry.Free();
                    if (player != null && player.GetParent() == null) player.Free();
                    if (root.IsInsideTree()) { root.QueueFree(); await Frames(tree, 3); } else root.Free();
                    foreach (Resource resource in resources) resource.Dispose();
                }
            }
        }
    }

    // Controlled saved-scene/native-keyboard proof only, not an encounter batch,
    // the normal route's HP1 state, or whole-game completion.
    public static async Task RunSteamRecoveryKeyboard(SceneTree tree, bool retireOnDefeat = false,
        bool exerciseFallbackApproach = false)
    {
        const int preparationAttacks = 7;
        var root = new Node2D { Name = "SteamSentryRecoveryKeyboard" };
        var resources = new HashSet<Resource>();
        CharacterBody2D player = null;
        BellBoltSentry sentry = null;
        PlayerCombat combat = null;
        PlayerShieldController shield = null;
        PlayerEnergyController energy = null;
        PlayerInputReader input = null;
        MechanicalClawController claw = null;
        LegacyThreadInteractionAdapter adapter = null;
        HealthComponent health = null, enemyHealth = null;
        KeyboardRouteInputDriver driver = null;
        SteamSentryRecoveryDriver recovery = null;
        var bolts = new Dictionary<ulong, BellBoltProjectile>();
        var warnings = new List<(ulong Id, ulong Frame)>();
        var born = new List<(ulong BoltId, ulong AttackId, ulong Frame, EnemyAttackKind Kind)>();
        var retired = new List<(ulong BoltId, ulong Frame, float Travel)>();
        var guarded = new List<(ulong BoltId, ulong Frame, bool Guarded, int Damage, int Hp, Vector2 Direction, Vector2 Source)>();
        var attacks = new List<(ulong Id, ulong Frame, bool PhysicalJ, int Direction)>();
        var connections = new List<(ulong AttackId, ulong HurtboxId, int Remaining)>();
        var casts = new List<(ulong Id, ulong Frame, int Applications)>();
        var clawFrames = new List<(ulong Id, MechanicalClawState State, ulong TargetId, ulong Frame, bool SampledK, bool PhysicalK)>();
        var stuns = new List<(ulong CastId, ulong Frame, int Applications)>();
        var enemyHp = new List<int>();
        BellBoltSentryState previousSentryState = BellBoltSentryState.Idle;
        int damaged = 0, died = 0, healed = 0, defeated = 0;
        int lastEnemyHealth = 0, lastApplications = 0;
        Vector2 lastSentryPosition = Vector2.Zero;
        AttackTimelineFrame lastSentryTimeline = default;
        ulong deadline = 0, entryFrame = 0, firstRetirementFrame = 0, firstReceiptWindow = 0;
        ulong defeatPhysics = 0, defeatInputSequence = 0, defeatedSentryId = 0;
        bool observedRealRetirement = false, firstReceipt = false, defeatedDuringJ2 = false;
        (ulong Frame, ulong Sequence, Vector2 Position, Vector2 Velocity, bool Supported,
            float RawAxis, float Axis, bool A, bool D, bool Burning)? completedMotion = null;
        string motionObservationError = null;
        int neutralDiscriminatorSamples = 0;
        int fallbackApproachTicks = 0;

        // All native callbacks buffer values only; commands/assertions run in the
        // awaited observer. Preserve synchronous Latched -> Released publications.
        void OnAttack(ulong id, int direction) => attacks.Add(
            (id, Engine.GetPhysicsFrames(), Input.IsPhysicalKeyPressed(Key.J), direction));
        void OnConnected(Hurtbox target) => connections.Add(
            (attacks.Count == 0 ? 0 : attacks[^1].Id, target.GetInstanceId(), lastEnemyHealth));
        void OnRetired(BellBoltProjectile bolt) => retired.Add(
            (bolt.GetInstanceId(), Engine.GetPhysicsFrames(), bolt.TravelledDistance));
        void OnWarning(AttackPresentationFrame frame)
        {
            if (frame.Timeline.Kind == EnemyAttackKind.BellBoltShot && frame.Timeline.Phase == AttackPhase.Telegraph &&
                (warnings.Count == 0 || warnings[^1].Id != frame.Timeline.AttackId))
                warnings.Add((frame.Timeline.AttackId, Engine.GetPhysicsFrames()));
        }
        void OnGuard(PlayerShieldResult result, ulong transition)
        {
            BellBoltProjectile live = bolts.Values.FirstOrDefault(bolt => GodotObject.IsInstanceValid(bolt) && !bolt.IsRetired);
            guarded.Add((live?.GetInstanceId() ?? 0, Engine.GetPhysicsFrames(), result.Guarded, result.DamageAmount,
                health.CurrentHealth, shield.GuardDirection,
                live == null ? Vector2.Zero : live.GlobalPosition - player.GlobalPosition));
        }
        void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target) => clawFrames.Add(
            (frame.CastId, frame.State, frame.LockedTargetRuntimeId, Engine.GetPhysicsFrames(),
                input.Current.ClawPressed, Input.IsPhysicalKeyPressed(Key.K)));
        void OnSentryFrame(BellBoltSentryFrame frame)
        {
            lastSentryTimeline = frame.AttackTimeline;
            lastApplications = adapter.ApplicationCount;
            if (frame.State == BellBoltSentryState.Stunned && previousSentryState != BellBoltSentryState.Stunned)
                stuns.Add((claw.CurrentFrame.CastId, Engine.GetPhysicsFrames(), lastApplications));
            previousSentryState = frame.State;
        }
        void OnDamaged(int remaining, Vector2 impulse) => damaged++;
        void OnDied() => died++;
        void OnHealed(int restored, int remaining) => healed++;
        void OnEnemyDamaged(int remaining, Vector2 impulse) { lastEnemyHealth = remaining; enemyHp.Add(remaining); }
        void OnDefeated()
        {
            defeated++;
            if (defeated != 1) return;
            lastEnemyHealth = enemyHealth.CurrentHealth;
            lastApplications = adapter.ApplicationCount;
            lastSentryPosition = sentry.GlobalPosition;
            defeatedSentryId = sentry.GetInstanceId();
            defeatPhysics = Engine.GetPhysicsFrames();
            defeatInputSequence = input.CurrentFrameSequence;
            defeatedDuringJ2 = attacks.Count == preparationAttacks + 2 && combat.IsAttacking;
            // The sole callback-side mutation emulates encounter batch removal;
            // the actual native actor must disappear while J2 is still in flight.
            if (retireOnDefeat) sentry.QueueFree();
        }
        void CollectOwnedShapes(Node node)
        {
            if (node is CollisionShape2D shape && shape.Shape != null) resources.Add(shape.Shape);
            Godot.Collections.Array<Node> children = node.GetChildren();
            using Godot.Collections.Array backing = (Godot.Collections.Array)children;
            foreach (Node child in children) CollectOwnedShapes(child);
        }
        BellBoltProjectile LiveBolt()
        {
            if (!GodotObject.IsInstanceValid(sentry) || !sentry.IsInsideTree() || sentry.IsQueuedForDeletion()) return null;
            Godot.Collections.Array<Node> children = sentry.GetChildren();
            using Godot.Collections.Array backing = (Godot.Collections.Array)children;
            foreach (Node child in children)
                if (child is BellBoltProjectile bolt && !bolt.IsQueuedForDeletion() && !bolt.IsRetired) return bolt;
            return null;
        }

        try
        {
            PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn",
                cacheMode: ResourceLoader.CacheMode.Ignore);
            resources.Add(playerScene);
            player = playerScene.Instantiate<CharacterBody2D>();
            player.Name = "ActualPlayer";
            player.Position = new Vector2(540f, 293f); // Saved full capsule feet=329.
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.AddToGroup("player");
            var motor = player.GetNode<PlayerMotor>("Motor");
            input = player.GetNode<PlayerInputReader>("Input");
            combat = player.GetNode<PlayerCombat>("Combat");
            energy = player.GetNode<PlayerEnergyController>("Energy");
            shield = player.GetNode<PlayerShieldController>("Shield");
            claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            health = player.GetNode<HealthComponent>("Health");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            resources.Add(motor.Tuning); resources.Add(energy.Tuning);
            resources.Add(player.GetNode<PlayerPresentationCoordinator>("Presentation").Profile);
            PackedScene sentryScene = ResourceLoader.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn",
                cacheMode: ResourceLoader.CacheMode.Ignore);
            resources.Add(sentryScene);
            sentry = sentryScene.Instantiate<BellBoltSentry>();
            sentry.Name = "canyon_v2_encounter_steam-valves_RecoverySentry";
            sentry.Position = new Vector2(300f, 300f);
            lastSentryPosition = sentry.Position;
            resources.Add(sentry.Settings);
            enemyHealth = sentry.GetNode<HealthComponent>("Health");
            lastEnemyHealth = enemyHealth.MaxHealth;
            var target = sentry.GetNode<ThreadTarget2D>("ThreadTarget");
            adapter = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            ulong playerId = player.GetInstanceId(), sentryId = sentry.GetInstanceId();
            ulong targetId = target.GetInstanceId(), hurtboxId = sentry.GetNode<Hurtbox>("Hurtbox").GetInstanceId();
            var floor = new StaticBody2D { Name = "Floor", Position = new Vector2(580f, 339f),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
            floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1100f, 20f) } });
            root.AddChild(floor); root.AddChild(player);
            CollectOwnedShapes(root); CollectOwnedShapes(sentry);
            combat.AttackStarted += OnAttack; combat.AttackConnected += OnConnected;
            claw.ClawFrameChanged += OnClaw; shield.ShieldResolved += OnGuard;
            health.Damaged += OnDamaged; health.Died += OnDied; health.Healed += OnHealed;
            enemyHealth.Damaged += OnEnemyDamaged;
            sentry.AttackFrameChanged += OnWarning; sentry.FrameChanged += OnSentryFrame; sentry.Defeated += OnDefeated;
            root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
            {
                // Values only, after this tick's real Input and Motor. Never
                // issue commands or throw assertions inside a native callback.
                try
                {
                    completedMotion = (Engine.GetPhysicsFrames(), input.CurrentFrameSequence,
                        player.GlobalPosition, player.Velocity, Supported(), input.RawFrame.MoveAxis,
                        input.Current.MoveAxis, Input.IsPhysicalKeyPressed(Key.A),
                        Input.IsPhysicalKeyPressed(Key.D), motor.IsDashing);
                }
                catch (Exception error) { motionObservationError = error.ToString(); }
            } });

            int hz = Engine.PhysicsTicksPerSecond;
            MechanicalClawSettings clawSettings = MechanicalClawSettings.Default;
            float fullAttack = combat.Tuning.AttackStartupSeconds + combat.Tuning.AttackActiveSeconds + combat.Tuning.AttackRecoverySeconds;
            ulong retiredCounterWindow = (ulong)Math.Ceiling(fullAttack * hz) + 6UL + 2UL;
            // Audited 2026-10-16 native caps apply to this saved 60Hz fixture,
            // not to arbitrary Hz. Keep the original pre-mount clock anchor;
            // AddChild completes Ready synchronously without a physics wait.
            const ulong legacyGlobalCap = 1215UL, legacyFirstReceiptCap = 73UL;
            TestAssert.Equal(60, hz, "Sentry historical native clock caps are qualified only at 60Hz");
            entryFrame = Engine.GetPhysicsFrames();
            tree.Root.AddChild(root);
            PlayerMotionSettings actualMotion = motor.MotionSettings;
            MobilityModuleSettings actualModule = motor.ThrusterState.Module;
            ulong configurationEpoch = motor.ThrusterState.ConfigurationEpoch;
            HorizontalForecastResult runStop = FreeMotionForecast.Neutral(actualMotion.MaxRunSpeed, true,
                actualMotion, actualModule, 1d / hz, checked((int)legacyGlobalCap), 2f);
            TestAssert.True(runStop.SpeedThresholdReached && runStop.Steps == 9 &&
                MathF.Abs(runStop.ForwardDistance - 1000f / 60f) < .003f &&
                actualModule.GroundFriction == 1800f && actualMotion.MaxRunSpeed == 260f,
                "saved actual standard module has independently known nine-tick, 1000/60 ground-neutral stopping sum");
            double approachSeconds = 180d / actualMotion.MaxRunSpeed +
                actualMotion.MaxRunSpeed / (double)actualMotion.GroundAcceleration + runStop.Steps / (double)hz;
            // One non-resetting clock: real preparation, warning/full flight,
            // approach/brake, empty-to-full natural regen, at most four spaced K
            // handshakes/retractions, and two full J/cadence/six-margin windows.
            double boundSeconds = preparationAttacks * (fullAttack + 2f / hz) +
                sentry.Settings.TelegraphSeconds + sentry.Settings.MaximumTravel / sentry.Settings.BoltSpeed +
                approachSeconds + energy.Tuning.RegenDelaySeconds + energy.Tuning.MaxEnergy / energy.Tuning.RegenPerSecond +
                4f * (MathF.Max(1f, sentry.Settings.ThreadStunSeconds) + clawSettings.MaxRange / clawSettings.ShuttleSpeed +
                    clawSettings.MaxRange / clawSettings.RetractSpeed + 6f / hz) +
                2f * (fullAttack + 26f / hz + 6f / hz) + 8f / hz;
            ulong derivedGlobalFrames = checked((ulong)Math.Ceiling(boundSeconds * hz));
            ulong effectiveGlobalFrames = Math.Min(derivedGlobalFrames, legacyGlobalCap);
            deadline = entryFrame + effectiveGlobalFrames;
            GD.Print($"STEAM_SENTRY_RECOVERY_DEADLINE entry={entryFrame} deadline={deadline} frames={deadline-entryFrame} " +
                $"derived={derivedGlobalFrames} legacy_cap={legacyGlobalCap} effective={effectiveGlobalFrames} " +
                $"clipped={derivedGlobalFrames > effectiveGlobalFrames} stop_ticks={runStop.Steps} " +
                $"module={actualModule.ModuleId} friction={actualModule.GroundFriction:R} config_epoch={configurationEpoch} " +
                $"prep_J=7 warning={sentry.Settings.TelegraphSeconds:R} maxflight={sentry.Settings.MaximumTravel/sentry.Settings.BoltSpeed:R} " +
                $"stun={sentry.Settings.ThreadStunSeconds:R} full_J={fullAttack:R} frozen=true");
            driver = KeyboardRouteInputDriver.Begin(player);
            ulong lastInputSequence = 0;
            int completedAttacks = 0, pendingAttackCount = 0, marginSamples = -1;
            int retiredTicks = 0, retiredOwnedTicks = 0, goneSafetySamples = 0;
            ulong pendingAttackId = 0, lastAttackCommand = 0;

            string Snapshot() => $"physics={Engine.GetPhysicsFrames()}/{deadline} input={input.CurrentFrameSequence} " +
                $"stage={recovery?.Stage ?? "Preparation"} player={player.GlobalPosition} velocity={player.Velocity} " +
                $"sentry={lastSentryPosition} state={previousSentryState} remaining={lastSentryTimeline.PhaseRemainingSeconds:R} " +
                $"gap={player.GlobalPosition.X-lastSentryPosition.X:R} energy={energy.EnergyRatio:R} heat={energy.HeatRatio:R} " +
                $"L={input.Current.ShieldHeld}/{shield.IsActive} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.CastId} " +
                $"K={driver.ClawEdges} apps={lastApplications} J={Math.Max(0,attacks.Count-preparationAttacks)} " +
                $"hp={health.CurrentHealth} enemy_hp={lastEnemyHealth} bolts={born.Count}/{retired.Count} " +
                $"retire_on_defeat={retireOnDefeat} defeat={defeatPhysics} retired_ticks={retiredTicks} gone_samples={goneSafetySamples} " +
                $"fallback_approach={exerciseFallbackApproach}/{fallbackApproachTicks}";
            void Check(bool condition, string claim) => TestAssert.True(condition, claim + "; " + Snapshot());
            bool SentryPresent() => GodotObject.IsInstanceValid(sentry) && sentry.IsInsideTree() && !sentry.IsQueuedForDeletion();
            int ObservedEnemyHealth() => GodotObject.IsInstanceValid(enemyHealth) ? enemyHealth.CurrentHealth : lastEnemyHealth;
            int ObservedApplications() => GodotObject.IsInstanceValid(adapter) ? adapter.ApplicationCount : lastApplications;
            bool Supported()
            {
                if (!player.IsOnFloor() || MathF.Abs(player.GlobalPosition.Y + 36f - 329f) > .2f ||
                    player.GlobalPosition.X - 20f < 30f || player.GlobalPosition.X + 20f > 1130f) return false;
                for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                    if (player.GetSlideCollision(index).GetCollider() == floor) return true;
                return false;
            }
            bool GuardRequired() => SentryPresent() && (sentry.CurrentFrame.State == BellBoltSentryState.Fire || LiveBolt() != null ||
                sentry.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph &&
                sentry.CurrentFrame.AttackTimeline.PhaseRemainingSeconds <= .40f);
            float SafeSpan()
            {
                AttackTimelineFrame timeline = sentry.CurrentFrame.AttackTimeline;
                if (sentry.CurrentFrame.State == BellBoltSentryState.Stunned && stuns.Count > 0)
                {
                    float elapsed = (Engine.GetPhysicsFrames() - stuns[^1].Frame) / (float)hz;
                    return MathF.Max(0f, sentry.Settings.ThreadStunSeconds - elapsed) + sentry.Settings.TelegraphSeconds - .40f;
                }
                return timeline.Phase switch
                {
                    AttackPhase.Telegraph => MathF.Max(0f, timeline.PhaseRemainingSeconds - .40f),
                    AttackPhase.Recovery => timeline.PhaseRemainingSeconds + sentry.Settings.TelegraphSeconds - .40f,
                    _ => 0f // Idle/None is never an unbounded cached stun window.
                };
            }
            bool SafeCounter() => LiveBolt() == null && !GuardRequired() &&
                SafeSpan() >= fullAttack + 8f / hz &&
                (sentry.CurrentFrame.State == BellBoltSentryState.Stunned && stuns.Count > 0 ||
                 sentry.CurrentFrame.State == BellBoltSentryState.Telegraph && sentry.CurrentFrame.AttackTimeline.PhaseRemainingSeconds > .70f ||
                 sentry.CurrentFrame.State == BellBoltSentryState.Recover &&
                    sentry.CurrentFrame.AttackTimeline.PhaseRemainingSeconds >= fullAttack + 8f / hz);
            bool StrikeRange() => player.GlobalPosition.X - sentry.GlobalPosition.X is >= 8f and <= 64f &&
                MathF.Abs(player.GlobalPosition.Y - sentry.GlobalPosition.Y) <= 46f;
            bool ExactPlan()
            {
                MechanicalClawTargetResolution selected = new MechanicalClawTargetResolver().ResolveKeyboard(player, input.KeyboardAimDirection);
                return selected.Surface == target && selected.Snapshot.RuntimeId == targetId && selected.Snapshot.Exists &&
                    selected.Snapshot.HasLineOfSight && selected.Snapshot.IsLegacyInteraction &&
                    target.TryGetEligibleReceiver(player, out Node receiver) && receiver == sentry;
            }
            async Task Tick(bool support = true)
            {
                await Frames(tree, 1);
                Check(Engine.GetPhysicsFrames() <= deadline, "STEAM_SENTRY_RECOVERY_FIXED_NATIVE_DEADLINE");
                Check(driver.ObservedFrames == 0 || input.CurrentFrameSequence > lastInputSequence,
                    "Sentry recovery uses newly native-processed input for every sample");
                lastInputSequence = input.CurrentFrameSequence;
                driver.ObserveFrame();
                bool sentryPresent = SentryPresent();
                if (sentryPresent)
                {
                    lastSentryPosition = sentry.GlobalPosition;
                    lastEnemyHealth = enemyHealth.CurrentHealth;
                    lastApplications = adapter.ApplicationCount;
                }
                BellBoltProjectile live = LiveBolt();
                if (live != null && !bolts.ContainsKey(live.GetInstanceId()))
                {
                    bolts.Add(live.GetInstanceId(), live);
                    CollectOwnedShapes(live);
                    live.Retired += OnRetired;
                    born.Add((live.GetInstanceId(), sentry.CurrentFrame.AttackTimeline.AttackId,
                        Engine.GetPhysicsFrames(), sentry.CurrentFrame.AttackTimeline.Kind));
                }
                Check(player.GetInstanceId() == playerId &&
                    player.SceneFilePath == "res://scenes/player/Player.tscn" &&
                    (retireOnDefeat && defeated == 1 && defeatedSentryId == sentryId ||
                     GodotObject.IsInstanceValid(sentry) && sentry.GetInstanceId() == sentryId &&
                        sentry.SceneFilePath == "res://scenes/enemies/BellBoltSentry.tscn") &&
                    motor.IsPhysicsProcessing() && input.IsPhysicsProcessing() && combat.IsPhysicsProcessing() && energy.IsPhysicsProcessing() &&
                    ReferenceEquals(motor.MotionSettings, actualMotion) && motor.ThrusterState.Module == actualModule &&
                    motor.ThrusterState.ConfigurationEpoch == configurationEpoch &&
                    shield.IsPhysicsProcessing() && claw.IsPhysicsProcessing() && health.MaxHealth == 5 && health.CurrentHealth == 5 &&
                    damaged == 0 && died == 0 && healed == 0 && motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0 &&
                    !input.Current.JumpPressed && !input.Current.JumpHeld && !input.Current.DashPressed && driver.ObservedMouseMotionSamples == 0 &&
                    (!support || Supported()), "saved Sentry recovery preserves HP5/live components/native floor without jump/dash/rescue");
                if (pendingAttackCount > 0)
                {
                    Check(LiveBolt() == null && (!sentryPresent || sentry.CurrentFrame.State != BellBoltSentryState.Fire),
                        "full native J and its six actual margin samples never meet a live bolt/Fire");
                    if (attacks.Count == pendingAttackCount)
                    {
                        pendingAttackId = attacks[^1].Id;
                        Check(attacks[^1].PhysicalJ && attacks[^1].Direction == (int)AttackDirection.Horizontal && combat.LastAttackFacing == -1,
                            "counter accepts one actual left-facing physical J");
                        if (combat.IsAttacking) Check(!GuardRequired(), "full J finishes before the real last-.40 guard window");
                        else if (marginSamples < 0) marginSamples = 6;
                        else if (--marginSamples == 0)
                        {
                            completedAttacks++;
                            Check(connections.Count == completedAttacks && connections[^1].AttackId == pendingAttackId &&
                                connections[^1].HurtboxId == hurtboxId && connections[^1].Remaining == 2 - completedAttacks &&
                                ObservedEnemyHealth() == 2 - completedAttacks,
                                "complete J plus six native safety samples owns one exact saved Sentry Hurtbox/HP transition");
                            pendingAttackCount = 0; marginSamples = -1;
                        }
                    }
                    else Check(attacks.Count < pendingAttackCount, "one physical J command cannot duplicate an accepted attack");
                }
            }
            void Fallback()
            {
                bool guard = GuardRequired();
                BellBoltProjectile live = LiveBolt();
                Vector2 source = live?.GlobalPosition ?? sentry.GetNode<Node2D>("Muzzle").GlobalPosition;
                float gap = player.GlobalPosition.X - sentry.GlobalPosition.X;
                bool move = false;
                if (observedRealRetirement && !guard && gap > 64f)
                {
                    Check(motor.ThrusterState.BurnRemaining == 0f,
                        "fallback neutral forecast cannot own an active jet");
                    HorizontalForecastResult stop = FreeMotionForecast.Neutral(player.Velocity.X, true,
                        motor.MotionSettings, motor.ThrusterState.Module, 1d / hz,
                        checked((int)(deadline - Engine.GetPhysicsFrames())), 2f);
                    Check(stop.SpeedThresholdReached,
                        "fallback neutral must settle within the caller's unchanged remaining clock");
                    move = gap - 60f > stop.ForwardDistance + MathF.Abs(player.Velocity.X) * 2f / hz;
                }
                driver.Command(InputActions.PrimaryAction, false);
                driver.Command(InputActions.MoveRight, false);
                driver.Command(InputActions.MoveLeft, move);
                driver.PointerButton(MouseButton.Right, guard, player, source);
                if (observedRealRetirement && pendingAttackCount == 0 && StrikeRange() && SafeCounter() &&
                    !input.Current.ShieldHeld && !shield.IsActive && modes.CanBeginAttack &&
                    Engine.GetPhysicsFrames() >= lastAttackCommand + 26UL &&
                    SentryCounterEnergyBudget.CanSpendAttack(energy.EnergyRatio * energy.Tuning.MaxEnergy,
                        energy.Tuning.ToSettings(), sentry.Settings, hz))
                    driver.Command(InputActions.PrimaryAction, true);
            }

            await Tick(support: false); await Tick();
            for (int preparation = 0; preparation < preparationAttacks; preparation++)
            {
                int before = attacks.Count;
                driver.Command(InputActions.PrimaryAction, true);
                do { await Tick(); } while (attacks.Count == before);
                driver.Command(InputActions.PrimaryAction, false);
                Check(attacks.Count == before + 1 && attacks[^1].PhysicalJ, "each preparatory J is one accepted real keyboard attack");
                while (combat.IsAttacking) await Tick();
            }
            Check(attacks.Count == 7 && connections.Count == 0 && driver.ClawEdges == 0 && energy.EnergyRatio < .2f &&
                player.GlobalPosition.DistanceTo(new Vector2(540f, 293f)) < .2f,
                "seven actual J naturally spend 84 energy before saved Sentry mounting, with default HP5 and no position writes");
            root.AddChild(sentry);
            recovery = new SteamSentryRecoveryDriver(player, sentry, driver);
            while (!recovery.Completed || ObservedEnemyHealth() > 0 || pendingAttackCount > 0)
            {
                if (retireOnDefeat && defeated > 0)
                {
                    bool tailHandled = recovery.TryFinishRetiredCounter();
                    await Tick(); // Exactly the same one ordinary native tick, not a nested tail wait.
                    retiredTicks++;
                    if (tailHandled) retiredOwnedTicks++;
                    if (!GodotObject.IsInstanceValid(sentry)) goneSafetySamples++;
                    Check(defeated == 1 && defeatedDuringJ2 && lastEnemyHealth == 0 &&
                        bolts.Values.All(bolt => !GodotObject.IsInstanceValid(bolt) || bolt.IsRetired),
                        "queued defeated Sentry cannot retain native attack authority while J2 completes");
                    if (recovery.Completed)
                        Check(tailHandled && retiredOwnedTicks == retiredTicks && goneSafetySamples >= 6 &&
                            input.CurrentFrameSequence > defeatInputSequence && !GodotObject.IsInstanceValid(sentry) &&
                            !GodotObject.IsInstanceValid(enemyHealth) && !GodotObject.IsInstanceValid(adapter) &&
                            completedAttacks == 2 && pendingAttackCount == 0 && !combat.IsAttacking &&
                            !Input.IsPhysicalKeyPressed(Key.J) && !Input.IsPhysicalKeyPressed(Key.K) && !Input.IsPhysicalKeyPressed(Key.L),
                            "retired-owner helper keeps each fresh tick through actual J2 completion and six native gone-enemy safety samples");
                    if (!recovery.Completed && Engine.GetPhysicsFrames() - defeatPhysics >= retiredCounterWindow)
                        Check(false, "STEAM_SENTRY_RETIRED_COUNTER_COMPLETION_RED queued owner removal must not abandon J2 full attack and six-sample completion");
                    continue;
                }
                if (!observedRealRetirement && LiveBolt() == null)
                {
                    var receipt = retired.FirstOrDefault(retirement => retirement.Travel > 0f &&
                        guarded.Any(guard => guard.BoltId == retirement.BoltId && guard.Guarded && guard.Damage == 0 && guard.Hp == 5 &&
                            guard.Frame <= retirement.Frame && guard.Direction.Normalized().Dot(guard.Source.Normalized()) >= .9f) &&
                        born.Any(bolt => bolt.BoltId == retirement.BoltId && bolt.Kind == EnemyAttackKind.BellBoltShot &&
                            warnings.Any(warning => warning.Id == bolt.AttackId && warning.Frame < bolt.Frame)));
                    if (receipt.BoltId != 0)
                    {
                        Check(driver.ClawEdges == 0 && attacks.Count == preparationAttacks,
                            "native typed warning -> real bolt -> Guarded/HP5 -> actual retirement precedes helper arm");
                        observedRealRetirement = true;
                        firstRetirementFrame = Engine.GetPhysicsFrames();
                        // Receipt-specific elapsed window, derived ONCE from this
                        // measured geometry and saved motion/claw clocks; the global
                        // deadline above is not reset or extended.
                        float distance = player.GlobalPosition.DistanceTo(target.WorldAnchor);
                        double approach = Math.Max(0d, MathF.Abs(player.GlobalPosition.X-sentry.GlobalPosition.X)-60f) /
                            actualMotion.MaxRunSpeed + actualMotion.MaxRunSpeed / (double)actualMotion.GroundAcceleration +
                            runStop.Steps / (double)hz;
                        ulong derivedFirstFrames = checked((ulong)Math.Ceiling((approach + distance/clawSettings.ShuttleSpeed) * hz)) + 10UL;
                        firstReceiptWindow = Math.Min(derivedFirstFrames, legacyFirstReceiptCap);
                        GD.Print($"STEAM_SENTRY_RECOVERY_REAL_RETIREMENT_OK bolt={receipt.BoltId} travel={receipt.Travel:R} " +
                            $"derived={derivedFirstFrames} legacy_cap={legacyFirstReceiptCap} effective={firstReceiptWindow} " +
                            $"clipped={derivedFirstFrames > firstReceiptWindow} " +
                            $"first_K_receipt_window={firstReceiptWindow} actual_energy={energy.EnergyRatio:R} " + Snapshot());
                    }
                }
                int beforeK = driver.ClawEdges, beforeJ = driver.AttackEdges, beforeApplications = adapter.ApplicationCount;
                ulong expectedCast = claw.CurrentFrame.NextCastId;
                bool legalK = observedRealRetirement && LiveBolt() == null && !GuardRequired() &&
                    claw.CurrentFrame.State == MechanicalClawState.Idle && sentry.CurrentFrame.State != BellBoltSentryState.Stunned &&
                    !input.Current.ShieldHeld && !shield.IsActive && !combat.IsAttacking && Supported() &&
                    MathF.Abs(player.Velocity.X) <= 2f && input.Current.MoveAxis == 0f && ExactPlan();
                bool legalJ = observedRealRetirement && pendingAttackCount == 0 && SafeCounter() && StrikeRange() &&
                    !input.Current.ShieldHeld && !shield.IsActive && modes.CanBeginAttack &&
                    SentryCounterEnergyBudget.CanSpendAttack(energy.EnergyRatio * energy.Tuning.MaxEnergy,
                        energy.Tuning.ToSettings(), sentry.Settings, hz);
                var beforeMotion = completedMotion;
                ulong issuedPhysics = Engine.GetPhysicsFrames(), issuedSequence = input.CurrentFrameSequence;
                Vector2 issuedPosition = player.GlobalPosition;
                float measuredGap = player.GlobalPosition.X - sentry.GlobalPosition.X;
                float measuredSpeed = MathF.Abs(player.Velocity.X);
                // Independent end-velocity arithmetic, not the forecast under
                // test. Standard ground neutral subtracts 1800/hz per tick;
                // preserve the route's original two-tick positioning reserve.
                double independentStop = 0d, remainingSpeed = measuredSpeed;
                for (int step = 0; remainingSpeed > 2d && step < hz; step++)
                {
                    remainingSpeed = Math.Max(0d, remainingSpeed - 1800d / hz);
                    independentStop += remainingSpeed / hz;
                }
                double reserve = 2d * measuredSpeed / hz;
                double oldStoppingEstimate = measuredSpeed * measuredSpeed / (2d * 2800d) + reserve;
                bool distinguishNeutral = observedRealRetirement && beforeMotion.HasValue &&
                    beforeMotion.Value.Supported && !beforeMotion.Value.Burning && measuredSpeed > 2f &&
                    measuredGap > 64f && measuredGap - 60f > oldStoppingEstimate &&
                    measuredGap - 60f <= independentStop + reserve;
                // Exercise the existing fallback only for the same real
                // post-retirement approach; the helper still owns K/J and its
                // retirement tail. No new enemy clock, movement or wait owner.
                bool fallbackApproach = exerciseFallbackApproach && observedRealRetirement && measuredGap > 64f;
                bool handled = !fallbackApproach && recovery.TryDrive(observedRealRetirement,
                    checked((int)(deadline - issuedPhysics)));
                if (!handled) Fallback();
                if (fallbackApproach) fallbackApproachTicks++;
                bool probeNeutral = distinguishNeutral &&
                    (handled && recovery.Stage == "ApproachAndBrake" || fallbackApproach && !GuardRequired());
                bool commandedA = Input.IsPhysicalKeyPressed(Key.A), commandedD = Input.IsPhysicalKeyPressed(Key.D);
                if (driver.ClawEdges != beforeK)
                {
                    Check(driver.ClawEdges == beforeK + 1 && driver.ClawEdges <= 4 && legalK &&
                        !Input.IsPhysicalKeyPressed(Key.L) && !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                        (casts.Count == 0 || Engine.GetPhysicsFrames() - casts[^1].Frame >= (ulong)hz),
                        "real K requires retired bolt, sampled L release, neutral floor stance, Idle claw, exact selector and no old Stunned/cooldown refresh");
                    casts.Add((expectedCast, Engine.GetPhysicsFrames(), beforeApplications));
                }
                if (driver.AttackEdges != beforeJ)
                {
                    if (completedAttacks == 0)
                        Check(energy.EnergyRatio * energy.Tuning.MaxEnergy > 70f &&
                            SafeSpan() >= 26f / hz + fullAttack + 8f / hz,
                            "STEAM_SENTRY_DOUBLE_COUNTER_PREPARATION_RED first J preserves the literal >70 two-payment reserve and complete native double window");
                    Check(driver.AttackEdges == beforeJ + 1 && legalJ && driver.AttackEdges <= preparationAttacks + 2 &&
                        (lastAttackCommand == 0 || Engine.GetPhysicsFrames() - lastAttackCommand >= 26UL),
                        "fresh J revalidates no live bolt, actual counter clock/range, sampled L release and original >58 energy budget");
                    pendingAttackCount = attacks.Count + 1;
                    lastAttackCommand = Engine.GetPhysicsFrames();
                }
                await Tick();
                if (probeNeutral)
                {
                    Check(motionObservationError == null && completedMotion.HasValue &&
                        beforeMotion.Value.Frame + 1UL == issuedPhysics && completedMotion.Value.Frame == issuedPhysics &&
                        completedMotion.Value.Sequence > issuedSequence &&
                        beforeMotion.Value.Position.DistanceTo(issuedPosition) < .01f &&
                        motor.ThrusterState.Module.GroundFriction == 1800f,
                        "neutral brake probe owns one completed prior sample and one fresh real Motor step on the standard module");
                    var afterMotion = completedMotion.Value;
                    float expectedVelocity = MathF.Sign(beforeMotion.Value.Velocity.X) *
                        MathF.Max(0f, measuredSpeed - 1800f / hz);
                    GD.Print($"STEAM_SENTRY_NEUTRAL_DISCRIMINATOR owner={(fallbackApproach ? "fallback" : "helper")} " +
                        $"physics={issuedPhysics} gap={measuredGap:R} " +
                        $"old_stop={oldStoppingEstimate:R} actual_stop={independentStop + reserve:R} " +
                        $"command_A={commandedA} command_D={commandedD} raw_axis={afterMotion.RawAxis:R} " +
                        $"sampled_axis={afterMotion.Axis:R} before_vx={beforeMotion.Value.Velocity.X:R} " +
                        $"actual_vx={afterMotion.Velocity.X:R} expected_vx={expectedVelocity:R}");
                    Check(!commandedA && !commandedD && !afterMotion.A && !afterMotion.D &&
                        afterMotion.RawAxis == 0f && afterMotion.Axis == 0f && afterMotion.Supported &&
                        !afterMotion.Burning && MathF.Abs(afterMotion.Velocity.X - expectedVelocity) < .003f &&
                        MathF.Abs(afterMotion.Position.X - beforeMotion.Value.Position.X - expectedVelocity / hz) < .003f,
                        "STEAM_SENTRY_NEUTRAL_BRAKE_RED actual 1800 ground-neutral distance releases A before the old 2800 estimate and completes one real neutral step");
                    neutralDiscriminatorSamples++;
                }
                if (!firstReceipt && casts.Count > 0)
                {
                    var cast = casts[0];
                    firstReceipt = clawFrames.Any(frame => frame.Frame >= cast.Frame && frame.Frame <= cast.Frame + 2UL &&
                            frame.State == MechanicalClawState.Aim && frame.SampledK && frame.PhysicalK) &&
                        clawFrames.Any(frame => frame.Frame >= cast.Frame && frame.Id == cast.Id &&
                            frame.State == MechanicalClawState.Latched && frame.TargetId == targetId) &&
                        stuns.Any(stun => stun.CastId == cast.Id && stun.Frame >= cast.Frame && stun.Applications == cast.Applications + 1) &&
                        adapter.ApplicationCount >= cast.Applications + 1;
                }
                if (observedRealRetirement && !firstReceipt && Engine.GetPhysicsFrames() - firstRetirementFrame >= firstReceiptWindow)
                    Check(false, "STEAM_SENTRY_RECOVERY_K_RECEIPT_RED missing new physical K/newCastId/exact adapter application/new Stunned within derived approach+handshake window");
            }
            Check(neutralDiscriminatorSamples > 0 && (!exerciseFallbackApproach || fallbackApproachTicks > 0),
                "saved Sentry route must exercise a genuine old-estimate versus actual-neutral stopping discriminator");
            Check(firstReceipt && completedAttacks == 2 && attacks.Count == preparationAttacks + 2 && connections.Count == 2 &&
                attacks.Skip(preparationAttacks).Select(attack => attack.Id).Distinct().Count() == 2 && enemyHp.SequenceEqual(new[] { 1, 0 }) &&
                defeated == 1 && casts.Count is > 0 and <= 4 && ObservedApplications() == casts.Count &&
                casts.Select(cast => cast.Id).Distinct().Count() == casts.Count &&
                casts.All(cast => clawFrames.Any(frame => frame.Frame >= cast.Frame && frame.Frame <= cast.Frame + 2UL &&
                        frame.State == MechanicalClawState.Aim && frame.SampledK && frame.PhysicalK) &&
                    clawFrames.Any(frame => frame.Frame >= cast.Frame && frame.Id == cast.Id && frame.State == MechanicalClawState.Latched && frame.TargetId == targetId) &&
                    stuns.Any(stun => stun.CastId == cast.Id && stun.Frame >= cast.Frame && stun.Applications == cast.Applications + 1)) &&
                recovery.CastCommands == casts.Count && recovery.Applications == ObservedApplications() && recovery.AttackCommands == 2,
                "native Sentry recovery owns <=4 successful real K, two exact fresh J/Hurtbox hits, HP2->1->0/Defeated1 and six-margin samples per J");
            GD.Print("STEAM_SENTRY_RECOVERY_KEYBOARD_OK controlled_hp=5 HP1_claim=false group_batch_claim=false whole_goal=false " + Snapshot());
            if (retireOnDefeat)
                GD.Print($"STEAM_SENTRY_RETIRED_COUNTER_COMPLETION_OK retired_ticks={retiredTicks} native_gone_samples={goneSafetySamples} " + Snapshot());
        }
        finally
        {
            if (combat != null) { combat.AttackStarted -= OnAttack; combat.AttackConnected -= OnConnected; }
            if (claw != null) claw.ClawFrameChanged -= OnClaw;
            if (shield != null) shield.ShieldResolved -= OnGuard;
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; health.Healed -= OnHealed; }
            if (GodotObject.IsInstanceValid(enemyHealth)) enemyHealth.Damaged -= OnEnemyDamaged;
            if (GodotObject.IsInstanceValid(sentry)) { sentry.AttackFrameChanged -= OnWarning; sentry.FrameChanged -= OnSentryFrame; sentry.Defeated -= OnDefeated; }
            foreach (BellBoltProjectile bolt in bolts.Values)
                if (GodotObject.IsInstanceValid(bolt)) bolt.Retired -= OnRetired;
            try { recovery?.Dispose(); }
            finally
            {
                try { driver?.Dispose(); }
                finally
                {
                    if (GodotObject.IsInstanceValid(sentry) && sentry.GetParent() == null) sentry.Free();
                    if (player != null && player.GetParent() == null) player.Free();
                    if (root.IsInsideTree()) { root.QueueFree(); await Frames(tree, 3); }
                    else root.Free();
                    foreach (Resource resource in resources) resource.Dispose();
                }
            }
        }
    }

    public static async Task Run(SceneTree tree)
    {
        TestAssert.True(ResourceLoader.Exists("res://scenes/enemies/BellBoltSentry.tscn"),
            "generated Bell-Bolt Sentry scene exists before its runtime contracts can be exercised");
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        try
        {
            TestAssert.Equal(CombatCollisionLayers.Enemy, sentry.CollisionLayer,
                "sentry body publishes the enemy collision bit");
            TestAssert.Equal(CombatCollisionLayers.World, sentry.CollisionMask,
                "sentry body collides only with solid world");
            TestAssert.Equal(2, sentry.GetNode<HealthComponent>("Health").MaxHealth,
                "first-pass sentry has two health");
            TestAssert.True(sentry.GetNode<Hurtbox>("Hurtbox").DamageReceiverPath == new NodePath("../Health"),
                "player attacks reach authoritative sentry health");
            TestAssert.Equal(ThreadTargetKind.LightEnemy, sentry.GetNode<ThreadTarget2D>("ThreadTarget").Kind,
                "thread target uses the light-enemy pull contract");
            TestAssert.True(sentry.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "ThreadTarget/LegacyThreadInteractionAdapter") != null,
                "legacy E plus primary interaction reaches the sentry receiver");
            TestAssert.True(sentry.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph") != null,
                "local warning remains available without global directors");
            TestAssert.True(sentry.GetNodeOrNull<Node2D>("Muzzle") != null,
                "generated actor exposes an authored muzzle");
            Type projectile = typeof(BellBoltSentryRuntimeTests).Assembly.GetType(
                "Knight.Enemies.Sentry.BellBoltProjectile");
            TestAssert.True(projectile != null,
                "ranged actor owns a concrete swept projectile runtime type");
            TestAssert.True(projectile.GetMethod("Launch") != null && projectile.GetMethod("Retire") != null &&
                projectile.GetProperty("IsRetired") != null,
                "projectile exposes explicit launch and idempotent retirement lifecycle");
            TestAssert.True(typeof(BellBoltSentry).GetEvent("AttackFrameChanged") != null &&
                typeof(BellBoltSentry).GetEvent("PresentationCueRequested") != null &&
                typeof(BellBoltSentry).GetProperty("CurrentFrame") != null,
                "actor exposes typed attack, cue and current-frame runtime contracts");
        }
        finally
        {
            sentry.Free();
        }
        await InvalidActorDeltasCannotPoisonVelocityOrLaunch(tree);
        await DeadOrHealthlessPlayerCannotStartAttack(tree);
        await ProjectileStopsAtThinWorldBeforePlayer(tree);
        await DirectionalShieldConsumesOnlyTheCorrectSide(tree);
        await ProjectileRetiresAtMaximumTravel(tree);
        await ActorLocksDirectionAndResetRestoresLateSpawn(tree);
        await ActorOwnedBoltDamagesExactlyOnceDuringRecovery(tree);
        await OwnerRemovalClearsInFlightProjectileAndTypedState(tree);
        await ThreadAndDeathCancelUnlaunchedShots(tree);
        await ActualDamageAndDeathForwardOnlyNonAttackAudio(tree);
        await SentryFramesUseTheSpecifiedProvisionalAudioCues(tree);
        await VisualDescendantsCanBeRemovedWithoutChangingCombat(tree);
        await PresentationDirectlyForwardsAndClearsTypedState(tree);
    }

    private static async Task ProjectileStopsAtThinWorldBeforePlayer(SceneTree tree)
    {
        Fixture fixture = await CreatePlayerFixture(tree, "BellBoltThinWall");
        try
        {
            Vector2 target = fixture.Hurtbox.GlobalPosition;
            var wall = new StaticBody2D
            {
                Name = "ThinWorldWall", Position = new Vector2(target.X - 80f, target.Y),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
            };
            wall.AddChild(new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(3f, 120f) }
            });
            fixture.Root.AddChild(wall);
            BellBoltProjectile bolt = await AddProjectile(tree, fixture.Root,
                target - Vector2.Right * 160f, Vector2.Right, 101UL);
            int before = fixture.Health.CurrentHealth;
            await Frames(tree, 45);
            TestAssert.True(bolt.IsRetired, "swept projectile retires on a three-pixel world wall");
            TestAssert.Equal(before, fixture.Health.CurrentHealth,
                "world collision prevents damage to a player behind thin cover");
        }
        finally
        {
            fixture.Root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task DeadOrHealthlessPlayerCannotStartAttack(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltLivingTargetGate" };
        Node2D deadPlayer = CreateLivingTarget("DeadPlayer", new Vector2(240f, 0f));
        root.AddChild(deadPlayer);
        tree.Root.AddChild(root);
        await Frames(tree, 1);
        HealthComponent playerHealth = deadPlayer.GetNode<HealthComponent>("Health");
        TestAssert.True(playerHealth.ApplyDamage(new DamageRequest(99, Vector2.Right, 0f, 621UL, 1UL)),
            "real player health reaches dead before the sentry is mounted");

        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.MotionMode = CharacterBody2D.MotionModeEnum.Floating;
        root.AddChild(sentry);
        await Frames(tree, 4);
        try
        {
            TestAssert.Equal(BellBoltSentryState.Idle, sentry.CurrentFrame.State,
                "dead player remaining in the player group cannot start a warning");
            TestAssert.True(!sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                "dead player cannot cause a delayed projectile");

            deadPlayer.RemoveFromGroup("player");
            deadPlayer.QueueFree();
            var healthless = new Node2D { Name = "HealthlessPlayer", Position = new Vector2(240f, 0f) };
            healthless.AddToGroup("player");
            root.AddChild(healthless);
            sentry.ResetForCheckpoint();
            await Frames(tree, 4);
            TestAssert.Equal(BellBoltSentryState.Idle, sentry.CurrentFrame.State,
                "healthless player doubles are ignored instead of assumed alive");
            TestAssert.True(!sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                "healthless player double cannot launch a projectile");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task InvalidActorDeltasCannotPoisonVelocityOrLaunch(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltInvalidActorDelta" };
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.MotionMode = CharacterBody2D.MotionModeEnum.Floating;
        Node2D player = CreateLivingTarget("Player", new Vector2(240f, 0f));
        root.AddChild(sentry);
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);
        sentry.SetPhysicsProcess(false);
        try
        {
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.MaxValue,
                         double.NegativeInfinity, -1d })
            {
                sentry.Velocity = Vector2.Zero;
                sentry._PhysicsProcess(invalid);
                TestAssert.True(float.IsFinite(sentry.Velocity.X) && float.IsFinite(sentry.Velocity.Y),
                    $"actor boundary keeps velocity finite for invalid delta {invalid}");
                TestAssert.True(!sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                    "invalid actor delta cannot skip the warning and launch a projectile");
            }
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task DirectionalShieldConsumesOnlyTheCorrectSide(SceneTree tree)
    {
        Fixture fixture = await CreatePlayerFixture(tree, "BellBoltShieldSides");
        try
        {
            fixture.Player.GlobalPosition = fixture.Input.Current.MouseWorld - Vector2.Right * 100f;
            fixture.Input._PhysicsProcess(0d);
            Input.ActionPress(InputActions.Shield);
            fixture.Input._PhysicsProcess(0d);
            fixture.Shield._PhysicsProcess(0d);
            fixture.Energy._PhysicsProcess(0d);
            fixture.Shield._PhysicsProcess(0d);
            TestAssert.True(fixture.Shield.IsActive && fixture.Shield.GuardDirection.X > .99f,
                "real RMB shield faces right before projectile contact");
            Vector2 target = fixture.Hurtbox.GlobalPosition;
            int before = fixture.Health.CurrentHealth;

            BellBoltProjectile correct = await AddProjectile(tree, fixture.Root,
                target + Vector2.Right * 90f, Vector2.Left, 201UL);
            await Frames(tree, 25);
            TestAssert.True(correct.IsRetired, "correct-side shield contact consumes the projectile");
            TestAssert.Equal(before, fixture.Health.CurrentHealth,
                "normal guard blocks the right-side Bell-Bolt entirely");

            BellBoltProjectile wrong = await AddProjectile(tree, fixture.Root,
                target - Vector2.Right * 90f, Vector2.Right, 202UL);
            await Frames(tree, 25);
            TestAssert.True(wrong.IsRetired, "wrong-side body contact still consumes the projectile");
            TestAssert.Equal(before - 1, fixture.Health.CurrentHealth,
                "wrong-side shield allows exactly one health segment of Bell-Bolt damage");
        }
        finally
        {
            Input.ActionRelease(InputActions.Shield);
            fixture.Root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ProjectileRetiresAtMaximumTravel(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltRange" };
        tree.Root.AddChild(root);
        try
        {
            BellBoltProjectile bolt = await AddProjectile(tree, root, Vector2.Zero, Vector2.Right, 301UL);
            await Frames(tree, 100);
            TestAssert.True(bolt.IsRetired, "projectile retires after 640 pixels without a target");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ActorLocksDirectionAndResetRestoresLateSpawn(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltActorLifecycle" };
        tree.Root.AddChild(root);
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        Node2D player = CreateLivingTarget("Player", new Vector2(710f, 70f));
        var floor = new StaticBody2D
        {
            Name = "Floor", Position = new Vector2(420f, 135f),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(900f, 20f) }
        });
        root.AddChild(floor);
        root.AddChild(sentry);
        sentry.GlobalPosition = new Vector2(420f, 100f); // mirrors EncounterGroup assignment after AddChild
        root.AddChild(player);
        var frames = new List<AttackPresentationFrame>();
        sentry.AttackFrameChanged += frames.Add;
        try
        {
            for (int count = 0; count < 80 && !sentry.CurrentFrame.AttackTimeline.HasLockedTarget; count++)
                await Frames(tree, 1);
            TestAssert.True(sentry.CurrentFrame.AttackTimeline.HasLockedTarget,
                "runtime warning visibly reaches its midpoint lock");
            Vector2 lockedAim = sentry.CurrentFrame.AimDirection;
            player.GlobalPosition = sentry.GlobalPosition - Vector2.Right * 290f;
            for (int count = 0; count < 80 && !sentry.GetChildren().OfType<BellBoltProjectile>().Any(); count++)
                await Frames(tree, 1);
            BellBoltProjectile bolt = sentry.GetChildren().OfType<BellBoltProjectile>().Single();
            Vector2 muzzle = sentry.GetNode<Node2D>("Muzzle").GlobalPosition;
            await Frames(tree, 2);
            TestAssert.True(bolt.GlobalPosition.X > muzzle.X && lockedAim.X > 0f,
                "shot keeps the pre-crossing locked direction after player changes side");
            for (int count = 0; count < 12 && sentry.CurrentFrame.State != BellBoltSentryState.Recover; count++)
                await Frames(tree, 1);
            TestAssert.True(sentry.CurrentFrame.State == BellBoltSentryState.Recover &&
                GodotObject.IsInstanceValid(bolt) && !bolt.IsRetired && bolt.Visible,
                "visible projectile circle remains truthful after the 0.12s launch pose enters recovery");
            TestAssert.Equal(1, sentry.GetChildren().OfType<BellBoltProjectile>().Count(),
                "actor owns at most one live projectile");
            TestAssert.True(frames.Any(frame => frame.Timeline.Phase == AttackPhase.Telegraph) &&
                frames.Any(frame => frame.Timeline.Phase == AttackPhase.Active),
                "typed warning always precedes the active projectile circle");

            HealthComponent health = sentry.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 401UL, 1UL)),
                "fixture can damage authoritative sentry health before reset");
            sentry.ResetForCheckpoint();
            TestAssert.True(sentry.CurrentFrame.AttackTimeline.Phase == AttackPhase.None,
                "checkpoint reset synchronously clears typed attack identity");
            await Frames(tree, 2);
            TestAssert.Equal(2, health.CurrentHealth, "checkpoint reset restores configured two health");
            TestAssert.Near(420f, sentry.GlobalPosition.X, .01f,
                "checkpoint reset restores position assigned after AddChild rather than origin");
            TestAssert.True(!sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                "checkpoint reset removes the actor-owned in-flight bolt");
        }
        finally
        {
            sentry.AttackFrameChanged -= frames.Add;
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ThreadAndDeathCancelUnlaunchedShots(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltInterruptions" };
        Node2D player = CreateLivingTarget("Player", new Vector2(260f, 0f));
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        root.AddChild(sentry);
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 3);
        try
        {
            TestAssert.Equal(BellBoltSentryState.Telegraph, sentry.CurrentFrame.State,
                "runtime actor enters its warning before interruption");
            sentry.ApplyThreadPull(Vector2.Left * 540f);
            await Frames(tree, 1);
            TestAssert.Equal(BellBoltSentryState.Stunned, sentry.CurrentFrame.State,
                "real light-enemy receiver turns thread contact into visible stun");
            TestAssert.True(!sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                "thread interruption cannot leak a delayed bolt");

            sentry.ResetForCheckpoint();
            await Frames(tree, 35);
            int defeated = 0;
            sentry.Defeated += () => defeated++;
            HealthComponent health = sentry.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(99, Vector2.Right, 0f, 501UL, 2UL)),
                "lethal real HealthComponent request is accepted at an attack boundary");
            await Frames(tree, 2);
            TestAssert.Equal(1, defeated, "death emits Defeated exactly once");
            TestAssert.Equal(BellBoltSentryState.Dead, sentry.CurrentFrame.State,
                "death wins over stun and firing");
            TestAssert.True(!sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                "death plus pending fire cannot create an orphan bolt");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ActorOwnedBoltDamagesExactlyOnceDuringRecovery(SceneTree tree)
    {
        Fixture fixture = await CreatePlayerFixture(tree, "BellBoltActorOwnedLateDamage");
        fixture.Player.AddToGroup("player");
        fixture.Player.GlobalPosition = Vector2.Zero;
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.GlobalPosition = Vector2.Left * 320f;
        var floor = new StaticBody2D
        {
            Name = "BroadFloor", Position = new Vector2(-160f, 45f),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(1000f, 20f) }
        });
        fixture.Root.AddChild(floor);
        fixture.Root.AddChild(sentry);
        await Frames(tree, 2);
        int before = fixture.Health.CurrentHealth;
        int damageEvents = 0;
        BellBoltSentryState stateAtDamage = BellBoltSentryState.Idle;
        void OnDamaged(int remaining, Vector2 impulse)
        {
            damageEvents++;
            stateAtDamage = sentry.CurrentFrame.State;
        }
        fixture.Health.Damaged += OnDamaged;
        try
        {
            for (int count = 0; count < 100 && sentry.CurrentFrame.State != BellBoltSentryState.Recover; count++)
                await Frames(tree, 1);
            BellBoltProjectile liveBolt = sentry.GetChildren().OfType<BellBoltProjectile>().Single();
            TestAssert.True(sentry.CurrentFrame.State == BellBoltSentryState.Recover &&
                liveBolt.Visible && !liveBolt.IsRetired,
                "actor-owned bolt remains visibly dangerous after the 0.12 second fire pose");

            for (int count = 0; count < 80 && fixture.Health.CurrentHealth == before; count++)
                await Frames(tree, 1);
            TestAssert.Equal(before - 1, fixture.Health.CurrentHealth,
                "actor-owned late-flight bolt removes exactly one real player health segment");
            TestAssert.Equal(1, damageEvents, "actor-owned projectile contact produces one damage event");
            TestAssert.Equal(BellBoltSentryState.Recover, stateAtDamage,
                "real player damage lands while the firing actor is already recovering");
            sentry.SetPhysicsProcess(false);
            await Frames(tree, 4);
            TestAssert.Equal(before - 1, fixture.Health.CurrentHealth,
                "retired actor-owned bolt cannot deal delayed duplicate damage");
        }
        finally
        {
            fixture.Health.Damaged -= OnDamaged;
            fixture.Player.RemoveFromGroup("player");
            fixture.Root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task OwnerRemovalClearsInFlightProjectileAndTypedState(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltOwnerRemoval" };
        var vfx = new PrologueVfxDirector { Name = "Vfx", WorldEffectsPath = new NodePath("World") };
        vfx.AddChild(new Node2D { Name = "World" });
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.MotionMode = CharacterBody2D.MotionModeEnum.Floating;
        Node2D player = CreateLivingTarget("Player", new Vector2(300f, 0f));
        root.AddChild(vfx);
        root.AddChild(sentry);
        root.AddChild(player);
        tree.Root.AddChild(root);
        try
        {
            for (int count = 0; count < 90 && !sentry.GetChildren().OfType<BellBoltProjectile>().Any(); count++)
                await Frames(tree, 1);
            BellBoltProjectile bolt = sentry.GetChildren().OfType<BellBoltProjectile>().Single();
            TestAssert.True(bolt.Visible && !bolt.IsRetired,
                "owner-removal fixture begins with a live visible projectile");
            sentry.QueueFree();
            await Frames(tree, 3);
            TestAssert.True(!GodotObject.IsInstanceValid(sentry) && !GodotObject.IsInstanceValid(bolt),
                "removing the owner cannot leave an orphan projectile or delayed damage source");
            TestAssert.Equal(0, vfx.TypedEffectCount,
                "owner removal releases the sentry typed presentation identity");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task SentryFramesUseTheSpecifiedProvisionalAudioCues(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltAudioMapping" };
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.Name = "Sentry";
        PrologueAudioDirector audio = PrologueAudioDirector.CreateRuntimeTree();
        audio.PlaybackEnabled = true;
        root.AddChild(audio);
        root.AddChild(sentry);
        tree.Root.AddChild(root);
        await Frames(tree, 1);
        try
        {
            var telegraph = new AttackPresentationFrame(
                new AttackTimelineFrame(701UL, EnemyAttackKind.BellBoltShot, AttackPhase.Telegraph,
                    1.10f, 1f, new Vector2(640f, 0f), false),
                new AttackShapeFrame(AttackShapeKind.Capsule, new Vector2(320f, 0f),
                    new Vector2(2f, 322f), -Mathf.Pi * .5f, Array.Empty<Vector2>())).Validate();
            audio.PlayAttackFrame(sentry, telegraph, Vector2.Zero);
            TestAssert.Equal(PrologueAudioCue.LaborerTelegraph, audio.LastCue,
                "Bell-Bolt warning reuses LaborerTelegraph provisionally");

            var active = new AttackPresentationFrame(
                telegraph.Timeline with { Phase = AttackPhase.Active, PhaseDurationSeconds = .12f,
                    PhaseRemainingSeconds = .12f, HasLockedTarget = true },
                new AttackShapeFrame(AttackShapeKind.Circle, new Vector2(8f, 0f),
                    Vector2.One * 7f, 0f, Array.Empty<Vector2>())).Validate();
            audio.PlayAttackFrame(sentry, active, Vector2.Zero);
            TestAssert.Equal(PrologueAudioCue.RustShellAttack, audio.LastCue,
                "Bell-Bolt launch reuses RustShellAttack provisionally");

            var recovery = active with
            {
                Timeline = active.Timeline with { Phase = AttackPhase.Recovery,
                    PhaseDurationSeconds = 1f, PhaseRemainingSeconds = 1f }
            };
            audio.PlayAttackFrame(sentry, recovery, Vector2.Zero);
            TestAssert.Equal(PrologueAudioCue.LaborerRecovery, audio.LastCue,
                "Bell-Bolt recovery reuses LaborerRecovery provisionally");

            PrologueAudioDirector.PlayPresentationCueFromTree(sentry, PresentationCue.Hurt, Vector2.Zero);
            TestAssert.Equal(PrologueAudioCue.LaborerHurt, audio.LastCue,
                "Bell-Bolt hurt reuses LaborerHurt rather than a player fallback");
            PrologueAudioDirector.PlayPresentationCueFromTree(sentry, PresentationCue.Died, Vector2.Zero);
            TestAssert.Equal(PrologueAudioCue.LaborerDeath, audio.LastCue,
                "Bell-Bolt defeat reuses LaborerDeath rather than a player fallback");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ActualDamageAndDeathForwardOnlyNonAttackAudio(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltActualCueForwarding" };
        PrologueAudioDirector audio = PrologueAudioDirector.CreateRuntimeTree();
        audio.PlaybackEnabled = true;
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        root.AddChild(audio);
        root.AddChild(sentry);
        tree.Root.AddChild(root);
        await Frames(tree, 1);
        try
        {
            int attackFramePlayback = audio.FramePlaybackCount;
            HealthComponent health = sentry.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 611UL, 1UL)),
                "real sentry damage event is accepted for hurt-audio forwarding");
            TestAssert.Equal(PrologueAudioCue.LaborerHurt, audio.LastCue,
                "actual sentry damage forwards LaborerHurt through its presentation node");
            TestAssert.Equal(attackFramePlayback, audio.FramePlaybackCount,
                "hurt forwarding does not duplicate the typed attack-phase path");

            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Right, 0f, 612UL, 2UL)),
                "real lethal sentry damage event is accepted for death-audio forwarding");
            TestAssert.Equal(PrologueAudioCue.LaborerDeath, audio.LastCue,
                "actual sentry death forwards LaborerDeath through its presentation node");
            TestAssert.Equal(attackFramePlayback, audio.FramePlaybackCount,
                "death forwarding does not duplicate the typed attack-phase path");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task VisualDescendantsCanBeRemovedWithoutChangingCombat(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltNoVisuals" };
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.MotionMode = CharacterBody2D.MotionModeEnum.Floating;
        Node2D player = CreateLivingTarget("Player", new Vector2(260f, 0f));
        root.AddChild(sentry);
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 1);
        try
        {
            Node visualRoot = sentry.GetNode("VisualRoot");
            foreach (Node child in visualRoot.GetChildren()) child.QueueFree();
            await Frames(tree, 2);
            for (int count = 0; count < 90 && !sentry.GetChildren().OfType<BellBoltProjectile>().Any(); count++)
                await Frames(tree, 1);
            TestAssert.True(sentry.GetChildren().OfType<BellBoltProjectile>().Any(),
                "combat keeps firing when every visual descendant is removed");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task PresentationDirectlyForwardsAndClearsTypedState(SceneTree tree)
    {
        var root = new Node2D { Name = "BellBoltTypedForwarding" };
        PrologueAudioDirector audio = PrologueAudioDirector.CreateRuntimeTree();
        audio.PlaybackEnabled = false;
        var vfx = new PrologueVfxDirector { Name = "Vfx", WorldEffectsPath = new NodePath("World") };
        vfx.AddChild(new Node2D { Name = "World" });
        BellBoltSentry sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
            .Instantiate<BellBoltSentry>();
        sentry.MotionMode = CharacterBody2D.MotionModeEnum.Floating;
        Node2D player = CreateLivingTarget("Player", new Vector2(260f, 0f));
        root.AddChild(audio);
        root.AddChild(vfx);
        root.AddChild(sentry);
        root.AddChild(player);
        tree.Root.AddChild(root);
        try
        {
            for (int count = 0; count < 20 && audio.FramePlaybackCount == 0; count++) await Frames(tree, 1);
            TestAssert.True(audio.FramePlaybackCount > 0,
                "sentry presentation directly forwards typed frames outside CurrentRoom binding");
            TestAssert.True(vfx.TypedEffectCount == 1 && vfx.LastTypedAttackShape == AttackShapeKind.Capsule,
                "direct VFX forwarding renders the thin warning corridor");
            int firstPlayback = audio.FramePlaybackCount;
            sentry.ResetForCheckpoint();
            TestAssert.Equal(0, vfx.TypedEffectCount, "reset immediately releases typed VFX state");
            for (int count = 0; count < 20 && audio.FramePlaybackCount == firstPlayback; count++) await Frames(tree, 1);
            TestAssert.True(audio.FramePlaybackCount > firstPlayback,
                "reset releases audio identity so the restarted attack-one warning is not deduplicated");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 3);
        }
    }

    private static async Task<Fixture> CreatePlayerFixture(SceneTree tree, string name)
    {
        Input.ActionRelease(InputActions.Shield);
        var root = new Node2D { Name = name };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        health.InvulnerabilitySeconds = 0f;
        return new Fixture(root, player, health, player.GetNode<Hurtbox>("Hurtbox"),
            player.GetNode<PlayerInputReader>("Input"), player.GetNode<PlayerShieldController>("Shield"),
            player.GetNode<PlayerEnergyController>("Energy"));
    }

    private static async Task<BellBoltProjectile> AddProjectile(SceneTree tree, Node2D root,
        Vector2 position, Vector2 direction, ulong attackId)
    {
        var bolt = new BellBoltProjectile
        {
            Name = $"BellBolt{attackId}", CollisionLayer = 0,
            CollisionMask = CombatCollisionLayers.World
        };
        bolt.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider", Shape = new CircleShape2D { Radius = 7f }
        });
        var hitbox = new Hitbox
        {
            Name = "Hitbox", CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.Player, Damage = 1, Knockback = 120f,
            GuardClass = DamageGuardClass.Normal, Monitoring = false, Monitorable = false
        };
        hitbox.AddChild(new CollisionShape2D
        {
            Name = "HitCollider", Shape = new CircleShape2D { Radius = 7f }
        });
        bolt.AddChild(hitbox);
        root.AddChild(bolt);
        await Frames(tree, 1);
        bolt.Launch(position, direction, attackId, new BellBoltSentrySettings());
        return bolt;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static Node2D CreateLivingTarget(string name, Vector2 position)
    {
        var target = new Node2D { Name = name, Position = position };
        target.AddToGroup("player");
        target.AddChild(new HealthComponent { Name = "Health", MaxHealth = 5, InvulnerabilitySeconds = 0f });
        return target;
    }

    private sealed record Fixture(Node2D Root, CharacterBody2D Player, HealthComponent Health,
        Hurtbox Hurtbox, PlayerInputReader Input, PlayerShieldController Shield,
        PlayerEnergyController Energy);
}
