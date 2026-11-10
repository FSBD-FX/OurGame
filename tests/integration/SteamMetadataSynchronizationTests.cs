using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Knight.Build.Prologue;
using Knight.Presentation;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// This tests generated, detached authoring graphs, not runtime traversal or rendered appearance.
public static class SteamMetadataSynchronizationTests
{
    private const string Marker = "Gameplay/MicroScenes/SteamValves";
    private const string Wash = "VisualRoot/GameplayReadable/MicroSceneReadability/SteamValves";
    private const string Composition = "VisualRoot/Midground/ChallengeCompositions/SteamValves";
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        Marker+"|position", Marker+"|metadata/local_bounds", Wash+"|position", Wash+"|polygon", Composition+"|CompositionBounds"
    };

    public static void Run(string storage)
    {
#if METADATA_RESOURCE_IDENTITY
        MetadataResourceTrace.Begin();
#endif
        string path = Path.Combine(storage, "SteamMetadataBasin.tscn");
        PrologueSector original=null,target=null,donor=null;
        Shape2D[] donorShapes=Array.Empty<Shape2D>();
        try
        {
            original=Load();target=Load();donor=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
#if METADATA_RESOURCE_IDENTITY
            MetadataResourceTrace.Capture("canonical",null,original);
            MetadataResourceTrace.Capture("target",null,target);
            MetadataResourceTrace.Capture("donor",null,donor);
#endif
            OwnDonorShapes(donor,out donorShapes,original,target);
            string originalPersistence=PersistentLinks(GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonBasin)));
            var before = Snapshot(original);
            target.SceneFilePath = "";
            SteamMetadataSynchronizer.Apply(target, donor);
            AssertValues(target); // RED must be a real unchanged marker, not a compile/missing method error.
            AssertOnlyAllowed(before, Snapshot(target));
            var after=Snapshot(target);
            string[] changed=before.Where(p=>after[p.Key]!=p.Value).Select(p=>p.Key).ToArray();
            bool alreadyCurrent=original.GetNode(Marker).GetMeta("local_bounds").AsRect2()==new Rect2(1760,200,1160,520);
            TestAssert.True(alreadyCurrent ? changed.Length==0 : changed.ToHashSet().SetEquals(Allowed), "old input changes exactly five fields; already current input is exactly idempotent");
            TestAssert.Equal(Error.Ok, PrologueCanyonBuildParts.SaveAndValidate(target, path, out int n, out int saved, out string failure), "save narrow generated Basin: "+failure);
            TestAssert.Equal(n, saved, "exact saved node count");
            using var privateSaved=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
            TestAssert.Equal(originalPersistence,PersistentLinks(privateSaved),"saved persistent groups, connections and nested scene identities are unchanged");
            var reloaded = privateSaved.Instantiate<PrologueSector>();
            try
            {
#if METADATA_RESOURCE_IDENTITY
                MetadataResourceTrace.Capture("private-ignore",privateSaved,reloaded);
#endif
                AssertValues(reloaded);
                AssertOnlyAllowed(before, Snapshot(reloaded));
                TestAssert.Equal(45, reloaded.GetNode("Gameplay/Collision").GetChildCount(), "current Basin45 including14 existing annex bodies, metadata adds no terrain");
                var beforeAgain = Snapshot(reloaded);
                SteamMetadataSynchronizer.Apply(reloaded, donor);
                TestAssert.True(Equal(beforeAgain, Snapshot(reloaded)), "idempotent sync changes nothing else");
                var collider = Walk(reloaded).OfType<CollisionShape2D>().First(x=>x.Shape!=null);
                collider.Disabled = !collider.Disabled;
                TestAssert.True(!OnlyAllowed(before, Snapshot(reloaded)), "non-allowlist physical change is detected by the same semantic checker");
                collider.Disabled = !collider.Disabled;
                TestAssert.True(OnlyAllowed(before, Snapshot(reloaded)), "physical negative control restored exactly");
                var rectangle=Walk(reloaded).OfType<CollisionShape2D>().First(x=>x.Shape is RectangleShape2D);
                Shape2D borrowed=rectangle.Shape;
                using(var owned=(RectangleShape2D)borrowed.Duplicate())
                {
                    rectangle.Shape=owned;
                    try
                    {
                        var immutableOwnedBefore=Snapshot(reloaded); Vector2 size=owned.Size;
                        owned.Size+=new Vector2(1,0);
                        TestAssert.True(!OnlyAllowed(immutableOwnedBefore,Snapshot(reloaded)),"same checker catches a deep owned resource mutation, not only object identity");
                        owned.Size=size;
                        TestAssert.True(Equal(immutableOwnedBefore,Snapshot(reloaded)),"owned resource negative restored exactly");
                    }
                    finally { rectangle.Shape=borrowed; }
                }
                TestAssert.True(OnlyAllowed(before,Snapshot(reloaded)),"owned negative never mutates borrowed canonical shape");
            }
            finally { reloaded.Free(); }
            Reject("null", null, donor);
            Reject("same_graph", donor, donor);
            Control("foreign_sector", x=>x.SectorId=PrologueSectorId.CanyonAscent, donor);
            Control("missing_target", x=>x.GetNode(Marker).Free(), donor);
            Control("wrong_class", x=> { var old=x.GetNode(Wash); var parent=old.GetParent(); old.Free(); parent.AddChild(new Node2D {Name="SteamValves"}); }, donor);
            var invalidDonor = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
            Shape2D[] invalidShapes=Array.Empty<Shape2D>();
            try { OwnDonorShapes(invalidDonor,out invalidShapes,original,target,donor);invalidDonor.GetNode(Marker).SetMeta("local_bounds", new Rect2()); Control("zero_bounds", _=>{}, invalidDonor); }
            finally { invalidDonor.Free(); ReleaseOwned(invalidShapes); }
            AssertParentCatalog(path);
            TestAssert.True(Equal(before, Snapshot(original)), "borrowed original graph and shared resources were not mutated");
            GD.Print($"STEAM_METADATA_STAGED_OK nodes={n} fields=5 nodes_changed=3 basin_solids=45 world_solids=158 nonallowlist_equal=true input_rejections=6 extra_mutation_controls=2 canonical_written=false normal_route=false");
        }
        finally { if(GodotObject.IsInstanceValid(original))original.Free();if(GodotObject.IsInstanceValid(target))target.Free();if(GodotObject.IsInstanceValid(donor))donor.Free();ReleaseOwned(donorShapes);
#if METADATA_RESOURCE_IDENTITY
            MetadataResourceTrace.Finish();
#endif
        }
    }

    public static void RunArchive(string storage)
    {
        CheckSnapshotOwnership();
#if METADATA_RESOURCE_IDENTITY
        MetadataResourceTrace.Begin();
#endif
        const string marker="Gameplay/MicroScenes/ArchiveSideCave",composition="VisualRoot/Midground/ChallengeCompositions/ArchiveSideCave";
        var allowed=new HashSet<string>(StringComparer.Ordinal){marker+"|position",marker+"|metadata/local_bounds",composition+"|CompositionBounds"};
        var packed=GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonDescent));
        PrologueSector original=null,target=null,donor=null;Shape2D[] owned=Array.Empty<Shape2D>();
        try
        {
            original=packed.Instantiate<PrologueSector>();target=packed.Instantiate<PrologueSector>();donor=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent);
