using System;
using Godot;

namespace Knight.Player;

public enum LandingImpactFallbackReason
{
    Missing,
    Reliable,
    NotAirborne,
    WallContact,
    PlatformCarry,
    InvalidMotion,
    InitialOverlap,
    NoPredictedContact,
    MultipleContacts,
    InvalidContact,
    Recovery,
    MotionMismatch,
    ColliderChanged,
    UnknownColliderVelocity,
    RotatingSupport,
    IncompleteSupportQuery
}

public readonly record struct LandingImpactSample(float Speed, LandingImpactFallbackReason Reason)
{
    public bool IsReliable => Reason == LandingImpactFallbackReason.Reliable &&
        float.IsFinite(Speed) && Speed >= 0f;
    public static LandingImpactSample Unavailable(LandingImpactFallbackReason reason) =>
        new(float.NaN, reason);

    // This validates the numerical measurement, not native contact eligibility.
    // The sampler calls it only after the complete motion/support proof succeeds.
    public static LandingImpactSample MeasureRelativeNormal(Vector2 bodyVelocity,
        Vector2 contactPointVelocity, Vector2 normal)
    {
        if (!float.IsFinite(bodyVelocity.X) || !float.IsFinite(bodyVelocity.Y) ||
            !float.IsFinite(contactPointVelocity.X) || !float.IsFinite(contactPointVelocity.Y))
            return Unavailable(LandingImpactFallbackReason.InvalidMotion);
        if (!float.IsFinite(normal.X) || !float.IsFinite(normal.Y) ||
            Math.Abs(normal.LengthSquared() - 1f) >= .001f || normal.Dot(Vector2.Up) <= 0f)
            return Unavailable(LandingImpactFallbackReason.InvalidContact);
        float speed = -(bodyVelocity - contactPointVelocity).Dot(normal);
        return float.IsFinite(speed) ? new(Math.Max(0f, speed), LandingImpactFallbackReason.Reliable)
            : Unavailable(LandingImpactFallbackReason.InvalidMotion);
    }
}

internal readonly record struct LandingImpactPreparation(Vector2 Origin, Vector2 Velocity,
    Vector2 Motion, Vector2 Travel, Vector2 Remainder, Vector2 Normal, Rid Collider,
    Vector2 ColliderVelocity, float ContactDepth, LandingImpactFallbackReason Reason);

internal readonly record struct LandingFloorSupport(bool AllSoft, bool Unique, bool Complete);

// Queries only: the Motor still makes exactly one native MoveAndSlide per tick.
internal sealed class LandingImpactSampler : IDisposable
{
    private const float MotionTolerance = .002f;
    private readonly KinematicCollision2D _overlap = new();
    private readonly KinematicCollision2D _predicted = new();
    private readonly KinematicCollision2D _motionOnly = new();
    private readonly PhysicsTestMotionParameters2D _supportParameters = new();
    private readonly PhysicsTestMotionResult2D _supportResult = new();
    private readonly Godot.Collections.Array<Rid> _excluded = new();

