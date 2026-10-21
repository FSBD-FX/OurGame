using System;
using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;

namespace Knight.Tests.Presentation;

public partial class BossRetryPresentation : SceneTree
{
    private const int FinalFrame = 600;
    private const int AudioDrainLeadFrames = 90;
    private PrologueLevelController _level;
    private CharacterBody2D _player;
    private Camera2D _camera;
    private int _frame;
    private ulong _firstBossId;
    private bool _retryActivated;
    private bool _phaseTwoReached;
    private bool _recoveryControlStarted;
    private PlayerCombat _playerCombat;
    private Hurtbox _trackedBossHurtbox;
    private HealthComponent _trackedBossHealth;
    private int _transitionHealth;
    private int _bossHitConnections;
    private bool _bossStrikeHeld;
    private int _bossStrikeReleaseFrame;
    private Task _audioDrainTask;
    private bool _failurePending;

    public override void _Initialize()
    {
        try
        {
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
            Root.AddChild(_level);
            _player = _level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _playerCombat = _player.GetNode<PlayerCombat>("Combat");
            _playerCombat.AttackConnected += OnPlayerAttackConnected;
            _player.GetNode<Camera2D>("Camera2D").Enabled = false;
            _camera = new Camera2D { Name = "RetryProofCamera", Enabled = true, Position = new Vector2(390, 40) };
            _level.AddChild(_camera);
            GD.Print("BOSS_RETRY_PRESENTATION_STAGE=route_to_arena");
        }
        catch (Exception exception)
        {
            BeginFailure("BOSS_RETRY_PRESENTATION_INIT_FAIL", exception);
        }
    }

    public override bool _Process(double delta)
    {
        if (_failurePending)
        {
            CompleteFailureWhenAudioSettles();
            return false;
        }
        if (_frame == FinalFrame - 1 && _audioDrainTask?.IsCompleted == false)
        {
            try { ThrowIfAudioDrainFailed(); }
            catch (Exception exception)
            {
                BeginFailure($"BOSS_RETRY_PRESENTATION_FAIL frame={_frame}", exception);
            }
            return false;
        }
        _frame++;
        try
        {
            Drive();
            if (_frame == FinalFrame - AudioDrainLeadFrames) StartAudioDrain();
            ThrowIfAudioDrainFailed();
            if (_frame != FinalFrame) return false;
            ReleaseAll();
            BossArenaController arena = Arena();
            if (!_retryActivated || !arena.IsEncounterActive || arena.CurrentBoss.GetInstanceId() == _firstBossId)
                throw new InvalidOperationException("retry proof did not restore and restart a fresh Boss");
            if (_frame != FinalFrame)
                throw new InvalidOperationException(
                    $"Boss retry completion marker must use the actual final frame; actual={_frame}, expected={FinalFrame}");
            if (_audioDrainTask == null || !_audioDrainTask.IsCompletedSuccessfully)
                throw new InvalidOperationException("Boss retry audio drain did not complete before the final frame");
            if (!_level.IsInsideTree() || _level.GetNodeOrNull<Knight.Audio.PrologueAudioDirector>("Audio") != null)
                throw new InvalidOperationException("Boss retry final frame must keep the visible level and release only audio");
            GD.Print($"BOSS_RETRY_PRESENTATION_FINAL_VISIBLE frame={_frame} " +
                $"encounter_active={arena.IsEncounterActive} level_inside_tree=True");
            GD.Print($"BOSS_RETRY_PRESENTATION_COMPLETE frame={_frame}");
            TestSceneTeardown.DrainManagedWrappersBeforeQuit("boss-retry");
            Quit(0);
        }
        catch (Exception exception)
        {
            BeginFailure($"BOSS_RETRY_PRESENTATION_FAIL frame={_frame}", exception);
        }
        return false;
    }

    private void StartAudioDrain()
    {
        _audioDrainTask ??= TestSceneTeardown.DrainAudioOwner(this, _level);
    }

    private void ThrowIfAudioDrainFailed()
    {
        if (_audioDrainTask?.IsFaulted == true)
            throw new InvalidOperationException("Boss retry audio drain failed",
                _audioDrainTask.Exception?.GetBaseException());
        if (_audioDrainTask?.IsCanceled == true)
            throw new InvalidOperationException("Boss retry audio drain was canceled");
    }

    private void BeginFailure(string label, Exception exception)
    {
        if (_failurePending) return;
        ReleaseAll();
        GD.PushError($"{label}: {exception}");
        _failurePending = true;
        if (_audioDrainTask == null && IsInstanceValid(_level) && _level.IsInsideTree()) StartAudioDrain();
        CompleteFailureWhenAudioSettles();
    }

    private void CompleteFailureWhenAudioSettles()
    {
        if (_audioDrainTask?.IsFaulted == true)
            _ = _audioDrainTask.Exception;
        if (_audioDrainTask == null || _audioDrainTask.IsCompleted) Quit(1);
    }

