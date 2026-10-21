using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;

namespace Knight.Prologue.Seamless;

public static class CanyonWorldLayout
{
    public const int CurrentLayoutVersion = 2;
    public const string UpperBasinShortcutId = "canyon.v2.upper-basin-shortcut";
    public const string CourtyardSealMechanismId = "canyon.v2.courtyard-seal-open";

    private static readonly IReadOnlyList<CanyonCameraFocusRegion> AuthoredCameraFocusRegions =
        Array.AsReadOnly(new[]
        {
            new CanyonCameraFocusRegion("canyon.v2.camera.controlled-abyss",
                new Rect2(1920f, 1176f, 640f, 2424f), new Vector2(0f, 80f),
                new Vector2(0f, 260f), .32f),
            new CanyonCameraFocusRegion("canyon.v2.camera.entrance",
                new Rect2(0f, 0f, 1280f, 720f), new Vector2(80f, -20f),
                Vector2.Zero, .28f),
            new CanyonCameraFocusRegion("canyon.v2.camera.descent",
                new Rect2(1280f, 0f, 1280f, 3600f), new Vector2(0f, 40f),
                Vector2.Zero, .32f),
            new CanyonCameraFocusRegion("canyon.v2.camera.basin",
                new Rect2(1280f, 3600f, 3840f, 720f), new Vector2(100f, -40f),
                Vector2.Zero, .30f),
            new CanyonCameraFocusRegion("canyon.v2.camera.ascent",
                new Rect2(3840f, 720f, 1280f, 2880f), new Vector2(80f, -140f),
                Vector2.Zero, .30f),
            new CanyonCameraFocusRegion("canyon.v2.camera.temple",
                new Rect2(5120f, 720f, 2560f, 720f), new Vector2(140f, -20f),
                Vector2.Zero, .28f)
        });

    private static readonly IReadOnlyList<Rect2> AuthoredAbyssBrakeWallSegments =
        Array.AsReadOnly(new[]
        {
            new Rect2(2490f, 1296f, 36f, 304f),
            new Rect2(2490f, 1600f, 36f, 400f),
            new Rect2(2490f, 2000f, 36f, 800f),
            new Rect2(2490f, 2800f, 36f, 800f)
        });

    private static readonly IReadOnlyList<Rect2> AuthoredAbyssSideExits =
        Array.AsReadOnly(new[]
        {
            new Rect2(2350f, 1540f, 200f, 40f),
            new Rect2(1920f, 2220f, 200f, 40f),
            new Rect2(2360f, 3080f, 190f, 40f)
        });

    private static readonly IReadOnlyDictionary<PrologueSectorId, Rect2> AuthoredSectorBounds =
        new ReadOnlyDictionary<PrologueSectorId, Rect2>(new Dictionary<PrologueSectorId, Rect2>
        {
            [PrologueSectorId.CanyonEntrance] = new(0f, 0f, 1280f, 720f),
            [PrologueSectorId.CanyonDescent] = new(1280f, 0f, 1280f, 3600f),
            [PrologueSectorId.CanyonBasin] = new(1280f, 3600f, 3840f, 720f),
            [PrologueSectorId.CanyonAscent] = new(3840f, 720f, 1280f, 2880f),
            [PrologueSectorId.LostTemple] = new(5120f, 720f, 2560f, 720f)
        });

