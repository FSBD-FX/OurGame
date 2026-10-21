using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.World;

namespace Knight.Tests.Integration;

public static class PrologueCanyonFixRound1Tests
{
    private const float SeamHalfWidth = 80f;
    private const float SeamHalfHeight = 120f;

    private static readonly string[] SectorPaths =
    [
        "res://scenes/prologue/sectors/CanyonEntrance.tscn",
        "res://scenes/prologue/sectors/CanyonDescent.tscn",
        "res://scenes/prologue/sectors/CanyonBasin.tscn",
        "res://scenes/prologue/sectors/CanyonAscent.tscn",
        "res://scenes/prologue/sectors/LostTemple.tscn"
    ];

    public static void RunStructural()
    {
        var failures = new List<string>();
        RunContract(RecoveryAreasCarveEveryAuthoredSeamCorridor, failures);
        RunContract(BellChainDoorBlocksTheFloorRouteUntilMechanicalClawInteraction, failures);
        RunContract(BasinShieldLaneHasProductionHazardAuthority, failures);
        RunContract(AscentLaunchDistancesIncreaseForLandingsAndAnchors, failures);
        if (failures.Count > 0)
            throw new InvalidOperationException("Task8 Fix1 structural failures:\n" +
                string.Join("\n", failures));
        GD.Print("TASK8_FIX1_STRUCTURE_OK recovery_carves=4 bell_gate=floor shield=behavior ascent=increasing");
    }

    private static void RunContract(Action contract, ICollection<string> failures)
    {
        try
        {
            contract();
        }
        catch (Exception exception)
        {
            failures.Add($"{contract.Method.Name}: {exception.Message}");
        }
    }

    private static void RecoveryAreasCarveEveryAuthoredSeamCorridor()
    {
        foreach (string path in SectorPaths)
        {
            PrologueSector sector = GD.Load<PackedScene>(path).Instantiate<PrologueSector>();
            try
            {
                Area2D[] recoveryAreas = sector.GetNode("Gameplay/FallRecovery")
                    .GetChildren().OfType<Area2D>().ToArray();
                TestAssert.True(recoveryAreas.Length > 0,
                    $"{sector.SectorId} retains authoritative fall recovery away from seams");
                foreach (Marker2D seam in sector.GetNode("Gameplay/RouteMarkers")
                    .GetChildren().OfType<Marker2D>())
                {
                    bool bottom = Mathf.Abs(seam.Position.Y - sector.Bounds.End.Y) <= 24f;
                    bool side = Mathf.Abs(seam.Position.X - sector.Bounds.Position.X) <= 24f ||
                        Mathf.Abs(seam.Position.X - sector.Bounds.End.X) <= 24f;
                    if (!bottom || side) continue;
                    Rect2 corridor = new(seam.Position - new Vector2(SeamHalfWidth, SeamHalfHeight),
                        new Vector2(SeamHalfWidth * 2f, SeamHalfHeight * 2f));
                    foreach (Area2D recovery in recoveryAreas)
                    {
                        CollisionShape2D collision = recovery.GetNodeOrNull<CollisionShape2D>("Collision");
                        if (collision?.Shape is not RectangleShape2D rectangle) continue;
                        Rect2 recoveryRect = new(recovery.Position + collision.Position -
                            rectangle.Size / 2f, rectangle.Size);
                        TestAssert.True(!recoveryRect.Intersects(corridor, includeBorders: true),
                            $"{sector.SectorId}/{recovery.Name} carves a player-and-camera-safe " +
                            $"corridor around {seam.Name}; recovery={recoveryRect} seam={corridor}");
                    }
                }
            }
            finally
            {
                sector.Free();
            }
        }
    }

    private static void BellChainDoorBlocksTheFloorRouteUntilMechanicalClawInteraction()
    {
        PrologueSector sector = GD.Load<PackedScene>(SectorPaths[4]).Instantiate<PrologueSector>();
        try
        {
            BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
                "Gameplay/Mechanisms/BellChain");
            chain.ApplyConfiguredDoorGeometry();
            CollisionShape2D collision = chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath);
            TestAssert.True(collision.Shape is RectangleShape2D,
                "BellChain shortcut owns a real rectangular collision gate");
            var shape = (RectangleShape2D)collision.Shape;
            Vector2 center = chain.Position + collision.GetParent<Node2D>().Position + collision.Position;
            float top = center.Y - shape.Size.Y / 2f;
            float bottom = center.Y + shape.Size.Y / 2f;
            TestAssert.True(top <= 190f && bottom >= 610f,
                "closed BellChain door reaches from the temple ceiling to the mandatory floor; " +
                $"top={top:0.0} bottom={bottom:0.0} size={shape.Size}");
            GrappleSurface2D target = chain.GetNode<GrappleSurface2D>("ThreadTarget");
            TestAssert.True(target.Material == GrappleSurfaceMaterial.TempleMetal &&
                target.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                    "LegacyThreadInteractionAdapter") != null,
                "BellChain gate is opened by the real E-mode mechanical-claw legacy adapter");
        }
        finally
        {
            sector.Free();
        }
    }

    private static void BasinShieldLaneHasProductionHazardAuthority()
    {
        PrologueSector sector = GD.Load<PackedScene>(SectorPaths[2]).Instantiate<PrologueSector>();
        try
        {
            Area2D lane = sector.GetNode<Area2D>("Gameplay/ShieldLane");
            Script script = lane.GetScript().AsGodotObject() as Script;
            TestAssert.True(script?.ResourcePath ==
                "res://scripts/prologue/traversal/CanyonShieldLaneHazard.cs",
                "Basin shield lane owns production pulse/damage authority instead of metadata only; " +
                $"script={script?.ResourcePath ?? "<none>"}");
            TestAssert.True(lane.CollisionMask != 0 && lane.Monitoring,
                "Basin shield hazard monitors actual player lane entry and crossing");
        }
        finally
        {
            sector.Free();
        }
    }

    private static void AscentLaunchDistancesIncreaseForLandingsAndAnchors()
    {
        PrologueSector sector = GD.Load<PackedScene>(SectorPaths[3]).Instantiate<PrologueSector>();
        try
        {
            TraversalSegment2D[] segments = sector.GetNode("Gameplay/TraversalContracts")
                .GetChildren().OfType<TraversalSegment2D>()
                .Where(segment => segment.SegmentId.ToString().StartsWith("ascent_grapple_",
                    StringComparison.Ordinal)).ToArray();
            TestAssert.Equal(3, segments.Length, "ascent exposes all three assisted segments");
            float[] landingDistances = segments.Select(segment =>
                segment.LaunchZone.GetCenter().DistanceTo(segment.LandingZone.GetCenter())).ToArray();
            float[] anchorDistances = segments.Select(segment =>
                segment.LaunchZone.GetCenter().DistanceTo(
                    sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath).Position)).ToArray();
            TestAssert.True(StrictlyIncreasing(landingDistances),
                "ascent launch-to-landing distances strictly increase one < two < three; " +
                string.Join(",", landingDistances.Select(value => value.ToString("0.0"))));
            TestAssert.True(StrictlyIncreasing(anchorDistances),
                "ascent launch-to-anchor distances strictly increase one < two < three; " +
                string.Join(",", anchorDistances.Select(value => value.ToString("0.0"))));
        }
        finally
        {
            sector.Free();
        }
    }

    private static bool StrictlyIncreasing(IReadOnlyList<float> values) =>
        values.Count == 3 && values[0] < values[1] && values[1] < values[2];
}
