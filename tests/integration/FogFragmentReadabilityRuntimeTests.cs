using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

/// <summary>Saved native fragment, GPU cue eligibility and real one-time contact; saved fog is explicitly isolated.</summary>
public static class FogFragmentReadabilityRuntimeTests
{
    private const string FragmentId = "canyon.v2.fragment.entrance";
    private static readonly Vector2 NearActor = new(40f, 600f);
    private static readonly Vector2 CueWorld = new(280f, 578f);
    private static readonly Vector2I CuePixel = new(256, 170);
    private static readonly Vector2I AreaPixel = new(268, 170);
    private static readonly Color Cyan = new(132f / 255f, 235f / 255f, 209f / 255f);
    // Sample centre world(280.5,578.5), actor(40,600), d=241.45910627.
    // Entrance bounds(0,0,1280,720): nearest edge141.5>64; no other region/rest
    // field overlaps. Hand-checked .28*smoothstep((d-180)/440)=.01486265190.
    // Left lamp faces away. Expectations never call the fog/overlay model.
    private static readonly Color EmptyFog = new(.98513735f, .98513735f, .98513735f);
    private static readonly Color FoggedCyan = new(.50995345f, .90787167f, .80742630f);
    private const float PixelTolerance = .008f;

    public static async Task Run(SceneTree tree)
    {
        var api = new FragmentApi(); // Missing production API is an explicit behavior RED.
        await SavedFragmentCueRequiresCurrentEligibility(tree, api);
        await RealContactConsumesTheSavedFragmentOnce(tree, api);
        GD.Print("FOG_READABILITY_NATIVE_FRAGMENT_OK");
    }

