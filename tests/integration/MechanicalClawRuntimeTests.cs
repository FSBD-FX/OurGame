using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class MechanicalClawRuntimeTests
{
    private const float Frame = 1f / 60f;

    public static async Task Run(SceneTree tree)
    {
        await ResolverUsesRealPhysicsForRangeAssistMaterialsAndObstruction(tree);
        await ScheduledAttachedSpaceBoostKeepsLatchAndSuppressesMotorJump(tree);
        await RuntimeTravelsLatchesReelsMovesReleasesAndCleansVisuals(tree);
        await AutomaticPhysicsLongSwingAndReleaseStayVelocityBounded(tree);
        await AutomaticInputMapQRetractsInDeclaredPhysicsOrder(tree);
        await AbilityShieldAndGrappleScalesComposeInRealMotor(tree);
        await TargetUnloadFailsClosedAndClearsTheSingleClaw(tree);
        await NoTargetFailureUsesOnlyTheLocalCasterMarker(tree);
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
            MechanicalClawTargetResolution edge = resolver.Resolve(caster,
                pointAssist.WorldAnchor + Vector2.Down * 36f);
            TestAssert.Equal(pointAssist, edge.Surface,
                "the authored 36-pixel assist boundary remains inclusive");
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

            MechanicalClawTargetResolution ceiling = await ResolveTerrain("CeilingUnderside",
                new Vector2(0f, -200f), new Vector2(140f, 20f), Vector2.Zero,
                new Vector2(0f, -400f), GrappleSurfaceMaterial.GripStone);
            AssertTerrainHit(ceiling, new Vector2(0f, -190f), MechanicalClawFailure.None,
                "ceiling underside");

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
                originalHit);
            TestAssert.True(originalHit.DistanceTo(locked.WorldAnchor) <= .001f,
                "terrain lock keeps the original physics hit rather than the profile node anchor");
            lockedTerrain.QueueFree();
            await Frames(tree, 2);
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task ScheduledAttachedSpaceBoostKeepsLatchAndSuppressesMotorJump(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawScheduledBoost", 200f, new Vector2(0f, -200f));
        var events = new List<MechanicalClawFrame>();
        void Record(MechanicalClawFrame frame, GrappleSurface2D target) => events.Add(frame);
        fixture.Claw.ClawFrameChanged += Record;
        try
        {
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 &&
                fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture,
                    Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "scheduled boost setup reaches a live latch while LMB remains held");

            ulong castId = fixture.Claw.CurrentFrame.CastId;
            GrappleSurface2D target = fixture.Claw.CurrentTarget;
            float beforeBoostY = fixture.Player.Velocity.Y;
            await ScheduledStep(tree, fixture, Input(primaryHeld: true, jumpPressed: true,
                jumpHeld: true, mouseWorld: fixture.Surface.WorldAnchor));

            TestAssert.Equal(MechanicalClawState.Boost, fixture.Claw.CurrentFrame.State,
                "Space is routed to the attached one-shot boost instead of retract");
            TestAssert.Equal(castId, fixture.Claw.CurrentFrame.CastId,
                "the accepted boost keeps the same cast identity");
            TestAssert.Equal(target, fixture.Claw.CurrentTarget,
                "the accepted boost keeps the same typed surface");
            TestAssert.True(fixture.Line.Visible && fixture.ClawVisual.Visible &&
                fixture.ClawVisual.LatchVisible,
                "line and shuttle remain visibly attached during Boost");
            TestAssert.True(fixture.Player.Velocity.Y < beforeBoostY,
                "the accepted boost increases upward velocity");
            TestAssert.Near(fixture.Claw.CurrentFrame.VelocityCorrection.Y,
                fixture.Player.Velocity.Y, 2f,
                "the motor consumes no ordinary jump velocity on the attached Space frame");

            await ScheduledStep(tree, fixture,
                Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            await ScheduledStep(tree, fixture, Input(primaryHeld: true, jumpPressed: true,
                jumpHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(castId, fixture.Claw.CurrentFrame.CastId,
                "a second Space edge keeps the same live cast");
            TestAssert.True(fixture.Claw.CurrentFrame.State != MechanicalClawState.Boost &&
                fixture.Claw.CurrentFrame.BoostConsumed,
                "a second airborne Space edge adds no second boost");

            await ScheduledStep(tree, fixture, Input(primaryReleased: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.True(events.Exists(frame => frame.State == MechanicalClawState.Released),
                "releasing LMB remains the ordinary release path after boosting");
        }
        finally
        {
            fixture.Claw.ClawFrameChanged -= Record;
            await Dispose(tree, fixture.Root);
        }
    }

    private static async Task AutomaticPhysicsLongSwingAndReleaseStayVelocityBounded(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateScheduledRuntimeFixture(tree,
            "MechanicalClawAutomaticSwing", 200f, new Vector2(0f, -200f));
        try
        {
            await ScheduledStep(tree, fixture, Input(grappleModePressed: true,
                primaryPressed: true, primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 &&
                fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture,
                    Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "automatic physics reaches Latched without manually calling any _PhysicsProcess method");

            float maximumSpeed = fixture.Player.Velocity.Length();
            for (int index = 0; index < 120; index++)
            {
                await ScheduledStep(tree, fixture, Input(primaryHeld: true, swingAxis: 1f,
                    mouseWorld: fixture.Surface.WorldAnchor));
                maximumSpeed = Mathf.Max(maximumSpeed, fixture.Player.Velocity.Length());
            }
            TestAssert.True(maximumSpeed < 1200f,
                $"120 automatic swing frames keep velocity bounded; maximum={maximumSpeed:0.00}");

            float beforeReleaseSpeed = fixture.Player.Velocity.Length();
            await ScheduledStep(tree, fixture, Input(primaryHeld: false, primaryReleased: true,
                swingAxis: 1f, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "automatic primary release publishes the Task 5 Released frame");
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
                primaryPressed: true, primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            for (int index = 0; index < 120 &&
                fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
                await ScheduledStep(tree, fixture,
                    Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "Q regression setup has a live automatically scheduled claw");

            Godot.Input.ActionPress(InputActions.PrimaryAction);
            await Frames(tree, 1);
            fixture.Input.SetPhysicsProcess(true);
            Godot.Input.ActionPress(InputActions.SelectAttackMode);
            await Frames(tree, 2);
            Godot.Input.ActionRelease(InputActions.SelectAttackMode);
            Godot.Input.ActionRelease(InputActions.PrimaryAction);

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
                primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Aim, fixture.Claw.CurrentFrame.State,
                "E plus primary mouse press enters visible Aim preview");
            TestAssert.True(fixture.ClawVisual.Visible && fixture.Line.CurrentState == ThreadLineState.Preview,
                "the subscribed line and shuttle present the real preview frame");

            int travelFrames = 0;
            for (int index = 0; index < 120 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
            {
                Step(fixture, Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
                if (fixture.Claw.CurrentFrame.State == MechanicalClawState.Travel) travelFrames++;
            }
            TestAssert.True(travelFrames >= 2,
                "the 420-pixel shuttle remains in real Travel for multiple physics steps");
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "the real runtime latches the authored surface after shuttle travel");
            TestAssert.Equal(fixture.Surface, fixture.Claw.CurrentTarget,
                "the runtime exposes exactly one locked authored target");

            float initialRope = fixture.Claw.CurrentFrame.RopeLength;
            Vector2 beforeMove = fixture.Player.GlobalPosition;
            Step(fixture, Input(primaryHeld: true, reelAxis: -1f,
                mouseWorld: fixture.Surface.WorldAnchor), applyMotor: true);
            TestAssert.True(fixture.Claw.CurrentFrame.RopeLength < initialRope,
                "W reel input shortens the live rope");
            TestAssert.True(fixture.Player.GlobalPosition.X > beforeMove.X,
                "PlayerMotor applies the model velocity correction toward the anchor without teleporting");

            float beforeShield = fixture.Claw.CurrentFrame.RopeLength;
            Step(fixture, Input(primaryHeld: true, reelAxis: -1f, shieldHeld: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Near(beforeShield, fixture.Claw.CurrentFrame.RopeLength, .001f,
                "right-mouse shield input feeds ReelAxis zero while preserving the connection");
            TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Swing or MechanicalClawState.Reel,
                "shield does not detach an already connected claw");

            Step(fixture, Input(primaryReleased: true, mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "primary release publishes Released before retracting");
            await SettleToIdle(tree, fixture);
            TestAssert.True(fixture.Line.CurrentState == ThreadLineState.Idle && fixture.ClawVisual.IsClawClear,
                "release leaves no line or shuttle after the claw fully retracts");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task TargetUnloadFailsClosedAndClearsTheSingleClaw(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, "MechanicalClawUnload", 360f);
        try
        {
            Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
                primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            while (fixture.Claw.CurrentFrame.State != MechanicalClawState.Travel)
                Step(fixture, Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));

            Step(fixture, Input(primaryPressed: true, primaryHeld: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            TestAssert.Equal(MechanicalClawFailure.AlreadyDeployed, fixture.Claw.CurrentFrame.Failure,
                "a second primary press has active-claw priority and never allocates another cast");
            await SettleToIdle(tree, fixture);

            Step(fixture, Input(primaryPressed: true, primaryHeld: true,
                mouseWorld: fixture.Surface.WorldAnchor));
            while (fixture.Claw.CurrentFrame.State != MechanicalClawState.Travel)
                Step(fixture, Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
            while (fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched)
                Step(fixture, Input(primaryHeld: true, mouseWorld: fixture.Surface.WorldAnchor));
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
                primaryHeld: true, mouseWorld: emptyCursor));
            Step(fixture, Input(primaryHeld: true, mouseWorld: emptyCursor));

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
        frame = model.Step(frame, new MechanicalClawInput(true, true, false, false, 0f, 0f, false),
            target, Frame);
        frame = model.Step(frame, new MechanicalClawInput(false, true, false, false, 0f, 0f, false),
            target, Frame);
        return frame.Failure;
    }

    private static async Task<RuntimeFixture> CreateRuntimeFixture(SceneTree tree, string name, float range)
        => await CreateRuntimeFixture(tree, name, range, new Vector2(range, 0f));

    private static async Task<RuntimeFixture> CreateRuntimeFixture(SceneTree tree, string name, float range,
        Vector2 surfacePosition)
    {
        var root = new Node2D { Name = name };
        GrappleSurface2D surface = Surface("Surface", surfacePosition,
            GrappleSurfaceMaterial.DedicatedAnchor);
        root.AddChild(surface);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
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
        float range, Vector2 surfacePosition)
    {
        RuntimeFixture fixture = await CreateRuntimeFixture(tree, name, range, surfacePosition);
        fixture.Root.AddChild(WorldBlocker(new Vector2(0f, 60f), new Vector2(1600f, 20f)));
        fixture.Player.GlobalPosition = new Vector2(0f, 14f);
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
