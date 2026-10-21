using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

/// <summary>
/// Display-backed, isolated attack-cue pixels. The saved-scene formal fog gate
/// remains CLOSED: this does not certify the other Task3 protected cues.
/// No artificial attack frames, manual actor steps, image files or user IO.
/// </summary>
public static class FogReadabilityRuntimeTests
{
    private const float PixelTolerance = .008f;
    private static readonly Vector2 WorldOffset = new(2048f, 1408f);
    private static readonly Color Gold = new(215f / 255f, 168f / 255f, 74f / 255f);
    // d=368: .72*smoothstep((368-180)/440)=.2820084748.
    // Pixel-centre and the real floor's contact margin are within .008 of these
    // independent literals; none of these expectations use the fog/overlay model.
    private static readonly Color FoggedGold = new(.6053654f, .4730297f, .2083583f);
    private static readonly Color ShadowedGold = new(.23607843f, .18447059f, .08125490f);
    private static readonly Color EmptyFog = new(.7179915f, .7179915f, .7179915f);

    public static async Task Run(SceneTree tree)
    {
        // Absence is an explicit behavior RED, not a compile-time dependency on
        // the proposed production type. Root runs this before implementing it.
        var api = new OverlayApi();
        await RealLockedTelegraphAndCameraProjectionReachPixels(tree, api);
        await NativeWallRejectsTheStillLockedSource(tree, api);
        await RealNoneAndDetachedOrFreedSourcesClearPixels(tree, api);
        await LogicalCanvasStretchKeepsTheRealRightHandSourceEligible(tree, api);
        await RealActiveProjectileUsesItsCurrentGeometry(tree, api);
        await TwoRealWarningsSurviveIndependentCancellation(tree, api);
        await QueuedRealEnemyAncestorWithdrawsItsWarningImmediately(tree, api);
        GD.Print("FOG_READABILITY_FORMAL_GATE_STILL_CLOSED");
    }

    private static async Task QueuedRealEnemyAncestorWithdrawsItsWarningImmediately(SceneTree tree, OverlayApi api)
    {
        using var fixture = await ReadabilityFixture.Create(tree, api, "FogActualQueuedSourceAncestor");
        AttackPresentationFrame locked = await fixture.WaitForLockedTelegraph();
        Vector2 sample = fixture.WarningSample(locked);
        var overlay = (FogReadabilityOverlay)fixture.Overlay;
        api.PresentMany(overlay, new[] { fixture.Source }, fixture.Player.GlobalPosition, fixture.LastOcclusion);
        TestAssert.Equal(1, overlay.PresentedCueCount, "same actual current warning is eligible immediately before enemy retirement");
        fixture.Sentry.QueueFree();
        TestAssert.True(fixture.Sentry.IsQueuedForDeletion() && GodotObject.IsInstanceValid(fixture.Source) &&
            fixture.Source.IsInsideTree() && !fixture.Source.IsQueuedForDeletion(),
            "native queued enemy ancestor leaves a live unqueued child before Godot flushes deletion");
        api.PresentMany(overlay, new[] { fixture.Source }, fixture.Player.GlobalPosition, fixture.LastOcclusion);
        TestAssert.Equal(0, overlay.PresentedCueCount,
            "queued real enemy ancestor revokes the current warning immediately, not one frame after retirement");
        using Image pixels = await fixture.ReadPixels();
        AssertPixel(pixels, fixture.PixelAt(sample), EmptyFog, "flushing the queued real enemy leaves no protected stale warning pixels");
        GD.Print("FOG_READABILITY_REAL_QUEUED_ANCESTOR_REVOKED_OK");
    }

