using System;
using System.Reflection;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Expression = System.Linq.Expressions.Expression;

namespace Knight.Tests.Integration;

// A local, passive witness around the entrance and supported wall drives. Never turns an
// unobserved slide/snap/recovery into a start-to-end chord.
internal static class Task10OrdinaryWallSweepEvidence
{
    // Coordinates are confined to the local authored corridors.
    // This is for float reconciliation, not a geometric penetration waiver.
    private const float ReconcileEpsilon = .001f;

    private static Func<PlayerMotor, T> BindMotorField<T>(string name)
    {
        var field = typeof(PlayerMotor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(T)) throw new InvalidOperationException($"Missing Motor witness field {name}.");
        var argument = Expression.Parameter(typeof(PlayerMotor));
        return Expression.Lambda<Func<PlayerMotor, T>>(Expression.Field(argument, field), argument).Compile();
    }

    internal sealed class Observer : IDisposable
    {
        private static readonly Func<PlayerMotor, ulong> Completed = BindMotorField<ulong>("_lastPresentationPhysicsFrame");
        private static readonly Func<PlayerMotor, Vector2> Correction = BindMotorField<Vector2>("_grappleVelocityCorrection");
        private readonly PrologueLevelController _level;
        private readonly PrologueGameFlowController _flow;
        private readonly PrologueSector _sector;
        private readonly CharacterBody2D _player;
        private readonly PlayerMotor _motor;
        private readonly PlayerInputReader _input;
        private readonly CollisionShape2D _capsule;
        private readonly CapsuleShape2D _capsuleResource;
        private readonly uint _capsuleOwner;
        private readonly Solid[] _solids;
        private readonly ThrusterPostMotorObserver _before, _after;
        private readonly float _radius, _axisHalf, _nativeMargin;
        private readonly bool _frozen, _rift;
        private bool CorridorMode => _frozen || _rift;
        private readonly float _away;
        private string CorridorPrefix => _rift ? "RIFT_WALL_SWEEP" : "FROZEN_WALL_SWEEP";
        private readonly HealthComponent _health;
        private readonly int _frozenHealth, _frozenJumps, _frozenDashes;
        private readonly float _frozenStartX;
        private Vector2 _origin, _correction, _platform;
        private Vector2 _riftPreviousBody, _riftPreviousCapsule, _riftBodyBefore;
        private int _riftPoseChain, _riftMotorDelta;
        private ulong _tick, _sequence, _first, _last, _lastInput;
        private float _step;
        private int _samples, _reconstructed, _unknown, _substeps;
        private int _wallContacts, _ceilingContacts, _supportContacts;
        private int _frozenApproachSamples, _frozenBlockedSamples, _frozenRetreatSamples;
        private float _frozenBlockedX, _frozenRetreatDistance;
        private bool _frozenNeutral;
        private bool _closed;
        private string _failure;

