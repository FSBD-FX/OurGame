using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Integration;

// Break caught: a working runtime fixture whose actual saved/generated Basin
// or saved parent still has no new gear, or loses existing authored content.
public static partial class CanyonMovingGearPlacementTests
{
    private const string GearPath="Gameplay/Mechanisms/UpperGalleryMovingGear";
    private const string BasinPath="res://scenes/prologue/sectors/CanyonBasin.tscn";
    private const string ParentPath="res://scenes/prologue/PrologueLevel.tscn";

    public static async Task RunSaved(SceneTree tree)
    {
        var leaf=GD.Load<PackedScene>(BasinPath).Instantiate<PrologueSector>();
        try { CheckGear(leaf,"current saved leaf"); }
        finally { leaf.Free(); }
        var parent=GD.Load<PackedScene>(ParentPath).Instantiate<PrologueLevelController>();
        try { CheckParent(parent); }
        finally { parent.Free(); }
        GD.Print("MOVING_GEAR_SAVED_LAYOUT_OK saved_leaf=true saved_parent=true solids=158 fragments=9 configured_enemies=76 runtime=false human=false");
        await RunCandidateRuntime(tree,ParentPath);
    }

    public static async Task RunCandidate(SceneTree tree)
    {
        string storage=IsolatedPrologueFixture.ValidateStorageRoot();
        string leafPath=Path.Combine(storage,"CanyonBasin-moving-gear-v314.tscn");
        string parentPath=Path.Combine(storage,"PrologueLevel-moving-gear-v314.tscn");
        TestAssert.True(!File.Exists(leafPath) && !File.Exists(parentPath),"candidate outputs must be fresh");
        var original=GD.Load<PackedScene>(BasinPath).Instantiate<PrologueSector>();
        SortedDictionary<string,string> baseline;
        try { baseline=Fingerprint(original); }
        finally { original.Free(); }
        var generated=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
        try { CheckGear(generated,"production generated leaf"); }
        finally { generated.Free(); }
        // Preserve the current authored visual layers as well as gameplay.
        // Full regeneration also rebuilds older backdrop coverage, which is an
        // unrelated change; the same production authoring operation can add the
        // gear to an instance of the actual saved leaf instead.
        generated=GD.Load<PackedScene>(BasinPath).Instantiate<PrologueSector>();
        try
        {
            EnsureCandidateGear(generated);
            CheckGear(generated,"staged current leaf");
            AssertFingerprint(baseline,generated,"generated");
            TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(generated,leafPath,
                out int before,out int after,out string failure),"moving leaf saved roundtrip: "+failure);
            TestAssert.Equal(before,after,"moving leaf does not lose nodes during packing");
        }
        finally { generated.Free(); }
        var saved=GD.Load<PackedScene>(leafPath).Instantiate<PrologueSector>();
        try
        {
            CheckGear(saved,"staged reloaded leaf");
            // Re-running authoring after publication starts from a leaf that
            // already contains the gear. Exercise that exact retained input,
            // without modifying the actual canonical source just to test it.
            EnsureCandidateGear(saved);
            AssertFingerprint(baseline,saved,"reloaded");
        }
        finally { saved.Free(); }
        var paths=Enum.GetValues<PrologueSectorId>().ToDictionary(id=>id,
            id=>id==PrologueSectorId.CanyonBasin?leafPath:PrologueCanyonBuildParts.ScenePath(id));
        var parent=BuildPrologueLevel.Build(paths);
        try { CheckParent(parent); }
        finally { parent.Free(); }
        // Prove the exact current parent graph too, not only a freshly generated
        // parent that could silently rebuild unrelated UI/art nodes. Replace only
        // the owned array entry, never mutate the cached source definition.
        parent=GD.Load<PackedScene>(ParentPath).Instantiate<PrologueLevelController>();
        var parentBaseline=Fingerprint(parent);
        var world=parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var old=world.Definitions.Single(d=>d.Id==PrologueSectorId.CanyonBasin);
        using var replacement=new PrologueSectorDefinition(old.Id,GD.Load<PackedScene>(leafPath),
            old.WorldOffset,old.Bounds,old.Previous,old.Next)
        {
            NeighborValues=new Godot.Collections.Array<int>(old.NeighborValues),
            KeyInteractionBounds=new Godot.Collections.Array<Rect2>(old.KeyInteractionBounds)
        };
        try
        {
            var changed=new Godot.Collections.Array<PrologueSectorDefinition>();
            foreach(var definition in world.Definitions) changed.Add(definition.Id==old.Id?replacement:definition);
            world.Definitions=changed;
            TestAssert.True(parentBaseline.SequenceEqual(Fingerprint(parent)),"candidate cannot rebuild unrelated current parent nodes or exported settings");
            CheckParent(parent);
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(parent,parentPath),"current parent binds the exact independently saved candidate leaf");
        }
        finally { parent.Free(); }
        var reloaded=GD.Load<PackedScene>(parentPath).Instantiate<PrologueLevelController>();
        try { CheckParent(reloaded); }
        finally { reloaded.Free(); }
        GD.Print("MOVING_GEAR_CANDIDATE_LAYOUT_OK original_content_preserved=true saved_leaf=true current_parent_graph=true bound_candidate_leaf=true solids=158 fragments=9 configured_enemies=76 staged_only=true human=false");
        await RunCandidateRuntime(tree,parentPath);
    }

    private static void CheckParent(PrologueLevelController level)
    {
        var definitions=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
        TestAssert.Equal(5,definitions.Count,"new gear retains all five saved sectors");
        int fragments=0;
        foreach(var definition in definitions)
        {
            var sector=definition.Scene.Instantiate<PrologueSector>();
            try
            {
                fragments+=Walk(sector).Count(n=>n is Knight.Prologue.Collections.QuestFragment);
                if(definition.Id==PrologueSectorId.CanyonBasin)
                {
                    TestAssert.True(definition.WorldOffset==new Vector2(1280,3600) &&
                        definition.Bounds==new Rect2(0,0,5120,720),"gear does not enlarge or relocate Basin bounds");
                    CheckGear(sector,"parent's actual Basin dependency");
                }
            }
            finally { sector.Free(); }
        }
        TestAssert.Equal(9,fragments,"all nine saved fragments remain present");
        using var expected=MinimapTerrainCatalogBuilder.Build(definitions.ToArray());
        var actual=level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
        TestAssert.True(expected.SolidCount==158 && expected.Revision==actual.Revision &&
            expected.SerializedValues.AsSpan().SequenceEqual(actual.SerializedValues),"gear is not new solid terrain and cannot stale the saved minimap");
        TestAssert.Equal(26,CanyonEncounterCatalog.Normal.Sum(e=>e.Enemies.Count),"existing encounter slots remain26");
        TestAssert.Equal(50,Knight.Prologue.Residents.ResidentAuthoredLayout.Load().Count,"existing resident slots remain50");
    }

    private static void EnsureCandidateGear(PrologueSector sector)
    {
        if(!sector.HasNode(GearPath)) CanyonContentBuilder.AddUpperGalleryMovingGear(sector);
        CheckGear(sector,"candidate authoring input");
    }

    private static void CheckGear(PrologueSector sector,string context)
    {
        var gear=sector.GetNodeOrNull<CanyonMovingGear>(GearPath);
        TestAssert.True(gear!=null,"MOVING_GEAR_PLACEMENT_RED: "+context+" must contain the real UpperGalleryMovingGear");
        TestAssert.True(Walk(sector).OfType<CanyonMovingGear>().Count()==1 &&
            gear.Position==new Vector2(2120,-50) && gear.TravelOffset==new Vector2(0,80) &&
            gear.Radius==24 && gear.Damage==2 && gear.PeriodSeconds==3.2f && gear.ContactIntervalSeconds==.5f,
            context+": one bounded new gear uses the audited safe-window configuration");
        var shape=gear.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(gear.GetChildCount()==1 && shape.Shape is CircleShape2D {Radius:24} &&
            shape.Transform==Transform2D.Identity && !shape.Disabled,
            context+": matching saved circle, no new solid or grapple proxy");
    }

    // Selected authoring comparison: paths/types/transforms, collision extents
    // and simple C# exports. This is not a complete built-in/storage-property or
    // anonymous-resource/array comparison. Current serialized artifacts are
    // independently reviewed too; runtime getters/resource identities are not data.
    private static void AssertFingerprint(SortedDictionary<string,string> baseline,PrologueSector current,string stage)
    {
        var actual=Fingerprint(current);
        var differences=baseline.Keys.Union(actual.Keys).Where(key=>
            !baseline.TryGetValue(key,out string old) || !actual.TryGetValue(key,out string now) || old!=now).Take(8)
            .Select(key=>$"{key}: before=[{baseline.GetValueOrDefault(key,"<missing>")}] after=[{actual.GetValueOrDefault(key,"<missing>")}]");
        TestAssert.True(baseline.SequenceEqual(actual),"moving gear preserves original authored content "+stage+": "+string.Join("; ",differences));
    }

    private static SortedDictionary<string,string> Fingerprint(Node root)
    {
        var values=new SortedDictionary<string,string>(StringComparer.Ordinal);
        foreach(Node node in Walk(root))
        {
            string path=root.GetPathTo(node).ToString();
            if(path==GearPath || path.StartsWith(GearPath+"/",StringComparison.Ordinal)) continue;
            var fields=new List<string>{node.GetType().FullName};
            if(node is Node2D n) fields.Add("transform="+Value(n.Transform));
            if(node is CollisionShape2D c) fields.Add($"shape={c.Shape?.GetType().Name}:{Value(c.Shape?.GetRect())}:{c.Disabled}:{c.OneWayCollision}");
            foreach(var property in node.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance)
                .Where(p=>p.CanRead && p.GetIndexParameters().Length==0 && p.GetCustomAttribute<ExportAttribute>()!=null)
                .OrderBy(p=>p.Name,StringComparer.Ordinal))
            {
                object value=property.GetValue(node);
                if(value is null || value is string or bool or int or uint or long or ulong or float or double or
                    Enum or Vector2 or Rect2 or Color or NodePath)
                    fields.Add(property.Name+"="+Value(value));
                else if(value is Resource resource && !string.IsNullOrEmpty(resource.ResourcePath))
                    fields.Add(property.Name+"="+resource.ResourcePath);
            }
            values.Add(path,string.Join("|",fields));
        }
        return values;
    }
    private static string Value(object value)=>value switch
    {
        float f=>(f==0?0f:f).ToString(System.Globalization.CultureInfo.InvariantCulture),
        double d=>(d==0?0d:d).ToString(System.Globalization.CultureInfo.InvariantCulture),
        Vector2 v=>$"({Value(v.X)},{Value(v.Y)})",
        Rect2 r=>$"{Value(r.Position)}:{Value(r.Size)}",
        Transform2D t=>$"{Value(t.X)}:{Value(t.Y)}:{Value(t.Origin)}",
        _=>value?.ToString()??"<null>"
    };
    private static IEnumerable<Node> Walk(Node root)
    { yield return root; foreach(Node child in root.GetChildren()) foreach(Node n in Walk(child)) yield return n; }
}
