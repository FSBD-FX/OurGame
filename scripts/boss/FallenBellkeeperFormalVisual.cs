using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Boss;

public partial class FallenBellkeeperFormalVisual : Node2D, IEntityVisual2D
{
    private readonly Dictionary<string, Node2D> _parts = new();
    private readonly Dictionary<string, Vector2> _restPositions = new();
    private readonly Dictionary<string, float> _restRotations = new();
    private FallenBellkeeperFrame _frame = new(FallenBellkeeperState.Dormant, FallenBellkeeperAttack.None,
        false, true, false, false);
    private FallenBellkeeper _attackSource;
    private string _baseAnimation = "dormant";
    private float _overrideSeconds;
    private float _phase;
    private float _attackProgress;
    private Vector2 _hammerThrowFromPosition;
    private float _hammerThrowFromRotation;
    private float _hammerThrowBlend = 1f;
    private bool _hammerThrowBoundaryActive;
    private ulong _hammerThrowLastBlendFrame = ulong.MaxValue;
    private Vector2 _hammerThrowExitArmorFromPosition;
    private float _hammerThrowExitArmorFromRotation;
    private Vector2 _hammerThrowExitChainFromPosition;
    private float _hammerThrowExitChainFromRotation;
    private float _hammerThrowExitArmorBlend = 1f;
    private bool _hammerThrowExitArmorBoundaryActive;
    private ulong _hammerThrowExitArmorLastBlendFrame = ulong.MaxValue;
    private Vector2 _sweepBoundaryHammerFromPosition;
    private float _sweepBoundaryHammerFromRotation;
    private Vector2 _sweepBoundaryArmorFromPosition;
    private float _sweepBoundaryArmorFromRotation;
    private float _sweepBoundaryHammerBlend = 1f;
    private bool _sweepBoundaryActive;
    private ulong _sweepBoundaryLastBlendFrame = ulong.MaxValue;
    private Vector2 _groundWaveExitHammerFromPosition;
    private float _groundWaveExitHammerFromRotation;
    private Vector2 _groundWaveExitArmorFromPosition;
    private float _groundWaveExitArmorFromRotation;
    private float _groundWaveExitHammerBlend = 1f;
    private bool _groundWaveExitHammerBoundaryActive;
    private ulong _groundWaveExitLastBlendFrame = ulong.MaxValue;
    private readonly Dictionary<string, PoseTransform2D> _phaseStarts = new();

    public string CurrentAnimation { get; private set; } = "dormant";
    public string CurrentAttackPose { get; private set; } = "boss_none";
    public AttackPresentationFrame CurrentAttackFrame { get; private set; }
    public float CurrentContactIntensity { get; private set; }

