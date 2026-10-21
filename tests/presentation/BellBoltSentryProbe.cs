using System;
using System.Linq;
using Godot;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Presentation;

// Standalone enemy demonstration. This is intentionally not the normal prologue or a placement claim.
public partial class BellBoltSentryProbe : SceneTree
{
    private const int TimeoutFrame = 520;
    private Node2D _demo;
    private CharacterBody2D _player;
    private BellBoltSentry _sentry;
    private HealthComponent _playerHealth;
    private HealthComponent _sentryHealth;
    private PlayerShieldController _shield;
    private PlayerCombat _combat;
    private Label _status;
    private int _frame;
    private int _stage;
    private int _stageStart;
    private int _lastAttackFrame = -100;
    private bool _ordinaryHit;
    private bool _guarded;
    private bool _threadStunned;
    private bool _defeated;
    private bool _threadInputSent;
    private int _threadInputFrame;
    private int _threadStunFrame;
    private ulong _threadAttackId;

    public override void _Initialize()
    {
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Bell-Bolt proof requires a real renderer.");
            ReleaseAll();
            _demo = new Node2D { Name = "BellBoltSentryEnemyDemo" };
            _demo.AddChild(new BellBoltDemoBackdrop { Name = "Backdrop", ZIndex = -10 });
            AddWorldBody(_demo, "BroadFloor", new Vector2(600f, 420f), new Vector2(1120f, 40f));
            AddWorldBody(_demo, "CoverColumn", new Vector2(900f, 325f), new Vector2(44f, 150f));

            _player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            _player.Name = "RealPlayer";
            _player.Position = new Vector2(300f, 360f);
            _player.AddToGroup("player");
            _sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn")
                .Instantiate<BellBoltSentry>();
            _sentry.Name = "BellBoltSentryDemoOnly";
            _sentry.Position = new Vector2(560f, 360f);
            _demo.AddChild(_player);
            _demo.AddChild(_sentry);

            var canvas = new CanvasLayer { Name = "DemoHud" };
            _status = new Label
            {
                Name = "Status",
                Position = new Vector2(28f, 24f),
                Size = new Vector2(900f, 100f)
            };
            _status.AddThemeFontSizeOverride("font_size", 24);
            _status.AddThemeColorOverride("font_color", new Color("f5df9a"));
            _status.AddThemeColorOverride("font_shadow_color", Colors.Black);
            _status.AddThemeConstantOverride("shadow_offset_x", 2);
            _status.AddThemeConstantOverride("shadow_offset_y", 2);
            canvas.AddChild(_status);
            _demo.AddChild(canvas);
            Root.AddChild(_demo);

            _playerHealth = _player.GetNode<HealthComponent>("Health");
            _playerHealth.InvulnerabilitySeconds = 0f;
            _sentryHealth = _sentry.GetNode<HealthComponent>("Health");
            _shield = _player.GetNode<PlayerShieldController>("Shield");
            _combat = _player.GetNode<PlayerCombat>("Combat");
            _playerHealth.Damaged += OnPlayerDamaged;
            _shield.ShieldResolved += OnShieldResolved;
            _sentry.Defeated += OnDefeated;
            SetStage(0, "1/4  Ordinary shot — observe full gold warning, red lock, then one damage");
            GD.Print("BELL_BOLT_DEMO_KIND=standalone_enemy_demo existing_world_spawns_unchanged=true");
        }
        catch (Exception exception)
        {
            GD.PushError("BELL_BOLT_PROBE_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        _frame++;
        try
        {
            if (!GodotObject.IsInstanceValid(_player) || !GodotObject.IsInstanceValid(_sentry))
                throw new InvalidOperationException("demo actors disappeared before completion");
            AimAt(_sentry.GlobalPosition);
            DriveRealInput();
            if (_frame % 60 == 0)
                GD.Print($"BELL_BOLT_PROOF_SAMPLE frame={_frame} stage={_stage} player={_player.GlobalPosition} " +
                    $"sentry={_sentry.GlobalPosition} state={_sentry.CurrentFrame.State} " +
                    $"phase={_sentry.CurrentFrame.AttackTimeline.Phase} player_hp={_playerHealth.CurrentHealth}");
            _status.Text = StageText() +
                $"\nPlayer HP {_playerHealth.CurrentHealth}/5   Sentry HP {_sentryHealth.CurrentHealth}/2   " +
                $"State {_sentry.CurrentFrame.State}";
            if (_defeated && _frame - _stageStart >= 45)
            {
                ReleaseAll();
                GD.Print($"BELL_BOLT_SENTRY_PROOF_OK ordinary={_ordinaryHit} shield={_guarded} " +
                    $"thread={_threadStunned} defeated={_defeated} frame={_frame}");
                Quit(0);
            }
            else if (_frame >= TimeoutFrame)
                throw new InvalidOperationException($"timeout ordinary={_ordinaryHit} shield={_guarded} " +
                    $"thread={_threadStunned} defeated={_defeated} stage={_stage}");
        }
        catch (Exception exception)
        {
            ReleaseAll();
            GD.PushError($"BELL_BOLT_PROBE_FAIL frame={_frame}: {exception}");
            Quit(1);
        }
        return false;
    }

    public override void _Finalize()
    {
        ReleaseAll();
        if (GodotObject.IsInstanceValid(_playerHealth)) _playerHealth.Damaged -= OnPlayerDamaged;
        if (GodotObject.IsInstanceValid(_shield)) _shield.ShieldResolved -= OnShieldResolved;
        if (GodotObject.IsInstanceValid(_sentry)) _sentry.Defeated -= OnDefeated;
    }

    private void DriveRealInput()
    {
        if (_stage == 0 && _ordinaryHit)
        {
            Send(InputActions.Shield, true);
            SetStage(1, "2/4  RMB guard — correct-facing shield consumes the next bolt");
        }
        if (_stage == 1 && _guarded)
        {
            Send(InputActions.Shield, false);
            Send(InputActions.SelectGrappleMode, true);
            SetStage(2, "3/4  E + LMB — real grapple input interrupts the warning with stun");
        }
        if (_stage == 2)
        {
            int local = _frame - _stageStart;
            if (local == 2) Send(InputActions.SelectGrappleMode, false);
            if (!_threadInputSent && _sentry.CurrentFrame.State == BellBoltSentryState.Telegraph)
            {
                _threadInputSent = true;
                _threadInputFrame = _frame;
                _threadAttackId = _sentry.CurrentFrame.AttackTimeline.AttackId;
                int projectilesBefore = _sentry.GetChildren().OfType<BellBoltProjectile>().Count();
                if (_threadAttackId == 0 || projectilesBefore != 0)
                    throw new InvalidOperationException("thread proof did not begin on a clean live telegraph");
                GD.Print($"BELL_BOLT_PROOF_THREAD_BEFORE frame={_frame} phase=Telegraph " +
                    $"attack_id={_threadAttackId} projectiles={projectilesBefore}");
                Send(InputActions.PrimaryAction, true);
            }
            if (_threadInputSent && _frame - _threadInputFrame == 2)
                Send(InputActions.PrimaryAction, false);
            if (_threadInputSent && _sentry.GetChildren().OfType<BellBoltProjectile>().Any())
                throw new InvalidOperationException("interrupted telegraph emitted a projectile");
            if (_threadInputSent && !_threadStunned &&
                _sentry.CurrentFrame.State == BellBoltSentryState.Stunned)
            {
                _threadStunned = true;
                _threadStunFrame = _frame;
                GD.Print($"BELL_BOLT_PROOF_THREAD_STUNNED frame={_frame} prior_phase=Telegraph " +
                    $"attack_id={_threadAttackId} phase={_sentry.CurrentFrame.AttackTimeline.Phase} projectiles=0");
            }
            if (_threadStunned && _frame - _threadStunFrame >= 30)
            {
                Send(InputActions.SelectAttackMode, true);
                Send(InputActions.MoveRight, true);
                SetStage(3, "4/4  Q + LMB — close distance and land two real attacks to defeat");
            }
        }
        if (_stage != 3) return;
        int attackLocal = _frame - _stageStart;
        if (attackLocal == 2) Send(InputActions.SelectAttackMode, false);
        float distance = _sentry.GlobalPosition.X - _player.GlobalPosition.X;
        if (distance > 66f)
        {
            Send(InputActions.MoveRight, true);
            return;
        }
        Send(InputActions.MoveRight, false);
        if (!_defeated && !_combat.IsAttacking && _frame - _lastAttackFrame >= 20)
        {
            Send(InputActions.PrimaryAction, true);
            _lastAttackFrame = _frame;
        }
        else if (_frame - _lastAttackFrame == 2)
        {
            Send(InputActions.PrimaryAction, false);
        }
    }

    private void OnPlayerDamaged(int remaining, Vector2 impulse)
    {
        if (_stage != 0) return;
        _ordinaryHit = true;
        GD.Print($"BELL_BOLT_PROOF_ORDINARY_HIT frame={_frame} remaining={remaining} impulse={impulse}");
    }

    private void OnShieldResolved(PlayerShieldResult result, ulong transition)
    {
        if (_stage != 1 || !result.Guarded || result.DamageAmount != 0) return;
        _guarded = true;
        GD.Print($"BELL_BOLT_PROOF_SHIELD_BLOCK frame={_frame} transition={transition}");
    }

    private void OnDefeated()
    {
        _defeated = true;
        ReleaseAll();
        SetStage(4, "DEMO COMPLETE — ranged warning, guard, thread counterplay, real defeat");
        GD.Print($"BELL_BOLT_PROOF_DEFEATED frame={_frame}");
    }

    private void SetStage(int stage, string text)
    {
        _stage = stage;
        _stageStart = _frame;
        _status.Text = text;
        GD.Print($"BELL_BOLT_PROOF_STAGE={stage} frame={_frame} text={text}");
    }

    private string StageText() => _stage switch
    {
        0 => "1/4  Ordinary shot: full warning → one damage",
        1 => "2/4  Hold RMB: correct-facing shield interception",
        2 => "3/4  E then LMB: thread interruption and stun",
        3 => "4/4  Q then LMB: two real melee hits",
        _ => "DEMO COMPLETE — standalone Bell-Bolt Sentry"
    };

    private void AimAt(Vector2 world)
    {
        Vector2 pointer = _player.GetViewport().GetFinalTransform() *
            (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(world));
        var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
        _player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
    }

    private static void AddWorldBody(Node2D root, string name, Vector2 position, Vector2 size)
    {
        var body = new StaticBody2D
        {
            Name = name, Position = position,
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        body.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = size } });
        root.AddChild(body);
    }

    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseAll()
    {
        foreach (StringName action in new[]
        {
            InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown,
            InputActions.Jump, InputActions.Dash, InputActions.PrimaryAction, InputActions.Shield,
            InputActions.SelectAttackMode, InputActions.SelectGrappleMode
        }) Send(action, false);
    }
}

internal partial class BellBoltDemoBackdrop : Node2D
{
    public override void _Draw()
    {
        DrawRect(new Rect2(-200f, -200f, 1600f, 800f), new Color("151923"));
        DrawPolygon([new Vector2(-100f, 400f), new Vector2(1300f, 400f),
            new Vector2(1300f, 600f), new Vector2(-100f, 600f)], [new Color("342c29")]);
        for (int x = 40; x < 1240; x += 90)
            DrawLine(new Vector2(x, 400f), new Vector2(x + 35f, 438f), new Color("665039"), 4f, true);
        DrawRect(new Rect2(878f, 250f, 44f, 150f), new Color("493b37"));
        DrawLine(new Vector2(900f, 250f), new Vector2(900f, 400f), new Color("a77542"), 5f, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(430f, 185f), "BELL-BOLT SENTRY / ENEMY DEMO",
            HorizontalAlignment.Left, -1f, 28, new Color("d9bd68"));
        DrawString(ThemeDB.FallbackFont, new Vector2(430f, 215f),
            "Provisional native silhouette — not final art or prologue placement",
            HorizontalAlignment.Left, -1f, 17, new Color("a9a39a"));
    }
}
