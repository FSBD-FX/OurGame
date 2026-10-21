using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knight.Tests.Integration;

// Read-only presentation contracts, not pixel/readability or mapped-input proof.
public static class CanyonSteamDiverterVisualTests
{
    public static async Task RunVaporFadeContracts(SceneTree tree)
    {
        await RunStrokeBoundsContracts(tree); // Old eighteen, overlap literals and real stroke bounds first.
        GD.Print("TASK17B_VAPOR_FADE_NEIGHBORS_OK oldEighteenAndStroke=true");
        VaporFadeLiterals();
        var failures = new List<string>();
        foreach (string label in new[] { "Frozen", "Trench", "Transformed" })
            foreach (bool danger in new[] { false, true })
            {
                string name = label + (danger ? "-active" : "-warn");
                GD.Print("TASK17B_VAPOR_FADE_GROUP_BEGIN " + name);
                try { await ActualVaporFade(tree, label, danger); GD.Print("TASK17B_VAPOR_FADE_GROUP_OK " + name); }
                catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
                { failures.Add(name + ": " + failure.Message); }
            }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B consumed vapor alpha does not attenuate upward\n" + string.Join("\n", failures));
        GD.Print("TASK17B_VAPOR_FADE_CONTRACTS_OK consumedVertexColors=true warningAndActive=true upperAttenuation=true manualClock=true beautyCertified=false fullLevel=false");
    }

    // Mutation caught: replacing the consumed upper fade with constant alpha
    // recreates the observed full-height solid lobe. Inspect the exact colors
    // passed to DrawPolygon, not a parallel opacity formula or a pixel model.
    private static async Task ActualVaporFade(SceneTree tree, string label, bool danger)
    {
        string name = label + (danger ? "-active" : "-warn");
        bool allFade = true;
        foreach (double phase in new[] { 0d, .37d, 1.11d })
            await CanyonSteamDiverterPhysicalTests.WithFixture(tree, "vapor-fade-" + name, 0f, async f =>
            {
                Vector2[] literal = StateFlowLiteralFixture(f, label);
                CanyonSteamDiverterVisual visual = AddVisual(f); visual.SetProcess(false);
                visual.Position = new(17, 11); visual.Rotation = .31f; visual.Scale = new(1.25f, .8f);
                await PaintFrame(tree); visual._Process(0d); RequireBinding(visual);
                if (visual.IsProcessing() || visual.Presentation.CosmeticSeconds != 0d)
                    throw new InvalidOperationException("Vapor fade fixture requires zero initial cosmetic phase and disabled automatic processing.");
                f.Pair.Advance(10d);
                if (danger) { f.Pair.Advance(10d); f.Pair.Advance(10d); }
                await PaintFrame(tree); visual._Process(0d);
                var frame = f.Right.Frame; float elapsed = f.Right.ElapsedSeconds;
                CanyonSteamDiverterSide side = f.Pair.SelectedSide;
                visual._Process(phase); await PaintFrame(tree);
                if (visual.Presentation.Right.Danger != danger || visual.Presentation.Right.Warning == danger ||
                    f.Right.Collision.Disabled == danger || f.Right.Monitoring != danger ||
                    !f.Right.Frame.Equals(frame) || f.Right.ElapsedSeconds != elapsed || f.Pair.SelectedSide != side ||
                    visual.Presentation.CosmeticSeconds != phase)
                    throw new InvalidOperationException("Vapor fade fixture requires actual committed state and unchanged gameplay during cosmetic sampling.");
                AssertCorners(visual.Presentation.Right.WorldCorners, literal);
                var forms = visual.RightSteamPaint.Forms;
                if (forms.Length == 0) throw new InvalidOperationException("Vapor fade fixture requires actual nonempty consumed forms.");
                bool fade = true, constantAlpha = true;
                float maximumRatio = 0f;
                foreach (var form in forms)
                {
                    foreach (Vector2 point in form.ContourWorld) RequireInsideLiteral(point, literal);
                    var bands = VaporAlphaBands(form.ContourWorld, form.VertexColors, literal);
                    fade &= HasUpperAttenuation(bands.Body, bands.Upper);
                    maximumRatio = Math.Max(maximumRatio, bands.Upper / bands.Body);
                    for (int vertex = 1; vertex < form.VertexColors.Length; vertex++)
                        constantAlpha &= form.VertexColors[vertex].A == form.VertexColors[0].A;
                }
                allFade &= fade;
                GD.Print(FormattableString.Invariant($"TASK17B_VAPOR_FADE_SAMPLE case={name} phase={phase:0.00} forms={forms.Length} maximumUpperBodyRatio={maximumRatio:0.000} constantAlpha={constantAlpha} attenuated={fade} actualWarning={!danger} actualActive={danger} colliderMatches=True gameplayUnchanged=True literalBounds=True consumedByDraw=True"));
            }, automatic: false);
        TestAssert.True(allFade, "Task17B " + name + " consumed upper vapor alpha is at least 30 percent below body at all three phases");
    }

    // Test-local bands use the independently literal nozzle-to-top axis, so a
    // rotated/nonuniform footprint is not confused with world Y. Each form's
    // body is its 20..55% vertex-height band, upper is its 75..100% band.
    // Compare each form separately: narrower crowns/reduced overlap alone
    // cannot masquerade as attenuation of the actual consumed alpha field.
    private static (float Body, float Upper) VaporAlphaBands(Vector2[] contour, Color[] colors, Vector2[] literal)
    {
        if (contour.Length < 3 || colors.Length != contour.Length || literal.Length != 4)
            throw new InvalidOperationException("Vapor alpha data must contain one consumed color per polygon vertex and a literal quadrilateral.");
        Vector2 mouth = (literal[2] + literal[3]) * .5f;
        Vector2 axis = (literal[0] + literal[1]) * .5f - mouth;
        if (!float.IsFinite(axis.LengthSquared()) || axis.LengthSquared() <= 0f)
            throw new InvalidOperationException("Vapor alpha literal axis must be finite and nondegenerate.");
        float low = float.PositiveInfinity, high = float.NegativeInfinity;
        var heights = new float[contour.Length];
        for (int i = 0; i < contour.Length; i++)
        {
            Color color = colors[i];
            heights[i] = (contour[i] - mouth).Dot(axis) / axis.LengthSquared();
            if (!float.IsFinite(heights[i]) || !float.IsFinite(color.R) || !float.IsFinite(color.G) ||
                !float.IsFinite(color.B) || !float.IsFinite(color.A) || color.A < 0f || color.A >= 1f)
                throw new InvalidOperationException("Vapor alpha data must be finite translucent production paint.");
            low = Math.Min(low, heights[i]); high = Math.Max(high, heights[i]);
        }
        if (high - low <= .01f) throw new InvalidOperationException("Vapor alpha form must have a nondegenerate height.");
        double body = 0d, upper = 0d; int bodyCount = 0, upperCount = 0;
        for (int i = 0; i < heights.Length; i++)
        {
            float height = (heights[i] - low) / (high - low);
            if (height >= .20f && height <= .55f) { body += colors[i].A; bodyCount++; }
            if (height >= .75f) { upper += colors[i].A; upperCount++; }
        }
        if (bodyCount == 0 || upperCount == 0 || body <= 0d)
            throw new InvalidOperationException("Vapor alpha form requires nonempty body and upper bands with a visible body.");
        return ((float)(body / bodyCount), (float)(upper / upperCount));
    }

    private static bool HasUpperAttenuation(float body, float upper) =>
        body > .02f && upper > 0f && upper <= body * .70f;

