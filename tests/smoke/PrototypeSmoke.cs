using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Shared;
using Knight.Tests;
using Knight.World;

namespace Knight.Tests.Smoke;

/// <summary>
/// Fixed-tick, input-driven wiring check for the generated technical-prototype route.
/// This is intentionally a SceneTree rather than a unit test: the contracts below must
/// survive scene packing, physics registration, input routing, and area contacts.
/// </summary>
public partial class PrototypeSmoke : SceneTree
{
    private const int MaximumPhysicsFrames = 240;
    private int _physicsFrames;

    public override async void _Initialize()
    {
        try
        {
            GD.Print("SMOKE_STAGE=load_room");
            PrototypeRoomController room = GD.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn")
                .Instantiate<PrototypeRoomController>();
            Root.AddChild(room);
            await PhysicsFrames(2);

            CharacterBody2D player = room.GetNode<CharacterBody2D>("Player");
            TestAssert.True(player.IsInGroup("player"), "player group");
            TestAssert.True(room.HasNode("Geometry/ThreadAnchor"), "thread anchor exists");
            TestAssert.True(room.HasNode("Actors/RustShellSquire"), "enemy exists");
            TestAssert.True(InputMap.HasAction(InputActions.Thread), "thread input exists");

            ThreadTarget2D anchor = room.GetNode<ThreadTarget2D>("Geometry/ThreadAnchor");
            RustShellSquire enemy = room.GetNode<RustShellSquire>("Actors/RustShellSquire");
            HealthComponent enemyHealth = enemy.GetNode<HealthComponent>("Health");
            Vector2 checkpoint = player.GlobalPosition;

            GD.Print("SMOKE_STAGE=move");
            ResetPlayer(player, checkpoint);
            float startX = player.GlobalPosition.X;
            await Hold(InputActions.MoveRight, 30);
            TestAssert.True(player.GlobalPosition.X >= startX + 40f, "right input moves player at least 40 pixels");

            GD.Print("SMOKE_STAGE=jump");
            ResetPlayer(player, checkpoint);
            await PhysicsFrames(2);
            float groundedY = player.GlobalPosition.Y;
            Input.ActionPress(InputActions.Jump);
            await PhysicsFrames(10);
            Input.ActionRelease(InputActions.Jump);
            TestAssert.True(player.GlobalPosition.Y < groundedY - 4f, "jump raises player within ten physics frames");

            GD.Print("SMOKE_STAGE=attack_damage");
            ResetPlayer(player, new Vector2(465f, 28f));
            enemy.GlobalPosition = new Vector2(520f, 28f);
            enemyHealth.ResetFull();
            int healthBefore = enemyHealth.CurrentHealth;
            int attacksStarted = 0;
            int attackConnections = 0;
            PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
            combat.AttackStarted += (_, _) => attacksStarted++;
            combat.AttackConnected += _ => attackConnections++;
            Input.ActionPress(InputActions.MoveRight);
            await PhysicsFrames(1);
            Input.ActionRelease(InputActions.MoveRight);
            Input.ActionPress(InputActions.Attack);
            await PhysicsFrames(12);
            Input.ActionRelease(InputActions.Attack);
            GD.Print($"SMOKE_ATTACK_FIXTURE player={player.GlobalPosition} enemy={enemy.GlobalPosition} " +
                $"health={enemyHealth.CurrentHealth} started={attacksStarted} connections={attackConnections}");
            TestAssert.True(enemyHealth.CurrentHealth < healthBefore, "horizontal attack damages the enemy fixture");

            GD.Print("SMOKE_STAGE=thread_anchor");
            ResetPlayer(player, new Vector2(100f, -80f));
            PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
            motor.SetPhysicsProcess(false);
            player.Velocity = Vector2.Zero;
            ThreadAbility thread = player.GetNode<ThreadAbility>("ThreadAbility");
            await TapRightAndWaitForThread(thread);
            TestAssert.True(player.Velocity.Dot((anchor.GlobalPosition - player.GlobalPosition).Normalized()) > 300f,
                "thread pull velocity points toward the anchor");

            GD.Print("SMOKE_STAGE=thread_occlusion");
            var threadWall = new StaticBody2D
            {
                Name = "SmokeThreadWall",
                Position = new Vector2(180f, -80f),
                CollisionLayer = CombatCollisionLayers.World
            };
            threadWall.AddChild(new CollisionShape2D
            {
                Name = "Collision",
                Shape = new RectangleShape2D { Size = new Vector2(20f, 120f) }
            });
            room.AddChild(threadWall);
            await PhysicsFrames(1);
            ResetPlayer(player, new Vector2(100f, -80f));
            await TapRightAndWaitForThread(thread);
            TestAssert.True(thread.CurrentCastFrame.State == ThreadCastState.Failed &&
                thread.CurrentCastFrame.Failure == ThreadFailureReason.Obstructed,
                "world wall produces the named obstruction failure instead of allowing an anchor pull");
            threadWall.QueueFree();
            player.Velocity = Vector2.Zero;
            motor.ResetTransientState();
            motor.SetPhysicsProcess(true);

            GD.Print("SMOKE_STAGE=fall_reset");
            ResetPlayer(player, new Vector2(0f, 560f));
            await PhysicsFrames(3);
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(player.GlobalPosition.DistanceTo(checkpoint) < 2f,
                "kill zone returns player to the initial checkpoint");

            room.QueueFree();
            GD.Print("PROTOTYPE_SMOKE_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.Print("PROTOTYPE_SMOKE_FAIL: " + exception);
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private async Task Hold(StringName action, int frames)
    {
        Input.ActionPress(action);
        await PhysicsFrames(frames);
        Input.ActionRelease(action);
    }

    private async Task TapRightAndWaitForThread(ThreadAbility ability)
    {
        Input.ActionPress(InputActions.MoveRight);
        Input.ActionPress(InputActions.Thread);
        await PhysicsFrames(2);
        Input.ActionRelease(InputActions.Thread);
        Input.ActionRelease(InputActions.MoveRight);
        for (int index = 0; index < 90 && ability.CurrentCastFrame.State is not
            (ThreadCastState.Apply or ThreadCastState.Failed); index++)
            await PhysicsFrames(1);
        if (ability.CurrentCastFrame.State is not (ThreadCastState.Apply or ThreadCastState.Failed))
            throw new InvalidOperationException(
                $"smoke timed out waiting for real thread result; current={ability.CurrentCastFrame.State}");
    }

    private async Task PhysicsFrames(int frames)
    {
        for (int index = 0; index < frames; index++)
        {
            if (++_physicsFrames > MaximumPhysicsFrames)
                throw new InvalidOperationException("smoke hard timeout after fixed physics-frame budget");
            await ToSignal(this, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static void ResetPlayer(CharacterBody2D player, Vector2 position)
    {
        player.GlobalPosition = position;
        player.GetNode<PlayerMotor>("Motor").ResetTransientState();
        player.GetNode<PlayerCombat>("Combat").ResetTransientState();
        player.GetNode<ThreadAbility>("ThreadAbility").ResetTransientState();
    }
}