        internal Observer(SceneTree tree, PrologueLevelController level, CharacterBody2D player,
            bool frozen = false, bool rift = false)
        {
            ScalarControls();
            TestAssert.True(!frozen || !rift, "supported wall observations cannot mix scopes");
            _frozen = frozen;
            _rift = rift;
            _away = rift ? -1f : 1f;
            _level = level; _player = player; _flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            _sector = level.CurrentSectorRoot;
            _motor = player.GetNode<PlayerMotor>("Motor"); _input = player.GetNode<PlayerInputReader>("Input");
            _capsule = player.GetNode<CollisionShape2D>("BodyCollider");
            _capsuleResource = _capsule.Shape as CapsuleShape2D ?? throw new InvalidOperationException("Original Player has no saved capsule.");
            _capsuleOwner = ShapeOwner(player, _capsule, _capsuleResource);
            _radius = _capsuleResource.Radius;
            _axisHalf = _capsuleResource.Height / 2f - _radius;
            _nativeMargin = player.SafeMargin;
            TestAssert.True(level.GetTree() == tree && level.Player == player && _flow.State == PrologueGameFlowState.Playing &&
                level.CurrentSector == (CorridorMode ? PrologueSectorId.CanyonDescent : PrologueSectorId.CanyonEntrance) && player.IsOnFloor() &&
                _motor.ProcessPhysicsPriority == 0 && _input.ProcessPhysicsPriority == -300 &&
                float.IsFinite(_nativeMargin) && _nativeMargin >= 0f && _nativeMargin < _radius && _axisHalf >= 0f,
                "local wall observer binds the actual supported saved Player and its physics boundary");
            _solids = frozen ? [new(_sector, "FrozenSentryRecessWall", Vector2.Right), new(_sector, "FrozenLeftCatch", Vector2.Up)] :
                rift ? [new(_sector, "RiftAlternationWall", Vector2.Left), new(_sector, "RiftMiddleCatch", Vector2.Up)] :
                [new(_sector, "EntranceOuterWall", Vector2.Right), new(_sector, "LowBridgeCeiling", Vector2.Down), new(_sector, "StartFloor", Vector2.Up)];
            if (CorridorMode)
            {
                if (frozen) FrozenScalarControls();
                if (rift) RiftScalarControls();
                _health = player.GetNode<HealthComponent>("Health");
                _frozenHealth = _health.CurrentHealth;
                _frozenJumps = _motor.AcceptedJumpCount;
                _frozenDashes = _motor.AcceptedDashCount;
                _frozenStartX = _capsule.GlobalPosition.X;
                bool savedGeometry = frozen ? HasFrozenWallSize(_solids[0].Bounds) :
                    _solids[0].Bounds == new Rect2(2200f, 400f, 36f, 700f) &&
                    _solids[1].Bounds == new Rect2(1500f, 856f, 700f, 24f);
                TestAssert.True(savedGeometry && _frozenHealth > 0 && SupportedCorridor(_capsule.GlobalPosition) &&
                    _away * (_frozenStartX - WallFaceX()) >= 12f,
                    $"{CorridorPrefix} starts fully supported beside the original saved wall, with a real approach runway");
            }
            _before = new ThrusterPostMotorObserver { Name = "OrdinaryWallBeforeMotor", ProcessMode = Node.ProcessModeEnum.Always,
                ProcessPhysicsPriority = -1, Observe = Before };
            _after = new ThrusterPostMotorObserver { Name = "OrdinaryWallAfterMotor", ProcessMode = Node.ProcessModeEnum.Always,
                ProcessPhysicsPriority = 1, Observe = After };
            if (_rift)
            {
                _riftPreviousBody = _player.GlobalPosition;
                _riftPreviousCapsule = _capsule.GlobalPosition;
            }
            level.AddChild(_before); level.AddChild(_after);
        }

        private void Before()
        {
            if (_closed || _failure != null) return;
            try
            {
                ulong frame = Engine.GetPhysicsFrames(), input = _input.CurrentFrameSequence;
                if (_tick != 0 || input == 0 || Completed(_motor) >= frame ||
                    (_last != 0 && (frame != _last + 1 || input != _lastInput + 1)) ||
                    _flow.State != PrologueGameFlowState.Playing || _level.Player != _player || _level.CurrentSectorRoot != _sector)
                { Fail($"missing, stale or foreign pre-Motor sample at {frame}/{input}"); return; }
                if (_capsule.Disabled || _player.IsShapeOwnerDisabled(_capsuleOwner) || !Upright(_capsule.GlobalTransform) ||
                    _capsule.GetParent() != _player || _capsule.Shape != _capsuleResource || _radius != _capsuleResource.Radius ||
                    _axisHalf != _capsuleResource.Height / 2f - _radius || _nativeMargin != _player.SafeMargin)
                { Fail("saved collision capsule identity, size or native margin changed"); return; }
                foreach (Solid solid in _solids) solid.AssertUnchanged(_player);
                _tick = frame; _sequence = input; _origin = _capsule.GlobalPosition;
                if (_rift)
                {
                    _riftBodyBefore = _player.GlobalPosition;
                    if (!RiftPoseMatches(_riftPreviousBody, _riftPreviousCapsule, _riftBodyBefore, _origin))
                    { Fail($"unobserved Rift displacement before {frame}: body={_riftPreviousBody}->{_riftBodyBefore} capsule={_riftPreviousCapsule}->{_origin}"); return; }
                    _riftPoseChain++;
                }
                _step = (float)_motor.GetPhysicsProcessDeltaTime();
                _correction = Correction(_motor); _platform = _player.GetPlatformVelocity();
                if (CorridorMode && !CorridorState(_origin)) Fail($"pre-Motor support or ordinary motion changed at {frame}: center={_origin}");
            }
            catch (Exception error) { Fail($"pre-Motor observation: {error}"); }
        }

