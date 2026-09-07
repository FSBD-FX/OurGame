using Knight.Prologue.Seamless;
using Knight.Settings;

namespace Knight.Prologue.Guidance;

public readonly record struct PrologueGuidanceContext(
    PrologueObjective Objective,
    PrologueSectorId Sector,
    PrologueBossGuidanceState BossState,
    int SameSectorFailureCount,
    GameLanguage Language,
    bool GuidanceEnabled);
