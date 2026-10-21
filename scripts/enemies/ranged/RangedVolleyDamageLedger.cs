using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Enemies.Ranged;

// Kept for the lifetime of the owner's live projectiles. Health's short cache
// and invulnerability cannot substitute for one attempt per receiver/volley.
public sealed class RangedVolleyDamageLedger
{
    private readonly Dictionary<DamageAttackIdentity, HashSet<ulong>> _attempted = new();

    public bool TryDeliver(Hurtbox target, DamageRequest request)
    {
        if (!GodotObject.IsInstanceValid(target) || !target.IsInsideTree() || target.IsQueuedForDeletion() ||
            request.SourceRuntimeId == 0 || request.SourceAttackId == 0 || request.Amount <= 0 ||
            !float.IsFinite(request.Direction.X) || !float.IsFinite(request.Direction.Y) ||
            !float.IsFinite(request.Knockback) || request.Knockback < 0f) return false;
        // Reject an absent path without asking Hurtbox to throw a setup error.
        Node pathNode = target.GetNodeOrNull<Node>(target.DamageReceiverPath);
        if (!GodotObject.IsInstanceValid(pathNode) || pathNode.IsQueuedForDeletion() ||
            pathNode is not IDamageReceiver) return false;
        IDamageReceiver receiver = target.ResolveDamageReceiver();
        if (receiver is not Node receiverNode || !GodotObject.IsInstanceValid(receiverNode) ||
            receiverNode.IsQueuedForDeletion()) return false;
        if (!_attempted.TryGetValue(request.AttackIdentity, out HashSet<ulong> receivers))
        {
            receivers = new HashSet<ulong>();
            _attempted.Add(request.AttackIdentity, receivers);
        }
        // Mark BEFORE Receive: invulnerability, a block or synchronous callbacks
        // must not permit a surviving pellet to submit the same attempt later.
        if (!receivers.Add(receiverNode.GetInstanceId())) return false;
        return receiver.ReceiveDamage(request);
    }

    public void Release(DamageAttackIdentity identity) => _attempted.Remove(identity);
    public void Clear() => _attempted.Clear();
}
