using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Enemies.Skitter;

public partial class CanyonSkitter : CharacterBody2D, IThreadPullReceiver, IEncounterActor
{
    [Export] public CanyonSkitterSettings Settings { get; set; } = new();
    public event Action Defeated;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;
    public event Action<CanyonSkitterFrame> FrameChanged;
    public CanyonSkitterFrame CurrentFrame { get; private set; }

    private CanyonSkitterModel _model;
    private HealthComponent _health;
    private Hitbox _hitbox;
    private AttackTelegraphVisual _telegraph;
    private Vector2 _spawnPosition;
    private bool _spawnCaptured;
    private bool _damageLogical;
    private bool _defeatedEmitted;
    private Vector2 _pullVelocity;
    private float _rushTravel;
    private float _retreatTravel;
    private float _safeRushDistance;
    private float _belowPerchSeconds;
    private ulong _environmentAttackId;
    private CanyonSkitterState _lastState;

    public override void _Ready()
    {
        Settings ??= new CanyonSkitterSettings();
        if (Settings.Validate().Count > 0)
            throw new InvalidOperationException("Invalid Canyon Skitter settings: " +
                string.Join("; ", Settings.Validate()));
        _model = new CanyonSkitterModel(Settings);
        _health = GetNode<HealthComponent>("Health");
        _hitbox = GetNode<Hitbox>("RushHitbox");
        _telegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _hitbox.TargetFilter = CanHit;
        _hitbox.Connected += OnHitConnected;
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        InitializeResidence();
        CallDeferred(nameof(CaptureSpawnPosition));
    }

