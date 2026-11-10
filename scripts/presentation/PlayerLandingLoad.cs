using System;

namespace Knight.Presentation;

// Value snapshots only. The Motor remains the sole owner of impact measurement.
public readonly record struct PlayerLandingObservation(ulong OwnerId, ulong Count, float Speed, bool Reliable);
public readonly record struct PlayerLandingLoadFrame(bool Active, float Compression, float Strength);

/// <summary>One measured contact, one elapsed-time absorption envelope; no native ownership.</summary>
public sealed class PlayerLandingLoadModel
{
    private ulong _owner, _count;
    private double _age, _duration;
    private float _strength;
    private PlayerLandingLoadFrame _frame;

    public PlayerLandingLoadFrame Step(PlayerLandingObservation observation, bool grounded,
        bool suppressed, float deltaSeconds)
    {
        if (observation.OwnerId == 0)
        {
            _owner = _count = 0;
            return Clear();
        }
        if (_owner != observation.OwnerId || observation.Count < _count)
        {
            _owner = observation.OwnerId;
            _count = observation.Count;
            return Clear(); // A binding/rollback primes history, never replays it.
        }
        if (suppressed || !grounded)
        {
            _count = observation.Count;
            return Clear();
        }
        if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0f) return _frame;

        bool fresh = observation.Count > _count;
        _count = observation.Count;
        if (fresh)
        {
            if (!observation.Reliable || !float.IsFinite(observation.Speed) || observation.Speed <= 0f)
                return Clear(); // Reject this count permanently, not just this payload.
            double ratio = Math.Clamp((double)observation.Speed / 650d, 0d, 1d);
            _strength = (float)(ratio * ratio);
            _age = 0d;
            _duration = .18d + .10d * _strength;
        }
        if (_duration <= 0d) return _frame = default;
        _age += deltaSeconds;
        if (_age >= _duration) return Clear();
        double u = _age / _duration;
        float compression = (float)(4d * _strength * 4d * u * (1d - u));
        return _frame = new(true, compression, _strength);
    }

    private PlayerLandingLoadFrame Clear()
    {
        _age = _duration = 0d;
        _strength = 0f;
        return _frame = default;
    }
}
