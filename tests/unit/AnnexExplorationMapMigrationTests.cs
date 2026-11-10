using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Knight.Presentation.Minimap;
using Knight.Save;
using Knight.Build.Prologue;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;
using Environment = System.Environment;

namespace Knight.Tests.Unit;

internal static class AnnexExplorationMapMigrationTests
{
    private const string PriorRevision="D8581BF1A4012D09B85953DDF863B607273AE569EBA7B226D6989098DCA09A2D";
    private const string CurrentRevision="5A99D13578D8271BD99AA01678884F615A681958281D8A621CBA2D27F86581B4";
    private const string CurrentSha="D6EC6A12A6D3CF6BDB0C1A9C981565A6EC43D901BB8175C987DFE0A19ADC61CE";

    internal static void Run(string root)
    {
        using var target=ExplorationMapSaveTests.CurrentRevisionCatalog();
        AssertTarget(target);
        AssertPhysicalDelta(target);
        AssertCurrentFactoryAudits();
        foreach(string kind in new[]{"sparse","zero","full"})
        {
            var (cells,edges)=PriorBits(kind);
            string path=Path.Combine(root,"map-annex-source-"+kind+".json");
            WriteFresh(path,Wire(cells,edges));
            string hash=ExplorationMapSaveTests.Hash(path);
            var loaded=PrologueSaveStore.LoadOrNew(path,2,target);
            ExplorationMapSaveTests.AssertCore(loaded);
            TestAssert.True(loaded.ExplorationMap!=null,
                "ANNEX_MAP_MIGRATION_RED: unchanged old exploration must survive actual144-to158 loading");
            AssertBits(loaded.ExplorationMap,target,kind);
            TestAssert.Equal(hash,ExplorationMapSaveTests.Hash(path),"annex migration never rewrites source "+kind);
            File.WriteAllText(path+".sha256",hash);
            string saved=Path.Combine(root,"map-annex-resaved-"+kind+".json");
            TestAssert.True(!File.Exists(saved),"annex resave uses a fresh isolated slot");
            PrologueSaveStore.SaveAtomic(saved,loaded);
            File.WriteAllText(saved+".sha256",ExplorationMapSaveTests.Hash(saved));
            TestAssert.Equal(hash,ExplorationMapSaveTests.Hash(path),"resaving leaves old annex source untouched");
        }
        int negatives=Controls(root,target);
        File.WriteAllText(Path.Combine(root,"map-annex-writer-pid.txt"),Environment.ProcessId.ToString());
        GD.Print($"ANNEX_MAP_MIGRATION_OK current_target=true old_solids=144 new_solids=158 old_edges=12468 new_edges=13908 sparse=true zero=true full=true new_room_dark=true new_edges_dark=1440 core_preserved=true source_read_only=true negatives={negatives}");
    }

