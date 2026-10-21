using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;
using Knight.World;

namespace Knight.Tests.Integration;

public static class RangedWeaponPlacementRuntimeTests
{
    private const string Scatter = "res://scenes/enemies/RivetScatterGunner.tscn";
    private const string Burst = "res://scenes/enemies/BeltBurstGunner.tscn";
    private const string Missile = "res://scenes/enemies/FurnaceMissileGunner.tscn";
    private const string Laser = "res://scenes/enemies/PrismLaserGunner.tscn";
    private static ulong _damageId = 814000UL;

    public enum MixedCounterProof { None, RejectShort, Connect, EarlyWarnings, TightWarning }

    public static async Task RunMixedThreatPolicy(SceneTree tree, bool resourcePolicy = false,
        int preparationAttacks = 0, MixedCounterProof counterProof = MixedCounterProof.None)
    {
        const string encounterId = "canyon.v2.encounter.counterweight-lift";
        Transform2D canvas = tree.Root.CanvasTransform;
        RangedFairnessRecoveryProbe root = null;
        PrologueSector sector = null;
        CharacterBody2D player = null;
        KeyboardRouteInputDriver driver = null;
        try
        {
            ReleaseFairnessKeys();
            root = new RangedFairnessRecoveryProbe { Name = "SavedMixedThreatPolicy" };
            sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn")
                .Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
            root.AddChild(sector);
            player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position = new Vector2(4100, 3500);
            player.AddToGroup("player");
            root.AddChild(player);
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/CounterweightLiftEncounter");
            Node2D actors = sector.GetNode<Node2D>("Gameplay/Actors");
            if (preparationAttacks > 0)
            {
                // Only pretrial resource preparation: native J costs, never state writes.
                group.SetPhysicsProcess(false);
                player.Position = new Vector2(4700, 3424);
            }
            group.ApplyProgress(new PrologueProgress());
            var births = new Dictionary<ulong, Vector2>();
            actors.ChildEnteredTree += child => child.Ready += () =>
            {
                if (child is Node2D actor) births[actor.GetInstanceId()] = actor.GlobalPosition;
            };
            tree.Root.AddChild(root);
            foreach (KillZone zone in Descendants(sector).OfType<KillZone>()) zone.RoomControllerPath = zone.GetPathTo(root);
            foreach (CanyonEncounterGroup other in Descendants(sector).OfType<CanyonEncounterGroup>())
                if (other != group) other.SetPhysicsProcess(false);
            await Frames(tree, 4);
            if (preparationAttacks > 0)
            {
                var preparationCombat = player.GetNode<PlayerCombat>("Combat");
                var preparationEnergy = player.GetNode<PlayerEnergyController>("Energy");
                int acceptedPreparationAttacks = 0;
                preparationCombat.AttackStarted += (_, _) => acceptedPreparationAttacks++;
                TestAssert.True(!group.IsActive && preparationEnergy.EnergyRatio > .99f,
                    "MIXED_RESOURCE_SETUP: full ordinary energy before real pretrial J preparation");
                driver = KeyboardRouteInputDriver.Begin(player);
                for (int preparation = 0; preparation < preparationAttacks; preparation++)
                {
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, true);
                    await Frames(tree, 2);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, false);
                    for (int wait = 0; wait < 24 && preparationCombat.IsAttacking; wait++) await Frames(tree, 1);
                    TestAssert.True(!preparationCombat.IsAttacking,
                        "MIXED_RESOURCE_SETUP: each actual J finishes before the next real press");
                }
                TestAssert.True(acceptedPreparationAttacks == preparationAttacks &&
                    preparationEnergy.EnergyRatio is > 0f and < .17f && root.RecoveryRequests == 0 &&
                    player.GetNode<HealthComponent>("Health").CurrentHealth == 5,
                    "MIXED_RESOURCE_SETUP: seven accepted native J actions spend84 energy without HP/reset/rescue");
                AssertFairnessSupport(player, sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding"),
                    new Rect2(4600, 3460, 200, 40));
                player.GlobalPosition = new Vector2(4100, 3500);
                group.SetPhysicsProcess(true);
                await Frames(tree, 4);
            }
            TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1,
                "MIXED_POLICY_SETUP: original Moth naturally activates the saved group");
            // Explicit isolated starting arrangement BEFORE the live mixed trial.
            // No position/velocity/HP writes, resets or artificial phase inputs follow.
            player.GlobalPosition = new Vector2(4700, 3424);
            await Frames(tree, 6);
            var health = player.GetNode<HealthComponent>("Health");
            var input = player.GetNode<PlayerInputReader>("Input");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var combat = player.GetNode<PlayerCombat>("Combat");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            StaticBody2D floor = sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding");
            AssertFairnessSupport(player, floor, new Rect2(4600, 3460, 200, 40));
            TestAssert.True(health.CurrentHealth == 5 && health.MaxHealth == 5 &&
                MathF.Abs(health.InvulnerabilitySeconds - .35f) < .0001f &&
                (preparationAttacks == 0 ? energy.EnergyRatio > .99f : energy.EnergyRatio is > 0f and < .45f),
                "MIXED_POLICY_SETUP: retain saved five HP/grace and explicitly prepared ordinary resources");
            Kill(actors.GetChild(0)); // Real prior-actor Health death; not group completion.
            for (int frame = 0; frame < 60 && group.LiveActorCount != 2; frame++) await Frames(tree, 1);
            TestAssert.True(group.CurrentBatch == 1 && group.LiveActorCount == 2,
                "MIXED_POLICY_SETUP: real .60s warning produces the live original pair");
            RustShellSquire rust = Children(actors).OfType<RustShellSquire>().Single();
            RangedWeaponEnemy burst = Children(actors).OfType<RangedWeaponEnemy>().Single();
            TestAssert.True(births[rust.GetInstanceId()] == new Vector2(4630, 3424) &&
                births[burst.GetInstanceId()] == new Vector2(4770, 3431) &&
                actors.GetChildren().OfType<Node2D>().First() == rust &&
                rust.SceneFilePath == CanyonEncounterCatalog.RustShellScene && burst.SceneFilePath == Burst,
                "MIXED_POLICY_SETUP: actual saved birth/order has selected Rust left and nonselected Burst right");
            var hurtbox = player.GetNode<Hurtbox>("Hurtbox");
            var guardedReceiver = player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage");
            var observer = new RangedDamageObservation { Name = "MixedDamageObservation", Inner = guardedReceiver };
            player.AddChild(observer);
            hurtbox.SetDamageReceiverPath(new NodePath("../MixedDamageObservation"));
            ulong burstId = burst.GetInstanceId(), playerId = player.GetInstanceId();
            int burstGuards = 0, rustGuards = 0, heals = 0, deaths = 0, candidateSamples = 0, emissions = 0;
            burst.ShotEmitted += (_, _) => emissions++;
            shield.ShieldResolved += (result, _) =>
            {
                if (!result.Guarded || observer.Requests.Count == 0) return;
                if (observer.Requests[^1].SourceRuntimeId == burstId) burstGuards++;
                else rustGuards++;
            };
            health.Healed += (_, _) => heals++;
            health.Died += () => deaths++;
            driver ??= KeyboardRouteInputDriver.Begin(player);
            // Keep the original safe birth stance, then approach with real A
            // during the natural warning so the actual Rust hitbox can contact.
            driver.Command(Knight.Shared.InputActions.MoveLeft, true);
            await Frames(tree, 5);
            driver.Command(Knight.Shared.InputActions.MoveLeft, false);
            await Frames(tree, 5);
            driver.ObserveFrame();
            TestAssert.True(health.CurrentHealth == 5 && root.RecoveryRequests == 0 &&
                player.GlobalPosition.X is > 4670f and < 4695f,
                "MIXED_POLICY_SETUP: actual A approach, not a closer spawn/teleport, enters Rust's contact reach");
            AssertFairnessSupport(player, floor, new Rect2(4600, 3460, 200, 40));
            var leaves = new List<AttackTelegraphVisual>(8);
            if (counterProof is MixedCounterProof.EarlyWarnings or MixedCounterProof.TightWarning)
            {
                await EarlyWarningCounters(tree, root, sector, actors, player, driver, rust, burst, encounterId,
                    counterProof == MixedCounterProof.TightWarning);
                return;
            }
            Vector2 previous = player.GlobalPosition;
            bool testedNonselected = false, testedPause = false, testedCommittedGap = false;
            bool lateShieldSample = false, lowEnergyActiveGuard = false;
            bool shortCounterRejected = false, counterConnected = false;
            int counterStarts = 0, rustCounterContacts = 0;
            combat.AttackStarted += (_, _) => counterStarts++;
            combat.AttackConnected += receiver =>
            {
                if (receiver == rust.GetNode<Hurtbox>("Hurtbox")) rustCounterContacts++;
            };
            // Separate native counter evidence window; original Main's900 budget
            // and legacy110-frame regression are not extended.
            int evidenceFrames = counterProof == MixedCounterProof.None ? 110 : 180;
            for (int frame = 0; frame < evidenceFrames; frame++)
            {
                bool candidate = rust.CurrentState == RustShellState.Recover &&
                    burst.CurrentFrame.State == RangedWeaponState.Telegraph;
                burst.AppendAuthorizedAttackLeaves(leaves);
                bool committedGap = rust.CurrentState == RustShellState.Recover &&
                    burst.HasPresentationAuthority && burst.CurrentFrame.State == RangedWeaponState.Active &&
                    burst.CurrentFrame.AttackTimeline.Phase == AttackPhase.Active && leaves.Count == 0;
                leaves.Clear();
                var danger = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(
                    actors, encounterId, rust, false, leaves);
                float commonRecovery = rust.CurrentFrame.AttackTimeline.Phase == AttackPhase.Recovery &&
                    burst.CurrentFrame.AttackTimeline.Phase == AttackPhase.Recovery
                    ? MathF.Min(rust.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
                        burst.CurrentFrame.AttackTimeline.PhaseRemainingSeconds) : 0f;
                var attackSettings = combat.Tuning.ToCombatSettings();
                float completeCounter = attackSettings.StartupSeconds + attackSettings.ActiveSeconds +
                    attackSettings.RecoverySeconds + 6f / Engine.PhysicsTicksPerSecond;
                if (counterProof == MixedCounterProof.RejectShort && !danger.Threat &&
                    commonRecovery is > 0f && commonRecovery < completeCounter)
                {
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, danger),
                        "RANGED_MIXED_SHORT_COUNTER_RED: real common Recovery shorter than complete J plus input margin is not a counter window");
                    shortCounterRejected = true;
                }
                if (counterProof == MixedCounterProof.Connect && !counterConnected &&
                    !danger.Threat && commonRecovery >= completeCounter)
                {
                    TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, danger),
                        "MIXED_COUNTER_NATIVE_RED: real complete shared Recovery admits a counter with retained resources");
                    driver.PointerButton(MouseButton.Right, false, player, rust.GlobalPosition);
                    for (int wait = 0; wait < 3 && input.Current.ShieldHeld; wait++) await Frames(tree, 1);
                    var fresh = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(
                        actors, encounterId, rust, false, leaves);
                    TestAssert.True(!input.Current.ShieldHeld && !shield.IsActive &&
                        PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, fresh) &&
                        player.GetNode<PlayerActionModeController>("ActionModes").CanBeginAttack,
                        "MIXED_COUNTER_NATIVE_RED: real shield release is followed by fresh full-window admission");
                    int enemyHp = rust.GetNode<HealthComponent>("Health").CurrentHealth;
                    driver.Command(Knight.Shared.InputActions.MoveLeft, true);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, true);
                    await Frames(tree, 2);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, false);
                    driver.Command(Knight.Shared.InputActions.MoveLeft, false);
                    for (int wait = 0; wait < 24 && combat.IsAttacking; wait++) await Frames(tree, 1);
                    await Frames(tree, 6); // Native post-attack re-guard margin, not just AttackStarted.
                    TestAssert.True(!combat.IsAttacking && counterStarts == 1 && rustCounterContacts == 1 &&
                        rust.GetNode<HealthComponent>("Health").CurrentHealth == enemyHp - 1 &&
                        health.CurrentHealth == 5 && heals == 0 && deaths == 0 && root.RecoveryRequests == 0,
                        "MIXED_COUNTER_NATIVE_RED: accepted new J completes and actual enemy Health loses one without player rescue/damage");
                    AssertFairnessSupport(player, floor, new Rect2(4600, 3460, 200, 40));
                    counterConnected = true;
                    GD.Print($"RANGED_MIXED_COUNTER_CONTACT hp={enemyHp}->{enemyHp-1} player_hp=5 " +
                        $"common_before={commonRecovery:R} required={completeCounter:R} actual_attack_completed=true " +
                        $"actual_rust_hurtbox_contacts={rustCounterContacts} post_attack_physics=6");
                    break; // Single counter proof, not a whole-fight kill certificate.
                }
                if (committedGap)
                {
                    TestAssert.True(danger.Threat && danger.HasRanged &&
                        danger.GuardSource.X > player.GlobalPosition.X,
                        "RANGED_MIXED_COMMITTED_BURST_GAP_RED: an authorized Active burst does not become safe between retired and pending rounds");
                    TestAssert.True(danger.SourceActorId == burstId && danger.AttackId != 0 &&
                        danger.Phase == AttackPhase.Active && danger.FromActiveGap,
                        "same-pass metadata preserves the real zero-leaf Active Burst source");
                    testedCommittedGap = true;
                }
                bool defend = resourcePolicy
                    ? PrologueCanyonGeneratedMainRouteTests.Task10MixedDefendWindow(shield, danger)
                    : PrologueCanyonGeneratedMainRouteTests.Task10GeneralDefendWindow(
                        combat, energy, danger.Threat, false, 30, 0);
                driver.PointerButton(MouseButton.Right, defend, player, danger.GuardSource);
                await Frames(tree, 1);
                driver.ObserveFrame();
                TestAssert.True(player.GetInstanceId() == playerId && health.CurrentHealth == 5 &&
                    heals == 0 && deaths == 0 && root.RecoveryRequests == 0 &&
                    player.GlobalPosition.DistanceTo(previous) < 32f && motor.IsPhysicsProcessing() &&
                    input.IsPhysicsProcessing() && energy.IsPhysicsProcessing() && shield.IsPhysicsProcessing() &&
                    hurtbox.ResolveDamageReceiver() == observer && observer.Inner == guardedReceiver,
                    "RANGED_MIXED_SURVIVAL_RED: normal same player and original guarded receiver survive without rescue");
                previous = player.GlobalPosition;
                AssertFairnessSupport(player, floor, new Rect2(4600, 3460, 200, 40));
                if (resourcePolicy && danger.SourceActorId == burstId && danger.Phase == AttackPhase.Telegraph &&
                    danger.RemainingSeconds <= 8f / Engine.PhysicsTicksPerSecond &&
                    input.Current.ShieldHeld && shield.IsActive && shield.GuardDirection.X > .9f)
                    lateShieldSample = true;
                if (resourcePolicy && danger.Phase == AttackPhase.Active && energy.EnergyRatio <= .35f &&
                    shield.CanBeginGuard && input.Current.ShieldHeld && shield.IsActive)
                    lowEnergyActiveGuard = true;
                candidateSamples = candidate ? candidateSamples + 1 : 0;
                if (!testedNonselected && candidateSamples >= 3)
                {
                    TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10EnemyThreat(burst, false, leaves),
                        "MIXED_POLICY_SETUP: actual nonselected Burst warning still has current authority");
                    TestAssert.True(danger.Threat && danger.HasRanged && danger.GuardSource.X > player.GlobalPosition.X,
                        "RANGED_MIXED_NONSELECTED_THREAT_RED: selected Rust Recovery must not erase the real right-side Burst warning");
                    if (resourcePolicy)
                    {
                        TestAssert.True(danger.RemainingSeconds > 8f / Engine.PhysicsTicksPerSecond,
                            "MIXED_RESOURCE_SETUP: genuine early nonselected warning outside the late guard lead");
                        TestAssert.True(!defend && !input.Current.ShieldHeld && !shield.IsActive,
                            "RANGED_MIXED_EARLY_GUARD_RELEASE_RED: counter-veto warning must release actual L while no immediate guard is needed");
                    }
                    else TestAssert.True(input.Current.ShieldHeld && shield.IsActive && shield.GuardDirection.X > .9f,
                        "original whole-warning keyboard guard remains the old regression");
                    TestAssert.True(danger.SourceActorId == burstId && danger.AttackId != 0 &&
                        danger.Phase == AttackPhase.Telegraph && !danger.FromActiveGap,
                        "RANGED_MIXED_METADATA_RED: same-pass winner identifies the actual nonselected Burst warning rather than default/stale metadata");
                    testedNonselected = true;
                    tree.Paused = true;
                    try
                    {
                        var paused = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(
                            actors, encounterId, rust, false, leaves);
                        TestAssert.True(!paused.Threat && leaves.Count == 0,
                            "paused actual mixed warning cannot supply current guard authority");
                        testedPause = true;
                    }
                    finally { tree.Paused = false; }
                }
            }
            GD.Print($"RANGED_MIXED_CONTACT_SAMPLE rust_guards={rustGuards} burst_guards={burstGuards} emissions={emissions} " +
                $"requests={observer.Requests.Count} burst_requests={observer.Requests.Count(request => request.SourceRuntimeId == burstId)} " +
                $"rust={rust.CurrentState} burst={burst.CurrentFrame.State}/{burst.CurrentFrame.AttackTimeline.PhaseRemainingSeconds:R} " +
                $"hp={health.CurrentHealth} energy={energy.EnergyRatio:R} heat={energy.HeatRatio:R} overheated={energy.IsOverheated} " +
                $"player={player.GlobalPosition} rust_pos={rust.GlobalPosition} burst_pos={burst.GlobalPosition} " +
                $"shield={input.Current.ShieldHeld}/{shield.IsActive}/{shield.GuardDirection}");
            TestAssert.True(testedCommittedGap,
                "MIXED_POLICY_SETUP: natural Rust Recovery/Burst Active contains a genuine zero-leaf gap");
            if (resourcePolicy)
                TestAssert.True(lateShieldSample && (preparationAttacks == 0 || lowEnergyActiveGuard) &&
                    energy.EnergyRatio > 0f,
                    $"MIXED_RESOURCE_NATIVE_RED: late sampled shield and actual low-energy Active availability; " +
                    $"late={lateShieldSample} low={lowEnergyActiveGuard} preparation={preparationAttacks}");
            TestAssert.True(counterProof != MixedCounterProof.RejectShort || shortCounterRejected,
                "MIXED_COUNTER_SETUP: naturally observed real short shared Recovery, not a synthetic timeline");
            TestAssert.True(counterProof != MixedCounterProof.Connect || counterConnected,
                "MIXED_COUNTER_SETUP: naturally observed complete shared Recovery and actual Health counter contact");
            TestAssert.True(testedNonselected && testedPause && rustGuards >= 1 && burstGuards == 3 &&
                observer.Requests.Count(request => request.SourceRuntimeId == burstId) == 3 && !energy.IsOverheated,
                $"actual Rust contact and each first Burst round resolve Guarded with native five HP intact; " +
                $"rust={rustGuards} burst={burstGuards} emissions={emissions} requests={observer.Requests.Count}");
            var wrongGroup = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(
                actors, "not.this.encounter", rust, false, leaves);
            TestAssert.True(!wrongGroup.Threat && !wrongGroup.HasRanged && leaves.Count == 0,
                "unrelated membership cannot lend a selected actor's stale authority");
            actors.QueueFree();
            var queued = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(
                actors, encounterId, rust, false, leaves);
            TestAssert.True(!queued.Threat && !queued.HasRanged && leaves.Count == 0,
                "queued actor container rejects still-live child wrappers synchronously");
            GD.Print($"RANGED_MIXED_THREAT_POLICY_OK rust_guards={rustGuards} burst_guards={burstGuards} hp=5->{health.CurrentHealth} " +
                $"energy={energy.EnergyRatio:R} heat={energy.HeatRatio:R} committed_gap={testedCommittedGap} " +
                $"resource_policy={resourcePolicy} preparation_attacks={preparationAttacks} late_shield={lateShieldSample} " +
                $"low_energy_active_guard={lowEnergyActiveGuard} counter_proof={counterProof} " +
                $"short_rejected={shortCounterRejected} counter_connected={counterConnected} " +
                $"native_motor=true explicit_pretrial_setup=true whole_loop=false whole_goal=false");
        }
        finally
        {
            tree.Paused = false;
            try { driver?.Dispose(); }
            finally
            {
                try { await DisposeFairnessFixture(tree, root, sector, player); }
                finally { tree.Root.CanvasTransform = canvas; }
            }
        }
    }

    private static async Task EarlyWarningCounters(SceneTree tree, RangedFairnessRecoveryProbe root,
        PrologueSector sector, Node2D actors, CharacterBody2D player, KeyboardRouteInputDriver driver,
        RustShellSquire rust, RangedWeaponEnemy burst, string encounterId, bool boundaryOnly = false)
    {
        var combat = player.GetNode<PlayerCombat>("Combat");
        var energy = player.GetNode<PlayerEnergyController>("Energy");
        var shield = player.GetNode<PlayerShieldController>("Shield");
        var input = player.GetNode<PlayerInputReader>("Input");
        var modes = player.GetNode<PlayerActionModeController>("ActionModes");
        var health = player.GetNode<HealthComponent>("Health");
        var enemyHealth = rust.GetNode<HealthComponent>("Health");
        var target = rust.GetNode<Hurtbox>("Hurtbox");
        var floor = sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding");
        var leaves = new List<AttackTelegraphVisual>(8);
        var attackIds = new List<ulong>(2);
        ulong playerId = player.GetInstanceId(), trialStart = Engine.GetPhysicsFrames(), started = 0;
        int originalHp = enemyHealth.CurrentHealth, contacts = 0, healed = 0, completed = 0;
        int activeBreaches = 0, emittedDuringAttack = 0, projectileRejects = 0, gapRejects = 0, shortRejects = 0;
        bool watching = false;
        float fullWindow = combat.Tuning.AttackStartupSeconds + combat.Tuning.AttackActiveSeconds +
            combat.Tuning.AttackRecoverySeconds + 6f / Engine.PhysicsTicksPerSecond;
        bool SafePhase(AttackTimelineFrame t) => t.Phase is AttackPhase.Telegraph or AttackPhase.Recovery &&
            float.IsFinite(t.PhaseRemainingSeconds) && t.PhaseRemainingSeconds >= fullWindow;
        bool LiveProjectile()
        {
            leaves.Clear();
            try
            {
                burst.AppendAuthorizedAttackLeaves(leaves);
                return leaves.Any(leaf => leaf.GetParent() is RangedWeaponProjectile);
            }
            finally { leaves.Clear(); }
        }
        void RustAdvanced(RustShellFrame frame, int _) { if (watching && frame.AttackTimeline.Phase == AttackPhase.Active) activeBreaches++; }
        void BurstAdvanced(RangedWeaponFrame frame)
        {
            if (watching && (frame.AttackTimeline.Phase == AttackPhase.Active || LiveProjectile())) activeBreaches++;
        }
        void Emitted(ulong _, Vector2 __) { if (watching) emittedDuringAttack++; }
        void Started(ulong id, int _) { attackIds.Add(id); started = Engine.GetPhysicsFrames(); }
        void Connected(Hurtbox box) { if (box == target) contacts++; }
        void Healed(int _, int __) => healed++;
        rust.FrameChanged += RustAdvanced;
        burst.FrameChanged += BurstAdvanced;
        burst.ShotEmitted += Emitted;
        combat.AttackStarted += Started;
        combat.AttackConnected += Connected;
        health.Healed += Healed;
        try
        {
            var firstRust = rust.CurrentFrame.AttackTimeline;
            var firstBurst = burst.CurrentFrame.AttackTimeline;
            TestAssert.True(firstRust.Phase == AttackPhase.Telegraph && firstBurst.Phase == AttackPhase.Telegraph &&
                SafePhase(firstRust) && SafePhase(firstBurst) && !LiveProjectile() && burst.HasPresentationAuthority,
                $"EARLY_COUNTER_SETUP: real first warnings retain complete J+6ticks; rust={firstRust.Phase}/{firstRust.PhaseRemainingSeconds:R} burst={firstBurst.Phase}/{firstBurst.PhaseRemainingSeconds:R}");
            if (boundaryOnly)
            {
                // Reach a real finite warning between24 and25 ticks, not a forged danger value.
                for (int tick = 0; tick < 16 &&
                    rust.CurrentFrame.AttackTimeline.PhaseRemainingSeconds > fullWindow + 1.5f / Engine.PhysicsTicksPerSecond; tick++)
                {
                    await Frames(tree, 1);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                }
                var tightRust = rust.CurrentFrame.AttackTimeline;
                var tightBurst = burst.CurrentFrame.AttackTimeline;
                TestAssert.True(tightRust.Phase == AttackPhase.Telegraph && tightBurst.Phase == AttackPhase.Telegraph &&
                    SafePhase(tightRust) && SafePhase(tightBurst) &&
                    tightRust.PhaseRemainingSeconds < fullWindow + 1.5f / Engine.PhysicsTicksPerSecond && !LiveProjectile(),
                    "EARLY_TIGHT_SETUP: native warning remains nominal fullJ+6 but not sampling/command lead");
                var tightDanger = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(actors, encounterId, rust, false, leaves);
                bool admitted = PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, tightDanger);
                GD.Print($"EARLY_TIGHT_SAMPLE rust_remaining={tightRust.PhaseRemainingSeconds:R} burst_remaining={tightBurst.PhaseRemainingSeconds:R} nominal_full_plus6={fullWindow:R} physics={Engine.GetPhysicsFrames()} admitted={admitted}");
                if (admitted)
                {
                    watching = true;
                    driver.AimAt(player, rust.GlobalPosition);
                    driver.Command(Knight.Shared.InputActions.MoveLeft, true);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, true);
                    await Frames(tree, 2);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, false);
                    driver.Command(Knight.Shared.InputActions.MoveLeft, false);
                    for (int tick = 0; tick < 24 && combat.IsAttacking; tick++) await Frames(tree, 1);
                    ulong finished = Engine.GetPhysicsFrames();
                    await Frames(tree, 6);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    watching = false;
                    GD.Print($"EARLY_TIGHT_ACTUAL attack_started={started} completed={finished} margin_end={Engine.GetPhysicsFrames()} active_breaches={activeBreaches} contacts={contacts} player_hp={health.CurrentHealth}");
                    TestAssert.True(activeBreaches == 0 && emittedDuringAttack == 0,
                        "RANGED_EARLY_SAMPLED_BOUNDARY_RED: admitted native J must keep actual full attack plus six post-completion ticks before every Active");
                    TestAssert.True(false, "RANGED_EARLY_SAMPLED_BOUNDARY_RED: tight sampled warning must be denied before J");
                }
                TestAssert.True(attackIds.Count == 0 && contacts == 0 && health.CurrentHealth == 5 && healed == 0 && root.RecoveryRequests == 0,
                    "EARLY_TIGHT_REJECT: unsafe native boundary denies J without damage/heal/recovery");
                AssertFairnessSupport(player, floor, new Rect2(4600, 3460, 200, 40));
                GD.Print("RANGED_EARLY_SAMPLED_BOUNDARY_OK native_warning=true admitted=false no_attack=true full_post_completion_margin=6");
                return;
            }
            var first = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(actors, encounterId, rust, false, leaves);
            TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, first),
                "RANGED_MIXED_EARLY_COUNTER_RED: enough real early warnings admit full J plus six ticks, not whole-warning starvation");
            // Keep the existing counter-fixture180-physics bound, including actions/margin.
            while (Engine.GetPhysicsFrames() - trialStart < 180 && completed < 2)
            {
                var rt = rust.CurrentFrame.AttackTimeline;
                var bt = burst.CurrentFrame.AttackTimeline;
                var danger = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(actors, encounterId, rust, false, leaves);
                bool projectile = LiveProjectile();
                bool candidate = SafePhase(rt) && SafePhase(bt) && !projectile;
                float gap = player.GlobalPosition.X - rust.GlobalPosition.X;
                if (projectile || danger.FromActiveGap ||
                    (rt.Phase == AttackPhase.Telegraph && rt.PhaseRemainingSeconds < fullWindow) ||
                    (bt.Phase == AttackPhase.Telegraph && bt.PhaseRemainingSeconds < fullWindow))
                {
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, danger),
                        "EARLY_COUNTER_UNSAFE_RED: live projectile, Active gap and insufficient real warning must still veto J");
                    if (projectile) projectileRejects++;
                    if (danger.FromActiveGap) gapRejects++;
                    if (!projectile && !danger.FromActiveGap) shortRejects++;
                }
                if (candidate && gap is >= 8f and <= 64f &&
                    PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, danger) &&
                    Engine.GetPhysicsFrames() - trialStart + (ulong)MathF.Ceiling(fullWindow * Engine.PhysicsTicksPerSecond) + 3 < 180)
                {
                    driver.PointerButton(MouseButton.Right, false, player, rust.GlobalPosition);
                    for (int wait = 0; wait < 3 && input.Current.ShieldHeld; wait++) await Frames(tree, 1);
                    var fresh = PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(actors, encounterId, rust, false, leaves);
                    if (!SafePhase(rust.CurrentFrame.AttackTimeline) || !SafePhase(burst.CurrentFrame.AttackTimeline) || LiveProjectile() ||
                        !PrologueCanyonGeneratedMainRouteTests.Task10MixedCounterWindow(combat, energy, fresh) || !modes.CanBeginAttack)
                        continue;
                    int beforeContacts = contacts, beforeHp = enemyHealth.CurrentHealth;
                    watching = true;
                    driver.AimAt(player, rust.GlobalPosition);
                    driver.Command(Knight.Shared.InputActions.MoveLeft, true);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, true);
                    await Frames(tree, 2);
                    driver.Command(Knight.Shared.InputActions.PrimaryAction, false);
                    driver.Command(Knight.Shared.InputActions.MoveLeft, false);
                    for (int wait = 0; wait < 24 && combat.IsAttacking; wait++) await Frames(tree, 1);
                    ulong finished = Engine.GetPhysicsFrames();
                    await Frames(tree, 6);
                    // PhysicsFrame is pre-node. Keep event observation through the last tick's node processing.
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    watching = false;
                    TestAssert.True(!combat.IsAttacking && attackIds.Count == completed + 1 &&
                        (completed == 0 || attackIds[0] != attackIds[1]) && contacts == beforeContacts + 1 &&
                        enemyHealth.CurrentHealth == beforeHp - 1 && activeBreaches == 0 && emittedDuringAttack == 0 &&
                        Engine.GetPhysicsFrames() >= finished + 6 && started <= finished,
                        $"EARLY_COUNTER_NATIVE: complete real J+6 precedes every Active/projectile; counters={completed} contacts={contacts} hp={enemyHealth.CurrentHealth} active={activeBreaches} shots={emittedDuringAttack}");
                    completed++;
                    GD.Print($"EARLY_COUNTER_CONTACT ordinal={completed} attack_id={attackIds[^1]} started={started} completed={finished} margin_end={Engine.GetPhysicsFrames()} enemy_hp={beforeHp}->{enemyHealth.CurrentHealth} player_hp={health.CurrentHealth} active_breaches={activeBreaches}");
                }
                else
                {
                    bool approach = candidate && gap > 64f;
                    driver.Command(Knight.Shared.InputActions.MoveLeft, approach);
                    driver.PointerButton(MouseButton.Right,
                        PrologueCanyonGeneratedMainRouteTests.Task10MixedDefendWindow(shield, danger), player, danger.GuardSource);
                    await Frames(tree, 1);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                }
                driver.ObserveFrame();
                TestAssert.True(player.GetInstanceId() == playerId && health.CurrentHealth == 5 && healed == 0 && root.RecoveryRequests == 0,
                    "EARLY_COUNTER_SURVIVAL: same original five-HP player, no healing/recovery/state injection");
                AssertFairnessSupport(player, floor, new Rect2(4600, 3460, 200, 40));
            }
            TestAssert.True(completed == 2 && contacts == 2 && enemyHealth.CurrentHealth == originalHp - 2 &&
                projectileRejects > 0 && gapRejects > 0 && shortRejects > 0,
                $"EARLY_COUNTER_TWO_SETUP: two real contacts and unsafe boundaries within original180 ticks; complete={completed} projectile={projectileRejects} gap={gapRejects} short={shortRejects}");
            GD.Print($"RANGED_MIXED_EARLY_COUNTER_OK contacts=2 distinct_attacks=2 player_hp=5 no_heal=true no_recovery=true native_ticks={Engine.GetPhysicsFrames()-trialStart} whole_loop=false");
        }
        finally
        {
            rust.FrameChanged -= RustAdvanced;
            burst.FrameChanged -= BurstAdvanced;
            burst.ShotEmitted -= Emitted;
            combat.AttackStarted -= Started;
            combat.AttackConnected -= Connected;
            health.Healed -= Healed;
        }
    }

    public static void RunCatalog() => Knight.Tests.Unit.CanyonEncounterCatalogTests.Run();

    // Break caught: the real group mounts a fail-closed ranged actor without its
    // pre-AddChild arena; a living visible in-range unobstructed player gets no warning.
    public static async Task RunSpawnContract(SceneTree tree)
    {
        Transform2D canvas = tree.Root.CanvasTransform;
        tree.Root.CanvasTransform = Transform2D.Identity;
        Fixture f = CreateContractFixture(tree, mixed: false);
        try
        {
            await Frames(tree, 5);
            RangedWeaponEnemy actor = Children(f.Actors).OfType<RangedWeaponEnemy>().Single();
            TestAssert.Equal(Scatter, actor.SceneFilePath, "spawn contract uses the actual generated saved leaf, not a builder double");
            AssertAttackPrerequisites(tree, f, actor);
            await Until(tree, () => actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12);
            TestAssert.Equal(RangedWeaponState.Telegraph, actor.CurrentFrame.State,
                "RANGED_PLACEMENT_ENVELOPE_RED: real group must configure the ranged arena before AddChild so natural warning begins");
            TestAssert.True(f.ReadyPositions.TryGetValue(actor.GetInstanceId(), out Vector2 ready) &&
                            ready.DistanceTo(new Vector2(200, 300)) < .01f,
                "nonzero actorRoot transform still delivers the authored world placement before actor Ready");
            int emissions = 0;
            actor.ShotEmitted += (_, _) => emissions++;
            await Until(tree, () => emissions > 0, 100);
            TestAssert.True(emissions > 0, "group-spawned real saved Scatter naturally emits after warning");
            f.Player.GlobalPosition = new Vector2(1200, 300);
            await Frames(tree, 4);
            TestAssert.True(!f.Group.IsActive && !GodotObject.IsInstanceValid(actor),
                "leaving the configured arena retires the real weapon owner and its owned attacks");
            GD.Print("RANGED_PLACEMENT_SPAWN_CONTRACT_OK pre_ready_world=true native_warning=true");
        }
        finally { await Dispose(tree, f); tree.Root.CanvasTransform = canvas; }
    }

    public static async Task Run(SceneTree tree)
    {
        await MixedWarningReselectsAndNeedsBothDefeats(tree);
        await MixedReservedOverlapSuppressesWholeBatch(tree);
        await CounterweightRustSettlementRejectsInvalidGeometry(tree);
        await OtherEncounterCannotBorrowRustSettlementException(tree);
        await SavedCounterweightLandingStillAdmitsMixedFight(tree, center: false);
        await SavedCounterweightLandingStillAdmitsMixedFight(tree, center: true);
        foreach (MapCase item in Cases)
        foreach (bool blockPrimary in new[] { true, false })
            await SavedSectorUsesCurrentCatalogAndFires(tree, item, blockPrimary);
        await MixedWholeBatchSuppressionIsAtomic(tree);
        await MixedPendingWarningsCancelOnLifecycle(tree);
        GD.Print("RANGED_PLACEMENT_RUNTIME_OK saved_roles=4 candidates=8 native_geometry=true mixed_two_live=true whole_goal=false");
    }

    // Break caught: the real mixed landing has no survivable keyboard jump / real
    // claw-and-release route to an existing supported foothold. No placement
    // fixture's disabled motor, enlarged HP, or body-only query proves this gate.
    public static async Task RunFairness(SceneTree tree)
    {
        await SavedMixedFightKeyboardEscape(tree, clawRoute: false);
        await SavedMixedFightKeyboardEscape(tree, clawRoute: true);
        GD.Print("RANGED_FAIRNESS_RUNTIME_OK cases=2 engine_keyboard_events=true os_human_play=false normal_player=true supported_safe_landing=true");
    }

    // Break caught by real Main: an initially player-blocked complete batch is
    // suppressed forever, even after ordinary keyboard movement makes it legal.
    // Independent API: never replaces or hides the old failing claw fairness case.
    public static async Task RunSuppressionRecovery(SceneTree tree)
    {
        foreach (string mode in new[] { "recover", "pause", "reset", "exit", "static-world-blocked" })
            await SavedSuppressedBatchRecoversAfterPlayerClearance(tree, mode);
        await MixedPendingWarningsCancelOnLifecycle(tree); // Existing cancellation/re-entry coverage retained verbatim.
        GD.Print("RANGED_SUPPRESSION_RECOVERY_RUNTIME_OK normal_saved_player=true keyboard_clearance=true fresh_full_warning=true atomic_pair=true lifecycle=true static_illegal_stays_illegal=true");
    }

    private static async Task SavedSuppressedBatchRecoversAfterPlayerClearance(SceneTree tree, string mode)
    {
        Transform2D canvas = tree.Root.CanvasTransform;
        bool originalPaused = tree.Paused;
        RangedFairnessRecoveryProbe root = null;
        PrologueSector sector = null;
        CharacterBody2D player = null;
        var resources = new List<Resource>();
        try
        {
            TestAssert.True(!originalPaused, "SUPPRESSION_SETUP: normal suite begins unpaused");
            ReleaseFairnessKeys();
            root = new RangedFairnessRecoveryProbe { Name = "SuppressionRecovery_" + mode };
            sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
            root.AddChild(sector);
            player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position = new Vector2(4100, 3500);
            player.AddToGroup("player");
            root.AddChild(player);
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/CounterweightLiftEncounter");
            Node2D actors = sector.GetNode<Node2D>("Gameplay/Actors");
            var progress = new PrologueProgress();
            group.ApplyProgress(progress);
            var f = new Fixture(root, actors, player, group, progress, resources, new());
            var warningBirths = new List<(ulong Frame, Vector2 Site, Vector2 Player)>();
            var warningEnds = new List<ulong>();
            var mixedBirths = new List<(ulong Id, string Scene, Vector2 Position, ulong Frame)>();
            int completions = 0, defeatedGroups = 0, suppressions = 0, deaths = 0, heals = 0;
            group.Completed += _ => completions++;
            group.GroupDefeated += _ => defeatedGroups++;
            group.ArrivalSuppressed += _ => suppressions++;
            actors.ChildEnteredTree += child => child.Ready += () =>
            {
                if (child is CanyonSpawnWarningVisual warning)
                {
                    warningBirths.Add((Engine.GetPhysicsFrames(), warning.GlobalPosition, player.GlobalPosition));
                    GD.Print($"RANGED_SUPPRESSION_WARNING_START mode={mode} physics={Engine.GetPhysicsFrames()} site={warning.GlobalPosition} player={player.GlobalPosition}");
                    warning.TreeExiting += () =>
                    {
                        warningEnds.Add(Engine.GetPhysicsFrames());
                        GD.Print($"RANGED_SUPPRESSION_WARNING_END mode={mode} physics={Engine.GetPhysicsFrames()} site={warning.GlobalPosition}");
                    };
                }
                if (child is CharacterBody2D actor && group.CurrentBatch == 1)
                {
                    mixedBirths.Add((actor.GetInstanceId(), actor.SceneFilePath, actor.GlobalPosition, Engine.GetPhysicsFrames()));
                    GD.Print($"RANGED_SUPPRESSION_ACTUAL_READY mode={mode} physics={Engine.GetPhysicsFrames()} id={actor.GetInstanceId()} scene={actor.SceneFilePath} position={actor.GlobalPosition} player={player.GlobalPosition}");
                }
            };
            tree.Root.AddChild(root);
            foreach (KillZone zone in Descendants(sector).OfType<KillZone>()) zone.RoomControllerPath = zone.GetPathTo(root);
            foreach (CanyonEncounterGroup other in Descendants(sector).OfType<CanyonEncounterGroup>())
                if (other != group) other.SetPhysicsProcess(false);
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
            PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
            PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
            StaticBody2D landing = sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding");
            Rect2 landingRect = new(4600, 3460, 200, 40);
            health.Died += () => deaths++;
            health.Healed += (_, _) => heals++;
            ulong identity = player.GetInstanceId();
            Vector2 last = player.GlobalPosition;
            bool episode = false;
            ulong pausedPhysics = 0;
            int samples = 0;
            async Task Step(int frames)
            {
                for (int frame = 0; frame < frames; frame++)
                {
                    await Frames(tree, 1);
                    samples++;
                    TestAssert.True(player.GetInstanceId() == identity && health.CurrentHealth == 5 && health.MaxHealth == 5 &&
                                    MathF.Abs(health.InvulnerabilitySeconds - .35f) < .0001f && root.RecoveryRequests == 0 && deaths == 0 && heals == 0 &&
                                    float.IsFinite(energy.EnergyRatio) && energy.EnergyRatio > .99f && energy.EnergyRatio <= 1f,
                        $"RANGED_SUPPRESSION_NORMAL_PLAYER_RED: same default5HP player, normal energy, no damage/heal/death/recovery mode={mode} sample={samples}");
                    TestAssert.True(motor.IsPhysicsProcessing() && input.IsPhysicsProcessing() && energy.IsPhysicsProcessing() &&
                                    health.IsPhysicsProcessing() && player.GetNode<MechanicalClawController>("MechanicalClaw").IsPhysicsProcessing() &&
                                    player.GetNode<PlayerActionModeController>("ActionModes").IsPhysicsProcessing() &&
                                    player.GetNode<Camera2D>("Camera2D").Enabled && player.GetNode<Camera2D>("Camera2D").IsCurrent() &&
                                    player.CollisionLayer == CombatCollisionLayers.Player && player.CollisionMask == CombatCollisionLayers.World &&
                                    !player.GetNode<CollisionShape2D>("BodyCollider").Disabled && !player.GetNode<CollisionShape2D>("Hurtbox/HurtboxCollider").Disabled &&
                                    player.GetNode<Hurtbox>("Hurtbox").Monitorable &&
                                    player.GetNode<Hurtbox>("Hurtbox").ResolveDamageReceiver() == player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage"),
                        "suppression episode retains all packed gameplay owners and genuine full-body/guarded damage route");
                    TestAssert.True(float.IsFinite(player.GlobalPosition.X) && float.IsFinite(player.GlobalPosition.Y) &&
                                    player.GlobalPosition.DistanceTo(last) < 32f,
                        "RANGED_SUPPRESSION_NO_WARP_RED: only ordinary bounded native movement after fixture arrangement");
                    last = player.GlobalPosition;
                    if (!episode) continue;
                    AssertFairnessSupport(player, landing, landingRect);
                    AssertFairnessBodyClear(tree, player);
                    TestAssert.True(completions == 0 && defeatedGroups == 0 && !group.IsComplete &&
                                    !progress.ActivatedMechanisms.Contains(group.EncounterId.ToString()) &&
                                    (group.LiveActorCount == 0 || group.LiveActorCount == 2),
                        "RANGED_SUPPRESSION_ATOMIC_NO_FALSE_PROGRESS_RED: blocked/retry wave is all-or-none and cannot fake completion");
                    ulong[] starts = warningBirths.Select(warning => warning.Frame).Distinct().ToArray();
                    if (starts.Length >= 2 && Engine.GetPhysicsFrames() - starts[1] - pausedPhysics < 36UL)
                        TestAssert.True(group.LiveActorCount == 0 && mixedBirths.Count == 0,
                            "RANGED_SUPPRESSION_FULL_REWARNING_RED: no actor before36 unpaused ordinary physics ticks of the fresh .60s warning");
                    if (samples % 15 == 0)
                        GD.Print($"RANGED_SUPPRESSION_SAMPLE mode={mode} sample={samples} physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} velocity={player.Velocity} hp={health.CurrentHealth} energy={energy.EnergyRatio} floor={player.IsOnFloor()} batch={group.CurrentBatch} live={group.LiveActorCount} suppressed={group.SpawnSuppressed} warning={group.IsArrivalWarningActive} warning_waves={starts.Length} suppressions={suppressions} recovery={root.RecoveryRequests}");
                }
            }
            await Step(4);
            TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1 &&
                            actors.GetChild(0).SceneFilePath == CanyonEncounterCatalog.MothScene,
                "SUPPRESSION_SETUP: real saved group naturally activates its unchanged Moth at actual4100 activation");
            // Explicit local fixture arrangement BEFORE the observed episode.
            // Actual normal Main already reached this pose; this local test does
            // not claim to reproduce its complete entrance traversal.
            player.GlobalPosition = new Vector2(4732.7334f, 3424);
            last = player.GlobalPosition;
            await Step(6);
            AssertFairnessSupport(player, landing, landingRect);
            AssertFairnessBodyClear(tree, player);
            TestAssert.True(MathF.Abs(player.GlobalPosition.X - 4700f) < 64f && MathF.Abs(player.GlobalPosition.X - 4770f) < 64f &&
                            MathF.Abs(player.GlobalPosition.X - 4630f) > 64f,
                "SUPPRESSION_SETUP: true Main middle pose blocks both primaries; remaining4630 Rust/Burst bodies overlap each other");
            ulong inputStart = input.CurrentFrameSequence;
            episode = true;
            Kill(actors.GetChild(0));
            await Step(2);
            TestAssert.True(group.CurrentBatch == 1 && group.IsArrivalWarningActive && group.LiveActorCount == 0,
                "SUPPRESSION_SETUP: genuine Moth Health defeat starts original whole-pair arrival warning");
            for (int frame = 0; frame < 60 && !group.SpawnSuppressed; frame++) await Step(1);
            TestAssert.True(group.SpawnSuppressed && group.IsActive && group.CurrentBatch == 1 && group.LiveActorCount == 0 &&
                            !group.IsArrivalWarningActive && mixedBirths.Count == 0,
                "SUPPRESSION_SETUP: real blocked complete batch is suppressed, never partially born or completed");
            await Step(2);
            ulong[] initialStarts = warningBirths.Select(warning => warning.Frame).Distinct().ToArray();
            TestAssert.True(initialStarts.Length == 1 && warningEnds.Count == 4 && warningEnds.All(end => end - initialStarts[0] >= 36UL),
                "SUPPRESSION_SETUP: original four literal candidate warning leaves finish the full .60s before suppression");
            for (int frame = 0; frame < 180; frame++)
            {
                await Step(1);
                TestAssert.True(group.SpawnSuppressed && group.LiveActorCount == 0 && !group.IsArrivalWarningActive && mixedBirths.Count == 0 &&
                                warningBirths.Select(warning => warning.Frame).Distinct().Count() == 1 &&
                                !Children(actors).OfType<CanyonSpawnWarningVisual>().Any(warning => !warning.IsQueuedForDeletion()),
                    "RANGED_SUPPRESSION_BLOCKED_HOLD_RED:180 blocked normal frames have no birth, false progress or warning flash/replay");
            }
            if (mode == "static-world-blocked")
            {
                AddOwnedFloor(f, "PersistentRustBurstLeftBlocker", new Vector2(4630, 3400), new Vector2(55, 40));
                // Far side of the Burst capsule, outside the blocked player's
                // original/full walked body: geometry cannot push the player clear.
                AddOwnedFloor(f, "PersistentBurstRightBlocker", new Vector2(4785, 3420), new Vector2(15, 20));
            }
            FairnessKey(Key.A, true);
            await Step(8);
            FairnessKey(Key.A, false);
            await Step(8);
            TestAssert.True(player.GlobalPosition.X >= 4694f && player.GlobalPosition.X <= 4706f &&
                            MathF.Abs(player.GlobalPosition.Y - 3424f) < 1f && player.Velocity.Length() < .1f &&
                            input.CurrentFrameSequence > inputStart && motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0,
                $"SUPPRESSION_SETUP: actual A8/release8 movement reaches and stops in legal center window, no warp/jump/dash; player={player.GlobalPosition} velocity={player.Velocity}");
            AssertFairnessSupport(player, landing, landingRect);
            AssertFairnessBodyClear(tree, player);
            if (mode == "static-world-blocked")
            {
                AssertSuppressionCandidateWorldBlocked(tree, new Vector2(4630, 3431));
                AssertSuppressionCandidateWorldBlocked(tree, new Vector2(4770, 3431));
                for (int frame = 0; frame < 180; frame++)
                {
                    await Step(1);
                    TestAssert.True(group.SpawnSuppressed && group.LiveActorCount == 0 && !group.IsArrivalWarningActive && mixedBirths.Count == 0 &&
                                    warningBirths.Select(warning => warning.Frame).Distinct().Count() == 1,
                        "RANGED_SUPPRESSION_STATIC_ILLEGAL_RED: retry cannot make unchanged native World-blocked geometry legal or flash warnings");
                }
                GD.Print("RANGED_SUPPRESSION_STATIC_NEGATIVE_OK normal_player_clear=true persistent_native_blockers=true");
                return;
            }
            // Independently legal complete center pair: final Rust3428 is the
            // literal native4px-settled body, not an actor teleport or model step.
            ValidateCandidate(tree, f, CanyonEncounterCatalog.RustShellScene, new Vector2(4630, 3428));
            ValidateCandidate(tree, f, Burst, new Vector2(4770, 3431));
            TestAssert.True(player.GlobalPosition.X - 4630f >= 64f && 4770f - player.GlobalPosition.X >= 64f,
                "SUPPRESSION_SETUP: current packed player's whole body and both64px exclusions admit the separated supported pair");
            for (int frame = 0; frame < 120 && warningBirths.Select(warning => warning.Frame).Distinct().Count() < 2; frame++) await Step(1);
            ulong[] retryStarts = warningBirths.Select(warning => warning.Frame).Distinct().ToArray();
            TestAssert.True(retryStarts.Length == 2 && group.IsArrivalWarningActive && group.LiveActorCount == 0,
                "RANGED_SUPPRESSION_RECOVERY_WARNING_RED: after real player clearance, low-frequency whole-batch revalidation must start one fresh full warning within120normalframes, not remain permanently suppressed");
            ulong retryStart = retryStarts[1];
            var retryLeaves = warningBirths.Where(warning => warning.Frame == retryStart).ToArray();
            Vector2[] literalSites = { new(4700, 3424), new(4630, 3424), new(4770, 3431), new(4630, 3431) };
            TestAssert.True(retryLeaves.Length == 4 && literalSites.All(site => retryLeaves.Any(warning => warning.Site == site)) &&
                            retryLeaves.All(warning => warning.Player.X >= 4694f && warning.Player.X <= 4706f),
                "fresh retry warning records all four actual literal sites against the current truly legal player pose");
            if (mode == "pause")
            {
                ulong pauseStart = Engine.GetPhysicsFrames();
                Vector2 pausedPose = player.GlobalPosition;
                tree.Paused = true;
                for (int frame = 0; frame < 20; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(group.IsArrivalWarningActive && group.LiveActorCount == 0 && mixedBirths.Count == 0 && player.GlobalPosition == pausedPose,
                    "RANGED_SUPPRESSION_PAUSE_RED: pending retry warning cannot advance/birth or move the normal player while paused");
                pausedPhysics += Engine.GetPhysicsFrames() - pauseStart;
                tree.Paused = false;
            }
            if (mode == "reset")
            {
                group.ResetForCheckpoint(); // Explicit lifecycle cancellation test, never recovery-path activation/reset shortcut.
                for (int frame = 0; frame < 120; frame++)
                {
                    await Step(1);
                    TestAssert.True(!group.IsActive && group.CurrentBatch == -1 && group.LiveActorCount == 0 && !group.IsArrivalWarningActive &&
                                    !group.SpawnSuppressed && mixedBirths.Count == 0 && warningBirths.Select(warning => warning.Frame).Distinct().Count() == 2 &&
                                    !Children(actors).OfType<CanyonSpawnWarningVisual>().Any(warning => !warning.IsQueuedForDeletion()),
                        "RANGED_SUPPRESSION_RESET_CANCEL_RED: reset invalidates suppressed/retry generation with no late birth or warning replay");
                }
                return;
            }
            if (mode == "exit")
            {
                root.QueueFree();
                await Frames(tree, 120);
                TestAssert.True(!GodotObject.IsInstanceValid(root) && mixedBirths.Count == 0 && completions == 0 && defeatedGroups == 0,
                    "RANGED_SUPPRESSION_EXIT_CANCEL_RED: actual tree exit cannot late-spawn the pending retry batch or complete it");
                return;
            }
            for (int frame = 0; frame < 60 && group.LiveActorCount == 0; frame++) await Step(1);
            TestAssert.True(group.LiveActorCount == 2 && mixedBirths.Count == 2 && !group.IsArrivalWarningActive && !group.SpawnSuppressed &&
                            mixedBirths.All(birth => birth.Frame - retryStart - pausedPhysics >= 36UL) &&
                            mixedBirths.Select(birth => birth.Frame).Distinct().Count() == 1 &&
                            mixedBirths.Any(birth => birth.Scene == CanyonEncounterCatalog.RustShellScene && birth.Position == new Vector2(4630, 3424)) &&
                            mixedBirths.Any(birth => birth.Scene == Burst && birth.Position == new Vector2(4770, 3431)),
                "RANGED_SUPPRESSION_RECOVERY_ATOMIC_BIRTH_RED: full fresh .60s warning ends in same-tick actual complete Rust4630/Burst4770 birth, without stale suppression");
            await Step(6);
            foreach (CharacterBody2D actor in Children(actors).OfType<CharacterBody2D>())
            {
                TestAssert.True(actor.IsPhysicsProcessing() && actor.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                    "recovered pair retains actual living normal AI owners");
                AssertFairnessSupport(actor, landing, landingRect);
                AssertFairnessBodyClear(tree, actor);
            }
            AssertFairnessSupport(player, landing, landingRect);
            AssertFairnessBodyClear(tree, player);
            TestAssert.True(warningBirths.Select(warning => warning.Frame).Distinct().Count() == 2 && warningEnds.Count == 8,
                "one suppressed warning and exactly one legitimate fresh warning, all leaves retired after atomic birth");
            GD.Print($"RANGED_SUPPRESSION_RECOVERY_CASE_OK mode={mode} same_player={identity} player={player.GlobalPosition} hp={health.CurrentHealth} energy={energy.EnergyRatio} recovery=0 warning_start={retryStart} native_ready={mixedBirths[0].Frame} unpaused_warning_ticks={mixedBirths[0].Frame - retryStart - pausedPhysics} live={group.LiveActorCount}");
        }
        finally
        {
            tree.Paused = originalPaused;
            try { ReleaseFairnessKeys(); }
            finally
            {
                try { await DisposeFairnessFixture(tree, root, sector, player); }
                finally
                {
                    try { foreach (Resource resource in resources) resource.Dispose(); }
                    finally { tree.Root.CanvasTransform = canvas; }
                }
            }
        }
    }

    private static void AssertSuppressionCandidateWorldBlocked(SceneTree tree, Vector2 site)
    {
        CharacterBody2D probe = GD.Load<PackedScene>(Burst).Instantiate<CharacterBody2D>();
        try
        {
            CollisionShape2D collider = probe.GetNode<CollisionShape2D>("BodyCollider");
            using var query = new PhysicsShapeQueryParameters2D { Shape = collider.Shape,
                Transform = new Transform2D(0f, site + collider.Position + Vector2.Up * .5f), Margin = 0f,
                CollisionMask = CombatCollisionLayers.World, CollideWithBodies = true, CollideWithAreas = false };
            using Godot.Collections.Array hits = (Godot.Collections.Array)tree.Root.World2D.DirectSpaceState.IntersectShape(query, 32);
            TestAssert.True(hits.Count > 0, "static suppression negative has a real native World blocker at literal Burst site=" + site);
        }
        finally { probe.Free(); }
    }

    private static async Task SavedMixedFightKeyboardEscape(SceneTree tree, bool clawRoute)
    {
        Transform2D canvas = tree.Root.CanvasTransform;
        RangedFairnessRecoveryProbe root = null;
        PrologueSector sector = null;
        CharacterBody2D player = null;
        try
        {
            ReleaseFairnessKeys();
            root = new RangedFairnessRecoveryProbe { Name = clawRoute ? "EndpointClawEscape" : "CenterJumpEscape" };
            sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
            root.AddChild(sector);
            // Deliberately NOT PackedPlayer/PreparePlayer: every saved gameplay owner
            // and the camera retain their normal packed processing and resource values.
            player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position = new Vector2(4100, 3500);
            player.AddToGroup("player");
            root.AddChild(player);
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/CounterweightLiftEncounter");
            Node2D actors = sector.GetNode<Node2D>("Gameplay/Actors");
            var progress = new PrologueProgress();
            group.ApplyProgress(progress);
            var f = new Fixture(root, actors, player, group, progress, new(), new());
            actors.ChildEnteredTree += child => child.Ready += () =>
            {
                if (child is Node2D actor) f.ReadyPositions[actor.GetInstanceId()] = actor.GlobalPosition;
            };
            tree.Root.AddChild(root);
            // Isolated saved-sector fixture rebinds only recovery routing, never its
            // volumes/shapes/masks. Any real recovery request is a terminal failure;
            // the observer cannot heal, reposition, or respawn the player.
            foreach (KillZone zone in Descendants(sector).OfType<KillZone>()) zone.RoomControllerPath = zone.GetPathTo(root);
            foreach (CanyonEncounterGroup other in Descendants(sector).OfType<CanyonEncounterGroup>())
                if (other != group) other.SetPhysicsProcess(false);
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
            PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
            PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
            MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            int deaths = 0, heals = 0;
            health.Died += () => deaths++;
            health.Healed += (_, _) => heals++;
            RustShellSquire rust = null;
            RangedWeaponEnemy burst = null;
            int emissions = 0;
            ulong identity = player.GetInstanceId();
            Vector2 last = player.GlobalPosition;
            int pathFrame = 0;
            async Task Step(int count)
            {
                for (int frame = 0; frame < count; frame++)
                {
                    await Frames(tree, 1);
                    pathFrame++;
                    TestAssert.True(GodotObject.IsInstanceValid(player) && player.GetInstanceId() == identity && health.CurrentHealth > 0 &&
                                    deaths == 0 && heals == 0 && root.RecoveryRequests == 0,
                        $"RANGED_FAIRNESS_SURVIVAL_RED: same normal player survives without recovery/heal frame={pathFrame} hp={health.CurrentHealth} requests={root.RecoveryRequests}");
                    TestAssert.True(float.IsFinite(player.GlobalPosition.X) && float.IsFinite(player.GlobalPosition.Y) &&
                                    player.GlobalPosition.DistanceTo(last) < 32f,
                        $"RANGED_FAIRNESS_NO_TELEPORT_RED: ordinary bounded native movement frame={pathFrame} before={last} after={player.GlobalPosition}");
                    TestAssert.True(motor.IsPhysicsProcessing() && input.IsPhysicsProcessing() && energy.IsPhysicsProcessing() &&
                                    claw.IsPhysicsProcessing() && player.GetNode<PlayerActionModeController>("ActionModes").IsPhysicsProcessing() &&
                                    health.IsPhysicsProcessing() && player.GetNode<Camera2D>("Camera2D").Enabled &&
                                    player.GetNode<Camera2D>("Camera2D").IsCurrent() &&
                                    player.CollisionLayer == CombatCollisionLayers.Player && player.CollisionMask == CombatCollisionLayers.World &&
                                    !player.GetNode<CollisionShape2D>("BodyCollider").Disabled &&
                                    !player.GetNode<CollisionShape2D>("Hurtbox/HurtboxCollider").Disabled && health.MaxHealth == 5 &&
                                    player.GetNode<Hurtbox>("Hurtbox").Monitorable &&
                                    player.GetNode<Hurtbox>("Hurtbox").CollisionLayer == CombatCollisionLayers.Player &&
                                    player.GetNode<Hurtbox>("Hurtbox").ResolveDamageReceiver() == player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage") &&
                                    MathF.Abs(health.InvulnerabilitySeconds - .35f) < .0001f && float.IsFinite(energy.EnergyRatio) &&
                                    energy.EnergyRatio >= 0f && energy.EnergyRatio <= 1f,
                        "fairness retains packed input/motor/claw/action/energy/full-body/Hurtbox and normal5HP/.35s damage grace");
                    last = player.GlobalPosition;
                    if (pathFrame % 10 == 0)
                        GD.Print($"RANGED_FAIRNESS_PATH route={(clawRoute ? "claw" : "jump")} frame={pathFrame} player={player.GlobalPosition} velocity={player.Velocity} hp={health.CurrentHealth} energy={energy.EnergyRatio} floor={player.IsOnFloor()} claw={claw.CurrentFrame.State} rust={(GodotObject.IsInstanceValid(rust) ? rust.GlobalPosition.ToString() : "pending")} burst={(GodotObject.IsInstanceValid(burst) ? burst.GlobalPosition.ToString() : "pending")} emissions={emissions} recovery={root.RecoveryRequests}");
                }
            }
            await Step(4);
            TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1 &&
                            actors.GetChild(0).SceneFilePath == CanyonEncounterCatalog.MothScene,
                "FAIRNESS_SETUP: genuine saved old Moth begins the actual Counterweight group");
            // A starting-pose arrangement BEFORE the warning/escape, not an escape
            // teleport. From here every displacement is ordinary native gameplay.
            player.GlobalPosition = new Vector2(clawRoute ? 4770 : 4700, 3424);
            last = player.GlobalPosition;
            await Step(6);
            AssertFairnessSupport(player, sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding"), new Rect2(4600, 3460, 200, 40));
            TestAssert.True(health.CurrentHealth == 5 && energy.EnergyRatio > .99f,
                "FAIRNESS_SETUP: actual default player begins with unmodified normal health/energy");
            Kill(actors.GetChild(0)); // Genuine Health death, never group.Complete().
            await Step(2);
            TestAssert.True(group.CurrentBatch == 1 && group.IsArrivalWarningActive && group.LiveActorCount == 0,
                "FAIRNESS_SETUP: old Moth defeat starts actual .60s whole-pair warning");
            GD.Print($"RANGED_FAIRNESS_ARRIVAL_WARNING route={(clawRoute ? "claw" : "jump")} physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} live={group.LiveActorCount}");
            await Step(24);
            TestAssert.True(group.IsArrivalWarningActive && group.LiveActorCount == 0 && player.IsOnFloor(),
                "FAIRNESS_SETUP: real supported player observes pending .60s warning without premature birth");
            await Step(16);
            TestAssert.Equal(2, group.LiveActorCount, "FAIRNESS_SETUP: actual mixed pair must arrive before escape");
            rust = Children(actors).OfType<RustShellSquire>().Single();
            burst = Children(actors).OfType<RangedWeaponEnemy>().Single();
            burst.ShotEmitted += (_, _) => emissions++;
            Vector2 expectedRust = new(clawRoute ? 4700 : 4630, 3424);
            Vector2 expectedBurst = new(clawRoute ? 4630 : 4770, 3431);
            TestAssert.True(f.ReadyPositions[rust.GetInstanceId()] == expectedRust && f.ReadyPositions[burst.GetInstanceId()] == expectedBurst &&
                            burst.SceneFilePath == Burst && rust.SceneFilePath == CanyonEncounterCatalog.RustShellScene,
                "FAIRNESS_SETUP: actual center/endpoint live pair has the independently declared literal birth selection");
            foreach (CharacterBody2D actor in new CharacterBody2D[] { rust, burst })
            {
                TestAssert.True(actor.IsPhysicsProcessing() && actor.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                    "FAIRNESS_SETUP: actual mixed AI is processing and living");
                AssertFairnessSupport(actor, sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding"), new Rect2(4600, 3460, 200, 40));
                AssertFairnessBodyClear(tree, actor);
            }
            TestAssert.True(!BodyBounds(rust, rust.GlobalPosition).Intersects(BodyBounds(burst, burst.GlobalPosition)) &&
                            !BodyBounds(player, player.GlobalPosition).Intersects(BodyBounds(rust, rust.GlobalPosition)) &&
                            !BodyBounds(player, player.GlobalPosition).Intersects(BodyBounds(burst, burst.GlobalPosition)),
                "FAIRNESS_SETUP: actual living supported bodies are separated, not just reserved boxes");
            AssertAttackPrerequisites(tree, f, burst);
            TestAssert.True(rust.HasLineOfSightTo(player) && rust.GlobalPosition.DistanceTo(player.GlobalPosition) < 360f &&
                            tree.Root.GetVisibleRect().HasPoint(rust.GetGlobalTransformWithCanvas().Origin),
                "FAIRNESS_SETUP: real Rust has native LOS/range and real camera viewport admission");
            AttackTelegraphVisual rustWarning = rust.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            AttackTelegraphVisual burstWarning = burst.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            for (int frame = 0; frame < 12 && (rustWarning.CurrentFrame.Timeline.Phase != AttackPhase.Telegraph ||
                    burstWarning.CurrentFrame.Timeline.Phase != AttackPhase.Telegraph); frame++) await Step(1);
            TestAssert.True(rustWarning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
                            burstWarning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph,
                "FAIRNESS_SETUP: both actual living sources naturally warn before keyboard escape");
            Rect2[] originalCorridors = { FairnessAttackBounds(rustWarning.CurrentFrame.Shape), FairnessAttackBounds(burstWarning.CurrentFrame.Shape) };
            int initialJumps = motor.AcceptedJumpCount;
            ulong inputSequence = input.CurrentFrameSequence;
            GD.Print($"RANGED_FAIRNESS_BEFORE route={(clawRoute ? "claw" : "jump")} physics={Engine.GetPhysicsFrames()} player_id={identity} player={player.GlobalPosition} hp={health.CurrentHealth}/{health.MaxHealth} grace={health.InvulnerabilitySeconds} energy={energy.EnergyRatio} recovery={root.RecoveryRequests} rust={rust.GlobalPosition} rust_warning={rustWarning.CurrentFrame} burst={burst.GlobalPosition} burst_warning={burstWarning.CurrentFrame} budgets=jump110_claw100_walk110_settle75_hold30");
            // Source-only proposal: one Space+A ordinary jump, then travel left
            // down the two unchanged saved access steps to StepOne at x4420.
            FairnessKey(Key.A, true);
            if (clawRoute) FairnessKey(Key.W, true);
            FairnessKey(Key.Space, true);
            await Step(clawRoute ? 3 : 20);
            FairnessKey(Key.Space, false);
            if (clawRoute)
            {
                GrappleSurface2D expectedSurface = sector.GetNode<GrappleSurface2D>("Gameplay/Collision/LiftBackLandingGrip/GrappleSurfaceProfile");
                TestAssert.True(expectedSurface.IsAvailable && expectedSurface.UseCollisionHitPoint &&
                                expectedSurface.Material == GrappleSurfaceMaterial.TempleMetal,
                    "FAIRNESS_SETUP: claw proposal uses unchanged real rough metal collision profile, not an authored label/target double");
                FairnessKey(Key.K, true);
                await Step(2);
                FairnessKey(Key.K, false);
                FairnessKey(Key.A, false);
                FairnessKey(Key.W, false);
                bool attached = false;
                for (int frame = 0; frame < 100; frame++)
                {
                    await Step(1);
                    if (claw.CurrentTarget == expectedSurface && claw.CurrentFrame.State == MechanicalClawState.Swing && claw.CurrentFrame.Arrived)
                    { attached = true; break; }
                }
                GD.Print($"RANGED_FAIRNESS_ATTACHMENT player={player.GlobalPosition} target={claw.CurrentTarget?.GetPath()} frame={claw.CurrentFrame}");
                TestAssert.True(attached && claw.CurrentFrame.LockedTargetRuntimeId == expectedSurface.GetInstanceId(),
                    "RANGED_FAIRNESS_REAL_ROUGH_ATTACHMENT_RED: actual W+A/Space/K must reach real saved metal contact through full native body collision within100frames");
                // Do not declare attachment itself safe: release through a real
                // fresh Space edge, then prove the subsequent supported landing.
                ulong attachedCast = claw.CurrentFrame.CastId;
                ulong attachedTarget = expectedSurface.GetInstanceId();
                int jumpsBeforeRelease = motor.AcceptedJumpCount, dashesBeforeRelease = motor.AcceptedDashCount;
                Vector2 releaseStart = player.GlobalPosition;
                var releasePublications = new List<(MechanicalClawFrame Frame, Vector2 Velocity, ulong Physics, ulong Target)>();
                void ObserveRelease(MechanicalClawFrame frame, GrappleSurface2D target)
                    => releasePublications.Add((frame, player.Velocity, Engine.GetPhysicsFrames(),
                        GodotObject.IsInstanceValid(target) ? target.GetInstanceId() : 0UL));
                claw.ClawFrameChanged += ObserveRelease;
                try
                {
                    FairnessKey(Key.A, true);
                    FairnessKey(Key.Space, true);
                    await Step(2); // Unchanged actual route budget, never direct motor/model stepping.
                    FairnessKey(Key.Space, false);
                    var terminal = releasePublications.Where(sample => sample.Frame.State is
                        MechanicalClawState.Boost or MechanicalClawState.Released).ToArray();
                    int boostIndex = releasePublications.FindIndex(sample => sample.Frame.State == MechanicalClawState.Boost);
                    TestAssert.True(attachedCast != 0 && terminal.Length == 2 && boostIndex >= 0 &&
                                    boostIndex + 1 < releasePublications.Count &&
                                    releasePublications[boostIndex + 1].Frame.State == MechanicalClawState.Released &&
                                    terminal[0].Frame.State == MechanicalClawState.Boost &&
                                    terminal[1].Frame.State == MechanicalClawState.Released &&
                                    terminal.All(sample => sample.Frame.CastId == attachedCast &&
                                        sample.Frame.LockedTargetRuntimeId == attachedTarget &&
                                        sample.Frame.ReleaseKind == MechanicalClawReleaseKind.Boost &&
                                        sample.Frame.BoostConsumed && sample.Frame.Failure == MechanicalClawFailure.None) &&
                                    terminal[0].Physics == terminal[1].Physics && terminal[0].Target == attachedTarget &&
                                    terminal[1].Target == 0UL && terminal[0].Velocity.DistanceTo(terminal[1].Velocity) < .001f &&
                                    claw.CurrentTarget == null,
                        "RANGED_FAIRNESS_REAL_RELEASE_RED: fresh Space publishes exactly one adjacent same-cast Boost→Released transaction with genuine BoostConsumed, not cancel/obstruction/damage/target loss");
                    // Read-only motor receipt AFTER ordinary native owners ran:
                    // preservation must be armed and the real release must supply
                    // upward motion beyond the pre-ApplyMotor publication velocity.
                    // A null target/publication alone cannot satisfy this effect.
                    TestAssert.True(motor.HasGrappleReleaseMomentum && motor.GrappleMovementScale == 1f &&
                                    motor.AcceptedJumpCount == jumpsBeforeRelease && motor.AcceptedDashCount == dashesBeforeRelease &&
                                    !motor.IsDashing && player.Velocity.Length() <= 900.01f &&
                                    player.Velocity.Y < terminal[0].Velocity.Y - 20f && player.GlobalPosition.Y < releaseStart.Y - .25f,
                        $"RANGED_FAIRNESS_BOOST_MOTOR_RECEIPT_RED: same native motor receives real composed boost without a second jump/dash; before={terminal[0].Velocity} after={player.Velocity} position_before={releaseStart} after={player.GlobalPosition} momentum={motor.HasGrappleReleaseMomentum}");
                    GD.Print($"RANGED_FAIRNESS_BOOST_RECEIPT cast={attachedCast} target={attachedTarget} sequence=Boost,Released physics={terminal[0].Physics} publication_velocity={terminal[0].Velocity} motor_velocity={player.Velocity} momentum={motor.HasGrappleReleaseMomentum} jumps_delta={motor.AcceptedJumpCount - jumpsBeforeRelease} dashes_delta={motor.AcceptedDashCount - dashesBeforeRelease}");
                }
                finally { claw.ClawFrameChanged -= ObserveRelease; }
            }
            for (int frame = 0; frame < 110 && player.GlobalPosition.X > 4420f; frame++) await Step(1);
            FairnessKey(Key.A, false);
            await Step(75);
            TestAssert.True(motor.AcceptedJumpCount == initialJumps + 1 && input.CurrentFrameSequence > inputSequence &&
                            claw.CurrentTarget == null && !motor.IsDashing,
                "RANGED_FAIRNESS_KEYBOARD_ROUTE_RED: exactly one ordinary accepted jump and sampled keyboard route, no dash or lingering attachment");
            StaticBody2D destination = sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftAccessStepOne");
            GD.Print($"RANGED_FAIRNESS_LANDING route={(clawRoute ? "claw" : "jump")} player={player.GlobalPosition} velocity={player.Velocity} hp={health.CurrentHealth} energy={energy.EnergyRatio} floor={player.IsOnFloor()} landing={destination.GlobalPosition} emissions={emissions} rust={rust.GlobalPosition} burst={burst.GlobalPosition}");
            AssertFairnessSupport(player, destination, new Rect2(4320, 3556, 160, 40));
            AssertFairnessBodyClear(tree, player);
            Rect2 finalBody = BodyBounds(player, player.GlobalPosition);
            TestAssert.True(originalCorridors.All(corridor => !corridor.Intersects(finalBody)) &&
                            finalBody.Position.Y > 3470f && finalBody.End.X < 4480f && emissions > 0,
                "RANGED_FAIRNESS_SUPPORTED_ESCAPE_RED: real supported lower foothold clears both original opposed corridors after real hostile emission, never just in-air y decrease");
            for (int frame = 0; frame < 30; frame++)
            {
                await Step(1);
                AssertFairnessSupport(player, destination, new Rect2(4320, 3556, 160, 40));
                AssertFairnessBodyClear(tree, player);
                TestAssert.True(group.LiveActorCount == 2 && !group.IsComplete && rust.IsPhysicsProcessing() && burst.IsPhysicsProcessing(),
                    "safe landing does not delete, disable or defeat either actual threat");
                foreach (AttackTelegraphVisual leaf in Descendants(actors).OfType<AttackTelegraphVisual>())
                    if (leaf.CurrentFrame.Timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active)
                        TestAssert.True(!FairnessAttackBounds(leaf.CurrentFrame.Shape).Intersects(BodyBounds(player, player.GlobalPosition)),
                            "RANGED_FAIRNESS_SAFE_HOLD_RED: final supported player stays clear of current real warning/active attack geometry for30normalframes");
            }
            GD.Print($"RANGED_FAIRNESS_CASE_OK route={(clawRoute ? "claw" : "jump")} same_player={identity} start_hp=5 end_hp={health.CurrentHealth} energy={energy.EnergyRatio} recovery=0 endpoint={player.GlobalPosition} emissions={emissions} normal_physics_frames={pathFrame}");
        }
        finally
        {
            try { ReleaseFairnessKeys(); }
            finally
            {
                try { await DisposeFairnessFixture(tree, root, sector, player); }
                finally { tree.Root.CanvasTransform = canvas; }
            }
        }
    }

    private static async Task DisposeFairnessFixture(SceneTree tree, Node2D root, PrologueSector sector,
        CharacterBody2D player)
    {
        // Handles failures anywhere in construction, including a detached packed
        // instance not yet parented to the fixture. Each later owner is protected
        // from an earlier cleanup exception; borrowed packed resources stay borrowed.
        try
        {
            if (GodotObject.IsInstanceValid(player)) player.RemoveFromGroup("player");
        }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(root))
                {
                    if (root.IsInsideTree()) { root.QueueFree(); await Frames(tree, 3); }
                    else root.Free();
                }
            }
            finally
            {
                try
                {
                    if (GodotObject.IsInstanceValid(sector) && sector.GetParent() == null) sector.Free();
                }
                finally
                {
                    if (GodotObject.IsInstanceValid(player) && player.GetParent() == null) player.Free();
                }
            }
        }
    }

    private static void FairnessKey(Key key, bool pressed)
    {
        using var inputEvent = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed, Echo = false };
        Input.ParseInputEvent(inputEvent);
        Input.FlushBufferedEvents();
        TestAssert.True(Input.IsPhysicalKeyPressed(key) == pressed, $"native engine keyboard receipt {key}={pressed}; not OS injection");
    }

    private static void ReleaseFairnessKeys()
    {
        Exception firstFailure = null;
        foreach (Key key in new[] { Key.A, Key.D, Key.W, Key.S, Key.K, Key.Space, Key.Shift, Key.J, Key.Q })
        {
            try { FairnessKey(key, false); }
            catch (Exception failure) { firstFailure ??= failure; }
        }
        if (firstFailure != null) throw firstFailure;
    }

    private static void AssertFairnessSupport(CharacterBody2D actor, StaticBody2D floor, Rect2 expectedFloor)
    {
        CollisionShape2D shape = Children(floor).OfType<CollisionShape2D>().Single();
        Vector2 half = ((RectangleShape2D)shape.Shape).Size * .5f;
        Rect2 actualFloor = new(shape.ToGlobal(-half), half * 2f);
        Rect2 body = BodyBounds(actor, actor.GlobalPosition);
        bool actualContact = false;
        for (int index = 0; index < actor.GetSlideCollisionCount(); index++)
        {
            using KinematicCollision2D contact = actor.GetSlideCollision(index);
            actualContact |= contact.GetCollider() == floor && contact.GetNormal().Dot(Vector2.Up) > .9f;
        }
        TestAssert.True(actor.IsOnFloor() && actualContact && !shape.Disabled &&
                        (floor.CollisionLayer & CombatCollisionLayers.World) != 0 && actualFloor == expectedFloor &&
                        body.Position.X >= expectedFloor.Position.X && body.End.X <= expectedFloor.End.X &&
                        MathF.Abs(body.End.Y - expectedFloor.Position.Y) < 1f,
            $"RANGED_FAIRNESS_NATIVE_SUPPORT_RED: genuine full-foot native floor contact actor={actor.Name} pos={actor.GlobalPosition} body={body} expected={expectedFloor} actual={actualFloor} floor={actor.IsOnFloor()} contact={actualContact}");
    }

    private static void AssertFairnessBodyClear(SceneTree tree, CharacterBody2D actor)
    {
        var excluded = new Godot.Collections.Array<Rid> { actor.GetRid() };
        using Godot.Collections.Array excludedOwner = (Godot.Collections.Array)excluded;
        using var query = new PhysicsShapeQueryParameters2D { Shape = actor.GetNode<CollisionShape2D>("BodyCollider").Shape,
            Transform = new Transform2D(0f, BodyBounds(actor, actor.GlobalPosition).GetCenter() + Vector2.Up * .5f), Margin = 0f,
            CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy, CollideWithBodies = true,
            CollideWithAreas = false, Exclude = excluded };
        using Godot.Collections.Array hits = (Godot.Collections.Array)tree.Root.World2D.DirectSpaceState.IntersectShape(query, 32);
        TestAssert.Equal(0, hits.Count, "fairness complete native capsule is clear of world/enemies with only self excluded");
    }

    // Conservative world AABB of the real published attack shape. This is a
    // safe-region exclusion oracle, not a replacement for native damage/collision.
    private static Rect2 FairnessAttackBounds(AttackShapeFrame shape)
    {
        if (shape.Kind == AttackShapeKind.ConvexPolygon)
        {
            Vector2 min = shape.Points[0], max = min;
            foreach (Vector2 point in shape.Points)
            {
                min = new Vector2(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y));
                max = new Vector2(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y));
            }
            return new Rect2(min, max - min);
        }
        Vector2 half = shape.Extent;
        if (shape.Kind == AttackShapeKind.Capsule)
        {
            Vector2 axis = Vector2.Down.Rotated(shape.RotationRadians) * MathF.Max(0f, half.Y - half.X);
            half = axis.Abs() + Vector2.One * half.X;
        }
        else if (shape.Kind is AttackShapeKind.Rectangle or AttackShapeKind.GroundBand)
        {
            Vector2 x = Vector2.Right.Rotated(shape.RotationRadians) * half.X;
            Vector2 y = Vector2.Down.Rotated(shape.RotationRadians) * half.Y;
            half = x.Abs() + y.Abs();
        }
        return new Rect2(shape.Origin - half, half * 2f);
    }

    private static readonly MapCase[] Cases =
    {
        new(PrologueSectorId.CanyonDescent, "CanyonDescent", "ControlledAbyssEncounter", Scatter,
            RangedWeaponRole.Scatter, new Vector2(2200, 3524), new Vector2(2450, 3531), new Vector2(2070, 3531), 2),
        new(PrologueSectorId.CanyonAscent, "CanyonAscent", "PendulumGalleryEncounter", Laser,
            RangedWeaponRole.Laser, new Vector2(4500, 2660), new Vector2(4300, 2331), new Vector2(4460, 2331), 2),
        new(PrologueSectorId.CanyonBasin, "CanyonBasin", "SteamValvesEncounter", Missile,
            RangedWeaponRole.Missile, new Vector2(3300, 4190), new Vector2(3750, 4207), new Vector2(3250, 4207), 2),
        new(PrologueSectorId.CanyonAscent, "CanyonAscent", "CounterweightLiftEncounter", Burst,
            RangedWeaponRole.Burst, new Vector2(4100, 3500), new Vector2(4770, 3431), new Vector2(4630, 3431), 1)
    };

    private static async Task CounterweightRustSettlementRejectsInvalidGeometry(SceneTree tree)
    {
        foreach (string invalid in new[] { "drop-over-four", "floor-gap", "blocked-drop" })
        {
            Fixture f = CreateSavedCounterweightFixture(tree, "InvalidRustSettlement_" + invalid);
            try
            {
                await Frames(tree, 4);
                TestAssert.True(f.Group.LiveActorCount == 1 && f.Group.CurrentBatch == 0,
                    "negative settlement case begins with actual saved Moth");
                PrologueSector sector = (PrologueSector)f.Root.GetChild(0);
                StaticBody2D landing = sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding");
                CollisionShape2D floor = Children(landing).OfType<CollisionShape2D>().Single();
                if (invalid == "drop-over-four")
                {
                    landing.Position += Vector2.Down * 2f; // Rust foot gap is now6, not4.
                    AddOwnedFloor(f, "BurstSupport", new Vector2(4630, 3480), new Vector2(60, 40));
                    AddOwnedFloor(f, "PlayerSupport", new Vector2(4770, 3480), new Vector2(60, 40));
                }
                else if (invalid == "floor-gap")
                {
                    floor.Disabled = true;
                    AddOwnedFloor(f, "LeftSplitSupport", new Vector2(4647.5f, 3480), new Vector2(95, 40));
                    AddOwnedFloor(f, "RightSplitSupport", new Vector2(4752.5f, 3480), new Vector2(95, 40));
                }
                else AddOwnedFloor(f, "RustDropBlocker", new Vector2(4700, 3459), new Vector2(40, 1));
                f.Player.GlobalPosition = new Vector2(4770, 3424);
                await Frames(tree, 2);
                f.Player.Velocity = Vector2.Down * 20f;
                f.Player.MoveAndSlide();
                TestAssert.True(f.Player.IsOnFloor(), "negative geometry keeps the real endpoint player natively supported");
                ValidateCandidate(tree, f, Burst, new Vector2(4630, 3431));
                Kill(f.Actors.GetChild(0));
                await Frames(tree, 2);
                TestAssert.True(f.Group.IsArrivalWarningActive && f.Group.LiveActorCount == 0,
                    "invalid settlement still displays the actual whole-batch warning");
                await Frames(tree, 40);
                TestAssert.True(f.Group.SpawnSuppressed && f.Group.LiveActorCount == 0 &&
                                f.Actors.GetChildCount() == 0 && !f.Group.IsComplete,
                    "Counterweight Rust settlement fails closed for " + invalid + " despite a legal Burst/player site");
            }
            finally { await Dispose(tree, f); }
        }
    }

    private static async Task OtherEncounterCannotBorrowRustSettlementException(SceneTree tree)
    {
        Fixture f = CreateContractFixture(tree, mixed: true, unsupportedRust: true);
        try
        {
            await BeginMixedWarning(tree, f);
            await Frames(tree, 40);
            TestAssert.True(f.Group.SpawnSuppressed && f.Group.LiveActorCount == 0 && !f.Group.IsComplete,
                "4px-above-floor Rust in a different encounter cannot borrow the Counterweight-only settlement exception");
        }
        finally { await Dispose(tree, f); }
    }

    private static Fixture CreateSavedCounterweightFixture(SceneTree tree, string name)
    {
        var root = new Node2D { Name = name };
        PrologueSector sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn")
            .Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
        root.AddChild(sector);
        CharacterBody2D player = PackedPlayer(new Vector2(4100, 3500));
        root.AddChild(player);
        CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/CounterweightLiftEncounter");
        Node2D actors = sector.GetNode<Node2D>("Gameplay/Actors");
        var progress = new PrologueProgress();
        group.ApplyProgress(progress);
        var f = new Fixture(root, actors, player, group, progress, new(), new());
        actors.ChildEnteredTree += child => child.Ready += () =>
        {
            if (child is Node2D actor) f.ReadyPositions[actor.GetInstanceId()] = actor.GlobalPosition;
        };
        tree.Root.AddChild(root);
        foreach (CanyonEncounterGroup other in Descendants(sector).OfType<CanyonEncounterGroup>())
            if (other != group) other.SetPhysicsProcess(false);
        PreparePlayer(player);
        return f;
    }

    // Break caught: ordinary supported center landing blocks the old fixed Rust
    // and silently erases the entire promised local mixed fight.
    private static async Task SavedCounterweightLandingStillAdmitsMixedFight(SceneTree tree, bool center)
    {
        Fixture f = CreateSavedCounterweightFixture(tree, center ? "SavedCounterweightCenterLanding" : "SavedCounterweightRustGrounding");
        PrologueSector sector = (PrologueSector)f.Root.GetChild(0);
        CharacterBody2D player = f.Player;
        CanyonEncounterGroup group = f.Group;
        Node2D actors = f.Actors;
        try
        {
            await Frames(tree, 4);
            TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1,
                "center-landing witness starts with the unchanged actual saved Moth batch");
            float playerX = center ? 4700f : 4770f;
            player.GlobalPosition = new Vector2(playerX, 3424);
            await Frames(tree, 1);
            player.Velocity = Vector2.Down * 20f;
            player.MoveAndSlide();
            TestAssert.True(player.IsOnFloor() && MathF.Abs(player.GlobalPosition.X - playerX) < .01f &&
                            MathF.Abs(player.GlobalPosition.Y - 3424f) < 1f,
                "actual packed player is natively grounded on the real saved landing at x=" + playerX);
            if (!center)
            {
                CanyonEnemyPlacement rust = group.Definition.Enemies.Single(enemy =>
                    enemy.ScenePath == CanyonEncounterCatalog.RustShellScene);
                TestAssert.True(rust.WorldPosition == new Vector2(4700, 3424) && rust.ActivationBatch == 1 &&
                                rust.AlternateWorldPosition == new Vector2(4630, 3424),
                    "grounding witness preserves old actual Rust primary/batch and the sole approved alternate4630");
                CharacterBody2D probe = GD.Load<PackedScene>(rust.ScenePath).Instantiate<CharacterBody2D>();
                try
                {
                    Rect2 body = BodyBounds(probe, rust.WorldPosition);
                    StaticBody2D landing = sector.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding");
                    CollisionShape2D floorShape = Children(landing).OfType<CollisionShape2D>().Single();
                    Vector2 half = ((RectangleShape2D)floorShape.Shape).Size * .5f;
                    Vector2 topLeft = floorShape.ToGlobal(-half);
                    Vector2 topRight = floorShape.ToGlobal(new Vector2(half.X, -half.Y));
                    TestAssert.True(topLeft == new Vector2(4600, 3460) && topRight == new Vector2(4800, 3460) &&
                                    MathF.Abs(body.End.Y - 3456f) < .01f &&
                                    MathF.Abs(topLeft.Y - body.End.Y - 4f) < .01f &&
                                    body.Position.X >= topLeft.X && body.End.X <= topRight.X,
                        "real saved Rust capsule foot3456 is exactly4px above the full-width native landing top3460");
                    TestAssert.True(!body.Intersects(BodyBounds(player, player.GlobalPosition)) &&
                                    MathF.Abs(player.GlobalPosition.X - rust.WorldPosition.X) >= 64f,
                        "endpoint packed player does not block old Rust capsule or its64px arrival exclusion");
                    CollisionShape2D collider = probe.GetNode<CollisionShape2D>("BodyCollider");
                    using var query = new PhysicsShapeQueryParameters2D { Shape = collider.Shape,
                        Transform = new Transform2D(0f, rust.WorldPosition + collider.Position + Vector2.Up * .5f),
                        CollisionMask = CombatCollisionLayers.World, CollideWithBodies = true, CollideWithAreas = false };
                    using Godot.Collections.Array hits = (Godot.Collections.Array)tree.Root.World2D.DirectSpaceState.IntersectShape(query, 32);
                    TestAssert.Equal(0, hits.Count, "native full Rust capsule is clear at its unchanged authored root before4px settlement");
                    ValidateCandidate(tree, f, Burst, new Vector2(4630, 3431));
                    TestAssert.True(MathF.Abs(player.GlobalPosition.X - 4630f) >= 64f,
                        "opposite Burst candidate is supported, native-clear and not player-blocked");
                }
                finally { probe.Free(); }
            }
            Kill(actors.GetChild(0));
            await Frames(tree, 2);
            TestAssert.True(group.CurrentBatch == 1 && group.IsArrivalWarningActive && group.LiveActorCount == 0,
                "center-landing actual Moth death opens the full mixed arrival warning");
            await Frames(tree, 24);
            TestAssert.True(group.IsArrivalWarningActive && group.LiveActorCount == 0 && player.IsOnFloor(),
                "supported center player remains during the real .60s pending window with no premature actors");
            await Frames(tree, 16);
            TestAssert.Equal(2, group.LiveActorCount, center
                ? "RANGED_PLACEMENT_CENTER_LANDING_RED: ordinary grounded center4700 must admit two legal mixed actors after the warning"
                : "RANGED_PLACEMENT_RUST_GROUNDING_RED: clear endpoint player and fully supported4px Rust settlement must admit the actual two-actor mixed batch");
            TestAssert.True(!group.SpawnSuppressed && Children(actors).OfType<RangedWeaponEnemy>().Count() == 1 &&
                            !group.IsComplete,
                "center landing preserves a genuine live ranged/melee pair, not completion or serial fallback");
            CharacterBody2D melee = Children(actors).OfType<CharacterBody2D>().Single(node => node is not RangedWeaponEnemy);
            TestAssert.True(melee.IsOnFloor() && MathF.Abs(melee.GlobalPosition.Y - 3428f) < 1f,
                "unchanged authored Rust3424 naturally falls4px and settles through native physics, without spawn teleport");
            RangedWeaponEnemy ranged = Children(actors).OfType<RangedWeaponEnemy>().Single();
            Vector2 expectedRust = center ? new Vector2(4630, 3424) : new Vector2(4700, 3424);
            Vector2 expectedBurst = center ? new Vector2(4770, 3431) : new Vector2(4630, 3431);
            TestAssert.True(f.ReadyPositions.TryGetValue(melee.GetInstanceId(), out Vector2 bornRust) && bornRust == expectedRust &&
                            f.ReadyPositions.TryGetValue(ranged.GetInstanceId(), out Vector2 bornBurst) && bornBurst == expectedBurst,
                "actual saved pair atomically chooses literal Rust/Burst sites before Ready: " + (center ? "center4630/4770" : "endpoint4700/4630"));
            Rect2 rustReservation = BodyBounds(melee, expectedRust);
            rustReservation.Size += Vector2.Down * 4f;
            Rect2 burstReservation = BodyBounds(ranged, expectedBurst);
            TestAssert.True(!rustReservation.Intersects(burstReservation) &&
                            !rustReservation.Intersects(BodyBounds(player, player.GlobalPosition)) &&
                            !burstReservation.Intersects(BodyBounds(player, player.GlobalPosition)) &&
                            MathF.Abs(expectedRust.X - player.GlobalPosition.X) >= 64f &&
                            MathF.Abs(expectedBurst.X - player.GlobalPosition.X) >= 64f &&
                            MathF.Abs(expectedRust.X - expectedBurst.X) >= rustReservation.Size.X * .5f + burstReservation.Size.X * .5f,
                "complete Rust4px-swept/Burst reservations and actual player body are disjoint with the authored separation");
            foreach (CharacterBody2D actual in new CharacterBody2D[] { melee, ranged })
            {
                Rect2 actualBody = BodyBounds(actual, actual.GlobalPosition);
                TestAssert.True(group.Definition.ArenaBounds.Encloses(actualBody) &&
                                !CanyonEncounterCatalog.ArrivalBodyOverlapsRest(actualBody),
                    "both complete actual native bodies remain in the authored non-rest arena");
                var excluded = new Godot.Collections.Array<Rid> { actual.GetRid() };
                using Godot.Collections.Array excludedOwner = (Godot.Collections.Array)excluded;
                using var bodyQuery = new PhysicsShapeQueryParameters2D { Shape = actual.GetNode<CollisionShape2D>("BodyCollider").Shape,
                    Transform = new Transform2D(0f, actualBody.GetCenter() + Vector2.Up * .5f), Margin = 0f,
                    CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy,
                    CollideWithBodies = true, CollideWithAreas = false, Exclude = excluded };
                using Godot.Collections.Array bodyHits = (Godot.Collections.Array)tree.Root.World2D.DirectSpaceState.IntersectShape(bodyQuery, 32);
                TestAssert.Equal(0, bodyHits.Count, "full actual capsule is native-clear of world and its living sibling after settlement");
            }
            TestAssert.True(!BodyBounds(melee, melee.GlobalPosition).Intersects(BodyBounds(ranged, ranged.GlobalPosition)),
                "both actual living mixed bodies stay disjoint, not only their pre-spawn reservation boxes");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: a saved group keeps stale placements, skips the real .60s
    // warning, chooses the player-blocked candidate, spawns in world geometry,
    // or completes a local mixed fight after only one genuine defeat.
    private static async Task SavedSectorUsesCurrentCatalogAndFires(SceneTree tree, MapCase item, bool blockPrimary)
    {
        Transform2D canvas = tree.Root.CanvasTransform;
        var root = new Node2D { Name = "SavedWeaponPlacement_" + item.Role };
        PrologueSector sector = GD.Load<PackedScene>($"res://scenes/prologue/sectors/{item.Scene}.tscn")
            .Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[item.Sector].Position;
        root.AddChild(sector);
        CharacterBody2D player = PackedPlayer(item.Activation);
        root.AddChild(player);
        CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/" + item.Group);
        Node2D actors = sector.GetNode<Node2D>("Gameplay/Actors");
        var progress = new PrologueProgress();
        group.ApplyProgress(progress);
        var f = new Fixture(root, actors, player, group, progress, new(), new());
        tree.Root.AddChild(root);
        foreach (CanyonEncounterGroup other in Descendants(sector).OfType<CanyonEncounterGroup>())
            if (other != group) other.SetPhysicsProcess(false);
        PreparePlayer(player);
        int completions = 0;
        group.GroupDefeated += _ => completions++;
        try
        {
            TestAssert.True(group.Definition != null && group.Definition.StableId == group.EncounterId.ToString(),
                "saved sector resolves the current C# catalog by its unchanged encounter ID");
            CanyonEnemyPlacement slot = group.Definition.Enemies.Single(enemy => enemy.ScenePath == item.Path);
            TestAssert.True(slot.WorldPosition == item.Primary && slot.AlternateWorldPosition == item.Alternate &&
                            slot.ActivationBatch == item.Batch,
                "actual saved-sector group contains the requested weapon slot from current catalog: " + item.Role);
            await Frames(tree, 1);
            if (item.Role == RangedWeaponRole.Scatter)
            {
                player.Velocity = Vector2.Down * 20f;
                player.MoveAndSlide();
                TestAssert.True(player.IsOnFloor(), "first Moth lesson still requires actual safe-bottom contact");
            }
            await Frames(tree, 4);
            TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1,
                "saved group retains its original single first actor: " + item.Role);
            ValidateCandidate(tree, f, item.Path, item.Primary);
            ValidateCandidate(tree, f, item.Path, item.Alternate);
            Vector2 blocked = blockPrimary ? item.Primary : item.Alternate;
            Vector2 expected = blockPrimary ? item.Alternate : item.Primary;
            while (group.CurrentBatch < item.Batch)
            {
                player.GlobalPosition = blocked + Vector2.Up * 7f;
                foreach (Node live in Children(actors)) Kill(live);
                await Frames(tree, 2);
                TestAssert.True(group.IsArrivalWarningActive && group.LiveActorCount == 0 && !group.IsComplete,
                    "real defeated prior batch opens warnings with no damaging later actor: " + item.Role);
                await Frames(tree, 24);
                TestAssert.Equal(0, group.LiveActorCount, "authored later batch never appears before .60s warning: " + item.Role);
                await Frames(tree, 16);
                TestAssert.True(group.LiveActorCount > 0 && !group.SpawnSuppressed,
                    "saved native candidates admit the later batch: " + item.Role);
            }
            RangedWeaponEnemy ranged = Children(actors).OfType<RangedWeaponEnemy>().Single();
            TestAssert.True(ranged.Role == item.Role && ranged.SceneFilePath == item.Path &&
                            ranged.GlobalPosition.DistanceTo(expected) < 3f,
                "birth selects the candidate not occupied by the current packed player: " + item.Role);
            tree.Root.CanvasTransform = new Transform2D(0f, tree.Root.GetVisibleRect().Size * .5f -
                (ranged.GlobalPosition + player.GlobalPosition) * .5f);
            int emissions = 0;
            ranged.ShotEmitted += (_, _) => emissions++;
            if (item.Role == RangedWeaponRole.Scatter)
            {
                // Both birth candidates stay380px apart. Blocking one is a
                // placement oracle, not a valid360px firing target.
                AssertAttackPrerequisites(tree, f, ranged, withinDetectionRange: false);
                for (int frame = 0; frame < 75; frame++) // Same1.25s ordinary window; inspect EVERY current state.
                {
                    await Frames(tree, 1);
                    TestAssert.Equal(0, emissions, "out-of-range saved Scatter zero emissions at normal frame=" + frame);
                    TestAssert.Equal(RangedWeaponState.Idle, ranged.CurrentFrame.State,
                        "out-of-range saved Scatter current Idle at normal frame=" + frame);
                    var leaves = new List<AttackTelegraphVisual>();
                    ranged.AppendAuthorizedAttackLeaves(leaves);
                    TestAssert.True(leaves.Count == 0 && ranged.CurrentFrame.AttackTimeline.Phase == AttackPhase.None,
                        "out-of-range saved Scatter zero current authorized leaves/AttackPhase.None at normal frame=" + frame);
                }
                await MoveScatterPlayerToSupportedFiringSite(tree, f, sector);
                tree.Root.CanvasTransform = new Transform2D(0f, tree.Root.GetVisibleRect().Size * .5f -
                    (ranged.GlobalPosition + player.GlobalPosition) * .5f);
            }
            AssertAttackPrerequisites(tree, f, ranged);
            await Until(tree, () => ranged.CurrentFrame.State == RangedWeaponState.Telegraph, 180);
            TestAssert.Equal(RangedWeaponState.Telegraph, ranged.CurrentFrame.State,
                "actual saved map weapon naturally warns with real LOS/viewport/player: " + item.Role);
            await Until(tree, () => emissions > 0, 180);
            TestAssert.True(emissions > 0, "actual saved map weapon naturally fires: " + item.Role);
            TestAssert.True(!group.IsComplete && completions == 0,
                "weapon firing is not fabricated encounter completion");
            if (item.Role == RangedWeaponRole.Burst)
            {
                TestAssert.Equal(2, group.LiveActorCount, "native Counterweight actually keeps both melee and Burst alive together");
                Node melee = Children(actors).Single(node => node != ranged);
                Kill(ranged);
                await Frames(tree, 3);
                TestAssert.True(group.LiveActorCount == 1 && !group.IsComplete && completions == 0,
                    "first real mixed defeat cannot advance or complete while its melee sibling lives");
                Kill(melee);
            }
            else Kill(ranged);
            await Frames(tree, 4);
            TestAssert.True(group.IsComplete && group.LiveActorCount == 0 && completions == 1,
                "all actual final-batch Defeated callbacks complete exactly once: " + item.Role);
            TestAssert.True(!progress.ActivatedMechanisms.Contains(group.EncounterId.ToString()),
                "non-gating new weapon groups do not mutate the seal prerequisite");
            GD.Print($"RANGED_PLACEMENT_NATIVE_SITE_OK role={item.Role} primary={item.Primary} alternate={item.Alternate} chosen={expected} mixed={item.Role == RangedWeaponRole.Burst}");
        }
        finally { await Dispose(tree, f); tree.Root.CanvasTransform = canvas; }
    }

    private static async Task MoveScatterPlayerToSupportedFiringSite(SceneTree tree, Fixture f, PrologueSector sector)
    {
        f.Player.GlobalPosition = new Vector2(2260, 3524);
        await Frames(tree, 1);
        f.Player.Velocity = Vector2.Down * 20f;
        f.Player.MoveAndSlide();
        Rect2 body = BodyBounds(f.Player, f.Player.GlobalPosition);
        TestAssert.True(f.Player.IsOnFloor() && f.Player.GlobalPosition.DistanceTo(new Vector2(2260, 3524)) < 1f &&
                        f.Group.Definition.ArenaBounds.Encloses(body) && !CanyonEncounterCatalog.ArrivalBodyOverlapsRest(body),
            "actual packed Scatter target moves to2260/3524 and obtains native floor contact with its whole body inside non-rest arena");
        StaticBody2D landing = sector.GetNode<StaticBody2D>("Gameplay/Collision/AbyssSafeBottom");
        CollisionShape2D floor = Children(landing).OfType<CollisionShape2D>().Single();
        Vector2 half = ((RectangleShape2D)floor.Shape).Size * .5f;
        Vector2 left = floor.ToGlobal(-half);
        Vector2 right = floor.ToGlobal(new Vector2(half.X, -half.Y));
        TestAssert.True(!floor.Disabled && (landing.CollisionLayer & CombatCollisionLayers.World) != 0 &&
                        left == new Vector2(1920, 3560) && right == new Vector2(2560, 3560) &&
                        body.Position.X >= left.X && body.End.X <= right.X && MathF.Abs(body.End.Y - left.Y) < 1f,
            "real saved AbyssSafeBottom top3560 continuously supports the packed player's entire foot width at the firing site");
        var excluded = new Godot.Collections.Array<Rid> { f.Player.GetRid() };
        using Godot.Collections.Array excludedOwner = (Godot.Collections.Array)excluded;
        using var query = new PhysicsShapeQueryParameters2D { Shape = f.Player.GetNode<CollisionShape2D>("BodyCollider").Shape,
            Transform = new Transform2D(0f, body.GetCenter() + Vector2.Up * .5f), Margin = 0f,
            CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy,
            CollideWithBodies = true, CollideWithAreas = false, Exclude = excluded };
        using Godot.Collections.Array hits = (Godot.Collections.Array)tree.Root.World2D.DirectSpaceState.IntersectShape(query, 32);
        TestAssert.Equal(0, hits.Count, "actual packed firing-site capsule is native-clear of World/enemies with only self excluded");
    }

    private static async Task MixedWarningReselectsAndNeedsBothDefeats(SceneTree tree)
    {
        Fixture f = CreateContractFixture(tree, mixed: true);
        try
        {
            await BeginMixedWarning(tree, f);
            await Frames(tree, 24);
            TestAssert.Equal(0, f.Group.LiveActorCount, "mixed later batch remains unmounted before warning deadline");
            f.Player.GlobalPosition = new Vector2(500, 300);
            await Frames(tree, 16);
            RangedWeaponEnemy ranged = Children(f.Actors).OfType<RangedWeaponEnemy>().Single();
            TestAssert.True(f.Group.LiveActorCount == 2 && ranged.GlobalPosition.DistanceTo(new Vector2(200, 300)) < 3f,
                "mixed pair resolves atomically and reselects against the current player at birth, not warning start");
            Rect2 a = BodyBounds(ranged, ranged.GlobalPosition);
            CharacterBody2D melee = Children(f.Actors).OfType<CharacterBody2D>().Single(node => node != ranged);
            TestAssert.True(!a.Intersects(BodyBounds(melee, melee.GlobalPosition)),
                "reserved candidates keep simultaneously added native bodies disjoint");
            int completed = 0;
            f.Group.GroupDefeated += _ => completed++;
            Kill(melee);
            await Frames(tree, 3);
            TestAssert.True(f.Group.LiveActorCount == 1 && !f.Group.IsComplete && completed == 0,
                "mixed melee defeat leaves living ranged sibling authoritative");
            Kill(ranged);
            await Frames(tree, 4);
            TestAssert.True(f.Group.IsComplete && completed == 1 && f.Group.LiveActorCount == 0,
                "both genuine mixed Health deaths advance exactly once, never Complete() shortcut");
        }
        finally { await Dispose(tree, f); }
    }

    private static async Task MixedWholeBatchSuppressionIsAtomic(SceneTree tree)
    {
        Fixture f = CreateContractFixture(tree, mixed: true);
        try
        {
            await BeginMixedWarning(tree, f);
            AddOwnedFloor(f, "BlockPrimary", new Vector2(200, 270), new Vector2(55, 40));
            AddOwnedFloor(f, "BlockAlternate", new Vector2(500, 270), new Vector2(55, 40));
            await Frames(tree, 40);
            TestAssert.True(f.Group.SpawnSuppressed && f.Group.LiveActorCount == 0 &&
                            f.Actors.GetChildCount() == 0 && !f.Group.IsComplete && f.Group.IsActive,
                "RANGED_PLACEMENT_ATOMIC_SUPPRESSION_RED: illegal ranged pair suppresses the entire mixed batch, never leaves a partial melee victory");
            TestAssert.True(!f.Progress.ActivatedMechanisms.Contains(f.Group.EncounterId.ToString()),
                "suppression cannot fabricate progression");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: both capsule queries run before AddChild and overlook the
    // simultaneously reserved melee body, producing two stacked actors.
    private static async Task MixedReservedOverlapSuppressesWholeBatch(SceneTree tree)
    {
        Fixture f = CreateContractFixture(tree, mixed: true, reservedOverlap: true);
        try
        {
            await BeginMixedWarning(tree, f);
            await Frames(tree, 40);
            TestAssert.True(f.Group.SpawnSuppressed && f.Group.LiveActorCount == 0 &&
                            f.Actors.GetChildCount() == 0 && !f.Group.IsComplete,
                "RANGED_PLACEMENT_RESERVED_OVERLAP_RED: player-blocked alternate plus reserved-melee primary overlap suppresses both before AddChild");
        }
        finally { await Dispose(tree, f); }
    }

    private static async Task MixedPendingWarningsCancelOnLifecycle(SceneTree tree)
    {
        foreach (string reason in new[] { "leave", "reset", "death", "remove" })
        {
            Fixture f = CreateContractFixture(tree, mixed: true);
            try
            {
                await BeginMixedWarning(tree, f);
                int completed = 0;
                f.Group.GroupDefeated += _ => completed++;
                if (reason == "leave") f.Player.GlobalPosition = new Vector2(1200, 300);
                if (reason == "reset")
                {
                    f.Player.GlobalPosition = new Vector2(1200, 300);
                    f.Group.ResetForCheckpoint();
                }
                if (reason == "death") Kill(f.Player);
                if (reason == "remove") f.Group.QueueFree();
                await Frames(tree, 44);
                TestAssert.True(f.Actors.GetChildCount() == 0 && completed == 0 &&
                    (!GodotObject.IsInstanceValid(f.Group) || !f.Group.IsComplete && !f.Group.IsArrivalWarningActive),
                    "pending mixed warning cannot late-spawn or complete after " + reason);
                if (reason is "leave" or "reset")
                {
                    f.Player.GlobalPosition = new Vector2(440, 300);
                    await Frames(tree, 4);
                    TestAssert.True(f.Group.CurrentBatch == 0 && f.Group.LiveActorCount == 1 && !f.Group.IsComplete,
                        "re-entry restarts original first batch after mixed warning " + reason);
                }
            }
            finally { await Dispose(tree, f); }
        }
    }

    private static async Task BeginMixedWarning(SceneTree tree, Fixture f)
    {
        await Frames(tree, 4);
        TestAssert.True(f.Group.CurrentBatch == 0 && f.Group.LiveActorCount == 1,
            "mixed fixture starts with a real original actor");
        Kill(f.Actors.GetChild(0));
        await Frames(tree, 2);
        TestAssert.True(f.Group.IsArrivalWarningActive && f.Group.LiveActorCount == 0,
            "RANGED_PLACEMENT_MIXED_WARNING_RED: the whole later mixed batch uses visible .60s warnings before either actor is born");
    }

    private static Fixture CreateContractFixture(SceneTree tree, bool mixed, bool reservedOverlap = false,
        bool unsupportedRust = false)
    {
        var root = new Node2D { Name = mixed ? "MixedWeaponArrivalContract" : "WeaponSpawnEnvelopeContract" };
        var actors = new Node2D { Name = "Actors", Position = new Vector2(73, 41) };
        CharacterBody2D player = PackedPlayer(new Vector2(440, 300));
        var definition = new CanyonEncounterDefinition("canyon.v2.encounter.weapon-contract", CanyonRouteNodeId.GearPistons,
            false, new Rect2(80, 120, 1000, 500), new Rect2(80, 120, 1000, 500),
            Array.AsReadOnly(mixed ? new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(700, 297), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(350, unsupportedRust ? 293 : 297), 1),
                new CanyonEnemyPlacement(Burst, new Vector2(reservedOverlap ? 350 : 200, 300), 1, new Vector2(500, 300))
            } : new[] { new CanyonEnemyPlacement(Scatter, new Vector2(200, 300), 0) }));
        var group = new CanyonEncounterGroup { Name = "Encounter", EncounterId = definition.StableId,
            Definition = definition, ActorRootPath = new NodePath("../Actors") };
        var progress = new PrologueProgress();
        group.ApplyProgress(progress);
        var f = new Fixture(root, actors, player, group, progress, new(), new());
        actors.ChildEnteredTree += child => child.Ready += () =>
        {
            if (child is Node2D actor) f.ReadyPositions[actor.GetInstanceId()] = actor.GlobalPosition;
        };
        AddOwnedFloor(f, "Floor", new Vector2(580, 339), new Vector2(1100, 20));
        root.AddChild(player);
        root.AddChild(actors);
        root.AddChild(group);
        tree.Root.AddChild(root);
        PreparePlayer(player);
        return f;
    }

    private static CharacterBody2D PackedPlayer(Vector2 position)
    {
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = position;
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        player.AddToGroup("player");
        return player;
    }

    private static void PreparePlayer(CharacterBody2D player)
    {
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        health.MaxHealth = 1000;
        health.InvulnerabilitySeconds = 0f;
        health.ResetFull();
    }

    private static void AssertAttackPrerequisites(SceneTree tree, Fixture f, RangedWeaponEnemy actor,
        bool withinDetectionRange = true)
    {
        Vector2 muzzle = actor.GetNode<Node2D>("Muzzle").GlobalPosition;
        TestAssert.True(f.Player.IsInsideTree() && f.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0 &&
                        actor.GetNode<HealthComponent>("Health").CurrentHealth > 0,
            "native placement has actual living packed player and living saved weapon actor");
        TestAssert.True(f.Group.Definition.ArenaBounds.Encloses(BodyBounds(actor, actor.GlobalPosition)) &&
                        f.Group.Definition.ArenaBounds.HasPoint(muzzle) &&
                        !CanyonEncounterCatalog.ArrivalBodyOverlapsRest(BodyBounds(actor, actor.GlobalPosition)),
            "actual packed capsule and muzzle are legal in authored non-rest arena");
        float distance = muzzle.DistanceTo(f.Player.GlobalPosition);
        TestAssert.True(withinDetectionRange ? distance < actor.Settings.DetectionRange : distance > actor.Settings.DetectionRange,
            withinDetectionRange ? "real native target is within the role's detection distance" :
                "real native birth-blocker target is explicitly beyond the unchanged role detection distance");
        TestAssert.True(tree.Root.GetVisibleRect().HasPoint(actor.GetNode<Node2D>("Muzzle").GetGlobalTransformWithCanvas().Origin) &&
                        tree.Root.GetVisibleRect().HasPoint(f.Player.GetGlobalTransformWithCanvas().Origin),
            "actual viewport contains the actual player and weapon muzzle");
        TestAssert.True(RangedAttackGeometry.HasClearWorldPath(actor.GetWorld2D().DirectSpaceState, muzzle, f.Player.GlobalPosition),
            "native world LOS, not a fabricated visible flag, admits the actual living player");
    }

    private static void ValidateCandidate(SceneTree tree, Fixture f, string path, Vector2 site)
    {
        CharacterBody2D probe = GD.Load<PackedScene>(path).Instantiate<CharacterBody2D>();
        try
        {
            Rect2 body = BodyBounds(probe, site);
            TestAssert.True(f.Group.Definition.ArenaBounds.Encloses(body) && !CanyonEncounterCatalog.ArrivalBodyOverlapsRest(body),
                $"candidate full saved capsule stays in arena and outside rest: {path} {site}");
            CollisionShape2D collider = probe.GetNode<CollisionShape2D>("BodyCollider");
            using var query = new PhysicsShapeQueryParameters2D { Shape = collider.Shape,
                Transform = new Transform2D(0f, site + collider.Position + Vector2.Up * .5f),
                CollisionMask = CombatCollisionLayers.World, CollideWithBodies = true, CollideWithAreas = false };
            using Godot.Collections.Array hits = (Godot.Collections.Array)tree.Root.World2D.DirectSpaceState.IntersectShape(query, 32);
            TestAssert.Equal(0, hits.Count, $"native world query leaves full candidate capsule clear: {path} {site}");
            var intervals = new List<(float Left, float Right)>();
            foreach (StaticBody2D floor in Descendants(f.Root).OfType<StaticBody2D>())
            {
                if ((floor.CollisionLayer & CombatCollisionLayers.World) == 0) continue;
                foreach (CollisionShape2D shape in Children(floor).OfType<CollisionShape2D>())
                {
                    if (shape.Disabled || shape.Shape is not RectangleShape2D rectangle) continue;
                    Vector2 half = rectangle.Size * .5f;
                    Vector2 left = shape.ToGlobal(-half);
                    Vector2 right = shape.ToGlobal(new Vector2(half.X, -half.Y));
                    Vector2 bottomLeft = shape.ToGlobal(new Vector2(-half.X, half.Y));
                    if (MathF.Abs(left.Y - right.Y) < .05f && MathF.Abs(left.X - bottomLeft.X) < .05f &&
                        MathF.Abs(left.Y - body.End.Y) < 1.25f)
                        intervals.Add((MathF.Min(left.X, right.X), MathF.Max(left.X, right.X)));
                }
            }
            float covered = body.Position.X;
            foreach (var interval in intervals.OrderBy(interval => interval.Left))
            {
                if (interval.Right <= covered) continue;
                if (interval.Left > covered + .05f) break;
                covered = interval.Right;
                if (covered >= body.End.X - .05f) break;
            }
            TestAssert.True(covered >= body.End.X - .05f,
                $"continuous native rectangle top edges support the whole saved-capsule foot width: {path} {site}");
        }
        finally { probe.Free(); }
    }

    private static Rect2 BodyBounds(CharacterBody2D actor, Vector2 position)
    {
        CollisionShape2D collider = actor.GetNode<CollisionShape2D>("BodyCollider");
        Vector2 half = collider.Shape switch
        {
            CapsuleShape2D capsule => new Vector2(capsule.Radius, capsule.Height * .5f),
            RectangleShape2D rectangle => rectangle.Size * .5f,
            _ => throw new InvalidOperationException("Placement proof requires actual packed capsule or rectangle")
        };
        return new Rect2(position + collider.Position - half, half * 2f);
    }

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (Node child in Children(node))
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private static Node[] Children(Node node)
    {
        using Godot.Collections.Array children = (Godot.Collections.Array)node.GetChildren();
        var snapshot = new Node[children.Count];
        for (int i = 0; i < children.Count; i++) snapshot[i] = (Node)children[i].AsGodotObject();
        return snapshot;
    }

    private static void AddOwnedFloor(Fixture f, string name, Vector2 center, Vector2 size)
    {
        var shape = new RectangleShape2D { Size = size };
        f.Resources.Add(shape);
        var floor = new StaticBody2D { Name = name, Position = center, CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = shape });
        f.Root.AddChild(floor);
    }

    private static void Kill(Node actor)
    {
        HealthComponent health = actor.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0f, ++_damageId)),
            "actual HealthComponent damage produces the production Defeated event");
    }

    private static async Task Until(SceneTree tree, Func<bool> condition, int frames)
    {
        for (int i = 0; i < frames && !condition(); i++) await Frames(tree, 1);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task Dispose(SceneTree tree, Fixture f)
    {
        if (GodotObject.IsInstanceValid(f.Player)) f.Player.RemoveFromGroup("player");
        if (GodotObject.IsInstanceValid(f.Root)) f.Root.QueueFree();
        await Frames(tree, 3);
        foreach (Resource resource in f.Resources) resource.Dispose();
    }

    private sealed record MapCase(PrologueSectorId Sector, string Scene, string Group, string Path,
        RangedWeaponRole Role, Vector2 Activation, Vector2 Primary, Vector2 Alternate, int Batch);
    private sealed record Fixture(Node2D Root, Node2D Actors, CharacterBody2D Player, CanyonEncounterGroup Group,
        PrologueProgress Progress, List<Resource> Resources, Dictionary<ulong, Vector2> ReadyPositions);
}

