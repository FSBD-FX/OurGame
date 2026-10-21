using System;
using Godot;

namespace Knight.Mechanisms;

// A non-harmful local descent brake; visual and physical extents share Bounds.
// No player registration survives a reset, scene stream, or exit.
public partial class CanyonUpdraft : Node2D
{
    private static readonly StringName WindGroup = new("canyon_updraft_fields");
    [Export] public Rect2 Bounds { get; set; } = new(0,0,534,2120);
    [Export] public float DescentSpeed { get; set; } = 500;
    [Export] public float BrakingAcceleration { get; set; } = 3000;
    private float _flow;

    public override void _EnterTree() => AddToGroup(WindGroup);
    public override void _ExitTree() => RemoveFromGroup(WindGroup);
    public override void _Ready()
    {
        if(!Bounds.Position.IsFinite() || !Bounds.Size.IsFinite() || Bounds.Size.X<=0 || Bounds.Size.Y<=0 ||
            !float.IsFinite(DescentSpeed) || DescentSpeed<=0 || !float.IsFinite(BrakingAcceleration) || BrakingAcceleration<=0)
            throw new InvalidOperationException("Updraft requires finite positive bounds/speed/braking");
    }

    public static Vector2 LimitDescent(Vector2 velocity,float seconds,float speed,float braking)
    {
        if(seconds<=0 || velocity.Y<=speed) return velocity;
        return new Vector2(velocity.X,Mathf.MoveToward(velocity.Y,speed,braking*seconds));
    }

    public static Vector2 ApplyAtPlayer(CharacterBody2D player,Vector2 velocity,double delta)
    {
        if(!player.IsInsideTree() || delta<=0 || velocity.Y<=0) return velocity;
        foreach(Node node in player.GetTree().GetNodesInGroup(WindGroup))
        {
            if(node is not CanyonUpdraft wind || !GodotObject.IsInstanceValid(wind) ||
                !wind.IsInsideTree() || wind.IsQueuedForDeletion() || !wind.CanProcess() ||
                wind.GetWorld2D()!=player.GetWorld2D() || !wind.Bounds.HasPoint(wind.ToLocal(player.GlobalPosition))) continue;
            bool staged=false;
            for(Node ancestor=wind; ancestor!=null; ancestor=ancestor.GetParent())
                if(ancestor.HasMeta("_seamless_staged") && ancestor.GetMeta("_seamless_staged").AsBool()) { staged=true; break; }
            if(!staged) velocity=LimitDescent(velocity,(float)delta,wind.DescentSpeed,wind.BrakingAcceleration);
        }
        return velocity;
    }

    public override void _Process(double delta)
    {
        _flow=(_flow+(float)delta*86)%84;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var air = new Color("8fc8be");
        Vector2 p=Bounds.Position;
        // Soft isolated streaks, not a boxed hazard boundary or a moving HUD hint.
        for(int row=0; row<(int)(Bounds.Size.Y/84)+1; row++)
            for(int lane=0; lane<6; lane++)
            {
                float y=row*84+((lane%3)*23)-_flow;
                if(y<12 || y>Bounds.Size.Y-28) continue;
                float x=36+lane*(Bounds.Size.X-72)/5+Mathf.Sin(row*1.7f+lane)*9;
                Vector2 at=p+new Vector2(x,y);
                Color color=air; color.A=(lane%2==0?.25f:.16f);
                DrawLine(at+new Vector2(0,18),at,color,1.6f,true);
                if(row%4==0)
                {
                    DrawLine(at+new Vector2(-4,5),at,color,1.6f,true);
                    DrawLine(at+new Vector2(4,5),at,color,1.6f,true);
                }
            }
        for(int side=0; side<2; side++)
            for(int port=0; port<4; port++)
            {
                Vector2 at=p+new Vector2(side==0?-12:Bounds.Size.X+12,42+port*560);
                DrawCircle(at,13,new Color("25383c"));
                DrawArc(at,11,0,Mathf.Tau,16,new Color("65968e"),2,true);
                for(int slot=-1; slot<=1; slot++)
                    DrawLine(at+new Vector2(-6,slot*5),at+new Vector2(6,slot*5),new Color("9cc2af"),2,true);
            }
    }
}