    public override void _Ready()
    {
        _attackSource = GetParent()?.GetParent() as FallenBellkeeper;
        if (_attackSource != null) _attackSource.AttackFrameChanged += PresentAttack;
        EnsureParts();
        ApplyPose();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_attackSource)) _attackSource.AttackFrameChanged -= PresentAttack;
    }

    private void EnsureParts()
    {
        if (_parts.Count > 0) return;
        foreach (string name in new[] { "Body", "ArmorShell", "Hammer", "Chain", "Core" })
        {
            Node2D part = GetNode<Node2D>(name);
            _parts.Add(name, part);
            _restPositions.Add(name, part.Position);
            _restRotations.Add(name, part.Rotation);
        }
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        if (_overrideSeconds > 0f)
        {
            _overrideSeconds = Mathf.Max(0f, _overrideSeconds - (float)delta);
            if (Mathf.IsZeroApprox(_overrideSeconds)) CurrentAnimation = _baseAnimation;
        }
        ApplyPose();
    }

    public void ApplyBossFrame(FallenBellkeeperFrame frame, int facing)
    {
        bool crossesHammerThrowBoundary = IsHammerThrowVisualState(frame.State) !=
            IsHammerThrowVisualState(_frame.State);
        if (crossesHammerThrowBoundary && !_hammerThrowBoundaryActive)
            StartHammerThrowBoundary();
        _frame = frame;
        _baseAnimation = frame.State switch
        {
            FallenBellkeeperState.Dormant => "dormant",
            FallenBellkeeperState.Intro => "intro",
            FallenBellkeeperState.PhaseOne => "open_phase",
            FallenBellkeeperState.Transition => "transition",
            FallenBellkeeperState.PhaseTwoArmored => "armor_closed",
            FallenBellkeeperState.HammerTelegraph => "hammer_telegraph",
            FallenBellkeeperState.HammerThrown => "hammer_thrown",
            FallenBellkeeperState.Exposed => "exposed",
            FallenBellkeeperState.Defeated => "defeated",
            _ => "dormant"
        };
        if (frame.State == FallenBellkeeperState.Defeated)
        {
            _overrideSeconds = 0f;
            CurrentAnimation = _baseAnimation;
        }
        else if (_overrideSeconds <= 0f)
        {
            CurrentAnimation = _baseAnimation;
        }
        CurrentAttackPose = ResolveAttackPose(frame);
        if (frame.AttackTimeline.Phase != AttackPhase.None &&
            (CurrentAttackFrame.Timeline.AttackId != frame.AttackTimeline.AttackId ||
             CurrentAttackFrame.Timeline.Phase != frame.AttackTimeline.Phase))
            PresentAttack(new AttackPresentationFrame(frame.AttackTimeline, default));
        Scale = new Vector2(facing < 0 ? -1f : 1f, 1f);
        ApplyPose();
    }

    public void PresentAttack(AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase != AttackPhase.None &&
            frame.Timeline.Kind != EnemyAttackKind.BossHammerThrow)
        {
            _hammerThrowBoundaryActive = false;
            _hammerThrowExitArmorBoundaryActive = false;
        }
        if (frame.Timeline.Phase != AttackPhase.None &&
            frame.Timeline.Kind != EnemyAttackKind.BossSweep)
            _sweepBoundaryActive = false;
        if (frame.Timeline.Phase == AttackPhase.None &&
            CurrentAttackFrame.Timeline.Kind == EnemyAttackKind.BossGroundWave)
            StartGroundWaveExitHammerBoundary();
        if (frame.Timeline.Phase == AttackPhase.None &&
            CurrentAttackFrame.Timeline.Kind == EnemyAttackKind.BossHammerThrow)
        {
            StartHammerThrowExitBoundary();
            StartHammerThrowBoundary();
        }
        if (frame.Timeline.Kind == EnemyAttackKind.BossHammerThrow &&
            frame.Timeline.Phase == AttackPhase.Telegraph &&
            CurrentAttackFrame.Timeline.Phase != AttackPhase.Telegraph)
            StartHammerThrowBoundary();
        if (frame.Timeline.AttackId != CurrentAttackFrame.Timeline.AttackId &&
            frame.Timeline.Kind == EnemyAttackKind.BossSweep)
        {
            EnsureParts();
            Node2D hammer = _parts["Hammer"];
            Node2D armor = _parts["ArmorShell"];
            _sweepBoundaryHammerFromPosition = hammer.Position;
            _sweepBoundaryHammerFromRotation = hammer.Rotation;
            _sweepBoundaryArmorFromPosition = armor.Position;
            _sweepBoundaryArmorFromRotation = armor.Rotation;
            _sweepBoundaryHammerBlend = 0f;
            _sweepBoundaryActive = true;
            _sweepBoundaryLastBlendFrame = ulong.MaxValue;
        }
        if (frame.Timeline.AttackId != CurrentAttackFrame.Timeline.AttackId)
            _phaseStarts.Clear();
        else if (_parts.Count > 0 &&
            frame.Timeline.Phase != CurrentAttackFrame.Timeline.Phase)
            CapturePhaseStarts();
        else if (frame.Timeline.Phase != CurrentAttackFrame.Timeline.Phase)
            _phaseStarts.Clear();
        CurrentAttackFrame = frame;
        _attackProgress = Progress(frame.Timeline);
        CurrentContactIntensity = Contact(frame.Timeline, _attackProgress);
        CurrentAttackPose = ResolveAttackPose(frame.Timeline.Kind, frame.Timeline.Phase);
        ApplyPose();
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        FallenBellkeeperState state = frame.State switch
        {
            EntityVisualState.Dead => FallenBellkeeperState.Defeated,
            EntityVisualState.Attack => FallenBellkeeperState.PhaseOne,
            EntityVisualState.Hurt => FallenBellkeeperState.Exposed,
            _ => FallenBellkeeperState.Dormant
        };
        ApplyBossFrame(new FallenBellkeeperFrame(state, FallenBellkeeperAttack.None, false,
            state == FallenBellkeeperState.Dormant, false, false), frame.Facing);
    }

    public void PlayCue(PresentationCue cue)
    {
        CurrentAnimation = cue switch
        {
            PresentationCue.WeakpointOpened => "chain_pull",
            PresentationCue.Hurt => "recovery",
            PresentationCue.PhaseChanged => "transition",
            PresentationCue.Died => "defeated",
            _ => CurrentAnimation
        };
        _overrideSeconds = cue switch
        {
            PresentationCue.WeakpointOpened => 0.2f,
            PresentationCue.Hurt => 0.13f,
            PresentationCue.PhaseChanged => 0.25f,
            PresentationCue.Died => 0.8f,
            _ => _overrideSeconds
        };
        ApplyPose();
    }

    private void ApplyPose()
    {
        EnsureParts();
        foreach ((string name, Node2D part) in _parts)
        {
            part.Position = _restPositions[name];
            part.Rotation = _restRotations[name];
            part.Visible = true;
            part.Modulate = Colors.White;
        }

        Node2D body = _parts["Body"];
        Node2D armor = _parts["ArmorShell"];
        Node2D hammer = _parts["Hammer"];
        Node2D chain = _parts["Chain"];
        Node2D core = _parts["Core"];
        float weight = Mathf.Sin(_phase * 2.6f);
        body.Position += new Vector2(0f, weight * 1.2f);
        armor.Position += new Vector2(0f, weight * 0.6f);
        chain.Visible = false;
        core.Visible = false;

        switch (CurrentAnimation)
        {
            case "dormant":
                body.Rotation = -0.08f;
                hammer.Rotation = 0.4f;
                break;
            case "intro":
                body.Rotation = -0.14f;
                armor.Rotation = -0.2f;
                hammer.Rotation = -0.42f;
                break;
            case "open_phase":
                armor.Position += new Vector2(-35f, -5f);
                armor.Rotation = -0.42f;
                core.Visible = true;
                core.Modulate = new Color(1f, 1f, 1f, 0.55f);
                hammer.Rotation = -0.16f + weight * 0.04f;
                break;
            case "transition":
                armor.Position += new Vector2(-12f, -22f);
                armor.Rotation = -0.82f;
                hammer.Rotation = -0.75f;
                core.Visible = true;
                break;
            case "armor_closed":
                hammer.Rotation = -0.12f;
                break;
            case "hammer_telegraph":
                body.Rotation = -0.18f;
                hammer.Position += new Vector2(-8f, -16f);
                hammer.Rotation = -1.05f;
                break;
            case "hammer_thrown":
                hammer.Position = new Vector2(84f, -55f);
                hammer.Rotation = 0.3f + _phase * 1.8f;
                chain.Visible = true;
                chain.Position = new Vector2(28f, -52f);
                break;
            case "chain_pull":
                hammer.Position = new Vector2(68f, -38f);
                hammer.Rotation = -0.2f;
                chain.Visible = true;
                chain.Position = new Vector2(15f, -45f);
                chain.Rotation = -0.3f;
                armor.Position += new Vector2(-22f, -14f);
                armor.Rotation = -0.5f;
                break;
            case "exposed":
                armor.Position += new Vector2(-60f, -25f);
                armor.Rotation = -0.95f;
                hammer.Position += new Vector2(24f, 18f);
                hammer.Rotation = 0.92f;
                core.Visible = true;
                break;
            case "recovery":
                body.Rotation = -0.3f;
                armor.Position += new Vector2(-44f, -8f);
                armor.Rotation = -0.64f;
                core.Visible = true;
                core.Modulate = new Color(1f, 0.72f, 0.65f);
                break;
            case "defeated":
                body.Position += new Vector2(16f, 48f);
                body.Rotation = 1.35f;
                armor.Position += new Vector2(-32f, 52f);
                armor.Rotation = -1.2f;
                hammer.Position += new Vector2(42f, 56f);
                hammer.Rotation = 1.1f;
                chain.Visible = true;
                chain.Position += new Vector2(18f, 58f);
                break;
        }
        ApplyAttackPose(body, armor, hammer, chain, core);
        BlendHammerThrowBoundary(hammer);
        BlendSweepBoundary(armor, hammer);
        BlendGroundWaveExitBoundary(armor, hammer);
        BlendHammerThrowExitBoundary(armor, chain);
        foreach (Node2D part in _parts.Values)
            if (part is FallenBellkeeperFormalPart formalPart) formalPart.SetContactIntensity(CurrentContactIntensity);
    }

    private void BlendHammerThrowBoundary(Node2D hammer)
    {
        if (!_hammerThrowBoundaryActive) return;
        ulong processFrame = Engine.GetProcessFrames();
        if (processFrame != _hammerThrowLastBlendFrame)
        {
            _hammerThrowBlend = Mathf.Min(1f, _hammerThrowBlend + .125f);
            _hammerThrowLastBlendFrame = processFrame;
        }
        float weight = Smooth(_hammerThrowBlend);
        hammer.Position = _hammerThrowFromPosition.Lerp(hammer.Position, weight);
        hammer.Rotation = Mathf.LerpAngle(_hammerThrowFromRotation, hammer.Rotation, weight);
        if (Mathf.IsEqualApprox(_hammerThrowBlend, 1f)) _hammerThrowBoundaryActive = false;
    }

    private static bool IsHammerThrowVisualState(FallenBellkeeperState state) =>
        state is FallenBellkeeperState.HammerTelegraph or FallenBellkeeperState.HammerThrown;

    private void BlendSweepBoundary(Node2D armor, Node2D hammer)
    {
        if (!_sweepBoundaryActive) return;
        ulong processFrame = Engine.GetProcessFrames();
        if (processFrame != _sweepBoundaryLastBlendFrame)
        {
            _sweepBoundaryHammerBlend = Mathf.Min(1f, _sweepBoundaryHammerBlend + .125f);
            _sweepBoundaryLastBlendFrame = processFrame;
        }
        float weight = Smooth(_sweepBoundaryHammerBlend);
        hammer.Position = _sweepBoundaryHammerFromPosition.Lerp(hammer.Position, weight);
        hammer.Rotation = Mathf.LerpAngle(_sweepBoundaryHammerFromRotation, hammer.Rotation, weight);
        armor.Position = _sweepBoundaryArmorFromPosition.Lerp(armor.Position, weight);
        armor.Rotation = Mathf.LerpAngle(_sweepBoundaryArmorFromRotation, armor.Rotation, weight);
        if (Mathf.IsEqualApprox(_sweepBoundaryHammerBlend, 1f)) _sweepBoundaryActive = false;
    }

    private void BlendGroundWaveExitBoundary(Node2D armor, Node2D hammer)
    {
        if (!_groundWaveExitHammerBoundaryActive) return;
        ulong processFrame = Engine.GetProcessFrames();
        if (processFrame != _groundWaveExitLastBlendFrame)
        {
            _groundWaveExitHammerBlend = Mathf.Min(1f, _groundWaveExitHammerBlend + .125f);
            _groundWaveExitLastBlendFrame = processFrame;
        }
        float weight = Smooth(_groundWaveExitHammerBlend);
        hammer.Position = _groundWaveExitHammerFromPosition.Lerp(hammer.Position, weight);
        hammer.Rotation = Mathf.LerpAngle(_groundWaveExitHammerFromRotation, hammer.Rotation, weight);
        armor.Position = _groundWaveExitArmorFromPosition.Lerp(armor.Position, weight);
        armor.Rotation = Mathf.LerpAngle(_groundWaveExitArmorFromRotation, armor.Rotation, weight);
        if (Mathf.IsEqualApprox(_groundWaveExitHammerBlend, 1f))
            _groundWaveExitHammerBoundaryActive = false;
    }

    private void StartGroundWaveExitHammerBoundary()
    {
        EnsureParts();
        Node2D hammer = _parts["Hammer"];
        Node2D armor = _parts["ArmorShell"];
        _groundWaveExitHammerFromPosition = hammer.Position;
        _groundWaveExitHammerFromRotation = hammer.Rotation;
        _groundWaveExitArmorFromPosition = armor.Position;
        _groundWaveExitArmorFromRotation = armor.Rotation;
        _groundWaveExitHammerBlend = 0f;
        _groundWaveExitHammerBoundaryActive = true;
        _groundWaveExitLastBlendFrame = ulong.MaxValue;
    }

    private void StartHammerThrowExitBoundary()
    {
        EnsureParts();
        Node2D armor = _parts["ArmorShell"];
        Node2D chain = _parts["Chain"];
        _hammerThrowExitArmorFromPosition = armor.Position;
        _hammerThrowExitArmorFromRotation = armor.Rotation;
        _hammerThrowExitChainFromPosition = chain.Position;
        _hammerThrowExitChainFromRotation = chain.Rotation;
        _hammerThrowExitArmorBlend = 0f;
        _hammerThrowExitArmorBoundaryActive = true;
        _hammerThrowExitArmorLastBlendFrame = ulong.MaxValue;
    }

    private void StartHammerThrowBoundary()
    {
        EnsureParts();
        Node2D hammer = _parts["Hammer"];
        _hammerThrowFromPosition = hammer.Position;
        _hammerThrowFromRotation = hammer.Rotation;
        _hammerThrowBlend = 0f;
        _hammerThrowBoundaryActive = true;
        _hammerThrowLastBlendFrame = ulong.MaxValue;
    }

    private void BlendHammerThrowExitBoundary(Node2D armor, Node2D chain)
    {
        if (!_hammerThrowExitArmorBoundaryActive) return;
        ulong processFrame = Engine.GetProcessFrames();
        if (processFrame != _hammerThrowExitArmorLastBlendFrame)
        {
            _hammerThrowExitArmorBlend = Mathf.Min(1f, _hammerThrowExitArmorBlend + .125f);
            _hammerThrowExitArmorLastBlendFrame = processFrame;
        }
        float weight = Smooth(_hammerThrowExitArmorBlend);
        armor.Position = _hammerThrowExitArmorFromPosition.Lerp(armor.Position, weight);
        armor.Rotation = Mathf.LerpAngle(_hammerThrowExitArmorFromRotation, armor.Rotation, weight);
        chain.Position = _hammerThrowExitChainFromPosition.Lerp(chain.Position, weight);
        chain.Rotation = Mathf.LerpAngle(_hammerThrowExitChainFromRotation, chain.Rotation, weight);
        if (Mathf.IsEqualApprox(_hammerThrowExitArmorBlend, 1f))
            _hammerThrowExitArmorBoundaryActive = false;
    }

    private static string ResolveAttackPose(FallenBellkeeperFrame frame) =>
        ResolveAttackPose(frame.AttackTimeline.Kind, frame.AttackTimeline.Phase);

    private static string ResolveAttackPose(EnemyAttackKind attack, AttackPhase phase) =>
        (attack, phase) switch
        {
            (EnemyAttackKind.BossSweep, AttackPhase.Telegraph) => "boss_sweep_windup",
            (EnemyAttackKind.BossSweep, AttackPhase.Active) => "boss_sweep_release",
            (EnemyAttackKind.BossLeapSlam, AttackPhase.Telegraph) => "boss_leap_crouch",
            (EnemyAttackKind.BossLeapSlam, AttackPhase.Active) => "boss_leap_impact",
            (EnemyAttackKind.BossGroundWave, AttackPhase.Telegraph) => "boss_groundwave_raise",
            (EnemyAttackKind.BossGroundWave, AttackPhase.Active) => "boss_groundwave_release",
            (EnemyAttackKind.BossHammerThrow, AttackPhase.Telegraph) => "boss_hammer_windup",
            (EnemyAttackKind.BossHammerThrow, AttackPhase.Active) => "boss_hammer_release",
            (_, AttackPhase.Recovery) => "boss_attack_recover",
            _ => "boss_none"
        };

    private void ApplyAttackPose(Node2D body, Node2D armor, Node2D hammer, Node2D chain, Node2D core)
    {
        Vector2 bodyPosition = body.Position;
        float bodyRotation = body.Rotation;
        Vector2 armorPosition = armor.Position;
        float armorRotation = armor.Rotation;
        Vector2 hammerPosition = hammer.Position;
        float hammerRotation = hammer.Rotation;
        Vector2 chainPosition = chain.Position;
        float chainRotation = chain.Rotation;
        switch (CurrentAttackPose)
        {
            case "boss_sweep_windup":
                body.Rotation -= .12f;
                armor.Position += new Vector2(-8f, 2f);
                armor.Rotation -= .08f;
                hammer.Position += new Vector2(-18f, -8f);
                hammer.Rotation = -1.22f;
                chain.Visible = true;
                chain.Position += new Vector2(-10f, -4f);
                chain.Rotation = -.16f;
                break;
            case "boss_sweep_release":
                body.Rotation += .18f;
                armor.Position += new Vector2(-14f, 5f);
                armor.Rotation += .10f;
                hammer.Position += new Vector2(38f, 10f);
                hammer.Rotation = .42f;
                chain.Visible = true;
                chain.Position += new Vector2(-4f, 8f);
                chain.Rotation = .22f;
                ApplyContactImpulse(body, armor, hammer, chain);
                break;
            case "boss_leap_crouch":
                body.Position += new Vector2(0f, 18f);
                armor.Position += new Vector2(5f, 13f);
                armor.Rotation = .10f;
                hammer.Position += new Vector2(-6f, 12f);
                hammer.Rotation = -.72f;
                chain.Visible = true;
                chain.Position += new Vector2(12f, 16f);
                chain.Rotation = .15f;
                break;
            case "boss_leap_impact":
                body.Position += new Vector2(0f, 7f);
                armor.Position += new Vector2(7f, -14f);
                armor.Rotation = -.18f;
                hammer.Position += new Vector2(12f, 30f);
                hammer.Rotation = 1.08f;
                chain.Visible = true;
                chain.Position += new Vector2(14f, -18f);
                chain.Rotation = -.30f;
                ApplyContactImpulse(body, armor, hammer, chain);
                break;
            case "boss_groundwave_raise":
                body.Rotation -= .08f;
                armor.Position += new Vector2(-4f, -11f);
                armor.Rotation = -.16f;
                hammer.Position += new Vector2(-2f, -28f);
                hammer.Rotation = -1.52f;
                chain.Visible = true;
                chain.Position += new Vector2(4f, -24f);
                chain.Rotation = -.42f;
                break;
            case "boss_groundwave_release":
                body.Rotation += .12f;
                armor.Position += new Vector2(-8f, -5f);
                armor.Rotation = .17f;
                hammer.Position += new Vector2(18f, 34f);
                hammer.Rotation = .82f;
                chain.Visible = true;
                chain.Position += new Vector2(7f, 7f);
                chain.Rotation = .34f;
                ApplyContactImpulse(body, armor, hammer, chain);
                break;
            case "boss_hammer_windup":
                body.Rotation -= .18f;
                armor.Position += new Vector2(10f, -5f);
                armor.Rotation = .16f;
                hammer.Position += new Vector2(-20f, -28f);
                hammer.Rotation = -1.32f;
                chain.Visible = true;
                chain.Position += new Vector2(21f, -20f);
                chain.Rotation = -.52f;
                break;
            case "boss_hammer_release":
                body.Position += new Vector2(-6f, 2f);
                armor.Position += new Vector2(12f, -4f);
                armor.Rotation = .24f;
                hammer.Position += new Vector2(57f, -12f);
                hammer.Rotation = .28f;
                chain.Visible = true;
                chain.Position += new Vector2(34f, -18f);
                chain.Rotation = .48f;
                ApplyContactImpulse(body, armor, hammer, chain);
                break;
            case "boss_attack_recover":
                body.Rotation = -.10f;
                armor.Position += new Vector2(-10f, -4f);
                hammer.Position += new Vector2(8f, 9f);
                hammer.Rotation = .24f;
                chain.Visible = true;
                chain.Position += new Vector2(4f, 3f);
                break;
        }

        float primary = Smooth(_attackProgress);
        float delayed = Smooth(Mathf.Clamp((_attackProgress - .16f) / .84f, 0f, 1f));
        BlendPart(body, "Body", bodyPosition, bodyRotation, primary);
        BlendPart(hammer, "Hammer", hammerPosition, hammerRotation, primary);
        BlendPart(armor, "ArmorShell", armorPosition, armorRotation, delayed);
        BlendPart(chain, "Chain", chainPosition, chainRotation, delayed);
    }

    private void ApplyContactImpulse(params Node2D[] parts)
    {
        foreach (Node2D part in parts)
            if (part is FallenBellkeeperFormalPart formalPart) formalPart.SetContactIntensity(CurrentContactIntensity);
    }

    private void BlendPart(Node2D part, string name, Vector2 fromPosition, float fromRotation, float weight)
    {
        PoseTransform2D from = _phaseStarts.TryGetValue(name, out PoseTransform2D captured)
            ? captured : new PoseTransform2D(fromPosition, fromRotation, Vector2.One, Colors.White);
        PoseTransform2D to = new(part.Position, part.Rotation, Vector2.One, Colors.White);
        PoseTransform2D result = PoseTransform2D.Interpolate(from, to, weight);
        part.Position = result.Position;
        part.Rotation = result.RotationRadians;
    }

    private void CapturePhaseStarts()
    {
        _phaseStarts.Clear();
        foreach ((string name, Node2D part) in _parts)
            _phaseStarts[name] = new PoseTransform2D(part.Position, part.Rotation, Vector2.One, part.Modulate);
    }

    private static float Progress(AttackTimelineFrame timeline) => timeline.PhaseDurationSeconds <= 0f ? 1f :
        Mathf.Clamp(1f - timeline.PhaseRemainingSeconds / timeline.PhaseDurationSeconds, 0f, 1f);
    private static float Smooth(float value) => value * value * (3f - 2f * value);
    private static float Contact(AttackTimelineFrame timeline, float progress) => timeline.Phase != AttackPhase.Active ? 0f :
        Mathf.Clamp(1f - progress / Mathf.Min(1f, .05f / Mathf.Max(.0001f, timeline.PhaseDurationSeconds)), 0f, 1f);
}
