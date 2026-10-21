using Godot;
using Knight.Combat;

namespace Knight.Tests.Integration;

public static class DamageSourceIdentityRuntimeTests
{
    public static void Run()
    {
        PersistentPlayerDistinguishesLocalAttackOneAcrossRuntimeSources();
    }

    private static void PersistentPlayerDistinguishesLocalAttackOneAcrossRuntimeSources()
    {
        SceneTree tree = (SceneTree)Engine.GetMainLoop();
        var fixture = new Node2D { Name = "DamageSourceIdentityFixture" };
        var player = new Node2D { Name = "PersistentPlayer" };
        var health = new HealthComponent { Name = "Health", MaxHealth = 6, InvulnerabilitySeconds = .10f };
        var hurtbox = new Hurtbox { Name = "Hurtbox", DamageReceiverPath = new NodePath("../Health") };
        player.AddChild(health);
        player.AddChild(hurtbox);
        fixture.AddChild(player);

        Node rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate();
        Node moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn").Instantiate();
        Node boss = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn").Instantiate();
        fixture.AddChild(rust);
        fixture.AddChild(moth);
        fixture.AddChild(boss);
        tree.Root.AddChild(fixture);

        Hitbox rustHitbox = rust.GetNode<Hitbox>("AttackHitbox");
        Hitbox mothHitbox = moth.GetNode<Hitbox>("AttackHitbox");
        Hitbox bossHitbox = boss.GetNode<Hitbox>("AttackHitbox");

        TestAssert.True(Strike(rustHitbox, hurtbox, 1UL),
            "ordinary source A local AttackId 1 damages the persistent player");
        health._PhysicsProcess(.11);
        TestAssert.True(!Strike(rustHitbox, hurtbox, 1UL),
            "same source and same local AttackId remains deduplicated after invulnerability");
        TestAssert.Equal(5, health.CurrentHealth,
            "same-source replay cannot remove another health segment");

        TestAssert.True(Strike(mothHitbox, hurtbox, 1UL),
            "ordinary source B local AttackId 1 remains distinct after invulnerability");
        health._PhysicsProcess(.11);
        TestAssert.True(Strike(bossHitbox, hurtbox, 1UL),
            "Boss local AttackId 1 remains distinct from ordinary local AttackId 1");
        TestAssert.Equal(3, health.CurrentHealth,
            "three runtime sources with the same local ordinal each apply exactly once");

        health.ResetFull();
        TestAssert.True(Strike(rustHitbox, hurtbox, 1UL),
            "respawn reset intentionally clears the prior same-source dedup state");
        health._PhysicsProcess(.11);
        TestAssert.True(Strike(mothHitbox, hurtbox, 1UL),
            "source distinction remains intact after respawn reset");
        TestAssert.Equal(4, health.CurrentHealth,
            "reset accepts one hit from each distinct ordinary runtime source");

        fixture.Free();
    }

    private static bool Strike(Hitbox source, Hurtbox target, ulong localAttackId)
    {
        source.Begin(localAttackId);
        bool accepted = source.TryConnect(target);
        source.End();
        return accepted;
    }
}
