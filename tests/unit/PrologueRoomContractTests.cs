using System;
using System.Collections.Generic;
using Godot;
using Knight.Config;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Tests.Unit;

public static class PrologueRoomContractTests
{
    public static void Run()
    {
        ValidSyntheticRoomPassesContract();
        MissingStructuralNodesAreNamed();
        RequestedEntranceMustExist();
        DuplicateExitIdsAreRejected();
        CameraBoundsMustBePositive();
        RoomPresentationProfileFallsBackSafely();
        HudMethodsOnlyUpdateControls();
    }

    private static void ValidSyntheticRoomPassesContract()
    {
        PrologueRoom room = SyntheticRoom();
        TestAssert.Equal(0, room.ValidateContract("start").Count,
            "synthetic room with entrances, exits, KillZone and positive bounds is valid");
        room.Free();
    }

    private static void MissingStructuralNodesAreNamed()
    {
        PrologueRoom room = SyntheticRoom();
        room.GetNode("Entrances").Free();
        room.GetNode("KillZone").Free();
        IReadOnlyList<string> errors = room.ValidateContract("start");
        TestAssert.True(Contains(errors, "Entrances"), "missing entrance root is named");
        TestAssert.True(Contains(errors, "KillZone"), "missing KillZone is named");
        room.Free();
    }

    private static void RequestedEntranceMustExist()
    {
        PrologueRoom room = SyntheticRoom();
        TestAssert.True(Contains(room.ValidateContract("from_missing"), "from_missing"),
            "requested entrance marker must exist under Entrances");
        room.Free();
    }

    private static void DuplicateExitIdsAreRejected()
    {
        PrologueRoom room = SyntheticRoom();
        room.GetNode("Exits").AddChild(new RoomExit { Name = "DuplicateEast", ExitId = "east" });
        TestAssert.True(Contains(room.ValidateContract("start"), "east"),
            "duplicate authored exit IDs are rejected");
        room.Free();
    }

    private static void CameraBoundsMustBePositive()
    {
        PrologueRoom room = SyntheticRoom();
        room.CameraBounds = new Rect2(0, 0, 1280, 0);
        TestAssert.True(Contains(room.ValidateContract("start"), "CameraBounds"),
            "non-positive camera bounds are rejected");
        room.Free();
    }

    private static void RoomPresentationProfileFallsBackSafely()
    {
        bool previous = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        var root = new Node2D { Name = "VisualRoot" };
        var fallback = new Node2D { Name = "Placeholder" };
        root.AddChild(fallback);
        var missing = new RoomPresentationProfile();
        Node2D missingResolved = missing.Resolve(root);
        TestAssert.True(ReferenceEquals(fallback, missingResolved) && fallback.Visible,
            "missing room art uses visible placeholder");

        var invalid = new RoomPresentationProfile { VisualScene = Pack(new Node { Name = "Invalid" }) };
        Node2D invalidResolved = invalid.Resolve(root);
        TestAssert.True(ReferenceEquals(fallback, invalidResolved) && fallback.Visible,
            "malformed room art uses visible placeholder");

        var valid = new RoomPresentationProfile { VisualScene = Pack(new Node2D { Name = "Authored" }) };
        Node2D validResolved = valid.Resolve(root);
        TestAssert.Equal(new StringName("Override"), validResolved.Name, "valid art mounts at stable Override path");
        TestAssert.True(!fallback.Visible, "valid room art hides placeholder");
        root.Free();
        PrologueProductionMode.AllowDiagnosticFallbacks = previous;
    }

    private static void HudMethodsOnlyUpdateControls()
    {
        var hud = new PrologueHud { Name = "Hud" };
        var health = new Label { Name = "Health" };
        var thread = new Label { Name = "Thread" };
        var objective = new Label { Name = "Objective" };
        var prompt = new Label { Name = "Prompt" };
        var boss = new Control { Name = "Boss", Position = new Vector2(390, 150) };
        boss.AddChild(new Label { Name = "Name" });
        boss.AddChild(new ProgressBar { Name = "Health" });
        boss.AddChild(new Label { Name = "State" });
        hud.AddChild(health);
        hud.AddChild(thread);
        hud.AddChild(objective);
        hud.AddChild(prompt);
        hud.AddChild(boss);
        hud.BindControls();

        hud.SetHealth(3, 5);
        hud.SetThreadReady(false);
        hud.SetObjective("Reach the bell");
        hud.ShowPrompt("Jump");
        hud.ShowBoss("Fallen Bellkeeper", 6, 10, 2, false);
        TestAssert.Equal("HEALTH 3/5", health.Text, "HUD health text is controlled by SetHealth");
        TestAssert.Equal("THREAD COOLING", thread.Text, "HUD thread text is controlled by SetThreadReady");
        TestAssert.Equal("Reach the bell", objective.Text, "HUD objective text is controlled by SetObjective");
        TestAssert.True(prompt.Visible && prompt.Text == "Jump", "HUD prompt is visible with requested text");
        TestAssert.True(boss.Visible, "Boss panel shows on request");
        TestAssert.True(boss.Position.Y >= 150f,
            "Boss panel leaves visible separation below the persistent HUD");
        TestAssert.Equal("PHASE 2 / ARMORED", boss.GetNode<Label>("State").Text,
            "phase-two closed armor is labeled armored");
        hud.ShowBoss("Fallen Bellkeeper", 10, 10, 1, false);
        TestAssert.Equal("PHASE 1 / OPEN", boss.GetNode<Label>("State").Text,
            "phase one is labeled open rather than armored");
        hud.HideBoss();
        TestAssert.True(!boss.Visible, "Boss panel hides on request");
        hud.Free();
    }

    internal static PrologueRoom SyntheticRoom()
    {
        var room = new PrologueRoom
        {
            Name = "SyntheticRoom",
            RoomId = PrologueRoomId.AwakeningCourtyard,
            CameraBounds = new Rect2(-640, -360, 1280, 720)
        };
        var entrances = new Node2D { Name = "Entrances" };
        entrances.AddChild(new Marker2D { Name = "start", Position = new Vector2(40, 40) });
        room.AddChild(entrances);
        var exits = new Node2D { Name = "Exits" };
        exits.AddChild(new RoomExit { Name = "East", ExitId = "east" });
        room.AddChild(exits);
        room.AddChild(new Area2D { Name = "KillZone" });
        var presentation = new Node2D { Name = "Presentation" };
        var visualRoot = new Node2D { Name = "VisualRoot" };
        visualRoot.AddChild(new Node2D { Name = "Placeholder" });
        presentation.AddChild(visualRoot);
        room.AddChild(presentation);
        return room;
    }

    private static PackedScene Pack(Node root)
    {
        var scene = new PackedScene();
        TestAssert.Equal(Error.Ok, scene.Pack(root), "test presentation scene packs");
        root.Free();
        return scene;
    }

    private static bool Contains(IReadOnlyList<string> errors, string value)
    {
        foreach (string error in errors)
            if (error.Contains(value, StringComparison.Ordinal)) return true;
        return false;
    }
}
