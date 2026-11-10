#if TASK14_DISPOSE_IDENTIFICATION || REFCOUNTED_FATAL_IDENTITY
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Environment = System.Environment;
using Knight.Config;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Residents;
#if REFCOUNTED_FATAL_IDENTITY
using Knight.Enemies.Ranged;
#endif

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

#if REFCOUNTED_FATAL_IDENTITY
    internal readonly record struct ResidentAssignmentOperation(long Operation, string Slot, string Path);
    private static long _assignmentOperations, _assignmentRecords, _assignmentFaults, _assignmentTruncated;
    private static long _assignmentNullFields, _assignmentSnapshots;
    internal static bool ResidentAssignmentTraceActive { get; private set; }
    internal static void BeginResidentAssignmentTrace()
    {
        _assignmentOperations = _assignmentRecords = _assignmentFaults = _assignmentTruncated = 0;
        _assignmentNullFields = _assignmentSnapshots = 0;
        ResidentAssignmentTraceActive = true;
        try
        {
            DeclareMainManagedThread();
            Console.Error.WriteLine("RESIDENT_ASSIGNMENT_TRACE_BEGIN subtypes=all before_instantiate_getstate=0 after_rejection_getstate=scrap_only added_loads=0 added_instantiates=0 managed_setter_only=true resource_fix=false");
            Console.Error.Flush();
        }
        catch { _assignmentFaults++; }
    }
    internal static ResidentAssignmentOperation BeginResidentAssignmentOperation(string slot, string path)
    {
        if (!ResidentAssignmentTraceActive) return default;
        long index = ++_assignmentOperations;
        if (index > 512) { _assignmentTruncated++; return default; }
        var operation = new ResidentAssignmentOperation(index, slot, path);
        try
        {
            AssertMainThread();
            Console.Error.WriteLine("RESIDENT_ASSIGNMENT_ENTER " + System.Text.Json.JsonSerializer.Serialize(operation));
            Console.Error.Flush();
        }
        catch { _assignmentFaults++; }
        return operation;
    }
    internal static bool ObserveResidentAssignment(ResidentAssignmentOperation operation, ResidentSceneBinding binding,
        ResidentSubtypeProfile field, bool evaluated, bool? gateMissing, bool captureAfterSource = true)
    {
        if (operation.Operation <= 0) return false;
        try
        {
            AssertMainThread();
            _assignmentRecords++;
            if (evaluated && gateMissing == true && captureAfterSource) _assignmentNullFields++;
            bool present = !ReferenceEquals(binding, null);
            var setter = present ? binding.ProfileAssignments :
                new ResidentSceneBinding.ProfileAssignmentState(0, 0, 0, false, "", 0, 0, 0);
            _assignmentFaults += setter.Faults;
            string fieldState = !evaluated ? "not_evaluated" : ReferenceEquals(field, null) ? "null" : "managed_object";
            Console.Error.WriteLine("RESIDENT_ASSIGNMENT_ROW " + System.Text.Json.JsonSerializer.Serialize(new
            {
                operation.Operation, operation.Slot, operation.Path, BindingPresent = present,
                CaptureAfterSource = captureAfterSource, Setter = setter,
                Evaluated = evaluated, GateMissing = gateMissing, FieldState = fieldState,
                FieldManagedType = fieldState == "managed_object" ? field.GetType().FullName : "",
                FieldManagedHash = fieldState == "managed_object" ? RuntimeHelpers.GetHashCode(field) : 0,
                FieldManagedSerial = fieldState == "managed_object" ? field.Task14ManagedSerial : 0
            }));
            Console.Error.Flush();
            return true;
        }
        catch (Exception error)
        {
            _assignmentFaults++;
            try { Console.Error.WriteLine($"RESIDENT_ASSIGNMENT_FAULT operation={operation.Operation} type={error.GetType().Name} completeness=unknown"); }
            catch { /* Preserve the original admission result and exception. */ }
            return false;
        }
    }
    internal static ResidentProfileSource ObserveResidentRejectionSource(ResidentAssignmentOperation operation,
        PackedScene scene, bool rowWritten, bool? originalMissing)
    {
        if (!ResidentAssignmentTraceActive || operation.Operation <= 0 || !rowWritten || originalMissing != true) return default;
        try
        {
            AssertMainThread();
            var snapshot = ReadResidentProfileSource(scene, operation.Slot, operation.Path, operation.Operation,
                "resident-profile-after-rejection");
            Console.Error.WriteLine("RESIDENT_ASSIGNMENT_REJECTION_SOURCE " + System.Text.Json.JsonSerializer.Serialize(new
            {
                operation.Operation, operation.Slot, operation.Path, Stage = "after_original_profile_null_gate",
                NotAtConversion = true, SecondBridge = true, Snapshot = snapshot
            }));
            Console.Error.Flush();
            _assignmentSnapshots++;
            return snapshot;
        }
        catch (Exception error)
        {
            _assignmentFaults++;
            try { Console.Error.WriteLine($"RESIDENT_ASSIGNMENT_FAULT operation={operation.Operation} boundary=after_rejection type={error.GetType().Name} completeness=unknown"); }
            catch { /* Preserve the original rejection and its existing cleanup. */ }
            return new(operation.Operation, "fault", 0, "", 0, 0, "", "", false);
        }
    }
    internal static bool EndResidentAssignmentTrace()
    {
        if (!ResidentAssignmentTraceActive) return false;
        ResidentAssignmentTraceActive = false;
        try
        {
            Console.Error.WriteLine($"RESIDENT_ASSIGNMENT_TRACE_END operations={_assignmentOperations} records={_assignmentRecords} " +
                $"unpaired={Math.Max(0, _assignmentOperations - _assignmentRecords)} faults={_assignmentFaults} " +
                $"truncated={_assignmentTruncated} resource_fix=false whole_post=false after_rejections={_assignmentNullFields} after_sources={_assignmentSnapshots}");
            Console.Error.Flush();
            return _assignmentOperations > 0 && _assignmentOperations == _assignmentRecords &&
                _assignmentFaults == 0 && _assignmentTruncated == 0 && _assignmentNullFields == _assignmentSnapshots;
        }
        catch { _assignmentFaults++; return false; }
    }

    internal readonly record struct ResidentProfileSource(long Operation, string State, ulong SceneId,
        string ScenePath, ulong ProfileId, long ManagedSerial, string ManagedType, string ProfilePath,
        bool LocalToScene);
    internal readonly record struct ResidentProfilePair(string State, ulong ProfileId, long ManagedSerial,
        string IdentityRelation);
    private const int MaximumResidentProfileReads = 512;
    private static long _residentProfileReads, _residentProfilePairs, _residentProfileFaults, _residentProfileTruncated;
    internal static bool ResidentProfileTraceActive { get; private set; }
    internal static void BeginResidentProfileTrace()
    {
        _residentProfileReads = _residentProfilePairs = _residentProfileFaults = _residentProfileTruncated = 0;
        ResidentProfileTraceActive = true;
        try
        {
            DeclareMainManagedThread();
            Console.Error.WriteLine("RESIDENT_PROFILE_TRACE_BEGIN subtype=scrap-scatter added_loads=0 added_instantiates=0 added_getstate=true resource_fix=false");
            Console.Error.Flush();
        }
        catch { _residentProfileFaults++; }
    }
    internal static bool EndResidentProfileTrace()
    {
        if (!ResidentProfileTraceActive) return false;
        ResidentProfileTraceActive = false;
        try
        {
            Console.Error.WriteLine($"RESIDENT_PROFILE_TRACE_END reads={_residentProfileReads} pairs={_residentProfilePairs} " +
                $"unpaired={Math.Max(0, _residentProfileReads - _residentProfilePairs)} faults={_residentProfileFaults} " +
                $"truncated={_residentProfileTruncated} resource_fix=false whole_post=false");
            Console.Error.Flush();
            return _residentProfileFaults == 0 && _residentProfileTruncated == 0 &&
                _residentProfileReads > 0 && _residentProfileReads == _residentProfilePairs;
        }
        catch { _residentProfileFaults++; return false; }
    }
    internal static ResidentProfileSource CaptureResidentProfileSource(PackedScene scene, string slot, string source)
    {
        long operation = System.Threading.Interlocked.Increment(ref _residentProfileReads);
        ResidentProfileSource Empty(string state, ulong sceneId = 0, string path = "") =>
            new(operation, state, sceneId, path, 0, 0, "", "", false);
        try
        {
            AssertMainThread();
            if (operation > MaximumResidentProfileReads)
            {
                _residentProfileTruncated++;
                return Empty("unknown_cap");
            }
            return Emit(ReadResidentProfileSource(scene, slot, source, operation, "resident-profile-source"));
        }
        catch (Exception error)
        {
            _residentProfileFaults++;
            try { Console.Error.WriteLine($"RESIDENT_PROFILE_OBSERVER_FAULT operation={operation} boundary=source type={error.GetType().Name} completeness=unknown"); }
            catch { /* A failed sink cannot abort the original Instantiate. */ }
            return Empty("fault");
        }
        ResidentProfileSource Emit(ResidentProfileSource result)
        {
            Console.Error.WriteLine("RESIDENT_PROFILE_SOURCE " + System.Text.Json.JsonSerializer.Serialize(new
                { Slot = slot, DeclaredSource = source, Snapshot = result }));
            Console.Error.Flush();
            return result;
        }
    }
    private static ResidentProfileSource ReadResidentProfileSource(PackedScene scene, string slot, string source,
        long operation, string phase)
    {
            ResidentProfileSource Empty(string state, ulong sceneId = 0, string path = "") =>
                new(operation, state, sceneId, path, 0, 0, "", "", false);
            if (!GodotObject.IsInstanceValid(scene)) return Empty("unknown_scene");
            ulong sceneId = scene.GetInstanceId();
            string path = scene.ResourcePath ?? "";
            // These objects belong to the same loaded graph. Do not Dispose the
            // SceneState or any Resource borrowed from its returned Variant.
            SceneState state = scene.GetState();
            if (!GodotObject.IsInstanceValid(state)) return Empty("unknown_state", sceneId, path);
            if (state.GetBaseSceneState() != null) return Empty("unknown_inherited", sceneId, path);
            int count = state.GetNodeCount(), target = -1;
            if (count > 512) return Empty("unknown_node_budget", sceneId, path);
            for (int index = 0; index < count; index++)
            {
                using NodePath nodePath = state.GetNodePath(index);
                string candidate = nodePath.ToString();
                if (candidate != "ResidentBinding" && candidate != "./ResidentBinding") continue;
                if (target != -1) return Empty("unknown_duplicate_node", sceneId, path);
                target = index;
            }
            if (target == -1) return Empty("unknown_missing_node", sceneId, path);
            if (state.IsNodeInstancePlaceholder(target) || state.GetNodeInstance(target) != null)
                return Empty("unknown_nested_binding", sceneId, path);
            int properties = state.GetNodePropertyCount(target), match = -1;
            if (properties > 128) return Empty("unknown_property_budget", sceneId, path);
            for (int index = 0; index < properties; index++)
            {
                using StringName property = state.GetNodePropertyName(target, index);
                if (property.ToString() != "Profile") continue;
                if (match != -1) return Empty("unknown_duplicate_property", sceneId, path);
                match = index;
            }
            if (match == -1) return Empty("unknown_missing_property", sceneId, path);
            using Variant value = state.GetNodePropertyValue(target, match);
            if (value.VariantType == Variant.Type.Nil) return Empty("nil", sceneId, path);
            if (value.VariantType != Variant.Type.Object) return Empty("wrong_variant_type", sceneId, path);
            GodotObject observed = value.AsGodotObject();
            if (!GodotObject.IsInstanceValid(observed)) return Empty("object_wrapper_null_or_invalid", sceneId, path);
            if (observed is not ResidentSubtypeProfile profile)
                return new(operation, "object_wrong_type", sceneId, path, observed.GetInstanceId(), 0,
                    observed.GetType().Name, observed is Resource resource ? resource.ResourcePath ?? "" : "", false);
            ObserveResource(profile, sceneId, source, slot, phase);
            return new(operation, "object", sceneId, path, profile.GetInstanceId(), profile.Task14ManagedSerial,
                profile.GetType().Name, profile.ResourcePath ?? "", profile.ResourceLocalToScene);
    }
    internal static ResidentProfilePair ObserveResidentProfileInstance(ResidentProfileSource source,
        ResidentSubtypeProfile profile, bool evaluated)
    {
        if (source.State == null || source.State == "unknown_cap") return default;
        try
        {
            AssertMainThread();
            _residentProfilePairs++;
            string state = !evaluated ? "not_evaluated" : profile == null ? "null" :
                GodotObject.IsInstanceValid(profile) ? "object" : "invalid";
            ulong id = state == "object" ? profile.GetInstanceId() : 0;
            long serial = state == "object" ? profile.Task14ManagedSerial : 0;
            if (state == "object")
                ObserveResource(profile, source.SceneId, source.ScenePath, "same-instantiate", "resident-profile-instance");
            string relation = source.State != "object" || state != "object" ? "not_comparable" :
                source.ProfileId == id ? "same_native" : "different_native_not_automatically_corrupt";
            var pair = new ResidentProfilePair(state, id, serial, relation);
            Console.Error.WriteLine("RESIDENT_PROFILE_INSTANCE " + System.Text.Json.JsonSerializer.Serialize(new
                { SourceOperation = source.Operation, Source = source, Instance = pair }));
            Console.Error.Flush();
            return pair;
        }
        catch (Exception error)
        {
            _residentProfileFaults++;
            try { Console.Error.WriteLine($"RESIDENT_PROFILE_OBSERVER_FAULT operation={source.Operation} boundary=instance type={error.GetType().Name} completeness=unknown"); }
            catch { /* Preserve original gate/ConfigureActor behavior. */ }
            return new("fault", 0, 0, "not_comparable");
        }
    }
