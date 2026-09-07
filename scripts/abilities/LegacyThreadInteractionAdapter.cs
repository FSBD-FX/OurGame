using System;
using Godot;

namespace Knight.Abilities;

public partial class LegacyThreadInteractionAdapter : Node
{
    [Export] public NodePath TargetPath { get; set; } = new("..");
    [Export] public float PlayerPullSpeed { get; set; } = 620f;
    [Export] public float EnemyPullSpeed { get; set; } = 540f;

    public int ApplicationCount { get; private set; }

    public bool TryApply(Node2D player, Vector2 origin)
    {
        if (!GodotObject.IsInstanceValid(player) || !FinitePositive(PlayerPullSpeed) ||
            !FinitePositive(EnemyPullSpeed)) return false;
        ThreadTarget2D target = GetNodeOrNull<ThreadTarget2D>(TargetPath);
        if (target == null || !target.Kind.IsLegacyInteraction()) return false;

        bool accepted = target.TryApplyLegacyInteraction(player, origin, new ThreadAbilitySettings(
            PlayerPullSpeed: PlayerPullSpeed,
            EnemyPullSpeed: EnemyPullSpeed));
        if (!accepted) return false;
        ApplicationCount++;
        return true;
    }

    private static bool FinitePositive(float value) => float.IsFinite(value) && value > 0f;
}
