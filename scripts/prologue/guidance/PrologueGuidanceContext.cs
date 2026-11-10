using Knight.Abilities;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Presentation.Fog;

namespace Knight.Prologue.Guidance;

public enum PrologueRecyclingLesson { None, Combination, CombinationLaunched, Springboard, FixedGear }

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
    bool ShieldLaneNearby = false,
    PrologueRecyclingLesson RecyclingLesson = PrologueRecyclingLesson.None,
    VisibleLaserWarning VisibleLaser = VisibleLaserWarning.None,
    bool ThrusterOpportunity = false);
