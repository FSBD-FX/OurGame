using System;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Mechanisms;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.World;

namespace Knight.Build.Prologue;

/// <summary>
/// Owns the active, replaceable graybox content for the five canyon sectors. Common sector roots,
/// streaming seams, recovery volumes, packing, and validation remain in
/// <see cref="PrologueCanyonBuildParts"/>.
/// </summary>
public static class CanyonContentBuilder
{
    public static void Build(PrologueSector root)
    {
        ArgumentNullException.ThrowIfNull(root);
        AddMicroSceneMarkers(root);
        EnsureContainer(root.GetNode("Gameplay"), "Mechanisms");
        switch (root.SectorId)
        {
            case PrologueSectorId.CanyonEntrance:
                BuildEntrance(root);
                break;
            case PrologueSectorId.CanyonDescent:
                BuildDescent(root);
                break;
            case PrologueSectorId.CanyonBasin:
                BuildBasin(root);
                break;
            case PrologueSectorId.CanyonAscent:
                BuildAscent(root);
                break;
            case PrologueSectorId.LostTemple:
                BuildTemple(root);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(root.SectorId), root.SectorId, null);
        }
    }

    private static void AddMicroSceneMarkers(PrologueSector root)
    {
        Node markers = EnsureContainer(root.GetNode("Gameplay"), "MicroScenes");
        Node2D readability = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"),
            "MicroSceneReadability");
        foreach (CanyonMicroSceneDefinition definition in CanyonContentCatalog.ForSector(root.SectorId))
        {
            var marker = new Marker2D
            {
                Name = definition.Id,
                Position = definition.LocalBounds.GetCenter()
            };
            marker.SetMeta("local_bounds", definition.LocalBounds);
            marker.SetMeta("optional_subarea", definition.OptionalSubarea);
            markers.AddChild(marker);

            Color color = definition.OptionalSubarea
                ? new Color(0.52f, 0.69f, 0.60f, .055f)
                : new Color(0.72f, 0.58f, 0.30f, .04f);
            readability.AddChild(RectPolygon(definition.Id, definition.LocalBounds.GetCenter(),
                definition.LocalBounds.Size - new Vector2(8f, 8f), color));
        }
    }

    private static void BuildEntrance(PrologueSector root)
    {
        Platform(root, "StartFloor", new Vector2(260, 630), new Vector2(520, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "JumpLedge", new Vector2(560, 630), new Vector2(300, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "AttackFloor", new Vector2(1000, 630), new Vector2(560, 40),
            GrappleSurfaceMaterial.GripStone);
        PointTarget(root, "EntranceGripStone", new Vector2(920, 390),
            GrappleSurfaceMaterial.GripStone, "tutorial");
        Segment(root, "entrance_move_attack", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(220, 550, 100, 48),
            new Rect2(450, 550, 220, 48));
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
        Platform(root, "EntryShelf", new Vector2(140, 630), new Vector2(280, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "GripStoneCatchZone", new Vector2(340, 700), new Vector2(160, 32),
            GrappleSurfaceMaterial.GripStone, catchShelf: true);
        Platform(root, "GripLanding", new Vector2(720, 760), new Vector2(300, 40),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "GripCatchShelf", new Vector2(430, 870), new Vector2(430, 32),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "IceLanding", new Vector2(1010, 990), new Vector2(360, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "IceCatchShelf", new Vector2(790, 1120), new Vector2(420, 32),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "BottomApproach", new Vector2(1080, 1260), new Vector2(360, 36),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "BottomLip", new Vector2(1120, 1400), new Vector2(320, 28),
            GrappleSurfaceMaterial.GripStone);

        Platform(root, "CraneShaftWall", new Vector2(600, 310), new Vector2(42, 300),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "FrostVentCeiling", new Vector2(800, 500), new Vector2(360, 32),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "VisibleIceWall", new Vector2(1240, 720), new Vector2(40, 240),
            GrappleSurfaceMaterial.Ice);
        Platform(root, "SelfRescueWall", new Vector2(1120, 620), new Vector2(42, 260),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "SelfRescueCatch", new Vector2(1180, 900), new Vector2(200, 32),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);

        PointTarget(root, "GripStoneLesson", new Vector2(260, 260),
            GrappleSurfaceMaterial.GripStone, "tutorial");
        PointTarget(root, "DedicatedLessonAnchor", new Vector2(470, 330),
            GrappleSurfaceMaterial.DedicatedAnchor, "tutorial", 28);
        PointTarget(root, "VisibleIceRejection", new Vector2(900, 720),
            GrappleSurfaceMaterial.Ice, "tutorial");
        Segment(root, "descent_grip_lesson", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(100, 550, 140, 48), new Rect2(590, 684, 260, 48),
            new NodePath("Gameplay/GrappleSurfaces/DedicatedLessonAnchor"));
        Segment(root, "descent_ice_rejection", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(650, 684, 100, 48),
            new Rect2(880, 914, 220, 48));
        RouteMarker(root, "PreviousSeam", new Vector2(2, 574));
        RouteMarker(root, "NextSeam", new Vector2(1100, 1438));

        BuildArchiveAlcove(root);
        AddMechanism(root, "Counterweight", CanyonClawMechanismKind.Counterweight,
            new Vector2(360, 420));
        AddMechanism(root, "HiddenPanel", CanyonClawMechanismKind.HiddenPanel,
            new Vector2(180, 1080));
    }

    private static void BuildArchiveAlcove(PrologueSector root)
    {
        Platform(root, "ArchiveFloor", new Vector2(180, 1224), new Vector2(360, 32),
            GrappleSurfaceMaterial.GripStone, oneWay: true);
        Platform(root, "ArchiveSkipFloor", new Vector2(180, 1320), new Vector2(360, 20),
            GrappleSurfaceMaterial.GripStone);
        Node2D path = OptionalPath(root, "ArchiveAlcove");
        Marker(path, "PassStart", new Vector2(60, 1172));
        Marker(path, "PassEnd", new Vector2(300, 1172));
        Marker(path, "SkipStart", new Vector2(60, 1274));
        Marker(path, "SkipEnd", new Vector2(300, 1274));
        Marker(path, "Reward", new Vector2(180, 1080));
    }

    private static void BuildBasin(PrologueSector root)
    {
        Platform(root, "BasinFloor", new Vector2(800, 630), new Vector2(1600, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "EntryCatch", new Vector2(1100, 180), new Vector2(340, 32),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CollapsedGearStep", new Vector2(520, 630), new Vector2(240, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "BrokenBridgeStep", new Vector2(780, 630), new Vector2(250, 40),
            GrappleSurfaceMaterial.GripStone);
        PointTarget(root, "BasinGripStone", new Vector2(780, 360),
            GrappleSurfaceMaterial.GripStone, "tutorial");
        Segment(root, "basin_shield_lane", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(220, 554, 100, 48),
            new Rect2(450, 550, 200, 48));
        Segment(root, "basin_checkpoint", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(740, 550, 100, 48),
            new Rect2(980, 554, 220, 48));
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
        BuildSalvageCache(root);
        AddMechanism(root, "CargoPlatform", CanyonClawMechanismKind.CargoPlatform,
            new Vector2(760, 430));
        AddBasinActors(root);
    }

    private static void BuildSupplyCave(PrologueSector root)
    {
        Platform(root, "SupplyCaveFloor", new Vector2(610, 520), new Vector2(520, 24),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "SupplyCaveReturnStep", new Vector2(850, 520), new Vector2(180, 24),
            GrappleSurfaceMaterial.GripStone);
        Node2D path = OptionalPath(root, "SupplyCave");
        Marker(path, "PassStart", new Vector2(390, 472));
        Marker(path, "PassEnd", new Vector2(850, 472));
        Marker(path, "SkipStart", new Vector2(350, 574));
        Marker(path, "SkipEnd", new Vector2(850, 574));
        Marker(path, "Reward", new Vector2(620, 474));
    }

    private static void BuildSalvageCache(PrologueSector root)
    {
        Platform(root, "SalvageFloor", new Vector2(1350, 520), new Vector2(420, 24),
            GrappleSurfaceMaterial.GripStone);
        Node2D path = OptionalPath(root, "SalvageCache");
        Marker(path, "PassStart", new Vector2(1160, 472));
        Marker(path, "PassEnd", new Vector2(1510, 472));
        Marker(path, "SkipStart", new Vector2(1160, 574));
        Marker(path, "SkipEnd", new Vector2(1510, 574));
        Marker(path, "Reward", new Vector2(1360, 472));
    }

    private static void AddBasinActors(PrologueSector root)
    {
        var actors = new Node2D { Name = "Actors" };
        RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        rust.Name = "RustShellSquire";
        rust.Position = new Vector2(940, 574);
        rust.SetMeta("encounter_pressure", "mixed-light");
        actors.AddChild(rust);
        BellBackLaborer laborer = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
            .Instantiate<BellBackLaborer>();
        laborer.Name = "BellBackLaborer";
        laborer.Position = new Vector2(1370, 574);
        laborer.SetMeta("encounter_pressure", "mixed-heavy");
        actors.AddChild(laborer);
        root.GetNode("Gameplay").AddChild(actors);
    }

    private static void BuildAscent(PrologueSector root)
    {
        Platform(root, "AscentEntry", new Vector2(150, 1350), new Vector2(300, 40),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "LandingOne", new Vector2(600, 1212), new Vector2(660, 40),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "LandingTwo", new Vector2(830, 1032), new Vector2(360, 40),
            GrappleSurfaceMaterial.GripStone, oneWay: true);
        // The final landing begins at x=855 and sits at y=734.  That leaves a 28px-plus
        // ray notch at its left edge and a 12px physical headroom band below CatchThree's
        // y=630 underside, while retaining the 15%-inset authored landing interior.
        Platform(root, "LandingThree", new Vector2(1067.5f, 734), new Vector2(425, 40),
            GrappleSurfaceMaterial.GripStone, oneWay: true);
        Platform(root, "CatchOneMain", new Vector2(500, 1390), new Vector2(600, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchOne", new Vector2(1120, 1350), new Vector2(60, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchOneSweep", new Vector2(945, 1390), new Vector2(290, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchTwoMainLeft", new Vector2(700, 1300), new Vector2(200, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchTwoMainRight", new Vector2(850, 1300), new Vector2(80, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchTwo", new Vector2(1000, 1300), new Vector2(60, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchThreeMainMiddle", new Vector2(950, 1110), new Vector2(60, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchThreeMainRight", new Vector2(1240, 1110), new Vector2(80, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        Platform(root, "CatchThree", new Vector2(600, 1110), new Vector2(60, 28),
            GrappleSurfaceMaterial.GripStone, catchShelf: true, oneWay: true);
        StaticBody2D returnOne = Platform(root, "CatchReturnEdgeOne",
            new Vector2(822, 1070), new Vector2(24, 60), GrappleSurfaceMaterial.GripStone);
        StaticBody2D returnTwo = Platform(root, "CatchReturnWallTwo",
            new Vector2(1040, 790), new Vector2(36, 120), GrappleSurfaceMaterial.GripStone);
        // CatchThree deliberately targets the underside at about (882, 630).  The
        // ceiling begins at x=850, stays clear of the AnchorThree cast by about 37px,
        // and the route from CatchThree clears AnchorTwo's circle by more than 12px.
        StaticBody2D returnThree = Platform(root, "CatchReturnCeilingThree",
            new Vector2(898, 618), new Vector2(96, 24), GrappleSurfaceMaterial.GripStone);
        returnOne.SetMeta("catch_return_for", "CatchOne");
        returnTwo.SetMeta("catch_return_for", "CatchTwo");
        returnThree.SetMeta("catch_return_for", "CatchThree");
        Platform(root, "CounterweightWall", new Vector2(60, 1070), new Vector2(36, 260),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "ScaffoldUnderside", new Vector2(300, 860), new Vector2(180, 30),
            GrappleSurfaceMaterial.GripStone);
        Platform(root, "CeilingRail", new Vector2(1010, 390), new Vector2(420, 30),
            GrappleSurfaceMaterial.GripStone);

        PointTarget(root, "AscentAnchorOne", new Vector2(400, 1030),
            GrappleSurfaceMaterial.DedicatedAnchor, "tutorial", 28);
        PointTarget(root, "AscentAnchorTwo", new Vector2(720, 800),
            GrappleSurfaceMaterial.DedicatedAnchor, "tutorial", 28);
        // Remain above the landing, but enter the camera's visible aim area during one
        // normal jump. y=500 stayed 17px off-screen even at the jump apex.
        PointTarget(root, "AscentAnchorThree", new Vector2(814, 580),
            GrappleSurfaceMaterial.DedicatedAnchor, "tutorial", 28);
        Segment(root, "ascent_grapple_one", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(90, 1274, 120, 48), new Rect2(280, 1132, 640, 48),
            new NodePath("Gameplay/GrappleSurfaces/AscentAnchorOne"));
        Segment(root, "ascent_grapple_two", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(440, 1132, 120, 48), new Rect2(650, 952, 360, 48),
            new NodePath("Gameplay/GrappleSurfaces/AscentAnchorTwo"));
        Segment(root, "ascent_grapple_three", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(730, 952, 80, 48), new Rect2(812, 654, 468, 48),
            new NodePath("Gameplay/GrappleSurfaces/AscentAnchorThree"));
        RouteMarker(root, "PreviousSeam", new Vector2(2, 1314));
        RouteMarker(root, "NextSeam", new Vector2(1278, 656));
        BuildHighRuin(root);
        AddAscentMoth(root);
    }

    private static void BuildHighRuin(PrologueSector root)
    {
        Platform(root, "HighRuinFloor", new Vector2(1130, 520), new Vector2(280, 28),
            GrappleSurfaceMaterial.GripStone);
        Node2D path = OptionalPath(root, "HighRuin");
        Marker(path, "PassStart", new Vector2(1010, 470));
        Marker(path, "PassEnd", new Vector2(1240, 470));
        Marker(path, "SkipStart", new Vector2(1010, 656));
        Marker(path, "SkipEnd", new Vector2(1240, 656));
        Marker(path, "Reward", new Vector2(1130, 470));
    }

    private static void AddAscentMoth(PrologueSector root)
    {
        var actors = new Node2D { Name = "Actors" };
        GraveLampMoth moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        moth.Name = "GraveLampMothSightline";
        moth.Position = new Vector2(1120, 430);
        moth.SetMeta("encounter_pressure", "sightline-only");
        actors.AddChild(moth);
        root.GetNode("Gameplay").AddChild(actors);
    }

    private static void BuildTemple(PrologueSector root)
    {
        Platform(root, "TempleSeamLanding", new Vector2(90, 700), new Vector2(180, 24),
            GrappleSurfaceMaterial.TempleMetal);
        Platform(root, "TempleSeamStep", new Vector2(210, 660), new Vector2(180, 32),
            GrappleSurfaceMaterial.TempleMetal);
        Platform(root, "OuterWallFloor", new Vector2(550, 630), new Vector2(580, 40),
            GrappleSurfaceMaterial.TempleMetal);
        Platform(root, "MechanismDais", new Vector2(470, 630), new Vector2(280, 40),
            GrappleSurfaceMaterial.TempleMetal, oneWay: true);
        Platform(root, "CheckpointFloor", new Vector2(850, 630), new Vector2(440, 40),
            GrappleSurfaceMaterial.TempleMetal);
        Platform(root, "ArenaFloor", new Vector2(1480, 630), new Vector2(880, 40),
            GrappleSurfaceMaterial.TempleMetal);
        Platform(root, "TempleCatch", new Vector2(650, 690), new Vector2(380, 24),
            GrappleSurfaceMaterial.TempleMetal, catchShelf: true, oneWay: true);
        PointTarget(root, "TempleAssist", new Vector2(900, 360),
            GrappleSurfaceMaterial.TempleMetal, "boss-critical");
        PointTarget(root, "ForbiddenRelicFace", new Vector2(1160, 360),
            GrappleSurfaceMaterial.Forbidden, "boundary", 34);
        Segment(root, "temple_mechanism", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(280, 550, 80, 48),
            new Rect2(380, 550, 200, 48));
        Segment(root, "temple_checkpoint", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(450, 550, 100, 48),
            new Rect2(720, 554, 220, 48));
        Segment(root, "temple_boss_entry", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, new Rect2(930, 554, 100, 48),
            new Rect2(1190, 554, 220, 48));
        RouteMarker(root, "PreviousSeam", new Vector2(2, 574));
        Marker(root.GetNode("Gameplay"), "BossApproachStart", new Vector2(980, 574));

        var mechanisms = root.GetNode<Node2D>("Gameplay/Mechanisms");
        BellChainMechanism chain = GD.Load<PackedScene>(
                "res://scenes/mechanisms/BellChainMechanism.tscn")
            .Instantiate<BellChainMechanism>();
        chain.Name = "BellChain";
        chain.Position = new Vector2(600, 400);
        chain.DoorHeight = 420f;
        mechanisms.AddChild(chain);
        AddMechanism(root, "BellBody", CanyonClawMechanismKind.BellBody,
            new Vector2(1020, 420));

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
        Area2D trigger = Trigger("ActivationTrigger", new Vector2(1200, 520),
            new Vector2(90, 180));
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

    private static CanyonClawMechanism2D AddMechanism(PrologueSector root, string name,
        CanyonClawMechanismKind kind, Vector2 position)
    {
        var mechanism = new CanyonClawMechanism2D
        {
            Name = name,
            Kind = kind,
            Position = position,
            LinkedNodePath = new NodePath("LinkedNode")
        };
        var linked = new Node2D { Name = "LinkedNode" };
        linked.AddChild(RectPolygon("ReadableMechanism", Vector2.Zero, new Vector2(72, 72),
            kind == CanyonClawMechanismKind.HiddenPanel
                ? new Color("6c725d")
                : new Color("9a7640")));
        mechanism.AddChild(linked);

        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Position = new Vector2(0, -64),
            Kind = ThreadTargetKind.Mechanism,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius,
            PullReceiverPath = new NodePath("..")
        };
        target.SetMeta("target_role", "mechanism");
        var collision = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        collision.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 28f }
        });
        target.AddChild(collision);
        target.AddChild(new LegacyThreadInteractionAdapter
        {
            Name = "LegacyThreadInteractionAdapter",
            TargetPath = new NodePath("..")
        });
        mechanism.AddChild(target);
        root.GetNode("Gameplay/Mechanisms").AddChild(mechanism);
        return mechanism;
    }

    private static StaticBody2D Platform(PrologueSector root, string name, Vector2 center,
        Vector2 size, GrappleSurfaceMaterial material, bool catchShelf = false,
        bool oneWay = false)
    {
        if (material == GrappleSurfaceMaterial.DedicatedAnchor)
            throw new ArgumentOutOfRangeException(nameof(material),
                "collision bodies require an explicit terrain material");
        var body = new StaticBody2D
        {
            Name = name,
            Position = center,
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Material = material,
            UseCollisionHitPoint = true
        });
        body.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size },
            OneWayCollision = oneWay,
            OneWayCollisionMargin = oneWay ? 16f : 1f
        });
        if (catchShelf) body.SetMeta("catch_shelf", true);
        root.GetNode("Gameplay/Collision").AddChild(body);

        Color fill = material switch
        {
            GrappleSurfaceMaterial.TempleMetal => new Color("58686e"),
            GrappleSurfaceMaterial.Ice => new Color("537f94"),
            GrappleSurfaceMaterial.Forbidden => new Color("554853"),
            _ when catchShelf => new Color("4e786e"),
            _ => new Color("40565a")
        };
        Polygon2D visual = RectPolygon(name, center, size, fill);
        visual.AddChild(RectPolygon("ReadableEdge", new Vector2(0, -size.Y / 2f + 3),
            new Vector2(size.X, 6), material == GrappleSurfaceMaterial.Ice
                ? new Color("a8d8e8")
                : new Color("a9c79d")));
        root.GetNode("VisualRoot/GameplayReadable").AddChild(visual);
        return body;
    }

    private static GrappleSurface2D PointTarget(PrologueSector root, string name,
        Vector2 position, GrappleSurfaceMaterial material, string role, float radius = 24f)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = material,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius,
            UseCollisionHitPoint = false
        };
        surface.SetMeta("target_role", role);
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
            radius + 5f, color));
        return surface;
    }

    private static TraversalSegment2D Segment(PrologueSector root, string id,
        TraversalSegmentKind kind, TraversalAbility abilities, Rect2 launch, Rect2 landing,
        NodePath assist = default)
    {
        TraversalSegment2D segment = PrologueBuildParts.TraversalSegment(
            root.GetNode("Gameplay/TraversalContracts"), id, kind, abilities, launch, landing,
            assist ?? new NodePath());
        segment.AddChild(ZonePolygon("LaunchZoneVisual", launch,
            new Color(0.36f, .72f, .62f, .32f)));
        segment.AddChild(ZonePolygon("LandingZoneVisual", landing,
            new Color(.82f, .66f, .32f, .34f)));
        return segment;
    }

    private static Polygon2D ZonePolygon(string name, Rect2 zone, Color color) => new()
    {
        Name = name,
        // Keep the authored route bounds inspectable without presenting test volumes as
        // floating platforms. First-level guidance supplies the actual player-facing cues.
        Visible = false,
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
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        door.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden,
            UseCollisionHitPoint = true
        });
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(36, 180) }
        });
        door.AddChild(RectPolygon("ReadableDoor", Vector2.Zero, new Vector2(36, 180),
            new Color("8e6a46")));
        return door;
    }

    private static Node2D EnsureContainer(Node parent, string name)
    {
        Node2D existing = parent.GetNodeOrNull<Node2D>(name);
        if (existing != null) return existing;
        var created = new Node2D { Name = name };
        parent.AddChild(created);
        return created;
    }

    private static Polygon2D RectPolygon(string name, Vector2 center, Vector2 size,
        Color color) => new()
    {
        Name = name,
        Position = center,
        Polygon =
        [
            new Vector2(-size.X / 2f, -size.Y / 2f),
            new Vector2(size.X / 2f, -size.Y / 2f),
            new Vector2(size.X / 2f, size.Y / 2f),
            new Vector2(-size.X / 2f, size.Y / 2f)
        ],
        Color = color
    };

    private static Polygon2D CirclePolygon(string name, Vector2 center, float radius,
        Color color)
    {
        var points = new Vector2[20];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = Mathf.Tau * index / points.Length;
            points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return new Polygon2D { Name = name, Position = center, Polygon = points, Color = color };
    }
}
