using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class BasinAscentCameraRuntimeTests
{
    public static Task Run(SceneTree tree) => RunCore(tree, null);

    // Natural-GC diagnostic only: the same three mounted camera fixtures,
    // changing just this Level load's cache mode. Runtime enemy loads stay normal.
    public static async Task RunCacheDiagnostic(SceneTree tree, ResourceLoader.CacheMode mode)
    {
        TestAssert.True(mode is ResourceLoader.CacheMode.Reuse or ResourceLoader.CacheMode.Ignore or ResourceLoader.CacheMode.IgnoreDeep,
            "cache diagnostic permits only the three declared comparison modes");
        bool prior = PrologueProductionMode.AllowDiagnosticFallbacks;
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            await RunCore(tree, mode);
            GD.Print($"LEVEL_CACHE_BEHAVIOR_DONE mode={mode} cycles=3 playing=3 released=3 production_load_changed=false behavior_accepted=false");
        }
        finally { PrologueProductionMode.AllowDiagnosticFallbacks = prior; }
    }

    private static async Task RunCore(SceneTree tree, ResourceLoader.CacheMode? diagnosticCacheMode)
    {
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        int sequence = 0;
        void Observe(int cycle, PrologueSectorId sector, bool high, string stage, ulong owner)
        {
            if (!diagnosticCacheMode.HasValue)
                GD.Print($"CAMERA_FIXTURE_BOUNDARY sequence={++sequence} cycle={cycle} sector={sector} " +
                    $"high={high} stage={stage} root={owner} engine_ms={Time.GetTicksMsec()} " +
                    $"physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()}");
        }
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            await CheckPosition(tree, PrologueSectorId.CanyonBasin, new Vector2(3915, 3759),
                new Vector2(3915, 3760), "ShortcutStep3796", new Rect2(3840, 3796, 150, 32), false,
                "canyon.v2.resident.steam-valves.01", diagnosticCacheMode, 0, Observe);
            await CheckPosition(tree, PrologueSectorId.CanyonAscent, new Vector2(3915, 3547),
                new Vector2(3915, 3548), "LiftEntryFloor", new Rect2(3840, 3584, 160, 16), false,
                null, diagnosticCacheMode, 1, Observe);
            // x4780 physically overlaps the new split-level resident. Keep the
            // same high seam/support, but place this stationary camera fixture
            // at x4870: full capsule inside, no physical actor overlap.
            await CheckPosition(tree, PrologueSectorId.CanyonAscent, new Vector2(4870, 1283),
                new Vector2(4870, 1284), "AscentFinalCatch", new Rect2(4660, 1320, 240, 40), true,
                "canyon.v2.resident.split-level.01", diagnosticCacheMode, 2, Observe);
            GD.Print("BASIN_ASCENT_CAMERA_RUNTIME_OK");
        }
        finally
        {
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
        }
    }

    // Break caught: V1 sector limits, lost native floor contact or a world-space focus assigned
    // as player-local camera Position can clip a grounded capsule in V2's y=3600 seam holds.
    private static async Task CheckPosition(SceneTree tree, PrologueSectorId sector, Vector2 spawn,
        Vector2 settled, string supportName, Rect2 supportBounds, bool aboveSharedBand,
        string deferredResident = null, ResourceLoader.CacheMode? diagnosticCacheMode = null, int cycle = 0,
        Action<int, PrologueSectorId, bool, string, ulong> observe = null)
    {
        PrologueLevelController level = null;
        int recoveries = 0;
        ulong ownedId = 0;
        Exception primary = null;
        void Boundary(string stage, ulong owner = 0)
        {
            if (diagnosticCacheMode is { } mode)
                GD.Print($"LEVEL_CACHE_BOUNDARY mode={mode} cycle={cycle} sector={sector} stage={stage} root={owner}");
        }
        void Recovered(string reason) => recoveries++;
        void Observe(string stage, ulong owner = 0) => observe?.Invoke(cycle, sector, aboveSharedBand, stage, owner);
        try
        {
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("check-enter:" + sector);
#endif
            Boundary("load-enter");
            level = CreateLevelFixture(sector, spawn, diagnosticCacheMode, () => Boundary("loaded"),
                (stage, owner) => Observe(stage, owner));
            ownedId = level.GetInstanceId();
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("factory-returned", level);
#endif
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            level.RecoveryCompleted += Recovered;
            ulong playerId = player.GetInstanceId();
            ulong worldId = world.GetInstanceId();
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("mount-enter", level);
#endif
            Observe("mount-enter", ownedId);
            tree.Root.AddChild(level);
            Observe("mount-return", ownedId);
            Boundary("mounted", ownedId);
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("mount-return", level);
#endif
            await Frames(tree, 8);
            Observe("eight-frames-return", ownedId);
            await WaitForPlaying(tree, level);
            Observe("playing-return", ownedId);
            Boundary("playing", ownedId);
            TestAssert.Equal(50, level.ResidentLayout.Count, "camera preparation keeps all50 authored identities");
            if (deferredResident != null)
                TestAssert.True(level.Residents.Snapshot(deferredResident) is { Alive: true, Attached: false },
                    "the player's protected resident stays alive and uncommitted, not deleted or spawned nearby");
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("original-eight-frames-returned", level);
#endif
            var camera = player.GetNode<Camera2D>("Camera2D");
            var collider = player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(collider.Shape is CapsuleShape2D, "fixture projects the actual player capsule, not a center point");
            StaticBody2D support = ResolveAuthoredSupport(world, sector, supportName, supportBounds);
            Vector2 initialOffset = camera.Offset;
            float minMargin = float.PositiveInfinity;
            for (int sample = 0; sample < 12; sample++)
            {
                await Frames(tree, 1);
                TestAssert.Equal(sector, world.CurrentSector, "camera protection must not advance or reverse the world sector authority");
                TestAssert.Equal(sector, level.CurrentSector!.Value, "level location agrees with the unchanged world sector");
                TestAssert.True(world.MountedSectorIds.Contains(PrologueSectorId.CanyonBasin) &&
                    world.MountedSectorIds.Contains(PrologueSectorId.CanyonAscent) && world.MountedSectorIds.Count <= 3,
                    "both real adjacent sectors remain mounted within the existing stream budget");
                TestAssert.Equal(playerId, level.Player.GetInstanceId(), "camera adjustment preserves the persistent player instance");
                TestAssert.Equal(worldId, level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").GetInstanceId(),
                    "camera adjustment preserves the persistent seamless world instance");
                TestAssert.Equal(0, recoveries, "camera visibility cannot be obtained through recovery relocation");
                float margin = AssertV2GroundedSample(player, world, support, sector, settled,
                    aboveSharedBand, initialOffset);
                minMargin = Math.Min(minMargin, margin);
                GD.Print($"BASIN_ASCENT_CAMERA_SAMPLE sector={sector} high={aboveSharedBand} sample={sample} " +
                    $"player={player.GlobalPosition} support={support.GetPath()} native_upward_contact=True " +
                    $"margin={margin} focus={camera.Position} limits={camera.LimitLeft},{camera.LimitTop}.." +
                    $"{camera.LimitRight},{camera.LimitBottom} recovery={recoveries}");
            }
            if (deferredResident != null)
            {
                TestAssert.True(level.Residents.Snapshot(deferredResident) is { Alive: true, Attached: false },
                    "visible near-player deferred slot cannot bypass active admission during the hold");
                GD.Print("RESIDENT_PLAYER_RESERVATION_DEFERRED_OK id=" + deferredResident +
                    " alive=true attached=false playing=true stationary_camera_fixture=true normal_route=false");
            }
            GD.Print($"BASIN_ASCENT_CAMERA_CASE_OK sector={sector} high={aboveSharedBand} minMargin={minMargin}");
            if (sector == PrologueSectorId.CanyonBasin)
            {
                // Lifecycle fixture only: a public checkpoint relocation clears
                // the player envelope and normal camera visibility. No manual
                // TrySpawn, manager reset, pose write or disabled camera.
                var before = level.Residents.Snapshot(deferredResident);
                int actualBirths = 0;
                CharacterBody2D born = null;
                void Added(Node node)
                {
                    if (node is CharacterBody2D body &&
                        body.GetNodeOrNull<Knight.Prologue.Residents.ResidentSceneBinding>("ResidentBinding")
                            ?.Profile?.SubtypeId == "scrap-wanderer" &&
                        body.GlobalPosition.DistanceSquaredTo(new Vector2(3770, 3880)) < .01f)
                    { actualBirths++; born = body; }
                }
                tree.NodeAdded += Added;
                try
                {
                    TestAssert.True(world.RestoreCheckpoint("canyon.v2.basin-camp"),
                        "isolated lifecycle fixture uses the real public checkpoint transition");
                    for (int frame = 0; frame < 180 &&
                        !level.Residents.Snapshot(deferredResident).Attached; frame++) await Frames(tree, 1);
                    var after = level.Residents.Snapshot(deferredResident);
                    TestAssert.True(after is { Alive: true, Attached: true, Health: 3 } &&
                        after.Epoch == before.Epoch && after.ResetRevision == before.ResetRevision && actualBirths == 1,
                        "deferred original identity automatically mounts exactly once after genuine admission clears");
                    await Frames(tree, 12);
                    var currentWorld = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                    TestAssert.True(actualBirths == 1 && level.Player.GetInstanceId() == playerId &&
                        ReferenceEquals(currentWorld, world) && currentWorld.GetInstanceId() == worldId && recoveries == 0 &&
                        level.GetNode<PrologueGameFlowController>("GameFlow").State == PrologueGameFlowState.Playing,
                        "late retry preserves session, player/world identity and no duplicate birth or recovery");
                    TestAssert.True(GodotObject.IsInstanceValid(born) && born.IsInsideTree() &&
                        currentWorld.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector currentSector) &&
                        ReferenceEquals(born.GetParent(), currentSector.GetNode("Gameplay/Actors")),
                        "the counted native root belongs to the current authoritative sector");
                    var healthBefore = level.ResidentLayout.ToDictionary(site => site.Definition.StableId,
                        site => level.Residents.Snapshot(site.Definition.StableId).Health);
                    // Controlled custody evidence, not a keyboard combat claim:
                    // damage this exact born root through the real Health event,
                    // and require only the named stable slot to change 3 -> 2.
                    TestAssert.True(born.GetNode<HealthComponent>("Health").ApplyDamage(new(1, Vector2.Zero, 0, 62868UL)) &&
                        born.GetNode<HealthComponent>("Health").CurrentHealth == 2 &&
                        level.Residents.Snapshot(deferredResident).Health == 2 &&
                        healthBefore.Where(pair => pair.Key != deferredResident)
                            .All(pair => level.Residents.Snapshot(pair.Key).Health == pair.Value),
                        "the counted root's real damage receipt must update exactly its deferred stable owner");
                    GD.Print("RESIDENT_PLAYER_RESERVATION_RETRY_OK id=" + deferredResident +
                        " births=1 epoch_preserved=true exact_owner_health=3->2 fixture_checkpoint_transfer=true manual_spawns=0 normal_route=false");
                }
                finally { tree.NodeAdded -= Added; }
            }
        }
        catch (Exception error) { primary = error; throw; }
        finally
        {
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("teardown-enter", level);
#endif
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryCompleted -= Recovered;
                Observe("cleanup-enter", ownedId);
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("original-free-enter", level);
#endif
                if (diagnosticCacheMode.HasValue)
                {
                    await ApprovedLanternRuntimeTests.ReleaseOwnedLevel(tree, level, primary);
                    TestAssert.True(!GodotObject.IsInstanceValid(level), "diagnostic owns and releases this exact mounted Level");
                    Boundary("released", ownedId); // Saved scalar only; never resolve a retired native ID.
                }
                else
                {
                    if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    else level.Free();
                }
                // Only after the original cleanup returns. The retired root's
                // identity is a saved scalar; do not resolve or read it again.
                Observe("cleanup-return", ownedId);
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("original-free-return", level);
#endif
            }
        }
    }

    internal static async Task WaitForPlaying(SceneTree tree, PrologueLevelController level)
    {
        // Population preparation owns gameplay activation; a fixed delay is not
        // evidence that the normal session committed. Do not force any state.
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        for (int frame = 0; frame < 180 && flow.State == PrologueGameFlowState.Intro; frame++)
            await Frames(tree, 1);
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        string unprepared = string.Join(",", level.ResidentLayout
            .Where(site => world.MountedSectorIds.Contains(site.Definition.Sector) &&
                level.Residents.Snapshot(site.Definition.StableId) is { Alive: true, Attached: false })
            .Select(site => site.Definition.StableId));
        TestAssert.True(flow.State == PrologueGameFlowState.Playing && !level.ResidentPreparationPending,
            $"camera fixture must first commit its real session: state={flow.State} " +
            $"pending={level.ResidentPreparationPending} reason={level.ResidentPreparationFailure} " +
            $"unprepared={unprepared}; no camera or motor claim before activation");
    }

    internal static StaticBody2D ResolveAuthoredSupport(SeamlessPrologueWorld world,
        PrologueSectorId sector, string supportName, Rect2 expectedBounds)
    {
        TestAssert.True(world.TryGetMountedSector(sector, out PrologueSector mounted),
            "ground hold resolves its actual mounted sector");
        StaticBody2D support = mounted.GetNode<StaticBody2D>("Gameplay/Collision/" + supportName);
        CollisionShape2D collider = support.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(collider.Shape is RectangleShape2D && !collider.Disabled,
            supportName + ": native support uses the enabled saved rectangular collision");
        var rectangle = (RectangleShape2D)collider.Shape;
        TestAssert.True((collider.GlobalPosition - rectangle.Size * .5f)
                .DistanceTo(expectedBounds.Position) < .01f &&
            rectangle.Size.DistanceTo(expectedBounds.Size) < .01f,
            supportName + ": saved support matches the independently audited world rectangle");
        return support;
    }

    internal static float AssertV2GroundedSample(CharacterBody2D player, SeamlessPrologueWorld world,
        StaticBody2D support, PrologueSectorId sector, Vector2 settled, bool aboveSharedBand,
        Vector2 initialOffset)
    {
        TestAssert.Equal(2, world.LayoutVersion, "grounded holds exercise the saved V2 world");
        TestAssert.True(world.ProcessMode != Node.ProcessModeEnum.Disabled && world.IsPhysicsProcessing(),
            "the real seamless world continues automatic camera updates throughout each hold");
        TestAssert.Equal(Node.ProcessModeEnum.Disabled, player.GetNode<PlayerInputReader>("Input").ProcessMode,
            "grounded holds disable input rather than driving or relocating the player");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        TestAssert.True(motor.ProcessMode != Node.ProcessModeEnum.Disabled && motor.IsPhysicsProcessing(),
            "the actual PlayerMotor remains automatic during every ground sample");
        TestAssert.True(player.IsOnFloor(), "all three camera holds remain on authored ground");
        bool upwardContact = false;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            using KinematicCollision2D contact = player.GetSlideCollision(index);
            if (contact.GetCollider() == support && contact.GetNormal().Y < -.7f)
                upwardContact = true;
        }
        TestAssert.True(upwardContact,
            $"{sector}: native upward slide contact must name {support.GetPath()}; player={player.GlobalPosition}");
        TestAssert.True(player.GlobalPosition.DistanceTo(settled) <= .25f,
            $"{sector}: automatic motor settles on the audited support without relocation; " +
            $"expected={settled} actual={player.GlobalPosition}");
        TestAssert.True(player.GlobalPosition.X > 3840f && player.GlobalPosition.X < 5120f &&
            (sector == PrologueSectorId.CanyonBasin ? player.GlobalPosition.Y > 3600f : player.GlobalPosition.Y < 3600f),
            "the capsule center retains its authoritative y-side of the shared x3840..5120 seam");

        Camera2D camera = player.GetNode<Camera2D>("Camera2D");
        TestAssert.True(camera.Enabled && ReferenceEquals(camera.GetParent(), player),
            "the automatic live camera remains owned by the same persistent player");
        TestAssert.Equal(0, camera.LimitLeft, "V2 holds retain the global design left bound");
        TestAssert.Equal(0, camera.LimitTop, "V2 holds retain the global design top bound");
        TestAssert.Equal(7680, camera.LimitRight, "V2 holds retain the global design right bound");
        TestAssert.Equal(4320, camera.LimitBottom, "V2 holds retain the global design bottom bound");
        Vector2 expectedFocus = sector == PrologueSectorId.CanyonBasin
            ? new Vector2(100, -40) : new Vector2(80, -140);
        TestAssert.True(camera.Position.DistanceTo(expectedFocus) < .25f,
            $"{sector}: stationary V2 camera retains its authored player-local focus; actual={camera.Position}");
        TestAssert.Equal(initialOffset, camera.Offset, "route focus preserves the independent shake Offset channel");
        Vector2 viewport = player.GetViewportRect().Size;
        TestAssert.Equal(new Vector2(1280, 720), viewport,
            "full-capsule projection uses the actual 1280x720 logical viewport");
        Vector2 center = camera.GetScreenCenterPosition();
        Vector2 worldHalf = viewport * .5f / camera.Zoom;
        TestAssert.True(center.X - worldHalf.X >= -.01f && center.Y - worldHalf.Y >= -.01f &&
            center.X + worldHalf.X <= 7680.01f && center.Y + worldHalf.Y <= 4320.01f,
            "the complete horizontal and vertical viewport stays inside V2 global design coverage");
        if (aboveSharedBand)
            TestAssert.True(center.Y - worldHalf.Y < 3600f,
                "high grounded Ascent genuinely looks above Basin's y3600 authored coverage");

        CollisionShape2D collider = player.GetNode<CollisionShape2D>("BodyCollider");
        TestAssert.True(collider.Shape is CapsuleShape2D, "each hold projects the actual full capsule");
        var capsule = (CapsuleShape2D)collider.Shape;
        Transform2D canvas = collider.GetGlobalTransformWithCanvas();
        Vector2 half = new(capsule.Radius, capsule.Height * .5f);
        Vector2[] corners = { canvas * -half, canvas * new Vector2(half.X, -half.Y),
            canvas * half, canvas * new Vector2(-half.X, half.Y) };
        float horizontalMargin = Math.Min(corners.Min(point => point.X), viewport.X - corners.Max(point => point.X));
        float verticalMargin = Math.Min(corners.Min(point => point.Y), viewport.Y - corners.Max(point => point.Y));
        float margin = Math.Min(horizontalMargin, verticalMargin);
        TestAssert.True(margin >= 16f,
            $"{sector}: complete real capsule requires 16px clearance on all four viewport edges; " +
            $"horizontal={horizontalMargin} vertical={verticalMargin}");
        return margin;
    }

