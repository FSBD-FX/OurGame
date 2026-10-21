using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class ContinuousCanyonReadabilityTests
{
    private const string MainPath = "res://scenes/prologue/PrologueLevel.tscn";
    private static readonly PrologueSectorId[] Sectors =
    [
        PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent,
        PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent,
        PrologueSectorId.LostTemple
    ];

    // Hand-authored world-space actor/support/next-target probes. These are presentation
    // witnesses only; gameplay collision and traversal authority remain in Gameplay.
    private static readonly IReadOnlyDictionary<PrologueSectorId, Rect2[]> ProtectedWorldProbes =
        new Dictionary<PrologueSectorId, Rect2[]>
        {
            [PrologueSectorId.CanyonEntrance] =
            [new(120, 500, 80, 136), new(0, 636, 320, 40), new(400, 636, 100, 40),
                new(1190, 32, 70, 72)],
            [PrologueSectorId.CanyonDescent] =
            [new(2035, 3520, 80, 136), new(1920, 3560, 640, 40), new(2350, 3080, 190, 40),
                new(1300, 28, 120, 72)],
            [PrologueSectorId.CanyonBasin] =
            [new(1640, 3860, 80, 136), new(1280, 3996, 800, 40), new(2420, 3916, 300, 80)],
            [PrologueSectorId.CanyonAscent] =
            [new(4620, 3160, 80, 136), new(4660, 3080, 140, 40), new(4748, 2840, 36, 60)],
            [PrologueSectorId.LostTemple] =
            [new(5860, 980, 80, 136), new(5680, 1116, 400, 40), new(6080, 1116, 240, 40)]
        };

    public static void Run(bool fromBuilder)
    {
        AssertFormalForegroundSafetyContract();
        AssertDepthStagingLeavesSampleConsumersOff();
        var silhouetteIds = new HashSet<string>(StringComparer.Ordinal);
        var heightLayers = new HashSet<int>();
        var challengeKinds = new HashSet<int>();
        Color? sharedVoid = null;
        int challengeCompositions = 0;

        foreach (PrologueSectorId id in Sectors)
        {
            PrologueSector sector = fromBuilder
                ? PrologueCanyonBuildParts.Build(id)
                : GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(id))
                    .Instantiate<PrologueSector>();
            try
            {
                Node visualRoot = sector.GetNode("VisualRoot");
                foreach (string layer in RoomVisualCatalog.StableSectorVisualRoots)
                    TestAssert.True(sector.GetNodeOrNull(layer) is Node2D,
                        $"{id} retains replaceable {layer}");

                GameplayFingerprint before = Fingerprint(sector);
                AssertCoverage(sector, id, ref sharedVoid);
                AssertChallengeCompositions(sector, id, silhouetteIds, heightLayers,
                    challengeKinds, ref challengeCompositions);
                AssertForegroundSafety(sector, id);
                AssertMechanismDressingIsVisualOnly(sector, id);
                AssertEntryRiftHasNoGenericRectangleDressing(sector, id);
                AssertEntryRiftMicroSceneMarkersAndNoRegionTint(sector, id);
                AssertDepthStagingConsumerBindings(sector, id);

                var detached = new List<Node>();
                foreach (string layerName in new[] { "Background", "Midground", "Foreground" })
                {
                    Node layer = visualRoot.GetNode(layerName);
                    visualRoot.RemoveChild(layer);
                    detached.Add(layer);
                    visualRoot.AddChild(new Node2D { Name = layerName });
                }
                TestAssert.Equal(before, Fingerprint(sector),
                    $"{id} gameplay collision/material/route/telegraph/activation fingerprint survives art replacement");
                foreach (string layerName in new[] { "Background", "Midground", "Foreground" })
                {
                    Node replacement = visualRoot.GetNode(layerName);
                    visualRoot.RemoveChild(replacement);
                    replacement.Free();
                }
                foreach (Node layer in detached) layer.Free();
            }
            finally
            {
                sector.Free();
            }
        }

        TestAssert.Equal(12, challengeCompositions,
            "the twelve accepted challenge contracts each bind one real challenge composition");
        TestAssert.Equal(12, silhouetteIds.Count,
            "challenge compositions use twelve distinct rendered silhouette recipes, not palette labels");
        TestAssert.Equal(12, challengeKinds.Count,
            "composition coverage maps to the twelve accepted challenge kinds without inventing gameplay variants");
        TestAssert.True(heightLayers.SetEquals([0, 1, 2]),
            "challenge composition spans lower, middle and upper canyon height layers");
        GD.Print($"CONTINUOUS_CANYON_READABILITY_OK source={(fromBuilder ? "builder" : "packed")} " +
                 $"compositions={challengeCompositions} silhouettes={silhouetteIds.Count} heights={heightLayers.Count}");
    }

    // Break caught: lost or overbroad builder-to-renderer assignments, including a stale
    // packed leaf. Expectations select the actual named consumers and collision materials,
    // not a production profile-selection helper or a private paint/color formula.
    private static void AssertDepthStagingConsumerBindings(PrologueSector sector,
        PrologueSectorId id)
    {
        var selectedBackdrops = new HashSet<CanyonBackdropVisual>();
        string compositionName = id switch
        {
            PrologueSectorId.CanyonEntrance => "LowCeilingBridge",
            PrologueSectorId.CanyonDescent => "StaggeredRift",
            _ => null
        };
        if (compositionName != null)
        {
            selectedBackdrops.Add(sector.GetNode<CanyonBackdropVisual>(
                "VisualRoot/Midground/CanyonLandmark"));
            CanyonBackdropVisual composition = sector.GetNode<CanyonBackdropVisual>(
                $"VisualRoot/Midground/ChallengeCompositions/{compositionName}");
            TestAssert.Equal(id == PrologueSectorId.CanyonEntrance
                    ? "broken-entrance-bell" : "staggered-belfry-ribs",
                composition.CompositionId, $"{id} selects the accepted depth-staging composition");
            selectedBackdrops.Add(composition);
        }

        CanyonBackdropVisual[] backdrops = sector.FindChildren("*", "", true, false)
            .OfType<CanyonBackdropVisual>().ToArray();
        TestAssert.True(backdrops.Length >= 4, $"{id} inspects actual backdrop consumers");
        foreach (CanyonBackdropVisual backdrop in backdrops)
            TestAssert.Equal(selectedBackdrops.Contains(backdrop), backdrop.UseCanyonDepthStaging,
                $"{id}/{backdrop.Name} depth staging is bound only to selected landmark/composition consumers; " +
                "shared distant field, coverage skirt and other compositions remain off");

        CanyonPlatformVisual[] skins = sector.FindChildren("*", "", true, false)
            .OfType<CanyonPlatformVisual>().ToArray();
        TestAssert.True(skins.Length > 0, $"{id} inspects actual terrain-skin consumers");
        int selectedStoneSkins = 0;
        foreach (CanyonPlatformVisual skin in skins)
        {
            GrappleSurface2D collisionMaterial = sector.GetNode<GrappleSurface2D>(
                $"Gameplay/Collision/{skin.Name}/GrappleSurfaceProfile");
            bool expected = (id is PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent) &&
                            collisionMaterial.Material == GrappleSurfaceMaterial.GripStone;
            TestAssert.Equal(expected, skin.UseCanyonDepthStaging,
                $"{id}/{skin.Name} depth staging follows Entrance/Descent GripStone collision consumers only; " +
                $"material={collisionMaterial.Material}");
            if (expected) selectedStoneSkins++;
        }
        if (compositionName != null)
            TestAssert.True(selectedStoneSkins > 0, $"{id} exercises opted-in real GripStone skins");
    }

    // No production tree entry: inspect frozen packed leaves without running their
    // _Ready paths, loading save/settings services, or mounting the sample entry scene.
    private static void AssertDepthStagingLeavesSampleConsumersOff()
    {
        var defaultBackdrop = new CanyonBackdropVisual();
        var defaultSkin = new CanyonPlatformVisual();
        var defaultBasinSkin = new BasinPlatformVisual();
        try
        {
            TestAssert.True(!defaultBackdrop.UseCanyonDepthStaging &&
                            !defaultSkin.UseCanyonDepthStaging &&
                            !defaultBasinSkin.UseCanyonDepthStaging,
                "unbound backdrop/base/Basin consumers retain default-off depth staging");
        }
        finally
        {
            defaultBackdrop.Free();
            defaultSkin.Free();
            defaultBasinSkin.Free();
        }

        foreach (string path in new[]
                 {
                     "res://scenes/prologue/sample/legacy/CanyonEntrance.tscn",
                     "res://scenes/prologue/sample/legacy/CanyonDescent.tscn",
                     "res://scenes/prologue/sample/BasinQualitySampleSector.tscn"
                 })
        {
            Node sample = GD.Load<PackedScene>(path).Instantiate();
            try
            {
                Node[] consumers = sample.FindChildren("*", "", true, false).ToArray();
                CanyonBackdropVisual[] backdrops = consumers.OfType<CanyonBackdropVisual>().ToArray();
                CanyonPlatformVisual[] skins = consumers.OfType<CanyonPlatformVisual>().ToArray();
                TestAssert.True(skins.Length > 0, $"{path} contains actual packed skin consumers");
                if (path.Contains("/legacy/", StringComparison.Ordinal))
                    TestAssert.True(backdrops.Length > 0,
                        $"{path} contains actual legacy backdrop consumers");
                else
                {
                    TestAssert.True(skins.Any(skin => skin is BasinPlatformVisual),
                        "Basin sample exercises its inherited terrain consumer");
                    TestAssert.True(skins.Any(skin => skin.GetType() == typeof(CanyonPlatformVisual)),
                        "Basin sample exercises its base terrain consumer");
                }
                foreach (CanyonBackdropVisual backdrop in backdrops)
                    TestAssert.True(!backdrop.UseCanyonDepthStaging,
                        $"{path}/{backdrop.Name} legacy backdrop remains off");
                foreach (CanyonPlatformVisual skin in skins)
                    TestAssert.True(!skin.UseCanyonDepthStaging,
                        $"{path}/{skin.Name} legacy/sample skin remains off");
            }
            finally
            {
                sample.Free();
            }
        }
    }

    public static async Task RunMountedRuntime(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(MainPath)
            .Instantiate<PrologueLevelController>();
        level.Name = "Task9MountedReadability";
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "task15-mounted-readability");
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        Camera2D camera = player.GetNode<Camera2D>("Camera2D");
        camera.Enabled = true;
        tree.Root.AddChild(level);
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        try
        {
            await PhysicsFrames(tree, 8);
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task9-mounted-1920"))
            {
                // GameFlow applies persisted settings during startup, so select the proof
                // window only after that production path has completed.
                DisplayServer.WindowSetSize(new Vector2I(1920, 1080));
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            foreach (string path in new[] { "Input", "Motor", "Combat", "Shield" })
                player.GetNode(path).SetPhysicsProcess(false);

            Vector2 contentSize = tree.Root.GetVisibleRect().Size;
            Vector2 viewportSize = (Vector2)DisplayServer.WindowGetSize();
            bool headlessDisplay = viewportSize == Vector2.Zero;
            if (headlessDisplay) viewportSize = contentSize;
            TestAssert.True(contentSize == new Vector2(1280, 720),
                $"mounted readability preserves the authored 1280x720 canvas, actual={contentSize}");
            TestAssert.True(viewportSize == new Vector2(1280, 720) ||
                            viewportSize == new Vector2(1920, 1080),
                $"mounted readability runs at supported 1280x720 or 1920x1080 window, actual={viewportSize}");
            MountedProbe[] probes = MountedProbes();
            foreach (MountedProbe probe in probes)
            {
                player.GlobalPosition = probe.PlayerWorld;
                player.Velocity = Vector2.Zero;
                await PhysicsFrames(tree, 12);
                TestAssert.Equal(probe.SectorId, world.CurrentSector,
                    $"{probe.Label} camera probe resolves the intended mounted sector");
                TestAssert.True(world.TryGetMountedSector(probe.SectorId, out PrologueSector sector),
                    $"{probe.Label} resolves the live mounted sector graph");

                CanyonBackdropVisual skirt = sector.GetNode<CanyonBackdropVisual>(
                    "VisualRoot/Background/CanyonCoverageSkirt");
                Transform2D inverseSkirt = skirt.GetGlobalTransform().AffineInverse();
                Transform2D finalTransform = headlessDisplay
                    ? Transform2D.Identity
                    : tree.Root.GetFinalTransform();
                Transform2D inverseCanvas =
                    (finalTransform * tree.Root.CanvasTransform).AffineInverse();
                Rect2 viewportWorld = Bounds([
                    inverseCanvas * Vector2.Zero,
                    inverseCanvas * new Vector2(viewportSize.X, 0),
                    inverseCanvas * viewportSize,
                    inverseCanvas * new Vector2(0, viewportSize.Y)
                ]);
                Rect2 viewportInSkirt = Bounds([
                    inverseSkirt * viewportWorld.Position,
                    inverseSkirt * new Vector2(viewportWorld.End.X, viewportWorld.Position.Y),
                    inverseSkirt * viewportWorld.End,
                    inverseSkirt * new Vector2(viewportWorld.Position.X, viewportWorld.End.Y)
                ]);
                TestAssert.True(skirt.SectorBounds.Encloses(viewportInSkirt),
                    $"{probe.Label} live inverse-canvas footprint stays inside mounted skirt " +
                    $"viewport={viewportWorld} skirt={skirt.SectorBounds}");

                Rect2 actorScreen = PlayerScreenBounds(player, finalTransform);
                Rect2 supportScreen = CollisionScreenBounds(sector.GetNode<CollisionShape2D>(
                    $"Gameplay/Collision/{probe.Support}/Collision"), finalTransform);
                Rect2 targetScreen = CollisionScreenBounds(sector.GetNode<CollisionShape2D>(
                    $"Gameplay/Collision/{probe.NextTarget}/Collision"), finalTransform);
                Rect2 screen = new(Vector2.Zero, viewportSize);
                TestAssert.True(screen.Intersects(actorScreen) && screen.Intersects(supportScreen) &&
                                screen.Intersects(targetScreen),
                    $"{probe.Label} live camera keeps actor, support and next target inside the normal viewport");

                Node foreground = sector.GetNode("VisualRoot/Foreground/DepthOccluders");
                foreach (Polygon2D polygon in foreground.GetChildren().OfType<Polygon2D>())
                {
                    Rect2 occluderScreen = PolygonScreenBounds(polygon, finalTransform);
                    TestAssert.True(!occluderScreen.Intersects(actorScreen) &&
                                    !occluderScreen.Intersects(supportScreen) &&
                                    !occluderScreen.Intersects(targetScreen),
                        $"{probe.Label}/{polygon.Name} live foreground misses actor/support/next target");
                }

                GameplayFingerprint before = Fingerprint(sector);
                sector.GetNode<CanvasItem>("VisualRoot/Background").Visible = false;
                sector.GetNode<CanvasItem>("VisualRoot/Midground").Visible = false;
                sector.GetNode<CanvasItem>("VisualRoot/Foreground").Visible = false;
                // The art-removal contract is structural and transform-based. A process frame is
                // sufficient here and, unlike FramePostDraw, also advances under the supported
                // headless test renderer where no drawable swapchain exists.
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.Equal(before, Fingerprint(sector),
                    $"{probe.Label} live art-removal frame preserves gameplay authority");
                TestAssert.True(screen.Intersects(PlayerScreenBounds(player, finalTransform)) &&
                                screen.Intersects(CollisionScreenBounds(sector.GetNode<CollisionShape2D>(
                                    $"Gameplay/Collision/{probe.Support}/Collision"), finalTransform)) &&
                                screen.Intersects(CollisionScreenBounds(sector.GetNode<CollisionShape2D>(
                                    $"Gameplay/Collision/{probe.NextTarget}/Collision"), finalTransform)),
                    $"{probe.Label} actor/support/next target remain readable with decoration hidden");
                sector.GetNode<CanvasItem>("VisualRoot/Background").Visible = true;
                sector.GetNode<CanvasItem>("VisualRoot/Midground").Visible = true;
                sector.GetNode<CanvasItem>("VisualRoot/Foreground").Visible = true;
            }
            await AssertFirstSeamForegroundClear(tree, world, player, headlessDisplay);
            await AssertLiveEncounterSurvivesArtReplacement(tree, world, player);
            await PhysicsFrames(tree, 18);
            TestAssert.True(world.RetiredSectorCount == 0,
                "mounted readability drains retired sectors after the final seam probe");
            GD.Print($"TASK9_MOUNTED_READABILITY_OK window={viewportSize} canvas={contentSize} " +
                     $"headless={headlessDisplay} " +
                     $"probes={probes.Length} " +
                     $"current={world.CurrentSector} mounted={string.Join(',', world.MountedSectorIds)} retired=0");
        }
        finally
        {
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            for (int index = 0; index < 4; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static MountedProbe[] MountedProbes() =>
    [
        new(PrologueSectorId.CanyonEntrance, new Vector2(160, 600), "StartFloor",
            "BridgeFarBank", "entrance-lower-edge"),
        new(PrologueSectorId.CanyonDescent, new Vector2(2400, 3044), "AbyssExit3",
            "AbyssBrakeRight4", "descent-abyss-seam"),
        new(PrologueSectorId.CanyonBasin, new Vector2(1680, 3960), "BasinCampFloor",
            "GearEntry", "basin-camp-landmark"),
        new(PrologueSectorId.CanyonAscent, new Vector2(4700, 3424), "LiftBackLanding",
            "LiftBackLandingGrip", "ascent-lift-junction"),
        new(PrologueSectorId.LostTemple, new Vector2(5920, 1080), "CheckpointFloor",
            "ArenaFloor", "temple-facade")
    ];

    // Break caught: a generic upper-corner foreground polygon from either resident sector
    // overlaps the real bridge-to-rift launch/arrival actor envelope at the first seam.
    private static async Task AssertFirstSeamForegroundClear(SceneTree tree,
        SeamlessPrologueWorld world, CharacterBody2D player, bool headlessDisplay)
    {
        Transform2D finalTransform = headlessDisplay
            ? Transform2D.Identity
            : tree.Root.GetFinalTransform();
        foreach ((Vector2 position, string label) in new[]
                 {
                     (new Vector2(1220, 64), "entrance-launch"),
                     (new Vector2(1360, 64), "descent-arrival")
                 })
        {
            player.GlobalPosition = position;
            player.Velocity = Vector2.Zero;
            await PhysicsFrames(tree, 12);
            Rect2 actorScreen = PlayerScreenBounds(player, finalTransform);
            foreach (PrologueSectorId resident in new[]
                     {
                         PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent
                     })
            {
                TestAssert.True(world.TryGetMountedSector(resident, out PrologueSector sector),
                    $"{label} keeps {resident} resident for two-sided seam foreground proof");
                Node foreground = sector.GetNode("VisualRoot/Foreground/DepthOccluders");
                foreach (Polygon2D polygon in foreground.GetChildren().OfType<Polygon2D>())
                    TestAssert.True(!PolygonScreenBounds(polygon, finalTransform)
                            .Intersects(actorScreen),
                        $"{label}/{resident}/{polygon.Name} foreground misses the live actor envelope");
            }
        }
        GD.Print("TASK9_FIX1_FIRST_SEAM_FOREGROUND_OK probes=2 residents=Entrance,Descent");
    }

    // Characterization witness required by the art-replacement contract: with the decorative
    // layers actually removed and replaced, a production encounter still activates and its
    // gameplay-readable piston telegraph stays bound through the real mechanism path.
    private static async Task AssertLiveEncounterSurvivesArtReplacement(SceneTree tree,
        SeamlessPrologueWorld world, CharacterBody2D player)
    {
        player.GlobalPosition = new Vector2(1680, 3960);
        player.Velocity = Vector2.Zero;
        await PhysicsFrames(tree, 12);
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin,
                out PrologueSector sector),
            "art-replacement encounter witness mounts the production Basin sector");

        Node visualRoot = sector.GetNode("VisualRoot");
        Node gameplay = sector.GetNode("Gameplay");
        Node gameplayReadable = sector.GetNode("VisualRoot/GameplayReadable");
        CanyonEncounterGroup encounter = sector.GetNode<CanyonEncounterGroup>(
            "Gameplay/Encounters/GearPistonsEncounter");
        CanyonGearPiston piston = sector.GetNode<CanyonGearPiston>(
            "Gameplay/Mechanisms/GearPiston");
        CanvasItem telegraph = piston.GetNodeOrNull<CanvasItem>(piston.TelegraphPath);
        TestAssert.True(!encounter.IsActive && encounter.LiveActorCount == 0 && telegraph != null,
            "bounded live witness starts before Gear Pistons activation with its telegraph bound");

        var detached = new List<Node>();
        try
        {
            foreach (string layerName in new[] { "Background", "Midground", "Foreground" })
            {
                Node layer = visualRoot.GetNode(layerName);
                visualRoot.RemoveChild(layer);
                detached.Add(layer);
                visualRoot.AddChild(new Node2D { Name = layerName });
            }
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(ReferenceEquals(gameplay, sector.GetNode("Gameplay")) &&
                            ReferenceEquals(gameplayReadable,
                                sector.GetNode("VisualRoot/GameplayReadable")) &&
                            ReferenceEquals(telegraph,
                                piston.GetNodeOrNull<CanvasItem>(piston.TelegraphPath)),
                "decorative replacement preserves Gameplay, GameplayReadable and live telegraph binding");

            // Stay inside the production activation bounds but away from the authored spawn;
            // this witnesses activation without turning the presentation test into a combat hit.
            player.GlobalPosition = new Vector2(2460, 4050);
            player.Velocity = Vector2.Zero;
            for (int frame = 0; frame < 12 && !encounter.IsActive; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            Node actors = gameplay.GetNode("Actors");
            TestAssert.True(encounter.IsActive && encounter.LiveActorCount == 1 &&
                            actors.GetChildren().Count(actor => actor is IEncounterActor) == 1,
                "real Gear Pistons activation spawns its authored encounter actor after art replacement");
            TestAssert.True(ReferenceEquals(telegraph,
                                piston.GetNodeOrNull<CanvasItem>(piston.TelegraphPath)) &&
                            telegraph.IsInsideTree(),
                "encounter activation leaves the gameplay-readable piston telegraph live and bound");
            GD.Print("TASK9_FIX1_ART_REPLACEMENT_ENCOUNTER_OK " +
                     $"id={encounter.EncounterId} active={encounter.IsActive} " +
                     $"actors={encounter.LiveActorCount} telegraph={telegraph.GetPath()}");

            player.GlobalPosition = new Vector2(1680, 3960);
            player.Velocity = Vector2.Zero;
            for (int frame = 0; frame < 12 && encounter.IsActive; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!encounter.IsActive && encounter.LiveActorCount == 0,
                "live art-replacement witness retires its actor before fixture teardown");
        }
        finally
        {
            foreach (Node layer in detached) layer.Free();
        }
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static Rect2 PlayerScreenBounds(CharacterBody2D player, Transform2D finalTransform)
    {
        CollisionShape2D collision = player.GetNode<CollisionShape2D>("BodyCollider");
        var capsule = (CapsuleShape2D)collision.Shape;
        return TransformedBounds(finalTransform * collision.GetGlobalTransformWithCanvas(),
            new Vector2(capsule.Radius, capsule.Height * .5f));
    }

    private static Rect2 CollisionScreenBounds(CollisionShape2D collision,
        Transform2D finalTransform)
    {
        TestAssert.True(collision.Shape is RectangleShape2D,
            $"{collision.Name} readability target uses an inspectable rectangle");
        return TransformedBounds(finalTransform * collision.GetGlobalTransformWithCanvas(),
            ((RectangleShape2D)collision.Shape).Size * .5f);
    }

    private static Rect2 PolygonScreenBounds(Polygon2D polygon, Transform2D finalTransform)
    {
        Transform2D transform = finalTransform * polygon.GetGlobalTransformWithCanvas();
        return Bounds(polygon.Polygon.Select(point => transform * point).ToArray());
    }

    private static Rect2 TransformedBounds(Transform2D transform, Vector2 halfExtents) => Bounds([
        transform * -halfExtents, transform * new Vector2(halfExtents.X, -halfExtents.Y),
        transform * halfExtents, transform * new Vector2(-halfExtents.X, halfExtents.Y)
    ]);

    private static Rect2 Bounds(Vector2[] points)
    {
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vector2 point in points)
        {
            minX = Mathf.Min(minX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxX = Mathf.Max(maxX, point.X);
            maxY = Mathf.Max(maxY, point.Y);
        }
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    private static void AssertFormalForegroundSafetyContract()
    {
        System.Reflection.MethodInfo method = typeof(RoomFormalVisual).GetMethod(
            "ForegroundCoverageAvoidsProtectedRegions",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        TestAssert.True(method != null,
            "formal room art exposes the same actor/support/target foreground safety contract as canyon art");
        Rect2[] protectedRegions = [new Rect2(80, 80, 80, 120), new Rect2(40, 200, 220, 28)];
        bool overlapping = (bool)method.Invoke(null,
            [new Rect2(100, 100, 50, 80), protectedRegions]);
        bool separate = (bool)method.Invoke(null,
            [new Rect2(300, 20, 40, 90), protectedRegions]);
        TestAssert.True(!overlapping && separate,
            "foreground coverage rejects actor/support overlap and accepts a separate edge silhouette");
    }

    private static void AssertCoverage(PrologueSector sector, PrologueSectorId id,
        ref Color? sharedVoid)
    {
        Polygon2D canyonVoid = sector.GetNode<Polygon2D>("VisualRoot/Background/CanyonVoid");
        TestAssert.True(Mathf.IsEqualApprox(canyonVoid.Color.A, 1f),
            $"{id} opaque base remains an intentional skirt color");
        if (sharedVoid.HasValue)
            TestAssert.Equal(sharedVoid.Value, canyonVoid.Color,
                $"{id} uses the common opaque skirt base instead of a rectangular sector seam");
        else
            sharedVoid = canyonVoid.Color;

        var skirt = sector.GetNodeOrNull<CanyonBackdropVisual>(
            "VisualRoot/Background/CanyonCoverageSkirt");
        TestAssert.True(skirt != null, $"{id} has a replaceable low-Z coverage skirt");
        TestAssert.True(!skirt.ZAsRelative && skirt.ZIndex <= -80,
            $"{id} coverage skirt stays below all authored scenery regardless of tree order");
        TestAssert.True(skirt.Get("CoverageLayer").AsBool(),
            $"{id} coverage skirt selects the scenery-only opaque coverage renderer");

        Vector2 sectorOffset = CanyonWorldLayout.SectorBounds[id].Position;
        Rect2 coverage = new(skirt.SectorBounds.Position + sectorOffset,
            skirt.SectorBounds.Size);
        foreach ((Vector2 center, Vector2 viewport, Vector2 zoom, string label) in CoverageProbes())
        {
            // Inverse-canvas result for an unrotated Camera2D: the visible world footprint is
            // viewport / abs(zoom), centered on the real screen center (including focus bias).
            Vector2 worldSize = viewport / zoom.Abs();
            Rect2 inverseCanvasFootprint = new(center - worldSize * .5f, worldSize);
            TestAssert.True(coverage.Encloses(inverseCanvasFootprint),
                $"{id} skirt contains {label} inverse-canvas viewport {inverseCanvasFootprint}");
        }
    }

    private static IEnumerable<(Vector2 Center, Vector2 Viewport, Vector2 Zoom, string Label)>
        CoverageProbes()
    {
        yield return (new Vector2(560, 650), new Vector2(1280, 720), Vector2.One,
            "1280x720 entrance lower edge");
        yield return (new Vector2(2340, 1580), new Vector2(1920, 1080), Vector2.One,
            "1920x1080 descent/right seam");
        yield return (new Vector2(2240, 3820), new Vector2(1920, 1080), Vector2.One,
            "1920x1080 basin upper edge");
        yield return (new Vector2(4740, 2520), new Vector2(1280, 720), Vector2.One,
            "1280x720 ascent side focus");
        yield return (new Vector2(6540, 900), new Vector2(1920, 1080), Vector2.One,
            "1920x1080 pre-boss upper band");
    }

    private static void AssertChallengeCompositions(PrologueSector sector, PrologueSectorId id,
        ISet<string> silhouetteIds, ISet<int> heightLayers, ISet<int> challengeKinds,
        ref int total)
    {
        Node compositions = sector.GetNode("VisualRoot/Midground/ChallengeCompositions");
        foreach (Node child in compositions.GetChildren())
        {
            TestAssert.True(child is CanyonBackdropVisual,
                $"{id}/{child.Name} binds its silhouette ID to an actual rendered composition");
            TestAssert.Equal("challenge", child.Get("VisualRole").AsString(),
                $"{id}/{child.Name} is a rendered challenge landmark, not a backdrop palette");
            string silhouette = child.Get("CompositionId").AsString();
            int heightLayer = child.GetMeta("height_layer").AsInt32();
            int challengeKind = child.GetMeta("challenge_kind").AsInt32();
            CanyonRouteNodeDefinition expected = CanyonWorldLayout.Nodes.Single(node =>
                node.ChallengeKind.HasValue && (int)node.ChallengeKind.Value == challengeKind);
            Rect2 compositionLocal = ((CanyonBackdropVisual)child).CompositionBounds;
            Rect2 compositionWorld = new(compositionLocal.Position +
                CanyonWorldLayout.SectorBounds[id].Position, compositionLocal.Size);
            TestAssert.Equal(expected.WorldBounds, compositionWorld,
                $"{id}/{child.Name} renders at the accepted challenge world bounds after mounting");
            TestAssert.True(!string.IsNullOrWhiteSpace(silhouette) &&
                            !silhouette.Contains("palette", StringComparison.OrdinalIgnoreCase),
                $"{id}/{child.Name} exposes a non-palette silhouette recipe");
            TestAssert.True(silhouetteIds.Add(silhouette),
                $"{id}/{child.Name} silhouette recipe '{silhouette}' is distinct");
            TestAssert.True(heightLayer is >= 0 and <= 2,
                $"{id}/{child.Name} declares lower/middle/upper height layer");
            TestAssert.True(challengeKinds.Add(challengeKind),
                $"{id}/{child.Name} binds exactly one accepted challenge kind");
            heightLayers.Add(heightLayer);
            total++;
        }
    }

    private static void AssertForegroundSafety(PrologueSector sector, PrologueSectorId id)
    {
        Node occluders = sector.GetNode("VisualRoot/Foreground/DepthOccluders");
        TestAssert.True(occluders.GetChildCount() > 0,
            $"{id} has a replaceable near-depth foreground layer");
        foreach (Node child in occluders.GetChildren())
        {
            TestAssert.True(child is Polygon2D,
                $"{id}/{child.Name} foreground coverage is an inspectable authored polygon");
            var polygon = (Polygon2D)child;
            Rect2 localBounds = PolygonBounds(polygon);
            Rect2 localSector = new(Vector2.Zero, CanyonWorldLayout.SectorBounds[id].Size);
            TestAssert.True(localSector.Encloses(localBounds),
                $"{id}/{child.Name} foreground geometry is authored in sector-local coordinates");
            Rect2 worldBounds = new(localBounds.Position +
                CanyonWorldLayout.SectorBounds[id].Position, localBounds.Size);
            foreach (Rect2 protectedProbe in ProtectedWorldProbes[id])
                TestAssert.True(!worldBounds.Intersects(protectedProbe),
                    $"{id}/{child.Name} foreground must not cover actor/support/next-target probe {protectedProbe}");
        }
    }

    private static void AssertMechanismDressingIsVisualOnly(PrologueSector sector,
        PrologueSectorId id)
    {
        if (id == PrologueSectorId.CanyonAscent)
        {
            Polygon2D closed = sector.GetNode<Polygon2D>(
                "VisualRoot/GameplayReadable/MechanismTelegraphs/UpperBasinShortcutClosed");
            TestAssert.True(closed.GetChildCount() >= 3,
                "closed shortcut keeps its canonical root while adding readable shutter dressing");
            AssertVisualOnly(closed);
        }
        if (id == PrologueSectorId.CanyonBasin)
        {
            foreach (string phase in new[] { "SteamJetTelegraph", "SteamJetActive" })
            {
                Polygon2D steam = sector.GetNode<Polygon2D>(
                    $"VisualRoot/GameplayReadable/MechanismTelegraphs/{phase}");
                TestAssert.Equal(1, steam.ZIndex,
                    $"{phase} renders above floor skin but below actors");
                TestAssert.True(steam.GetChildCount() > 0,
                    $"{phase} has visual-only motion-readable internal shaping");
                AssertVisualOnly(steam);
            }
        }
    }

    private static void AssertEntryRiftHasNoGenericRectangleDressing(PrologueSector sector,
        PrologueSectorId id)
    {
        if (id is not (PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent)) return;

        // Reintroducing the legacy narrow, sector-height rectangle dressing must fail even
        // when its nodes are renamed. This tests emitted geometry, not the authored art's beauty.
        float sectorHeight = CanyonWorldLayout.SectorBounds[id].Size.Y;
        foreach (Polygon2D polygon in sector.GetNode("VisualRoot/Midground")
                     .GetChildren().OfType<Polygon2D>())
        {
            if (polygon.Polygon.Length != 4) continue;
            Rect2 bounds = PolygonBounds(polygon);
            bool rectangle = polygon.Polygon.Select(point => polygon.Transform * point).All(point =>
                (Mathf.IsEqualApprox(point.X, bounds.Position.X) ||
                 Mathf.IsEqualApprox(point.X, bounds.End.X)) &&
                (Mathf.IsEqualApprox(point.Y, bounds.Position.Y) ||
                 Mathf.IsEqualApprox(point.Y, bounds.End.Y)));
            bool narrowSectorRib = rectangle && bounds.Size.X > 0f && bounds.Size.X <= 48f &&
                                   bounds.Size.Y >= sectorHeight * .30f &&
                                   bounds.Size.Y >= bounds.Size.X * 8f;
            TestAssert.True(!narrowSectorRib,
                $"{id}/{polygon.Name} must not repeat narrow sector-height rectangular midground dressing: {bounds}");
        }
    }

    private static void AssertEntryRiftMicroSceneMarkersAndNoRegionTint(PrologueSector sector,
        PrologueSectorId id)
    {
        if (id is not (PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent)) return;

        // Hand-checked local contracts, independent of the catalog/builder. An early return
        // in AddMicroSceneMarkers must not remove the guidance markers with the tint.
        (string Name, Rect2 Bounds, bool Optional, CanyonRouteNodeId Route,
            CanyonChallengeKind Challenge)[] expected = id == PrologueSectorId.CanyonEntrance
            ? [("LowCeilingBridge", new(0, 0, 1280, 720), false,
                CanyonRouteNodeId.LowCeilingBridge, CanyonChallengeKind.LowCeilingBridge)]
            : [
                ("StaggeredRift", new(0, 0, 1280, 400), false,
                    CanyonRouteNodeId.StaggeredRift, CanyonChallengeKind.StaggeredRift),
                ("ControlledAbyss", new(640, 400, 640, 3200), false,
                    CanyonRouteNodeId.ControlledAbyss, CanyonChallengeKind.ControlledAbyss),
                ("ArchiveSideCave", new(0, 400, 640, 1400), true,
                    CanyonRouteNodeId.ArchiveSideCave, CanyonChallengeKind.ArchiveSideCave),
                ("FrozenDetour", new(0, 1800, 640, 1560), true,
                    CanyonRouteNodeId.FrozenDetour, CanyonChallengeKind.FrozenDetour)
            ];
        Node markers = sector.GetNodeOrNull("Gameplay/MicroScenes");
        TestAssert.True(markers != null, $"{id} retains authoritative MicroScenes markers");
        TestAssert.Equal(expected.Length, markers.GetChildCount(),
            $"{id} retains every authoritative micro-scene marker without duplicates");
        foreach (var contract in expected)
        {
            Marker2D marker = markers.GetNodeOrNull<Marker2D>(contract.Name);
            TestAssert.True(marker != null, $"{id}/{contract.Name} retains its real Marker2D");
            TestAssert.Equal(contract.Bounds.GetCenter(), marker.Position,
                $"{id}/{contract.Name} guidance target stays at its authored local center");
            foreach (string key in new[] { "local_bounds", "optional_subarea", "route_node_id", "challenge_kind" })
                TestAssert.True(marker.HasMeta(key), $"{id}/{contract.Name} retains {key} metadata");
            TestAssert.Equal(contract.Bounds, marker.GetMeta("local_bounds").AsRect2(),
                $"{id}/{contract.Name} retains local bounds");
            TestAssert.Equal(contract.Optional, marker.GetMeta("optional_subarea").AsBool(),
                $"{id}/{contract.Name} retains optional-subarea authority");
            TestAssert.Equal((int)contract.Route, marker.GetMeta("route_node_id").AsInt32(),
                $"{id}/{contract.Name} retains route identity");
            TestAssert.Equal((int)contract.Challenge, marker.GetMeta("challenge_kind").AsInt32(),
                $"{id}/{contract.Name} retains challenge identity");
        }

        Node readable = sector.GetNode("VisualRoot/GameplayReadable");
        TestAssert.True(readable.GetNodeOrNull("MicroSceneReadability") is Node2D,
            $"{id} retains its replaceable micro-scene readability container");
        // A renamed/reparented old wash still fails: inspect all real emitted descendants,
        // including parent transforms/visibility, rather than selecting tint node names.
        foreach (Polygon2D polygon in readable.FindChildren("*", "Polygon2D", true, false)
                     .Cast<Polygon2D>())
        {
            if (polygon.Polygon.Length != 4) continue;
            Transform2D transform = Transform2D.Identity;
            float alpha = polygon.Color.A * polygon.SelfModulate.A;
            bool visible = true;
            for (Node ancestor = polygon; ancestor != sector; ancestor = ancestor.GetParent())
            {
                if (ancestor is Node2D node2D) transform = node2D.Transform * transform;
                if (ancestor is CanvasItem canvas)
                {
                    visible &= canvas.Visible;
                    alpha *= canvas.Modulate.A;
                }
            }
            if (!visible || alpha <= 0f) continue;
            Vector2[] points = polygon.Polygon.Select(point => transform * point).ToArray();
            Rect2 bounds = Bounds(points);
            bool rectangle = bounds.Size.X > 0f && bounds.Size.Y > 0f &&
                points.Distinct().Count() == 4 && points.All(point =>
                    (Mathf.IsEqualApprox(point.X, bounds.Position.X) ||
                     Mathf.IsEqualApprox(point.X, bounds.End.X)) &&
                    (Mathf.IsEqualApprox(point.Y, bounds.Position.Y) ||
                     Mathf.IsEqualApprox(point.Y, bounds.End.Y)));
            // Allow the old 4px inset and small variants, but not smaller real surfaces or
            // telegraphs. Reducing alpha cannot hide an otherwise visible region wash.
            bool wholeMicroBounds = expected.Any(contract =>
                Mathf.Abs(bounds.Position.X - contract.Bounds.Position.X) <= 8f &&
                Mathf.Abs(bounds.Position.Y - contract.Bounds.Position.Y) <= 8f &&
                Mathf.Abs(bounds.End.X - contract.Bounds.End.X) <= 8f &&
                Mathf.Abs(bounds.End.Y - contract.Bounds.End.Y) <= 8f);
            TestAssert.True(!(rectangle && wholeMicroBounds),
                $"{id}/{polygon.Name} must not draw a visible whole-micro-bounds rectangular region tint " +
                $"in GameplayReadable: bounds={bounds}, alpha={alpha}");
        }
    }

    private static GameplayFingerprint Fingerprint(PrologueSector sector)
    {
        Node gameplay = sector.GetNode("Gameplay");
        int collisions = gameplay.FindChildren("*", "CollisionShape2D", true, false).Count;
        int routeMarkers = gameplay.GetNode("RouteMarkers").GetChildCount();
        int encounters = gameplay.GetNodeOrNull("Encounters")?.GetChildCount() ?? 0;
        int mechanisms = gameplay.GetNodeOrNull("Mechanisms")?.GetChildCount() ?? 0;
        string materials = string.Join(",", gameplay.FindChildren("*", "GrappleSurface2D", true, false)
            .Cast<GrappleSurface2D>().Select(surface => ((int)surface.Material).ToString())
            .OrderBy(value => value, StringComparer.Ordinal));
        int readableTelegraphs = sector.GetNode("VisualRoot/GameplayReadable")
            .FindChildren("*", "Polygon2D", true, false).Count;
        return new GameplayFingerprint(collisions, routeMarkers, encounters, mechanisms,
            readableTelegraphs, materials);
    }

    private static Rect2 PolygonBounds(Polygon2D polygon)
    {
        TestAssert.True(polygon.Polygon.Length >= 3,
            $"{polygon.Name} foreground polygon has visible geometry");
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vector2 point in polygon.Polygon)
        {
            Vector2 world = polygon.Transform * point;
            minX = Mathf.Min(minX, world.X);
            minY = Mathf.Min(minY, world.Y);
            maxX = Mathf.Max(maxX, world.X);
            maxY = Mathf.Max(maxY, world.Y);
        }
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    private static void AssertVisualOnly(Node node)
    {
        TestAssert.True(node is not CollisionObject2D && node is not CollisionShape2D &&
                        node is not GrappleSurface2D,
            $"{node.Name} mechanism dressing owns no gameplay authority");
        foreach (Node child in node.GetChildren()) AssertVisualOnly(child);
    }

    private sealed record GameplayFingerprint(int Collisions, int RouteMarkers,
        int Encounters, int Mechanisms, int ReadableTelegraphs, string Materials);

    private sealed record MountedProbe(PrologueSectorId SectorId, Vector2 PlayerWorld,
        string Support, string NextTarget, string Label);
}
