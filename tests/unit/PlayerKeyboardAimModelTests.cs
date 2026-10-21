using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerKeyboardAimModelTests
{
    public static void Run()
    {
        foreach (Vector2 axis in new[] { Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down,
            new Vector2(-1,-1), new Vector2(1,-1), new Vector2(-1,1), new Vector2(1,1) })
            TestAssert.True(PlayerKeyboardAimModel.ResolveDirection(axis, 1).IsEqualApprox(axis.Normalized()),
                "EightDirectionsAndFacingFallback: " + axis);
        TestAssert.Equal(Vector2.Left, PlayerKeyboardAimModel.ResolveDirection(Vector2.Zero, -1), "idle faces left");
        TestAssert.Equal(Vector2.Right, PlayerKeyboardAimModel.ResolveDirection(new Vector2(float.NaN, 1), 1),
            "invalid direction safely falls back");
        TestAssert.Equal(Vector2.Right, PlayerKeyboardAimModel.ResolveDirection(new Vector2(1,.05f), 1),
            "analog drift snaps to eight-way direction");
    }
}
