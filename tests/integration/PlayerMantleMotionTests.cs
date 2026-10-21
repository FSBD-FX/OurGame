using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Presentation;

namespace Knight.Tests.Integration;

/// <summary>Mounted formal leaf only: no level, gameplay controller, input or save fixture.</summary>
public static class PlayerMantleMotionTests
{
    public static async Task Run(SceneTree tree)
    {
        await WholeNodeSecondaryMustNotSlideOrRotateMantleShoulder(tree);
        await ActualDrawConsumerKeepsBoundedClothAndPoseOwnership(tree);
    }

    // Break caught: forwarding the delayed hem channel as a whole mantle node transform
    // moves the upper cloth band away from its authored shoulder attachment.
    private static async Task WholeNodeSecondaryMustNotSlideOrRotateMantleShoulder(SceneTree tree)
    {
        var fixture = new Node2D { Name = "MantleShoulderAttachmentFixture" };
        try
        {
            PackedScene scene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
            PlayerFormalVisual control = scene.Instantiate<PlayerFormalVisual>();
            PlayerFormalVisual driven = scene.Instantiate<PlayerFormalVisual>();
            control.Name = "IdentitySecondary";
            driven.Name = "DelayedHemSecondary";
            fixture.AddChild(control);
            fixture.AddChild(driven);
            tree.Root.AddChild(fixture);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            var basis = new PlayerPresentationFrame(
                new EntityPresentationFrame(EntityVisualState.Move, 1, true),
                new PlayerLocomotionFrame(PlayerLocomotionState.Run, .25f),
                new PlayerActionFrame(PlayerActionState.GroundAttack,
                    PlayerActionPhase.Contact, .55f, false),
                SecondaryMotionFrame.Identity);
            // Both real rigs receive the same history. Settle entry blending before comparison;
            // no clock advance or different action/locomotion/facing can explain the difference.
            for (int index = 0; index < 16; index++)
            {
                control.ApplyPlayerFrame(basis);
                driven.ApplyPlayerFrame(basis);
            }
            AssertTransformNear(control.GetNode<Node2D>("Mantle").GlobalTransform,
                driven.GetNode<Node2D>("Mantle").GlobalTransform, "identical settled mantle baseline");

            var secondary = new SecondaryMotionFrame(
                Pose(-6f, 2f, .20f),
                Pose(-2f, 4f, -.09f),
                Pose(1f, -1f, .17f),
                Pose(-3f, 2f, -.13f));
            control.ApplyPlayerFrame(basis);
            driven.ApplyPlayerFrame(basis with { Secondary = secondary });

            AssertChannel(control, driven, "Lantern", new Vector2(-2f, 4f), -.09f);
            AssertChannel(control, driven, "ShoulderCore", new Vector2(1f, -1f), .17f);
            AssertChannel(control, driven, "Tether", new Vector2(-3f, 2f), -.13f);
            foreach (string name in new[] { "Body", "NearArm", "FarArm", "NearLeg", "FarLeg", "Weapon" })
                AssertTransformNear(control.GetNode<Node2D>(name).Transform,
                    driven.GetNode<Node2D>(name).Transform, $"secondary must not steal {name} pose ownership");
            GD.Print("MANTLE_SHOULDER_CONTROL_CHANNELS_OK");

            Node2D controlMantle = control.GetNode<Node2D>("Mantle");
            Node2D drivenMantle = driven.GetNode<Node2D>("Mantle");
            // Literal authored upper-band samples include both top silhouette points and trim ends.
            foreach (Vector2 shoulderPoint in new[]
                     {
                         new Vector2(-17f, -9f), new Vector2(13f, -8f),
                         new Vector2(-12f, -3f), new Vector2(11f, -3f)
                     })
                AssertVectorNear(controlMantle.ToGlobal(shoulderPoint),
                    drivenMantle.ToGlobal(shoulderPoint),
                    $"delayed hem secondary must not slide/rotate pinned upper cloth point {shoulderPoint}");
            AssertTransformNear(controlMantle.GlobalTransform, drivenMantle.GlobalTransform,
                "delayed hem secondary must leave the authored shoulder attachment transform unchanged");
        }
        finally
        {
            // Immediate test-owned cleanup also runs on the intended behavioral RED.
            if (GodotObject.IsInstanceValid(fixture))
            {
                fixture.GetParent()?.RemoveChild(fixture);
                fixture.Free();
            }
        }
    }

