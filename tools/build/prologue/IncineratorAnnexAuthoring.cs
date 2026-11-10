using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Tools;

namespace Knight.Build.Prologue;

// Detached staged outputs only. Publishing the two explicitly named source
// files is a separate checked host operation after the process terminates.
public static class IncineratorAnnexAuthoring
{
    private const string BasinPath="res://scenes/prologue/sectors/CanyonBasin.tscn";
    private const string MainPath="res://scenes/prologue/PrologueLevel.tscn";

    public static void StageLeaf(string storage)
    {
        string output=FreshOutput(storage,"CanyonBasin-incinerator-v251.tscn");
        using PackedScene packed=GD.Load<PackedScene>(BasinPath);
        PrologueSector sector=packed.Instantiate<PrologueSector>();
        var owned=new List<Shape2D>();
        try
        {
            bool routeUpdate=sector.Bounds==new Rect2(0,0,5120,720);
            Require(routeUpdate || sector.Bounds==new Rect2(0,0,3840,720),"source Basin is not a supported annex layout");
            var original=ExistingGeometry(sector);
            if(routeUpdate)
            {
                Require(sector.HasNode("Gameplay/Mechanisms/IncineratorPool"),"expanded source lacks the actual incinerator");
                var old=sector.GetNode<TraversalSegment2D>("Gameplay/TraversalContracts/canyonv2routeincinerator-to-steam");
                Require(old.SegmentId=="canyon.v2.route.incinerator-to-steam" &&
                    old.Kind==TraversalSegmentKind.SingleJump && old.LaunchZone==new Rect2(3840,4,100,72) &&
                    old.LandingZone==new Rect2(3720,-72,100,72),"only the retained old upper return can be rebound");
                Require(!sector.HasNode("Gameplay/TraversalContracts/canyonv2routelift-to-incinerator") &&
                    !sector.HasNode("Gameplay/TraversalContracts/canyonv2routeincinerator-to-lift"),"upper contracts already changed");
                old.Free();
                CanyonContentBuilder.AddIncineratorUpperContracts(sector);
            }
            else
            {
                sector.Bounds=new Rect2(0,0,5120,720);
                CanyonContentBuilder.AddIncineratorAnnex(sector);
            }
            // A contracts-only update borrows the saved collision resources;
            // only a newly constructed annex creates shapes owned by us.
            if(!routeUpdate)
                foreach(Node node in Walk(sector))
                    if(node is CollisionShape2D shape && shape.Shape is { } resource &&
                        sector.GetPathTo(shape).ToString().Contains("Incinerator",StringComparison.Ordinal) && !owned.Contains(resource))
                        owned.Add(resource);
            var recovery=sector.GetNode<Area2D>("Gameplay/FallRecovery/RecoveryVolume");
            var collider=recovery.GetNode<CollisionShape2D>("Collision");
            if(!routeUpdate)
            {
                Require(collider.Shape is RectangleShape2D r && r.Size==new Vector2(3840,16) &&
                    recovery.Position==new Vector2(1920,712),"pre-annex recovery strip differs");
                var enlarged=new RectangleShape2D {Size=new Vector2(5120,16)};
                owned.Add(enlarged); collider.Shape=enlarged; recovery.Position=new Vector2(2560,712);
            }
            Require(original.SequenceEqual(ExistingGeometry(sector).Where(pair=>routeUpdate || !pair.Key.Contains("Incinerator",StringComparison.Ordinal))),
                "unrelated original structural geometry changed");
            Error error=PrologueCanyonBuildParts.SaveAndValidate(sector,output,out int before,out int after,out string failure);
            Require(error==Error.Ok && before==after,"staged basin roundtrip failed: "+failure);
            PrintArtifact("leaf",output,null);
            GD.Print($"INCINERATOR_AUTHORING_LEAF_OK nodes_before={before} nodes_after={after} old_geometry_preserved=true staged_only=true output={output}");
        }
        finally
        {
            sector.Free();
            foreach(Shape2D shape in owned) shape.Dispose();
        }
    }

