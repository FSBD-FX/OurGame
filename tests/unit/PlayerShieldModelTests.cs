using Godot;
using Knight.Combat;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerShieldModelTests
{
    public static void Run()
    {
        FrontNormalIsFullyGuarded();
        RearNormalBypassesShield();
        FrontArcIncludesSeventyFiveDegrees();
        FrontHeavyResolvesToOneDamageAndFortyFivePercentKnockback();
        HeavyWithoutEnergyBreaksGuard();
        UnblockableBypassesShield();
    }

    private static PlayerShieldModel Model() => new(PlayerShieldSettings.Approved);

    private static void FrontNormalIsFullyGuarded()
    {
        PlayerShieldResult result = Model().Resolve(new PlayerShieldInput(
            true, Vector2.Right, Vector2.Right, DamageGuardClass.Normal, true));

        TestAssert.True(result == new PlayerShieldResult(true, 0, 0f, false, false),
            "front normal is negated without spending heavy energy");
    }

    private static void RearNormalBypassesShield()
    {
        PlayerShieldResult result = Model().Resolve(new PlayerShieldInput(
            true, Vector2.Right, Vector2.Left, DamageGuardClass.Normal, true));

        TestAssert.True(!result.Guarded && result.DamageAmount == 1 && result.KnockbackScale == 1f,
            "rear attack bypasses shield unchanged");
    }

    private static void FrontArcIncludesSeventyFiveDegrees()
    {
        Vector2 edge = Vector2.Right.Rotated(Mathf.DegToRad(75f));
        PlayerShieldResult result = Model().Resolve(new PlayerShieldInput(
            true, Vector2.Right, edge, DamageGuardClass.Normal, true));

        TestAssert.True(result.Guarded, "the 150-degree front arc includes its 75-degree boundary");
    }

    private static void FrontHeavyResolvesToOneDamageAndFortyFivePercentKnockback()
    {
        PlayerShieldResult result = Model().Resolve(new PlayerShieldInput(
            true, Vector2.Right, Vector2.Right, DamageGuardClass.Heavy, true));

        TestAssert.True(result == new PlayerShieldResult(true, 1, .45f, true, false),
            "front heavy costs energy and resolves to one damage at 45 percent knockback");
    }

    private static void HeavyWithoutEnergyBreaksGuard()
    {
        PlayerShieldResult result = Model().Resolve(new PlayerShieldInput(
            true, Vector2.Right, Vector2.Right, DamageGuardClass.Heavy, false));

        TestAssert.True(result == new PlayerShieldResult(false, 1, 1f, false, true),
            "front heavy without 24 energy breaks guard and passes through");
    }

    private static void UnblockableBypassesShield()
    {
        PlayerShieldResult result = Model().Resolve(new PlayerShieldInput(
            true, Vector2.Right, Vector2.Right, DamageGuardClass.Unblockable, true));

        TestAssert.True(!result.Guarded && result.DamageAmount == 1 && result.KnockbackScale == 1f,
            "unblockable damage bypasses an otherwise valid shield");
    }
}
