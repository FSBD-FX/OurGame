using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tools;
using Knight.Mechanisms;

namespace Knight.Build.Prologue;

public partial class BuildBellChainRoom : SceneTree
{
    public override void _Initialize()
    {
        Rect2 bounds = new(-640, -360, 1920, 720);
        var room = new PrologueRoom { Name = "BellChainMechanism", RoomId = PrologueRoomId.BellChainMechanism, CameraBounds = bounds };
        room.AddChild(PrologueBuildParts.Presentation("BELL-CHAIN CONVERGENCE", bounds, new Color("15121c"), new Color("3b3346")));
        var geometry = new Node2D { Name = "Geometry" }; room.AddChild(geometry);
        PrologueBuildParts.Platform(geometry, "LowerFloor", new Vector2(220, 290), new Vector2(1720, 40), new Color("51465b"));
        PrologueBuildParts.Platform(geometry, "UpperArrival", new Vector2(-330, 160), new Vector2(420, 28), new Color("6c5a70"));
        PrologueBuildParts.Platform(geometry, "CompletionDais", new Vector2(900, 200), new Vector2(320, 28), new Color("7d685f"));
        geometry.AddChild(Surface("TempleMetal", new Vector2(300, -40), GrappleSurfaceMaterial.TempleMetal));
        PrologueBuildParts.AddFormalArt(room, geometry, bounds);
        var traversal = new Node2D { Name = "TraversalContracts" };
        room.AddChild(traversal);
        PrologueBuildParts.TraversalSegment(traversal, "arrival_to_chain_floor",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(-256, 98, 96, 48), new Rect2(-72, 222, 96, 48));
        PrologueBuildParts.TraversalSegment(traversal, "chain_floor_to_completion_dais",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(636, 222, 48, 48), new Rect2(740, 138, 96, 48));
        var entrances = new Node2D { Name = "Entrances" }; room.AddChild(entrances);
        PrologueBuildParts.Entrance(entrances, "from_upper", new Vector2(-500, 108));
        PrologueBuildParts.Entrance(entrances, "from_lower", new Vector2(-500, 238));
        PrologueBuildParts.Entrance(entrances, "p1_complete", new Vector2(900, 128));
        var exits = new Node2D { Name = "Exits" }; room.AddChild(exits);
        PrologueBuildParts.Exit(exits, "ToUpper", "to_upper", new Vector2(-610, 100), new Color("75ced8"));
        PrologueBuildParts.Exit(exits, "ToLower", "to_lower", new Vector2(-610, 250), new Color("b5a86a"));
        PrologueBuildParts.Exit(exits, "P1Complete", "p1_complete", new Vector2(1120, 220), new Color("e0bd69"));
        PrologueBuildParts.Exit(exits, "Shortcut", "shortcut", new Vector2(1030, 220), new Color("62c5b5"));
        var actors = new Node2D { Name = "Actors" }; room.AddChild(actors);
        var markers = new Node2D { Name = "SpawnMarkers" }; room.AddChild(markers);
        PrologueBuildParts.Entrance(markers, "Laborer", new Vector2(430, 198));
        room.AddChild(new EncounterController
        {
            Name = "EncounterController", ActorRootPath = new NodePath("../Actors"), AutoActivate = true,
            Definition = new EncounterDefinition
            {
                EncounterId = "bell_back_laborer",
                Spawns = new Godot.Collections.Array<EncounterSpawn>
                {
                    new() { SpawnId = "laborer", MarkerPath = new NodePath("../SpawnMarkers/Laborer"),
                        Scene = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn") }
                }
            }
        });
        var mechanisms = new Node2D { Name = "Mechanisms" }; room.AddChild(mechanisms);
        BellChainMechanism chain = GD.Load<PackedScene>("res://scenes/mechanisms/BellChainMechanism.tscn")
            .Instantiate<BellChainMechanism>();
        chain.Name = "BellChain"; chain.Position = new Vector2(720, 198); mechanisms.AddChild(chain);
        PrologueBuildParts.KillZone(room, new Rect2(-720, 500, 2200, 120));
        Save(room);
    }

    private void Save(PrologueRoom room)
    {
        const string path = "res://scenes/prologue/rooms/BellChainMechanism.tscn";
        Error error = SceneBuildUtil.PackAndSave(room, path); room.Free();
        if (error != Error.Ok) { GD.PushError($"BuildBellChainRoom failed: {error}"); Quit(1); return; }
        PrologueRoom copy = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
        bool valid = copy.ValidateContract("from_upper").Count == 0 && copy.ValidateContract("from_lower").Count == 0 &&
            copy.GetNodeOrNull<EncounterController>("EncounterController")?.Definition?.Validate().Count == 0 &&
            copy.GetNodeOrNull<BellChainMechanism>("Mechanisms/BellChain") != null &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/TempleMetal")?.Material ==
                GrappleSurfaceMaterial.TempleMetal;
        copy.Free();
        if (!valid) { GD.PushError("BuildBellChainRoom contract validation failed."); Quit(1); return; }
        GD.Print("BuildBellChainRoom: packed and validated."); Quit(0);
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