// Test-only observer for the isolated saved sector's unchanged recovery volumes.
// Intentionally cannot implement a successful respawn or hide an unsafe route.
public partial class RangedFairnessRecoveryProbe : Node2D, ILevelRecovery
{
    public int RecoveryRequests { get; private set; }
    public void RequestRespawn(string reason)
    {
        RecoveryRequests++;
        GD.Print($"RANGED_FAIRNESS_RECOVERY_REQUEST reason={reason} physics={Engine.GetPhysicsFrames()}");
    }
}

#if RANGED_SPAWN_IDENTITY
// Conditional diagnosis only: neither this type nor its Group call sites exist
// in the ordinary build. Rows/tokens contain no GodotObject or borrowed resource.
internal readonly record struct RangedSpawnIdentityRow
{
    public long Sequence { get; init; }
    public int Fixture { get; init; }
    public long Operation { get; init; }
    public string Kind { get; init; }
    public string Purpose { get; init; }
    public string Edge { get; init; }
    public string ScenePath { get; init; }
    public ulong GroupId { get; init; }
    public ulong SceneId { get; init; }
    public int SceneManagedHash { get; init; }
    public ulong InstanceId { get; init; }
    public string InstanceType { get; init; }
    public int Batch { get; init; }
    public ulong Generation { get; init; }
    public int ActorCount { get; init; }
    public bool PhysicsKnown { get; init; }
    public ulong Physics { get; init; }
    public ulong Usec { get; init; }
    public long ManagedTicks { get; init; }
    public int Thread { get; init; }
    public string ErrorText { get; init; }
}