    public static void StageParent(string storage,string stagedLeafPath)
    {
        string output=FreshOutput(storage,"PrologueLevel-incinerator-v251.tscn");
        Require(!string.IsNullOrWhiteSpace(stagedLeafPath) && Path.IsPathFullyQualified(stagedLeafPath) &&
            File.Exists(stagedLeafPath),"an explicitly bound staged leaf is required");
        using PackedScene stagedLeaf=GD.Load<PackedScene>(stagedLeafPath);
        using PackedScene packed=GD.Load<PackedScene>(MainPath);
        PrologueLevelController level=packed.Instantiate<PrologueLevelController>();
        PrologueSectorDefinition replacement=null;
        MinimapTerrainCatalog catalog=null;
        try
        {
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            Require(world.Definitions.Count==5,"current parent must retain its five definitions");
            var old=world.Definitions.Single(d=>d.Id==PrologueSectorId.CanyonBasin);
            Require((old.Bounds==new Rect2(0,0,3840,720) || old.Bounds==new Rect2(0,0,5120,720)) && old.WorldOffset==new Vector2(1280,3600) &&
                old.Scene.ResourcePath==BasinPath,"parent source Basin identity differs");
            PrologueSector expanded=stagedLeaf.Instantiate<PrologueSector>();
            try {Require(expanded.Bounds==new Rect2(0,0,5120,720) &&
                expanded.HasNode("Gameplay/Mechanisms/IncineratorPool"),"staged leaf must contain the full expanded room");}
            finally {expanded.Free();}
            replacement=new PrologueSectorDefinition(PrologueSectorId.CanyonBasin,stagedLeaf,old.WorldOffset,
                new Rect2(0,0,5120,720),old.Previous,old.Next)
            {
                NeighborValues=new Godot.Collections.Array<int>(old.NeighborValues),
                KeyInteractionBounds=new Godot.Collections.Array<Rect2>(old.KeyInteractionBounds)
            };
            var changed=new Godot.Collections.Array<PrologueSectorDefinition>();
            foreach(var definition in world.Definitions) changed.Add(definition.Id==PrologueSectorId.CanyonBasin?replacement:definition);
            world.Definitions=changed;
            var definitions=changed.ToArray();
            var minimap=level.GetNode<PrologueMinimapController>("MinimapController");
            var originalCheckpoints=level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints;
            var checkpoints=MinimapTerrainCatalogBuilder.Checkpoints(definitions);
            Require(checkpoints.Count==originalCheckpoints.Count && checkpoints.All(pair=>
                originalCheckpoints.TryGetValue(pair.Key,out var position) && position==pair.Value),
                "annex cannot relocate or silently replace authored checkpoints");
            catalog=MinimapTerrainCatalogBuilder.Build(definitions);
            minimap.Catalog=catalog;
            int expectedNodes=Walk(level).Count();
            Require(SceneBuildUtil.PackAndSave(level,output)==Error.Ok,"staged parent packing failed");
            using PackedScene saved=GD.Load<PackedScene>(output);
            var copy=saved.Instantiate<PrologueLevelController>();
            try
            {
                Require(Walk(copy).Count()==expectedNodes,"parent lost existing nodes while serializing");
                var actual=copy.GetNode<PrologueMinimapController>("MinimapController").Catalog;
                Require(actual.Revision==catalog.Revision && actual.SerializedValues.AsSpan().SequenceEqual(catalog.SerializedValues),
                    "staged parent minimap differs");
                var actualDefinitions=copy.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
                Require(actualDefinitions.Count==5 && actualDefinitions.Single(d=>d.Id==PrologueSectorId.CanyonBasin).Bounds==new Rect2(0,0,5120,720),
                    "staged parent lost expanded definition");
            }
            finally {copy.Free();}
            // Validate against the new staged geometry before touching either
            // playable source. Only its external leaf path is then rebased.
            string text=File.ReadAllText(output);
            string reference="path=\""+stagedLeaf.ResourcePath.Replace('\\','/')+"\"";
            Require(text.Contains(reference,StringComparison.Ordinal) &&
                text.IndexOf(reference,StringComparison.Ordinal)==text.LastIndexOf(reference,StringComparison.Ordinal),
                "staged parent must contain exactly one explicit staged-leaf reference");
            File.WriteAllText(output,text.Replace(reference,"path=\""+BasinPath+"\"",StringComparison.Ordinal));
            PrintArtifact("parent",output,stagedLeafPath);
            GD.Print($"INCINERATOR_AUTHORING_PARENT_OK nodes={expectedNodes} solids={catalog.SolidCount} revision={catalog.Revision} " +
                $"checkpoints_preserved=true final_leaf_reference=true staged_only=true output={output}");
        }
        finally {level.Free(); catalog?.Dispose(); replacement?.Dispose();}
    }

    private static void PrintArtifact(string mode,string output,string leaf)
    {
        string digest=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output)));
        string input=leaf==null?"none":Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(leaf)));
        GD.Print($"INCINERATOR_AUTHORING_ARTIFACT mode={mode} sha256={digest} leaf_input_sha256={input}");
    }

    private static string FreshOutput(string storage,string name)
    {
        if(!Path.IsPathFullyQualified(storage) || !Directory.Exists(storage))
            throw new ArgumentException("Existing isolated absolute storage required.");
        string output=Path.Combine(storage,name);
        if(File.Exists(output)) throw new InvalidOperationException("Do not overwrite a staged authoring output.");
        return output;
    }

    private static SortedDictionary<string,string> ExistingGeometry(PrologueSector sector)
    {
        var result=new SortedDictionary<string,string>(StringComparer.Ordinal);
        foreach(Node node in Walk(sector.GetNode("Gameplay/Collision")))
            if(node is CollisionShape2D shape && shape.Shape is RectangleShape2D rectangle)
                result.Add(sector.GetPathTo(shape).ToString(),
                    $"{((Node2D)shape.GetParent()).Transform}|{shape.Transform}|{rectangle.Size}|{shape.Disabled}|{shape.OneWayCollision}");
        return result;
    }
    private static IEnumerable<Node> Walk(Node root)
    {yield return root;foreach(Node child in root.GetChildren())foreach(Node node in Walk(child))yield return node;}
    private static void Require(bool value,string message)
    {if(!value)throw new InvalidOperationException(message);}
}
