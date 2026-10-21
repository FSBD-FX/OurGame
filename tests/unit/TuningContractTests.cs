using System;
using System.Collections.Generic;
using Knight.Config;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class TuningContractTests
{
    public static void Run()
    {
        DefaultsAreValid();
        PlayerTuningForwardsEveryValue();
        ThreadTuningForwardsEveryValue();
        PlayerEnergyTuningForwardsEveryValue();
        PlayerTuningRejectsInvalidBoundaries();
        ThreadTuningRejectsInvalidBoundaries();
        PlayerEnergyTuningRejectsInvalidBoundaries();
    }

    private static void DefaultsAreValid()
    {
        TestAssert.Equal(0, new PlayerTuning().Validate().Count, "default player tuning is valid");
        TestAssert.Equal(0, new ThreadTuning().Validate().Count, "default thread tuning is valid");
    }

    private static void PlayerTuningForwardsEveryValue()
    {
        var tuning = new PlayerTuning
        {
            MaxRunSpeed = 101f,
            GroundAcceleration = 102f,
            GroundDeceleration = 103f,
            AirAcceleration = 104f,
            Gravity = 105f,
            MaxFallSpeed = 106f,
            JumpSpeed = 107f,
            JumpCutSpeed = 108f,
            CoyoteSeconds = 1.09f,
            JumpBufferSeconds = 1.10f,
            DashSpeed = 111f,
            DashSeconds = 1.12f,
            AttackStartupSeconds = 1.13f,
            AttackActiveSeconds = 1.14f,
            AttackRecoverySeconds = 1.15f,
            AttackDamage = 16,
            AttackKnockback = 117f,
            DownBounceSpeed = 118f,
            AimDeadZone = 0.19f
        };

        PlayerMotionSettings motion = tuning.ToMotionSettings();
        PlayerCombatSettings combat = tuning.ToCombatSettings();

        TestAssert.Equal(101f, motion.MaxRunSpeed, "run speed is forwarded");
        TestAssert.Equal(102f, motion.GroundAcceleration, "ground acceleration is forwarded");
        TestAssert.Equal(103f, motion.GroundDeceleration, "ground deceleration is forwarded");
        TestAssert.Equal(104f, motion.AirAcceleration, "air acceleration is forwarded");
        TestAssert.Equal(105f, motion.Gravity, "gravity is forwarded");
        TestAssert.Equal(106f, motion.MaxFallSpeed, "fall speed is forwarded");
        TestAssert.Equal(107f, motion.JumpSpeed, "jump speed is forwarded");
        TestAssert.Equal(108f, motion.JumpCutSpeed, "jump cut speed is forwarded");
        TestAssert.Equal(1.09f, motion.CoyoteSeconds, "coyote time is forwarded");
        TestAssert.Equal(1.10f, motion.JumpBufferSeconds, "jump buffer is forwarded");
        TestAssert.Equal(111f, motion.DashSpeed, "dash speed is forwarded");
        TestAssert.Equal(1.12f, motion.DashSeconds, "dash time is forwarded");
        TestAssert.Equal(1.13f, combat.StartupSeconds, "attack startup is forwarded");
        TestAssert.Equal(1.14f, combat.ActiveSeconds, "attack active time is forwarded");
        TestAssert.Equal(1.15f, combat.RecoverySeconds, "attack recovery is forwarded");
        TestAssert.Equal(16, combat.Damage, "attack damage is forwarded");
        TestAssert.Equal(117f, combat.Knockback, "attack knockback is forwarded");
        TestAssert.Equal(118f, combat.DownBounceSpeed, "down bounce is forwarded");
        TestAssert.Equal(0.19f, combat.AimDeadZone, "aim dead zone is forwarded");
    }

    private static void ThreadTuningForwardsEveryValue()
    {
        var tuning = new ThreadTuning
        {
            TapThresholdSeconds = .11f,
            TapAssistRange = 201f,
            TapConeHalfAngleDegrees = 22f,
            HoldRange = 203f,
            HoldConeHalfAngleDegrees = 24f,
            CooldownSeconds = 2.05f,
            PlayerPullSpeed = 206f,
            EnemyPullSpeed = 207f,
            VisualSeconds = 2.08f,
            WindupSeconds = .09f,
            MinimumTravelSeconds = .10f,
            MaximumTravelSeconds = .11f,
            TravelDistanceForMaximumDuration = 212f,
            LatchSeconds = .13f,
            ApplySeconds = .14f,
            RecoverySeconds = .15f
        };

        var settings = tuning.ToSettings();
        TestAssert.Equal(.11f, settings.TapThresholdSeconds, "tap threshold is forwarded");
        TestAssert.Equal(201f, settings.TapAssistRange, "tap assist range is forwarded");
        TestAssert.Equal(22f, settings.TapConeHalfAngleDegrees, "tap cone is forwarded");
        TestAssert.Equal(203f, settings.HoldRange, "hold range is forwarded");
        TestAssert.Equal(24f, settings.HoldConeHalfAngleDegrees, "hold cone is forwarded");
        TestAssert.Equal(2.05f, settings.CooldownSeconds, "thread cooldown is forwarded");
        TestAssert.Equal(206f, settings.PlayerPullSpeed, "player pull speed is forwarded");
        TestAssert.Equal(207f, settings.EnemyPullSpeed, "enemy pull speed is forwarded");
        TestAssert.Equal(2.08f, settings.VisualSeconds, "thread visual time is forwarded");
        TestAssert.Equal(.09f, settings.WindupSeconds, "windup is forwarded");
        TestAssert.Equal(.10f, settings.MinimumTravelSeconds, "minimum travel is forwarded");
        TestAssert.Equal(.11f, settings.MaximumTravelSeconds, "maximum travel is forwarded");
        TestAssert.Equal(212f, settings.TravelDistanceForMaximumDuration, "travel distance is forwarded");
        TestAssert.Equal(.13f, settings.LatchSeconds, "latch is forwarded");
        TestAssert.Equal(.14f, settings.ApplySeconds, "apply is forwarded");
        TestAssert.Equal(.15f, settings.RecoverySeconds, "recovery is forwarded");
    }

    private static void PlayerEnergyTuningForwardsEveryValue()
    {
        var tuning = new PlayerEnergyTuning
        {
            MaxEnergy = 100f,
            AttackCost = 12f,
            RegenDelaySeconds = .55f,
            RegenPerSecond = 28f,
            ShieldDrainPerSecond = 24f,
            ShieldHeatPerSecond = 42f,
            HeatCoolDelaySeconds = .35f,
            HeatCoolPerSecond = 35f,
            OverheatUnlockHeat = 35f
        };

        PlayerEnergySettings settings = tuning.ToSettings();
        TestAssert.Equal(100f, settings.MaxEnergy, "energy maximum is forwarded");
        TestAssert.Equal(12f, settings.AttackCost, "attack cost is forwarded");
        TestAssert.Equal(.55f, settings.RegenDelaySeconds, "energy regen delay is forwarded");
        TestAssert.Equal(28f, settings.RegenPerSecond, "energy regen rate is forwarded");
        TestAssert.Equal(24f, settings.ShieldDrainPerSecond, "shield drain is forwarded");
        TestAssert.Equal(42f, settings.ShieldHeatPerSecond, "shield heat is forwarded");
        TestAssert.Equal(.35f, settings.HeatCoolDelaySeconds, "heat cooling delay is forwarded");
        TestAssert.Equal(35f, settings.HeatCoolPerSecond, "heat cooling rate is forwarded");
        TestAssert.Equal(35f, settings.OverheatUnlockHeat, "overheat unlock heat is forwarded");
        TestAssert.Equal(0, tuning.Validate().Count, "approved energy tuning is valid");
    }

    private static void PlayerTuningRejectsInvalidBoundaries()
    {
        var cases = new (string Name, Action<PlayerTuning> Mutate)[]
        {
            (nameof(PlayerTuning.MaxRunSpeed), x => x.MaxRunSpeed = 0f),
            (nameof(PlayerTuning.GroundAcceleration), x => x.GroundAcceleration = 0f),
            (nameof(PlayerTuning.GroundDeceleration), x => x.GroundDeceleration = 0f),
            (nameof(PlayerTuning.AirAcceleration), x => x.AirAcceleration = 0f),
            (nameof(PlayerTuning.Gravity), x => x.Gravity = 0f),
            (nameof(PlayerTuning.MaxFallSpeed), x => x.MaxFallSpeed = 0f),
            (nameof(PlayerTuning.JumpSpeed), x => x.JumpSpeed = 0f),
            (nameof(PlayerTuning.JumpCutSpeed), x => x.JumpCutSpeed = 0f),
            (nameof(PlayerTuning.CoyoteSeconds), x => x.CoyoteSeconds = -0.01f),
            (nameof(PlayerTuning.JumpBufferSeconds), x => x.JumpBufferSeconds = -0.01f),
            (nameof(PlayerTuning.DashSpeed), x => x.DashSpeed = 0f),
            (nameof(PlayerTuning.DashSeconds), x => x.DashSeconds = 0f),
            (nameof(PlayerTuning.AttackStartupSeconds), x => x.AttackStartupSeconds = -0.01f),
            (nameof(PlayerTuning.AttackActiveSeconds), x => x.AttackActiveSeconds = 0f),
            (nameof(PlayerTuning.AttackRecoverySeconds), x => x.AttackRecoverySeconds = -0.01f),
            (nameof(PlayerTuning.AttackDamage), x => x.AttackDamage = 0),
            (nameof(PlayerTuning.AttackKnockback), x => x.AttackKnockback = -0.01f),
            (nameof(PlayerTuning.DownBounceSpeed), x => x.DownBounceSpeed = 0f),
            (nameof(PlayerTuning.AimDeadZone), x => x.AimDeadZone = -0.01f),
            (nameof(PlayerTuning.AimDeadZone), x => x.AimDeadZone = 1.01f)
        };

        foreach ((string name, Action<PlayerTuning> mutate) in cases)
        {
            var tuning = new PlayerTuning();
            mutate(tuning);
            AssertNamedError(tuning.Validate(), name);
        }
    }

    private static void ThreadTuningRejectsInvalidBoundaries()
    {
        var cases = new (string Name, Action<ThreadTuning> Mutate)[]
        {
            (nameof(ThreadTuning.Range), x => x.Range = 0f),
            (nameof(ThreadTuning.ConeHalfAngleDegrees), x => x.ConeHalfAngleDegrees = 0f),
            (nameof(ThreadTuning.ConeHalfAngleDegrees), x => x.ConeHalfAngleDegrees = 181f),
            (nameof(ThreadTuning.TapThresholdSeconds), x => x.TapThresholdSeconds = -0.01f),
            (nameof(ThreadTuning.TapAssistRange), x => x.TapAssistRange = 0f),
            (nameof(ThreadTuning.TapConeHalfAngleDegrees), x => x.TapConeHalfAngleDegrees = 0f),
            (nameof(ThreadTuning.TapConeHalfAngleDegrees), x => x.TapConeHalfAngleDegrees = 181f),
            (nameof(ThreadTuning.HoldRange), x => x.HoldRange = 0f),
            (nameof(ThreadTuning.HoldConeHalfAngleDegrees), x => x.HoldConeHalfAngleDegrees = 0f),
            (nameof(ThreadTuning.HoldConeHalfAngleDegrees), x => x.HoldConeHalfAngleDegrees = 181f),
            (nameof(ThreadTuning.CooldownSeconds), x => x.CooldownSeconds = -0.01f),
            (nameof(ThreadTuning.PlayerPullSpeed), x => x.PlayerPullSpeed = 0f),
            (nameof(ThreadTuning.EnemyPullSpeed), x => x.EnemyPullSpeed = 0f),
            (nameof(ThreadTuning.VisualSeconds), x => x.VisualSeconds = -0.01f),
            (nameof(ThreadTuning.WindupSeconds), x => x.WindupSeconds = -0.01f),
            (nameof(ThreadTuning.MinimumTravelSeconds), x => x.MinimumTravelSeconds = 0f),
            (nameof(ThreadTuning.MaximumTravelSeconds), x => x.MaximumTravelSeconds = 0f),
            (nameof(ThreadTuning.MaximumTravelSeconds), x => x.MaximumTravelSeconds = .01f),
            (nameof(ThreadTuning.TravelDistanceForMaximumDuration), x => x.TravelDistanceForMaximumDuration = 0f),
            (nameof(ThreadTuning.LatchSeconds), x => x.LatchSeconds = -0.01f),
            (nameof(ThreadTuning.ApplySeconds), x => x.ApplySeconds = -0.01f),
            (nameof(ThreadTuning.RecoverySeconds), x => x.RecoverySeconds = -0.01f)
        };

        foreach ((string name, Action<ThreadTuning> mutate) in cases)
        {
            var tuning = new ThreadTuning();
            mutate(tuning);
            AssertNamedError(tuning.Validate(), name);
        }

        var nonFiniteCases = new (string Name, Action<ThreadTuning> Mutate)[]
        {
            (nameof(ThreadTuning.TapThresholdSeconds), x => x.TapThresholdSeconds = float.NaN),
            (nameof(ThreadTuning.TapAssistRange), x => x.TapAssistRange = float.NaN),
            (nameof(ThreadTuning.TapConeHalfAngleDegrees), x => x.TapConeHalfAngleDegrees = float.NaN),
            (nameof(ThreadTuning.HoldRange), x => x.HoldRange = float.NaN),
            (nameof(ThreadTuning.HoldConeHalfAngleDegrees), x => x.HoldConeHalfAngleDegrees = float.NaN),
            (nameof(ThreadTuning.CooldownSeconds), x => x.CooldownSeconds = float.NaN),
            (nameof(ThreadTuning.PlayerPullSpeed), x => x.PlayerPullSpeed = float.NaN),
            (nameof(ThreadTuning.EnemyPullSpeed), x => x.EnemyPullSpeed = float.NaN),
            (nameof(ThreadTuning.VisualSeconds), x => x.VisualSeconds = float.NaN),
            (nameof(ThreadTuning.WindupSeconds), x => x.WindupSeconds = float.NaN),
            (nameof(ThreadTuning.MinimumTravelSeconds), x => x.MinimumTravelSeconds = float.NaN),
            (nameof(ThreadTuning.MaximumTravelSeconds), x => x.MaximumTravelSeconds = float.NaN),
            (nameof(ThreadTuning.TravelDistanceForMaximumDuration), x => x.TravelDistanceForMaximumDuration = float.NaN),
            (nameof(ThreadTuning.LatchSeconds), x => x.LatchSeconds = float.NaN),
            (nameof(ThreadTuning.ApplySeconds), x => x.ApplySeconds = float.NaN),
            (nameof(ThreadTuning.RecoverySeconds), x => x.RecoverySeconds = float.NaN)
        };
        foreach ((string name, Action<ThreadTuning> mutate) in nonFiniteCases)
        {
            var tuning = new ThreadTuning();
            mutate(tuning);
            AssertNamedError(tuning.Validate(), name);
        }
    }

    private static void PlayerEnergyTuningRejectsInvalidBoundaries()
    {
        var cases = new (string Name, Action<PlayerEnergyTuning> Mutate)[]
        {
            (nameof(PlayerEnergyTuning.MaxEnergy), x => x.MaxEnergy = 0f),
            (nameof(PlayerEnergyTuning.AttackCost), x => x.AttackCost = -0.01f),
            (nameof(PlayerEnergyTuning.AttackCost), x => x.AttackCost = 101f),
            (nameof(PlayerEnergyTuning.RegenDelaySeconds), x => x.RegenDelaySeconds = -0.01f),
            (nameof(PlayerEnergyTuning.RegenPerSecond), x => x.RegenPerSecond = -0.01f),
            (nameof(PlayerEnergyTuning.ShieldDrainPerSecond), x => x.ShieldDrainPerSecond = -0.01f),
            (nameof(PlayerEnergyTuning.ShieldHeatPerSecond), x => x.ShieldHeatPerSecond = -0.01f),
            (nameof(PlayerEnergyTuning.HeatCoolDelaySeconds), x => x.HeatCoolDelaySeconds = -0.01f),
            (nameof(PlayerEnergyTuning.HeatCoolPerSecond), x => x.HeatCoolPerSecond = -0.01f),
            (nameof(PlayerEnergyTuning.OverheatUnlockHeat), x => x.OverheatUnlockHeat = 100f),
            (nameof(PlayerEnergyTuning.OverheatUnlockHeat), x => x.OverheatUnlockHeat = -0.01f)
        };

        foreach ((string name, Action<PlayerEnergyTuning> mutate) in cases)
        {
            var tuning = new PlayerEnergyTuning();
            mutate(tuning);
            AssertNamedError(tuning.Validate(), name);
        }

        var nonFinite = new PlayerEnergyTuning { ShieldHeatPerSecond = float.NaN };
        AssertNamedError(nonFinite.Validate(), nameof(PlayerEnergyTuning.ShieldHeatPerSecond));
    }

    private static void AssertNamedError(IReadOnlyList<string> errors, string propertyName)
    {
        foreach (string error in errors)
            if (error.Contains(propertyName, StringComparison.Ordinal)) return;
        throw new InvalidOperationException($"Assertion failed: invalid tuning names {propertyName}");
    }
}
