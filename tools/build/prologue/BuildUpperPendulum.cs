using Godot;
using Knight.Abilities;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildUpperPendulum : SceneTree
{
    public override void _Initialize()
    {
        Rect2 bounds = new(-640, -720, 2400, 1080);
        var room = new PrologueRoom { Name = "UpperPendulum", RoomId = PrologueRoomId.UpperPendulum, CameraBounds = bounds };
        room.AddChild(PrologueBuildParts.Presentation("UPPER PENDULUM", bounds, new Color("0b1720"), new Color("243843")));
        var geometry = new Node2D { Name = "Geometry" }; room.AddChild(geometry);
        PrologueBuildParts.Platform(geometry, "EntryFloor", new Vector2(-350, 250), new Vector2(600, 40), new Color("304c50"));
        PrologueBuildParts.Platform(geometry, "LowerSwing", new Vector2(150, 210), new Vector2(220, 28), new Color("3a5f61"));
        PrologueBuildParts.Platform(geometry, "MidSwing", new Vector2(400, 170), new Vector2(230, 28), new Color("446e6a"));
        PrologueBuildParts.Platform(geometry, "HiddenShelf", new Vector2(930, 20), new Vector2(260, 28), new Color("395450"));
        PrologueBuildParts.Platform(geometry, "HighSwing", new Vector2(620, 120), new Vector2(250, 28), new Color("527d70"));
        PrologueBuildParts.Platform(geometry, "MechanismLanding", new Vector2(970, 120), new Vector2(520, 40), new Color("304c50"));
        ThreadTarget2D anchorLow = PrologueBuildParts.Anchor(geometry, "AnchorLow", new Vector2(170, -90));
        ThreadTarget2D anchorHigh = PrologueBuildParts.Anchor(geometry, "AnchorHigh", new Vector2(360, -50));
        ThreadTarget2D reboundTarget = PrologueBuildParts.Anchor(geometry, "ReboundTarget", new Vector2(580, -60));
        foreach (ThreadTarget2D anchor in new[] { anchorLow, anchorHigh, reboundTarget })
        {
            anchor.Material = GrappleSurfaceMaterial.DedicatedAnchor;
            anchor.AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius;
        }
        PrologueBuildParts.AddFormalArt(room, geometry, bounds);
        var traversal = new Node2D { Name = "TraversalContracts" };
        room.AddChild(traversal);
        PrologueBuildParts.TraversalSegment(traversal, "entry_to_lower_swing",
            TraversalSegmentKind.Assisted, TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(-166, 134, 96, 96), new Rect2(70, 148, 160, 48),
            new NodePath("Geometry/AnchorLow"));
        PrologueBuildParts.TraversalSegment(traversal, "lower_to_mid_swing",
            TraversalSegmentKind.Assisted, TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(104, 148, 96, 48), new Rect2(310, 108, 180, 48),
            new NodePath("Geometry/AnchorHigh"));
        PrologueBuildParts.TraversalSegment(traversal, "mid_to_high_swing",
            TraversalSegmentKind.Assisted, TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(344, 108, 96, 48), new Rect2(520, 58, 180, 48),
            new NodePath("Geometry/ReboundTarget"));
        PrologueBuildParts.TraversalSegment(traversal, "high_to_mechanism_landing",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(544, 58, 96, 48), new Rect2(710, 52, 100, 48));

        var entrances = new Node2D { Name = "Entrances" }; room.AddChild(entrances);
        PrologueBuildParts.Entrance(entrances, "from_corridor", new Vector2(-540, 198));
        PrologueBuildParts.Entrance(entrances, "from_mechanism", new Vector2(970, 68));
        var exits = new Node2D { Name = "Exits" }; room.AddChild(exits);
        PrologueBuildParts.Exit(exits, "ToCorridor", "to_corridor", new Vector2(-610, 190), new Color("79d7ac"));
        PrologueBuildParts.Exit(exits, "ToMechanism", "to_mechanism", new Vector2(1210, 60), new Color("e0bd69"));

        var spawnMarkers = new Node2D { Name = "SpawnMarkers" }; room.AddChild(spawnMarkers);
        PrologueBuildParts.Entrance(spawnMarkers, "Air", new Vector2(900, -200));
        room.AddChild(new Node2D { Name = "Actors" });
        room.AddChild(new EncounterController
        {
            Name = "EncounterController", ActorRootPath = new NodePath("../Actors"), AutoActivate = true,
            Definition = new EncounterDefinition
            {
                EncounterId = "upper_moth",
                Spawns = new Godot.Collections.Array<EncounterSpawn>
                {
                    new() { SpawnId = "air", MarkerPath = new NodePath("../SpawnMarkers/Air"),
                        Scene = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn") }
                }
            }
        });
        var hidden = new HiddenAlcoveTrigger
        {
            Name = "HiddenAlcove", DiscoveryId = "upper_hidden_alcove", Position = new Vector2(930, -38),
            CollisionLayer = 0, CollisionMask = Knight.Combat.CombatCollisionLayers.Player, Monitoring = true
        };
        hidden.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(180, 100) } });
        room.AddChild(hidden);
        PrologueBuildParts.KillZone(room, new Rect2(-720, 500, 2600, 120));
        Save(room);
    }

    private void Save(PrologueRoom room)
    {
        const string path = "res://scenes/prologue/rooms/UpperPendulum.tscn";
        Error error = SceneBuildUtil.PackAndSave(room, path); room.Free();
        if (error != Error.Ok) { GD.PushError($"BuildUpperPendulum failed: {error}"); Quit(1); return; }
        PrologueRoom copy = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
        bool valid = copy.ValidateContract("from_corridor").Count == 0 &&
            copy.GetNodeOrNull<EncounterController>("EncounterController")?.Definition?.Validate().Count == 0 &&
            copy.GetNodeOrNull<HiddenAlcoveTrigger>("HiddenAlcove") != null &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/AnchorLow")?.Material ==
                GrappleSurfaceMaterial.DedicatedAnchor &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/AnchorHigh")?.Material ==
                GrappleSurfaceMaterial.DedicatedAnchor &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/ReboundTarget")?.Material ==
                GrappleSurfaceMaterial.DedicatedAnchor;
        copy.Free();
        if (!valid) { GD.PushError("BuildUpperPendulum contract validation failed."); Quit(1); return; }
        GD.Print("BuildUpperPendulum: packed and validated."); Quit(0);
    }
}
