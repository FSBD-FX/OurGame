using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Enemies;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Integration;

public static class PrologueCanyonLegacyReplacementTests
{
    private static readonly string[] Paths =
    [
        "res://scenes/prologue/sectors/CanyonEntrance.tscn",
        "res://scenes/prologue/sectors/CanyonDescent.tscn",
        "res://scenes/prologue/sectors/CanyonBasin.tscn",
        "res://scenes/prologue/sectors/CanyonAscent.tscn",
        "res://scenes/prologue/sectors/LostTemple.tscn"
    ];

    private static readonly Vector2[] Offsets =
    [
        Vector2.Zero, new(1280, 0), new(1280, 1440), new(2880, 720), new(4160, 720)
    ];

    public static void PersistentPlayerAndFiveSectorContent()
    {
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        try
        {
            TestAssert.True(player.GetNodeOrNull<PlayerInputReader>("Input") != null &&
                player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw") != null &&
                player.GetNodeOrNull<Camera2D>("Camera2D") != null,
                "continuous canyon keeps one complete persistent player contract");
            TestAssert.Equal(5, Paths.Count(path => ResourceLoader.Exists(path)),
                "all five formal canyon sector resources exist independently");
        }
        finally
        {
            player.Free();
        }
    }

    public static void FiveSectorSeamsFormOneContinuousU()
    {
        var sectors = Paths.Select(Load).ToArray();
        try
        {
            for (int index = 0; index < sectors.Length - 1; index++)
            {
                Vector2 next = Offsets[index] + sectors[index].GetNode<Marker2D>(
                    "Gameplay/RouteMarkers/NextSeam").Position;
                Vector2 previous = Offsets[index + 1] + sectors[index + 1].GetNode<Marker2D>(
                    "Gameplay/RouteMarkers/PreviousSeam").Position;
                TestAssert.True(next.DistanceTo(previous) <= 96f,
                    $"continuous U seam {index + 1} remains inside the authored streaming corridor; " +
                    $"next={next} previous={previous}");
            }
        }
        finally
        {
            FreeAll(sectors);
        }
    }

    public static void ExactMandatoryRouteConvergesOnTemple()
    {
        string[] expected =
        [
            "entrance_move_attack", "descent_grip_lesson", "descent_ice_rejection",
            "basin_shield_lane", "basin_checkpoint", "ascent_grapple_one",
            "ascent_grapple_two", "ascent_grapple_three", "temple_mechanism",
            "temple_checkpoint", "temple_boss_entry"
        ];
        var actual = new List<string>();
        foreach (string path in Paths)
        {
            PrologueSector sector = Load(path);
            try
            {
                foreach (TraversalSegment2D segment in sector.GetNode("Gameplay/TraversalContracts")
                    .GetChildren().OfType<TraversalSegment2D>())
                {
                    actual.Add(segment.SegmentId.ToString());
                    TestAssert.True((segment.RequiredAbilities & TraversalAbility.Dash) == 0,
                        $"{segment.SegmentId} replacement route never requires Dash");
                }
            }
            finally
            {
                sector.Free();
            }
        }
        TestAssert.Equal(string.Join("|", expected), string.Join("|", actual),
            "exact eleven-segment route converges on Lost Temple");
    }

    public static void WrongAndValidDescentTargetsStayDistinct()
    {
        WithSector(1, sector =>
        {
            GrappleSurfaceMaterial[] materials =
            [
                sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/GripStoneLesson").Material,
                sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/DedicatedLessonAnchor").Material,
                sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/VisibleIceRejection").Material
            ];
            TestAssert.Equal("GripStone|DedicatedAnchor|Ice", string.Join("|", materials),
                "valid, assisted and rejected descent targets remain typed and distinct");
        });
    }

