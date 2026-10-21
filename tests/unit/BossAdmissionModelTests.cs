using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Save;

namespace Knight.Tests.Unit;

public static class BossAdmissionModelTests
{
    private static readonly string[] Fragments =
    [
        "canyon.v2.fragment.entrance",
        "canyon.v2.fragment.rift",
        "canyon.v2.fragment.abyss-floor",
        "canyon.v2.fragment.gear-gallery",
        "canyon.v2.fragment.ascent-mid",
        "canyon.v2.fragment.courtyard",
        "canyon.v2.fragment.archive",
        "canyon.v2.fragment.steam-loop",
        "canyon.v2.fragment.temple-upper"
    ];

    public static void Run()
    {
        FragmentBoundariesHaveIndependentLiteralExpectations();
        DuplicateAndNonFragmentRecordsCannotCompleteFive();
        DiscoveryAndSupplyRecordsCannotReplaceFragments();
        CourtyardAndBellAreBothRequiredAfterQuota();
        CrankAndEncounterNamesDoNotReplaceCompletedSeal();
        PersistedLegacyExemptionOnlySkipsTheFragmentQuota();
        ForecourtCheckpointDoesNotInferAnExemption();
        DefeatedStateIsRestoredByCallerNotUsedAsAnAdmissionInput();
        MissingRuntimeProgressFailsClosed();
    }

    // Catches zero/five admission, an off-by-one threshold, or a negative shortfall at nine.
    private static void FragmentBoundariesHaveIndependentLiteralExpectations()
    {
        AssertResult(new PrologueProgress(), false, 6, false, false, "zero fragments");
        AssertResult(ProgressWith(5), false, 1, false, false, "five fragments");
        AssertResult(ProgressWith(6, true, true), true, 0, true, true, "six fragments");
        AssertResult(ProgressWith(9, true, true), true, 0, true, true, "nine fragments");
    }

    // Catches counting duplicate attempts, area discoveries, rewards, or foreign fragment IDs.
    private static void DuplicateAndNonFragmentRecordsCannotCompleteFive()
    {
        PrologueProgress progress = ProgressWith(5, true, true);
        progress.TryCollectFragment("canyon.v2.fragment.entrance");
        progress.TryCollectFragment("canyon.v2.fragment.entrance");
        progress.TryCollectFragment("canyon.v2.discovery.archive-side-cave");
        progress.TryCollectFragment("canyon.v2.reward.archive-supply");
        progress.TryCollectFragment("canyon.v2.fragment.unregistered");
        progress.Discover("canyon.v2.discovery.archive-side-cave");
        progress.Discover("canyon.v2.reward.archive-supply");
        AssertResult(progress, false, 1, true, true, "five real fragments cannot be padded");
    }

    // Catches replacing the fragment set with Discoveries.Count or treating supply as a fragment.
    private static void DiscoveryAndSupplyRecordsCannotReplaceFragments()
    {
        PrologueProgress progress = ProgressWith(0, true, true);
        foreach (string id in new[]
        {
            "canyon.v2.discovery.archive-side-cave", "canyon.v2.discovery.frozen-detour",
            "canyon.v2.reward.archive-supply", "canyon.v2.reward.temple-supply",
            "hidden_alcove", "supply_cave"
        }) progress.Discover(id);
        AssertResult(progress, false, 6, true, true, "six discovery and supply records are not fragments");
    }

    // Catches OR instead of AND, or readiness diagnostics taken from quota rather than exact IDs.
    private static void CourtyardAndBellAreBothRequiredAfterQuota()
    {
        AssertResult(ProgressWith(6), false, 0, false, false, "neither existing prerequisite");
        AssertResult(ProgressWith(6, true, false), false, 0, true, false, "courtyard only");
        AssertResult(ProgressWith(6, false, true), false, 0, false, true, "bell only");
        AssertResult(ProgressWith(6, true, true), true, 0, true, true, "both existing prerequisites");
    }