    private void Drive()
    {
        if (_frame == 30) TouchExit("Exits/ToCorridor");
        if (_frame == 60) TouchExit("Exits/ToUpper");
        if (_frame == 90) TouchExit("Exits/ToMechanism");
        if (_frame == 120) TouchExit("Exits/P1Complete");
        if (_frame == 155)
        {
            Fixture(_level.CurrentRoom.GetNode<Marker2D>("Entrances/boss_retry").GlobalPosition);
            GD.Print("BOSS_RETRY_PRESENTATION_STAGE=checkpoint_lit");
        }
        if (_frame == 185)
        {
            Fixture(_level.CurrentRoom.GetNode<Area2D>("ActivationTrigger").GlobalPosition);
            GD.Print("BOSS_RETRY_PRESENTATION_STAGE=first_attempt");
        }
        if (_bossStrikeHeld && _frame >= _bossStrikeReleaseFrame)
        {
            Release(InputActions.Attack);
            Release(InputActions.MoveRight);
            _bossStrikeHeld = false;
        }
        if (_frame >= 235 && Arena().CurrentBoss.CurrentFrame.State == FallenBellkeeperState.PhaseOne)
        {
            FallenBellkeeper boss = Arena().CurrentBoss;
            if (_trackedBossHurtbox == null)
            {
                _trackedBossHurtbox = boss.GetNode<Hurtbox>("Hurtbox");
                _trackedBossHealth = boss.GetNode<HealthComponent>("Health");
                _transitionHealth = Mathf.FloorToInt(
                    _trackedBossHealth.MaxHealth * boss.Tuning.TransitionHealthRatio);
                HealthComponent playerHealth = _player.GetNode<HealthComponent>("Health");
                playerHealth.MaxHealth = 99;
                playerHealth.ResetFull();
            }
            if (_trackedBossHealth.CurrentHealth > _transitionHealth &&
                !_bossStrikeHeld && !_playerCombat.IsAttacking)
            {
                PositionForBossStrike(boss);
                Press(InputActions.MoveRight);
                Press(InputActions.Attack);
                _bossStrikeHeld = true;
                _bossStrikeReleaseFrame = _frame + 2;
            }
        }
        if (_frame >= 235 && !_phaseTwoReached &&
            Arena().CurrentBoss.CurrentFrame.State == FallenBellkeeperState.PhaseTwoArmored)
        {
            if (_trackedBossHealth.CurrentHealth != _transitionHealth || _bossHitConnections != 5)
                throw new InvalidOperationException(
                    $"real player combat did not reach the phase threshold exactly; " +
                    $"hits={_bossHitConnections}, health={_trackedBossHealth.CurrentHealth}, threshold={_transitionHealth}");
            _phaseTwoReached = true;
            GD.Print($"BOSS_RETRY_REAL_PLAYER_DAMAGE_OK hits={_bossHitConnections} " +
                $"health={_trackedBossHealth.CurrentHealth}");
        }
        if (_frame >= 235 && _frame < 430 && _phaseTwoReached)
        {
            BossArenaController arena = Arena();
            if (_firstBossId == 0)
            {
                if (arena.CurrentBoss.CurrentFrame.State != FallenBellkeeperState.PhaseTwoArmored)
                    throw new InvalidOperationException($"retry death must occur in phase two; got {arena.CurrentBoss.CurrentFrame.State}");
                _firstBossId = arena.CurrentBoss.GetInstanceId();
                _player.GetNode<HealthComponent>("Health").ApplyDamage(
                    new DamageRequest(999, Vector2.Right, 0f, 99001UL));
                GD.Print($"BOSS_RETRY_PRESENTATION_STAGE=player_death phase=PhaseTwoArmored frame={_frame}");
            }
        }
        if (_frame >= 315 && _firstBossId != 0 && !_recoveryControlStarted && Arena().CurrentBoss.GetInstanceId() != _firstBossId)
        {
            BossArenaController arena = Arena();
            if (arena.CurrentBoss.CurrentFrame.State != FallenBellkeeperState.Dormant)
                throw new InvalidOperationException("death did not restore one fresh dormant Boss");
            _recoveryControlStarted = true;
            GD.Print($"BOSS_RETRY_PRESENTATION_STAGE=checkpoint_restored frame={_frame}");
            Press(InputActions.MoveRight);
        }
        if (_recoveryControlStarted && !_retryActivated && Arena().IsEncounterActive &&
            Arena().CurrentBoss.GetInstanceId() != _firstBossId)
        {
            _retryActivated = true;
            Release(InputActions.MoveRight);
            GD.Print($"BOSS_RETRY_PRESENTATION_STAGE=retry_started frame={_frame}");
        }
        if (_frame == 500)
        {
            Release(InputActions.MoveRight);
            GD.Print("BOSS_RETRY_PRESENTATION_STAGE=fresh_phase_one");
        }
    }

    private void TouchExit(string path)
    {
        RoomExit exit = _level.CurrentRoom.GetNode<RoomExit>(path);
        Fixture(exit.GlobalPosition);
    }

    private BossArenaController Arena() =>
        _level.CurrentRoom.GetNode<BossArenaController>("BossArenaController");

    private void OnPlayerAttackConnected(Hurtbox hurtbox)
    {
        if (hurtbox == _trackedBossHurtbox) _bossHitConnections++;
    }

    private void PositionForBossStrike(FallenBellkeeper boss)
    {
        _player.GlobalPosition = boss.GlobalPosition + new Vector2(-52, 0);
        _player.Velocity = Vector2.Zero;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
    }

    private void Fixture(Vector2 position)
    {
        _player = _level.Player;
        _player.GlobalPosition = position;
        _player.Velocity = Vector2.Zero;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
    }

    private static void Press(StringName action) => Input.ActionPress(action);
    private static void Release(StringName action) => Input.ActionRelease(action);
    private static void ReleaseAll()
    {
        Release(InputActions.MoveRight);
        Release(InputActions.Attack);
        Release(InputActions.Thread);
        Release(InputActions.Jump);
    }
}
