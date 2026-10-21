using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Knight.Prologue;

public sealed record PrologueRecoverySample(string Reason, bool InBossSector, double Seconds);

public sealed record ProloguePointSample(float X, float Y);

public sealed record PrologueInputEdgeSample(double SecondsBeforeRecovery, string Action);

public sealed record PrologueFallSample(
    string Reason,
    string Sector,
    ProloguePointSample WorldPosition,
    string LastGroundedPlatformId,
    string ActiveCheckpoint,
    string ClawState,
    string ClawTarget,
    string ClawMaterial,
    string ClawFailure,
    IReadOnlyList<PrologueInputEdgeSample> RecentInputs);

public sealed record PrologueRouteNodeSample(string Node, string Event, double Seconds);

public sealed record PrologueEncounterSample(string EncounterId, string Event, double Seconds);

public sealed record PrologueMechanismSample(string MechanismId, double Seconds);

public sealed record PrologueShortcutTraversalSample(
    string ShortcutId, string From, string To, double Seconds);

public sealed record PrologueFireflyTargetSample(string Target, double Seconds);

public sealed record ProloguePlaytestReport(
    int Version,
    string RunId,
    string StartedAtUtc,
    string UpdatedAtUtc,
    string EvidenceSource,
    double ElapsedSeconds,
    string Duration,
    bool Completed,
    bool FirstClearWithinTargetWindow,
    string CurrentSector,
    string Objective,
    IReadOnlyList<string> SectorsVisited,
    string FirstCheckpoint,
    IReadOnlyList<string> CheckpointsActivated,
    IReadOnlyDictionary<string, int> GrappleFailuresByReason,
    int ShieldOverheats,
    IReadOnlyList<string> ModeSwitches,
    IReadOnlyList<int> SelectedSlots,
    int TotalDeaths,
    int OrdinaryDeaths,
    int BossDeaths,
    int BossAttempts,
    IReadOnlyList<double> BossReentrySeconds,
    IReadOnlyList<PrologueRecoverySample> Recoveries,
    bool ShortcutOpen,
    IReadOnlyList<string> Discoveries,
    IReadOnlyList<PrologueFallSample> Falls)
{
    public const int CurrentVersion = 4;
    // 0: historical/unspecified. 1: independent keyboard requests plus explicitly
    // named legacy adapter edges. This does not certify a keyboard-only playthrough.
    public int InputContractVersion { get; init; }
    public IReadOnlyList<PrologueRouteNodeSample> RouteNodes { get; init; } =
        Array.Empty<PrologueRouteNodeSample>();
    public IReadOnlyList<PrologueEncounterSample> Encounters { get; init; } =
        Array.Empty<PrologueEncounterSample>();
    public IReadOnlyList<PrologueMechanismSample> Mechanisms { get; init; } =
        Array.Empty<PrologueMechanismSample>();
    public IReadOnlyList<PrologueShortcutTraversalSample> ShortcutTraversals { get; init; } =
        Array.Empty<PrologueShortcutTraversalSample>();
    public IReadOnlyList<PrologueFireflyTargetSample> FireflyTargets { get; init; } =
        Array.Empty<PrologueFireflyTargetSample>();
}

