using System;
using Godot;
using Knight.Combat;
using Knight.Config;

namespace Knight.Boss;

public sealed class FallenBellkeeperModel
{
    private enum PhaseOneWindow { Telegraph, Active, Recovery }

    private static readonly FallenBellkeeperAttack[] PhaseOneAttacks =
    {
        FallenBellkeeperAttack.Sweep,
        FallenBellkeeperAttack.LeapSlam,
        FallenBellkeeperAttack.GroundWave
    };

    private readonly BossSettings _settings;
    private FallenBellkeeperState _state = FallenBellkeeperState.Dormant;
    private PhaseOneWindow _phaseOneWindow = PhaseOneWindow.Telegraph;
    private int _phaseOneAttackIndex;
    private float _elapsed;
    private bool _phaseTwoTriggered;
    private bool _phaseTwoHammerRecovery;
    private ulong _nextAttackId;
    private ulong _attackId;
    private Vector2 _lockedWorldTarget;
    private bool _hasLockedTarget;

    public FallenBellkeeperModel(BossSettings settings)
    {
        if (BossTuning.Validate(settings).Count > 0)
            throw new ArgumentException("Invalid Boss settings.", nameof(settings));
        _settings = settings;
    }

    public FallenBellkeeperFrame Step(FallenBellkeeperInput input, double delta,
        Vector2 targetWorldPosition = default)
    {
        if (_state == FallenBellkeeperState.Defeated || input.HealthFraction <= 0f)
            return Frame(TransitionTo(FallenBellkeeperState.Defeated));

        float seconds = MathF.Max(0f, (float)delta);
        bool entered = false;
        switch (_state)
        {
            case FallenBellkeeperState.Dormant:
                if (input.ArenaActive) entered = TransitionTo(FallenBellkeeperState.Intro);
                break;
            case FallenBellkeeperState.Intro:
                if (Advance(seconds, _settings.IntroSeconds))
                {
                    ResetPhaseOneWindow();
                    entered = TransitionTo(FallenBellkeeperState.PhaseOne);
                }
                break;
            case FallenBellkeeperState.PhaseOne:
                if (!_phaseTwoTriggered && input.HealthFraction <= _settings.TransitionHealthRatio)
                {
                    _phaseTwoTriggered = true;
                    entered = TransitionTo(FallenBellkeeperState.Transition);
                }
                else AdvancePhaseOne(seconds, targetWorldPosition);
                break;
            case FallenBellkeeperState.Transition:
                if (Advance(seconds, _settings.TransitionSeconds))
                {
                    _phaseTwoHammerRecovery = false;
                    entered = TransitionTo(FallenBellkeeperState.PhaseTwoArmored);
                }
                break;
            case FallenBellkeeperState.PhaseTwoArmored:
                float armoredDuration = _phaseTwoHammerRecovery
                    ? _settings.TimingFor(FallenBellkeeperAttack.HammerThrow).RecoverySeconds
                    : _settings.PhaseTwoArmoredSeconds;
                if (Advance(seconds, armoredDuration))
                {
                    _phaseTwoHammerRecovery = false;
                    entered = TransitionTo(FallenBellkeeperState.HammerTelegraph);
                }
                break;
            case FallenBellkeeperState.HammerTelegraph:
                if (Advance(seconds,
                    _settings.TimingFor(FallenBellkeeperAttack.HammerThrow).TelegraphSeconds))
                {
                    LockTarget(targetWorldPosition);
                    entered = TransitionTo(FallenBellkeeperState.HammerThrown);
                }
                break;
            case FallenBellkeeperState.HammerThrown:
                if (input.ChainThreadRequested)
                {
                    _phaseTwoHammerRecovery = false;
                    entered = TransitionTo(FallenBellkeeperState.Exposed);
                }
                else if (Advance(seconds,
                    _settings.TimingFor(FallenBellkeeperAttack.HammerThrow).ActiveSeconds))
                {
                    _phaseTwoHammerRecovery = true;
                    entered = TransitionTo(FallenBellkeeperState.PhaseTwoArmored);
                }
                break;
            case FallenBellkeeperState.Exposed:
                if (Advance(seconds, _settings.ExposedSeconds))
                {
                    _phaseTwoHammerRecovery = false;
                    entered = TransitionTo(FallenBellkeeperState.PhaseTwoArmored);
                }
                break;
        }
        return Frame(entered);
    }

    private void AdvancePhaseOne(float seconds, Vector2 targetWorldPosition)
    {
        FallenBellkeeperAttack attack = PhaseOneAttacks[_phaseOneAttackIndex];
        BossAttackTiming timing = _settings.TimingFor(attack);
        _elapsed += seconds;
        float duration = _phaseOneWindow switch
        {
            PhaseOneWindow.Telegraph => timing.TelegraphSeconds,
            PhaseOneWindow.Active => timing.ActiveSeconds,
            _ => timing.RecoverySeconds
        };
        if (_elapsed + .000001f < duration) return;
        _elapsed = 0f;
        switch (_phaseOneWindow)
        {
            case PhaseOneWindow.Telegraph:
                LockTarget(targetWorldPosition);
                _phaseOneWindow = PhaseOneWindow.Active;
                break;
            case PhaseOneWindow.Active:
                _phaseOneWindow = PhaseOneWindow.Recovery;
                break;
            default:
                _phaseOneWindow = PhaseOneWindow.Telegraph;
                _phaseOneAttackIndex = (_phaseOneAttackIndex + 1) % PhaseOneAttacks.Length;
                BeginAttack();
                break;
        }
    }

