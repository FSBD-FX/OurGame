using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Ui;

namespace Knight.Tests.Integration;

/// <summary>
/// Actual W/K, native collision-hit selection and HUD GPU pixels in isolated fog.
/// Does not enable formal fog, present a reticle directly, or inject claw frames.
/// </summary>
public static class FogClawReadabilityRuntimeTests
{
    private static readonly Vector2 WorldOffset = new(2048f, 1408f);
    private static readonly Vector2 NativeHit = WorldOffset + new Vector2(256f, 80f);
    private static readonly Vector2I DiamondPixel = new(249, 87);
    private static readonly Color Cyan = new(114f / 255f, 217f / 255f, 209f / 255f);
    // Pixel centre (249.5,87.5), actor (256,400), d=312.56759269.
    // The independently hand-checked 180/620/.72 deep-abyss profile, outside
    // the leftward lamp cone, gives alpha=.15669171. No fog model computes
    // either expected pixel, and the beam ends at y80, above this sample.
    private static readonly Color FoggedCyan = new(.3770084f, .7176388f, .6911821f);
    private static readonly Color EmptyFog = new(.8433083f, .8433083f, .8433083f);
    private const float PixelTolerance = .008f;

    public static async Task Run(SceneTree tree)
    {
        await NativePreviewAndCastStayReadableAboveFog(tree);
        await DetachedPlayerCannotLeaveACachedClawCue(tree);
        GD.Print("FOG_READABILITY_CLAW_OK");
    }

    // Kills profile-centre selection, cast/preview disagreement and a HUD cue
    // rendered beneath the fog. Expectations are literal native hit/pixels.
    private static async Task NativePreviewAndCastStayReadableAboveFog(SceneTree tree)
    {
        using var fixture = await ClawFixture.Create(tree, "FogNativeKeyboardClaw");
        await fixture.PreviewFromRealW();
        ulong selectedId = fixture.Beam.GetInstanceId();
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertNativePreview();
            fixture.AssertReticleAtNativeHit();
            AssertPixel(pixels, Cyan, "the actual W preview diamond remains opaque cyan above fog");
        }

        fixture.HudLayer.Layer = 0;
        try
        {
            using Image pixels = await fixture.ReadPixels();
            fixture.AssertNativePreview();
            fixture.AssertReticleAtNativeHit();
            AssertPixel(pixels, FoggedCyan,
                "placing this same real HUD beneath layer1 fog actually darkens its diamond pixel");
        }
        finally { fixture.HudLayer.Layer = 5; }

        using (Image pixels = await fixture.ReadPixels())
            AssertPixel(pixels, Cyan, "restoring only HUD layer5 restores the same real preview pixel");

