using System;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Collections;

public readonly record struct BossAdmissionResult(
    bool Allowed, int MissingFragments, bool CourtyardReady, bool BellChainReady);

/// <summary>
/// Evaluates pre-fight entry requirements. Callers restore a defeated encounter
/// before consulting admission; BossDefeated is not a replacement for these inputs.
/// MissingFragments reports the effective unmet admission quantity, so a legitimate
/// legacy quota exemption returns zero without changing actual collected records.
/// </summary>
public static class BossAdmissionModel
{
    public static BossAdmissionResult Evaluate(PrologueProgress progress)
    {
        if (progress == null)
            return new(false, QuestFragmentCatalog.RequiredCount, false, false);

        int missing = progress.LegacyCollectionQuotaExempt ? 0 : Math.Max(0,
            QuestFragmentCatalog.RequiredCount -
            QuestFragmentProgressModel.CountCollected(progress.CollectedFragmentIds));
        bool courtyard = progress.ActivatedMechanisms.Contains(CanyonWorldLayout.CourtyardSealMechanismId);
        bool bell = progress.BellChainShortcutOpen;
        return new(missing == 0 && courtyard && bell, missing, courtyard, bell);
    }
}
