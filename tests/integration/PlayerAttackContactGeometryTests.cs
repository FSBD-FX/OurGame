using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class PlayerAttackContactGeometryTests
{
    public static void Run(SceneTree tree)
    {
        var root = new Node2D { Name = "ContactGeometryFixture" };
        var combat = new Node2D { Name = "Combat" };
        var hitbox = new Node2D { Name = "AttackHitbox" };
        var attack = new CollisionShape2D { Name = "AttackCollider", Shape = new RectangleShape2D { Size = new Vector2(10, 10) } };
        var target = new Hurtbox { Name = "Target", CollisionLayer = 0, CollisionMask = 0 };
        var surface = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 20) } };
        root.AddChild(combat);
        root.AddChild(new HealthComponent { Name = "Health" });
        combat.AddChild(hitbox);
        hitbox.AddChild(attack);
        root.AddChild(target);
        target.AddChild(surface);
        tree.Root.AddChild(root);
        try
        {
            // A 10px attack at (0,0) penetrates a 20px target by 1px.
            // Independent literal surface coordinates, not an oracle using the resolver.
            foreach (var row in new[] {
                (Center: new Vector2(14, 0), Axis: 0, Expected: 4f),
                (Center: new Vector2(-14, 0), Axis: 0, Expected: -4f),
                (Center: new Vector2(0, -14), Axis: 1, Expected: -4f),
                (Center: new Vector2(0, 14), Axis: 1, Expected: 4f) })
            {
                target.Position = row.Center;
                // This synchronous geometry fixture has no physics/process boundary.
                // Observed: the parent already reported -14 while its child's cached
                // GlobalTransform still reported +14 from the preceding row. Flush
                // transform notifications explicitly; real contact callbacks occur
                // after the engine's normal collision-transform synchronization.
                target.ForceUpdateTransform();
                surface.ForceUpdateTransform();
                Transform2D targetWorld = target.GlobalTransform;
                Transform2D surfaceWorld = surface.GlobalTransform;
                Transform2D attackWorld = attack.GlobalTransform;
                Vector2[] rawContacts = attack.Shape.CollideAndGetContacts(
                    attackWorld, surface.Shape, surfaceWorld);
                GD.Print($"PLAYER_CONTACT_GEOMETRY_DIAGNOSTIC row={row.Center} " +
                    $"target_position={target.Position} target_global={targetWorld} " +
                    $"surface_local={surface.Transform} surface_global={surfaceWorld} " +
                    $"attack_global={attackWorld} contacts=[{string.Join(", ", rawContacts)}]");
                Vector2 contact = PlayerAttackContact.Resolve(combat, target, new Vector2(900, 900));
                GD.Print($"PLAYER_CONTACT_GEOMETRY_RESOLVED row={row.Center} contact={contact}");
                TestAssert.Near(row.Expected, row.Axis == 0 ? contact.X : contact.Y, .02f,
                    $"contact remains on the literal target-facing surface for target {row.Center}");
                TestAssert.True(Mathf.Abs(row.Axis == 0 ? contact.Y : contact.X) <= 5.02f,
                    "contact lies beside the attacking shape rather than an unrelated far edge");
            }

            target.Position = new Vector2(6, -3);
            surface.Position = new Vector2(8, 3);
            root.Position = new Vector2(100, 200);
            root.Rotation = Mathf.Pi * .5f;
            root.ForceUpdateTransform();
            combat.ForceUpdateTransform();
            hitbox.ForceUpdateTransform();
            attack.ForceUpdateTransform();
            target.ForceUpdateTransform();
            surface.ForceUpdateTransform();
            Vector2 offsetContact = PlayerAttackContact.Resolve(combat, target, Vector2.Zero);
            TestAssert.Near(204f, offsetContact.Y, .02f,
                "offset target shape and rotated world parent produce a world-space contact");
            TestAssert.True(Mathf.Abs(offsetContact.X - 100f) <= 5.02f,
                "rotated contact remains in the literal attacking span");

            surface.Shape = null;
            TestAssert.Equal(attack.GlobalPosition, PlayerAttackContact.Resolve(combat, target, Vector2.One),
                "missing target geometry falls back to the actual attack origin, not target center");
            surface.Shape = new RectangleShape2D { Size = new Vector2(20, 20) };
            target.Position = new Vector2(1000, 1000);
            target.ForceUpdateTransform();
            surface.ForceUpdateTransform();
            TestAssert.Equal(attack.GlobalPosition, PlayerAttackContact.Resolve(combat, target, Vector2.One),
                "non-overlap signal fixtures do not invent a distant physical contact");
            hitbox.RemoveChild(attack);
            attack.Free();
            TestAssert.Equal(new Vector2(9, 17), PlayerAttackContact.Resolve(combat, target, new Vector2(9, 17)),
                "missing attack shape preserves the caller's safe fallback");
            GD.Print("PLAYER_ATTACK_CONTACT_GEOMETRY_OK directions=4 offset_rotated=True fallback=True");
        }
        finally { root.Free(); }
    }
}
