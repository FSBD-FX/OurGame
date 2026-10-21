using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static partial class ThreadTarget2DTests
{
    public static void Run()
    {
        LightEnemyPullUsesConfiguredEnemySpeedTowardPlayer();
    }

    private static void LightEnemyPullUsesConfiguredEnemySpeedTowardPlayer()
    {
        var player = new Node2D { GlobalPosition = Vector2.Zero };
        var receiver = new ThreadPullReceiverSpy { Name = "Receiver" };
        var target = new ThreadTarget2D
        {
            Kind = ThreadTargetKind.LightEnemy,
            PullReceiverPath = new NodePath("Receiver"),
            GlobalPosition = new Vector2(120f, 0f)
        };
        target.AddChild(receiver);

        target.ApplyPull(player, player.GlobalPosition, new ThreadAbilitySettings(EnemyPullSpeed: 540f));

        TestAssert.Near(-540f, receiver.LastVelocity.X, 0.001f,
            "a light enemy receives the configured pull speed toward the player");
        TestAssert.Near(0f, receiver.LastVelocity.Y, 0.001f,
            "a light enemy pull points directly toward a horizontally aligned player");
        target.Free();
        player.Free();
    }

    private sealed partial class ThreadPullReceiverSpy : Node2D, IThreadPullReceiver
    {
        public Vector2 LastVelocity { get; private set; }

        public void ApplyThreadPull(Vector2 velocity)
        {
            LastVelocity = velocity;
        }
    }
}
