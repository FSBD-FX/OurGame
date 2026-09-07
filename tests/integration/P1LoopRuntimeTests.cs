using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;

namespace Knight.Tests.Integration;

public static class P1LoopRuntimeTests
{
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
        level.RecoveryRequested += _ => requested++;
        level.RecoveryCompleted += _ => completed++;
        level.RequestRespawn("HIDDEN DISCOVERY RECOVERY");
        TestAssert.True(requested == 1 && completed == 1,
            "Task 9 recovery request and completion both occur in the same physics frame");
        TestAssert.True(level.Progress.HiddenAlcoveVisited,
            "hidden discovery persists when death restores the active checkpoint");
        await Dispose(tree, level);
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
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 2);
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
