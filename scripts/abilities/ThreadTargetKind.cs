namespace Knight.Abilities;

public enum ThreadTargetKind
{
    Anchor,
    LightEnemy,
    HeavyEnemy,
    Mechanism
}

public static class ThreadTargetKindExtensions
{
    public static bool IsLegacyInteraction(this ThreadTargetKind kind) => kind is
        ThreadTargetKind.LightEnemy or ThreadTargetKind.HeavyEnemy or ThreadTargetKind.Mechanism;
}
