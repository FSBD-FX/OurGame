using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Tests.Presentation;

// Raster consumer fixture: typed presentation frames are intentional, not simulated gameplay.
public partial class ClawConnectionReadabilityProbe : SceneTree
{
    private string _output;
    private readonly List<string> _failures = new();

    public override async void _Initialize()
    {
        SubViewport viewport = null;
        PlayerFormalVisual detachedFormal = null;
        try
        {
            if (DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A real renderer is required; headless cannot certify pixels.");
            string argument = Array.Find(OS.GetCmdlineUserArgs(), value => value.StartsWith("--output="));
            if (argument == null || !Path.IsPathFullyQualified(argument[9..]))
                throw new ArgumentException("--output=<external absolute directory> is required");
            _output = Path.GetFullPath(argument[9..]);
            string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
            if ((_output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(
                project.TrimEnd('/', '\\') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Raster output must be outside the project.");
            Directory.CreateDirectory(_output);
            viewport = new SubViewport
            {
                Name = "ClawReadability", Size = new Vector2I(512, 384),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            Root.AddChild(viewport);
            viewport.AddChild(new ColorRect { Size = new Vector2(512, 384), Color = new Color("17252b") });
            var player = new Node2D { Name = "Player", Position = new Vector2(256, 240) };
            var visualRoot = new Node2D { Name = "VisualRoot" };
            PlayerFormalVisual formal = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn")
                .Instantiate<PlayerFormalVisual>();
            formal.Name = "Formal";
            var shuttle = new ThreadShuttleVisual { Name = "MechanicalClaw" };
            var line = new ThreadLineVisual { Name = "Line", ZIndex = -1 };
            shuttle.AddChild(line);
            visualRoot.AddChild(formal);
            visualRoot.AddChild(shuttle);
            player.AddChild(visualRoot);
            viewport.AddChild(player);
            await ToSignal(this, SignalName.ProcessFrame);

            foreach (int facing in new[] { 1, -1 })
            foreach (bool upward in new[] { false, true })
            {
                string name = $"{(facing > 0 ? "right" : "left")}-{(upward ? "up" : "horizontal")}";
                Vector2 aim = upward ? Vector2.Up : new Vector2(facing, 0);
                var pose = new PlayerPresentationFrame(new EntityPresentationFrame(EntityVisualState.Idle, facing, true),
                    new PlayerLocomotionFrame(PlayerLocomotionState.Idle, .5f),
                    new PlayerActionFrame(PlayerActionState.ThreadApply, PlayerActionPhase.Contact, .5f, false, aim),
                    SecondaryMotionFrame.Identity);
                for (int settle = 0; settle < 24; settle++) formal.ApplyPlayerFrame(pose);
                // Independent art landmark: the near-arm limb is 24px long; its wrist is
                // at local (0,21). Never ask the cue implementation where it drew itself.
                Vector2 wrist = formal.GetNode<Node2D>("NearArm").ToGlobal(new Vector2(0, 21));
                var frame = new MechanicalClawFrame(1, MechanicalClawState.Reel, MechanicalClawFailure.None,
                    player.GlobalPosition + aim * 170f, 170f, Vector2.Zero, true,
                    SurfaceNormal: -aim, DockPoint: player.GlobalPosition + aim * 170f, Arrived: false,
                    CasterWorldPosition: player.GlobalPosition);
                line.Clear();
                shuttle.Clear();
                using Image off = await Capture(viewport, name + "-off");
                Color background = off.GetPixel(8, 8);
                int bodyPixels = 0;
                for (int y = 175; y < 285; y++)
                for (int x = 205; x < 307; x++)
                    if (Difference(off.GetPixel(x, y), background) > .12f) bodyPixels++;
                Require(bodyPixels > 300, $"{name}: actual formal rig must rasterize, body pixels={bodyPixels}");

                foreach (MechanicalClawState attached in new[] { MechanicalClawState.Latched,
                             MechanicalClawState.Reel, MechanicalClawState.Swing, MechanicalClawState.Boost })
                {
                    shuttle.Present(frame with { State = attached });
                    using Image clearLos = await Capture(viewport, name + "-clear-los-" + attached);
                    int visiblePixels = ChangedPixels(off, clearLos, wrist, cyanOnly: true);
                    Require(visiblePixels >= 60,
                        $"{name}/{attached}: clear LOS wrist ring needs >=60 changed cyan pixels, got {visiblePixels}");
                    Require(!shuttle.ObscuredConnectionVisible,
                        $"{name}/{attached}: an always-visible ring must not misreport LOS as obscured");
                    GD.Print($"CLAW_WRIST_CLEAR_LOS pose={name} state={attached} cyan={visiblePixels}");
                }
                shuttle.Present(frame with { EffectiveVisibility = false });
                using Image on = await Capture(viewport, name + "-on");
                int cyanPixels = ChangedPixels(off, on, wrist, cyanOnly: true);
                // A radius-12 split ring covers about 62px of circumference; a 2px
                // stroke should comfortably expose 60 bright pixels in this annulus.
                Require(cyanPixels >= 60, $"{name}: wrist ring needs >=60 changed cyan pixels, got {cyanPixels}");
                using Image stationary = await Capture(viewport, name + "-stationary");
                int autonomousChanges = ChangedPixels(on, stationary, wrist, cyanOnly: false);
                Require(autonomousChanges == 0, $"{name}: stationary rope must not spin, changed={autonomousChanges}");
                shuttle.Present(frame with { EffectiveVisibility = false, RopeLength = 161f });
                using Image reeled = await Capture(viewport, name + "-reeled");
                int reelChanges = ChangedPixels(on, reeled, wrist, cyanOnly: false);
                Require(reelChanges >= 12, $"{name}: actual rope take-up must rotate the wrist ring, changed={reelChanges}");
                shuttle.Present(frame with { State = MechanicalClawState.Released, EffectiveVisibility = false });
                using Image released = await Capture(viewport, name + "-released");
                int releasePixels = ChangedPixels(off, released, wrist, cyanOnly: true);
                Require(releasePixels == 0, $"{name}: release leaves no extra local cyan pixels, got {releasePixels}");
                shuttle.Clear();
                using Image cleared = await Capture(viewport, name + "-clear");
                int clearPixels = ChangedPixels(off, cleared, wrist, cyanOnly: true);
                Require(clearPixels == 0, $"{name}: Clear leaves no extra local cyan pixels, got {clearPixels}");
                shuttle.Present(frame with { State = MechanicalClawState.Travel, ShuttleDistance = 100f });
                using Image travel = await Capture(viewport, name + "-travel");
                Require(ChangedPixels(off, travel, wrist, cyanOnly: true) == 0,
                    $"{name}: Travel must not show an attached wrist ring");
                foreach (MechanicalClawFrame detached in new[]
                         {
                             frame with { State = MechanicalClawState.Retract, RopeLength = 100f,
                                 EffectiveVisibility = false },
                             frame with { State = MechanicalClawState.Failed,
                                 Failure = MechanicalClawFailure.TargetLost, EffectiveVisibility = false }
                         })
                {
                    // Keep the retracting needle / target-loss mark away from the wrist;
                    // local NoTarget feedback is a different visual contract.
                    shuttle.Present(detached);
                    using Image absent = await Capture(viewport, name + "-" + detached.State);
                    int detachedPixels = ChangedPixels(off, absent, wrist, cyanOnly: true);
                    Require(detachedPixels == 0,
                        $"{name}: {detached.State} leaves no attached wrist ring, cyan={detachedPixels}");
                    GD.Print($"CLAW_WRIST_DETACHED pose={name} state={detached.State} cyan={detachedPixels}");
                }
                shuttle.Present(new ThreadCastFrame(1, ThreadCastState.Apply, ThreadAimMode.Hold,
                    aim, 1, true, ThreadFailureReason.None, 1f, 0f),
                    player.GlobalPosition, frame.AnchorPoint);
                using Image legacy = await Capture(viewport, name + "-legacy");
                Require(ChangedPixels(off, legacy, wrist, cyanOnly: true) == 0,
                    $"{name}: legacy ThreadCast Apply must not inherit the mechanical wrist ring");
                GD.Print($"CLAW_WRIST_RASTER pose={name} wrist={wrist} cyan={cyanPixels} " +
                    $"stationary_changes={autonomousChanges} reel_changes={reelChanges} release={releasePixels} clear={clearPixels}");
            }

            await CheckModelDrivenReeling(viewport, shuttle, player.GlobalPosition,
                formal.GetNode<Node2D>("NearArm").ToGlobal(new Vector2(0, 21)));

            // Keep the previous rig alive: validity alone cannot identify the current
            // hand after the authored Formal path is replaced.
            PlayerPresentationFrame replacementPose = formal.CurrentPlayerFrame;
            detachedFormal = formal;
            visualRoot.RemoveChild(detachedFormal);
            formal = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn")
                .Instantiate<PlayerFormalVisual>();
            formal.Name = "Formal";
            formal.Position = new Vector2(70, 0);
            visualRoot.AddChild(formal);
            for (int settle = 0; settle < 24; settle++) formal.ApplyPlayerFrame(replacementPose);
            Vector2 replacementWrist = formal.GetNode<Node2D>("NearArm").ToGlobal(new Vector2(0, 21));
            var replacementFrame = new MechanicalClawFrame(2, MechanicalClawState.Reel, MechanicalClawFailure.None,
                player.GlobalPosition + Vector2.Up * 170f, 170f, Vector2.Zero, true,
                SurfaceNormal: Vector2.Down,
                DockPoint: player.GlobalPosition + Vector2.Up * 170f,
                Arrived: false,
                CasterWorldPosition: player.GlobalPosition);
            line.Clear();
            shuttle.Clear();
            using Image replacementOff = await Capture(viewport, "replacement-off");
            shuttle.Present(replacementFrame with { EffectiveVisibility = false });
            using Image replacementOn = await Capture(viewport, "replacement-on");
            int replacementPixels = ChangedPixels(replacementOff, replacementOn, replacementWrist, cyanOnly: true);
            Require(replacementPixels >= 60,
                $"replacement rig: ring follows the current wrist while old rig stays alive, cyan={replacementPixels}");
            GD.Print($"CLAW_WRIST_REPLACEMENT wrist={replacementWrist} cyan={replacementPixels} " +
                $"old_alive={GodotObject.IsInstanceValid(detachedFormal)} old_in_tree={detachedFormal.IsInsideTree()}");

            shuttle.Clear();
            int ownedChildren = shuttle.GetChildCount();
            Node ownedCue = shuttle.GetNode<Node2D>("ConnectionCue");
            visualRoot.RemoveChild(shuttle);
            shuttle.RequestReady();
            visualRoot.AddChild(shuttle);
            bool sameOwnership = shuttle.GetChildCount() == ownedChildren &&
                shuttle.GetNode<Node2D>("ConnectionCue") == ownedCue;
            Require(sameOwnership, $"re-entry reuses its owned cue instead of accumulating Draw handlers; " +
                $"children before={ownedChildren} after={shuttle.GetChildCount()}");
            GD.Print($"CLAW_CUE_REENTRY same_ownership={sameOwnership} children={shuttle.GetChildCount()}");
            // Abort before a duplicate child's pending Draw can call through the newer
            // child and generate unrelated engine errors. No frame is yielded here.
            if (!sameOwnership) throw new InvalidOperationException(string.Join("\n", _failures));
            shuttle.Present(replacementFrame with { EffectiveVisibility = false });
            using Image reentered = await Capture(viewport, "reentered-on");
            int reentryPixels = ChangedPixels(replacementOff, reentered, replacementWrist, cyanOnly: true);
            Require(reentryPixels >= 60, $"re-entry retains one working wrist ring, cyan={reentryPixels}");
            if (_failures.Count > 0) throw new InvalidOperationException(string.Join("\n", _failures));
            GD.Print($"CLAW_CONNECTION_READABILITY_OK output={_output}");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError("CLAW_CONNECTION_READABILITY_FAIL: " + exception); Quit(1); }
        finally
        {
            if (GodotObject.IsInstanceValid(detachedFormal)) detachedFormal.Free();
            if (GodotObject.IsInstanceValid(viewport)) viewport.QueueFree();
        }
    }

    // Real deterministic model -> actual raster consumer, not InputMap or motor simulation.
    // Kinematics stay fixed so only rope take-up can change the hand-ring pixels.
    private async Task CheckModelDrivenReeling(SubViewport viewport, ThreadShuttleVisual shuttle,
        Vector2 origin, Vector2 wrist)
    {
        var model = new MechanicalClawModel();
        var target = new MechanicalClawTargetSnapshot(
            RuntimeId: 9,
            Exists: true,
            Material: GrappleSurfaceMaterial.GripStone,
            WorldAnchor: origin + Vector2.Up * 170f,
            HasLineOfSight: true,
            IsLegacyInteraction: false,
            SurfaceNormal: Vector2.Down);
        Vector2 dockPoint = target.WorldAnchor + target.SurfaceNormal *
            MechanicalClawSettings.Default.MinimumRopeLength;
        var cast = new MechanicalClawInput(MechanicalClawCommand.Cast, 0f, false, dockPoint);
        var automatic = new MechanicalClawInput(MechanicalClawCommand.None, 0f, false, dockPoint);
        MechanicalClawFrame frame = model.Step(MechanicalClawFrame.Idle(origin),
            cast, target, 1f / 60f);
        for (int tick = 0; tick < 40 && frame.State != MechanicalClawState.Latched; tick++)
            frame = model.Step(frame, automatic, target, 1f / 60f);
        Require(frame.State == MechanicalClawState.Latched, "model fixture must actually traverse cast to latch");
        float originalLength = frame.RopeLength;
        shuttle.Present(frame);
        float originalRotation = shuttle.SpoolRotation;
        using Image initial = await Capture(viewport, "model-latched");
        for (int tick = 0; tick < 3; tick++)
            frame = model.Step(frame, automatic, target, 1f / 60f);
        shuttle.Present(frame);
        float reeledRotation = shuttle.SpoolRotation;
        using Image reeled = await Capture(viewport, "model-automatic");
        int reelPixels = ChangedPixels(initial, reeled, wrist, cyanOnly: false);
        Require(frame.RopeLength < originalLength && reeledRotation > originalRotation && reelPixels >= 12,
            $"automatic take-up shortens rope and visibly rotates the wrist ring, changed={reelPixels}");
        for (int tick = 0; tick < 60; tick++)
            frame = model.Step(frame, automatic, target, 1f / 60f);
        shuttle.Present(frame);
        float minimumLength = frame.RopeLength;
        using Image minimum = await Capture(viewport, "model-minimum");
        for (int tick = 0; tick < 12; tick++)
        {
            frame = model.Step(frame, automatic, target, 1f / 60f);
            shuttle.Present(frame);
        }
        using Image heldAtMinimum = await Capture(viewport, "model-minimum-automatic");
        Require(minimumLength < originalLength && frame.RopeLength == minimumLength &&
            ChangedPixels(minimum, heldAtMinimum, wrist, cyanOnly: false) == 0,
            "automatic take-up at the rope limit must not animate nonexistent movement");
        MechanicalClawInput arrived = automatic with { Arrived = true };
        frame = model.Step(frame, arrived, target, 1f / 60f);
        Require(frame.State == MechanicalClawState.Swing && frame.Arrived,
            "the model reaches an observable arrived Swing boundary");
        shuttle.Present(frame);
        float arrivedRotation = shuttle.SpoolRotation;
        using Image arrivedImage = await Capture(viewport, "model-arrived");
        for (int tick = 0; tick < 12; tick++)
        {
            frame = model.Step(frame, arrived, target, 1f / 60f);
            shuttle.Present(frame);
        }
        using Image stableSwing = await Capture(viewport, "model-arrived-stable");
        Require(Mathf.IsEqualApprox(arrivedRotation, shuttle.SpoolRotation) &&
            ChangedPixels(arrivedImage, stableSwing, wrist, cyanOnly: false) == 0,
            "arrived Swing keeps the spool and wrist raster stable");

        MechanicalClawFrame attachedAtArrival = frame;
        frame = model.Step(frame, new MechanicalClawInput(MechanicalClawCommand.BoostRelease,
            0f, true, dockPoint), target, 1f / 60f);
        shuttle.Present(frame);
        Require(frame.State == MechanicalClawState.Released && shuttle.IsClawClear,
            "Space release clears the arrived connection immediately");
        MechanicalClawFrame cancelled = model.Step(attachedAtArrival,
            new MechanicalClawInput(MechanicalClawCommand.Cancel, 0f, true, dockPoint),
            target, 1f / 60f);
        shuttle.Present(cancelled);
        Require(cancelled.State == MechanicalClawState.Released && shuttle.IsClawClear,
            "second-click cancellation also clears the arrived connection immediately");
        GD.Print($"CLAW_WRIST_MODEL original={originalLength} minimum={minimumLength} " +
            $"automatic_pixels={reelPixels} arrived_stable=True release_clear=True cancel_clear=True");
    }

    private async Task<Image> Capture(SubViewport viewport, string name)
    {
        for (int frame = 0; frame < 2; frame++)
        {
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
        Image image = viewport.GetTexture().GetImage();
        if (image == null || image.IsEmpty() || image.GetWidth() != 512 || image.GetHeight() != 384)
            throw new InvalidOperationException("The renderer returned no valid viewport raster.");
        Error result = image.SavePng(Path.Combine(_output, name + ".png"));
        if (result != Error.Ok) { image.Dispose(); throw new IOException($"PNG save failed: {result}"); }
        return image;
    }

    private static int ChangedPixels(Image before, Image after, Vector2 wrist, bool cyanOnly)
    {
        int count = 0;
        for (int y = (int)wrist.Y - 16; y <= (int)wrist.Y + 16; y++)
        for (int x = (int)wrist.X - 16; x <= (int)wrist.X + 16; x++)
        {
            float distance = new Vector2(x + .5f, y + .5f).DistanceTo(wrist);
            if (distance < 8f || distance > 15f) continue;
            Color color = after.GetPixel(x, y);
            if (Difference(before.GetPixel(x, y), color) <= .12f) continue;
            if (!cyanOnly || color.G > .65f && color.B > .6f && color.R > .35f && color.G > color.R + .08f)
                count++;
        }
        return count;
    }

    private static float Difference(Color a, Color b) =>
        Mathf.Max(Mathf.Abs(a.R - b.R), Mathf.Max(Mathf.Abs(a.G - b.G), Mathf.Abs(a.B - b.B)));

    private void Require(bool condition, string message)
    {
        if (!condition) _failures.Add(message);
    }
}
