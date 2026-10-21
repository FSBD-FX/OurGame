using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Skitter;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Presentation;

// Generated full-level regional proof, NOT normal-start completion or human play.
// The single labeled setup below is the only position/velocity write. Thereafter all
// movement, combat and reward collection go through physical-key/mouse InputEvents.
// Catches missing actual-player binding, visual-only opening, fake encounter completion,
// gate damage, repeat healing, and a broken original one-jump Archive-to-Frozen route.
public partial class ArchivePressureGateProbe : SceneTree
{
    private const string RewardId = "canyon.v2.reward.archive-supply";
    private const int MaximumPhysicsFrames = 3600;
    private static readonly Vector2I WindowSize = new(1280, 720);
    private static readonly Key[] Keys = [Key.A, Key.D, Key.W, Key.S, Key.Space, Key.Q, Key.E];
    private static readonly MouseButton[] Buttons = [MouseButton.Left, MouseButton.Right];
    private readonly Dictionary<Key, bool> _wantedKeys = new();
    private readonly Dictionary<Key, bool> _heldKeys = new();
    private readonly Dictionary<MouseButton, bool> _wantedButtons = new();
    private readonly Dictionary<MouseButton, bool> _heldButtons = new();
    private readonly HashSet<ulong> _seenActors = new();
    private readonly HashSet<ulong> _deadActors = new();
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private PrologueSector _sector;
    private CharacterBody2D _player;
    private HealthComponent _health;
    private PlayerMotor _motor;
    private PlayerCombat _combat;
    private PlayerActionModeController _modes;
    private MechanicalClawController _claw;
    private CanyonEncounterGroup _group;
    private Node _actors;
    private CanyonPressurePlateGate _gate;
    private PrologueSupplyCache _supply;
    private StaticBody2D _archiveFloor;
    private Label _stageLabel;
    private Label _stateLabel;
    private StreamWriter _metadata;
    private string _proofDirectory;
    private string _stage = "initialization";
    private string _pendingSnapshot;
    private int _frames;
    private int _snapshots;
    private int _lastAttack = -100;
    private int _attacksStarted;
    private int _attacksConnected;
    private int _damageEvents;
    private int _healEvents;
    private int _healedAmount;
    private int _recoveriesRequested;
    private int _recoveriesCompleted;
    private int _rustDeaths;
    private int _skitterDeaths;
    private int _warningHealth;
    private bool _sawArrivalWarning;
    private bool _sawShieldCounterplay;
    private bool _setup;
    private bool _encounterDefeated;
    private bool _checkGateDamage;
    private int _damageBeforeGate;
    private bool _routeActive;
    private int _routeJumps;
    private int _routeDashes;
    private ulong _routeCastId;
    private ulong _routeNextCastId;
    private MechanicalClawState _routeClawState;
    private int _routeArchiveRetouchFrames;
    private string _lastFloor = "unobserved";
    private Vector2 _stepStartPosition;
    private int _lastAcceptedJumps;
    private bool _roofProbeActive;
    private int _roofProbeStartFrame;
    private int _roofProbeHealth;
    private int _roofProbeHeals;
    private int _roofProbeJumps;
    private int _roofProbeDashes;
    private bool _roofClawProofComplete;

