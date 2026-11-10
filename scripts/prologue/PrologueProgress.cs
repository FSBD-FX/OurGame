using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;
using Knight.Save;

namespace Knight.Prologue;

public sealed class PrologueProgress
{
    private readonly HashSet<StringName> _completedTutorials = new();
    private readonly HashSet<string> _discoveries = new(StringComparer.Ordinal);
    private readonly HashSet<string> _openShortcuts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activatedMechanisms = new(StringComparer.Ordinal);
    private readonly HashSet<string> _collectedFragmentIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _claimedRewardIds = new(StringComparer.Ordinal);
    private Vector2 _startPosition;
    private PrologueSectorId _checkpointSector = PrologueSectorId.CanyonEntrance;
    private Vector2 _checkpointPosition;
    private bool _checkpointActivated;
    private string _checkpointId = "canyon.v2.start";

    public PrologueProgress() : this(Vector2.Zero) { }
    private PrologueWorldProfile _profile;
    internal void ConfigureProfile(PrologueWorldProfile profile)
    {
        if(ReferenceEquals(_profile,profile))return;
        if(_profile!=null || _checkpointActivated || _claimedRewardIds.Count!=0 || _collectedFragmentIds.Count!=0 ||
            _discoveries.Count!=0 || _completedTutorials.Count!=0 || _openShortcuts.Count!=0 || _activatedMechanisms.Count!=0 || BossDefeated)
            throw new InvalidOperationException("World profile is selected only before progress begins.");
        _profile=profile;
        if(profile!=null)_startPosition=profile.Start.WorldPosition;
        Reset();
    }

    public PrologueProgress(Vector2 startPosition)
    {
        if (!Finite(startPosition)) throw new ArgumentOutOfRangeException(nameof(startPosition));
        _startPosition = startPosition;
        _checkpointPosition = startPosition;
    }

    public IReadOnlySet<string> OpenShortcuts => _openShortcuts;
    public IReadOnlySet<string> ActivatedMechanisms => _activatedMechanisms;
    public bool BellChainShortcutOpen => IsShortcutOpen("canyon.v2.temple-bell-gate");
    public bool HiddenAlcoveVisited => _discoveries.Contains("hidden_alcove") ||
                                      _discoveries.Contains("supply_cave");
    public bool BossDefeated { get; private set; }
    public IReadOnlySet<StringName> CompletedTutorials => _completedTutorials;
    public IReadOnlySet<string> Discoveries => _discoveries;
    public IReadOnlySet<string> CollectedFragmentIds => _collectedFragmentIds;
    public IReadOnlySet<string> ClaimedRewardIds => _claimedRewardIds;
    // Only the flow's durable-first transaction calls this for live gameplay.
    public bool TryClaimResourceReward(string stableId) =>
        ResourceRewardCatalog.TryGet(stableId,out _) && _claimedRewardIds.Add(stableId);
    public int ResourceTotal(string resource) => ResourceRewardCatalog.Total(_claimedRewardIds,resource);
    public int CollectionRuleVersion { get; private set; } = 1;
    public bool LegacyCollectionQuotaExempt { get; private set; }

    // The flow commits a fragment here only after its candidate save succeeds.
    public bool TryCollectFragment(string stableId) =>
        QuestFragmentCatalog.IsKnown(stableId) && _collectedFragmentIds.Add(stableId);

    public PrologueSaveData ToSave(PrologueSaveData basis)
    {
        if (basis == null) throw new ArgumentNullException(nameof(basis));
        var candidate = basis.Normalize(_profile);
        PrologueCheckpointSnapshot snapshot = RestoreCheckpoint();
        candidate.HasActivatedCheckpoint = snapshot.HasActivatedCheckpoint;
        candidate.CheckpointId = snapshot.CheckpointId;
        candidate.CheckpointSector = snapshot.SectorId.ToString();
        candidate.CheckpointWorldX = snapshot.WorldPosition.X;
        candidate.CheckpointWorldY = snapshot.WorldPosition.Y;
        candidate.ShortcutOpen = snapshot.ShortcutOpen;
        candidate.OpenShortcuts = _openShortcuts.ToList();
        candidate.ActivatedMechanisms = _activatedMechanisms.ToList();
        candidate.Discoveries = _discoveries.ToList();
        candidate.CompletedTutorials = _completedTutorials.Select(id => id.ToString()).ToList();
        candidate.BossDefeated = BossDefeated;
        candidate.CollectionRuleVersion = CollectionRuleVersion;
        candidate.LegacyCollectionQuotaExempt = LegacyCollectionQuotaExempt;
        candidate.CollectedFragmentIds = _collectedFragmentIds.ToList();
        candidate.ClaimedRewardIds = _claimedRewardIds.ToList();
        return candidate.Normalize(_profile);
    }

