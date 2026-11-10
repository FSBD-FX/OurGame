using Godot;
using Knight.Boss;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Tests.Integration;

public static class PrologueMechanismVisualTests
{
    public static void Run()
    {
        AssertLeaf("GraveLampVisual", MechanismVisualKind.GraveLamp);
        AssertLeaf("ThreadAnchorVisual", MechanismVisualKind.ThreadAnchor);
        AssertLeaf("BellChainVisual", MechanismVisualKind.BellChain);
        AssertLeaf("ShortcutDoorVisual", MechanismVisualKind.ShortcutDoor);

        PrologueRoom corridor = GD.Load<PackedScene>(
            "res://scenes/prologue/rooms/BrokenToothCorridor.tscn").Instantiate<PrologueRoom>();
        TestAssert.True(corridor.GetNodeOrNull<PrologueMechanismVisual>("Checkpoint/Visual") != null,
            "corridor checkpoint uses the formal Grave Lamp visual");
        TestAssert.True(corridor.GetNodeOrNull<PrologueMechanismVisual>("Geometry/ThreadAnchor/Formal") != null,
            "corridor thread target uses the formal anchor visual");
        TestAssert.Equal(0, corridor.FindChildren("Placeholder", "Polygon2D", true, false).Count,
            "formal corridor mechanisms contain no polygon placeholder named Placeholder");
        corridor.Free();

        BellChainMechanism mechanism = GD.Load<PackedScene>(
            "res://scenes/mechanisms/BellChainMechanism.tscn").Instantiate<BellChainMechanism>();
        PrologueMechanismVisual chain = mechanism.GetNode<PrologueMechanismVisual>("VisualRoot/Formal");
        PrologueMechanismVisual door = mechanism.GetNode<PrologueMechanismVisual>("VisualRoot/Door");
        Node2D target = mechanism.GetNode<Node2D>("ThreadTarget");
        Node2D targetMarker = chain.GetNode<Node2D>("TargetMarker");
        TestAssert.Equal(target.GlobalPosition, targetMarker.GlobalPosition,
            "Bell Chain formal marker and mechanical-thread endpoint are the same world point");
        var progress = new PrologueProgress();
        mechanism.ApplyProgress(progress);
        TestAssert.Equal(MechanismVisualState.Idle, chain.CurrentState,
            "closed Bell Chain starts in available idle state");
        mechanism.TryReceiveThread(default);
        TestAssert.Equal(MechanismVisualState.Active, chain.CurrentState,
            "thread activation drives the Bell Chain visual active");
        TestAssert.Equal(MechanismVisualState.Active, door.CurrentState,
            "thread activation drives the shortcut door visual active");
        mechanism.ResetForCheckpoint();
        TestAssert.Equal(MechanismVisualState.Restored, chain.CurrentState,
            "persistent shortcut restores an explicit active visual after checkpoint reset");
        mechanism.Free();

        FallenBellkeeper boss = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn")
            .Instantiate<FallenBellkeeper>();
        TestAssert.True(boss.GetNodeOrNull<Node2D>("VisualRoot/Formal/Chain") != null &&
            boss.GetNodeOrNull<Node2D>("ChainTarget") != null,
            "Boss chain art and Boss chain gameplay target remain independent formal nodes");
        boss.Free();

        PrologueRoom arena = GD.Load<PackedScene>(
            "res://scenes/prologue/rooms/FallenBellkeeperArena.tscn").Instantiate<PrologueRoom>();
        TestAssert.True(arena.GetNodeOrNull<PrologueMechanismVisual>("Doors/Entry/Visual") != null &&
            arena.GetNodeOrNull<PrologueMechanismVisual>("Doors/Exit/Visual") != null,
            "Boss arena doors use the formal shortcut-door visual contract");
        arena.Free();
    }

    private static void AssertLeaf(string sceneName, MechanismVisualKind expectedKind)
    {
        string path = $"res://scenes/presentation/{sceneName}.tscn";
        TestAssert.True(ResourceLoader.Exists(path), $"{sceneName} leaf scene exists");
        PrologueMechanismVisual visual = GD.Load<PackedScene>(path).Instantiate<PrologueMechanismVisual>();
        TestAssert.Equal(expectedKind, visual.Kind, $"{sceneName} declares the expected mechanism kind");
        foreach (MechanismVisualState state in System.Enum.GetValues<MechanismVisualState>())
        {
            visual.SetState(state);
            TestAssert.Equal(state, visual.CurrentState, $"{sceneName} presents {state}");
        }
        visual.Free();
    }
}
