using System;
using Godot;

namespace Knight.Abilities;

/// <summary>
/// Deterministic state and rope-constraint calculation. The caller supplies the latest caster
/// position and velocity by carrying them forward in <see cref="MechanicalClawFrame"/>; this
/// model returns a velocity correction and never writes a position or queries the scene tree.
/// </summary>
public sealed class MechanicalClawModel
{
    private const float Epsilon = .0001f;
    private readonly MechanicalClawSettings _settings;

    public MechanicalClawModel(MechanicalClawSettings? settings = null)
    {
        _settings = settings ?? MechanicalClawSettings.Default;
        _settings.Validate();
    }

    public MechanicalClawFrame Step(MechanicalClawFrame previous, MechanicalClawInput input,
        MechanicalClawTargetSnapshot target, float delta)
    {
        float seconds = Mathf.Max(0f, delta);
        previous = Normalize(previous);

        return previous.State switch
        {
            MechanicalClawState.Idle => StepIdle(previous, input, target),
            MechanicalClawState.Aim => StepAim(previous, input, target),
            MechanicalClawState.Windup => StepWindup(previous, input, target),
            MechanicalClawState.Travel => StepTravel(previous, input, target, seconds),
            MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing =>
                StepAttached(previous, input, target, seconds),
            MechanicalClawState.Boost => StepAttached(previous, input, target, seconds),
            MechanicalClawState.Released => ToRetract(previous),
            MechanicalClawState.Retract => StepRetract(previous, input, seconds),
            MechanicalClawState.Failed => previous.CastId == 0UL ? ToIdle(previous) : ToRetract(previous),
            _ => ToIdle(previous)
        };
    }

    private MechanicalClawFrame StepIdle(MechanicalClawFrame previous, MechanicalClawInput input,
        MechanicalClawTargetSnapshot target)
    {
        if (input.Command != MechanicalClawCommand.Cast) return ToIdle(previous);
        return new MechanicalClawFrame(
            CastId: 0UL,
            State: MechanicalClawState.Aim,
            Failure: MechanicalClawFailure.None,
            AnchorPoint: target.WorldAnchor,
            RopeLength: 0f,
            VelocityCorrection: Vector2.Zero,
            EffectiveVisibility: IsEffectivelyVisible(target, previous.CasterWorldPosition),
            LockedTargetRuntimeId: 0UL,
            CasterWorldPosition: previous.CasterWorldPosition,
            CasterVelocity: previous.CasterVelocity,
            ShuttleDistance: 0f,
            NextCastId: previous.NextCastId,
            SurfaceNormal: target.SurfaceNormal,
            DockPoint: Vector2.Zero,
            Arrived: false,
            ReleaseKind: MechanicalClawReleaseKind.None);
    }

    private MechanicalClawFrame StepAim(MechanicalClawFrame previous, MechanicalClawInput input,
        MechanicalClawTargetSnapshot target)
    {
        if (IsCancellation(input.Command))
            return ToReleased(previous, MechanicalClawReleaseKind.Cancel);

        MechanicalClawFailure failure = ValidateTarget(target, previous.CasterWorldPosition);
        if (failure != MechanicalClawFailure.None) return Fail(previous, failure);

        ulong castId = previous.NextCastId;
        ulong nextCastId = castId == ulong.MaxValue ? 1UL : castId + 1UL;
        return new MechanicalClawFrame(
            CastId: castId,
            State: MechanicalClawState.Windup,
            Failure: MechanicalClawFailure.None,
            AnchorPoint: target.WorldAnchor,
            RopeLength: 0f,
            VelocityCorrection: Vector2.Zero,
            EffectiveVisibility: true,
            LockedTargetRuntimeId: 0UL,
            CasterWorldPosition: previous.CasterWorldPosition,
            CasterVelocity: previous.CasterVelocity,
            ShuttleDistance: 0f,
            NextCastId: nextCastId,
            SurfaceNormal: target.SurfaceNormal,
            DockPoint: Vector2.Zero,
            Arrived: false,
            ReleaseKind: MechanicalClawReleaseKind.None);
    }