    // Catches checking prerequisite labels instead of the durable completed-seal ID.
    private static void CrankAndEncounterNamesDoNotReplaceCompletedSeal()
    {
        PrologueProgress progress = ProgressWith(6, false, true);
        progress.ActivateMechanism("canyon.v2.courtyard-crank");
        progress.ActivateMechanism("canyon.v2.encounter.split-level");
        AssertResult(progress, false, 0, false, true, "seal completion must be committed by its mechanism");
        progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
        AssertResult(progress, true, 0, true, true, "exact completed-seal ID admits after bell and quota");
    }

    // Catches an exemption bypassing courtyard/bell or fabricating collected quantity.
    private static void PersistedLegacyExemptionOnlySkipsTheFragmentQuota()
    {
        PrologueProgress progress = RestoreEligibleLegacySnapshot();
        AssertResult(progress, false, 0, false, false, "legacy exemption with neither prerequisite");
        progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
        AssertResult(progress, false, 0, true, false, "legacy exemption still needs the bell");
        progress = RestoreEligibleLegacySnapshot();
        progress.OpenBellChainShortcut();
        AssertResult(progress, false, 0, false, true, "legacy exemption still needs the courtyard");
        progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
        AssertResult(progress, true, 0, true, true, "legitimate persisted exemption skips only quantity");
        TestAssert.True(progress.LegacyCollectionQuotaExempt,
            "admission retains the legitimate persisted quota exemption");
        TestAssert.Equal(0, progress.CollectedFragmentIds.Count,
            "admission never manufactures or consumes legacy collection records");
    }

    // Catches granting exemption merely from a current save's checkpoint location.
    private static void ForecourtCheckpointDoesNotInferAnExemption()
    {
        PrologueProgress progress = ProgressWith(0, true, true);
        progress.ActivateCheckpoint("canyon.v2.temple-forecourt");
        AssertResult(progress, false, 6, true, true, "current forecourt checkpoint has no implicit exemption");
    }

    // Catches a model-level BossDefeated shortcut; the caller owns restored open doors.
    private static void DefeatedStateIsRestoredByCallerNotUsedAsAnAdmissionInput()
    {
        var progress = new PrologueProgress();
        progress.MarkBossDefeated();
        AssertResult(progress, false, 6, false, false, "defeated history does not fabricate pre-fight inputs");
        progress = ProgressWith(6, true, true);
        progress.MarkBossDefeated();
        AssertResult(progress, true, 0, true, true, "completed state does not erase valid pre-fight inputs");
    }

    // Catches an unbound controller receiving an accidental admission.
    private static void MissingRuntimeProgressFailsClosed() =>
        AssertResult(null, false, 6, false, false, "unbound progress");

    private static PrologueProgress ProgressWith(int count, bool courtyard = false, bool bell = false)
    {
        var progress = new PrologueProgress();
        for (int index = 0; index < count; index++) progress.TryCollectFragment(Fragments[index]);
        if (courtyard) progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
        if (bell) progress.OpenBellChainShortcut();
        return progress;
    }

    private static PrologueProgress RestoreEligibleLegacySnapshot()
    {
        // Represents the schema4 snapshot retained after a legitimate schema3
        // activated-forecourt migration. Migration eligibility itself is tested
        // by QuestFragmentSaveTests; admission consumes this trusted metadata.
        PrologueSaveData saved = PrologueSaveData.NewLayoutV2();
        saved.CheckpointId = "canyon.v2.temple-forecourt";
        saved.HasActivatedCheckpoint = true;
        saved.LegacyCollectionQuotaExempt = true;
        var progress = new PrologueProgress();
        progress.Restore(saved.Normalize());
        return progress;
    }

    private static void AssertResult(PrologueProgress progress, bool allowed, int missing,
        bool courtyard, bool bell, string context)
    {
        BossAdmissionResult actual = BossAdmissionModel.Evaluate(progress);
        TestAssert.Equal(missing, actual.MissingFragments, $"{context}: effective missing fragment count");
        TestAssert.Equal(courtyard, actual.CourtyardReady, $"{context}: courtyard readiness");
        TestAssert.Equal(bell, actual.BellChainReady, $"{context}: bell-chain readiness");
        TestAssert.Equal(allowed, actual.Allowed, $"{context}: central admission decision");
    }
}
