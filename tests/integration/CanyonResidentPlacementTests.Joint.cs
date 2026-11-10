using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Build.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static partial class CanyonResidentPlacementTests
{
    // Test artifact only: it grants no formal placement/publication authority.
    private sealed record CensusJointSeal(byte[] Payload,string Digest,string ContextDigest)
    {
        public IReadOnlyList<ResidentKeepOut> Reservations {get;init;}
    }
    private sealed record JointSiteDto(string StableId,float[] Home,float[] Territory,float[][] Points,float[] Occupancy);
    private sealed record JointPayloadDto(string Kind,int Schema,string ContextSha256,JointSiteDto[] Sites);
    private sealed record JointRuntimeSite(string StableId,Vector2 Home,Rect2 Territory,IReadOnlyList<Vector2> Points,Rect2 Occupancy);
    private static readonly JsonSerializerOptions JointJson=new() {PropertyNamingPolicy=JsonNamingPolicy.CamelCase,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};

    private static CensusJointSeal EncodeJoint(JointSiteDto[] sites,string context)
    {
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(new JointPayloadDto("final-joint",1,context,sites),JointJson);
        return new(bytes,Convert.ToHexString(SHA256.HashData(bytes)),context);
    }
    private static IReadOnlyList<JointRuntimeSite> ReadJointData(byte[] bytes,string expectedDigest,string expectedContext)
    {
        try
        {
            return Array.AsReadOnly(ResidentAuthoredLayout.Parse(bytes,expectedDigest,expectedContext).Select(site =>
                new JointRuntimeSite(site.Definition.StableId,site.Definition.Placement.WorldSpawn,
                    site.Definition.Placement.Territory,site.Definition.Placement.PatrolPoints,site.Occupancy)).ToArray());
        }
        catch(ArgumentException error) { throw new ArgumentException("joint."+error.Message,error); }
    }

    public static void RunJointCodecContracts()
    {
        // Literal value/shape controls only; these synthetic centers are NOT legal native sites.
        const string context="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var sites=CanyonResidentCatalog.All.Select(slot=>
        {
            Rect2 bounds=CanyonWorldLayout.Nodes.Single(x=>x.Id==slot.RouteNode).WorldBounds;
            Vector2 home=bounds.GetCenter();
            float[] point={home.X,home.Y};
            float[][] points=slot.Archetype==ResidentArchetype.Turret?new[]{point}:new[]{point,new[]{home.X+16,home.Y},point.ToArray()};
            return new JointSiteDto(slot.StableId,point,new[]{bounds.Position.X,bounds.Position.Y,bounds.Size.X,bounds.Size.Y},points,new[]{home.X-24,home.Y-24,64f,48f});
        }).ToArray();
        CensusJointSeal seal=EncodeJoint(sites,context);
        TestAssert.True(seal!=null&&seal.Payload!=null&&seal.Payload.Length>0,"joint codec encodes fixed fifty numeric rows before native allocation");
        using var document=JsonDocument.Parse(seal.Payload);
        var root=document.RootElement;
        TestAssert.True(root.GetProperty("kind").GetString()=="final-joint"&&root.GetProperty("schema").GetInt32()==1&&root.GetProperty("contextSha256").GetString()==context,"numeric joint schema/context preserved");
        TestAssert.Equal(50,root.GetProperty("sites").GetArrayLength(),"exact fifty encoded identities, not subtype buckets");
        for(int i=0;i<50;i++)
        {
            var row=root.GetProperty("sites")[i];
            TestAssert.True(row.GetProperty("stableId").GetString()==sites[i].StableId&&row.GetProperty("home")[0].ValueKind==JsonValueKind.Number&&row.GetProperty("home")[0].GetSingle()==sites[i].Home[0]&&row.GetProperty("points").GetArrayLength()==sites[i].Points.Length,"ordered numeric sites are not culture-dependent Godot strings");
        }
        TestAssert.Equal(Convert.ToHexString(SHA256.HashData(seal.Payload)),seal.Digest,"external seal hashes actual payload, not self-reported embedded hash");
        var parsed=ReadJointData(seal.Payload,seal.Digest,context);
        TestAssert.True(parsed.Count==50&&parsed[0].StableId=="canyon.v2.resident.low-ceiling-bridge.01"&&parsed[0].Points.Count==3,"consumer restores exact identity/order/closed points");
        for(int i=0;i<50;i++)
        {
            var actual=parsed[i];var expected=sites[i];
            TestAssert.True(actual.StableId==expected.StableId&&actual.Home==new Vector2(expected.Home[0],expected.Home[1])&&
                actual.Territory==new Rect2(expected.Territory[0],expected.Territory[1],expected.Territory[2],expected.Territory[3])&&
                actual.Occupancy==new Rect2(expected.Occupancy[0],expected.Occupancy[1],expected.Occupancy[2],expected.Occupancy[3])&&
                actual.Points.SequenceEqual(expected.Points.Select(p=>new Vector2(p[0],p[1]))),"all fifty complete numeric rows round-trip in exact point order");
        }
        int rejected=0;
        void Reject(string label,byte[] bytes,string digest= null,string expectedContext=null)
        {
            bool denied=false;
            try { _=ReadJointData(bytes,digest??Convert.ToHexString(SHA256.HashData(bytes)),expectedContext??context); }
            catch(ArgumentException error) { denied=error.Message.StartsWith("joint.",StringComparison.Ordinal); }
            TestAssert.True(denied,"joint consumer must reject before native allocation: "+label);rejected++;
        }
        byte[] Make(JointSiteDto[] rows,int schema=1,string kind="final-joint")=>JsonSerializer.SerializeToUtf8Bytes(new JointPayloadDto(kind,schema,context,rows),JointJson);
        // Re-sealed semantic controls are test-owned author inputs, NOT permission to edit a trusted receipt.
        Reject("missing",Make(sites.Skip(1).ToArray()));
        var duplicate=sites.ToArray();duplicate[1]=duplicate[1] with{StableId=sites[0].StableId};Reject("duplicate",Make(duplicate));
        var unknown=sites.ToArray();unknown[0]=unknown[0] with{StableId="canyon.v2.resident.low-ceiling-bridge.99"};Reject("unknown",Make(unknown));
        var swapped=sites.ToArray();(swapped[0],swapped[1])=(swapped[1],swapped[0]);Reject("row_order",Make(swapped));
        var moved=sites.ToArray();moved[0]=moved[0] with{Home=new[]{sites[0].Home[0]+1,sites[0].Home[1]}};Reject("home_mismatch",Make(moved));
        var expanded=sites.ToArray();var box=expanded[0].Territory.ToArray();box[2]+=64;expanded[0]=expanded[0] with{Territory=box};Reject("territory_expansion",Make(expanded));
        var shortPoint=sites.ToArray();shortPoint[0]=shortPoint[0] with{Home=new[]{1f}};Reject("point_shape",Make(shortPoint));
        var open=sites.ToArray();open[0]=open[0] with{Points=sites[0].Points.Take(2).ToArray()};Reject("open_route",Make(open));
        var degenerate=sites.ToArray();degenerate[0]=degenerate[0] with{Points=new[]{sites[0].Home,sites[0].Home,sites[0].Home}};Reject("no_motion",Make(degenerate));
        int turret=Array.FindIndex(CanyonResidentCatalog.All.ToArray(),x=>x.Archetype==ResidentArchetype.Turret);
        var badTurret=sites.ToArray();badTurret[turret]=badTurret[turret] with{Points=new[]{sites[turret].Home,sites[turret].Home}};Reject("turret_multiple",Make(badTurret));
        var zero=sites.ToArray();zero[0]=zero[0] with{Occupancy=new[]{1f,2f,0f,4f}};Reject("empty_occupancy",Make(zero));
        var overflow=sites.ToArray();overflow[0]=overflow[0] with{Occupancy=new[]{float.MaxValue,0,float.MaxValue,1}};Reject("overflowed_rectangle_end",Make(overflow));
        Reject("schema",Make(sites,2));Reject("kind",Make(sites,1,"accepted-bucket"));
        Reject("context",seal.Payload,seal.Digest,new string('B',64));
        Reject("digest",seal.Payload,new string('0',64));
        Reject("malformed",System.Text.Encoding.UTF8.GetBytes("{"));
        int flyer=Array.FindIndex(CanyonResidentCatalog.All.ToArray(),x=>x.Archetype==ResidentArchetype.Flyer);
        var reordered=sites.ToArray();var h=sites[flyer].Home;
        reordered[flyer]=reordered[flyer] with{Points=new[]{h,new[]{h[0],h[1]+16},new[]{h[0]+16,h[1]},h.ToArray()}};
        var first=EncodeJoint(reordered,context);var reversed=reordered.ToArray();
        TestAssert.True(ReadJointData(first.Payload,first.Digest,context)[flyer].Points.SequenceEqual(reordered[flyer].Points.Select(p=>new Vector2(p[0],p[1]))),"four-point flyer positive preserves all ordered interior points before seal-tamper control");
        reversed[flyer]=reversed[flyer] with{Points=new[]{h,new[]{h[0]+16,h[1]},new[]{h[0],h[1]+16},h.ToArray()}};
        Reject("geometry_legal_order_tamper",Make(reversed),first.Digest);
        TestAssert.Equal(18,rejected,"eighteen independent pre-allocation controls executed");
        byte[] mutable=seal.Payload.ToArray();var retained=ReadJointData(mutable,seal.Digest,context);Array.Fill(mutable,(byte)0);
        TestAssert.True(retained[0].Home==parsed[0].Home&&retained[0].Points.SequenceEqual(parsed[0].Points),"parsed values own their numeric copies, not live payload/DTO arrays");
        Rect2[] tangent={new(0,0,64,64),new(0,112,64,64)};
        TestAssert.True(JointCorridorsHaveSpacing(tangent),"two full Grow24 corridors may be exactly tangent");
        Rect2[] settled={tangent[0],new(0,111.9f,64,64)};
        TestAssert.True(!JointCorridorsHaveSpacing(settled),"actual settled-height path must reject a0.1px Grow24 overlap even when physical bodies remain disjoint");
        GD.Print("RESIDENT_JOINT_CODEC_OK identities=50 numeric=true ordered=true native_geometry=false actor_mounts=0");
    }

    private static bool JointCorridorsHaveSpacing(IReadOnlyList<Rect2> corridors)
    {
        for(int i=0;i<corridors.Count;i++)
        for(int j=i+1;j<corridors.Count;j++)
            if(corridors[i].Grow(24).Intersects(corridors[j].Grow(24)))return false;
        return true;
    }

    private static float[] JointRect(Rect2 r)=>new[]{r.Position.X,r.Position.Y,r.Size.X,r.Size.Y};

    private static string WriteJointContext(string storage,IReadOnlyDictionary<PrologueSectorId,string> paths,
        string leaves,IReadOnlyList<ResidentKeepOut> keep)
    {
        var files=new List<(string Label,string Path)>();
        string caseRoot=Directory.GetParent(storage)!.FullName;
        files.Add(("owner-source-before",Path.Combine(caseRoot,"source-before.txt")));
        files.Add(("owner-helpers-before",Path.Combine(caseRoot,"helpers-before.txt")));
        TestAssert.Equal(5,paths.Count,"joint context binds all five actually mounted saved sectors");
        foreach(var pair in paths.OrderBy(x=>x.Key))files.Add(("sector:"+pair.Key,ProjectSettings.GlobalizePath(pair.Value)));
        string profiles=Path.Combine(storage,"leaf-source-profiles");
        TestAssert.Equal(24,Directory.GetFiles(leaves,"*.tscn").Length,"context includes all actual saved subtype leaves");
        TestAssert.Equal(24,Directory.GetFiles(profiles,"*.tres").Length,"context includes all actual exported profiles");
        foreach(var directory in new[]{("leaf",leaves),("profile",profiles)})
            foreach(string file in Directory.EnumerateFiles(directory.Item2,"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.Ordinal))
                files.Add((directory.Item1+":"+Path.GetRelativePath(directory.Item2,file).Replace('\\','/'),file));
        var context=new {
            kind="actual-joint-context",schema=1,
            files=files.Select(x=>new{label=x.Label,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x.Path)))}).ToArray(),
            reservations=keep.OrderBy(x=>x.SourceId,StringComparer.Ordinal).ThenBy(x=>x.Kind)
                .Select(x=>new{source=x.SourceId,kind=x.Kind.ToString(),bounds=JointRect(x.WorldBounds)}).ToArray()
        };
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(context,JointJson);
        string output=Path.Combine(storage,"candidate-final-joint-context.json");
        TestAssert.True(!File.Exists(output),"joint context never overwrites another case/version");File.WriteAllBytes(output,bytes);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed record JointPrepared(JointRuntimeSite Site,CharacterBody2D Actor);

    // Transactional preparation: no AddChild until EVERY decoded identity has
    // passed its actual leaf/full-body/native corridor/spacing check.
    private static List<JointPrepared> PrepareJoint(NativeFixture fixture,CensusLeafSource source,
        byte[] bytes,string expectedDigest,string expectedContext,IReadOnlyList<ResidentKeepOut> keep)
    {
        var sites=ReadJointData(bytes,expectedDigest,expectedContext);
        var result=new List<JointPrepared>();
        try
        {
            for(int i=0;i<sites.Count;i++)
            {
                var site=sites[i];var slot=CanyonResidentCatalog.All[i];
                var actor=source.Configure(slot,site.Points.ToArray(),site.Territory);result.Add(new(site,actor));
                if(actor.IsInsideTree())throw new ArgumentException("joint.preparation_inside_tree");
                Vector2 low=new(site.Points.Min(p=>p.X),site.Points.Min(p=>p.Y)),high=new(site.Points.Max(p=>p.X),site.Points.Max(p=>p.Y));
                bool ground=slot.Archetype is not(ResidentArchetype.Flyer or ResidentArchetype.Diver);
                var outward=ResidentPlacementGeometry.CheckCorridor(actor,new(0,low),new(0,high),fixture.Space,fixture.Scope,site.Territory,ground,keep);
                var backward=ResidentPlacementGeometry.CheckCorridor(actor,new(0,high),new(0,low),fixture.Space,fixture.Scope,site.Territory,ground,keep);
                if(!outward.CorridorClear||!backward.CorridorClear)
                    throw new ArgumentException("joint.native_geometry:"+site.StableId+":"+string.Join(",",outward.Issues.Concat(backward.Issues)));
                if(site.Occupancy!=outward.WorldCorridorBounds.Grow(24)||site.Occupancy!=backward.WorldCorridorBounds.Grow(24))
                    throw new ArgumentException("joint.native_occupancy:"+site.StableId);
                if(sites.Take(i).Any(x=>x.Occupancy.Intersects(site.Occupancy)))throw new ArgumentException("joint.native_spacing:"+site.StableId);
            }
            return result;
        }
        catch
        {
            foreach(var item in result)if(GodotObject.IsInstanceValid(item.Actor))item.Actor.Free();
            throw;
        }
    }

    private static async Task RunJointFreezeContracts(SceneTree tree,NativeFixture fixture,CensusLeafSource source,CensusJointSeal seal,string storage)
    {
        TestAssert.True(seal!=null,"actual final joint50 must produce a frozen numeric payload before actor mounting");
        TestAssert.True(seal.Reservations!=null&&seal.Reservations.Count(x=>x.Kind==ResidentKeepOutKind.OriginalEnemy)==36,"actual captured old26 primary/alternate36 reservations retained");
        string path=Path.Combine(storage,"candidate-final-joint.json"),receipt=Path.Combine(storage,"candidate-final-joint-receipt.json");
        TestAssert.True(!File.Exists(path)&&!File.Exists(receipt),"joint payload and independent receipt never overwrite another case/version");
        File.WriteAllBytes(path,seal.Payload);
        File.WriteAllBytes(receipt,JsonSerializer.SerializeToUtf8Bytes(new{kind="test-only-final-joint-receipt",schema=1,payloadSha256=seal.Digest,
            contextSha256=seal.ContextDigest,identities=50,canonicalChanged=false,formalSpawned=0,nativeMotionCertified=false},JointJson));
        TestAssert.Equal(seal.ContextDigest,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(storage,"candidate-final-joint-context.json")))),"trusted context receipt matches persisted actual inputs");
        string before=fixture.CollisionSnapshot();
        var decoded=ReadJointData(File.ReadAllBytes(path),seal.Digest,seal.ContextDigest);
        byte[] runtimeBytes=Godot.FileAccess.GetFileAsBytes(ResidentAuthoredLayout.ResourcePath);
        var runtimeSites=ReadJointData(runtimeBytes,ResidentAuthoredLayout.PayloadSha256,ResidentAuthoredLayout.SourceContextSha256);
        for(int i=0;i<50;i++)
            TestAssert.True(runtimeSites[i].StableId==decoded[i].StableId&&runtimeSites[i].Home==decoded[i].Home&&
                runtimeSites[i].Territory==decoded[i].Territory&&runtimeSites[i].Occupancy==decoded[i].Occupancy&&
                runtimeSites[i].Points.SequenceEqual(decoded[i].Points),
                "production versioned input exactly matches all current independently selected numeric rows and ordered paths");
        // Exercise the exact production res:// input, not just a newly selected
        // test payload. The historical context string grants no current native
        // authority: the full current 143-world checks below still have to pass.
        var prepared=PrepareJoint(fixture,source,runtimeBytes,
            ResidentAuthoredLayout.PayloadSha256,ResidentAuthoredLayout.SourceContextSha256,seal.Reservations);
        try
        {
            TestAssert.Equal(50,prepared.Count,"all fifty original ordered identities prepare off-tree as one transaction");
            var rows=JsonSerializer.Deserialize<JointPayloadDto>(seal.Payload,JointJson)!.Sites;
            int rejected=0;
            void RejectNative(string label,JointSiteDto[] changed,string reason,string expectedId,string expectedIssue=null)
            {
                var negative=EncodeJoint(changed,seal.ContextDigest);bool denied=false;string observed="accepted";
                try
                {
                    var unexpected=PrepareJoint(fixture,source,negative.Payload,negative.Digest,seal.ContextDigest,seal.Reservations);
                    foreach(var item in unexpected)item.Actor.Free();
                }
                catch(ArgumentException error)
                {
                    observed=error.Message;denied=observed.StartsWith(reason+expectedId,StringComparison.Ordinal)&&
                        (expectedIssue==null||observed.Contains(expectedIssue,StringComparison.Ordinal));
                }
                TestAssert.True(denied,"re-sealed test-owned native counterexample rejected by intended geometry guard: "+label+" observed="+observed);
                TestAssert.Equal(1,GearDescendants(fixture.Scope).OfType<CharacterBody2D>().Count(),"failed whole package performs zero AddChild: "+label);
                GD.Print("RESIDENT_JOINT_NATIVE_REJECTION "+JsonSerializer.Serialize(new{kind=label,stableId=expectedId,issue=observed,actorMounts=0},JointJson));
                GD.Print("RESIDENT_JOINT_NATIVE_REJECT_OK case="+label+" actor_mounts=0 atomic=true");rejected++;
            }
            var shrunk=rows.ToArray();var occupancy=shrunk[^1].Occupancy.ToArray();occupancy[2]-=1;shrunk[^1]=shrunk[^1] with{Occupancy=occupancy};
            RejectNative("shrunk_occupancy",shrunk,"joint.native_occupancy:",rows[^1].StableId);
            // Pick an actual saved solid inside the original territory of a
            // stationary turret; no invented wall or fixture-only obstacle.
            var wall=(from pair in CanyonResidentCatalog.All.Select((s,i)=>(Slot:s,Index:i))
                where pair.Slot.Archetype==ResidentArchetype.Turret
                from solid in GearDescendants(fixture.Sectors[pair.Slot.Sector].GetNode("Gameplay/Collision")).OfType<CollisionShape2D>()
                where solid.Shape is RectangleShape2D&&!solid.Disabled
                let point=(solid.GlobalTransform*solid.Shape.GetRect()).GetCenter()
                let body=source.Raw(CanyonResidentCatalog.Subtypes.Single(x=>x.Id==pair.Slot.SubtypeId)).GetNode<CollisionShape2D>("BodyCollider")
                where decoded[pair.Index].Territory.Encloses(new Transform2D(0,point)*body.Transform*body.Shape.GetRect())
                select (pair.Index,point)).First();
            var blocked=rows.ToArray();float[] wallPoint={wall.point.X,wall.point.Y};
            blocked[wall.Index]=blocked[wall.Index] with{Home=wallPoint,Points=new[]{wallPoint}};
            RejectNative("actual_solid",blocked,"joint.native_geometry:",rows[wall.Index].StableId,"native_world");
            var hazard=(from pair in CanyonResidentCatalog.All.Select((s,i)=>(Slot:s,Index:i))
                where pair.Slot.Archetype is ResidentArchetype.Flyer or ResidentArchetype.Diver
                from region in seal.Reservations where region.Kind==ResidentKeepOutKind.Hazard
                let point=region.WorldBounds.GetCenter()
                let body=source.Raw(CanyonResidentCatalog.Subtypes.Single(x=>x.Id==pair.Slot.SubtypeId)).GetNode<CollisionShape2D>("BodyCollider")
                where new[]{point,point+new Vector2(16,0)}.All(p=>decoded[pair.Index].Territory.Encloses(new Transform2D(0,p)*body.Transform*body.Shape.GetRect()))
                select (pair.Index,point,region.SourceId)).First();
            var unsafeRows=rows.ToArray();float[] h={hazard.point.X,hazard.point.Y};
            unsafeRows[hazard.Index]=unsafeRows[hazard.Index] with{Home=h,Points=new[]{h,new[]{h[0]+16,h[1]},h.ToArray()}};
            RejectNative("actual_hazard",unsafeRows,"joint.native_geometry:",rows[hazard.Index].StableId,"reserved.Hazard:"+hazard.SourceId);
            TestAssert.Equal(3,rejected,"all three actual native geometry counterexamples executed");
            fixture.AssertNativePreconditions(143);
            TestAssert.Equal(before,fixture.CollisionSnapshot(),"joint decode/preparation/counterexamples do not mutate actual saved shapes or mount partial actors");
            TestAssert.True(prepared.Select(x=>x.Site.StableId).SequenceEqual(CanyonResidentCatalog.All.Select(x=>x.StableId)),"prepared actors retain exact final-selected order, never a reordered easier subset");
            GD.Print($"RESIDENT_JOINT_FROZEN_OK identities=50 prepared_off_tree=50 native_negatives=3 actor_mounts=0 payload_sha256={seal.Digest} context_sha256={seal.ContextDigest} formal_spawned=0 motion_certified=false");
            GD.Print("RESIDENT_AUTHORED_NATIVE_INPUT_OK identities=50 actual_resource=true current_candidate_terrain=143 native_certificate=false formal_spawned=0");
            await RunJointNativePatrol(tree,fixture,prepared,seal.Reservations,storage);
        }
        finally{foreach(var item in prepared)if(GodotObject.IsInstanceValid(item.Actor))item.Actor.Free();}
    }

    private static async Task RunJointNativePatrol(SceneTree tree,NativeFixture fixture,IReadOnlyList<JointPrepared> prepared,
        IReadOnlyList<ResidentKeepOut> keep,string storage)
    {
        TestAssert.Equal(50,prepared.Count,"native patrol does not reduce fifty to easier examples");
        TestAssert.True(Engine.PhysicsTicksPerSecond==60&&Engine.TimeScale==1,"patrol budget is real sixty-Hz normal-time physics");
        foreach(var item in prepared)
        {
            float length=item.Site.Points.Zip(item.Site.Points.Skip(1),(a,b)=>a.DistanceTo(b)).Sum();
            TestAssert.True(MathF.Ceiling(length/48*60)+60+item.Site.Points.Count*2<=720,"predeclared twelve-second path-length budget covers exact frozen ordinary patrol: "+item.Site.StableId);
        }
        int[] hp=new int[50],healed=new int[50],nextPoint=new int[50];
        var visits=Enumerable.Range(0,50).Select(_=>new List<int>()).ToArray();
        var originalColliders=GearDescendants(fixture.Scope).OfType<CollisionShape2D>().Where(x=>x.Shape!=null)
            .Select(x=>(Node:x,Id:x.GetInstanceId(),Pose:x.GlobalTransform,Shape:x.Shape.GetInstanceId(),Bounds:x.Shape.GetRect(),x.Disabled,x.OneWayCollision)).ToArray();
        Vector2[] previous=new Vector2[50];Rect2[] envelopes=new Rect2[50],shapeRects=new Rect2[50];Transform2D[] colliderTransforms=new Transform2D[50];
        float[] maxStep=new float[50],certifiedY=Enumerable.Repeat(float.NaN,50).ToArray();
        HealthComponent[] health=new HealthComponent[50];CollisionShape2D[] bodies=new CollisionShape2D[50];
        int samples=0,settledChecks=0;ulong lastPhysics=0,firstPhysics=0;Exception failure=null;
        var nativeWatch=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<50;i++)
        {
            int capture=i;var item=prepared[i];var actor=item.Actor;
            TestAssert.True(!actor.IsInsideTree(),"all fifty stay off-tree until complete validation finished");
            health[i]=actor.GetNode<HealthComponent>("Health");hp[i]=health[i].MaxHealth;
            TestAssert.True(hp[i]>0,"actual saved leaf retains a positive original maximum");
            health[i].Healed+=(_,_)=>healed[capture]++;
            foreach(var shape in GearDescendants(actor).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))
                shape.Shape=fixture.Own((Shape2D)shape.Shape.Duplicate());
            bodies[i]=actor.GetNode<CollisionShape2D>("BodyCollider");shapeRects[i]=bodies[i].Shape.GetRect();
            colliderTransforms[i]=bodies[i].Transform;
            var parent=fixture.Sectors[CanyonResidentCatalog.All[i].Sector].GetNode<Node2D>("Gameplay/Actors");
            actor.Transform=parent.GlobalTransform.AffineInverse()*new Transform2D(0,item.Site.Home);
            parent.AddChild(actor); // The ONLY actor pose write is above, before AddChild.
            TestAssert.True(actor.IsNodeReady()&&actor.IsInsideTree()&&actor.IsPhysicsProcessing()&&health[i].CurrentHealth==hp[i],"actual native Ready/physics and original full HP: "+item.Site.StableId);
            previous[i]=actor.GlobalPosition;envelopes[i]=item.Site.Occupancy.Grow(-24);
        }
        TestAssert.Equal(51,GearDescendants(fixture.Scope).OfType<CharacterBody2D>().Count(),"all fifty prepared actors must really mount alongside the owned boss before patrol evidence");
        TestAssert.Equal(50,prepared.Select(x=>x.Actor.GetInstanceId()).Distinct().Count(),"fifty distinct real native actor roots coexist");
        GD.Print($"RESIDENT_JOINT_READY_OBSERVED identities=50 native_bodies=51 setup_ms={nativeWatch.ElapsedMilliseconds} formal_spawned=0");
        var observer=new BasinBoardingTickObserver {ProcessPhysicsPriority=1000};
        observer.Sample=delta=>
        {
            if(failure!=null||samples>=720)return;
            try
            {
                ulong physics=Engine.GetPhysicsFrames();if(physics==lastPhysics)return;
                TestAssert.True(lastPhysics==0||physics==lastPhysics+1,"observer samples every unique native physics frame with no gaps");
                if(firstPhysics==0)firstPhysics=physics;lastPhysics=physics;
                samples++;
                TestAssert.True(Math.Abs(delta-1.0/60)<.00001,"every observation is an actual normal-time physics step");
                for(int i=0;i<50;i++)
                {
                    var item=prepared[i];var actor=item.Actor;var slot=CanyonResidentCatalog.All[i];
                    TestAssert.True(GodotObject.IsInstanceValid(actor)&&actor.IsInsideTree()&&actor.IsPhysicsProcessing(),"resident remains native/active: "+item.Site.StableId);
                    TestAssert.True(health[i].CurrentHealth==hp[i]&&healed[i]==0,"resident HP remains unchanged with no injected healing: "+item.Site.StableId);
                    TestAssert.True(!bodies[i].Disabled&&!bodies[i].TopLevel&&bodies[i].Transform==colliderTransforms[i]&&bodies[i].Shape.GetRect()==shapeRects[i]&&
                        actor.GlobalTransform.X==Vector2.Right&&actor.GlobalTransform.Y==Vector2.Down,"native full collider/root bases stay unchanged: "+item.Site.StableId);
                    Vector2 actual=actor.GlobalPosition;Rect2 bounds=bodies[i].GlobalTransform*bodies[i].Shape.GetRect();
                    maxStep[i]=MathF.Max(maxStep[i],actual.DistanceTo(previous[i]));previous[i]=actual;
                    TestAssert.True(maxStep[i]<=1.2f,"continuous original48px/s native patrol, no teleport: "+item.Site.StableId+" step="+maxStep[i]);
                    bool ground=slot.Archetype is not(ResidentArchetype.Flyer or ResidentArchetype.Diver);
                    if(ground)
                    {
                        TestAssert.True(actor.IsOnFloor()&&MathF.Abs(actual.Y-item.Site.Home.Y)<.6f,"full native ground support maintained: "+item.Site.StableId+" position="+actual);
                        // Native SafeMargin settles a grounded actor slightly off its
                        // authored tangent. Re-certify the WHOLE actual-height path,
                        // not a widened unchecked rectangle or a relaxed body.
                        float offset=actual.Y-item.Site.Home.Y;
                        if(certifiedY[i]!=offset)
                        {
                            settledChecks++;
                            Vector2 low=new(item.Site.Points.Min(p=>p.X),actual.Y),high=new(item.Site.Points.Max(p=>p.X),actual.Y);
                            var outward=ResidentPlacementGeometry.CheckCorridor(actor,new(0,low),new(0,high),fixture.Space,fixture.Scope,item.Site.Territory,true,keep);
                            var backward=ResidentPlacementGeometry.CheckCorridor(actor,new(0,high),new(0,low),fixture.Space,fixture.Scope,item.Site.Territory,true,keep);
                            TestAssert.True(outward.CorridorClear&&backward.CorridorClear,"whole actual settled-height corridor retains full native support/hazard clearance: "+item.Site.StableId+" issues="+string.Join(",",outward.Issues.Concat(backward.Issues)));
                            envelopes[i]=outward.WorldCorridorBounds;certifiedY[i]=offset;
                            TestAssert.True(JointCorridorsHaveSpacing(envelopes),"all fifty actual full-path Grow24 envelopes remain mutually disjoint after native settlement: "+item.Site.StableId);
                        }
                    }
                    TestAssert.True(item.Site.Territory.Encloses(bounds)&&envelopes[i].Encloses(bounds)&&keep.All(x=>!x.WorldBounds.Intersects(bounds)),"actual full body stays inside independently checked path/original territory and outside every keep-out: "+item.Site.StableId+" position="+actual+" body="+bounds+" corridor="+envelopes[i]);
                    if(slot.Archetype==ResidentArchetype.Turret)
                        TestAssert.True(actual.DistanceTo(item.Site.Home)<.6f,"stationary turret stays at actual supported home: "+item.Site.StableId);
                    else if(nextPoint[i]<item.Site.Points.Count&&actual.DistanceTo(item.Site.Points[nextPoint[i]])<=2)
                    {visits[i].Add(samples);nextPoint[i]++;}
                }
                if(samples==1||samples%120==0)GD.Print($"RESIDENT_JOINT_PATROL_PROGRESS physics_steps={samples}/720 native_frame={physics} settled_corridor_checks={settledChecks} elapsed_ms={nativeWatch.ElapsedMilliseconds}");
            }
            catch(Exception error){failure=error;}
        };
        fixture.Scope.AddChild(observer);
        try
        {
            while(samples<720&&failure==null)
            {await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            File.WriteAllBytes(Path.Combine(storage,"candidate-joint-native-patrol.json"),JsonSerializer.SerializeToUtf8Bytes(new{
                kind="test-only-native-patrol-observations",schema=1,authoredPayloadSha256=ResidentAuthoredLayout.PayloadSha256,physicsSteps=samples,settledCorridorChecks=settledChecks,elapsedMs=nativeWatch.ElapsedMilliseconds,error=failure?.Message,formalSpawned=0,attackTested=false,
                actors=prepared.Select((item,i)=>new{stableId=item.Site.StableId,home=new[]{item.Site.Home.X,item.Site.Home.Y},
                    finalPosition=new[]{previous[i].X,previous[i].Y},waypointVisitTicks=visits[i].ToArray(),expectedWaypoints=item.Site.Points.Count,
                    maxStep=maxStep[i],health=hp[i],healed=healed[i],stationary=CanyonResidentCatalog.All[i].Archetype==ResidentArchetype.Turret}).ToArray()
            },JointJson));
            if(failure!=null)throw new InvalidOperationException("joint native patrol observation failed",failure);
            TestAssert.Equal(720,samples,"observe every real physics step for the entire fixed native patrol window");
            TestAssert.True(lastPhysics-firstPhysics+1==720,"720 observations span exactly720 distinct native physics IDs");
            TestAssert.True(JointCorridorsHaveSpacing(envelopes),"final full fifty actual settled-path Grow24 spacing remains intact");
            TestAssert.True(originalColliders.All(x=>GodotObject.IsInstanceValid(x.Node)&&x.Node.GetInstanceId()==x.Id&&x.Node.GlobalTransform==x.Pose&&
                x.Node.Shape.GetInstanceId()==x.Shape&&x.Node.Shape.GetRect()==x.Bounds&&x.Node.Disabled==x.Disabled&&x.Node.OneWayCollision==x.OneWayCollision),"all original map collision shapes/poses/enabled states remain unchanged while residents move");
            for(int i=0;i<50;i++)
            {
                var item=prepared[i];bool ground=CanyonResidentCatalog.All[i].Archetype is not(ResidentArchetype.Flyer or ResidentArchetype.Diver);
                GD.Print($"RESIDENT_JOINT_PATROL_OBSERVED id={item.Site.StableId} physics_steps={samples} waypoints={visits[i].Count}/{item.Site.Points.Count} max_step={maxStep[i]} hp={hp[i]} healed={healed[i]} final={previous[i]} target_present=false");
                if(CanyonResidentCatalog.All[i].Archetype!=ResidentArchetype.Turret)
                    TestAssert.Equal(item.Site.Points.Count,visits[i].Count,"actual positions witness every frozen waypoint in order including final home, not model-index skipping: "+item.Site.StableId);
                var check=ResidentPlacementGeometry.CheckBody(item.Actor,item.Actor.GlobalTransform,fixture.Space,fixture.Scope,item.Site.Territory,ground,keep);
                TestAssert.True(check.BodyGeometryClear,"final complete real native body is clear/supported: "+item.Site.StableId+" issues="+string.Join(",",check.Issues));
            }
            TestAssert.Equal(51,GearDescendants(fixture.Scope).OfType<CharacterBody2D>().Count(),"all fifty still coexist with actual boss after full patrol observations");
            GD.Print("RESIDENT_JOINT_PATROL_OK identities=50 actual_ready=50 native_bodies=51 closed_patrols=41 stationary_turrets=9 physics_steps=720 no_pose_writes_after_mount=true healed=0 target_present=false attack_tested=false formal_spawned=0 normal_world=false");
        }
        finally{observer.Sample=null;if(GodotObject.IsInstanceValid(observer))observer.Free();}
    }
}
