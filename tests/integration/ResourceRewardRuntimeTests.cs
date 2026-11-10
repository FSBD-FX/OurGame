using System;
using Environment = System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Interactions;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Save;

namespace Knight.Tests.Integration;

public static class ResourceRewardRuntimeTests
{
    private const string A="knight.first-gate.branch.a.chest";
    private const string B="knight.first-gate.branch.b.chest";
    private static readonly string[] Witnesses={"resource-rewards-all.json","resource-rewards-reset.json","resource-rewards-old-v4.json"};

    public static async Task Run(SceneTree tree,string storage)
    {
        PrologueLevelController level=null;
        try
        {
            level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            var paths=IsolatedPrologueFixture.Configure(level,"resource-reward-Q");
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
            tree.Root.AddChild(level);await Frames(tree,4);flow.StartNew();
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var player=level.Player;
            var input=player.GetNode<PlayerInputReader>("Input");
            for(int i=0;i<90 && (!player.IsOnFloor() || player.Velocity.Length()>.1f);i++) await KeyboardActionRuntimeTests.Frames(tree,input,1);
            TestAssert.True(player.IsOnFloor() && player.Velocity.Length()<=.1f,"real initial player settles for Q without pose/velocity writes");
            int initialHealth=player.GetNode<HealthComponent>("Health").CurrentHealth;
            int initialFragments=level.Progress.CollectedFragmentIds.Count;
            string leafPath=Path.Combine(storage,"resource-cache-candidate.tscn");
            var authored=CreateCandidate();
            using(var packed=new PackedScene())
            {
                TestAssert.Equal(Error.Ok,packed.Pack(authored),"candidate reward leaf packs");
                TestAssert.Equal(Error.Ok,ResourceSaver.Save(packed,leafPath),"candidate leaf saved only to isolated evidence");
            }
            authored.Free();
            using var leaf=GD.Load<PackedScene>(leafPath);
            var cache=Mount(leaf,level,player);
            await KeyboardActionRuntimeTests.Frames(tree,input,3);
            TestAssert.True(!cache.TryInteract(player),"direct call without accepted Q cannot claim");
            bool accepted=await Pulse(tree,input);
            TestAssert.True(accepted,"real saved input accepts fresh Q edge");
            TestAssert.True(level.Progress.ClaimedRewardIds.Contains(A) && cache.IsClaimed,
                "fresh real Q grants registered resource reward before feedback");
            TestAssert.True(cache.FeedbackCount==1 && level.Progress.ResourceTotal("fiber")==3 && cache.IsInsideTree(),"first cache grants3fiber and stays mounted");
            TestAssert.True(!cache.GetNode<Node2D>("AvailableVisual").Visible && cache.GetNode<Node2D>("ClaimedVisual").Visible,"actual skin hook switches only after claim");
            TestAssert.True(PrologueSaveStore.LoadOrNew(paths.SavePath,2).ClaimedRewardIds.Contains(A),"disk committed before successful cache observation");
            var duplicate=Mount(leaf,level,player);await Frames(tree,2);
            TestAssert.True(duplicate.IsClaimed && !duplicate.CanInteract(player) && duplicate.FeedbackCount==0,"second physical instance sharingID cannot grant twice");
            duplicate.QueueFree();await Frames(tree,2);
            await Pulse(tree,input);
            TestAssert.True(cache.FeedbackCount==1 && level.Progress.ResourceTotal("fiber")==3,"repeated Q cannot duplicate grant");

            cache.RewardId=B;await KeyboardActionRuntimeTests.Frames(tree,input,3);
            string originalSha=Hash(paths.SavePath);var previousSave=flow.Save;
            string blocked=Path.Combine(paths.CaseRoot,"blocked-save-target");Directory.CreateDirectory(blocked);
            flow.SavePath=blocked;
            KeyboardActionRuntimeTests.KeyEvent(Key.Q,true);await KeyboardActionRuntimeTests.Frames(tree,input,4);
            TestAssert.True(cache.LastResult==ResourceRewardClaimResult.SaveFailed && !cache.IsClaimed &&
                !level.Progress.ClaimedRewardIds.Contains(B) && ReferenceEquals(previousSave,flow.Save) && originalSha==Hash(paths.SavePath),
                "real atomic-write failure leaves ledger, Save identity, original bytes and cache unspent");
            flow.SavePath=paths.SavePath;await KeyboardActionRuntimeTests.Frames(tree,input,4);
            TestAssert.True(!cache.IsClaimed,"held failed Q is not replayed when disk target becomes valid");
            KeyboardActionRuntimeTests.KeyEvent(Key.Q,false);await KeyboardActionRuntimeTests.Frames(tree,input,3);
            await Pulse(tree,input);
            TestAssert.True(cache.IsClaimed && level.Progress.ResourceTotal("mineral")==4,"fresh Q retries actual failed grant once");
            foreach(var reward in ResourceRewardCatalog.All.Where(x=>x.StableId!=A && x.StableId!=B))
            {
                cache.RewardId=reward.StableId;await KeyboardActionRuntimeTests.Frames(tree,input,2);
                await Pulse(tree,input);
                TestAssert.True(cache.IsClaimed,"actual Q claims each independent catalog ID: "+reward.StableId);
            }
            AssertAll(level.Progress.ClaimedRewardIds);
            TestAssert.True(player.GetNode<HealthComponent>("Health").CurrentHealth==initialHealth &&
                level.Progress.CollectedFragmentIds.Count==initialFragments,"materials do not heal, damage or satisfy fragmentquota");
            var draft=PrologueSaveData.NewLayoutV2();
            typeof(PrologueGameFlowController).GetMethod("PopulateProgressSnapshot",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(flow,new object[]{draft});
            TestAssert.Equal(54,draft.ClaimedRewardIds.Count,"generic snapshot copies full live ledger into an initially empty draft");
            flow.PersistProgress("resource-reward-native");
            AssertAll(PrologueSaveStore.LoadOrNew(paths.SavePath,2).ClaimedRewardIds);
            cache.Hide();int recoveries=0;level.RecoveryCompleted+=_=>recoveries++;
            var health=player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,Vector2.Zero,0f,665401UL,player.GetInstanceId(),DamageGuardClass.Unblockable)),"real death through health/recovery");
            for(int i=0;i<150 && recoveries==0;i++) await Frames(tree,1);
            TestAssert.True(recoveries==1 && flow.State==PrologueGameFlowState.Playing,"real death recovers exactly once");
            AssertAll(level.Progress.ClaimedRewardIds);
            flow.PersistProgress("resource-reward-after-death");
            File.Copy(paths.SavePath,Path.Combine(storage,Witnesses[0]),false);
            string continueSha=Hash(paths.SavePath);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);level=null;
            level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            IsolatedPrologueFixture.ConfigureExisting(level,paths.SavePath);
            flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
            tree.Root.AddChild(level);await Frames(tree,4);
            TestAssert.True(flow.HasSave,"remounted real title sees reward save");
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            AssertAll(level.Progress.ClaimedRewardIds);
            TestAssert.Equal(continueSha,Hash(paths.SavePath),"actual Continue reads without rewriting");
            player=level.Player;input=player.GetNode<PlayerInputReader>("Input");
            cache=Mount(leaf,level,player);await Frames(tree,3);
            TestAssert.True(cache.IsClaimed && !cache.CanInteract(player),"loaded claim withdraws reloaded physical cache");
            flow.ReturnToTitle();flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==0 && PrologueSaveStore.LoadOrNew(paths.SavePath,2).ClaimedRewardIds.Count==0,"explicit new game clears both durable and live grants");
            player=level.Player;input=player.GetNode<PlayerInputReader>("Input");
            for(int i=0;i<90 && (!player.IsOnFloor() || player.Velocity.Length()>.1f);i++) await KeyboardActionRuntimeTests.Frames(tree,input,1);
            if(GodotObject.IsInstanceValid(cache) && cache.IsInsideTree()) cache.QueueFree();
            await Frames(tree,2);cache=Mount(leaf,level,player);await KeyboardActionRuntimeTests.Frames(tree,input,3);
            int callback=0;bool reentry=false;var callbackCache=cache;var callbackPlayer=player;var callbackFlow=flow;
            cache.Claimed+=_=>
            {
                callback++;
                TestAssert.True(PrologueSaveStore.LoadOrNew(paths.SavePath,2).ClaimedRewardIds.Contains(A),"success signal observes committed disk");
                reentry=callbackCache.TryInteract(callbackPlayer);
                callbackFlow.ReturnToTitle();callbackFlow.StartNew();
                if(GodotObject.IsInstanceValid(callbackCache)) callbackCache.QueueFree();
            };
            await Pulse(tree,input);await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(callback==1 && !reentry && level.Progress.ClaimedRewardIds.Count==0 &&
                PrologueSaveStore.LoadOrNew(paths.SavePath,2).ClaimedRewardIds.Count==0,"success reentry/new/unload cannot restore stale claim after callback");
            File.Copy(paths.SavePath,Path.Combine(storage,Witnesses[1]),false);
            var old=JsonNode.Parse(File.ReadAllText(Path.Combine(storage,Witnesses[0])))!.AsObject();old.Remove("claimedRewardIds");
            File.WriteAllText(Path.Combine(storage,Witnesses[2]),old.ToJsonString());
            File.WriteAllText(Path.Combine(storage,"resource-rewards-cold-manifest.json"),JsonSerializer.Serialize(new
            { writerPid=Environment.ProcessId, hashes=Witnesses.Select(n=>Hash(Path.Combine(storage,n))).ToArray() }));
            GD.Print($"RESOURCE_REWARD_WARM_OK writer_pid={Environment.ProcessId} grants=54 actual_Q=true save_failure_retry=true death=true continue=true new=true reentry=true staged_leaf=true current_map_placed=false human=false");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.Q,false);
            if(tree.Paused)tree.Paused=false;
            if(level!=null && GodotObject.IsInstanceValid(level))await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }

    public static void RunCold(string storage)
    {
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(storage,"resource-rewards-cold-manifest.json")));
        int writer=manifest.RootElement.GetProperty("writerPid").GetInt32();
        TestAssert.True(writer>0 && writer!=Environment.ProcessId,"cold reward reader is a distinct actual process");
        string[] hashes=manifest.RootElement.GetProperty("hashes").EnumerateArray().Select(x=>x.GetString()).ToArray();
        TestAssert.Equal(3,hashes.Length,"exact three cold witnesses");
        for(int i=0;i<Witnesses.Length;i++)
        {
            string path=Path.Combine(storage,Witnesses[i]);
            TestAssert.Equal(hashes[i],Hash(path),"cold witness matches writer bytes");
            var data=PrologueSaveStore.LoadOrNew(path,2);
            TestAssert.True(!data.WasRecovered,"cold witness valid and not recovered");
            if(i==0)AssertAll(data.ClaimedRewardIds);else TestAssert.Equal(0,data.ClaimedRewardIds.Count,"reset/oldv4 remain empty");
            TestAssert.Equal(hashes[i],Hash(path),"cold read preserves original bytes");
        }
        GD.Print($"RESOURCE_REWARD_COLD_OK writer_pid={writer} reader_pid={Environment.ProcessId} witnesses=3 fixed_totals=9/33/44/12/2 source_read_only=true");
    }
    private static PrologueResourceCache CreateCandidate()
    {
        var cache=new PrologueResourceCache{Name="ResourceCache",RewardId=A};
        foreach(string name in new[]{"AvailableVisual","ClaimedVisual"})
        {
            var visual=new Polygon2D{Name=name,Polygon=new[]{new Vector2(-14,-10),new Vector2(14,-10),new Vector2(14,10),new Vector2(-14,10)},
                Color=name=="AvailableVisual"?new Color(.2f,.65f,.5f):new Color(.15f,.2f,.2f)};
            cache.AddChild(visual);visual.Owner=cache;
        }
        var status=new Label{Name="Status",Position=new Vector2(-75,-42),Size=new Vector2(150,24),HorizontalAlignment=HorizontalAlignment.Center};
        cache.AddChild(status);status.Owner=cache;return cache;
    }
    private static PrologueResourceCache Mount(PackedScene leaf,PrologueLevelController level,CharacterBody2D player)
    {
        var cache=leaf.Instantiate<PrologueResourceCache>();var sector=level.CurrentSectorRoot;
        cache.Position=sector.ToLocal(player.GlobalPosition+new Vector2(48,0));
        sector.AddChild(cache);return cache; // NodeAdded must bind it, never call BindRuntime from this fixture.
    }
    private static async Task<bool> Pulse(SceneTree tree,PlayerInputReader input)
    {
        KeyboardActionRuntimeTests.KeyEvent(Key.Q,true);bool accepted=false;
        for(int i=0;i<3;i++){await KeyboardActionRuntimeTests.Frames(tree,input,1);accepted|=input.Current.InteractPressed;}
        KeyboardActionRuntimeTests.KeyEvent(Key.Q,false);await KeyboardActionRuntimeTests.Frames(tree,input,3);return accepted;
    }
    private static void AssertAll(System.Collections.Generic.IEnumerable<string> ids)
    {
        TestAssert.Equal(54,ids.Distinct(StringComparer.Ordinal).Count(),"all54 independent grants retained");
        foreach((string kind,int count) in new[]{("fiber",9),("mineral",33),("salvage",44),("data",12),("echo-memory",2)})
            TestAssert.Equal(count,ResourceRewardCatalog.Total(ids,kind),"literal actual grant totals "+kind);
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static async Task Frames(SceneTree tree,int count){for(int i=0;i<count;i++)await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);}
}