#if METADATA_RESOURCE_IDENTITY
            MetadataResourceTrace.Capture("canonical",packed,original);
            MetadataResourceTrace.Capture("target",packed,target);
            MetadataResourceTrace.Capture("donor",null,donor);
#endif
            OwnDonorShapes(donor,out owned,original,target);
            var before=Snapshot(original);target.SceneFilePath="";SteamMetadataSynchronizer.ApplyArchive(target,donor);
            Values(target);Unchanged(before,Snapshot(target));
            var after=Snapshot(target);string[] changed=before.Keys.Where(k=>before[k]!=after[k]).ToArray();
            bool current=original.GetNode(marker).GetMeta("local_bounds").AsRect2()==new Rect2(0,1176,704,624);
            TestAssert.True(current?changed.Length==0:changed.ToHashSet().SetEquals(allowed),"Archive changes exactly three fields, or zero when already current");
            string path=Path.Combine(storage,"ArchiveMetadataDescent.tscn");
            TestAssert.Equal(Error.Ok,PrologueCanyonBuildParts.SaveAndValidate(target,path,out int n,out int saved,out string failure),"save narrow Archive: "+failure);
            TestAssert.Equal(n,saved,"Archive exact saved node count");
            using var staged=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
            TestAssert.Equal(PersistentLinks(packed),PersistentLinks(staged),"Archive all persistent groups/connections/nested-scene links retained");
            var reloaded=staged.Instantiate<PrologueSector>();
            try
            {
#if METADATA_RESOURCE_IDENTITY
                MetadataResourceTrace.Capture("private-ignore",staged,reloaded);
#endif
                Values(reloaded);Unchanged(before,Snapshot(reloaded));
                TestAssert.Equal(37,reloaded.GetNode("Gameplay/Collision").GetChildCount(),"Archive retains current37 Descent solids, metadata cannot change terrain");
                var idempotent=Snapshot(reloaded);SteamMetadataSynchronizer.ApplyArchive(reloaded,donor);
                TestAssert.True(Equal(idempotent,Snapshot(reloaded)),"Archive producer is idempotent");
                var shape=Walk(reloaded).OfType<CollisionShape2D>().First(x=>x.Shape!=null);shape.Disabled=!shape.Disabled;
                TestAssert.True(!Equivalent(before,Snapshot(reloaded)),"Archive exact same checker detects nonallowlist collision mutation");shape.Disabled=!shape.Disabled;
                TestAssert.True(Equivalent(before,Snapshot(reloaded)),"Archive negative restored without touching borrowed shapes");
            }
            finally { reloaded.Free(); }
            RejectArchive("null",null,donor);RejectArchive("same_graph",donor,donor);
            ControlArchive("foreign_sector",x=>x.SectorId=PrologueSectorId.CanyonAscent,donor);
            ControlArchive("missing_target",x=>x.GetNode(marker).Free(),donor);
            ControlArchive("wrong_class",x=>{var old=x.GetNode(composition);var parent=old.GetParent();old.Free();parent.AddChild(new Node2D{Name="ArchiveSideCave"});},donor);
            var invalid=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent);Shape2D[] invalidOwned=Array.Empty<Shape2D>();
            try { OwnDonorShapes(invalid,out invalidOwned,original,target,donor);invalid.GetNode(marker).SetMeta("local_bounds",new Rect2());ControlArchive("zero_bounds",_=>{},invalid); }
            finally { invalid.Free();ReleaseOwned(invalidOwned); }
            AssertParentCatalog(path,PrologueSectorId.CanyonDescent,"ARCHIVE_METADATA");
            TestAssert.True(Equal(before,Snapshot(original)),"Archive borrowed original graph/resources unchanged");
            GD.Print($"ARCHIVE_METADATA_STAGED_OK nodes={n} fields=3 nodes_changed=2 descent_solids=37 world_solids=158 nonallowlist_equal=true input_rejections=6 extra_mutation_controls=1 canonical_written=false normal_route=false");
        }
        finally { if(GodotObject.IsInstanceValid(original))original.Free();if(GodotObject.IsInstanceValid(target))target.Free();if(GodotObject.IsInstanceValid(donor))donor.Free();ReleaseOwned(owned);
#if METADATA_RESOURCE_IDENTITY
            MetadataResourceTrace.Finish();
#endif
        }

        void Values(PrologueSector root)
        {
            TestAssert.Equal(new Rect2(0,1176,704,624),root.GetNode(marker).GetMeta("local_bounds").AsRect2(),"saved Archive marker uses approved finite704 territory, not obsolete640");
            TestAssert.Equal(new Vector2(352,1488),root.GetNode<Marker2D>(marker).Position,"saved Archive center matches finite region");
            var visual=root.GetNode<CanyonBackdropVisual>(composition);TestAssert.Equal(new Rect2(0,1176,704,624),visual.CompositionBounds,"saved Archive composition agrees");
            TestAssert.Equal(Vector2.Zero,visual.Position,"Archive composition has no double translation");
            TestAssert.True(!root.HasNode("VisualRoot/GameplayReadable/MicroSceneReadability/ArchiveSideCave"),"Descent deliberately has no wash; never invent one");
        }
        bool Equivalent(Dictionary<string,string> a,Dictionary<string,string> b)=>a.Count==b.Count&&a.All(x=>b.TryGetValue(x.Key,out var v)&&(allowed.Contains(x.Key)||v==x.Value));
        void Unchanged(Dictionary<string,string> a,Dictionary<string,string> b)=>TestAssert.True(Equivalent(a,b),"Archive every stored semantic outside three primitive fields is unchanged");
        void RejectArchive(string label,PrologueSector destination,PrologueSector source)
        {
            var beforeReject=destination==null?null:Snapshot(destination);bool rejected=false;
            try { SteamMetadataSynchronizer.ApplyArchive(destination,source); }catch(ArgumentException){rejected=true;}
            TestAssert.True(rejected&&(destination==null||Equal(beforeReject,Snapshot(destination))),"Archive input rejection atomic: "+label);
            GD.Print("ARCHIVE_METADATA_REJECT_OK case="+label+" atomic=true");
        }
        void ControlArchive(string label,Action<PrologueSector> mutate,PrologueSector source)
        {var destination=packed.Instantiate<PrologueSector>();try{destination.SceneFilePath="";mutate(destination);RejectArchive(label,destination,source);}finally{destination.Free();}}
    }

    // The caller keeps ownership of its actual wrappers until native graph and
    // pack teardown. This checks our Variant/container custody, not cold-wrapper
    // rehydration or all resource-loader leaks.
    private static void CheckSnapshotOwnership()
    {
        bool control = Check(false), snapshot = Check(true);
        TestAssert.True(control && snapshot, "owned graph and Snapshot Variant references retire after their explicit owners");
        GD.Print("METADATA_SNAPSHOT_OWNERSHIP_OK cases=2 owned_wrappers=true no_snapshot=true snapshot=true cold_wrapper_lifetime_proved=false no_load_save_gc_or_borrowed_dispose=true");

        static bool Check(bool observe)
        {
            var shape = new CircleShape2D { Radius = 9 };
            ulong shapeId = shape.GetInstanceId();
            var graph = new Node2D { Name = "SnapshotOwnershipRoot" };
            var body = new StaticBody2D { Name = "Body" };
            var collision = new CollisionShape2D { Name = "Collision", Shape = shape };
            graph.AddChild(body); body.AddChild(collision); body.Owner = graph; collision.Owner = graph;
            var packed = new PackedScene();
            ulong packedId = packed.GetInstanceId();
            try
            {
                TestAssert.Equal(Error.Ok, packed.Pack(graph), "owned Snapshot fixture packs a real collision graph");
                // The call borrows a Variant; release our temporary explicitly.
                // Set after Pack so the saved bundle cannot reference itself.
                using (var metadata = Variant.From(packed)) graph.SetMeta("owned_bundle", metadata);
                if (observe)
                {
                    var values = Snapshot(graph);
                    TestAssert.True(values.TryGetValue(".|metadata/owned_bundle", out string bundle) &&
                        bundle.Contains("_bundled", StringComparison.Ordinal) && bundle.Contains("CircleShape2D", StringComparison.Ordinal),
                        "real Snapshot visits PackedScene bundle and native shape, not only node scalars");
                }
            }
            finally
            {
                graph.RemoveMeta("owned_bundle"); graph.Free(); packed.Dispose();
                shape.Dispose();
            }
            bool retired = !GodotObject.IsInstanceIdValid(shapeId);
            GD.Print($"METADATA_SNAPSHOT_OWNERSHIP_OBSERVED observe={observe} owned_wrapper=true shape_id={shapeId} retired={retired} packed_retired={!GodotObject.IsInstanceIdValid(packedId)} no_gc=true no_borrowed_dispose=true");
            return retired;
        }
    }

    private static PrologueSector Load() => GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonBasin)).Instantiate<PrologueSector>();
    private static void AssertValues(PrologueSector root)
    {
        TestAssert.Equal(new Rect2(1760,200,1160,520), root.GetNode(Marker).GetMeta("local_bounds").AsRect2(), "actual Steam marker expands upward without altering physical terrain");
        TestAssert.Equal(new Vector2(2340,460), root.GetNode<Marker2D>(Marker).Position, "actual marker center");
        var wash = root.GetNode<Polygon2D>(Wash);
        TestAssert.Equal(new Vector2(2340,460), wash.Position, "non-colliding readability center");
        Vector2[] wanted = {new(-576,-256),new(576,-256),new(576,256),new(-576,256)};
        TestAssert.True(wash.Polygon.SequenceEqual(wanted), "actual independent four-point inset wash, not resized collision");
        var backdrop = root.GetNode<CanyonBackdropVisual>(Composition);
        TestAssert.Equal(new Rect2(1760,200,1160,520), backdrop.CompositionBounds, "background composition uses source region");
        TestAssert.Equal(Vector2.Zero, backdrop.Position, "no double translation of backdrop");
    }

    private static void Control(string label, Action<PrologueSector> mutate, PrologueSector donor)
    {
        var target = Load();
        try { target.SceneFilePath=""; mutate(target); Reject(label,target,donor); }
        finally { target.Free(); }
    }
    private static void Reject(string label, PrologueSector target, PrologueSector donor)
    {
        var before = target==null ? null : Snapshot(target);
        bool rejected=false;
        try { SteamMetadataSynchronizer.Apply(target,donor); }
        catch (ArgumentException) { rejected=true; }
        TestAssert.True(rejected, "reject invalid authoring input: "+label);
        TestAssert.True(target==null || Equal(before,Snapshot(target)), "rejection is atomic: "+label);
        GD.Print("STEAM_METADATA_REJECT_OK case="+label+" atomic=true");
    }

    private static void AssertParentCatalog(string path,PrologueSectorId sector=PrologueSectorId.CanyonBasin,string marker="STEAM_METADATA")
    {
        var paths=Enum.GetValues<PrologueSectorId>().ToDictionary(x=>x,PrologueCanyonBuildParts.ScenePath);
        paths[sector]=path;
        PrologueLevelController old=null,candidate=null,saved=null;Resource[] owned=Array.Empty<Resource>();ulong[] borrowed=Array.Empty<ulong>();
        try
        {
            old=BuildPrologueLevel.Build();owned=Owned(old);
            candidate=BuildPrologueLevel.Build(paths);owned=owned.Concat(Owned(candidate)).ToArray();
            saved=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
#if METADATA_RESOURCE_IDENTITY
            MetadataResourceTrace.Scene("parent-old",old.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Single(x=>x.Id==sector).Scene,old.GetInstanceId());
            MetadataResourceTrace.Scene("parent-candidate",candidate.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Single(x=>x.Id==sector).Scene,candidate.GetInstanceId());
            MetadataResourceTrace.Scene("parent-saved",saved.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Single(x=>x.Id==sector).Scene,saved.GetInstanceId());
#endif
            borrowed=new[]{old,candidate}.SelectMany(x=>x.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Select(d=>d.Scene.GetInstanceId())).Distinct().ToArray();
            TestAssert.True(owned.Length==12&&owned.Select(x=>x.GetInstanceId()).Distinct().Count()==12&&!owned.Any(x=>borrowed.Contains(x.GetInstanceId())),"only two builders' ten newly constructed definitions and two new catalogs are owned");
            var reference=old.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            foreach(var level in new[]{candidate,saved})
            {
                var actual=level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
                TestAssert.True(reference.SolidCount==158 && actual.SolidCount==158 && actual.Revision==reference.Revision && actual.SerializedValues.AsSpan().SequenceEqual(reference.SerializedValues), "complete current158 catalog including annex/baffle and boundary order/revision is byte-equivalent");
            }
            GD.Print(marker+"_CATALOG_OK explicit_paths=5 solids=158 serialized_bytes_equal=true revision_equal=true boundary_order_equal=true canonical_parent_written=false");
        }
        finally
        {
            // These two detached authoring roots own the newly-created native arrays. Freeing
            // their nodes does not clear the C# export fields/native Array references.
            foreach(var level in new[]{old,candidate}.Where(GodotObject.IsInstanceValid))
            {
                level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Clear();
                level.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;
            }
            if(GodotObject.IsInstanceValid(old))old.Free();if(GodotObject.IsInstanceValid(candidate))candidate.Free();if(GodotObject.IsInstanceValid(saved))saved.Free();ReleaseOwned(owned);
            TestAssert.True(borrowed.All(GodotObject.IsInstanceIdValid),"no borrowed sector PackedScene was disposed");
#if METADATA_RESOURCE_IDENTITY
            MetadataResourceTrace.Liveness("after-parent-cleanup");
#endif
        }
        static Resource[] Owned(PrologueLevelController level)=>level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Cast<Resource>().Append(level.GetNode<PrologueMinimapController>("MinimapController").Catalog).ToArray();
    }

    internal static void OwnDonorShapes(PrologueSector donor,out Shape2D[] owned,params PrologueSector[] borrowed)
    {
        // These exact factory paths directly new their shapes. The cached GraveLamp
        // leaf has no collision. ResourcePath alone is NOT an ownership proof.
        owned=Array.Empty<Shape2D>();
        var expected=new Dictionary<string,Type>(StringComparer.Ordinal);
        var sizes=new Dictionary<string,Vector2>(StringComparer.Ordinal);var radii=new Dictionary<string,float>(StringComparer.Ordinal);
        void Platforms(params string[] names){foreach(string name in names)expected.Add("Gameplay/Collision/"+name+"/Collision",typeof(RectangleShape2D));}
        void Rectangle(string path,Vector2 size){expected.Add(path,typeof(RectangleShape2D));sizes.Add(path,size);}
        void Circle(string path,float radius){expected.Add(path,typeof(CircleShape2D));radii.Add(path,radius);}
        if(donor.SectorId==PrologueSectorId.CanyonDescent)
        {
            Platforms("RiftRoof","RiftOuterRightWall","RiftArchiveOuterWall","RiftUpperFloor","RiftMiddleCatch","RiftLowerCatch","RiftAlternationWall","AbyssCommitment","AbyssSafeBottom","ArchiveEntry","ArchiveShelf","ArchiveFloor","ArchiveSupplyRoof","ArchiveSupplyRightWall","ArchiveResidentLowerFloor","ArchiveResidentUpperFloor","ArchiveResidentUpperBackWall","FrozenCeiling","FrozenOuterUpperWall","FrozenSentryRecessWall","FrozenOuterLowerWall","FrozenLeftCatch","FrozenUpperResidentShelf","FrozenRightCatch","FrozenLowerResidentShelf","FrozenExit");
            for(int i=1;i<=4;i++)Platforms("AbyssBrakeRight"+i,"AbyssBrakeLeft"+i);for(int i=1;i<=3;i++)Platforms("AbyssExit"+i);
            Rectangle("Gameplay/Mechanisms/ArchivePressureGate/Door/Collision",new(40,216));
            foreach(string side in new[]{"LeftJet","RightJet"})Rectangle("Gameplay/Mechanisms/SteamDiverterFrozen/"+side+"/Collision",new(56,150));
            Rectangle("Gameplay/FallRecovery/RecoveryVolume/Collision",new(336,16));Rectangle("Gameplay/FallRecovery/RecoveryVolume2/Collision",new(564,16));
            Circle("Gameplay/Mechanisms/HiddenPanel/ThreadTarget/TargetCollision/Collision",28);Circle("Gameplay/GrappleSurfaces/SteamDiverterFrozenControl/TargetCollision/Collision",22);Circle("Gameplay/Rewards/ArchiveSupply/Collision",28);
            foreach(string id in new[]{"rift","abyss-floor","archive"})Circle("Gameplay/QuestFragments/"+id+"/Collision",18);
        }
        else if(donor.SectorId==PrologueSectorId.CanyonBasin)
        {
            Platforms("BasinCampFloor","GearEntry","GearPistonOne","GearPistonTwo","GearResidentLeftShelf","GearWestClimbWall","GearWestLaunchShelf","GearResidentRightShelf","UpperGearGalleryOne","UpperGearGalleryTwo","UpperGearGalleryThree","SteamTrenchFloor","SteamResidentBay","SteamRejoinTop","ShortcutBasinWalkway","ShortcutCampReturnStep","ShortcutStep4116","ShortcutStep3956","ShortcutStep3796","ShortcutStep3636","ShortcutStep3956Grip","ShortcutStep3796Grip","ShortcutStep3636Grip");
            for(int i=1;i<=8;i++)Platforms("SteamRejoinStep"+i);
            foreach(var annex in new (string Name,Vector2 Size)[]{
                ("IncineratorUpperEntryA",new(160,24)),("IncineratorUpperEntryB",new(140,24)),
                ("IncineratorSafeGallery",new(800,24)),("IncineratorWestBank",new(760,24)),
                ("IncineratorContainmentFloor",new(720,8)),("IncineratorEastBank",new(216,24)),
                ("IncineratorEastWall",new(24,720)),("IncineratorRoof",new(1040,40)),
                ("IncineratorReturn1",new(136,16)),("IncineratorReturn2",new(136,16)),("IncineratorReturn3",new(136,16)),
                ("IncineratorWestReturn1",new(100,16)),("IncineratorWestReturn2",new(100,16)),("IncineratorWestReturn3",new(100,16))})
                Rectangle("Gameplay/Collision/"+annex.Name+"/Collision",annex.Size);
            Rectangle("Gameplay/Mechanisms/IncineratorPool/Collision",new(720,276));
            Rectangle("Gameplay/Mechanisms/GearPiston/Collision",new(132,32));Rectangle("Gameplay/Mechanisms/SteamJet/Collision",new(72,150));
            foreach(string side in new[]{"LeftJet","RightJet"})Rectangle("Gameplay/Mechanisms/SteamDiverterTrench/"+side+"/Collision",new(64,150));
            Rectangle("Gameplay/Mechanisms/BasinCounterweightLift/Collision",new(120,24));Rectangle("Gameplay/ShieldLane/Collision",new(260,120));Rectangle("Gameplay/BasinCheckpoint/Collision",new(52,104));Rectangle("Gameplay/FallRecovery/RecoveryVolume/Collision",new(5120,16));
            foreach(string id in new[]{"SteamValveControl","SteamDiverterTrenchControl","BasinCounterweightControl"})Circle("Gameplay/GrappleSurfaces/"+id+"/TargetCollision/Collision",22);
            Circle("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor/TargetCollision/Collision",24);
            Circle("Gameplay/Mechanisms/UpperGalleryMovingGear/Collision",24);
            Circle("Gameplay/Mechanisms/UpperGalleryUnderArcGear/Collision",24);
            foreach(string id in new[]{"gear-gallery","steam-loop"})Circle("Gameplay/QuestFragments/"+id+"/Collision",18);
        }
        else if(donor.SectorId==PrologueSectorId.CanyonAscent)
        {
            void PlatformSize(string name,Vector2 size)=>Rectangle("Gameplay/Collision/"+name+"/Collision",size);
            PlatformSize("LiftEntryFloor",new(160,16));foreach(string name in new[]{"LiftAccessStepOne","LiftAccessStepTwo"})PlatformSize(name,new(160,40));
            PlatformSize("LiftBackLanding",new(200,40));PlatformSize("LiftBackLandingGrip",new(36,60));
            for(int i=1;i<=9;i++){PlatformSize("AscentGrip"+i,new(i==2?140:240,40));PlatformSize("AscentGrip"+i+"Grip",new(36,60));}
            Rectangle("Gameplay/Collision/AscentGrip8/RaisedBaffle",new(4,21));
            PlatformSize("PendulumCeiling",new(220,40));PlatformSize("PendulumResidentFloor",new(184,40));PlatformSize("PendulumResidentRoof",new(128,40));PlatformSize("PendulumResidentBackWall",new(24,176));
            PlatformSize("SplitUpperGallery",new(280,40));PlatformSize("AscentFinalCatch",new(240,40));PlatformSize("AscentFinalCatchGrip",new(36,60));PlatformSize("CourtyardApproach",new(140,40));
            for(int y=3476;y>=1556;y-=160){PlatformSize("ShortcutStep"+y,new(150,y==3476?16:32));PlatformSize("ShortcutStep"+y+"Grip",new(36,60));}
            Rectangle("Gameplay/Mechanisms/CounterweightLift/Collision",new(80,24));Rectangle("Gameplay/Mechanisms/PendulumHazard/Collision",new(28,220));Rectangle("Gameplay/Mechanisms/UpperBasinShortcutWinch/Barrier/Collision",new(410,1930));
            Rectangle("Gameplay/FallRecovery/RecoveryVolume/Collision",new(464,16));
            foreach(string name in new[]{"CounterweightLiftControl","UpperBasinShortcutControl","CourtyardSealLinkageControl"})Circle("Gameplay/GrappleSurfaces/"+name+"/TargetCollision/Collision",22);
            Circle("Gameplay/QuestFragments/ascent-mid/Collision",18);
        }
        else throw new ArgumentException("No audited donor factory for this sector.");
        var actual=Walk(donor).OfType<CollisionShape2D>().Where(x=>x.Shape!=null).ToDictionary(x=>donor.GetPathTo(x).ToString(),x=>x.Shape,StringComparer.Ordinal);
        var forbidden=borrowed.SelectMany(Walk).OfType<CollisionShape2D>().Where(x=>x.Shape!=null).Select(x=>x.Shape.GetInstanceId()).ToHashSet();
        // Assign proven known-path resources before the whole-set assertion so the
        // caller's finally also covers a later completeness/dimension failure.
        owned=actual.Where(x=>expected.TryGetValue(x.Key,out var type)&&x.Value.GetType()==type&&string.IsNullOrEmpty(x.Value.ResourcePath)&&!forbidden.Contains(x.Value.GetInstanceId())).Select(x=>x.Value).Distinct().ToArray();
        int count=donor.SectorId switch{PrologueSectorId.CanyonDescent=>48,PrologueSectorId.CanyonBasin=>62,_=>66};
        TestAssert.True(expected.Count==count&&actual.Keys.ToHashSet().SetEquals(expected.Keys)&&owned.Length==count&&owned.Select(x=>x.GetInstanceId()).Distinct().Count()==count,
            "exact audited donor paths/types and distinct fresh native IDs; no borrowed canonical/other-donor shapes"+
            $" sector={donor.SectorId} expected={expected.Count}/{count} actual={actual.Count} owned={owned.Length}"+
            " missing="+string.Join(",",expected.Keys.Except(actual.Keys))+" extra="+string.Join(",",actual.Keys.Except(expected.Keys)));
        foreach(var pair in sizes)TestAssert.Equal(pair.Value,((RectangleShape2D)actual[pair.Key]).Size,"audited direct-new rectangle size: "+pair.Key);
        foreach(var pair in radii)TestAssert.Equal(pair.Value,((CircleShape2D)actual[pair.Key]).Radius,"audited direct-new circle radius: "+pair.Key);
        int circles=donor.SectorId switch{PrologueSectorId.CanyonAscent=>4,PrologueSectorId.CanyonBasin=>8,_=>6};
        GD.Print($"METADATA_DONOR_OWNERS_OK sector={donor.SectorId} paths={count} native_ids={count} rectangles={count-circles} circles={circles} borrowed_disjoint=true");
    }
    private static void ReleaseOwned(IEnumerable<Resource> resources)
    {
        foreach(var resource in resources) { ulong id=resource.GetInstanceId();resource.Dispose();TestAssert.True(!GodotObject.IsInstanceIdValid(id),"exact owned resource released, no GC/wait"); }
    }

    private static IEnumerable<Node> Walk(Node root) { yield return root; foreach(Node child in root.GetChildren()) foreach(Node item in Walk(child)) yield return item; }
    internal static string PersistentLinks(PackedScene packed)
    {
        using var state=packed.GetState(); var values=new List<string>();
        for(int i=0;i<state.GetNodeCount();i++)
            values.Add(state.GetNodePath(i)+"|"+string.Join(",",state.GetNodeGroups(i))+"|"+state.GetNodeInstance(i)?.ResourcePath+"|"+state.GetNodeInstancePlaceholder(i));
        for(int i=0;i<state.GetConnectionCount();i++)
        {
            using var binds=state.GetConnectionBinds(i);
            values.Add(state.GetConnectionSource(i)+"|"+state.GetConnectionSignal(i)+"|"+state.GetConnectionTarget(i)+"|"+state.GetConnectionMethod(i)+"|"+state.GetConnectionFlags(i)+"|"+state.GetConnectionUnbinds(i)+"|"+Convert.ToHexString(GD.VarToBytes(binds)));
        }
        return string.Join("\n",values);
    }
    private static bool Equal(Dictionary<string,string> a, Dictionary<string,string> b) => a.Count==b.Count && a.All(x=>b.TryGetValue(x.Key,out string value)&&value==x.Value);
    private static bool OnlyAllowed(Dictionary<string,string> a, Dictionary<string,string> b) => a.Count==b.Count && a.All(x=>b.TryGetValue(x.Key,out string value)&&(Allowed.Contains(x.Key)||value==x.Value));
    private static void AssertOnlyAllowed(Dictionary<string,string> a, Dictionary<string,string> b)
    {
        string[] differences=a.Keys.Union(b.Keys).Where(k=>!Allowed.Contains(k)&&(!a.TryGetValue(k,out var v)||!b.TryGetValue(k,out var w)||v!=w)).ToArray();
        TestAssert.True(differences.Length==0, "all stored semantics, paths, order, groups, sharing and resources outside five fields: "+string.Join(",",differences.Take(12)));
    }

    // Snapshot immediate immutable scalar strings, including recursive resource contents and sharing.
    // Never retain a live Variant/resource as the before-state; a shared mutation would change both.
    internal static Dictionary<string,string> Snapshot(Node root)
    {
        var output=new Dictionary<string,string>(StringComparer.Ordinal);
        var resources=new Dictionary<ulong,string>();
        foreach(var node in Walk(root))
        {
            string prefix=root.GetPathTo(node).ToString();
            output[prefix+"|$type"]=node.GetClass()+"/"+node.GetType().FullName;
            output[prefix+"|$order"]=node.GetIndex().ToString();
            output[prefix+"|$owner"]=node.Owner==null?"":root.GetPathTo(node.Owner).ToString();
            var groups=node.GetGroups(); using var groupsOwner=(Godot.Collections.Array)groups;
            output[prefix+"|$groups"]=string.Join(",",groups.OrderBy(x=>x.ToString()));
            var properties=node.GetPropertyList(); using var propertiesOwner=(Godot.Collections.Array)properties;
            foreach(var property in properties)
            {
                using(property)
                {
                    using var nameValue=property["name"]; using var usage=property["usage"];
                    string name=nameValue.AsString();
                    if((usage.AsInt64()&(long)PropertyUsageFlags.Storage)==0 || node==root&&name=="scene_file_path" || name.StartsWith("metadata/",StringComparison.Ordinal)) continue;
                    string key=prefix+"|"+name; using var actual=node.Get(name);
                    output[key]=Value(actual,key);
                }
            }
            var metadata=node.GetMetaList(); using var metadataOwner=(Godot.Collections.Array)metadata;
            foreach(var name in metadata.OrderBy(x=>x.ToString()))
            { string key=prefix+"|metadata/"+name; using var actual=node.GetMeta(name); output[key]=Value(actual,key); }
        }
        return output;

        string Value(Variant value,string key)
        {
            if(value.VariantType==Variant.Type.Object)
            {
                var obj=value.AsGodotObject(); if(obj==null)return "null";
                if(obj is not Resource resource) throw new InvalidOperationException("Unsupported stored object: "+key);
                ulong id=resource.GetInstanceId();
                if(resources.TryGetValue(id,out string reference))return "ref:"+reference;
                resources.Add(id,key);
                string resourcePath=resource.ResourcePath, header=resource.GetClass();
                if(!string.IsNullOrEmpty(resourcePath)&&!resourcePath.Contains("::",StringComparison.Ordinal))
                {
                    string absolute=ProjectSettings.GlobalizePath(resourcePath);
                    header+="@"+resourcePath+"#"+(File.Exists(absolute)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(absolute))):"nonfile");
                }
                var fields=new List<string>();
                var properties=resource.GetPropertyList(); using var propertiesOwner=(Godot.Collections.Array)properties;
                foreach(var property in properties)
                {
                    using(property)
                    {
                        using var nameValue=property["name"]; using var usage=property["usage"];
                        string name=nameValue.AsString();
                        if((usage.AsInt64()&(long)PropertyUsageFlags.Storage)==0 || name is "resource_path" or "resource_scene_unique_id")continue;
                        using var actual=resource.Get(name);
                        fields.Add(name+"="+Value(actual,key+"/"+name));
                    }
                }
                return header+"{"+string.Join(";",fields)+"}";
            }
            if(value.VariantType==Variant.Type.Array)
            {
                using var array=value.AsGodotArray();var items=new List<string>();
                for(int i=0;i<array.Count;i++) { using var actual=array[i]; items.Add(Value(actual,key+"/"+i)); }
                return "array["+string.Join(";",items)+"]";
            }
            if(value.VariantType==Variant.Type.Dictionary)
            {
                using var dictionary=value.AsGodotDictionary();var entries=new List<string>();int i=0;
                foreach(var pair in dictionary) { using var actualKey=pair.Key; using var actualValue=pair.Value; entries.Add(Value(actualKey,key+"/key"+i)+"="+Value(actualValue,key+"/value"+i));i++; }
                return "dictionary["+string.Join(";",entries)+"]";
            }
            if(value.VariantType is Variant.Type.Callable or Variant.Type.Signal or Variant.Type.Rid)throw new InvalidOperationException("Unsupported stored handle: "+key);
            return value.VariantType+":"+Convert.ToHexString(GD.VarToBytes(value));
        }
    }
}

