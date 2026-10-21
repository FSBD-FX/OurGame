using Godot;

namespace Knight.World;

/// <summary>Small, scene-independent safe-position model for the prototype route.</summary>
public sealed class PrototypeCheckpoint
{
    public Vector2 CurrentPosition { get; private set; }

    public PrototypeCheckpoint(Vector2 initialPosition)
    {
        Activate(initialPosition);
    }

    public bool Activate(Vector2 safePosition)
    {
        if (!float.IsFinite(safePosition.X) || !float.IsFinite(safePosition.Y)) return false;
        CurrentPosition = safePosition;
        return true;
    }

    public void ApplyRespawn(CharacterBody2D body)
    {
        body.GlobalPosition = CurrentPosition;
        body.Velocity = Vector2.Zero;
    }
}
