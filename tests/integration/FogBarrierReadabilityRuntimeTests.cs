using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Original saved door bodies, real native state and GPU pixels; controlled fixture, not traversal.</summary>
public static class FogBarrierReadabilityRuntimeTests
{
    private static readonly Color Edge=new("cbbd93");
    public static async Task Run(SceneTree tree)
    {
        var api=new BarrierApi();
        foreach(string path in new[] { "Mechanisms/CourtyardSeal", "Mechanisms/BellChain", "Mechanisms/UpperBasinShortcutWinch",
            "Doors/AdmissionBarrier", "Doors/Entry", "Doors/Exit", "Mechanisms/TemplePressureGate", "Mechanisms/ArchivePressureGate" })
        {
            using var f=await Fixture.Create(tree,api,path);
            using(Image image=await f.Pixels())
            {
                TestAssert.Equal(1,api.Count(f.Overlay),"one actual closed original body survives fog; duplicate references do not double count: "+path);
                Pixel(image,f.Pixel(f.Probe),true,"current real door face survives fog: "+path);
                Pixel(image,f.Pixel(f.Collision.GlobalPosition),false,"neutral boundary does not fill the whole obstacle: "+path);
            }
            f.Protected.Layer=0;
            using(Image image=await f.Pixels()) Pixel(image,f.Pixel(f.Probe),false,"same mark below fog is attenuated: "+path);
            f.Protected.Layer=2;
            if(path=="Doors/AdmissionBarrier")
            {
                var top=f.Body.GetNode<CollisionShape2D>("TopCollision");
                f.Probe=f.Collision.GlobalTransform*new Vector2(17,-50);
                f.Actor=f.Collision.GlobalTransform*new Vector2(250,-350); f.Focus();
                using(Image image=await f.Pixels()) Pixel(image,f.Pixel(f.Probe),false,"same body's native roof occludes the left column's inward right face");
                top.Disabled=true;
                using(Image image=await f.Pixels()) Pixel(image,f.Pixel(f.Probe),true,"only disabling that actual native roof permits the same column face");
                top.Disabled=false;
                // Test the two long/remote original pieces independently, not only the first owner.
                foreach(string leaf in new[] { "TopCollision", "RightCollision" })
                {
                    var shape=f.Body.GetNode<CollisionShape2D>(leaf);
                    Rect2 r=((RectangleShape2D)shape.Shape).GetRect();
                    f.Probe=shape.GlobalTransform*(leaf=="TopCollision" ? new Vector2(0,r.Position.Y+1) : new Vector2(r.End.X-1,0));
                    f.Actor=shape.GlobalTransform*(leaf=="TopCollision" ? new Vector2(0,r.Position.Y-485) : new Vector2(r.End.X+485,0));
                    f.Focus();
                    using(Image image=await f.Pixels()) Pixel(image,f.Pixel(f.Probe),true,"original multi-owner current face is independently readable: "+leaf);
                }
            }
            if(f.Source is BellChainMechanism bell)
            {
                TestAssert.True(bell.TryReceiveThread(default),"original bell receiver accepts thread");
                f.SubmitNow();
                TestAssert.Equal(1,api.Count(f.Overlay),"logical IsOpen cannot erase still-enabled deferred native collision");
                await f.Frames(2);
                TestAssert.Equal(0,api.Count(f.Overlay),"actual deferred bell collision removal erases all protected boundary paint");
                bell.ApplyProgress(new PrologueProgress()); await f.Frames(2);
                TestAssert.Equal(1,api.Count(f.Overlay),"original reset restores actual closed native boundary");
            }
            else if(f.Source is CanyonSealGate seal)
            {
                var progress=new PrologueProgress(); progress.ActivateMechanism(seal.RequiredMechanismId); progress.ActivateMechanism(seal.RequiredEncounterId);
                seal.ApplyProgress(progress); f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"actual seal prerequisites remove the solid face immediately");
                seal.ApplyProgress(new PrologueProgress()); await f.Frames(1);
            }
            else if(f.Source is CanyonShortcutWinch shortcut)
            {
                TestAssert.True(shortcut.TryReceiveThread(default),"original shortcut thread opens authored barrier"); f.SubmitNow();
                TestAssert.Equal(0,api.Count(f.Overlay),"opened long shortcut cannot leave a ghost wall");
                shortcut.ApplyProgress(new PrologueProgress()); await f.Frames(1);
            }
            // Original seal/pressure callbacks own collision and would legitimately
            // restore it on the next tick. Stop that owner for this disabled-native negative control.
            f.Source.SetPhysicsProcess(false);
            foreach(Node child in f.Body.GetChildren()) if(child is CollisionShape2D shape) shape.SetDeferred(CollisionShape2D.PropertyName.Disabled,true);
            await f.Frames(2); TestAssert.Equal(0,api.Count(f.Overlay),"disabled actual native shapes leave no solid cue: "+path);
        }
        using(var f=await Fixture.Create(tree,api,"Mechanisms/CourtyardSeal"))
        {
            // These checks catch stale source authority, not ordinary geometric occlusion alone.
            StaticBody2D wall=f.Wall(new Rect2(f.Actor.X+180,f.Actor.Y-280,16,560));
            using(Image image=await f.Pixels()) { TestAssert.Equal(0,api.Count(f.Overlay),"a real wall hides the entire door face"); Pixel(image,f.Pixel(f.Probe),false,"no hidden door pixels above fog"); }
            wall.GetParent().RemoveChild(wall); wall.Free();
            f.Body.ProcessMode=Node.ProcessModeEnum.Disabled; f.Body.DisableMode=CollisionObject2D.DisableModeEnum.KeepActive;
            using(Image image=await f.Pixels()) Pixel(image,f.Pixel(f.Probe),true,"stopped but KeepActive physical barrier stays solid and readable");
            f.Body.DisableMode=CollisionObject2D.DisableModeEnum.Remove;
            using(Image image=await f.Pixels()) TestAssert.Equal(0,api.Count(f.Overlay),"Disabled+Remove physical barrier has no ghost boundary");
            f.Body.ProcessMode=Node.ProcessModeEnum.Inherit; await f.Frames(2);
            tree.Paused=true; using(Image image=await f.Pixels()) TestAssert.Equal(0,api.Count(f.Overlay),"pause clears protected door paint"); tree.Paused=false;
            using(Image image=await f.Pixels()) Pixel(image,f.Pixel(f.Probe),true,"resume revalidates real door");
            f.Body.Visible=false; f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"hidden original body cannot expose a room"); f.Body.Visible=true;
            f.Sector.SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true); f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"staged ancestor revokes authority"); f.Sector.RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);
            uint owner=(uint)f.Body.GetShapeOwners()[0]; Transform2D original=f.Body.ShapeOwnerGetTransform(owner);
            f.Body.ShapeOwnerSetTransform(owner,new Transform2D(0,new Vector2(24,0))*original); f.SubmitNow();
            TestAssert.Equal(0,api.Count(f.Overlay),"native/node drift cannot authorize a stale collision child"); f.Body.ShapeOwnerSetTransform(owner,original);
            f.Actor-=new Vector2(1000,0); f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"bounded cue cannot reveal a far barrier"); f.Actor+=new Vector2(1000,0);
            Node parent=f.Body.GetParent(); parent.RemoveChild(f.Body); f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"detached door immediately clears paint");
            parent.AddChild(f.Body); await f.Frames(2); using(Image image=await f.Pixels()) TestAssert.Equal(1,api.Count(f.Overlay),"same original door reentry revalidates native identity");
            f.Sector.QueueFree(); f.SubmitNow(); TestAssert.Equal(0,api.Count(f.Overlay),"queued sector clears protected paint before deletion flush");
        }
        GD.Print("FOG_READABILITY_SAVED_BARRIERS_OK pixels=true fixture=true traversal=false formal_visible=false");
    }
    private static void Pixel(Image image,Vector2I pixel,bool expected,string message)
    {
        TestAssert.True(pixel.X>=0 && pixel.X<image.GetWidth() && pixel.Y>=0 && pixel.Y<image.GetHeight(),"probe inside actual viewport: "+pixel);
        Color c=image.GetPixelv(pixel); bool same=Mathf.Abs(c.R-Edge.R)<.009f && Mathf.Abs(c.G-Edge.G)<.009f && Mathf.Abs(c.B-Edge.B)<.009f;
        TestAssert.True(same==expected,$"{message}; pixel={pixel} actual={c}");
    }
    private sealed class BarrierApi
    {
        private readonly Type _type=typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogBarrierReadabilityOverlay");
        private readonly MethodInfo _present; private readonly PropertyInfo _count;
        public BarrierApi()
        {
            TestAssert.True(_type!=null && typeof(Node2D).IsAssignableFrom(_type),"closed original doors and quota barrier need current per-shape neutral faces above fog");
            _present=_type.GetMethod("PresentBarriers",[typeof(IReadOnlyList<StaticBody2D>),typeof(Vector2),typeof(HeadlampOcclusionFrame)]);
            _count=_type.GetProperty("PresentedBodyCount");
            TestAssert.True(_present?.ReturnType==typeof(void) && _count?.PropertyType==typeof(int),"barrier paint consumes real original bodies and the shared fresh native frame");
        }
        public Node2D Create()=>(Node2D)Activator.CreateInstance(_type);
        public int Count(Node2D overlay)=>(int)_count.GetValue(overlay);
        public void Present(Node2D overlay,IReadOnlyList<StaticBody2D> bodies,Vector2 actor,HeadlampOcclusionFrame frame)
        { try { _present.Invoke(overlay,[bodies,actor,frame]); } catch(TargetInvocationException e) when(e.InnerException!=null) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; } }
    }
    private sealed class Fixture:IDisposable
    {
        private readonly SceneTree _tree; private readonly BarrierApi _api; private readonly SubViewport _viewport;
        private readonly Node2D _world; private readonly Camera2D _camera; private readonly PrologueFogCanvas _fog;
        private readonly HeadlampOcclusionSampler _sampler; private Exception _failure; private bool _disposed;
        public PrologueSector Sector { get; }
        public Node2D Source { get; }
        public StaticBody2D Body { get; }
        public CollisionShape2D Collision { get; }
        public CanvasLayer Protected { get; }
        public Node2D Overlay { get; }
        public Vector2 Actor { get; set; }
        public Vector2 Probe { get; set; }
        private Fixture(SceneTree tree,BarrierApi api,string path)
        {
            _tree=tree; _api=api; bool ascent=path.Contains("Shortcut"),archive=path.Contains("Archive");
            string sectorName=ascent ? "CanyonAscent" : archive ? "CanyonDescent" : "LostTemple";
            _viewport=new SubViewport { Size=new Vector2I(512,384),World2D=new World2D(),RenderTargetUpdateMode=SubViewport.UpdateMode.Always,ProcessMode=Node.ProcessModeEnum.Always };
            var background=new CanvasLayer { Layer=-10 }; background.AddChild(new ColorRect { Size=new Vector2(512,384),Color=Colors.White }); _viewport.AddChild(background);
            _world=new Node2D { ProcessMode=Node.ProcessModeEnum.Pausable }; _viewport.AddChild(_world);
            var packed=GD.Load<PackedScene>("res://scenes/prologue/sectors/"+sectorName+".tscn").Instantiate<PrologueSector>();
            Sector=new PrologueSector { SectorId=ascent ? PrologueSectorId.CanyonAscent : archive ? PrologueSectorId.CanyonDescent : PrologueSectorId.LostTemple,
                Position=ascent ? new Vector2(3840,720) : archive ? new Vector2(1280,720) : new Vector2(5120,720) };
            var gameplay=new Node2D { Name="Gameplay" }; var container=new Node2D { Name=path.Split('/')[0] }; gameplay.AddChild(container); Sector.AddChild(gameplay); _world.AddChild(Sector);
            Source=packed.GetNode<Node2D>("Gameplay/"+path); ClearOwners(Source); Source.GetParent().RemoveChild(Source); container.AddChild(Source); packed.Free();
            Body=Source is StaticBody2D direct ? direct : Source.GetNode<StaticBody2D>(path.Contains("Shortcut") ? "Barrier" : "Door");
            Collision=Body.GetNode<CollisionShape2D>("Collision");
            _camera=new Camera2D { PositionSmoothingEnabled=false }; _viewport.AddChild(_camera);
            var fogLayer=new CanvasLayer { Layer=1 }; _fog=new PrologueFogCanvas { Size=new Vector2(512,384) }; fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            Protected=new CanvasLayer { Layer=2,FollowViewportEnabled=true,FollowViewportScale=1 }; Overlay=api.Create(); Protected.AddChild(Overlay); _viewport.AddChild(Protected);
            tree.Root.AddChild(_viewport); _camera.MakeCurrent();
            Rect2 r=((RectangleShape2D)Collision.Shape).GetRect(); Probe=Collision.GlobalTransform*new Vector2(r.Position.X+1,0);
            Actor=Collision.GlobalTransform*new Vector2(r.Position.X-485,0); Focus();
            _sampler=new HeadlampOcclusionSampler(_world); RenderingServer.FramePreDraw+=Present;
        }
        private static void ClearOwners(Node node) { node.Owner=null; foreach(Node child in node.GetChildren()) ClearOwners(child); }
        public static async Task<Fixture> Create(SceneTree tree,BarrierApi api,string path)
        { var f=new Fixture(tree,api,path); try { await f.Frames(3); return f; } catch { f.Dispose(); throw; } }
        public Vector2I Pixel(Vector2 point) { Vector2 p=_world.GetCanvasTransform()*point; return new Vector2I(Mathf.FloorToInt(p.X),Mathf.FloorToInt(p.Y)); }
        public void Focus() { _camera.Position=Probe+new Vector2(70,0); _camera.ForceUpdateScroll(); }
        public StaticBody2D Wall(Rect2 rect)
        { var wall=new StaticBody2D { Position=rect.GetCenter(),CollisionLayer=CombatCollisionLayers.World }; wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=rect.Size } }); _world.AddChild(wall); return wall; }
        public void SubmitNow()=>_api.Present(Overlay,new[] { Body,Body },Actor,_sampler.Capture());
        private void Present()
        {
            if(_disposed || _failure!=null) return;
            try { var frame=_sampler.Capture(); _api.Present(Overlay,new[] { Body,Body },Actor,frame);
                TestAssert.True(frame.IsComplete && frame.PhysicsQueries==0,"original barriers use complete current native geometry without added physics queries");
                _fog.Present(Actor,Actor,Vector2.Left,(_viewport.GetFinalTransform()*_world.GetCanvasTransform()).AffineInverse(),frame); }
            catch(Exception error) { _failure??=error; }
        }
        public async Task Frames(int count)
        { for(int i=0;i<count;i++) { await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame); await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame); await _tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw); if(_failure!=null) ExceptionDispatchInfo.Capture(_failure).Throw(); } }
        public async Task<Image> Pixels() { await Frames(1); return _viewport.GetTexture().GetImage(); }
        public void Dispose()
        { if(_disposed) return; _disposed=true; _tree.Paused=false; RenderingServer.FramePreDraw-=Present; _sampler.Dispose();
            if(GodotObject.IsInstanceValid(Body) && Body.GetParent()==null) Body.Free(); _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free(); }
    }
}
