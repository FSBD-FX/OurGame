using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Original saved terrain leaves in a controlled GPU viewport, not a traversal proof.</summary>
public static class FogTerrainReadabilityRuntimeTests
{
    private static readonly Color Edge = new(157f / 255f, 187f / 255f, 149f / 255f);
    private static readonly Color FoggedEdge = new(.44329412f, .528f, .42070588f);
    private static readonly Vector2 Actor = new(40, 600);

    public static async Task Run(SceneTree tree)
    {
        // Catches a sparse-ray interpolation authorizing a thin blocker between
        // otherwise visible endpoints. The wedge coordinates are hand-derived.
        MethodInfo segment = typeof(HeadlampOcclusionFrame).GetMethod("CanSeeSegment", [typeof(Vector2), typeof(Vector2), typeof(Vector2)]);
        TestAssert.True(segment?.ReturnType == typeof(bool),
            "native occlusion frame must reject a blocker anywhere across a protected terrain segment");
        var wedgeWorld = new Node2D();
        var wedgeBody = new StaticBody2D { Position = new Vector2(200, 621.21f), CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
        wedgeBody.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1f, .18f) } });
        wedgeWorld.AddChild(wedgeBody); tree.Root.AddChild(wedgeWorld);
        try
        {
            using var sampler = new HeadlampOcclusionSampler(wedgeWorld);
            HeadlampOcclusionFrame frame = sampler.Capture();
            Vector2 a = new(304, 635.5f), b = new(312, 635.5f);
            TestAssert.True(frame.CanSee(Actor, a) && frame.CanSee(Actor, b) &&
                !(bool)segment.Invoke(frame, [Actor, a, b]) &&
                !(bool)segment.Invoke(frame, [Actor, b, a]) &&
                (bool)segment.Invoke(frame, [Actor, new Vector2(312,635.5f), new Vector2(320,635.5f)]) &&
                !(bool)segment.Invoke(frame, [new Vector2(float.NaN,0), a, b]),
                "exact segment visibility rejects an interior native blocker in either order but admits its clear neighbor and rejects invalid input");
        }
        finally { wedgeWorld.GetParent().RemoveChild(wedgeWorld); wedgeWorld.Free(); }
        var api = new TerrainApi();
        using var fixture = await TerrainFixture.Create(tree, api);
        fixture.Submit = false;
        using Image baseline = await fixture.Pixels();
        Pixel(baseline, 316, 229, FoggedEdge, "original saved StartFloor edge lies under the real fog");
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
        {
            Pixel(image, 316, 229, Edge, "native StartFloor right edge is protected at its exact support height");
            Pixel(image, 416, 229, Edge, "original far bank left landing edge is protected independently");
            Same(image, baseline, 360, 229, "the real eighty-pixel gap is not filled by a support cue");
            Same(image, baseline, 316, 240, "terrain protection does not reveal the platform face or surrounding area");
        }
        fixture.Protected.Layer = 0;
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, FoggedEdge, "lowering only the same cue below fog visibly darkens the edge");
        fixture.Protected.Layer = 2;
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, Edge, "restoring layer2 restores the same support edge");

        StaticBody2D needle = fixture.Wall(new Rect2(199.5f, 621.12f, 1f, .18f));
        using (Image image = await fixture.Pixels())
        {
            HeadlampOcclusionFrame frame = fixture.Frame;
            Vector2 a = new(304, 635.5f), b = new(312, 635.5f);
            TestAssert.True(frame.CanSee(Actor, a) && frame.CanSee(Actor, b) &&
                !(bool)segment.Invoke(frame, [Actor, a, b]),
                "both endpoint rays miss the native needle but the complete visibility wedge is blocked");
            Pixel(image, 308, 229, FoggedEdge, "thin blocker between endpoint rays cannot leak an eight-pixel protected strip");
            // Actual top..top+2 visibility is wider than the earlier635.5 probe
            // wedge.296..304 stays wholly above the needle at x200 (y>=621.818).
            Pixel(image, 300, 229, Edge, "a genuinely unblocked neighboring strip is still visible");
        }
        needle.GetParent().RemoveChild(needle); needle.Free();
        var nativeRectangle = (RectangleShape2D)fixture.StartCollision.Shape;
        nativeRectangle.Size = new Vector2(320, 1);
        fixture.Submit = false;
        using Image thinBaseline = await fixture.Pixels();
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
            Same(image, thinBaseline, 316, 248, "native support thinner than the display mark cannot paint a false landing beyond its actual bottom");
        nativeRectangle.Size = new Vector2(320, 40);
        StaticBody2D lip = fixture.Wall(new Rect2(304, 635.6f, 8, .3f));
        using (Image image = await fixture.Pixels())
        {
            Pixel(image, 308, 229, FoggedEdge, "a native lip in the half-pixel probe gap still blocks the real support boundary");
            Pixel(image, 300, 229, Edge, "the uncovered adjacent original edge stays protected beside the lip");
        }
        lip.GetParent().RemoveChild(lip); lip.Free();
        StaticBody2D wall = fixture.Wall(new Rect2(160, 560, 16, 80));
        using (Image image = await fixture.Pixels())
        {
            Pixel(image, 316, 229, FoggedEdge, "native wall hides the same platform, not merely its centre");
            Pixel(image, 416, 229, FoggedEdge, "wall-hidden next room does not acquire a landing cue");
        }
        wall.GetParent().RemoveChild(wall); wall.Free();
        fixture.StartCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        await fixture.Frames(2);
        fixture.Submit = false;
        using Image disabledBaseline = await fixture.Pixels();
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
            Same(image, disabledBaseline, 316, 229, "disabled native support cannot leave a visually valid landing");
        fixture.StartCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        await fixture.Frames(2);
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, Edge, "fresh native reenable restores the original landing edge");

        fixture.ActorPosition = new Vector2(40, 680);
        fixture.Submit = false;
        using Image belowBaseline = await fixture.Pixels();
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
            Same(image, belowBaseline, 316, 229, "platform underside cannot reveal its own hidden top face");
        fixture.ActorPosition = new Vector2(-400, 600);
        fixture.Submit = false;
        using Image farBaseline = await fixture.Pixels();
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
            Same(image, farBaseline, 316, 229, "terrain outside the620 candidate radius is not revealed");
        fixture.ActorPosition = Actor;
        fixture.StartVisual.Hide();
        fixture.Submit = false;
        using Image hiddenBaseline = await fixture.Pixels();
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
        {
            Same(image, hiddenBaseline, 316, 229, "hidden original skin cannot leave a terrain cue");
            TestAssert.True(api.Count(fixture.Overlay) > 0 && !fixture.StartVisual.IsVisibleInTree(),
                "hidden original platform skin is excluded without erasing the independent bank");
        }
        fixture.StartVisual.Show();
        Node parent = fixture.StartVisual.GetParent();
        parent.RemoveChild(fixture.StartVisual);
        fixture.Submit = false;
        using Image detachedBaseline = await fixture.Pixels();
        fixture.Submit = true;
        using (Image image = await fixture.Pixels())
        {
            Same(image, detachedBaseline, 316, 229, "a detached original skin cannot retain its old protected pixels");
            TestAssert.True(!fixture.StartVisual.IsInsideTree(), "detached live reference is not a terrain source");
        }
        parent.AddChild(fixture.StartVisual);
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, Edge, "same original reentered source restores its native support cue");
        fixture.Tree.Paused = true;
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, FoggedEdge, "pause clears terrain commands on the next actual raster");
        fixture.Tree.Paused = false;
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, Edge, "resume uses a fresh native support snapshot");
        // A circle just touches the original support at a large canyon coordinate.
        // dy372 and radius620 give reach496 exactly; the remaining strip is one
        // float ULP wide at x5000. This must not fail native polygon triangulation.
        fixture.WorldOffset = new Vector2(5000, 2000);
        fixture.ActorPosition = new Vector2(4504.00048828125f, 2264);
        string[] nativeErrors;
        using (var errors = new NativeErrorObserver())
        {
            await fixture.Frames(1);
            TestAssert.True(api.Count(fixture.Overlay) > 0,
                "radius-boundary test exercises a real submitted native support strip");
            nativeErrors = errors.Snapshot();
        }
        TestAssert.Equal(0, nativeErrors.Length,
            "finite radius-clipped terrain strip must render without native errors: " + string.Join(" | ", nativeErrors));
        fixture.WorldOffset = Vector2.Zero;
        fixture.ActorPosition = Actor;
        using (Image image = await fixture.Pixels())
            Pixel(image, 316, 229, Edge, "normal support pixels remain exact after the radius-boundary case");
        fixture.StartVisual.GetParent().QueueFree();
        // The visual leaf is not queued yet; an ancestor is. Submission must clear
        // immediately, before the next deferred native deletion flush.
        api.Present(fixture.Overlay, fixture.Sources, Actor, fixture.Frame);
        TestAssert.Equal(0, api.Count(fixture.Overlay), "queued readable ancestor withdraws all platform cues immediately");
        GD.Print("FOG_READABILITY_NATIVE_TERRAIN_OK pixels=true fixture=true traversal=false formal_visible=false");
    }

    private static void Pixel(Image image, int x, int y, Color expected, string message)
    {
        Color actual = image.GetPixel(x, y);
        TestAssert.True(float.IsFinite(actual.R) && float.IsFinite(actual.G) && float.IsFinite(actual.B) &&
            Mathf.Abs(actual.R - expected.R) <= .008f && Mathf.Abs(actual.G - expected.G) <= .008f &&
            Mathf.Abs(actual.B - expected.B) <= .008f && Mathf.Abs(actual.A - 1f) <= .008f,
            $"{message}; pixel={x},{y} expected={expected} actual={actual}");
    }
    private static void Same(Image a, Image b, int x, int y, string message) => Pixel(a, x, y, b.GetPixel(x, y), message);

    private sealed class TerrainApi
    {
        private readonly Type _type = typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogTerrainReadabilityOverlay");
        private readonly MethodInfo _present, _clear;
        private readonly PropertyInfo _count;
        public TerrainApi()
        {
            TestAssert.True(_type != null && typeof(Node2D).IsAssignableFrom(_type),
                "real terrain overlay must protect nearby original native support edges above fog");
            _present = _type.GetMethod("PresentTerrain", [typeof(IReadOnlyList<CanyonPlatformVisual>), typeof(Vector2), typeof(HeadlampOcclusionFrame)]);
            _clear = _type.GetMethod("Clear", Type.EmptyTypes);
            _count = _type.GetProperty("PresentedSegmentCount");
            TestAssert.True(_present?.ReturnType == typeof(void) && _clear?.ReturnType == typeof(void) && _count?.PropertyType == typeof(int),
                "terrain overlay accepts original authored skins plus the shared immutable native frame");
        }
        public Node2D Create() => (Node2D)Activator.CreateInstance(_type);
        public int Count(Node2D overlay) => (int)_count.GetValue(overlay);
        public void Present(Node2D overlay, IReadOnlyList<CanyonPlatformVisual> sources, Vector2 actor, HeadlampOcclusionFrame frame) => Invoke(_present, overlay, [sources, actor, frame]);
        public void Clear(Node2D overlay) => Invoke(_clear, overlay, null);
        private static void Invoke(MethodInfo method, object source, object[] args)
        {
            try { method.Invoke(source, args); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
    }

    private sealed class TerrainFixture : IDisposable
    {
        public SceneTree Tree { get; }
        private readonly TerrainApi _api;
        private readonly SubViewport _viewport;
        private readonly Node2D _world;
        private readonly Camera2D _camera;
        private readonly PrologueFogCanvas _fog;
        private readonly HeadlampOcclusionSampler _sampler;
        private Exception _failure;
        public CanvasLayer Protected { get; }
        public Node2D Overlay { get; }
        public CanyonPlatformVisual StartVisual { get; }
        public CollisionShape2D StartCollision { get; }
        public IReadOnlyList<CanyonPlatformVisual> Sources { get; }
        public HeadlampOcclusionFrame Frame { get; private set; }
        public Vector2 ActorPosition { get; set; } = Actor;
        public bool Submit { get; set; } = true;
        public Vector2 WorldOffset
        {
            set
            {
                _world.Position = value;
                _camera.Position = new Vector2(256,600) + value;
                _camera.ForceUpdateScroll();
            }
        }
        private bool _disposed;

        private TerrainFixture(SceneTree tree, TerrainApi api)
        {
            Tree = tree; _api = api;
            _viewport = new SubViewport { Size = new Vector2I(512, 384), World2D = new World2D(),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always, ProcessMode = Node.ProcessModeEnum.Always };
            var background = new CanvasLayer { Layer = -10 };
            background.AddChild(new ColorRect { Size = new Vector2(512,384), Color = Colors.White });
            _viewport.AddChild(background);
            _world = new Node2D(); _viewport.AddChild(_world);
            // Keep original packed native bodies and replaceable skins. Other
            // stateful actors are not mounted by this presentation-only fixture.
            var packed = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonEntrance.tscn").Instantiate<PrologueSector>();
            var sector = new PrologueSector { Name = "OriginalTerrainLeaves", SectorId = PrologueSectorId.CanyonEntrance };
            var gameplay = new Node2D { Name = "Gameplay" };
            var collision = new Node2D { Name = "Collision" };
            gameplay.AddChild(collision); sector.AddChild(gameplay);
            var visualRoot = new Node2D { Name = "VisualRoot" };
            var readable = new Node2D { Name = "GameplayReadable" };
            visualRoot.AddChild(readable); sector.AddChild(visualRoot);
            var sources = new List<CanyonPlatformVisual>();
            foreach (string name in new[] { "StartFloor", "BridgeFarBank" })
            {
                var body = packed.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
                var skin = packed.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/" + name);
                ClearOwners(body); ClearOwners(skin);
                body.GetParent().RemoveChild(body); skin.GetParent().RemoveChild(skin);
                collision.AddChild(body); readable.AddChild(skin); sources.Add(skin);
                if (name == "StartFloor") { StartVisual = skin; StartCollision = body.GetNode<CollisionShape2D>("Collision"); }
            }
            packed.Free(); Sources = sources; _world.AddChild(sector);
            _camera = new Camera2D { Position = new Vector2(256,600), PositionSmoothingEnabled = false };
            _viewport.AddChild(_camera);
            var fogLayer = new CanvasLayer { Layer = 1 };
            _fog = new PrologueFogCanvas { Size = new Vector2(512,384) };
            fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            Protected = new CanvasLayer { Layer = 2, FollowViewportEnabled = true, FollowViewportScale = 1f };
            Overlay = api.Create(); Protected.AddChild(Overlay); _viewport.AddChild(Protected);
            tree.Root.AddChild(_viewport); _camera.MakeCurrent(); _camera.ForceUpdateScroll();
            _sampler = new HeadlampOcclusionSampler(_world);
            RenderingServer.FramePreDraw += Present;
        }
        private static void ClearOwners(Node node)
        {
            node.Owner = null;
            foreach (Node child in node.GetChildren()) ClearOwners(child);
        }
        public static async Task<TerrainFixture> Create(SceneTree tree, TerrainApi api)
        {
            var fixture = new TerrainFixture(tree, api);
            try { await fixture.Frames(2); return fixture; }
            catch { fixture.Dispose(); throw; }
        }
        public StaticBody2D Wall(Rect2 rect)
        {
            var body = new StaticBody2D { Position = rect.GetCenter(), CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
            body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = rect.Size } });
            _world.AddChild(body); return body;
        }
        private void Present()
        {
            if (_disposed || _failure != null) return;
            try
            {
                Frame = _sampler.Capture();
                TestAssert.True(Frame.IsComplete && Frame.PhysicsQueries == 0, "terrain uses a complete current native snapshot with no added space queries");
                _fog.Present(ActorPosition, ActorPosition, Vector2.Left,
                    (_viewport.GetFinalTransform() * _world.GetCanvasTransform()).AffineInverse(), Frame);
                if (Submit) _api.Present(Overlay, Sources, ActorPosition, Frame); else _api.Clear(Overlay);
            }
            catch (Exception error) { _failure ??= error; }
        }
        public async Task Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
                await Tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (_failure != null) ExceptionDispatchInfo.Capture(_failure).Throw();
            }
        }
        public async Task<Image> Pixels()
        {
            await Frames(1); // Inspect the first actual raster after each mutation.
            Image image = _viewport.GetTexture().GetImage();
            TestAssert.True(image != null && !image.IsEmpty() && image.GetSize() == new Vector2I(512,384), "real GPU returns the terrain fixture raster");
            return image;
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true; Tree.Paused = false;
            RenderingServer.FramePreDraw -= Present; _sampler.Dispose();
            if (GodotObject.IsInstanceValid(StartVisual) && StartVisual.GetParent() == null) StartVisual.Free();
            _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free();
        }
    }
}