        private void After()
        {
            if (_closed || _failure != null) return;
            try
            {
                ulong frame = Engine.GetPhysicsFrames();
                if (_tick != frame || _sequence != _input.CurrentFrameSequence || Completed(_motor) != frame ||
                    !float.IsFinite(_step) || _step <= 0f || !Upright(_capsule.GlobalTransform))
                { Fail($"missing or mispaired native Motor completion at {frame}/{_sequence}"); return; }
                _tick = 0;
                Vector2 end = _capsule.GlobalPosition, delta = end - _origin;
                Vector2? riftNativeDelta = null;
                if (_rift)
                {
                    Vector2 bodyEnd = _player.GlobalPosition, bodyDelta = bodyEnd - _riftBodyBefore;
                    riftNativeDelta = _player.GetPositionDelta();
                    if (!RiftMotorMatches(bodyDelta, delta, riftNativeDelta.Value))
                    { Fail($"Rift native net displacement disagrees at {frame}: body={bodyDelta} capsule={delta} native={riftNativeDelta}"); return; }
                    _riftPreviousBody = bodyEnd;
                    _riftPreviousCapsule = end;
                    _riftMotorDelta++;
                }
                if (_samples++ == 0) _first = frame;
                _last = frame; _lastInput = _sequence;
                int collisions = _player.GetSlideCollisionCount();
                if (collisions < 0 || collisions > _player.MaxSlides + 1)
                { Fail($"native slide cache exceeds the saved body bound: {collisions}"); return; }
                Vector2 reportedTravel = default, reportedRemainder = default;
                bool staticContacts = true;
                bool wallHit = false, supportHit = false;
                for (int i = 0; i < collisions; i++)
                {
                    // Borrow the just-completed native cache, then retain only scalar facts.
                    KinematicCollision2D hit = _player.GetSlideCollision(i);
                    if (hit == null) { Fail($"null slide cache item {i} at {frame}"); return; }
                    if (i == 0) { reportedTravel = hit.GetTravel(); reportedRemainder = hit.GetRemainder(); }
                    staticContacts &= hit.GetCollider() is StaticBody2D && hit.GetColliderVelocity() == Vector2.Zero;
                    if (hit.GetLocalShape() != _capsule) continue;
                    if (_solids[0].Matches(hit)) { _wallContacts++; wallHit = true; }
                    if (!CorridorMode && _solids[1].Matches(hit)) _ceilingContacts++;
                    if (_solids[^1].Matches(hit)) { _supportContacts++; supportHit = true; }
                }

                foreach (Solid solid in _solids)
                {
                    if (Penetrates(_origin, _origin, solid.Bounds, _radius, _axisHalf, _nativeMargin) ||
                        Penetrates(end, end, solid.Bounds, _radius, _axisHalf, _nativeMargin))
                    { Fail($"measured endpoint penetration tick={frame} solid={solid.Body.Name} before={_origin} after={end}"); return; }
                }
                // Wall is a one-sided local constraint; even an unsolved cache cannot
                // turn an observed full side change into a clean sampled result.
                if (!WithinWallSide(end.X, WallFaceX(), _nativeMargin, _away))
                { Fail($"actual capsule left the walkable side of the original wall at tick={frame} center={end}"); return; }

                // Motor's public two-part free velocity and the bracketed correction
                // are the values used to issue MoveAndSlide. Don't infer from velocity
                // after collision, and don't splice a multi-contact chord.
                Vector2 request = (_motor.UpdraftCandidateVelocity + _motor.UpdraftVelocityDelta + _correction) * _step;
                if (CorridorMode)
                {
                    foreach (Solid solid in _solids) solid.AssertUnchanged(_player);
                    if (!CorridorState(end)) { Fail($"post-Motor support or ordinary motion changed at {frame}: center={end}"); return; }
                    if (!supportHit && !ProbeCorridorSupport(end, out string probeFailure))
                    { Fail($"actual {_solids[1].Body.Name} shape does not support the capsule at tick={frame} center={end}: {probeFailure}"); return; }
                    ObserveCorridorDrive(frame, end, delta, request, wallHit);
                    if (_failure != null) return;
                }
                bool known = staticContacts && _platform == Vector2.Zero && _player.GetPlatformVelocity() == Vector2.Zero &&
                    _correction == Correction(_motor) && !_player.FloorConstantSpeed &&
                    Close(delta, riftNativeDelta ?? _player.GetPositionDelta()) &&
                    CanUseReportedStep(collisions, request, delta, _player.GetLastMotion(), reportedTravel, reportedRemainder);
                if (!known) { _unknown++; return; }
                _reconstructed++;
                if (delta.LengthSquared() <= ReconcileEpsilon * ReconcileEpsilon) return;
                _substeps++;
                foreach (Solid solid in _solids)
                    if (Penetrates(_origin, end, solid.Bounds, _radius, _axisHalf, _nativeMargin))
                    { Fail($"reconciled native sweep penetrates tick={frame} solid={solid.Body.Name} before={_origin} after={end}"); return; }
            }
            catch (Exception error) { Fail($"post-Motor observation: {error}"); }
        }

