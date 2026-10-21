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

/// <summary>Original saved doors and real plate/closure clocks; controlled GPU fixture, not traversal.</summary>
public static class FogPressureGateReadabilityRuntimeTests
{
    private static readonly Color Warning=new(1f,.43f,.15f);

    // Catches omitted closing paint, a copied/stale countdown, treating the open
    // warning door as an active occluder, and exposing that paint through a wall.
    public static async Task Run(SceneTree tree)
    {
        var api=new GateApi();
        foreach(bool temple in new[] { false,true })
        {
            using var f=await GateFixture.Create(tree,api,temple);
            await f.Warning();
            TestAssert.Equal(temple ? new Rect2(5930,760,30,200) : new Rect2(1720,1424,40,216),
                f.Visual.ClosedSlabWorldRect,"original saved door collision dimensions/world pose remain unchanged");
            TestAssert.True(f.Gate.IsOpen && f.Gate.Frame.Traversable && !f.Gate.Frame.HazardActive &&
                f.Gate.DoorCollision.Disabled && !f.DoorBlocks(),"warning is still physically traversable, not a hitbox");
            f.Submit=false; f.Fog.Visible=false;
            using(Image original=await f.Pixels())
            {
                Pixel(original,256,192,Warning,"original world paint has the same literal warning centerline");
                Pixel(original,f.TickX,f.FirstTickY,Warning,"original world paint has the first countdown tick");
            }
            f.Fog.Visible=true; f.Submit=true;
            using(Image image=await f.Pixels())
            {
                Pixel(image,256,192,Warning,"actual closing centerline survives the real fog");
                Pixel(image,f.TickX,f.FirstTickY,Warning,"actual countdown tick survives the real fog");
                TestAssert.Equal(1,api.Count(f.Overlay),"duplicate references paint only one original gate");
            }
            f.Protected.Layer=0;
            using(Image image=await f.Pixels())
                TestAssert.True(image.GetPixel(256,192).R<.9f,"same warning under real fog is attenuated at485px");
            f.Protected.Layer=2;

            if(!temple)
            {
                uint owner=(uint)f.Door.GetShapeOwners()[0];
                f.Door.ShapeOwnerSetDisabled(owner,false); api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"native enabled owner cannot masquerade as the open warning door");
                f.Door.ShapeOwnerSetDisabled(owner,true);
                Transform2D native=f.Door.ShapeOwnerGetTransform(owner);
                f.Door.ShapeOwnerSetTransform(owner,new Transform2D(0,new Vector2(24,0))*native);
                api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"native/node transform mismatch rejects stale world paint");
                f.Door.ShapeOwnerSetTransform(owner,native);
                f.Visual.Visible=false; api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"hidden original source cannot expose warning paint"); f.Visual.Visible=true;
                f.Gate.GetParent().SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true);
                api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"staged ancestor withdraws warning before the next raster");
                f.Gate.GetParent().RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);
                f.Gate.SetPhysicsProcess(false); api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"stopped closure clock does not leave an imminent closing cue");
                f.Gate.SetPhysicsProcess(true);
                StaticBody2D wall=f.Wall(new Rect2(f.Center.X-200,f.Center.Y-120,16,240));
                using(Image image=await f.Pixels())
                { TestAssert.Equal(0,api.Count(f.Overlay),"wall-hidden original warning stays hidden"); TestAssert.True(image.GetPixel(256,192).R<.9f,"wall-hidden centerline is not promoted above fog"); }
                wall.GetParent().RemoveChild(wall); wall.Free();
                StaticBody2D needle=f.Wall(new Rect2(f.Actor.X+100,f.Actor.Y+.1f,.2f,.2f));
                using(Image image=await f.Pixels())
                {
                    TestAssert.True(image.GetPixel(256,192).R<.9f,"full width stroke visibility catches a needle between endpoint rays");
                    Pixel(image,256,180,Warning,"genuinely visible neighboring stroke is not blanket hidden");
                }
                needle.GetParent().RemoveChild(needle); needle.Free();
                tree.Paused=true; float phase=f.Gate.Frame.Phase01;
                using(Image image=await f.Pixels()) TestAssert.Equal(0,api.Count(f.Overlay),"pause clears closing warning commands");
                TestAssert.Equal(phase,f.Gate.Frame.Phase01,"pause leaves the actual gate countdown unchanged"); tree.Paused=false;
                using(Image image=await f.Pixels()) Pixel(image,256,192,Warning,"resume revalidates the current real warning");
                Node parent=f.Visual.GetParent(); parent.RemoveChild(f.Visual);
                api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"detached live original paint cannot retain a warning"); parent.AddChild(f.Visual);
                using(Image image=await f.Pixels()) Pixel(image,256,192,Warning,"same original paint can reenter without stale commands");
            }

            // Reenter the real plate, then leave again: this is a new natural
            // countdown, not an injected Frame or duplicated phase model.
            await f.Warning();
            await f.Frames(20);
            TestAssert.True(f.Gate.IsDangerTelegraphActive && f.Gate.Frame.Phase01>.5f,
                "real closing clock has advanced into its late warning");
            using(Image image=await f.Pixels())
            {
                Pixel(image,f.TickX,f.FirstTickY,Warning,"first remaining countdown tick stays orange");
                TestAssert.True(image.GetPixel(f.TickX,f.FirstTickY+100).G<.33f,
                    "expired fifth tick uses the original dark paint rather than a copied full countdown");
            }
            f.Player.GlobalPosition=f.PlatePose; await f.Frames(3);
            TestAssert.True(!f.Gate.IsDangerTelegraphActive && f.Gate.IsOpen && !f.DoorBlocks() && api.Count(f.Overlay)==0,
                "real reentry cancels closing paint without a blocking frame");
            f.Player.GlobalPosition=f.OutsidePose; await f.Frames(130);
            TestAssert.True(!f.Gate.IsOpen && f.DoorBlocks() && api.Count(f.Overlay)==0,
                "completed actual native closure does not retain a stale warning");
            if(!temple)
            {
                await f.Warning(); f.Gate.ResetForCheckpoint(); api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"checkpoint reset revokes warning synchronously, without guessing deferred collision");
                await f.Frames(2); TestAssert.True(!f.Gate.IsOpen && f.DoorBlocks(),"unoccupied reset still commits actual safe closure");
                await f.Warning(); f.Gate.GetParent().QueueFree(); api.Present(f.Overlay,f.Sources,f.Actor,f.Frame);
                TestAssert.Equal(0,api.Count(f.Overlay),"queued source ancestor revokes warning before deletion flush");
            }
        }
        GD.Print("FOG_READABILITY_SAVED_PRESSURE_GATE_OK pixels=true fixture=true traversal=false formal_visible=false");
    }

    private static void Pixel(Image image,int x,int y,Color expected,string message)
    {
        Color actual=image.GetPixel(x,y);
        TestAssert.True(Mathf.Abs(actual.R-expected.R)<=.008f && Mathf.Abs(actual.G-expected.G)<=.008f &&
            Mathf.Abs(actual.B-expected.B)<=.008f && Mathf.Abs(actual.A-1)<=.008f,$"{message}; pixel={x},{y} actual={actual}");
    }
    private sealed class GateApi
    {
        private readonly Type _type=typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogPressureGateReadabilityOverlay");
        private readonly MethodInfo _present,_clear; private readonly PropertyInfo _count;
        public GateApi()
        {
            TestAssert.True(_type!=null && typeof(Node2D).IsAssignableFrom(_type),"real closing pressure gates need shared original countdown paint above fog");
            _present=_type.GetMethod("PresentGates",[typeof(IReadOnlyList<CanyonPressureGateVisual>),typeof(Vector2),typeof(HeadlampOcclusionFrame)]);
            _clear=_type.GetMethod("Clear",Type.EmptyTypes); _count=_type.GetProperty("PresentedGateCount");
            TestAssert.True(_present?.ReturnType==typeof(void) && _clear?.ReturnType==typeof(void) && _count?.PropertyType==typeof(int),
                "closing paint consumer reads original visual instances and one fresh immutable frame");
        }
        public Node2D Create()=>(Node2D)Activator.CreateInstance(_type);
        public int Count(Node2D overlay)=>(int)_count.GetValue(overlay);
        public void Clear(Node2D overlay)=>_clear.Invoke(overlay,null);
        public void Present(Node2D overlay,IReadOnlyList<CanyonPressureGateVisual> sources,Vector2 actor,HeadlampOcclusionFrame frame)
        { try { _present.Invoke(overlay,[sources,actor,frame]); } catch(TargetInvocationException error) when(error.InnerException!=null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; } }
    }
    private sealed class GateFixture:IDisposable
    {
        private readonly SceneTree _tree; private readonly GateApi _api; private readonly SubViewport _viewport;
        private readonly Node2D _world; private readonly HeadlampOcclusionSampler _sampler;
        private Exception _failure; private bool _disposed;
        public CanyonPressurePlateGate Gate { get; }
        public CanyonPressureGateVisual Visual { get; }
        public StaticBody2D Door { get; }
        public CharacterBody2D Player { get; }
        public IReadOnlyList<CanyonPressureGateVisual> Sources { get; }
        public CanvasLayer Protected { get; }
        public Node2D Overlay { get; }
        public PrologueFogCanvas Fog { get; }
        public HeadlampOcclusionFrame Frame { get; private set; }
        public Vector2 Center { get; }
        public Vector2 Actor=>Center-Vector2.Right*485;
        public Vector2 PlatePose { get; }
        public Vector2 OutsidePose=>PlatePose-Vector2.Right*150;
        public int TickX { get; }
        public int FirstTickY { get; }
        public bool Submit { get; set; }=true;
        private GateFixture(SceneTree tree,GateApi api,bool temple)
        {
            _tree=tree; _api=api;
            _viewport=new SubViewport { Size=new Vector2I(512,384),World2D=new World2D(),RenderTargetUpdateMode=SubViewport.UpdateMode.Always,ProcessMode=Node.ProcessModeEnum.Always };
            var background=new CanvasLayer { Layer=-10 }; background.AddChild(new ColorRect { Size=new Vector2(512,384),Color=Colors.White }); _viewport.AddChild(background);
            _world=new Node2D { ProcessMode=Node.ProcessModeEnum.Pausable }; _viewport.AddChild(_world);
            var packed=GD.Load<PackedScene>(temple ? "res://scenes/prologue/sectors/LostTemple.tscn" : "res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
            var sector=new PrologueSector { SectorId=temple ? PrologueSectorId.LostTemple : PrologueSectorId.CanyonDescent,
                Position=temple ? new Vector2(5120,720) : new Vector2(1280,0) };
            var gameplay=new Node2D { Name="Gameplay" }; var mechanisms=new Node2D { Name="Mechanisms" };
            gameplay.AddChild(mechanisms); sector.AddChild(gameplay); _world.AddChild(sector);
            Gate=packed.GetNode<CanyonPressurePlateGate>(temple ? "Gameplay/Mechanisms/TemplePressureGate" : "Gameplay/Mechanisms/ArchivePressureGate");
            ClearOwners(Gate); Gate.GetParent().RemoveChild(Gate); mechanisms.AddChild(Gate); packed.Free();
            Visual=Gate.GetNode<CanyonPressureGateVisual>("ReadableGate"); Door=Gate.GetNode<StaticBody2D>("Door");
            Center=temple ? new Vector2(5945,860) : new Vector2(1740,1532);
            TickX=temple ? 262 : 251; FirstTickY=temple ? 118 : 110;
            PlatePose=new Vector2(Gate.PlateWorldRect.GetCenter().X,Gate.PlateWorldRect.End.Y-36);
            Player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position=PlatePose; Player.GetNode<Camera2D>("Camera2D").Enabled=false; _world.AddChild(Player); Gate.PlayerBody=Player;
            Wall(new Rect2(Center.X-300,Gate.PlateWorldRect.End.Y,600,20)); Sources=new[] { Visual,Visual };
            var camera=new Camera2D { Position=Center,PositionSmoothingEnabled=false }; _viewport.AddChild(camera);
            var fogLayer=new CanvasLayer { Layer=1 }; Fog=new PrologueFogCanvas { Size=new Vector2(512,384) }; fogLayer.AddChild(Fog); _viewport.AddChild(fogLayer);
            Protected=new CanvasLayer { Layer=2,FollowViewportEnabled=true,FollowViewportScale=1 }; Overlay=api.Create(); Protected.AddChild(Overlay); _viewport.AddChild(Protected);
            tree.Root.AddChild(_viewport); camera.MakeCurrent(); camera.ForceUpdateScroll();
            _sampler=new HeadlampOcclusionSampler(_world); RenderingServer.FramePreDraw+=Present;
        }
        private static void ClearOwners(Node node) { node.Owner=null; foreach(Node child in node.GetChildren()) ClearOwners(child); }
        public static async Task<GateFixture> Create(SceneTree tree,GateApi api,bool temple)
        { var f=new GateFixture(tree,api,temple); try { await f.Frames(8); return f; } catch { f.Dispose(); throw; } }
        public async Task Warning()
        {
            Player.GlobalPosition=PlatePose; Player.Velocity=Vector2.Zero; await Frames(8);
            TestAssert.True(Player.IsOnFloor() && Gate.IsOpen,"real grounded Player presses the saved pressure plate");
            Player.GlobalPosition=OutsidePose; Player.Velocity=Vector2.Zero; await Frames(86);
            TestAssert.True(Gate.IsDangerTelegraphActive && Gate.DoorCollision.Disabled,"original grace naturally reaches harmless closing warning");
        }
        public StaticBody2D Wall(Rect2 bounds)
        { var wall=new StaticBody2D { Position=bounds.GetCenter(),CollisionLayer=CombatCollisionLayers.World,CollisionMask=0 }; wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=bounds.Size } }); _world.AddChild(wall); return wall; }
        public bool DoorBlocks()
        {
            using var query=PhysicsRayQueryParameters2D.Create(Center-Vector2.Right*50,Center+Vector2.Right*50,CombatCollisionLayers.World);
            query.CollideWithAreas=false; using var hit=_world.GetWorld2D().DirectSpaceState.IntersectRay(query); return hit.Count>0;
        }
        private void Present()
        {
            if(_disposed || _failure!=null) return;
            try { Frame=_sampler.Capture(); TestAssert.True(Frame.IsComplete && Frame.PhysicsQueries==0,"gate shares fresh native geometry without adding queries");
                Fog.Present(Actor,Actor,Vector2.Left,(_viewport.GetFinalTransform()*_world.GetCanvasTransform()).AffineInverse(),Frame);
                if(Submit) _api.Present(Overlay,Sources,Actor,Frame); else _api.Clear(Overlay); }
            catch(Exception error) { _failure??=error; }
        }
        public async Task Frames(int count)
        {
            for(int index=0;index<count;index++)
            { await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame); await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw); if(_failure!=null) ExceptionDispatchInfo.Capture(_failure).Throw(); }
        }
        public async Task<Image> Pixels()
        { await Frames(1); Image image=_viewport.GetTexture().GetImage(); TestAssert.True(image!=null && !image.IsEmpty(),"actual pressure gate GPU raster exists"); return image; }
        public void Dispose()
        {
            if(_disposed) return; _disposed=true; _tree.Paused=false; RenderingServer.FramePreDraw-=Present; _sampler.Dispose();
            if(GodotObject.IsInstanceValid(Visual) && Visual.GetParent()==null) Visual.Free();
            _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free();
        }
    }
}
