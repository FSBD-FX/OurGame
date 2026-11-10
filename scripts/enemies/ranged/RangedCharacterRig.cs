using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Enemies.Ranged;

public readonly record struct RangedRigLimb(Vector2 Start, Vector2 Joint, Vector2 End);
public readonly record struct RangedRigPose(RangedWeaponState State, float Brace, float Recoil,
    float Collapse, float Charge, float Heat, float AimAngle, Transform2D TorsoTransform,
    Transform2D HeadTransform, Transform2D WeaponTransform, RangedRigLimb FarArm,
    RangedRigLimb NearArm, RangedRigLimb FarLeg, RangedRigLimb NearLeg);

// Value-only, display-time rig. It never owns physics, attack permission or
// native Resources. Publication cannot advance bones; only Advance does that.
public sealed class RangedCharacterRig
{
    private RangedWeaponRole _role;
    private RangedWeaponState _state;
    private float _brace, _collapse, _recoil, _recoilVelocity, _charge, _heat;
    private float _aimAngle, _targetAngle;
    private ulong _attackId, _revokedThrough;
    private int _lastEmission = -1;
    private AttackPhase _acceptedPhase;
    private float _acceptedProgress;

    public void Reset(RangedWeaponRole role)
    {
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        _role = role; _state = RangedWeaponState.Idle;
        _brace = _collapse = _recoil = _recoilVelocity = _charge = _heat = 0f;
        _aimAngle = _targetAngle = 0f; _attackId = _revokedThrough = 0; _lastEmission = -1;
        _acceptedPhase = AttackPhase.None; _acceptedProgress = 0f;
    }

    public void Apply(RangedWeaponFrame frame)
    {
        if (_state == RangedWeaponState.Dead || !Enum.IsDefined(frame.State)) return;
        if (frame.State is RangedWeaponState.Idle or RangedWeaponState.Stunned or RangedWeaponState.Dead)
        {
            _state = frame.State;
            ClearAttack();
            return;
        }
        AttackTimelineFrame timeline = frame.AttackTimeline;
        AttackPhase expected = frame.State switch { RangedWeaponState.Telegraph => AttackPhase.Telegraph,
            RangedWeaponState.Active => AttackPhase.Active, _ => AttackPhase.Recovery };
        EnemyAttackKind kind = _role switch { RangedWeaponRole.Scatter => EnemyAttackKind.RivetScatterShot,
            RangedWeaponRole.Burst => EnemyAttackKind.BeltBurstShot, RangedWeaponRole.Missile => EnemyAttackKind.FurnaceMissileShot,
            _ => EnemyAttackKind.PrismLaserBeam };
        if (timeline.AttackId == 0 || timeline.AttackId <= _revokedThrough || timeline.AttackId < _attackId ||
            timeline.Kind != kind || timeline.Phase != expected || !float.IsFinite(timeline.PhaseDurationSeconds) ||
            timeline.PhaseDurationSeconds <= 0f || !float.IsFinite(timeline.PhaseRemainingSeconds) ||
            timeline.PhaseRemainingSeconds < 0f || timeline.PhaseRemainingSeconds > timeline.PhaseDurationSeconds) return;
        if (timeline.AttackId != _attackId)
        {
            _attackId = timeline.AttackId; _lastEmission = -1;
            _acceptedPhase = AttackPhase.None; _acceptedProgress = 0f;
        }
        float progress = 1f - timeline.PhaseRemainingSeconds / timeline.PhaseDurationSeconds;
        // Emission identity and phase progression are separate: an unseen real
        // tail can still kick during Recovery, but a saved earlier warning or
        // phase cannot restore expired charge, heat, aim or brace authority.
        if ((int)timeline.Phase > (int)_acceptedPhase ||
            (timeline.Phase == _acceptedPhase && progress >= _acceptedProgress))
        {
            _acceptedPhase = timeline.Phase; _acceptedProgress = progress;
            _state = frame.State;
            if (float.IsFinite(frame.AimDirection.X) && float.IsFinite(frame.AimDirection.Y) &&
                frame.AimDirection != Vector2.Zero)
                _targetAngle = MathF.Atan2(frame.AimDirection.Y, frame.AimDirection.X);
            _charge = _state == RangedWeaponState.Telegraph ? progress : 0f;
            _heat = _state == RangedWeaponState.Active ? 1f : _state == RangedWeaponState.Recovery ? 1f - progress : 0f;
        }
        // The saved default is three, but the existing validated settings allow
        // up to eight. Presentation must consume the real interval, not assume
        // the default author's burst length.
        int maximum = _role == RangedWeaponRole.Burst ? 8 : 1;
        long end = (long)frame.FirstEmissionIndex + frame.EmissionsDue - 1L;
        if (frame.FirstEmissionIndex >= 0 && frame.EmissionsDue > 0 && end < maximum &&
            frame.State is RangedWeaponState.Active or RangedWeaponState.Recovery)
        {
            int count = (int)Math.Max(0L, end - Math.Max(_lastEmission, frame.FirstEmissionIndex - 1));
            _lastEmission = Math.Max(_lastEmission, (int)end);
            _recoilVelocity = Math.Min(300f, _recoilVelocity + count * 90f);
        }
    }

