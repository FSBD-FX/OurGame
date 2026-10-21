using System;
using System.Linq;
using Godot;

namespace Knight.Tests.Presentation;

internal readonly record struct MinimapTimingEndpoint(long Ticks,ulong Frame,int Process,int Thread,
    ulong World,ulong Actor,ulong Support,int Health,Vector2 Position,bool Focused,bool Valid);

internal sealed class MinimapStrictSampleBuffer
{
    internal readonly MinimapTimingEndpoint[] Endpoints;
    internal int Count,InvalidIntervals,FocusLosses,ClockRegressions,Drops,Overflow;
    internal readonly int Warmup,Required;
    internal MinimapStrictSampleBuffer(int warmup=120,int required=600)
    {
        if(warmup<0 || required<1 || warmup+required+1>8192)throw new ArgumentException("Timing window bounds.");
        Warmup=warmup;Required=required;Endpoints=new MinimapTimingEndpoint[warmup+required+1];
    }
    internal void Add(MinimapTimingEndpoint endpoint)
    {
        if(Count==Endpoints.Length){Overflow++;return;}
        if(Count>0)
        {
            var previous=Endpoints[Count-1];
            if(previous.Focused && !endpoint.Focused)FocusLosses++;
            if(endpoint.Ticks<=previous.Ticks)ClockRegressions++;
            if(endpoint.Frame!=previous.Frame+1)Drops++;
            if(!previous.Valid || !endpoint.Valid || !previous.Focused || !endpoint.Focused || previous.Health<=0 || endpoint.Ticks<=previous.Ticks || endpoint.Frame!=previous.Frame+1 ||
                endpoint.Process!=previous.Process || endpoint.Thread!=previous.Thread || endpoint.World!=previous.World ||
                endpoint.Actor!=previous.Actor || endpoint.Support!=previous.Support || endpoint.Health<=0 ||
                endpoint.Health!=previous.Health || endpoint.Position!=previous.Position)InvalidIntervals++;
        }
        Endpoints[Count++]=endpoint;
    }
    internal bool Complete=>Count==Endpoints.Length && InvalidIntervals==0 && FocusLosses==0 && ClockRegressions==0 && Drops==0 && Overflow==0;
    internal double P95Milliseconds(long frequency)
    {
        if(!Complete)throw new InvalidOperationException("Invalid measuring dataset cannot be summarized as certified.");
        long[] ticks=new long[Required];
        for(int i=0;i<Required;i++)ticks[i]=Endpoints[Warmup+i+1].Ticks-Endpoints[Warmup+i].Ticks;
        Array.Sort(ticks);return ticks[(int)Math.Ceiling(Required*.95)-1]*1000d/frequency;
    }
    internal static bool Compare(double off,double on,double recovered)=>double.IsFinite(off)&&off>0 && double.IsFinite(on)&&double.IsFinite(recovered) &&
        Math.Abs(recovered-off)<=off*.1 && on<=1.05*Math.Max(off,recovered);
}

internal static class MinimapStrictSampleBufferTests
{
    internal static void Run()
    {
        var a=new MinimapTimingEndpoint(10,1,1,1,1,1,1,5,Vector2.Zero,true,true);
        var b=a with{Ticks=20,Frame=2};var c=a with{Ticks=30,Frame=3};
        MinimapStrictSampleBuffer Window(params MinimapTimingEndpoint[] values){var window=new MinimapStrictSampleBuffer(0,2);foreach(var v in values)window.Add(v);return window;}
        TestAssert.True(!Window(a with{Valid=false},b,c).Complete,"StrictIntervalsRejectInvalidFirstEndpoint: valid next endpoint cannot bridge an invalid first");
        TestAssert.True(!Window(a,b,c with{Valid=false}).Complete,"invalid last endpoint rejects dataset");
        TestAssert.True(!Window(a,b with{Focused=false},c).Complete,"focus loss and recovered focus cannot bridge");
        TestAssert.True(!Window(a,b with{Frame=3},c with{Frame=4}).Complete,"nonadjacent frames reject dataset");
        TestAssert.True(!Window(a,b).Complete,"early exit remains incomplete");
        TestAssert.True(!Window(a,b,c,c with{Ticks=40,Frame=4}).Complete,"overflow permanently invalidates dataset");
        TestAssert.True(!Window(a,b,c with{Ticks=19}).Complete,"clock regression rejects dataset");
        TestAssert.True(!Window(a,b,c with{Thread=2}).Complete && !Window(a,b,c with{Actor=2}).Complete,"thread/world living actor workload match both endpoints");
        TestAssert.True(Window(a,b,c).Complete && !MinimapStrictSampleBuffer.Compare(100,100,111) && MinimapStrictSampleBuffer.Compare(100,104,100) && !MinimapStrictSampleBuffer.Compare(100,106,100),"recovered baseline drift and strict5percent comparison");
        GD.Print("MINIMAP_STRICT_TIMING_CONTROLS_OK cases=9 unit_metadata_only=True normal_performance=False");
    }
}
