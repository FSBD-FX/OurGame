#nullable enable
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Enemies.Sentry;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knight.Tests.Presentation;

// Regional generated-level evidence: one disclosed initial pose, then physical
// input only. No actor suppression, tuning changes, checkpoint repair or grants.
public static class CanyonSteamDiverterRouteProbe
{
    public static async Task Run(SceneTree tree, string route, string storage)
    {
        if (route == "frozen") await new FrozenProbe(tree, storage).Run();
        else if (route == "trench") await new TrenchProbe(tree, storage).Run();
        else throw new ArgumentException("Task17B requires exact frozen or trench route.");
    }

    // Dedicated second regional proof. Duplicated small capture/input routines
    // keep the already-proved Frozen sequence untouched; no generic combat driver.
    private sealed class TrenchProbe
    {
        private const int MaximumPhysicsFrames = 5400;
        private readonly SceneTree _tree;
        private readonly string _output;
        private readonly Dictionary<Key, bool> _keys = new();
        private readonly Dictionary<MouseButton, bool> _buttons = new();
        private PrologueLevelController _level = null!;
        private PrologueSector _sector = null!;
        private CharacterBody2D _player = null!;
        private HealthComponent _health = null!;
        private MechanicalClawController _claw = null!;
        private PlayerActionModeController _modes = null!;
        private PlayerCombat _combat = null!;
        private PlayerShieldController _shield = null!;
        private PlayerMotor _motor = null!;
        private CanyonEncounterGroup _group = null!;
        private CanyonSteamDiverter _pair = null!;
        private CanyonSteamJet _left = null!, _right = null!, _oldJet = null!;
        private CanyonSteamValve _oldValve = null!;
        private ThreadTarget2D _control = null!;
        private LegacyThreadInteractionAdapter _adapter = null!;
        private StaticBody2D _floor = null!;
        private BellBackLaborer _laborer = null!;
        private BellBoltSentry _sentry = null!;
        private StreamWriter? _metadata;
        private Label? _stageLabel;
        private Vector2 _pointer;
        private ulong _startedPhysics;
        private ulong _lastLatchTarget;
        private string _stage = "startup";
        private string? _lastRoute;
        private int _snapshot, _recoveries, _heals, _damage, _latches;
        private int _laborerDefeats, _laborerDeaths, _laborerDamage, _sentryDefeats, _sentryDeaths, _sentryDamage;
        private int _attacksStarted, _attacksConnected, _blocks;
        private bool _setup, _combatBound, _arrivalSeen, _climbing, _sentryInterrupted;
        private int? _routeHealth;
        private Rect2 _oldJetBounds;
        private float _oldJetLeftClear, _oldJetRightClear, _oldJetStageMargin;
        private int _oldJetPassages, _oldJetActiveOverlaps, _oldJetPassageActiveOverlaps;
        private bool _oldJetPassage, _oldJetOverlapWasActive;
        private int _castFailures;
        private MechanicalClawFailure _lastCastFailure;