public static class ProloguePlaytestReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string ToJson(ProloguePlaytestReport report) =>
        JsonSerializer.Serialize(report ?? throw new ArgumentNullException(nameof(report)), JsonOptions);

    public static ProloguePlaytestReport FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Report JSON is empty.");
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("version", out JsonElement versionElement) ||
                !versionElement.TryGetInt32(out int version))
                throw new InvalidOperationException("Report version is missing.");
            if (version == ProloguePlaytestReport.CurrentVersion)
            {
                ProloguePlaytestReport current = JsonSerializer.Deserialize<ProloguePlaytestReport>(json, JsonOptions) ??
                    throw new InvalidOperationException("Report payload is null.");
                if (current.Falls == null)
                    throw new InvalidOperationException("Report v3 fall history is missing.");
                if (current.InputContractVersion is not (0 or 1))
                    throw new InvalidOperationException("Unsupported input contract version.");
                if (current.RouteNodes == null || current.Encounters == null ||
                    current.Mechanisms == null || current.ShortcutTraversals == null ||
                    current.FireflyTargets == null)
                    throw new InvalidOperationException("Report v3 route telemetry cannot be null.");
                return current;
            }
            if (version is 2 or 3)
            {
                // V2 had no fall context and V3 had no ordered route telemetry. Migrate absent
                // evidence to empty lists rather than fabricating observations.
                ProloguePlaytestReport legacy = JsonSerializer.Deserialize<ProloguePlaytestReport>(json, JsonOptions) ??
                    throw new InvalidOperationException("Report payload is null.");
                if (version == 3 && (!document.RootElement.TryGetProperty("falls", out JsonElement falls) ||
                                     falls.ValueKind == JsonValueKind.Null || legacy.Falls == null))
                    throw new InvalidOperationException("Report v3 fall history is missing.");
                return legacy with
                {
                    Version = ProloguePlaytestReport.CurrentVersion,
                    InputContractVersion = 0,
                    Falls = version == 2 ? Array.Empty<PrologueFallSample>() : legacy.Falls,
                    RouteNodes = Array.Empty<PrologueRouteNodeSample>(),
                    Encounters = Array.Empty<PrologueEncounterSample>(),
                    Mechanisms = Array.Empty<PrologueMechanismSample>(),
                    ShortcutTraversals = Array.Empty<PrologueShortcutTraversalSample>(),
                    FireflyTargets = Array.Empty<PrologueFireflyTargetSample>()
                };
            }
            if (version != 1)
                throw new InvalidOperationException($"Unsupported report version {version}.");
            JsonElement root = document.RootElement;
            double elapsed = ReadDouble(root, "elapsedSeconds");
            return new ProloguePlaytestReport(
                ProloguePlaytestReport.CurrentVersion,
                ReadString(root, "runId", "migrated-v1"),
                ReadString(root, "startedAtUtc", string.Empty),
                ReadString(root, "updatedAtUtc", string.Empty),
                ReadString(root, "evidenceSource", "UnknownV1"),
                elapsed,
                FormatDuration(elapsed),
                ReadBool(root, "completed"),
                ReadBool(root, "completed") && elapsed is >= 600d and <= 900d,
                "UnknownSector",
                ReadString(root, "objective", nameof(PrologueObjective.Wake)),
                Array.Empty<string>(),
                "NotActivated",
                Array.Empty<string>(),
                new Dictionary<string, int>(StringComparer.Ordinal),
                0,
                Array.Empty<string>(),
                Array.Empty<int>(),
                0, 0, 0, 0,
                Array.Empty<double>(),
                Array.Empty<PrologueRecoverySample>(),
                false,
                Array.Empty<string>(),
                Array.Empty<PrologueFallSample>());
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Malformed report JSON.", exception);
        }
    }

    public static string ToMarkdown(ProloguePlaytestReport report)
    {
        var text = new StringBuilder();
        text.AppendLine($"# Prologue playtest — {MarkdownCell(report.RunId)}");
        text.AppendLine();
        text.AppendLine("| Metric | Result |");
        text.AppendLine("| --- | --- |");
        text.AppendLine($"| Report version | {report.Version} |");
        text.AppendLine($"| Input contract version | {report.InputContractVersion} |");
        text.AppendLine($"| Duration | {MarkdownCell(report.Duration)} |");
        text.AppendLine($"| Evidence source | {MarkdownCell(report.EvidenceSource)} |");
        text.AppendLine($"| Completed | {YesNo(report.Completed)} |");
        text.AppendLine($"| 10–15 minute target | {YesNo(report.FirstClearWithinTargetWindow)} |");
        text.AppendLine($"| Sectors visited | {MarkdownCell(string.Join(" → ", report.SectorsVisited))} |");
        text.AppendLine($"| First checkpoint | {MarkdownCell(report.FirstCheckpoint)} |");
        text.AppendLine($"| Checkpoints | {MarkdownCell(string.Join(", ", report.CheckpointsActivated))} |");
        text.AppendLine($"| Grapple failures by reason | {MarkdownCell(FormatDictionary(report.GrappleFailuresByReason))} |");
        text.AppendLine($"| Shield overheats | {report.ShieldOverheats} |");
        text.AppendLine($"| Mode switches | {MarkdownCell(string.Join(", ", report.ModeSwitches))} |");
        text.AppendLine($"| Selected slots | {string.Join(", ", report.SelectedSlots)} |");
        text.AppendLine($"| Deaths | {report.TotalDeaths} total; {report.OrdinaryDeaths} ordinary; {report.BossDeaths} Boss |");
        text.AppendLine($"| Boss attempts | {report.BossAttempts} |");
        text.AppendLine($"| Boss reentry | {MarkdownCell(FormatReentries(report.BossReentrySeconds))} |");
        text.AppendLine($"| Recovery to control | {MarkdownCell(FormatRecoveries(report.Recoveries))} |");
        text.AppendLine($"| Shortcut persisted | {YesNo(report.ShortcutOpen)} |");
        text.AppendLine($"| Discoveries | {MarkdownCell(string.Join(", ", report.Discoveries))} |");
        text.AppendLine($"| Falls | {report.Falls?.Count ?? 0} |");
        text.AppendLine($"| Route-node events | {report.RouteNodes?.Count ?? 0} |");
        text.AppendLine($"| Encounter events | {report.Encounters?.Count ?? 0} |");
        text.AppendLine($"| Mechanism activations | {report.Mechanisms?.Count ?? 0} |");
        text.AppendLine($"| Shortcut traversals | {report.ShortcutTraversals?.Count ?? 0} |");
        text.AppendLine($"| Firefly targets | {report.FireflyTargets?.Count ?? 0} |");
        text.AppendLine($"| Last state | {MarkdownCell($"{report.CurrentSector} / {report.Objective}")} |");
        text.AppendLine();
        text.AppendLine("## Falls");
        text.AppendLine();
        if (report.Falls == null || report.Falls.Count == 0)
            text.AppendLine("- none");
        else
        {
            for (int index = 0; index < report.Falls.Count; index++)
                AppendFall(text, index + 1, report.Falls[index]);
        }
        text.AppendLine();
        text.AppendLine("## Human notes");
        text.AppendLine();
        text.AppendLine("- Unclear prompts: _fill after play_");
        text.AppendLine("- Feel / blockers: _fill after play_");
        text.AppendLine("- Route reasoning: _fill after play_");
        return text.ToString();
    }

    public static string FormatDuration(double seconds)
    {
        int wholeSeconds = Math.Max(0, (int)Math.Floor(double.IsFinite(seconds) ? seconds : 0d));
        return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
    }

    private static string FormatDictionary(IReadOnlyDictionary<string, int> values)
    {
        if (values.Count == 0) return "none";
        var parts = new List<string>();
        foreach ((string key, int value) in values) parts.Add($"{key}: {value}");
        parts.Sort(StringComparer.Ordinal);
        return string.Join(", ", parts);
    }

    private static string FormatReentries(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return "not measured";
        var formatted = new string[values.Count];
        for (int index = 0; index < values.Count; index++) formatted[index] = $"{values[index]:0.00}s";
        return string.Join(", ", formatted);
    }

    private static string FormatRecoveries(IReadOnlyList<PrologueRecoverySample> values)
    {
        if (values.Count == 0) return "not measured";
        var formatted = new string[values.Count];
        for (int index = 0; index < values.Count; index++)
            formatted[index] = $"{values[index].Reason}: {values[index].Seconds:0.00}s";
        return string.Join(", ", formatted);
    }

    private static void AppendFall(StringBuilder text, int ordinal, PrologueFallSample fall)
    {
        text.AppendLine($"### Fall {ordinal}");
        text.AppendLine();
        text.AppendLine("| Field | Value |");
        text.AppendLine("| --- | --- |");
        text.AppendLine($"| Reason | {MarkdownCell(fall.Reason)} |");
        text.AppendLine($"| Sector | {MarkdownCell(fall.Sector)} |");
        text.AppendLine($"| World position | {MarkdownCell(FormatPoint(fall.WorldPosition))} |");
        text.AppendLine($"| Last grounded platform | {MarkdownCell(fall.LastGroundedPlatformId)} |");
        text.AppendLine($"| Active checkpoint | {MarkdownCell(fall.ActiveCheckpoint)} |");
        text.AppendLine($"| Claw state | {MarkdownCell(fall.ClawState)} |");
        text.AppendLine($"| Claw target | {MarkdownCell(fall.ClawTarget)} |");
        text.AppendLine($"| Claw material | {MarkdownCell(fall.ClawMaterial)} |");
        text.AppendLine($"| Claw failure | {MarkdownCell(fall.ClawFailure)} |");
        text.AppendLine($"| Recent inputs | {MarkdownCell(FormatInputs(fall.RecentInputs))} |");
        text.AppendLine();
    }

    private static string FormatPoint(ProloguePointSample point) =>
        $"({point.X.ToString("R", CultureInfo.InvariantCulture)}, " +
        $"{point.Y.ToString("R", CultureInfo.InvariantCulture)})";

    private static string FormatInputs(IReadOnlyList<PrologueInputEdgeSample> values)
    {
        if (values == null || values.Count == 0) return "none";
        var formatted = new string[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            PrologueInputEdgeSample input = values[index];
            formatted[index] = $"{input.Action} ({input.SecondsBeforeRecovery.ToString("0.00", CultureInfo.InvariantCulture)}s)";
        }
        return string.Join(", ", formatted);
    }

    private static string MarkdownCell(string value) => (value ?? string.Empty)
        .Replace("\\", "\\\\")
        .Replace("|", "\\|")
        .Replace("\r\n", "\n")
        .Replace("\r", "\n")
        .Replace("\n", "<br>");

    private static string ReadString(JsonElement root, string name, string fallback) =>
        root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback : fallback;
    private static double ReadDouble(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.TryGetDouble(out double number) &&
        double.IsFinite(number) ? number : 0d;
    private static bool ReadBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;
    private static string YesNo(bool value) => value ? "yes" : "no";
}
