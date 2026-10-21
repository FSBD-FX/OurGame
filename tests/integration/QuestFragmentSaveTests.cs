using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Tests.Integration;

public static class QuestFragmentSaveTests
{
    public static void Run()
    {
        string caseRoot = OS.GetCmdlineUserArgs().Single(value =>
            value.StartsWith("--case-storage-root=", StringComparison.Ordinal))["--case-storage-root=".Length..];
        TestAssert.True(Path.IsPathFullyQualified(caseRoot), "fragment save fixtures require explicit absolute case storage");
        string root = Path.Combine(caseRoot, "fragment-save-fixtures");
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "prologue-save-layout-v2.json");

        void Write(JsonObject data) => File.WriteAllText(path, data.ToJsonString());
        PrologueSaveData ReadUnchanged()
        {
            string before = Hash(path);
            PrologueSaveData data = PrologueSaveStore.LoadOrNew(path, 2);
            TestAssert.Equal(before, Hash(path), "fragment migration never rewrites its source slot");
            return data;
        }
        foreach ((string checkpoint, bool activated, bool boss, bool complete, bool exempt) in new[]
        {
            ("canyon.v2.start", false, false, false, false),
            ("canyon.v2.basin-camp", true, false, false, false),
            ("canyon.v2.temple-forecourt", true, false, false, true),
            ("canyon.v2.temple-forecourt", false, false, false, false),
            ("canyon.v2.basin-camp", true, true, false, true),
            ("canyon.v2.start", false, false, true, true)
        })
        {
            var wire = OldThree(checkpoint, activated);
            wire["bossDefeated"] = boss;
            wire["completed"] = complete;
            // Even a supplied old-format quota flag/ids cannot manufacture new progress.
            wire["legacyCollectionQuotaExempt"] = true;
            wire["collectedFragmentIds"] = new JsonArray("canyon.v2.fragment.entrance");
            Write(wire);
            var data = ReadUnchanged();
            TestAssert.True(!data.WasRecovered && data.Version == 4,
                "schema3 migrates explicitly to schema4 without a recovered/restart boundary");
            TestAssert.Equal(exempt, data.LegacyCollectionQuotaExempt,
                $"raw old progress, not normalized location or injected metadata, sets quota exemption: {checkpoint}/{activated}/{boss}/{complete}");
            TestAssert.True(data.CollectionRuleVersion == 1 && data.CollectedFragmentIds.Count == 0 &&
                data.OpenShortcuts.Count == 0 && data.ActivatedMechanisms.Count == 0,
                "migration preserves eligibility without inventing fragments, bell shortcut or courtyard seal");
        }
        var missingActivation = OldThree("canyon.v2.temple-forecourt", true);
        missingActivation.Remove("hasActivatedCheckpoint");
        Write(missingActivation);
        var withoutRawActivation = ReadUnchanged();
        TestAssert.True(!withoutRawActivation.WasRecovered && withoutRawActivation.HasActivatedCheckpoint &&
            withoutRawActivation.CheckpointId == "canyon.v2.temple-forecourt" && !withoutRawActivation.LegacyCollectionQuotaExempt,
            "Normalize may canonicalize checkpoint activation but must not create a raw migration entitlement");

