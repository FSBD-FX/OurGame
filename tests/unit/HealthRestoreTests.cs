using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Tests.Unit;

public static class HealthRestoreTests
{
    public static void Run()
    {
        RestoreReportsActualGainAndOnlyEmitsHealed();
        InvalidFullAndDeadRestoresDoNothing();
        HealingPreservesTheRemainingInvulnerabilityWindow();
        HealingPreservesSourceAttackDeduplication();
    }

    private static void RestoreReportsActualGainAndOnlyEmitsHealed()
    {
        var health = new HealthComponent { MaxHealth = 5, InvulnerabilitySeconds = 0f };
        try
        {
            int damaged = 0, deaths = 0;
            var heals = new List<(int Restored, int Remaining)>();
            health.Damaged += (_, _) => damaged++;
            health.Died += () => deaths++;
            health.Healed += (restored, remaining) => heals.Add((restored, remaining));
            TestAssert.True(health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 1UL)),
                "restore fixture starts alive with two health");

            TestAssert.Equal(1, health.RestoreHealth(1), "positive restore returns the actual one-point gain");
            TestAssert.Equal(3, health.CurrentHealth, "partial restore does not refill all health");
            TestAssert.Equal(1, heals.Count, "partial restore emits one healing event");
            TestAssert.Equal((1, 3), heals[0], "healing event reports actual gain and remaining health");
            TestAssert.Equal(2, health.RestoreHealth(int.MaxValue),
                "oversized restore caps at missing health without integer overflow");
            TestAssert.Equal(5, health.CurrentHealth, "oversized restore stops at maximum health");
            TestAssert.Equal(2, heals.Count, "capped restore emits exactly one further healing event");
            TestAssert.Equal((2, 5), heals[1], "capped healing event reports actual rather than requested gain");
            TestAssert.Equal(1, damaged, "healing never emits an extra Damaged signal");
            TestAssert.Equal(0, deaths, "healing never emits Died");
        }
        finally { health.Free(); }
    }

    private static void InvalidFullAndDeadRestoresDoNothing()
    {
        var health = new HealthComponent { MaxHealth = 5, InvulnerabilitySeconds = 0f };
        try
        {
            int heals = 0, deaths = 0, damaged = 0;
            health.Healed += (_, _) => heals++;
            health.Died += () => deaths++;
            health.Damaged += (_, _) => damaged++;
            health._Ready();
            TestAssert.Equal(0, health.RestoreHealth(1), "full health rejects a supply without consuming a gain");
            TestAssert.Equal(5, health.CurrentHealth, "full-health restore leaves health unchanged");
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Right, 0f, 1UL)),
                "invalid-amount fixture is alive and injured");
            foreach (int amount in new[] { 0, -1, int.MinValue })
            {
                TestAssert.Equal(0, health.RestoreHealth(amount), "nonpositive restore returns zero");
                TestAssert.Equal(3, health.CurrentHealth, "nonpositive restore cannot damage or heal");
            }
            TestAssert.True(health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 2UL)),
                "fixture takes a real lethal hit");
            TestAssert.Equal(0, health.RestoreHealth(int.MaxValue), "healing cannot revive a dead receiver");
            TestAssert.Equal(0, health.CurrentHealth, "dead receiver stays at zero health");
            TestAssert.True(!health.ApplyDamage(new DamageRequest(1, Vector2.Right, 0f, 3UL)),
                "failed healing cannot clear the dead-state guard");
            TestAssert.Equal(0, heals, "invalid, full and dead restores emit no healing event");
            TestAssert.Equal(2, damaged, "only the two actual attacks emit Damaged");
            TestAssert.Equal(1, deaths, "failed healing does not reset or repeat death");
        }
        finally { health.Free(); }
    }

    private static void HealingPreservesTheRemainingInvulnerabilityWindow()
    {
        var health = new HealthComponent { MaxHealth = 5, InvulnerabilitySeconds = 1f };
        try
        {
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Left, 0f, 1UL)),
                "first attack starts the invulnerability window");
            health._PhysicsProcess(.5);
            TestAssert.Equal(1, health.RestoreHealth(1), "healing is allowed during damage invulnerability");
            var next = new DamageRequest(1, Vector2.Left, 0f, 2UL);
            TestAssert.True(!health.ApplyDamage(next), "healing does not clear remaining invulnerability");
            TestAssert.Equal(4, health.CurrentHealth, "protected health includes the actual healing gain");
            health._PhysicsProcess(.51);
            TestAssert.True(health.ApplyDamage(next),
                "healing does not restart the timer; the rejected attack works after the original expiry");
            TestAssert.Equal(3, health.CurrentHealth, "ordinary damage resumes after the preserved timer expires");
        }
        finally { health.Free(); }
    }

    private static void HealingPreservesSourceAttackDeduplication()
    {
        var health = new HealthComponent { MaxHealth = 5, InvulnerabilitySeconds = 0f };
        try
        {
            var first = new DamageRequest(2, Vector2.Right, 0f, 7UL, SourceRuntimeId: 41UL);
            TestAssert.True(health.ApplyDamage(first), "first source/attack pair damages the receiver");
            TestAssert.Equal(1, health.RestoreHealth(1), "dedup fixture receives a partial heal");
            TestAssert.True(!health.ApplyDamage(first), "healing must not forget an already processed attack");
            TestAssert.Equal(4, health.CurrentHealth, "duplicate damage cannot remove restored health");
            TestAssert.True(health.ApplyDamage(first with { SourceRuntimeId = 42UL }),
                "another source with the same local attack id remains a valid ordinary attack");
            TestAssert.Equal(2, health.CurrentHealth, "healing does not block later distinct-source damage");
        }
        finally { health.Free(); }
    }
}
