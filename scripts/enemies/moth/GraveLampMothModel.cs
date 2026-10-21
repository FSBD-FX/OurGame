using System;
using Godot;
using Knight.Combat;
using Knight.Config;

namespace Knight.Enemies.Moth;

public sealed class GraveLampMothModel
{
    private readonly EnemyArchetypeSettings _settings;
    private GraveLampMothState _state = GraveLampMothState.Hover;
    private float _stateElapsed;
    private float _diveX;
    private float _diveY;
    private ulong _nextAttackId;
    private ulong _attackId;
    private Vector2 _lockedWorldTarget;
    private bool _hasLockedTarget;

    public GraveLampMothModel(EnemyArchetypeSettings settings)
    {
        if (EnemyArchetypeTuning.Validate(settings).Count > 0)
            throw new ArgumentException("Invalid enemy archetype settings.", nameof(settings));
        _settings = settings;
    }

    public GraveLampMothFrame Step(GraveLampMothInput input, double delta,
        Vector2 targetWorldPosition = default)
    {
        float seconds = MathF.Max(0f, (float)delta);
        if (_state == GraveLampMothState.Dead || input.Dead)
            return Frame(TransitionTo(GraveLampMothState.Dead), 0f, 0f, false);

        if (input.ThreadRequested)
            return Frame(TransitionTo(GraveLampMothState.ThreadStunned), 0f, 0f, false);

        bool entered = false;
        switch (_state)
        {
            case GraveLampMothState.Hover:
                if (input.HasPlayer && Distance(input) <= _settings.DetectionRange)
                    entered = TransitionTo(GraveLampMothState.Align);
                break;

            case GraveLampMothState.Align:
                if (!input.HasPlayer || Distance(input) > _settings.DetectionRange)
                    entered = TransitionTo(GraveLampMothState.Hover);
                else if (MathF.Abs(input.AlignmentErrorX) <= _settings.HorizontalAlignmentTolerance &&
                    MathF.Abs(input.AlignmentErrorY) <= _settings.VerticalAlignmentTolerance)
                    entered = TransitionTo(GraveLampMothState.Telegraph);
                break;

            case GraveLampMothState.Telegraph:
                if (!input.HasPlayer)
                    entered = TransitionTo(GraveLampMothState.Hover);
                else if (Advance(seconds, _settings.TelegraphSeconds))
                {
                    LockDive(input);
                    _lockedWorldTarget = targetWorldPosition;
                    _hasLockedTarget = true;
                    entered = TransitionTo(GraveLampMothState.Dive);
                }
                break;

            case GraveLampMothState.Dive:
                if (input.WorldContact || Advance(seconds, _settings.AttackSeconds))
                    entered = TransitionTo(GraveLampMothState.Recover);
                break;

            case GraveLampMothState.Recover:
                if (Advance(seconds, _settings.RecoverySeconds))
                    entered = TransitionTo(GraveLampMothState.Hover);
                break;

            case GraveLampMothState.ThreadStunned:
                if (Advance(seconds, _settings.ThreadStunSeconds))
                    entered = TransitionTo(GraveLampMothState.Hover);
                break;
        }

        return _state switch
        {
            GraveLampMothState.Align => Frame(entered,
                MathF.Sign(input.AlignmentErrorX) * _settings.MoveSpeed,
                MathF.Sign(input.AlignmentErrorY) * _settings.MoveSpeed,
                false),
            GraveLampMothState.Dive => Frame(entered, _diveX, _diveY, true),
            _ => Frame(entered, 0f, 0f, false)
        };
    }

    private static float Distance(GraveLampMothInput input) =>
        MathF.Sqrt(input.SignedPlayerX * input.SignedPlayerX + input.SignedPlayerY * input.SignedPlayerY);

    // Opt-in actors revoke transient combat without reusing attack identities.
    public GraveLampMothFrame CancelAttack()
    {
        bool entered=_state is not (GraveLampMothState.Dead or GraveLampMothState.ThreadStunned) && TransitionTo(GraveLampMothState.Hover);
        _diveX=_diveY=0;_lockedWorldTarget=Vector2.Zero;_hasLockedTarget=false;
        return Frame(entered,0,0,false);
    }

    private void LockDive(GraveLampMothInput input)
    {
        float length = Distance(input);
        if (length <= 0.0001f)
        {
            _diveX = 0f;
            _diveY = _settings.DiveSpeed;
            return;
        }
        _diveX = input.SignedPlayerX / length * _settings.DiveSpeed;
        _diveY = input.SignedPlayerY / length * _settings.DiveSpeed;
    }

    private bool Advance(float seconds, float duration)
    {
        _stateElapsed += seconds;
        return _stateElapsed + 0.000001f >= duration;
    }

    private bool TransitionTo(GraveLampMothState next)
    {
        if (_state == next) return false;
        if (next == GraveLampMothState.Telegraph)
        {
            _attackId = ++_nextAttackId;
            _lockedWorldTarget = Vector2.Zero;
            _hasLockedTarget = false;
        }
        _state = next;
        _stateElapsed = 0f;
        return true;
    }

    private GraveLampMothFrame Frame(bool entered, float velocityX, float velocityY, bool attackActive) =>
        new(_state, velocityX, velocityY, attackActive, entered, Timeline());

    private AttackTimelineFrame Timeline()
    {
        (AttackPhase phase, float duration) = _state switch
        {
            GraveLampMothState.Telegraph => (AttackPhase.Telegraph, _settings.TelegraphSeconds),
            GraveLampMothState.Dive => (AttackPhase.Active, _settings.AttackSeconds),
            GraveLampMothState.Recover => (AttackPhase.Recovery, _settings.RecoverySeconds),
            _ => (AttackPhase.None, 0f)
        };
        if (phase == AttackPhase.None) return default;
        return new AttackTimelineFrame(
            _attackId,
            EnemyAttackKind.MothDive,
            phase,
            duration,
            MathF.Max(0f, duration - _stateElapsed),
            _lockedWorldTarget,
            _hasLockedTarget);
    }
}
