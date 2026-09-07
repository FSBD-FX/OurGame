using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Player;
using Knight.Prologue.Seamless;

namespace Knight.Prologue;

internal readonly record struct PrologueInputEdgeState(
    bool ShieldHeld,
    bool MoveLeftHeld,
    bool MoveRightHeld,
    bool MoveUpHeld,
    bool MoveDownHeld);

public partial class ProloguePlaytestRecorder : Node
{
    [Export] public string OutputDirectory { get; set; } = "user://playtests/prologue";
    [Export(PropertyHint.Range, "0.25,10,0.25,or_greater")]
    public double FlushIntervalSeconds { get; set; } = 1d;
    // Retained as a scene-serialization compatibility surface. Headless automation never writes
    // hands-on evidence, even when an old fixture sets this flag.
    public bool RecordInHeadlessMode { get; set; }

    public string LatestJsonPath { get; private set; } = string.Empty;
    public string LatestMarkdownPath { get; private set; } = string.Empty;

    private PrologueLevelController _level;
    private PrologueCompletionController _completion;
    private ProloguePlaytestSession _session;
    private PrologueSectorId _observedSector;
    private PrologueSectorId _checkpointSector;
    private bool _checkpointActivated;
    private PlayerActionModeController _actionModes;
    private PlayerEnergyController _energy;
    private MechanicalClawController _claw;
    private PlayerInputReader _input;
    private PrologueInputEdgeState _inputEdgeState;
    private ulong _lastInputFrameSequence = ulong.MaxValue;
    private string _lastGroundedPlatformId = "NotObserved";
    private bool _energyWasOverheated;
    private ulong _lastFailureCastId;
    private MechanicalClawFailure _lastFailure;
    private bool _bossWasActive;
    private bool _initialized;
    private double _flushRemaining;
    private string _runJsonPath = string.Empty;
    private string _runMarkdownPath = string.Empty;