        private void Fail(string reason) => _failure ??= reason;

        private float WallFaceX() => CapsuleWallFaceX(_solids[0].Bounds, _radius, _away);

        private bool SupportedCorridor(Vector2 center) => WithinSupportedCorridor(center, _solids[0].Bounds,
            _solids[1].Bounds, _radius, _radius + _axisHalf, _nativeMargin, _away);

        private bool CorridorState(Vector2 center) => _level.Player == _player && _level.CurrentSectorRoot == _sector &&
            _flow.State == PrologueGameFlowState.Playing && _health.CurrentHealth == _frozenHealth &&
            _motor.AcceptedJumpCount == _frozenJumps && _motor.AcceptedDashCount == _frozenDashes && !_motor.IsDashing &&
            Correction(_motor) == Vector2.Zero && _player.IsOnFloor() && SupportedCorridor(center);

        private bool ProbeCorridorSupport(Vector2 center, out string failure)
        {
            // Snap may supply floor state without publishing an exact floor hit
            // in the slide cache. A whole-body query can conflate the nearby
            // wall and the floor. The actual capsule's bottom axis stays one
            // radius away from the wall; query the supporting surface there.
            // IsOnFloor and full-capsule geometry are checked separately.
            Vector2 foot = center + Vector2.Down * (_radius + _axisHalf);
            Vector2 reach = Vector2.Down * (.5f + _nativeMargin);
            using var query = PhysicsRayQueryParameters2D.Create(foot - reach, foot + reach, _player.CollisionMask);
            query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;
            using var hit = _player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            if (hit.Count == 0) { failure = $"no native center-foot hit at {foot}"; return false; }
            GodotObject collider = hit["collider"].AsGodotObject();
            int index = hit["shape"].AsInt32();
            Vector2 normal = hit["normal"].AsVector2(), point = hit["position"].AsVector2();
            Solid floor = _solids[1];
            bool exact = floor.Matches(collider, index, normal) && MathF.Abs(point.Y - floor.Bounds.Position.Y) <= ReconcileEpsilon;
            failure = exact ? null : $"center-foot collider={(collider as Node)?.Name.ToString()} index={index} normal={normal} point={point}";
            return exact;
        }

        private void ObserveCorridorDrive(ulong frame, Vector2 end, Vector2 delta, Vector2 request, bool wallHit)
        {
            bool a = Input.IsPhysicalKeyPressed(Key.A), d = Input.IsPhysicalKeyPressed(Key.D);
            float raw = _input.RawFrame.MoveAxis, sampled = _input.CurrentFrame.MoveAxis;
            bool press = _rift ? d && !a : a && !d;
            bool retreat = _rift ? a && !d : d && !a;
            if (!_frozenNeutral && press && _away * raw < -.99f && _away * sampled < -.99f)
            {
                if (_frozenBlockedSamples == 0 && _away * delta.X < -.1f) _frozenApproachSamples++;
                bool blocked = IsBlockedPress(end.X, WallFaceX(), delta.X, _away * request.X, wallHit);
                if (blocked && _frozenApproachSamples >= 3 && _away * (_frozenStartX - end.X) >= 12f)
                {
                    _frozenBlockedSamples++;
                    _frozenBlockedX = end.X;
                }
                else _frozenBlockedSamples = 0;
                return;
            }
            if (!a && !d && raw == 0f && sampled == 0f && _frozenBlockedSamples >= 3 &&
                _frozenRetreatSamples == 0 && !_frozenNeutral && MathF.Abs(end.X - _frozenBlockedX) <= .5f)
            { _frozenNeutral = true; return; }
            if (_frozenNeutral && retreat && _away * raw > .99f && _away * sampled > .99f &&
                _away * request.X > .05f && _away * delta.X > .05f && !wallHit)
            {
                _frozenRetreatSamples++;
                _frozenRetreatDistance = _away * (end.X - _frozenBlockedX);
                return;
            }
            Fail($"unverified physical pressure/neutral/retreat sequence at {frame}: physical={a}/{d} axes={raw}/{sampled} " +
                $"before={_origin} after={end} request={request} wallHit={wallHit}");
        }

