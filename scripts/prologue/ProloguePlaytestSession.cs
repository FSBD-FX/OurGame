using System;
using System.Collections.Generic;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue.Seamless;

namespace Knight.Prologue;

public sealed class ProloguePlaytestSession
{
    private const double InputEdgeWindowSeconds = 1.5d;
    private static readonly HashSet<string> StableInputEdges = new(StringComparer.Ordinal)
    {
        "Q", "E", "LMB_DOWN", "LMB_UP", "RMB_DOWN", "RMB_UP", "SPACE", "SHIFT",
        "J", "K", "Q_INTERACT", "L_DOWN", "L_UP",
        "A_DOWN", "A_UP", "D_DOWN", "D_UP", "W_DOWN", "W_UP", "S_DOWN", "S_UP"
    };
    private readonly List<PrologueSectorId> _sectorsVisited = new();
    private readonly List<string> _checkpointsActivated = new();
    private readonly HashSet<string> _checkpointKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _grappleFailures = new(StringComparer.Ordinal);
    private readonly List<string> _modeSwitches = new();
    private readonly List<int> _selectedSlots = new();
    private readonly List<double> _bossReentrySeconds = new();
    private readonly List<PrologueRecoverySample> _recoveries = new();
    private readonly List<PrologueFallSample> _falls = new();
    private readonly List<PrologueRouteNodeSample> _routeNodes = new();
    private readonly List<PrologueEncounterSample> _encounters = new();
    private readonly List<PrologueMechanismSample> _mechanisms = new();
    private readonly List<PrologueShortcutTraversalSample> _shortcutTraversals = new();
    private readonly List<PrologueFireflyTargetSample> _fireflyTargets = new();
    private readonly List<InputEdgeEvent> _inputEdges = new();
    private readonly HashSet<string> _discoveries = new(StringComparer.Ordinal);
    private double? _lastBossDeathAt;
    private string _activeRecoveryReason;
    private bool _activeRecoveryInBossSector;
    private double _activeRecoveryStartedAt;

