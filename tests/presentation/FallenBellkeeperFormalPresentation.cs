using System;
using Godot;
using Knight.Boss;

namespace Knight.Tests.Presentation;

public partial class FallenBellkeeperFormalPresentation : SceneTree
{
    private int _frame;

    public override void _Initialize()
    {
        try
        {
            Root.AddChild(new ColorRect
            {
                Size = new Vector2(1280, 720),
                Color = new Color("0a0e12"),
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            AddLabel("FALLEN BELLKEEPER · FORMAL STATE READABILITY", new Vector2(80, 24),
                new Vector2(1120, 42), 25, new Color("d6b56b"));

            AddBoss(new Vector2(205, 230), FallenBellkeeperState.Dormant, "DORMANT");
            AddBoss(new Vector2(510, 230), FallenBellkeeperState.PhaseOne, "OPEN PHASE");
            AddBoss(new Vector2(815, 230), FallenBellkeeperState.HammerTelegraph, "TELEGRAPH");
            AddBoss(new Vector2(1120, 230), FallenBellkeeperState.HammerThrown, "HAMMER THROWN");
            AddBoss(new Vector2(360, 530), FallenBellkeeperState.PhaseTwoArmored, "ARMOR CLOSED");
            AddBoss(new Vector2(670, 530), FallenBellkeeperState.Exposed, "EXPOSED");
            AddBoss(new Vector2(980, 530), FallenBellkeeperState.Defeated, "DEFEATED");
        }
        catch (Exception exception)
        {
            GD.PushError("FALLEN_BELLKEEPER_FORMAL_PRESENTATION_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        if (++_frame != 120) return false;
        GD.Print("FALLEN_BELLKEEPER_FORMAL_PRESENTATION_COMPLETE frame=120");
        Quit(0);
        return false;
    }

    private void AddBoss(Vector2 position, FallenBellkeeperState state, string label)
    {
        FallenBellkeeperFormalVisual visual = GD.Load<PackedScene>(
            "res://scenes/presentation/FallenBellkeeperFormalVisual.tscn")
            .Instantiate<FallenBellkeeperFormalVisual>();
        visual.Position = position;
        visual.Scale = new Vector2(0.72f, 0.72f);
        Root.AddChild(visual);
        visual.ApplyBossFrame(new FallenBellkeeperFrame(state, FallenBellkeeperAttack.None, false,
            state == FallenBellkeeperState.PhaseTwoArmored,
            state == FallenBellkeeperState.HammerThrown, true), 1);
        AddLabel(label, position + new Vector2(-130, 112), new Vector2(260, 28), 15, new Color("c9c3b2"));
    }

    private void AddLabel(string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            Position = position,
            Size = size,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        Root.AddChild(label);
    }
}