    // Kills Telegraph-only qualification or a cached capsule surviving into
    // Fire: sample the current real projectile's opaque white circle edge.
    private static async Task RealActiveProjectileUsesItsCurrentGeometry(SceneTree tree, OverlayApi api)
    {
        using var fixture = await ReadabilityFixture.Create(tree, api, "FogRealActiveProjectile");
        AttackPresentationFrame warning = await fixture.WaitForLockedTelegraph();
        bool active = false;
        for (int frame = 0; frame < 45; frame++)
        {
            await fixture.PhysicsFrames(1);
            if (fixture.Source.CurrentFrame.Timeline.Phase != AttackPhase.Active) continue;
            active = true;
            break;
        }
        TestAssert.True(active, "same real Sentry naturally fires within the bounded locked-telegraph tail");
        using (Image pixels = await fixture.ReadPixels())
        {
            AttackPresentationFrame actual = fixture.LastSubmittedAttack;
            TestAssert.Equal(warning.Timeline.AttackId, actual.Timeline.AttackId,
                "Active pixels belong to the same genuine attack, not a new or forged frame");
            TestAssert.Equal(AttackPhase.Active, actual.Timeline.Phase, "readback still covers the real Active phase");
            TestAssert.Equal(AttackShapeKind.Circle, actual.Shape.Kind, "real projectile publishes a circle, not the old warning capsule");
            TestAssert.Near(7f, actual.Shape.Extent.X, .001f, "actual Active circle retains the authored projectile radius");
            Vector2 edge = actual.Shape.Origin + new Vector2(7f, 0f);
            AssertPixel(pixels, fixture.PixelAt(edge), Colors.White,
                "current projectile edge remains opaque above fog during Active");
            AssertPixel(pixels, new Vector2I(400, 186), new Color(.7167694f, .7167694f, .7167694f),
                "former telegraph location no longer retains a protected gold capsule");
        }
        GD.Print("FOG_READABILITY_REAL_ACTIVE_PROJECTILE_OK");
    }

    // Kills a single-copy/last-source-wins overlay. Both authors run their real
    // physics against the same living player; no presentation frame is forged.
    private static async Task TwoRealWarningsSurviveIndependentCancellation(SceneTree tree, OverlayApi api)
    {
        using var fixture = await ReadabilityFixture.Create(tree, api, "FogTwoRealSentryWarnings", secondSentry: true);
        AttackPresentationFrame first = await fixture.WaitForLockedTelegraph();
        AttackTelegraphVisual secondSource = fixture.SecondSentry.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        AttackPresentationFrame second = secondSource.CurrentFrame;
        TestAssert.True(second.Timeline.Phase == AttackPhase.Telegraph && second.Timeline.HasLockedTarget &&
            second.Timeline.PhaseRemainingSeconds >= .30f,
            "second real Sentry independently reaches locked Telegraph while the first is still locked");
        Vector2 firstPoint = fixture.WarningSample(first);
        // Independent geometry: second muzzle(480,272), player(32,184),
        // normalized aim(-.981249,-.192746); 80px down the capsule plus its
        // 2px lower edge => (401.1146,258.5429).
        Vector2 secondPoint = WorldOffset + new Vector2(401.1146f, 258.5429f);
        Vector2 secondMuzzle = fixture.SecondSentry.GetNode<Node2D>("Muzzle").GlobalPosition;
        TestAssert.True(secondMuzzle.DistanceTo(WorldOffset + new Vector2(480f, 272f)) < .2f,
            "second actual muzzle matches the independently calculated warning sample");
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameLockedAttack(first.Timeline.AttackId);
            TestAssert.Equal(second.Timeline.AttackId, secondSource.CurrentFrame.Timeline.AttackId,
                "second raster belongs to the same independently live attack");
            AssertPixel(pixels, fixture.PixelAt(firstPoint), Gold, "first warning is not overwritten by the second submission");
            AssertPixel(pixels, fixture.PixelAt(secondPoint), Gold, "second simultaneous warning is protected too");
        }

