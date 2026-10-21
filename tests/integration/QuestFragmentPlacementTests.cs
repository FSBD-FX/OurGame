using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Saved-artifact coverage only: no scene enters the tree, no builder is invoked,
// and no route, collection transaction or Boss admission is inferred from it.
public static class QuestFragmentPlacementTests
{
    private const float PlayerRadius = 20f;
    private const float PlayerHeight = 72f;
    private const float GeometryTolerance = .01f;

    // Literal, hand-checked authored supports. Do not derive the expected IDs,
    // offsets, optional classification or regions from the production catalog.
    private static readonly SectorCase[] Sectors =
    [
        new(PrologueSectorId.CanyonEntrance,
            "res://scenes/prologue/sectors/CanyonEntrance.tscn", new(0, 0)),
        new(PrologueSectorId.CanyonDescent,
            "res://scenes/prologue/sectors/CanyonDescent.tscn", new(1280, 0)),
        new(PrologueSectorId.CanyonBasin,
            "res://scenes/prologue/sectors/CanyonBasin.tscn", new(1280, 3600)),
        new(PrologueSectorId.CanyonAscent,
            "res://scenes/prologue/sectors/CanyonAscent.tscn", new(3840, 720)),
        new(PrologueSectorId.LostTemple,
            "res://scenes/prologue/sectors/LostTemple.tscn", new(5120, 720))
    ];

    private static readonly FragmentCase[] Fragments =
    [
        new("canyon.v2.fragment.entrance", PrologueSectorId.CanyonEntrance,
            CanyonRouteNodeId.LowCeilingBridge, false, "StartFloor",
            new(0, 636, 320, 40), new(280, 600)),
        new("canyon.v2.fragment.rift", PrologueSectorId.CanyonDescent,
            CanyonRouteNodeId.StaggeredRift, false, "RiftLowerCatch",
            new(1360, 300, 740, 24), new(1460, 264)),
        new("canyon.v2.fragment.abyss-floor", PrologueSectorId.CanyonDescent,
            CanyonRouteNodeId.ControlledAbyss, false, "AbyssSafeBottom",
            new(1920, 3560, 640, 40), new(2240, 3524)),
        new("canyon.v2.fragment.gear-gallery", PrologueSectorId.CanyonBasin,
            CanyonRouteNodeId.GearPistons, false, "UpperGearGalleryOne",
            new(3040, 3756, 240, 40), new(3160, 3720)),
        new("canyon.v2.fragment.ascent-mid", PrologueSectorId.CanyonAscent,
            CanyonRouteNodeId.CounterweightLift, false, "AscentGrip2",
            new(4660, 3080, 140, 40), new(4730, 3044)),
        new("canyon.v2.fragment.courtyard", PrologueSectorId.LostTemple,
            CanyonRouteNodeId.SealedCourtyard, false, "CourtyardFloor",
            new(5120, 1240, 520, 40), new(5320, 1204)),
        new("canyon.v2.fragment.archive", PrologueSectorId.CanyonDescent,
            CanyonRouteNodeId.ArchiveSideCave, true, "ArchiveFloor",
            new(1280, 1640, 600, 40), new(1520, 1604)),
        new("canyon.v2.fragment.steam-loop", PrologueSectorId.CanyonBasin,
            CanyonRouteNodeId.SteamValves, true, "SteamTrenchFloor",
            new(3040, 4236, 1160, 40), new(3380, 4200)),
        new("canyon.v2.fragment.temple-upper", PrologueSectorId.LostTemple,
            CanyonRouteNodeId.SealedCourtyard, true, "TempleSupplyShelf",
            new(5865, 960, 215, 40), new(5896, 924))
    ];

