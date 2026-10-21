using System;
using System.IO;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Tests.Unit;

public static class PrologueSaveTests
{
    public static void Run()
    {
        SchemaV1MigratesOnlyCompletionAndBestRunEvidence();
        SchemaV2RoundTripsContinuousSectorIndependentProgress();
        BossDefeatRoundTripsBeforeCompletion();
        CorruptAndNonFiniteCheckpointDataFailClosed();
        AutomationCannotClaimHumanCompletionStatistics();
        LayoutEligibilityInspectsWireTokenBeforeNormalization();
        LayoutV2RejectsUnknownStableIdsAndCanonicalizesCoordinates();
    }

    private static void SchemaV1MigratesOnlyCompletionAndBestRunEvidence()
    {
        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "legacy-v1.json");
            File.WriteAllText(path, """
                {
                  "version": 1,
                  "checkpointRoom": "FallenBellkeeperArena",
                  "checkpointEntrance": "boss_retry",
                  "checkpointX": 9999,
                  "checkpointY": 111,
                  "shortcutOpen": true,
                  "hiddenAlcoveVisited": true,
                  "completedTutorials": ["thread_anchor"],
                  "energy": 3,
                  "heat": 99,
                  "shieldActive": true,
                  "clawState": "Latched",
                  "combatState": "Attack2",
                  "completed": true,
                  "bestHumanCompletionSeconds": 640,
                  "bestHumanDeaths": 2
                }
                """);

