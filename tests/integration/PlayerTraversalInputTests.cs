using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Shared;
using Knight.World;

namespace Knight.Tests.Integration;

public static class PlayerTraversalInputTests
{
    public static async Task RunAutoPullClawTask3(SceneTree tree)
    {
        await GrappleReleaseUsesObservedVelocityOnceRemovesInwardAndCaps(tree);
    }

    public static async Task Run(SceneTree tree)
    {
        await MotorJumpSuppressionConsumesExactlyOnePhysicsStep(tree);
        await GrappleReleaseUsesObservedVelocityOnceRemovesInwardAndCaps(tree);
        await SpaceUpDashClawThenDashRejectsSecondDash(tree);
        await SimultaneousSpaceUpDashClawThenDashRejectsSecondDash(tree);
        await SpaceClawDashThenDashRejectsSecondDash(tree);
        await AirDashClawThenDashRejectsSecondDash(tree);
        await AirDashPauseResumeThenDashRejectsSecondDash(tree);
        await AirDashWorldCollisionThenDashRejectsSecondDash(tree);
        await AirDashDownStrikeBounceThenDashRejectsSecondDash(tree);
        await AirDashRoomTransitionThenDashRejectsSecondDash(tree);
        await RealLandingRefundsExactlyOneNewAirDash(tree);
        await DashPressedDuringActiveDashPublishesRejection(tree);
        await GroundDashTakeoffPreservesOneAirDash(tree);
        GD.Print("TRAVERSAL_CASES_OK automatic_keyboard=11 scheduled_adapter=2 mouse=false floor_collision_preserved=true");
    }

    private static async Task MotorJumpSuppressionConsumesExactlyOnePhysicsStep(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "OneFrameJumpSuppression");
        try
        {
            PlayerInputReader input = fixture.Player.GetNode<PlayerInputReader>("Input");
            input.SetPhysicsProcess(false);
            fixture.Motor.SetPhysicsProcess(false);
            var jump = new PlayerInputFrame(0f, Vector2.Zero, true, true, false, false, false);
            typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
                BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, jump);
            fixture.Motor.SuppressJumpForCurrentPhysicsFrame();
            fixture.Motor._PhysicsProcess(1f / 60f);
            TestAssert.True(fixture.Player.IsOnFloor() && fixture.Player.Velocity.Y >= -1f,
                "suppression rewrites the motor's ordinary Space jump for one physics step");

            typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
                BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, jump);
            fixture.Motor._PhysicsProcess(1f / 60f);
            TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Player.Velocity.Y < 0f,
                "jump suppression clears after exactly one motor step");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task GrappleReleaseUsesObservedVelocityOnceRemovesInwardAndCaps(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "AtomicGrappleReleaseComposition");
        try
        {
            fixture.Motor.SetPhysicsProcess(false);
            fixture.Motor.ExternalImpulse(new Vector2(-700f, 100f));

            fixture.Motor.ApplyGrappleRelease(Vector2.Right, new Vector2(200f, -1200f),
                900f, preserveLaunchMomentum: true);

            TestAssert.True(fixture.Player.Velocity.DistanceTo(new Vector2(0f, -900f)) < .01f,
                "release starts from observed body velocity, adds boost once, removes inward face motion, then caps");
            TestAssert.True(fixture.Motor.HasGrappleReleaseMomentum,
                "the composed release preserves launch momentum only when requested");
            TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
                "the same release transaction clears the grapple movement constraint");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task SpaceUpDashClawThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "SpaceUpDashThread", withAnchor: true);
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp);
            await WaitFor(tree, () => !fixture.Motor.IsDashing, "finite upward burn ends before fresh K");
            await CastAndReleaseClaw(tree, fixture);
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "Space -> W+Shift -> W+K fixed-anchor claw -> Shift");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task SimultaneousSpaceUpDashClawThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "SimultaneousTraversal", withAnchor: true);
        try
        {
            ulong landingBefore = fixture.Motor.LandingCount;
            SetAction(InputActions.MoveUp, true);
            SetAction(InputActions.Jump, true);
            SetAction(InputActions.Dash, true);
            SetAction(InputActions.Claw, true);
            await Frames(tree, 1);
            TestAssert.Equal(1, fixture.Motor.AcceptedDashCount,
                SimultaneousDiagnostics(fixture, "same-frame W+Space+Shift+K accepts one short jet"));
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "accepted Shift consumes ordinary Space without buffering it");
            TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle &&
                fixture.Claw.CurrentFrame.NextCastId == 1UL,
                "same-frame accepted Shift suppresses K without starting an invisible claw cast");
            TestAssert.True(!fixture.Motor.AirDashAvailable && !fixture.Player.IsOnFloor(),
                "original same-frame Space consumes the air qualification before launch");
            SetAction(InputActions.Jump, false);
            SetAction(InputActions.Dash, false);
            SetAction(InputActions.Claw, false);
            SetAction(InputActions.MoveUp, false);
            await WaitFor(tree, () => !fixture.Motor.IsDashing, "simultaneous launch finite burn completion");
            TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Motor.LandingCount == landingBefore,
                "real floor remains enabled and no new landing refunds the combined launch");
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "same-frame W+Space+Shift+K -> later Shift");
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "consumed Space does not replay after burn");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static string SimultaneousDiagnostics(RuntimeFixture fixture, string expectation) =>
        $"{expectation}; floor={fixture.Player.IsOnFloor()}, position={fixture.Player.Position}, " +
        $"velocity={fixture.Player.Velocity}, accepted={fixture.Motor.AcceptedDashCount}, " +
        $"rejected={fixture.Motor.RejectedDashCount}, available={fixture.Motor.AirDashAvailable}, " +
        $"dashing={fixture.Motor.IsDashing}, claw={fixture.Claw.CurrentFrame.State}/" +
        $"{fixture.Claw.CurrentFrame.Failure}";

    private static async Task WaitForSimultaneous(SceneTree tree, RuntimeFixture fixture,
        Func<bool> predicate, string expectation)
    {
        for (int index = 0; index < 180; index++)
        {
            if (predicate()) return;
            await Frames(tree, 1);
        }
        throw new InvalidOperationException(SimultaneousDiagnostics(fixture,
            $"timed out waiting for {expectation}"));
    }

    private static async Task SpaceClawDashThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "SpaceThreadDashDash", withAnchor: true);
        try
        {
            await TakeOff(tree, fixture);
            await CastAndReleaseClaw(tree, fixture);
            TestAssert.Equal(0, fixture.Motor.AcceptedDashCount, SimultaneousDiagnostics(fixture, "no short jet before explicit first Shift"));
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp);
            TestAssert.True(!fixture.Motor.AirDashAvailable && !fixture.Player.IsOnFloor(),
                SimultaneousDiagnostics(fixture, "claw then first upward Shift consumes airborne qualification"));
            await WaitFor(tree, () => !fixture.Motor.IsDashing, "finite burn ends after claw release");
            TestAssert.True(!fixture.Player.IsOnFloor(), SimultaneousDiagnostics(fixture,
                "claw then upward short jet remains in the original airborne cycle"));
            TestAssert.True(!fixture.Motor.AirDashAvailable, SimultaneousDiagnostics(fixture,
                "no real intervening landing refunded the first upward Shift"));
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "Space -> W+K fixed-anchor claw -> Shift -> Shift");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AirDashClawThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "DashThreadApply", withAnchor: true);
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp);
            await WaitFor(tree, () => !fixture.Motor.IsDashing, "finite airborne burn ends before fresh K");
            await CastAndReleaseClaw(tree, fixture);
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "air dash -> real MechanicalClaw -> Shift");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AirDashPauseResumeThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "DashPauseResume");
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp);
            await Frames(tree, 10);
            SetAction(InputActions.Pause, true);
            tree.Paused = true;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Paused = false;
            SetAction(InputActions.Pause, false);
            await Frames(tree, 1);
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "air dash -> pause/resume -> Shift");
        }
        finally
        {
            tree.Paused = false;
            await Dispose(tree, fixture.Root);
        }
    }

    private static async Task AirDashWorldCollisionThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "DashWorldCollision", withCollisionCorner: true);
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp, InputActions.MoveRight);
            string colliderName = await WaitForIntendedWorldCollision(tree, fixture.Player);
            TestAssert.True(colliderName is "SideWall" or "Ceiling",
                $"real slide collision names the intended fixture collider (actual={colliderName})");
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "air dash -> wall/ceiling/side collision -> Shift");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AirDashDownStrikeBounceThenDashRejectsSecondDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "DashDownStrikeBounce", withBounceTarget: true);
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp);
            await WaitFor(tree, () => !fixture.Motor.IsDashing, "air dash completion before down-strike");
            await WaitFor(tree, () => fixture.Player.Velocity.Y > 0f &&
                fixture.Player.Position.Y >= -180f && fixture.Player.Position.Y <= -150f,
                "real descending body enters the fixed down-strike target's approach window");
            SetAction(InputActions.MoveDown, true);
            await Frames(tree, 1);
            SetAction(InputActions.Attack, true);
            Hitbox hitbox = fixture.Player.GetNode<Hitbox>("Combat/AttackHitbox");
            await WaitFor(tree, () => hitbox.Monitoring, "real down-strike active window");
            TestAssert.True(hitbox.Position.Y > 0f,
                "real InputMap down-strike places the active hitbox below the airborne player");
            await WaitFor(tree, () => fixture.BounceTargetHealth.CurrentHealth == 4,
                "real Hitbox Connected damage on the bounce target");
            TestAssert.True(fixture.Player.Velocity.Y < 0f,
                "PlayerCombat.OnHitboxConnected applies the production down-strike bounce");
            SetAction(InputActions.Attack, false);
            SetAction(InputActions.MoveDown, false);
            await Frames(tree, 1);
            await Frames(tree, 8);
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "air dash -> down-strike bounce/knockback -> Shift");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AirDashRoomTransitionThenDashRejectsSecondDash(SceneTree tree)
    {
        ReleaseInput();
        await Frames(tree, 1);
        CharacterBody2D player = PlayerScene();
        PrologueLevelController level = CreateTransitionLevel(player);
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 3);
            PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
            await PressTogether(tree, InputActions.MoveRight, InputActions.Dash);
            await WaitFor(tree, () => level.CurrentRoomId == PrologueRoomId.BrokenToothCorridor,
                "real airborne RoomExit transition to the floorless destination");
            await Frames(tree, 12);
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(motor, "air dash -> no-floor room transition -> Shift");
        }
        finally { await Dispose(tree, level); }
    }

    private static async Task RealLandingRefundsExactlyOneNewAirDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "AuthoritativeLanding", floorY: 260f);
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveUp);
            await Frames(tree, 10);
            await Tap(tree, InputActions.Dash);
            AssertRejectedBeforeLanding(fixture.Motor, "pre-landing airborne cycle");
            await WaitFor(tree, () => fixture.Player.IsOnFloor(), "real StaticBody2D landing");
            await TakeOff(tree, fixture);
            await Tap(tree, InputActions.Dash);
            TestAssert.Equal(2, fixture.Motor.AcceptedDashCount,
                "authoritative landing refunds exactly one dash for the next airborne cycle");
            await Frames(tree, 10);
            await Tap(tree, InputActions.Dash);
            TestAssert.Equal(2, fixture.Motor.AcceptedDashCount,
                "the refunded airborne dash can still only be consumed once");
            TestAssert.True(fixture.Motor.RejectedDashCount >= 2,
                "both airborne cycles publish their rejected second press");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task DashPressedDuringActiveDashPublishesRejection(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "ActiveDashRepeatedPress");
        try
        {
            await TakeOff(tree, fixture);
            await DashWithAim(tree, fixture.Motor, InputActions.MoveRight);
            TestAssert.True(fixture.Motor.IsDashing, "real InputMap first press leaves the dash active");
            Vector2 activeDashVelocity = fixture.Player.Velocity;
            await Tap(tree, InputActions.Dash);

            TestAssert.Equal(1, fixture.Motor.AcceptedDashCount,
                "a repeated press during active dash does not publish another successful transition");
            TestAssert.Equal(1, fixture.Motor.RejectedDashCount,
                "a repeated press during active dash publishes one rejected transition");
            float drag = Mathf.Exp(-1.6f / 60f);
            float expectedX = (activeDashVelocity.X * drag + 5600f / 60f) * drag + 5600f / 60f;
            TestAssert.Near(expectedX, fixture.Player.Velocity.X, .004f,
                "rejected Shift still integrates two legal air-drag/thrust ticks instead of freezing velocity");
            TestAssert.Near(activeDashVelocity.Y + 1850f * 2f / 60f, fixture.Player.Velocity.Y, .004f,
                "rejected Shift still integrates two legal gravity ticks");
            TestAssert.True(!fixture.Motor.AirDashAvailable,
                "active-dash rejection leaves the airborne budget consumed");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task GroundDashTakeoffPreservesOneAirDash(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "GroundDashThenAirDash");
        try
        {
            await Tap(tree, InputActions.Dash);
            TestAssert.Equal(1, fixture.Motor.AcceptedDashCount, "real grounded Shift starts one ground dash");
            TestAssert.True(fixture.Motor.AirDashAvailable,
                "ground dash leaves the next airborne-cycle dash available");
            await WaitFor(tree, () => !fixture.Motor.IsDashing, "ground dash completion");

            await TakeOff(tree, fixture);
            await Tap(tree, InputActions.Dash);
            TestAssert.Equal(2, fixture.Motor.AcceptedDashCount,
                "after real takeoff the preserved air dash succeeds exactly once");
            await Tap(tree, InputActions.Dash);
            TestAssert.Equal(2, fixture.Motor.AcceptedDashCount,
                "the next airborne press does not start a third dash");
            TestAssert.True(fixture.Motor.RejectedDashCount >= 1,
                "the next airborne press publishes rejected feedback");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static void AssertRejectedBeforeLanding(PlayerMotor motor, string sequence)
    {
        TestAssert.Equal(1, motor.AcceptedDashCount, $"{sequence} accepts exactly one airborne dash");
        TestAssert.True(motor.RejectedDashCount >= 1, $"{sequence} publishes a rejected second dash");
        TestAssert.True(!motor.AirDashAvailable, $"{sequence} keeps the airborne dash consumed");
    }

    private static async Task<RuntimeFixture> CreateFixture(SceneTree tree, string name,
        bool withAnchor = false, bool withCollisionCorner = false, bool withBounceTarget = false,
        float floorY = 60f)
    {
        ReleaseInput();
        await Frames(tree, 1);
        var root = new Node2D { Name = name };
        CharacterBody2D player = PlayerScene();
        player.Position = new Vector2(0f, floorY - 46f);
        root.AddChild(CreateWorldBody("LandingFloor", new Vector2(0f, floorY), new Vector2(1000f, 20f)));
        if (withCollisionCorner)
        {
            root.AddChild(CreateWorldBody("SideWall", new Vector2(105f, 0f), new Vector2(20f, 300f)));
            root.AddChild(CreateWorldBody("Ceiling", new Vector2(0f, -105f), new Vector2(300f, 20f)));
        }
        if (withAnchor)
        {
            root.AddChild(CreateClawSurface());
        }
        HealthComponent bounceTargetHealth = null;
        if (withBounceTarget)
        {
            Node2D bounceTarget = CreateBounceTarget();
            bounceTargetHealth = bounceTarget.GetNode<HealthComponent>("Health");
            root.AddChild(bounceTarget);
        }
        root.AddChild(player);
        tree.Root.AddChild(root);
        await WaitFor(tree, () => player.IsOnFloor(), $"{name} real landed fixture start");
        TestAssert.True(player.IsOnFloor(), $"{name} begins on the real StaticBody2D floor");
        return new RuntimeFixture(root, player, player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"), bounceTargetHealth);
    }

    private static CharacterBody2D PlayerScene() =>
        GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();

    private static StaticBody2D CreateWorldBody(string name, Vector2 position, Vector2 size)
    {
        var body = new StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = size } });
        return body;
    }

    private static Node2D CreateBounceTarget()
    {
        var target = new Node2D { Name = "BounceTarget", Position = new Vector2(0f, -45f) };
        target.AddChild(new HealthComponent { Name = "Health", MaxHealth = 5, InvulnerabilitySeconds = 0f });
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(80f, 30f) }
        });
        target.AddChild(hurtbox);
        return target;
    }

    private static PrologueLevelController CreateTransitionLevel(CharacterBody2D player)
    {
        RoomDefinition roomA = TransitionDefinition(PrologueRoomId.AwakeningCourtyard,
            TransitionRoom(PrologueRoomId.AwakeningCourtyard, true));
        RoomDefinition roomB = TransitionDefinition(PrologueRoomId.BrokenToothCorridor,
            TransitionRoom(PrologueRoomId.BrokenToothCorridor, false));
        roomA.OutgoingLinks.Add(new PrologueRoomLinkResource
        {
            SourceRoom = PrologueRoomId.AwakeningCourtyard,
            ExitId = "east",
            DestinationRoom = PrologueRoomId.BrokenToothCorridor,
            EntranceId = "start"
        });
        var level = new PrologueLevelController
        {
            Name = "TraversalTransitionLevel",
            RoomDefinitions = [roomA, roomB],
            RequiredRooms = [PrologueRoomId.AwakeningCourtyard]
        };
        var playerRoot = new Node2D { Name = "PlayerRoot" };
        player.Name = "Player";
        playerRoot.AddChild(player);
        level.AddChild(playerRoot);
        level.AddChild(new Node2D { Name = "RoomRoot" });
        level.AddChild(new CanvasLayer { Name = "Hud" });
        return level;
    }

    private static RoomDefinition TransitionDefinition(PrologueRoomId id, PackedScene scene) => new()
    {
        RoomId = id,
        Scene = scene,
        StartEntrance = "start",
        CameraBounds = new Rect2(-640f, -360f, 1280f, 720f)
    };

    private static PackedScene TransitionRoom(PrologueRoomId id, bool withExit)
    {
        var room = new PrologueRoom
        {
            Name = id.ToString(),
            RoomId = id,
            CameraBounds = new Rect2(-640f, -360f, 1280f, 720f)
        };
        var entrances = new Node2D { Name = "Entrances" };
        entrances.AddChild(new Marker2D { Name = "start", Position = Vector2.Zero });
        room.AddChild(entrances);
        var exits = new Node2D { Name = "Exits" };
        if (withExit)
        {
            var exit = new RoomExit
            {
                Name = "EastExit",
                ExitId = "east",
                Position = new Vector2(85f, 0f),
                CollisionLayer = CombatCollisionLayers.None,
                CollisionMask = CombatCollisionLayers.Player,
                Monitoring = true
            };
            exit.AddChild(new CollisionShape2D
            {
                Name = "Collision",
                Shape = new RectangleShape2D { Size = new Vector2(30f, 140f) }
            });
            exits.AddChild(exit);
        }
        room.AddChild(exits);
        var kill = new KillZone
        {
            Name = "KillZone",
            Position = new Vector2(10000f, 10000f),
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player
        };
        kill.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(20f, 20f) }
        });
        room.AddChild(kill);
        var presentation = new Node2D { Name = "Presentation" };
        var visual = new Node2D { Name = "VisualRoot" };
        visual.AddChild(new Node2D { Name = "Placeholder" });
        presentation.AddChild(visual);
        room.AddChild(presentation);
        // A valid typed production presentation is required even in this tiny
        // floorless transition fixture; do not enable diagnostic fallbacks.
        room.AddChild(new RoomFormalVisual { Name = "Art", Room = id });
        Own(room, room);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(room), "floorless traversal transition room packs");
        room.Free();
        return packed;
    }

    private static void Own(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            Own(child, owner);
        }
    }

    private static async Task CastAndReleaseClaw(SceneTree tree, RuntimeFixture fixture)
    {
        ulong landingBefore = fixture.Motor.LandingCount;
        SetAction(InputActions.MoveUp, true);
        await Frames(tree, 1);
        GrappleSurface2D surface = fixture.Root.GetNode<GrappleSurface2D>("ClawSurface");
        TestAssert.True(fixture.Claw.KeyboardPreview.Surface == surface &&
            fixture.Claw.KeyboardPreview.Snapshot.HasLineOfSight,
            SimultaneousDiagnostics(fixture, "real W preview selects the fixed authored anchor with LOS"));
        ulong nextCast = fixture.Claw.CurrentFrame.NextCastId;
        SetAction(InputActions.Claw, true);
        await Frames(tree, 1);
        SetAction(InputActions.Claw, false);
        TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Aim &&
            fixture.Claw.CurrentFrame.NextCastId == nextCast,
            SimultaneousDiagnostics(fixture, "fresh physical K begins a new cast"));
        for (int index = 0; index < 180 && fixture.Claw.CurrentFrame.State is not
            (MechanicalClawState.Latched or MechanicalClawState.Swing or MechanicalClawState.Reel); index++)
        {
            TestAssert.True(fixture.Claw.CurrentFrame.State is not MechanicalClawState.Failed and not MechanicalClawState.Idle,
                SimultaneousDiagnostics(fixture, "fixed-anchor keyboard cast must not fail before attaching"));
            await Frames(tree, 1);
        }
        if (fixture.Claw.CurrentFrame.State is not
            (MechanicalClawState.Latched or MechanicalClawState.Swing or MechanicalClawState.Reel))
        {
            throw new InvalidOperationException(
                $"Timed out waiting for real MechanicalClaw latch through W+K fixed anchor; " +
                $"state={fixture.Claw.CurrentFrame.State}, failure={fixture.Claw.CurrentFrame.Failure}, " +
                $"player={fixture.Player.GlobalPosition}, surface={surface.WorldAnchor}, " +
                $"landingBefore={landingBefore}, landingNow={fixture.Motor.LandingCount}");
        }
        SetAction(InputActions.Claw, true);
        await Frames(tree, 1);
        SetAction(InputActions.Claw, false);
        SetAction(InputActions.MoveUp, false);
        await WaitFor(tree, () => fixture.Claw.CurrentFrame.State is MechanicalClawState.Released or
            MechanicalClawState.Retract,
            "fresh physical K cancellation enters bounded retract without waiting through a landing");
        TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Motor.LandingCount == landingBefore,
            "real fixed-anchor cast/release preserves the same airborne cycle with floor collision still enabled");
    }

    private static GrappleSurface2D CreateClawSurface()
    {
        var surface = new GrappleSurface2D
        {
            Name = "ClawSurface",
            Position = new Vector2(0f, -260f),
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
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
            Shape = new CircleShape2D { Radius = 18f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static async Task TakeOff(SceneTree tree, RuntimeFixture fixture)
    {
        TestAssert.True(fixture.Player.IsOnFloor(), "Space takeoff starts from an authoritative landed state");
        SetAction(InputActions.Jump, false);
        await Frames(tree, 1);
        SetAction(InputActions.Jump, true);
        await WaitFor(tree, () => !fixture.Player.IsOnFloor() && fixture.Player.Velocity.Y < 0f,
            "real Space takeoff and left-floor edge");
        await Frames(tree, 1);
        TestAssert.True(!fixture.Player.IsOnFloor(), "held ordinary Space establishes a real airborne traversal window");
    }

    private static async Task DashWithAim(SceneTree tree, PlayerMotor motor, params StringName[] directions)
    {
        int acceptedBefore = motor.AcceptedDashCount;
        foreach (StringName direction in directions) SetAction(direction, true);
        await Frames(tree, 1);
        SetAction(InputActions.Dash, true);
        await WaitFor(tree, () => motor.AcceptedDashCount == acceptedBefore + 1,
            "real InputMap dash acceptance");
        SetAction(InputActions.Dash, false);
        foreach (StringName direction in directions) SetAction(direction, false);
        await Frames(tree, 1);
    }

    private static async Task<string> WaitForIntendedWorldCollision(SceneTree tree, CharacterBody2D player)
    {
        for (int frame = 0; frame < 30; frame++)
        {
            await Frames(tree, 1);
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
            {
                KinematicCollision2D collision = player.GetSlideCollision(index);
                if (collision.GetCollider() is not Node collider) continue;
                string colliderName = collider.Name.ToString();
                if (colliderName is "SideWall" or "Ceiling") return colliderName;
            }
        }
        throw new InvalidOperationException(
            $"Timed out waiting for intended wall/ceiling slide collision; wall={player.IsOnWall()}, ceiling={player.IsOnCeiling()}");
    }

    private static async Task Tap(SceneTree tree, StringName action, int heldFrames = 1)
    {
        SetAction(action, true);
        await Frames(tree, heldFrames);
        SetAction(action, false);
        await Frames(tree, 1);
    }

    private static async Task PressTogether(SceneTree tree, params StringName[] actions)
    {
        foreach (StringName action in actions) SetAction(action, true);
        await Frames(tree, 1);
        foreach (StringName action in actions) SetAction(action, false);
        await Frames(tree, 1);
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> predicate, string description)
    {
        for (int index = 0; index < 180; index++)
        {
            if (predicate()) return;
            await Frames(tree, 1);
        }
        throw new InvalidOperationException($"Timed out waiting for {description}");
    }

    private static async Task Dispose(SceneTree tree, Node root)
    {
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void ReleaseInput()
    {
        ThrusterPlayerRuntimeTests.Release();
        SetAction(InputActions.MoveLeft, false);
        SetAction(InputActions.MoveRight, false);
        SetAction(InputActions.MoveUp, false);
        SetAction(InputActions.MoveDown, false);
        SetAction(InputActions.Jump, false);
        SetAction(InputActions.Dash, false);
        SetAction(InputActions.Attack, false);
        SetAction(InputActions.Claw, false);
        SetAction(InputActions.KeyboardShield, false);
        SetAction(InputActions.Pause, false);
    }

    private sealed record RuntimeFixture(
        Node2D Root,
        CharacterBody2D Player,
        PlayerMotor Motor,
        MechanicalClawController Claw,
        HealthComponent BounceTargetHealth);

    private static void SetAction(StringName action, bool pressed)
    {
        Key key = action.ToString() switch
        {
            "move_left" => Key.A, "move_right" => Key.D, "move_up" => Key.W, "move_down" => Key.S,
            "jump" => Key.Space, "dash" => Key.Shift, "claw" => Key.K, "attack" => Key.J,
            "keyboard_shield" => Key.L, "pause" => Key.Escape,
            _ => throw new InvalidOperationException("Unsupported physical traversal action: " + action)
        };
        ThrusterPlayerRuntimeTests.KeyEvent(key, pressed);
    }
}
