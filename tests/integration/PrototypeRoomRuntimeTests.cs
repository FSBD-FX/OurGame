using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Shared;
using Knight.Vfx;
using Knight.World;

namespace Knight.Tests.Integration;

public static class PrototypeRoomRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await EnemyRayTreatsThePlayerAsVisibleAndAWorldWallAsOccluding(tree);
        await GeneratedPlayerUsesTheMechanicalClawOnly(tree);
        await ActiveMechanicalClawFallResetClearsRuntimeWithoutDamage(tree);
        await DeathRestoresTheWholeLocalEncounterAcrossOnePhysicsFrame(tree);
    }

    private static async Task ActiveMechanicalClawFallResetClearsRuntimeWithoutDamage(SceneTree tree)
    {
        PrototypeRoomController room = GD.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn")
            .Instantiate<PrototypeRoomController>();
        tree.Root.AddChild(room);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        CharacterBody2D player = room.GetNode<CharacterBody2D>("Player");
        GrappleSurface2D target = room.GetNode<GrappleSurface2D>("Geometry/ThreadAnchor");
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        ThreadLineVisual line = player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        ThreadShuttleVisual shuttle = player.GetNode<ThreadShuttleVisual>("VisualRoot/MechanicalClaw");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        motor.SetPhysicsProcess(false);
        player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        player.GetNode<PlayerEnergyController>("Energy").SetPhysicsProcess(false);
        player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        player.GlobalPosition = target.WorldAnchor + new Vector2(-180f, 0f);

        StepClaw(input, modes, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: target.WorldAnchor));
        for (int index = 0; index < 120 && claw.CurrentFrame.State != MechanicalClawState.Latched; index++)
            StepClaw(input, modes, claw,
                ClawInput(primaryHeld: true, mouseWorld: target.WorldAnchor));
        TestAssert.Equal(MechanicalClawState.Latched, claw.CurrentFrame.State,
            "fall-reset setup has an active mechanical claw attached to the prototype anchor");
        TestAssert.True(line.ActiveStrandCount > 0 && !shuttle.IsClawClear,
            "fall-reset setup has live claw presentation before crossing the kill zone");

        int damagedEvents = 0;
        health.Damaged += OnDamaged;
        player.GlobalPosition = room.GetNode<Area2D>("KillZone").GlobalPosition;
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        health.Damaged -= OnDamaged;

        TestAssert.Equal(0, damagedEvents,
            "prototype fall recovery remains a no-damage reset path");
        TestAssert.Equal(MechanicalClawState.Idle, claw.CurrentFrame.State,
            "prototype fall recovery resets the active mechanical claw");
        TestAssert.True(claw.CurrentTarget == null && motor.GrappleMovementScale == 1f,
            "prototype fall recovery clears the old target and motor constraint");
        TestAssert.Equal(0, line.ActiveStrandCount,
            "prototype fall recovery leaves no old world rope");
        TestAssert.True(shuttle.IsClawClear,
            "prototype fall recovery leaves no old claw shuttle");
        TestAssert.True(claw.IsPhysicsProcessing() && motor.IsPhysicsProcessing(),
            "prototype fall recovery resumes claw and motor scheduling after the reset frame");

        room.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        void OnDamaged(int remaining, Vector2 impulse) => damagedEvents++;
    }

    private static async Task GeneratedPlayerUsesTheMechanicalClawOnly(SceneTree tree)
    {
        PrototypeRoomController room = GD.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn")
            .Instantiate<PrototypeRoomController>();
        tree.Root.AddChild(room);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        CharacterBody2D player = room.GetNode<CharacterBody2D>("Player");
        TestAssert.True(player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw") != null,
            "the real prototype instance mounts the one-live mechanical claw runtime");
        TestAssert.True(player.GetNodeOrNull<ThreadAbility>("ThreadAbility") == null,
            "the generated main player never mounts the legacy cooldown ability");
        TestAssert.True(player.GetNodeOrNull<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line") != null,
            "the real prototype instance preserves the stable nested claw visual path");

        room.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task EnemyRayTreatsThePlayerAsVisibleAndAWorldWallAsOccluding(SceneTree tree)
    {
        var testRoot = new Node2D { Name = "EnemySightPhysicsTest" };
        RustShellSquire enemy = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        enemy.Position = Vector2.Zero;
        enemy.Gravity = 0f;
        enemy.CollisionLayer = 1u << 2;
        enemy.CollisionMask = 1u << 0;
        var player = new CharacterBody2D
        {
            Name = "PlayerTarget",
            Position = new Vector2(180f, 0f),
            CollisionLayer = 1u << 1,
            CollisionMask = 1u << 0
        };
        player.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(32f, 64f) }
        });
        player.AddToGroup("player");
        testRoot.AddChild(enemy);
        testRoot.AddChild(player);
        tree.Root.AddChild(testRoot);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

        PhysicsRayQueryParameters2D rawQuery = PhysicsRayQueryParameters2D.Create(enemy.GlobalPosition, player.GlobalPosition);
        rawQuery.Exclude = new Godot.Collections.Array<Rid> { enemy.GetRid() };
        Godot.Collections.Dictionary rawHit = enemy.GetWorld2D().DirectSpaceState.IntersectRay(rawQuery);
        TestAssert.True(rawHit.Count > 0 && rawHit["collider"].AsGodotObject() == player,
            "the real physics ray first hits the player body when unobstructed");
        TestAssert.True(enemy.HasLineOfSightTo(player), "hitting the target player is visible, not an occluder");
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.Equal(RustShellState.Chase, enemy.CurrentState,
            "real unobstructed sight advances the enemy from idle into chase");

        var wall = new StaticBody2D { Name = "WorldWall", Position = new Vector2(90f, 0f), CollisionLayer = 1u << 0 };
        wall.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(20f, 120f) }
        });
        testRoot.AddChild(wall);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(!enemy.HasLineOfSightTo(player), "a world body before the player occludes sight");
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.Equal(RustShellState.Idle, enemy.CurrentState,
            "world occlusion returns the enemy to idle");

        testRoot.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task DeathRestoresTheWholeLocalEncounterAcrossOnePhysicsFrame(SceneTree tree)
    {
        PrototypeRoomController room = GD.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn")
            .Instantiate<PrototypeRoomController>();
        tree.Root.AddChild(room);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        CharacterBody2D player = room.GetNode<CharacterBody2D>("Player");
        RustShellSquire enemy = room.GetNode<RustShellSquire>("Actors/RustShellSquire");
        HealthComponent playerHealth = player.GetNode<HealthComponent>("Health");
        HealthComponent enemyHealth = enemy.GetNode<HealthComponent>("Health");
        Vector2 safePosition = player.GlobalPosition;
        Vector2 enemySafePosition = enemy.GlobalPosition;

        player.GlobalPosition = new Vector2(300f, 400f);
        player.GetNode<PlayerMotor>("Motor").ExternalImpulse(new Vector2(350f, -220f));
        player.GetNode<Hitbox>("Combat/AttackHitbox").Begin(401UL);
        player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line")
            .Points = [Vector2.Zero, Vector2.Right * 100f];
        enemy.ApplyThreadPull(new Vector2(-300f, -80f));
        enemyHealth.ApplyDamage(new DamageRequest(1, Vector2.Right, 80f, 402UL));

        playerHealth.ApplyDamage(new DamageRequest(playerHealth.MaxHealth, Vector2.Down, 100f, 403UL));
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        TestAssert.True(player.GlobalPosition.DistanceTo(safePosition) < 1.1f,
            "death returns player to the local checkpoint within one physics integration step");
        TestAssert.True(player.Velocity.Length() < 61f,
            "death clears prior player velocity before normal gravity resumes");
        TestAssert.Equal(playerHealth.MaxHealth, playerHealth.CurrentHealth, "death restores player health");
        TestAssert.True(!player.GetNode<Hitbox>("Combat/AttackHitbox").Monitoring,
            "death closes the active combat hitbox");
        TestAssert.Equal(0, player.GetNode<ThreadLineVisual>(
                "VisualRoot/MechanicalClaw/Line").Points.Length,
            "death clears the active mechanical-claw presentation");
        TestAssert.True(player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing() &&
            player.GetNode<PlayerCombat>("Combat").IsPhysicsProcessing() &&
            player.GetNode("MechanicalClaw").IsPhysicsProcessing(),
            "player motion, combat and mechanical-claw processing resume after one physics frame");
        TestAssert.True(enemy.GlobalPosition.DistanceTo(enemySafePosition) < 1.1f,
            "death restores enemy checkpoint position within one physics integration step");
        TestAssert.True(enemy.Velocity.Length() < 61f, "death clears enemy pull impulse before normal gravity resumes");
        TestAssert.Equal(enemyHealth.MaxHealth, enemyHealth.CurrentHealth, "death restores enemy health");
        TestAssert.Equal(RustShellState.Idle, enemy.CurrentState, "death restores the enemy state machine");

        room.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void StepClaw(PlayerInputReader input, PlayerActionModeController modes,
        MechanicalClawController claw, PlayerInputFrame frame)
    {
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);
        modes._PhysicsProcess(1f / 60f);
        claw._PhysicsProcess(1f / 60f);
    }

    private static PlayerInputFrame ClawInput(bool grappleModePressed = false,
        bool primaryPressed = false, bool primaryHeld = false, Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, false, false, false, false, false,
            MouseWorld: mouseWorld,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld);
}