        internal int FrozenSampleCount => CorridorSampleCount;
        internal int CorridorSampleCount
        {
            get
            {
                TestAssert.True(_failure == null, $"{CorridorPrefix}_RED: {_failure}");
                return _samples;
            }
        }
        internal bool FrozenBlocked => CorridorBlocked;
        internal bool FrozenRetreated => CorridorRetreated;
        internal bool CorridorBlocked => _frozenBlockedSamples >= 3;
        internal bool CorridorRetreated => _frozenRetreatSamples >= 3 && _frozenRetreatDistance >= 12f;

        internal void AssertFrozenComplete() => AssertCorridorComplete(rift: false);
        internal void AssertRiftComplete() => AssertCorridorComplete(rift: true);

        private void AssertCorridorComplete(bool rift)
        {
            _closed = true;
            if (_rift && (_riftPoseChain != _samples || _riftMotorDelta != _samples ||
                !RiftPoseMatches(_riftPreviousBody, _riftPreviousCapsule, _player.GlobalPosition, _capsule.GlobalPosition)))
                Fail($"Rift motion chain incomplete at exit: pose={_riftPoseChain} motor={_riftMotorDelta} samples={_samples}");
            TestAssert.True((rift ? _rift : _frozen) && _failure == null && _tick == 0 && _reconstructed > 0 && _substeps > 0 &&
                _samples == _reconstructed + _unknown && _last - _first + 1 == (ulong)_samples &&
                _wallContacts >= _frozenBlockedSamples && _supportContacts > 0 && CorridorBlocked && _frozenNeutral && CorridorRetreated,
                $"{CorridorPrefix}_RED: samples={_samples} reconstructed={_reconstructed} unknown={_unknown} substeps={_substeps} " +
                $"contacts={_wallContacts}/{_supportContacts} approach={_frozenApproachSamples} blocked={_frozenBlockedSamples} " +
                $"neutral={_frozenNeutral} retreat={_frozenRetreatSamples}/{_frozenRetreatDistance:R}: {_failure}");
            GD.Print($"{CorridorPrefix}_OK samples={_samples} reconstructed={_reconstructed} unknown={_unknown} substeps={_substeps} " +
                $"wall_contacts={_wallContacts} support_contacts={_supportContacts} blocked_samples={_frozenBlockedSamples} retreat_samples={_frozenRetreatSamples} " +
                $"first_tick={_first} last_tick={_last} player={_player.GetInstanceId()} wall={_solids[0].Body.GetInstanceId()} " +
                $"wall_shape={_solids[0].ShapeInstanceId} support={_solids[1].Body.GetInstanceId()} support_shape={_solids[1].ShapeInstanceId} " +
                $"health={_frozenHealth} recovery=0 scope={(rift ? "rift_alternation_wall" : "frozen_sentry_recess_wall")} full_route=false" +
                (rift ? $" pose_chain={_riftPoseChain} motor_delta={_riftMotorDelta} terminal_pose=true" : string.Empty));
        }

        internal void AssertComplete()
        {
            _closed = true;
            if (_failure != null) GD.Print($"ORDINARY_WALL_SWEEP_LOCAL_RED {_failure}");
            TestAssert.True(_failure == null && _tick == 0 && _samples > 0 && _reconstructed > 0 && _substeps > 0 &&
                _samples == _reconstructed + _unknown && _last - _first + 1 == (ulong)_samples &&
                _wallContacts > 0 && _ceilingContacts > 0 && _supportContacts > 0,
                $"local ordinary wall evidence samples={_samples} reconstructed={_reconstructed} unknown={_unknown} " +
                $"substeps={_substeps} contacts={_wallContacts}/{_ceilingContacts}/{_supportContacts}: {_failure}");
            GD.Print($"ORDINARY_WALL_SWEEP_LOCAL_OK samples={_samples} reconstructed={_reconstructed} unknown={_unknown} " +
                $"substeps={_substeps} wall_contacts={_wallContacts} ceiling_contacts={_ceilingContacts} support_contacts={_supportContacts} " +
                $"first_tick={_first} last_tick={_last} player={_player.GetInstanceId()} wall={_solids[0].Body.GetInstanceId()} " +
                $"ceiling={_solids[1].Body.GetInstanceId()} support={_solids[2].Body.GetInstanceId()} " +
                "scope=entrance_three_saved_static_shapes full_route=false");
        }

