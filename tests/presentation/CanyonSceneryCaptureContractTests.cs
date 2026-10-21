using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;

namespace Knight.Tests.Presentation;

internal static class CanyonSceneryCaptureContractTests
{
    internal static void Run(string output, string storage)
    {
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
        string userData = Path.GetFullPath(OS.GetUserDataDir());
        CanyonSceneryComparisonCapture.ValidatePaths(output, storage, project, userData);
        string caseRoot = Path.Combine(storage, $"contract-case-{Guid.NewGuid():N}");
        if (Directory.Exists(caseRoot))
            throw new InvalidOperationException("capture contract case root already exists");
        Directory.CreateDirectory(caseRoot);
        string caseOutput = Path.Combine(caseRoot, "output");
        string caseStorage = Path.Combine(caseRoot, "storage");
        Directory.CreateDirectory(caseOutput);
        Directory.CreateDirectory(caseStorage);
        string markerDirectory = Path.Combine(caseRoot, "contract-occupied");
        Directory.CreateDirectory(markerDirectory);
        string marker = Path.Combine(markerDirectory, "marker.txt");
        const string originalMarker = "case-owned-marker-must-not-be-replaced";
        using (var writer = new StreamWriter(new FileStream(marker, FileMode.CreateNew, System.IO.FileAccess.Write)))
            writer.Write(originalMarker);
        string[] childrenBefore = Directory.GetFileSystemEntries(caseRoot)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        RunValidationChecks(caseOutput, caseStorage, project, userData);
        string[] childrenAfter = Directory.GetFileSystemEntries(caseRoot)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        if (!childrenBefore.SequenceEqual(childrenAfter, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("capture contract wrote a case-owned sibling outside supplied roots");
        if (File.ReadAllText(marker) != originalMarker)
            throw new InvalidOperationException("capture contract replaced a pre-existing case-owned marker");

        string argumentOutput = Path.Combine(caseRoot, "argument-output");
        string argumentStorage = Path.Combine(caseRoot, "argument-storage");
        Directory.CreateDirectory(argumentOutput);
        Directory.CreateDirectory(argumentStorage);
        string outputArg = $"--output={argumentOutput}";
        string storageArg = $"--case-storage-root={argumentStorage}";
        var valid = CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg, storageArg], project, userData, false);
        if (valid.Output != argumentOutput || valid.Storage != argumentStorage)
            throw new InvalidOperationException("capture contract changed validated arguments");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [storageArg], project, userData, false), "missing output argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg], project, userData, false), "missing storage argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg, outputArg, storageArg], project, userData, false),
            "duplicate output argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg, storageArg, storageArg], project, userData, false),
            "duplicate storage argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            ["--output=", storageArg], project, userData, false), "empty output argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg, storageArg, "--knight-playtest-grant-file", "case-owned-grant"],
            project, userData, false), "grant file argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg, storageArg, "--knight-playtest-grant-nonce", "case-owned-nonce"],
            project, userData, false), "grant nonce argument");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidateInvocation(
            [outputArg, storageArg], project, userData, true), "grant environment");

        string linkTarget = Path.Combine(caseRoot, "reparse-target");
        Directory.CreateDirectory(Path.Combine(linkTarget, "nested"));
        string link = Path.Combine(caseRoot, "reparse-link");
        CreateCaseOwnedJunction(caseRoot, link, linkTarget);
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths(
            Path.Combine(link, "nested"), argumentStorage, project, userData),
            "reparse ancestor");
        GD.Print("TASK15_CAPTURE_CONTRACT_OK absolute_empty_unique_external=true " +
            "overlap_rejected=true arguments_rejected=true grants_rejected=true reparse_rejected=true " +
            "no_sibling_overwrite=true");
    }

    private static void CreateCaseOwnedJunction(string caseRoot, string link, string target)
    {
        string ownedPrefix = Path.GetFullPath(caseRoot).TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(link).StartsWith(ownedPrefix, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFullPath(target).StartsWith(ownedPrefix, StringComparison.OrdinalIgnoreCase) ||
            Directory.Exists(link) || File.Exists(link) || !Directory.Exists(target))
            throw new InvalidOperationException("junction fixture escaped or would replace case-owned storage");
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add("New-Item -ItemType Junction -Path $env:KNIGHT_TASK15_LINK " +
            "-Target $env:KNIGHT_TASK15_TARGET | Out-Null");
        start.Environment["KNIGHT_TASK15_LINK"] = link;
        start.Environment["KNIGHT_TASK15_TARGET"] = target;
        using Process process = Process.Start(start) ??
            throw new InvalidOperationException("could not start case-owned junction fixture");
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 || !Directory.Exists(link) ||
            (new DirectoryInfo(link).Attributes & FileAttributes.ReparsePoint) == 0)
            throw new InvalidOperationException($"case-owned junction fixture failed: {error}");
    }

    private static void RunValidationChecks(string output, string storage, string project, string userData)
    {
        CanyonSceneryComparisonCapture.ValidatePaths(output, storage, project, userData);
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths(output, output, project, userData),
            "identical output/storage");
        string overlapRoot = Path.Combine(storage, "contract-overlap");
        Directory.CreateDirectory(overlapRoot);
        string nested = Path.Combine(overlapRoot, "nested");
        Directory.CreateDirectory(nested);
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths(nested, overlapRoot, project, userData),
            "overlapping roots");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths("relative", storage, project, userData),
            "relative output");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths(project, storage, project, userData),
            "project output");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths(output, userData, project, userData),
            "real userdata storage");
        string occupied = Path.Combine(storage, "contract-occupied");
        Directory.CreateDirectory(occupied);
        using (var writer = new StreamWriter(new FileStream(Path.Combine(occupied, "marker.txt"),
                   FileMode.CreateNew, System.IO.FileAccess.Write)))
            writer.Write("test fixture");
        ExpectReject(() => CanyonSceneryComparisonCapture.ValidatePaths(occupied, output, project, userData),
            "occupied output");
    }

    private static void ExpectReject(Action action, string label)
    {
        try { action(); }
        catch (ArgumentException)
        {
            GD.Print($"TASK15_CAPTURE_REJECT_OK label={label}");
            return;
        }
        throw new InvalidOperationException($"capture contract accepted {label}");
    }
}
