using Godot;
using Knight.Combat;

namespace Knight.Tests.Unit;

public static class PlayerCombatSceneTests
{
    public static void Run()
    {
        GeneratedPlayerHitboxCannotDetectOwnHurtbox();
        HitboxOnlyMonitorsDuringActiveAttackWindow();
        HitboxConnectsEachExternalHurtboxOnlyOnce();
    }

    private static void GeneratedPlayerHitboxCannotDetectOwnHurtbox()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn");
        Node player = scene.Instantiate();
        var hurtbox = player.GetNode<Hurtbox>("Hurtbox");
        var hitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");

        TestAssert.Equal(CombatCollisionLayers.Player, hurtbox.CollisionLayer,
            "player hurtbox uses the dedicated hurtbox layer");
        TestAssert.Equal(CombatCollisionLayers.PlayerAttack, hitbox.CollisionLayer,
            "player attack hitbox does not publish a collision layer");
        TestAssert.Equal(CombatCollisionLayers.Enemy, hitbox.CollisionMask,
            "player attack hitbox only scans the hurtbox layer");
        TestAssert.True((hitbox.CollisionMask & hurtbox.CollisionLayer) == 0,
            "player attack does not scan the player's own hurtbox layer");
        TestAssert.True(hitbox.IsOwnHurtbox(hurtbox),
            "owner/source guard rejects the player's own hurtbox even if collision configuration changes");
        player.Free();
    }

    private static void HitboxOnlyMonitorsDuringActiveAttackWindow()
    {
        var hitbox = new Hitbox();

        hitbox.Begin(1UL);
        TestAssert.True(hitbox.Monitoring, "attack hitbox monitors during active attack frames");
        hitbox.End();
        TestAssert.True(!hitbox.Monitoring, "attack hitbox stops monitoring outside active attack frames");
        hitbox.Free();
    }

    private static void HitboxConnectsEachExternalHurtboxOnlyOnce()
    {
        var source = new Node { Name = "Source" };
        var target = new Node { Name = "Target" };
        var health = new HealthComponent { Name = "Health", MaxHealth = 5, InvulnerabilitySeconds = 0f };
        var hurtbox = new Hurtbox { Name = "Hurtbox" };
        var hitbox = new Hitbox { Name = "Hitbox" };
        source.AddChild(hitbox);
        target.AddChild(health);
        target.AddChild(hurtbox);
        hitbox.Owner = source;
        health.Owner = target;
        hurtbox.Owner = target;
        int connections = 0;
        hitbox.Connected += _ => connections++;

        hitbox.Begin(42UL);
        bool first = hitbox.TryConnect(hurtbox);
        bool duplicate = hitbox.TryConnect(hurtbox);

        TestAssert.True(first, "first external hurtbox overlap connects");
        TestAssert.True(!duplicate, "same hurtbox does not connect twice in an attack");
        TestAssert.Equal(1, connections, "one overlap emits one connected signal");
        TestAssert.Equal(4, health.CurrentHealth, "one overlap applies one damage request");
        source.Free();
        target.Free();
    }
}