    public override void _Process(double delta)
    {
        if (!_initialized)
        {
            TryInitialize();
            return;
        }

        _session.Advance(delta);
        ObserveRuntime();
        _flushRemaining -= delta;
        if (_flushRemaining <= 0d)
        {
            Persist();
            _flushRemaining = Math.Max(0.25d, FlushIntervalSeconds);
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_level))
        {
            _level.RecoveryRequested -= OnRecoveryRequested;
            _level.RecoveryCompleted -= OnRecoveryCompleted;
        }
        UnbindTelemetry();
        if (_initialized) Persist();
    }

    private void TryInitialize()
    {
        if (DisplayServer.GetName() == "headless")
        {
            SetProcess(false);
            return;
        }
        _level = GetParentOrNull<PrologueLevelController>();
        if (_level?.Player == null || !_level.CurrentSector.HasValue || _level.CurrentSectorRoot == null) return;
        _completion = _level.GetNodeOrNull<PrologueCompletionController>("Completion");
        if (_completion == null) return;

        string evidenceSource = ResolveEvidenceSource();
        if (evidenceSource == null)
        {
            SetProcess(false);
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string runId = now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
        _session = new ProloguePlaytestSession(runId, now.ToString("O"), evidenceSource);
        _observedSector = _level.CurrentSector.Value;
        _session.VisitSector(_observedSector);
        PrologueCheckpointSnapshot checkpoint = _level.Progress.RestoreCheckpoint();
        _session.ObserveDiscoveries(checkpoint.ShortcutOpen, checkpoint.Progress.Discoveries);
        _checkpointSector = checkpoint.SectorId;
        _checkpointActivated = checkpoint.HasActivatedCheckpoint;
        if (_checkpointActivated) _session.ActivateCheckpoint(_checkpointSector);
        BindTelemetry();
        _level.RecoveryRequested += OnRecoveryRequested;
        _level.RecoveryCompleted += OnRecoveryCompleted;

        string directory = NormalizeDirectory(OutputDirectory);
        LatestJsonPath = $"{directory}/latest.json";
        LatestMarkdownPath = $"{directory}/latest.md";
        _runJsonPath = $"{directory}/{runId}.json";
        _runMarkdownPath = $"{directory}/{runId}.md";
        _initialized = true;
        _flushRemaining = Math.Max(0.25d, FlushIntervalSeconds);
        Persist();
    }

    private void ObserveRuntime()
    {
        bool changed = false;
        if (_level.CurrentSector.HasValue && _level.CurrentSector.Value != _observedSector)
        {
            _observedSector = _level.CurrentSector.Value;
            _session.VisitSector(_observedSector);
            _bossWasActive = false;
            changed = true;
        }

        ObserveGroundedSupport();
        ObserveInputEdges();

        PrologueCheckpointSnapshot checkpoint = _level.Progress.RestoreCheckpoint();
        bool shortcutBefore = _session.ShortcutOpen;
        int discoveryCountBefore = _session.BuildReport(_observedSector,
            _completion.CurrentObjective, string.Empty).Discoveries.Count;
        _session.ObserveDiscoveries(checkpoint.ShortcutOpen, checkpoint.Progress.Discoveries);
        int discoveryCountAfter = _session.BuildReport(_observedSector,
            _completion.CurrentObjective, string.Empty).Discoveries.Count;
        if (_session.ShortcutOpen != shortcutBefore || discoveryCountAfter != discoveryCountBefore)
            changed = true;
        if (checkpoint.HasActivatedCheckpoint &&
            (!_checkpointActivated || checkpoint.SectorId != _checkpointSector))
        {
            _checkpointActivated = true;
            _checkpointSector = checkpoint.SectorId;
            _session.ActivateCheckpoint(checkpoint.SectorId);
            changed = true;
        }

        BossArenaController arena = FindDescendant<BossArenaController>(_level.CurrentSectorRoot);
        bool bossActive = arena?.IsEncounterActive == true;
        if (bossActive && !_bossWasActive)
        {
            _session.BeginBossAttempt();
            changed = true;
        }
        _bossWasActive = bossActive;

        if (_completion.CompletionCount > 0 && !_session.Completed)
        {
            _session.Complete(_level.Progress.BellChainShortcutOpen, _level.Progress.Discoveries);
            changed = true;
        }

        if (changed) Persist();
    }

    private void OnRecoveryRequested(string reason)
    {
        if (reason is not ("FALL RESET" or "DEATH RESET")) return;
        ObserveInputEdges();
        BossArenaController arena = FindDescendant<BossArenaController>(_level.CurrentSectorRoot);
        bool duringBossAttempt = arena?.IsEncounterActive == true;
        PrologueFallSample fall = reason == "FALL RESET" ? CaptureFallContext(reason) : null;
        _session.RecordDeath(duringBossAttempt);
        if (fall != null) _session.RecordFall(fall);
        _session.BeginRecovery(reason, duringBossAttempt);
        Persist();
    }

    private void OnRecoveryCompleted(string reason)
    {
        if (reason is not ("FALL RESET" or "DEATH RESET")) return;
        _session.FinishRecovery();
        Persist();
    }

    private string ResolveEvidenceSource()
    {
        return ProloguePlaytestEvidenceGrantVerifier.TryAuthorizeCurrentRun(out string source)
            ? source : null;
    }

    private void Persist()
    {
        try
        {
            ProloguePlaytestReport report = _session.BuildReport(
                _level.CurrentSector ?? _observedSector,
                _completion.CurrentObjective,
                DateTimeOffset.UtcNow.ToString("O"));
            Write(_runJsonPath, ProloguePlaytestReportFormatter.ToJson(report));
            Write(_runMarkdownPath, ProloguePlaytestReportFormatter.ToMarkdown(report));
            Write(LatestJsonPath, ProloguePlaytestReportFormatter.ToJson(report));
            Write(LatestMarkdownPath, ProloguePlaytestReportFormatter.ToMarkdown(report));
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Playtest evidence could not be written: {exception.Message}");
        }
    }

    private static void Write(string resourcePath, string content)
    {
        string absolutePath = ProjectSettings.GlobalizePath(resourcePath);
        string directory = System.IO.Path.GetDirectoryName(absolutePath) ??
            throw new InvalidOperationException($"Playtest path has no directory: {resourcePath}");
        Error directoryError = DirAccess.MakeDirRecursiveAbsolute(directory);
        if (directoryError != Error.Ok && directoryError != Error.AlreadyExists)
            throw new InvalidOperationException($"Cannot create playtest directory: {directoryError}");
        using FileAccess file = FileAccess.Open(resourcePath, FileAccess.ModeFlags.Write);
        if (file == null) throw new InvalidOperationException($"Cannot open playtest report: {FileAccess.GetOpenError()}");
        file.StoreString(content);
    }

    private static string NormalizeDirectory(string directory)
    {
        string value = string.IsNullOrWhiteSpace(directory) ? "user://playtests/prologue" : directory.Trim();
        return value.TrimEnd('/', '\\');
    }

    internal static IReadOnlyList<string> SampleInputEdges(PlayerInputFrame frame,
        ref PrologueInputEdgeState prior)
    {
        var edges = new System.Collections.Generic.List<string>(10);
        if (frame.AttackModePressed) edges.Add("Q");
        if (frame.GrappleModePressed) edges.Add("E");
        if (frame.PrimaryPressed) edges.Add("LMB_DOWN");
        if (frame.PrimaryReleased) edges.Add("LMB_UP");
        AppendEdgeTransition(edges, prior.ShieldHeld, frame.ShieldHeld, "RMB_DOWN", "RMB_UP");
        if (frame.JumpPressed) edges.Add("SPACE");
        if (frame.DashPressed) edges.Add("SHIFT");

        bool moveLeft = frame.MoveAxis < -.5f;
        bool moveRight = frame.MoveAxis > .5f;
        bool moveUp = frame.Aim.Y < -.5f;
        bool moveDown = frame.Aim.Y > .5f;
        AppendEdgeTransition(edges, prior.MoveLeftHeld, moveLeft, "A_DOWN", "A_UP");
        AppendEdgeTransition(edges, prior.MoveRightHeld, moveRight, "D_DOWN", "D_UP");
        AppendEdgeTransition(edges, prior.MoveUpHeld, moveUp, "W_DOWN", "W_UP");
        AppendEdgeTransition(edges, prior.MoveDownHeld, moveDown, "S_DOWN", "S_UP");
        prior = new PrologueInputEdgeState(frame.ShieldHeld, moveLeft, moveRight, moveUp, moveDown);
        return edges;
    }

    internal static bool TryClaimInputFrameSequence(ulong sequence, ref ulong lastSequence)
    {
        if (lastSequence == sequence) return false;
        lastSequence = sequence;
        return true;
    }

    internal static string TryReadGroundedSupportPath(CharacterBody2D player, Node sceneRoot)
    {
        if (player == null || sceneRoot == null || !player.IsOnFloor()) return string.Empty;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(index);
            if (collision == null || collision.GetNormal().Y >= -.5f) continue;
            if (collision.GetCollider() is not Node collider || !GodotObject.IsInstanceValid(collider)) continue;
            if (!sceneRoot.IsAncestorOf(collider) && collider != sceneRoot) continue;
            return sceneRoot.GetPathTo(collider).ToString();
        }
        return string.Empty;
    }

    private static void AppendEdgeTransition(System.Collections.Generic.ICollection<string> edges,
        bool wasHeld, bool isHeld, string down, string up)
    {
        if (!wasHeld && isHeld) edges.Add(down);
        else if (wasHeld && !isHeld) edges.Add(up);
    }

    private void ObserveGroundedSupport()
    {
        string support = TryReadGroundedSupportPath(_level.Player, _level.CurrentSectorRoot);
        if (!string.IsNullOrWhiteSpace(support)) _lastGroundedPlatformId = support;
    }

    private void ObserveInputEdges()
    {
        if (!GodotObject.IsInstanceValid(_input))
            _input = _level.Player.GetNodeOrNull<PlayerInputReader>("Input");
        if (_input == null) return;
        if (!TryClaimInputFrameSequence(_input.CurrentFrameSequence, ref _lastInputFrameSequence)) return;
        foreach (string edge in SampleInputEdges(_input.CurrentFrame, ref _inputEdgeState))
            _session.RecordInputEdge(edge);
    }

    private PrologueFallSample CaptureFallContext(string reason)
    {
        PrologueCheckpointSnapshot checkpoint = _level.Progress.RestoreCheckpoint();
        MechanicalClawFrame frame = _claw?.CurrentFrame ?? default;
        GrappleSurface2D target = _claw?.CurrentTarget;
        string targetName = "None";
        string targetMaterial = "None";
        if (target != null && GodotObject.IsInstanceValid(target))
        {
            targetName = target.Name.ToString();
            targetMaterial = target.Material.ToString();
        }

        return new PrologueFallSample(
            reason,
            (_level.CurrentSector ?? _observedSector).ToString(),
            new ProloguePointSample(_level.Player.GlobalPosition.X, _level.Player.GlobalPosition.Y),
            _lastGroundedPlatformId,
            checkpoint.HasActivatedCheckpoint ? checkpoint.SectorId.ToString() : "NotActivated",
            frame.State.ToString(),
            targetName,
            targetMaterial,
            frame.Failure.ToString(),
            _session.RecentInputEdges);
    }

    private static T FindDescendant<T>(Node root) where T : Node
    {
        if (root == null) return null;
        if (root is T match) return match;
        foreach (Node child in root.GetChildren())
        {
            T result = FindDescendant<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void BindTelemetry()
    {
        _input = _level.Player.GetNodeOrNull<PlayerInputReader>("Input");
        _actionModes = _level.Player.GetNodeOrNull<PlayerActionModeController>("ActionModes");
        _energy = _level.Player.GetNodeOrNull<PlayerEnergyController>("Energy");
        _claw = _level.Player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        if (_actionModes != null)
        {
            _actionModes.ModeChanged += OnModeChanged;
            _actionModes.LoadoutChanged += OnLoadoutChanged;
            _session.RecordModeSwitch(_actionModes.CurrentMode);
            _session.RecordSlotSelection(_actionModes.ActiveSlot);
        }
        if (_energy != null)
        {
            _energyWasOverheated = _energy.IsOverheated;
            _energy.EnergyFrameChanged += OnEnergyFrameChanged;
        }
        if (_claw != null) _claw.ClawFrameChanged += OnClawFrameChanged;
    }

    private void UnbindTelemetry()
    {
        if (IsInstanceValid(_actionModes))
        {
            _actionModes.ModeChanged -= OnModeChanged;
            _actionModes.LoadoutChanged -= OnLoadoutChanged;
        }
        if (IsInstanceValid(_energy)) _energy.EnergyFrameChanged -= OnEnergyFrameChanged;
        if (IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClawFrameChanged;
    }

    private void OnModeChanged(PlayerActionMode mode) => _session?.RecordModeSwitch(mode);
    private void OnLoadoutChanged(int slot) => _session?.RecordSlotSelection(slot);

    private void OnEnergyFrameChanged(PlayerEnergyFrame frame)
    {
        if (frame.State.Overheated && !_energyWasOverheated) _session?.RecordShieldOverheat();
        _energyWasOverheated = frame.State.Overheated;
    }

    private void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D _)
    {
        if (frame.Failure == MechanicalClawFailure.None) return;
        if (frame.CastId == _lastFailureCastId && frame.Failure == _lastFailure) return;
        _lastFailureCastId = frame.CastId;
        _lastFailure = frame.Failure;
        _session?.RecordGrappleFailure(frame.Failure);
    }
}
