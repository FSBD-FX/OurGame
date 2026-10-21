using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;
using Knight.Enemies.Ranged;
using Knight.Mechanisms;

namespace Knight.Enemies.Sentry;

public partial class BellBoltSentry : CharacterBody2D, IThreadPullReceiver, IEncounterActor, IResidentEnemyAgent, IDamageReceiver,IThreadInteractionReceiver
{
    [Export] public NodePath HealthPath { get; set; } = new("Health");
    [Export] public NodePath MuzzlePath { get; set; } = new("Muzzle");
    [Export] public BellBoltSentrySettings Settings { get; set; } = new();

    public event Action Defeated;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;
    public event Action<BellBoltSentryFrame> FrameChanged;

    public BellBoltSentryFrame CurrentFrame { get; private set; } =
        new(BellBoltSentryState.Idle, Vector2.Zero, false, default);

    private BellBoltSentryModel _model;
    private HealthComponent _health;
    private Node2D _muzzle;
    private AttackTelegraphVisual _telegraph;
    private BellBoltProjectile _projectile;
    private Vector2 _spawnPosition;
    private bool _spawnCaptured;
    private bool _threadRequested;
    private bool _defeatedEmitted;
    private ResidentTurretResidence _residence;
    private Node2D _residentTarget;
    private ResidentFamily _combatFamily;
    private ResidentCombatProfile _combatProfile;
    private bool _ghostManifest;
    private float _ghostRemaining;
    private Node2D _ghostVisual;
    private Color _ghostColor;
    private bool HasGhostProfile=>_combatFamily==ResidentFamily.Ghost&&_combatProfile!=null;
    private bool GhostAllowsInteraction=>!HasGhostProfile||_ghostManifest;

    public override void _Ready()
    {
        Settings ??= new BellBoltSentrySettings();
        if (Settings.Validate().Count > 0)
            throw new InvalidOperationException("Invalid Bell-Bolt Sentry settings: " +
                string.Join("; ", Settings.Validate()));
        _model = new BellBoltSentryModel(Settings);
        _health = GetNode<HealthComponent>(HealthPath);
        _muzzle = GetNode<Node2D>(MuzzlePath);
        _telegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        if(HasGhostProfile)
        {
            _ghostVisual=GetNodeOrNull<Node2D>("VisualRoot");if(IsInstanceValid(_ghostVisual))_ghostColor=_ghostVisual.Modulate;
            ResetGhostPhase();
        }
        _residence?.Initialize(_health);
        CallDeferred(nameof(CaptureSpawnPosition));
    }

    public override void _ExitTree()
    {
        RetireProjectile();
        AttackFrameChanged?.Invoke(default);
        if (IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_residence is {VoidPending:true}){CancelResidentAttack();_residence.ApplyPendingVoidDeath();}
        float step = NormalizeStep(delta, Settings.Gravity);
        StepGhostPhase(step);
        CaptureSpawnPosition();
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        HealthComponent playerHealth = player?.GetNodeOrNull<HealthComponent>("Health");
        Vector2 muzzle = _muzzle.GlobalPosition;
        if(_residence!=null)
        {
            bool committed=CurrentFrame.AttackTimeline.Phase!=AttackPhase.None&&_residentTarget!=null;
            player=committed?_residence.TargetAllowed(_residentTarget,Settings.DetectionRange,muzzle)?_residentTarget:null:
                _residence.SelectTarget(Settings.DetectionRange,muzzle);
            if(!_residence.CombatAvailable||committed&&player==null){CancelResidentAttack(_residence.Available&&!GhostAllowsInteraction);player=null;}
            playerHealth=player?.GetNodeOrNull<HealthComponent>("Health");
        }
        Vector2 target = player?.GlobalPosition ?? muzzle;
        bool targetVisible = playerHealth?.CurrentHealth > 0 && HasClearWorldPath(muzzle, target);
        CurrentFrame = _model.Step(new BellBoltSentryInput(muzzle, target, targetVisible,
            _threadRequested, _health.CurrentHealth <= 0), step);
        _threadRequested = false;
        if(_residence!=null&&CurrentFrame.State==BellBoltSentryState.Telegraph)_residentTarget??=player;
        if (CurrentFrame.FireRequested && _projectile == null) SpawnProjectile();
        PublishAttack(CurrentFrame.AttackTimeline);
        FrameChanged?.Invoke(CurrentFrame);

        if (CurrentFrame.State == BellBoltSentryState.Dead)
        {
            Velocity = Vector2.Zero;
            return;
        }
        float vertical = float.IsFinite(Velocity.Y) ? Velocity.Y : 0f;
        float nextVertical = vertical + Settings.Gravity * step;
        Velocity = new Vector2(0f, float.IsFinite(nextVertical) ? nextVertical : vertical);
        Transform2D before=_residence?.BodyPose??default;
        MoveAndSlide();
        if(_residence?.ObserveVoidContact(before)==true){CancelResidentAttack();_residence.ApplyPendingVoidDeath();}
    }

