using System.Collections.Generic;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public sealed record CanyonEncounterDefinition(
    string StableId, CanyonRouteNodeId RouteNode, bool Optional,
    Rect2 ActivationBounds, Rect2 ArenaBounds,
    IReadOnlyList<CanyonEnemyPlacement> Enemies,
    bool GatesProgression = false);

public sealed record CanyonEnemyPlacement(
    string ScenePath, Vector2 WorldPosition, int ActivationBatch,
    Vector2? AlternateWorldPosition = null);
