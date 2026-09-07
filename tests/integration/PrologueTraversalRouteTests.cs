using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PrologueTraversalRouteTests
{
    private const int SingleJumpBudget = 90;
    private const int AssistedBudget = 150;
    private const int JumpReleaseFrame = 18;
    private const string SuppressClawEnvironment = "KNIGHT_TRAVERSAL_SUPPRESS_CLAW_SEGMENT";
    private const string ZeroLandingEnvironment = "KNIGHT_TRAVERSAL_ZERO_WIDTH_SEGMENT";

    private static readonly RoomCase[] Rooms =
    {
        new("AwakeningCourtyard", "res://scenes/prologue/rooms/AwakeningCourtyard.tscn"),
        new("BrokenToothCorridor", "res://scenes/prologue/rooms/BrokenToothCorridor.tscn"),
        new("UpperPendulum", "res://scenes/prologue/rooms/UpperPendulum.tscn"),
        new("LowerBuriedMachinery", "res://scenes/prologue/rooms/LowerBuriedMachinery.tscn"),
        new("BellChainMechanism", "res://scenes/prologue/rooms/BellChainMechanism.tscn"),
        new("FallenBellkeeperArena", "res://scenes/prologue/rooms/FallenBellkeeperArena.tscn")
    };

    public static async Task Run(SceneTree tree)
    {
        AssertContinuousSmokeSourceGuard();
        ReleaseAll();
        foreach (RoomCase roomCase in Rooms)
        {
            PrologueRoom manifestRoom = LoadRoom(roomCase.ScenePath);
            IReadOnlyList<TraversalSegment2D> contracts = Contracts(manifestRoom);
            foreach (TraversalSegment2D contract in contracts)
            {
                string samples;
                if (contract.Kind == TraversalSegmentKind.SingleJump)
                {
                    foreach (float fraction in new[] { .2f, .5f, .8f })
                        await RunFreshSample(tree, roomCase, contract.SegmentId.ToString(), fraction);
                    samples = "20,50,80";
                }
                else if (contract.Kind == TraversalSegmentKind.Assisted)
                {
                    await RunFreshSample(tree, roomCase, contract.SegmentId.ToString(), .5f);
                    samples = "50";
                }
                else continue;

                GD.Print($"PROLOGUE_TRAVERSAL_SEGMENT_OK room={roomCase.Name} " +
                    $"segment={contract.SegmentId} abilities={contract.RequiredAbilities} sample={samples}");
            }
            manifestRoom.Free();
        }
        ReleaseAll();
    }

    private static void AssertContinuousSmokeSourceGuard()
    {
        string smokeSource = FileAccess.GetFileAsString(
            "res://tests/smoke/PrologueCriticalPathSmoke.cs");
        const string upperEntryMarker = "PROLOGUE_THREAD_SMOKE_ROOM_CLEANUP_OK";
        const string nextMethod = "private async Task LaborerAndShortcut";
        int start = smokeSource.IndexOf(upperEntryMarker, StringComparison.Ordinal);
        int end = smokeSource.IndexOf(nextMethod, start, StringComparison.Ordinal);
        TestAssert.True(start >= 0 && end > start,
            "smoke source exposes a bounded post-entry Upper route block");
        string route = smokeSource.Substring(start, end - start);
        TestAssert.True(route.Contains("TraverseUpperContractsContinuously", StringComparison.Ordinal) &&
            route.Contains("ReachUpperMechanismExitContinuously", StringComparison.Ordinal),
            "Upper smoke uses the stateful real-input route and exit helpers");
        TestAssert.True(!route.Contains("Fixture(", StringComparison.Ordinal) &&
            !route.Contains("TouchExit(\"Exits/ToMechanism\"", StringComparison.Ordinal) &&
            !route.Contains("_player.GlobalPosition =", StringComparison.Ordinal) &&
            !route.Contains("_player.Position =", StringComparison.Ordinal),
            "post-entry Upper smoke call block has no reposition or direct exit touch");

        string helperSource = FileAccess.GetFileAsString(
            "res://tests/integration/PrologueTraversalRouteTests.cs");
        string traversalHelper = ExtractCSharpMethodBody(helperSource,
            "public static async Task TraverseUpperContractsContinuously(");
        string exitHelper = ExtractCSharpMethodBody(helperSource,
            "public static async Task ReachUpperMechanismExitContinuously(");
        AssertNoTraversalDiscontinuity("TraverseUpperContractsContinuously", traversalHelper);
        AssertNoTraversalDiscontinuity("ReachUpperMechanismExitContinuously", exitHelper);
    }

    private static void AssertNoTraversalDiscontinuity(string helperName, string body)
    {
        bool stateWrite = System.Text.RegularExpressions.Regex.IsMatch(body,
            @"\bplayer\.(?:GlobalPosition|Position|Velocity)\s*(?:=(?!=)|[+\-*/%&|^]=)");
        bool fixture = body.Contains("Fixture(", StringComparison.Ordinal);
        bool directExit = body.Contains("TouchExit(", StringComparison.Ordinal) ||
            body.Contains(".TouchExit(", StringComparison.Ordinal);
        TestAssert.True(!stateWrite && !fixture && !directExit,
            $"continuous helper guard rejects position/velocity writes or direct-touch in {helperName}; " +
            $"stateWrite={stateWrite}, fixture={fixture}, directExit={directExit}");
    }

    private static string ExtractCSharpMethodBody(string source, string signature)
    {
        System.Text.RegularExpressions.Match declaration =
            System.Text.RegularExpressions.Regex.Match(source,
                @"(?m)^[ \t]*" + System.Text.RegularExpressions.Regex.Escape(signature));
        int signatureIndex = declaration.Success ? declaration.Index : -1;
        TestAssert.True(signatureIndex >= 0,
            $"source exposes a line-anchored helper declaration {signature}");
        int open = source.IndexOf('{', signatureIndex);
        TestAssert.True(open >= 0, $"helper {signature} has an opening brace");

        int depth = 0;
        bool lineComment = false;
        bool blockComment = false;
        bool quoted = false;
        bool verbatim = false;
        bool character = false;
        for (int index = open; index < source.Length; index++)
        {
            char current = source[index];
            char next = index + 1 < source.Length ? source[index + 1] : '\0';
            if (lineComment)
            {
                if (current == '\n') lineComment = false;
                continue;
            }
            if (blockComment)
            {
                if (current == '*' && next == '/')
                {
                    blockComment = false;
                    index++;
                }
                continue;
            }
            if (quoted)
            {
                if (verbatim && current == '"' && next == '"')
                {
                    index++;
                    continue;
                }
                if (current == '"' && (verbatim || index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                    verbatim = false;
                }
                continue;
            }
            if (character)
            {
                if (current == '\'' && (index == 0 || source[index - 1] != '\\'))
                    character = false;
                continue;
            }
            if (current == '/' && next == '/')
            {
                lineComment = true;
                index++;
                continue;
            }
            if (current == '/' && next == '*')
            {
                blockComment = true;
                index++;
                continue;
            }
            if (current == '"')
            {
                quoted = true;
                verbatim = index > 0 && source[index - 1] == '@';
                continue;
            }
            if (current == '\'')
            {
                character = true;
                continue;
            }
            if (current == '{') depth++;
            else if (current == '}' && --depth == 0)
                return source.Substring(open + 1, index - open - 1);
        }

        throw new InvalidOperationException($"helper {signature} has no balanced closing brace");
    }

    public static async Task TraverseUpperContractsContinuously(SceneTree tree,
        PrologueRoom room, CharacterBody2D player, Func<Task> physicsFrame = null)
    {
        TestAssert.Equal(PrologueRoomId.UpperPendulum, room.RoomId,
            "continuous traversal helper is mounted in Upper Pendulum");
        string[] ordered =
        {
            "entry_to_lower_swing",
            "lower_to_mid_swing",
            "mid_to_high_swing",
            "high_to_mechanism_landing"
        };
        ReleaseAll();
        foreach (string segmentId in ordered)
        {
            TraversalSegment2D segment = room.GetNode<TraversalSegment2D>(
                $"TraversalContracts/{segmentId}");
            AssertContractValid("UpperPendulum", segment, room);
            await WalkIntoLaunchZone(tree, player, segment, physicsFrame);
            var trace = new RouteTrace("UpperPendulum", segmentId, .5f, player.Position,
                player.GetNode<PlayerMotor>("Motor"),
                player.GetNode<MechanicalClawController>("MechanicalClaw"));
            if (segment.Kind == TraversalSegmentKind.Assisted)
                trace.ExpectedTargetId = room.GetNode<GrappleSurface2D>(segment.AssistTargetPath)
                    .GetInstanceId();
            Area2D killZone = room.GetNode<Area2D>("KillZone");
            killZone.BodyEntered += trace.OnKillZoneEntered;
            foreach (Node child in room.GetNode("Exits").GetChildren())
                if (child is RoomExit exit) exit.ExitRequested += trace.OnExitRequested;
            trace.Claw.ClawFrameChanged += trace.OnClawFrameChanged;
            try
            {
                trace.BeginRoute();
                if (segment.Kind == TraversalSegmentKind.Assisted)
                    await DriveAssisted(tree, player, room, segment, trace, physicsFrame);
                else
                    await DriveSingleJump(tree, player, segment, trace, physicsFrame);
                AssertSuccessfulLanding(player, segment, trace);
                GD.Print($"PROLOGUE_TRAVERSAL_SMOKE_SEGMENT_OK room=UpperPendulum " +
                    $"segment={segment.SegmentId} abilities={segment.RequiredAbilities}");
            }
            catch (Exception exception)
            {
                int budget = segment.Kind == TraversalSegmentKind.Assisted
                    ? AssistedBudget : SingleJumpBudget;
                throw new InvalidOperationException(trace.Diagnostics(player, budget), exception);
            }
            finally
            {
                ReleaseAll();
                trace.Claw.ClawFrameChanged -= trace.OnClawFrameChanged;
                killZone.BodyEntered -= trace.OnKillZoneEntered;
                foreach (Node child in room.GetNode("Exits").GetChildren())
                    if (child is RoomExit exit) exit.ExitRequested -= trace.OnExitRequested;
            }
        }
    }

    public static async Task ReachUpperMechanismExitContinuously(SceneTree tree,
        PrologueRoom room, CharacterBody2D player, Func<Task> physicsFrame = null)
    {
        RoomExit exit = room.GetNode<RoomExit>("Exits/ToMechanism");
        bool requested = false;
        void OnRequested(StringName exitId) => requested = exitId == "to_mechanism";
        exit.ExitRequested += OnRequested;
        Input.ActionPress(InputActions.MoveRight);
        Input.ActionPress(InputActions.Jump);
        int jumpHeld = 0;
        try
        {
            for (int frame = 0; frame < 240 && !requested; frame++)
            {
                await NextPhysicsFrame(tree, physicsFrame);
                if (Input.IsActionPressed(InputActions.Jump) && ++jumpHeld >= 18)
                    Input.ActionRelease(InputActions.Jump);
                if (!Input.IsActionPressed(InputActions.Jump) && frame > 45 &&
                    player.IsOnFloor() && player.Position.X < exit.Position.X - 24f)
                {
                    Input.ActionPress(InputActions.Jump);
                    jumpHeld = 0;
                }
            }
            TestAssert.True(requested,
                $"continuous real movement reaches the authored Upper mechanism exit; " +
                $"player={player.Position}, exit={exit.Position}, onFloor={player.IsOnFloor()}");
        }
        finally
        {
            Input.ActionRelease(InputActions.MoveRight);
            Input.ActionRelease(InputActions.Jump);
            if (GodotObject.IsInstanceValid(exit)) exit.ExitRequested -= OnRequested;
        }
    }

    private static async Task RunFreshSample(SceneTree tree, RoomCase roomCase,
        string segmentId, float fraction)
    {
        ReleaseAll();
        await PhysicsFrames(tree, 1);
        var root = new Node2D { Name = $"Traversal_{roomCase.Name}_{segmentId}_{fraction:0.0}" };
        PrologueRoom room = LoadRoom(roomCase.ScenePath);
        TraversalSegment2D segment = room.GetNode<TraversalSegment2D>($"TraversalContracts/{segmentId}");
        if (OS.GetEnvironment(ZeroLandingEnvironment) == segmentId)
            segment.LandingZone = new Rect2(segment.LandingZone.Position,
                new Vector2(0f, segment.LandingZone.Size.Y));
        AssertContractValid(roomCase.Name, segment, room);

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "TraversalPlayer";
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        Vector2 start = new(
            Mathf.Lerp(segment.LaunchZone.Position.X, segment.LaunchZone.End.X, fraction),
            segment.LaunchZone.GetCenter().Y);
        player.Position = start;
        root.AddChild(room);
        root.AddChild(player);
        tree.Root.AddChild(root);

        var trace = new RouteTrace(roomCase.Name, segmentId, fraction, start,
            player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"));
        if (segment.Kind == TraversalSegmentKind.Assisted)
            trace.ExpectedTargetId = room.GetNode<GrappleSurface2D>(segment.AssistTargetPath).GetInstanceId();
        Area2D killZone = room.GetNode<Area2D>("KillZone");
        killZone.BodyEntered += trace.OnKillZoneEntered;
        foreach (Node child in room.GetNode("Exits").GetChildren())
            if (child is RoomExit exit) exit.ExitRequested += trace.OnExitRequested;
        trace.Claw.ClawFrameChanged += trace.OnClawFrameChanged;

        try
        {
            await WaitForStableFloor(tree, player, trace, 45);
            trace.BeginRoute();
            if (segment.Kind == TraversalSegmentKind.SingleJump)
                await DriveSingleJump(tree, player, segment, trace);
            else if (segment.Kind == TraversalSegmentKind.Assisted)
                await DriveAssisted(tree, player, room, segment, trace);
            else
                throw new InvalidOperationException($"route probe does not execute {segment.Kind}");
            AssertSuccessfulLanding(player, segment, trace);
            GD.Print(trace.SuccessDiagnostics(player));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(trace.Diagnostics(player,
                segment.Kind == TraversalSegmentKind.SingleJump ? SingleJumpBudget : AssistedBudget), exception);
        }
        finally
        {
            ReleaseAll();
            tree.Paused = false;
            trace.Claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            killZone.BodyEntered -= trace.OnKillZoneEntered;
            foreach (Node child in room.GetNode("Exits").GetChildren())
                if (child is RoomExit exit) exit.ExitRequested -= trace.OnExitRequested;
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 1);
        }
    }

    private static async Task DriveSingleJump(SceneTree tree, CharacterBody2D player,
        TraversalSegment2D segment, RouteTrace trace, Func<Task> physicsFrame = null)
    {
        int direction = Mathf.Sign(segment.LandingZone.GetCenter().X - segment.LaunchZone.GetCenter().X);
        StringName horizontal = direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        Input.ActionPress(horizontal);
        Input.ActionPress(InputActions.Jump);
        for (int frame = 0; frame < SingleJumpBudget; frame++)
        {
            trace.Frame = frame;
            await NextPhysicsFrame(tree, physicsFrame);
            trace.Observe(player, segment);
            if (frame == JumpReleaseFrame) Input.ActionRelease(InputActions.Jump);
            if (PassedHorizontalCenter(player.Position.X, segment.LandingZone.GetCenter().X, direction))
                Input.ActionRelease(horizontal);
            if (trace.StableLandingFrames >= 2 && segment.LandingZone.HasPoint(player.Position)) break;
        }
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(horizontal);
    }

    private static async Task DriveAssisted(SceneTree tree, CharacterBody2D player, PrologueRoom room,
        TraversalSegment2D segment, RouteTrace trace, Func<Task> physicsFrame = null)
    {
        GrappleSurface2D target = room.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
        Vector2 aim = player.GlobalPosition.DirectionTo(target.GlobalPosition);
        int landingDirection = Mathf.Sign(segment.LandingZone.GetCenter().X - segment.LaunchZone.GetCenter().X);
        StringName horizontal = landingDirection < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        StringName vertical = aim.Y < 0f ? InputActions.MoveUp : InputActions.MoveDown;
        bool suppressClaw = OS.GetEnvironment(SuppressClawEnvironment) == segment.SegmentId.ToString();
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        Input.ActionPress(horizontal);
        Input.ActionPress(vertical);
        Input.ActionPress(InputActions.Jump);
        if (!suppressClaw)
        {
            Input.ActionPress(InputActions.SelectGrappleMode);
            Input.ActionPress(InputActions.PrimaryAction);
        }

        try
        {
            for (int frame = 0; frame < AssistedBudget; frame++)
            {
                trace.Frame = frame;
                SampleClawInput(input, modes, claw, target.WorldAnchor);
                await NextPhysicsFrame(tree, physicsFrame);
                trace.Observe(player, segment);
                if (frame == 0) Input.ActionRelease(InputActions.SelectGrappleMode);
                if (trace.HasAttached)
                {
                    // Keep swinging toward the authored landing and release as soon as the
                    // current, already-applied tangential velocity predicts a valid ballistic
                    // landing. This probes the new claw contract instead of depending on the old
                    // cumulative-correction bug's room-specific fixed reel duration.
                    float predictedLandingX = PredictDetachedLandingX(player, trace.Motor,
                        segment.LandingZone.GetCenter().Y, landingDirection);
                    if (predictedLandingX >= segment.LandingZone.Position.X &&
                        predictedLandingX <= segment.LandingZone.End.X)
                    {
                        Input.ActionRelease(InputActions.PrimaryAction);
                        Input.ActionRelease(vertical);
                    }
                }
                if (PassedHorizontalCenter(player.Position.X, segment.LandingZone.GetCenter().X, landingDirection))
                    Input.ActionRelease(horizontal);
                if (trace.StableLandingFrames >= 2 && segment.LandingZone.HasPoint(player.Position)) break;
            }
        }
        finally
        {
            Input.ActionRelease(InputActions.Jump);
            Input.ActionRelease(InputActions.PrimaryAction);
            Input.ActionRelease(InputActions.SelectGrappleMode);
            Input.ActionRelease(vertical);
            Input.ActionRelease(horizontal);
            input.SetPhysicsProcess(true);
            modes.SetPhysicsProcess(true);
            claw.SetPhysicsProcess(true);
        }
    }

    private static void SampleClawInput(PlayerInputReader input, PlayerActionModeController modes,
        MechanicalClawController claw, Vector2 mouseWorld)
    {
        input._PhysicsProcess(0d);
        PlayerInputFrame frame = input.CurrentFrame with { MouseWorld = mouseWorld };
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);
        modes._PhysicsProcess(0d);
        claw._PhysicsProcess(1d / 60d);
    }

    private static float PredictDetachedLandingX(CharacterBody2D player, PlayerMotor motor,
        float landingY, float moveAxis)
    {
        const float seconds = 1f / 60f;
        // Player.Velocity already contains the currently applied claw component. Release consumes
        // the Task 5 tangential correction into that velocity once; predicting another addition
        // here would encode the cumulative-runtime bug instead of the real detach trajectory.
        Vector2 velocity = player.Velocity;

        Vector2 position = player.Position;
        for (int frame = 0; frame < AssistedBudget; frame++)
        {
            velocity.X = Mathf.MoveToward(velocity.X,
                Mathf.Clamp(moveAxis, -1f, 1f) * motor.Tuning.MaxRunSpeed,
                motor.Tuning.AirAcceleration * seconds);
            velocity.Y = Mathf.Min(velocity.Y + motor.Tuning.Gravity * seconds,
                motor.Tuning.MaxFallSpeed);
            position += velocity * seconds;
            if (velocity.Y >= 0f && position.Y >= landingY) return position.X;
        }
        return float.NaN;
    }

    private static void AssertSuccessfulLanding(CharacterBody2D player, TraversalSegment2D segment,
        RouteTrace trace)
    {
        TestAssert.True(trace.LeftFloor, "real Space input leaves the supported launch surface");
        TestAssert.True(trace.StableLandingFrames >= 2 && player.IsOnFloor() &&
            segment.LandingZone.HasPoint(player.Position), "player lands stably inside the target zone");
        TestAssert.Equal(0, trace.Motor.AcceptedDashCount, "mandatory route starts zero dashes");
        TestAssert.True(!trace.Killed && trace.ExitRequests == 0,
            "route success cannot pass through recovery or room transition");
        if (segment.Kind == TraversalSegmentKind.SingleJump)
        {
            TestAssert.Equal(0, trace.CastCount, "SingleJump route starts no mechanical-claw cast");
            TestAssert.Equal(0, trace.ApplyCount, "SingleJump route latches no grapple surface");
        }
        else
        {
            TestAssert.Equal(1, trace.CastCount, "Assisted route starts exactly one mechanical-claw cast");
            TestAssert.Equal(1, trace.ApplyCount, "Assisted route latches exactly one grapple surface");
            TestAssert.Equal(trace.ExpectedTargetId, trace.AppliedTargetId,
                "Assisted route applies to the contract's exact authored anchor");
            TestAssert.Equal(MechanicalClawFailure.None, trace.LastFailure,
                "Assisted route has no mechanical-claw failure");
            float maxPreviewDistance = MechanicalClawSettings.Default.MaxRange;
            TestAssert.True(!float.IsNaN(trace.FirstPreviewDistance) &&
                trace.FirstPreviewDistance <= maxPreviewDistance,
                $"real post-jump Aim preview stays within the approved {maxPreviewDistance:0}px envelope");
        }
    }

    private static void AssertContractValid(string roomName, TraversalSegment2D segment, PrologueRoom room)
    {
        IReadOnlyList<string> errors = segment.Validate(room);
        if (errors.Count > 0)
            throw new InvalidOperationException($"room={roomName} segment={segment.SegmentId} " +
                $"validation failed before physics probing: {string.Join("; ", errors)}");
    }

    private static async Task WaitForStableFloor(SceneTree tree, CharacterBody2D player,
        RouteTrace trace, int maxFrames)
    {
        int stable = 0;
        for (int frame = 0; frame < maxFrames && stable < 2; frame++)
        {
            await PhysicsFrames(tree, 1);
            trace.Frame = -maxFrames + frame;
            trace.Observe(player, null);
            stable = player.IsOnFloor() ? stable + 1 : 0;
        }
        TestAssert.True(stable >= 2, "launch sample settles on the packed real collision surface");
    }

    private static async Task WalkIntoLaunchZone(SceneTree tree, CharacterBody2D player,
        TraversalSegment2D segment, Func<Task> physicsFrame)
    {
        int stable = 0;
        for (int frame = 0; frame < 240 && stable < 2; frame++)
        {
            float delta = segment.LaunchZone.GetCenter().X - player.Position.X;
            if (Mathf.Abs(delta) > 8f)
                Input.ActionPress(delta < 0f ? InputActions.MoveLeft : InputActions.MoveRight);
            else
            {
                Input.ActionRelease(InputActions.MoveLeft);
                Input.ActionRelease(InputActions.MoveRight);
            }
            await NextPhysicsFrame(tree, physicsFrame);
            stable = player.IsOnFloor() && segment.LaunchZone.HasPoint(player.Position) &&
                Mathf.Abs(segment.LaunchZone.GetCenter().X - player.Position.X) <= 12f
                ? stable + 1 : 0;
        }
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        TestAssert.True(stable >= 2,
            $"continuous route walks into supported launch zone for {segment.SegmentId}");
    }

    private static bool PassedHorizontalCenter(float x, float center, int direction) =>
        direction > 0 ? x >= center : x <= center;

    private static PrologueRoom LoadRoom(string path) =>
        GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();

    private static IReadOnlyList<TraversalSegment2D> Contracts(PrologueRoom room)
    {
        var result = new List<TraversalSegment2D>();
        foreach (Node child in room.GetNode("TraversalContracts").GetChildren())
            if (child is TraversalSegment2D segment) result.Add(segment);
        return result;
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task NextPhysicsFrame(SceneTree tree, Func<Task> physicsFrame)
    {
        if (physicsFrame != null) await physicsFrame();
        else await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    public static void ReleaseAll()
    {
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(InputActions.Dash);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.Shield);
        Input.ActionRelease(InputActions.Pause);
    }

    private sealed record RoomCase(string Name, string ScenePath);

    private sealed class RouteTrace
    {
        public RouteTrace(string room, string segment, float sample, Vector2 start,
            PlayerMotor motor, MechanicalClawController claw)
        {
            Room = room;
            Segment = segment;
            Sample = sample;
            Start = start;
            Motor = motor;
            Claw = claw;
        }

        public string Room { get; }
        public string Segment { get; }
        public float Sample { get; }
        public Vector2 Start { get; }
        public PlayerMotor Motor { get; }
        public MechanicalClawController Claw { get; }
        public int Frame { get; set; }
        public int CastCount { get; private set; }
        public int ApplyCount { get; private set; }
        public ulong ExpectedTargetId { get; set; }
        public ulong AppliedTargetId { get; private set; }
        public int ExitRequests { get; private set; }
        public MechanicalClawFailure LastFailure { get; private set; }
        public MechanicalClawState LastClawState { get; private set; } = MechanicalClawState.Idle;
        public bool HasAttached { get; private set; }
        public bool Killed { get; private set; }
        public bool LeftFloor { get; private set; }
        public int StableLandingFrames { get; private set; }
        public bool LastOnFloor { get; private set; }
        public int LastSlideCollisions { get; private set; }
        public int LandingZoneCrossings { get; private set; }
        public float ClosestLandingCenterDistance { get; private set; } = float.PositiveInfinity;
        public float FirstPreviewDistance { get; private set; } = float.NaN;
        private readonly HashSet<ulong> _castIds = new();
        private readonly HashSet<ulong> _applyIds = new();
        private readonly List<string> _trajectory = new();
        private readonly List<string> _clawEvents = new();

        public void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            _clawEvents.Add($"f{Frame}:{frame.State}/visible={frame.EffectiveVisibility}/" +
                $"target={frame.LockedTargetRuntimeId}/published={target?.GetInstanceId() ?? 0UL}/" +
                $"failure={frame.Failure}");
            if (frame.State == MechanicalClawState.Windup && frame.CastId != 0 && _castIds.Add(frame.CastId))
                CastCount++;
            if (frame.State == MechanicalClawState.Aim && target != null &&
                float.IsNaN(FirstPreviewDistance) && Claw.GetParent() is Node2D owner)
                FirstPreviewDistance = owner.GlobalPosition.DistanceTo(target.WorldAnchor);
            if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing && frame.CastId != 0 && _applyIds.Add(frame.CastId))
            {
                ApplyCount++;
                AppliedTargetId = target?.GetInstanceId() ?? 0UL;
            }
            HasAttached |= frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing;
            if (frame.State == MechanicalClawState.Failed) LastFailure = frame.Failure;
            LastClawState = frame.State;
        }

        public void BeginRoute()
        {
            Frame = 0;
            LeftFloor = false;
            StableLandingFrames = 0;
            LastOnFloor = true;
            LastSlideCollisions = 0;
            LandingZoneCrossings = 0;
            ClosestLandingCenterDistance = float.PositiveInfinity;
            FirstPreviewDistance = float.NaN;
            _trajectory.Clear();
            _clawEvents.Clear();
        }

        public void OnKillZoneEntered(Node2D body)
        {
            if (body.IsInGroup("player")) Killed = true;
        }

        public void OnExitRequested(StringName exitId) => ExitRequests++;

        public void Observe(CharacterBody2D player, TraversalSegment2D segment)
        {
            LastOnFloor = player.IsOnFloor();
            LastSlideCollisions = player.GetSlideCollisionCount();
            LeftFloor |= !LastOnFloor;
            StableLandingFrames = LeftFloor && LastOnFloor ? StableLandingFrames + 1 : 0;
            if (segment != null)
            {
                if (segment.LandingZone.HasPoint(player.Position)) LandingZoneCrossings++;
                ClosestLandingCenterDistance = Mathf.Min(ClosestLandingCenterDistance,
                    player.Position.DistanceTo(segment.LandingZone.GetCenter()));
            }
            if (Frame >= 0 && Frame % 10 == 0)
                _trajectory.Add($"{Frame}:{player.Position}/{player.Velocity}/floor={LastOnFloor}");
        }

        public string Diagnostics(CharacterBody2D player, int budget) =>
            $"TRAVERSAL_ROUTE_FAILED room={Room} segment={Segment} sample={Sample:0.0} " +
            $"start={Start} final={player.Position} frame={Frame} budget={budget} " +
            $"acceptedDash={Motor.AcceptedDashCount} rejectedDash={Motor.RejectedDashCount} " +
            $"castCount={CastCount} applyCount={ApplyCount} clawState={LastClawState} " +
            $"clawFailure={LastFailure} clawCleanup={Claw.CurrentFrame.State} " +
            $"killed={Killed} exitRequests={ExitRequests} onFloor={LastOnFloor} " +
            $"slideCollisions={LastSlideCollisions} stableLandingFrames={StableLandingFrames} " +
            $"landingZoneCrossings={LandingZoneCrossings} closestLanding={ClosestLandingCenterDistance:0.00} " +
            $"firstPreviewDistance={FirstPreviewDistance:0.00} " +
            $"expectedTarget={ExpectedTargetId} appliedTarget={AppliedTargetId} " +
            $"trajectory=[{string.Join(";", _trajectory)}] clawEvents=[{string.Join(";", _clawEvents)}]";

        public string SuccessDiagnostics(CharacterBody2D player) =>
            $"PROLOGUE_TRAVERSAL_SAMPLE_OK room={Room} segment={Segment} sample={Sample:0.0} " +
            $"start={Start} landed={player.Position} frame={Frame} onFloor={LastOnFloor} " +
            $"slideCollisions={LastSlideCollisions} stableLandingFrames={StableLandingFrames} " +
            $"acceptedDash={Motor.AcceptedDashCount} castCount={CastCount} applyCount={ApplyCount} " +
            $"firstPreviewDistance={FirstPreviewDistance:0.00}";
    }
}
