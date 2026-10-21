using System;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Moth;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;

namespace Knight.Tests.Presentation;

// Native-window, generated-level input proof. Only the initial regional setup writes
// player position/velocity; all combat thereafter uses ordinary InputEvents.
public partial class Task14GeneratedRegionProbe : SceneTree
{
    private PrologueLevelController _level;
    private CharacterBody2D _player;
    private CanyonEncounterGroup _group;
    private Node _actors;
    private HealthComponent _playerHealth;
    private PlayerCombat _combat;
    private PlayerActionModeController _modes;
    private Label _status;
    private string _region;
    private int _frame;
    private int _lastAttack = -100;
    private int _grappleUntil;
    private int _lastGrapple = -100;
    private int _firstDefeatFrame = -1;
    private int _birthFrame = -1;
    private int _completeFrame = -1;
    private int _warningHp;
    private int _attacksStarted;
    private int _attacksConnected;
    private bool _sawWarning;
    private bool _sawCounterplay;
    private bool _setup;

    public override void _Initialize()
    {
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Task14 regional proof requires native Vulkan window");
            _region = OS.GetCmdlineUserArgs().Single(arg => arg.StartsWith("--region="))["--region=".Length..];
            if (_region is not ("archive" or "frozen"))
                throw new InvalidOperationException("Task14 proof region must be archive or frozen");
            ReleaseInput();
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
            IsolatedPrologueFixture.Configure(_level, "task14-input-" + _region);
            _player = _level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            Root.AddChild(_level);
            var hud = new CanvasLayer { Name = "Task14EvidenceHud", Layer = 40 };
            _status = new Label
            {
                Position = new Vector2(22, 22), Size = new Vector2(1220, 80)
            };
            _status.AddThemeFontSizeOverride("font_size", 22);
            _status.AddThemeColorOverride("font_color", new Color("fff1c9"));
            _status.AddThemeColorOverride("font_shadow_color", Colors.Black);
            hud.AddChild(_status);
            _level.AddChild(hud);
            GD.Print($"TASK14_INPUT_KIND=generated_full_level region={_region} " +
                "setup=one_initial_player_position_velocity only_after_setup=InputEvents");
        }
        catch (Exception exception)
        {
            GD.PushError("TASK14_PROBE_INIT_FAIL " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        _frame++;
        try
        {
            if (!_setup)
            {
                SetupRegion();
                return false;
            }
            if (_group == null || !GodotObject.IsInstanceValid(_group))
            {
                if (_frame > 80) throw new InvalidOperationException("generated group did not mount");
                ResolveGroup();
                return false;
            }
            if (_playerHealth.CurrentHealth <= 0)
                throw new InvalidOperationException("real player died during input proof");
            if (_group.IsComplete)
            {
                if (_completeFrame < 0) _completeFrame = _frame;
                ReleaseInput();
                _status.Text = $"TASK 14 / {_region.ToUpperInvariant()} COMPLETE    Player HP {_playerHealth.CurrentHealth}";
                if (_frame - _completeFrame > 45)
                {
                    if (_firstDefeatFrame < 0 || !_sawWarning || _birthFrame < 0 ||
                        _attacksStarted < 2 || _attacksConnected < 2 || !_sawCounterplay)
                        throw new InvalidOperationException("proof lacked genuine first/second defeat, cue, input hits or counterplay");
                    GD.Print($"TASK14_REGION_INPUT_OK region={_region} first_defeat={_firstDefeatFrame} " +
                        $"birth={_birthFrame} complete={_completeFrame} attacks={_attacksStarted} " +
                        $"hits={_attacksConnected} counterplay={_sawCounterplay} hp={_playerHealth.CurrentHealth}");
                    Quit(0);
                }
                return false;
            }
            if (_group.SpawnSuppressed)
                throw new InvalidOperationException("normal generated region suppressed its new actor");
            Node2D actor = _actors.GetChildren().OfType<Node2D>().FirstOrDefault(node =>
                node.Name.ToString().StartsWith(_group.EncounterId.ToString().Replace('.', '_'),
                    StringComparison.Ordinal));
            if (_group.CurrentBatch == 1 && _firstDefeatFrame < 0)
                _firstDefeatFrame = _frame;
            if (_group.IsArrivalWarningActive)
            {
                if (!_sawWarning) _warningHp = _playerHealth.CurrentHealth;
                _sawWarning = true;
                if (_playerHealth.CurrentHealth != _warningHp)
                    throw new InvalidOperationException("player HP changed during no-actor arrival warning");
                ReleaseCombatInput();
            }
            else if (actor != null && _group.IsActive)
            {
                if (_group.CurrentBatch == 1 && _birthFrame < 0) _birthFrame = _frame;
                DriveActor(actor);
            }
            else ReleaseCombatInput();
            _status.Text = $"TASK 14 / {_region.ToUpperInvariant()}  Frame {_frame}   " +
                $"HP {_playerHealth.CurrentHealth}   Batch {_group.CurrentBatch}   " +
                $"Cue {_group.IsArrivalWarningActive}   Actor {actor?.GetType().Name ?? "none"} " +
                $"{actor?.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth.ToString() ?? ""}";
            if (_frame % 60 == 0)
                GD.Print($"TASK14_REGION_SAMPLE region={_region} frame={_frame} player={_player.GlobalPosition} " +
                    $"hp={_playerHealth.CurrentHealth} group={_group.IsActive}/{_group.CurrentBatch}/" +
                    $"{_group.LiveActorCount} cue={_group.IsArrivalWarningActive} actor={actor?.GlobalPosition} " +
                    $"actor_hp={actor?.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth} " +
                    $"attacks={_attacksStarted}/{_attacksConnected}");
            if (_frame > 1500) throw new InvalidOperationException("regional real-input proof timeout");
        }
        catch (Exception exception)
        {
            ReleaseInput();
            GD.PushError($"TASK14_REGION_INPUT_FAIL region={_region} frame={_frame}: {exception}");
            Quit(1);
        }
        return false;
    }

    private void SetupRegion()
    {
        _player = _level.Player;
        if (!GodotObject.IsInstanceValid(_player)) return;
        _player.GlobalPosition = _region == "archive"
            ? new Vector2(1465, 1604) : new Vector2(1400, 2144);
        _player.Velocity = Vector2.Zero;
        Camera2D camera = _player.GetNode<Camera2D>("Camera2D");
        camera.Enabled = true;
        camera.ResetSmoothing();
        camera.ForceUpdateScroll();
        _playerHealth = _player.GetNode<HealthComponent>("Health");
        _combat = _player.GetNode<PlayerCombat>("Combat");
        _modes = _player.GetNode<PlayerActionModeController>("ActionModes");
        _combat.AttackStarted += (_, _) => _attacksStarted++;
        _combat.AttackConnected += _ => _attacksConnected++;
        Send(InputActions.SelectAttackMode, true);
        _setup = true;
        GD.Print($"TASK14_REGION_SETUP region={_region} player={_player.GlobalPosition} " +
            $"hp={_playerHealth.CurrentHealth} collision_layer={_player.CollisionLayer}");
    }

    private void ResolveGroup()
    {
        PrologueSector sector = _level.GetNodeOrNull<PrologueSector>("WorldRoot/Sectors/CanyonDescent");
        if (sector == null) return;
        _group = sector.GetNodeOrNull<CanyonEncounterGroup>(
            "Gameplay/Encounters/" + (_region == "archive"
                ? "ArchiveSideCaveEncounter" : "FrozenUpperEncounter"));
        _actors = sector.GetNodeOrNull("Gameplay/Actors");
    }

    private void DriveActor(Node2D actor)
    {
        if (_frame == 4) Send(InputActions.SelectAttackMode, false);
        Aim(actor.GlobalPosition);
        float dx = actor.GlobalPosition.X - _player.GlobalPosition.X;
        float dy = actor.GlobalPosition.Y - _player.GlobalPosition.Y;
        if (actor is GraveLampMoth moth && dy < -70f)
        {
            DriveMothClaw(moth);
            return;
        }
        float distance = MathF.Abs(dx);
        bool threat = actor switch
        {
            RustShellSquire rust => rust.CurrentState is RustShellState.Telegraph or RustShellState.Attack,
            CanyonSkitter skitter => skitter.CurrentFrame.State is CanyonSkitterState.Telegraph or CanyonSkitterState.Rush,
            BellBoltSentry sentry => sentry.CurrentFrame.State is BellBoltSentryState.Telegraph or BellBoltSentryState.Fire,
            GraveLampMoth liveMoth => liveMoth.CurrentState is GraveLampMothState.Telegraph or GraveLampMothState.Dive,
            _ => false
        };
        bool moveRight = dx > 63f || dx < 0f && distance < 29f;
        bool moveLeft = dx < -63f || dx > 0f && distance < 29f;
        if (_player.GlobalPosition.X < _group.Definition.ArenaBounds.Position.X + 28f) moveLeft = false;
        if (_player.GlobalPosition.X > _group.Definition.ArenaBounds.End.X - 28f) moveRight = false;
        Send(InputActions.MoveLeft, moveLeft);
        Send(InputActions.MoveRight, moveRight);
        float remaining = actor switch
        {
            RustShellSquire rust => rust.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
            CanyonSkitter skitter => skitter.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
            BellBoltSentry sentry => sentry.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
            GraveLampMoth timedMoth => timedMoth.CurrentFrame.AttackTimeline.PhaseRemainingSeconds,
            _ => 0f
        };
        bool defend = threat && !_combat.IsAttacking && distance < 120f &&
            (remaining < .18f || actor is CanyonSkitter rushing &&
                rushing.CurrentFrame.State == CanyonSkitterState.Rush);
        Send(InputActions.Shield, defend);
        if (defend) _sawCounterplay = true;
        bool verticalStrike = actor is GraveLampMoth && dy is >= -74f and <= -10f && distance <= 36f;
        bool horizontalStrike = distance is >= 28f and <= 64f && MathF.Abs(dy) < 48f;
        bool strike = !threat && (verticalStrike || horizontalStrike) &&
            !_combat.IsAttacking && _modes.CanBeginAttack && _frame - _lastAttack >= 23;
        if (_frame % 30 == 0 && _group.CurrentBatch == 1)
            GD.Print($"TASK14_INPUT_DIAG frame={_frame} dx={dx} dy={dy} actor_state=" +
                $"{(actor is CanyonSkitter sk ? sk.CurrentFrame.State.ToString() : actor.GetType().Name)} " +
                $"threat={threat} strike={strike} attack_mode={_modes.CurrentMode} " +
                $"can_attack={_modes.CanBeginAttack} afford={_modes.CanAffordAttack} " +
                $"energy={_player.GetNode<PlayerEnergyController>("Energy").EnergyRatio:0.00} " +
                $"attacking={_combat.IsAttacking} " +
                $"shield={_player.GetNode<PlayerInputReader>("Input").Current.ShieldHeld} " +
                $"last_attack={_lastAttack}");
        if (strike)
        {
            Send(InputActions.Shield, false);
            Send(InputActions.MoveUp, verticalStrike);
            Send(InputActions.MoveLeft, !verticalStrike && dx < 0f);
            Send(InputActions.MoveRight, !verticalStrike && dx > 0f);
            Send(InputActions.PrimaryAction, true);
            _lastAttack = _frame;
        }
        if (_frame - _lastAttack == 2)
        {
            Send(InputActions.PrimaryAction, false);
            Send(InputActions.MoveUp, false);
        }
    }

    private void DriveMothClaw(GraveLampMoth moth)
    {
        ReleaseCombatInput();
        if (_frame - _lastGrapple < 80) return;
        if (_grappleUntil == 0)
        {
            Send(InputActions.SelectGrappleMode, true);
            _grappleUntil = _frame + 2;
            return;
        }
        if (_frame == _grappleUntil)
        {
            Send(InputActions.SelectGrappleMode, false);
            Aim(moth.GetNode<ThreadTarget2D>("ThreadTarget").GlobalPosition);
            Send(InputActions.PrimaryAction, true);
        }
        if (_frame == _grappleUntil + 2)
        {
            Send(InputActions.PrimaryAction, false);
            Send(InputActions.SelectAttackMode, true);
            _lastGrapple = _frame;
            _grappleUntil = 0;
            _sawCounterplay = true;
        }
        if (_frame == _lastGrapple + 2) Send(InputActions.SelectAttackMode, false);
    }

    private void Aim(Vector2 world)
    {
        Vector2 pointer = _player.GetViewport().GetFinalTransform() *
            (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(world));
        var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
        _player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
    }

    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseCombatInput()
    {
        Send(InputActions.MoveLeft, false);
        Send(InputActions.MoveRight, false);
        Send(InputActions.PrimaryAction, false);
        Send(InputActions.Shield, false);
    }

    private static void ReleaseInput()
    {
        ReleaseCombatInput();
        Send(InputActions.SelectAttackMode, false);
        Send(InputActions.SelectGrappleMode, false);
        Send(InputActions.Jump, false);
    }

    public override void _Finalize() => ReleaseInput();
}
