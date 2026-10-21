using System.Linq;
using System.IO;
using Knight.Prologue;
using Knight.Save;

namespace Knight.Tests.Unit;

public static class QuestFragmentStateTests
{
    public static void Run()
    {
        var progress = new PrologueProgress();
        TestAssert.True(progress.CollectionRuleVersion == 1 && !progress.LegacyCollectionQuotaExempt &&
            progress.CollectedFragmentIds.Count == 0, "fresh progress has explicit collection rule, no exemption and no fragments");
        TestAssert.True(progress.TryCollectFragment("canyon.v2.fragment.entrance"),
            "known fragment commits to independent authoritative progress");
        TestAssert.True(!progress.TryCollectFragment("canyon.v2.fragment.entrance") &&
            !progress.TryCollectFragment("supply_cave") && !progress.TryCollectFragment(null),
            "duplicate, invalid and null fragments cannot commit");
        progress.Discover("supply_cave");
        TestAssert.Equal(1, progress.CollectedFragmentIds.Count, "discovering a supply cache is never collecting a fragment");
        var snapshot = progress.RestoreCheckpoint();
        TestAssert.True(snapshot.Progress.CollectedFragmentIds.SetEquals(new[] { "canyon.v2.fragment.entrance" }) &&
            snapshot.Progress.CollectionRuleVersion == 1 && !snapshot.Progress.LegacyCollectionQuotaExempt,
            "death/checkpoint snapshot preserves independent collection metadata");
        progress.TryCollectFragment("canyon.v2.fragment.rift");
        TestAssert.Equal(1, snapshot.Progress.CollectedFragmentIds.Count,
            "snapshot collection is a detached copy, not a live set alias");

        var basis = PrologueSaveData.NewLayoutV2();
        basis.Objective = nameof(PrologueObjective.PullTempleMechanism);
        basis.Completed = true;
        basis.BestHumanCompletionSeconds = 640;
        basis.BestHumanDeaths = 2;
        progress.CompleteTutorial("grip_stone");
        progress.MarkBossDefeated();
        progress.ActivateCheckpoint("canyon.v2.basin-camp");
        progress.OpenBellChainShortcut();
        progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
        var candidate = progress.ToSave(basis);
        TestAssert.True(candidate.CollectedFragmentIds.Count == 2 && basis.CollectedFragmentIds.Count == 0 &&
            candidate.CollectionRuleVersion == 1 && !candidate.LegacyCollectionQuotaExempt,
            "save candidate copies collection state without mutating shared prior save");
        TestAssert.True(candidate.CheckpointId == "canyon.v2.basin-camp" && candidate.HasActivatedCheckpoint &&
            candidate.OpenShortcuts.Contains("canyon.v2.temple-bell-gate") &&
            candidate.ActivatedMechanisms.Contains("canyon.v2.courtyard-seal-open") &&
            candidate.Objective == basis.Objective && candidate.Discoveries.Contains("supply_cave") &&
            candidate.BossDefeated && candidate.Completed && candidate.CompletedTutorials.Contains("grip_stone") &&
            candidate.BestHumanCompletionSeconds == 640 && candidate.BestHumanDeaths == 2,
            "candidate retains existing progress fields and caller-owned objective semantics");
        candidate.CollectedFragmentIds.Clear();
        TestAssert.Equal(2, progress.CollectedFragmentIds.Count, "candidate set cannot mutate live progress");

        var oldEligible = PrologueSaveData.NewLayoutV2();
        oldEligible.LegacyCollectionQuotaExempt = true;
        oldEligible.CollectedFragmentIds = ["canyon.v2.fragment.archive"];
        progress.Restore(oldEligible);
        oldEligible.CollectedFragmentIds.Clear();
        TestAssert.True(progress.LegacyCollectionQuotaExempt && progress.CollectionRuleVersion == 1 &&
            progress.CollectedFragmentIds.SetEquals(new[] { "canyon.v2.fragment.archive" }),
            "Continue restores true collection metadata without retaining save collection aliases");
        var restoredSnapshot = progress.RestoreCheckpoint();
        TestAssert.True(restoredSnapshot.Progress.LegacyCollectionQuotaExempt &&
            restoredSnapshot.Progress.CollectedFragmentIds.Count == 1, "death snapshot does not drop migrated entitlement");
        TestAssert.True(progress.ToSave(PrologueSaveData.NewLayoutV2()).LegacyCollectionQuotaExempt,
            "later checkpoint persistence retains restored migration entitlement");
        progress.Reset();
        TestAssert.True(progress.CollectionRuleVersion == 1 && !progress.LegacyCollectionQuotaExempt &&
            progress.CollectedFragmentIds.Count == 0 && progress.Discoveries.Count == 0,
            "only new-game Reset clears collection progress and entitlement");
        progress.TryCollectFragment("canyon.v2.fragment.entrance");
        progress.OpenBellChainShortcut();
        var invalid = PrologueSaveData.NewLayoutV2();
        invalid.CollectionRuleVersion = 2;
        bool refused = false;
        try { progress.Restore(invalid); } catch (InvalidDataException) { refused = true; }
        TestAssert.True(refused && progress.CollectedFragmentIds.Count == 1 && progress.BellChainShortcutOpen,
            "invalid collection Restore refuses before clearing live fragment or mechanism progress");
    }
}
