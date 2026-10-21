using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Presentation.Fog;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Original saved Animatable sources, natural clocks and actual GPU pixels; not traversal.</summary>
public static class FogMovingGeometryReadabilityRuntimeTests
{
    private static readonly Color Edge=new("cbbd93");
    public static async Task Run(SceneTree tree)
    {
        var api=new MovingApi();
        await Warnings(tree,api);
        foreach(string name in new[] { "CounterweightLift","PendulumHazard","GearPiston" })
        {
            using var f=await Fixture.Create(tree,api,name);
            if(f.Body is CanyonGearPiston piston)
            {
                TestAssert.True(piston.Collision.Disabled && api.Count(f.Overlay)==0,
                    "retracted piston is not presented as a solid landing/obstacle");
                await f.Until(()=>piston.Frame.Telegraphing,100);
                TestAssert.Equal(0,api.Count(f.Overlay),"original warning movement is still nonsolid");
                await f.Until(()=>piston.Frame.HazardActive,45);
                TestAssert.True(!piston.Collision.Disabled && f.Frame.Shapes.Count==1,
                    "only naturally committed active piston native shape authorizes a boundary");
            }
            using(Image image=await f.Pixels())
            {
                TestAssert.Equal(1,api.Count(f.Overlay),"duplicate original source references paint one native body");
                Pixel(image,f.PixelAtEdge(),true,"current native face survives real fog");
                Pixel(image,f.PixelAtInterior(),false,"boundary cue does not expose the whole solid face");
            }
            f.Protected.Layer=0;
            using(Image image=await f.Pixels()) Pixel(image,f.PixelAtEdge(),false,"lowering the same edge below fog attenuates it");
            f.Protected.Layer=2;
            if(f.Body is CanyonGearPiston)
            {
                await f.Until(()=>!((CanyonGearPiston)f.Body).Frame.HazardActive,65);
                TestAssert.Equal(0,api.Count(f.Overlay),"actual piston collision retraction revokes all solid boundary paint");
                continue;
            }
            Vector2 oldPoint=f.EdgeWorld; Transform2D old=f.Collision.GlobalTransform;
            if(f.Body is CanyonCounterweightLift lift)
                TestAssert.True(lift.TryReceiveThread(default),"original lift receiver activates the actual carriage");
            await f.Frames(30);
            TestAssert.True(old.Origin.DistanceTo(f.Collision.GlobalTransform.Origin)>20 ||
                old.X.DistanceTo(f.Collision.GlobalTransform.X)>.2f,"original callback actually translates/rotates native geometry");
            using(Image image=await f.Pixels())
            {
                Pixel(image,f.PixelAtEdge(),true,"current moving/rotating face follows native pose, not rest AABB");
                Pixel(image,f.Pixel(oldPoint),false,"previous world-space edge does not remain painted");
            }
            // Stop processing is NOT disappearance: KeepActive geometry remains solid.
            f.Body.ProcessMode=Node.ProcessModeEnum.Disabled; f.Body.DisableMode=CollisionObject2D.DisableModeEnum.KeepActive;
            using(Image image=await f.Pixels()) Pixel(image,f.PixelAtEdge(),true,"stopped KeepActive native body retains a neutral solid boundary");
            f.Body.DisableMode=CollisionObject2D.DisableModeEnum.Remove;
            using(Image image=await f.Pixels()) TestAssert.Equal(0,api.Count(f.Overlay),"Disabled+Remove native body leaves no ghost solid edge");
            f.Body.ProcessMode=Node.ProcessModeEnum.Inherit;
            await f.Frames(2);
            tree.Paused=true; Transform2D paused=f.Collision.GlobalTransform;
            using(Image image=await f.Pixels()) TestAssert.Equal(0,api.Count(f.Overlay),"pause clears protected commands");
            TestAssert.Equal(paused,f.Collision.GlobalTransform,"paused source keeps its original native pose"); tree.Paused=false;
            f.Body.SetPhysicsProcess(false); await f.Frames(2);
            using(Image image=await f.Pixels()) Pixel(image,f.PixelAtEdge(),true,"resume revalidates stopped but solid source");
            StaticBody2D wall=f.Wall(new Rect2(f.Actor.X+200,f.Actor.Y-200,16,500));
            using(Image image=await f.Pixels())
            { TestAssert.Equal(0,api.Count(f.Overlay),"native wall occludes every visible face"); Pixel(image,f.PixelAtEdge(),false,"wall-hidden body is not revealed above fog"); }
            wall.GetParent().RemoveChild(wall); wall.Free();
            f.Sector.SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true); f.SubmitNow();
            TestAssert.Equal(0,api.Count(f.Overlay),"staged ancestor immediately revokes native body paint");
            f.Sector.RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);
            f.Body.GetNode<CanvasItem>(name=="CounterweightLift" ? "Carriage" : "VisiblePendulum").Visible=false; f.SubmitNow();
            TestAssert.Equal(0,api.Count(f.Overlay),"hidden original source skin cannot expose a body");
            f.Body.GetNode<CanvasItem>(name=="CounterweightLift" ? "Carriage" : "VisiblePendulum").Visible=true;
            uint owner=(uint)f.Body.GetShapeOwners()[0]; Transform2D native=f.Body.ShapeOwnerGetTransform(owner);
            f.Body.ShapeOwnerSetTransform(owner,new Transform2D(0,new Vector2(24,0))*native); f.SubmitNow();
            TestAssert.Equal(0,api.Count(f.Overlay),"native/node drift rejects stale original visual alignment");
            f.Body.ShapeOwnerSetTransform(owner,native);
            f.Actor+=new Vector2(-1000,0); f.SubmitNow();
            TestAssert.Equal(0,api.Count(f.Overlay),"far source does not reveal the world beyond the bounded local cue");
            f.Actor-=new Vector2(-1000,0);
            Node parent=f.Body.GetParent(); parent.RemoveChild(f.Body); f.SubmitNow();
            TestAssert.Equal(0,api.Count(f.Overlay),"detached source cannot retain a native boundary"); parent.AddChild(f.Body); await f.Frames(2);
            using(Image image=await f.Pixels()) TestAssert.Equal(1,api.Count(f.Overlay),"same source reentry rebuilds current native paint");
            f.Sector.QueueFree(); f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"queued ancestor revokes paint before flush");
        }
        GD.Print("FOG_READABILITY_SAVED_MOVING_GEOMETRY_OK pixels=true fixture=true traversal=false formal_visible=false");
    }
    private static async Task Warnings(SceneTree tree,MovingApi api)
    {
        foreach(string name in new[] { "GearPiston","PendulumHazard" })
        {
            using var f=await Fixture.Create(tree,api,name);
            TestAssert.True(f.Warning!=null,"original saved mechanism owns a separate warning polygon");
            f.FocusWarning();
            await f.Until(()=>f.WarningActive,180);
            Color protectedPixel=default;
            using(Image image=await f.Pixels())
            {
                GD.Print($"FOG_MOVING_WARNING_DIAGNOSTIC source={name} active={f.WarningActive} visible={f.Warning.IsVisibleInTree()} " +
                    $"warning_count={((FogMovingGeometryReadabilityOverlay)f.Overlay).PresentedWarningCount} patches={((FogMovingGeometryReadabilityOverlay)f.Overlay).PresentedWarningSegmentCount} " +
                    $"center_los={f.Frame.CanSee(f.Actor,f.Warning.GlobalPosition)} actor={f.Actor} source_position={f.Warning.GlobalPosition}");
                TestAssert.True(f.WarningActive && f.Frame.CanSee(f.Actor,f.Warning.GlobalPosition),"positive warning pixel has a live natural warning and an unobstructed native line of sight");
                protectedPixel=WarningPixel(image,f.Pixel(f.Warning.GlobalPosition),f.Warning.Color,true,
                    "original natural advance warning remains readable above fog before active obstruction");
            }
            if(f.Body is CanyonGearPiston)
                TestAssert.Equal(0,api.Count(f.Overlay),"protected warning is not a solid piston boundary");
            f.Protected.Layer=0;
            await f.NextWarning();
            using(Image image=await f.Pixels())
                WarningPixel(image,f.Pixel(f.Warning.GlobalPosition),f.Warning.Color,false,"same original warning below fog is attenuated",protectedPixel);
            f.Protected.Layer=2;
            StaticBody2D wall=f.Wall(new Rect2(f.Actor.X-180,(f.Actor.Y+f.Warning.GlobalPosition.Y)*.5f-8,360,16));
            await f.NextWarning();
            using(Image image=await f.Pixels())
                WarningPixel(image,f.Pixel(f.Warning.GlobalPosition),f.Warning.Color,false,"opaque native wall forbids warning reveal",protectedPixel);
            wall.GetParent().RemoveChild(wall); wall.Free();
            await f.NextWarning();
            using(Image image=await f.Pixels())
                WarningPixel(image,f.Pixel(f.Warning.GlobalPosition),f.Warning.Color,true,"same original warning returns after real occluder removal");
            var overlay=(FogMovingGeometryReadabilityOverlay)f.Overlay;
            f.SubmitNow();
            TestAssert.Equal(1,overlay.PresentedWarningCount,"duplicate original references count only one live warning");
            Vector2 original=f.Warning.GlobalPosition;
            f.Warning.Visible=false; f.SubmitNow();
            TestAssert.Equal(0,overlay.PresentedWarningCount,"hidden original warning revokes protected paint immediately");
            f.Warning.Visible=true;
            f.Body.SetPhysicsProcess(false); f.SubmitNow();
            TestAssert.Equal(0,overlay.PresentedWarningCount,"stopped mechanism cannot retain a stale advance warning");
            f.Body.SetPhysicsProcess(true);
            tree.Paused=true; f.SubmitNow();
            TestAssert.True(overlay.PresentedWarningCount==0 && overlay.LastOcclusionFrame==null,"pause clears warning and snapshot provenance");
            tree.Paused=false;
            f.Sector.SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true); f.SubmitNow();
            TestAssert.Equal(0,overlay.PresentedWarningCount,"staged source cannot expose its warning");
            f.Sector.RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);
            StringName originalName=f.Warning.Name;
            f.Warning.Name="NotTheOriginalTelegraph"; f.SubmitNow();
            TestAssert.Equal(0,overlay.PresentedWarningCount,"noncanonical warning cannot inherit protection");
            f.Warning.Name=originalName;
            f.Actor+=new Vector2(0,1000); f.SubmitNow();
            TestAssert.Equal(0,overlay.PresentedWarningCount,"distant warning never exposes a remote room");
            f.Actor-=new Vector2(0,1000); f.SubmitNow();
            TestAssert.Equal(1,overlay.PresentedWarningCount,"same original current warning returns after eligibility restoration");
            await f.Until(()=>!f.WarningActive,180);
            TestAssert.Equal(0,overlay.PresentedWarningCount,"natural end of warning clears all its protected patches");
            TestAssert.Equal(original,f.Warning.GlobalPosition,"fixed sibling warning never follows the moving or rotating body");
            GD.Print($"FOG_MOVING_ADVANCE_WARNING_PIXELS_OK source={name} original={f.Warning.GetInstanceId()} natural=true");
        }
    }
    private static Color WarningPixel(Image image,Vector2I pixel,Color source,bool expected,string message,Color protectedReference=default)
    {
        TestAssert.True(pixel.X>=0 && pixel.X<image.GetWidth() && pixel.Y>=0 && pixel.Y<image.GetHeight(),"warning probe is in actual viewport");
        Color c=image.GetPixelv(pixel);
        // A protected original alpha blend cannot be darker than color*alpha on black.
        // The saved opaque piston therefore also requires its actual original RGB.
        bool protectedColor=c.R>=source.R*source.A-.009f && c.G>=source.G*source.A-.009f && c.B>=source.B*source.A-.009f;
        TestAssert.True(expected ? protectedColor : c.R<protectedReference.R-.1f && c.G<protectedReference.G-.06f,
            $"{message}; pixel={pixel} actual={c} source={source} protectedReference={protectedReference}");
        return c;
    }
    private static void Pixel(Image image,Vector2I pixel,bool expected,string message)
    {
        TestAssert.True(pixel.X>=0 && pixel.X<image.GetWidth() && pixel.Y>=0 && pixel.Y<image.GetHeight(),$"probe remains in actual viewport: {pixel}");
        Color c=image.GetPixelv(pixel);
        bool same=Mathf.Abs(c.R-Edge.R)<.009f && Mathf.Abs(c.G-Edge.G)<.009f && Mathf.Abs(c.B-Edge.B)<.009f;
        TestAssert.True(same==expected,$"{message}; pixel={pixel} actual={c}");
    }
    private sealed class MovingApi
    {
        private readonly Type _type=typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogMovingGeometryReadabilityOverlay");
        private readonly MethodInfo _present,_clear; private readonly PropertyInfo _count;
        public MovingApi()
        {
            TestAssert.True(_type!=null && typeof(Node2D).IsAssignableFrom(_type),"original lift, piston and rotating pendulum need current native neutral boundaries above fog");
            _present=_type.GetMethod("PresentBodies",[typeof(IReadOnlyList<AnimatableBody2D>),typeof(Vector2),typeof(HeadlampOcclusionFrame)]);
            _clear=_type.GetMethod("Clear",Type.EmptyTypes); _count=_type.GetProperty("PresentedBodyCount");
            TestAssert.True(_present?.ReturnType==typeof(void) && _clear!=null && _count?.PropertyType==typeof(int),"moving geometry consumer requires the exact current sources and shared native frame");
        }
        public Node2D Create()=>(Node2D)Activator.CreateInstance(_type);
        public int Count(Node2D overlay)=>(int)_count.GetValue(overlay);
        public void Clear(Node2D overlay)=>_clear.Invoke(overlay,null);
        public void Present(Node2D overlay,IReadOnlyList<AnimatableBody2D> sources,Vector2 actor,HeadlampOcclusionFrame frame)
        { try { _present.Invoke(overlay,[sources,actor,frame]); } catch(TargetInvocationException e) when(e.InnerException!=null) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; } }
    }
    private sealed class Fixture:IDisposable
    {
        private readonly SceneTree _tree; private readonly MovingApi _api; private readonly SubViewport _viewport;
        private readonly Node2D _world; private readonly HeadlampOcclusionSampler _sampler; private readonly Camera2D _camera;
        private Exception _failure; private bool _disposed;
        public PrologueSector Sector { get; }
        public AnimatableBody2D Body { get; }
        public Polygon2D Warning { get; }
        public bool WarningActive=>Body switch { CanyonGearPiston p=>p.Frame.Telegraphing,CanyonPendulumHazard p=>p.Frame.Telegraphing,_=>false };
        public CollisionShape2D Collision { get; }
        public Node2D Overlay { get; }
        public CanvasLayer Protected { get; }
        public HeadlampOcclusionFrame Frame { get; private set; }
        public Vector2 Actor { get; set; }
        private readonly PrologueFogCanvas _fog;
        private Fixture(SceneTree tree,MovingApi api,string name)
        {
            _tree=tree; _api=api; bool basin=name=="GearPiston";
            _viewport=new SubViewport { Size=new Vector2I(512,384),World2D=new World2D(),RenderTargetUpdateMode=SubViewport.UpdateMode.Always,ProcessMode=Node.ProcessModeEnum.Always };
            var background=new CanvasLayer { Layer=-10 }; background.AddChild(new ColorRect { Size=new Vector2(512,384),Color=Colors.White }); _viewport.AddChild(background);
            _world=new Node2D { ProcessMode=Node.ProcessModeEnum.Pausable }; _viewport.AddChild(_world);
            var packed=GD.Load<PackedScene>(basin ? "res://scenes/prologue/sectors/CanyonBasin.tscn" : "res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
            Sector=new PrologueSector { SectorId=basin ? PrologueSectorId.CanyonBasin : PrologueSectorId.CanyonAscent,
                Position=basin ? new Vector2(1280,3600) : new Vector2(3840,720) };
            var gameplay=new Node2D { Name="Gameplay" }; var mechanisms=new Node2D { Name="Mechanisms" }; gameplay.AddChild(mechanisms); Sector.AddChild(gameplay); _world.AddChild(Sector);
            Body=packed.GetNode<AnimatableBody2D>("Gameplay/Mechanisms/"+name); ClearOwners(Body); Body.GetParent().RemoveChild(Body); mechanisms.AddChild(Body);
            if(name!="CounterweightLift")
            {
                var visuals=new Node2D { Name="VisualRoot" }; var readable=new Node2D { Name="GameplayReadable" };
                var telegraphs=new Node2D { Name="MechanismTelegraphs" }; visuals.AddChild(readable); readable.AddChild(telegraphs); Sector.AddChild(visuals);
                Warning=packed.GetNode<Polygon2D>("VisualRoot/GameplayReadable/MechanismTelegraphs/"+(basin ? "GearPistonTelegraph" : "PendulumTelegraph"));
                ClearOwners(Warning); Warning.GetParent().RemoveChild(Warning); telegraphs.AddChild(Warning);
            }
            packed.Free();
            Collision=Body.GetNode<CollisionShape2D>("Collision");
            Vector2 center=Sector.Position+(basin ? new Vector2(1320,260) : name=="CounterweightLift" ? new Vector2(1000,2820) : new Vector2(1080,1370));
            _camera=new Camera2D { Position=center,PositionSmoothingEnabled=false }; _viewport.AddChild(_camera);
            Actor=center+new Vector2(-485,-70);
            var fogLayer=new CanvasLayer { Layer=1 }; _fog=new PrologueFogCanvas { Size=new Vector2(512,384) }; fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            Protected=new CanvasLayer { Layer=2,FollowViewportEnabled=true,FollowViewportScale=1 }; Overlay=api.Create(); Protected.AddChild(Overlay); _viewport.AddChild(Protected);
            tree.Root.AddChild(_viewport); _camera.MakeCurrent(); _camera.ForceUpdateScroll(); _sampler=new HeadlampOcclusionSampler(_world); RenderingServer.FramePreDraw+=Present;
        }
        private static void ClearOwners(Node node) { node.Owner=null; foreach(Node child in node.GetChildren()) ClearOwners(child); }
        public static async Task<Fixture> Create(SceneTree tree,MovingApi api,string name)
        { var f=new Fixture(tree,api,name); try { await f.Frames(3); return f; } catch { f.Dispose(); throw; } }
        public Vector2 EdgeWorld { get { Rect2 r=((RectangleShape2D)Collision.Shape).GetRect(); return Collision.GlobalTransform*new Vector2(r.Position.X+1,0); } }
        public Vector2I Pixel(Vector2 point) { Vector2 p=_world.GetCanvasTransform()*point; return new Vector2I(Mathf.FloorToInt(p.X),Mathf.FloorToInt(p.Y)); }
        public Vector2I PixelAtEdge()=>Pixel(EdgeWorld);
        public Vector2I PixelAtInterior()=>Pixel(Collision.GlobalTransform*Vector2.Zero);
        public void FocusWarning()
        { _camera.Position=Warning.GlobalPosition; _camera.ForceUpdateScroll(); Actor=Warning.GlobalPosition+new Vector2(0,435); }
        public async Task NextWarning()
        { await Until(()=>!WarningActive,180); await Until(()=>WarningActive,180); }
        public StaticBody2D Wall(Rect2 rect)
        { var wall=new StaticBody2D { Position=rect.GetCenter(),CollisionLayer=CombatCollisionLayers.World }; wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=rect.Size } }); _world.AddChild(wall); return wall; }
        public void SubmitNow() { Frame=_sampler.Capture(); _api.Present(Overlay,new[] { Body,Body },Actor,Frame); }
        private void Present()
        {
            if(_disposed || _failure!=null) return;
            try { SubmitNow(); TestAssert.True(Frame.IsComplete && Frame.PhysicsQueries==0,"moving source uses exact current native snapshot without added physics queries");
                _fog.Present(Actor,Actor,Vector2.Left,(_viewport.GetFinalTransform()*_world.GetCanvasTransform()).AffineInverse(),Frame); }
            catch(Exception error) { _failure??=error; }
        }
        public async Task Frames(int count)
        { for(int i=0;i<count;i++) { await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame); await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame); await _tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw); if(_failure!=null) ExceptionDispatchInfo.Capture(_failure).Throw(); } }
        public async Task Until(Func<bool> condition,int maximum)
        { for(int i=0;i<maximum && !condition();i++) await Frames(1); TestAssert.True(condition(),"original natural mechanism phase reached within its authored cycle"); }
        public async Task<Image> Pixels() { await Frames(1); return _viewport.GetTexture().GetImage(); }
        public void Dispose()
        { if(_disposed) return; _disposed=true; _tree.Paused=false; RenderingServer.FramePreDraw-=Present; _sampler.Dispose();
            if(GodotObject.IsInstanceValid(Body) && Body.GetParent()==null) Body.Free(); _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free(); }
    }
}
