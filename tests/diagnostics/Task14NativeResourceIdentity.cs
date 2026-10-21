#if TASK14_DISPOSE_IDENTIFICATION || REFCOUNTED_FATAL_IDENTITY
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Knight.Config;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Diagnostics;

// Test-symbol-only, bounded identity bridge. Static state contains only immutable managed data.
internal static class Task14NativeResourceIdentity
{
    private const int MaxRecords = 2048;
    private const int MaxScenes = 32;
    private const int MaxNodesPerScene = 512;
    private const int MaxPropertiesPerNode = 128;
    private const int MaxTotalProperties = 4096;
    private static readonly ConcurrentDictionary<long, IdentityRecord> Records = new();
    private static long _nextSerial;
    private static int _mainManagedThreadId;

    internal readonly record struct SnapshotResult(int DistinctSectorDefinitions,
        int BossTuningRecords, int PresentationProfileRecords);

    private readonly record struct IdentityRecord(string ManagedType, long ManagedSerial,
        int ManagedHash, ulong NativeId, string ResourcePath, ulong OwnerId,
        string OwnerPath, string OwnerLabel, string Phase);

    internal static long NextSerial() => Interlocked.Increment(ref _nextSerial);

    internal static void DeclareMainManagedThread()
    {
        int current = System.Environment.CurrentManagedThreadId;
        int previous = Interlocked.CompareExchange(ref _mainManagedThreadId, current, 0);
        if (previous != 0 && previous != current)
            throw new InvalidOperationException("unexpected main managed-thread declaration");
        Console.Error.WriteLine($"TASK14_NATIVE_IDENTITY_MAIN_THREAD managed_thread={current}");
    }

