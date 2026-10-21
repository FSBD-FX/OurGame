using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class P1LoopRuntimeTests
{
    public static async Task RunHiddenRecoveryOnly(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        Task7Case original = Task7Cases(tree)[1];
        TestAssert.Equal("P1LoopRuntimeTests.HiddenDiscoveryPersistsAcrossDeathRecovery", original.Name,
            "narrow P1 recovery invokes the original registered case");
        await original.Execute();
        GD.Print("THRUSTER_P1_HIDDEN_RECOVERY_OK same_registered_case=true saved_formal_main=true public_request=true health_death=false human=false full_route=false");
    }

    public static async Task RunRecoveryExitBoundaryOnly(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await RecoveryExitBoundaryCase(tree, deferRemoval: true);
        await RecoveryExitBoundaryCase(tree, deferRemoval: false);
        GD.Print("THRUSTER_P1_RECOVERY_EXIT_OK cases=2 public_request=true private_resume_invoked=false health_death=false human=false full_route=false");
    }

    private static async Task RecoveryExitBoundaryCase(SceneTree tree, bool deferRemoval)
    {
        string removal = deferRemoval ? "deferred" : "callback";
        PrologueLevelController level = await LoadLevel(tree, "PendingRecoveryExitFixture_" + removal);
        CharacterBody2D player = level.Player;
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        ulong levelId = level.GetInstanceId();
        ulong playerId = player.GetInstanceId();
        int completed = 0;
        bool completionClosed = false;
        bool removed = false;
        Exception primaryFailure = null;
        void RemoveOwnedLevel()
        {
            if (!GodotObject.IsInstanceValid(level) || !level.IsInsideTree() || level.IsQueuedForDeletion()) return;
            tree.Root.RemoveChild(level);
            removed = true;
            GD.Print($"P1_RECOVERY_EXIT_TRACE stage=removed removal={removal} physics={Engine.GetPhysicsFrames()} level={levelId} player={playerId} inside={level.IsInsideTree()} player_processing={player.IsPhysicsProcessing()}");
        }
        PrologueLevelController.RecoveryCompletedEventHandler completionObserver = reason =>
        {
            completed++;
            completionClosed = !player.IsPhysicsProcessing() && !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing();
            GD.Print($"P1_RECOVERY_EXIT_TRACE stage=completion removal={removal} physics={Engine.GetPhysicsFrames()} level={levelId} player={playerId} completed={completed} closed={completionClosed} reason={reason}");
            // Exercise both public callback exits: before resume is entered, and
            // after it has entered the first process wait. Never invoke resume ourselves.
            if (deferRemoval) Callable.From(RemoveOwnedLevel).CallDeferred();
            else RemoveOwnedLevel();
        };
        level.RecoveryCompleted += completionObserver;
        try
        {
            level.RequestRespawn("P1 EXIT DURING RECOVERY");
            int observedProcessFrames = 0;
            while (!removed && observedProcessFrames < 4)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                observedProcessFrames++;
            }
            TestAssert.True(completed == 1 && completionClosed && removed && !level.IsInsideTree(),
                "exit probe reaches detached-owner window after one completion but before gameplay resumption");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(GodotObject.IsInstanceValid(level) && GodotObject.IsInstanceValid(player) &&
                level.GetInstanceId() == levelId && player.GetInstanceId() == playerId &&
                !level.IsInsideTree() && !player.IsPhysicsProcessing() && !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing(),
                "pending public recovery never resumes or replaces the detached owner and player");
            GD.Print($"P1_RECOVERY_EXIT_CASE_OK removal={removal} completed=1 detached_owner=true original_player=true gameplay_stayed_disabled=true " +
                $"process_observation={observedProcessFrames} process_cap=4 physics_observation=1 " +
                "public_request=true health_death=false human=false full_route=false");
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    level.RecoveryCompleted -= completionObserver;
                    if (level.IsInsideTree()) await Dispose(tree, level);
                    else level.Free();
                }
                GD.Print($"P1_RECOVERY_EXIT_CLEANUP_OK removal={removal} primary_failure={primaryFailure != null} owned_level_only=true");
            }
            catch (Exception cleanupFailure)
            {
                if (primaryFailure != null) throw new AggregateException(primaryFailure, cleanupFailure);
                throw;
            }
        }
    }

    public static async Task Run(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
    }

    internal static IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Task7CaseRegistry.Case(nameof(P1LoopRuntimeTests),
            nameof(UpperAndLowerRealExitRoutesBothReachConvergence),
            PrologueCanyonLegacyReplacementTests.ExactMandatoryRouteConvergesOnTemple),
        Task7CaseRegistry.Case(nameof(P1LoopRuntimeTests),
            nameof(HiddenDiscoveryPersistsAcrossDeathRecovery),
            () => HiddenDiscoveryPersistsAcrossDeathRecovery(tree)),
        Task7CaseRegistry.Case(nameof(P1LoopRuntimeTests),
            nameof(PartialLowerEncounterResetsBothActors),
            () => PartialLowerEncounterResetsBothActors(tree))
    ];

    private static async Task UpperAndLowerRealExitRoutesBothReachConvergence(SceneTree tree)
    {
        PrologueLevelController upper = await LoadLevel(tree, "UpperRouteFixture");
        await TouchExit(tree, upper, "Exits/ToCorridor", PrologueRoomId.BrokenToothCorridor);
        await TouchExit(tree, upper, "Exits/ToUpper", PrologueRoomId.UpperPendulum);
        await TouchExit(tree, upper, "Exits/ToMechanism", PrologueRoomId.BellChainMechanism);
        await TouchExit(tree, upper, "Exits/ToLower", PrologueRoomId.LowerBuriedMachinery);
        await TouchExit(tree, upper, "Exits/ToCorridor", PrologueRoomId.BrokenToothCorridor);
        await Dispose(tree, upper);

        PrologueLevelController lower = await LoadLevel(tree, "LowerRouteFixture");
        await TouchExit(tree, lower, "Exits/ToCorridor", PrologueRoomId.BrokenToothCorridor);
        await TouchExit(tree, lower, "Exits/ToLower", PrologueRoomId.LowerBuriedMachinery);
        await TouchExit(tree, lower, "Exits/ToMechanism", PrologueRoomId.BellChainMechanism);
        await TouchExit(tree, lower, "Exits/ToUpper", PrologueRoomId.UpperPendulum);
        await TouchExit(tree, lower, "Exits/ToCorridor", PrologueRoomId.BrokenToothCorridor);
        await Dispose(tree, lower);
    }

    private static async Task HiddenDiscoveryPersistsAcrossDeathRecovery(SceneTree tree)
    {
        PrologueLevelController level = await LoadLevel(tree, "HiddenPersistenceFixture");
        Exception primaryFailure = null;
        try
        {
        Label objective = level.GetNode<Label>("Hud/FormalHud/ObjectiveRibbon/Text");
        string objectiveBeforeDiscovery = objective.Text;
        typeof(PrologueLevelController).GetMethod("OnHiddenAlcoveDiscovered",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(level,
                [new StringName("upper_hidden_alcove")]);
        TestAssert.True(level.Progress.HiddenAlcoveVisited,
            "hidden discovery event records durable progress independently of room mapping");
        TestAssert.Equal(objectiveBeforeDiscovery, objective.Text,
            "hidden discovery feedback does not replace the active route objective");

        int requested = 0;
        int completed = 0;
        PlayerInputReader input = level.Player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = level.Player.GetNode<PlayerMotor>("Motor");
        HealthComponent health = level.Player.GetNode<HealthComponent>("Health");
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        ulong originalLevelId = level.GetInstanceId();
        ulong originalPlayerId = level.Player.GetInstanceId();
        const string recoveryReason = "HIDDEN DISCOVERY RECOVERY";
        string requestedReason = null;
        string completedReason = null;
        bool completionKeptDiscovery = false;
        bool completionKeptProcessingDisabled = false;
        ulong completionLevelId = 0;
        ulong completionPlayerId = 0;
        var eventOrder = new List<string>(2);
        PrologueLevelController.RecoveryRequestedEventHandler requestObserver = reason =>
        {
            requested++;
            requestedReason = reason;
            eventOrder.Add("requested");
        };
        PrologueLevelController.RecoveryCompletedEventHandler completionObserver = reason =>
        {
            completed++;
            completedReason = reason;
            eventOrder.Add("completed");
            completionKeptDiscovery = level.Progress.HiddenAlcoveVisited;
            completionLevelId = level.GetInstanceId();
            completionPlayerId = level.Player.GetInstanceId();
            completionKeptProcessingDisabled = !level.Player.IsPhysicsProcessing() &&
                !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing();
        };
        level.RecoveryRequested += requestObserver;
        level.RecoveryCompleted += completionObserver;
        var recoveryTrace = new List<string>(8);
        bool traceOverflow = false;
        int completionWaitFrames = 0;
        int requestedOnReturn = -1;
        int completedOnReturn = -1;
        void TraceRecovery(string stage, string reason = "")
        {
            if (recoveryTrace.Count >= 8) { traceOverflow = true; return; }
            recoveryTrace.Add($"P1_HIDDEN_RECOVERY_TRACE stage={stage} reason={reason} " +
                $"physics={Engine.GetPhysicsFrames()} level={level.GetInstanceId()} player={level.Player.GetInstanceId()} " +
                $"requested={requested} completed={completed} hidden={level.Progress.HiddenAlcoveVisited} " +
                $"objective_kept={objective.Text == objectiveBeforeDiscovery} hp={health.CurrentHealth} " +
                $"position={level.Player.GlobalPosition} velocity={level.Player.Velocity} flow={flow.State} " +
                $"player_processing={level.Player.IsPhysicsProcessing()} input_processing={input.IsPhysicsProcessing()} " +
                $"motor_processing={motor.IsPhysicsProcessing()} sequence={input.CurrentFrameSequence} " +
                $"move={input.Current.MoveAxis} primary={input.Current.PrimaryHeld} shield={input.Current.ShieldHeld} " +
                $"raw_move={input.RawFrame.MoveAxis} raw_primary={input.RawFrame.PrimaryHeld} raw_shield={input.RawFrame.ShieldHeld} " +
                $"map_right={Input.IsActionPressed(InputActions.MoveRight)} map_primary={Input.IsActionPressed(InputActions.PrimaryAction)} " +
                $"map_shield={Input.IsActionPressed(InputActions.Shield)}");
        }
        // Installed after the existing counting/Ready observers; not signal-entry snapshots.
        PrologueLevelController.RecoveryRequestedEventHandler requestTrace =
            reason => TraceRecovery("request_callback", reason);
        PrologueLevelController.RecoveryCompletedEventHandler completionTrace =
            reason => TraceRecovery("completion_callback", reason);
        level.RecoveryRequested += requestTrace;
        level.RecoveryCompleted += completionTrace;
        try
        {
        TraceRecovery("before_request");
        level.RequestRespawn(recoveryReason);
        requestedOnReturn = requested;
        completedOnReturn = completed;
        TraceRecovery("after_request_return");
        TestAssert.True(requested == 1 && completed == 0 && requestedReason == recoveryReason &&
            !level.Player.IsPhysicsProcessing() && !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing(),
            "public recovery accepts one request and stops gameplay before deferred completion");
        // The original zero-wait premise was invalid. Adopt the existing generated-main
        // completion-only cap; do not borrow its actor-reset or resume wait budgets.
        while (completed == 0 && completionWaitFrames < 4)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            completionWaitFrames++;
        }
        TraceRecovery("after_completion_observation");
        TestAssert.True(requested == 1 && completed == 1 && completedReason == recoveryReason &&
            completionKeptDiscovery && completionKeptProcessingDisabled &&
            completionLevelId == originalLevelId && completionPlayerId == originalPlayerId &&
            level.GetInstanceId() == originalLevelId && level.Player.GetInstanceId() == originalPlayerId &&
            string.Join("|", eventOrder) == "requested|completed" && !traceOverflow,
            $"one deferred completion preserves discovery and persistent identities; " +
            $"wait_frames={completionWaitFrames} requested={requested} completed={completed} " +
            $"hidden_at_completion={completionKeptDiscovery} stopped_at_completion={completionKeptProcessingDisabled}");
        }
        finally
        {
            level.RecoveryRequested -= requestTrace;
            level.RecoveryCompleted -= completionTrace;
            level.RecoveryRequested -= requestObserver;
            level.RecoveryCompleted -= completionObserver;
            foreach (string row in recoveryTrace) GD.Print(row);
            GD.Print($"P1_HIDDEN_RECOVERY_ASSERT_PARTS requested={requested} completed={completed} " +
                $"immediate_requested={requestedOnReturn} immediate_completed={completedOnReturn} " +
                $"completion_wait={completionWaitFrames} completion_cap=4 " +
                $"completion_hidden={completionKeptDiscovery} completion_stopped={completionKeptProcessingDisabled} " +
                $"completion_level={completionLevelId} completion_player={completionPlayerId} " +
                $"hidden={level.Progress.HiddenAlcoveVisited} flow={flow.State} hp={health.CurrentHealth} " +
                $"player_processing={level.Player.IsPhysicsProcessing()} trace_rows={recoveryTrace.Count} overflow={traceOverflow}");
        }
        TestAssert.True(level.Progress.HiddenAlcoveVisited,
            "hidden discovery persists when death restores the active checkpoint");
        GD.Print($"P1_HIDDEN_DEFERRED_COMPLETION_OK requested={requested} completed={completed} " +
            $"completion_wait={completionWaitFrames} completion_cap=4 " +
            "original_setup_physics=2 original_dispose_process=1 " +
            "completion_snapshot=true discovery_preserved=true same_level_player=true public_request=true health_death=false");
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree()) await Dispose(tree, level);
                    else level.Free();
                }
                GD.Print($"P1_HIDDEN_RECOVERY_FIXTURE_CLEANUP_OK primary_failure={primaryFailure != null} original_queue_free_and_process_tail=true");
            }
            catch (Exception cleanupFailure)
            {
                if (primaryFailure != null)
                    throw new AggregateException("P1 recovery assertion and owned cleanup both failed",
                        primaryFailure, cleanupFailure);
                throw;
            }
        }
    }

    private static async Task PartialLowerEncounterResetsBothActors(SceneTree tree)
    {
        PrologueRoom room = GD.Load<PackedScene>("res://scenes/prologue/rooms/LowerBuriedMachinery.tscn")
            .Instantiate<PrologueRoom>();
        room.Name = "PartialEncounterFixture";
        tree.Root.AddChild(room);
        await Frames(tree, 2);
        EncounterController controller = room.GetNode<EncounterController>("EncounterController");
        Node actors = room.GetNode("Actors");
        TestAssert.True(controller.IsActive && actors.GetChildCount() == 2,
            "formal lower encounter activates both real enemy scenes");
        IReadOnlyList<ulong> before = Ids(actors);
        actors.GetNode("ground").GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(999, Vector2.Right, 0f, 8301UL));
        TestAssert.True(!controller.IsCompleted, "one lower-route kill leaves the mixed encounter incomplete");

        room.ResetRoomState();
        TestAssert.Equal(2, actors.GetChildCount(), "checkpoint reset recreates the full lower encounter");
        IReadOnlyList<ulong> after = Ids(actors);
        TestAssert.True(before[0] != after[0] && before[1] != after[1],
            "partial encounter reset replaces both actors, including the survivor");
        TestAssert.Equal(actors.GetNode("ground").GetNode<HealthComponent>("Health").MaxHealth,
            actors.GetNode("ground").GetNode<HealthComponent>("Health").CurrentHealth,
            "reset ground actor is full health");
        TestAssert.Equal(actors.GetNode("air").GetNode<HealthComponent>("Health").MaxHealth,
            actors.GetNode("air").GetNode<HealthComponent>("Health").CurrentHealth,
            "reset aerial actor is full health");
        await Dispose(tree, room);
    }

    private static async Task<PrologueLevelController> LoadLevel(SceneTree tree, string name)
    {
        PrologueLevelController level = CreateLevelFixture(name);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        return level;
    }

    internal static PrologueLevelController CreateLevelFixture(string name)
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        IsolatedPrologueFixture.Configure(level, "p1-" + name);
        return level;
    }

    private static async Task TouchExit(SceneTree tree, PrologueLevelController level,
        string exitPath, PrologueRoomId expected)
    {
        RoomExit exit = level.CurrentRoom.GetNode<RoomExit>(exitPath);
        level.Player.GlobalPosition = exit.GlobalPosition;
        await Frames(tree, 5);
        TestAssert.Equal(expected, level.CurrentRoomId,
            $"real RoomExit {exit.ExitId} reaches its declared destination");
    }

    private static IReadOnlyList<ulong> Ids(Node actors) =>
        new[] { actors.GetNode("ground").GetInstanceId(), actors.GetNode("air").GetInstanceId() };

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task Dispose(SceneTree tree, Node node)
    {
        node.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
