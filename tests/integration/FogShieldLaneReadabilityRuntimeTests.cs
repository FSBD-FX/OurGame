using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Integration;

/// <summary>Saved persistent damage source, real native shape and GPU; not traversal or final art.</summary>
public static class FogShieldLaneReadabilityRuntimeTests
{
    private static readonly Vector2 Actor = new(-165,600);
    private static readonly Color Danger = new(.52f,.35f,.22f);

    // Break caught: omitting the real polling source, requiring EnemyAttack or
    // Monitorable, retaining paint after polling stops, or using stale node bounds.
    public static async Task Run(SceneTree tree)
    {
        var api=new LaneApi();
        using var f=await LaneFixture.Create(tree,api);
        // Freeze automatic cosmetic time, not the real hazard physics. Advancing
        // the public process callback below gives two deterministic GPU samples.
        f.Overlay.SetProcess(false);
        f.Submit=false; using Image baseline=await f.Pixels(); f.Submit=true;
        TestAssert.True(f.Lane.CollisionLayer==0 && f.Lane.CollisionMask==2 && f.Lane.Monitoring &&
            f.Lane.IsPhysicsProcessing() && f.Lane.CanProcess() && !f.Collision.Disabled,
            "the original saved layer-zero polling damage source is actually running");
        TestAssert.Equal(new Vector2(260,120),((RectangleShape2D)f.Collision.Shape).Size,
            "saved native damage dimensions are preserved");
        using(Image image=await f.Pixels())
        {
            Pixel(image,191,192,Danger,"native danger perimeter is continuous muted bronze rather than the bright orange box");
            Pixel(image,195,133,new Color(.88f,.57f,.25f),"upper native corner remains a clear danger marker above the muted perimeter");
            Same(image,baseline,193,192,"muted perimeter is thin and never expands the native danger field");
            Same(image,baseline,220,192,"danger cue does not fill the entire lane or illuminate its room");
            DifferentRegion(image,baseline,new Rect2I(433,144,12,96),64,
                "persistent lane shows its right-side damage source, not only a perimeter box");
            // Break caught: rendering only an abstract source line (or moving
            // the solid machinery to the left) instead of the real right source.
            MechanismBody(image,baseline,new Rect2I(418,144,26,96),260,
                "right source has a readable solid coil housing inside the native damage field");
            MechanismBody(image,baseline,new Rect2I(412,152,6,80),180,
                "coil has a solid shaded side instead of only a flat bright source rail");
            DifferentRegion(image,baseline,new Rect2I(280,177,100,7),90,
                "upper electrical discharge visibly connects the field to its source");
            DifferentRegion(image,baseline,new Rect2I(280,201,100,7),90,
                "lower electrical discharge remains visible without flashing danger off");
            DifferentRegion(image,baseline,new Rect2I(229,161,4,5),2,
                "persistent lane has a visible left-facing current tip inside the real danger area");
            Same(image,baseline,242,163,"left-facing chevron stays open on its right, not a right-facing arrow");
            DifferentRegion(image,baseline,new Rect2I(229,218,4,5),2,
                "lower current row remains visible independently of the upper row");
            TestAssert.Equal(1,api.Count(f.Overlay),"duplicate references do not duplicate the original lane source");
        }
        using(Image before=await f.Pixels())
        {
            float first=ChangedCentroidX(before,baseline,new Rect2I(236,172,44,3));
            f.Overlay._Process(.1);
            using Image after=await f.Pixels();
            float second=ChangedCentroidX(after,baseline,new Rect2I(236,172,44,3));
            TestAssert.True(first-second>2f && first-second<6f,
                $"cosmetic current moves world-left without a false safe interval; before={first} after={second}");
            float arcLeft=ArcShiftDifference(before,after,4);
            float arcRight=ArcShiftDifference(before,after,-4);
            TestAssert.True(arcLeft<arcRight*.8f,
                $"continuous discharge ripple moves left toward the player, not back toward its source; left_error={arcLeft} right_error={arcRight}");
            Pixel(after,191,192,Danger,"danger boundary stays on while cosmetic current moves");
        }
        // Break caught: fixed segment count undersamples a widened lane and
        // aliases an intended left-travelling ripple into apparent right motion.
        f.Lane.Scale=new Vector2(3f,1f);
        using(Image before=await f.Pixels())
        {
            f.Overlay._Process(.1);
            using Image after=await f.Pixels();
            float left=ArcShiftDifference(before,after,4),right=ArcShiftDifference(before,after,-4);
            TestAssert.True(left<right*.8f,
                $"widened lane retains world-left ripple without spatial aliasing; left_error={left} right_error={right}");
        }
        f.Lane.Scale=new Vector2(-1,1);
        using(Image image=await f.Pixels())
        {
            DifferentRegion(image,baseline,new Rect2I(433,144,12,96),64,"mirrored native field keeps world-right danger source");
            MechanismBody(image,baseline,new Rect2I(418,144,26,96),260,
                "mirroring the native shape never moves solid machinery away from the world-right source");
            DifferentRegion(image,baseline,new Rect2I(392,161,4,5),2,"mirrored native field retains world-left current tips");
            Same(image,baseline,406,163,"mirrored current does not reverse toward world-right");
        }
        f.Lane.Scale=Vector2.One;
        // Break caught: a fixed world inset crosses the center in a narrow
        // transformed native field and falsely advertises a left-side source.
        f.Lane.Scale=new Vector2(.05f,1f);
        f.Submit=false; using Image narrowBaseline=await f.Pixels(); f.Submit=true;
        using(Image image=await f.Pixels())
        {
            DifferentRegion(image,narrowBaseline,new Rect2I(323,181,3,24),16,
                "narrow transformed lane keeps its source on world-right");
            Same(image,narrowBaseline,316,192,"narrow field never shifts its source to world-left");
        }
        f.Lane.Scale=Vector2.One;
        // Original entrance density at355px attenuates only~.098, confirmed by
        // the .9019608 raster. For the >.1 layer negative use a genuinely foggy
        // 485px fixture distance; do not strengthen production fog or tolerance.
        f.ActorPosition=new Vector2(-295,600);
        f.Protected.Layer=0;
        using(Image image=await f.Pixels())
            TestAssert.True(image.GetPixel(191,192).R<Danger.R*.92f,
                $"putting the same boundary under real fog attenuates it; pixel={image.GetPixel(191,192)} actor={f.ActorPosition} distance_to_left={f.ActorPosition.DistanceTo(new Vector2(190,600))} count={api.Count(f.Overlay)}");
        f.Protected.Layer=2; f.ActorPosition=Actor;
        f.Lane.Monitorable=false;
        using(Image image=await f.Pixels()) Pixel(image,191,192,Danger,"Monitorable does not disable this source's body polling");
        f.Lane.Monitorable=true;
        f.Lane.SetPhysicsProcess(false);
        using(Image image=await f.Pixels())
        { Same(image,baseline,191,192,"disabled damage callback cannot leave a false hazard despite live native Area"); SameInterior(image,baseline,"stopped polling withdraws all direction strokes"); TestAssert.Equal(0,api.Count(f.Overlay),"stopped polling is not danger"); }
        f.Lane.SetPhysicsProcess(true);
        f.Lane.ProcessMode=Node.ProcessModeEnum.Disabled;
        f.Lane.DisableMode=CollisionObject2D.DisableModeEnum.KeepActive;
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"KeepActive preserves geometry but not this disabled damage callback");
        f.Lane.ProcessMode=Node.ProcessModeEnum.Inherit;
        f.Lane.DisableMode=CollisionObject2D.DisableModeEnum.Remove;
        f.Lane.GetParent().ProcessMode=Node.ProcessModeEnum.Disabled;
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"inherited disabled polling also withdraws damage paint");
        f.Lane.GetParent().ProcessMode=Node.ProcessModeEnum.Inherit;
        // Direct same-frame consumer check isolates Monitoring from callback
        // eligibility. Restore it synchronously before original polling runs.
        f.Lane.Monitoring=false;
        api.Present(f.Overlay,f.Sources,Actor,f.Frame);
        TestAssert.Equal(0,api.Count(f.Overlay),"Monitoring alone immediately revokes the same-frame source while polling is still enabled");
        f.Lane.Monitoring=true;
        // Original hazard unconditionally queries overlaps in its callback.
        // A stopped/unmonitored fixture must stop that callback first; this
        // paired GPU control complements the isolated synchronous check above.
        f.Lane.SetPhysicsProcess(false); f.Lane.Monitoring=false;
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"stopped nonmonitoring source leaves no stale damage boundary");
        f.Lane.Monitoring=true; f.Lane.SetPhysicsProcess(true);
        f.Lane.CollisionMask=0;
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"source unable to detect Player has no player-danger cue");
        f.Lane.CollisionMask=CombatCollisionLayers.Player;
        f.Lane.Damage=0;
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"zero-damage lane is not painted as a damaging source");
        f.Lane.Damage=1;
        uint owner=(uint)f.Lane.GetShapeOwners()[0];
        f.Lane.ShapeOwnerSetDisabled(owner,true);
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"native disabled owner withdraws danger even if node flag is stale");
        f.Lane.ShapeOwnerSetDisabled(owner,false);
        f.Collision.Disabled=true;
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"disabled Collision child withdraws the native boundary");
        f.Collision.Disabled=false;
        Transform2D original=f.Lane.ShapeOwnerGetTransform(owner);
        f.Lane.ShapeOwnerSetTransform(owner,new Transform2D(0,new Vector2(24,0))*original);
        using(Image image=await f.Pixels())
        { Same(image,baseline,191,192,"old authored node geometry cannot retain danger"); Pixel(image,215,192,Danger,"paint follows the current native owner rather than Collision node transform"); }
        f.Lane.ShapeOwnerSetTransform(owner,original);

        StaticBody2D wall=f.Wall(new Rect2(0,540,16,120));
        f.Submit=false; using Image blocked=await f.Pixels(); f.Submit=true;
        using(Image image=await f.Pixels())
        { Same(image,blocked,191,192,"actual wall blocks danger in the next room"); SameInterior(image,blocked,"wall-hidden direction strokes are not exposed"); TestAssert.Equal(0,api.Count(f.Overlay),"wall-hidden whole lane is not exposed"); }
        wall.GetParent().RemoveChild(wall); wall.Free();
        // Between actor(-165,600) and complete left strip190..193/596..604:
        // endpoint rays miss this .2px square, but the painted visibility hull does not.
        StaticBody2D needle=f.Wall(new Rect2(9.9f,599.9f,.2f,.2f));
        f.Submit=false; using Image thinBaseline=await f.Pixels(); f.Submit=true;
        using(Image image=await f.Pixels())
        {
            TestAssert.True(f.Frame.CanSee(Actor,new Vector2(190,596)) && f.Frame.CanSee(Actor,new Vector2(190,604)),
                "fractional blocker lies between genuinely clear endpoint rays");
            Same(image,thinBaseline,191,192,"full stroke hull rejects a thin blocker that sparse endpoints miss");
            Same(image,thinBaseline,440,192,"source spine also respects the full-stroke visibility hull");
            Pixel(image,191,152,Danger,"independent visible boundary portions remain readable");
        }
        needle.GetParent().RemoveChild(needle); needle.Free();
        f.Lane.GetParent().SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true);
        using(Image image=await f.Pixels()) { Same(image,baseline,191,192,"staged ancestor cannot advertise retired native danger"); SameInterior(image,baseline,"staged ancestor withdraws all direction strokes"); }
        f.Lane.GetParent().RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);
        f.Lane.Hide();
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"hidden source withdraws old paint");
        f.Lane.Show(); f.ActorPosition=new Vector2(-500,600);
        f.Submit=false; using Image far=await f.Pixels(); f.Submit=true;
        using(Image image=await f.Pixels())
        { Same(image,far,191,192,"source beyond620 is not revealed"); TestAssert.Equal(0,api.Count(f.Overlay),"out-of-range lane is absent"); }
        f.ActorPosition=Actor;
        tree.Paused=true;
        using(Image image=await f.Pixels()) { Same(image,baseline,191,192,"pause removes protected damage paint immediately"); SameInterior(image,baseline,"pause removes every direction stroke"); }
        tree.Paused=false;
        using(Image image=await f.Pixels()) Pixel(image,191,192,Danger,"resume checks the actually running native lane again");
        Node parent=f.Lane.GetParent(); parent.RemoveChild(f.Lane);
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"detached instance-valid source leaves no stale hazard");
        parent.AddChild(f.Lane);
        using(Image image=await f.Pixels()) Pixel(image,191,192,Danger,"same reentered live source can regain danger paint");
        f.Protected.RemoveChild(f.Overlay);
        TestAssert.Equal(0,api.Count(f.Overlay),"overlay's own exit clears its commands before any next raster");
        using(Image image=await f.Pixels()) Same(image,baseline,191,192,"detached display cannot retain a hazard");
        f.Protected.AddChild(f.Overlay);
        using(Image image=await f.Pixels()) Pixel(image,191,192,Danger,"display reentry revalidates rather than reusing stale commands");
        // Break caught: finite native transforms can produce an infinite source
        // span; a world-coordinate floating loop must never stall rendering.
        // The outer process owner is bounded if the old code hangs here.
        f.Lane.Scale=new Vector2(20000f,1f);
        f.Submit=false; using Image hugeBaseline=await f.Pixels(); f.Submit=true;
        GD.Print("COIL_OVERSCALE_ENTER scale=20000,1 native_finite=true");
        using(Image image=await f.Pixels())
        {
            SameInterior(image,hugeBaseline,"undrawable extreme-scale coil submits no nonfinite offscreen paint");
            TestAssert.Equal(0,api.Count(f.Overlay),"extreme-scale native field cannot hang its guarded presentation");
        }
        GD.Print("COIL_OVERSCALE_RETURN bounded=true");
        f.Lane.Scale=Vector2.One;
        // Break caught: protected, above-fog cosmetic current is painted over
        // the player's core. This is a display-only clearance, not a safe hole:
        // native geometry, the perimeter and the emitting device stay active.
        foreach(Vector2 actor in new[] { new Vector2(320,600),new Vector2(370,600),new Vector2(320,628) })
        {
            f.ActorPosition=actor;
            f.Submit=false; using Image clearActor=await f.Pixels(); f.Submit=true;
            using Image visibleActor=await f.Pixels();
            int centerX=(int)actor.X,centerY=(int)actor.Y-408;
            for(int y=centerY-24;y<centerY+24;y++)
                for(int x=centerX-10;x<centerX+10;x++)
                    Same(visibleActor,clearActor,x,y,"current preserves the moving actor core instead of drawing over it");
            Pixel(visibleActor,191,192,Danger,"actor clarity never removes the real field boundary");
            MechanismBody(visibleActor,clearActor,new Rect2I(418,144,26,96),260,
                "actor clarity never erases the solid emitter");
            DifferentRegion(visibleActor,clearActor,new Rect2I(248,177,24,8),12,
                "live current stays visible away from the actor; the hazard has no cosmetic safe interval");
        }
        f.ActorPosition=Actor;
        parent.QueueFree(); api.Present(f.Overlay,f.Sources,Actor,f.Frame);
        TestAssert.Equal(0,api.Count(f.Overlay),"queued source ancestor revokes paint before native deletion flush");
        GD.Print("FOG_READABILITY_NATIVE_SHIELD_LANE_OK pixels=true fixture=true traversal=false formal_visible=false");
    }

    private static void Pixel(Image image,int x,int y,Color expected,string message)
    {
        Color actual=image.GetPixel(x,y);
        TestAssert.True(Mathf.Abs(actual.R-expected.R)<=.008f && Mathf.Abs(actual.G-expected.G)<=.008f &&
            Mathf.Abs(actual.B-expected.B)<=.008f && Mathf.Abs(actual.A-1f)<=.008f,
            $"{message}; pixel={x},{y} expected={expected} actual={actual}");
    }
    private static void Same(Image a,Image b,int x,int y,string message)=>Pixel(a,x,y,b.GetPixel(x,y),message);

    private static void SameInterior(Image image,Image baseline,string message)
    {
        for(int y=140;y<244;y++)
            for(int x=198;x<444;x++) Same(image,baseline,x,y,message);
    }

    private static float ChangedCentroidX(Image image,Image baseline,Rect2I region)
    {
        float sum=0f; int count=0;
        for(int y=region.Position.Y;y<region.End.Y;y++)
            for(int x=region.Position.X;x<region.End.X;x++)
            {
                Color a=image.GetPixel(x,y),b=baseline.GetPixel(x,y);
                if(Mathf.Abs(a.R-b.R)+Mathf.Abs(a.G-b.G)+Mathf.Abs(a.B-b.B)<=.08f) continue;
                sum+=x; count++;
            }
        TestAssert.True(count>=4,"real raster contains the isolated cosmetic tracer");
        return sum/count;
    }

    private static void DifferentRegion(Image image,Image baseline,Rect2I region,int minimum,string message)
    {
        int changed=0;
        for(int y=region.Position.Y;y<region.End.Y;y++)
            for(int x=region.Position.X;x<region.End.X;x++)
            {
                Color a=image.GetPixel(x,y),b=baseline.GetPixel(x,y);
                if(Mathf.Abs(a.R-b.R)+Mathf.Abs(a.G-b.G)+Mathf.Abs(a.B-b.B)>.08f) changed++;
            }
        TestAssert.True(changed>=minimum,$"{message}; changed={changed} minimum={minimum}");
    }

    private static void MechanismBody(Image image,Image baseline,Rect2I region,int minimum,string message)
    {
        int solid=0;
        for(int y=region.Position.Y;y<region.End.Y;y++)
            for(int x=region.Position.X;x<region.End.X;x++)
            {
                Color a=image.GetPixel(x,y),b=baseline.GetPixel(x,y);
                // Dark opaque metal, not the prior bright emissive line, arrow,
                // translucent tracer or unchanged world beneath the cue.
                if(a.R<.65f && a.G<.65f && a.B<.65f &&
                    Mathf.Abs(a.R-b.R)+Mathf.Abs(a.G-b.G)+Mathf.Abs(a.B-b.B)>.25f) solid++;
            }
        TestAssert.True(solid>=minimum,$"{message}; metal_pixels={solid} minimum={minimum}");
    }

    private static float ArcShiftDifference(Image before,Image after,int sourceOffset)
    {
        float sum=0f;
        // World-left travel by four pixels means the next raster resembles the
        // previous raster four pixels to its right. Hand-picked interior ROI
        // excludes the static arrows, coil and old cosmetic dot tracers.
        for(int y=176;y<186;y++)
            for(int x=285;x<370;x++)
            {
                Color a=after.GetPixel(x,y),b=before.GetPixel(x+sourceOffset,y);
                sum+=Mathf.Abs(a.R-b.R)+Mathf.Abs(a.G-b.G)+Mathf.Abs(a.B-b.B);
            }
        return sum;
    }

    private sealed class LaneApi
    {
        private readonly Type _type=typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogShieldLaneReadabilityOverlay");
        private readonly MethodInfo _present,_clear;
        private readonly PropertyInfo _count;
        public LaneApi()
        {
            TestAssert.True(_type!=null && typeof(Node2D).IsAssignableFrom(_type),
                "original saved polling damage lane needs a display-only native boundary above fog");
            _present=_type.GetMethod("PresentLanes",[typeof(IReadOnlyList<CanyonShieldLaneHazard>),typeof(Vector2),typeof(HeadlampOcclusionFrame)]);
            _clear=_type.GetMethod("Clear",Type.EmptyTypes); _count=_type.GetProperty("PresentedLaneCount");
            TestAssert.True(_present?.ReturnType==typeof(void) && _clear?.ReturnType==typeof(void) && _count?.PropertyType==typeof(int),
                "lane cue reads actual polling entities and a shared immutable snapshot");
        }
        public Node2D Create()=>(Node2D)Activator.CreateInstance(_type);
        public int Count(Node2D overlay)=>(int)_count.GetValue(overlay);
        public void Clear(Node2D overlay)=>_clear.Invoke(overlay,null);
        public void Present(Node2D overlay,IReadOnlyList<CanyonShieldLaneHazard> sources,Vector2 actor,HeadlampOcclusionFrame frame)
        {
            try { _present.Invoke(overlay,[sources,actor,frame]); }
            catch(TargetInvocationException error) when(error.InnerException!=null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
    }
    private sealed class LaneFixture:IDisposable
    {
        private readonly SceneTree _tree;
        private readonly LaneApi _api;
        private readonly SubViewport _viewport;
        private readonly World2D _ownedWorld=new();
        private readonly List<RectangleShape2D> _ownedWallShapes=new();
        private readonly Node2D _world;
        private readonly PrologueFogCanvas _fog;
        private readonly HeadlampOcclusionSampler _sampler;
        private Exception _failure;
        private bool _disposed;
        public CanyonShieldLaneHazard Lane { get; }
        public CollisionShape2D Collision { get; }
        public IReadOnlyList<CanyonShieldLaneHazard> Sources { get; }
        public CanvasLayer Protected { get; }
        public Node2D Overlay { get; }
        public HeadlampOcclusionFrame Frame { get; private set; }
        public Vector2 ActorPosition { get; set; }=Actor;
        public bool Submit { get; set; }=true;
        private LaneFixture(SceneTree tree,LaneApi api)
        {
            _tree=tree; _api=api;
            _viewport=new SubViewport { Size=new Vector2I(512,384),World2D=_ownedWorld,
                RenderTargetUpdateMode=SubViewport.UpdateMode.Always,ProcessMode=Node.ProcessModeEnum.Always };
            var background=new CanvasLayer { Layer=-10 };
            background.AddChild(new ColorRect { Size=new Vector2(512,384),Color=Colors.White }); _viewport.AddChild(background);
            _world=new Node2D(); _viewport.AddChild(_world);
            var packed=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
            var sector=new PrologueSector { Name="OriginalLaneLeaf",SectorId=PrologueSectorId.CanyonBasin };
            var gameplay=new Node2D { Name="Gameplay" }; sector.AddChild(gameplay); _world.AddChild(sector);
            Lane=packed.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
            ClearOwners(Lane); Lane.GetParent().RemoveChild(Lane); gameplay.AddChild(Lane); Lane.Position=new Vector2(320,600);
            Collision=Lane.GetNode<CollisionShape2D>("Collision"); packed.Free(); Sources=new[] { Lane,Lane };
            var camera=new Camera2D { Position=new Vector2(256,600),PositionSmoothingEnabled=false }; _viewport.AddChild(camera);
            var fogLayer=new CanvasLayer { Layer=1 }; _fog=new PrologueFogCanvas { Size=new Vector2(512,384) };
            fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            Protected=new CanvasLayer { Layer=2,FollowViewportEnabled=true,FollowViewportScale=1f };
            Overlay=api.Create(); Protected.AddChild(Overlay); _viewport.AddChild(Protected);
            tree.Root.AddChild(_viewport); camera.MakeCurrent(); camera.ForceUpdateScroll();
            _sampler=new HeadlampOcclusionSampler(_world); RenderingServer.FramePreDraw+=Present;
        }
        private static void ClearOwners(Node node) { node.Owner=null; foreach(Node child in node.GetChildren()) ClearOwners(child); }
        public static async Task<LaneFixture> Create(SceneTree tree,LaneApi api)
        {
            var f=new LaneFixture(tree,api);
            try { await f.Frames(2); return f; } catch { f.Dispose(); throw; }
        }
        public StaticBody2D Wall(Rect2 bounds)
        {
            var wall=new StaticBody2D { Position=bounds.GetCenter(),CollisionLayer=CombatCollisionLayers.World,CollisionMask=0 };
            var shape=new RectangleShape2D { Size=bounds.Size }; _ownedWallShapes.Add(shape);
            wall.AddChild(new CollisionShape2D { Shape=shape }); _world.AddChild(wall); return wall;
        }
        private void Present()
        {
            if(_disposed || _failure!=null) return;
            try
            {
                Frame=_sampler.Capture(); TestAssert.True(Frame.IsComplete && Frame.PhysicsQueries==0,"persistent lane shares native snapshot with no space queries");
                _fog.Present(ActorPosition,ActorPosition,Vector2.Left,(_viewport.GetFinalTransform()*_world.GetCanvasTransform()).AffineInverse(),Frame);
                if(Submit) _api.Present(Overlay,Sources,ActorPosition,Frame); else _api.Clear(Overlay);
            }
            catch(Exception error) { _failure??=error; }
        }
        public async Task Frames(int count)
        {
            for(int index=0;index<count;index++)
            {
                await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                if(_failure!=null) ExceptionDispatchInfo.Capture(_failure).Throw();
            }
        }
        public async Task<Image> Pixels()
        {
            await Frames(1); Image image=_viewport.GetTexture().GetImage();
            TestAssert.True(image!=null && !image.IsEmpty() && image.GetSize()==new Vector2I(512,384),"actual GPU returns the first current lane raster"); return image;
        }
        public void Dispose()
        {
            if(_disposed) return; _disposed=true; _tree.Paused=false;
            RenderingServer.FramePreDraw-=Present; _sampler.Dispose();
            if(GodotObject.IsInstanceValid(Lane) && Lane.GetParent()==null) Lane.Free();
            if(GodotObject.IsInstanceValid(Overlay) && Overlay.GetParent()==null) Overlay.Free();
            _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free();
            // Release fixture-owned physics resources on the main thread.
            // Finalizer-thread RID frees can remain queued at immediate Quit;
            // completed managed GC alone does not prove native cleanup.
            // The authored lane shape is borrowed and is never disposed here.
            foreach(RectangleShape2D shape in _ownedWallShapes) shape.Dispose();
            _ownedWallShapes.Clear(); _ownedWorld.Dispose();
        }
    }
}
