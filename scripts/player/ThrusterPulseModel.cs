using System;
using Godot;
using Knight.Config;

namespace Knight.Player;

public readonly record struct ThrusterIdentity(ulong OwnerId, ulong Lifecycle,
    ulong ConfigurationEpoch, ulong BurnId);

public enum ThrusterRejectReason
{
    None, NoPress, InvalidSample, Dead, ControlsBlocked, GuardHeld, Burning,
    NoAirUse, DuplicateSample, StaleRequest, InvalidDirection, StaleDecision
}

public enum ThrusterEndReason
{
    None, Exhausted, ReverseCancelled, ClawTakeover, ExternalReplacement, Reset, Death, OwnerRetired
}

public readonly record struct ThrusterPulseStartRequest(ulong SampleSequence,
    bool ShiftPressed, bool RawJumpPressed, bool OnFloor, bool Alive,
    bool ControlsBlocked, bool GuardHeld, Vector2 Direction, ulong Lifecycle,
    ulong ConfigurationEpoch);

// No public constructor or init setters: a decision is a model-bound value,
// not an authority grant that a presentation consumer may manufacture.
public readonly struct ThrusterPulseStartDecision
{
    internal object OwnerStamp { get; }
    internal ulong Revision { get; }
    internal ThrusterPulseStartRequest Request { get; }
    internal Vector2 Direction { get; }
    public bool CanStart => Reason == ThrusterRejectReason.None && OwnerStamp != null;
    public ThrusterRejectReason Reason { get; }

    internal ThrusterPulseStartDecision(object ownerStamp, ulong revision,
        ThrusterPulseStartRequest request, Vector2 direction, ThrusterRejectReason reason)
    {
        OwnerStamp = ownerStamp;
        Revision = revision;
        Request = request;
        Direction = direction;
        Reason = reason;
    }
}

public readonly record struct ThrusterPulseCommitResult(bool Accepted,
    ThrusterRejectReason Reason, ThrusterIdentity Identity);

public readonly record struct ThrusterPulseEnd(bool Ended, ThrusterIdentity Identity,
    ThrusterEndReason Reason);

public readonly record struct ThrusterPulseSnapshot(MobilityModuleSettings Module,
    ulong OwnerId, ulong Lifecycle, ulong ConfigurationEpoch, ulong Revision,
    float BurnRemaining, Vector2 BurnDirection, bool AirUseAvailable,
    ThrusterIdentity ActiveIdentity, int AcceptedCount, ulong LastCommittedSample,
    ulong LastLandingId, ulong BurnSerial);

// Owns finite pulse bookkeeping, never body velocity or world eligibility.
// Motor must additionally consume rejected input samples and revalidate world
// facts after external callbacks; Preview deliberately has no side effects.
public sealed class ThrusterPulseModel
{
    private readonly object _ownerStamp = new();
    public MobilityModuleSettings Module { get; private set; }
    public ulong OwnerId { get; }
    public ulong Lifecycle { get; private set; } = 1UL;
    public ulong ConfigurationEpoch { get; private set; } = 1UL;
    public ulong Revision { get; private set; }
    public float BurnRemaining { get; private set; }
    public Vector2 BurnDirection { get; private set; } = Vector2.Right;
    public bool AirUseAvailable { get; private set; } = true;
    public ThrusterIdentity ActiveIdentity { get; private set; }
    public int AcceptedCount { get; private set; }
    public ulong LastCommittedSample { get; private set; }
    public ulong LastLandingId { get; private set; }
    public ulong BurnSerial { get; private set; }
    public bool IsBurning => BurnRemaining > 0f;
    public ThrusterPulseSnapshot Snapshot => new(Module, OwnerId, Lifecycle,
        ConfigurationEpoch, Revision, BurnRemaining, BurnDirection, AirUseAvailable,
        ActiveIdentity, AcceptedCount, LastCommittedSample, LastLandingId, BurnSerial);

    public ThrusterPulseModel(MobilityModuleSettings module, ulong ownerId)
    {
        var errors = MobilityModuleDefinition.Validate(module);
        if (errors.Count > 0) throw new ArgumentException(string.Join("; ", errors), nameof(module));
        if (ownerId == 0UL) throw new ArgumentOutOfRangeException(nameof(ownerId));
        Module = module;
        OwnerId = ownerId;
    }

    public ThrusterPulseStartDecision Preview(ThrusterPulseStartRequest request)
    {
        ThrusterRejectReason rejection = Check(request);
        Vector2 direction = rejection == ThrusterRejectReason.None
            ? Normalize(request.Direction) : Vector2.Zero;
        return new ThrusterPulseStartDecision(_ownerStamp, Revision, request, direction, rejection);
    }