    public override async void _Initialize()
    {
        int exitCode = 1;
        try
        {
            ValidateInvocation(); // Every proof/helper write follows this closed boundary.
            Directory.CreateDirectory(_proofDirectory);
            string framesDirectory = ProofPath("frames");
            Require(!Directory.Exists(framesDirectory) ||
                !Directory.EnumerateFileSystemEntries(framesDirectory).Any(),
                "proof frames directory must be fresh; failed cases must not be overwritten");
            Directory.CreateDirectory(framesDirectory);
            _metadata = new StreamWriter(new FileStream(ProofPath("metadata.jsonl"),
                FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            Record("invocation", new
            {
                project = ProjectSettings.GlobalizePath("res://"),
                unconsumedEngineArguments = OS.GetCmdlineArgs(), userArguments = OS.GetCmdlineUserArgs(),
                renderer = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                adapter = RenderingServer.GetVideoAdapterName(), audioDriver = AudioServer.GetDriverName(),
                writeMoviePath = Engine.GetWriteMoviePath(), expectedFixedFps = 60,
                fixedFpsProvenance = "Guarded launcher processes.txt request plus engine Movie Maker log; not independently verified by this probe",
                limits = "Scripted InputEvents; one initial regional setup; silent fixed-fps; not human/realtime/audio/full-prologue acceptance"
            });

            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
            _flow.HeadlessAutoStart = false;
            _flow.AutomationAutoStart = false;
            IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(_level,
                "task16b-archive-generated-proof");
            Record("isolated_paths_before_add_child", paths);
            _level.RecoveryRequested += _ => _recoveriesRequested++;
            _level.RecoveryCompleted += _ => _recoveriesCompleted++;
            Root.AddChild(_level);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Require(_flow.State == PrologueGameFlowState.Title, "both autostarts remain disabled");
            _flow.StartNew();
            Require(_flow.State == PrologueGameFlowState.Playing, "explicit isolated StartNew enters Playing");
            _player = _level.Player;
            Require(GodotObject.IsInstanceValid(_player), "actual level player exists");
            _health = _player.GetNode<HealthComponent>("Health");
            _motor = _player.GetNode<PlayerMotor>("Motor");
            _combat = _player.GetNode<PlayerCombat>("Combat");
            _modes = _player.GetNode<PlayerActionModeController>("ActionModes");
            _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
            _combat.AttackStarted += (_, _) => _attacksStarted++;
            _combat.AttackConnected += _ => _attacksConnected++;
            _health.Damaged += (remaining, impulse) =>
            {
                _damageEvents++;
                Record("real_damage", new { remaining, impulse = impulse.ToString(), stage = _stage });
            };
            _health.Healed += (restored, remaining) =>
            {
                _healEvents++;
                _healedAmount += restored;
                Record("real_heal", new { restored, remaining, stage = _stage });
            };
            SetWindowAndHud();
            Stage("INITIAL REGIONAL SETUP (1465,1604) — generated Archive; not normal-start completion");
            await Step(() =>
            {
                _player.GlobalPosition = new Vector2(1465f, 1604f); // ONE labeled regional setup.
                _player.Velocity = Vector2.Zero; // Same setup, before any proof traversal.
                Camera2D camera = _player.GetNode<Camera2D>("Camera2D");
                camera.Enabled = true;
                camera.ResetSmoothing();
                camera.ForceUpdateScroll();
                _setup = true;
            });
            await Until("actual generated Archive mounts", 90, () =>
            {
                _sector = _level.GetNodeOrNull<PrologueSector>("WorldRoot/Sectors/CanyonDescent");
                return _sector?.GetNodeOrNull<CanyonPressurePlateGate>(
                    "Gameplay/Mechanisms/ArchivePressureGate") != null;
            });
            ResolveGeneratedInstallation();
            await Frames(3);
            Require(HasFloorContact(_archiveFloor) && _gate.PlayerBody == _player,
                "regional setup stands on actual ArchiveFloor and gate binds the actual level player");
            Record("regional_setup", new { player = _player.GlobalPosition.ToString(),
                health = _health.CurrentHealth, initialWrites = "one position plus zero velocity", afterSetup = "InputEvents only" });

            await DefeatActualArchiveEncounter();
            await VisitActualAlcove();
            await TraverseOldArchiveToFrozen();
            Stage("PROOF COMPLETE — Rust + Skitter defeated; optional supply; old left-side jump/fall route intact");
            await Frames(18);
            Require(_roofClawProofComplete && _rustDeaths == 1 && _skitterDeaths == 1 && _deadActors.Count == 2 &&
                _group.IsComplete && _group.LiveActorCount == 0 && _recoveriesRequested == 0 &&
                _recoveriesCompleted == 0, "final actual encounter defeat stays stable without recovery");
            Record("success", new { frames = _frames, health = _health.CurrentHealth,
                attacks = _attacksStarted, hits = _attacksConnected, rustDeaths = _rustDeaths,
                skitterDeaths = _skitterDeaths, damageEvents = _damageEvents,
                healEvents = _healEvents, healedAmount = _healedAmount, rewardSpent = _supply.IsSpent,
                roofClawProof = _roofClawProofComplete,
                roofClawScope = "One forbidden-door cast and one real roof attachment/boost/support/left return; not all possible bypass trajectories",
                routeJumpDelta = _motor.AcceptedJumpCount - _routeJumps, dashDelta = _motor.AcceptedDashCount - _routeDashes,
                snapshots = _snapshots, recoveries = _recoveriesCompleted });
            GD.Print($"TASK16B_ARCHIVE_GENERATED_PROOF_OK frames={_frames} " +
                $"rust_deaths={_rustDeaths} skitter_deaths={_skitterDeaths} " +
                $"reward_spent={_supply.IsSpent} heals={_healEvents} healed={_healedAmount} " +
                $"single_jump={_motor.AcceptedJumpCount == _routeJumps + 1} " +
                $"recoveries={_recoveriesCompleted} proof={_proofDirectory}");
            exitCode = 0;
        }
        catch (Exception error)
        {
            Record("failure", new { error = error.ToString(), stage = _stage, state = StateSnapshot() });
            GD.PushError($"TASK16B_ARCHIVE_GENERATED_PROOF_FAIL frame={_frames} stage={_stage}: {error}");
        }
        finally
        {
            // Independent releases: an exception releasing one input cannot strand another.
            if (!ReleaseAllImmediate()) exitCode = 1;
            _metadata?.Dispose();
            _metadata = null;
            // No teardown/GC workaround. Process ownership and exit evidence belong to launcher.
            Quit(exitCode);
        }
    }

    private void ResolveGeneratedInstallation()
    {
        _gate = _sector.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/ArchivePressureGate");
        _supply = _sector.GetNode<PrologueSupplyCache>("Gameplay/Rewards/ArchiveSupply");
        _group = _sector.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/ArchiveSideCaveEncounter");
        _actors = _sector.GetNode("Gameplay/Actors");
        _archiveFloor = Body("ArchiveFloor");
        Require(_gate.MechanismId == "canyon.v2.pressure-gate.archive-reward" &&
            _supply.DiscoveryId == RewardId && _supply.HealthAmount == 2 && !_supply.IsSpent,
            "actual generated stable gate and finite two-health reward exist and are initially unspent");
        Require(_gate.DoorCollision.Shape is RectangleShape2D shape &&
            shape.Size == new Vector2(40f, 216f), "actual door has the authored blocking shape");
    }

    private async Task DefeatActualArchiveEncounter()
    {
        Stage("A/D + Q + mouse: defeat the actual Archive Rust, then arrival-cued Skitter");
        WantKey(Key.Q, true);
        await Step();
        WantKey(Key.Q, false);
        for (int index = 0; index < 1500 && !_group.IsComplete; index++)
        {
            Require(!_group.SpawnSuppressed, "actual Archive arrival was not suppressed");
            Node2D actor = FindActualActor();
            if (actor != null) ObserveActor(actor);
            if (_group.IsArrivalWarningActive)
            {
                if (!_sawArrivalWarning)
                {
                    _sawArrivalWarning = true;
                    _warningHealth = _health.CurrentHealth;
                    Stage("Rust defeated — harmless arrival cue before the actual Skitter");
                }
                Require(_health.CurrentHealth == _warningHealth,
                    "no actor means no damage during the arrival warning");
                WantNeutral();
                await Step();
            }
            else if (actor != null && _group.IsActive)
                await Step(() => DriveActor(actor));
            else
            {
                WantNeutral();
                await Step();
            }
        }
        WantNeutral();
        Require(_group.IsComplete && _rustDeaths == 1 && _skitterDeaths == 1 &&
            _deadActors.Count == 2 && _seenActors.Count == 2 && _sawArrivalWarning &&
            _attacksStarted >= 2 && _attacksConnected >= 2,
            "actual attacks must defeat both generated enemy instances without retirement or API kills");
        _encounterDefeated = true;
        Stage("Actual encounter defeated — no replacement, no fake progress; walk to the optional gate");
        await Frames(30);
        Record("encounter_complete", new { rustDeaths = _rustDeaths, skitterDeaths = _skitterDeaths,
            attacks = _attacksStarted, hits = _attacksConnected, counterplay = _sawShieldCounterplay,
            realDamageEvents = _damageEvents, health = _health.CurrentHealth });
    }

    private Node2D FindActualActor()
    {
        var children = _actors.GetChildren();
        using var childrenOwner = (Godot.Collections.Array)children;
        foreach (Node child in children)
            if (child is Node2D node && node.Name.ToString().StartsWith(
                _group.EncounterId.ToString().Replace('.', '_'), StringComparison.Ordinal))
                return node; // Borrowed scene node; only the temporary array wrapper is owned here.
        return null;
    }

    private void ObserveActor(Node2D actor)
    {
        ulong instance = actor.GetInstanceId();
        if (!_seenActors.Add(instance)) return;
        Require(actor is RustShellSquire or CanyonSkitter, "Archive uses its actual Rust and Skitter actors");
        bool rust = actor is RustShellSquire;
        string type = actor.GetType().Name;
        actor.GetNode<HealthComponent>("Health").Died += () =>
        {
            if (!_deadActors.Add(instance)) return;
            if (rust) _rustDeaths++; else _skitterDeaths++;
            Record("actual_enemy_died", new { instance, type, inputAttacks = _attacksStarted });
        };
        Record("actual_enemy_seen", new { instance, type, position = actor.GlobalPosition.ToString(),
            batch = _group.CurrentBatch });
    }

    // Based on frozen Task14GeneratedRegionProbe's Archive combat controller; no enemy mutation.
    private void DriveActor(Node2D actor)
    {
        Require(GodotObject.IsInstanceValid(actor), "selected actual actor remains mounted for input");
        Aim(actor.GlobalPosition);
        float dx = actor.GlobalPosition.X - _player.GlobalPosition.X;
        float dy = actor.GlobalPosition.Y - _player.GlobalPosition.Y;
        float distance = MathF.Abs(dx);
        bool threat = actor switch
        {
            RustShellSquire rust => rust.CurrentState is RustShellState.Telegraph or RustShellState.Attack,
            CanyonSkitter skitter => skitter.CurrentFrame.State is CanyonSkitterState.Telegraph or CanyonSkitterState.Rush,
            _ => false
        };
        bool right = dx > 63f || dx < 0f && distance < 29f;
        bool left = dx < -63f || dx > 0f && distance < 29f;
        if (_player.GlobalPosition.X < _group.Definition.ArenaBounds.Position.X + 28f) left = false;
        if (_player.GlobalPosition.X > _group.Definition.ArenaBounds.End.X - 28f) right = false;
        WantKey(Key.A, left);
        WantKey(Key.D, right);
        float remaining = actor switch
        {
            RustShellSquire rust => rust.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
            CanyonSkitter skitter => skitter.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
            _ => 0f
        };
        bool defend = threat && !_combat.IsAttacking && distance < 120f &&
            (remaining < .18f || actor is CanyonSkitter rushing && rushing.CurrentFrame.State == CanyonSkitterState.Rush);
        WantButton(MouseButton.Right, defend);
        if (defend) _sawShieldCounterplay = true;
        bool strike = !threat && distance is >= 28f and <= 64f && MathF.Abs(dy) < 48f &&
            !_combat.IsAttacking && _modes.CanBeginAttack && _frames - _lastAttack >= 23;
        if (strike)
        {
            WantButton(MouseButton.Right, false);
            WantKey(Key.A, dx < 0f);
            WantKey(Key.D, dx > 0f);
            WantButton(MouseButton.Left, true);
            _lastAttack = _frames;
        }
        if (_frames - _lastAttack >= 2) WantButton(MouseButton.Left, false);
    }

    private async Task VisitActualAlcove()
    {
        _damageBeforeGate = _damageEvents;
        _checkGateDamage = true;
        await WalkTo(1510f, 160);
        Stage("Outside: actual closed collision; brass plate connects to the optional supply door");
        await Until("outside gate reaches a committed safe close", 170, () => !_gate.IsOpen);
        Require(!_gate.DoorCollision.Disabled && !_gate.Frame.Traversable,
            "closed visual corresponds to committed physical blocking shape");
        await Frames(24);
        await ShowClosedDoorAirborneApproach();
        await ShowClosedGateRoofClawApproach();

        Stage("D: grounded feet press the real plate; wait for committed open collision");
        await WalkTo(1650f, 100);
        await Until("actual grounded plate opens committed door", 30, () =>
            _player.IsOnFloor() && _gate.IsOpen && _gate.HasOpened);
        Require(_gate.DoorCollision.Disabled && _gate.Frame.Traversable,
            "crossing is allowed only after committed shape opening");
        await Frames(18);

        int beforeHealth = _health.CurrentHealth;
        int beforeHeals = _healEvents;
        int beforeAmount = _healedAmount;
        Stage(beforeHealth < _health.MaxHealth
            ? "D: real combat injury → finite Archive supply (+2 maximum)"
            : "D: full health → actual supply remains available (no healing claim)");
        await WalkTo(1820f, 130);
        await Until("actual player overlaps actual finite supply", 20, () => _supply.OverlapsBody(_player));
        await Frames(8);
        if (beforeHealth < _health.MaxHealth)
        {
            int restored = Math.Min(2, _health.MaxHealth - beforeHealth);
            Require(_damageEvents > 0 && _supply.IsSpent && _health.CurrentHealth == beforeHealth + restored &&
                _healEvents == beforeHeals + 1 && _healedAmount == beforeAmount + restored &&
                _flow.Save.Discoveries.Contains(RewardId),
                "only genuine combat damage permits one finite overlap heal and persisted reward discovery");
        }
        else Require(!_supply.IsSpent && _healEvents == beforeHeals && _health.CurrentHealth == beforeHealth,
            "full-health real overlap does not consume or invent healing");
        int afterHealth = _health.CurrentHealth;
        int afterHeals = _healEvents;
        Stage("Interior hold: same player remains safe beyond grace + closing warning");
        await Frames(130);
        Require(_gate.IsOpen && _gate.DoorCollision.Disabled && !_gate.IsDangerTelegraphActive &&
            _player.IsOnFloor() && _health.CurrentHealth == afterHealth && _healEvents == afterHeals,
            "interior occupancy holds actual collision open without duplicate healing or gate damage");

        Stage("D: the visible right enclosure is a real wall, not an exit through decoration");
        WantKey(Key.D, true);
        StaticBody2D rightWall = Body("ArchiveSupplyRightWall");
        await Until("actual player is physically blocked by the visible right wall", 65,
            () => HasSideContact(rightWall));
        WantNeutral();
        await Frames(8);
        Require(_player.GlobalPosition.X < 1880f && _health.CurrentHealth == afterHealth &&
            _healEvents == afterHeals && _gate.IsOpen,
            "right wall blocks the real player without damage, duplicate reward, or gate closure");

        Stage("A: return across the doorway; then re-enter the same finite supply overlap");
        await WalkTo(1650f, 130);
        await WalkTo(1820f, 130);
        await Until("second genuine supply overlap", 20, () => _supply.OverlapsBody(_player));
        Require(_health.CurrentHealth == afterHealth && _healEvents == afterHeals &&
            _supply.IsSpent == (beforeHealth < _health.MaxHealth),
            "re-entering cannot duplicate reward, healing, or consume a full-health supply");
        Stage("A: leave optional alcove and plate; harmless countdown closes the actual door");
        await WalkTo(1510f, 170);
        await Until("closing warning remains open and harmless", 150, () => _gate.IsDangerTelegraphActive);
        Require(_gate.Frame.Traversable && !_gate.Frame.HazardActive, "closing warning remains harmless and passable");
        Stage("Orange closing countdown — still traversable; no crushing or health loss");
        await Until("actual committed close after warning", 70, () => !_gate.IsOpen);
        Require(!_gate.DoorCollision.Disabled && !_gate.Frame.Traversable &&
            _damageEvents == _damageBeforeGate, "gate visit and return cause no damage");
        await Frames(18);
        _checkGateDamage = false;
    }

    private async Task ShowClosedDoorAirborneApproach()
    {
        // Start immediately OUTSIDE the visible plate by normal walking, not another setup.
        await WalkTo(1595f, 80);
        Require(!_gate.IsOpen && _player.GlobalPosition.X < 1620f && HasFloorContact(_archiveFloor),
            "airborne approach starts grounded outside the plate with a physically closed door");
        Stage("D + Space before the plate: an airborne approach cannot activate or bypass the closed door");
        int jumps = _motor.AcceptedJumpCount;
        WantKey(Key.D, true);
        WantKey(Key.Space, true);
        bool blockedInAir = false;
        for (int index = 0; index < 40; index++)
        {
            await Step();
            Require(!_gate.IsOpen && !_gate.DoorCollision.Disabled && !_gate.Frame.Traversable,
                "jumping across the plate must not activate the actual closed gate");
            if (!_player.IsOnFloor() && HasSideContact(_gate.GetNode<StaticBody2D>("Door")))
            {
                blockedInAir = true;
                break;
            }
        }
        Require(blockedInAir && _motor.AcceptedJumpCount == jumps + 1 &&
            _player.GlobalPosition.X < 1720f,
            "ordinary airborne approach hits the real closed door instead of passing through/over it");
        Record("closed_door_airborne_contact", StateSnapshot());
        WantNeutral();
        // Returning can legitimately touch the plate. Wait for its normal close, never reset it.
        await WalkTo(1510f, 130);
        await Until("normal outside return closes gate again", 170, () => !_gate.IsOpen);
    }

    private async Task ShowClosedGateRoofClawApproach()
    {
        // Exactly two attempts, never retried: a rejected door cast, then a permitted
        // roof cast with ONE attached Space boost. Neither attempt may enter the alcove.
        StaticBody2D door = _gate.GetNode<StaticBody2D>("Door");
        StaticBody2D roof = Body("ArchiveSupplyRoof");
        GrappleSurface2D doorSurface = door.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        GrappleSurface2D roofSurface = roof.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        using NodePath doorPathValue = doorSurface.GetPath();
        using NodePath roofSurfacePathValue = roofSurface.GetPath();
        using NodePath roofBodyPathValue = roof.GetPath();
        string doorPath = doorPathValue.ToString();
        string roofSurfacePath = roofSurfacePathValue.ToString();
        string roofBodyPath = roofBodyPathValue.ToString();
        Require(doorSurface.Material == GrappleSurfaceMaterial.Forbidden &&
            roofSurface.Material == GrappleSurfaceMaterial.GripStone &&
            doorSurface.UseCollisionHitPoint && roofSurface.UseCollisionHitPoint,
            "actual generated door rejects attachment but the actual stone roof permits it");
        Require(HasFloorContact(_archiveFloor) && _player.GlobalPosition.X < 1600f &&
            !_gate.IsOpen && !_supply.IsSpent && !_supply.OverlapsBody(_player),
            "roof-claw attempt starts from actual left floor with a closed door and unspent supply");
        _roofProbeStartFrame = _frames;
        _roofProbeHealth = _health.CurrentHealth;
        _roofProbeHeals = _healEvents;
        _roofProbeJumps = _motor.AcceptedJumpCount;
        _roofProbeDashes = _motor.AcceptedDashCount;
        _roofProbeActive = true;
        var samples = new List<(MechanicalClawFrame Frame, ulong TargetId)>();
        MechanicalClawFrame? previous = null;
        ulong previousTarget = 0UL;
        void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D surface)
        {
            // Borrowed surface only. Record transient Boost -> Released publications;
            // assert after Step rather than throwing inside a production event callback.
            ulong targetId = GodotObject.IsInstanceValid(surface) ? surface.GetInstanceId() : 0UL;
            samples.Add((frame, targetId));
            if (!previous.HasValue || previous.Value.State != frame.State ||
                previous.Value.Failure != frame.Failure || previous.Value.CastId != frame.CastId ||
                previous.Value.LockedTargetRuntimeId != frame.LockedTargetRuntimeId ||
                previous.Value.Arrived != frame.Arrived || previous.Value.ReleaseKind != frame.ReleaseKind ||
                targetId != previousTarget)
                Record("roof_probe_claw_event", new
                {
                    stage = _stage, state = frame.State.ToString(), failure = frame.Failure.ToString(),
                    castId = frame.CastId, nextCastId = frame.NextCastId, targetId,
                    targetPath = targetId == 0UL ? null : surface == doorSurface ? doorPath :
                        surface == roofSurface ? roofSurfacePath : "unexpected-surface:" + surface.Name,
                    lockedTarget = frame.LockedTargetRuntimeId, anchor = frame.AnchorPoint.ToString(),
                    normal = frame.SurfaceNormal.ToString(), dock = frame.DockPoint.ToString(),
                    frame.Arrived, frame.BoostConsumed, release = frame.ReleaseKind.ToString(),
                    caster = frame.CasterWorldPosition.ToString(), acceptedOrdinaryJumps = _motor.AcceptedJumpCount
                });
            previous = frame;
            previousTarget = targetId;
        }
        _claw.ClawFrameChanged += ObserveClaw;
        try
        {
            WantNeutral();
            Stage("E + one LMB at the closed door: Forbidden means NO attachment, not an invisible route");
            WantKey(Key.E, true);
            await Step();
            WantKey(Key.E, false);
            await Step();
            Require(_modes.CurrentMode == PlayerActionMode.Grapple &&
                _claw.CurrentFrame.State == MechanicalClawState.Idle,
                "real E input selects an idle grapple before the single door cast");
            ulong nextBeforeDoor = _claw.CurrentFrame.NextCastId;
            Vector2 doorAim = new(1740f, 1532f);
            Record("roof_probe_cast_input", new { purpose = "forbidden-door", aim = doorAim.ToString(),
                expectedSurfaceId = doorSurface.GetInstanceId(), expectedPath = doorPath });
            await Step(() => { Aim(doorAim); WantButton(MouseButton.Left, true); });
            WantButton(MouseButton.Left, false);
            await Until("one actual door cast reports Forbidden and returns naturally to Idle", 24, () =>
                samples.Any(sample => sample.Frame.Failure == MechanicalClawFailure.Forbidden) &&
                _claw.CurrentFrame.State == MechanicalClawState.Idle);
            Require(samples.Any(sample => sample.Frame.State == MechanicalClawState.Aim &&
                    sample.TargetId == doorSurface.GetInstanceId()) &&
                !samples.Any(sample => sample.Frame.State is MechanicalClawState.Windup or
                    MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Boost) &&
                _claw.CurrentFrame.NextCastId == nextBeforeDoor && _claw.CurrentTarget == null &&
                _claw.ResidencyTarget == null,
                "Forbidden result belongs to the actual door and never allocates a live attachment cast");

            await WalkTo(1595f, 80);
            Require(HasFloorContact(_archiveFloor) && _player.GlobalPosition.X < 1620f,
                "roof cast begins by ordinary walking outside the plate, not by another setup");
            samples.Clear();
            previous = null;
            ulong nextBeforeRoof = _claw.CurrentFrame.NextCastId;
            Vector2 roofAim = new(1722f, 1408f);
            Stage("One LMB at the stone roof edge: real GripStone attachment and automatic pull");
            Record("roof_probe_cast_input", new { purpose = "permitted-roof", aim = roofAim.ToString(),
                expectedSurfaceId = roofSurface.GetInstanceId(), expectedPath = roofSurfacePath });
            await Step(() => { Aim(roofAim); WantButton(MouseButton.Left, true); });
            WantButton(MouseButton.Left, false);
            await Until("actual roof attachment reaches its real docking envelope", 180, () =>
                (_claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing) &&
                _claw.CurrentFrame.Arrived);
            MechanicalClawFrame attached = _claw.CurrentFrame;
            Require(_claw.CurrentTarget == roofSurface && _claw.ResidencyTarget == roofSurface &&
                attached.LockedTargetRuntimeId == roofSurface.GetInstanceId() &&
                attached.CastId == nextBeforeRoof && attached.NextCastId == nextBeforeRoof + 1UL &&
                attached.SurfaceNormal.Dot(Vector2.Left) > .99f &&
                MathF.Abs(attached.AnchorPoint.X - 1720f) <= 1f &&
                attached.AnchorPoint.Y is >= 1400f and <= 1424f &&
                samples.Any(sample => sample.Frame.State == MechanicalClawState.Latched &&
                    sample.TargetId == roofSurface.GetInstanceId()),
                "the single real cast attaches to this roof's exact surface instance and left collision face");
            Record("roof_probe_actual_attachment", StateSnapshot());

            Stage("A + ONE attached Space: grapple boost, NOT another ordinary jump; D steers above the roof");
            PlayerInputReader boostInput = _player.GetNode<PlayerInputReader>("Input");
            WantKey(Key.A, true);
            WantKey(Key.Space, true);
            await Step();
            Record("roof_probe_boost_input_sample", new { observation = "after-send-step",
                inputFrameSequence = boostInput.CurrentFrameSequence,
                jumpPressed = boostInput.CurrentFrame.JumpPressed, jumpHeld = boostInput.CurrentFrame.JumpHeld,
                moveAxis = boostInput.CurrentFrame.MoveAxis, clawState = _claw.CurrentFrame.State.ToString() });
            // This physics_frame injection reaches one-shot action sampling on the next tick,
            // as the retained case's Space and LMB edges demonstrate. Keep the same A/Space
            // press through that boundary; do not retry the press or release A before boost.
            await Step();
            Record("roof_probe_boost_input_sample", new { observation = "after-next-sample-step",
                inputFrameSequence = boostInput.CurrentFrameSequence,
                jumpPressed = boostInput.CurrentFrame.JumpPressed, jumpHeld = boostInput.CurrentFrame.JumpHeld,
                moveAxis = boostInput.CurrentFrame.MoveAxis, clawState = _claw.CurrentFrame.State.ToString() });
            WantKey(Key.A, false);
            WantKey(Key.Space, false);
            int boostIndex = samples.FindIndex(sample => sample.Frame.State == MechanicalClawState.Boost);
            Require(boostIndex >= 0 && boostIndex + 1 < samples.Count &&
                samples[boostIndex + 1].Frame.State == MechanicalClawState.Released &&
                samples[boostIndex + 1].Frame.ReleaseKind == MechanicalClawReleaseKind.Boost &&
                samples[boostIndex + 1].Frame.BoostConsumed &&
                samples.Count(sample => sample.Frame.State == MechanicalClawState.Boost) == 1 &&
                _motor.AcceptedJumpCount == _roofProbeJumps,
                "one attached Space publishes exactly one Boost then Released without an ordinary jump");
            for (int index = 0; index < 100 && !HasFloorContact(roof); index++)
            {
                WantKey(Key.D, _player.GlobalPosition.X < 1800f);
                await Step();
            }
            WantNeutral();
            await Frames(6);
            var roofBounds = Bounds(roof);
            float radius = ((CapsuleShape2D)_player.GetNode<CollisionShape2D>("BodyCollider").Shape).Radius;
            Require(HasFloorContact(roof) && _player.GlobalPosition.X >= roofBounds.Left + radius - .5f &&
                _player.GlobalPosition.X <= roofBounds.Right - radius + .5f &&
                _motor.AcceptedJumpCount == _roofProbeJumps &&
                _claw.CurrentFrame.NextCastId == nextBeforeRoof + 1UL &&
                samples.Count(sample => sample.Frame.State == MechanicalClawState.Boost) == 1,
                "real roof collider supports the whole player after one claw boost, with no extra cast/jump");
            Stage("Actual roof support: closed alcove remains sealed below; no supply overlap or healing");
            Record("roof_probe_roof_support", new { roofInstance = roof.GetInstanceId(),
                roofPath = roofBodyPath, surfaceInstance = roofSurface.GetInstanceId(),
                actualFloor = FloorContacts(), player = _player.GlobalPosition.ToString(),
                ordinaryJumpDelta = _motor.AcceptedJumpCount - _roofProbeJumps, boosts = 1 });
            await Frames(12);

            Stage("A: normal left walk/fall from roof to ArchiveFloor — no second leap or forced return");
            WantKey(Key.A, true);
            await Until("real left return reaches ArchiveFloor outside the plate", 160, () =>
                HasFloorContact(_archiveFloor) && _player.GlobalPosition.X < 1600f);
            WantNeutral();
            await Frames(10);
            await WalkTo(1510f, 80);
            WantKey(Key.Q, true);
            await Step();
            WantKey(Key.Q, false);
            await Until("real Q selection and natural grounded Idle clear the roof cast", 30, () =>
                _modes.CurrentMode == PlayerActionMode.Attack &&
                _claw.CurrentFrame.State == MechanicalClawState.Idle &&
                _claw.CurrentTarget == null && _claw.ResidencyTarget == null &&
                !_motor.HasGrappleReleaseMomentum);
            await Step(); // Deliver the staged Q release even if the predicate was already satisfied.
            Require(HasFloorContact(_archiveFloor) && _player.GlobalPosition.X < 1600f &&
                _claw.CurrentFrame.NextCastId == nextBeforeRoof + 1UL &&
                samples.Count(sample => sample.Frame.State == MechanicalClawState.Boost) == 1,
                "same player returns naturally, with one roof cast/boost and no hidden extra leap");
            _roofClawProofComplete = true;
            Record("roof_probe_complete", new { frames = _frames - _roofProbeStartFrame,
                ordinaryJumpDelta = _motor.AcceptedJumpCount - _roofProbeJumps,
                dashDelta = _motor.AcceptedDashCount - _roofProbeDashes, acceptedRoofCasts = 1, boosts = 1,
                player = _player.GlobalPosition.ToString(), actualFloor = FloorContacts(),
                scope = "One actual forbidden-door cast and one roof attachment/boost/support/left-return trajectory; not exhaustive bypass certification" });
        }
        finally
        {
            _claw.ClawFrameChanged -= ObserveClaw;
            _roofProbeActive = false;
        }
    }

    // Adapted read-only source: PrologueCanyonGeneratedMainRouteTests.TraverseArchiveFloorToFrozenLeft.
    private async Task TraverseOldArchiveToFrozen()
    {
        Stage("A: return to the unchanged old launch area; supply alcove was optional");
        await WalkTo(1465f, 80);
        StaticBody2D ceilingBody = Body("FrozenCeiling");
        StaticBody2D targetBody = Body("FrozenLeftCatch");
        var archive = Bounds(_archiveFloor);
        var ceiling = Bounds(ceilingBody);
        var target = Bounds(targetBody);
        float radius = ((CapsuleShape2D)_player.GetNode<CollisionShape2D>("BodyCollider").Shape).Radius;
        TraversalSegment2D route = _sector.GetNode<TraversalSegment2D>(
            "Gameplay/TraversalContracts/canyonv2routearchive-to-frozen-detour");
        Require(HasFloorContact(_archiveFloor) && route.Kind == TraversalSegmentKind.SingleJump &&
            route.RequiredAbilities == TraversalAbility.Jump && route.LaunchZone.HasPoint(_sector.ToLocal(_player.GlobalPosition)),
            "ordinary-jump launch uses actual unchanged ArchiveFloor and authored launch rectangle");
        _routeJumps = _motor.AcceptedJumpCount;
        _routeDashes = _motor.AcceptedDashCount;
        _routeCastId = _claw.CurrentFrame.CastId;
        _routeNextCastId = _claw.CurrentFrame.NextCastId;
        _routeClawState = _claw.CurrentFrame.State;
        _routeActive = true;
        Vector2 jumpStart = _player.GlobalPosition;
        Stage("Archive left-side jump/fall route: A + one Space; NO extra jump / dash / claw / recovery");
        Record("old_route_launch", new { position = jumpStart.ToString(), nextInputPhysicsFrame = _frames + 1,
            acceptedJumpsBefore = _routeJumps, launchZone = route.LaunchZone.ToString(),
            interpretation = "Existing left-side jump/fall route; an ArchiveFloor retouch is recorded, not hidden" });
        WantNeutral();
        WantKey(Key.A, true);
        WantKey(Key.Space, true);
        bool cleared = false;
        Vector2 leftClear = Vector2.Zero;
        for (int index = 0; index < 240 && !HasFloorContact(ceilingBody) && !HasFloorContact(targetBody); index++)
        {
            await Step();
            if (index >= 6) WantKey(Key.Space, false);
            if (!cleared && _player.GlobalPosition.X <= archive.Left - radius)
            {
                cleared = true;
                leftClear = _player.GlobalPosition;
                Record("archive_left_clearance", new { position = leftClear.ToString(),
                    acceptedJumpCount = _motor.AcceptedJumpCount, floor = FloorContacts(),
                    archiveRetouchFramesAfterJump = _routeArchiveRetouchFrames });
            }
            if (cleared) SteerWithin(ceiling.Left + radius + 12f, ceiling.Right - radius - 12f, false);
        }
        WantNeutral();
        await Frames(2);
        bool direct = HasFloorContact(targetBody);
        Require(cleared && (direct || HasFloorContact(ceilingBody)),
            "one ordinary jump plus A/D reaches real Frozen support via the unchanged Archive left exit");
        Vector2 ceilingLanding = direct ? Vector2.Zero : _player.GlobalPosition;
        if (!direct)
        {
            Require(_player.GlobalPosition.X >= ceiling.Left + radius - .5f &&
                _player.GlobalPosition.X <= ceiling.Right - radius + .5f,
                "whole player capsule lands on actual FrozenCeiling");
            Stage("A: walk off FrozenCeiling; A/D air correction reaches the old left catch");
            WantKey(Key.A, true);
            await Until("walk off actual FrozenCeiling without another jump", 120,
                () => !HasFloorContact(ceilingBody) && !_player.IsOnFloor());
            WantKey(Key.A, false);
            WantKey(Key.D, true);
            for (int index = 0; index < 180 && !HasFloorContact(targetBody); index++)
            {
                await Step();
                SteerWithin(target.Left + radius + 10f, target.Right - radius - 10f, true);
            }
            WantNeutral();
            await Frames(2);
        }
        Vector2 landing = _player.GlobalPosition;
        Require(HasFloorContact(targetBody) && landing.X >= target.Left + radius - .5f &&
            landing.X <= target.Right - radius + .5f && route.LandingZone.HasPoint(_sector.ToLocal(landing)) &&
            _motor.AcceptedJumpCount == _routeJumps + 1 && _motor.AcceptedDashCount == _routeDashes &&
            _claw.CurrentFrame.CastId == _routeCastId && _claw.CurrentFrame.NextCastId == _routeNextCastId &&
            _recoveriesRequested == 0 && _recoveriesCompleted == 0,
            "same player lands on actual old FrozenLeftCatch with exactly one ordinary jump and no assist/recovery");
        Record("old_route_complete", new { jumpStart = jumpStart.ToString(), leftClear = leftClear.ToString(),
            ceilingLanding = ceilingLanding.ToString(), landing = landing.ToString(), directLanding = direct,
            jumpDelta = _motor.AcceptedJumpCount - _routeJumps, dashDelta = _motor.AcceptedDashCount - _routeDashes,
            clawCastUnchanged = _claw.CurrentFrame.CastId == _routeCastId, actualFloor = targetBody.Name.ToString(),
            archiveRetouchFramesAfterJump = _routeArchiveRetouchFrames,
            limitation = "Legacy one-jump-plus-A/D route contract; floor transitions are explicit, not an uninterrupted-airborne claim" });
    }

    private void SteerWithin(float minimum, float maximum, bool brakeLeftward)
    {
        bool right = _player.GlobalPosition.X < minimum || brakeLeftward && _player.Velocity.X < -5f;
        bool left = !right && _player.GlobalPosition.X > maximum;
        WantKey(Key.A, left);
        WantKey(Key.D, right);
    }

    private async Task WalkTo(float target, int maximumFrames)
    {
        WantNeutral();
        for (int index = 0; index < maximumFrames; index++)
        {
            float distance = target - _player.GlobalPosition.X;
            if (MathF.Abs(distance) <= 6f)
            {
                WantNeutral();
                await Frames(10);
                Require(_player.IsOnFloor() && MathF.Abs(target - _player.GlobalPosition.X) <= 16f,
                    $"real horizontal input stops near {target}; actual={_player.GlobalPosition}");
                return;
            }
            WantKey(Key.A, distance < 0f);
            WantKey(Key.D, distance > 0f);
            await Step();
        }
        throw new InvalidOperationException($"real-input walk timeout target={target} actual={_player.GlobalPosition}");
    }

    private async Task Until(string predicate, int maximumFrames, Func<bool> satisfied)
    {
        for (int index = 0; index < maximumFrames; index++)
        {
            if (satisfied()) { Record("observed", new { predicate }); return; }
            await Step();
        }
        throw new InvalidOperationException($"Timeout ({maximumFrames} physics frames): {predicate}");
    }

    private async Task Frames(int count)
    {
        for (int index = 0; index < count; index++) await Step();
    }

    private async Task Step(Action beforePhysics = null)
    {
        // SceneTree.physics_frame is emitted BEFORE node _PhysicsProcess callbacks.
        // Stage real events here, then observe after node physics. This does not promise
        // same-tick one-shot input consumption; edge assertions must cross the next
        // sampling boundary demonstrated by the retained Space/LMB input evidence.
        await ToSignal(this, SignalName.PhysicsFrame);
        _frames++;
        beforePhysics?.Invoke();
        if (_player != null) _stepStartPosition = _player.GlobalPosition;
        FlushWantedInputs();
        await ToSignal(this, SignalName.ProcessFrame);
        if (_motor != null && _motor.AcceptedJumpCount != _lastAcceptedJumps)
        {
            Record("accepted_jump_takeoff", new { physicsFrame = _frames, fixedFpsSeconds = _frames / 60d,
                inputStartPosition = _stepStartPosition.ToString(), afterPhysicsPosition = _player.GlobalPosition.ToString(),
                acceptedJumpCount = _motor.AcceptedJumpCount, stage = _stage });
            _lastAcceptedJumps = _motor.AcceptedJumpCount;
        }
        EnsureLiveAndContracts();
        UpdateHud();
        if (_pendingSnapshot != null)
        {
            string name = _pendingSnapshot;
            _pendingSnapshot = null;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string path = ProofPath(Path.Combine("frames", $"{_snapshots++:D3}-{_frames:D4}.png"));
            Require(!File.Exists(path), "proof snapshot must not overwrite an earlier frame");
            using Image frame = Root.GetTexture().GetImage();
            Require(frame.SavePng(path) == Error.Ok, "native rendered frame must be saved successfully");
            Record("snapshot", new { name, path });
        }
        if (_frames % 60 == 0) Record("sample", StateSnapshot());
    }

    private void EnsureLiveAndContracts()
    {
        Require(_frames <= MaximumPhysicsFrames, "bounded proof exceeded 60 seconds at fixed 60 fps");
        if (!_setup) return;
        Require(GodotObject.IsInstanceValid(_player) && _level.Player == _player && _player.IsInsideTree() &&
            _health.CurrentHealth > 0 && float.IsFinite(_player.GlobalPosition.X) &&
            float.IsFinite(_player.GlobalPosition.Y) && _flow.State == PrologueGameFlowState.Playing &&
            _recoveriesRequested == 0 && _recoveriesCompleted == 0,
            "same living player remains in Playing with finite position and no recovery");
        if (_gate != null)
            Require(GodotObject.IsInstanceValid(_gate) && _gate.PlayerBody == _player &&
                _gate.IsOpen == _gate.DoorCollision.Disabled && _gate.Frame.Traversable == _gate.IsOpen &&
                !_gate.Frame.HazardActive, "committed gate collision, frame and actual-player binding agree");
        if (_encounterDefeated)
            Require(GodotObject.IsInstanceValid(_group) && _group.IsComplete && _group.LiveActorCount == 0 &&
                !_group.SpawnSuppressed && _rustDeaths == 1 && _skitterDeaths == 1,
                "actual Archive defeat remains stable throughout the gate and old route");
        if (_checkGateDamage) Require(_damageEvents == _damageBeforeGate, "optional gate cannot deal damage");
        if (_roofProbeActive)
        {
            Require(_frames - _roofProbeStartFrame <= 480,
                "bounded single-attempt roof-claw proof exceeded eight seconds at fixed 60 fps");
            Require(!_gate.IsOpen && !_gate.DoorCollision.Disabled && !_gate.Frame.Traversable &&
                !_supply.IsSpent && !_supply.OverlapsBody(_player) &&
                _health.CurrentHealth == _roofProbeHealth && _healEvents == _roofProbeHeals &&
                _motor.AcceptedJumpCount == _roofProbeJumps && _motor.AcceptedDashCount == _roofProbeDashes,
                "roof approach keeps the actual gate closed/reward untouched/HP unchanged, with no ordinary jump or dash");
            CollisionShape2D collider = _player.GetNode<CollisionShape2D>("BodyCollider");
            Require(collider.Shape is CapsuleShape2D && collider.GlobalScale.IsEqualApprox(Vector2.One) &&
                Mathf.IsZeroApprox(collider.GlobalRotation), "roof approach observes the actual upright unscaled player capsule");
            var capsule = (CapsuleShape2D)collider.Shape;
            Vector2 extent = new(capsule.Radius, capsule.Height * .5f);
            Rect2 capsuleBounds = new(collider.GlobalPosition - extent, extent * 2f);
            Require(!capsuleBounds.Intersects(_gate.InteriorWorldRect),
                "actual capsule's conservative bounds never enter the closed alcove during the roof trajectory");
        }
        if (_routeActive)
        {
            Require(_motor.AcceptedJumpCount <= _routeJumps + 1 && _motor.AcceptedDashCount == _routeDashes &&
                _claw.CurrentFrame.CastId == _routeCastId && _claw.CurrentFrame.NextCastId == _routeNextCastId &&
                _claw.CurrentFrame.State == _routeClawState && !_motor.HasGrappleReleaseMomentum,
                "old route never borrows a second jump, dash, claw cast, or grapple momentum");
            if (_motor.AcceptedJumpCount > _routeJumps && HasFloorContact(_archiveFloor)) _routeArchiveRetouchFrames++;
        }
        string floor = FloorContacts();
        if (floor != _lastFloor)
        {
            _lastFloor = floor;
            Record("floor_transition", new { floor, position = _player.GlobalPosition.ToString(),
                jumpCount = _motor.AcceptedJumpCount, stage = _stage });
        }
    }

    private void WantKey(Key key, bool pressed) => _wantedKeys[key] = pressed;
    private void WantButton(MouseButton button, bool pressed) => _wantedButtons[button] = pressed;
    private void WantNeutral()
    {
        foreach (Key key in Keys) WantKey(key, false);
        foreach (MouseButton button in Buttons) WantButton(button, false);
    }

    private void FlushWantedInputs()
    {
        foreach (Key key in Keys)
            if (_wantedKeys.GetValueOrDefault(key) != _heldKeys.GetValueOrDefault(key))
                SendKeyImmediate(key, _wantedKeys.GetValueOrDefault(key));
        foreach (MouseButton button in Buttons)
            if (_wantedButtons.GetValueOrDefault(button) != _heldButtons.GetValueOrDefault(button))
                SendButtonImmediate(button, _wantedButtons.GetValueOrDefault(button));
    }

    private void SendKeyImmediate(Key key, bool pressed)
    {
        _heldKeys[key] = pressed; // Set BEFORE ParseInputEvent so a thrown press still gets released.
        using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed, Echo = false };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
        Record("input", new { key = key.ToString(), pressed, via = "InputEventKey+InputMap", timing = "before_node_physics" });
    }

