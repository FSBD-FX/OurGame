using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Tools;

namespace Knight.Tests.Integration;

// Tests own external staging. Production never loads this evidence directory.
public static class ResidentDevelopmentPackageTests
{
    private const string AuthoringInput = "G:/knight/验证证据/october13-stable-claw/resident-map-census-20261022-120049-668-9a2387f46483492a8551808d46b8268e/storage/leaf-source-profiles";
    private const string ParentPath = "res://scenes/prologue/PrologueLevel.tscn";

    public static void RunStage(string storage)
    {
        CheckRegistry();
        string profiles = Path.Combine(storage, "development-profile-input");
        string leaves = Path.Combine(storage, "development-package-v001");
        TestAssert.True(!Directory.Exists(profiles) && !Directory.Exists(leaves), "fresh explicit authoring staging");
        Directory.CreateDirectory(profiles); Directory.CreateDirectory(leaves);
        foreach (var type in CanyonResidentCatalog.Subtypes)
            File.Copy(Path.Combine(AuthoringInput, type.Id + ".tres"), Path.Combine(profiles, type.Id + ".tres"), false);
        BuildResidentSubtypeScenes.BuildInto(leaves, profiles);
        TestAssert.Equal(48, Directory.GetFiles(leaves).Length, "24 distinct leaves plus 24 editable authoring profiles");
        var slots = ResidentAuthoredLayout.Load();
        foreach (var type in ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001))
        {
            string path = Path.Combine(leaves, type.Id + ".tscn");
            TestAssert.True(File.ReadAllBytes(Path.Combine(profiles, type.Id + ".tres")).SequenceEqual(
                File.ReadAllBytes(Path.Combine(leaves, type.Id + ".tres"))), "authoring bytes preserved " + type.Id);
            TestAssert.True(!File.ReadAllText(path).Contains(storage, StringComparison.OrdinalIgnoreCase), "portable embedded profile " + type.Id);
            var staged = type with { ScenePath = path };
            using var scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            var slot = slots.First(x => x.Definition.SubtypeId == type.Id).Definition;
            if (type.Id == "rock-wanderer")
                Reject(() => ResidentSceneBinding.Instantiate(scene, staged, slot), "default factory rejects actual placeholder leaf");
            CharacterBody2D actor = ResidentSceneBinding.Instantiate(scene, staged, slot, true);
            try
            {
                var binding = actor.GetNode<ResidentSceneBinding>("ResidentBinding");
                TestAssert.True(!actor.IsInsideTree() && binding.PresentationState == ResidentSceneState.ExplicitPlaceholder &&
                    binding.Profile.SubtypeId == type.Id && binding.Profile.Family == type.Family &&
                    binding.Profile.Archetype == type.Archetype && binding.Profile.Carrier == type.Carrier,
                    "actual persisted profile and pre-Ready consumer " + type.Id);
            }
            finally { actor.Free(); }
        }
        StageParent(storage);
        GD.Print("RESIDENT_DEVELOPMENT_STAGE_OK leaves=24 profiles=24 authoring_bytes_equal=true actual_factory=true default_placeholder_rejected=true canonical_written=false auto_spawned=0 final_art=false");
    }

    private static void CheckRegistry()
    {
        var development = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001);
        TestAssert.Equal(24, development.Count, "every development subtype is selected");
        foreach (var baseline in CanyonResidentCatalog.Subtypes)
        {
            var actual = development.Single(x => x.Id == baseline.Id);
            TestAssert.Equal(ResidentSceneState.ExplicitPlaceholder, actual.SceneState, "development selection is explicit, not unfinished or final " + actual.Id);
            TestAssert.True(actual == baseline with { SceneState = ResidentSceneState.ExplicitPlaceholder,
                ScenePath = ResidentSceneRegistry.DevelopmentRoot + baseline.Id + ".tscn" }, "fixed res path and all identity fields preserved " + baseline.Id);
        }
        TestAssert.True(ResidentSceneRegistry.Select(ResidentContentMode.FinalOnly).SequenceEqual(CanyonResidentCatalog.Subtypes) &&
            CanyonResidentCatalog.Subtypes.All(x => x.SceneState == ResidentSceneState.Unimplemented && x.ScenePath == null),
            "default final catalog unchanged; development never upgrades final readiness");
        Reject(() => ResidentSceneRegistry.Select((ResidentContentMode)999), "unknown content mode rejected");
        GD.Print("RESIDENT_DEVELOPMENT_REGISTRY_OK subtypes=24 fixed_paths=true final_catalog_unchanged=true invalid_mode_rejected=true");
    }

    private static void StageParent(string storage)
    {
        var saved = GD.Load<PackedScene>(ParentPath);
        PrologueLevelController root = saved.Instantiate<PrologueLevelController>();
        try
        {
            root.SceneFilePath = "";
            var before = SteamMetadataSynchronizationTests.Snapshot(root);
            root.ResidentContent = ResidentContentMode.DevelopmentV001;
            string path = Path.Combine(storage, "Development-PrologueLevel.tscn");
            TestAssert.Equal(Error.Ok, SceneBuildUtil.PackAndSave(root, path), "single exported content selection saves");
            using var staged = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            PrologueLevelController copy = staged.Instantiate<PrologueLevelController>();
            try
            {
                copy.SceneFilePath = "";
                TestAssert.Equal(ResidentContentMode.DevelopmentV001, copy.ResidentContent, "saved parent selects explicit development content");
                var after = SteamMetadataSynchronizationTests.Snapshot(copy);
                before.Remove(".|ResidentContent"); after.Remove(".|ResidentContent");
                TestAssert.True(before.Count == after.Count && before.All(x => after.TryGetValue(x.Key, out string value) && value == x.Value),
                    "all parent nonselection semantics preserved including terrain catalog and canonical refs");
                TestAssert.True(SteamMetadataSynchronizationTests.PersistentLinks(saved).SequenceEqual(
                    SteamMetadataSynchronizationTests.PersistentLinks(staged)), "parent persistent links unchanged");
            }
            finally { copy.Free(); }
        }
        finally { root.Free(); }
    }

    public static async Task RunPublished(SceneTree tree)
    {
        CheckRegistry();
        foreach (var type in ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001))
            TestAssert.True(ResourceLoader.Exists(type.ScenePath) && Godot.FileAccess.FileExists(
                "res://assets/data/residents/profiles/v001/" + type.Id + ".tres"), "actual production content exists " + type.Id);
        await Consumer(true);
        await Consumer(false);
        await Consumer(false); // A fresh real Level/manager must acquire independently after retirement.
        GD.Print("RESIDENT_DEVELOPMENT_PUBLISHED_OK leaves=24 profiles=24 actual_level=true actual_manager_birth=6 source_custody_births=4 manager_replacement=1 final_only_rejected=true default_placeholder_rejected=true automatic_births=0 final_art=false normal_route=false");

        async Task Consumer(bool finalControl)
        {
            string name = finalControl ? "resident-final-only-control" : "resident-development-consumer";
            var paths = IsolatedPrologueFixture.CreatePaths(name);
            PrologueLevelController root = PrologueGameFlowTests.Instantiate(name, paths.SavePath);
            try
            {
                TestAssert.Equal(ResidentContentMode.DevelopmentV001, root.ResidentContent, "actual saved normal parent explicitly selects development version");
                if (finalControl) root.ResidentContent = ResidentContentMode.FinalOnly;
                root.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = false;
                tree.Root.AddChild(root);
                for (int i = 0; i < 4; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                var slot = root.ResidentLayout.First().Definition;
                TestAssert.True(root.Residents != null && root.ResidentLayout.Count == 50 && root.Residents.TrackedActorCount == 0,
                    "real title initializes exact fifty identities without fake automatic births");
                TestAssert.True(!root.Residents.TrySpawn(slot.StableId, out _, out _, out var defaultIssues) &&
                    defaultIssues.Contains("scene_not_ready") && root.Residents.TrackedActorCount == 0,
                    "manager default never silently accepts development placeholders");
                if (finalControl)
                {
                    TestAssert.True(!root.Residents.TrySpawn(slot.StableId, out _, out _, out var finalIssues, true) &&
                        finalIssues.Contains("scene_not_ready"), "final-only Level keeps canonical unfinished-scene refusal");
                    return;
                }
                ManagerSourceSeed custody = CheckLiveManagerSourceCustody(root.Residents, slot);
                TestAssert.True(root.Residents.TrySpawn(slot.StableId, out var actor, out var lease, out var issues, true),
                    "actual normal Level manager must consume selected saved development scene: " + string.Join("|", issues));
                TestAssert.True(actor.IsInsideTree() && actor.IsNodeReady() && actor.GetParent().Name == "Actors" &&
                    actor.GlobalPosition.DistanceTo(slot.Placement.WorldSpawn) < .001f &&
                    actor.GetNode<HealthComponent>("Health").CurrentHealth == 3 &&
                    lease.ActorInstance == actor.GetInstanceId() && root.Residents.TrackedActorCount == 1,
                    "native Ready, actual authored pose, direct actor parent and HP committed together");
                TestAssert.True(!root.Residents.TrySpawn(slot.StableId, out _, out _, out _, true) &&
                    root.Residents.TrackedActorCount == 1, "production manager cannot duplicate the real native identity");
                actor.Free();
                root.Residents.Dispose();
                CheckRetiredManagerSource(root.Residents, slot, custody);
            }
            finally
            {
                tree.Paused = false;
                if (GodotObject.IsInstanceValid(root))
                {
                    if (root.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
                    else root.Free();
                }
            }
        }
    }

    private sealed class ManagerSourceSeed
    {
        internal WeakReference Scene, Profile;
        internal ResidentSubtypeProfile Strong;
        internal ulong ProfileId;
    }
    private sealed class ManagerFinalizerSignals
    {
        internal readonly ManualResetEventSlim Entered = new(false), Release = new(false), Returned = new(false);
        internal int TimedOut;
    }
    private sealed class ManagerFinalizerGate(ManagerFinalizerSignals signals)
    {
        ~ManagerFinalizerGate()
        {
            signals.Entered.Set();
            if (!signals.Release.Wait(5000)) Interlocked.Exchange(ref signals.TimedOut, 1);
            signals.Returned.Set();
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ArmManagerFinalizerGate(ManagerFinalizerSignals signals) => _ = new ManagerFinalizerGate(signals);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropManagerSourcePin(ManagerSourceSeed seed) => seed.Strong = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ManagerSourceSeed FirstManagerSource(CanyonResidentManager manager, ResidentPopulationDefinition slot)
    {
        var type = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001).Single(x => x.Id == slot.SubtypeId);
        PackedScene saved = GD.Load<PackedScene>(type.ScenePath);
        CharacterBody2D actor = null;
        try
        {
            TestAssert.True(manager.TrySpawn(slot.StableId, out actor, out _, out var issues, true),
                "actual source-custody first manager birth: " + string.Join("|", issues));
            var profile = actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile;
            TestAssert.True(actor.IsNodeReady() && GodotObject.IsInstanceValid(profile),
                "source-custody first actual manager actor reaches Ready with exported Profile");
            return new ManagerSourceSeed { Scene = new WeakReference(saved), Profile = new WeakReference(profile),
                Strong = profile, ProfileId = profile.GetInstanceId() };
        }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ManagerSourceSeed CheckLiveManagerSourceCustody(CanyonResidentManager manager, ResidentPopulationDefinition slot)
    {
        ManagerSourceSeed seed = FirstManagerSource(manager, slot);
        TestAssert.True(manager.TrackedActorCount == 0 && manager.Snapshot(slot.StableId) is { Alive: true, Attached: false },
            "first source-custody actor exits but its actual manager and live scalar slot remain");
        GC.Collect(); GC.WaitForPendingFinalizers();
        bool sceneAlive = seed.Scene.IsAlive;
        var signals = new ManagerFinalizerSignals();
        bool entered = false, returned = false, profileAlive = false, committed = false;
        ulong reloadedId = 0; string reasons = "";
        CharacterBody2D second = null;
        try
        {
            ArmManagerFinalizerGate(signals); GC.Collect();
            entered = signals.Entered.Wait(1000);
            TestAssert.True(entered && Volatile.Read(ref signals.TimedOut) == 0,
                "MANAGER_SOURCE_NOT_EXERCISED: bounded CLR finalizer gate must enter");
            DropManagerSourcePin(seed); GC.Collect(); // No wait while the bounded gate owns the finalizer thread.
            profileAlive = seed.Profile.IsAlive;
            committed = manager.TrySpawn(slot.StableId, out second, out _, out var issues, true);
            reasons = string.Join("|", issues);
            if (committed) reloadedId = second.GetNode<ResidentSceneBinding>("ResidentBinding").Profile.GetInstanceId();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(second)) second.Free();
            signals.Release.Set(); returned = signals.Returned.Wait(1000);
        }
        GD.Print("RESIDENT_MANAGER_SOURCE_WINDOW " + System.Text.Json.JsonSerializer.Serialize(new
        {
            Slot = slot.StableId, SceneAlive = sceneAlive, ProfileAlive = profileAlive,
            ProfileId = seed.ProfileId, ReloadProfileId = reloadedId, Committed = committed,
            GateEntered = entered, GateReturned = returned, GateTimedOut = Volatile.Read(ref signals.TimedOut) != 0,
            Issues = reasons
        }));
        TestAssert.True(returned && Volatile.Read(ref signals.TimedOut) == 0,
            "MANAGER_SOURCE_NOT_EXERCISED: bounded CLR finalizer gate must return");
        TestAssert.True(profileAlive && committed && reloadedId == seed.ProfileId &&
            manager.TrackedActorCount == 0 && manager.Snapshot(slot.StableId) is { Alive: true, Attached: false },
            "RESIDENT_MANAGER_SOURCE_RED: live manager must preserve its saved Profile across owned actor unload and real rebirth");
        GC.KeepAlive(seed.Strong); GC.KeepAlive(manager);
        return seed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckRetiredManagerSource(CanyonResidentManager manager, ResidentPopulationDefinition slot,
        ManagerSourceSeed seed)
    {
        // Keep the retired manager object itself alive: collection must come
        // from its explicit reference release, not from losing the manager.
        GC.Collect(); GC.WaitForPendingFinalizers();
        GC.Collect(); GC.WaitForPendingFinalizers();
        bool rejected = !manager.TrySpawn(slot.StableId, out _, out _, out _, true);
        bool sceneAlive = seed.Scene.IsAlive;
        GD.Print("RESIDENT_MANAGER_SOURCE_RELEASED " + System.Text.Json.JsonSerializer.Serialize(new
        { Slot = slot.StableId, ProfileId = seed.ProfileId, SceneAlive = sceneAlive,
            ProfileAlive = seed.Profile.IsAlive, RetiredRejected = rejected, ManagerRetained = true }));
        TestAssert.True(!sceneAlive && rejected && manager.TrackedActorCount == 0,
            "retired live manager drops its saved scene references and cannot reacquire a resident");
        GC.KeepAlive(manager);
    }

    private static void Reject(Action operation, string message)
    {
        bool rejected = false;
        try { operation(); } catch (ArgumentException) { rejected = true; }
        TestAssert.True(rejected, message);
    }
}
