using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Moth;
using Knight.Boss;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Settings;

namespace Knight.Tests.Integration;

/// <summary>
/// Saved-scene controller lifecycle. Uniforms/snapshots alone are not pixel acceptance;
/// actual saved-world pixels are checked by FormalFogVisibilityRuntimeTests.
/// </summary>
public static class HeadlampFogControllerRuntimeTests
{
    public static async Task RunSavedArenaSource(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level, "headlamp-current-arena-source");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await RenderFrames(tree, 5);
            flow.StartNew();
            await RenderFrames(tree, 5);
            // This is an explicitly controlled saved-arena fixture, not a
            // title-to-Boss route, quota admission or checkpoint progression proof.
            BossAdmissionFixturePrerequisites.Apply(level.Progress);
            var sectors = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(sectors.RestoreCheckpoint(PrologueSectorId.LostTemple, new Vector2(6540, 1080)),
                "saved stream adapter accepts its authored LostTemple fixture destination");
            await RenderFrames(tree, 8);
            TestAssert.True(level.CurrentSector == PrologueSectorId.LostTemple &&
                level.Player.GlobalPosition.DistanceTo(new Vector2(6540, 1080)) < 5f,
                "the real stream adapter mounts the saved arena around the same player");
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            level.Player.Velocity = Vector2.Zero;
            BossArenaController arena = level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            FallenBellkeeper boss = arena.CurrentBoss;
            TestAssert.True(boss != null && boss.GetParent() == level.CurrentSectorRoot.GetNode("Gameplay/BossRoot"),
                "saved arena owns the actual current packed Boss under its canonical BossRoot");
            AttackTelegraphVisual source = boss.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            var cues = level.GetNode<FogReadabilityOverlay>("ProtectedCues/Attacks");
            var controller = level.GetNode<PrologueFogController>("FogController");
            arena.BeginEncounter(); // Ordinary public arena activation; not a gate-eligibility assertion.
            for (int tick = 0; tick < 120 && source.CurrentFrame.Timeline.Phase != AttackPhase.Telegraph; tick++)
                await RenderFrames(tree, 1);
            TestAssert.True(arena.IsEncounterActive && source.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
                source.CurrentFrame.Shape.Kind == AttackShapeKind.ConvexPolygon && cues.PresentedCueCount == 1 &&
                ReferenceEquals(controller.LastOcclusionFrame, cues.LastOcclusionFrame),
                "actual arena Boss naturally publishes its real Sweep polygon through the saved collector with the same fresh native snapshot");
            AttackTelegraphVisual copy = null;
            foreach (Node child in cues.GetChildren())
                if (child is AttackTelegraphVisual candidate && candidate.CurrentFrame.Timeline.Phase != AttackPhase.None) copy = candidate;
            AttackPresentationFrame actual = source.CurrentFrame, copied = copy?.CurrentFrame ?? default;
            TestAssert.True(copied.Timeline == actual.Timeline && copied.Shape.Kind == actual.Shape.Kind &&
                copied.Shape.Points.Count == actual.Shape.Points.Count && copied.Shape.Points.Count >= 3,
                "saved protected leaf retains the real Boss timeline and native polygon vertex count");
            for (int index = 0; index < actual.Shape.Points.Count; index++)
                AssertPoint(actual.Shape.Points[index], copied.Shape.Points[index], "same real Boss world polygon vertex");
            ulong formerId = boss.GetInstanceId();
            arena.ResetForCheckpoint();
            TestAssert.True(arena.CurrentBoss != null && arena.CurrentBoss.GetInstanceId() != formerId &&
                !boss.IsInsideTree() && source.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph,
                "actual arena reset replaces the Boss while the detached old leaf still retains its real warning");
            await RenderFrames(tree, 2);
            TestAssert.True(!arena.IsEncounterActive && cues.PresentedCueCount == 0 &&
                arena.CurrentBoss.CurrentFrame.AttackTimeline.Phase == AttackPhase.None,
                "new dormant CurrentBoss cannot inherit the detached former Boss warning");
            AssertGateOpen(level.GetNode<PrologueFogCanvas>("Fog/Canvas"), "current arena source/reset");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_CURRENT_BOSS_RESET_OK pixels=false gate_admission=false formal_visible=true");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    public static async Task Run(SceneTree tree)
    {
        Stage("before-load");
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        Stage("after-instantiate");
        PlayerPresentationCoordinator presentation = null;
        PackedScene originalOverride = null;
        try
        {
            IsolatedPrologueFixture.Configure(level, "headlamp-controller-formal-gate");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            Stage("after-mount");
            await RenderFrames(tree, 5);
            Stage("title-rendered");
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State, "real saved scene begins at Title");

            Node controller = UniqueController(level);
            PrologueFogCanvas canvas = Unique<PrologueFogCanvas>(level, "saved scene fog canvas");
            Node2D world = level.GetNode<Node2D>("WorldRoot");
            CanvasLayer layer = UniqueFogLayer(level, canvas);
            Node fragmentCues = SavedFragmentCues(level);
            TestAssert.True(!world.IsAncestorOf(layer) && !world.IsAncestorOf(controller),
                "the single fog layer/controller cannot retire with a streamed world sector");
            AssertGateClosed(canvas, "Title");
            TestAssert.Equal(0UL, Count(controller), "Title does not submit a gameplay fog frame");
            TestAssert.Equal(0, Read<int>(fragmentCues, "PresentedCueCount"),
                "Title does not reveal mounted quest fragments through the protected layer");
            await AssertFrozen(tree, controller, canvas, "Title");

            presentation = level.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            originalOverride = presentation.Profile.VisualScene;
            flow.StartNew();
            Stage("after-start-new");
            await RenderFrames(tree, 8);
            Stage("playing-rendered");
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "real StartNew enters Playing");
            TestAssert.True(Count(controller) > 0, "late-bound controller submits after level.Player exists");
            AssertSubmitted(level, controller, canvas, presentation, "initial Playing");
            TestAssert.True(Read<int>(fragmentCues, "PresentedCueCount") == 1 &&
                ReferenceEquals(Snapshot(controller), Read<HeadlampOcclusionFrame>(fragmentCues, "LastOcclusionFrame")),
                "saved layer2 receives the one actual nearby entrance fragment and the controller's same fresh native snapshot");
            Stage("before-moth");
            await AssertMountedRealMothLifecycle(tree, level, flow, controller, canvas);
            await AssertLatestReducedFogSettings(tree, level, flow, controller, canvas, presentation);
            await AssertRealKeyboardHalfTurnStaysSmooth(tree, level, canvas);