    public static void Run()
    {
        var saved = new Dictionary<PrologueSectorId, PrologueSector>();
        try
        {
            foreach (SectorCase expected in Sectors)
            {
                PackedScene packed = GD.Load<PackedScene>(expected.Path);
                TestAssert.True(packed != null, $"saved fragment sector loads: {expected.Path}");
                PrologueSector sector = packed.Instantiate<PrologueSector>();
                saved.Add(expected.Id, sector);
                TestAssert.True(!sector.IsInsideTree(),
                    $"{expected.Id} saved-artifact fixture never runs Ready or runtime binding");
                TestAssert.Equal(expected.Id, sector.SectorId,
                    $"{expected.Path} keeps its authored sector identity");
                TestAssert.Equal(expected.Offset, CanyonWorldLayout.SectorBounds[expected.Id].Position,
                    $"{expected.Id} placement uses the literal continuous-world offset");
            }

            // RED names the missing production behavior before any optional seam
            // assertion: unbuilt/in-memory nodes cannot satisfy a saved artifact.
            QuestFragment[] all = saved.Values.SelectMany(Descendants)
                .OfType<QuestFragment>().ToArray();
            TestAssert.Equal(9, all.Length,
                "actual saved canyon sector PackedScenes contain exactly nine quest-fragment Areas");
            TestAssert.Equal(9, all.Select(fragment => fragment.GetInstanceId()).Distinct().Count(),
                "each saved quest fragment is its own independent Area instance");
            TestAssert.Equal(9, all.Select(fragment => fragment.StableId)
                .Distinct(StringComparer.Ordinal).Count(),
                "saved fragment stable IDs are globally unique across all five sectors");
            AssertIds(Fragments.Select(expected => expected.StableId), all.Select(fragment => fragment.StableId),
                "saved fragment IDs equal the independently approved nine-item inventory");
            AssertIds(Fragments.Select(expected => expected.StableId),
                QuestFragmentCatalog.Definitions.Select(definition => definition.StableId),
                "runtime catalog and saved inventory have no missing or extra IDs");
            TestAssert.Equal(6, QuestFragmentCatalog.Definitions.Count(definition => !definition.Optional),
                "saved inventory's directory reserves six main-route fragments");
            TestAssert.Equal(3, QuestFragmentCatalog.Definitions.Count(definition => definition.Optional),
                "saved inventory's directory reserves three optional fragments");

            AssertActualPlayerCapsule();
            foreach (SectorCase expected in Sectors)
            {
                PrologueSector sector = saved[expected.Id];
                QuestFragment[] actual = Descendants(sector).OfType<QuestFragment>().ToArray();
                TestAssert.Equal(Fragments.Count(fragment => fragment.SectorId == expected.Id), actual.Length,
                    $"{expected.Id} saves only the fragments assigned to that sector");
                foreach (FragmentCase fragment in Fragments.Where(fragment => fragment.SectorId == expected.Id))
                    AssertSavedPickup(sector, expected.Offset,
                        actual.Single(node => node.StableId == fragment.StableId), fragment);
            }
            AssertUpperGalleryRegions(saved[PrologueSectorId.CanyonBasin]);
            GD.Print("QUEST_FRAGMENT_PLACEMENT_OK saved_packed_scenes=5 fragments=9 main=6 optional=3 " +
                "outside_tree=true static_support_only=true normal_route=false");
        }
        finally
        {
            foreach (PrologueSector sector in saved.Values) sector.Free();
        }
    }

