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

/// <summary>Original saved steam hazards in an isolated GPU world, not traversal acceptance.</summary>
public static class FogSteamReadabilityRuntimeTests
{
    private static readonly Vector2 Actor = new(40, 600);
    private static readonly Color Warning = new(1f, .73f, .28f);
    private static readonly Color Danger = new(1f, .53f, .17f);

    public static async Task Run(SceneTree tree)
    {
        var api = new SteamApi();
        using var fixture = await SteamFixture.Create(tree, api);
        using Image safe = await fixture.Pixels();
        TestAssert.Equal(0, api.Count(fixture.Overlay), "original saved jet starts safe, not a fabricated warning");
        fixture.Jet.AdvanceRuntime(.96f);
        TestAssert.True(fixture.Jet.Frame.Telegraphing && !fixture.Jet.Frame.HazardActive &&
            fixture.Jet.Collision.Disabled && !fixture.Jet.Monitoring, "original standalone clock enters harmless early warning");
        using (Image image = await fixture.Pixels())
        {
            Pixel(image, 285, 182, Warning, "early warning protects the exact original native left boundary");
            Same(image, safe, 300, 182, "warning protection does not illuminate the danger interior or its room");
            Same(image, safe, 285, 194, "warning uses broken strokes rather than an active solid boundary");
            TestAssert.Equal(1, api.Count(fixture.Overlay), "one original jet is eligible, not each source-list duplicate");
        }
        // At244px the actual entrance fog is deliberately almost clear, so a
        // .1 attenuation assertion there was an invalid fixture premise. Move
        // only the test actor to485px (still within620), not the fog parameters.
        fixture.ActorPosition = new Vector2(-200,600);
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Warning,"the same warning is protected in genuinely dense entrance fog");
        fixture.Protected.Layer = 0;
        using (Image image = await fixture.Pixels())
            TestAssert.True(image.GetPixel(285,182).G < Warning.G - .1f,
                $"the very same warning below real fog is visibly attenuated; pixel={image.GetPixel(285,182)} jets={api.Count(fixture.Overlay)} canvas={fixture.Overlay.GetCanvasTransform()} sourceCanvas={fixture.Jet.GetCanvasTransform()}");
        fixture.Protected.Layer = 2;
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Warning,"restoring layer2 protects the same native warning");
        fixture.ActorPosition = Actor;
        fixture.Jet.AdvanceRuntime(.4f);
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Warning,"the original main warning retains its harmless broken boundary");
        fixture.Jet.AdvanceRuntime(.4f);
        TestAssert.True(fixture.Jet.Frame.HazardActive && !fixture.Jet.Collision.Disabled && fixture.Jet.Monitoring,
            "original saved clock actually enables its native damaging Area before the danger cue");
        using (Image image = await fixture.Pixels())
        {
            Pixel(image,285,182,Danger,"active collision changes the protected boundary to danger orange");
            Pixel(image,286,194,Danger,"active boundary is solid and wider, not merely a recolored warning");
            Same(image,safe,300,182,"active cue still does not fill the entire hazard or reveal its surroundings");
        }
        fixture.Jet.AdvanceRuntime(1f);
        TestAssert.True(!fixture.Jet.Frame.Telegraphing && !fixture.Jet.Frame.HazardActive && fixture.Jet.Frame.Traversable &&
            fixture.Jet.Collision.Disabled && !fixture.Jet.Monitoring,"the original standalone clock completes its actual active-to-safe cycle");
        using (Image image = await fixture.Pixels())
        { Same(image,safe,285,182,"original active-to-safe transition leaves no stale protected danger"); TestAssert.Equal(0,api.Count(fixture.Overlay),"actual safe cycle has no jet cue"); }
        fixture.Jet.EnableForNewRun(); fixture.Jet.AdvanceRuntime(1.76f);

        // Native owner transform can differ from the authored CollisionShape node.
        uint owner = (uint)fixture.Jet.GetShapeOwners()[0];
        Transform2D initial = fixture.Jet.ShapeOwnerGetTransform(owner);
        fixture.Jet.ShapeOwnerSetTransform(owner, new Transform2D(0, new Vector2(24,0)) * initial);
        TestAssert.Equal(Vector2.Zero, fixture.Jet.Collision.Position, "native owner moved without moving the authored child");
        using (Image image = await fixture.Pixels())
        {
            Same(image,safe,285,182,"old authored-child position cannot retain a danger outline");
            Pixel(image,309,182,Danger,"danger follows actual native owner geometry, not stale node geometry");
        }
        fixture.Jet.ShapeOwnerSetTransform(owner, initial);
        fixture.Jet.Collision.Disabled = true;
        using (Image image = await fixture.Pixels())
        { Same(image,safe,285,182,"active logic with disabled native collision fails closed"); TestAssert.Equal(0,api.Count(fixture.Overlay),"inconsistent collision state is not a danger source"); }
        fixture.Jet.Collision.Disabled = false;
        fixture.Jet.Monitoring = false;
        using (Image image = await fixture.Pixels()) Same(image,safe,285,182,"nonmonitoring active Area cannot retain its protected danger");
        fixture.Jet.Monitoring = true;

        fixture.Jet.ProcessMode = Node.ProcessModeEnum.Disabled;
        using (Image image = await fixture.Pixels())
        { Same(image,safe,285,182,"Disabled plus Remove removes the native Area, so retained active flags cannot authorize danger"); TestAssert.Equal(0,api.Count(fixture.Overlay),"removed simulation source has no danger cue"); }
        fixture.Jet.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Danger,"KeepActive retains actual native danger despite disabled callbacks");
        fixture.Jet.ProcessMode = Node.ProcessModeEnum.Inherit;
        fixture.Jet.DisableMode = CollisionObject2D.DisableModeEnum.Remove;
        fixture.Jet.GetParent().ProcessMode = Node.ProcessModeEnum.Disabled;
        using (Image image = await fixture.Pixels()) Same(image,safe,285,182,"inherited Disabled plus Remove also withdraws the native hazard");
        fixture.Jet.GetParent().ProcessMode = Node.ProcessModeEnum.Inherit;
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Danger,"restoring inherited simulation restores current native danger");
        fixture.Jet.GetParent().SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true);
        using (Image image = await fixture.Pixels())
        { Same(image,safe,285,182,"staged source ancestor cannot reveal unmounted danger"); TestAssert.Equal(0,api.Count(fixture.Overlay),"staged batch has no protected hazard"); }
        fixture.Jet.GetParent().RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Danger,"removing the fixture staging marker restores the current source");

        StaticBody2D wall = fixture.Wall(new Rect2(160,560,16,80));
        fixture.Submit = false; using Image blocked = await fixture.Pixels(); fixture.Submit = true;
        using (Image image = await fixture.Pixels())
        { Same(image,blocked,285,182,"native wall occludes the real jet boundary"); TestAssert.Equal(0,api.Count(fixture.Overlay),"fully hidden native hazard does not reveal its next room"); }
        wall.GetParent().RemoveChild(wall); wall.Free();
        // Both endpoint rays to the left strip at y589..597 miss this tiny
        // obstacle; the complete painted quad's visibility wedge intersects it.
        StaticBody2D needle = fixture.Wall(new Rect2(159.9f,595.9f,.2f,.2f));
        fixture.Submit = false; using Image needleBaseline = await fixture.Pixels(); fixture.Submit = true;
        using (Image image = await fixture.Pixels())
        {
            TestAssert.True(fixture.Frame.CanSee(Actor,new Vector2(284,589)) && fixture.Frame.CanSee(Actor,new Vector2(284,597)),
                "tiny native blocker is genuinely between clear endpoint rays");
            Same(image,needleBaseline,285,182,"a blocker between endpoint rays rejects the complete painted strip");
            Pixel(image,285,150,Danger,"an independently visible neighboring strip remains protected");
        }
        needle.GetParent().RemoveChild(needle); needle.Free();

        fixture.Diverter.Advance(.8d);
        var right = fixture.Diverter.GetNode<CanyonSteamJet>("RightJet");
        TestAssert.True(right.Frame.Telegraphing && fixture.Diverter.GetNode<CanyonSteamJet>("LeftJet").DisabledForRun,
            "original saved diverter, not a copied phase model, selects only its right early warning");
        using (Image image = await fixture.Pixels())
        { Pixel(image,389,182,Warning,"second independent original jet is protected in the same batch"); TestAssert.Equal(2,api.Count(fixture.Overlay),"batch retains standalone and selected diverter, not last-source-wins"); }
        TestAssert.True(fixture.Diverter.TryReceiveThread(default),"original diverter accepts its actual switch");
        using (Image image = await fixture.Pixels())
        { Same(image,safe,389,182,"actual handoff disarms and withdraws the outgoing warning"); TestAssert.Equal(1,api.Count(fixture.Overlay),"handoff is not falsely painted as two active jets"); }
        await fixture.Frames(2); fixture.Diverter.Advance(.61d); fixture.Diverter.Advance(.8d);
        using (Image image = await fixture.Pixels()) Pixel(image,169,182,Warning,"after original handoff the actual left jet becomes the protected source");
        fixture.Diverter.ResetForCheckpoint();
        using (Image image = await fixture.Pixels())
        { Same(image,safe,169,182,"checkpoint reset removes the original left warning immediately"); TestAssert.Equal(1,api.Count(fixture.Overlay),"reset's safe right jet does not invent a warning"); }
        fixture.Valve.TryActivate();
        using (Image image = await fixture.Pixels())
        { Same(image,safe,285,182,"actual paired valve disables its standalone jet and removes the danger cue"); TestAssert.Equal(0,api.Count(fixture.Overlay),"disabled-for-run is not an active danger"); }
        fixture.Jet.EnableForNewRun(); fixture.Jet.AdvanceRuntime(1.76f);
        fixture.Jet.Hide();
        using (Image image = await fixture.Pixels()) Same(image,safe,285,182,"hidden source cannot retain a jet cue");
        fixture.Jet.Show();
        fixture.ActorPosition = new Vector2(-400,600);
        fixture.Submit = false; using Image far = await fixture.Pixels(); fixture.Submit = true;
        using (Image image = await fixture.Pixels())
        { Same(image,far,285,182,"native hazard beyond620 is not revealed"); TestAssert.Equal(0,api.Count(fixture.Overlay),"out-of-range batch is empty"); }
        fixture.ActorPosition = Actor;
        tree.Paused = true;
        using (Image image = await fixture.Pixels()) Same(image,safe,285,182,"pause clears existing steam paint at the next raster");
        tree.Paused = false;
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Danger,"resume revalidates actual current danger");
        Node parent = fixture.Jet.GetParent(); parent.RemoveChild(fixture.Jet);
        using (Image image = await fixture.Pixels()) Same(image,safe,285,182,"detached original jet cannot leave stale danger");
        parent.AddChild(fixture.Jet);
        using (Image image = await fixture.Pixels()) Pixel(image,285,182,Danger,"same live original reentered jet restores current native danger");
        parent.QueueFree();
        api.Present(fixture.Overlay,fixture.Sources,Actor,fixture.Frame);
        TestAssert.Equal(0,api.Count(fixture.Overlay),"queued source ancestor revokes all steam commands before native deletion flush");
        GD.Print("FOG_READABILITY_NATIVE_STEAM_OK pixels=true fixture=true traversal=false formal_visible=false");
    }

    private static void Pixel(Image image,int x,int y,Color expected,string message)
    {
        Color actual=image.GetPixel(x,y);
        TestAssert.True(Mathf.Abs(actual.R-expected.R)<=.008f && Mathf.Abs(actual.G-expected.G)<=.008f &&
            Mathf.Abs(actual.B-expected.B)<=.008f && Mathf.Abs(actual.A-1f)<=.008f,
            $"{message}; pixel={x},{y} expected={expected} actual={actual}");
    }
    private static void Same(Image a,Image b,int x,int y,string message)=>Pixel(a,x,y,b.GetPixel(x,y),message);

    private sealed class SteamApi
    {
        private readonly Type _type=typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogSteamReadabilityOverlay");
        private readonly MethodInfo _present,_clear;
        private readonly PropertyInfo _count;
        public SteamApi()
        {
            TestAssert.True(_type!=null && typeof(Node2D).IsAssignableFrom(_type),
                "real steam protection must distinguish the original native jet warning and active danger above fog");
            _present=_type.GetMethod("PresentSteam",[typeof(IReadOnlyList<CanyonSteamJet>),typeof(Vector2),typeof(HeadlampOcclusionFrame)]);
            _clear=_type.GetMethod("Clear",Type.EmptyTypes); _count=_type.GetProperty("PresentedJetCount");
            TestAssert.True(_present?.ReturnType==typeof(void) && _clear?.ReturnType==typeof(void) && _count?.PropertyType==typeof(int),
                "steam protection accepts original actual jets and the controller's immutable native snapshot");
        }
        public Node2D Create()=>(Node2D)Activator.CreateInstance(_type);
        public int Count(Node2D overlay)=>(int)_count.GetValue(overlay);
        public void Present(Node2D overlay,IReadOnlyList<CanyonSteamJet> sources,Vector2 actor,HeadlampOcclusionFrame frame)=>Invoke(_present,overlay,[sources,actor,frame]);
        public void Clear(Node2D overlay)=>Invoke(_clear,overlay,null);
        private static void Invoke(MethodInfo method,object source,object[] args)
        {
            try { method.Invoke(source,args); }
            catch(TargetInvocationException error) when(error.InnerException!=null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
    }

    private sealed class SteamFixture:IDisposable
    {
        private readonly SceneTree _tree;
        private readonly SteamApi _api;
        private readonly SubViewport _viewport;
        private readonly Node2D _world;
        private readonly PrologueFogCanvas _fog;
        private readonly HeadlampOcclusionSampler _sampler;
        private Exception _failure;
        private bool _disposed;
        public CanyonSteamJet Jet { get; }
        public CanyonSteamDiverter Diverter { get; }
        public CanyonSteamValve Valve { get; }
        public IReadOnlyList<CanyonSteamJet> Sources { get; }
        public CanvasLayer Protected { get; }
        public Node2D Overlay { get; }
        public HeadlampOcclusionFrame Frame { get; private set; }
        public Vector2 ActorPosition { get; set; }=Actor;
        public bool Submit { get; set; }=true;

        private SteamFixture(SceneTree tree,SteamApi api)
        {
            _tree=tree; _api=api;
            _viewport=new SubViewport { Size=new Vector2I(512,384),World2D=new World2D(),
                RenderTargetUpdateMode=SubViewport.UpdateMode.Always,ProcessMode=Node.ProcessModeEnum.Always };
            var background=new CanvasLayer { Layer=-10 };
            background.AddChild(new ColorRect { Size=new Vector2(512,384),Color=Colors.White }); _viewport.AddChild(background);
            _world=new Node2D(); _viewport.AddChild(_world);
            var packed=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
            var sector=new PrologueSector { Name="OriginalSteamLeaves",SectorId=PrologueSectorId.CanyonBasin };
            var gameplay=new Node2D { Name="Gameplay" }; var mechanisms=new Node2D { Name="Mechanisms" };
            gameplay.AddChild(mechanisms); sector.AddChild(gameplay); _world.AddChild(sector);
            Jet=packed.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            Diverter=packed.GetNode<CanyonSteamDiverter>("Gameplay/Mechanisms/SteamDiverterTrench");
            Valve=packed.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
            foreach(Node leaf in new Node[] { Jet,Diverter,Valve })
            { ClearOwners(leaf); leaf.GetParent().RemoveChild(leaf); mechanisms.AddChild(leaf); }
            Jet.Position=new Vector2(320,600); Diverter.Position=new Vector2(300,560);
            packed.Free();
            Sources=new[] { Jet,Diverter.GetNode<CanyonSteamJet>("LeftJet"),Diverter.GetNode<CanyonSteamJet>("RightJet"),Jet };
            var camera=new Camera2D { Position=new Vector2(256,600),PositionSmoothingEnabled=false }; _viewport.AddChild(camera);
            var fogLayer=new CanvasLayer { Layer=1 }; _fog=new PrologueFogCanvas { Size=new Vector2(512,384) };
            fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            Protected=new CanvasLayer { Layer=2,FollowViewportEnabled=true,FollowViewportScale=1f };
            Overlay=api.Create(); Protected.AddChild(Overlay); _viewport.AddChild(Protected);
            tree.Root.AddChild(_viewport); camera.MakeCurrent(); camera.ForceUpdateScroll();
            Jet.SetPhysicsProcess(false); Diverter.SetPhysicsProcess(false);
            _sampler=new HeadlampOcclusionSampler(_world); RenderingServer.FramePreDraw+=Present;
        }
        private static void ClearOwners(Node node) { node.Owner=null; foreach(Node child in node.GetChildren()) ClearOwners(child); }
        public static async Task<SteamFixture> Create(SceneTree tree,SteamApi api)
        {
            var fixture=new SteamFixture(tree,api);
            try { await fixture.Frames(2); return fixture; } catch { fixture.Dispose(); throw; }
        }
        public StaticBody2D Wall(Rect2 bounds)
        {
            var body=new StaticBody2D { Position=bounds.GetCenter(),CollisionLayer=CombatCollisionLayers.World,CollisionMask=0 };
            body.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=bounds.Size } }); _world.AddChild(body); return body;
        }
        private void Present()
        {
            if(_disposed || _failure!=null) return;
            try
            {
                Frame=_sampler.Capture(); TestAssert.True(Frame.IsComplete && Frame.PhysicsQueries==0,"steam shares a complete native snapshot without space queries");
                _fog.Present(ActorPosition,ActorPosition,Vector2.Left,(_viewport.GetFinalTransform()*_world.GetCanvasTransform()).AffineInverse(),Frame);
                if(Submit) _api.Present(Overlay,Sources,ActorPosition,Frame); else _api.Clear(Overlay);
            }
            catch(Exception error) { _failure??=error; }
        }
        public async Task Frames(int count)
        {
            for(int i=0;i<count;i++)
            {
                await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                if(_failure!=null) ExceptionDispatchInfo.Capture(_failure).Throw();
            }
        }
        public async Task<Image> Pixels()
        {
            await Frames(1); Image image=_viewport.GetTexture().GetImage();
            TestAssert.True(image!=null && !image.IsEmpty() && image.GetSize()==new Vector2I(512,384),"hardware returns the first actual steam raster"); return image;
        }
        public void Dispose()
        {
            if(_disposed) return; _disposed=true; _tree.Paused=false;
            RenderingServer.FramePreDraw-=Present; _sampler.Dispose();
            if(GodotObject.IsInstanceValid(Jet) && Jet.GetParent()==null) Jet.Free();
            _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free();
        }
    }
}