    public void ClearAttack()
    {
        _revokedThrough = Math.Max(_revokedThrough, _attackId);
        _charge = _heat = _recoil = _recoilVelocity = 0f;
        if (_state is not (RangedWeaponState.Dead or RangedWeaponState.Stunned)) _state = RangedWeaponState.Idle;
    }

    public void Advance(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0d || delta >= 1d) return;
        float dt = (float)delta;
        float settle = 1f - MathF.Exp(-dt / .075f);
        float target = _state is RangedWeaponState.Telegraph or RangedWeaponState.Active ? 1f :
            _state == RangedWeaponState.Recovery ? _heat : 0f;
        _brace = Mathf.Lerp(_brace, target, settle);
        _collapse = Mathf.Lerp(_collapse, _state == RangedWeaponState.Dead ? 1f : 0f, settle);
        _aimAngle += Mathf.AngleDifference(_aimAngle, _targetAngle) * (1f - MathF.Exp(-dt / .06f));
        // Exact critically damped spring, so render rate does not change recoil.
        const float omega = 22f;
        float coefficient = _recoilVelocity + omega * _recoil;
        float decay = MathF.Exp(-omega * dt);
        _recoil = (_recoil + coefficient * dt) * decay;
        _recoilVelocity = (_recoilVelocity - omega * coefficient * dt) * decay;
    }

    public RangedRigPose Pose
    {
        get
        {
            float facing = MathF.Cos(_aimAngle);
            float lean = _role switch { RangedWeaponRole.Scatter => .10f, RangedWeaponRole.Burst => .065f,
                RangedWeaponRole.Missile => .13f, _ => .045f };
            var torso = new Transform2D(-facing * (lean * _brace + .012f * _recoil) + facing * .55f * _collapse,
                new Vector2(-MathF.Cos(_aimAngle) * _recoil * .22f, _brace * 2f + _collapse * 9f));
            var head = torso * new Transform2D(MathF.Sin(_aimAngle) * .1f, new Vector2(0f, -25f));
            var weapon = new Transform2D(_aimAngle, torso * new Vector2(-MathF.Cos(_aimAngle) * _recoil, -5f));
            float spread = _role switch { RangedWeaponRole.Scatter => 20f, RangedWeaponRole.Burst => 15f,
                RangedWeaponRole.Missile => 22f, _ => 12f };
            return new(_state, _brace, _recoil, _collapse, _charge, _heat, _aimAngle, torso, head, weapon,
                // Keep anatomical elbow branches continuous through overhead.
                // Interpolating a bend sign through zero would shorten bones.
                Solve(torso * new Vector2(-12f, -8f), weapon * new Vector2(7f, 7f), 18f, -1f),
                Solve(torso * new Vector2(12f, -8f), weapon * new Vector2(-7f, 9f), 18f, 1f),
                Solve(torso * new Vector2(-8f, 14f), new Vector2(-spread, 34f), 13f, -1f),
                Solve(torso * new Vector2(8f, 14f), new Vector2(spread, 34f), 13f, 1f));
        }
    }

    private static RangedRigLimb Solve(Vector2 start, Vector2 end, float length, float bend)
    {
        Vector2 offset = end - start;
        float distance = offset.Length();
        Vector2 direction = distance > .00001f ? offset / distance : Vector2.Down;
        if (distance > length * 2f) { distance = length * 2f; end = start + direction * distance; }
        float half = distance * .5f;
        Vector2 joint = start + direction * half + direction.Orthogonal() * (MathF.Sqrt(MathF.Max(0f, length * length - half * half)) * bend);
        return new(start, joint, end);
    }
}

public readonly record struct RangedPartGeometry(Transform2D Transform, Rect2 Canvas,
    Rect2 DrawRect, bool UsesTexture, IReadOnlyList<Vector2> Contour);
