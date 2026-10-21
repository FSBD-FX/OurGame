using System;
using System.Collections.Generic;
using Godot;
using Knight.Player;

namespace Knight.Presentation;

[Flags]
public enum ThrusterCoastExit
{
    None = 0, Jump = 1, ClawAttachment = 2, ExternalReplacement = 4,
    Landing = 8, CollisionBlocked = 16, BrakedToZero = 32, Reset = 64,
    Death = 128, Superseded = 256, ConfigurationChange = 512
}

public enum ThrusterTransitionKind { Started, Ended }

public readonly record struct ThrusterPresentationTransition(ulong Sequence,
    ThrusterTransitionKind Kind, ThrusterIdentity Identity, Vector2 Direction,
    ThrusterEndReason Reason);

public readonly record struct ThrusterPresentationFrame(ThrusterIdentity Identity,
    Vector2 Direction, bool Burning, bool Coasting, bool Braking, float CoastRemaining);

// Observation only: Motor supplies real accepted/end/force/completed-step facts.
// No callback, body velocity, gameplay budget, or physical authority lives here.
public sealed class ThrusterPresentationModel
{
    private const float MaximumCoast = .45f;
    private const int JournalCapacity = 64;
    private readonly ulong _ownerId;
    private readonly Queue<ThrusterPresentationTransition> _journal = new();
    private ThrusterIdentity _acceptedIdentity;
    private ThrusterIdentity _identity;
    private ulong _life = 1UL, _epoch = 1UL;
    private Vector2 _direction;
    private bool _burning, _producedThrust, _braking;
    private float _coastRemaining;

    public ulong LatestSequence { get; private set; }
    public ulong FirstAvailableSequence => _journal.Count == 0 ? LatestSequence + 1UL : _journal.Peek().Sequence;
    public ThrusterPresentationFrame Frame => new(_identity,
        _identity == default ? Vector2.Zero : _direction,
        _burning, _coastRemaining > 0f, _braking, _coastRemaining);

    public ThrusterPresentationModel(ulong ownerId)
    {
        if (ownerId == 0UL) throw new ArgumentOutOfRangeException(nameof(ownerId));
        _ownerId = ownerId;
    }

    public bool Start(ThrusterPulseCommitResult accepted, Vector2 direction)
    {
        ThrusterIdentity identity = accepted.Identity;
        if (!accepted.Accepted || accepted.Reason != ThrusterRejectReason.None || _burning ||
            identity.OwnerId != _ownerId || identity.Lifecycle < _life ||
            identity.ConfigurationEpoch < _epoch || identity.BurnId == 0UL ||
            identity.BurnId <= _acceptedIdentity.BurnId || !Finite(direction) ||
            Math.Abs(direction.LengthSquared() - 1f) > .001f) return false;
        _life = identity.Lifecycle; _epoch = identity.ConfigurationEpoch;
        _acceptedIdentity = _identity = identity;
        _direction = direction; _burning = true;
        _producedThrust = false; _coastRemaining = 0f; _braking = false;
        Append(ThrusterTransitionKind.Started, identity, ThrusterEndReason.None);
        return true;
    }

    public bool RecordThrust(ThrusterIdentity identity, Vector2 thrustVelocityDelta)
    {
        if (!_burning || identity != _identity || !Finite(thrustVelocityDelta) ||
            thrustVelocityDelta == Vector2.Zero) return false;
        _producedThrust = true;
        return true;
    }

    public bool End(ThrusterPulseEnd end)
    {
        if (!end.Ended || !_burning || end.Identity != _identity ||
            !Enum.IsDefined(end.Reason) || end.Reason == ThrusterEndReason.None) return false;
        _burning = false;
        Append(ThrusterTransitionKind.Ended, end.Identity, end.Reason);
        _coastRemaining = _producedThrust &&
            end.Reason is ThrusterEndReason.Exhausted or ThrusterEndReason.ReverseCancelled
            ? MaximumCoast : 0f;
        if (_coastRemaining == 0f) ClearObservation();
        return true;
    }

    public void Step(float deltaSeconds, bool braking, ThrusterCoastExit exit)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if ((exit & ~(ThrusterCoastExit)1023) != 0) throw new ArgumentOutOfRangeException(nameof(exit));
        if (deltaSeconds == 0f && exit == ThrusterCoastExit.None) return;
        _braking = braking;
        if (exit != ThrusterCoastExit.None)
        {
            _coastRemaining = 0f; _producedThrust = false;
            if (!_burning) _identity = default;
            return;
        }
        if (_coastRemaining > 0f)
        {
            _coastRemaining = Math.Max(0f, _coastRemaining - deltaSeconds);
            if (_coastRemaining == 0f) ClearObservation();
        }
    }

    public void Synchronize(ThrusterPulseSnapshot pulse)
    {
        if (pulse.OwnerId != _ownerId || pulse.Lifecycle < _life || pulse.ConfigurationEpoch < _epoch)
            return;
        if (pulse.Lifecycle == _life && pulse.ConfigurationEpoch == _epoch) return;
        _life = pulse.Lifecycle; _epoch = pulse.ConfigurationEpoch;
        _burning = false; _braking = false; ClearObservation();
    }

    // Each reader owns a cursor. Return copies, never a mutable model queue.
    // A late reader sees FirstAvailableSequence and must resync, not replay
    // an expired prefix or grow a second unbounded history.
    public IReadOnlyList<ThrusterPresentationTransition> ReadTransitionsAfter(ulong sequence)
    {
        var result = new List<ThrusterPresentationTransition>();
        foreach (var fact in _journal) if (fact.Sequence > sequence) result.Add(fact);
        return result.ToArray();
    }

    private void ClearObservation()
    {
        _identity = default; _producedThrust = false; _coastRemaining = 0f;
    }

    private void Append(ThrusterTransitionKind kind, ThrusterIdentity identity, ThrusterEndReason reason)
    {
        LatestSequence = checked(LatestSequence + 1UL);
        _journal.Enqueue(new(LatestSequence, kind, identity, _direction, reason));
        if (_journal.Count > JournalCapacity) _journal.Dequeue();
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