    private static void VaporFadeLiterals()
    {
        Vector2[] boundary = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] contour = { new(2, 10), new(2, 7), new(2, 2), new(2, 0), new(8, 0), new(8, 2), new(8, 7), new(8, 10) };
        foreach (var sample in new[] {
            (Name: "constant", Alphas: new[] { .4f,.4f,.4f,.4f,.4f,.4f,.4f,.4f }, Body: .4f, Upper: .4f, Expected: false),
            (Name: "fading", Alphas: new[] { .4f,.4f,.1f,.1f,.1f,.1f,.4f,.4f }, Body: .4f, Upper: .1f, Expected: true),
            (Name: "inverted", Alphas: new[] { .1f,.1f,.4f,.4f,.4f,.4f,.1f,.1f }, Body: .1f, Upper: .4f, Expected: false) })
        {
            var bands = VaporAlphaBands(contour, Array.ConvertAll(sample.Alphas, alpha => new Color(1f, 1f, 1f, alpha)), boundary);
            bool attenuated = HasUpperAttenuation(bands.Body, bands.Upper);
            TestAssert.True(Math.Abs(bands.Body - sample.Body) < .00001f && Math.Abs(bands.Upper - sample.Upper) < .00001f && attenuated == sample.Expected,
                "vapor alpha test bands distinguish independently literal " + sample.Name);
            GD.Print(FormattableString.Invariant($"TASK17B_VAPOR_FADE_LITERAL case={sample.Name} body={bands.Body:0.000} upper={bands.Upper:0.000} attenuated={attenuated}"));
        }
        GD.Print("TASK17B_VAPOR_FADE_LITERALS_OK cases=3 independent=true");
    }

    public static async Task RunStrokeBoundsContracts(SceneTree tree)
    {
        await RunFlowContracts(tree); // All existing eighteen are prerequisites.
        GD.Print("TASK17B_STROKE_NEIGHBORS_OK existingEighteen=true");
        StrokeClearanceLiterals();
        var failures = new List<string>();
        foreach (string label in new[] { "Frozen", "Trench", "Transformed" })
        {
            GD.Print("TASK17B_STROKE_GROUP_BEGIN " + label);
            try { await ActualWarningStrokeBounds(tree, label); GD.Print("TASK17B_STROKE_GROUP_OK " + label); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(label + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B actual warning stroke escapes borrowed hazard footprint\n" + string.Join("\n", failures));
        GD.Print("TASK17B_STROKE_BOUNDS_CONTRACTS_OK realWarning=true localStrokeExtent=true transformed=true fullLevel=false beautyCertified=false");
    }

    // A center-only bounds check misses the actual four-local-unit dark
    // DrawLine stroke. A half-local-unit antialias margin is also reserved.
    // Break caught: warning line paint bleeding outside any real borrowed edge.
    private static Task ActualWarningStrokeBounds(SceneTree tree, string label) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "stroke-" + label, 0f, async f =>
        {
            Vector2[] literal = StateFlowLiteralFixture(f, label);
            CanyonSteamDiverterVisual visual = AddVisual(f);
            visual.SetProcess(false);
            visual.Position = new(17, 11); visual.Rotation = .31f; visual.Scale = new(1.25f, .8f);
            await PaintFrame(tree); visual._Process(0d); RequireBinding(visual);
            f.Pair.Advance(10d); await PaintFrame(tree); visual._Process(0d);
            CanyonSteamDiverterWarningPaint paint = visual.RightWarningPaint;
            if (!visual.Presentation.Right.Warning || visual.Presentation.Right.Danger ||
                !f.Right.Collision.Disabled || f.Right.Monitoring || paint.MarksWorld.Length == 0)
                throw new InvalidOperationException("Stroke fixture requires actual committed harmless warning and nonempty consumed marks.");
            AssertCorners(visual.Presentation.Right.WorldCorners, literal);
            Vector2[] localBoundary = Array.ConvertAll(literal, visual.ToLocal);
            float minimum = float.PositiveInfinity;
            foreach (Vector2[] mark in paint.MarksWorld)
            {
                if (mark.Length != 2) throw new InvalidOperationException("Stroke fixture requires each consumed warning mark to be one real DrawLine segment.");
                foreach (Vector2 point in mark) RequireInsideLiteral(point, literal);
                minimum = Math.Min(minimum, MinimumLocalStrokeClearance(localBoundary, Array.ConvertAll(mark, visual.ToLocal)));
            }
            const float darkWidth = 4f, antialiasSafety = .5f;
            bool otherStrokeContained = true;
            // DrawFootprint also strokes an optional outline (width2) and
            // warning vapor filaments (dark underlay width3); neither may
            // introduce a new escaped line while the mark inset is repaired.
            Vector2[][] otherPaths = new Vector2[paint.OutlineWorld.Length >= 2 ? 1 + visual.RightSteamPaint.FilamentsWorld.Length : visual.RightSteamPaint.FilamentsWorld.Length][];
            int pathIndex = 0;
            if (paint.OutlineWorld.Length >= 2) otherPaths[pathIndex++] = paint.OutlineWorld;
            foreach (Vector2[] filament in visual.RightSteamPaint.FilamentsWorld) otherPaths[pathIndex++] = filament;
            for (int path = 0; path < otherPaths.Length; path++)
            {
                Vector2[] local = Array.ConvertAll(otherPaths[path], visual.ToLocal);
                float radiusWithSafety = (paint.OutlineWorld.Length >= 2 && path == 0 ? 1f : 1.5f) + antialiasSafety;
                for (int point = 1; point < local.Length; point++)
                    otherStrokeContained &= MinimumLocalStrokeClearance(localBoundary, new[] { local[point - 1], local[point] }) >= radiusWithSafety;
            }
            bool strokeContained = float.IsFinite(minimum) && minimum >= darkWidth * .5f + antialiasSafety && otherStrokeContained;
            GD.Print(FormattableString.Invariant($"TASK17B_STROKE_SAMPLE case={label} marks={paint.MarksWorld.Length} darkWidth={darkWidth:0.0} antialiasSafety={antialiasSafety:0.0} minimumLocalClearance={minimum:0.000} otherStrokeContained={otherStrokeContained} strokeContained={strokeContained} actualWarning=True colliderHarmless=True transformedVisual=True consumedByDraw=True"));
            TestAssert.True(strokeContained,
                "Task17B " + label + " warning dark stroke plus antialias margin stays inside actual transformed hazard footprint");
        }, automatic: false);

    // For a convex literal quad, every endpoint's signed normal distance to
    // each edge bounds the entire round-capped straight segment's stroke.
    // Called on _Draw-local points, so visual scale/rotation are not erased.
    private static float MinimumLocalStrokeClearance(Vector2[] boundary, Vector2[] line)
    {
        if (boundary.Length != 4 || line.Length != 2)
            throw new InvalidOperationException("Stroke-clearance fixture requires a quadrilateral and one segment.");
        double twiceArea = 0;
        for (int i = 0; i < 4; i++) twiceArea += (double)boundary[i].Cross(boundary[(i + 1) % 4]);
        if (twiceArea == 0d) throw new InvalidOperationException("Stroke-clearance fixture requires nondegenerate real corners.");
        int insideSign = Math.Sign(twiceArea);
        float minimum = float.PositiveInfinity;
        foreach (Vector2 point in line) for (int i = 0; i < 4; i++)
        {
            Vector2 edge = boundary[(i + 1) % 4] - boundary[i];
            if (edge.LengthSquared() == 0f) throw new InvalidOperationException("Stroke-clearance fixture has a degenerate edge.");
            minimum = Math.Min(minimum, insideSign * edge.Cross(point - boundary[i]) / edge.Length());
        }
        return minimum;
    }

    private static void StrokeClearanceLiterals()
    {
        Vector2[] square = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        foreach (var sample in new[] {
            (Label: "safe", A: new Vector2(3, 3), B: new Vector2(3, 7), Expected: true),
            (Label: "radius-touches-edge", A: new Vector2(2, 3), B: new Vector2(2, 7), Expected: false),
            (Label: "radius-escapes-edge", A: new Vector2(1.5f, 3), B: new Vector2(1.5f, 7), Expected: false) })
        {
            float clearance = MinimumLocalStrokeClearance(square, new[] { sample.A, sample.B });
            bool contained = clearance >= 2.5f; // 4-unit stroke radius2 + .5 AA safety.
            GD.Print(FormattableString.Invariant($"TASK17B_STROKE_LITERAL case={sample.Label} minimumLocalClearance={clearance:0.000} expected={sample.Expected} contained={contained}"));
            TestAssert.True(contained == sample.Expected, "stroke-radius test predicate distinguishes " + sample.Label);
        }
        GD.Print("TASK17B_STROKE_LITERALS_OK cases=3 localRadius=2 antialiasSafety=0.5");
    }

    public static async Task RunFlowContracts(SceneTree tree)
    {
        PositiveAreaOverlapLiterals(); // Test predicate regression, not new paint behavior.
        await RunStateSilhouetteContracts(tree); // Existing twelve + three are prerequisites, not this RED.
        GD.Print("TASK17B_FLOW_NEIGHBORS_OK existingTwelveThree=true");
        var failures = new List<string>();
        foreach (string label in new[] { "Frozen", "Trench", "Transformed" })
        {
            GD.Print("TASK17B_FLOW_GROUP_BEGIN " + label);
            try { await ActualUpwardFlow(tree, label); GD.Print("TASK17B_FLOW_GROUP_OK " + label); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(label + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B consumed internal steam flow is absent or not rising\n" + string.Join("\n", failures));
        GD.Print("TASK17B_FLOW_CONTRACTS_OK consumedFilaments=true upward=true cosmeticOnly=true manualClock=true beautyCertified=false fullLevel=false");
    }

    // Breaks caught: missing/static/downward flow, merely recoloring a rigid
    // body, moving in world-up rather than actual nozzle->top, or advancing
    // hazard time to fake movement. Every FilamentsWorld path must be a flow
    // feature (static range hints cannot be mixed into this collection).
    private static Task ActualUpwardFlow(SceneTree tree, string label) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "flow-" + label, 0f, async f =>
        {
            Vector2[] literal = StateFlowLiteralFixture(f, label);
            CanyonSteamDiverterVisual visual = AddVisual(f); visual.SetProcess(false);
            visual.Position = new(17, 11); visual.Rotation = .31f; await PaintFrame(tree);
            visual._Process(0d); RequireBinding(visual);
            if (visual.Presentation.CosmeticSeconds != 0d || visual.IsProcessing())
                throw new InvalidOperationException("Flow fixture requires processing disabled before the first optional paint.");
            f.Pair.Advance(10d); f.Pair.Advance(10d); f.Pair.Advance(10d); await PaintFrame(tree); visual._Process(0d);
            if (!visual.Presentation.Right.Danger || visual.Presentation.Right.Warning || !f.Right.Monitoring || f.Right.Collision.Disabled)
                throw new InvalidOperationException("Flow fixture requires actual committed damaging active phase.");
            AssertCorners(visual.Presentation.Right.WorldCorners, literal);
            var frame = f.Right.Frame; float elapsed = f.Right.ElapsedSeconds;
            CanyonSteamDiverterSide selected = f.Pair.SelectedSide;
            Vector2 axis = (literal[0] + literal[1] - literal[2] - literal[3]) * .5f;
            float axisLength = axis.Length(); Vector2 upward = axis / axisLength;
            // Explicit short non-wrap window, not a loop/frame beauty metric.
            // Keep feature slots/point counts persistent only over this window;
            // no prescribed total curl count. At least .025 axis length per
            // .10s excludes numerical jitter (3.75px or transformed1.875px).
            Vector2[][] previous = null; bool allRising = true;
            for (int sample = 1; sample <= 3; sample++)
            {
                visual._Process(.10d); await PaintFrame(tree);
                double expectedPhase = sample * .10d;
                if (Math.Abs(visual.Presentation.CosmeticSeconds - expectedPhase) > 1e-12 ||
                    !f.Right.Frame.Equals(frame) || f.Right.ElapsedSeconds != elapsed || f.Pair.SelectedSide != selected ||
                    !visual.Presentation.Right.Danger || visual.Presentation.Right.Warning || !f.Right.Monitoring || f.Right.Collision.Disabled)
                    throw new InvalidOperationException("Flow cosmetic-only boundary changed actual elapsed/phase/selection/collider state.");
                Vector2[][] paths = Array.ConvertAll(visual.RightSteamPaint.FilamentsWorld, path => (Vector2[])path.Clone());
                bool drawable = paths.Length > 0;
                foreach (Vector2[] path in paths)
                {
                    float length = 0;
                    foreach (Vector2 point in path) RequireInsideLiteral(point, literal);
                    for (int point = 1; point < path.Length; point++) length += path[point].DistanceTo(path[point - 1]);
                    drawable &= path.Length >= 2 && length > 1f;
                }
                GD.Print(FormattableString.Invariant($"TASK17B_FLOW_SAMPLE case={label} phase={visual.Presentation.CosmeticSeconds:0.00} paths={paths.Length} drawable={drawable} axisLength={axisLength:0.000} actualActive=True actualWarning=False colliderActive=True gameplayUnchanged=True consumedByDraw=True"));
                allRising &= drawable;
                if (previous != null)
                {
                    bool persistent = paths.Length > 0 && paths.Length == previous.Length;
                    bool rising = persistent; float minimumRise = float.PositiveInfinity;
                    if (persistent) for (int path = 0; path < paths.Length; path++)
                    {
                        if (paths[path].Length < 2 || paths[path].Length != previous[path].Length) { persistent = false; rising = false; break; }
                        float rise = 0;
                        for (int point = 0; point < paths[path].Length; point++) rise += (paths[path][point] - previous[path][point]).Dot(upward);
                        rise /= paths[path].Length;
                        minimumRise = Math.Min(minimumRise, rise); rising &= rise >= axisLength * .025f;
                    }
                    allRising &= persistent && rising;
                    GD.Print(FormattableString.Invariant($"TASK17B_FLOW_INTERVAL case={label} from={(sample - 1) * .10d:0.00} to={expectedPhase:0.00} persistent={persistent} rising={rising} minimumRise={(float.IsFinite(minimumRise) ? minimumRise : 0):0.000} requiredRise={axisLength * .025f:0.000} gameplayUnchanged=True"));
                }
                previous = paths;
            }
            TestAssert.True(allRising, "Task17B " + label + " consumed drawable flow rises along actual nozzle-to-top axis over both short intervals");
        }, automatic: false);

    public static async Task RunStateSilhouetteContracts(SceneTree tree)
    {
        await RunRefinementContracts(tree); // All old five + three + four must pass first.
        GD.Print("TASK17B_SILHOUETTE_NEIGHBORS_OK existingFiveThreeFour=true");
        var failures = new List<string>();
        foreach (string label in new[] { "Frozen", "Trench", "Transformed" })
        {
            GD.Print("TASK17B_SILHOUETTE_GROUP_BEGIN " + label);
            try { await SamePhaseStateSilhouettes(tree, label); GD.Print("TASK17B_SILHOUETTE_GROUP_OK " + label); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(label + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B same-phase warning and active filled silhouettes remain identical\n" + string.Join("\n", failures));
        GD.Print("TASK17B_SILHOUETTE_CONTRACTS_OK samePhase=true consumedPolygons=true colorIgnored=true manualClock=true beautyCertified=false fullLevel=false");
    }

    // Break caught: warning/active reuse identical filled contours and differ
    // only by hue/opacity. Sample positions are independently literal footprint
    // cell centers: across (column+.5)/11, down (row+.5)/21. The 12/231 minimum
    // rejects color-only or numerical-jitter changes; it is NOT a readability
    // or beauty score and prescribes neither polygon shape nor form count.
    private static async Task SamePhaseStateSilhouettes(SceneTree tree, string label)
    {
        bool allDistinct = true;
        foreach (double phase in new[] { 0d, .37d, 1.11d })
            await CanyonSteamDiverterPhysicalTests.WithFixture(tree, "silhouette-" + label, 0f, async f =>
            {
                Vector2[] literal = StateFlowLiteralFixture(f, label);
                CanyonSteamDiverterVisual visual = AddVisual(f);
                visual.SetProcess(false); // Before the first explicit callback or awaited frame.
                visual.Position = new(17, 11); visual.Rotation = .31f;
                await PaintFrame(tree);
                visual._Process(0d); RequireBinding(visual);
                if (visual.Presentation.CosmeticSeconds != 0d || visual.IsProcessing())
                    throw new InvalidOperationException("Silhouette fixture requires optional processing disabled before first paint.");
                visual._Process(phase); // Only disclosed optional cosmetic phase, never a gameplay clock.
                f.Pair.Advance(10d); await PaintFrame(tree); visual._Process(0d);
                if (!visual.Presentation.Right.Warning || visual.Presentation.Right.Danger ||
                    f.Right.Monitoring || !f.Right.Collision.Disabled)
                    throw new InvalidOperationException("Silhouette fixture requires actual committed harmless warning.");
                AssertCorners(visual.Presentation.Right.WorldCorners, literal);
                double warningPhase = visual.Presentation.CosmeticSeconds;
                CanyonSteamDiverterSteamPaint warning = visual.RightSteamPaint;
                f.Pair.Advance(10d); f.Pair.Advance(10d); await PaintFrame(tree); visual._Process(0d);
                if (!visual.Presentation.Right.Danger || visual.Presentation.Right.Warning ||
                    !f.Right.Monitoring || f.Right.Collision.Disabled)
                    throw new InvalidOperationException("Silhouette fixture requires actual committed damaging active phase.");
                AssertCorners(visual.Presentation.Right.WorldCorners, literal);
                double activePhase = visual.Presentation.CosmeticSeconds;
                if (warningPhase != phase || activePhase != phase || warningPhase != activePhase)
                    throw new InvalidOperationException("Silhouette fixture must compare exact equal cosmetic phases.");
                CanyonSteamDiverterSteamPaint active = visual.RightSteamPaint;
                foreach (var paint in new[] { warning, active })
                    foreach (var form in paint.Forms)
                        foreach (Vector2 point in form.ContourWorld) RequireInsideLiteral(point, literal);
                int warningCovered = 0, activeCovered = 0, different = 0;
                for (int row = 0; row < 21; row++) for (int column = 0; column < 11; column++)
                {
                    float across = (column + .5f) / 11f, down = (row + .5f) / 21f;
                    Vector2 point = literal[0].Lerp(literal[1], across).Lerp(literal[3].Lerp(literal[2], across), down);
                    bool w = FilledAt(warning, point), a = FilledAt(active, point);
                    if (w) warningCovered++; if (a) activeCovered++; if (w != a) different++;
                }
                bool distinct = warningCovered > 0 && activeCovered > warningCovered && different >= 12;
                allDistinct &= distinct;
                GD.Print(FormattableString.Invariant($"TASK17B_SILHOUETTE_SAMPLE case={label} phase={phase:0.00} warningPhase={warningPhase:0.00} activePhase={activePhase:0.00} equalPhase=True committedWarning=True committedActive=True grid=231 warningCovered={warningCovered} activeCovered={activeCovered} different={different} distinct={distinct} colorIgnored=True consumedByDraw=True"));
            }, automatic: false);
        TestAssert.True(allDistinct, "Task17B " + label + " same-phase filled silhouettes differ on at least 12 of 231 samples with thinner warning");
    }

    // Test-local semantic-equivalence extraction shared by state/flow only.
    // Expectations stay hand-written; no production geometry helper computes them.
    private static Vector2[] StateFlowLiteralFixture(CanyonSteamDiverterPhysicalTests.Fixture f, string label)
    {
        if (label == "Transformed")
        {
            f.Right.Collision.Scale = new(2f, .5f); f.Right.Collision.Rotation = Mathf.Pi / 2;
            return new[] { new Vector2(537.5f, 229), new Vector2(537.5f, 341), new Vector2(462.5f, 341), new Vector2(462.5f, 229) };
        }
        bool frozen = label == "Frozen";
        Vector2 control = frozen ? new(1740, 2500) : new(3990, 4070);
        f.Pair.GlobalPosition = control; f.Target.GlobalPosition = control;
        f.Left.Position = (frozen ? new Vector2(1660, 2540) : new Vector2(3890, 4110)) - control;
        f.Right.Position = (frozen ? new Vector2(1820, 2540) : new Vector2(4110, 4110)) - control;
        ((RectangleShape2D)f.Left.Collision.Shape).Size = frozen ? new(56, 150) : new(64, 150);
        ((RectangleShape2D)f.Right.Collision.Shape).Size = frozen ? new(56, 150) : new(64, 150);
        return frozen ? new[] { new Vector2(1792, 2465), new Vector2(1848, 2465), new Vector2(1848, 2615), new Vector2(1792, 2615) }
            : new[] { new Vector2(4078, 4035), new Vector2(4142, 4035), new Vector2(4142, 4185), new Vector2(4078, 4185) };
    }
    private static bool FilledAt(CanyonSteamDiverterSteamPaint paint, Vector2 point)
    {
        // Exactly the filled contours consumed by _Draw; no opacity/hue/stroke
        // comparison, renderer replacement or approximation of their geometry.
        foreach (var form in paint.Forms)
            if (Geometry2D.IsPointInPolygon(point, form.ContourWorld)) return true;
        return false;
    }

    public static async Task RunRefinementContracts(SceneTree tree)
    {
        await RunInstallationPresentationContracts(tree); // Existing five + three are prerequisites, not this RED.
        GD.Print("TASK17B_REFINEMENT_NEIGHBORS_OK existingFiveAndThree=true");
        var failures = new List<string>();
        foreach (Func<SceneTree, Task> contract in new Func<SceneTree, Task>[] {
            DirectionFittings, FrozenPlume, TrenchPlume, TransformedPlume })
        {
            GD.Print("TASK17B_REFINEMENT_GROUP_BEGIN " + contract.Method.Name);
            try { await contract(tree); GD.Print("TASK17B_REFINEMENT_GROUP_OK " + contract.Method.Name); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(contract.Method.Name + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B steam render language remains ambiguous\n" + string.Join("\n", failures));
        GD.Print("TASK17B_REFINEMENT_CONTRACTS_OK consumedPaint=true component=true manualClock=true beautyCertified=false fullLevel=false");
    }

    // Breaks caught: stale selected fitting/needle, external navigation arrow,
    // or a freed control leaving ghost extracted geometry. No color preference.
    private static Task DirectionFittings(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "refinement-direction", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = AddVisual(f); await PaintFrame(tree); RequireBinding(visual);
            int externalArrows = 0;
            void Observe(CanyonSteamDiverterSide side)
            {
                var paint = visual.DirectionPaint; Vector2 control = new(400, 255);
                TestAssert.Equal(2, paint.Ports.Length, "actual draw has two pipe-mounted direction fittings");
                for (int i = 0; i < 2; i++)
                {
                    int sign = i == 0 ? -1 : 1;
                    TestAssert.True(paint.Ports[i].CenterWorld.DistanceTo(control + new Vector2(sign * 53, 0)) < .001f &&
                        paint.Ports[i].Selected == (i == 0 ? side == CanyonSteamDiverterSide.Left : side == CanyonSteamDiverterSide.Right),
                        "actual selected fitting follows desired side at the real pipe branch");
                    if (paint.Ports[i].ArrowheadWorld.Length != 0) externalArrows++;
                }
                TestAssert.True(paint.NeedleWorld.Length == 2 && paint.NeedleWorld[0].DistanceTo(control) < .001f &&
                    paint.NeedleWorld[1].DistanceTo(control) < 19 &&
                    Math.Sign(paint.NeedleWorld[1].X - control.X) == (side == CanyonSteamDiverterSide.Left ? -1 : 1),
                    "actual wheel needle stays confined and follows selected side");
            }
            Observe(CanyonSteamDiverterSide.Right);
            if (!f.Pair.TryReceiveThread(Interaction(f))) throw new InvalidOperationException("Refinement fixture cannot accept real LEFT receiver.");
            await PaintFrame(tree); Observe(CanyonSteamDiverterSide.Left);
            await f.Step(); f.Pair.Advance(10d); await PaintFrame(tree);
            if (!f.Pair.TryReceiveThread(Interaction(f))) throw new InvalidOperationException("Refinement fixture cannot accept real RIGHT receiver.");
            await PaintFrame(tree); Observe(CanyonSteamDiverterSide.Right);
            f.Target.Free(); await PaintFrame(tree);
            TestAssert.True(!visual.Presentation.IsBound && visual.DirectionPaint.Ports.Length == 0 &&
                visual.DirectionPaint.NeedleWorld.Length == 0 && visual.LeftSteamPaint.Forms.Length == 0 && visual.RightSteamPaint.Forms.Length == 0,
                "lost actual control clears consumed direction and steam geometry");
            GD.Print($"TASK17B_REFINEMENT_DIRECTION observedSides=Right,Left,Right externalArrowContours={externalArrows} consumedByDraw=true");
            TestAssert.True(externalArrows == 0, "Task17B actual direction fittings have no external navigation arrow contour");
        }, automatic: false);

    private static Task FrozenPlume(SceneTree tree) => PlumeGeometry(tree, "FrozenPlume");
    private static Task TrenchPlume(SceneTree tree) => PlumeGeometry(tree, "TrenchPlume");
    private static Task TransformedPlume(SceneTree tree) => PlumeGeometry(tree, "TransformedPlume");

    // Breaks caught: schematic closed active frame, sparse warning-only sparks,
    // wrong transformed bounds, plume on dormant/handoff, or opaque surfaces.
    // Rounded overlapping contours are structural honesty, not art approval.
    private static Task PlumeGeometry(SceneTree tree, string label) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "refinement-" + label, 0f, async f =>
        {
            Vector2[] literal;
            if (label == "TransformedPlume")
            {
                f.Right.Collision.Scale = new(2f, .5f); f.Right.Collision.Rotation = Mathf.Pi / 2;
                literal = new[] { new Vector2(537.5f, 229), new Vector2(537.5f, 341), new Vector2(462.5f, 341), new Vector2(462.5f, 229) };
            }
            else
            {
                bool frozen = label == "FrozenPlume";
                Vector2 control = frozen ? new(1740, 2500) : new(3990, 4070);
                f.Pair.GlobalPosition = control; f.Target.GlobalPosition = control;
                f.Left.Position = (frozen ? new Vector2(1660, 2540) : new Vector2(3890, 4110)) - control;
                f.Right.Position = (frozen ? new Vector2(1820, 2540) : new Vector2(4110, 4110)) - control;
                ((RectangleShape2D)f.Left.Collision.Shape).Size = frozen ? new(56, 150) : new(64, 150);
                ((RectangleShape2D)f.Right.Collision.Shape).Size = frozen ? new(56, 150) : new(64, 150);
                literal = frozen ? new[] { new Vector2(1792, 2465), new Vector2(1848, 2465), new Vector2(1848, 2615), new Vector2(1792, 2615) }
                    : new[] { new Vector2(4078, 4035), new Vector2(4142, 4035), new Vector2(4142, 4185), new Vector2(4078, 4185) };
            }
            CanyonSteamDiverterVisual visual = AddVisual(f);
            visual.Position = new(17, 11); visual.Rotation = .31f; await PaintFrame(tree); RequireBinding(visual);
            AssertCorners(visual.Presentation.Right.WorldCorners, literal);
            TestAssert.True(visual.LeftSteamPaint.Forms.Length == 0 && visual.RightSteamPaint.Forms.Length == 0,
                "dormant and initial safe jets paint no steam plume");
            f.Pair.Advance(10d); await PaintFrame(tree);
            if (!visual.Presentation.Right.Warning || f.Right.Monitoring || !f.Right.Collision.Disabled)
                throw new InvalidOperationException("Refinement fixture requires actual committed harmless warning.");
            bool warningPlume = CheckSteamGeometry(visual.RightSteamPaint, literal);
            double cosmetic = visual.Presentation.CosmeticSeconds;
            CanyonSteamDiverterSteamPaint pausedPaint = visual.RightSteamPaint;
            tree.Paused = true; await PaintFrame(tree); await PaintFrame(tree);
            TestAssert.Equal(cosmetic, visual.Presentation.CosmeticSeconds, "paused steam consumes no cosmetic time");
            TestAssert.Equal(pausedPaint.Forms.Length, visual.RightSteamPaint.Forms.Length, "paused steam retains visible forms");
            for (int form = 0; form < pausedPaint.Forms.Length; form++)
            {
                var before = pausedPaint.Forms[form]; var after = visual.RightSteamPaint.Forms[form];
                TestAssert.Equal(before.Opacity, after.Opacity, "paused steam opacity remains unchanged");
                TestAssert.Equal(before.ContourWorld.Length, after.ContourWorld.Length, "paused steam retains visible contour");
                for (int point = 0; point < before.ContourWorld.Length; point++)
                    TestAssert.True(before.ContourWorld[point].DistanceTo(after.ContourWorld[point]) < .001f, "paused steam contour does not move");
            }
            tree.Paused = false;
            f.Pair.Advance(10d); f.Pair.Advance(10d); await PaintFrame(tree);
            if (!visual.Presentation.Right.Danger || !f.Right.Monitoring || f.Right.Collision.Disabled)
                throw new InvalidOperationException("Refinement fixture requires actual committed damaging active phase.");
            bool activePlume = CheckSteamGeometry(visual.RightSteamPaint, literal);
            Vector2[] outline = visual.RightSteamPaint.HardOutlineWorld;
            bool hardRectangle = outline.Length >= 5 && outline[0].DistanceTo(outline[outline.Length - 1]) < .001f;
            foreach (Vector2 corner in literal)
            {
                bool reachesCorner = false;
                foreach (Vector2 point in outline) reachesCorner |= point.DistanceTo(corner) < .001f;
                hardRectangle &= reachesCorner;
            }
            if (!f.Pair.TryReceiveThread(Interaction(f))) throw new InvalidOperationException("Refinement fixture cannot enter real handoff.");
            await PaintFrame(tree);
            TestAssert.True(visual.Presentation.HandoffVapor && visual.LeftSteamPaint.Forms.Length == 0 && visual.RightSteamPaint.Forms.Length == 0 &&
                visual.LeftSteamPaint.HardOutlineWorld.Length == 0 && visual.RightSteamPaint.HardOutlineWorld.Length == 0,
                "small harmless wheel handoff is distinct from hazard plume geometry");
            GD.Print($"TASK17B_REFINEMENT_PLUME case={label} warningPlume={warningPlume} activePlume={activePlume} closedHardRectangle={hardRectangle} literalBounds=true consumedByDraw=true");
            TestAssert.True(warningPlume && activePlume && !hardRectangle,
                "Task17B " + label + " warning and active paint use overlapping translucent steam without a hard rectangular frame");
        }, automatic: false);

    private static bool CheckSteamGeometry(CanyonSteamDiverterSteamPaint paint, Vector2[] literal)
    {
        bool rounded = paint.Forms.Length >= 2;
        bool overlap = false;
        foreach (CanyonSteamDiverterSteamForm form in paint.Forms)
        {
            rounded &= form.ContourWorld.Length >= 6 && form.Opacity > 0 && form.Opacity < 1;
            foreach (Vector2 point in form.ContourWorld) RequireInsideLiteral(point, literal);
        }
        foreach (Vector2 point in paint.HardOutlineWorld) RequireInsideLiteral(point, literal);
        foreach (Vector2[] filament in paint.FilamentsWorld) foreach (Vector2 point in filament) RequireInsideLiteral(point, literal);
        for (int a = 0; a < paint.Forms.Length; a++) for (int b = a + 1; b < paint.Forms.Length; b++)
            overlap |= PositiveAreaOverlap(paint.Forms[a].ContourWorld, paint.Forms[b].ContourWorld);
        return rounded && overlap;
    }

    // The former a-vertex-in-b check missed outer-first containment and a
    // crossing with no vertices inside either polygon. Boundary contact is
    // not overlapping filled steam: require positive shared area instead.
    private static bool PositiveAreaOverlap(Vector2[] a, Vector2[] b)
    {
        double twiceArea = 0;
        foreach (Vector2[] contour in Geometry2D.IntersectPolygons(a, b))
        {
            if (contour.Length < 3) continue;
            Vector2 origin = contour[0];
            for (int i = 1; i + 1 < contour.Length; i++)
            {
                Vector2 p = contour[i] - origin, q = contour[i + 1] - origin;
                twiceArea += (double)p.X * q.Y - (double)p.Y * q.X;
            }
        }
        // Signed contours account for API hole winding; translation avoids
        // cancellation from large world coordinates. No beauty/size threshold.
        return Math.Abs(twiceArea) > 0d;
    }

    private static void PositiveAreaOverlapLiterals()
    {
        Vector2[] outer = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] inner = { new(2, 2), new(4, 2), new(4, 4), new(2, 4) };
        Vector2[] reversedInner = { new(2, 4), new(4, 4), new(4, 2), new(2, 2) };
        Vector2[] horizontal = { new(-3, -1), new(3, -1), new(3, 1), new(-3, 1) };
        Vector2[] vertical = { new(-1, -3), new(1, -3), new(1, 3), new(-1, 3) };
        Vector2[] separated = { new(12, 0), new(14, 0), new(14, 2), new(12, 2) };
        Vector2[] edgeTouch = { new(10, 2), new(12, 2), new(12, 4), new(10, 4) };
        Vector2[] pointTouch = { new(10, 10), new(12, 10), new(12, 12), new(10, 12) };
        foreach (var sample in new[] {
            (Label: "contained", A: outer, B: inner, Expected: true),
            (Label: "contained-reversed-winding", A: outer, B: reversedInner, Expected: true),
            (Label: "cross-no-contained-vertices", A: horizontal, B: vertical, Expected: true),
            (Label: "separated", A: outer, B: separated, Expected: false),
            (Label: "edge-contact-only", A: outer, B: edgeTouch, Expected: false),
            (Label: "point-contact-only", A: outer, B: pointTouch, Expected: false) })
        {
            bool forward = PositiveAreaOverlap(sample.A, sample.B);
            bool reverse = PositiveAreaOverlap(sample.B, sample.A);
            GD.Print($"TASK17B_OVERLAP_LITERAL case={sample.Label} expected={sample.Expected} forward={forward} reverse={reverse} positiveArea=true");
            TestAssert.True(forward == sample.Expected && reverse == sample.Expected,
                "test-local positive-area overlap handles " + sample.Label + " in both argument orders");
        }
        GD.Print("TASK17B_OVERLAP_LITERALS_OK cases=6 argumentOrders=12 testPredicateOnly=true");
    }
    private static void RequireInsideLiteral(Vector2 point, Vector2[] literal)
    {
        TestAssert.True(float.IsFinite(point.X) && float.IsFinite(point.Y), "steam geometry remains finite");
        float sign = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector2 edge = literal[(i + 1) % 4] - literal[i];
            float cross = edge.Cross(point - literal[i]);
            if (Math.Abs(cross) <= edge.Length() * .001f) continue;
            if (sign == 0) sign = Math.Sign(cross);
            TestAssert.True(Math.Sign(cross) == sign, "consumed steam stays inside independently literal transformed hazard footprint");
        }
    }

    public static async Task RunInstallationPresentationContracts(SceneTree tree)
    {
        // Old contracts are prerequisites, never reclassified as this RED.
        await RunContracts(tree);
        GD.Print("TASK17B_PRESENTATION_NEIGHBORS_OK existingVisualContracts=true");
        var failures = new List<string>();
        foreach (Func<SceneTree, Task> contract in new Func<SceneTree, Task>[] {
            FrozenSupport, TrenchSupport, WarningSilhouette })
        {
            GD.Print("TASK17B_PRESENTATION_GROUP_BEGIN " + contract.Method.Name);
            try { await contract(tree); GD.Print("TASK17B_PRESENTATION_GROUP_OK " + contract.Method.Name); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(contract.Method.Name + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B installed steam presentation contracts are not satisfied\n" + string.Join("\n", failures));
        GD.Print("TASK17B_PRESENTATION_CONTRACTS_OK component=true manualClock=true fullLevel=false");
    }

    // Literal approved world coordinates, not a generated-level/route fixture.
    private static Task FrozenSupport(SceneTree tree) => SupportContact(tree, true);
    private static Task TrenchSupport(SceneTree tree) => SupportContact(tree, false);
    private static Task SupportContact(SceneTree tree, bool frozen) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, frozen ? "frozen-support-paint" : "trench-support-paint", 0f, async f =>
        {
            string label = frozen ? "frozen" : "trench";
            Vector2 control = frozen ? new(1740, 2500) : new(3990, 4070);
            Vector2 left = frozen ? new(1660, 2540) : new(3890, 4110);
            Vector2 right = frozen ? new(1820, 2540) : new(4110, 4110);
            float mouthY = frozen ? 2615f : 4185f, floorTopY = frozen ? 2640f : 4236f;
            // Disclosed presentation-only fixture geometry setup. No player,
            // health, save, input, production floor or shared helper is changed.
            f.Pair.GlobalPosition = control;
            f.Left.Position = left - control; f.Right.Position = right - control;
            ((RectangleShape2D)f.Left.Collision.Shape).Size = frozen ? new(56, 150) : new(64, 150);
            ((RectangleShape2D)f.Right.Collision.Shape).Size = frozen ? new(56, 150) : new(64, 150);
            f.Target.GlobalPosition = control;
            StaticBody2D floor = f.Root.GetNode<StaticBody2D>("Floor");
            CollisionShape2D support = floor.GetNode<CollisionShape2D>("Collision");
            var borrowed = (RectangleShape2D)support.Shape;
            floor.GlobalPosition = frozen ? new(1750, 2660) : new(3620, 4256);
            borrowed.Size = frozen ? new(340, 40) : new(1160, 40);
            CanyonSteamDiverterVisual visual = AddVisual(f);
            visual.SupportShapePath = new("../../Floor/Collision");
            visual.Position = new(17, 11); visual.Rotation = .31f;
            await PaintFrame(tree); RequireBinding(visual);
            float half = frozen ? 28f : 32f;
            AssertCorners(visual.Presentation.Left.WorldCorners, new(left.X - half, mouthY - 150),
                new(left.X + half, mouthY - 150), new(left.X + half, mouthY), new(left.X - half, mouthY));
            AssertCorners(visual.Presentation.Right.WorldCorners, new(right.X - half, mouthY - 150),
                new(right.X + half, mouthY - 150), new(right.X + half, mouthY), new(right.X - half, mouthY));
            Vector2 actualFloorTop = support.GlobalTransform * new Vector2(0, -20);
            if (Mathf.Abs(actualFloorTop.Y - floorTopY) > .001f || support.Shape != borrowed)
                throw new InvalidOperationException("Task17B support fixture's literal actual floor rectangle prerequisite failed.");
            GD.Print($"TASK17B_SUPPORT_CONTACT {label} mouthY={mouthY:0} floorTopY={floorTopY:0} leftPoints={visual.SupportPaint.LeftStemWorld.Length} rightPoints={visual.SupportPaint.RightStemWorld.Length} component=true");
            TestAssert.True(visual.SupportPaint.LeftStemWorld.Length == 2 && visual.SupportPaint.RightStemWorld.Length == 2,
                label + " support paint connects each actual nozzle to existing floor top");
            AssertStem(visual.SupportPaint.LeftStemWorld, new(left.X, mouthY), new(left.X, floorTopY));
            AssertStem(visual.SupportPaint.RightStemWorld, new(right.X, mouthY), new(right.X, floorTopY));
            if (frozen)
            {
                // Hand-derived top-edge intersections after actual rectangle
                // scale/rotation, independent of the production projection.
                floor.GlobalPosition = new(1750, 2760);
                support.Scale = new(2f, .5f); support.Rotation = Mathf.Pi / 6f;
                await PaintFrame(tree);
                AssertStem(visual.SupportPaint.LeftStemWorld, new(1660, 2615), new(1660, 2696.4915f));
                AssertStem(visual.SupportPaint.RightStemWorld, new(1820, 2615), new(1820, 2788.8675f));
                support.Scale = Vector2.One; support.Rotation = 0f; floor.GlobalPosition = new(1750, 2660);
            }
            f.Pair.Advance(10d); f.Pair.Advance(10d); f.Pair.Advance(10d); await PaintFrame(tree);
            if (!visual.Presentation.Right.Danger || f.Right.Collision.Disabled || !f.Right.Monitoring)
                throw new InvalidOperationException("Task17B support optional-loss fixture requires committed actual active right hazard.");
            var frame = f.Right.Frame;
            float elapsed = f.Right.ElapsedSeconds;
            CanyonSteamDiverterSide selected = f.Pair.SelectedSide;
            Vector2[] corners = (Vector2[])visual.Presentation.Right.WorldCorners.Clone();
            async Task CheckOptionalLoss()
            {
                await PaintFrame(tree);
                TestAssert.True(visual.SupportPaint.LeftStemWorld.Length == 0 && visual.SupportPaint.RightStemWorld.Length == 0,
                    "absent/missing/unsupported/removed optional support suppresses only support paint");
                TestAssert.True(visual.Presentation.IsBound && visual.Presentation.Right.Danger &&
                    !f.Right.Collision.Disabled && f.Right.Monitoring && GodotObject.IsInstanceValid(borrowed),
                    "optional support loss preserves committed hazard and borrowed rectangle lifetime");
                TestAssert.Equal(frame, f.Right.Frame, "support paint cannot change committed gameplay phase");
                TestAssert.Equal(elapsed, f.Right.ElapsedSeconds, "support paint cannot advance gameplay clock");
                TestAssert.Equal(selected, f.Pair.SelectedSide, "support paint cannot switch controller selection");
                AssertCorners(visual.Presentation.Right.WorldCorners, corners);
            }
            visual.SupportShapePath = new(); await CheckOptionalLoss();
            visual.SupportShapePath = new("../../Missing/Collision"); await CheckOptionalLoss();
            visual.SupportShapePath = new("../../Floor/Collision");
            support.Shape = new CircleShape2D { Radius = 20 }; await CheckOptionalLoss();
            support.Shape = borrowed; await PaintFrame(tree);
            AssertStem(visual.SupportPaint.RightStemWorld, new(right.X, mouthY), new(right.X, floorTopY));
            support.Free(); await CheckOptionalLoss();
        }, automatic: false);

    private static Task WarningSilhouette(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "warning-paint-silhouette", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = AddVisual(f);
            f.Pair.Advance(10d); await PaintFrame(tree); RequireBinding(visual);
            CanyonSteamDiverterWarningPaint paint = visual.RightWarningPaint;
            if (!visual.Presentation.Right.Warning || visual.Presentation.Right.Danger ||
                !f.Right.Collision.Disabled || f.Right.Monitoring || paint.MarksWorld.Length == 0)
                throw new InvalidOperationException("Task17B warning fixture requires actual harmless warning and nonempty DrawFootprint paint snapshot.");
            // Actual fixture footprint is hand-derived: x472..528,y210..360.
            AssertCorners(visual.Presentation.Right.WorldCorners, new(472, 210), new(528, 210), new(528, 360), new(472, 360));
            int fullWidthRungs = 0, continuousRails = 0;
            foreach (Vector2[] mark in paint.MarksWorld)
            {
                if (mark.Length != 2) throw new InvalidOperationException("Task17B warning paint requires two-point consumed strokes.");
                if (Mathf.Abs(mark[0].Y - mark[1].Y) < .001f && Mathf.Abs(mark[0].X - mark[1].X) >= 50f) fullWidthRungs++;
                foreach (Vector2 point in mark) RequireInsideWarning(point);
            }
            for (int index = 1; index < paint.OutlineWorld.Length; index++)
                if (Mathf.Abs(paint.OutlineWorld[index].X - paint.OutlineWorld[index - 1].X) < .001f &&
                    Mathf.Abs(paint.OutlineWorld[index].Y - paint.OutlineWorld[index - 1].Y) >= 100f) continuousRails++;
            foreach (Vector2 point in paint.OutlineWorld) RequireInsideWarning(point);
            GD.Print($"TASK17B_WARNING_GEOMETRY fullWidthRungs={fullWidthRungs} continuousRails={continuousRails} snapshotConsumedByDraw=true component=true");
            TestAssert.True(fullWidthRungs == 0 && continuousRails == 0,
                "Task17B warning paint avoids continuous ladder rails and full-width rungs");
        }, automatic: false);

    private static void RequireInsideWarning(Vector2 point) => TestAssert.True(
        float.IsFinite(point.X) && float.IsFinite(point.Y) && point.X >= 471.999f && point.X <= 528.001f &&
        point.Y >= 209.999f && point.Y <= 360.001f, "warning strokes stay inside unchanged actual hazard footprint");
    private static void AssertStem(Vector2[] actual, Vector2 mouth, Vector2 floorTop)
    {
        TestAssert.True(actual.Length == 2 && actual[0].DistanceTo(mouth) < .001f && actual[1].DistanceTo(floorTop) < .001f,
            "support stroke joins actual mouth to independently hand-derived real rectangle top");
    }

    public static async Task RunContracts(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (Func<SceneTree, Task> contract in new Func<SceneTree, Task>[] {
            DirectionsAndCommittedHazards, TransformedActualShapes, PauseAndRemoval, MissingReferences, FreedJetReference })
        {
            GD.Print("TASK17A_VISUAL_GROUP_BEGIN " + contract.Method.Name);
            try { await contract(tree); GD.Print("TASK17A_VISUAL_GROUP_OK " + contract.Method.Name); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(contract.Method.Name + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: paired diverter presentation contracts are not satisfied\n" + string.Join("\n", failures));
    }

    // Break caught: arrow/pipe selection or handoff vapor disagrees with the
    // controller; warning/danger paint appears on a disarmed jet.
    private static Task DirectionsAndCommittedHazards(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "visual-directions-controlled", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = AddVisual(f);
            await PaintFrame(tree);
            RequireBinding(visual);
            TestAssert.Equal(CanyonSteamDiverterSide.Right, visual.Presentation.SelectedSide, "default arrow and selected pipe point right");
            AssertPaint(visual.Presentation, false, false, false, false, false);
            TestAssert.True(f.Pair.TryReceiveThread(Interaction(f)), "presentation fixture's direct left receiver is accepted");
            await PaintFrame(tree);
            TestAssert.Equal(CanyonSteamDiverterSide.Left, visual.Presentation.SelectedSide, "handoff arrow shows desired left, not stale outgoing right");
            AssertPaint(visual.Presentation, true, false, false, false, false);
            await f.Step(); f.Pair.Advance(10d); await PaintFrame(tree);
            AssertPaint(visual.Presentation, false, false, false, false, false);
            f.Pair.Advance(10d); await PaintFrame(tree); // Early warning.
            AssertPaint(visual.Presentation, false, true, false, false, false);
            f.Pair.Advance(10d); await PaintFrame(tree); // Ordinary warning.
            AssertPaint(visual.Presentation, false, true, false, false, false);
            f.Pair.Advance(10d); await PaintFrame(tree); // Actual active collision.
            AssertPaint(visual.Presentation, false, false, true, false, false);
            TestAssert.True(!f.Left.Collision.Disabled && f.Left.Monitoring, "danger presentation corresponds to committed active left collision");
            TestAssert.True(f.Pair.TryReceiveThread(Interaction(f)), "presentation fixture's reverse receiver is accepted");
            await PaintFrame(tree);
            TestAssert.Equal(CanyonSteamDiverterSide.Right, visual.Presentation.SelectedSide, "reverse handoff arrow immediately points right");
            AssertPaint(visual.Presentation, true, false, false, false, false);
        }, automatic: false);

    // Break caught: decorative approximations ignore collision size/rotation/
    // scale or the visual's own transform. Literal corners are hand-derived.
    private static Task TransformedActualShapes(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "visual-shapes-controlled", 0f, async f =>
        {
            f.Right.Collision.Scale = new Vector2(2f, .5f);
            f.Right.Collision.Rotation = Mathf.Pi / 2f;
            CanyonSteamDiverterVisual visual = AddVisual(f);
            visual.Position = new Vector2(17f, 11f); visual.Rotation = .31f;
            await PaintFrame(tree); RequireBinding(visual);
            TestAssert.Equal(new Vector2(400f, 255f), visual.Presentation.ControlWorldPosition, "pump joins the actual claw control world anchor");
            AssertCorners(visual.Presentation.Left.WorldCorners,
                new(272f, 210f), new(328f, 210f), new(328f, 360f), new(272f, 360f));
            AssertCorners(visual.Presentation.Right.WorldCorners,
                new(537.5f, 229f), new(537.5f, 341f), new(462.5f, 341f), new(462.5f, 229f));
        }, automatic: false);

    // Break caught: cosmetic motion advances while paused; optional paint
    // removal changes selected side, clocks, collision, or healthy pocket.
    private static Task PauseAndRemoval(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "visual-pause-removal", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = AddVisual(f); await PaintFrame(tree); RequireBinding(visual);
            await f.Step(); await PaintFrame(tree);
            double cosmetic = visual.Presentation.CosmeticSeconds;
            float rightTime = f.Right.ElapsedSeconds;
            tree.Paused = true; await PaintFrame(tree); await PaintFrame(tree);
            TestAssert.Equal(cosmetic, visual.Presentation.CosmeticSeconds, "paused optional vapor motion consumes no cosmetic time");
            TestAssert.Equal(rightTime, f.Right.ElapsedSeconds, "paused presentation cannot advance gameplay clock");
            tree.Paused = false;
            visual.Free();
            await f.ObserveTwoCycles(CanyonSteamDiverterSide.Right, 100, mustDamage: false, pocket: true);
            TestAssert.Equal(CanyonSteamDiverterSide.Right, f.Pair.SelectedSide, "deleting actual presentation leaves automatic route choice unchanged");
            TestAssert.Equal(100, f.Health.CurrentHealth, "deleting presentation cannot remove safe pocket protection");
        });

    // Break caught: missing/freed dependencies retain ghost hazard feedback or
    // throw an engine error. This is independent of controller retirement.
    private static Task MissingReferences(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "visual-missing-refs", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = AddVisual(f); await PaintFrame(tree); RequireBinding(visual);
            f.Target.Free(); await PaintFrame(tree);
            TestAssert.True(!visual.Presentation.IsBound && !visual.Presentation.HandoffVapor &&
                !visual.Presentation.Left.Warning && !visual.Presentation.Left.Danger &&
                !visual.Presentation.Right.Warning && !visual.Presentation.Right.Danger,
                "freed control clears optional feedback instead of retaining a ghost connection");
            visual.Free();
            var orphan = new CanyonSteamDiverterVisual { ControllerPath = new("Missing"), ControlPath = new("Missing") };
            f.Root.AddChild(orphan); await PaintFrame(tree);
            TestAssert.True(!orphan.Presentation.IsBound, "missing presentation paths fail closed without dereferencing a freed object");
        }, automatic: false);

    internal static CanyonSteamDiverterVisual AddVisual(CanyonSteamDiverterPhysicalTests.Fixture f)
    {
        var visual = new CanyonSteamDiverterVisual { Name = "DiverterPaint" };
        f.Pair.AddChild(visual); return visual;
    }
    // Break caught: a freed child collision/jet remains in the paint snapshot.
    private static Task FreedJetReference(SceneTree tree) =>
        CanyonSteamDiverterPhysicalTests.WithFixture(tree, "visual-freed-jet", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = AddVisual(f); await PaintFrame(tree); RequireBinding(visual);
            f.Left.Free(); await PaintFrame(tree);
            TestAssert.True(!visual.Presentation.IsBound && !visual.Presentation.Left.Warning &&
                !visual.Presentation.Left.Danger && !visual.Presentation.Right.Warning && !visual.Presentation.Right.Danger,
                "freed actual jet clears optional hazard feedback safely");
        }, automatic: false);
    private static ThreadInteraction Interaction(CanyonSteamDiverterPhysicalTests.Fixture f) =>
        new(f.Player, f.Player.GlobalPosition, f.Target.WorldAnchor, new ThreadAbilitySettings());
    private static async Task PaintFrame(SceneTree tree)
    {
        // ProcessFrame emits before node _Process: the second signal observes
        // the completed first paint update, without calling production callbacks.
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
    private static void RequireBinding(CanyonSteamDiverterVisual visual) =>
        TestAssert.True(visual.IsNodeReady() && visual.Presentation.IsBound, "removable visual binds committed controller and actual control");
    private static void AssertPaint(CanyonSteamDiverterPresentation frame, bool vapor,
        bool leftWarning, bool leftDanger, bool rightWarning, bool rightDanger)
    {
        TestAssert.Equal(vapor, frame.HandoffVapor, "distinct harmless vapor is present only during committed handoff");
        TestAssert.Equal(leftWarning, frame.Left.Warning, "left warning paint matches committed phase");
        TestAssert.Equal(leftDanger, frame.Left.Danger, "left danger paint matches committed collision");
        TestAssert.Equal(rightWarning, frame.Right.Warning, "right warning paint matches committed phase");
        TestAssert.Equal(rightDanger, frame.Right.Danger, "right danger paint matches committed collision");
    }
    private static void AssertCorners(Vector2[] actual, params Vector2[] literal)
    {
        TestAssert.Equal(4, actual.Length, "paint uses all four actual transformed collision corners");
        for (int index = 0; index < 4; index++)
            TestAssert.True(actual[index].DistanceTo(literal[index]) < .001f, "warning/danger corner matches independently hand-derived world point " + index);
    }
}
