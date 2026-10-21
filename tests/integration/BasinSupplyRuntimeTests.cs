using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class BasinSupplyRuntimeTests
{
    private const string SupplyPath =
        "WorldRoot/Sectors/CanyonBasin/Gameplay/OptionalPaths/SalvageCache/RecoverySupply";
    // Controlled sample-layout fixture, not full Main-v2 supply or normal-route proof.
    // The production sample definitions own this raised shelf and recovery supply.
    private static readonly Vector2 UpperSpawn = new(2650, 1856);
    private static readonly Vector2 SampleCheckpoint = new(2380, 2014);
    private static bool _traceCompleteLifetime;

    public static async Task Run(SceneTree tree)
    {
        TraceLifetime("CASE_ENTER case=newgame_reentry");
        await NewGameDuringHealingCannotSpendTheNewRun(tree);
        TraceLifetime("CASE_RETURN case=newgame_reentry");
        TraceLifetime("CASE_ENTER case=title_reentry");
        await ReturnToTitleDuringHealingStillPersistsTheClaim(tree);
        TraceLifetime("CASE_RETURN case=title_reentry");
        TraceLifetime("CASE_ENTER case=check lifecycle=true damage=3");
        await CheckSupply(tree, damage: 3, expectedRestore: 2, lifecycle: true);
        TraceLifetime("CASE_RETURN case=check lifecycle=true damage=3");
        TraceLifetime("CASE_ENTER case=check lifecycle=false damage=1");
        await CheckSupply(tree, damage: 1, expectedRestore: 1, lifecycle: false);
        TraceLifetime("CASE_RETURN case=check lifecycle=false damage=1");
        GD.Print("BASIN_SUPPLY_RUNTIME_OK controlled_sample_fixture=true full_main_v2=false");
    }

    // Execute the entire existing Supply sequence, not the older NewGame-only
    // characterization below. Scalar markers perturb observation, not ownership,
    // caching, teardown, frame waits, input, or production lifetime policy.
    public static async Task RunCompleteLifetime(SceneTree tree)
    {
        bool previousFallback = PrologueProductionMode.AllowDiagnosticFallbacks;
        bool previousTrace = _traceCompleteLifetime;
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            _traceCompleteLifetime = true;
            TraceLifetime("SUITE_ENTER prefix=none resource_policy_changed=false extra_frames=0");
            await Run(tree);
            TraceLifetime("SUITE_RETURN prefix=none cause_proved=false");
        }
        finally
        {
            _traceCompleteLifetime = previousTrace;
            PrologueProductionMode.AllowDiagnosticFallbacks = previousFallback;
        }
    }

    // A bounded contrast: retain the original mount, auto-start, full-health
    // supply overlap and teardown, but never request injury or healing reentry.
    // The ordinary initialization Health.ResetFull is deliberately preserved.
    public static async Task RunMountOnly(SceneTree tree)
    {
        bool previousFallback = PrologueProductionMode.AllowDiagnosticFallbacks;
        bool previousTrace = _traceCompleteLifetime;
        var rootIds = new HashSet<ulong>();
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            _traceCompleteLifetime = true;
            TraceLifetime("MOUNT_ONLY_ENTER roots=5 extra_damage_or_healing_requests=false initialization_preserved=true");
            for (int index = 1; index <= 5; index++)
            {
                PrologueLevelController level = null;
                ulong rootId = 0;
                string savePath = Path.Combine(CreateStorageRoot("mount-only"), "save.json");
                TraceLifetime($"MOUNT_CASE_ENTER index={index}");
                try
                {
                    level = await OpenSample(tree, savePath, continueSaved: false);
                    rootId = TraceRoot($"MOUNT_ONLY_MOUNTED index={index}", level);
                    TestAssert.True(rootIds.Add(rootId), "each mount-only scene is a distinct real root");
                    TestAssert.Equal(PrologueGameFlowState.Playing,
                        level.GetNode<PrologueGameFlowController>("GameFlow").State,
                        "the original automatic new-game initialization actually reaches Playing");
                    HealthComponent health = level.Player.GetNode<HealthComponent>("Health");
                    PrologueSupplyCache supply = level.GetNode<PrologueSupplyCache>(SupplyPath);
                    TestAssert.True(supply.GetOverlappingBodies().Contains(level.Player),
                        "mount-only control physically contacts the original full-health supply");
                    TestAssert.True(health.MaxHealth > 0 && health.CurrentHealth == health.MaxHealth,
                        "mount-only control keeps normal initialized full health");
                    TestAssert.True(!supply.IsSpent && !level.Progress.Discoveries.Contains("salvage_cache") &&
                        !PrologueSaveStore.LoadOrNew(savePath).Discoveries.Contains("salvage_cache"),
                        "real full-health supply overlap neither consumes nor persists an earned claim");
                    TraceLifetime($"MOUNT_ONLY_CONTROL index={index} hp={health.CurrentHealth} max={health.MaxHealth} overlap=true spent=false", rootId);
                }
                finally
                {
                    if (GodotObject.IsInstanceValid(level))
                    {
                        TraceRoot($"RELEASE_ENTER site=mount_only index={index}", level);
                        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                        TraceLifetime($"RELEASE_RETURN site=mount_only index={index}", rootId);
                        TestAssert.True(!GodotObject.IsInstanceValid(level),
                            "the original mount-only teardown actually frees its native root");
                    }
                }
                TraceLifetime($"MOUNT_CASE_RETURN index={index}", rootId);
            }
            TestAssert.Equal(5, rootIds.Count, "five independent mount-only roots returned");
            TraceLifetime("MOUNT_ONLY_RETURN roots=5 extra_damage_or_healing_requests=false cause_proved=false");
        }
        finally
        {
            _traceCompleteLifetime = previousTrace;
            PrologueProductionMode.AllowDiagnosticFallbacks = previousFallback;
        }
    }

    private static void TraceLifetime(string step, ulong rootId = 0)
    {
        if (_traceCompleteLifetime) GD.Print($"BASIN_SUPPLY_LIFETIME {step} root_id={rootId}");
    }

    private static ulong TraceRoot(string step, Node root)
    {
        if (!_traceCompleteLifetime) return 0;
        ulong rootId = root.GetInstanceId();
        TraceLifetime(step, rootId);
        return rootId;
    }

    // Process-isolated characterization of the existing pre-suite boundary.
    // Keep default resource caching, original teardown and immediate continuation;
    // no GC, lifetime pin, extra frame, or production resource mutation.
    public static async Task RunLifetimeBoundary(SceneTree tree, string prefix)
    {
        TestAssert.True(prefix is "none" or "claw" or "pre", "known lifetime diagnostic prefix");
        bool previousFallback = PrologueProductionMode.AllowDiagnosticFallbacks;
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            if (prefix == "pre")
            {
                await BasinAscentCameraRuntimeTests.Run(tree);
                await BasinLaborerClearanceRuntimeTests.Run(tree);
            }
            if (prefix != "none")
            {
                await BasinClawTargetTests.Run(tree);
                Knight.Tests.Unit.HealthRestoreTests.Run();
            }
            GD.Print($"BASIN_RESOURCE_BOUNDARY_ENTER prefix={prefix} default_cache=true extra_frames=0");
            await NewGameDuringHealingCannotSpendTheNewRun(tree);
            GD.Print($"BASIN_RESOURCE_BOUNDARY_COMPLETE prefix={prefix} native_diagnostics_must_be_checked=true cause_proved=false");
        }
        finally { PrologueProductionMode.AllowDiagnosticFallbacks = previousFallback; }
    }

    private static async Task CheckSupply(SceneTree tree, int damage, int expectedRestore, bool lifecycle)
    {
        string directory = CreateStorageRoot("supply");
        string savePath = Path.Combine(directory, "save.json");
        PrologueLevelController level = null;
        try
        {
            level = await OpenSample(tree, savePath, continueSaved: false);
            // Controlled public progress setup, not a claim of walking to the checkpoint.
            // Later death/Continue must restore the real sample camp checkpoint, not the supply.
            TestAssert.True(level.Progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, SampleCheckpoint),
                "fixture assigns the authored sample camp checkpoint before acquiring the supply");
            CharacterBody2D player = level.Player;
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            PrologueSupplyCache supply = level.GetNode<PrologueSupplyCache>(SupplyPath);
            TestAssert.True(supply.GetOverlappingBodies().Contains(player),
                "actual player collision enters the authored supply area on the upper platform");
            AssertAppearance(supply, spent: false);
            string availableText = supply.GetNode<Label>("Status").Text;
            TestAssert.Equal(health.MaxHealth, health.CurrentHealth, "full-health arrival stays full");
            TestAssert.True(!level.Progress.Discoveries.Contains("salvage_cache") &&
                !PrologueSaveStore.LoadOrNew(savePath).Discoveries.Contains("salvage_cache"),
                "full-health contact neither consumes nor earns the optional supply discovery");

            await WalkTo(tree, player, 2570);
            TestAssert.True(!supply.GetOverlappingBodies().Contains(player), "A input really leaves the supply area");
            TestAssert.True(health.ApplyDamage(new DamageRequest(damage, Vector2.Right, 0f, 991UL)),
                "public damage API arranges the injured-player fixture");
            int healed = 0, healEvents = 0;
            bool spentAtHeal = false, discoveredAtHeal = false;
            health.Healed += (amount, _) =>
            {
                healed += amount;
                healEvents++;
                spentAtHeal = supply.IsSpent;
                discoveredAtHeal = level.Progress.Discoveries.Contains("salvage_cache");
            };
            await WalkTo(tree, player, 2650);
            TestAssert.Equal(expectedRestore, healed, "real re-entry restores only the available missing health");
            TestAssert.Equal(1, healEvents, "one overlap awards one healing event");
            GD.Print($"BASIN_SUPPLY_HEALED_OBSERVER spent={spentAtHeal} discovered={discoveredAtHeal}");
            TestAssert.True(spentAtHeal && discoveredAtHeal,
                "synchronous Healed observers already see the supply spent and discovery recorded");
            TestAssert.Equal(5 - damage + expectedRestore, health.CurrentHealth,
                "optional supply restores two health, capped by maximum");
            AssertAppearance(supply, spent: true);
            TestAssert.True(supply.GetNode<Label>("Status").Text != availableText,
                "spent supply has a distinct readable status, not just hidden artwork");
            TestAssert.True(level.Progress.Discoveries.Contains("salvage_cache") &&
                PrologueSaveStore.LoadOrNew(savePath).Discoveries.Contains("salvage_cache"),
                "earned discovery is already persisted without a later checkpoint or manual save");
            TestAssert.Equal(health.CurrentHealth,
                level.GetNode<PrologueFormalHud>("Hud/FormalHud").CurrentModel.HealthSegments.Count(
                    segment => segment == HealthShardState.Filled),
                "actual formal HUD reflects healed health through runtime signal binding");

            await WalkTo(tree, player, 2570);
            await PhysicsTicks(tree, 24);
            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Right, 0f, 992UL)),
                "spent-supply repeat fixture is injured again after invulnerability expires");
            int beforeRepeat = health.CurrentHealth;
            await WalkTo(tree, player, 2650);
            TestAssert.Equal(beforeRepeat, health.CurrentHealth, "spent supply cannot heal on another real entry");
            TestAssert.Equal(1, healEvents, "repeat entry emits no further heal");
            if (!lifecycle) return;

            int recoveries = 0;
            level.RecoveryCompleted += _ => recoveries++;
            await PhysicsTicks(tree, 24);
            TestAssert.True(health.ApplyDamage(new DamageRequest(99, Vector2.Right, 0f, 993UL)),
                "lifecycle fixture dies through actual Health damage and level recovery binding");
            await PhysicsTicks(tree, 8);
            TestAssert.Equal(1, recoveries, "actual death completes one recovery");
            TestAssert.True(player.GlobalPosition.DistanceTo(SampleCheckpoint) < 12f,
                "death returns to the sample camp checkpoint, not to the optional supply");
            AssertAppearance(level.GetNode<PrologueSupplyCache>(SupplyPath), spent: true);
            TestAssert.Equal(1, healEvents, "death recovery does not re-award the consumed supply");

            ulong oldRootId = TraceRoot("RELEASE_ENTER site=check.lifecycle_swap lifecycle=true", level);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            TraceLifetime("RELEASE_RETURN site=check.lifecycle_swap lifecycle=true", oldRootId);
            level = null;
            TraceLifetime("CONTINUE_OPEN_ENTER lifecycle=true");
            level = await OpenSample(tree, savePath, continueSaved: true);
            TraceRoot("CONTINUE_OPEN_RETURN lifecycle=true", level);
            TestAssert.True(level.Player.GlobalPosition.DistanceTo(SampleCheckpoint) < 12f,
                "Continue restores the saved sample camp checkpoint while mounting spent supply state");
            AssertAppearance(level.GetNode<PrologueSupplyCache>(SupplyPath), spent: true);
            TestAssert.True(level.Progress.Discoveries.Contains("salvage_cache"),
                "a fresh actual sample-layout scene Continue restores the disk-earned spent state");
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.ReturnToTitle();
            flow.StartNew();
            await PhysicsTicks(tree, 8);
            AssertAppearance(level.GetNode<PrologueSupplyCache>(SupplyPath), spent: false);
            TestAssert.True(!level.Progress.Discoveries.Contains("salvage_cache") &&
                !PrologueSaveStore.LoadOrNew(savePath).Discoveries.Contains("salvage_cache"),
                "real New Game clears both saved discovery and mounted supply appearance");
            ulong statusRootId = TraceRoot("STATUS_ENTER lifecycle=true", level);
            await CheckStatusReplacement(tree, level.GetNode<PrologueSupplyCache>(SupplyPath));
            TraceLifetime("STATUS_RETURN lifecycle=true", statusRootId);
        }
        finally
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Send(InputActions.MoveLeft, false);
            Send(InputActions.MoveRight, false);
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                ulong rootId = TraceRoot($"RELEASE_ENTER site=check.finally lifecycle={lifecycle.ToString().ToLowerInvariant()}", level);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                TraceLifetime($"RELEASE_RETURN site=check.finally lifecycle={lifecycle.ToString().ToLowerInvariant()}", rootId);
            }
        }
    }

    public static async Task NewGameDuringHealingCannotSpendTheNewRun(SceneTree tree)
    {
        string directory = CreateStorageRoot("supply-reentry");
        string savePath = Path.Combine(directory, "save.json");
        PrologueLevelController level = null;
        HealthComponent health = null;
        HealthComponent.HealedEventHandler onHealed = null;
        try
        {
            level = await OpenSample(tree, savePath, continueSaved: false);
            CharacterBody2D player = level.Player;
            health = player.GetNode<HealthComponent>("Health");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var supply = level.GetNode<PrologueSupplyCache>(SupplyPath);
            await WalkTo(tree, player, 2570);
            TestAssert.True(!supply.GetOverlappingBodies().Contains(player),
                "reentrant fixture physically leaves the supply before the injured return");
            TestAssert.True(health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 994UL)),
                "public damage arranges a healable reentrant fixture");
            int callbacks = 0;
            bool spentAtHeal = false, discoveredAtHeal = false, resetWasClean = false;
            onHealed = (_, _) =>
            {
                callbacks++;
                if (callbacks != 1) return;
                spentAtHeal = supply.IsSpent;
                discoveredAtHeal = level.Progress.Discoveries.Contains("salvage_cache");
                // Deliberately synchronous public flow calls expose an old claim writing
                // into a new run after a real Health signal returns. No Area signal injection.
                flow.ReturnToTitle();
                flow.StartNew();
                resetWasClean = flow.State == PrologueGameFlowState.Playing &&
                    !level.Progress.Discoveries.Contains("salvage_cache");
            };
            health.Healed += onHealed;
            await WalkTo(tree, player, 2650);
            await PhysicsTicks(tree, 4);
            bool discoveredAfter = level.Progress.Discoveries.Contains("salvage_cache");
            PrologueSaveData saved = PrologueSaveStore.LoadOrNew(savePath);
            TestAssert.True(File.Exists(savePath) && !saved.WasRecovered,
                "new-game callback leaves a valid actual disk save, not a missing or recovered fallback");
            bool savedAfter = saved.Discoveries.Contains("salvage_cache");
            GD.Print($"BASIN_SUPPLY_NEW_RUN_REENTRY callbacks={callbacks} spent_at_heal={spentAtHeal} " +
                $"discovered_at_heal={discoveredAtHeal} reset_clean={resetWasClean} " +
                $"discovered_after={discoveredAfter} saved_after={savedAfter}");
            TestAssert.Equal(1, callbacks, "one real supply entry invokes exactly one healing callback");
            TestAssert.True(resetWasClean, "the synchronous new-game request actually starts a clean playing run");
            TestAssert.True(level.GetNode<Node2D>("WorldRoot").ProcessMode == Node.ProcessModeEnum.Inherit &&
                level.GetNode<Node2D>("PlayerRoot").ProcessMode == Node.ProcessModeEnum.Inherit && player.CanProcess(),
                "same-frame title then new-game leaves no stale deferred freeze after physics queries finish");
            TestAssert.True(!discoveredAfter && !savedAfter,
                "the previous run's outer claim cannot mark or persist the new run's supply as spent");
            AssertAppearance(level.GetNode<PrologueSupplyCache>(SupplyPath), spent: false);
            TestAssert.Equal(health.MaxHealth, health.CurrentHealth, "new game retains its ordinary full-health reset");
            TestAssert.True(spentAtHeal && discoveredAtHeal,
                "the old run's healing callback observes a consistent completed claim before resetting it");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(health) && onHealed != null) health.Healed -= onHealed;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Send(InputActions.MoveLeft, false);
            Send(InputActions.MoveRight, false);
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                ulong rootId = TraceRoot("RELEASE_ENTER site=newgame.finally", level);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                TraceLifetime("RELEASE_RETURN site=newgame.finally", rootId);
            }
        }
    }

    public static async Task ReturnToTitleDuringHealingStillPersistsTheClaim(SceneTree tree)
    {
        string directory = CreateStorageRoot("supply-title");
        string savePath = Path.Combine(directory, "save.json");
        PrologueLevelController level = null;
        HealthComponent health = null;
        HealthComponent.HealedEventHandler onHealed = null;
        try
        {
            level = await OpenSample(tree, savePath, continueSaved: false);
            level.Progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, SampleCheckpoint);
            CharacterBody2D player = level.Player;
            health = player.GetNode<HealthComponent>("Health");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var supply = level.GetNode<PrologueSupplyCache>(SupplyPath);
            await WalkTo(tree, player, 2570);
            TestAssert.True(!supply.GetOverlappingBodies().Contains(player),
                "title-callback fixture physically exits before the next supply contact");
            TestAssert.True(health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 995UL)),
                "title-callback fixture is injured through public damage");
            int callbacks = 0, restored = 0;
            onHealed = (amount, _) =>
            {
                callbacks++;
                restored += amount;
                flow.ReturnToTitle();
            };
            health.Healed += onHealed;
            // Title disables gameplay as soon as the Area heals, so stop on the actual
            // callback rather than requiring the now-disabled player to reach x2650.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Send(InputActions.MoveRight, true);
            for (int tick = 0; tick < 90 && callbacks == 0; tick++) await PhysicsTicks(tree, 1);
            Send(InputActions.MoveRight, false);
            await PhysicsTicks(tree, 4);
            TestAssert.Equal(1, callbacks, "one real Area contact returns to title through its healing callback");
            TestAssert.Equal(2, restored, "returning to title does not discard the already-earned healing");
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
                "the callback really left Playing without starting another run");
            TestAssert.True(level.GetNode<Node2D>("WorldRoot").ProcessMode == Node.ProcessModeEnum.Disabled &&
                level.GetNode<Node2D>("PlayerRoot").ProcessMode == Node.ProcessModeEnum.Disabled && !player.CanProcess(),
                "title requested during real BodyEntered freezes sample physics after queries finish");
            PrologueSaveData saved = PrologueSaveStore.LoadOrNew(savePath);
            GD.Print($"BASIN_SUPPLY_TITLE_REENTRY callbacks={callbacks} state={flow.State} " +
                $"spent={supply.IsSpent} saved={saved.Discoveries.Contains("salvage_cache")}");
            TestAssert.True(supply.IsSpent && level.Progress.Discoveries.Contains("salvage_cache"),
                "returning to title retains the same run's earned discovery");
            TestAssert.True(File.Exists(savePath) && !saved.WasRecovered &&
                saved.Discoveries.Contains("salvage_cache"),
                "the same run's claim persists even though its Healed callback returned to Title");
            flow.Continue();
            await PhysicsTicks(tree, 8);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "actual Continue resumes the claimed run");
            AssertAppearance(level.GetNode<PrologueSupplyCache>(SupplyPath), spent: true);
            TestAssert.Equal(1, callbacks, "Continue cannot award the spent supply again");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(health) && onHealed != null) health.Healed -= onHealed;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Send(InputActions.MoveLeft, false);
            Send(InputActions.MoveRight, false);
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                ulong rootId = TraceRoot("RELEASE_ENTER site=title.finally", level);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                TraceLifetime("RELEASE_RETURN site=title.finally", rootId);
            }
        }
    }

    private static async Task CheckStatusReplacement(SceneTree tree, PrologueSupplyCache supply)
    {
        Label previous = supply.GetNode<Label>("Status");
        string expectedText = previous.Text;
        TestAssert.True(!string.IsNullOrWhiteSpace(expectedText), "replacement fixture begins with readable status");
        // The full-health new-run supply has stable semantics; only the skin label changes.
        var replacement = (Label)previous.Duplicate();
        replacement.Name = "Status";
        replacement.Text = string.Empty;
        supply.RemoveChild(previous);
        supply.AddChild(replacement);
        try
        {
            await PhysicsTicks(tree, 4);
            TestAssert.True(GodotObject.IsInstanceValid(previous) && !previous.IsInsideTree(),
                "the previous status label stays alive but detached during the skin replacement check");
            TestAssert.Equal(expectedText, supply.GetNode<Label>("Status").Text,
                "a replacement Status label receives current text even when supply semantics have not changed");
        }
        finally { previous.Free(); }
    }

    private static async Task<PrologueLevelController> OpenSample(SceneTree tree, string savePath, bool continueSaved)
    {
        PrologueLevelController level = CreateSampleFixture(savePath, continueSaved);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        try
        {
            tree.Root.AddChild(level);
            await PhysicsTicks(tree, 3);
            if (continueSaved)
            {
                TestAssert.True(flow.HasSave, "isolated earned save exposes actual Continue");
                flow.Continue();
            }
            await PhysicsTicks(tree, 8);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, level.CurrentSector,
                "controlled sample fixture mounts the real sample Basin scene");
            TestAssert.True(level.Player.IsOnFloor(), "sample fixture settles on actual authored floor physics");
            TraceRoot($"OPEN_RETURN continue_saved={continueSaved.ToString().ToLowerInvariant()}", level);
            return level;
        }
        catch
        {
            ulong rootId = TraceRoot($"RELEASE_ENTER site=open.catch continue_saved={continueSaved.ToString().ToLowerInvariant()}", level);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            TraceLifetime($"RELEASE_RETURN site=open.catch continue_saved={continueSaved.ToString().ToLowerInvariant()}", rootId);
            throw;
        }
    }

    internal static string CreateStorageRoot(string suffix)
        => IsolatedPrologueFixture.CreatePaths("basin-" + suffix).CaseRoot;

    internal static PrologueLevelController CreateSampleFixture(string savePath, bool continueSaved)
    {
        IsolatedPrologueFixture.ValidateExistingSavePath(savePath);
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        TraceRoot($"ROOT_CREATED continue_saved={continueSaved.ToString().ToLowerInvariant()}", level);
        // Reuse the actual independent sample's production geometry and runtime modes.
        // Unlike BasinQualitySampleEntry's fixed camp start, this controlled fixture
        // arranges only its upper-shelf spawn before entering the tree. All later
        // crossings, recovery and Continue still use shared production controllers.
        level.FreezeWorldWhenGameplayInactive = true;
        level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position = UpperSpawn;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        world.InitialSector = PrologueSectorId.CanyonBasin;
        world.LayoutVersion = 1;
        DescentClawGuidanceRuntimeTests.TraceSectorDefinitions(world, "basin.before-replace");
        world.Definitions = BasinSampleLegacyLayout.CreateDefinitions();
        DescentClawGuidanceRuntimeTests.TraceSectorDefinitions(world, "basin.after-replace");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.BasinQualitySampleMode = true;
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = !continueSaved;
        IsolatedPrologueFixture.ConfigureExisting(level, savePath);
        level.GetNode<PrologueCompletionController>("Completion").BasinQualitySampleMode = true;
        level.GetNode<PrologueUiRoot>("ProductUi").BasinQualitySampleMode = true;
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        guidance.SetGuidanceEnabled(false);
        guidance.ProcessMode = Node.ProcessModeEnum.Disabled;
        return level;
    }

    private static void AssertAppearance(PrologueSupplyCache supply, bool spent)
    {
        TestAssert.Equal(spent, supply.IsSpent, "supply consumption state matches durable discovery");
        TestAssert.Equal(!spent, supply.GetNode<Node2D>("AvailableVisual").IsVisibleInTree(),
            "available artwork agrees with consumption state");
        TestAssert.Equal(spent, supply.GetNode<Node2D>("SpentVisual").IsVisibleInTree(),
            "spent artwork agrees with consumption state");
        Label status = supply.GetNode<Label>("Status");
        TestAssert.True(status.IsVisibleInTree() && !string.IsNullOrWhiteSpace(status.Text),
            "supply status stays visible and readable in both states");
    }

    private static async Task WalkTo(SceneTree tree, CharacterBody2D player, float x)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        bool right = player.GlobalPosition.X < x;
        StringName action = right ? InputActions.MoveRight : InputActions.MoveLeft;
        Send(action, true);
        try
        {
            for (int tick = 0; tick < 90; tick++)
            {
                await PhysicsTicks(tree, 1);
                if (right ? player.GlobalPosition.X >= x : player.GlobalPosition.X <= x) return;
            }
            throw new TimeoutException($"A/D supply crossing stalled: target={x}, player={player.GlobalPosition}");
        }
        finally { Send(action, false); await PhysicsTicks(tree, 6); }
    }

    private static async Task PhysicsTicks(SceneTree tree, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Send(StringName action, bool pressed) =>
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed });
}
