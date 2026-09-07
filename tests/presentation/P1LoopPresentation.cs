using System;
using Godot;
using Knight.Abilities;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;

namespace Knight.Tests.Presentation;

public partial class P1LoopPresentation : SceneTree
{
    private const int FinalFrame = 900;
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
            _proofCamera = new Camera2D
            {
                Name = "P1ProofCamera",
                Position = new Vector2(320, 70),
                Enabled = true
            };
            _level.AddChild(_proofCamera);
            GD.Print("P1_PRESENTATION_STAGE=awakening_route");
        }
        catch (Exception exception)
        {
            GD.PushError("P1_PRESENTATION_INIT_FAIL: " + exception);
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
            GD.Print("P1_PRESENTATION_COMPLETE frame=900");
            Quit(0);
            return false;
        }
        catch (Exception exception)
        {
            ReleaseAll();
            GD.PushError($"P1_PRESENTATION_FAIL frame={_frame}: {exception}");
            Quit(1);
            return false;
        }
    }

    private void Drive(int frame)
    {
        if (frame == 45) TouchExit("Exits/ToCorridor", "corridor");
        if (frame == 90)
        {
            _proofCamera.Position = new Vector2(770, 70);
            TouchExit("Exits/ToUpper", "upper_pendulum");
        }

        if (frame == 125)
        {
            _proofCamera.Position = new Vector2(120, -130);
            Fixture(new Vector2(-160, 105));
            GD.Print("P1_PRESENTATION_STAGE=upper_vertical_thread_route");
        }
        if (frame == 165) AimAndThread(true, true);
        if (frame == 168) ReleaseThreadAim();
        if (frame == 205)
        {
            _proofCamera.Position = new Vector2(770, -170);
            Fixture(new Vector2(880, -75));
            GD.Print("P1_PRESENTATION_STAGE=upper_hidden_alcove");
        }
        if (frame == 255) TouchExit("Exits/ToMechanism", "upper_convergence");
        if (frame == 295) TouchExit("Exits/ToLower", "lower_branch");

        if (frame == 335)
        {
            _proofCamera.Position = new Vector2(300, 80);
            Fixture(new Vector2(30, 198));
            GD.Print("P1_PRESENTATION_STAGE=lower_mixed_encounter");
        }
        if (frame == 375) Press(InputActions.MoveRight);
        if (frame == 395) Press(InputActions.Attack);
        if (frame == 398) Release(InputActions.Attack);
        if (frame == 425) Release(InputActions.MoveRight);
        if (frame == 455)
        {
            _proofCamera.Position = new Vector2(760, 80);
            Fixture(new Vector2(690, 198));
        }
        if (frame == 505) TouchExit("Exits/ToMechanism", "lower_convergence");

        if (frame == 545)
        {
            _proofCamera.Position = new Vector2(400, 70);
            Fixture(new Vector2(275, 198));
            GD.Print("P1_PRESENTATION_STAGE=bell_back_laborer");
        }
        if (frame == 585) AimAndThread(true, false);
        if (frame == 588) ReleaseThreadAim();
        if (frame == 625) Press(InputActions.Attack);
        if (frame == 628) Release(InputActions.Attack);

        if (frame == 675)
        {
            _proofCamera.Position = new Vector2(780, 70);
            Fixture(new Vector2(530, 198));
            _player.GetNodeOrNull<ThreadAbility>("ThreadAbility")?.ResetTransientState();
            GD.Print("P1_PRESENTATION_STAGE=bell_chain_shortcut");
        }
        if (frame == 710) AimAndThread(true, true);
        if (frame == 713) ReleaseThreadAim();
        if (frame == 770)
        {
            Fixture(new Vector2(930, 198));
            Press(InputActions.MoveRight);
        }
        if (frame == 800) Release(InputActions.MoveRight);
        if (frame == 815)
        {
            if (_level.CurrentRoomId != PrologueRoomId.BrokenToothCorridor)
                throw new InvalidOperationException("opened shortcut did not return to the corridor");
            GD.Print("P1_PRESENTATION_TRANSITION=permanent_shortcut_return");
        }
        if (frame == 850)
        {
            _proofCamera.Position = new Vector2(760, 70);
            GD.Print("P1_PRESENTATION_STAGE=corridor_shortcut_restored");
        }
    }

    private void TouchExit(string path, string stage)
    {
        RoomExit exit = _level.CurrentRoom.GetNode<RoomExit>(path);
        Fixture(exit.GlobalPosition);
        GD.Print($"P1_PRESENTATION_TRANSITION={stage}");
    }

    private void Fixture(Vector2 position)
    {
        _player = _level.Player;
        _player.GlobalPosition = position;
        _player.Velocity = Vector2.Zero;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        _player.GetNodeOrNull<ThreadAbility>("ThreadAbility")?.ResetTransientState();
    }

    private static void AimAndThread(bool right, bool up)
    {
        if (right) Press(InputActions.MoveRight);
        if (up) Press(InputActions.MoveUp);
        Press(InputActions.Thread);
    }

    private static void ReleaseThreadAim()
    {
        Release(InputActions.Thread);
        Release(InputActions.MoveRight);
        Release(InputActions.MoveUp);
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