    public void ApplyThreadPull(Vector2 velocity)
    {
        if(_residence is {CombatAvailable:false})return;
        if (_health == null || _health.CurrentHealth <= 0 || !Finite(velocity)) return;
        _threadRequested = true;
    }

    public void ConfigureResidence(ResidentPopulationDefinition definition)
    {
        if(IsInsideTree()||_residence!=null)throw new InvalidOperationException("Configure Sentry residence once before mounting.");
        ArgumentNullException.ThrowIfNull(definition);
        ResidentCombatProfile profile=definition.CombatProfile;
        if(definition.Family==ResidentFamily.Ghost)
        {
            if(profile==null||profile.BoneWeakness!=ResidentBoneWeakness.None||!Duration(profile.GhostHiddenSeconds)||!Duration(profile.GhostManifestSeconds))
                throw new ArgumentException("Ghost Sentry needs an exclusive finite manifestation profile.",nameof(definition));
        }
        else if(definition.Family==ResidentFamily.Bone||profile!=null)
            throw new ArgumentException("Sentry cannot silently ignore another family's combat profile.",nameof(definition));
        var residence=new ResidentTurretResidence(this,definition,()=>GhostAllowsInteraction);
        _residence=residence;_combatFamily=definition.Family;_combatProfile=profile;
        static bool Duration(float value)=>float.IsFinite(value)&&value>=1f/60&&value<=60;
    }
    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if(!Live(this)||!Live(interaction.Player)||!IsInstanceValid(_health)||_health.CurrentHealth<=0||!GhostAllowsInteraction||
            _residence is {CombatAvailable:false}||interaction.Player.GetTree()!=GetTree()||interaction.Player.GetWorld2D()!=GetWorld2D()||
            interaction.Settings==null||!float.IsFinite(interaction.Settings.EnemyPullSpeed)||interaction.Settings.EnemyPullSpeed<=0)return false;
        Vector2 velocity=GlobalPosition.DirectionTo(interaction.Player.GlobalPosition)*interaction.Settings.EnemyPullSpeed;
        if(!Finite(velocity))return false;ApplyThreadPull(velocity);return true;
    }
    private static bool Live(Node node)
    {
        if(!IsInstanceValid(node)||!node.IsInsideTree())return false;
        for(Node current=node;current!=null;current=current.GetParent())if(current.IsQueuedForDeletion())return false;return true;
    }
    private void ResetGhostPhase()
    {
        if(!HasGhostProfile)return;
        _ghostManifest=false;_ghostRemaining=_combatProfile.GhostHiddenSeconds;PresentGhostPhase();
    }
    private void StepGhostPhase(float step)
    {
        if(!HasGhostProfile||!float.IsFinite(step)||step<=0||_residence==null||!_residence.Available)return;
        float cycle=_combatProfile.GhostHiddenSeconds+_combatProfile.GhostManifestSeconds;
        _ghostRemaining-=step%cycle;bool changed=false;
        while(_ghostRemaining<=0)
        {
            _ghostManifest=!_ghostManifest;_ghostRemaining+=_ghostManifest?_combatProfile.GhostManifestSeconds:_combatProfile.GhostHiddenSeconds;changed=true;
            if(!_ghostManifest)CancelResidentAttack(preserveAcceptedThread:true);
        }
        if(changed){_residence.ApplyAuthority();PresentGhostPhase();}
    }
    private void PresentGhostPhase()
    {
        if(IsInstanceValid(_ghostVisual))_ghostVisual.Modulate=new(_ghostColor.R,_ghostColor.G,_ghostColor.B,_ghostColor.A*(_ghostManifest?1:.3f));
    }
    public void SetResidentDormant(bool dormant)
    {
        if(_residence==null)throw new InvalidOperationException("Only configured Sentry residents can sleep.");
        if(!_residence.SetDormant(dormant))return;
        if(_model!=null&&dormant)CancelResidentAttack();_residence.ApplyAuthority();
    }
    public bool ReceiveDamage(DamageRequest request)=>_residence!=null?_residence.ReceiveDamage(request):_health?.ReceiveDamage(request)??false;
    private void CancelResidentAttack(bool preserveAcceptedThread=false)
    {
        // Commit before callbacks: a Reset inside retirement/None must retain
        // its own cleared request, never restore a captured old-life bool.
        CurrentFrame=_model.CancelAttack();_residentTarget=null;_threadRequested=preserveAcceptedThread&&_threadRequested;RetireProjectile();PublishAttack(default);
    }

    public void ResetForCheckpoint()
    {
        RetireProjectile();
        if (_spawnCaptured) GlobalPosition = _spawnPosition;
        Velocity = Vector2.Zero;
        _threadRequested = false;
        _defeatedEmitted = false;
        _model = new BellBoltSentryModel(Settings);
        CurrentFrame = new BellBoltSentryFrame(BellBoltSentryState.Idle, Vector2.Zero, false, default);
        _health?.ResetFull();
        _residentTarget=null;ResetGhostPhase();_residence?.ResetForCheckpoint();
        if(!HasGhostProfile)GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.AddToGroup(ThreadTarget2D.TargetGroup);
        PublishAttack(default);
        FrameChanged?.Invoke(CurrentFrame);
    }

    private void CaptureSpawnPosition()
    {
        if (_spawnCaptured || !IsInsideTree()) return;
        _spawnPosition = GlobalPosition;
        _spawnCaptured = true;
    }

    private bool HasClearWorldPath(Vector2 from, Vector2 to)
    {
        if (!Finite(from) || !Finite(to)) return false;
        var query = PhysicsRayQueryParameters2D.Create(from, to, CombatCollisionLayers.World);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.HitFromInside = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        return GetWorld2D().DirectSpaceState.IntersectRay(query).Count == 0;
    }

    private void SpawnProjectile()
    {
        if (CurrentFrame.AimDirection.IsZeroApprox() || !Finite(CurrentFrame.AimDirection)) return;
        var bolt = new BellBoltProjectile
        {
            Name = $"BellBolt{CurrentFrame.AttackTimeline.AttackId}",
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.World,
            ZIndex = 7
        };
        try
        {
            bolt.CreateOwnedCollision(Settings.BoltRadius,Settings.Damage);
            AddChild(bolt);
            _projectile = bolt;
            bolt.Retired += OnProjectileRetired;
            if(_residence!=null)bolt.SetResidentAuthority(_residence.Territory,()=>_residence.CombatAvailable);
            bolt.Launch(_muzzle.GlobalPosition, CurrentFrame.AimDirection,
                CurrentFrame.AttackTimeline.AttackId, Settings);
        }
        catch
        {
            if(IsInstanceValid(bolt)){if(!bolt.IsInsideTree())bolt.ReleaseOwnedCollision();bolt.Free();}
            if(ReferenceEquals(_projectile,bolt))_projectile=null;
            throw;
        }
    }

    private void RetireProjectile()
    {
        if (!IsInstanceValid(_projectile))
        {
            _projectile = null;
            return;
        }
        BellBoltProjectile bolt = _projectile;
        _projectile = null;
        bolt.Retired -= OnProjectileRetired;
        bolt.Retire();
        bolt.QueueFree();
    }

    private void OnProjectileRetired(BellBoltProjectile bolt)
    {
        bolt.Retired -= OnProjectileRetired;
        if (ReferenceEquals(_projectile, bolt)) _projectile = null;
        bolt.QueueFree();
    }

    private void PublishAttack(AttackTimelineFrame timeline)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            if (IsInstanceValid(_telegraph)) _telegraph.Present(default); else _telegraph = null;
            AttackFrameChanged?.Invoke(default);
            return;
        }
        Vector2 aim = CurrentFrame.AimDirection.IsZeroApprox() ? Vector2.Right : CurrentFrame.AimDirection;
        AttackShapeFrame shape;
        if (timeline.Phase == AttackPhase.Telegraph)
        {
            float radius=_residence==null?2f:Settings.BoltRadius;
            Vector2 endpoint = ClippedEndpoint(_muzzle.GlobalPosition,
                _muzzle.GlobalPosition + aim * Settings.MaximumTravel);
            Vector2 delta = endpoint - _muzzle.GlobalPosition;
            shape = new AttackShapeFrame(AttackShapeKind.Capsule,
                (_muzzle.GlobalPosition + endpoint) * .5f,
                new Vector2(radius, delta.Length() * .5f + radius),
                delta.Angle() - Mathf.Pi * .5f, Array.Empty<Vector2>());
            if (timeline.HasLockedTarget)
                timeline = timeline with { LockedWorldTarget = endpoint };
        }
        else if (timeline.Phase == AttackPhase.Active && IsInstanceValid(_projectile))
        {
            shape = new AttackShapeFrame(AttackShapeKind.Circle, _projectile.GlobalPosition,
                Vector2.One * Settings.BoltRadius, 0f, Array.Empty<Vector2>());
        }
        else
        {
            shape = new AttackShapeFrame(AttackShapeKind.Circle, _muzzle.GlobalPosition,
                Vector2.One, 0f, Array.Empty<Vector2>());
        }
        var presentation = new AttackPresentationFrame(timeline, shape).Validate();
        if (IsInstanceValid(_telegraph)) _telegraph.Present(presentation); else _telegraph = null;
        AttackFrameChanged?.Invoke(presentation);
    }

    private Vector2 ClippedEndpoint(Vector2 from, Vector2 desired)
    {
        if(_residence!=null)
        {
            Vector2 bounded=RangedAttackGeometry.ClipToArea(from,desired,_residence.Territory,Settings.BoltRadius);
            var excludes=new Godot.Collections.Array<Rid>{GetRid()};
            using Godot.Collections.Array backing=(Godot.Collections.Array)excludes;
            // The warning owns the bolt's whole radius, including at World
            // walls. A center ray leaves its round cap inside the obstacle.
            return RangedAttackGeometry.ClipBeam(GetWorld2D().DirectSpaceState,from,
                bounded-from,from.DistanceTo(bounded),Settings.BoltRadius,excludes);
        }
        var query = PhysicsRayQueryParameters2D.Create(from, desired, CombatCollisionLayers.World);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.HitFromInside = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 && hit.TryGetValue("position", out Variant value) ? value.AsVector2() : desired;
    }

    private void OnDamaged(int remaining, Vector2 impulse) =>
        PresentationCueRequested?.Invoke(PresentationCue.Hurt);

    private void OnDied()
    {
        // Own this life's publication before any subscriber can reset the
        // actor. Never write the old guard after a new life has begun.
        if (_defeatedEmitted) return;
        _defeatedEmitted = true;
        RetireProjectile();
        _residentTarget=null;_residence?.ApplyAuthority();
        GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.RemoveFromGroup(ThreadTarget2D.TargetGroup);
        PublishAttack(default);
        PresentationCueRequested?.Invoke(PresentationCue.Died);
        Defeated?.Invoke();
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static float NormalizeStep(double delta, float gravity)
    {
        if (!double.IsFinite(delta) || delta <= 0d || !float.IsFinite(gravity) || gravity <= 0f)
            return 0f;
        double maximumSafeStep = float.MaxValue / gravity;
        return delta <= maximumSafeStep ? (float)delta : 0f;
    }
}
