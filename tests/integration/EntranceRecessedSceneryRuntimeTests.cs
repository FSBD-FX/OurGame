using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Presentation;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class EntranceRecessedSceneryRuntimeTests
{
    private sealed record Paint(string Role, IReadOnlyList<Vector2> Points, IReadOnlyList<Color> Colors);
    private static readonly Vector2[] Cavity = [new(48,80),new(600,80),new(600,400),new(1280,400),new(1280,636),new(48,636)];
    private static readonly string[] Roles = ["recess-base","ambient-wash","rear-rock","far-bank-recess",
        "shale-recession","fault-shadow","buried-rib","buried-rib-bevel","broken-conduit",
        "broken-conduit-bevel","earth-mass","light-wash"];

    public static async Task Run(SceneTree tree)
    {
        foreach (bool built in new[] { true, false })
        {
            PrologueSector sector = built ? PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonEntrance) :
                GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonEntrance)).Instantiate<PrologueSector>();
            try
            {
                var scenery = sector.GetNode<CanyonBackdropVisual>("VisualRoot/Midground/CanyonLandmark");
                Paint[] plan = ReadPlan(scenery);
                AssertPlan(plan);
                TestAssert.Equal(-20, ((Node2D)scenery.GetParent()).ZIndex, "recess is behind gameplay, not a new foreground mask");
                TestAssert.Equal(0, scenery.ZIndex, "recess does not override its authored depth");
                TestAssert.True(scenery.ZAsRelative && scenery.Scale == Vector2.One && scenery.Rotation == 0f && scenery.Position == Vector2.Zero,
                    "actual builder/packed consumer keeps its original local transform");
                AssertVisualOnly(scenery);
            }
            finally { sector.Free(); }
        }
        RunGeometryControls();
        RunIsolationAndScaling();
        await RunMountedLeaf(tree);
        GD.Print("ENTRANCE_RECESSED_DEPTH_OK consumers=2 polygons=12 geometry_controls=13 containment=literal_L protection_y=608 draw_request_only=true pixels=false human=false");
    }

    private static Paint[] ReadPlan(CanyonBackdropVisual scenery)
    {
        MethodInfo method = scenery.GetType().GetMethod("BuildEntranceDepthPaintPlan", Type.EmptyTypes);
        TestAssert.True(method != null, "ENTRANCE_RECESSED_DEPTH actual saved/builder landmark needs recessed rock and buried mechanical paint");
        TestAssert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>),
            "replaceable scenery exposes a read-only authoring plan");
        object result = method.Invoke(scenery, null);
        object[] commands = ((IEnumerable)result).Cast<object>().ToArray();
        RefuseOverwrite(result);
        return commands.Select(command =>
        {
            Type type = command.GetType();
            var points = (IReadOnlyList<Vector2>)type.GetProperty("Points").GetValue(command);
            var colors = (IReadOnlyList<Color>)type.GetProperty("Colors").GetValue(command);
            RefuseOverwrite(points); RefuseOverwrite(colors);
            return new Paint((string)type.GetProperty("Role").GetValue(command), points, colors);
        }).ToArray();
    }

    private static void RefuseOverwrite(object value)
    {
        if (value is not IList list || list.Count == 0) return;
        bool refused = false;
        try { list[0] = list[0]; } catch (NotSupportedException) { refused = true; }
        TestAssert.True(refused, "plan/points/colors refuse caller mutation");
    }

    private static void AssertPlan(Paint[] plan)
    {
        TestAssert.Equal(12, plan.Length, "sparse depth uses twelve large polygons, not particle/detail spam");
        TestAssert.True(plan.Select(p => p.Role).SequenceEqual(Roles), "rock/recess/metal/light layering follows the authored depth order");
        Vector2[][] originals = [Cavity, [new(85,100),new(540,80),new(470,375),new(342,560),new(98,603)],
            [new(110,332),new(226,260),new(356,286),new(444,377),new(417,530),new(115,601)],
            [new(616,456),new(792,418),new(970,452),new(1190,431),new(1280,470),new(1280,636),new(600,636)]];
        for (int i = 0; i < 4; i++) TestAssert.True(plan[i].Points.SequenceEqual(originals[i]), "original four background contours remain intact");
        Color[] baseColors = [new("5a6253",.55f),new("596052",.46f),new("334447",.5f),new("172c34",.65f),new("132831",.8f),new("283b3b",.5f)];
        TestAssert.True(plan[0].Colors.SequenceEqual(baseColors), "existing staged base vertex values are preserved");
        foreach ((Paint paint, int index) in plan.Select((paint, index) => (paint, index)))
        {
            TestAssert.True(ValidPolygon(paint.Points.ToArray()), "real depth polygon must be finite, simple, triangulatable and nondegenerate: " + paint.Role);
            TestAssert.True(InsideCavity(paint.Points.ToArray()), "entire polygon, not only vertices, stays in literal L cavity: " + paint.Role);
            TestAssert.True(paint.Points.Count <= 12 && paint.Colors.Count == paint.Points.Count, "bounded per-vertex paint data");
            foreach (Color color in paint.Colors)
                TestAssert.True(float.IsFinite(color.R) && float.IsFinite(color.G) && float.IsFinite(color.B) && float.IsFinite(color.A) &&
                    color.R >= 0 && color.G >= 0 && color.B >= 0 && color.A >= 0 && color.A <= 1,
                    "finite nonnegative ordinary (not HDR/additive) colors");
            if (index < 4) continue;
            TestAssert.True(paint.Points.All(p => p.Y < 608f), "new scenic marks cannot enter true support-edge protection band");
            TestAssert.True(paint.Colors.All(c => c.A <= .42f && c.R <= .56f && c.G <= .56f && c.B <= .56f),
                "added scenery remains low-alpha dark earth/metal, not a glowing target");
        }
    }

    private static void RunIsolationAndScaling()
    {
        var visual = new CanyonBackdropVisual { SectorId = PrologueSectorId.CanyonEntrance, UseCanyonDepthStaging = true };
        try
        {
            Paint[] baseline = ReadPlan(visual);
            foreach (Vector2 size in new[] { new Vector2(640,360), new Vector2(2560,1440), new Vector2(640,900) })
            {
                visual.SectorBounds = new Rect2(new Vector2(123,-77), size);
                Paint[] scaled = ReadPlan(visual);
                TestAssert.Equal(12, scaled.Length, "valid resized layer retains sparse composition");
                for (int i = 0; i < baseline.Length; i++)
                    for (int j = 0; j < baseline[i].Points.Count; j++)
                        TestAssert.True(scaled[i].Points[j].DistanceTo(baseline[i].Points[j] * size / new Vector2(1280,720)) < .001f,
                            "bounds position is not baked twice; size scales actual local paint");
            }
            void Empty(string reason) => TestAssert.Equal(0, ReadPlan(visual).Length, reason);
            visual.SectorBounds = new Rect2(0,0,1280,720);
            foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>().Where(id => id != PrologueSectorId.CanyonEntrance))
            { visual.SectorId = sector; Empty("new scenery does not leak into another sector"); }
            visual.SectorId = PrologueSectorId.CanyonEntrance;
            visual.UseCanyonDepthStaging = false; Empty("old unstaged sample retains old draw path"); visual.UseCanyonDepthStaging = true;
            visual.DistantLayer = true; Empty("continuous distant field remains unchanged"); visual.DistantLayer = false;
            visual.CoverageLayer = true; Empty("coverage skirt remains unchanged"); visual.CoverageLayer = false;
            foreach (string role in new[] { "challenge", "coverage", "unknown" }) { visual.VisualRole = role; Empty("composition roles remain isolated"); }
            visual.VisualRole = "sector";
            foreach (Vector2 size in new[] { Vector2.Zero, new Vector2(-1,720), new Vector2(1280,float.NaN), new Vector2(float.PositiveInfinity,720) })
            { visual.SectorBounds = new Rect2(Vector2.Zero,size); Empty("invalid dimensions cannot submit new depth geometry"); }
        }
        finally { visual.Free(); }
    }

    private static async Task RunMountedLeaf(SceneTree tree)
    {
        var sector = GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonEntrance)).Instantiate<PrologueSector>();
        var visual = sector.GetNode<CanyonBackdropVisual>("VisualRoot/Midground/CanyonLandmark");
        string before = PhysicsAndEdgeFingerprint(sector);
        int requests = 0;
        Action onDraw = () => requests++;
        visual.Draw += onDraw;
        try
        {
            tree.Root.AddChild(sector);
            for (int i = 0; i < 3; i++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            int prior = requests;
            visual.QueueRedraw();
            for (int i = 0; i < 6; i++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            TestAssert.True(requests > prior, "real mounted saved landmark receives a native redraw request without manually calling Draw/_Draw");
            TestAssert.Equal(before, PhysicsAndEdgeFingerprint(sector), "scenery mount/redraw must preserve all static collision, grab profiles and readable-edge data");
            GD.Print($"ENTRANCE_DEPTH_DRAW_REQUEST_OK native_requests={requests} fake_draw_calls=0 command_submission_observed=false pixels=false");
        }
        finally
        {
            visual.Draw -= onDraw;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,sector);
        }
    }

    private static string PhysicsAndEdgeFingerprint(Node root)
    {
        var rows = new List<string>();
        void Walk(Node node)
        {
            string path = root.GetPathTo(node).ToString();
            if (node is StaticBody2D body) rows.Add($"body|{path}|{body.Transform}|{body.CollisionLayer}|{body.CollisionMask}");
            if (node is CollisionShape2D shape && shape.GetParent() is StaticBody2D)
                rows.Add($"shape|{path}|{shape.Transform}|{shape.Disabled}|{shape.OneWayCollision}|{shape.OneWayCollisionMargin}|{shape.Shape?.GetClass()}|{(shape.Shape is RectangleShape2D box ? box.Size : Vector2.Zero)}");
            if (node is GrappleSurface2D surface) rows.Add($"grab|{path}|{surface.Transform}|{surface.Material}|{surface.UseCollisionHitPoint}");
            if (node is Polygon2D edge && node.Name == "ReadableEdge") rows.Add($"edge|{path}|{edge.Transform}|{edge.Color}|{string.Join(";",edge.Polygon)}");
            foreach (Node child in node.GetChildren()) Walk(child);
        }
        Walk(root);
        return string.Join("\n",rows.OrderBy(row => row,StringComparer.Ordinal));
    }

    private static void AssertVisualOnly(Node node)
    {
        TestAssert.True(node is not CollisionObject2D && node is not CollisionShape2D && node is not GrappleSurface2D,
            "scenery does not gain gameplay collision/grapple authority");
        foreach (Node child in node.GetChildren()) AssertVisualOnly(child);
    }

    private static bool ValidPolygon(Vector2[] p)
    {
        if (p.Length < 3 || p.Any(v => !float.IsFinite(v.X) || !float.IsFinite(v.Y)) || p.Distinct().Count() != p.Length || Math.Abs(Area(p)) < .01d) return false;
        for (int i = 0; i < p.Length; i++) for (int j = i+1; j < p.Length; j++)
            if (j != i+1 && !(i == 0 && j == p.Length-1) && Touch(p[i],p[(i+1)%p.Length],p[j],p[(j+1)%p.Length])) return false;
        int[] triangles = Geometry2D.TriangulatePolygon(p);
        if (triangles.Length != (p.Length-2)*3 || triangles.Any(i => i < 0 || i >= p.Length)) return false;
        double sum = 0;
        for (int i = 0; i < triangles.Length; i+=3) sum += Math.Abs(Area([p[triangles[i]],p[triangles[i+1]],p[triangles[i+2]]]));
        return Math.Abs(sum - Math.Abs(Area(p))) < .5d;
    }
    private static bool Touch(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float abC=(b-a).Cross(c-a), abD=(b-a).Cross(d-a), cdA=(d-c).Cross(a-c), cdB=(d-c).Cross(b-c);
        bool On(Vector2 p,Vector2 x,Vector2 y) => p.X >= Math.Min(x.X,y.X)-.001f && p.X <= Math.Max(x.X,y.X)+.001f && p.Y >= Math.Min(x.Y,y.Y)-.001f && p.Y <= Math.Max(x.Y,y.Y)+.001f;
        return abC*abD < 0 && cdA*cdB < 0 || Math.Abs(abC)<.001f && On(c,a,b) || Math.Abs(abD)<.001f && On(d,a,b) || Math.Abs(cdA)<.001f && On(a,c,d) || Math.Abs(cdB)<.001f && On(b,c,d);
    }
    private static double Area(Vector2[] p)
    {
        double sum=0;
        for(int i=0;i<p.Length;i++) sum+=(double)p[i].X*p[(i+1)%p.Length].Y-(double)p[i].Y*p[(i+1)%p.Length].X;
        return sum*.5d;
    }
    private static bool InsideCavity(Vector2[] p) => Geometry2D.ClipPolygons(p,Cavity).All(contour => Math.Abs(Area(contour)) < .01d);

    private static void RunGeometryControls()
    {
        Vector2[][] invalid = [
            [new(100,100),new(200,200),new(100,200),new(200,100)],
            [new(100,100),new(200,100),new(200,100),new(100,200)],
            [new(100,100),new(200,100),new(300,100)],
            [new(float.NaN,100),new(200,100),new(100,200)],
            [new(150,100),new(180,200),new(100,140),new(200,140),new(120,200)],
            [new(100,100),new(200,100)],
            [new(float.PositiveInfinity,100),new(200,100),new(100,200)],
            [new(100,100),new(200,100),new(150,100),new(150,200)]
        ];
        foreach(Vector2[] sample in invalid) TestAssert.True(!ValidPolygon(sample),"geometry verifier rejects malformed/degenerate/self-crossing literal");
        TestAssert.True(ValidPolygon(Cavity) && ValidPolygon(Cavity.Reverse().ToArray()),"legal concave L works in both windings");
        Vector2[] acrossNotch=[new(500,200),new(1100,450),new(500,500)];
        TestAssert.True(acrossNotch.All(p => (p.X<=600 && p.Y>=80 && p.Y<=636) || (p.X<=1280 && p.Y>=400 && p.Y<=636)),"negative control really has all vertices inside L");
        TestAssert.True(!InsideCavity(acrossNotch),"full-polygon containment rejects an edge crossing the L notch despite valid vertices");
        TestAssert.True(InsideCavity([new(100,100),new(500,100),new(500,500),new(100,500)]),"entire left recess is valid");
        TestAssert.True(!InsideCavity([new(700,100),new(800,100),new(800,200),new(700,200)]),"solid upper-right is not scenery cavity");
        GD.Print("ENTRANCE_DEPTH_GEOMETRY_CONTROLS_OK literals=13 concave_notch=true self_crossing=true expected_from_production=false");
    }
}
