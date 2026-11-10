using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Knight.Presentation.Minimap;
using Knight.Presentation.Fog;
using Knight.Tests.Unit;
using System.Reflection;
using Knight.Build.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tools;
using Knight.Save;
using System.Text.Json;
using System.Linq;

namespace Knight.Tests.Integration;

public static class MinimapVersionedGridTests
{
    public static async Task Run(SceneTree tree, string storage)
    {
        CrossGridSaveIdentity(storage);
        // Break caught: a large-map payload is rejected or silently interpreted
        // as old grid8, making the final row wrap/drop instead of remain distinct.
        using var catalog = new MinimapTerrainCatalog();
        byte[] payload = Payload(2, 16, new Rect2(0, 0, 16384, 12288), "grid-v2");
        bool accepted = false;
        try { catalog.SerializedValues = payload; accepted = true; }
        catch (ArgumentException) { }
        TestAssert.True(accepted,
            "MINIMAP_GRID_V2_RED: an explicit grid16 saved catalog must represent the expanded world under the unchanged bit cap");
        TestAssert.True(catalog.Columns == 1024 && catalog.Rows == 768 && catalog.CellCount == 786432,
            "large-map independent literal dimensions use16, not old8 or an increased cap");
        TestAssert.Equal(new Rect2(16368, 12272, 16, 16), catalog.Cell(786431),
            "large-map final cell preserves exact world coordinates and cannot alias zero");
        TestAssert.True(payload.AsSpan().SequenceEqual(catalog.SerializedValues),
            "saved explicit payload survives publication without rewriting");
        GD.Print("MINIMAP_GRID_V2_OK columns=1024 rows=768 cells=786432 last=16368,12272,16,16");
        LargeWorldExploration(catalog);
        SchemaAndLegacyControls();
        VisibilityAndScheduling();
        ExplorationMapModelTests.Run();
        MinimapVisibilityTests.Run();
        await Hud(tree);
        SavedBuilder(storage);
        Persistence(storage);
        ExplorationMapSaveTests.Run();
        GD.Print($"MINIMAP_GRID_WARM_OK writer_pid={System.Environment.ProcessId} witnesses=3 saved_catalogs=8,16 core_preserved=true source_readonly=true pixels=false human=false");
    }

