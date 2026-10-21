using System;
using Godot;
using Knight.Combat;

namespace Knight.Enemies.Skitter;

public enum CanyonSkitterState { Idle, Telegraph, Rush, Retreat, Recover, Stunned, Falling, Dead }

public readonly record struct CanyonSkitterInput(Vector2 Self, Vector2 Target, bool TargetVisible,
    bool CompatibleShelf, bool Supported, bool ThreadRequested, bool Dead);

public readonly record struct CanyonSkitterFrame(CanyonSkitterState State, int Side,
    float PlannedRushDistance, float RetreatDistance, float MoveSpeed, bool DamageActive,
    AttackTimelineFrame AttackTimeline);

public sealed class CanyonSkitterModel
{
    private readonly CanyonSkitterSettings _settings;
    private CanyonSkitterState _state;
    private float _elapsed;
    private int _side;
    private bool _locked;
    private float _plannedRushDistance;
    private ulong _nextAttackId;
    private ulong _attackId;

    public CanyonSkitterModel(CanyonSkitterSettings settings = null)
    {
        _settings = settings ?? new CanyonSkitterSettings();
        if (_settings.Validate().Count != 0)
            throw new ArgumentException("Invalid Canyon Skitter settings.", nameof(settings));
    }

    public CanyonSkitterFrame Step(CanyonSkitterInput input, double delta)
    {
        if (_state == CanyonSkitterState.Dead || input.Dead)
        {
            Transition(CanyonSkitterState.Dead);
            return Frame();
        }
        if (input.ThreadRequested)
        {
            Transition(CanyonSkitterState.Stunned, force: true);
            return Frame();
        }
        if (!input.Supported && _state != CanyonSkitterState.Falling)
        {
            Transition(CanyonSkitterState.Falling);
            return Frame();
        }
        if (_state == CanyonSkitterState.Falling && input.Supported)
        {
            Transition(CanyonSkitterState.Recover);
            return Frame();
        }

        float seconds = double.IsFinite(delta) && delta >= 0d && delta <= 1d
            ? (float)delta : 0f;
        bool valid = Eligible(input, out int targetSide, out float distance);

        switch (_state)
        {
            case CanyonSkitterState.Idle:
                if (valid)
                {
                    _side = targetSide;
                    _attackId = ++_nextAttackId;
                    _locked = false;
                    _plannedRushDistance = 0f;
                    Transition(CanyonSkitterState.Telegraph);
                }
                break;
            case CanyonSkitterState.Telegraph:
                if (!_locked && !valid)
                {
                    ClearAttack();
                    Transition(CanyonSkitterState.Idle);
                    break;
                }
                if (!_locked) _side = targetSide;
                _elapsed += seconds;
                if (!_locked && _elapsed + .000001f >= _settings.TelegraphSeconds * .5f)
                {
                    _locked = true;
                    _plannedRushDistance = MathF.Min(_settings.RushDistance,
                        MathF.Max(0f, distance - 34f));
                }
                if (_locked && (!valid || targetSide != _side || distance <= 34f))
                {
                    ClearAttack();
                    Transition(CanyonSkitterState.Recover);
                    break;
                }
                if (_elapsed + .000001f >= _settings.TelegraphSeconds)
                {
                    if (_plannedRushDistance > 0f) Transition(CanyonSkitterState.Rush);
                    else { ClearAttack(); Transition(CanyonSkitterState.Recover); }
                }
                break;
            case CanyonSkitterState.Rush:
                if (!valid || targetSide != _side || distance <= 34f)
                {
                    Transition(CanyonSkitterState.Retreat);
                    break;
                }
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.RushSeconds)
                    Transition(CanyonSkitterState.Retreat);
                break;
            case CanyonSkitterState.Retreat:
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.RetreatSeconds)
                    Transition(CanyonSkitterState.Recover);
                break;
            case CanyonSkitterState.Recover:
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.RecoverySeconds)
                {
                    ClearAttack();
                    Transition(CanyonSkitterState.Idle);
                }
                break;
            case CanyonSkitterState.Stunned:
                _elapsed += seconds;
                if (_elapsed + .000001f >= _settings.ThreadStunSeconds)
                {
                    ClearAttack();
                    Transition(input.Supported ? CanyonSkitterState.Idle : CanyonSkitterState.Falling);
                }
                break;
        }
        return Frame();
    }

    public CanyonSkitterFrame StopRush()
    {
        if (_state == CanyonSkitterState.Rush) Transition(CanyonSkitterState.Retreat);
        return Frame();
    }

    public CanyonSkitterFrame StopRetreat()
    {
        if (_state == CanyonSkitterState.Retreat) Transition(CanyonSkitterState.Recover);
        return Frame();
    }

    private bool Eligible(CanyonSkitterInput input, out int side, out float distance)
    {
        side = 0;
        distance = 0f;
        if (!input.TargetVisible || !input.CompatibleShelf || !input.Supported ||
            !Finite(input.Self) || !Finite(input.Target)) return false;
        float offset = input.Target.X - input.Self.X;
        if (!float.IsFinite(offset) || MathF.Abs(offset) <= .0001f ||
            MathF.Abs(input.Target.Y - input.Self.Y) > _settings.VerticalTolerance) return false;
        distance = MathF.Abs(offset);
        if (distance > _settings.EngageRange) return false;
        side = MathF.Sign(offset);
        return true;
    }

    private CanyonSkitterFrame Frame()
    {
        AttackPhase phase = _state switch
        {
            CanyonSkitterState.Telegraph => AttackPhase.Telegraph,
            CanyonSkitterState.Rush => AttackPhase.Active,
            CanyonSkitterState.Retreat or CanyonSkitterState.Recover when _attackId != 0 => AttackPhase.Recovery,
            _ => AttackPhase.None
        };
        float duration = phase switch
        {
            AttackPhase.Telegraph => _settings.TelegraphSeconds,
            AttackPhase.Active => _settings.RushSeconds,
            AttackPhase.Recovery when _state == CanyonSkitterState.Retreat => _settings.RetreatSeconds,
            AttackPhase.Recovery => _settings.RecoverySeconds,
            _ => 0f
        };
        AttackTimelineFrame timeline = phase == AttackPhase.None ? default :
            new(_attackId, EnemyAttackKind.SkitterRush, phase, duration,
                MathF.Max(0f, duration - _elapsed), new Vector2(_plannedRushDistance * _side, 0f), _locked);
        float speed = _state switch
        {
            CanyonSkitterState.Rush => _settings.RushSpeed * _side,
            CanyonSkitterState.Retreat => -_settings.RetreatSpeed * _side,
            _ => 0f
        };
        return new(_state, _side, _plannedRushDistance, _settings.RetreatDistance,
            speed, _state == CanyonSkitterState.Rush, timeline);
    }

    private void Transition(CanyonSkitterState next, bool force = false)
    {
        if (!force && _state == next) return;
        _state = next;
        _elapsed = 0f;
        if (next is CanyonSkitterState.Dead or CanyonSkitterState.Stunned or CanyonSkitterState.Falling)
            ClearAttack();
    }

    private void ClearAttack()
    {
        _attackId = 0;
        _plannedRushDistance = 0f;
        _locked = false;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