    public void SetStartPosition(Vector2 startPosition)
    {
        if (!Finite(startPosition)) throw new ArgumentOutOfRangeException(nameof(startPosition));
        if(_profile!=null && startPosition!=_profile.Start.WorldPosition)throw new ArgumentException("Profile start position is canonical.");
        _startPosition = startPosition;
        if (!_checkpointActivated)
        {
            _checkpointSector = PrologueSectorId.CanyonEntrance;
            _checkpointPosition = startPosition;
        }
    }

    public bool ActivateCheckpoint(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        if(_profile!=null)
        {
            foreach(var checkpoint in _profile.Checkpoints)
                if(checkpoint.SectorId==sectorId && checkpoint.WorldPosition==worldPosition)return ActivateCheckpoint(checkpoint.StableId);
            return false;
        }
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

    public bool ActivateCheckpoint(string stableId)
    {
        if(_profile!=null)
        {
            if(!_profile.CanAdvanceCheckpoint(_checkpointId,stableId) ||
                !_profile.TryResolveCheckpoint(stableId,out var profileSector,out var profilePosition))return false;
            _checkpointId=stableId;_checkpointSector=profileSector;_checkpointPosition=profilePosition;_checkpointActivated=true;return true;
        }
        if (!CanyonWorldLayout.TryResolveCheckpoint(stableId, out var sector, out var position) ||
            !ActivateCheckpoint(sector, position)) return false;
        _checkpointId = stableId;
        return true;
    }

    public bool OpenBellChainShortcut()
    {
        return OpenShortcut("canyon.v2.temple-bell-gate");
    }

    public bool OpenShortcut(string stableId) =>
        !string.IsNullOrWhiteSpace(stableId) && _openShortcuts.Add(stableId);

    public bool ActivateMechanism(string stableId) =>
        !string.IsNullOrWhiteSpace(stableId) && _activatedMechanisms.Add(stableId);

    public bool IsShortcutOpen(string stableId) =>
        !string.IsNullOrWhiteSpace(stableId) && _openShortcuts.Contains(stableId);

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
            new HashSet<string>(_discoveries, StringComparer.Ordinal),
            new HashSet<string>(_collectedFragmentIds, StringComparer.Ordinal),
            CollectionRuleVersion, LegacyCollectionQuotaExempt), _checkpointId);

    public void Reset()
    {
        _checkpointSector = _profile?.Start.SectorId ?? PrologueSectorId.CanyonEntrance;
        _checkpointPosition = _startPosition;
        _checkpointActivated = false;
        _checkpointId = _profile?.Start.StableId ?? "canyon.v2.start";
        _openShortcuts.Clear();
        _activatedMechanisms.Clear();
        _collectedFragmentIds.Clear();
        _claimedRewardIds.Clear();
        CollectionRuleVersion = 1;
        LegacyCollectionQuotaExempt = false;
        BossDefeated = false;
        _completedTutorials.Clear();
        _discoveries.Clear();
    }

    public void Restore(PrologueSaveData data)
    {
        PrologueSaveData normalized = (data ?? (_profile==null?new PrologueSaveData():PrologueSaveData.NewForProfile(_profile))).Normalize(_profile);
        Reset();
        CollectionRuleVersion = normalized.CollectionRuleVersion;
        LegacyCollectionQuotaExempt = normalized.LegacyCollectionQuotaExempt;
        foreach (string id in normalized.CollectedFragmentIds) _collectedFragmentIds.Add(id);
        foreach (string id in normalized.ClaimedRewardIds) _claimedRewardIds.Add(id);
        if (_profile!=null || normalized.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
        {
            _checkpointId = normalized.CheckpointId;
            _checkpointSector = Enum.Parse<PrologueSectorId>(normalized.CheckpointSector);
            _checkpointPosition = normalized.CheckpointWorldPosition;
        }
        if (normalized.HasActivatedCheckpoint)
        {
            _checkpointSector = Enum.Parse<PrologueSectorId>(normalized.CheckpointSector);
            _checkpointPosition = normalized.CheckpointWorldPosition;
            _checkpointActivated = true;
        }
        if (_profile!=null || normalized.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
        {
            foreach (string id in normalized.OpenShortcuts) _openShortcuts.Add(id);
            foreach (string id in normalized.ActivatedMechanisms) _activatedMechanisms.Add(id);
        }
        else if (normalized.ShortcutOpen) _openShortcuts.Add("canyon.v2.temple-bell-gate");
        BossDefeated = normalized.BossDefeated;
        foreach (string discovery in normalized.Discoveries) _discoveries.Add(discovery);
        foreach (string tutorial in normalized.CompletedTutorials) _completedTutorials.Add(tutorial);
    }

    private static bool Empty(StringName value) => value == null || value.IsEmpty;
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
