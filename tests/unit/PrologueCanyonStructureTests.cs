using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Mechanisms;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Unit;

public static class PrologueCanyonStructureTests
{
    private const float SeamMargin = PrologueSectorStreamModel.UnloadSeamMargin;
    private static readonly string[] VisualLayers =
    [
        "Background", "Midground", "GameplayReadable", "Foreground", "Effects"
    ];

    private static readonly SectorCase[] Sectors =
    [
        new(PrologueSectorId.CanyonEntrance, "res://scenes/prologue/sectors/CanyonEntrance.tscn",
            new Rect2(0, 0, 1280, 720), new Vector2(0, 0),
            ["entrance_move_attack"]),
        new(PrologueSectorId.CanyonDescent, "res://scenes/prologue/sectors/CanyonDescent.tscn",
            new Rect2(0, 0, 1280, 1440), new Vector2(1280, 0),
            ["descent_grip_lesson", "descent_ice_rejection"]),
        new(PrologueSectorId.CanyonBasin, "res://scenes/prologue/sectors/CanyonBasin.tscn",
            new Rect2(0, 0, 1600, 720), new Vector2(1280, 1440),
            ["basin_shield_lane", "basin_checkpoint"]),
        new(PrologueSectorId.CanyonAscent, "res://scenes/prologue/sectors/CanyonAscent.tscn",
            new Rect2(0, 0, 1280, 1440), new Vector2(2880, 720),
            ["ascent_grapple_one", "ascent_grapple_two", "ascent_grapple_three"]),
        new(PrologueSectorId.LostTemple, "res://scenes/prologue/sectors/LostTemple.tscn",
            new Rect2(0, 0, 1920, 720), new Vector2(4160, 720),
            ["temple_mechanism", "temple_checkpoint", "temple_boss_entry"])
    ];

    public static void Run(PackedScene mainScene)
    {
        AssertExactMicroSceneCatalog();
        var observedSegments = new List<string>();
        var observedMicroScenes = new List<string>();
        var observedClawMechanisms = new List<string>();
        foreach (SectorCase sectorCase in Sectors)
        {
            TestAssert.True(ResourceLoader.Exists(sectorCase.Path),
                $"generated canyon sector exists: {sectorCase.Id}");
            PackedScene packed = GD.Load<PackedScene>(sectorCase.Path);
            TestAssert.True(packed != null, $"generated canyon sector loads: {sectorCase.Id}");
            PrologueSector sector = packed.Instantiate<PrologueSector>();
            try
            {
                AssertSectorContract(sectorCase, sector, observedSegments, observedMicroScenes,
                    observedClawMechanisms);
            }
            finally
            {
                sector.Free();
            }
        }

        string[] expectedSegments =
        [
            "entrance_move_attack", "descent_grip_lesson", "descent_ice_rejection",
            "basin_shield_lane", "basin_checkpoint", "ascent_grapple_one",
            "ascent_grapple_two", "ascent_grapple_three", "temple_mechanism",
            "temple_checkpoint", "temple_boss_entry"
        ];
        TestAssert.Equal(string.Join("|", expectedSegments), string.Join("|", observedSegments),
            "five canyon sectors expose the exact eleven mandatory route segments in order");
        TestAssert.Equal(string.Join("|", ExpectedMicroScenes.Select(item => item.Id)),
            string.Join("|", observedMicroScenes),
            "generated canyon exposes each of the exact fifteen catalog markers once and in order");
        string[] expectedClawMechanisms =
        [
            "CanyonDescent/Gameplay/Mechanisms/Counterweight:Counterweight",
            "CanyonDescent/Gameplay/Mechanisms/HiddenPanel:HiddenPanel",
            "CanyonBasin/Gameplay/Mechanisms/CargoPlatform:CargoPlatform",
            "LostTemple/Gameplay/Mechanisms/BellBody:BellBody"
        ];
        TestAssert.Equal(4, observedClawMechanisms.Count,
            "generated canyon contains exactly four claw mechanisms");
        TestAssert.Equal(string.Join("|", expectedClawMechanisms),
            string.Join("|", observedClawMechanisms),
            "generated canyon claws occupy only the four approved sector paths and kinds");
        AssertMainComposition(mainScene);
        GD.Print("TASK3_CANYON_STRUCTURE_OK sectors=5 segments=11 micro_scenes=15 " +
            "visual_layers=5 typed_collision=True recovery_max=320 mandatory_dash=0");
    }

