using System;
using Godot;
using Knight.Presentation;

namespace Knight.Tests.Presentation;

public partial class PlayerFormalPresentation : SceneTree
{
    private const int FinalFrame = 120;
    private int _frame;

    public override void _Initialize()
    {
        try
        {
            Root.AddChild(new ColorRect
            {
                Name = "Backdrop",
                Position = Vector2.Zero,
                Size = new Vector2(1280, 720),
                Color = new Color("0b1013"),
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            AddTitle();
            AddPose("IDLE", new Vector2(155, 205), new(EntityVisualState.Idle, 1, true));
            AddPose("MOVE", new Vector2(465, 205), new(EntityVisualState.Move, 1, true));
            AddPose("RISE", new Vector2(775, 205), new(EntityVisualState.Rise, 1, false));
            AddPose("FALL", new Vector2(1085, 205), new(EntityVisualState.Fall, 1, false));
            AddPose("GROUND ATTACK", new Vector2(155, 500), new(EntityVisualState.Attack, 1, true));
            AddPose("THREAD CAST", new Vector2(465, 500), new(EntityVisualState.Idle, 1, true), PresentationCue.ThreadFired);
            AddPose("HURT", new Vector2(775, 500), new(EntityVisualState.Hurt, 1, true), PresentationCue.Hurt);
            AddPose("DEAD", new Vector2(1085, 500), new(EntityVisualState.Dead, 1, false), PresentationCue.Died);
        }
        catch (Exception exception)
        {
            GD.PushError("PLAYER_FORMAL_PRESENTATION_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        _frame++;
        if (_frame != FinalFrame) return false;
        GD.Print("PLAYER_FORMAL_PRESENTATION_COMPLETE frame=120");
        Quit(0);
        return false;
    }

    private void AddTitle()
    {
        var title = new Label
        {
            Name = "Title",
            Position = new Vector2(64, 38),
            Size = new Vector2(1152, 64),
            Text = "THREAD-SPOOL BEARER · FORMAL GAMEPLAY RIG",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color("d6b56b")
        };
        title.AddThemeFontSizeOverride("font_size", 26);
        Root.AddChild(title);
    }

    private void AddPose(string labelText, Vector2 position, EntityPresentationFrame frame,
        PresentationCue? cue = null)
    {
        PlayerFormalVisual visual = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn")
            .Instantiate<PlayerFormalVisual>();
        visual.Name = labelText.Replace(" ", string.Empty) + "Rig";
        visual.Position = position;
        Root.AddChild(visual);
        visual.ApplyFrame(frame);
        if (cue.HasValue) visual.PlayCue(cue.Value);
        var label = new Label
        {
            Name = visual.Name + "Label",
            Position = position + new Vector2(-120, 82),
            Size = new Vector2(240, 32),
            Text = labelText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color("c9c3b2")
        };
        label.AddThemeFontSizeOverride("font_size", 16);
        Root.AddChild(label);
    }
}