    private void SendButtonImmediate(MouseButton button, bool pressed)
    {
        _heldButtons[button] = pressed; // Same fail-safe ordering as keys.
        Vector2 pointer = _player?.GetViewport().GetMousePosition() ?? Vector2.Zero;
        using var input = new InputEventMouseButton { ButtonIndex = button, Pressed = pressed,
            Position = pointer, GlobalPosition = pointer };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
        Record("input", new { mouse = button.ToString(), pressed, via = "InputEventMouseButton+InputMap", timing = "before_node_physics" });
    }

    private void Aim(Vector2 world)
    {
        Vector2 pointer = _player.GetViewport().GetFinalTransform() *
            (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(world));
        using var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
        _player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
    }

    private bool ReleaseAllImmediate()
    {
        bool success = true;
        foreach (Key key in Keys)
        {
            try { if (_heldKeys.GetValueOrDefault(key)) SendKeyImmediate(key, false); }
            catch (Exception error) { success = false; GD.PushError($"TASK16B_PROOF_RELEASE_FAIL key={key}: {error}"); }
        }
        foreach (MouseButton button in Buttons)
        {
            try { if (_heldButtons.GetValueOrDefault(button)) SendButtonImmediate(button, false); }
            catch (Exception error) { success = false; GD.PushError($"TASK16B_PROOF_RELEASE_FAIL mouse={button}: {error}"); }
        }
        WantNeutral();
        return success;
    }