    public static void RecoveryClearsPendingSeamDanger()
    {
        WithSector(1, sector =>
        {
            Marker2D seam = sector.GetNode<Marker2D>("Gameplay/RouteMarkers/NextSeam");
            foreach (Area2D area in sector.GetNode("Gameplay/FallRecovery").GetChildren().OfType<Area2D>())
            {
                var shape = area.GetNode<CollisionShape2D>("Collision").Shape as RectangleShape2D;
                Rect2 bounds = new(area.Position - shape.Size / 2f, shape.Size);
                TestAssert.True(!bounds.HasPoint(seam.Position),
                    "Descent recovery authority does not consume the Basin seam");
            }
        });
    }

    public static void IceFailureTargetRemainsVisibleAndCollidable()
    {
        WithSector(1, sector =>
        {
            GrappleSurface2D ice = sector.GetNode<GrappleSurface2D>(
                "Gameplay/GrappleSurfaces/VisibleIceRejection");
            TestAssert.True(ice.Material == GrappleSurfaceMaterial.Ice &&
                ice.GetNode<Area2D>("TargetCollision").CollisionLayer != 0,
                "visible Ice lesson remains a real resolver target with a named rejection material");
        });
    }

    public static void DescentApplyAndLandingContractIsComplete()
    {
        WithSector(1, sector =>
        {
            TraversalSegment2D segment = sector.GetNode<TraversalSegment2D>(
                "Gameplay/TraversalContracts/descent_grip_lesson");
            TestAssert.True(segment.Kind == TraversalSegmentKind.Assisted &&
                sector.GetNodeOrNull<GrappleSurface2D>(segment.AssistTargetPath) != null &&
                sector.Bounds.Encloses(segment.LandingZone),
                "descent apply owns an explicit assist target and safe landing zone");
        });
    }

