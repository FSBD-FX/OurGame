using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Shared;
using Knight.Tests.Integration;
using Knight.Vfx;

namespace Knight.Tests.Smoke;

public partial class PrologueCriticalPathSmoke : SceneTree
{
    private const int TotalFrameBudget = 4700;
    private int _frames;
    private PrologueLevelController _level;
    private CharacterBody2D _player;
    private ThreadExperienceRuntimeTests.ThreadRouteProbe _threadProbe;

    public override async void _Initialize()
    {
        try
        {
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            Root.AddChild(_level);
            await Frames(3);
            if (OS.HasEnvironment("KNIGHT_FORCE_SMOKE_FAILURE"))
                throw new InvalidOperationException("FORCED_SMOKE_FAILURE");
            _player = _level.Player;
            _player.GetNode<Camera2D>("Camera2D").Enabled = false;
            _threadProbe = new ThreadExperienceRuntimeTests.ThreadRouteProbe();
            _player.GetNode<ThreadAbility>("ThreadAbility").CastFrameChanged += _threadProbe.Observe;
            await CourtyardInputs();
            await CorridorTeaching();
            await LowerRouteToMechanism();
            await LaborerAndShortcut();
            await DeathRestoresPersistentShortcut();
            await UpperRouteAndLightPull();
            await BossDeathRetryAndCompletion();
            _threadProbe.AssertIntegratedRoute();
            GD.Print($"PROLOGUE_THREAD_EXPERIENCE_OK successful_casts={_threadProbe.SuccessfulCastCount} " +
                $"obstructed={_threadProbe.SawObstructedFailure}");
            GD.Print("PROLOGUE_THREAD_SMOKE_OK");
            GD.Print($"PROLOGUE_FRAME_BUDGET_OK used={_frames} total={TotalFrameBudget}");
            await Teardown();
            GD.Print("PROLOGUE_CRITICAL_PATH_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            await Teardown();
            Quit(1);
        }
    }

    private async Task Teardown()
    {
        ReleaseAll();
        if (IsInstanceValid(_player) && _threadProbe != null)
            _player.GetNodeOrNull<ThreadAbility>("ThreadAbility").CastFrameChanged -= _threadProbe.Observe;
        if (IsInstanceValid(_level))
            await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);
        _level = null;
        _player = null;
        // The complete level owns exported Godot Array wrappers. Finalize them before
        // native shutdown so both success and intentional-failure probes exit cleanly.
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    private async Task CourtyardInputs()
    {
        Stage("courtyard_move_jump_attack", 180);
        TestAssert.Equal(PrologueRoomId.AwakeningCourtyard, _level.CurrentRoomId,
            "critical path starts in Awakening Courtyard");
        float startX = _player.GlobalPosition.X;
        Press(InputActions.MoveRight);
        await Frames(18);
        Release(InputActions.MoveRight);
        TestAssert.True(_player.GlobalPosition.X > startX + 35,
            "real movement input advances the player");
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
        TestAssert.True(minimumY < startY - 35, "real jump input produces a visible jump");
        int attacks = 0;
        _player.GetNode<PlayerCombat>("Combat").AttackStarted += (_, _) => attacks++;
        Press(InputActions.Attack);
        await Frame();
        Release(InputActions.Attack);
        await Frames(8);
        TestAssert.Equal(1, attacks, "real attack input starts exactly one attack");
    }

    private async Task CorridorTeaching()
    {
        Stage("corridor_checkpoint_combat_thread", 460);
        await MoveThroughExit("Exits/ToCorridor", PrologueRoomId.BrokenToothCorridor, 600,
            jumpContinuously: true);
        await Frames(3);
        LegacyPrologueCheckpointSnapshot beforeFirstNode = _level.LegacyCheckpointForFixture;
        TestAssert.True(beforeFirstNode.Room == PrologueRoomId.AwakeningCourtyard &&
            beforeFirstNode.Entrance == "start",
            $"before real checkpoint contact the last activated node remains courtyard/start; " +
            $"actual={beforeFirstNode.Room}/{beforeFirstNode.Entrance}");
        TestAssert.True(_player.GetNode<HealthComponent>("Health").ApplyDamage(
                new DamageRequest(999, Vector2.Right, 0f, 86001UL)),
            "first pre-checkpoint death is accepted after the real room transition settles");
        for (int frame = 0; frame < 120 &&
            _level.CurrentRoomId != PrologueRoomId.AwakeningCourtyard; frame++)
            await Frame();
        await Frames(3);
        TestAssert.Equal(PrologueRoomId.AwakeningCourtyard, _level.CurrentRoomId,
            "first death before checkpoint contact restores the previous courtyard node");
        LegacyPrologueCheckpointSnapshot afterFirstDeath = _level.LegacyCheckpointForFixture;
        TestAssert.True(afterFirstDeath.Room == PrologueRoomId.AwakeningCourtyard &&
            afterFirstDeath.Entrance == "start" && _player.IsOnFloor(),
            $"first death cannot advance to the unactivated corridor checkpoint; " +
            $"actual={afterFirstDeath.Room}/{afterFirstDeath.Entrance} player={_player.Position}");
        GD.Print("PROLOGUE_PRECHECKPOINT_RECOVERY_OK");
        await MoveThroughExit("Exits/ToCorridor", PrologueRoomId.BrokenToothCorridor, 600,
            jumpContinuously: true);
        RustShellSquire squire = _level.CurrentRoom.GetNode<RustShellSquire>("Actors/RustShellSquire");
        var attackProbe = new CombatReadabilityExperienceTests.AttackRouteProbe(
            "real corridor Rust-Shell", squire.GetNode<Hitbox>("AttackHitbox"),
            squire.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider"));
        squire.AttackFrameChanged += attackProbe.Observe;
        Fixture(squire.GlobalPosition + new Vector2(-52, 0));
        _player.GetNode<HealthComponent>("Health").MaxHealth = 99;
        _player.GetNode<HealthComponent>("Health").ResetFull();
        PlayerMotor attackObservationMotor = _player.GetNode<PlayerMotor>("Motor");
        attackObservationMotor.SetPhysicsProcess(false);
        try
        {
            await WaitForAttackKind(attackProbe, EnemyAttackKind.RustShellThrust, 90);
        }
        finally
        {
            attackObservationMotor.SetPhysicsProcess(true);
        }
        attackProbe.AssertObserved(EnemyAttackKind.RustShellThrust);
        HealthComponent enemyHealth = squire.GetNode<HealthComponent>("Health");
        int before = enemyHealth.CurrentHealth;
        Fixture(new Vector2(-132, 198));
        await PlayerStrike(InputActions.MoveRight);
        TestAssert.True(enemyHealth.CurrentHealth < before,
            "real attack area contact damages the corridor enemy");
        while (enemyHealth.CurrentHealth > 0)
        {
            Fixture(squire.GlobalPosition + new Vector2(-52, 0));
            await PlayerStrike(InputActions.MoveRight);
        }
        TestAssert.Equal(0, enemyHealth.CurrentHealth,
            "real player hitboxes clear the observed corridor attacker before traversal");

        TraversalSegment2D entryJumpSegment = _level.CurrentRoom.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_floor_to_teaching_ledge");
        TraversalSegment2D corridorSegment = _level.CurrentRoom.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_to_anchor_landing");
        Marker2D corridorEntrance = _level.CurrentRoom.GetNode<Marker2D>("Entrances/from_courtyard");
        Fixture(corridorEntrance.GlobalPosition);
        await Frames(3);
        Press(InputActions.MoveRight);
        for (int frame = 0; frame < 120 && _player.Position.X < -155f; frame++) await Frame();
        Release(InputActions.MoveRight);
        TestAssert.True(_player.Position.X >= -165f && _player.Position.X <= -135f && _player.IsOnFloor(),
            $"real corridor traversal takes a supported run-up before the teaching ledge; player={_player.Position}");
        PlayerMotor corridorMotor = _player.GetNode<PlayerMotor>("Motor");
        int acceptedDashesBeforeLesson = corridorMotor.AcceptedDashCount;
        bool sawSingleJumpLaunch = entryJumpSegment.LaunchZone.HasPoint(_player.Position) &&
            _player.IsOnFloor();
        bool sawSingleJumpLanding = false;
        int stableThreadLaunchFrames = 0;
        Press(InputActions.MoveRight);
        bool lessonJumpPressed = false;
        int lessonJumpFrames = 0;
        var lessonTrajectory = new List<string>();
        for (int frame = 0; frame < 180 && stableThreadLaunchFrames < 2; frame++)
        {
            if (!lessonJumpPressed && entryJumpSegment.LaunchZone.HasPoint(_player.Position))
            {
                Press(InputActions.Jump);
                lessonJumpPressed = true;
            }
            if (lessonJumpPressed && Input.IsActionPressed(InputActions.Jump) && ++lessonJumpFrames >= 19)
                Release(InputActions.Jump);
            if (_player.Position.X >= corridorSegment.LaunchZone.GetCenter().X)
                Release(InputActions.MoveRight);
            await Frame();
            sawSingleJumpLaunch |= entryJumpSegment.LaunchZone.HasPoint(_player.Position) &&
                _player.IsOnFloor();
            sawSingleJumpLanding |= entryJumpSegment.LandingZone.HasPoint(_player.Position) &&
                _player.IsOnFloor();
            stableThreadLaunchFrames = corridorSegment.LaunchZone.HasPoint(_player.Position) &&
                _player.IsOnFloor() ? stableThreadLaunchFrames + 1 : 0;
            if (frame % 10 == 0)
                lessonTrajectory.Add($"{frame}:{_player.Position}/{_player.Velocity}/floor={_player.IsOnFloor()}");
        }
        Release(InputActions.Jump);
        Release(InputActions.MoveRight);
        TestAssert.True(sawSingleJumpLaunch && sawSingleJumpLanding,
            $"continuous corridor input crosses the authored entry SingleJump launch and landing zones; " +
            $"launch={sawSingleJumpLaunch} landing={sawSingleJumpLanding} " +
            $"trajectory=[{string.Join(';', lessonTrajectory)}]");
        TestAssert.True(corridorSegment.LaunchZone.HasPoint(_player.Position) && _player.IsOnFloor(),
            $"continuous corridor movement reaches the authored thread launch zone; " +
            $"player={_player.Position} trajectory=[{string.Join(';', lessonTrajectory)}]");
        TestAssert.Equal(acceptedDashesBeforeLesson, corridorMotor.AcceptedDashCount,
            "corridor entry SingleJump reaches the teaching ledge without Dash or a hidden second jump");
        TestAssert.True(_level.Progress.CompletedTutorials.Contains("thread_anchor"),
            "real tutorial area records the thread lesson");
        ThreadTarget2D anchor = _level.CurrentRoom.GetNode<ThreadTarget2D>("Geometry/ThreadAnchor");
        ThreadAbility ability = _player.GetNode<ThreadAbility>("ThreadAbility");
        float holdDistance = _player.GlobalPosition.DistanceTo(anchor.GlobalPosition);
        TestAssert.True(holdDistance > ability.Tuning.TapAssistRange &&
            holdDistance <= ability.Tuning.HoldRange,
            $"authored hold-aim proof starts outside Tap range and inside Hold range; " +
            $"distance={holdDistance:0.0} tap={ability.Tuning.TapAssistRange:0.0} hold={ability.Tuning.HoldRange:0.0}");
        int hits = 0;
        ability.ThreadHit += () => hits++;
        ability.ResetTransientState();
        Vector2 assistedAim = _player.GlobalPosition.DirectionTo(anchor.GlobalPosition);
        PressAim(assistedAim);
        Press(InputActions.Jump);
        Press(InputActions.Thread);
        ThreadCastFrame anchorCast = default;
        int stableLandingFrames = 0;
        for (int frame = 0; frame < 180 && stableLandingFrames < 2; frame++)
        {
            await Frame();
            if (ability.CurrentCastFrame.State == ThreadCastState.Apply)
                anchorCast = ability.CurrentCastFrame;
            if (frame == 11) Release(InputActions.Thread);
            if (frame == 12) Release(InputActions.MoveUp);
            if (frame == 18) Release(InputActions.Jump);
            if (_player.Position.X >= corridorSegment.LandingZone.GetCenter().X)
                Release(InputActions.MoveRight);
            stableLandingFrames = corridorSegment.LandingZone.HasPoint(_player.Position) &&
                _player.IsOnFloor() ? stableLandingFrames + 1 : 0;
        }
        Release(InputActions.Thread);
        Release(InputActions.Jump);
        ReleaseAim();
        TestAssert.Equal(ThreadCastState.Apply, anchorCast.State,
            $"real hold-aim helper reaches Apply on the authored corridor anchor; failure={anchorCast.Failure}");
        TestAssert.Equal(anchor.GetInstanceId(), anchorCast.TargetId,
            "real assisted Hold locks the authored corridor anchor outside Tap range");
        TestAssert.Equal(1, hits, "real aim and thread input selects the authored corridor anchor");
        GD.Print("PROLOGUE_THREAD_SMOKE_HOLD_OK");

        TestAssert.True(stableLandingFrames >= 2 &&
            corridorSegment.LandingZone.HasPoint(_player.Position) && _player.IsOnFloor(),
            $"real hold cast carries the player into the authored landing zone; player={_player.Position}");

        var wall = new StaticBody2D
        {
            Name = "ThreadSmokeWall",
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None,
            GlobalPosition = _player.GlobalPosition.Lerp(anchor.GlobalPosition, .5f)
        };
        wall.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(24, 180) }
        });
        _level.CurrentRoom.AddChild(wall);
        await Frame();
        PlayerMotor motor = _player.GetNode<PlayerMotor>("Motor");
        motor.SetPhysicsProcess(false);
        _player.Velocity = Vector2.Zero;
        int hitsBeforeBlockedCast = hits;
        try
        {
            try
            {
                ThreadCastFrame blocked = await HoldAimAndReleaseThread(
                    _player.GlobalPosition.DirectionTo(anchor.GlobalPosition), 12);
                TestAssert.Equal(ThreadCastState.Failed, blocked.State,
                    "real wall obstruction ends in Failed");
                TestAssert.Equal(ThreadFailureReason.Obstructed, blocked.Failure,
                    "real wall obstruction exposes the named failure");
                TestAssert.Equal(hitsBeforeBlockedCast, hits,
                    "wall-obstructed thread emits no compatibility hit");
                TestAssert.True(_player.Velocity.IsZeroApprox(),
                    "wall-obstructed thread applies zero gameplay pull");
                await WaitForThreadIdle();
                AssertThreadPresentationClear("wall-obstructed cast");
            }
            finally
            {
                if (IsInstanceValid(wall)) wall.QueueFree();
                await Frame();
            }

            Vector2 retryDirection = _player.GlobalPosition.DirectionTo(anchor.GlobalPosition);
            ThreadCastFrame retry = await RetryThreadWithoutReset(retryDirection, 12);
            TestAssert.Equal(ThreadCastState.Apply, retry.State,
                $"natural retry after Obstructed reaches Apply; failure={retry.Failure}");
            TestAssert.Equal(anchor.GetInstanceId(), retry.TargetId,
                "natural retry locks the same authored anchor without ResetTransientState");
            TestAssert.Equal(hitsBeforeBlockedCast + 1, hits,
                "natural retry emits exactly one new compatibility hit");
            TestAssert.True(_player.Velocity.Dot(retryDirection) > 1f,
                $"natural retry applies a real pull toward the authored anchor; velocity={_player.Velocity}");
            await WaitForThreadIdle();
            AssertThreadPresentationClear("natural retry recovery");
        }
        finally
        {
            motor.SetPhysicsProcess(true);
        }
        GD.Print("PROLOGUE_THREAD_SMOKE_OBSTRUCTED_OK");
        GD.Print("PROLOGUE_THREAD_SMOKE_NATURAL_RETRY_OK");

