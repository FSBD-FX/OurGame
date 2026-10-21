using Knight.Prologue;
using Knight.Save;

namespace Knight.Tests;

/// <summary>
/// Explicit metadata prerequisites for existing visual/exit lifecycle fixtures.
/// Seeding here is NOT a collection, admission, or natural-route proof.
/// Admission assertions keep their separate independent literal expectations.
/// </summary>
internal static class BossAdmissionFixturePrerequisites
{
    private static readonly string[] Fragments =
    [
        "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
        "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.gear-gallery",
        "canyon.v2.fragment.ascent-mid", "canyon.v2.fragment.courtyard"
    ];

    internal static void Apply(PrologueProgress progress)
    {
        foreach (string id in Fragments) progress.TryCollectFragment(id);
        progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
        progress.OpenBellChainShortcut();
    }

    internal static void Apply(PrologueSaveData saved)
    {
        saved.CollectedFragmentIds.AddRange(Fragments);
        saved.ActivatedMechanisms.Add("canyon.v2.courtyard-seal-open");
        saved.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
    }
}
