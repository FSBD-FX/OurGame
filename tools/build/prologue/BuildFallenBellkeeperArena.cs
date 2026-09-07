using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildFallenBellkeeperArena : SceneTree
{
    public override void _Initialize()
    {
        Rect2 bounds = new(-640, -360, 1920, 720);
        var room = new PrologueRoom
        {
            Name = "FallenBellkeeperArena",
            RoomId = PrologueRoomId.FallenBellkeeperArena,
            CameraBounds = bounds
        };
        room.AddChild(PrologueBuildParts.Presentation("THE FALLEN BELLKEEPER", bounds,
            new Color("100f18"), new Color("302536")));
        var geometry = new Node2D { Name = "Geometry" };
        room.AddChild(geometry);
        PrologueBuildParts.Platform(geometry, "ArenaFloor", new Vector2(260, 290),
            new Vector2(1800, 40), new Color("55455c"));
        PrologueBuildParts.Platform(geometry, "LeftDais", new Vector2(-360, 190),
            new Vector2(260, 24), new Color("675268"));
        PrologueBuildParts.Platform(geometry, "RightDais", new Vector2(900, 190),
            new Vector2(260, 24), new Color("675268"));
        geometry.AddChild(Surface("Forbidden", new Vector2(450, -100), GrappleSurfaceMaterial.Forbidden));
        PrologueBuildParts.AddFormalArt(room, geometry, bounds);
        var traversal = new Node2D { Name = "TraversalContracts" };
        room.AddChild(traversal);
        PrologueBuildParts.TraversalSegment(traversal, "retry_to_arena_floor",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(-374, 130, 96, 48), new Rect2(-182, 222, 96, 48));
        var entrances = new Node2D { Name = "Entrances" };
        room.AddChild(entrances);
        PrologueBuildParts.Entrance(entrances, "from_mechanism", new Vector2(-500, 138));
        PrologueBuildParts.Entrance(entrances, "boss_retry", new Vector2(-320, 138));
        PrologueBuildParts.Entrance(entrances, "after_boss", new Vector2(900, 138));
        var exits = new Node2D { Name = "Exits" };
        room.AddChild(exits);
        PrologueBuildParts.Exit(exits, "BossComplete", "boss_complete", new Vector2(1110, 218),
            new Color("e6ca74"));

        var doors = new Node2D { Name = "Doors" };
        room.AddChild(doors);
        doors.AddChild(Door("Entry", new Vector2(-130, 198), new Color("ad8747")));
        doors.AddChild(Door("Exit", new Vector2(1010, 198), new Color("ad8747")));
        var bossRoot = new Node2D { Name = "BossRoot" };
        room.AddChild(bossRoot);
        var trigger = new Area2D
        {
            Name = "ActivationTrigger",
            Position = new Vector2(-70, 180),
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        trigger.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(90, 180) }
        });
        room.AddChild(trigger);
        CheckpointTrigger checkpoint = PrologueBuildParts.Checkpoint(room, new Vector2(-320, 138), "boss_retry");
        checkpoint.Name = "BossCheckpoint";
        room.AddChild(new BossArenaController
        {
            Name = "BossArenaController",
            BossScene = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn"),
            BossSpawnPosition = new Vector2(430, 188)
        });
        PrologueBuildParts.KillZone(room, new Rect2(-720, 500, 2200, 120));

        const string path = "res://scenes/prologue/rooms/FallenBellkeeperArena.tscn";
        Error error = SceneBuildUtil.PackAndSave(room, path);
        room.Free();
        if (error != Error.Ok) { GD.PushError($"BuildFallenBellkeeperArena failed: {error}"); Quit(1); return; }
        PrologueRoom copy = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
        bool valid = copy.ValidateContract("from_mechanism").Count == 0 &&
            copy.ValidateContract("boss_retry").Count == 0 &&
            copy.GetNodeOrNull<BossArenaController>("BossArenaController") != null &&
            copy.GetNodeOrNull<CheckpointTrigger>("BossCheckpoint") != null &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/Forbidden")?.Material ==
                GrappleSurfaceMaterial.Forbidden;
        copy.Free();
        if (!valid) { GD.PushError("BuildFallenBellkeeperArena contract validation failed."); Quit(1); return; }
        GD.Print("BuildFallenBellkeeperArena: packed and validated.");
        Quit(0);
    }

    private static GrappleSurface2D Surface(string name, Vector2 position,
        GrappleSurfaceMaterial material)
    {
        var surface = new GrappleSurface2D { Name = name, Position = position, Material = material };
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
            Name = "Collision", Shape = new CircleShape2D { Radius = 25f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static StaticBody2D Door(string name, Vector2 position, Color color)
    {
        var door = new StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(40, 190) }
        });
        PackedScene scene = GD.Load<PackedScene>("res://scenes/presentation/ShortcutDoorVisual.tscn");
        if (scene == null) throw new System.InvalidOperationException(
            "ShortcutDoorVisual must be built before the Boss arena.");
        PrologueMechanismVisual visual = scene.Instantiate<PrologueMechanismVisual>();
        visual.Name = "Visual";
        door.AddChild(visual);
        return door;
    }
}