internal sealed class RangedSpawnTraceBuffer
{
    internal readonly record struct Snapshot(RangedSpawnIdentityRow[] Rows, long Total,
        long Dropped, long NativeErrors, long ObservationErrors);
    private readonly object _sync = new();
    private readonly RangedSpawnIdentityRow[] _rows;
    private int _count;
    private long _total, _dropped, _nativeErrors, _observationErrors;
    private bool _stopped;
    public RangedSpawnTraceBuffer(int capacity = 256)
    {
        if (capacity is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(capacity));
        _rows = new RangedSpawnIdentityRow[capacity];
    }
    public long Record(RangedSpawnIdentityRow row)
    {
        lock (_sync)
        {
            if (_stopped) return 0;
            long sequence = ++_total;
            if (row.Kind == "native_error") _nativeErrors++;
            if (row.Kind == "observation_error") _observationErrors++;
            if (_count == _rows.Length) { _dropped++; return sequence; }
            string error = row.ErrorText;
            if (error?.Length > 512) error = error[..512];
            _rows[_count++] = row with { Sequence = sequence, ErrorText = error };
            return sequence;
        }
    }
    public Snapshot Freeze()
    {
        lock (_sync)
        {
            _stopped = true;
            return new Snapshot(_rows[.._count], _total, _dropped, _nativeErrors, _observationErrors);
        }
    }
}

