using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Tools;
using Knight.World;

namespace Knight.Build.Prologue;

public static class PrologueCanyonBuildParts
{
    private const float RecoverySeamNormalTolerance = 24f;
    private const float RecoverySeamTangentialTolerance = 24f;
    private const float RecoverySeamNearestTieTolerance = .01f;
    private const float RecoveryCarveHalfWidth = 96f;
    public static Rect2 EntranceBounds => Bounds(PrologueSectorId.CanyonEntrance);
    public static Rect2 DescentBounds => Bounds(PrologueSectorId.CanyonDescent);
    public static Rect2 BasinBounds => Bounds(PrologueSectorId.CanyonBasin);
    public static Rect2 AscentBounds => Bounds(PrologueSectorId.CanyonAscent);
    public static Rect2 TempleBounds => Bounds(PrologueSectorId.LostTemple);

    public static IReadOnlyDictionary<PrologueSectorId, Vector2> WorldOffsets { get; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<PrologueSectorId, Vector2>(
            CanyonWorldLayout.SectorBounds.ToDictionary(pair => pair.Key, pair => pair.Value.Position));

    public static string ScenePath(PrologueSectorId id) =>
        $"res://scenes/prologue/sectors/{id}.tscn";

    public static Rect2 Bounds(PrologueSectorId id) =>
        new(Vector2.Zero, CanyonWorldLayout.SectorBounds[id].Size);

    public static PrologueSector Build(PrologueSectorId id)
    {
        PrologueSector root = CreateRoot(id, Bounds(id));
        CanyonContentBuilder.Build(root);
        AddFallRecovery(root);
        return root;
    }

    public static Error SaveAndValidate(PrologueSector sector, string path, out int nodesBefore,
        out int nodesAfter, out string failure)
    {
        nodesBefore = CountNodes(sector);
        nodesAfter = 0;
        failure = string.Empty;
        Error error = SceneBuildUtil.PackAndSave(sector, path);
        if (error != Error.Ok)
        {
            failure = $"pack/save failed: {error}";
            return error;
        }
        PackedScene packed = GD.Load<PackedScene>(path);
        if (packed == null)
        {
            failure = "saved PackedScene could not be loaded";
            return Error.CantOpen;
        }
        PrologueSector copy = packed.Instantiate<PrologueSector>();
        try
        {
            nodesAfter = CountNodes(copy);
            if (nodesAfter != nodesBefore)
            {
                failure = $"node loss before={nodesBefore} after={nodesAfter}";
                return Error.InvalidData;
            }
            var definition = new PrologueSectorDefinition(copy.SectorId, packed,
                WorldOffsets[copy.SectorId], copy.Bounds, null, null);
            IReadOnlyList<string> errors = copy.ValidateContract(definition);
            if (errors.Count > 0)
            {
                failure = "sector contract failed: " + string.Join("; ", errors);
                return Error.InvalidData;
            }
            return Error.Ok;
        }
        finally
        {
            copy.Free();
        }
    }

    public static Godot.Collections.Array<Rect2> CriticalInteractionBounds(PrologueSectorId id)
    {
        var result = new Godot.Collections.Array<Rect2>();
        foreach ((Vector2 center, Vector2 size) in id switch
        {
            PrologueSectorId.CanyonEntrance => new[] { (new Vector2(440, 656), new Vector2(48, 40)) },
            PrologueSectorId.CanyonDescent =>
            [
                (new Vector2(160, 1440), new Vector2(48, 48)),
                (new Vector2(380, 1610), new Vector2(80, 60)),
                (new Vector2(460, 1532), new Vector2(40, 216)),
                (new Vector2(540, 1604), new Vector2(56, 56)),
                (new Vector2(1228, 1480), new Vector2(36, 160))
            ],
            PrologueSectorId.CanyonBasin => new[] { (new Vector2(1320, 280), new Vector2(48, 48)) },
            PrologueSectorId.CanyonAscent =>
            [
                (new Vector2(1140, 2500), new Vector2(56, 56))
            ],
            PrologueSectorId.LostTemple =>
            [
                (new Vector2(560, 250), new Vector2(56, 56)),
                (new Vector2(940, 240), new Vector2(48, 48)),
                (new Vector2(778, 210), new Vector2(60, 60)),
                (new Vector2(825, 140), new Vector2(30, 200)),
                (new Vector2(890, 204), new Vector2(56, 56))
            ],
            _ => Array.Empty<(Vector2, Vector2)>()
        })
            result.Add(new Rect2(center - size / 2f, size));
        if (id == PrologueSectorId.CanyonDescent)
        {
            result.Add(new Rect2(438, 2478, 44, 44));
            result.Add(new Rect2(352, 2465, 56, 150));
            result.Add(new Rect2(512, 2465, 56, 150));
        }
        else if (id == PrologueSectorId.CanyonBasin)
        {
            result.Add(new Rect2(2688, 448, 44, 44));
            result.Add(new Rect2(2578, 435, 64, 150));
            result.Add(new Rect2(2798, 435, 64, 150));
        }
        return result;
    }

    private static PrologueSector CreateRoot(PrologueSectorId id, Rect2 bounds)
    {
        var root = new PrologueSector { Name = id.ToString(), SectorId = id, Bounds = bounds };
        var visual = new Node2D { Name = "VisualRoot" };
        visual.AddChild(new Node2D { Name = "Background", ZIndex = -40 });
        visual.AddChild(new Node2D { Name = "Midground", ZIndex = -20 });
        visual.AddChild(new Node2D { Name = "GameplayReadable", ZIndex = -2 });
        visual.AddChild(new Node2D { Name = "Foreground", ZIndex = 20 });
        visual.AddChild(new Node2D { Name = "Effects", ZIndex = 30 });
        root.AddChild(visual);
        AddBackdrop(root, bounds, id);

        var gameplay = new Node2D { Name = "Gameplay" };
        gameplay.AddChild(new Node2D { Name = "Collision" });
        gameplay.AddChild(new Node2D { Name = "GrappleSurfaces" });
        gameplay.AddChild(new Node2D { Name = "TraversalContracts" });
        gameplay.AddChild(new Node2D { Name = "RouteMarkers" });
        gameplay.AddChild(new Node2D { Name = "FallRecovery" });
        gameplay.AddChild(new Node2D { Name = "OptionalPaths" });
        root.AddChild(gameplay);
        return root;
    }

    private static void AddBackdrop(PrologueSector root, Rect2 bounds, PrologueSectorId id)
    {
        Color backdrop = id switch
        {
            PrologueSectorId.CanyonEntrance => new Color("101c22"),
            PrologueSectorId.CanyonDescent => new Color("11191f"),
            PrologueSectorId.CanyonBasin => new Color("151a1b"),
            PrologueSectorId.CanyonAscent => new Color("101b20"),
            _ => new Color("17151e")
        };
        Node2D background = root.GetNode<Node2D>("VisualRoot/Background");
        background.AddChild(RectPolygon("CanyonVoid", bounds.GetCenter(), bounds.Size, backdrop));
        background.AddChild(new Knight.Presentation.CanyonBackdropVisual
        {
            Name = "DistantCanyon", SectorId = id, SectorBounds = bounds, DistantLayer = true
        });
        Node2D mid = root.GetNode<Node2D>("VisualRoot/Midground");
        // Entry and its required Descent neighbor share authored, irregular ruin shoulders.
        // Keep the generic staging recipe for the other sectors only.
        if (id is not (PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent))
        {
            for (int index = 0; index < 7; index++)
            {
                float x = bounds.Position.X + 90 + index * (bounds.Size.X - 180) / 6f;
                float height = bounds.Size.Y * (.38f + (index % 3) * .08f);
                mid.AddChild(RectPolygon($"CanyonRib{index + 1}",
                    new Vector2(x, bounds.End.Y - height / 2f), new Vector2(32, height),
                    new Color("1e2c30")));
            }
        }
        mid.AddChild(new Knight.Presentation.CanyonBackdropVisual
        {
            Name = "CanyonLandmark", SectorId = id, SectorBounds = bounds, DistantLayer = false,
            UseCanyonDepthStaging = id is PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent
        });
        Label label = new()
        {
            Name = "SectorLabel", Text = id.ToString(), Position = bounds.Position + new Vector2(42, 34),
            Modulate = new Color(0.75f, 0.82f, 0.76f, .55f), Visible = false
        };
        root.GetNode("VisualRoot/Effects").AddChild(label);
    }

    private static void BuildEntrance(PrologueSector root)
    {
        Platform(root, "StartFloor", new Vector2(260, 630), new Vector2(520, 40));
        Platform(root, "JumpLedge", new Vector2(560, 630), new Vector2(300, 40));
        Platform(root, "AttackFloor", new Vector2(1000, 630), new Vector2(560, 40));
        Surface(root, "EntranceGripStone", new Vector2(920, 390),
            GrappleSurfaceMaterial.GripStone);
        Segment(root, "entrance_move_attack", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(220, 550, 100, 48),
            new Rect2(470, 550, 180, 48));
        RouteMarker(root, "NextSeam", new Vector2(1278, 574));
        Marker(root, "Spawn", new Vector2(120, 574));

        var actors = new Node2D { Name = "Actors" };
        RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        rust.Name = "RustShellSquire";
        rust.Position = new Vector2(1010, 574);
        rust.SetMeta("encounter_pressure", "low");
        actors.AddChild(rust);
        root.GetNode("Gameplay").AddChild(actors);
    }

    private static void BuildDescent(PrologueSector root)
    {
        Platform(root, "EntryShelf", new Vector2(140, 570), new Vector2(280, 40));
        Platform(root, "GripStoneCatchZone", new Vector2(410, 640), new Vector2(300, 32), true);
        Platform(root, "GripLanding", new Vector2(720, 760), new Vector2(260, 40), true);
        Platform(root, "GripCatchShelf", new Vector2(430, 870), new Vector2(430, 32), true);
        Platform(root, "IceLanding", new Vector2(1010, 990), new Vector2(300, 40));
        Platform(root, "IceCatchShelf", new Vector2(790, 1120), new Vector2(360, 32), true);
        Platform(root, "BottomApproach", new Vector2(1080, 1260), new Vector2(320, 36));
        Platform(root, "BottomLip", new Vector2(1120, 1400), new Vector2(320, 28));
        Surface(root, "GripStoneLesson", new Vector2(260, 260), GrappleSurfaceMaterial.GripStone);
        Surface(root, "DedicatedLessonAnchor", new Vector2(440, 360),
            GrappleSurfaceMaterial.DedicatedAnchor, 28);
        Surface(root, "VisibleIceRejection", new Vector2(900, 720), GrappleSurfaceMaterial.Ice);
        Segment(root, "descent_grip_lesson", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(110, 490, 120, 48), new Rect2(620, 684, 200, 48),
            new NodePath("Gameplay/GrappleSurfaces/DedicatedLessonAnchor"));
        Segment(root, "descent_ice_rejection", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(650, 684, 100, 48),
            new Rect2(900, 914, 180, 48));
        RouteMarker(root, "PreviousSeam", new Vector2(2, 574));
        RouteMarker(root, "NextSeam", new Vector2(1100, 1438));
    }

    private static void BuildBasin(PrologueSector root)
    {
        Platform(root, "BasinFloor", new Vector2(800, 630), new Vector2(1600, 40));
        Platform(root, "EntryCatch", new Vector2(1100, 180), new Vector2(300, 32), true);
        Platform(root, "CollapsedGearStep", new Vector2(520, 630), new Vector2(220, 40));
        Platform(root, "BrokenBridgeStep", new Vector2(780, 630), new Vector2(230, 40));
        Surface(root, "BasinGripStone", new Vector2(780, 360), GrappleSurfaceMaterial.GripStone);
        Segment(root, "basin_shield_lane", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(220, 554, 100, 48),
            new Rect2(470, 550, 160, 48));
        Segment(root, "basin_checkpoint", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(740, 550, 100, 48),
            new Rect2(990, 554, 180, 48));
        RouteMarker(root, "PreviousSeam", new Vector2(1100, 2));
        RouteMarker(root, "NextSeam", new Vector2(1598, 574));

        Area2D shieldLane = Trigger("ShieldLane", new Vector2(520, 520), new Vector2(330, 180));
        shieldLane.SetMeta("input", "mouse_right");
        root.GetNode("Gameplay").AddChild(shieldLane);
        shieldLane.SetScript(GD.Load<Script>(
            "res://scripts/prologue/traversal/CanyonShieldLaneHazard.cs"));
        CheckpointTrigger checkpoint = PrologueBuildParts.Checkpoint(root.GetNode("Gameplay"),
            new Vector2(1100, 574), "basin_checkpoint");
        checkpoint.Name = "Checkpoint";
        BuildSupplyCave(root);
    }

    private static void BuildSupplyCave(PrologueSector root)
    {
        Platform(root, "SupplyCaveFloor", new Vector2(610, 520), new Vector2(520, 24));
        Platform(root, "SupplyCaveReturnStep", new Vector2(850, 520), new Vector2(180, 24));
        Node2D path = OptionalPath(root, "SupplyCave");
        Marker(path, "PassStart", new Vector2(390, 472));
        Marker(path, "PassEnd", new Vector2(850, 472));
        Marker(path, "SkipStart", new Vector2(350, 574));
        Marker(path, "SkipEnd", new Vector2(850, 574));
        Marker(path, "Reward", new Vector2(620, 474));
    }

    private static void BuildAscent(PrologueSector root)
    {
        Platform(root, "AscentEntry", new Vector2(150, 1350), new Vector2(300, 40));
        Platform(root, "LandingOne", new Vector2(520, 1212), new Vector2(280, 40), oneWay: true);
        Platform(root, "LandingTwo", new Vector2(850, 1032), new Vector2(280, 40), oneWay: true);
        Platform(root, "LandingThree", new Vector2(1070, 712), new Vector2(420, 40), oneWay: true);
        Platform(root, "CatchOne", new Vector2(500, 1390), new Vector2(500, 28), true);
        Platform(root, "CatchTwo", new Vector2(850, 1300), new Vector2(500, 28), true);
        Platform(root, "CatchThree", new Vector2(1050, 1110), new Vector2(460, 28), true);
        Surface(root, "AscentAnchorOne", new Vector2(400, 1030),
            GrappleSurfaceMaterial.DedicatedAnchor, 28);
        Surface(root, "AscentAnchorTwo", new Vector2(750, 800),
            GrappleSurfaceMaterial.DedicatedAnchor, 28);
        Surface(root, "AscentAnchorThree", new Vector2(900, 500),
            GrappleSurfaceMaterial.DedicatedAnchor, 28);
        Segment(root, "ascent_grapple_one", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(90, 1274, 120, 48), new Rect2(380, 1132, 200, 48),
            new NodePath("Gameplay/GrappleSurfaces/AscentAnchorOne"));
        Segment(root, "ascent_grapple_two", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(440, 1132, 120, 48), new Rect2(720, 952, 200, 48),
            new NodePath("Gameplay/GrappleSurfaces/AscentAnchorTwo"));
        Segment(root, "ascent_grapple_three", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(730, 952, 80, 48), new Rect2(880, 632, 350, 48),
            new NodePath("Gameplay/GrappleSurfaces/AscentAnchorThree"));
        RouteMarker(root, "PreviousSeam", new Vector2(2, 1314));
        RouteMarker(root, "NextSeam", new Vector2(1278, 656));
        BuildHighRuin(root);
    }

    private static void BuildHighRuin(PrologueSector root)
    {
        Platform(root, "HighRuinFloor", new Vector2(1130, 520), new Vector2(260, 28));
        Node2D path = OptionalPath(root, "HighRuin");
        Marker(path, "PassStart", new Vector2(1010, 470));
        Marker(path, "PassEnd", new Vector2(1240, 470));
        Marker(path, "SkipStart", new Vector2(1010, 656));
        Marker(path, "SkipEnd", new Vector2(1240, 656));
        Marker(path, "Reward", new Vector2(1130, 570));
    }

    private static void BuildTemple(PrologueSector root)
    {
        Platform(root, "TempleSeamLanding", new Vector2(90, 700), new Vector2(180, 24));
        Platform(root, "TempleSeamStep", new Vector2(210, 660), new Vector2(180, 32));
        Platform(root, "OuterWallFloor", new Vector2(550, 630), new Vector2(580, 40));
        Platform(root, "MechanismDais", new Vector2(470, 630), new Vector2(260, 40), oneWay: true);
        Platform(root, "CheckpointFloor", new Vector2(850, 630), new Vector2(420, 40));
        Platform(root, "ArenaFloor", new Vector2(1480, 630), new Vector2(880, 40));
        Platform(root, "TempleCatch", new Vector2(650, 690), new Vector2(380, 24), true);
        Surface(root, "TempleAssist", new Vector2(900, 360), GrappleSurfaceMaterial.TempleMetal);
        Segment(root, "temple_mechanism", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(280, 550, 80, 48),
            new Rect2(430, 550, 100, 48));
        Segment(root, "temple_checkpoint", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(450, 550, 100, 48),
            new Rect2(730, 554, 180, 48));
        Segment(root, "temple_boss_entry", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(930, 554, 100, 48),
            new Rect2(1210, 554, 180, 48));
        RouteMarker(root, "PreviousSeam", new Vector2(2, 574));
        Marker(root.GetNode("Gameplay"), "BossApproachStart", new Vector2(980, 574));

        var mechanisms = new Node2D { Name = "Mechanisms" };
        BellChainMechanism chain = GD.Load<PackedScene>(
                "res://scenes/mechanisms/BellChainMechanism.tscn")
            .Instantiate<BellChainMechanism>();
        chain.Name = "BellChain";
        chain.Position = new Vector2(600, 400);
        chain.DoorHeight = 420f;
        mechanisms.AddChild(chain);
        root.GetNode("Gameplay").AddChild(mechanisms);

        CheckpointTrigger checkpoint = PrologueBuildParts.Checkpoint(root.GetNode("Gameplay"),
            new Vector2(880, 574), "temple_checkpoint");
        checkpoint.Name = "TempleCheckpoint";
        AddBossArena(root);
    }

    private static void AddBossArena(PrologueSector root)
    {
        Node gameplay = root.GetNode("Gameplay");
        var doors = new Node2D { Name = "Doors" };
        doors.AddChild(Door("Entry", new Vector2(1120, 520)));
        doors.AddChild(Door("Exit", new Vector2(1840, 520)));
        gameplay.AddChild(doors);
        gameplay.AddChild(new Node2D { Name = "BossRoot" });
        Area2D trigger = Trigger("ActivationTrigger", new Vector2(1200, 520), new Vector2(90, 180));
        gameplay.AddChild(trigger);
        gameplay.AddChild(new BossArenaController
        {
            Name = "BossArenaController",
            BossScene = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn"),
            BossRootPath = new NodePath("../BossRoot"),
            EntryDoorCollisionPath = new NodePath("../Doors/Entry/Collision"),
            ExitDoorCollisionPath = new NodePath("../Doors/Exit/Collision"),
            ActivationTriggerPath = new NodePath("../ActivationTrigger"),
            BossSpawnPosition = new Vector2(1500, 574)
        });
    }

    private static void AddFallRecovery(PrologueSector root)
    {
        Rect2 bounds = root.Bounds;
        Node recovery = root.GetNode("Gameplay/FallRecovery");
        var spans = new List<(float Start, float End)> { (bounds.Position.X, bounds.End.X) };
        var bottomSeamIntervals = new List<(float Start, float End)>();
        foreach (Marker2D seam in root.GetNode("Gameplay/RouteMarkers").GetChildren())
        {
            if (!IsUnambiguousBottomRecoverySeam(seam.Position, bounds)) continue;
            float carveStart = Mathf.Clamp(seam.Position.X - RecoveryCarveHalfWidth,
                bounds.Position.X, bounds.End.X);
            float carveEnd = Mathf.Clamp(seam.Position.X + RecoveryCarveHalfWidth,
                bounds.Position.X, bounds.End.X);
            bottomSeamIntervals.Add((carveStart, carveEnd));
            var carved = new List<(float Start, float End)>();
            foreach ((float start, float end) in spans)
            {
                if (carveEnd <= start || carveStart >= end)
                {
                    carved.Add((start, end));
                    continue;
                }
                if (carveStart - start >= 16f) carved.Add((start, carveStart));
                if (end - carveEnd >= 16f) carved.Add((carveEnd, end));
            }
            spans = carved;
        }
        AssertBottomRecoveryComplement(bounds, bottomSeamIntervals, spans, root);
        int volumeIndex = 0;
        foreach ((float start, float end) in spans)
            AddRecoveryVolume(recovery, bounds, start, end, volumeIndex++);
        Marker(recovery, "RecoveryLanding", root.SectorId switch
        {
            PrologueSectorId.CanyonEntrance => new Vector2(160, 600),
            PrologueSectorId.CanyonDescent => new Vector2(880, 1260),
            PrologueSectorId.CanyonBasin => new Vector2(400, 360),
            PrologueSectorId.CanyonAscent => new Vector2(80, 2804),
            _ => new Vector2(160, 484)
        });
    }

    // Tests share this deterministic builder seam directly; it deliberately exposes no gameplay
    // control and keeps recovery carving bound to the exact classifier used by AddFallRecovery.
    internal static bool IsUnambiguousBottomRecoverySeam(Vector2 marker, Rect2 bounds) =>
        ClassifyNearestBoundedSeamEdge(marker, bounds) == RecoverySeamEdge.Bottom;

    // A seam is only valid when its normal distance and tangential coordinate both lie in their
    // explicitly named tolerances. Exactly one nearest valid edge wins; a tie is a corner and
    // deliberately fails closed before recovery geometry is carved.
    private static RecoverySeamEdge ClassifyNearestBoundedSeamEdge(Vector2 marker, Rect2 bounds)
    {
        var candidates = new List<(RecoverySeamEdge Edge, float NormalDistance)>();
        AddBoundedCandidate(RecoverySeamEdge.Left, Mathf.Abs(marker.X - bounds.Position.X),
            marker.Y, bounds.Position.Y, bounds.End.Y, candidates);
        AddBoundedCandidate(RecoverySeamEdge.Right, Mathf.Abs(marker.X - bounds.End.X),
            marker.Y, bounds.Position.Y, bounds.End.Y, candidates);
        AddBoundedCandidate(RecoverySeamEdge.Top, Mathf.Abs(marker.Y - bounds.Position.Y),
            marker.X, bounds.Position.X, bounds.End.X, candidates);
        AddBoundedCandidate(RecoverySeamEdge.Bottom, Mathf.Abs(marker.Y - bounds.End.Y),
            marker.X, bounds.Position.X, bounds.End.X, candidates);
        if (candidates.Count == 0) return RecoverySeamEdge.None;

        float nearest = candidates.Min(candidate => candidate.NormalDistance);
        List<(RecoverySeamEdge Edge, float NormalDistance)> nearestCandidates = candidates
            .Where(candidate => Mathf.Abs(candidate.NormalDistance - nearest) <=
                RecoverySeamNearestTieTolerance)
            .ToList();
        return nearestCandidates.Count == 1 ? nearestCandidates[0].Edge : RecoverySeamEdge.None;
    }

    private static void AddBoundedCandidate(RecoverySeamEdge edge, float normalDistance,
        float tangential, float tangentialStart, float tangentialEnd,
        ICollection<(RecoverySeamEdge Edge, float NormalDistance)> candidates)
    {
        if (normalDistance > RecoverySeamNormalTolerance ||
            tangential < tangentialStart - RecoverySeamTangentialTolerance ||
            tangential > tangentialEnd + RecoverySeamTangentialTolerance) return;
        candidates.Add((edge, normalDistance));
    }

    private enum RecoverySeamEdge
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    private static void AssertBottomRecoveryComplement(Rect2 bounds,
        IReadOnlyList<(float Start, float End)> bottomSeamIntervals,
        IReadOnlyList<(float Start, float End)> spans, PrologueSector root)
    {
        float cursor = bounds.Position.X;
        foreach ((float start, float end) in spans)
        {
            if (start < cursor - .01f || end <= start)
                throw new InvalidOperationException($"{root.SectorId} recovery spans overlap or invert.");
            cursor = end;
        }
        if (cursor > bounds.End.X + .01f)
            throw new InvalidOperationException($"{root.SectorId} recovery exceeds its bottom bounds.");
        for (float x = bounds.Position.X + .5f; x < bounds.End.X; x += 1f)
        {
            bool carvedByTrueBottomSeam = bottomSeamIntervals.Any(interval =>
                x >= interval.Start && x <= interval.End);
            bool coveredByRecovery = spans.Any(span => x >= span.Start && x <= span.End);
            if (coveredByRecovery == carvedByTrueBottomSeam)
                throw new InvalidOperationException($"{root.SectorId} bottom recovery complement is " +
                    $"incomplete at x={x:0.0}; carved={carvedByTrueBottomSeam} covered={coveredByRecovery}.");
        }
    }

    private static void AddRecoveryVolume(Node recovery, Rect2 bounds, float start, float end,
        int index)
    {
        var volume = new KillZone
        {
            Name = index == 0 ? "RecoveryVolume" : $"RecoveryVolume{index + 1}",
            Position = new Vector2((start + end) / 2f, bounds.End.Y - 8),
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true,
            RoomControllerPath = new NodePath("../../../../../..")
        };
        volume.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D
            {
                Size = new Vector2(end - start, 16)
            }
        });
        recovery.AddChild(volume);
    }

    private static StaticBody2D Platform(PrologueSector root, string name, Vector2 center,
        Vector2 size, bool catchShelf = false, bool oneWay = false)
    {
        var body = new StaticBody2D
        {
            Name = name,
            Position = center,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = size },
            OneWayCollision = oneWay,
            OneWayCollisionMargin = oneWay ? 16f : 1f
        });
        if (catchShelf) body.SetMeta("catch_shelf", true);
        root.GetNode("Gameplay/Collision").AddChild(body);
        Color fill = catchShelf ? new Color("4e786e") : new Color("40565a");
        Polygon2D visual = RectPolygon(name, center, size, fill);
        visual.AddChild(RectPolygon("ReadableEdge", new Vector2(0, -size.Y / 2f + 3),
            new Vector2(size.X, 6), new Color("a9c79d")));
        root.GetNode("VisualRoot/GameplayReadable").AddChild(visual);
        return body;
    }

    private static GrappleSurface2D Surface(PrologueSector root, string name, Vector2 position,
        GrappleSurfaceMaterial material, float radius = 24)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = material,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
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
            Name = "Collision", Shape = new CircleShape2D { Radius = radius }
        });
        surface.AddChild(area);
        root.GetNode("Gameplay/GrappleSurfaces").AddChild(surface);
        Color color = material switch
        {
            GrappleSurfaceMaterial.DedicatedAnchor => new Color("d2ad5c"),
            GrappleSurfaceMaterial.GripStone => new Color("76b59c"),
            GrappleSurfaceMaterial.TempleMetal => new Color("9dd3d2"),
            GrappleSurfaceMaterial.Ice => new Color("91cde8"),
            _ => new Color("6f6974")
        };
        root.GetNode("VisualRoot/GameplayReadable").AddChild(CirclePolygon(name, position,
            radius + 5, color));
        return surface;
    }

    private static TraversalSegment2D Segment(PrologueSector root, string id,
        TraversalSegmentKind kind, TraversalAbility abilities, Rect2 launch, Rect2 landing,
        NodePath assist = default)
    {
        TraversalSegment2D segment = PrologueBuildParts.TraversalSegment(
            root.GetNode("Gameplay/TraversalContracts"), id, kind, abilities, launch, landing,
            assist ?? new NodePath());
        Polygon2D launchVisual = ZonePolygon("LaunchZoneVisual", launch, new Color(0.36f, .72f, .62f, .32f));
        Polygon2D landingVisual = ZonePolygon("LandingZoneVisual", landing, new Color(.82f, .66f, .32f, .34f));
        segment.AddChild(launchVisual);
        segment.AddChild(landingVisual);
        return segment;
    }

    private static Polygon2D ZonePolygon(string name, Rect2 zone, Color color) => new()
    {
        Name = name,
        Polygon =
        [
            zone.Position, new Vector2(zone.End.X, zone.Position.Y), zone.End,
            new Vector2(zone.Position.X, zone.End.Y)
        ],
        Color = color
    };

    private static void RouteMarker(PrologueSector root, string name, Vector2 position) =>
        Marker(root.GetNode("Gameplay/RouteMarkers"), name, position);

    private static Marker2D Marker(Node parent, string name, Vector2 position)
    {
        var marker = new Marker2D { Name = name, Position = position };
        parent.AddChild(marker);
        return marker;
    }

    private static Node2D OptionalPath(PrologueSector root, string name)
    {
        var path = new Node2D { Name = name };
        root.GetNode("Gameplay/OptionalPaths").AddChild(path);
        return path;
    }

    private static Area2D Trigger(string name, Vector2 position, Vector2 size)
    {
        var trigger = new Area2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        trigger.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = size }
        });
        return trigger;
    }

    private static StaticBody2D Door(string name, Vector2 position)
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
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(36, 180) }
        });
        door.AddChild(RectPolygon("ReadableDoor", Vector2.Zero, new Vector2(36, 180),
            new Color("8e6a46")));
        return door;
    }

    private static Polygon2D RectPolygon(string name, Vector2 center, Vector2 size, Color color) => new()
    {
        Name = name,
        Position = center,
        Polygon =
        [
            new Vector2(-size.X / 2, -size.Y / 2), new Vector2(size.X / 2, -size.Y / 2),
            new Vector2(size.X / 2, size.Y / 2), new Vector2(-size.X / 2, size.Y / 2)
        ],
        Color = color
    };

    private static Polygon2D CirclePolygon(string name, Vector2 center, float radius, Color color)
    {
        var points = new Vector2[20];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = Mathf.Tau * index / points.Length;
            points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return new Polygon2D { Name = name, Position = center, Polygon = points, Color = color };
    }

    private static int CountNodes(Node root)
    {
        int count = 1;
        foreach (Node child in root.GetChildren()) count += CountNodes(child);
        return count;
    }
}
