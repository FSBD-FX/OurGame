using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;

namespace Knight.Mechanisms;

// A horizontal pressure plate: native contact owns launch, Motor owns motion.
// Authored collision resources are borrowed, never disposed by this component.
public partial class CanyonSpringboard : Area2D, ICheckpointResettable, ICanyonTelegraphSource
{
    [Export] public Vector2 LaunchVelocity { get; set; } = new(-620f, -480f);
    [Export] public float Width { get; set; } = 64f;
    [Export] public float RechargeSeconds { get; set; } = 1.2f;
    [Export] public NodePath SupportBodyPath { get; set; } = new("../Floor");

    public ulong LaunchSequence { get; private set; }
    public float TriggerWidth => _width;
    public Vector2 EffectiveLaunchVelocity => _launch;
    public bool HasLiveSensor => OwnsActivation();
    public bool IsDangerTelegraphActive => OwnsActivation() && _cooldown <= 0f;
    public event Action<CharacterBody2D, Vector2, ulong> Launched;

    private readonly HashSet<ulong> _latched = new();
    private readonly HashSet<ulong> _present = new();
    private PhysicsBody2D _support;
    private CollisionShape2D _collider;
    private Vector2 _launch;
    private float _width, _recharge, _cooldown;
    private ulong _epoch;
    private bool _ready;

    public override void _Ready()
    {
        if (!LaunchVelocity.IsFinite() || LaunchVelocity.Length() < 100f || LaunchVelocity.Length() > 1200f ||
            !float.IsFinite(Width) || Width < 16f || Width > 128f ||
            !float.IsFinite(RechargeSeconds) || RechargeSeconds < .2f || RechargeSeconds > 10f)
            throw new InvalidOperationException("Springboard requires finite bounded launch, width and recharge.");
        PhysicsBody2D support = GetNodeOrNull<PhysicsBody2D>(SupportBodyPath);
        CollisionShape2D collider = GetNodeOrNull<CollisionShape2D>("Collision");
        if (support == null || !support.IsInsideTree() || support.GetWorld2D() != GetWorld2D() ||
            !NativeSensorMatches(collider, Width, HasStagedOwner()))
            throw new InvalidOperationException("Springboard requires one authored sensor and its real native support.");
        _support = support; _collider = collider;
        _launch = LaunchVelocity; _width = Width; _recharge = RechargeSeconds;
        CollisionLayer = CombatCollisionLayers.None;
        CollisionMask = CombatCollisionLayers.Player;
        Monitoring = true; Monitorable = false;
        ProcessPhysicsPriority = 100; // Consume this tick's already-resolved Motor floor contacts.
        _ready = true;
    }

    public override void _ExitTree()
    {
        _epoch++; _ready = false; _latched.Clear(); _present.Clear(); _support = null; _collider = null;
    }

    public void ResetForCheckpoint()
    {
        _epoch++; _cooldown = 0f; _latched.Clear(); _present.Clear();
        // Sequence stays monotonic: presentation must not replay an old launch.
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0d || !OwnsActivation()) return;

        _cooldown = Mathf.Max(0f, _cooldown - (float)delta);
        Godot.Collections.Array<Node2D> bodies = GetOverlappingBodies();
        using Godot.Collections.Array backing = (Godot.Collections.Array)bodies;
        _present.Clear();
        foreach (Node2D body in bodies)
            if (IsInstanceValid(body) && !body.IsQueuedForDeletion()) _present.Add(body.GetInstanceId());
        _latched.RemoveWhere(id => !_present.Contains(id));
        if (_cooldown > 0f) { QueueRedraw(); return; }

