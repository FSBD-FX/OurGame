using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;

namespace Knight.Tests.Integration;

// Local mounted mechanism regression preparation, not entry/region/combat/reward/save proof.
// Breaks caught: an enabled chain door lets ordinary input bypass it; claw resolves through
// that door; real legacy latch stops opening it; or the same mounted player cannot complete
// the already-qualified ordinary west shelf path after genuine opening.
public static partial class TemplePressureGateAccessPhysicsTests
{
    public static Task ClosedChainAccess(SceneTree tree) => AccessCore(tree, false, true, true);

    private static Node2D AssembleClosedChain(out Node2D collisionScope, out CharacterBody2D player,
        out CanyonPressurePlateGate gate, out ClosedChainObservation observation)
    {
        var root = new Node2D { Name = "Task18MountedClosedTemple", Position = Offset };
        try
        {
            var sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn").Instantiate<Node2D>();
            root.AddChild(sector); // Complete authored sector, including live mechanisms/reward/encounter/Boss.
            collisionScope = sector.GetNode<Node2D>("Gameplay");
            gate = collisionScope.GetNode<CanyonPressurePlateGate>("Mechanisms/TemplePressureGate");
            player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position = new Vector2(5770, 1080) - Offset; // Only pose write, before mount.
            root.AddChild(player);
            player.AddToGroup("player");
            gate.PlayerBody = player;
            var capsule = (CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            TestAssert.True(capsule.Height == 72 && capsule.Radius == 20, "mounted packed Player retains full real capsule");
            observation = new ClosedChainObservation(collisionScope, player, gate);
            GD.Print("TASK18_TEMPLE_CLOSED_CHAIN_SETUP scope=mounted-closed-chain-to-post-open fixture=true fullRoute=false parent=(5120,720) player=(5770,1080) poseWritesBeforeMountOnly=true completeSector=true initialChainClosed=true rewardPresent=true rewardBound=false upstreamFixture=true normalEntry=false");
            return root;
        }
        catch { root.Free(); throw; }
    }

    private sealed class ClosedChainObservation
    {
        internal readonly BellChainMechanism Chain;
        internal readonly StaticBody2D Door;
        internal readonly CollisionShape2D DoorCollision;
        internal readonly ThreadTarget2D Target;
        internal readonly LegacyThreadInteractionAdapter Adapter;
        internal readonly MechanicalClawController Claw;
        internal readonly CanyonEncounterGroup Encounter;
        internal readonly BossArenaController Boss;
        internal readonly PrologueSupplyCache Reward;
        private readonly Node2D _scope;
        private readonly CharacterBody2D _player;
        private readonly CanyonPressurePlateGate _gate;
        private readonly PrologueProgress _progress = new();
        private Exception _observerFailure;
        private bool _subscribed;
        internal string Stage = "closed";
        internal int OpenedEvents, ChainLatches;
        internal ulong ChainCast;
        internal readonly HashSet<ulong> StageCasts = new();
        internal readonly List<MechanicalClawFrame> Publications = new();

        internal ClosedChainObservation(Node2D scope, CharacterBody2D player, CanyonPressurePlateGate gate)
        {
            _scope = scope; _player = player; _gate = gate;
            Chain = scope.GetNode<BellChainMechanism>("Mechanisms/BellChain");
            Door = Chain.GetNode<StaticBody2D>("Door");
            DoorCollision = Chain.GetNode<CollisionShape2D>(Chain.DoorCollisionPath);
            Target = Chain.GetNode<ThreadTarget2D>("ThreadTarget");
            Adapter = Target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            Claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            Encounter = scope.GetNode<CanyonEncounterGroup>("Encounters/SealedCourtyardEncounter");
            Boss = scope.GetNode<BossArenaController>("BossArenaController");
            Reward = scope.GetNode<PrologueSupplyCache>("Rewards/TempleSupply");
        }

        internal void ApplyUpstreamFixture()
        {
            _progress.ActivateMechanism("canyon.v2.courtyard-crank");
            _progress.ActivateMechanism("canyon.v2.encounter.split-level");
            _scope.GetNode<CanyonSealGate>("Mechanisms/CourtyardSeal").ApplyProgress(_progress);
            Chain.ApplyProgress(_progress); // No shortcut; never pre-open the chain.
            GD.Print("TASK18_TEMPLE_CLOSED_CHAIN_UPSTREAM_FIXTURE skippedAcquisition=true marked=canyon.v2.courtyard-crank,canyon.v2.encounter.split-level sealDerivedNormally=true chainShortcut=false rewardPresent=true rewardBound=false flowBound=false saveAccess=false fullRoute=false");
        }

        internal void Subscribe()
        {
            Chain.Opened += OnOpened;
            Claw.ClawFrameChanged += OnFrame;
            _subscribed = true;
        }

        private void OnOpened(StringName shortcut)
        {
            try
            {
                TestAssert.True(Stage == "chain" && shortcut.ToString() == "canyon.v2.temple-bell-gate",
                    "only genuine requested chain interaction emits the authored Opened event");
                OpenedEvents++;
                TestAssert.True(OpenedEvents == 1, "mounted chain emits Opened exactly once");
            }
            catch (Exception exception) { _observerFailure ??= exception; }
        }

        private void OnFrame(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (_observerFailure != null) return;
            try
            {
                AssertContext(_player.GetNode<HealthComponent>("Health").MaxHealth);
                Publications.Add(frame);
                if (frame.CastId != 0) StageCasts.Add(frame.CastId);
                if (Stage is "beam" or "roof")
                    TestAssert.True(target == null && frame.LockedTargetRuntimeId == 0 &&
                        frame.State is not (MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing),
                        "sampled closed-door casts publish no attachment or beam lock");
                if (Stage == "chain" && frame.CastId != 0)
                {
                    if (ChainCast == 0) ChainCast = frame.CastId;
                    TestAssert.True(frame.CastId == ChainCast, "genuine chain cast has one independent identity");
                    if (frame.State == MechanicalClawState.Latched)
                    {
                        ChainLatches++;
                        TestAssert.True(ChainLatches == 1 && target == Target &&
                            frame.LockedTargetRuntimeId == Target.GetInstanceId(),
                            "one actual chain latch publication names the authored ThreadTarget");
                    }
                }
                if (Stage == "opened") AssertOpened(_player, Health(_player));
                if (Stage is "beam" or "roof" or "chain")
                    GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_PUBLICATION stage={Stage} target={target?.GetPath()} {WestClaw(frame)}");
            }
            catch (Exception exception) { _observerFailure = exception; }
        }

        internal void BeginStage(string stage) { ThrowIfFailed(); Stage = stage; StageCasts.Clear(); Publications.Clear(); }
        internal void ThrowIfFailed()
        {
            if (_observerFailure != null) throw new InvalidOperationException("Mounted closed-chain observer failed", _observerFailure);
        }
        internal void AssertContext(int hp)
        {
            TestAssert.True(Health(_player) == hp && hp == _player.GetNode<HealthComponent>("Health").MaxHealth,
                "mounted case preserves real full HP without healing");
            AssertNoEmbedding(_player);
            TestAssert.True(!Encounter.IsActive && !Encounter.IsComplete && Encounter.LiveActorCount == 0 &&
                !Encounter.IsArrivalWarningActive && !Boss.IsEncounterActive && GodotObject.IsInstanceValid(Boss.CurrentBoss),
                $"local mounted input leaves live encounter and Boss inactive: stage={Stage} encounter={Encounter.IsActive} complete={Encounter.IsComplete} actors={Encounter.LiveActorCount} boss={Boss.IsEncounterActive} player={_player.GlobalPosition}");
            TestAssert.True(Reward.IsInsideTree() && !Reward.IsSpent && !_progress.BellChainShortcutOpen && !_progress.BossDefeated,
                "live unbound reward remains present; no shortcut/save/combat fixture completion");
        }
        internal void AssertClosed(int hp)
        {
            ThrowIfFailed(); AssertContext(hp);
            TestAssert.True(!Chain.IsOpen && !DoorCollision.Disabled && OpenedEvents == 0 && Adapter.ApplicationCount == 0 &&
                !_gate.HasOpened && !_gate.IsOpen && Bounds(_player).End.X <= 5805 + .5f,
                "every closed sample remains wholly west without chain opening, embedding, crossing or plate activation");
        }
        internal void AssertOpened(CharacterBody2D player, int hp)
        {
            ThrowIfFailed(); AssertContext(hp);
            TestAssert.True(player == _player && Chain.IsOpen && DoorCollision.Disabled && Adapter.ApplicationCount == 1 &&
                OpenedEvents == 1 && ChainLatches == 1 && ChainCast != 0 && !ChainDoorRayHits(player, Door),
                "same mounted chain stays physically open with exactly one genuine application/latch/event");
        }
        internal void UnsubscribeAndThrow()
        {
            if (_subscribed) { Chain.Opened -= OnOpened; Claw.ClawFrameChanged -= OnFrame; _subscribed = false; }
            ThrowIfFailed();
        }
    }

    private static async Task ClosedChainPrelude(SceneTree tree, Node2D scope, CharacterBody2D player,
        CanyonPressurePlateGate gate, ClosedChainObservation observation, int hp)
    {
        observation.Subscribe();
        observation.AssertClosed(hp);
        Rect2 doorRect = TerrainRectangle(observation.DoorCollision);
        Rect2 beamRect = TerrainRectangle(scope.GetNode<CollisionShape2D>("Collision/TempleSupplyGripBeam/Collision"));
        Rect2 floorRect = TerrainRectangle(scope.GetNode<CollisionShape2D>("Collision/CheckpointFloor/Collision"));
        Rect2 ceilingRect = TerrainRectangle(scope.GetNode<CollisionShape2D>("Collision/TempleSupplyCeiling/Collision"));
        TestAssert.True(doorRect == new Rect2(5805, 850, 50, 240) && beamRect == new Rect2(5805, 820, 50, 40) &&
            doorRect.Intersection(beamRect).Size == new Vector2(50, 10) && floorRect.Position.Y == 1116 &&
            ceilingRect.End.Y == 760 && floorRect.Position.Y - doorRect.End.Y == 26 &&
            beamRect.Position.Y - ceilingRect.End.Y == 60 && observation.Target.WorldAnchor == new Vector2(5680, 890),
            "post-Ready literal chain door/beam overlap, floor, ceiling and target match authored mount");
        TestAssert.True(Contact(player, scope.GetNode<StaticBody2D>("Collision/CheckpointFloor"), true) &&
            !gate.HasOpened && ChainDoorRayHits(player, observation.Door), "initial real CheckpointFloor supports player west of physically closed chain");
        GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_GEOMETRY door={doorRect} beam={beamRect} intersection={doorRect.Intersection(beamRect)} floorTop=1116 ceilingBottom=760 capsuleHeight=72 radius=20 undergap=26 roofGap=60 arithmeticOnly=true {Describe(player, gate)}");
        int jumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
        int dashes = player.GetNode<PlayerMotor>("Motor").AcceptedDashCount;
        bool sideContact = false;
        int physicalSupportSamples = 0, oldSlideMissingSamples = 0;
        ActionKey(player, Key.D, true);
        try
        {
            for (int i = 0; i < 90; i++)
            {
                await Frame(tree); observation.AssertClosed(hp);
                sideContact |= ChainSideContact(player, observation.Door);
                bool oldFloor = OriginalFloor(scope, player);
                AssertClosedWalkPhysicalSupport(scope, player, i, oldFloor,
                    i == 0 || (!oldFloor && oldSlideMissingSamples == 0) || i == 89);
                physicalSupportSamples++;
                if (!oldFloor) oldSlideMissingSamples++;
            }
        }
        finally { ActionKey(player, Key.D, false); }
        await Frame(tree, 6); observation.AssertClosed(hp);
        TestAssert.True(sideContact &&
            player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumps &&
            player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == dashes,
            "bounded actual D contacts closed Door west face without jump/dash or crossing");
        GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_WALK_OK budget=90 sideContact=true noCrossing=true physicalSupportSamples={physicalSupportSamples} oldSlideMissingSamples={oldSlideMissingSamples} {Describe(player, gate)}");

        bool airborne = false;
        ActionKey(player, Key.D, true); ActionKey(player, Key.Space, true);
        try
        {
            for (int i = 0; i < 90; i++)
            {
                await Frame(tree);
                if (i == 2) ActionKey(player, Key.Space, false); // One press held for exactly three frames.
                observation.AssertClosed(hp);
                airborne |= !player.IsOnFloor();
                TestAssert.True(player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == dashes &&
                    !observation.Claw.CurrentFrame.BoostConsumed, "closed ordinary jump uses no dash/Boost");
            }
        }
        finally { ActionKey(player, Key.Space, false); ActionKey(player, Key.D, false); }
        await Frame(tree, 6); observation.AssertClosed(hp);
        TestAssert.True(airborne && OriginalFloor(scope, player) &&
            player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumps + 1,
            "one accepted actual ordinary jump goes airborne and returns lower floor without bypass");
        GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_JUMP_OK budget=90 acceptedJumps=1 airborne=true noCrossing=true {Describe(player, gate)}");

        Vector2 naturalPose = player.GlobalPosition;
        await ClosedTerrainAttempt(tree, scope, player, gate, observation, hp, "beam", new Vector2(5828, 840), naturalPose);
        await ClosedTerrainAttempt(tree, scope, player, gate, observation, hp, "roof", new Vector2(5828, 740), naturalPose);
        observation.BeginStage("chain");
        TestAssert.True(observation.Target.WorldAnchor == new Vector2(5680, 890), "genuine cast uses actual offset chain WorldAnchor");
        await SelectGrappleAndCast(tree, player, observation.Target.WorldAnchor);
        for (int i = 0; i < 120 && (!observation.Chain.IsOpen || !observation.DoorCollision.Disabled); i++)
        {
            await Frame(tree); observation.ThrowIfFailed(); observation.AssertContext(hp);
        }
        observation.AssertOpened(player, hp);
        TestAssert.True(observation.StageCasts.Count == 1, "only chain-target cast identity counted, independently of failed attempts");
        await WaitNaturalIdle(tree, scope, player, observation, hp, false);
        observation.BeginStage("opened");
        GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_OPEN_OK scope=mounted-closed-chain-to-post-open genuineInput=true latches={observation.ChainLatches} applications={observation.Adapter.ApplicationCount} openedEvents={observation.OpenedEvents} chainCast={observation.ChainCast} doorDisabled=true physicalQueryClear=true rewardPresent=true rewardBound=false fullRoute=false {Describe(player, gate)}");
        await Walk(tree, player, 5820, 90, () => observation.AssertOpened(player, hp));
        TestAssert.True(OriginalFloor(scope, player) && player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumps + 1 &&
            player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == dashes,
            "same full capsule reaches original post-open approach by real D only");
    }

    private static void AssertClosedWalkPhysicalSupport(Node2D scope, CharacterBody2D player,
        int sample, bool oldFloor, bool log)
    {
        var floor = scope.GetNode<StaticBody2D>("Collision/CheckpointFloor");
        Rect2 floorRect = TerrainRectangle(floor.GetNode<CollisionShape2D>("Collision"));
        Rect2 bounds = Bounds(player);
        float footY = bounds.End.Y;
        bool supported = floorRect.Position.Y == 1116 && MathF.Abs(footY - floorRect.Position.Y) <= .1f &&
            bounds.Position.X >= floorRect.Position.X && bounds.End.X <= floorRect.End.X &&
            MathF.Abs(player.Velocity.Y) <= .01f;
        float[] probeX = { player.GetNode<CollisionShape2D>("BodyCollider").GlobalPosition.X,
            bounds.Position.X + 1, bounds.End.X - 1 };
        var witnesses = new List<string>(3);
        for (int ray = 0; ray < probeX.Length; ray++)
        {
            Vector2 from = new(probeX[ray], footY - .25f), to = new(probeX[ray], footY + 1);
            using var query = PhysicsRayQueryParameters2D.Create(from, to, CombatCollisionLayers.World);
            query.CollideWithBodies = true; query.CollideWithAreas = false;
            using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            Node collider = hit.Count == 0 ? null : hit["collider"].AsGodotObject() as Node;
            Vector2 point = hit.Count == 0 ? Vector2.Zero : hit["position"].AsVector2();
            Vector2 normal = hit.Count == 0 ? Vector2.Zero : hit["normal"].AsVector2();
            bool raySupports = collider == floor && normal.Y <= -.99f && MathF.Abs(point.Y - footY) <= .1f;
            supported &= raySupports;
            witnesses.Add($"ray={ray} from={from} to={to} hit={hit.Count != 0} collider={collider?.GetPath()} id={collider?.GetInstanceId()} point={point} normal={normal} support={raySupports}");
        }
        string diagnostic = $"sample={sample} IsOnFloor={player.IsOnFloor()} oldOriginalFloor={oldFloor} contacts={Contacts(player)} position={player.GlobalPosition} velocity={player.Velocity} bounds={bounds} floor={floor.GetPath()} floorId={floor.GetInstanceId()} floorRect={floorRect} physicalSupport={supported} {string.Join(';', witnesses)}";
        if (log || !supported) GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_PHYSICAL_SUPPORT {diagnostic}");
        TestAssert.True(supported, $"every closed D sample has same-frame exact CheckpointFloor support from all three real foot rays: {diagnostic}");
    }

    private static async Task ClosedTerrainAttempt(SceneTree tree, Node2D scope, CharacterBody2D player,
        CanyonPressurePlateGate gate, ClosedChainObservation observation, int hp, string stage, Vector2 aim, Vector2 naturalPose)
    {
        observation.BeginStage(stage);
        observation.AssertClosed(hp);
        TestAssert.True(OriginalFloor(scope, player) && player.GlobalPosition.DistanceTo(naturalPose) < .5f,
            "both sampled no-bypass aims begin at the same naturally supported west pose");
        using var query = PhysicsRayQueryParameters2D.Create(player.GlobalPosition, aim,
            CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget);
        query.CollideWithBodies = true; query.CollideWithAreas = true;
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        Node first = hit.Count == 0 ? null : hit["collider"].AsGodotObject() as Node;
        var resolution = new MechanicalClawTargetResolver().Resolve(player, aim);
        GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_FIRST_HIT stage={stage} aim={aim} collider={first?.GetPath()} point={(hit.Count == 0 ? Vector2.Zero : hit["position"].AsVector2())} snapshot={resolution.Snapshot} actualPose={player.GlobalPosition}");
        TestAssert.True(first == observation.Door && resolution.Surface == null && resolution.Snapshot.Exists &&
            !resolution.Snapshot.HasLineOfSight && resolution.Snapshot.RuntimeId == observation.Door.GetInstanceId(),
            "actual first-hit unprofiled closed Door yields obstructed terrain snapshot, never inferred from NoTarget");
        await SelectGrappleAndCast(tree, player, aim);
        await WaitNaturalIdle(tree, scope, player, observation, hp, true);
        bool obstructed = false;
        int aimPublications = 0;
        foreach (var frame in observation.Publications)
        {
            if (frame.State == MechanicalClawState.Aim) aimPublications++;
            obstructed |= frame.State == MechanicalClawState.Failed && frame.Failure == MechanicalClawFailure.Obstructed;
        }
        TestAssert.True(aimPublications == 1 && observation.StageCasts.Count <= 1 && obstructed && OriginalFloor(scope, player) &&
            player.GlobalPosition.DistanceTo(naturalPose) < .5f,
            "one real failed cast publishes Obstructed, not NoTarget, and naturally returns Idle at unchanged west support");
        string marker = stage == "beam" ? "TASK18_TEMPLE_CLOSED_CHAIN_BEAM_ATTEMPT_OK" : "TASK18_TEMPLE_CLOSED_CHAIN_ROOF_ATTEMPT_OK";
        GD.Print($"{marker} aim={aim} firstCollider={first.GetPath()} rejection=Obstructed attachment=false applications=0 openedEvents=0 sampledOnly=true exhaustiveProof=false {Describe(player, gate)}");
    }

    private static async Task SelectGrappleAndCast(SceneTree tree, CharacterBody2D player, Vector2 aim)
    {
        ActionKey(player, Key.E, true); await Frame(tree); ActionKey(player, Key.E, false);
        TestAssert.True(player.GetNode<PlayerActionModeController>("ActionModes").CanBeginGrapple,
            "real E selects grapple for each sampled closed-chain cast");
        ulong sequence = player.GetNode<PlayerInputReader>("Input").MouseMotionSequence;
        Mouse(player, true, aim); await Frame(tree); Mouse(player, false, aim);
        TestAssert.True(player.GetNode<PlayerInputReader>("Input").MouseMotionSequence > sequence &&
            player.GetNode<PlayerInputReader>("Input").CurrentFrame.MouseWorld.DistanceTo(aim) < 1,
            "real viewport LMB/motion drives mounted claw with literal world aim");
    }

    private static async Task WaitNaturalIdle(SceneTree tree, Node2D scope, CharacterBody2D player,
        ClosedChainObservation observation, int hp, bool closed)
    {
        for (int i = 0; i < 120; i++)
        {
            await Frame(tree);
            if (closed) observation.AssertClosed(hp); else observation.AssertOpened(player, hp);
            if (observation.Claw.CurrentFrame.State == MechanicalClawState.Idle && OriginalFloor(scope, player)) return;
        }
        throw new InvalidOperationException($"Bounded real Idle/lower support missing stage={observation.Stage} {Claw(player)} contacts={Contacts(player)}");
    }

    private static Rect2 TerrainRectangle(CollisionShape2D collision)
    {
        TestAssert.True(collision.Shape is RectangleShape2D && collision.GlobalRotation == 0 && collision.GlobalScale == Vector2.One,
            "mounted authored terrain rectangle is axis-aligned and unscaled");
        Vector2 size = ((RectangleShape2D)collision.Shape).Size;
        return new Rect2(collision.GlobalPosition - size / 2, size);
    }

    private static bool ChainSideContact(CharacterBody2D player, StaticBody2D door)
    {
        for (int i = 0; i < player.GetSlideCollisionCount(); i++)
        {
            var hit = player.GetSlideCollision(i);
            if (hit.GetCollider() == door && hit.GetNormal().X < -.7f) return true;
        }
        return false;
    }

    private static bool ChainDoorRayHits(CharacterBody2D player, StaticBody2D door)
    {
        using var query = PhysicsRayQueryParameters2D.Create(new Vector2(5780, 970), new Vector2(5880, 970), CombatCollisionLayers.World);
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        return hit.Count != 0 && hit["collider"].AsGodotObject() == door;
    }
}