#endif

    internal static long NextSectorAllocationSerial()
    {
        long serial = NextSerial();
#if REFCOUNTED_FATAL_IDENTITY
        try
        {
            long row = Interlocked.Increment(ref _sectorAllocationRows);
            if (row > 4096)
            {
                Interlocked.Increment(ref _sectorAllocationLosses);
                if (row == 4097)
                    Console.Error.WriteLine("TASK14_SECTOR_ALLOCATION_CAP rows=4096 completeness=unknown");
                return serial;
            }
            int thread = System.Environment.CurrentManagedThreadId;
            int declaredMain = Volatile.Read(ref _mainManagedThreadId);
            bool onDeclaredMain = declaredMain != 0 && thread == declaredMain;
            bool tracing = onDeclaredMain && Volatile.Read(ref _originalProbeTrace);
            long operation = tracing ? _currentOriginalOperation : 0;
            string context = declaredMain == 0 ? "main-undeclared" : !onDeclaredMain ? "other-thread-unscoped" :
                !tracing ? "trace-not-started" : operation == 0 ? "no-active-probe" : "active-original-probe";
            string stack = System.Environment.StackTrace;
            bool truncated = stack.Length > 32768;
            if (truncated)
            {
                stack = stack[..32768];
                Interlocked.Increment(ref _sectorAllocationLosses);
            }
            Console.Error.WriteLine("TASK14_SECTOR_ALLOCATION " + System.Text.Json.JsonSerializer.Serialize(new
            {
                ManagedType = "Knight.Prologue.Seamless.PrologueSectorDefinition", ManagedSerial = serial,
                ManagedThreadId = thread, ContextKind = context, Operation = operation,
                Slot = operation == 0 ? "" : _originalSlot,
                Path = operation == 0 ? "" : _originalPath,
                ManagedStack = stack, StackTruncated = truncated
            }));
            Console.Error.Flush();
        }
        catch
        {
            Interlocked.Increment(ref _sectorAllocationLosses);
            try { Console.Error.WriteLine("TASK14_SECTOR_ALLOCATION_LOSS completeness=unknown"); }
            catch { /* Keep a unique serial even when diagnostic IO fails. */ }
        }
#endif
        return serial;
    }