#if REFCOUNTED_FATAL_IDENTITY
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
#endif
    internal static PrologueLevelController CreateLevelFixture(PrologueSectorId sector, Vector2 spawn,
        ResourceLoader.CacheMode? diagnosticCacheMode = null, Action loaded = null,
        Action<string, ulong> observe = null)
    {
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("factory-load-enter:" + sector);
#endif
        PackedScene DiagnosticSource()
        {
            PackedScene source = ResourceLoader.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn",
                cacheMode: diagnosticCacheMode.Value);
            loaded?.Invoke();
            return source;
        }
        // Synchronous pass-through only: no new async resource hold, registry,
        // cache mode, wait or lifecycle operation. Returns are not finally logs.
        PackedScene Loaded(PackedScene source)
        {
            observe?.Invoke("load-return", 0);
            observe?.Invoke("instantiate-enter", 0);
            return source;
        }
        observe?.Invoke("load-enter", 0);
#if REFCOUNTED_FATAL_IDENTITY
        var level = Loaded(diagnosticCacheMode.HasValue ? DiagnosticSource() :
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CaptureCameraFixtureSource(
                GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn"), sector))
            .Instantiate<PrologueLevelController>();
#else
        var level = Loaded(diagnosticCacheMode.HasValue ? DiagnosticSource() :
            GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")).Instantiate<PrologueLevelController>();
#endif
        observe?.Invoke("instantiate-return", level.GetInstanceId());
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("factory-instantiated", level);
#endif
        // Controlled pre-tree fixture position. Motor/camera/AI remain automatic;
        // these holds do not claim to exercise the real InputMap seam crossing.
        var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = spawn;
        player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
        level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = sector;
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "basin-camera-" + sector);
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CameraFixtureBoundary("factory-configured", level);
#endif
        return level;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
