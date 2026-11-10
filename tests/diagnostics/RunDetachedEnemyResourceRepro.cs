#if REFCOUNTED_FATAL_IDENTITY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Knight.Build.Prologue;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Ranged;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
using Knight.Prologue.Residents;
using Knight.Tests.Integration;

namespace Knight.Tests.Diagnostics;

// Characterizes whether detached saved-scene load/instantiate/free is sufficient
// to reproduce the invalid resource binding without world/flow/physics owners.
public partial class RunDetachedEnemyResourceRepro : SceneTree
{
    private PackedScene _parentCarrier;
    public override async void _Initialize()
    {
        int mounted = 0, roots = 0, residentRoots = 0; bool subscribed = false;
        void Added(Node node) { if (node is CharacterBody2D) mounted++; }
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string heldPhase = args.SingleOrDefault(x => x is "--phase=resident-cache-held-load" or
                "--phase=resident-cache-held-factory");
            if (heldPhase != null)
            {
                TestAssert.True(args.Length == 2 && args.Count(x => x.StartsWith("--case-storage-root=")) == 1,
                    "exact held-cache boundary arguments");
                _ = IsolatedPrologueFixture.ValidateStorageRoot();
                string arm = heldPhase.EndsWith("-load", StringComparison.Ordinal) ? "load" : "factory";
                RunHeldCacheWindow(arm);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("held-cache-after-root-drop-exit-only");
                GD.Print($"HELD_CACHE_DIAGNOSTIC_COMPLETE arm={arm} behavior_accepted=false resource_fix=false");
                Quit(0);
                return;
            }
            string cacheWindow = args.SingleOrDefault(x => x.StartsWith("--phase=resident-cache-", StringComparison.Ordinal));
            if (cacheWindow != null)
            {
                string mode = cacheWindow.Substring("--phase=resident-cache-".Length);
                TestAssert.True(args.Length == 2 && args.Count(x => x.StartsWith("--case-storage-root=")) == 1 &&
                    mode is "none" or "reuse" or "ignore" or "deep", "exact independent cache-window args");
                _ = IsolatedPrologueFixture.ValidateStorageRoot();
                RunIndependentCacheWindow(mode);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("independent-cache-window-exit-only");
                GD.Print($"CACHE_WINDOW_DIAGNOSTIC_COMPLETE mode={mode} behavior_accepted=false resource_fix=false");
                Quit(0);
                return;
            }
            bool sourceLifetime = args.Contains("--phase=resident-resource-source-unrooted") ||
                args.Contains("--phase=resident-resource-source-rooted");
            if (sourceLifetime)
            {
                TestAssert.True(args.Length == 2 && args.Count(x => x.StartsWith("--case-storage-root=")) == 1,
                    "exact source lifetime args");
                _ = IsolatedPrologueFixture.ValidateStorageRoot();
                RunSourceLifetime(args.Contains("--phase=resident-resource-source-rooted"));
                RunCachedSourceFinalizerWindow(args.Contains("--phase=resident-resource-source-rooted"));
                if (args.Contains("--phase=resident-resource-source-rooted")) RunAllResidentSceneCustody();
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("source-lifetime-exit-only");
                GD.Print("SOURCE_LIFETIME_DIAGNOSTIC_COMPLETE behavior_accepted=false resource_fix=false");
                Quit(0);
                return;
            }
            bool parentCarrier = args.Contains("--phase=resident-resource-parent-repro");
            bool readyCase = args.Contains("--phase=resident-resource-ready-control") ||
                args.Contains("--phase=resident-resource-ready-repro");
            bool mountReady = args.Contains("--phase=resident-resource-ready-repro");
            string readyMode = mountReady ? "mounted" : "detached";
            TestAssert.True(args.Length == 2 && args.Count(x => x.StartsWith("--case-storage-root=")) == 1 &&
                args.Count(x => x == "--phase=resident-resource-repro" || x == "--phase=resident-resource-parent-repro" ||
                    x == "--phase=resident-resource-ready-control" || x == "--phase=resident-resource-ready-repro") == 1,
                "exact resource reproduction args");
            _ = IsolatedPrologueFixture.ValidateStorageRoot();
            Task14NativeResourceIdentity.BeginOriginalProbeTrace();
            if (parentCarrier)
            {
                // Controlled diagnostic variable, not a production cache/fix:
                // native dependency graph loaded but no parent or world mounted.
                _parentCarrier = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
                GD.Print("DETACHED_PARENT_CARRIER loaded=true instantiated=false scene_id=" + _parentCarrier.GetInstanceId());
            }
            string[] paths = CanyonEncounterCatalog.Normal.SelectMany(x => x.Enemies).Select(x => x.ScenePath)
                .Distinct(StringComparer.Ordinal).ToArray();
            TestAssert.Equal(9, paths.Length, "same nine saved original scene paths");
            NodeAdded += Added; subscribed = true;
            GD.Print("DETACHED_RESOURCE_REPRO_BEGIN gameplay=false physics_queries=0 borrowed_dispose=0 added_gc=0");
            for (int batch = 0; batch < 48; batch++)
            {
                var probes = new Dictionary<string, (CharacterBody2D Actor, long Operation, ulong[] Resources)>();
                try
                {
                    foreach (string path in paths)
                    {
                        string slot = "detached-batch-" + batch;
                        long operation = Task14NativeResourceIdentity.BeginOriginalProbe(slot, path);
                        CharacterBody2D probe = null;
                        try
                        {
                            var scene = GD.Load<PackedScene>(path);
                            Task14NativeResourceIdentity.OriginalProbeBoundary(operation, "load-return", slot, path, scene.GetInstanceId());
                            Task14NativeResourceIdentity.OriginalProbeBoundary(operation, "instantiate-enter", slot, path, scene.GetInstanceId());
                            probe = scene.Instantiate<CharacterBody2D>();
                            probes.Add(path, (probe, operation, Array.Empty<ulong>())); roots++;
                            Task14NativeResourceIdentity.OriginalProbeBoundary(operation, "instantiate-return", slot, path, probe.GetInstanceId());
                            ulong[] resources = Task14NativeResourceIdentity.ObserveOriginalProbe(probe, operation, slot, path);
                            probes[path] = (probe, operation, resources);
                            TestAssert.True(!probe.IsInsideTree(), "resource probe never mounts or runs Ready");
                            bool hasConfiguration = probe switch
                            {
                                GraveLampMoth actor => actor.Tuning != null,
                                BellBackLaborer actor => actor.Tuning != null,
                                BellBoltSentry actor => actor.Settings != null,
                                CanyonSkitter actor => actor.Settings != null,
                                RangedWeaponEnemy actor => actor.Settings != null,
                                _ => true
                            };
                            TestAssert.True(hasConfiguration, "saved detached configuration must remain assigned: " + path);
                        }
                        finally { Task14NativeResourceIdentity.EndOriginalProbe(operation); }
                    }
                }
                finally
                {
                    foreach (var entry in probes)
                    {
                        var value = entry.Value;
                        if (!GodotObject.IsInstanceValid(value.Actor)) continue;
                        Task14NativeResourceIdentity.OriginalProbeBoundary(value.Operation, "free-enter", "detached-batch-" + batch, entry.Key, value.Actor.GetInstanceId());
                        value.Actor.Free();
                        Task14NativeResourceIdentity.OriginalProbeBoundary(value.Operation, "free-return", "detached-batch-" + batch, entry.Key, 0);
                        Task14NativeResourceIdentity.OriginalProbeLiveness(value.Operation, value.Resources);
                    }
                }
                if (batch % 4 == 3)
                {
                    await ToSignal(this, SignalName.PhysicsFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                }
            }
            TestAssert.True(roots == 432 && mounted == 0, "all probes remain detached; no gameplay scene or physics queries");
            var types = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001);
            var authored = ResidentAuthoredLayout.Load();
            TestAssert.Equal(24, types.Count, "same24 published saved resident leaves, not original carrier aliases");
            TestAssert.Equal(24, types.Select(type => type.ScenePath).Distinct(StringComparer.Ordinal).Count(),
                "every subtype has its own published leaf");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            GD.Print("DETACHED_RESIDENT_FACTORY_BEGIN batches=20 leaves=24 original_carrier_prefix=432 tree=false ready=false added_getstate=0 added_gc=0 resource_fix=false");
            for (int batch = 0; batch < 20; batch++)
            {
                residentRoots += RunResidentBatch(batch, types, authored, seen);
                if (batch % 4 == 3)
                {
                    await ToSignal(this, SignalName.PhysicsFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                }
            }
            TestAssert.Equal(480, residentRoots,
                "RESIDENT_DETACHED_FACTORY_RED: all24 saved subtype leaves must traverse original factory and Free in20 rounds");
            TestAssert.True(seen.Count == 24 && mounted == 0,
                "all24 published resident factory leaves stay detached through release");
            GD.Print("DETACHED_RESIDENT_FACTORY_COMPLETE batches=20 leaves=24 roots=480 freed=480 mounted=0 actual_factory=true ready=false original_carrier_prefix=432 behavior_accepted=false");
            if (readyCase)
            {
                RunResidentReadyCase(types, authored, mountReady);
                // Same post-free deferred opportunity in both arms. No await or
                // physics step occurs while the added actor is mounted.
                await ToSignal(this, SignalName.ProcessFrame);
                TestAssert.Equal(mountReady ? 1 : 0, mounted,
                    "only the added mounted arm enters the tree");
            }
            TestAssert.True(Task14NativeResourceIdentity.EndResidentAssignmentTrace(),
                "actual saved factory assignments have complete scalar serial/leaf/slot evidence");
            _parentCarrier = null;
            if (readyCase) Console.Error.WriteLine("READY_REPRO_EXIT_DRAIN boundary=ENTER mode=" + readyMode + " original_gc=true added_gc=0");
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("detached-resource-repro");
            if (readyCase)
            {
                Console.Error.WriteLine("READY_REPRO_EXIT_DRAIN boundary=RETURN mode=" + readyMode + " original_gc=true added_gc=0");
                GD.Print("RESIDENT_READY_REPRO_COMPLETE mode=" + readyMode +
                    " original_carrier_prefix=432 factory_prefix=480 operations=481 mounted=" + mounted +
                    " additional_process_wait=1 behavior_accepted=false");
            }
            else GD.Print("DETACHED_RESOURCE_REPRO_COMPLETE batches=48 roots=432 mounted=0 gameplay=false physics_queries=0 behavior_accepted=false");
            Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("detached-resource-repro-failed");
            Quit(1);
        }
        finally
        {
            _parentCarrier = null;
            Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            if (subscribed) NodeAdded -= Added;
        }
    }