    private static void CrossGridSaveIdentity(string storage)
    {
        // I1: equal revision and one byte in both grids must never reinterpret
        // knowledge of one8x8 cell as knowledge of the entire16x16 region.
        using var old=new MinimapTerrainCatalog(new Rect2(0,0,16,16),"terrain");
        var prior=new ExplorationMapModel(old);
        prior.Restore(new ExplorationMapSaveData {TerrainRevision="terrain",FreeCellsBase64="AQ==",BoundarySegmentsBase64=""});
        TestAssert.True(prior.IsCellExplored(0) && !prior.IsCellExplored(1) && !prior.IsCellExplored(2) && !prior.IsCellExplored(3),
            "counterexample starts with legitimate literal partial old8 memory, not full exploration");
        var oldSave=prior.CaptureSave();
        var oldCore=JsonSerializer.Deserialize<PrologueSaveData>(ExplorationMapSaveTests.CoreJson,ExplorationMapSaveTests.OldCoreOptions);
        oldCore.ExplorationMap=oldSave;
        string oldPath=Path.Combine(storage,"identity-grid8.json");PrologueSaveStore.SaveAtomic(oldPath,oldCore);
        string oldHash=ExplorationMapSaveTests.Hash(oldPath);
        bool allRejected=true;
        foreach(bool raw in new[]{false,true})
        {
            using var next=raw?new MinimapTerrainCatalog():new MinimapTerrainCatalog(old.WorldBounds,"terrain",cellSize:16);
            if(raw)next.SerializedValues=Payload(2,16,old.WorldBounds,"terrain");
            var model=new ExplorationMapModel(next);model.Restore(oldSave);
            bool modelRejected=!model.IsCellExplored(0);
            var fromOld=PrologueSaveStore.LoadOrNew(oldPath,2,next);ExplorationMapSaveTests.AssertCore(fromOld);
            bool storeRejected=fromOld.ExplorationMap==null;
            model.Clear();model.Advance(new(Vector2.Zero,1,1,true,
                new HeadlampOcclusionFrame(Array.Empty<HeadlampOccluder>(),true),new[]{next.WorldBounds}));
            TestAssert.True(model.IsCellExplored(0),"reverse case starts with actual valid16-world model knowledge");
            var nextSave=model.CaptureSave();var reverse=new ExplorationMapModel(old);reverse.Restore(nextSave);
            bool reverseModelRejected=!reverse.IsCellExplored(0);
            var nextCore=JsonSerializer.Deserialize<PrologueSaveData>(ExplorationMapSaveTests.CoreJson,ExplorationMapSaveTests.OldCoreOptions);
            nextCore.ExplorationMap=nextSave;
            string nextPath=Path.Combine(storage,raw?"identity-grid16-raw.json":"identity-grid16-constructor.json");
            PrologueSaveStore.SaveAtomic(nextPath,nextCore);string nextHash=ExplorationMapSaveTests.Hash(nextPath);
            var fromNew=PrologueSaveStore.LoadOrNew(nextPath,2,old);ExplorationMapSaveTests.AssertCore(fromNew);
            bool reverseStoreRejected=fromNew.ExplorationMap==null;
            TestAssert.Equal(oldHash,ExplorationMapSaveTests.Hash(oldPath),"old same-revision source remains read-only");
            TestAssert.Equal(nextHash,ExplorationMapSaveTests.Hash(nextPath),"new same-revision source remains read-only");
            allRejected &= modelRejected && storeRejected && reverseModelRejected && reverseStoreRejected;
            GD.Print($"MINIMAP_GRID_IDENTITY_CONTROL raw_schema={raw} equal_revision=true equal_byte_count=true old_to_new_model_rejected={modelRejected} old_to_new_store_rejected={storeRejected} new_to_old_model_rejected={reverseModelRejected} new_to_old_store_rejected={reverseStoreRejected} core_preserved=true");
        }
        TestAssert.True(allRejected,"MINIMAP_GRID_IDENTITY_RED: same-revision same-byte-count maps must reject cross-grid memory in model and real store in both directions");
    }