    private static void AssertSectorContract(SectorCase expected, PrologueSector sector,
        ICollection<string> observedSegments, ICollection<string> observedMicroScenes,
        ICollection<string> observedClawMechanisms)
    {
        TestAssert.Equal(expected.Id.ToString(), sector.Name.ToString(),
            $"{expected.Id} root name is stable");
        TestAssert.Equal(expected.Id, sector.SectorId, $"{expected.Id} root identity is typed");
        TestAssert.Equal(expected.Bounds, sector.Bounds, $"{expected.Id} local bounds are exact");
        TestAssert.Equal(Vector2.Zero, sector.Position, $"{expected.Id} root remains local at origin");

        Node2D visual = sector.GetNodeOrNull<Node2D>("VisualRoot");
        TestAssert.True(visual != null, $"{expected.Id} exposes stable VisualRoot");
        string[] actualLayers = visual.GetChildren().Select(child => child.Name.ToString()).ToArray();
        TestAssert.Equal(string.Join("|", VisualLayers), string.Join("|", actualLayers),
            $"{expected.Id} exposes the exact replaceable visual layers");

        Node2D gameplay = sector.GetNodeOrNull<Node2D>("Gameplay");
        Node2D collision = sector.GetNodeOrNull<Node2D>("Gameplay/Collision");
        Node2D traversal = sector.GetNodeOrNull<Node2D>("Gameplay/TraversalContracts");
        TestAssert.True(gameplay != null && collision != null && traversal != null,
            $"{expected.Id} separates gameplay collision and traversal from VisualRoot");
        TestAssert.True(collision.GetChildren().OfType<StaticBody2D>().Any(body =>
                body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D),
            $"{expected.Id} contains authoritative packed world collision");
        TestAssert.True(sector.GetNodeOrNull<Area2D>("Gameplay/FallRecovery/RecoveryVolume")
                ?.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D,
            $"{expected.Id} exposes an authoritative fall-recovery volume");
        TestAssert.True(sector.GetNodeOrNull<Marker2D>("Gameplay/FallRecovery/RecoveryLanding") != null,
            $"{expected.Id} exposes a visible recovery landing target");

        AssertMicroSceneMarkers(expected, sector, observedMicroScenes);
        AssertTypedGameplayBodies(expected, gameplay, visual);
        ObserveCanyonClawMechanisms(expected, sector, observedClawMechanisms);

        Marker2D previous = sector.GetNodeOrNull<Marker2D>("Gameplay/RouteMarkers/PreviousSeam");
        Marker2D next = sector.GetNodeOrNull<Marker2D>("Gameplay/RouteMarkers/NextSeam");
        if (expected.Id == PrologueSectorId.CanyonEntrance)
            TestAssert.True(previous == null && next != null,
                "entrance exposes only the forward continuous seam");
        else if (expected.Id == PrologueSectorId.LostTemple)
            TestAssert.True(previous != null && next == null,
                "temple exposes only the backward continuous seam");
        else
            TestAssert.True(previous != null && next != null,
                $"{expected.Id} exposes both continuous route seams");

        string[] actualSegments = traversal.GetChildren().OfType<TraversalSegment2D>()
            .Select(segment => segment.SegmentId.ToString()).ToArray();
        TestAssert.Equal(string.Join("|", expected.Segments), string.Join("|", actualSegments),
            $"{expected.Id} mandatory segments are exact and ordered");
        foreach (TraversalSegment2D segment in traversal.GetChildren().OfType<TraversalSegment2D>())
        {
            IReadOnlyList<string> errors = segment.Validate(sector);
            TestAssert.Equal(0, errors.Count,
                $"{expected.Id}/{segment.SegmentId} validates: {string.Join("; ", errors)}");
            TestAssert.True((segment.RequiredAbilities & TraversalAbility.Dash) == 0,
                $"{segment.SegmentId} mandatory ability mask excludes Dash");
            TestAssert.True(segment.LandingZone.Size.X >= 180f,
                $"{segment.SegmentId} required landing is at least 180 px wide");
            TestAssert.True(expected.Bounds.Encloses(segment.LaunchZone) &&
                expected.Bounds.Encloses(segment.LandingZone),
                $"{segment.SegmentId} launch and landing zones stay inside sector bounds");
            TestAssert.True(segment.GetNodeOrNull<CanvasItem>("LaunchZoneVisual") != null &&
                segment.GetNodeOrNull<CanvasItem>("LandingZoneVisual") != null,
                $"{segment.SegmentId} launch and landing bounds remain inspectable");
            TestAssert.True(!segment.GetNode<CanvasItem>("LaunchZoneVisual").Visible &&
                !segment.GetNode<CanvasItem>("LandingZoneVisual").Visible,
                $"{segment.SegmentId} debug volumes do not masquerade as floating platforms");
            if (segment.Kind == TraversalSegmentKind.Assisted)
            {
                TestAssert.True(!segment.AssistTargetPath.IsEmpty &&
                    sector.GetNodeOrNull<GrappleSurface2D>(segment.AssistTargetPath) != null,
                    $"{segment.SegmentId} exposes an explicit grapple assist surface");
                GrappleSurface2D assist = sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
                float distance = segment.LaunchZone.GetCenter().DistanceTo(
                    PositionInSector(sector, assist));
                TestAssert.True(distance >= 360f && distance <= 520f,
                    $"{segment.SegmentId} necessary grapple distance stays inside [360,520]; " +
                    $"actual={distance:0.00}");
                AssertCatchLayer(expected.Id, sector, segment);
            }
            observedSegments.Add(segment.SegmentId.ToString());
        }

        foreach (GrappleSurface2D surface in Descendants(sector).OfType<GrappleSurface2D>())
        {
            TestAssert.Equal(0, surface.Validate().Count,
                $"{expected.Id}/{surface.Name} surface contract validates");
            if (surface.UseCollisionHitPoint)
                TestAssert.True(surface.GetParent() is StaticBody2D,
                    $"{expected.Id}/{surface.Name} terrain profile belongs to authoritative collision");
            else
                TestAssert.True(surface.GetNodeOrNull<Area2D>("TargetCollision")
                        ?.GetNodeOrNull<CollisionShape2D>("Collision") != null,
                    $"{expected.Id}/{surface.Name} point target has real target collision");
            if (surface.UseCollisionHitPoint ||
                surface.Material is GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden)
                continue;
            AssertCriticalSurfaceClearOfSeams(expected, PositionInSector(sector, surface));
        }

        AssertAuthoredContent(expected.Id, sector);
    }