    public static async Task RunSaveFailure(SceneTree tree)
    {
        await using var fixture = await FragmentFixture.Create(tree, new FragmentApi());
        string originalHash = fixture.SaveHash();
        object originalSave = fixture.Flow.Save;
        using (var denied = new FileStream(fixture.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
        {
            fixture.Player.GlobalPosition = fixture.Fragment.GlobalPosition;
            await fixture.PhysicsFrames(8);
            TestAssert.True(fixture.BodyEntries == 1 &&
                fixture.Fragment.LastResult == FragmentCollectionResult.SaveFailed && !fixture.Fragment.IsCollected &&
                fixture.CollectedEvents == 0 && fixture.Fragment.FeedbackCount == 0 && fixture.Visual.Visible &&
                ReferenceEquals(originalSave, fixture.Flow.Save) && fixture.SaveHash() == originalHash,
                "real original fragment BodyEntered hits a genuine denied atomic save without live/disk/feedback commit");
            fixture.AssertCollection(0);
            fixture.Player.GlobalPosition = NearActor;
            await fixture.PhysicsFrames(2);
            TestAssert.Equal("保存失败 · 离开再碰触重试", fixture.Fragment.GetNode<Label>("Status").Text,
                "failure presentation must consume the original localized retry instruction, not a synthetic result");
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), new Color(.10f, .08f, .07f),
                    "real saved fragment failure has a screen-contained opaque feedback strip above the actual fog");
            Label status = fixture.Fragment.GetNode<Label>("Status");
            Node owner = typeof(QuestFragment).GetField("_statusRenderer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(fixture.Fragment) as Node;
            TestAssert.True(!status.Visible && owner == fixture.Overlay &&
                !fixture.Level.GetNode<Node2D>("ProtectedCues/Fragments").IsVisibleInTree(),
                "only the actual visible replacement owns the original label; hidden saved overlay cannot steal it");
            await AssertFailureGlyphs(tree, fixture, "保存失败 · 离开再碰触重试");
            fixture.Flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English, VSync = false });
            await fixture.PhysicsFrames(2);
            TestAssert.Equal("SAVE FAILED · LEAVE AND TOUCH AGAIN", status.Text,
                "real own-slot settings update changes the original retry text without touching the collection transaction");
            await AssertFailureGlyphs(tree, fixture, "SAVE FAILED · LEAVE AND TOUCH AGAIN");
            fixture.CameraZoom = new Vector2(1.5f, 1.5f);
            await AssertFailureGlyphs(tree, fixture, "SAVE FAILED · LEAVE AND TOUCH AGAIN");
            fixture.CameraZoom = Vector2.One;
            fixture.UseSavedOverlay = true;
            await fixture.PhysicsFrames(3);
            await AssertFailureGlyphs(tree, fixture, "SAVE FAILED · LEAVE AND TOUCH AGAIN");
            owner = typeof(QuestFragment).GetField("_statusRenderer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(fixture.Fragment) as Node;
            TestAssert.True(owner == fixture.Level.GetNode<Node2D>("ProtectedCues/Fragments") && !status.Visible,
                "actual original mounted controller, not the isolated consumer, owns and draws the same failed original source");
            fixture.UseSavedOverlay = false;
            await fixture.PhysicsFrames(3);
            fixture.AssertCollection(0);
            TestAssert.True(fixture.SaveHash() == originalHash, "language display cannot write the locked collection slot");

            fixture.Submit = false;
            using Image clearBaseline = await fixture.ReadPixels();
            TestAssert.True(status.Visible, "Clear releases original world label visibility");
            fixture.Submit = true;
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), new Color(.10f, .08f, .07f), "same failure returns after Clear");
            fixture.ProtectedLayer.Layer = 0;
            using (Image pixels = await fixture.ReadPixels())
                TestAssert.True(pixels.GetPixel(488, 226).R < .09f, "moving the actual error strip below fog really attenuates it");
            fixture.ProtectedLayer.Layer = 2;
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(488, 226), new Color(.10f, .08f, .07f), "restoring protected layer restores opaque text backdrop");

            StaticBody2D wall = fixture.AddWall();
            await fixture.PhysicsFrames(2);
            using (Image pixels = await fixture.ReadPixels())
                TestAssert.True(!fixture.LastSnapshot.CanSee(NearActor, fixture.Fragment.GlobalPosition) &&
                    pixels.GetPixel(80, 226).R > .9f && status.Visible,
                    $"a real native wall removes the entire failure strip and releases the source label; " +
                    $"pixel={pixels.GetPixel(80, 226)} status={status.Visible} " +
                    $"los={fixture.LastSnapshot.CanSee(NearActor, fixture.Fragment.GlobalPosition)} " +
                    $"actor={fixture.Player.GlobalPosition} target={fixture.Fragment.GlobalPosition} " +
                    $"count={((FogFragmentReadabilityOverlay)fixture.Overlay).PresentedCueCount}");
            wall.Free();
            await fixture.PhysicsFrames(2);
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), new Color(.10f, .08f, .07f), "wall removal restores the same real failed result");
            fixture.Player.GlobalPosition = new Vector2(20, 600);
            await fixture.PhysicsFrames(2);
            using (Image pixels = await fixture.ReadPixels())
                TestAssert.True(pixels.GetPixel(80, 226).R > .9f && status.Visible,
                    "260 distance cannot retain a global error strip for the original nearby-only source");
            fixture.Player.GlobalPosition = NearActor;
            fixture.Level.GetTree().Paused = true;
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), clearBaseline.GetPixel(80, 226),
                    "actual pause clears the stale strip and restores the original label");
            fixture.Level.GetTree().Paused = false;
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), new Color(.10f, .08f, .07f), "resume draws only the current failure");
            Node parent = fixture.Fragment.GetParent();
            parent.RemoveChild(fixture.Fragment);
            using (Image pixels = await fixture.ReadPixels())
                TestAssert.True(status.Visible && pixels.GetPixel(80, 226).R > .9f,
                    "real failed source unload releases display ownership and cannot leave a cached strip");
            parent.AddChild(fixture.Fragment);
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), new Color(.10f, .08f, .07f), "same failed native source can reenter without a new failure model");
            fixture.ProtectedLayer.RemoveChild(fixture.Overlay);
            using (Image pixels = await fixture.ReadPixels())
                TestAssert.True(status.Visible && pixels.GetPixel(80, 226).R > .9f,
                    "actual overlay unload releases its original world label even while source failure persists");
            fixture.ProtectedLayer.AddChild(fixture.Overlay);
            using (Image pixels = await fixture.ReadPixels())
                AssertPixel(pixels, new Vector2I(80, 226), new Color(.10f, .08f, .07f), "same renderer remount restores current failed source only");
        }
        fixture.Player.GlobalPosition = fixture.Fragment.GlobalPosition;
        await fixture.PhysicsFrames(8);
        fixture.AssertCollection(1);
        TestAssert.True(fixture.BodyEntries == 2 && fixture.Fragment.FeedbackCount == 1 &&
            fixture.CollectedEvents == 1 && fixture.CommitObserved,
            "unlock and genuine leave/reenter retry collect the same original fragment exactly once");
        fixture.Player.GlobalPosition = NearActor;
        using (Image pixels = await fixture.ReadPixels())
            TestAssert.True(pixels.GetPixel(80, 226).R > .9f && fixture.Fragment.GetNode<Label>("Status").Visible,
                "successful actual retry removes the error strip and releases its source without a second model");
        GD.Print("FOG_READABILITY_FRAGMENT_REAL_SAVE_FAILURE_OK");
    }

    private static async Task AssertFailureGlyphs(SceneTree tree, FragmentFixture fixture, string literal)
    {
        // Independent native Label oracle: complete literal glyphs, not production
        // metadata, a non-empty Text check or one sampled background pixel.
        var reference = new SubViewport { Size = new Vector2I(512, 384),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        reference.AddChild(new ColorRect { Size = new Vector2(512, 384), Color = new Color(.10f, .08f, .07f) });
        var label = new Label { Position = new Vector2(28, 233), Size = new Vector2(456, 23),
            Text = literal, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontOverride("font", fixture.Fragment.GetNode<Label>("Status").GetThemeFont("font"));
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Colors.White);
        reference.AddChild(label);
        tree.Root.AddChild(reference);
        try
        {
            using Image actual = await fixture.ReadPixels();
            using Image expected = reference.GetTexture().GetImage();
            int ink = 0;
            for (int y = 233; y < 256; y++) for (int x = 28; x < 484; x++)
            {
                Color wanted = expected.GetPixel(x, y), got = actual.GetPixel(x, y);
                if (wanted.R > .2f) ink++;
                TestAssert.True(Mathf.Abs(wanted.R - got.R) <= PixelTolerance &&
                    Mathf.Abs(wanted.G - got.G) <= PixelTolerance && Mathf.Abs(wanted.B - got.B) <= PixelTolerance,
                    $"complete native reference glyph raster must match the real original localized failure; literal={literal} pixel={x},{y} expected={wanted} actual={got}");
            }
            TestAssert.True(ink > 100, "complete visible retry instruction has actual GPU glyph ink, not an empty reference");
        }
        finally { reference.GetParent()?.RemoveChild(reference); reference.Free(); }
    }

    // Kills absent/below-fog cues, area reveal, stale LOS/radius and cached detached sources.
    private static async Task SavedFragmentCueRequiresCurrentEligibility(SceneTree tree, FragmentApi api)
    {
        await using var fixture = await FragmentFixture.Create(tree, api);
        fixture.Submit = false;
        using Image baseline = await fixture.ReadPixels();
        AssertPixel(baseline, CuePixel, EmptyFog, "saved presentation leaves the hand-selected cue point clear before protection");
        fixture.Submit = true;
        using (Image pixels = await fixture.ReadPixels())
        {
            AssertPixel(pixels, CuePixel, Cyan, "original uncollected fragment has an opaque tiny core at exactly240 source distance");
            AssertPixel(pixels, AreaPixel, baseline.GetPixel(AreaPixel.X, AreaPixel.Y),
                "protecting one tiny fragment cue does not reveal its surrounding area");
        }
        fixture.ProtectedLayer.Layer = 0;
        try
        {
            using Image pixels = await fixture.ReadPixels();
            AssertPixel(pixels, CuePixel, FoggedCyan, "the same actual cue under layer1 fog is genuinely darkened");
        }
        finally { fixture.ProtectedLayer.Layer = 2; }
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, Cyan, "restoring only protected layer2 restores the original cue");

        StaticBody2D wall = fixture.AddWall();
        using (Image pixels = await fixture.ReadPixels())
        {
            TestAssert.True(!fixture.LastSnapshot.CanSee(NearActor, fixture.Fragment.GlobalPosition) &&
                !fixture.LastSnapshot.CanSee(NearActor, CueWorld), "fresh native rectangle blocks both source centre and cue point");
            AssertPixel(pixels, CuePixel, new Color(.72f, .72f, .72f),
                "same fragment references cannot expose a protected cue through the native wall");
        }
        wall.GetParent().RemoveChild(wall);
        wall.Free();
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, Cyan, "removing the real wall restores visibility without mutating the fragment");

        fixture.Player.GlobalPosition = new Vector2(20f, 600f); // Fixture distance condition, not route evidence.
        fixture.Submit = false;
        using Image farBaseline = await fixture.ReadPixels();
        fixture.Submit = true;
        using (Image pixels = await fixture.ReadPixels())
        {
            TestAssert.Near(260f, fixture.Player.GlobalPosition.DistanceTo(fixture.Fragment.GlobalPosition), .001f,
                "same original source is genuinely beyond the240 cue radius");
            AssertPixel(pixels, CuePixel, farBaseline.GetPixel(CuePixel.X, CuePixel.Y),
                "out-of-range original fragment matches the actual Clear baseline at that distance");
        }
        fixture.Player.GlobalPosition = NearActor;
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, Cyan, "near eligibility returns without changing the native source");
        Node parent = fixture.Fragment.GetParent();
        parent.RemoveChild(fixture.Fragment);
        TestAssert.True(GodotObject.IsInstanceValid(fixture.Fragment) && !fixture.Fragment.IsInsideTree(),
            "removed native fragment remains instance-valid and is still submitted by reference");
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, EmptyFog, "same detached native fragment cannot leave a cached cue");
        parent.AddChild(fixture.Fragment);
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, Cyan, "readding the original uncollected fragment restores only its live cue");
        fixture.AssertCollection(0);
        GD.Print("FOG_READABILITY_FRAGMENT_NATIVE_ELIGIBILITY_PIXELS_OK");
    }

    // A fresh saved fixture proves reentry preserves the actual pickup subscription,
    // separately from the presentation-only detach/readd case above.
    private static async Task RealContactConsumesTheSavedFragmentOnce(SceneTree tree, FragmentApi api)
    {
        await using var fixture = await FragmentFixture.Create(tree, api);
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, Cyan, "fresh saved fragment starts with a genuine eligible cue");
        Node parent = fixture.Fragment.GetParent();
        for (int cycle = 0; cycle < 2; cycle++)
        {
            parent.RemoveChild(fixture.Fragment);
            await fixture.PhysicsFrames(2);
            parent.AddChild(fixture.Fragment);
            await fixture.PhysicsFrames(2);
        }
        fixture.AssertCollection(0);
        fixture.Player.GlobalPosition = fixture.Fragment.GlobalPosition;
        for (int frame = 0; frame < 8 && fixture.BodyEntries < 1; frame++) await fixture.PhysicsFrames(1);
        TestAssert.True(fixture.BodyEntries == 1 && fixture.CollectedEvents == 1 && fixture.CommitObserved &&
            fixture.Fragment.FeedbackCount == 1 && fixture.Fragment.IsCollected && !fixture.Visual.Visible,
            "actual original-player BodyEntered collects once after disk/live/HUD commit with one feedback");
        fixture.AssertCollection(1);
        string consumedHash = fixture.SaveHash();
        fixture.Player.GlobalPosition = NearActor;
        await fixture.PhysicsFrames(2);
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, EmptyFog, "returning the player leaves no old cue for the genuinely collected ID");
        fixture.Player.GlobalPosition = fixture.Fragment.GlobalPosition;
        for (int frame = 0; frame < 8 && fixture.BodyEntries < 2; frame++) await fixture.PhysicsFrames(1);
        fixture.AssertCollection(1);
        TestAssert.True(fixture.BodyEntries == 2 && fixture.CollectedEvents == 1 && fixture.Fragment.FeedbackCount == 1 &&
            fixture.SaveHash() == consumedHash, "a second real native contact cannot repeat collection, feedback or saved-byte changes");
        fixture.Player.GlobalPosition = NearActor;
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, CuePixel, EmptyFog, "repeat contact still cannot restore a consumed fragment cue");
        TestAssert.True(fixture.Fragment.LastResult == FragmentCollectionResult.Collected,
            "collected result persists before the real new-session notification");
        fixture.Flow.ReturnToTitle();
        fixture.Flow.StartNew();
        fixture.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        fixture.AssertCollection(0);
        TestAssert.True(fixture.Fragment.LastResult == FragmentCollectionResult.Unavailable &&
            fixture.Fragment.FeedbackCount == 1 && fixture.CollectedEvents == 1 && fixture.Visual.Visible,
            "same twice-reentered fragment receives real SessionStarted reset without duplicate feedback");
        GD.Print("FOG_READABILITY_FRAGMENT_NATIVE_CONTACT_ONCE_OK");
    }

    private static void AssertPixel(Image pixels, Vector2I point, Color expected, string message)
    {
        Color actual = pixels.GetPixel(point.X, point.Y);
        TestAssert.True(float.IsFinite(actual.R) && float.IsFinite(actual.G) && float.IsFinite(actual.B) &&
            Mathf.Abs(actual.R - expected.R) <= PixelTolerance && Mathf.Abs(actual.G - expected.G) <= PixelTolerance &&
            Mathf.Abs(actual.B - expected.B) <= PixelTolerance && Mathf.Abs(actual.A - 1f) <= PixelTolerance,
            $"{message}; pixel={point} expected={expected} actual={actual}");
    }

    private sealed class FragmentApi
    {
        private readonly Type _type;
        private readonly MethodInfo _present, _clear;
        private readonly PropertyInfo _radius;
        public FragmentApi()
        {
            _type = typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogFragmentReadabilityOverlay");
            TestAssert.True(_type != null && typeof(Node2D).IsAssignableFrom(_type) && !_type.IsAbstract,
                "real FogFragmentReadabilityOverlay Node2D must exist to protect nearby native uncollected fragments above fog");
            _present = _type.GetMethod("PresentFragments", BindingFlags.Public | BindingFlags.Instance, null,
                [typeof(IReadOnlyList<QuestFragment>), typeof(Vector2), typeof(HeadlampOcclusionFrame)], null);
            _clear = _type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            _radius = _type.GetProperty("FragmentCueRadius", BindingFlags.Public | BindingFlags.Instance);
            TestAssert.True(_present?.ReturnType == typeof(void) && _clear?.ReturnType == typeof(void) &&
                _radius?.PropertyType == typeof(float) && _radius.GetCustomAttribute<ExportAttribute>() != null,
                "real fragment overlay exposes batch PresentFragments, Clear and exported FragmentCueRadius");
        }
        public Node2D Create()
        {
            var result = Activator.CreateInstance(_type) as Node2D;
            TestAssert.True(result != null, "real fragment overlay can be instantiated");
            try
            {
                TestAssert.Near(240f, (float)_radius.GetValue(result), .001f, "native default radius admits the240 boundary but not the260 condition");
                return result;
            }
            catch { result.Free(); throw; }
        }
        public void Present(Node2D overlay, IReadOnlyList<QuestFragment> sources, Vector2 actor, HeadlampOcclusionFrame native) =>
            Invoke(_present, overlay, [sources, actor, native]);
        public void Clear(Node2D overlay) => Invoke(_clear, overlay, null);
        private static void Invoke(MethodInfo method, Node2D overlay, object[] arguments)
        {
            try { method.Invoke(overlay, arguments); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            { ExceptionDispatchInfo.Capture(exception.InnerException).Throw(); throw; }
        }
    }

    private sealed class FragmentFixture : IAsyncDisposable
    {
        private readonly SceneTree _tree;
        private readonly FragmentApi _api;
        private readonly SubViewport _viewport;
        private HeadlampOcclusionSampler _sampler;
        private Camera2D _camera;
        private PrologueFogCanvas _fog;
        private Node2D _overlay, _world;
        private IsolatedPrologueFixturePaths _paths;
        private IReadOnlyList<QuestFragment> _sources;
        private Exception _failure;
        private bool _subscribed, _sourceSubscribed, _disposed;
        private ulong _playerId, _fragmentId;
        public PrologueLevelController Level { get; private set; }
        public CharacterBody2D Player { get; private set; }
        public QuestFragment Fragment { get; private set; }
        public QuestFragmentVisual Visual { get; private set; }
        public CanvasLayer ProtectedLayer { get; private set; }
        public PrologueGameFlowController Flow { get; private set; }
        public PrologueFormalHud Hud { get; private set; }
        public HeadlampOcclusionFrame LastSnapshot { get; private set; }
        public bool Submit { get; set; } = true;
        public int BodyEntries { get; private set; }
        public int CollectedEvents { get; private set; }
        public bool CommitObserved { get; private set; }
        public string SavePath => _paths.SavePath;
        public Node2D Overlay => _overlay;
        public Vector2 CameraZoom { get => _camera.Zoom; set => _camera.Zoom = value; }
        public bool UseSavedOverlay { get; set; }

        private FragmentFixture(SceneTree tree, FragmentApi api)
        {
            _tree = tree; _api = api;
            _viewport = new SubViewport { Name = "FogSavedNativeFragment", Size = new Vector2I(512, 384),
                World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        }
        public static async Task<FragmentFixture> Create(SceneTree tree, FragmentApi api)
        {
            var fixture = new FragmentFixture(tree, api);
            try { await fixture.Initialize(); return fixture; }
            catch { await fixture.DisposeAsync(); throw; }
        }
        private async Task Initialize()
        {
            var background = new CanvasLayer { Layer = -10 };
            background.AddChild(new ColorRect { Size = new Vector2(512, 384), Color = Colors.White });
            _viewport.AddChild(background);
            Level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            _paths = IsolatedPrologueFixture.Configure(Level, "fog-native-fragment");
            Flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
            Flow.HeadlessAutoStart = false; Flow.AutomationAutoStart = false;
            _viewport.AddChild(Level);
            _tree.Root.AddChild(_viewport);
            await PhysicsFrames(5);
            Flow.StartNew();
            await PhysicsFrames(12);
            Player = Level.Player; _playerId = Player.GetInstanceId();
            Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false); // after Ready AND StartNew
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Player.GlobalPosition = NearActor; Player.Velocity = Vector2.Zero;
            _world = Level.GetNode<Node2D>("WorldRoot");
            foreach (Node sector in _world.GetNode("Sectors").GetChildren())
                sector.GetNodeOrNull<CanvasItem>("VisualRoot")?.Hide();
            HideSavedCanvasLayers(Level);
            _camera = new Camera2D { Position = new Vector2(280f, 600f), Enabled = true,
                PositionSmoothingEnabled = false, Zoom = Vector2.One };
            _viewport.AddChild(_camera); _camera.MakeCurrent(); _camera.ForceUpdateScroll();
            Fragment = Level.CurrentSectorRoot.GetNode<QuestFragment>("Gameplay/QuestFragments/entrance");
            Visual = Fragment.GetNode<QuestFragmentVisual>("AvailableVisual");
            _fragmentId = Fragment.GetInstanceId(); _sources = new[] { Fragment };
            Hud = Level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            TestAssert.True(Flow.State == PrologueGameFlowState.Playing && Player.GetNode<HealthComponent>("Health").CurrentHealth > 0 &&
                !Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing() && Player.IsOnFloor() &&
                Fragment.StableId == FragmentId && Fragment.GlobalPosition == new Vector2(280, 600) &&
                Fragment.GetNode<CollisionShape2D>("Collision").Shape is CircleShape2D { Radius: 18f } &&
                Visual.IsVisibleInTree() && Visual.GlobalPosition + new Vector2(0, -22) == CueWorld &&
                !Level.GetNode<Control>("Fog/Canvas").IsVisibleInTree(),
                "saved playing living player, floor636, original native circle18/ID/visual and isolated saved fog layer match the literal fixture");
            using (var query = PhysicsRayQueryParameters2D.Create(new Vector2(40, 635.75f), new Vector2(40, 637), CombatCollisionLayers.World))
            {
                var hit = Player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                TestAssert.True(hit.ContainsKey("collider") && hit["collider"].AsGodotObject() ==
                    Level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/StartFloor"), "native original StartFloor remains beneath the full-height saved player");
            }
            AssertCollection(0);
            Fragment.BodyEntered += OnBody; Fragment.Collected += OnCollected; _sourceSubscribed = true;
            var fogLayer = new CanvasLayer { Layer = 1 };
            _fog = new PrologueFogCanvas { Size = new Vector2(512, 384) };
            fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            ProtectedLayer = new CanvasLayer { Layer = 2, FollowViewportEnabled = true, FollowViewportScale = 1f };
            _overlay = _api.Create(); ProtectedLayer.AddChild(_overlay); _viewport.AddChild(ProtectedLayer);
            _sampler = new HeadlampOcclusionSampler(_world);
            RenderingServer.FramePreDraw += PresentCurrent; _subscribed = true;
            await PhysicsFrames(2);
        }
        private static void HideSavedCanvasLayers(Node node)
        {
            if (node is CanvasLayer layer) layer.Visible = false;
            foreach (Node child in node.GetChildren()) HideSavedCanvasLayers(child);
        }
        public StaticBody2D AddWall()
        {
            var wall = new StaticBody2D { Name = "NativeFragmentOccluder", Position = new Vector2(160, 590),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
            wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(16, 60) } });
            _world.AddChild(wall);
            return wall;
        }
        public void AssertCollection(int count)
        {
            TestAssert.True(File.Exists(_paths.SavePath), "real saved flow wrote its case-isolated slot");
            using JsonDocument disk = JsonDocument.Parse(File.ReadAllBytes(_paths.SavePath));
            JsonElement raw = disk.RootElement;
            string[] ids = raw.GetProperty("collectedFragmentIds").EnumerateArray().Select(value => value.GetString()).ToArray();
            string[] expected = count == 0 ? Array.Empty<string>() : new[] { FragmentId };
            TestAssert.True(raw.GetProperty("version").GetInt32() == 4 && raw.GetProperty("layoutVersion").GetInt32() == 2 &&
                raw.GetProperty("collectionRuleVersion").GetInt32() == 1 && !raw.GetProperty("legacyCollectionQuotaExempt").GetBoolean() &&
                ids.SequenceEqual(expected) && Level.Progress.CollectedFragmentIds.SetEquals(expected) &&
                Flow.Save.CollectedFragmentIds.SequenceEqual(expected) && Hud.CurrentModel.FragmentCount == count &&
                Hud.CurrentModel.FragmentRequired == 6 && !Hud.CurrentModel.CollectionQuotaExempt &&
                Player.GetInstanceId() == _playerId && Fragment.GetInstanceId() == _fragmentId,
                "original native actor/fragment, raw disk, live progress, accepted Save and actual HUD agree on exactly zero or one current ID");
        }
        public string SaveHash() => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_paths.SavePath)));
        private void OnBody(Node2D body) { if (body == Player) BodyEntries++; }
        private void OnCollected(string id)
        {
            try
            {
                CollectedEvents++;
                TestAssert.True(id == FragmentId && CollectedEvents == 1 && Fragment.FeedbackCount == 1 &&
                    Fragment.IsCollected && !Visual.Visible, "native Collected callback follows its one genuine feedback/visual commit");
                AssertCollection(1); CommitObserved = true;
            }
            catch (Exception error) { _failure ??= error; }
        }
        private void PresentCurrent()
        {
            if (_disposed || _failure != null) return;
            try
            {
                HideSavedCanvasLayers(Level); // Keep any saved protected copy out of this isolated layer negative-control.
                TestAssert.True(!Level.GetNode<Control>("Fog/Canvas").IsVisibleInTree(),
                    "saved formal fog is effectively hidden; independent pixel fixture cannot double-apply fog");
                Level.GetNode<CanvasLayer>("ProtectedCues").Visible = UseSavedOverlay;
                _overlay.Visible = !UseSavedOverlay;
                _camera.MakeCurrent(); _camera.ForceUpdateScroll();
                Transform2D world = _world.GetCanvasTransform();
                Transform2D overlay = _overlay.IsInsideTree() ? _overlay.GetCanvasTransform() : world;
                TestAssert.True((!_overlay.IsInsideTree() || (world.X.DistanceTo(overlay.X) < .001f && world.Y.DistanceTo(overlay.Y) < .001f &&
                    world.Origin.DistanceTo(overlay.Origin) < .01f)) &&
                    (world * CueWorld).DistanceTo(new Vector2(256, 192) + new Vector2(0, -22) * _camera.Zoom) < .01f,
                    "real own camera maps the raw native cue under its actual zoom and protected world layer follows the same viewport");
                LastSnapshot = _sampler.Capture();
                TestAssert.True(LastSnapshot.IsComplete, "each render receives a fresh complete saved-world native rectangle snapshot");
                _fog.Present(Player.GlobalPosition, Player.GlobalPosition, Vector2.Left,
                    (_viewport.GetFinalTransform() * world).AffineInverse(), LastSnapshot);
                TestAssert.True(_fog.FrameValid, "actual fog accepts the current saved native snapshot");
                if (Submit) _api.Present(_overlay, _sources, Player.GlobalPosition, LastSnapshot);
                else _api.Clear(_overlay);
            }
            catch (Exception error) { _failure ??= error; }
        }
        public async Task PhysicsFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                ThrowFailure();
            }
        }
        public async Task<Image> ReadPixels()
        {
            for (int frame = 0; frame < 2; frame++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                ThrowFailure();
            }
            Image pixels = _viewport.GetTexture().GetImage();
            try
            {
                TestAssert.True(pixels != null && !pixels.IsEmpty() && pixels.GetSize() == new Vector2I(512, 384),
                    "actual GPU returns the saved-fragment fixture's512x384 image");
                return pixels;
            }
            catch { pixels?.Dispose(); throw; }
        }
        private void ThrowFailure() { if (_failure != null) ExceptionDispatchInfo.Capture(_failure).Throw(); }
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_subscribed) RenderingServer.FramePreDraw -= PresentCurrent;
            if (_sourceSubscribed && GodotObject.IsInstanceValid(Fragment))
            { Fragment.BodyEntered -= OnBody; Fragment.Collected -= OnCollected; }
            _sampler?.Dispose();
            try
            {
                if (GodotObject.IsInstanceValid(Level) && Level.IsInsideTree() && Level.GetNodeOrNull<Node>("Audio") != null)
                    await TestSceneTeardown.DrainAudioOwner(_tree, Level);
            }
            finally
            {
                if (GodotObject.IsInstanceValid(Fragment) && Fragment.GetParent() == null) Fragment.Free();
                if (GodotObject.IsInstanceValid(_overlay) && _overlay.GetParent() == null) _overlay.Free();
                if (GodotObject.IsInstanceValid(Level) && Level.GetParent() == null) Level.Free();
                if (GodotObject.IsInstanceValid(_viewport))
                { _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free(); }
            }
        }
    }
}
