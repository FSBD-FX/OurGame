using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public sealed record CanyonMicroSceneDefinition(
    string Id,
    PrologueSectorId SectorId,
    Rect2 LocalBounds,
    bool OptionalSubarea,
    CanyonChallengeKind? ChallengeKind = null,
    CanyonRouteNodeId RouteNodeId = default);