    private sealed record SourceSeed(WeakReference Weak, ResidentSubtypeProfile Strong,
        ulong NativeId, long Serial, int Hash, bool Freed, int Calls, int NullCalls);
    private readonly record struct SourceSecond(string Outcome, ulong NativeId, long Serial);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ResidentSubtypeProfile ReadSourceProfile(PackedScene scene)
    {
        // Same saved graph. These native objects are borrowed; only the value
        // Variant/NodePath/StringName containers below belong to this helper.
        SceneState state = scene.GetState();
        TestAssert.True(GodotObject.IsInstanceValid(state) && state.GetBaseSceneState() == null,
            "source lifetime requires a direct saved graph");
        ResidentSubtypeProfile found = null;
        for (int node = 0; node < state.GetNodeCount(); node++)
        {
            using NodePath path = state.GetNodePath(node);
            if (path.ToString() is not ("ResidentBinding" or "./ResidentBinding")) continue;
            for (int property = 0; property < state.GetNodePropertyCount(node); property++)
            {
                using StringName name = state.GetNodePropertyName(node, property);
                if (name.ToString() != "Profile") continue;
                using Variant value = state.GetNodePropertyValue(node, property);
                TestAssert.True(found == null && value.VariantType == Variant.Type.Object,
                    "source lifetime needs exactly one object Profile");
                found = value.AsGodotObject() as ResidentSubtypeProfile;
            }
        }
        TestAssert.True(GodotObject.IsInstanceValid(found), "saved source Profile is valid before first factory");
        return found;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SourceSeed FirstSourceActor(PackedScene scene, ResidentSubtypeDefinition type,
        ResidentPopulationDefinition slot, bool retain)
    {
        ResidentSubtypeProfile source = ReadSourceProfile(scene);
        CharacterBody2D actor = null;
        SourceSeed result;
        try
        {
            actor = ResidentSceneBinding.Instantiate(scene, type, slot, allowPlaceholder: true);
            var binding = actor.GetNode<ResidentSceneBinding>("ResidentBinding");
            var setter = binding.ProfileAssignments;
            TestAssert.True(!actor.IsInsideTree() && !source.ResourceLocalToScene &&
                ReferenceEquals(source, binding.Profile) && source.GetInstanceId() == binding.Profile.GetInstanceId() &&
                setter.Calls == 1 && setter.NullCalls == 0 && !setter.LastWasNull && setter.Faults == 0,
                "source and first actual factory binding share the non-local native/CLR Profile and one nonnull setter");
            result = new(new WeakReference(source), retain ? source : null, source.GetInstanceId(),
                source.Task14ManagedSerial, RuntimeHelpers.GetHashCode(source), false, setter.Calls, setter.NullCalls);
        }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
        TestAssert.True(!GodotObject.IsInstanceValid(actor), "source lifetime first sole actor is freed before helper returns");
        return result with { Freed = true };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CollectSourceOnce(SourceSeed seed)
    {
        // Deliberate test-only midpoint, exactly once. No weak Target retrieval,
        // retry-to-collect, physics/world step or extra production cache root.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        bool alive = seed.Weak.IsAlive;
        GC.KeepAlive(seed.Strong);
        return alive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SourceSecond SecondSourceActor(PackedScene scene, ResidentSubtypeDefinition type,
        ResidentPopulationDefinition slot)
    {
        CharacterBody2D actor = null;
        try
        {
            actor = ResidentSceneBinding.Instantiate(scene, type, slot, allowPlaceholder: true);
            var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            TestAssert.True(!actor.IsInsideTree() && GodotObject.IsInstanceValid(profile),
                "second actual factory returns a detached actor with real assigned Profile");
            return new("assigned", profile.GetInstanceId(), profile.Task14ManagedSerial);
        }
        catch (ArgumentException error) when (
            error.Message.StartsWith("Require one exported binding and explicit admission", StringComparison.Ordinal) &&
            error.Message.Contains("profile_missing=True", StringComparison.Ordinal))
        {
            // The original factory owns/frees its rejected root. Its actual
            // assignment trace, not this message alone, proves a null setter.
            return new("null-rejected", 0, 0);
        }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunSourceLifetime(bool retain)
    {
        var type = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001).Single(x => x.Id == "scrap-scatter");
        var slot = ResidentAuthoredLayout.Load().Single(x => x.Definition.StableId ==
            "canyon.v2.resident.low-ceiling-bridge.03").Definition;
        PackedScene saved = GD.Load<PackedScene>(type.ScenePath); // The only load in either arm.
        ulong sceneId = saved.GetInstanceId();
        SourceSeed seed = null;
        bool? weakAlive = null;
        SourceSecond second = default;
        int sequence = 0, collections = 0;
        void Row(string stage)
        {
            TestAssert.True(saved.GetInstanceId() == sceneId, "same held PackedScene across midpoint GC");
            Console.Error.WriteLine("SOURCE_LIFETIME_ROW " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Sequence = ++sequence, Stage = stage, Mode = retain ? "rooted" : "unrooted",
                Path = type.ScenePath, Slot = slot.StableId, SceneId = sceneId,
                ProfileId = seed?.NativeId ?? 0, ManagedSerial = seed?.Serial ?? 0,
                ManagedHash = seed?.Hash ?? 0, LocalToScene = false,
                FirstFreed = seed?.Freed ?? false, FirstSetterCalls = seed?.Calls ?? 0,
                FirstSetterNulls = seed?.NullCalls ?? 0, Collections = collections,
                StrongRoot = retain, WeakAlive = weakAlive,
                SecondOutcome = second.Outcome ?? "", SecondNativeId = second.NativeId,
                SecondSerial = second.Serial
            }));
            Console.Error.Flush();
        }
        Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
        Row("BEGIN");
        seed = FirstSourceActor(saved, type, slot, retain); // helper stack exits before Collect.
        Row("FIRST");
        collections = 1;
        Row("GC_ENTER");
        weakAlive = CollectSourceOnce(seed);
        Row("GC_RETURN");
        TestAssert.True(!retain || weakAlive == true, "a collected B strong root invalidates this experiment");
        second = SecondSourceActor(saved, type, slot);
        Row("SECOND");
        TestAssert.True(Task14NativeResourceIdentity.EndResidentAssignmentTrace(), "source lifetime actual two assignments are complete");
        Row("COMPLETE");
        GC.KeepAlive(seed.Strong);
        GC.KeepAlive(saved);
    }

    private sealed class ResurrectionSeed
    {
        internal WeakReference Scene;
        internal WeakReference Profile;
        internal ResidentSubtypeProfile Strong;
        internal ulong ProfileId;
        internal WeakReference Settings;
        internal RangedWeaponSettings SettingsStrong;
        internal ulong SettingsId;
    }

    private readonly record struct ResurrectionSecond(string Outcome, ulong ProfileId, ulong SettingsId);

    private sealed class FinalizerWindowSignals
    {
        internal readonly ManualResetEventSlim Entered = new(false);
        internal readonly ManualResetEventSlim Release = new(false);
        internal readonly ManualResetEventSlim Returned = new(false);
        internal int TimedOut;
    }

    // Test-only bounded CLR gate. The finalizer never touches Godot or writes
    // diagnostics; finally releases it even when setup or the actual load fails.
    private sealed class FinalizerWindowGate
    {
        private readonly FinalizerWindowSignals _signals;
        internal FinalizerWindowGate(FinalizerWindowSignals signals) => _signals = signals;
        ~FinalizerWindowGate()
        {
            _signals.Entered.Set();
            if (!_signals.Release.Wait(5000)) Interlocked.Exchange(ref _signals.TimedOut, 1);
            _signals.Returned.Set();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ArmFinalizerWindow(FinalizerWindowSignals signals) =>
        _ = new FinalizerWindowGate(signals);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropResurrectionRoot(ResurrectionSeed seed) => seed.Strong = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ResurrectionSeed CreateResurrectionSeed(ResidentSubtypeDefinition type,
        ResidentPopulationDefinition slot)
    {
        PackedScene saved = GD.Load<PackedScene>(type.ScenePath);
        CharacterBody2D actor = null;
        try
        {
            actor = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder: true);
            ResidentSubtypeProfile profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            RangedWeaponSettings settings = ((RangedWeaponEnemy)actor).Settings;
            TestAssert.True(GodotObject.IsInstanceValid(profile) && !profile.ResourceLocalToScene &&
                GodotObject.IsInstanceValid(settings) && !settings.ResourceLocalToScene,
                "cached-source window begins with valid shared saved Profile and sibling Settings");
            return new ResurrectionSeed
            {
                Scene = new WeakReference(saved), Profile = new WeakReference(profile),
                Strong = profile, ProfileId = profile.GetInstanceId(),
                Settings = new WeakReference(settings), SettingsStrong = settings,
                SettingsId = settings.GetInstanceId()
            };
        }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ResurrectionSecond ReloadResurrectionActor(ResidentSubtypeDefinition type,
        ResidentPopulationDefinition slot)
    {
        CharacterBody2D actor = null;
        try
        {
            actor = ResidentSceneBinding.Instantiate(GD.Load<PackedScene>(type.ScenePath), type, slot,
                allowPlaceholder: true);
            var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            var settings = ((RangedWeaponEnemy)actor).Settings;
            TestAssert.True(!actor.IsInsideTree() && GodotObject.IsInstanceValid(profile) &&
                GodotObject.IsInstanceValid(settings),
                "cached-source reload returns both actual saved resources on the detached actor");
            return new("assigned", profile.GetInstanceId(), settings.GetInstanceId());
        }
        catch (ArgumentException error) when (
            error.Message.StartsWith("Require one exported binding and explicit admission", StringComparison.Ordinal) &&
            error.Message.Contains("profile_missing=True", StringComparison.Ordinal))
        {
            return new("null-rejected", 0, 0);
        }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunCachedSourceFinalizerWindow(bool retain)
    {
        var type = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001).Single(x => x.Id == "scrap-scatter");
        var slot = ResidentAuthoredLayout.Load().Single(x => x.Definition.StableId ==
            "canyon.v2.resident.low-ceiling-bridge.03").Definition;
        // Unlike the original held-PackedScene experiment above, its creating
        // helper exits and no caller holds that scene across this midpoint.
        ResurrectionSeed seed = CreateResurrectionSeed(type, slot);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        bool sceneAlive = seed.Scene.IsAlive;
        bool beforeAlive = seed.Profile.IsAlive;
        bool settingsBeforeAlive = seed.Settings.IsAlive;
        var signals = new FinalizerWindowSignals();
        ResurrectionSecond second = default;
        bool entered = false, afterAlive = false, settingsAfterAlive = false, returned = false;
        int sequence = 0;
        void Row(string stage)
        {
            Console.Error.WriteLine("SOURCE_RESURRECTION_ROW " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Sequence = ++sequence, Stage = stage, Mode = retain ? "rooted" : "unrooted",
                Path = type.ScenePath, Slot = slot.StableId, ProfileId = seed.ProfileId,
                SceneAlive = sceneAlive, BeforeAlive = beforeAlive, GateEntered = entered,
                StrongRoot = seed.Strong != null, AfterAlive = afterAlive,
                SettingsId = seed.SettingsId, SettingsStrongRoot = seed.SettingsStrong != null,
                SettingsBeforeAlive = settingsBeforeAlive, SettingsAfterAlive = settingsAfterAlive,
                GateReturned = returned, GateTimedOut = Volatile.Read(ref signals.TimedOut) != 0,
                Outcome = second.Outcome ?? "", ReloadProfileId = second.ProfileId,
                ReloadSettingsId = second.SettingsId
            }));
            Console.Error.Flush();
        }
        Row("PRIMED");
        TestAssert.True(!sceneAlive && beforeAlive && settingsBeforeAlive,
            "SOURCE_RESURRECTION_NOT_EXERCISED: scene wrapper must be collected while both explicit resource roots are alive");
        try
        {
            ArmFinalizerWindow(signals);
            GC.Collect();
            entered = signals.Entered.Wait(1000);
            Row("GATE");
            TestAssert.True(entered, "SOURCE_RESURRECTION_NOT_EXERCISED: bounded finalizer gate did not enter");
            if (!retain) DropResurrectionRoot(seed);
            GC.Collect(); // No wait: the bounded gate owns the finalizer thread.
            afterAlive = seed.Profile.IsAlive;
            settingsAfterAlive = seed.Settings.IsAlive;
            Row("COLLECTED");
            TestAssert.True(afterAlive == retain && settingsAfterAlive &&
                Volatile.Read(ref signals.TimedOut) == 0,
                "SOURCE_RESURRECTION_NOT_EXERCISED: exact rooted/live versus unrooted/collected midpoint was not established");
            second = ReloadResurrectionActor(type, slot);
            Row("RELOADED");
        }
        finally
        {
            signals.Release.Set();
            returned = signals.Returned.Wait(1000);
            Row("RELEASED");
        }
        TestAssert.True(returned && Volatile.Read(ref signals.TimedOut) == 0,
            "SOURCE_RESURRECTION_NOT_EXERCISED: finalizer gate did not return within its bound");
        TestAssert.True(second.Outcome == "assigned" && second.ProfileId != 0 &&
            second.SettingsId == seed.SettingsId,
            "SOURCE_RESURRECTION_RED: actual saved factory must retain a nonnull Profile through cached source reacquisition");
        TestAssert.True(!retain || second.ProfileId == seed.ProfileId,
            "rooted cached source reuses the actual live Profile identity");
        GC.KeepAlive(seed.Strong);
        GC.KeepAlive(seed.SettingsStrong);
        Row("COMPLETE");
    }

    private sealed class HeldSceneCarrier { internal PackedScene Scene; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadHeldScene(HeldSceneCarrier carrier, string path) =>
        carrier.Scene = GD.Load<PackedScene>(path);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (bool Held, int Hash, string Pointer) ObserveHeldScene(HeldSceneCarrier carrier)
    {
        // Managed-only field/hash reads; never ObjectID, IsInstanceValid, State,
        // exported children, or WeakReference.Target in the load-only arm.
        PackedScene saved = carrier.Scene;
        return saved is null ? (false, 0, "0x0000000000000000") :
            (true, RuntimeHelpers.GetHashCode(saved), $"0x{saved.NativeInstance.ToInt64():X16}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropHeldScene(HeldSceneCarrier carrier) => carrier.Scene = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CacheWindowReload ContinueHeldFactory(HeldSceneCarrier carrier,
        ResidentSubtypeDefinition type, ResidentPopulationDefinition slot, CacheWindowSeed seed)
    {
        CharacterBody2D actor = null;
        try
        {
            // This is the same retained scene, NOT a second Load. All factory
            // validation/native queries and Free belong to this whole B arm.
            PackedScene saved = carrier.Scene;
            actor = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder: true);
            var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            var settings = ((RangedWeaponEnemy)actor).Settings;
            var profileValues = CacheWindowValues(profile, 7);
            var settingsValues = CacheWindowValues(settings, 23);
            bool equal = seed.ProfileValues.All(x => profileValues.TryGetValue(x.Key, out string value) && value == x.Value) &&
                seed.SettingsValues.All(x => settingsValues.TryGetValue(x.Key, out string value) && value == x.Value);
            TestAssert.True(!actor.IsInsideTree() && GodotObject.IsInstanceValid(profile) && GodotObject.IsInstanceValid(settings),
                "held factory uses original detached saved resources");
            return new("assigned", profile.GetInstanceId(), settings.GetInstanceId(),
                profile.Task14ManagedSerial, settings.Task14ManagedSerial, equal, new(profile), new(settings));
        }
        catch (ArgumentException error) when (error.Message.StartsWith("Require one exported binding and explicit admission", StringComparison.Ordinal) &&
            error.Message.Contains("profile_missing=True", StringComparison.Ordinal))
        { return new("null-rejected"); }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunHeldCacheWindow(string arm)
    {
        TestAssert.True(arm is "load" or "factory", "exact held cache arm");
        var type = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001).Single(x => x.Id == "scrap-scatter");
        var slot = ResidentAuthoredLayout.Load().Single(x => x.Definition.StableId ==
            "canyon.v2.resident.low-ceiling-bridge.03").Definition;
        CacheWindowSeed seed = PrimeIndependentCacheWindow(type, slot);
        GC.Collect(); GC.WaitForPendingFinalizers();
        bool sceneAlive = seed.Scene.IsAlive, profileBefore = seed.Profile.IsAlive, settingsBefore = seed.Settings.IsAlive;
        var signals = new FinalizerWindowSignals();
        var carrier = new HeldSceneCarrier();
        CacheWindowReload reload = new("not-requested");
        bool entered = false, profileAfter = false, settingsAfter = false, returned = false;
        int sequence = 0;
        void Row(string stage)
        {
            var identity = ObserveHeldScene(carrier);
            Console.Error.WriteLine("HELD_CACHE_ROW " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Sequence = ++sequence, Stage = stage, Arm = arm, Path = type.ScenePath, Slot = slot.StableId,
                seed.ProfileSerial, seed.SettingsSerial,
                ProfileFields = seed.ProfileValues.Count, SettingsFields = seed.SettingsValues.Count,
                SceneAlive = sceneAlive, ProfileBefore = profileBefore, SettingsBefore = settingsBefore,
                GateEntered = entered, ProfileAfter = profileAfter, SettingsAfter = settingsAfter,
                GateReturned = returned, GateTimedOut = Volatile.Read(ref signals.TimedOut) != 0,
                identity.Held, ManagedHash = identity.Hash, NativePointer = identity.Pointer,
                reload.Outcome, reload.ValuesEqual
            }));
            Console.Error.Flush();
        }
        try
        {
            Row("PRIMED");
            TestAssert.True(!sceneAlive && profileBefore && settingsBefore,
                "HELD_CACHE_NOT_EXERCISED: original scene weak-dead while both old resource roots remain alive");
            Exception operationError = null, cleanupError = null;
            try
            {
                ArmFinalizerWindow(signals); GC.Collect(); entered = signals.Entered.Wait(1000);
                Row("GATE");
                TestAssert.True(entered, "HELD_CACHE_NOT_EXERCISED: original bounded finalizer gate not entered");
                DropIndependentCacheRoots(seed); GC.Collect();
                profileAfter = seed.Profile.IsAlive; settingsAfter = seed.Settings.IsAlive;
                Row("COLLECTED");
                TestAssert.True(!profileAfter && !settingsAfter && Volatile.Read(ref signals.TimedOut) == 0,
                    "HELD_CACHE_NOT_EXERCISED: both original resource wrappers weak-dead before operation");
                Row("LOAD_BEGIN");
                LoadHeldScene(carrier, type.ScenePath);
                Row("LOADED");
                TestAssert.True(carrier.Scene is not null, "held default scene load returned nonnull");
                reload = arm == "factory" ? ContinueHeldFactory(carrier, type, slot, seed) : new("load-only");
                Row("CONTINUATION_END");
            }
            catch (Exception error) { operationError = error; }
            finally
            {
                // Log before release: resource finalizers can run before the
                // main thread emits RELEASED. A logging failure never holds gate.
                try { Row("RELEASE_BEGIN"); }
                catch (Exception error) { cleanupError = error; }
                finally { signals.Release.Set(); returned = signals.Returned.Wait(1000); }
                try { Row("RELEASED"); }
                catch (Exception error) { cleanupError = cleanupError == null ? error : new AggregateException(cleanupError, error); }
            }
            if (operationError != null || cleanupError != null)
                throw new AggregateException("held operation/release failed", new[] { operationError, cleanupError }.Where(x => x != null));
            TestAssert.True(returned && Volatile.Read(ref signals.TimedOut) == 0,
                "HELD_CACHE_NOT_EXERCISED: finalizer sentinel did not return inside unchanged bound");
            GC.Collect(); GC.WaitForPendingFinalizers();
            GC.KeepAlive(carrier.Scene);
            Row("HELD_DRAIN_RETURN");
            TestAssert.True(Volatile.Read(ref signals.TimedOut) == 0,
                "held finalizer drain did not exceed original gate bound");
            DropHeldScene(carrier);
            Row("ROOT_DROPPED");
            GC.Collect(); GC.WaitForPendingFinalizers();
            Row("COMPLETE");
        }
        finally
        {
            signals.Release.Set();
            DropHeldScene(carrier);
        }
    }

