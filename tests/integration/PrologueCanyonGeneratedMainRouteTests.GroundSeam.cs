using System;
using Godot;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Passive evidence on the original whole route, never another movement owner.
    private sealed partial class Task10GroundSeamEvidence : IDisposable
    {
        private readonly record struct SeamFeet(Vector2 Near, Vector2 Far, bool NearStance, bool FarStance);
        private readonly record struct SeamSample(ulong Tick, ulong Owner, Vector2 Root,
            PrologueSectorId Sector, float Phase, SeamFeet Feet, bool Walking,
            ulong Publication, ulong Process, bool InPhysics, double? PresentationTime,
            ulong Motor = 0, ulong Revision = 0);
        private readonly record struct ClockBoundary(char Kind, ulong Tick, ulong Process, ulong Publication,
            bool InPhysics, Vector2 Root, bool InsideTree, bool Processing, bool CanProcess,
            bool Paused, int Flow, double? PresentationTime);
        private readonly PrologueLevelController _level;
        private readonly SeamlessPrologueWorld _world;
        private readonly CharacterBody2D _player;
        private readonly PlayerPresentationCoordinator _coordinator;
        private readonly PlayerFormalVisual _formal;
        private readonly PlayerFormalLayer _near, _far;
        private readonly SceneTree _tree;
        private readonly PrologueGameFlowController _flow;
        private readonly ClockBoundary[] _clockTrail = new ClockBoundary[128];
        private readonly string _variant;
        private readonly ulong _levelId, _worldId, _playerId, _physicsWorldId;
        private readonly int _observerFailures;
        private ulong _lastTick = ulong.MaxValue, _entranceId, _descentId, _leftId, _rightId;
        private int _walked, _leftSamples, _rightSamples, _handoffs, _stancePairs;
        private int _clockWrite, _clockCount;
        private ulong _publicationSequence;
        private float _maximumStep;
        private bool _committedWithSource, _disposed;
        private SeamSample? _previous;
        private Exception _failure;

        public Task10GroundSeamEvidence(PrologueLevelController level, SeamlessPrologueWorld world,
            CharacterBody2D player, string variant)
        {
            // Missing measurement functionality must fail BEFORE event ownership is acquired.
            RunMeasuredPairControls();
            RunGroundSeamPhysicsControls();
            RunPublishedPairControls();
            _level = level; _world = world; _player = player; _variant = variant;
            _levelId = level.GetInstanceId(); _worldId = world.GetInstanceId();
            _playerId = player.GetInstanceId(); _physicsWorldId = player.GetWorld2D().GetInstanceId();
            _coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
            _formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            _near = _formal.GetNode<PlayerFormalLayer>("NearLeg");
            _far = _formal.GetNode<PlayerFormalLayer>("FarLeg");
            _tree = player.GetTree();
            _flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            _observerFailures = _coordinator.FrameObserverFailureCount;
            try
            {
                _tree.ProcessFrame += ObserveIdleBoundary;
                _coordinator.FramePublished += Observe;
                InitializeNativeTrace();
            }
            catch (Exception primary)
            {
                try { Dispose(); }
                catch (Exception cleanup) { throw new AggregateException(primary, cleanup); }
                throw;
            }
        }

        private static (int Comparisons, float MaximumStep) MeasurePair(SeamFeet previous, SeamFeet current)
        {
            int comparisons = 0;
            float maximumStep = 0;
            if (previous.NearStance && current.NearStance)
            { comparisons++; maximumStep = previous.Near.DistanceTo(current.Near); }
            if (previous.FarStance && current.FarStance)
            { comparisons++; maximumStep = Mathf.Max(maximumStep, previous.Far.DistanceTo(current.Far)); }
            return (comparisons, maximumStep);
        }

        private static void RunMeasuredPairControls()
        {
            var before = new SeamFeet(new(1268, 636), new(1278, 636), true, true);
            var normal = MeasurePair(before, new(new(1268, 636), new(1278, 636), true, true));
            var teleport = MeasurePair(before, new(new(1272, 636), new(1278, 636), true, true));
            var noStance = MeasurePair(new(new(1268, 636), new(1278, 636), true, false),
                new(new(1268, 636), new(1278, 636), false, true));
            TestAssert.True(normal.Comparisons == 2 && normal.MaximumStep == 0,
                "WORLD_GROUND_SEAM_CONTROL_RED: literal stationary stance requires two measured feet (test-only kernel)");
            TestAssert.True(teleport.Comparisons == 2 && teleport.MaximumStep == 4 && teleport.MaximumStep > .15f,
                "WORLD_GROUND_SEAM_CONTROL_RED: literal 4px planted-foot teleport must be rejected");
            TestAssert.True(noStance.Comparisons == 0 && noStance.MaximumStep == 0,
                "WORLD_GROUND_SEAM_CONTROL_RED: opposite stance feet provide no comparable pair");
            GD.Print("WORLD_GROUND_SEAM_MEASURE_CONTROLS_OK positive=1 negative=2 native=false");
        }

        private void Observe(PlayerPresentationFrame frame)
        {
            if (_failure != null || _disposed) return;
            try
            {
                _publicationSequence++;
                RecordClockBoundary('P', frame.PresentationTimeSeconds);
                ulong tick = Engine.GetPhysicsFrames();
                bool newPhysicsTick = tick != _lastTick;
                _lastTick = tick;
                TestAssert.True(_formal.CurrentPlayerFrame == frame,
                    "WORLD_GROUND_SEAM: published frame must be the exact current formal Draw frame");
                Vector2 root = _player.GlobalPosition;
                if (root.X < 1100 || root.X > 1420 || Mathf.Abs(root.Y - 600) > 2)
                { _previous = null; return; }
                AssertIdentity();
                var left = ReadFloor(PrologueSectorId.CanyonEntrance, "BridgeFarBank", new(400, 636, 880, 84));
                var right = ReadFloor(PrologueSectorId.CanyonDescent, "RiftUpperFloor", new(1280, 636, 620, 24));
                if (_entranceId == 0)
                {
                    _entranceId = left.Sector; _descentId = right.Sector;
                    _leftId = left.Shape; _rightId = right.Shape;
                }
                TestAssert.True(left.Sector == _entranceId && right.Sector == _descentId &&
                    left.Shape == _leftId && right.Shape == _rightId && _leftId != _rightId,
                    "WORLD_GROUND_SEAM: actual mounted sector/shape instances must not be substituted");
                var gait = frame.Gait;
                bool walking = _player.IsOnFloor() && _player.Velocity.X > 80 &&
                    gait.Observed && gait.Weight >= .999f && !frame.AirJet.Active && !frame.OrdinaryAir.Active &&
                    frame.Action.State == PlayerActionState.None &&
                    frame.Locomotion.State is PlayerLocomotionState.Run or PlayerLocomotionState.StartRun;
                var now = new SeamSample(tick, walking ? ActualFloorShape() : 0, root,
                    _world.CurrentSector, gait.Phase,
                    new(_near.ToGlobal(_near.LegSoleLocal), _far.ToGlobal(_far.LegSoleLocal),
                        gait.NearStance, gait.FarStance), walking, _publicationSequence,
                    Engine.GetProcessFrames(), Engine.IsInPhysicsFrame(), frame.PresentationTimeSeconds,
                    _nativeMotorId, _nativeMotor.MotionAuthorityRevision);
                if (walking)
                {
                    TestAssert.True(now.Owner == _leftId || now.Owner == _rightId,
                        $"WORLD_GROUND_SEAM: upward native slide contact must name one live floor shape owner={now.Owner}");
                    AssertDraw(now.Feet, gait);
                    // Every actual P remains a predecessor, but extra P within one
                    // physics tick must not inflate the original native sample gate.
                    if (newPhysicsTick)
                    {
                        _walked++;
                        if (now.Owner == _leftId)
                        {
                            if (_leftSamples++ == 0) Receipt("source", now);
                        }
                        else
                        {
                            if (_rightSamples++ == 0) Receipt("destination", now);
                        }
                    }
                    if (_previous is { Walking: true } old && old.Owner == _leftId && now.Owner == _rightId)
                    {
                        // The user-approved joint contract uses adjacent actual P,
                        // with independently complete native steps, never invented poses.
                        Receipt("handoff-before", old); Receipt("handoff-after", now);
                        PrintClockTrail(old, now);
                        var native = PrintNativeHandoff(old, now);
                        var measured = ValidatePublishedSeamHandoff(old, now, native,
                            _nativeMotorId, _nativeMotor.MotionAuthorityRevision, _leftId, _rightId);
                        GD.Print($"WORLD_GROUND_SEAM_JOINT_PAIR variant={_variant} complete={measured.Complete} " +
                            $"physics_gap={now.Tick-old.Tick} publication_gap={now.Publication-old.Publication} " +
                            $"native_before={measured.NativeBefore} native_after={measured.NativeAfter} " +
                            $"common_stance={measured.Comparisons} sole_step={measured.MaximumStep:R} reason={measured.Reason} " +
                            "policy=adjacent-publications-complete-native-v1 missing_visuals_fabricated=false pixels=false");
                        TestAssert.True(measured.Complete,
                            $"WORLD_GROUND_SEAM_INSUFFICIENT: joint proof rejected {measured.Reason}; " +
                            $"ticks={old.Tick}->{now.Tick} publications={old.Publication}->{now.Publication} sole={measured.MaximumStep}");
                        _handoffs++; _stancePairs += measured.Comparisons;
                        _maximumStep = Mathf.Max(_maximumStep, measured.MaximumStep);
                    }
                }
                // Sector commit may follow the first actual destination contact by a physics tick.
                // ReadFloor above proves the ORIGINAL source is still live only in this local window.
                if (!_committedWithSource && _rightSamples > 0 && _world.CurrentSector == PrologueSectorId.CanyonDescent)
                { _committedWithSource = true; Receipt("descent-commit-source-retained", now); }
                _previous = now;
            }
            catch (Exception error) { _failure ??= error; } // Coordinator intentionally isolates observers.
        }

        private void ObserveIdleBoundary()
        {
            if (_failure != null || _disposed) return;
            try
            {
                // SceneTree.ProcessFrame is BEFORE node _Process, not a completed presentation.
                RecordClockBoundary('I', GodotObject.IsInstanceValid(_coordinator)
                    ? _coordinator.LastPlayerFrame.PresentationTimeSeconds : null);
            }
            catch (Exception error) { _failure ??= error; }
        }

        private void RecordClockBoundary(char kind, double? presentationTime)
        {
            if (!Live(_player)) return;
            Vector2 root = _player.GlobalPosition;
            if (root.X < 1100 || root.X > 1420 || Mathf.Abs(root.Y - 600) > 2) return;
            bool inside = GodotObject.IsInstanceValid(_coordinator) && _coordinator.IsInsideTree();
            _clockTrail[_clockWrite] = new(kind, Engine.GetPhysicsFrames(), Engine.GetProcessFrames(),
                _publicationSequence, Engine.IsInPhysicsFrame(), root, inside,
                inside && _coordinator.IsProcessing(), inside && _coordinator.CanProcess(), _tree.Paused,
                GodotObject.IsInstanceValid(_flow) ? (int)_flow.State : -1, presentationTime);
            _clockWrite = (_clockWrite + 1) % _clockTrail.Length;
            _clockCount = Math.Min(_clockCount + 1, _clockTrail.Length);
        }

        private void PrintClockTrail(SeamSample old, SeamSample now)
        {
            GD.Print($"WORLD_GROUND_SEAM_CLOCK_PAIR variant={_variant} old_tick={old.Tick} now_tick={now.Tick} " +
                $"old_process={old.Process} now_process={now.Process} old_publication={old.Publication} " +
                $"now_publication={now.Publication} samples={_clockCount} capacity={_clockTrail.Length} " +
                "I_is_pre_node=true P_is_post_formal=true passive=true");
            int first = (_clockWrite - _clockCount + _clockTrail.Length) % _clockTrail.Length;
            for (int i = 0; i < _clockCount; i++)
            {
                ClockBoundary sample = _clockTrail[(first + i) % _clockTrail.Length];
                GD.Print($"WORLD_GROUND_SEAM_CLOCK_SAMPLE variant={_variant} ordinal={i} kind={sample.Kind} " +
                    $"tick={sample.Tick} process={sample.Process} publication={sample.Publication} " +
                    $"in_physics={sample.InPhysics} root={sample.Root} inside_tree={sample.InsideTree} " +
                    $"processing={sample.Processing} can_process={sample.CanProcess} paused={sample.Paused} " +
                    $"flow={sample.Flow} has_time={sample.PresentationTime.HasValue} " +
                    $"presentation_time={sample.PresentationTime.GetValueOrDefault(-1d):R}");
            }
        }

        private void AssertIdentity()
        {
            TestAssert.True(Live(_level) && Live(_world) && Live(_player) &&
                _level.GetInstanceId() == _levelId && _world.GetInstanceId() == _worldId &&
                _player.GetInstanceId() == _playerId && ReferenceEquals(_level.Player, _player) &&
                ReferenceEquals(_level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors"), _world) &&
                ReferenceEquals(_player.GetParent(), _level.GetNode("PlayerRoot")) &&
                _level.GetNode("PlayerRoot").GetChildCount() == 1 &&
                _player.GetWorld2D().GetInstanceId() == _physicsWorldId &&
                _world.GetWorld2D().GetInstanceId() == _physicsWorldId &&
                _level.CurrentSector == _world.CurrentSector &&
                _world.TryGetMountedSector(_world.CurrentSector, out var current) &&
                ReferenceEquals(_level.CurrentSectorRoot, current) && ReferenceEquals(current.GetParent(), _world),
                "WORLD_GROUND_SEAM: original level/player/native world and committed sector root must agree");
        }

        private (ulong Sector, ulong Shape) ReadFloor(PrologueSectorId id, string name, Rect2 literal)
        {
            TestAssert.True(_world.TryGetMountedSector(id, out var sector) && Live(sector) &&
                ReferenceEquals(sector.GetParent(), _world) && sector.ProcessMode != Node.ProcessModeEnum.Disabled,
                $"WORLD_GROUND_SEAM: actual mounted sector must be live {id}");
            var body = sector.GetNode<StaticBody2D>($"Gameplay/Collision/{name}");
            var shape = body.GetNode<CollisionShape2D>("Collision");
            TestAssert.True(Live(body) && Live(shape) && ReferenceEquals(shape.GetParent(), body) &&
                body.GetShapeOwners().Length == 1 &&
                body.ShapeOwnerGetShapeCount((uint)body.GetShapeOwners()[0]) == 1 &&
                !body.IsShapeOwnerDisabled((uint)body.GetShapeOwners()[0]) &&
                ReferenceEquals(body.ShapeOwnerGetOwner((uint)body.GetShapeOwners()[0]), shape) &&
                body.ShapeOwnerGetShape((uint)body.GetShapeOwners()[0], 0) == shape.Shape &&
                body.ProcessMode != Node.ProcessModeEnum.Disabled && !shape.Disabled &&
                (body.CollisionLayer & _player.CollisionMask) != 0 && shape.Shape is RectangleShape2D &&
                body.GetWorld2D().GetInstanceId() == _physicsWorldId,
                $"WORLD_GROUND_SEAM: real gameplay body has one enabled native rectangle {id}/{name}");
            var rectangle = (RectangleShape2D)shape.Shape; // Borrowed Resource; never disposed by evidence.
            Vector2 a = shape.ToGlobal(-rectangle.Size * .5f), b = shape.ToGlobal(rectangle.Size * .5f);
            TestAssert.True(shape.GlobalTransform.X.IsEqualApprox(Vector2.Right) &&
                shape.GlobalTransform.Y.IsEqualApprox(Vector2.Down) && new Rect2(a, b - a).IsEqualApprox(literal),
                $"WORLD_GROUND_SEAM: actual saved world rectangle differs {id}/{name} rect={new Rect2(a, b - a)}");
            return (sector.GetInstanceId(), shape.GetInstanceId());
        }

        private ulong ActualFloorShape()
        {
            for (int i = 0; i < _player.GetSlideCollisionCount(); i++)
            {
                var collision = _player.GetSlideCollision(i);
                if (collision.GetNormal().Dot(Vector2.Up) >= .7f &&
                    collision.GetColliderShape() is CollisionShape2D shape)
                {
                    TestAssert.True(Live(shape), "WORLD_GROUND_SEAM: contact must still reference a live shape");
                    return shape.GetInstanceId();
                }
            }
            return 0;
        }

        private void AssertDraw(SeamFeet feet, PlayerGroundGaitFrame gait)
        {
            foreach (var leg in new[] { _near, _far })
            {
                TestAssert.Near(18, leg.LegHipLocal.DistanceTo(leg.LegKneeLocal), .002f,
                    "WORLD_GROUND_SEAM_DRAW: fixed 18px upper leg");
                TestAssert.Near(18, leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal), .002f,
                    "WORLD_GROUND_SEAM_DRAW: fixed 18px lower leg");
            }
            if (feet.NearStance) TestAssert.True(feet.Near.DistanceTo(gait.NearSoleWorld) <= .05f,
                "WORLD_GROUND_SEAM_DRAW: actual near stance sole matches same-frame gait");
            if (feet.FarStance) TestAssert.True(feet.Far.DistanceTo(gait.FarSoleWorld) <= .05f,
                "WORLD_GROUND_SEAM_DRAW: actual far stance sole matches same-frame gait");
        }

        private static bool Live(Node node) => GodotObject.IsInstanceValid(node) &&
            node.IsInsideTree() && !node.IsQueuedForDeletion();

        private void Receipt(string stage, SeamSample sample) => GD.Print(
            $"WORLD_GROUND_SEAM_SAMPLE stage={stage} variant={_variant} level={_levelId} world={_worldId} " +
            $"physics_world={_physicsWorldId} player={_playerId} tick={sample.Tick} owner={sample.Owner} " +
            $"sector={sample.Sector} root={sample.Root} phase={sample.Phase} near={sample.Feet.Near} " +
            $"near_stance={sample.Feet.NearStance} far={sample.Feet.Far} far_stance={sample.Feet.FarStance} " +
            $"walking={sample.Walking} process={sample.Process} publication={sample.Publication} " +
            $"in_physics={sample.InPhysics} has_time={sample.PresentationTime.HasValue} " +
            $"presentation_time={sample.PresentationTime.GetValueOrDefault(-1d):R} motor={sample.Motor} revision={sample.Revision}");

        public void AssertComplete()
        {
            if (_failure != null) throw new InvalidOperationException(
                $"WORLD_GROUND_SEAM: cached observer failure variant={_variant}", _failure);
            TestAssert.True(!_disposed && _coordinator.FrameObserverFailureCount == _observerFailures,
                "WORLD_GROUND_SEAM: no observer fault may be swallowed or bypassed by disposal");
            TestAssert.True(_leftSamples > 0 && _rightSamples > 0 && _walked >= 10 &&
                _handoffs > 0 && _stancePairs > 0 && _maximumStep <= .15f && _committedWithSource,
                $"WORLD_GROUND_SEAM_INSUFFICIENT: left={_leftSamples} right={_rightSamples} walk={_walked} " +
                $"handoff={_handoffs} stance={_stancePairs} max={_maximumStep} source_retained={_committedWithSource}");
            AssertNativeTraceComplete();
            GD.Print($"WORLD_GROUND_SEAM_OK variant={_variant} display={DisplayServer.GetName()} native_world=true " +
                $"context=original-whole-route-after-completion world={_worldId} player={_playerId} " +
                $"left={_leftSamples} right={_rightSamples} walk={_walked} handoff={_handoffs} " +
                $"stance={_stancePairs} max_world_sole_step={_maximumStep} descent_source_retained=true " +
                "policy=adjacent-publications-complete-native-v1 actual_p_predecessor=true pixels=false");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var failures = new System.Collections.Generic.List<Exception>();
            void Clean(Action action)
            {
                try { action(); }
                catch (Exception error) { failures.Add(error); }
            }
            Clean(CloseNativeTrace);
            Clean(() => { if (GodotObject.IsInstanceValid(_tree)) _tree.ProcessFrame -= ObserveIdleBoundary; });
            Clean(() => { if (GodotObject.IsInstanceValid(_coordinator)) _coordinator.FramePublished -= Observe; });
            if (failures.Count > 0) throw new AggregateException("Ground seam evidence cleanup failed", failures);
        }
    }
}