    private static void ObserveCanyonClawMechanisms(SectorCase expected, PrologueSector sector,
        ICollection<string> observed)
    {
        foreach (CanyonClawMechanism2D mechanism in Descendants(sector)
                     .OfType<CanyonClawMechanism2D>())
        {
            NodePath path = sector.GetPathTo(mechanism);
            observed.Add($"{expected.Id}/{path}:{mechanism.Kind}");
        }
    }

    private static readonly ExpectedMicroScene[] ExpectedMicroScenes =
    [
        new("BrokenBridgeLookout", PrologueSectorId.CanyonEntrance, new Rect2(0, 0, 360, 720), false),
        new("SuspendedYard", PrologueSectorId.CanyonEntrance, new Rect2(360, 0, 430, 720), false),
        new("CollapsedGate", PrologueSectorId.CanyonEntrance, new Rect2(790, 0, 490, 720), false),
        new("CraneShaft", PrologueSectorId.CanyonDescent, new Rect2(0, 0, 640, 500), false),
        new("FrostVent", PrologueSectorId.CanyonDescent, new Rect2(520, 360, 760, 440), false),
        new("BrokenLift", PrologueSectorId.CanyonDescent, new Rect2(300, 760, 980, 680), false),
        new("ArchiveAlcove", PrologueSectorId.CanyonDescent, new Rect2(0, 980, 360, 300), true),
        new("GearGraveyard", PrologueSectorId.CanyonBasin, new Rect2(0, 0, 520, 720), false),
        new("SteamPumpTrench", PrologueSectorId.CanyonBasin, new Rect2(520, 0, 580, 720), false),
        new("CheckpointCampSalvage", PrologueSectorId.CanyonBasin, new Rect2(1100, 0, 500, 720), false),
        new("CounterweightWell", PrologueSectorId.CanyonAscent, new Rect2(0, 900, 480, 540), false),
        new("ScaffoldPendulumGallery", PrologueSectorId.CanyonAscent, new Rect2(320, 360, 700, 900), false),
        new("CeilingRailHighRuin", PrologueSectorId.CanyonAscent, new Rect2(760, 0, 520, 560), false),
        new("OuterSealGuardWorkshop", PrologueSectorId.LostTemple, new Rect2(0, 0, 760, 720), false),
        new("BellAntechamberBossHall", PrologueSectorId.LostTemple, new Rect2(760, 0, 1160, 720), false)
    ];

