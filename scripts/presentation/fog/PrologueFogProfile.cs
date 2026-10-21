using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

/// <summary>
/// Immutable candidate settings, not visually accepted tuning. Region IDs describe
/// the sampled world point; an actor entering BossHall must not relabel its viewport.
/// </summary>
public readonly record struct PrologueFogProfile(float ClearRadius, float OuterRadius,
    float MaxOpacity, float BeamRange, float BeamHalfAngleDegrees)
{
    public static PrologueFogProfile Exploration { get; } = new(180f, 620f, .55f, 620f, 22.5f);
    public static PrologueFogProfile Entrance { get; } = new(180f, 620f, .28f, 620f, 22.5f);
    public static PrologueFogProfile DeepAbyss { get; } = new(180f, 620f, .72f, 620f, 22.5f);
    public static PrologueFogProfile RestArea { get; } = new(180f, 620f, .10f, 620f, 22.5f);
    public static PrologueFogProfile BossHall { get; } = new(180f, 620f, .18f, 620f, 22.5f);

    public static PrologueFogProfile ForRegion(CanyonRouteNodeId region) => region switch
    {
        CanyonRouteNodeId.LowCeilingBridge => Entrance,
        CanyonRouteNodeId.ControlledAbyss => DeepAbyss,
        CanyonRouteNodeId.BasinCamp or CanyonRouteNodeId.SafeExit => RestArea,
        CanyonRouteNodeId.BossHall => BossHall,
        _ => Exploration
    };
}
