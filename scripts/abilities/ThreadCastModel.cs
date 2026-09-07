using Godot;

namespace Knight.Abilities;

public sealed class ThreadCastModel
{
    private readonly ThreadAbilitySettings _settings;

    private ThreadCastState _state = ThreadCastState.Idle;
    private ThreadAimMode _aimMode = ThreadAimMode.Tap;
    private ThreadFailureReason _failure = ThreadFailureReason.None;
    private Vector2 _aim = Vector2.Right;
    private ulong _castId;
    private ulong _nextCastId = 1;
    private ulong _targetId;
    private bool _hasTarget;
    private bool _awaitingHoldDecision;
    private float _holdElapsedSeconds;
    private float _stateDurationSeconds;
    private float _stateRemainingSeconds;

    public ThreadCastModel(ThreadAbilitySettings settings = null)
    {
        _settings = settings ?? new ThreadAbilitySettings();
    }

    public ThreadCastFrame Step(ThreadCastInput input, float deltaSeconds, ThreadTargetSnapshot target,
        bool cooldownReady)
    {
        float delta = Mathf.Max(0f, deltaSeconds);
        if (input.Cancelled && _state != ThreadCastState.Idle)
        {
            if (_state == ThreadCastState.Recovery) EnterIdle();
            else EnterFailed(ThreadFailureReason.Cancelled);
            return Frame();
        }

        switch (_state)
        {
            case ThreadCastState.Idle:
                StepIdle(input, delta, target, cooldownReady);
                break;
            case ThreadCastState.Preview:
                StepPreview(input, target, cooldownReady);
                break;
            case ThreadCastState.Windup:
            case ThreadCastState.Travel:
            case ThreadCastState.Latched:
            case ThreadCastState.Apply:
            case ThreadCastState.Recovery:
                StepCastPhase(delta, target);
                break;
            case ThreadCastState.Failed:
                _stateRemainingSeconds = Mathf.Max(0f, _stateRemainingSeconds - delta);
                if (_stateRemainingSeconds <= .000001f) EnterIdle();
                break;
        }

        return Frame();
    }

    private void StepIdle(ThreadCastInput input, float delta, ThreadTargetSnapshot target, bool cooldownReady)
    {
        if (input.Pressed)
        {
            if (!cooldownReady)
            {
                EnterFailed(ThreadFailureReason.Cooldown);
                return;
            }
            _aim = input.Aim;
            if (input.Released || !input.Held)
            {
                BeginCast(ThreadAimMode.Tap, target, cooldownReady);
                return;
            }

            _awaitingHoldDecision = true;
            _holdElapsedSeconds = delta;
        }
        else if (_awaitingHoldDecision && input.Released)
        {
            _aim = input.Aim;
            BeginCast(ThreadAimMode.Tap, target, cooldownReady);
            return;
        }
        else if (_awaitingHoldDecision && input.Held)
        {
            _aim = input.Aim;
            _holdElapsedSeconds += delta;
        }
        else if (_awaitingHoldDecision)
        {
            _awaitingHoldDecision = false;
            _holdElapsedSeconds = 0f;
        }

        if (_awaitingHoldDecision && _holdElapsedSeconds >= _settings.TapThresholdSeconds)
        {
            _awaitingHoldDecision = false;
            _holdElapsedSeconds = 0f;
            _aimMode = ThreadAimMode.Hold;
            SetPreviewTarget(target);
            EnterState(ThreadCastState.Preview, 0f);
        }
    }

    private void StepPreview(ThreadCastInput input, ThreadTargetSnapshot target, bool cooldownReady)
    {
        _aim = input.Aim;
        SetPreviewTarget(target);
        if (input.Released) BeginCast(ThreadAimMode.Hold, target, cooldownReady);
    }

    private void SetPreviewTarget(ThreadTargetSnapshot target)
    {
        _targetId = target.IsValid ? target.Id : 0;
        _hasTarget = target.IsValid;
    }

    private void BeginCast(ThreadAimMode aimMode, ThreadTargetSnapshot target, bool cooldownReady)
    {
        _awaitingHoldDecision = false;
        _holdElapsedSeconds = 0f;
        if (!cooldownReady)
        {
            EnterFailed(ThreadFailureReason.Cooldown);
            return;
        }
        if (!target.IsValid)
        {
            EnterFailed(target.InvalidReason == ThreadFailureReason.None
                ? ThreadFailureReason.NoTarget
                : target.InvalidReason);
            return;
        }

        _castId = _nextCastId++;
        _aimMode = aimMode;
        _targetId = target.Id;
        _hasTarget = true;
        _failure = ThreadFailureReason.None;
        EnterState(ThreadCastState.Windup, _settings.WindupSeconds);
    }

    private void StepCastPhase(float delta, ThreadTargetSnapshot target)
    {
        if (_state != ThreadCastState.Recovery && (!target.IsValid || target.Id != _targetId))
        {
            EnterFailed(target.InvalidReason == ThreadFailureReason.None
                ? ThreadFailureReason.TargetLost
                : target.InvalidReason);
            return;
        }

        _stateRemainingSeconds = Mathf.Max(0f, _stateRemainingSeconds - delta);
        if (_stateRemainingSeconds > .000001f) return;

        switch (_state)
        {
            case ThreadCastState.Windup:
                EnterState(ThreadCastState.Travel, TravelSeconds(target.RelativePosition));
                break;
            case ThreadCastState.Travel:
                EnterState(ThreadCastState.Latched, _settings.LatchSeconds);
                break;
            case ThreadCastState.Latched:
                EnterState(ThreadCastState.Apply, _settings.ApplySeconds);
                break;
            case ThreadCastState.Apply:
                EnterState(ThreadCastState.Recovery, _settings.RecoverySeconds);
                break;
            case ThreadCastState.Recovery:
                EnterIdle();
                break;
        }
    }

    private float TravelSeconds(Vector2 targetPosition)
    {
        float ratio = Mathf.Clamp(targetPosition.Length() / _settings.TravelDistanceForMaximumDuration, 0f, 1f);
        return Mathf.Lerp(_settings.MinimumTravelSeconds, _settings.MaximumTravelSeconds, ratio);
    }

    private void EnterFailed(ThreadFailureReason failure)
    {
        _state = ThreadCastState.Failed;
        _failure = failure;
        _castId = 0;
        _targetId = 0;
        _hasTarget = false;
        _stateDurationSeconds = _settings.VisualSeconds;
        _stateRemainingSeconds = _settings.VisualSeconds;
        _awaitingHoldDecision = false;
        _holdElapsedSeconds = 0f;
    }

    private void EnterIdle()
    {
        _state = ThreadCastState.Idle;
        _failure = ThreadFailureReason.None;
        _castId = 0;
        _targetId = 0;
        _hasTarget = false;
        _stateDurationSeconds = 0f;
        _stateRemainingSeconds = 0f;
        _awaitingHoldDecision = false;
        _holdElapsedSeconds = 0f;
    }

    private void EnterState(ThreadCastState state, float durationSeconds)
    {
        _state = state;
        _failure = ThreadFailureReason.None;
        _stateDurationSeconds = durationSeconds;
        _stateRemainingSeconds = durationSeconds;
    }

    private ThreadCastFrame Frame() => new(
        _castId,
        _state,
        _aimMode,
        _aim,
        _targetId,
        _hasTarget,
        _failure,
        _stateDurationSeconds,
        _stateRemainingSeconds);
}