internal static class RangedSpawnIdentityTrace
{
    internal readonly record struct Token(RangedSpawnIdentityRow Row);
    private static RangedSpawnTraceBuffer _active;
    private static int _fixture;
    private static long _operation;

    private static RangedSpawnIdentityRow Stamp(RangedSpawnIdentityRow row) => row with
    {
        Fixture = System.Threading.Volatile.Read(ref _fixture),
        ManagedTicks = System.Diagnostics.Stopwatch.GetTimestamp(),
        Thread = System.Environment.CurrentManagedThreadId
    };

    private static void Record(RangedSpawnIdentityRow row) =>
        System.Threading.Volatile.Read(ref _active)?.Record(Stamp(row));

    private static bool Target(CanyonEncounterGroup group) =>
        System.Threading.Volatile.Read(ref _active) != null &&
        group.EncounterId.ToString() == "canyon.v2.encounter.counterweight-lift";

    internal static Token Begin(CanyonEncounterGroup group, string purpose, string scenePath,
        PackedScene scene, int batch, ulong generation)
    {
        if (!Target(group)) return default;
        try
        {
            var row = new RangedSpawnIdentityRow
            {
                Operation = System.Threading.Interlocked.Increment(ref _operation),
                Kind = "op", Purpose = purpose, Edge = "begin", ScenePath = scenePath,
                GroupId = group.GetInstanceId(), Batch = batch, Generation = generation,
                SceneId = scene?.GetInstanceId() ?? 0,
                SceneManagedHash = scene == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(scene),
                PhysicsKnown = true, Physics = Engine.GetPhysicsFrames(), Usec = Time.GetTicksUsec()
            };
            Record(row);
            return new Token(row);
        }
        catch (Exception error) { ObservationError(error); return default; }
    }

