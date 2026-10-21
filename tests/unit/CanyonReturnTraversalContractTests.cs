using System;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Unit;

public static class CanyonReturnTraversalContractTests
{
    // Value3 is deliberately new: legacy saved enum values0/1/2 must not move.
    // RED first catches rejection of actual walk/drop routes as an undefined kind.
    private const TraversalSegmentKind GroundTransfer = (TraversalSegmentKind)3;

    public static void Run()
    {
        WalkDropNeedsNoUnearnedAbility();
        AbyssEgressIsNotMisrepresentedAsAJump(fromBuilder: true);
        AbyssEgressIsNotMisrepresentedAsAJump(fromBuilder: false);
        CrossSectorClawDropUsesTheOriginalFaceAndFloor();
        AuthoredAbyssReverseUsesTheCrossSectorContract(fromBuilder: true);
        AuthoredAbyssReverseUsesTheCrossSectorContract(fromBuilder: false);
    }

    private static void WalkDropNeedsNoUnearnedAbility()
    {
        var room = new Node2D();
        try
        {
            var segment = new TraversalSegment2D
            {
                Name = "walk_drop", SegmentId = "walk_drop", Kind = GroundTransfer,
                RequiredAbilities = TraversalAbility.None,
                LaunchZone = new Rect2(0, 0, 120, 72), LandingZone = new Rect2(-140, 436, 180, 72)
            };
            room.AddChild(segment);
            TestAssert.Equal(0, segment.Validate(room).Count,
                "walk/drop contract accepts no special abilities instead of demanding a fake jump");
            foreach (TraversalAbility illegal in new[] { TraversalAbility.Jump, TraversalAbility.Thread,
                         TraversalAbility.Dash, TraversalAbility.Jump | TraversalAbility.Thread })
            {
                segment.RequiredAbilities = illegal;
                TestAssert.True(segment.Validate(room).Any(error => error.Contains("GroundTransfer")),
                    $"walk/drop cannot disguise an unearned ability: {illegal}");
            }
            segment.RequiredAbilities = TraversalAbility.None;
            segment.AssistTargetPath = new NodePath("InventedAssist");
            TestAssert.True(segment.Validate(room).Any(error => error.Contains("AssistTargetPath")),
                "walk/drop cannot declare a fabricated or borrowed assist target");
            segment.AssistTargetPath = new NodePath();
            segment.LandingZone = new Rect2(-140, -80, 180, 72);
            TestAssert.True(segment.Validate(room).Any(error => error.Contains("upward")),
                "walk/drop cannot counterfeit an upward ledge transfer");
            segment.LandingZone = new Rect2(-140, 0, 180, 72);
            TestAssert.Equal(0, segment.Validate(room).Count,
                "level walking remains valid without jump or claw");
            segment.Kind = (TraversalSegmentKind)99;
            TestAssert.True(segment.Validate(room).Any(error => error.Contains("Kind")),
                "adding one kind does not make arbitrary enum values valid");
        }
        finally { room.Free(); }
    }

