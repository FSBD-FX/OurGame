using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Tools.Export;

// Documentation projection only. Never mounts actors, drives input, changes
// cached resources or replaces the playable map with its flattened preview.
public static class CurrentPrologueMapSnapshot
{
    public static string BuildJson(PrologueLevelController level)
    {
        var definitions = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
        using var catalog = MinimapTerrainCatalogBuilder.Build(definitions);
        var saved = level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
        Require(definitions.Length == 5 && catalog.SolidCount == saved.SolidCount && catalog.Revision == saved.Revision &&
            catalog.SerializedValues.AsSpan().SequenceEqual(saved.SerializedValues), "saved parent/catalog mismatch");
        var enemies = new List<object>();
        var objects = new List<object>();
        var fragments = new List<object>();
        var bosses = new List<object>();
        var surfaces = new List<object>();
        var configuredHazardVolumes = new List<object>();
        var configuredMovingGears = new List<object>();
        var configuredArcGears = new List<object>();
        var sources = new SortedSet<string>(StringComparer.Ordinal)
        {
            "res://scenes/prologue/PrologueLevel.tscn", ResidentAuthoredLayout.ResourcePath,
            "res://scripts/prologue/seamless/CanyonWorldLayout.cs",
            "res://scripts/prologue/collections/QuestFragmentCatalog.cs",
            "res://tools/build/prologue/CanyonEncounterCatalog.cs",
            "res://tools/build/prologue/CanyonResidentCatalog.cs",
            "res://tools/build/prologue/MinimapTerrainCatalogBuilder.cs",
            "res://docs/production/2026-10-16-prologue-population-final-plan-v002.md"
        };
        foreach (var encounter in CanyonEncounterCatalog.Normal)
        {
            var region = CanyonWorldLayout.Nodes.Single(n => n.Id == encounter.RouteNode);
            for (int slot = 0; slot < encounter.Enemies.Count; slot++)
            {
                var placement = encounter.Enemies[slot];
                enemies.Add(new { id = encounter.StableId + ".slot-" + slot, cohort = "encounter",
                    name = Path.GetFileNameWithoutExtension(placement.ScenePath), family = "Encounter", archetype = "Encounter",
                    sector = region.SectorId.ToString(), region = region.Id.ToString(), optional = encounter.Optional,
                    point = Point(placement.WorldPosition), scene = placement.ScenePath,
                    batch = placement.ActivationBatch, alternatePoint = placement.AlternateWorldPosition is { } alt ? Point(alt) : null,
                    territory = (float[])null, patrol = Array.Empty<float[]>(), arena = Rectangle(encounter.ArenaBounds),
                    activation = Rectangle(encounter.ActivationBounds), status = "configured-slot-not-live-count" });
            }
        }
        foreach (var site in ResidentAuthoredLayout.Load())
        {
            var d = site.Definition;
            var type = CanyonResidentCatalog.Subtypes.Single(s => s.Id == d.SubtypeId);
            enemies.Add(new { id = d.StableId, cohort = "resident", name = type.DisplayName,
                family = d.Family.ToString(), archetype = d.Archetype.ToString(),
                sector = d.Sector.ToString(), region = d.RouteNode.ToString(),
                optional = CanyonWorldLayout.Nodes.Single(n => n.Id == d.RouteNode).Optional,
                point = Point(d.Placement.WorldSpawn), scene = type.ScenePath, batch = -1,
                alternatePoint = (float[])null, territory = Rectangle(d.Placement.Territory),
                patrol = d.Placement.PatrolPoints.Select(Point).ToArray(), occupancy = Rectangle(site.Occupancy),
                status = "authored-resident-not-live-count" });
        }
        foreach (var definition in definitions.OrderBy(d => d.Id))
        {
            sources.Add(definition.Scene.ResourcePath);
            var sector = definition.Scene.Instantiate<PrologueSector>();
            try { Walk(sector, new Transform2D(0, definition.WorldOffset)); }
            finally { sector.Free(); }
            void Walk(Node node, Transform2D parent)
            {
                Require(node == sector || node is not CanvasLayer, "unexpected canvas inheritance break");
                Transform2D t = node != sector && node is Node2D n ? parent * n.Transform : parent;
                if (node is Node2D current) Require(!current.TopLevel, "top-level snapshot transform unsupported");
                string path = sector.GetPathTo(node).ToString();
                if (node is QuestFragment fragment)
                {
                    var d = QuestFragmentCatalog.Definitions.Single(f => f.StableId == fragment.StableId);
                    fragments.Add(new { id = d.StableId, point = Point(t.Origin), sector = definition.Id.ToString(),
                        optional = d.Optional, path });
                }
                if (node is BossArenaController arena)
                    bosses.Add(new { id = definition.Id + "/" + path, point = Point(t * arena.BossSpawnPosition),
                        scene = arena.BossScene?.ResourcePath, status = "configured-spawn-not-live-birth" });
                if (node is GrappleSurface2D surface)
                    surfaces.Add(new { id = definition.Id + "/" + path, point = Point(t * surface.AnchorOffset),
                        material = surface.Material.ToString(), status = "material-only-not-live-eligibility" });
                if (node is CanyonIncinerator incinerator)
                {
                    // Detached snapshots have not run _Ready: DangerBounds is not initialized.
                    // Read the saved borrowed shape and full transform instead, without mounting
                    // the area or creating an actor/damage request to discover its geometry.
                    var collision = incinerator.GetNode<CollisionShape2D>("Collision");
                    var rectangle = collision.Shape as RectangleShape2D;
                    Require(rectangle != null && rectangle.Size.IsFinite() &&
                        rectangle.Size.X > 0f && rectangle.Size.Y > 0f &&
                        rectangle.Size.IsEqualApprox(incinerator.Size) &&
                        collision.Transform.IsEqualApprox(Transform2D.Identity) &&
                        incinerator.GetChildren().OfType<CollisionShape2D>().Count() == 1 &&
                        !incinerator.GetChildren().OfType<CollisionPolygon2D>().Any() &&
                        float.IsFinite(incinerator.PulseSeconds) && incinerator.PulseSeconds >= .1f &&
                        incinerator.PulseSeconds <= 5f, "unsupported configured incinerator geometry or cadence");
                    Transform2D shapeTransform = t * collision.Transform;
                    Rect2 shapeBounds = new(-rectangle.Size * .5f, rectangle.Size);
                    string id = definition.Id + "/" + path;
                    configuredHazardVolumes.Add(new
                    {
                        id, objectId = id, type = nameof(CanyonIncinerator), sector = definition.Id.ToString(), path,
                        corners = new[] { Point(shapeTransform * shapeBounds.Position),
                            Point(shapeTransform * new Vector2(shapeBounds.End.X, shapeBounds.Position.Y)),
                            Point(shapeTransform * shapeBounds.End),
                            Point(shapeTransform * new Vector2(shapeBounds.Position.X, shapeBounds.End.Y)) },
                        pulseSeconds = incinerator.PulseSeconds, damagePolicy = "max-health-contact-attempt",
                        unblockable = true, respectsHealthInvulnerability = true,
                        status = "configured-volume-not-live-activation"
                    });
                    sources.Add("res://scripts/mechanisms/CanyonIncinerator.cs");
                }
                if(node is CanyonMovingGear moving)
                {
                    // Off-tree authoring snapshot: do not use Ready-only
                    // CurrentMotion/DangerRadius/MotionWorldBounds here.
                    var collision=moving.GetNode<CollisionShape2D>("Collision");
                    Require(collision.Shape is CircleShape2D circle && !collision.Disabled &&
                        Mathf.IsEqualApprox(circle.Radius,moving.Radius) &&
                        moving.GetChildCount()==1 && collision.Transform.IsEqualApprox(Transform2D.Identity) &&
                        t.X.DistanceSquaredTo(Vector2.Right)<.00000001f &&
                        t.Y.DistanceSquaredTo(Vector2.Down)<.00000001f &&
                        float.IsFinite(moving.Radius) && moving.Radius is >=12 and <=96 &&
                        moving.Damage is >=1 and <=100 &&
                        float.IsFinite(moving.ContactIntervalSeconds) && moving.ContactIntervalSeconds is >=.1f and <=5f,
                        "unsupported configured moving gear circle, basis or damage cadence");
                    _=new MovingGearMotionModel(moving.TravelOffset,moving.PeriodSeconds);
                    Vector2 start=t.Origin,end=t*moving.TravelOffset,extent=Vector2.One*moving.Radius;
                    var envelope=new Rect2(start.Min(end)-extent,(end-start).Abs()+extent*2);
                    string id=definition.Id+"/"+path;
                    configuredMovingGears.Add(new
                    {
                        id,objectId=id,type=nameof(CanyonMovingGear),sector=definition.Id.ToString(),path,
                        start=Point(start),end=Point(end),radius=moving.Radius,periodSeconds=moving.PeriodSeconds,
                        damage=moving.Damage,contactIntervalSeconds=moving.ContactIntervalSeconds,
                        envelope=Rectangle(envelope),envelopePolicy="conservative-aabb-not-damage-shape",
                        status="configured-rail-not-runtime-route-proof"
                    });
                    sources.Add("res://scripts/mechanisms/CanyonMovingGear.cs");
                    sources.Add("res://scripts/mechanisms/MovingGearMotionModel.cs");
                }
                if(node is CanyonArcGear arc)
                {
                    // Detached saved configuration: no Ready/native clock or cached runtime bounds.
                    var collision=arc.GetNode<CollisionShape2D>("Collision");
                    Require(collision.Shape is CircleShape2D circle && !collision.Disabled &&
                        Mathf.IsEqualApprox(circle.Radius,arc.Radius) && arc.GetChildCount()==1 &&
                        collision.Transform.IsEqualApprox(Transform2D.Identity) &&
                        t.X.DistanceSquaredTo(Vector2.Right)<.00000001f &&
                        t.Y.DistanceSquaredTo(Vector2.Down)<.00000001f &&
                        float.IsFinite(arc.Radius) && arc.Radius is >=12 and <=96 && arc.Damage is >=1 and <=100 &&
                        float.IsFinite(arc.ContactIntervalSeconds) && arc.ContactIntervalSeconds is >=.1f and <=5f,
                        "unsupported configured arc circle, basis or damage cadence");
                    _=new ArcGearMotionModel(arc.PivotOffset,arc.SwingRadians,arc.PeriodSeconds);
                    Vector2 start=t.Origin,pivot=start+arc.PivotOffset,ray=-arc.PivotOffset;
                    Vector2 end=pivot+ray.Rotated(arc.SwingRadians);
                    Rect2 envelope=CircularArcSweep.Bounds(pivot,ray,Math.Min(0,arc.SwingRadians),Math.Max(0,arc.SwingRadians),arc.Radius);
                    string id=definition.Id+"/"+path;
                    configuredArcGears.Add(new
                    {
                        id,objectId=id,type=nameof(CanyonArcGear),sector=definition.Id.ToString(),path,
                        start=Point(start),pivot=Point(pivot),end=Point(end),swingRadians=arc.SwingRadians,
                        radius=arc.Radius,periodSeconds=arc.PeriodSeconds,damage=arc.Damage,
                        contactIntervalSeconds=arc.ContactIntervalSeconds,envelope=Rectangle(envelope),
                        envelopePolicy="conservative-aabb-not-damage-shape",status="configured-arc-not-runtime-route-proof"
                    });
                    sources.Add("res://scripts/mechanisms/CanyonArcGear.cs");
                    sources.Add("res://scripts/mechanisms/ArcGearMotionModel.cs");
                    sources.Add("res://scripts/mechanisms/CircularArcSweep.cs");
                }
                var ids = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string key in new[] { "MechanismId", "RequiredMechanismId", "RequiredEncounterId", "DiscoveryId" })
                {
                    var property = node.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
                    if (property?.PropertyType != typeof(string) || !property.CanRead) continue;
                    string value = (string)property.GetValue(node);
                    if (!string.IsNullOrEmpty(value)) ids.Add(key, value);
                }
                bool device = node is Node2D && (node.GetType().Namespace?.StartsWith("Knight.Mechanisms", StringComparison.Ordinal) == true ||
                    node.GetType().Namespace == "Knight.Prologue.Traversal" || ids.Count != 0 ||
                    path.StartsWith("Gameplay/Doors/", StringComparison.Ordinal) && node.GetParent()?.Name == "Doors");
                if (device)
                    objects.Add(new { id = definition.Id + "/" + path, point = Point(t.Origin),
                        type = node.GetType().Name, sector = definition.Id.ToString(), path, ids,
                        status = "saved-anchor-not-dynamic-danger-envelope" });
                foreach (Node child in node.GetChildren()) Walk(child, t);
            }
        }
        var checkpoints = MinimapTerrainCatalogBuilder.Checkpoints(definitions);
        Require(enemies.Count == 76 && fragments.Count == 9 && bosses.Count == 1 &&
            configuredHazardVolumes.Count == 1 && configuredMovingGears.Count==1 && configuredArcGears.Count==1, "incomplete current map projection");
        var solids = Enumerable.Range(0, catalog.SolidCount).Select(i =>
        {
            var s = catalog.Solid(i); Rect2 r = s.LocalBounds;
            return new { id = "catalog-" + i, corners = new[] { Point(s.LocalToWorld * r.Position),
                Point(s.LocalToWorld * new Vector2(r.End.X, r.Position.Y)), Point(s.LocalToWorld * r.End),
                Point(s.LocalToWorld * new Vector2(r.Position.X, r.End.Y)) } };
        }).ToArray();
        return JsonSerializer.Serialize(new
        {
            schema = "knight.current-layout.v1", staticConfigurationOnly = true, wholeGameAccepted = false,
            layoutVersion = CanyonWorldLayout.CurrentLayoutVersion, bounds = Rectangle(CanyonWorldLayout.DesignBounds),
            terrainRevision = catalog.Revision, requiredFragments = QuestFragmentCatalog.RequiredCount,
            sectors = definitions.OrderBy(d => d.Id).Select(d => new { id = d.Id.ToString(), bounds = Rectangle(d.WorldBounds),
                offset = Point(d.WorldOffset), scene = d.Scene.ResourcePath }).ToArray(),
            regions = CanyonWorldLayout.Nodes.Select(n => new { id = n.Id.ToString(), sector = n.SectorId.ToString(),
                bounds = Rectangle(n.WorldBounds), optional = n.Optional, rest = n.RestArea }).ToArray(),
            edges = CanyonWorldLayout.Edges.Select(e => new { from = e.From.ToString(), to = e.To.ToString(),
                bidirectional = e.Bidirectional, gate = e.Gate.ToString(), mechanism = e.RequiredMechanismId,
                shortcut = e.StableShortcutId, status = "topology-not-verified-travel-trajectory" }).ToArray(),
            solids, enemies, fragments, objects, bosses, surfaces, configuredHazardVolumes, configuredMovingGears, configuredArcGears,
            checkpoints = checkpoints.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new { id = p.Key, point = Point(p.Value) }).ToArray(),
            npcs = new[] { new { id = "ECHO", count = 1, placement = "follows-player-no-fixed-spawn", status = "guidance-not-combat-helper" } },
            independentFriendlyBodies = 0, independentMerchants = 0,
            sources = sources.Select(path => new { path, sha256 = Convert.ToHexString(SHA256.HashData(Godot.FileAccess.GetFileAsBytes(path))) }).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private static float[] Point(Vector2 p)
    {
        Require(p.IsFinite(), "non-finite map point"); return new[] { p.X, p.Y };
    }
    private static float[] Rectangle(Rect2 r)
    {
        Require(r.Position.IsFinite() && r.Size.IsFinite() && r.Size.X > 0 && r.Size.Y > 0, "invalid map rectangle");
        return new[] { r.Position.X, r.Position.Y, r.Size.X, r.Size.Y };
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
