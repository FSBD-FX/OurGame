using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Prologue.Guidance;

public partial class PrologueGuidanceController : Node
{
    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath CompletionPath { get; set; } = new("../Completion");
    [Export] public NodePath VisualPath { get; set; } = new("../Hud/Guidance/VisualRoot");

    public bool GuidanceEnabled { get; private set; } = true;
    public GameLanguage Language { get; private set; } = GameLanguage.ChineseSimplified;
    public int SameSectorFailureCount { get; private set; }
    public PrologueGuidanceInstruction CurrentInstruction { get; private set; } =
        PrologueGuidanceInstruction.Hidden;

    private PrologueLevelController _level;
    private PrologueCompletionController _completion;
    private IPrologueGuidanceVisual _visual;
    private PrologueSectorId? _observedSector;
    private bool _firstCombatNearby;
    private ulong _descentOpportunityPhysics = ulong.MaxValue;
    private bool _descentOpportunityAvailable;
    private Vector2 _descentOpportunityPoint;
    private PrologueFormalHud _formalHud;
    private ProloguePauseMenu _pause;
    private PrologueGameFlowController _flow;
    private string _lastSpokenInstruction = string.Empty;
    private PrologueFogController _fog;
    private FogOcclusionObservation? _currentFogObservation;
    private bool _receivedFogPresentation;
    private PrologueSectorId? _fogReceiptSector;
    private ulong _fogReceiptWorldId;
    private ulong _fogReceiptPlayerId;
    private ulong _fogReceiptSectorRootId;
    private bool _ascentTransferLearned;
    private bool _ascentFirstShelfSupported;
    private ulong _ascentSupportPhysics = ulong.MaxValue;
    private bool _thrusterLessonLearned;
    private ulong _thrusterLessonPlayerId;
    private ulong _thrusterLessonMotorId;
    private ulong _thrusterLessonSequence;
    // Identity belongs to the ordinary instruction actually evaluated and presented,
    // not whichever objective/claw phase happens to be live at a later resize notification.
    private PrologueObjective? _presentedOrdinaryEarlyLesson;
    private readonly HashSet<string> _missingWarnings = new(StringComparer.Ordinal);
    private Node2D _recyclingSource;
    private CanyonSpringboard _recyclingPad;
    private Action<CharacterBody2D,Vector2,ulong> _recyclingHandler;
    private PrologueRecyclingLesson _recyclingLesson;
    private ulong _recyclingLaunchSequence;
    private bool _recyclingLaunched;

    public override void _Ready()
    {
        _level = GetNode<PrologueLevelController>(LevelPath);
        _completion = GetNode<PrologueCompletionController>(CompletionPath);
        BindVisual();
        if(_level.WorldProfile!=null){Hide();return;}
        _pause = _level.GetNodeOrNull<ProloguePauseMenu>("PauseMenu");
        if (_pause != null)
        {
            _pause.Opened += OnPauseOpened;
            _pause.Closed += Refresh;
        }
        _level.RecoveryCompleted += OnRecoveryCompleted;
        _flow = _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        if (_flow != null) _flow.SessionStarted += OnSessionStarted;
        _fog = _level.GetNodeOrNull<PrologueFogController>("FogController");
        if (_fog != null) _fog.ObservationPresented += OnFogObservation;
        Refresh();
    }

    public override void _Process(double delta)
    {
        if(_level?.WorldProfile!=null){Hide();return;}
        // Label redraw is queued before FramePreDraw. Overwriting its new lane
        // text with the ordinary lesson here would make pixels stay generic
        // forever despite correct CurrentInstruction/Label.Text after pre-draw.
        // This single-use receipt only avoids that competing UI write; it never
        // authorizes a new context from an old observation. A missing publisher
        // falls back on the next tick, not an indefinitely cached lane card.
        bool received = _receivedFogPresentation;
        _receivedFogPresentation = false;
        bool retain = received && GuidanceEnabled && !GetTree().Paused &&
            GodotObject.IsInstanceValid(_level) && _level.CurrentSector == _fogReceiptSector &&
            _flow?.State == PrologueGameFlowState.Playing && !_flow.BasinQualitySampleMode &&
            !_completion.BasinQualitySampleMode && GodotObject.IsInstanceValid(_fog) &&
            _fog.IsInsideTree() && !_fog.IsQueuedForDeletion() &&
            _level.GetNodeOrNull<PrologueFogController>("FogController") == _fog &&
            GodotObject.IsInstanceValid(_level.Player) && _level.Player.IsVisibleInTree() &&
            !_level.Player.IsQueuedForDeletion() && _level.Player.GetInstanceId() == _fogReceiptPlayerId &&
            _level.GetNodeOrNull<Node2D>("WorldRoot") is { } receiptWorld &&
            !receiptWorld.IsQueuedForDeletion() && receiptWorld.GetInstanceId() == _fogReceiptWorldId &&
            GodotObject.IsInstanceValid(_level.CurrentSectorRoot) && _level.CurrentSectorRoot.IsInsideTree() &&
            !_level.CurrentSectorRoot.IsQueuedForDeletion() && _level.CurrentSectorRoot.GetInstanceId() == _fogReceiptSectorRootId &&
            receiptWorld.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is { } receiptSectors &&
            !receiptSectors.IsQueuedForDeletion() && _fogReceiptSector.HasValue &&
            receiptSectors.TryGetMountedSector(_fogReceiptSector.Value, out PrologueSector mountedReceipt) &&
            mountedReceipt == _level.CurrentSectorRoot &&
            _formalHud?.CurrentModel.RecoveryVisible != true && _formalHud?.CurrentModel.CompletionVisible != true &&
            _formalHud?.CurrentModel.BossVisible != true && _completion.CurrentObjective != PrologueObjective.Complete &&
            ResolveBossState(_level.CurrentSectorRoot) is (PrologueBossGuidanceState.None or PrologueBossGuidanceState.Inactive) &&
            _level.Player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is > 0;
        // Revoke an obsolete card before it can speak or survive another write.
        if (received && !retain) { Refresh(); return; }
        TrySpeakVisibleInstruction();
        if (!retain) Refresh();
    }