    internal LandingImpactPreparation Prepare(CharacterBody2D body, Vector2 velocity,
        bool wasOnFloor, double delta)
    {
        LandingImpactPreparation unavailable = new(body.GlobalPosition, velocity, default,
            default, default, default, default, default, 0f, LandingImpactFallbackReason.InvalidMotion);
        if (wasOnFloor) return unavailable with { Reason = LandingImpactFallbackReason.NotAirborne };
        if (body.IsOnWall()) return unavailable with { Reason = LandingImpactFallbackReason.WallContact };
        Vector2 platformVelocity = body.GetPlatformVelocity();
        if (!Finite(platformVelocity) || platformVelocity != Vector2.Zero)
            return unavailable with { Reason = LandingImpactFallbackReason.PlatformCarry };
        if (!Finite(velocity) || !Finite(body.GlobalTransform.Origin) ||
            !Finite(body.GlobalTransform.X) || !Finite(body.GlobalTransform.Y) ||
            !double.IsFinite(delta) || delta <= 0.0 || delta > float.MaxValue)
            return unavailable;
        Vector2 motion = velocity * (float)delta;
        unavailable = unavailable with { Motion = motion };
        if (!Finite(motion) || motion.LengthSquared() == 0f) return unavailable;
        if (body.TestMove(body.GlobalTransform, Vector2.Zero, _overlap, body.SafeMargin, true))
            return unavailable with { Reason = LandingImpactFallbackReason.InitialOverlap };
        if (!body.TestMove(body.GlobalTransform, motion, _predicted, body.SafeMargin, true))
            return unavailable with { Reason = LandingImpactFallbackReason.NoPredictedContact };
        Vector2 normal = _predicted.GetNormal();
        if (!FloorNormal(body, normal) || !Finite(_predicted.GetTravel()) ||
            !Finite(_predicted.GetRemainder()) || !float.IsFinite(_predicted.GetDepth()))
            return unavailable with { Reason = LandingImpactFallbackReason.InvalidContact };
        // GetDepth describes overlap at the detected future contact, not necessarily
        // initial recovery. A normal native floor hit can have positive depth.
        // Demand no zero-motion recovery AND identical motion-only/recovery queries.
        if (!body.TestMove(body.GlobalTransform, motion, _motionOnly, body.SafeMargin, false) ||
            _motionOnly.GetColliderRid() != _predicted.GetColliderRid() ||
            !Near(_motionOnly.GetTravel(), _predicted.GetTravel()) ||
            !Near(_motionOnly.GetRemainder(), _predicted.GetRemainder()) ||
            !Near(_motionOnly.GetNormal(), normal) ||
            !float.IsFinite(_motionOnly.GetDepth()) ||
            Math.Abs(_motionOnly.GetDepth() - _predicted.GetDepth()) > MotionTolerance)
            return unavailable with { Reason = LandingImpactFallbackReason.Recovery };
        if (!Near(_predicted.GetTravel() + _predicted.GetRemainder(), motion))
            return unavailable with { Reason = LandingImpactFallbackReason.MotionMismatch };
        float travelFraction = _predicted.GetTravel().Dot(motion) / motion.LengthSquared();
        if (!float.IsFinite(travelFraction) || travelFraction <= 0f || travelFraction > 1f ||
            !Near(_predicted.GetTravel(), motion * travelFraction))
            return unavailable with { Reason = LandingImpactFallbackReason.MotionMismatch };
        LandingImpactFallbackReason velocityReason = ContactVelocity(_predicted.GetColliderRid(),
            _predicted.GetPosition(), out Vector2 colliderVelocity);
        if (velocityReason != LandingImpactFallbackReason.Reliable)
            return unavailable with { Reason = velocityReason };
        if (!Near(colliderVelocity, _predicted.GetColliderVelocity()))
            return unavailable with { Reason = LandingImpactFallbackReason.UnknownColliderVelocity };
        return new(body.GlobalPosition, velocity, motion, _predicted.GetTravel(),
            _predicted.GetRemainder(), normal, _predicted.GetColliderRid(), colliderVelocity,
            _predicted.GetDepth(), LandingImpactFallbackReason.Reliable);
    }

