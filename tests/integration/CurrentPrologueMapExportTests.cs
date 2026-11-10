using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tools.Export;

namespace Knight.Tests.Integration;

public static class CurrentPrologueMapExportTests
{
    public static void Run(string storage)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            var definitions = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
            using var actual = MinimapTerrainCatalogBuilder.Build(definitions);
            var saved = level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            bool catalogMatches = saved.Revision == actual.Revision &&
                saved.SerializedValues.AsSpan().SequenceEqual(actual.SerializedValues);
            GD.Print($"CURRENT_PROLOGUE_MAP_CATALOG_STATE definitions={definitions.Length} " +
                $"saved_solids={saved.SolidCount} actual_solids={actual.SolidCount} match={catalogMatches} " +
                $"saved_revision={saved.Revision} actual_revision={actual.Revision}");
            if (!catalogMatches)
            {
                // Stage evidence only; never publish a replacement into the live scene here.
                using var candidate = new FileStream(Path.Combine(storage, "candidate-minimap-catalog.bin"),
                    FileMode.CreateNew, System.IO.FileAccess.Write);
                candidate.Write(actual.SerializedValues);
                GD.Print($"CURRENT_PROLOGUE_MAP_CATALOG_MISMATCH saved={saved.SolidCount}/{saved.Revision} " +
                    $"actual={actual.SolidCount}/{actual.Revision} candidate_bytes={actual.SerializedValues.Length}");
            }
            TestAssert.True(definitions.Length == 5 && actual.SolidCount == 158 && catalogMatches,
                "map export must use the actual normal saved parent and all158 physical solids");
            string output = Path.Combine(storage, "current-prologue-map.json");
            TestAssert.True(!File.Exists(output), "map export cannot silently overwrite an existing artifact");
            File.WriteAllText(output, CurrentPrologueMapSnapshot.BuildJson(level));
            TestAssert.True(File.Exists(output), "current map snapshot absent: no annotated artifact was exported");
            using var json = JsonDocument.Parse(File.ReadAllText(output));
            var root = json.RootElement;
            TestAssert.Equal("knight.current-layout.v1", root.GetProperty("schema").GetString(), "map schema");
            TestAssert.True(root.GetProperty("staticConfigurationOnly").GetBoolean() &&
                !root.GetProperty("wholeGameAccepted").GetBoolean(), "map cannot certify runtime behavior");
            EqualPoint(root.GetProperty("bounds"), 0, 0, 7680, 4320);
            TestAssert.Equal(saved.Revision, root.GetProperty("terrainRevision").GetString(), "same real catalog revision");
            var solids = root.GetProperty("solids").EnumerateArray().ToArray();
            TestAssert.Equal(158, solids.Length, "all actual static structural solids exported");
            TestAssert.True(solids.Any(x => Corners(x, 0, 0, 48, 720)) &&
                solids.Any(x => Corners(x, 2856, 3720, 136, 24)) &&
                solids.Count(x => Corners(x, 4660, 1620, 4, 21)) == 1 &&
                solids.Count(x => Corners(x, 4660, 1640, 240, 40)) == 1,
                "world rectangles retain offsets, including one saved baffle and unchanged Grip8 floor");
            TestAssert.True(solids.Count(x => Corners(x, 5360, 3836, 800, 24)) == 1 &&
                solids.Count(x => Corners(x, 5440, 4296, 720, 8)) == 1 &&
                solids.Count(x => Corners(x, 6376, 3600, 24, 720)) == 1,
                "actual annex gallery, containment floor and outer wall retain independent world coordinates");
            var regions = root.GetProperty("regions").EnumerateArray().ToArray();
            TestAssert.Equal(15, regions.Length, "all current functional regions include the later optional annex");
            var annex = regions.Single(x => x.GetProperty("id").GetString() == "IncineratorAnnex");
            EqualPoint(annex.GetProperty("bounds"), 5120, 3600, 1280, 720);
            TestAssert.True(annex.GetProperty("optional").GetBoolean() &&
                annex.GetProperty("sector").GetString() == "CanyonBasin", "annex is a Basin side region, not a new stage");
            var edges = root.GetProperty("edges").EnumerateArray().ToArray();
            TestAssert.True(edges.Count(x => x.GetProperty("from").GetString() == "SteamValves" &&
                x.GetProperty("to").GetString() == "IncineratorAnnex" && !x.GetProperty("bidirectional").GetBoolean()) == 1 &&
                edges.Count(x => x.GetProperty("from").GetString() == "CounterweightLift" &&
                x.GetProperty("to").GetString() == "IncineratorAnnex" && x.GetProperty("bidirectional").GetBoolean()) == 1 &&
                !edges.Any(x => x.GetProperty("from").GetString() == "IncineratorAnnex" &&
                    x.GetProperty("to").GetString() == "SteamValves"),
                "annex topology preserves one-way steam entry and the distinct bidirectional lift return");
            TestAssert.True(root.TryGetProperty("configuredHazardVolumes", out var volumes) &&
                volumes.ValueKind == JsonValueKind.Array && volumes.GetArrayLength() == 1,
                "CURRENT_MAP_INCINERATOR_VOLUME_RED: fixed high-temperature contact volume must not disappear into a point marker");
            var pool = volumes[0];
            TestAssert.True(pool.GetProperty("id").GetString() == "CanyonBasin/Gameplay/Mechanisms/IncineratorPool" &&
                pool.GetProperty("objectId").GetString() == pool.GetProperty("id").GetString() &&
                pool.GetProperty("type").GetString() == "CanyonIncinerator" &&
                pool.GetProperty("status").GetString() == "configured-volume-not-live-activation",
                "configured pool uses its saved path identity and is not a live activation claim");
            EqualPoint(pool.GetProperty("corners")[0], 5440, 4020);
            EqualPoint(pool.GetProperty("corners")[1], 6160, 4020);
            EqualPoint(pool.GetProperty("corners")[2], 6160, 4296);
            EqualPoint(pool.GetProperty("corners")[3], 5440, 4296);
            TestAssert.True(pool.GetProperty("pulseSeconds").GetSingle() == .1f &&
                pool.GetProperty("damagePolicy").GetString() == "max-health-contact-attempt" &&
                pool.GetProperty("unblockable").GetBoolean() && pool.GetProperty("respectsHealthInvulnerability").GetBoolean() &&
                root.GetProperty("objects").EnumerateArray().Count(x => x.GetProperty("id").GetString() ==
                    pool.GetProperty("objectId").GetString()) == 1,
                "fixed hazard volume annotates one existing object and retains actual health-pipeline rejection semantics");
            TestAssert.True(root.GetProperty("sources").EnumerateArray().Count(x =>
                x.GetProperty("path").GetString() == "res://scripts/mechanisms/CanyonIncinerator.cs") == 1,
                "configured hazard semantics bind the actual current damage-source implementation hash");
            var enemies = root.GetProperty("enemies").EnumerateArray().ToArray();
            TestAssert.True(enemies.Length == 76 && enemies.Select(x => x.GetProperty("id").GetString()).Distinct().Count() == 76 &&
                enemies.Count(x => x.GetProperty("cohort").GetString() == "encounter") == 26 &&
                enemies.Count(x => x.GetProperty("cohort").GetString() == "resident") == 50,
                "original26 slots plus50 authored residents, no alternate spawn double counting");
            var echo = enemies.Single(x => x.GetProperty("id").GetString() == "canyon.v2.resident.split-level.02");
            EqualPoint(echo.GetProperty("point"), 4750, 1608);
            EqualPoint(echo.GetProperty("territory"), 3840, 1200, 1280, 800);
            EqualPoint(echo.GetProperty("patrol")[1], 4686, 1608);
            EqualPoint(echo.GetProperty("patrol")[2], 4814, 1608);
            var fragments = root.GetProperty("fragments").EnumerateArray().ToArray();
            TestAssert.True(fragments.Length == 9 && fragments.Select(x => x.GetProperty("id").GetString()).Distinct().Count() == 9 &&
                root.GetProperty("requiredFragments").GetInt32() == 6, "nine actual fragment sites, original six-item admission component");
            var start = root.GetProperty("checkpoints").EnumerateArray().Single(x => x.GetProperty("id").GetString() == "canyon.v2.start");
            EqualPoint(start.GetProperty("point"), 160, 600);
            var boss = root.GetProperty("bosses").EnumerateArray().Single();
            EqualPoint(boss.GetProperty("point"), 6720, 1080);
            TestAssert.Equal("configured-spawn-not-live-birth", boss.GetProperty("status").GetString(), "boss point is not a live birth claim");
            TestAssert.True(root.GetProperty("objects").GetArrayLength() > 0 && root.GetProperty("sources").GetArrayLength() >= 8,
                "actual saved mechanism anchors and bound source digests are present");
            TestAssert.True(root.TryGetProperty("configuredMovingGears",out var moving) &&
                moving.ValueKind==JsonValueKind.Array && moving.GetArrayLength()==1,
                "CURRENT_MAP_MOVING_GEAR_RED: moving gear needs an authored full-rail record, not only an initial point");
            var rail=moving[0];
            string gearId="CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear";
            TestAssert.True(rail.GetProperty("id").GetString()==gearId && rail.GetProperty("objectId").GetString()==gearId &&
                rail.GetProperty("type").GetString()=="CanyonMovingGear" && rail.GetProperty("radius").GetSingle()==24 &&
                rail.GetProperty("periodSeconds").GetSingle()==3.2f && rail.GetProperty("damage").GetInt32()==2 &&
                rail.GetProperty("contactIntervalSeconds").GetSingle()==.5f &&
                rail.GetProperty("status").GetString()=="configured-rail-not-runtime-route-proof" &&
                rail.GetProperty("envelopePolicy").GetString()=="conservative-aabb-not-damage-shape",
                "rail keeps current authoring and distinguishes reservation from actual capsule damage");
            EqualPoint(rail.GetProperty("start"),3400,3550); EqualPoint(rail.GetProperty("end"),3400,3630);
            EqualPoint(rail.GetProperty("envelope"),3376,3526,48,128);
            TestAssert.Equal(1,root.GetProperty("objects").EnumerateArray().Count(x=>x.GetProperty("id").GetString()==gearId),
                "rail annotates exactly one actual saved device, not a second trap");
            TestAssert.True(root.TryGetProperty("configuredArcGears",out var arcs) &&
                arcs.ValueKind==JsonValueKind.Array && arcs.GetArrayLength()==1,
                "CURRENT_MAP_ARC_GEAR_RED: saved arc needs pivot/signed curve and full envelope, not an initial point or straight chord");
            var arc=arcs[0];
            const string arcId="CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear";
            TestAssert.True(arc.GetProperty("id").GetString()==arcId && arc.GetProperty("objectId").GetString()==arcId &&
                arc.GetProperty("type").GetString()=="CanyonArcGear" && arc.GetProperty("radius").GetSingle()==24 &&
                arc.GetProperty("swingRadians").GetSingle()==-.6435011f && arc.GetProperty("periodSeconds").GetSingle()==3.2f &&
                arc.GetProperty("damage").GetInt32()==2 && arc.GetProperty("contactIntervalSeconds").GetSingle()==.5f &&
                arc.GetProperty("status").GetString()=="configured-arc-not-runtime-route-proof" &&
                arc.GetProperty("envelopePolicy").GetString()=="conservative-aabb-not-damage-shape",
                "arc retains signed motor authoring and distinguishes reservation from actual native damage");
            EqualPoint(arc.GetProperty("start"),3320,3956); EqualPoint(arc.GetProperty("pivot"),3400,3716);
            var end=arc.GetProperty("end").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            var envelope=arc.GetProperty("envelope").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            TestAssert.True(end.Length==2 && new Vector2(end[0],end[1]).DistanceTo(new(3480,3956))<.01f &&
                envelope.Length==4 && new Vector2(envelope[0],envelope[1]).DistanceTo(new(3296,3932))<.01f &&
                new Vector2(envelope[2],envelope[3]).DistanceTo(new(208,60.9822f))<.01f,
                "literal full arc envelope contains its middle low point, not only equal-height endpoints");
            TestAssert.Equal(1,root.GetProperty("objects").EnumerateArray().Count(x=>x.GetProperty("id").GetString()==arcId),
                "arc record annotates exactly one real saved sibling");
            TestAssert.Equal(57,root.GetProperty("objects").GetArrayLength(),"one new logical arc device, not a new solid or duplicate rail");
            foreach(string source in new[]{"CanyonArcGear","ArcGearMotionModel","CircularArcSweep"})
                TestAssert.Equal(1,root.GetProperty("sources").EnumerateArray().Count(x=>
                    x.GetProperty("path").GetString()=="res://scripts/mechanisms/"+source+".cs"),"one current source binding for "+source);
            AssertArcExportRejectsBadSavedConfiguration(level);
            GD.Print("CURRENT_PROLOGUE_MAP_DATA_SHA256 sha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))));
            GD.Print("CURRENT_PROLOGUE_MAP_EXPORT_OK saved_parent=true solids=158 encounter_slots=26 residents=50 fragments=9 " +
                "world_offsets=true boss_configured=true static_configuration_only=true whole_goal=false");
        }
        finally { level.Free(); }
    }

    private static void AssertArcExportRejectsBadSavedConfiguration(PrologueLevelController level)
    {
        var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var original=world.Definitions;
        var basin=original.Single(x=>x.Id==PrologueSectorId.CanyonBasin);
        const string path="Gameplay/Mechanisms/UpperGalleryUnderArcGear";
        var cases=new (string Name,Action<CanyonArcGear,CollisionShape2D> Change)[]{
            ("disabled",(a,c)=>c.Disabled=true),
            ("radius-mismatch",(a,c)=>((CircleShape2D)c.Shape).Radius=25),
            ("shape-offset",(a,c)=>c.Position=Vector2.Right),
            ("source-rotation",(a,c)=>a.Rotation=.1f),
            ("source-nonuniform",(a,c)=>a.Scale=new(1.01f,1)),
            ("zero-pivot",(a,c)=>a.PivotOffset=Vector2.Zero),
            ("long-pivot",(a,c)=>a.PivotOffset=new(513,0)),
            ("zero-angle",(a,c)=>a.SwingRadians=0),
            ("excess-angle",(a,c)=>a.SwingRadians=Mathf.Pi+.1f),
            ("short-period",(a,c)=>a.PeriodSeconds=.9f),
            ("excess-speed",(a,c)=>{a.PivotOffset=new(512,0);a.SwingRadians=Mathf.Pi;a.PeriodSeconds=1;}),
            ("zero-damage",(a,c)=>a.Damage=0),
            ("excess-radius",(a,c)=>{a.Radius=97;((CircleShape2D)c.Shape).Radius=97;})
        };
        foreach(var test in cases)
        {
            var leaf=basin.Scene.Instantiate<PrologueSector>();
            using var privateCircle=new CircleShape2D {Radius=24};
            using var packed=new PackedScene();
            try
            {
                var arc=leaf.GetNode<CanyonArcGear>(path);
                var collider=arc.GetNode<CollisionShape2D>("Collision");
                collider.Shape=privateCircle; // Never mutate a borrowed cached shape.
                test.Change(arc,collider);
                TestAssert.Equal(Error.Ok,packed.Pack(leaf),"bad-input fixture still uses a real serialized leaf graph");
                using var replacement=new PrologueSectorDefinition(basin.Id,packed,basin.WorldOffset,basin.Bounds,basin.Previous,basin.Next)
                {
                    NeighborValues=new Godot.Collections.Array<int>(basin.NeighborValues),
                    KeyInteractionBounds=new Godot.Collections.Array<Rect2>(basin.KeyInteractionBounds)
                };
                var changed=new Godot.Collections.Array<PrologueSectorDefinition>();
                foreach(var definition in original)changed.Add(definition.Id==basin.Id?replacement:definition);
                world.Definitions=changed;
                bool rejected=false;
                try {CurrentPrologueMapSnapshot.BuildJson(level);}
                catch(InvalidOperationException error) when(error.Message.Contains("unsupported configured arc",StringComparison.Ordinal)){rejected=true;}
                catch(ArgumentOutOfRangeException error) when(error.ParamName is "pivotOffset" or "swingRadians" or "periodSeconds"){rejected=true;}
                finally {world.Definitions=original;}
                TestAssert.True(rejected,"CURRENT_MAP_ARC_REJECT_RED: exporter must reject actual saved bad input "+test.Name);
                GD.Print("CURRENT_MAP_ARC_REJECT_CASE case="+test.Name+" detached_saved_config=true canonical_mutated=false");
            }
            finally {world.Definitions=original;leaf.Free();}
        }
        TestAssert.True(ReferenceEquals(original,world.Definitions),"restore exact borrowed parent definitions after bad-config controls");
        GD.Print("CURRENT_MAP_ARC_REJECT_OK cases=13 actual_saved_shape=true actual_exporter=true canonical_mutated=false native_actor=false human=false");
    }

    private static void EqualPoint(JsonElement values, params float[] expected)
    {
        TestAssert.True(values.ValueKind == JsonValueKind.Array && values.GetArrayLength() == expected.Length &&
            values.EnumerateArray().Select(x => x.GetSingle()).SequenceEqual(expected), "independent authored coordinates match");
    }

    private static bool Corners(JsonElement solid, float x, float y, float w, float h)
    {
        var points = solid.GetProperty("corners").EnumerateArray().ToArray();
        return points.Length == 4 && points.Any(p => p[0].GetSingle() == x && p[1].GetSingle() == y) &&
            points.Any(p => p[0].GetSingle() == x + w && p[1].GetSingle() == y + h);
    }
}
