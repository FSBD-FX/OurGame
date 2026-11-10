using System;
using Godot;
using Knight.Player;

namespace Knight.Presentation;

public readonly record struct PlayerGroundBrakeFrame(bool Observed, bool Braking,
    bool Reverse, bool CrossedZero, float Strength, float PelvisDrop, float TorsoLeanWorld);

/// <summary>Value-only control load. Never integrates physical velocity or owns native objects.</summary>
public sealed class PlayerGroundBrakeModel
{
    private bool _hasSample;
    private ulong _owner, _revision, _tick;
    private Vector2 _root;
    private float _strength, _leanWorld;
    private PlayerGroundBrakeFrame _frame;

    public PlayerGroundBrakeFrame Step(PlayerGroundMotionObservation sample, PlayerGroundSample ground,
        bool grounded, bool suppressed, float delta)
    {
        if (sample.OwnerId == 0 || suppressed || !grounded || !sample.Grounded || !sample.Eligible)
            return Prime(sample, ground);
        if (!_hasSample || sample.OwnerId != _owner || sample.Revision != _revision ||
            sample.PhysicsTick < _tick || sample.PhysicsTick - _tick > 8 ||
            !ground.RootWorld.IsFinite() || !_root.IsFinite() || ground.RootWorld.DistanceSquaredTo(_root) > 4096)
            return Prime(sample, ground);
        // Loss of physical support invalidates cached load even when render
        // time cannot advance. Prime consumes that rejected stamp as well.
        if (!Valid(sample, ground)) return Prime(sample, ground);
        // A valid zero-time notification may precede Motor in the same tick.
        if (!float.IsFinite(delta) || delta <= 0f) return _frame;
        if (sample.PhysicsTick == _tick) return _frame;

        double elapsed = (sample.PhysicsTick - _tick) * (double)sample.StepSeconds;
        double before = sample.BeforeX, after = sample.IntegratedX;
        double lost = sample.BrakeReachedZero ? Math.Abs(before) : Math.Abs(before) - Math.Abs(after);
        bool braking = Math.Abs(before) > 8d && lost > .001d &&
            (sample.BrakeReachedZero || before * after >= 0d);
        double target = braking ? Math.Clamp(Math.Abs(before) / 260d, 0d, 1d) *
            Math.Clamp(lost / sample.StepSeconds / 3200d, 0d, 1d) : 0d;
        double response = 1d - Math.Exp(-elapsed / (braking ? .035d : .10d));
        _strength = (float)(_strength + (target - _strength) * response);
        double lean = braking ? -Math.Sign(before) * .13d * target : 0d;
        _leanWorld = (float)(_leanWorld + (lean - _leanWorld) * response);
        if (_strength < .0001f) _strength = _leanWorld = 0f;
        _tick = sample.PhysicsTick; _root = ground.RootWorld;
        return _frame = new(true, braking, braking && sample.ReverseBraking,
            braking && sample.BrakeReachedZero, _strength, 2.2f * _strength, _leanWorld);
    }

    private PlayerGroundBrakeFrame Prime(PlayerGroundMotionObservation sample, PlayerGroundSample ground)
    {
        _hasSample = sample.OwnerId != 0;
        _owner = sample.OwnerId; _revision = sample.Revision; _tick = sample.PhysicsTick;
        _root = ground.RootWorld; _strength = _leanWorld = 0f;
        return _frame = default;
    }

    private static bool Valid(PlayerGroundMotionObservation sample, PlayerGroundSample ground)
    {
        if (sample.PhysicsTick == 0 || ground.PhysicsTick != sample.PhysicsTick ||
            !float.IsFinite(sample.StepSeconds) || sample.StepSeconds <= 0f || sample.StepSeconds > .25f ||
            !float.IsFinite(sample.BeforeX) || !float.IsFinite(sample.IntegratedX) ||
            !float.IsFinite(sample.ActualX) || !float.IsFinite(sample.MoveAxis) || Mathf.Abs(sample.MoveAxis) > 1f ||
            !ground.Observed || !ground.RootWorld.IsFinite() || ground.SupportId == 0 || !ground.HasBounds ||
            !ground.SupportBounds.Position.IsFinite() || !ground.SupportBounds.Size.IsFinite() ||
            ground.SupportBounds.Size.X <= 0 || ground.SupportBounds.Size.Y <= 0 ||
            !ground.GroundPoint.IsFinite() || !ground.GroundNormal.IsFinite() ||
            ground.GroundNormal.LengthSquared() < .25f || ground.GroundNormal.Normalized().Dot(Vector2.Up) <= .5f ||
            !ground.SupportTransform.X.IsFinite() || !ground.SupportTransform.Y.IsFinite() ||
            !ground.SupportTransform.Origin.IsFinite() || !float.IsFinite(ground.SupportTransform.Determinant()) ||
            Mathf.Abs(ground.SupportTransform.Determinant()) < .000001f) return false;
        if (sample.ReverseBraking != ((double)sample.BeforeX * sample.MoveAxis < 0d)) return false;
        if (sample.BrakeReachedZero && (!sample.ReverseBraking ||
            (double)sample.BeforeX * sample.IntegratedX > 0d)) return false;
        return true;
    }
}