    internal static void ColdRead(string root)
    {
        int writer=int.Parse(File.ReadAllText(Path.Combine(root,"map-annex-writer-pid.txt")));
        TestAssert.True(writer!=Environment.ProcessId,"annex cold reader must be a distinct process");
        using var target=ExplorationMapSaveTests.CurrentRevisionCatalog();
        AssertTarget(target);
        foreach(string kind in new[]{"sparse","zero","full"})
        foreach(string stem in new[]{"source","resaved"})
        {
            string path=Path.Combine(root,"map-annex-"+stem+"-"+kind+".json");
            string hash=ExplorationMapSaveTests.Hash(path);
            TestAssert.Equal(File.ReadAllText(path+".sha256"),hash,"cold reader receives exact retained annex wire");
            using(var wire=JsonDocument.Parse(File.ReadAllText(path)))
                TestAssert.Equal(stem=="source"?PriorRevision:CurrentRevision,
                    wire.RootElement.GetProperty("explorationMap").GetProperty("terrainRevision").GetString(),
                    "cold differentiates old source from converted current save");
            var loaded=PrologueSaveStore.LoadOrNew(path,2,target);
            ExplorationMapSaveTests.AssertCore(loaded);
            TestAssert.True(loaded.ExplorationMap!=null,"cold current annex migration remains available");
            AssertBits(loaded.ExplorationMap,target,kind);
            TestAssert.Equal(hash,ExplorationMapSaveTests.Hash(path),"annex cold reader is read-only");
        }
        GD.Print($"ANNEX_MAP_COLD_OK writer_pid={writer} reader_pid={Environment.ProcessId} witnesses=6 old_and_resaved=true source_read_only=true new_room_dark=true");
        foreach(var item in LegacySources)
        foreach(bool resaved in new[]{false,true})
        {
            string path=Path.Combine(root,resaved?"map-annex-legacy-"+item.Name+"-resaved.json":item.File);
            string hash=ExplorationMapSaveTests.Hash(path);
            TestAssert.Equal(File.ReadAllText(path+".sha256"),hash,"legacy chain cold exact witness bytes");
            using(var wire=JsonDocument.Parse(File.ReadAllText(path)))
                TestAssert.Equal(resaved?CurrentRevision:item.Revision,
                    wire.RootElement.GetProperty("explorationMap").GetProperty("terrainRevision").GetString(),
                    "legacy cold source retains its real historical version");
            var loaded=PrologueSaveStore.LoadOrNew(path,2,target);
            ExplorationMapSaveTests.AssertCore(loaded);AssertLegacyBits(loaded.ExplorationMap,target,item);
            TestAssert.Equal(hash,ExplorationMapSaveTests.Hash(path),"legacy chain cold source is read-only");
        }
        GD.Print("ANNEX_MAP_LEGACY_COLD_OK witnesses=6 source_versions=3 literal_bits=true read_only=true");
    }

    private sealed record Legacy(string Name,string File,string Revision,int[] Cells,int[] Edges);
    private static readonly Legacy[] LegacySources=
    [
        new("b264","map-v133-migration.json","B264EB610D985D63288925D90C9F239950A1D406AF7063E0BF5C470F8E62089E",
            [0,447715,447737,518399],[0,6525,6570,6574,13907]),
        new("shelf","map-postrise-sparse.json","48020BD483F79CC058185DB230C17608740B5FC43309E2F783F0C7C02B45CA15",
            [0,518399,446925,446932,445968,466128],[0,8787,8838,13907]),
        new("post","map-grip8-baffle-post-source.json","BEE5E8069591E12D83F10EB0477FFCA3CFEAC1A314DE6C6C958F7E257271E3C2",
            [0,194501,518399],[0,10737,10797,10806,13907])
    ];

    internal static void RunHistoricalChains(string root)
    {
        using var target=ExplorationMapSaveTests.CurrentRevisionCatalog();AssertTarget(target);
        foreach(var item in LegacySources)
        {
            string path=Path.Combine(root,item.File),hash=ExplorationMapSaveTests.Hash(path);
            var loaded=PrologueSaveStore.LoadOrNew(path,2,target);
            ExplorationMapSaveTests.AssertCore(loaded);
            TestAssert.True(loaded.ExplorationMap!=null,"ANNEX_MAP_LEGACY_CHAIN_RED: preserve previously supported "+item.Name+" source through144 to actual158");
            AssertLegacyBits(loaded.ExplorationMap,target,item);
            TestAssert.Equal(hash,ExplorationMapSaveTests.Hash(path),"legacy chain never rewrites old source");
            string saved=Path.Combine(root,"map-annex-legacy-"+item.Name+"-resaved.json");
            TestAssert.True(!File.Exists(saved),"legacy chain resave is fresh isolated evidence");
            PrologueSaveStore.SaveAtomic(saved,loaded);File.WriteAllText(saved+".sha256",ExplorationMapSaveTests.Hash(saved));
        }
        GD.Print("ANNEX_MAP_LEGACY_CHAINS_OK sources=3 current_target=true literal_bits=true source_read_only=true");
    }

