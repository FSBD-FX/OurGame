using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

/// <summary>Real generated four-role timing/geometry and GPU pixels. Isolated rendering, not formal-scene visual acceptance.</summary>
public static class FogRangedReadabilityRuntimeTests
{
    private static readonly Vector2 Offset = new(2048f, 1408f);
    private static readonly Color Gold = new(215f / 255f, 168f / 255f, 74f / 255f);
    private static readonly Color ShadowedGold = new(.23607843f, .18447059f, .08125490f);

    public static async Task Run(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
            await GeneratedRoleHasRealWarningAndEveryActiveLeafPixel(tree, role);
        await IndependentSourcesSurviveOtherOwnersCancellation(tree);
        await FogMothReadabilityRuntimeTests.Run(tree);
        GD.Print("FOG_RANGED_READABILITY_PIXELS_OK roles=4 source_scenes=generated native_physics=true gpu=true formal_scene_acceptance=false");
    }

    // Break caught: copying only a warning (or only one projectile), drawing the
    // copy below fog, ignoring current World LOS, or retaining canceled geometry.
    // Pixel literals are hand-calculated from the authored native muzzle/floor
    // and 180/620/.72 field. No production fog/geometry helper supplies them.
    private static async Task GeneratedRoleHasRealWarningAndEveryActiveLeafPixel(SceneTree tree, RangedWeaponRole role)
    {
        using var f = await RasterFixture.Create(tree, role, "FogGeneratedRanged" + role);
        AttackTelegraphVisual warning = f.Actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        await f.Until(() => warning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
            warning.CurrentFrame.Timeline.HasLockedTarget, 100);
        AttackPresentationFrame locked = warning.CurrentFrame;
        TestAssert.True(locked.Timeline.Phase == AttackPhase.Telegraph && locked.Timeline.HasLockedTarget &&
            locked.Timeline.Kind == FogRangedSavedControllerRuntimeTests.Kind(role) &&
            f.Actor.IsOnFloor() && f.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
            "generated " + role + " naturally locks against a living packed Player on a real native floor");
        AssertWarningGeometryAndSample(f, role, locked);
        Vector2I warningPixel = WarningPixel(role);
        using (Image pixels = await f.ReadPixels())
        {
            f.AssertSameLocked(warning, locked.Timeline.AttackId);
            f.AssertCurrentCopies();
            Pixel(pixels, warningPixel, Gold, "real " + role + " locked warning is opaque gold above fog");
        }
        f.ProtectedLayer.Layer = 0;
        using (Image pixels = await f.ReadPixels())
        {
            f.AssertSameLocked(warning, locked.Timeline.AttackId);
            f.AssertCurrentCopies();
            Pixel(pixels, warningPixel, FoggedGold(role), "same real " + role + " warning darkens beneath fog");
        }
        f.ProtectedLayer.Layer = 2;
        StaticBody2D wall = f.AddWorldRect("FreshNativeWall", Offset + new Vector2(432f, 260f), new Vector2(12f, 280f));
        using (Image pixels = await f.ReadPixels())
        {
            f.AssertSameLocked(warning, locked.Timeline.AttackId);
            TestAssert.True(f.Snapshot.IsComplete && !f.Snapshot.CanSee(f.Player.GlobalPosition, warning.GlobalPosition) &&
                f.Overlay.PresentedCueCount == 0, "fresh native wall rejects the still-real locked " + role + " source");
            Pixel(pixels, warningPixel, ShadowedGold, "hidden " + role + " warning remains shadowed, never promoted through World");
        }
        wall.GetParent().RemoveChild(wall); wall.Free();
        using (Image pixels = await f.ReadPixels())
        {
            f.AssertSameLocked(warning, locked.Timeline.AttackId);
            f.AssertCurrentCopies();
            Pixel(pixels, warningPixel, Gold, "removing only the actual wall restores current " + role + " warning eligibility");
        }

        // The genuine direction remains locked; moving the fixture target up/right
        // avoids collision without hiding the actor, invoking physics manually,
        // or putting the player beneath the native floor and breaking LOS. The
        // added horizontal distance keeps every sampled tail outside near-clear fog.
        f.Player.GlobalPosition = Offset + new Vector2(736f, 140f);
        await f.Until(() => f.Actor.CurrentFrame.State == (role == RangedWeaponRole.Laser
            ? RangedWeaponState.Active : RangedWeaponState.Recovery), 100);
        await f.Frames(2);
        var activeEdgePixels = new List<Vector2I>();
        AttackTelegraphVisual[] activeSources = Array.Empty<AttackTelegraphVisual>();
        using (Image pixels = await f.ReadPixels())
        {
            int expected = role switch { RangedWeaponRole.Scatter => 5, RangedWeaponRole.Burst => 3, _ => 1 };
            TestAssert.Equal(expected, f.Submitted.Count, "actual " + role + " physical Active/tail source count at readback");
            TestAssert.True(f.Submitted.All(leaf => leaf.CurrentFrame.Timeline.Phase == AttackPhase.Active),
                "all " + role + " sampled leaves belong to current physical Active attacks");
            f.AssertCurrentCopies();
            activeSources = f.Submitted.ToArray();
            foreach (AttackTelegraphVisual leaf in f.Submitted)
            {
                AttackShapeFrame shape = leaf.CurrentFrame.Shape;
                Vector2 sample;
                if (role == RangedWeaponRole.Laser)
                {
                    TestAssert.True(shape.Kind == AttackShapeKind.Capsule && shape.Extent.X == 5f &&
                        MathF.Abs(f.Actor.CurrentFrame.AimDirection.Y) > .20f,
                        "actual Laser pixels use its real five-pixel half-width diagonal beam, not an axis-aligned stand-in");
                    sample = CapsuleNearMuzzle(shape, 40f);
                }
                else
                {
                    float radius = role switch { RangedWeaponRole.Scatter => 6f, RangedWeaponRole.Burst => 5f, _ => 8f };
                    TestAssert.True(shape.Kind == AttackShapeKind.Circle && shape.Extent == Vector2.One * radius &&
                        leaf.GetParent() is RangedWeaponProjectile { IsRetired: false },
                        "actual " + role + " circle is attached to its live physical projectile");
                    sample = shape.Origin + Vector2.Right * radius;
                    foreach (AttackTelegraphVisual other in f.Submitted)
                        if (other != leaf)
                            TestAssert.True(sample.DistanceTo(other.CurrentFrame.Shape.Origin) > radius + 3f,
                                "each chosen " + role + " edge pixel is outside every other projectile's outline");
                }
                Vector2I pixel = f.PixelAt(sample);
                activeEdgePixels.Add(pixel);
                Pixel(pixels, pixel, Colors.White,
                    "each independent current " + role + " Active/tail edge is opaque white above fog");
            }
        }

        f.Actor.ApplyThreadPull(Vector2.Left * 540f);
        using (Image pixels = await f.ReadPixels())
        {
            TestAssert.True(f.Submitted.Count == 0 && f.Overlay.PresentedCueCount == 0 &&
                warning.CurrentFrame.Timeline.Phase == AttackPhase.None,
                "actual thread interruption cancels " + role + " warning, physical beam and every projectile leaf");
            Pixel(pixels, warningPixel, EmptyAfterTargetMove(role),
                "real " + role + " cancellation leaves only fog at its former warning pixel");
            await AssertNoRetainedActiveEdges(f, pixels, activeEdgePixels, activeSources, role);
        }
        // The locked shot may legitimately finish beyond detection range. A new
        // warning is tested against the original admitted target pose again.
        f.Player.GlobalPosition = Offset + new Vector2(576f, role == RangedWeaponRole.Laser ? 212f : 292f);
        f.Actor.ResetForCheckpoint();
        await f.Until(() => warning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph, 20);
        using (Image pixels = await f.ReadPixels())
        {
            TestAssert.True(f.Submitted.Count == 1 && warning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph,
                "new actual " + role + " warning after reset can replace the withdrawn attack");
            f.AssertCurrentCopies();
        }
        f.Actor.ApplyThreadPull(Vector2.Right * 540f);
        using (Image pixels = await f.ReadPixels())
            TestAssert.True(f.Submitted.Count == 0 && f.Overlay.PresentedCueCount == 0,
                "new post-reset " + role + " source also cancels without renewing an old protected pool entry");
        GD.Print("FOG_RANGED_ROLE_WARNING_ACTIVE_TAIL_CANCEL_PIXELS_OK role=" + role);
    }

