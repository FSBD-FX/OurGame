using Knight.Abilities;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Settings;

namespace Knight.Prologue.Guidance;

public readonly record struct PrologueGuidanceContext(
    PrologueObjective Objective,
    PrologueSectorId Sector,
    PrologueBossGuidanceState BossState,
    int SameSectorFailureCount,
    GameLanguage Language,
    bool GuidanceEnabled,
    MechanicalClawState ClawState = MechanicalClawState.Idle,
    bool FirstCombatNearby = false,
    bool DescentClawOpportunity = false,
    bool TerrainClawAttached = false,
    BossAdmissionResult? BossAdmission = null,
    bool AscentTransferOpportunity = false,
    bool ShieldLaneNearby = false);
