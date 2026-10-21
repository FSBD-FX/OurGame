using System;
using Godot;
using Knight.Combat;
using Knight.Enemies.Skitter;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Presentation;

// Standalone functional-visual input probe; never mounts production persistence or a route catalog.
public partial class CanyonSkitterProbe : SceneTree
{
    private Node2D _demo;
    private CharacterBody2D _player;
    private CanyonSkitter _skitter;
    private HealthComponent _playerHealth;
    private HealthComponent _skitterHealth;
    private PlayerShieldController _shield;
    private PlayerCombat _combat;
    private Label _status;
    private int _frame;
    private int _stage;
    private int _stageStart;
    private int _lastAttack = -100;
    private bool _blocked;
    private bool _sawRetreat;
    private bool _pursued;
    private float _pursuitStartX;
    private bool _threadSent;
    private bool _stunned;
    private bool _defeated;

    public override void _Initialize()
    {
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Skitter playable proof requires a renderer.");
            ReleaseAll();
            _demo = new Node2D { Name = "CanyonSkitterStandaloneInputDemo" };
            _demo.AddChild(new SkitterDemoBackdrop { Name = "Backdrop", ZIndex = -10 });
            AddWorldBody(_demo, "BroadFloor", new Vector2(600f, 420f), new Vector2(1120f, 40f));
            _player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            _player.Name = "RealPlayer";
            _player.Position = new Vector2(650f, 364f);
            _player.AddToGroup("player");
            _skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
                .Instantiate<CanyonSkitter>();
            _skitter.Name = "CanyonSkitterDemoOnly";
            _skitter.Position = new Vector2(560f, 400f);
            _demo.AddChild(_player);
            _demo.AddChild(_skitter);
            var canvas = new CanvasLayer { Name = "DemoHud" };
            _status = new Label
            {
                Name = "Status", Position = new Vector2(28f, 24f), Size = new Vector2(1130f, 100f)
            };
            _status.AddThemeFontSizeOverride("font_size", 23);
            _status.AddThemeColorOverride("font_color", new Color("f5dfb1"));
            _status.AddThemeColorOverride("font_shadow_color", Colors.Black);
            _status.AddThemeConstantOverride("shadow_offset_x", 2);
            _status.AddThemeConstantOverride("shadow_offset_y", 2);
            canvas.AddChild(_status);
            _demo.AddChild(canvas);
            Root.AddChild(_demo);
            _playerHealth = _player.GetNode<HealthComponent>("Health");
            _skitterHealth = _skitter.GetNode<HealthComponent>("Health");
            _shield = _player.GetNode<PlayerShieldController>("Shield");
            _combat = _player.GetNode<PlayerCombat>("Combat");
            _shield.ShieldResolved += OnShieldResolved;
            _skitter.Defeated += OnDefeated;
            Send(InputActions.Shield, true);
            GD.Print("SKITTER_PROOF_KIND=standalone_enemy_demo world_placements_changed=false");
            SetStage(0, "1/4 Hold RMB toward the charge; read the six-leg crouch and bounded rush");
        }
        catch (Exception exception)
        {
            GD.PushError("SKITTER_PROBE_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        _frame++;
        try
        {
            if (!GodotObject.IsInstanceValid(_player) || !GodotObject.IsInstanceValid(_skitter))
                throw new InvalidOperationException("probe actor disappeared");
            AimAt(_skitter.GlobalPosition);
            DriveRealInput();
            if (_frame % 30 == 0)
                GD.Print($"SKITTER_PROOF_SAMPLE frame={_frame} stage={_stage} " +
                    $"player={_player.GlobalPosition} skitter={_skitter.GlobalPosition} " +
                    $"state={_skitter.CurrentFrame.State} phase={_skitter.CurrentFrame.AttackTimeline.Phase} " +
                    $"player_hp={_playerHealth.CurrentHealth} skitter_hp={_skitterHealth.CurrentHealth}");
            _status.Text = StageText() +
                $"\nPlayer HP {_playerHealth.CurrentHealth}/5   Skitter HP {_skitterHealth.CurrentHealth}/2   " +
                $"State {_skitter.CurrentFrame.State}   Frame {_frame}";
            if (_defeated && _frame - _stageStart >= 45)
            {
                ReleaseAll();
                GD.Print($"CANYON_SKITTER_PROOF_OK block={_blocked} retreat={_sawRetreat} " +
                    $"pursuit={_pursued} thread={_stunned} defeat={_defeated} frame={_frame}");
                Quit(0);
            }
            else if (_frame >= 650)
                throw new InvalidOperationException($"probe timeout: stage={_stage} block={_blocked} " +
                    $"retreat={_sawRetreat} pursuit={_pursued} thread={_stunned} defeat={_defeated}");
        }
        catch (Exception exception)
        {
            ReleaseAll();
            GD.PushError($"SKITTER_PROBE_FAIL frame={_frame}: {exception}");
            Quit(1);
        }
        return false;
    }

    public override void _Finalize()
    {
        ReleaseAll();
        if (GodotObject.IsInstanceValid(_shield)) _shield.ShieldResolved -= OnShieldResolved;
        if (GodotObject.IsInstanceValid(_skitter)) _skitter.Defeated -= OnDefeated;
    }

    private void DriveRealInput()
    {
        if (_stage == 0)
        {
            if (_blocked && _skitter.CurrentFrame.State == CanyonSkitterState.Retreat)
            {
                _sawRetreat = true;
                _pursuitStartX = _player.GlobalPosition.X;
                Send(InputActions.Shield, false);
                Send(InputActions.MoveLeft, true);
                SetStage(1, "2/4 The foe yields ground; pursue its vulnerable retreat");
            }
            return;
        }
        if (_stage == 1)
        {
            float covered = _pursuitStartX - _player.GlobalPosition.X;
            if (covered >= 25f)
            {
                Send(InputActions.MoveLeft, false);
                _pursued = true;
                GD.Print($"SKITTER_PROOF_PURSUIT frame={_frame} displacement={covered} " +
                    $"gap={_player.GlobalPosition.X - _skitter.GlobalPosition.X}");
                Send(InputActions.SelectGrappleMode, true);
                SetStage(2, "3/4 E then LMB interrupts the next warning, no W required");
            }
            return;
        }
        if (_stage == 2)
        {
            int local = _frame - _stageStart;
            if (local == 2) Send(InputActions.SelectGrappleMode, false);
            if (!_threadSent && _skitter.CurrentFrame.State == CanyonSkitterState.Telegraph)
            {
                _threadSent = true;
                GD.Print($"SKITTER_PROOF_THREAD_BEFORE frame={_frame} " +
                    $"attack_id={_skitter.CurrentFrame.AttackTimeline.AttackId} " +
                    $"phase={_skitter.CurrentFrame.AttackTimeline.Phase}");
                Send(InputActions.PrimaryAction, true);
            }
            if (_threadSent && local > 2) Send(InputActions.PrimaryAction, false);
            if (_threadSent && _skitter.CurrentFrame.State == CanyonSkitterState.Stunned)
            {
                _stunned = true;
                Send(InputActions.SelectAttackMode, true);
                SetStage(3, "4/4 Q then LMB: two normal melee hits defeat the light enemy");
                GD.Print($"SKITTER_PROOF_THREAD_STUNNED frame={_frame} " +
                    $"phase={_skitter.CurrentFrame.AttackTimeline.Phase}");
            }
            return;
        }
        if (_stage != 3) return;
        int attackLocal = _frame - _stageStart;
        if (attackLocal == 2) Send(InputActions.SelectAttackMode, false);
        float distance = _player.GlobalPosition.X - _skitter.GlobalPosition.X;
        if (distance < 43f)
        {
            Send(InputActions.MoveLeft, false);
            Send(InputActions.MoveRight, true);
            return;
        }
        Send(InputActions.MoveRight, false);
        if (distance > 64f)
        {
            Send(InputActions.MoveLeft, true);
            return;
        }
        Send(InputActions.MoveLeft, false);
        if (!_defeated && !_combat.IsAttacking && _frame - _lastAttack >= 22)
        {
            Send(InputActions.PrimaryAction, true);
            _lastAttack = _frame;
        }
        else if (_frame - _lastAttack == 2) Send(InputActions.PrimaryAction, false);
    }

    private void OnShieldResolved(PlayerShieldResult result, ulong transition)
    {
        if (_stage != 0 || !result.Guarded || result.DamageAmount != 0) return;
        _blocked = true;
        GD.Print($"SKITTER_PROOF_BLOCK frame={_frame} transition={transition} " +
            $"player_hp={_playerHealth.CurrentHealth}");
    }

    private void OnDefeated()
    {
        _defeated = true;
        ReleaseAll();
        SetStage(4, "DEMO COMPLETE — warning, bounded rush, punishable retreat, claw, real defeat");
        GD.Print($"SKITTER_PROOF_DEFEATED frame={_frame}");
    }

    private void SetStage(int stage, string text)
    {
        _stage = stage;
        _stageStart = _frame;
        _status.Text = text;
        GD.Print($"SKITTER_PROOF_STAGE={stage} frame={_frame} text={text}");
    }

    private string StageText() => _stage switch
    {
        0 => "1/4  Hold RMB: face the short ground charge",
        1 => "2/4  Pursue the vulnerable retreat",
        2 => "3/4  E + LMB: interrupt a fresh warning",
        3 => "4/4  Q + LMB: normal close-range defeat",
        _ => "DEMO COMPLETE — provisional Canyon Skitter"
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
        body.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = size }
        });
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

internal partial class SkitterDemoBackdrop : Node2D
{
    public override void _Draw()
    {
        DrawRect(new Rect2(-200f, -200f, 1600f, 800f), new Color("191918"));
        DrawRect(new Rect2(-100f, 400f, 1500f, 220f), new Color("43372d"));
        for (int x = 40; x < 1240; x += 96)
            DrawLine(new Vector2(x, 400f), new Vector2(x + 34f, 440f), new Color("75583e"), 4f, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(410f, 225f), "CANYON SKITTER / ENEMY DEMO",
            HorizontalAlignment.Left, -1f, 27, new Color("e4d5ae"));
        DrawString(ThemeDB.FallbackFont, new Vector2(410f, 253f),
            "Functional native six-leg visual — not final art or route placement",
            HorizontalAlignment.Left, -1f, 17, new Color("a9a095"));
    }
}