    public static void CompletedAscentTargetsStayUnique()
    {
        WithSector(3, sector =>
        {
            ulong[] ids = sector.GetNode("Gameplay/TraversalContracts").GetChildren()
                .OfType<TraversalSegment2D>().Where(segment => segment.Kind == TraversalSegmentKind.Assisted)
                .Select(segment => sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath)
                    .GetInstanceId()).ToArray();
            TestAssert.True(ids.Length == 3 && ids.Distinct().Count() == 3,
                "three completed ascent lessons retain three unique assist targets");
        });
    }

    public static void ExpandedTeachingIsLimitedToTheEntranceBeat()
    {
        WithSector(0, sector =>
        {
            TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                .OfType<RustShellSquire>().Count(),
                "entrance expanded teaching pressure is limited to one Rust-Shell");
            TestAssert.Equal(1, sector.GetNode("Gameplay/TraversalContracts").GetChildCount(),
                "entrance exposes one focused move-and-attack teaching segment");
        });
    }

    public static void RecoveryAndCheckpointAuthoritiesAreExplicit()
    {
        int recoveries = 0;
        foreach (string path in Paths)
        {
            PrologueSector sector = Load(path);
            try
            {
                recoveries += sector.GetNodeOrNull<Marker2D>(
                    "Gameplay/FallRecovery/RecoveryLanding") == null ? 0 : 1;
            }
            finally
            {
                sector.Free();
            }
        }
        TestAssert.Equal(5, recoveries, "each sector owns one explicit recovery landing authority");
    }

    public static void BothFormalCheckpointsExist()
    {
        WithSector(2, basin => TestAssert.True(basin.GetNodeOrNull<CheckpointTrigger>(
                "Gameplay/Checkpoint") != null, "Basin checkpoint exists"));
        WithSector(4, temple => TestAssert.True(temple.GetNodeOrNull<CheckpointTrigger>(
                "Gameplay/TempleCheckpoint") != null, "Temple checkpoint exists"));
    }

    public static void CriticalPathOrdersMechanismBeforeBoss()
    {
        WithSector(4, sector =>
        {
            float mechanismX = sector.GetNode<BellChainMechanism>(
                "Gameplay/Mechanisms/BellChain").Position.X;
            float triggerX = sector.GetNode<Area2D>("Gameplay/ActivationTrigger").Position.X;
            TestAssert.True(mechanismX < triggerX,
                "critical path orders the BellChain shortcut before the Boss activation trigger");
        });
    }

    public static void FreshShortcutExposesOpenedSignalAndClosedGate()
    {
        WithSector(4, sector =>
        {
            BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
                "Gameplay/Mechanisms/BellChain");
            TestAssert.True(chain.HasSignal(BellChainMechanism.SignalName.Opened) && !chain.IsOpen &&
                !chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath).Disabled,
                "fresh BellChain exposes the opening signal and starts physically closed");
        });
    }

    public static void EveryAssistPathResolvesWithoutMissingTarget()
    {
        foreach (string path in Paths)
        {
            PrologueSector sector = Load(path);
            try
            {
                foreach (TraversalSegment2D segment in sector.GetNode("Gameplay/TraversalContracts")
                    .GetChildren().OfType<TraversalSegment2D>().Where(item =>
                        item.Kind == TraversalSegmentKind.Assisted))
                    TestAssert.True(!segment.AssistTargetPath.IsEmpty &&
                        sector.GetNodeOrNull<GrappleSurface2D>(segment.AssistTargetPath) != null,
                        $"{segment.SegmentId} has no missing guidance target");
            }
            finally
            {
                sector.Free();
            }
        }
    }

    public static void BossCompletionAuthorityIsFullyWired()
    {
        WithSector(4, sector =>
        {
            BossArenaController arena = sector.GetNode<BossArenaController>(
                "Gameplay/BossArenaController");
            TestAssert.True(arena.BossScene?.ResourcePath.EndsWith("FallenBellkeeper.tscn",
                    StringComparison.Ordinal) == true &&
                sector.GetNodeOrNull<Area2D>("Gameplay/ActivationTrigger") != null &&
                sector.GetNodeOrNull<CollisionShape2D>("Gameplay/Doors/Exit/Collision") != null,
                "Lost Temple owns real Fallen Bellkeeper activation and completion-exit authority");
        });
    }

    public static void InspectableRouteEvidenceSourcesArePacked()
    {
        int markers = 0;
        foreach (string path in Paths)
        {
            PrologueSector sector = Load(path);
            try
            {
                markers += sector.GetNode("Gameplay/RouteMarkers").GetChildCount();
                TestAssert.True(sector.GetNodeOrNull<Label>("VisualRoot/Effects/SectorLabel") != null,
                    $"{sector.SectorId} packs an inspectable evidence label");
            }
            finally
            {
                sector.Free();
            }
        }
        TestAssert.Equal(8, markers, "five-sector route packs all eight directional seam markers");
    }

    public static void SectorRetirementHasTypedClawTargets()
    {
        int targetCount = 0;
        foreach (string path in Paths)
        {
            PrologueSector sector = Load(path);
            try
            {
                foreach (GrappleSurface2D surface in Descendants(sector).OfType<GrappleSurface2D>())
                {
                    targetCount++;
                    TestAssert.Equal(0, surface.Validate().Count,
                        $"{sector.SectorId}/{surface.Name} remains safe for typed unload cleanup");
                }
            }
            finally
            {
                sector.Free();
            }
        }
        TestAssert.True(targetCount >= 9, "continuous sectors expose typed targets for unload cleanup");
    }

    public static void PersistentClawAndSectorTargetsShareTheRuntimeContract()
    {
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        try
        {
            TestAssert.True(player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw") != null,
                "persistent player owns the typed mechanical-claw cleanup authority");
            WithSector(4, sector => TestAssert.True(sector.GetNodeOrNull<GrappleSurface2D>(
                    "Gameplay/Mechanisms/BellChain/ThreadTarget") != null,
                "streamed Temple mechanism exposes a typed grapple target"));
        }
        finally
        {
            player.Free();
        }
    }

    private static PrologueSector Load(string path) =>
        GD.Load<PackedScene>(path).Instantiate<PrologueSector>();

    private static void WithSector(int index, Action<PrologueSector> assertion)
    {
        PrologueSector sector = Load(Paths[index]);
        try { assertion(sector); }
        finally { sector.Free(); }
    }

    private static void FreeAll(IEnumerable<PrologueSector> sectors)
    {
        foreach (PrologueSector sector in sectors) sector.Free();
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }
}
