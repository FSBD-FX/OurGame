using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerCombatModelTests
{
    public static void Run()
    {
        AttackUsesUpWhenAimYIsNegativePastDeadZone();
        AttackUsesDownOnlyWhileAirborne();
        HorizontalAttackUsesAimBeforeMovementFacing();
        HorizontalAttackKeepsFacingWhenAimIsInsideDeadZone();
        AttackCannotRestartDuringActiveOrRecoveryFrames();
        ANewAttackReceivesANewSourceAttackId();
    }

    private static void AttackUsesUpWhenAimYIsNegativePastDeadZone()
    {
        AttackDirection direction = PlayerCombatModel.ResolveDirection(new Vector2(0.8f, -0.51f), false, -1);

        TestAssert.Equal(AttackDirection.Up, direction, "upward aim takes precedence over horizontal aim");
    }

    private static void AttackUsesDownOnlyWhileAirborne()
    {
        AttackDirection grounded = PlayerCombatModel.ResolveDirection(new Vector2(0f, 0.51f), false, 1);
        AttackDirection airborne = PlayerCombatModel.ResolveDirection(new Vector2(0f, 0.51f), true, 1);

        TestAssert.Equal(AttackDirection.Horizontal, grounded, "downward aim remains horizontal while grounded");
        TestAssert.Equal(AttackDirection.Down, airborne, "downward aim attacks down while airborne");
    }

    private static void HorizontalAttackUsesAimBeforeMovementFacing()
    {
        AttackDirection direction = PlayerCombatModel.ResolveDirection(new Vector2(-0.8f, 0f), false, 1,
            out int facing);

        TestAssert.Equal(-1, facing, "left aim wins when movement has established a right-facing fallback");
        TestAssert.Equal(AttackDirection.Horizontal, direction, "horizontal aim remains a horizontal attack");
    }

    private static void HorizontalAttackKeepsFacingWhenAimIsInsideDeadZone()
    {
        PlayerCombatModel.ResolveDirection(new Vector2(0.5f, 0f), false, -1, out int facing);

        TestAssert.Equal(-1, facing, "aim inside the dead zone preserves the existing facing");
    }

    private static void AttackCannotRestartDuringActiveOrRecoveryFrames()
    {
        var model = new PlayerCombatModel();
        ulong? attack = model.TryStart(AttackDirection.Horizontal);

        AttackFrame startup = model.Tick(0.05d);
        TestAssert.True(!startup.IsActive, "attack is inactive before startup completes");
        AttackFrame active = model.Tick(0.02d);
        TestAssert.True(active.IsActive, "attack is active during the active interval");
        TestAssert.True(!model.TryStart(AttackDirection.Up).HasValue, "attack cannot restart during active frames");
        AttackFrame recovery = model.Tick(0.10d);
        TestAssert.True(!recovery.IsActive, "attack is inactive during recovery");
        TestAssert.True(!model.TryStart(AttackDirection.Down).HasValue, "attack cannot restart during recovery frames");
        TestAssert.True(attack.HasValue, "first attack receives an attack ID");
    }

    private static void ANewAttackReceivesANewSourceAttackId()
    {
        var model = new PlayerCombatModel();
        ulong first = model.TryStart(AttackDirection.Horizontal)!.Value;
        model.Tick(0.30d);
        ulong second = model.TryStart(AttackDirection.Up)!.Value;

        TestAssert.True(second > first, "each completed attack receives a monotonically increasing source attack ID");
    }
}