            PrologueSaveData migrated = PrologueSaveStore.LoadOrNew(path);
            TestAssert.Equal(PrologueSaveData.CurrentVersion, migrated.Version,
                "schema-v1 save migrates explicitly to schema v2");
            TestAssert.True(migrated.Completed && migrated.BestHumanCompletionSeconds == 640d &&
                migrated.BestHumanDeaths == 2,
                "schema-v1 retains supported completion and best-run evidence");
            TestAssert.True(!migrated.HasActivatedCheckpoint && !migrated.ShortcutOpen &&
                migrated.Discoveries.Count == 0 && migrated.CompletedTutorials.Count == 0,
                "schema-v1 discards room/entrance/coordinate and continuous-world progress authority");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance.ToString(), migrated.CheckpointSector,
                "schema-v1 room identity cannot select a continuous checkpoint");
            foreach (string transient in new[] { "Energy", "Heat", "ShieldActive", "ClawState", "CombatState" })
                TestAssert.True(typeof(PrologueSaveData).GetProperty(transient) == null,
                    $"schema-v2 does not expose transient combat field {transient}");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void BossDefeatRoundTripsBeforeCompletion()
    {
        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "boss-boundary.json");
            File.WriteAllText(path, """
                {"version":2,"hasActivatedCheckpoint":true,"checkpointSector":"LostTemple",
                 "checkpointWorldX":5040,"checkpointWorldY":1294,"shortcutOpen":true,
                 "bossDefeated":true,"completed":false,"objective":"DefeatBellkeeper"}
                """);
            PrologueSaveData loaded = PrologueSaveStore.LoadOrNew(path);
            PrologueSaveStore.SaveAtomic(path, loaded);
            string roundTripped = File.ReadAllText(path);
            TestAssert.True(roundTripped.Contains("\"bossDefeated\": true", StringComparison.Ordinal) &&
                roundTripped.Contains("\"completed\": false", StringComparison.Ordinal),
                "schema-v2 persists Boss defeat independently at the real pre-completion boundary");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void SchemaV2RoundTripsContinuousSectorIndependentProgress()
    {
        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "nested", "save.json");
            var expected = new PrologueSaveData
            {
                HasActivatedCheckpoint = true,
                CheckpointSector = PrologueSectorId.LostTemple.ToString(),
                CheckpointWorldX = 10480.5f,
                CheckpointWorldY = 620.25f,
                ShortcutOpen = true,
                Discoveries = ["high_ruin", "supply_cave"],
                CompletedTutorials = ["attack_energy", "grip_stone"],
                Objective = PrologueObjective.PullTempleMechanism.ToString()
            };
            PrologueSaveStore.SaveAtomic(path, expected);
            PrologueSaveData actual = PrologueSaveStore.LoadOrNew(path);
            TestAssert.True(actual.HasActivatedCheckpoint, "explicit checkpoint activation round trips");
            TestAssert.Equal(PrologueSectorId.LostTemple.ToString(), actual.CheckpointSector,
                "continuous checkpoint sector round trips");
            TestAssert.Equal(new Vector2(10480.5f, 620.25f), actual.CheckpointWorldPosition,
                "continuous checkpoint world position round trips without room coordinates");
            TestAssert.True(actual.ShortcutOpen && actual.Discoveries.Count == 2 &&
                actual.CompletedTutorials.Count == 2,
                "shortcut, discoveries and tutorials round trip");
            TestAssert.Equal(PrologueObjective.PullTempleMechanism.ToString(), actual.Objective,
                "sector-independent objective data round trips");
            TestAssert.Equal(0, Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp").Length,
                "atomic save leaves no temporary files");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void CorruptAndNonFiniteCheckpointDataFailClosed()
    {
        string root = TempRoot();
        try
        {
            string corrupt = Path.Combine(root, "corrupt.json");
            File.WriteAllText(corrupt, "not-json");
            PrologueSaveData recovered = PrologueSaveStore.LoadOrNew(corrupt);
            TestAssert.True(recovered.WasRecovered && !recovered.HasActivatedCheckpoint &&
                !recovered.Completed,
                "malformed save recovers without claiming progress");

            string nonFinite = Path.Combine(root, "nonfinite.json");
            File.WriteAllText(nonFinite, """
                {"version":2,"hasActivatedCheckpoint":true,"checkpointSector":"CanyonBasin",
                 "checkpointWorldX":1e999,"checkpointWorldY":1250}
                """);
            PrologueSaveData rejected = PrologueSaveStore.LoadOrNew(nonFinite);
            TestAssert.True(rejected.WasRecovered && !rejected.HasActivatedCheckpoint,
                "non-finite world position is rejected fail-closed");

            AssertThrows<InvalidDataException>(() => PrologueSaveStore.SaveAtomic(
                    Path.Combine(root, "invalid-write.json"), new PrologueSaveData
                    {
                        HasActivatedCheckpoint = true,
                        CheckpointSector = PrologueSectorId.CanyonBasin.ToString(),
                        CheckpointWorldX = float.NaN,
                        CheckpointWorldY = 1f
                    }),
                "non-finite world position cannot be written as a schema-v2 save");

            foreach ((string name, float x, float y) in new[]
                     {
                         ("inactive-nan", float.NaN, 1f),
                         ("inactive-positive-infinity", float.PositiveInfinity, 1f),
                         ("inactive-negative-infinity", 1f, float.NegativeInfinity)
                     })
                AssertThrows<InvalidDataException>(() => PrologueSaveStore.SaveAtomic(
                        Path.Combine(root, name + ".json"), new PrologueSaveData
                        {
                            HasActivatedCheckpoint = false,
                            CheckpointSector = nameof(PrologueSectorId.CanyonEntrance),
                            CheckpointWorldX = x,
                            CheckpointWorldY = y
                        }),
                    $"inactive schema-v2 checkpoint coordinates reject {name} before canonicalization");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void AutomationCannotClaimHumanCompletionStatistics()
    {
        var save = new PrologueSaveData();
        TestAssert.True(!save.RecordCompletion("DeterministicCapture", 540d, 0),
            "automated capture is rejected as human evidence");
        TestAssert.True(save.Completed && save.BestHumanCompletionSeconds < 0d,
            "automated completion state cannot populate human best-run evidence");
        TestAssert.True(save.RecordCompletion("HandsOnLauncher", 620d, 3),
            "real HandsOnLauncher run may record human completion");
        TestAssert.Equal(620d, save.BestHumanCompletionSeconds,
            "human completion time is recorded");
        TestAssert.Equal(3, save.BestHumanDeaths,
            "human death count remains paired with its best time");
    }

    private static void LayoutEligibilityInspectsWireTokenBeforeNormalization()
    {
        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "layout.json");
            File.WriteAllText(path, "{\"version\":2}");
            TestAssert.Equal(0, PrologueSaveStore.LoadOrNew(path).LayoutVersion,
                "generic legacy/sample normalization preserves missing layout as zero");
            TestAssert.True(PrologueSaveStore.LoadOrNew(path, 2).WasRecovered,
                "full-world missing wire layout is refused before normalization");
            foreach (string token in new[] { "0", "1", "99", "null", "\"2\"", "2.5", "true" })
            {
                File.WriteAllText(path, "{\"version\":2,\"layoutVersion\":" + token + "}");
                TestAssert.True(PrologueSaveStore.LoadOrNew(path, 2).WasRecovered,
                    "full-world invalid layout token fails closed: " + token);
            }
            File.WriteAllText(path, "{\"version\":2,\"LayoutVersion\":2}");
            TestAssert.True(PrologueSaveStore.LoadOrNew(path, 2).WasRecovered,
                "PascalCase property cannot counterfeit camelCase wire layoutVersion");
            PrologueSaveStore.SaveAtomic(path, PrologueSaveData.NewForLayout(2));
            TestAssert.Equal(2, PrologueSaveStore.LoadOrNew(path, 2).LayoutVersion,
                "explicit layout 2 survives normalization and atomic round trip");
        }
        finally { Directory.Delete(root, true); }
    }

    private static string TempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "knight-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void LayoutV2RejectsUnknownStableIdsAndCanonicalizesCoordinates()
    {
        var data = PrologueSaveData.NewLayoutV2();
        data.CheckpointId = "canyon.v2.temple-forecourt";
        data.CheckpointWorldX = 5040;
        data.CheckpointWorldY = 1294;
        var normalized = data.Normalize();
        TestAssert.Equal(new Vector2(5920,1080), normalized.CheckpointWorldPosition,
            "CoordinateRejection: current stable ID ignores crafted old Temple coordinates");
        foreach (Action<PrologueSaveData> corrupt in new Action<PrologueSaveData>[]
        {
            save => save.CheckpointId = "canyon.v2.not-authored",
            save => save.OpenShortcuts.Add("canyon.v2.not-authored"),
            save => save.ActivatedMechanisms.Add("canyon.v2.not-authored"),
            save => save.Discoveries.Add("canyon.v2.not-authored"),
            save => save.ActivatedMechanisms.Add("canyon.v2.encounter.gear-pistons")
        })
        {
            var invalid = PrologueSaveData.NewLayoutV2();
            corrupt(invalid);
            AssertThrows<InvalidDataException>(() => invalid.Normalize(),
                "unknown stable IDs and ordinary kill markers are rejected, not silently repaired");
        }
        data.OpenShortcuts = ["canyon.v2.temple-bell-gate", "canyon.v2.upper-basin-shortcut"];
        data.ActivatedMechanisms = ["canyon.v2.steam-valve-lower", "canyon.v2.courtyard-crank",
            "canyon.v2.encounter.split-level", "canyon.v2.courtyard-seal-open"];
        TestAssert.Equal(4, data.Normalize().ActivatedMechanisms.Count,
            "real valve, crank, exact seal prerequisite and completed seal are persistable");

        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "missing-layout.json");
            File.WriteAllText(path, """
                {"version":3,"checkpointId":"canyon.v2.basin-camp","openShortcuts":[],
                 "activatedMechanisms":[],"discoveries":[]}
                """);
            TestAssert.Equal(0, PrologueSaveStore.LoadOrNew(path).LayoutVersion,
                "all-current-looking fields never synthesize the missing camelCase layout token");
            TestAssert.True(PrologueSaveStore.LoadOrNew(path, 2).WasRecovered,
                "missing layout cannot enter full-world Continue");
            File.WriteAllText(path, "{\"version\":1,\"layoutVersion\":2}");
            TestAssert.True(PrologueSaveStore.LoadOrNew(path, 2).WasRecovered,
                "a layout2 token cannot make old schema1 current or allow an unbacked overwrite");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }
}
