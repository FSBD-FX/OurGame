using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class MechanicalClawRuntimeTests
{
    private const float Frame = 1f / 60f;

    public static async Task Run(SceneTree tree)
    {
        await ResolverUsesRealPhysicsForRangeAssistMaterialsAndObstruction(tree);
        await MoveUpAndMoveDownDoNotChangeAutomaticTakeUp(tree);
        await ScheduledAttachedSpacePublishesAtomicReleaseAndSuppressesMotorJump(tree);
        await ScheduledBoostKeepsTheRenderedAttachedAction(tree);
        await AutomaticBoostReleasePreservesUpwardInertia(tree);
        await RealClawTakeoverEndsBurnWithoutRefund(tree);
        await CancelledTravelPreservesOrdinaryShortHop(tree);
        await RuntimeTravelsLatchesReelsMovesReleasesAndCleansVisuals(tree);
        await AttachedOcclusionReleasesBeforePullingThroughTheBlocker(tree);
        await AttachedVisibilityBoundariesUseOnlyTheSafeConnectionCue(tree);
        await NonfatalDamagePreservesContinuousInputAndConsumesOneShotEdges(tree);
        await AutomaticPhysicsLongSwingAndReleaseStayVelocityBounded(tree);
        await AutomaticInputMapQRetractsInDeclaredPhysicsOrder(tree);
        await AbilityShieldAndGrappleScalesComposeInRealMotor(tree);
        await TargetUnloadFailsClosedAndClearsTheSingleClaw(tree);
        await NoTargetFailureUsesOnlyTheLocalCasterMarker(tree);
    }

    private static async Task MoveUpAndMoveDownDoNotChangeAutomaticTakeUp(SceneTree tree)
    {
        float baseline = await MeasureTakeUp(default, "NoVerticalInput");
        float moveUp = await MeasureTakeUp(InputActions.MoveUp, "MoveUp");
        float moveDown = await MeasureTakeUp(InputActions.MoveDown, "MoveDown");

        TestAssert.True(baseline > 0f,
            "the runtime fixture observes automatic rope take-up without W or S");
        TestAssert.Near(baseline, moveUp, .001f,
            "the real input reader's W action cannot become rope control");
        TestAssert.Near(baseline, moveDown, .001f,
            "the real input reader's S action cannot become rope control");

        async Task<float> MeasureTakeUp(StringName action, string suffix)
        {
            RuntimeFixture fixture = await CreateRuntimeFixture(tree,
                "MechanicalClawAutoTakeUp" + suffix, 220f);
            try
            {
                Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                    mouseWorld: fixture.Surface.WorldAnchor));
                Step(fixture, Input(primaryReleased: true,
                    mouseWorld: fixture.Surface.WorldAnchor));
                for (int index = 0; index < 120 &&
                    fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                    Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
                TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                    $"{suffix} fixture reaches the same attached boundary");

                float before = fixture.Claw.CurrentFrame.RopeLength;
                if (action != default) Godot.Input.ActionPress(action);
                fixture.Input._PhysicsProcess(Frame);
                fixture.Claw._PhysicsProcess(Frame);
                PlayerInputFrame sampled = fixture.Input.CurrentFrame;
                float takeUp = before - fixture.Claw.CurrentFrame.RopeLength;
                TestAssert.Near(0f, sampled.MoveAxis, .0001f,
                    $"{suffix} keeps horizontal SwingAxis neutral");
                if (action != default)
                    TestAssert.True(Mathf.Abs(sampled.Aim.Y) > .9f,
                        $"{suffix} is actually sampled through PlayerInputReader's vertical action");
                return takeUp;
            }
            finally
            {
                if (action != default) Godot.Input.ActionRelease(action);
                await Dispose(tree, fixture.Root);
            }
        }
    }

    private static async Task ResolverUsesRealPhysicsForRangeAssistMaterialsAndObstruction(SceneTree tree)
    {
        var root = new Node2D { Name = "MechanicalClawResolverFixture" };
        var caster = new CharacterBody2D
        {
            Name = "Caster",
            Position = Vector2.Zero,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        caster.AddChild(new CollisionShape2D
        {
            Name = "OwnedWorldCollider",
            Shape = new CircleShape2D { Radius = 18f }
        });
        var ownedTargetCollider = new Area2D
        {
            Name = "OwnedThreadTargetCollider",
            Position = new Vector2(90f, 0f),
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        ownedTargetCollider.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 14f }
        });
        caster.AddChild(ownedTargetCollider);
        root.AddChild(caster);
        GrappleSurface2D at620 = Surface("At620", new Vector2(620f, 0f),
            GrappleSurfaceMaterial.DedicatedAnchor);
        GrappleSurface2D pointAssist = Surface("PointAssist", new Vector2(-180f, 120f),
            GrappleSurfaceMaterial.GripStone);
        root.AddChild(at620);
        root.AddChild(pointAssist);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        try
        {
            var resolver = new MechanicalClawTargetResolver(MechanicalClawSettings.Default);
            AssertResolved(resolver.Resolve(caster, at620.WorldAnchor), at620,
                MechanicalClawFailure.None, "an exact 620 px point anchor remains inclusively inside range");
            TestAssert.True(resolver.Resolve(caster, at620.WorldAnchor).Snapshot.HasLineOfSight,
                "caster-owned World and ThreadTarget colliders are excluded from the target ray");
            TestAssert.True(!MechanicalClawTargetResolver.TryGetLineOcclusion(caster, at620,
                Vector2.Zero, at620.WorldAnchor, out _, out _),
                "visual occlusion excludes the caster, its owned collider, and the point target collider");
            MechanicalClawTargetResolution edge = resolver.Resolve(caster,
                pointAssist.WorldAnchor + Vector2.Down * 36f);
            TestAssert.Equal(pointAssist, edge.Surface,
                "the authored 36-pixel assist boundary remains inclusive");
            TestAssert.True(edge.Snapshot.SurfaceNormal.Dot(
                    new Vector2(180f, -120f).Normalized()) > .9999f,
                "point-anchor admission saves the normalized direction from anchor to caster");
            MechanicalClawTargetResolution outside = resolver.Resolve(caster,
                pointAssist.WorldAnchor + Vector2.Down * 36.01f);
            TestAssert.True(outside.Surface == null && !outside.Snapshot.Exists,
                "36.01 pixels cannot select a surface even when the physics shapes overlap");
            at620.QueueFree();
            pointAssist.QueueFree();
            await Frames(tree, 2);

            async Task<MechanicalClawTargetResolution> ResolveTerrain(string name, Vector2 bodyPosition,
                Vector2 size, Vector2 casterPosition, Vector2 cursor, GrappleSurfaceMaterial material)
            {
                StaticBody2D terrain = Terrain(name, bodyPosition, size, material);
                root.AddChild(terrain);
                caster.GlobalPosition = casterPosition;
                await Frames(tree, 2);
                MechanicalClawTargetResolution resolution = resolver.Resolve(caster, cursor);
                terrain.QueueFree();
                await Frames(tree, 2);
                return resolution;
            }

            MechanicalClawTargetResolution wall = await ResolveTerrain("WallFace",
                new Vector2(200f, 0f), new Vector2(20f, 120f), Vector2.Zero,
                new Vector2(400f, 0f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(wall, new Vector2(190f, 0f), MechanicalClawFailure.None,
                "wall face");
            TestAssert.Equal(Vector2.Left, wall.Snapshot.SurfaceNormal,
                "wall admission retains the real outward physics normal");

            MechanicalClawTargetResolution ceiling = await ResolveTerrain("CeilingUnderside",
                new Vector2(0f, -200f), new Vector2(140f, 20f), Vector2.Zero,
                new Vector2(0f, -400f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(ceiling, new Vector2(0f, -190f), MechanicalClawFailure.None,
                "ceiling underside");
            TestAssert.Equal(Vector2.Down, ceiling.Snapshot.SurfaceNormal,
                "ceiling admission retains the real outward physics normal");

            MechanicalClawTargetResolution platform = await ResolveTerrain("PlatformUnderside",
                new Vector2(200f, -200f), new Vector2(160f, 20f), new Vector2(200f, 0f),
                new Vector2(200f, -400f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(platform, new Vector2(200f, -190f), MechanicalClawFailure.None,
                "platform underside");

            MechanicalClawTargetResolution platformEdge = await ResolveTerrain("PlatformEdge",
                new Vector2(300f, -100f), new Vector2(100f, 20f), new Vector2(0f, -100f),
                new Vector2(600f, -100f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(platformEdge, new Vector2(250f, -100f), MechanicalClawFailure.None,
                "platform edge");

            MechanicalClawTargetResolution metal = await ResolveTerrain("TempleMetalTerrain",
                new Vector2(210f, 0f), new Vector2(20f, 100f), Vector2.Zero,
                new Vector2(400f, 0f), GrappleSurfaceMaterial.TempleMetal);
            AssertTerrainHit(metal, new Vector2(200f, 0f), MechanicalClawFailure.None,
                "TempleMetal terrain");

            MechanicalClawTargetResolution ice = await ResolveTerrain("IceTerrain",
                new Vector2(210f, 0f), new Vector2(20f, 100f), Vector2.Zero,
                new Vector2(400f, 0f), GrappleSurfaceMaterial.Ice);
            AssertTerrainHit(ice, new Vector2(200f, 0f), MechanicalClawFailure.IceSlip,
                "Ice terrain");

            MechanicalClawTargetResolution forbidden = await ResolveTerrain("ForbiddenTerrain",
                new Vector2(210f, 0f), new Vector2(20f, 100f), Vector2.Zero,
                new Vector2(400f, 0f), GrappleSurfaceMaterial.Forbidden);
            AssertTerrainHit(forbidden, new Vector2(200f, 0f), MechanicalClawFailure.Forbidden,
                "Forbidden terrain");

            MechanicalClawTargetResolution exactRange = await ResolveTerrain("Exact620Terrain",
                new Vector2(630f, 0f), new Vector2(20f, 100f), Vector2.Zero,
                new Vector2(700f, 0f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(exactRange, new Vector2(620f, 0f), MechanicalClawFailure.None,
                "terrain hit exactly at 620 pixels");

            MechanicalClawTargetResolution beyondRange = await ResolveTerrain("Beyond620Terrain",
                new Vector2(630.01f, 0f), new Vector2(20f, 100f), Vector2.Zero,
                new Vector2(700f, 0f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(beyondRange, new Vector2(620.01f, 0f), MechanicalClawFailure.OutOfRange,
                "terrain hit beyond the 620-pixel range");

            StaticBody2D blocker = WorldBlocker(new Vector2(120f, 0f), new Vector2(20f, 100f));
            StaticBody2D behind = Terrain("TypedBehindUntyped", new Vector2(220f, 0f),
                new Vector2(20f, 100f), GrappleSurfaceMaterial.GripStone);
            root.AddChild(blocker);
            root.AddChild(behind);
            caster.GlobalPosition = Vector2.Zero;
            await Frames(tree, 2);
            MechanicalClawTargetResolution obstructed = resolver.Resolve(caster, new Vector2(400f, 0f));
            TestAssert.True(obstructed.Surface == null,
                "the first untyped collider blocks the typed terrain behind it");
            TestAssert.Equal(MechanicalClawFailure.Obstructed, FailureFor(obstructed.Snapshot),
                "untyped first-hit geometry reports blocked instead of inventing a material profile");
            blocker.QueueFree();
            behind.QueueFree();
            await Frames(tree, 2);

            StaticBody2D lockedTerrain = Terrain("LockedTerrain", new Vector2(230f, 0f),
                new Vector2(20f, 100f), GrappleSurfaceMaterial.GripStone);
            root.AddChild(lockedTerrain);
            await Frames(tree, 2);
            MechanicalClawTargetResolution initial = resolver.Resolve(caster, new Vector2(400f, 0f));
            Vector2 originalHit = initial.Snapshot.WorldAnchor;
            initial.Surface.AnchorOffset = new Vector2(900f, 700f);
            MechanicalClawTargetSnapshot locked = resolver.SnapshotLocked(caster, initial.Surface,
                originalHit, initial.Snapshot.SurfaceNormal);
            TestAssert.True(!MechanicalClawTargetResolver.TryGetLineOcclusion(caster, initial.Surface,
                caster.GlobalPosition, new Vector2(230f, 0f), out _, out _),
                "the locked target terrain itself is not a visual occluder");
            TestAssert.True(originalHit.DistanceTo(locked.WorldAnchor) <= .001f,
                "terrain lock keeps the original physics hit rather than the profile node anchor");
            TestAssert.Equal(initial.Snapshot.SurfaceNormal, locked.SurfaceNormal,
                "terrain lock keeps the original physics face normal after admission");
            lockedTerrain.QueueFree();
            await Frames(tree, 2);
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ScheduledAttachedSpacePublishesAtomicReleaseAndSuppressesMotorJump(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawScheduledBoost", 200f, new Vector2(0f, -200f));
        var events = new List<MechanicalClawFrame>();
        void Record(MechanicalClawFrame frame, GrappleSurface2D target) => events.Add(frame);
        fixture.Claw.ClawFrameChanged += Record;
        try
        {
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture,
                Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 &&
                fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture,
                    Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "scheduled boost setup reaches a live latch after a one-frame LMB click");

            ulong castId = fixture.Claw.CurrentFrame.CastId;
            int jumpsBefore = fixture.Motor.AcceptedJumpCount;
            await ScheduledStep(tree, fixture, Input(jumpPressed: true,
                jumpHeld: true, mouseWorld: fixture.Surface.WorldAnchor));

            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "the Task 2 bridge routes attached Space to one Released transaction frame");
            TestAssert.Equal(castId, fixture.Claw.CurrentFrame.CastId,
                "the boost-release transaction keeps the live cast identity");
            TestAssert.Equal(MechanicalClawReleaseKind.Boost, fixture.Claw.CurrentFrame.ReleaseKind,
                "the controller receives Boost metadata for Task 3's atomic publication");
            TestAssert.True(fixture.Claw.CurrentFrame.BoostConsumed,
                "the attached boost-release command is consumed exactly once");
            TestAssert.Equal(jumpsBefore, fixture.Motor.AcceptedJumpCount,
                "attached Space is suppressed before the ordinary motor jump path");
            TestAssert.Equal(1, events.FindAll(frame => frame.State == MechanicalClawState.Released &&
                frame.ReleaseKind == MechanicalClawReleaseKind.Boost).Count,
                "the compile-valid Task 2 bridge publishes one metadata-bearing release");
        }
        finally
        {
            fixture.Claw.ClawFrameChanged -= Record;
            await Dispose(tree, fixture.Root);
        }
    }

    public static async Task ScheduledBoostKeepsTheRenderedAttachedAction(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawBoostPresentation", 200f, new Vector2(0f, -200f));
        PlayerPresentationCoordinator coordinator = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = fixture.Player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        Node effects = fixture.Player.GetNode("VisualRoot/Effects");
        using var profile = new PresentationProfile();
        using PackedScene wrongCue = CueScene("WrongAttackCue");
        using PackedScene attachedCue = CueScene("AttachedCue");
        var displayed = new List<(PlayerPresentationFrame Frame, PlayerPose Pose)>();
        var clawStates = new List<MechanicalClawState>();
        var observations = new BoostPublicationObservation[128];
        int observationCount = 0, observationOrdinal = 0, observationOverflow = 0;
        int publicationCount = 0, observerFailuresBefore = coordinator.FrameObserverFailureCount;
        BoostPublicationObservation latestPublication = default;
        bool trace = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--trace-boost-render");
        int boostEvents = 0, wrongAtBoost = 0, attachedAtBoost = 0;
        PlayerPose beforeBoostPose = PlayerPose.Identity;
        BoostPublicationObservation Capture(string boundary, PlayerPresentationFrame published = default,
            MechanicalClawFrame emittedClaw = default)
        {
            var observation = new BoostPublicationObservation(++observationOrdinal, boundary,
                (long)Engine.GetProcessFrames(), (long)Engine.GetPhysicsFrames(),
                (long)Engine.GetFramesDrawn(), published, formal.CurrentPlayerFrame,
                coordinator.LastPlayerFrame, formal.CurrentActionPose, fixture.Claw.CurrentFrame,
                emittedClaw);
            if (observationCount < observations.Length) observations[observationCount++] = observation;
            else observationOverflow++;
            return observation;
        }
        void ObservePresentation(PlayerPresentationFrame frame)
        {
            if (boostEvents == 0 && frame.Action.State == PlayerActionState.ThreadApply)
                beforeBoostPose = formal.CurrentActionPose;
            else if (boostEvents > 0 && displayed.Count < 3)
                displayed.Add((frame, formal.CurrentActionPose));
            if (boostEvents > 0)
            {
                publicationCount++;
                latestPublication = Capture("publication", frame);
            }
            if (trace && boostEvents > 0)
                GD.Print($"BOOST_RENDER_TRACE render={Engine.GetFramesDrawn()} physics={Engine.GetPhysicsFrames()} " +
                    $"state={fixture.Claw.CurrentFrame.State} action={frame.Action.State}/{frame.Action.Phase} " +
                    $"player={fixture.Player.GlobalPosition} velocity={fixture.Player.Velocity} " +
                    $"arm_position={formal.CurrentActionPose.NearArm.Position} arm_rotation={formal.CurrentActionPose.NearArm.RotationRadians}");
        }
        void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            clawStates.Add(frame.State);
            if (boostEvents > 0 || frame.State == MechanicalClawState.Boost)
                Capture("claw_callback", emittedClaw: frame);
            if (frame.State != MechanicalClawState.Boost) return;
            boostEvents++;
            // The real coordinator subscribed in Ready, before this observer. Count
            // only the fresh profile's effects on the actual accepted Boost edge.
            foreach (Node child in effects.GetChildren())
            {
                if (child.Name.ToString().Contains("WrongAttackCue")) wrongAtBoost++;
                if (child.Name.ToString().Contains("AttachedCue")) attachedAtBoost++;
            }
        }
        coordinator.FramePublished += ObservePresentation;
        fixture.Claw.ClawFrameChanged += ObserveClaw;
        try
        {
            // Fixed test-owned framing. Modes/Claw/Motor/Coordinator run on the real
            // scheduler; only sampled input is injected, never a Boost frame or pose.
            fixture.Root.AddChild(new Camera2D
            {
                Name = "BoostProofCamera", Position = new Vector2(0, -90),
                Zoom = new Vector2(2, 2), Enabled = true
            });
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture,
                Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "Boost presentation setup attaches from one click and automatic take-up");
            for (int settle = 0; settle < 45; settle++)
                await ScheduledStep(tree, fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.True(beforeBoostPose.Owns(PlayerPoseSlot.NearArm) &&
                beforeBoostPose.NearArm != PoseTransform2D.Identity,
                "the real render coordinator has established a nonidentity attached arm before Boost");
            // Exercise the optional cue extension without mutating the cached Player profile.
            profile.CueScenes[PresentationCue.AttackStarted.ToString()] = wrongCue;
            profile.CueScenes[PresentationCue.ThreadApplied.ToString()] = attachedCue;
            coordinator.Profile = profile;
            Capture("boost_step_before");
            await ScheduledStep(tree, fixture, Input(jumpPressed: true,
                jumpHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            Capture("boost_step_after");
            TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Released or
                MechanicalClawState.Retract,
                "gameplay leaves attachment in the same Space transaction");
            for (int after = 0; after < 8 && displayed.Count < 2; after++)
            {
                Capture("after_step_before");
                await ScheduledStep(tree, fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
                Capture("after_step_after");
            }
            Capture("final_read");
            GD.Print($"BOOST_PRESENTATION_OBSERVED events={boostEvents} render_samples={displayed.Count} " +
                $"configured_attached_cues={attachedAtBoost} configured_attack_cues={wrongAtBoost}");
            TestAssert.Equal(1, boostEvents, "the scheduled input publishes one accepted Boost edge");
            int boostIndex = clawStates.IndexOf(MechanicalClawState.Boost);
            TestAssert.True(boostIndex >= 0 && clawStates.Count > boostIndex + 1 &&
                clawStates[boostIndex + 1] == MechanicalClawState.Released,
                "the controller publishes ordered Boost then Released atomically");
            TestAssert.True(attachedAtBoost == 1 && wrongAtBoost == 0,
                "a configured Boost cue instantiates ThreadApplied exactly once, never the AttackStarted fallback");
            TestAssert.True(displayed.Count >= 2,
                "the real coordinator publishes two positive-delta transaction frames");
            TestAssert.True(displayed[0].Frame.Action.State == PlayerActionState.ThreadApply &&
                displayed[0].Frame.Action.Phase == PlayerActionPhase.Contact &&
                displayed[0].Pose.Owns(PlayerPoseSlot.NearArm),
                "the first displayed transaction frame is typed launch contact with arm ownership");
            TestAssert.True(displayed[1].Frame.Action.State == PlayerActionState.ThreadApply &&
                displayed[1].Frame.Action.Phase == PlayerActionPhase.Recovery,
                "the following displayed frame is typed release recovery");
            TestAssert.Equal(0, observationOverflow,
                "BOOST_PUBLICATION_OVERFLOW: every observed publication must be retained");
            TestAssert.Equal(observerFailuresBefore, coordinator.FrameObserverFailureCount,
                "BOOST_PUBLICATION_OBSERVER_FAILURE: boundary capture must not be swallowed");
            int checkedPublications = 0;
            for (int index = 0; index < observationCount; index++)
            {
                BoostPublicationObservation observation = observations[index];
                if (observation.Boundary != "publication") continue;
                checkedPublications++;
                TestAssert.Equal(observation.Published, observation.Formal,
                    $"BOOST_PUBLICATION_FORMAL_FULL_FRAME: ordinal={observation.Ordinal} " +
                    $"process={observation.Process} physics={observation.Physics}");
                TestAssert.Equal(observation.Published, observation.Coordinator,
                    $"BOOST_PUBLICATION_COORDINATOR_FULL_FRAME: ordinal={observation.Ordinal}");
            }
            TestAssert.True(checkedPublications == publicationCount && checkedPublications >= 2,
                "BOOST_PUBLICATION_COMPLETE: all native publications are checked, including a third");
            // The await may span more than the first two publications. Compare the
            // current complete typed frame with the latest observed publication,
            // not the historical second recovery snapshot. No yield separates reads.
            TestAssert.Equal(latestPublication.Published, formal.CurrentPlayerFrame,
                "BOOST_PUBLICATION_FINAL_FORMAL: the latest complete typed frame owns the visual");
            TestAssert.Equal(latestPublication.Published, coordinator.LastPlayerFrame,
                "BOOST_PUBLICATION_FINAL_COORDINATOR: the final read matches the latest publication");
            TestAssert.True(Math.Abs(displayed[0].Pose.NearArm.RotationRadians -
                beforeBoostPose.NearArm.RotationRadians) < .35f,
                "the displayed launch does not restart the attached arm from identity");
        }
        finally
        {
            coordinator.FramePublished -= ObservePresentation;
            fixture.Claw.ClawFrameChanged -= ObserveClaw;
            GD.Print($"BOOST_PUBLICATION_BOUNDARY_SUMMARY publications={publicationCount} " +
                $"records={observationCount} overflow={observationOverflow} " +
                $"observer_failures={observerFailuresBefore}->{coordinator.FrameObserverFailureCount} " +
                $"latest_ordinal={latestPublication.Ordinal} rendered_frame_claim=false");
            for (int index = 0; index < observationCount; index++)
            {
                BoostPublicationObservation observation = observations[index];
                GD.Print($"BOOST_PUBLICATION_BOUNDARY ordinal={observation.Ordinal} boundary={observation.Boundary} " +
                    $"process={observation.Process} physics={observation.Physics} drawn={observation.Drawn} " +
                    $"published={observation.Published} formal={observation.Formal} coordinator={observation.Coordinator} " +
                    $"pose={observation.Pose} claw={observation.Claw} emitted_claw={observation.EmittedClaw}");
            }
            await Dispose(tree, fixture.Root);
        }

        static PackedScene CueScene(string name)
        {
            var scene = new PackedScene();
            var node = new Node2D { Name = name };
            try { TestAssert.Equal(Error.Ok, scene.Pack(node), "test-owned configured cue packs"); }
            finally { node.Free(); }
            return scene;
        }
    }

    public static async Task AutomaticBoostReleasePreservesUpwardInertia(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawBoostReleaseInertia", 80f, new Vector2(-80f, -300f),
            new Vector2(0f, -300f));
        try
        {
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture,
                Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "release inertia fixture reaches an actual scheduled latch");
            MechanicalClawFrame attached = fixture.Claw.CurrentFrame;
            Vector2 toAnchor = attached.CasterWorldPosition.DirectionTo(attached.AnchorPoint);
            Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
            Vector2 boostDelta = Vector2.Up * MechanicalClawSettings.Default.BoostUpwardSpeed +
                tangent * MechanicalClawSettings.Default.BoostTangentialSpeed;
            await ScheduledStep(tree, fixture, Input(jumpPressed: true,
                jumpHeld: true, swingAxis: 1f, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "release inertia fixture ends the attached Space transaction in Released");
            TestAssert.Equal(MechanicalClawReleaseKind.Boost, fixture.Claw.CurrentFrame.ReleaseKind,
                "release inertia fixture retains typed Boost release metadata");
            float before = fixture.Player.Velocity.Y;
            GD.Print($"GRAPPLE_RELEASE_FIXTURE anchor={attached.AnchorPoint} normal={attached.SurfaceNormal} " +
                $"boost_delta={boostDelta} release_velocity={fixture.Player.Velocity}");
            TestAssert.True(before < -fixture.Motor.Tuning.JumpCutSpeed - 60f && !fixture.Player.IsOnFloor(),
                $"release setup has real upward launch momentum above ordinary jump-cut speed; vy={before}");

            for (int sample = 1; sample <= 3; sample++)
            {
                await ScheduledStep(tree, fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
                float expected = before + fixture.Motor.Tuning.Gravity * Frame * sample;
                TestAssert.Near(expected, fixture.Player.Velocity.Y, 1f,
                    $"grapple release retains upward momentum under gravity without holding Space, tick={sample}; " +
                    $"before={before} expected={expected} actual={fixture.Player.Velocity.Y}");
            }
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount,
                "attached launch/release does not create an ordinary or second jump");

            for (int index = 0; index < 180 && !fixture.Player.IsOnFloor(); index++)
                await ScheduledStep(tree, fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.True(fixture.Player.IsOnFloor(), "released player returns to the physical floor");
            await ScheduledStep(tree, fixture, Input(jumpPressed: true, jumpHeld: true));
            TestAssert.Equal(1, fixture.Motor.AcceptedJumpCount, "landing restores an ordinary jump");
            await ScheduledStep(tree, fixture, Input());
            TestAssert.True(fixture.Player.Velocity.Y >= -fixture.Motor.Tuning.JumpCutSpeed,
                "ordinary short-hop release still cuts upward velocity after a grapple landing");
            GD.Print($"GRAPPLE_RELEASE_INERTIA_OK before_y={before:0.00} gravity_only_ticks=3 short_hop_restored=True");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    public static async Task RealClawTakeoverEndsBurnWithoutRefund(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawReleaseDuringDash", 200f, new Vector2(-60f, -80f),
            new Vector2(0f, -40f));
        try
        {
            Vector2 anchor = fixture.Surface.WorldAnchor;
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                mouseWorld: anchor) with { DashPressed = true, Aim = Vector2.Up });
            TestAssert.True(fixture.Motor.IsDashing,
                "dash-release setup starts one real upward air dash before casting");
            TestAssert.True(!fixture.Motor.AirDashAvailable,
                "the pre-tree airborne start makes this the single budgeted air dash");
            await ScheduledStep(tree, fixture, Input(primaryPressed: true, mouseWorld: anchor));
            await ScheduledStep(tree, fixture, Input(primaryReleased: true, mouseWorld: anchor));
            for (int tick = 0; tick < 12 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; tick++)
                await ScheduledStep(tree, fixture, Input(mouseWorld: anchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "the short real cast reaches a latch inside the existing dash window");
            await ScheduledStep(tree, fixture, Input(mouseWorld: anchor));
            TestAssert.True(!fixture.Motor.IsDashing &&
                !fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox(),
                $"real attached claw ends short-jet burn before applying its own correction; " +
                $"dashing={fixture.Motor.IsDashing} state={fixture.Claw.CurrentFrame.State} " +
                $"correction={fixture.Claw.CurrentFrame.VelocityCorrection}");
            await ScheduledStep(tree, fixture, Input(jumpPressed: true, jumpHeld: true,
                mouseWorld: anchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "attached Space atomically releases after real claw takeover");
            TestAssert.Equal(MechanicalClawReleaseKind.Boost, fixture.Claw.CurrentFrame.ReleaseKind,
                "claw takeover release retains Boost metadata");
            TestAssert.True(!fixture.Motor.IsDashing, "release cannot restore ended burn");
            TestAssert.True(!fixture.Motor.IsDashing && !fixture.Player.IsOnFloor(),
                "claw takes ownership while genuinely airborne, without a landing refund");
            float beforeReleaseTail = fixture.Player.Velocity.Y;
            await ScheduledStep(tree, fixture, Input());
            TestAssert.Near(Mathf.Min(beforeReleaseTail + 1850f / 60f, 780f),
                fixture.Player.Velocity.Y, 1f, "new grapple release source follows gravity without resurrecting jet force");
            TestAssert.Equal(1, fixture.Motor.AcceptedDashCount, "the overlap accepts only one dash");
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "the overlap creates no ordinary extra jump");
            TestAssert.True(!fixture.Motor.AirDashAvailable, "claw release does not refund the air dash");
            GD.Print("GRAPPLE_SHORT_JET_TAKEOVER_OK burn_ended=True air_refunded=False source=scheduled_fixture human=false");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    public static async Task CancelledTravelPreservesOrdinaryShortHop(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawCancelledTravelShortHop", 500f, new Vector2(500f, -100f));
        try
        {
            for (int index = 0; index < 20 && !fixture.Player.IsOnFloor(); index++)
                await ScheduledStep(tree, fixture, Input());
            await ScheduledStep(tree, fixture, Input(jumpPressed: true, jumpHeld: true));
            TestAssert.Equal(1, fixture.Motor.AcceptedJumpCount, "travel cancellation starts with an ordinary jump");
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, jumpHeld: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture, Input(primaryReleased: true, jumpHeld: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 20 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Travel; index++)
                await ScheduledStep(tree, fixture, Input(jumpHeld: true,
                    mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Travel, fixture.Claw.CurrentFrame.State,
                "cancelled cast is still traveling and has never latched");
            TestAssert.True(fixture.Player.Velocity.Y < -fixture.Motor.Tuning.JumpCutSpeed,
                "ordinary jump is still above the short-hop cut speed before cancellation");
            await ScheduledStep(tree, fixture, Input(primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "a second click cancels unlatched Travel into Released");
            TestAssert.True(fixture.Player.Velocity.Y >= -fixture.Motor.Tuning.JumpCutSpeed,
                $"cancelling an unlatched claw cannot disable ordinary short-hop control; vy={fixture.Player.Velocity.Y}");
            GD.Print("GRAPPLE_TRAVEL_CANCEL_SHORT_HOP_OK");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AutomaticPhysicsLongSwingAndReleaseStayVelocityBounded(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawAutomaticSwing", 200f, new Vector2(0f, -200f));
        try
        {
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture,
                Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 &&
                fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture,
                    Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "automatic physics reaches Latched without manually calling any _PhysicsProcess method");

            float maximumSpeed = fixture.Player.Velocity.Length();
            for (int index = 0; index < 120; index++)
            {
                await ScheduledStep(tree, fixture, Input(swingAxis: 1f,
                    mouseWorld: fixture.Surface.WorldAnchor));
                maximumSpeed = Mathf.Max(maximumSpeed, fixture.Player.Velocity.Length());
            }
            TestAssert.True(maximumSpeed < 1200f,
                $"120 automatic swing frames keep velocity bounded; maximum={maximumSpeed:0.00}");

            float beforeReleaseSpeed = fixture.Player.Velocity.Length();
            await ScheduledStep(tree, fixture, Input(primaryPressed: true,
                swingAxis: 1f, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "the second click publishes the Released cancellation frame");
            TestAssert.True(fixture.Player.Velocity.Length() <= beforeReleaseSpeed + 180f,
                $"Released tangential inertia is inherited once instead of being added a second time; " +
                $"before={beforeReleaseSpeed:0.00}, after={fixture.Player.Velocity.Length():0.00}");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AutomaticInputMapQRetractsInDeclaredPhysicsOrder(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawAutomaticQ", 180f, new Vector2(180f, 0f));
        try
        {
            TestAssert.Equal(-300, fixture.Input.ProcessPhysicsPriority,
                "generated Input samples first at the declared physics priority");
            TestAssert.Equal(-200, fixture.Modes.ProcessPhysicsPriority,
                "generated ActionModes routes the sampled Q/E frame before the claw");
            TestAssert.Equal(-100, fixture.Claw.ProcessPhysicsPriority,
                "generated MechanicalClaw runs after ActionModes and before Motor");
            TestAssert.Equal(0, fixture.Motor.ProcessPhysicsPriority,
                "generated Motor consumes the current claw correction last");

            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture,
                Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 &&
                fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture,
                    Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "Q regression setup has a live automatically scheduled claw");

            fixture.Input.SetPhysicsProcess(true);
            Godot.Input.ActionPress(InputActions.SelectAttackMode);
            await Frames(tree, 2);
            Godot.Input.ActionRelease(InputActions.SelectAttackMode);

            TestAssert.Equal(PlayerActionMode.Attack, fixture.Modes.CurrentMode,
                "the real InputMap Q action selects Attack mode under automatic scheduling");
            TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Released or
                    MechanicalClawState.Retract,
                "the same real Q frame safely retracts the live claw without a direct method call");
        }
        finally
        {
            Godot.Input.ActionRelease(InputActions.SelectAttackMode);
            Godot.Input.ActionRelease(InputActions.SelectGrappleMode);
            Godot.Input.ActionRelease(InputActions.PrimaryAction);
            await Dispose(tree, fixture.Root);
        }
    }

    private static async Task AbilityShieldAndGrappleScalesComposeInRealMotor(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawTripleScale", 180f, new Vector2(180f, 0f));
        try
        {
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Motor.AbilityMovementScale = .5f;
            fixture.Motor.ShieldMovementScale = .65f;
            fixture.Motor.ApplyGrappleConstraint(Vector2.Zero, .65f);
            for (int index = 0; index < 20; index++)
                await ScheduledStep(tree, fixture, Input(swingAxis: 1f));

            float expectedScale = .5f * .65f * .65f;
            float expectedVelocity = fixture.Motor.Tuning.MaxRunSpeed * expectedScale;
            TestAssert.Near(expectedScale, fixture.Motor.EffectiveMovementScale, .0001f,
                "ability, shield and grapple movement modifiers multiply independently");
            TestAssert.Near(expectedVelocity, fixture.Player.Velocity.X, 1.1f,
                "real PlayerMotor movement converges to the triple-scaled horizontal speed");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task RuntimeTravelsLatchesReelsMovesReleasesAndCleansVisuals(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, "MechanicalClawLifecycle", 420f);
        try
        {
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Aim, fixture.Claw.CurrentFrame.State,
                "E plus primary mouse press enters visible Aim preview");
            TestAssert.True(fixture.ClawVisual.Visible && fixture.Line.CurrentState == ThreadLineState.Preview,
                "the subscribed line and shuttle present the real preview frame");
            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));

            int travelFrames = 0;
            for (int index = 0; index < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
            {
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
                if (fixture.Claw.CurrentFrame.State == MechanicalClawState.Travel) travelFrames++;
            }
            TestAssert.True(travelFrames >= 2,
                "the 420-pixel shuttle remains in real Travel for multiple physics steps");
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "the real runtime latches the authored surface after shuttle travel");
            TestAssert.Equal(fixture.Surface, fixture.Claw.CurrentTarget,
                "the runtime exposes exactly one locked authored target");
            TestAssert.Near(1f, fixture.Claw.CurrentFrame.SurfaceNormal.Length(), .0001f,
                "the runtime frame preserves a normalized point-anchor admission direction");
            TestAssert.True(fixture.Claw.CurrentFrame.SurfaceNormal.X < -.999f &&
                    Mathf.Abs(fixture.Claw.CurrentFrame.SurfaceNormal.Y) < .01f,
                "the point-anchor normal points from the authored anchor toward the slightly settled caster");
            Vector2 dockOffset = fixture.Claw.CurrentFrame.DockPoint -
                fixture.Claw.CurrentFrame.AnchorPoint;
            TestAssert.Near(48f, dockOffset.Length(), .001f,
                "the controller stores a dock one minimum safe rope length outside the point anchor");
            TestAssert.True(dockOffset.Normalized().Dot(
                    fixture.Claw.CurrentFrame.SurfaceNormal) > .9999f,
                "the stored dock remains on the admitted outward-normal side of the point anchor");
            TestAssert.True(!fixture.Claw.CurrentFrame.Arrived,
                "Task 1 geometry metadata does not invent a physical arrival");

            float initialRope = fixture.Claw.CurrentFrame.RopeLength;
            Vector2 beforeMove = fixture.Player.GlobalPosition;
            Step(fixture, Input(
                mouseWorld: fixture.Surface.WorldAnchor), applyMotor: true);
            TestAssert.True(fixture.Claw.CurrentFrame.RopeLength < initialRope,
                "automatic take-up shortens the live rope with zero vertical input");
            TestAssert.True(fixture.Player.GlobalPosition.X > beforeMove.X,
                "PlayerMotor applies the model velocity correction toward the anchor without teleporting");

            Step(fixture, Input(primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "the second click publishes Released before retracting");
            await SettleToIdle(tree, fixture);
            TestAssert.True(fixture.Line.CurrentState == ThreadLineState.Idle && fixture.ClawVisual.IsClawClear,
                "release leaves no line or shuttle after the claw fully retracts");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    public static async Task AttachedOcclusionReleasesBeforePullingThroughTheBlocker(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, "MechanicalClawAttachedOcclusion", 420f);
        try
        {
            // Freeze the existing manual fixture at a hand-checked ray origin; all claw
            // phases and subscribed visuals still come from real controller/model steps.
            fixture.Player.GlobalPosition = Vector2.Zero;
            fixture.Player.Velocity = Vector2.Zero;
            Latch();
            TestAssert.True(fixture.Line.ActiveStrandCount > 0 &&
                fixture.Line.LastWorldEnd.DistanceTo(new Vector2(420f, 0f)) < .01f,
                "unobstructed attached line initially reaches the actual 420-pixel target");
            TestAssert.True(fixture.ClawVisual.LatchVisible && !fixture.ClawVisual.ObscuredConnectionVisible,
                "a visible latch does not show the obscured local connection cue");

            StaticBody2D blocker = WorldBlocker(new Vector2(210f, 0f), new Vector2(40f, 120f));
            fixture.Root.AddChild(blocker);
            await Frames(tree, 2);
            var released = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += RecordReleased;
            Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            fixture.Claw.ClawFrameChanged -= RecordReleased;
            TestAssert.Equal(1, released.Count,
                "post-latch world occlusion publishes exactly one Released transaction");
            TestAssert.True(released[0].Failure == MechanicalClawFailure.Obstructed &&
                released[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed,
                "post-latch world occlusion is typed Released/Obstructed");
            TestAssert.True(MechanicalClawTargetResolver.TryGetLineOcclusion(fixture.Player, fixture.Surface,
                    released[0].CasterWorldPosition, released[0].AnchorPoint,
                    out Vector2 hit, out Vector2 normal) && Math.Abs(hit.X - 190f) < .1f,
                $"the retained real occlusion query hits the blocker's first face at x=190; hit={hit}");
            Vector2 inset = hit + normal.Normalized() * 4f;
            TestAssert.True(Math.Abs(inset.X - 186f) < .1f && inset.X <= 190f,
                $"renderer-compatible normal inset stays before the first face; inset={inset}");
            TestAssert.True(fixture.Line.CurrentState == ThreadLineState.Idle &&
                fixture.Line.ActiveStrandCount == 0 && fixture.Line.Gradient == null &&
                !fixture.ClawVisual.ObscuredConnectionVisible && !fixture.ClawVisual.LatchVisible,
                "Released immediately clears the subscribed line and shuttle attached cues");
            TestAssert.True(fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() &&
                Math.Abs(fixture.Motor.GrappleMovementScale - 1f) < .0001f,
                "obstructed release clears residual correction and motor scale");
            await SettleToIdle(tree, fixture);

            void RecordReleased(MechanicalClawFrame frame, GrappleSurface2D _)
            {
                if (frame.State == MechanicalClawState.Released) released.Add(frame);
            }
        }
        finally { await Dispose(tree, fixture.Root); }

        void Latch()
        {
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int frame = 0; frame < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; frame++)
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "real E and one primary click travel to and latch the fixture target");
        }
    }

    public static async Task AttachedVisibilityBoundariesUseOnlyTheSafeConnectionCue(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, "MechanicalClawVisibilityBoundaries", 420f);
        ulong cast = 0UL;
        try
        {
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int frame = 0; frame < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; frame++)
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "visibility boundaries start from an actual input-driven latch");
            cast = fixture.Claw.CurrentFrame.CastId;
            MechanicalClawFrame attached = fixture.Claw.CurrentFrame;

            // Freeze presentation-only boundary frames after the real input latch. Current
            // auto-pull correctly fails closed on live obstruction, which is covered by the
            // preceding physical route; these samples isolate its subscribed visual consumers.
            MechanicalClawFrame outOfRange = attached with
            {
                CasterWorldPosition = new Vector2(-600f, 0f),
                EffectiveVisibility = false
            };
            PresentBoundary(outOfRange);
            TestAssert.True(!MechanicalClawTargetResolver.TryGetLineOcclusion(fixture.Player, fixture.Surface,
                outOfRange.CasterWorldPosition, fixture.Surface.WorldAnchor, out _, out _),
                "the 1020-pixel attachment is out of range but has no intervening collider");
            AssertCueOnly(outOfRange, "out-of-range attachment with unobstructed line of sight");

            StaticBody2D grazing = WorldBlocker(new Vector2(250f, 0f), new Vector2(200f, 40f));
            fixture.Root.AddChild(grazing);
            await Frames(tree, 2);
            MechanicalClawFrame grazingFrame = attached with
            {
                CasterWorldPosition = new Vector2(0f, -40f),
                EffectiveVisibility = false
            };
            PresentBoundary(grazingFrame);
            TestAssert.Equal(ThreadLineState.Occluded, fixture.Line.CurrentState,
                "a shallow-angle wall crossing produces a clipped connection");
            TestAssert.True(fixture.Line.LastWorldEnd.DistanceTo(new Vector2(210f, -24f)) < .1f,
                "a grazing ray clears the wall along its normal, not by a nearly parallel ray inset");
            grazing.QueueFree();
            fixture.Root.AddChild(WorldBlocker(Vector2.Zero, new Vector2(40f, 120f)));
            await Frames(tree, 2);
            TestAssert.True(MechanicalClawTargetResolver.TryGetLineOcclusion(fixture.Player, fixture.Surface,
                Vector2.Zero, fixture.Surface.WorldAnchor, out _, out Vector2 insideNormal) && insideNormal.IsZeroApprox(),
                "the visual ray detects an origin inside a world blocker as an indeterminate exit face");
            MechanicalClawFrame insideFrame = attached with
                { CasterWorldPosition = Vector2.Zero, EffectiveVisibility = true };
            PresentBoundary(insideFrame);
            TestAssert.True(insideFrame.EffectiveVisibility,
                "inside-blocker presentation does not change the gameplay admission ray's visibility");
            AssertCueOnly(insideFrame, "attachment whose caster is inside a blocker");

            // Direct presentation boundary check: these frames do not claim real
            // controller phase transitions; they protect the cue's phase whitelist.
            foreach (MechanicalClawState state in new[] { MechanicalClawState.Latched, MechanicalClawState.Reel,
                MechanicalClawState.Swing, MechanicalClawState.Boost })
            {
                fixture.ClawVisual.Present(attached with { State = state, EffectiveVisibility = false });
                TestAssert.True(fixture.ClawVisual.ObscuredConnectionVisible && fixture.ClawVisual.LatchVisible,
                    $"{state} retains the local cue and locked-target diamond when obscured");
            }
            foreach (MechanicalClawState state in new[] { MechanicalClawState.Aim, MechanicalClawState.Windup,
                MechanicalClawState.Travel, MechanicalClawState.Released, MechanicalClawState.Retract,
                MechanicalClawState.Failed, MechanicalClawState.Idle })
            {
                fixture.ClawVisual.Present(attached with { State = MechanicalClawState.Latched, EffectiveVisibility = false });
                fixture.ClawVisual.Present(attached with { State = state, EffectiveVisibility = false });
                TestAssert.True(!fixture.ClawVisual.ObscuredConnectionVisible,
                    $"{state} clears a previously active obscured connection cue");
            }
        }
        finally { await Dispose(tree, fixture.Root); }

        void PresentBoundary(MechanicalClawFrame frame)
        {
            typeof(ThreadLineVisual).GetMethod("OnClawFrameChanged",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                fixture.Line, new object[] { frame, fixture.Surface });
            fixture.ClawVisual.Present(frame);
        }

        void AssertCueOnly(MechanicalClawFrame frame, string boundary)
        {
            TestAssert.True(frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost,
                $"{boundary} keeps an attached phase");
            TestAssert.True(frame.CastId == cast && frame.Failure == MechanicalClawFailure.None &&
                fixture.Claw.CurrentTarget == fixture.Surface,
                $"{boundary} preserves the real cast and target without failure");
            TestAssert.True(fixture.Line.ActiveStrandCount == 0 && fixture.Line.Gradient == null &&
                fixture.ClawVisual.ObscuredConnectionVisible && fixture.ClawVisual.LatchVisible,
                $"{boundary} shows only the local connected cue and target diamond, never a full rope");
        }
    }

    private static async Task NonfatalDamagePreservesContinuousInputAndConsumesOneShotEdges(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree,
            "MechanicalClawNonfatalDamageInput", 240f);
        try
        {
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int frame = 0; frame < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched;
                 frame++)
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "damage-input fixture starts from one real cast and latch");

            int residencyReleases = 0;
            fixture.Claw.TargetResidencyChanged += ObserveResidency;
            PlayerInputFrame sampled = new(
                MoveAxis: .5f,
                Aim: new Vector2(.25f, -.75f),
                JumpPressed: true,
                JumpHeld: true,
                DashPressed: true,
                AttackPressed: true,
                ThreadPressed: true,
                ThreadHeld: true,
                ThreadReleased: true,
                MouseWorld: new Vector2(321f, -123f),
                AttackModePressed: true,
                GrappleModePressed: true,
                RequestedSlot: 3,
                PrimaryPressed: true,
                PrimaryHeld: true,
                PrimaryReleased: true,
                ShieldHeld: true);
            SetInputFrame(fixture.Input, sampled);
            HealthComponent health = fixture.Player.GetNode<HealthComponent>("Health");
            health.ResetFull();
            health.InvulnerabilitySeconds = 0f;
            TestAssert.True(health.ApplyDamage(new DamageRequest(
                    1, Vector2.Right, 0f, 901UL, 902UL)),
                "nonfatal damage reaches the actual player health component");

            PlayerInputFrame afterDamage = fixture.Input.CurrentFrame;
            TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle &&
                fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null &&
                residencyReleases == 1 &&
                fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() &&
                Math.Abs(fixture.Motor.GrappleMovementScale - 1f) < .0001f,
                "nonfatal damage clears the claw, constraint, and residency exactly once");
            TestAssert.True(afterDamage.MoveAxis == sampled.MoveAxis &&
                afterDamage.Aim == sampled.Aim && afterDamage.MouseWorld == sampled.MouseWorld &&
                afterDamage.JumpHeld && afterDamage.ThreadHeld && afterDamage.PrimaryHeld &&
                afterDamage.ShieldHeld,
                "nonfatal damage preserves held defense and continuous movement/aim input");
            TestAssert.True(!afterDamage.JumpPressed && !afterDamage.DashPressed &&
                !afterDamage.AttackPressed && !afterDamage.ThreadPressed && !afterDamage.ThreadReleased &&
                !afterDamage.AttackModePressed && !afterDamage.GrappleModePressed &&
                afterDamage.RequestedSlot == 0 && !afterDamage.PrimaryPressed &&
                !afterDamage.PrimaryReleased,
                "nonfatal damage consumes every stale one-shot action, mode, and slot edge");

            fixture.Modes._PhysicsProcess(0d);
            fixture.Claw._PhysicsProcess(Frame);
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "the preserved held input cannot replay the consumed primary edge as a new cast");

            SetInputFrame(fixture.Input, sampled);
            fixture.Claw.ResetTransientState();
            TestAssert.Equal(default, fixture.Input.CurrentFrame,
                "the public claw reset retains full sampled-frame clearing");

            SetInputFrame(fixture.Input, sampled);
            health.ResetFull();
            health.InvulnerabilitySeconds = 0f;
            TestAssert.True(health.ApplyDamage(new DamageRequest(
                    99, Vector2.Right, 0f, 903UL, 904UL)),
                "fatal damage reaches the actual player health component");
            TestAssert.Equal(default, fixture.Input.CurrentFrame,
                "death retains full sampled-frame clearing");
            GD.Print("GRAPPLE_DAMAGE_INPUT_INVALIDATION_OK nonfatal=continuous-preserved " +
                "edges=consumed lifecycle=full-clear residency_releases=1");

            fixture.Claw.TargetResidencyChanged -= ObserveResidency;
            void ObserveResidency(GrappleSurface2D _, bool resident)
            {
                if (!resident) residencyReleases++;
            }
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task TargetUnloadFailsClosedAndClearsTheSingleClaw(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, "MechanicalClawUnload", 360f);
        try
        {
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            while (fixture.Claw.CurrentFrame.State != MechanicalClawState.Travel)
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));

            ulong cancelledCast = fixture.Claw.CurrentFrame.CastId;
            Step(fixture, Input(primaryPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Released &&
                fixture.Claw.CurrentFrame.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                fixture.Claw.CurrentFrame.CastId == cancelledCast,
                "a second click cancels the one live cast without allocating another identity");
            await SettleToIdle(tree, fixture);

            Step(fixture, Input(primaryPressed: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            while (fixture.Claw.CurrentFrame.State != MechanicalClawState.Travel)
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            while (fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched)
                Step(fixture, Input(mouseWorld: fixture.Surface.WorldAnchor));
            var events = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += Record;
            fixture.Claw.NotifyTargetWillUnload(fixture.Surface);
            fixture.Claw.ClawFrameChanged -= Record;

            int failedCount = events.FindAll(frame => frame.State == MechanicalClawState.Failed &&
                frame.Failure == MechanicalClawFailure.TargetLost).Count;
            TestAssert.Equal(1, failedCount,
                "NotifyTargetWillUnload synchronously publishes Failed/TargetLost exactly once");
            TestAssert.True(events.Count >= 2 && events[0].State == MechanicalClawState.Failed &&
                events[^1].State == MechanicalClawState.Idle,
                "target unload exposes failure before its immediate terminal Idle cleanup");
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "target unload completes cleanup synchronously without another physics tick");
            TestAssert.True(fixture.Claw.CurrentTarget == null,
                "target loss clears the strong public target reference");
            TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
                "target unload synchronously clears the motor constraint scale");
            TestAssert.True(fixture.Line.CurrentState == ThreadLineState.Idle &&
                fixture.Line.ActiveStrandCount == 0 && fixture.ClawVisual.IsClawClear,
                "target unload leaves no residual line or shuttle");

            void Record(MechanicalClawFrame frame, GrappleSurface2D target) => events.Add(frame);
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task NoTargetFailureUsesOnlyTheLocalCasterMarker(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, "MechanicalClawNoTarget", 420f);
        try
        {
            Vector2 emptyCursor = new(-300f, 260f);
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                mouseWorld: emptyCursor));
            Step(fixture, Input(primaryReleased: true, mouseWorld: emptyCursor));

            TestAssert.Equal(MechanicalClawFailure.NoTarget, fixture.Claw.CurrentFrame.Failure,
                "empty cursor uses the typed NoTarget failure");
            TestAssert.Equal(0, fixture.Line.ActiveStrandCount,
                "NoTarget never draws a world rope from the player to world origin");
            TestAssert.True(fixture.ClawVisual.FailureAtCasterVisible &&
                fixture.ClawVisual.ShuttleWorldPosition.DistanceTo(fixture.Player.GlobalPosition) < 32f,
                "NoTarget keeps only its local failure marker beside the caster");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static void AssertResolved(MechanicalClawTargetResolution resolution,
        GrappleSurface2D expected, MechanicalClawFailure failure, string message)
    {
        TestAssert.Equal(expected, resolution.Surface, message + " selects the expected physics surface");
        TestAssert.Equal(failure, FailureFor(resolution.Snapshot), message + " preserves failure priority");
    }

    private static void AssertTerrainHit(MechanicalClawTargetResolution resolution,
        Vector2 expectedHit, MechanicalClawFailure failure, string context)
    {
        TestAssert.True(resolution.Surface != null && resolution.Surface.UseCollisionHitPoint,
            $"{context} resolves an explicitly typed terrain profile");
        TestAssert.True(expectedHit.DistanceTo(resolution.Snapshot.WorldAnchor) <= .05f,
            $"{context} locks the real physics hit position instead of profile.WorldAnchor");
        TestAssert.Equal(failure, FailureFor(resolution.Snapshot),
            $"{context} preserves typed failure priority");
    }

    private static MechanicalClawFailure FailureFor(MechanicalClawTargetSnapshot target)
    {
        var model = new MechanicalClawModel();
        MechanicalClawFrame frame = MechanicalClawFrame.Idle(Vector2.Zero, Vector2.Zero);
        frame = model.Step(frame, new MechanicalClawInput(MechanicalClawCommand.Cast,
                0f, false, Vector2.Zero),
            target, Frame);
        frame = model.Step(frame, new MechanicalClawInput(MechanicalClawCommand.None,
                0f, false, Vector2.Zero),
            target, Frame);
        return frame.Failure;
    }

    private static async Task<RuntimeFixture> CreateRuntimeFixture(SceneTree tree, string name, float range)
        => await CreateRuntimeFixture(tree, name, range, new Vector2(range, 0f));

    private static async Task<RuntimeFixture> CreateRuntimeFixture(SceneTree tree, string name, float range,
        Vector2 surfacePosition, Vector2? playerPosition = null)
    {
        var root = new Node2D { Name = name };
        GrappleSurface2D surface = Surface("Surface", surfacePosition,
            GrappleSurfaceMaterial.DedicatedAnchor);
        root.AddChild(surface);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Position = playerPosition ?? Vector2.Zero;
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        ThreadShuttleVisual clawVisual = player.GetNode<ThreadShuttleVisual>("VisualRoot/MechanicalClaw");
        ThreadLineVisual line = player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        motor.SetPhysicsProcess(false);
        player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        player.GetNode<PlayerEnergyController>("Energy").SetPhysicsProcess(false);
        player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        return new RuntimeFixture(root, player, surface, input, modes, claw, motor, clawVisual, line);
    }

    private static async Task<RuntimeFixture> CreateScheduledRuntimeFixture(SceneTree tree, string name,
        float range, Vector2 surfacePosition, Vector2? playerPosition = null)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, name, range, surfacePosition,
            playerPosition ?? new Vector2(0f, 14f));
        fixture.Root.AddChild(WorldBlocker(new Vector2(0f, 60f), new Vector2(1600f, 20f)));
        fixture.Modes.SetPhysicsProcess(true);
        fixture.Claw.SetPhysicsProcess(true);
        fixture.Motor.SetPhysicsProcess(true);
        return fixture;
    }

    private static void Step(RuntimeFixture fixture, PlayerInputFrame frame, bool applyMotor = false)
    {
        SetInputFrame(fixture.Input, frame);
        fixture.Modes._PhysicsProcess(Frame);
        fixture.Claw._PhysicsProcess(Frame);
        if (applyMotor) fixture.Motor._PhysicsProcess(Frame);
    }

    private static async Task ScheduledStep(SceneTree tree, RuntimeFixture fixture,
        PlayerInputFrame frame)
    {
        SetInputFrame(fixture.Input, frame);
        await Frames(tree, 1);
    }

    private static async Task SettleToIdle(SceneTree tree, RuntimeFixture fixture)
    {
        for (int index = 0; index < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Idle; index++)
        {
            Step(fixture, Input(mouseWorld: fixture.Claw.CurrentFrame.AnchorPoint));
            await Frames(tree, 1);
        }
        TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
            "the one live claw returns to Idle inside the bounded retract window");
    }

    private static PlayerInputFrame Input(bool grappleModePressed = false, bool primaryPressed = false,
        bool primaryHeld = false, bool primaryReleased = false, float swingAxis = 0f,
        float reelAxis = 0f, bool shieldHeld = false, bool jumpPressed = false,
        bool jumpHeld = false, Vector2 mouseWorld = default) =>
        new(swingAxis, new Vector2(0f, reelAxis), jumpPressed, jumpHeld, false, false, false,
            MouseWorld: mouseWorld,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld,
            PrimaryReleased: primaryReleased,
            ShieldHeld: shieldHeld);

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame) =>
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);

    private static GrappleSurface2D Surface(string name, Vector2 position, GrappleSurfaceMaterial material)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = material,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 10f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static StaticBody2D Terrain(string name, Vector2 position, Vector2 size,
        GrappleSurfaceMaterial material)
    {
        var terrain = new StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        terrain.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size }
        });
        terrain.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Material = material,
            UseCollisionHitPoint = true,
            AnchorOffset = new Vector2(777f, 555f)
        });
        return terrain;
    }

    private static StaticBody2D WorldBlocker(Vector2 position, Vector2 size)
    {
        var blocker = new StaticBody2D
        {
            Name = "LineOfSightBlocker",
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        blocker.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size }
        });
        return blocker;
    }

    private static async Task Dispose(SceneTree tree, Node root)
    {
        root.QueueFree();
        await Frames(tree, 2);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private readonly record struct BoostPublicationObservation(int Ordinal, string Boundary,
        long Process, long Physics, long Drawn, PlayerPresentationFrame Published,
        PlayerPresentationFrame Formal, PlayerPresentationFrame Coordinator, PlayerPose Pose,
        MechanicalClawFrame Claw, MechanicalClawFrame EmittedClaw);

    private sealed record RuntimeFixture(
        Node2D Root,
        CharacterBody2D Player,
        GrappleSurface2D Surface,
        PlayerInputReader Input,
        PlayerActionModeController Modes,
        MechanicalClawController Claw,
        PlayerMotor Motor,
        ThreadShuttleVisual ClawVisual,
        ThreadLineVisual Line);
}
