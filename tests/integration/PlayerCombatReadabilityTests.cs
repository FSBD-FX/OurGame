using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PlayerCombatReadabilityTests
{
    public static async Task Run(SceneTree tree)
    {
        ShieldTimelineContracts();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var fixture = new Node2D { Name = "PlayerCombatReadability" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
        floor.AddChild(new CollisionShape2D {
            Shape = new RectangleShape2D { Size = new Vector2(800, 20) } });
        fixture.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        try
        {
            await Frames(tree, 12);
            var rig = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            float idleArm = rig.CurrentPose.NearArm.RotationRadians;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            AimAt(player, player.GlobalPosition + new Vector2(300, 0));
            Send(InputActions.Shield, true);
            await Frames(tree, 12);
            TestAssert.True(shield.IsActive, "real sampled shield and energy establish a sustained guard");
            TestAssert.True(Mathf.Abs(rig.CurrentPose.NearArm.RotationRadians - idleArm) > .35f,
                "a live sustained guard raises the formal player's arm, not only the separate shield fan");
            TestAssert.Equal(PlayerActionState.Shield, rig.CurrentPlayerFrame.Action.State,
                "the actual coordinator publishes Shield instead of neutral locomotion");
            TestAssert.True(rig.CurrentPlayerFrame.Action.Aim.Dot(shield.GuardDirection) > .99f,
                "guard pose follows the actual defensive direction");
            TestAssert.True(rig.Position == Vector2.Zero, "guard presentation does not move player root");
            AimAt(player, player.GlobalPosition + new Vector2(-300, 0));
            await Frames(tree, 4);
            TestAssert.True(shield.GuardDirection.X < -.9f && rig.Scale.X < 0f &&
                rig.CurrentPlayerFrame.Action.Aim.Dot(shield.GuardDirection) > .99f,
                "real pointer reversal mirrors the player and keeps body support aligned with the shield");
            Send(InputActions.Shield, false);
            await Frames(tree, 15);
            TestAssert.True(!shield.IsActive && rig.CurrentPlayerFrame.Action.State == PlayerActionState.None,
                "actual release leaves no stuck guard action");
            Send(InputActions.Shield, true);
            await Frames(tree, 12);
            for (int tick = 0; tick < 320 && shield.IsActive; tick++) await Frames(tree, 1);
            TestAssert.True(player.GetNode<PlayerEnergyController>("Energy").IsOverheated && !shield.IsActive,
                "held input naturally overheats rather than faking shield closure");
            await Frames(tree, 15);
            TestAssert.True(rig.CurrentPlayerFrame.Action.State != PlayerActionState.Shield,
                "held RMB cannot keep the body guarding after actual shield overheat");
        }
        finally
        {
            Send(InputActions.Shield, false);
            Send(InputActions.PrimaryAction, false);
            fixture.QueueFree();
            await Frames(tree, 3);
        }
    }

    private static void ShieldTimelineContracts()
    {
        var timeline = new PlayerPresentationTimeline();
        var input = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, 1,
            ShieldActive: true, ShieldAim: Vector2.Left);
        PlayerPresentationFrame frame = timeline.Step(input, .03f);
        TestAssert.True(frame.Action.State == PlayerActionState.Shield &&
            frame.Action.Phase == PlayerActionPhase.Anticipation,
            "shield enters through a timed preparation pose");
        frame = timeline.Step(input, .07f);
        TestAssert.True(frame.Action.Phase == PlayerActionPhase.Contact && frame.Action.Aim == Vector2.Left,
            "held real shield reaches stable support and preserves aim");
        frame = timeline.Step(input with { ShieldActive = false }, .04f);
        TestAssert.True(frame.Action.State == PlayerActionState.Shield &&
            frame.Action.Phase == PlayerActionPhase.Recovery, "release has an explicit recovery phase");
        frame = timeline.Step(input with { ShieldActive = false }, .09f);
        TestAssert.Equal(PlayerActionState.None, frame.Action.State, "release completes without waiting for a new action");
        timeline.Step(input, .2f);
        frame = timeline.Step(input with { AttackId = 11, IsAttacking = true }, .01f);
        TestAssert.Equal(PlayerActionState.GroundAttack, frame.Action.State,
            "an already accepted attack is never hidden by stale/held shield state");
        frame = timeline.Step(input with { Dead = true }, .01f);
        TestAssert.Equal(PlayerActionState.None, frame.Action.State, "death suppresses guarding");
        frame = new PlayerPresentationTimeline().Step(input with { CheckpointRecovering = true }, .2f);
        TestAssert.Equal(PlayerActionState.None, frame.Action.State, "checkpoint presentation cannot keep a guard overlay");
        frame = new PlayerPresentationTimeline().Step(input with { HurtTriggered = true }, .01f);
        TestAssert.Equal(PlayerActionState.Hurt, frame.Action.State, "hurt has precedence over guard");
        timeline = new PlayerPresentationTimeline();
        timeline.Step(input, .2f);
        timeline.Step(input with { ShieldActive = false }, .06f);
        frame = timeline.Step(input, .01f);
        TestAssert.True(frame.Action.Phase == PlayerActionPhase.Anticipation && frame.Action.NormalizedTime < .2f,
            "repress during release starts a new timed guard instead of reusing release progress");
        timeline.Step(input with { HurtTriggered = true }, .01f);
        // Finish hurt in small frame steps so the following guard entry is observed.
        for (int index = 0; index < 24; index++)
        {
            frame = timeline.Step(input, .01f);
            if (frame.Action.State == PlayerActionState.Shield) break;
        }
        TestAssert.True(frame.Action.State == PlayerActionState.Shield &&
            frame.Action.Phase == PlayerActionPhase.Anticipation && frame.Action.NormalizedTime < .2f,
            "held active shield re-enters through preparation after hurt ends");
        timeline.Step(input with { AttackId = 22, IsAttacking = true }, .01f);
        for (int index = 0; index < 50; index++)
        {
            frame = timeline.Step(input with { AttackId = 22, IsAttacking = false }, .01f);
            if (frame.Action.State == PlayerActionState.Shield) break;
        }
        TestAssert.True(frame.Action.State == PlayerActionState.Shield &&
            frame.Action.Phase == PlayerActionPhase.Anticipation && frame.Action.NormalizedTime < .2f,
            "held active shield re-enters through preparation after an accepted attack ends");
    }

    private static void AimAt(Node2D player, Vector2 world)
    {
        Vector2 pointer = player.GetViewport().GetFinalTransform() *
            (player.GetGlobalTransformWithCanvas() * player.ToLocal(world));
        Input.ParseInputEvent(new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer });
        Input.FlushBufferedEvents();
    }

    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action,
            Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