        var richLegacy = OldThree("canyon.v2.temple-forecourt", true);
        richLegacy["openShortcuts"] = new JsonArray("canyon.v2.temple-bell-gate", "canyon.v2.upper-basin-shortcut");
        richLegacy["activatedMechanisms"] = new JsonArray("canyon.v2.courtyard-seal-open", "canyon.v2.courtyard-crank");
        richLegacy["discoveries"] = new JsonArray("supply_cave");
        richLegacy["completedTutorials"] = new JsonArray("grip_stone");
        richLegacy["bossDefeated"] = true;
        richLegacy["completed"] = true;
        richLegacy["objective"] = "Complete";
        richLegacy["bestHumanCompletionSeconds"] = 640;
        richLegacy["bestHumanDeaths"] = 2;
        Write(richLegacy);
        var retained = ReadUnchanged();
        TestAssert.True(!retained.WasRecovered && retained.CheckpointId == "canyon.v2.temple-forecourt" &&
            retained.CheckpointWorldPosition == new Vector2(5920,1080) && retained.HasActivatedCheckpoint &&
            retained.OpenShortcuts.Count == 2 && retained.ActivatedMechanisms.Count == 2 &&
            retained.Discoveries.SequenceEqual(new[] { "supply_cave" }) &&
            retained.CompletedTutorials.SequenceEqual(new[] { "grip_stone" }) &&
            retained.BossDefeated && retained.Completed && retained.Objective == "Complete" &&
            retained.BestHumanCompletionSeconds == 640 && retained.BestHumanDeaths == 2,
            "legacy migration retains independent checkpoint, controls, discoveries, tutorial and completion evidence");
        var restoredProgress = new PrologueProgress();
        restoredProgress.Restore(retained);
        var restoredCandidate = restoredProgress.ToSave(retained);
        TestAssert.True(restoredCandidate.CheckpointId == retained.CheckpointId &&
            restoredCandidate.OpenShortcuts.SequenceEqual(retained.OpenShortcuts) &&
            restoredCandidate.ActivatedMechanisms.SequenceEqual(retained.ActivatedMechanisms) &&
            restoredCandidate.Discoveries.SequenceEqual(retained.Discoveries) &&
            restoredCandidate.CompletedTutorials.SequenceEqual(retained.CompletedTutorials) &&
            restoredCandidate.BossDefeated && restoredCandidate.Completed && restoredCandidate.Objective == retained.Objective &&
            restoredCandidate.BestHumanCompletionSeconds == 640 && restoredCandidate.BestHumanDeaths == 2 &&
            restoredCandidate.LegacyCollectionQuotaExempt && restoredCandidate.CollectedFragmentIds.Count == 0,
            "migration, live Restore and detached ToSave retain old durable progress without fake fragments");

        foreach (Action<JsonObject> corrupt in new Action<JsonObject>[]
        {
            data => data["checkpointId"] = "canyon.v2.foreign",
            data => data["openShortcuts"] = new JsonArray("canyon.v2.foreign"),
            data => data["activatedMechanisms"] = new JsonArray("canyon.v2.foreign"),
            data => data["discoveries"] = new JsonArray("canyon.v2.foreign"),
            data => data["objective"] = "NotAnObjective",
            data => data["hasActivatedCheckpoint"] = "true",
            data => data["bossDefeated"] = 1,
            data => data["version"] = "3",
            data => data["layoutVersion"] = "2",
            data => data.Remove("layoutVersion")
        })
        {
            var data = OldThree("canyon.v2.temple-forecourt", true);
            corrupt(data);
            Write(data);
            var rejected = ReadUnchanged();
            TestAssert.True(rejected.WasRecovered && !rejected.LegacyCollectionQuotaExempt && rejected.CollectedFragmentIds.Count == 0,
                "damaged legacy payload cannot gain quota entitlement");
        }

        var current = PrologueSaveData.NewLayoutV2();
        TestAssert.True(current.Version == 4 && current.CollectionRuleVersion == 1 &&
            !current.LegacyCollectionQuotaExempt && current.CollectedFragmentIds.Count == 0,
            "fresh schema4 starts with no fragments or exemption");
        current.CheckpointId = "canyon.v2.temple-forecourt";
        current.HasActivatedCheckpoint = true;
        current.CheckpointWorldX = 6000;
        current.CheckpointWorldY = 1000;
        current.CollectedFragmentIds = ["canyon.v2.fragment.entrance", "canyon.v2.fragment.entrance"];
        PrologueSaveStore.SaveAtomic(path, current);
        var roundTrip = ReadUnchanged();
        TestAssert.True(!roundTrip.WasRecovered && !roundTrip.LegacyCollectionQuotaExempt &&
            roundTrip.CollectedFragmentIds.SequenceEqual(new[] { "canyon.v2.fragment.entrance" }),
            "current schema location never derives exemption, and distinct fragment ids round-trip");
        current.LegacyCollectionQuotaExempt = true;
        PrologueSaveStore.SaveAtomic(path, current);
        TestAssert.True(ReadUnchanged().LegacyCollectionQuotaExempt,
            "explicit legitimately migrated quota entitlement survives schema4 round trip");
        var validCurrent = JsonNode.Parse(File.ReadAllText(path)).AsObject();
        foreach (Action<JsonObject> corrupt in new Action<JsonObject>[]
        {
            data => data.Remove("collectionRuleVersion"),
            data => data["collectionRuleVersion"] = 0,
            data => data["collectionRuleVersion"] = 2,
            data => data["collectionRuleVersion"] = "1",
            data => data["collectionRuleVersion"] = 1.5,
            data => data.Remove("legacyCollectionQuotaExempt"),
            data => data["legacyCollectionQuotaExempt"] = "true",
            data => data.Remove("collectedFragmentIds"),
            data => data["collectedFragmentIds"] = null,
            data => data["collectedFragmentIds"] = new JsonArray((JsonNode)null),
            data => data["collectedFragmentIds"] = new JsonArray("canyon.v2.fragment.foreign"),
            data => data["collectedFragmentIds"] = new JsonArray("canyon.v2.discovery.archive-side-cave")
        })
        {
            var data = validCurrent.DeepClone().AsObject();
            corrupt(data);
            Write(data);
            TestAssert.True(ReadUnchanged().WasRecovered, "current schema requires explicit valid collection wire metadata");
        }
        foreach (Action<PrologueSaveData> corrupt in new Action<PrologueSaveData>[]
        {
            data => data.CollectionRuleVersion = 0,
            data => data.CollectedFragmentIds = null,
            data => data.CollectedFragmentIds.Add(null),
            data => data.CollectedFragmentIds.Add("supply_cave")
        })
        {
            var data = PrologueSaveData.NewLayoutV2();
            corrupt(data);
            bool refused = false;
            try { data.Normalize(); } catch (InvalidDataException) { refused = true; }
            TestAssert.True(refused, "Normalize refuses malformed collection metadata, including non-fragment IDs");
        }

