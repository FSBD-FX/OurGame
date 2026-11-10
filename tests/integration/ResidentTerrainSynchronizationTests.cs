using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Presentation;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Tools;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class ResidentTerrainSynchronizationTests
{
    internal const string BaselineRoot="res://tests/fixtures/resident-terrain-v059/";
    internal static IReadOnlyDictionary<PrologueSectorId,string> BaselinePaths => Enum.GetValues<PrologueSectorId>().ToDictionary(x=>x,x=>BaselineRoot+x+".tscn");
    private static readonly (PrologueSectorId Id,string Name,Rect2 World,bool Catch)[] Expected =
    {
        (PrologueSectorId.CanyonDescent,"ArchiveResidentLowerFloor",new(1424,1776,496,24),false),
        (PrologueSectorId.CanyonDescent,"ArchiveResidentUpperFloor",new(1736,1248,232,24),false),
        (PrologueSectorId.CanyonDescent,"ArchiveResidentUpperBackWall",new(1944,1100,24,172),false),
        (PrologueSectorId.CanyonDescent,"FrozenOuterUpperWall",new(1280,1800,36,280),false),
        (PrologueSectorId.CanyonDescent,"FrozenSentryRecessWall",new(1280,2080,8,140),false),
        (PrologueSectorId.CanyonDescent,"FrozenOuterLowerWall",new(1280,2220,36,1140),false),
        (PrologueSectorId.CanyonDescent,"FrozenUpperResidentShelf",new(1316,2400,368,40),true),
        (PrologueSectorId.CanyonDescent,"FrozenLowerResidentShelf",new(1540,2880,300,40),true),
        (PrologueSectorId.CanyonBasin,"GearResidentLeftShelf",new(2304,3792,152,24),false),
        (PrologueSectorId.CanyonBasin,"GearWestClimbWall",new(2280,3700,24,116),false),
        (PrologueSectorId.CanyonBasin,"GearWestLaunchShelf",new(1932,3746,168,40),false),
        (PrologueSectorId.CanyonBasin,"GearResidentRightShelf",new(2856,3720,136,24),false),
        (PrologueSectorId.CanyonBasin,"SteamResidentBay",new(3680,3912,200,24),false),
        (PrologueSectorId.CanyonAscent,"PendulumResidentFloor",new(4936,2520,184,40),false),
        (PrologueSectorId.CanyonAscent,"PendulumResidentRoof",new(4992,2344,128,40),false),
        (PrologueSectorId.CanyonAscent,"PendulumResidentBackWall",new(5096,2384,24,176),false)
    };

    public static void Run(string storage)
    {
        StageGalleryThroat(storage);
        foreach(var id in new[]{PrologueSectorId.CanyonDescent,PrologueSectorId.CanyonBasin,PrologueSectorId.CanyonAscent})
        {
            var baselinePacked=GD.Load<PackedScene>(BaselinePaths[id]);
            var original=baselinePacked.Instantiate<PrologueSector>();
            var target=baselinePacked.Instantiate<PrologueSector>();
            var donor=PrologueCanyonBuildParts.Build(id);
            Shape2D[] owned=Array.Empty<Shape2D>(),donorOwned=Array.Empty<Shape2D>();
            try
            {
                SteamMetadataSynchronizationTests.OwnDonorShapes(donor,out donorOwned,original,target);
                if(id==PrologueSectorId.CanyonDescent)Rejections(baselinePacked,donor);
                var before=SteamMetadataSynchronizationTests.Snapshot(original);
                target.SceneFilePath="";
                owned=ResidentTerrainSynchronizer.Apply(target,donor);
                AssertGeometry(target); // A no-op must fail expected37/actual29, not a compiler/setup failure.
                foreach(string added in NewPaths(id))donor.GetNode(added).Owner=donor;
                var sourceValues=SteamMetadataSynchronizationTests.Snapshot(donor);
                var targetValues=SteamMetadataSynchronizationTests.Snapshot(target);
                string[] newDifferences=sourceValues.Keys.Where(k=>NewPaths(id).Contains(k.Split('|')[0])&&!k.EndsWith("|$order",StringComparison.Ordinal)&&(!targetValues.TryGetValue(k,out var v)||v!=sourceValues[k])).ToArray();
                TestAssert.True(newDifferences.Length==0,"every new stored value/metadata/resource agrees with actual generator, except approved append order: "+string.Join(",",newDifferences.Take(10)));
                AssertDelta(before,SteamMetadataSynchronizationTests.Snapshot(target),id);
                TestAssert.True(ResidentTerrainSynchronizer.Apply(target,donor).Length==0&&targetValues.OrderBy(x=>x.Key).SequenceEqual(SteamMetadataSynchronizationTests.Snapshot(target).OrderBy(x=>x.Key)),"complete already-current delta is idempotent, with no new shapes or other changes");
                string path=Path.Combine(storage,"ResidentTerrain-"+id+".tscn");
                TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(target,path,out int n,out int saved,out string failure),"save exact generated delta: "+failure);
                TestAssert.Equal(n,saved,"new subtree node count survives pack");
                TestAssert.True(!File.ReadAllText(path).Contains(BaselineRoot,StringComparison.Ordinal)&&!File.ReadAllText(path).Contains(storage.Replace('\\','/'),StringComparison.OrdinalIgnoreCase),"generated normal leaf has no test-fixture or absolute staging dependency");
                using var packed=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
                var loaded=packed.Instantiate<PrologueSector>();
                try
                {
                    AssertGeometry(loaded);AssertDelta(before,SteamMetadataSynchronizationTests.Snapshot(loaded),id);
                    var paths=NewPaths(id);
                    string filtered=string.Join("\n",SteamMetadataSynchronizationTests.PersistentLinks(packed).Split('\n').Where(x=>!paths.Contains(x.Split('|')[0].StartsWith("./",StringComparison.Ordinal)?x.Split('|')[0][2..]:x.Split('|')[0])));
                    TestAssert.Equal(SteamMetadataSynchronizationTests.PersistentLinks(baselinePacked),filtered,"all old persistent groups, connections and nested scene links unchanged");
                    var unrelated=loaded.GetNode("Gameplay/Collision").GetChild(0).GetNode<CollisionShape2D>("Collision");
                    unrelated.Disabled=!unrelated.Disabled;
                    bool detected=false;try{AssertDelta(before,SteamMetadataSynchronizationTests.Snapshot(loaded),id);}catch(InvalidOperationException){detected=true;}
                    TestAssert.True(detected,"same semantic checker detects unrelated physical mutation");unrelated.Disabled=!unrelated.Disabled;
                    AssertDelta(before,SteamMetadataSynchronizationTests.Snapshot(loaded),id);
                }
                finally{loaded.Free();}
                TestAssert.True(before.OrderBy(x=>x.Key).SequenceEqual(SteamMetadataSynchronizationTests.Snapshot(original).OrderBy(x=>x.Key)),"borrowed baseline graph/resources unchanged");
                GD.Print($"RESIDENT_TERRAIN_STAGED_OK sector={id} added={Expected.Count(x=>x.Id==id)} old_semantics=true saved_reload=true persistent_links=true mutation_detected=true canonical_written=false");
            }
            finally
            {
                target.Free();original.Free();donor.Free();foreach(var shape in owned.Concat(donorOwned))shape.Dispose();
            }
        }
        StageParent(storage);
        var normal=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            var definitions=normal.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
            foreach(var definition in definitions)
            {
                var actual=definition.Scene.Instantiate<PrologueSector>();
                try{if(Expected.Any(x=>x.Id==definition.Id))AssertGeometry(actual);}finally{actual.Free();}
            }
            using var built=MinimapTerrainCatalogBuilder.Build(definitions);
            var saved=normal.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            TestAssert.True(saved.SolidCount==143&&saved.Revision==built.Revision&&saved.SerializedValues.AsSpan().SequenceEqual(built.SerializedValues),"actual normal five saved sectors and embedded catalog agree, not candidate-only");
            GD.Print("RESIDENT_TERRAIN_NORMAL_OK actual_canonical=true solids=143 exact_new_pairs=16 external_paths=5 catalog_same_physics=true normal_route=false");
        }
        finally{normal.Free();}
    }

    private static void StageGalleryThroat(string storage)
    {
        const string bodyPath="Gameplay/Collision/GearResidentRightShelf";
        const string skinPath="VisualRoot/GameplayReadable/GearResidentRightShelf";
        var allowed=new HashSet<string>(StringComparer.Ordinal)
        {
            bodyPath+"|position",bodyPath+"/Collision|shape",skinPath+"|position",
            skinPath+"|SkinSize",skinPath+"/ReadableEdge|polygon"
        };
        var source=GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonBasin));
        var original=source.Instantiate<PrologueSector>();
        var target=source.Instantiate<PrologueSector>();
        var donor=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
        Shape2D[] donorOwned=Array.Empty<Shape2D>();
        RectangleShape2D owned=null;
        try
        {
            SteamMetadataSynchronizationTests.OwnDonorShapes(donor,out donorOwned,original,target);
            var before=SteamMetadataSynchronizationTests.Snapshot(original);
            var body=target.GetNode<StaticBody2D>(bodyPath);
            var skin=target.GetNode<CanyonPlatformVisual>(skinPath);
            var donorBody=donor.GetNode<StaticBody2D>(bodyPath);
            var donorSkin=donor.GetNode<CanyonPlatformVisual>(skinPath);
            var collider=body.GetNode<CollisionShape2D>("Collision");
            var rectangle=(RectangleShape2D)collider.Shape;
            TestAssert.True((body.Position==new Vector2(1652,132) && rectangle.Size==new Vector2(152,24) ||
                body.Position==new Vector2(1644,132) && rectangle.Size==new Vector2(136,24)) &&
                skin.Position==body.Position && skin.SkinSize==rectangle.Size,
                "Gallery correction accepts only the recorded old or current shelf pair");
            TestAssert.True(donorBody.Position==new Vector2(1644,132) && donorSkin.SkinSize==new Vector2(136,24),
                "generator must preserve the left edge and heavy resident support while clearing the throat");
            target.SceneFilePath="";
            owned=(RectangleShape2D)rectangle.Duplicate();owned.ResourceSceneUniqueId="";
            owned.Size=((RectangleShape2D)donorBody.GetNode<CollisionShape2D>("Collision").Shape).Size;
            collider.Shape=owned;
            body.Position=donorBody.Position;skin.Position=donorSkin.Position;skin.SkinSize=donorSkin.SkinSize;
            skin.GetNode<Polygon2D>("ReadableEdge").Polygon=(Vector2[])donorSkin.GetNode<Polygon2D>("ReadableEdge").Polygon.Clone();
            AssertAllowed(before,SteamMetadataSynchronizationTests.Snapshot(target),allowed,"detached current Basin");
            string stagedBasin=Path.Combine(storage,"GalleryThroat-CanyonBasin.tscn");
            TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(target,stagedBasin,
                out int nodes,out int saved,out string failure),"stage current Basin correction: "+failure);
            TestAssert.Equal(nodes,saved,"Gallery correction preserves every saved node");
            using var packed=ResourceLoader.Load<PackedScene>(stagedBasin,cacheMode:ResourceLoader.CacheMode.Ignore);
            var reloaded=packed.Instantiate<PrologueSector>();
            try
            {
                AssertAllowed(before,SteamMetadataSynchronizationTests.Snapshot(reloaded),allowed,"reloaded current Basin");
                TestAssert.Equal(SteamMetadataSynchronizationTests.PersistentLinks(source),
                    SteamMetadataSynchronizationTests.PersistentLinks(packed),"Gallery correction preserves all persistent and nested links");
                TestAssert.True(reloaded.GetNode<StaticBody2D>(bodyPath).Position==new Vector2(1644,132) &&
                    ((RectangleShape2D)reloaded.GetNode<CollisionShape2D>(bodyPath+"/Collision").Shape).Size==new Vector2(136,24),
                    "saved corrected physical rectangle is real, not only an authoring-memory change");
            }
            finally { reloaded.Free(); }
            TestAssert.True(before.OrderBy(x=>x.Key).SequenceEqual(
                SteamMetadataSynchronizationTests.Snapshot(original).OrderBy(x=>x.Key)),"borrowed original Basin is unchanged");
            var parentSource=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
            var parent=parentSource.Instantiate<PrologueLevelController>();
            var definitions=new List<PrologueSectorDefinition>();
            MinimapTerrainCatalog catalog=null;
            try
            {
                var parentBefore=SteamMetadataSynchronizationTests.Snapshot(parent);
                parent.SceneFilePath="";
                foreach(var previous in parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions)
                {
                    var definition=new PrologueSectorDefinition(previous.Id,
                        previous.Id==PrologueSectorId.CanyonBasin?GD.Load<PackedScene>(stagedBasin):previous.Scene,
                        previous.WorldOffset,previous.Bounds,previous.Previous,previous.Next);
                    definition.NeighborValues.Clear();foreach(int value in previous.NeighborValues)definition.NeighborValues.Add(value);
                    foreach(Rect2 bounds in previous.KeyInteractionBounds)definition.KeyInteractionBounds.Add(bounds);
                    definitions.Add(definition);
                }
                catalog=ResidentTerrainSynchronizer.ApplyCatalog(parent,definitions);
                AssertAllowed(parentBefore,SteamMetadataSynchronizationTests.Snapshot(parent),
                    new HashSet<string>{"MinimapController|Catalog"},"current parent outside catalog");
                string stagedParent=Path.Combine(storage,"GalleryThroat-PrologueLevel.tscn");
                TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(parent,stagedParent),"stage external-reference current parent");
                using var parentPacked=ResourceLoader.Load<PackedScene>(stagedParent,cacheMode:ResourceLoader.CacheMode.Ignore);
                var parentReloaded=parentPacked.Instantiate<PrologueLevelController>();
                try
                {
                    AssertAllowed(parentBefore,SteamMetadataSynchronizationTests.Snapshot(parentReloaded),
                        new HashSet<string>{"MinimapController|Catalog"},"reloaded parent outside catalog");
                    var actual=parentReloaded.GetNode<PrologueMinimapController>("MinimapController").Catalog;
                    TestAssert.True(actual.SolidCount==143 && actual.Revision==catalog.Revision &&
                        actual.SerializedValues.AsSpan().SequenceEqual(catalog.SerializedValues),"full saved catalog/revision/boundary order matches corrected geometry");
                    TestAssert.True(parentReloaded.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions
                        .All(d=>d.Scene.ResourcePath==PrologueCanyonBuildParts.ScenePath(d.Id)),"parent retains five original external references");
                    TestAssert.Equal(SteamMetadataSynchronizationTests.PersistentLinks(parentSource),
                        SteamMetadataSynchronizationTests.PersistentLinks(parentPacked),"parent persistent/nested links unchanged");
                    GD.Print($"GALLERY_THROAT_STAGE_OK current_scene=true fields=5 solids=143 boundaries={actual.BoundaryCount} bytes={actual.SerializedValues.Length} noncatalog_equal=true canonical_written=false");
                }
                finally { parentReloaded.Free(); }
            }
            finally
            {
                parent.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;parent.Free();catalog?.Dispose();
                foreach(var definition in definitions){definition.Scene=null;definition.Dispose();}
            }
        }
        finally { target.Free();original.Free();donor.Free();owned?.Dispose();foreach(var shape in donorOwned)shape.Dispose(); }

        static void AssertAllowed(Dictionary<string,string> before,Dictionary<string,string> after,
            HashSet<string> allowed,string scope)
        {
            string[] differences=before.Keys.Union(after.Keys).Where(k=>!allowed.Contains(k) &&
                (!before.TryGetValue(k,out string a)||!after.TryGetValue(k,out string b)||a!=b)).ToArray();
            TestAssert.True(differences.Length==0,scope+" unintended delta: "+string.Join(",",differences.Take(8)));
        }
    }

    private static void StageParent(string storage)
    {
        var source=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        var target=source.Instantiate<PrologueLevelController>();
        var definitions=new List<PrologueSectorDefinition>();MinimapTerrainCatalog catalog=null;
        try
        {
            var before=SteamMetadataSynchronizationTests.Snapshot(target);target.SceneFilePath="";
            var originalDefinitions=target.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
            foreach(var old in originalDefinitions)
            {
                string candidate=Path.Combine(storage,"ResidentTerrain-"+old.Id+".tscn");
                var definition=new PrologueSectorDefinition(old.Id,GD.Load<PackedScene>(File.Exists(candidate)?candidate:PrologueCanyonBuildParts.ScenePath(old.Id)),old.WorldOffset,old.Bounds,old.Previous,old.Next);
                definition.NeighborValues.Clear();foreach(int neighbor in old.NeighborValues)definition.NeighborValues.Add(neighbor);
                foreach(Rect2 bounds in old.KeyInteractionBounds)definition.KeyInteractionBounds.Add(bounds);
                definitions.Add(definition);
            }
            catalog=ResidentTerrainSynchronizer.ApplyCatalog(target,definitions);
            var actual=target.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            TestAssert.Equal(143,actual.SolidCount,"normal parent must receive catalog from all five matching leaves");
            TestAssert.True(ReferenceEquals(catalog,actual),"caller owns the newly constructed catalog, never the old borrowed one");
            using var expected=MinimapTerrainCatalogBuilder.Build(definitions);
            TestAssert.True(actual.Revision==expected.Revision&&actual.SerializedValues.AsSpan().SequenceEqual(expected.SerializedValues),"entire physical-source catalog bytes/revision/boundary order agree");
            var after=SteamMetadataSynchronizationTests.Snapshot(target);
            AssertParentDelta(before,after);
            string path=Path.Combine(storage,"ResidentTerrain-PrologueLevel.tscn");
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(target,path),"save normal external-reference parent, never embedded candidate snapshots");
            using var packed=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);var reloaded=packed.Instantiate<PrologueLevelController>();
            try
            {
                AssertParentDelta(before,SteamMetadataSynchronizationTests.Snapshot(reloaded));
                TestAssert.Equal(SteamMetadataSynchronizationTests.PersistentLinks(source),SteamMetadataSynchronizationTests.PersistentLinks(packed),"normal parent persistent and nested links unchanged");
                var saved=reloaded.GetNode<PrologueMinimapController>("MinimapController").Catalog;
                TestAssert.True(saved.SerializedValues.AsSpan().SequenceEqual(expected.SerializedValues)&&saved.Revision==expected.Revision,"saved full catalog and revision agree");
                TestAssert.True(reloaded.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.All(d=>d.Scene.ResourcePath==PrologueCanyonBuildParts.ScenePath(d.Id)),"all five normal external scene references preserved");
                var checkpoints=reloaded.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints;
                var rebuilt=MinimapTerrainCatalogBuilder.Checkpoints(definitions);using var rebuiltOwner=(Godot.Collections.Dictionary)rebuilt;
                TestAssert.True(checkpoints.Count==3&&rebuilt.Count==3&&checkpoints.All(x=>rebuilt.TryGetValue(x.Key,out var p)&&p==x.Value)&&checkpoints["canyon.v2.start"]==new Vector2(160,600)&&checkpoints["canyon.v2.basin-camp"]==new Vector2(1680,3960)&&checkpoints["canyon.v2.temple-forecourt"]==new Vector2(5920,1080),"three original checkpoint IDs/positions match new physical leaves");
                GD.Print("RESIDENT_TERRAIN_PARENT_OK solids=143 external_paths=5 serialized_bytes_equal=true revision_equal=true boundary_order_equal=true checkpoints=3 noncatalog_equal=true saved_reload=true canonical_written=false");
            }
            finally{reloaded.Free();}
        }
        finally
        {
            target.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;target.Free();catalog?.Dispose();
            foreach(var d in definitions){d.Scene=null;d.Dispose();}
        }
        static void AssertParentDelta(Dictionary<string,string> before,Dictionary<string,string> after)
        {
            string[] differences=before.Keys.Union(after.Keys).Where(k=>k!="MinimapController|Catalog"&&(!before.TryGetValue(k,out var a)||!after.TryGetValue(k,out var b)||a!=b)).ToArray();
            TestAssert.True(differences.Length==0,"normal parent outside catalog unchanged: "+string.Join(",",differences.Take(12)));
        }
    }

    private static void Rejections(PackedScene baseline,PrologueSector donor)
    {
        Reject("null",null,donor);Reject("same_graph",donor,donor);
        Control("foreign_sector",x=>x.SectorId=PrologueSectorId.CanyonBasin);
        Control("partial_pair",x=>x.GetNode("Gameplay/Collision").AddChild(new StaticBody2D{Name="ArchiveResidentLowerFloor"}));
        var last=donor.GetNode<StaticBody2D>("Gameplay/Collision/FrozenLowerResidentShelf");
        var edge=donor.GetNode<Polygon2D>("VisualRoot/GameplayReadable/FrozenLowerResidentShelf/ReadableEdge");
        uint mask=last.CollisionMask;try{last.CollisionMask=2;Control("last_donor_layer",_=>{});}finally{last.CollisionMask=mask;}
        var polygon=edge.Polygon;try{edge.Polygon=new[]{Vector2.Zero};Control("last_donor_edge",_=>{});}finally{edge.Polygon=polygon;}
        void Control(string name,Action<PrologueSector> mutate)
        {var target=baseline.Instantiate<PrologueSector>();try{target.SceneFilePath="";mutate(target);Reject(name,target,donor);}finally{target.Free();}}
        void Reject(string name,PrologueSector target,PrologueSector source)
        {
            var before=target==null?null:SteamMetadataSynchronizationTests.Snapshot(target);bool rejected=false;
            try{var accidental=ResidentTerrainSynchronizer.Apply(target,source);foreach(var shape in accidental)shape.Dispose();}catch(ArgumentException){rejected=true;}
            TestAssert.True(rejected,"invalid complete authoring delta must reject before mutation: "+name);
            TestAssert.True(target==null||before.OrderBy(x=>x.Key).SequenceEqual(SteamMetadataSynchronizationTests.Snapshot(target).OrderBy(x=>x.Key)),"invalid authoring rejection is atomic: "+name);
            GD.Print("RESIDENT_TERRAIN_REJECT_OK case="+name+" atomic=true");
        }
    }

    private static HashSet<string> NewPaths(PrologueSectorId id) => Expected.Where(x=>x.Id==id).SelectMany(x=>new[]{"Gameplay/Collision/"+x.Name,"Gameplay/Collision/"+x.Name+"/GrappleSurfaceProfile","Gameplay/Collision/"+x.Name+"/Collision","VisualRoot/GameplayReadable/"+x.Name,"VisualRoot/GameplayReadable/"+x.Name+"/ReadableEdge"}).ToHashSet(StringComparer.Ordinal);
    private static void AssertDelta(Dictionary<string,string> before,Dictionary<string,string> after,PrologueSectorId id)
    {
        var added=NewPaths(id);
        var allowed=id==PrologueSectorId.CanyonDescent?new HashSet<string>{"Gameplay/Collision/AbyssExit2|position","Gameplay/Collision/AbyssExit2/Collision|shape","VisualRoot/GameplayReadable/AbyssExit2|position","VisualRoot/GameplayReadable/AbyssExit2|SkinSize","VisualRoot/GameplayReadable/AbyssExit2/ReadableEdge|polygon"}:new();
        string[] differences=before.Keys.Union(after.Keys).Where(k=>!added.Contains(k.Split('|')[0])&&!allowed.Contains(k)&&(!before.TryGetValue(k,out var a)||!after.TryGetValue(k,out var b)||a!=b)).ToArray();
        TestAssert.True(differences.Length==0,"outside exact16 subtrees/five Abyss fields unchanged: "+string.Join(",",differences.Take(12)));
        TestAssert.True(added.SetEquals(after.Keys.Select(k=>k.Split('|')[0]).Except(before.Keys.Select(k=>k.Split('|')[0]))),"exact added node path set, not another subtree");
    }
    private static void AssertGeometry(PrologueSector root)
    {
        int count=root.SectorId switch{PrologueSectorId.CanyonDescent=>37,PrologueSectorId.CanyonBasin=>31,_=>57};
        TestAssert.Equal(count,root.GetNode("Gameplay/Collision").GetChildCount(),"real matching normal terrain collision count");
        foreach(var e in Expected.Where(x=>x.Id==root.SectorId))
        {
            var body=root.GetNode<StaticBody2D>("Gameplay/Collision/"+e.Name);var collider=body.GetNode<CollisionShape2D>("Collision");
            var profile=body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");var visual=root.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/"+e.Name);
            var material=root.SectorId==PrologueSectorId.CanyonDescent?GrappleSurfaceMaterial.GripStone:GrappleSurfaceMaterial.TempleMetal;
            TestAssert.Equal(e.World.GetCenter()-PrologueCanyonBuildParts.WorldOffsets[root.SectorId],body.Position,"literal world center "+e.Name);
            TestAssert.True(body.CollisionLayer==33&&body.CollisionMask==0&&body.GetChildCount()==2&&visual.GetChildCount()==1&&body.Owner==root&&collider.Owner==root&&profile.Owner==root&&visual.Owner==root&&visual.GetChild(0).Owner==root,"full saved subtree/owner/layers "+e.Name);
            TestAssert.True(collider.Shape is RectangleShape2D rect&&rect.Size==e.World.Size&&!collider.Disabled&&!collider.OneWayCollision&&collider.Position==Vector2.Zero,"full native rectangle "+e.Name);
            TestAssert.True(profile.Material==material&&profile.UseCollisionHitPoint&&profile.Position==Vector2.Zero&&visual.SurfaceMaterial==material&&visual.SectorId==root.SectorId&&visual.Position==body.Position&&visual.SkinSize==e.World.Size&&visual.CatchShelf==e.Catch,"grapple/material/skin agrees "+e.Name);
            TestAssert.True(body.GetMeta(MinimapTerrainCatalogBuilder.StructuralMarker).AsString()==MinimapTerrainCatalogBuilder.Provenance&&body.HasMeta("catch_shelf")==e.Catch,"structural minimap provenance and catch metadata "+e.Name);
            var edge=visual.GetNode<Polygon2D>("ReadableEdge");float x=e.World.Size.X/2;
            TestAssert.True(edge.Polygon.SequenceEqual(new[]{new Vector2(-x,-3),new Vector2(x,-3),new Vector2(x,3),new Vector2(-x,3)})&&edge.Position==new Vector2(0,-e.World.Size.Y/2+3),"literal edge matches physical support "+e.Name);
        }
        if(root.SectorId==PrologueSectorId.CanyonDescent)
        {
            var body=root.GetNode<StaticBody2D>("Gameplay/Collision/AbyssExit2");var skin=root.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/AbyssExit2");
            TestAssert.True(body.Position==new Vector2(748,2240)&&((RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape).Size==new Vector2(216,40)&&skin.Position==body.Position&&skin.SkinSize==new Vector2(216,40),"approved AbyssExit2 width216 center748, no left-edge change");
            TestAssert.True(body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material==GrappleSurfaceMaterial.GripStone&&skin.GetNode<Polygon2D>("ReadableEdge").Polygon.All(x=>Mathf.Abs(x.X)==108),"AbyssExit2 original grip and matching edge");
        }
    }
}
