using Godot;
using Knight.World;

namespace Knight.Tests.Unit;

public static class PrototypeCheckpointTests
{
    public static void Run()
    {
        StoresTheMostRecentFiniteSafePosition();
        RejectsNonFiniteSafePositions();
        RespawnClearsVelocityAndUsesTheStoredPosition();
    }

    private static void StoresTheMostRecentFiniteSafePosition()
    {
        var checkpoint = new PrototypeCheckpoint(new Vector2(-500f, 180f));

        bool accepted = checkpoint.Activate(new Vector2(210f, 64f));

        TestAssert.True(accepted, "finite checkpoint positions are accepted");
        TestAssert.Equal(new Vector2(210f, 64f), checkpoint.CurrentPosition,
            "the latest finite activation becomes the respawn position");
    }

    private static void RejectsNonFiniteSafePositions()
    {
        var checkpoint = new PrototypeCheckpoint(new Vector2(12f, 34f));

        bool accepted = checkpoint.Activate(new Vector2(float.NaN, float.PositiveInfinity));

        TestAssert.True(!accepted, "non-finite checkpoint positions are rejected");
        TestAssert.Equal(new Vector2(12f, 34f), checkpoint.CurrentPosition,
            "a rejected activation keeps the last valid respawn position");
    }

    private static void RespawnClearsVelocityAndUsesTheStoredPosition()
    {
        var checkpoint = new PrototypeCheckpoint(new Vector2(260f, -80f));
        var player = new CharacterBody2D
        {
            GlobalPosition = new Vector2(999f, 999f),
            Velocity = new Vector2(-80f, 150f)
        };

        checkpoint.ApplyRespawn(player);

        TestAssert.Equal(checkpoint.CurrentPosition, player.GlobalPosition,
            "respawn returns the player to the stored checkpoint");
        TestAssert.Equal(Vector2.Zero, player.Velocity, "respawn clears all player velocity");
        player.Free();
    }
}