    private static void AssertSavedPickup(PrologueSector sector, Vector2 offset,
        QuestFragment pickup, FragmentCase expected)
    {
        string context = expected.StableId;
        Node2D container = sector.GetNodeOrNull<Node2D>("Gameplay/QuestFragments");
        TestAssert.True(container != null && pickup.GetParent() == container,
            $"{context} is independently serialized under Gameplay/QuestFragments, not a supply reward");
        QuestFragmentDefinition definition = QuestFragmentCatalog.Definitions.Single(
            definition => definition.StableId == expected.StableId);
        TestAssert.Equal(expected.Region, definition.Region, $"{context} directory region is exact");
        TestAssert.Equal(expected.Optional, definition.Optional, $"{context} directory optional identity is exact");

        Vector2 actualCenter = PositionInSector(sector, pickup) + offset;
        TestAssert.Near(expected.Center.X, actualCenter.X, GeometryTolerance,
            $"{context} saves its supported world x, not an unconverted world-space local position");
        TestAssert.Near(expected.Center.Y, actualCenter.Y, GeometryTolerance,
            $"{context} saves its supported world y, not an unconverted world-space local position");
        TestAssert.Equal(0u, pickup.CollisionLayer, $"{context} does not add world or attack collision");
        TestAssert.Equal(2u, pickup.CollisionMask, $"{context} serialized contact listens only for the player body");
        TestAssert.True(pickup.Monitoring, $"{context} is collectible without relying on Ready to enable monitoring");

        CollisionShape2D[] contactShapes = pickup.GetChildren().OfType<CollisionShape2D>().ToArray();
        TestAssert.Equal(1, contactShapes.Length, $"{context} owns one separate contact collision shape");
        CollisionShape2D contact = contactShapes[0];
        TestAssert.True(!contact.Disabled && contact.Shape is CircleShape2D { Radius: > 0f and <= PlayerRadius },
            $"{context} saves an enabled, bounded contact circle independent of the visual");
        TestAssert.Equal(Vector2.Zero, contact.Position, $"{context} contact circle remains at its supported center");
        Node2D visual = pickup.GetNodeOrNull<Node2D>(pickup.VisualPath);
        Label status = pickup.GetNodeOrNull<Label>(pickup.StatusPath);
        TestAssert.True(visual != null && visual.GetParent() == pickup && visual.Visible &&
            status != null && status.GetParent() == pickup,
            $"{context} saves separate replaceable available-visual and status-label interfaces");
        TestAssert.True(!Descendants(visual).Any(node => node is CollisionObject2D or CollisionShape2D) &&
            !Descendants(status).Any(node => node is CollisionObject2D or CollisionShape2D),
            $"{context} presentation replacements cannot own collection or world collision");

        StaticBody2D support = sector.GetNodeOrNull<StaticBody2D>($"Gameplay/Collision/{expected.SupportName}");
        TestAssert.True(support != null && (support.CollisionLayer & 1u) != 0,
            $"{context} has its actual saved world-collision support");
        CollisionShape2D supportShape = support.GetNodeOrNull<CollisionShape2D>("Collision");
        TestAssert.True(supportShape != null && !supportShape.Disabled,
            $"{context} support's actual saved collision is enabled");
        Rect2 actualSupport = WorldRectangle(sector, supportShape, offset);
        TestAssert.Equal(expected.Support, actualSupport,
            $"{context} support rectangle is read from saved shapes, not rebuilt or mocked");
        AssertCompleteCapsuleSupport(actualCenter, actualSupport, context);
        foreach (CollisionShape2D shape in Descendants(sector.GetNode("Gameplay/Collision"))
                     .OfType<CollisionShape2D>())
        {
            if (shape.Disabled || shape.GetParent() is not StaticBody2D body ||
                (body.CollisionLayer & 1u) == 0) continue;
            Rect2 rectangle = WorldRectangle(sector, shape, offset);
            TestAssert.True(!CapsuleInteriorOverlapsRectangle(actualCenter, rectangle),
                $"{context} complete 72x40 capsule has authored static clearance from {body.Name}");
        }
        TestAssert.True(CanyonWorldLayout.TryResolveRouteNode(actualCenter, out CanyonRouteNodeId region) &&
            region == expected.Region,
            $"{context} supported player center maps to its directory region {expected.Region}");
    }

    private static void AssertActualPlayerCapsule()
    {
        Node2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<Node2D>();
        try
        {
            CollisionShape2D body = player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(body.Shape is CapsuleShape2D,
                "placement clearance uses the real saved Player BodyCollider capsule");
            var capsule = (CapsuleShape2D)body.Shape;
            TestAssert.Equal(PlayerRadius, capsule.Radius, "placement proof keeps the real player's 20px radius");
            TestAssert.Equal(PlayerHeight, capsule.Height, "placement proof keeps the real player's 72px height");
            TestAssert.Equal(Vector2.Zero, body.Position, "real capsule center matches the tested player center");
            TestAssert.Equal(Vector2.One, body.Scale, "placement proof does not shrink the real player capsule");
        }
        finally { player.Free(); }
    }

