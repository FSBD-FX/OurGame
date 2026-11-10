using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Integration;

// Break caught: a tested arc runtime that was never added to the saved or
// generated map, or replaces existing terrain/content instead of adding a sibling.
public static partial class CanyonArcGearPlacementTests
{
    private const string GearPath = "Gameplay/Mechanisms/UpperGalleryUnderArcGear";
    private const string BasinPath = "res://scenes/prologue/sectors/CanyonBasin.tscn";
    private const string ParentPath = "res://scenes/prologue/PrologueLevel.tscn";

    public static async Task RunSaved(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        var leaf = GD.Load<PackedScene>(BasinPath).Instantiate<PrologueSector>();
        try { CheckGear(leaf, "current saved leaf"); }
        finally { leaf.Free(); }
        var parent = GD.Load<PackedScene>(ParentPath).Instantiate<PrologueLevelController>();
        try { CheckParent(parent); }
        finally { parent.Free(); }
        GD.Print("ARC_GEAR_SAVED_LAYOUT_OK saved_leaf=true saved_parent=true solids=158 fragments=9 configured_enemies=76 runtime=false human=false");
        await RunRuntime(tree, ParentPath);
    }

    public static async Task RunCandidate(SceneTree tree)
    {
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        string leafPath = Path.Combine(storage, "CanyonBasin-under-arc-v331.tscn");
        string parentPath = Path.Combine(storage, "PrologueLevel-under-arc-v331.tscn");
        TestAssert.True(!File.Exists(leafPath) && !File.Exists(parentPath), "arc candidate must be a fresh artifact");
        var generated = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
        try { CheckGear(generated, "production generated leaf"); }
        finally { generated.Free(); }
        var leaf = GD.Load<PackedScene>(BasinPath).Instantiate<PrologueSector>();
        var baseline = Fingerprint(leaf);
        try
        {
            EnsureArc(leaf); CheckGear(leaf, "candidate current leaf");
            AssertFingerprint(baseline, leaf, "candidate authoring");
            TestAssert.Equal(Error.Ok, PrologueCanyonBuildParts.SaveAndValidate(leaf, leafPath,
                out int before, out int after, out string failure), "arc candidate roundtrip: " + failure);
            TestAssert.Equal(before, after, "arc packing retains all nodes");
        }
        finally { leaf.Free(); }
        var saved = GD.Load<PackedScene>(leafPath).Instantiate<PrologueSector>();
        try
        {
            CheckGear(saved, "reloaded candidate"); EnsureArc(saved);
            AssertFingerprint(baseline, saved, "reloaded existing-arc rerun");
            bool duplicateRejected = false;
            try { CanyonContentBuilder.AddUpperGalleryUnderArcGear(saved); }
            catch (InvalidOperationException) { duplicateRejected = true; }
            TestAssert.True(duplicateRejected && Walk(saved).OfType<CanyonArcGear>().Count() == 1,
                "production authoring rejects duplicate without silently replacing the saved arc");
        }
        finally { saved.Free(); }
        var parent = GD.Load<PackedScene>(ParentPath).Instantiate<PrologueLevelController>();
        var parentBaseline = Fingerprint(parent);
        var world = parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var old = world.Definitions.Single(d => d.Id == PrologueSectorId.CanyonBasin);
        using var replacement = new PrologueSectorDefinition(old.Id, GD.Load<PackedScene>(leafPath),
            old.WorldOffset, old.Bounds, old.Previous, old.Next)
        {
            NeighborValues = new Godot.Collections.Array<int>(old.NeighborValues),
            KeyInteractionBounds = new Godot.Collections.Array<Rect2>(old.KeyInteractionBounds)
        };
        try
        {
            var definitions = new Godot.Collections.Array<PrologueSectorDefinition>();
            foreach (var definition in world.Definitions) definitions.Add(definition.Id == old.Id ? replacement : definition);
            world.Definitions = definitions;
            TestAssert.True(parentBaseline.SequenceEqual(Fingerprint(parent)), "candidate cannot rebuild current parent art or UI");
            CheckParent(parent);
            TestAssert.Equal(Error.Ok, SceneBuildUtil.PackAndSave(parent, parentPath), "current parent binds only candidate Basin dependency");
        }
        finally { parent.Free(); }
        var reloadedParent = GD.Load<PackedScene>(parentPath).Instantiate<PrologueLevelController>();
        try { CheckParent(reloadedParent); }
        finally { reloadedParent.Free(); }
        GD.Print("ARC_GEAR_GENERATED_LAYOUT_OK generated_leaf=true original_counts=true runtime=false human=false");
        GD.Print("ARC_GEAR_CANDIDATE_LAYOUT_OK original_content_preserved=true saved_reload=true current_parent_graph=true bound_candidate_leaf=true duplicate_rejected=true staged_only=true human=false");
        await RunRuntime(tree, parentPath);
        GD.Print("ARC_GEAR_CANDIDATE_ARTIFACT leaf_sha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(leafPath))) +
            " parent_sha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(parentPath))) + " runtime_qualified=true canonical_published=false");
    }

    private static void CheckGear(PrologueSector sector, string context)
    {
        var gear = sector.GetNodeOrNull<CanyonArcGear>(GearPath);
        TestAssert.True(gear != null, "ARC_GEAR_PLACEMENT_RED: " + context + " must contain the real UpperGalleryUnderArcGear");
        TestAssert.True(Walk(sector).OfType<CanyonArcGear>().Count() == 1 &&
            gear.Position == new Vector2(2040, 356) && gear.PivotOffset == new Vector2(80, -240) &&
            gear.SwingRadians == -.6435011f && gear.PeriodSeconds == 3.2f &&
            gear.Radius == 24 && gear.Damage == 2 && gear.ContactIntervalSeconds == .5f,
            context + ": one independent arc uses the explicit walk/high-jump configuration");
        var collision = gear.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(gear.GetChildCount() == 1 && collision.Shape is CircleShape2D { Radius: 24 } &&
            collision.Transform == Transform2D.Identity && !collision.Disabled,
            context + ": exact circle and no added solid or grapple proxy");
        var straight = sector.GetNode<CanyonMovingGear>("Gameplay/Mechanisms/UpperGalleryMovingGear");
        TestAssert.True(Walk(sector).OfType<CanyonMovingGear>().Count() == 1 &&
            straight.Position == new Vector2(2120, -50) && straight.TravelOffset == new Vector2(0, 80) &&
            straight.Radius == 24 && straight.Damage == 2 && straight.PeriodSeconds == 3.2f,
            context + ": original straight is retained, not replaced or recounted as arc");
    }

    private static void EnsureArc(PrologueSector sector)
    { if (!sector.HasNode(GearPath)) CanyonContentBuilder.AddUpperGalleryUnderArcGear(sector); }

    // Selected node/type/transform/shape/export preservation, supplemented by
    // independent serialized-artifact diff at publication; not a full built-in
    // storage-property/anonymous-resource/array comparison.
    private static SortedDictionary<string, string> Fingerprint(Node root)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (Node node in Walk(root))
        {
            string path = root.GetPathTo(node).ToString();
            if (path == GearPath || path.StartsWith(GearPath + "/", StringComparison.Ordinal)) continue;
            var fields = new List<string> { node.GetType().FullName };
            if (node is Node2D n) fields.Add("transform=" + Value(n.Transform));
            if (node is CollisionShape2D c) fields.Add($"shape={c.Shape?.GetType().Name}:{Value(c.Shape?.GetRect())}:{c.Disabled}:{c.OneWayCollision}");
            foreach (var property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<ExportAttribute>() != null)
                .OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                object value = property.GetValue(node);
                if (value is null || value is string or bool or int or uint or long or ulong or float or double or
                    Enum or Vector2 or Rect2 or Color or NodePath) fields.Add(property.Name + "=" + Value(value));
                else if (value is Resource resource && !string.IsNullOrEmpty(resource.ResourcePath))
                    fields.Add(property.Name + "=" + resource.ResourcePath);
            }
            values.Add(path, string.Join("|", fields));
        }
        return values;
    }
    private static void AssertFingerprint(SortedDictionary<string, string> before, Node root, string label)
    {
        var after = Fingerprint(root);
        TestAssert.True(before.SequenceEqual(after), "arc preserves existing authored nodes " + label + ": " +
            string.Join("; ", before.Keys.Union(after.Keys).Where(k => before.GetValueOrDefault(k) != after.GetValueOrDefault(k))
                .Take(8).Select(k => k + " old=" + before.GetValueOrDefault(k) + " new=" + after.GetValueOrDefault(k))));
    }
    private static string Value(object value) => value switch
    {
        float f => (f == 0 ? 0f : f).ToString(System.Globalization.CultureInfo.InvariantCulture),
        double d => (d == 0 ? 0d : d).ToString(System.Globalization.CultureInfo.InvariantCulture),
        Vector2 v => $"({Value(v.X)},{Value(v.Y)})",
        Rect2 r => $"{Value(r.Position)}:{Value(r.Size)}",
        Transform2D t => $"{Value(t.X)}:{Value(t.Y)}:{Value(t.Origin)}",
        _ => value?.ToString() ?? "<null>"
    };

    private static void CheckParent(PrologueLevelController level)
    {
        var definitions = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
        TestAssert.Equal(5, definitions.Count, "arc retains the five saved sectors");
        int fragments = 0, entranceFixed = 0;
        foreach (var definition in definitions)
        {
            var sector = definition.Scene.Instantiate<PrologueSector>();
            try
            {
                fragments += Walk(sector).Count(n => n is Knight.Prologue.Collections.QuestFragment);
                if (definition.Id == PrologueSectorId.CanyonEntrance)
                    entranceFixed += Walk(sector).Count(n => n.GetType() == typeof(CanyonFixedGear));
                if (definition.Id == PrologueSectorId.CanyonBasin)
                {
                    TestAssert.True(definition.WorldOffset == new Vector2(1280, 3600) &&
                        definition.Bounds == new Rect2(0, 0, 5120, 720), "arc does not enlarge the Basin");
                    CheckGear(sector, "current parent's actual Basin dependency");
                }
            }
            finally { sector.Free(); }
        }
        TestAssert.Equal(9, fragments, "all existing saved fragments retained");
        TestAssert.Equal(2, entranceFixed, "two original entrance fixed gears retained");
        using var expected = MinimapTerrainCatalogBuilder.Build(definitions.ToArray());
        var actual = level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
        TestAssert.True(expected.SolidCount == 158 && expected.Revision == actual.Revision &&
            expected.SerializedValues.AsSpan().SequenceEqual(actual.SerializedValues), "arc is not new minimap terrain");
        TestAssert.Equal(26, CanyonEncounterCatalog.Normal.Sum(e => e.Enemies.Count), "original encounter26 retained");
        TestAssert.Equal(50, ResidentAuthoredLayout.Load().Count, "original resident50 retained");
    }

    private static IEnumerable<Node> Walk(Node root)
    { yield return root; foreach (Node child in root.GetChildren()) foreach (Node n in Walk(child)) yield return n; }
}