        Press(InputActions.MoveRight);
        bool checkpointJumped = false;
        int checkpointJumpFrames = 0;
        for (int frame = 0; frame < 300; frame++)
        {
            if (!checkpointJumped && _player.Position.X >= 840f)
            {
                Press(InputActions.Jump);
                checkpointJumped = true;
            }
            if (checkpointJumped && ++checkpointJumpFrames >= 19) Release(InputActions.Jump);
            await Frame();
            LegacyPrologueCheckpointSnapshot current = _level.LegacyCheckpointForFixture;
            if (current.Room == PrologueRoomId.BrokenToothCorridor && current.Entrance == "checkpoint" &&
                _player.Position.X >= 1030f && _player.IsOnFloor()) break;
        }
        Release(InputActions.Jump);
        Release(InputActions.MoveRight);
        LegacyPrologueCheckpointSnapshot checkpoint = _level.LegacyCheckpointForFixture;
        TestAssert.True(checkpoint.Room == PrologueRoomId.BrokenToothCorridor &&
            checkpoint.Entrance == "checkpoint",
            $"continuous movement across the real corridor gap records its checkpoint; " +
            $"actual={checkpoint.Room}/{checkpoint.Entrance}");
        TestAssert.True(_player.Position.X >= 1030f && _player.IsOnFloor(),
            $"corridor route lands on the supported completion floor before choosing a branch; " +
            $"player={_player.Position}");
    }

    private async Task UpperRouteAndLightPull()
    {
        Stage("upper_route_light_enemy_pull", 2400);
        PressAim(Vector2.Right);
        Press(InputActions.Thread);
        await Frames(12);
        TestAssert.Equal(ThreadCastState.Preview,
            _player.GetNode<ThreadAbility>("ThreadAbility").CurrentCastFrame.State,
            "real held input reaches Preview before room transition");
        RoomExit transition = _level.CurrentRoom.GetNode<RoomExit>("Exits/ToUpper");
        bool jumpPressed = false;
        int jumpFrames = 0;
        for (int frame = 0; frame < 360 && _level.CurrentRoomId == PrologueRoomId.BrokenToothCorridor; frame++)
        {
            if (!jumpPressed && _player.GlobalPosition.X >= transition.GlobalPosition.X - 80f)
            {
                Press(InputActions.Jump);
                jumpPressed = true;
            }
            if (jumpPressed && ++jumpFrames >= 19) Release(InputActions.Jump);
            await Frame();
        }
        Release(InputActions.Jump);
        Release(InputActions.Thread);
        ReleaseAim();
        TestAssert.Equal(PrologueRoomId.UpperPendulum, _level.CurrentRoomId,
            "real room exit transitions while thread Preview is active");
        await Frames(2);
        AssertThreadPresentationClear("room transition");
        GD.Print("PROLOGUE_THREAD_SMOKE_ROOM_CLEANUP_OK");

        PrologueRoom upper = _level.CurrentRoom;
        GraveLampMoth moth = upper.GetNode<GraveLampMoth>("Actors/air");
        var mothAttackProbe = new CombatReadabilityExperienceTests.AttackRouteProbe(
            "real Upper Grave-Lamp Moth", moth.GetNode<Hitbox>("AttackHitbox"),
            moth.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider"));
        moth.AttackFrameChanged += mothAttackProbe.Observe;
        await PrologueTraversalRouteTests.TraverseUpperContractsContinuously(
            this, upper, _player, Frame);
        PlayerMotor motor = _player.GetNode<PlayerMotor>("Motor");
        await ObserveMothAttackIfNeeded(moth, mothAttackProbe, motor);
        mothAttackProbe.AssertObserved(EnemyAttackKind.MothDive);
        PositionForMothThreadCast(moth);
        moth.SetPhysicsProcess(false);
        motor.SetPhysicsProcess(false);
        ThreadCastFrame mothCast;
        try
        {
            mothCast = await HoldAimAndReleaseThread(
                _player.GlobalPosition.DirectionTo(moth.GlobalPosition), 12);
            TestAssert.Equal(ThreadCastState.Apply, mothCast.State,
                $"real hold-aim helper reaches Apply on the light enemy; failure={mothCast.Failure}");
            TestAssert.Equal(moth.GetNode<ThreadTarget2D>("ThreadTarget").GetInstanceId(), mothCast.TargetId,
                "real hold-aim helper selects the light enemy ThreadTarget");
            TestAssert.True(moth.Velocity.Length() > 100f,
                $"real Apply delivers pull velocity to the light enemy; velocity={moth.Velocity}");
        }
        finally
        {
            moth.SetPhysicsProcess(true);
            motor.SetPhysicsProcess(true);
        }
        for (int frame = 0; frame < 12 && moth.CurrentState != GraveLampMothState.ThreadStunned; frame++)
            await Frame();
        TestAssert.Equal(GraveLampMothState.ThreadStunned, moth.CurrentState,
            "real thread input pulls and stuns the light aerial enemy on its next physics update");
        await PrologueTraversalRouteTests.ReachUpperMechanismExitContinuously(
            this, upper, _player, Frame);
        for (int frame = 0; frame < 8 && _level.CurrentRoomId != PrologueRoomId.BellChainMechanism; frame++)
            await Frame();
        TestAssert.Equal(PrologueRoomId.BellChainMechanism, _level.CurrentRoomId,
            "continuous Upper route transitions through the real mechanism exit");
        BellChainMechanism restored = _level.CurrentRoom.GetNode<BellChainMechanism>("Mechanisms/BellChain");
        TestAssert.True(restored.IsOpen && restored.GetNode<CollisionShape2D>("Door/Collision").Disabled,
            "upper route reload applies the shortcut progress preserved through death");
    }

    private async Task LowerRouteToMechanism()
    {
        Stage("lower_route_real_exit_contracts", 1550);
        await MoveThroughExit("Exits/ToLower", PrologueRoomId.LowerBuriedMachinery, 600,
            jumpContinuously: true, jumpUntilWorldX: 900f);
        TestAssert.Equal(PrologueRoomId.LowerBuriedMachinery, _level.CurrentRoom.RoomId,
            "real corridor lower exit instantiates the authored Lower Buried Machinery room");
        TraversalSegment2D segment = _level.CurrentRoom.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_to_arena");
        bool sawLaunch = false;
        bool sawLanding = false;
        bool jumpPressed = false;
        int jumpFrames = 0;
        Press(InputActions.MoveRight);
        for (int frame = 0; frame < 720 && _level.CurrentRoomId == PrologueRoomId.LowerBuriedMachinery; frame++)
        {
            Vector2 local = _player.Position;
            sawLaunch |= segment.LaunchZone.HasPoint(local);
            sawLanding |= segment.LandingZone.HasPoint(local) && _player.IsOnFloor();
            if (!jumpPressed && sawLaunch)
            {
                Press(InputActions.Jump);
                jumpPressed = true;
            }
            if (jumpPressed && ++jumpFrames >= 12) Release(InputActions.Jump);
            await Frame();
        }
        Release(InputActions.Jump);
        Release(InputActions.MoveRight);
        TestAssert.True(sawLaunch && sawLanding,
            $"continuous Lower route crosses its authored launch and landing zones; " +
            $"launch={sawLaunch} landing={sawLanding}");
        TestAssert.Equal(PrologueRoomId.BellChainMechanism, _level.CurrentRoomId,
            "continuous Lower route reaches the real mechanism exit without a fixture teleport");
        GD.Print("PROLOGUE_SIX_ROOM_LOWER_ROUTE_OK");
    }

    private async Task LaborerAndShortcut()
    {
        Stage("laborer_exposure_and_shortcut", 2150);
        BellBackLaborer laborer = _level.CurrentRoom.GetNode<BellBackLaborer>("Actors/laborer");
        var laborerAttackProbe = new CombatReadabilityExperienceTests.AttackRouteProbe(
            "real mechanism Bell-Back Laborer", laborer.GetNode<Hitbox>("AttackHitbox"),
            laborer.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider"));
        laborer.AttackFrameChanged += laborerAttackProbe.Observe;
        Fixture(laborer.GlobalPosition + new Vector2(-140, 0));
        PlayerMotor observationMotor = _player.GetNode<PlayerMotor>("Motor");
        observationMotor.SetPhysicsProcess(false);
        try
        {
            await WaitForAttackKind(laborerAttackProbe, EnemyAttackKind.LaborerSweep, 180);
        }
        finally
        {
            observationMotor.SetPhysicsProcess(true);
        }
        laborerAttackProbe.AssertObserved(EnemyAttackKind.LaborerSweep);
        Fixture(laborer.GlobalPosition + new Vector2(-190, 35));
        ThreadCastFrame laborerCast = await HoldAimAndReleaseThread(Vector2.Right, 12);
        TestAssert.Equal(ThreadCastState.Apply, laborerCast.State,
            $"real hold-aim helper reaches Apply on the heavy tutorial target; failure={laborerCast.Failure}");
        TestAssert.Equal(BellBackLaborerState.Exposed, laborer.CurrentState,
            "real thread input exposes the heavy laborer without pulling it");

        BellChainMechanism mechanism = _level.CurrentRoom.GetNode<BellChainMechanism>("Mechanisms/BellChain");
        Fixture(mechanism.GlobalPosition + new Vector2(-190, 40));
        float tapStartX = _player.GlobalPosition.X;
        ThreadCastFrame mechanismCast = await TapThread();
        TestAssert.Equal(ThreadCastState.Apply, mechanismCast.State,
            "neutral tap helper reaches Apply on the Bell-Chain mechanism");
        TestAssert.Equal(ThreadAimMode.Tap, mechanismCast.AimMode,
            "neutral tap helper remains typed Tap through Bell-Chain Apply");
        TestAssert.True(Mathf.Abs(_player.GlobalPosition.X - tapStartX) < .5f,
            "neutral tap uses no directional movement input");
        TestAssert.True(mechanism.IsOpen && _level.Progress.BellChainShortcutOpen,
            "real thread input opens and persists the Bell-Chain shortcut");
        TestAssert.True(mechanism.GetNode<CollisionShape2D>("Door/Collision").Disabled,
            "opening the shortcut removes the physical gate");
        GD.Print("PROLOGUE_THREAD_SMOKE_TAP_OK");
    }

    private async Task ObserveMothAttackIfNeeded(GraveLampMoth moth,
        CombatReadabilityExperienceTests.AttackRouteProbe mothAttackProbe, PlayerMotor motor)
    {
        if (mothAttackProbe.HasActive(EnemyAttackKind.MothDive)) return;
        Fixture(moth.GlobalPosition + new Vector2(-80, 150));
        motor.SetPhysicsProcess(false);
        try
        {
            await WaitForAttackKind(mothAttackProbe, EnemyAttackKind.MothDive, 180);
        }
        finally
        {
            motor.SetPhysicsProcess(true);
        }
    }

    private void PositionForMothThreadCast(GraveLampMoth moth)
    {
        Fixture(moth.GlobalPosition + new Vector2(-180, 0));
    }

    private async Task DeathRestoresPersistentShortcut()
    {
        Stage("death_recovery_and_shortcut_persistence", 2300);
        BellBackLaborer laborer = _level.CurrentRoom.GetNode<BellBackLaborer>("Actors/laborer");
        Fixture(laborer.GlobalPosition + new Vector2(-140, 0));
        _player.GetNode<HealthComponent>("Health").MaxHealth = 99;
        _player.GetNode<HealthComponent>("Health").ResetFull();
        PlayerMotor motor = _player.GetNode<PlayerMotor>("Motor");
        Hurtbox hurtbox = _player.GetNode<Hurtbox>("Hurtbox");
        motor.SetPhysicsProcess(false);
        hurtbox.Monitorable = false;
        try
        {
            PressAim(Vector2.Right);
            Press(InputActions.Thread);
            await Frames(12);
            TestAssert.Equal(ThreadCastState.Preview,
                _player.GetNode<ThreadAbility>("ThreadAbility").CurrentCastFrame.State,
                "real held input reaches Preview before death");
            ThreadLineVisual line = _player.GetNode<ThreadLineVisual>("ThreadLine");
            ThreadShuttleVisual shuttle = _player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle");
            PrologueVfxDirector vfx = _level.GetNode<PrologueVfxDirector>("Vfx");
            TestAssert.True(line.ActiveStrandCount > 0 && !shuttle.IsClear &&
                vfx.ActiveThreadEffectCount > 0,
                "death cleanup precondition owns a live thread line, shuttle, and typed effect");
            await AssertDangerPresentationActive(laborer);
        }
        finally
        {
            motor.SetPhysicsProcess(true);
            hurtbox.Monitorable = true;
        }
        TestAssert.True(_player.GetNode<HealthComponent>("Health").ApplyDamage(
                new DamageRequest(999, Vector2.Right, 0f, 87001UL)),
            "fatal cleanup probe is accepted immediately after real attack danger becomes active");
        ThreadAbility dyingAbility = _player.GetNode<ThreadAbility>("ThreadAbility");
        TestAssert.True(dyingAbility.CurrentCastFrame.State == ThreadCastState.Failed &&
            dyingAbility.CurrentCastFrame.Failure == ThreadFailureReason.Cancelled,
            $"fatal damage synchronously cancels the active thread cast before recovery starts; " +
            $"actual={dyingAbility.CurrentCastFrame.State}/{dyingAbility.CurrentCastFrame.Failure}");
        TestAssert.Equal(0, _player.GetNode<ThreadLineVisual>("ThreadLine").ActiveStrandCount,
            "fatal damage synchronously clears the thread line before recovery starts");
        TestAssert.True(_player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle").IsClear,
            "fatal damage synchronously clears the thread shuttle before recovery starts");
        Release(InputActions.Thread);
        ReleaseAim();
        await Frames(5);
        TestAssert.Equal(PrologueRoomId.BrokenToothCorridor, _level.CurrentRoomId,
            "death restores the last real checkpoint room");
        TestAssert.True(_player.IsOnFloor() && _player.Position.X >= 1030f,
            $"checkpoint recovery settles on the authored CompletionFloor; player={_player.Position}");
        TestAssert.True(_level.Progress.BellChainShortcutOpen,
            "death recovery preserves permanent shortcut progress");
        AssertThreadPresentationClear("death recovery");
        GD.Print("PROLOGUE_THREAD_SMOKE_DEATH_CLEANUP_OK");
    }

    private async Task BossDeathRetryAndCompletion()
    {
        Stage("boss_entry_and_death_retry", 3150);
        await ReachBossExitContinuously();
        Fixture(_level.CurrentRoom.GetNode<Marker2D>("Entrances/boss_retry").GlobalPosition);
        await Frames(3);
        BossArenaController arena = _level.CurrentRoom.GetNode<BossArenaController>("BossArenaController");
        Area2D trigger = _level.CurrentRoom.GetNode<Area2D>("ActivationTrigger");
        Fixture(trigger.GlobalPosition);
        await Frames(3);
        TestAssert.True(arena.IsEncounterActive, "real activation area starts the Boss encounter");
        await Frames(65);
        ulong firstBoss = arena.CurrentBoss.GetInstanceId();
        _player.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(999, Vector2.Right, 0f, 87002UL));
        await Frames(5);
        arena = _level.CurrentRoom.GetNode<BossArenaController>("BossArenaController");
        TestAssert.True(arena.CurrentBoss.GetInstanceId() != firstBoss &&
            arena.CurrentBoss.CurrentFrame.State == FallenBellkeeperState.Dormant,
            "Boss death recovery replaces the fight with one clean dormant Boss");
        Press(InputActions.MoveRight);
        for (int i = 0; i < 180 && !arena.IsEncounterActive; i++) await Frame();
        Release(InputActions.MoveRight);
        TestAssert.True(arena.IsEncounterActive,
            "Boss retry is reachable from the checkpoint with real movement within three seconds");
        FallenBellkeeper boss = arena.CurrentBoss;
        var bossAttackProbe = new CombatReadabilityExperienceTests.AttackRouteProbe(
            "real Fallen Bellkeeper", boss.GetNode<Hitbox>("AttackHitbox"),
            boss.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider"));
        boss.AttackFrameChanged += bossAttackProbe.Observe;
        _player.GetNode<HealthComponent>("Health").MaxHealth = 99;
        _player.GetNode<HealthComponent>("Health").ResetFull();
        PlayerMotor bossObservationMotor = _player.GetNode<PlayerMotor>("Motor");
        bossObservationMotor.SetPhysicsProcess(false);
        try
        {
            Fixture(boss.GlobalPosition + new Vector2(-110, 0));
            bossObservationMotor.SetPhysicsProcess(false);
            for (int frame = 0; frame < 480 &&
                !(bossAttackProbe.HasActive(EnemyAttackKind.BossSweep) &&
                  bossAttackProbe.HasActive(EnemyAttackKind.BossLeapSlam) &&
                  bossAttackProbe.HasActive(EnemyAttackKind.BossGroundWave)); frame++) await Frame();
        }
        finally
        {
            bossObservationMotor.SetPhysicsProcess(true);
        }
        bossAttackProbe.AssertObserved(EnemyAttackKind.BossSweep);
        bossAttackProbe.AssertObserved(EnemyAttackKind.BossLeapSlam);
        bossAttackProbe.AssertObserved(EnemyAttackKind.BossGroundWave);

        Stage("boss_phase_transition", 4000);
        HealthComponent bossHealth = boss.GetNode<HealthComponent>("Health");
        Hurtbox bossHurtbox = boss.GetNode<Hurtbox>("Hurtbox");
        PlayerCombat playerCombat = _player.GetNode<PlayerCombat>("Combat");
        int bossHitConnections = 0;
        playerCombat.AttackConnected += hurtbox =>
        {
            if (hurtbox == bossHurtbox) bossHitConnections++;
        };
        _player.GetNode<HealthComponent>("Health").MaxHealth = 99;
        _player.GetNode<HealthComponent>("Health").ResetFull();
        int transitionHealth = Mathf.FloorToInt(
            bossHealth.MaxHealth * boss.Tuning.TransitionHealthRatio);
        int strikes = 0;
        while (bossHealth.CurrentHealth > transitionHealth && strikes++ < 10)
        {
            PositionForBossStrike(boss);
            int before = bossHealth.CurrentHealth;
            int connectionsBefore = bossHitConnections;
            await PlayerStrike(InputActions.MoveRight);
            TestAssert.True(bossHealth.CurrentHealth == before - 1 &&
                bossHitConnections == connectionsBefore + 1,
                "each phase-one health loss must come from one connected formal player hitbox");
        }
        await Frames(2);
        TestAssert.Equal(FallenBellkeeperState.Transition, boss.CurrentFrame.State,
            "real player attacks trigger the Boss health-ratio phase transition");
        int phaseOneHits = bossHitConnections;

        Stage("boss_chain_exposure_and_final_hit", 4100);
        int exposureWindows = 0;
        while (bossHealth.CurrentHealth > 0 && exposureWindows++ < 3)
        {
            for (int i = 0; i < 260 && boss.CurrentFrame.State != FallenBellkeeperState.HammerThrown; i++)
                await Frame();
            TestAssert.Equal(FallenBellkeeperState.HammerThrown, boss.CurrentFrame.State,
                "Boss reaches the authored thrown-hammer thread window");
            Fixture(boss.GlobalPosition + new Vector2(-190, 30));
            ThreadCastFrame bossCast = await HoldAimAndReleaseThread(Vector2.Right, 12);
            TestAssert.Equal(ThreadCastState.Apply, bossCast.State,
                $"real hold-aim helper reaches Apply on the Boss hammer chain; failure={bossCast.Failure}");
            TestAssert.Equal(FallenBellkeeperState.Exposed, boss.CurrentFrame.State,
                "real player thread input opens the Boss weak point");
            while (bossHealth.CurrentHealth > 0 && boss.CurrentFrame.State == FallenBellkeeperState.Exposed)
            {
                PositionForBossStrike(boss);
                int before = bossHealth.CurrentHealth;
                int connectionsBefore = bossHitConnections;
                await PlayerStrike(InputActions.MoveRight);
                TestAssert.True(bossHealth.CurrentHealth == before - 1 &&
                    bossHitConnections == connectionsBefore + 1,
                    "each exposed health loss must come from one connected formal player hitbox");
            }
        }
        await Frames(3);
        TestAssert.True(phaseOneHits == 5 && bossHitConnections == 12,
            $"approved 12-health Boss must take five phase-one and seven exposed player hits; " +
            $"phase_one={phaseOneHits}, total={bossHitConnections}");
        GD.Print($"PROLOGUE_BOSS_REAL_PLAYER_DAMAGE_OK phase_one_hits={phaseOneHits} " +
            $"total_hits={bossHitConnections} health={bossHealth.CurrentHealth}");
        PrologueCompletionController completion = _level.GetNode<PrologueCompletionController>("Completion");
        TestAssert.True(bossHealth.CurrentHealth == 0 && _level.Progress.BossDefeated,
            "real final player attack defeats the Boss and records progress");
        TestAssert.Equal(PrologueObjective.Complete, completion.CurrentObjective,
            "critical path reaches the terminal prologue objective");
        TestAssert.Equal(1, completion.CompletionCount,
            "completion is emitted exactly once");
        Label objective = _level.GetNode<Label>("Hud/Objective");
        TestAssert.True(objective.Text.Contains("PROLOGUE COMPLETE") && objective.Visible,
            "completion remains visibly readable while player control continues");
        bossAttackProbe.AssertObserved(EnemyAttackKind.BossHammerThrow);
        GD.Print("PROLOGUE_COMBAT_EXPERIENCE_OK ordinary=3 boss=4 geometry=TRUE");
    }

    private async Task ReachBossExitContinuously()
    {
        PrologueRoom mechanismRoom = _level.CurrentRoom;
        TestAssert.Equal(PrologueRoomId.BellChainMechanism, mechanismRoom.RoomId,
            "boss approach begins in the real Bell-Chain Mechanism room");
        await TraverseSingleJumpContinuously(mechanismRoom.GetNode<TraversalSegment2D>(
            "TraversalContracts/arrival_to_chain_floor"));
        await TraverseSingleJumpContinuously(mechanismRoom.GetNode<TraversalSegment2D>(
            "TraversalContracts/chain_floor_to_completion_dais"));

        RoomExit shortcut = mechanismRoom.GetNode<RoomExit>("Exits/Shortcut");
        bool shortcutRequested = false;
        void OnShortcutRequested(StringName _) => shortcutRequested = true;
        shortcut.ExitRequested += OnShortcutRequested;
        bool jumpPressed = false;
        int jumpFrames = 0;
        Press(InputActions.MoveRight);
        try
        {
            for (int frame = 0; frame < 240 &&
                _level.CurrentRoomId == PrologueRoomId.BellChainMechanism; frame++)
            {
                if (!jumpPressed && _player.Position.X >= 900f)
                {
                    Press(InputActions.Jump);
                    jumpPressed = true;
                }
                if (jumpPressed && ++jumpFrames >= 19) Release(InputActions.Jump);
                await Frame();
            }
        }
        finally
        {
            Release(InputActions.Jump);
            Release(InputActions.MoveRight);
            if (IsInstanceValid(shortcut)) shortcut.ExitRequested -= OnShortcutRequested;
        }
        TestAssert.True(!shortcutRequested,
            "real CompletionDais jump passes above Shortcut without triggering it");
        TestAssert.Equal(PrologueRoomId.FallenBellkeeperArena, _level.CurrentRoomId,
            "real CompletionDais jump reaches P1Complete and enters the Boss arena");
        GD.Print("PROLOGUE_MECHANISM_BOSS_ROUTE_OK");
    }

    private async Task TraverseSingleJumpContinuously(TraversalSegment2D segment)
    {
        int dashBefore = _player.GetNode<PlayerMotor>("Motor").AcceptedDashCount;
        int castsBefore = _threadProbe.SuccessfulCastCount;
        int stableLaunch = 0;
        Press(InputActions.MoveRight);
        for (int frame = 0; frame < 360 && stableLaunch < 2; frame++)
        {
            await Frame();
            stableLaunch = segment.LaunchZone.HasPoint(_player.Position) && _player.IsOnFloor()
                ? stableLaunch + 1 : 0;
        }
        TestAssert.True(stableLaunch >= 2,
            $"continuous mechanism route reaches supported launch zone for {segment.SegmentId}");

        Press(InputActions.Jump);
        int jumpFrames = 0;
        int stableLanding = 0;
        for (int frame = 0; frame < 120 && stableLanding < 2; frame++)
        {
            await Frame();
            if (++jumpFrames >= 19) Release(InputActions.Jump);
            if (_player.Position.X >= segment.LandingZone.GetCenter().X)
                Release(InputActions.MoveRight);
            stableLanding = segment.LandingZone.HasPoint(_player.Position) && _player.IsOnFloor()
                ? stableLanding + 1 : 0;
        }
        Release(InputActions.Jump);
        Release(InputActions.MoveRight);
        TestAssert.True(stableLanding >= 2,
            $"continuous mechanism route lands inside {segment.SegmentId} target zone; player={_player.Position}");
        TestAssert.Equal(dashBefore, _player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,
            $"continuous mechanism route uses zero Dash for {segment.SegmentId}");
        TestAssert.Equal(castsBefore, _threadProbe.SuccessfulCastCount,
            $"continuous mechanism route uses zero Thread casts for {segment.SegmentId}");
    }

    private async Task MoveThroughExit(string path, PrologueRoomId expected, int frameBudget,
        bool jumpContinuously = false, float jumpUntilWorldX = float.PositiveInfinity)
    {
        RoomExit exit = _level.CurrentRoom.GetNode<RoomExit>(path);
        PrologueRoomId source = _level.CurrentRoomId;
        StringName horizontal = exit.GlobalPosition.X < _player.GlobalPosition.X
            ? InputActions.MoveLeft : InputActions.MoveRight;
        int jumpFrames = 0;
        Press(horizontal);
        for (int frame = 0; frame < frameBudget && _level.CurrentRoomId == source; frame++)
        {
            bool beforeJumpLimit = horizontal == InputActions.MoveRight
                ? _player.GlobalPosition.X < jumpUntilWorldX
                : _player.GlobalPosition.X > jumpUntilWorldX;
            if (jumpContinuously && beforeJumpLimit && _player.IsOnFloor() &&
                !Input.IsActionPressed(InputActions.Jump))
            {
                Press(InputActions.Jump);
                jumpFrames = 0;
            }
            if (Input.IsActionPressed(InputActions.Jump) && ++jumpFrames >= 11)
                Release(InputActions.Jump);
            await Frame();
        }
        Release(InputActions.Jump);
        Release(horizontal);
        TestAssert.Equal(expected, _level.CurrentRoomId,
            $"continuous real movement through {path} reaches {expected}; " +
            $"player={_player.GlobalPosition} frame_budget={frameBudget}");
    }

    private async Task PlayerStrike(StringName facingAction)
    {
        PlayerCombat combat = _player.GetNode<PlayerCombat>("Combat");
        for (int i = 0; i < 30 && combat.IsAttacking; i++) await Frame();
        TestAssert.True(!combat.IsAttacking, "player combat recovers before the next real attack input");
        Press(facingAction);
        Press(InputActions.Attack);
        await Frames(2);
        Release(InputActions.Attack);
        for (int i = 0; i < 30 && combat.IsAttacking; i++) await Frame();
        Release(facingAction);
        TestAssert.True(!combat.IsAttacking, "real player attack completes without a fixture reset");
    }

    private async Task<ThreadCastFrame> TapThread()
    {
        ReleaseAim();
        ThreadAbility ability = _player.GetNode<ThreadAbility>("ThreadAbility");
        ability.ResetTransientState();
        Press(InputActions.Thread);
        await Frames(2);
        Release(InputActions.Thread);
        return await WaitForThreadResult(ability);
    }

    private async Task<ThreadCastFrame> HoldAimAndReleaseThread(Vector2 aim, int holdFrames)
    {
        ThreadAbility ability = _player.GetNode<ThreadAbility>("ThreadAbility");
        ability.ResetTransientState();
        PressAim(aim);
        Press(InputActions.Thread);
        await Frames(Mathf.Max(10, holdFrames));
        Release(InputActions.Thread);
        await Frame();
        ReleaseAim();
        return await WaitForThreadResult(ability);
    }

    private async Task<ThreadCastFrame> RetryThreadWithoutReset(Vector2 aim, int holdFrames)
    {
        ThreadAbility ability = _player.GetNode<ThreadAbility>("ThreadAbility");
        TestAssert.Equal(ThreadCastState.Idle, ability.CurrentCastFrame.State,
            "natural retry starts from runtime Idle without ResetTransientState");
        PressAim(aim);
        Press(InputActions.Thread);
        await Frames(Mathf.Max(10, holdFrames));
        Release(InputActions.Thread);
        await Frame();
        ReleaseAim();
        return await WaitForThreadResult(ability);
    }

    private async Task<ThreadCastFrame> WaitForThreadResult(ThreadAbility ability)
    {
        for (int index = 0; index < 90; index++)
        {
            if (ability.CurrentCastFrame.State is ThreadCastState.Apply or ThreadCastState.Failed)
                return ability.CurrentCastFrame;
            await Frame();
        }
        throw new TimeoutException(
            $"thread smoke timed out waiting for Apply/Failed; current={ability.CurrentCastFrame.State}");
    }

    private async Task WaitForThreadIdle()
    {
        ThreadAbility ability = _player.GetNode<ThreadAbility>("ThreadAbility");
        for (int index = 0; index < 90 && ability.CurrentCastFrame.State != ThreadCastState.Idle; index++)
            await Frame();
        TestAssert.Equal(ThreadCastState.Idle, ability.CurrentCastFrame.State,
            "thread smoke returns to Idle within its bounded recovery");
    }

    private async Task WaitForAttackKind(CombatReadabilityExperienceTests.AttackRouteProbe probe,
        EnemyAttackKind kind, int frameBudget)
    {
        for (int frame = 0; frame < frameBudget && !probe.HasActive(kind); frame++) await Frame();
    }

    private async Task AssertDangerPresentationActive(BellBackLaborer laborer)
    {
        PrologueVfxDirector vfx = _level.GetNode<PrologueVfxDirector>("Vfx");
        bool sawRealActive = false;
        void Observe(AttackPresentationFrame frame) =>
            sawRealActive |= frame.Timeline.Phase == AttackPhase.Active;
        laborer.AttackFrameChanged += Observe;
        try
        {
            for (int frame = 0; frame < 180 &&
                !(sawRealActive && vfx.ActiveDangerCount > 0); frame++) await Frame();
        }
        finally
        {
            laborer.AttackFrameChanged -= Observe;
        }
        TestAssert.True(sawRealActive && vfx.ActiveDangerCount > 0 &&
            vfx.TypedEffectCount > 0,
            $"cleanup boundary must begin with a real nonzero attack danger VFX; " +
            $"active={sawRealActive} danger={vfx.ActiveDangerCount} " +
            $"typed={vfx.TypedEffectCount}");
        GD.Print("PROLOGUE_ATTACK_VFX_PRECONDITION_OK");
    }

    private void AssertThreadPresentationClear(string boundary)
    {
        ThreadAbility ability = _player.GetNode<ThreadAbility>("ThreadAbility");
        ThreadLineVisual line = _player.GetNode<ThreadLineVisual>("ThreadLine");
        ThreadShuttleVisual shuttle = _player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle");
        PrologueVfxDirector vfx = _level.GetNode<PrologueVfxDirector>("Vfx");
        TestAssert.Equal(ThreadCastState.Idle, ability.CurrentCastFrame.State,
            $"{boundary} clears the thread cast state");
        TestAssert.Equal(0, line.ActiveStrandCount, $"{boundary} clears every thread strand");
        TestAssert.True(shuttle.IsClear, $"{boundary} clears the shuttle visual");
        TestAssert.True(vfx.TypedEffectCount == 0 && vfx.ActiveDangerCount == 0 &&
            vfx.ActiveThreadEffectCount == 0,
            $"{boundary} clears attack, danger, and thread VFX presentation");
    }

    private static void PressAim(Vector2 aim)
    {
        ReleaseAim();
        if (aim.X < -.01f) Press(InputActions.MoveLeft);
        if (aim.X > .01f) Press(InputActions.MoveRight);
        if (aim.Y < -.01f) Press(InputActions.MoveUp);
        if (aim.Y > .01f) Press(InputActions.MoveDown);
    }

    private static void ReleaseAim()
    {
        Release(InputActions.MoveLeft);
        Release(InputActions.MoveRight);
        Release(InputActions.MoveUp);
        Release(InputActions.MoveDown);
    }

    private void Fixture(Vector2 position)
    {
        _player = _level.Player;
        _player.GlobalPosition = position;
        _player.Velocity = Vector2.Zero;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        _player.GetNodeOrNull<ThreadAbility>("ThreadAbility")?.ResetTransientState();
    }

    private void PositionForBossStrike(FallenBellkeeper boss)
    {
        _player.GlobalPosition = boss.GlobalPosition + new Vector2(-52, 0);
        _player.Velocity = Vector2.Zero;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
    }

    private void Stage(string name, int latestFrame)
    {
        if (_frames > latestFrame)
            throw new TimeoutException($"stage '{name}' began after fixed budget {latestFrame} at frame {_frames}");
        GD.Print($"PROLOGUE_STAGE={name} frame={_frames} budget={latestFrame}");
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await Frame();
    }

    private async Task Frame()
    {
        if (++_frames > TotalFrameBudget)
            throw new TimeoutException($"prologue smoke exceeded {TotalFrameBudget} physics frames");
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
        Release(InputActions.SelectAttackMode);
        Release(InputActions.SelectGrappleMode);
        Release(InputActions.PrimaryAction);
        Release(InputActions.Pause);
    }
}
