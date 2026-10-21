using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tools;
using Knight.Save;
using Knight.Tests.Unit;
using System.Text.Json;
using System.Text;
using System.Reflection;

namespace Knight.Tests.Integration;

public static class MinimapCatalogTests
{
    public static async Task Run(SceneTree tree)
    {
        RawSchemaControls();
        UnsupportedInheritance();
        var formal = ResourceLoader.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn", cacheMode: ResourceLoader.CacheMode.Ignore).Instantiate<PrologueLevelController>();
        try
        {
            var definitions = formal.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
            var built = MinimapTerrainCatalogBuilder.Build(definitions);
            var saved = formal.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            Equal(built, saved);
            TestAssert.True(built.SerializedValues.AsSpan().SequenceEqual(saved.SerializedValues), "actual saved catalog bytes remain exact after extraction validation");
            TestAssert.True(saved.SolidCount == 130 && saved.CellCount == 518400 && saved.WorldBounds == new Rect2(0,0,7680,4320),
                "PackedCatalogMatchesAuthoredShapes: independent authored count and full definitions envelope");
            int[] expectedCounts = {11,27,26,54,12};
            int cursor = 0;
            for (int sector = 0; sector < 5; sector++)
            {
                var root = definitions[sector].Scene.Instantiate<PrologueSector>();
                try
                {
                    var rows = root.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>()
                        .Where(n => n.HasMeta(MinimapTerrainCatalogBuilder.StructuralMarker))
                        .SelectMany(n => n.GetChildren().OfType<CollisionShape2D>())
                        .OrderBy(n => root.GetPathTo(n).ToString(), StringComparer.Ordinal).ToArray();
                    TestAssert.Equal(expectedCounts[sector], rows.Length, "independently counted sector rectangles");
                    foreach (var shape in rows)
                    {
                        var body = (Node2D)shape.GetParent();
                        var expected = new Transform2D(0, definitions[sector].WorldOffset) * body.Transform * shape.Transform;
                        TestAssert.True(saved.Solid(cursor).LocalToWorld == expected && saved.Solid(cursor).LocalBounds == ((RectangleShape2D)shape.Shape).GetRect(), "actual saved rectangle world placement");
                        cursor++;
                    }
                }
                finally { root.Free(); }
            }
            TestAssert.True(Enumerable.Range(0,saved.SolidCount).Any(i => saved.Solid(i).LocalToWorld.Origin == new Vector2(4232,3526) && saved.Solid(i).LocalBounds.Size == new Vector2(36,60)),
                "protruding Basin rectangle retained104 units above owning bounds");
            var descriptors = formal.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints;
            TestAssert.True(descriptors.Count == 3 && descriptors["canyon.v2.start"] == new Vector2(160,600) &&
                descriptors["canyon.v2.basin-camp"] == new Vector2(1680,3960) &&
                descriptors["canyon.v2.temple-forecourt"] == new Vector2(5920,1080), "saved actual checkpoint coordinates, not activation history");
            GD.Print($"MINIMAP_CASE_OK PackedCatalogMatchesAuthoredShapes solids={saved.SolidCount} boundaries={saved.BoundaryCount} revision={saved.Revision}");
            // Rejection before replacement must leave final bytes unchanged, even with a valid in-memory pack.
            string main = ProjectSettings.GlobalizePath("res://scenes/prologue/PrologueLevel.tscn");
            byte[] previous = File.ReadAllBytes(main);
            bool rejected = false;
            try { SceneBuildUtil.PackAndSaveValidated(formal, "res://scenes/prologue/PrologueLevel.tscn", _ => throw new InvalidOperationException("owned failing-stage control")); }
            catch (InvalidOperationException e) when (e.Message == "owned failing-stage control") { rejected = true; }
            TestAssert.True(rejected && previous.AsSpan().SequenceEqual(File.ReadAllBytes(main)), "failing saved stage never replaces previous valid main");
            TestAssert.True(!Directory.EnumerateFiles(Path.GetDirectoryName(main)!, ".task6-PrologueLevel-*.tscn").Any(), "owned staging cleanup");
            GD.Print("MINIMAP_CASE_OK FailedStagingPreservesPreviousMain");
            Malformed(saved);
        }
        finally { formal.Free(); }
        await TransformsAndExclusions(tree);
        GD.Print("MINIMAP_CATALOG_OK");
    }

