using System;
using System.Collections.Concurrent;
using Godot;

namespace Knight.Tests;

/// <summary>Copies native errors without consuming or suppressing normal engine output.</summary>
public sealed partial class NativeErrorObserver : Logger, IDisposable
{
    private readonly ConcurrentQueue<string> _errors = new();
    private bool _disposed;

    public NativeErrorObserver() => OS.AddLogger(this);

    public override void _LogError(string function, string file, int line, string code,
        string rationale, bool editorNotify, int errorType,
        Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType != (int)ErrorType.Warning)
            _errors.Enqueue($"{file}:{line} {function}: {code} {rationale}");
    }

    public string[] Snapshot() => _errors.ToArray();

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OS.RemoveLogger(this);
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