    private sealed class CacheWindowSeed
    {
        internal WeakReference Scene, Profile, Settings;
        internal ResidentSubtypeProfile ProfileRoot;
        internal RangedWeaponSettings SettingsRoot;
        internal ulong ProfileId, SettingsId;
        internal long ProfileSerial, SettingsSerial;
        internal Dictionary<string, string> ProfileValues, SettingsValues;
    }

    private sealed record CacheWindowReload(string Outcome, ulong ProfileId = 0, ulong SettingsId = 0,
        long ProfileSerial = 0, long SettingsSerial = 0, bool ValuesEqual = false,
        WeakReference Profile = null, WeakReference Settings = null);

    // No resources escape through the value comparison: all current exported
    // fields are scalar. A future non-scalar field fails this characterization.
    private static Dictionary<string, string> CacheWindowValues(Resource resource, int expected)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in resource.GetType().GetProperties())
        {
            if (!Attribute.IsDefined(property, typeof(ExportAttribute))) continue;
            object value = property.GetValue(resource);
            TestAssert.True(value is string or Enum or int || value is float f && float.IsFinite(f),
                "cache-window comparison supports every actual scalar exported value, never resource references");
            values.Add(property.Name, value is float number ? number.ToString("R", System.Globalization.CultureInfo.InvariantCulture) :
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
        }
        TestAssert.Equal(expected, values.Count, "all authored exported values, not just Validate()");
        return values;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CacheWindowSeed PrimeIndependentCacheWindow(ResidentSubtypeDefinition type,
        ResidentPopulationDefinition slot)
    {
        PackedScene saved = GD.Load<PackedScene>(type.ScenePath);
        CharacterBody2D actor = null;
        try
        {
            actor = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder: true);
            var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            var settings = ((RangedWeaponEnemy)actor).Settings;
            TestAssert.True(GodotObject.IsInstanceValid(profile) && GodotObject.IsInstanceValid(settings) &&
                !profile.ResourceLocalToScene && !settings.ResourceLocalToScene,
                "independent cache window starts with actual shared saved resources");
            return new()
            {
                Scene = new(saved), Profile = new(profile), Settings = new(settings),
                ProfileRoot = profile, SettingsRoot = settings,
                ProfileId = profile.GetInstanceId(), SettingsId = settings.GetInstanceId(),
                ProfileSerial = profile.Task14ManagedSerial, SettingsSerial = settings.Task14ManagedSerial,
                ProfileValues = CacheWindowValues(profile, 7), SettingsValues = CacheWindowValues(settings, 23)
            };
        }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropIndependentCacheRoots(CacheWindowSeed seed)
    { seed.ProfileRoot = null; seed.SettingsRoot = null; }

    // Only this diagnostic arm avoids the main scene/subresource cache. The
    // production loader and Reuse control remain unchanged. Ignore still reuses
    // external dependencies; Deep is the independent comparison of that factor.
    private static PackedScene LoadIndependentCacheScene(string path, string mode) => mode switch
    {
        "ignore" => ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore),
        "deep" => ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep),
        _ => GD.Load<PackedScene>(path)
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CacheWindowReload ReloadIndependentCacheWindow(ResidentSubtypeDefinition type,
        ResidentPopulationDefinition slot, CacheWindowSeed seed, string mode)
    {
        CharacterBody2D actor = null;
        try
        {
            PackedScene saved = LoadIndependentCacheScene(type.ScenePath, mode);
            actor = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder: true);
            var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            var settings = ((RangedWeaponEnemy)actor).Settings;
            var profileValues = CacheWindowValues(profile, 7);
            var settingsValues = CacheWindowValues(settings, 23);
            bool equal = seed.ProfileValues.All(x => profileValues.TryGetValue(x.Key, out string value) && value == x.Value) &&
                seed.SettingsValues.All(x => settingsValues.TryGetValue(x.Key, out string value) && value == x.Value);
            TestAssert.True(!actor.IsInsideTree() && GodotObject.IsInstanceValid(profile) && GodotObject.IsInstanceValid(settings),
                "independent reload uses original detached factory and both actual saved resources");
            return new("assigned", profile.GetInstanceId(), settings.GetInstanceId(),
                profile.Task14ManagedSerial, settings.Task14ManagedSerial, equal, new(profile), new(settings));
        }
        catch (ArgumentException error) when (error.Message.StartsWith("Require one exported binding and explicit admission", StringComparison.Ordinal) &&
            error.Message.Contains("profile_missing=True", StringComparison.Ordinal))
        { return new("null-rejected"); }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunIndependentCacheWindow(string mode)
    {
        var type = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001).Single(x => x.Id == "scrap-scatter");
        var slot = ResidentAuthoredLayout.Load().Single(x => x.Definition.StableId ==
            "canyon.v2.resident.low-ceiling-bridge.03").Definition;
        CacheWindowSeed seed = PrimeIndependentCacheWindow(type, slot);
        GC.Collect(); GC.WaitForPendingFinalizers();
        bool sceneAlive = seed.Scene.IsAlive, profileBefore = seed.Profile.IsAlive, settingsBefore = seed.Settings.IsAlive;
        var signals = new FinalizerWindowSignals();
        CacheWindowReload reload = new("not-requested");
        bool entered = false, profileAfter = false, settingsAfter = false, returned = false;
        bool oldReleased = false, newReleased = false;
        int sequence = 0;
        void Row(string stage)
        {
            Console.Error.WriteLine("CACHE_WINDOW_ROW " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Sequence = ++sequence, Stage = stage, Mode = mode, Path = type.ScenePath, Slot = slot.StableId,
                seed.ProfileId, seed.SettingsId, seed.ProfileSerial, seed.SettingsSerial,
                ProfileFields = seed.ProfileValues.Count, SettingsFields = seed.SettingsValues.Count,
                SceneAlive = sceneAlive, ProfileBefore = profileBefore, SettingsBefore = settingsBefore,
                GateEntered = entered, ProfileAfter = profileAfter, SettingsAfter = settingsAfter,
                GateReturned = returned, GateTimedOut = Volatile.Read(ref signals.TimedOut) != 0,
                reload.Outcome, ReloadProfileId = reload.ProfileId, ReloadSettingsId = reload.SettingsId,
                ReloadProfileSerial = reload.ProfileSerial, ReloadSettingsSerial = reload.SettingsSerial,
                reload.ValuesEqual, OldReleased = oldReleased, NewReleased = newReleased
            }));
            Console.Error.Flush();
        }
        Row("PRIMED");
        TestAssert.True(!sceneAlive && profileBefore && settingsBefore,
            "CACHE_WINDOW_NOT_EXERCISED: scene wrapper dead while both original resource roots are alive");
        try
        {
            ArmFinalizerWindow(signals); GC.Collect(); entered = signals.Entered.Wait(1000);
            Row("GATE");
            TestAssert.True(entered, "CACHE_WINDOW_NOT_EXERCISED: bounded finalizer gate not entered");
            DropIndependentCacheRoots(seed); GC.Collect();
            profileAfter = seed.Profile.IsAlive; settingsAfter = seed.Settings.IsAlive;
            Row("COLLECTED");
            TestAssert.True(!profileAfter && !settingsAfter && Volatile.Read(ref signals.TimedOut) == 0,
                "CACHE_WINDOW_NOT_EXERCISED: both old resources must be weak-dead while finalization is pending");
            if (mode != "none") reload = ReloadIndependentCacheWindow(type, slot, seed, mode);
            Row("RELOADED");
        }
        finally
        {
            signals.Release.Set(); returned = signals.Returned.Wait(1000); Row("RELEASED");
        }
        TestAssert.True(returned && Volatile.Read(ref signals.TimedOut) == 0,
            "CACHE_WINDOW_NOT_EXERCISED: bounded finalizer gate failed to return");
        GC.Collect(); GC.WaitForPendingFinalizers();
        oldReleased = !seed.Profile.IsAlive && !seed.Settings.IsAlive;
        newReleased = mode == "none" || reload.Profile != null && reload.Settings != null &&
            !reload.Profile.IsAlive && !reload.Settings.IsAlive;
        Row("FINALIZED");
        TestAssert.True(oldReleased && newReleased, "actual old/new resource wrappers must be released after gate completion");
        if (mode != "none")
        {
            TestAssert.True(reload.Outcome == "assigned" && reload.ValuesEqual && reload.ProfileId != 0 && reload.SettingsId != 0 &&
                reload.ProfileSerial > 0 && reload.SettingsSerial > 0,
                "CACHE_WINDOW_RED: original factory must receive both actual resources with all30 exported values unchanged");
            TestAssert.True(mode is not ("ignore" or "deep") || reload.ProfileId != seed.ProfileId && reload.SettingsId != seed.SettingsId &&
                reload.ProfileSerial != seed.ProfileSerial && reload.SettingsSerial != seed.SettingsSerial,
                "CACHE_WINDOW_RED: independent load must not resurrect old native or managed resource identities");
        }
        Row("COMPLETE");
    }

    private sealed record ManagedGraphWitness(string Path, string Type, ulong Id, WeakReference Weak);
    private sealed record SceneGraphWitness(WeakReference[] Roots, ManagedGraphWitness[] Managed, int Scenes);

    // Test-only reader. No returned Resource/SceneState/Variant or closure pins
    // the graph: only the existing PackedScene source owner may keep it alive.
    private sealed class ManagedGraphReader
    {
        internal readonly List<ManagedGraphWitness> Managed = new();
        private readonly HashSet<ulong> _resources = new(), _states = new();
        internal int Scenes;
        private int _values;
        internal void Resource(Resource resource, string path, int depth)
        {
            TestAssert.True(depth <= 32 && ++_values <= 16384 && GodotObject.IsInstanceValid(resource),
                "resident saved graph is valid and bounded");
            ulong id = resource.GetInstanceId();
            if (!_resources.Add(id)) return;
            TestAssert.True(_resources.Count <= 1024, "resident saved graph resource bound");
            if (resource.GetType().Assembly == typeof(ResidentSubtypeProfile).Assembly)
                Managed.Add(new(path, resource.GetType().Name, id, new WeakReference(resource)));
            if (resource is PackedScene scene)
            {
                Scenes++;
                State(scene.GetState(), path, depth + 1);
                return;
            }
            var properties = resource.GetPropertyList();
            using var propertiesOwner = (Godot.Collections.Array)properties;
            foreach (var property in properties)
            using (property)
            {
                using var name = property["name"]; using var usage = property["usage"];
                if ((usage.AsInt64() & (long)PropertyUsageFlags.Storage) == 0) continue;
                using Variant value = resource.Get(name.AsString());
                Value(value, path, depth + 1);
            }
        }
        private void State(SceneState state, string path, int depth)
        {
            TestAssert.True(depth <= 32 && GodotObject.IsInstanceValid(state), "valid bounded saved scene state");
            if (!_states.Add(state.GetInstanceId())) return;
            SceneState parent = state.GetBaseSceneState();
            if (GodotObject.IsInstanceValid(parent)) State(parent, path, depth + 1);
            TestAssert.True(state.GetNodeCount() <= 4096, "resident saved node bound");
            for (int node = 0; node < state.GetNodeCount(); node++)
            {
                PackedScene nested = state.GetNodeInstance(node);
                if (GodotObject.IsInstanceValid(nested)) Resource(nested, path, depth + 1);
                int count = state.GetNodePropertyCount(node);
                TestAssert.True(count <= 256, "resident saved node property bound");
                for (int property = 0; property < count; property++)
                {
                    using Variant value = state.GetNodePropertyValue(node, property);
                    Value(value, path, depth + 1);
                }
            }
        }
        private void Value(Variant value, string path, int depth)
        {
            TestAssert.True(depth <= 32 && ++_values <= 16384, "resident saved value bound");
            if (value.VariantType == Variant.Type.Object)
            {
                GodotObject obj = value.AsGodotObject();
                TestAssert.True(obj == null || obj is Resource, "saved graph object must be a resource, never an actor");
                if (obj is Resource resource) Resource(resource, path, depth + 1);
            }
            else if (value.VariantType == Variant.Type.Array)
            {
                using var array = value.AsGodotArray();
                for (int i = 0; i < array.Count; i++) { using Variant item = array[i]; Value(item, path, depth + 1); }
            }
            else if (value.VariantType == Variant.Type.Dictionary)
            {
                using var dictionary = value.AsGodotDictionary();
                foreach (var pair in dictionary)
                {
                    using Variant key = pair.Key; using Variant item = pair.Value;
                    Value(key, path, depth + 1); Value(item, path, depth + 1);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SceneGraphWitness CaptureResidentGraphs(ResidentOriginalSceneSources owner,
        IReadOnlyList<ResidentSubtypeDefinition> types)
    {
        var reader = new ManagedGraphReader(); var roots = new List<WeakReference>();
        foreach (var type in types)
        {
            PackedScene saved = owner.Get(type.ScenePath);
            TestAssert.True(saved.ResourcePath == type.ScenePath, "graph owner acquires the exact saved resident leaf");
            roots.Add(new WeakReference(saved)); reader.Resource(saved, type.ScenePath, 0);
        }
        return new(roots.ToArray(), reader.Managed.ToArray(), reader.Scenes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ValidManagedGraph(SceneGraphWitness graph)
    {
        // Observe ALL weak states before retrieving any target and temporarily
        // rooting it to verify the same native identity.
        int alive = graph.Managed.Count(x => x.Weak.IsAlive);
        if (alive != graph.Managed.Length) return alive;
        foreach (var item in graph.Managed)
        {
            var resource = item.Weak.Target as Resource;
            TestAssert.True(GodotObject.IsInstanceValid(resource) && resource.GetInstanceId() == item.Id,
                "scene-only custody retains the original valid managed/native resource, not just a dead CLR object");
        }
        return alive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ExerciseResidentGraphFactories(ResidentOriginalSceneSources owner,
        IReadOnlyList<ResidentSubtypeDefinition> types, SceneGraphWitness graph)
    {
        var authored = ResidentAuthoredLayout.Load(); int count = 0;
        foreach (var type in types)
        {
            var slot = authored.First(x => x.Definition.SubtypeId == type.Id).Definition;
            CharacterBody2D actor = null;
            try
            {
                actor = ResidentSceneBinding.Instantiate(owner.Get(type.ScenePath), type, slot, allowPlaceholder: true);
                var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
                ulong expected = graph.Managed.Single(x => x.Path == type.ScenePath && x.Type == nameof(ResidentSubtypeProfile)).Id;
                TestAssert.True(!actor.IsInsideTree() && !actor.IsNodeReady() && profile.GetInstanceId() == expected,
                    "all24 actual detached factories receive the original pre-factory saved Profile");
                count++;
            }
            finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunAllResidentSceneCustody()
    {
        var types = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001);
        TestAssert.True(types.Count == 24 && types.Select(x => x.ScenePath).Distinct().Count() == 24,
            "complete current resident leaf catalog, not a carrier subset");
        int loads = 0;
        using var owner = new ResidentOriginalSceneSources(types.Select(x => x.ScenePath), path =>
        { loads++; return GD.Load<PackedScene>(path); });
        SceneGraphWitness graph = CaptureResidentGraphs(owner, types);
        var expected = new Dictionary<string, int>
        {
            ["ResidentSubtypeProfile"] = 24, ["PresentationProfile"] = 15, ["EnemyArchetypeTuning"] = 8,
            ["BellBackLaborerTuning"] = 3, ["CanyonSkitterSettings"] = 4,
            ["RangedWeaponSettings"] = 4, ["BellBoltSentrySettings"] = 1
        };
        var actual = graph.Managed.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
        TestAssert.True(graph.Managed.Length == 59 && actual.Count == 7 &&
            expected.All(x => actual.TryGetValue(x.Key, out int count) && count == x.Value) && graph.Scenes >= 27,
            "all24 saved graphs include literal59 managed resources and three nested formal scenes");
        GC.Collect(); GC.WaitForPendingFinalizers();
        int before = ValidManagedGraph(graph);
        TestAssert.True(before == 59 && graph.Roots.All(x => x.IsAlive),
            "RESIDENT_SCENE_CUSTODY_RED: scene-only owner must hold all saved managed resources before first factory");
        int factories = ExerciseResidentGraphFactories(owner, types, graph);
        GC.Collect(); GC.WaitForPendingFinalizers();
        int after = ValidManagedGraph(graph);
        TestAssert.True(after == 59 && loads == 24 && factories == 24 && graph.Roots.All(x => x.IsAlive),
            "scene-only custody survives actual factory/free/secondGC without reloading the catalog");
        owner.Dispose();
        GC.Collect(); GC.WaitForPendingFinalizers();
        int released = graph.Roots.Count(x => !x.IsAlive);
        TestAssert.True(released == 24, "retired scene owner drops every root reference without Resource.Dispose");
        GD.Print($"RESIDENT_SCENE_CUSTODY_COMPLETE roots=24 managed=59 nested_scenes={graph.Scenes - 24} before={before} after={after} factories={factories} loads={loads} released={released} gameplay=false resource_fix=false");
        GC.KeepAlive(owner);
    }

    // A/B variable includes real tree entry/Ready/ExitTree, not a manual Ready
    // invocation. No synthetic world, player, pose or borrowed Resource disposal.
    private void RunResidentReadyCase(IReadOnlyList<ResidentSubtypeDefinition> types,
        IReadOnlyList<ResidentAuthoredSite> authored, bool mount)
    {
        var type = types.Single(item => item.Id == "scrap-scatter");
        var slot = authored.Single(site => site.Definition.StableId ==
            "canyon.v2.resident.low-ceiling-bridge.03").Definition;
        string mode = mount ? "mounted" : "detached";
        Console.Error.WriteLine("RESIDENT_READY_BEGIN mode=" + mode +
            " operation=481 saved_slot=" + slot.StableId + " added_gc=0");
        CharacterBody2D actor = null;
        int entered = 0, ready = 0, exiting = 0, exited = 0, order = 0;
        ulong physicsBefore = 0;
        bool inside = false, nodeReady = false, settingsAssigned = false;
        try
        {
            var saved = GD.Load<PackedScene>(type.ScenePath);
            actor = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder: true);
            actor.TreeEntered += () => { entered++; order = order * 10 + 1; };
            actor.Ready += () => { ready++; order = order * 10 + 2; };
            actor.TreeExiting += () => { exiting++; order = order * 10 + 3; };
            actor.TreeExited += () => { exited++; order = order * 10 + 4; };
            settingsAssigned = !ReferenceEquals(((RangedWeaponEnemy)actor).Settings, null);
            physicsBefore = Engine.GetPhysicsFrames();
            if (mount) Root.AddChild(actor);
            inside = actor.IsInsideTree();
            nodeReady = actor.IsNodeReady();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(actor)) actor.Free();
        }
        ulong physicsAfter = Engine.GetPhysicsFrames();
        bool released = !GodotObject.IsInstanceValid(actor);
        Console.Error.WriteLine("RESIDENT_READY_ROW " + System.Text.Json.JsonSerializer.Serialize(new
        {
            Operation = 481, Mode = mode, Slot = slot.StableId, Path = type.ScenePath,
            Mounted = mount, Entered = entered, Ready = ready, Exiting = exiting, Exited = exited,
            Order = order, InsideBeforeFree = inside, ReadyBeforeFree = nodeReady,
            SavedSettingsAssigned = settingsAssigned, PhysicsBefore = physicsBefore,
            PhysicsAfter = physicsAfter, Released = released
        }));
        Console.Error.Flush();
        int events = mount ? 1 : 0;
        TestAssert.True(released && settingsAssigned && inside == mount && nodeReady == mount &&
            entered == events && ready == events && exiting == events && exited == events &&
            order == (mount ? 1234 : 0) && physicsBefore == physicsAfter,
            "saved resident synchronous tree/Ready/ExitTree path must match its actual scalar events");
    }

    // Return scalars only. Do not pin the PackedScene, binding or Profile across
    // batches, dispose borrowed Resources, or synthesize a replacement Profile.
    private static int RunResidentBatch(int batch, IReadOnlyList<ResidentSubtypeDefinition> types,
        IReadOnlyList<ResidentAuthoredSite> authored, HashSet<string> seen)
    {
        int freed = 0;
        for (int index = 0; index < types.Count; index++)
        {
            var type = types[index];
            var slot = type.Id == "scrap-scatter"
                ? authored.Single(site => site.Definition.StableId == "canyon.v2.resident.low-ceiling-bridge.03").Definition
                : authored.First(site => site.Definition.SubtypeId == type.Id).Definition;
            CharacterBody2D actor = null;
            try
            {
                var saved = GD.Load<PackedScene>(type.ScenePath);
                actor = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder: true);
                TestAssert.True(!actor.IsInsideTree(), "saved resident factory must not mount or run Ready");
                seen.Add(type.ScenePath);
            }
            finally
            {
                // A throwing factory already owns its rejected root. This local
                // owns only successfully returned actors, never a borrowed Resource.
                if (GodotObject.IsInstanceValid(actor)) actor.Free();
            }
            TestAssert.True(!GodotObject.IsInstanceValid(actor), "returned resident root must be released");
            freed++;
            Console.Error.WriteLine("DETACHED_RESIDENT_RELEASE " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Operation = batch * types.Count + index + 1,
                Batch = batch, Slot = slot.StableId, Path = type.ScenePath, Released = true
            }));
            Console.Error.Flush();
        }
        return freed;
    }
}
#endif