    private static void SavedBuilder(string storage)
    {
        // Break caught: requesting grid16 still builds/fingerprints old8, or
        // coarsens physical geometry/segments instead of only free-space cells.
        var root=new PrologueSector {Name="GridFixture",SectorId=PrologueSectorId.CanyonEntrance,Bounds=new(0,0,512,512)};
        using var shape=new RectangleShape2D {Size=new(32,128)};
        try
        {
            var gameplay=new Node2D {Name="Gameplay"};root.AddChild(gameplay);
            var collision=new Node2D {Name="Collision"};gameplay.AddChild(collision);
            var body=new StaticBody2D {Name="Wall",Position=new(224,256)};collision.AddChild(body);
            body.SetMeta(MinimapTerrainCatalogBuilder.StructuralMarker,MinimapTerrainCatalogBuilder.Provenance);
            body.AddChild(new CollisionShape2D {Name="Collision",Shape=shape});
            string path=Path.Combine(storage,"grid-sector.tscn");
            TestAssert.True(!File.Exists(path),"saved-grid fixture never overwrites a previous witness");
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"actual structural fixture packs/saves/reinstantiates");
            using var packed=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
            using var definition=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,packed,new(64,-32),root.Bounds,null,null);
            using var old=MinimapTerrainCatalogBuilder.Build(new[]{definition});
            GD.Print($"MINIMAP_GRID_BUILDER_OLD revision={old.Revision} sha={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(old.SerializedValues))}");
            TestAssert.Equal("78402CFF1F3BF152A0EDC14B7CCDFA63EA9AE0D6F4B102D4FBD91ECEB3BA5F6F",old.Revision,
                "old builder revision remains exact to unchanged-body213513 witness");
            TestAssert.Equal("488F68FA2FA808C40D3C7E8E2C7528806D695893ACC8F8F16F9A78821ABC27B6",
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(old.SerializedValues)),"old saved catalog payload stays byte-exact");
            using var large=MinimapTerrainCatalogBuilder.Build(new[]{definition},16);
            TestAssert.True(large.CellSize==16 && large.CellCount==1024 && old.CellSize==8 && old.CellCount==4096,
                "MINIMAP_GRID_BUILDER_RED: real saved-sector extraction must honor selected16 grid, not silently emit old8");
            TestAssert.True(old.Revision!=large.Revision,"same geometry with different cell addressing has a different save revision");
            TestAssert.True(large.SolidCount==1 && large.BoundaryCount==44 && old.BoundaryCount==44,
                "32x128 physical rectangle retains44 <=8-world boundary pieces independently of free-cell grid");
            TestAssert.Equal(new Rect2(-16,-64,32,128),large.Solid(0).LocalBounds,"physical32-thick wall bounds retained exactly");
            TestAssert.Equal(new Transform2D(0,new Vector2(288,224)),large.Solid(0).LocalToWorld,"actual saved shape offset applied once");
            for(int id=0;id<44;id++)TestAssert.Equal(old.Boundary(id),large.Boundary(id),"new grid cannot reorder/coarsen physical boundary "+id);
            string catalogPath=Path.Combine(storage,"grid16.tres");
            TestAssert.Equal(Error.Ok,ResourceSaver.Save(old,Path.Combine(storage,"grid8.tres")),"actual old native catalog witness saves without changing payload");
            TestAssert.Equal(Error.Ok,ResourceSaver.Save(large,catalogPath),"actual native resource saves schema2 grid16");
            using var saved=ResourceLoader.Load<MinimapTerrainCatalog>(catalogPath,cacheMode:ResourceLoader.CacheMode.Ignore);
            TestAssert.True(saved.CellSize==16 && saved.Columns==32 && saved.Rows==32 && saved.CellCount==1024 &&
                large.SerializedValues.AsSpan().SequenceEqual(saved.SerializedValues),"native save/reload preserves grid and payload bytes");
            TestAssert.Equal(new Rect2(560,464,16,16),saved.Cell(1023),"reloaded offset final cell maps to actual world");
            using var current=ExplorationMapSaveTests.CurrentRevisionCatalog();
            TestAssert.Equal("D6EC6A12A6D3CF6BDB0C1A9C981565A6EC43D901BB8175C987DFE0A19ADC61CE",
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(current.SerializedValues)),"actual current parent embedded old8 catalog remains exact");
            bool rejected=false;
            try { using var invalid=MinimapTerrainCatalogBuilder.Build(new[]{definition},32); }
            catch(ArgumentException) {rejected=true;}
            TestAssert.True(rejected,"unsupported builder grid cannot silently select a different addressing scheme");
            GD.Print("MINIMAP_GRID_BUILDER_OK saved_reload=true grids=8,16 solids=1 edges=44 physical_wall=32,128 current_old8_exact=true");
        }
        finally {root.Free();}
    }

    private static void Persistence(string storage)
    {
        // Break caught: new cell IDs or grid-specific revision are discarded,
        // cross-loaded as old8 memory, or cause valid core progress to reset.
        using var catalog=ResourceLoader.Load<MinimapTerrainCatalog>(Path.Combine(storage,"grid16.tres"),cacheMode:ResourceLoader.CacheMode.Ignore);
        var model=new ExplorationMapModel(catalog);Drain(model,new Vector2(96,0),new[]{catalog.WorldBounds});
        TestAssert.True(model.IsCellExplored(66) && !model.IsCellExplored(1023),"warm grid16 actual model reveals literalnear66, not far1023");
        var core=JsonSerializer.Deserialize<PrologueSaveData>(ExplorationMapSaveTests.CoreJson,ExplorationMapSaveTests.OldCoreOptions);
        core.ExplorationMap=model.CaptureSave();
        string path=Path.Combine(storage,"grid16-progress.json");
        TestAssert.True(!File.Exists(path),"new grid save uses a fresh external case slot");
        PrologueSaveStore.SaveAtomic(path,core);
        string before=ExplorationMapSaveTests.Hash(path);
        AssertRead(path,catalog);
        using var old=ResourceLoader.Load<MinimapTerrainCatalog>(Path.Combine(storage,"grid8.tres"),cacheMode:ResourceLoader.CacheMode.Ignore);
        var mismatched=PrologueSaveStore.LoadOrNew(path,2,old);ExplorationMapSaveTests.AssertCore(mismatched);
        TestAssert.True(mismatched.ExplorationMap==null,"real saved old8 catalog rejects new16 optional bits, preserving every core field");
        TestAssert.Equal(before,ExplorationMapSaveTests.Hash(path),"both ordinary and mismatched reads leave source bytes unchanged");
        string[] names={"grid8.tres","grid16.tres","grid16-progress.json"};
        File.WriteAllLines(Path.Combine(storage,"grid-witnesses.sha256"),names.Select(name=>name+"|"+ExplorationMapSaveTests.Hash(Path.Combine(storage,name))));
        File.WriteAllText(Path.Combine(storage,"grid-writer-pid.txt"),System.Environment.ProcessId.ToString());
    }

    private static void AssertRead(string path,MinimapTerrainCatalog catalog)
    {
        var read=PrologueSaveStore.LoadOrNew(path,2,catalog);ExplorationMapSaveTests.AssertCore(read);
        TestAssert.True(read.ExplorationMap!=null,"actual store restores compatible new-grid optional memory");
        var restored=new ExplorationMapModel(catalog);restored.Restore(read.ExplorationMap);
        TestAssert.True(restored.IsCellExplored(66) && !restored.IsCellExplored(1023),"stored16-world near/far IDs remain distinct");
    }

    public static void RunCold(string storage)
    {
        int writer=int.Parse(File.ReadAllText(Path.Combine(storage,"grid-writer-pid.txt")));
        TestAssert.True(writer>0 && writer!=System.Environment.ProcessId,"new-grid cold reader is a distinct actual process");
        string[] names={"grid8.tres","grid16.tres","grid16-progress.json"};
        string[] manifest=File.ReadAllLines(Path.Combine(storage,"grid-witnesses.sha256"));
        TestAssert.True(manifest.Length==3,"new-grid cold source manifest requires all three exact witnesses");
        for(int i=0;i<3;i++)TestAssert.Equal(names[i]+"|"+ExplorationMapSaveTests.Hash(Path.Combine(storage,names[i])),manifest[i],"cold pre-read witness byte identity");
        using var catalog=ResourceLoader.Load<MinimapTerrainCatalog>(Path.Combine(storage,"grid16.tres"),cacheMode:ResourceLoader.CacheMode.Ignore);
        using var old=ResourceLoader.Load<MinimapTerrainCatalog>(Path.Combine(storage,"grid8.tres"),cacheMode:ResourceLoader.CacheMode.Ignore);
        TestAssert.True(catalog.CellSize==16 && catalog.CellCount==1024 && catalog.BoundaryCount==44 && old.CellSize==8 && old.CellCount==4096,
            "cold actual native reload preserves both serialized schemas and fine boundaries");
        string path=Path.Combine(storage,"grid16-progress.json");AssertRead(path,catalog);
        var mismatched=PrologueSaveStore.LoadOrNew(path,2,old);ExplorationMapSaveTests.AssertCore(mismatched);
        TestAssert.True(mismatched.ExplorationMap==null,"cold wrong-grid read discards only optional map");
        ExplorationMapColdReadTests.Run();
        for(int i=0;i<3;i++)TestAssert.Equal(manifest[i],names[i]+"|"+ExplorationMapSaveTests.Hash(Path.Combine(storage,names[i])),"cold post-read source remains exact");
        GD.Print($"MINIMAP_GRID_COLD_OK writer_pid={writer} reader_pid={System.Environment.ProcessId} witnesses=3 saved_catalogs=8,16 core_preserved=true source_readonly=true pixels=false human=false");
    }

    private static async Task Hud(SceneTree tree)
    {
        // Break caught: new map cells are sampled with old8-world cache/shader
        // coordinates, or checkpoint discovery uses a different cell than the mask.
        using var catalog=new MinimapTerrainCatalog(new Rect2(-4096,-4096,8192,8192),"hud-grid16",cellSize:16);
        var model=new ExplorationMapModel(catalog);
        var actor=new Vector2(160,160);Drain(model,actor,new[]{catalog.WorldBounds});
        ulong revision=model.RevisionCounter;
        var view=new PrologueMinimapView {Name="VersionedGridHud",Size=PrologueMinimapView.BaseSize};
        view.ConfigureCheckpoints(new[]{new MinimapCheckpointDescriptor("known",new Vector2(168,168)),
            new MinimapCheckpointDescriptor("unknown",new Vector2(320,160))});
        tree.Root.AddChild(view);
        try
        {
            view.Present(model,actor,Vector2.Right,0);
            TestAssert.Equal(new Vector2(4096,3072),view.CacheWorldRect.Size,
                "MINIMAP_GRID_HUD_RED: actual cache world extent must match16-world cells, not old8");
            TestAssert.Equal(new Vector2(-1888,-1376),view.CacheWorldRect.Position,
                "grid16 cache aligns to actual catalog lattice at negative world origin");
            TestAssert.Equal(65536,view.StaticDirectoryBytes,"boundary directory keeps64-world buckets across8192-world extent");
            var pixels=(byte[])typeof(PrologueMinimapView).GetField("_pixels",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view);
            TestAssert.True(pixels[96*256+128]==255 && pixels[0]==0,
                "actual R8 cache has known actor cell and untouched distant cell, no whole-map fallback");
            var material=(ShaderMaterial)typeof(PrologueMinimapView).GetField("_maskMaterial",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view);
            using(var value=material.GetShaderParameter("cell_size"))
                TestAssert.Near(16,(float)value.AsDouble(),0,"shader sampling uses selected catalog cell size");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(1,view.DiscoveredCheckpointCount,"actual draw discovers known16-world checkpoint, not remote unknown checkpoint");
            int rebuilt=view.CacheRebuildCount,uploaded=view.TextureUploadCount,created=view.TextureCreationCount;
            for(int i=0;i<120;i++)view.Present(model,actor,Vector2.Right,1d/60);
            TestAssert.True(view.CacheRebuildCount==rebuilt && view.TextureUploadCount==uploaded && view.TextureCreationCount==created && model.RevisionCounter==revision,
                "idle presentation neither uploads/recreates nor explores on grid16");
            using var old=new MinimapTerrainCatalog(catalog.WorldBounds,"hud-grid8");
            var oldModel=new ExplorationMapModel(old);Drain(oldModel,actor,new[]{old.WorldBounds});
            view.Present(oldModel,actor,Vector2.Right,0);
            TestAssert.Equal(new Vector2(2048,1536),view.CacheWorldRect.Size,"switching back restores old8-world cache rather than stale16 sampling");
            using(var value=material.GetShaderParameter("cell_size"))
                TestAssert.Near(8,(float)value.AsDouble(),0,"catalog switch updates actual shader uniform back to8");
            TestAssert.Equal(created,view.TextureCreationCount,"catalog switch reuses view-owned texture");
            GD.Print("MINIMAP_GRID_HUD_OK grid16_cache=4096,3072 grid8_cache=2048,1536 buckets_world=64 checkpoint=1 uniform_switch=16,8 idle=120 pixels=false human=false");
        }
        finally {view.Free();}
    }

    private static void LargeWorldExploration(MinimapTerrainCatalog catalog)
    {
        var model = new ExplorationMapModel(catalog);
        Drain(model, new Vector2(16360, 12264), new[] { catalog.WorldBounds });
        TestAssert.True(model.IsCellExplored(786431) && !model.IsCellExplored(0),
            "MINIMAP_GRID_MODEL_RED: real model and scheduler reveal the large-world last cell without wrapping or rejecting an in-bounds actor");
        TestAssert.Equal(98304, model.BitsetBytes, "large map fits96KiB free bits without raising1M cap");
        var save = model.CaptureSave(); model.Clear(); model.Restore(save);
        TestAssert.True(model.IsCellExplored(786431) && !model.IsCellExplored(0), "last-cell memory restores independently of cell zero");
    }

    private static void SchemaAndLegacyControls()
    {
        byte[] old = Payload(1, 8, new Rect2(-8, -16, 32, 32), "old-grid");
        using var legacy = new MinimapTerrainCatalog(); legacy.SerializedValues = old;
        using var constructed = new MinimapTerrainCatalog(new Rect2(-8, -16, 32, 32), "old-grid");
        TestAssert.True(legacy.CellSize == 8 && legacy.CellCount == 16 &&
            old.AsSpan().SequenceEqual(constructed.SerializedValues), "old default constructor retains literal schema1 wire exactly");
        TestAssert.Equal(new Rect2(16, 8, 8, 8), legacy.Cell(15), "old negative-origin final cell unchanged");
        using var historical = HistoricalMinimapCatalogFixture.Baffle();
        TestAssert.Equal("CD66FCE70550F1D5414634B3380D2E73A98ADA2963F94EC104976AE6D0DE3566",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(historical.SerializedValues)),
            "retained historical grid8 payload attestation is exact, not rewritten as schema2");
        foreach (byte[] bad in new[] {
            Payload(2, 8, new Rect2(0,0,32,32), "bad"),
            Payload(2, 0, new Rect2(0,0,32,32), "bad"),
            Payload(2, 32, new Rect2(0,0,32,32), "bad"),
            Payload(3, 16, new Rect2(0,0,32,32), "bad"),
            Payload(2, 16, new Rect2(0,0,33,32), "bad"),
            Payload(2, 16, new Rect2(0,0,16384,16400), "bad"),
            Payload(2, 16, new Rect2(0,0,float.NaN,32), "bad"),
            Payload(2, 16, new Rect2(0,0,32,32), "bad")[..^1] })
        {
            using var target = new MinimapTerrainCatalog(); bool rejected=false;
            try { target.SerializedValues=bad; }
            catch(Exception error) when(error is ArgumentException or EndOfStreamException) { rejected=true; }
            TestAssert.True(rejected && target.CellCount==0 && target.SerializedValues.Length==0,
                "invalid schema/grid/extent/count cannot publish partial catalog state");
            target.SerializedValues=old;
            TestAssert.Equal(new Rect2(16,8,8,8),target.Cell(15),"rejected payload does not freeze resource or poison later valid load");
        }
        GD.Print("MINIMAP_GRID_SCHEMA_OK legacy_exact=true malformed=8 atomic=true cap_unchanged=true");
    }

    private static void VisibilityAndScheduling()
    {
        using var catalog = new MinimapTerrainCatalog(new Rect2(-256,-256,512,512),"grid16-visibility",cellSize:16);
        var model = new ExplorationMapModel(catalog);
        Drain(model,Vector2.Zero,new[]{catalog.WorldBounds});
        TestAssert.True(model.IsCellExplored(16*32+24) && !model.IsCellExplored(16*32+25),
            "grid16 whole patch128..144 within160 reveals;144..160 far corner outside160 remains black");
        model.Clear();
        Drain(model,Vector2.Zero,new[]{new Rect2(-16,-32,32,96),new Rect2(32,-32,96,96)});
        TestAssert.True(!model.IsCellExplored(16*32+18),"grid16 loaded endpoints cannot paint across unloaded16-world seam");
        var wall=new HeadlampOcclusionFrame(new[]{new HeadlampOccluder(Transform2D.Identity,new Rect2(32,-128,32,256))},true);
        model.Clear();Drain(model,Vector2.Zero,new[]{catalog.WorldBounds},wall);
        TestAssert.True(model.IsCellExplored(16*32+16) && !model.IsCellExplored(16*32+20),
            "32-thick physical wall blocks far cell without widening/coarsening geometry");
        foreach(int gap in new[]{32,48,64}) foreach(int offset in new[]{0,5})
        {
            var left = new MinimapSolid(Transform2D.Identity,new Rect2(-128+offset,-128,128,256));
            var right = new MinimapSolid(Transform2D.Identity,new Rect2(gap+offset,-128,128,256));
            using var corridor = new MinimapTerrainCatalog(new Rect2(-256,-256,512,512),"corridor",
                new[]{left,right},new[]{new MinimapBoundary(0,new(offset,-8),new(offset,0),Vector2.Right),
                    new MinimapBoundary(1,new(gap+offset,-8),new(gap+offset,0),Vector2.Left)},16);
            var frame=new HeadlampOcclusionFrame(new[]{new HeadlampOccluder(Transform2D.Identity,left.LocalBounds),
                new HeadlampOccluder(Transform2D.Identity,right.LocalBounds)},true);
            var narrow=new ExplorationMapModel(corridor);
            Drain(narrow,new Vector2(offset+gap*.5f,0),new[]{corridor.WorldBounds},frame);
            TestAssert.True(narrow.IsBoundaryExplored(0) && narrow.IsBoundaryExplored(1),
                "fine8-world outlines survive real32/48/64 corridor and non-grid offset");
            for(int id=0;id<corridor.CellCount;id++) if(narrow.IsCellExplored(id))
            {
                Rect2 cell=corridor.Cell(id);
                TestAssert.True(cell.Position.X>offset && cell.End.X<offset+gap,
                    "narrow corridor never reveals a whole cell touching/crossing either wall");
            }
        }
        var denseSolid=new MinimapSolid(Transform2D.Identity,new Rect2(-8,-8,1,8));
        var edges=new MinimapBoundary[8192];
        Array.Fill(edges,new MinimapBoundary(0,new(-7,-8),new(-7,0),Vector2.Right));
        using var dense=new MinimapTerrainCatalog(catalog.WorldBounds,"dense16",new[]{denseSolid},edges,16);
        var near=new ExplorationMapModel(dense);
        near.Advance(new(new Vector2(7.5f,7.5f),1,1,true,
            new HeadlampOcclusionFrame(new[]{new HeadlampOccluder(Transform2D.Identity,denseSolid.LocalBounds)},true),new[]{dense.WorldBounds}));
        TestAssert.True(near.IsCellExplored(528),"dense boundaries cannot starve nearer actual grid16 free cell");
        TestAssert.True(near.LastCandidateCount<=64 && near.LastSchedulerNodeExpansions<=256 && near.LastSchedulerQueryRebuilds<=2,
            "grid16 dense case retains original per-stamp work ceilings");
        GD.Print("MINIMAP_GRID_MODEL_OK radius=160 thick_wall=32 gaps=32,48,64 offsets=0,5 loaded_gap=true dense=8192 budgets=64,256,2 pixels=false");
    }

    private static void Drain(ExplorationMapModel model,Vector2 actor,Rect2[] loaded,HeadlampOcclusionFrame frame=null)
    {
        frame ??= new HeadlampOcclusionFrame(Array.Empty<HeadlampOccluder>(),true);
        for(ulong stamp=1;stamp<=64;stamp++)
        {
            model.Advance(new(actor,stamp,1,true,frame,loaded));
            TestAssert.True(model.LastCandidateCount<=64 && model.LastSchedulerNodeExpansions<=256 && model.LastSchedulerQueryRebuilds<=2,
                "large-grid model never buys wider coverage with increased per-stamp work");
        }
    }

    private static byte[] Payload(int schema, int cellSize, Rect2 bounds, string revision)
    {
        // Independent wire fixture, not constructor/serializer output reused as expected.
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        byte[] text = Encoding.UTF8.GetBytes(revision);
        writer.Write(schema);
        if (schema == 2) writer.Write(cellSize);
        writer.Write(bounds.Position.X); writer.Write(bounds.Position.Y);
        writer.Write(bounds.Size.X); writer.Write(bounds.Size.Y);
        writer.Write(text.Length); writer.Write(0); writer.Write(0); writer.Write(text);
        return stream.ToArray();
    }
}
