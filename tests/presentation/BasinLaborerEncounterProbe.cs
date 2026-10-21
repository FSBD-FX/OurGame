using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// Generated-main observation from the authored entry shelf. Only the pre-tree spawn is
// positioned directly; subsequent movement uses InputMap, with live AI and combat.
public partial class BasinLaborerEncounterProbe : SceneTree
{
    private CharacterBody2D _player;
    private BellBackLaborer _laborer;
    private HealthComponent _health;
    private Hitbox _hitbox;
    private Hurtbox _playerHurtbox;
    private StaticBody2D _upper;
    private CollisionShape2D _attackCollider;
    private string _output;
    private bool _upperObservation;
    private bool _groundObservation;
    private readonly HashSet<ulong> _upperStarts = new();
    private readonly HashSet<ulong> _groundActive = new();
    private readonly HashSet<ulong> _groundRecovered = new();
    private readonly HashSet<string> _captures = new();
    private AttackPhase _lastPhase;
    private ulong _lastId, _lastObservedTick;
    private int _recoveries, _upperConnections, _upperDamage, _outsideUpperDamage, _groundConnections;
    private int _groundFloorSamples, _groundCeilingContacts;
    private float _spawnEnemyX, _groundMaxVerticalSpeed;
    private StaticBody2D _ground;
    private Vector2 _enemyMin = new(float.PositiveInfinity, float.PositiveInfinity);
    private Vector2 _enemyMax = new(float.NegativeInfinity, float.NegativeInfinity);