#if REFCOUNTED_FATAL_IDENTITY
    private static long _sectorAllocationRows, _sectorAllocationLosses;
    internal static long SectorAllocationRows => Interlocked.Read(ref _sectorAllocationRows);
    internal static long SectorAllocationLosses => Interlocked.Read(ref _sectorAllocationLosses);
    internal static void AssertSectorAllocationComplete()
    {
        long losses = SectorAllocationLosses;
        if (losses != 0)
            throw new InvalidOperationException("Sector allocation context incomplete: lost=" + losses);
        Console.Error.WriteLine("TASK14_SECTOR_ALLOCATION_COMPLETE rows=" + SectorAllocationRows + " losses=0 native_owner_proved=false");
    }
#endif

    // A field initializer must receive its unique serial even if diagnostic IO
    // fails. This captures managed allocation context, NOT native ownership.
    internal static long NextPresentationAllocationSerial()
    {
        long serial = NextSerial();
#if REFCOUNTED_FATAL_IDENTITY
        try
        {
            long row = Interlocked.Increment(ref _presentationAllocationRows);
            if (row > MaximumPresentationAllocationRows)
            {
                Interlocked.Increment(ref _presentationAllocationLosses);
                if (row == MaximumPresentationAllocationRows + 1)
                    Console.Error.WriteLine("TASK14_PRESENTATION_ALLOCATION_CAP rows=" + MaximumPresentationAllocationRows + " completeness=unknown");
                return serial;
            }
            int thread = System.Environment.CurrentManagedThreadId;
            int declaredMain = Volatile.Read(ref _mainManagedThreadId);
            bool onDeclaredMain = declaredMain != 0 && thread == declaredMain;
            bool tracing = onDeclaredMain && Volatile.Read(ref _originalProbeTrace);
            long operation = tracing ? _currentOriginalOperation : 0;
            string context = declaredMain == 0 ? "main-undeclared" : !onDeclaredMain ? "other-thread-unscoped" :
                !tracing ? "trace-not-started" : operation == 0 ? "no-active-probe" : "active-original-probe";
            string stack = System.Environment.StackTrace;
            bool truncated = stack.Length > MaximumPresentationStackCharacters;
            if (truncated)
            {
                stack = stack[..MaximumPresentationStackCharacters];
                Interlocked.Increment(ref _presentationAllocationLosses);
            }
            Console.Error.WriteLine("TASK14_PRESENTATION_ALLOCATION " + System.Text.Json.JsonSerializer.Serialize(new
            {
                ManagedType = "PresentationProfile", ManagedSerial = serial, ManagedThreadId = thread,
                ContextKind = context, Operation = operation,
                Slot = operation == 0 ? "" : _originalSlot,
                Path = operation == 0 ? "" : _originalPath,
                ManagedStack = stack, StackTruncated = truncated
            }));
            Console.Error.Flush();
        }
        catch
        {
            Interlocked.Increment(ref _presentationAllocationLosses);
            try { Console.Error.WriteLine("TASK14_PRESENTATION_ALLOCATION_LOSS completeness=unknown"); }
            catch { /* Diagnostic sink failure must not abort native resource construction. */ }
        }
#endif
        return serial;
    }

