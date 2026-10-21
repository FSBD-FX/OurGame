using Godot;
using System.Collections.Generic;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Boss;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Ranged;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
using Knight.Interactions;
using Knight.Mechanisms;
using Knight.Prologue.Traversal;

namespace Knight.Presentation.Fog;

public readonly record struct FogOcclusionObservation(HeadlampOcclusionFrame Frame,
    Vector2 ActorWorld, ulong PhysicsFrame, ulong WorldInstanceId);

/// <summary>
/// Display-only bridge from the live level and resolved lamp to the fog canvas.
/// Display only after the current frame and every required protected consumer are ready.
/// </summary>
public partial class PrologueFogController : Node
{
    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath CanvasPath { get; set; } = new("../Fog/Canvas");

    public ulong PresentedFrameCount { get; private set; }
    public event System.Action<FogOcclusionObservation> ObservationPresented;
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    public Vector2 LampWorldPosition { get; private set; }
    public PrologueFogProfile CurrentProfile { get; private set; } = PrologueFogProfile.Exploration;

    private PrologueLevelController _level;
    private PrologueFogCanvas _canvas;
    private Node2D _world;
    private HeadlampOcclusionSampler _sampler;
    private FogFragmentReadabilityOverlay _fragmentCues;
    private readonly List<QuestFragment> _fragments = new();
    private FogReadabilityOverlay _attackCues;
    private FogInteractionReadabilityOverlay _interactionCue;
    private FogTerrainReadabilityOverlay _terrainCues;
    private FogSteamReadabilityOverlay _steamCues;
    private FogShieldLaneReadabilityOverlay _laneCues;
    private FogPressureGateReadabilityOverlay _gateCues;
    private FogMovingGeometryReadabilityOverlay _movingCues;
    private FogBarrierReadabilityOverlay _barrierCues;
    private readonly List<StaticBody2D> _barriers = new();
    private readonly List<AnimatableBody2D> _movingBodies = new();
    private readonly List<CanyonPressureGateVisual> _gates = new();
    private readonly List<CanyonShieldLaneHazard> _lanes = new();
    private readonly List<CanyonSteamJet> _steam = new();
    private readonly List<CanyonPlatformVisual> _terrain = new();
    private readonly List<AttackTelegraphVisual> _attacks = new();
    private Vector2 _direction = Vector2.Right;
    private bool _hasDirection;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _level = GetNode<PrologueLevelController>(LevelPath);
        _canvas = GetNode<PrologueFogCanvas>(CanvasPath);
        _fragmentCues = GetNodeOrNull<FogFragmentReadabilityOverlay>("../ProtectedCues/Fragments");
        _attackCues = GetNodeOrNull<FogReadabilityOverlay>("../ProtectedCues/Attacks");
        _interactionCue = GetNodeOrNull<FogInteractionReadabilityOverlay>("../ProtectedCues/Interaction");
        _terrainCues = GetNodeOrNull<FogTerrainReadabilityOverlay>("../ProtectedCues/Terrain");
        _steamCues = GetNodeOrNull<FogSteamReadabilityOverlay>("../ProtectedCues/Steam");
        _laneCues = GetNodeOrNull<FogShieldLaneReadabilityOverlay>("../ProtectedCues/ShieldLane");
        _gateCues = GetNodeOrNull<FogPressureGateReadabilityOverlay>("../ProtectedCues/PressureGates");
        _movingCues = GetNodeOrNull<FogMovingGeometryReadabilityOverlay>("../ProtectedCues/MovingBodies");
        _barrierCues = GetNodeOrNull<FogBarrierReadabilityOverlay>("../ProtectedCues/Barriers");
        _canvas.Visible = false;
        RenderingServer.FramePreDraw += OnFramePreDraw;
    }

    public override void _Process(double delta)
    {
        _canvas.Visible = false;
        if (Live(_fragmentCues)) _fragmentCues.Clear();
        if (Live(_attackCues)) _attackCues.Clear();
        if (Live(_interactionCue)) _interactionCue.Clear();
        if (Live(_terrainCues)) _terrainCues.Clear();
        if (Live(_steamCues)) _steamCues.Clear();
        if (Live(_laneCues)) _laneCues.Clear();
        if (Live(_gateCues)) _gateCues.Clear();
        if (Live(_movingCues)) _movingCues.Clear();
        if (Live(_barrierCues)) _barrierCues.Clear();
        if (!Playing() || !Live(_level.Player)) return;
        PlayerInputReader input = _level.Player.GetNodeOrNull<PlayerInputReader>("Input");
        if (!Live(input)) return;
        Vector2 target = input.KeyboardAimDirection;
        if (!HeadlampOcclusionSampler.Finite(target) || target.IsZeroApprox()) return;
        if (!_hasDirection)
        {
            _direction = target.Normalized();
            _hasDirection = true;
        }
        else if (double.IsFinite(delta) && delta > 0d)
        {
            // Angle interpolation also handles a 180-degree turn without a zero
            // vector midway; exponential damping is independent of frame rate.
            float weight = (float)(1d - System.Math.Exp(-10d * delta));
            _direction = Vector2.FromAngle(Mathf.LerpAngle(_direction.Angle(), target.Angle(), weight));
        }
    }

    private void OnFramePreDraw()
    {
        if (!Live(this) || !Live(_canvas)) return;
        _canvas.Visible = false;
        if (Live(_fragmentCues)) _fragmentCues.Clear();
        if (Live(_attackCues)) _attackCues.Clear();
        if (Live(_interactionCue)) _interactionCue.Clear();
        if (Live(_terrainCues)) _terrainCues.Clear();
        if (Live(_steamCues)) _steamCues.Clear();
        if (Live(_laneCues)) _laneCues.Clear();
        if (Live(_gateCues)) _gateCues.Clear();
        if (Live(_movingCues)) _movingCues.Clear();
        if (Live(_barrierCues)) _barrierCues.Clear();
        if (!Playing() || !Live(_level.Player)) return;
        PlayerPresentationCoordinator presentation =
            _level.Player.GetNodeOrNull<PlayerPresentationCoordinator>("Presentation");
        if (!Live(presentation) || presentation.ActiveVisual is not PlayerFormalVisual formal || !Live(formal)) return;
        Node2D world = _level.GetNodeOrNull<Node2D>("WorldRoot");
        if (!Live(world)) return;
        if (world != _world)
        {
            _sampler?.Dispose();
            _world = world;
            _sampler = new HeadlampOcclusionSampler(world);
        }

        // Read after every presentation _Process, including late pose/camera
        // changes. Never cache Formal, a retired sector, or an old door snapshot.
        LampWorldPosition = formal.LanternWorldPosition;
        LastOcclusionFrame = _sampler.Capture();
        Transform2D pixelToWorld =
            (GetViewport().GetFinalTransform() * world.GetCanvasTransform()).AffineInverse();
        CurrentProfile = PrologueFogModel.ResolveProfile(_level.Player.GlobalPosition);
        // ApplySettings replaces its normalized data object; read the live
        // option for every submission rather than retaining startup settings.
        bool reducedFog = _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.Settings.ReducedFog == true;
        _canvas.Present(_level.Player.GlobalPosition, LampWorldPosition, _direction,
            pixelToWorld, LastOcclusionFrame, reducedFog);
        PresentMountedFragments(world);
        PresentMountedAttacks(world);
        PresentMountedTerrain(world);
        PresentMountedSteam(world);
        PresentMountedLanes(world);
        PresentMountedGates(world);
        PresentMountedMovingBodies(world);
        PresentMountedBarriers(world);
        if (Live(_interactionCue)) _interactionCue.PresentInteraction(
            _level.Player.GetNodeOrNull<EnvironmentInteractionController>("EnvironmentInteraction"), LastOcclusionFrame);
        // Initial/paused/retired frames remain hidden. Missing presentation
        // protection must leave the world bright, never conceal a required cue.
        // A zero cue count is normal (no nearby threat), not a missing consumer.
        _canvas.Visible = _canvas.FrameValid && ProtectedConsumersReady();
        PresentedFrameCount++;
        // Borrow the existing capture synchronously; subscribers add no snapshot
        // or physics query and must independently gate gameplay authorization.
        ObservationPresented?.Invoke(new FogOcclusionObservation(LastOcclusionFrame,
            _level.Player.GlobalPosition, Engine.GetPhysicsFrames(), world.GetInstanceId()));
    }

    private bool ProtectedConsumersReady() =>
        Protected(_fragmentCues) && Protected(_attackCues) && Protected(_interactionCue) &&
        Protected(_terrainCues) && Protected(_steamCues) && Protected(_laneCues) &&
        Protected(_gateCues) && Protected(_movingCues) && Protected(_barrierCues);

    private static bool Protected(Node2D consumer) => Live(consumer) && consumer.IsVisibleInTree();

    private void PresentMountedAttacks(Node2D world)
    {
        _attacks.Clear();
        if (!Live(_attackCues)) return;
        if (world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is { } sectors && Live(sectors))
        {
            foreach (PrologueSectorId id in sectors.MountedSectorIds)
            {
                if (!sectors.TryGetMountedSector(id, out PrologueSector sector) || !Live(sector)) continue;
                Node actors = sector.GetNodeOrNull<Node>("Gameplay/Actors");
                if (Live(actors))
                    for (int index = 0; index < actors.GetChildCount(); index++)
                    {
                        Node actor = actors.GetChild(index);
                        if (actor is RustShellSquire or BellBackLaborer or GraveLampMoth or BellBoltSentry or CanyonSkitter or RangedWeaponEnemy)
                            AddLiveAttack(actor);
                    }

                // The arena owns the current boss identity; retired/replaced
                // bosses and similarly named leaves elsewhere are not sources.
                BossArenaController arena = sector.GetNodeOrNull<BossArenaController>("Gameplay/BossArenaController");
                Node bossRoot = sector.GetNodeOrNull<Node>("Gameplay/BossRoot");
                if (Live(arena) && Live(bossRoot) && Live(arena.CurrentBoss) &&
                    arena.CurrentBoss.GetParent() == bossRoot) AddLiveAttack(arena.CurrentBoss);
            }
        }
        // One fresh batch, including empty: no last-source-wins erasure and no
        // retained warning after streaming, cancellation, pause or boss reset.
        _attackCues.PresentAttacks(_attacks, _level.Player.GlobalPosition, LastOcclusionFrame);
    }

    private void AddLiveAttack(Node actor)
    {
        if (!Live(actor)) return;
        if (actor is RangedWeaponEnemy ranged)
        {
            ranged.AppendAuthorizedAttackLeaves(_attacks);
            return;
        }
        AttackTelegraphVisual leaf = actor.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        if (Live(leaf)) _attacks.Add(leaf);
    }

    private void PresentMountedFragments(Node2D world)
    {
        _fragments.Clear();
        if (!Live(_fragmentCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors ||
            !Live(sectors)) return;
        // Include mounted neighbors at sector boundaries, not global scene groups
        // or a retained current-sector cache. Catalog size is nine stable IDs.
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
        {
            if (!sectors.TryGetMountedSector(id, out PrologueSector sector) || !Live(sector)) continue;
            Node container = sector.GetNodeOrNull<Node>("Gameplay/QuestFragments");
            if (!Live(container)) continue;
            for (int index = 0; index < container.GetChildCount(); index++)
                if (container.GetChild(index) is QuestFragment fragment) _fragments.Add(fragment);
        }
        _fragmentCues.PresentFragments(_fragments, _level.Player.GlobalPosition, LastOcclusionFrame);
    }

    private void PresentMountedTerrain(Node2D world)
    {
        _terrain.Clear();
        if (!Live(_terrainCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors ||
            !Live(sectors)) return;
        // Authored static support skins only. The overlay independently proves
        // the paired live native body and current snapshot membership; neither
        // nearby decorations nor a retired neighbor authorize an edge cue.
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
        {
            if (!sectors.TryGetMountedSector(id, out PrologueSector sector) || !Live(sector)) continue;
            Node readable = sector.GetNodeOrNull<Node>("VisualRoot/GameplayReadable");
            if (!Live(readable)) continue;
            for (int index = 0; index < readable.GetChildCount(); index++)
                if (readable.GetChild(index) is CanyonPlatformVisual platform) _terrain.Add(platform);
        }
        _terrainCues.PresentTerrain(_terrain, _level.Player.GlobalPosition, LastOcclusionFrame);
    }

    private bool Playing() => Live(_level) && !GetTree().Paused &&
        _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State == PrologueGameFlowState.Playing;

    private void PresentMountedSteam(Node2D world)
    {
        _steam.Clear();
        if (!Live(_steamCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors ||
            !Live(sectors)) return;
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
        {
            if (!sectors.TryGetMountedSector(id,out PrologueSector sector) || !Live(sector)) continue;
            Node mechanisms=sector.GetNodeOrNull<Node>("Gameplay/Mechanisms");
            if (!Live(mechanisms)) continue;
            for (int index=0;index<mechanisms.GetChildCount();index++)
            {
                Node source=mechanisms.GetChild(index);
                if (source is CanyonSteamJet jet) _steam.Add(jet);
                else if (source is CanyonSteamDiverter pair && Live(pair))
                {
                    CanyonSteamJet left=pair.GetNodeOrNull<CanyonSteamJet>(pair.LeftJetPath);
                    CanyonSteamJet right=pair.GetNodeOrNull<CanyonSteamJet>(pair.RightJetPath);
                    if (Live(left)) _steam.Add(left);
                    if (Live(right)) _steam.Add(right);
                }
            }
        }
        // Single current-frame batch; the display consumer independently checks
        // each original native owner and current disarmed/warning/active state.
        _steamCues.PresentSteam(_steam,_level.Player.GlobalPosition,LastOcclusionFrame);
    }

    private void PresentMountedLanes(Node2D world)
    {
        _lanes.Clear();
        if (!Live(_laneCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors ||
            !Live(sectors)) return;
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
            if (sectors.TryGetMountedSector(id,out PrologueSector sector) && Live(sector) &&
                sector.GetNodeOrNull<CanyonShieldLaneHazard>("Gameplay/ShieldLane") is { } lane && Live(lane))
                _lanes.Add(lane);
        // Source owns damage/pulse timing. This exact fresh frame only protects
        // visible native edges of the mounted running body-polling source.
        _laneCues.PresentLanes(_lanes,_level.Player.GlobalPosition,LastOcclusionFrame);
    }

    private void PresentMountedGates(Node2D world)
    {
        _gates.Clear();
        if (!Live(_gateCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors ||
            !Live(sectors)) return;
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
        {
            if (!sectors.TryGetMountedSector(id, out PrologueSector sector) || !Live(sector)) continue;
            Node mechanisms = sector.GetNodeOrNull<Node>("Gameplay/Mechanisms");
            if (!Live(mechanisms)) continue;
            for (int index = 0; index < mechanisms.GetChildCount(); index++)
                if (mechanisms.GetChild(index) is CanyonPressurePlateGate gate && Live(gate) &&
                    gate.GetNodeOrNull<CanyonPressureGateVisual>("ReadableGate") is { } visual && Live(visual))
                    _gates.Add(visual);
        }
        // Paint and countdown come from the actual mounted original visual;
        // the overlay never substitutes a phase clock or opens/closes a door.
        _gateCues.PresentGates(_gates, _level.Player.GlobalPosition, LastOcclusionFrame);
    }

    private void PresentMountedMovingBodies(Node2D world)
    {
        _movingBodies.Clear();
        if (!Live(_movingCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors ||
            !Live(sectors)) return;
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
        {
            if (!sectors.TryGetMountedSector(id, out PrologueSector sector) || !Live(sector)) continue;
            Node mechanisms = sector.GetNodeOrNull<Node>("Gameplay/Mechanisms");
            if (!Live(mechanisms)) continue;
            for (int index = 0; index < mechanisms.GetChildCount(); index++)
                if (mechanisms.GetChild(index) is AnimatableBody2D body &&
                    body is CanyonCounterweightLift or CanyonGearPiston or CanyonPendulumHazard) _movingBodies.Add(body);
        }
        // Current native solidity, not generic HazardActive or a duplicated
        // movement clock, authorizes these neutral visible face strips.
        _movingCues.PresentBodies(_movingBodies, _level.Player.GlobalPosition, LastOcclusionFrame);
    }

    private void PresentMountedBarriers(Node2D world)
    {
        _barriers.Clear();
        if (!Live(_barrierCues) || world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors") is not { } sectors || !Live(sectors)) return;
        foreach (PrologueSectorId id in sectors.MountedSectorIds)
        {
            if (!sectors.TryGetMountedSector(id,out PrologueSector sector) || !Live(sector)) continue;
            foreach (string path in FogBarrierReadabilityOverlay.SourcePaths)
                if (sector.GetNodeOrNull<StaticBody2D>(path) is { } body && Live(body)) _barriers.Add(body);
        }
        // Native committed shapes, not the mechanism's pending visual/open
        // state, decide which of these original current doors is still solid.
        _barrierCues.PresentBarriers(_barriers,_level.Player.GlobalPosition,LastOcclusionFrame);
    }

    private static bool Live(Node node) => IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();

    public override void _ExitTree()
    {
        RenderingServer.FramePreDraw -= OnFramePreDraw;
        if (IsInstanceValid(_interactionCue)) _interactionCue.ClearAndRelease();
        if (IsInstanceValid(_terrainCues)) _terrainCues.Clear();
        if (IsInstanceValid(_steamCues)) _steamCues.Clear();
        if (IsInstanceValid(_laneCues)) _laneCues.Clear();
        if (IsInstanceValid(_gateCues)) _gateCues.Clear();
        if (IsInstanceValid(_movingCues)) _movingCues.Clear();
        if (IsInstanceValid(_barrierCues)) _barrierCues.Clear();
        _sampler?.Dispose();
        _sampler = null;
        _world = null;
        _canvas = null;
        _level = null;
        _fragmentCues = null;
        _attackCues = null;
        _interactionCue = null;
        _terrainCues = null;
        _steamCues = null;
        _laneCues = null;
        _gateCues = null;
        _movingCues = null;
        _barrierCues = null;
        _fragments.Clear();
        _attacks.Clear();
        _terrain.Clear();
        _steam.Clear();
        _lanes.Clear();
        _gates.Clear();
        _movingBodies.Clear();
        _barriers.Clear();
        LastOcclusionFrame = null;
        _hasDirection = false;
        RequestReady();
    }
}
