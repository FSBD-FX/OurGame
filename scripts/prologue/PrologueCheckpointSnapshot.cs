using System.Collections.Generic;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Prologue;

public readonly record struct PrologueProgressSnapshot(
    bool BossDefeated,
    IReadOnlySet<string> Discoveries,
    IReadOnlySet<string> CollectedFragmentIds = null,
    int CollectionRuleVersion = 1,
    bool LegacyCollectionQuotaExempt = false);

public readonly record struct PrologueCheckpointSnapshot(
    PrologueSectorId SectorId,
    Vector2 WorldPosition,
    bool HasActivatedCheckpoint,
    bool ShortcutOpen,
    PrologueProgressSnapshot Progress,
    string CheckpointId = "canyon.v2.start");