    private void ResetPhaseOneWindow()
    {
        _phaseOneWindow = PhaseOneWindow.Telegraph;
        _phaseOneAttackIndex = 0;
        _elapsed = 0f;
        BeginAttack();
    }

    private void BeginAttack()
    {
        _attackId = ++_nextAttackId;
        _lockedWorldTarget = Vector2.Zero;
        _hasLockedTarget = false;
    }

    private void LockTarget(Vector2 targetWorldPosition)
    {
        _lockedWorldTarget = targetWorldPosition;
        _hasLockedTarget = true;
    }

    private bool Advance(float seconds, float duration)
    {
        _elapsed += seconds;
        return _elapsed + .000001f >= duration;
    }

    private bool TransitionTo(FallenBellkeeperState next)
    {
        if (_state == next) return false;
        if (next == FallenBellkeeperState.HammerTelegraph) BeginAttack();
        _state = next;
        _elapsed = 0f;
        return true;
    }

    private FallenBellkeeperFrame Frame(bool entered)
    {
        if (_state == FallenBellkeeperState.Defeated)
            return new FallenBellkeeperFrame(_state, FallenBellkeeperAttack.None,
                false, false, false, entered, default);
        FallenBellkeeperAttack attack = _state == FallenBellkeeperState.PhaseOne
            ? PhaseOneAttacks[_phaseOneAttackIndex]
            : _state is FallenBellkeeperState.HammerTelegraph or FallenBellkeeperState.HammerThrown
                ? FallenBellkeeperAttack.HammerThrow
                : FallenBellkeeperAttack.None;
        bool attackActive = _state == FallenBellkeeperState.PhaseOne &&
            _phaseOneWindow == PhaseOneWindow.Active || _state == FallenBellkeeperState.HammerThrown;
        bool armorClosed = _state is FallenBellkeeperState.PhaseTwoArmored or
            FallenBellkeeperState.HammerTelegraph or FallenBellkeeperState.HammerThrown;
        bool chainTarget = _state == FallenBellkeeperState.HammerThrown;
        return new FallenBellkeeperFrame(
            _state, attack, attackActive, armorClosed, chainTarget, entered, Timeline());
    }

    private AttackTimelineFrame Timeline()
    {
        FallenBellkeeperAttack attack;
        AttackPhase phase;
        BossAttackTiming timing;
        if (_state == FallenBellkeeperState.PhaseOne)
        {
            attack = PhaseOneAttacks[_phaseOneAttackIndex];
            timing = _settings.TimingFor(attack);
            phase = _phaseOneWindow switch
            {
                PhaseOneWindow.Telegraph => AttackPhase.Telegraph,
                PhaseOneWindow.Active => AttackPhase.Active,
                _ => AttackPhase.Recovery
            };
        }
        else if (_state == FallenBellkeeperState.HammerTelegraph)
        {
            attack = FallenBellkeeperAttack.HammerThrow;
            timing = _settings.TimingFor(attack);
            phase = AttackPhase.Telegraph;
        }
        else if (_state == FallenBellkeeperState.HammerThrown)
        {
            attack = FallenBellkeeperAttack.HammerThrow;
            timing = _settings.TimingFor(attack);
            phase = AttackPhase.Active;
        }
        else if (_state == FallenBellkeeperState.PhaseTwoArmored && _phaseTwoHammerRecovery)
        {
            attack = FallenBellkeeperAttack.HammerThrow;
            timing = _settings.TimingFor(attack);
            phase = AttackPhase.Recovery;
        }
        else return default;

        float duration = phase switch
        {
            AttackPhase.Telegraph => timing.TelegraphSeconds,
            AttackPhase.Active => timing.ActiveSeconds,
            _ => timing.RecoverySeconds
        };
        return new AttackTimelineFrame(
            _attackId,
            KindFor(attack),
            phase,
            duration,
            MathF.Max(0f, duration - _elapsed),
            _lockedWorldTarget,
            _hasLockedTarget);
    }

    private static EnemyAttackKind KindFor(FallenBellkeeperAttack attack) => attack switch
    {
        FallenBellkeeperAttack.Sweep => EnemyAttackKind.BossSweep,
        FallenBellkeeperAttack.LeapSlam => EnemyAttackKind.BossLeapSlam,
        FallenBellkeeperAttack.GroundWave => EnemyAttackKind.BossGroundWave,
        FallenBellkeeperAttack.HammerThrow => EnemyAttackKind.BossHammerThrow,
        _ => EnemyAttackKind.None
    };
}
