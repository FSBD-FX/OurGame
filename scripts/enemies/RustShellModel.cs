using System;
using Godot;
using Knight.Combat;

namespace Knight.Enemies;

/// <summary>Deterministic, node-free behavior model for the Rust-Shell Squire.</summary>
public sealed class RustShellModel
{
    private readonly RustShellSettings _settings;
    private RustShellState _state = RustShellState.Idle;
    private float _stateElapsed;
    private ulong _nextAttackId;
    private ulong _attackId;
    private Vector2 _lockedTarget;
    private bool _hasLockedTarget;

    public RustShellModel(RustShellSettings settings = null)
    {
        _settings = settings ?? new RustShellSettings();
        if (_settings.Validate().Count > 0)
            throw new ArgumentException("Invalid Rust-Shell settings.", nameof(settings));
    }

    public RustShellFrame Step(float signedDistanceToPlayer, bool seesPlayer, bool stunned, bool dead, double delta)
    {
        bool enteredState = false;
        float seconds = MathF.Max(0f, (float)delta);
        if (_state == RustShellState.Dead || dead)
        {
            enteredState = TransitionTo(RustShellState.Dead);
            return Frame(0f, false, enteredState);
        }

        if (stunned)
        {
            enteredState = TransitionTo(RustShellState.Stunned);
            return Frame(0f, false, enteredState);
        }

        float distance = MathF.Abs(signedDistanceToPlayer);
        switch (_state)
        {
            case RustShellState.Idle:
                if (seesPlayer && distance <= _settings.DetectionRange)
                {
                    enteredState = TransitionTo(distance <= _settings.AttackRange
                        ? RustShellState.Telegraph
                        : RustShellState.Chase);
                }
                break;

            case RustShellState.Chase:
                if (!seesPlayer || distance > _settings.DetectionRange)
                    enteredState = TransitionTo(RustShellState.Idle);
                else if (distance <= _settings.AttackRange)
                    enteredState = TransitionTo(RustShellState.Telegraph);
                break;

            case RustShellState.Telegraph:
                if (AdvanceTimer(seconds, _settings.TelegraphSeconds))
                {
                    _lockedTarget = new Vector2(signedDistanceToPlayer, 0f);
                    _hasLockedTarget = true;
                    enteredState = TransitionTo(RustShellState.Attack);
                }
                break;

            case RustShellState.Attack:
                if (AdvanceTimer(seconds, _settings.AttackSeconds))
                    enteredState = TransitionTo(RustShellState.Recover);
                break;

            case RustShellState.Recover:
                if (AdvanceTimer(seconds, _settings.RecoverySeconds))
                {
                    enteredState = TransitionTo(seesPlayer && distance <= _settings.DetectionRange
                        ? distance <= _settings.AttackRange ? RustShellState.Telegraph : RustShellState.Chase
                        : RustShellState.Idle);
                }
                break;

            case RustShellState.Stunned:
                if (AdvanceTimer(seconds, _settings.ThreadStunSeconds))
                    enteredState = TransitionTo(seesPlayer ? RustShellState.Chase : RustShellState.Idle);
                break;
        }

        float direction = _state == RustShellState.Chase ? MathF.Sign(signedDistanceToPlayer) : 0f;
        return Frame(direction, _state == RustShellState.Attack, enteredState);
    }

    private bool AdvanceTimer(float seconds, float duration)
    {
        _stateElapsed += seconds;
        return _stateElapsed >= duration;
    }

    private bool TransitionTo(RustShellState next)
    {
        if (_state == next) return false;
        if (next == RustShellState.Telegraph)
        {
            _attackId = ++_nextAttackId;
            _hasLockedTarget = false;
            _lockedTarget = Vector2.Zero;
        }
        _state = next;
        _stateElapsed = 0f;
        return true;
    }

    private RustShellFrame Frame(float moveDirection, bool attackActive, bool enteredState)
    {
        return new RustShellFrame(_state, moveDirection, attackActive, enteredState, Timeline());
    }

    private AttackTimelineFrame Timeline()
    {
        (AttackPhase phase, float duration) = _state switch
        {
            RustShellState.Telegraph => (AttackPhase.Telegraph, _settings.TelegraphSeconds),
            RustShellState.Attack => (AttackPhase.Active, _settings.AttackSeconds),
            RustShellState.Recover => (AttackPhase.Recovery, _settings.RecoverySeconds),
            _ => (AttackPhase.None, 0f)
        };
        if (phase == AttackPhase.None) return default;
        return new AttackTimelineFrame(
            _attackId,
            EnemyAttackKind.RustShellThrust,
            phase,
            duration,
            MathF.Max(0f, duration - _stateElapsed),
            _lockedTarget,
            _hasLockedTarget);
    }
}
