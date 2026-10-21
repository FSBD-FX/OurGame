using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Skitter;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class ContinuousCanyonEncounterTests
{
    public static async Task Run(SceneTree tree)
    {
        await RunTask6PhysicsContactBatch(tree);
        await RunTask6MainRouteActivation(tree);
        await GeneratedFirstCombatGuidanceUsesLiveEncounterActor(tree);
        await RealDefeatsAdvanceBatchesAndCompleteOnce(tree);
        await RetirementAndResetNeverFabricateDefeats(tree);
        await ControlledAbyssWaitsForActualSafeLanding(tree);
        await GeneratedAdjacentGroupsRespectEffectiveRangeCap(tree);
        await GeneratedMain5PreservesObservationThenEngages(tree);
        await GeneratedFrozenArenasPreserveNoncombatBypass(tree);
        await GeneratedMain7DefeatSuppliesExactSealInput(tree);
        await GeneratedCourtyardRetiresLiveEnemyBeforeRest(tree);
        GD.Print("TASK6_CONTINUOUS_ENCOUNTERS_OK genuine_defeats=True retirement_safe=True safe_landing=True preboss_rest=True");
    }

    public static async Task RunTask14(SceneTree tree)
    {
        await ArrivalUsesPlayerPositionAtBirthAfterVisibleDeadline(tree);
        await ArrivalCueCancelsOnLifecycleChange(tree);
        await ArrivalCueFreezesWithPausedPhysics(tree);
        await PackedPlayerDeathCancelsArrivalWithoutDefeat(tree);
        await RunSuppressionRecoveryGuards(tree);
        await SplitFloorGapRejectsBridgedFootprint(tree);
        await NearRestBoundaryRejectsBodyIntrusion(tree);
        await FrozenRealCapsuleOverhangRetainsOnlyUntilWholeBodyExits(tree);
        await GeneratedPairsRemainSupportedAndChooseAwayFromPlayer(tree);
        GD.Print("TASK14_ENCOUNTER_BEHAVIOR_OK");
    }

    public static async Task RunTask14FrozenOnly(SceneTree tree) =>
        await FrozenRealCapsuleOverhangRetainsOnlyUntilWholeBodyExits(tree);

    public static async Task RunTask14GeneratedOnly(SceneTree tree) =>
        await GeneratedPairsRemainSupportedAndChooseAwayFromPlayer(tree);

    public static async Task RunSuppressionRecoveryGuards(SceneTree tree)
    {
        await InvalidPairSuppressesWithoutCompletingAndResetsOnExit(tree);
        await SuppressedWholeBatchRevalidatesAfterNativeBlockerChanges(tree);
        await SuppressionAndRewarningCancelOnOrdinaryLifecycle(tree);
        await SuppressionAndRewarningFreezeWithPausedPhysics(tree);
        GD.Print("TASK14_SUPPRESSION_RECOVERY_GUARDS_OK quiet_invalid=True full_batch=True " +
            "birth_revalidation=True ordinary_lifecycle=True paused_physics=True synchronous_reentry=False");
    }

    private static async Task ArrivalUsesPlayerPositionAtBirthAfterVisibleDeadline(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-arrival", CanyonRouteNodeId.GearPistons, false,
            new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(0, -6), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(100, 1), 1,
                    new Vector2(-100, 1))
            })), new Vector2(100, -6));
        AddFloor(fixture.Root, new Vector2(0, 50), new Vector2(500, 40));
        try
        {
            await Frames(tree, 4);
            Kill(fixture.Actors.GetChild(0), 714001UL);
            await Frames(tree, 2);
            TestAssert.True(fixture.Group.IsActive && fixture.Group.LiveActorCount == 0 &&
                            fixture.Group.IsArrivalWarningActive && !fixture.Group.IsComplete,
                "actual first defeat begins visible warning without mounting a damaging second actor");
            await Frames(tree, 24);
            TestAssert.True(fixture.Group.LiveActorCount == 0,
                "second actor cannot appear before the 0.60-second physics warning deadline");
            fixture.Player.GlobalPosition = new Vector2(-100, -6);
            await Frames(tree, 18);
            TestAssert.True(fixture.Group.LiveActorCount == 1 &&
                            fixture.Actors.GetChild(0) is Knight.Enemies.Sentry.BellBoltSentry sentry &&
                            MathF.Abs(sentry.GlobalPosition.X - 100f) < 1f,
                "birth-time player movement selects the farther valid primary and exactly one real sentry");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task InvalidPairSuppressesWithoutCompletingAndResetsOnExit(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-invalid-arrival", CanyonRouteNodeId.GearPistons, false,
            new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, Vector2.Zero, 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SkitterScene, new Vector2(100, 1), 1,
                    new Vector2(-100, 1))
            })), Vector2.Zero);
        using var probe = new ArrivalProbe(fixture);
        try
        {
            await Frames(tree, 4);
            Kill(fixture.Actors.GetChild(0), 714002UL);
            await Frames(tree, 45);
            TestAssert.True(fixture.Group.SpawnSuppressed && !fixture.Group.IsComplete &&
                            fixture.Group.IsActive && fixture.Group.LiveActorCount == 0 &&
                            !fixture.Group.IsArrivalWarningActive &&
                            !fixture.Progress.ActivatedMechanisms.Contains(fixture.Group.EncounterId.ToString()),
                "unsupported pair ends once as explicit non-defeat suppression without progression");
            await Frames(tree, 90);
            TestAssert.True(fixture.Group.SpawnSuppressed && fixture.Group.LiveActorCount == 0 &&
                            !fixture.Group.IsArrivalWarningActive && probe.Suppressions == 1 &&
                            probe.WarningStarts.Count == 2 && probe.ActualBirths == 1 &&
                            !fixture.Group.IsComplete && probe.Completions == 0 && probe.Defeats == 0,
                "persistently unsupported sites stay quietly suppressed without warning/signal storms, " +
                "partial birth or fabricated progression while same-visit legal preflight remains allowed");
            fixture.Player.GlobalPosition = new Vector2(900, 0);
            fixture.Group._PhysicsProcess(0);
            TestAssert.True(!fixture.Group.SpawnSuppressed && !fixture.Group.IsActive,
                "real arena departure clears the exceptional arrival state");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task SuppressedWholeBatchRevalidatesAfterNativeBlockerChanges(SceneTree tree)
    {
        var (fixture, leftBlocker, rightBlocker) = CreateBlockedArrivalFixture(tree, "blocker-recheck");
        using var probe = new ArrivalProbe(fixture);
        try
        {
            await BeginBlockedArrival(tree, fixture, probe, 715001UL);
            leftBlocker.QueueFree();
            await Frames(tree, 60);
            TestAssert.True(fixture.Group.SpawnSuppressed && !fixture.Group.IsArrivalWarningActive &&
                            fixture.Group.LiveActorCount == 0 && probe.Suppressions == 1 &&
                            probe.WarningStarts.Count == 2 && probe.ActualBirths == 1,
                "removing only one actual blocker cannot preflight or mount a partial two-actor batch");

            rightBlocker.QueueFree();
            await AwaitRewarning(tree, fixture, probe, 4);
            StaticBody2D lateBlocker = AddArrivalBlocker(fixture.Root, new Vector2(100, -4));
            await Frames(tree, 42);
            TestAssert.True(fixture.Group.SpawnSuppressed && !fixture.Group.IsArrivalWarningActive &&
                            fixture.Group.LiveActorCount == 0 && probe.Suppressions == 2 &&
                            probe.WarningStarts.Count == 4 && probe.ActualBirths == 1 &&
                            probe.Completions == 0 && probe.Defeats == 0,
                "a real blocker added after legal preflight is rechecked at birth and suppresses " +
                "the entire batch without stale cached positions or a partial sibling");
            await Frames(tree, 30);
            TestAssert.True(probe.Suppressions == 2 && probe.WarningStarts.Count == 4 &&
                            fixture.Group.LiveActorCount == 0,
                "the new illegal state remains quiet across further preflight opportunities");

            lateBlocker.QueueFree();
            await AwaitRewarning(tree, fixture, probe, 6);
            await Frames(tree, 24);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && !fixture.Group.SpawnSuppressed &&
                            fixture.Group.LiveActorCount == 0 && probe.ActualBirths == 1,
                "a second legal transition restarts the full warning rather than spending old warning time");
            await Frames(tree, 18);
            TestAssert.True(!fixture.Group.IsArrivalWarningActive && !fixture.Group.SpawnSuppressed &&
                            fixture.Group.IsActive && fixture.Group.CurrentBatch == 1 &&
                            fixture.Group.LiveActorCount == 2 && probe.ActualBirths == 3 &&
                            probe.WarningStarts.Count == 6 && probe.WarningEnds.Count == 6 &&
                            probe.Completions == 0 && probe.Defeats == 0 &&
                            !fixture.Progress.ActivatedMechanisms.Contains(fixture.Group.EncounterId.ToString()),
                "both literal sentries mount after a fresh deadline without completing from a retry");
            for (int i = 0; i < probe.WarningStarts.Count; i++)
                TestAssert.True(probe.WarningEnds[i] - probe.WarningStarts[i] >= 36UL,
                    "each actual warning Ready-to-TreeExiting interval spans at least 36 unpaused physics ticks");
            for (int i = fixture.Actors.GetChildCount() - 1; i >= 0; i--)
            {
                Node actor = fixture.Actors.GetChild(i);
                if (actor is IEncounterActor) Kill(actor, (ulong)(715010 + i));
            }
            await Frames(tree, 3);
            TestAssert.True(fixture.Group.IsComplete && !fixture.Group.IsActive &&
                            fixture.Group.LiveActorCount == 0 && probe.Completions == 1 && probe.Defeats == 1 &&
                            fixture.Progress.ActivatedMechanisms.Contains(fixture.Group.EncounterId.ToString()),
                "only genuine final actor defeats complete and persist the recovered batch exactly once");
            await Frames(tree, 30);
            TestAssert.True(probe.ActualBirths == 3 && probe.Completions == 1 && probe.Defeats == 1 &&
                            probe.Suppressions == 2,
                "completion clears recovery work and cannot replay actors, suppression or completion");
            GD.Print("TASK14_SUPPRESSION_NATIVE_RECHECK_OK first_blocked=True one_clear_still_blocked=True " +
                "late_blocker_atomic=True full_warning_twice=True genuine_completion=1");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task SuppressionAndRewarningCancelOnOrdinaryLifecycle(SceneTree tree)
    {
        ulong source = 715100UL;
        foreach (bool rewarning in new[] { false, true })
        foreach (string change in new[] { "reset", "arena-exit", "missing-player", "death", "owner-removal", "queued-owner" })
        {
            var (fixture, leftBlocker, rightBlocker) = CreateBlockedArrivalFixture(tree,
                $"lifecycle-{change}-{rewarning}", packedPlayer: true);
            using var probe = new ArrivalProbe(fixture);
            string stableId = fixture.Group.EncounterId.ToString();
            try
            {
                await BeginBlockedArrival(tree, fixture, probe, source++);
                if (rewarning)
                {
                    leftBlocker.QueueFree();
                    rightBlocker.QueueFree();
                    await AwaitRewarning(tree, fixture, probe, 4);
                    await Frames(tree, 5);
                }
                switch (change)
                {
                    case "reset":
                        fixture.Player.GlobalPosition = new Vector2(900, 0);
                        fixture.Group.ResetForCheckpoint();
                        break;
                    case "arena-exit":
                        fixture.Player.GlobalPosition = new Vector2(900, 0);
                        break;
                    case "missing-player":
                        fixture.Player.RemoveFromGroup("player");
                        break;
                    case "death":
                        HealthComponent health = fixture.Player.GetNode<HealthComponent>("Health");
                        TestAssert.True(fixture.Player.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(
                                health.CurrentHealth, Vector2.Right, 0f, source++)) && health.CurrentHealth == 0,
                            "ordinary lifecycle death uses the real packed player Hurtbox and Health");
                        break;
                    case "owner-removal":
                        fixture.Root.RemoveChild(fixture.Group);
                        fixture.Group.QueueFree();
                        break;
                    case "queued-owner":
                        fixture.Group.QueueFree();
                        break;
                }
                await Frames(tree, 60);
                TestAssert.True(probe.ActualBirths == 1 && probe.Completions == 0 && probe.Defeats == 0 &&
                                probe.Suppressions == 1 && CountArrivalWarnings(fixture.Actors) == 0 &&
                                CountEncounterActors(fixture.Actors) == 0 &&
                                !fixture.Progress.ActivatedMechanisms.Contains(stableId),
                    $"ordinary {change} during {(rewarning ? "rewarning" : "suppression")} " +
                    "cancels recovery with no delayed actor, warning, progress or defeat");
                if (GodotObject.IsInstanceValid(fixture.Group))
                    TestAssert.True(!fixture.Group.IsActive && !fixture.Group.IsComplete &&
                                    !fixture.Group.SpawnSuppressed && !fixture.Group.IsArrivalWarningActive,
                        "surviving retired owners clear suppression and warning state");
            }
            finally { await fixture.Dispose(tree); }
        }
        GD.Print("TASK14_SUPPRESSION_ORDINARY_LIFECYCLE_OK states=suppressed,rewarning " +
            "changes=reset,arena-exit,missing-player,death,owner-removal,queued-owner synchronous_callback=False");
    }

    private static async Task SuppressionAndRewarningFreezeWithPausedPhysics(SceneTree tree)
    {
        var (fixture, leftBlocker, rightBlocker) = CreateBlockedArrivalFixture(tree, "pause-recovery");
        using var probe = new ArrivalProbe(fixture);
        try
        {
            await BeginBlockedArrival(tree, fixture, probe, 715200UL);
            leftBlocker.QueueFree();
            rightBlocker.QueueFree();
            tree.Paused = true;
            await ProcessFrames(tree, 45);
            TestAssert.True(fixture.Group.SpawnSuppressed && !fixture.Group.IsArrivalWarningActive &&
                            fixture.Group.LiveActorCount == 0 && probe.WarningStarts.Count == 2 &&
                            probe.Suppressions == 1 && probe.ActualBirths == 1,
                "paused process frames cannot preflight newly clear sites or begin a recovery warning");
            tree.Paused = false;
            await AwaitRewarning(tree, fixture, probe, 4);
            await Frames(tree, 5);
            tree.Paused = true;
            await ProcessFrames(tree, 45);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && !fixture.Group.SpawnSuppressed &&
                            fixture.Group.LiveActorCount == 0 && probe.WarningStarts.Count == 4 &&
                            probe.WarningEnds.Count == 2 && probe.ActualBirths == 1,
                "paused process frames cannot consume the fresh warning or mount its actors");
            tree.Paused = false;
            await Frames(tree, 20);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 0,
                "resuming retains the unspent warning time instead of accruing paused time");
            await Frames(tree, 20);
            TestAssert.True(!fixture.Group.IsArrivalWarningActive && !fixture.Group.SpawnSuppressed &&
                            fixture.Group.LiveActorCount == 2 && probe.ActualBirths == 3 &&
                            probe.WarningStarts.Count == 4 && probe.Suppressions == 1 &&
                            probe.Completions == 0 && probe.Defeats == 0,
                "resumed physics completes one real whole-batch birth without duplicating recovery");
            GD.Print("TASK14_SUPPRESSION_PAUSE_OK suppressed_process=45 rewarning_process=45 " +
                "fresh_remaining_time=True atomic_births=2");
        }
        finally
        {
            tree.Paused = false;
            await fixture.Dispose(tree);
        }
    }

    private static (Fixture Fixture, StaticBody2D LeftBlocker, StaticBody2D RightBlocker)
        CreateBlockedArrivalFixture(SceneTree tree, string suffix, bool packedPlayer = false)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            $"canyon.v2.encounter.test-suppression-{suffix}", CanyonRouteNodeId.GearPistons, false,
            new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(-180, -6), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(-100, 1), 1),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(100, 1), 1)
            }), true), new Vector2(0, -6), packedPlayer);
        AddFloor(fixture.Root, new Vector2(0, 50), new Vector2(500, 40));
        return (fixture, AddArrivalBlocker(fixture.Root, new Vector2(-100, -4)),
            AddArrivalBlocker(fixture.Root, new Vector2(100, -4)));
    }

    private static StaticBody2D AddArrivalBlocker(Node parent, Vector2 position)
    {
        var blocker = new StaticBody2D { Name = "ArrivalNativeBlocker", Position = position,
            CollisionLayer = CombatCollisionLayers.World };
        blocker.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(60, 72) } });
        parent.AddChild(blocker);
        return blocker;
    }

    private static async Task BeginBlockedArrival(SceneTree tree, Fixture fixture, ArrivalProbe probe, ulong source)
    {
        await Frames(tree, 4);
        TestAssert.True(fixture.Group.IsActive && fixture.Group.CurrentBatch == 0 &&
                        fixture.Group.LiveActorCount == 1 && probe.ActualBirths == 1,
            "blocked recovery begins with one real initial actor");
        Kill(fixture.Actors.GetChild(0), source);
        await Frames(tree, 45);
        TestAssert.True(fixture.Group.IsActive && fixture.Group.CurrentBatch == 1 &&
                        fixture.Group.SpawnSuppressed && !fixture.Group.IsArrivalWarningActive &&
                        fixture.Group.LiveActorCount == 0 && probe.Suppressions == 1 &&
                        probe.WarningStarts.Count == 2 && probe.WarningEnds.Count == 2 &&
                        probe.ActualBirths == 1 && probe.Completions == 0 && probe.Defeats == 0,
            "genuine initial defeat and full warning end in one explicit native-blocked suppression");
    }

    private static async Task AwaitRewarning(SceneTree tree, Fixture fixture, ArrivalProbe probe, int warningCount)
    {
        for (int frame = 0; frame < 30 && !fixture.Group.IsArrivalWarningActive; frame++)
            await Frames(tree, 1);
        TestAssert.True(fixture.Group.IsArrivalWarningActive && !fixture.Group.SpawnSuppressed &&
                        fixture.Group.IsActive && fixture.Group.CurrentBatch == 1 &&
                        fixture.Group.LiveActorCount == 0 && probe.WarningStarts.Count == warningCount &&
                        probe.ActualBirths == 1 && probe.Completions == 0 && probe.Defeats == 0,
            "clearing actual native blockers must re-arm a fresh visible warning within 30 physics frames, " +
            "not birth actors, keep sticky suppression or change batch/progression");
    }

    private static int CountArrivalWarnings(Node actors)
    {
        int count = 0;
        for (int i = 0; i < actors.GetChildCount(); i++)
            if (actors.GetChild(i) is CanyonSpawnWarningVisual) count++;
        return count;
    }

    private static int CountEncounterActors(Node actors)
    {
        int count = 0;
        for (int i = 0; i < actors.GetChildCount(); i++)
            if (actors.GetChild(i) is IEncounterActor) count++;
        return count;
    }

    private sealed class ArrivalProbe : IDisposable
    {
        private readonly Fixture _fixture;
        private readonly List<CanyonSpawnWarningVisual> _warnings = new();
        public List<ulong> WarningStarts { get; } = new();
        public List<ulong> WarningEnds { get; } = new();
        public int Suppressions { get; private set; }
        public int ActualBirths { get; private set; }
        public int Completions { get; private set; }
        public int Defeats { get; private set; }

        public ArrivalProbe(Fixture fixture)
        {
            _fixture = fixture;
            fixture.Actors.ChildEnteredTree += OnChildEntered;
            fixture.Group.ArrivalSuppressed += OnSuppressed;
            fixture.Group.Completed += OnCompleted;
            fixture.Group.GroupDefeated += OnDefeated;
        }

        private void OnChildEntered(Node child)
        {
            if (child is IEncounterActor) ActualBirths++;
            if (child is not CanyonSpawnWarningVisual warning) return;
            _warnings.Add(warning);
            warning.Ready += OnWarningReady;
            warning.TreeExiting += OnWarningExit;
        }

        private void OnWarningReady() => WarningStarts.Add(Engine.GetPhysicsFrames());
        private void OnWarningExit() => WarningEnds.Add(Engine.GetPhysicsFrames());
        private void OnSuppressed(StringName _) => Suppressions++;
        private void OnCompleted(StringName _) => Completions++;
        private void OnDefeated(StringName _) => Defeats++;

        public void Dispose()
        {
            if (GodotObject.IsInstanceValid(_fixture.Actors))
                _fixture.Actors.ChildEnteredTree -= OnChildEntered;
            if (GodotObject.IsInstanceValid(_fixture.Group))
            {
                _fixture.Group.ArrivalSuppressed -= OnSuppressed;
                _fixture.Group.Completed -= OnCompleted;
                _fixture.Group.GroupDefeated -= OnDefeated;
            }
            foreach (CanyonSpawnWarningVisual warning in _warnings)
                if (GodotObject.IsInstanceValid(warning))
                {
                    warning.Ready -= OnWarningReady;
                    warning.TreeExiting -= OnWarningExit;
                }
            _warnings.Clear();
        }
    }

    private static async Task SplitFloorGapRejectsBridgedFootprint(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-split-floor-gap", CanyonRouteNodeId.GearPistons, false,
            new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(-160, -6), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(100, 1), 1,
                    new Vector2(-100, 1))
            })), Vector2.Zero);
        AddFloor(fixture.Root, new Vector2(-150, 50), new Vector2(200, 40));
        AddFloor(fixture.Root, new Vector2(70, 50), new Vector2(48, 40));
        AddFloor(fixture.Root, new Vector2(130, 50), new Vector2(48, 40));
        try
        {
            await Frames(tree, 4);
            Kill(fixture.Actors.GetChild(0), 714030UL);
            await Frames(tree, 42);
            TestAssert.True(fixture.Group.IsActive && !fixture.Group.SpawnSuppressed &&
                            fixture.Group.LiveActorCount == 1 && fixture.Actors.GetChildCount() == 1 &&
                            fixture.Actors.GetChild(0) is Knight.Enemies.Sentry.BellBoltSentry sentry &&
                            MathF.Abs(sentry.GlobalPosition.X + 100f) < 1f,
                "body spanning a 12px floor gap is rejected despite both former edge rays hitting; supported alternate spawns");
            GD.Print("TASK14_SPLIT_FLOOR_GAP_OK alternate=-100 gap=[94,106]");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task NearRestBoundaryRejectsBodyIntrusion(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-rest-edge", CanyonRouteNodeId.GearPistons, false,
            new Rect2(1080, 3600, 300, 200), new Rect2(1070, 3590, 330, 220),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(1100, 3700), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(1270, 3700), 1,
                    new Vector2(1200, 3700))
            })), new Vector2(1130, 3700));
        AddFloor(fixture.Root, new Vector2(1200, 3749), new Vector2(400, 40));
        try
        {
            await Frames(tree, 4);
            Kill(fixture.Actors.GetChild(0), 714031UL);
            await Frames(tree, 42);
            TestAssert.True(fixture.Group.IsActive && !fixture.Group.SpawnSuppressed &&
                            fixture.Group.LiveActorCount == 1 && fixture.Actors.GetChildCount() == 1 &&
                            fixture.Actors.GetChild(0) is Knight.Enemies.Sentry.BellBoltSentry sentry &&
                            MathF.Abs(sentry.GlobalPosition.X - 1200f) < 1f,
                "Sentry site center outside rest x=1280 but body right edge crossing it must be rejected in favor of safe alternate");
            GD.Print("TASK14_REST_BODY_EDGE_OK alternate=1200 primary_body=[1245,1295] rest_starts=1280");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task ArrivalCueCancelsOnLifecycleChange(SceneTree tree)
    {
        ulong source = 714010UL;
        foreach (string change in new[] { "reset", "exit", "missing-player", "sector-removal" })
        {
            Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
                $"canyon.v2.encounter.test-cue-{change}", CanyonRouteNodeId.GearPistons, false,
                new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
                Array.AsReadOnly(new[]
                {
                    new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, Vector2.Zero, 0),
                    new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(100, 1), 1,
                        new Vector2(-100, 1))
                })), Vector2.Zero);
            AddFloor(fixture.Root, new Vector2(0, 50), new Vector2(500, 40));
            try
            {
                await Frames(tree, 4);
                Kill(fixture.Actors.GetChild(0), source++);
                await Frames(tree, 2);
                TestAssert.True(fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 0,
                    $"{change} begins from a genuine first defeat and pending warning");
                switch (change)
                {
                    case "reset":
                        fixture.Player.GlobalPosition = new Vector2(900, 0);
                        fixture.Group.ResetForCheckpoint();
                        break;
                    case "exit":
                        fixture.Player.GlobalPosition = new Vector2(900, 0);
                        fixture.Group._PhysicsProcess(0);
                        break;
                    case "missing-player":
                        fixture.Player.RemoveFromGroup("player");
                        fixture.Group._PhysicsProcess(0);
                        break;
                    case "sector-removal":
                        fixture.Root.RemoveChild(fixture.Group);
                        fixture.Group.QueueFree();
                        break;
                }
                await Frames(tree, 45);
                TestAssert.True(fixture.Actors.GetChildren().All(child => child.Name != "ArrivalWarning") &&
                                fixture.Actors.GetChildren().All(child => child is not IEncounterActor),
                    $"{change} clears warning and prevents delayed actor birth");
                if (change != "sector-removal")
                    TestAssert.True(!fixture.Group.IsArrivalWarningActive && !fixture.Group.IsActive &&
                                    !fixture.Group.IsComplete,
                        $"{change} cancels without inventing encounter completion");
            }
            finally { await fixture.Dispose(tree); }
        }
        GD.Print("TASK14_ARRIVAL_LIFECYCLE_OK reset=True exit=True missing_player=True sector_removal=True");
    }

    private static async Task ArrivalCueFreezesWithPausedPhysics(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-pause-arrival", CanyonRouteNodeId.GearPistons, false,
            new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(-160, -6), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(100, 1), 1,
                    new Vector2(-100, 1))
            })), Vector2.Zero);
        AddFloor(fixture.Root, new Vector2(0, 50), new Vector2(500, 40));
        try
        {
            await Frames(tree, 4);
            Kill(fixture.Actors.GetChild(0), 714011UL);
            await Frames(tree, 10);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 0,
                "pause fixture begins with a real first death and partial warning countdown");
            tree.Paused = true;
            await ProcessFrames(tree, 90);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 0,
                "ninety paused process frames cannot advance the physics arrival countdown");
            tree.Paused = false;
            await Frames(tree, 12);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 0,
                "resuming preserves the warning's remaining physics time");
            await Frames(tree, 25);
            TestAssert.True(!fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 1 &&
                            fixture.Actors.GetChildren().OfType<Knight.Enemies.Sentry.BellBoltSentry>().Count() == 1,
                "one real Sentry arrives only after the remaining resumed physics time");
            await Frames(tree, 15);
            TestAssert.True(fixture.Group.LiveActorCount == 1,
                "pause/resume cannot duplicate second-wave birth");
            GD.Print("TASK14_ARRIVAL_PAUSE_OK paused_process_frames=90 one_birth=True");
        }
        finally
        {
            tree.Paused = false;
            await fixture.Dispose(tree);
        }
    }

    private static async Task PackedPlayerDeathCancelsArrivalWithoutDefeat(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-player-death-arrival", CanyonRouteNodeId.GearPistons, false,
            new Rect2(-220, -100, 440, 200), new Rect2(-250, -120, 500, 240),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(-160, -6), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.SentryScene, new Vector2(100, 1), 1,
                    new Vector2(-100, 1))
            })), Vector2.Zero, packedPlayer: true);
        AddFloor(fixture.Root, new Vector2(0, 50), new Vector2(500, 40));
        int defeated = 0;
        int completed = 0;
        fixture.Group.GroupDefeated += _ => defeated++;
        fixture.Group.Completed += _ => completed++;
        try
        {
            await Frames(tree, 4);
            Kill(fixture.Actors.GetChild(0), 714012UL);
            await Frames(tree, 2);
            TestAssert.True(fixture.Group.IsArrivalWarningActive && fixture.Group.LiveActorCount == 0,
                "packed player death fixture begins during real warning, before second actor birth");
            HealthComponent health = fixture.Player.GetNode<HealthComponent>("Health");
            Hurtbox hurtbox = fixture.Player.GetNode<Hurtbox>("Hurtbox");
            TestAssert.True(health.CurrentHealth > 0 &&
                            hurtbox.Receive(new DamageRequest(health.CurrentHealth,
                                Vector2.Right, 0f, 714013UL)) && health.CurrentHealth == 0,
                "actual packed Player Hurtbox routes fixture lethal damage through GuardedDamage and Health");
            await Frames(tree, 45);
            TestAssert.True(fixture.Player.IsInGroup("player") &&
                            !fixture.Group.IsArrivalWarningActive && !fixture.Group.IsActive &&
                            !fixture.Group.IsComplete && fixture.Group.LiveActorCount == 0 &&
                            fixture.Actors.GetChildCount() == 0 && defeated == 0 && completed == 0 &&
                            !fixture.Progress.ActivatedMechanisms.Contains(fixture.Group.EncounterId.ToString()),
                "dead packed Player cancels cue with no late actor, fabricated defeat or progress");
            fixture.Group.ResetForCheckpoint();
            health.ResetFull();
            fixture.Player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage").ResetFull();
            await Frames(tree, 4);
            TestAssert.True(health.CurrentHealth == health.MaxHealth && fixture.Group.IsActive &&
                            fixture.Group.CurrentBatch == 0 && fixture.Group.LiveActorCount == 1 &&
                            fixture.Actors.GetChildren().OfType<RustShellSquire>().Count() == 1,
                "checkpoint reset and packed Player health restore permit normal first-wave re-entry");
            fixture.Player.GlobalPosition = new Vector2(900, 0);
            fixture.Group._PhysicsProcess(0);
            TestAssert.True(!fixture.Group.IsActive && fixture.Group.LiveActorCount == 0,
                "post-reset arena departure retires the re-entered actor");
            GD.Print("TASK14_ARRIVAL_PACKED_PLAYER_DEATH_OK hurtbox=True late_birth=False reset_reentry=True");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task FrozenRealCapsuleOverhangRetainsOnlyUntilWholeBodyExits(SceneTree tree)
    {
        var wrapper = new Node2D { Name = "Task14FrozenRealCapsule" };
        tree.Root.AddChild(wrapper);
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonDescent].Position;
        wrapper.AddChild(sector);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = new Vector2(1400, 2144);
        wrapper.AddChild(player);
        player.AddToGroup("player");
        try
        {
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/FrozenUpperEncounter");
            await Frames(tree, 4);
            TestAssert.True(group.IsActive && group.LiveActorCount == 1,
                "the generated frozen upper arena activates with its real first actor");
            player.GlobalPosition = new Vector2(1277.69f, 2143.5f);
            player.Velocity = new Vector2(0, 25);
            player.MoveAndSlide();
            TestAssert.True(player.IsOnFloor(), "real player capsule still has supported floor contact");
            group._PhysicsProcess(0);
            TestAssert.True(group.IsActive && group.LiveActorCount == 1,
                "supported real player capsule overhanging the frozen left boundary retains the fight");
            player.GlobalPosition = new Vector2(1259, 2144);
            group._PhysicsProcess(0);
            TestAssert.True(!group.IsActive && group.LiveActorCount == 0,
                "whole capsule clear of the left boundary genuinely retires the encounter");
        }
        finally
        {
            player.RemoveFromGroup("player");
            wrapper.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedPairsRemainSupportedAndChooseAwayFromPlayer(SceneTree tree)
    {
        var cases = new[]
        {
            (PrologueSectorId.CanyonDescent, "CanyonDescent", "ControlledAbyssEncounter",
                new Vector2(2200, 3524), new Vector2(2450, 3524), new Vector2(2070, 3531), true,
                new Vector2(2450, 3531)),
            (PrologueSectorId.CanyonDescent, "CanyonDescent", "FrozenUpperEncounter",
                new Vector2(1400, 2050), new Vector2(1450, 2144), new Vector2(1315, 2151), false,
                new Vector2(1450, 2151)),
            (PrologueSectorId.CanyonBasin, "CanyonBasin", "SteamValvesEncounter",
                new Vector2(3300, 4190), new Vector2(3750, 4200), new Vector2(3250, 4207), false,
                new Vector2(3750, 4207)),
            (PrologueSectorId.CanyonDescent, "CanyonDescent", "ArchiveSideCaveEncounter",
                new Vector2(1510, 1604), new Vector2(1380, 1604), new Vector2(1600, 1641), false,
                new Vector2(1380, 1641)),
            (PrologueSectorId.CanyonAscent, "CanyonAscent", "PendulumGalleryEncounter",
                new Vector2(4500, 2660), new Vector2(4300, 2324), new Vector2(4460, 2361), false,
                new Vector2(4300, 2361))
        };
        ulong source = 714100UL;
        foreach (var item in cases)
        foreach ((Vector2 killPosition, Vector2 expected) in new[]
                 {
                     (item.Item5, item.Item6),
                     (new Vector2(item.Item6.X, item.Item5.Y), item.Item8)
                 }.Concat(item.Item3 is "ArchiveSideCaveEncounter" or "PendulumGalleryEncounter"
                     ? new[] { (new Vector2(item.Item3 == "ArchiveSideCaveEncounter" ? 1550f : 4380f,
                         item.Item5.Y), item.Item8) }
                     : Array.Empty<(Vector2, Vector2)>()))
        {
            var wrapper = new Node2D { Name = "Task14GeneratedPair_" + item.Item3 };
            tree.Root.AddChild(wrapper);
            PrologueSector sector = GD.Load<PackedScene>(
                $"res://scenes/prologue/sectors/{item.Item2}.tscn").Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[item.Item1].Position;
            wrapper.AddChild(sector);
            CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            player.GlobalPosition = item.Item4;
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
            wrapper.AddChild(player);
            player.AddToGroup("player");
            try
            {
                CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
                    "Gameplay/Encounters/" + item.Item3);
                Node actors = sector.GetNode("Gameplay/Actors");
                if (item.Item7)
                {
                    await Frames(tree, 1);
                    player.Velocity = new Vector2(0, 20);
                    player.MoveAndSlide();
                    TestAssert.True(player.IsOnFloor(), "abyss generated setup has actual safe-floor contact");
                }
                await Frames(tree, 4);
                TestAssert.True(group.IsActive && group.LiveActorCount == 1,
                    $"{item.Item3} generated group mounts its unchanged first actor");
                player.GlobalPosition = killPosition;
                Kill(actors.GetChild(0), source++);
                await Frames(tree, 2);
                TestAssert.True(group.IsArrivalWarningActive && group.LiveActorCount == 0,
                    $"{item.Item3} generated world shows two-site warning without an actor");
                await Frames(tree, 39);
                TestAssert.True(!group.SpawnSuppressed && group.IsActive && !group.IsComplete &&
                                group.LiveActorCount == 1 && actors.GetChildCount() == 1 &&
                                actors.GetChild(0) is Node2D actor &&
                                actor.GlobalPosition.DistanceTo(expected) < 3f,
                    $"{item.Item3} generated floor selects the distant site {expected} from {killPosition} without suppression");
                if (item.Item3 == "PendulumGalleryEncounter" && killPosition.X != 4380f)
                {
                    CanyonSkitter skitter = actors.GetChildren().OfType<CanyonSkitter>().Single();
                    player.GlobalPosition = new Vector2(4380, 2324);
                    bool rush = false;
                    bool retreat = false;
                    float left = skitter.GlobalPosition.X;
                    float right = left;
                    for (int frame = 0; frame < 150 && !retreat; frame++)
                    {
                        await Frames(tree, 1);
                        rush |= skitter.CurrentFrame.State == CanyonSkitterState.Rush;
                        retreat |= skitter.CurrentFrame.State == CanyonSkitterState.Retreat;
                        left = MathF.Min(left, skitter.GlobalPosition.X);
                        right = MathF.Max(right, skitter.GlobalPosition.X);
                    }
                    TestAssert.True(rush && retreat && left >= 4274f && right <= 4486f &&
                                    skitter.GlobalPosition.Y < 2390f,
                        $"Grip5 generated Skitter rushes and retreats from {expected} toward both facing directions without leaving supported shelf, observed=[{left},{right}]");
                    GD.Print($"TASK14_GRIP5_MOTION_OK spawn={expected} observed=[{left},{right}] packed_player_layer={player.CollisionLayer}");
                }
                if (item.Item3 == "ArchiveSideCaveEncounter" && killPosition.X == 1380f)
                {
                    var recorder = new ProloguePlaytestRecorder { Name = "Task14Recorder" };
                    wrapper.AddChild(recorder);
                    var session = new ProloguePlaytestSession("task14-archive-chain",
                        "2026-09-23T00:00:00Z", "AutomatedFixture");
                    recorder.ConfigureOrderedTelemetryForTest(session, wrapper);
                    int defeatedSignals = 0;
                    group.GroupDefeated += stableId =>
                    {
                        if (stableId == group.EncounterId) defeatedSignals++;
                    };
                    Kill(actors.GetChild(0), source++);
                    await Frames(tree, 3);
                    ProloguePlaytestReport report = session.BuildReport(
                        PrologueSectorId.CanyonDescent, PrologueObjective.ReachBasin,
                        "2026-09-23T00:00:01Z");
                    TestAssert.True(group.IsComplete && defeatedSignals == 1 &&
                                    report.Encounters.Count(sample =>
                                        sample.EncounterId == group.EncounterId.ToString() &&
                                        sample.Event == "Defeated") == 1,
                        "actual new Skitter death drives group stable-ID defeat into recorder telemetry");
                }
                if (!group.IsComplete)
                {
                    Kill(actors.GetChild(0), source++);
                    await Frames(tree, 3);
                }
                if (item.Item3 is "ControlledAbyssEncounter" or "SteamValvesEncounter" or "PendulumGalleryEncounter")
                {
                    TestAssert.True(!group.IsComplete && group.CurrentBatch == 2 &&
                                    group.IsArrivalWarningActive && group.LiveActorCount == 0,
                        $"{item.Item3} old second actor defeat preserves the added real weapon batch and its warning");
                    await Frames(tree, 40);
                    TestAssert.True(group.LiveActorCount == 1 &&
                                    actors.GetChild(0) is Knight.Enemies.Ranged.RangedWeaponEnemy,
                        $"{item.Item3} mounts one actual new saved weapon after its own visible deadline");
                    Kill(actors.GetChild(0), source++);
                    await Frames(tree, 3);
                }
                TestAssert.True(group.IsComplete && !group.IsActive && group.LiveActorCount == 0,
                    $"{item.Item3} final real actor death completes and retires cleanly");
                player.GlobalPosition = new Vector2(99999, 99999);
                group.ResetForCheckpoint();
                await Frames(tree, 2);
                TestAssert.True(!group.IsComplete && !group.IsActive && group.LiveActorCount == 0,
                    $"{item.Item3} checkpoint reset clears non-gating completion");
                player.GlobalPosition = item.Item4;
                await Frames(tree, 3);
                TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1,
                    $"{item.Item3} re-entry starts with the original first actor");
                player.GlobalPosition = new Vector2(99999, 99999);
                group._PhysicsProcess(0);
                TestAssert.True(!group.IsActive && group.LiveActorCount == 0,
                    $"{item.Item3} departure retires the re-entered actor");
            }
            finally
            {
                player.RemoveFromGroup("player");
                wrapper.QueueFree();
                await Frames(tree, 2);
            }
        }
    }

    public static async Task RunTask6PhysicsContactBatch(SceneTree tree)
    {
        var root = new Node2D { Name = "Task6PhysicsContactBatchFixture" };
        var actors = new Node2D { Name = "Actors" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        var group = new CanyonEncounterGroup
        {
            Name = "Encounter", EncounterId = "canyon.v2.encounter.test-contact-batch",
            Definition = new CanyonEncounterDefinition(
                "canyon.v2.encounter.test-contact-batch", CanyonRouteNodeId.GearPistons, false,
                new Rect2(-100, -100, 200, 200), new Rect2(-1000, -1000, 2000, 2000),
                Array.AsReadOnly(new[]
                {
                    new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, Vector2.Zero, 0),
                    new CanyonEnemyPlacement(CanyonEncounterCatalog.LaborerScene, new Vector2(100, 0), 1)
                })),
            ActorRootPath = new NodePath("../Actors")
        };
        root.AddChild(player);
        player.AddToGroup("player");
        root.AddChild(actors);
        root.AddChild(group);
        tree.Root.AddChild(root);
        Hitbox playerHitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
        try
        {
            await Frames(tree, 3);
            RustShellSquire first = actors.GetChildren().OfType<RustShellSquire>().Single();
            first.SetPhysicsProcess(false);
            HealthComponent health = first.GetNode<HealthComponent>(first.HealthPath);
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth - 1,
                    Vector2.Zero, 0f, 620001UL)),
                "contact fixture leaves the first real batch actor at one health");
            await Frames(tree, 30);
            first.GlobalPosition = player.GlobalPosition;
            int connected = 0;
            bool mountedDuringContactCallback = false;
            playerHitbox.Connected += _ =>
            {
                connected++;
                mountedDuringContactCallback |= group.LiveActorCount > 0;
            };
            playerHitbox.Begin(620002UL);
            await Frames(tree, 3);
            playerHitbox.End();

            TestAssert.True(connected == 1 && !mountedDuringContactCallback &&
                            group.IsActive && group.CurrentBatch == 1 &&
                            group.LiveActorCount == 1 &&
                            actors.GetChildren().OfType<BellBackLaborer>().Count() == 1,
                "actual player Hitbox.AreaEntered contact defers the next batch past query flush, then mounts it safely");
        }
        finally
        {
            playerHitbox.End();
            player.RemoveFromGroup("player");
            root.QueueFree();
            await Frames(tree, 2);
        }
        await AssertPendingBatchCancelledByLifecycle(tree, reset: true);
        await AssertPendingBatchCancelledByLifecycle(tree, reset: false);
        GD.Print("TASK6_PLAYER_HITBOX_BATCH_TRANSITION_OK");
    }

    private static async Task AssertPendingBatchCancelledByLifecycle(SceneTree tree, bool reset)
    {
        var root = new Node2D { Name = reset ? "Task6PendingResetFixture" : "Task6PendingRetireFixture" };
        var actors = new Node2D { Name = "Actors" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        var group = new CanyonEncounterGroup
        {
            Name = "Encounter", EncounterId = "canyon.v2.encounter.test-contact-cancel",
            Definition = new CanyonEncounterDefinition(
                "canyon.v2.encounter.test-contact-cancel", CanyonRouteNodeId.GearPistons, false,
                new Rect2(-100, -100, 200, 200), new Rect2(-1000, -1000, 2000, 2000),
                Array.AsReadOnly(new[]
                {
                    new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, Vector2.Zero, 0),
                    new CanyonEnemyPlacement(CanyonEncounterCatalog.LaborerScene, new Vector2(100, 0), 1)
                })),
            ActorRootPath = new NodePath("../Actors")
        };
        root.AddChild(player);
        player.AddToGroup("player");
        root.AddChild(actors);
        root.AddChild(group);
        tree.Root.AddChild(root);
        Hitbox playerHitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
        try
        {
            await Frames(tree, 3);
            RustShellSquire first = actors.GetChildren().OfType<RustShellSquire>().Single();
            first.SetPhysicsProcess(false);
            HealthComponent health = first.GetNode<HealthComponent>(first.HealthPath);
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth - 1,
                    Vector2.Zero, 0f, reset ? 620003UL : 620005UL)),
                "lifecycle fixture leaves the first real batch actor at one health");
            await Frames(tree, 30);
            playerHitbox.Begin(reset ? 620004UL : 620006UL);
            TestAssert.True(playerHitbox.TryConnect(first.GetNode<Hurtbox>("Hurtbox")),
                "actual player hitbox synchronously defeats the final actor before lifecycle cancellation");
            playerHitbox.End();
            TestAssert.True(group.CurrentBatch == 1 && group.LiveActorCount == 0,
                "next batch is pending and not mounted synchronously");

            if (reset)
                group.ResetForCheckpoint();
            else
            {
                player.GlobalPosition = new Vector2(2200, 0);
                group._PhysicsProcess(0d);
            }
            await Frames(tree, 3);
            TestAssert.True(!group.IsActive && group.CurrentBatch == -1 &&
                            group.LiveActorCount == 0 &&
                            actors.GetChildren().OfType<IEncounterActor>().Count() == 0,
                $"{(reset ? "checkpoint reset" : "arena retirement")} cancels a pending deferred batch without resurrection");
        }
        finally
        {
            playerHitbox.End();
            player.RemoveFromGroup("player");
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    public static async Task RunTask6MainRouteActivation(SceneTree tree)
    {
        await AssertMainRouteGrappleActivates(tree, "PendulumGalleryEncounter", "AscentGrip3",
            "AscentGrip4", new Vector2(4464.0845f, 2803.995f));
        await AssertMainRouteGrappleActivates(tree, "SplitLevelEncounter", "AscentGrip6",
            "AscentGrip7", new Vector2(4695.9155f, 2083.9963f));
        GD.Print("TASK6_MAIN_ROUTE_ACTIVATION_OK main6=True main7=True input_grapple=True");
    }

    private static async Task AssertMainRouteGrappleActivates(SceneTree tree, string groupName,
        string startSupportName, string targetSupportName, Vector2 startPosition)
    {
        var root = new Node2D { Name = "Task6MainRouteActivation_" + groupName };
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = startPosition;
        player.GetNode<Camera2D>("Camera2D").Enabled = true;
        tree.Root.AddChild(root);
        root.AddChild(sector);
        root.AddChild(player);
        player.AddToGroup("player");
        try
        {
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/" + groupName);
            StaticBody2D startSupport = sector.GetNode<StaticBody2D>(
                "Gameplay/Collision/" + startSupportName);
            StaticBody2D targetSupport = sector.GetNode<StaticBody2D>(
                "Gameplay/Collision/" + targetSupportName);
            StaticBody2D grip = sector.GetNode<StaticBody2D>(
                "Gameplay/Collision/" + targetSupportName + "Grip");
            await Frames(tree, 4);
            TestAssert.True(ReferenceEquals(player, tree.GetFirstNodeInGroup("player")) &&
                            group.IsPhysicsProcessing(),
                $"{groupName} observes this real player through its enabled physics loop");
            TestAssert.True(!group.IsActive && !group.IsComplete && group.LiveActorCount == 0 &&
                            player.GlobalPosition.DistanceTo(startSupport.GlobalPosition) < 180f,
                $"{groupName} retains an observation span on the preceding ordinary main-route support");

            SendAction(player, InputActions.SelectGrappleMode, true);
            await Frames(tree, 1);
            SendAction(player, InputActions.SelectGrappleMode, false);
            await Frames(tree, 1);
            TestAssert.Equal(PlayerActionMode.Grapple,
                player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode,
                "ordinary ascent input selects grapple mode before route traversal");
            var gripShape = (RectangleShape2D)grip.GetNode<CollisionShape2D>("Collision").Shape;
            Vector2 aim = new(grip.GlobalPosition.X,
                grip.GlobalPosition.Y - gripShape.Size.Y / 2f + 10f);
            SetPointer(player, aim);
            GrappleSurface2D expected = grip.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            TestAssert.Equal(expected, new MechanicalClawTargetResolver().Resolve(player, aim).Surface,
                $"{targetSupportName} ordinary route input resolves its unchanged physical grip");
            SendMouse(player, MouseButton.Left, true, aim);
            await Frames(tree, 1);
            SendMouse(player, MouseButton.Left, false, aim);
            Vector2 minimum = player.GlobalPosition;
            Vector2 maximum = player.GlobalPosition;
            bool crossedActivation = false;
            MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            bool attached = false;
            for (int frame = 0; frame < 300 && !group.IsActive; frame++)
            {
                await Frames(tree, 1);
                minimum = new Vector2(Mathf.Min(minimum.X, player.GlobalPosition.X),
                    Mathf.Min(minimum.Y, player.GlobalPosition.Y));
                maximum = new Vector2(Mathf.Max(maximum.X, player.GlobalPosition.X),
                    Mathf.Max(maximum.Y, player.GlobalPosition.Y));
                crossedActivation |= group.Definition.ActivationBounds.HasPoint(player.GlobalPosition);
                attached |= claw.CurrentFrame.State is MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Latched;
            }
            if (crossedActivation && !group.IsActive)
                await tree.ToSignal(tree.CreateTimer(.05d, true, true),
                    SceneTreeTimer.SignalName.Timeout);

            TestAssert.True(group.IsActive && group.LiveActorCount == 1 && !group.IsComplete &&
                            crossedActivation && attached &&
                            targetSupport.GlobalPosition.Y < startSupport.GlobalPosition.Y,
                $"ordinary unforced {startSupportName}->{targetSupportName} grapple enters {groupName} activation " +
                $"(position={player.GlobalPosition}, sampled={minimum}..{maximum}, " +
                $"activation={group.Definition.ActivationBounds}, crossed={crossedActivation}, " +
                $"attached={attached}, claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure}, " +
                $"target={claw.CurrentTarget?.GetParent()?.Name})");
        }
        finally
        {
            ReleaseInput();
            player.RemoveFromGroup("player");
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedFirstCombatGuidanceUsesLiveEncounterActor(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = new Vector2(160, 600);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "task14-first-combat-guidance");
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 5);
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            CanyonEncounterGroup group = level.GetNode<CanyonEncounterGroup>(
                "WorldRoot/Sectors/CanyonEntrance/Gameplay/Encounters/LowCeilingBridgeEncounter");
            Node actors = level.GetNode(
                "WorldRoot/Sectors/CanyonEntrance/Gameplay/Actors");
            Node marker = actors.GetNode("RustShellSquire");
            TestAssert.True(marker is Marker2D && marker is not RustShellSquire &&
                            (bool)marker.GetMeta("guidance_only", false),
                "layout-2 retains an inert static guidance marker without fake enemy health or AI");

            group.SetPhysicsProcess(false);
            player.GlobalPosition = new Vector2(500, 520);
            await Pulse(tree, InputActions.SelectAttackMode);
            guidance.RefreshNow();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!HasCombatCue(guidance) && group.LiveActorCount == 0,
                "the inactive authored marker alone never triggers the first-combat cue");

            group.SetPhysicsProcess(true);
            await Frames(tree, 3);
            RustShellSquire live = actors.GetChildren().OfType<RustShellSquire>().Single();
            live.GlobalPosition = player.GlobalPosition + new Vector2(120, 0);
            await SettleGuidance(tree, guidance);
            TestAssert.True(group.IsActive && group.LiveActorCount == 1 &&
                            live.GetNode<HealthComponent>(live.HealthPath).CurrentHealth > 0 &&
                            live.HasLineOfSightTo(player) && HasCombatCue(guidance),
                "a genuine nearby generated Rust-Shell in Attack mode triggers the combat cue");

            Kill(live, 610007UL);
            guidance.RefreshNow();
            TestAssert.True(!HasCombatCue(guidance),
                "a genuinely defeated encounter actor immediately withdraws the combat cue");
            await Frames(tree, 2);

            player.GlobalPosition = new Vector2(160, 600);
            group.ResetForCheckpoint();
            await Frames(tree, 2);
            player.GlobalPosition = new Vector2(500, 520);
            await Frames(tree, 3);
            RustShellSquire retiring = actors.GetChildren().OfType<RustShellSquire>().Single();
            retiring.GlobalPosition = player.GlobalPosition + new Vector2(120, 0);
            await SettleGuidance(tree, guidance);
            TestAssert.True(HasCombatCue(guidance),
                "checkpoint remount reacquires the genuine first encounter actor");
            player.GlobalPosition = new Vector2(800, 520);
            await Frames(tree, 2);
            await SettleGuidance(tree, guidance);
            TestAssert.True(!group.IsActive && group.LiveActorCount == 0 && !HasCombatCue(guidance),
                "live retirement removes the combat cue without treating the marker as an enemy");

            PrologueSector descent = level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent");
            descent.Visible = true;
            Node descentActors = descent.GetNode("Gameplay/Actors");
            RustShellSquire otherSector = GD.Load<PackedScene>(CanyonEncounterCatalog.RustShellScene)
                .Instantiate<RustShellSquire>();
            descentActors.AddChild(otherSector);
            otherSector.GlobalPosition = player.GlobalPosition + new Vector2(120, 0);
            otherSector.SetProcess(false);
            otherSector.SetPhysicsProcess(false);
            guidance.RefreshNow();
            TestAssert.True(otherSector.IsVisibleInTree() &&
                            otherSector.GetNode<HealthComponent>(otherSector.HealthPath).CurrentHealth > 0 &&
                            !HasCombatCue(guidance),
                "a real living Rust-Shell in another sector cannot trigger Entrance combat guidance");
        }
        finally
        {
            Input.ActionRelease(InputActions.SelectAttackMode);
            Input.ActionRelease(InputActions.SelectGrappleMode);
            level.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task RealDefeatsAdvanceBatchesAndCompleteOnce(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-progression", CanyonRouteNodeId.SplitLevelEncounter, false,
            new Rect2(-100, -100, 200, 200), new Rect2(-200, -200, 400, 400),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(40, 0), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.MothScene, new Vector2(120, -40), 1)
            }), true));
        try
        {
            int defeatedEvents = 0;
            fixture.Group.GroupDefeated += id =>
            {
                TestAssert.Equal("canyon.v2.encounter.test-progression", id.ToString(),
                    "GroupDefeated publishes the stable ID");
                defeatedEvents++;
            };
            await Frames(tree, 3);
            TestAssert.True(fixture.Group.IsActive && fixture.Group.CurrentBatch == 0 &&
                            fixture.Group.LiveActorCount == 1,
                "activation bounds mount only the first authored batch");
            Node first = fixture.Actors.GetChild(0);
            Kill(first, 610001UL);
            await Frames(tree, 2);
            TestAssert.True(!GodotObject.IsInstanceValid(first) || first.GetParent() == null,
                "defeated batch actor retires before the next batch");
            TestAssert.True(fixture.Group.IsActive && fixture.Group.CurrentBatch == 1 &&
                            fixture.Group.LiveActorCount == 1 && !fixture.Group.IsComplete,
                "a genuine first-batch defeat mounts the second batch without completing early");
            Node second = fixture.Actors.GetChild(0);
            Kill(second, 610002UL);
            await Frames(tree, 2);
            TestAssert.True(fixture.Group.IsComplete && !fixture.Group.IsActive &&
                            fixture.Group.LiveActorCount == 0 && defeatedEvents == 1,
                "all genuine defeats publish GroupDefeated exactly once");
            TestAssert.True(fixture.Progress.ActivatedMechanisms.Contains(
                    "canyon.v2.encounter.test-progression"),
                "only a progression encounter writes its stable completion seam");
            fixture.Group.Complete();
            TestAssert.Equal(1, defeatedEvents,
                "legacy Complete fixture cannot replay genuine GroupDefeated");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task RetirementAndResetNeverFabricateDefeats(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-ordinary", CanyonRouteNodeId.SealedCourtyard, false,
            new Rect2(-100, -100, 200, 200), new Rect2(-180, -180, 360, 360),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene, new Vector2(40, 0), 0),
                new CanyonEnemyPlacement(CanyonEncounterCatalog.LaborerScene, new Vector2(100, 0), 1)
            })));
        try
        {
            int defeatedEvents = 0;
            fixture.Group.GroupDefeated += _ => defeatedEvents++;
            await Frames(tree, 3);
            Node retiredAlive = fixture.Actors.GetChild(0);
            fixture.Player.GlobalPosition = new Vector2(500, 0);
            await Frames(tree, 2);
            TestAssert.True(!fixture.Group.IsActive && !fixture.Group.IsComplete &&
                            fixture.Group.LiveActorCount == 0 && defeatedEvents == 0,
                "leaving an arena retires a living actor without counting a kill; " +
                $"active={fixture.Group.IsActive} complete={fixture.Group.IsComplete} " +
                $"live={fixture.Group.LiveActorCount} events={defeatedEvents} " +
                $"player={fixture.Player.GlobalPosition}");
            TestAssert.True(!fixture.Progress.ActivatedMechanisms.Contains(fixture.Group.EncounterId.ToString()),
                "ordinary encounter retirement never persists a completion");
            fixture.Player.GlobalPosition = Vector2.Zero;
            await Frames(tree, 3);
            TestAssert.True(fixture.Group.IsActive && fixture.Group.CurrentBatch == 0 &&
                            fixture.Group.LiveActorCount == 1,
                "re-entering after live retirement remounts the first batch");
            fixture.Group.ResetForCheckpoint();
            await Frames(tree, 2);
            if (GodotObject.IsInstanceValid(retiredAlive) && retiredAlive.GetParent() != null)
                Kill(retiredAlive, 610003UL);
            TestAssert.True(!fixture.Group.IsComplete && defeatedEvents == 0,
                "reset detaches prior subscriptions and never converts teardown into defeat");
        }
        finally { await fixture.Dispose(tree); }
    }

    private static async Task ControlledAbyssWaitsForActualSafeLanding(SceneTree tree)
    {
        Fixture fixture = CreateFixture(tree, new CanyonEncounterDefinition(
            "canyon.v2.encounter.test-abyss", CanyonRouteNodeId.ControlledAbyss, false,
            new Rect2(2080, 3440, 360, 150), new Rect2(1960, 3240, 560, 350),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.MothScene, new Vector2(2280, 3420), 0)
            })), new Vector2(2200, 3450));
        StaticBody2D floor = AddFloor(fixture.Root, new Vector2(2240, 3580), new Vector2(640, 40));
        try
        {
            await Frames(tree, 3);
            TestAssert.True(!fixture.Player.IsOnFloor() && !fixture.Group.IsActive,
                "Main3 stays dormant during the protected incoming fall even inside its rectangle");
            for (int frame = 0; frame < 30 && !fixture.Player.IsOnFloor(); frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                fixture.Player.Velocity = new Vector2(0, 420);
                fixture.Player.MoveAndSlide();
            }
            await Frames(tree, 3);
            TestAssert.True(fixture.Player.IsOnFloor() && fixture.Group.IsActive &&
                            fixture.Group.LiveActorCount == 1,
                "Main3 activates only after actual CharacterBody safe-bottom contact");
        }
        finally
        {
            floor.QueueFree();
            await fixture.Dispose(tree);
        }
    }

    private static async Task GeneratedCourtyardRetiresLiveEnemyBeforeRest(SceneTree tree)
    {
        var wrapper = new Node2D { Name = "GeneratedCourtyardEncounterFixture" };
        tree.Root.AddChild(wrapper);
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/LostTemple.tscn").Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.LostTemple].Position;
        wrapper.AddChild(sector);
        var player = PlayerAt(new Vector2(5600, 1120));
        wrapper.AddChild(player);
        player.AddToGroup("player");
        try
        {
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/SealedCourtyardEncounter");
            int defeatedEvents = 0;
            group.GroupDefeated += _ => defeatedEvents++;
            await Frames(tree, 8);
            TestAssert.True(group.IsActive && group.LiveActorCount == 1,
                "generated Main8 mounts an actual live Rust-Shell inside the unlocked courtyard");
            player.GlobalPosition = new Vector2(5920, 1080);
            await Frames(tree, 3);
            TestAssert.True(!group.IsActive && !group.IsComplete && group.LiveActorCount == 0 &&
                            defeatedEvents == 0,
                "actual live Main8 retires before the pre-Boss checkpoint/rest without a fabricated defeat");
        }
        finally
        {
            player.RemoveFromGroup("player");
            wrapper.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedMain7DefeatSuppliesExactSealInput(SceneTree tree)
    {
        var wrapper = new Node2D { Name = "GeneratedMain7SealFixture" };
        tree.Root.AddChild(wrapper);
        PrologueSector ascent = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
        ascent.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
        wrapper.AddChild(ascent);
        PrologueSector temple = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/LostTemple.tscn").Instantiate<PrologueSector>();
        temple.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.LostTemple].Position;
        wrapper.AddChild(temple);
        var player = PlayerAt(new Vector2(4500, 1800));
        wrapper.AddChild(player);
        player.AddToGroup("player");
        var progress = new PrologueProgress();
        try
        {
            CanyonEncounterGroup main7 = ascent.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/SplitLevelEncounter");
            CanyonSteamValve crank = ascent.GetNode<CanyonSteamValve>(
                "Gameplay/Mechanisms/CourtyardSealLinkage");
            CanyonSealGate seal = temple.GetNode<CanyonSealGate>(
                "Gameplay/Mechanisms/CourtyardSeal");
            main7.ApplyProgress(progress);
            crank.ApplyProgress(progress);
            seal.ApplyProgress(progress);
            await Frames(tree, 4);
            Node actors = ascent.GetNode("Gameplay/Actors");
            TestAssert.True(main7.CurrentBatch == 0 && actors.GetChildCount() == 1,
                "generated Main7 begins with one actual Laborer batch");
            Kill(actors.GetChild(0), 610005UL);
            await Frames(tree, 3);
            TestAssert.True(main7.CurrentBatch == 1 && actors.GetChildCount() == 1,
                "generated Main7 genuine Laborer defeat advances to the Moth lane");
            Kill(actors.GetChild(0), 610006UL);
            await Frames(tree, 3);
            TestAssert.True(main7.IsComplete && progress.ActivatedMechanisms.Contains(
                    "canyon.v2.encounter.split-level") && !seal.IsOpen &&
                    !progress.ActivatedMechanisms.Contains(CanyonWorldLayout.CourtyardSealMechanismId),
                "genuine Main7 defeat supplies only its exact prerequisite, never opens the seal alone");
            TestAssert.True(crank.TryReceiveThread(default),
                "existing generated crank accepts its unchanged interaction contract");
            await Frames(tree, 3);
            TestAssert.True(seal.IsOpen && progress.ActivatedMechanisms.Contains(
                    CanyonWorldLayout.CourtyardSealMechanismId),
                "genuine Main7 plus the existing crank opens the exact two-condition seal");
        }
        finally
        {
            player.RemoveFromGroup("player");
            wrapper.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedAdjacentGroupsRespectEffectiveRangeCap(SceneTree tree)
    {
        var wrapper = new Node2D { Name = "GeneratedBasinAdjacentEncounterFixture" };
        tree.Root.AddChild(wrapper);
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin].Position;
        wrapper.AddChild(sector);
        var player = PlayerAt(new Vector2(2300, 3960));
        wrapper.AddChild(player);
        player.AddToGroup("player");
        try
        {
            CanyonEncounterGroup gears = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/GearPistonsEncounter");
            CanyonEncounterGroup steam = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/SteamValvesEncounter");
            Node actors = sector.GetNode("Gameplay/Actors");
            await Frames(tree, 5);
            TestAssert.True(gears.IsActive && gears.LiveActorCount == 1 &&
                            !steam.IsActive && actors.GetChildCount() == 1,
                "GearPistons mounts only its first actual batch");
            Kill(actors.GetChild(0), 610004UL);
            await Frames(tree, 3);
            TestAssert.True(gears.CurrentBatch == 1 && gears.LiveActorCount == 1 &&
                            actors.GetChildCount() == 1,
                "second GearPistons height replaces rather than piles onto the first batch");
            player.GlobalPosition = new Vector2(3300, 4190);
            await Frames(tree, 4);
            TestAssert.True(!gears.IsActive && steam.IsActive &&
                            gears.LiveActorCount + steam.LiveActorCount <= 2 &&
                            actors.GetChildCount() <= 2,
                "adjacent Gear/Steam groups retire the horizontal-only Laborer before mounting the next arena");
        }
        finally
        {
            player.RemoveFromGroup("player");
            wrapper.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedMain5PreservesObservationThenEngages(SceneTree tree)
    {
        var wrapper = new Node2D { Name = "GeneratedMain5ObservationFixture" };
        tree.Root.AddChild(wrapper);
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
        wrapper.AddChild(sector);
        var player = PlayerAt(new Vector2(4100, 3500));
        wrapper.AddChild(player);
        player.AddToGroup("player");
        try
        {
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/CounterweightLiftEncounter");
            Node actors = sector.GetNode("Gameplay/Actors");
            await Frames(tree, 4);
            GraveLampMoth moth = actors.GetChildren().OfType<GraveLampMoth>().Single();
            TestAssert.True(group.IsActive && group.LiveActorCount == 1 &&
                            moth.CurrentState == GraveLampMothState.Hover,
                "Main5 mounts its Moth while preserving the initial grapple observation span");

            player.GlobalPosition = new Vector2(4700, 3300);
            await Frames(tree, 3);
            TestAssert.True(moth.CurrentState != GraveLampMothState.Hover,
                "Main5 Moth genuinely engages as the player progresses up the lift arena");
        }
        finally
        {
            player.RemoveFromGroup("player");
            wrapper.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedFrozenArenasPreserveNoncombatBypass(SceneTree tree)
    {
        var wrapper = new Node2D { Name = "GeneratedFrozenEncounterFixture" };
        tree.Root.AddChild(wrapper);
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
        sector.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonDescent].Position;
        wrapper.AddChild(sector);
        var player = PlayerAt(new Vector2(1400, 2050));
        wrapper.AddChild(player);
        player.AddToGroup("player");
        try
        {
            CanyonEncounterGroup upper = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/FrozenUpperEncounter");
            CanyonEncounterGroup lower = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/FrozenLowerEncounter");
            Node actors = sector.GetNode("Gameplay/Actors");
            await Frames(tree, 4);
            TestAssert.True(upper.IsActive && !lower.IsActive && actors.GetChildCount() == 1,
                "upper Frozen safe-stone island mounts only its actual Moth");
            player.GlobalPosition = new Vector2(1440, 3200);
            await Frames(tree, 4);
            TestAssert.True(!upper.IsActive && lower.IsActive &&
                            upper.LiveActorCount + lower.LiveActorCount == 1 &&
                            actors.GetChildCount() == 1,
                "lower Frozen safe-stone arena is separate and mounts only its first batch");

            player.GlobalPosition = new Vector2(1900, 3400);
            lower.ResetForCheckpoint();
            await Frames(tree, 2);
            player.GlobalPosition = new Vector2(1740, 3208);
            await Frames(tree, 4);
            TestAssert.True(!lower.IsActive && lower.LiveActorCount == 0,
                "the right-side Frozen return launch remains a genuine noncombat bypass");
        }
        finally
        {
            player.RemoveFromGroup("player");
            wrapper.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static Fixture CreateFixture(SceneTree tree, CanyonEncounterDefinition definition,
        Vector2? playerPosition = null, bool packedPlayer = false)
    {
        var root = new Node2D { Name = "Task6EncounterFixture" };
        var actors = new Node2D { Name = "Actors" };
        CharacterBody2D player = packedPlayer
            ? GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>()
            : PlayerAt(playerPosition ?? Vector2.Zero);
        if (packedPlayer)
        {
            player.GlobalPosition = playerPosition ?? Vector2.Zero;
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        }
        var group = new CanyonEncounterGroup
        {
            Name = "Encounter", EncounterId = definition.StableId,
            Definition = definition, ActorRootPath = new NodePath("../Actors")
        };
        var progress = new PrologueProgress();
        root.AddChild(player);
        player.AddToGroup("player");
        root.AddChild(actors);
        root.AddChild(group);
        group.ApplyProgress(progress);
        tree.Root.AddChild(root);
        return new Fixture(root, actors, player, group, progress);
    }

    private static CharacterBody2D PlayerAt(Vector2 position)
    {
        var player = new CharacterBody2D { Name = "Player", GlobalPosition = position };
        player.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CapsuleShape2D { Radius = 16, Height = 72 }
        });
        return player;
    }

    private static StaticBody2D AddFloor(Node parent, Vector2 center, Vector2 size)
    {
        var floor = new StaticBody2D { Name = "AbyssSafeBottom", Position = center,
            CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        parent.AddChild(floor);
        return floor;
    }

    private static void Kill(Node actor, ulong sourceId)
    {
        HealthComponent health = actor.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,
                Vector2.Right, 0f, sourceId)),
            "test damage kills the actual encounter actor through HealthComponent");
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task ProcessFrames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task Pulse(SceneTree tree, StringName action)
    {
        Input.ActionPress(action);
        await Frames(tree, 2);
        Input.ActionRelease(action);
        await Frames(tree, 2);
    }

    private static async Task SettleGuidance(SceneTree tree, PrologueGuidanceController guidance)
    {
        await Frames(tree, 2);
        guidance.RefreshNow();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static bool HasCombatCue(PrologueGuidanceController guidance) =>
        guidance.CurrentInstruction.Visible &&
        guidance.CurrentInstruction.StepText.Contains("RMB", StringComparison.Ordinal) &&
        guidance.CurrentInstruction.StepText.Contains("LMB", StringComparison.Ordinal) &&
        !string.IsNullOrEmpty(guidance.CurrentInstruction.ContextTitle);

    private static void SetPointer(Node2D canvas, Vector2 worldPosition)
    {
        Vector2 viewportPosition = canvas.GetViewport().GetFinalTransform() *
            (canvas.GetGlobalTransformWithCanvas() * canvas.ToLocal(worldPosition));
        var motion = new InputEventMouseMotion
        {
            Position = viewportPosition,
            GlobalPosition = viewportPosition
        };
        Input.ParseInputEvent(motion);
        canvas.GetViewport().PushInput(motion, true);
        Input.FlushBufferedEvents();
    }

    private static void SendMouse(Node2D canvas, MouseButton button, bool pressed,
        Vector2 worldPosition)
    {
        SetPointer(canvas, worldPosition);
        Vector2 viewportPosition = canvas.GetViewport().GetFinalTransform() *
            (canvas.GetGlobalTransformWithCanvas() * canvas.ToLocal(worldPosition));
        var click = new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Position = viewportPosition,
            GlobalPosition = viewportPosition,
            ButtonMask = pressed ? MouseButtonMask.Left : 0
        };
        Input.ParseInputEvent(click);
        canvas.GetViewport().PushInput(click, true);
        Input.FlushBufferedEvents();
    }

    private static void SendAction(Node2D canvas, StringName action, bool pressed)
    {
        var input = new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        };
        Input.ParseInputEvent(input);
        canvas.GetViewport().PushInput(input, true);
        Input.FlushBufferedEvents();
    }

    private static void ReleaseInput()
    {
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.Shield);
    }

    private sealed record Fixture(Node2D Root, Node2D Actors, CharacterBody2D Player,
        CanyonEncounterGroup Group, PrologueProgress Progress)
    {
        public async Task Dispose(SceneTree tree)
        {
            Player.RemoveFromGroup("player");
            Root.QueueFree();
            await Frames(tree, 2);
        }
    }
}