        public void Dispose() { _closed = true; _before.Observe = null; _after.Observe = null; }
    }

    private sealed class Solid
    {
        internal readonly StaticBody2D Body;
        internal readonly Rect2 Bounds;
        private readonly CollisionShape2D _shape;
        private readonly RectangleShape2D _rectangle;
        private readonly uint _owner;
        private readonly Vector2 _expectedNormal;
        internal ulong ShapeInstanceId => _shape.GetInstanceId();

        internal Solid(PrologueSector sector, string name, Vector2 expectedNormal)
        {
            Body = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
            _shape = Body.GetNode<CollisionShape2D>("Collision");
            _rectangle = _shape.Shape as RectangleShape2D ?? throw new InvalidOperationException($"{name} is not a saved rectangle.");
            _owner = ShapeOwner(Body, _shape, _rectangle);
            _expectedNormal = expectedNormal;
            Bounds = new(_shape.GlobalPosition - _rectangle.Size / 2f, _rectangle.Size);
        }

        internal void AssertUnchanged(CharacterBody2D player)
        {
            if (!_shape.IsInsideTree() || _shape.GetParent() != Body || _shape.Shape != _rectangle || _shape.Disabled ||
                Body.IsShapeOwnerDisabled(_owner) || _shape.OneWayCollision || (Body.CollisionLayer & player.CollisionMask) == 0 ||
                !Upright(_shape.GlobalTransform) || new Rect2(_shape.GlobalPosition - _rectangle.Size / 2f, _rectangle.Size) != Bounds ||
                Bounds.Size.X <= 0f || Bounds.Size.Y <= 0f)
                throw new InvalidOperationException($"Native {Body.Name} rectangle or enabled ownership changed.");
        }

        internal bool Matches(KinematicCollision2D hit) => hit.GetCollider() == Body &&
            Body.ShapeFindOwner(hit.GetColliderShapeIndex()) == _owner && hit.GetNormal().Dot(_expectedNormal) > .7f;

        internal bool Matches(GodotObject collider, int shapeIndex, Vector2 normal) => collider == Body &&
            Body.ShapeFindOwner(shapeIndex) == _owner && normal.Dot(_expectedNormal) > .7f;
    }

    private static uint ShapeOwner(CollisionObject2D body, CollisionShape2D shape, Shape2D resource)
    {
        uint owner = 0; bool found = false;
        foreach (int raw in body.GetShapeOwners())
        {
            uint candidate = (uint)raw;
            if (body.ShapeOwnerGetOwner(candidate) != shape) continue;
            if (found || body.ShapeOwnerGetShapeCount(candidate) != 1 || body.ShapeOwnerGetShape(candidate, 0) != resource)
                throw new InvalidOperationException($"Ambiguous native shape ownership: {shape.Name}.");
            owner = candidate; found = true;
        }
        if (!found) throw new InvalidOperationException($"Missing native shape owner: {shape.Name}.");
        return owner;
    }

    private static bool Upright(Transform2D value) => value.X.IsEqualApprox(Vector2.Right) &&
        value.Y.IsEqualApprox(Vector2.Down) && value.Origin.IsFinite();
    private static bool Close(Vector2 a, Vector2 b) => a.IsFinite() && b.IsFinite() &&
        a.DistanceSquaredTo(b) <= ReconcileEpsilon * ReconcileEpsilon;

    private static bool RiftPoseMatches(Vector2 previousBody, Vector2 previousCapsule, Vector2 body, Vector2 capsule) =>
        Close(previousBody, body) && Close(previousCapsule, capsule);

    private static bool RiftMotorMatches(Vector2 body, Vector2 capsule, Vector2 native) =>
        Close(body, native) && Close(capsule, native) && Close(body, capsule);

    private static float CapsuleWallFaceX(Rect2 wall, float radius, float away) =>
        away < 0f ? wall.Position.X - radius : wall.End.X + radius;

    private static bool WithinWallSide(float x, float face, float margin, float away) =>
        away < 0f ? x <= face + margin : x >= face - margin;