    private static void UnsupportedInheritance()
    {
        var paths = IsolatedPrologueFixture.CreatePaths("unsupported-inheritance");
        var failures = new System.Collections.Generic.List<string>();
        foreach (bool control in new[]{false,true})
        {
            var root = new PrologueSector {Name="ControlSector",Bounds=new(0,0,256,256)};
            var gameplay = new Node2D {Name="Gameplay"};var collision = new Node2D {Name="Collision"};
            root.AddChild(gameplay);gameplay.AddChild(collision);
            var carrier = new Node2D {Name="Carrier",Position=new(20,30),Rotation=.25f};collision.AddChild(carrier);
            Node bridge = control ? new Control {Name="Bridge",Position=new(15,25),Rotation=.3f} : new Node {Name="Bridge"};
            carrier.AddChild(bridge);
            var body = new StaticBody2D {Name="Terrain",Position=new(60,60)};bridge.AddChild(body);
            body.SetMeta(MinimapTerrainCatalogBuilder.StructuralMarker,MinimapTerrainCatalogBuilder.Provenance);
            body.AddChild(new CollisionShape2D {Name="Shape",Shape=new RectangleShape2D {Size=new(16,16)}});
            bridge.AddChild(new CheckpointTrigger {Name="Checkpoint",EntranceId="control-checkpoint",Position=new(50,50)});
            root.AddChild(new Node2D {Name="Spawn",Position=new(16,32)});
            try
            {
                string path=Path.Combine(paths.CaseRoot,control?"control.tscn":"plain-node.tscn");
                TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"actual unsupported ancestry fixture saves");
                var packed=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
                var definition=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,packed,new(100,-40),root.Bounds,null,null);
                Reject(()=>MinimapTerrainCatalogBuilder.Build(new[]{definition}),"UnsupportedStructuralInheritanceRejected: saved " +(control?"Control":"plain Node")+" cannot silently transmit affine");
                Reject(()=>MinimapTerrainCatalogBuilder.Checkpoints(new[]{definition}),"UnsupportedCheckpointInheritanceRejected: saved checkpoint ancestor break");
                GD.Print($"MINIMAP_INHERITANCE_FIXTURE kind={(control?"Control":"plainNode")} saved_reload=True offset=100,-40 rotated_parent=True");
            }
            finally {root.Free();}
        }
        foreach(bool topLevel in new[]{true,false})
        {
            var root=new PrologueSector {Name="ControlSector",Bounds=new(0,0,256,256)};
            var spawn=new Node2D {Name="Spawn",Position=new(16,32),TopLevel=topLevel};root.AddChild(spawn);
            if(!topLevel)spawn.Transform=new Transform2D(Vector2.Zero,Vector2.Down,new Vector2(16,32));
            try
            {
                string path=Path.Combine(paths.CaseRoot,topLevel?"spawn-top-level.tscn":"spawn-invalid-transform.tscn");
                TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"actual invalid Spawn fixture saves");
                var packed=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
                var definition=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,packed,new(100,-40),root.Bounds,null,null);
                if(topLevel)Reject(()=>MinimapTerrainCatalogBuilder.Checkpoints(new[]{definition}),"UnsupportedSpawnInheritanceRejected: saved TopLevel");
                else
                {
                    var loaded=packed.Instantiate<PrologueSector>();
                    try
                    {
                        Transform2D actual=loaded.GetNode<Node2D>("Spawn").Transform;
                        GD.Print($"MINIMAP_SPAWN_ZERO_RELOAD actual={actual} valid={MinimapTerrainCatalog.Valid(actual)} saved_zero_scale_normalized=True");
                        TestAssert.True(MinimapTerrainCatalog.Valid(actual),"native saved zero-scale reload is actually a valid epsilon transform, not an invalid negative");
                        TestAssert.Equal(new Vector2(116,-8),MinimapTerrainCatalogBuilder.Checkpoints(new[]{definition})["canyon.v2.start"],"lawful native epsilon Spawn still uses original direct Position plus offset once");
                    }
                    finally {loaded.Free();}
                    var singular=new Node2D {Name="OffTreeSingularSpawn"};root.AddChild(singular);
                    singular.Transform=new Transform2D(Vector2.Zero,Vector2.Down,new Vector2(16,32));
                    TestAssert.True(singular.Transform.Determinant()==0 && !MinimapTerrainCatalog.Valid(singular.Transform),"off-tree finite singular input really is invalid before invoking actual helper");
                    bool rejected=false;
                    try {typeof(MinimapTerrainCatalogBuilder).GetMethod("ValidatePath",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{singular,root});}
                    catch(TargetInvocationException e) when(e.InnerException is ArgumentException){rejected=true;}
                    TestAssert.True(rejected,"actual private path validator rejects proven off-tree singular Spawn; not saved/reload invalid proof");
                    GD.Print($"MINIMAP_SPAWN_SINGULAR_HELPER actual={singular.Transform} rejected={rejected} off_tree=True saved_reload_invalid_proof=False");
                }
                GD.Print($"MINIMAP_SPAWN_FIXTURE top_level={topLevel} saved_reload=True");
            }
            finally {root.Free();}
        }
        TestAssert.True(failures.Count==0,"UnsupportedStructuralInheritanceRejected: " + string.Join(";",failures));
        void Reject(Action action,string name)
        {
            bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}
            GD.Print($"MINIMAP_INHERITANCE_CONTROL rejected={rejected} name={name}");
            if(!rejected)failures.Add(name);
        }
    }

    public static void RawSchemaControls()
    {
        var reader = typeof(PrologueCanyonGeneratedMainRouteTests).GetMethod("Task18RawSave", BindingFlags.NonPublic | BindingFlags.Static)!;
        PrologueSaveData Read(string json) => (PrologueSaveData)reader.Invoke(null, new object[]{Encoding.UTF8.GetBytes(json)})!;
        string core = ExplorationMapSaveTests.CoreJson.Trim();
        string Add(string property) => core[..^1] + "," + property + "}";
        ExplorationMapSaveTests.AssertCore(Read(core));
        bool accepted = true;
        try { ExplorationMapSaveTests.AssertCore(Read(Add("\"explorationMap\":" + ExplorationMapSaveTests.MapJson))); }
        catch (TargetInvocationException) { accepted = false; }
        TestAssert.True(accepted, "Task18RawOptionalMapAccepted: approved optional extension preserves independent twenty-field core");
        ExplorationMapSaveTests.AssertCore(Read(Add("\"explorationMap\":null")));
        foreach (var control in new[]{
            ("missingcore",core.Replace("\"version\":4,", "")),
            ("duplicatecore",Add("\"version\":4")),
            ("unknownextra",Add("\"unknown\":null")),
            ("duplicatemap",Add("\"explorationMap\":null,\"explorationMap\":{}")),
            ("illegalmapkind",Add("\"explorationMap\":[]"))})
        {
            bool rejected = false;
            try { Read(control.Item2); } catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException) { rejected = true; }
            TestAssert.True(rejected, "Task18 raw independent schema rejects " + control.Item1);
            GD.Print("MINIMAP_RAW_SCHEMA_REJECT " + control.Item1);
        }
        GD.Print("MINIMAP_CASE_OK Task18RawOptionalMapAccepted no_map=True object_map=True null_map=True negative_controls=5");
    }

    private static async Task TransformsAndExclusions(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("catalog-saved-transform");
        string path = Path.Combine(paths.CaseRoot,"fixture.tscn");
        var root = new PrologueSector {Name="ControlSector",Position=new(600,700), Bounds=new(-128,-128,512,512)};
        var gameplay = new Node2D{Name="Gameplay"}; var collision = new Node2D{Name="Collision"};
        root.AddChild(gameplay); gameplay.AddChild(collision);
        var a = new Node2D{Name="A",Transform=new(new Vector2(0,1),new Vector2(-1,0),new Vector2(10,20))};
        var b = new Node2D{Name="B",Transform=new(new Vector2(-1,0),new Vector2(0,1),new Vector2(3,4))};
        collision.AddChild(a); a.AddChild(b);
        var body = new StaticBody2D{Name="Terrain",Position=new(5,6),CollisionLayer=CombatCollisionLayers.World}; b.AddChild(body);
        body.SetMeta(MinimapTerrainCatalogBuilder.StructuralMarker,MinimapTerrainCatalogBuilder.Provenance);
        body.AddChild(new CollisionShape2D{Name="ShapeA",Position=new(7,8),Shape=new RectangleShape2D{Size=new(4,2)}});
        body.AddChild(new CollisionShape2D{Name="ShapeB",Shape=new RectangleShape2D{Size=new(1,20)}});
        var door = new StaticBody2D{Name="Door",Position=new(200,200),CollisionLayer=CombatCollisionLayers.World}; collision.AddChild(door);
        door.AddChild(new CollisionShape2D{Name="DoorShape",Shape=new RectangleShape2D{Size=new(20,30)}});
        var moving = new AnimatableBody2D{Name="Moving",Position=new(230,230),CollisionLayer=CombatCollisionLayers.World}; collision.AddChild(moving);
        moving.AddChild(new CollisionShape2D{Name="MovingShape",Shape=new RectangleShape2D{Size=new(20,30)}});
        try
        {
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"save actual affine fixture");
            var scene = ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
            var definition = new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,scene,new(100,-40),root.Bounds,null,null);
            var catalog = MinimapTerrainCatalogBuilder.Build(new[]{definition});
            TestAssert.Equal(2,catalog.SolidCount,"DoorsMovingBodiesExcluded: only marked structural shapes");
            // Text scenes store Node2D rotation/scale, not an exact integer basis. Independent
            // ideal90-degree coordinates are checked here; native membership below stays EXACT.
            TestAssert.Near(92,catalog.Solid(0).LocalToWorld.Origin.X,.001f,"hand-derived rotated mirrored X, root translation ignored");
            TestAssert.Near(-29,catalog.Solid(0).LocalToWorld.Origin.Y,.001f,"hand-derived rotated mirrored Y, offset once");
            TestAssert.Near(100,catalog.Solid(1).LocalToWorld.Origin.X,.001f,"second shape independent world X");
            TestAssert.Near(-22,catalog.Solid(1).LocalToWorld.Origin.Y,.001f,"second shape independent world Y");
            TestAssert.True(catalog.Solid(1).LocalBounds.Size==new Vector2(1,20),"thin-wall geometry never quantized");
            var again = MinimapTerrainCatalogBuilder.Build(new[]{definition}); Equal(catalog,again);
            var changed = scene.Instantiate<PrologueSector>();
            try
            {
                changed.Modulate=Colors.Red;
                var visualPack=new PackedScene(); TestAssert.Equal(Error.Ok,visualPack.Pack(changed),"visual-only control packs"); definition.Scene=visualPack;
                TestAssert.Equal(catalog.Revision,MinimapTerrainCatalogBuilder.Build(new[]{definition}).Revision,"DeterministicRevisionAndSegmentIds: visual-only retains revision");
                changed.GetNode<CollisionShape2D>("Gameplay/Collision/A/B/Terrain/ShapeA").Position+=new Vector2(.25f,0);
                var geometryPack=new PackedScene(); TestAssert.Equal(Error.Ok,geometryPack.Pack(changed),"geometry control packs"); definition.Scene=geometryPack;
                var altered=MinimapTerrainCatalogBuilder.Build(new[]{definition});
                TestAssert.True(catalog.Revision!=altered.Revision,"GeometryChangeInvalidatesOnlyMap: meaningful shape offset changes revision");
                var core=JsonSerializer.Deserialize<PrologueSaveData>(ExplorationMapSaveTests.CoreJson,ExplorationMapSaveTests.OldCoreOptions);
                core.ExplorationMap=new ExplorationMapModel(catalog).CaptureSave();
                string save=Path.Combine(paths.CaseRoot,"geometry-revision-save.json");PrologueSaveStore.SaveAtomic(save,core);
                string before=ExplorationMapSaveTests.Hash(save);
                var read=PrologueSaveStore.LoadOrNew(save,2,altered);
                ExplorationMapSaveTests.AssertCore(read);
                TestAssert.True(read.ExplorationMap==null && before==ExplorationMapSaveTests.Hash(save),"geometry mismatch drops ONLY optional map, preserving complete gameplay core and disk bytes");
            }
            finally {changed.Free();}
            var mounted=scene.Instantiate<PrologueSector>(); mounted.Position=definition.WorldOffset;
            tree.Root.AddChild(mounted); await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
            try
            {
                using var sampler=new HeadlampOcclusionSampler(mounted); var frame=sampler.Capture();
                TestAssert.True(frame.IsComplete && frame.Shapes.Count==4,"actual native frame includes unmarked door and moving blocker");
                for(int i=0;i<2;i++)
                {
                    var s=catalog.Solid(i);
                    TestAssert.True(frame.Shapes.Any(row=>row.WorldToLocal==s.LocalToWorld.AffineInverse() && row.LocalBounds==s.LocalBounds),"saved/mounted exact native fog membership");
                }
                TestAssert.True(!MinimapVisibility.BoundaryAllowed(new Vector2(92,-16),catalog.Solid(0),catalog.Boundary(0),frame),"second thin shape actually blocks farther front-face wedge");
                TestAssert.True(MinimapVisibility.BoundaryAllowed(new Vector2(92,-25),catalog.Solid(0),catalog.Boundary(0),frame),"real fog frame authorizes unobstructed actual front face only");
            }
            finally {mounted.Free();}
            GD.Print("MINIMAP_CASE_OK DoorsMovingBodiesExcluded native_blockers=4 structural=2");
            GD.Print("MINIMAP_CASE_OK DeterministicRevisionAndSegmentIds");
            GD.Print("MINIMAP_CASE_OK GeometryChangeInvalidatesOnlyMap");
        }
        finally {root.Free();}
    }

    internal static void Equal(MinimapTerrainCatalog a,MinimapTerrainCatalog b)
    {
        TestAssert.True(a.Revision==b.Revision && a.WorldBounds==b.WorldBounds && a.SolidCount==b.SolidCount && a.BoundaryCount==b.BoundaryCount,"serialized catalog counts/bounds/revision");
        for(int i=0;i<a.SolidCount;i++)TestAssert.True(a.Solid(i)==b.Solid(i),"serialized exact affine/local rect order");
        for(int i=0;i<a.BoundaryCount;i++)TestAssert.True(a.Boundary(i)==b.Boundary(i),"serialized exact stable segment order");
    }
    private static void Malformed(MinimapTerrainCatalog catalog)
    {
        foreach(int at in new[]{0,20,24,28})
        {
            byte[] values=catalog.SerializedValues; Array.Fill(values,(byte)255,at,4);
            var invalid=new MinimapTerrainCatalog(); bool rejected=false;
            try {invalid.SerializedValues=values;}catch(ArgumentException){rejected=true;}
            TestAssert.True(rejected && invalid.CellCount==0,"malformed/over-capacity serialization remains unconfigured");
        }
        byte[] exported=catalog.SerializedValues;exported[0]=0;
        TestAssert.True(catalog.SerializedValues[0]==1,"export cannot mutate frozen catalog");
        GD.Print("MINIMAP_CASE_OK MalformedOverCapacityFailsClosed");
    }
}