    private static void CrossSectorClawDropUsesTheOriginalFaceAndFloor()
    {
        // No tree, Ready, save, placement or physical-route claim. Both real
        // sectors share the same parent so an explicit relative path can resolve.
        var world = new Node2D();
        try
        {
            PrologueSector basin = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
            PrologueSector descent = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent);
            basin.Position = CanyonWorldLayout.SectorBounds[basin.SectorId].Position;
            descent.Position = CanyonWorldLayout.SectorBounds[descent.SectorId].Position;
            world.AddChild(basin); world.AddChild(descent);
            var segment = new TraversalSegment2D
            {
                Name = "claw_drop", SegmentId = "claw_drop", Kind = (TraversalSegmentKind)4,
                RequiredAbilities = TraversalAbility.Thread,
                LaunchZone = new Rect2(380, 324, 120, 72),
                LandingZone = new Rect2(880, -112, 160, 72),
                AssistTargetPath = new NodePath("../CanyonDescent/Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile"),
                AssistHitPoint = new Vector2(-18, 290.7278f), AssistHitNormal = Vector2.Left,
                LandingSupportPath = new NodePath("../CanyonDescent/Gameplay/Collision/AbyssSafeBottom")
            };
            basin.GetNode("Gameplay/TraversalContracts").AddChild(segment);
            TestAssert.Equal(0, segment.Validate(basin).Count,
                "Thread-only cross-sector claw/drop uses the real west face inside620, then the original bottom floor");
            foreach (TraversalAbility illegal in new[] { TraversalAbility.None, TraversalAbility.Jump,
                         TraversalAbility.Jump | TraversalAbility.Thread, TraversalAbility.Thread | TraversalAbility.Dash })
            {
                segment.RequiredAbilities = illegal;
                TestAssert.True(segment.Validate(basin).Any(error => error.Contains("ClawDrop")),
                    $"ClawDrop cannot invent or omit an ability: {illegal}");
            }
            segment.RequiredAbilities = TraversalAbility.Thread;
            segment.AssistHitPoint = Vector2.Zero;
            TestAssert.True(segment.Validate(basin).Any(error => error.Contains("face point")),
                "solid-body center is not a terrain collision hit point");
            segment.AssistHitPoint = new Vector2(-18, 290.7278f);
            segment.AssistHitNormal = Vector2.Right;
            TestAssert.True(segment.Validate(basin).Any(error => error.Contains("face point")),
                "the opposite normal cannot authorize a different side of the wall");
            segment.AssistHitNormal = Vector2.Left;
            GrappleSurface2D surface = descent.GetNode<GrappleSurface2D>(
                "Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile");
            surface.Material = GrappleSurfaceMaterial.Ice;
            TestAssert.True(segment.Validate(basin).Any(error => error.Contains("solid terrain")),
                "icy terrain cannot satisfy the return claw contract");
            surface.Material = GrappleSurfaceMaterial.GripStone;
            CollisionShape2D collider = surface.GetParent().GetNode<CollisionShape2D>("Collision");
            collider.Disabled = true;
            TestAssert.True(segment.Validate(basin).Any(error => error.Contains("solid terrain")),
                "disabled collision cannot satisfy the return claw contract");
            collider.Disabled = false;
            segment.LaunchZone = new Rect2(280, 424, 120, 72);
            TestAssert.True(segment.Validate(basin).Any(error => error.Contains("620")),
                "a real point beyond620 is rejected without raising the claw range");
            segment.LaunchZone = new Rect2(380, 324, 120, 72);
            segment.LandingZone = new Rect2(880, -152, 160, 72);
            TestAssert.True(segment.Validate(basin).Any(error => error.Contains("landing support")),
                "a floating landing rectangle is not the original solid floor");
            segment.LandingZone = new Rect2(880, -112, 160, 72);
            world.RemoveChild(descent);
            try
            {
                TestAssert.True(segment.Validate(basin).Any(error => error.Contains("resolve")),
                    "a missing destination sector cannot be replaced with a source-sector stand-in");
            }
            finally { world.AddChild(descent); }
            TestAssert.Equal(0, segment.Validate(basin).Count,
                "restoring the original terrain resolves the unchanged contract");
        }
        finally { world.Free(); }
    }

    private static void AuthoredAbyssReverseUsesTheCrossSectorContract(bool fromBuilder)
    {
        var world = new Node2D();
        try
        {
            PrologueSector Load(PrologueSectorId id) => fromBuilder
                ? PrologueCanyonBuildParts.Build(id)
                : GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(id)).Instantiate<PrologueSector>();
            PrologueSector basin = Load(PrologueSectorId.CanyonBasin);
            PrologueSector descent = Load(PrologueSectorId.CanyonDescent);
            basin.Position = CanyonWorldLayout.SectorBounds[basin.SectorId].Position;
            descent.Position = CanyonWorldLayout.SectorBounds[descent.SectorId].Position;
            world.AddChild(basin); world.AddChild(descent);
            TraversalSegment2D[] matches = basin.GetNode("Gameplay/TraversalContracts").GetChildren()
                .OfType<TraversalSegment2D>().Where(segment =>
                    segment.SegmentId == "canyon.v2.route.basin-camp-to-abyss").ToArray();
            string origin = fromBuilder ? "builder" : "saved";
            TestAssert.Equal(1, matches.Length, $"{origin} has one explicit reverse collection route, not an implied graph edge");
            TraversalSegment2D route = matches[0];
            TestAssert.Equal(TraversalSegmentKind.ClawDrop, route.Kind, $"{origin} records the real claw/drop transfer");
            TestAssert.Equal(TraversalAbility.Thread, route.RequiredAbilities, $"{origin} does not invent Jump or Dash");
            TestAssert.Equal(new Rect2(380, 324, 120, 72), route.LaunchZone, $"{origin} uses the actual camp launch in source-local coordinates");
            TestAssert.Equal(new Rect2(880, -112, 160, 72), route.LandingZone, $"{origin} ends on the actual adjacent-sector bottom");
            TestAssert.Equal("../CanyonDescent/Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile",
                route.AssistTargetPath.ToString(), $"{origin} names the original destination-sector west brake");
            TestAssert.Equal(new Vector2(-18, 290.7278f), route.AssistHitPoint, $"{origin} uses the measured collision-local hit point");
            TestAssert.Equal(Vector2.Left, route.AssistHitNormal, $"{origin} uses the actual west-face normal");
            TestAssert.Equal("../CanyonDescent/Gameplay/Collision/AbyssSafeBottom", route.LandingSupportPath.ToString(),
                $"{origin} names the original floor, not an added helper platform");
            TestAssert.Equal(0, route.Validate(basin).Count, $"{origin} resolves both real sector graphs: " + string.Join("; ", route.Validate(basin)));
            TestAssert.True(!route.GetNode<CanvasItem>("LaunchZoneVisual").Visible &&
                !route.GetNode<CanvasItem>("LandingZoneVisual").Visible, $"{origin} metadata is not displayed as substitute terrain");
        }
        finally { world.Free(); }
    }

    private static void AbyssEgressIsNotMisrepresentedAsAJump(bool fromBuilder)
    {
        // No tree/Ready, player placement, collection or route-completion claim.
        PrologueSector sector = fromBuilder
            ? PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent)
            : GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonDescent.tscn")
                .Instantiate<PrologueSector>();
        try
        {
            TraversalSegment2D segment = sector.GetNode("Gameplay/TraversalContracts").GetChildren()
                .OfType<TraversalSegment2D>().Single(item =>
                    item.SegmentId == "canyon.v2.route.abyss-to-basin-camp");
            string origin = fromBuilder ? "builder" : "saved";
            TestAssert.Equal(GroundTransfer, segment.Kind,
                $"{origin} abyss egress describes the actual zero-jump return, not SingleJump");
            TestAssert.Equal(TraversalAbility.None, segment.RequiredAbilities,
                $"{origin} abyss egress demands no jump, claw or dash");
            TestAssert.Equal(new Rect2(680, 3488, 120, 72), segment.LaunchZone,
                $"{origin} preserves the original source-sector launch rectangle");
            TestAssert.Equal(new Rect2(540, 3924, 180, 72), segment.LandingZone,
                $"{origin} preserves the original cross-sector camp landing rectangle");
            TestAssert.Equal(0, segment.Validate(sector).Count,
                $"{origin} walk/drop declaration validates without inventing an assist");
            TestAssert.True(!segment.GetNode<CanvasItem>("LaunchZoneVisual").Visible &&
                !segment.GetNode<CanvasItem>("LandingZoneVisual").Visible,
                $"{origin} debug bounds stay hidden, not substitute platforms");
        }
        finally { sector.Free(); }
    }
}