    private static bool WithinSupportedCorridor(Vector2 center, Rect2 wall, Rect2 floor, float radius,
        float halfHeight, float margin, float away = 1f) => center.IsFinite() &&
        WithinWallSide(center.X, CapsuleWallFaceX(wall, radius, away), margin, away) &&
        center.Y - halfHeight >= wall.Position.Y && center.Y + halfHeight <= wall.End.Y &&
        center.X - radius >= floor.Position.X && center.X + radius <= floor.End.X &&
        MathF.Abs(center.Y + halfHeight - floor.Position.Y) <= margin + .25f;

    private static bool HasFrozenWallSize(Rect2 wall) => wall.Size == new Vector2(8f, 140f);

    private static bool IsBlockedPress(float centerX, float clearFaceX, float dx, float towardInAwaySpace, bool wallHit) =>
        wallHit && towardInAwaySpace < -.05f && MathF.Abs(dx) <= .25f && MathF.Abs(centerX - clearFaceX) <= 1f;

    private static void FrozenScalarControls()
    {
        Rect2 wall = new(1280f, 2080f, 8f, 140f), floor = new(1280f, 2180f, 230f, 40f);
        TestAssert.True(HasFrozenWallSize(wall) && !HasFrozenWallSize(new Rect2(1252f, 2080f, 36f, 140f)),
            "preserving the right face does not prove the original thin wall geometry");
        TestAssert.True(WithinSupportedCorridor(new(1308f, 2144f), wall, floor, 20f, 36f, .08f) &&
            !WithinSupportedCorridor(new(1258f, 2144f), wall, floor, 20f, 36f, .08f) &&
            !WithinSupportedCorridor(new(1307f, 2144f), wall, floor, 20f, 36f, .08f) &&
            !WithinSupportedCorridor(new(1308f, 2115f), wall, new Rect2(1280f, 2151f, 230f, 40f), 20f, 36f, .08f) &&
            !WithinSupportedCorridor(new(1491f, 2144f), wall, floor, 20f, 36f, .08f),
            "frozen corridor rejects a full through-step, excess penetration, a corner bypass on raised support and partial floor support");
        TestAssert.True(IsBlockedPress(1308f, 1308f, 0f, -.6f, true) &&
            !IsBlockedPress(1308f, 1308f, 0f, -.6f, false) &&
            !IsBlockedPress(1308f, 1308f, 0f, 0f, true) &&
            !IsBlockedPress(1312f, 1308f, 0f, -.6f, true) &&
            !IsBlockedPress(1308f, 1308f, -.5f, -.6f, true),
            "frozen blocked press rejects no target hit, neutral pressure, a distant obstruction and non-stationary arrival");
    }

    private static void RiftScalarControls()
    {
        Rect2 wall = new(2200f, 400f, 36f, 700f), floor = new(1500f, 856f, 700f, 24f);
        float face = CapsuleWallFaceX(wall, 20f, -1f);
        TestAssert.True(face == 2180f && WithinSupportedCorridor(new(2180f, 820f), wall, floor, 20f, 36f, .08f, -1f) &&
            !WithinSupportedCorridor(new(2180.1f, 820f), wall, floor, 20f, 36f, .08f, -1f) &&
            !WithinSupportedCorridor(new(2256f, 820f), wall, new Rect2(1500f, 856f, 800f, 24f), 20f, 36f, .08f, -1f) &&
            IsBlockedPress(2180f, face, 0f, -1f, true) && !IsBlockedPress(2180f, face, 0f, 1f, true),
            "opposite-side Rift corridor rejects margin excess, a full through-step even on extended support, and a retreat counted as pressure");
        Vector2 stable = new(2180f, 820f), skipped = new(2170f, 820f), retreat = new(-1f, 0f);
        TestAssert.True(RiftPoseMatches(stable, stable, stable, stable) &&
            !RiftPoseMatches(stable, stable, skipped, skipped) && !RiftPoseMatches(stable, stable, stable, skipped) &&
            RiftMotorMatches(retreat, retreat, retreat) && !RiftMotorMatches(retreat, retreat, Vector2.Zero) &&
            !RiftMotorMatches(retreat, Vector2.Zero, retreat),
            "Rift rejects skipped between-step retreat and within-step body/capsule/native disagreement independently of path reconstruction");
    }

