using System;
using Godot;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Presentation;

namespace Knight.Tests.Presentation;

public partial class EnemyFormalPresentation : SceneTree
{
    private int _frame;

    public override void _Initialize()
    {
        try
        {
            Root.AddChild(new ColorRect
            {
                Name = "Backdrop", Size = new Vector2(1280, 720), Color = new Color("0b1013"),
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            AddLabel("FORMAL ENEMY SILHOUETTES · STATE READABILITY", new Vector2(100, 28),
                new Vector2(1080, 44), 25, new Color("d6b56b"));

            AddRust(new Vector2(170, 180), RustShellState.Idle, "RUST · IDLE");
            AddRust(new Vector2(475, 180), RustShellState.Telegraph, "TELEGRAPH");
            AddRust(new Vector2(780, 180), RustShellState.Attack, "ATTACK");
            AddRust(new Vector2(1085, 180), RustShellState.Stunned, "STUNNED");

            AddMoth(new Vector2(170, 375), GraveLampMothState.Hover, "MOTH · HOVER");
            AddMoth(new Vector2(475, 375), GraveLampMothState.Telegraph, "TELEGRAPH");
            AddMoth(new Vector2(780, 375), GraveLampMothState.Dive, "DIVE");
            AddMoth(new Vector2(1085, 375), GraveLampMothState.ThreadStunned, "THREAD PULL");

            AddLaborer(new Vector2(170, 580), BellBackLaborerState.Guard, "LABORER · GUARD");
            AddLaborer(new Vector2(475, 580), BellBackLaborerState.Telegraph, "TELEGRAPH");
            AddLaborer(new Vector2(780, 580), BellBackLaborerState.Attack, "ATTACK");
            AddLaborer(new Vector2(1085, 580), BellBackLaborerState.Exposed, "EXPOSED");
        }
        catch (Exception exception)
        {
            GD.PushError("ENEMY_FORMAL_PRESENTATION_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        if (++_frame != 120) return false;
        GD.Print("ENEMY_FORMAL_PRESENTATION_COMPLETE frame=120");
        Quit(0);
        return false;
    }

    private void AddRust(Vector2 position, RustShellState state, string label)
    {
        RustShellFormalVisual visual = Load<RustShellFormalVisual>("RustShellFormalVisual", position);
        visual.ApplyRustShellFrame(new RustShellFrame(state, 0f, state == RustShellState.Attack, true), 1);
        PoseLabel(label, position);
    }

    private void AddMoth(Vector2 position, GraveLampMothState state, string label)
    {
        GraveLampMothFormalVisual visual = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual", position);
        visual.ApplyMothFrame(new GraveLampMothFrame(state, 0f, 0f, state == GraveLampMothState.Dive, true), 1, false);
        PoseLabel(label, position);
    }

    private void AddLaborer(Vector2 position, BellBackLaborerState state, string label)
    {
        BellBackLaborerFormalVisual visual = Load<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual", position);
        visual.ApplyLaborerFrame(new BellBackLaborerFrame(state, 0f, state == BellBackLaborerState.Attack,
            state is BellBackLaborerState.Guard or BellBackLaborerState.Telegraph, true), 1);
        PoseLabel(label, position);
    }

    private T Load<T>(string sceneName, Vector2 position) where T : Node2D
    {
        T visual = GD.Load<PackedScene>($"res://scenes/presentation/{sceneName}.tscn").Instantiate<T>();
        visual.Position = position;
        Root.AddChild(visual);
        return visual;
    }

    private void PoseLabel(string text, Vector2 position) =>
        AddLabel(text, position + new Vector2(-125, 76), new Vector2(250, 28), 15, new Color("c9c3b2"));

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
