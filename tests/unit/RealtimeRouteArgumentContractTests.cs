using System;

namespace Knight.Tests.Unit;

/// <summary>Pure consumer contracts for the realtime harness; no route or filesystem side effects.</summary>
public static class RealtimeRouteArgumentContractTests
{
    public const int CaseCount = 28;

    public static void Run()
    {
        const string storage = "--case-storage-root=E:/OurGame/Knight/.evidence/realtime-contract/storage";
        const string main = "--route-variant=main";
        const string exploration = "--route-variant=exploration";
        const string keyboard = "--keyboard-route";

        // These cases catch the harness admitting a fallback, first-match duplicate, partial
        // route, focus/input shortcut, or relative storage before invoking the shared route.
        Accepts("main full route", [main, storage], "main");
        Accepts("exploration full route", [storage, exploration], "exploration");
        Accepts("keyboard main full route", [keyboard, main, storage], "main");
        Accepts("keyboard exploration full route", [storage, exploration, keyboard], "exploration");
        Rejects("duplicate keyboard selection", [main, storage, keyboard, keyboard]);
        Rejects("valued keyboard selection", [main, storage, "--keyboard-route=true"]);
        Rejects("keyboard fallback selection", [main, storage, "--keyboard-route=mouse"]);
        Rejects("missing variant", [storage]);
        Rejects("duplicate same variant", [main, main, storage]);
        Rejects("duplicate different variant", [main, exploration, storage]);
        Rejects("unknown variant", ["--route-variant=shortcut", storage]);
        Rejects("empty variant", ["--route-variant=", storage]);
        Rejects("missing storage", [main]);
        Rejects("empty storage", [main, "--case-storage-root="]);
        Rejects("whitespace storage", [main, "--case-storage-root=   "]);
        Rejects("relative storage", [main, "--case-storage-root=storage"]);
        Rejects("drive-relative storage", [main, "--case-storage-root=E:storage"]);
        Rejects("root-relative storage", [main, "--case-storage-root=\\storage"]);
        Rejects("duplicate storage", [main, storage, storage]);
        Rejects("gear entry shortcut", [main, storage, "--gear-entry-window-only"]);
        Rejects("telemetry shortcut", [main, storage, "--telemetry-only"]);
        Rejects("physical exit shortcut", [main, storage, "--physical-exit-only"]);
        Rejects("storage guard shortcut", [main, storage, "--storage-guard-only"]);
        Rejects("background window", [main, storage, "--background-window"]);
        Rejects("input shortcut", [main, storage, "--skip-input"]);
        Rejects("focus shortcut", [main, storage, "--fake-focus"]);
        Rejects("unknown option", [main, storage, "--unknown-option"]);
        Rejects("unknown positional argument", [main, storage, "extra"]);
    }

    private static void Accepts(string name, string[] arguments, string expectedVariant)
    {
        string actual = null;
        try { actual = RunRealtimePrologueProbe.ValidateRouteArguments(arguments); }
        catch (ArgumentException exception)
        {
            TestAssert.True(false, $"realtime argument contract must accept {name}: {exception.Message}");
        }
        TestAssert.Equal(expectedVariant, actual,
            $"realtime argument contract accepts {name} and identifies the selected route");
    }

    private static void Rejects(string name, string[] arguments)
    {
        try
        {
            RunRealtimePrologueProbe.ValidateRouteArguments(arguments);
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new InvalidOperationException($"Realtime argument contract unexpectedly accepted {name}.");
    }
}