    public override void _ExitTree()
    {
        _boneThreadRemaining=0;
        _residentVoidPending=false;
        DisableDamage();
        AttackFrameChanged?.Invoke(default);
        if (IsInstanceValid(_hitbox))
        {
            _hitbox.Connected -= OnHitConnected;
            _hitbox.TargetFilter = null;
        }
        if (IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = NormalizeStep(delta);
        CaptureSpawnPosition();
        if(_residentModel!=null&&_residentVoidPending)ApplyPendingResidentVoidDeath();
        if (_health == null || _health.CurrentHealth <= 0)
        {
            DisableDamage();
            Velocity = Vector2.Zero;
            if(_residentModel!=null)ObserveResidence(null,false,step);
            return;
        }

        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        HealthComponent playerHealth = player?.GetNodeOrNull<HealthComponent>("Health");
        Vector2 targetPosition = player?.GlobalPosition ?? GlobalPosition;
        float targetFootY = player is CharacterBody2D ? targetPosition.Y + 36f : targetPosition.Y;
        bool supported = HasGroundContact();
        bool compatible = player != null && MathF.Abs(targetFootY - GlobalPosition.Y) <=
            Settings.VerticalTolerance && HasFullSupport(targetPosition.X);
        bool visible = playerHealth?.CurrentHealth > 0 && compatible &&
            HasClearWorldPath(GlobalPosition + new Vector2(0f, -12f),
                targetPosition + new Vector2(0f, player is CharacterBody2D ? 0f : -12f));
        visible=ObserveResidence(player,visible,step);
        var input = new CanyonSkitterInput(GlobalPosition,
            new Vector2(targetPosition.X, targetFootY), visible, compatible, supported, false, false);
        CurrentFrame = _model.Step(input, step);
        // Physical lift still enters Falling under the unchanged model. Only
        // this Bone policy retains a bounded K-source damage opening in air;
        // it does not keep the body stunned or renew a window on landing.
        if(HasBoneProfile)
        {
            if(_residentDormant||_residentVoidPending||CurrentFrame.State is not (CanyonSkitterState.Stunned or CanyonSkitterState.Falling))_boneThreadRemaining=0;
            else _boneThreadRemaining=MathF.Max(0,_boneThreadRemaining-step);
        }
        if (CurrentFrame.State != _lastState) OnStateEntry(CurrentFrame);

        float desiredX = 0f;
        if (CurrentFrame.State == CanyonSkitterState.Rush)
        {
            float budget = MathF.Min(CurrentFrame.PlannedRushDistance, _safeRushDistance) - _rushTravel;
            desiredX = CurrentFrame.Side * MathF.Max(0f,
                MathF.Min(Settings.RushSpeed * step, budget));
            float targetGap = (targetPosition.X - GlobalPosition.X) * CurrentFrame.Side - 34f;
            desiredX = CurrentFrame.Side * MathF.Min(MathF.Abs(desiredX), MathF.Max(0f, targetGap));
            if (MathF.Abs(desiredX) < .001f || !CanAuthorMove(desiredX))
            {
                CurrentFrame = _model.StopRush();
                OnStateEntry(CurrentFrame);
                desiredX = 0f;
            }
        }
        else if (CurrentFrame.State == CanyonSkitterState.Retreat)
        {
            float homeGap = _residentModel!=null
                ? (GlobalPosition.X-_residentCombatOriginX)*CurrentFrame.Side
                : _spawnCaptured ? (GlobalPosition.X - _spawnPosition.X) * CurrentFrame.Side : _rushTravel;
            float budget = MathF.Min(Settings.RetreatDistance - _retreatTravel, MathF.Max(0f, homeGap));
            desiredX = -CurrentFrame.Side * MathF.Max(0f,
                MathF.Min(Settings.RetreatSpeed * step, budget));
            if (MathF.Abs(desiredX) < .001f || !CanAuthorMove(desiredX))
            {
                CurrentFrame = _model.StopRetreat();
                OnStateEntry(CurrentFrame);
                desiredX = 0f;
            }
        }

        if(_residentModel!=null&&CurrentFrame.State==CanyonSkitterState.Idle&&
            ResidenceFrame.State is Knight.Enemies.Residents.ResidentPatrolState.Patrolling or Knight.Enemies.Residents.ResidentPatrolState.Returning)
            desiredX=ResidenceFrame.MoveDirection*_residentSettings.Speed*step;
        if(_residentModel!=null&&(_residentDormant||_residentVoidPending))desiredX=0;
        float previousX = GlobalPosition.X;
        float vertical = float.IsFinite(Velocity.Y) ? Velocity.Y : 0f;
        float nextVertical = vertical + Settings.Gravity * step;
        if (!float.IsFinite(nextVertical)) nextVertical = 0f;
        float pullX = _pullVelocity.X;
        float xVelocity = step > 0f ? desiredX / step + pullX : 0f;
        Velocity = new Vector2(float.IsFinite(xVelocity) ? xVelocity : 0f,
            nextVertical);
        Transform2D beforeResidentMove=_residentModel!=null?_residentBody.GlobalTransform:default;
        MoveAndSlide();
        if(_residentModel!=null&&_health.CurrentHealth>0)
        {
            if(!_residentVoidPending&&Knight.Enemies.Residents.ResidentVoidContact.Crossed(this,_residentBody,beforeResidentMove))_residentVoidPending=true;
            if(_residentVoidPending)ApplyPendingResidentVoidDeath();
        }
        _pullVelocity *= Mathf.Exp(-9f * step);
        if (_pullVelocity.LengthSquared() < 1f) _pullVelocity = Vector2.Zero;
        float actualX = GlobalPosition.X - previousX;
        if (CurrentFrame.State == CanyonSkitterState.Rush)
            _rushTravel += MathF.Max(0f, actualX * CurrentFrame.Side);
        else if (CurrentFrame.State == CanyonSkitterState.Retreat)
            _retreatTravel += MathF.Max(0f, -actualX * CurrentFrame.Side);

        if (_residentModel==null && !HasGroundContact() && _spawnCaptured &&
            GlobalPosition.Y >= _spawnPosition.Y + 240f && Velocity.Y >= 0f)
            _belowPerchSeconds += step;
        else _belowPerchSeconds = 0f;
        if (_belowPerchSeconds >= .25f)
        {
            _belowPerchSeconds = 0f;
            _health.ApplyDamage(new DamageRequest(_health.CurrentHealth, Vector2.Down, 0f,
                ++_environmentAttackId, GetInstanceId()));
        }
        PublishFrame();
        _lastState = CurrentFrame.State;
    }

    public void ApplyThreadPull(Vector2 velocity)
    {
        if(_residentModel!=null&&(_residentDormant||_residentVoidPending))return;
        if (_health == null || _health.CurrentHealth <= 0 || !Finite(velocity)) return;
        DisableDamage();
        CurrentFrame = _model.Step(new CanyonSkitterInput(GlobalPosition, GlobalPosition,
            false, false, HasGroundContact(), true, false), 0d);
        _lastState = CurrentFrame.State;
        _rushTravel = 0f;
        _retreatTravel = 0f;
        Vector2 bounded = velocity.LimitLength(600f);
        _pullVelocity = new Vector2(bounded.X, 0f);
        Velocity = bounded;
        if(HasBoneProfile)_boneThreadRemaining=Settings.ThreadStunSeconds;
        PublishFrame();
    }

    public void ResetForCheckpoint()
    {
        _boneThreadRemaining=0;
        _residentVoidPending=false;
        if(_residentModel!=null)_residentModel=new Knight.Enemies.Residents.ResidentPatrolModel(_residence,_residentSettings);
        DisableDamage();
        if (_spawnCaptured) GlobalPosition = _spawnPosition;
        Velocity = Vector2.Zero;
        _pullVelocity = Vector2.Zero;
        _rushTravel = _retreatTravel = _safeRushDistance = _belowPerchSeconds = 0f;
        _defeatedEmitted = false;
        _model = new CanyonSkitterModel(Settings);
        CurrentFrame = default;
        _lastState = CanyonSkitterState.Idle;
        _health?.ResetFull();
        GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.AddToGroup(ThreadTarget2D.TargetGroup);
        if(_residentModel!=null)ApplyResidentDormancy();
        PublishFrame();
    }

    private void CaptureSpawnPosition()
    {
        if (_spawnCaptured || !IsInsideTree()) return;
        _spawnPosition = GlobalPosition;
        _spawnCaptured = true;
    }

    private void OnStateEntry(CanyonSkitterFrame frame)
    {
        if(_residentModel!=null&&frame.State==CanyonSkitterState.Telegraph)_residentCombatOriginX=GlobalPosition.X;
        if (frame.State == CanyonSkitterState.Rush)
        {
            _rushTravel = 0f;
            _retreatTravel = 0f;
            _safeRushDistance = ForecastSafeRush(frame.Side, frame.PlannedRushDistance);
            if (_safeRushDistance > 0f && frame.AttackTimeline.AttackId != 0)
            {
                _damageLogical = true;
                _hitbox.Position = new Vector2(frame.Side * 22f, -12f);
                _hitbox.Begin(frame.AttackTimeline.AttackId);
            }
        }
        else DisableDamage();
        _lastState = frame.State;
    }

    private float ForecastSafeRush(int side, float authored)
    {
        float safe = 0f;
        for (float distance = 2f; distance <= authored + .001f; distance += 2f)
        {
            if (!CanAuthorMove(side * distance)) break;
            safe = distance;
        }
        return safe;
    }

    private bool CanAuthorMove(float dx) =>
        _residentModel!=null ? CanResidentMove(dx) :
        float.IsFinite(dx) && !TestMove(GlobalTransform, new Vector2(dx, 0f)) && HasFullSupport(GlobalPosition.X + dx);

    private bool HasGroundContact() =>
        IsOnFloor() &&
        WorldUnder(new Vector2(GlobalPosition.X - 12f, GlobalPosition.Y), 2f) &&
        WorldUnder(new Vector2(GlobalPosition.X + 12f, GlobalPosition.Y), 2f);

    // Longer probes are only for forecasted movement and target-shelf checks.
    private bool HasFullSupport(float centerX) =>
        WorldUnder(new Vector2(centerX - 12f, GlobalPosition.Y), 20f) &&
        WorldUnder(new Vector2(centerX + 12f, GlobalPosition.Y), 20f);

    private bool WorldUnder(Vector2 point, float reach)
    {
        var query = PhysicsRayQueryParameters2D.Create(point + new Vector2(0f, -4f),
            point + new Vector2(0f, reach), CombatCollisionLayers.World);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        return GetWorld2D().DirectSpaceState.IntersectRay(query).Count > 0;
    }

    private bool HasClearWorldPath(Vector2 from, Vector2 to)
    {
        if (!Finite(from) || !Finite(to)) return false;
        var query = PhysicsRayQueryParameters2D.Create(from, to, CombatCollisionLayers.World);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        return GetWorld2D().DirectSpaceState.IntersectRay(query).Count == 0;
    }

    private bool CanHit(Hurtbox hurtbox)
    {
        if (!_damageLogical || CurrentFrame.State != CanyonSkitterState.Rush ||
            CurrentFrame.Side == 0) return false;
        Node2D player = hurtbox.GetParent() as Node2D;
        if(_residentModel!=null&&(!ResidentTargetAllowed(player)||
            !HasClearWorldPath(GlobalPosition+new Vector2(0,-12),player.GlobalPosition)))return false;
        return player != null && player.IsInGroup("player") &&
            (player.GlobalPosition.X - GlobalPosition.X) * CurrentFrame.Side > 0f;
    }

    private void OnHitConnected(Hurtbox hurtbox)
    {
        DisableDamage();
        CurrentFrame = _model.StopRush();
        _lastState = CurrentFrame.State;
        PublishFrame();
    }

    private void DisableDamage()
    {
        _damageLogical = false;
        if (IsInstanceValid(_hitbox)) Callable.From(_hitbox.End).CallDeferred();
    }

    private void PublishFrame()
    {
        AttackTimelineFrame timeline = CurrentFrame.AttackTimeline;
        if (timeline.Phase == AttackPhase.None)
        {
            if (IsInstanceValid(_telegraph)) _telegraph.Present(default);
            AttackFrameChanged?.Invoke(default);
            FrameChanged?.Invoke(CurrentFrame);
            return;
        }
        float extent = timeline.Phase == AttackPhase.Telegraph
            ? MathF.Max(1f, CurrentFrame.PlannedRushDistance > 0f
                ? CurrentFrame.PlannedRushDistance : Settings.RushDistance) : 16f;
        Vector2 origin = GlobalPosition + new Vector2(CurrentFrame.Side * (14f + extent * .5f), -12f);
        var shape = new AttackShapeFrame(AttackShapeKind.Capsule, origin,
            new Vector2(8f, extent * .5f), MathF.PI * .5f, Array.Empty<Vector2>());
        var presentation = new AttackPresentationFrame(timeline, shape).Validate();
        if (IsInstanceValid(_telegraph)) _telegraph.Present(presentation);
        AttackFrameChanged?.Invoke(presentation);
        FrameChanged?.Invoke(CurrentFrame);
    }

    private void OnDamaged(int remaining, Vector2 impulse) =>
        PresentationCueRequested?.Invoke(PresentationCue.Hurt);

    private void OnDied()
    {
        _boneThreadRemaining=0;
        _residentVoidPending=false;
        DisableDamage();
        CurrentFrame = _model.Step(new CanyonSkitterInput(GlobalPosition, GlobalPosition,
            false, false, false, false, true), 0d);
        if(_residentModel!=null)ApplyResidentDormancy();
        GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.RemoveFromGroup(ThreadTarget2D.TargetGroup);
        PublishFrame();
        PresentationCueRequested?.Invoke(PresentationCue.Died);
        if (_defeatedEmitted) return;
        _defeatedEmitted = true;
        Defeated?.Invoke();
    }

    private float NormalizeStep(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0d || delta > .25d ||
            !float.IsFinite(Settings.Gravity) || Settings.Gravity <= 0f) return 0f;
        return (float)delta;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