    // Called by finalizers through Task14DisposeProbe. No native Godot call or Resource retention.
    internal static string DisposeIdentity(long managedSerial)
    {
        if (managedSerial <= 0 || !Records.TryGetValue(managedSerial, out IdentityRecord value))
            return "native_identity=unknown";
        return $"native_identity=observed native_id={value.NativeId} " +
               $"resource_path={value.ResourcePath} owner_id={value.OwnerId} " +
               $"owner_path={value.OwnerPath} owner_label={value.OwnerLabel} " +
               $"observed_phase={value.Phase}";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static SnapshotResult ObserveMountedLevel(PrologueLevelController level,
        string phase, bool includeSceneDependencies)
    {
        AssertMainThread();
        if (!GodotObject.IsInstanceValid(level) || !level.IsInsideTree())
            throw new InvalidOperationException($"{phase}: mounted level is not valid inside tree");
        ulong levelId = level.GetInstanceId();
        using NodePath levelPathValue = level.GetPath();
        string levelPath = levelPathValue.ToString();
        Console.Error.WriteLine($"TASK14_NATIVE_IDENTITY_LEVEL phase={phase} " +
                                $"native_id={levelId} path={levelPath} scene={level.SceneFilePath}");
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        if (!GodotObject.IsInstanceValid(world) || world.Definitions == null)
            throw new InvalidOperationException($"{phase}: live world definitions unavailable");
        var ids = new HashSet<PrologueSectorId>();
        var walk = new SceneWalk();
        int index = 0;
        foreach (PrologueSectorDefinition definition in world.Definitions)
        {
            if (!GodotObject.IsInstanceValid(definition))
                throw new InvalidOperationException($"{phase}: sector definition {index} invalid");
            if (!ids.Add(definition.Id))
                throw new InvalidOperationException($"{phase}: duplicate sector definition {definition.Id}");
            string ownerLabel = $"level:{levelId}/sector:{definition.Id}/definition:{index}";
            ObserveResource(definition, levelId, levelPath, ownerLabel, phase);
            if (includeSceneDependencies)
            {
                PackedScene scene = definition.Scene;
                if (!GodotObject.IsInstanceValid(scene))
                    throw new InvalidOperationException($"{phase}: sector {definition.Id} has no valid packed scene");
                walk.Visit(scene, levelId, levelPath, ownerLabel + "/scene", phase, 0);
            }
            index++;
        }
        if (ids.Count != 5)
            throw new InvalidOperationException($"{phase}: expected five authored sector definitions, found {ids.Count}");

        PlayerPresentationCoordinator presentation = level.GetNodeOrNull<PlayerPresentationCoordinator>(
            "PlayerRoot/Player/Presentation");
        if (GodotObject.IsInstanceValid(presentation) &&
            GodotObject.IsInstanceValid(presentation.Profile))
            ObserveResource(presentation.Profile, presentation.GetInstanceId(),
                "relative:PlayerRoot/Player/Presentation",
                $"level:{levelId}/live-player-presentation", phase);

        int boss = Records.Values.Count(value => value.ManagedType == nameof(BossTuning));
        int profile = Records.Values.Count(value => value.ManagedType == nameof(PresentationProfile));
        Console.Error.WriteLine($"TASK14_NATIVE_IDENTITY_LEVEL_SNAPSHOT phase={phase} " +
            $"sectors={ids.Count} boss_records={boss} profile_records={profile} " +
            $"scenes_visited={walk.SceneCount} properties_visited={walk.PropertyCount}");
        return new SnapshotResult(ids.Count, boss, profile);
    }

    private static void AssertMainThread()
    {
        int expected = Volatile.Read(ref _mainManagedThreadId);
        int current = System.Environment.CurrentManagedThreadId;
        if (expected <= 0 || current != expected)
            throw new InvalidOperationException(
                $"native identity queried off recorded main managed thread: expected={expected} current={current}");
    }

    private static long ManagedSerial(Resource resource) => resource switch
    {
        PrologueSectorDefinition sector => sector.Task14ManagedSerial,
        BossTuning boss => boss.Task14ManagedSerial,
        PresentationProfile profile => profile.Task14ManagedSerial,
        _ => 0
    };

    private static void ObserveResource(Resource resource, ulong ownerId, string ownerPath,
        string ownerLabel, string phase)
    {
        AssertMainThread();
        if (!GodotObject.IsInstanceValid(resource))
            throw new InvalidOperationException($"{phase}: observed target Resource was invalid");
        long serial = ManagedSerial(resource);
        ulong nativeId = resource.GetInstanceId();
        string path = resource.ResourcePath ?? string.Empty;
        string type = resource.GetType().Name;
        int managedHash = RuntimeHelpers.GetHashCode(resource);
        if (serial > 0)
        {
            if (!Records.ContainsKey(serial) && Records.Count >= MaxRecords)
                throw new InvalidOperationException($"identity registry exceeds {MaxRecords} immutable records");
            var record = new IdentityRecord(type, serial, managedHash, nativeId, path,
                ownerId, ownerPath, ownerLabel, phase);
            if (Records.TryGetValue(serial, out IdentityRecord previous) &&
                previous.NativeId != nativeId)
                throw new InvalidOperationException($"managed serial {serial} mapped to different native IDs");
            Records.TryAdd(serial, record);
        }
        Console.Error.WriteLine($"TASK14_NATIVE_IDENTITY_OBSERVED phase={phase} " +
            $"type={type} managed_serial={serial} managed_hash={managedHash} " +
            $"native_id={nativeId} resource_path={path} owner_id={ownerId} " +
            $"owner_path={ownerPath} owner_label={ownerLabel} " +
            $"registered={(serial > 0 ? "yes" : "unknown_serial")}");
    }

    private sealed class SceneWalk
    {
        private readonly HashSet<ulong> _visitedSceneIds = new();
        internal int SceneCount { get; private set; }
        internal int PropertyCount { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Visit(PackedScene scene, ulong ownerId, string ownerPath,
            string ownerLabel, string phase, int depth)
        {
            AssertMainThread();
            if (depth > 3) throw new InvalidOperationException("packed dependency depth exceeded 3");
            if (!GodotObject.IsInstanceValid(scene))
                throw new InvalidOperationException("packed dependency is invalid");
            ulong sceneId = scene.GetInstanceId();
            if (!_visitedSceneIds.Add(sceneId)) return;
            if (++SceneCount > MaxScenes)
                throw new InvalidOperationException($"packed dependency graph exceeds {MaxScenes} scenes");
            string scenePath = scene.ResourcePath ?? string.Empty;
            Console.Error.WriteLine($"TASK14_NATIVE_IDENTITY_SCENE phase={phase} " +
                $"scene_id={sceneId} source_path={scenePath} owner_id={ownerId} " +
                $"owner_path={ownerPath} owner_label={ownerLabel} depth={depth} " +
                "packed_reference_not_mounted_node=true");
            using SceneState state = scene.GetState() ??
                throw new InvalidOperationException($"packed scene state missing: {scenePath}");
            int nodes = state.GetNodeCount();
            if (nodes > MaxNodesPerScene)
                throw new InvalidOperationException($"packed scene {scenePath} has {nodes} nodes; cap={MaxNodesPerScene}");
            for (int node = 0; node < nodes; node++)
            {
                using NodePath nodePathValue = state.GetNodePath(node, false);
                string nodePath = nodePathValue?.ToString() ?? "<none>";
                string label = ownerLabel + "/scene:" + scenePath + "/node:" + nodePath;
                PackedScene instance = state.GetNodeInstance(node);
                if (GodotObject.IsInstanceValid(instance))
                    Visit(instance, ownerId, ownerPath, label + "/instance", phase, depth + 1);
                int properties = state.GetNodePropertyCount(node);
                if (properties > MaxPropertiesPerNode)
                    throw new InvalidOperationException($"packed node {label} has {properties} properties; cap={MaxPropertiesPerNode}");
                for (int property = 0; property < properties; property++)
                {
                    if (++PropertyCount > MaxTotalProperties)
                        throw new InvalidOperationException($"packed graph exceeds {MaxTotalProperties} properties");
                    using StringName propertyNameValue = state.GetNodePropertyName(node, property);
                    string propertyName = propertyNameValue?.ToString() ?? "<none>";
                    using Variant value = state.GetNodePropertyValue(node, property);
                    if (value.VariantType != Variant.Type.Object) continue;
                    GodotObject observed = value.AsGodotObject();
                    if (observed is PrologueSectorDefinition or BossTuning or PresentationProfile)
                        ObserveResource((Resource)observed, ownerId, ownerPath,
                            label + "/property:" + propertyName, phase);
                    if (propertyName == "BossScene" && observed is PackedScene bossScene)
                        Visit(bossScene, ownerId, ownerPath,
                            label + "/property:BossScene", phase, depth + 1);
                }
            }
        }
    }
}
#endif