        public TrenchProbe(SceneTree tree, string storage)
        { _tree = tree; _output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(storage))!, "proof"); }

        public async Task Run()
        {
            ValidateCapture();
            Directory.CreateDirectory(Path.Combine(_output, "frames"));
            _metadata = new StreamWriter(new FileStream(Path.Combine(_output, "metadata.jsonl"), FileMode.CreateNew,
                System.IO.FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            Record("invocation", new { route = "trench", adapter = RenderingServer.GetVideoAdapterName(),
                audio = AudioServer.GetDriverName(), movie = Engine.GetWriteMoviePath(),
                limits = "generated-regional;one-initial-pose;actual-combat-and-input;silent-fixed60;not-human-realtime" });
            try
            {
                _startedPhysics = Engine.GetPhysicsFrames();
                _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
                var flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
                var paths = IsolatedPrologueFixture.Configure(_level, "task17b-trench-input-movie");
                Record("isolated_before_mount", paths);
                _level.RecoveryRequested += _ => _recoveries++;
                _tree.Root.AddChild(_level);
                for (int i = 0; i < 4; i++) await Step();
                Require(flow.State == PrologueGameFlowState.Title && !flow.HasSave, "isolated empty Title");
                flow.StartNew(); Require(flow.State == PrologueGameFlowState.Playing, "actual StartNew enters Playing");
                _player = _level.Player; _health = _player.GetNode<HealthComponent>("Health");
                _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
                _modes = _player.GetNode<PlayerActionModeController>("ActionModes");
                _combat = _player.GetNode<PlayerCombat>("Combat"); _shield = _player.GetNode<PlayerShieldController>("Shield");
                _motor = _player.GetNode<PlayerMotor>("Motor");
                _health.Damaged += (remaining, impulse) => { _damage++; Record("actual_player_damage", new { remaining, impulse = Point(impulse) }); };
                _health.Healed += (restored, remaining) => { _heals++; Record("actual_player_heal", new { restored, remaining }); };
                _combat.AttackStarted += (id, direction) => { _attacksStarted++; Record("actual_attack_started", new { id, direction }); };
                _combat.AttackConnected += hurtbox => { _attacksConnected++; Record("actual_attack_connected", new { target = hurtbox.GetInstanceId(), path = hurtbox.GetPath().ToString() }); };
                _shield.ShieldResolved += (result, transition) => { if (result.Guarded && result.DamageAmount == 0) _blocks++; Record("actual_shield_result", new { result.Guarded, result.DamageAmount, transition }); };
                _claw.ClawFrameChanged += ObserveClaw;
                SetHud(); Stage("SETUP: (3300,4200), real Laborer/Sentry encounter; normal tuning");
                await Step(() =>
                {
                    _player.GlobalPosition = new Vector2(3300, 4200); // ONLY disclosed pose write.
                    _player.Velocity = Vector2.Zero;
                    var camera = _player.GetNode<Camera2D>("Camera2D");
                    camera.Enabled = true; camera.ResetSmoothing(); camera.ForceUpdateScroll();
                    _setup = true;
                    Record("only_initial_pose", new { position = Point(_player.GlobalPosition), velocity = Point(_player.Velocity) });
                });
                await Until(90, () => _level.GetNodeOrNull<PrologueSector>("WorldRoot/Sectors/CanyonBasin") != null, "actual Basin mounts");
                _sector = _level.GetNode<PrologueSector>("WorldRoot/Sectors/CanyonBasin");
                _floor = _sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamTrenchFloor");
                _pair = _sector.GetNode<CanyonSteamDiverter>("Gameplay/Mechanisms/SteamDiverterTrench");
                _left = _pair.GetNode<CanyonSteamJet>("LeftJet"); _right = _pair.GetNode<CanyonSteamJet>("RightJet");
                _control = _sector.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamDiverterTrenchControl");
                _adapter = _control.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
                _oldValve = _sector.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
                _oldJet = _sector.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
                ReadOldJetPassageGeometry();
                _group = Descendants(_sector).OfType<CanyonEncounterGroup>().Single(group => group.EncounterId.ToString() == "canyon.v2.encounter.steam-valves");
                Require(_pair.MechanismId == "canyon.v2.steam-diverter.trench" && !_pair.HasActivated && _adapter.ApplicationCount == 0,
                    "fresh exact installed Trench pair");
                await Until(30, () => SupportedBy(_floor), "actual initial trench support"); await Snapshot("before-combat");
                Require(!_oldValve.IsActivated && !_oldJet.DisabledForRun, "preserve upper-layer old valve and genuinely cycling old jet");
                await Until(120, () => Descendants(_sector).OfType<BellBackLaborer>().Any(), "real Laborer batch activates");
                _laborer = Descendants(_sector).OfType<BellBackLaborer>().Single();
                ulong laborerId = _laborer.GetInstanceId();
                _laborer.Defeated += () => { _laborerDefeats++; Record("actual_laborer_defeated", new { id = laborerId }); };
                _laborer.GetNode<HealthComponent>("Health").Died += () => { _laborerDeaths++; Record("actual_laborer_died", new { id = laborerId }); };
                _laborer.GetNode<HealthComponent>("Health").Damaged += (remaining, impulse) => { _laborerDamage++; Record("actual_laborer_damage", new { id = laborerId, remaining, impulse = Point(impulse) }); };
                _combatBound = true;
                await DefeatLaborer();
                Stage("Actual two-site Sentry arrival warning; accept either real chosen site");
                await Until(90, () => _group.IsArrivalWarningActive, "actual arrival warning after Laborer death");
                _arrivalSeen = true; await Snapshot("sentry-arrival-warning");
                await Until(120, () => Descendants(_sector).OfType<BellBoltSentry>().Any(), "actual arriving Sentry, not suppressed/fabricated");
                _sentry = Descendants(_sector).OfType<BellBoltSentry>().Single();
                Require(Math.Abs(_sentry.GlobalPosition.X - 3250) < 1f || Math.Abs(_sentry.GlobalPosition.X - 3750) < 1f,
                    "actual Sentry uses one of both unchanged authored sites");
                ulong sentryId = _sentry.GetInstanceId();
                Record("actual_sentry_site", new { id = sentryId, position = Point(_sentry.GlobalPosition) });
                _sentry.Defeated += () => { _sentryDefeats++; Record("actual_sentry_defeated", new { id = sentryId }); };
                _sentry.GetNode<HealthComponent>("Health").Died += () => { _sentryDeaths++; Record("actual_sentry_died", new { id = sentryId }); };
                _sentry.GetNode<HealthComponent>("Health").Damaged += (remaining, impulse) => { _sentryDamage++; Record("actual_sentry_damage", new { id = sentryId, remaining, impulse = Point(impulse) }); };
                await DefeatSentry();
                await Until(60, () => _group.IsComplete && _group.LiveActorCount == 0, "actual encounter complete from both real defeats");
                Require(_laborerDefeats == 1 && _laborerDeaths == 1 && _laborerDamage > 0 && _sentryDefeats == 1 && _sentryDeaths == 1 && _sentryDamage > 0 && _arrivalSeen && !_group.SpawnSuppressed,
                    "actual damage/death callbacks and real arrival precede pair control");
                Record("combat_complete", Sample()); await Snapshot("combat-complete");
                _routeHealth = _health.CurrentHealth; // Observe only; never write health.
                Stage("D: safe-window old-jet passage if needed, then supported pair approach");
                await WalkTo(3830, 300); await Snapshot("pair-before-control");
                await ApproachPairControl(1, 0);
                Stage("E/LMB LEFT steam: actual target/handoff, then D forward crossing");
                await SwitchPair(CanyonSteamDiverterSide.Left); await WalkTo(4170, 160); await Snapshot("forward-supported");
                await ObserveCycle(_left);
                Stage("Fresh E/LMB RIGHT steam: A reverse and safe return");
                await SwitchPair(CanyonSteamDiverterSide.Right); await WalkTo(3830, 160); await Snapshot("return-supported");
                await ObserveCycle(_right); Record("roundtrip_complete", Sample());
                await ApproachPairControl(3, 2);
                Stage("Third real LEFT switch: right stays dormant on actual stair approach");
                await SwitchPair(CanyonSteamDiverterSide.Left); await WalkTo(4170, 160); await Snapshot("rejoin-start");
                _climbing = true;
                for (int index = 0; index < 8; index++) await JumpToStep(index);
                await Until(60, () => RouteName() == "CounterweightLift" && GuidanceName() == "CounterweightLift", "actual physical rejoin reaches ordinary Lift and actual guidance");
                Require(SupportedBy(_sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamRejoinStep8")) ||
                    SupportedBy(_sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamRejoinTop")), "real unchanged final rejoin support");
                Require(_adapter.ApplicationCount == 3 && _health.CurrentHealth == _routeHealth && !_oldValve.IsActivated && !_oldJet.DisabledForRun &&
                    _oldJetPassages > 0 && _oldJetPassageActiveOverlaps == 0 && _oldJet.ElapsedSeconds > 0 && _heals == 0 && _recoveries == 0,
                    "three actual pair switches, supported roundtrip/rejoin and unchanged post-combat health");
                Require(_level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").LatestJsonPath == string.Empty && !Directory.Exists(paths.PlaytestsDirectory), "no fabricated hands-on recorder output");
                Record("trench_complete", Sample()); await Snapshot("lift-rejoined");
                GD.Print("TASK17B_MOVIE_TRENCH_OK generated=true physicalInput=true laborer=true sentry=true arrival=true actualDeaths=2 oldValvePreserved=true oldJetCycling=true safeWindowCrossings=true pairCasts=3 cross=true reverse=true return=true realSteps=8 lift=true guidance=true actorsUnmodified=true silentFixed60=true");
                for (int i = 0; i < 30; i++) await Step();
            }
            catch (Exception error) { Record("failed_retained", new { error = error.ToString(), observation = _setup ? Sample() : null }); throw; }
            finally
            {
                ReleaseInputs();
                if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= ObserveClaw;
                if (GodotObject.IsInstanceValid(_level) && _level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, _level);
                _metadata?.Dispose();
            }
        }

        private async Task DefeatLaborer()
        {
            Stage("Q/RMB: real Laborer warning and correct-facing sweep block");
            await SelectMode(PlayerActionMode.Attack); await ApproachEnemy(_laborer, 240);
            await Until(180, () => _laborer.CurrentState == BellBackLaborerState.Telegraph, "Laborer real Telegraph");
            await Snapshot("laborer-telegraph");
            int beforeBlocks = _blocks;
            await Step(() => { Aim(_laborer.GlobalPosition); Button(MouseButton.Right, true); });
            await Until(12, () => _shield.IsActive, "actual RMB shield raises");
            await Until(150, () => _blocks > beforeBlocks, "actual sweep shield resolution"); await Snapshot("laborer-guard");
            await Step(() => Button(MouseButton.Right, false)); await Until(12, () => !_shield.IsActive, "RMB release before claw");
            for (int round = 0; round < 4 && _laborerDefeats == 0; round++)
            {
                await ApproachEnemy(_laborer, 180);
                Stage("E/LMB real Laborer armor opening, then Q/LMB real melee round " + (round + 1));
                await CastTarget(_laborer.GetNode<ThreadTarget2D>("ThreadTarget"));
                await Until(20, () => _laborer.CurrentState == BellBackLaborerState.Exposed, "real accepted thread enters Exposed");
                if (round == 0) await Snapshot("laborer-exposed");
                await SelectMode(PlayerActionMode.Attack);
                for (int hit = 0; hit < 4 && _laborerDefeats == 0 && _laborer.CurrentState == BellBackLaborerState.Exposed; hit++)
                {
                    await Until(60, () => _laborerDefeats > 0 || _laborer.CurrentState != BellBackLaborerState.Exposed ||
                        (_modes.CanBeginAttack && !_combat.IsAttacking), "actual Laborer attack readiness/exposure arbitration");
                    if (_laborerDefeats > 0 || _laborer.CurrentState != BellBackLaborerState.Exposed) break;
                    await AttackToward(_laborer);
                }
            }
            Require(_laborerDefeats == 1 && _laborerDeaths == 1, "bounded real Laborer melee defeats armored actor");
        }

        private async Task DefeatSentry()
        {
            Stage("Q/RMB: approach whichever actual Sentry site and intercept its bolt");
            await SelectMode(PlayerActionMode.Attack); await ApproachEnemy(_sentry, 300);
            await Until(180, () => _sentry.CurrentFrame.State == BellBoltSentryState.Telegraph, "real Sentry warning");
            await Snapshot("sentry-telegraph");
            int beforeBlocks = _blocks;
            await Step(() => { Aim(_sentry.GlobalPosition); Button(MouseButton.Right, true); });
            await Until(12, () => _shield.IsActive, "actual Sentry-facing shield raises");
            await Until(180, () => _blocks > beforeBlocks, "actual front bolt shield resolution"); await Snapshot("sentry-guard");
            await Step(() => Button(MouseButton.Right, false)); await Until(12, () => !_shield.IsActive, "RMB released before thread/melee");
            await SelectMode(PlayerActionMode.Grapple);
            await Until(180, () => _sentry.CurrentFrame.State == BellBoltSentryState.Telegraph, "fresh actual warning for thread interruption");
            await CastTarget(_sentry.GetNode<ThreadTarget2D>("ThreadTarget"));
            await Until(20, () => _sentry.CurrentFrame.State == BellBoltSentryState.Stunned, "actual thread interrupts Sentry into Stunned");
            _sentryInterrupted = true; await Snapshot("sentry-thread-stunned");
            await SelectMode(PlayerActionMode.Attack);
            for (int hit = 0; hit < 4 && _sentryDefeats == 0; hit++)
            {
                await Until(90, () => _modes.CanBeginAttack && !_combat.IsAttacking, "actual Q/LMB attack arbitration");
                await AttackToward(_sentry);
            }
            Require(_sentryDefeats == 1 && _sentryDeaths == 1 && _sentryInterrupted, "bounded real Sentry melee defeat after actual thread interruption");
        }

        private async Task ApproachEnemy(Node2D actor, int cap)
        {
            await CrossOldJetIfBetween(actor.GlobalPosition.X);
            for (int i = 0; i < cap; i++)
            {
                Require(GodotObject.IsInstanceValid(actor) && actor.IsInsideTree(), "actual combat actor cannot disappear without real defeat");
                float dx = actor.GlobalPosition.X - _player.GlobalPosition.X;
                if (Math.Abs(dx) >= 42 && Math.Abs(dx) <= 68 && Math.Abs(_player.Velocity.X) <= .5f) break;
                int axis = Math.Abs(dx) > 68 ? Math.Sign(dx) : Math.Abs(dx) < 42 ? -Math.Sign(dx) : 0;
                await Step(() => { Move(axis); Aim(actor.GlobalPosition); });
            }
            await Step(() => Move(0));
            Require(Math.Abs(actor.GlobalPosition.X - _player.GlobalPosition.X) >= 35 && Math.Abs(actor.GlobalPosition.X - _player.GlobalPosition.X) <= 70 && SupportedBy(_floor),
                "observed real melee separation/support, not an authored teleport");
        }
        private async Task SelectMode(PlayerActionMode mode)
        {
            Key key = mode == PlayerActionMode.Attack ? Key.Q : Key.E;
            await Step(() => { Button(MouseButton.Left, false); Button(MouseButton.Right, false); KeyEvent(key, true); });
            await Step(() => KeyEvent(key, false));
            await Until(12, () => _modes.CurrentMode == mode && !_shield.IsActive, "literal mode input and released shield");
        }
        private async Task AttackToward(Node2D actor)
        {
            Require(_modes.CanBeginAttack && !_combat.IsAttacking && !_shield.IsActive, "actual attack permission before LMB");
            int before = _attacksStarted;
            await Step(() => { Move(Math.Sign(actor.GlobalPosition.X - _player.GlobalPosition.X)); Aim(actor.GlobalPosition); Button(MouseButton.Left, true); });
            await Step(() => { Button(MouseButton.Left, false); Move(0); });
            Require(_attacksStarted == before + 1, "one literal LMB starts one real permitted attack");
            await Until(60, () => !_combat.IsAttacking, "actual attack timeline completes");
        }
        private async Task CastTarget(ThreadTarget2D target)
        {
            await SelectMode(PlayerActionMode.Grapple);
            await Until(30, () => _claw.CurrentFrame.State is MechanicalClawState.Idle or MechanicalClawState.Released, "fresh actual claw");
            var adapter = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            int accepted = adapter.ApplicationCount, latches = _latches, failures = _castFailures;
            ulong targetId = target.GetInstanceId();
            await Step(() => { Aim(target.GlobalPosition); Button(MouseButton.Left, true); });
            await Until(90, () => _latches > latches || _castFailures > failures,
                "real target latch callback; no accepted latch observed");
            Require(_latches > latches, "real cast rejected: target=" + target.GetPath() + " failure=" + _lastCastFailure);
            Require(_lastLatchTarget == targetId && adapter.ApplicationCount == accepted + 1, "exact real target accepted, not nearby terrain/actor");
            await Step(() => Button(MouseButton.Left, false));
        }
        private async Task SwitchPair(CanyonSteamDiverterSide wanted)
        {
            int before = _adapter.ApplicationCount;
            await CastTarget(_control);
            Require(_pair.HasActivated && _adapter.ApplicationCount == before + 1 && _pair.SelectedSide == wanted && _pair.IsHandingOff, "actual pair accepted into desired handoff");
            await Snapshot("handoff-" + wanted + "-" + _adapter.ApplicationCount);
            for (int i = 0; i < 45 && _pair.IsHandingOff; i++)
            {
                Require(_left.Collision.Disabled && _right.Collision.Disabled && !_left.Frame.HazardActive && !_right.Frame.HazardActive, "real two-sided harmless handoff");
                await Step();
            }
            Require(!_pair.IsHandingOff, "real .60s handoff completes without clock API");
        }
        private async Task ApproachPairControl(int ordinal, int acceptedBefore)
        {
            Stage("D: ordinary central approach before real pair cast " + ordinal);
            await WalkTo(4000, 120);
            Require(_pair.SelectedSide == CanyonSteamDiverterSide.Right && !_pair.IsHandingOff &&
                _adapter.ApplicationCount == acceptedBefore && _left.DisabledForRun &&
                !_left.Frame.Telegraphing && !_left.Frame.HazardActive && _left.Collision.Disabled && !_left.Monitoring,
                "current RIGHT leaves LEFT genuinely dormant before central cast");
            Require(_player.GlobalPosition.X > 3942 && _player.GlobalPosition.X < 4058 &&
                Math.Abs(_player.Velocity.X) <= .5f && Math.Abs(_player.Velocity.Y) <= .5f && SupportedBy(_floor),
                "observed stopped central safe pocket on actual trench floor");
            var hurtShape = _player.GetNode<CollisionShape2D>("Hurtbox/HurtboxCollider");
            var capsule = (CapsuleShape2D)hurtShape.Shape; // Upright centered capsule already checked for old-jet passage.
            Vector2 half = new(capsule.Radius, capsule.Height / 2);
            Vector2 a = hurtShape.ToGlobal(-half), b = hurtShape.ToGlobal(half);
            var capsuleBounds = new Rect2(a, b - a).Abs();
            Rect2[] footprints = new[] { _left, _right }.Select(jet =>
            {
                Require(jet.Collision.Shape is RectangleShape2D &&
                    Math.Abs(jet.Collision.GlobalTransform.X.Y) < .001f && Math.Abs(jet.Collision.GlobalTransform.Y.X) < .001f,
                    "actual pair footprints are upright rectangles");
                var rectangle = (RectangleShape2D)jet.Collision.Shape;
                Vector2 start = jet.Collision.ToGlobal(-rectangle.Size / 2), end = jet.Collision.ToGlobal(rectangle.Size / 2);
                return new Rect2(start, end - start).Abs();
            }).ToArray();
            Require(footprints.All(bounds => !capsuleBounds.Intersects(bounds)), "actual player capsule clear of both pair footprints");
            Record("pair_central_approach", new { ordinal, acceptedBefore = _adapter.ApplicationCount,
                capsule = new { radius = capsule.Radius, height = capsule.Height, min = Point(capsuleBounds.Position), max = Point(capsuleBounds.End) },
                footprints = footprints.Select(bounds => new { min = Point(bounds.Position), max = Point(bounds.End) }).ToArray(),
                observation = Sample() });
        }
        private async Task WalkTo(float x, int cap, bool oldJetPassage = false)
        {
            if (!oldJetPassage) await CrossOldJetIfBetween(x);
            int axis = Math.Sign(x - _player.GlobalPosition.X);
            await Step(() => Move(axis));
            await Until(cap, () => axis > 0 ? _player.GlobalPosition.X >= x : _player.GlobalPosition.X <= x, "physical walking reaches candidate X=" + x);
            await Step(() => Move(0));
            await Until(20, () => Math.Abs(_player.Velocity.X) <= .5f && SupportedBy(_floor), "observed stopped motor on actual trench floor");
            Require(_player.GlobalPosition.X >= 3040 && _player.GlobalPosition.X < 4200, "actual trench-floor supported endpoint");
        }
        private void ReadOldJetPassageGeometry()
        {
            Require(_oldJet.Collision.Shape is RectangleShape2D &&
                Math.Abs(_oldJet.Collision.GlobalTransform.X.Y) < .001f && Math.Abs(_oldJet.Collision.GlobalTransform.Y.X) < .001f,
                "old jet passage borrows actual axis-aligned rectangle, not authored center clearance");
            var rectangle = (RectangleShape2D)_oldJet.Collision.Shape;
            Vector2 a = _oldJet.Collision.ToGlobal(-rectangle.Size / 2), b = _oldJet.Collision.ToGlobal(rectangle.Size / 2);
            _oldJetBounds = new Rect2(a, b - a).Abs();
            var hurtShape = _player.GetNode<CollisionShape2D>("Hurtbox/HurtboxCollider");
            Require(hurtShape.Shape is CapsuleShape2D && hurtShape.GlobalPosition.IsEqualApprox(_player.GlobalPosition) &&
                Math.Abs(hurtShape.GlobalTransform.X.Y) < .001f && Math.Abs(hurtShape.GlobalTransform.Y.X) < .001f,
                "actual centered upright player hurtbox capsule required for concrete old-jet passage");
            float radius = ((CapsuleShape2D)hurtShape.Shape).Radius * Math.Abs(hurtShape.GlobalScale.X);
            _oldJetLeftClear = _oldJetBounds.Position.X - radius;
            _oldJetRightClear = _oldJetBounds.End.X + radius;
            float speed = _motor.Tuning.MaxRunSpeed, tick = 1f / Engine.PhysicsTicksPerSecond;
            // Continuous stop distance plus four physical ticks of input/observation reserve.
            // Actual stop/phase assertions below, never Step-count assumptions, decide passage.
            _oldJetStageMargin = Mathf.Ceil(speed * speed / (2 * _motor.Tuning.GroundDeceleration) + 4 * speed * tick + 1);
            Record("old_jet_geometry", new { bounds = new { x = _oldJetBounds.Position.X, y = _oldJetBounds.Position.Y,
                width = _oldJetBounds.Size.X, height = _oldJetBounds.Size.Y }, radius, leftClear = _oldJetLeftClear,
                rightClear = _oldJetRightClear, stagingMargin = _oldJetStageMargin });
        }
        private async Task CrossOldJetIfBetween(float destinationX)
        {
            float fromX = _player.GlobalPosition.X;
            int axis = fromX < _oldJetLeftClear && destinationX > _oldJetRightClear ? 1 :
                fromX > _oldJetRightClear && destinationX < _oldJetLeftClear ? -1 : 0;
            if (axis == 0) return;
            float near = axis > 0 ? _oldJetLeftClear - _oldJetStageMargin : _oldJetRightClear + _oldJetStageMargin;
            float far = axis > 0 ? _oldJetRightClear + _oldJetStageMargin : _oldJetLeftClear - _oldJetStageMargin;
            Stage("A/D: stop outside real old-jet capsule footprint; await sufficient actual safe window");
            if (axis > 0 ? fromX < near : fromX > near) await WalkTo(near, 120, oldJetPassage: true);
            await Step(() => Move(0));
            await Until(20, () => Math.Abs(_player.Velocity.X) <= .5f && SupportedBy(_floor), "old-jet wait starts actually stopped on trench floor");
            Require(axis > 0 ? _player.GlobalPosition.X < _oldJetLeftClear : _player.GlobalPosition.X > _oldJetRightClear,
                "old-jet staging cannot wait inside expanded actual hazard");
            Require(!_shield.IsActive && !_combat.IsAttacking && !_motor.IsDashing && _motor.EffectiveMovementScale > 0,
                "old-jet passage uses current ordinary running permission");
            float speed = _motor.Tuning.MaxRunSpeed * _motor.EffectiveMovementScale;
            float tick = 1f / Engine.PhysicsTicksPerSecond;
            float required = Math.Abs(far - _player.GlobalPosition.X) / speed + speed / _motor.Tuning.GroundAcceleration +
                speed / _motor.Tuning.GroundDeceleration + 4 * tick;
            float harmless = _oldJet.SafeSeconds + _oldJet.TelegraphSeconds;
            float cycle = harmless + _oldJet.ActiveSeconds;
            Require(required < harmless, "actual old-jet window must accommodate derived travel/ramp/brake/reserve time");
            ulong waiting = Engine.GetPhysicsFrames();
            float Remaining() => harmless - Mathf.PosMod(_oldJet.ElapsedSeconds, cycle);
            bool Ready() => !_oldJet.Frame.HazardActive && _oldJet.Collision.Disabled && !_oldJet.Monitoring && Remaining() >= required;
            while (!Ready())
            {
                await Step();
                Require(Engine.GetPhysicsFrames() - waiting <= Mathf.Ceil(cycle * Engine.PhysicsTicksPerSecond) + 4,
                    "one bounded real old-jet cycle did not provide sufficient passage window");
                Require(Math.Abs(_player.Velocity.X) <= .5f && SupportedBy(_floor) &&
                    (axis > 0 ? _player.GlobalPosition.X < _oldJetLeftClear : _player.GlobalPosition.X > _oldJetRightClear),
                    "passive safe-window wait remains stopped outside actual jet");
            }
            float startX = _player.GlobalPosition.X, elapsed = _oldJet.ElapsedSeconds, remaining = Remaining();
            ulong physics = Engine.GetPhysicsFrames(); int health = _health.CurrentHealth;
            Record("old_jet_passage_start", new { axis, startX, far, elapsed, remaining, required, physics, observation = Sample() });
            _oldJetPassage = true;
            try { await WalkTo(far, 120, oldJetPassage: true); }
            finally { _oldJetPassage = false; }
            Require((axis > 0 ? _player.GlobalPosition.X > _oldJetRightClear : _player.GlobalPosition.X < _oldJetLeftClear) &&
                _oldJet.ElapsedSeconds > elapsed && _oldJet.ElapsedSeconds - elapsed < remaining &&
                !_oldJet.Frame.HazardActive && _oldJet.Collision.Disabled && !_oldJet.Monitoring,
                "actual stopped opposite endpoint reached within same uninterrupted harmless old-jet window");
            _oldJetPassages++;
            Record("old_jet_passage_complete", new { passage = _oldJetPassages, axis, startX, endX = _player.GlobalPosition.X,
                elapsedStart = elapsed, elapsedEnd = _oldJet.ElapsedSeconds, remaining, required, physicsStart = physics,
                physicsEnd = Engine.GetPhysicsFrames(), healthBefore = health, healthAfter = _health.CurrentHealth, observation = Sample() });
            if (_oldJetPassages == 1) await Snapshot("old-jet-safe-window-passage");
        }
        private async Task ObserveCycle(CanyonSteamJet jet)
        {
            Stage("Actual " + _pair.SelectedSide + " warning/active; endpoint remains supported");
            await Until(150, () => jet.Frame.Telegraphing, "actual selected warning");
            Require(jet.Collision.Disabled, "warning collision harmless"); await Snapshot("warning-" + _pair.SelectedSide);
            await Until(60, () => jet.Frame.HazardActive, "actual selected active");
            Require(!jet.Collision.Disabled && jet.Monitoring && SupportedBy(_floor), "actual active collision and supported observation"); await Snapshot("active-" + _pair.SelectedSide);
        }
        private async Task JumpToStep(int index)
        {
            Stage("D/Space: real unchanged SteamRejoinStep" + (index + 1));
            StaticBody2D step = _sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamRejoinStep" + (index + 1));
            StaticBody2D top = _sector.GetNode<StaticBody2D>("Gameplay/Collision/SteamRejoinTop");
            float targetX = CanyonWorldLayout.SteamRejoinSteps[index].GetCenter().X;
            int jumps = _motor.AcceptedJumpCount;
            Require(_player.IsOnFloor(), "each physical stair jump starts grounded");
            await Step(() => { Move(1); KeyEvent(Key.Space, true); });
            await Until(8, () => _motor.AcceptedJumpCount == jumps + 1, "literal Space accepted as real motor jump");
            for (int i = 0; i < 100 && !(SupportedBy(step) || (index == 7 && SupportedBy(top))); i++)
                await Step(() => { Move(_player.GlobalPosition.X < targetX ? 1 : 0); if (_player.Velocity.Y >= 0) KeyEvent(Key.Space, false); });
            await Step(() => { Move(0); KeyEvent(Key.Space, false); });
            await Until(20, () => Math.Abs(_player.Velocity.X) <= .5f && (SupportedBy(step) || (index == 7 && SupportedBy(top))), "actual named stair landing and stop");
            Require(_motor.AcceptedJumpCount == jumps + 1, "one real jump per unchanged support, no pose/velocity shortcut");
            Record("actual_rejoin_step", new { step = index + 1, observation = Sample() });
            if (index == 0 || index == 3) await Snapshot("rejoin-step-" + (index + 1));
        }

        private async Task Step(Action? input = null)
        {
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame); // Input BEFORE node processing, never post-draw.
            input?.Invoke(); Input.FlushBufferedEvents();
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            Require(Engine.GetPhysicsFrames() - _startedPhysics <= MaximumPhysicsFrames, "90-second actual physics budget exhausted");
            if (!_setup) return;
            Record("sample", Sample());
            if (GodotObject.IsInstanceValid(_oldJet))
            {
                Require(!_oldValve.IsActivated && !_oldJet.DisabledForRun, "old valve stays unactivated and jet never retires");
                bool overlap = _oldJet.Frame.HazardActive && _oldJet.GetOverlappingAreas().Contains(_player.GetNode<Hurtbox>("Hurtbox"));
                if (overlap && !_oldJetOverlapWasActive)
                {
                    _oldJetActiveOverlaps++; if (_oldJetPassage) _oldJetPassageActiveOverlaps++;
                    Record("actual_old_jet_active_overlap", new { duringPassage = _oldJetPassage, observation = Sample() });
                }
                _oldJetOverlapWasActive = overlap;
                if (_oldJetPassage) Require(!overlap && !_oldJet.Frame.HazardActive && _oldJet.Collision.Disabled && !_oldJet.Monitoring,
                    "real old-jet passage must remain in observed non-damaging phase");
            }
            Require(_health.CurrentHealth > 0 && _recoveries == 0 && _heals == 0 && _player.GlobalPosition.Y < 4305, "actual death/heal/recovery/fall is failed retained proof");
            Require(!_routeHealth.HasValue || _health.CurrentHealth == _routeHealth.Value, "post-combat roundtrip/rejoin health remains unchanged");
            if (_combatBound) Require(_group.IsComplete || _group.IsActive, "encounter cannot retire instead of real defeats");
            if (GodotObject.IsInstanceValid(_group) && _group.IsArrivalWarningActive) _arrivalSeen = true;
            if (_climbing) Require(RouteName() is "SteamValves" or "CounterweightLift", "actual jump trajectory remains in approved Steam/Lift union");
        }
        private async Task Until(int cap, Func<bool> condition, string requirement)
        { for (int i = 0; i < cap && !condition(); i++) await Step(); Require(condition(), requirement); }
        private bool SupportedBy(StaticBody2D body)
        {
            if (!_player.IsOnFloor()) return false;
            for (int i = 0; i < _player.GetSlideCollisionCount(); i++)
            { var collision = _player.GetSlideCollision(i); if (collision.GetCollider() == body && collision.GetNormal().Y < -.7f) return true; }
            return false;
        }
        private string? RouteName() => CanyonWorldLayout.TryResolveRouteNode(_player.GlobalPosition, out var route) ? route.ToString() : null;
        private string? GuidanceName() => typeof(CanyonFireflyGuidanceController).GetField("_lastRouteNode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_level.GetNode<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance"))?.ToString();
        private void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.State == MechanicalClawState.Latched) { _latches++; _lastLatchTarget = frame.LockedTargetRuntimeId; }
            if (frame.State == MechanicalClawState.Failed) { _castFailures++; _lastCastFailure = frame.Failure; }
            Record("actual_claw", new { frame.CastId, state = frame.State.ToString(), failure = frame.Failure.ToString(), targetId = frame.LockedTargetRuntimeId,
                targetName = GodotObject.IsInstanceValid(target) ? target.Name.ToString() : null });
        }
        private object Sample()
        {
            string? route = RouteName();
            if (route != _lastRoute) { Record("actual_route_transition", new { previous = _lastRoute, route, position = Point(_player.GlobalPosition), guidance = GuidanceName() }); _lastRoute = route; }
            return new { physics = Engine.GetPhysicsFrames(), stage = _stage, world = Point(_player.GlobalPosition), canvas = Point(_player.GetGlobalTransformWithCanvas().Origin),
                velocity = Point(_player.Velocity), health = _health.CurrentHealth, damage = _damage, heals = _heals, recoveries = _recoveries,
                floor = _player.IsOnFloor(), contacts = Enumerable.Range(0, _player.GetSlideCollisionCount()).Select(i => (_player.GetSlideCollision(i).GetCollider() as Node)?.Name.ToString() ?? "non-node").ToArray(),
                enemies = Descendants(_level).Where(node => node is Node2D && node != _player && node.GetNodeOrNull<HealthComponent>("Health") != null)
                    .Select(node => new { id = node.GetInstanceId(), name = node.Name.ToString(), type = node.GetType().Name, position = Point(((Node2D)node).GlobalPosition),
                        health = node.GetNode<HealthComponent>("Health").CurrentHealth, state = node.GetType().GetProperty("CurrentState")?.GetValue(node)?.ToString() ?? node.GetType().GetProperty("CurrentFrame")?.GetValue(node)?.ToString() }).ToArray(),
                encounters = Descendants(_level).OfType<CanyonEncounterGroup>().Select(group => new { id = group.EncounterId.ToString(), active = group.IsActive, complete = group.IsComplete, live = group.LiveActorCount, warning = group.IsArrivalWarningActive, suppressed = group.SpawnSuppressed }).ToArray(),
                combat = new { attacks = _attacksStarted, connected = _attacksConnected, blocks = _blocks, laborerDefeats = _laborerDefeats, laborerDeaths = _laborerDeaths, laborerDamage = _laborerDamage,
                    sentryDefeats = _sentryDefeats, sentryDeaths = _sentryDeaths, sentryDamage = _sentryDamage, arrival = _arrivalSeen, interrupted = _sentryInterrupted },
                claw = _claw.CurrentFrame.State.ToString(), target = _claw.CurrentFrame.LockedTargetRuntimeId, latches = _latches,
                mode = _modes.CurrentMode.ToString(), canAttack = _modes.CanBeginAttack, attacking = _combat.IsAttacking, shield = _shield.IsActive,
                pair = GodotObject.IsInstanceValid(_pair) ? new { accepted = _adapter.ApplicationCount, side = _pair.SelectedSide.ToString(), handoff = _pair.IsHandingOff, activated = _pair.HasActivated, left = Jet(_left), right = Jet(_right) } : null,
                oldValve = GodotObject.IsInstanceValid(_oldValve) ? new { activated = _oldValve.IsActivated, jet = Jet(_oldJet) } : null,
                oldJetPassage = new { completed = _oldJetPassages, activeOverlaps = _oldJetActiveOverlaps,
                    passageActiveOverlaps = _oldJetPassageActiveOverlaps, crossing = _oldJetPassage },
                route, guidance = GuidanceName(), jumps = _motor.AcceptedJumpCount, heldKeys = _keys.Where(entry => entry.Value).Select(entry => entry.Key.ToString()).ToArray(),
                heldButtons = _buttons.Where(entry => entry.Value).Select(entry => entry.Key.ToString()).ToArray() };
        }
        private static object Jet(CanyonSteamJet jet) => new { elapsed = jet.ElapsedSeconds, warning = jet.Frame.Telegraphing, active = jet.Frame.HazardActive,
            disabled = jet.Collision.Disabled, monitoring = jet.Monitoring, retired = jet.DisabledForRun };
        private static IEnumerable<Node> Descendants(Node root)
        { for (int i = 0; i < root.GetChildCount(); i++) { Node child = root.GetChild(i); yield return child; foreach (Node nested in Descendants(child)) yield return nested; } }
        private static object Point(Vector2 point) => new { x = point.X, y = point.Y };
        private void Record(string kind, object? value) => _metadata?.WriteLine(JsonSerializer.Serialize(new { kind, stage = _stage, value }));
        private void Stage(string stage) { _stage = stage; if (_stageLabel != null) _stageLabel.Text = stage; GD.Print("TASK17B_TRENCH_PHASE " + stage); Record("phase", stage); }
        private void Move(int axis) { KeyEvent(Key.A, axis < 0); KeyEvent(Key.D, axis > 0); }
        private void KeyEvent(Key key, bool pressed)
        { if (_keys.TryGetValue(key, out bool held) && held == pressed) return; _keys[key] = pressed; using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed, Echo = false }; Input.ParseInputEvent(input); }
        private void Aim(Vector2 world)
        { _pointer = _player.GetViewport().GetFinalTransform() * (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(world)); using var motion = new InputEventMouseMotion { Position = _pointer, GlobalPosition = _pointer }; _player.GetViewport().PushInput(motion, true); Input.ParseInputEvent(motion); Input.FlushBufferedEvents(); }
        private void Button(MouseButton button, bool pressed)
        { if (_buttons.TryGetValue(button, out bool held) && held == pressed) return; _buttons[button] = pressed; using var input = new InputEventMouseButton { ButtonIndex = button, Pressed = pressed, Position = _pointer, GlobalPosition = _pointer }; Input.ParseInputEvent(input); }
        private void ReleaseInputs()
        {
            foreach (Key key in _keys.Keys.ToArray()) { try { KeyEvent(key, false); } catch (Exception error) { GD.PushError("Task17B Trench key release: " + error); } }
            foreach (MouseButton button in _buttons.Keys.ToArray()) { try { Button(button, false); } catch (Exception error) { GD.PushError("Task17B Trench mouse release: " + error); } }
            Input.FlushBufferedEvents();
        }
        private async Task Snapshot(string label)
        {
            await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image image = _tree.Root.GetTexture().GetImage(); Require(image.GetWidth() == 1280 && image.GetHeight() == 720, "native full-size capture");
            string path = Path.Combine(_output, "frames", $"{++_snapshot:D2}-{label}.png");
            Require(!File.Exists(path) && image.SavePng(path) == Error.Ok, "fresh external native PNG"); Record("native_snapshot", new { path, observation = Sample() });
        }
        private void SetHud()
        {
            _tree.Root.ContentScaleSize = new Vector2I(1280, 720); _tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            _tree.Root.Size = new Vector2I(1280, 720); DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            var hud = new CanvasLayer { Name = "Task17BTrenchDisclosure", Layer = 50 };
            hud.AddChild(new Label { Text = "REGIONAL · ONE INITIAL POSE · REAL COMBAT/ACTORS · SCRIPTED INPUT · SILENT FIXED60 · NOT HUMAN", Position = new Vector2(12, 666), MouseFilter = Control.MouseFilterEnum.Ignore });
            _stageLabel = new Label { Position = new Vector2(12, 690), MouseFilter = Control.MouseFilterEnum.Ignore }; hud.AddChild(_stageLabel); _level.AddChild(hud);
        }
        private void ValidateCapture()
        {
            for (DirectoryInfo? directory = new(_output); directory != null; directory = directory.Parent) Require(!directory.Exists || (directory.Attributes & FileAttributes.ReparsePoint) == 0, "capture rejects reparse ancestors");
            Require(Directory.Exists(_output) && !File.Exists(Path.Combine(_output, "metadata.jsonl")), "fresh exact launcher proof directory");
            Require(DisplayServer.GetName() != "headless" && ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString() == "gl_compatibility", "normal configured OpenGL Compatibility");
            string adapter = RenderingServer.GetVideoAdapterName();
            Require(!string.IsNullOrWhiteSpace(adapter) && !new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render" }.Any(name => adapter.Contains(name, StringComparison.OrdinalIgnoreCase)), "software renderer rejected");
            Require(AudioServer.GetDriverName() == "Dummy" && Path.GetFullPath(Engine.GetWriteMoviePath()).Equals(Path.Combine(_output, "raw.avi"), StringComparison.OrdinalIgnoreCase), "exact isolated raw AVI/Dummy capture");
        }
        private static void Require(bool condition, string requirement)
        { if (!condition) throw new InvalidOperationException("Task17B Trench input proof: " + requirement); }
    }

    private sealed class FrozenProbe
    {
        private const int MaximumPhysicsFrames = 2400;
        private readonly SceneTree _tree;
        private readonly string _output;
        private readonly Dictionary<Key, bool> _keys = new();
        private PrologueLevelController _level = null!;
        private CharacterBody2D _player = null!;
        private HealthComponent _health = null!;
        private MechanicalClawController _claw = null!;
        private PlayerActionModeController _modes = null!;
        private CanyonSteamDiverter _pair = null!;
        private CanyonSteamJet _left = null!, _right = null!;
        private ThreadTarget2D _control = null!;
        private LegacyThreadInteractionAdapter _adapter = null!;
        private StaticBody2D _incoming = null!, _floor = null!;
        private StreamWriter? _metadata;
        private Label? _stageLabel;
        private bool _mouseHeld;
        private Vector2 _pointer;
        private bool _setup;
        private int _recoveries;
        private int _heals;
        private int _latches;
        private ulong _lastLatchTarget;
        private ulong _startedPhysics;
        private string _stage = "startup";
        private int _snapshot;
        private int? _requiredHealth;

        public FrozenProbe(SceneTree tree, string storage)
        {
            _tree = tree;
            _output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(storage))!, "proof");
        }

        public async Task Run()
        {
            ValidateCapture(); // Runner has already validated storage and PID release.
            Directory.CreateDirectory(Path.Combine(_output, "frames"));
            _metadata = new StreamWriter(new FileStream(Path.Combine(_output, "metadata.jsonl"),
                FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            Record("invocation", new { route = "frozen", adapter = RenderingServer.GetVideoAdapterName(),
                audio = AudioServer.GetDriverName(), movie = Engine.GetWriteMoviePath(),
                limits = "generated-regional;one-initial-pose;physical-input;silent-fixed60;not-human-realtime;Trench-combat-pending" });
            try
            {
                _startedPhysics = Engine.GetPhysicsFrames();
                _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
                var flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
                var paths = IsolatedPrologueFixture.Configure(_level, "task17b-frozen-input-movie");
                Record("isolated_before_mount", paths);
                _level.RecoveryRequested += _ => _recoveries++;
                _tree.Root.AddChild(_level);
                for (int i = 0; i < 4; i++) await Step();
                Require(flow.State == PrologueGameFlowState.Title && !flow.HasSave, "isolated empty Title with both autostarts disabled");
                flow.StartNew();
                Require(flow.State == PrologueGameFlowState.Playing, "actual StartNew enters Playing");
                _player = _level.Player;
                _health = _player.GetNode<HealthComponent>("Health");
                _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
                _modes = _player.GetNode<PlayerActionModeController>("ActionModes");
                _health.Damaged += (remaining, impulse) => Record("actual_damage", new { remaining, impulse = Point(impulse) });
                _health.Healed += (restored, remaining) => { _heals++; Record("actual_heal", new { restored, remaining }); };
                _claw.ClawFrameChanged += ObserveClaw;
                SetHud();
                Stage("SETUP: prior FrozenLeftCatch (1490,2144); actors/tuning remain real");
                await Step(() =>
                {
                    _player.GlobalPosition = new Vector2(1490, 2144); // ONLY disclosed pose write.
                    _player.Velocity = Vector2.Zero; // Same initial setup, never route correction.
                    var camera = _player.GetNode<Camera2D>("Camera2D");
                    camera.Enabled = true; camera.ResetSmoothing(); camera.ForceUpdateScroll();
                    _setup = true;
                    Record("only_initial_pose", new { position = Point(_player.GlobalPosition), velocity = Point(_player.Velocity) });
                });
                await Until(90, () => _level.GetNodeOrNull<PrologueSector>("WorldRoot/Sectors/CanyonDescent") != null, "actual Descent residency mounts");
                var sector = _level.GetNode<PrologueSector>("WorldRoot/Sectors/CanyonDescent");
                _incoming = sector.GetNode<StaticBody2D>("Gameplay/Collision/FrozenLeftCatch");
                _floor = sector.GetNode<StaticBody2D>("Gameplay/Collision/FrozenRightCatch");
                _pair = sector.GetNode<CanyonSteamDiverter>("Gameplay/Mechanisms/SteamDiverterFrozen");
                _left = _pair.GetNode<CanyonSteamJet>("LeftJet"); _right = _pair.GetNode<CanyonSteamJet>("RightJet");
                _control = sector.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamDiverterFrozenControl");
                _adapter = _control.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
                Require(_pair.MechanismId == "canyon.v2.steam-diverter.frozen" && !_pair.HasActivated && _pair.SelectedSide == CanyonSteamDiverterSide.Right, "real default installed frozen pair");
                await Until(30, () => SupportedBy(_incoming), "initial pose settles on actual prior shelf");
                _requiredHealth = _health.CurrentHealth;
                Record("prior_shelf_health", Sample());
                await Snapshot("incoming-floor");

                Stage("D: leave prior shelf and land on actual FrozenRightCatch; no teleport");
                await Step(() => KeyEvent(Key.D, true));
                await Until(90, () => _player.GlobalPosition.X >= 1605, "actual D reaches candidate incoming landing X");
                await Step(() => KeyEvent(Key.D, false));
                await Until(120, () => SupportedBy(_floor), "actual gravity lands on right catch");
                await Until(20, () => Math.Abs(_player.Velocity.X) <= .5f && SupportedBy(_floor), "observed neutral motor stops on actual right catch");
                Require(_player.GlobalPosition.X < _left.GlobalPosition.X && _left.DisabledForRun &&
                    _left.Collision.Disabled && !_left.Monitoring && !_left.Frame.HazardActive,
                    "actual supported incoming landing remains a left-side approach for meaningful forward traversal; default left jet is harmless");
                int crossingHealth = _health.CurrentHealth;
                Record("incoming_landed", Sample()); await Snapshot("incoming-landing");

                Stage("E/LMB: real control latch, both jets harmless during .60s handoff");
                await Cast(CanyonSteamDiverterSide.Left);
                Stage("D: cross left→right after handoff; selected arrow denotes STEAM FLOW");
                await Walk(Key.D, () => _player.GlobalPosition.X >= 1875, 120, crossingHealth);
                Require(_player.GlobalPosition.X <= 1898 && SupportedBy(_floor), "actual forward endpoint supported outside right steam footprint");
                await Snapshot("forward-supported");
                Stage("Watch actual left warning and active column; right endpoint stays harmless");
                await ObserveCycle(_left, crossingHealth);
                Stage("Fresh E/LMB: reverse to RIGHT steam; A: actual safe return");
                await Cast(CanyonSteamDiverterSide.Right);
                await Walk(Key.A, () => _player.GlobalPosition.X <= 1605, 120, crossingHealth);
                Require(_player.GlobalPosition.X >= 1582 && SupportedBy(_floor), "actual return endpoint supported outside left steam footprint");
                await Snapshot("return-supported");
                Stage("Watch actual right warning and active column; left endpoint stays harmless");
                await ObserveCycle(_right, crossingHealth);
                Require(_adapter.ApplicationCount == 2 && _latches == 2 && _heals == 0 && _recoveries == 0 && _health.CurrentHealth == crossingHealth,
                    "two exact real casts, healthy unhealed crossings, no recovery correction");
                Require(_level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").LatestJsonPath == string.Empty && !Directory.Exists(paths.PlaytestsDirectory),
                    "scripted movie has no automatic recorder output or fabricated hands-on telemetry");
                Record("frozen_complete", Sample());
                GD.Print("TASK17B_MOVIE_FROZEN_OK generated=true initialPriorShelf=true incomingLanding=true physicalInput=true casts=2 cross=true reverse=true return=true clocks=true actorsUnmodified=true combatProof=false silentFixed60=true");
                for (int i = 0; i < 30; i++) await Step();
            }
            catch (Exception error) { Record("failed_retained", new { error = error.ToString(), observation = _setup ? Sample() : null }); throw; }
            finally
            {
                ReleaseInputs();
                if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= ObserveClaw;
                if (GodotObject.IsInstanceValid(_level) && _level!.IsInsideTree())
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, _level);
                _metadata?.Dispose();
            }
        }

        private async Task Cast(CanyonSteamDiverterSide wanted)
        {
            await Until(30, () => _claw.CurrentFrame.State is MechanicalClawState.Idle or MechanicalClawState.Released, "fresh claw before cast");
            int count = _adapter.ApplicationCount, latchCount = _latches;
            await Step(() => KeyEvent(Key.E, true));
            await Until(8, () => _modes.CurrentMode == PlayerActionMode.Grapple, "literal E sampled into Grapple");
            await Step(() => KeyEvent(Key.E, false));
            await Step(() => { Aim(_control.GlobalPosition); MouseEvent(true); });
            await Until(90, () => _latches > latchCount, "real claw latch callback");
            Require(_lastLatchTarget == _control.GetInstanceId() && _adapter.ApplicationCount == count + 1 &&
                _pair.HasActivated && _pair.SelectedSide == wanted && _pair.IsHandingOff, "actual exact control target accepted into expected handoff");
            await Step(() => MouseEvent(false));
            await Snapshot("handoff-" + wanted);
            int handoffHealth = _health.CurrentHealth;
            for (int i = 0; i < 45 && _pair.IsHandingOff; i++)
            {
                Require(_left.Collision.Disabled && _right.Collision.Disabled && !_left.Frame.HazardActive && !_right.Frame.HazardActive,
                    "actual handoff is collision harmless on both sides");
                await Step(); Require(_health.CurrentHealth == handoffHealth, "handoff does not damage player");
            }
            Require(!_pair.IsHandingOff && _pair.SelectedSide == wanted, "bounded real handoff completes without manual clock advance");
        }

        private async Task Walk(Key key, Func<bool> arrived, int cap, int health)
        {
            await Step(() => KeyEvent(key, true));
            for (int i = 0; i < cap && !arrived(); i++) { await Step(); Require(_health.CurrentHealth == health, "actual crossing keeps observed starting health"); }
            await Step(() => KeyEvent(key, false));
            Require(arrived() && _health.CurrentHealth == health, "bounded physical walking reaches healthy endpoint");
            await Until(20, () => Math.Abs(_player.Velocity.X) <= .5f && SupportedBy(_floor),
                "observed motor has stopped on named floor, not a presumed number of physics ticks");
        }

        private async Task ObserveCycle(CanyonSteamJet jet, int health)
        {
            await Until(150, () => jet.Frame.Telegraphing, "actual selected warning phase");
            Require(jet.Collision.Disabled, "warning actual collision harmless"); await Snapshot("warning-" + _pair.SelectedSide);
            await Until(60, () => jet.Frame.HazardActive, "actual selected active phase");
            Require(!jet.Collision.Disabled && jet.Monitoring, "active actual collision enabled"); await Snapshot("active-" + _pair.SelectedSide);
            Require(_health.CurrentHealth == health && SupportedBy(_floor), "cycle observation outside active footprint remains healthy supported");
        }

        private async Task Step(Action? input = null)
        {
            // ProcessFrame is BEFORE node processing. FramePostDraw is capture-only.
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            input?.Invoke(); Input.FlushBufferedEvents();
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            Require(Engine.GetPhysicsFrames() - _startedPhysics <= MaximumPhysicsFrames, "40-second physics budget exhausted");
            if (_setup)
            {
                Record("sample", Sample());
                Require(_health.CurrentHealth > 0 && _recoveries == 0 && _heals == 0 && _player.GlobalPosition.Y < 2705,
                    "real death/recovery/heal/fall is retained failed proof, never repaired");
                Require(!_requiredHealth.HasValue || _health.CurrentHealth == _requiredHealth.Value,
                    "health remains unchanged from observed prior shelf through incoming landing/cross/return");
            }
        }
        private async Task Until(int cap, Func<bool> condition, string requirement)
        { for (int i = 0; i < cap && !condition(); i++) await Step(); Require(condition(), requirement); }
        private bool SupportedBy(StaticBody2D body)
        {
            if (!_player.IsOnFloor()) return false;
            for (int i = 0; i < _player.GetSlideCollisionCount(); i++)
            { var collision = _player.GetSlideCollision(i); if (collision.GetCollider() == body && collision.GetNormal().Y < -.7f) return true; }
            return false;
        }
        private void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.State == MechanicalClawState.Latched) { _latches++; _lastLatchTarget = frame.LockedTargetRuntimeId; }
            Record("actual_claw", new { frame.CastId, state = frame.State.ToString(), failure = frame.Failure.ToString(),
                targetId = frame.LockedTargetRuntimeId, targetName = GodotObject.IsInstanceValid(target) ? target.Name.ToString() : null });
        }
        private object Sample()
        {
            string[] contacts = Enumerable.Range(0, _player.GetSlideCollisionCount()).Select(i =>
                (_player.GetSlideCollision(i).GetCollider() as Node)?.Name.ToString() ?? "non-node").ToArray();
            var guidance = _level!.GetNode<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance");
            object? guidanceRoute = typeof(CanyonFireflyGuidanceController).GetField("_lastRouteNode", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(guidance);
            var enemies = Descendants(_level).Where(node => node is Node2D && node.GetNodeOrNull<HealthComponent>("Health") != null && node != _player)
                .Select(node => new { id = node.GetInstanceId(), name = node.Name.ToString(), type = node.GetType().Name,
                    position = Point(((Node2D)node).GlobalPosition), health = node.GetNode<HealthComponent>("Health").CurrentHealth,
                    telegraph = node.GetType().GetProperty("CurrentState")?.GetValue(node)?.ToString() ?? node.GetType().GetProperty("CurrentFrame")?.GetValue(node)?.ToString() }).ToArray();
            return new { physics = Engine.GetPhysicsFrames(), stage = _stage, world = Point(_player.GlobalPosition),
                canvas = Point(_player.GetGlobalTransformWithCanvas().Origin), velocity = Point(_player.Velocity), health = _health.CurrentHealth,
                floor = _player.IsOnFloor(), contacts, recoveries = _recoveries, heals = _heals, enemies,
                encounters = Descendants(_level).OfType<CanyonEncounterGroup>().Select(group => new { id = group.EncounterId.ToString(), active = group.IsActive, complete = group.IsComplete, live = group.LiveActorCount, warning = group.IsArrivalWarningActive, suppressed = group.SpawnSuppressed }).ToArray(),
                mode = _modes.CurrentMode.ToString(), inputSequence = _player.GetNode<PlayerInputReader>("Input").CurrentFrameSequence,
                claw = _claw.CurrentFrame.State.ToString(), target = _claw.CurrentFrame.LockedTargetRuntimeId, latches = _latches,
                accepted = GodotObject.IsInstanceValid(_adapter) ? _adapter.ApplicationCount : 0,
                pair = GodotObject.IsInstanceValid(_pair) ? new { side = _pair.SelectedSide.ToString(), handoff = _pair.IsHandingOff, activated = _pair.HasActivated,
                    left = Jet(_left), right = Jet(_right) } : null,
                route = CanyonWorldLayout.TryResolveRouteNode(_player.GlobalPosition, out var route) ? route.ToString() : null,
                guidance = guidanceRoute?.ToString(), heldKeys = _keys.Where(entry => entry.Value).Select(entry => entry.Key.ToString()).ToArray(), primaryHeld = _mouseHeld };
        }
        private static object Jet(CanyonSteamJet jet) => new { elapsed = jet.ElapsedSeconds, warning = jet.Frame.Telegraphing,
            active = jet.Frame.HazardActive, disabled = jet.Collision.Disabled, monitoring = jet.Monitoring, retired = jet.DisabledForRun };
        private static IEnumerable<Node> Descendants(Node root)
        { for (int i = 0; i < root.GetChildCount(); i++) { Node child = root.GetChild(i); yield return child; foreach (Node nested in Descendants(child)) yield return nested; } }
        private static object Point(Vector2 value) => new { x = value.X, y = value.Y };
        private void Record(string kind, object? value) => _metadata?.WriteLine(JsonSerializer.Serialize(new { kind, stage = _stage, value }));
        private void Stage(string stage) { _stage = stage; if (_stageLabel != null) _stageLabel.Text = stage; GD.Print("TASK17B_FROZEN_PHASE " + stage); Record("phase", stage); }
        private void KeyEvent(Key key, bool pressed)
        { _keys[key] = pressed; using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed, Echo = false }; Input.ParseInputEvent(input); }
        private void Aim(Vector2 world)
        {
            _pointer = _player.GetViewport().GetFinalTransform() * (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(world));
            using var motion = new InputEventMouseMotion { Position = _pointer, GlobalPosition = _pointer };
            _player.GetViewport().PushInput(motion, true); Input.ParseInputEvent(motion); Input.FlushBufferedEvents();
        }
        private void MouseEvent(bool pressed)
        { _mouseHeld = pressed; using var input = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pointer, GlobalPosition = _pointer }; Input.ParseInputEvent(input); }
        private void ReleaseInputs()
        {
            foreach (Key key in _keys.Keys.ToArray()) { try { KeyEvent(key, false); } catch (Exception error) { GD.PushError("Task17B key release: " + error); } }
            try { MouseEvent(false); Input.FlushBufferedEvents(); } catch (Exception error) { GD.PushError("Task17B mouse release: " + error); }
        }
        private async Task Snapshot(string label)
        {
            await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image image = _tree.Root.GetTexture().GetImage();
            Require(image.GetWidth() == 1280 && image.GetHeight() == 720, "native full-size capture dimensions");
            string path = Path.Combine(_output, "frames", $"{++_snapshot:D2}-{label}.png");
            Require(!File.Exists(path) && image.SavePng(path) == Error.Ok, "fresh native PNG saved inside exact case");
            Record("native_snapshot", new { path, observation = Sample() });
        }
        private void SetHud()
        {
            _tree.Root.ContentScaleSize = new Vector2I(1280, 720); _tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            _tree.Root.Size = new Vector2I(1280, 720); DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            var hud = new CanvasLayer { Name = "Task17BFrozenDisclosure", Layer = 50 };
            // Footer keeps the formal health/energy HUD at the top unobstructed.
            hud.AddChild(new Label { Text = "REGIONAL · ONE INITIAL POSE · REAL ACTORS · SCRIPTED INPUT · SILENT FIXED60 · NOT HUMAN", Position = new Vector2(12, 666), MouseFilter = Control.MouseFilterEnum.Ignore });
            _stageLabel = new Label { Position = new Vector2(12, 690), MouseFilter = Control.MouseFilterEnum.Ignore };
            hud.AddChild(_stageLabel); _level!.AddChild(hud);
        }
        private void ValidateCapture()
        {
            for (DirectoryInfo? directory = new(_output); directory != null; directory = directory.Parent)
                Require(!directory.Exists || (directory.Attributes & FileAttributes.ReparsePoint) == 0, "capture rejects reparse ancestors");
            Require(Directory.Exists(_output) && !File.Exists(Path.Combine(_output, "metadata.jsonl")), "unique launcher-owned proof directory");
            Require(DisplayServer.GetName() != "headless" && ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString() == "gl_compatibility", "normal configured hardware OpenGL Compatibility required");
            string adapter = RenderingServer.GetVideoAdapterName();
            Require(!string.IsNullOrWhiteSpace(adapter) && !new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render" }.Any(name => adapter.Contains(name, StringComparison.OrdinalIgnoreCase)), "software renderer rejected");
            Require(AudioServer.GetDriverName() == "Dummy" && Path.GetFullPath(Engine.GetWriteMoviePath()).Equals(Path.Combine(_output, "raw.avi"), StringComparison.OrdinalIgnoreCase), "exact external raw.avi and Dummy capture required");
        }
        private static void Require(bool condition, string requirement)
        { if (!condition) throw new InvalidOperationException("Task17B Frozen input proof: " + requirement); }
    }
}