    // Breaks caught: disconnected/unmapped material planes, rigid or sign-cancelled motion,
    // stale/accumulating shape state, a second mirror, and secondary stealing action attachment.
    private static async Task ActualDrawConsumerKeepsBoundedClothAndPoseOwnership(SceneTree tree)
    {
        var fixture = new Node2D { Name = "MantleActualDrawConsumerFixture" };
        try
        {
            PackedScene scene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
            PlayerFormalVisual control = scene.Instantiate<PlayerFormalVisual>();
            PlayerFormalVisual driven = scene.Instantiate<PlayerFormalVisual>();
            fixture.AddChild(control);
            fixture.AddChild(driven);
            tree.Root.AddChild(fixture);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            PlayerFormalLayer cloth = driven.GetNode<PlayerFormalLayer>("Mantle");

            ExactRestAndBoundedSharedMapping(cloth);
            RenderedUpperBandRemainsPinnedAcrossActualPolygonEdges(cloth);
            LiveConsumerReplacesRatherThanAccumulates(control, driven, cloth);
            MirroredFramesUseOnlyFormalRootMirror(control, driven);
            ExistingSpringLagAndSettleReachActualDrawConsumer(driven, cloth);
            ActionReleaseDeathAndRecoveryKeepAuthoredAttachment(control, driven, cloth);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(fixture))
            {
                fixture.GetParent()?.RemoveChild(fixture);
                fixture.Free();
            }
        }
    }

    private static readonly Vector2[] PaintSamples =
    [
        // Full silhouette, then every material-plane point, then both collar trim endpoints.
        new(-17f, -9f), new(13f, -8f), new(20f, 11f), new(13f, 29f),
        new(4f, 24f), new(-4f, 31f), new(-11f, 24f), new(-20f, 28f), new(-17f, 7f),
        new(-14f, -6f), new(-7f, -6f), new(-4f, 22f), new(-10f, 20f),
        new(-15f, 23f), new(-13f, 7f), new(-6f, -6f), new(5f, -5f),
        new(8f, 20f), new(4f, 20f), new(-2f, 26f), new(-4f, 18f),
        new(6f, -5f), new(11f, -5f), new(16f, 10f), new(12f, 24f), new(8f, 21f),
        new(-12f, -3f), new(11f, -3f)
    ];

    private static void ExactRestAndBoundedSharedMapping(PlayerFormalLayer cloth)
    {
        cloth.SetMantleSecondaryMotion(PoseTransform2D.Identity);
        AssertExactRest(cloth, "identity preserves every authored paint sample exactly");
        foreach (PoseTransform2D extreme in new[]
                 {
                     Pose(float.MaxValue, float.MaxValue, float.MaxValue),
                     Pose(-float.MaxValue, -float.MaxValue, -float.MaxValue),
                     Pose(float.MaxValue, -float.MaxValue, -float.MaxValue),
                     Pose(float.NaN, float.PositiveInfinity, float.NegativeInfinity)
                 })
        {
            cloth.SetMantleSecondaryMotion(extreme);
            foreach (Vector2 rest in PaintSamples)
            {
                Vector2 actual = cloth.MapMantlePoint(rest);
                TestAssert.True(float.IsFinite(actual.X) && float.IsFinite(actual.Y),
                    "extreme/invalid drive keeps actual draw sample finite");
                TestAssert.True(Mathf.Abs(actual.X - rest.X) <= 12.0001f &&
                    Mathf.Abs(actual.Y - rest.Y) <= 7.0001f,
                    "actual draw sample never exceeds 12 horizontal / 7 vertical displacement");
                if (rest.Y <= -3f)
                    TestAssert.Equal(rest, actual, "entire upper band and trim remain pinned exactly");
            }
        }
        AssertExactRest(cloth, "fully invalid motion fails closed to exact rest");
        TestAssert.Equal(Vector2.Zero, cloth.MapMantlePoint(new Vector2(float.NaN, 31f)),
            "invalid sample coordinates fail closed without nonfinite render geometry");
        cloth.SetMantleSecondaryMotion(Pose(-6f, 2f, float.NaN));
        AssertVectorNear(new Vector2(-10f, 33f), cloth.MapMantlePoint(new Vector2(-4f, 31f)),
            "invalid angular drive does not discard finite translation components");
        cloth.SetMantleSecondaryMotion(Pose(-6f, 2f, .20f));
        AssertVectorNear(new Vector2(-6.8f, 33f), cloth.MapMantlePoint(new Vector2(-4f, 31f)),
            "hem consumes translation and angle once through the actual draw mapper");
        AssertVectorNear(new Vector2(-1.4f, 15f), cloth.MapMantlePoint(new Vector2(0f, 14f)),
            "middle has half hem response, not one rigid node transform");
        AssertVectorNear(new Vector2(0f, -3f), cloth.MapMantlePoint(new Vector2(0f, -3f)),
            "response starts continuously from the pinned band");
        Vector2 edge = cloth.MapMantlePoint(new Vector2(0f, -2.999f));
        TestAssert.True(edge.DistanceTo(new Vector2(0f, -2.999f)) < .0001f,
            "mapping has no jump immediately below the pinned band");
    }

    // Break caught: mapping only authored vertices lets a straight DrawPolygon edge with
    // a moving lower endpoint drift inside the pinned band despite MapMantlePoint pinning it.
    private static void RenderedUpperBandRemainsPinnedAcrossActualPolygonEdges(PlayerFormalLayer cloth)
    {
        MethodInfo mapper = typeof(PlayerFormalLayer).GetMethod("MapMantlePoints",
            BindingFlags.Instance | BindingFlags.NonPublic);
        TestAssert.True(mapper != null, "actual Draw-used polygon mapper is available");
        // These are the four exact DrawMantle inputs, sliced from the unchanged authored
        // samples above. Expected left/right edge intersections are hand-derived literals,
        // not the point mapper's answer for hypothetical extra vertices.
        var polygons = new (string Name, Vector2[] RestPoints, Vector2[] CrossSections)[]
        {
            ("contour", PaintSamples[..9],
                [new(-17f, 14.1052632f), new(-17f, 14.4736842f), new(-17f, 14.8421053f)]),
            ("fold", PaintSamples[9..15],
                [new(-13.9230769f, -6.8928571f), new(-13.8461538f, -6.7857143f),
                    new(-13.7692308f, -6.6785714f)]),
            ("light", PaintSamples[15..21],
                [new(-5.9166667f, 5f), new(-5.8333333f, 5.12f), new(-5.75f, 5.24f)]),
            ("shade", PaintSamples[21..26],
                [new(6f, 11f), new(6.0769231f, 11.3333333f), new(6.1538462f, 11.6666667f)])
        };
        var drives = new (string Name, PoseTransform2D Motion)[]
        {
            ("identity", PoseTransform2D.Identity),
            ("trailing", Pose(-6f, 2f, -.20f)),
            ("reverse", Pose(6f, -2f, .20f)),
            ("positive extreme", Pose(float.MaxValue, float.MaxValue, float.MaxValue)),
            ("negative extreme", Pose(-float.MaxValue, -float.MaxValue, -float.MaxValue)),
            ("mixed extreme", Pose(float.MaxValue, -float.MaxValue, -float.MaxValue)),
            ("invalid", Pose(float.NaN, float.PositiveInfinity, float.NegativeInfinity))
        };
        foreach (var drive in drives)
        {
            cloth.SetMantleSecondaryMotion(drive.Motion);
            foreach (var polygon in polygons)
            {
                // Reflection reaches the real private array consumer used by DrawMantle;
                // no test-only production API or duplicate deformation implementation.
                Vector2[] mapped = (Vector2[])mapper.Invoke(cloth,
                    [(Vector2[])polygon.RestPoints.Clone()]);
                if (polygon.Name == "contour")
                {
                    MethodInfo outlineMapper = typeof(PlayerFormalLayer).GetMethod("MapMantleEdges",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    TestAssert.True(outlineMapper != null, "actual open outline shares the edge splitter");
                    Vector2[] outline = (Vector2[])outlineMapper.Invoke(cloth,
                        [(Vector2[])polygon.RestPoints.Clone(), false]);
                    TestAssert.Equal(mapped.Length - 1, outline.Length,
                        "open outline excludes the fill-only closing-edge intersection");
                    AssertVectorNear(cloth.MapMantlePoint(polygon.RestPoints[0]), outline[0],
                        "outline starts at its unchanged authored first endpoint");
                    AssertVectorNear(cloth.MapMantlePoint(polygon.RestPoints[^1]), outline[^1],
                        "outline ends at its authored last endpoint, no new implicit half closing stroke");
                }
                foreach (Vector2 point in mapped)
                    TestAssert.True(float.IsFinite(point.X) && float.IsFinite(point.Y),
                        $"rendered upper band {polygon.Name}/{drive.Name} geometry is finite");
                for (int sample = 0; sample < polygon.CrossSections.Length; sample++)
                {
                    float y = -5f + sample;
                    List<float> actual = ClosedPolygonCrossSection(mapped, y);
                    string label = $"rendered upper band {polygon.Name}/{drive.Name} at y={y}";
                    TestAssert.Equal(2, actual.Count, $"{label} has two distinct boundary intersections");
                    TestAssert.Near(polygon.CrossSections[sample].X, actual[0], .0001f,
                        $"{label} left edge stays at rest");
                    TestAssert.Near(polygon.CrossSections[sample].Y, actual[1], .0001f,
                        $"{label} right edge stays at rest");
                }
                if (polygon.Name == "contour" && drive.Name == "trailing")
                    TestAssert.True(System.Array.Exists(mapped,
                            point => point.DistanceTo(new Vector2(-13.2f, 33f)) < .0001f),
                        "actual polygon consumer still deforms the hem while its rendered upper band is pinned");
            }
        }
        cloth.SetMantleSecondaryMotion(PoseTransform2D.Identity);
        GD.Print("MANTLE_RENDERED_UPPER_BAND_OK");
    }

    // DrawPolygon closes the last-to-first edge. Include it, horizontal edges and exact
    // boundary vertices; deduplicate a shared vertex so the assertion measures boundaries.
    private static List<float> ClosedPolygonCrossSection(Vector2[] points, float y)
    {
        var intersections = new List<float>();
        for (int index = 0; index < points.Length; index++)
        {
            Vector2 start = points[index];
            Vector2 end = points[(index + 1) % points.Length];
            if (start.Y == end.Y)
            {
                if (start.Y == y)
                {
                    AddDistinct(start.X);
                    AddDistinct(end.X);
                }
                continue;
            }
            if (y < Mathf.Min(start.Y, end.Y) || y > Mathf.Max(start.Y, end.Y)) continue;
            AddDistinct(start.X + (end.X - start.X) * ((y - start.Y) / (end.Y - start.Y)));
        }
        intersections.Sort();
        return intersections;

        void AddDistinct(float x)
        {
            foreach (float existing in intersections)
                if (Mathf.Abs(existing - x) < .0001f) return;
            intersections.Add(x);
        }
    }

    private static void LiveConsumerReplacesRatherThanAccumulates(PlayerFormalVisual control,
        PlayerFormalVisual driven, PlayerFormalLayer cloth)
    {
        PlayerPresentationFrame basis = MotionFrame(PlayerLocomotionState.Run, PlayerActionFrame.None);
        PoseTransform2D drive = Pose(-6f, 2f, -.20f);
        for (int index = 0; index < 24; index++)
        {
            control.ApplyPlayerFrame(basis);
            driven.ApplyPlayerFrame(basis with { Secondary = MantleOnly(drive) });
            AssertVectorNear(new Vector2(-13.2f, 33f), cloth.MapMantlePoint(new Vector2(-4f, 31f)),
                "repeated typed frames replace the draw drive without mutating rest points");
        }
        AssertTransformNear(control.GetNode<Node2D>("Mantle").Transform, cloth.Transform,
            "render attachment excludes whole-node secondary double drive");
        AssertVectorNear(new Vector2(-6f, 2f),
            driven.CurrentPose.Mantle.Position - control.CurrentPose.Mantle.Position,
            "logical CurrentPose mantle still contains the compatibility aggregate");
        driven.ApplyPlayerFrame(basis);
        AssertExactRest(cloth, "typed identity clears prior geometry immediately");
    }

    private static void MirroredFramesUseOnlyFormalRootMirror(PlayerFormalVisual right,
        PlayerFormalVisual left)
    {
        var secondary = MantleOnly(Pose(-6f, 2f, -.20f));
        PlayerPresentationFrame basis = MotionFrame(PlayerLocomotionState.Idle, PlayerActionFrame.None);
        for (int index = 0; index < 24; index++)
        {
            right.ApplyPlayerFrame(basis with { Secondary = secondary });
            left.ApplyPlayerFrame(basis with
            {
                Base = new EntityPresentationFrame(EntityVisualState.Idle, -1, true),
                Secondary = secondary
            });
        }
        PlayerFormalLayer rightCloth = right.GetNode<PlayerFormalLayer>("Mantle");
        PlayerFormalLayer leftCloth = left.GetNode<PlayerFormalLayer>("Mantle");
        TestAssert.Equal(Vector2.One, right.Scale, "right Formal root does not mirror");
        TestAssert.Equal(new Vector2(-1f, 1f), left.Scale, "left Formal root owns the sole X mirror");
        AssertTransformNear(rightCloth.Transform, leftCloth.Transform,
            "mantle attachment has no local second mirror");
        foreach (Vector2 rest in PaintSamples)
        {
            Vector2 local = rightCloth.MapMantlePoint(rest);
            TestAssert.Equal(local, leftCloth.MapMantlePoint(rest),
                "equivalent facing-local drive keeps identical cloth geometry");
            Vector2 rightWorld = rightCloth.ToGlobal(local);
            AssertVectorNear(new Vector2(-rightWorld.X, rightWorld.Y),
                leftCloth.ToGlobal(leftCloth.MapMantlePoint(rest)),
                "actual world cloth points mirror exactly once");
        }
    }

    private static void ExistingSpringLagAndSettleReachActualDrawConsumer(PlayerFormalVisual formal,
        PlayerFormalLayer cloth)
    {
        var spring = new SecondaryMotionModel();
        var forcing = new SecondaryMotionInput(new Vector2(240f, 0f), new Vector2(9000f, 0f),
            0f, 0f, 0f, PlayerActionPhase.None, 0f);
        PlayerPresentationFrame basis = MotionFrame(PlayerLocomotionState.Run, PlayerActionFrame.None);
        SecondaryMotionFrame first = spring.Step(forcing, 1f / 60f);
        formal.ApplyPlayerFrame(basis with { Secondary = first });
        Vector2 hem = new(-4f, 31f);
        float firstX = cloth.MapMantlePoint(hem).X - hem.X;
        TestAssert.True(firstX < -.001f && firstX > -12f,
            "rightward start trails left partway, not a rigid rotation/translation sign cancellation");
        formal.ApplyPlayerFrame(basis with { Secondary = spring.Step(default, 1f / 60f) });
        float releasedX = cloth.MapMantlePoint(hem).X - hem.X;
        TestAssert.True(releasedX < firstX,
            "actual drawn hem retains the existing spring inertia after drive removal");
        for (int index = 0; index < 300; index++)
            formal.ApplyPlayerFrame(basis with { Secondary = spring.Step(default, 1f / 60f) });
        TestAssert.True(cloth.MapMantlePoint(hem).DistanceTo(hem) < .001f,
            "actual hem settles through existing spring outputs without a shape reset or new spring");
        formal.ApplyPlayerFrame(basis);
        AssertExactRest(cloth, "final identity has no accumulated draw deformation");
    }

    private static void ActionReleaseDeathAndRecoveryKeepAuthoredAttachment(PlayerFormalVisual control,
        PlayerFormalVisual driven, PlayerFormalLayer cloth)
    {
        // Return both rigs to the same history/facing after the mirror and spring checks.
        PlayerPresentationFrame settled = MotionFrame(PlayerLocomotionState.Idle, PlayerActionFrame.None);
        for (int index = 0; index < 24; index++)
        {
            control.ApplyPlayerFrame(settled);
            driven.ApplyPlayerFrame(settled);
        }
        // The prior spring check advanced only driven to Run. Compare after both entry
        // blends settle, not on their intentionally different first transition frame.
        ApplyPaired(settled, false);
        foreach (PlayerPresentationFrame frame in new[]
                 {
                     MotionFrame(PlayerLocomotionState.Run, Action(PlayerActionState.Shield, PlayerActionPhase.Anticipation)),
                     MotionFrame(PlayerLocomotionState.Run, Action(PlayerActionState.Shield, PlayerActionPhase.Contact)),
                     MotionFrame(PlayerLocomotionState.Run, Action(PlayerActionState.Shield, PlayerActionPhase.Recovery)),
                     MotionFrame(PlayerLocomotionState.Run, PlayerActionFrame.None),
                     MotionFrame(PlayerLocomotionState.Rise, Action(PlayerActionState.ThreadThrow, PlayerActionPhase.Burst)),
                     MotionFrame(PlayerLocomotionState.Fall, Action(PlayerActionState.ThreadApply, PlayerActionPhase.Contact)),
                     MotionFrame(PlayerLocomotionState.Fall, PlayerActionFrame.None),
                     MotionFrame(PlayerLocomotionState.SoftLand, Action(PlayerActionState.GroundAttack, PlayerActionPhase.Anticipation)),
                     MotionFrame(PlayerLocomotionState.Run, Action(PlayerActionState.GroundAttack, PlayerActionPhase.Contact)),
                     MotionFrame(PlayerLocomotionState.Run, Action(PlayerActionState.GroundAttack, PlayerActionPhase.Recovery)),
                     MotionFrame(PlayerLocomotionState.Dead, PlayerActionFrame.None),
                     MotionFrame(PlayerLocomotionState.Recovery, PlayerActionFrame.None),
                     MotionFrame(PlayerLocomotionState.Idle, PlayerActionFrame.None)
                 })
        {
            ApplyPaired(frame, true);
            ApplyPaired(frame, false);
        }

        void ApplyPaired(PlayerPresentationFrame frame, bool hasDrive)
        {
            control.ApplyPlayerFrame(frame);
            driven.ApplyPlayerFrame(frame with
            {
                Secondary = hasDrive ? MantleOnly(Pose(-6f, 2f, -.2f)) : SecondaryMotionFrame.Identity
            });
            TestAssert.Equal(control.CurrentActionPose, driven.CurrentActionPose,
                "mantle shape never changes authored action pose or slot ownership");
            foreach (string name in new[] { "Body", "NearArm", "FarArm", "NearLeg", "FarLeg", "Weapon", "Mantle", "Lantern", "ShoulderCore", "Tether" })
                AssertTransformNear(control.GetNode<Node2D>(name).Transform,
                    driven.GetNode<Node2D>(name).Transform,
                    $"{frame.Locomotion.State}/{frame.Action.State} retains authored {name} attachment");
            if (hasDrive)
                TestAssert.True(cloth.MapMantlePoint(new Vector2(-4f, 31f)).X < -4.001f,
                    "action boundary still forwards real hem drive");
            else AssertExactRest(cloth, "action/release/death/recovery identity clears old shape");
        }
    }

    private static void AssertExactRest(PlayerFormalLayer cloth, string message)
    {
        foreach (Vector2 rest in PaintSamples)
            TestAssert.Equal(rest, cloth.MapMantlePoint(rest), message);
    }

    private static PlayerPresentationFrame MotionFrame(PlayerLocomotionState locomotion,
        PlayerActionFrame action) => new(
        new EntityPresentationFrame(locomotion == PlayerLocomotionState.Dead
            ? EntityVisualState.Dead : EntityVisualState.Idle, 1, true),
        new PlayerLocomotionFrame(locomotion, .25f), action, SecondaryMotionFrame.Identity);

    private static PlayerActionFrame Action(PlayerActionState state, PlayerActionPhase phase) =>
        new(state, phase, 1f, phase == PlayerActionPhase.Recovery, new Vector2(1f, -.3f));

    private static SecondaryMotionFrame MantleOnly(PoseTransform2D drive) =>
        SecondaryMotionFrame.Identity with { Mantle = drive };

    private static PoseTransform2D Pose(float x, float y, float angle) =>
        new(new Vector2(x, y), angle, Vector2.One, Colors.White);

    private static void AssertChannel(PlayerFormalVisual control, PlayerFormalVisual driven,
        string name, Vector2 positionDelta, float rotationDelta)
    {
        Node2D baseline = control.GetNode<Node2D>(name);
        Node2D actual = driven.GetNode<Node2D>(name);
        AssertVectorNear(baseline.Position + positionDelta, actual.Position,
            $"{name} retains its independently authored secondary translation");
        TestAssert.Near(baseline.Rotation + rotationDelta, actual.Rotation, .0001f,
            $"{name} retains its independently authored secondary rotation");
        TestAssert.Equal(baseline.Scale, actual.Scale, $"{name} secondary does not stretch the layer");
        TestAssert.Equal(baseline.Modulate, actual.Modulate, $"{name} secondary keeps existing material");
    }

    private static void AssertTransformNear(Transform2D expected, Transform2D actual, string message)
    {
        AssertVectorNear(expected.Origin, actual.Origin, $"{message} origin");
        AssertVectorNear(expected.X, actual.X, $"{message} X basis");
        AssertVectorNear(expected.Y, actual.Y, $"{message} Y basis");
    }

    private static void AssertVectorNear(Vector2 expected, Vector2 actual, string message)
    {
        TestAssert.True(float.IsFinite(actual.X) && float.IsFinite(actual.Y), $"{message} is finite");
        TestAssert.Near(expected.X, actual.X, .0001f, $"{message} X");
        TestAssert.Near(expected.Y, actual.Y, .0001f, $"{message} Y");
    }
}
