using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.World;

namespace Knight.Tests.Unit;

public static class PrototypeRoomStructureTests
{
    public static void Run()
    {
        GeneratedRoomContainsThePlayableRouteContracts();
        CollisionContractsUseSingleBitLayersAndIntentionalMasks();
        RouteGeometryMatchesTheAuthoredCoordinates();
    }

    private static void GeneratedRoomContainsThePlayableRouteContracts()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn");
        TestAssert.True(scene != null, "the generated prototype room scene exists");
        Node room = scene.Instantiate();

        TestAssert.True(room.GetNodeOrNull<CharacterBody2D>("Player") != null,
            "room instances the generated player leaf scene");
        TestAssert.True(room.GetNodeOrNull<CharacterBody2D>("Actors/RustShellSquire") != null,
            "room instances the generated Rust-Shell Squire leaf scene");
        TestAssert.True(room.GetNodeOrNull<Node2D>("Geometry/ThreadAnchor") != null,
            "room exposes a thread anchor across the gap");
        TestAssert.True(room.GetNodeOrNull<Area2D>("Checkpoint") != null,
            "room exposes a checkpoint trigger before the gap");
        TestAssert.True(room.GetNodeOrNull<Area2D>("KillZone") != null,
            "room exposes an observable fall reset zone");
        TestAssert.True(room.GetNodeOrNull<Area2D>("ExitMarker") != null,
            "room exposes an exit marker after the enemy");

        room.Free();
    }

    private static void CollisionContractsUseSingleBitLayersAndIntentionalMasks()
    {
        Node room = LoadRoom();
        var player = room.GetNode<CharacterBody2D>("Player");
        var enemy = room.GetNode<CharacterBody2D>("Actors/RustShellSquire");
        var playerHurtbox = player.GetNode<Hurtbox>("Hurtbox");
        var enemyHurtbox = enemy.GetNode<Hurtbox>("Hurtbox");
        var playerAttack = player.GetNode<Hitbox>("Combat/AttackHitbox");
        var enemyAttack = enemy.GetNode<Hitbox>("AttackHitbox");
        var anchorArea = room.GetNode<Area2D>("Geometry/ThreadAnchor/TargetCollision");

        AssertSingleBit(1u << 0, room.GetNode<StaticBody2D>("Geometry/SpawnPlatform").CollisionLayer, "world");
        AssertSingleBit(1u << 1, player.CollisionLayer, "player");
        AssertSingleBit(1u << 2, enemy.CollisionLayer, "enemy");
        AssertSingleBit(1u << 3, playerAttack.CollisionLayer, "player attack");
        AssertSingleBit(1u << 4, enemyAttack.CollisionLayer, "enemy attack");
        AssertSingleBit(1u << 5, anchorArea.CollisionLayer, "thread target");
        TestAssert.Equal(1u << 0, player.CollisionMask, "player body collides only with world");
        TestAssert.Equal(1u << 0, enemy.CollisionMask, "enemy body collides only with world");
        TestAssert.Equal(1u << 1, playerHurtbox.CollisionLayer, "player hurtbox publishes only the player target bit");
        TestAssert.Equal(1u << 2, enemyHurtbox.CollisionLayer, "player attack resolves against enemy-layer hurtboxes");
        TestAssert.Equal(1u << 2, playerAttack.CollisionMask, "player attack observes enemy hurtboxes only");
        TestAssert.Equal(1u << 1, enemyAttack.CollisionMask, "enemy attack observes player hurtboxes only");
        TestAssert.Equal(1u << 1, room.GetNode<Area2D>("Checkpoint").CollisionMask, "checkpoint observes player only");
        TestAssert.Equal(1u << 1, room.GetNode<Area2D>("KillZone").CollisionMask, "kill zone observes player only");
        TestAssert.Equal(1u << 1, room.GetNode<Area2D>("ExitMarker").CollisionMask, "exit observes player only");
        room.Free();
    }

    private static void RouteGeometryMatchesTheAuthoredCoordinates()
    {
        Node room = LoadRoom();
        AssertPlatform(room, "SpawnPlatform", new Vector2(-405f, 250f), new Vector2(310f, 30f));
        AssertPlatform(room, "StepOne", new Vector2(-120f, 170f), new Vector2(120f, 30f));
        AssertPlatform(room, "StepTwo", new Vector2(80f, 80f), new Vector2(120f, 30f));
        AssertPlatform(room, "LandingPlatform", new Vector2(520f, 80f), new Vector2(320f, 30f));
        TestAssert.Equal(new Vector2(260f, -80f), room.GetNode<Node2D>("Geometry/ThreadAnchor").Position,
            "thread anchor marks the authored 260-pixel traversal gap");
        TestAssert.Equal(new Vector2(520f, 28f), room.GetNode<RustShellSquire>("Actors/RustShellSquire").Position,
            "enemy stands on the landing platform");
        TestAssert.True(room.GetNodeOrNull<Hurtbox>("BellTarget/Hurtbox") != null,
            "suspended bell exposes a combat hurtbox for down-attack bounce");
        TestAssert.True(room.GetNodeOrNull<PrototypeHud>("Hud") != null &&
            room.GetNodeOrNull<Label>("Hud/Status") != null, "HUD exposes readable prototype status");
        room.Free();
    }

    private static Node LoadRoom()
    {
        return ResourceLoader.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn").Instantiate();
    }

    private static void AssertSingleBit(uint expected, uint actual, string contract)
    {
        TestAssert.Equal(expected, actual, $"{contract} uses its assigned layer bit");
        TestAssert.True(actual != 0 && (actual & (actual - 1)) == 0, $"{contract} is exactly one collision bit");
    }

    private static void AssertPlatform(Node room, string name, Vector2 expectedPosition, Vector2 expectedSize)
    {
        StaticBody2D body = room.GetNode<StaticBody2D>($"Geometry/{name}");
        var shape = body.GetNode<CollisionShape2D>("Collision").Shape as RectangleShape2D;
        TestAssert.Equal(expectedPosition, body.Position, $"{name} authored position");
        TestAssert.Equal(expectedSize, shape.Size, $"{name} rectangle collision size");
        TestAssert.Equal(1u << 0, body.CollisionLayer, $"{name} belongs only to world");
        TestAssert.Equal(0u, body.CollisionMask, $"{name} does not scan collision layers");
    }
}