    private static void AssertLegacyBits(ExplorationMapSaveData data,MinimapTerrainCatalog target,Legacy item)
    {
        TestAssert.True(data!=null && data.TerrainRevision==CurrentRevision,"legacy chain has the actual current version");
        var model=new ExplorationMapModel(target);model.Restore(data);
        for(int id=0;id<518400;id++) TestAssert.Equal(item.Cells.Contains(id),model.IsCellExplored(id),"literal "+item.Name+" chain cell "+id);
        for(int id=0;id<13908;id++) TestAssert.Equal(item.Edges.Contains(id),model.IsBoundaryExplored(id),"literal "+item.Name+" chain boundary "+id);
    }

    private static void AssertTarget(MinimapTerrainCatalog target)
    {
        TestAssert.True(target.Revision==CurrentRevision && target.SolidCount==158 && target.BoundaryCount==13908 &&
            target.WorldBounds==new Rect2(0,0,7680,4320) && target.Columns==960 && target.Rows==540 && target.CellCount==518400,
            "annex tests use the actual current158 catalog, not a relabelled historical fixture");
        TestAssert.Equal(CurrentSha,Convert.ToHexString(SHA256.HashData(target.SerializedValues)),"current complete catalog bytes attested");
    }

    private static void AssertCurrentFactoryAudits()
    {
        foreach(var id in new[]{PrologueSectorId.CanyonBasin,PrologueSectorId.CanyonAscent})
        {
            var donor=PrologueCanyonBuildParts.Build(id);Shape2D[] owned=[];
            try
            {
                SteamMetadataSynchronizationTests.OwnDonorShapes(donor,out owned);
                if(id==PrologueSectorId.CanyonBasin)
                {
                    AssertMovingGearDonorAuditRejectsCorruption(donor,owned);
                    AssertMovingGearDonorAuditRejectsCorruption(donor,owned,"Gameplay/Mechanisms/UpperGalleryUnderArcGear/Collision","ARC_GEAR");
                }
            }
            finally {donor.Free();foreach(var shape in owned) shape.Dispose();}
        }
    }

