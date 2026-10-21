using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Residents;

public enum ResidentFamily { Native, Scrap, Precision, Bone, Ghost }
public enum ResidentArchetype { Wanderer, Charger, Flyer, Diver, Turret, Heavy }
public enum ResidentSceneState { Unimplemented, ExplicitPlaceholder, Implemented }

// Immutable runtime policy is not a formal scene/placement certificate.
public enum ResidentBoneWeakness { None, RearOrExposed, RecoveryOrThreadStunned, RecoveryOrThreadExposed }
public sealed record ResidentCombatProfile(ResidentBoneWeakness BoneWeakness = ResidentBoneWeakness.None,
    float GhostHiddenSeconds = 0, float GhostManifestSeconds = 0);

// ProbeScenePath is only a body-shape/behavior-development reference. It must
// never be mounted as a supposedly finished Bone/Ghost/etc. family scene.
public sealed record ResidentSubtypeDefinition(string Id, string DisplayName,
    ResidentFamily Family, ResidentArchetype Archetype, string ProbeScenePath,
    ResidentSceneState SceneState = ResidentSceneState.Unimplemented, string ScenePath = null,
    ResidentCarrierKind Carrier = ResidentCarrierKind.Unspecified);

// A candidate is immutable, but it is NOT a native placement certificate.
// Missing placement is null, never a default origin/empty-rectangle "site".
public sealed class ResidentPlacementCandidate
{
    public Vector2 WorldSpawn { get; }
    public Rect2 Territory { get; }
    public IReadOnlyList<Vector2> PatrolPoints { get; }

    public ResidentPlacementCandidate(Vector2 worldSpawn, Rect2 territory,
        IEnumerable<Vector2> patrolPoints)
    {
        WorldSpawn = worldSpawn;
        Territory = territory;
        PatrolPoints = Array.AsReadOnly((patrolPoints ?? throw new ArgumentNullException(
            nameof(patrolPoints))).ToArray());
    }
}

public sealed record ResidentPopulationDefinition(string StableId, string SubtypeId,
    CanyonRouteNodeId RouteNode, PrologueSectorId Sector, ResidentFamily Family,
    ResidentArchetype Archetype, ResidentPlacementCandidate Placement = null,
    ResidentCombatProfile CombatProfile = null);