    private static void AssertExactMicroSceneCatalog()
    {
        IReadOnlyList<CanyonMicroSceneDefinition> actual = CanyonContentCatalog.All;
        TestAssert.Equal(15, actual.Count, "dense canyon catalog contains exactly fifteen definitions");
        TestAssert.Equal(15, actual.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count(),
            "dense canyon catalog contains no duplicate ID");
        for (int index = 0; index < ExpectedMicroScenes.Length; index++)
        {
            ExpectedMicroScene expected = ExpectedMicroScenes[index];
            CanyonMicroSceneDefinition definition = actual[index];
            TestAssert.Equal(expected.Id, definition.Id, $"catalog item {index} has exact ID");
            TestAssert.Equal(expected.SectorId, definition.SectorId,
                $"catalog item {expected.Id} has exact sector");
            TestAssert.Equal(expected.LocalBounds, definition.LocalBounds,
                $"catalog item {expected.Id} has exact local bounds");
            TestAssert.Equal(expected.OptionalSubarea, definition.OptionalSubarea,
                $"catalog item {expected.Id} has exact optional flag");
        }
        TestAssert.Equal("ArchiveAlcove", actual.Single(item => item.OptionalSubarea).Id,
            "ArchiveAlcove is the catalog's only optional subarea");
    }

    private static void AssertMicroSceneMarkers(SectorCase expected, PrologueSector sector,
        ICollection<string> observed)
    {
        Node markers = sector.GetNodeOrNull("Gameplay/MicroScenes");
        TestAssert.True(markers != null, $"{expected.Id} exposes stable Gameplay/MicroScenes root");
        ExpectedMicroScene[] definitions = ExpectedMicroScenes
            .Where(item => item.SectorId == expected.Id).ToArray();
        TestAssert.Equal(definitions.Length, markers.GetChildCount(),
            $"{expected.Id} contains only its exact catalog markers");
        foreach (ExpectedMicroScene definition in definitions)
        {
            Marker2D marker = markers.GetNodeOrNull<Marker2D>(definition.Id);
            TestAssert.True(marker != null && definition.LocalBounds.HasPoint(marker.Position),
                $"{definition.Id} marker exists inside its literal local bounds");
            TestAssert.True(expected.Bounds.Encloses(definition.LocalBounds),
                $"{definition.Id} local bounds remain inside {expected.Id}");
            TestAssert.Equal(definition.OptionalSubarea,
                marker.GetMeta("optional_subarea", false).AsBool(),
                $"{definition.Id} marker carries its explicit optional identity");
            observed.Add(definition.Id);
        }
    }

