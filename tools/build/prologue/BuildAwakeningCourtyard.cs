using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildAwakeningCourtyard : SceneTree
{
    public override void _Initialize()
    {
        Rect2 bounds = new(-640, -360, 1920, 720);
        var room = new PrologueRoom
        {
            Name = "AwakeningCourtyard",
            RoomId = PrologueRoomId.AwakeningCourtyard,
            CameraBounds = bounds
        };
        room.AddChild(PrologueBuildParts.Presentation("AWAKENING COURTYARD", bounds,
            new Color("111923"), new Color("1e3036")));
        var geometry = new Node2D { Name = "Geometry" };
        room.AddChild(geometry);
        PrologueBuildParts.Platform(geometry, "SpawnFloor", new Vector2(-360, 250), new Vector2(560, 40), new Color("314c43"));
        PrologueBuildParts.Platform(geometry, "JumpStep", new Vector2(40, 165), new Vector2(180, 30), new Color("35564a"));
        PrologueBuildParts.Platform(geometry, "AttackLedge", new Vector2(255, 120), new Vector2(260, 30), new Color("3a5e50"));
        PrologueBuildParts.Platform(geometry, "ExitFloor", new Vector2(730, 250), new Vector2(650, 40), new Color("314c43"));
        geometry.AddChild(Surface("GripStone", new Vector2(520, -80), GrappleSurfaceMaterial.GripStone));
        PrologueBuildParts.AddFormalArt(room, geometry, bounds);
        var traversal = new Node2D { Name = "TraversalContracts" };
        room.AddChild(traversal);
        PrologueBuildParts.TraversalSegment(traversal, "spawn_to_jump_step",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(-196, 182, 96, 48), new Rect2(-8, 102, 96, 48));
        PrologueBuildParts.TraversalSegment(traversal, "jump_step_to_attack_ledge",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(14, 102, 96, 48), new Rect2(145, 57, 96, 48));

        var entrances = new Node2D { Name = "Entrances" };
        room.AddChild(entrances);
        PrologueBuildParts.Entrance(entrances, "start", new Vector2(-540, 198));
        PrologueBuildParts.Entrance(entrances, "from_corridor", new Vector2(1000, 198));
        var exits = new Node2D { Name = "Exits" };
        room.AddChild(exits);
        PrologueBuildParts.Exit(exits, "ToCorridor", "to_corridor", new Vector2(1040, 190), new Color("75d89b"));
        var tutorials = new Node2D { Name = "Tutorials" };
        room.AddChild(tutorials);
        PrologueBuildParts.Tutorial(tutorials, "Move", "move", "A / D  MOVE", new Vector2(-450, 180), new Vector2(180, 130));
        PrologueBuildParts.Tutorial(tutorials, "Jump", "jump", "SPACE  JUMP", new Vector2(-60, 120), new Vector2(180, 170));
        PrologueBuildParts.Tutorial(tutorials, "Attack", "attack", "J  ATTACK", new Vector2(420, 45), new Vector2(220, 160));
        PrologueBuildParts.KillZone(room, new Rect2(-720, 500, 2100, 100));

        Save(room, "res://scenes/prologue/rooms/AwakeningCourtyard.tscn");
    }

    private void Save(PrologueRoom room, string path)
    {
        Error error = SceneBuildUtil.PackAndSave(room, path);
        room.Free();
        if (error != Error.Ok) { GD.PushError($"BuildAwakeningCourtyard failed: {error}"); Quit(1); return; }
        PrologueRoom copy = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
        bool valid = copy.ValidateContract("start").Count == 0 &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/GripStone")?.Material ==
                GrappleSurfaceMaterial.GripStone;
        copy.Free();
        if (!valid) { GD.PushError("BuildAwakeningCourtyard contract validation failed."); Quit(1); return; }
        GD.Print("BuildAwakeningCourtyard: packed and validated.");
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
}
