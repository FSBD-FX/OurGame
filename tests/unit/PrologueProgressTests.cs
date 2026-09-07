using System;
using System.IO;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class PrologueProgressTests
{
    private static readonly Vector2 Start = new(140f, 420f);
    private static readonly Vector2 Basin = new(5520f, 1250f);
    private static readonly Vector2 Temple = new(10480f, 620f);

    public static void Run()
    {
        RestoreUsesStartUntilAnExplicitApprovedCheckpointActivates();
        TempleSupersedesBasinOnlyAfterExplicitActivation();
        LoadedOrVisitedExtentNeverSelectsCheckpointAuthority();
        CheckpointSnapshotPreservesPermanentDiscoveriesAndTutorials();
        InvalidActivationPreservesLastValidAuthority();
        BossDefeatIsSessionProgress();
        ContinuousAuthorityContainsNoLegacyAliases();
    }

    private static void RestoreUsesStartUntilAnExplicitApprovedCheckpointActivates()
    {
        var progress = new PrologueProgress(Start);
        PrologueCheckpointSnapshot initial = progress.RestoreCheckpoint();
        TestAssert.True(!initial.HasActivatedCheckpoint,
            "new run has no explicit Basin or Temple checkpoint activation");
        TestAssert.Equal(PrologueSectorId.CanyonEntrance, initial.SectorId,
            "death before Basin restores to the authored start sector");
        TestAssert.Equal(Start, initial.WorldPosition,
            "death before Basin restores to the authored start position");

        TestAssert.True(progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, Basin),
            "explicit physical Basin activation gains checkpoint authority");
        PrologueCheckpointSnapshot basin = progress.RestoreCheckpoint();
        TestAssert.True(basin.HasActivatedCheckpoint && basin.SectorId == PrologueSectorId.CanyonBasin,
            "death after Basin restores the explicitly activated Basin checkpoint");
        TestAssert.Equal(Basin, basin.WorldPosition, "Basin world position is preserved exactly");
    }

    private static void TempleSupersedesBasinOnlyAfterExplicitActivation()
    {
        var progress = new PrologueProgress(Start);
        progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, Basin);
        PrologueCheckpointSnapshot beforeTemple = progress.RestoreCheckpoint();
        TestAssert.Equal(PrologueSectorId.CanyonBasin, beforeTemple.SectorId,
            "entering or mounting Temple cannot implicitly supersede Basin");

        TestAssert.True(progress.ActivateCheckpoint(PrologueSectorId.LostTemple, Temple),
            "explicit physical Temple activation gains checkpoint authority");
        PrologueCheckpointSnapshot afterTemple = progress.RestoreCheckpoint();
        TestAssert.Equal(PrologueSectorId.LostTemple, afterTemple.SectorId,
            "death after Temple restores the explicit Temple checkpoint");
        TestAssert.Equal(Temple, afterTemple.WorldPosition,
            "Temple activation stores a finite continuous-world position");
    }

    private static void LoadedOrVisitedExtentNeverSelectsCheckpointAuthority()
    {
        var progress = new PrologueProgress(Start);
        foreach (PrologueSectorId unauthorized in new[]
                 {
                     PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent,
                     PrologueSectorId.CanyonAscent
                 })
            TestAssert.True(!progress.ActivateCheckpoint(unauthorized, new Vector2(999f, 999f)),
                $"mounted/visited sector {unauthorized} cannot become checkpoint authority");
        TestAssert.Equal(PrologueSectorId.CanyonEntrance, progress.RestoreCheckpoint().SectorId,
            "furthest mounted or visited sector never changes the start restore authority");
    }

    private static void CheckpointSnapshotPreservesPermanentDiscoveriesAndTutorials()
    {
        var progress = new PrologueProgress(Start);
        progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, Basin);
        progress.OpenBellChainShortcut();
        progress.VisitHiddenAlcove();
        progress.CompleteTutorial("grip_stone");

        PrologueCheckpointSnapshot snapshot = progress.RestoreCheckpoint();
        TestAssert.True(snapshot.ShortcutOpen, "shortcut opened after checkpoint survives death");
        TestAssert.True(snapshot.Progress.Discoveries.Contains("hidden_alcove") &&
            progress.CompletedTutorials.Contains("grip_stone"),
            "optional discovery and completed guidance remain permanent progress without snapshot aliases");
    }

    private static void InvalidActivationPreservesLastValidAuthority()
    {
        var progress = new PrologueProgress(Start);
        progress.ActivateCheckpoint(PrologueSectorId.CanyonBasin, Basin);
        foreach (Vector2 invalid in new[]
                 {
                     new Vector2(float.NaN, 1f), new Vector2(1f, float.PositiveInfinity)
                 })
            TestAssert.True(!progress.ActivateCheckpoint(PrologueSectorId.LostTemple, invalid),
                "non-finite checkpoint positions fail closed");
        TestAssert.Equal(Basin, progress.RestoreCheckpoint().WorldPosition,
            "invalid activation preserves the last valid checkpoint position");
    }

    private static void BossDefeatIsSessionProgress()
    {
        var progress = new PrologueProgress(Start);
        TestAssert.True(progress.MarkBossDefeated(), "first Boss defeat changes session state");
        TestAssert.True(!progress.MarkBossDefeated(), "repeated Boss defeat is idempotent");
        TestAssert.True(progress.BossDefeated, "Boss completion stays marked for the session");
    }

    private static void ContinuousAuthorityContainsNoLegacyAliases()
    {
        string checkpoint = File.ReadAllText(ProjectSettings.GlobalizePath(
            "res://scripts/prologue/PrologueCheckpointSnapshot.cs"));
        foreach (string forbidden in new[]
                 {
                     "PrologueRoomId", "LegacyRoom", "LegacyEntrance", " Room =>", " Entrance =>",
                     "BellChainShortcutOpen =>", "HiddenAlcoveVisited =>", "CompletedTutorials =>"
                 })
            TestAssert.True(!checkpoint.Contains(forbidden, StringComparison.Ordinal),
                $"continuous checkpoint snapshot removes legacy alias '{forbidden}'");

        string progress = File.ReadAllText(ProjectSettings.GlobalizePath(
            "res://scripts/prologue/PrologueProgress.cs"));
        foreach (string forbidden in new[]
                 { "_legacyCheckpoint", "ActivateCheckpoint(PrologueRoomId", "RestoreLegacy(" })
            TestAssert.True(!progress.Contains(forbidden, StringComparison.Ordinal),
                $"continuous progress authority removes legacy surface '{forbidden}'");

        string save = File.ReadAllText(ProjectSettings.GlobalizePath(
            "res://scripts/save/PrologueSaveData.cs"));
        foreach (string forbidden in new[]
                 { "public string CheckpointRoom", "public string CheckpointEntrance",
                     "public bool HiddenAlcoveVisited" })
            TestAssert.True(!save.Contains(forbidden, StringComparison.Ordinal),
                $"schema-v2 save removes JsonIgnore legacy alias '{forbidden}'");

        string guidance = File.ReadAllText(ProjectSettings.GlobalizePath(
            "res://scripts/prologue/guidance/PrologueGuidanceController.cs"));
        TestAssert.True(!guidance.Contains("SameRoomFailureCount", StringComparison.Ordinal),
            "continuous guidance removes the legacy same-room failure alias");
    }
}