    private static void AssertMovingGearDonorAuditRejectsCorruption(PrologueSector donor,Shape2D[] owned,
        string path="Gameplay/Mechanisms/UpperGalleryMovingGear/Collision",string marker="MOVING_GEAR")
    {
        const string complete="exact audited donor paths/types and distinct fresh native IDs";
        var collider=donor.GetNode<CollisionShape2D>(path);
        var original=(CircleShape2D)collider.Shape;
        ulong originalId=original.GetInstanceId();
        TestAssert.True(owned.Length==62 && owned.Contains(original) && original.Radius==24 &&
            string.IsNullOrEmpty(original.ResourcePath),"moving circle has an exact direct-new factory ownership audit");
        int controls=0;
        void Rejected(string label,string message,params PrologueSector[] borrowed)
        {
            bool rejected=false;
            try {SteamMetadataSynchronizationTests.OwnDonorShapes(donor,out _,borrowed);}
            catch(InvalidOperationException error) when(error.Message.Contains(message,StringComparison.Ordinal)) {rejected=true;}
            TestAssert.True(rejected,"MOVING_GEAR_DONOR_AUDIT_RED: must reject "+label);
            controls++;
        }

        original.Radius=25;
        try {Rejected("wrong circle radius","audited direct-new circle radius: "+path);}
        finally {original.Radius=24;}
        using(var wrong=new RectangleShape2D {Size=new(48,48)})
        {
            collider.Shape=wrong;
            try {Rejected("wrong native shape type",complete);}
            finally {collider.Shape=original;}
        }
        using(var extraShape=new CircleShape2D {Radius=24})
        {
            var extra=new CollisionShape2D {Name="UnexpectedCollider",Shape=extraShape};
            collider.GetParent().AddChild(extra);
            try {Rejected("unlisted extra collision path",complete);}
            finally {extra.Free();}
        }
        var other=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin); Shape2D[] otherOwned=[];
        try
        {
            SteamMetadataSynchronizationTests.OwnDonorShapes(other,out otherOwned,donor);
            Shape2D borrowed=other.GetNode<CollisionShape2D>(path).Shape;
            TestAssert.True(string.IsNullOrEmpty(borrowed.ResourcePath) && borrowed.GetInstanceId()!=originalId,
                "borrowed negative isolates native identity, not a nonempty cached ResourcePath");
            collider.Shape=borrowed;
            try {Rejected("another donor's fresh circle",complete,other);}
            finally {collider.Shape=original;}
            TestAssert.True(GodotObject.IsInstanceValid(borrowed),"audit never disposes another donor's borrowed circle");
        }
        finally {other.Free();foreach(var shape in otherOwned) shape.Dispose();}
        SteamMetadataSynchronizationTests.OwnDonorShapes(donor,out var restored);
        TestAssert.True(restored.Length==62 && restored.Select(x=>x.GetInstanceId()).ToHashSet().SetEquals(owned.Select(x=>x.GetInstanceId())) &&
            GodotObject.IsInstanceIdValid(originalId) && collider.Shape==original && original.Radius==24,
            "all audit negatives restore exact original owned IDs and authored radius");
        TestAssert.Equal(4,controls,"all donor audit corruption controls executed");
        GD.Print(marker+"_DONOR_AUDIT_OK paths=62 circles=8 rectangles=54 negatives=4 exact_restore=true borrowed_empty_path_rejected=true human=false");
    }

    private static void AssertPhysicalDelta(MinimapTerrainCatalog target)
    {
        using var prior=HistoricalMinimapCatalogFixture.Baffle();
        TestAssert.True(prior.Revision==PriorRevision && prior.SolidCount==144 && prior.BoundaryCount==12468,
            "independent prior catalog is exact144");
        var owners=Enumerable.Range(0,144).Select(old=>Enumerable.Range(0,158)
            .Single(current=>target.Solid(current)==prior.Solid(old))).ToArray();
        for(int id=0;id<12468;id++)
        {
            MinimapBoundary old=prior.Boundary(id),current=target.Boundary(id<6664?id:id+1440);
            TestAssert.True(current==(old with{ShapeId=owners[old.ShapeId]}),
                "complete unchanged owner/endpoint/normal identity at old edge "+id);
        }
        Rect2[] added=
        [
            new(5100,3676,160,24),new(5240,3756,140,24),new(5360,3836,800,24),new(4680,3996,760,24),
            new(5440,4296,720,8),new(6160,3996,216,24),new(6376,3600,24,720),new(5360,3600,1040,40),
            new(6240,3916,136,16),new(6240,3836,136,16),new(6240,3756,136,16),
            new(5140,3916,100,16),new(5140,3836,100,16),new(5140,3756,100,16)
        ];
        var newOwners=Enumerable.Range(0,158).Except(owners).ToArray();
        TestAssert.Equal(14,newOwners.Length,"only the fourteen authored annex supports were added");
        foreach(Rect2 rectangle in added)
            TestAssert.Equal(1,newOwners.Count(id=>target.Solid(id).LocalToWorld*target.Solid(id).LocalBounds==rectangle),
                "independent literal annex structural footprint "+rectangle);
    }

    private static (byte[] Cells,byte[] Edges) PriorBits(string kind)
    {
        byte[] cells=new byte[64800],edges=new byte[1559];
        if(kind=="full") {Array.Fill(cells,(byte)255);Array.Fill(edges,(byte)255);edges[^1]=15;}
        if(kind=="sparse")
        {
            foreach(int id in new[]{0,518399,449*960+638,449*960+639,450*960+640,459*960+636,
                459*960+637,499*960+583,499*960+584,502*960+638,503*960+584,448*960+800,450*960+800,450*960+801}) Set(cells,id);
            foreach(int id in new[]{0,6663,6664,12467}) Set(edges,id);
        }
        return(cells,edges);
    }

    private static JsonObject Wire(byte[] cells,byte[] edges)
    {
        var core=JsonNode.Parse(ExplorationMapSaveTests.CoreJson).AsObject();
        core["explorationMap"]=new JsonObject
        {
            ["formatVersion"]=1,["terrainRevision"]=PriorRevision,
            ["freeCellsBase64"]=Convert.ToBase64String(cells),["boundarySegmentsBase64"]=Convert.ToBase64String(edges)
        };
        return core;
    }

    private static void AssertBits(ExplorationMapSaveData data,MinimapTerrainCatalog target,string kind)
    {
        TestAssert.True(data!=null && data.FormatVersion==1 && data.TerrainRevision==CurrentRevision,
            "actual converted optional data carries only the exact current revision");
        var (cells,edges)=PriorBits(kind);
        var model=new ExplorationMapModel(target);model.Restore(data);
        // Geometric inclusive-touch oracle, independent of production integer
        // mask ranges. Newroom includes all added supports except the two lips.
        Rect2[] forgotten=[new(5120,3600,1280,720),new(5100,3676,160,24),new(4680,3996,760,24)];
        for(int id=0;id<518400;id++)
        {
            Rect2 cell=new((id%960)*8,(id/960)*8,8,8);
            bool touches=forgotten.Any(r=>cell.Position.X<=r.End.X && cell.End.X>=r.Position.X &&
                cell.Position.Y<=r.End.Y && cell.End.Y>=r.Position.Y);
            TestAssert.Equal(Bit(cells,id)&&!touches,model.IsCellExplored(id),"independent annex cell "+id);
        }
        for(int id=0;id<13908;id++)
        {
            bool expected=id<6664?Bit(edges,id):id>=8104&&Bit(edges,id-1440);
            TestAssert.Equal(expected,model.IsBoundaryExplored(id),"independent annex boundary "+id);
        }
        byte[] encoded=Convert.FromBase64String(data.BoundarySegmentsBase64);
        TestAssert.True(encoded.Length==1739 && (encoded[^1]&0xf0)==0,"new bitset length and padding remain canonical");
    }

    private static int Controls(string root,MinimapTerrainCatalog target)
    {
        var (cells,edges)=PriorBits("sparse");
        int cases=0;
        void Reject(string name,Action<JsonObject> mutate,MinimapTerrainCatalog actual=null)
        {
            JsonObject wire=Wire(cells,edges);mutate(wire);
            string path=Path.Combine(root,"map-annex-negative-"+name+".json");WriteFresh(path,wire);
            string before=ExplorationMapSaveTests.Hash(path);
            var loaded=PrologueSaveStore.LoadOrNew(path,2,actual??target);
            ExplorationMapSaveTests.AssertCore(loaded);
            TestAssert.True(loaded.ExplorationMap==null,"unknown or malformed annex extension rejected: "+name);
            TestAssert.Equal(before,ExplorationMapSaveTests.Hash(path),"rejected optional source is read-only: "+name);
            cases++;
        }
        Reject("unknown-source",w=>w["explorationMap"]["terrainRevision"]="unattested");
        Reject("shelf-size-spoof",w=>w["explorationMap"]["terrainRevision"]="48020BD483F79CC058185DB230C17608740B5FC43309E2F783F0C7C02B45CA15");
        Reject("current-label-old-bits",w=>w["explorationMap"]["terrainRevision"]=CurrentRevision);
        Reject("format",w=>w["explorationMap"]["formatVersion"]=2);
        Reject("missing-cell",w=>w["explorationMap"].AsObject().Remove("freeCellsBase64"));
        Reject("missing-edge",w=>w["explorationMap"].AsObject().Remove("boundarySegmentsBase64"));
        Reject("short-cell",w=>w["explorationMap"]["freeCellsBase64"]=Convert.ToBase64String(new byte[64799]));
        Reject("long-cell",w=>w["explorationMap"]["freeCellsBase64"]=Convert.ToBase64String(new byte[64801]));
        Reject("short-edge",w=>w["explorationMap"]["boundarySegmentsBase64"]=Convert.ToBase64String(new byte[1558]));
        Reject("long-edge",w=>w["explorationMap"]["boundarySegmentsBase64"]=Convert.ToBase64String(new byte[1560]));
        Reject("padding",w=>{byte[] bad=(byte[])edges.Clone();bad[^1]|=0x80;w["explorationMap"]["boundarySegmentsBase64"]=Convert.ToBase64String(bad);});
        Reject("noncanonical",w=>w["explorationMap"]["boundarySegmentsBase64"]=" "+Convert.ToBase64String(edges));
        Reject("not-base64",w=>w["explorationMap"]["boundarySegmentsBase64"]=new string('!',2080));
        Reject("over-capacity",w=>w["explorationMap"]["freeCellsBase64"]=new string('A',200000));
        var solids=Enumerable.Range(0,target.SolidCount).Select(target.Solid).ToArray();
        var boundaries=Enumerable.Range(0,target.BoundaryCount).Select(target.Boundary).ToArray();
        var shifted=(MinimapSolid[])solids.Clone();
        shifted[0]=shifted[0] with{LocalBounds=new Rect2(shifted[0].LocalBounds.Position+new Vector2(.25f,0),shifted[0].LocalBounds.Size)};
        using(var fake=new MinimapTerrainCatalog(target.WorldBounds,CurrentRevision,shifted,boundaries)) Reject("forged-geometry",_=>{},fake);
        var normals=(MinimapBoundary[])boundaries.Clone();normals[0]=normals[0] with{OutwardLocal=-normals[0].OutwardLocal};
        using(var fake=new MinimapTerrainCatalog(target.WorldBounds,CurrentRevision,solids,normals)) Reject("forged-normal",_=>{},fake);
        using(var fake=new MinimapTerrainCatalog(new Rect2(0,0,7688,4320),CurrentRevision,solids,boundaries)) Reject("forged-grid",_=>{},fake);
        using(var fake=new MinimapTerrainCatalog(target.WorldBounds,CurrentRevision,solids.Append(solids[0]).ToArray(),boundaries)) Reject("forged-count",_=>{},fake);
        using(var fake=new MinimapTerrainCatalog(target.WorldBounds,"wrong-target",solids,boundaries)) Reject("wrong-target",_=>{},fake);
        string invalid=Path.Combine(root,"map-annex-invalid-core.json");
        JsonObject invalidWire=Wire(cells,edges);invalidWire["version"]=999;WriteFresh(invalid,invalidWire);
        string invalidHash=ExplorationMapSaveTests.Hash(invalid);
        var recovered=PrologueSaveStore.LoadOrNew(invalid,2,target);
        TestAssert.True(recovered.WasRecovered && !recovered.Completed && !recovered.BossDefeated && recovered.ExplorationMap==null,
            "an optional map cannot manufacture or rescue invalid core progress");
        TestAssert.Equal(invalidHash,ExplorationMapSaveTests.Hash(invalid),"invalid core source remains untouched");
        return cases+1;
    }

    private static void WriteFresh(string path,JsonObject wire)
    {TestAssert.True(!File.Exists(path),"test evidence path must be fresh");File.WriteAllText(path,wire.ToJsonString());}
    private static void Set(byte[] bits,int id)=>bits[id>>3]|=(byte)(1<<(id&7));
    private static bool Bit(byte[] bits,int id)=>(bits[id>>3]&(1<<(id&7)))!=0;
}
