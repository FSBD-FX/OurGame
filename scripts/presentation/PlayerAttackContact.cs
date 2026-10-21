using Godot;
using Knight.Combat;

namespace Knight.Presentation;

/// <summary>Presentation-only contact sampling after an accepted attack connection.</summary>
public static class PlayerAttackContact
{
    public static Vector2 Resolve(Node2D combat, Hurtbox hurtbox, Vector2 fallback)
    {
        if (!GodotObject.IsInstanceValid(combat)) return fallback;
        CollisionShape2D attack = combat.GetNodeOrNull<CollisionShape2D>("AttackHitbox/AttackCollider");
        if (!GodotObject.IsInstanceValid(attack)) return fallback;
        Vector2 origin = attack.GlobalPosition;
        if (attack.Disabled || !GodotObject.IsInstanceValid(attack.Shape) ||
            !GodotObject.IsInstanceValid(hurtbox) || hurtbox.IsQueuedForDeletion()) return origin;

        Vector2 closest = origin;
        float closestDistance = float.PositiveInfinity;
        foreach (Node child in hurtbox.GetChildren())
        {
            if (child is not CollisionShape2D target || target.Disabled ||
                !GodotObject.IsInstanceValid(target.Shape)) continue;
            Vector2[] contacts = attack.Shape.CollideAndGetContacts(
                attack.GlobalTransform, target.Shape, target.GlobalTransform);
            // Each pair is attack-boundary, target-boundary. Use the target surface,
            // not the target origin or the middle of an overlapping bounding box.
            for (int index = 1; index < contacts.Length; index += 2)
            {
                Vector2 point = contacts[index];
                if (!point.IsFinite()) continue;
                float distance = point.DistanceSquaredTo(origin);
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = point;
            }
        }
        // Accepted damage may remove/disable the shape before this observer runs.
        // An unavailable manifold must not change that gameplay result.
        return closest;
    }
}