    internal static Token Returned(Token token, Node instance)
    {
        if (token.Row.Operation == 0) return default;
        try
        {
            // Only the returned instance is queried. Never re-read PackedScene.
            var row = token.Row with
            {
                Edge = "returned", InstanceId = instance?.GetInstanceId() ?? 0,
                InstanceType = instance?.GetType().Name ?? "null",
                Physics = Engine.GetPhysicsFrames(), Usec = Time.GetTicksUsec()
            };
            Record(row);
            return new Token(row);
        }
        catch (Exception error) { ObservationError(error); return token; }
    }

    internal static void FreeEdge(Token token, string edge)
    {
        if (token.Row.Operation == 0) return;
        // Token only: in particular, no candidate access after Free.
        Record(token.Row with { Kind = "free", Edge = edge, PhysicsKnown = false, Physics = 0, Usec = 0 });
    }

    internal static Token GroupEdge(CanyonEncounterGroup group, string purpose, string edge,
        int batch, ulong generation, int actorCount)
    {
        if (!Target(group)) return default;
        try
        {
            var row = new RangedSpawnIdentityRow
            {
                Kind = "group", Purpose = purpose, Edge = edge, GroupId = group.GetInstanceId(),
                Batch = batch, Generation = generation, ActorCount = actorCount,
                PhysicsKnown = true, Physics = Engine.GetPhysicsFrames(), Usec = Time.GetTicksUsec()
            };
            Record(row);
            return new Token(row);
        }
        catch (Exception error) { ObservationError(error); return default; }
    }

