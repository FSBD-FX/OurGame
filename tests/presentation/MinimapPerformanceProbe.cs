using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;
using Environment = System.Environment;

namespace Knight.Tests.Presentation;

// Test-only raw local-update timing. Never described as rendered FPS proof.
public static class MinimapPerformanceProbe
{
    public static void Oracle(MinimapTerrainCatalog catalog,HeadlampOcclusionFrame nativeFrame,Vector2 nativeActor,Rect2[] nativeLoaded,string root,string oraclePath)
    {
        bool compare=!string.IsNullOrEmpty(oraclePath);
        string path=compare?oraclePath:Path.Combine(root,"pre-floyd-behavior-oracle.bin");
        using var file=new FileStream(path,compare?FileMode.Open:FileMode.CreateNew,compare?System.IO.FileAccess.Read:System.IO.FileAccess.Write,FileShare.Read);
        using var writer=compare?null:new BinaryWriter(file);
        using var reader=compare?new BinaryReader(file):null;
        Vector2 actor=nativeActor;Rect2[] loaded=nativeLoaded;HeadlampOcclusionFrame frame=nativeFrame;
        if(!compare)
        {
            writer.Write(1);byte[] values=catalog.SerializedValues;writer.Write(values.Length);writer.Write(values);
            V(writer,actor);writer.Write(loaded.Length);foreach(var r in loaded){V(writer,r.Position);V(writer,r.Size);}
            writer.Write(frame.Shapes.Count);
            foreach(var s in frame.Shapes){V(writer,s.WorldToLocal.X);V(writer,s.WorldToLocal.Y);V(writer,s.WorldToLocal.Origin);V(writer,s.LocalBounds.Position);V(writer,s.LocalBounds.Size);writer.Write(s.BodyInstanceId);writer.Write(s.OwnerId);writer.Write(s.ShapeIndex);}
            writer.Write(1120);
        }
        else
        {
            TestAssert.Equal(1,reader.ReadInt32(),"immutable behavior oracle schema");
            int length=reader.ReadInt32();TestAssert.True(length>0 && length<=80*MinimapTerrainCatalog.MaximumBits,"bounded oracle catalog");
            TestAssert.True(reader.ReadBytes(length).AsSpan().SequenceEqual(catalog.SerializedValues),"oracle uses exact same actual saved catalog");
            actor=R(reader);int count=reader.ReadInt32();TestAssert.True(count is >0 and <=64,"bounded oracle loaded bounds");
            loaded=new Rect2[count];for(int i=0;i<count;i++)loaded[i]=new(R(reader),R(reader));
            count=reader.ReadInt32();TestAssert.True(count is >=0 and <=256,"bounded actual native snapshot");
            var shapes=new HeadlampOccluder[count];for(int i=0;i<count;i++)shapes[i]=new(new(R(reader),R(reader),R(reader)),new(R(reader),R(reader)),reader.ReadUInt64(),reader.ReadUInt32(),reader.ReadInt32());
            frame=new HeadlampOcclusionFrame(shapes,true);TestAssert.Equal(1120,reader.ReadInt32(),"fixed original workload unchanged");
        }
        var scheduler=new MinimapCandidateScheduler(catalog);var model=new ExplorationMapModel(catalog);
        for(int sample=-120;sample<1000;sample++)
        {
            Vector2 position=sample<0?actor:actor+new Vector2((sample%120)*.5f,0);
            using var rowStream=new MemoryStream();using var row=new BinaryWriter(rowStream);
            row.Write(sample);V(row,position);scheduler.BeginStamp(position);int candidates=0;
            for(int k=0;k<64;k++){if(!scheduler.TryNext(out int id,out bool kind))break;row.Write(id);row.Write(kind);candidates++;}
            row.Write(-1);row.Write(candidates);
            model.Advance(new(position,(ulong)(sample+121),1,true,frame,loaded));
            TestAssert.True(candidates==model.LastCandidateCount && scheduler.LastNodeExpansions==model.LastSchedulerNodeExpansions && scheduler.LastQueryRebuilds==model.LastSchedulerQueryRebuilds,"independent candidate emission matches actual model work each stamp");
            row.Write(model.LastCandidateCount);row.Write(model.LastChargedOccluderTests);row.Write(model.LastFiniteOccluderSweepTests);row.Write(model.LastSchedulerNodeExpansions);row.Write(model.LastSchedulerQueryRebuilds);row.Write(model.RevisionCounter);
            var bits=model.CaptureSave();row.Write(bits.FreeCellsBase64);row.Write(bits.BoundarySegmentsBase64);
            byte[] actual=rowStream.ToArray();
            if(compare)
            {
                int length=reader.ReadInt32();TestAssert.True(length>0 && length<100000,"bounded exact oracle row");
                TestAssert.True(reader.ReadBytes(length).AsSpan().SequenceEqual(actual),"Floyd exact per-stamp IDs/kinds/work/revision/complete bitsets equal BEFORE patch oracle, sample="+sample);
            }
            else{writer.Write(actual.Length);writer.Write(actual);}
        }
        if(compare)TestAssert.Equal(file.Length,file.Position,"no unvalidated trailing oracle values");
        GD.Print($"MINIMAP_FLOYD_ORACLE compare={compare} stamps=1120 moving=1000 warmup=120 exact_ids_kinds_work_full_bitsets=True path={path} budget_timing=False");
        static void V(BinaryWriter w,Vector2 v){w.Write(v.X);w.Write(v.Y);}
        static Vector2 R(BinaryReader r)=>new(r.ReadSingle(),r.ReadSingle());
    }

