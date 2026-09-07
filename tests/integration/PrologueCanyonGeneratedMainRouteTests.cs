using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Shared;

namespace Knight.Tests.Integration;

/// <summary>
/// Runs through the exact generated PrologueLevel with one persistent production player. After
/// the pre-tree spawn is selected, this proof advances only through real InputMap events and
/// physics frames; it never writes player position or velocity and never calls stream transitions.
/// </summary>
public static class PrologueCanyonGeneratedMainRouteTests
{
    private const string MainPath = "res://scenes/prologue/PrologueLevel.tscn";
    private const int StableLandingFrames = 12;
    private const float LandingMarginRatio = .15f;
    private static readonly PrologueSectorId[] OrderedRoute = Enum.GetValues<PrologueSectorId>();

    public static async Task<bool> Run(SceneTree tree, bool captureFrames)
        => await Run(tree, captureFrames, observer: null);

    internal static async Task<bool> Run(SceneTree tree, bool captureFrames,
        ITask9GeneratedMainMilestoneObserver observer)
    {
        ReleaseAll(Vector2.Zero);
        PackedScene packed = GD.Load<PackedScene>(MainPath) ??
            throw new InvalidOperationException($"exact generated main scene is missing: {MainPath}");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        level.Name = "Task8Fix1GeneratedMainProof";
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = new Vector2(120f, 574f); // sole pre-tree spawn assignment
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        flow.SavePath = $"user://task8-fix1-proof/{Guid.NewGuid():N}/save.json";
        flow.SettingsPath = $"user://task8-fix1-proof/{Guid.NewGuid():N}/settings.json";
        observer?.Configure(level);
        Camera2D camera = player.GetNode<Camera2D>("Camera2D");
        camera.Enabled = true;
        ulong playerId = player.GetInstanceId();
        tree.Root.AddChild(level);
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var monitor = new RouteMonitor(level, player, camera, world, playerId);
        try
        {
            await PhysicsFrames(tree, monitor, 5);
            monitor.AssertCurrent(PrologueSectorId.CanyonEntrance);
            await Capture(tree, captureFrames, "01-entrance");
            if (observer != null)
                await observer.BeforeBasin(new Task9GeneratedMainContext(level, player, world,
                    count => PhysicsFrames(tree, monitor, count)));

            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => world.CurrentSector == PrologueSectorId.CanyonDescent,
                maxFrames: 520, jumpPeriod: 52);
            await StableSector(tree, monitor, PrologueSectorId.CanyonDescent, "02-descent",
                captureFrames);
            if (observer != null)
                await observer.AtDescent(new Task9GeneratedMainContext(level, player, world,
                    count => PhysicsFrames(tree, monitor, count)));
            if (DisplayServer.GetName() == "headless" && observer == null)
            {
                GD.Print($"TASK8_GENERATED_MAIN_HEADLESS_STREAM_OK current={world.CurrentSector} " +
                    $"frames={monitor.Frames} mounted={string.Join(",", world.MountedSectorIds)} " +
                    $"max_step={monitor.MaxStep:0.00} recovery_requested={monitor.RecoveryRequested} " +
                    $"recovery_completed={monitor.RecoveryCompleted} pointer_boundary=DescentGrapple");
                monitor.AssertNoProductionRecovery("headless generated-main stream boundary");
                return false;
            }

            PrologueSector descent = Mounted(world, PrologueSectorId.CanyonDescent);
            await GrappleToLanding(tree, monitor, descent,
                "descent_grip_lesson", maxFrames: 220);
            if (observer != null)
                await observer.AfterDescentGrapple(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));
            if (observer != null)
            {
                await DriveHorizontalUntil(tree, monitor, moveRight: true,
                    stop: () => world.CurrentSector == PrologueSectorId.CanyonDescent &&
                        player.GlobalPosition.Y >= 1360f,
                    maxFrames: 600, jumpPeriod: 58);
                await observer.BeforeBasinSeam(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));
            }
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => world.CurrentSector == PrologueSectorId.CanyonBasin,
                maxFrames: 720, jumpPeriod: 58);
            await StableSector(tree, monitor, PrologueSectorId.CanyonBasin, "03-basin",
                captureFrames);
            if (observer != null)
                await observer.AtBasinBeforeShieldLane(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));

            await WaitForFloor(tree, monitor, 300);
            await DriveHorizontalUntil(tree, monitor, moveRight: false,
                stop: () => player.GlobalPosition.X <= 2040f, maxFrames: 240, jumpPeriod: 0);
            int basinHealth = player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth;
            SendMouse(MouseButton.Right, true, player, player.GlobalPosition + Vector2.Right * 240f);
            await PhysicsFrames(tree, monitor, 3);
            TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                "continuous reverse approach raises the real source-facing shield before the lane");
            await DriveHorizontalUntil(tree, monitor, moveRight: false,
                stop: () => player.GlobalPosition.X <= 1560f, maxFrames: 300, jumpPeriod: 0,
                mouseAim: () => player.GlobalPosition + Vector2.Right * 240f);
            SendMouse(MouseButton.Right, false, player, player.GlobalPosition + Vector2.Right * 240f);
            TestAssert.Equal(basinHealth,
                player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth,
                "source-facing shield prevents damage on the returnable reverse lane crossing");
            PlayerEnergyController basinEnergy = player.GetNode<PlayerEnergyController>("Energy");
            for (int frame = 0; frame < 360 &&
                 (basinEnergy.EnergyRatio < .98f || basinEnergy.HeatRatio > .02f); frame++)
                await PhysicsFrames(tree, monitor, 1);
            TestAssert.True(basinEnergy.EnergyRatio >= .98f && basinEnergy.HeatRatio <= .02f,
                "continuous route waits for visible shield cooldown/regeneration before returning");
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => player.GlobalPosition.X >= 1600f,
                maxFrames: 80, jumpPeriod: 0);
            SendMouse(MouseButton.Right, true, player, player.GlobalPosition + Vector2.Right * 100f);
            await PhysicsFrames(tree, monitor, 3);
            TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                "continuous mandatory crossing raises the regenerated right-facing shield");
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => player.GlobalPosition.X >= 2000f,
                maxFrames: 320, jumpPeriod: 0,
                mouseAim: () => player.GlobalPosition + Vector2.Right * 240f);
            SendMouse(MouseButton.Right, false, player, player.GlobalPosition + Vector2.Right * 240f);
            TestAssert.Equal(basinHealth,
                player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth,
                "source-facing shield prevents damage on the mandatory forward lane crossing");
            if (observer != null)
            {
                await DriveHorizontalUntil(tree, monitor, moveRight: true,
                    stop: () => level.Progress.RestoreCheckpoint() is
                    { HasActivatedCheckpoint: true, SectorId: PrologueSectorId.CanyonBasin },
                    maxFrames: 240, jumpPeriod: 0);
                await observer.AfterBasinCheckpoint(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));
            }
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => world.CurrentSector == PrologueSectorId.CanyonAscent,
                maxFrames: 560, jumpPeriod: 60);
            SendMouse(MouseButton.Right, false, player, player.GlobalPosition);
            await StableSector(tree, monitor, PrologueSectorId.CanyonAscent, "04-ascent",
                captureFrames);

            PrologueSector ascent = Mounted(world, PrologueSectorId.CanyonAscent);
            foreach (string segment in new[]
                     { "ascent_grapple_one", "ascent_grapple_two", "ascent_grapple_three" })
                await GrappleToLanding(tree, monitor, ascent, segment, maxFrames: 300);
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => player.GlobalPosition.X >= 4080f,
                maxFrames: 320, jumpPeriod: 0);
            SendAction(InputActions.MoveRight, true);
            PulseJump(true);
            await PhysicsFrames(tree, monitor, 8);
            PulseJump(false);
            for (int frame = 0; frame < 120 &&
                 world.CurrentSector != PrologueSectorId.LostTemple; frame++)
                await PhysicsFrames(tree, monitor, 1);
            SendAction(InputActions.MoveRight, false);
            TestAssert.Equal(PrologueSectorId.LostTemple, world.CurrentSector,
                "one real Space press clears the authored 82-pixel final seam step into Temple");
            await StableSector(tree, monitor, PrologueSectorId.LostTemple, "05-temple",
                captureFrames);

            PrologueSector temple = Mounted(world, PrologueSectorId.LostTemple);
            BellChainMechanism chain = temple.GetNode<BellChainMechanism>(
                "Gameplay/Mechanisms/BellChain");
            GrappleSurface2D bellTarget = chain.GetNode<GrappleSurface2D>("ThreadTarget");
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => player.GlobalPosition.X >= 4840f, maxFrames: 320, jumpPeriod: 45);
            await CastAt(tree, monitor, player, bellTarget, () => chain.IsOpen, 150);
            TestAssert.True(chain.IsOpen,
                "continuous generated-main route opens BellChain through the real E+mouse cast");
            GD.Print($"TASK8_GENERATED_MAIN_BELL_OK target={bellTarget.WorldAnchor} " +
                $"player={player.GlobalPosition} open={chain.IsOpen}");
            if (observer != null)
            {
                await DriveHorizontalUntil(tree, monitor, moveRight: true,
                    stop: () => level.Progress.RestoreCheckpoint() is
                    { HasActivatedCheckpoint: true, SectorId: PrologueSectorId.LostTemple },
                    maxFrames: 240, jumpPeriod: 0);
                await observer.AfterTempleCheckpoint(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));
            }
            await DriveHorizontalUntil(tree, monitor, moveRight: true,
                stop: () => temple.GetNode<BossArenaController>(
                    "Gameplay/BossArenaController").IsEncounterActive,
                maxFrames: 520, jumpPeriod: 62);
            BossArenaController boss = temple.GetNode<BossArenaController>(
                "Gameplay/BossArenaController");
            TestAssert.True(boss.IsEncounterActive,
                "continuous generated-main route reaches the real physical boss trigger");
            await Capture(tree, captureFrames, "06-boss-trigger");
            if (observer != null)
            {
                await observer.AfterBossTrigger(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));
                await DriveHorizontalUntil(tree, monitor, moveRight: true,
                    stop: () => boss.IsEncounterActive,
                    maxFrames: 360, jumpPeriod: 54);
                await DriveHorizontalUntil(tree, monitor, moveRight: true,
                    stop: () => player.GlobalPosition.X >= 5600f,
                    maxFrames: 180, jumpPeriod: 0);
                await observer.AfterBossRetry(new Task9GeneratedMainContext(level, player,
                    world, count => PhysicsFrames(tree, monitor, count)));
            }

            monitor.AssertComplete();
            if (observer == null)
                monitor.AssertNoProductionRecovery("continuous generated-main route");
            else
                monitor.AssertTask9Recoveries("Task 9 generated-main route", expected: 4);
            TestAssert.Equal(0, player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,
                "generated-main route uses no Dash");
            GD.Print($"TASK8_GENERATED_MAIN_ROUTE_OK sectors={string.Join(",", monitor.Visited)} " +
                $"seams=4 max_step={monitor.MaxStep:0.00} player_id={playerId} " +
                $"mounted_max={monitor.MaxMounted} recovery_requested={monitor.RecoveryRequested} " +
                $"recovery_completed={monitor.RecoveryCompleted} boss_trigger=True capture={captureFrames}");
            return true;
        }
        finally
        {
            monitor.Dispose();
            ReleaseAll(player.GlobalPosition);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task StableSector(SceneTree tree, RouteMonitor monitor,
        PrologueSectorId expected, string captureName, bool captureFrames)
    {
        await PhysicsFrames(tree, monitor, 12);
        monitor.AssertCurrent(expected);
        monitor.AssertCameraCoverage();
        await Capture(tree, captureFrames, captureName);
        GD.Print($"TASK8_GENERATED_MAIN_SEAM_OK current={expected} frames={monitor.Frames} " +
            $"mounted={string.Join(",", monitor.World.MountedSectorIds)} " +
            $"max_step={monitor.MaxStep:0.00} recovery_requested={monitor.RecoveryRequested} " +
            $"recovery_completed={monitor.RecoveryCompleted}");
    }

    private static PrologueSector Mounted(SeamlessPrologueWorld world, PrologueSectorId id) =>
        world.GetChildren().OfType<PrologueSector>().Single(sector => sector.SectorId == id);

    private static async Task GrappleToLanding(SceneTree tree, RouteMonitor monitor,
        PrologueSector sector, string segmentId, int maxFrames)
    {
        TraversalSegment2D segment = sector.GetNode<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{segmentId}");
        Rect2 launch = new(segment.LaunchZone.Position + sector.GlobalPosition,
            segment.LaunchZone.Size);
        Rect2 landing = new(segment.LandingZone.Position + sector.GlobalPosition,
            segment.LandingZone.Size);
        GrappleSurface2D target = sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
        MechanicalClawController claw = monitor.Player.GetNode<MechanicalClawController>(
            "MechanicalClaw");
        int casts = 0;
        int latches = 0;
        claw.ClawFrameChanged += Observe;
        try
        {
            if (monitor.Player.GlobalPosition.X < launch.GetCenter().X - 8f)
                await DriveHorizontalUntil(tree, monitor, moveRight: true,
                    stop: () => monitor.Player.GlobalPosition.X >= launch.GetCenter().X - 8f,
                    maxFrames: 160, jumpPeriod: 0);
            else if (monitor.Player.GlobalPosition.X > launch.GetCenter().X + 8f)
                await DriveHorizontalUntil(tree, monitor, moveRight: false,
                    stop: () => monitor.Player.GlobalPosition.X <= launch.GetCenter().X + 8f,
                    maxFrames: 160, jumpPeriod: 0);
            TestAssert.True(launch.HasPoint(monitor.Player.GlobalPosition),
                $"continuous route enters the authored {segmentId} launch zone by real movement; " +
                $"position={monitor.Player.GlobalPosition} launch={launch}");
            int jumpsBefore = monitor.Player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.MoveUp, true);
            PulseJump(true);
            int visibilityWaitFrames = await WaitForVisibleTargetDuringJump(tree, monitor,
                target, segmentId, jumpsBefore);
            MoveMouse(monitor.Player, target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, monitor, 2);
            SendAction(InputActions.SelectGrappleMode, false);
            PlayerInputReader sampled = monitor.Player.GetNode<PlayerInputReader>("Input");
            TestAssert.True(IsTargetVisible(monitor.Player, target.WorldAnchor),
                $"{segmentId}: grapple target remains on-screen at the real LMB press; " +
                $"viewport_target={ToViewport(monitor.Player, target.WorldAnchor)}");
            GD.Print($"TASK8_GENERATED_MAIN_GRAPPLE_INPUT segment={segmentId} " +
                $"player={monitor.Player.GlobalPosition} target={target.WorldAnchor} " +
                $"distance={monitor.Player.GlobalPosition.DistanceTo(target.WorldAnchor):0.0} " +
                $"mouse={sampled.Current.MouseWorld} viewport_target={ToViewport(monitor.Player, target.WorldAnchor)} " +
                $"canvas={monitor.Player.GetViewport().GetCanvasTransform()} " +
                $"final={monitor.Player.GetViewport().GetFinalTransform()} " +
                $"visible=True visibility_wait_frames={visibilityWaitFrames} mode=" +
                $"{monitor.Player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode}");
            SendMouse(MouseButton.Left, true, monitor.Player, target.WorldAnchor);
            int stable = 0;
            bool released = false;
            for (int frame = 0; frame < maxFrames; frame++)
            {
                if (frame == 8) PulseJump(false);
                // Once the claw is locked, a real player may keep holding LMB while the
                // target leaves the screen. Do not manufacture an off-screen mouse move.
                if (IsTargetVisible(monitor.Player, target.WorldAnchor))
                    MoveMouse(monitor.Player, target.WorldAnchor);
                await PhysicsFrames(tree, monitor, 1);
                if (monitor.Player.GlobalPosition.X >= landing.GetCenter().X)
                    SendAction(InputActions.MoveRight, false);
                if (!released && latches > 0 &&
                    monitor.Player.GlobalPosition.Y <= landing.GetCenter().Y &&
                    PredictLandingX(monitor.Player, landing.GetCenter().Y) is float predicted &&
                    predicted >= landing.Position.X && predicted <= landing.End.X)
                {
                    SendAction(InputActions.MoveUp, false);
                    ReleasePrimaryAtCurrentCursor(monitor.Player);
                    released = true;
                }
                stable = monitor.Player.IsOnFloor() &&
                    HasLandingMargin(landing, monitor.Player.GlobalPosition)
                    ? stable + 1 : 0;
                if (stable >= StableLandingFrames) break;
            }
            TestAssert.True(stable >= StableLandingFrames && casts == 1 && latches == 1,
                $"continuous {segmentId} uses one real cast/latch and lands for twelve frames " +
                "with at least 15% horizontal/vertical margin; " +
                $"position={monitor.Player.GlobalPosition} casts={casts} latches={latches}");
            GD.Print($"TASK8_GENERATED_MAIN_GRAPPLE_OK segment={segmentId} casts={casts} " +
                $"latches={latches} landing={monitor.Player.GlobalPosition}");
        }
        finally
        {
            claw.ClawFrameChanged -= Observe;
            PulseJump(false);
            SendAction(InputActions.MoveRight, false);
            SendAction(InputActions.MoveUp, false);
            ReleasePrimaryAtCurrentCursor(monitor.Player);
        }

        void Observe(MechanicalClawFrame frame, GrappleSurface2D observed)
        {
            if (frame.State == MechanicalClawState.Windup && frame.CastId != 0) casts++;
            if (frame.State == MechanicalClawState.Latched && frame.CastId != 0) latches++;
        }
    }

    private static async Task<int> WaitForVisibleTargetDuringJump(SceneTree tree,
        RouteMonitor monitor, GrappleSurface2D target, string segmentId, int jumpsBefore)
    {
        CharacterBody2D player = monitor.Player;
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        // This is only the already-requested ordinary jump: no second Space edge, dash,
        // grapple boost, player-state write, or camera adjustment may reveal the target.
        int apexFrameBudget = Mathf.CeilToInt(motor.Tuning.JumpSpeed / motor.Tuning.Gravity *
            Engine.PhysicsTicksPerSecond) + 2;
        int waited = 0;
        while (!IsTargetVisible(player, target.WorldAnchor) && waited < apexFrameBudget)
        {
            bool jumpStarted = motor.AcceptedJumpCount > jumpsBefore;
            if (jumpStarted && (player.IsOnFloor() || player.Velocity.Y >= 0f)) break;
            await PhysicsFrames(tree, monitor, 1);
            waited++;
        }
        TestAssert.True(IsTargetVisible(player, target.WorldAnchor),
            $"{segmentId}: target is not human-aimable before the ordinary jump apex; " +
            $"player={player.GlobalPosition} velocity={player.Velocity} target={target.WorldAnchor} " +
            $"viewport_target={ToViewport(player, target.WorldAnchor)} " +
            $"visible_rect={player.GetViewport().GetVisibleRect()} waited_frames={waited} " +
            $"apex_budget={apexFrameBudget} accepted_jumps={motor.AcceptedJumpCount - jumpsBefore}; " +
            "no off-screen mouse event was sent");
        return waited;
    }

    private static bool IsTargetVisible(CharacterBody2D player, Vector2 worldPosition) =>
        player.GetViewport().GetVisibleRect().Grow(-32f)
            .HasPoint(ToViewport(player, worldPosition));

    private static void ReleasePrimaryAtCurrentCursor(CharacterBody2D player)
    {
        Vector2 viewport = player.GetNode<PlayerInputReader>("Input")
            .LastObservedMouseViewportPosition;
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            Position = viewport,
            GlobalPosition = viewport,
            ButtonMask = (MouseButtonMask)0
        });
        Input.FlushBufferedEvents();
    }

    private static bool HasLandingMargin(Rect2 zone, Vector2 point)
    {
        Vector2 inset = zone.Size * LandingMarginRatio;
        return new Rect2(zone.Position + inset, zone.Size - inset * 2f).HasPoint(point);
    }

    private static async Task CastAt(SceneTree tree, RouteMonitor monitor,
        CharacterBody2D player, GrappleSurface2D target, Func<bool> completed, int maxFrames)
    {
        MoveMouse(player, target.WorldAnchor);
        SendAction(InputActions.SelectGrappleMode, true);
        await PhysicsFrames(tree, monitor, 2);
        SendAction(InputActions.SelectGrappleMode, false);
        SendMouse(MouseButton.Left, true, player, target.WorldAnchor);
        for (int frame = 0; frame < maxFrames && !completed(); frame++)
        {
            MoveMouse(player, target.WorldAnchor);
            await PhysicsFrames(tree, monitor, 1);
        }
        SendMouse(MouseButton.Left, false, player, target.WorldAnchor);
        await PhysicsFrames(tree, monitor, 3);
    }

    private static async Task DriveHorizontalUntil(SceneTree tree, RouteMonitor monitor,
        bool moveRight, Func<bool> stop, int maxFrames, int jumpPeriod,
        Func<Vector2> mouseAim = null)
    {
        StringName direction = moveRight ? InputActions.MoveRight : InputActions.MoveLeft;
        SendAction(direction, true);
        try
        {
            for (int frame = 0; frame < maxFrames && !stop(); frame++)
            {
                if (mouseAim != null) MoveMouse(monitor.Player, mouseAim());
                if (jumpPeriod > 0 && frame % jumpPeriod == 0 && monitor.Player.IsOnFloor())
                    PulseJump(true);
                if (jumpPeriod > 0 && frame % jumpPeriod == 8) PulseJump(false);
                await PhysicsFrames(tree, monitor, 1);
            }
        }
        finally
        {
            PulseJump(false);
            SendAction(direction, false);
        }
        TestAssert.True(stop(),
            $"continuous input route reaches its next physical condition; " +
            $"position={monitor.Player.GlobalPosition} current={monitor.World.CurrentSector}");
    }

    private static async Task WaitForFloor(SceneTree tree, RouteMonitor monitor, int maxFrames)
    {
        for (int frame = 0; frame < maxFrames && !monitor.Player.IsOnFloor(); frame++)
            await PhysicsFrames(tree, monitor, 1);
        TestAssert.True(monitor.Player.IsOnFloor(),
            $"persistent player reaches production floor support; position={monitor.Player.GlobalPosition}");
    }

    private static float PredictLandingX(CharacterBody2D player, float landingY)
    {
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        Vector2 position = player.GlobalPosition;
        Vector2 velocity = player.Velocity;
        const float seconds = 1f / 60f;
        for (int frame = 0; frame < 300; frame++)
        {
            velocity.X = Mathf.MoveToward(velocity.X, motor.Tuning.MaxRunSpeed,
                motor.Tuning.AirAcceleration * seconds);
            velocity.Y = Mathf.Min(velocity.Y + motor.Tuning.Gravity * seconds,
                motor.Tuning.MaxFallSpeed);
            position += velocity * seconds;
            if (velocity.Y >= 0f && position.Y >= landingY) return position.X;
        }
        return float.NaN;
    }

    private static async Task PhysicsFrames(SceneTree tree, RouteMonitor monitor, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            monitor.Observe();
        }
    }

    private static void PulseJump(bool pressed) => SendAction(InputActions.Jump, pressed);

    private static void SendAction(StringName action, bool pressed)
    {
        var input = new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }

    private static void MoveMouse(CharacterBody2D player, Vector2 worldPosition)
    {
        // Route one real MouseMotion event through both the spawned target viewport and
        // InputMap. PlayerInputReader observes that normal event in production, so this does
        // not depend on an OS cursor warp or synthesize a PlayerInputFrame.
        Vector2 internalViewport = player.GetViewport().GetFinalTransform() *
            (player.GetGlobalTransformWithCanvas() * player.ToLocal(worldPosition));
        var motion = new InputEventMouseMotion
        {
            Position = internalViewport,
            GlobalPosition = internalViewport
        };
        player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
    }

    private static void SendMouse(MouseButton button, bool pressed, CharacterBody2D player,
        Vector2 worldPosition)
    {
        if (GodotObject.IsInstanceValid(player)) MoveMouse(player, worldPosition);
        Vector2 viewport = GodotObject.IsInstanceValid(player)
            ? player.GetViewport().GetFinalTransform() *
              (player.GetGlobalTransformWithCanvas() * player.ToLocal(worldPosition))
            : Vector2.Zero;
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Position = viewport,
            GlobalPosition = viewport,
            ButtonMask = pressed ? button == MouseButton.Left
                ? MouseButtonMask.Left
                : MouseButtonMask.Right
                : (MouseButtonMask)0
        });
        Input.FlushBufferedEvents();
    }

    private static Vector2 ToViewport(CharacterBody2D player, Vector2 worldPosition) =>
        player.GetViewport().GetFinalTransform() *
        (player.GetViewport().GetCanvasTransform() * worldPosition);

    private static void ReleaseAll(Vector2 worldPosition)
    {
        foreach (StringName action in new[]
                 { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
                     InputActions.MoveDown, InputActions.Jump, InputActions.Dash,
                     InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
                     InputActions.PrimaryAction, InputActions.Shield })
            Input.ActionRelease(action);
        Input.ParseInputEvent(new InputEventMouseButton
            { ButtonIndex = MouseButton.Left, Pressed = false, Position = worldPosition });
        Input.ParseInputEvent(new InputEventMouseButton
            { ButtonIndex = MouseButton.Right, Pressed = false, Position = worldPosition });
    }

    private static async Task Capture(SceneTree tree, bool enabled, string name)
    {
        if (!enabled) return;
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image image = tree.Root.GetTexture().GetImage();
        TestAssert.Equal(1280, image.GetWidth(), $"{name} proof frame width is 1280");
        TestAssert.Equal(720, image.GetHeight(), $"{name} proof frame height is 720");
        string directory = ProjectSettings.GlobalizePath("res://artifacts/task8-fix1-proof");
        DirAccess.MakeDirRecursiveAbsolute(directory);
        Error error = image.SavePng(Path.Combine(directory, name + ".png"));
        TestAssert.Equal(Error.Ok, error, $"{name} proof frame saves successfully");
    }

    private sealed class RouteMonitor : IDisposable
    {
        private readonly PrologueLevelController _level;
        private readonly ulong _playerId;
        private Vector2 _previous;
        private int _highestRouteIndex;
        private bool _recoveryRepositionPending;
        private bool _recoveryRequestPending;

        public RouteMonitor(PrologueLevelController level, CharacterBody2D player, Camera2D camera,
            SeamlessPrologueWorld world, ulong playerId)
        {
            _level = level;
            Player = player;
            Camera = camera;
            World = world;
            _playerId = playerId;
            _previous = player.GlobalPosition;
            Visited.Add(world.CurrentSector);
            _level.RecoveryRequested += OnRecoveryRequested;
            _level.RecoveryCompleted += OnRecoveryCompleted;
        }

        public CharacterBody2D Player { get; }
        public Camera2D Camera { get; }
        public SeamlessPrologueWorld World { get; }
        public List<PrologueSectorId> Visited { get; } = new();
        public int Frames { get; private set; }
        public float MaxStep { get; private set; }
        public int MaxMounted { get; private set; }
        public int RecoveryRequested { get; private set; }
        public int RecoveryCompleted { get; private set; }

        public void Dispose()
        {
            if (!GodotObject.IsInstanceValid(_level)) return;
            _level.RecoveryRequested -= OnRecoveryRequested;
            _level.RecoveryCompleted -= OnRecoveryCompleted;
        }

        public void AssertNoProductionRecovery(string context)
        {
            TestAssert.Equal(0, RecoveryRequested,
                $"{context} emits zero measured production RecoveryRequested events");
            TestAssert.Equal(0, RecoveryCompleted,
                $"{context} emits zero measured production RecoveryCompleted events");
        }

        public void AssertTask9Recoveries(string context, int expected)
        {
            TestAssert.Equal(expected, RecoveryRequested,
                $"{context} emits the exact measured RecoveryRequested count");
            TestAssert.Equal(expected, RecoveryCompleted,
                $"{context} pairs every RecoveryRequested with RecoveryCompleted");
            TestAssert.True(!_recoveryRequestPending && !_recoveryRepositionPending,
                $"{context} closes every recovery and its single displacement exemption");
        }

        public void Observe()
        {
            Frames++;
            TestAssert.Equal(_playerId, Player.GetInstanceId(),
                "one persistent production player survives every seam");
            TestAssert.True(Player.GetParent()?.Name == "PlayerRoot",
                "persistent player is never reparented into a streamed sector");
            float step = Player.GlobalPosition.DistanceTo(_previous);
            MaxStep = Mathf.Max(MaxStep, step);
            if (_recoveryRepositionPending)
            {
                PrologueCheckpointSnapshot checkpoint = _level.Progress.RestoreCheckpoint();
                TestAssert.True(Player.GlobalPosition.DistanceTo(checkpoint.WorldPosition) < 1.1f,
                    $"the only displacement exemption is a measured recovery to the explicit " +
                    $"checkpoint; expected={checkpoint.WorldPosition} actual={Player.GlobalPosition}");
                _recoveryRepositionPending = false;
            }
            else TestAssert.True(step <= 72f,
                $"no frame performs a position reset or teleport; step={step:0.00} " +
                $"from={_previous} to={Player.GlobalPosition}");
            _previous = Player.GlobalPosition;

            int routeIndex = Array.IndexOf(OrderedRoute, World.CurrentSector);
            TestAssert.True(routeIndex >= _highestRouteIndex && routeIndex <= _highestRouteIndex + 1,
                $"streaming current route advances in order without skipping/backtracking; " +
                $"previous={OrderedRoute[_highestRouteIndex]} current={World.CurrentSector}");
            if (routeIndex > _highestRouteIndex)
            {
                _highestRouteIndex = routeIndex;
                Visited.Add(World.CurrentSector);
            }
            TestAssert.True(World.MountedSectorIds.Contains(World.CurrentSector),
                $"current sector {World.CurrentSector} is mounted on every observed frame");
            MaxMounted = Math.Max(MaxMounted, World.MountedSectorIds.Count);
            if (_highestRouteIndex > 0)
                TestAssert.True(Player.GlobalPosition.DistanceTo(new Vector2(120f, 574f)) > 160f,
                    "route never respawns early at the round-start coordinate");
        }

        public void AssertCurrent(PrologueSectorId expected) =>
            TestAssert.Equal(expected, World.CurrentSector,
                $"streaming resolves expected continuous sector {expected}");

        public void AssertCameraCoverage()
        {
            Vector2 position = Player.GlobalPosition;
            const float tolerance = 96f;
            TestAssert.True(Camera.Enabled && position.X >= Camera.LimitLeft - tolerance &&
                position.X <= Camera.LimitRight + tolerance &&
                position.Y >= Camera.LimitTop - tolerance &&
                position.Y <= Camera.LimitBottom + tolerance,
                $"camera limits cover the persistent player after seam; player={position} " +
                $"limits=({Camera.LimitLeft},{Camera.LimitTop})-({Camera.LimitRight},{Camera.LimitBottom})");
        }

        public void AssertComplete()
        {
            TestAssert.Equal(string.Join("|", OrderedRoute), string.Join("|", Visited),
                "generated-main proof crosses all four seams in exact route order");
            TestAssert.True(MaxMounted is >= 2 and <= 3,
                $"streaming keeps the current route and bounded neighbours mounted; max={MaxMounted}");
            AssertCameraCoverage();
        }

        private void OnRecoveryRequested(string _)
        {
            TestAssert.True(!_recoveryRequestPending,
                "a Task 9 recovery request cannot overlap an unfinished recovery");
            _recoveryRequestPending = true;
            RecoveryRequested++;
        }

        private void OnRecoveryCompleted(string _)
        {
            TestAssert.True(_recoveryRequestPending,
                "RecoveryCompleted is paired with a preceding measured RecoveryRequested");
            _recoveryRequestPending = false;
            RecoveryCompleted++;
            _recoveryRepositionPending = true;
        }
    }
}
