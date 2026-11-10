using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Environment=System.Environment;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Interactions;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class FirstGateForecourtRouteTests
{
    private static readonly string[] MidSources={"FirstGateForecourtCampaign-v344.tscn","forecourt-mid-route.json"};
    private static readonly string[] MidIds=Enumerable.Range(13,6).Select(n=>$"knight.first-gate.main.{n:00}.resource").ToArray();
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    // Controlled checkpoint recovery, never normal start-to-gate evidence.
    public static async Task RunAscent(SceneTree tree,string storage,bool fullColumn=false,bool fromBottom=false)
    {
        TestAssert.True(PrologueWorldProfiles.TryResolve("knight.first-gate.forecourt.v1",out var profile),"forecourt profile");
        string parent=FirstGateForecourtTerrainTests.BuildSavedParent(storage);
        using var packed=ResourceLoader.Load<PackedScene>(parent,cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=packed.Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"forecourt-ascent-controlled");
        var seed=PrologueSaveData.NewForProfile(profile);
        seed.CheckpointId=fromBottom?"knight.first-gate.bottom-refuge":"knight.first-gate.forecourt-refuge";
        seed=seed.Normalize(profile);
        PrologueSaveStore.SaveAtomic(paths.SavePath,seed,profile:profile);
        byte[] seededBytes=File.ReadAllBytes(paths.SavePath);
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        KeyboardRouteInputDriver driver=null;
        MechanicalClawController claw=null;
        Action<MechanicalClawFrame,GrappleSurface2D> observed=null;
        int ticks=0,recoveries=0;
        var samples=new Queue<string>();var events=new Queue<string>();
        var states=new List<MechanicalClawState>();
        ulong releaseCast=0;bool watchRelease=false;
        var releaseEvents=new List<(ulong Cast,MechanicalClawState State,MechanicalClawReleaseKind Kind)>();
        string supportingContact=null;
        void Recovered(string reason)=>recoveries++;
        level.RecoveryCompleted+=Recovered;
        tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.HasSave && flow.LoadResult.Status==PrologueSaveLoadStatus.Current && !flow.Save.WasRecovered,
                "isolated canonical seed loads before Continue");
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            TestAssert.True(File.ReadAllBytes(paths.SavePath).AsSpan().SequenceEqual(seededBytes),"Continue does not rewrite selected slot");
            var player=level.Player;ulong actor=player.GetInstanceId();
            var input=player.GetNode<PlayerInputReader>("Input");
            var motor=player.GetNode<PlayerMotor>("Motor");
            var health=player.GetNode<HealthComponent>("Health");
            var interaction=player.GetNode<EnvironmentInteractionController>("EnvironmentInteraction");
            claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonEntrance,out var sector),"actual saved forecourt sector mounted");
            var lower=sector.GetNode<StaticBody2D>("Gameplay/Collision/Column16LowerCatch");
            var target=lower.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            ulong targetId=target.GetInstanceId(),floorId=lower.GetInstanceId();
            observed=(frame,actual)=>
            {
                // Only scalar snapshots. No retained target/resource/query or
                // observation that mutates physics or extends object lifetime.
                if(states.Count==0 || states[^1]!=frame.State)
                {
                    if(events.Count==128)events.Dequeue();
                    events.Enqueue($"t={ticks} cast={frame.CastId} state={frame.State} fail={frame.Failure} locked={frame.LockedTargetRuntimeId} anchor={frame.AnchorPoint} normal={frame.SurfaceNormal} dock={frame.DockPoint} arrived={frame.Arrived} at={frame.CasterWorldPosition} vel={frame.CasterVelocity} release={frame.ReleaseKind}");
                    states.Add(frame.State);
                }
                if(watchRelease && frame.CastId==releaseCast && frame.State is MechanicalClawState.Boost or MechanicalClawState.Released)
                    releaseEvents.Add((frame.CastId,frame.State,frame.ReleaseKind));
            };
            claw.ClawFrameChanged+=observed;
            driver=KeyboardRouteInputDriver.Begin(player);
            GD.Print($"FORECOURT_ASCENT_BEGIN controlled_checkpoint_continue=true seed={seed.CheckpointId} actor={actor} expected_target={targetId} expected_floor={floorId} full_route=false");
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver.ObserveFrame();ticks++;
                if(samples.Count==256)samples.Dequeue();
                samples.Enqueue($"t={ticks} p={player.Position} v={player.Velocity} floor={player.IsOnFloor()} wall={player.IsOnWall()} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure} arrived={claw.CurrentFrame.Arrived} hp={health.CurrentHealth} jumps={motor.AcceptedJumpCount} dashes={motor.AcceptedDashCount}");
                TestAssert.True(ticks<=(fullColumn?6000:900) && player.GetInstanceId()==actor && health.CurrentHealth==5 && recoveries==0,
                    $"controlled ascent retains same living player/fullHP/no recovery t={ticks} pos={player.Position}");
            }
            async Task Until(Func<bool> done,int budget,string stage)
            {
                for(int i=0;i<budget && !done();i++)await Step();
                GD.Print($"FORECOURT_ASCENT_STAGE stage={stage} ticks={ticks} pos={player.Position} velocity={player.Velocity} claw={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} anchor={claw.CurrentFrame.AnchorPoint} normal={claw.CurrentFrame.SurfaceNormal} dock={claw.CurrentFrame.DockPoint}");
                TestAssert.True(done(),"FORECOURT_ASCENT_ROUTE_RED: "+stage);
            }
            async Task Move(float x,string stage,int budget=240)
            {
                bool right=player.Position.X<x;
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);
                await Until(()=>right?player.Position.X>=x:player.Position.X<=x,budget,stage);
                driver.ReleaseAll();
                await Until(()=>player.IsOnFloor() && player.Velocity.Length()<1,90,stage+"-settled");
            }
            async Task Pulse()
            {
                driver.Command(InputActions.Interact,true);for(int i=0;i<3;i++)await Step();
                driver.Command(InputActions.Interact,false);for(int i=0;i<3;i++)await Step();
            }
            async Task Claim(int n)
            {
                string id=$"knight.first-gate.main.{n:00}.resource";
                await Until(()=>player.IsOnFloor() && player.Velocity.Length()<1 && interaction.CurrentCandidate?.StableId==id,30,"actual-Q-candidate-"+n);
                var cache=interaction.CurrentCandidate as PrologueResourceCache;
                int count=level.Progress.ClaimedRewardIds.Count;
                TestAssert.True(cache is {IsClaimed:false,FeedbackCount:0},"fresh actual cache before Q "+n);
                await Pulse();
                TestAssert.True(cache.IsClaimed && cache.FeedbackCount==1 && cache.LastResult==ResourceRewardClaimResult.Claimed &&
                    level.Progress.ClaimedRewardIds.Count==count+1 && level.Progress.ClaimedRewardIds.Contains(id) &&
                    PrologueSaveStore.LoadCandidate(paths.SavePath,profile).Data.ClaimedRewardIds.Contains(id),"actual Q and durable unique grant "+n);
                GD.Print($"FORECOURT_MID_Q id={id} count={level.Progress.ClaimedRewardIds.Count} same_actor={actor}");
            }
            await Until(()=>player.IsOnFloor() && player.Velocity.Length()<1,60,"canonical refuge settled");
            TestAssert.True(Math.Abs(player.Position.X-seed.CheckpointWorldX)<1 && Math.Abs(player.Position.Y-7452)<8,"real canonical refuge floor");
            if(fromBottom)
            {
                await Move(6048,"13-real-rock-opening",1200);await Claim(13);
                await Move(7424,"14-low-mountain-passage",1200);await Claim(14);
                await Move(8896,"15-real-refuge-contact",1200);await Claim(15);
                TestAssert.True(world.CheckpointId=="knight.first-gate.forecourt-refuge" && flow.Save.CheckpointId==world.CheckpointId,
                    "physically reached15 replaces the seeded bottom checkpoint through real contact");
            }
            TestAssert.True(Math.Abs(player.Position.X-8896)<32 && Math.Abs(player.Position.Y-7452)<8,"actual15 launch floor");
            int jumps=motor.AcceptedJumpCount,dashes=motor.AcceptedDashCount;
            driver.Command(InputActions.MoveUp,true);driver.Command(InputActions.MoveRight,true);await Step();
            driver.Command(InputActions.Claw,true);await Step();
            driver.Command(InputActions.Claw,false);
            // The key adapter and production Aim/Windup are sampled on later
            // physics ticks. Hold real diagonal keys until the actual cast
            // enters Travel; never substitute an intended AimAt position.
            await Until(()=>claw.CurrentFrame.State==MechanicalClawState.Travel,24,"sampled diagonal cast enters Travel");
            driver.Command(InputActions.MoveUp,false);driver.Command(InputActions.MoveRight,false);
            await Until(()=>claw.CurrentFrame.State==MechanicalClawState.Swing && claw.CurrentFrame.Arrived,240,"real lower-catch side arrival");
            var arrived=claw.CurrentFrame;
            TestAssert.True(states.Contains(MechanicalClawState.Reel) && claw.CurrentTarget==target && arrived.LockedTargetRuntimeId==targetId &&
                arrived.SurfaceNormal.Dot(Vector2.Left)>.99f && Math.Abs(arrived.AnchorPoint.X-9344)<1 && arrived.AnchorPoint.Y is >=7040 and <=7072 &&
                arrived.CasterWorldPosition.DistanceTo(arrived.DockPoint)<=8.01f,
                "actual target/left native face/reel/dock, not merely intended aim or any attachment");
            releaseCast=arrived.CastId;watchRelease=true;
            driver.Command(InputActions.MoveLeft,true);driver.Command(InputActions.Jump,true);await Step();
            await Until(()=>releaseEvents.Count>=2,8,"actual attached Space publication");
            watchRelease=false;
            driver.Command(InputActions.Jump,false);driver.Command(InputActions.MoveLeft,false);
            TestAssert.True(releaseEvents.Count==2 && releaseEvents.Select(e=>e.State).SequenceEqual(new[]{MechanicalClawState.Boost,MechanicalClawState.Released}) &&
                releaseEvents.All(e=>e.Cast==releaseCast && e.Kind==MechanicalClawReleaseKind.Boost) && motor.AcceptedJumpCount==jumps,
                "attached Space publishes actual Boost then Released without an ordinary extra jump");
            driver.Command(InputActions.MoveRight,true);
            await Until(()=>player.Position.X>9392,100,"cross actual lower-catch west edge");
            driver.Command(InputActions.MoveRight,false);
            bool OnLower()
            {
                if(!player.IsOnFloor())return false;
                for(int i=0;i<player.GetSlideCollisionCount();i++)
                {
                    var contact=player.GetSlideCollision(i); // borrowed collision, never disposed
                    if(contact.GetColliderId()!=floorId || contact.GetNormal().Dot(Vector2.Up)<.99f)continue;
                    supportingContact=$"physics={Engine.GetPhysicsFrames()} collider={contact.GetColliderId()} normal={contact.GetNormal()} point={contact.GetPosition()} player={player.Position}";
                    return true;
                }
                return false;
            }
            await Until(OnLower,120,"native supporting contact on first catch");
            TestAssert.True(player.Position.X is >9364 and <9708 && Math.Abs(player.Position.Y-7004)<3 &&
                motor.AcceptedJumpCount==jumps && motor.AcceptedDashCount==dashes && driver.ClawEdges==1,
                "native upper landing with one K/attached Space, no dash, pose injection or extra jump");
            driver.ReleaseAll();for(int i=0;i<8;i++)await Step();
            TestAssert.True(OnLower(),"stable first-catch landing after release");
            GD.Print("FORECOURT_ASCENT_NATIVE_SUPPORT "+supportingContact);
            File.WriteAllText(Path.Combine(storage,"forecourt-ascent-route.json"),JsonSerializer.Serialize(new
                {passed=true,controlled_checkpoint_continue=true,full_route=false,human=false,ticks,actor,targetId,floorId,recoveries,
                 supportingContact,releaseEvents=releaseEvents.Select(e=>new{e.Cast,state=e.State.ToString(),kind=e.Kind.ToString()}).ToArray(),
                 samples=samples.ToArray(),events=events.ToArray()}));
            GD.Print($"FORECOURT_ASCENT_OK controlled_checkpoint_continue=true side_target=true reel_arrived=true attached_boost=true native_floor=true actor={actor} samples={ticks} recoveries={recoveries} extra_jump=false dash=false full_route=false human=false");
            if(fullColumn)
            {
                async Task Transfer(string name,bool toRight,float approach,float cross,float floorY)
                {
                    await Move(approach,name+"-launch");
                    var body=sector.GetNode<StaticBody2D>("Gameplay/Collision/"+name);
                    var expected=body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
                    ulong expectedId=expected.GetInstanceId(),supportId=body.GetInstanceId();
                    driver.Command(InputActions.MoveUp,true);driver.Command(toRight?InputActions.MoveRight:InputActions.MoveLeft,true);
                    await Step();driver.Command(InputActions.Claw,true);await Step();driver.Command(InputActions.Claw,false);
                    await Until(()=>claw.CurrentFrame.State is MechanicalClawState.Travel or MechanicalClawState.Failed,24,name+"-native-cast");
                    driver.ReleaseAll();
                    GD.Print($"FORECOURT_COLUMN_TARGET expected={expectedId} actual={claw.CurrentFrame.LockedTargetRuntimeId} at={player.Position} anchor={claw.CurrentFrame.AnchorPoint} normal={claw.CurrentFrame.SurfaceNormal}");
                    TestAssert.True(claw.CurrentFrame.LockedTargetRuntimeId==expectedId && claw.CurrentTarget==expected,
                        "FORECOURT_COLUMN_ROUTE_RED: actual first solid is intended next side catch "+name);
                    await Until(()=>claw.CurrentFrame.State==MechanicalClawState.Swing && claw.CurrentFrame.Arrived,240,name+"-arrival");
                    Vector2 normal=toRight?Vector2.Left:Vector2.Right;
                    TestAssert.True(claw.CurrentFrame.SurfaceNormal.Dot(normal)>.99f,"native side not inaccessible underside "+name);
                    releaseCast=claw.CurrentFrame.CastId;releaseEvents.Clear();watchRelease=true;
                    driver.Command(toRight?InputActions.MoveLeft:InputActions.MoveRight,true);driver.Command(InputActions.Jump,true);
                    await Until(()=>releaseEvents.Count>=2,8,name+"-attached-boost");watchRelease=false;
                    TestAssert.True(releaseEvents.Count==2 && releaseEvents[0].State==MechanicalClawState.Boost && releaseEvents[1].State==MechanicalClawState.Released,
                        "one actual atomic attached release "+name);
                    driver.ReleaseAll();driver.Command(toRight?InputActions.MoveRight:InputActions.MoveLeft,true);
                    await Until(()=>toRight?player.Position.X>=cross:player.Position.X<=cross,100,name+"-edge");driver.ReleaseAll();
                    bool OnSupport()
                    {
                        if(!player.IsOnFloor())return false;
                        for(int i=0;i<player.GetSlideCollisionCount();i++)
                        {
                            var contact=player.GetSlideCollision(i);
                            if(contact.GetColliderId()==supportId && contact.GetNormal().Dot(Vector2.Up)>.99f)
                            {
                                GD.Print($"FORECOURT_COLUMN_NATIVE_SUPPORT name={name} collider={supportId} normal={contact.GetNormal()} point={contact.GetPosition()} player={player.Position}");
                                return true;
                            }
                        }
                        return false;
                    }
                    await Until(OnSupport,120,name+"-support");
                    TestAssert.True(Math.Abs(player.Position.Y-(floorY-36))<3 && motor.AcceptedJumpCount==jumps && motor.AcceptedDashCount==dashes,
                        "actual native support without extra jump or jet "+name);
                }
                await Transfer("Column16EastCatch",true,9480,9980,6592);
                await Transfer("Column16WestCatch",false,10096,9660,6144);
                await Transfer("Column16UpperLanding",true,9480,9980,5664);
                if(fromBottom){await Move(9952,"16-cache-safe-approach");await Claim(16);}
                await Move(10880,"17-gallery-resource-approach");
                if(fromBottom)await Claim(17);
                TestAssert.True(player.Position.Y is >5625 and <5635 && driver.ClawEdges==4,"continuous15→16 four catches→17 actual support");
                GD.Print($"FORECOURT_COLUMN_OK controlled_checkpoint_continue=true catches=4 same_actor={actor} extra_jump=false dash=false recoveries={recoveries} samples={ticks} full_route=false human=false");
                if(fromBottom)
                {
                    await Move(11552,"18-real-entrance",480);await Claim(18);
                    string beforeReplay=Hash(paths.SavePath);await Pulse();
                    TestAssert.True(beforeReplay==Hash(paths.SavePath) && level.Progress.ClaimedRewardIds.Order().SequenceEqual(MidIds.Order()) &&
                        level.Progress.ResourceTotal("salvage")==6 && !flow.Save.Completed && !flow.Save.BossDefeated &&
                        flow.Save.CheckpointId=="knight.first-gate.forecourt-refuge","all six real grants, replay idempotence, actual15 checkpoint and no formal completion");
                    File.Copy(paths.SavePath,Path.Combine(storage,MidSources[1]),false);
                    File.WriteAllText(Path.Combine(storage,"forecourt-mid-route-cold-manifest.json"),JsonSerializer.Serialize(new
                        {writerPid=Environment.ProcessId,hashes=MidSources.Select(f=>Hash(Path.Combine(storage,f))).ToArray()}));
                    GD.Print($"FORECOURT_MID_WARM_OK writer_pid={Environment.ProcessId} witnesses=2 actual_Q=6 checkpoint15_contact=true same_actor={actor} catches=4 recoveries=0 full_route=false human=false");
                }
            }
        }
        catch
        {
            File.WriteAllText(Path.Combine(storage,"forecourt-ascent-failure.json"),JsonSerializer.Serialize(new
                {controlled_checkpoint_continue=true,full_route=false,ticks,recoveries,supportingContact,
                 releaseEvents=releaseEvents.Select(e=>new{e.Cast,state=e.State.ToString(),kind=e.Kind.ToString()}).ToArray(),
                 samples=samples.ToArray(),events=events.ToArray()}));
            foreach(string sample in samples.TakeLast(12))GD.Print("FORECOURT_ASCENT_SAMPLE "+sample);
            foreach(string sample in events.TakeLast(12))GD.Print("FORECOURT_ASCENT_EVENT "+sample);
            throw;
        }
        finally
        {
            driver?.Dispose();if(claw!=null && observed!=null)claw.ClawFrameChanged-=observed;
            level.RecoveryCompleted-=Recovered;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }

    public static async Task RunMidCold(SceneTree tree,string storage)
    {
        using var receipt=JsonDocument.Parse(File.ReadAllText(Path.Combine(storage,"forecourt-mid-route-cold-manifest.json")));
        int writer=receipt.RootElement.GetProperty("writerPid").GetInt32();
        string[] hashes=receipt.RootElement.GetProperty("hashes").EnumerateArray().Select(x=>x.GetString()).ToArray();
        TestAssert.True(writer>0 && writer!=Environment.ProcessId && hashes.Length==2,"distinct mid-route cold reader");
        for(int i=0;i<2;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,MidSources[i])),"mid-route cold source hash");
        using var packed=ResourceLoader.Load<PackedScene>(Path.Combine(storage,MidSources[0]),cacheMode:ResourceLoader.CacheMode.Ignore);
        var level=packed.Instantiate<PrologueLevelController>();var paths=IsolatedPrologueFixture.Configure(level,"forecourt-mid-cold");
        File.Copy(Path.Combine(storage,MidSources[1]),paths.SavePath,false);string selected=Hash(paths.SavePath);
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            flow.Continue();await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,level);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.ProfileContract.Id=="knight.first-gate.forecourt.v1" && world.CheckpointId=="knight.first-gate.forecourt-refuge" &&
                !flow.Save.WasRecovered && !flow.Save.Completed && !flow.Save.BossDefeated &&
                level.Progress.ClaimedRewardIds.Order().SequenceEqual(MidIds.Order()) && level.Progress.ResourceTotal("salvage")==6 &&
                Math.Abs(level.Player.Position.X-8896)<1 && Math.Abs(level.Player.Position.Y-7448)<8,
                "cold actual Continue restores physically earned15 checkpoint and six real13–18 Q grants");
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonEntrance,out var sector),"cold sector present");
            foreach(var cache in sector.GetNode("Gameplay/ResourceCaches").GetChildren().OfType<PrologueResourceCache>())
                TestAssert.Equal(MidIds.Contains(cache.RewardId),cache.IsClaimed,"cold cache runtime restoration "+cache.RewardId);
            TestAssert.Equal(selected,Hash(paths.SavePath),"cold Continue never rewrites the selected isolated slot");
        }
        finally{await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);}
        for(int i=0;i<2;i++)TestAssert.Equal(hashes[i],Hash(Path.Combine(storage,MidSources[i])),"cold sources remain byte-identical");
        GD.Print($"FORECOURT_MID_COLD_OK writer_pid={writer} reader_pid={Environment.ProcessId} witnesses=2 actual_continue=true grants=6 checkpoint15=true source_readonly=true full_route=false human=false");
    }
}
