using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static class CanyonEncounterCatalog
{
    public const string RustShellScene = "res://scenes/enemies/RustShellSquire.tscn";
    public const string LaborerScene = "res://scenes/enemies/BellBackLaborer.tscn";
    public const string MothScene = "res://scenes/enemies/GraveLampMoth.tscn";
    public const string SentryScene = "res://scenes/enemies/BellBoltSentry.tscn";
    public const string SkitterScene = "res://scenes/enemies/CanyonSkitter.tscn";
    public const string ScatterScene = "res://scenes/enemies/RivetScatterGunner.tscn";
    public const string BurstScene = "res://scenes/enemies/BeltBurstGunner.tscn";
    public const string MissileScene = "res://scenes/enemies/FurnaceMissileGunner.tscn";
    public const string LaserScene = "res://scenes/enemies/PrismLaserGunner.tscn";

    private static readonly IReadOnlyList<CanyonEncounterDefinition> AuthoredNormal =
        Array.AsReadOnly(new[]
        {
            Encounter("canyon.v2.encounter.low-ceiling-bridge", CanyonRouteNodeId.LowCeilingBridge,
                false, new Rect2(400, 460, 210, 180), new Rect2(400, 400, 340, 280),
                Enemy(RustShellScene, 560, 600, 0)),
            Encounter("canyon.v2.encounter.staggered-rift", CanyonRouteNodeId.StaggeredRift,
                false, new Rect2(1320, 500, 260, 180), new Rect2(1280, 480, 620, 240),
                Enemy(RustShellScene, 1740, 600, 0)),
            Encounter("canyon.v2.encounter.controlled-abyss", CanyonRouteNodeId.ControlledAbyss,
                false, new Rect2(2080, 3440, 360, 150), new Rect2(1960, 3240, 560, 350),
                Enemy(MothScene, 2280, 3420, 0),
                Enemy(SentryScene, 2450, 3531, 1, 2070, 3531),
                Enemy(ScatterScene, 2450, 3531, 2, 2070, 3531)),
            Encounter("canyon.v2.encounter.gear-pistons", CanyonRouteNodeId.GearPistons,
                false, new Rect2(2200, 3820, 280, 260), new Rect2(2080, 3600, 960, 720),
                Enemy(RustShellScene, 2300, 3960, 0),
                Enemy(LaborerScene, 2860, 3794, 1)),
            Encounter("canyon.v2.encounter.counterweight-lift", CanyonRouteNodeId.CounterweightLift,
                false, new Rect2(3900, 3380, 300, 210), new Rect2(3840, 2800, 1280, 800),
                Enemy(MothScene, 5000, 3000, 0),
                Enemy(RustShellScene, 4700, 3424, 1, 4630, 3424),
                Enemy(BurstScene, 4770, 3431, 1, 4630, 3431)),
            Encounter("canyon.v2.encounter.pendulum-gallery", CanyonRouteNodeId.PendulumGallery,
                false, new Rect2(4380, 2580, 400, 210), new Rect2(3840, 2000, 1280, 800),
                Enemy(LaborerScene, 4350, 2318, 0),
                Enemy(SkitterScene, 4300, 2361, 1, 4460, 2361),
                Enemy(LaserScene, 4300, 2331, 2, 4460, 2331)),
            Encounter("canyon.v2.encounter.split-level", CanyonRouteNodeId.SplitLevelEncounter,
                false, new Rect2(4380, 1760, 400, 230), new Rect2(3840, 1200, 1280, 800),
                true,
                Enemy(LaborerScene, 4180, 1438, 0),
                Enemy(MothScene, 4300, 1280, 1)),
            Encounter("canyon.v2.encounter.sealed-courtyard", CanyonRouteNodeId.SealedCourtyard,
                false, new Rect2(5550, 1030, 210, 190), new Rect2(5540, 920, 340, 330),
                Enemy(RustShellScene, 5630, 1160, 0),
                Enemy(LaborerScene, 5790, 1074, 1)),
            Encounter("canyon.v2.encounter.archive-side-cave", CanyonRouteNodeId.ArchiveSideCave,
                true, new Rect2(1420, 1480, 300, 210), new Rect2(1280, 1380, 600, 410),
                Enemy(RustShellScene, 1550, 1604, 0),
                Enemy(SkitterScene, 1380, 1641, 1, 1600, 1641)),
            Encounter("canyon.v2.encounter.frozen-upper", CanyonRouteNodeId.FrozenDetour,
                true, new Rect2(1280, 1940, 230, 240), new Rect2(1280, 1840, 230, 440),
                Enemy(MothScene, 1410, 2030, 0),
                Enemy(SentryScene, 1450, 2151, 1, 1315, 2151)),
            Encounter("canyon.v2.encounter.steam-valves", CanyonRouteNodeId.SteamValves,
                true, new Rect2(3160, 4080, 300, 230), new Rect2(3040, 3960, 800, 360),
                Enemy(LaborerScene, 3600, 4194, 0),
                Enemy(SentryScene, 3750, 4207, 1, 3250, 4207),
                Enemy(MissileScene, 3750, 4207, 2, 3250, 4207)),
            Encounter("canyon.v2.encounter.frozen-lower", CanyonRouteNodeId.FrozenDetour,
                true, new Rect2(1280, 3100, 240, 170), new Rect2(1280, 2800, 560, 560),
                Enemy(RustShellScene, 1460, 3244, 0),
                Enemy(MothScene, 1710, 3020, 1))
        });

    public static IReadOnlyList<CanyonEncounterDefinition> Normal => AuthoredNormal;
    public static CanyonRouteNodeId BossRouteNode => CanyonRouteNodeId.BossHall;

    public static IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (CanyonEncounterDefinition encounter in Normal)
        {
            if (string.IsNullOrWhiteSpace(encounter.StableId) || !ids.Add(encounter.StableId))
                errors.Add($"encounter stable ID must be nonblank and unique: {encounter.StableId}");
            CanyonRouteNodeDefinition route = CanyonWorldLayout.Nodes.SingleOrDefault(node =>
                node.Id == encounter.RouteNode);
            if (route == null)
            {
                errors.Add($"{encounter.StableId} references missing route {encounter.RouteNode}");
                continue;
            }
            if (route.Optional != encounter.Optional)
                errors.Add($"{encounter.StableId} optional identity must match {route.Id}");
            if (!Contains(route.WorldBounds, encounter.ActivationBounds) ||
                !Contains(route.WorldBounds, encounter.ArenaBounds))
                errors.Add($"{encounter.StableId} bounds must stay inside {route.Id}");
            if (encounter.Enemies == null || encounter.Enemies.Count is < 1 or > 3)
            {
                errors.Add($"{encounter.StableId} must author one to three finite enemy slots");
                continue;
            }
            int[] batches = encounter.Enemies.Select(enemy => enemy.ActivationBatch)
                .Distinct().OrderBy(batch => batch).ToArray();
            if (batches[0] != 0 || !batches.SequenceEqual(Enumerable.Range(0, batches.Length)))
                errors.Add($"{encounter.StableId} activation batches must be contiguous from zero");
            foreach (var batch in encounter.Enemies.GroupBy(enemy => enemy.ActivationBatch))
            {
                if (batch.Count() > 2)
                    errors.Add($"{encounter.StableId} batch{batch.Key} cannot mount more than two actors");
                if (batch.Count(enemy => IsNewRangedScene(enemy.ScenePath)) > 1)
                    errors.Add($"{encounter.StableId} batch{batch.Key} cannot combine two new ranged weapons");
                if (batch.Count(enemy => !IsNewRangedScene(enemy.ScenePath)) > 1)
                    errors.Add($"{encounter.StableId} must preserve separate batches for its old actors");
            }
            foreach (CanyonEnemyPlacement enemy in encounter.Enemies)
            {
                if (enemy.ScenePath is not RustShellScene and not LaborerScene and not MothScene
                    and not SentryScene and not SkitterScene && !IsNewRangedScene(enemy.ScenePath))
                    errors.Add($"{encounter.StableId} uses unapproved scene {enemy.ScenePath}");
                if (!Finite(enemy.WorldPosition) || !encounter.ArenaBounds.HasPoint(enemy.WorldPosition))
                    errors.Add($"{encounter.StableId} placement must stay in its arena");
                if (enemy.AlternateWorldPosition is Vector2 alternate)
                {
                    if (!Finite(alternate) || !encounter.ArenaBounds.HasPoint(alternate) ||
                        MathF.Abs(alternate.X - enemy.WorldPosition.X) < 64f ||
                        MathF.Abs(alternate.Y - enemy.WorldPosition.Y) > 1f)
                        errors.Add($"{encounter.StableId} alternate must be finite, in arena and separated");
                    Rect2? primaryBody = ArrivalBodyBounds(enemy.ScenePath, enemy.WorldPosition);
                    Rect2? alternateBody = ArrivalBodyBounds(enemy.ScenePath, alternate);
                    if (primaryBody == null || alternateBody == null)
                        errors.Add($"{encounter.StableId} arrival requires a supported packed body shape");
                    else if (ArrivalBodyOverlapsRest(primaryBody.Value) ||
                             ArrivalBodyOverlapsRest(alternateBody.Value))
                        errors.Add($"{encounter.StableId} arrival cannot overlap a rest area");
                }
            }
        }
        return Array.AsReadOnly(errors.ToArray());
    }

    private static CanyonEncounterDefinition Encounter(string stableId, CanyonRouteNodeId route,
        bool optional, Rect2 activation, Rect2 arena, params CanyonEnemyPlacement[] enemies) =>
        new(stableId, route, optional, activation, arena,
            Array.AsReadOnly(enemies));

    private static CanyonEncounterDefinition Encounter(string stableId, CanyonRouteNodeId route,
        bool optional, Rect2 activation, Rect2 arena, bool gatesProgression,
        params CanyonEnemyPlacement[] enemies) =>
        new(stableId, route, optional, activation, arena,
            Array.AsReadOnly(enemies), gatesProgression);

    private static CanyonEnemyPlacement Enemy(string scene, float x, float y, int batch) =>
        new(scene, new Vector2(x, y), batch);

    private static CanyonEnemyPlacement Enemy(string scene, float x, float y, int batch,
        float alternateX, float alternateY) =>
        new(scene, new Vector2(x, y), batch, new Vector2(alternateX, alternateY));

    private static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);

    private static bool IsNewRangedScene(string path) =>
        path is ScatterScene or BurstScene or MissileScene or LaserScene;

    internal static bool ArrivalBodyOverlapsRest(Rect2 body) =>
        CanyonWorldLayout.EncounterRestBounds.Any(rest => rest.Intersects(body));

    internal static Rect2? ArrivalBodyBounds(string scenePath, Vector2 site)
    {
        PackedScene scene = GD.Load<PackedScene>(scenePath);
        CharacterBody2D actor = scene?.Instantiate() as CharacterBody2D;
        if (actor == null) return null;
        try
        {
            CollisionShape2D collider = actor.GetNodeOrNull<CollisionShape2D>("BodyCollider");
            Vector2 extent = collider?.Shape switch
            {
                CapsuleShape2D capsule => new Vector2(capsule.Radius, capsule.Height * 0.5f),
                RectangleShape2D rectangle => rectangle.Size * 0.5f,
                _ => Vector2.Zero
            };
            if (extent.X <= 0f || extent.Y <= 0f) return null;
            Vector2 center = site + collider.Position;
            return new Rect2(center - extent, extent * 2f);
        }
        finally { actor.Free(); }
    }

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X && inner.Position.Y >= outer.Position.Y &&
        inner.End.X <= outer.End.X && inner.End.Y <= outer.End.Y;
}
