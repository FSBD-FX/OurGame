using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation.Fog;

namespace Knight.Tests.Presentation;

/// <summary>
/// Actual GPU readback, not material-parameter or CPU-model acceptance. These fixtures
/// distance fixtures use the authored entrance field, where maximum opacity is .28,
/// clear radius 180 and outer/beam range 620. Region fixtures sample independently
/// checked authored bounds, including the final rest row's 32px inward transition.
/// Expected white-background RGB values below are independently calculated literals.
/// No image, save, settings, telemetry or other user-data files are written.
/// </summary>
public static class HeadlampFogRasterTests
{
    private static readonly Vector2 Actor = new(128f, 192f);
    private static readonly Vector2 Lamp = new(160f, 192f);
    private const float PixelTolerance = .008f;

    // A bounded diagnostic, not full-route FPS acceptance. Same living actor,
    // unchanged level/settings/workload, old->candidate->old, all endpoints kept.
    public static async Task RunNormalShaderComparison(SceneTree tree)
    {
        const string baselinePath = "E:/OurGame/Knight/.evidence/october13-stable-claw/fog-raster-20261013-021818-817-186fe045d70e4690a9a6360171409489/before/assets/shaders/prologue-fog.gdshader";
        byte[] original = File.ReadAllBytes(baselinePath);
        TestAssert.Equal("6A49749E031DE9D2A06C6F4CBBBBC4C6ADD0ECED5A2228CEBC535ED54FF41D27",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)), "frozen pre-edit shader identity");
        TestAssert.True(Engine.TimeScale == 1 && !OS.GetCmdlineArgs().Contains("--fixed-fps") &&
            !OS.GetCmdlineArgs().Contains("--write-movie"), "normal shader comparison has no time substitution");
        MinimapStrictSampleBufferTests.Run();
        using var baseline = new Shader { Code = Encoding.UTF8.GetString(original) };
        using PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        var level = packed.Instantiate<Knight.Prologue.PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level,"fog-shader-normal-comparison");
        var flow = level.GetNode<Knight.Prologue.PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
        var buffers = new[] { new MinimapStrictSampleBuffer(60,240), new MinimapStrictSampleBuffer(60,240), new MinimapStrictSampleBuffer(60,240) };
        ShaderMaterial material=null; Shader candidate=null;
        int focusEvents=0; Action focus=()=>focusEvents++;
        tree.Root.FocusEntered+=focus; tree.Root.FocusExited+=focus;
        var started=Stopwatch.StartNew();
        try
        {
            tree.Root.AddChild(level);
            for(int i=0;i<5;i++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            flow.StartNew();
            for(int i=0;i<60;i++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var actor=level.Player; var health=actor.GetNode<HealthComponent>("Health");
            var fog=level.GetNode<PrologueFogCanvas>("Fog/Canvas");
            material=(ShaderMaterial)fog.Material; candidate=material.Shader;
            ulong actorId=actor.GetInstanceId(),worldId=level.GetNode<Node2D>("WorldRoot").GetInstanceId();
            Vector2 position=actor.GlobalPosition; int hp=health.CurrentHealth;
            ulong Support()=>actor.GetSlideCollisionCount()>0 && actor.GetSlideCollision(0).GetCollider() is GodotObject body ? body.GetInstanceId():0;
            ulong support=Support(); Vector2I size=tree.Root.Size;
            int maxFps=Engine.MaxFps; var vsync=DisplayServer.WindowGetVsyncMode();
            string settings=System.Text.Json.JsonSerializer.Serialize(flow.Settings);
            int initialFocusEvents=focusEvents;
            GD.Print($"FOG_SHADER_PERF_CONTEXT actor={actorId} position={position} support={support} hp={hp} size={size} adapter={RenderingServer.GetVideoAdapterName()} frequency={Stopwatch.Frequency} warmup=60 intervals=240 arms=old-candidate-old");
            for(int arm=0;arm<3;arm++)
            {
                material.Shader=arm==1?candidate:baseline;
                var expected=material.Shader;
                for(int i=0;i<buffers[arm].Endpoints.Length;i++)
                {
                    TestAssert.True(started.Elapsed.TotalSeconds<45,"shader comparison stops incomplete at45s; never substitutes endpoints");
                    await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    bool valid=flow.State==Knight.Prologue.PrologueGameFlowState.Playing && !tree.Paused && actor.IsOnFloor() &&
                        actor.GetInstanceId()==actorId && actor.GlobalPosition==position && health.CurrentHealth==hp && Support()==support && support!=0 &&
                        fog.FrameValid && fog.Visible && material.Shader==expected && tree.Root.Visible && tree.Root.Size==size &&
                        Engine.TimeScale==1 && Engine.MaxFps==maxFps && DisplayServer.WindowGetVsyncMode()==vsync;
                    buffers[arm].Add(new MinimapTimingEndpoint(Stopwatch.GetTimestamp(),Engine.GetProcessFrames(),
                        System.Environment.ProcessId,System.Environment.CurrentManagedThreadId,worldId,actorId,Support(),hp,actor.GlobalPosition,tree.Root.HasFocus(),valid));
                }
                GD.Print($"FOG_SHADER_PERF_ARM arm={arm} count={buffers[arm].Count} complete={buffers[arm].Complete}");
            }
            TestAssert.True(focusEvents==initialFocusEvents && buffers.All(b=>b.Complete) &&
                System.Text.Json.JsonSerializer.Serialize(flow.Settings)==settings,"shader comparison all endpoints/workload/focus/settings valid");
            double before=buffers[0].P95Milliseconds(Stopwatch.Frequency),after=buffers[1].P95Milliseconds(Stopwatch.Frequency),recovered=buffers[2].P95Milliseconds(Stopwatch.Frequency);
            string result=FormattableString.Invariant($"before_p95_ms={before}\ncandidate_p95_ms={after}\nrecovered_p95_ms={recovered}\nfrequency={Stopwatch.Frequency}\n");
            File.WriteAllText(Path.Combine(paths.CaseRoot,"comparison.txt"),result);
            GD.Print("FOG_SHADER_PERF_COMPARISON "+result.Replace('\n',' '));
            TestAssert.True(MinimapStrictSampleBuffer.Compare(before,after,recovered),"FogShaderPerformanceBudget: baseline recovery10percent and feature5percent");
        }
        finally
        {
            if(material!=null && GodotObject.IsInstanceValid(material)) material.Shader=candidate;
            tree.Root.FocusEntered-=focus; tree.Root.FocusExited-=focus;
            for(int arm=0;arm<3;arm++)
            {
                var rows=new StringBuilder("index,ticks,frame,actor,support,health,x,y,focused,valid\n");
                for(int i=0;i<buffers[arm].Count;i++)
                {var e=buffers[arm].Endpoints[i];rows.AppendLine(FormattableString.Invariant($"{i},{e.Ticks},{e.Frame},{e.Actor},{e.Support},{e.Health},{e.Position.X},{e.Position.Y},{e.Focused},{e.Valid}"));}
                File.WriteAllText(Path.Combine(paths.CaseRoot,$"arm-{arm}.csv"),rows.ToString());
            }
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
        }
    }

    public static async Task Run(SceneTree tree)
    {
        await DistanceAndDirectionReachPixels(tree);
        await ReducedFogChangesPixelsWithoutChangingDistanceBeamOrNativeOcclusion(tree);
        await LampOriginIsNotActorOrigin(tree);
        await NativeWallBlocksNearClearAndBeamAndSnapshotsStayFrozen(tree);
        await RealCameraTranslationAndZoomReachPixels(tree);
        await SameCanvasInstanceRendersAfterReentryWithoutRequestReady(tree);
        await NonDiagonalProjectionPreservesBothAxesAndTranslation(tree);
        await RegionRowsResolveAtTheShadedWorldPoint(tree);
        await LogicalCanvasStretchPreservesWorldSamplesAtBothResolutions(tree);
        await BothNativeRectangleRowsReachDistinctShadowPixels(tree);
        // Keep the independently discovered overflow boundary last so the ordinary
        // projection/field/texture cases report before this intentionally new RED.
        await FiniteProjectionWhichOverflowsViewportIsRejectedConservatively(tree);
        await ShadowSilhouetteFadesOnVisibleSideWithoutRevealingBehindWall(tree);
        await TransformedAndLampOnlySilhouettesKeepTheirSoftBlockedBoundary(tree);
    }

    // Catches the binary visible/hidden discontinuity at a real rectangle's
    // silhouette. The shadowed side must remain fully occluded; softness cannot
    // be obtained by blurring light through the wall or changing its collision.
    private static async Task ShadowSilhouetteFadesOnVisibleSideWithoutRevealingBehindWall(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogOneSidedShadowEdge");
        CollisionShape2D collider = fixture.AddWorldRectangle(new Rect2(220f, 176f, 16f, 32f));
        await PhysicsFrames(tree, 2);
        HeadlampOcclusionFrame closed = fixture.Sampler.Capture();
        // Actor=(128,192), near top corner=(220,176). At x=400 the
        // upper silhouette crosses y=144.69565. These samples are independent.
        TestAssert.True(closed.CanSee(Actor, new Vector2(400.5f, 138.5f)) &&
            !closed.CanSee(Actor, new Vector2(400.5f, 150.5f)),
            "shadow-edge fixture straddles the exact native silhouette");
        fixture.Present(Actor, Actor, Vector2.Zero, Transform2D.Identity, closed);
        using Image shaded = await fixture.ReadPixels();
        string evidence = IsolatedPrologueFixture.CreatePaths("fog-shadow-edge").CaseRoot;
        TestAssert.Equal(Error.Ok, shaded.SavePng(System.IO.Path.Combine(evidence, "shadow-edge.png")),
            "retain original GPU shadow-edge evidence");
        float penumbra = shaded.GetPixel(400, 138).R;
        GD.Print($"FOG_SHADOW_EDGE_RGB y112={shaded.GetPixel(400,112).R} y132={shaded.GetPixel(400,132).R} y138={penumbra} y143={shaded.GetPixel(400,143).R} y150={shaded.GetPixel(400,150).R} evidence={evidence}");
        TestAssert.True(penumbra > .725f && penumbra < .94f,
            $"FogShadowVisibleSideFeather: visible pixel beside shadow must be intermediate, actual={penumbra}");
        AssertGray(shaded, 400, 150, .72f, "soft edge never clears a truly occluded point");
        AssertGray(shaded, 400, 192, .72f, "shadow interior remains fully occluded");
        AssertGray(shaded, 208, 192, 1f, "near side before the wall stays clear");
        float maximumStep = 0f;
        for (int y = 112; y < 151; y++)
            maximumStep = Mathf.Max(maximumStep, Mathf.Abs(shaded.GetPixel(400, y).R - shaded.GetPixel(400, y + 1).R));
        TestAssert.True(maximumStep <= .03f,
            $"FogShadowContinuousEdge: adjacent pixels must fade rather than cut; maximumStep={maximumStep}");

        collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        await PhysicsFrames(tree, 2);
        fixture.Present(Actor, Actor, Vector2.Zero, Transform2D.Identity, fixture.Sampler.Capture());
        using Image opened = await fixture.ReadPixels();
        TestAssert.True(opened.GetPixel(400,138).R > .95f,
            "opening actual native geometry removes its edge shade without residual darkness");
        GD.Print($"HEADLAMP_FOG_RASTER_SHADOW_EDGE_OK max_step={maximumStep}");
    }

    // Catches local-space feather widths, forgotten basis inversion, and applying
    // softness to the actor circle but leaving the actual lamp shadow binary.
    private static async Task TransformedAndLampOnlySilhouettesKeepTheirSoftBlockedBoundary(SceneTree tree)
    {
        foreach ((string name, Transform2D transform, bool lampOnly) in new[]
        {
            ("rotated", new Transform2D(Mathf.Pi / 2f, new Vector2(512f, 0f)), false),
            ("mirrored-scaled", new Transform2D(new Vector2(-1.2f, 0f),
                new Vector2(.2f, .8f), new Vector2(580f, 40f)), false),
            ("lamp-only", Transform2D.Identity, true)
        })
        {
            using var fixture = new RasterFixture(tree, "FogEdge-" + name);
            fixture.Scope.Transform = transform;
            CollisionShape2D collider = fixture.AddWorldRectangle(new Rect2(220f,176f,16f,32f));
            await PhysicsFrames(tree,2);
            Vector2 actor = transform * (lampOnly ? new Vector2(-600f,192f) : Actor);
            Vector2 lamp = transform * Actor;
            Vector2 direction = lampOnly ? Vector2.Right : Vector2.Zero;
            HeadlampOcclusionFrame closed = fixture.Sampler.Capture();
            TestAssert.True(closed.CanSee(lamp,transform * new Vector2(400.5f,138.5f)) &&
                !closed.CanSee(lamp,transform * new Vector2(400.5f,150.5f)),
                $"{name} native transformed rectangle still straddles the chosen silhouette");
            fixture.Present(actor,lamp,direction,transform,closed);
            using Image shaded = await fixture.ReadPixels();
            float edge = shaded.GetPixel(400,138).R;
            float shadow = shaded.GetPixel(400,150).R;
            collider.SetDeferred(CollisionShape2D.PropertyName.Disabled,true);
            await PhysicsFrames(tree,2);
            fixture.Present(actor,lamp,direction,transform,fixture.Sampler.Capture());
            using Image open = await fixture.ReadPixels();
            float clear = open.GetPixel(400,138).R;
            GD.Print($"FOG_TRANSFORMED_EDGE name={name} edge={edge} shadow={shadow} open={clear}");
            TestAssert.True(edge > shadow + .005f && edge < clear - .025f,
                $"{name} has intermediate visible-side fade, not a hard cut or full darkness");
            AssertGray(shaded,400,150,.72f,$"{name} blocked point stays fully shadowed");
        }
        GD.Print("HEADLAMP_FOG_RASTER_TRANSFORMED_LAMP_EDGES_OK");
    }

    // Catches a missing renderer, screen-centred distance field, ignored aim, and
    // a cone which is declared on the material but never reaches the actual raster.
    private static async Task DistanceAndDirectionReachPixels(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogDistanceAndDirection");
        HeadlampOcclusionFrame empty = fixture.Sampler.Capture();
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty);
        using Image noBeam = await fixture.ReadPixels();
        AssertGray(noBeam, 128, 192, 1f, "actor's near-clear centre is actually white");
        // d=272; .28*smoothstep(0,1,(272-180)/440)=.0316048685.
        AssertGray(noBeam, 400, 192, .96839513f, "distance fade uses the actor, not screen centre");
        // d=352; .28*smoothstep(0,1,(352-180)/440)=.0949088505.
        AssertGray(noBeam, 480, 192, .90509115f, "farther pixels carry more actual black fog");

        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, empty);
        using Image rightBeam = await fixture.ReadPixels();
        // On-axis lamp distance=240; multiply the previous opacity by smoothstep(240/620).
        AssertGray(rightBeam, 400, 192, .98945903f, "right-facing lamp clears its real beam pixels");
        fixture.Present(Actor, Lamp, Vector2.Left, Transform2D.Identity, empty);
        using Image opposite = await fixture.ReadPixels();
        AssertGray(opposite, 400, 192, .96839513f, "opposite-facing cone leaves right-side fog intact");
        TestAssert.True(rightBeam.GetPixel(400, 192).R > opposite.GetPixel(400, 192).R + .012f,
            "aim changes GPU pixels, not just a shader parameter");
        TestAssert.True(fixture.Fog.FrameValid, "a rendered complete frame is accepted after pixel verification");
        GD.Print("HEADLAMP_FOG_RASTER_DISTANCE_DIRECTION_OK");
    }

    // Same live canvas and immutable native frames. Expectations are independent
    // literals, not model output or material-parameter acceptance. The .5 opacity
    // multiplier is provisional accessibility tuning, not user-approved art.
    private static async Task ReducedFogChangesPixelsWithoutChangingDistanceBeamOrNativeOcclusion(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogReducedOpacity");
        HeadlampOcclusionFrame empty = fixture.Sampler.Capture();
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty);
        using (Image normal = await fixture.ReadPixels())
            AssertGray(normal, 400, 192, .96839513f, "normal fog starts at the established distance opacity");
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty, reducedFog: true);
        using (Image reduced = await fixture.ReadPixels())
            AssertGray(reduced, 400, 192, .984197565f, "reduced fog halves actual black distance opacity");
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty, reducedFog: false);
        using (Image restored = await fixture.ReadPixels())
            AssertGray(restored, 400, 192, .96839513f, "turning reduced fog off restores pixels on the same canvas");

        foreach ((Vector2 actor, float gray) in new[]
                 {
                     (new Vector2(240f, 192f), 1f),   // d=160 remains inside clear radius 180.
                     (new Vector2(0f, 192f), .93f),   // d=400: t=.5; alpha=.28*.5*.5.
                     (new Vector2(-240f, 192f), .86f) // d=640 remains beyond outer radius 620.
                 })
        {
            fixture.Present(actor, actor, Vector2.Zero, Transform2D.Identity, empty, reducedFog: true);
            using Image distance = await fixture.ReadPixels();
            AssertGray(distance, 400, 192, gray, "reduced fog preserves clear/outer distance thresholds");
        }

        // Actor d=800 is saturated; lamp d=400. Unlike the near-beam sample,
        // normal/reduced differ by .0996, well above the unchanged .008 tolerance.
        Vector2 farActor = new(-400f, 192f), beamLamp = new(0f, 192f);
        fixture.Present(farActor, beamLamp, Vector2.Right, Transform2D.Identity, empty);
        using (Image normalBeam = await fixture.ReadPixels())
            AssertGray(normalBeam, 400, 192, .80074519f, "normal beam still uses the original 620 range");
        fixture.Present(farActor, beamLamp, Vector2.Right, Transform2D.Identity, empty, reducedFog: true);
        using (Image reducedBeam = await fixture.ReadPixels())
            AssertGray(reducedBeam, 400, 192, .90037260f, "reduced fog scales the actual beam opacity, not its range");

        CollisionShape2D collider = fixture.AddWorldRectangle(new Rect2(220f, 64f, 16f, 256f));
        await PhysicsFrames(tree, 2);
        HeadlampOcclusionFrame closed = fixture.Sampler.Capture();
        TestAssert.True(closed.IsComplete && closed.Shapes.Count == 1 &&
            !closed.CanSee(Actor, new Vector2(272f, 192f)) &&
            !closed.CanSee(Lamp, new Vector2(400f, 192f)), "reduced fixture uses a real native blocking wall");
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, closed, reducedFog: true);
        using (Image reducedWall = await fixture.ReadPixels())
        {
            AssertGray(reducedWall, 208, 192, 1f, "reduced fog keeps same-side near pixels clear");
            AssertGray(reducedWall, 272, 192, .86f, "reduced near-clear cannot bypass native wall occlusion");
            AssertGray(reducedWall, 400, 192, .86f, "reduced beam cannot bypass native wall occlusion");
        }
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, closed);
        using (Image normalWall = await fixture.ReadPixels())
            AssertGray(normalWall, 272, 192, .72f, "turning reduced fog off restores wall opacity without replacing geometry");
        collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        await PhysicsFrames(tree, 2);
        HeadlampOcclusionFrame opened = fixture.Sampler.Capture();
        TestAssert.True(opened.IsComplete && opened.Shapes.Count == 0 &&
            opened.CanSee(Actor, new Vector2(272f, 192f)) &&
            !closed.CanSee(Actor, new Vector2(272f, 192f)), "settings never mutate frozen native snapshots");
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, opened, reducedFog: true);
        using (Image openNear = await fixture.ReadPixels())
            AssertGray(openNear, 272, 192, 1f, "only a fresh real open-wall snapshot restores the near-clear pixels");
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, closed, reducedFog: true);
        using (Image oldClosed = await fixture.ReadPixels())
            AssertGray(oldClosed, 272, 192, .86f, "old closed geometry stays blocked even in reduced fog");
        GD.Print("HEADLAMP_FOG_RASTER_REDUCED_OPACITY_OK");
    }

    // Moving the lamp well ahead makes actor-origin substitution visibly wrong:
    // the correct on-axis result is .98786586; a lamp at the actor yields .74118590.
    private static async Task LampOriginIsNotActorOrigin(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogSeparateLampOrigin");
        fixture.Present(new Vector2(-160f, 192f), new Vector2(320f, 192f), Vector2.Right,
            Transform2D.Identity, fixture.Sampler.Capture());
        using Image pixels = await fixture.ReadPixels();
        // Actor distance=560, lamp distance=80, sample world point=(400,192).
        AssertGray(pixels, 400, 192, .98786586f, "beam range starts at the supplied lamp, not the actor");
        GD.Print("HEADLAMP_FOG_RASTER_LAMP_ORIGIN_OK");
    }

    // Catches ignored native/data-texture geometry, beam leakage through a wall,
    // an unconditional 180px clear circle, stale gate uploads, and mutable snapshots.
    private static async Task NativeWallBlocksNearClearAndBeamAndSnapshotsStayFrozen(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogNativeThinWall");
        CollisionShape2D collider = fixture.AddWorldRectangle(new Rect2(220f, 64f, 16f, 256f));
        await PhysicsFrames(tree, 2);
        HeadlampOcclusionFrame closed = fixture.Sampler.Capture();
        TestAssert.True(closed.IsComplete && closed.Shapes.Count == 1,
            "wall fixture supplies one real enabled native World rectangle");
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, closed);
        using (Image blocked = await fixture.ReadPixels())
        {
            AssertGray(blocked, 208, 192, 1f, "same-side near pixels remain clear");
            // (272,192) is only 144px from actor but both rays cross the native wall.
            AssertGray(blocked, 272, 192, .72f, "near-clear cannot reveal pixels behind the wall");
            AssertGray(blocked, 400, 192, .72f, "the lamp beam cannot reveal pixels behind the wall");
        }

        collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        await PhysicsFrames(tree, 2);
        HeadlampOcclusionFrame open = fixture.Sampler.Capture();
        TestAssert.True(open.IsComplete && open.Shapes.Count == 0,
            "disabled native gate is absent from a new live frame");
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, open);
        using (Image opened = await fixture.ReadPixels())
        {
            AssertGray(opened, 272, 192, 1f, "new open-gate frame restores near-clear pixels");
            AssertGray(opened, 400, 192, .98945903f, "new open-gate frame restores beam pixels");
        }
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, closed);
        using (Image oldClosed = await fixture.ReadPixels())
            AssertGray(oldClosed, 400, 192, .72f, "previous closed snapshot stays closed after live gate opens");

        collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        await PhysicsFrames(tree, 2);
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, fixture.Sampler.Capture());
        using (Image reclosed = await fixture.ReadPixels())
            AssertGray(reclosed, 400, 192, .72f, "re-enabled native gate refreshes the actual GPU shadow");
        fixture.Present(Actor, Lamp, Vector2.Right, Transform2D.Identity, open);
        using (Image oldOpen = await fixture.ReadPixels())
            AssertGray(oldOpen, 400, 192, .98945903f, "previous open snapshot stays open after live gate closes");
        GD.Print("HEADLAMP_FOG_RASTER_NATIVE_WALL_SNAPSHOTS_OK");
    }

    // Catches ignored, reversed, or screen-centred projection matrices. A real
    // Camera2D supplies the inverse; expected world points and colours are literals.
    private static async Task RealCameraTranslationAndZoomReachPixels(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogRealCameraProjection");
        var camera = new Camera2D
        {
            Name = "Camera2D", Position = new Vector2(512f, 192f),
            Zoom = new Vector2(2f, 2f), Enabled = true
        };
        fixture.Scope.AddChild(camera);
        camera.MakeCurrent();
        camera.ForceUpdateScroll();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Transform2D viewportToWorld = fixture.Scope.GetCanvasTransform().AffineInverse();
        Vector2 worldSample = viewportToWorld * new Vector2(400f, 192f);
        TestAssert.True(worldSample.DistanceTo(new Vector2(584f, 192f)) < .01f,
            "real translated 2x camera puts viewport (400,192) at world (584,192)");
        fixture.Present(Actor, Lamp, Vector2.Zero, viewportToWorld, fixture.Sampler.Capture());
        using Image pixels = await fixture.ReadPixels();
        // d=456; .28*smoothstep((456-180)/440)=.1923000451.
        AssertGray(pixels, 400, 192, .80769995f, "camera-space sample uses the supplied world inverse");
        // viewport x=32 maps to world x=400, independently matching the first fixture.
        AssertGray(pixels, 32, 192, .96839513f, "camera zoom preserves the same world's distance fade");
        GD.Print("HEADLAMP_FOG_RASTER_CAMERA_MATRIX_OK");
    }

    // Catches resources disposed by _ExitTree which are only recreated in the
    // one-time _Ready callback, leaving a re-entered ColorRect as a white cover.
    private static async Task SameCanvasInstanceRendersAfterReentryWithoutRequestReady(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogSameInstanceReentry");
        HeadlampOcclusionFrame empty = fixture.Sampler.Capture();
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty);
        using (Image before = await fixture.ReadPixels())
            AssertGray(before, 400, 192, .96839513f, "canvas renders the expected fog before re-entry");

        PrologueFogCanvas sameCanvas = fixture.Fog;
        Node layer = sameCanvas.GetParent();
        layer.RemoveChild(sameCanvas);
        // Deliberately no RequestReady: ordinary reparenting reuses the same node.
        layer.AddChild(sameCanvas);
        TestAssert.True(ReferenceEquals(sameCanvas, fixture.Fog) && sameCanvas.IsInsideTree(),
            "lifecycle fixture re-enters the same live canvas instance");
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty);
        using (Image reentered = await fixture.ReadPixels())
        {
            AssertGray(reentered, 128, 192, 1f, "re-entered canvas preserves the near-clear centre");
            AssertGray(reentered, 400, 192, .96839513f,
                "same canvas still renders black fog, not a white cover, after re-entry without RequestReady");
        }
        TestAssert.True(fixture.Fog.FrameValid,
            "re-entered canvas accepts the newly rendered frame after real pixel verification");
        GD.Print("HEADLAMP_FOG_RASTER_SAME_INSTANCE_REENTRY_OK");
    }

    // Catches transposed/dropped basis terms and dropped Y translation. The second
    // actor deliberately puts the Y=14 term on the steepest part of the fade curve.
    private static async Task NonDiagonalProjectionPreservesBothAxesAndTranslation(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogNonDiagonalProjection");
        var viewportToWorld = new Transform2D(
            new Vector2(1f, .25f), new Vector2(-.5f, 1f), new Vector2(264f, 14f));
        // x=200-64+264=400; y=50+128+14=192. No model/sampler computes the expectation.
        fixture.Present(Actor, Lamp, Vector2.Zero, viewportToWorld, fixture.Sampler.Capture());
        using (Image pixels = await fixture.ReadPixels())
            AssertGray(pixels, 200, 128, .96839513f,
                "non-diagonal viewport basis and translation map pixel (200,128) to world (400,192)");

        // At the same world sample, actor=(400,-208) gives d=400 and t=.5:
        // black alpha=.28*.5=.14. Dropping only origin.Y=14 instead gives RGB=.8733456.
        fixture.Present(new Vector2(400f, -208f), Lamp, Vector2.Zero,
            viewportToWorld, fixture.Sampler.Capture());
        using (Image yTranslation = await fixture.ReadPixels())
            AssertGray(yTranslation, 200, 128, .86f,
                "Y translation reaches pixels independently of the horizontal distance fixture");
        GD.Print("HEADLAMP_FOG_RASTER_NONDIAGONAL_MATRIX_OK");
    }

    // Catches only-row-zero field sampling, actor-region substitution, a dropped
    // final rest row, reversed field/rest order, wrong blend width and boss leakage.
    private static async Task RegionRowsResolveAtTheShadedWorldPoint(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogAuthoredFieldRows");
        Vector2 pixel = new(200f, 128f);
        foreach ((string name, Vector2 point, float gray) in new[]
                 {
                     // ControlledAbyss=(1920,400,640,3200): every edge is >=320px away.
                     ("deep-abyss", new Vector2(2240f, 1600f), .28f),
                     // SafeExit/rest=(7280,720,400,720): 200px minimum edge distance.
                     ("last-rest-interior", new Vector2(7480f, 1080f), .90f),
                     // Last rest is 16px from its left edge: smooth(16/32)=.5;
                     // boss .18 -> rest .10 gives alpha .14, white RGB .86.
                     // Pixel-centre sampling makes the physical value about .8618744.
                     ("last-rest-half-blend", new Vector2(7296f, 1080f), .86f),
                     // SealedCourtyard=(5120,720,960,720): 80px from top, not in BossHall.
                     ("court-not-boss", new Vector2(5600f, 800f), .45f)
                 })
        {
            Vector2 actor = point - new Vector2(800f, 0f);
            var inverse = new Transform2D(Vector2.Right, Vector2.Down, point - pixel);
            fixture.Present(actor, actor + new Vector2(32f, 0f), Vector2.Zero,
                inverse, fixture.Sampler.Capture());
            using Image pixels = await fixture.ReadPixels();
            AssertGray(pixels, 200, 128, gray,
                $"authored field/rest rows resolve density at shaded world point {name}={point}");
        }
        GD.Print("HEADLAMP_FOG_RASTER_REGION_ROWS_OK");
    }

    // Catches treating physical FRAGCOORD as logical canvas pixels, omitting the
    // actual SubViewport final transform, or applying the canvas stretch twice.
    private static async Task LogicalCanvasStretchPreservesWorldSamplesAtBothResolutions(SceneTree tree)
    {
        foreach ((Vector2I resolution, Vector2I pixel) in new[]
                 {
                     (new Vector2I(1280, 720), new Vector2I(400, 192)),
                     (new Vector2I(1920, 1080), new Vector2I(600, 288))
                 })
        {
            using var fixture = new RasterFixture(tree, $"FogStretch{resolution.X}",
                resolution, new Vector2I(1280, 720));
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Transform2D inverse = fixture.PixelsToWorld;
            TestAssert.True((inverse * (Vector2)pixel).DistanceTo(new Vector2(400f, 192f)) < .01f,
                $"actual {resolution} viewport final/canvas transforms preserve the world sample");
            fixture.Present(Actor, Lamp, Vector2.Zero, inverse, fixture.Sampler.Capture());
            using Image pixels = await fixture.ReadPixels();
            AssertGray(pixels, pixel.X, pixel.Y, .96839513f,
                $"physical {resolution} pixels retain the same world-point fog on a 1280x720 logical canvas");
        }
        GD.Print("HEADLAMP_FOG_RASTER_MULTIRESOLUTION_OK");
    }

    // Neither wall can shade the other's sample. Reading only the first uploaded
    // native rectangle therefore fails regardless of native owner enumeration order.
    private static async Task BothNativeRectangleRowsReachDistinctShadowPixels(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogTwoNativeRectangleRows");
        fixture.AddWorldRectangle(new Rect2(220f, 176f, 16f, 32f));
        CollisionShape2D upper = fixture.AddWorldRectangle(new Rect2(112f, 96f, 32f, 16f));
        ((Node2D)upper.GetParent()).Rotation = .25f;
        await PhysicsFrames(tree, 2);
        HeadlampOcclusionFrame frame = fixture.Sampler.Capture();
        TestAssert.True(frame.IsComplete && frame.Shapes.Count == 2,
            "two real native rectangles, including a rotated 16px-thick rectangle, are uploaded");
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, frame);
        using Image pixels = await fixture.ReadPixels();
        AssertGray(pixels, 400, 192, .72f, "right-side rectangle contributes its own native shadow");
        AssertGray(pixels, 128, 64, .72f, "separate rotated upper rectangle blocks the near-clear circle");
        AssertGray(pixels, 80, 192, 1f, "two occluder rows do not darken an unobstructed near point");
        GD.Print("HEADLAMP_FOG_RASTER_TWO_NATIVE_ROWS_OK");
    }

    // Finite matrix elements and a finite nonzero determinant do not guarantee a
    // usable viewport projection: these finite columns overflow at real corners.
    private static async Task FiniteProjectionWhichOverflowsViewportIsRejectedConservatively(SceneTree tree)
    {
        using var fixture = new RasterFixture(tree, "FogOverflowingFiniteProjection");
        HeadlampOcclusionFrame empty = fixture.Sampler.Capture();
        fixture.Present(Actor, Lamp, Vector2.Zero, Transform2D.Identity, empty);
        using (Image valid = await fixture.ReadPixels())
            AssertGray(valid, 400, 192, .96839513f, "overflow fixture first renders an ordinary valid frame");

        var overflow = new Transform2D(new Vector2(1e38f, 1f),
            new Vector2(-1e38f, 0f), Vector2.Zero);
        TestAssert.True(float.IsFinite(overflow.Determinant()) && overflow.Determinant() != 0f,
            "overflow fixture has finite entries and a finite nonzero determinant");
        fixture.Present(Actor, Lamp, Vector2.Zero, overflow, empty);
        using (Image conservative = await fixture.ReadPixels())
            AssertGray(conservative, 400, 192, .72f,
                "overflowing viewport projection uses conservative identity-space entrance fog, never NaN or white");
        TestAssert.True(!fixture.Fog.FrameValid,
            "finite matrix whose actual viewport corners overflow is rejected as an invalid frame");
        GD.Print("HEADLAMP_FOG_RASTER_INVALID_PROJECTION_OK");
    }

    private static void AssertGray(Image image, int x, int y, float expected, string message)
    {
        Color pixel = image.GetPixel(x, y);
        TestAssert.True(float.IsFinite(pixel.R) && float.IsFinite(pixel.G) && float.IsFinite(pixel.B) &&
            Mathf.Abs(pixel.R - expected) <= PixelTolerance &&
            Mathf.Abs(pixel.G - expected) <= PixelTolerance &&
            Mathf.Abs(pixel.B - expected) <= PixelTolerance,
            $"{message}; pixel=({x},{y}) expectedRGB={expected:0.00000000} actual={pixel} tolerance={PixelTolerance}");
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private sealed class RasterFixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly SubViewport _viewport;
        private readonly Vector2I _pixelSize;
        public Node2D Scope { get; }
        public PrologueFogCanvas Fog { get; }
        public HeadlampOcclusionSampler Sampler { get; }
        public Transform2D PixelsToWorld =>
            (_viewport.GetFinalTransform() * Scope.GetCanvasTransform()).AffineInverse();

        public RasterFixture(SceneTree tree, string name,
            Vector2I? physicalSize = null, Vector2I? logicalSize = null)
        {
            _tree = tree;
            _pixelSize = physicalSize ?? new Vector2I(512, 384);
            Vector2I canvasSize = logicalSize ?? _pixelSize;
            _viewport = new SubViewport
            {
                Name = name, Size = _pixelSize, World2D = new World2D(),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            if (logicalSize.HasValue)
            {
                _viewport.Size2DOverride = logicalSize.Value;
                _viewport.Size2DOverrideStretch = true;
            }
            tree.Root.AddChild(_viewport);
            var background = new CanvasLayer { Name = "WhiteBackground", Layer = -10 };
            background.AddChild(new ColorRect
            {
                Size = (Vector2)canvasSize, Color = Colors.White,
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            _viewport.AddChild(background);
            Scope = new Node2D { Name = "NativeWorld" };
            _viewport.AddChild(Scope);
            var fogLayer = new CanvasLayer { Name = "Fog", Layer = 1 };
            Fog = new PrologueFogCanvas
            {
                Name = "Canvas", Size = (Vector2)canvasSize,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            fogLayer.AddChild(Fog);
            _viewport.AddChild(fogLayer);
            Sampler = new HeadlampOcclusionSampler(Scope);
        }

        public CollisionShape2D AddWorldRectangle(Rect2 rectangle)
        {
            var body = new StaticBody2D
            {
                Name = "NativeWall", Position = rectangle.GetCenter(),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
            };
            var collider = new CollisionShape2D
            {
                Name = "Shape", Shape = new RectangleShape2D { Size = rectangle.Size }
            };
            body.AddChild(collider);
            Scope.AddChild(body);
            return collider;
        }

        public void Present(Vector2 actor, Vector2 lamp, Vector2 direction,
            Transform2D viewportToWorld, HeadlampOcclusionFrame occlusion, bool reducedFog = false) =>
            Fog.Present(actor, lamp, direction, viewportToWorld, occlusion, reducedFog);

        public async Task<Image> ReadPixels()
        {
            for (int frame = 0; frame < 2; frame++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
            Image image = _viewport.GetTexture().GetImage();
            try
            {
                TestAssert.True(image != null && !image.IsEmpty() && image.GetSize() == _pixelSize,
                    $"real GPU readback returns a nonempty {_pixelSize} viewport raster");
                return image;
            }
            catch
            {
                image?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Sampler.Dispose();
            if (!GodotObject.IsInstanceValid(_viewport)) return;
            _viewport.GetParent()?.RemoveChild(_viewport);
            _viewport.Free();
        }
    }
}