    private static void AssertTypedGameplayBodies(SectorCase expected, Node gameplay, Node visual)
    {
        StaticBody2D[] bodies = Descendants(gameplay).OfType<StaticBody2D>()
            .Where(body => !BelongsToInstancedSubscene(body, gameplay)).ToArray();
        TestAssert.True(bodies.Length > 0, $"{expected.Id} contains generated gameplay bodies");
        foreach (StaticBody2D body in bodies)
        {
            GrappleSurface2D profile = body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
            TestAssert.True(profile != null && profile.UseCollisionHitPoint,
                $"{expected.Id}/{body.Name} owns a typed collision-hit profile");
            TestAssert.True(profile.Material is GrappleSurfaceMaterial.GripStone or
                    GrappleSurfaceMaterial.TempleMetal or GrappleSurfaceMaterial.Ice or
                    GrappleSurfaceMaterial.Forbidden,
                $"{expected.Id}/{body.Name} uses an explicit terrain material");
            TestAssert.Equal(CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
                body.CollisionLayer,
                $"{expected.Id}/{body.Name} participates in world and thread-target queries");
        }
        TestAssert.Equal(0, Descendants(visual).OfType<GrappleSurface2D>().Count(),
            $"{expected.Id} formal/replaceable art owns no collision or claw rules");
    }

    private static bool BelongsToInstancedSubscene(Node node, Node gameplay)
    {
        for (Node current = node.GetParent(); current != null && current != gameplay;
             current = current.GetParent())
            if (!string.IsNullOrEmpty(current.SceneFilePath)) return true;
        return false;
    }

    private static void AssertCatchLayer(PrologueSectorId id, PrologueSector sector,
        TraversalSegment2D segment)
    {
        string path = segment.SegmentId.ToString() switch
        {
            "descent_grip_lesson" => "Gameplay/Collision/GripStoneCatchZone",
            "ascent_grapple_one" => "Gameplay/Collision/CatchOne",
            "ascent_grapple_two" => "Gameplay/Collision/CatchTwo",
            "ascent_grapple_three" => "Gameplay/Collision/CatchThree",
            _ => throw new InvalidOperationException($"missing catch-layer authority for {segment.SegmentId}")
        };
        StaticBody2D catchLayer = sector.GetNodeOrNull<StaticBody2D>(path);
        TestAssert.True(catchLayer != null && catchLayer.HasMeta("catch_shelf"),
            $"{id}/{segment.SegmentId} exposes its named catch platform");
        float verticalDrop = catchLayer.Position.Y - segment.LaunchZone.GetCenter().Y;
        TestAssert.True(verticalDrop >= 0f && verticalDrop <= 320f,
            $"{id}/{segment.SegmentId} catch layer is within 320 vertical px below launch; " +
            $"actual={verticalDrop:0.00}");
        if (id == PrologueSectorId.CanyonAscent)
        {
            StaticBody2D returnSurface = sector.GetNode("Gameplay/Collision").GetChildren()
                .OfType<StaticBody2D>().SingleOrDefault(body =>
                    body.HasMeta("catch_return_for") &&
                    body.GetMeta("catch_return_for").AsString() == catchLayer.Name.ToString());
            GrappleSurface2D profile = returnSurface?.GetNodeOrNull<GrappleSurface2D>(
                "GrappleSurfaceProfile");
            TestAssert.True(returnSurface != null && profile != null && profile.UseCollisionHitPoint,
                $"{id}/{segment.SegmentId} exposes one explicit typed collision-hit return surface");
            RectangleShape2D catchShape = catchLayer.GetNode<CollisionShape2D>("Collision").Shape as
                RectangleShape2D ?? throw new InvalidOperationException("catch layer must be rectangular");
            RectangleShape2D returnShape = returnSurface.GetNode<CollisionShape2D>("Collision").Shape as
                RectangleShape2D ?? throw new InvalidOperationException("catch return must be rectangular");
            Vector2 retryStart = catchLayer.Position -
                new Vector2(0f, catchShape.Size.Y / 2f + 36f);
            Vector2 returnHit = FirstRayHitOnRectangle(retryStart, returnSurface.Position,
                returnSurface.Position, returnShape.Size);
            float returnDistance = retryStart.DistanceTo(returnHit);
            TestAssert.True(returnDistance >= 360f && returnDistance <= 520f,
                $"{id}/{segment.SegmentId} typed return collision hit on its authored center ray stays " +
                $"inside [360,520]; actual={returnDistance:0.00} hit={returnHit}");
        }
    }