    private static readonly IReadOnlyList<CanyonRouteNodeDefinition> AuthoredNodes =
        Array.AsReadOnly(new[]
        {
            Node(CanyonRouteNodeId.LowCeilingBridge, PrologueSectorId.CanyonEntrance,
                new Rect2(0f, 0f, 1280f, 720f), CanyonChallengeKind.LowCeilingBridge),
            Node(CanyonRouteNodeId.StaggeredRift, PrologueSectorId.CanyonDescent,
                new Rect2(1280f, 0f, 1280f, 1176f), CanyonChallengeKind.StaggeredRift),
            Node(CanyonRouteNodeId.ControlledAbyss, PrologueSectorId.CanyonDescent,
                new Rect2(1920f, 1176f, 640f, 2424f), CanyonChallengeKind.ControlledAbyss),
            Node(CanyonRouteNodeId.ArchiveSideCave, PrologueSectorId.CanyonDescent,
                new Rect2(1280f, 1176f, 640f, 624f), CanyonChallengeKind.ArchiveSideCave,
                optional: true, discoveryId: "canyon.v2.discovery.archive-side-cave"),
            Node(CanyonRouteNodeId.FrozenDetour, PrologueSectorId.CanyonDescent,
                new Rect2(1280f, 1800f, 640f, 1560f), CanyonChallengeKind.FrozenDetour,
                optional: true, discoveryId: "canyon.v2.discovery.frozen-detour"),
            Node(CanyonRouteNodeId.BasinCamp, PrologueSectorId.CanyonBasin,
                new Rect2(1280f, 3600f, 800f, 720f), null, restArea: true),
            Node(CanyonRouteNodeId.GearPistons, PrologueSectorId.CanyonBasin,
                new Rect2(2080f, 3600f, 960f, 720f), CanyonChallengeKind.GearPistons),
            Node(CanyonRouteNodeId.SteamValves, PrologueSectorId.CanyonBasin,
                new Rect2(3040f, 3960f, 1160f, 360f), CanyonChallengeKind.SteamValves,
                optional: true),
            Node(CanyonRouteNodeId.CounterweightLift, PrologueSectorId.CanyonAscent,
                new Rect2(3840f, 2800f, 1280f, 800f), CanyonChallengeKind.CounterweightLift),
            Node(CanyonRouteNodeId.PendulumGallery, PrologueSectorId.CanyonAscent,
                new Rect2(3840f, 2000f, 1280f, 800f), CanyonChallengeKind.PendulumGallery),
            Node(CanyonRouteNodeId.SplitLevelEncounter, PrologueSectorId.CanyonAscent,
                new Rect2(3840f, 1200f, 1280f, 800f), CanyonChallengeKind.SplitLevelEncounter),
            Node(CanyonRouteNodeId.SealedCourtyard, PrologueSectorId.LostTemple,
                new Rect2(5120f, 720f, 960f, 720f), CanyonChallengeKind.SealedCourtyard),
            Node(CanyonRouteNodeId.BossHall, PrologueSectorId.LostTemple,
                new Rect2(6080f, 720f, 1200f, 720f), CanyonChallengeKind.BossHall),
            Node(CanyonRouteNodeId.SafeExit, PrologueSectorId.LostTemple,
                new Rect2(7280f, 720f, 400f, 720f), null, restArea: true)
        });

    private static readonly IReadOnlyList<CanyonRouteEdge> AuthoredEdges =
        Array.AsReadOnly(new[]
        {
            Edge(CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift,
                "canyon.v2.route.bridge-to-rift"),
            Edge(CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.ControlledAbyss,
                "canyon.v2.route.rift-to-abyss"),
            new(CanyonRouteNodeId.ControlledAbyss, CanyonRouteNodeId.BasinCamp, true,
                CanyonRouteGate.None, TraversalSegmentId: "canyon.v2.route.abyss-to-basin-camp",
                ReverseTraversalSegmentId: "canyon.v2.route.basin-camp-to-abyss"),
            Edge(CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.ArchiveSideCave,
                "canyon.v2.route.rift-to-archive"),
            Edge(CanyonRouteNodeId.ArchiveSideCave, CanyonRouteNodeId.FrozenDetour,
                "canyon.v2.route.archive-to-frozen-detour"),
            Edge(CanyonRouteNodeId.FrozenDetour, CanyonRouteNodeId.BasinCamp,
                "canyon.v2.route.frozen-detour-to-basin-camp"),
            Edge(CanyonRouteNodeId.BasinCamp, CanyonRouteNodeId.GearPistons,
                "canyon.v2.route.basin-camp-to-gears"),
            Edge(CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.CounterweightLift,
                "canyon.v2.route.gears-to-lift-high"),
            Edge(CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.SteamValves,
                "canyon.v2.route.gears-to-steam-valves"),
            Edge(CanyonRouteNodeId.SteamValves, CanyonRouteNodeId.CounterweightLift,
                "canyon.v2.route.steam-valves-to-lift"),
            Edge(CanyonRouteNodeId.CounterweightLift, CanyonRouteNodeId.PendulumGallery,
                "canyon.v2.route.lift-to-pendulums"),
            Edge(CanyonRouteNodeId.PendulumGallery, CanyonRouteNodeId.SplitLevelEncounter,
                "canyon.v2.route.pendulums-to-split-level"),
            Edge(CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.SealedCourtyard,
                "canyon.v2.route.split-level-to-courtyard"),
            new(CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, false,
                CanyonRouteGate.MechanismActivated, TraversalSegmentId:
                    "canyon.v2.route.courtyard-to-boss-hall",
                RequiredMechanismId: CourtyardSealMechanismId),
            new(CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit, false,
                CanyonRouteGate.BossDefeated, TraversalSegmentId:
                    "canyon.v2.route.boss-hall-to-safe-exit"),
            new(CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.BasinCamp, true,
                CanyonRouteGate.ShortcutOpen, StableShortcutId: UpperBasinShortcutId,
                TraversalSegmentId: "canyon.v2.route.upper-to-basin-shortcut",
                ReverseTraversalSegmentId: "canyon.v2.route.basin-to-upper-shortcut")
        });