    public static void Profile(MinimapTerrainCatalog catalog,HeadlampOcclusionFrame frame,Vector2 actor,Rect2[] loaded,string root)
    {
        // Instrument the SAME production scheduler separately, retaining near-first attempts.
        // No timings here are budget certification: per-candidate timestamp overhead is explicit.
        var scheduler=new MinimapCandidateScheduler(catalog);
        var reference=new ExplorationMapModel(catalog);
        var cells=new bool[catalog.CellCount];var boundaries=new bool[catalog.BoundaryCount];
        var csv=new StringBuilder("index,begin_ticks,next_ticks,coverage_ticks,visibility_ticks,total_ticks,candidates,charged,sweeps,expansions,rebuilds,state_matches_reference\n");
        long[] begin=new long[1000],next=new long[1000],coverage=new long[1000],visibility=new long[1000],total=new long[1000];
        int[] candidateCounts=new int[1000],chargedCounts=new int[1000],sweepCounts=new int[1000],expansionCounts=new int[1000],rebuildCounts=new int[1000];
        ulong stamp=1;bool same=true;
        for(int sample=-120;sample<1000;sample++)
        {
            Vector2 position=sample<0?actor:actor+new Vector2((sample%120)*.5f,0);
            long started=Stopwatch.GetTimestamp(),t=started;
            scheduler.BeginStamp(position);long b=Stopwatch.GetTimestamp()-t,n=0,c=0,v=0;int candidates=0,charged=0,sweeps=0;
            for(int k=0;k<64;k++)
            {
                t=Stopwatch.GetTimestamp();bool found=scheduler.TryNext(out int id,out bool boundary);n+=Stopwatch.GetTimestamp()-t;
                if(!found)break;candidates++;
                if(boundary?boundaries[id]:cells[id])continue;
                bool covered,allowed;int actual;
                t=Stopwatch.GetTimestamp();
                if(boundary)
                {
                    var segment=catalog.Boundary(id);var solid=catalog.Solid(segment.ShapeId);
                    Vector2 a=solid.LocalToWorld*segment.FromLocal,z=solid.LocalToWorld*segment.ToLocal;
                    covered=Within(position,a)&&Within(position,z)&&MinimapLoadedCoverage.Boundary(position,a,z,loaded);
                    c+=Stopwatch.GetTimestamp()-t;if(!covered)continue;charged+=frame.Shapes.Count;
                    t=Stopwatch.GetTimestamp();allowed=MinimapVisibility.BoundaryAllowed(position,solid,segment,frame,out actual);v+=Stopwatch.GetTimestamp()-t;
                }
                else
                {
                    var rect=catalog.Cell(id);
                    covered=Within(position,rect.Position)&&Within(position,rect.End)&&Within(position,new(rect.End.X,rect.Position.Y))&&Within(position,new(rect.Position.X,rect.End.Y))&&MinimapLoadedCoverage.Cell(position,rect,loaded);
                    c+=Stopwatch.GetTimestamp()-t;if(!covered)continue;charged+=frame.Shapes.Count;
                    t=Stopwatch.GetTimestamp();allowed=MinimapVisibility.CellAllowed(position,rect,frame,out actual);v+=Stopwatch.GetTimestamp()-t;
                }
                sweeps+=actual;if(allowed){if(boundary)boundaries[id]=true;else cells[id]=true;}
            }
            long elapsed=Stopwatch.GetTimestamp()-started;
            reference.Advance(new(position,stamp++,1,true,frame,loaded));
            bool matches=reference.LastCandidateCount==candidates && reference.LastChargedOccluderTests==charged && reference.LastFiniteOccluderSweepTests==sweeps && reference.LastSchedulerNodeExpansions==scheduler.LastNodeExpansions && reference.LastSchedulerQueryRebuilds==scheduler.LastQueryRebuilds;
            // Full state equality outside measured sections, not only matching favorable aggregate counts.
            for(int i=0;i<cells.Length;i++)matches &= cells[i]==reference.IsCellExplored(i);
            for(int i=0;i<boundaries.Length;i++)matches &= boundaries[i]==reference.IsBoundaryExplored(i);
            same &= matches;
            if(sample<0)continue;
            begin[sample]=b;next[sample]=n;coverage[sample]=c;visibility[sample]=v;total[sample]=elapsed;
            candidateCounts[sample]=candidates;chargedCounts[sample]=charged;sweepCounts[sample]=sweeps;expansionCounts[sample]=scheduler.LastNodeExpansions;rebuildCounts[sample]=scheduler.LastQueryRebuilds;
            csv.AppendLine($"{sample},{b},{n},{c},{v},{elapsed},{candidates},{charged},{sweeps},{scheduler.LastNodeExpansions},{scheduler.LastQueryRebuilds},{matches}");
        }
        File.WriteAllText(Path.Combine(root,"actual-catalog-moving-attribution.csv"),csv.ToString());
        long[] overhead=new long[1000];for(int i=0;i<1000;i++){long t=Stopwatch.GetTimestamp();overhead[i]=Stopwatch.GetTimestamp()-t;}
        File.WriteAllLines(Path.Combine(root,"attribution-clock-overhead.csv"),overhead.Select((v,i)=>$"{i},{v}"));
        GD.Print($"MINIMAP_PROFILE samples=1000 warmup=120 frequency={Stopwatch.Frequency} begin_sum_ticks={begin.Sum()} next_sum_ticks={next.Sum()} coverage_sum_ticks={coverage.Sum()} visibility_sum_ticks={visibility.Sum()} total_sum_ticks={total.Sum()} clock_overhead_p95_ticks={overhead.OrderBy(v=>v).ElementAt(949)} state_matches_production={same} budget_certification=False");
        TestAssert.True(same,"instrumented attribution exactly matches actual production model state and work counts");
        static bool Within(Vector2 a,Vector2 p){double x=(double)p.X-a.X,y=(double)p.Y-a.Y;return x*x+y*y<=25600d;}
    }

