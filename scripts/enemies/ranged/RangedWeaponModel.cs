using System;
using Godot;
using Knight.Combat;

namespace Knight.Enemies.Ranged;

public sealed class RangedWeaponModel
{
    private const double BoundaryRelativeTolerance = .000001d;
    private readonly ModelSettings _settings;
    private RangedWeaponState _state;
    private double _elapsed;
    private Vector2 _aimDirection;
    private Vector2 _lockedWorldTarget;
    private bool _aimLocked;
    private int _emissionsEmitted;
    private ulong _nextAttackId;
    private ulong _attackId;

    public RangedWeaponModel(RangedWeaponSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Validate().Count > 0)
            throw new ArgumentException("Invalid ranged weapon settings.", nameof(settings));
        _settings = new ModelSettings(
            settings.Role,
            settings.DetectionRange,
            settings.MaximumTravel,
            settings.TelegraphSeconds,
            settings.DirectionLockSeconds,
            settings.ActiveSeconds,
            settings.RecoverySeconds,
            settings.ThreadStunSeconds,
            settings.BurstEmissionCount,
            settings.BurstIntervalSeconds);
    }

    public RangedWeaponFrame Step(RangedWeaponInput input, double delta)
    {
        if (_state == RangedWeaponState.Dead || input.Dead)
        {
            Transition(RangedWeaponState.Dead);
            return Frame();
        }
        if (input.StunRequested)
        {
            Transition(RangedWeaponState.Stunned);
            return Frame();
        }

        double seconds = double.IsFinite(delta) && delta > 0d && delta <= float.MaxValue ? delta : 0d;
        bool validAim = TryAim(input, out Vector2 aim, out float distance);
        bool targetAvailable = input.TargetVisible && validAim && distance <= _settings.DetectionRange;
        int firstEmissionIndex = -1;
        int emissionsDue = 0;

        // Readability contract: advance only the phase that existed on entry.
        // Surplus delta is not carried across phases or into a new attack cycle.
        switch (_state)
        {
            case RangedWeaponState.Idle:
                if (targetAvailable)
                {
                    _attackId = ++_nextAttackId;
                    _aimDirection = aim;
                    Transition(RangedWeaponState.Telegraph);
                }
                break;
            case RangedWeaponState.Telegraph:
                if (!_aimLocked)
                {
                    if (!targetAvailable)
                    {
                        Transition(RangedWeaponState.Idle);
                        break;
                    }
                    _aimDirection = aim;
                }
                Advance(_settings.TelegraphSeconds, seconds);
                if (!_aimLocked && Reached(_settings.DirectionLockSeconds))
                {
                    _aimLocked = true;
                    _lockedWorldTarget = input.MuzzleWorldPosition + _aimDirection * _settings.MaximumTravel;
                }
                if (Reached(_settings.TelegraphSeconds))
                {
                    Transition(RangedWeaponState.Active);
                    _emissionsEmitted = 1;
                    firstEmissionIndex = 0;
                    emissionsDue = 1;
                }
                break;
            case RangedWeaponState.Active:
                Advance(_settings.ActiveSeconds, seconds);
                int scheduled = ScheduledEmissionCount();
                if (scheduled > _emissionsEmitted)
                {
                    firstEmissionIndex = _emissionsEmitted;
                    emissionsDue = scheduled - _emissionsEmitted;
                    _emissionsEmitted = scheduled;
                }
                if (Reached(_settings.ActiveSeconds)) Transition(RangedWeaponState.Recovery);
                break;
            case RangedWeaponState.Recovery:
                Advance(_settings.RecoverySeconds, seconds);
                if (Reached(_settings.RecoverySeconds)) Transition(RangedWeaponState.Idle);
                break;
            case RangedWeaponState.Stunned:
                Advance(_settings.ThreadStunSeconds, seconds);
                if (Reached(_settings.ThreadStunSeconds)) Transition(RangedWeaponState.Idle);
                break;
        }

        return Frame(firstEmissionIndex, emissionsDue);
    }

    public RangedWeaponFrame CancelAttack()
    {
        if(_state is not (RangedWeaponState.Dead or RangedWeaponState.Stunned))Transition(RangedWeaponState.Idle);
        return Frame(); // _nextAttackId survives sleep/territorial cancellation.
    }

    private int ScheduledEmissionCount()
    {
        if (_settings.Role != RangedWeaponRole.Burst) return 1;
        double tolerance = _settings.BurstIntervalSeconds * BoundaryRelativeTolerance;
        double available = Math.Floor((_elapsed + tolerance) / _settings.BurstIntervalSeconds) + 1d;
        return (int)Math.Min(_settings.BurstEmissionCount, available);
    }

    private void Advance(float duration, double seconds) =>
        _elapsed = Math.Min(duration, _elapsed + seconds);

    private bool Reached(float boundary) =>
        _elapsed >= boundary || boundary - _elapsed <= boundary * BoundaryRelativeTolerance;

    private void Transition(RangedWeaponState next)
    {
        if (_state == next) return;
        _state = next;
        _elapsed = 0d;
        if (next is RangedWeaponState.Idle or RangedWeaponState.Stunned or RangedWeaponState.Dead)
        {
            _attackId = 0;
            _aimDirection = Vector2.Zero;
            _lockedWorldTarget = Vector2.Zero;
            _aimLocked = false;
            _emissionsEmitted = 0;
        }
    }

    private RangedWeaponFrame Frame(int firstEmissionIndex = -1, int emissionsDue = 0) =>
        new(_state, _aimDirection, Timeline(), firstEmissionIndex, emissionsDue);

    private AttackTimelineFrame Timeline()
    {
        (AttackPhase phase, float duration) = _state switch
        {
            RangedWeaponState.Telegraph => (AttackPhase.Telegraph, _settings.TelegraphSeconds),
            RangedWeaponState.Active => (AttackPhase.Active, _settings.ActiveSeconds),
            RangedWeaponState.Recovery => (AttackPhase.Recovery, _settings.RecoverySeconds),
            _ => (AttackPhase.None, 0f)
        };
        if (phase == AttackPhase.None) return default;
        EnemyAttackKind kind = _settings.Role switch
        {
            RangedWeaponRole.Scatter => EnemyAttackKind.RivetScatterShot,
            RangedWeaponRole.Burst => EnemyAttackKind.BeltBurstShot,
            RangedWeaponRole.Missile => EnemyAttackKind.FurnaceMissileShot,
            RangedWeaponRole.Laser => EnemyAttackKind.PrismLaserBeam,
            _ => EnemyAttackKind.None
        };
        return new AttackTimelineFrame(_attackId, kind, phase, duration,
            (float)Math.Max(0d, duration - _elapsed), _lockedWorldTarget, _aimLocked);
    }

    private static bool TryAim(RangedWeaponInput input, out Vector2 direction, out float distance)
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

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private readonly record struct ModelSettings(
        RangedWeaponRole Role,
        float DetectionRange,
        float MaximumTravel,
        float TelegraphSeconds,
        float DirectionLockSeconds,
        float ActiveSeconds,
        float RecoverySeconds,
        float ThreadStunSeconds,
        int BurstEmissionCount,
        float BurstIntervalSeconds);
}