    private void TrySpeakVisibleInstruction()
    {
        if (!GuidanceEnabled || GetTree().Paused || _flow?.State != PrologueGameFlowState.Playing ||
            _formalHud?.CurrentModel.RecoveryVisible == true || _formalHud?.CurrentModel.CompletionVisible == true ||
            _formalHud?.CurrentModel.BossVisible == true || _completion?.CurrentObjective == PrologueObjective.Complete ||
            !CurrentInstruction.Visible || string.IsNullOrWhiteSpace(CurrentInstruction.StepText) ||
            _visual is not WorldTargetMarkerVisual marker || !GodotObject.IsInstanceValid(marker) ||
            !marker.IsInsideTree() || marker.IsQueuedForDeletion() || !marker.IsVisibleInTree() ||
            !marker.HasVisiblePrompt || !marker.RenderedTextContained ||
            marker.RenderedStepText != CurrentInstruction.StepText ||
            string.Equals(_lastSpokenInstruction, CurrentInstruction.StepText, StringComparison.Ordinal)) return;
        Label text = marker.GetNodeOrNull<Label>("Text");
        if (!GodotObject.IsInstanceValid(text) || !text.IsVisibleInTree() ||
            !text.Text.Contains(CurrentInstruction.StepText, StringComparison.Ordinal)) return;
        PrologueAudioDirector audio = _level.GetNodeOrNull<PrologueAudioDirector>("Audio");
        if (GodotObject.IsInstanceValid(audio) && audio.TryPlayGuidanceCrackle(CurrentInstruction.StepText))
            _lastSpokenInstruction = CurrentInstruction.StepText;
    }

    public override void _ExitTree()
    {
        ClearRecyclingSource();
        _presentedOrdinaryEarlyLesson = null;
        if (GodotObject.IsInstanceValid(_level) && _level.WorldProfile==null) _level.RecoveryCompleted -= OnRecoveryCompleted;
        if (GodotObject.IsInstanceValid(_flow)) _flow.SessionStarted -= OnSessionStarted;
        if (GodotObject.IsInstanceValid(_fog)) _fog.ObservationPresented -= OnFogObservation;
        _fog = null;
        _currentFogObservation = null;
        _receivedFogPresentation = false;
        if (GodotObject.IsInstanceValid(_pause))
        {
            _pause.Opened -= OnPauseOpened;
            _pause.Closed -= Refresh;
        }
        if (_visual is WorldTargetMarkerVisual marker && GodotObject.IsInstanceValid(marker))
            marker.EarlyLessonCoverageChanged -= UpdateEarlyLessonCoverage;
        _visual = null;
        if (GodotObject.IsInstanceValid(_formalHud) && _formalHud.IsInsideTree())
        {
            _formalHud.SetEarlyLessonCoverage(null);
            _formalHud.SetContextualObjective(string.Empty);
        }
    }

    public void SetGuidanceEnabled(bool enabled)
    {
        GuidanceEnabled = enabled;
        Refresh();
    }

    public void ToggleGuidance() => SetGuidanceEnabled(!GuidanceEnabled);

    public void RefreshNow() => Refresh();

    public void SetLanguage(GameLanguage language)
    {
        Language = Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        Refresh();
    }

    private void Refresh()
    {
        // Sample entry configures language/guidance before mounting the level.
        // Retain those settings, but tree-backed presentation begins in _Ready.
        if (!IsInsideTree()) return;
        BindVisual();
        if(_level?.WorldProfile!=null){Hide();return;}
        if (GetTree().Paused)
        {
            Hide(preserveCombatContext: true);
            return;
        }
        if (_level == null || _completion == null || !_level.CurrentSector.HasValue ||
            !GodotObject.IsInstanceValid(_level.CurrentSectorRoot))
        {
            Hide();
            return;
        }

        PrologueSectorId sectorId = _level.CurrentSector.Value;
        if (_observedSector != sectorId)
        {
            _observedSector = sectorId;
            SameSectorFailureCount = 0;
            _firstCombatNearby = false;
            _descentOpportunityPhysics = ulong.MaxValue;
            _ascentSupportPhysics = ulong.MaxValue;
        }
        _formalHud = _level.GetNodeOrNull<PrologueFormalHud>("Hud/FormalHud");
        SetMarkerlessSafeEdgePolicy();
        var player = _level.Player;
        MechanicalClawController claw = GodotObject.IsInstanceValid(player)
            ? player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw") : null;
        MechanicalClawState clawState = GodotObject.IsInstanceValid(claw)
            ? claw.CurrentFrame.State : MechanicalClawState.Idle;
        _firstCombatNearby = HasNearbyFirstCombat(sectorId, clawState);
        bool descentOpportunity = HasDescentClawOpportunity(sectorId, clawState);
        bool ascentOpportunity = HasAscentTransferOpportunity(sectorId, clawState);
        PrologueObjective evaluatedObjective = _completion.CurrentObjective;
        BossArenaController arena = _level.CurrentSectorRoot.GetNodeOrNull<BossArenaController>(
            "Gameplay/BossArenaController");
        _presentedOrdinaryEarlyLesson = null;
        CurrentInstruction = PrologueGuidanceModel.Evaluate(new PrologueGuidanceContext(
            evaluatedObjective, sectorId, ResolveBossState(_level.CurrentSectorRoot),
            SameSectorFailureCount, Language, GuidanceEnabled, clawState, _firstCombatNearby,
            descentOpportunity, TerrainClawAttached: HasTerrainClawAttachment(claw, clawState),
            BossAdmission: arena?.RequiresFragmentAdmission == true
                ? BossAdmissionModel.Evaluate(_level.Progress) : null,
            AscentTransferOpportunity: ascentOpportunity,
            ShieldLaneNearby: HasNearbyShieldLane(sectorId, clawState),
            RecyclingLesson: ResolveRecyclingLesson(sectorId,clawState),
            VisibleLaser: ResolveVisibleLaser(sectorId),
            ThrusterOpportunity: HasThrusterLessonOpportunity(sectorId, clawState)),
            ResolveInputMapGlyphs(Language));
        _formalHud?.SetContextualObjective(CurrentInstruction.ContextTitle);
        Present(sectorId, claw);
        if (CurrentInstruction.Visible && string.IsNullOrEmpty(CurrentInstruction.ContextTitle) &&
            evaluatedObjective is (PrologueObjective.LearnAttackMode or PrologueObjective.LearnGrappleMode) &&
            clawState is not (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost))
            _presentedOrdinaryEarlyLesson = evaluatedObjective;
        UpdateEarlyLessonCoverage();
    }

