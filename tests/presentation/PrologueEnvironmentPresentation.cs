using System;
using Godot;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Tests.Presentation;

public partial class PrologueEnvironmentPresentation : SceneTree
{
    private int _frame;

    public override void _Initialize()
    {
        try
        {
            Root.AddChild(new ColorRect
            {
                Size = new Vector2(1280, 720), Color = new Color("070b0f"),
                MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = -100
            });
            AddLabel("NAMELESS BELFRY · SIX FORMAL ROOMS", new Vector2(70, 18),
                new Vector2(1140, 38), 24, new Color("d6b56b"));
            AddRoom("AwakeningCourtyard", "AWAKENING COURTYARD", new Vector2(220, 195));
            AddRoom("BrokenToothCorridor", "BROKEN-TOOTH CORRIDOR", new Vector2(640, 195));
            AddRoom("UpperPendulum", "UPPER PENDULUM", new Vector2(1060, 195));
            AddRoom("LowerBuriedMachinery", "BURIED MACHINERY", new Vector2(220, 515));
            AddRoom("BellChainMechanism", "BELL-CHAIN CHAMBER", new Vector2(640, 515));
            AddRoom("FallenBellkeeperArena", "FALLEN BELLKEEPER ARENA", new Vector2(1060, 515));
        }
        catch (Exception exception)
        {
            GD.PushError("PROLOGUE_ENVIRONMENT_PRESENTATION_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        if (++_frame != 120) return false;
        GD.Print("PROLOGUE_ENVIRONMENT_PRESENTATION_COMPLETE frame=120");
        Quit(0);
        return false;
    }

    private void AddRoom(string sceneName, string label, Vector2 center)
    {
        PrologueRoom room = GD.Load<PackedScene>($"res://scenes/prologue/rooms/{sceneName}.tscn")
            .Instantiate<PrologueRoom>();
        RoomFormalVisual art = room.GetNode<RoomFormalVisual>("Art");
        room.RemoveChild(art);
        float scale = sceneName == "UpperPendulum" ? 0.2f : 0.22f;
        art.Scale = Vector2.One * scale;
        art.Position = center - room.CameraBounds.GetCenter() * scale;
        Root.AddChild(art);
        room.Free();
        AddLabel(label, center + new Vector2(-190, 124), new Vector2(380, 24), 13, new Color("c9c3b2"));
    }

    private void AddLabel(string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text, Position = position, Size = size,
            HorizontalAlignment = HorizontalAlignment.Center, Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        Root.AddChild(label);
    }
}
