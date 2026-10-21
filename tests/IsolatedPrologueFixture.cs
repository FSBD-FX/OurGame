using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Knight.Prologue;

namespace Knight.Tests;

/// <summary>
/// Gives mounted PrologueLevel test fixtures case-unique external persistence paths before
/// _Ready can read or write production defaults. Artifacts are intentionally retained.
/// </summary>
internal static class IsolatedPrologueFixture
{
    private const string StorageArgument = "--case-storage-root=";
    private static readonly HashSet<string> OwnedCases = new(StringComparer.OrdinalIgnoreCase);

    internal static IsolatedPrologueFixturePaths Configure(PrologueLevelController level,
        string caseName)
    {
        if (!GodotObject.IsInstanceValid(level) || level.IsInsideTree())
            throw new InvalidOperationException(
                "Prologue fixture isolation must be configured before AddChild.");
        return ConfigureExisting(level, CreatePaths(caseName).SavePath);
    }

    internal static IsolatedPrologueFixturePaths CreatePaths(string caseName)
    {
        string storageRoot = ValidateStorageRoot();
        string safeCase = string.Concat(caseName.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_'));
        string caseRoot = Path.Combine(storageRoot, "full-regression",
            $"{safeCase}-{Guid.NewGuid():N}");
        AssertNoReparseAncestors(caseRoot);
        Directory.CreateDirectory(caseRoot);
        AssertNoReparseAncestors(caseRoot);
        OwnedCases.Add(Path.GetFullPath(caseRoot));
        return ValidateExistingSavePath(Path.Combine(caseRoot, "save.json"));
    }

    // "Existing" means reuse this process's allocated slot; the file itself may
    // be absent, valid, completed or deliberately corrupt. No new GUID on reload.
    internal static IsolatedPrologueFixturePaths ValidateExistingSavePath(string savePath)
    {
        string storageRoot = ValidateStorageRoot();
        if (!Path.IsPathFullyQualified(savePath))
            throw new InvalidOperationException("retained fixture save must be absolute");
        string fullSave = Path.GetFullPath(savePath);
        string caseRoot = Path.GetDirectoryName(fullSave)!;
        if (!OwnedCases.Contains(caseRoot) || !Directory.Exists(caseRoot) ||
            !IsWithin(caseRoot, storageRoot))
            throw new InvalidOperationException("retained fixture slot must belong to this process's admitted case");
        var paths = new IsolatedPrologueFixturePaths(caseRoot, fullSave,
            Path.Combine(caseRoot, "settings.json"), Path.Combine(caseRoot, "playtests"));
        foreach (string path in new[]
                 { paths.SavePath, paths.SettingsPath, paths.PlaytestsDirectory })
        {
            if (!IsWithin(Path.GetFullPath(path), caseRoot))
                throw new InvalidOperationException(
                    $"fixture persistence path escaped external storage: {path}");
            AssertNoReparseAncestors(path);
        }
        string legacy = Path.Combine(caseRoot, Path.GetFileName(PrologueGameFlowController.LegacySavePath));
        AssertNoReparseAncestors(legacy);
        return paths;
    }

    internal static IsolatedPrologueFixturePaths ConfigureExisting(PrologueLevelController level, string savePath)
    {
        if (!GodotObject.IsInstanceValid(level) || level.IsInsideTree())
            throw new InvalidOperationException(
                "Prologue fixture isolation must be configured before AddChild.");
        IsolatedPrologueFixturePaths paths = ValidateExistingSavePath(savePath);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.SavePath = paths.SavePath;
        flow.SettingsPath = paths.SettingsPath;
        level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory =
            paths.PlaytestsDirectory;
        return paths;
    }

    // Read-only admission. Aggregate entry must call this before its first fixture,
    // not wait for the first Configure call somewhere inside a later test.
    internal static string ValidateStorageRoot()
    {
        string[] args = OS.GetCmdlineUserArgs();
        string supplied = Array.Find(args, value =>
            value.StartsWith(StorageArgument, StringComparison.Ordinal));
        if (supplied == null || args.Count(value => value.StartsWith(StorageArgument,
                StringComparison.Ordinal)) != 1)
            throw new InvalidOperationException(
                "full regression requires exactly one --case-storage-root=<absolute path>");
        if (args.Any(value => value is "--knight-playtest-grant-file" or
                "--knight-playtest-grant-nonce") ||
            OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"))
            throw new InvalidOperationException(
                "automated full regression rejects hands-on evidence grants");

        string suppliedRoot = supplied[StorageArgument.Length..];
        if (!Path.IsPathFullyQualified(suppliedRoot))
            throw new InvalidOperationException(
                "full-regression case storage must be an absolute directory");
        string storageRoot = Path.GetFullPath(suppliedRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(storageRoot))
            throw new InvalidOperationException(
                "full-regression case storage must be an existing absolute directory");
        AssertNoReparseAncestors(storageRoot);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string userData = Path.GetFullPath(OS.GetUserDataDir())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (IsWithin(storageRoot, project) || IsWithin(storageRoot, userData))
            throw new InvalidOperationException(
                "full-regression storage must stay outside project and application user data");

        return storageRoot;
    }

    private static void AssertNoReparseAncestors(string path)
    {
        // DirectoryInfo.Exists is false for a file; test the leaf separately so
        // seed writes cannot follow a save/settings/legacy-file symlink.
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException($"fixture storage cannot traverse a reparse point: {path}");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        for (DirectoryInfo current = new(path); current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException(
                    $"fixture storage cannot traverse a reparse point: {current.FullName}");
    }

    private static bool IsWithin(string candidate, string root)
    {
        string normalizedCandidate = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(normalizedCandidate, normalizedRoot,
                   StringComparison.OrdinalIgnoreCase) ||
               normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record IsolatedPrologueFixturePaths(string CaseRoot, string SavePath,
    string SettingsPath, string PlaytestsDirectory);