    private static readonly IReadOnlyDictionary<string, Vector2> AuthoredCheckpointPositions =
        new ReadOnlyDictionary<string, Vector2>(new Dictionary<string, Vector2>(StringComparer.Ordinal)
        {
            ["canyon.v2.start"] = new(160f, 600f),
            ["canyon.v2.basin-camp"] = new(1680f, 3960f),
            ["canyon.v2.temple-forecourt"] = new(5920f, 1080f)
        });

    public static Rect2 DesignBounds { get; } = new(0f, 0f, 7680f, 4320f);
    public static IReadOnlyList<CanyonCameraFocusRegion> CameraFocusRegions =>
        AuthoredCameraFocusRegions;
    public static Rect2 FirstAbyssBrakeFace { get; } = new(2490f, 1400f, 36f, 160f);
    public static IReadOnlyList<Rect2> AbyssBrakeWallSegments =>
        AuthoredAbyssBrakeWallSegments;
    public static IReadOnlyList<Rect2> AbyssSideExits => AuthoredAbyssSideExits;
    public static Rect2 AbyssSafeBottom { get; } = new(1920f, 3560f, 640f, 40f);
    public static IReadOnlyList<Rect2> SteamRejoinSteps { get; } = Array.AsReadOnly(new[]
    {
        new Rect2(4200f, 4156f, 150f, 40f),
        new Rect2(4310f, 4076f, 150f, 40f),
        new Rect2(4420f, 3996f, 150f, 40f),
        new Rect2(4530f, 3916f, 150f, 40f),
        new Rect2(4640f, 3836f, 150f, 40f),
        new Rect2(4750f, 3756f, 150f, 40f),
        new Rect2(4860f, 3676f, 150f, 40f),
        new Rect2(4970f, 3600f, 150f, 40f)
    });
    public static Rect2 SteamRejoinTop { get; } = new(4940f, 3600f, 180f, 40f);
    public static IReadOnlyList<Rect2> EncounterRestBounds { get; } =
        Array.AsReadOnly(new[]
        {
            new Rect2(1280f, 3600f, 800f, 720f),
            new Rect2(5880f, 920f, 200f, 240f),
            new Rect2(7280f, 720f, 400f, 720f)
        });
    public static IReadOnlyDictionary<PrologueSectorId, Rect2> SectorBounds => AuthoredSectorBounds;
    public static IReadOnlyList<CanyonRouteNodeDefinition> Nodes => AuthoredNodes;
    public static IReadOnlyList<CanyonRouteEdge> Edges => AuthoredEdges;
    public static IReadOnlyDictionary<string, Vector2> CheckpointWorldPositions =>
        AuthoredCheckpointPositions;
    public static IReadOnlyList<string> StableCheckpointIds { get; } = Array.AsReadOnly(new[]
    {
        "canyon.v2.start",
        "canyon.v2.basin-camp",
        "canyon.v2.temple-forecourt"
    });
    public static IReadOnlyList<string> StableShortcutIds { get; } =
        Array.AsReadOnly(new[] { UpperBasinShortcutId });
    public static IReadOnlyList<string> StableMechanismIds { get; } =
        Array.AsReadOnly(new[] { CourtyardSealMechanismId });
    // Graph gate IDs are not the complete persistence registry. These are the
    // additional authored runtime controls/prerequisite, never ordinary kills.
    public static IReadOnlyList<string> PersistableShortcutIds { get; } =
        Array.AsReadOnly(new[] { UpperBasinShortcutId, "canyon.v2.temple-bell-gate" });
    public static IReadOnlyList<string> PersistableMechanismIds { get; } =
        Array.AsReadOnly(new[] { CourtyardSealMechanismId, "canyon.v2.steam-valve-lower",
            "canyon.v2.courtyard-crank", "canyon.v2.encounter.split-level" });
    public static IReadOnlyList<string> PersistableRewardIds { get; } =
        Array.AsReadOnly(new[] { "canyon.v2.reward.archive-supply", "canyon.v2.reward.temple-supply" });
    public static IReadOnlyList<string> LegacyDiscoveryIds { get; } =
        Array.AsReadOnly(new[] { "hidden_alcove", "supply_cave", "high_ruin" });
    public static IReadOnlyList<string> StableDiscoveryIds { get; } = Array.AsReadOnly(new[]
    {
        "canyon.v2.discovery.archive-side-cave",
        "canyon.v2.discovery.frozen-detour"
    });

