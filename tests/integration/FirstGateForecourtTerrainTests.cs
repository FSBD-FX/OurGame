using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using Environment = System.Environment;
using Godot;
using Knight.Build.Prologue;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Player;
using Knight.Combat;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Tools;

namespace Knight.Tests.Integration;

public static class FirstGateForecourtTerrainTests
{
    private static readonly string[] ColdSources={"FirstGateForecourtCampaign-v344.tscn","forecourt-first-resource.json","forecourt-new.json"};

    public static async Task Run(SceneTree tree,string storage)
    {
        string parent=BuildSavedParent(storage);
        using var packed=ResourceLoader.Load<PackedScene>(parent,cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=packed.Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"forecourt-real-start");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        KeyboardRouteInputDriver driver=null;int recoveries=0,ticks=0;
        void Recovered(string reason)=>recoveries++;
        level.RecoveryCompleted+=Recovered;tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var minimap=level.GetNode<PrologueMinimapController>("MinimapController");
            TestAssert.True(world.ProfileContract?.Id=="knight.first-gate.forecourt.v1" && level.Residents==null &&
                world.MountedSectorIds.Count==1 && minimap.Model.Catalog.CellSize==16 && minimap.Model.Catalog.SolidCount is >48 and <=128,
                "actual saved parent Ready mounts the new16 profile, not borrowed old enemies/map");
            TestAssert.Equal(8,((ShaderMaterial)level.GetNode<Knight.Presentation.Fog.PrologueFogCanvas>("Fog/Canvas").Material).GetShaderParameter("field_count").AsInt32(),
                "actual fog runtime receives full bounds plus seven real refuges");
            flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            File.Copy(paths.SavePath,Path.Combine(storage,ColdSources[2]),false);
            var player=level.Player;ulong identity=player.GetInstanceId();
            var input=player.GetNode<PlayerInputReader>("Input");driver=KeyboardRouteInputDriver.Begin(player);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver.ObserveFrame();ticks++;
                // Headless has no render event. Exercise the same production
                // callback, as existing lifecycle fixtures do; never inject bits
                // or call the map's private observer with invented observations.
                typeof(Knight.Presentation.Fog.PrologueFogController)
                    .GetMethod("OnFramePreDraw",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(level.GetNode<Knight.Presentation.Fog.PrologueFogController>("FogController"),null);
                TestAssert.True(ticks<=180 && player.GetInstanceId()==identity && recoveries==0 && player.GetNode<HealthComponent>("Health").CurrentHealth==5,
                    "short start/Q proof retains actual player, HP and zero recoveries");
            }
            driver.Command(InputActions.MoveRight,true);
            while(player.Position.X<560 && ticks<140)await Step();
            driver.ReleaseAll();for(int i=0;i<10;i++)await Step();
            TestAssert.True(player.Position.X is >540 and <610 && player.IsOnFloor(),"native ordinary movement reaches first actual cache without pose injection");
            driver.Command(InputActions.Interact,true);await Step();driver.Command(InputActions.Interact,false);
            for(int i=0;i<4;i++)await Step();
            const string first="knight.first-gate.main.01.resource";
            TestAssert.True(level.Progress.ClaimedRewardIds.SequenceEqual(new[]{first}) && level.Progress.ResourceTotal("mineral")==1 &&
                flow.Save.ExplorationMap?.FormatVersion==2 && minimap.Model.IsCellExplored(29708) && !minimap.Model.IsCellExplored(525231),
                "actual Q grants once in full new profile and saves16 memory while distant free gate area remains dark");
            File.Copy(paths.SavePath,Path.Combine(storage,ColdSources[1]),false);
            var read=PrologueSaveStore.LoadCandidate(paths.SavePath,world.ProfileContract,minimap.Catalog).Data;
            TestAssert.True(!read.WasRecovered && read.ClaimedRewardIds.SequenceEqual(new[]{first}) && read.ExplorationMap?.FormatVersion==2,
                "durable selected slot contains the actual first Q and optional map2, no formal completion");
            GD.Print("FORECOURT_MAP_CALLBACK_CONTROL headless_explicit_predraw=true actual_world_sampler=true saved_start_known=true distant_free_gate_dark=true pixels=false");
            File.WriteAllText(Path.Combine(storage,"forecourt-cold-manifest.json"),JsonSerializer.Serialize(new
                {writerPid=Environment.ProcessId,hashes=ColdSources.Select(f=>Hash(Path.Combine(storage,f))).ToArray()}));
            GD.Print($"FORECOURT_WARM_OK writer_pid={Environment.ProcessId} witnesses=3 actual_Q=1 grid=16 profile_ready=true distant_gate_dark=true full_route=false boss_deployed=false human=false");
        }
        finally{driver?.Dispose();level.RecoveryCompleted-=Recovered;await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
    }