    private static void AssertUpperGalleryRegions(PrologueSector basin)
    {
        var galleries = new[]
        {
            (Name: "UpperGearGalleryOne", Support: new Rect2(3040, 3756, 240, 40), Center: new Vector2(3160, 3720)),
            (Name: "UpperGearGalleryTwo", Support: new Rect2(3280, 3676, 240, 40), Center: new Vector2(3400, 3640)),
            (Name: "UpperGearGalleryThree", Support: new Rect2(3520, 3600, 320, 40), Center: new Vector2(3680, 3564))
        };
        foreach (var gallery in galleries)
        {
            CollisionShape2D shape = basin.GetNode<CollisionShape2D>(
                $"Gameplay/Collision/{gallery.Name}/Collision");
            Rect2 support = WorldRectangle(basin, shape, new Vector2(1280, 3600));
            TestAssert.Equal(gallery.Support, support, $"{gallery.Name} region checks use the actual saved support");
            Vector2[] supportedCenters =
            [
                gallery.Center,
                new(support.Position.X + PlayerRadius, support.Position.Y - PlayerHeight / 2f),
                new(support.End.X - PlayerRadius, support.Position.Y - PlayerHeight / 2f)
            ];
            foreach (Vector2 point in supportedCenters)
            {
                AssertCompleteCapsuleSupport(point, support, gallery.Name);
                TestAssert.True(CanyonWorldLayout.TryResolveRouteNode(point, out CanyonRouteNodeId region) &&
                    region == CanyonRouteNodeId.GearPistons,
                    $"{gallery.Name} full-capsule supported center {point} maps to GearPistons");
            }
        }
        Vector2[] offPlatformVoid =
        [
            new(3160, 3800), new(3400, 3720), new(3680, 3644),
            new(3160, 3520), new(3400, 3440), new(3680, 3360)
        ];
        foreach (Vector2 point in offPlatformVoid)
            TestAssert.True(!CanyonWorldLayout.TryResolveRouteNode(point, out CanyonRouteNodeId region) ||
                region != CanyonRouteNodeId.GearPistons,
                $"upper-gallery fallback cannot label off-platform void {point} as GearPistons");
    }

    private static void AssertCompleteCapsuleSupport(Vector2 center, Rect2 support, string context)
    {
        TestAssert.True(center.X - PlayerRadius >= support.Position.X - GeometryTolerance &&
            center.X + PlayerRadius <= support.End.X + GeometryTolerance,
            $"{context} supports the complete capsule width, not only its center point");
        TestAssert.Near(support.Position.Y, center.Y + PlayerHeight / 2f, GeometryTolerance,
            $"{context} complete capsule rests on the actual support top");
    }

    private static bool CapsuleInteriorOverlapsRectangle(Vector2 center, Rect2 rectangle)
    {
        // A vertical capsule is the radius-20 expansion of its 32px spine.
        // Distance between that spine and an axis-aligned rectangle is exact;
        // touching a support is legal, penetrating another solid is not.
        float spineHalfLength = PlayerHeight / 2f - PlayerRadius;
        float dx = Mathf.Max(rectangle.Position.X - center.X,
            Mathf.Max(center.X - rectangle.End.X, 0f));
        float dy = Mathf.Max(rectangle.Position.Y - (center.Y + spineHalfLength),
            Mathf.Max((center.Y - spineHalfLength) - rectangle.End.Y, 0f));
        return dx * dx + dy * dy < PlayerRadius * PlayerRadius - GeometryTolerance;
    }

    private static Rect2 WorldRectangle(PrologueSector sector, CollisionShape2D shape, Vector2 offset)
    {
        TestAssert.True(shape.Shape is RectangleShape2D,
            $"{shape.GetParent().Name} saved terrain uses the authored rectangular support contract");
        Transform2D transform = TransformInSector(sector, shape);
        TestAssert.True(transform.X.IsEqualApprox(Vector2.Right) && transform.Y.IsEqualApprox(Vector2.Down),
            $"{shape.GetParent().Name} static clearance fixture has no unaccounted rotation or scale");
        Vector2 center = transform * Vector2.Zero + offset;
        Vector2 size = ((RectangleShape2D)shape.Shape).Size;
        return new Rect2(center - size / 2f, size);
    }

    private static Vector2 PositionInSector(PrologueSector sector, Node2D node) =>
        TransformInSector(sector, node) * Vector2.Zero;

    private static Transform2D TransformInSector(PrologueSector sector, Node node)
    {
        Transform2D transform = Transform2D.Identity;
        for (Node current = node; current != null && current != sector; current = current.GetParent())
            if (current is Node2D canvas) transform = canvas.Transform * transform;
        return transform;
    }

    private static void AssertIds(IEnumerable<string> expected, IEnumerable<string> actual, string message) =>
        TestAssert.Equal(string.Join("|", expected.OrderBy(id => id, StringComparer.Ordinal)),
            string.Join("|", actual.OrderBy(id => id, StringComparer.Ordinal)), message);

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed record SectorCase(PrologueSectorId Id, string Path, Vector2 Offset);
    private sealed record FragmentCase(string StableId, PrologueSectorId SectorId,
        CanyonRouteNodeId Region, bool Optional, string SupportName, Rect2 Support, Vector2 Center);
}
