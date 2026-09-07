using System;
using System.Collections.Generic;
using Godot;
using Knight.Boss;

namespace Knight.Config;

public readonly record struct BossAttackTiming(
    float TelegraphSeconds,
    float ActiveSeconds,
    float RecoverySeconds);

public readonly record struct BossSettings(
    int MaxHealth,
    float TransitionHealthRatio,
    float IntroSeconds,
    BossAttackTiming SweepTiming,
    BossAttackTiming LeapSlamTiming,
    BossAttackTiming GroundWaveTiming,
    BossAttackTiming HammerThrowTiming,
    float TransitionSeconds,
    float PhaseTwoArmoredSeconds,
    float ExposedSeconds,
    float RetryReadyDelaySeconds,
    float MoveSpeed,
    int ContactDamage)
{
    public BossAttackTiming TimingFor(FallenBellkeeperAttack attack) => attack switch
    {
        FallenBellkeeperAttack.Sweep => SweepTiming,
        FallenBellkeeperAttack.LeapSlam => LeapSlamTiming,
        FallenBellkeeperAttack.GroundWave => GroundWaveTiming,
        FallenBellkeeperAttack.HammerThrow => HammerThrowTiming,
        _ => throw new ArgumentOutOfRangeException(nameof(attack), attack,
            "Only concrete Fallen Bellkeeper attacks have timings.")
    };
}

[GlobalClass]
public partial class BossTuning : Resource
{
    [Export] public int MaxHealth { get; set; } = 12;
    [Export] public float TransitionHealthRatio { get; set; } = .60f;
    [Export] public float IntroSeconds { get; set; } = 1.0f;
    [Export] public float SweepTelegraphSeconds { get; set; } = .80f;
    [Export] public float SweepActiveSeconds { get; set; } = .25f;
    [Export] public float SweepRecoverySeconds { get; set; } = .70f;
    [Export] public float LeapSlamTelegraphSeconds { get; set; } = .90f;
    [Export] public float LeapSlamActiveSeconds { get; set; } = .25f;
    [Export] public float LeapSlamRecoverySeconds { get; set; } = .75f;
    [Export] public float GroundWaveTelegraphSeconds { get; set; } = 1.00f;
    [Export] public float GroundWaveActiveSeconds { get; set; } = .30f;
    [Export] public float GroundWaveRecoverySeconds { get; set; } = .75f;
    [Export] public float HammerThrowTelegraphSeconds { get; set; } = .85f;
    [Export] public float HammerThrowActiveSeconds { get; set; } = .80f;
    [Export] public float HammerThrowRecoverySeconds { get; set; } = .70f;
    [Export] public float TransitionSeconds { get; set; } = 1.20f;
    [Export] public float PhaseTwoArmoredSeconds { get; set; } = .65f;
    [Export] public float ExposedSeconds { get; set; } = 2.0f;
    [Export] public float RetryReadyDelaySeconds { get; set; } = 2.5f;
    [Export] public float MoveSpeed { get; set; } = 90f;
    [Export] public int ContactDamage { get; set; } = 1;

    public BossAttackTiming TimingFor(FallenBellkeeperAttack attack) => attack switch
    {
        FallenBellkeeperAttack.Sweep => new(
            SweepTelegraphSeconds, SweepActiveSeconds, SweepRecoverySeconds),
        FallenBellkeeperAttack.LeapSlam => new(
            LeapSlamTelegraphSeconds, LeapSlamActiveSeconds, LeapSlamRecoverySeconds),
        FallenBellkeeperAttack.GroundWave => new(
            GroundWaveTelegraphSeconds, GroundWaveActiveSeconds, GroundWaveRecoverySeconds),
        FallenBellkeeperAttack.HammerThrow => new(
            HammerThrowTelegraphSeconds, HammerThrowActiveSeconds, HammerThrowRecoverySeconds),
        _ => throw new ArgumentOutOfRangeException(nameof(attack), attack,
            "Only concrete Fallen Bellkeeper attacks have timings.")
    };

    public BossSettings ToSettings() => new(
        MaxHealth,
        TransitionHealthRatio,
        IntroSeconds,
        TimingFor(FallenBellkeeperAttack.Sweep),
        TimingFor(FallenBellkeeperAttack.LeapSlam),
        TimingFor(FallenBellkeeperAttack.GroundWave),
        TimingFor(FallenBellkeeperAttack.HammerThrow),
        TransitionSeconds,
        PhaseTwoArmoredSeconds,
        ExposedSeconds,
        RetryReadyDelaySeconds,
        MoveSpeed,
        ContactDamage);

    public IReadOnlyList<string> Validate() => Validate(ToSettings());

    public static IReadOnlyList<string> Validate(BossSettings settings)
    {
        var errors = new List<string>();
        if (settings.MaxHealth <= 0) errors.Add($"{nameof(MaxHealth)} must be greater than zero");
        if (!float.IsFinite(settings.TransitionHealthRatio) || settings.TransitionHealthRatio <= 0f ||
            settings.TransitionHealthRatio >= 1f)
            errors.Add($"{nameof(TransitionHealthRatio)} must be between zero and one");
        Positive(errors, nameof(IntroSeconds), settings.IntroSeconds);
        ValidateTiming(errors, nameof(BossSettings.SweepTiming), settings.SweepTiming);
        ValidateTiming(errors, nameof(BossSettings.LeapSlamTiming), settings.LeapSlamTiming);
        ValidateTiming(errors, nameof(BossSettings.GroundWaveTiming), settings.GroundWaveTiming);
        ValidateTiming(errors, nameof(BossSettings.HammerThrowTiming), settings.HammerThrowTiming);
        Positive(errors, nameof(TransitionSeconds), settings.TransitionSeconds);
        NonNegative(errors, nameof(PhaseTwoArmoredSeconds), settings.PhaseTwoArmoredSeconds);
        Positive(errors, nameof(ExposedSeconds), settings.ExposedSeconds);
        Positive(errors, nameof(RetryReadyDelaySeconds), settings.RetryReadyDelaySeconds);
        if (settings.RetryReadyDelaySeconds > 3f)
            errors.Add($"{nameof(RetryReadyDelaySeconds)} must be at most three seconds");
        Positive(errors, nameof(MoveSpeed), settings.MoveSpeed);
        if (settings.ContactDamage != 1) errors.Add($"{nameof(ContactDamage)} must equal one in the first level");
        return errors;
    }

    private static void ValidateTiming(ICollection<string> errors, string name, BossAttackTiming timing)
    {
        NonNegative(errors, $"{name}.{nameof(BossAttackTiming.TelegraphSeconds)}", timing.TelegraphSeconds);
        Positive(errors, $"{name}.{nameof(BossAttackTiming.ActiveSeconds)}", timing.ActiveSeconds);
        NonNegative(errors, $"{name}.{nameof(BossAttackTiming.RecoverySeconds)}", timing.RecoverySeconds);
    }

    private static void Positive(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value <= 0f) errors.Add($"{name} must be greater than zero");
    }

    private static void NonNegative(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value < 0f) errors.Add($"{name} must not be negative");
    }
}
