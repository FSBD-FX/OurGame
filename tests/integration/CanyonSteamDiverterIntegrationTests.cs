using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Tests.Integration;

// Explicit bounded packed/component and synthetic generated-mounted contracts.
// Manual recorder/direct receiver/fixed-position samples are never input or human proof.
public static class CanyonSteamDiverterIntegrationTests
{
    public const string FrozenId = "canyon.v2.steam-diverter.frozen";
    public const string TrenchId = "canyon.v2.steam-diverter.trench";

    // Break caught: omitted/misbound device, wrong world/local conversion, widened
    // enemy arena or a regenerated artifact that diverges from the live builder.
    public static void RunPackedContracts() => PackedPairsMatchApprovedWorldGeometry();

    // Actual existing private consumer code, no future-API shell or mounted
    // level. Literals are independent of any future corridor implementation.
    public static void RunRouteMembershipContracts()
    {
        MethodInfo recorder = typeof(ProloguePlaytestRecorder).GetMethod("ResolveRouteNode",
            BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Vector2) }, null);
        MethodInfo guidance = typeof(CanyonFireflyGuidanceController).GetMethod("TryResolveRouteNode",
            BindingFlags.NonPublic | BindingFlags.Static, null,
            new[] { typeof(Vector2), typeof(CanyonRouteNodeId).MakeByRefType() }, null);
        if (recorder == null || guidance == null)
            throw new InvalidOperationException("Task17B route prerequisite: both existing private static consumer resolvers must exist.");
        (string Name, string Kind, Vector2 Position, CanyonRouteNodeId? Expected)[] cases =
        {
            ("steam-3890", "ordinary", new(3890, 4110), CanyonRouteNodeId.SteamValves),
            ("steam-4110", "ordinary", new(4110, 4110), CanyonRouteNodeId.SteamValves),
            ("steam-4190", "ordinary", new(4190, 4110), CanyonRouteNodeId.SteamValves),
            ("gap-4210", "gap", new(4210, 4110), CanyonRouteNodeId.SteamValves),
            ("gap-4275", "gap", new(4275, 4130), CanyonRouteNodeId.SteamValves),
            ("gap-4385", "gap", new(4385, 4050), CanyonRouteNodeId.SteamValves),
            ("rejoin-5000", "gap", new(5000, 3601), CanyonRouteNodeId.SteamValves),
            ("lift-precedence", "ordinary", new(5000, 3599), CanyonRouteNodeId.CounterweightLift),
            ("outside-4210", "outside", new(4210, 3900), null),
            ("outside-4600", "outside", new(4600, 3660), null),
            ("outside-5500", "outside", new(5500, 3800), null),
            ("left-top-inclusive", "boundary", new(4510, 3756), CanyonRouteNodeId.SteamValves),
            ("before-left", "boundary", new(4509.999f, 3756), null),
            ("before-top", "boundary", new(4510, 3755.999f), null),
            ("bottom-exclusive", "boundary", new(4210, 4160), null),
            ("before-right", "boundary", new(5139.999f, 3601), CanyonRouteNodeId.SteamValves),
            ("right-exclusive", "boundary", new(5140, 3601), null),
            ("entrance-preserved", "ordinary", new(160, 600), CanyonRouteNodeId.LowCeilingBridge),
            ("frozen-preserved", "ordinary", new(1740, 2500), CanyonRouteNodeId.FrozenDetour),
            ("outside-left", "outside", new(-1, 600), null),
            ("nan-x", "nonfinite", new(float.NaN, 4110), null),
            ("nan-y", "nonfinite", new(4110, float.NaN), null),
            ("nan-both", "nonfinite", new(float.NaN, float.NaN), null),
            ("positive-infinity-x", "nonfinite", new(float.PositiveInfinity, 4110), null),
            ("negative-infinity-x", "nonfinite", new(float.NegativeInfinity, 4110), null),
            ("positive-infinity-y", "nonfinite", new(4110, float.PositiveInfinity), null),
            ("negative-infinity-y", "nonfinite", new(4110, float.NegativeInfinity), null)
        };
        var failures = new List<string>();
        foreach ((string consumer, MethodInfo resolver) in new[] { ("recorder", recorder), ("guidance", guidance) })
        foreach (var sample in cases)
        {
            CanyonRouteNodeId? actual;
            if (consumer == "recorder") actual = (CanyonRouteNodeId?)resolver.Invoke(null, new object[] { sample.Position });
            else
            {
                object[] arguments = { sample.Position, default(CanyonRouteNodeId) };
                actual = (bool)resolver.Invoke(null, arguments) ? (CanyonRouteNodeId)arguments[1] : null;
            }
            string expectedText = sample.Expected?.ToString() ?? "null", actualText = actual?.ToString() ?? "null";
            bool matched = actual == sample.Expected;
            GD.Print($"TASK17B_ROUTE_MEMBERSHIP consumer={consumer} case={sample.Name} kind={sample.Kind} " +
                $"x={sample.Position.X.ToString("R", CultureInfo.InvariantCulture)} y={sample.Position.Y.ToString("R", CultureInfo.InvariantCulture)} " +
                $"expected={expectedText} actual={actualText} matched={matched}");
            // Nonfinite HasPoint bugs are covered behavioral mismatches too,
            // not an unrelated prerequisite that hides the finite corridor RED.
            if (!matched) failures.Add($"{consumer}/{sample.Name}: expected={expectedText} actual={actualText}");
        }
        GD.Print($"TASK17B_ROUTE_MEMBERSHIP_SUMMARY consumers=2 cases={cases.Length} mismatches={failures.Count} mounted=false");
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B both route consumers classify exact steam rejoin membership\n" + string.Join("\n", failures));
        GD.Print("TASK17B_ROUTE_MEMBERSHIP_CONTRACTS_OK consumers=2 literalCases=27 mounted=false");
    }

    public static void RunArchiveMetadataNeighborContract()
    {
        RunInstallationBindingsContracts(); // Current exact new coverage prerequisite.
        GD.Print("TASK17B_ARCHIVE_METADATA_NEIGHBOR_BEGIN method=ParentStreamMetadataMatchesArchiveInstallation offTree=true");
        ArchivePressureGateIntegrationTests.ParentStreamMetadataMatchesArchiveInstallation();
        GD.Print("TASK17B_ARCHIVE_METADATA_NEIGHBOR_OK method=ParentStreamMetadataMatchesArchiveInstallation offTree=true");
    }

    // Break caught: mounted consumers fabricate route exit/reentry in the
    // rejoin union, lose guidance progress, or fail to clear outside membership.
    public static async Task RunMountedRouteContracts(SceneTree tree)
    {
        PrologueLevelController level = NewMountedFixture("task17b-mounted-route", out var flow, out var paths);
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        world.SectorMounted += FreezeMountedActorCallbacks;
        tree.Root.AddChild(level);
        try
        {
            await StartMountedBasin(tree, level, flow, world);
            ProloguePlaytestRecorder recorder = level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder");
            var session = BindMountedManualRecorder(level, recorder, world, "task17b-mounted-route");
            CanyonFireflyGuidanceController guidance = level.GetNode<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance");
            guidance.ClearTransient();
            (Vector2 Position, Vector2 Progress)[] steam =
            {
                (new(3890, 4110), new(3890, 4110)),
                (new(4110, 4110), new(4110, 4110)),
                (new(4190, 4110), new(4110, 4110)),
                (new(4210, 4110), new(4210, 4110)),
                (new(4275, 4130), new(4210, 4110)),
                (new(4385, 4050), new(4385, 4050)),
                (new(5000, 3601), new(5000, 3601))
            };
            foreach (var sample in steam)
            {
                level.Player.GlobalPosition = sample.Position; // Disclosed synthetic sample, no movement claim.
                guidance.RefreshNow();
                ObserveMountedRoute(recorder);
                ObserveMountedRoute(recorder); // Duplicate observation must not create another edge.
                TestAssert.True(ReadPrivate<CanyonRouteNodeId?>(guidance, "_lastRouteNode") == CanyonRouteNodeId.SteamValves &&
                    ReadPrivate<bool>(guidance, "_hasProgressPosition") &&
                    ReadPrivate<Vector2>(guidance, "_lastProgressPosition") == sample.Progress,
                    "mounted guidance retains SteamValves and literal progress anchors through the rejoin corridor");
                AssertMountedRouteEvents(session, "SteamValves/Enter");
                TestAssert.Equal(0, RecorderReport(session).Mechanisms.Count,
                    "route entry and synthetic progress alone never fabricate mechanism activation");
                GD.Print($"TASK17B_MOUNTED_ROUTE_SAMPLE position={sample.Position} route=SteamValves progress={sample.Progress} manual=true synthetic=true");
            }
            level.Player.GlobalPosition = new(5000, 3599);
            guidance.RefreshNow(); ObserveMountedRoute(recorder);
            TestAssert.True(ReadPrivate<CanyonRouteNodeId?>(guidance, "_lastRouteNode") == CanyonRouteNodeId.CounterweightLift &&
                ReadPrivate<bool>(guidance, "_hasProgressPosition") &&
                ReadPrivate<Vector2>(guidance, "_lastProgressPosition") == new Vector2(5000, 3599),
                "actual mounted guidance respects Lift precedence and starts its new progress anchor");
            AssertMountedRouteEvents(session, "SteamValves/Enter", "SteamValves/Exit", "CounterweightLift/Enter");
            level.Player.GlobalPosition = new(4600, 3660);
            guidance.RefreshNow(); ObserveMountedRoute(recorder); ObserveMountedRoute(recorder);
            TestAssert.True(ReadPrivate<CanyonRouteNodeId?>(guidance, "_lastRouteNode") == null &&
                !ReadPrivate<bool>(guidance, "_hasProgressPosition"),
                "outside the actual union clears mounted guidance route and progress state");
            AssertMountedRouteEvents(session, "SteamValves/Enter", "SteamValves/Exit", "CounterweightLift/Enter", "CounterweightLift/Exit");
            AssertManualRecorderUninitialized(recorder);
            TestAssert.True(!Directory.Exists(paths.PlaytestsDirectory), "manual generated recorder never opens automatic output");
            GD.Print("TASK17B_MOUNTED_ROUTE_CONTRACTS_OK generated=true manualRecorder=true syntheticPositions=true actorCallbacksFrozen=true inputProof=false");
        }
        finally
        {
            world.SectorMounted -= FreezeMountedActorCallbacks;
            ResumeOwnedPause(level);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    // Break caught: installed clocks advance during actual pause, world reset
    // misses a pair, retired bindings replay, or generated remount retains local state.
    public static async Task RunMountedLifecycleContracts(SceneTree tree)
    {
        PrologueLevelController level = NewMountedFixture("task17b-mounted-lifecycle", out var flow, out var paths);
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        world.SectorMounted += FreezeMountedActorCallbacks;
        tree.Root.AddChild(level);
        try
        {
            await StartMountedBasin(tree, level, flow, world);
            ProloguePlaytestRecorder recorder = level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder");
            var session = BindMountedManualRecorder(level, recorder, world, "task17b-mounted-lifecycle");
            CanyonSteamDiverter frozen = MountedPair(world, true), trench = MountedPair(world, false);
            TestAssert.True(frozen.MechanismId == FrozenId && trench.MechanismId == TrenchId,
                "actual mounted pairs retain the two exact distinct local telemetry IDs");
            ulong frozenInstance = frozen.GetInstanceId(), trenchInstance = trench.GetInstanceId();
            AssertMountedDefault(frozen); AssertMountedDefault(trench);
            ObserveMountedRoute(recorder); AssertEvents(session, 0, 0, "mounted route entry alone leaves both local activation counts zero");
            AcceptedDirect(frozen, "generated frozen"); AcceptedDirect(trench, "generated trench");
            ObserveMountedRoute(recorder); ObserveMountedRoute(recorder);
            AssertEvents(session, 1, 1, "mounted accepted DIRECT latches record once per distinct local ID");
            TestAssert.True(!frozen.TryReceiveThread(default) && !trench.TryReceiveThread(default), "mounted busy DIRECT calls are rejected");
            ObserveMountedRoute(recorder); AssertEvents(session, 1, 1, "busy rejection cannot add activation events");

            ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open();
            var frozenClock = MountedClockSnapshot(frozen); var trenchClock = MountedClockSnapshot(trench);
            for (int frame = 0; frame < 16; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(tree.Paused && pause.IsOpen && flow.State == PrologueGameFlowState.Paused &&
                frozen.IsHandingOff && trench.IsHandingOff &&
                frozenClock.Equals(MountedClockSnapshot(frozen)) && trenchClock.Equals(MountedClockSnapshot(trench)),
                "actual PauseMenu freezes both installed handoff and jet clocks across ProcessFrame waits");
            pause.Execute(ProloguePauseCommand.Resume);
            TestAssert.True(!tree.Paused && !pause.IsOpen && flow.State == PrologueGameFlowState.Playing, "actual pause menu Resume restores Playing");
            await WaitForHandoffs(tree, frozen, trench);
            AcceptedDirect(frozen, "generated frozen reverse"); AcceptedDirect(trench, "generated trench reverse");
            ObserveMountedRoute(recorder); AssertEvents(session, 1, 1, "fresh mounted reverse while already activated records no extra edge");

            level.Player.GlobalPosition = new(1740, 2604); // Regional reset fixture on real Frozen support.
            await WaitMountedCondition(tree, () => world.CurrentSector == PrologueSectorId.CanyonDescent, "synthetic placement selects real Descent for reset");
            world.ResetCurrentSectorForCheckpoint();
            AssertMountedDefault(frozen); ObserveMountedRoute(recorder);
            AcceptedDirect(frozen, "generated frozen after observed world reset");
            ObserveMountedRoute(recorder); AssertEvents(session, 2, 1, "observed Descent world reset permits one new Frozen lifecycle edge");
            TestAssert.True(world.RestoreCheckpoint("canyon.v2.basin-camp"), "real basin checkpoint accepts Trench reset destination");
            await WaitMountedCondition(tree, () => world.CurrentSector == PrologueSectorId.CanyonBasin, "Basin current for actual reset");
            world.ResetCurrentSectorForCheckpoint();
            AssertMountedDefault(trench); ObserveMountedRoute(recorder);
            AcceptedDirect(trench, "generated trench after observed world reset");
            ObserveMountedRoute(recorder); AssertEvents(session, 2, 2, "observed Basin world reset permits one new Trench lifecycle edge");

            MechanicalClawController claw = level.Player.GetNode<MechanicalClawController>("MechanicalClaw");
            claw.ResetTransientState();
            await CommittedFrames(tree, 2);
            TestAssert.True(claw.ResidencyTarget == null && ReadPrivate<HashSet<PrologueSectorId>>(world, "_pinnedSectors").Count == 0,
                "actual claw release leaves no residency pins before both-pair unload");
            TestAssert.True(world.RestoreCheckpoint("canyon.v2.temple-forecourt"), "real temple checkpoint accepts both-pair unload destination");
            TestAssert.True(!world.MountedSectorIds.Contains(PrologueSectorId.CanyonDescent) &&
                !world.MountedSectorIds.Contains(PrologueSectorId.CanyonBasin) &&
                !frozen.TryReceiveThread(default) && !trench.TryReceiveThread(default),
                "actual detached old pair callbacks reject before retirement frees their objects");
            ObserveMountedRoute(recorder);
            var bindings = ReadPrivate<System.Collections.IDictionary>(recorder, "_mechanismBindings");
            TestAssert.True(!bindings.Contains(frozenInstance) && !bindings.Contains(trenchInstance), "manual recorder prunes both actual retired instance bindings");
            await WaitMountedCondition(tree, () => world.CurrentSector == PrologueSectorId.LostTemple &&
                !GodotObject.IsInstanceValid(frozen) && !GodotObject.IsInstanceValid(trench), "real temple transfer frees both installed old pairs");
            ObserveMountedRoute(recorder); AssertEvents(session, 2, 2, "retired invalid pair references produce no replayed activation");
            TestAssert.True(world.RestoreCheckpoint("canyon.v2.basin-camp"), "actual basin checkpoint remounts both generated pairs");
            await WaitMountedCondition(tree, () => world.TryGetMountedSector(PrologueSectorId.CanyonDescent, out _) &&
                world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out _), "both generated pairs remount at Basin");
            CanyonSteamDiverter nextFrozen = MountedPair(world, true), nextTrench = MountedPair(world, false);
            TestAssert.True(nextFrozen.MechanismId == FrozenId && nextTrench.MechanismId == TrenchId,
                "actual remounted pairs retain both exact stable local IDs");
            TestAssert.True(nextFrozen.GetInstanceId() != frozenInstance && nextTrench.GetInstanceId() != trenchInstance,
                "real streamer remount creates distinct fresh pair identities");
            AssertMountedDefault(nextFrozen); AssertMountedDefault(nextTrench);
            ObserveMountedRoute(recorder); AssertEvents(session, 2, 2, "fresh generated default remount never replays old active flags");
            AcceptedDirect(nextFrozen, "generated frozen remount"); AcceptedDirect(nextTrench, "generated trench remount");
            ObserveMountedRoute(recorder); ObserveMountedRoute(recorder);
            AssertEvents(session, 3, 3, "new generated remount records exactly one new local activation per ID");
            TestAssert.True(!GodotObject.IsInstanceValid(frozen) && !GodotObject.IsInstanceValid(trench) &&
                !bindings.Contains(frozenInstance) && !bindings.Contains(trenchInstance), "new activation cannot resurrect old objects or their pruned bindings");
            AssertManualRecorderUninitialized(recorder);
            TestAssert.True(!Directory.Exists(paths.PlaytestsDirectory), "mounted manual lifecycle recorder writes no automatic telemetry output");
            GD.Print("TASK17B_MOUNTED_LIFECYCLE_CONTRACTS_OK generated=true manualRecorder=true directReceiver=true pause=true worldReset=true unloadBoth=true freshRemount=true actorCallbacksFrozen=true inputProof=false");
        }
        finally
        {
            world.SectorMounted -= FreezeMountedActorCallbacks;
            ResumeOwnedPause(level);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static PrologueLevelController NewMountedFixture(string label,
        out PrologueGameFlowController flow, out IsolatedPrologueFixturePaths paths)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
        paths = IsolatedPrologueFixture.Configure(level, label); // Must precede every AddChild/Ready.
        GD.Print($"TASK17B_MOUNTED_SETUP case={label} storage={paths.CaseRoot} source=AutomatedFixture manualRecorder=true syntheticPositions=true actorCallbacksFrozen=true inputProof=false");
        return level;
    }

    private static async Task StartMountedBasin(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, SeamlessPrologueWorld world)
    {
        await CommittedFrames(tree, 4);
        TestAssert.True(flow.State == PrologueGameFlowState.Title && !flow.HasSave, "isolated generated mounted fixture starts at empty Title");
        flow.StartNew();
        TestAssert.True(flow.State == PrologueGameFlowState.Playing, "actual isolated mounted flow enters Playing");
        TestAssert.True(world.RestoreCheckpoint("canyon.v2.basin-camp"), "actual basin checkpoint restores generated mounted consumer fixture");
        await WaitMountedCondition(tree, () => world.CurrentSector == PrologueSectorId.CanyonBasin &&
            new[] { PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent }
                .All(id => world.TryGetMountedSector(id, out _)), "actual Basin residency includes both pair sectors and Ascent");
    }

    private static ProloguePlaytestSession BindMountedManualRecorder(PrologueLevelController level,
        ProloguePlaytestRecorder recorder, SeamlessPrologueWorld world, string label)
    {
        AssertManualRecorderUninitialized(recorder);
        var session = new ProloguePlaytestSession(label, "2026-09-27T00:00:00Z", "AutomatedFixture");
        recorder.ConfigureOrderedTelemetryForTest(session, world);
        PrivateField(recorder, "_level").SetValue(recorder, level); // Actual ordered route observer dependency only.
        return session;
    }

    private static void ObserveMountedRoute(ProloguePlaytestRecorder recorder)
    {
        MethodInfo observe = typeof(ProloguePlaytestRecorder).GetMethod("ObserveOrderedRouteTelemetry", BindingFlags.Instance | BindingFlags.NonPublic);
        if (observe == null) throw new InvalidOperationException("existing ordered route observer missing");
        observe.Invoke(recorder, null);
    }

    private static void AssertMountedRouteEvents(ProloguePlaytestSession session, params string[] expected) =>
        TestAssert.True(RecorderReport(session).RouteNodes.Select(sample => sample.Node + "/" + sample.Event).SequenceEqual(expected),
            "mounted real ordered observer records exact Steam entry continuity and Lift/outside exits");

    private static FieldInfo PrivateField(object owner, string name) => owner.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("existing private fixture dependency missing: " + name);
    private static T ReadPrivate<T>(object owner, string name) => (T)PrivateField(owner, name).GetValue(owner);
    private static void AssertManualRecorderUninitialized(ProloguePlaytestRecorder recorder) =>
        TestAssert.True(!ReadPrivate<bool>(recorder, "_initialized") && !recorder.IsProcessing() && recorder.LatestJsonPath == string.Empty,
            "headless mounted recorder remains uninitialized and nonpersisting without a hands-on grant");

    private static CanyonSteamDiverter MountedPair(SeamlessPrologueWorld world, bool frozen) =>
        world.GetNode<CanyonSteamDiverter>((frozen ? "CanyonDescent" : "CanyonBasin") +
            "/Gameplay/Mechanisms/SteamDiverter" + (frozen ? "Frozen" : "Trench"));

    private static void AssertMountedDefault(CanyonSteamDiverter pair)
    {
        CanyonSteamJet left = pair.GetNode<CanyonSteamJet>("LeftJet"), right = pair.GetNode<CanyonSteamJet>("RightJet");
        TestAssert.True(!pair.HasActivated && !pair.IsHandingOff && pair.SelectedSide == CanyonSteamDiverterSide.Right &&
            left.DisabledForRun && !right.DisabledForRun && !left.Frame.HazardActive && !right.Frame.HazardActive &&
            left.Collision.Disabled && right.Collision.Disabled && !left.Monitoring && !right.Monitoring,
            "generated pair starts/resets/remounts unactivated Right default with both hazards harmless");
    }

    private static object MountedClockSnapshot(CanyonSteamDiverter pair)
    {
        CanyonSteamDiverterModel model = ReadPrivate<CanyonSteamDiverterModel>(pair, "_model");
        CanyonSteamDiverterClock leftClock = ReadPrivate<CanyonSteamDiverterClock>(pair, "_leftClock"),
            rightClock = ReadPrivate<CanyonSteamDiverterClock>(pair, "_rightClock");
        CanyonSteamJet left = pair.GetNode<CanyonSteamJet>("LeftJet"), right = pair.GetNode<CanyonSteamJet>("RightJet");
        return (ReadPrivate<double>(model, "_handoffElapsed"), leftClock.PhaseElapsedSeconds, rightClock.PhaseElapsedSeconds,
            left.ElapsedSeconds, right.ElapsedSeconds, left.Frame, right.Frame);
    }

    private static async Task WaitMountedCondition(SceneTree tree, Func<bool> condition, string reason)
    {
        for (int frame = 0; frame < 60 && !condition(); frame++) await CommittedFrames(tree, 1);
        TestAssert.True(condition(), reason + " within 60 committed frames");
    }

    // Same test-local Archive functional isolation: callbacks only, never
    // ProcessMode/collision changes/API kills. Fresh streamed sectors inherit it.
    private static void FreezeMountedActorCallbacks(PrologueSector sector)
    {
        Node groups = sector.GetNode("Gameplay/Encounters");
        for (int child = 0; child < groups.GetChildCount(); child++)
            if (groups.GetChild(child) is CanyonEncounterGroup group)
            { group.SetPhysicsProcess(false); group.SetProcess(false); }
        FreezeActorCallbackTree(sector.GetNode("Gameplay/Actors"));
    }

    private static void FreezeActorCallbackTree(Node node)
    {
        node.SetPhysicsProcess(false); node.SetProcess(false);
        for (int child = 0; child < node.GetChildCount(); child++) FreezeActorCallbackTree(node.GetChild(child));
    }

    private static void ResumeOwnedPause(PrologueLevelController level)
    {
        ProloguePauseMenu pause = level.GetNodeOrNull<ProloguePauseMenu>("PauseMenu");
        if (GodotObject.IsInstanceValid(pause) && pause.IsOpen) pause.Execute(ProloguePauseCommand.Resume);
    }

    // Break caught: optional support is never bound by the real builder/pack,
    // or parent seam validation omits the actual controls and jet footprints.
    // Every instance remains OFF TREE; no Ready/full-level/persistence runs.
    public static void RunInstallationBindingsContracts()
    {
        RunPackedContracts(); // Old geometry failures are not this new RED.
        GD.Print("TASK17B_INSTALLATION_BINDINGS_NEIGHBORS_OK packedGeometry=true offTree=true");
        var failures = new List<string>();
        foreach (bool liveBuilder in new[] { false, true })
        foreach (bool frozen in new[] { true, false })
        {
            PrologueSectorId id = frozen ? PrologueSectorId.CanyonDescent : PrologueSectorId.CanyonBasin;
            string source = liveBuilder ? "builder" : "packed";
            string supportName = frozen ? "FrozenRightCatch" : "SteamTrenchFloor";
            PrologueSector sector = liveBuilder ? PrologueCanyonBuildParts.Build(id) :
                GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(id)).Instantiate<PrologueSector>();
            try
            {
                CanyonSteamDiverterVisual visual = Descendants(sector.GetNode("VisualRoot/GameplayReadable"))
                    .OfType<CanyonSteamDiverterVisual>().Single();
                CollisionShape2D actual = sector.GetNode<CollisionShape2D>("Gameplay/Collision/" + supportName + "/Collision");
                string expected = "../../../../Gameplay/Collision/" + supportName + "/Collision";
                bool bound = visual.SupportShapePath != null && !visual.SupportShapePath.IsEmpty &&
                    visual.SupportShapePath.ToString() == expected && visual.GetNodeOrNull(visual.SupportShapePath) == actual;
                GD.Print($"TASK17B_SUPPORT_BINDING source={source} sector={id} floor={supportName} bound={bound} offTree=true");
                TestAssert.True(bound, "Task17B visual binds actual " + supportName + " Collision through SupportShapePath");
                TestAssert.True(visual.ZIndex == 1 && visual.GetParent().Name.ToString() == "MechanismTelegraphs",
                    "installed removable support paint remains above floor/below actors at relativeZ+1");
            }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add($"{source}-support/{id}: {failure.Message}"); }
            finally { sector.Free(); }
        }

        PrologueLevelController parent = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            if (parent.IsInsideTree() || parent.IsNodeReady())
                throw new InvalidOperationException("Task17B parent metadata prerequisite: packed parent must remain off tree.");
            PrologueSectorDefinition[] packed = parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
            if (packed.Length != 5 || packed.Select(definition => definition.Id).Distinct().Count() != 5)
                throw new InvalidOperationException("Task17B parent metadata prerequisite: five unique original sector definitions.");
            foreach (bool liveBuilder in new[] { false, true })
            {
                string source = liveBuilder ? "builder" : "packed";
                var definitions = new List<PrologueSectorDefinition>();
                try
                {
                    if (liveBuilder)
                        foreach (PrologueSectorDefinition template in packed)
                        {
                            // Test-local resources reuse actual parent graph/scene
                            // geometry; only interactions come from the live API.
                            var definition = new PrologueSectorDefinition(template.Id, template.Scene,
                                template.WorldOffset, template.Bounds, template.Previous, template.Next);
                            var interactions = PrologueCanyonBuildParts.CriticalInteractionBounds(template.Id);
                            foreach (Rect2 rect in interactions) definition.KeyInteractionBounds.Add(rect);
                            definitions.Add(definition);
                        }
                    else definitions.AddRange(packed);
                    _ = new PrologueSectorStreamModel(definitions); // Actual96px validation, not fake residency proof.
                    GD.Print($"TASK17B_PARENT_STREAM_VALID source={source} definitions=5 margin=96 offTree=true");
                    foreach (PrologueSectorDefinition definition in definitions)
                    {
                        Rect2[] original = ExistingInteractions(definition.Id);
                        Rect2[] actual = definition.KeyInteractionBounds.ToArray();
                        if (actual.Length < original.Length || !actual.Take(original.Length).SequenceEqual(original) ||
                            (definition.Id is not (PrologueSectorId.CanyonDescent or PrologueSectorId.CanyonBasin) &&
                                !actual.SequenceEqual(original)))
                            throw new InvalidOperationException("Task17B parent metadata prerequisite: every original interaction entry/order must be preserved for " + definition.Id);
                        if (definition.Id is not (PrologueSectorId.CanyonDescent or PrologueSectorId.CanyonBasin)) continue;
                        Rect2[] added = definition.Id == PrologueSectorId.CanyonDescent
                            ? new[] { new Rect2(438, 2478, 44, 44), new Rect2(352, 2465, 56, 150), new Rect2(512, 2465, 56, 150) }
                            : new[] { new Rect2(2688, 448, 44, 44), new Rect2(2578, 435, 64, 150), new Rect2(2798, 435, 64, 150) };
                        GD.Print($"TASK17B_PARENT_COVERAGE source={source} sector={definition.Id} present={added.Count(rect => actual.Contains(rect))} expected=3 existing={original.Length} offTree=true");
                        try
                        {
                            TestAssert.True(actual.Length == original.Length + added.Length && actual.Skip(original.Length).SequenceEqual(added),
                                "Task17B parent seam metadata appends exact control and both jet bounds for " + definition.Id);
                        }
                        catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
                        { failures.Add($"{source}-metadata/{definition.Id}: {failure.Message}"); }
                    }
                }
                finally
                {
                    if (liveBuilder) foreach (PrologueSectorDefinition definition in definitions)
                        definition.Dispose();
                }
            }
        }
        finally { parent.Free(); }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B support bindings and parent seam coverage are incomplete\n" + string.Join("\n", failures));
        GD.Print("TASK17B_INSTALLATION_BINDINGS_CONTRACTS_OK support=true parentCoverage=true streamModel=true offTree=true");
    }

    private static Rect2[] ExistingInteractions(PrologueSectorId id) => id switch
    {
        PrologueSectorId.CanyonEntrance => new[] { new Rect2(416, 636, 48, 40) },
        PrologueSectorId.CanyonDescent => new[] { new Rect2(136, 1416, 48, 48), new Rect2(340, 1580, 80, 60),
            new Rect2(440, 1424, 40, 216), new Rect2(512, 1576, 56, 56), new Rect2(1210, 520, 36, 160) },
        PrologueSectorId.CanyonBasin => new[] { new Rect2(1296, 256, 48, 48) },
        PrologueSectorId.CanyonAscent => new[] { new Rect2(1112, 2472, 56, 56) },
        PrologueSectorId.LostTemple => new[] { new Rect2(532, 222, 56, 56), new Rect2(916, 216, 48, 48) },
        _ => throw new InvalidOperationException("Task17B parent metadata has an undefined sector.")
    };

    public static void PackedPairsMatchApprovedWorldGeometry()
    {
        var failures = new List<string>();
        foreach (bool liveBuilder in new[] { false, true })
        foreach (bool frozen in new[] { true, false })
        {
            PrologueSectorId id = frozen ? PrologueSectorId.CanyonDescent : PrologueSectorId.CanyonBasin;
            GD.Print($"TASK17B_PACKED_BEGIN source={(liveBuilder ? "builder" : "packed")} sector={id}");
            PackedScene packed = liveBuilder ? null : GD.Load<PackedScene>(
                $"res://scenes/prologue/sectors/{id}.tscn");
            if (!liveBuilder && packed == null)
                throw new InvalidOperationException("Task17B prerequisite: accepted existing leaf must load.");
            PrologueSector sector = liveBuilder ? PrologueCanyonBuildParts.Build(id) : packed.Instantiate<PrologueSector>();
            try { AssertPair(sector, frozen); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add($"{(liveBuilder ? "builder" : "packed")}/{id}: {failure.Message}"); }
            finally { sector.Free(); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: Task17B approved paired installations are missing or inconsistent\n" + string.Join("\n", failures));
        TestAssert.Equal(new Rect2(3040, 3960, 1160, 360),
            CanyonWorldLayout.Nodes.Single(node => node.Id == CanyonRouteNodeId.SteamValves).WorldBounds,
            "SteamValves route supports the actual paired controls through x4200");
        TestAssert.Equal(new Rect2(3040, 3960, 800, 360),
            CanyonEncounterCatalog.Normal.Single(encounter => encounter.StableId == "canyon.v2.encounter.steam-valves").ArenaBounds,
            "SteamValves enemy arena retains its approved 800-wide footprint");
    }

    private static void AssertPair(PrologueSector sector, bool frozen)
    {
        string name = frozen ? "SteamDiverterFrozen" : "SteamDiverterTrench";
        string id = frozen ? FrozenId : TrenchId;
        Vector2 origin = frozen ? new(1280, 0) : new(1280, 3600);
        Vector2 controlWorld = frozen ? new(1740, 2500) : new(3990, 4070);
        Vector2 leftWorld = frozen ? new(1660, 2540) : new(3890, 4110);
        Vector2 rightWorld = frozen ? new(1820, 2540) : new(4110, 4110);
        Vector2 size = frozen ? new(56, 150) : new(64, 150);
        CanyonSteamDiverter pair = sector.GetNodeOrNull<CanyonSteamDiverter>("Gameplay/Mechanisms/" + name);
        TestAssert.True(pair != null, "approved " + name + " paired installation exists under Gameplay/Mechanisms");
        TestAssert.Equal(id, pair.MechanismId, "installation has its exact unique local telemetry ID");
        TestAssert.Equal("LeftJet", pair.LeftJetPath.ToString(), "left child uses the reviewed default binding");
        TestAssert.Equal("RightJet", pair.RightJetPath.ToString(), "right child uses the reviewed default binding");
        foreach ((string child, Vector2 world) in new[] { ("LeftJet", leftWorld), ("RightJet", rightWorld) })
        {
            CanyonSteamJet jet = pair.GetNodeOrNull<CanyonSteamJet>(child);
            TestAssert.True(jet != null, "each approved jet is a real individually bound CanyonSteamJet");
            TestAssert.Equal(world, origin + pair.Position + jet.Position, "jet world center converts sector-local exactly once");
            TestAssert.True(jet.Collision.Shape is RectangleShape2D rect && rect.Size == size && jet.Collision.Disabled,
                "approved full-size jet collision is authored disabled");
            TestAssert.True(jet.SafeSeconds == 1f && jet.TelegraphSeconds == .4f && jet.ActiveSeconds == .6f,
                "new jet retains reviewed default tuning");
        }
        ThreadTarget2D control = sector.GetNodeOrNull<ThreadTarget2D>("Gameplay/GrappleSurfaces/" + name + "Control");
        TestAssert.True(control != null && origin + control.Position == controlWorld &&
            control.Kind == ThreadTargetKind.Mechanism &&
            control.PullReceiverPath.ToString() == "../../Mechanisms/" + name,
            "existing static claw target binds the actual local receiver at approved world position");
        TestAssert.True(control.GetNode<CollisionShape2D>("TargetCollision/Collision").Shape is CircleShape2D circle && circle.Radius == 22f &&
            control.GetNodeOrNull<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter") != null,
            "approved control keeps 22-radius target and existing claw adapter");
        CanyonSteamDiverterVisual[] skins = Descendants(sector.GetNode("VisualRoot/GameplayReadable"))
            .OfType<CanyonSteamDiverterVisual>().ToArray();
        TestAssert.True(skins.Length == 1 && skins[0].GetNodeOrNull<CanyonSteamDiverter>(skins[0].ControllerPath) == pair &&
            skins[0].GetNodeOrNull<ThreadTarget2D>(skins[0].ControlPath) == control,
            "removable visual resolves actual controller and actual static control explicitly");
        StaticBody2D floor = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + (frozen ? "FrozenRightCatch" : "SteamTrenchFloor"));
        Vector2 floorSize = frozen ? new(340, 40) : new(1160, 40);
        Vector2 floorCenter = frozen ? new(1750, 2660) : new(3620, 4256);
        TestAssert.True(origin + floor.Position == floorCenter && floor.GetNode<CollisionShape2D>("Collision").Shape is RectangleShape2D support && support.Size == floorSize,
            "approved real support remains intact without an invented platform");
        TestAssert.Equal(1, Descendants(sector).OfType<CanyonSteamDiverter>().Count(), "each branch contains exactly one paired controller");
        if (!frozen)
        {
            TestAssert.Equal(new Vector2(3160, 4120), origin + sector.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve").Position, "old valve body remains unchanged");
            TestAssert.Equal(new Vector2(3160, 4040), origin + sector.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamValveControl").Position, "old valve actual control remains unchanged");
            TestAssert.Equal(new Vector2(3460, 4110), origin + sector.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet").Position, "old autonomous jet remains unchanged");
        }
    }

    public static Task RunRuntimeContracts(SceneTree tree) => ActivationOnlyFollowsAcceptedLatch(tree);

    // Break caught: accepted receiver does not expose local activation, reset
    // retains activation, or a failed/retired receiver creates an activation.
    // This does NOT claim mapped E/LMB or generated-route traversal evidence.
    public static async Task ActivationOnlyFollowsAcceptedLatch(SceneTree tree)
    {
        var root = new Node2D { Name = "Task17BDirectReceiverLocalActivation" };
        var pair = new CanyonSteamDiverter { Name = "Pair", MechanismId = FrozenId };
        pair.AddChild(Jet("LeftJet", new(-80, 0)));
        pair.AddChild(Jet("RightJet", new(80, 0)));
        root.AddChild(pair);
        tree.Root.AddChild(root);
        try
        {
            if (!pair.IsNodeReady()) throw new InvalidOperationException("Task17B prerequisite: component Ready before direct receiver observation.");
            TestAssert.True(!pair.HasActivated, "unoperated local device has no activation");
            bool accepted = pair.TryReceiveThread(default);
            if (!accepted || !pair.IsHandingOff) throw new InvalidOperationException("Task17B prerequisite: reviewed intact receiver must accept first latch.");
            GD.Print("TASK17B_DIRECT_ACCEPT accepted=true hasActivated=" + pair.HasActivated);
            TestAssert.True(pair.HasActivated, "Task17B accepted latch publishes local activation");
            TestAssert.True(!pair.TryReceiveThread(default) && pair.HasActivated,
                "busy rejection retains one existing activation without a new accepted latch");
            pair.ResetForCheckpoint();
            TestAssert.True(!pair.HasActivated && pair.SelectedSide == CanyonSteamDiverterSide.Right && !pair.IsHandingOff,
                "checkpoint reset clears local activation and restores default selection");
            root.RemoveChild(pair);
            TestAssert.True(!pair.TryReceiveThread(default) && !pair.HasActivated,
                "retired stale receiver cannot create activation");
            pair.ResetForCheckpoint();
            TestAssert.True(!pair.HasActivated, "fail-closed checkpoint reset cannot retain activation");
            pair.Free();
            foreach (string id in new[] { FrozenId, TrenchId })
            {
                PrologueSaveData save = PrologueSaveData.NewLayoutV2();
                save.ActivatedMechanisms.Add(id);
                bool rejected = false;
                try { save.Normalize(); } catch (InvalidDataException) { rejected = true; }
                TestAssert.True(rejected, "local diverter ID is not accepted as durable progression: " + id);
            }
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(pair) && !pair.IsInsideTree()) pair.Free();
            root.QueueFree();
            for (int i = 0; i < 3; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    public static async Task RunRecorderContracts(SceneTree tree, string storage)
    {
        if (DisplayServer.GetName() != "headless")
            throw new InvalidOperationException("Task17B prerequisite: manual component recorder requires headless no-persistence mode.");
        // Safe focused neighbors execute before the new assertion. A failure here
        // is not the expected missing-diverter-binding RED.
        await RecorderPreservesPressureGateAndOldValve(tree, storage);
        await RecorderObservesLocalActivationAndRemount(tree, storage);
        GD.Print("TASK17B_COMPONENT_RECORDER_CONTRACTS_OK manual=true directReceiver=true fullLevel=false");
    }

    // Break caught: missing controller binding, fabricated baseline events,
    // duplicate observations/casts, or stale retired-instance publication.
    public static async Task RecorderObservesLocalActivationAndRemount(SceneTree tree, string storage)
    {
        var root = new Node2D { Name = "Task17BManualComponentRecorder" };
        CanyonSteamDiverter frozen = RecorderPair("Frozen", FrozenId, new(400, 300));
        CanyonSteamDiverter trench = RecorderPair("Trench", TrenchId, new(800, 300));
        var recorder = ManualRecorder(storage, "PairRecorder");
        var session = new ProloguePlaytestSession("task17b-direct-manual-pairs", "2026-09-27T00:00:00Z", "AutomatedFixture");
        root.AddChild(frozen); root.AddChild(trench); root.AddChild(recorder);
        tree.Root.AddChild(root);
        try
        {
            await CommittedFrames(tree, 2);
            RecorderPrerequisite(recorder, frozen, trench);
            recorder.ConfigureOrderedTelemetryForTest(session, root);
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 0, 0, "mounted unoperated pairs produce no local activation event");

            AcceptedDirect(frozen, "initial frozen"); AcceptedDirect(trench, "initial trench");
            recorder.ObserveOrderedTelemetryForTest();
            ProloguePlaytestReport first = RecorderReport(session);
            GD.Print($"TASK17B_RECORDER_DIRECT_ACCEPT frozenActive={frozen.HasActivated} trenchActive={trench.HasActivated} " +
                $"frozenEvents={first.Mechanisms.Count(sample => sample.MechanismId == FrozenId)} " +
                $"trenchEvents={first.Mechanisms.Count(sample => sample.MechanismId == TrenchId)} manual=true");
            AssertEvents(session, 1, 1, "Task17B recorder records accepted local diverter activation");
            recorder.ObserveOrderedTelemetryForTest(); recorder.ObserveOrderedTelemetryForTest();
            TestAssert.True(!frozen.TryReceiveThread(default) && !trench.TryReceiveThread(default),
                "real busy pairs reject additional DIRECT casts");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 1, 1, "busy rejection and repeated observations do not double-record activation");

            await WaitForHandoffs(tree, frozen, trench);
            AcceptedDirect(frozen, "fresh reverse frozen"); AcceptedDirect(trench, "fresh reverse trench");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 1, 1, "fresh accepted switch while local flag remains true is not another lifecycle rising edge");

            frozen.ResetForCheckpoint(); trench.ResetForCheckpoint();
            TestAssert.True(!frozen.HasActivated && !trench.HasActivated,
                "observed checkpoint reset clears both local flags");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 1, 1, "reset itself records no activation");
            AcceptedDirect(frozen, "post-reset frozen"); AcceptedDirect(trench, "post-reset trench");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 2, 2, "accepted latch after an observed reset creates exactly one new lifecycle edge per ID");

            root.RemoveChild(frozen); root.RemoveChild(trench);
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.True(!frozen.TryReceiveThread(default) && !trench.TryReceiveThread(default),
                "retired old pair callbacks cannot accept or reactivate");
            frozen.Advance(10d); trench.Advance(10d);
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 2, 2, "manual observation prunes retired bindings without replaying old active flags");

            CanyonSteamDiverter nextFrozen = RecorderPair("FrozenRemount", FrozenId, new(400, 300));
            CanyonSteamDiverter nextTrench = RecorderPair("TrenchRemount", TrenchId, new(800, 300));
            root.AddChild(nextFrozen); root.AddChild(nextTrench);
            await CommittedFrames(tree, 2);
            TestAssert.True(!nextFrozen.HasActivated && !nextTrench.HasActivated &&
                nextFrozen.SelectedSide == CanyonSteamDiverterSide.Right && nextTrench.SelectedSide == CanyonSteamDiverterSide.Right,
                "new default remount has independent false activation and default side");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 2, 2, "fresh default remount never replays removed-instance activation");
            AcceptedDirect(nextFrozen, "remounted frozen"); AcceptedDirect(nextTrench, "remounted trench");
            recorder.ObserveOrderedTelemetryForTest(); recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 3, 3, "new remount first accepted latch records once per exact ID");
            TestAssert.True(!frozen.TryReceiveThread(default) && !trench.TryReceiveThread(default),
                "old callbacks remain rejected after new instances activate");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEvents(session, 3, 3, "stale callbacks cannot publish into the new mount's recorder lifecycle");

            var lateRecorder = ManualRecorder(storage, "LatePairRecorder");
            root.AddChild(lateRecorder);
            await CommittedFrames(tree, 1);
            var lateSession = new ProloguePlaytestSession("task17b-direct-manual-late", "2026-09-27T00:00:00Z", "AutomatedFixture");
            lateRecorder.ConfigureOrderedTelemetryForTest(lateSession, root);
            lateRecorder.ObserveOrderedTelemetryForTest();
            AssertEvents(lateSession, 0, 0, "already-active late-bound real pairs establish a baseline rather than fabricating an event");
            nextFrozen.ResetForCheckpoint(); lateRecorder.ObserveOrderedTelemetryForTest();
            AcceptedDirect(nextFrozen, "late observed-reset frozen");
            lateRecorder.ObserveOrderedTelemetryForTest(); lateRecorder.ObserveOrderedTelemetryForTest();
            AssertEvents(lateSession, 1, 0, "late baseline still observes a later real false-to-true edge without replaying the untouched trench");
            TestAssert.True(recorder.LatestJsonPath == string.Empty && lateRecorder.LatestJsonPath == string.Empty,
                "manual uninitialized component recorders have not opened persistence output");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(frozen) && !frozen.IsInsideTree()) frozen.Free();
            if (GodotObject.IsInstanceValid(trench) && !trench.IsInsideTree()) trench.Free();
            root.QueueFree(); await CommittedFrames(tree, 3);
        }
    }

    // Real gate physics/anti-crush opening and old valve DIRECT acceptance, not
    // mocked flags. The plain CharacterBody2D is a disclosed protected-occupant
    // fixture; its one transform change is not player movement/route evidence.
    private static async Task RecorderPreservesPressureGateAndOldValve(SceneTree tree, string storage)
    {
        var root = new Node2D { Name = "Task17BManualRecorderUnchangedNeighbors" };
        var occupant = new CharacterBody2D
        {
            Name = "ProtectedOccupantFixture", Position = new(1500, 1604),
            CollisionLayer = CombatCollisionLayers.Player, CollisionMask = CombatCollisionLayers.World
        };
        occupant.AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = 12, Height = 72 } });
        var gate = new CanyonPressurePlateGate { Name = "ExistingGate", PlayerBody = occupant };
        var door = new StaticBody2D { Name = "Door", Position = new(1740, 1532), CollisionLayer = CombatCollisionLayers.World };
        door.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new(40, 216) }, Disabled = true });
        gate.AddChild(door);
        CanyonSteamJet jet = Jet("ExistingJet", new(2800, 3000));
        var valve = new CanyonSteamValve { Name = "ExistingValve", PairedJetPath = new("../ExistingJet") };
        var recorder = ManualRecorder(storage, "NeighborRecorder");
        root.AddChild(occupant); root.AddChild(gate); root.AddChild(jet); root.AddChild(valve); root.AddChild(recorder);
        tree.Root.AddChild(root);
        var session = new ProloguePlaytestSession("task17b-manual-neighbors", "2026-09-27T00:00:00Z", "AutomatedFixture");
        try
        {
            await CommittedFrames(tree, 3);
            if (gate.HasOpened || valve.IsActivated || recorder.IsProcessing())
                throw new InvalidOperationException("Task17B neighbor prerequisite: unoperated gate/valve and nonpersisting recorder baseline.");
            recorder.ConfigureOrderedTelemetryForTest(session, root);
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.Equal(0, RecorderReport(session).Mechanisms.Count, "unchanged neighbor baseline produces no activation");
            occupant.GlobalPosition = new(1820, 1604);
            await CommittedFrames(tree, 8);
            if (!gate.HasOpened || !gate.IsOpen || !gate.DoorCollision.Disabled)
                throw new InvalidOperationException("Task17B neighbor prerequisite: real protected-occupant gate opening must commit.");
            if (!valve.TryReceiveThread(default) || !valve.IsActivated || !jet.DisabledForRun)
                throw new InvalidOperationException("Task17B neighbor prerequisite: old DIRECT valve activation must disarm its actual paired jet.");
            recorder.ObserveOrderedTelemetryForTest(); recorder.ObserveOrderedTelemetryForTest();
            ProloguePlaytestReport report = RecorderReport(session);
            TestAssert.True(report.Mechanisms.Count == 2 &&
                report.Mechanisms.Count(sample => sample.MechanismId == "canyon.v2.pressure-gate.archive-reward") == 1 &&
                report.Mechanisms.Count(sample => sample.MechanismId == "canyon.v2.steam-valve-lower") == 1,
                "unchanged pressure-gate and old-valve recorder cases each retain one real rising edge");
            TestAssert.True(recorder.LatestJsonPath == string.Empty, "neighbor component recorder writes no persistence output");
            GD.Print("TASK17B_RECORDER_NEIGHBORS_OK manual=true protectedOccupantGate=true directValve=true");
        }
        finally { root.QueueFree(); await CommittedFrames(tree, 3); }
    }

    private static CanyonSteamDiverter RecorderPair(string name, string id, Vector2 position)
    {
        var pair = new CanyonSteamDiverter { Name = name, MechanismId = id, Position = position };
        pair.AddChild(Jet("LeftJet", new(-80, 0))); pair.AddChild(Jet("RightJet", new(80, 0)));
        return pair;
    }

    private static ProloguePlaytestRecorder ManualRecorder(string storage, string name) => new()
    {
        Name = name, OutputDirectory = Path.Combine(storage, "manual-component-recorders", name)
    };

    private static void RecorderPrerequisite(ProloguePlaytestRecorder recorder, params CanyonSteamDiverter[] pairs)
    {
        if (recorder.IsProcessing() || recorder.LatestJsonPath != string.Empty ||
            pairs.Any(pair => !pair.IsNodeReady() || pair.HasActivated))
            throw new InvalidOperationException("Task17B recorder prerequisite: Ready/default pairs and headless nonpersisting recorder.");
    }

    private static void AcceptedDirect(CanyonSteamDiverter pair, string label)
    {
        if (!pair.TryReceiveThread(default) || !pair.HasActivated || !pair.IsHandingOff)
            throw new InvalidOperationException("Task17B recorder prerequisite: reviewed DIRECT acceptance/local flag/handoff failed: " + label);
    }

    private static ProloguePlaytestReport RecorderReport(ProloguePlaytestSession session) =>
        session.BuildReport(PrologueSectorId.CanyonBasin, PrologueObjective.ReachBasin, "2026-09-27T00:00:01Z");

    private static void AssertEvents(ProloguePlaytestSession session, int frozen, int trench, string reason)
    {
        ProloguePlaytestReport report = RecorderReport(session);
        TestAssert.True(report.Mechanisms.Count == frozen + trench &&
            report.Mechanisms.Count(sample => sample.MechanismId == FrozenId) == frozen &&
            report.Mechanisms.Count(sample => sample.MechanismId == TrenchId) == trench, reason);
    }

    private static async Task WaitForHandoffs(SceneTree tree, params CanyonSteamDiverter[] pairs)
    {
        for (int i = 0; i < 120 && pairs.Any(pair => pair.IsHandingOff); i++) await CommittedFrames(tree, 1);
        if (pairs.Any(pair => pair.IsHandingOff))
            throw new InvalidOperationException("Task17B recorder prerequisite: real owned-clock handoff exceeded bounded fixture deadline.");
    }

    private static async Task CommittedFrames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static CanyonSteamJet Jet(string name, Vector2 center)
    {
        var jet = new CanyonSteamJet { Name = name, Position = center };
        jet.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new(56, 150) }, Disabled = true });
        jet.AddChild(new Polygon2D { Name = "Telegraph" });
        jet.AddChild(new Polygon2D { Name = "ActiveVisual" });
        return jet;
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node nested in Descendants(child)) yield return nested;
        }
    }
}