    private static async Task AssertNoRetainedActiveEdges(RasterFixture f, Image canceled,
        IReadOnlyList<Vector2I> edgePixels, IReadOnlyList<AttackTelegraphVisual> formerSources, RangedWeaponRole role)
    {
        TestAssert.True(edgePixels.Count > 0 && f.Actor.CurrentFrame.State == RangedWeaponState.Stunned,
            "actual sampled Active edges exist before checking " + role + " cancellation absence");
        Vector2 player = f.Player.GlobalPosition;
        StaticBody2D floor = f.Scope.GetNode<StaticBody2D>("ActualNativeFloor");
        Transform2D nativeFloor = floor.GlobalTransform;
        var visibility = new List<(CanvasItem Item, bool WasVisible)>();
        var muted = new HashSet<ulong>();
        void HideOriginal(CanvasItem item)
        {
            if (!GodotObject.IsInstanceValid(item) || !muted.Add(item.GetInstanceId())) return;
            visibility.Add((item, item.Visible)); item.Hide();
        }
        try
        {
            // Independent absence-only control: hide the protected renderer and
            // actual original attack owners, never ask a model for expected fog.
            // The already captured canceled image stays immutable during this read.
            HideOriginal(f.Overlay);
            HideOriginal(f.Actor);
            HideOriginal(f.Actor.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph"));
            foreach (AttackTelegraphVisual source in formerSources)
                if (GodotObject.IsInstanceValid(source))
                {
                    HideOriginal(source);
                    HideOriginal(source.GetParent() as CanvasItem);
                }
            using Image baseline = await f.ReadPixels();
            TestAssert.True(f.Player.GlobalPosition == player && floor.GlobalTransform == nativeFloor &&
                f.Actor.CurrentFrame.State == RangedWeaponState.Stunned && f.Submitted.Count == 0,
                "attack-free control keeps the same player/native floor/stunned state without manual physics or model calls");
            foreach (Vector2I point in edgePixels)
            {
                Color empty = baseline.GetPixel(point.X, point.Y);
                TestAssert.True(empty.R < .98f && empty.G < .98f && empty.B < .98f &&
                    Mathf.Abs(empty.R - empty.G) < .008f && Mathf.Abs(empty.G - empty.B) < .008f &&
                    Mathf.Abs(empty.A - 1f) < .008f,
                    $"{role} former actual Active edge has a genuinely nonwhite attack-free fog baseline; point={point} baseline={empty}");
                Pixel(canceled, point, empty,
                    "actual " + role + " cancellation removes each just-sampled Active/tail white edge, not only its older warning");
            }
        }
        finally
        {
            foreach ((CanvasItem item, bool wasVisible) in visibility)
                if (GodotObject.IsInstanceValid(item)) item.Visible = wasVisible;
        }
    }

    // Break caught: clearing one canceled owner erases another independently
    // live actual source, or a protected pool retains the canceled first source.
    private static async Task IndependentSourcesSurviveOtherOwnersCancellation(SceneTree tree)
    {
        using var f = await RasterFixture.Create(tree, RangedWeaponRole.Burst, "FogTwoGeneratedRangedOwners");
        f.AddWorldRect("UpperOwnerFloor", Offset + new Vector2(256f, 239f), new Vector2(96f, 20f));
        RangedWeaponEnemy second = f.AddActor(RangedWeaponRole.Burst, Offset + new Vector2(256f, 200f));
        AttackTelegraphVisual firstWarning = f.Actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        AttackTelegraphVisual secondWarning = second.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        await f.Until(() => firstWarning.CurrentFrame.Timeline.HasLockedTarget &&
            secondWarning.CurrentFrame.Timeline.HasLockedTarget, 100);
        TestAssert.True(firstWarning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
            secondWarning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph && second.IsOnFloor() &&
            second.GetNode<Node2D>("Muzzle").GlobalPosition.DistanceTo(Offset + new Vector2(256f, 192f)) < .2f,
            "two actual generated Burst owners independently lock on distinct native support rows");
        // Hand-derived second beam side at 40px: aim=(.9544800,.2982750),
        // normal=(.2982750,-.9544800), muzzle=(256,192), half-width=5.
        // Its floor bottom249 stays 12px above the lower body's top261.
        Vector2I secondPixel = new(296, 199);
        using (Image pixels = await f.ReadPixels())
        {
            TestAssert.Equal(2, f.Submitted.Count, "one current batch contains both independently owned warnings");
            f.AssertCurrentCopies();
            Pixel(pixels, WarningPixel(RangedWeaponRole.Burst), Gold, "first owner's actual warning survives the second batch source");
            Pixel(pixels, secondPixel, Gold, "second independent owner's actual warning has its own protected pixels");
        }
        f.Actor.ApplyThreadPull(Vector2.Zero);
        using (Image pixels = await f.ReadPixels())
        {
            TestAssert.True(firstWarning.CurrentFrame.Timeline.Phase == AttackPhase.None &&
                secondWarning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph && f.Submitted.Count == 1 &&
                second.IsAncestorOf(f.Submitted[0]), "canceling only first real owner preserves second's live warning authority");
            f.AssertCurrentCopies();
            Pixel(pixels, WarningPixel(RangedWeaponRole.Burst), new Color(.90613270f, .90613270f, .90613270f),
                "canceled first owner leaves no stale protected gold in the shared pool");
            Pixel(pixels, secondPixel, Gold, "independently live second warning remains opaque after first owner cancellation");
        }
        GD.Print("FOG_RANGED_TWO_ACTUAL_OWNERS_INDEPENDENT_CANCELLATION_PIXELS_OK");
    }

    private static void AssertWarningGeometryAndSample(RasterFixture f, RangedWeaponRole role, AttackPresentationFrame frame)
    {
        Vector2 muzzle = f.Actor.GetNode<Node2D>("Muzzle").GlobalPosition;
        TestAssert.True(muzzle.DistanceTo(Offset + new Vector2(256f, 292f)) < .2f,
            "actual settled generated muzzle agrees with the hand-checked 29px floor offset");
        Vector2 sample;
        if (role == RangedWeaponRole.Scatter)
        {
            TestAssert.True(frame.Shape.Kind == AttackShapeKind.ConvexPolygon && frame.Shape.Points.Count == 3 &&
                f.Actor.CurrentFrame.AimDirection.DistanceTo(Vector2.Right) < .001f,
                "actual Scatter warning is the clipped three-vertex fan aimed at the independent horizontal target");
            sample = frame.Shape.Points[0] + (frame.Shape.Points[1] - frame.Shape.Points[0]).Normalized() * 40f;
            TestAssert.True(sample.DistanceTo(Offset + new Vector2(294.4505f, 280.9745f)) < .2f,
                "real Scatter world fan edge matches the hand-derived -16 degree sample");
        }
        else
        {
            TestAssert.Equal(AttackShapeKind.Capsule, frame.Shape.Kind, "actual generated warning retains native-clipped Capsule geometry");
            sample = CapsuleNearMuzzle(frame.Shape, 40f);
            Vector2 expected = role switch { RangedWeaponRole.Missile => new(296f, 284f),
                RangedWeaponRole.Laser => new(293.5929f, 277.4479f), _ => new(296f, 287f) };
            TestAssert.True(sample.DistanceTo(Offset + expected) < .2f,
                "real " + role + " capsule side agrees with the independently calculated warning point");
        }
        TestAssert.True(f.PixelAt(sample) == WarningPixel(role), "actual native warning projects onto its hand-selected GPU pixel");
    }

    private static Vector2 CapsuleNearMuzzle(AttackShapeFrame shape, float distance) =>
        new Transform2D(shape.RotationRadians, shape.Origin) *
            new Vector2(shape.Extent.X, -(shape.Extent.Y - shape.Extent.X) + distance);

    private static Vector2I WarningPixel(RangedWeaponRole role) => role switch
    { RangedWeaponRole.Scatter => new(294, 281), RangedWeaponRole.Missile => new(296, 284),
        RangedWeaponRole.Laser => new(294, 277), _ => new(296, 287) };
    // Raster-centre distances: Scatter281.69575787, Burst279.53622306,
    // Missile279.60060801, diagonal Laser289.01989551. All samples are well
    // inside the DeepAbyss field and outside the rightward lamp cone.
    private static Color FoggedGold(RangedWeaponRole role) => role switch
    { RangedWeaponRole.Scatter => new(.76084114f, .59451773f, .26187090f),
        RangedWeaponRole.Missile => new(.76390092f, .59690863f, .26292404f),
        RangedWeaponRole.Laser => new(.74980104f, .58589105f, .25807106f),
        _ => new(.76399424f, .59698154f, .26295616f) };
    private static Color EmptyAfterTargetMove(RangedWeaponRole role)
    {
        float value = role switch { RangedWeaponRole.Scatter => .48819237f, RangedWeaponRole.Missile => .49039063f,
            RangedWeaponRole.Laser => .49090771f, _ => .48826030f };
        return new Color(value, value, value);
    }

    private static void Pixel(Image image, Vector2I point, Color expected, string label)
    {
        TestAssert.True(point.X >= 0 && point.Y >= 0 && point.X < image.GetWidth() && point.Y < image.GetHeight(),
            label + ": chosen native-geometry pixel must be inside the actual raster");
        Color actual = image.GetPixel(point.X, point.Y);
        TestAssert.True(float.IsFinite(actual.R) && float.IsFinite(actual.G) && float.IsFinite(actual.B) &&
            Mathf.Abs(actual.R - expected.R) < .008f && Mathf.Abs(actual.G - expected.G) < .008f &&
            Mathf.Abs(actual.B - expected.B) < .008f && Mathf.Abs(actual.A - 1f) < .008f,
            $"{label}; point={point} expected={expected} actual={actual}");
    }

    private sealed class RasterFixture : IDisposable
    {
        private readonly SceneTree _tree;
        private SubViewport _viewport;
        private PrologueFogCanvas _fog;
        private HeadlampOcclusionSampler _sampler;
        private readonly HashSet<Resource> _resources = new();
        private readonly List<PackedScene> _scenes = new();
        private readonly List<RangedWeaponEnemy> _actors = new();
        private Exception _renderFailure;
        private bool _disposed;
        public Node2D Scope { get; private set; }
        public CharacterBody2D Player { get; private set; }
        public RangedWeaponEnemy Actor { get; private set; }
        public CanvasLayer ProtectedLayer { get; private set; }
        public FogReadabilityOverlay Overlay { get; private set; }
        public HeadlampOcclusionFrame Snapshot { get; private set; }
        public List<AttackTelegraphVisual> Submitted { get; private set; } = new();

        private RasterFixture(SceneTree tree) { _tree = tree; }

        private void Initialize(RangedWeaponRole role, string name)
        {
            var world2D = new World2D(); _resources.Add(world2D);
            _viewport = new SubViewport { Name = name, Size = new Vector2I(768, 512), World2D = world2D,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            _tree.Root.AddChild(_viewport);
            var background = new CanvasLayer { Layer = -10 };
            background.AddChild(new ColorRect { Size = new Vector2(768f, 512f), Color = Colors.White });
            _viewport.AddChild(background);
            Scope = new Node2D { Name = "NativeWorld" }; _viewport.AddChild(Scope);
            var camera = new Camera2D { Position = Offset + new Vector2(384f, 256f), Enabled = true,
                PositionSmoothingEnabled = false };
            Scope.AddChild(camera); camera.MakeCurrent(); camera.ForceUpdateScroll();
            AddWorldRect("ActualNativeFloor", Offset + new Vector2(384f, 339f), new Vector2(768f, 20f));
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Player.Position = Offset + new Vector2(576f, role == RangedWeaponRole.Laser ? 212f : 292f);
            Scope.AddChild(Player);
            Player.RemoveFromGroup("player");
            Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            Player.GetNode<CanvasItem>("VisualRoot").Hide();
            Actor = AddActor(role, Offset + new Vector2(256f, 300f));
            var fogLayer = new CanvasLayer { Layer = 1 };
            _fog = new PrologueFogCanvas { Size = new Vector2(768f, 512f) };
            fogLayer.AddChild(_fog); _viewport.AddChild(fogLayer);
            ProtectedLayer = new CanvasLayer { Layer = 2, FollowViewportEnabled = true, FollowViewportScale = 1f };
            Overlay = new FogReadabilityOverlay(); ProtectedLayer.AddChild(Overlay); _viewport.AddChild(ProtectedLayer);
            _sampler = new HeadlampOcclusionSampler(Scope);
            RenderingServer.FramePreDraw += Present;
        }

        public static async Task<RasterFixture> Create(SceneTree tree, RangedWeaponRole role, string name)
        {
            var f = new RasterFixture(tree);
            try
            {
                f.Initialize(role, name);
                await f.Until(() => f.Actor.IsOnFloor(), 30);
                TestAssert.True(f.Actor.IsOnFloor() && f.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                    "actual generated actor settles naturally on native World support before targeting starts");
                f.Player.AddToGroup("player");
                return f;
            }
            catch { f.Dispose(); throw; }
        }

        public RangedWeaponEnemy AddActor(RangedWeaponRole role, Vector2 position)
        {
            string name = role switch { RangedWeaponRole.Scatter => nameof(RivetScatterGunner),
                RangedWeaponRole.Burst => nameof(BeltBurstGunner), RangedWeaponRole.Missile => nameof(FurnaceMissileGunner),
                _ => nameof(PrismLaserGunner) };
            string path = $"res://scenes/enemies/{name}.tscn";
            PackedScene scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            TestAssert.True(scene != null, "actual generated GPU scene exists: " + path);
            _scenes.Add(scene);
            RangedWeaponEnemy actor = scene.Instantiate<RangedWeaponEnemy>();
            _actors.Add(actor); _resources.Add(actor.Settings);
            FogRangedSavedControllerRuntimeTests.CollectShapes(actor, _resources);
            TestAssert.True(actor.SceneFilePath == path && actor.Role == role && actor.Settings.Role == role,
                "actual ignored-cache generated GPU role/settings identity: " + role);
            actor.Name = name + _actors.Count;
            actor.Position = position;
            actor.ConfigureEncounterEnvelope(new Rect2(Offset, new Vector2(768f, 512f)));
            Scope.AddChild(actor);
            actor.GetNode<CanvasItem>("VisualRoot/WeaponVisual").Hide();
            return actor;
        }

        public StaticBody2D AddWorldRect(string name, Vector2 center, Vector2 size)
        {
            var shape = new RectangleShape2D { Size = size }; _resources.Add(shape);
            var body = new StaticBody2D { Name = name, Position = center,
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
            body.AddChild(new CollisionShape2D { Shape = shape }); Scope.AddChild(body);
            return body;
        }

        private void Present()
        {
            if (_disposed || _renderFailure != null) return;
            try
            {
                Snapshot = _sampler.Capture();
                TestAssert.True(Snapshot.IsComplete && Overlay.GetWorld2D().GetRid() == Scope.GetWorld2D().GetRid(),
                    "independent raster uses one complete current native World2D snapshot");
                _fog.Present(Player.GlobalPosition, Player.GlobalPosition, Vector2.Right,
                    (_viewport.GetFinalTransform() * Scope.GetCanvasTransform()).AffineInverse(), Snapshot);
                Submitted = new List<AttackTelegraphVisual>();
                foreach (RangedWeaponEnemy actor in _actors)
                    if (GodotObject.IsInstanceValid(actor)) actor.AppendAuthorizedAttackLeaves(Submitted);
                Overlay.PresentAttacks(Submitted, Player.GlobalPosition, Snapshot);
            }
            catch (Exception error) { _renderFailure = error; }
        }

        public void AssertCurrentCopies()
        {
            TestAssert.True(_fog.FrameValid && _fog.Visible && ReferenceEquals(Snapshot, Overlay.LastOcclusionFrame),
                "actual fog and protected copy consume the same fresh native pre-draw frame");
            foreach (AttackTelegraphVisual source in Submitted)
                TestAssert.True(source.IsInsideTree() && source.IsVisibleInTree() &&
                    source.GetViewport() == Overlay.GetViewport() &&
                    source.GetWorld2D().GetRid() == Overlay.GetWorld2D().GetRid() &&
                    source.GlobalPosition.DistanceTo(Player.GlobalPosition) <= 620f &&
                    _viewport.GetVisibleRect().HasPoint(source.GetGlobalTransformWithCanvas().Origin) &&
                    Snapshot.CanSee(Player.GlobalPosition, source.GlobalPosition),
                    "every sampled real leaf independently passes current viewport/world/620px/native-LOS admission");
            var copies = Overlay.GetChildren().OfType<AttackTelegraphVisual>()
                .Where(copy => copy.CurrentFrame.Timeline.Phase != AttackPhase.None).ToList();
            FogRangedSavedControllerRuntimeTests.EqualCopies(Submitted, copies, "GPU current real-source batch");
        }

        public void AssertSameLocked(AttackTelegraphVisual source, ulong id) => TestAssert.True(
            source.CurrentFrame.Timeline.AttackId == id && source.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
            source.CurrentFrame.Timeline.HasLockedTarget, "GPU still samples the same actual naturally locked warning");

        public Vector2I PixelAt(Vector2 world)
        {
            Vector2 point = _viewport.GetFinalTransform() * Scope.GetCanvasTransform() * world;
            return new Vector2I(Mathf.RoundToInt(point.X), Mathf.RoundToInt(point.Y));
        }
        public async Task Until(Func<bool> condition, int limit)
        { for (int tick = 0; tick < limit && !condition(); tick++) await Frames(1); }
        public async Task Frames(int count)
        {
            for (int tick = 0; tick < count; tick++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (_renderFailure != null) ExceptionDispatchInfo.Capture(_renderFailure).Throw();
            }
        }
        public async Task<Image> ReadPixels()
        {
            await Frames(2);
            Image pixels = _viewport.GetTexture().GetImage();
            try
            {
                TestAssert.True(pixels != null && !pixels.IsEmpty() && pixels.GetSize() == new Vector2I(768, 512),
                    "actual hardware GPU returns the independent 768x512 native ranged raster");
                return pixels;
            }
            catch { pixels?.Dispose(); throw; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            RenderingServer.FramePreDraw -= Present;
            _sampler?.Dispose();
            if (GodotObject.IsInstanceValid(_viewport)) { _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free(); }
            // Initialization is protected by Create's try/catch too. A failed
            // validation/configuration can leave a newly instantiated node not
            // yet parented under the viewport; free it before its owned resources.
            foreach (RangedWeaponEnemy actor in _actors)
                if (GodotObject.IsInstanceValid(actor)) { actor.GetParent()?.RemoveChild(actor); actor.Free(); }
            if (GodotObject.IsInstanceValid(Player)) { Player.GetParent()?.RemoveChild(Player); Player.Free(); }
            foreach (Resource resource in _resources) if (GodotObject.IsInstanceValid(resource)) resource.Dispose();
            foreach (PackedScene scene in _scenes) scene.Dispose();
        }
    }
}
