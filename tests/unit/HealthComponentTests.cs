using Godot;
using Knight.Combat;

namespace Knight.Tests.Unit;

public static class HealthComponentTests
{
    public static void Run()
    {
        DamageReducesHealthOncePerSourceAttack();
        InvulnerabilityRejectsImmediateSecondAttack();
        HealthNeverDropsBelowZeroAndDeathFiresOnce();
    }

    private static void DamageReducesHealthOncePerSourceAttack()
    {
        var health = new HealthComponent { MaxHealth = 5 };
        var request = new DamageRequest(2, Vector2.Right, 80f, 17UL);
        int damaged = 0;
        health.Damaged += (_, _) => damaged++;

        bool first = health.ApplyDamage(request);
        bool duplicate = health.ApplyDamage(request);

        TestAssert.True(first, "the first source attack damages health");
        TestAssert.True(!duplicate, "a source attack cannot damage health twice");
        TestAssert.Equal(3, health.CurrentHealth, "accepted damage reduces current health once");
        TestAssert.Equal(1, damaged, "accepted damage emits one damaged signal");
        health.Free();
    }

    private static void InvulnerabilityRejectsImmediateSecondAttack()
    {
        var health = new HealthComponent { MaxHealth = 5 };

        bool first = health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 1UL));
        bool second = health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 2UL));

        TestAssert.True(first, "first attack is accepted");
        TestAssert.True(!second, "a distinct attack is rejected during invulnerability");
        TestAssert.Equal(4, health.CurrentHealth, "invulnerability preserves remaining health");
        health.Free();
    }

    private static void HealthNeverDropsBelowZeroAndDeathFiresOnce()
    {
        var health = new HealthComponent { MaxHealth = 2, InvulnerabilitySeconds = 0f };
        int deaths = 0;
        health.Died += () => deaths++;

        bool lethal = health.ApplyDamage(new DamageRequest(10, Vector2.Up, 0f, 1UL));
        bool afterDeath = health.ApplyDamage(new DamageRequest(1, Vector2.Up, 0f, 2UL));

        TestAssert.True(lethal, "lethal damage is accepted");
        TestAssert.True(!afterDeath, "damage after death is rejected");
        TestAssert.Equal(0, health.CurrentHealth, "health is clamped at zero");
        TestAssert.Equal(1, deaths, "death signal fires exactly once");
        health.Free();
    }
}
