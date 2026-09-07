using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Player;

namespace Knight.Tests.Unit;

public static partial class ThreadInteractionTests
{
    public static void Run()
    {
        MechanismCallsReceiverOnceWithoutMovingPlayer();
        LightEnemyKeepsExistingPullBehavior();
        AnchorStillPullsPlayer();
        HeavyReceiverOpensWeakPointWithoutMovingBodies();
        HeavyWithoutReceiverFallsBackToPlayerPull();
    }

    private static void MechanismCallsReceiverOnceWithoutMovingPlayer()
    {
        Fixture fixture = PlayerFixture();
        var receiver = new InteractionSpy { Name = "Receiver", Accept = true };
        var target = Target(ThreadTargetKind.Mechanism, receiver);
        target.ApplyPull(fixture.Player, fixture.Player.GlobalPosition, Settings());
        TestAssert.Equal(1, receiver.Calls, "mechanism target calls its receiver exactly once");
        TestAssert.Equal(fixture.Player, receiver.Last.Player, "interaction identifies the persistent player");
        TestAssert.Equal(Vector2.Zero, fixture.Player.Velocity, "accepted mechanism interaction does not impulse the player");
        target.Free(); fixture.Player.Free();
    }

    private static void LightEnemyKeepsExistingPullBehavior()
    {
        var player = new Node2D { GlobalPosition = Vector2.Zero };
        var receiver = new PullSpy { Name = "Receiver" };
        ThreadTarget2D target = Target(ThreadTargetKind.LightEnemy, receiver);
        target.GlobalPosition = new Vector2(100, 0);
        target.ApplyPull(player, player.GlobalPosition, Settings());
        TestAssert.Near(-540f, receiver.Velocity.X, 0.001f, "light enemy remains pulled toward player");
        target.Free(); player.Free();
    }

    private static void AnchorStillPullsPlayer()
    {
        Fixture fixture = PlayerFixture();
        ThreadTarget2D target = Target(ThreadTargetKind.Anchor, null);
        target.GlobalPosition = new Vector2(100, 0);
        target.ApplyPull(fixture.Player, fixture.Player.GlobalPosition, Settings());
        TestAssert.Near(620f, fixture.Player.Velocity.X, 0.001f, "anchor still pulls player at configured speed");
        target.Free(); fixture.Player.Free();
    }

    private static void HeavyReceiverOpensWeakPointWithoutMovingBodies()
    {
        Fixture fixture = PlayerFixture();
        var receiver = new InteractionSpy { Name = "Receiver", Accept = true };
        var target = Target(ThreadTargetKind.HeavyEnemy, receiver);
        target.ApplyPull(fixture.Player, fixture.Player.GlobalPosition, Settings());
        TestAssert.Equal(1, receiver.Calls, "heavy interaction receiver opens its weak point once");
        TestAssert.Equal(Vector2.Zero, fixture.Player.Velocity, "accepted heavy interaction never moves player");
        TestAssert.Equal(Vector2.Zero, receiver.Velocity, "accepted heavy interaction never moves heavy body");
        target.Free(); fixture.Player.Free();
    }

    private static void HeavyWithoutReceiverFallsBackToPlayerPull()
    {
        Fixture fixture = PlayerFixture();
        ThreadTarget2D target = Target(ThreadTargetKind.HeavyEnemy, null);
        target.GlobalPosition = new Vector2(0, -100);
        target.ApplyPull(fixture.Player, fixture.Player.GlobalPosition, Settings());
        TestAssert.Near(-620f, fixture.Player.Velocity.Y, 0.001f,
            "heavy target without interaction receiver falls back to player pull");
        target.Free(); fixture.Player.Free();
    }

    private static ThreadTarget2D Target(ThreadTargetKind kind, Node receiver)
    {
        var target = new ThreadTarget2D { Name = "Target", Kind = kind, PullReceiverPath = new NodePath("Receiver") };
        if (receiver != null) target.AddChild(receiver);
        return target;
    }

    private static Fixture PlayerFixture()
    {
        var player = new CharacterBody2D { Name = "Player" };
        player.AddChild(new PlayerInputReader { Name = "Input" });
        var motor = new PlayerMotor { Name = "Motor" };
        player.AddChild(motor);
        motor._Ready();
        return new Fixture(player);
    }

    private static ThreadAbilitySettings Settings() => new(PlayerPullSpeed: 620f, EnemyPullSpeed: 540f);

    private sealed record Fixture(CharacterBody2D Player);

    private sealed partial class PullSpy : Node2D, IThreadPullReceiver
    {
        public Vector2 Velocity { get; private set; }
        public void ApplyThreadPull(Vector2 velocity) => Velocity = velocity;
    }

    private sealed partial class InteractionSpy : Node2D, IThreadInteractionReceiver
    {
        public bool Accept { get; set; }
        public int Calls { get; private set; }
        public ThreadInteraction Last { get; private set; }
        public Vector2 Velocity { get; private set; }
        public bool TryReceiveThread(ThreadInteraction interaction)
        {
            Calls++;
            Last = interaction;
            return Accept;
        }
    }
}
