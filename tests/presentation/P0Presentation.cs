using System;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;

namespace Knight.Tests.Presentation;

public partial class P0Presentation : SceneTree
{
    private const int FinalFrame = 600;
    private PrologueLevelController _level;
    private CharacterBody2D _player;
    private Camera2D _proofCamera;
    private int _frame;

    public override void _Initialize()
    {
        try
        {
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
            Root.AddChild(_level);
            _player = _level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _player.GetNode<Camera2D>("Camera2D").Enabled = false;
            _proofCamera = new Camera2D { Name = "ProofCamera", Position = new Vector2(200, 80), Enabled = true };
            _level.AddChild(_proofCamera);
            GD.Print("P0_PRESENTATION_STAGE=courtyard");
        }
        catch (Exception exception)
        {
            GD.PushError("P0_PRESENTATION_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        _frame++;
        try
        {
            Drive(_frame);
            if (_frame != FinalFrame) return false;
            ReleaseAll();
            GD.Print("P0_PRESENTATION_COMPLETE frame=600");
            Quit(0);
            return false;
        }
        catch (Exception exception)
        {
            ReleaseAll();
            GD.PushError($"P0_PRESENTATION_FAIL frame={_frame}: {exception}");
            Quit(1);
            return false;
        }
    }

    private void Drive(int frame)
    {
        if (frame == 1) Press(InputActions.MoveRight);
        if (frame == 55) Press(InputActions.Jump);
        if (frame == 68) Release(InputActions.Jump);
        if (frame == 95) { Press(InputActions.Attack); GD.Print("P0_PRESENTATION_STAGE=attack_teaching"); }
        if (frame == 98) Release(InputActions.Attack);
        if (frame == 150) Release(InputActions.MoveRight);

        if (frame == 175)
        {
            GD.Print("P0_PRESENTATION_STAGE=corridor_transition");
            Fixture(new Vector2(1040, 190));
        }
        if (frame == 205) _proofCamera.Position = new Vector2(250, 80);
        if (frame == 230)
        {
            GD.Print("P0_PRESENTATION_STAGE=rust_shell");
            Fixture(new Vector2(-135, 198));
            Press(InputActions.MoveRight);
            Press(InputActions.Attack);
        }
        if (frame == 233) { Release(InputActions.Attack); Release(InputActions.MoveRight); }
        if (frame == 300)
        {
            GD.Print("P0_PRESENTATION_STAGE=checkpoint");
            _proofCamera.Position = new Vector2(700, 80);
            Fixture(new Vector2(960, 198));
        }
        if (frame == 365)
        {
            GD.Print("P0_PRESENTATION_STAGE=thread_anchor");
            _proofCamera.Position = new Vector2(300, 80);
            Fixture(new Vector2(210, 68));
            Press(InputActions.MoveRight);
            Press(InputActions.MoveUp);
            Press(InputActions.Thread);
        }
        if (frame == 369) { Release(InputActions.Thread); Release(InputActions.MoveUp); }
        if (frame == 455) Release(InputActions.MoveRight);
        if (frame == 500)
        {
            GD.Print("P0_PRESENTATION_STAGE=p0_complete");
            _proofCamera.Position = new Vector2(1200, 80);
            Fixture(new Vector2(1570, 190));
        }
    }

    private void Fixture(Vector2 position)
    {
        _player = _level.Player;
        _player.GlobalPosition = position;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        _player.GetNodeOrNull<ThreadAbility>("ThreadAbility")?.ResetTransientState();
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
