using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Shared;
using Knight.World;

namespace Knight.Tests.Presentation;

/// <summary>Fixed-frame visual proof route. It uses the same InputMap actions as a player and always exits at frame 720.</summary>
public partial class PrototypePresentation : SceneTree
{
    private const int FinalFrame = 720;
    private PrototypeRoomController _room = null!;
    private CharacterBody2D _player = null!;
    private RustShellSquire _enemy = null!;
    private int _frame;

    public override void _Initialize()
    {
        try
        {
            _room = GD.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn").Instantiate<PrototypeRoomController>();
            Root.AddChild(_room);
            _player = _room.GetNode<CharacterBody2D>("Player");
            _enemy = _room.GetNode<RustShellSquire>("Actors/RustShellSquire");

            // This independent overview camera is current before the first movie frame.
            _player.GetNode<Camera2D>("Camera2D").Enabled = false;
            var camera = new Camera2D { Name = "PresentationCamera", Position = new Vector2(90f, 70f), Enabled = true };
            _room.AddChild(camera);
            GD.Print("PRESENTATION_STAGE=spawn");
        }
        catch (Exception exception)
        {
            GD.PushError("PRESENTATION_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        _frame++;
        try
        {
            DriveFrame(_frame);
            if (_frame != FinalFrame) return false;
            ReleaseAll();
            GD.Print("PRESENTATION_COMPLETE frame=720");
            Quit(0);
            return false;
        }
        catch (Exception exception)
        {
            ReleaseAll();
            GD.PushError("PRESENTATION_FAIL frame=" + _frame + ": " + exception);
            Quit(1);
            return false;
        }
    }

    private void DriveFrame(int frame)
    {
        // 0-120: spawn, accelerate and variable jump. 121-180: air dash.
        if (frame == 1) Press(InputActions.MoveRight);
        if (frame == 45) { GD.Print("PRESENTATION_STAGE=variable_jump"); Press(InputActions.Jump); }
        if (frame == 60) Release(InputActions.Jump);
        if (frame == 125) { GD.Print("PRESENTATION_STAGE=air_dash"); Press(InputActions.Dash); }
        if (frame == 126) Release(InputActions.Dash);

        if (frame == 50) Release(InputActions.MoveRight);

        // 181-300: a deterministic pre-gap fixture and anchor pull.
        if (frame == 181) { GD.Print("PRESENTATION_STAGE=thread_gap"); Fixture(new Vector2(100f, -80f)); Press(InputActions.MoveRight); Press(InputActions.Thread); }
        if (frame == 184) { Release(InputActions.Thread); Release(InputActions.MoveRight); }

        // 301-420: demonstrate horizontal/up/down combat, including down bounce on the suspended bell.
        if (frame == 301) { GD.Print("PRESENTATION_STAGE=horizontal_attack"); Fixture(new Vector2(465f, 28f)); Press(InputActions.MoveRight); Press(InputActions.Attack); }
        if (frame == 303) { Release(InputActions.Attack); Release(InputActions.MoveRight); }
        if (frame == 340) { GD.Print("PRESENTATION_STAGE=up_attack"); Fixture(new Vector2(450f, -40f)); Press(InputActions.MoveUp); Press(InputActions.Attack); }
        if (frame == 342) { Release(InputActions.MoveUp); Release(InputActions.Attack); }
        if (frame == 380) { GD.Print("PRESENTATION_STAGE=down_bounce"); Fixture(new Vector2(450f, -150f)); Press(InputActions.MoveDown); Press(InputActions.Attack); }
        if (frame == 382) { Release(InputActions.MoveDown); Release(InputActions.Attack); }

        // 421-540: intentional fall invokes the room's local recovery rather than a scene reload.
        if (frame == 430) { GD.Print("PRESENTATION_STAGE=fall_reset"); Fixture(new Vector2(0f, 560f)); }

        // 541-720: resolve/passage of the one enemy and touch the completion marker.
        if (frame == 550) { GD.Print("PRESENTATION_STAGE=enemy_pass"); Fixture(new Vector2(465f, 28f)); Press(InputActions.MoveRight); Press(InputActions.Attack); }
        if (frame == 552) Release(InputActions.Attack);
        if (frame == 620) { GD.Print("PRESENTATION_STAGE=complete_marker"); Fixture(new Vector2(640f, 20f)); }
    }

    private void Fixture(Vector2 position)
    {
        _player.GlobalPosition = position;
        _player.GetNode<PlayerMotor>("Motor").ResetTransientState();
        _player.GetNode<PlayerCombat>("Combat").ResetTransientState();
        _player.GetNode<ThreadAbility>("ThreadAbility").ResetTransientState();
        _enemy.GetNode<HealthComponent>("Health").ResetFull();
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