    internal LandingImpactSample Complete(CharacterBody2D body, LandingImpactPreparation before,
        LandingFloorSupport support)
    {
        if (!support.Complete)
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.IncompleteSupportQuery);
        if (!support.Unique || body.GetSlideCollisionCount() != 1)
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.MultipleContacts);
        if (before.Reason != LandingImpactFallbackReason.Reliable)
            return LandingImpactSample.Unavailable(before.Reason);
        KinematicCollision2D contact = body.GetSlideCollision(0); // Borrowed; never Dispose.
        if (!FloorNormal(body, contact.GetNormal()) || !Finite(contact.GetTravel()) ||
            !Finite(contact.GetRemainder()) || !float.IsFinite(contact.GetDepth()))
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.InvalidContact);
        if (Math.Abs(contact.GetDepth() - before.ContactDepth) > MotionTolerance)
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.Recovery);
        if (contact.GetColliderRid() != before.Collider)
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.ColliderChanged);
        Vector2 expectedTravel = contact.GetTravel() + contact.GetRemainder().Slide(contact.GetNormal());
        if (!Near(contact.GetNormal(), before.Normal) || !Near(contact.GetTravel(), before.Travel) ||
            !Near(contact.GetRemainder(), before.Remainder) ||
            !Near(contact.GetTravel() + contact.GetRemainder(), before.Motion) ||
            !Near(body.GlobalPosition - before.Origin, expectedTravel))
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.MotionMismatch);
        LandingImpactFallbackReason velocityReason = ContactVelocity(contact.GetColliderRid(),
            contact.GetPosition(), out Vector2 colliderVelocity);
        if (velocityReason != LandingImpactFallbackReason.Reliable)
            return LandingImpactSample.Unavailable(velocityReason);
        if (!Near(colliderVelocity, before.ColliderVelocity) || !Near(colliderVelocity, contact.GetColliderVelocity()))
            return LandingImpactSample.Unavailable(LandingImpactFallbackReason.UnknownColliderVelocity);
        return LandingImpactSample.MeasureRelativeNormal(before.Velocity,
            before.ColliderVelocity, before.Normal);
    }

    internal LandingFloorSupport InspectFloorSupport(CharacterBody2D body)
    {
        _excluded.Clear();
        int floors = 0;
        bool allSoft = true;
        for (int index = 0; index < body.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D contact = body.GetSlideCollision(index);
            _excluded.Add(contact.GetColliderRid());
            if (!FloorNormal(body, contact.GetNormal())) continue;
            floors++;
            allSoft &= ExplicitSoft(contact.GetCollider());
        }
        // Slide count is not a complete support inventory. Find additional touching
        // bodies without disabling collision or modifying the body's exceptions.
        _supportParameters.From = body.GlobalTransform;
        _supportParameters.Motion = -body.UpDirection.Normalized() * .001f;
        _supportParameters.Margin = body.SafeMargin;
        _supportParameters.RecoveryAsCollision = true;
        for (int index = 0; index < 16; index++)
        {
            _supportParameters.ExcludeBodies = _excluded;
            if (!PhysicsServer2D.BodyTestMotion(body.GetRid(), _supportParameters, _supportResult))
                return new(floors > 0 && allSoft, floors == 1, true);
            Rid collider = _supportResult.GetColliderRid();
            if (!collider.IsValid || _excluded.Contains(collider) ||
                !Finite(_supportResult.GetCollisionNormal()))
                return new(false, false, false);
            _excluded.Add(collider);
            if (!FloorNormal(body, _supportResult.GetCollisionNormal())) continue;
            floors++;
            allSoft &= ExplicitSoft(_supportResult.GetCollider());
        }
        return new(false, false, false);
    }

    private static LandingImpactFallbackReason ContactVelocity(Rid collider, Vector2 point,
        out Vector2 velocity)
    {
        velocity = default;
        if (!collider.IsValid || !Finite(point)) return LandingImpactFallbackReason.UnknownColliderVelocity;
        PhysicsDirectBodyState2D state = PhysicsServer2D.BodyGetDirectState(collider); // Borrowed.
        if (state == null || !float.IsFinite(state.AngularVelocity) || !Finite(state.Transform.Origin))
            return LandingImpactFallbackReason.UnknownColliderVelocity;
        if (state.AngularVelocity != 0f) return LandingImpactFallbackReason.RotatingSupport;
        // This API's argument is an offset in GLOBAL coordinates, despite its name.
        velocity = state.GetVelocityAtLocalPosition(point - state.Transform.Origin);
        return Finite(velocity) ? LandingImpactFallbackReason.Reliable
            : LandingImpactFallbackReason.UnknownColliderVelocity;
    }

    private static bool ExplicitSoft(GodotObject collider) => collider != null &&
        GodotObject.IsInstanceValid(collider) && collider.HasMeta("soft_landing") &&
        collider.GetMeta("soft_landing").VariantType == Variant.Type.Bool &&
        collider.GetMeta("soft_landing").AsBool();

    private static bool FloorNormal(CharacterBody2D body, Vector2 normal) => Finite(normal) &&
        Math.Abs(normal.LengthSquared() - 1f) < .001f && Finite(body.UpDirection) &&
        body.UpDirection.LengthSquared() > 0f && float.IsFinite(body.FloorMaxAngle) &&
        normal.Dot(Vector2.Up) > 0f &&
        normal.Dot(body.UpDirection.Normalized()) >= Mathf.Cos(body.FloorMaxAngle);

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private static bool Near(Vector2 a, Vector2 b) => Finite(a) && Finite(b) &&
        (a - b).LengthSquared() <= MotionTolerance * MotionTolerance;

    public void Dispose()
    {
        _overlap.Dispose();
        _predicted.Dispose();
        _motionOnly.Dispose();
        _supportParameters.Dispose();
        _supportResult.Dispose();
        ((Godot.Collections.Array)_excluded).Dispose();
    }
}
