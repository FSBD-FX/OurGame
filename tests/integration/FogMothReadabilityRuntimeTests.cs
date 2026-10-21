using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Moth;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

/// <summary>Real packed Moth timing/geometry and GPU pixels, not formal-scene visual acceptance.</summary>
public static class FogMothReadabilityRuntimeTests
{
    private static readonly Vector2 Offset = new(2048, 1408);
    private static readonly Vector2I WarningPixel = new(278, 240);
    private static readonly Color Gold = new(215f / 255f, 168f / 255f, 74f / 255f);
    // Independent raster-centre math: actor(576,300), sample(278.5,240.5),
    // d=303.39166106; .72*smoothstep((d-180)/440)=.13811266366.
    private static readonly Color FoggedGold = new(.72668932f, .56783166f, .25011633f);
    private static readonly Color EmptyFog = new(.86188734f, .86188734f, .86188734f);

    public static async Task Run(SceneTree tree)
    {
        var viewport = new SubViewport { Name = "FogActualMothRaster", Size = new Vector2I(768, 512),
            World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        HeadlampOcclusionSampler sampler = null;
        bool subscribed = false;
        Exception failure = null;
        AttackPresentationFrame submitted = default;
        try
        {
            tree.Root.AddChild(viewport);
            var background = new CanvasLayer { Layer = -10 };
            background.AddChild(new ColorRect { Size = new Vector2(768, 512), Color = Colors.White });
            viewport.AddChild(background);
            var world = new Node2D { Name = "NativeWorld" };
            viewport.AddChild(world);
            var camera = new Camera2D { Position = Offset + new Vector2(384, 256),
                Enabled = true, PositionSmoothingEnabled = false };
            world.AddChild(camera); camera.MakeCurrent(); camera.ForceUpdateScroll();
            var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.Position = Offset + new Vector2(256, 300);
            world.AddChild(player);
            player.AddToGroup("player"); // The saved level builder normally registers this packed Player.
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            player.GetNode<CanvasItem>("VisualRoot").Hide();
            var moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn").Instantiate<GraveLampMoth>();
            moth.Position = Offset + new Vector2(256, 160);
            world.AddChild(moth);
            moth.GetNode<CanvasItem>("VisualRoot/Formal").Hide();
            AttackTelegraphVisual source = moth.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            var fogLayer = new CanvasLayer { Layer = 1 };
            var fog = new PrologueFogCanvas { Size = new Vector2(768, 512) };
            fogLayer.AddChild(fog); viewport.AddChild(fogLayer);
            var protectedLayer = new CanvasLayer { Layer = 2, FollowViewportEnabled = true, FollowViewportScale = 1f };
            var overlay = new FogReadabilityOverlay();
            protectedLayer.AddChild(overlay); viewport.AddChild(protectedLayer);
            sampler = new HeadlampOcclusionSampler(world);
            HeadlampOcclusionFrame snapshot = null;
            void Present()
            {
                if (failure != null) return;
                try
                {
                    snapshot = sampler.Capture();
                    fog.Present(player.GlobalPosition, player.GlobalPosition, Vector2.Right,
                        (viewport.GetFinalTransform() * world.GetCanvasTransform()).AffineInverse(), snapshot);
                    submitted = source.CurrentFrame;
                    overlay.PresentAttacks(new[] { source }, player.GlobalPosition, snapshot);
                }
                catch (Exception error) { failure = error; }
            }
            RenderingServer.FramePreDraw += Present; subscribed = true;
            try
            {
                for (int tick = 0; tick < 100; tick++)
                {
                    await Frames(1);
                    if (source.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph && source.CurrentFrame.Timeline.HasLockedTarget &&
                        source.CurrentFrame.Timeline.PhaseRemainingSeconds <= .28f) break;
                }
                AttackPresentationFrame locked = source.CurrentFrame;
                TestAssert.True(locked.Timeline.Phase == AttackPhase.Telegraph && locked.Timeline.HasLockedTarget &&
                    locked.Timeline.PhaseRemainingSeconds >= .20f && locked.Shape.Kind == AttackShapeKind.Capsule &&
                    locked.Shape.Extent.X == 22f && moth.GlobalPosition.DistanceTo(Offset + new Vector2(256, 160)) < .01f &&
                    locked.Timeline.LockedWorldTarget.DistanceTo(Offset + new Vector2(256, 307)) < .1f &&
                    player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                    "default packed Moth naturally locks the authored22px swept circle and147px downward dive against a living packed Player");
                player.GlobalPosition = Offset + new Vector2(576, 300); // Only fixture displacement, after real target lock.
                using (Image pixels = await Pixels())
                {
                    AssertLocked();
                    Pixel(pixels, WarningPixel, Gold, "real Moth swept-circle progress edge is opaque above fog");
                    // Centre(306.5,240.5), d=275.99003605 =>1-alpha=.91214963937.
                    Pixel(pixels, new Vector2I(306, 240), new Color(.91214964f, .91214964f, .91214964f),
                        "a nearby point outside the22px capsule is still fogged, never an area reveal");
                }
                protectedLayer.Layer = 0;
                using (Image pixels = await Pixels())
                {
                    AssertLocked();
                    Pixel(pixels, WarningPixel, FoggedGold, "same real warning geometry is darkened when its copy is below fog");
                }
                protectedLayer.Layer = 2;
                var wall = new StaticBody2D { Position = Offset + new Vector2(416, 230),
                    CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
                wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(16, 220) } });
                world.AddChild(wall);
                using (Image pixels = await Pixels())
                {
                    AssertLocked();
                    TestAssert.True(!snapshot.CanSee(player.GlobalPosition, source.GlobalPosition) && overlay.PresentedCueCount == 0,
                        "fresh actual16px native wall rejects the still-locked Moth source");
                    Pixel(pixels, WarningPixel, new Color(.23607843f, .18447059f, .08125490f),
                        "Moth warning remains behind the wall, not promoted into the hidden room");
                }
                world.RemoveChild(wall); wall.Free();
                for (int tick = 0; tick < 30 && source.CurrentFrame.Timeline.Phase != AttackPhase.Active; tick++) await Frames(1);
                using (Image pixels = await Pixels())
                {
                    TestAssert.True(submitted.Timeline.AttackId == locked.Timeline.AttackId && submitted.Timeline.Phase == AttackPhase.Active &&
                        submitted.Shape.Kind == AttackShapeKind.Circle && submitted.Shape.Extent == new Vector2(22, 22) &&
                        submitted.Shape.Origin.DistanceTo(moth.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider").GlobalPosition) < .001f,
                        "same real attack switches after movement from locked capsule to the current native Active circle");
                    Vector2 edge = submitted.Shape.Origin - Offset + new Vector2(22, 0);
                    Pixel(pixels, new Vector2I(Mathf.RoundToInt(edge.X), Mathf.RoundToInt(edge.Y)), Colors.White,
                        "current moving Moth Active circle edge is protected above fog");
                    Pixel(pixels, WarningPixel, EmptyFog, "former locked Moth path does not retain stale protected gold");
                }
                for (int tick = 0; tick < 35 && source.CurrentFrame.Timeline.Phase == AttackPhase.Active; tick++) await Frames(1);
                using (Image pixels = await Pixels())
                {
                    TestAssert.Equal(AttackPhase.Recovery, source.CurrentFrame.Timeline.Phase, "actual fixed-duration dive reaches Recovery naturally");
                    TestAssert.Equal(0, overlay.PresentedCueCount, "Recovery clears the protected moving circle");
                    Pixel(pixels, WarningPixel, EmptyFog, "Recovery leaves no old protected path");
                }
                GD.Print("FOG_READABILITY_REAL_MOTH_CAPSULE_ACTIVE_RECOVERY_PIXELS_OK");

                void AssertLocked() => TestAssert.True(submitted.Timeline.AttackId == locked.Timeline.AttackId &&
                    submitted.Timeline.Phase == AttackPhase.Telegraph && submitted.Timeline.HasLockedTarget,
                    "raster still belongs to the same naturally timed real locked Moth warning");
            }
            finally { if (subscribed) { RenderingServer.FramePreDraw -= Present; subscribed = false; } }

            async Task Frames(int count)
            {
                for (int tick = 0; tick < count; tick++)
                {
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }
            async Task<Image> Pixels()
            {
                await Frames(2);
                Image image = viewport.GetTexture().GetImage();
                try
                {
                    TestAssert.True(image != null && !image.IsEmpty() && image.GetSize() == new Vector2I(768, 512),
                        "actual GPU returns the isolated768x512 Moth raster");
                    return image;
                }
                catch { image?.Dispose(); throw; }
            }
        }
        finally
        {
            sampler?.Dispose();
            if (GodotObject.IsInstanceValid(viewport)) { viewport.GetParent()?.RemoveChild(viewport); viewport.Free(); }
        }
    }

    private static void Pixel(Image image, Vector2I point, Color expected, string message)
    {
        TestAssert.True(point.X >= 0 && point.X < image.GetWidth() && point.Y >= 0 && point.Y < image.GetHeight(),
            message + ": selected native-geometry pixel must be inside the actual raster");
        Color actual = image.GetPixel(point.X, point.Y);
        TestAssert.True(float.IsFinite(actual.R) && float.IsFinite(actual.G) && float.IsFinite(actual.B) && Mathf.Abs(actual.R - expected.R) < .008f &&
            Mathf.Abs(actual.G - expected.G) < .008f && Mathf.Abs(actual.B - expected.B) < .008f,
            $"{message}; point={point} expected={expected} actual={actual}");
    }
}
