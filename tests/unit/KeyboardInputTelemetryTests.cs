using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class KeyboardInputTelemetryTests
{
    public static void Run()
    {
        var prior = default(PrologueInputEdgeState);
        var frame = new PlayerInputFrame(0, Vector2.Zero, false, false, false, true, false,
            ShieldHeld: true, ClawPressed: true, InteractPressed: true, KeyboardActions: true,
            KeyboardShieldHeld: true);
        var edges = ProloguePlaytestRecorder.SampleInputEdges(frame, ref prior);
        Same(edges, "J", "K", "Q_INTERACT", "L_DOWN");
        var held = frame with { AttackPressed = false, ClawPressed = false, InteractPressed = false };
        Same(ProloguePlaytestRecorder.SampleInputEdges(held, ref prior));
        // Both raw guard sources can coexist. Logical aiming source is not evidence
        // that the L key was held: a fresh J alongside an old RMB must remain RMB.
        var mixed = held with { KeyboardShieldHeld = false, LegacyShieldHeld = true };
        Same(ProloguePlaytestRecorder.SampleInputEdges(mixed, ref prior), "L_UP", "RMB_DOWN");
        Same(ProloguePlaytestRecorder.SampleInputEdges(default, ref prior), "RMB_UP");
        var legacy = new PlayerInputFrame(0, Vector2.Zero, false, false, false, false, false,
            AttackModePressed: true, GrappleModePressed: true, PrimaryPressed: true, ShieldHeld: true);
        Same(ProloguePlaytestRecorder.SampleInputEdges(legacy, ref prior), "Q", "E", "LMB_DOWN", "RMB_DOWN");

        var session = new ProloguePlaytestSession("keyboard-telemetry", "2026-10-10T00:00:00Z", "AutomatedFixture");
        foreach (string edge in edges) session.RecordInputEdge(edge);
        Same(session.RecentInputEdges.Select(sample => sample.Action), "J", "K", "Q_INTERACT", "L_DOWN");
        var report = session.BuildReport(PrologueSectorId.CanyonEntrance, PrologueObjective.LearnAttackMode, "now");
        string json = ProloguePlaytestReportFormatter.ToJson(report);
        using (var document = JsonDocument.Parse(json))
            TestAssert.True(document.RootElement.TryGetProperty("inputContractVersion", out var version) &&
                version.GetInt32() == 1, "new telemetry identifies independent keyboard semantics version 1");
        TestAssert.True(ProloguePlaytestReportFormatter.ToMarkdown(report).Contains("Input contract version | 1"),
            "human-readable report preserves keyboard input contract provenance");
        var old = JsonNode.Parse(json)!.AsObject();
        old.Remove("inputContractVersion");
        string restored = ProloguePlaytestReportFormatter.ToJson(ProloguePlaytestReportFormatter.FromJson(old.ToJsonString()));
        using (var document = JsonDocument.Parse(restored))
            TestAssert.Equal(0, document.RootElement.GetProperty("inputContractVersion").GetInt32(),
                "historical v4 without input metadata stays unknown, never fabricated as keyboard evidence");
        old["inputContractVersion"] = 99;
        bool rejected = false;
        try { ProloguePlaytestReportFormatter.FromJson(old.ToJsonString()); }
        catch (InvalidOperationException) { rejected = true; }
        TestAssert.True(rejected, "unknown input contract version is rejected");
        GD.Print("KEYBOARD_INPUT_TELEMETRY_OK");
    }

    private static void Same(System.Collections.Generic.IEnumerable<string> actual, params string[] expected) =>
        TestAssert.Equal(string.Join("|", expected), string.Join("|", actual), "exact stable input edges and releases");
}