#if REFCOUNTED_FATAL_IDENTITY
    internal const int MaximumPresentationAllocationRows = 4096;
    private const int MaximumPresentationStackCharacters = 32768;
    private static long _presentationAllocationRows, _presentationAllocationLosses;
    internal static long PresentationAllocationRows => Interlocked.Read(ref _presentationAllocationRows);
    internal static long PresentationAllocationLosses => Interlocked.Read(ref _presentationAllocationLosses);

    internal static void AssertPresentationAllocationComplete()
    {
        long losses = PresentationAllocationLosses;
        if (losses != 0)
            throw new InvalidOperationException("Presentation allocation context incomplete: lost=" + losses);
        Console.Error.WriteLine("TASK14_PRESENTATION_ALLOCATION_COMPLETE rows=" + PresentationAllocationRows + " losses=0 native_owner_proved=false");
    }

    private static bool _originalProbeTrace;
    internal static bool OriginalProbeTraceActive => _originalProbeTrace;
    private static int _sectorAssignmentSnapshots;
    // Borrowed current fields only. All retained identity state is scalar; no
    // scene dependency, array, resource or callback survives this call.
    internal static int ObserveAssignedSectorDefinitions(ulong rootId,
        SeamlessPrologueWorld world,string stage)
    {
        if (!_originalProbeTrace) return 0;
        AssertMainThread();
        if(rootId==0 || !GodotObject.IsInstanceValid(world) || string.IsNullOrWhiteSpace(stage))
            throw new InvalidOperationException("sector assignment root/world/stage invalid");
        var definitions=world.Definitions;
        int count=definitions?.Count ?? -1;
        if(count<0 || count>16) throw new InvalidOperationException("sector assignment field count outside 0..16");
        if(++_sectorAssignmentSnapshots>64) throw new InvalidOperationException("sector assignment snapshot cap64");
        int sequence=_sectorAssignmentSnapshots;
        ulong worldId=world.GetInstanceId();
        Console.Error.WriteLine("TASK14_SECTOR_ASSIGNMENT_SNAPSHOT " + System.Text.Json.JsonSerializer.Serialize(new
            {Sequence=sequence,Stage=stage,RootId=rootId,WorldId=worldId,Count=count}));
        Console.Error.Flush();
        for(int index=0;index<count;index++)
        {
            PrologueSectorDefinition definition=definitions[index];
            if(!GodotObject.IsInstanceValid(definition))
                throw new InvalidOperationException("sector assignment definition invalid at index " + index);
            long serial=definition.Task14ManagedSerial;
            if(serial<=0) throw new InvalidOperationException("sector assignment serial unavailable");
            int sector=(int)definition.Id;
            ObserveResource(definition,rootId,"WorldRoot/Sectors/Definitions/"+index,
                $"sector-assignment:{rootId}:{worldId}:{index}:{sector}",stage);
            Console.Error.WriteLine("TASK14_SECTOR_ASSIGNMENT_ROW " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Sequence=sequence,Stage=stage,RootId=rootId,WorldId=worldId,Index=index,
                Sector=sector,ManagedSerial=serial,NativeId=ObservedNativeId(serial)
            }));
            Console.Error.Flush();
        }
        return count;
    }
    private static long _originalOperation, _currentOriginalOperation;
    private static string _originalSlot = "", _originalPath = "";
    private static int _cameraFixtureBoundaries;
    private sealed record CameraSourceWitness(int Ordinal, string Sector, int ManagedHash,
        bool LoadedNull, WeakReference Source);
    private static readonly List<CameraSourceWitness> _cameraSourceWitnesses = new(3);
    private static int _cameraSourceWitnessOmitted;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static PackedScene CaptureCameraFixtureSource(PackedScene source, PrologueSectorId sector)
    {
        if (!_originalProbeTrace) return source;
        AssertMainThread();
        if (_cameraSourceWitnesses.Count == 3)
        {
            _cameraSourceWitnessOmitted++;
            return source;
        }
        // Only a short weak handle crosses the synchronous factory boundary.
        // No extra Load, native lookup, GetState, GC or source ownership change.
        _cameraSourceWitnesses.Add(new(_cameraSourceWitnesses.Count + 1, sector.ToString(),
            source is null ? 0 : RuntimeHelpers.GetHashCode(source), source is null,
            new WeakReference(source, trackResurrection: false)));
        CameraFixtureBoundary("factory-source-load-return:" + sector);
        return source;
    }

    internal static void CameraFixtureBoundary(string stage, PrologueLevelController level = null)
    {
        if (!_originalProbeTrace) return;
        AssertMainThread();
        if (++_cameraFixtureBoundaries > 48)
            throw new InvalidOperationException("camera fixture boundary trace cap");
        bool valid = GodotObject.IsInstanceValid(level);
        Console.Error.WriteLine("CAMERA_FIXTURE_LIFETIME " + System.Text.Json.JsonSerializer.Serialize(new
        {
            Sequence = _cameraFixtureBoundaries, Stage = stage,
            NativeId = valid ? level.GetInstanceId() : 0UL,
            InsideTree = valid && level.IsInsideTree(), Physics = Engine.GetPhysicsFrames(),
            SourceWitnesses = _cameraSourceWitnesses.Select(witness => new
            {
                witness.Ordinal, witness.Sector, witness.ManagedHash, witness.LoadedNull,
                WeakAlive = witness.Source.IsAlive
            }).ToArray(),
            SourceWitnessOmitted = _cameraSourceWitnessOmitted,
            SourcePath = "res://scenes/prologue/PrologueLevel.tscn",
            ExtraLoads = 0, ExtraFrames = 0, ExtraCollections = 0, SourceStrongRootAdded = false
        }));
        Console.Error.Flush();
    }

    internal static void BeginOriginalProbeTrace()
    {
        DeclareMainManagedThread();
        _originalProbeTrace = true;
        _sectorAssignmentSnapshots = 0;
        _cameraSourceWitnesses.Clear();
        _cameraSourceWitnessOmitted = 0;
        Console.Error.WriteLine("ORIGINAL_PROBE_TRACE_BEGIN no_added_loads=true no_added_instantiates=true no_added_getstate=true scalar_only=true behavior_accepted=false");
    }

    internal static long BeginOriginalProbe(string slot, string path, string acquisitionStage = "load-enter")
    {
        if (!_originalProbeTrace) return 0;
        AssertMainThread();
        if (_currentOriginalOperation != 0)
            throw new InvalidOperationException("original probe diagnostic reentry; active=" + _currentOriginalOperation);
        if (++_originalOperation > 20000) throw new InvalidOperationException("original probe trace cap");
        _currentOriginalOperation = _originalOperation; _originalSlot = slot; _originalPath = path;
        try { OriginalProbeBoundary(_originalOperation, acquisitionStage, slot, path, 0); }
        catch
        {
            _currentOriginalOperation = 0; _originalSlot = _originalPath = "";
            throw;
        }
        return _originalOperation;
    }

    internal static void OriginalProbeBoundary(long operation, string stage, string slot, string path, ulong nativeId)
    {
        if (operation == 0) return;
        AssertMainThread();
        Console.Error.WriteLine("ORIGINAL_PROBE_OPERATION " + System.Text.Json.JsonSerializer.Serialize(new
            { Operation = operation, Stage = stage, Slot = slot, Path = path, NativeId = nativeId,
                ManagedThreadId = System.Environment.CurrentManagedThreadId }));
        Console.Error.Flush();
        if (stage == "instantiate-return") _currentOriginalOperation = 0;
    }

    internal static void EndOriginalProbe(long operation)
    {
        if (operation == 0) return;
        AssertMainThread();
        if (_currentOriginalOperation == operation || _currentOriginalOperation == 0)
        { _currentOriginalOperation = 0; _originalSlot = _originalPath = ""; }
    }

    // Wraps only an already existing default construction. Returns that exact
    // object, never retains it, and cannot add a load or extend its lifetime.
    internal static T ObserveOriginalDefault<T>(T resource, string role) where T : Resource
    {
        if (_originalProbeTrace && _currentOriginalOperation != 0)
            ObserveResource(resource, 0, _originalPath,
                $"original-default:{_currentOriginalOperation}:{_originalSlot}:{role}", "original-default");
        return resource;
    }

    internal static ulong[] ObserveOriginalProbe(CharacterBody2D probe, long operation, string slot, string path)
    {
        if (operation == 0) return Array.Empty<ulong>();
        AssertMainThread();
        var ids = new List<ulong>();
        ulong owner = probe.GetInstanceId();
        bool? configurationNull = probe switch
        {
            Knight.Enemies.Laborer.BellBackLaborer actor => actor.Tuning == null,
            Knight.Enemies.Moth.GraveLampMoth actor => actor.Tuning == null,
            Knight.Enemies.Sentry.BellBoltSentry actor => actor.Settings == null,
            Knight.Enemies.Skitter.CanyonSkitter actor => actor.Settings == null,
            RangedWeaponEnemy actor => actor.Settings == null,
            _ => null
        };
        Console.Error.WriteLine("ORIGINAL_PROBE_ROOT " + System.Text.Json.JsonSerializer.Serialize(new
        {
            Operation = operation, Slot = slot, Path = path, NativeId = owner,
            RuntimeType = probe.GetType().FullName,
            IsMoth = probe is Knight.Enemies.Moth.GraveLampMoth,
            ConfigurationNull = configurationNull
        }));
        Console.Error.Flush();
        void Record(Resource resource, string role)
        {
            if (resource == null) return;
            ObserveResource(resource, owner, path, $"original-probe:{operation}:{slot}:{role}", "original-assigned");
            ids.Add(resource.GetInstanceId());
        }
        switch (probe)
        {
            case Knight.Enemies.Laborer.BellBackLaborer actor: Record(actor.Tuning, "Tuning"); break;
            case Knight.Enemies.Moth.GraveLampMoth actor: Record(actor.Tuning, "Tuning"); break;
            case Knight.Enemies.Sentry.BellBoltSentry actor: Record(actor.Settings, "Settings"); break;
            case Knight.Enemies.Skitter.CanyonSkitter actor: Record(actor.Settings, "Settings"); break;
            case RangedWeaponEnemy actor: Record(actor.Settings, "Settings"); break;
        }
        Record(probe.GetNodeOrNull<CollisionShape2D>("BodyCollider")?.Shape, "BodyCollider.Shape");
        // Do not create an additional Godot array wrapper just to observe the
        // existing child nodes; that would add a separate finalizer cohort.
        for (int index = 0; index < probe.GetChildCount(); index++)
        {
            Node child = probe.GetChild(index);
            switch (child)
            {
                case RustShellPresentationCoordinator presentation: Record(presentation.Profile, "Presentation.Profile"); break;
                case Knight.Enemies.Laborer.BellBackLaborerPresentationCoordinator presentation: Record(presentation.Profile, "Presentation.Profile"); break;
                case Knight.Enemies.Moth.GraveLampMothPresentationCoordinator presentation: Record(presentation.Profile, "Presentation.Profile"); break;
            }
        }
        return ids.Distinct().ToArray();
    }

    internal static void OriginalProbeLiveness(long operation, IEnumerable<ulong> ids)
    {
        if (operation == 0) return;
        AssertMainThread();
        foreach (ulong id in ids)
            Console.Error.WriteLine("ORIGINAL_PROBE_LIVENESS " + System.Text.Json.JsonSerializer.Serialize(new
                { Operation = operation, NativeId = id, Alive = GodotObject.IsInstanceIdValid(id) }));
        Console.Error.Flush();
    }
