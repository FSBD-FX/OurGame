using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Interactions;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests.Integration;

public static class FirstGateAbyssExplorationTests
{
    private static readonly string[] Sources={"FirstGateAbyssExplore-v342.tscn","abyss-exploration-all30.json","abyss-exploration-new.json"};
    private static readonly string[] ExpectedIds=Enumerable.Range(1,12).Select(n=>$"knight.first-gate.main.{n:00}.resource")
        .Concat("abcdef".SelectMany(c=>new[]{"chest","resource.01","resource.02"}.Select(s=>$"knight.first-gate.branch.{c}.{s}"))).Order().ToArray();

    public static async Task Run(SceneTree tree,string storage)
    {
        var profile=FirstGateAbyssProfileTests.Run(storage);
        string parent=FirstGateAbyssRuntimeTests.BuildSavedParent(storage,profile,Sources[0]);
        using var scene=ResourceLoader.Load<PackedScene>(parent,cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=scene.Instantiate<PrologueLevelController>();var paths=IsolatedPrologueFixture.Configure(level,"abyss-continuous-all30");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        KeyboardRouteInputDriver driver=null;int ticks=0,recoveries=0;bool lifecycle=false;
        HealthComponent observedHealth=null;HealthComponent.DamagedEventHandler damageTrace=null;
        void Recovered(string reason){recoveries++;GD.Print($"ABYSS_EXPLORE_RECOVERY reason={reason} ticks={ticks}");}
        level.RecoveryCompleted+=Recovered;tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var player=level.Player;ulong actor=player.GetInstanceId();
            var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            var health=player.GetNode<HealthComponent>("Health");var interaction=player.GetNode<EnvironmentInteractionController>("EnvironmentInteraction");
            observedHealth=health;
            damageTrace=(_,_)=>GD.Print($"ABYSS_EXPLORE_DAMAGE ticks={ticks} pos={player.Position} velocity={player.Velocity} hp={health.CurrentHealth} height={motor.LastFallHeight} damage={motor.LastFallDamage} impact={motor.LastLandingImpact.Speed} reason={motor.LastLandingImpact.Reason} floor={player.IsOnFloor()} wall={player.IsOnWall()}");
            health.Damaged+=damageTrace;
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.ProfileContract==profile && world.MountedSectorIds.Count==1 && level.Residents==null &&
                Math.Abs(player.Position.X-192)<1 && Math.Abs(player.Position.Y-472)<8,"all30 begins at actual StartNew, not prepositioned transfer fixture");
            driver=KeyboardRouteInputDriver.Begin(player);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver?.ObserveFrame();ticks++;
                TestAssert.True(ticks<=24000 && player.GetInstanceId()==actor && (lifecycle || health.CurrentHealth>0 && recoveries==0),
                    $"continuous exploration alive/sameactor/no navigation by recovery ticks={ticks} pos={player.Position} hp={health.CurrentHealth}");
            }
            async Task Until(Func<bool> done,int budget,string stage)
            {
                int count=0;while(!done() && count++<budget)await Step();
                GD.Print($"ABYSS_EXPLORE_ROUTE stage={stage} ticks={ticks} pos={player.Position} velocity={player.Velocity} floor={player.IsOnFloor()} hp={health.CurrentHealth} grants={level.Progress.ClaimedRewardIds.Count}");
                TestAssert.True(done(),"ABYSS_EXPLORE_ROUTE_RED: "+stage+" actual="+player.Position);
            }
            async Task Move(bool right,float x,string stage)
            {
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);
                await Until(()=>right?player.Position.X>=x:player.Position.X<=x,1800,stage);
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,false);
            }
            async Task Align(float x,string stage)=>await Move(player.Position.X<x,x,stage);
            async Task Floor(float y,string stage)=>await Until(()=>player.IsOnFloor() && player.Position.Y>y-50 && player.Position.Y<y-25,360,stage);
            async Task Pulse()
            {
                driver.Command(InputActions.Interact,true);for(int i=0;i<3;i++)await Step();
                driver.Command(InputActions.Interact,false);for(int i=0;i<3;i++)await Step();
            }
            async Task Claim(string id,bool failFirst=false)
            {
                await Until(()=>player.IsOnFloor() && player.Velocity.Length()<.1f,120,"Q-stand-"+id);
                await Until(()=>interaction.CurrentCandidate!=null,8,"Q-actual-candidate-"+id);
                TestAssert.Equal(id,interaction.CurrentCandidate?.StableId,"actual unique Q candidate, including D/E shared approach");
                int before=level.Progress.ClaimedRewardIds.Count;
                PrologueResourceCache failedCache=null;int feedbackBefore=0;
                if(failFirst)
                {
                    failedCache=interaction.CurrentCandidate as PrologueResourceCache;
                    TestAssert.True(failedCache!=null && failedCache.RewardId==id && !failedCache.IsClaimed &&
                        failedCache.LastResult==ResourceRewardClaimResult.Unavailable,"failure control starts with the actual untouched selected cache");
                    feedbackBefore=failedCache.FeedbackCount;
                    string slot=flow.SavePath,sha=Hash(paths.SavePath);var accepted=flow.Save;
                    // A no-op/unconsumed Q preserves bytes too. Only this same
                    // cache's actual failed-save result proves the attempted arm.
                    bool FailedAttemptProved()=>failedCache.LastResult==ResourceRewardClaimResult.SaveFailed &&
                        failedCache.FeedbackCount==feedbackBefore && !failedCache.IsClaimed &&
                        !level.Progress.ClaimedRewardIds.Contains(id) && level.Progress.ClaimedRewardIds.Count==before &&
                        ReferenceEquals(accepted,flow.Save) && Hash(paths.SavePath)==sha;
                    try
                    {
                        flow.SavePath=paths.CaseRoot;
                        TestAssert.True(!FailedAttemptProved(),"ABYSS_CHEST_FAILURE_CONTROL_RED: skipped Q must not certify an attempted failed save");
                        await Pulse();
                        TestAssert.True(FailedAttemptProved(),"actual optional chest SaveFailed grants nothing/feedback and preserves accepted slot");
                    }
                    finally{flow.SavePath=slot;}
                    TestAssert.True(ReferenceEquals(failedCache,interaction.CurrentCandidate),"fresh retry keeps the same actual selected cache");
                }
                await Pulse();
                if(failedCache!=null)
                {
                    TestAssert.True(failedCache.LastResult==ResourceRewardClaimResult.Claimed && failedCache.IsClaimed &&
                        failedCache.FeedbackCount==feedbackBefore+1,"fresh same-cache retry succeeds and publishes feedback exactly once");
                    GD.Print("ABYSS_CHEST_FAILURE_CONTROL_OK skipped_q_rejected=true actual_savefailed=true feedback_unchanged=true same_cache_retry=true");
                }
                TestAssert.True(level.Progress.ClaimedRewardIds.Count==before+1 && level.Progress.ClaimedRewardIds.Contains(id) &&
                    PrologueSaveStore.LoadCandidate(paths.SavePath,profile).Data.ClaimedRewardIds.Contains(id),"real placed fresh Q durably claims "+id);
                GD.Print($"ABYSS_EXPLORE_Q id={id} count={level.Progress.ClaimedRewardIds.Count} selected_slot=true");
            }
            async Task Main(int n)=>await Claim($"knight.first-gate.main.{n:00}.resource");
            async Task Branch(char c,string kind,bool fail=false)=>await Claim($"knight.first-gate.branch.{c}.{kind}",fail);
            async Task Jump(bool right,float x,string stage)
            {
                await Until(()=>player.IsOnFloor() && Math.Abs(player.Velocity.X)<1,40,stage+"-grounded");
                int before=motor.AcceptedJumpCount;
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);driver.Command(InputActions.Jump,true);
                await Until(()=>player.IsOnFloor() && (right?player.Position.X>x:player.Position.X<x),180,stage);
                driver.Command(InputActions.Jump,false);driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,false);
                TestAssert.Equal(before+1,motor.AcceptedJumpCount,"one separately grounded jump "+stage);
            }
            await Floor(512,"01-spawn");await Move(true,576,"01-resource");await Main(1);
            await Move(true,1180,"02-resource");await Floor(864,"02-support");await Main(2);
            await Move(true,1504,"A-first");await Branch('a',"resource.01");
            await Move(true,1750,"A-chest");await Branch('a',"chest");
            TestAssert.Equal("knight.first-gate.a-refuge",world.CheckpointId,"actual A checkpoint contact");
            await Move(true,1888,"A-last");await Branch('a',"resource.02");
            await Move(true,2070,"A-rejoin03");await Floor(1248,"03-support");await Move(false,704,"03-resource");await Main(3);
            await Move(false,280,"04-turn");await Floor(1696,"04-floor");await Move(true,1605,"04-gap");await Jump(true,1780,"04-single-jump");
            await Move(true,2464,"04-resource");await Main(4);
            await Move(true,4048,"05-catch-entry");await Floor(1984,"05-catch");await Main(5);
            await Move(true,4094,"B-running-catch-launch");
            int bJump=motor.AcceptedJumpCount,bJet=motor.AcceptedDashCount;
            driver.Command(InputActions.Jump,true);await Step();await Step();
            TestAssert.True(motor.AcceptedJumpCount==bJump+1 && !player.IsOnFloor(),"actual B ordinaryjump precedes Shift");
            driver.Command(InputActions.MoveRight,false);driver.Command(InputActions.MoveUp,true);driver.Command(InputActions.Dash,true);
            await Until(()=>motor.AcceptedDashCount==bJet+1,8,"B-vertical-jet-committed");
            TestAssert.True(motor.ThrusterState.BurnDirection.Dot(Vector2.Up)>.999f,"real vertical direction held through actual jet edge");
            driver.Command(InputActions.Dash,false);driver.Command(InputActions.MoveUp,false);driver.Command(InputActions.MoveRight,true);
            await Until(()=>player.IsOnFloor() && player.Position.X>4320 && player.Position.Y is >1620 and <1670,220,"B-upper-bank");
            driver.Command(InputActions.MoveRight,false);driver.Command(InputActions.Jump,false);
            await Move(true,4608,"B-first");await Branch('b',"resource.01");await Move(true,4992,"B-chest");await Branch('b',"chest",true);
            await Move(true,5344,"B-real-downshaft");await Floor(2240,"B-lower-floor");await Move(false,4992,"B-last");await Branch('b',"resource.02");
            await Move(false,2688,"B-rejoin06");await Main(6);
            await Move(false,1024,"C-upper-entry");await Floor(2400,"C-upper-floor");await Align(1024,"C-first-align");await Branch('c',"resource.01");
            await Move(false,704,"C-downshaft");await Floor(2944,"C-bottom");await Align(704,"C-chest-align");await Branch('c',"chest");
            await Move(true,1280,"C-last");await Branch('c',"resource.02");
            await Move(true,1760,"C-rejoin07");driver.Command(InputActions.MoveLeft,true);
            await Until(()=>player.Velocity.X<=0,60,"07-real-reverse-brake");driver.Command(InputActions.MoveLeft,false);
            await Floor(3584,"07-side-rest");await Align(1760,"07-resource-align");await Main(7);
            await Move(false,1692,"D-side-rest-launch");await Jump(false,1490,"D-entry-single-jump");
            await Move(false,1024,"D-first");await Branch('d',"resource.01");
            await Move(false,704,"D-real-windshaft");driver.Command(InputActions.MoveRight,true);
            await Until(()=>player.Velocity.X>=0,60,"D-real-reverse-brake-before-E-opening");driver.Command(InputActions.MoveRight,false);
            await Floor(5088,"D-bottom");await Align(704,"D-chest-align");await Branch('d',"chest");
            await Move(true,768,"E-shared-approach-first");await Branch('e',"resource.01");
            await Move(true,928,"D-last");await Branch('d',"resource.02");
            await Move(true,1188,"D-return-bridge");await Jump(true,1400,"D-09-rejoin-single-jump");
            await Move(true,4352,"08-resource-from09");await Main(8);await Move(false,1600,"09-resource");await Main(9);
            await Move(false,1372,"E-return-gap-launch");await Jump(false,1168,"E-entry-single-jump");
            await Move(false,336,"E-downshaft");await Floor(5408,"E-catch");await Move(true,480,"E-leave-catch");await Floor(5728,"E-bottom");
            await Align(416,"E-chest-align");await Branch('e',"chest");await Move(true,928,"E-last");await Branch('e',"resource.02");
            await Move(true,1600,"E-rejoin10");await Main(10);
            await Move(true,3728,"10-far-well");await Floor(6112,"10-far-catch");await Move(true,3870,"11-drop");await Floor(6656,"11-forest-floor");
            for(int i=2;i>=0;i--){float left=2016+i*512;await Move(false,left+92,"11-pillar-"+i);await Jump(false,left-36,"11-pillar-jump-"+i);}
            await Move(false,1600,"11-resource");await Main(11);
            await Move(false,1180,"F-entry-bridge");await Jump(false,980,"F-entry-single-jump");
            await Move(false,768,"F-first");await Branch('f',"resource.01");
            await Move(false,336,"F-downshaft");await Floor(7072,"F-catch");await Move(true,480,"F-leave-catch");await Floor(7488,"F-bottom");
            await Align(416,"F-chest-align");await Branch('f',"chest");await Move(true,928,"F-last");await Branch('f',"resource.02");
            await Move(true,5184,"F-rejoin12-resource");await Main(12);
            AssertAll30(level.Progress.ClaimedRewardIds,resource=>level.Progress.ResourceTotal(resource));
            string duplicateSha=Hash(paths.SavePath);await Pulse();
            TestAssert.True(Hash(paths.SavePath)==duplicateSha && level.Progress.ClaimedRewardIds.Count==30,"fresh Q at consumed final pile never duplicates or rewrites");
            await Move(true,5480,"12-physical-end");
            TestAssert.True(motor.AcceptedJumpCount==9 && motor.AcceptedDashCount==1 && recoveries==0 && health.CurrentHealth>0 &&
                world.CheckpointId=="knight.first-gate.bottom-refuge" && !flow.TryComplete() && !flow.Save.Completed,
                "one continuous living all30 route, nine separately grounded ordinaryjumps, one jet, no recovery navigation or formal completion");
            int routeHp=health.CurrentHealth,routeTicks=ticks;driver.Dispose();driver=null;lifecycle=true;
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,Vector2.Zero,0f,942300UL,actor,DamageGuardClass.Unblockable)),"only after route, deliberate actual lethal lifecycle check");
            await Until(()=>recoveries==1 && flow.State==PrologueGameFlowState.Playing,180,"real-bottom-death-recovery");
            TestAssert.True(Math.Abs(player.Position.X-5376)<1 && Math.Abs(player.Position.Y-7448)<8,"same actor recovers at actual bottom checkpoint");
            flow.PersistProgress("abyss-all30-real-Q");File.Copy(paths.SavePath,Path.Combine(storage,Sources[1]),false);
            flow.ReturnToTitle();string continueSha=Hash(paths.SavePath);flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            AssertAll30(level.Progress.ClaimedRewardIds,resource=>level.Progress.ResourceTotal(resource));
            TestAssert.Equal(continueSha,Hash(paths.SavePath),"actual selected Continue all30 readonly");
            flow.ReturnToTitle();flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==0 && world.CheckpointId==profile.Start.StableId,"real New resets expanded slot only");
            File.Copy(paths.SavePath,Path.Combine(storage,Sources[2]),false);
            File.WriteAllText(Path.Combine(storage,"abyss-exploration-cold-manifest.json"),System.Text.Json.JsonSerializer.Serialize(new{writerPid=System.Environment.ProcessId,hashes=Sources.Select(n=>Hash(Path.Combine(storage,n))).ToArray()}));
            GD.Print($"ABYSS_EXPLORE_ROUTE_FACTS ticks={routeTicks} hp={routeHp} main07_side=true D_bypass=true abyss_refuge_touched=false all_map_pixels=false");
            GD.Print($"ABYSS_EXPLORE_WARM_OK writer_pid={System.Environment.ProcessId} actual_Q=30 jumps=9 jets=1 branches=6 recoveries_before_lifecycle=0 same_player=true continue_readonly=true new=true human=false");
        }
        finally
        {
            if(observedHealth!=null && GodotObject.IsInstanceValid(observedHealth) && damageTrace!=null)observedHealth.Damaged-=damageTrace;
            driver?.Dispose();level.RecoveryCompleted-=Recovered;await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }

    public static async Task RunCold(SceneTree tree,string storage)
    {
        using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(storage,"abyss-exploration-cold-manifest.json")));
        int writer=document.RootElement.GetProperty("writerPid").GetInt32();var hashes=document.RootElement.GetProperty("hashes").EnumerateArray().Select(x=>x.GetString()).ToArray();
        TestAssert.True(writer>0 && writer!=System.Environment.ProcessId && hashes.Length==3,"distinct all30 cold PID and literal three source witnesses");
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,Sources[i])),"all30 cold handoff source SHA");
        using var scene=ResourceLoader.Load<PackedScene>(Path.Combine(storage,Sources[0]),cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=scene.Instantiate<PrologueLevelController>();var paths=IsolatedPrologueFixture.Configure(level,"abyss-all30-independent-cold");
        File.Copy(Path.Combine(storage,Sources[1]),paths.SavePath,false);string sha=Hash(paths.SavePath);
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            AssertAll30(level.Progress.ClaimedRewardIds,resource=>level.Progress.ResourceTotal(resource));
            TestAssert.True(world.ProfileContract.Id=="knight.first-gate.abyss.v1" && world.CheckpointId=="knight.first-gate.bottom-refuge" &&
                Math.Abs(level.Player.Position.X-5376)<1 && Math.Abs(level.Player.Position.Y-7448)<8 && Hash(paths.SavePath)==sha,
                "independent actual Continue restores all30 and canonical bottom readonly");
            TestAssert.True(PrologueSaveStore.LoadCandidate(Path.Combine(storage,Sources[2]),world.ProfileContract).Data.ClaimedRewardIds.Count==0,"cold reads actual warmNew empty witness, not a second interactive New walkthrough");
        }
        finally{await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,Sources[i])),"all30 reader preserves original witnesses");
        GD.Print($"ABYSS_EXPLORE_COLD_OK writer_pid={writer} reader_pid={System.Environment.ProcessId} witnesses=3 actual_continue=true grants=30 source_readonly=true human=false");
    }

    private static void AssertAll30(System.Collections.Generic.IEnumerable<string> ids,Func<string,int> total)
    {
        TestAssert.True(ids.Order().SequenceEqual(ExpectedIds),"all thirty literal main01–12 and a–f IDs, not a Q counter");
        foreach(var (id,amount) in new[]{("mineral",28),("fiber",9),("salvage",6),("data",3),("echo-memory",2)})
            TestAssert.Equal(amount,total(id),"literal actual thirty grant total "+id);
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    // Real saved terrain/actor; this sole pre-Ready spawn is a transfer fixture,
    // never evidence that all rewards were reached from the campaign start.
    public static async Task RunBTransfer(SceneTree tree,string leaf)
    {
        using var terrain=GD.Load<PackedScene>(leaf);
        var root=new Node2D {Name="Actual05CatchToBranchB"};
        root.AddChild(terrain.Instantiate());
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position=new(4048,1948);player.GetNode<Camera2D>("Camera2D").Enabled=false;root.AddChild(player);
        var health=player.GetNode<HealthComponent>("Health");
        var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
        ulong actor=player.GetInstanceId();int ticks=0,injuries=0;
        var afterMotor=new BasinBoardingTickObserver {ProcessPhysicsPriority=100};
        afterMotor.Sample=_=>
        {
            if(ticks is >=14 and <=24)
                GD.Print($"ABYSS_B_POST physics={Engine.GetPhysicsFrames()} ticks={ticks} seq={input.CurrentFrameSequence} rawaim={input.RawFrame.Aim} aim={input.KeyboardAimDirection} rawshift={input.RawFrame.DashPressed} W={Input.IsPhysicalKeyPressed(Key.W)} D={Input.IsPhysicalKeyPressed(Key.D)} accepted={motor.AcceptedDashCount} direction={motor.ThrusterState.BurnDirection} remain={motor.ThrusterState.BurnRemaining} pos={player.Position}");
        };
        root.AddChild(afterMotor);
        HealthComponent.DamagedEventHandler damaged=(_,_)=>injuries++;health.Damaged+=damaged;
        KeyboardRouteInputDriver driver=null;tree.Root.AddChild(root);
        try
        {
            driver=KeyboardRouteInputDriver.Begin(player);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver.ObserveFrame();ticks++;
                if(ticks%15==0)GD.Print($"ABYSS_B_TRANSFER_TRACE ticks={ticks} pos={player.Position} velocity={player.Velocity} floor={player.IsOnFloor()} jumps={motor.AcceptedJumpCount} jets={motor.AcceptedDashCount} burn={motor.IsDashing} airuse={motor.AirDashAvailable}");
                TestAssert.True(ticks<=600 && player.GetInstanceId()==actor && health.CurrentHealth==5 && injuries==0,
                    "actual transfer remains bounded/alive/same actor with no injected recovery or HP");
            }
            async Task Until(Func<bool> done,int budget,string stage)
            {
                int count=0;while(!done() && count++<budget)await Step();
                GD.Print($"ABYSS_B_TRANSFER stage={stage} ticks={ticks} pos={player.Position} velocity={player.Velocity} floor={player.IsOnFloor()} jumps={motor.AcceptedJumpCount} jets={motor.AcceptedDashCount}");
                TestAssert.True(done(),"ABYSS_B_TRANSFER_RED: "+stage);
            }
            await Until(()=>player.IsOnFloor(),30,"real05-catch");
            TestAssert.True(player.Position.Y is >1940 and <1955,"fixture actually stands on05catch1984");
            driver.Command(InputActions.MoveRight,true);
            await Until(()=>player.Position.X>=4094 && player.IsOnFloor(),80,"catch-running-launch");
            driver.Command(InputActions.Jump,true);await Step();await Step();
            TestAssert.True(motor.AcceptedJumpCount==1 && motor.AcceptedDashCount==0 && !player.IsOnFloor(),
                "ordinary Space must really jump before a distinct Shift sample");
            driver.Command(InputActions.MoveRight,false);driver.Command(InputActions.MoveUp,true);
            driver.Command(InputActions.Dash,true);
            GD.Print($"ABYSS_B_COMMAND physics={Engine.GetPhysicsFrames()} ticks={ticks} seq={input.CurrentFrameSequence} W={Input.IsPhysicalKeyPressed(Key.W)} D={Input.IsPhysicalKeyPressed(Key.D)} Shift={Input.IsPhysicalKeyPressed(Key.Shift)}");
            await Until(()=>motor.AcceptedDashCount==1,8,"vertical-jet-actually-committed");
            GD.Print($"ABYSS_B_AFTER_STEP physics={Engine.GetPhysicsFrames()} ticks={ticks} seq={input.CurrentFrameSequence} aim={input.KeyboardAimDirection} accepted={motor.AcceptedDashCount} direction={motor.ThrusterState.BurnDirection}");
            TestAssert.True(motor.ThrusterState.BurnDirection.Dot(Vector2.Up)>.999f,
                "sampled W must be retained through the actual Shift edge, not replaced before jet commitment");
            driver.Command(InputActions.Dash,false);driver.Command(InputActions.MoveUp,false);
            driver.Command(InputActions.MoveRight,true);
            await Until(()=>player.IsOnFloor() && player.Position.X>4320 && player.Position.Y is >1620 and <1670,220,"real-B-upper-bank");
            driver.Command(InputActions.MoveRight,false);driver.Command(InputActions.Jump,false);
            for(int i=0;i<8;i++)await Step();
            TestAssert.True(motor.AcceptedJumpCount==1 && motor.AcceptedDashCount==1 && motor.AirDashAvailable &&
                player.IsOnFloor() && health.CurrentHealth==5 && injuries==0,
                "one ordinary jump/one finite jet reaches real bank and only genuine landing restores air use");
            GD.Print($"ABYSS_B_TRANSFER_OK ticks={ticks} jumps=1 jets=1 real_catch=true real_bank=true hp=5 same_player=true initial_fixture_only=true normal_route=false human=false");
        }
        finally
        {
            driver?.Dispose();health.Damaged-=damaged;afterMotor.Sample=null;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);
        }
    }
}
