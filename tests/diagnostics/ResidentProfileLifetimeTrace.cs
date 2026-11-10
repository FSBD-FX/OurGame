#if REFCOUNTED_FATAL_IDENTITY
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Environment = System.Environment;
using Knight.Prologue.Residents;

namespace Knight.Tests.Diagnostics;

// Symbol-only scalar journal. No row/frame/sink closure retains a native wrapper.
internal static class ResidentProfileLifetimeTrace
{
    internal readonly record struct Context(long Operation, string Owner, long Epoch, long Reset,
        string Slot, string Path);
    internal sealed record Row(string Kind)
    {
        public long Sequence { get; init; }
        public int Thread { get; init; }
        public Context Context { get; init; }
        public long Serial { get; init; }
        public int Hash { get; init; }
        public string Type { get; init; } = "";
        public string Stack { get; init; } = "";
        public ulong NativeId { get; init; }
        public ulong ActorId { get; init; }
        public ulong BindingId { get; init; }
        public ulong SceneId { get; init; }
        public long CacheId { get; init; }
        public long Assignment { get; init; }
        public long Generation { get; init; }
        public ulong SectorId { get; init; }
        public string Detail { get; init; } = "";
        public string SubjectOwner { get; init; } = "";
    }
    internal sealed class Journal
    {
        private sealed record Frame(Context Context, Frame Parent, int Depth);
        private readonly object _gate = new();
        private readonly Action<string> _sink;
        private readonly int _eventCap, _linkCap, _depthCap;
        private readonly List<Row> _rows = new();
        private readonly Dictionary<int, Frame> _frames = new();
        private long _operations, _faults;
        private int _links;
        internal Journal(Action<string> sink, int events = 8192, int links = 2048, int depth = 32)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            if (events < 1 || links < 1 || depth < 1) throw new ArgumentOutOfRangeException(nameof(events));
            _eventCap = events; _linkCap = links; _depthCap = depth;
        }
        internal Context Current
        {
            get { lock (_gate) return ContextFor(Environment.CurrentManagedThreadId); }
        }
        internal Row[] Rows { get { lock (_gate) return _rows.ToArray(); } }
        private Context ContextFor(int thread) => _frames.TryGetValue(thread, out var frame)
            ? frame.Context : new(0, "", 0, 0, "", "");
        internal IDisposable Begin(string owner, long epoch, long reset, string slot, string path)
        {
            lock (_gate)
            {
                int thread = Environment.CurrentManagedThreadId;
                _frames.TryGetValue(thread, out var parent);
                if ((parent?.Depth ?? 0) >= _depthCap || _rows.Count >= _eventCap)
                { Fault(thread); return new Empty(); }
                var frame = new Frame(new(++_operations, owner, epoch, reset, slot, path), parent, (parent?.Depth ?? 0) + 1);
                _frames[thread] = frame;
                Record(new("preparation-enter"));
                return new Scope(this, thread, frame);
            }
        }
        internal void Record(Row row)
        {
            lock (_gate)
            {
                int thread = Environment.CurrentManagedThreadId;
                if (_rows.Count >= _eventCap || row.Kind == "nonnull-gate" && _links >= _linkCap)
                { Fault(thread); return; }
                if (row.Kind == "nonnull-gate") _links++;
                row = row with { Sequence = _rows.Count + 1, Thread = thread,
                    Context = row.Context.Owner == null ? ContextFor(thread) : row.Context };
                if (row.Stack.Length > 4096) row = row with { Stack = row.Stack[..4096] };
                _rows.Add(row);
                try { _sink("PROFILE_LIFETIME_ROW " + System.Text.Json.JsonSerializer.Serialize(row)); }
                catch { Fault(thread); }
            }
        }
        private void Fault(int thread) { _faults++; _frames.Remove(thread); }
        internal void MarkIncomplete() { lock (_gate) Fault(Environment.CurrentManagedThreadId); }
        internal long SceneCacheId(ulong scene)
        {
            lock (_gate)
            {
                long operation = ContextFor(Environment.CurrentManagedThreadId).Operation;
                for (int i = _rows.Count - 1; i >= 0; i--)
                    if (_rows[i].SceneId == scene && _rows[i].Context.Operation == operation && _rows[i].CacheId != 0)
                        return _rows[i].CacheId;
                return 0;
            }
        }
        private void End(int thread, Frame frame)
        {
            lock (_gate)
            {
                if (thread != Environment.CurrentManagedThreadId || !_frames.TryGetValue(thread, out var current) ||
                    !ReferenceEquals(current, frame))
                { Fault(thread); Fault(Environment.CurrentManagedThreadId); return; }
                Record(new("preparation-exit"));
                // A sink/cap failure invalidates the whole stack; never restore it.
                if (!_frames.TryGetValue(thread, out current) || !ReferenceEquals(current, frame)) return;
                if (frame.Parent == null) _frames.Remove(thread); else _frames[thread] = frame.Parent;
            }
        }
        private sealed class Scope(Journal journal, int thread, Frame frame) : IDisposable
        {
            private int _ended;
            public void Dispose() { if (Interlocked.Exchange(ref _ended, 1) == 0) journal.End(thread, frame); }
        }
        internal void AssertComplete()
        {
            lock (_gate)
                if (_faults != 0 || _frames.Count != 0)
                    throw new InvalidOperationException($"PROFILE_LIFETIME_INCOMPLETE faults={_faults} active_threads={_frames.Count}");
        }
    }
    private sealed class Empty : IDisposable { public void Dispose() { } }
    private static Journal _journal = new(line => { Console.Error.WriteLine(line); Console.Error.Flush(); });
    internal static Journal Active => Volatile.Read(ref _journal);
    internal static IDisposable BeginPreparation(string owner, long epoch, long reset, string slot, string path)
    {
        try { return Active.Begin(owner, epoch, reset, slot, path); }
        catch { Active.MarkIncomplete(); return new Empty(); }
    }
    internal static void Stage(string kind, ulong actorId = 0, string detail = "")
    {
        try { Active.Record(new(kind) { ActorId = actorId, Detail = detail }); }
        catch { Active.MarkIncomplete(); }
    }
    internal static ulong PreparedActorId()
    {
        try
        {
            long operation = Active.Current.Operation;
            var rows = Active.Rows;
            for (int i = rows.Length - 1; i >= 0; i--)
                if (rows[i].Kind == "nonnull-gate" && rows[i].Context.Operation == operation) return rows[i].ActorId;
        }
        catch { Active.MarkIncomplete(); }
        return 0;
    }
    internal static void Lease(string kind, string owner, ResidentLease lease)
    {
        try { Active.Record(new(kind) { Context = new(0, owner, lease.Epoch, 0, lease.StableId, ""),
            ActorId = lease.ActorInstance, SectorId = lease.SectorInstance, Generation = lease.Generation,
            Detail = "custody-only_not-native-death" }); }
        catch { Active.MarkIncomplete(); }
    }
    internal static ulong SourceValid(long cache, string kind, PackedScene scene, string path, string owner, string role)
    {
        if (!Task14NativeResourceIdentity.CanObserveOnDeclaredMainThread) { Active.MarkIncomplete(); return 0; }
        try
        {
            // Caller is already inside its original valid branch, after its disposed-loader guard.
            ulong id = scene.GetInstanceId();
            Source(cache, kind, id, path, owner, role);
            return id;
        }
        catch { Active.MarkIncomplete(); return 0; }
    }
    internal static void Source(long cache, string kind, ulong scene, string path, string owner, string role)
    {
        try
        {
            var context = Active.Current;
            if (context.Operation == 0) context = new(0, owner ?? "", 0, 0, "", path);
            Active.Record(new(kind) { Context = context, CacheId = cache, SceneId = scene, Detail = path,
                Type = role, SubjectOwner = owner ?? "" });
        }
        catch { Active.MarkIncomplete(); }
    }
    internal static IDisposable UseJournalForContract(Journal journal)
    {
        var previous = Active;
        previous.AssertComplete(); journal.AssertComplete();
        Volatile.Write(ref _journal, journal);
        return new Restore(previous);
    }
    private sealed class Restore(Journal previous) : IDisposable
    {
        private int _restored;
        public void Dispose() { if (Interlocked.Exchange(ref _restored, 1) == 0) Volatile.Write(ref _journal, previous); }
    }
    // Called from the actual managed ctor, including before main-thread declaration.
    internal static void Born(ResidentSubtypeProfile profile, long serial)
    {
        try { Active.Record(new("born") { Serial = serial, Hash = RuntimeHelpers.GetHashCode(profile),
            Type = profile.GetType().FullName, Stack = Environment.StackTrace }); }
        catch { Active.MarkIncomplete(); }
    }
    internal static void Born(Knight.Config.PlayerTuning tuning, long serial)
    {
        try { Active.Record(new("born") { Serial = serial, Hash = RuntimeHelpers.GetHashCode(tuning),
            Type = tuning.GetType().FullName, Stack = Environment.StackTrace }); }
        catch { Active.MarkIncomplete(); }
    }
    internal static Knight.Config.PlayerTuning PlayerTuningCreated(Knight.Config.PlayerTuning tuning, string role)
    {
        PlayerTuningSnapshot(tuning, null, "player-tuning-created", role);
        return tuning;
    }
    internal static void PlayerTuningAssigned(Knight.Config.PlayerTuning tuning, Node owner, string role) =>
        PlayerTuningSnapshot(tuning, owner, "player-tuning-assigned", role);

    private static void PlayerTuningSnapshot(Knight.Config.PlayerTuning tuning, Node owner, string kind, string role)
    {
        ulong native = 0, ownerId = 0;
        try
        {
            if (!Task14NativeResourceIdentity.CanObserveOnDeclaredMainThread) Active.MarkIncomplete();
            else
            {
                ownerId = owner == null ? 0 : owner.GetInstanceId();
                Task14NativeResourceIdentity.ObserveResource(tuning, ownerId, "", role, kind);
                native = Task14NativeResourceIdentity.ObservedNativeId(tuning.Task14ManagedSerial);
            }
        }
        catch { Active.MarkIncomplete(); }
        try
        {
            Active.Record(new(kind) { Serial = tuning.Task14ManagedSerial, Hash = RuntimeHelpers.GetHashCode(tuning),
                Type = tuning.GetType().FullName, NativeId = native, ActorId = ownerId, Detail = role });
        }
        catch { Active.MarkIncomplete(); }
    }
    internal static void GateSnapshot(ResidentSubtypeProfile profile, CharacterBody2D actor,
        ResidentSceneBinding binding, PackedScene scene, long assignment)
    {
        if (!Task14NativeResourceIdentity.CanObserveOnDeclaredMainThread) { Active.MarkIncomplete(); return; }
        try
        {
            ulong actorId = actor.GetInstanceId();
            Task14NativeResourceIdentity.ObserveResource(profile, actorId, Active.Current.Path ?? "",
                "resident-nonnull-gate", "resident-nonnull-gate");
            ulong sceneId = scene.GetInstanceId();
            Active.Record(new("nonnull-gate") { Serial = profile.Task14ManagedSerial,
                Hash = RuntimeHelpers.GetHashCode(profile), Type = profile.GetType().FullName,
                NativeId = Task14NativeResourceIdentity.ObservedNativeId(profile.Task14ManagedSerial),
                ActorId = actorId, BindingId = binding.GetInstanceId(), SceneId = sceneId,
                CacheId = Active.SceneCacheId(sceneId), Assignment = assignment });
        }
        catch { Active.MarkIncomplete(); }
    }
}
#endif
