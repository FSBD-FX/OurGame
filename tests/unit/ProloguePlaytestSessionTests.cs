using System;
using System.Collections.Generic;
using System.Linq;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class ProloguePlaytestSessionTests
{
    public static void Run()
    {
        ContinuousRouteAndCheckpointTelemetryStayDeterministic();
        MigratedMechanicFieldsReachJsonAndMarkdownExactly();
        ReportSchemaV1MigrationIsExplicitAndFailClosed();
        VersionThreeFallContextRoundTripsEveryExactField();
        MarkdownFallCellsEscapeDynamicValues();
        VersionTwoReportMigrationLeavesFallHistoryEmpty();
        InputEdgeWindowFiltersExpiredAndUnstableSamples();
        DeathResetCannotImpersonateAFall();
        FallCaptureMustPrecedeRecoveryMutation();
        CompletionFreezesClockAndBossRetryRemainsMeasured();
    }

    private static void ContinuousRouteAndCheckpointTelemetryStayDeterministic()
    {
        var session = NewSession("route-run");
        foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>())
        {
            session.VisitSector(sector);
            session.VisitSector(sector);
        }
        session.ActivateCheckpoint(PrologueSectorId.CanyonBasin);
        session.ActivateCheckpoint(PrologueSectorId.CanyonBasin);
        session.ActivateCheckpoint(PrologueSectorId.LostTemple);

        TestAssert.Equal(5, session.SectorsVisited.Count,
            "all five continuous sectors are recorded without frame duplicates");
        TestAssert.Equal(PrologueSectorId.CanyonBasin.ToString(), session.FirstCheckpoint,
            "the first explicit checkpoint is retained independently of later Temple activation");
        TestAssert.Equal(2, session.CheckpointsActivated.Count,
            "Basin and Temple activation each produce one checkpoint record");
    }

    private static void MigratedMechanicFieldsReachJsonAndMarkdownExactly()
    {
        var session = NewSession("mechanics-run");
        foreach (PrologueSectorId sector in Enum.GetValues<PrologueSectorId>()) session.VisitSector(sector);
        session.ActivateCheckpoint(PrologueSectorId.CanyonBasin);
        session.ActivateCheckpoint(PrologueSectorId.LostTemple);
        session.RecordGrappleFailure(MechanicalClawFailure.IceSlip);
        session.RecordShieldOverheat();
        session.RecordModeSwitch(PlayerActionMode.Grapple);
        session.RecordModeSwitch(PlayerActionMode.Attack);
        session.RecordSlotSelection(1);
        session.RecordSlotSelection(4);
        session.BeginBossAttempt();
        session.RecordDeath(inBossSector: true);
        session.BeginRecovery("DEATH RESET", inBossSector: true);
        session.Advance(.25d);
        session.FinishRecovery();
        session.BeginBossAttempt();
        session.Complete(shortcutOpen: true, discoveries: ["supply_cave"]);

        ProloguePlaytestReport report = session.BuildReport(
            PrologueSectorId.LostTemple, PrologueObjective.Complete, "2026-08-26T00:10:30Z");
        string json = ProloguePlaytestReportFormatter.ToJson(report);
        string markdown = ProloguePlaytestReportFormatter.ToMarkdown(report);
        foreach (string field in new[]
                 {
                     "\"sectorsVisited\"", "\"firstCheckpoint\"",
                     "\"grappleFailuresByReason\"", "\"shieldOverheats\"",
                     "\"modeSwitches\"", "\"selectedSlots\""
                 })
            TestAssert.True(json.Contains(field, StringComparison.Ordinal),
                $"schema-v2 JSON exposes exact migrated field {field}");
        foreach (string value in new[]
                 {
                     "CanyonEntrance", "CanyonDescent", "CanyonBasin", "CanyonAscent",
                     "LostTemple", "IceSlip", "Shield overheats", "Selected slots"
                 })
            TestAssert.True(markdown.Contains(value, StringComparison.Ordinal),
                $"Markdown playtest evidence exposes {value}");
        TestAssert.True(report.BossAttempts == 2 && report.Completed && report.ShortcutOpen,
            "Boss retry/completion and shortcut persistence remain in the migrated report");
    }

    private static void ReportSchemaV1MigrationIsExplicitAndFailClosed()
    {
        const string legacy = """
            {"version":1,"runId":"legacy","startedAtUtc":"2026-08-14T00:00:00Z",
             "updatedAtUtc":"2026-08-14T00:10:00Z","evidenceSource":"HandsOnLauncher",
             "elapsedSeconds":600,"completed":true,"roomsVisited":["AwakeningCourtyard"],
             "currentRoom":"FallenBellkeeperArena","objective":"Complete"}
            """;
        ProloguePlaytestReport migrated = ProloguePlaytestReportFormatter.FromJson(legacy);
        TestAssert.Equal(ProloguePlaytestReport.CurrentVersion, migrated.Version,
            "schema-v1 report has an explicit schema-v3 migration");
        TestAssert.True(migrated.SectorsVisited.Count == 0 && migrated.FirstCheckpoint == "NotActivated",
            "schema-v1 room identity cannot impersonate continuous-world sector/checkpoint evidence");
        TestAssert.Equal(0, migrated.Falls.Count,
            "schema-v1 migration cannot invent fall coordinates or contextual telemetry");

        AssertThrows<InvalidOperationException>(() =>
                ProloguePlaytestReportFormatter.FromJson("{\"version\":999}"),
                "unknown report versions fail closed");
    }

    private static void VersionThreeFallContextRoundTripsEveryExactField()
    {
        var session = NewSession("fall-context-run");
        session.VisitSector(PrologueSectorId.CanyonDescent);
        session.RecordInputEdge("E");
        session.Advance(.5d);
        session.RecordInputEdge("LMB_DOWN");
        session.Advance(.25d);
        session.RecordInputEdge("SPACE");
        session.Advance(.25d);
        session.RecordFall(SyntheticFall(session.RecentInputEdges));

        ProloguePlaytestReport report = session.BuildReport(PrologueSectorId.CanyonDescent,
            PrologueObjective.LearnGrappleMode, "2026-08-29T00:00:01Z");
        string json = ProloguePlaytestReportFormatter.ToJson(report);
        ProloguePlaytestReport restored = ProloguePlaytestReportFormatter.FromJson(json);
        string markdown = ProloguePlaytestReportFormatter.ToMarkdown(restored);

        TestAssert.Equal(3, restored.Version, "fall context reports use schema version three");
        TestAssert.Equal(1, restored.Falls.Count, "one real fall is retained exactly once");
        PrologueFallSample fall = restored.Falls.Single();
        TestAssert.Equal("FALL RESET", fall.Reason, "fall reason round-trips exactly");
        TestAssert.Equal("CanyonDescent", fall.Sector, "fall sector round-trips exactly");
        TestAssert.Equal(new ProloguePointSample(1304.5f, 702.25f), fall.WorldPosition,
            "fall world position round-trips without normalization");
        TestAssert.Equal("Gameplay/Platforms/GripCatchShelf", fall.LastGroundedPlatformId,
            "last grounded platform uses the stable scene-relative path");
        TestAssert.Equal("NotActivated", fall.ActiveCheckpoint,
            "a fall before any checkpoint does not impersonate an activated checkpoint");
        TestAssert.Equal("Latched", fall.ClawState, "attached claw state round-trips exactly");
        TestAssert.Equal("GripStone", fall.ClawTarget, "claw target round-trips exactly");
        TestAssert.Equal("GripStone", fall.ClawMaterial, "claw material round-trips exactly");
        TestAssert.Equal("None", fall.ClawFailure, "claw failure round-trips exactly");
        TestAssert.Equal(3, fall.RecentInputs.Count, "recent input edge history round-trips exactly");
        AssertInput(fall.RecentInputs[0], 1d, "E");
        AssertInput(fall.RecentInputs[1], .5d, "LMB_DOWN");
        AssertInput(fall.RecentInputs[2], .25d, "SPACE");

        foreach (string exactJson in new[]
                 {
                     "\"falls\"", "\"reason\": \"FALL RESET\"",
                     "\"sector\": \"CanyonDescent\"", "\"x\": 1304.5",
                     "\"y\": 702.25", "\"lastGroundedPlatformId\": \"Gameplay/Platforms/GripCatchShelf\"",
                     "\"activeCheckpoint\": \"NotActivated\"", "\"clawState\": \"Latched\"",
                     "\"clawTarget\": \"GripStone\"", "\"clawMaterial\": \"GripStone\"",
                     "\"clawFailure\": \"None\"", "\"action\": \"E\"",
                     "\"action\": \"LMB_DOWN\"", "\"action\": \"SPACE\""
                 })
            TestAssert.True(json.Contains(exactJson, StringComparison.Ordinal),
                $"version-three JSON retains exact fall field {exactJson}");
        foreach (string exactMarkdown in new[]
                 {
                     "## Falls", "### Fall 1", "| Reason | FALL RESET |",
                     "| Sector | CanyonDescent |", "| World position | (1304.5, 702.25) |",
                     "| Last grounded platform | Gameplay/Platforms/GripCatchShelf |",
                     "| Active checkpoint | NotActivated |", "| Claw state | Latched |",
                     "| Claw target | GripStone |", "| Claw material | GripStone |",
                     "| Claw failure | None |",
                     "| Recent inputs | E (1.00s), LMB_DOWN (0.50s), SPACE (0.25s) |"
                 })
            TestAssert.True(markdown.Contains(exactMarkdown, StringComparison.Ordinal),
                $"version-three Markdown retains exact fall field {exactMarkdown}");
    }

    private static void VersionTwoReportMigrationLeavesFallHistoryEmpty()
    {
        const string v2Fixture = """
            {"version":2,"runId":"v2-real-fixture","startedAtUtc":"2026-08-28T00:00:00Z",
             "updatedAtUtc":"2026-08-28T00:12:00Z","evidenceSource":"HandsOnLauncher",
             "elapsedSeconds":720,"duration":"12:00","completed":true,
             "firstClearWithinTargetWindow":true,"currentSector":"LostTemple","objective":"Complete",
             "sectorsVisited":["CanyonEntrance","CanyonDescent","CanyonBasin","CanyonAscent","LostTemple"],
             "firstCheckpoint":"CanyonBasin","checkpointsActivated":["CanyonBasin","LostTemple"],
             "grappleFailuresByReason":{"IceSlip":1},"shieldOverheats":1,
             "modeSwitches":["Grapple"],"selectedSlots":[1],"totalDeaths":1,"ordinaryDeaths":1,
             "bossDeaths":0,"bossAttempts":1,"bossReentrySeconds":[],"recoveries":[],
             "shortcutOpen":true,"discoveries":["hidden_alcove"]}
            """;
        ProloguePlaytestReport migrated = ProloguePlaytestReportFormatter.FromJson(v2Fixture);
        string json = ProloguePlaytestReportFormatter.ToJson(migrated);

        TestAssert.Equal(3, migrated.Version, "real schema-v2 fixture migrates to version three");
        TestAssert.Equal(0, migrated.Falls.Count,
            "schema-v2 migration leaves fall history empty instead of inventing coordinates");
        TestAssert.True(json.Contains("\"falls\": []", StringComparison.Ordinal) &&
                        !json.Contains("worldPosition", StringComparison.Ordinal),
            "migrated schema-v2 JSON publishes an explicit empty fall list without synthetic position data");
    }

    private static void MarkdownFallCellsEscapeDynamicValues()
    {
        var session = NewSession("markdown-escape-run");
        PrologueFallSample fall = SyntheticFall(
        [
            new PrologueInputEdgeSample(0d, "E|LMB\nSPACE\\")
        ]) with
        {
            Reason = "FALL|RESET\r\nagain\\",
            LastGroundedPlatformId = "Gameplay\\Platforms|GripCatchShelf\rnext",
            ClawTarget = "Grip|Stone\nside\\",
            ClawFailure = null
        };
        ProloguePlaytestReport report = session.BuildReport(
            PrologueSectorId.CanyonDescent, PrologueObjective.LearnGrappleMode,
            "2026-09-01T00:00:01Z") with { Falls = [fall] };
        string markdown = ProloguePlaytestReportFormatter.ToMarkdown(report);

        TestAssert.True(markdown.Contains("| Reason | FALL\\|RESET<br>again\\\\ |", StringComparison.Ordinal),
            "fall reason escapes pipes, CR/LF, and backslashes inside a Markdown cell");
        TestAssert.True(markdown.Contains("| Last grounded platform | Gameplay\\\\Platforms\\|GripCatchShelf<br>next |", StringComparison.Ordinal),
            "platform identity escapes dynamic Markdown cell delimiters without losing text");
        TestAssert.True(markdown.Contains("| Claw target | Grip\\|Stone<br>side\\\\ |", StringComparison.Ordinal),
            "claw target stays in the second Markdown column after a newline and pipe");
        TestAssert.True(markdown.Contains("| Recent inputs | E\\|LMB<br>SPACE\\\\ (0.00s) |", StringComparison.Ordinal),
            "input action escapes dynamic Markdown cell delimiters without changing timing");
        TestAssert.True(markdown.Contains("| Claw failure |  |", StringComparison.Ordinal),
            "null dynamic fall fields render as an empty second Markdown cell");

        foreach (string label in new[]
                 {
                     "Reason", "Last grounded platform", "Claw target", "Claw failure", "Recent inputs"
                 })
            AssertTwoColumnMarkdownRow(markdown, label);
    }

    private static void InputEdgeWindowFiltersExpiredAndUnstableSamples()
    {
        var session = NewSession("edge-window-run");
        session.RecordInputEdge("E");
        session.RecordInputEdge("MOUSE_WORLD");
        session.Advance(1.5d);
        TestAssert.Equal(1, session.RecentInputEdges.Count,
            "the exact 1.5-second input edge boundary remains visible before recovery");
        session.Advance(.01d);
        session.RecordInputEdge("SPACE");

        IReadOnlyList<PrologueInputEdgeSample> recent = session.RecentInputEdges;
        TestAssert.Equal(1, recent.Count,
            "expired edges and unstable mouse samples do not enter fall diagnostics");
        AssertInput(recent[0], 0d, "SPACE");
    }

    private static void FallCaptureMustPrecedeRecoveryMutation()
    {
        var session = NewSession("recovery-order-run");
        PrologueFallSample fall = SyntheticFall(Array.Empty<PrologueInputEdgeSample>());
        session.RecordDeath(inBossSector: false);
        session.RecordFall(fall);
        session.BeginRecovery("FALL RESET", inBossSector: false);
        TestAssert.Equal(1, session.Falls.Count,
            "fall context is accepted before recovery starts mutating transient runtime state");
        session.FinishRecovery();
        session.RecordDeath(inBossSector: false);
        session.BeginRecovery("FALL RESET", inBossSector: false);
        session.RecordFall(fall);
        TestAssert.Equal(1, session.Falls.Count,
            "fall context is rejected after recovery begins so reset state cannot overwrite it");
    }

    private static void DeathResetCannotImpersonateAFall()
    {
        var session = NewSession("death-is-not-fall-run");
        PrologueFallSample fall = SyntheticFall(Array.Empty<PrologueInputEdgeSample>()) with
        {
            Reason = "DEATH RESET"
        };
        session.RecordFall(fall);
        TestAssert.Equal(0, session.Falls.Count,
            "ordinary death recovery never manufactures a fall-context record");
    }

    private static void CompletionFreezesClockAndBossRetryRemainsMeasured()
    {
        var session = NewSession("timing-run");
        session.BeginBossAttempt();
        session.Advance(12d);
        session.RecordDeath(inBossSector: true);
        session.BeginRecovery("DEATH RESET", inBossSector: true);
        session.Advance(.35d);
        session.FinishRecovery();
        session.Advance(2.75d);
        session.BeginBossAttempt();
        session.Advance(600d);
        session.Complete(shortcutOpen: true, discoveries: Array.Empty<string>());
        double frozen = session.ElapsedSeconds;
        session.Advance(60d);

        TestAssert.True(session.BossReentrySeconds.Count == 1 &&
            Math.Abs(session.BossReentrySeconds.Single() - 2.75d) < .001d,
            "Boss retry delay is measured from restored control");
        TestAssert.True(Math.Abs(session.ElapsedSeconds - frozen) < .001d,
            "completion freezes elapsed gameplay time");
    }

    private static ProloguePlaytestSession NewSession(string runId) =>
        new(runId, "2026-08-26T00:00:00Z", "HandsOnLauncher");

    private static PrologueFallSample SyntheticFall(IReadOnlyList<PrologueInputEdgeSample> recentInputs) => new(
        "FALL RESET", "CanyonDescent", new ProloguePointSample(1304.5f, 702.25f),
        "Gameplay/Platforms/GripCatchShelf", "NotActivated", "Latched", "GripStone", "GripStone",
        "None", recentInputs);

    private static void AssertInput(PrologueInputEdgeSample input, double seconds,
        string action)
    {
        TestAssert.True(Math.Abs(input.SecondsBeforeRecovery - seconds) < .0001d,
            $"input {action} retains its exact pre-recovery timestamp");
        TestAssert.Equal(action, input.Action, $"input action {action} round-trips exactly");
    }

    private static void AssertTwoColumnMarkdownRow(string markdown, string label)
    {
        string line = markdown.Split('\n').Single(value => value.StartsWith($"| {label} | ",
            StringComparison.Ordinal)).TrimEnd('\r');
        TestAssert.Equal(3, CountUnescapedPipes(line),
            $"dynamic {label} telemetry remains a two-column Markdown row");
        TestAssert.True(!line.Contains('\r') && !line.Contains('\n'),
            $"dynamic {label} telemetry cannot split its Markdown row");
    }

    private static int CountUnescapedPipes(string value)
    {
        int count = 0;
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '|') continue;
            int slashes = 0;
            for (int prior = index - 1; prior >= 0 && value[prior] == '\\'; prior--) slashes++;
            if (slashes % 2 == 0) count++;
        }
        return count;
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException(message);
    }
}
