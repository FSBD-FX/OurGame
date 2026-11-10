using System;
using Godot;

namespace Knight.Presentation;

public partial class PlayerFormalVisual
{
    private const float GuardImpactOmega = 25f;
    private ulong _guardImpactSource, _guardImpactTransition;
    private Vector2 _guardImpactWorld, _guardImpactVelocity;

    // Production event channel: the controller has already resolved a real,
    // zero-damage normal block. This owns no force, timer or native Resource.
    public void PresentGuardImpact(ulong sourceId, ulong transitionId, Vector2 aim)
    {
        if (sourceId == 0 || transitionId == 0 || !aim.IsFinite() || aim.LengthSquared() < .0001f)
            return;
        Vector2 direction = aim.Normalized();
        if (!direction.IsFinite() || direction.LengthSquared() < .99f) return;
        // A replacement source requires explicit source withdrawal first.
        if (_guardImpactSource != 0 && _guardImpactSource != sourceId) return;
        if (transitionId <= _guardImpactTransition) return;
        _guardImpactSource = sourceId;
        _guardImpactTransition = transitionId;
        _guardImpactVelocity = (_guardImpactVelocity - direction *
            (3f * GuardImpactOmega * MathF.E)).LimitLength(255f);
    }

    public void ClearGuardImpact()
    {
        bool displayed = _guardImpactWorld.LengthSquared() > .00000001f;
        ResetGuardImpactValues();
        // A withdrawn publisher must not leave a compressed arm frozen on screen.
        // Reuse the same frame/time, never integrate another animation step.
        if (displayed && _hasTypedFrame && IsInsideTree() && !IsQueuedForDeletion())
            ApplyPlayerFrameCore(CurrentPlayerFrame, preserveCueOverride: false, resetTransitions: false);
    }

    private void ResetGuardImpactValues()
    {
        _guardImpactSource = _guardImpactTransition = 0;
        _guardImpactWorld = _guardImpactVelocity = Vector2.Zero;
    }

    private Vector2 StepGuardImpactWorld(PlayerPresentationFrame frame, float? elapsed, bool reset)
    {
        if (reset || !frame.PresentationTimeSeconds.HasValue ||
            !double.IsFinite(frame.PresentationTimeSeconds.Value) ||
            frame.Locomotion.State is PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery)
        {
            ResetGuardImpactValues();
            return Vector2.Zero;
        }
        float dt = elapsed ?? 0f;
        if (float.IsFinite(dt) && dt > 0f)
        {
            if (frame.Action.State != PlayerActionState.Shield || frame.Action.Phase == PlayerActionPhase.Recovery)
                _guardImpactVelocity = -GuardImpactOmega * _guardImpactWorld;
            // Exact critically damped free response, independent of frame partition.
            float decay = MathF.Exp(-GuardImpactOmega * dt);
            Vector2 tangent = _guardImpactVelocity + GuardImpactOmega * _guardImpactWorld;
            _guardImpactWorld = (_guardImpactWorld + tangent * dt) * decay;
            _guardImpactVelocity = (_guardImpactVelocity - GuardImpactOmega * tangent * dt) * decay;
            if (_guardImpactWorld.LengthSquared() > 36f)
            {
                Vector2 outward = _guardImpactWorld.Normalized();
                _guardImpactWorld = outward * 6f;
                _guardImpactVelocity -= outward * Mathf.Max(0f, _guardImpactVelocity.Dot(outward));
            }
            if (_guardImpactWorld.LengthSquared() < .00000001f && _guardImpactVelocity.LengthSquared() < .000001f)
                _guardImpactWorld = _guardImpactVelocity = Vector2.Zero;
        }
        return _guardImpactWorld;
    }

    private static Vector2 GuardImpactInLocal(PlayerFormalLayer layer, Vector2 worldOffset)
    {
        Transform2D transform = layer.GlobalTransform;
        float determinant = transform.Determinant();
        if (!float.IsFinite(determinant) || MathF.Abs(determinant) < .00001f) return Vector2.Zero;
        Vector2 local = transform.AffineInverse().BasisXform(worldOffset);
        return local.IsFinite() ? local : Vector2.Zero;
    }
}