    public ProloguePlaytestSession(string runId, string startedAtUtc, string evidenceSource)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Playtest run ID is required.", nameof(runId));
        if (string.IsNullOrWhiteSpace(startedAtUtc)) throw new ArgumentException("Playtest start time is required.", nameof(startedAtUtc));
        if (string.IsNullOrWhiteSpace(evidenceSource)) throw new ArgumentException("Evidence source is required.", nameof(evidenceSource));
        RunId = runId;
        StartedAtUtc = startedAtUtc;
        EvidenceSource = evidenceSource;
    }

    public string RunId { get; }
    public string StartedAtUtc { get; }
    public string EvidenceSource { get; }
    public double ElapsedSeconds { get; private set; }
    public bool Completed { get; private set; }
    public bool FirstClearWithinTargetWindow => Completed && ElapsedSeconds is >= 600d and <= 900d;
    public IReadOnlyList<PrologueSectorId> SectorsVisited => _sectorsVisited;
    public string FirstCheckpoint { get; private set; } = "NotActivated";
    public IReadOnlyList<string> CheckpointsActivated => _checkpointsActivated;
    public int TotalDeaths { get; private set; }
    public int OrdinaryDeaths { get; private set; }
    public int BossDeaths { get; private set; }
    public int BossAttempts { get; private set; }
    public IReadOnlyList<double> BossReentrySeconds => _bossReentrySeconds;
    public IReadOnlyList<PrologueRecoverySample> Recoveries => _recoveries;
    public IReadOnlyList<PrologueFallSample> Falls => _falls;
    public IReadOnlyList<PrologueInputEdgeSample> RecentInputEdges => BuildRecentInputEdges();
    public bool ShortcutOpen { get; private set; }

    public void Advance(double delta)
    {
        if (Completed || delta <= 0d || !double.IsFinite(delta)) return;
        ElapsedSeconds += delta;
        PruneExpiredInputEdges();
    }

    public void VisitSector(PrologueSectorId sector)
    {
        if (!Enum.IsDefined(sector)) return;
        if (_sectorsVisited.Count == 0 || _sectorsVisited[^1] != sector) _sectorsVisited.Add(sector);
    }

    public void ActivateCheckpoint(PrologueSectorId sector)
    {
        if (sector is not (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple)) return;
        string key = sector.ToString();
        if (!_checkpointKeys.Add(key)) return;
        _checkpointsActivated.Add(key);
        if (FirstCheckpoint == "NotActivated") FirstCheckpoint = key;
    }

    public void RecordGrappleFailure(MechanicalClawFailure failure)
    {
        if (failure == MechanicalClawFailure.None) return;
        string key = failure.ToString();
        _grappleFailures[key] = _grappleFailures.GetValueOrDefault(key) + 1;
    }

    public void RecordShieldOverheat() => ShieldOverheats++;
    public int ShieldOverheats { get; private set; }

    public void RecordModeSwitch(PlayerActionMode mode)
    {
        string value = mode.ToString();
        if (_modeSwitches.Count == 0 || _modeSwitches[^1] != value) _modeSwitches.Add(value);
    }

    public void RecordSlotSelection(int slot)
    {
        if (slot is < 1 or > 4) return;
        if (_selectedSlots.Count == 0 || _selectedSlots[^1] != slot) _selectedSlots.Add(slot);
    }

    public void RecordDeath(bool inBossSector)
    {
        TotalDeaths++;
        if (inBossSector)
        {
            BossDeaths++;
            _lastBossDeathAt = ElapsedSeconds;
        }
        else OrdinaryDeaths++;
    }

    public void RecordInputEdge(string action)
    {
        if (!StableInputEdges.Contains(action)) return;
        PruneExpiredInputEdges();
        _inputEdges.Add(new InputEdgeEvent(ElapsedSeconds, action));
    }

    public void RecordFall(PrologueFallSample fall)
    {
        // A recovery callback is the last moment at which the live player/claw/checkpoint state
        // is trustworthy. Reject all non-fall reasons and any late post-reset write.
        if (fall == null || fall.Reason != "FALL RESET" || _activeRecoveryReason != null) return;
        _falls.Add(new PrologueFallSample(
            fall.Reason,
            fall.Sector,
            fall.WorldPosition,
            fall.LastGroundedPlatformId,
            fall.ActiveCheckpoint,
            fall.ClawState,
            fall.ClawTarget,
            fall.ClawMaterial,
            fall.ClawFailure,
            CloneInputs(fall.RecentInputs)));
    }

    public void BeginBossAttempt()
    {
        BossAttempts++;
        if (!_lastBossDeathAt.HasValue) return;
        _bossReentrySeconds.Add(Math.Max(0d, ElapsedSeconds - _lastBossDeathAt.Value));
        _lastBossDeathAt = null;
    }

    public void BeginRecovery(string reason, bool inBossSector)
    {
        if (string.IsNullOrWhiteSpace(reason) || _activeRecoveryReason != null) return;
        _activeRecoveryReason = reason;
        _activeRecoveryInBossSector = inBossSector;
        _activeRecoveryStartedAt = ElapsedSeconds;
    }

    public void FinishRecovery()
    {
        if (_activeRecoveryReason == null) return;
        _recoveries.Add(new PrologueRecoverySample(_activeRecoveryReason,
            _activeRecoveryInBossSector, Math.Max(0d, ElapsedSeconds - _activeRecoveryStartedAt)));
        if (_activeRecoveryInBossSector) _lastBossDeathAt = ElapsedSeconds;
        _activeRecoveryReason = null;
    }

    public void ObserveDiscoveries(bool shortcutOpen, IEnumerable<string> discoveries)
    {
        ShortcutOpen |= shortcutOpen;
        foreach (string discovery in discoveries ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(discovery)) _discoveries.Add(discovery);
    }

    public void RecordRouteNode(CanyonRouteNodeId node, bool entered)
    {
        if (!Enum.IsDefined(node)) return;
        string state = entered ? "Enter" : "Exit";
        if (_routeNodes.Count > 0 && _routeNodes[^1].Node == node.ToString() &&
            _routeNodes[^1].Event == state) return;
        _routeNodes.Add(new PrologueRouteNodeSample(node.ToString(), state, ElapsedSeconds));
    }

    public void RecordEncounter(string encounterId, bool activated)
    {
        if (string.IsNullOrWhiteSpace(encounterId)) return;
        string state = activated ? "Activated" : "Defeated";
        _encounters.Add(new PrologueEncounterSample(encounterId, state, ElapsedSeconds));
    }

    public void RecordMechanism(string mechanismId)
    {
        if (string.IsNullOrWhiteSpace(mechanismId)) return;
        _mechanisms.Add(new PrologueMechanismSample(mechanismId, ElapsedSeconds));
    }

    public void RecordShortcutDirection(string shortcutId, CanyonRouteNodeId from,
        CanyonRouteNodeId to)
    {
        if (string.IsNullOrWhiteSpace(shortcutId) || !Enum.IsDefined(from) ||
            !Enum.IsDefined(to) || from == to) return;
        PrologueShortcutTraversalSample sample = new(shortcutId, from.ToString(), to.ToString(),
            ElapsedSeconds);
        if (_shortcutTraversals.Count > 0 && _shortcutTraversals[^1] with { Seconds = 0d } ==
            sample with { Seconds = 0d }) return;
        _shortcutTraversals.Add(sample);
    }

    public void RecordFireflyTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target) ||
            _fireflyTargets.Count > 0 && _fireflyTargets[^1].Target == target) return;
        _fireflyTargets.Add(new PrologueFireflyTargetSample(target, ElapsedSeconds));
    }

    public bool Complete(bool shortcutOpen, IEnumerable<string> discoveries)
    {
        if (Completed) return false;
        Completed = true;
        ObserveDiscoveries(shortcutOpen, discoveries);
        return true;
    }

    public ProloguePlaytestReport BuildReport(PrologueSectorId currentSector,
        PrologueObjective objective, string updatedAtUtc)
    {
        var report = new ProloguePlaytestReport(
            ProloguePlaytestReport.CurrentVersion,
            RunId, StartedAtUtc, updatedAtUtc, EvidenceSource, ElapsedSeconds,
            ProloguePlaytestReportFormatter.FormatDuration(ElapsedSeconds), Completed,
            FirstClearWithinTargetWindow, currentSector.ToString(), objective.ToString(),
            ConvertSectors(_sectorsVisited), FirstCheckpoint, new List<string>(_checkpointsActivated),
            new Dictionary<string, int>(_grappleFailures, StringComparer.Ordinal), ShieldOverheats,
            new List<string>(_modeSwitches), new List<int>(_selectedSlots), TotalDeaths,
            OrdinaryDeaths, BossDeaths, BossAttempts, new List<double>(_bossReentrySeconds),
            new List<PrologueRecoverySample>(_recoveries), ShortcutOpen,
            SortedDiscoveries(), CloneFalls());
        return report with
        {
            InputContractVersion = 1,
            RouteNodes = new List<PrologueRouteNodeSample>(_routeNodes),
            Encounters = new List<PrologueEncounterSample>(_encounters),
            Mechanisms = new List<PrologueMechanismSample>(_mechanisms),
            ShortcutTraversals = new List<PrologueShortcutTraversalSample>(_shortcutTraversals),
            FireflyTargets = new List<PrologueFireflyTargetSample>(_fireflyTargets)
        };
    }

    private static IReadOnlyList<string> ConvertSectors(IEnumerable<PrologueSectorId> sectors)
    {
        var result = new List<string>();
        foreach (PrologueSectorId sector in sectors) result.Add(sector.ToString());
        return result;
    }

    private IReadOnlyList<string> SortedDiscoveries()
    {
        var result = new List<string>(_discoveries);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private IReadOnlyList<PrologueInputEdgeSample> BuildRecentInputEdges()
    {
        PruneExpiredInputEdges();
        var result = new List<PrologueInputEdgeSample>(_inputEdges.Count);
        foreach (InputEdgeEvent edge in _inputEdges)
            result.Add(new PrologueInputEdgeSample(Math.Max(0d, ElapsedSeconds - edge.Timestamp), edge.Action));
        return result;
    }

    private void PruneExpiredInputEdges()
    {
        for (int index = _inputEdges.Count - 1; index >= 0; index--)
        {
            if (ElapsedSeconds - _inputEdges[index].Timestamp > InputEdgeWindowSeconds)
                _inputEdges.RemoveAt(index);
        }
    }

    private static IReadOnlyList<PrologueInputEdgeSample> CloneInputs(
        IReadOnlyList<PrologueInputEdgeSample> inputs)
    {
        if (inputs == null || inputs.Count == 0) return Array.Empty<PrologueInputEdgeSample>();
        var result = new List<PrologueInputEdgeSample>(inputs.Count);
        foreach (PrologueInputEdgeSample input in inputs)
            result.Add(new PrologueInputEdgeSample(input.SecondsBeforeRecovery, input.Action));
        return result;
    }

    private IReadOnlyList<PrologueFallSample> CloneFalls()
    {
        var result = new List<PrologueFallSample>(_falls.Count);
        foreach (PrologueFallSample fall in _falls)
        {
            result.Add(new PrologueFallSample(
                fall.Reason, fall.Sector, fall.WorldPosition, fall.LastGroundedPlatformId,
                fall.ActiveCheckpoint, fall.ClawState, fall.ClawTarget, fall.ClawMaterial,
                fall.ClawFailure, CloneInputs(fall.RecentInputs)));
        }
        return result;
    }

    private readonly record struct InputEdgeEvent(double Timestamp, string Action);
}
