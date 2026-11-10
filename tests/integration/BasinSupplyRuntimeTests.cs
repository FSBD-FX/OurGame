using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    private static bool _traceOriginalMounts;
    private static string _mountCase = "none";
    private static int _mountRows, _mountOmitted, _mountFaults, _mountSequence, _mountRoots;
    private static int _resourceRows, _resourceOmitted, _resourceFaults, _resourceSequence;
    private static int _sectorRows, _sectorOmitted, _sectorFaults, _sectorSequence, _sectorOperations;
    private static List<(int Operation, string Kind, int Sector)> _sectorStack = new();

    public static async Task Run(SceneTree tree)
    {
        bool previousMounts = _traceOriginalMounts;
        string previousCase = _mountCase;
        int previousRows = _mountRows, previousOmitted = _mountOmitted, previousFaults = _mountFaults,
            previousSequence = _mountSequence, previousRoots = _mountRoots;
        int previousResourceRows = _resourceRows, previousResourceOmitted = _resourceOmitted,
            previousResourceFaults = _resourceFaults, previousResourceSequence = _resourceSequence;
        var previousSectorObserver = PrologueSectorDefinition.OperationObserver;
        int previousObserverFaults = PrologueSectorDefinition.OperationObserverFaults;
        int previousSectorRows = _sectorRows, previousSectorOmitted = _sectorOmitted,
            previousSectorFaults = _sectorFaults, previousSectorSequence = _sectorSequence,
            previousSectorOperations = _sectorOperations;
        var previousSectorStack = _sectorStack;
        _traceOriginalMounts = true; _mountCase = "suite-entry";
        _mountRows = _mountOmitted = _mountFaults = _mountSequence = _mountRoots = 0;
        _resourceRows = _resourceOmitted = _resourceFaults = _resourceSequence = 0;
        _sectorRows = _sectorOmitted = _sectorFaults = _sectorSequence = _sectorOperations = 0;
        _sectorStack = new();
        PrologueSectorDefinition.OperationObserverFaults = 0;
        PrologueSectorDefinition.OperationObserver = TraceSectorOperation;
        try
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
        finally
        {
            try
            {
                GD.Print($"BASIN_SUPPLY_MOUNT_END rows={_mountRows}/128 sequence={_mountSequence} " +
                    $"created_roots={_mountRoots} omitted={_mountOmitted} faults={_mountFaults} " +
                    "original_four_cases_retained=true completion_from_end_marker=false extra_waits=0 new_strong_roots=0 subtree_walk=false cause_proved=false");
                GD.Print($"BASIN_SUPPLY_RESOURCE_END rows={_resourceRows}/64 sequence={_resourceSequence} " +
                    $"created_roots={_mountRoots} omitted={_resourceOmitted} faults={_resourceFaults} " +
                    "scalar_only=true no_retained_resource_roots=true extra_waits=0 cause_proved=false");
                GD.Print($"BASIN_SECTOR_OPERATION_END rows={_sectorRows}/1024 sequence={_sectorSequence} " +
                    $"created_roots={_mountRoots} operations={_sectorOperations} open={_sectorStack.Count} " +
                    $"omitted={_sectorOmitted} faults={_sectorFaults} observer_faults={PrologueSectorDefinition.OperationObserverFaults} " +
                    "scalar_only=true resource_policy_changed=false extra_waits=0 cause_proved=false");
            }
            catch (Exception) { /* A missing diagnostic END is invalid evidence, never a replacement primary failure. */ }
            _traceOriginalMounts = previousMounts; _mountCase = previousCase;
            _mountRows = previousRows; _mountOmitted = previousOmitted; _mountFaults = previousFaults;
            _mountSequence = previousSequence; _mountRoots = previousRoots;
            _resourceRows = previousResourceRows; _resourceOmitted = previousResourceOmitted;
            _resourceFaults = previousResourceFaults; _resourceSequence = previousResourceSequence;
            PrologueSectorDefinition.OperationObserver = previousSectorObserver;
            PrologueSectorDefinition.OperationObserverFaults = previousObserverFaults;
            _sectorRows = previousSectorRows; _sectorOmitted = previousSectorOmitted;
            _sectorFaults = previousSectorFaults; _sectorSequence = previousSectorSequence;
            _sectorOperations = previousSectorOperations; _sectorStack = previousSectorStack;
        }
    }

    // Execute the entire existing Supply sequence, not the older NewGame-only
    // characterization below. Scalar markers perturb observation, not ownership,
    // caching, teardown, frame waits, input, or production lifetime policy.
    public static async Task RunCompleteLifetime(SceneTree tree, string prefix = "none")
    {
        bool previousFallback = PrologueProductionMode.AllowDiagnosticFallbacks;
        bool previousTrace = _traceCompleteLifetime;
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            _traceCompleteLifetime = true;
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DeclareMainManagedThread();
#endif
            TraceLifetime($"SUITE_ENTER prefix={prefix} resource_policy_changed=false extra_frames=0");
            await Run(tree);
            TraceLifetime($"SUITE_RETURN prefix={prefix} cause_proved=false");
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
        if (_traceOriginalMounts && step.StartsWith("CASE_ENTER case=", StringComparison.Ordinal))
            _mountCase = step.Substring("CASE_ENTER case=".Length);
        if (_traceCompleteLifetime) GD.Print($"BASIN_SUPPLY_LIFETIME {step} root_id={rootId}");
    }

    private static void TraceResourceBoundary(string stage, string site)
    {
        if (!_traceOriginalMounts) return;
        _resourceSequence++;
        if (_resourceRows >= 64) { _resourceOmitted++; return; }
        try
        {
            GD.Print("BASIN_SUPPLY_RESOURCE_BOUNDARY " + JsonSerializer.Serialize(new
            {
                sequence = _resourceSequence, scope = _mountCase, stage, site,
                createdRoots = _mountRoots, engineMs = Time.GetTicksMsec(),
                physics = Engine.GetPhysicsFrames(), process = Engine.GetProcessFrames()
            }));
            _resourceRows++;
        }
        catch (Exception) { _resourceFaults++; } // Preserve the primary failure.
    }

    // Only the opt-in original Supply sequence receives this callback. The
    // stack contains scalar IDs/strings, never scene/resource wrappers. A
    // missing return stays open so a thrown scene operation cannot look passed.
    private static void TraceSectorOperation(string kind, int sector, bool enter)
    {
        if (!_traceOriginalMounts) return;
        try
        {
            _sectorSequence++;
            int operation = 0;
            if (enter)
            {
                operation = ++_sectorOperations;
                _sectorStack.Add((operation, kind, sector));
            }
            else if (_sectorStack.Count > 0 && _sectorStack[^1].Kind == kind &&
                _sectorStack[^1].Sector == sector)
            {
                operation = _sectorStack[^1].Operation;
                _sectorStack.RemoveAt(_sectorStack.Count - 1);
            }
            else _sectorFaults++;
            if (_sectorRows >= 1024) { _sectorOmitted++; return; }
            GD.Print("BASIN_SECTOR_OPERATION " + JsonSerializer.Serialize(new
            {
                sequence = _sectorSequence, operation, scope = _mountCase, kind,
                stage = enter ? "enter" : "return", sector, createdRoots = _mountRoots,
                engineMs = Time.GetTicksMsec(), physics = Engine.GetPhysicsFrames(),
                process = Engine.GetProcessFrames()
            }));
            _sectorRows++;
        }
        catch (Exception) { _sectorFaults++; }
    }

    // Pass through the expression's already-borrowed return value. No field,
    // closure, cache, collection, identity read or disposal keeps it alive after
    // this call. Logging can still perturb timing; it is not a lifetime fix.
    private static PackedScene TraceLoadedSample(PackedScene borrowed)
    {
        TraceResourceBoundary("load-return", "sample");
        TraceResourceBoundary("instantiate-enter", "sample");
        return borrowed;
    }

    // Observe only references the original caller already obtained. No GetScript,
    // GetChildren/GetPath, peer discovery, GC, wait, cache change or native object
    // reference is retained by this scalar diagnostic.
    private static void TraceOriginalMount(string stage, PrologueLevelController level,
        Node observed, string authoredReference)
    {
        if (!_traceOriginalMounts) return;
        _mountSequence++;
        if (stage == "root-created") _mountRoots++;
        if (_mountRows >= 128) { _mountOmitted++; return; }
        try
        {
            bool rootValid = GodotObject.IsInstanceValid(level);
            bool observedValid = GodotObject.IsInstanceValid(observed);
            string row = JsonSerializer.Serialize(new
            {
                sequence = _mountSequence, scope = _mountCase, stage,
                physics = Engine.GetPhysicsFrames(),
                rootId = rootValid ? level.GetInstanceId() : 0UL,
                rootType = level?.GetType().FullName ?? "null",
                rootInTree = rootValid && level.IsInsideTree(),
                reference = authoredReference, referenceKind = "original-authored-lookup-not-GetPath",
                observedId = observedValid ? observed.GetInstanceId() : 0UL,
                observedType = observed?.GetType().FullName ?? "null",
                observedInTree = observedValid && observed.IsInsideTree(),
                identitySource = "already-obtained-local-reference", scriptResourceUnmeasured = true,
                extraIdentityReads = true, resourcePolicyChanged = false, extraWaits = 0
            });
            GD.Print("BASIN_SUPPLY_MOUNT_OBS " + row);
            _mountRows++;
        }
        catch (Exception) { _mountFaults++; } // Never mask the original lifecycle/bridge failure.
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
            TraceResourceBoundary("cleanup-enter", "check.lifecycle_swap");
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            TraceResourceBoundary("cleanup-return", "check.lifecycle_swap");
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
                TraceResourceBoundary("cleanup-enter", "check.finally");
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                TraceResourceBoundary("cleanup-return", "check.finally");
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
                TraceResourceBoundary("cleanup-enter", "newgame.finally");
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                TraceResourceBoundary("cleanup-return", "newgame.finally");
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
            PrologueSaveData acceptedSlot = flow.Save;
            byte[] beforeClaim = File.ReadAllBytes(savePath);
            string retainedSlot = Path.Combine(directory, "retained-before-claim.json");
            var continueButton = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue");
            await WalkTo(tree, player, 2570);
            TestAssert.True(!supply.GetOverlappingBodies().Contains(player),
                "title-callback fixture physically exits before the next supply contact");
            TestAssert.True(health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 995UL)),
                "title-callback fixture is injured through public damage");
            int callbacks = 0, restored = 0, receipts = 0;
            bool genericTitleWriteRefused = false, receiptAfterDisk = false, continueInitiallyDisabled = false;
            level.ProgressPersistRequested += reason =>
            {
                if (reason != "discovery") return;
                receipts++;
                receiptAfterDisk = callbacks == 1 && flow.State == PrologueGameFlowState.Title &&
                    PrologueSaveStore.LoadOrNew(savePath).Discoveries.Contains("salvage_cache");
            };
            onHealed = (amount, _) =>
            {
                callbacks++;
                restored += amount;
                // A real unavailable case slot tests menu refresh without changing the
                // in-memory progress or deleting any file.
                File.Move(savePath, retainedSlot);
                flow.ReturnToTitle();
                continueInitiallyDisabled = continueButton.Disabled && !flow.HasSave;
                flow.PersistProgress("discovery");
                genericTitleWriteRefused = !File.Exists(savePath) &&
                    beforeClaim.AsSpan().SequenceEqual(File.ReadAllBytes(retainedSlot));
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
            TestAssert.True(genericTitleWriteRefused,
                "generic discovery requests must still not write in Title, even inside the actual healing callback");
            TestAssert.True(File.Exists(savePath) && !saved.WasRecovered &&
                saved.Discoveries.Contains("salvage_cache"),
                "the same run's claim persists even though its Healed callback returned to Title");
            TestAssert.True(receipts == 1 && receiptAfterDisk && ReferenceEquals(flow.Save, acceptedSlot) &&
                saved.HasActivatedCheckpoint && saved.CheckpointWorldPosition == SampleCheckpoint,
                "the one post-heal receipt sees the durable full checkpoint and the same accepted Save identity");
            TestAssert.True(continueInitiallyDisabled && flow.HasSave && !continueButton.Disabled,
                "title enables its real Continue action when the owned commit makes the case slot available");
            continueButton.EmitSignal(Button.SignalName.Pressed);
            await PhysicsTicks(tree, 8);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "actual Continue resumes the claimed run");
            AssertAppearance(level.GetNode<PrologueSupplyCache>(SupplyPath), spent: true);
            TestAssert.Equal(1, callbacks, "Continue cannot award the spent supply again");
            GD.Print("BASIN_SUPPLY_OWNED_TITLE_COMMIT_OK generic_title_refused=true receipt_after_disk=true same_slot=true real_continue=true button_reenabled=true");
            health.Healed -= onHealed; onHealed = null;
            await SameSaveContinueCannotFinishAnOldSupplyClaim(tree, level, savePath);
            await LockedTitleSupplyCannotPublishUnsavedProgress(tree, level, savePath);
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
                TraceResourceBoundary("cleanup-enter", "title.finally");
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                TraceResourceBoundary("cleanup-return", "title.finally");
                TraceLifetime("RELEASE_RETURN site=title.finally", rootId);
            }
        }
    }

    private static async Task SameSaveContinueCannotFinishAnOldSupplyClaim(SceneTree tree,
        PrologueLevelController level, string savePath)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.ReturnToTitle(); flow.StartNew(); await PhysicsTicks(tree, 4);
        level.Progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, SampleCheckpoint);
        CharacterBody2D player = level.Player;
        var supply = level.GetNode<PrologueSupplyCache>(SupplyPath);
        var health = player.GetNode<HealthComponent>("Health");
        await WalkTo(tree, player, 2570);
        TestAssert.True(flow.State == PrologueGameFlowState.Playing && !supply.IsSpent &&
            !supply.GetOverlappingBodies().Contains(player) &&
            health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 996UL)),
            "same-Save reentry fixture starts a clean session, leaves the real supply, and receives real damage");
        PrologueSaveData accepted = flow.Save;
        byte[] protectedBytes = null;
        int callbacks = 0, publications = 0, resumedSessions = 0;
        bool failedStartRetained = false, restoredSame = false;
        void OnStarted() => resumedSessions++;
        void OnPersist(string reason) { if (reason == "discovery") publications++; }
        void OnHealed(int amount, int remaining)
        {
            callbacks++;
            if (callbacks != 1 || !flow.TrySaveBeforeQuit(out _)) return;
            // Keep legal, noncanonical bytes so an otherwise identical stale rewrite
            // cannot masquerade as a successful no-write assertion.
            File.AppendAllText(savePath, "\n  \n");
            protectedBytes = File.ReadAllBytes(savePath);
            flow.ReturnToTitle();
            using (var locked = new FileStream(savePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
                flow.StartNew();
            failedStartRetained = flow.State == PrologueGameFlowState.Title && ReferenceEquals(flow.Save, accepted) &&
                flow.LastRejectedTransition.StartsWith("startup-save:", StringComparison.Ordinal) &&
                protectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(savePath));
            flow.Continue();
            restoredSame = flow.State == PrologueGameFlowState.Playing && ReferenceEquals(flow.Save, accepted) &&
                supply.IsSpent && level.Progress.Discoveries.Contains("salvage_cache");
        }
        flow.SessionStarted += OnStarted;
        level.ProgressPersistRequested += OnPersist;
        health.Healed += OnHealed;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Send(InputActions.MoveRight, true);
            for (int tick = 0; tick < 90 && callbacks == 0; tick++) await PhysicsTicks(tree, 1);
            Send(InputActions.MoveRight, false);
            await PhysicsTicks(tree, 4);
            TestAssert.True(callbacks == 1 && failedStartRetained && restoredSame && resumedSessions == 1,
                "actual healing callback rejects a blocked Start, then a real Continue restores the same Save and discovery");
            TestAssert.True(publications == 0 && protectedBytes != null &&
                protectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(savePath)),
                "old claim cannot publish or rewrite the exact same Save after a different session admission");
            GD.Print("BASIN_SUPPLY_STALE_CLAIM_OK real_heal=true failed_start=true same_save_continue=true old_receipts=0 exact_bytes=true");
        }
        finally
        {
            Send(InputActions.MoveRight, false);
            flow.SessionStarted -= OnStarted;
            level.ProgressPersistRequested -= OnPersist;
            health.Healed -= OnHealed;
        }
    }

    private static async Task LockedTitleSupplyCannotPublishUnsavedProgress(SceneTree tree,
        PrologueLevelController level, string savePath)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.ReturnToTitle(); flow.StartNew(); await PhysicsTicks(tree, 4);
        var supply = level.GetNode<PrologueSupplyCache>(SupplyPath);
        CharacterBody2D player = level.Player;
        var health = player.GetNode<HealthComponent>("Health");
        await WalkTo(tree, player, 2570);
        PrologueSaveData previous = flow.Save;
        Vector2 previousCheckpoint = previous.CheckpointWorldPosition;
        byte[] previousBytes = File.ReadAllBytes(savePath);
        Vector2 pendingCheckpoint = previousCheckpoint == UpperSpawn ? SampleCheckpoint : UpperSpawn;
        level.Progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, pendingCheckpoint);
        TestAssert.True(level.Progress.RestoreCheckpoint().WorldPosition == pendingCheckpoint &&
            !supply.IsSpent && !supply.GetOverlappingBodies().Contains(player) &&
            health.ApplyDamage(new DamageRequest(3, Vector2.Right, 0f, 997UL)),
            "locked claim fixture has a distinct live checkpoint and a real injured player");
        FileStream blocker = null;
        int callbacks = 0, receipts = 0;
        void OnPersist(string reason) { if (reason == "discovery") receipts++; }
        void OnHealed(int _, int __)
        {
            callbacks++;
            blocker = new FileStream(savePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
            flow.ReturnToTitle();
        }
        level.ProgressPersistRequested += OnPersist;
        health.Healed += OnHealed;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Send(InputActions.MoveRight, true);
            for (int tick = 0; tick < 90 && callbacks == 0; tick++) await PhysicsTicks(tree, 1);
            Send(InputActions.MoveRight, false);
            await PhysicsTicks(tree, 4);
            TestAssert.True(callbacks == 1 && flow.State == PrologueGameFlowState.Title && blocker != null &&
                ReferenceEquals(previous, flow.Save) && receipts == 0 &&
                !previous.Discoveries.Contains("salvage_cache") && previous.CheckpointWorldPosition == previousCheckpoint &&
                previousBytes.AsSpan().SequenceEqual(File.ReadAllBytes(savePath)),
                "TITLE_SUPPLY_IO_MUTATED: blocked replacement must not publish unsaved progress in the accepted slot");
            TestAssert.True(flow.LastRejectedTransition.StartsWith("supply-save:", StringComparison.Ordinal),
                "blocked title supply is reported as a failed commit, not silently discarded");
            blocker.Dispose(); blocker = null;
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").EmitSignal(Button.SignalName.Pressed);
            await PhysicsTicks(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && !supply.IsSpent &&
                level.Progress.RestoreCheckpoint().WorldPosition == previousCheckpoint && callbacks == 1 &&
                previousBytes.AsSpan().SequenceEqual(File.ReadAllBytes(savePath)),
                "actual Continue restores the previous checkpoint and still-available supply without rewriting disk");
            GD.Print("BASIN_SUPPLY_TITLE_IO_REFUSED_OK actual_lock=true committed_save_unchanged=true old_continue=true receipts=0");
        }
        finally
        {
            blocker?.Dispose();
            Send(InputActions.MoveRight, false);
            level.ProgressPersistRequested -= OnPersist;
            health.Healed -= OnHealed;
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
            TraceOriginalMount("mount-enter", level, flow, "GameFlow");
            tree.Root.AddChild(level);
            TraceOriginalMount("mount-return", level, flow, "GameFlow");
            await PhysicsTicks(tree, 3);
            if (continueSaved)
            {
                TestAssert.True(flow.HasSave, "isolated earned save exposes actual Continue");
                flow.Continue();
            }
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                !level.ResidentPreparationPending,
                "the legacy sample reaches actual Playing before its original floor observations");
            await PhysicsTicks(tree, 8);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, level.CurrentSector,
                "controlled sample fixture mounts the real sample Basin scene");
            if (!level.Player.IsOnFloor())
            {
                var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                GD.Print("BASIN_SAMPLE_OPEN_NOT_GROUNDED " + JsonSerializer.Serialize(new
                {
                    layout = world.LayoutVersion,
                    content = level.ResidentContent.ToString(),
                    state = flow.State.ToString(),
                    pending = level.ResidentPreparationPending,
                    reason = level.ResidentPreparationFailure,
                    x = level.Player.GlobalPosition.X,
                    y = level.Player.GlobalPosition.Y,
                    vx = level.Player.Velocity.X,
                    vy = level.Player.Velocity.Y,
                    canProcess = level.Player.CanProcess()
                }));
            }
            TestAssert.True(level.Player.IsOnFloor(), "sample fixture settles on actual authored floor physics");
            TraceOriginalMount("open-return", level, flow, "GameFlow");
            TraceRoot($"OPEN_RETURN continue_saved={continueSaved.ToString().ToLowerInvariant()}", level);
            return level;
        }
        catch
        {
            ulong rootId = TraceRoot($"RELEASE_ENTER site=open.catch continue_saved={continueSaved.ToString().ToLowerInvariant()}", level);
            TraceResourceBoundary("cleanup-enter", "open.catch");
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            TraceResourceBoundary("cleanup-return", "open.catch");
            TraceLifetime($"RELEASE_RETURN site=open.catch continue_saved={continueSaved.ToString().ToLowerInvariant()}", rootId);
            throw;
        }
    }

    internal static string CreateStorageRoot(string suffix)
        => IsolatedPrologueFixture.CreatePaths("basin-" + suffix).CaseRoot;

    internal static PrologueLevelController CreateSampleFixture(string savePath, bool continueSaved)
    {
        IsolatedPrologueFixture.ValidateExistingSavePath(savePath);
        TraceResourceBoundary("load-enter", "sample");
        var level = TraceLoadedSample(GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn"))
            .Instantiate<PrologueLevelController>();
        TraceResourceBoundary("instantiate-return", "sample");
#if REFCOUNTED_FATAL_IDENTITY
        if (_traceCompleteLifetime) ObserveWithOwnedRoot(level,ObserveAssignedPlayerProfile);
#endif
        TraceRoot($"ROOT_CREATED continue_saved={continueSaved.ToString().ToLowerInvariant()}", level);
        TraceOriginalMount("root-created", level, level, ".");
        // Reuse the actual independent sample's production geometry and runtime modes.
        // Unlike BasinQualitySampleEntry's fixed camp start, this controlled fixture
        // arranges only its upper-shelf spawn before entering the tree. All later
        // crossings, recovery and Continue still use shared production controllers.
        level.FreezeWorldWhenGameplayInactive = true;
        level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position = UpperSpawn;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TraceOriginalMount("world-before-config", level, world, "WorldRoot/Sectors");
        DescentClawGuidanceRuntimeTests.TraceSectorDefinitions(world, "basin.before-replace");
#if REFCOUNTED_FATAL_IDENTITY
        if(_traceCompleteLifetime) ObserveWithOwnedRoot(level,root=>
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveAssignedSectorDefinitions(
                root.GetInstanceId(),world,"basin.before-replace"));
#endif
        BasinQualitySampleEntry.ConfigureLegacyWorld(level);
        DescentClawGuidanceRuntimeTests.TraceSectorDefinitions(world, "basin.after-replace");
#if REFCOUNTED_FATAL_IDENTITY
        if(_traceCompleteLifetime) ObserveWithOwnedRoot(level,root=>
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveAssignedSectorDefinitions(
                root.GetInstanceId(),world,"basin.after-replace"));
#endif
        TraceOriginalMount("world-after-config", level, world, "WorldRoot/Sectors");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        TraceOriginalMount("flow-config", level, flow, "GameFlow");
        flow.BasinQualitySampleMode = true;
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = !continueSaved;
        IsolatedPrologueFixture.ConfigureExisting(level, savePath);
        level.GetNode<PrologueCompletionController>("Completion").BasinQualitySampleMode = true;
        level.GetNode<PrologueUiRoot>("ProductUi").BasinQualitySampleMode = true;
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        guidance.SetGuidanceEnabled(false);
        guidance.ProcessMode = Node.ProcessModeEnum.Disabled;
        TraceOriginalMount("guidance-configured", level, guidance, "Guidance");
        return level;
    }

#if REFCOUNTED_FATAL_IDENTITY
    internal static void CheckAssignedProfileObservationFailure(SceneTree tree)
    {
        using var packed=new PackedScene();
        var unrelated=new Node {Name="UnrelatedSupplyObservationOwner"};
        try
        {
            tree.Root.AddChild(unrelated);
            TestAssert.True(GodotObject.IsInstanceValid(unrelated) &&
                unrelated.GetParent()?.GetInstanceId()==tree.Root.GetInstanceId(),
                "owner control belongs to the real SceneTree root before initialization finishes");
            var template=new Node {Name="OffTreeObservationOwner"};
            try {TestAssert.Equal(Error.Ok,packed.Pack(template),"owner control packs a real native root");}
            finally {template.Free();}
            for(int index=0;index<2;index++)
            {
                Node created=packed.Instantiate<Node>();
                var primary=new InvalidOperationException("assigned profile observation control");
                var cleanup=new InvalidOperationException("assigned profile cleanup control");
                Exception observed=null;
                try
                {
                    ObserveWithOwnedRoot(created,level=>throw primary,
                        index==0?null:level=>{level.Free();throw cleanup;});
                }
                catch(Exception error) {observed=error;}
                try
                {
                    bool exact=index==0?ReferenceEquals(primary,observed):
                        observed is AggregateException errors && errors.InnerExceptions.Count==2 &&
                        ReferenceEquals(primary,errors.InnerExceptions[0]) && ReferenceEquals(cleanup,errors.InnerExceptions[1]);
                    GD.Print($"SUPPLY_PROFILE_OWNER_PARTS index={index} managed_nonnull={!ReferenceEquals(created,null)} " +
                        $"native_valid={GodotObject.IsInstanceValid(created)} exact={exact} " +
                        $"unrelated_valid={GodotObject.IsInstanceValid(unrelated)} inside={unrelated.IsInsideTree()} " +
                        $"same_parent={ReferenceEquals(unrelated.GetParent(),tree.Root)}");
                    TestAssert.True(created!=null && !GodotObject.IsInstanceValid(created) && exact &&
                        GodotObject.IsInstanceValid(unrelated) &&
                        unrelated.GetParent()?.GetInstanceId()==tree.Root.GetInstanceId(),
                        "SUPPLY_PROFILE_OWNER_RED: observation failure releases its actual instantiated root and preserves exact errors, not unrelated owners");
                }
                finally {if(GodotObject.IsInstanceValid(created)) created.Free();}
            }
            GD.Print("SUPPLY_PROFILE_OWNER_CONTRACT_OK cases=2 original_exception=true cleanup_exception=true actual_instantiated_root_released=true unrelated_retained=true full_supply_changed=false");
        }
        finally {unrelated.Free();}
    }

    internal static void ObserveWithOwnedRoot<T>(T level,Action<T> observe,Action<T> release=null) where T:Node
    {
        try {observe(level);}
        catch(Exception primary)
        {
            // No caller receives the root if observation fails before return.
            try
            {
                if(release==null) level.Free();
                else release(level);
            }
            catch(Exception cleanup) {throw new AggregateException(primary,cleanup);}
            throw;
        }
    }

    // Observe the actual assigned field synchronously. Never retain this borrowed
    // Resource across an await, dispose it, or infer ownership from allocation order.
    private static void ObserveAssignedPlayerProfile(PrologueLevelController level)
    {
        var presentation=level.GetNode<PlayerPresentationCoordinator>("PlayerRoot/Player/Presentation");
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResource(presentation.Profile,
            level.GetInstanceId(), "PlayerRoot/Player/Presentation/Profile",
            "BasinSupply.assigned-player-profile", "basin-supply.after-original-instantiate");
        var motor=level.GetNode<Knight.Player.PlayerMotor>("PlayerRoot/Player/Motor");
        var combat=level.GetNode<Knight.Player.PlayerCombat>("PlayerRoot/Player/Combat");
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResource(motor.Tuning,
            level.GetInstanceId(), "PlayerRoot/Player/Motor/Tuning",
            "BasinSupply.assigned-motor-tuning", "basin-supply.after-original-instantiate");
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResource(combat.Tuning,
            level.GetInstanceId(), "PlayerRoot/Player/Combat/Tuning",
            "BasinSupply.assigned-combat-tuning", "basin-supply.after-original-instantiate");
        GD.Print($"SUPPLY_ASSIGNED_TUNING_ALIAS root={level.GetInstanceId()} " +
            $"same_wrapper={ReferenceEquals(motor.Tuning,combat.Tuning)} " +
            $"motor_serial={motor.Tuning.Task14ManagedSerial} combat_serial={combat.Tuning.Task14ManagedSerial} borrowed=true");
    }
#endif

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