    private void OnFogObservation(FogOcclusionObservation observation)
    {
        if (!GodotObject.IsInstanceValid(_level) || !IsInsideTree() || IsQueuedForDeletion() ||
            !_level.CurrentSector.HasValue) return;
        // Valid only during the synchronous current pre-draw submission. Never
        // authorize from the previous frame's cue count or add a physics query.
        _currentFogObservation = observation;
        try
        {
            Refresh();
            if (HasCurrentFogObservation(_level.CurrentSector.Value))
            {
                _receivedFogPresentation = true;
                _fogReceiptSector = _level.CurrentSector;
                _fogReceiptWorldId = observation.WorldInstanceId;
                _fogReceiptPlayerId = _level.Player.GetInstanceId();
                _fogReceiptSectorRootId = _level.CurrentSectorRoot.GetInstanceId();
            }
            else _receivedFogPresentation = false;
        }
        finally { _currentFogObservation = null; }
    }

    private VisibleLaserWarning ResolveVisibleLaser(PrologueSectorId sector)
    {
        if (!HasCurrentFogObservation(sector)) return VisibleLaserWarning.None;
        var cues = _level.GetNodeOrNull<FogReadabilityOverlay>("ProtectedCues/Attacks");
        return GodotObject.IsInstanceValid(cues)
            ? cues.ReadVisibleLaser(_currentFogObservation.Value.Frame) : VisibleLaserWarning.None;
    }

    private bool HasCurrentFogObservation(PrologueSectorId sector)
    {
        if (!GuidanceEnabled || _currentFogObservation is not { } observation ||
            _flow?.State != PrologueGameFlowState.Playing || _flow.BasinQualitySampleMode ||
            _completion.BasinQualitySampleMode || GetTree().Paused ||
            !GodotObject.IsInstanceValid(_fog) || !_fog.IsInsideTree() || _fog.IsQueuedForDeletion() ||
            _level.GetNodeOrNull<PrologueFogController>("FogController") != _fog ||
            _formalHud?.CurrentModel.RecoveryVisible == true || _formalHud?.CurrentModel.CompletionVisible == true ||
            _formalHud?.CurrentModel.BossVisible == true) return false;
        CharacterBody2D player = _level.Player;
        Node2D world = _level.GetNodeOrNull<Node2D>("WorldRoot");
        SeamlessPrologueWorld sectors = world?.GetNodeOrNull<SeamlessPrologueWorld>("Sectors");
        return GodotObject.IsInstanceValid(player) && player.IsInsideTree() && player.IsVisibleInTree() &&
            !player.IsQueuedForDeletion() && player.IsPhysicsProcessing() &&
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is > 0 &&
            GodotObject.IsInstanceValid(world) && world.IsInsideTree() && !world.IsQueuedForDeletion() &&
            sectors?.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion &&
            sectors.TryGetMountedSector(sector, out PrologueSector mounted) && mounted == _level.CurrentSectorRoot &&
            observation.WorldInstanceId == world.GetInstanceId() && observation.PhysicsFrame == Engine.GetPhysicsFrames() &&
            observation.ActorWorld == player.GlobalPosition && observation.Frame?.IsComplete == true &&
            ReferenceEquals(observation.Frame, _fog.LastOcclusionFrame);
    }