        fixture.SecondSentry.ApplyThreadPull(Vector2.Zero);
        await fixture.PhysicsFrames(2);
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameLockedAttack(first.Timeline.AttackId);
            TestAssert.Equal(AttackPhase.None, secondSource.CurrentFrame.Timeline.Phase,
                "only the second real thread cancellation publishes None");
            AssertPixel(pixels, fixture.PixelAt(firstPoint), Gold,
                "canceling the second source does not erase the still-live first warning");
            // d from actor(32,184) to sample centre(401.5,259.5):
            // .72*smoothstep((377.1346-180)/440)=.3040779 => .6959221.
            AssertPixel(pixels, fixture.PixelAt(secondPoint), new Color(.6959221f, .6959221f, .6959221f),
                "canceled second warning leaves only fog, never a pooled stale copy");
        }
        GD.Print("FOG_READABILITY_TWO_REAL_SOURCES_INDEPENDENT_NONE_OK");
    }

    // Kills missing/under-fog drawing, screen-space copies, ignored camera zoom,
    // and Clear() retaining a previously submitted source.
    private static async Task RealLockedTelegraphAndCameraProjectionReachPixels(SceneTree tree, OverlayApi api)
    {
        using var fixture = await ReadabilityFixture.Create(tree, api, "FogRealSentryWarning");
        AttackPresentationFrame frame = await fixture.WaitForLockedTelegraph();
        Vector2 warning = fixture.WarningSample(frame);
        Vector2I initialPixel = fixture.PixelAt(warning);
        TestAssert.Equal(new Vector2I(400, 186), initialPixel,
            "real horizontal capsule's opaque lower progress edge projects to the hand-checked initial pixel");
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameLockedAttack(frame.Timeline.AttackId);
            AssertPixel(pixels, initialPixel, Gold, "real locked Sentry warning remains opaque gold above fog");
            // This point is 24px below the warning, outside its 4px stroke and
            // radius-2 capsule: protecting the warning must not clear an area.
            // Pixel centre (400.5,210.5), actor nominal (32,184):
            // d=sqrt(368.5^2+26.5^2), independently gives RGB=.71450047.
            AssertPixel(pixels, new Vector2I(400, 210), new Color(.71450047f, .71450047f, .71450047f),
                "unrelated nearby world pixels remain fogged, not a white reveal rectangle");
        }

        fixture.Camera.Position += new Vector2(4f, -16f);
        fixture.Camera.Zoom = new Vector2(1.1f, 1.5f);
        fixture.Camera.ForceUpdateScroll();
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameLockedAttack(frame.Timeline.AttackId);
            Vector2I transformedPixel = fixture.PixelAt(warning);
            TestAssert.True(transformedPixel.X >= 409 && transformedPixel.X <= 411 &&
                transformedPixel.Y >= 206 && transformedPixel.Y <= 208,
                $"actual translated/nonuniformly zoomed camera maps the same real warning near (410,207), got {transformedPixel}");
            AssertPixel(pixels, transformedPixel, Gold, "warning uses actual world-to-canvas camera projection");
        }

        fixture.SubmitAttack = false;
        api.Clear(fixture.Overlay);
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameLockedAttack(frame.Timeline.AttackId);
            AssertPixel(pixels, fixture.PixelAt(warning), FoggedGold,
                "explicit Clear removes the protected copy while the original real world warning remains under fog");
        }
        GD.Print("FOG_READABILITY_REAL_SENTRY_CAMERA_CLEAR_OK");
    }

    // Kills a cached/unconditional warning which reveals an enemy through a wall.
    // The wall is inserted AFTER direction lock, so no fake frame is needed to
    // keep a real Telegraph alive behind it.
    private static async Task NativeWallRejectsTheStillLockedSource(SceneTree tree, OverlayApi api)
    {
        using var fixture = await ReadabilityFixture.Create(tree, api, "FogRealSentryNativeWall");
        AttackPresentationFrame frame = await fixture.WaitForLockedTelegraph();
        Vector2 warning = fixture.WarningSample(frame);
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, fixture.PixelAt(warning), Gold, "unobstructed source is protected before wall insertion");

        fixture.AddWorldRectangle(new Rect2(WorldOffset + new Vector2(260f, 120f), new Vector2(16f, 100f)));
        await fixture.PhysicsFrames(2);
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameLockedAttack(frame.Timeline.AttackId);
            HeadlampOcclusionFrame snapshot = fixture.LastOcclusion;
            TestAssert.True(snapshot != null && snapshot.IsComplete && snapshot.PhysicsQueries == 0,
                "wall eligibility uses a complete current native rectangle snapshot");
            TestAssert.True(!snapshot.CanSee(fixture.Player.GlobalPosition, fixture.Muzzle.GlobalPosition),
                "actual new 16px World wall blocks actor-to-source LOS");
            AttackPresentationFrame clipped = fixture.Source.CurrentFrame;
            TestAssert.Near(WorldOffset.X + 276f, clipped.Timeline.LockedWorldTarget.X, .2f,
                "Sentry's own native first-hit ray clips the still-locked warning to the wall's right face");
            AssertPixel(pixels, fixture.PixelAt(warning), ShadowedGold,
                "same-ID real world warning remains shadowed, not promoted through the wall");
        }
        GD.Print("FOG_READABILITY_NATIVE_WALL_NO_REVEAL_OK");
    }

    // Kills stale cached geometry after real None, and validity-only lifetime
    // checks which miss streaming's detach-now/free-later retirement boundary.
    private static async Task RealNoneAndDetachedOrFreedSourcesClearPixels(SceneTree tree, OverlayApi api)
    {
        using var fixture = await ReadabilityFixture.Create(tree, api, "FogRealSentryLifetime");
        AttackPresentationFrame frame = await fixture.WaitForLockedTelegraph();
        Vector2 warning = fixture.WarningSample(frame);
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, fixture.PixelAt(warning), Gold, "real cue is present before genuine thread cancellation");

        fixture.Sentry.ApplyThreadPull(Vector2.Zero);
        await fixture.PhysicsFrames(2);
        using (Image pixels = await fixture.ReadPixels())
        {
            TestAssert.Equal(BellBoltSentryState.Stunned, fixture.Sentry.CurrentFrame.State,
                "public thread receiver enters the real Stunned state on natural physics");
            TestAssert.Equal(AttackPhase.None, fixture.Source.CurrentFrame.Timeline.Phase,
                "real Sentry cancellation clears its authoritative telegraph leaf");
            TestAssert.Equal(AttackPhase.None, fixture.LastAttackEvent.Timeline.Phase,
                "real cancellation publishes None to presentation consumers");
            AssertPixel(pixels, fixture.PixelAt(warning), EmptyFog,
                "real None removes the previous warning without clearing the fog itself");
        }

        AttackPresentationFrame next = await fixture.WaitForLockedTelegraph(frame.Timeline.AttackId);
        warning = fixture.WarningSample(next);
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, fixture.PixelAt(warning), Gold, "a new genuine attack is protected before source retirement");

        fixture.Scope.RemoveChild(fixture.Sentry);
        TestAssert.True(GodotObject.IsInstanceValid(fixture.Source) && !fixture.Source.IsInsideTree(),
            "retired source is detached but deliberately still a valid object");
        TestAssert.Equal(next.Timeline.AttackId, fixture.Source.CurrentFrame.Timeline.AttackId,
            "detached real leaf retains the old frame, exercising consumer-side live-tree rejection");
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, fixture.PixelAt(warning), EmptyFog,
                "PresentAttack automatically rejects the same detached source without explicit Clear");

        fixture.Sentry.Free();
        TestAssert.True(!GodotObject.IsInstanceValid(fixture.Source), "source is really freed before stale-reference resubmission");
        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, fixture.PixelAt(warning), EmptyFog,
                "PresentAttack safely rejects the same freed source reference without stale pixels");
        GD.Print("FOG_READABILITY_NONE_DETACH_FREE_OK");
    }

    // Kills mixing a physical source point with logical viewport bounds. The
    // real source is inside both textures; at 1920 its physical x=1728 exceeds
    // the logical width 1280, without being offscreen or beyond detection range.
    private static async Task LogicalCanvasStretchKeepsTheRealRightHandSourceEligible(SceneTree tree, OverlayApi api)
    {
        Vector2I logicalSize = new(1280, 720);
        foreach ((Vector2I physicalSize, Vector2I expectedWarningPixel, float expectedSourceX) in new[]
                 {
                     (new Vector2I(1280, 720), new Vector2I(1072, 186), 1152f),
                     (new Vector2I(1920, 1080), new Vector2I(1608, 279), 1728f)
                 })
        {
            using var fixture = await ReadabilityFixture.Create(tree, api, $"FogRealSentryStretch{physicalSize.X}",
                physicalSize, logicalSize, horizontalOffset: 672f);
            AttackPresentationFrame frame = await fixture.WaitForLockedTelegraph();
            Vector2 warning = fixture.WarningSample(frame);
            TestAssert.Equal(expectedWarningPixel, fixture.PixelAt(warning),
                "same real capsule edge maps to the independent logical/physical warning sample");
            Vector2 logicalSource = fixture.Source.GetGlobalTransformWithCanvas().Origin;
            TestAssert.Near(1152f, logicalSource.X, .01f,
                "real source is at logical x1152, not a screen-centred fake target");
            Vector2 physicalSource = fixture.PhysicalPoint(fixture.Source.GlobalPosition);
            TestAssert.Near(expectedSourceX, physicalSource.X, .01f,
                "real final viewport stretch maps source x1152 to its hand-checked physical coordinate");
            TestAssert.True(physicalSource.X > 0f && physicalSource.X < physicalSize.X &&
                physicalSource.Y > 0f && physicalSource.Y < physicalSize.Y,
                "real source's physical origin is inside the actual texture before eligibility is tested");
            if (physicalSize.X == 1920)
                TestAssert.True(physicalSource.X > logicalSize.X,
                    "1920 case genuinely places the visible physical source beyond logical width1280");
            using (Image pixels = await fixture.ReadPixels())
            {
                fixture.AssertSameLockedAttack(frame.Timeline.AttackId);
                AssertPixel(pixels, expectedWarningPixel, Gold,
                    $"real right-hand Sentry remains protected at {physicalSize} physical / {logicalSize} logical size");
            }
            GD.Print($"FOG_READABILITY_STRETCH_{physicalSize.X}_OK");
        }
    }

    private static void AssertPixel(Image image, Vector2I point, Color expected, string message)
    {
        TestAssert.True(point.X >= 0 && point.Y >= 0 && point.X < image.GetWidth() && point.Y < image.GetHeight(),
            $"{message}: hand-selected real-frame sample {point} is inside the physical viewport");
        Color actual = image.GetPixel(point.X, point.Y);
        TestAssert.True(float.IsFinite(actual.R) && float.IsFinite(actual.G) && float.IsFinite(actual.B) &&
            Mathf.Abs(actual.R - expected.R) <= PixelTolerance &&
            Mathf.Abs(actual.G - expected.G) <= PixelTolerance &&
            Mathf.Abs(actual.B - expected.B) <= PixelTolerance,
            $"{message}; pixel={point} expected={expected} actual={actual} tolerance={PixelTolerance}");
    }

    private sealed class OverlayApi
    {
        private readonly Type _type;
        private readonly MethodInfo _present;
        private readonly MethodInfo _clear;
        private readonly MethodInfo _presentMany;

        public OverlayApi()
        {
            _type = typeof(PrologueFogCanvas).Assembly.GetType("Knight.Presentation.Fog.FogReadabilityOverlay");
            TestAssert.True(_type != null && typeof(Node2D).IsAssignableFrom(_type) && !_type.IsAbstract,
                "real FogReadabilityOverlay Node2D must exist to protect the authoritative attack warning above fog");
            _present = _type.GetMethod("PresentAttack", BindingFlags.Public | BindingFlags.Instance,
                null, [typeof(AttackTelegraphVisual), typeof(Vector2), typeof(HeadlampOcclusionFrame)], null);
            _clear = _type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            _presentMany = _type.GetMethod("PresentAttacks", BindingFlags.Public | BindingFlags.Instance,
                null, [typeof(IReadOnlyList<AttackTelegraphVisual>), typeof(Vector2), typeof(HeadlampOcclusionFrame)], null);
            TestAssert.True(_present != null && _present.ReturnType == typeof(void) &&
                _clear != null && _clear.ReturnType == typeof(void),
                "real overlay exposes PresentAttack(actual source, actor world position, native snapshot) and Clear");
        }

        public Node2D Create()
        {
            var node = Activator.CreateInstance(_type) as Node2D;
            TestAssert.True(node != null, "real overlay can be instantiated as a Godot Node2D");
            return node;
        }

        public void Present(Node2D overlay, AttackTelegraphVisual source, Vector2 actor, HeadlampOcclusionFrame snapshot) =>
            Invoke(_present, overlay, [source, actor, snapshot]);

        public void Clear(Node2D overlay) => Invoke(_clear, overlay, null);

        public void PresentMany(Node2D overlay, IReadOnlyList<AttackTelegraphVisual> sources,
            Vector2 actor, HeadlampOcclusionFrame snapshot)
        {
            TestAssert.True(_presentMany != null && _presentMany.ReturnType == typeof(void),
                "real overlay must accept all simultaneous authoritative attack leaves in one PresentAttacks submission");
            Invoke(_presentMany, overlay, [sources, actor, snapshot]);
        }

        private static void Invoke(MethodInfo method, object instance, object[] arguments)
        {
            try { method.Invoke(instance, arguments); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    private sealed class ReadabilityFixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly OverlayApi _api;
        private readonly SubViewport _viewport;
        private readonly PrologueFogCanvas _fog;
        private readonly HeadlampOcclusionSampler _sampler;
        private readonly Vector2I _pixelSize;
        private readonly float _horizontalOffset;
        private Exception _renderFailure;
        private bool _disposed;
        public Node2D Scope { get; }
        public CharacterBody2D Player { get; }
        public BellBoltSentry Sentry { get; }
        public BellBoltSentry SecondSentry { get; }
        public Node2D Muzzle { get; }
        public AttackTelegraphVisual Source { get; }
        public Camera2D Camera { get; }
        public Node2D Overlay { get; }
        public bool SubmitAttack { get; set; } = true;
        public HeadlampOcclusionFrame LastOcclusion { get; private set; }
        public AttackPresentationFrame LastAttackEvent { get; private set; }
        public AttackPresentationFrame LastSubmittedAttack { get; private set; }
        private readonly AttackTelegraphVisual[] _sources;

        private ReadabilityFixture(SceneTree tree, OverlayApi api, string name,
            Vector2I? physicalSize = null, Vector2I? logicalSize = null, float horizontalOffset = 0f, bool secondSentry = false)
        {
            _tree = tree;
            _api = api;
            _pixelSize = physicalSize ?? new Vector2I(512, 384);
            _horizontalOffset = horizontalOffset;
            Vector2I canvasSize = logicalSize ?? _pixelSize;
            _viewport = new SubViewport
            {
                Name = name, Size = _pixelSize, World2D = new World2D(),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            if (logicalSize.HasValue)
            {
                _viewport.Size2DOverride = logicalSize.Value;
                _viewport.Size2DOverrideStretch = true;
            }
            tree.Root.AddChild(_viewport);
            var whiteLayer = new CanvasLayer { Name = "WhiteBackground", Layer = -10 };
            whiteLayer.AddChild(new ColorRect
            {
                Size = (Vector2)canvasSize, Color = Colors.White,
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            _viewport.AddChild(whiteLayer);
            Scope = new Node2D { Name = "NativeWorld" };
            _viewport.AddChild(Scope);
            Camera = new Camera2D
            {
                Name = "ActualCamera", Position = WorldOffset + (Vector2)canvasSize * .5f,
                Enabled = true, PositionSmoothingEnabled = false, Zoom = Vector2.One
            };
            Scope.AddChild(Camera);
            Camera.MakeCurrent();
            Camera.ForceUpdateScroll();
            var fogLayer = new CanvasLayer { Name = "IsolatedVisibleFog", Layer = 1 };
            _fog = new PrologueFogCanvas
            {
                Size = (Vector2)canvasSize, MouseFilter = Control.MouseFilterEnum.Ignore
            };
            fogLayer.AddChild(_fog);
            _viewport.AddChild(fogLayer);
            var protectedLayer = new CanvasLayer
            {
                Name = "ProtectedAttackOnly", Layer = 2,
                FollowViewportEnabled = true, FollowViewportScale = 1f
            };
            Overlay = api.Create();
            protectedLayer.AddChild(Overlay);
            _viewport.AddChild(protectedLayer);

            AddWorldRectangle(new Rect2(WorldOffset + new Vector2(432f + _horizontalOffset, 221f), new Vector2(96f, 16f)));
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Player.Position = WorldOffset + new Vector2(32f + _horizontalOffset, 184f);
            Scope.AddChild(Player);
            Player.RemoveFromGroup("player"); // no target until the real Sentry settles on its floor
            Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            Player.Velocity = Vector2.Zero;
            Sentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn").Instantiate<BellBoltSentry>();
            Sentry.Position = WorldOffset + new Vector2(480f + _horizontalOffset, 192f);
            Scope.AddChild(Sentry);
            Muzzle = Sentry.GetNode<Node2D>("Muzzle");
            Source = Sentry.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            Sentry.AttackFrameChanged += OnAttackFrame;
            if (secondSentry)
            {
                AddWorldRectangle(new Rect2(WorldOffset + new Vector2(432f, 309f), new Vector2(96f, 16f)));
                SecondSentry = GD.Load<PackedScene>("res://scenes/enemies/BellBoltSentry.tscn").Instantiate<BellBoltSentry>();
                SecondSentry.Name = "SecondRealSentry";
                SecondSentry.Position = WorldOffset + new Vector2(480f, 280f);
                Scope.AddChild(SecondSentry);
                _sources = [Source, SecondSentry.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph")];
            }
            _sampler = new HeadlampOcclusionSampler(Scope);
            RenderingServer.FramePreDraw += PresentLatestFrame;
        }

        public static async Task<ReadabilityFixture> Create(SceneTree tree, OverlayApi api, string name,
            Vector2I? physicalSize = null, Vector2I? logicalSize = null, float horizontalOffset = 0f, bool secondSentry = false)
        {
            var fixture = new ReadabilityFixture(tree, api, name, physicalSize, logicalSize, horizontalOffset, secondSentry);
            try
            {
                for (int frame = 0; frame < 30 && !fixture.Sentry.IsOnFloor(); frame++)
                    await fixture.PhysicsFrames(1);
                TestAssert.True(fixture.Sentry.IsOnFloor(), "real default-gravity Sentry naturally settles on a native World rectangle");
                if (secondSentry)
                    TestAssert.True(fixture.SecondSentry.IsOnFloor(), "second default-gravity Sentry also naturally settles before targeting starts");
                TestAssert.Near(WorldOffset.Y + 184f, fixture.Muzzle.GlobalPosition.Y, .2f,
                    "real authored muzzle and capsule floor offset match the independently selected warning row");
                fixture.Player.GlobalPosition = new Vector2(WorldOffset.X + 32f + fixture._horizontalOffset,
                    fixture.Muzzle.GlobalPosition.Y);
                fixture.Player.AddToGroup("player");
                TestAssert.Near(448f, fixture.Player.GlobalPosition.DistanceTo(fixture.Muzzle.GlobalPosition), .01f,
                    "real living player starts 448 world pixels from the actual muzzle, within detection but outside near-clear fog");
                TestAssert.True(fixture.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                    "real Player Health is initialized, not a healthless fake attack target");
                return fixture;
            }
            catch { fixture.Dispose(); throw; }
        }

        public async Task<AttackPresentationFrame> WaitForLockedTelegraph(ulong previousAttackId = 0)
        {
            for (int frame = 0; frame < 120; frame++)
            {
                await PhysicsFrames(1);
                AttackPresentationFrame actual = Source.CurrentFrame;
                if (actual.Timeline.Phase != AttackPhase.Telegraph || !actual.Timeline.HasLockedTarget ||
                    actual.Timeline.AttackId == previousAttackId || actual.Timeline.PhaseRemainingSeconds < .35f ||
                    actual.Timeline.PhaseRemainingSeconds > .45f) continue;
                AssertSameLockedAttack(actual.Timeline.AttackId);
                return actual;
            }
            throw new InvalidOperationException("Assertion failed: real Sentry did not naturally publish a new locked Telegraph inside the bounded observation window");
        }

        public void AssertSameLockedAttack(ulong attackId)
        {
            AttackPresentationFrame actual = Source.CurrentFrame;
            TestAssert.True(attackId != 0 && actual.Timeline.AttackId == attackId &&
                actual.Timeline.Phase == AttackPhase.Telegraph && actual.Timeline.HasLockedTarget,
                "raster sample still belongs to the same real locked Telegraph, not Active/None or a synthetic frame");
            TestAssert.Equal(actual.Timeline.AttackId, LastAttackEvent.Timeline.AttackId,
                "actual source leaf and real attack event identify the same attack");
            TestAssert.Equal(actual.Timeline.Phase, LastAttackEvent.Timeline.Phase,
                "actual source leaf and real attack event identify the same phase");
        }

        public Vector2 WarningSample(AttackPresentationFrame actual)
        {
            TestAssert.Equal(AttackShapeKind.Capsule, actual.Shape.Kind, "actual locked Sentry warning uses its native-clipped capsule");
            TestAssert.Near(2f, actual.Shape.Extent.X, .001f, "actual warning radius is the authored 2px");
            TestAssert.True(Sentry.CurrentFrame.AimDirection.DistanceTo(Vector2.Left) < .001f,
                "settled real target produces the independently checked horizontal leftward capsule");
            TestAssert.Near(640f, actual.Shape.Extent.Y * 2f - 4f, .2f,
                "unobstructed actual ray publishes the complete 640px warning");
            // The real shape's local bottom straight side connects vertices 23
            // and 24. Locked progress >.59 includes that whole opaque Gold side.
            Transform2D shapeWorld = new(actual.Shape.RotationRadians, actual.Shape.Origin);
            Vector2 sample = shapeWorld * new Vector2(actual.Shape.Extent.X,
                -(actual.Shape.Extent.Y - actual.Shape.Extent.X) + 80f);
            TestAssert.True(sample.DistanceTo(WorldOffset + new Vector2(400f + _horizontalOffset, 186f)) < .3f,
                "real-frame-derived sample agrees with the hand-checked world warning point");
            return sample;
        }

        public Vector2I PixelAt(Vector2 worldPoint)
        {
            Vector2 physical = PhysicalPoint(worldPoint);
            return new Vector2I(Mathf.RoundToInt(physical.X), Mathf.RoundToInt(physical.Y));
        }

        public Vector2 PhysicalPoint(Vector2 worldPoint)
        {
            AssertLayerProjection();
            Vector2 physical = _viewport.GetFinalTransform() * Scope.GetCanvasTransform() * worldPoint;
            TestAssert.True(float.IsFinite(physical.X) && float.IsFinite(physical.Y), "actual world-to-physical warning projection is finite");
            return physical;
        }

        public void AddWorldRectangle(Rect2 bounds)
        {
            var body = new StaticBody2D
            {
                Position = bounds.GetCenter(), CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
            };
            body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = bounds.Size } });
            Scope.AddChild(body);
        }

        public async Task PhysicsFrames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                ThrowRenderFailure();
            }
        }

        public async Task<Image> ReadPixels()
        {
            for (int frame = 0; frame < 2; frame++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                ThrowRenderFailure();
            }
            Image pixels = _viewport.GetTexture().GetImage();
            try
            {
                TestAssert.True(pixels != null && !pixels.IsEmpty() && pixels.GetSize() == _pixelSize,
                    $"real GPU readback supplies the isolated {_pixelSize} physical raster");
                return pixels;
            }
            catch { pixels?.Dispose(); throw; }
        }

        private void OnAttackFrame(AttackPresentationFrame frame) => LastAttackEvent = frame;

        private void PresentLatestFrame()
        {
            if (_disposed || _renderFailure != null) return;
            try
            {
                AssertLayerProjection();
                LastOcclusion = _sampler.Capture();
                TestAssert.True(LastOcclusion.IsComplete, "isolated native rectangle snapshot is complete before rendering");
                Transform2D pixelsToWorld = (_viewport.GetFinalTransform() * Scope.GetCanvasTransform()).AffineInverse();
                _fog.Present(Player.GlobalPosition, Player.GlobalPosition, Vector2.Left, pixelsToWorld, LastOcclusion);
                // Intentionally pass the SAME reference after detach/Free. The
                // production consumer, not a filtering fixture, must reject it.
                if (SubmitAttack)
                {
                    if (GodotObject.IsInstanceValid(Source)) LastSubmittedAttack = Source.CurrentFrame;
                    if (_sources != null) _api.PresentMany(Overlay, _sources, Player.GlobalPosition, LastOcclusion);
                    else _api.Present(Overlay, Source, Player.GlobalPosition, LastOcclusion);
                }
            }
            catch (Exception exception) { _renderFailure = exception; }
        }

        private void AssertLayerProjection()
        {
            Transform2D world = Scope.GetCanvasTransform();
            Transform2D overlay = Overlay.GetCanvasTransform();
            TestAssert.True(world.X.DistanceTo(overlay.X) < .001f && world.Y.DistanceTo(overlay.Y) < .001f &&
                world.Origin.DistanceTo(overlay.Origin) < .01f,
                "fixture layer2 follows the actual world canvas under camera/zoom before any production pixel assertion");
        }

        private void ThrowRenderFailure()
        {
            if (_renderFailure != null) ExceptionDispatchInfo.Capture(_renderFailure).Throw();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            RenderingServer.FramePreDraw -= PresentLatestFrame;
            _sampler.Dispose();
            if (GodotObject.IsInstanceValid(Sentry))
            {
                Sentry.AttackFrameChanged -= OnAttackFrame;
                if (!Sentry.IsInsideTree()) Sentry.Free();
            }
            if (!GodotObject.IsInstanceValid(_viewport)) return;
            _viewport.GetParent()?.RemoveChild(_viewport);
            _viewport.Free();
        }
    }
}
