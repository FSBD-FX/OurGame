using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Real keyboard traversal only. No position, velocity, physics, AI or
    // resource writes; no attached Boost and no retry jump.
    private static async Task ReturnTask10SplitGalleryFromBarrier(SceneTree tree,
        CharacterBody2D player, RouteMonitor monitor, PrologueSector ascent, Action observe)
    {
        if (KeyboardRouteInputDriver.Current == null) return; // Legacy caller unchanged.
        var gallery = ascent.GetNode<StaticBody2D>("Gameplay/Collision/SplitUpperGallery");
        var galleryShape = gallery.GetNode<CollisionShape2D>("Collision");
        var barrier = ascent.GetNode<StaticBody2D>("Gameplay/Mechanisms/UpperBasinShortcutWinch/Barrier");
        var barrierShape = barrier.GetNode<CollisionShape2D>("Collision");
        var grip9Shape = ascent.GetNode<CollisionShape2D>("Gameplay/Collision/AscentGrip9/Collision");
        var body = player.GetNode<CollisionShape2D>("BodyCollider");
        var capsule = (CapsuleShape2D)body.Shape;
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var driver = KeyboardRouteInputDriver.Current;
        ulong playerId = player.GetInstanceId(), start = Engine.GetPhysicsFrames();
        int hp = health.CurrentHealth, requests = monitor.RecoveryRequested, recoveries = monitor.RecoveryCompleted;
        int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount,
            attacks = driver.AttackEdges, casts = driver.ClawEdges;
        ulong nextCast = claw.CurrentFrame.NextCastId;
        const ulong budget = 90;
        string phase = "entry";
        bool jumpSeen = false, contactSeen = false;
        float minimumFeet = float.PositiveInfinity;

        float Top(CollisionShape2D shape)
        {
            var rectangle = (RectangleShape2D)shape.Shape;
            return Mathf.Min(shape.ToGlobal(new Vector2(0, -rectangle.Size.Y / 2)).Y,
                shape.ToGlobal(new Vector2(0, rectangle.Size.Y / 2)).Y);
        }
        bool UpContact(StaticBody2D support)
        {
            if (!player.IsOnFloor()) return false;
            for (int i = 0; i < player.GetSlideCollisionCount(); i++)
            {
                using var contact = player.GetSlideCollision(i);
                if (contact.GetCollider() == support && contact.GetNormal().Y < -.7f) return true;
            }
            return false;
        }
        float Feet() => Mathf.Max(body.ToGlobal(new Vector2(0, -capsule.Height / 2)).Y,
            body.ToGlobal(new Vector2(0, capsule.Height / 2)).Y);
        bool Supported(StaticBody2D support, CollisionShape2D shape) =>
            TryTask10CapsuleCorridor(player, shape, out _, out _, out _, out _) &&
            UpContact(support) && MathF.Abs(Feet() - Top(shape)) <= 1f;
        bool Stationary() => player.Velocity.IsFinite() &&
            MathF.Abs(player.Velocity.X) <= 2f && MathF.Abs(player.Velocity.Y) <= 2f;
        if (Supported(gallery, galleryShape)) return;
        void Check(bool value, string condition) => TestAssert.True(value,
            $"SPLIT_GALLERY_RETURN_RED {condition}; phase={phase} actual={Engine.GetPhysicsFrames()-start}/{budget} " +
            $"position={player.GlobalPosition} velocity={player.Velocity} jump={jumps}->{motor.AcceptedJumpCount}");
        void Guard()
        {
            Check(GodotObject.IsInstanceValid(player) && player.IsInsideTree() && !player.IsQueuedForDeletion() &&
                player.GetInstanceId() == playerId && monitor.Player == player &&
                KeyboardRouteInputDriver.Current == driver, "same actual Player and existing keyboard driver");
            Check(health.CurrentHealth == hp && hp > 0 && monitor.RecoveryRequested == requests &&
                monitor.RecoveryCompleted == recoveries, "unchanged HP and recovery counts");
            Check(motor.AcceptedJumpCount >= jumps && motor.AcceptedJumpCount <= jumps + 1 &&
                motor.AcceptedDashCount == dashes && driver.AttackEdges == attacks && driver.ClawEdges == casts &&
                claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentFrame.NextCastId == nextCast,
                "at most one ordinary jump, no new dash/J/K or attached assistance");
            Check(Engine.GetPhysicsFrames()-start <= budget, "one actual shared physics deadline");
            var rectangle = (RectangleShape2D)grip9Shape.Shape;
            float ceilingLeft = grip9Shape.ToGlobal(new Vector2(-rectangle.Size.X / 2, 0)).X;
            Check(player.GlobalPosition.IsFinite() && body.GlobalPosition.X + capsule.Radius <= ceilingLeft - 2f,
                "do not drive into Grip9's upper side block");
        }
        void Log(string edge) => GD.Print($"SPLIT_GALLERY_RETURN {edge} physics={Engine.GetPhysicsFrames()} " +
            $"actual={Engine.GetPhysicsFrames()-start}/{budget} phase={phase} player={playerId} " +
            $"position={player.GlobalPosition} velocity={player.Velocity} hp={hp}->{health.CurrentHealth} " +
            $"jumps={jumps}->{motor.AcceptedJumpCount} dashes={dashes}->{motor.AcceptedDashCount} " +
            $"gallery_full={Supported(gallery,galleryShape)} barrier_full={Supported(barrier,barrierShape)} " +
            $"sample={input.CurrentFrameSequence} move={input.CurrentFrame.MoveAxis} " +
            $"jump={input.CurrentFrame.JumpPressed}/{input.CurrentFrame.JumpHeld} min_feet={minimumFeet:R}");
        async Task Tick()
        {
            Guard();
            Check(Engine.GetPhysicsFrames()-start < budget, "reject before an extra physics wait");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            observe(); // Original monitor/evidence/diagnostics and any existing scoped guard.
            Guard();
            minimumFeet = MathF.Min(minimumFeet, Feet());
            if (!jumpSeen && motor.AcceptedJumpCount == jumps + 1) { jumpSeen = true; Log("accepted-jump"); }
            if (!contactSeen && Supported(gallery, galleryShape)) { contactSeen = true; Log("first-gallery-contact"); }
        }
        Guard();
        Check(Supported(barrier, barrierShape) &&
            TryTask10CapsuleCorridor(player, galleryShape, out _, out float right, out float left, out _) == false &&
            right <= left, "actual complete Barrier support left of Gallery, not an arbitrary rescue origin");
        Log("entry");
        try
        {
            phase = "release-and-settle";
            ulong sequence = input.CurrentFrameSequence;
            ReleaseAll(player.GlobalPosition);
            while (input.CurrentFrameSequence <= sequence || input.CurrentFrame.JumpPressed ||
                input.CurrentFrame.JumpHeld || input.CurrentFrame.MoveAxis != 0f || !Stationary())
            {
                await Tick();
                Check(Supported(barrier, barrierShape), "neutral preparation retains actual Barrier support");
            }
            Check(motor.AcceptedJumpCount == jumps && !Input.IsPhysicalKeyPressed(Key.Space),
                "old Space is really released before the single grounded edge");
            phase = "ordinary-right-jump";
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.Jump, true);
            Log("single-edge-issued");
            while (!Supported(gallery, galleryShape)) await Tick();
            Check(jumpSeen && motor.AcceptedJumpCount == jumps + 1,
                "actual Gallery landing follows exactly one accepted ordinary jump");
            phase = "release-on-gallery";
            sequence = input.CurrentFrameSequence;
            ReleaseAll(player.GlobalPosition);
            while (input.CurrentFrameSequence <= sequence || input.CurrentFrame.JumpPressed ||
                input.CurrentFrame.JumpHeld || input.CurrentFrame.MoveAxis != 0f || !Stationary())
            {
                await Tick();
                Check(Supported(gallery, galleryShape), "neutral braking retains full actual Gallery contact");
            }
            Guard();
            Check(Supported(gallery, galleryShape) && motor.AcceptedJumpCount == jumps + 1,
                "final fresh neutral full Gallery, no second jump or correction");
            Log("complete");
        }
        finally { ReleaseAll(player.GlobalPosition); }
    }
}
