using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Knight.Prologue;

internal readonly record struct ProloguePlaytestLaunchContext(
    IReadOnlyList<string> ProcessArguments,
    IReadOnlyList<string> UserArguments,
    string ProjectPath,
    DateTimeOffset NowUtc,
    string EnvironmentSource,
    Func<string, string> ConsumeGrant);

internal static class ProloguePlaytestEvidenceGrantVerifier
{
    private const string Source = "HandsOnLauncher";
    private const string GrantFileArgument = "--knight-playtest-grant-file";
    private const string GrantNonceArgument = "--knight-playtest-grant-nonce";
    private const string GrantDirectoryName = "knight-playtest-grants";
    private static readonly string[] ForbiddenLaunchArguments =
    [
        "--headless", "--editor", "-e", "--project-manager", "-p", "--script",
        "--write-movie", "--capture", "--automation"
    ];

    public static bool TryAuthorizeCurrentRun(out string evidenceSource)
    {
        evidenceSource = null;
        string projectPath = ProjectSettings.GlobalizePath("res://");
        var context = new ProloguePlaytestLaunchContext(
            OS.GetCmdlineArgs(),
            OS.GetCmdlineUserArgs(),
            projectPath,
            DateTimeOffset.UtcNow,
            OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE")
                ? OS.GetEnvironment("KNIGHT_PLAYTEST_SOURCE")
                : null,
            ConsumeGrantFileAtomically);
        return TryAuthorize(context, out evidenceSource);
    }

    internal static bool TryAuthorize(ProloguePlaytestLaunchContext context,
        out string evidenceSource)
    {
        evidenceSource = null;
        if (!TryGetSingleArgument(context.UserArguments, GrantFileArgument,
                out string suppliedGrantPath) ||
            !TryGetCanonicalDedicatedGrantPath(suppliedGrantPath, out string grantPath))
            return false;

        string json;
        try
        {
            // Claim the capability before any remaining decision. A same-directory move gives
            // exactly one process ownership of its bytes; every rejection after this point is
            // therefore terminal and cannot leave the original grant replayable.
            json = context.ConsumeGrant(grantPath);
        }
        catch
        {
            return false;
        }

        if (HasMalformedLaunchSurface(context.ProcessArguments) ||
            HasMalformedLaunchSurface(context.UserArguments) ||
            HasForbiddenLaunchSurface(context.ProcessArguments) ||
            HasForbiddenLaunchSurface(context.UserArguments) ||
            !string.Equals(context.EnvironmentSource, Source, StringComparison.Ordinal) ||
            !TryGetSingleArgument(context.UserArguments, GrantNonceArgument,
                out string argumentNonce) ||
            string.IsNullOrWhiteSpace(argumentNonce) || string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            GrantDocument grant = JsonSerializer.Deserialize<GrantDocument>(json.TrimStart('\uFEFF'),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (grant == null || grant.Version != 1 ||
                !string.Equals(grant.Source, Source, StringComparison.Ordinal) ||
                !string.Equals(grant.Nonce, argumentNonce, StringComparison.Ordinal) ||
                !CanonicalProjectEquals(grant.ProjectPath, context.ProjectPath) ||
                !DateTimeOffset.TryParse(grant.IssuedAtUtc, out DateTimeOffset issued) ||
                !DateTimeOffset.TryParse(grant.ExpiresAtUtc, out DateTimeOffset expires) ||
                issued > context.NowUtc || expires <= context.NowUtc || expires <= issued ||
                expires - issued > TimeSpan.FromSeconds(120) ||
                context.NowUtc - issued > TimeSpan.FromSeconds(120))
                return false;
            evidenceSource = Source;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string ConsumeGrantFileAtomically(string grantPath)
    {
        string claimedPath = Path.Combine(Path.GetDirectoryName(grantPath)!,
            ".consumed-" + Guid.NewGuid().ToString("N") + ".json");
        bool claimed = false;
        try
        {
            File.Move(grantPath, claimedPath);
            claimed = true;
            if ((File.GetAttributes(claimedPath) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Playtest evidence grant cannot be a reparse point.");
            return File.ReadAllText(claimedPath);
        }
        finally
        {
            // Cleanup is part of consumption. If it fails, the exception denies authorization;
            // the original capability path still remains unavailable after the successful move.
            if (claimed) File.Delete(claimedPath);
        }
    }

    private static bool HasForbiddenLaunchSurface(IEnumerable<string> arguments) =>
        (arguments ?? []).Any(argument => ForbiddenLaunchArguments.Any(forbidden =>
            string.Equals(argument, forbidden, StringComparison.OrdinalIgnoreCase) ||
            (argument?.StartsWith(forbidden + "=", StringComparison.OrdinalIgnoreCase) ?? false)));

    private static bool HasMalformedLaunchSurface(IEnumerable<string> arguments) =>
        (arguments ?? []).Any(string.IsNullOrWhiteSpace);

    private static bool TryGetSingleArgument(IReadOnlyList<string> arguments, string name,
        out string value)
    {
        value = null;
        int matches = 0;
        for (int index = 0; index < (arguments?.Count ?? 0); index++)
        {
            if (!string.Equals(arguments[index], name, StringComparison.Ordinal)) continue;
            matches++;
            if (index + 1 < arguments.Count) value = arguments[index + 1];
        }
        return matches == 1 && value != null;
    }

    private static bool TryGetCanonicalDedicatedGrantPath(string suppliedPath,
        out string canonicalPath)
    {
        canonicalPath = null;
        if (string.IsNullOrWhiteSpace(suppliedPath)) return false;
        try
        {
            string grantDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                GrantDirectoryName)).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            if (Directory.Exists(grantDirectory) &&
                (File.GetAttributes(grantDirectory) & FileAttributes.ReparsePoint) != 0)
                return false;

            string candidate = Path.GetFullPath(suppliedPath);
            string parent = Path.GetDirectoryName(candidate)?.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(parent, grantDirectory, PathComparison())) return false;
            canonicalPath = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static StringComparison PathComparison() => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static bool CanonicalProjectEquals(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        try
        {
            string canonicalLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string canonicalRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            return string.Equals(canonicalLeft, canonicalRight,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase :
                    StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private sealed class GrantDocument
    {
        public int Version { get; set; }
        public string Source { get; set; }
        public string ProjectPath { get; set; }
        public string IssuedAtUtc { get; set; }
        public string ExpiresAtUtc { get; set; }
        public string Nonce { get; set; }
    }
}