    public static async Task RunCold(SceneTree tree,string storage)
    {
        using var receipt=JsonDocument.Parse(File.ReadAllText(Path.Combine(storage,"forecourt-cold-manifest.json")));
        int writer=receipt.RootElement.GetProperty("writerPid").GetInt32();
        string[] hashes=receipt.RootElement.GetProperty("hashes").EnumerateArray().Select(e=>e.GetString()).ToArray();
        TestAssert.True(writer>0 && writer!=Environment.ProcessId && hashes.Length==3,"distinct cold process with three fixed source witnesses");
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,ColdSources[i])),"cold input hash");
        using var packed=ResourceLoader.Load<PackedScene>(Path.Combine(storage,ColdSources[0]),cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=packed.Instantiate<PrologueLevelController>();var paths=IsolatedPrologueFixture.Configure(level,"forecourt-independent-cold");
        File.Copy(Path.Combine(storage,ColdSources[1]),paths.SavePath,false);string selected=Hash(paths.SavePath);
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");var map=level.GetNode<PrologueMinimapController>("MinimapController");
            TestAssert.True(world.ProfileContract.Id=="knight.first-gate.forecourt.v1" && map.Model.Catalog.CellSize==16 &&
                level.Progress.ClaimedRewardIds.SequenceEqual(new[]{"knight.first-gate.main.01.resource"}) && level.Progress.ResourceTotal("mineral")==1 &&
                world.CheckpointId==world.ProfileContract.Start.StableId && !flow.Save.Completed && !flow.Save.BossDefeated && !map.Model.IsCellExplored(525231),
                "actual cold Continue restores new16 world/grant/start and leaves distant free gate dark");
            TestAssert.True(Math.Abs(level.Player.Position.X-192)<1 && Math.Abs(level.Player.Position.Y-472)<8,"cold actor uses canonical start before any refuge");
            TestAssert.Equal(selected,Hash(paths.SavePath),"cold Continue is readonly on selected isolated slot");
            using var raw=JsonDocument.Parse(File.ReadAllText(Path.Combine(storage,ColdSources[1])));
            var sourceMap=raw.RootElement.GetProperty("explorationMap");
            byte[] expected=Convert.FromBase64String(sourceMap.GetProperty("freeCellsBase64").GetString());
            byte[] actual=Convert.FromBase64String(map.CaptureExploration().FreeCellsBase64);
            TestAssert.True(sourceMap.GetProperty("formatVersion").GetInt32()==2 && expected.Length==98304 &&
                expected.Any(b=>b!=0) && actual.Length==expected.Length && expected.Zip(actual,(a,b)=>(a&b)==a).All(x=>x) &&
                map.Model.IsCellExplored(29708),"cold live model preserves actual nonempty saved16 knowledge, not only empty bytes or core grants");
            var empty=PrologueSaveStore.LoadCandidate(Path.Combine(storage,ColdSources[2]),world.ProfileContract).Data;
            TestAssert.True(!empty.WasRecovered && empty.ClaimedRewardIds.Count==0 && !empty.HasActivatedCheckpoint,"actual new-game witness stays empty");
        }
        finally{await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,ColdSources[i])),"cold source is never rewritten");
        GD.Print($"FORECOURT_COLD_OK writer_pid={writer} reader_pid={Environment.ProcessId} witnesses=3 actual_continue=true grants=1 grid=16 source_readonly=true full_route=false human=false");
    }

    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    internal static string BuildSavedParent(string storage)
    {
        // Break caught: bounds-only/marker-only extension cannot open the native
        // sealed12 exit, create the later rooms or survive a real saved parent.
        Type author=typeof(FirstGateAbyssTerrain).Assembly.GetType("Knight.Build.Prologue.FirstGateForecourtTerrain");
        TestAssert.True(author!=null,"FORECOURT_TERRAIN_RED: physical13–24/G–J terrain must exist, not only allocation metadata");
        var root=(PrologueSector)author.GetMethod("Build").Invoke(null,null);
        try
        {
            Rect2[] solids=root.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>().Select(SolidRect).ToArray();
            TestAssert.True(root.Bounds==new Rect2(0,0,16384,12288) && solids.Length>48 && solids.Length<=128,
                "actual full envelope and native merged structural budget leave room for later enemies/mechanisms");
            foreach(var (at,occupied,label) in new[]{
                (new Vector2(5880,7300),false,"old12 wall is actually opened"),
                (new Vector2(7400,7350),false,"14 low mountain passage"),
                (new Vector2(8800,6800),false,"15 tall buried forecourt"),
                (new Vector2(9700,5900),false,"16 column climb"),
                (new Vector2(9792,6448),true,"16 central rock tooth remains a real blocker"),
                (new Vector2(9792,6720),false,"16 lower side-transfer corridor is not intercepted by old spine"),
                (new Vector2(9860,6608),false,"16 east-catch side dock has native body clearance"),
                (new Vector2(10800,5400),false,"17 side gallery"),
                (new Vector2(12500,5100),false,"19 receiving hall"),
                (new Vector2(13200,4200),false,"20 upper gallery"),
                (new Vector2(14050,6300),false,"21 separate machine shaft"),
                (new Vector2(12700,7400),false,"22 lower return"),
                (new Vector2(13200,8350),false,"23 gate approach"),
                (new Vector2(15100,8200),false,"24 boss forecourt reserved"),
                (new Vector2(15100,8550),true,"forecourt has a real supporting floor"),
                (new Vector2(15840,8300),true,"true gate exterior remains sealed"),
                (new Vector2(8336,6900),false,"G actually connects mountain14 to buried15"),
                (new Vector2(8750,5800),false,"H rises from15 outside the main16 column"),
                (new Vector2(9500,5248),false,"H crosses above16 and rejoins17"),
                (new Vector2(14656,6000),false,"I descends outside21 from20 towards22"),
                (new Vector2(14400,7440),false,"I returns into22 before gate approach"),
                (new Vector2(13312,6100),true,"I is not an unintended19 to mid21 shortcut"),
                (new Vector2(11000,6900),true,"not four-way open between stacked routes"),
                (new Vector2(16000,5000),true,"right exterior is native rock"),
                (new Vector2(7400,7100),true,"G and main14 have an actual dividing roof")})
                TestAssert.Equal(occupied,solids.Any(r=>r.HasPoint(at)),"FORECOURT_GEOMETRY_LITERAL: "+label);
            var caches=root.GetNode("Gameplay/ResourceCaches").GetChildren().OfType<PrologueResourceCache>().ToArray();
            TestAssert.True(caches.Length==54 && caches.Select(c=>c.RewardId).Order().SequenceEqual(ResourceRewardCatalog.StableIds.Order()),
                "all54 distinct registered main/branch reward objects survive, not duplicate identities");
            foreach(var (kind,total) in new[]{("mineral",33),("fiber",9),("salvage",44),("data",12),("echo-memory",2)})
                TestAssert.Equal(total,ResourceRewardCatalog.Total(caches.Select(c=>c.RewardId),kind),"literal full reward grant total "+kind);
            AssertAirConnectivity(solids,caches);
            TestAssert.True(PrologueWorldProfiles.TryResolve("knight.first-gate.forecourt.v1",out var profile),
                "FORECOURT_PROFILE_RED: full terrain resolves its own immutable profile");
            int cellSize=(int)(typeof(PrologueWorldProfile).GetProperty("MinimapCellSize")?.GetValue(profile)??8);
            TestAssert.True(cellSize==16 && profile.DesignBounds==root.Bounds && profile.AuthoredMainSegments==24 &&
                profile.AuthoredBranches==10 && profile.Checkpoints.Count==7 && profile.FogFields.Count==8,
                "full profile explicitly selects16 with actual seven canonical recovery points");
            foreach(string key in new[]{"knight.first-gate.development.v1","knight.first-gate.abyss.v1"})
            {
                TestAssert.True(PrologueWorldProfiles.TryResolve(key,out var old),"old profile still resolves");
                TestAssert.Equal(8,(int)typeof(PrologueWorldProfile).GetProperty("MinimapCellSize").GetValue(old),"old profile grid stays8");
            }
            string leaf=Path.Combine(storage,"FirstGateForecourt01To24-v344.tscn");
            TestAssert.True(!File.Exists(leaf),"new leaf never overwrites old candidate");
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,leaf),"actual full terrain save");
            using var packed=GD.Load<PackedScene>(leaf);
            using var def=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,packed,Vector2.Zero,root.Bounds,null,null);
            var level=BuildPrologueLevel.BuildCandidate(profile,new[]{def});
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var catalog=level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            try
            {
                TestAssert.True(catalog.CellSize==16 && catalog.CellCount==786432 && catalog.SolidCount==solids.Length,
                    "FORECOURT_PARENT_GRID_RED: real BuildCandidate uses profile16, unchanged cap and exact native solids");
                TestAssert.Equal("user://knight/knight.first-gate.forecourt.v1.json",level.GetNode<PrologueGameFlowController>("GameFlow").SavePath,
                    "new full profile has independent default slot before Ready");
                TestAssert.True(level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints.Count==7,
                    "all actual grave lamps are part of the saved map checkpoint table");
                string parent=Path.Combine(storage,"FirstGateForecourtCampaign-v344.tscn");
                TestAssert.Equal(Error.Ok,BuildPrologueLevel.SaveCandidate(level,parent),"paired save recomputation also uses16");
                using var fresh=ResourceLoader.Load<PackedScene>(parent,cacheMode:ResourceLoader.CacheMode.Ignore);
                var copy=fresh.Instantiate<PrologueLevelController>();
                try
                {
                    var map=copy.GetNode<PrologueMinimapController>("MinimapController").Catalog;
                    TestAssert.True(map.CellSize==16 && map.CellCount==786432 && map.SerializedValues.AsSpan().SequenceEqual(catalog.SerializedValues),
                        "native saved parent reload preserves exact16 catalog");
                }
                finally{copy.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Clear();copy.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;copy.Free();}
                GD.Print($"FORECOURT_TERRAIN_AUTHORING_OK nodes={Count(root)} solids={solids.Length} rewards=54 main=24 branches=10 cells=786432 grid=16 saved_parent=true topology_only=true actual_traversal=false boss_deployed=false human=false");
                return parent;
            }
            finally
            {
                var definitions=world.Definitions.ToArray();var snapshots=definitions.Select(d=>d.Scene).Where(s=>s!=null && !ReferenceEquals(s,packed)).Distinct().ToArray();
                world.Definitions.Clear();level.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;
                foreach(var item in definitions)item.Scene=null;level.Free();foreach(var item in definitions)item.Dispose();foreach(var snapshot in snapshots)snapshot.Dispose();catalog.Dispose();
            }
        }
        finally
        {
            var shapes=All(root).OfType<CollisionShape2D>().Select(n=>n.Shape).Distinct().ToArray();
            root.Free();foreach(var shape in shapes)shape.Dispose();
        }
    }
    private static void AssertAirConnectivity(Rect2[] solids,PrologueResourceCache[] caches)
    {
        // Topological clearance, NOT a jump/claw/fall/route or combat certificate.
        // The conservative40x72 box encloses the saved player's r20/h72 capsule.
        const int w=512,h=384;var blocked=new bool[w*h];var seen=new bool[w*h];
        foreach(Rect2 solid in solids)
        {
            int x0=Math.Max(0,(int)Math.Floor((solid.Position.X-20)/32f));
            int x1=Math.Min(w-1,(int)Math.Ceiling((solid.End.X+20)/32f));
            int y0=Math.Max(0,(int)Math.Floor((solid.Position.Y-36)/32f));
            int y1=Math.Min(h-1,(int)Math.Ceiling((solid.End.Y+36)/32f));
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                blocked[y*w+x]|=solid.Intersects(new Rect2(x*32+16-20,y*32+16-36,40,72));
        }
        int Cell(Vector2 at)=>(int)(at.Y/32)*w+(int)(at.X/32);
        int first=Cell(new(5500,7448));TestAssert.True(!blocked[first],"literal old12 start remains clear");
        var queue=new Queue<int>();queue.Enqueue(first);seen[first]=true;
        while(queue.Count>0)
        {
            int i=queue.Dequeue(),x=i%w,y=i/w;
            foreach(var (dx,dy) in new[]{(-1,0),(1,0),(0,-1),(0,1)})
            {int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=w||ny>=h)continue;int n=ny*w+nx;if(!blocked[n]&&!seen[n]){seen[n]=true;queue.Enqueue(n);}}
        }
        foreach(var cache in caches.Where(c=>!c.RewardId.Contains(".main.") || int.Parse(c.RewardId.Split('.')[3])>=13))
        {
            if(cache.RewardId.Contains(".branch.") && cache.RewardId.Split('.')[3][0]<'g')continue;
            TestAssert.True(seen[Cell(cache.Position-new Vector2(0,24))],"new reward approach connects to12 with capsule-enclosing clearance: "+cache.RewardId);
        }
        TestAssert.True(!seen[Cell(new(16000,8300))],"exterior cannot be reached through a bounds-only expansion");
        GD.Print("FORECOURT_CLEARANCE_TOPOLOGY_OK new_rewards=24 capsule_box=40x72 grid=32 movement_certificate=false");
    }
    private static Rect2 SolidRect(StaticBody2D body)
    {var shape=(RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape;return new(body.Position-shape.Size/2,shape.Size);}
    private static IEnumerable<Node> All(Node root)
    {yield return root;foreach(Node child in root.GetChildren())foreach(Node item in All(child))yield return item;}
    private static int Count(Node root)=>1+root.GetChildren().Sum(Count);
}