            Node visualRoot = level.Player.GetNode("VisualRoot");
            PlayerFormalVisual oldFormal = Read<PlayerFormalVisual>(presentation, "ActiveVisual");
            visualRoot.RemoveChild(oldFormal);
            oldFormal.Free();
            PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
            PlayerFormalVisual replacement = formalScene.Instantiate<PlayerFormalVisual>();
            replacement.Name = "FormalReplacement";
            visualRoot.AddChild(replacement);
            presentation.BindVisualRoot(visualRoot);
            TestAssert.True(ReferenceEquals(replacement, Read<PlayerFormalVisual>(presentation, "ActiveVisual")),
                "BindVisualRoot/Profile.Resolve owns the replacement formal authority");
            await AssertLatestPoseAndProjection(tree, level, controller, canvas, presentation, replacement);

            // Resolve creates a distinct Override while the authored replacement is still present.
            // A controller scanning the first Formal child would silently bind the wrong lantern.
            presentation.Profile.VisualScene = formalScene;
            presentation.BindVisualRoot(visualRoot);
            PlayerFormalVisual resolvedOverride = Read<PlayerFormalVisual>(presentation, "ActiveVisual");
            TestAssert.True(!ReferenceEquals(replacement, resolvedOverride) &&
                resolvedOverride.GetParent() == visualRoot && resolvedOverride.Name == "Override",
                "Profile.VisualScene supplies the actual resolved Override instance");
            int formalCount = All<PlayerFormalVisual>(visualRoot).Count;
            await AssertLatestPoseAndProjection(tree, level, controller, canvas, presentation, resolvedOverride);
            await RenderFrames(tree, 3);
            TestAssert.Equal(formalCount, All<PlayerFormalVisual>(visualRoot).Count,
                "reading the active visual never calls Resolve again or creates another Override");