#endif

    internal static bool CensusScope { get; private set; }
    internal static void BeginCensus()
    {
        DeclareMainManagedThread();
        if(CensusScope)throw new InvalidOperationException("one census identity scope per process");
        CensusScope=true;
        CensusBoundary("package-build-begin");
    }

    internal static void CensusBoundary(string stage)
    {
        if(!CensusScope)return;
        Console.Error.WriteLine("CENSUS_RESOURCE_STAGE stage="+stage);
        // The registry contains scalar IDs only. Do not recreate a wrapper or
        // retain the borrowed resource to measure when its native identity dies.
        AssertMainThread();
        foreach(var row in Records.Values.Where(x=>x.ManagedType=="RangedWeaponSettings").OrderBy(x=>x.ManagedSerial))
            Console.Error.WriteLine("CENSUS_RANGED_LIVENESS "+System.Text.Json.JsonSerializer.Serialize(new
                {Stage=stage,Serial=row.ManagedSerial,row.NativeId,Alive=GodotObject.IsInstanceIdValid(row.NativeId)}));
        Console.Error.Flush();
    }

    private static long _censusLeafOperation;
    private static readonly HashSet<ulong> CarrierImportIds=new();
    internal static void CheckCensusCarrierImport(string subtype,string path,bool cacheBefore,PackedScene carrier)
    {
        if(!CensusScope)return;
        bool cacheAfter=ResourceLoader.HasCached(path);ulong id=carrier.GetInstanceId();
        Console.Error.WriteLine("CENSUS_CARRIER_IMPORT "+System.Text.Json.JsonSerializer.Serialize(new
            {Subtype=subtype,Path=path,CacheBefore=cacheBefore,CacheAfter=cacheAfter,SceneId=id}));
        Console.Error.Flush();
        if(cacheBefore!=cacheAfter||!CarrierImportIds.Add(id))
            throw new InvalidOperationException("Assertion failed: carrier authoring import must preserve the shared cache and own a distinct scene");
    }
    internal static long BeginCensusLeafOperation(string subtype,string slot,string path)
    {
        if(!CensusScope)return 0;
        long operation=Interlocked.Increment(ref _censusLeafOperation);
        if(operation>4096)throw new InvalidOperationException("census leaf operation cap");
        CensusLeafOperation(operation,"load-enter",subtype,slot,path);
        return operation;
    }

    // Scalar operation boundaries in the same stream as native errors. No Resource
    // or delegate is retained and an instantiate-exit is NOT a success receipt.
    internal static void CensusLeafOperation(long operation,string stage,string subtype,string slot,string path)
    {
        if(!CensusScope)return;
        Console.Error.WriteLine("CENSUS_LEAF_OPERATION "+System.Text.Json.JsonSerializer.Serialize(new
            {Operation=operation,Stage=stage,Subtype=subtype,Slot=slot,Path=path}));
        Console.Error.Flush();
    }

    internal static void ObserveCensusResource(Resource resource,string source,string phase)
    {
        if(CensusScope)ObserveResource(resource,0,source,phase,phase);
    }

