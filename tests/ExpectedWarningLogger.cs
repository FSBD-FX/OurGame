using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Knight.Tests;

/// <summary>
/// Test-only observer for deliberately emitted warnings. Registration does not consume or
/// alter Godot's normal output. The callback only copies immutable values into a concurrent
/// queue; matching and assertions remain on the test runner thread.
/// </summary>
public sealed partial class ExpectedWarningLogger : Logger, IDisposable
{
    public sealed record Warning(
        string Function,
        string File,
        int Line,
        string Code,
        string Rationale,
        int ErrorType)
    {
        public string Message => !string.IsNullOrEmpty(Rationale) ? Rationale : Code;
    }

    public sealed record Expected(string Message, string SourceFile, string Function, int Count);

    private readonly ConcurrentQueue<Warning> _warnings = new();
    private bool _disposed;

    public ExpectedWarningLogger()
    {
        OS.AddLogger(this);
    }

    public override void _LogError(string function, string file, int line, string code,
        string rationale, bool editorNotify, int errorType,
        Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType != (int)ErrorType.Warning)
            return;

        _warnings.Enqueue(new Warning(
            function ?? string.Empty,
            file ?? string.Empty,
            line,
            code ?? string.Empty,
            rationale ?? string.Empty,
            errorType));
    }

    public IReadOnlyList<Warning> Snapshot() => _warnings.ToArray();

    public static Expected[] GameFlowNegativeExpectations() =>
    [
        new Expected("Rejected prologue flow transition Title->Playing.",
            "scripts/prologue/PrologueGameFlowController.cs", "Reject", 6),
        new Expected("Rejected prologue flow transition Completed->Recovering.",
            "scripts/prologue/PrologueGameFlowController.cs", "Reject", 1)
    ];

    public static Expected[] ThrowingClawObserverExpectations() =>
    [
        new Expected("MechanicalClawController isolated a throwing ClawFrameChanged observer: " +
            "intentional Task7 observer fault",
            "scripts/abilities/MechanicalClawController.cs", "DispatchFrameChanged", 1)
    ];

    public void AssertExact(params Expected[] expected)
    {
        string failure = FindExactFailure(Snapshot(), expected);
        TestAssert.True(failure == null, failure ?? "expected warning multiset matches exactly");
    }

    public static string FindExactFailure(IReadOnlyList<Warning> actual,
        IReadOnlyList<Expected> expected)
    {
        var remaining = new List<Warning>(actual);
        foreach (Expected item in expected)
        {
            int matched = 0;
            for (int index = remaining.Count - 1; index >= 0; index--)
            {
                Warning warning = remaining[index];
                if (!string.Equals(warning.Message, item.Message, StringComparison.Ordinal) ||
                    !warning.File.Replace('\\', '/').EndsWith(item.SourceFile.Replace('\\', '/'),
                        StringComparison.OrdinalIgnoreCase) ||
                    !warning.Function.Contains(item.Function, StringComparison.Ordinal))
                    continue;

                matched++;
                remaining.RemoveAt(index);
            }

            if (matched != item.Count)
                return $"expected warning '{item.Message}' from {item.SourceFile}:{item.Function} " +
                    $"count={item.Count}, actual={matched}; observed={Describe(actual)}";
        }

        if (remaining.Count > 0)
            return $"unexpected warning(s): {Describe(remaining)}";

        return null;
    }

    public static void AssertMatcherGuards()
    {
        var warning = new Warning("Probe", "tests/RunContinuousCanyonTests.cs", 1,
            "", "TASK10_EXPECTED_WARNING_PROBE", (int)ErrorType.Warning);
        var expected = new Expected("TASK10_EXPECTED_WARNING_PROBE",
            "tests/RunContinuousCanyonTests.cs", "Probe", 1);

        TestAssert.True(FindExactFailure(Array.Empty<Warning>(), new[] { expected }) != null,
            "expected-warning guard rejects a missing warning");
        TestAssert.True(FindExactFailure(new[] { warning, warning }, new[] { expected }) != null,
            "expected-warning guard rejects an extra warning");
        TestAssert.True(FindExactFailure(new[] { warning }, new[] { expected }) == null,
            "expected-warning guard accepts the exact warning multiset");
    }

    public new void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        OS.RemoveLogger(this);
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string Describe(IEnumerable<Warning> warnings) => string.Join(" | ", warnings.Select(
        warning => $"type={warning.ErrorType} file={warning.File}:{warning.Line} " +
            $"function={warning.Function} code='{warning.Code}' rationale='{warning.Rationale}'"));
}