    private static bool CanUseReportedStep(int collisions, Vector2 request, Vector2 net, Vector2 last,
        Vector2 travel, Vector2 remainder) => Close(net, last) && (collisions == 0 ? Close(request, last) :
        collisions == 1 && Close(travel, last) && Close(travel + remainder, request));

    private static bool Penetrates(Vector2 a, Vector2 b, Rect2 rect, float radius, float axisHalf, float margin)
    {
        // Translate the capsule axis to the rectangle (vertical Minkowski sum),
        // then test its circular radius against the exact swept center segment.
        var expanded = new Rect2(rect.Position - new Vector2(0f, axisHalf), rect.Size + new Vector2(0f, 2f * axisHalf));
        float enter = 0f, leave = 1f;
        Vector2 d = b - a;
        if (Clip(a.X, d.X, expanded.Position.X, expanded.End.X, ref enter, ref leave) &&
            Clip(a.Y, d.Y, expanded.Position.Y, expanded.End.Y, ref enter, ref leave)) return true;
        float nearest = MathF.Min(PointRect(a, expanded), PointRect(b, expanded));
        nearest = MathF.Min(nearest, PointSegment(expanded.Position, a, d));
        nearest = MathF.Min(nearest, PointSegment(expanded.End, a, d));
        nearest = MathF.Min(nearest, PointSegment(new Vector2(expanded.Position.X, expanded.End.Y), a, d));
        nearest = MathF.Min(nearest, PointSegment(new Vector2(expanded.End.X, expanded.Position.Y), a, d));
        float threshold = radius - margin;
        return nearest < threshold * threshold;
    }

    private static bool Clip(float a, float d, float min, float max, ref float enter, ref float leave)
    {
        if (d == 0f) return a >= min && a <= max;
        float first = (min - a) / d, last = (max - a) / d;
        enter = MathF.Max(enter, MathF.Min(first, last)); leave = MathF.Min(leave, MathF.Max(first, last));
        return enter <= leave;
    }

    private static float PointRect(Vector2 point, Rect2 rect) => point.DistanceSquaredTo(new Vector2(
        Math.Clamp(point.X, rect.Position.X, rect.End.X), Math.Clamp(point.Y, rect.Position.Y, rect.End.Y)));
    private static float PointSegment(Vector2 point, Vector2 start, Vector2 delta)
    {
        float length = delta.LengthSquared();
        float t = length == 0f ? 0f : Math.Clamp((point - start).Dot(delta) / length, 0f, 1f);
        return point.DistanceSquaredTo(start + t * delta);
    }

    private static void ScalarControls()
    {
        Rect2 solid = new(4, 4, 2, 2);
        TestAssert.True(Penetrates(Vector2.Zero, new Vector2(10, 10), solid, 1f, 0f, 0f) &&
            !Penetrates(Vector2.Zero, new Vector2(0, 10), solid, 1f, 0f, 0f) &&
            !Penetrates(new Vector2(0, 10), new Vector2(10, 10), solid, 1f, 0f, 0f),
            "actual bent substeps cannot be replaced by their false-positive chord");
        TestAssert.True(!Penetrates(new Vector2(0, 1), new Vector2(10, 1), solid, 1f, 2f, 0f) &&
            Penetrates(new Vector2(0, 1.1f), new Vector2(10, 1.1f), solid, 1f, 2f, 0f) &&
            !Penetrates(new Vector2(0, 1.05f), new Vector2(10, 1.05f), solid, 1f, 2f, .08f),
            "tangent support and native contact margin differ from actual geometric penetration");
        var corner = new Vector2(3.2f, 1.2f);
        TestAssert.True(!Penetrates(corner, corner, solid, 1f, 2f, 0f),
            "capsule round corners do not behave like their enclosing AABB");
        TestAssert.True(CanUseReportedStep(1, new Vector2(5, 0), new Vector2(2, 0), new Vector2(2, 0),
                new Vector2(2, 0), new Vector2(3, 0)) &&
            !CanUseReportedStep(1, new Vector2(5, 0), new Vector2(2, 0), new Vector2(2, 0),
                new Vector2(2, 0), new Vector2(4, 0)) &&
            !CanUseReportedStep(2, new Vector2(5, 0), new Vector2(2, 0), new Vector2(2, 0),
                new Vector2(2, 0), new Vector2(3, 0)),
            "only reconciled native substeps qualify; hidden recovery and multi-slide tails stay unknown");
    }
}