        await fixture.CastShownPreviewFromRealK(selectedId);
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameNativeCast(selectedId);
            fixture.AssertReticleAtNativeHit();
            AssertPixel(pixels, Cyan, "the real K locked native hit, not the remote profile centre, stays readable");
        }

        // A second fresh K is the real cancellation route. W release means
        // this fixture's later Idle faces right, with no candidate on that ray.
        // Do not require every Idle in a real level to have no valid preview.
        ulong castId = fixture.Claw.CurrentFrame.CastId;
        bool sawCancel = false;
        void ObserveCancel(MechanicalClawFrame frame, GrappleSurface2D target) =>
            sawCancel |= frame.CastId == castId && frame.ReleaseKind == MechanicalClawReleaseKind.Cancel;
        fixture.Claw.ClawFrameChanged += ObserveCancel;
        try
        {
            fixture.KeyEvent(Key.K, true);
            for (int frame = 0; frame < 6 && !sawCancel; frame++) await fixture.PhysicsFrames(1);
            TestAssert.True(sawCancel,
                "fresh second K publishes actual same-cast cancellation, not a synthetic None frame");
        }
        finally
        {
            fixture.KeyEvent(Key.K, false);
            fixture.KeyEvent(Key.W, false);
            fixture.Claw.ClawFrameChanged -= ObserveCancel;
        }
        using (Image pixels = await fixture.ReadPixels())
        {
            TestAssert.True(!fixture.Claw.KeyboardPreview.Snapshot.Exists,
                "right-facing cancel/None fixture has no newly eligible candidate to replace the old cue");
            TestAssert.True(!fixture.Reticle.Visible && !fixture.Reticle.CurrentFrame.Visible,
                "a real unqualified Released/Retract or empty Idle cannot retain the old cue");
            AssertPixel(pixels, EmptyFog, "real cancel and empty preview leave only fog at the old diamond pixel");
        }
        GD.Print("FOG_READABILITY_NATIVE_CLAW_PREVIEW_CAST_PIXELS_OK");
    }

    // Kills HUD caching an instance-valid but detached Input/Claw. The original
    // player and its native components remain referenced; no manual cue hide.
    private static async Task DetachedPlayerCannotLeaveACachedClawCue(SceneTree tree)
    {
        using var fixture = await ClawFixture.Create(tree, "FogDetachedKeyboardClaw");
        await fixture.PreviewFromRealW();
        ulong selectedId = fixture.Beam.GetInstanceId();
        await fixture.CastShownPreviewFromRealK(selectedId);
        using (Image pixels = await fixture.ReadPixels())
        {
            fixture.AssertSameNativeCast(selectedId);
            fixture.AssertReticleAtNativeHit();
            AssertPixel(pixels, Cyan, "the same source has a genuine visible cue before player detach");
        }

        fixture.Scope.RemoveChild(fixture.Player);
        TestAssert.True(GodotObject.IsInstanceValid(fixture.Player) &&
            GodotObject.IsInstanceValid(fixture.Reader) && GodotObject.IsInstanceValid(fixture.Claw) &&
            !fixture.Player.IsInsideTree() && !fixture.Reader.IsInsideTree() && !fixture.Claw.IsInsideTree() &&
            fixture.Hud.IsInsideTree() && fixture.Reticle.IsInsideTree(),
            "detach keeps original native source references while the actual HUD remains live");
        using (Image pixels = await fixture.ReadPixels())
        {
            TestAssert.True(!fixture.Reticle.Visible && !fixture.Reticle.CurrentFrame.Visible,
                "live HUD rejects an instance-valid detached player's cached claw cue");
            AssertPixel(pixels, EmptyFog, "detached player leaves no stale protected cyan pixel above fog");
        }
        GD.Print("FOG_READABILITY_NATIVE_CLAW_PLAYER_DETACH_CLEAR_OK");
    }

    private static void AssertPixel(Image pixels, Color expected, string message)
    {
        Color actual = pixels.GetPixel(DiamondPixel.X, DiamondPixel.Y);
        TestAssert.Near(expected.R, actual.R, PixelTolerance, message + " R");
        TestAssert.Near(expected.G, actual.G, PixelTolerance, message + " G");
        TestAssert.Near(expected.B, actual.B, PixelTolerance, message + " B");
        TestAssert.Near(1f, actual.A, PixelTolerance, message + " alpha");
    }

    private sealed class ClawFixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly SubViewport _viewport;
        private readonly Camera2D _camera;
        private readonly PrologueFogCanvas _fog;
        private readonly Vector2 _actorPosition = WorldOffset + new Vector2(256f, 400f);
        private HeadlampOcclusionSampler _sampler;
        private Exception _renderFailure;
        private bool _subscribed;
        private bool _disposed;
        public Node2D Scope { get; }
        public CharacterBody2D Player { get; }
        public PlayerInputReader Reader { get; }
        public MechanicalClawController Claw { get; }
        public GrappleSurface2D Beam { get; }
        public CanvasLayer HudLayer { get; }
        public PrologueFormalHud Hud { get; }
        public PrologueAimReticle Reticle { get; }

        private ClawFixture(SceneTree tree, string name)
        {
            _tree = tree;
            _viewport = new SubViewport
            {
                Name = name, Size = new Vector2I(512, 512), World2D = new World2D(),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            var background = new CanvasLayer { Name = "WhiteBackground", Layer = -10 };
            background.AddChild(new ColorRect
            {
                Size = new Vector2(512, 512), Color = Colors.White,
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            _viewport.AddChild(background);
            Scope = new Node2D { Name = "NativeWorld" };
            _viewport.AddChild(Scope);
            _camera = new Camera2D
            {
                Name = "ActualCamera", Position = WorldOffset + new Vector2(256f, 256f),
                Enabled = true, PositionSmoothingEnabled = false, Zoom = Vector2.One
            };
            Scope.AddChild(_camera);

            var beamBody = new StaticBody2D
            {
                Name = "NativeTempleBeam", Position = WorldOffset + new Vector2(256f, 72f),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
            };
            beamBody.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(64f, 16f) } });
            Beam = new GrappleSurface2D
            {
                Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.TempleMetal,
                UseCollisionHitPoint = true, AnchorOffset = new Vector2(999f, 999f)
            };
            beamBody.AddChild(Beam);
            Scope.AddChild(beamBody);

            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Name = "Player";
            Player.Position = _actorPosition;
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Scope.AddChild(Player);
            Reader = Player.GetNode<PlayerInputReader>("Input");
            Claw = Player.GetNode<MechanicalClawController>("MechanicalClaw");

            var fogLayer = new CanvasLayer { Name = "IsolatedVisibleFog", Layer = 1 };
            _fog = new PrologueFogCanvas { Size = new Vector2(512, 512) };
            fogLayer.AddChild(_fog);
            _viewport.AddChild(fogLayer);
            HudLayer = new CanvasLayer { Name = "ActualHudLayer", Layer = 5 };
            Hud = new PrologueFormalHud
            {
                Name = "PrologueFormalHud", Size = new Vector2(512, 512),
                InputPath = "../../NativeWorld/Player/Input",
                PlayerPresentationPath = "../../NativeWorld/Player/Presentation",
                MechanicalClawPath = "../../NativeWorld/Player/MechanicalClaw"
            };
            // No other HUD controls overlap (256,80). This slice does not
            // redesign the existing protected-region avoidance policy.
            Reticle = new PrologueAimReticle { Name = "AimReticle", MouseFilter = Control.MouseFilterEnum.Ignore };
            Hud.AddChild(Reticle);
            HudLayer.AddChild(Hud);
            _viewport.AddChild(HudLayer);
        }

        public static async Task<ClawFixture> Create(SceneTree tree, string name)
        {
            var fixture = new ClawFixture(tree, name);
            try
            {
                fixture.KeyEvent(Key.W, false);
                fixture.KeyEvent(Key.K, false);
                tree.Root.AddChild(fixture._viewport);
                // Ready enables native physics processing; freeze only Motor
                // immediately afterwards, before the first fixture physics tick.
                fixture.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
                fixture._camera.MakeCurrent();
                fixture._camera.ForceUpdateScroll();
                fixture._sampler = new HeadlampOcclusionSampler(fixture.Scope);
                RenderingServer.FramePreDraw += fixture.PresentNativeFog;
                fixture._subscribed = true;
                await fixture.PhysicsFrames(2);
                int hp = fixture.Player.GetNode<HealthComponent>("Health").CurrentHealth;
                bool motor = fixture.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing();
                bool input = fixture.Reader.IsPhysicsProcessing();
                bool claw = fixture.Claw.IsPhysicsProcessing();
                TestAssert.True(hp > 0 && !motor && input && claw,
                    $"actual saved Player is living; only Motor is frozen while Input/Claw run native physics: hp={hp}, motor={motor}, input={input}, claw={claw}");
                return fixture;
            }
            catch { fixture.Dispose(); throw; }
        }

        public void KeyEvent(Key key, bool pressed)
        {
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
            Input.FlushBufferedEvents();
        }

        public async Task PreviewFromRealW()
        {
            KeyEvent(Key.W, true);
            await PhysicsFrames(3);
            TestAssert.True(Input.IsPhysicalKeyPressed(Key.W) && Reader.CurrentFrame.KeyboardActions &&
                Reader.KeyboardAimDirection.DistanceTo(Vector2.Up) < .001f && !Reader.HasObservedMouseMotion,
                "actual physical W creates a native upward keyboard sample without mouse input");
            AssertNativePreview();
        }

        public void AssertNativePreview()
        {
            MechanicalClawTargetResolution actual = Claw.KeyboardPreview;
            TestAssert.True(actual.Surface == Beam && actual.Snapshot.Exists && actual.Snapshot.HasLineOfSight &&
                !actual.Snapshot.IsLegacyInteraction && actual.Snapshot.RuntimeId == Beam.GetInstanceId(),
                "real keyboard preview selects the sole available native nonlegacy beam with LOS");
            TestAssert.True(actual.Snapshot.WorldAnchor.DistanceTo(NativeHit) < .01f &&
                actual.Snapshot.SurfaceNormal.DistanceTo(Vector2.Down) < .001f,
                "native ray hits literal beam underside (256,80), not its deliberately remote profile anchor");
        }

        public async Task CastShownPreviewFromRealK(ulong selectedId)
        {
            AssertNativePreview();
            ulong issue = Reader.CurrentFrameSequence;
            TestAssert.True(!Input.IsPhysicalKeyPressed(Key.K), "cast begins with a released real K key");
            KeyEvent(Key.K, true);
            try
            {
                for (int frame = 0; frame < 30; frame++)
                {
                    await PhysicsFrames(1);
                    if (Claw.CurrentFrame.State != MechanicalClawState.Travel) continue;
                    TestAssert.True(Reader.CurrentFrameSequence > issue,
                        "real K locks only after a fresh native input sample");
                    AssertSameNativeCast(selectedId);
                    return;
                }
                throw new InvalidOperationException("Assertion failed: real K did not naturally reach Travel within30 native physics samples");
            }
            finally { KeyEvent(Key.K, false); }
        }

        public void AssertSameNativeCast(ulong selectedId)
        {
            MechanicalClawFrame actual = Claw.CurrentFrame;
            TestAssert.True(actual.CastId != 0UL && actual.LockedTargetRuntimeId == selectedId &&
                selectedId == Beam.GetInstanceId() && Claw.CurrentTarget == Beam && actual.EffectiveVisibility &&
                actual.State is MechanicalClawState.Travel or MechanicalClawState.Latched or
                    MechanicalClawState.Reel or MechanicalClawState.Swing,
                "same genuine K cast retains the originally shown native beam and a live visible locked phase");
            TestAssert.True(actual.AnchorPoint.DistanceTo(NativeHit) < .01f,
                "real cast retains the literal native collision hit displayed before K");
        }

        public void AssertReticleAtNativeHit()
        {
            Vector2 projected = _viewport.GetCanvasTransform() * NativeHit;
            TestAssert.True(projected.DistanceTo(new Vector2(256f, 80f)) < .01f,
                "actual camera maps the offset native hit to the hand-checked raw HUD point (256,80)");
            TestAssert.True(Reticle.Visible && Reticle.CurrentFrame.Visible &&
                Reticle.CurrentFrame.State == PrologueAimReticleState.Grippable &&
                Reticle.CurrentFrame.ScreenPosition.DistanceTo(projected) < .01f &&
                (Reticle.Position + Reticle.Size * .5f).DistanceTo(projected) < .01f,
                "production HUD displays exactly the native selected hit, without a fixture-presented or displaced cue");
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
                TestAssert.True(pixels != null && !pixels.IsEmpty() && pixels.GetSize() == new Vector2I(512, 512),
                    "real GPU readback supplies this fixture's 512x512 pixels");
                return pixels;
            }
            catch { pixels?.Dispose(); throw; }
        }

        private void PresentNativeFog()
        {
            if (_disposed || _renderFailure != null) return;
            try
            {
                HeadlampOcclusionFrame native = _sampler.Capture();
                TestAssert.True(native.IsComplete && native.Shapes.Count == 1,
                    "each rendered frame uses a fresh complete native beam rectangle snapshot");
                Transform2D pixelsToWorld = (_viewport.GetFinalTransform() * Scope.GetCanvasTransform()).AffineInverse();
                _fog.Present(_actorPosition, _actorPosition, Vector2.Left, pixelsToWorld, native);
                TestAssert.True(_fog.FrameValid, "actual fog accepts this current native snapshot and projection");
            }
            catch (Exception exception) { _renderFailure = exception; }
        }

        private void ThrowRenderFailure()
        {
            if (_renderFailure != null) ExceptionDispatchInfo.Capture(_renderFailure).Throw();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_subscribed) RenderingServer.FramePreDraw -= PresentNativeFog;
            KeyEvent(Key.K, false);
            KeyEvent(Key.W, false);
            _sampler?.Dispose();
            // Detached player is no longer owned by the viewport, but remains
            // our original native instance; free it separately exactly once.
            if (GodotObject.IsInstanceValid(Player) && Player.GetParent() == null) Player.Free();
            if (!GodotObject.IsInstanceValid(_viewport)) return;
            _viewport.GetParent()?.RemoveChild(_viewport);
            _viewport.Free();
        }
    }
}