            await AssertNativeSnapshotRefresh(tree, level, controller, canvas);
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open();
            TestAssert.True(tree.Paused && flow.State == PrologueGameFlowState.Paused,
                "real PauseMenu.Open pauses the session");
            await AssertFrozen(tree, controller, canvas, "Paused");
            ulong pausedCount = Count(controller);
            HeadlampOcclusionFrame pausedFrame = Snapshot(controller);
            pause.Execute(ProloguePauseCommand.Resume);
            await RenderFrames(tree, 3);
            TestAssert.True(!tree.Paused && flow.State == PrologueGameFlowState.Playing &&
                Count(controller) > pausedCount && !ReferenceEquals(pausedFrame, Snapshot(controller)),
                "Resume commits a new live native frame instead of retaining the paused snapshot");
            AssertSubmitted(level, controller, canvas, presentation, "resumed Playing");

            await AssertRecovery(tree, level, flow, controller, canvas, presentation);
            TestAssert.Equal(0, presentation.FrameObserverFailureCount,
                "no downstream presentation observer exception was swallowed during controller lifecycle");
            GD.Print("HEADLAMP_FOG_CONTROLLER_FORMAL_GATE_OK");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(presentation)) presentation.Profile.VisualScene = originalOverride;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    private static void Stage(string stage) => GD.Print(
        $"HEADLAMP_CONTROLLER_STAGE stage={stage} usec={Time.GetTicksUsec()} " +
        $"process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()}");

    // Controlled saved-scene integration, not natural-route or pixel acceptance.
    // The actual packed Moth owns physics, timing and geometry throughout.
    private static async Task AssertMountedRealMothLifecycle(SceneTree tree,
        PrologueLevelController level, PrologueGameFlowController flow, Node controller,
        PrologueFogCanvas canvas)
    {
        Node2D cues = level.GetNodeOrNull<Node2D>("ProtectedCues/Attacks");
        TestAssert.True(cues is FogReadabilityOverlay && cues.GetParent() is CanvasLayer
            { Layer: 2, FollowViewportEnabled: true, FollowViewportScale: 1f },
            "saved parent packs its own world-following layer2 attack consumer, not a test-injected overlay");
        PlayerMotor motor = level.Player.GetNode<PlayerMotor>("Motor");
        bool processing = motor.IsPhysicsProcessing();
        Node actors = level.CurrentSectorRoot.GetNode("Gameplay/Actors");
        GraveLampMoth moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        moth.Name = "FogIntegrationRealMoth";
        AttackTelegraphVisual source = null;
        StringName attackLeafName = "AttackTelegraph";
        Vector2 playerPosition = level.Player.GlobalPosition;
        try
        {
            motor.SetPhysicsProcess(false);
            level.Player.Velocity = Vector2.Zero;
            actors.AddChild(moth);
            moth.GlobalPosition = playerPosition + Vector2.Up * 140f;
            source = moth.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            for (int tick = 0; tick < 80; tick++)
            {
                await RenderFrames(tree, 1);
                if (source.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
                    source.CurrentFrame.Timeline.HasLockedTarget) break;
            }
            AttackPresentationFrame locked = source.CurrentFrame;
            TestAssert.True(locked.Timeline.Phase == AttackPhase.Telegraph && locked.Timeline.HasLockedTarget &&
                locked.Shape.Kind == AttackShapeKind.Capsule && locked.Shape.Extent.X == 22f,
                "packed default Moth naturally locks its actual swept-circle warning on the living saved player");
            AssertProtected(AttackPhase.Telegraph, AttackShapeKind.Capsule);
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open();
            await RenderFrames(tree, 3);
            TestAssert.Equal(0, Read<int>(cues, "PresentedCueCount"),
                "real pause clears all saved attack copies while the authoritative Moth remains frozen");
            pause.Execute(ProloguePauseCommand.Resume);
            await RenderFrames(tree, 2);
            AssertProtected(AttackPhase.Telegraph, AttackShapeKind.Capsule);

            // Relocate only the fixture player after lock: avoid real contact and
            // leave the genuine same-ID dive fixed to its original target.
            level.Player.GlobalPosition = playerPosition + Vector2.Right * 100f;
            for (int tick = 0; tick < 60 && source.CurrentFrame.Timeline.Phase != AttackPhase.Active; tick++)
                await RenderFrames(tree, 1);
            TestAssert.True(source.CurrentFrame.Timeline.AttackId == locked.Timeline.AttackId &&
                source.CurrentFrame.Timeline.Phase == AttackPhase.Active,
                "same packed Moth naturally enters Active without manual Step or forged presentation frames");
            AssertProtected(AttackPhase.Active, AttackShapeKind.Circle);

            // Keep enemy/Actors continuously in-tree and preserve the saved
            // guidance paths. Only the authoritative leaf's fixed path changes;
            // a cached leaf or global telegraph scan would wrongly retain it.
            source.Name = "NotTheCanonicalAttackLeaf";
            TestAssert.True(source.IsInsideTree() && source.IsVisibleInTree(),
                "real leaf outside the canonical leaf path is still live and visible: cached/global scanning would wrongly admit it");
            await RenderFrames(tree, 1);
            TestAssert.Equal(0, Read<int>(cues, "PresentedCueCount"),
                "leaving the canonical real attack leaf path removes the prior copy before the source frame expires");
            source.Name = attackLeafName;
            await RenderFrames(tree, 1);
            AssertProtected(AttackPhase.Active, AttackShapeKind.Circle);
            for (int tick = 0; tick < 45 && source.CurrentFrame.Timeline.Phase == AttackPhase.Active; tick++)
                await RenderFrames(tree, 1);
            TestAssert.Equal(AttackPhase.Recovery, source.CurrentFrame.Timeline.Phase,
                "same actual dive naturally closes into Recovery");
            TestAssert.Equal(0, Read<int>(cues, "PresentedCueCount"),
                "Recovery does not retain the old locked capsule or moving Active circle");
            AssertGateOpen(canvas, "actual mounted Moth lifecycle");
            GD.Print("HEADLAMP_FOG_CONTROLLER_MOUNTED_REAL_MOTH_OK pixels=false formal_visible=true");
        }
        finally
        {
            tree.Paused = false;
            if (flow.State == PrologueGameFlowState.Paused)
                level.GetNode<ProloguePauseMenu>("PauseMenu").Execute(ProloguePauseCommand.Resume);
            if (GodotObject.IsInstanceValid(moth))
            {
                moth.GetParent()?.RemoveChild(moth);
                moth.Free();
            }
            level.Player.GlobalPosition = playerPosition;
            if (GodotObject.IsInstanceValid(motor)) motor.SetPhysicsProcess(processing);
        }

        void AssertProtected(AttackPhase phase, AttackShapeKind kind)
        {
            TestAssert.True(Read<int>(cues, "PresentedCueCount") == 1 &&
                ReferenceEquals(Snapshot(controller), Read<HeadlampOcclusionFrame>(cues, "LastOcclusionFrame")),
                "saved controller submits one current Moth warning with the very same fresh native fog snapshot");
            var copies = new List<AttackTelegraphVisual>();
            foreach (Node child in cues.GetChildren())
                if (child is AttackTelegraphVisual copy && copy.CurrentFrame.Timeline.Phase != AttackPhase.None) copies.Add(copy);
            TestAssert.Equal(1, copies.Count, "only one real current warning reaches the protected pool");
            AttackPresentationFrame actual = source.CurrentFrame, copied = copies[0].CurrentFrame;
            TestAssert.True(copied.Timeline.AttackId == actual.Timeline.AttackId &&
                copied.Timeline == actual.Timeline && copied.Shape.Kind == kind && copied.Timeline.Phase == phase &&
                copied.Shape.Origin.DistanceTo(actual.Shape.Origin) < .001f &&
                copied.Shape.Extent.DistanceTo(actual.Shape.Extent) < .001f,
                "protected copy consumes the current real leaf geometry and phase, never a second attack model");
        }
    }

    // Kills a snapped direction or linear-vector interpolation through zero on
    // a real 180-degree keyboard turn. This is shader-input wiring, not pixels
    // or a claim that the still-hidden formal fog has been visually accepted.
    private static async Task AssertRealKeyboardHalfTurnStaysSmooth(SceneTree tree,
        PrologueLevelController level, PrologueFogCanvas canvas)
    {
        PlayerInputReader input = level.Player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = level.Player.GetNode<PlayerMotor>("Motor");
        bool motorWasProcessing = motor.IsPhysicsProcessing();
        void KeyEvent(Key key, bool pressed) => Input.ParseInputEvent(new InputEventKey
        { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
        Vector2 Direction() => Material(canvas).GetShaderParameter("lamp_direction").AsVector2();
        try
        {
            // Fix only fixture locomotion; the saved Input/Claw/controller and
            // actual scheduler keep running. Never call their process methods.
            motor.SetPhysicsProcess(false);
            ulong rightIssueSequence = input.CurrentFrameSequence;
            KeyEvent(Key.A, false); KeyEvent(Key.D, true);
            await RenderFrames(tree, 35);
            TestAssert.True(input.CurrentFrameSequence > rightIssueSequence &&
                input.CurrentFrame.KeyboardActions && input.CurrentFrame.MoveAxis > .9f &&
                Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.A) &&
                input.KeyboardAimDirection == Vector2.Right &&
                Direction().DistanceTo(Vector2.Right) < .02f,
                "actual sampled D settles the submitted lamp direction to the right");

            ulong issueSequence = input.CurrentFrameSequence;
            KeyEvent(Key.D, false); KeyEvent(Key.A, true);
            for (int frame = 0; frame < 8 && !(input.CurrentFrameSequence > issueSequence &&
                input.KeyboardAimDirection == Vector2.Left); frame++) await RenderFrames(tree, 1);
            TestAssert.True(input.CurrentFrameSequence > issueSequence && input.KeyboardAimDirection == Vector2.Left &&
                Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D),
                "a fresh physical A sample selects the opposite direction before checking smoothing");
            Vector2 first = Direction();
            TestAssert.True(Finite(first) && Math.Abs(first.Length() - 1f) < .001f &&
                first.DistanceTo(Vector2.Left) > .25f,
                "the first actual opposite-input submission is unit length and does not snap to the new aim");
            float previousDistance = first.DistanceTo(Vector2.Left);
            Vector2 previousDirection = first;
            bool madeProgress = false;
            for (int frame = 0; frame < 32; frame++)
            {
                await RenderFrames(tree, 1);
                Vector2 current = Direction();
                float distance = current.DistanceTo(Vector2.Left);
                TestAssert.True(Finite(current) && Math.Abs(current.Length() - 1f) < .001f &&
                    distance <= previousDistance + .0005f && current.DistanceTo(previousDirection) < 1.2f,
                    "real half-turn submissions remain unit vectors and approach the selected aim without reversal or a delayed opposite-direction snap");
                madeProgress |= distance < previousDistance - .001f;
                previousDistance = distance;
                previousDirection = current;
            }
            TestAssert.True(madeProgress && previousDistance < .05f,
                "the actual lamp direction finishes the bounded smooth half-turn instead of retaining the old aim");
            AssertGateOpen(canvas, "real keyboard half-turn");
            GD.Print("HEADLAMP_FOG_CONTROLLER_REAL_HALF_TURN_OK formal_visible=true");
        }
        finally
        {
            KeyEvent(Key.A, false); KeyEvent(Key.D, false);
            if (GodotObject.IsInstanceValid(motor)) motor.SetPhysicsProcess(motorWasProcessing);
        }
    }

    private static async Task AssertLatestReducedFogSettings(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, Node controller, PrologueFogCanvas canvas,
        PlayerPresentationCoordinator presentation)
    {
        // Actual settings-to-submission wiring, paired with separate real-world
        // and shader GPU raster tests; this method alone is not pixel acceptance.
        foreach ((bool reduced, bool flashes) in new[] { (true, false), (false, true), (true, true), (false, false) })
        {
            GameSettingsData previous = flow.Settings;
            GameSettingsData next = previous.Normalize();
            next.ReducedFog = reduced;
            next.ReduceFlashes = flashes;
            ulong before = Count(controller);
            HeadlampOcclusionFrame oldFrame = Snapshot(controller);
            flow.ApplySettings(next);
            TestAssert.True(!ReferenceEquals(previous, flow.Settings) && flow.Settings.ReducedFog == reduced &&
                flow.Settings.ReduceFlashes == flashes, "real Flow ApplySettings replaces and preserves independent accessibility options");
            await RenderFrames(tree, 3);
            TestAssert.True(Count(controller) > before && !ReferenceEquals(oldFrame, Snapshot(controller)),
                "each live settings change is followed by a new actual fog submission and native snapshot");
            TestAssert.True(Math.Abs(Material(canvas).GetShaderParameter("opacity_scale").AsSingle() -
                (reduced ? .5f : 1f)) < .0001f, "formal controller reads the latest ReducedFog instead of caching startup settings");
            AssertSubmitted(level, controller, canvas, presentation, "latest accessibility settings");
        }
        GD.Print("HEADLAMP_FOG_CONTROLLER_LATEST_REDUCED_SETTINGS_OK formal_visible=true");
    }

    private static async Task AssertLatestPoseAndProjection(SceneTree tree, PrologueLevelController level,
        Node controller, PrologueFogCanvas canvas, PlayerPresentationCoordinator presentation,
        PlayerFormalVisual formal)
    {
        var camera = level.Player.GetNode<Camera2D>("Camera2D");
        Node2D lantern = formal.GetNode<Node2D>("Lantern");
        Vector2 expectedLamp = default;
        Transform2D beforeProjection = Projection(level);
        Vector2 lateOffset = camera.Offset + new Vector2(41f, -29f);
        Vector2 lateZoom = camera.Zoom * new Vector2(1.25f, .8f);
        bool published = false;
        ulong before = Count(controller);
        void AfterAppliedPose(PlayerPresentationFrame _)
        {
            // Coordinator applies the real pose before publishing. Change both inputs late,
            // so an earlier _Process/previous-frame read cannot satisfy this rendered frame.
            lantern.Position += new Vector2(37f, -23f);
            camera.Offset = lateOffset;
            camera.Zoom = lateZoom;
            camera.ForceUpdateScroll();
            expectedLamp = lantern.GlobalPosition;
            published = true;
        }
        presentation.FramePublished += AfterAppliedPose;
        try { await RenderFrames(tree, 2); }
        finally { presentation.FramePublished -= AfterAppliedPose; }
        TestAssert.True(published && Count(controller) > before,
            "a real presentation publication and subsequent render submission occurred");
        TestAssert.Equal(0, presentation.FrameObserverFailureCount, "late-pose observer did not throw");
        TestAssert.True(expectedLamp.DistanceTo(level.Player.GlobalPosition) > 10f,
            "actual Lantern remains distinct from the actor/body origin");
        AssertPoint(expectedLamp, Material(canvas).GetShaderParameter("lamp_position").AsVector2(),
            "FramePreDraw submits the current resolved lantern after the published pose");
        Transform2D finalProjection = Projection(level);
        TestAssert.True(beforeProjection.X.DistanceTo(finalProjection.X) > .1f ||
            beforeProjection.Y.DistanceTo(finalProjection.Y) > .1f ||
            beforeProjection.Origin.DistanceTo(finalProjection.Origin) > 1f,
            "late camera fixture really changes the final pixel-to-world projection");
        AssertProjection(finalProjection, canvas, "late camera offset/nonuniform zoom");
        AssertSubmitted(level, controller, canvas, presentation, "replacement/Override Playing");
    }

    private static async Task AssertNativeSnapshotRefresh(SceneTree tree, PrologueLevelController level,
        Node controller, PrologueFogCanvas canvas)
    {
        // This deliberately remote native rectangle is a controlled snapshot probe, not a
        // route fixture or a rendered-visibility assertion. No actual sector has these coords.
        Vector2 origin = new(99940f, 100000f), target = new(100060f, 100000f);
        TestAssert.True(Snapshot(controller).CanSee(origin, target), "remote native probe ray starts clear");
        var wall = new StaticBody2D
        {
            Name = "FogControllerNativeSnapshotProbe", Position = new Vector2(100000f, 100000f),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        var shape = new CollisionShape2D
        {
            Name = "Shape", Shape = new RectangleShape2D { Size = new Vector2(16f, 80f) }
        };
        wall.AddChild(shape);
        level.GetNode<Node2D>("WorldRoot").AddChild(wall);
        try
        {
            await RenderFrames(tree, 2);
            HeadlampOcclusionFrame blocked = Snapshot(controller);
            TestAssert.True(blocked.IsComplete && !blocked.CanSee(origin, target),
                "new frame captures a real newly added World rectangle");
            shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            TestAssert.True(!shape.Disabled && !blocked.CanSee(origin, target),
                "queued Disabled does not rewrite the already captured native frame");
            await RenderFrames(tree, 3);
            HeadlampOcclusionFrame opened = Snapshot(controller);
            TestAssert.True(shape.Disabled && opened.IsComplete && !ReferenceEquals(blocked, opened) &&
                opened.CanSee(origin, target) && !blocked.CanSee(origin, target),
                "committed native door opening gets a fresh snapshot while the old snapshot stays blocked");
            AssertGateOpen(canvas, "native snapshot refresh");
        }
        finally
        {
            wall.GetParent()?.RemoveChild(wall);
            wall.Free();
        }
    }

    private static async Task AssertRecovery(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, Node controller, PrologueFogCanvas canvas,
        PlayerPresentationCoordinator presentation)
    {
        bool requested = false, completed = false;
        ulong requestCount = 0, completeCount = 0;
        void Requested(string _)
        {
            requested = true;
            requestCount = Count(controller);
        }
        void Completed(string _)
        {
            completed = true;
            completeCount = Count(controller);
        }
        HeadlampOcclusionFrame before = Snapshot(controller);
        level.RecoveryRequested += Requested;
        level.RecoveryCompleted += Completed;
        try
        {
            // Controlled displacement ensures an unchanged actor-position uniform cannot pass.
            level.Player.GlobalPosition += new Vector2(320f, -96f);
            Vector2 displaced = level.Player.GlobalPosition;
            level.RequestRespawn("FOG CONTROLLER FIXTURE RESET");
            TestAssert.True(requested && flow.State == PrologueGameFlowState.Recovering,
                "real RequestRespawn enters Recovering before deferred checkpoint restoration");
            TestAssert.Equal(requestCount, Count(controller), "Recovering request does not submit a frame");
            // Visibility is committed at FramePreDraw, not synchronously inside
            // RequestRespawn. The interval count below proves no recovering
            // frame was submitted; resumed Playing must obtain a fresh frame.
            for (int frame = 0; frame < 20 && !completed; frame++) await RenderFrames(tree, 1);
            TestAssert.True(completed && flow.State == PrologueGameFlowState.Playing,
                "real checkpoint recovery completes and returns to Playing");
            TestAssert.Equal(requestCount, completeCount,
                "no FramePreDraw submission occurs inside the observed Recovering interval");
            await RenderFrames(tree, 3);
            TestAssert.True(level.Player.GlobalPosition.DistanceTo(displaced) > 100f &&
                Count(controller) > completeCount && !ReferenceEquals(before, Snapshot(controller)),
                "checkpoint relocation resumes with a new pose and fresh native frame");
            AssertSubmitted(level, controller, canvas, presentation, "recovered Playing");
        }
        finally
        {
            level.RecoveryRequested -= Requested;
            level.RecoveryCompleted -= Completed;
        }
    }

    private static async Task AssertFrozen(SceneTree tree, Node controller, PrologueFogCanvas canvas, string phase)
    {
        ulong before = Count(controller);
        await RenderFrames(tree, 3);
        TestAssert.Equal(before, Count(controller), $"{phase} freezes gameplay fog submissions");
        TestAssert.Equal(0, Read<int>(SavedFragmentCues(canvas.GetParent().GetParent()), "PresentedCueCount"),
            $"{phase} clears protected fragment cues without retaining an old gameplay submission");
        AssertGateClosed(canvas, phase);
    }

    private static Node SavedFragmentCues(Node root)
    {
        CanvasLayer layer = root.GetNodeOrNull<CanvasLayer>("ProtectedCues");
        Node cues = root.GetNodeOrNull<Node>("ProtectedCues/Fragments");
        TestAssert.True(layer != null && layer.Layer == 2 && layer.FollowViewportEnabled &&
            Math.Abs(layer.FollowViewportScale - 1f) < .001f && cues is Node2D &&
            cues.GetType().FullName == "Knight.Presentation.Fog.FogFragmentReadabilityOverlay",
            "saved parent scene packs the actual world-following layer2 fragment consumer, not a test-injected overlay");
        return cues;
    }

    private static void AssertSubmitted(PrologueLevelController level, Node controller, PrologueFogCanvas canvas,
        PlayerPresentationCoordinator presentation, string phase)
    {
        HeadlampOcclusionFrame frame = Snapshot(controller);
        TestAssert.True(frame.IsComplete && frame.PhysicsQueries == 0 && canvas.FrameValid,
            $"{phase} has a complete accepted native snapshot without physics query inflation");
        AssertPoint(level.Player.GlobalPosition, Material(canvas).GetShaderParameter("actor_position").AsVector2(),
            $"{phase} submits the current real actor world position");
        AssertPoint(Read<PlayerFormalVisual>(presentation, "ActiveVisual").LanternWorldPosition,
            Material(canvas).GetShaderParameter("lamp_position").AsVector2(),
            $"{phase} uses Coordinator's active formal lantern authority");
        AssertProjection(Projection(level), canvas, phase);
        AssertGateOpen(canvas, phase);
    }

    private static Transform2D Projection(PrologueLevelController level) =>
        (level.GetViewport().GetFinalTransform() * level.GetNode<Node2D>("WorldRoot").GetCanvasTransform()).AffineInverse();

    private static void AssertProjection(Transform2D expected, PrologueFogCanvas canvas, string phase)
    {
        Vector3 x = Material(canvas).GetShaderParameter("screen_to_world_x").AsVector3();
        Vector3 y = Material(canvas).GetShaderParameter("screen_to_world_y").AsVector3();
        TestAssert.True(x.IsFinite() && y.IsFinite() &&
            x.DistanceTo(new Vector3(expected.X.X, expected.Y.X, expected.Origin.X)) <= .002f &&
            y.DistanceTo(new Vector3(expected.X.Y, expected.Y.Y, expected.Origin.Y)) <= .002f,
            $"{phase} maps physical pixels with actual final-transform/canvas inverse including both origins");
    }

    private static void AssertGateClosed(PrologueFogCanvas canvas, string phase) =>
        TestAssert.True(!canvas.Visible && !canvas.IsVisibleInTree(),
            $"{phase}: non-Playing formal fog stays hidden");

    private static void AssertGateOpen(PrologueFogCanvas canvas, string phase) =>
        TestAssert.True(canvas.FrameValid && canvas.Visible && canvas.IsVisibleInTree(),
            $"{phase}: current protected Playing frame displays the formal fog");

    private static ShaderMaterial Material(PrologueFogCanvas canvas)
    {
        TestAssert.True(canvas.Material is ShaderMaterial, "real fog canvas owns its actual shader material");
        return (ShaderMaterial)canvas.Material;
    }

    private static ulong Count(Node controller) => Read<ulong>(controller, "PresentedFrameCount");
    private static HeadlampOcclusionFrame Snapshot(Node controller) =>
        Read<HeadlampOcclusionFrame>(controller, "LastOcclusionFrame");

    private static T Read<T>(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        TestAssert.True(property?.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0 &&
            (property.SetMethod == null || !property.SetMethod.IsPublic),
            $"{target.GetType().Name}.{name} is a public read-only observation surface");
        object value = property.GetValue(target);
        TestAssert.True(value is T, $"{target.GetType().Name}.{name} supplies a nonnull {typeof(T).Name}");
        return (T)value;
    }

    private static Node UniqueController(Node root)
    {
        var found = new List<Node>();
        foreach (Node node in Descendants(root))
            if (node.GetType().Name == "PrologueFogController") found.Add(node);
        TestAssert.Equal(1, found.Count,
            "actual saved PrologueLevel mounts exactly one PrologueFogController (no test-injected controller)");
        return found[0];
    }

    private static CanvasLayer UniqueFogLayer(Node root, PrologueFogCanvas canvas)
    {
        var found = new List<CanvasLayer>();
        foreach (CanvasLayer layer in All<CanvasLayer>(root)) if (layer.Layer == 1) found.Add(layer);
        TestAssert.True(found.Count == 1 && found[0].IsAncestorOf(canvas),
            "saved scene has one layer-1 fog canvas between the world and HUD, not one per sector");
        return found[0];
    }

    private static T Unique<T>(Node root, string label) where T : Node
    {
        List<T> found = All<T>(root);
        TestAssert.Equal(1, found.Count, label + " is unique");
        return found[0];
    }

    private static List<T> All<T>(Node root) where T : Node
    {
        var found = new List<T>();
        foreach (Node node in Descendants(root)) if (node is T typed) found.Add(typed);
        return found;
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void AssertPoint(Vector2 expected, Vector2 actual, string message) =>
        TestAssert.True(Finite(actual) && expected.DistanceTo(actual) <= .002f,
            $"{message}; expected={expected} actual={actual}");

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static async Task RenderFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
}