    internal static void GroupReturned(Token token, int batch, ulong generation, int actorCount)
    {
        if (token.Row.GroupId == 0) return;
        Record(token.Row with { Edge = "returned", Batch = batch, Generation = generation,
            ActorCount = actorCount, PhysicsKnown = false, Physics = 0, Usec = 0 });
    }

    private static void ObservationError(Exception error) =>
        Record(new RangedSpawnIdentityRow { Kind = "observation_error", ErrorText = error.GetType().Name });

    internal static void NativeError(RangedSpawnTraceBuffer buffer, string function, string file,
        int line, string code, string rationale)
    {
        // Logger callbacks can be off-thread. No Godot calls/backtrace inspection.
        static string Bound(string value, int maximum) =>
            value == null ? "" : value.Length <= maximum ? value : value[..maximum];
        buffer.Record(Stamp(new RangedSpawnIdentityRow
        {
            Kind = "native_error", PhysicsKnown = false,
            ErrorText = Bound(file, 100) + ":" + line + " " + Bound(function, 80) + ": " +
                Bound(code, 160) + " " + Bound(rationale, 160)
        }));
    }

    private static void BufferContract()
    {
        var buffer = new RangedSpawnTraceBuffer(2);
        buffer.Record(new RangedSpawnIdentityRow { Sequence = 99, Fixture = 1, Kind = "op",
            Purpose = "live", Edge = "begin", Operation = 7, SceneId = 42, PhysicsKnown = true, Physics = 10 });
        buffer.Record(new RangedSpawnIdentityRow { Fixture = 1, Kind = "native_error",
            PhysicsKnown = false, ErrorText = "contract probe" });
        buffer.Record(new RangedSpawnIdentityRow { Fixture = 1, Kind = "op", Edge = "returned", Operation = 7 });
        var frozen = buffer.Freeze();
        TestAssert.True(frozen.Rows.Length == 2 && frozen.Total == 3 && frozen.Dropped == 1 &&
            frozen.NativeErrors == 1 && frozen.Rows[0].Sequence == 1 && frozen.Rows[1].Sequence == 2 &&
            frozen.Rows[0].SceneId == 42 && frozen.Rows[0].Operation == 7 &&
            !frozen.Rows[1].PhysicsKnown,
            "RANGED_SPAWN_TRACE_MODEL_RED: ordered bounded scalar prefix/error count must survive overflow");
        buffer.Record(new RangedSpawnIdentityRow { Kind = "native_error", ErrorText = "late" });
        var stopped = buffer.Freeze();
        TestAssert.True(stopped.Total == 3 && stopped.NativeErrors == 1 &&
            System.Text.Json.JsonSerializer.Serialize(frozen) == System.Text.Json.JsonSerializer.Serialize(stopped),
            "spawn trace stop is idempotent and rejects later callback records");
        bool rejected = false;
        try { _ = new RangedSpawnTraceBuffer(0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        TestAssert.True(rejected, "spawn trace rejects an unbounded/zero capacity contract");
        var primary = new InvalidOperationException("primary-contract-probe");
        Exception observed = null;
        try
        {
            try { throw primary; }
            finally
            {
                ValidateWhenCompleted(false, () => throw new InvalidOperationException("secondary-contract-probe"));
            }
        }
        catch (Exception error) { observed = error; }
        TestAssert.True(ReferenceEquals(primary, observed),
            "RANGED_SPAWN_TRACE_PRIMARY_FAILURE_RED: cleanup evidence validation must preserve the original fixture exception");
        bool validated = false;
        ValidateWhenCompleted(true, () => validated = true);
        TestAssert.True(validated, "a returned fixture still requires its native trace validation");
        CleanupContract();
        GD.Print("RANGED_SPAWN_TRACE_MODEL_OK overflow=true stop=true native_physics_unknown=true");
    }

    private static void CleanupContract()
    {
        var primary = new InvalidOperationException("fixture-primary-probe");
        var unhook = new InvalidOperationException("unhook-secondary-probe");
        var dump = new InvalidOperationException("dump-secondary-probe");
        var steps = new List<string>();
        Exception observed = null;
        try
        {
            FinishSession(System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary),
                () => { steps.Add("unhook"); throw unhook; },
                () => steps.Add("stop"),
                () => { steps.Add("dump"); throw dump; },
                () => steps.Add("validate"), _ => steps.Add("secondary"));
        }
        catch (Exception error) { observed = error; }
        TestAssert.True(ReferenceEquals(primary, observed) &&
            steps.Take(4).SequenceEqual(new[] { "unhook", "stop", "dump", "validate" }) &&
            steps.Count(step => step == "secondary") == 2,
            "RANGED_SPAWN_TRACE_UNWIND_RED: shared finalization must attempt every step and preserve the first fixture failure");

        steps.Clear();
        observed = null;
        try
        {
            FinishSession(null, () => { steps.Add("unhook"); throw unhook; },
                () => steps.Add("stop"), () => { steps.Add("dump"); throw dump; },
                () => steps.Add("validate"), _ => steps.Add("secondary"));
        }
        catch (Exception error) { observed = error; }
        TestAssert.True(ReferenceEquals(unhook, observed) &&
            steps.Take(4).SequenceEqual(new[] { "unhook", "stop", "dump", "validate" }) &&
            steps.Count(step => step == "secondary") == 1,
            "a successful body cannot waive its first cleanup failure or skip later steps");

        observed = null;
        try
        {
            FinishSession(System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary),
                () => throw unhook, () => { }, () => { }, () => { },
                _ => throw new InvalidOperationException("secondary-report-probe"));
        }
        catch (Exception error) { observed = error; }
        TestAssert.True(ReferenceEquals(primary, observed) &&
            primary.Data["RangedSpawnCleanupSecondary"] is string[] notes &&
            notes.Any(note => note.StartsWith("report:", StringComparison.Ordinal)),
            "a failed secondary reporter cannot replace the primary and must retain its own failure");

        // Own the wrapper before registration. Inject only after real OS.AddLogger
        // returns, then exercise the same finalizer used by the fixture session.
        var logger = new RangedSpawnIdentityLogger(new RangedSpawnTraceBuffer());
        var attachment = new InvalidOperationException("attachment-primary-probe");
        System.Runtime.ExceptionServices.ExceptionDispatchInfo attachFailure = null;
        try { logger.Attach(() => throw attachment); }
        catch (Exception error) { attachFailure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error); }
        observed = null;
        bool stopped = false, dumped = false;
        try
        {
            FinishSession(attachFailure, logger.Dispose, () => stopped = true,
                () => dumped = true, () => { }, message => Console.Error.WriteLine(message));
        }
        catch (Exception error) { observed = error; }
        TestAssert.True(ReferenceEquals(attachment, observed) && logger.RegistrationAttempted &&
            logger.DisposalAttempted && stopped && dumped,
            "failed attachment retains ownership and attempts real native unhook/disposal before preserving the primary");
        logger.Dispose();
        GD.Print("RANGED_SPAWN_TRACE_UNWIND_OK primary=true cleanup_only_fails=true reporter_failure_retained=true attachment_cleanup=true");
    }

    internal static void FinishSession(System.Runtime.ExceptionServices.ExceptionDispatchInfo primary,
        Action unhook, Action stop, Action dump, Action validate, Action<string> reportSecondary)
    {
        var first = primary;
        var secondary = new List<string>(4);
        void Attempt(string edge, Action step)
        {
            try { step(); }
            catch (Exception error)
            {
                if (first == null) first = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error);
                else secondary.Add(edge + ":" + error.GetType().Name);
            }
        }
        Attempt("unhook", unhook);
        Attempt("stop", stop);
        Attempt("dump", dump);
        Attempt("validate", validate);
        int reportCount = secondary.Count;
        for (int index = 0; index < reportCount; index++)
            Attempt("report", () => reportSecondary("RANGED_SPAWN_CLEANUP_SECONDARY " + secondary[index]));
        if (secondary.Count > 0 && first != null)
        {
            // Bounded scalar fallback if even the output sink fails. Never let
            // evidence attachment replace the original exception either.
            try { first.SourceException.Data["RangedSpawnCleanupSecondary"] = secondary.ToArray(); }
            catch (Exception) { }
        }
        first?.Throw();
    }

    private static void ValidateWhenCompleted(bool completed, Action validate)
    {
        if (completed) validate();
    }

    internal static async Task Run(SceneTree tree, bool contract)
    {
        BufferContract();
        var buffer = new RangedSpawnTraceBuffer();
        TestAssert.True(System.Threading.Interlocked.CompareExchange(ref _active, buffer, null) == null,
            "spawn identity has one owned active session");
        _operation = 0;
        RangedSpawnIdentityLogger logger = null;
        bool completed = false;
        System.Runtime.ExceptionServices.ExceptionDispatchInfo primary = null;
        try
        {
            logger = new RangedSpawnIdentityLogger(buffer);
            logger.Attach();
            int count = contract ? 1 : 4;
            for (int index = 1; index <= count; index++)
            {
                System.Threading.Volatile.Write(ref _fixture, index);
                Record(new RangedSpawnIdentityRow { Kind = "fixture", Edge = "begin" });
                bool returned = false;
                try
                {
                    await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(tree, resourcePolicy: true,
                        preparationAttacks: index == 2 ? 7 : 0,
                        counterProof: index == 3 ? RangedWeaponPlacementRuntimeTests.MixedCounterProof.RejectShort :
                            index == 4 ? RangedWeaponPlacementRuntimeTests.MixedCounterProof.Connect :
                            RangedWeaponPlacementRuntimeTests.MixedCounterProof.None);
                    returned = true;
                }
                finally { Record(new RangedSpawnIdentityRow { Kind = "fixture", Edge = returned ? "returned" : "failed" }); }
            }
            completed = true;
        }
        catch (Exception error) { primary = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error); }
        finally
        {
            RangedSpawnTraceBuffer.Snapshot snapshot = default;
            FinishSession(primary, () => logger?.Dispose(), () =>
            {
                System.Threading.Interlocked.CompareExchange(ref _active, null, buffer);
                snapshot = buffer.Freeze();
            }, () =>
            {
                GD.Print($"RANGED_SPAWN_IDENTITY_BEGIN retained={snapshot.Rows.Length} total={snapshot.Total} " +
                    $"dropped={snapshot.Dropped} native_errors={snapshot.NativeErrors} observation_errors={snapshot.ObservationErrors}");
                foreach (var row in snapshot.Rows)
                    GD.Print("RANGED_SPAWN_IDENTITY_ROW " + System.Text.Json.JsonSerializer.Serialize(row));
                GD.Print("RANGED_SPAWN_IDENTITY_END live_loop_stdout=false native_error_waived=false");
            }, () =>
            {
                if (contract) ValidateWhenCompleted(completed, () =>
                {
                    var begins = snapshot.Rows.Where(row => row.Kind == "op" && row.Edge == "begin").ToArray();
                    var returns = snapshot.Rows.Where(row => row.Kind == "op" && row.Edge == "returned").ToArray();
                    TestAssert.True(begins.Length == returns.Length && begins.Length > 0 &&
                        begins.Select(row => row.Operation).Distinct().Count() == begins.Length &&
                        begins.All(begin => returns.Count(end => end.Operation == begin.Operation &&
                            end.Fixture == begin.Fixture && end.GroupId == begin.GroupId &&
                            end.SceneId == begin.SceneId && end.SceneManagedHash == begin.SceneManagedHash &&
                            end.ScenePath == begin.ScenePath && end.Purpose == begin.Purpose && end.InstanceId != 0) == 1) &&
                        snapshot.Rows.Where(row => row.Kind == "free" && row.Edge == "begin").All(begin =>
                            snapshot.Rows.Count(end => end.Kind == "free" && end.Edge == "returned" &&
                                end.Operation == begin.Operation && end.InstanceId == begin.InstanceId) == 1),
                        "spawn identity pairs every returned native operation and preflight Free without resource re-read");
                    TestAssert.True(snapshot.Rows.Any(row => row.Kind == "op" && row.Purpose == "live" &&
                        row.Edge == "begin" && row.ScenePath == "res://scenes/enemies/BeltBurstGunner.tscn" && row.SceneId != 0) &&
                        snapshot.Rows.Any(row => row.Kind == "op" && row.Purpose == "preflight" && row.Edge == "returned" &&
                            row.InstanceId != 0) && snapshot.Rows.Any(row => row.Kind == "free" && row.Edge == "returned") &&
                        snapshot.Dropped == 0 && snapshot.NativeErrors == 0 && snapshot.ObservationErrors == 0,
                        "RANGED_SPAWN_TRACE_NATIVE_RED: actual saved birth/preflight/free identity boundaries must reach the bounded trace");
                });
            }, message => Console.Error.WriteLine(message));
        }
    }
}

internal sealed partial class RangedSpawnIdentityLogger : Logger, IDisposable
{
    private readonly RangedSpawnTraceBuffer _buffer;
    private bool _disposed;
    internal bool RegistrationAttempted { get; private set; }
    internal bool DisposalAttempted => _disposed;
    internal RangedSpawnIdentityLogger(RangedSpawnTraceBuffer buffer) { _buffer = buffer; }
    internal void Attach(Action afterRegistration = null)
    {
        if (RegistrationAttempted || _disposed) throw new InvalidOperationException("spawn logger attachment is single-use");
        RegistrationAttempted = true;
        OS.AddLogger(this);
        afterRegistration?.Invoke();
    }
    public override void _LogError(string function, string file, int line, string code,
        string rationale, bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType != (int)ErrorType.Warning)
            RangedSpawnIdentityTrace.NativeError(_buffer, function, file, line, code, rationale);
    }
    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        RangedSpawnIdentityTrace.FinishSession(null,
            () => { if (RegistrationAttempted) OS.RemoveLogger(this); },
            () => base.Dispose(), () => { }, () => { },
            message => Console.Error.WriteLine(message));
    }
}
#endif
