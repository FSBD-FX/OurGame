using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Shared;

namespace Knight.Tests.Smoke;

public partial class P0CoreSliceSmoke : SceneTree
{
    private const int FrameBudget = 720;
    private const int ThreadRouteBudget = 150;
    private const int ThreadPreviewBudget = 14;
    private const int ThreadJumpReleaseFrame = 18;
    private static readonly StringName CorridorCheckpointEntrance = "checkpoint";
    private int _frames;
    private PrologueLevelController _level;
    private CharacterBody2D _player;

    public override async void _Initialize()
    {
        try
        {
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            Root.AddChild(_level);
            await Frames(3);
            _player = _level.Player;
            ulong playerId = _player.GetInstanceId();

            await VerifyMoveJumpAndAttack();
            await EnterCorridor();
            TestAssert.Equal(playerId, _level.Player.GetInstanceId(),
                "P0 smoke keeps the persistent player across rooms");
            _player = _level.Player;
            await AttackSquire();
            await ActivateCheckpoint();
            await PullAcrossGap();
            await EnterCompletionExit();

            ReleaseAll();
            await TeardownLevel();
            GD.Print("P0_CORE_SLICE_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            ReleaseAll();
            await TeardownLevel();
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private async Task TeardownLevel()
    {
        if (_level != null && GodotObject.IsInstanceValid(_level))
            await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);
        _level = null;
        _player = null;
    }

    private async Task VerifyMoveJumpAndAttack()
    {
        TestAssert.Equal(PrologueRoomId.AwakeningCourtyard, _level.CurrentRoomId,
            "P0 smoke starts in courtyard");
        GD.Print("P0_STAGE=move");
        float startX = _player.GlobalPosition.X;
        Press(InputActions.MoveRight);
        await Frames(24);
        Release(InputActions.MoveRight);
        TestAssert.True(_player.GlobalPosition.X > startX + 55,
            "real InputMap movement advances through courtyard");

        GD.Print("P0_STAGE=jump");
        float startY = _player.GlobalPosition.Y;
        Press(InputActions.Jump);
        await Frames(5);
        Release(InputActions.Jump);
        float minimumY = _player.GlobalPosition.Y;
        for (int i = 0; i < 18; i++)
        {
            await Frame();
            minimumY = Mathf.Min(minimumY, _player.GlobalPosition.Y);
        }
        TestAssert.True(minimumY < startY - 35, "real InputMap jump leaves the floor visibly");

        GD.Print("P0_STAGE=attack_input");
        int attacks = 0;
        PlayerCombat combat = _player.GetNode<PlayerCombat>("Combat");
        combat.AttackStarted += (_, _) => attacks++;
        Press(InputActions.Attack);
        await Frame();
        Release(InputActions.Attack);
        await Frames(4);
        TestAssert.Equal(1, attacks, "real InputMap attack starts exactly one player attack");
    }

    private async Task EnterCorridor()
    {
        GD.Print("P0_STAGE=room_transition");
        Fixture(new Vector2(1040, 190));
        await Frames(5);
        TestAssert.Equal(PrologueRoomId.BrokenToothCorridor, _level.CurrentRoomId,
            "real RoomExit contact enters Broken-Tooth Corridor");
    }

    private async Task AttackSquire()
    {
        GD.Print("P0_STAGE=first_combat");
        RustShellSquire enemy = _level.CurrentRoom.GetNode<RustShellSquire>("Actors/RustShellSquire");
        HealthComponent health = enemy.GetNode<HealthComponent>("Health");
        int before = health.CurrentHealth;
        Fixture(new Vector2(-132, 198));
        await Frame();
        Press(InputActions.MoveRight);
        Press(InputActions.Attack);
        await Frame();
        Release(InputActions.Attack);
        Release(InputActions.MoveRight);
        await Frames(14);
        TestAssert.True(health.CurrentHealth < before,
            "real player attack contact damages the corridor Rust-Shell");
    }

    private async Task ActivateCheckpoint()
    {
        GD.Print("P0_STAGE=checkpoint");
        CheckpointTrigger checkpoint = _level.CurrentRoom.GetNode<CheckpointTrigger>("Checkpoint");
        Fixture(checkpoint.GlobalPosition);
        await WaitForCorridorCheckpoint(checkpoint);
        LegacyPrologueCheckpointSnapshot snapshot = _level.LegacyCheckpointForFixture;
        TestAssert.Equal(PrologueRoomId.BrokenToothCorridor, snapshot.Room,
            "real checkpoint contact records corridor room");
        TestAssert.Equal(CorridorCheckpointEntrance, snapshot.Entrance,
            "real checkpoint contact records stable entrance");
    }

    private async Task WaitForCorridorCheckpoint(CheckpointTrigger trigger)
    {
        const int maxPhysicsFrames = 8;
        int physicsFrames = 0;
        while (physicsFrames <= maxPhysicsFrames)
        {
            LegacyPrologueCheckpointSnapshot snapshot = _level.LegacyCheckpointForFixture;
            if (_level.CurrentRoomId == PrologueRoomId.BrokenToothCorridor &&
                snapshot.Room == PrologueRoomId.BrokenToothCorridor &&
                snapshot.Entrance == CorridorCheckpointEntrance)
                return;

            if (physicsFrames == maxPhysicsFrames) break;
            await Frame();
            physicsFrames++;
        }

        LegacyPrologueCheckpointSnapshot finalSnapshot = _level.LegacyCheckpointForFixture;
        throw new InvalidOperationException(
            $"Timed out waiting for corridor checkpoint; trigger={trigger.GlobalPosition}; " +
            $"player={_player.GlobalPosition}; actual={finalSnapshot.Room}/{finalSnapshot.Entrance}; " +
            $"frames={physicsFrames}/{maxPhysicsFrames}");
    }

    private async Task PullAcrossGap()
    {
        GD.Print("P0_STAGE=thread_anchor_gap");
        TraversalSegment2D segment = _level.CurrentRoom.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_to_anchor_landing");
        ThreadTarget2D anchor = _level.CurrentRoom.GetNode<ThreadTarget2D>(segment.AssistTargetPath);
        Fixture(_level.CurrentRoom.ToGlobal(segment.LaunchZone.GetCenter()));
        ThreadAbility thread = _player.GetNode<ThreadAbility>("ThreadAbility");
        float distance = _player.GlobalPosition.DistanceTo(anchor.GlobalPosition);
        TestAssert.True(distance > thread.Tuning.TapAssistRange && distance <= thread.Tuning.HoldRange,
            $"authored P0 anchor starts outside Tap range and inside Hold range; " +
            $"distance={distance:0.0} tap={thread.Tuning.TapAssistRange:0.0} " +
            $"hold={thread.Tuning.HoldRange:0.0}");
        int hits = 0;
        thread.ThreadHit += OnThreadHit;
        try
        {
            Vector2 aim = _player.GlobalPosition.DirectionTo(anchor.GlobalPosition);
            StringName horizontal = aim.X < 0f ? InputActions.MoveLeft : InputActions.MoveRight;
            StringName vertical = aim.Y < 0f ? InputActions.MoveUp : InputActions.MoveDown;
            int landingDirection = Mathf.Sign(
                segment.LandingZone.GetCenter().X - segment.LaunchZone.GetCenter().X);
            ThreadCastFrame applied = default;
            ThreadCastFrame cast = thread.CurrentCastFrame;
            Vector2 applyVelocity = Vector2.Zero;
            bool previewObserved = false;
            bool threadReleased = false;
            int stableLandingFrames = 0;
            int routeFrames = 0;

            Press(horizontal);
            Press(vertical);
            Press(InputActions.Jump);
            Press(InputActions.Thread);
            for (routeFrames = 1; routeFrames <= ThreadRouteBudget; routeFrames++)
            {
                await Frame();
                cast = thread.CurrentCastFrame;
                if (!threadReleased && cast.State == ThreadCastState.Preview)
                {
                    previewObserved = true;
                    threadReleased = true;
                    Release(InputActions.Thread);
                }
                if (!threadReleased && routeFrames >= ThreadPreviewBudget)
                    throw new TimeoutException(ThreadRouteDiagnostics(segment, anchor, thread, aim,
                        distance, cast, routeFrames, "preview_not_observed"));
                if (cast.State == ThreadCastState.Apply)
                {
                    applied = cast;
                    applyVelocity = _player.Velocity;
                }
                if (routeFrames == ThreadJumpReleaseFrame) Release(InputActions.Jump);
                if (threadReleased && routeFrames > ThreadPreviewBudget) Release(vertical);
                if (PassedHorizontalCenter(_level.CurrentRoom.ToLocal(_player.GlobalPosition).X,
                        segment.LandingZone.GetCenter().X, landingDirection))
                    Release(horizontal);

                Vector2 roomLocalPlayer = _level.CurrentRoom.ToLocal(_player.GlobalPosition);
                stableLandingFrames = segment.LandingZone.HasPoint(roomLocalPlayer) && _player.IsOnFloor()
                    ? stableLandingFrames + 1 : 0;
                if (previewObserved && applied.State == ThreadCastState.Apply && hits == 1 &&
                    stableLandingFrames >= 2)
                    break;
            }

            TestAssert.True(previewObserved,
                ThreadRouteDiagnostics(segment, anchor, thread, aim, distance, cast, routeFrames,
                    "preview_missing"));
            TestAssert.Equal(ThreadCastState.Apply, applied.State,
                ThreadRouteDiagnostics(segment, anchor, thread, aim, distance, cast, routeFrames,
                    "apply_missing"));
            TestAssert.Equal(anchor.GetInstanceId(), applied.TargetId,
                "real held input applies to the authored traversal target");
            TestAssert.Equal(1, hits,
                ThreadRouteDiagnostics(segment, anchor, thread, aim, distance, cast, routeFrames,
                    "thread_hit_missing"));
            TestAssert.True(applyVelocity.Dot(aim) > 300f,
                ThreadRouteDiagnostics(segment, anchor, thread, aim, distance, cast, routeFrames,
                    $"insufficient_pull_velocity={applyVelocity}"));
            TestAssert.True(stableLandingFrames >= 2 && _player.IsOnFloor() &&
                segment.LandingZone.HasPoint(_level.CurrentRoom.ToLocal(_player.GlobalPosition)),
                ThreadRouteDiagnostics(segment, anchor, thread, aim, distance, cast, routeFrames,
                    "landing_missing"));
        }
        finally
        {
            ReleaseAll();
            thread.ThreadHit -= OnThreadHit;
        }

        void OnThreadHit() => hits++;
    }

    private string ThreadRouteDiagnostics(TraversalSegment2D segment, ThreadTarget2D anchor,
        ThreadAbility ability, Vector2 aim, float distance, ThreadCastFrame cast, int routeFrames,
        string failure) =>
        $"P0 thread route failed; segment={segment.SegmentId}; distance={distance:0.0}; " +
        $"tap={ability.Tuning.TapAssistRange:0.0}; hold={ability.Tuning.HoldRange:0.0}; " +
        $"aim={aim}; state={cast.State}; failure={cast.Failure}; " +
        $"frames={routeFrames}/{ThreadRouteBudget}; player={_player.GlobalPosition}; " +
        $"anchor={anchor.GlobalPosition}; route_failure={failure}";

    private static bool PassedHorizontalCenter(float position, float center, int direction) =>
        direction < 0 ? position <= center : position >= center;

    private async Task EnterCompletionExit()
    {
        GD.Print("P0_STAGE=completion_exit");
        ulong previousRoom = _level.CurrentRoom.GetInstanceId();
        RoomExit completion = _level.CurrentRoom.GetNode<RoomExit>("Exits/P0Complete");
        Fixture(completion.GlobalPosition);
        await Frames(5);
        TestAssert.Equal(PrologueRoomId.BrokenToothCorridor, _level.CurrentRoomId,
            "P0 completion marker resolves its declared route");
        TestAssert.True(previousRoom != _level.CurrentRoom.GetInstanceId(),
            "P0 completion marker executes a real room transition");
        Marker2D checkpoint = _level.CurrentRoom.GetNode<Marker2D>("Entrances/checkpoint");
        TestAssert.True(_level.Player.GlobalPosition.DistanceTo(checkpoint.GlobalPosition) < 5f,
            $"P0 completion marker lands at authored checkpoint before ordinary physics resumes; " +
            $"player={_level.Player.GlobalPosition}; checkpoint={checkpoint.GlobalPosition}");
        PrologueHud hud = _level.GetNode<PrologueHud>("Hud");
        Label prompt = hud.GetNode<Label>("Prompt");
        Label objective = hud.GetNode<Label>("Objective");
        TestAssert.True(!prompt.Visible && string.IsNullOrEmpty(prompt.Text),
            "room transition clears the prior room tutorial prompt");
        TestAssert.Equal("P0 CORE SLICE COMPLETE", objective.Text,
            "temporary P0 completion exit gives visible completion feedback");
    }

    private void Fixture(Vector2 position)
    {
        _player = _level.Player;
        _player.GlobalPosition = position;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        _player.GetNodeOrNull<ThreadAbility>("ThreadAbility")?.ResetTransientState();
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await Frame();
    }

    private async Task Frame()
    {
        if (++_frames > FrameBudget) throw new TimeoutException($"P0 smoke exceeded {FrameBudget} physics frames");
        await ToSignal(this, SceneTree.SignalName.PhysicsFrame);
    }

    private static void Press(StringName action) => Input.ActionPress(action);
    private static void Release(StringName action) => Input.ActionRelease(action);

    private static void ReleaseAll()
    {
        Release(InputActions.MoveLeft);
        Release(InputActions.MoveRight);
        Release(InputActions.MoveUp);
        Release(InputActions.MoveDown);
        Release(InputActions.Jump);
        Release(InputActions.Dash);
        Release(InputActions.Attack);
        Release(InputActions.Thread);
    }
}