    public static void Run(MinimapTerrainCatalog catalog,HeadlampOcclusionFrame frame,Vector2 actor,Rect2[] loaded,string root)
    {
        int thread=Environment.CurrentManagedThreadId;
        long preparationBytes=GC.GetAllocatedBytesForCurrentThread(), preparationStart=Stopwatch.GetTimestamp();
        var model=new ExplorationMapModel(catalog);
        long preparationTicks=Stopwatch.GetTimestamp()-preparationStart;
        preparationBytes=GC.GetAllocatedBytesForCurrentThread()-preparationBytes;
        var rows=new StringBuilder("arm,index,ticks,managed_bytes,candidates,charged,sweeps,expansions,rebuilds,revision\n");
        ulong stamp=1;
        long[] overhead=new long[1000];
        for(int i=0;i<overhead.Length;i++){long t=Stopwatch.GetTimestamp();overhead[i]=Stopwatch.GetTimestamp()-t;}
        File.WriteAllLines(Path.Combine(root,"local-measurement-overhead.csv"),overhead.Select((v,i)=>$"{i},{v}"));
        for(int i=0;i<120;i++)model.Advance(new(actor,stamp++,1,true,frame,loaded));
        long idle=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<600;i++)model.Advance(new(actor,stamp++,1,false,frame,loaded));
        idle=GC.GetAllocatedBytesForCurrentThread()-idle;
        TestAssert.Equal(0L,idle,"actual catalog warmed idle managed allocation0");
        bool budgets=true;
        foreach(string arm in new[]{"stationary","moving"})
        {
            model.Clear();
            for(int i=0;i<120;i++)model.Advance(new(actor,stamp++,1,true,frame,loaded));
            ulong initialRevision=model.RevisionCounter;
            var ticks=new long[1000];var allocations=new long[1000];var candidates=new int[1000];var charged=new int[1000];var sweeps=new int[1000];var expansions=new int[1000];var rebuilds=new int[1000];var revisions=new ulong[1000];
            int gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2);
            for(int i=0;i<1000;i++)
            {
                Vector2 position=arm=="moving"?actor+new Vector2((i%120)*.5f,0):actor;
                long bytes=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();
                model.Advance(new(position,stamp++,1,true,frame,loaded));
                ticks[i]=Stopwatch.GetTimestamp()-start;allocations[i]=GC.GetAllocatedBytesForCurrentThread()-bytes;
                candidates[i]=model.LastCandidateCount;charged[i]=model.LastChargedOccluderTests;sweeps[i]=model.LastFiniteOccluderSweepTests;
                expansions[i]=model.LastSchedulerNodeExpansions;rebuilds[i]=model.LastSchedulerQueryRebuilds;revisions[i]=model.RevisionCounter;
            }
            for(int i=0;i<1000;i++)rows.AppendLine($"{arm},{i},{ticks[i]},{allocations[i]},{candidates[i]},{charged[i]},{sweeps[i]},{expansions[i]},{rebuilds[i]},{revisions[i]}");
            var sorted=ticks.OrderBy(v=>v).ToArray();double p95=sorted[949]*1000d/Stopwatch.Frequency,p99=sorted[989]*1000d/Stopwatch.Frequency;
            bool pass=allocations.Max()<=4096 && p95<=.5 && p99<=1;
            budgets &= pass;
            GD.Print($"MINIMAP_LOCAL_PERFORMANCE arm={arm} samples=1000 warmup=120 p95ms={p95:R} p99ms={p99:R} maxbytes={allocations.Max()} maxcandidate={candidates.Max()} maxcharged={charged.Max()} maxsweeps={sweeps.Max()} maxexpansions={expansions.Max()} rebuilds={rebuilds.Sum()} first_reveal_sample={Array.FindIndex(revisions,v=>v>initialRevision)} gc={GC.CollectionCount(0)-gc0}/{GC.CollectionCount(1)-gc1}/{GC.CollectionCount(2)-gc2} budget={pass}");
        }
        File.WriteAllText(Path.Combine(root,"actual-catalog-local-updates.csv"),rows.ToString());
        GD.Print($"MINIMAP_LOCAL_PREPARATION ticks={preparationTicks} frequency={Stopwatch.Frequency} managed_bytes={preparationBytes} bitset_bytes={model.BitsetBytes} directory_bytes={model.DirectoryBytes} cache_bytes={model.CandidateCacheBytes} cells={catalog.CellCount} boundaries={catalog.BoundaryCount} idle600bytes={idle} overhead_p95_ticks={overhead.OrderBy(v=>v).ElementAt(949)} same_thread={Environment.CurrentManagedThreadId==thread} rendered_timing=False forced_gc=False");
        TestAssert.True(budgets && Environment.CurrentManagedThreadId==thread,"actual catalog local-update budgets, raw retained before assertion");
    }
}