    private bool HasNearbyShieldLane(PrologueSectorId sector, MechanicalClawState state)
    {
        if (!GuidanceEnabled || sector != PrologueSectorId.CanyonBasin ||
            state != MechanicalClawState.Idle || _currentFogObservation is not { } observation ||
            _flow?.State != PrologueGameFlowState.Playing || _flow.BasinQualitySampleMode ||
            _completion.BasinQualitySampleMode || GetTree().Paused ||
            !GodotObject.IsInstanceValid(_fog) || !_fog.IsInsideTree() || _fog.IsQueuedForDeletion() ||
            _level.GetNodeOrNull<PrologueFogController>("FogController") != _fog ||
            _formalHud?.CurrentModel.RecoveryVisible == true || _formalHud?.CurrentModel.CompletionVisible == true ||
            _formalHud?.CurrentModel.BossVisible == true) return false;
        CharacterBody2D player = _level.Player;
        Node2D world = _level.GetNodeOrNull<Node2D>("WorldRoot");
        SeamlessPrologueWorld sectors = world?.GetNodeOrNull<SeamlessPrologueWorld>("Sectors");
        if (!GodotObject.IsInstanceValid(player) || !player.IsVisibleInTree() ||
            player.IsQueuedForDeletion() || !player.IsPhysicsProcessing() ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0 ||
            !GodotObject.IsInstanceValid(world) || world.IsQueuedForDeletion() ||
            sectors?.LayoutVersion != CanyonWorldLayout.CurrentLayoutVersion ||
            !sectors.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector basin) ||
            basin != _level.CurrentSectorRoot || observation.WorldInstanceId != world.GetInstanceId() ||
            observation.PhysicsFrame != Engine.GetPhysicsFrames() ||
            observation.ActorWorld != player.GlobalPosition ||
            !ReferenceEquals(observation.Frame, _fog.LastOcclusionFrame)) return false;
        CanyonShieldLaneHazard lane = basin.GetNodeOrNull<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
        FogShieldLaneReadabilityOverlay cues = _level.GetNodeOrNull<FogShieldLaneReadabilityOverlay>("ProtectedCues/ShieldLane");
        return GodotObject.IsInstanceValid(cues) && cues.HasNearbyVisibleLane(lane, observation.Frame);
    }

    private void BindVisual()
    {
        IPrologueGuidanceVisual next = GetNodeOrNull(VisualPath) as IPrologueGuidanceVisual;
        if (ReferenceEquals(next, _visual)) return;
        _presentedOrdinaryEarlyLesson = null;
        if (_visual is WorldTargetMarkerVisual previous && GodotObject.IsInstanceValid(previous))
            previous.EarlyLessonCoverageChanged -= UpdateEarlyLessonCoverage;
        _visual = next;
        if (_visual is WorldTargetMarkerVisual marker)
            marker.EarlyLessonCoverageChanged += UpdateEarlyLessonCoverage;
    }

    private void OnPauseOpened()
    {
        // This node pauses with the tree: no automatic refresh can withdraw the
        // last Playing card until Resume. The menu owns teaching while it is open.
        Hide(preserveCombatContext: true);
    }

    private void UpdateEarlyLessonCoverage()
    {
        if (!GodotObject.IsInstanceValid(_formalHud) || !_formalHud.IsInsideTree())
        {
            _presentedOrdinaryEarlyLesson = null;
            return;
        }
        if (!IsInsideTree() || !GodotObject.IsInstanceValid(_level) ||
            !GodotObject.IsInstanceValid(_completion))
        {
            _presentedOrdinaryEarlyLesson = null;
            _formalHud.SetEarlyLessonCoverage(null);
            return;
        }
        PrologueObjective? covered = null;
        PrologueObjective objective = _completion.CurrentObjective;
        WorldTargetMarkerVisual marker = _visual as WorldTargetMarkerVisual;
        Label label = GodotObject.IsInstanceValid(marker) ? marker.GetNodeOrNull<Label>("Text") : null;
        Control backdrop = GodotObject.IsInstanceValid(marker) ? marker.GetNodeOrNull<Control>("Backdrop") : null;
        CharacterBody2D player = _level.Player;
        MechanicalClawController claw = GodotObject.IsInstanceValid(player)
            ? player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw") : null;
        MechanicalClawState clawState = GodotObject.IsInstanceValid(claw)
            ? claw.CurrentFrame.State : MechanicalClawState.Idle;
        bool fullCurrentCard = CurrentInstruction.Visible && !string.IsNullOrWhiteSpace(CurrentInstruction.StepText) &&
            GodotObject.IsInstanceValid(marker) && marker.IsInsideTree() && !marker.IsQueuedForDeletion() &&
            marker.IsVisibleInTree() && marker.HasVisiblePrompt && marker.RenderedTextContained &&
            marker.RenderedStepText == CurrentInstruction.StepText &&
            GodotObject.IsInstanceValid(label) && !label.IsQueuedForDeletion() && label.IsVisibleInTree() &&
            GodotObject.IsInstanceValid(backdrop) && backdrop.IsVisibleInTree() &&
            label.Text.Contains(CurrentInstruction.StepText, StringComparison.Ordinal) &&
            marker.PromptRect.Grow(.5f).Encloses(label.GetRect()) &&
            label.GetMinimumSize().Y <= label.Size.Y + .5f;
        if (!fullCurrentCard || _presentedOrdinaryEarlyLesson != objective ||
            clawState is (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost))
            _presentedOrdinaryEarlyLesson = null;
        PrologueGameFlowController flow = _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        if (GuidanceEnabled && !GetTree().Paused && !_completion.BasinQualitySampleMode &&
            GodotObject.IsInstanceValid(player) && player.IsInsideTree() &&
            flow?.State == PrologueGameFlowState.Playing && !flow.BasinQualitySampleMode &&
            _level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors")?.LayoutVersion ==
                CanyonWorldLayout.CurrentLayoutVersion &&
            objective is (PrologueObjective.LearnAttackMode or PrologueObjective.LearnGrappleMode) &&
            _presentedOrdinaryEarlyLesson == objective &&
            clawState is not (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost) &&
            fullCurrentCard &&
            string.IsNullOrEmpty(CurrentInstruction.ContextTitle) &&
            _formalHud.IsVisibleInTree() && !_formalHud.CurrentModel.RecoveryVisible &&
            !_formalHud.CurrentModel.CompletionVisible && !_formalHud.CurrentModel.BossVisible)
            covered = objective;
        _formalHud.SetEarlyLessonCoverage(covered);
    }

    private static bool HasTerrainClawAttachment(MechanicalClawController claw, MechanicalClawState state)
    {
        if (state is not (MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost) ||
            !GodotObject.IsInstanceValid(claw) || !claw.IsInsideTree()) return false;
        GrappleSurface2D target = claw.CurrentTarget;
        // Material alone is insufficient: legacy chains may also be TempleMetal.
        // Use the existing live lock, not a fresh per-render physics query.
        return GodotObject.IsInstanceValid(target) && target.IsInsideTree() && target.IsAvailable &&
            target.GetTree() == claw.GetTree() && claw.CurrentFrame.LockedTargetRuntimeId != 0UL &&
            claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId() &&
            target.UseCollisionHitPoint &&
            target.Material is (GrappleSurfaceMaterial.GripStone or GrappleSurfaceMaterial.TempleMetal) &&
            target is not ThreadTarget2D &&
            FindFirstDescendant<LegacyThreadInteractionAdapter>(target) == null && target.Validate().Count == 0;
    }

    private void OnSessionStarted()
    {
        ClearRecyclingSource();
        _receivedFogPresentation = false;
        _lastSpokenInstruction = string.Empty;
        _level?.GetNodeOrNull<Knight.Audio.PrologueAudioDirector>("Audio")?.ResetGuidanceSession();
        // Per formal UI session, not persisted and not reset by death, pause or sector streaming.
        _ascentTransferLearned = false;
        _ascentSupportPhysics = ulong.MaxValue;
        _thrusterLessonLearned = false;
        _thrusterLessonPlayerId = _thrusterLessonMotorId = _thrusterLessonSequence = 0UL;
        Refresh();
    }

    private bool HasThrusterLessonOpportunity(PrologueSectorId sector, MechanicalClawState clawState)
    {
        CharacterBody2D player = _level.Player;
        PlayerMotor motor = LiveBranch(player) ? player.GetNodeOrNull<PlayerMotor>("Motor") : null;
        if (!LiveBranch(motor))
        {
            _thrusterLessonLearned = false;
            _thrusterLessonPlayerId = _thrusterLessonMotorId = _thrusterLessonSequence = 0UL;
            return false;
        }
        ulong playerId = player.GetInstanceId(), motorId = motor.GetInstanceId();
        ulong latest = motor.ThrusterPresentationSequence;
        if (_thrusterLessonPlayerId != playerId || _thrusterLessonMotorId != motorId)
        {
            // A new reader/owner starts now. The bounded journal survives
            // transient resets and may contain another session's real Started.
            _thrusterLessonLearned = false;
            _thrusterLessonPlayerId = playerId;
            _thrusterLessonMotorId = motorId;
            _thrusterLessonSequence = latest;
        }
        bool playing = _flow?.State == PrologueGameFlowState.Playing &&
            !_flow.BasinQualitySampleMode && !_completion.BasinQualitySampleMode &&
            !GetTree().Paused && player.IsVisibleInTree() && player.IsPhysicsProcessing() &&
            motor.IsPhysicsProcessing() && motor.CanProcess() &&
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is > 0 &&
            _formalHud?.CurrentModel.RecoveryVisible != true &&
            _formalHud?.CurrentModel.CompletionVisible != true &&
            _level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors")?.LayoutVersion ==
                CanyonWorldLayout.CurrentLayoutVersion;
        if (!playing)
        {
            _thrusterLessonSequence = latest;
            return false;
        }
        if (_thrusterLessonSequence > latest ||
            _thrusterLessonSequence < motor.ThrusterPresentationFirstSequence - 1UL)
            _thrusterLessonSequence = latest; // Do not infer success from a missing prefix.
        if (!_thrusterLessonLearned && _thrusterLessonSequence < latest)
        {
            ThrusterPulseSnapshot pulse = motor.ThrusterState;
            foreach (ThrusterPresentationTransition fact in motor.ReadThrusterTransitionsAfter(_thrusterLessonSequence))
                if (fact.Sequence <= latest && fact.Kind == ThrusterTransitionKind.Started &&
                    fact.Identity.OwnerId == motorId && pulse.OwnerId == motorId &&
                    fact.Identity.Lifecycle == pulse.Lifecycle &&
                    fact.Identity.ConfigurationEpoch == pulse.ConfigurationEpoch &&
                    fact.Identity.BurnId != 0UL && fact.Identity.BurnId <= pulse.BurnSerial)
                    _thrusterLessonLearned = true;
        }
        _thrusterLessonSequence = latest;
        // Observe real success even with guidance disabled and in later sectors.
        // Offering this optional card does not ask the player to jump or spend a pulse.
        return !_thrusterLessonLearned && sector == PrologueSectorId.CanyonEntrance &&
            player.IsOnFloor() && clawState == MechanicalClawState.Idle &&
            player.GetNodeOrNull<PlayerCombat>("Combat")?.IsAttacking != true &&
            player.GetNodeOrNull<PlayerShieldController>("Shield")?.IsActive != true &&
            player.GetNodeOrNull<PlayerInputReader>("Input")?.RawFrame.ShieldHeld != true;
    }

    private static bool LiveBranch(Node node)
    {
        if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node ancestor = node; ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor.IsQueuedForDeletion()) return false;
        return true;
    }

    private bool HasAscentTransferOpportunity(PrologueSectorId sector, MechanicalClawState state)
    {
        CharacterBody2D player = _level.Player;
        if (sector != PrologueSectorId.CanyonAscent || _flow?.State != PrologueGameFlowState.Playing ||
            GetTree().Paused || _flow.BasinQualitySampleMode || _completion.BasinQualitySampleMode ||
            _level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors")?.LayoutVersion !=
                CanyonWorldLayout.CurrentLayoutVersion ||
            !GodotObject.IsInstanceValid(player) || player.IsQueuedForDeletion() ||
            !player.IsVisibleInTree() || !player.IsPhysicsProcessing() ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0 ||
            _formalHud?.CurrentModel.RecoveryVisible == true || _formalHud?.CurrentModel.CompletionVisible == true)
        {
            _ascentSupportPhysics = ulong.MaxValue;
            return false;
        }
        ulong physics = Engine.GetPhysicsFrames();
        if (_ascentSupportPhysics != physics)
        {
            _ascentSupportPhysics = physics;
            // Observe success even when guidance is off, including Main's direct lift arrival.
            _ascentTransferLearned |= HasFullShelfSupport(player,
                _level.CurrentSectorRoot.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/AscentGrip2"));
            _ascentFirstShelfSupported = !_ascentTransferLearned && HasFullShelfSupport(player,
                _level.CurrentSectorRoot.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/AscentGrip1"));
        }
        return GuidanceEnabled && !_ascentTransferLearned && _ascentFirstShelfSupported &&
            state == MechanicalClawState.Idle &&
            _completion.CurrentObjective is (PrologueObjective.ClimbCanyon or PrologueObjective.EnterTemple) &&
            _formalHud?.CurrentModel.BossVisible != true &&
            player.GetNodeOrNull<PlayerInputReader>("Input")?.CurrentFrame.ShieldHeld != true &&
            player.GetNodeOrNull<PlayerShieldController>("Shield")?.IsActive != true;
    }

    private static bool HasFullShelfSupport(CharacterBody2D player, StaticBody2D shelf)
    {
        if (!GodotObject.IsInstanceValid(shelf) || !shelf.IsInsideTree() || shelf.IsQueuedForDeletion() ||
            !player.IsOnFloor()) return false;
        CollisionShape2D body = player.GetNodeOrNull<CollisionShape2D>("BodyCollider");
        CollisionShape2D support = shelf.GetNodeOrNull<CollisionShape2D>("Collision");
        if (!GodotObject.IsInstanceValid(body) || !GodotObject.IsInstanceValid(support) ||
            body.Disabled || support.Disabled ||
            body.Shape is not CapsuleShape2D capsule || support.Shape is not RectangleShape2D rectangle)
            return false;
        static bool AxisAligned(Transform2D t) => t.Origin.IsFinite() && t.X.IsFinite() && t.Y.IsFinite() &&
            Mathf.Abs(t.X.Y) < .001f && Mathf.Abs(t.Y.X) < .001f && t.X.X != 0f && t.Y.Y != 0f;
        Transform2D a = body.GlobalTransform, b = support.GlobalTransform;
        if (!AxisAligned(a) || !AxisAligned(b) || !float.IsFinite(capsule.Radius) ||
            !float.IsFinite(capsule.Height) || capsule.Radius <= 0f || capsule.Height < capsule.Radius * 2f ||
            !rectangle.Size.IsFinite() || rectangle.Size.X <= 0f || rectangle.Size.Y <= 0f) return false;
        float radius = capsule.Radius * Mathf.Abs(a.X.X);
        float halfWidth = rectangle.Size.X * Mathf.Abs(b.X.X) * .5f;
        float feet = a.Origin.Y + capsule.Height * Mathf.Abs(a.Y.Y) * .5f;
        float top = b.Origin.Y - rectangle.Size.Y * Mathf.Abs(b.Y.Y) * .5f;
        if (!float.IsFinite(radius) || !float.IsFinite(halfWidth) || !float.IsFinite(feet) ||
            !float.IsFinite(top) || a.Origin.X - radius < b.Origin.X - halfWidth ||
            a.Origin.X + radius > b.Origin.X + halfWidth || Mathf.Abs(feet - top) > 2f) return false;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            using KinematicCollision2D collision = player.GetSlideCollision(index);
            if (collision.GetCollider() == shelf && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private bool HasDescentClawOpportunity(PrologueSectorId sector, MechanicalClawState state)
    {
        CharacterBody2D player = _level.Player;
        if (!GuidanceEnabled || sector != PrologueSectorId.CanyonDescent ||
            _completion.CurrentObjective != PrologueObjective.ReachBasin ||
            state is not (MechanicalClawState.Idle or MechanicalClawState.Aim or
                MechanicalClawState.Windup or MechanicalClawState.Travel) ||
            !GodotObject.IsInstanceValid(player) || !player.IsVisibleInTree() || !player.IsPhysicsProcessing() ||
            _formalHud?.CurrentModel.RecoveryVisible == true || _formalHud?.CurrentModel.CompletionVisible == true ||
            _formalHud?.CurrentModel.BossVisible == true ||
            player.GetNodeOrNull<PlayerInputReader>("Input")?.CurrentFrame.ShieldHeld == true ||
            player.GetNodeOrNull<PlayerShieldController>("Shield")?.IsActive == true ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0)
        {
            _descentOpportunityPhysics = ulong.MaxValue;
            return false;
        }
        // One validated result per physical tick, reused for both copy and marker.
        ulong physics = Engine.GetPhysicsFrames();
        if (_descentOpportunityPhysics != physics)
        {
            _descentOpportunityPhysics = physics;
            _descentOpportunityAvailable = DescentClawGuidance.TryResolve(
                _level.CurrentSectorRoot, player, out _descentOpportunityPoint);
        }
        return _descentOpportunityAvailable;
    }

    private bool HasNearbyFirstCombat(PrologueSectorId sector, MechanicalClawState clawState)
    {
        CharacterBody2D player = _level.Player;
        if (!GuidanceEnabled || sector != PrologueSectorId.CanyonEntrance ||
            clawState != MechanicalClawState.Idle || !GodotObject.IsInstanceValid(player) ||
            !player.IsVisibleInTree() || !player.IsPhysicsProcessing() ||
            _formalHud?.CurrentModel.RecoveryVisible == true ||
            _formalHud?.CurrentModel.CompletionVisible == true ||
            _formalHud?.CurrentModel.BossVisible == true ||
            player.GetNodeOrNull<PlayerActionModeController>("ActionModes")?.CurrentMode != PlayerActionMode.Attack ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0)
            return false;

        // Layout 2's marker is not an actor. Resolve the original encounter's
        // live ownership, never a separate resident that shares its carrier type.
        Node actorRoot = _level.CurrentSectorRoot.GetNodeOrNull("Gameplay/Actors");
        RustShellSquire enemy = null;
        if (_level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors")?.LayoutVersion ==
            CanyonWorldLayout.CurrentLayoutVersion)
        {
            var encounter = _level.CurrentSectorRoot.GetNodeOrNull<CanyonEncounterGroup>(
                "Gameplay/Encounters/LowCeilingBridgeEncounter");
            if (!GodotObject.IsInstanceValid(encounter) || !GodotObject.IsInstanceValid(actorRoot))
                return false;
            foreach (Node child in actorRoot.GetChildren())
                if (child is RustShellSquire candidate && encounter.OwnsLiveActor(candidate))
                {
                    enemy = candidate;
                    break;
                }
        }
        else
            enemy = actorRoot?.GetNodeOrNull("RustShellSquire") as RustShellSquire;
        if (!GodotObject.IsInstanceValid(enemy) || !enemy.IsVisibleInTree() ||
            enemy.GetNodeOrNull<HealthComponent>(enemy.HealthPath)?.CurrentHealth is not > 0)
            return false;

        Vector2 separation = enemy.GlobalPosition - player.GlobalPosition;
        // A wider exit radius keeps the short card stable at the edge of the encounter.
        float range = _firstCombatNearby ? 400f : 340f;
        return Mathf.Abs(separation.X) <= range && Mathf.Abs(separation.Y) <= 100f &&
            _level.GetViewportRect().HasPoint(enemy.GetGlobalTransformWithCanvas().Origin) &&
            enemy.HasLineOfSightTo(player);
    }

    private static T FindFirstDescendant<T>(Node root) where T : Node
    {
        if (!GodotObject.IsInstanceValid(root)) return null;
        foreach (Node child in root.GetChildren())
        {
            if (child is T match) return match;
            T nested = FindFirstDescendant<T>(child);
            if (nested != null) return nested;
        }
        return null;
    }

    private PrologueRecyclingLesson ResolveRecyclingLesson(PrologueSectorId sectorId,MechanicalClawState state)
    {
        Node2D chosen=null;
        var lesson=PrologueRecyclingLesson.None;
        CharacterBody2D player=_level.Player;
        if(GuidanceEnabled && _completion.CurrentObjective!=PrologueObjective.Complete &&
            sectorId==PrologueSectorId.CanyonEntrance && state==MechanicalClawState.Idle &&
            _flow?.State==PrologueGameFlowState.Playing && GodotObject.IsInstanceValid(player) &&
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is >0)
        {
            Node mechanisms=_level.CurrentSectorRoot.GetNodeOrNull<Node>("Gameplay/Mechanisms");
            var combo=mechanisms?.GetNodeOrNull<CanyonSpringboard>("CombinationSpringboard");
            var paired=mechanisms?.GetNodeOrNull<CanyonFixedGear>("CombinationFixedGear");
            var pad=mechanisms?.GetNodeOrNull<CanyonSpringboard>("PracticeSpringboard");
            var gear=mechanisms?.GetNodeOrNull<CanyonFixedGear>("PracticeFixedGear");
            // These four authored devices share one unobstructed entry ledge.
            // Keep a semantic card through recharge; never cycle its text with
            // per-frame cooldown values or move the fixed reading rectangle.
            bool Near(Node2D source,float left,float right,float vertical)=>
                GodotObject.IsInstanceValid(source) && source.IsInsideTree() && !source.IsQueuedForDeletion() &&
                source.IsVisibleInTree() && source.CanProcess() && source.GetParent()==mechanisms &&
                source.GlobalPosition.IsFinite() && player.GlobalPosition.IsFinite() &&
                player.GlobalPosition.X>=source.GlobalPosition.X-left &&
                player.GlobalPosition.X<=source.GlobalPosition.X+right &&
                Mathf.Abs(player.GlobalPosition.Y-source.GlobalPosition.Y)<=vertical;
            if(Near(combo,160,80,180) && combo.HasLiveSensor &&
                GodotObject.IsInstanceValid(paired) && paired.IsDangerTelegraphActive &&
                paired.GlobalPosition.DistanceTo(combo.GlobalPosition)<240)
            { chosen=combo; lesson=PrologueRecyclingLesson.Combination; }
            else if(Near(pad,120,120,180) && pad.HasLiveSensor)
            { chosen=pad; lesson=PrologueRecyclingLesson.Springboard; }
            else if(Near(gear,100,100,210) && gear.IsDangerTelegraphActive)
            { chosen=gear; lesson=PrologueRecyclingLesson.FixedGear; }
        }
        if(!ReferenceEquals(chosen,_recyclingSource) || lesson!=_recyclingLesson)
        {
            ClearRecyclingSource(); _recyclingSource=chosen; _recyclingLesson=lesson;
            if(chosen is CanyonSpringboard pad)
            {
                _recyclingPad=pad; _recyclingLaunchSequence=pad.LaunchSequence;
                _recyclingHandler=(actor,velocity,sequence)=>ObserveRecyclingLaunch(pad,actor,velocity,sequence);
                pad.Launched+=_recyclingHandler;
            }
        }
        return lesson==PrologueRecyclingLesson.Combination && _recyclingLaunched
            ?PrologueRecyclingLesson.CombinationLaunched:lesson;
    }

    private void ObserveRecyclingLaunch(CanyonSpringboard source,CharacterBody2D actor,Vector2 velocity,ulong sequence)
    {
        if(!ReferenceEquals(source,_recyclingPad) || !ReferenceEquals(source,_recyclingSource) ||
            _recyclingLesson!=PrologueRecyclingLesson.Combination ||
            !GodotObject.IsInstanceValid(_level) || _level.CurrentSector!=PrologueSectorId.CanyonEntrance ||
            _level.Player!=actor || !source.HasLiveSensor || sequence<=_recyclingLaunchSequence ||
            !velocity.IsEqualApprox(source.EffectiveLaunchVelocity)) return;
        _recyclingLaunchSequence=sequence; _recyclingLaunched=true;
        // Defer normal Refresh to presentation; do not re-enter the motion or
        // collision callback, infer a death cause, or complete an objective.
    }

    private void ClearRecyclingSource()
    {
        if(GodotObject.IsInstanceValid(_recyclingPad) && _recyclingHandler!=null)
            _recyclingPad.Launched-=_recyclingHandler;
        _recyclingSource=null; _recyclingPad=null; _recyclingHandler=null;
        _recyclingLesson=PrologueRecyclingLesson.None; _recyclingLaunched=false; _recyclingLaunchSequence=0;
    }

    private void Present(PrologueSectorId sectorId, MechanicalClawController claw)
    {
        if (!CurrentInstruction.Visible)
        {
            SetExpandedTeaching(false);
            _visual?.Clear();
            return;
        }
        if (_visual == null) return;

        SetExpandedTeaching(CurrentInstruction.ContextTitle==Knight.Localization.PrologueStrings.Get("guidance.recycling_title",Language) ||
            SameSectorFailureCount >= 2 && string.IsNullOrEmpty(CurrentInstruction.ContextTitle));

        var views = new List<PrologueGuidanceTargetView>();
        PrologueSector sector = _level.CurrentSectorRoot;
        foreach (PrologueGuidanceTargetKey key in CurrentInstruction.Targets)
        {
            // A pending lesson retains its objective/text when the player leaves its
            // authored sector. It has no local static marker in the other sector.
            // Same-owner, unknown and dynamic keys still take the validation path below.
            if (PrologueGuidanceTargetResolver.TryGetStaticOwningSector(key, out var owner) && owner != sectorId)
                continue;
            Vector2 screen;
            bool dynamicClawTarget = key is PrologueGuidanceTargetKey.DescentClawOpportunity or
                PrologueGuidanceTargetKey.ClawAttachment;
            if (dynamicClawTarget)
            {
                Vector2 worldPoint = _descentOpportunityPoint;
                if (key == PrologueGuidanceTargetKey.ClawAttachment)
                {
                    if (!GodotObject.IsInstanceValid(claw) || !GodotObject.IsInstanceValid(claw.CurrentTarget) ||
                        !claw.CurrentTarget.IsAvailable ||
                        claw.CurrentFrame.LockedTargetRuntimeId != claw.CurrentTarget.GetInstanceId()) continue;
                    worldPoint = claw.CurrentFrame.AnchorPoint;
                }
                var player = _level.Player;
                if (!GodotObject.IsInstanceValid(player)) continue;
                screen = player.GetGlobalTransformWithCanvas() * player.ToLocal(worldPoint);
            }
            else if (PrologueGuidanceTargetResolver.TryResolve(sector, sectorId, key, out Node2D target))
                screen = target.GetGlobalTransformWithCanvas().Origin;
            else
            {
                WarnMissing(sectorId, key);
                continue;
            }
            Rect2 viewportRect = _level.GetViewportRect();
            Vector2 clamped = new(
                Mathf.Clamp(screen.X, 28f, viewportRect.Size.X - 28f),
                Mathf.Clamp(screen.Y, 28f, viewportRect.Size.Y - 28f));
            Vector2 offset = screen - clamped;
            bool onScreen = viewportRect.HasPoint(screen);
            views.Add(new PrologueGuidanceTargetView(clamped,
                offset.LengthSquared() > .001f ? offset.Normalized() : Vector2.Zero,
                offset.Length(), onScreen, dynamicClawTarget ? new Vector2(32, 32) : default));
        }
        _visual.Present(CurrentInstruction, views);
    }

    private void Hide(bool preserveCombatContext = false)
    {
        _receivedFogPresentation = false;
        _presentedOrdinaryEarlyLesson = null;
        if (!preserveCombatContext) { _firstCombatNearby = false; ClearRecyclingSource(); }
        _descentOpportunityPhysics = ulong.MaxValue;
        CurrentInstruction = PrologueGuidanceInstruction.Hidden;
        if (GodotObject.IsInstanceValid(_formalHud)) _formalHud.SetContextualObjective(string.Empty);
        if (GodotObject.IsInstanceValid(_formalHud)) _formalHud.SetEarlyLessonCoverage(null);
        SetExpandedTeaching(false);
        _visual?.Clear();
    }

    private void SetExpandedTeaching(bool expanded)
    {
        if (_visual is WorldTargetMarkerVisual marker) marker.SetExpandedTeaching(expanded);
    }

    private void SetMarkerlessSafeEdgePolicy()
    {
        if (_visual is not WorldTargetMarkerVisual marker) return;
        SeamlessPrologueWorld world = _level.GetNodeOrNull<SeamlessPrologueWorld>(
            "WorldRoot/Sectors");
        marker.SetMarkerlessSafeEdgeEnabled(world?.LayoutVersion ==
            CanyonWorldLayout.CurrentLayoutVersion);
    }

    private void OnRecoveryCompleted(string reason)
    {
        ClearRecyclingSource();
        _ascentSupportPhysics = ulong.MaxValue;
        if (reason is not ("FALL RESET" or "DEATH RESET") || !_level.CurrentSector.HasValue) return;
        if (_observedSector == _level.CurrentSector.Value) SameSectorFailureCount++;
        else
        {
            _observedSector = _level.CurrentSector.Value;
            SameSectorFailureCount = 1;
        }
        Refresh();
    }

    private static IReadOnlyDictionary<StringName, string> ResolveInputMapGlyphs(GameLanguage language)
    {
        var result = new Dictionary<StringName, string>();
        foreach (StringName action in GuidanceActions)
        {
            string glyph = null;
            foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
            {
                if (inputEvent is InputEventKey key && InputGlyphResolver.TryFormatKeyboardBinding(key, language, out string text))
                {
                    glyph = text;
                    break;
                }
                if (inputEvent is InputEventMouseButton mouse)
                {
                    glyph = mouse.ButtonIndex switch
                    {
                        MouseButton.Left => "LMB",
                        MouseButton.Right => "RMB",
                        _ => $"MOUSE {mouse.ButtonIndex}"
                    };
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(glyph))
                throw new InvalidOperationException(
                    $"First-level guidance requires a live InputMap binding for '{action}'.");
            result.Add(action, glyph);
        }
        return result;
    }

    private static PrologueBossGuidanceState ResolveBossState(PrologueSector sector)
    {
        BossArenaController arena = sector.GetNodeOrNull<BossArenaController>(
            "Gameplay/BossArenaController");
        if (arena == null || !arena.IsEncounterActive) return PrologueBossGuidanceState.Inactive;
        return arena.CurrentBoss?.CurrentFrame.State == FallenBellkeeperState.Exposed
            ? PrologueBossGuidanceState.ChainExposed
            : PrologueBossGuidanceState.Active;
    }

    private void WarnMissing(PrologueSectorId sector, PrologueGuidanceTargetKey key)
    {
        string id = $"{sector}/{key}";
        if (_missingWarnings.Add(id))
            GD.PushWarning($"Guidance target '{key}' is missing in current sector '{sector}'.");
    }

    private static readonly StringName[] GuidanceActions =
    [
        InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown,
        InputActions.Jump, InputActions.Dash, InputActions.Attack, InputActions.Claw,
        InputActions.KeyboardShield, InputActions.Slot1, InputActions.Slot2,
        InputActions.Slot3, InputActions.Slot4
    ];
}
