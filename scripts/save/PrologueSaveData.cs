using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Save;

public sealed class PrologueSaveData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public bool HasActivatedCheckpoint { get; set; }
    public string CheckpointSector { get; set; } = nameof(PrologueSectorId.CanyonEntrance);
    public float CheckpointWorldX { get; set; }
    public float CheckpointWorldY { get; set; }
    public bool ShortcutOpen { get; set; }
    public List<string> Discoveries { get; set; } = [];
    public bool BossDefeated { get; set; }
    public bool Completed { get; set; }
    public string Objective { get; set; } = nameof(PrologueObjective.Wake);
    public List<string> CompletedTutorials { get; set; } = [];
    public double BestHumanCompletionSeconds { get; set; } = -1d;
    public int BestHumanDeaths { get; set; } = -1;

    [JsonIgnore]
    public Vector2 CheckpointWorldPosition => new(CheckpointWorldX, CheckpointWorldY);

    [JsonIgnore]
    public bool WasRecovered { get; set; }

    public PrologueSaveData Normalize()
    {
        if (Version != CurrentVersion)
            throw new InvalidDataException($"Unsupported prologue save version {Version}.");

        PrologueSectorId sector = PrologueSectorId.CanyonEntrance;
        Vector2 position = Vector2.Zero;
        if (!float.IsFinite(CheckpointWorldX) || !float.IsFinite(CheckpointWorldY))
            throw new InvalidDataException("Checkpoint world coordinates must be finite before canonicalization.");
        if (HasActivatedCheckpoint)
        {
            if (!Enum.TryParse(CheckpointSector, ignoreCase: false, out sector) ||
                sector is not (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple))
                throw new InvalidDataException("Activated checkpoint sector must be CanyonBasin or LostTemple.");
            position = CheckpointWorldPosition;
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
                throw new InvalidDataException("Activated checkpoint world position must be finite.");
        }

        if (!Enum.TryParse(Objective, ignoreCase: false, out PrologueObjective objective) ||
            !Enum.IsDefined(objective))
            throw new InvalidDataException("Objective is invalid.");

        double bestSeconds = double.IsFinite(BestHumanCompletionSeconds) &&
                             BestHumanCompletionSeconds > 0d
            ? BestHumanCompletionSeconds
            : -1d;
        int bestDeaths = bestSeconds > 0d && BestHumanDeaths >= 0 ? BestHumanDeaths : -1;
        return new PrologueSaveData
        {
            Version = CurrentVersion,
            HasActivatedCheckpoint = HasActivatedCheckpoint,
            CheckpointSector = sector.ToString(),
            CheckpointWorldX = position.X,
            CheckpointWorldY = position.Y,
            ShortcutOpen = ShortcutOpen,
            Discoveries = NormalizeStrings(Discoveries),
            BossDefeated = BossDefeated,
            Completed = Completed,
            Objective = objective.ToString(),
            CompletedTutorials = NormalizeStrings(CompletedTutorials),
            BestHumanCompletionSeconds = bestSeconds,
            BestHumanDeaths = bestDeaths,
            WasRecovered = WasRecovered
        };
    }

    public bool RecordCompletion(string evidenceSource, double seconds, int deaths)
    {
        Completed = true;
        Objective = nameof(PrologueObjective.Complete);
        if (!string.Equals(evidenceSource, "HandsOnLauncher", StringComparison.Ordinal) ||
            !double.IsFinite(seconds) || seconds <= 0d || deaths < 0)
            return false;

        if (BestHumanCompletionSeconds < 0d || seconds < BestHumanCompletionSeconds)
        {
            BestHumanCompletionSeconds = seconds;
            BestHumanDeaths = deaths;
        }
        return true;
    }

    internal static PrologueSaveData MigrateV1(bool completed, double bestSeconds, int bestDeaths) =>
        new PrologueSaveData
        {
            Completed = completed,
            Objective = completed ? nameof(PrologueObjective.Complete) : nameof(PrologueObjective.Wake),
            BestHumanCompletionSeconds = double.IsFinite(bestSeconds) && bestSeconds > 0d
                ? bestSeconds : -1d,
            BestHumanDeaths = double.IsFinite(bestSeconds) && bestSeconds > 0d && bestDeaths >= 0
                ? bestDeaths : -1
        }.Normalize();

    private static List<string> NormalizeStrings(IEnumerable<string> values) => (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToList();

}