    private static Vector2 FirstRayHitOnRectangle(Vector2 origin, Vector2 target,
        Vector2 center, Vector2 size)
    {
        Vector2 direction = target - origin;
        float entry = 0f;
        float exit = 1f;
        ClipRayAxis(origin.X, direction.X, center.X - size.X / 2f, center.X + size.X / 2f,
            ref entry, ref exit);
        ClipRayAxis(origin.Y, direction.Y, center.Y - size.Y / 2f, center.Y + size.Y / 2f,
            ref entry, ref exit);
        TestAssert.True(entry <= exit + .0001f && entry <= 1f && exit >= 0f,
            "authored return center ray intersects its authoritative collision rectangle");
        return origin + direction * Mathf.Clamp(entry, 0f, 1f);
    }

    private static void ClipRayAxis(float origin, float delta, float minimum, float maximum,
        ref float entry, ref float exit)
    {
        if (Mathf.IsZeroApprox(delta))
        {
            TestAssert.True(origin >= minimum && origin <= maximum,
                "parallel authored return ray remains inside collision bounds");
            return;
        }
        float first = (minimum - origin) / delta;
        float second = (maximum - origin) / delta;
        entry = Mathf.Max(entry, Mathf.Min(first, second));
        exit = Mathf.Min(exit, Mathf.Max(first, second));
    }

    private static Vector2 PositionInSector(PrologueSector sector, GrappleSurface2D node)
    {
        Vector2 point = node.AnchorOffset;
        for (Node current = node; current != null && current != sector; current = current.GetParent())
            if (current is Node2D canvas)
                point = canvas.Transform * point;
        return point;
    }

    private static void AssertCriticalSurfaceClearOfSeams(SectorCase sector, Vector2 point)
    {
        float left = point.X - sector.Bounds.Position.X;
        float right = sector.Bounds.End.X - point.X;
        float top = point.Y - sector.Bounds.Position.Y;
        float bottom = sector.Bounds.End.Y - point.Y;
        switch (sector.Id)
        {
            case PrologueSectorId.CanyonEntrance:
                TestAssert.True(right >= SeamMargin, "entrance grapple surfaces clear the forward seam");
                break;
            case PrologueSectorId.CanyonDescent:
                TestAssert.True(left >= SeamMargin && bottom >= SeamMargin,
                    "descent grapple surfaces clear both unload seams");
                break;
            case PrologueSectorId.CanyonBasin:
                TestAssert.True(top >= SeamMargin && right >= SeamMargin,
                    "basin grapple surfaces clear both unload seams");
                break;
            case PrologueSectorId.CanyonAscent:
                TestAssert.True(left >= SeamMargin && right >= SeamMargin,
                    "ascent grapple surfaces clear both unload seams");
                break;
            case PrologueSectorId.LostTemple:
                TestAssert.True(left >= SeamMargin, "temple grapple surfaces clear the entry seam");
                break;
        }
    }