#if REFCOUNTED_FATAL_IDENTITY
    internal static void ObserveCensusRangedSettings(Node actor,string source,string phase)
    {
        if(CensusScope&&actor is RangedWeaponEnemy ranged&&ranged.Settings!=null)
            ObserveResource(ranged.Settings,ranged.GetInstanceId(),source,phase,phase);
    }
#endif

    internal static void CensusSummary()
    {
        if(!CensusScope)return;
        int profiles=Records.Values.Count(x=>x.ManagedType==nameof(ResidentSubtypeProfile)&&x.Phase=="census-fixture-new");
        int definitions=Records.Values.Count(x=>x.ManagedType==nameof(PrologueSectorDefinition)&&x.Phase=="census-temporary-definition");
        if(profiles!=24||definitions!=3)throw new InvalidOperationException($"incomplete census identities: fixture_profiles={profiles} definitions={definitions}");
        Console.WriteLine($"CENSUS_RESOURCE_IDENTITIES_OK fixture_profiles={profiles} temporary_definitions={definitions} scalar_only=true");
    }

    internal static void DeclareMainManagedThread()
    {
        int current = System.Environment.CurrentManagedThreadId;
        int previous = Interlocked.CompareExchange(ref _mainManagedThreadId, current, 0);
        if (previous != 0 && previous != current)
            throw new InvalidOperationException("unexpected main managed-thread declaration");
        Console.Error.WriteLine($"TASK14_NATIVE_IDENTITY_MAIN_THREAD managed_thread={current}");
    }

    // Called by finalizers through Task14DisposeProbe. No native Godot call or Resource retention.
    internal static bool CanObserveOnDeclaredMainThread => Volatile.Read(ref _mainManagedThreadId) > 0 &&
        Volatile.Read(ref _mainManagedThreadId) == Environment.CurrentManagedThreadId;
    internal static ulong ObservedNativeId(long serial) => Records.TryGetValue(serial, out var record) ? record.NativeId : 0;

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
        BellBackLaborerTuning tuning => tuning.Task14ManagedSerial,
        EnemyArchetypeTuning tuning => tuning.Task14ManagedSerial,
        Knight.Enemies.Sentry.BellBoltSentrySettings settings => settings.Task14ManagedSerial,
        Knight.Enemies.Skitter.CanyonSkitterSettings settings => settings.Task14ManagedSerial,
        BossTuning boss => boss.Task14ManagedSerial,
        PresentationProfile profile => profile.Task14ManagedSerial,
        PlayerTuning tuning => tuning.Task14ManagedSerial,
#if REFCOUNTED_FATAL_IDENTITY
        ResidentSubtypeProfile profile => profile.Task14ManagedSerial,
        RangedWeaponSettings settings => settings.Task14ManagedSerial,
#endif
        _ => 0
    };

    internal static void ObserveResource(Resource resource, ulong ownerId, string ownerPath,
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
