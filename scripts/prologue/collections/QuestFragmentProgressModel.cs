using System;
using System.Collections.Generic;
using System.Linq;

namespace Knight.Prologue.Collections;

public static class QuestFragmentProgressModel
{
    public static int CountCollected(IEnumerable<string> ids) =>
        (ids ?? []).Where(QuestFragmentCatalog.IsKnown).Distinct(StringComparer.Ordinal).Count();

    public static bool HasQuota(IEnumerable<string> ids, bool legacyExempt) =>
        legacyExempt || CountCollected(ids) >= QuestFragmentCatalog.RequiredCount;
}
