using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildLowerBuriedMachinery : SceneTree
{
    public override void _Initialize()
    {
        Rect2 bounds = new(-640, -360, 2400, 720);
        var room = new PrologueRoom { Name = "LowerBuriedMachinery", RoomId = PrologueRoomId.LowerBuriedMachinery, CameraBounds = bounds };
        room.AddChild(PrologueBuildParts.Presentation("LOWER BURIED MACHINERY", bounds, new Color("17130f"), new Color("413a2b")));
        var geometry = new Node2D { Name = "Geometry" }; room.AddChild(geometry);
        PrologueBuildParts.Platform(geometry, "EntryFloor", new Vector2(-350, 250), new Vector2(600, 40), new Color("4c4938"));
        PrologueBuildParts.Platform(geometry, "ArenaFloor", new Vector2(600, 250), new Vector2(1260, 40), new Color("5e5940"));
        PrologueBuildParts.Platform(geometry, "DodgeShelf", new Vector2(520, 80), new Vector2(220, 26), new Color("766b49"));
        PrologueBuildParts.Platform(geometry, "SupplyShelf", new Vector2(1020, 20), new Vector2(240, 26), new Color("766b49"));
        PrologueBuildParts.Platform(geometry, "MechanismLanding", new Vector2(1450, 250), new Vector2(540, 40), new Color("4c4938"));
        geometry.AddChild(Surface("Ice", new Vector2(280, -30), GrappleSurfaceMaterial.Ice));
        PrologueBuildParts.AddFormalArt(room, geometry, bounds);
        var traversal = new Node2D { Name = "TraversalContracts" };
        room.AddChild(traversal);
        PrologueBuildParts.TraversalSegment(traversal, "entry_to_arena",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(-186, 182, 96, 48), new Rect2(-30, 182, 96, 48));
        var entrances = new Node2D { Name = "Entrances" }; room.AddChild(entrances);
        PrologueBuildParts.Entrance(entrances, "from_corridor", new Vector2(-540, 198));
        PrologueBuildParts.Entrance(entrances, "from_mechanism", new Vector2(1480, 198));
        var exits = new Node2D { Name = "Exits" }; room.AddChild(exits);
        PrologueBuildParts.Exit(exits, "ToCorridor", "to_corridor", new Vector2(-610, 190), new Color("79d7ac"));
        PrologueBuildParts.Exit(exits, "ToMechanism", "to_mechanism", new Vector2(1690, 190), new Color("e0bd69"));

        var spawnMarkers = new Node2D { Name = "SpawnMarkers" }; room.AddChild(spawnMarkers);
        PrologueBuildParts.Entrance(spawnMarkers, "Ground", new Vector2(520, 198));
        PrologueBuildParts.Entrance(spawnMarkers, "Air", new Vector2(920, -80));
        room.AddChild(new Node2D { Name = "Actors" });
        room.AddChild(new EncounterController
        {
            Name = "EncounterController", ActorRootPath = new NodePath("../Actors"), AutoActivate = true,
            Definition = new EncounterDefinition
            {
                EncounterId = "lower_mixed",
                Spawns = new Godot.Collections.Array<EncounterSpawn>
                {
                    new() { SpawnId = "ground", MarkerPath = new NodePath("../SpawnMarkers/Ground"),
                        Scene = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn") },
                    new() { SpawnId = "air", MarkerPath = new NodePath("../SpawnMarkers/Air"),
                        Scene = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn") }
                }
            }
        });
        var tutorials = new Node2D { Name = "Tutorials" }; room.AddChild(tutorials);
        PrologueBuildParts.Tutorial(tutorials, "ThreadEnemy", "thread_enemy", "E + LEFT MOUSE  PULL LIGHT ENEMY",
            new Vector2(280, 90), new Vector2(260, 240));
        PrologueBuildParts.KillZone(room, new Rect2(-720, 500, 2600, 120));
        Save(room);
    }

    private void Save(PrologueRoom room)
    {
        const string path = "res://scenes/prologue/rooms/LowerBuriedMachinery.tscn";
        Error error = SceneBuildUtil.PackAndSave(room, path); room.Free();
        if (error != Error.Ok) { GD.PushError($"BuildLowerBuriedMachinery failed: {error}"); Quit(1); return; }
        PrologueRoom copy = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
        bool valid = copy.ValidateContract("from_corridor").Count == 0 &&
            copy.GetNodeOrNull<EncounterController>("EncounterController")?.Definition?.Validate().Count == 0 &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/Ice")?.Material == GrappleSurfaceMaterial.Ice;
        copy.Free();
        if (!valid) { GD.PushError("BuildLowerBuriedMachinery contract validation failed."); Quit(1); return; }
        GD.Print("BuildLowerBuriedMachinery: packed and validated."); Quit(0);
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