    private MechanicalClawFrame StepWindup(MechanicalClawFrame previous, MechanicalClawInput input,
        MechanicalClawTargetSnapshot target)
    {
        if (IsCancellation(input.Command))
            return ToReleased(previous, MechanicalClawReleaseKind.Cancel);

        MechanicalClawFailure failure = ValidateTarget(target, previous.CasterWorldPosition);
        if (failure != MechanicalClawFailure.None) return Fail(previous, failure);

        return new MechanicalClawFrame(
            CastId: previous.CastId,
            State: MechanicalClawState.Travel,
            Failure: MechanicalClawFailure.None,
            AnchorPoint: target.WorldAnchor,
            RopeLength: 0f,
            VelocityCorrection: Vector2.Zero,
            EffectiveVisibility: true,
            LockedTargetRuntimeId: target.RuntimeId,
            CasterWorldPosition: previous.CasterWorldPosition,
            CasterVelocity: previous.CasterVelocity,
            ShuttleDistance: 0f,
            NextCastId: previous.NextCastId,
            SurfaceNormal: target.SurfaceNormal,
            DockPoint: previous.DockPoint,
            Arrived: false,
            ReleaseKind: MechanicalClawReleaseKind.None);
    }

    private MechanicalClawFrame StepTravel(MechanicalClawFrame previous, MechanicalClawInput input,
        MechanicalClawTargetSnapshot target, float seconds)
    {
        if (!HasLockedTarget(target, previous)) return Fail(previous, MechanicalClawFailure.TargetLost);
        if (IsCancellation(input.Command))
            return ToReleased(previous, MechanicalClawReleaseKind.Cancel);

        float targetDistance = previous.CasterWorldPosition.DistanceTo(previous.AnchorPoint);
        float shuttleDistance = Mathf.Min(targetDistance,
            previous.ShuttleDistance + _settings.ShuttleSpeed * seconds);
        if (shuttleDistance + Epsilon < targetDistance)
        {
            return previous with
            {
                ShuttleDistance = shuttleDistance,
                EffectiveVisibility = IsEffectivelyVisible(target, previous.CasterWorldPosition),
                VelocityCorrection = Vector2.Zero,
                ReleaseKind = MechanicalClawReleaseKind.None,
                Arrived = false
            };
        }

        return previous with
        {
            State = MechanicalClawState.Latched,
            Failure = MechanicalClawFailure.None,
            RopeLength = Mathf.Clamp(targetDistance, _settings.MinimumRopeLength, _settings.MaxRange),
            VelocityCorrection = Vector2.Zero,
            ShuttleDistance = targetDistance,
            EffectiveVisibility = IsEffectivelyVisible(target, previous.CasterWorldPosition),
            BoostConsumed = false,
            ReleaseKind = MechanicalClawReleaseKind.None,
            Arrived = false
        };
    }

    private MechanicalClawFrame StepAttached(MechanicalClawFrame previous, MechanicalClawInput input,
        MechanicalClawTargetSnapshot target, float seconds)
    {
        if (!HasLockedTarget(target, previous)) return Fail(previous, MechanicalClawFailure.TargetLost);
        if (input.Command == MechanicalClawCommand.BoostRelease &&
            previous.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing)
            return ToReleased(previous, MechanicalClawReleaseKind.Boost) with { BoostConsumed = true };
        if (IsCancellation(input.Command))
            return ToReleased(previous, MechanicalClawReleaseKind.Cancel);

        if (previous.State == MechanicalClawState.Swing || input.Arrived)
        {
            // Arrival transfers the measured velocity, not the automatic servo's
            // cancellation of an older free-motion base, into the rope simulation.
            MechanicalClawFrame swingBase = previous.State == MechanicalClawState.Reel
                ? previous with { VelocityCorrection = Vector2.Zero } : previous;
            return previous with
            {
                State = MechanicalClawState.Swing,
                Failure = MechanicalClawFailure.None,
                RopeLength = previous.RopeLength,
                VelocityCorrection = CalculateVelocityCorrection(
                    swingBase, input.SwingAxis, previous.RopeLength, seconds),
                EffectiveVisibility = IsEffectivelyVisible(target, previous.CasterWorldPosition),
                DockPoint = input.DockPoint,
                Arrived = input.Arrived,
                ReleaseKind = MechanicalClawReleaseKind.None
            };
        }

        Vector2 dockPoint = input.DockPoint;
        float stopDistance = previous.AnchorPoint.DistanceTo(dockPoint);
        float ropeLength = Mathf.MoveToward(previous.RopeLength, stopDistance,
            _settings.AutoReelSpeed * seconds);
        float remaining = Mathf.Max(0f,
            previous.CasterWorldPosition.DistanceTo(dockPoint));
        float distanceCap = _settings.MaxPullSpeed *
            Mathf.Clamp(remaining / _settings.ArrivalDecelerationDistance, 0f, 1f);
        float brakingCap = Mathf.Sqrt(2f * _settings.MaxPullAcceleration * remaining);
        float targetwardSpeed = Mathf.Min(distanceCap,
            Mathf.Min(brakingCap, _settings.MaxPullSpeed));
        Vector2 velocityCorrection = CalculateAutoPullCorrection(previous,
            dockPoint, targetwardSpeed, seconds);

        return previous with
        {
            State = MechanicalClawState.Reel,
            Failure = MechanicalClawFailure.None,
            RopeLength = ropeLength,
            VelocityCorrection = velocityCorrection,
            EffectiveVisibility = IsEffectivelyVisible(target, previous.CasterWorldPosition),
            DockPoint = dockPoint,
            Arrived = false,
            ReleaseKind = MechanicalClawReleaseKind.None
        };
    }

