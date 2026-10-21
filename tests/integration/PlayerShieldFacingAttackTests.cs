using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PlayerShieldFacingAttackTests
{
    // Regression: a pointer-only shield turn must not make the presentation disagree
    // with the subsequently accepted attack's actual hitbox. No visual-frame injection.
    public static async Task Run(SceneTree tree)
    {
        Node2D fixture = null;
        try
        {
            ReleaseInputs();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            fixture = new Node2D { Name = "PlayerShieldFacingAttack" };
            var floor = new StaticBody2D
            {
                Position = new Vector2(0f, 60f),
                CollisionLayer = CombatCollisionLayers.World,
                CollisionMask = 0
            };
            floor.AddChild(new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
            });
            fixture.AddChild(floor);
            var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            player.Position = new Vector2(0f, 14f);
            fixture.AddChild(player);
            tree.Root.AddChild(fixture);
            await Frames(tree, 12);

            var input = player.GetNode<PlayerInputReader>("Input");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var combat = player.GetNode<PlayerCombat>("Combat");
            var hitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
            var presentation = player.GetNode<PlayerPresentationCoordinator>("Presentation");
            TestAssert.True(player.IsOnFloor() && Mathf.Abs(player.Velocity.X) < .01f,
                "fixture settles without horizontal movement or an earlier attack");
            TestAssert.Equal(1, presentation.LastPlayerFrame.Base.Facing,
                "the untouched production player initially faces right");

            SendAction(InputActions.SelectAttackMode, true);
            SendAction(InputActions.Slot1, true);
            await Frames(tree, 1);
            SendAction(InputActions.SelectAttackMode, false);
            SendAction(InputActions.Slot1, false);

            // Mouse events carry viewport coordinates; the production reader converts
            // them to world aim. Do not write shield direction or coordinator facing.
            Vector2 leftWorld = player.GlobalPosition + new Vector2(-300f, 0f);
            Vector2 pointer = player.GetViewport().GetFinalTransform() *
                (player.GetGlobalTransformWithCanvas() * player.ToLocal(leftWorld));
            Input.ParseInputEvent(new InputEventMouseMotion
            {
                Position = pointer,
                GlobalPosition = pointer
            });
            SendMouse(MouseButton.Right, true, pointer);
            await Until(tree, () => shield.IsActive && shield.GuardDirection.X < -.9f &&
                presentation.LastPlayerFrame.Base.Facing == -1, 60,
                "real leftward mouse aim raises a left-facing shield and publishes its facing",
                () => $"PLAYER_SHIELD_FACING_GUARD_TIMEOUT shield_active={shield.IsActive} " +
                    $"energy_overheated={player.GetNode<PlayerEnergyController>("Energy").IsOverheated} " +
                    $"energy_ratio={player.GetNode<PlayerEnergyController>("Energy").EnergyRatio} " +
                    $"shield_sustained={player.GetNode<PlayerEnergyController>("Energy").IsShieldSustained} " +
                    $"input_shield_held={input.CurrentFrame.ShieldHeld} " +
                    $"action_shield_pressed={Input.IsActionPressed(InputActions.Shield)} " +
                    $"mouse_right_pressed={Input.IsMouseButtonPressed(MouseButton.Right)} " +
                    $"has_observed_motion={input.HasObservedMouseMotion} " +
                    $"last_mouse_viewport={input.LastObservedMouseViewportPosition} " +
                    $"requested_pointer={pointer} mouse_world={input.CurrentFrame.MouseWorld} " +
                    $"player_world={player.GlobalPosition} guard_direction={shield.GuardDirection} " +
                    $"coordinator_frame={presentation.LastPlayerFrame} " +
                    $"mode={player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode}");
            TestAssert.True(input.CurrentFrame.Aim.IsZeroApprox() &&
                Mathf.Abs(input.CurrentFrame.MoveAxis) < .01f,
                "pointer-only guard turn supplies no A/D or directional attack input");

            // Release RMB and provide a fresh LMB edge together, without any movement
            // input that could synchronize Combat's own facing with the shield pose.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            SendMouse(MouseButton.Right, false, pointer);
            SendMouse(MouseButton.Left, true, pointer);
            await Until(tree, () => combat.IsAttacking &&
                presentation.LastPlayerFrame.Action.State == PlayerActionState.GroundAttack,
                30, "real attack is accepted and its action is published by _Process");

            PlayerPresentationFrame frame = presentation.LastPlayerFrame;
            float actualHitboxX = hitbox.Position.X;
            GD.Print($"PLAYER_SHIELD_FACING_ATTACK actual_hitbox_x={actualHitboxX} " +
                $"base_facing={frame.Base.Facing} action_aim={frame.Action.Aim} " +
                $"shield_active={shield.IsActive} sampled_aim={input.CurrentFrame.Aim}");
            TestAssert.True(actualHitboxX > 0f && !shield.IsActive &&
                input.CurrentFrame.Aim.IsZeroApprox() && Mathf.Abs(player.Velocity.X) < .01f,
                "stationary fresh attack really uses the unchanged right-side gameplay hitbox");
            int actualFacing = Math.Sign(actualHitboxX);
            TestAssert.Equal(actualFacing, frame.Base.Facing,
                "shield-to-attack presentation must face the real accepted attack hitbox");
            TestAssert.True(frame.Action.Aim.X * actualFacing > .99f,
                "shield-to-attack action aim must agree with the real accepted attack hitbox");
            GD.Print("PLAYER_SHIELD_FACING_ATTACK_OK real_mouse=True movement_input=False");
        }
        finally
        {
            ReleaseInputs();
            if (GodotObject.IsInstanceValid(fixture))
            {
                if (fixture.IsInsideTree())
                {
                    fixture.QueueFree();
                    await Frames(tree, 3);
                }
                else fixture.Free();
            }
        }
    }

    private static async Task Until(SceneTree tree, Func<bool> condition, int budget, string reason,
        Func<string> timeoutDiagnostic = null)
    {
        for (int index = 0; index < budget && !condition(); index++) await Frames(tree, 1);
        if (!condition() && timeoutDiagnostic != null) GD.Print(timeoutDiagnostic());
        TestAssert.True(condition(), reason);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void SendMouse(MouseButton button, bool pressed, Vector2 pointer)
    {
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Position = pointer,
            GlobalPosition = pointer,
            ButtonMask = pressed
                ? button == MouseButton.Left ? MouseButtonMask.Left : MouseButtonMask.Right
                : (MouseButtonMask)0
        });
        Input.FlushBufferedEvents();
    }

    private static void SendAction(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseInputs()
    {
        foreach (StringName action in new[]
        {
            InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.Jump, InputActions.Dash,
            InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
            InputActions.Slot1, InputActions.Slot2, InputActions.Slot3, InputActions.Slot4,
            InputActions.PrimaryAction, InputActions.Shield
        }) SendAction(action, false);
        SendMouse(MouseButton.Left, false, Vector2.Zero);
        SendMouse(MouseButton.Right, false, Vector2.Zero);
    }
}
