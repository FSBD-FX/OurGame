using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildBrokenToothCorridor : SceneTree
{
    public override void _Initialize()
    {
        Rect2 bounds = new(-640, -360, 2360, 720);
        var room = new PrologueRoom
        {
            Name = "BrokenToothCorridor",
            RoomId = PrologueRoomId.BrokenToothCorridor,
            CameraBounds = bounds
        };
        room.AddChild(PrologueBuildParts.Presentation("BROKEN-TOOTH CORRIDOR", bounds,
            new Color("101821"), new Color("25373d")));
        var geometry = new Node2D { Name = "Geometry" };
        room.AddChild(geometry);
        PrologueBuildParts.Platform(geometry, "EntryFloor", new Vector2(-350, 250), new Vector2(580, 40), new Color("314c43"));
        PrologueBuildParts.Platform(geometry, "TeachingLedge", new Vector2(130, 160), new Vector2(260, 30), new Color("3a5e50"));
        PrologueBuildParts.Platform(geometry, "AnchorLanding", new Vector2(650, 250), new Vector2(400, 40), new Color("426857"));
        PrologueBuildParts.Platform(geometry, "CompletionFloor", new Vector2(1310, 250), new Vector2(600, 40), new Color("314c43"));
        ThreadTarget2D threadAnchor = PrologueBuildParts.Anchor(geometry, "ThreadAnchor", new Vector2(480, -100));
        threadAnchor.Material = GrappleSurfaceMaterial.DedicatedAnchor;
        threadAnchor.AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius;
        PrologueBuildParts.AddFormalArt(room, geometry, bounds);
        var traversal = new Node2D { Name = "TraversalContracts" };
        room.AddChild(traversal);
        PrologueBuildParts.TraversalSegment(traversal, "entry_floor_to_teaching_ledge",
            TraversalSegmentKind.SingleJump, TraversalAbility.Jump,
            new Rect2(-126, 182, 48, 48), new Rect2(40, 97, 96, 48));
        PrologueBuildParts.TraversalSegment(traversal, "entry_to_anchor_landing",
            TraversalSegmentKind.Assisted, TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(140, 97, 96, 48), new Rect2(470, 182, 160, 48),
            new NodePath("Geometry/ThreadAnchor"));

        var entrances = new Node2D { Name = "Entrances" };
        room.AddChild(entrances);
        PrologueBuildParts.Entrance(entrances, "from_courtyard", new Vector2(-560, 198));
        PrologueBuildParts.Entrance(entrances, "from_corridor", new Vector2(-560, 198));
        PrologueBuildParts.Entrance(entrances, "checkpoint", new Vector2(1120, 198));
        PrologueBuildParts.Entrance(entrances, "from_upper", new Vector2(1220, 198));
        PrologueBuildParts.Entrance(entrances, "from_lower", new Vector2(1220, 198));
        var exits = new Node2D { Name = "Exits" };
        room.AddChild(exits);
        PrologueBuildParts.Exit(exits, "ToCourtyard", "to_courtyard", new Vector2(-610, 190), new Color("75d89b"));
        PrologueBuildParts.Exit(exits, "ToUpper", "to_upper", new Vector2(1320, 80), new Color("75ced8"));
        PrologueBuildParts.Exit(exits, "ToLower", "to_lower", new Vector2(1320, 250), new Color("b5a86a"));
        PrologueBuildParts.Exit(exits, "P0Complete", "p0_complete", new Vector2(1570, 190), new Color("d8b968"));

        var actors = new Node2D { Name = "Actors" };
        room.AddChild(actors);
        RustShellSquire enemy = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
        enemy.Name = "RustShellSquire";
        enemy.Position = new Vector2(-80, 198);
        enemy.CollisionLayer = CombatCollisionLayers.Enemy;
        enemy.CollisionMask = CombatCollisionLayers.World;
        actors.AddChild(enemy);

        PrologueBuildParts.Checkpoint(room, new Vector2(1120, 198), "checkpoint");
        var tutorials = new Node2D { Name = "Tutorials" };
        room.AddChild(tutorials);
        PrologueBuildParts.Tutorial(tutorials, "ThreadAnchor", "thread_anchor", "E + LEFT MOUSE  CLAW",
            new Vector2(150, 40), new Vector2(250, 220));
        PrologueBuildParts.KillZone(room, new Rect2(-720, 500, 2550, 100));

        Save(room, "res://scenes/prologue/rooms/BrokenToothCorridor.tscn");
    }

    private void Save(PrologueRoom room, string path)
    {
        Error error = SceneBuildUtil.PackAndSave(room, path);
        room.Free();
        if (error != Error.Ok) { GD.PushError($"BuildBrokenToothCorridor failed: {error}"); Quit(1); return; }
        PrologueRoom copy = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
        bool valid = copy.ValidateContract("from_courtyard").Count == 0 &&
            copy.GetNodeOrNull<RustShellSquire>("Actors/RustShellSquire") != null &&
            copy.GetNodeOrNull<GrappleSurface2D>("Geometry/ThreadAnchor")?.Material ==
                GrappleSurfaceMaterial.DedicatedAnchor;
        copy.Free();
        if (!valid) { GD.PushError("BuildBrokenToothCorridor contract validation failed."); Quit(1); return; }
        GD.Print("BuildBrokenToothCorridor: packed and validated.");
        Quit(0);
    }
}