    private MechanicalClawFrame StepRetract(MechanicalClawFrame previous, MechanicalClawInput input, float seconds)
    {
        float ropeLength = Mathf.Max(0f, previous.RopeLength - _settings.RetractSpeed * seconds);
        if (ropeLength <= Epsilon) return ToIdle(previous);
        return previous with
        {
            State = MechanicalClawState.Retract,
            Failure = MechanicalClawFailure.None,
            RopeLength = ropeLength,
            VelocityCorrection = Vector2.Zero,
            EffectiveVisibility = false,
            ReleaseKind = MechanicalClawReleaseKind.None
        };
    }

    private Vector2 CalculateAutoPullCorrection(MechanicalClawFrame previous,
        Vector2 dockPoint, float targetwardSpeed, float seconds)
    {
        Vector2 toDock = previous.CasterWorldPosition.DirectionTo(dockPoint);
        Vector2 unconstrainedBase = previous.CasterVelocity - previous.VelocityCorrection;
        // Control the complete approach velocity. A radial-only correction leaves
        // gravity perpendicular to a diagonal rope free to drag the caster down.
        // Steering starts in Swing, after genuine arrival at the saved dock.
        Vector2 boundedVelocity = previous.CasterVelocity.MoveToward(toDock * targetwardSpeed,
            _settings.MaxPullAcceleration * seconds);
        return boundedVelocity - unconstrainedBase;
    }

    private Vector2 CalculateVelocityCorrection(MechanicalClawFrame previous, float requestedSwingAxis,
        float ropeLength, float seconds)
    {
        Vector2 toAnchor = previous.CasterWorldPosition.DirectionTo(previous.AnchorPoint);
        if (toAnchor.IsZeroApprox()) return Vector2.Zero;

        float distance = previous.CasterWorldPosition.DistanceTo(previous.AnchorPoint);
        float radialError = Mathf.Max(0f, distance - ropeLength);
        float radialVelocity = previous.CasterVelocity.Dot(toAnchor);
        float radialAcceleration =
            radialError * _settings.RadialStiffness - radialVelocity * _settings.RadialDamping;
        float radialVelocityCorrection = Mathf.Max(0f,
            previous.VelocityCorrection.Dot(toAnchor) + radialAcceleration * seconds);
        Vector2 radialCorrection = toAnchor * radialVelocityCorrection;

        Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
        float dampedTangentialVelocity = previous.VelocityCorrection.Dot(tangent) *
            Mathf.Exp(-_settings.RadialDamping * seconds);
        float tangentialVelocity = dampedTangentialVelocity +
            Mathf.Clamp(requestedSwingAxis, -1f, 1f) * _settings.SwingAcceleration * seconds;
        Vector2 tangentialCorrection = tangent * tangentialVelocity;

        return radialCorrection + tangentialCorrection;
    }

