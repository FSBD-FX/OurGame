using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// An isolated real-input fixture, not a full-level playthrough or the final product HUD.
public partial class KeyboardControlsProof : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            RenderingServer.SetDefaultClearColor(new Color("101c29"));
            Root.AddChild(new KeyboardControlsProofHud());
            await KeyboardClawRuntimeTests.Run(this, presentation: true);
            for(int i=0;i<30;i++) await ToSignal(this,SignalName.ProcessFrame);
            GD.Print("KEYBOARD_CONTROLS_PROOF_OK"); Quit(0);
        }
        catch(Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}

public partial class KeyboardControlsProofHud : CanvasLayer
{
    private Label _label;
    public override void _Ready()
    {
        Layer = 20;
        AddChild(new ColorRect { Position = new Vector2(20,15), Size = new Vector2(930,112),
            Color = new Color("101c29",.95f), MouseFilter = Control.MouseFilterEnum.Ignore });
        _label = new Label { Position = new Vector2(32,22) };
        _label.AddThemeFontSizeOverride("font_size",20); AddChild(_label);
    }
    public override void _Process(double delta)
    {
        var player = GetTree().Root.FindChild("Player",true,false) as CharacterBody2D;
        if(player == null) return;
        var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
        var motor=player.GetNode<PlayerMotor>("Motor");
        _label.Text="KEYBOARD v001 | ISOLATED REAL-INPUT PROOF - NOT FINAL LEVEL\n"+
            "WASD aim | K cast / cancel | SPACE boost | J attack | L hold guard\n"+
            $"Claw: {claw.CurrentFrame.State} | Jumps: {motor.AcceptedJumpCount} | "+
            $"HP {player.GetNode<HealthComponent>("Health").CurrentHealth}/5";
    }
}
