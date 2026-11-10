using System;
using Environment=System.Environment;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;
using Knight.Ui;
using Knight.Settings;

namespace Knight.Tests.Integration;

public static class FirstGateAbyssRuntimeTests
{
    private static readonly string[] Sources={"FirstGateAbyssCampaign-v341.tscn","abyss-main.json","abyss-new.json"};
    private static readonly string[] MainIds=Enumerable.Range(1,12).Where(n=>n!=7).Select(n=>$"knight.first-gate.main.{n:00}.resource").ToArray();

    public static async Task Run(SceneTree tree,string storage)
    {
        var profile=FirstGateAbyssProfileTests.Run(storage);
        string parent=BuildSavedParent(storage,profile);
        using var scene=ResourceLoader.Load<PackedScene>(parent,cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=scene.Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"abyss-main-real-Q");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        KeyboardRouteInputDriver driver=null;int ticks=0,recoveries=0,windFrames=0;bool expectedRecovery=false;
        HealthComponent observedHealth=null;HealthComponent.DamagedEventHandler observedDamage=null;
        BasinBoardingTickObserver beforeMotor=null;
        void Recovered(string reason){recoveries++;GD.Print($"ABYSS_RECOVERY reason={reason} count={recoveries}");}
        level.RecoveryCompleted+=Recovered;tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.ProfileContract==profile && world.MountedSectorIds.Count==1 && level.Residents==null,"expanded graph Ready has one actual container, no old-map deployment");
            TestAssert.Equal(5,((ShaderMaterial)level.GetNode<PrologueFogCanvas>("Fog/Canvas").Material).GetShaderParameter("field_count").AsInt32(),"expanded graph uploads selected five fields");
            flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var completion=level.GetNode<PrologueCompletionController>("Completion");
            var formal=level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            completion.SetLanguage(GameLanguage.English);
            TestAssert.Equal("DEVELOPMENT: ENTRY 01–12 + BRANCHES A–F",formal.CurrentModel.Objective,
                "ABYSS_SCOPE_UI_RED: current displayed development scope must describe the actual expanded parent");
            completion.SetLanguage(GameLanguage.ChineseSimplified);
            TestAssert.Equal("开发样段：入口01–12＋A–F分路（并非完整首关）",formal.CurrentModel.Objective,"Chinese expanded scope remains explicitly partial");
            var player=level.Player;ulong actor=player.GetInstanceId();
            var health=player.GetNode<HealthComponent>("Health");var input=player.GetNode<PlayerInputReader>("Input");var motor=player.GetNode<PlayerMotor>("Motor");
            Transform2D beforeTransform=default;float beforeStep=0;ulong beforeTick=0;
            beforeMotor=new BasinBoardingTickObserver {ProcessPhysicsPriority=-1};
            beforeMotor.Sample=delta=>{beforeTransform=player.GlobalTransform;beforeStep=(float)delta;beforeTick=Engine.GetPhysicsFrames();};
            level.AddChild(beforeMotor);
            observedHealth=health;
            observedDamage=(remaining,impulse)=>
            {
                GD.Print($"ABYSS_DAMAGE hp={remaining} pos={player.Position} velocity={player.Velocity} height={motor.LastFallHeight} damage={motor.LastFallDamage} impact={motor.LastLandingImpact.Speed} reason={motor.LastLandingImpact.Reason} wall={player.IsOnWall()} floor={player.IsOnFloor()}");
                if(motor.LastFallHeight<1000)return;
                // Borrow the exact production query results inside Damaged, before
                // Died/recovery. No replay queries, Dispose, or retained native roots.
                var sampler=(LandingImpactSampler)typeof(PlayerMotor).GetField("_landingSampler",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(motor);
                var prediction=(KinematicCollision2D)typeof(LandingImpactSampler).GetField("_predicted",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sampler);
                var command=(motor.UpdraftCandidateVelocity+motor.UpdraftVelocityDelta)*beforeStep;
                float fraction=prediction.GetTravel().Dot(command)/command.LengthSquared();
                var moved=player.GlobalPosition-beforeTransform.Origin;
                GD.Print(FormattableString.Invariant($"ABYSS_IMPACT_PRE physics_before={beforeTick} physics_now={Engine.GetPhysicsFrames()} command={command} predicted_travel={prediction.GetTravel()} predicted_remainder={prediction.GetRemainder()} sum_error={(prediction.GetTravel()+prediction.GetRemainder()-command).Length():R} fraction={fraction:R} ray_error={(prediction.GetTravel()-command*fraction).Length():R} slides={player.GetSlideCollisionCount()}"));
                for(int i=0;i<player.GetSlideCollisionCount();i++)
                {
                    var contact=player.GetSlideCollision(i); // Borrowed; never Dispose.
                    var expected=contact.GetTravel()+contact.GetRemainder().Slide(contact.GetNormal());
                    GD.Print(FormattableString.Invariant($"ABYSS_IMPACT_POST index={i} moved={moved} actual_travel={contact.GetTravel()} actual_remainder={contact.GetRemainder()} normal_error={(contact.GetNormal()-prediction.GetNormal()).Length():R} travel_error={(contact.GetTravel()-prediction.GetTravel()).Length():R} remainder_error={(contact.GetRemainder()-prediction.GetRemainder()).Length():R} sum_error={(contact.GetTravel()+contact.GetRemainder()-command).Length():R} moved_error={(moved-expected).Length():R} depth_error={Math.Abs(contact.GetDepth()-prediction.GetDepth()):R}"));
                }
            };
            health.Damaged+=observedDamage;
            driver=KeyboardRouteInputDriver.Begin(player);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver?.ObserveFrame();ticks++;
                if(motor.UpdraftVelocityDelta.Y<0)windFrames++;
                if(player.Position.Y>4410 && player.Position.Y<4520)
                    GD.Print($"ABYSS_LANDING_TRACE pos={player.Position} vy={player.Velocity.Y} wind={motor.UpdraftVelocityDelta.Y} candidate={motor.UpdraftCandidateVelocity.Y} wall={player.IsOnWall()} floor={player.IsOnFloor()}");
                TestAssert.True(ticks<=14000 && player.GetInstanceId()==actor && (expectedRecovery || health.CurrentHealth>0 && recoveries==0),
                    $"continuous abyss route alive/same actor/no hidden recovery ticks={ticks} pos={player.Position} hp={health.CurrentHealth}");
            }
            async Task Until(Func<bool> done,int budget,string stage)
            {
                int count=0;while(!done() && count++<budget)await Step();
                GD.Print($"ABYSS_ROUTE stage={stage} ticks={ticks} pos={player.Position} floor={player.IsOnFloor()} hp={health.CurrentHealth} wind={windFrames}");
                TestAssert.True(done(),"ABYSS_ROUTE_RED: "+stage+" actual="+player.Position);
            }
            async Task Move(bool right,float x,string stage)
            {
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);
                await Until(()=>right?player.Position.X>=x:player.Position.X<=x,1800,stage);
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,false);
            }
            async Task Floor(float top,string stage)=>await Until(()=>player.IsOnFloor() && player.Position.Y>top-50 && player.Position.Y<top-25,300,stage);
            async Task Claim(int n)
            {
                string id=$"knight.first-gate.main.{n:00}.resource";
                await Until(()=>player.IsOnFloor() && player.Velocity.Length()<.1f,100,"Q-stand-"+n);
                driver.Command(InputActions.Interact,true);for(int i=0;i<3;i++)await Step();
                driver.Command(InputActions.Interact,false);for(int i=0;i<3;i++)await Step();
                TestAssert.True(level.Progress.ClaimedRewardIds.Contains(id) && PrologueSaveStore.LoadCandidate(paths.SavePath,profile).Data.ClaimedRewardIds.Contains(id),"real placed Q persisted "+id);
            }
            async Task Jump(bool right,float x,string stage)
            {
                await Until(()=>player.IsOnFloor() && Math.Abs(player.Velocity.X)<1,40,stage+"-grounded-launch");
                int before=motor.AcceptedJumpCount;
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);driver.Command(InputActions.Jump,true);
                await Until(()=>player.IsOnFloor() && (right?player.Position.X>x:player.Position.X<x),180,stage);
                driver.Command(InputActions.Jump,false);driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,false);
                TestAssert.Equal(before+1,motor.AcceptedJumpCount,"one separately grounded ordinary jump "+stage);
            }
            await Floor(512,"01-spawn");await Move(true,576,"01-resource");await Claim(1);
            await Move(true,1180,"02-rift");await Floor(864,"02-support");await Claim(2);
            await Move(false,704,"03-side-cave");await Floor(1248,"03-support");await Claim(3);
            await Move(false,280,"04-turn");await Floor(1696,"04-support");
            await Move(true,1605,"04-gap");await Jump(true,1780,"04-single-jump");
            await Move(true,2464,"04-resource");await Claim(4);
            await Move(true,4048,"05-open-real-handoff");await Floor(1984,"05-mid-gallery-support");await Claim(5);
            await Move(true,4200,"05-drop-lower-gallery");await Floor(2240,"06-lower-gallery-support");
            await Move(false,2688,"06-waist-resource");await Claim(6);
            await Move(false,1600,"07-long-wind-entry");
            driver.Command(InputActions.MoveRight,true);
            await Until(()=>player.Velocity.X>=0,60,"07-real-opposite-key-brake");
            driver.Command(InputActions.MoveRight,false);int fallStart=ticks;
            await Floor(4512,"07-unbroken-wind-bottom");int fallTicks=ticks-fallStart;
            TestAssert.True(fallTicks>=240 && fallTicks<=360 && windFrames>180,"actual long wind descent lasts4–6physics seconds, not an injected wait");
            await Move(true,4352,"08-turn-right-down");await Floor(5088,"08-low-rejoin");await Claim(8);
            await Move(false,1600,"09-west-corridor");await Claim(9);
            await Move(false,1280,"10-first-well-entry");await Floor(5408,"10-real-catch");
            await Move(false,1120,"10-leave-catch");await Floor(5728,"10-bottom-corridor");
            await Move(true,1600,"10-resource");await Claim(10);
            await Move(true,3728,"10-second-well-entry");await Floor(6112,"10-second-catch");
            await Move(true,3870,"11-leave-catch");await Floor(6656,"11-forest-floor");
            for(int i=2;i>=0;i--)
            {
                float left=2016+i*512;await Move(false,left+92,"11-pillar-"+i+"-approach");
                await Jump(false,left-36,"11-pillar-"+i+"-ordinary-jump");
            }
            await Move(false,1600,"11-resource");await Claim(11);
            await Move(false,1088,"12-bottom-turn");await Floor(7072,"12-real-catch");
            await Move(true,1260,"12-leave-catch");await Floor(7488,"12-final-corridor");
            await Move(true,5184,"12-resource");await Claim(12);await Move(true,5480,"12-physical-end");
            TestAssert.True(world.CheckpointId=="knight.first-gate.bottom-refuge" && level.Progress.ClaimedRewardIds.Order().SequenceEqual(MainIds.Order()) &&
                level.Progress.ResourceTotal("mineral")==11 && !flow.TryComplete() && !flow.Save.Completed,
                "normal main route obtains eleven literal reachable main resources;07side-rest andsix branches not falsely counted");
            TestAssert.True(motor.AcceptedJumpCount==4 && motor.AcceptedDashCount==0,"one gap/three pillars grounded jumps and no hidden propulsion grants");
            driver.Dispose();driver=null;
            expectedRecovery=true;
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,Vector2.Zero,0f,934123UL,actor,DamageGuardClass.Unblockable)),"real bottom checkpoint death accepted");
            await Until(()=>recoveries==1 && flow.State==PrologueGameFlowState.Playing,180,"12-real-bottom-recovery");
            TestAssert.True(Math.Abs(player.Position.X-5376)<1 && Math.Abs(player.Position.Y-7448)<8 && player.GetInstanceId()==actor,"bottom checkpoint restores same actor at actual canonical point");
            flow.PersistProgress("abyss-main-real-Q");File.Copy(paths.SavePath,Path.Combine(storage,Sources[1]),false);
            flow.ReturnToTitle();string sha=Hash(paths.SavePath);flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(Hash(paths.SavePath)==sha && level.Progress.ClaimedRewardIds.Order().SequenceEqual(MainIds.Order()),"actual selected Continue readonly eleven grants");
            flow.ReturnToTitle();flow.StartNew();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(level.Progress.ClaimedRewardIds.Count==0 && world.CheckpointId==profile.Start.StableId,"new expanded run resets only its own profile");
            File.Copy(paths.SavePath,Path.Combine(storage,Sources[2]),false);
            File.WriteAllText(Path.Combine(storage,"abyss-cold-manifest.json"),System.Text.Json.JsonSerializer.Serialize(new{writerPid=Environment.ProcessId,hashes=Sources.Select(n=>Hash(Path.Combine(storage,n))).ToArray()}));
            GD.Print($"ABYSS_MAIN_WARM_OK writer_pid={Environment.ProcessId} actual_Q=11 jumps=4 dash=0 wind_ticks={fallTicks} same_player=true death=1 continue_readonly=true new=true all30=false branches=false human=false");
        }
        finally
        {
            if(observedHealth!=null && GodotObject.IsInstanceValid(observedHealth) && observedDamage!=null)observedHealth.Damaged-=observedDamage;
            if(beforeMotor!=null && GodotObject.IsInstanceValid(beforeMotor))beforeMotor.Sample=null;
            driver?.Dispose();level.RecoveryCompleted-=Recovered;await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }

    internal static string BuildSavedParent(string storage,PrologueWorldProfile profile,string fileName="FirstGateAbyssCampaign-v341.tscn")
    {
        string leaf=FirstGateAbyssTerrainTests.Run(storage);using var scene=GD.Load<PackedScene>(leaf);
        using var def=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,scene,Vector2.Zero,FirstGateAbyssTerrain.Bounds,null,null);
        var level=BuildPrologueLevel.BuildCandidate(profile,new[]{def});
        var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var catalog=level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
        try
        {
            TestAssert.Equal("user://knight/knight.first-gate.abyss.v1.json",level.GetNode<PrologueGameFlowController>("GameFlow").SavePath,"expanded candidate distinct default slot before Ready");
            TestAssert.True(catalog.CellCount==786432 && level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints.Count==4,"actual saved large map/checkpoint table");
            string path=Path.Combine(storage,fileName);TestAssert.Equal(Error.Ok,BuildPrologueLevel.SaveCandidate(level,path),"save full expanded common graph externally");return path;
        }
        finally
        {
            var definitions=world.Definitions.ToArray();var snapshots=definitions.Select(d=>d.Scene).Where(s=>s!=null && !ReferenceEquals(s,scene)).Distinct().ToArray();
            world.Definitions.Clear();level.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;
            foreach(var item in definitions)item.Scene=null;level.Free();foreach(var item in definitions)item.Dispose();foreach(var snapshot in snapshots)snapshot.Dispose();catalog.Dispose();
        }
    }
    public static async Task RunCold(SceneTree tree,string storage)
    {
        using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(storage,"abyss-cold-manifest.json")));
        int writer=document.RootElement.GetProperty("writerPid").GetInt32();var hashes=document.RootElement.GetProperty("hashes").EnumerateArray().Select(x=>x.GetString()).ToArray();
        TestAssert.True(writer>0 && writer!=Environment.ProcessId && hashes.Length==3,"distinct cold writer/reader with fixed three sources");
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,Sources[i])),"cold source SHA handoff");
        using var scene=ResourceLoader.Load<PackedScene>(Path.Combine(storage,Sources[0]),cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=scene.Instantiate<PrologueLevelController>();var paths=IsolatedPrologueFixture.Configure(level,"abyss-independent-cold");
        File.Copy(Path.Combine(storage,Sources[1]),paths.SavePath,false);string sha=Hash(paths.SavePath);
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.ProfileContract.Id=="knight.first-gate.abyss.v1" && world.CheckpointId=="knight.first-gate.bottom-refuge" &&
                level.Progress.ClaimedRewardIds.Order().SequenceEqual(MainIds.Order()) && level.Progress.ResourceTotal("mineral")==11 && Hash(paths.SavePath)==sha,
                "actual cold Continue restores expanded world/eleven grants/bottom checkpoint read-only");
            TestAssert.True(Math.Abs(level.Player.Position.X-5376)<1 && Math.Abs(level.Player.Position.Y-7448)<8,"cold actual canonical bottom actor position");
            TestAssert.True(PrologueSaveStore.LoadCandidate(Path.Combine(storage,Sources[2]),world.ProfileContract).Data.ClaimedRewardIds.Count==0,"actual New witness stays empty");
        }
        finally{await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
        for(int i=0;i<3;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,Sources[i])),"reader never rewrites original source witnesses");
        GD.Print($"ABYSS_COLD_OK writer_pid={writer} reader_pid={Environment.ProcessId} witnesses=3 actual_continue=true grants=11 source_readonly=true human=false");
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
