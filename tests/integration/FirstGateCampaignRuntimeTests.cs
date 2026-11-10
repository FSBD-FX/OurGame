using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class FirstGateCampaignRuntimeTests
{
    private static readonly string[] ColdSources={"FirstGateEntryCampaign-v340.tscn","world-profile-seven.json","world-profile-new.json"};
    public static async Task RunCold(SceneTree tree,string storage)
    {
        string receipt=Path.Combine(storage,"world-profile-cold-manifest.json");
        TestAssert.True(File.Exists(receipt),"WORLD_PROFILE_COLD_RED: warm producer must retain a hashed handoff for independent cold reading");
        using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(receipt));
        int writer=document.RootElement.GetProperty("writerPid").GetInt32();
        string[] hashes=document.RootElement.GetProperty("hashes").EnumerateArray().Select(x=>x.GetString()).ToArray();
        TestAssert.True(writer>0 && writer!=System.Environment.ProcessId && hashes.Length==3,"independent writer/reader and three fixed source witnesses");
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,ColdSources[i])),"cold source handed off without mutation");
        using var scene=ResourceLoader.Load<PackedScene>(Path.Combine(storage,ColdSources[0]),cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=scene.Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"first-gate-independent-cold");
        File.Copy(Path.Combine(storage,ColdSources[1]),paths.SavePath,false);
        string selectedSha=Hash(paths.SavePath);
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.HasSave,"cold title recognizes only the selected profile save");
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");var profile=world.ProfileContract;
            TestAssert.True(profile!=null && level.Residents==null && world.MountedSectorIds.Count==1 &&
                world.CheckpointId=="knight.first-gate.a-refuge" && level.Progress.ClaimedRewardIds.Count==7 &&
                level.Progress.ResourceTotal("fiber")==4 && level.Progress.ResourceTotal("mineral")==6 && !flow.Save.Completed,
                "fresh native process restores new world, seven grants, canonical refuge and no formal completion");
            TestAssert.True(Math.Abs(level.Player.Position.X-1760)<1 && Math.Abs(level.Player.Position.Y-856)<8,
                "cold actual player restores at new refuge, not old-world coordinates");
            TestAssert.True(RefugeVisual(world).CurrentState is MechanismVisualState.Active or MechanismVisualState.Restored,
                "cold Continue restores persistent lit checkpoint presentation");
            TestAssert.Equal(selectedSha,Hash(paths.SavePath),"cold Continue does not rewrite its isolated selected slot");
            var reset=PrologueSaveStore.LoadCandidate(Path.Combine(storage,ColdSources[2]),profile).Data;
            TestAssert.True(!reset.WasRecovered && reset.ClaimedRewardIds.Count==0 && reset.CheckpointId==profile.Start.StableId && !reset.HasActivatedCheckpoint,
                "separate actual new-game witness remains empty and at start");
        }
        finally{await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,ColdSources[i])),"independent cold reader preserves original source witnesses");
        GD.Print($"WORLD_PROFILE_COLD_OK writer_pid={writer} reader_pid={System.Environment.ProcessId} witnesses=3 actual_continue=true grants=7 source_readonly=true human=false");
    }
    public static async Task Run(SceneTree tree,string parentPath,string storage)
    {
        using var scene=ResourceLoader.Load<PackedScene>(parentPath,cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=scene.Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"first-gate-real-Q");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        KeyboardRouteInputDriver driver=null;int ticks=0,recoveries=0;
        void OnRecovery(string reason)=>recoveries++;
        level.RecoveryCompleted+=OnRecovery;
        tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var profile=world.ProfileContract;
            var fogCanvas=level.GetNode<Knight.Presentation.Fog.PrologueFogCanvas>("Fog/Canvas");
            TestAssert.Equal(3,((ShaderMaterial)fogCanvas.Material).GetShaderParameter("field_count").AsInt32(),"actual saved graph uploads selected profile fields, not old region list");
            TestAssert.True(profile!=null && world.MountedSectorIds.SequenceEqual(new[]{PrologueSectorId.CanyonEntrance}),"real candidate world Ready mounts one actual container");
            TestAssert.True(level.Residents==null,"PROFILE_RUNTIME_RED: candidate must not deploy old-map residents");
            flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(flow.Save.LayoutVersion==3 && flow.Save.WorldProfileId==profile.Id &&
                world.CheckpointId==profile.Start.StableId && !level.Progress.RestoreCheckpoint().HasActivatedCheckpoint,
                "real New uses selected profile, not the next checkpoint");
            TestAssert.Equal(MechanismVisualState.Idle,RefugeVisual(world).CurrentState,"initial New shows unactivated refuge lamp");
            var player=level.Player;ulong identity=player.GetInstanceId();
            var health=player.GetNode<HealthComponent>("Health");
            var input=player.GetNode<PlayerInputReader>("Input");
            driver=KeyboardRouteInputDriver.Begin(player);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver.ObserveFrame();ticks++;
                TestAssert.True(ticks<=4200 && player.GetInstanceId()==identity,"same real player and bounded sample route");
            }
            async Task Until(Func<bool> reached,int budget,string stage)
            {
                int count=0;while(!reached() && count++<budget)await Step();
                GD.Print($"WORLD_PROFILE_ROUTE stage={stage} pos={player.Position} hp={health.CurrentHealth} ticks={ticks} recoveries={recoveries}");
                TestAssert.True(reached(),"new campaign route "+stage);
            }
            async Task Move(bool right,float x,string stage)
            {
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);
                await Until(()=>right?player.Position.X>=x:player.Position.X<=x,750,stage);
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,false);
            }
            async Task Pulse()
            {
                driver.Command(InputActions.Interact,true);for(int i=0;i<3;i++)await Step();
                driver.Command(InputActions.Interact,false);for(int i=0;i<3;i++)await Step();
            }
            async Task Claim(string id,bool failFirst=false)
            {
                await Until(()=>player.IsOnFloor() && player.Velocity.Length()<.1f,100,"stand-for-Q-"+id);
                int before=level.Progress.ClaimedRewardIds.Count;
                if(failFirst)
                {
                    string slot=flow.SavePath,sha=Hash(paths.SavePath);var save=flow.Save;
                    flow.SavePath=paths.CaseRoot;
                    await Pulse();
                    TestAssert.True(!level.Progress.ClaimedRewardIds.Contains(id) && ReferenceEquals(save,flow.Save) && Hash(paths.SavePath)==sha,
                        "profile-map actual disk write failure consumes nothing and preserves accepted save bytes");
                    flow.SavePath=slot;
                }
                await Pulse();
                TestAssert.True(level.Progress.ClaimedRewardIds.Contains(id) && level.Progress.ClaimedRewardIds.Count==before+1,
                    "actual authored placement claimed via fresh Q: "+id);
                TestAssert.True(PrologueSaveStore.LoadCandidate(paths.SavePath,profile).Data.ClaimedRewardIds.Contains(id),"actual Q durably commits selected profile before feedback");
            }
            async Task Death(string checkpoint,Vector2 spawn)
            {
                int before=recoveries;
                TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,Vector2.Zero,0f,812345UL,identity,DamageGuardClass.Unblockable)),"real health death accepted");
                await Until(()=>recoveries==before+1 && flow.State==PrologueGameFlowState.Playing,160,"real-death-"+checkpoint);
                TestAssert.True(player.GetInstanceId()==identity && world.CheckpointId==checkpoint && level.Progress.RestoreCheckpoint().CheckpointId==checkpoint &&
                    Math.Abs(player.Position.X-spawn.X)<1 && Math.Abs(player.Position.Y-spawn.Y)<8 && health.CurrentHealth==5,
                    "same-player recovery returns to canonical selected checkpoint, not unvisited next node");
            }
            await Until(()=>player.IsOnFloor(),60,"initial-floor");
            TestAssert.True(player.Position.X==192 && player.Position.Y is >470 and <480,"actual new entry location");
            var camera=player.GetNode<Camera2D>("Camera2D");
            TestAssert.True(camera.LimitLeft==0 && camera.LimitTop==0 && camera.LimitRight==3072 && camera.LimitBottom==2048,
                "camera uses new actual bounds, not old focus rectangles");
            await Move(true,576,"M01-resource");await Claim("knight.first-gate.main.01.resource");
            await Death("knight.first-gate.start",new(192,472));
            TestAssert.Equal(1,level.Progress.ClaimedRewardIds.Count,"death before first refuge retains obtained material");
            await Move(true,1180,"M02-resource");await Claim("knight.first-gate.main.02.resource",failFirst:true);
            await Move(true,1504,"A-first-pile");await Claim("knight.first-gate.branch.a.resource.01");
            await Move(true,1750,"A-chest-refuge");await Claim("knight.first-gate.branch.a.chest");
            TestAssert.True(world.CheckpointId=="knight.first-gate.a-refuge" && PrologueSaveStore.LoadCandidate(paths.SavePath,profile).Data.CheckpointId==world.CheckpointId,
                "real contact activated refuge durably in same Entrance container");
            TestAssert.Equal(MechanismVisualState.Active,RefugeVisual(world).CurrentState,"real durable activation lights persistent grave lamp");
            string repeatedSha=Hash(paths.SavePath);await Pulse();
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==4 && Hash(paths.SavePath)==repeatedSha,"fresh repeated Q cannot regrant or rewrite consumed cache");
            await Death("knight.first-gate.a-refuge",new(1760,856));
            await Move(true,1888,"A-last-pile");await Claim("knight.first-gate.branch.a.resource.02");
            await Move(true,2070,"A-down-to03");
            await Until(()=>player.IsOnFloor() && player.Position.Y>1180,120,"A-03-rejoin");
            await Move(false,704,"M03-resource");await Claim("knight.first-gate.main.03.resource");
            await Move(false,280,"lower-turn");await Until(()=>player.IsOnFloor() && player.Position.Y>1650,120,"lower-beam");
            await Move(true,1605,"single-jump-takeoff");await Until(()=>player.IsOnFloor() && Math.Abs(player.Velocity.X)<1,30,"braked-takeoff");
            driver.Command(InputActions.MoveRight,true);driver.Command(InputActions.Jump,true);
            await Until(()=>player.Position.X>1780 && player.IsOnFloor() && player.Position.Y<1670,120,"single-jump-far-bank");
            driver.Command(InputActions.Jump,false);driver.Command(InputActions.MoveRight,false);
            await Move(true,2464,"M04-resource");await Claim("knight.first-gate.main.04.resource");
            await Move(true,2660,"sample-physical-end");
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==7 && level.Progress.ResourceTotal("fiber")==4 && level.Progress.ResourceTotal("mineral")==6,
                "all seven real map placements credited once with literal totals");
            TestAssert.True(!flow.TryComplete() && !flow.Save.Completed && !level.Progress.BossDefeated && flow.State==PrologueGameFlowState.Playing,
                "partial fourth segment is not full prologue completion");
            flow.PersistProgress("first-gate-seven-real-Q");
            File.Copy(paths.SavePath,Path.Combine(storage,"world-profile-seven.json"),false);
            driver.Dispose();driver=null;
            flow.ReturnToTitle();string continueSha=Hash(paths.SavePath);flow.Continue();
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==7 && Hash(paths.SavePath)==continueSha && world.CheckpointId=="knight.first-gate.a-refuge",
                "actual title Continue preserves seven grants and selected checkpoint without rewriting save");
            TestAssert.True(RefugeVisual(world).CurrentState is MechanismVisualState.Active or MechanismVisualState.Restored,
                "warm Continue keeps persistent checkpoint lit");
            flow.ReturnToTitle();flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==0 && world.CheckpointId==profile.Start.StableId && !level.Progress.RestoreCheckpoint().HasActivatedCheckpoint,
                "real New clears only selected profile and restarts before refuge");
            TestAssert.Equal(MechanismVisualState.Idle,RefugeVisual(world).CurrentState,"PROFILE_REFUGE_RESET_RED: New clears previous run's lit checkpoint presentation");
            File.Copy(paths.SavePath,Path.Combine(storage,"world-profile-new.json"),false);
            File.WriteAllText(Path.Combine(storage,"world-profile-cold-manifest.json"),System.Text.Json.JsonSerializer.Serialize(new
            {writerPid=System.Environment.ProcessId,hashes=ColdSources.Select(n=>Hash(Path.Combine(storage,n))).ToArray()}));
            GD.Print($"WORLD_PROFILE_WARM_OK writer_pid={System.Environment.ProcessId} actual_map_Q=7 materials_fiber=4 materials_mineral=6 deaths=2 same_player=true continue_readonly=true new=true formal_complete=false human=false");
        }
        finally
        {
            driver?.Dispose();level.RecoveryCompleted-=OnRecovery;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }
    private static PrologueMechanismVisual RefugeVisual(SeamlessPrologueWorld world)
    {
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonEntrance,out var sector),"actual refuge container mounted");
        var visual=sector.GetNodeOrNull<PrologueMechanismVisual>("Gameplay/ARefuge/Visual");
        TestAssert.True(visual is {Visible:true,Kind:MechanismVisualKind.GraveLamp} && visual.IsVisibleInTree(),"real mounted checkpoint has persistent visible marker");
        return visual;
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
