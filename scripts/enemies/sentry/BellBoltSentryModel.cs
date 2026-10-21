using System;
using Godot;
using Knight.Combat;

namespace Knight.Enemies.Sentry;

public enum BellBoltSentryState
{
    Idle,
    Telegraph,
    Fire,
    Recover,
    Stunned,
    Dead
}

public readonly record struct BellBoltSentryInput(
    Vector2 MuzzleWorldPosition,
    Vector2 TargetWorldPosition,
    bool TargetVisible,
    bool StunRequested,
    bool Dead);

public readonly record struct BellBoltSentryFrame(
    BellBoltSentryState State,
    Vector2 AimDirection,
    bool FireRequested,
    AttackTimelineFrame AttackTimeline);

public sealed class BellBoltSentryModel
{
    private readonly BellBoltSentrySettings _settings;
    private BellBoltSentryState _state;
    private float _elapsed;
    private Vector2 _aimDirection;
    private Vector2 _lockedWorldTarget;
    private bool _aimLocked;
    private ulong _nextAttackId;
    private ulong _attackId;

    public BellBoltSentryModel(BellBoltSentrySettings settings = null)
    {
        _settings = settings ?? new BellBoltSentrySettings();
        if (_settings.Validate().Count > 0)
            throw new ArgumentException("Invalid Bell-Bolt Sentry settings.", nameof(settings));
    }

    public BellBoltSentryFrame Step(BellBoltSentryInput input, double delta)
    {
        if (_state == BellBoltSentryState.Dead || input.Dead)
        {
            Transition(BellBoltSentryState.Dead);
            return Frame(false);
        }
        if (input.StunRequested)
        {
            Transition(BellBoltSentryState.Stunned);
            return Frame(false);
        }

        float seconds = double.IsFinite(delta) && delta > 0d
            ? (float)Math.Min(delta, float.MaxValue)
            : 0f;
        bool validAim = TryAim(input, out Vector2 aim, out float distance);
        bool targetAvailable = input.TargetVisible && validAim && distance <= _settings.DetectionRange;
        bool fireRequested = false;

        switch (_state)
        {
            case BellBoltSentryState.Idle:
                if (targetAvailable)
                {
                    BeginAttack(input, aim);
                    Transition(BellBoltSentryState.Telegraph);
                }
                break;
            case BellBoltSentryState.Telegraph:
                if (!_aimLocked && !targetAvailable)
                {
                    ClearAttack();
                    Transition(BellBoltSentryState.Idle);
                    break;
                }
                if (!_aimLocked) _aimDirection = aim;
                _elapsed += seconds;
                if (!_aimLocked && _elapsed + .000001f >= _settings.DirectionLockSeconds)
                {
                    _aimLocked = true;
                    _lockedWorldTarget = input.MuzzleWorldPosition + _aimDirection * _settings.MaximumTravel;
                }
                if (_elapsed + .000001f >= _settings.TelegraphSeconds)
                {
                    if (!_aimLocked)
                    {
                        _aimLocked = true;
                        _lockedWorldTarget = input.MuzzleWorldPosition + _aimDirection * _settings.MaximumTravel;
                    }
                    Transition(BellBoltSentryState.Fire);
                    fireRequested = true;
                }
                break;
            case BellBoltSentryState.Fire:
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.FireSeconds)
                    Transition(BellBoltSentryState.Recover);
                break;
            case BellBoltSentryState.Recover:
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.RecoverySeconds)
                {
                    ClearAttack();
                    Transition(BellBoltSentryState.Idle);
                }
                break;
            case BellBoltSentryState.Stunned:
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.ThreadStunSeconds)
                {
                    ClearAttack();
                    Transition(BellBoltSentryState.Idle);
                }
                break;
        }

        return Frame(fireRequested);
    }

    public BellBoltSentryFrame CancelAttack()
    {
        if(_state is not (BellBoltSentryState.Dead or BellBoltSentryState.Stunned))
        {ClearAttack();Transition(BellBoltSentryState.Idle);}
        return Frame(false); // Never replace the model or reuse an old attack ID.
    }

    private bool TryAim(BellBoltSentryInput input, out Vector2 direction, out float distance)
    {
        direction = Vector2.Zero;
        distance = 0f;
        if (!Finite(input.MuzzleWorldPosition) || !Finite(input.TargetWorldPosition)) return false;
        Vector2 offset = input.TargetWorldPosition - input.MuzzleWorldPosition;
        distance = offset.Length();
        if (!float.IsFinite(distance) || distance <= .0001f) return false;
        direction = offset / distance;
        return Finite(direction);
    }

    private void BeginAttack(BellBoltSentryInput input, Vector2 aim)
    {
        _attackId = ++_nextAttackId;
        _aimDirection = aim;
        _lockedWorldTarget = Vector2.Zero;
        _aimLocked = false;
    }

    private void ClearAttack()
    {
        _attackId = 0;
        _aimDirection = Vector2.Zero;
        _lockedWorldTarget = Vector2.Zero;
        _aimLocked = false;
    }

    private void Transition(BellBoltSentryState next)
    {
        if (_state == next) return;
        _state = next;
        _elapsed = 0f;
        if (next is BellBoltSentryState.Stunned or BellBoltSentryState.Dead) ClearAttack();
    }

    private BellBoltSentryFrame Frame(bool fireRequested) =>
        new(_state, _aimDirection, fireRequested, Timeline());

    private AttackTimelineFrame Timeline()
    {
        (AttackPhase phase, float duration) = _state switch
        {
            BellBoltSentryState.Telegraph => (AttackPhase.Telegraph, _settings.TelegraphSeconds),
            BellBoltSentryState.Fire => (AttackPhase.Active, _settings.FireSeconds),
            BellBoltSentryState.Recover => (AttackPhase.Recovery, _settings.RecoverySeconds),
            _ => (AttackPhase.None, 0f)
        };
        if (phase == AttackPhase.None) return default;
        return new AttackTimelineFrame(_attackId, EnemyAttackKind.BellBoltShot, phase, duration,
            MathF.Max(0f, duration - _elapsed), _lockedWorldTarget, _aimLocked);
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