    public static bool TryResolveRouteNode(Vector2 worldPosition, out CanyonRouteNodeId id)
    {
        id = default;
        if (!float.IsFinite(worldPosition.X) || !float.IsFinite(worldPosition.Y)) return false;
        foreach (CanyonRouteNodeDefinition node in AuthoredNodes)
            if (node.WorldBounds.HasPoint(worldPosition))
            {
                id = node.Id;
                return true;
            }
        // These saved upper galleries sit outside the original GearPistons
        // rectangle. Map only their complete-player-width standing/landing
        // corridors, not the empty basin column above or below them.
        if (UpperGalleryCenterCorridorContains(new Rect2(3040f, 3756f, 240f, 40f), worldPosition) ||
            UpperGalleryCenterCorridorContains(new Rect2(3280f, 3676f, 240f, 40f), worldPosition) ||
            UpperGalleryCenterCorridorContains(new Rect2(3520f, 3600f, 320f, 40f), worldPosition))
        {
            id = CanyonRouteNodeId.GearPistons;
            return true;
        }
        // Only the union of the existing supports' player-center corridors is fallback.
        foreach (Rect2 step in SteamRejoinSteps)
            if (SteamRejoinCorridorContains(step, worldPosition))
            {
                id = CanyonRouteNodeId.SteamValves;
                return true;
            }
        if (!SteamRejoinCorridorContains(SteamRejoinTop, worldPosition)) return false;
        id = CanyonRouteNodeId.SteamValves;
        return true;
    }

    private static bool SteamRejoinCorridorContains(Rect2 support, Vector2 worldPosition) =>
        new Rect2(support.Position.X - 20f, support.Position.Y - 160f,
            support.Size.X + 40f, 164f).HasPoint(worldPosition);

    private static bool UpperGalleryCenterCorridorContains(Rect2 support, Vector2 worldPosition) =>
        worldPosition.X >= support.Position.X + 20f && worldPosition.X <= support.End.X - 20f &&
        worldPosition.Y >= support.Position.Y - 72f && worldPosition.Y <= support.Position.Y - 32f;

    public static IReadOnlyList<string> Validate() => CanyonRouteGraph.Validate(
        DesignBounds, SectorBounds, Nodes, Edges, CheckpointWorldPositions,
        StableCheckpointIds, StableShortcutIds, StableMechanismIds, StableDiscoveryIds);

    public static bool TryResolveCheckpoint(string id, out PrologueSectorId sector, out Vector2 position)
    {
        sector = PrologueSectorId.CanyonEntrance;
        position = default;
        if (id == null || !AuthoredCheckpointPositions.TryGetValue(id, out position)) return false;
        sector = id switch
        {
            "canyon.v2.basin-camp" => PrologueSectorId.CanyonBasin,
            "canyon.v2.temple-forecourt" => PrologueSectorId.LostTemple,
            _ => PrologueSectorId.CanyonEntrance
        };
        return true;
    }

    private static CanyonRouteNodeDefinition Node(CanyonRouteNodeId id, PrologueSectorId sectorId,
        Rect2 bounds, CanyonChallengeKind? challenge, bool optional = false,
        bool restArea = false, string discoveryId = "") =>
        new(id, sectorId, bounds, challenge, optional, restArea, discoveryId);

    private static CanyonRouteEdge Edge(CanyonRouteNodeId from, CanyonRouteNodeId to,
        string traversalSegmentId) => new(from, to, false,
            TraversalSegmentId: traversalSegmentId);
}
