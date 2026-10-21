#nullable enable
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;
using System;
using System.IO;

namespace Knight.Tests;

public partial class RunTask16PressureGateTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 2 || !args[1].StartsWith("--case-storage-root=", System.StringComparison.Ordinal) ||
                args[0] is not ("--model-only" or "--runtime-only" or "--all" or
                    "--fix1-mount-clear" or "--fix1-mount-occupied" or "--fix1-visual" or
                    "--fix1-pause-timing" or "--fix1-all"))
                throw new System.ArgumentException("Task16A accepts only an explicit mode and case storage root.");
            ValidateStorage(args[1].Substring("--case-storage-root=".Length));
            if (args[0] is "--model-only" or "--all") CanyonPressureGateModelTests.Run();
            if (args[0] is "--runtime-only" or "--all") await CanyonPressureGateRuntimeTests.Run(this);
            if (args[0] is "--fix1-mount-clear" or "--fix1-all")
                await CanyonPressureGateRuntimeTests.RunFix1MountClear(this);
            if (args[0] is "--fix1-mount-occupied" or "--fix1-all")
                await CanyonPressureGateRuntimeTests.RunFix1MountOccupied(this);
            if (args[0] is "--fix1-visual" or "--fix1-all")
                await CanyonPressureGateRuntimeTests.RunFix1Visual(this);
            if (args[0] is "--fix1-pause-timing" or "--fix1-all")
                await CanyonPressureGateRuntimeTests.RunFix1PauseTiming(this);
            GD.Print("TASK16A_PRESSURE_GATE_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static void ValidateStorage(string raw)
    {
        if (!Path.IsPathFullyQualified(raw))
            throw new ArgumentException("Task16A storage path must be absolute.");
        string storage = Path.GetFullPath(raw)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string evidence = Path.GetFullPath(
            "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task16a")
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string expectedProject = Path.GetFullPath(
            "E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555")
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? caseDirectory = Path.GetDirectoryName(storage);
        string? caseParent = caseDirectory == null ? null : Path.GetDirectoryName(caseDirectory);
        string expectedTemp = caseDirectory == null ? "" : Path.Combine(caseDirectory, "unit-temp");
        string actualTemp = Path.GetFullPath(Path.GetTempPath())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(storage) ||
            !Directory.Exists(expectedTemp) ||
            !string.Equals(caseParent, evidence, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(storage), "storage", StringComparison.Ordinal) ||
            !string.Equals(project, expectedProject, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(actualTemp, expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Task16A storage must be an existing dedicated evidence case.");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string? value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !string.Equals(Path.GetFullPath(value)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Task16A {name} must resolve inside the dedicated case.");
        }
        for (DirectoryInfo? directory = new(storage); directory != null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Task16A storage rejects reparse ancestors.");
        for (DirectoryInfo? directory = new(expectedTemp); directory != null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Task16A temporary path rejects reparse ancestors.");
    }
}