    public override void _Finalize() => ReleaseAllImmediate();

    private StaticBody2D Body(string name) => _sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
    private bool HasFloorContact(StaticBody2D support)
    {
        if (!_player.IsOnFloor()) return false;
        for (int index = 0; index < _player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = _player.GetSlideCollision(index);
            if (collision.GetCollider() == support && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private bool HasSideContact(StaticBody2D support)
    {
        for (int index = 0; index < _player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = _player.GetSlideCollision(index);
            if (collision.GetCollider() == support && MathF.Abs(collision.GetNormal().X) > .7f) return true;
        }
        return false;
    }

    private string FloorContacts()
    {
        if (!_player.IsOnFloor()) return "airborne";
        var names = new List<string>();
        for (int index = 0; index < _player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = _player.GetSlideCollision(index);
            if (collision.GetNormal().Y < -.7f && collision.GetCollider() is Node node)
                names.Add(node.Name.ToString());
        }
        return string.Join("|", names);
    }

    private static (float Left, float Right, float Top) Bounds(StaticBody2D body)
    {
        var shape = (RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape;
        return (body.GlobalPosition.X - shape.Size.X / 2f, body.GlobalPosition.X + shape.Size.X / 2f,
            body.GlobalPosition.Y - shape.Size.Y / 2f);
    }

    private void SetWindowAndHud()
    {
        Root.ContentScaleSize = WindowSize;
        Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        Root.Size = WindowSize;
        DisplayServer.WindowSetSize(WindowSize);
        var hud = new CanvasLayer { Name = "Task16BGeneratedProofLabels", Layer = 50 };
        hud.AddChild(new ColorRect { Color = new Color(.025f, .045f, .065f, .91f),
            Size = new Vector2(1280f, 104f), MouseFilter = Control.MouseFilterEnum.Ignore });
        hud.AddChild(Label("GENERATED ARCHIVE · ONE INITIAL REGIONAL SETUP · SCRIPTED INPUT · FIXED 60 FPS / SILENT",
            new Vector2(18f, 9f), 17, new Color("e3c990")));
        _stageLabel = Label("Setup", new Vector2(18f, 38f), 19, new Color("fff1d0"));
        _stateLabel = Label("", new Vector2(18f, 73f), 15, new Color("c6d9df"));
        hud.AddChild(_stageLabel);
        hud.AddChild(_stateLabel);
        _level.AddChild(hud);
    }

    private static Label Label(string text, Vector2 position, int size, Color color)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        return label;
    }

    private void Stage(string text)
    {
        _stage = text;
        _pendingSnapshot = text;
        if (_stageLabel != null) _stageLabel.Text = text;
        Record("stage", new { text });
        GD.Print($"TASK16B_PROOF_STAGE frame={_frames} {text}");
    }

    private void UpdateHud()
    {
        if (_stateLabel == null || _health == null) return;
        _stateLabel.Text = $"Frame {_frames}   HP {_health.CurrentHealth}/{_health.MaxHealth}   " +
            $"Rust/Skitter defeats {_rustDeaths}/{_skitterDeaths}   " +
            $"Door {(_gate == null ? "mounting" : _gate.IsOpen ? "OPEN" : "CLOSED")}   " +
            $"Reward {(_supply?.IsSpent == true ? "SPENT" : "AVAILABLE")}   " +
            $"Jumps {_motor.AcceptedJumpCount}   Recoveries {_recoveriesCompleted}";
    }

    private object StateSnapshot() => new
    {
        stage = _stage, player = _player?.GlobalPosition.ToString(), velocity = _player?.Velocity.ToString(),
        health = _health?.CurrentHealth, grounded = _player?.IsOnFloor(), gateOpen = _gate?.IsOpen,
        shapeDisabled = _gate?.DoorCollision.Disabled, traversable = _gate?.Frame.Traversable,
        warning = _gate?.IsDangerTelegraphActive, rewardSpent = _supply?.IsSpent,
        groupComplete = _group?.IsComplete, batch = _group?.CurrentBatch, actors = _group?.LiveActorCount,
        jumps = _motor?.AcceptedJumpCount, dashes = _motor?.AcceptedDashCount,
        clawState = _claw?.CurrentFrame.State.ToString(), clawFailure = _claw?.CurrentFrame.Failure.ToString(),
        clawCast = _claw?.CurrentFrame.CastId, clawNextCast = _claw?.CurrentFrame.NextCastId,
        clawTarget = _claw?.CurrentFrame.LockedTargetRuntimeId, clawAnchor = _claw?.CurrentFrame.AnchorPoint.ToString(),
        clawDock = _claw?.CurrentFrame.DockPoint.ToString(), clawArrived = _claw?.CurrentFrame.Arrived,
        clawRelease = _claw?.CurrentFrame.ReleaseKind.ToString(), roofClawProof = _roofClawProofComplete,
        recoveriesRequested = _recoveriesRequested, recoveriesCompleted = _recoveriesCompleted
    };

    private void Record(string kind, object data) => _metadata?.WriteLine(JsonSerializer.Serialize(new { kind, frame = _frames, data }));

    private void ValidateInvocation()
    {
        string[] args = OS.GetCmdlineUserArgs();
        Require(args.Length == 3 && args.Count(arg => arg == "--archive-generated-proof") == 1 &&
            args.Count(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
            args.Count(arg => arg.StartsWith("--proof-output-directory=", StringComparison.Ordinal)) == 1,
            "proof accepts only --archive-generated-proof, --case-storage-root and --proof-output-directory");
        string storage = Absolute(args.Single(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal))["--case-storage-root=".Length..]);
        _proofDirectory = Absolute(args.Single(arg => arg.StartsWith("--proof-output-directory=", StringComparison.Ordinal))["--proof-output-directory=".Length..]);
        string caseDirectory = Path.GetDirectoryName(storage) ?? "";
        string evidence = Absolute("E:/OurGame/Knight/.evidence/september09-continuous-canyon/task16b");
        string project = Absolute(ProjectSettings.GlobalizePath("res://"));
        string expectedProject = Absolute("E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555");
        string temp = Path.Combine(caseDirectory, "unit-temp");
        Require(Directory.Exists(storage) && Directory.Exists(temp) && Path.GetFileName(storage) == "storage" &&
            SamePath(Path.GetDirectoryName(caseDirectory) ?? "", evidence) &&
            SamePath(_proofDirectory, Path.Combine(caseDirectory, "proof")) && SamePath(project, expectedProject) &&
            SamePath(Path.GetTempPath(), temp), "project, storage, proof or child temporary path escaped the dedicated Task16B case");
        foreach (string variable in new[] { "TMP", "TEMP" })
            Require(SamePath(Absolute(System.Environment.GetEnvironmentVariable(variable) ?? ""), temp),
                $"child {variable} escaped the dedicated proof case");
        foreach (string path in new[] { project, storage, temp, _proofDirectory }) AssertNoReparseAncestors(path);
        Require(DisplayServer.GetName() != "headless", "generated proof requires a normal display window");
        Require(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString() == "gl_compatibility",
            "proof must preserve the configured OpenGL compatibility renderer");
        string adapter = RenderingServer.GetVideoAdapterName();
        Require(!new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render" }.Any(
            value => adapter.Contains(value, StringComparison.OrdinalIgnoreCase)), "software renderer is not hardware proof");
        Require(AudioServer.GetDriverName() == "Dummy", "proof must use the actual Dummy audio driver");
        string movie = Engine.GetWriteMoviePath();
        Require(!string.IsNullOrEmpty(movie) && SamePath(Absolute(movie), Path.Combine(_proofDirectory, "raw.avi")),
            "movie must be the exact dedicated proof/raw.avi, never a project or userdata path");
    }

    private string ProofPath(string relative)
    {
        string path = Absolute(Path.Combine(_proofDirectory, relative));
        Require(path.StartsWith(_proofDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "proof write escaped its dedicated output directory");
        AssertNoReparseAncestors(path);
        return path;
    }

    private static string Absolute(string raw)
    {
        Require(Path.IsPathFullyQualified(raw), "case paths must be fully qualified");
        return Path.GetFullPath(raw).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(Absolute(left), Absolute(right), StringComparison.OrdinalIgnoreCase);

    private static void AssertNoReparseAncestors(string path)
    {
        if (File.Exists(path)) Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0,
            "proof file cannot be a reparse point");
        for (DirectoryInfo current = new(path); current != null; current = current.Parent)
            if (current.Exists) Require((current.Attributes & FileAttributes.ReparsePoint) == 0,
                $"reparse ancestor rejected: {current.FullName}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