    private static void AssertAuthoredContent(PrologueSectorId id, PrologueSector sector)
    {
        switch (id)
        {
            case PrologueSectorId.CanyonEntrance:
                RustShellSquire rust = sector.GetNodeOrNull<RustShellSquire>(
                    "Gameplay/Actors/RustShellSquire");
                TestAssert.True(rust != null && rust.HasMeta("encounter_pressure") &&
                    rust.GetMeta("encounter_pressure").AsString() == "low",
                    "entrance contains exactly one explicitly low-pressure Rust-Shell lesson");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<RustShellSquire>().Count(),
                    "entrance contains only one Rust-Shell enemy");
                break;
            case PrologueSectorId.CanyonDescent:
                AssertSurface(sector, "Gameplay/GrappleSurfaces/GripStoneLesson",
                    GrappleSurfaceMaterial.GripStone);
                AssertSurface(sector, "Gameplay/GrappleSurfaces/DedicatedLessonAnchor",
                    GrappleSurfaceMaterial.DedicatedAnchor);
                AssertSurface(sector, "Gameplay/GrappleSurfaces/VisibleIceRejection",
                    GrappleSurfaceMaterial.Ice);
                TestAssert.True(sector.GetNode("Gameplay/Collision").GetChildren()
                    .Count(node => node.HasMeta("catch_shelf")) >= 2,
                    "descent places catch platforms below every required attempt");
                AssertTerrainProfile(sector, "Gameplay/Collision/CraneShaftWall",
                    GrappleSurfaceMaterial.GripStone);
                AssertTerrainProfile(sector, "Gameplay/Collision/FrostVentCeiling",
                    GrappleSurfaceMaterial.GripStone);
                AssertTerrainProfile(sector, "Gameplay/Collision/VisibleIceWall",
                    GrappleSurfaceMaterial.Ice);
                AssertMechanism(sector, "Gameplay/Mechanisms/Counterweight",
                    CanyonClawMechanismKind.Counterweight);
                AssertMechanism(sector, "Gameplay/Mechanisms/HiddenPanel",
                    CanyonClawMechanismKind.HiddenPanel);
                AssertOptionalPath(sector, "ArchiveAlcove");
                break;
            case PrologueSectorId.CanyonBasin:
                TestAssert.True(sector.GetNodeOrNull<Area2D>("Gameplay/ShieldLane") != null,
                    "basin contains the right-mouse shield lane");
                TestAssert.True(sector.GetNodeOrNull<CheckpointTrigger>("Gameplay/Checkpoint") != null,
                    "basin contains the first formal checkpoint");
                TestAssert.True(sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/PassStart") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/PassEnd") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/SkipStart") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/SkipEnd") != null,
                    "supply cave exposes independent pass and skip routes that rejoin mainline");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<RustShellSquire>().Count(),
                    "basin reuses exactly one Rust-Shell for mixed low-pressure combat");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<BellBackLaborer>().Count(),
                    "basin reuses exactly one Laborer for mixed low-pressure combat");
                AssertMechanism(sector, "Gameplay/Mechanisms/CargoPlatform",
                    CanyonClawMechanismKind.CargoPlatform);
                AssertOptionalPath(sector, "SalvageCache");
                break;
            case PrologueSectorId.CanyonAscent:
                TestAssert.True(sector.GetNode("Gameplay/Collision").GetChildren()
                    .Count(node => node.HasMeta("catch_shelf")) >= 3,
                    "ascent has a lower catch shelf for each assisted grapple");
                TestAssert.True(sector.GetNodeOrNull("Gameplay/OptionalPaths/HighRuin/PassStart") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/HighRuin/SkipEnd") != null,
                    "ascent high ruin is an explicit non-blocking optional detour");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<GraveLampMoth>().Count(),
                    "ascent reuses one Moth sightline without adding a mandatory combat wave");
                break;
            case PrologueSectorId.LostTemple:
                TestAssert.True(sector.GetNodeOrNull<BellChainMechanism>(
                    "Gameplay/Mechanisms/BellChain") != null,
                    "temple reuses the claw-compatible Bell Chain mechanism");
                TestAssert.True(sector.GetNodeOrNull<CheckpointTrigger>(
                    "Gameplay/TempleCheckpoint") != null,
                    "temple contains the second formal checkpoint");
                BossArenaController arena = sector.GetNodeOrNull<BossArenaController>(
                    "Gameplay/BossArenaController");
                Area2D trigger = sector.GetNodeOrNull<Area2D>("Gameplay/ActivationTrigger");
                TestAssert.True(arena?.BossScene != null && trigger != null &&
                    sector.GetNodeOrNull("Gameplay/Doors/Entry/Collision") != null,
                    "temple reuses the Fallen Bellkeeper arena behind an authored activation trigger");
                AssertMechanism(sector, "Gameplay/Mechanisms/BellBody",
                    CanyonClawMechanismKind.BellBody);
                break;
        }
    }

    private static void AssertTerrainProfile(Node root, string bodyPath,
        GrappleSurfaceMaterial material)
    {
        StaticBody2D body = root.GetNode<StaticBody2D>(bodyPath);
        GrappleSurface2D profile = body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        TestAssert.True(profile.UseCollisionHitPoint && profile.Material == material,
            $"{bodyPath} owns literal typed terrain material {material}");
    }

    private static void AssertMechanism(Node root, string path, CanyonClawMechanismKind kind)
    {
        CanyonClawMechanism2D mechanism = root.GetNodeOrNull<CanyonClawMechanism2D>(path);
        TestAssert.True(mechanism != null && mechanism.Kind == kind &&
            mechanism.GetNodeOrNull<ThreadTarget2D>("ThreadTarget") != null &&
            mechanism.GetNodeOrNull("LinkedNode") != null,
            $"{path} exposes the literal {kind} one-shot claw mechanism contract");
    }

    private static void AssertOptionalPath(Node root, string name)
    {
        Node path = root.GetNodeOrNull($"Gameplay/OptionalPaths/{name}");
        TestAssert.True(path?.GetNodeOrNull<Marker2D>("PassStart") != null &&
            path.GetNodeOrNull<Marker2D>("PassEnd") != null &&
            path.GetNodeOrNull<Marker2D>("SkipStart") != null &&
            path.GetNodeOrNull<Marker2D>("SkipEnd") != null,
            $"{name} exposes pass/skip markers and therefore cannot gate mainline completion");
    }

    private static void AssertSurface(Node root, string path, GrappleSurfaceMaterial material) =>
        TestAssert.Equal(material, root.GetNode<GrappleSurface2D>(path).Material,
            $"{path} exposes literal {material} gameplay identity");

    private static void AssertMainComposition(PackedScene packed)
    {
        TestAssert.True(packed != null, "generated canyon main scene loads");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try
        {
            SeamlessPrologueWorld world = level.GetNodeOrNull<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            TestAssert.True(world != null && world.Definitions.Count == Sectors.Length,
                "main scene composes exactly five authored canyon definitions");
            var definitions = new List<PrologueSectorDefinition>();
            for (int index = 0; index < Sectors.Length; index++)
            {
                SectorCase expected = Sectors[index];
                PrologueSectorDefinition definition = world.Definitions[index];
                TestAssert.Equal(expected.Id, definition.Id,
                    $"main definition {index} preserves route identity");
                TestAssert.Equal(expected.Bounds, definition.Bounds,
                    $"main definition {expected.Id} preserves local bounds");
                TestAssert.Equal(expected.WorldOffset, definition.WorldOffset,
                    $"main definition {expected.Id} preserves U-shaped world offset");
                TestAssert.Equal(expected.Path, definition.Scene.ResourcePath,
                    $"main definition {expected.Id} references the authored sector resource");
                TestAssert.True(definition.KeyInteractionBounds.Count > 0,
                    $"main definition {expected.Id} declares critical interaction bounds");
                definitions.Add(definition);
            }
            _ = new PrologueSectorStreamModel(definitions);
        }
        finally
        {
            level.Free();
        }
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed record SectorCase(PrologueSectorId Id, string Path, Rect2 Bounds,
        Vector2 WorldOffset, string[] Segments);

    private sealed record ExpectedMicroScene(string Id, PrologueSectorId SectorId,
        Rect2 LocalBounds, bool OptionalSubarea);
}
