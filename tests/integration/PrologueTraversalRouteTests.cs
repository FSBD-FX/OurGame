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
using Knight.Tests.Presentation;

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
            try
            {
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
            }
            finally
            {
                // This separate off-tree manifest is ours, not the mounted sample.
                // Awaited route failure must release it without hiding that failure.
                manifestRoom.Free();
            }
        }
        ReleaseAll();
    }

    public static async Task RunBrokenToothOnly(SceneTree tree)
    {
        ReleaseAll();
        try
        {
            await RunFreshSample(tree, new RoomCase("BrokenToothCorridor",
                "res://scenes/prologue/rooms/BrokenToothCorridor.tscn"),
                "entry_to_anchor_landing", .5f);
        }
        finally { ReleaseAll(); }
    }

    public static async Task RunUpperEntryOnly(SceneTree tree)
    {
        ReleaseAll();
        try
        {
            await RunFreshSample(tree, new RoomCase("UpperPendulum",
                "res://scenes/prologue/rooms/UpperPendulum.tscn"),
                "entry_to_lower_swing", .5f);
        }
        finally { ReleaseAll(); }
    }

    public static async Task RunUpperMidOnly(SceneTree tree)
    {
        ReleaseAll();
        try
        {
            await RunFreshSample(tree, new RoomCase("UpperPendulum",
                "res://scenes/prologue/rooms/UpperPendulum.tscn"), "lower_to_mid_swing", .5f);
            GD.Print("THRUSTER_UPPER_MID_OK original_saved_room=true original_sample_half=true budget_150_unchanged=true native_keyboard=true exact_landing_contact=true no_teleport=true no_boost_jet_recovery=true human=false");
        }
        finally { ReleaseAll(); }
    }

    public static async Task RunUpperHighOnly(SceneTree tree)
    {
        ReleaseAll();
        try
        {
            await RunFreshSample(tree, new RoomCase("UpperPendulum",
                "res://scenes/prologue/rooms/UpperPendulum.tscn"), "mid_to_high_swing", .5f);
            GD.Print("THRUSTER_UPPER_HIGH_OK original_saved_room=true original_sample_half=true budget_150_unchanged=true native_keyboard=true exact_landing_contact=true no_teleport=true no_boost_jet_recovery=true human=false");
        }
        finally { ReleaseAll(); }
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
        PrologueRoom room = null;
        TraversalSegment2D segment;
        try
        {
            room = LoadRoom(roomCase.ScenePath);
            segment = room.GetNode<TraversalSegment2D>($"TraversalContracts/{segmentId}");
            if (OS.GetEnvironment(ZeroLandingEnvironment) == segmentId)
                segment.LandingZone = new Rect2(segment.LandingZone.Position,
                    new Vector2(0f, segment.LandingZone.Size.Y));
            AssertContractValid(roomCase.Name, segment, room);
        }
        catch
        {
            // Validation owns this unmounted instance; the later root cleanup cannot see it.
            room?.Free();
            throw;
        }

        var root = new Node2D { Name = $"Traversal_{roomCase.Name}_{segmentId}_{fraction:0.0}" };
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
                await DriveAssisted(tree, player, room, segment, trace, freshSample: true);
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
        TraversalSegment2D segment, RouteTrace trace, Func<Task> physicsFrame = null, bool freshSample = false)
    {
        string keyboardLandingPath = null;
        if (freshSample)
        {
            if (trace.Room == "BrokenToothCorridor" && trace.Segment == "entry_to_anchor_landing")
                keyboardLandingPath = "Geometry/AnchorLanding";
            else if (trace.Room == "UpperPendulum" && trace.Segment == "entry_to_lower_swing")
                keyboardLandingPath = "Geometry/LowerSwing";
            else if (trace.Room == "UpperPendulum" && trace.Segment == "lower_to_mid_swing")
                keyboardLandingPath = "Geometry/MidSwing";
            else if (trace.Room == "UpperPendulum" && trace.Segment == "mid_to_high_swing")
                keyboardLandingPath = "Geometry/HighSwing";
        }
        if (keyboardLandingPath != null)
        {
            await DriveFreshAssistedKeyboard(tree, player, room, segment, trace, keyboardLandingPath, physicsFrame);
            return;
        }
        GrappleSurface2D target = room.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
        int landingDirection = Mathf.Sign(segment.LandingZone.GetCenter().X - segment.LaunchZone.GetCenter().X);
        StringName horizontal = landingDirection < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        bool suppressClaw = OS.GetEnvironment(SuppressClawEnvironment) == segment.SegmentId.ToString();
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        Input.ActionPress(horizontal);
        Input.ActionPress(InputActions.Jump);
        if (!suppressClaw)
        {
            Input.ActionPress(InputActions.SelectGrappleMode);
            Input.ActionPress(InputActions.PrimaryAction);
        }

        bool cancellationPressed = false;
        try
        {
            for (int frame = 0; frame < AssistedBudget; frame++)
            {
                trace.Frame = frame;
                SampleClawInput(input, modes, claw, target.WorldAnchor);
                if (Input.IsActionPressed(InputActions.PrimaryAction))
                    Input.ActionRelease(InputActions.PrimaryAction);
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
                    if (!cancellationPressed &&
                        predictedLandingX >= segment.LandingZone.Position.X &&
                        predictedLandingX <= segment.LandingZone.End.X)
                    {
                        Input.ActionPress(InputActions.PrimaryAction);
                        cancellationPressed = true;
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
            Input.ActionRelease(horizontal);
            input.SetPhysicsProcess(true);
            modes.SetPhysicsProcess(true);
            claw.SetPhysicsProcess(true);
        }
    }

    private static async Task DriveFreshAssistedKeyboard(SceneTree tree, CharacterBody2D player,
        PrologueRoom room, TraversalSegment2D segment, RouteTrace trace, string landingPath, Func<Task> physicsFrame)
    {
        // Only the explicitly whitelisted fresh routes share this execution core.
        // Retain each original mounted .5 sample and setup allowance; not continuous Upper.
        // Every wait below spends the same actual 150-physics-tick route budget.
        string route = $"room={trace.Room} segment={trace.Segment} landingPath={landingPath}";
        ulong deadline = trace.BeginPhysicsFrame + AssistedBudget;
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = trace.Claw;
        var motor = trace.Motor;
        var target = room.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
        var landing = room.GetNode<StaticBody2D>(landingPath);
        var landingShape = landing.GetNode<CollisionShape2D>("Collision");
        var rectangle = (RectangleShape2D)landingShape.Shape;
        var bodyShape = player.GetNode<CollisionShape2D>("BodyCollider");
        var capsule = (CapsuleShape2D)bodyShape.Shape;
        // The saved fixtures share identity world axes. This is not a transformed-room solver.
        TestAssert.True(room.GlobalTransform.IsEqualApprox(Transform2D.Identity) &&
            player.GlobalTransform.X.IsEqualApprox(Vector2.Right) && player.GlobalTransform.Y.IsEqualApprox(Vector2.Down) &&
            landingShape.GlobalTransform.X.IsEqualApprox(Vector2.Right) && landingShape.GlobalTransform.Y.IsEqualApprox(Vector2.Down) &&
            bodyShape.GlobalTransform.X.IsEqualApprox(Vector2.Right) && bodyShape.GlobalTransform.Y.IsEqualApprox(Vector2.Down),
            $"{route}: saved room, landing and capsule use shared identity axes");
        float left = landingShape.GlobalPosition.X - rectangle.Size.X * .5f;
        float right = landingShape.GlobalPosition.X + rectangle.Size.X * .5f;
        float top = landingShape.GlobalPosition.Y - rectangle.Size.Y * .5f;
        float halfHeight = capsule.Height * .5f;
        float standingY = top - halfHeight - bodyShape.Position.Y;
        float stopX = segment.LandingZone.GetCenter().X;
        const float supportMargin = 2f;
        int initialJumps = motor.AcceptedJumpCount;
        int spaceDowns = 0, sampledSpaces = 0, sampledKs = 0, stableContact = 0;
        bool jumpHeld = false, firstKSampled = false, arrivalSeen = false, cancelSent = false;
        bool motorReleaseSeen = false, boostSeen = false, selectionSeen = false;
        bool releaseKSampled = false, releaseJumpHeld = false;
        ulong castId = 0, releaseTick = 0, releaseSequence = 0;
        ulong observedInputSequence = input.CurrentFrameSequence;
        int releaseCount = 0;
        float lastLandingAxis = float.NaN;
        Vector2 savedNormal = Vector2.Zero, savedDock = Vector2.Zero;
        Vector2 releaseBefore = Vector2.Zero, expectedProjection = Vector2.Zero;
        MechanicalClawFrame beforeCancel = default, nativeRelease = default;
        Exception releaseObservationFailure = null;
        var stages = new List<string>();
        var sampledEdges = new List<string>();
        using var driver = KeyboardRouteInputDriver.Begin(player);

        void Stage(string stage, string details) => stages.Add(
            $"{stage}:{route} physics={Engine.GetPhysicsFrames()} elapsed={Engine.GetPhysicsFrames() - trace.BeginPhysicsFrame} " +
            $"sample={input.CurrentFrameSequence} position={player.Position} velocity={player.Velocity} {details}");

        void ObserveInputSnapshot()
        {
            // PhysicsFrame precedes the next Input node sample, so it still exposes
            // the prior completed sample even when several physics ticks share one
            // ProcessFrame. Polling below also covers the last completed route tick.
            // Sequence dedup makes these two passive boundaries one receipt stream.
            ulong sequence = input.CurrentFrameSequence;
            if (sequence <= observedInputSequence) return;
            observedInputSequence = sequence;
            PlayerInputFrame sample = input.CurrentFrame;
            sampledSpaces += sample.JumpPressed ? 1 : 0;
            sampledKs += sample.ClawPressed ? 1 : 0;
            firstKSampled |= !cancelSent && sample.ClawPressed;
            if (sample.JumpPressed || sample.ClawPressed)
                sampledEdges.Add($"sequence={sequence}/observedPhysics={Engine.GetPhysicsFrames()}/" +
                    $"Space={sample.JumpPressed}/K={sample.ClawPressed}/keyboard={sample.KeyboardActions}");
        }

        void ObservePriorPhysicsInput()
        {
            if (Engine.GetPhysicsFrames() <= deadline) ObserveInputSnapshot();
            // Input samples at -300 after this signal. Issue each post-release
            // control from the prior completed move, not only after ProcessFrame
            // (which may span multiple physics steps).
            if (cancelSent && releaseCount > 0 && Engine.GetPhysicsFrames() > releaseTick &&
                Engine.GetPhysicsFrames() <= deadline)
            {
                driver.Command(InputActions.Claw, false);
                float axis = ChooseLandingAxis(player.Position, player.Velocity, player.IsOnFloor(),
                    checked((int)(deadline - Engine.GetPhysicsFrames() + 1UL)));
                CommandLandingAxis(axis);
                if (axis != lastLandingAxis)
                {
                    Stage("next_physics_landing_input", $"axis={axis} releasePhysics={releaseTick} beforeInputSample=true");
                    lastLandingAxis = axis;
                }
            }
        }

        Vector2 ProjectCancel(Vector2 velocity)
        {
            Vector2 normal = savedNormal.Normalized();
            float inward = velocity.Dot(normal);
            if (inward < 0f) velocity -= normal * inward;
            float limit = MechanicalClawSettings.Default.ReleaseSpeedLimit;
            return velocity.Length() > limit ? velocity.Normalized() * limit : velocity;
        }

        void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D published)
        {
            // Publish precedes ApplyMotor: callback body velocity is NOT the release result.
            boostSeen |= frame.State == MechanicalClawState.Boost || frame.BoostConsumed ||
                frame.ReleaseKind == MechanicalClawReleaseKind.Boost;
            if (!selectionSeen && driver.ClawEdges == 1 && frame.State == MechanicalClawState.Aim)
            {
                selectionSeen = true;
                savedNormal = frame.SurfaceNormal;
                Stage("initial_cast_selection", $"target={published?.GetInstanceId() ?? 0UL} " +
                    $"normal={savedNormal} rawK={Input.IsPhysicalKeyPressed(Key.K)} sampledK={input.Current.ClawPressed}");
            }
            if (frame.State == MechanicalClawState.Released)
            {
                releaseCount++;
                nativeRelease = frame;
                releaseBefore = player.Velocity;
                expectedProjection = ProjectCancel(releaseBefore);
                releaseTick = Engine.GetPhysicsFrames();
                releaseSequence = input.CurrentFrameSequence;
                releaseKSampled = input.Current.ClawPressed;
                releaseJumpHeld = input.Current.JumpHeld;
                Stage("native_release_pre_motor", $"cast={frame.CastId} release={frame.ReleaseKind} " +
                    $"expectedProjection={expectedProjection} boostDelta=(0,0) sampledK={input.Current.ClawPressed}");
            }
        }

        bool Supported(float centerX) => centerX + bodyShape.Position.X - capsule.Radius >= left + supportMargin &&
            centerX + bodyShape.Position.X + capsule.Radius <= right - supportMargin;

        bool HasLandingContact()
        {
            bool exact = false;
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
            {
                using KinematicCollision2D contact = player.GetSlideCollision(index);
                if (contact.GetCollider()?.GetInstanceId() == landing.GetInstanceId() &&
                    contact.GetNormal().Dot(Vector2.Up) > .99f &&
                    Mathf.Abs(contact.GetPosition().Y - top) <= 1f) exact = true;
            }
            float feet = bodyShape.GlobalPosition.Y + halfHeight;
            return exact && player.IsOnFloor() && Supported(player.Position.X) &&
                Mathf.Abs(feet - top) <= 1f && segment.LandingZone.HasPoint(player.Position);
        }

        void ObserveCompletedRelease()
        {
            if (releaseCount == 0 || motorReleaseSeen || releaseObservationFailure != null) return;
            try
            {
                TestAssert.True(Engine.GetPhysicsFrames() == releaseTick && input.Current.MoveAxis == 1f,
                    $"{route}: observe exact release Motor tick, before any next-tick braking");
                var expectedState = new PlayerMotionState { Velocity = expectedProjection,
                    GrappleRiseProtected = !beforeCancel.VelocityCorrection.IsZeroApprox() && expectedProjection.Y < 0f };
                var expectedModel = new PlayerMotionModel(motor.MotionSettings);
                var expectedInput = new PlayerInputFrame(1f, Vector2.Right, false, releaseJumpHeld, false, false, false);
                expectedState.Velocity = expectedModel.StepFree(expectedState, expectedInput, false,
                    1d / Engine.PhysicsTicksPerSecond, motor.ThrusterState.Module, Vector2.Zero, 0f, false, false).Velocity;
                Stage("actual_motor_after_release", $"releasePhysics={releaseTick} releaseSample={releaseSequence} " +
                    $"preProjection={releaseBefore} projected={expectedProjection} ordinaryTicks=1 " +
                    $"expectedOrdinary={expectedState.Velocity} actualMotor={player.Velocity} " +
                    $"nativeRelease={nativeRelease.ReleaseKind} preserve={motor.HasGrappleReleaseMomentum}");
                TestAssert.True(player.GetSlideCollisionCount() == 0 &&
                    player.Velocity.DistanceTo(expectedState.Velocity) <= 1f,
                    $"{route}: same-tick airborne Motor applies cancel projection plus ordinary D/gravity, no boost impulse");
                motorReleaseSeen = true;
            }
            catch (Exception exception) { releaseObservationFailure = exception; }
        }

        var releaseObserver = new ThrusterPostMotorObserver { Name = "ExactReleaseMotorObservation",
            ProcessPhysicsPriority = 100, Observe = ObserveCompletedRelease };
        player.AddChild(releaseObserver);

        async Task FreshSample()
        {
            ulong sequence = input.CurrentFrameSequence;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                TestAssert.True(Engine.GetPhysicsFrames() < deadline,
                    $"{route}: sampling cannot extend the actual 150-tick route deadline");
                await NextPhysicsFrame(tree, physicsFrame);
                TestAssert.True(Engine.GetPhysicsFrames() <= deadline, $"{route}: physics wait stays inside route deadline");
                // Observe the completed native Motor, not PhysicsFrame's pre-node signal boundary.
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(Engine.GetPhysicsFrames() <= deadline, $"{route}: post-Motor wait stays inside route deadline");
                if (input.CurrentFrameSequence <= sequence) continue;
                trace.Frame = checked((int)(Engine.GetPhysicsFrames() - trace.BeginPhysicsFrame));
                driver.ObserveFrame();
                trace.Observe(player, segment);
                ObserveInputSnapshot();
                if (releaseObservationFailure != null) throw releaseObservationFailure;
                if (jumpHeld && trace.Frame >= JumpReleaseFrame)
                {
                    driver.Command(InputActions.Jump, false);
                    jumpHeld = false;
                    Stage("space_up", "single ordinary jump; no further Space edge");
                }
                stableContact = trace.LeftFloor && HasLandingContact() ? stableContact + 1 : 0;
                TestAssert.True(!trace.Killed && trace.ExitRequests == 0 && motor.AcceptedDashCount == 0 && !boostSeen,
                    $"{route}: cannot recover, transition, dash or Boost through mandatory traversal");
                return;
            }
            throw new InvalidOperationException($"{route}: input sequence did not advance within bounded actual ticks");
        }

        float ChooseLandingAxis(Vector2 position, Vector2 velocity, bool onFloor, int remaining)
        {
            float distance = stopX - position.X;
            if (Mathf.Abs(distance) <= 3f && Mathf.Abs(velocity.X) <= 1f) return 0f;
            if (velocity.X * distance <= 0f) return Mathf.Sign(distance);
            var reverse = FreeMotionForecast.Reverse(velocity.X, onFloor, motor.MotionSettings,
                motor.ThrusterState.Module, 1d / Engine.PhysicsTicksPerSecond, remaining, .001f);
            return reverse.ForwardDistance >= Mathf.Abs(distance) ? -Mathf.Sign(velocity.X) : Mathf.Sign(distance);
        }

        void CommandLandingAxis(float axis)
        {
            driver.Command(InputActions.MoveLeft, axis < 0f);
            driver.Command(InputActions.MoveRight, axis > 0f);
        }

        bool PredictSafeCancel(out float predictedX, out int ticks)
        {
            Vector2 position = player.Position;
            Vector2 projected = ProjectCancel(player.Velocity);
            var state = new PlayerMotionState { Velocity = projected,
                GrappleRiseProtected = !claw.CurrentFrame.VelocityCorrection.IsZeroApprox() && projected.Y < 0f };
            var model = new PlayerMotionModel(motor.MotionSettings);
            MobilityModuleSettings module = motor.ThrusterState.Module;
            float seconds = 1f / Engine.PhysicsTicksPerSecond;
            int remaining = checked((int)(deadline - Engine.GetPhysicsFrames()));
            for (ticks = 1; ticks + 2 <= remaining; ticks++)
            {
                // First release step still samples the already-held D. Subsequent
                // steps execute the same bounded D/A reverse-braking plan as native input below.
                float axis = ticks == 1 ? 1f : ChooseLandingAxis(position, state.Velocity, false, remaining - ticks + 1);
                var sample = new PlayerInputFrame(axis, Vector2.Right, false, jumpHeld, false, false, false);
                state.Velocity = model.StepFree(state, sample, false, seconds, module,
                    Vector2.Zero, 0f, false, false).Velocity;
                if (state.Velocity.Y >= 0f) state.GrappleRiseProtected = false;
                position += state.Velocity * seconds;
                if (state.Velocity.Y >= 0f && position.Y >= standingY)
                {
                    predictedX = position.X;
                    return Supported(predictedX) && segment.LandingZone.HasPoint(new Vector2(predictedX, standingY));
                }
            }
            predictedX = float.NaN;
            return false;
        }

        claw.ClawFrameChanged += ObserveClaw;
        tree.PhysicsFrame += ObservePriorPhysicsInput;
        try
        {
            TestAssert.True(input.IsPhysicsProcessing() && claw.IsPhysicsProcessing() && motor.IsPhysicsProcessing() &&
                player.GetNode<PlayerActionModeController>("ActionModes").IsPhysicsProcessing(),
                $"{route}: keeps all production input and motion consumers live");
            driver.Command(InputActions.MoveRight, true);
            driver.Command(InputActions.MoveUp, true);
            await FreshSample();
            driver.Command(InputActions.Jump, true);
            spaceDowns++;
            jumpHeld = true;
            await FreshSample();
            TestAssert.Equal(initialJumps + 1, motor.AcceptedJumpCount, $"{route}: one real ordinary jump is accepted before K");
            TestAssert.True(!player.IsOnFloor() && claw.KeyboardPreview.Surface == target &&
                claw.KeyboardPreview.Snapshot.HasLineOfSight && !claw.KeyboardPreview.Snapshot.IsLegacyInteraction &&
                player.GlobalPosition.DistanceTo(claw.KeyboardPreview.Snapshot.WorldAnchor) <= MechanicalClawSettings.Default.MaxRange,
                $"{route}: fresh post-jump NE keyboard preview selects the exact visible authored target within 620px");
            savedNormal = claw.KeyboardPreview.Snapshot.SurfaceNormal;
            Stage("post_jump_preview", $"target={target.GetInstanceId()} normal={savedNormal} aim={input.KeyboardAimDirection}");
            bool suppressClaw = OS.GetEnvironment(SuppressClawEnvironment) == segment.SegmentId.ToString();
            if (!suppressClaw)
            {
                driver.Command(InputActions.Claw, true);
                Stage("first_k_raw", $"rawK={Input.IsPhysicalKeyPressed(Key.K)} edges={driver.ClawEdges}");
            }
            while (Engine.GetPhysicsFrames() < deadline)
            {
                await FreshSample();
                MechanicalClawFrame current = claw.CurrentFrame;
                if (!suppressClaw && castId == 0 && firstKSampled && trace.CastCount == 1 &&
                    current.CastId != 0 && current.LockedTargetRuntimeId == target.GetInstanceId())
                {
                    castId = current.CastId;
                    TestAssert.True(selectionSeen && current.SurfaceNormal.IsEqualApprox(savedNormal),
                        $"{route}: locked cast preserves the initial native selection normal");
                    savedDock = current.DockPoint;
                    Stage("first_k_sample_and_lock", $"cast={castId} target={current.LockedTargetRuntimeId} " +
                        $"sampledKCount={sampledKs} normal={savedNormal} dock={savedDock}");
                    driver.Command(InputActions.Claw, false);
                    driver.Command(InputActions.MoveUp, false);
                }
                if (!arrivalSeen && castId != 0 && current.CastId == castId &&
                    current.State == MechanicalClawState.Swing && current.Arrived &&
                    current.LockedTargetRuntimeId == target.GetInstanceId() &&
                    current.SurfaceNormal.IsEqualApprox(savedNormal) && current.DockPoint.IsEqualApprox(savedDock) &&
                    player.GlobalPosition.DistanceTo(current.DockPoint) <= MechanicalClawSettings.Default.ArrivalTolerance)
                {
                    arrivalSeen = true;
                    Stage("actual_arrival", $"cast={castId} state={current.State} arrived={current.Arrived} " +
                        $"normal={current.SurfaceNormal} dock={current.DockPoint} distance={player.GlobalPosition.DistanceTo(current.DockPoint)}");
                }
                if (arrivalSeen && !cancelSent && !jumpHeld && current.State == MechanicalClawState.Swing &&
                    current.Arrived && current.CastId == castId &&
                    current.LockedTargetRuntimeId == target.GetInstanceId() &&
                    current.SurfaceNormal.IsEqualApprox(savedNormal) && current.DockPoint.IsEqualApprox(savedDock) &&
                    player.GlobalPosition.DistanceTo(current.DockPoint) <= MechanicalClawSettings.Default.ArrivalTolerance &&
                    !Input.IsPhysicalKeyPressed(Key.K) &&
                    PredictSafeCancel(out float predictedX, out int predictedTicks))
                {
                    beforeCancel = current;
                    cancelSent = true;
                    Stage("second_k_edge_plan", $"cast={castId} expectedProjection={ProjectCancel(player.Velocity)} " +
                        $"predictedX={predictedX} predictedTicks={predictedTicks} standingY={standingY} " +
                        $"preserve={!current.VelocityCorrection.IsZeroApprox()} boostDelta=(0,0)");
                    driver.Command(InputActions.Claw, true);
                }
                else if (cancelSent && releaseCount > 0)
                {
                    driver.Command(InputActions.Claw, false);
                }
                if (stableContact >= 2 && trace.StableLandingFrames >= 2) break;
            }
            TestAssert.True(arrivalSeen && cancelSent && releaseCount == 1 && motorReleaseSeen &&
                nativeRelease.CastId == castId && nativeRelease.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                releaseKSampled && beforeCancel.LockedTargetRuntimeId == target.GetInstanceId() && !boostSeen,
                $"{route}: real Arrived-Swing is followed by one same-cast Released-Cancel and subsequent native Motor sample");
            TestAssert.True(stableContact >= 2 && HasLandingContact() && Engine.GetPhysicsFrames() <= deadline,
                $"{route}: two real exact landing upward contacts support the entire capsule inside original zone by tick 150");
            TestAssert.Equal(initialJumps + 1, motor.AcceptedJumpCount, $"{route}: mandatory route has exactly one accepted ordinary jump");
            TestAssert.True(spaceDowns == 1 && sampledSpaces == 1 && driver.ClawEdges == 2 && sampledKs == 2 &&
                driver.AttackEdges == 0 && driver.ShieldEdges == 0,
                $"{route}: bounded route proves one Space and two K edges without J, L or second Space");
            Stage("final_real_contact", $"contactStable={stableContact} feet={bodyShape.GlobalPosition.Y + halfHeight} " +
                $"top={top} fullSupport={Supported(player.Position.X)} K={driver.ClawEdges}/{sampledKs} Space={spaceDowns}/{sampledSpaces}");
        }
        finally
        {
            releaseObserver.Observe = null;
            releaseObserver.Free();
            tree.PhysicsFrame -= ObservePriorPhysicsInput;
            claw.ClawFrameChanged -= ObserveClaw;
            Stage("final_contact_observation", $"exactSupportedContact={HasLandingContact()} " +
                $"contactStable={stableContact} feet={bodyShape.GlobalPosition.Y + halfHeight} top={top} " +
                $"originalZone={segment.LandingZone.HasPoint(player.Position)} killed={trace.Killed} exits={trace.ExitRequests}");
            GD.Print($"ASSISTED_KEYBOARD_STAGES {route} K={driver.ClawEdges}/{sampledKs} Space={spaceDowns}/{sampledSpaces} " +
                $"actualElapsed={Engine.GetPhysicsFrames() - trace.BeginPhysicsFrame} budget={AssistedBudget} " +
                $"sampledEdges=[{string.Join(";", sampledEdges)}] stages=[{string.Join(";", stages)}]");
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
            _playerId = motor.GetParent().GetInstanceId();
        }

        public string Room { get; }
        public string Segment { get; }
        public float Sample { get; }
        public Vector2 Start { get; }
        public PlayerMotor Motor { get; }
        public MechanicalClawController Claw { get; }
        public int Frame { get; set; }
        public ulong BeginPhysicsFrame { get; private set; }
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
        private readonly ulong _playerId;
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
            BeginPhysicsFrame = Engine.GetPhysicsFrames();
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
            if (body.GetInstanceId() == _playerId) Killed = true;
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
