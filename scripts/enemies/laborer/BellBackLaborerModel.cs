using System;
using Godot;
using Knight.Combat;
using Knight.Config;

namespace Knight.Enemies.Laborer;

public sealed class BellBackLaborerModel
{
    private readonly BellBackLaborerSettings _settings;
    private BellBackLaborerState _state = BellBackLaborerState.Idle;
    private float _elapsed;
    private ulong _nextAttackId;
    private ulong _attackId;
    private Vector2 _lockedTarget;
    private bool _hasLockedTarget;

    public BellBackLaborerModel(BellBackLaborerSettings? settings = null)
    {
        _settings = settings ?? new BellBackLaborerTuning().ToSettings();
        if (BellBackLaborerTuning.Validate(_settings).Count > 0)
            throw new ArgumentException("Invalid Bell-Back Laborer settings.", nameof(settings));
    }

    public BellBackLaborerFrame Step(float signedDistanceToPlayer, bool seesPlayer,
        bool threadRequested, bool dead, double delta)
    {
        if (_state == BellBackLaborerState.Dead || dead)
        {
            bool entered = Transition(BellBackLaborerState.Dead);
            return Frame(0f, false, false, entered);
        }
        if (threadRequested)
        {
            bool entered = Transition(BellBackLaborerState.Exposed);
            return Frame(0f, false, false, entered);
        }

        float seconds = MathF.Max(0f, (float)delta);
        float distance = MathF.Abs(signedDistanceToPlayer);
        bool changed = false;
        switch (_state)
        {
            case BellBackLaborerState.Idle:
                if (seesPlayer && distance <= _settings.DetectionRange)
                    changed = Transition(distance <= _settings.GuardRange ? BellBackLaborerState.Guard : BellBackLaborerState.Approach);
                break;
            case BellBackLaborerState.Approach:
                if (!seesPlayer || distance > _settings.DetectionRange) changed = Transition(BellBackLaborerState.Idle);
                else if (distance <= _settings.GuardRange) changed = Transition(BellBackLaborerState.Guard);
                break;
            case BellBackLaborerState.Guard:
                if (!seesPlayer || distance > _settings.DetectionRange) changed = Transition(BellBackLaborerState.Idle);
                else if (distance > _settings.GuardRange) changed = Transition(BellBackLaborerState.Approach);
                else if (Advance(seconds, _settings.GuardSeconds)) changed = Transition(BellBackLaborerState.Telegraph);
                break;
            case BellBackLaborerState.Telegraph:
                if (Advance(seconds, _settings.TelegraphSeconds))
                {
                    _lockedTarget = new Vector2(signedDistanceToPlayer, 0f);
                    _hasLockedTarget = true;
                    changed = Transition(BellBackLaborerState.Attack);
                }
                break;
            case BellBackLaborerState.Attack:
                if (Advance(seconds, _settings.AttackSeconds)) changed = Transition(BellBackLaborerState.Recover);
                break;
            case BellBackLaborerState.Recover:
                if (Advance(seconds, _settings.RecoverySeconds)) changed = Transition(BellBackLaborerState.Guard);
                break;
            case BellBackLaborerState.Exposed:
                if (Advance(seconds, _settings.ExposedSeconds)) changed = Transition(BellBackLaborerState.Guard);
                break;
        }
        float direction = _state == BellBackLaborerState.Approach ? MathF.Sign(signedDistanceToPlayer) : 0f;
        return Frame(direction, _state == BellBackLaborerState.Attack,
            _state is BellBackLaborerState.Guard or BellBackLaborerState.Telegraph or BellBackLaborerState.Attack or BellBackLaborerState.Recover,
            changed);
    }

    private bool Advance(float seconds, float duration)
    {
        _elapsed += seconds;
        return _elapsed + .000001f >= duration;
    }

    private bool Transition(BellBackLaborerState next)
    {
        if (_state == next) return false;
        if (next == BellBackLaborerState.Telegraph)
        {
            _attackId = ++_nextAttackId;
            _lockedTarget = Vector2.Zero;
            _hasLockedTarget = false;
        }
        _state = next;
        _elapsed = 0f;
        return true;
    }

    private BellBackLaborerFrame Frame(float direction, bool attack, bool guarded, bool entered) =>
        new(_state, direction, attack, guarded, entered, Timeline());

    private AttackTimelineFrame Timeline()
    {
        (AttackPhase phase, float duration) = _state switch
        {
            BellBackLaborerState.Telegraph => (AttackPhase.Telegraph, _settings.TelegraphSeconds),
            BellBackLaborerState.Attack => (AttackPhase.Active, _settings.AttackSeconds),
            BellBackLaborerState.Recover => (AttackPhase.Recovery, _settings.RecoverySeconds),
            _ => (AttackPhase.None, 0f)
        };
        if (phase == AttackPhase.None) return default;
        return new AttackTimelineFrame(
            _attackId,
            EnemyAttackKind.LaborerSweep,
            phase,
            duration,
            MathF.Max(0f, duration - _elapsed),
            _lockedTarget,
            _hasLockedTarget);
    }
}