#if METADATA_RESOURCE_IDENTITY
// Diagnostic only. No native object, Variant, delegate or SceneState is retained.
internal static class MetadataResourceTrace
{
    private readonly record struct Identity(string Stage,string Role,ulong RootId,string NodePath,ulong NativeId,string Type,string Size,string Source);
    private static readonly List<Identity> Rows=new();
    private static bool _active;
    internal static void Begin()
    {
        if(_active||Rows.Count!=0)throw new InvalidOperationException("one metadata trace per process");
        _active=true;
    }
    private static void Record(Identity row)
    {
        if(!_active)return;
        if(Rows.Count>=4096)throw new InvalidOperationException("metadata trace identity cap");
        Rows.Add(row);
        Console.WriteLine("METADATA_RESOURCE_IDENTITY "+System.Text.Json.JsonSerializer.Serialize(row));
    }
    internal static void Scene(string stage,PackedScene scene,ulong rootId)
    {
        if(!_active)return;
        Record(new(stage,"scene",rootId,"",scene.GetInstanceId(),scene.GetClass(),"",scene.ResourcePath));
    }
    internal static void Capture(string stage,PackedScene packed,Node root)
    {
        if(!_active)return;
        ulong rootId=root.GetInstanceId();
        if(packed!=null)Scene(stage,packed,rootId);
        Visit(root);
        void Visit(Node node)
        {
            if(node is CollisionShape2D collision&&collision.Shape is Shape2D shape)
            {
                using NodePath path=root.GetPathTo(node);
                Record(new(stage,"shape",rootId,path.ToString(),shape.GetInstanceId(),shape.GetClass(),shape.GetRect().ToString(),shape.ResourcePath));
            }
            for(int i=0;i<node.GetChildCount();i++)Visit(node.GetChild(i));
        }
    }
    internal static void Liveness(string stage)
    {
        if(!_active)return;
        foreach(var row in Rows)
            Console.WriteLine("METADATA_RESOURCE_LIVENESS "+System.Text.Json.JsonSerializer.Serialize(new{Stage=stage,ObservedStage=row.Stage,row.NativeId,Alive=GodotObject.IsInstanceIdValid(row.NativeId)}));
    }
    internal static void Finish()
    {
        if(!_active)return;
        // These are already observed scalar identities, not object re-lookups.
        var saved=Rows.Single(x=>x.Stage=="save-default"&&x.Role=="scene");
        var privateSaved=Rows.Single(x=>x.Stage=="private-ignore"&&x.Role=="scene");
        var parent=Rows.Single(x=>x.Stage=="parent-candidate"&&x.Role=="scene");
        var defaults=Rows.Where(x=>x.Stage=="save-default"&&x.Role=="shape").Select(x=>x.NativeId).ToHashSet();
        var privateShapes=Rows.Where(x=>x.Stage=="private-ignore"&&x.Role=="shape").Select(x=>x.NativeId).ToHashSet();
        Console.WriteLine("METADATA_RESOURCE_RELATION "+System.Text.Json.JsonSerializer.Serialize(new{
            DefaultSceneId=saved.NativeId,PrivateSceneId=privateSaved.NativeId,ParentSceneId=parent.NativeId,
            DefaultParentSame=saved.NativeId==parent.NativeId,DefaultPrivateSame=saved.NativeId==privateSaved.NativeId,
            DefaultShapes=defaults.Count,PrivateShapes=privateShapes.Count,SharedShapes=defaults.Intersect(privateShapes).Count()}));
        Liveness("after-existing-teardown");
        Console.WriteLine("METADATA_RESOURCE_TRACE_COMPLETE scalar_only=true no_added_loads=true no_added_instantiates=true no_added_getstate=true behavior_accepted=false");
        _active=false;
    }
}
#endif
