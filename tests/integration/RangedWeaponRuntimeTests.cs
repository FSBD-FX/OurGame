using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class RangedWeaponRuntimeTests
{
    private static readonly Rect2 Arena = new(80f, 120f, 1000f, 500f);
    private static bool _suiteRunning;
    private static bool _loadSavedScenes;

    public static Task Run(SceneTree tree) => RunSuite(tree, savedScenes: false);
    public static async Task RunSavedScenes(SceneTree tree) => await RunSuite(tree, savedScenes: true);

    // Native regression boundaries: whole-warning L wastes carried energy;
    // a .35 admission gate then rejects an affordable, ordinary laser guard.
    public static async Task RunGalleryLaserCarriedPolicy(SceneTree tree)
    {
        if (_suiteRunning) throw new InvalidOperationException("Ranged actor suites must run serially.");
        _suiteRunning = true;
        _loadSavedScenes = true;
        try
        {
            await GalleryLaserCarriedTrial(tree, 4);
            await GalleryLaserCarriedTrial(tree, 8);
            GD.Print("GALLERY_LASER_CARRIED_POLICY_OK trials=HIGH4J,LOW8J native_keyboard=true " +
                "live_motor=true saved_receiver=true hp=5 group_batch_claim=false whole_goal=false");
        }
        finally { _loadSavedScenes = false; _suiteRunning = false; }
    }

    private static async Task GalleryLaserCarriedTrial(SceneTree tree, int preparationAttacks)
    {
        string trial = preparationAttacks == 4 ? "HIGH4J" : "LOW8J";
        const string encounterId = "canyon.v2.encounter.pendulum-gallery";
        var root = new Node2D { Name = encounterId.Replace('.', '_') + "_Carried" + trial };
        var resources = new HashSet<Resource>();
        RangedWeaponEnemy actor = null;
        PackedScene ownedLeafScene = null;
        Fixture fixture = null;
        KeyboardRouteInputDriver driver = null;
        CharacterBody2D player = null;
        PlayerInputReader input = null;
        PlayerCombat combat = null;
        PlayerEnergyController energy = null;
        PlayerShieldController shield = null;
        HealthComponent health = null, actorHealth = null;
        var attacks = new List<(ulong Id, ulong Frame, bool PhysicalJ, PlayerEnergyState Spent)>();
        var energies = new List<(ulong Frame, PlayerEnergyFrame Value)>();
        var warnings = new List<(ulong Frame, ulong ActorId, ulong TimelineId, EnemyAttackKind Kind)>();
        var shots = new List<(ulong Frame, ulong ActorId, ulong LocalDamageOrdinal, ulong TimelineId,
            EnemyAttackKind Kind, Vector2 Origin, PlayerEnergyState Energy, bool CanBegin)>();
        var guards = new List<(ulong Frame, PlayerShieldResult Result, ulong ActorId, ulong TimelineId,
            AttackPresentationFrame Beam, Vector2 Muzzle, Vector2 Player, Vector2 Direction,
            PlayerEnergyState Energy, bool RawL, bool HeldL, bool ActiveL, int Hp)>();
        var leaves = new List<AttackTelegraphVisual>(8);
        PlayerEnergyFrame latestEnergy = default;
        bool energyObserved = false, lastDecision = false, admitted = false, beamReceiptPrinted = false,
            budgetObserved = false;
        int damaged = 0, died = 0, healed = 0, connected = 0, actorDamaged = 0, defeated = 0;
        ulong deadline = 0, entryFrame = 0, playerId = 0, actorId = 0, hurtboxId = 0, receiverId = 0;

        // Synchronous callbacks only retain value receipts. Never assert, issue
        // commands or replace receivers in the production delivery stack.
        void OnEnergy(PlayerEnergyFrame frame)
        {
            latestEnergy = frame;
            energyObserved = true;
            energies.Add((Engine.GetPhysicsFrames(), frame));
        }
        void OnAttack(ulong id, int direction) => attacks.Add(
            (id, Engine.GetPhysicsFrames(), Input.IsPhysicalKeyPressed(Key.J), latestEnergy.State));
        void OnConnected(Hurtbox target) => connected++;
        void OnWarning(AttackPresentationFrame frame)
        {
            if (frame.Timeline.Kind == EnemyAttackKind.PrismLaserBeam && frame.Timeline.Phase == AttackPhase.Telegraph)
                warnings.Add((Engine.GetPhysicsFrames(), actor.GetInstanceId(), frame.Timeline.AttackId, frame.Timeline.Kind));
        }
        void OnShot(ulong ordinal, Vector2 origin) => shots.Add((Engine.GetPhysicsFrames(), actor.GetInstanceId(),
            ordinal, actor.CurrentFrame.AttackTimeline.AttackId, actor.CurrentFrame.AttackTimeline.Kind,
            origin, latestEnergy.State, shield.CanBeginGuard));
        void OnGuard(PlayerShieldResult result, ulong transition)
        {
            AttackPresentationFrame beam = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph").CurrentFrame;
            guards.Add((Engine.GetPhysicsFrames(), result, actor.GetInstanceId(), beam.Timeline.AttackId,
                beam, actor.GetNode<Node2D>("Muzzle").GlobalPosition, player.GlobalPosition,
                shield.GuardDirection, latestEnergy.State, Input.IsPhysicalKeyPressed(Key.L),
                input.Current.ShieldHeld, shield.IsActive, health.CurrentHealth));
        }
        void OnDamaged(int remaining, Vector2 impulse) => damaged++;
        void OnDied() => died++;
        void OnHealed(int restored, int remaining) => healed++;
        void OnActorDamaged(int remaining, Vector2 impulse) => actorDamaged++;
        void OnDefeated() => defeated++;

        try
        {
            PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn",
                cacheMode: ResourceLoader.CacheMode.Ignore);
            resources.Add(playerScene);
            player = playerScene.Instantiate<CharacterBody2D>();
            player.Name = "ActualPlayer";
            player.Position = new Vector2(540f, 293f);
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.AddToGroup("player");
            var motor = player.GetNode<PlayerMotor>("Motor");
            input = player.GetNode<PlayerInputReader>("Input");
            combat = player.GetNode<PlayerCombat>("Combat");
            energy = player.GetNode<PlayerEnergyController>("Energy");
            shield = player.GetNode<PlayerShieldController>("Shield");
            health = player.GetNode<HealthComponent>("Health");
            var hurtbox = player.GetNode<Hurtbox>("Hurtbox");
            var receiver = player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage");
            resources.Add(motor.Tuning);
            resources.Add(combat.Tuning);
            resources.Add(energy.Tuning);
            resources.Add(player.GetNode<PlayerPresentationCoordinator>("Presentation").Profile);
            CollectShapes(player, resources);
            actor = CreateActorForFixture(RangedWeaponRole.Laser, out ownedLeafScene);
            resources.UnionWith(FactoryResources(actor));
            actor.Name = encounterId.Replace('.', '_') + "_CarriedPrism" + trial;
            actor.Position = new Vector2(300f, 300f);
            actor.ConfigureEncounterEnvelope(Arena);
            actorHealth = actor.GetNode<HealthComponent>("Health");
            var floor = WorldRect("Floor", new Vector2(580f, 339f), new Vector2(1100f, 20f));
            CollectShapes(floor, resources);
            root.AddChild(floor);
            root.AddChild(player);
            fixture = new Fixture(root, actor, player, health, null, new List<Emission>(), resources, ownedLeafScene);
            playerId = player.GetInstanceId(); actorId = actor.GetInstanceId();
            hurtboxId = hurtbox.GetInstanceId(); receiverId = receiver.GetInstanceId();
            energy.EnergyFrameChanged += OnEnergy;
            combat.AttackStarted += OnAttack; combat.AttackConnected += OnConnected;
            shield.ShieldResolved += OnGuard;
            health.Damaged += OnDamaged; health.Died += OnDied; health.Healed += OnHealed;
            actorHealth.Damaged += OnActorDamaged;
            actor.ShotEmitted += OnShot; actor.AttackFrameChanged += OnWarning; actor.Defeated += OnDefeated;

            float hz = Engine.PhysicsTicksPerSecond;
            float fullJ = combat.Tuning.AttackStartupSeconds + combat.Tuning.AttackActiveSeconds + combat.Tuning.AttackRecoverySeconds;
            var settings = energy.Tuning.ToSettings();
            TestAssert.True(hz > 0f && fullJ > 0f && motor.Tuning.GroundDeceleration > 0f &&
                settings.MaxEnergy == 100f && settings.AttackCost == 12f && settings.RegenDelaySeconds == .55f &&
                settings.RegenPerSecond == 28f && settings.ShieldDrainPerSecond == 24f &&
                MathF.Abs(fullJ - .30f) < .0001f && MathF.Abs(actor.Settings.ActiveSeconds - .28f) < .0001f,
                "gallery carried-energy fixture requires actual saved J/regen/drain/beam tuning for its independent <=35 boundary");
            // One frozen global native budget: no retry, reset or progress extension.
            float boundSeconds = preparationAttacks * (fullJ + 2f / hz) + actor.Settings.TelegraphSeconds +
                actor.Settings.ActiveSeconds + actor.Settings.RecoverySeconds + 8f / hz;
            entryFrame = Engine.GetPhysicsFrames();
            deadline = entryFrame + (ulong)Math.Ceiling(boundSeconds * hz);
            GD.Print($"GALLERY_LASER_CARRIED_DEADLINE trial={trial} entry={entryFrame} deadline={deadline} " +
                $"budget_frames={deadline - entryFrame} prep_J={preparationAttacks} full_J={fullJ:R} " +
                $"warning={actor.Settings.TelegraphSeconds:R} active={actor.Settings.ActiveSeconds:R} " +
                $"recovery={actor.Settings.RecoverySeconds:R} frozen=true");
            tree.Root.AddChild(root);
            driver = KeyboardRouteInputDriver.Begin(player);
            ulong lastInputSample = 0;

            string Snapshot() => $"trial={trial} physics={Engine.GetPhysicsFrames()}/{deadline} sample={input.CurrentFrameSequence} " +
                $"player={player.GlobalPosition} velocity={player.Velocity} actor={actor.GlobalPosition} " +
                $"state={actor.CurrentFrame.State} phase={actor.CurrentFrame.AttackTimeline.Phase} " +
                $"energy_raw={latestEnergy.State.Energy:R} heat={latestEnergy.State.Heat:R} " +
                $"regen_delay={latestEnergy.State.RegenDelayRemaining:R} cool_delay={latestEnergy.State.HeatCoolDelayRemaining:R} " +
                $"overheat={latestEnergy.State.Overheated} decision={lastDecision} " +
                $"L_raw={Input.IsPhysicalKeyPressed(Key.L)} L_held={input.Current.ShieldHeld} L_active={shield.IsActive} " +
                $"can_begin={shield.CanBeginGuard} hp={health.CurrentHealth} damaged={damaged} J={attacks.Count} " +
                $"shots={shots.Count} guards={guards.Count} shot_receipt=" +
                (shots.Count == 0 ? "none" : $"actor:{shots[0].ActorId},local_damage_ordinal:{shots[0].LocalDamageOrdinal}," +
                    $"timeline:{shots[0].TimelineId},physics:{shots[0].Frame},origin:{shots[0].Origin}," +
                    $"energy:{shots[0].Energy.Energy:R},can_begin:{shots[0].CanBegin}");
            void Check(bool condition, string claim) => TestAssert.True(condition, claim + "; " + Snapshot());
            bool Supported()
            {
                if (!player.IsOnFloor() || MathF.Abs(player.GlobalPosition.Y + 36f - 329f) > .2f ||
                    player.GlobalPosition.X - 20f < 30f || player.GlobalPosition.X + 20f > 1130f) return false;
                for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                {
                    using KinematicCollision2D contact = player.GetSlideCollision(index);
                    if (contact.GetCollider() == floor && contact.GetNormal().Y < -.7f) return true;
                }
                return false;
            }
            async Task Tick(bool support = true)
            {
                await Frames(tree, 1);
                Check(Engine.GetPhysicsFrames() <= deadline, "GALLERY_LASER_FIXED_NATIVE_DEADLINE");
                Check(driver.ObservedFrames == 0 || input.CurrentFrameSequence > lastInputSample,
                    "each gallery sample is a freshly native-processed input frame");
                lastInputSample = input.CurrentFrameSequence;
                driver.ObserveFrame();
                if (shots.Count > 0 && !beamReceiptPrinted)
                {
                    beamReceiptPrinted = true;
                    GD.Print("GALLERY_LASER_ACTUAL_BEAM_RECEIPT normal_saved_receiver=true incoming_request_observed=false " + Snapshot());
                }
                if (shots.Count > 0 && !budgetObserved)
                {
                    Budget(shots[0].Energy, 0f, shots[0].CanBegin, "first_actual_beam_event");
                    budgetObserved = true;
                }
                Check(health.CurrentHealth == 5 && damaged == 0 && died == 0 && healed == 0,
                    trial == "LOW8J" && shots.Count > 0 ? "GALLERY_LASER_LOW_ACTUAL_BEAM_HP5_RED" : "gallery native HP5/no rescue");
                if (energyObserved)
                    Check(float.IsFinite(latestEnergy.State.Energy) && float.IsFinite(latestEnergy.State.Heat) &&
                        MathF.Abs(latestEnergy.State.Energy - energy.EnergyRatio * settings.MaxEnergy) < .001f &&
                        !latestEnergy.State.Overheated && latestEnergy.State.RegenDelayRemaining >= 0f &&
                        latestEnergy.State.HeatCoolDelayRemaining >= 0f,
                        "actual EnergyFrame.State agrees with native energy/heat/delay state without setters");
                Check(player.GetInstanceId() == playerId && actor.GetInstanceId() == actorId &&
                    player.SceneFilePath == "res://scenes/player/Player.tscn" && actor is PrismLaserGunner &&
                    actor.SceneFilePath == "res://scenes/enemies/PrismLaserGunner.tscn" &&
                    hurtbox.GetInstanceId() == hurtboxId && receiver.GetInstanceId() == receiverId &&
                    hurtbox.DamageReceiverPath.ToString() == "../GuardedDamage" &&
                    ReferenceEquals(hurtbox.ResolveDamageReceiver(), receiver) &&
                    motor.IsPhysicsProcessing() && input.IsPhysicsProcessing() && combat.IsPhysicsProcessing() &&
                    shield.IsPhysicsProcessing() && energy.IsPhysicsProcessing() && health.MaxHealth == 5 &&
                    motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0 && driver.ClawEdges == 0 &&
                    !input.Current.JumpPressed && !input.Current.DashPressed && !input.Current.ClawPressed &&
                    !Input.IsPhysicalKeyPressed(Key.K) && driver.ObservedMouseMotionSamples == 0 &&
                    (!support || Supported()), "gallery preserves saved identity, ordinary receiver, live native components and full support");
            }
            PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger Danger() =>
                PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(root, encounterId, actor, false, leaves);
            bool BeamCoversPlayer(AttackShapeFrame shape, Vector2 muzzle, Vector2 target)
            {
                Vector2 axis = Vector2.Down.Rotated(shape.RotationRadians);
                float halfLength = shape.Extent.Y - shape.Extent.X;
                Vector2 start = shape.Origin - axis * halfLength;
                Vector2 offset = target - start;
                return halfLength > 0f && start.DistanceTo(muzzle) < .02f && axis.X > .99f &&
                    offset.Dot(axis) > 0f && offset.Dot(axis) < 2f * halfLength &&
                    MathF.Abs(offset.Cross(axis)) <= shape.Extent.X;
            }
            void Budget(PlayerEnergyState state, float remainingWarning, bool canBegin, string boundary)
            {
                float required = (MathF.Max(0f, remainingWarning) + actor.Settings.ActiveSeconds + 4f / hz) * settings.ShieldDrainPerSecond;
                Check(energyObserved && !state.Overheated && canBegin && state.Energy >= required &&
                    (trial != "LOW8J" || state.Energy <= 35f),
                    $"GALLERY_LASER_CARRIED_BUDGET boundary={boundary} actual_E={state.Energy:R} required={required:R} literal_low_max=35");
                GD.Print($"GALLERY_LASER_CARRIED_BUDGET_OK trial={trial} boundary={boundary} actual_E={state.Energy:R} " +
                    $"required={required:R} remaining_warning={remainingWarning:R} heat={state.Heat:R} " +
                    $"regen_delay={state.RegenDelayRemaining:R} cool_delay={state.HeatCoolDelayRemaining:R}");
            }
            await Tick(false);
            await Tick();
            Check(player.Position.DistanceTo(new Vector2(540f, 293f)) < .2f, "saved Player mounts at x540 with feet329");
            driver.Command(InputActions.MoveLeft, true);
            await Tick();
            Check(Input.IsPhysicalKeyPressed(Key.A) && input.Current.MoveAxis < 0f &&
                input.KeyboardAimDirection.Dot(Vector2.Left) > .99f, "one actual native A establishes inward keyboard aim");
            driver.Command(InputActions.MoveLeft, false);
            do { await Tick(); } while (MathF.Abs(player.Velocity.X) > .001f);
            Vector2 preparedPosition = player.GlobalPosition;
            for (int preparation = 0; preparation < preparationAttacks; preparation++)
            {
                int before = attacks.Count;
                driver.Command(InputActions.PrimaryAction, true);
                do { await Tick(); } while (attacks.Count == before);
                driver.Command(InputActions.PrimaryAction, false);
                Check(attacks.Count == before + 1 && attacks[^1].PhysicalJ,
                    "each preparation is exactly one accepted physical J, released after its native callback");
                while (combat.IsAttacking) await Tick();
                Check(input.Current.MoveAxis == 0f && player.GlobalPosition.DistanceTo(preparedPosition) < .2f &&
                    !input.Current.ShieldHeld && !shield.IsActive && !Input.IsPhysicalKeyPressed(Key.L),
                    "full harmless J completes without movement or pre-drain while the actor remains unmounted");
            }
            Check(attacks.Count == preparationAttacks && driver.AttackEdges == preparationAttacks &&
                connected == 0 && energyObserved && !latestEnergy.State.Overheated &&
                MathF.Abs(attacks[^1].Spent.Energy - (preparationAttacks == 4 ? 52f : 4f)) < .01f &&
                MathF.Abs(latestEnergy.State.RegenDelayRemaining - (.55f - fullJ)) <= 3f / hz &&
                (trial != "LOW8J" || latestEnergy.State.Energy < 6f),
                "four/eight actual full J retain real spent energy and partially consumed .55 regen delay before actor mount");
            GD.Print("GALLERY_LASER_NATIVE_PREPARATION_OK " + Snapshot());
            root.AddChild(actor);
            do { await Tick(); } while (Danger().Phase != AttackPhase.Telegraph);
            ulong warningTimeline = actor.CurrentFrame.AttackTimeline.AttackId;
            Check(warnings.Count > 0 && warningTimeline != 0 && actorHealth.CurrentHealth == 2,
                "actual saved Prism begins typed warning after native carried-energy preparation");

            int earlySamples = 0, activeSamples = 0, recoveryReleaseSamples = 0;
            bool sawActive = false, sawRecovery = false;
            while (!sawRecovery || recoveryReleaseSamples < 3 || actor.CurrentFrame.State == RangedWeaponState.Recovery)
            {
                var danger = Danger();
                Check(attacks.Count == preparationAttacks && connected == 0 && actorDamaged == 0 && defeated == 0 &&
                    actorHealth.CurrentHealth == 2 && player.GlobalPosition.DistanceTo(preparedPosition) < .2f &&
                    input.Current.MoveAxis == 0f && !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                    !Input.IsPhysicalKeyPressed(Key.J), "one gallery beam cycle has no extra J, hit, defeat or movement");
                bool early = trial == "HIGH4J" && earlySamples < 8;
                if (early)
                    Check(danger.Threat && danger.SourceActorId == actorId && danger.Phase == AttackPhase.Telegraph &&
                        danger.RemainingSeconds > 8f / hz + 2f / hz, "HIGH samples first eight genuinely early native warning frames");
                lastDecision = PrologueCanyonGeneratedMainRouteTests.Task10GalleryLaserDefendWindow(
                    combat, energy, shield, danger, false, 30 + (int)(Engine.GetPhysicsFrames() - entryFrame), 0);
                if (lastDecision && !budgetObserved)
                {
                    Budget(latestEnergy.State, danger.Phase == AttackPhase.Telegraph ? danger.RemainingSeconds : 0f,
                        shield.CanBeginGuard, "first_actual_L_command");
                    budgetObserved = true;
                    admitted = true;
                }
                driver.Command(InputActions.KeyboardShield, lastDecision);
                await Tick(); // Exactly one ordinary native sample per shared-policy decision.
                if (early)
                {
                    Check(!Input.IsPhysicalKeyPressed(Key.L) && !input.Current.ShieldHeld && !shield.IsActive,
                        "GALLERY_LASER_HIGH_EARLY_WARNING_L_RED first eight native early-warning samples leave L raw/held/active false");
                    earlySamples++;
                }
                var actual = Danger();
                if (actual.Phase == AttackPhase.Active)
                {
                    sawActive = true;
                    activeSamples++;
                    Check(admitted && Input.IsPhysicalKeyPressed(Key.L) && input.Current.ShieldHeld && shield.IsActive &&
                        actual.SourceActorId == actorId && actual.AttackId == warningTimeline &&
                        shield.GuardDirection.Dot(Vector2.Left) > .99f,
                        "gallery full actual Active retains admitted L raw/held/active and inward guard");
                }
                if (sawActive && actor.CurrentFrame.State == RangedWeaponState.Recovery)
                {
                    sawRecovery = true;
                    if (!Input.IsPhysicalKeyPressed(Key.L) && !input.Current.ShieldHeld && !shield.IsActive)
                        recoveryReleaseSamples++;
                    else recoveryReleaseSamples = 0;
                    Check(recoveryReleaseSamples > 0 || actor.CurrentFrame.AttackTimeline.PhaseRemainingSeconds >=
                        actor.Settings.RecoverySeconds - 3f / hz,
                        "gallery L releases within three native Recovery samples, not an arbitrary held tail");
                }
            }
            Check(budgetObserved && sawActive && sawRecovery && activeSamples >= (int)MathF.Floor(actor.Settings.ActiveSeconds * hz) - 1 &&
                recoveryReleaseSamples >= 3 && shots.Count == 1 && warnings.All(w => w.ActorId == actorId &&
                    w.TimelineId == warningTimeline && w.Kind == EnemyAttackKind.PrismLaserBeam) &&
                shots[0].ActorId == actorId && shots[0].LocalDamageOrdinal != 0 && shots[0].TimelineId == warningTimeline &&
                shots[0].Kind == EnemyAttackKind.PrismLaserBeam && warnings[0].Frame < shots[0].Frame &&
                guards.Count == 1 && guards[0].Frame == shots[0].Frame && guards[0].ActorId == actorId &&
                guards[0].TimelineId == warningTimeline && guards[0].Result.Guarded && guards[0].Result.DamageAmount == 0 &&
                !guards[0].Result.SpendHeavyEnergy && !guards[0].Result.BrokeGuard && guards[0].Hp == 5 &&
                guards[0].RawL && guards[0].HeldL && guards[0].ActiveL &&
                guards[0].Beam.Timeline.Kind == EnemyAttackKind.PrismLaserBeam && guards[0].Beam.Timeline.Phase == AttackPhase.Active &&
                guards[0].Beam.Shape.Kind == AttackShapeKind.Capsule &&
                MathF.Abs(guards[0].Beam.Shape.Extent.X - actor.Settings.LaserHalfWidth) < .001f &&
                BeamCoversPlayer(guards[0].Beam.Shape, guards[0].Muzzle, guards[0].Player) &&
                guards[0].Muzzle.DistanceTo(shots[0].Origin) < .01f && guards[0].Muzzle.X < guards[0].Player.X &&
                guards[0].Direction.Normalized().Dot((guards[0].Muzzle - guards[0].Player).Normalized()) >=
                    Mathf.Cos(Mathf.DegToRad(PlayerShieldSettings.Approved.FrontArcDegrees * .5f)) &&
                energies.Any(e => e.Frame > attacks[^1].Frame && e.Frame < shots[0].Frame &&
                    e.Value.State.Energy > attacks[^1].Spent.Energy && e.Value.State.RegenDelayRemaining == 0f) &&
                attacks.Count == preparationAttacks && driver.AttackEdges == preparationAttacks && connected == 0 &&
                damaged == 0 && died == 0 && healed == 0 && actorDamaged == 0 && defeated == 0,
                "gallery native receipt: typed warning/actual source emission, same-physics normal guarded beam/HP5, full Active and released Recovery tail");
            GD.Print($"GALLERY_LASER_CARRIED_TRIAL_OK trial={trial} early_samples={earlySamples} active_samples={activeSamples} " +
                $"local_damage_ordinal={shots[0].LocalDamageOrdinal} timeline_attack_id={warningTimeline} " +
                $"same_physics_guard={guards[0].Frame} receiver=PlayerGuardedDamageReceiver incoming_request_observed=false " + Snapshot());
        }
        finally
        {
            if (energy != null) energy.EnergyFrameChanged -= OnEnergy;
            if (combat != null) { combat.AttackStarted -= OnAttack; combat.AttackConnected -= OnConnected; }
            if (shield != null) shield.ShieldResolved -= OnGuard;
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; health.Healed -= OnHealed; }
            if (actorHealth != null) actorHealth.Damaged -= OnActorDamaged;
            if (actor != null) { actor.ShotEmitted -= OnShot; actor.AttackFrameChanged -= OnWarning; actor.Defeated -= OnDefeated; }
            driver?.Dispose();
            if (actor != null && actor.GetParent() == null) actor.Free();
            if (player != null && player.GetParent() == null) player.Free();
            if (fixture != null) await DisposeFixture(tree, fixture);
            else
            {
                root.Free();
                foreach (Resource resource in resources) resource?.Dispose();
                ownedLeafScene?.Dispose();
            }
        }
    }

    // Intended to catch three real-route mutations: whole-warning L, the .35
    // guard gate, and parking every guarded approach. This is a controlled fixture,
    // not an actual Steam encounter-group/batch or full-route completion claim.
    public static async Task RunSteamMissileMovingPolicy(SceneTree tree)
    {
        if (_suiteRunning) throw new InvalidOperationException("Ranged actor suites must run serially.");
        _suiteRunning = true;
        _loadSavedScenes = true;
        try
        {
            var earlyBaseline = new List<SteamMissileCompletedStep>(8);
            await SteamMissileMovingTrial(tree, preparationAttacks: 4, earlyTrace: earlyBaseline);
            await SteamMissileMovingTrial(tree, preparationAttacks: 7);
            await SteamMissileMovingTrial(tree, preparationAttacks: 4, editRetiredAuthoring: true,
                expectedEarly: earlyBaseline);
            GD.Print("STEAM_MISSILE_MOVING_POLICY_OK trials=HIGH4J,LOW7J,HIGH4J_RETIRED_AUTHOR real_motor=true " +
                "native_keyboard=true hp=5 group_batch_claim=false whole_goal=false");
        }
        finally { _loadSavedScenes = false; _suiteRunning = false; }
    }

    private readonly record struct SteamMissileCompletedStep(ulong Physics, ulong Sequence,
        Vector2 Position, Vector2 Velocity, float RawAxis, float Axis,
        bool PhysicalA, bool PhysicalD, bool PhysicalL, bool ShieldActive, bool OnFloor);

    private static async Task SteamMissileMovingTrial(SceneTree tree, int preparationAttacks,
        bool editRetiredAuthoring = false, List<SteamMissileCompletedStep> earlyTrace = null,
        IReadOnlyList<SteamMissileCompletedStep> expectedEarly = null)
    {
        string trial = preparationAttacks == 4 ? "HIGH" : "LOW";
        const string encounterId = "canyon.v2.encounter.steam-valves";
        var root = new Node2D { Name = "SteamMovingMissile" + trial };
        var resources = new HashSet<Resource>();
        RangedWeaponEnemy actor = null;
        PackedScene ownedLeafScene = null;
        Fixture fixture = null;
        KeyboardRouteInputDriver driver = null;
        PlayerCombat combat = null;
        PlayerShieldController shield = null;
        HealthComponent health = null, actorHealth = null;
        CharacterBody2D player = null;
        PlayerInputReader input = null;
        PlayerEnergyController energy = null;
        Knight.Config.PlayerTuning retiredAuthoring = null;
        float originalLegacyDeceleration = 0f;
        PlayerMotionSettings appliedMotion = null;
        MobilityModuleSettings appliedModule = default;
        ulong appliedConfigurationEpoch = 0, appliedMotionRevision = 0;
        SteamMissileCompletedStep completedStep = default;
        var attacks = new List<(ulong Id, ulong Frame, bool PhysicalJ, int Direction)>();
        var connections = new List<(ulong AttackId, ulong HurtboxId, int Remaining)>();
        var shots = new List<(ulong ActorId, ulong Id, ulong Frame, EnemyAttackKind Kind, Vector2 Origin)>();
        var guards = new List<(PlayerShieldResult Result, ulong Frame, ulong AttackId, Vector2 Source, Vector2 Player,
            Vector2 Direction, float Energy, bool ActiveBlast, int Hp)>();
        var remainingHealth = new List<int>();
        var leaves = new List<AttackTelegraphVisual>(8);
        var guardLeaves = new List<AttackTelegraphVisual>(8);
        int damaged = 0, died = 0, healed = 0, defeated = 0;
        bool typedWarning = false;
        ulong deadline = 0, entryFrame = 0, playerId = 0, actorId = 0, hurtboxId = 0, firstWarningFrame = 0;

        // Callback bodies only buffer values. Assertions/commands belong to the
        // awaited trial, never to synchronous combat/health/presentation delivery.
        void OnAttack(ulong id, int direction) => attacks.Add(
            (id, Engine.GetPhysicsFrames(), Input.IsPhysicalKeyPressed(Key.J), direction));
        void OnConnected(Hurtbox target) => connections.Add(
            (attacks.Count == 0 ? 0 : attacks[^1].Id, target.GetInstanceId(), actorHealth.CurrentHealth));
        void OnShot(ulong id, Vector2 origin) => shots.Add(
            (actor.GetInstanceId(), id, Engine.GetPhysicsFrames(), actor.CurrentFrame.AttackTimeline.Kind, origin));
        void OnWarning(AttackPresentationFrame frame)
        {
            if (frame.Timeline.Kind == EnemyAttackKind.FurnaceMissileShot &&
                frame.Timeline.Phase == AttackPhase.Telegraph)
            {
                typedWarning = true;
                if (firstWarningFrame == 0) firstWarningFrame = Engine.GetPhysicsFrames();
            }
        }
        void OnGuard(PlayerShieldResult result, ulong transition)
        {
            actor.AppendAuthorizedAttackLeaves(guardLeaves);
            Vector2 source = Vector2.Zero;
            bool activeBlast = false;
            ulong attackId = 0;
            foreach (AttackTelegraphVisual leaf in guardLeaves)
            {
                AttackPresentationFrame frame = leaf.CurrentFrame;
                if (frame.Timeline.Kind != EnemyAttackKind.FurnaceMissileShot ||
                    frame.Timeline.Phase != AttackPhase.Active || frame.Shape.Kind != AttackShapeKind.Circle ||
                    frame.Shape.Extent.X < actor.Settings.ExplosionRadius - .01f) continue;
                source = frame.Shape.Origin;
                activeBlast = true;
                attackId = frame.Timeline.AttackId;
            }
            guardLeaves.Clear();
            guards.Add((result, Engine.GetPhysicsFrames(), attackId, source, player.GlobalPosition,
                shield.GuardDirection, energy.EnergyRatio, activeBlast, health.CurrentHealth));
        }
        void OnDamaged(int remaining, Vector2 impulse) => damaged++;
        void OnDied() => died++;
        void OnHealed(int restored, int remaining) => healed++;
        void OnActorDamaged(int remaining, Vector2 impulse) => remainingHealth.Add(remaining);
        void OnDefeated() => defeated++;

        try
        {
            PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn",
                cacheMode: ResourceLoader.CacheMode.Ignore);
            resources.Add(playerScene);
            player = playerScene.Instantiate<CharacterBody2D>();
            player.Name = "ActualPlayer";
            player.Position = new Vector2(300f, 293f); // Full saved capsule feet=329, not inside the floor.
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.AddToGroup("player");
            var motor = player.GetNode<PlayerMotor>("Motor");
            input = player.GetNode<PlayerInputReader>("Input");
            combat = player.GetNode<PlayerCombat>("Combat");
            energy = player.GetNode<PlayerEnergyController>("Energy");
            shield = player.GetNode<PlayerShieldController>("Shield");
            health = player.GetNode<HealthComponent>("Health");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            resources.Add(motor.Tuning);
            resources.Add(energy.Tuning);
            resources.Add(player.GetNode<PlayerPresentationCoordinator>("Presentation").Profile);
            CollectShapes(player, resources);
            actor = CreateActorForFixture(RangedWeaponRole.Missile, out ownedLeafScene);
            resources.UnionWith(FactoryResources(actor));
            actor.Name = encounterId.Replace('.', '_') + "_MovingFurnace" + trial;
            actor.Position = new Vector2(740f, 300f);
            actor.ConfigureEncounterEnvelope(Arena);
            actorHealth = actor.GetNode<HealthComponent>("Health");
            var floor = WorldRect("Floor", new Vector2(580f, 339f), new Vector2(1100f, 20f));
            CollectShapes(floor, resources);
            root.AddChild(floor);
            root.AddChild(player);
            root.AddChild(new SteamMissilePostMotorObserver { Name = "CompletedMissileMotorStep",
                ProcessPhysicsPriority = 100, Observe = () => completedStep = new(
                    Engine.GetPhysicsFrames(), input.CurrentFrameSequence, player.GlobalPosition, player.Velocity,
                    input.RawFrame.MoveAxis, input.Current.MoveAxis, Input.IsPhysicalKeyPressed(Key.A),
                    Input.IsPhysicalKeyPressed(Key.D), Input.IsPhysicalKeyPressed(Key.L), shield.IsActive,
                    player.IsOnFloor()) });
            fixture = new Fixture(root, actor, player, health, null, new List<Emission>(), resources, ownedLeafScene);
            playerId = player.GetInstanceId(); actorId = actor.GetInstanceId();
            hurtboxId = actor.GetNode<Hurtbox>("Hurtbox").GetInstanceId();
            combat.AttackStarted += OnAttack;
            combat.AttackConnected += OnConnected;
            shield.ShieldResolved += OnGuard;
            health.Damaged += OnDamaged; health.Died += OnDied; health.Healed += OnHealed;
            actorHealth.Damaged += OnActorDamaged;
            actor.ShotEmitted += OnShot; actor.AttackFrameChanged += OnWarning; actor.Defeated += OnDefeated;

            float hz = Engine.PhysicsTicksPerSecond;
            float fullAttack = combat.Tuning.AttackStartupSeconds + combat.Tuning.AttackActiveSeconds +
                combat.Tuning.AttackRecoverySeconds;
            float completeWindow = fullAttack + 8f / hz; // Six safety ticks + sample/command lead.
            TestAssert.True(fullAttack > 0f && energy.Tuning.RegenPerSecond > 0f &&
                energy.Tuning.HeatCoolPerSecond > 0f && motor.Tuning.GroundDeceleration > 0f,
                "Steam native deadline requires valid saved attack/regen/deceleration tuning");
            // Freeze once BEFORE mounting: preparation; the entire approach at
            // shield speed incl. acceleration; maximum native missile flight;
            // full empty-to-full natural regen/cooling; one cycle per real J;
            // complete attacks and their six-tick margins. No progress extensions.
            float cycle = actor.Settings.TelegraphSeconds + actor.Settings.ActiveSeconds + actor.Settings.RecoverySeconds;
            float boundSeconds = preparationAttacks * (fullAttack + 2f / hz) +
                440f / (motor.Tuning.MaxRunSpeed * PlayerShieldSettings.Approved.MovementScale) +
                motor.Tuning.MaxRunSpeed / motor.Tuning.GroundAcceleration +
                MathF.Min(actor.Settings.MissileMaximumAgeSeconds, actor.Settings.MaximumTravel / actor.Settings.ProjectileSpeed) +
                energy.Tuning.RegenDelaySeconds + energy.Tuning.MaxEnergy / energy.Tuning.RegenPerSecond +
                energy.Tuning.HeatCoolDelaySeconds + 100f / energy.Tuning.HeatCoolPerSecond +
                2f * cycle + 2f * completeWindow + 8f / hz;
            entryFrame = Engine.GetPhysicsFrames();
            deadline = entryFrame + (ulong)Math.Ceiling(boundSeconds * hz);
            GD.Print($"STEAM_MISSILE_MOVING_DEADLINE trial={trial} entry={entryFrame} deadline={deadline} " +
                $"budget_frames={deadline - entryFrame} prep_J={preparationAttacks} full_J={fullAttack:R} " +
                $"warning={actor.Settings.TelegraphSeconds:R} speed={actor.Settings.ProjectileSpeed:R} " +
                $"regen={energy.Tuning.RegenPerSecond:R} frozen=true");
            tree.Root.AddChild(root);
            driver = KeyboardRouteInputDriver.Begin(player);
            ulong lastInputSample = 0;

            string Snapshot() => $"trial={trial} physics={Engine.GetPhysicsFrames()}/{deadline} " +
                $"sample={input.CurrentFrameSequence} player={player.GlobalPosition} velocity={player.Velocity} " +
                $"actor={actor.GlobalPosition} gap={actor.GlobalPosition.X - player.GlobalPosition.X:R} " +
                $"state={actor.CurrentFrame.State} phase={actor.CurrentFrame.AttackTimeline.Phase} " +
                $"energy={energy.EnergyRatio:R} L={input.Current.ShieldHeld}/{shield.IsActive} " +
                $"D={input.Current.MoveAxis:R} hp={health.CurrentHealth} J={attacks.Count - preparationAttacks} " +
                $"shots={shots.Count} guards={guards.Count} enemy_hp={actorHealth.CurrentHealth}";
            bool Supported()
            {
                if (!player.IsOnFloor() || MathF.Abs(player.GlobalPosition.Y + 36f - 329f) > .2f ||
                    player.GlobalPosition.X - 20f < 30f || player.GlobalPosition.X + 20f > 1130f) return false;
                for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                    if (player.GetSlideCollision(index).GetCollider() == floor) return true;
                return false;
            }
            void Check(bool condition, string claim) => TestAssert.True(condition, claim + "; " + Snapshot());
            async Task Tick(bool support = true)
            {
                await Frames(tree, 1);
                Check(Engine.GetPhysicsFrames() <= deadline, "STEAM_MISSILE_FIXED_NATIVE_DEADLINE");
                Check(driver.ObservedFrames == 0 || input.CurrentFrameSequence > lastInputSample,
                    "each awaited safety/approach sample is a newly native-processed Input frame");
                lastInputSample = input.CurrentFrameSequence;
                driver.ObserveFrame();
                if (appliedMotion != null)
                    Check(ReferenceEquals(appliedMotion, motor.MotionSettings) &&
                        appliedModule == motor.ThrusterState.Module &&
                        appliedConfigurationEpoch == motor.ThrusterState.ConfigurationEpoch &&
                        appliedMotionRevision == motor.MotionAuthorityRevision && !motor.IsDashing,
                        "Steam trial keeps the actual applied motion/module/authority unchanged, including after an author-only hot edit");
                Check(player.GetInstanceId() == playerId && actor.GetInstanceId() == actorId &&
                    player.SceneFilePath == "res://scenes/player/Player.tscn" &&
                    actor.SceneFilePath == "res://scenes/enemies/FurnaceMissileGunner.tscn" &&
                    motor.IsPhysicsProcessing() && input.IsPhysicsProcessing() && combat.IsPhysicsProcessing() &&
                    shield.IsPhysicsProcessing() && energy.IsPhysicsProcessing() &&
                    health.MaxHealth == 5 && health.CurrentHealth == 5 && damaged == 0 && died == 0 && healed == 0 &&
                    motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0 && driver.ClawEdges == 0 &&
                    !input.Current.JumpPressed && !input.Current.DashPressed && !input.Current.ClawPressed &&
                    driver.ObservedMouseMotionSamples == 0 && (!support || Supported()),
                    "Steam moving fixture preserves saved identity/HP5/native support and live components without rescue");
            }
            PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger Danger() =>
                PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(root, encounterId, actor, false, leaves);
            bool StrikeRange() => actor.GlobalPosition.X - player.GlobalPosition.X is >= 8f and <= 64f &&
                MathF.Abs(actor.GlobalPosition.Y - player.GlobalPosition.Y) <= 46f;
            PrologueCanyonGeneratedMainRouteTests.Task10SteamMissileDecision Decision(
                PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger danger) =>
                PrologueCanyonGeneratedMainRouteTests.Task10SteamMissileKeyboardDecision(player, actor,
                    combat, energy, shield, danger, StrikeRange(), 30 + (int)(Engine.GetPhysicsFrames() - entryFrame), 0, leaves);
            void Drive(PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger danger,
                PrologueCanyonGeneratedMainRouteTests.Task10SteamMissileDecision decision)
            {
                float gap = actor.GlobalPosition.X - player.GlobalPosition.X;
                Check(Supported() && ReferenceEquals(appliedMotion, motor.MotionSettings) &&
                    appliedModule == motor.ThrusterState.Module && !motor.IsDashing,
                    "Steam ground forecast uses only its actual supported, unchanged free-motion domain");
                ulong now = Engine.GetPhysicsFrames();
                Check(now < deadline, "Steam forecast cannot borrow beyond the original frozen deadline");
                int remaining = checked((int)Math.Min((ulong)int.MaxValue, deadline - now));
                var stop = FreeMotionForecast.Neutral(player.Velocity.X, true, motor.MotionSettings,
                    motor.ThrusterState.Module, 1d / hz, remaining, 2f);
                Check(stop.SpeedThresholdReached, "Steam ground Neutral can stop within only the original remaining budget");
                float braking = MathF.Max(0f, stop.NetDistance) + MathF.Abs(player.Velocity.X) * 2f / hz;
                bool forward = gap > 64f && gap - 60f > braking &&
                    (!decision.Defend || decision.AllowShieldApproach);
                driver.Command(InputActions.MoveLeft, false); // Never aim away with A while a right-hand missile is incoming.
                driver.Command(InputActions.MoveRight, forward);
                driver.PointerButton(MouseButton.Right, decision.Defend, player,
                    danger.Threat ? danger.GuardSource : actor.GlobalPosition);
            }
            await Tick(support: false);
            await Tick();
            Check(health.CurrentHealth == 5 && player.Position.DistanceTo(new Vector2(300f, 293f)) < .2f,
                "saved Player mounts at nonpenetrating feet=329 with unchanged five HP");
            appliedMotion = motor.MotionSettings;
            appliedModule = motor.ThrusterState.Module;
            appliedConfigurationEpoch = motor.ThrusterState.ConfigurationEpoch;
            appliedMotionRevision = motor.MotionAuthorityRevision;
            for (int preparation = 0; preparation < preparationAttacks; preparation++)
            {
                int before = attacks.Count;
                driver.Command(InputActions.PrimaryAction, true);
                do { await Tick(); } while (attacks.Count == before);
                driver.Command(InputActions.PrimaryAction, false);
                Check(attacks.Count == before + 1 && attacks[^1].PhysicalJ,
                    "each preparation is exactly one accepted fresh physical J");
                while (combat.IsAttacking) await Tick();
            }
            Check(attacks.Count == preparationAttacks && connections.Count == 0 &&
                (trial == "HIGH" ? energy.EnergyRatio > .35f && energy.EnergyRatio < .6f : energy.EnergyRatio < .2f),
                "four/seven real J spend saved energy before the Furnace is mounted");
            root.AddChild(actor);
            do { await Tick(); } while (Danger().Phase != AttackPhase.Telegraph);
            Check(typedWarning && actorHealth.CurrentHealth == 2 && actor.GetNode<Node2D>("Muzzle").GlobalPosition.Y is > 291.8f and < 292.2f,
                "actual saved Furnace has its typed warning, two HP and real y292 muzzle");

            if (editRetiredAuthoring)
            {
                Check(preparationAttacks == 4 && expectedEarly?.Count == 8,
                    "retired-author negative control reuses the unchanged eight-sample HIGH behavior contract");
                retiredAuthoring = motor.Tuning;
                originalLegacyDeceleration = retiredAuthoring.GroundDeceleration;
                // Deliberately NOT a valid new saved migration configuration.
                // Ready has already frozen the motion/module; only obsolete
                // author data changes. Never RequestReady or equip this value.
                retiredAuthoring.GroundDeceleration = 1f;
                GD.Print($"STEAM_MISSILE_RETIRED_AUTHOR_EDIT resource_id={retiredAuthoring.GetInstanceId()} " +
                    $"before={originalLegacyDeceleration:R} after={retiredAuthoring.GroundDeceleration:R} " +
                    $"applied_ground_friction={appliedModule.GroundFriction:R} " +
                    $"configuration_epoch={appliedConfigurationEpoch} motion_revision={appliedMotionRevision} " +
                    "after_ready=true valid_saved_configuration=false");
            }

            if (trial == "HIGH")
            {
                float gapBefore = actor.GlobalPosition.X - player.GlobalPosition.X;
                Vector2 completedStart = completedStep.Position;
                int forwardSamples = 0;
                for (int sample = 0; sample < 8; sample++)
                {
                    var danger = Danger();
                    Check(danger.Threat && danger.SourceActorId == actorId && danger.Phase == AttackPhase.Telegraph &&
                        danger.RemainingSeconds > 8f / hz, "HIGH samples a real early warning, not the late guard window");
                    ulong issuedPhysics = Engine.GetPhysicsFrames(), issuedSequence = input.CurrentFrameSequence;
                    Drive(danger, Decision(danger));
                    await Tick();
                    Check(completedStep.Physics >= issuedPhysics && completedStep.Sequence > issuedSequence &&
                        completedStep.Sequence == input.CurrentFrameSequence && completedStep.OnFloor,
                        "HIGH observer records a fresh completed Motor step, not PhysicsFrame's pre-Motor signal");
                    SteamMissileCompletedStep relative = completedStep with
                    { Position = completedStep.Position - completedStart };
                    earlyTrace?.Add(relative);
                    GD.Print($"STEAM_MISSILE_EARLY_COMPLETED trial={trial} retired_author_edit={editRetiredAuthoring} " +
                        $"index={sample} issue_physics={issuedPhysics} completed_physics={completedStep.Physics} " +
                        $"issue_sample={issuedSequence} completed_sample={completedStep.Sequence} " +
                        $"delta={relative.Position} velocity={relative.Velocity} raw_axis={relative.RawAxis:R} " +
                        $"axis={relative.Axis:R} A={relative.PhysicalA} D={relative.PhysicalD} " +
                        $"L={relative.PhysicalL}/{relative.ShieldActive}");
                    if (expectedEarly != null)
                    {
                        SteamMissileCompletedStep baseline = expectedEarly[sample];
                        Check(relative.RawAxis == baseline.RawAxis && relative.Axis == baseline.Axis &&
                            relative.PhysicalA == baseline.PhysicalA && relative.PhysicalD == baseline.PhysicalD &&
                            relative.PhysicalL == baseline.PhysicalL && relative.ShieldActive == baseline.ShieldActive &&
                            relative.Position.DistanceTo(baseline.Position) < .001f &&
                            relative.Velocity.DistanceTo(baseline.Velocity) < .001f,
                            "STEAM_MISSILE_RETIRED_AUTHOR_BEHAVIOR_RED obsolete author edits must not alter actual eight-sample keyboard/Motor trajectory");
                    }
                    if (input.Current.MoveAxis > 0f && Input.IsPhysicalKeyPressed(Key.D) &&
                        !input.Current.ShieldHeld && !shield.IsActive && !Input.IsPhysicalKeyPressed(Key.L) && player.Velocity.X > 0f)
                        forwardSamples++;
                }
                float reduction = gapBefore - (actor.GlobalPosition.X - player.GlobalPosition.X);
                Check(forwardSamples == 8 && reduction >= 12f && shots.Count == 0 &&
                    attacks.Count == preparationAttacks && actorHealth.CurrentHealth == 2,
                    $"STEAM_MISSILE_EARLY_WARNING_APPROACH_RED samples={forwardSamples}/8 gap_reduction={reduction:R} expected>=12");
                GD.Print($"STEAM_MISSILE_EARLY_WARNING_APPROACH_OK samples=8 gap_reduction={reduction:R} " + Snapshot());
                return;
            }

            bool lowActiveApproach = false;
            while (actorHealth.CurrentHealth > 0)
            {
                var danger = Danger();
                var decision = Decision(danger);
                Drive(danger, decision);
                bool lowActiveCandidate = !lowActiveApproach && danger.Threat && danger.Phase == AttackPhase.Active &&
                    danger.SourceActorId == actorId && energy.EnergyRatio <= .35f &&
                    actor.GlobalPosition.X - player.GlobalPosition.X > 64f;
                if (guards.Any(guard => guard.Result.Guarded) && shots.Count > 0 && StrikeRange() &&
                    decision.CanCounter && !combat.IsAttacking && modes.CanAffordAttack)
                {
                    Check(danger.SafeCounterSeconds >= completeWindow &&
                        energy.EnergyRatio * energy.Tuning.MaxEnergy >= 2f * energy.Tuning.AttackCost &&
                        LiveProjectiles(actor).Count == 0,
                        "STEAM_MISSILE_COMPLETE_COUNTER_WINDOW_RED initial admission covers full J plus six native safety samples");
                    driver.Command(InputActions.MoveRight, false);
                    driver.PointerButton(MouseButton.Right, false, player, actor.GlobalPosition);
                    do { await Tick(); } while (input.Current.ShieldHeld || shield.IsActive);
                    var freshDanger = Danger();
                    var freshDecision = Decision(freshDanger);
                    if (!freshDecision.CanCounter || !StrikeRange() || !modes.CanBeginAttack) continue;
                    Check(freshDanger.SafeCounterSeconds >= completeWindow && LiveProjectiles(actor).Count == 0,
                        "STEAM_MISSILE_FRESH_COUNTER_WINDOW_RED recheck actual danger after sampled L release before fresh J");
                    int beforeAttack = attacks.Count, beforeConnection = connections.Count;
                    int beforeHp = actorHealth.CurrentHealth;
                    driver.Command(InputActions.PrimaryAction, true);
                    do { await Tick(); } while (attacks.Count == beforeAttack);
                    driver.Command(InputActions.PrimaryAction, false);
                    Check(attacks.Count == beforeAttack + 1 && attacks[^1].PhysicalJ &&
                        attacks[^1].Direction == (int)AttackDirection.Horizontal && combat.LastAttackFacing == 1,
                        "LOW counter accepts exactly one new actual forward J, separate from preparation");
                    do
                    {
                        await Tick();
                        var duringAttack = Danger();
                        Check(!(duringAttack.Threat && duringAttack.Phase == AttackPhase.Active),
                            "complete J never overlaps unexpected authorized Active danger");
                    } while (combat.IsAttacking);
                    for (int margin = 0; margin < 6; margin++)
                    {
                        await Tick();
                        var marginDanger = Danger();
                        Check(!(marginDanger.Threat && marginDanger.Phase == AttackPhase.Active),
                            "six native post-J safety samples remain outside authorized Active danger");
                    }
                    Check(connections.Count == beforeConnection + 1 && connections[^1].AttackId == attacks[^1].Id &&
                        connections[^1].HurtboxId == hurtboxId && connections[^1].Remaining == beforeHp - 1 &&
                        actorHealth.CurrentHealth == beforeHp - 1,
                        "actual fresh J connects exactly the saved Furnace Hurtbox and real HP falls by one");
                    continue;
                }
                await Tick();
                if (lowActiveCandidate)
                {
                    var actual = Danger();
                    Vector2 sourceDelta = actual.GuardSource - player.GlobalPosition;
                    Check(input.Current.MoveAxis > 0f && Input.IsPhysicalKeyPressed(Key.D) &&
                        input.Current.ShieldHeld && Input.IsPhysicalKeyPressed(Key.L) && shield.IsActive &&
                        energy.EnergyRatio <= .35f && player.Velocity.X > 0f && actual.Threat &&
                        actual.Phase == AttackPhase.Active && actual.SourceActorId == actorId && sourceDelta.X > 0f &&
                        shield.GuardDirection.Normalized().Dot(sourceDelta.Normalized()) >=
                            Mathf.Cos(Mathf.DegToRad(PlayerShieldSettings.Approved.FrontArcDegrees * .5f)),
                        "STEAM_MISSILE_LOW_ACTIVE_SHIELD_APPROACH_RED actual <=.35 energy must retain D+L/active source-facing guard");
                    lowActiveApproach = true;
                }
            }
            Check(lowActiveApproach && typedWarning && shots.Count > 0 &&
                shots.All(shot => shot.ActorId == actorId && shot.Id != 0 && shot.Kind == EnemyAttackKind.FurnaceMissileShot) &&
                guards.Any(guard => guard.Result.Guarded && guard.Result.DamageAmount == 0 && guard.ActiveBlast &&
                    shots.Any(shot => shot.Id == guard.AttackId && shot.Frame <= guard.Frame && firstWarningFrame < shot.Frame) &&
                    guard.Hp == 5 && guard.Source.X > guard.Player.X && guard.Direction.Normalized().Dot(
                        (guard.Source - guard.Player).Normalized()) >=
                            Mathf.Cos(Mathf.DegToRad(PlayerShieldSettings.Approved.FrontArcDegrees * .5f))) &&
                attacks.Count == preparationAttacks + 2 && driver.AttackEdges == preparationAttacks + 2 &&
                attacks.Skip(preparationAttacks).Select(attack => attack.Id).Distinct().Count() == 2 &&
                remainingHealth.SequenceEqual(new[] { 1, 0 }) && connections.Count == 2 && defeated == 1,
                "LOW native receipts: typed warning/shot, real guarded explosion HP5, two distinct exact Hurtbox J, HP2->1->0/Defeated1");
            GD.Print("STEAM_MISSILE_LOW_ACTIVE_COUNTER_OK low_D_L=true exact_J_hits=2 enemy_hp=2,1,0 defeated=1 " + Snapshot());
        }
        finally
        {
            if (retiredAuthoring != null)
            {
                retiredAuthoring.GroundDeceleration = originalLegacyDeceleration;
                GD.Print($"STEAM_MISSILE_RETIRED_AUTHOR_RESTORED resource_id={retiredAuthoring.GetInstanceId()} " +
                    $"value={retiredAuthoring.GroundDeceleration:R}");
            }
            if (combat != null)
            {
                combat.AttackStarted -= OnAttack; combat.AttackConnected -= OnConnected;
            }
            if (shield != null) shield.ShieldResolved -= OnGuard;
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; health.Healed -= OnHealed; }
            if (actorHealth != null) actorHealth.Damaged -= OnActorDamaged;
            if (actor != null)
            {
                actor.ShotEmitted -= OnShot; actor.AttackFrameChanged -= OnWarning; actor.Defeated -= OnDefeated;
            }
            driver?.Dispose(); // Releases every owned physical key before native nodes leave the tree, including RED.
            if (actor != null && actor.GetParent() == null) actor.Free(); // Preparation can fail before actor mounting.
            if (player != null && player.GetParent() == null) player.Free();
            if (fixture != null) await DisposeFixture(tree, fixture);
            else
            {
                root.Free();
                foreach (Resource resource in resources) resource?.Dispose();
                ownedLeafScene?.Dispose();
            }
        }
    }

    // Actual saved actors + shared route decisions + physical-key adapter.
    // Motor is frozen only to isolate a real native shield contact, not to prove locomotion.
    public static async Task RunThreatPolicy(SceneTree tree)
    {
        if (_suiteRunning) throw new InvalidOperationException("Ranged actor suites must run serially.");
        _suiteRunning = true;
        _loadSavedScenes = true;
        try
        {
            Fixture laser = await CreateFixture(tree, RangedWeaponRole.Laser, "KeyboardLaserPolicy",
                ordinaryHealth: true);
            try
            {
                using var driver = KeyboardRouteInputDriver.Begin(laser.Player);
                var leaves = new List<AttackTelegraphVisual>(8);
                var input = laser.Player.GetNode<PlayerInputReader>("Input");
                var combat = laser.Player.GetNode<PlayerCombat>("Combat");
                var energy = laser.Player.GetNode<PlayerEnergyController>("Energy");
                var shield = laser.Player.GetNode<PlayerShieldController>("Shield");
                int initialHp = laser.Health.CurrentHealth, guarded = 0, attacks = 0;
                TestAssert.Equal(5, initialHp, "controlled-pose keyboard policy retains saved Player five HP");
                shield.ShieldResolved += (result, _) => { if (result.Guarded) guarded++; };
                combat.AttackStarted += (_, _) => attacks++;
                await Until(tree, () => laser.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 30);
                TestAssert.True(Authorized(laser.Actor).Any(leaf =>
                    leaf.CurrentFrame.Timeline.Kind == EnemyAttackKind.PrismLaserBeam),
                    "saved laser has a real authorized warning before policy evaluation");
                bool threat = PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(laser.Actor, false, leaves);
                bool defend = PrologueCanyonGeneratedMainRouteTests.Task10GeneralDefendWindow(
                    combat, energy, threat, false, 30, 0); // Outside old idle guard cadence.
                driver.PointerButton(MouseButton.Right, defend, laser.Player, laser.Actor.GlobalPosition);
                await Frames(tree, 3);
                TestAssert.True(input.Current.ShieldHeld && shield.IsActive && shield.GuardDirection.X < -.9f,
                    "RANGED_DRIVER_TELEGRAPH_THREAT_RED: saved laser warning must drive real left-facing keyboard guard outside idle cadence");
                for (int frame = 0; frame < 130 && laser.Actor.CurrentFrame.State != RangedWeaponState.Recovery; frame++)
                {
                    threat = PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(laser.Actor, false, leaves);
                    defend = PrologueCanyonGeneratedMainRouteTests.Task10GeneralDefendWindow(
                        combat, energy, threat, false, 30, 0);
                    driver.PointerButton(MouseButton.Right, defend, laser.Player, laser.Actor.GlobalPosition);
                    await Frames(tree, 1);
                }
                TestAssert.Equal(RangedWeaponState.Recovery, laser.Actor.CurrentFrame.State,
                    "real laser naturally reaches Recovery within bound");
                TestAssert.True(guarded > 0 && laser.Health.CurrentHealth == initialHp && attacks == 0 &&
                    !energy.IsOverheated, "actual laser contact is Guarded with scene HP intact and no premature attack");
                TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(laser.Actor, false, leaves),
                    "laser Recovery has no phantom beam threat");
                driver.PointerButton(MouseButton.Right, false, laser.Player, laser.Actor.GlobalPosition);
                await Frames(tree, 3);
                TestAssert.True(!input.Current.ShieldHeld && !shield.IsActive,
                    "real L release is sampled before the ordinary counter");
                driver.Command(InputActions.PrimaryAction, true);
                await Frames(tree, 2);
                driver.Command(InputActions.PrimaryAction, false);
                TestAssert.Equal(1, attacks, "fresh physical J accepts one ordinary counter; not a 240px hit claim");
                TestAssert.Equal(0UL, driver.ObservedMouseMotionSamples, "policy adapter emits physical keys, not mouse motion");
                GD.Print($"RANGED_DRIVER_LASER_GUARD_OK guarded={guarded} hp={initialHp}->{laser.Health.CurrentHealth} " +
                    $"attacks={attacks} heat={energy.HeatRatio:R} controlled_pose=true hit_claim=false");
            }
            finally { await DisposeFixture(tree, laser); }

            foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
            {
                Fixture f = await CreateFixture(tree, role, "LiveThreatAuthority" + role, ordinaryHealth: true);
                try
                {
                    var leaves = new List<AttackTelegraphVisual>(8);
                    await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 30);
                    TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(f.Actor, false, leaves),
                        "all four actual saved warnings are threats without an emission quota: " + role);
                    tree.Paused = true;
                    try
                    {
                        TestAssert.True(Authorized(f.Actor).Count > 0 &&
                            !PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(f.Actor, false, leaves),
                            "paused live warning is not current gameplay authority: " + role);
                    }
                    finally { tree.Paused = false; }
                    f.Actor.ApplyThreadPull(Vector2.Left * 540f);
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(f.Actor, false, leaves),
                        "same-stack claw revocation cannot reuse the old warning: " + role);
                    f.Actor.ResetForCheckpoint();
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(f.Actor, false, leaves),
                        "reset clears prior source without waiting a physics tick: " + role);
                    f.Actor.QueueFree();
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(f.Actor, false, leaves),
                        "queued owner cannot supply a cached warning: " + role);
                }
                finally { tree.Paused = false; await DisposeFixture(tree, f); }
            }

            Fixture burst = await CreateFixture(tree, RangedWeaponRole.Burst, "RealRecoveryTail", ordinaryHealth: true);
            try
            {
                var leaves = new List<AttackTelegraphVisual>(8);
                await Until(tree, () => burst.Actor.CurrentFrame.State == RangedWeaponState.Recovery, 160);
                TestAssert.True(burst.Actor.CurrentFrame.State == RangedWeaponState.Recovery &&
                    Authorized(burst.Actor).Any(leaf => leaf.CurrentFrame.Timeline.Phase == AttackPhase.Active),
                    "saved Burst has a genuinely active tail after owner Recovery");
                TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(burst.Actor, false, leaves),
                    "owner Recovery does not erase actual projectile danger");
                burst.Actor.GetNode<HealthComponent>("Health").ApplyDamage(
                    new DamageRequest(2, Vector2.Left, 0f, 934UL, 934UL));
                TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(burst.Actor, false, leaves),
                    "actual lethal health damage revokes tail authority in the same stack");
            }
            finally { await DisposeFixture(tree, burst); }
            GD.Print("RANGED_DRIVER_THREAT_POLICY_OK saved_roles=4 native_guard=true active_tail=true authority_controls=true whole_goal=false");
        }
        finally { _loadSavedScenes = false; _suiteRunning = false; }
    }

    private static async Task RunSuite(SceneTree tree, bool savedScenes)
    {
        if (_suiteRunning) throw new InvalidOperationException("Ranged actor suites must run serially.");
        _suiteRunning = true;
        _loadSavedScenes = savedScenes;
        try
        {
            await SynchronousThreadCallbackRevokesBurstCatchupTail(tree);
            // First RED is a real missing attack, never a missing scene or class.
            await ScatterEmitsFiveBodiesAndKeepsTailWarnings(tree);
            await BurstEmitsThreeIndependentDamageAttempts(tree);
            await BurstHitchConsumesRecoveryTailEvents(tree);
            await MissileOnlyDamagesAtExplosion(tree);
            await LaserLocksDirectionAndDamagesAcrossItsRealWidth(tree);
            await LaserWideEdgeStopsAtWorldAndArena(tree);
            await ArenaViewportLivingTargetAndLosFailClosed(tree);
            await ThreadInterruptCancelsAttacksAndDoesNotRefreshStun(tree);
            await DeathAndSynchronousDamageCallbackRevokeOwnedAttacks(tree);
            await ResetPreservesDamageIdentityMonotonicity(tree);
            await QueuedOwnerRemovalCancelsEveryOwnedAttack(tree);
            await BorrowedSettingsCannotMutateMountedActor(tree);
            BuilderHierarchyUsesAuthoritativeCombatComponents();
            GD.Print("RANGED_ACTOR_RUNTIME_OK four_roles=true native_physics=true whole_goal=false");
            if (savedScenes)
                GD.Print("RANGED_ACTOR_SAVED_SCENES_OK roles=4 groups=14 resource_cache=ignore whole_goal=false");
        }
        finally { _loadSavedScenes = false; _suiteRunning = false; }
    }

    // Break caught: a ShotEmitted callback revokes the owner's attacks while
    // EmissionsDue still contains a third event, but catch-up launches it anyway.
    private static async Task SynchronousThreadCallbackRevokesBurstCatchupTail(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Burst, "BurstThreadReentry");
        try
        {
            await Until(tree, () => f.Emissions.Count == 1, 100);
            TestAssert.Equal(1, f.Emissions.Count, "real first burst shot exists before callback interruption");
            f.Actor.SetPhysicsProcess(false);
            int interrupts = 0;
            f.Actor.ShotEmitted += (_, _) =>
            {
                if (f.Emissions.Count != 2) return;
                interrupts++;
                f.Actor.ApplyThreadPull(Vector2.Left * 540f);
            };
            f.Actor._PhysicsProcess(.42d);
            TestAssert.Equal(1, interrupts, "real second burst emission invokes the synchronous thread callback");
            TestAssert.Equal(0, LiveProjectiles(f.Actor).Count,
                "RANGED_ACTOR_INTERRUPT_REENTRY_RED: synchronous thread callback must leave no live burst catch-up tail");
            TestAssert.Equal(2, f.Emissions.Count,
                "thread callback during second emission suppresses the third overdue burst event");
            TestAssert.Equal(0, Authorized(f.Actor).Count,
                "synchronous thread callback leaves no authorized tail warning");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: an actor never enters warning, emits fewer/more than five
    // swept bodies, drops their tail warnings, or permits same-volley HP repeats.
    private static async Task ScatterEmitsFiveBodiesAndKeepsTailWarnings(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Scatter, "ScatterRealVolley");
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12);
            TestAssert.Equal(RangedWeaponState.Telegraph, f.Actor.CurrentFrame.State,
                "RANGED_ACTOR_SCATTER_RED: living visible player in admitted arena must start real scatter warning");
            await Until(tree, () => f.Emissions.Count == 1, 90);
            TestAssert.Equal(1, f.Emissions.Count, "scatter publishes one actual emission event per volley");
            TestAssert.Equal(5, LiveProjectiles(f.Actor).Count,
                "scatter emits exactly five real owned projectile bodies");
            TestAssert.True(LiveProjectiles(f.Actor).Select(p => p.GlobalPosition).All(Finite),
                "all five real projectile origins are finite");
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Recovery, 20);
            f.Actor.SetPhysicsProcess(false);
            List<AttackTelegraphVisual> leaves = Authorized(f.Actor);
            TestAssert.Equal(5, leaves.Count,
                "all five live pellet warnings survive the actor's short Active phase");
            TestAssert.True(leaves.All(leaf => f.Actor.IsAncestorOf(leaf) && leaf.ImpactVisible),
                "authorized warnings are actual active descendants, not synthetic global copies");
            await Frames(tree, 90);
            TestAssert.Equal(99, f.Health.CurrentHealth, "a five-pellet scatter volley removes only one HP");
            TestAssert.Equal(1, f.Observer.Requests.Count,
                "shared volley ledger submits at most one attempt to the actual receiver");
            TestAssert.Equal(DamageGuardClass.Normal, f.Observer.Requests.Single().GuardClass,
                "scatter uses the Normal shield contract");
            TestAssert.Equal(0, Authorized(f.Actor).Count, "retired pellets release every tail warning");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: burst shares one damage identity, loses either scheduled
    // tail, or incorrectly couples its damage to the player's .35s default I-frame.
    private static async Task BurstEmitsThreeIndependentDamageAttempts(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Burst, "BurstRealVolley");
        try
        {
            await Until(tree, () => f.Emissions.Count == 3, 130);
            TestAssert.Equal(3, f.Emissions.Count, "burst emits three real t0/.18/.36 events");
            TestAssert.Equal(3, f.Emissions.Select(e => e.Id).Distinct().Count(),
                "burst emission events have distinct owner-local damage ordinals");
            f.Actor.SetPhysicsProcess(false);
            await Frames(tree, 100);
            TestAssert.Equal(97, f.Health.CurrentHealth,
                "zero-invulnerability real player loses one HP for each of the three burst rounds");
            TestAssert.Equal(3, f.Observer.Requests.Select(r => r.AttackIdentity).Distinct().Count(),
                "each burst round delivers its own real damage identity");
            TestAssert.True(f.Observer.Requests.All(r => r.SourceRuntimeId == f.Actor.GetInstanceId()),
                "burst requests retain their actor runtime owner identity");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: branching on Recovery before consuming EmissionsDue silently
    // drops two shots after a native game-loop hitch.
    private static async Task BurstHitchConsumesRecoveryTailEvents(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Burst, "BurstHitchTail");
        try
        {
            await Until(tree, () => f.Emissions.Count == 1, 100);
            f.Actor.SetPhysicsProcess(false);
            f.Actor._PhysicsProcess(.42d);
            TestAssert.Equal(RangedWeaponState.Recovery, f.Actor.CurrentFrame.State,
                "fixture advances burst directly from Active to Recovery");
            TestAssert.Equal(3, f.Emissions.Count,
                "Recovery frame consumes both overdue burst emissions before state presentation");
            TestAssert.Equal(3, LiveProjectiles(f.Actor).Count,
                "hitch tail creates all three physical rounds, not just three notifications");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: missile body submits contact damage before its explosion,
    // explosion submits twice, or no physical moving missile exists.
    private static async Task MissileOnlyDamagesAtExplosion(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Missile, "MissileExplosionOnly");
        try
        {
            await Until(tree, () => f.Emissions.Count == 1, 120);
            TestAssert.Equal(1, LiveProjectiles(f.Actor).Count, "missile emits one real moving projectile");
            TestAssert.Equal(100, f.Health.CurrentHealth, "distant missile launch itself cannot damage player");
            f.Actor.SetPhysicsProcess(false);
            bool explosionVisibleAtDamage = false;
            f.Health.Damaged += (_, _) => explosionVisibleAtDamage = Authorized(f.Actor).Any(
                leaf => leaf.CurrentFrame.Shape.Kind == AttackShapeKind.Circle &&
                    leaf.CurrentFrame.Shape.Extent.X >= 55f && leaf.ImpactVisible);
            await Until(tree, () => f.Health.CurrentHealth < 100, 100);
            TestAssert.Equal(99, f.Health.CurrentHealth, "missile explosion removes exactly one real HP");
            TestAssert.True(explosionVisibleAtDamage,
                "damage occurs under the authorized radius-56 explosion, never the radius-8 flight body");
            await Frames(tree, 1);
            TestAssert.True(f.Actor.GetNode<Node2D>("VisualRoot/Effects").GetChildren()
                .Any(node => node.GetType().Name == "RangedWeaponImpactRing"),
                "missile cosmetic blast remains live across a later physics frame; GPU rendering is separately gated");
            await Frames(tree, 40);
            TestAssert.Equal(1, f.Observer.Requests.Count, "missile contact plus explosion cannot submit twice");
            TestAssert.Equal(0, LiveProjectiles(f.Actor).Count, "exploded missile retires its owned body");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: laser chases after lock, uses a thin center ray for damage,
    // creates projectile impostors, or repeatedly damages throughout its active phase.
    private static async Task LaserLocksDirectionAndDamagesAcrossItsRealWidth(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Laser, "LaserRealWidth");
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.AttackTimeline.HasLockedTarget, 100);
            TestAssert.True(f.Actor.CurrentFrame.AttackTimeline.HasLockedTarget, "laser enters its visible lock");
            Vector2 locked = f.Actor.CurrentFrame.AimDirection;
            // Replace only this fixture's borrowed collider reference with an
            // owned radius20 circle. The actual Player receiver/Health remain.
            // Offset24 misses a thin ray but overlaps half-width5 by one pixel.
            var edgeCircle = new CircleShape2D { Radius = 20f };
            f.OwnedResources.Add(edgeCircle);
            f.Player.GetNode<CollisionShape2D>("Hurtbox/HurtboxCollider").Shape = edgeCircle;
            Vector2 muzzle = f.Actor.GetNode<Node2D>("Muzzle").GlobalPosition;
            f.Player.GlobalPosition = muzzle + locked * 240f + locked.Orthogonal() * 24f;
            await Frames(tree, 2);
            using (var centerRay = PhysicsRayQueryParameters2D.Create(muzzle,
                muzzle + locked * 560f, CombatCollisionLayers.Player))
            {
                centerRay.CollideWithAreas = true;
                centerRay.CollideWithBodies = false;
                using Godot.Collections.Dictionary hit = f.Actor.GetWorld2D().DirectSpaceState.IntersectRay(centerRay);
                TestAssert.Equal(0, hit.Count,
                    "native thin center ray misses the actual fixture hurtbox before the wide beam activates");
            }
            await Until(tree, () => f.Emissions.Count == 1, 70);
            TestAssert.Near(0f, locked.AngleTo(f.Actor.CurrentFrame.AimDirection), .001f,
                "locked laser direction remains fixed when player leaves its center line");
            TestAssert.Equal(0, LiveProjectiles(f.Actor).Count, "laser is a real sustained beam, not a bullet");
            await Until(tree, () => f.Health.CurrentHealth < 100, 10);
            TestAssert.Equal(99, f.Health.CurrentHealth,
                "real Capsule IntersectShape damages a hurtbox overlapping only the laser edge");
            List<AttackTelegraphVisual> active = Authorized(f.Actor);
            TestAssert.True(active.Any(leaf => leaf.CurrentFrame.Shape.Kind == AttackShapeKind.Capsule &&
                leaf.ImpactVisible && MathF.Abs(leaf.CurrentFrame.Shape.Extent.X - 5f) < .01f),
                "authorized active geometry exposes the true half-width-five capsule");
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Recovery, 30);
            f.Actor.SetPhysicsProcess(false);
            TestAssert.Equal(1, f.Observer.Requests.Count, "continuous laser submits once over its entire .28s Active");
            TestAssert.Equal(0, Authorized(f.Actor).Count, "laser recovery has no stale dangerous beam leaf");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: thick beam clips only its center line and leaks around a
    // wall edge, or its radius extends beyond the admitted arena.
    private static async Task LaserWideEdgeStopsAtWorldAndArena(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Laser, "LaserWideWorldEdge");
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.AttackTimeline.HasLockedTarget, 100);
            Vector2 muzzle = f.Actor.GetNode<Node2D>("Muzzle").GlobalPosition;
            Vector2 locked = f.Actor.CurrentFrame.AimDirection;
            StaticBody2D wall = AddWorldRect(f, "EdgeOnlyWall",
                muzzle + locked * 130f + locked.Orthogonal() * 7f, new Vector2(3f, 8f));
            wall.Rotation = locked.Angle();
            await Frames(tree, 2);
            using (var centerRay = PhysicsRayQueryParameters2D.Create(muzzle,
                muzzle + locked * 560f, CombatCollisionLayers.World))
            {
                centerRay.CollideWithAreas = false;
                centerRay.CollideWithBodies = true;
                using Godot.Collections.Dictionary hit = f.Actor.GetWorld2D().DirectSpaceState.IntersectRay(centerRay);
                TestAssert.Equal(0, hit.Count,
                    "native center ray misses edge-only wall before radius-five beam cast");
            }
            await Until(tree, () => f.Emissions.Count == 1, 70);
            AttackTelegraphVisual beam = Authorized(f.Actor).Single(leaf => leaf.ImpactVisible);
            TestAssert.True(beam.CurrentFrame.Shape.Origin.X < 330f,
                "thick laser cast is clipped at a wall missed by its center ray");
            TestAssert.Equal(100, f.Health.CurrentHealth, "edge world cover blocks the entire beam beyond contact");
            f.Actor.SetPhysicsProcess(false);
        }
        finally { await DisposeFixture(tree, f); }

        Fixture bounded = await CreateFixture(tree, RangedWeaponRole.Laser, "LaserArenaWidth", new Rect2(80f, 120f, 320f, 500f));
        try
        {
            await Until(tree, () => bounded.Emissions.Count == 1, 120);
            AttackShapeFrame shape = Authorized(bounded.Actor).Single(leaf => leaf.ImpactVisible).CurrentFrame.Shape;
            // Fixture fires almost horizontally: capsule far cap must remain <=400.
            float farX = shape.Origin.X + shape.Extent.Y * MathF.Abs(MathF.Sin(shape.RotationRadians));
            TestAssert.True(farX <= 400.01f, "full laser width including rounded cap remains inside arena");
            TestAssert.Equal(100, bounded.Health.CurrentHealth, "out-of-arena player cannot be hit through clipped cap");
            bounded.Actor.SetPhysicsProcess(false);
        }
        finally { await DisposeFixture(tree, bounded); }
    }

    // Break caught: an unconfigured/degenerate arena, offscreen muzzle, dead
    // target or native world occluder is treated as authorization to start firing.
    private static async Task ArenaViewportLivingTargetAndLosFailClosed(SceneTree tree)
    {
        foreach (Rect2? bounds in new Rect2?[] { null, new Rect2(0f, 0f, 0f, 100f), new Rect2(float.NaN, 0f, 900f, 600f) })
        {
            Fixture f = await CreateFixture(tree, RangedWeaponRole.Scatter, "InvalidArena", bounds, configure: bounds.HasValue);
            try
            {
                await Frames(tree, 100);
                TestAssert.Equal(0, f.Emissions.Count, "absent or non-positive/non-finite arena fails closed");
                TestAssert.Equal(AttackPhase.None, f.Actor.CurrentFrame.AttackTimeline.Phase,
                    "invalid arena cannot publish a warning");
            }
            finally { await DisposeFixture(tree, f); }
        }
        Fixture visible = await CreateFixture(tree, RangedWeaponRole.Scatter, "VisibilityGates");
        try
        {
            Transform2D originalCanvas = tree.Root.CanvasTransform;
            try
            {
                // Keep native world geometry, arena and distance valid. Only
                // the camera/canvas moves the gun out of the actual viewport.
                tree.Root.CanvasTransform = new Transform2D(0f, new Vector2(-250f, 0f));
                visible.Actor.ResetForCheckpoint();
                Vector2 muzzle = visible.Actor.GetNode<Node2D>("Muzzle").GlobalPosition;
                TestAssert.True(Arena.HasPoint(muzzle) && Arena.HasPoint(visible.Player.GlobalPosition),
                    "viewport negative control keeps actor and real player inside the admitted world arena");
                TestAssert.True(muzzle.DistanceTo(visible.Player.GlobalPosition) < 360f && visible.Health.CurrentHealth > 0,
                    "viewport negative control keeps a living player within scatter detection range");
                TestAssert.True(RangedAttackGeometry.HasClearWorldPath(visible.Actor.GetWorld2D().DirectSpaceState,
                    muzzle, visible.Player.GlobalPosition), "viewport negative control keeps native world LOS clear");
                TestAssert.True(!tree.Root.GetVisibleRect().HasPoint(visible.Actor.GetNode<Node2D>("Muzzle")
                        .GetGlobalTransformWithCanvas().Origin) &&
                    tree.Root.GetVisibleRect().HasPoint(visible.Player.GetGlobalTransformWithCanvas().Origin),
                    "actual viewport excludes only the muzzle while the real player remains onscreen");
                await Frames(tree, 100);
                TestAssert.Equal(0, visible.Emissions.Count, "offscreen muzzle cannot attack an onscreen player");
                TestAssert.Equal(AttackPhase.None, visible.Actor.CurrentFrame.AttackTimeline.Phase,
                    "viewport-only denial cannot start a warning");
            }
            finally { tree.Root.CanvasTransform = originalCanvas; }
            AddWorldRect(visible, "LosWall", new Vector2(320f, 280f), new Vector2(4f, 180f));
            visible.Actor.ResetForCheckpoint();
            await Frames(tree, 100);
            TestAssert.Equal(0, visible.Emissions.Count, "native solid-world wall prevents initial warning");
            visible.Root.GetNode("LosWall").QueueFree();
            visible.Health.ApplyDamage(new DamageRequest(1000, Vector2.Right, 0f, 911UL, 911UL));
            visible.Actor.ResetForCheckpoint();
            await Frames(tree, 100);
            TestAssert.Equal(0, visible.Emissions.Count, "dead actual player remaining in group cannot authorize attack");
        }
        finally { await DisposeFixture(tree, visible); }
    }

    // Break caught: thread interruption leaves a pending shot/beam/pellet, or
    // repeated interaction incorrectly refreshes/freezes the already-running .8s stun.
    private static async Task ThreadInterruptCancelsAttacksAndDoesNotRefreshStun(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            Fixture f = await CreateFixture(tree, role, "ThreadCancel" + role);
            try
            {
                await Until(tree, () => f.Emissions.Count > 0, 130);
                f.Actor.ApplyThreadPull(Vector2.Left * 540f);
                await Frames(tree, 1);
                TestAssert.Equal(RangedWeaponState.Stunned, f.Actor.CurrentFrame.State, "thread enters real actor stun: " + role);
                // Freeze the actor clock immediately at stun entry. Native
                // cleanup frames below must not add time to the .79/.81 probe.
                f.Actor.SetPhysicsProcess(false);
                TestAssert.Equal(0, Authorized(f.Actor).Count, "thread synchronously revokes all live attack warnings: " + role);
                await Frames(tree, 2);
                TestAssert.Equal(0, LiveProjectiles(f.Actor).Count, "thread removes all owner projectiles: " + role);
                f.Actor._PhysicsProcess(.4d);
                for (int i = 0; i < 5; i++) f.Actor.ApplyThreadPull(Vector2.Left * 540f);
                f.Actor._PhysicsProcess(.39d);
                TestAssert.Equal(RangedWeaponState.Stunned, f.Actor.CurrentFrame.State, "duplicate thread does not skip remaining stun: " + role);
                f.Actor._PhysicsProcess(.02d);
                TestAssert.Equal(RangedWeaponState.Idle, f.Actor.CurrentFrame.State,
                    "duplicate thread does not refresh or freeze the original .8s stun: " + role);
            }
            finally { await DisposeFixture(tree, f); }
        }
    }

    // Break caught: death cannot synchronously revoke dangerous descendants,
    // including reentrant death while a real player's damage callback is active.
    private static async Task DeathAndSynchronousDamageCallbackRevokeOwnedAttacks(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            Fixture f = await CreateFixture(tree, role, "DeathCancel" + role);
            int defeats = 0;
            f.Actor.Defeated += () => defeats++;
            try
            {
                await Until(tree, () => f.Emissions.Count > 0, 130);
                f.Actor.GetNode<HealthComponent>("Health").ApplyDamage(new DamageRequest(2, Vector2.Left, 0f, 922UL, 922UL));
                TestAssert.Equal(0, Authorized(f.Actor).Count, "death immediately revokes attack leaves: " + role);
                await Frames(tree, 3);
                TestAssert.Equal(RangedWeaponState.Dead, f.Actor.CurrentFrame.State, "death wins over firing: " + role);
                TestAssert.Equal(1, defeats, "actor emits defeat once: " + role);
                TestAssert.Equal(0, LiveProjectiles(f.Actor).Count, "dead owner has no live physical attacks: " + role);
            }
            finally { await DisposeFixture(tree, f); }
        }
        Fixture reentrant = await CreateFixture(tree, RangedWeaponRole.Scatter, "DamageCallbackDeath");
        try
        {
            reentrant.Health.Damaged += (_, _) => reentrant.Actor.GetNode<HealthComponent>("Health")
                .ApplyDamage(new DamageRequest(2, Vector2.Left, 0f, 933UL, 933UL));
            await Until(tree, () => reentrant.Health.CurrentHealth < 100, 160);
            await Frames(tree, 3);
            TestAssert.Equal(99, reentrant.Health.CurrentHealth,
                "synchronous owner death inside real receiver callback prevents remaining pellets from damaging");
            TestAssert.Equal(0, LiveProjectiles(reentrant.Actor).Count,
                "synchronous damage-callback death safely retires every remaining projectile");
        }
        finally { await DisposeFixture(tree, reentrant); }
    }

    // Break caught: reset reuses damage IDs retained by another Health receiver,
    // revives stale warning leaves, or fails to restore the captured spawn/HP.
    private static async Task ResetPreservesDamageIdentityMonotonicity(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Burst, "ResetDamageIdentity");
        try
        {
            await Until(tree, () => f.Emissions.Count == 3, 130);
            f.Actor.SetPhysicsProcess(false);
            await Frames(tree, 80);
            DamageAttackIdentity[] old = f.Observer.Requests.Select(r => r.AttackIdentity).ToArray();
            TestAssert.Equal(3, old.Length, "first real burst establishes three retained receiver identities");
            f.Actor.GlobalPosition += Vector2.Right * 50f;
            f.Actor.ResetForCheckpoint();
            TestAssert.Equal(0, Authorized(f.Actor).Count, "reset synchronously clears all telegraphs");
            TestAssert.Near(200f, f.Actor.GlobalPosition.X, .01f, "reset restores authored spawn position");
            TestAssert.Equal(2, f.Actor.GetNode<HealthComponent>("Health").CurrentHealth, "reset restores two actor HP");
            f.Actor.SetPhysicsProcess(true);
            await Until(tree, () => f.Emissions.Count == 6, 130);
            f.Actor.SetPhysicsProcess(false);
            await Frames(tree, 80);
            TestAssert.Equal(94, f.Health.CurrentHealth,
                "post-reset burst damages unchanged player health cache with three new identities");
            TestAssert.True(f.Observer.Requests.Skip(3).All(request => request.SourceAttackId > old.Max(id => id.LocalAttackId)),
                "damage ordinals remain strictly monotonic even when model timeline restarts at one");
        }
        finally { await DisposeFixture(tree, f); }
    }

    // Break caught: queue-delete/streaming exit leaves physical descendants or
    // authorized attack references able to damage after the owner is gone.
    private static async Task QueuedOwnerRemovalCancelsEveryOwnedAttack(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            Fixture f = await CreateFixture(tree, role, "QueuedOwner" + role);
            try
            {
                await Until(tree, () => f.Emissions.Count > 0, 130);
                Node2D[] owned = LiveProjectiles(f.Actor).ToArray();
                int before = f.Health.CurrentHealth;
                f.Actor.QueueFree();
                await Frames(tree, 4);
                TestAssert.True(owned.All(node => !GodotObject.IsInstanceValid(node)),
                    "queued owner removal frees all real owned projectiles: " + role);
                await Frames(tree, 90);
                TestAssert.Equal(before, f.Health.CurrentHealth,
                    "unloaded owner cannot leave delayed physical damage: " + role);
            }
            finally { await DisposeFixture(tree, f); }
        }
    }

    // Break caught: mounted actor reads a borrowed mutable resource every tick
    // instead of an owned validated snapshot, or disposes the borrowed export.
    private static async Task BorrowedSettingsCannotMutateMountedActor(SceneTree tree)
    {
        Fixture f = await CreateFixture(tree, RangedWeaponRole.Scatter, "SettingsSnapshot");
        RangedWeaponSettings borrowed = f.Actor.Settings;
        try
        {
            borrowed.Damage = 50;
            borrowed.ScatterPelletCount = 9;
            borrowed.ProjectileSpeed = 1f;
            borrowed.RecoverySeconds = .01f;
            await Until(tree, () => f.Emissions.Count == 1, 100);
            TestAssert.Equal(5, LiveProjectiles(f.Actor).Count, "mounted actor retains its validated five-pellet snapshot");
            f.Actor.SetPhysicsProcess(false);
            await Frames(tree, 100);
            TestAssert.Equal(99, f.Health.CurrentHealth, "runtime uses snapshot damage/speed, not edited borrowed resource");
        }
        finally { await DisposeFixture(tree, f, retainSettings: true); }
        TestAssert.True(GodotObject.IsInstanceValid(borrowed), "actor cleanup must not Dispose its borrowed exported settings");
        borrowed.Dispose();
    }

    private static void BuilderHierarchyUsesAuthoritativeCombatComponents()
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            RangedWeaponEnemy actor = CreateActorForFixture(role, out PackedScene ownedLeafScene);
            HashSet<Resource> resources = FactoryResources(actor);
            try
            {
                TestAssert.Equal(role, actor.Role, "leaf factory returns the requested real role");
                TestAssert.Equal(CombatCollisionLayers.Enemy, actor.CollisionLayer, "body uses enemy collision layer");
                TestAssert.Equal(CombatCollisionLayers.World, actor.CollisionMask, "body collides with native world");
                var body = actor.GetNode<CollisionShape2D>("BodyCollider");
                TestAssert.True(body.Shape is CapsuleShape2D capsule && capsule.Radius == 25f && capsule.Height == 68f &&
                    body.Position == new Vector2(0f, -5f), "builder preserves sentry full-body placement geometry");
                TestAssert.Equal(2, actor.GetNode<HealthComponent>("Health").MaxHealth, "first-pass weapon enemy has two HP");
                TestAssert.Equal(ThreadTargetKind.LightEnemy, actor.GetNode<ThreadTarget2D>("ThreadTarget").Kind,
                    "real thread target routes light-enemy interruption");
                TestAssert.True(actor.GetNodeOrNull<RangedWeaponVisual>("VisualRoot/WeaponVisual") != null &&
                    actor.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph") != null &&
                    actor.GetNodeOrNull<RangedWeaponPresentation>("Presentation") != null,
                    "factory includes independent weapon silhouette, local warning and typed presentation");
            }
            finally
            {
                actor.Free();
                foreach (Resource resource in resources) resource.Dispose();
                ownedLeafScene?.Dispose();
            }
        }
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree, RangedWeaponRole role, string name,
        Rect2? bounds = null, bool configure = true, bool ordinaryHealth = false)
    {
        RangedWeaponEnemy actor = CreateActorForFixture(role, out PackedScene ownedLeafScene);
        HashSet<Resource> resources = FactoryResources(actor);
        var root = new Node2D { Name = name };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "ActualPlayer";
        player.Position = new Vector2(440f, 300f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.AddToGroup("player");
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        root.AddChild(WorldRect("Floor", new Vector2(580f, 339f), new Vector2(1100f, 20f)));
        root.AddChild(player);
        actor.Position = new Vector2(200f, 300f);
        if (configure) actor.ConfigureEncounterEnvelope(bounds ?? Arena);
        var emissions = new List<Emission>();
        actor.ShotEmitted += (id, origin) => emissions.Add(new Emission(id, origin));
        root.AddChild(actor);
        tree.Root.AddChild(root);
        await Frames(tree, 2);
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        if (!ordinaryHealth)
        {
            health.MaxHealth = 100;
            health.InvulnerabilitySeconds = 0f;
            health.ResetFull();
        }
        Hurtbox hurtbox = player.GetNode<Hurtbox>("Hurtbox");
        var observer = new RangedDamageObservation { Name = "DamageObservation", Inner = hurtbox.ResolveDamageReceiver() };
        player.AddChild(observer);
        hurtbox.SetDamageReceiverPath(new NodePath("../DamageObservation"));
        CollectShapes(root.GetNode("Floor"), resources);
        return new Fixture(root, actor, player, health, observer, emissions, resources, ownedLeafScene);
    }

    private static RangedWeaponEnemy CreateActorForFixture(RangedWeaponRole role, out PackedScene ownedLeafScene)
    {
        ownedLeafScene = null;
        if (!_loadSavedScenes) return BuildRangedWeaponEnemies.CreateActor(role);
        string name = role switch
        {
            RangedWeaponRole.Scatter => nameof(RivetScatterGunner),
            RangedWeaponRole.Burst => nameof(BeltBurstGunner),
            RangedWeaponRole.Missile => nameof(FurnaceMissileGunner),
            RangedWeaponRole.Laser => nameof(PrismLaserGunner),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        string path = $"res://scenes/enemies/{name}.tscn";
        ownedLeafScene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        TestAssert.True(ownedLeafScene != null, "saved actor fixture must load the generated leaf scene: " + path);
        RangedWeaponEnemy actor = null;
        try
        {
            actor = ownedLeafScene.Instantiate<RangedWeaponEnemy>();
            TestAssert.Equal(path, actor.SceneFilePath, "saved behavior fixture retains its actual leaf SceneFilePath");
            TestAssert.Equal(role, actor.Role, "saved behavior fixture instantiates the requested concrete weapon role");
            TestAssert.Equal(role, actor.Settings.Role, "saved behavior fixture retains serialized role-specific settings");
            return actor;
        }
        catch
        {
            if (actor != null)
            {
                HashSet<Resource> resources = FactoryResources(actor);
                actor.Free();
                foreach (Resource resource in resources) resource.Dispose();
            }
            ownedLeafScene.Dispose();
            ownedLeafScene = null;
            throw;
        }
    }

    private static StaticBody2D WorldRect(string name, Vector2 center, Vector2 size)
    {
        var body = new StaticBody2D { Name = name, Position = center,
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        return body;
    }

    private static List<Node2D> LiveProjectiles(RangedWeaponEnemy actor) => actor.GetChildren()
        .OfType<RangedWeaponProjectile>().Where(node => !node.IsRetired && !node.IsQueuedForDeletion())
        .Cast<Node2D>().ToList();

    private static List<AttackTelegraphVisual> Authorized(RangedWeaponEnemy actor)
    {
        var leaves = new List<AttackTelegraphVisual>();
        actor.AppendAuthorizedAttackLeaves(leaves);
        return leaves;
    }

    private static async Task Until(SceneTree tree, Func<bool> condition, int frames)
    {
        for (int i = 0; i < frames && !condition(); i++) await Frames(tree, 1);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task DisposeFixture(SceneTree tree, Fixture f, bool retainSettings = false)
    {
        f.Root.QueueFree();
        await Frames(tree, 3);
        foreach (Resource resource in f.OwnedResources)
            if (!retainSettings || resource is not RangedWeaponSettings) resource.Dispose();
        f.OwnedLeafScene?.Dispose();
    }

    private static StaticBody2D AddWorldRect(Fixture f, string name, Vector2 center, Vector2 size)
    {
        StaticBody2D body = WorldRect(name, center, size);
        CollectShapes(body, f.OwnedResources);
        f.Root.AddChild(body);
        return body;
    }

    private static HashSet<Resource> FactoryResources(RangedWeaponEnemy actor)
    {
        var resources = new HashSet<Resource> { actor.Settings };
        CollectShapes(actor, resources);
        return resources;
    }

    private static void CollectShapes(Node node, HashSet<Resource> resources)
    {
        if (node is CollisionShape2D collider && collider.Shape != null) resources.Add(collider.Shape);
        foreach (Node child in node.GetChildren()) CollectShapes(child, resources);
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private sealed record Emission(ulong Id, Vector2 Origin);
    private sealed record Fixture(Node2D Root, RangedWeaponEnemy Actor, CharacterBody2D Player,
        HealthComponent Health, RangedDamageObservation Observer, List<Emission> Emissions,
        HashSet<Resource> OwnedResources, PackedScene OwnedLeafScene);
}

// Test-only observation preserves the real guarded receiver and Health side
// effects. It does not manufacture a hit, accept a rejected request, or bypass guard.
public partial class RangedDamageObservation : Node, IDamageReceiver
{
    public IDamageReceiver Inner { get; set; }
    public List<DamageRequest> Requests { get; } = new();
    public bool ReceiveDamage(DamageRequest request)
    {
        Requests.Add(request);
        return Inner.ReceiveDamage(request);
    }
}

// Passive scalar cache only. Assertions and input stay in the awaited trial.
internal sealed partial class SteamMissilePostMotorObserver : Node
{
    internal Action Observe;
    public override void _PhysicsProcess(double delta) => Observe?.Invoke();
}
