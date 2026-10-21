using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PlayerClawPresentationRuntimeTests
{
    private const float StepSeconds = 1f / 60f;

    public static async Task Run(SceneTree tree)
    {
        ReleaseInput();
        Fixture fixture = await CreateFixture(tree);
        try
        {
            await AtomicSpaceTransactionGetsTwoPositiveDeltaPresentationFrames(fixture);
            NewCastPreemptsQueuedLaunch(fixture);
            RecoveryResetPreemptsQueuedLaunch(fixture);
            ShieldAndDashPreemptQueuedLaunch(fixture);
            DeathPreemptsQueuedLaunch(fixture);
        }
        finally
        {
            ReleaseInput();
            fixture.Root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task AtomicSpaceTransactionGetsTwoPositiveDeltaPresentationFrames(Fixture fixture)
    {
        AdvanceNewCastToAttached(fixture);
        var clawStates = new List<MechanicalClawState>();
        fixture.Claw.ClawFrameChanged += OnClaw;
        try
        {
            StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        }
        finally { fixture.Claw.ClawFrameChanged -= OnClaw; }

        void OnClaw(MechanicalClawFrame frame, GrappleSurface2D _) => clawStates.Add(frame.State);

        TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
            "the actual controller transaction ends gameplay in Released immediately");
        int boostIndex = clawStates.IndexOf(MechanicalClawState.Boost);
        int releasedIndex = clawStates.IndexOf(MechanicalClawState.Released);
        TestAssert.True(boostIndex >= 0 && releasedIndex == boostIndex + 1,
            "the actual controller publishes ordered Boost then Released in one Space transaction");

        var displayed = new List<PlayerPresentationFrame>();
        fixture.Coordinator.FramePublished += displayed.Add;
        fixture.Coordinator._Process(StepSeconds);
        PlayerPresentationFrame launch = fixture.Coordinator.LastPlayerFrame;
        TestAssert.Equal(PlayerActionState.ThreadApply, launch.Action.State,
            "the coordinator publishes a claw action on the first positive-delta display frame");
        TestAssert.Equal(PlayerActionPhase.Contact, launch.Action.Phase,
            "the first positive-delta display frame preserves the accepted Boost onset");
        TestAssert.Equal(launch, fixture.Formal.CurrentPlayerFrame,
            "the formal visual applies the same typed Boost frame published by the coordinator");

        fixture.Coordinator._Process(StepSeconds);
        PlayerPresentationFrame recovery = fixture.Coordinator.LastPlayerFrame;
        fixture.Coordinator.FramePublished -= displayed.Add;
        TestAssert.Equal(PlayerActionState.ThreadApply, recovery.Action.State,
            "the second positive-delta display frame remains a typed claw action");
        TestAssert.Equal(PlayerActionPhase.Recovery, recovery.Action.Phase,
            "the display advances from Boost launch to Released recovery on the following frame");
        TestAssert.Equal(recovery, fixture.Formal.CurrentPlayerFrame,
            "the formal visual applies the coordinator's subsequent typed recovery frame");
        TestAssert.True(displayed.Count == 2 && displayed[0].Action.Phase == PlayerActionPhase.Contact &&
                        displayed[1].Action.Phase == PlayerActionPhase.Recovery,
            "published typed presentation order is Boost contact then Released recovery exactly once");

        StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        fixture.Coordinator._Process(StepSeconds);
        TestAssert.True(fixture.Coordinator.LastPlayerFrame.Action.Phase != PlayerActionPhase.Contact,
            "a repeated Space sample after release cannot replay the Boost presentation onset");
        await Task.CompletedTask;
    }

    private static void NewCastPreemptsQueuedLaunch(Fixture fixture)
    {
        SettleToIdle(fixture);
        AdvanceNewCastToAttached(fixture);
        StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        SettleToIdle(fixture);
        StartCast(fixture);
        fixture.Coordinator._Process(StepSeconds);
        TestAssert.Equal(PlayerActionPhase.Anticipation, fixture.Coordinator.LastPlayerFrame.Action.Phase,
            "a new cast preempts any stale launch/recovery envelope from the prior cast");
    }

    private static void RecoveryResetPreemptsQueuedLaunch(Fixture fixture)
    {
        AdvanceCurrentCastToAttached(fixture);
        StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        fixture.Coordinator.ResetRecoveryPresentation();
        fixture.Coordinator._Process(StepSeconds);
        TestAssert.True(fixture.Coordinator.LastPlayerFrame.Action.Phase != PlayerActionPhase.Contact,
            "recovery reset clears a queued presentation-only Boost launch");
    }

    private static void ShieldAndDashPreemptQueuedLaunch(Fixture fixture)
    {
        SettleToIdle(fixture);
        AdvanceNewCastToAttached(fixture);
        StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        SetInput(fixture.Input, InputFrame(shieldHeld: true, mouseWorld: Vector2.Right * 200f));
        fixture.Shield._PhysicsProcess(0d);
        fixture.Energy._PhysicsProcess(StepSeconds);
        fixture.Shield._PhysicsProcess(0d);
        TestAssert.True(fixture.Shield.IsActive, "real shield components establish the preemption fixture");
        fixture.Coordinator._Process(StepSeconds);
        TestAssert.Equal(PlayerActionState.Shield, fixture.Coordinator.LastPlayerFrame.Action.State,
            "active shield preempts a stale claw launch envelope");

        SetInput(fixture.Input, InputFrame());
        fixture.Shield._PhysicsProcess(0d);
        fixture.Energy._PhysicsProcess(StepSeconds);
        fixture.Shield._PhysicsProcess(0d);
        SettleToIdle(fixture);
        fixture.Motor.ResetTransientState();
        AdvanceNewCastToAttached(fixture);
        StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        SetInput(fixture.Input, InputFrame(dashPressed: true));
        fixture.Motor._PhysicsProcess(StepSeconds);
        TestAssert.True(fixture.Motor.IsDashing, "real motor accepts the dash preemption fixture");
        fixture.Coordinator._Process(StepSeconds);
        TestAssert.Equal(PlayerLocomotionState.Dash, fixture.Coordinator.LastPlayerFrame.Locomotion.State,
            "active dash preempts a stale claw launch envelope");
        TestAssert.True(fixture.Coordinator.LastPlayerFrame.Action.Phase != PlayerActionPhase.Contact,
            "dash cannot retain the stale Boost contact action over its locomotion");
    }

    private static void DeathPreemptsQueuedLaunch(Fixture fixture)
    {
        fixture.Motor.ResetTransientState();
        SettleToIdle(fixture);
        AdvanceNewCastToAttached(fixture);
        StepClaw(fixture, InputFrame(jumpPressed: true, mouseWorld: fixture.Surface.WorldAnchor));
        bool applied = fixture.Health.ApplyDamage(new DamageRequest(99, Vector2.Left, 0f, 7001));
        TestAssert.True(applied && fixture.Health.CurrentHealth == 0,
            "real health component establishes the death preemption fixture");
        fixture.Coordinator._Process(StepSeconds);
        TestAssert.Equal(PlayerLocomotionState.Dead, fixture.Coordinator.LastPlayerFrame.Locomotion.State,
            "death preempts the presentation-only claw envelope");
        TestAssert.Equal(PlayerActionFrame.None, fixture.Coordinator.LastPlayerFrame.Action,
            "death retains no stale claw action on the formal typed frame");
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree)
    {
        var root = new Node2D { Name = "PlayerClawPresentationRuntimeFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        GrappleSurface2D surface = Surface("PresentationSurface", new Vector2(180f, 0f));
        root.AddChild(player);
        root.AddChild(surface);
        tree.Root.AddChild(root);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var fixture = new Fixture(root, player, surface,
            player.GetNode<PlayerInputReader>("Input"),
            player.GetNode<PlayerActionModeController>("ActionModes"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"),
            player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<PlayerEnergyController>("Energy"),
            player.GetNode<PlayerShieldController>("Shield"),
            player.GetNode<HealthComponent>("Health"),
            player.GetNode<PlayerPresentationCoordinator>("Presentation"),
            player.GetNode<PlayerFormalVisual>("VisualRoot/Formal"));
        fixture.Input.SetPhysicsProcess(false);
        fixture.Modes.SetPhysicsProcess(false);
        fixture.Claw.SetPhysicsProcess(false);
        fixture.Motor.SetPhysicsProcess(false);
        fixture.Energy.SetPhysicsProcess(false);
        fixture.Shield.SetPhysicsProcess(false);
        fixture.Coordinator.SetProcess(false);
        fixture.Motor.Tuning = new PlayerTuning { Gravity = .01f };
        fixture.Claw.ResetTransientState();
        return fixture;
    }

    private static void AdvanceNewCastToAttached(Fixture fixture)
    {
        StartCast(fixture);
        AdvanceCurrentCastToAttached(fixture);
    }

    private static void StartCast(Fixture fixture)
    {
        StepClaw(fixture, InputFrame(grappleModePressed: true, primaryPressed: true,
            mouseWorld: fixture.Surface.WorldAnchor));
        TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Aim or
                        MechanicalClawState.Windup,
            "one real LMB edge starts a new mechanical-claw cast");
    }

    private static void AdvanceCurrentCastToAttached(Fixture fixture)
    {
        for (int frame = 0; frame < 180; frame++)
        {
            if (fixture.Claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing)
                return;
            if (fixture.Claw.CurrentFrame.State is MechanicalClawState.Failed or MechanicalClawState.Idle)
                throw new InvalidOperationException(
                    $"Mechanical claw failed before attachment: {fixture.Claw.CurrentFrame.State}/" +
                    fixture.Claw.CurrentFrame.Failure);
            StepClaw(fixture, InputFrame(mouseWorld: fixture.Surface.WorldAnchor));
        }
        throw new InvalidOperationException("Timed out waiting for real auto-pull attachment.");
    }

    private static void SettleToIdle(Fixture fixture)
    {
        SetInput(fixture.Input, InputFrame());
        fixture.Shield._PhysicsProcess(0d);
        for (int frame = 0; frame < 180; frame++)
        {
            if (fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle) return;
            StepClaw(fixture, InputFrame(mouseWorld: fixture.Surface.WorldAnchor));
        }
        throw new InvalidOperationException(
            $"Timed out waiting for claw Idle; current={fixture.Claw.CurrentFrame.State}");
    }

    private static void StepClaw(Fixture fixture, PlayerInputFrame frame)
    {
        SetInput(fixture.Input, frame);
        fixture.Modes._PhysicsProcess(0d);
        fixture.Claw._PhysicsProcess(StepSeconds);
    }

    private static PlayerInputFrame InputFrame(bool grappleModePressed = false,
        bool primaryPressed = false, bool jumpPressed = false, bool shieldHeld = false,
        bool dashPressed = false, Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, jumpPressed, false, dashPressed, false, false,
            MouseWorld: mouseWorld,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            ShieldHeld: shieldHeld);

    private static void SetInput(PlayerInputReader input, PlayerInputFrame frame) =>
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);

    private static GrappleSurface2D Surface(string name, Vector2 position)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 10f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static void ReleaseInput()
    {
        foreach (StringName action in new[]
                 {
                     InputActions.MoveLeft, InputActions.MoveRight, InputActions.Jump,
                     InputActions.Dash, InputActions.PrimaryAction, InputActions.Shield,
                     InputActions.SelectAttackMode, InputActions.SelectGrappleMode
                 })
            Input.ActionRelease(action);
    }

    private sealed record Fixture(
        Node2D Root,
        CharacterBody2D Player,
        GrappleSurface2D Surface,
        PlayerInputReader Input,
        PlayerActionModeController Modes,
        MechanicalClawController Claw,
        PlayerMotor Motor,
        PlayerEnergyController Energy,
        PlayerShieldController Shield,
        HealthComponent Health,
        PlayerPresentationCoordinator Coordinator,
        PlayerFormalVisual Formal);
}
