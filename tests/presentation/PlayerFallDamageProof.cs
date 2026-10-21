using System;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// Isolated running-player proof, not the final game HUD or a full-level playthrough.
public partial class PlayerFallDamageProof : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            Root.AddChild(new FallDamageProofHud());
            await PlayerFallDamageRuntimeTests.RunPresentation(this);
            GD.Print("FALL_DAMAGE_PROOF_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}

public partial class FallDamageProofHud : CanvasLayer
{
    private Label _status;
    public override void _Ready()
    {
        Layer = 20;
        AddChild(new ColorRect
        {
            Position = new Vector2(20, 20), Size = new Vector2(880, 115),
            Color = new Color("14232d", .94f), MouseFilter = Control.MouseFilterEnum.Ignore
        });
        _status = new Label { Position = new Vector2(36, 30) };
        _status.AddThemeFontSizeOverride("font_size", 24);
        AddChild(_status);
    }
    public override void _Process(double delta)
    {
        CharacterBody2D player = GetTree().Root.FindChild("Player", true, false) as CharacterBody2D;
        if (player == null) return;
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        _status.Text = "FALL DAMAGE v002 | ISOLATED RUNNING-PLAYER PROOF\n" +
            $"Health {health.CurrentHealth}/5   |   Downward speed {player.Velocity.Y:F0}\n" +
            $"Last landing: {motor.LastFallHeight:F0} units   |   Damage {motor.LastFallDamage}";
    }
}
