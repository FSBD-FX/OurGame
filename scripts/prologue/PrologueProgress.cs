using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Prologue;

public sealed class PrologueProgress
{
    private readonly HashSet<StringName> _completedTutorials = new();
    private readonly HashSet<string> _discoveries = new(StringComparer.Ordinal);
    private Vector2 _startPosition;
    private PrologueSectorId _checkpointSector = PrologueSectorId.CanyonEntrance;
    private Vector2 _checkpointPosition;
    private bool _checkpointActivated;

    public PrologueProgress() : this(Vector2.Zero) { }

    public PrologueProgress(Vector2 startPosition)
    {
        if (!Finite(startPosition)) throw new ArgumentOutOfRangeException(nameof(startPosition));
        _startPosition = startPosition;
        _checkpointPosition = startPosition;
    }

    public bool BellChainShortcutOpen { get; private set; }
    public bool HiddenAlcoveVisited => _discoveries.Contains("hidden_alcove") ||
                                      _discoveries.Contains("supply_cave");
    public bool BossDefeated { get; private set; }
    public IReadOnlySet<StringName> CompletedTutorials => _completedTutorials;
    public IReadOnlySet<string> Discoveries => _discoveries;

    public void SetStartPosition(Vector2 startPosition)
    {
        if (!Finite(startPosition)) throw new ArgumentOutOfRangeException(nameof(startPosition));
        _startPosition = startPosition;
        if (!_checkpointActivated)
        {
            _checkpointSector = PrologueSectorId.CanyonEntrance;
            _checkpointPosition = startPosition;
        }
    }

    public bool ActivateCheckpoint(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        if (sectorId is not (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple) ||
            !Finite(worldPosition) ||
            _checkpointActivated && _checkpointSector == PrologueSectorId.LostTemple &&
            sectorId == PrologueSectorId.CanyonBasin)
            return false;
        if (_checkpointActivated && _checkpointSector == sectorId &&
            _checkpointPosition.DistanceSquaredTo(worldPosition) <= .0001f)
            return false;
        _checkpointActivated = true;
        _checkpointSector = sectorId;
        _checkpointPosition = worldPosition;
        return true;
    }

    public bool OpenBellChainShortcut()
    {
        if (BellChainShortcutOpen) return false;
        BellChainShortcutOpen = true;
        return true;
    }

    public bool VisitHiddenAlcove() => Discover("hidden_alcove");

    public bool Discover(string id) => !string.IsNullOrWhiteSpace(id) && _discoveries.Add(id);

    public bool CompleteTutorial(StringName id) => !Empty(id) && _completedTutorials.Add(id);

    public bool MarkBossDefeated()
    {
        if (BossDefeated) return false;
        BossDefeated = true;
        return true;
    }

    public PrologueCheckpointSnapshot RestoreCheckpoint() => new(
        _checkpointSector,
        _checkpointPosition,
        _checkpointActivated,
        BellChainShortcutOpen,
        new PrologueProgressSnapshot(BossDefeated,
            new HashSet<string>(_discoveries, StringComparer.Ordinal)));

    public void Reset()
    {
        _checkpointSector = PrologueSectorId.CanyonEntrance;
        _checkpointPosition = _startPosition;
        _checkpointActivated = false;
        BellChainShortcutOpen = false;
        BossDefeated = false;
        _completedTutorials.Clear();
        _discoveries.Clear();
    }

    public void Restore(PrologueSaveData data)
    {
        Reset();
        PrologueSaveData normalized = (data ?? new PrologueSaveData()).Normalize();
        if (normalized.HasActivatedCheckpoint)
        {
            _checkpointSector = Enum.Parse<PrologueSectorId>(normalized.CheckpointSector);
            _checkpointPosition = normalized.CheckpointWorldPosition;
            _checkpointActivated = true;
        }
        BellChainShortcutOpen = normalized.ShortcutOpen;
        BossDefeated = normalized.BossDefeated;
        foreach (string discovery in normalized.Discoveries) _discoveries.Add(discovery);
        foreach (string tutorial in normalized.CompletedTutorials) _completedTutorials.Add(tutorial);
    }

    private static bool Empty(StringName value) => value == null || value.IsEmpty;
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