        foreach (Node2D node in bodies)
        {
            if (node is not CharacterBody2D player || !LivePlayer(player) ||
                _latched.Contains(player.GetInstanceId()) || !HasTopContact(player)) continue;
            PlayerMotor motor = player.GetNodeOrNull<PlayerMotor>("Motor");
            if (motor == null) continue;
            ulong epoch = _epoch;
            _latched.Add(player.GetInstanceId()); _cooldown = _recharge;
            MechanicalClawController claw = player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
            ulong clawEpoch = claw?.AuthorityRevision ?? 0UL;
            ulong motionEpoch = motor.MotionAuthorityRevision;
            claw?.ResetTransientState();
            // Reset can publish callbacks. A new authority or checkpoint wins;
            // never overwrite it with the previously captured contact.
            if (!OwnsActivation() || _epoch != epoch ||
                !LivePlayer(player) || !OwnerLive(motor) || !motor.CanProcess() ||
                player.GetNodeOrNull<PlayerMotor>("Motor") != motor || motor.MotionAuthorityRevision != motionEpoch ||
                claw != null && (!IsInstanceValid(claw) || claw.AuthorityRevision != clawEpoch + 1UL ||
                    claw.CurrentFrame.State != MechanicalClawState.Idle || claw.CurrentTarget != null)) return;
            motor.ApplyEnvironmentalLaunch(_launch);
            LaunchSequence = checked(LaunchSequence + 1UL);
            QueueRedraw();
            Launched?.Invoke(player, _launch, LaunchSequence);
            return;
        }
    }

    private bool LivePlayer(CharacterBody2D player) => OwnerLive(player) && player.CanProcess() && player.IsInGroup("player") &&
        player.GetWorld2D() == GetWorld2D() &&
        player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is > 0;

    private bool OwnsActivation() => _ready && OwnerLive(this) && OwnerLive(_support) &&
        IsVisibleInTree() && Monitoring && (CollisionMask & CombatCollisionLayers.Player) != 0 &&
        !GetTree().Paused && CanProcess() && NativeSensorMatches(_collider, _width);

    private bool NativeSensorMatches(CollisionShape2D collider, float width, bool preparing = false)
    {
        if (!IsInstanceValid(collider) || !collider.IsInsideTree() || collider.GetParent() != this ||
            collider.Disabled && !preparing || PhysicsServer2D.AreaGetShapeCount(GetRid()) != 1) return false;
        Rid shape = PhysicsServer2D.AreaGetShape(GetRid(), 0);
        Transform2D actual = PhysicsServer2D.AreaGetShapeTransform(GetRid(), 0);
        return PhysicsServer2D.ShapeGetType(shape) == PhysicsServer2D.ShapeType.Rectangle &&
            PhysicsServer2D.ShapeGetData(shape).AsVector2().IsEqualApprox(new Vector2(width * .5f, 6f)) &&
            actual.Origin.IsEqualApprox(new Vector2(0,-6)) &&
            actual.X.IsEqualApprox(Vector2.Right) && actual.Y.IsEqualApprox(Vector2.Down) &&
            collider.Transform.IsEqualApprox(actual);
    }

    private bool HasStagedOwner()
    {
        // The stream transaction disables authored collisions before Ready and
        // restores them at commit. Validate their geometry now; never enable it
        // here or grant activation before that transaction completes.
        for(Node current=this;current!=null;current=current.GetParent())
            if(current.HasMeta("_seamless_staged") && current.GetMeta("_seamless_staged").AsBool()) return true;
        return false;
    }

    private static bool OwnerLive(Node owner)
    {
        if (!IsInstanceValid(owner) || !owner.IsInsideTree()) return false;
        for (Node current = owner; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion() ||
                current.HasMeta("_seamless_staged") && current.GetMeta("_seamless_staged").AsBool()) return false;
        return true;
    }

    private bool HasTopContact(CharacterBody2D player)
    {
        if (!player.IsOnFloor() || player.Velocity.Y < -.1f) return false;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D contact = player.GetSlideCollision(index);
            if (contact.GetColliderRid() != _support.GetRid() || contact.GetNormal().Dot(Vector2.Up) < .98f)
                continue;
            Vector2 local = ToLocal(contact.GetPosition());
            if (local.IsFinite() && Mathf.Abs(local.X) <= _width * .5f && Mathf.Abs(local.Y) <= 2f)
                return true;
        }
        return false;
    }

    public override void _Draw()
    {
        if (!_ready) return;
        float depression = _cooldown > _recharge - .12f ? 3f : 0f;
        Color accent = new(_cooldown > 0f ? "67777b" : "d4aa58");
        DrawRect(new Rect2(-_width * .5f, -6, _width, 10), new Color("162329"));
        DrawLine(new(-_width * .5f + 3, -6 + depression), new(_width * .5f - 3, -6 + depression), accent, 4f, true);
        Vector2 direction = _launch.Normalized(), tip = new Vector2(0, -18) + direction * 12f;
        Vector2 tail = tip - direction * 18f, side = direction.Orthogonal() * 5f;
        DrawLine(tail, tip, accent, 2f, true);
        DrawLine(tip, tip - direction * 7f + side, accent, 2f, true);
        DrawLine(tip, tip - direction * 7f - side, accent, 2f, true);
    }
}