    public ThrusterPulseCommitResult TryCommit(ThrusterPulseStartDecision decision)
    {
        if (!ReferenceEquals(decision.OwnerStamp, _ownerStamp) || decision.Revision != Revision)
            return new(false, ThrusterRejectReason.StaleDecision, default);
        if (!decision.CanStart) return new(false, decision.Reason, default);
        if (Check(decision.Request) != ThrusterRejectReason.None)
            return new(false, ThrusterRejectReason.StaleDecision, default);
        ulong nextRevision = checked(Revision + 1UL), nextBurn = checked(BurnSerial + 1UL);
        int nextCount = checked(AcceptedCount + 1);
        BurnDirection = decision.Direction;
        BurnRemaining = Module.BurnDuration;
        if (!decision.Request.OnFloor || decision.Request.RawJumpPressed) AirUseAvailable = false;
        BurnSerial = nextBurn;
        AcceptedCount = nextCount;
        LastCommittedSample = decision.Request.SampleSequence;
        ActiveIdentity = new(OwnerId, Lifecycle, ConfigurationEpoch, nextBurn);
        Revision = nextRevision;
        return new(true, ThrusterRejectReason.None, ActiveIdentity);
    }

    public ThrusterPulseEnd ApplyDynamics(ThrusterIdentity identity, float remaining,
        bool cancelledForReverse)
    {
        if (!IsBurning || identity != ActiveIdentity) return default;
        if (!float.IsFinite(remaining) || remaining < 0f || remaining > BurnRemaining ||
            cancelledForReverse && remaining != 0f)
            throw new ArgumentOutOfRangeException(nameof(remaining), "remaining must be finite, nonincreasing and zero on reverse cancellation");
        if (remaining == 0f)
            return End(cancelledForReverse ? ThrusterEndReason.ReverseCancelled : ThrusterEndReason.Exhausted);
        if (remaining != BurnRemaining)
        {
            ulong nextRevision = checked(Revision + 1UL);
            BurnRemaining = remaining;
            Revision = nextRevision;
        }
        return default;
    }

    public ThrusterPulseEnd Interrupt(ThrusterEndReason reason)
    {
        ValidateReason(reason);
        if (IsBurning) return End(reason);
        Revision = checked(Revision + 1UL);
        return default;
    }

    public ThrusterPulseEnd ResetLifecycle(ThrusterEndReason reason)
    {
        ValidateReason(reason);
        ulong nextLife = checked(Lifecycle + 1UL), nextRevision = checked(Revision + 1UL);
        ThrusterPulseEnd ended = IsBurning ? new(true, ActiveIdentity, reason) : default;
        BurnRemaining = 0f;
        ActiveIdentity = default;
        BurnDirection = Vector2.Right;
        AirUseAvailable = true;
        AcceptedCount = 0;
        LastLandingId = 0UL;
        Lifecycle = nextLife;
        Revision = nextRevision;
        return ended;
    }

    // Notice must come from the Motor's observed real landing, not a key,
    // grapple callback, ceiling contact, sector entry or presentation timeout.
    public bool ObserveLanding(ulong expectedLifecycle, ulong landingId)
    {
        if (expectedLifecycle != Lifecycle || landingId == 0UL || landingId <= LastLandingId) return false;
        ulong nextRevision = checked(Revision + 1UL);
        LastLandingId = landingId;
        AirUseAvailable = true;
        Revision = nextRevision;
        return true;
    }

    // Internal value commit only. Actual two-tick stable support and all other
    // equip qualifications remain mandatory in the future public Motor entry.
    internal bool TryReplaceIdleModule(MobilityModuleSettings module)
    {
        if (IsBurning || MobilityModuleDefinition.Validate(module).Count != 0) return false;
        ulong nextEpoch = checked(ConfigurationEpoch + 1UL), nextRevision = checked(Revision + 1UL);
        Module = module;
        ConfigurationEpoch = nextEpoch;
        Revision = nextRevision;
        return true;
    }

    private ThrusterRejectReason Check(ThrusterPulseStartRequest request)
    {
        if (!request.ShiftPressed) return ThrusterRejectReason.NoPress;
        if (request.SampleSequence == 0UL) return ThrusterRejectReason.InvalidSample;
        if (request.Lifecycle != Lifecycle || request.ConfigurationEpoch != ConfigurationEpoch)
            return ThrusterRejectReason.StaleRequest;
        if (request.SampleSequence <= LastCommittedSample) return ThrusterRejectReason.DuplicateSample;
        if (!request.Alive) return ThrusterRejectReason.Dead;
        if (request.ControlsBlocked) return ThrusterRejectReason.ControlsBlocked;
        if (request.GuardHeld) return ThrusterRejectReason.GuardHeld;
        if (IsBurning) return ThrusterRejectReason.Burning;
        if (!request.OnFloor && !AirUseAvailable) return ThrusterRejectReason.NoAirUse;
        if (!float.IsFinite(request.Direction.X) || !float.IsFinite(request.Direction.Y) || request.Direction == Vector2.Zero)
            return ThrusterRejectReason.InvalidDirection;
        return ThrusterRejectReason.None;
    }

    private ThrusterPulseEnd End(ThrusterEndReason reason)
    {
        ulong nextRevision = checked(Revision + 1UL);
        var ended = new ThrusterPulseEnd(true, ActiveIdentity, reason);
        BurnRemaining = 0f;
        ActiveIdentity = default;
        Revision = nextRevision;
        return ended;
    }

    private static void ValidateReason(ThrusterEndReason reason)
    {
        if (!Enum.IsDefined(reason) || reason == ThrusterEndReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
    }

    private static Vector2 Normalize(Vector2 value)
    {
        double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
        return new Vector2((float)(value.X / length), (float)(value.Y / length));
    }
}