    private static MechanicalClawFrame ToReleased(MechanicalClawFrame previous,
        MechanicalClawReleaseKind releaseKind)
    {
        Vector2 radialDirection = previous.CasterWorldPosition.DirectionTo(previous.AnchorPoint);
        Vector2 tangentialCorrection = Vector2.Zero;
        if (!radialDirection.IsZeroApprox())
        {
            Vector2 tangentDirection = new(-radialDirection.Y, radialDirection.X);
            tangentialCorrection = tangentDirection * previous.VelocityCorrection.Dot(tangentDirection);
        }

        return previous with
        {
            State = MechanicalClawState.Released,
            Failure = MechanicalClawFailure.None,
            VelocityCorrection = tangentialCorrection,
            EffectiveVisibility = false,
            ReleaseKind = releaseKind
        };
    }

    private MechanicalClawFrame ToRetract(MechanicalClawFrame previous) => previous with
    {
        State = MechanicalClawState.Retract,
        Failure = MechanicalClawFailure.None,
        VelocityCorrection = Vector2.Zero,
        EffectiveVisibility = false,
        ReleaseKind = MechanicalClawReleaseKind.None
    };

    private static MechanicalClawFrame ToIdle(MechanicalClawFrame previous) => previous with
    {
        CastId = 0UL,
        State = MechanicalClawState.Idle,
        Failure = MechanicalClawFailure.None,
        AnchorPoint = Vector2.Zero,
        RopeLength = 0f,
        VelocityCorrection = Vector2.Zero,
        EffectiveVisibility = false,
        LockedTargetRuntimeId = 0UL,
        ShuttleDistance = 0f,
        BoostConsumed = false,
        SurfaceNormal = Vector2.Zero,
        DockPoint = Vector2.Zero,
        Arrived = false,
        ReleaseKind = MechanicalClawReleaseKind.None
    };

    private MechanicalClawFrame Fail(MechanicalClawFrame previous, MechanicalClawFailure failure) => previous with
    {
        State = MechanicalClawState.Failed,
        Failure = failure,
        EffectiveVisibility = false,
        VelocityCorrection = previous.CastId == 0UL ? Vector2.Zero : previous.VelocityCorrection,
        ReleaseKind = MechanicalClawReleaseKind.None
    };

    private static bool IsCancellation(MechanicalClawCommand command) =>
        command is MechanicalClawCommand.Cast or MechanicalClawCommand.Cancel;

    private static bool HasLockedTarget(MechanicalClawTargetSnapshot target, MechanicalClawFrame previous) =>
        previous.LockedTargetRuntimeId != 0UL && target.Exists &&
        target.RuntimeId == previous.LockedTargetRuntimeId;

    private MechanicalClawFailure ValidateTarget(MechanicalClawTargetSnapshot target, Vector2 casterWorldPosition)
    {
        if (!target.Exists || target.RuntimeId == 0UL) return MechanicalClawFailure.NoTarget;
        if (target.Material == GrappleSurfaceMaterial.Ice) return MechanicalClawFailure.IceSlip;
        if (target.Material == GrappleSurfaceMaterial.Forbidden) return MechanicalClawFailure.Forbidden;
        if (casterWorldPosition.DistanceTo(target.WorldAnchor) > _settings.MaxRange)
            return MechanicalClawFailure.OutOfRange;
        if (!target.HasLineOfSight) return MechanicalClawFailure.Obstructed;
        return MechanicalClawFailure.None;
    }

    private bool IsEffectivelyVisible(MechanicalClawTargetSnapshot target, Vector2 casterWorldPosition) =>
        target.Exists && target.RuntimeId != 0UL && target.HasLineOfSight &&
        casterWorldPosition.DistanceTo(target.WorldAnchor) <= _settings.MaxRange;

    private static MechanicalClawFrame Normalize(MechanicalClawFrame previous) => previous with
    {
        RopeLength = Mathf.Max(0f, previous.RopeLength),
        ShuttleDistance = Mathf.Max(0f, previous.ShuttleDistance),
        NextCastId = previous.NextCastId == 0UL ? 1UL : previous.NextCastId
    };
}