    public override async void _Initialize()
    {
        PrologueLevelController level = null;
        int result = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Display required.");
            _output = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--output="))?[9..];
            if (_output != null)
            {
                if (!Path.IsPathFullyQualified(_output)) throw new ArgumentException("Absolute output required.");
                _output = Path.GetFullPath(_output);
                string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
                if ((_output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(project, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Evidence must be outside the project.");
                Directory.CreateDirectory(_output);
            }
            Release();
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            _player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _player.Position = new Vector2(2380, 1568);
            _health = _player.GetNode<HealthComponent>("Health");
            _playerHurtbox = _player.GetNode<Hurtbox>("Hurtbox");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            string isolated = $"user://basin-laborer-probe/{Guid.NewGuid():N}";
            flow.AutomationAutoStart = true;
            flow.SavePath = isolated + "/save.json";
            flow.SettingsPath = isolated + "/settings.json";
            _health.Damaged += OnDamaged;
            level.RecoveryRequested += OnRecovery;
            Root.AddChild(level);
            await Frames(8);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, level.CurrentSector.Value, "actual Basin fixture");
            _laborer = level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
            _spawnEnemyX = _laborer.GlobalPosition.X;
            _hitbox = _laborer.GetNode<Hitbox>("AttackHitbox");
            _attackCollider = _hitbox.GetNode<CollisionShape2D>("AttackCollider");
            _laborer.AttackFrameChanged += OnAttack;
            _hitbox.Connected += OnConnected;
            level.GetNode<PrologueCompletionController>("Completion").RestoreSession(PrologueObjective.LearnShield);
            _upper = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/SalvageFloor");
            StaticBody2D entry = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/EntryCatch");
            await Settle(entry, 90);
            Capture("entry");
            Send(InputActions.MoveRight, true);
            await Until(() => _player.GlobalPosition.X >= 2585, 180, "walk to upper platform");
            Send(InputActions.MoveRight, false);
            await Settle(_upper, 180);
            _upperObservation = true;
            Capture("upper-start");
            await Frames(300);
            Capture("upper-end");
            GD.Print($"LABORER_UPPER_RESULT starts={_upperStarts.Count} connections={_upperConnections} damage={_upperDamage} health={_health.CurrentHealth} enemy_min={_enemyMin} enemy_max={_enemyMax}");
            _upperObservation = false;

            bool waitForApproach = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--wait-for-approach");
            if (waitForApproach || Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--ground-control"))
            {
                Send(InputActions.MoveRight, true);
                await Until(() => _player.GlobalPosition.X >= 2855, 180, "leave upper right edge before the sector boundary");
                Send(InputActions.MoveRight, false);
                StaticBody2D floor = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BasinFloor");
                _ground = floor;
                await Settle(floor, 120);
                _groundObservation = true;
                Capture("ground-start");
                if (!waitForApproach)
                {
                    // Close-contact control is distinct from waiting for the AI to approach.
                    Send(InputActions.MoveLeft, true);
                    await Until(() => _player.GlobalPosition.X - _laborer.GlobalPosition.X <= 60,
                        180, "walk into the enemy's existing same-floor attack range");
                    Send(InputActions.MoveLeft, false);
                }
                await Until(() => _groundRecovered.Count >= 2 || _recoveries > 0, 420,
                    waitForApproach ? "live enemy approaches the stationary player and completes two sweeps" :
                    "same-floor enemy completes two sweeps when approached");
                Capture("ground-end");
                TestAssert.True(_groundActive.Count >= 2, "ground control observes two real attack IDs");
                TestAssert.True(_groundConnections >= 2, "two actual laborer contacts reach the unshielded player on the same floor");
                if (waitForApproach)
                {
                    TestAssert.True(_laborer.GlobalPosition.X - _spawnEnemyX >= 100f,
                        "actual enemy traverses at least 100px from its authored position");
                    TestAssert.True(_groundFloorSamples >= 12 && _groundCeilingContacts == 0,
                        "enemy has real ground support without being pressed into the upper platform");
                    GD.Print($"LABORER_APPROACH_RESULT displacement={_laborer.GlobalPosition.X - _spawnEnemyX} floor_samples={_groundFloorSamples} ceiling_contacts={_groundCeilingContacts} max_abs_vy={_groundMaxVerticalSpeed} player={_player.GlobalPosition}");
                }
            }
            TestAssert.Equal(0, _recoveries, "observation uses no recovery");
            TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount, "observation uses no jump");
            TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedDashCount, "observation uses no dash");
            TestAssert.Equal(0, _upperStarts.Count, "enemy must not start new attacks through the solid upper platform");
            TestAssert.Equal(0, _upperDamage, "upper-platform observation takes no damage");
            GD.Print($"BASIN_LABORER_OK upper_new_attacks=0 upper_damage=0 ground_active={_groundActive.Count} ground_connections={_groundConnections} outside_upper_damage={_outsideUpperDamage} recoveries=0");
            result = 0;
        }
        catch (Exception exception) { GD.PushError("BASIN_LABORER_FAILED: " + exception); }
        finally
        {
            Release();
            if (GodotObject.IsInstanceValid(_laborer)) _laborer.AttackFrameChanged -= OnAttack;
            if (GodotObject.IsInstanceValid(_hitbox)) _hitbox.Connected -= OnConnected;
            if (GodotObject.IsInstanceValid(_health)) _health.Damaged -= OnDamaged;
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryRequested -= OnRecovery;
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
                else level.Free();
            }
            Quit(result);
        }
    }

    private void OnAttack(AttackPresentationFrame frame)
    {
        var timeline = frame.Timeline;
        if (timeline.Phase == AttackPhase.Telegraph && timeline.AttackId != _lastId && _upperObservation)
            _upperStarts.Add(timeline.AttackId);
        if (timeline.Phase == AttackPhase.Active && _groundObservation &&
            _player.GlobalPosition.Y > 1980) _groundActive.Add(timeline.AttackId);
        if (timeline.Phase == AttackPhase.Recovery && _groundActive.Contains(timeline.AttackId))
            _groundRecovered.Add(timeline.AttackId);
        if (timeline.Phase != _lastPhase || timeline.AttackId != _lastId)
            GD.Print($"LABORER_PHASE physics={Engine.GetPhysicsFrames()} id={timeline.AttackId} phase={timeline.Phase} upper={_upperObservation} player={_player.GlobalPosition} enemy={_laborer.GlobalPosition} facing={_laborer.Facing} published_origin={frame.Shape.Origin}");
        _lastId = timeline.AttackId;
        _lastPhase = timeline.Phase;
    }
    private void OnConnected(Hurtbox target)
    {
        if (target != _playerHurtbox) return;
        if (_upperObservation) _upperConnections++;
        if (_groundObservation) _groundConnections++;
        GD.Print($"LABORER_CONNECTED physics={Engine.GetPhysicsFrames()} id={_lastId} upper={_upperObservation} hp={_health.CurrentHealth} player={_player.GlobalPosition} enemy={_laborer.GlobalPosition}");
    }
    private void OnDamaged(int remaining, Vector2 impulse)
    {
        if (_upperObservation) _upperDamage++; else _outsideUpperDamage++;
        GD.Print($"LABORER_PLAYER_DAMAGE physics={Engine.GetPhysicsFrames()} upper={_upperObservation} hp={remaining} impulse={impulse}");
    }
    private void OnRecovery(string reason) => _recoveries++;
    private void Observe()
    {
        ulong tick = Engine.GetPhysicsFrames();
        if (tick == _lastObservedTick || !GodotObject.IsInstanceValid(_laborer)) return;
        _lastObservedTick = tick;
        if (_groundObservation)
        {
            if (IsSupported(_laborer, _ground)) _groundFloorSamples++;
            _groundMaxVerticalSpeed = Mathf.Max(_groundMaxVerticalSpeed, Mathf.Abs(_laborer.Velocity.Y));
            for (int i = 0; i < _laborer.GetSlideCollisionCount(); i++)
            {
                var collision = _laborer.GetSlideCollision(i);
                if (collision.GetCollider() == _upper && collision.GetNormal().Y > .7f) _groundCeilingContacts++;
            }
        }
        if (_upperObservation)
        {
            _enemyMin = _enemyMin.Min(_laborer.GlobalPosition);
            _enemyMax = _enemyMax.Max(_laborer.GlobalPosition);
            TestAssert.True(IsSupported(_player, _upper), "player remains on upper platform while observing");
        }
        var timeline = _laborer.CurrentFrame.AttackTimeline;
        string stage = (_upperObservation ? "upper-" : "approach-") + timeline.Phase;
        if (timeline.Phase == AttackPhase.Active || timeline.Phase == AttackPhase.Telegraph && timeline.PhaseRemainingSeconds < .1f)
        {
            if (_captures.Add(stage)) Capture(stage);
        }
    }
    private void Capture(string stage)
    {
        string contacts = string.Join(';', Enumerable.Range(0, _laborer.GetSlideCollisionCount()).Select(i =>
        {
            var c = _laborer.GetSlideCollision(i);
            return $"{(c.GetCollider() as Node)?.Name}:{c.GetNormal()}";
        }));
        string polygon = _attackCollider.Shape is ConvexPolygonShape2D shape
            ? string.Join(';', shape.Points.Select(p => _attackCollider.GlobalTransform * p)) : "none";
        GD.Print($"LABORER_SAMPLE stage={stage} physics={Engine.GetPhysicsFrames()} player={_player.GlobalPosition} hp={_health.CurrentHealth} enemy={_laborer.GlobalPosition} velocity={_laborer.Velocity} on_floor={_laborer.IsOnFloor()} contacts={contacts} state={_laborer.CurrentState} monitor={_hitbox.Monitoring} actual_polygon={polygon}");
        if (_output == null) return;
        using Image frame = Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, frame.SavePng(Path.Combine(_output, stage + ".png")), "save observation frame");
    }
    private async Task Frames(int count)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)count;
        while (Engine.GetPhysicsFrames() < end)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Observe();
        }
    }
    private async Task Until(Func<bool> condition, int budget, string stage)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (!condition() && Engine.GetPhysicsFrames() < end) await Frames(1);
        GD.Print($"LABORER_ROUTE stage={stage} physics={Engine.GetPhysicsFrames()} player={_player.GlobalPosition}");
        if (!condition()) Capture("route-failed");
        TestAssert.True(condition(), stage);
    }
    private async Task Settle(StaticBody2D support, int budget)
    {
        int stable = 0;
        ulong previous = 0;
        await Until(() =>
        {
            ulong tick = Engine.GetPhysicsFrames();
            if (tick == previous) return stable >= 12;
            previous = tick;
            stable = IsSupported(_player, support) ? stable + 1 : 0;
            return stable >= 12;
        }, budget, "supported in 12 distinct physics samples on " + support.Name);
    }
    private static bool IsSupported(CharacterBody2D body, StaticBody2D support)
    {
        if (!body.IsOnFloor()) return false;
        for (int i = 0; i < body.GetSlideCollisionCount(); i++)
        {
            var c = body.GetSlideCollision(i);
            if (c.GetCollider() == support && c.GetNormal().Y < -.7f) return true;
        }
        return false;
    }
    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static void Release()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.Jump,
            InputActions.Dash, InputActions.PrimaryAction, InputActions.Shield, InputActions.SelectGrappleMode,
            InputActions.SelectAttackMode, InputActions.MoveUp, InputActions.MoveDown }) Send(action, false);
    }
}