        string legacy = Path.Combine(root, "legacy-schema2.json");
        string unusedNew = Path.Combine(root, "absent-new-slot.json");
        foreach ((string sector, bool activated, bool exempt) in new[]
            { ("CanyonBasin", true, false), ("LostTemple", true, true), ("LostTemple", false, false) })
        {
            File.WriteAllText(legacy, $$"""
                {"version":2,"hasActivatedCheckpoint":{{activated.ToString().ToLowerInvariant()}},"checkpointSector":"{{sector}}",
                 "checkpointWorldX":-999,"checkpointWorldY":1,"shortcutOpen":false}
                """);
            string before = Hash(legacy);
            var loaded = PrologueSaveStore.LoadFullWorld(unusedNew, legacy);
            TestAssert.True(loaded.Status == PrologueSaveLoadStatus.MigratableLegacy &&
                loaded.Data.Version == 4 && loaded.Data.LegacyCollectionQuotaExempt == exempt &&
                loaded.Data.CollectedFragmentIds.Count == 0 && !File.Exists(unusedNew),
                "schema2 retains explicit legal sector mapping/choice and no synthesized fragments");
            TestAssert.Equal(before, Hash(legacy), "schema2 original bytes remain unchanged");
        }
        foreach (string oldWire in new[]
        {
            "{\"version\":2,\"checkpointSector\":\"LostTemple\"}",
            "{\"version\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"CanyonBasin\",\"bossDefeated\":true}",
            "{\"version\":2,\"checkpointSector\":\"CanyonEntrance\",\"completed\":true}"
        })
        {
            File.WriteAllText(legacy, oldWire);
            string before = Hash(legacy);
            var loaded = PrologueSaveStore.LoadFullWorld(unusedNew, legacy);
            bool warOver = oldWire.Contains("bossDefeated", StringComparison.Ordinal) || oldWire.Contains("completed", StringComparison.Ordinal);
            TestAssert.True(loaded.Status == PrologueSaveLoadStatus.MigratableLegacy && !loaded.Data.WasRecovered &&
                loaded.Data.LegacyCollectionQuotaExempt == warOver &&
                loaded.Data.BossDefeated == oldWire.Contains("bossDefeated", StringComparison.Ordinal) &&
                loaded.Data.Completed == oldWire.Contains("completed", StringComparison.Ordinal) && !File.Exists(unusedNew),
                "schema2 absent activation cannot grant entitlement; independent real old war-over flags can");
            TestAssert.Equal(before, Hash(legacy), "schema2 entitlement read preserves its source");
        }
        File.WriteAllText(legacy, "{\"version\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"CanyonAscent\"}");
        string invalidHash = Hash(legacy);
        var invalidMapping = PrologueSaveStore.LoadFullWorld(unusedNew, legacy);
        TestAssert.True(invalidMapping.Status == PrologueSaveLoadStatus.NeedsNewLayoutChoice &&
            !invalidMapping.Data.LegacyCollectionQuotaExempt && !File.Exists(unusedNew), "invalid old mapping keeps the explicit choice boundary");
        TestAssert.Equal(invalidHash, Hash(legacy), "invalid old mapping does not rewrite source");
        foreach (int oldLayout in new[] { 0, 1 })
        foreach (string sector in new[] { "CanyonBasin", "LostTemple" })
        {
            File.WriteAllText(legacy, $$"""
                {"version":2,"layoutVersion":{{oldLayout}},"hasActivatedCheckpoint":true,
                 "checkpointSector":"{{sector}}","checkpointWorldX":5040.5,"checkpointWorldY":1294.25,
                 "shortcutOpen":true,"bossDefeated":true,"completed":false,"objective":"DefeatBellkeeper"}
                """);
            string before = Hash(legacy);
            var generic = PrologueSaveStore.LoadOrNew(legacy);
            TestAssert.True(!generic.WasRecovered && generic.Version == 4 && generic.LayoutVersion == oldLayout &&
                generic.HasActivatedCheckpoint && generic.CheckpointSector == sector &&
                generic.CheckpointWorldPosition == new Vector2(5040.5f, 1294.25f) &&
                generic.CheckpointId == "canyon.v2.start" && generic.ShortcutOpen &&
                generic.OpenShortcuts.Count == 0 && generic.BossDefeated && !generic.Completed,
                $"generic schema2 layout{oldLayout} retains old sector/coordinates instead of applying the layout2 map: {sector}");
            TestAssert.True(generic.CollectionRuleVersion == 1 && !generic.LegacyCollectionQuotaExempt &&
                generic.CollectedFragmentIds.Count == 0,
                "generic old layouts cannot gain full-world quota authority even from a real old Boss-defeat flag");
            TestAssert.True(PrologueSaveStore.LoadOrNew(legacy, 2).WasRecovered,
                "preserving a generic layout0/1 save does not allow it through the full-world layout2 boundary");
            TestAssert.Equal(before, Hash(legacy), "generic schema2 layout0/1 inspection preserves original bytes");
        }
        foreach ((string sector, bool activated, string rawId) in new[]
        {
            ("CanyonBasin", true, "canyon.v2.temple-forecourt"),
            ("LostTemple", false, "canyon.v2.temple-forecourt"),
            ("LostTemple", true, "canyon.v2.start")
        })
        {
            File.WriteAllText(legacy, $$"""
                {"version":2,"layoutVersion":2,"checkpointSector":"{{sector}}",
                 "hasActivatedCheckpoint":{{activated.ToString().ToLowerInvariant()}},"checkpointId":"{{rawId}}"}
                """);
            string before = Hash(legacy);
            var mapped = PrologueSaveStore.LoadFullWorld(unusedNew, legacy).Data;
            var generic = PrologueSaveStore.LoadOrNew(legacy);
            TestAssert.True(!generic.WasRecovered && generic.CheckpointId == mapped.CheckpointId &&
                generic.CheckpointWorldPosition == mapped.CheckpointWorldPosition &&
                generic.LegacyCollectionQuotaExempt == mapped.LegacyCollectionQuotaExempt,
                "generic schema2 layout2 must use the same legal sector mapping before quota entitlement, never injected stable-ID authority");
            TestAssert.Equal(before, Hash(legacy), "generic legacy mapping is also read-only");
        }
        foreach (string oldWire in new[]
        {
            "{\"version\":2,\"layoutVersion\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"CanyonAscent\"}",
            "{\"version\":2,\"layoutVersion\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"LostTemple\",\"checkpointId\":\"foreign\"}"
        })
        {
            File.WriteAllText(legacy, oldWire);
            string before = Hash(legacy);
            TestAssert.True(PrologueSaveStore.LoadOrNew(legacy).WasRecovered,
                "generic layout2 legacy data cannot silently repair illegal sector/ID mapping");
            TestAssert.Equal(before, Hash(legacy), "rejected generic legacy mapping preserves source");
        }
        GD.Print("QUEST_FRAGMENT_SAVE_DATA_OK");
    }

    private static JsonObject OldThree(string checkpoint, bool activated) => new()
    {
        ["version"] = 3, ["layoutVersion"] = 2, ["checkpointId"] = checkpoint,
        ["hasActivatedCheckpoint"] = activated, ["checkpointSector"] = "LostTemple",
        ["checkpointWorldX"] = 0, ["checkpointWorldY"] = 0,
        ["openShortcuts"] = new JsonArray(), ["activatedMechanisms"] = new JsonArray(),
        ["discoveries"] = new JsonArray(), ["objective"] = "Wake",
        ["bossDefeated"] = false, ["completed"] = false
    };
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
