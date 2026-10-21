using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Tests.Integration;
using Environment=System.Environment;

namespace Knight.Tests.Presentation;

// Three sequential arms in one real rendered process, preserving the enabled
// model and its memory when disabling it again. No gameplay/input/fog changes.
public static class MinimapNormalTimeProbe
{
    public static async Task Run(SceneTree tree)
    {
        long started=Stopwatch.GetTimestamp();
        TestAssert.True(DisplayServer.GetName()=="Windows" && RenderingServer.GetCurrentRenderingMethod()=="gl_compatibility" &&
            Engine.TimeScale==1 && !OS.GetCmdlineArgs().Contains("--fixed-fps") && !OS.GetCmdlineArgs().Contains("--write-movie"),"strict normal runtime, production GL and no fixed/movie/headless");
        var level=ResourceLoader.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn",cacheMode:ResourceLoader.CacheMode.Ignore).Instantiate<PrologueLevelController>();
        var paths=IsolatedPrologueFixture.Configure(level,"strict-normal-time");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");flow.HeadlessAutoStart=false;flow.AutomationAutoStart=false;
        tree.Root.AddChild(level);await Frames(tree,5);flow.StartNew();await Frames(tree,60);
        tree.Root.Show();tree.Root.GrabFocus();await Frames(tree,5);
        var actor=level.Player;var health=actor.GetNode<HealthComponent>("Health");
        var map=level.GetNode<PrologueMinimapController>("MinimapController");var model=map.Model;
        var view=level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap");var fog=level.GetNode<PrologueFogController>("FogController");
        var handler=(Action<FogOcclusionObservation>)typeof(PrologueMinimapController).GetMethod("OnObservation",BindingFlags.NonPublic|BindingFlags.Instance)!.CreateDelegate(typeof(Action<FogOcclusionObservation>),map);
        var arms=new[]{new MinimapStrictSampleBuffer(),new MinimapStrictSampleBuffer(),new MinimapStrictSampleBuffer()};
        ulong actorId=actor.GetInstanceId(),world=level.GetNode<Node2D>("WorldRoot").GetInstanceId();Vector2 position=actor.GlobalPosition;
        int hp=health.CurrentHealth,focusEvents=0;ulong support=Support(actor);
        Vector2I windowSize=tree.Root.Size;var viewport=tree.Root.GetVisibleRect();var vsync=DisplayServer.WindowGetVsyncMode();int maxFps=Engine.MaxFps;
        ulong generation=map.WorldGeneration;
        var settings=Settings(flow);
        string settingsJson=System.Text.Json.JsonSerializer.Serialize(flow.Settings);
        Action focus=()=>focusEvents++;tree.Root.FocusExited+=focus;tree.Root.FocusEntered+=focus;
        bool subscribed=true;long[] overhead=new long[1000];
        for(int i=0;i<overhead.Length;i++){long t=Stopwatch.GetTimestamp();overhead[i]=Stopwatch.GetTimestamp()-t;}
        GD.Print($"MINIMAP_NORMAL_CONTEXT renderer={RenderingServer.GetCurrentRenderingMethod()} device={RenderingServer.GetVideoAdapterName()} display={DisplayServer.GetName()} window={tree.Root.Size} visible={tree.Root.Visible} focus={tree.Root.HasFocus()} vsync={DisplayServer.WindowGetVsyncMode()} maxfps={Engine.MaxFps} timeScale={Engine.TimeScale} actor={actorId} world={world} support={support} position={position} hp={hp} frequency={Stopwatch.Frequency} warmup=120 intervals=600 per_arm=True");
        File.WriteAllText(Path.Combine(paths.CaseRoot,"settings-workload.json"),settingsJson);
        try
        {
            for(int arm=0;arm<3;arm++)
            {
                bool enabled=arm==1;
                if(enabled&&!subscribed){fog.ObservationPresented+=handler;subscribed=true;}
                if(!enabled&&subscribed){fog.ObservationPresented-=handler;subscribed=false;}
                map.ProcessMode=enabled?Node.ProcessModeEnum.Always:Node.ProcessModeEnum.Disabled;
                if(!enabled)view.ClearPresentation();
                ulong startRevision=model.RevisionCounter;
                for(int i=0;i<arms[arm].Endpoints.Length;i++)
                {
                    TestAssert.True((Stopwatch.GetTimestamp()-started)/(double)Stopwatch.Frequency<110,"bounded collection stops incomplete at110s before fixed120s outer cap, never substitutes samples");
                    await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    bool valid=flow.State==PrologueGameFlowState.Playing&&!tree.Paused&&actor.IsOnFloor()&&actor.GetInstanceId()==actorId&&health.CurrentHealth==hp&&
                        actor.GlobalPosition==position&&Support(actor)==support&&support!=0&&ReferenceEquals(model,map.Model)&&tree.Root.Visible&&Engine.TimeScale==1&&
                        level.GetNode<Node2D>("WorldRoot").GetInstanceId()==world&&map.WorldGeneration==generation&&tree.Root.Size==windowSize&&tree.Root.GetVisibleRect()==viewport&&
                        DisplayServer.WindowGetVsyncMode()==vsync&&Engine.MaxFps==maxFps&&Settings(flow)==settings;
                    arms[arm].Add(new MinimapTimingEndpoint(Stopwatch.GetTimestamp(),Engine.GetProcessFrames(),Environment.ProcessId,Environment.CurrentManagedThreadId,
                        world,actor.GetInstanceId(),Support(actor),health.CurrentHealth,actor.GlobalPosition,tree.Root.HasFocus(),valid));
                }
                Dump(paths.CaseRoot,arm,arms[arm]);
                TestAssert.True(enabled || model.RevisionCounter==startRevision,"test-only disabled event subscription actually prevents observation updates while retaining memory");
                GD.Print($"MINIMAP_NORMAL_ARM arm={arm} count={arms[arm].Count} invalid={arms[arm].InvalidIntervals} focusLoss={arms[arm].FocusLosses} drops={arms[arm].Drops} clock={arms[arm].ClockRegressions} overflow={arms[arm].Overflow} same_model={ReferenceEquals(model,map.Model)} revision_start={startRevision} revision_end={model.RevisionCounter}");
            }
            File.WriteAllLines(Path.Combine(paths.CaseRoot,"clock-overhead-ticks.csv"),Array.ConvertAll(overhead,v=>v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            bool complete=Array.TrueForAll(arms,a=>a.Complete)&&focusEvents==0;
            File.WriteAllText(Path.Combine(paths.CaseRoot,"strict-normal-result.txt"),$"complete={complete}\nfocusEvents={focusEvents}\nfrequency={Stopwatch.Frequency}\n");
            TestAssert.True(complete,"strict rendered normal dataset requires every endpoint valid/focused, zero events/drops/overflow/regression; never skip invalid intervals");
            double off=arms[0].P95Milliseconds(Stopwatch.Frequency),on=arms[1].P95Milliseconds(Stopwatch.Frequency),recovered=arms[2].P95Milliseconds(Stopwatch.Frequency);
            File.AppendAllText(Path.Combine(paths.CaseRoot,"strict-normal-result.txt"),FormattableString.Invariant($"off_p95_ms={off}\non_p95_ms={on}\nrecovered_p95_ms={recovered}\n"));
            GD.Print(FormattableString.Invariant($"MINIMAP_NORMAL_COMPARISON off={off} on={on} recovered={recovered} recovery10percent={Math.Abs(recovered-off)<=off*.1} on5percent={on<=1.05*Math.Max(off,recovered)}"));
            TestAssert.True(MinimapStrictSampleBuffer.Compare(off,on,recovered),"strict recovery10percent and feature5percent budget");
        }
        finally
        {
            for(int arm=0;arm<3;arm++)Dump(paths.CaseRoot,arm,arms[arm]);
            tree.Root.FocusExited-=focus;tree.Root.FocusEntered-=focus;
            if(!subscribed)fog.ObservationPresented+=handler;
            map.ProcessMode=Node.ProcessModeEnum.Always;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }
    private static void Dump(string root,int arm,MinimapStrictSampleBuffer samples)
    {
        var text=new StringBuilder("index,ticks,frame,process,thread,world,actor,support,health,x,y,focused,valid,interval_ticks,previous_focused,previous_valid\n");
        for(int i=0;i<samples.Count;i++){var e=samples.Endpoints[i];var p=i==0?default:samples.Endpoints[i-1];text.AppendLine(FormattableString.Invariant($"{i},{e.Ticks},{e.Frame},{e.Process},{e.Thread},{e.World},{e.Actor},{e.Support},{e.Health},{e.Position.X},{e.Position.Y},{e.Focused},{e.Valid},{(i==0?0:e.Ticks-p.Ticks)},{p.Focused},{p.Valid}"));}
        File.WriteAllText(Path.Combine(root,$"strict-arm-{arm}-endpoints.csv"),text.ToString());
    }
    private static ulong Support(CharacterBody2D actor)=>actor.GetSlideCollisionCount()>0&&actor.GetSlideCollision(0).GetCollider() is GodotObject collider?collider.GetInstanceId():0;
    private static (float,float,float,float,float,int,int,int,bool,float,float,bool,bool,int,bool) Settings(PrologueGameFlowController flow)
    {var s=flow.Settings;return(s.MasterVolume,s.MusicVolume,s.EffectsVolume,s.AmbienceVolume,s.UiVolume,(int)s.WindowMode,s.ResolutionWidth,s.ResolutionHeight,s.VSync,s.UiScale,s.ScreenShake,s.ReduceFlashes,s.ReducedFog,(int)s.Language,s.GuidanceEnabled);}
    private static async Task Frames(SceneTree tree,int count){for(int i=0;i<count;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
}
