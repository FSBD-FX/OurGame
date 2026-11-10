using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class ResidentPatrolRangeRuntimeTests
{
    public static async Task RunMeleeReach(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        Knight.Tests.Unit.RustShellModelTests.Run();
        var errors = new List<Exception>();
        foreach (var item in new (string Name, float X, bool Resident, bool Dodge)[] {
            (Name:"close-hit", X:720f, Resident:true, Dodge:false),
            ("close-real-dodge",720f,true,true),
            ("resident-right",900f,true,false), ("resident-left",432f,true,false),
            ("wave-right",900f,false,false), ("wave-left",432f,false,false) })
        {
            try { await RunMeleeCase(tree, item.Name, item.X, item.Resident, item.Dodge); }
            catch (Exception error)
            {
                GD.Print($"RESIDENT_MELEE_CASE_FAILED case={item.Name} reason={error.Message}");
                errors.Add(error);
            }
        }
        if (errors.Count != 0) throw new AggregateException("First-cycle melee controls failed", errors);
        GD.Print("RESIDENT_MELEE_NATIVE_OK cases=6 first_cycles=true directions=2 real_hit=true real_dodge=true " +
            "wave=true saved_actor=true saved_player=true normal_autospawn=false human=false");
    }

    private static async Task RunMeleeCase(SceneTree tree, string name, float playerX, bool resident, bool dodge)
    {
        var fixture = new RangeFixture(tree);
        var probe = new FirstMeleeProbe(fixture, name, playerX, dodge);
        KeyboardRouteInputDriver driver = null;
        Exception failure = null;
        bool sent = false, released = false;
        try
        {
            await fixture.Mount(playerX, resident, () =>
            {
                probe.Attach();
                if (dodge) driver = KeyboardRouteInputDriver.Begin(fixture.Player);
            });
            for (int i = 0; i < 480 && !probe.Ended; i++)
            {
                probe.ThrowObservationFailure();
                if (dodge && probe.DodgeRequested && !sent)
                {
                    TestAssert.Equal(AttackPhase.Telegraph, probe.LastPhase, "MELEE_GUARD: D starts during the actual first warning");
                    driver.Command(InputActions.MoveRight, true); sent = true;
                    GD.Print($"RESIDENT_MELEE_INPUT case={name} edge=D_down tick={Engine.GetPhysicsFrames()} OS_origin=false");
                }
                if (sent && !released && fixture.Player.GlobalPosition.X >= playerX + 96)
                {
                    driver.Command(InputActions.MoveRight, false); released = true;
                    GD.Print($"RESIDENT_MELEE_INPUT case={name} edge=D_up tick={Engine.GetPhysicsFrames()} OS_origin=false");
                }
                await fixture.Frames(1);
                driver?.ObserveFrame();
            }
            probe.ThrowObservationFailure();
            probe.Print();
            probe.AssertComplete();
            if (dodge)
                TestAssert.True(sent && released && driver.KeyDownCount == 1 && driver.AttackEdges == 0 &&
                    driver.ClawEdges == 0 && driver.ShieldEdges == 0 && !Input.IsPhysicalKeyPressed(Key.D) &&
                    fixture.Player.GlobalPosition.X >= playerX + 96,
                    "MELEE_GUARD: only real D retreat/release, no combat, jump, shield or claw assistance");
            TestAssert.True(fixture.Recoveries == 0 && fixture.ActorHealth.CurrentHealth == 3 &&
                fixture.Player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == 0 &&
                fixture.Player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == 0,
                "MELEE_GUARD: no recovery, hostile damage, jump or thruster intervention");
            GD.Print($"RESIDENT_MELEE_CASE_OK case={name} first_cycle=true engine_keyboard={dodge} " +
                "after_tree_pose_velocity_hp_state_writes=0 normal_autospawn=false");
        }
        catch (Exception error) { failure = error; }
        finally
        {
            await PrologueCanyonGeneratedMainRouteTests.Task10FinalizeRoute(failure, new Func<Task>[] {
                () => { driver?.Dispose(); return Task.CompletedTask; },
                () => { probe.Detach(); return Task.CompletedTask; }, fixture.Dispose
            });
        }
    }

    private sealed class FirstMeleeProbe
    {
        private readonly RangeFixture _fixture;
        private readonly string _name;
        private readonly float _startX;
        private readonly bool _dodge;
        private readonly List<string> _rows = new();
        private RustShellSquire _actor;
        private CharacterBody2D _player;
        private Hitbox _hitbox;
        private Hurtbox _hurtbox;
        private CollisionShape2D _attackShape, _targetShape;
        private Action<AttackPresentationFrame> _presentation;
        private Action<RustShellFrame, int> _post;
        private Hitbox.ConnectedEventHandler _connected;
        private HealthComponent.DamagedEventHandler _damaged;
        private AttackTimelineFrame _last;
        private Exception _failure;
        private ulong _id, _warningTick, _activeTick, _recoveryTick, _endTick, _lastPostTick, _actorId, _playerId;
        private int _facing, _warnings, _actives, _recoveries, _connections, _damages, _postSamples;
        private bool _locked;
        private Vector2 _lockedTarget;
        private float _step, _minimumActiveGap = float.PositiveInfinity, _maximumActiveGap = float.NegativeInfinity;
        public bool Ended { get; private set; }
        public bool DodgeRequested { get; private set; }
        public AttackPhase LastPhase => _last.Phase;

        public FirstMeleeProbe(RangeFixture fixture, string name, float startX, bool dodge)
        { _fixture = fixture; _name = name; _startX = startX; _dodge = dodge; }

        public void Attach()
        {
            _actor = _fixture.Actor; _player = _fixture.Player;
            _actorId = _actor.GetInstanceId(); _playerId = _player.GetInstanceId();
            _hitbox = _actor.GetNode<Hitbox>("AttackHitbox");
            _hurtbox = _player.GetNode<Hurtbox>("Hurtbox");
            _attackShape = _hitbox.GetNode<CollisionShape2D>("AttackCollider");
            _targetShape = _hurtbox.GetNode<CollisionShape2D>("HurtboxCollider");
            TestAssert.True(_actor.CurrentFrame.AttackTimeline.Phase == AttackPhase.None &&
                _fixture.ActorHealth.CurrentHealth == 3 && _fixture.PlayerHealth.CurrentHealth == 5,
                "MELEE_GUARD: observers attach before the first attack, not after settle");
            _presentation = frame => Observe(() =>
            {
                if (Ended) return;
                var timeline = frame.Timeline;
                if (_id == 0)
                {
                    if (timeline.Phase == AttackPhase.None) return;
                    TestAssert.True(timeline.Phase == AttackPhase.Telegraph && timeline.AttackId == 1,
                        "MELEE_GUARD: first actual attack starts with full warning/id1");
                    _id = timeline.AttackId; _warningTick = Engine.GetPhysicsFrames(); _facing = _actor.Facing;
                    _step = (float)_actor.GetPhysicsProcessDeltaTime();
                    TestAssert.True(_facing == (_startX > 666 ? 1 : -1) && _step > 0,
                        "MELEE_GUARD: first warning faces the actual target and has a native delta");
                }
                if (timeline.AttackId != _id)
                {
                    TestAssert.True(_last.Phase == AttackPhase.Recovery &&
                        (timeline.Phase == AttackPhase.None || timeline.Phase == AttackPhase.Telegraph),
                        "MELEE_GUARD: first cycle may end only after complete recovery");
                    _endTick = Engine.GetPhysicsFrames(); Ended = true; return;
                }
                TestAssert.True(!Ended && _actor.Facing == _facing && timeline.Kind == EnemyAttackKind.RustShellThrust,
                    "MELEE_GUARD: committed first attack keeps identity/kind/facing");
                if (timeline.Phase != _last.Phase)
                {
                    TestAssert.True((int)timeline.Phase == (int)_last.Phase + 1,
                        "MELEE_GUARD: no skipped or replayed warning/active/recovery");
                    if (timeline.Phase == AttackPhase.Active) _activeTick = Engine.GetPhysicsFrames();
                    if (timeline.Phase == AttackPhase.Recovery) _recoveryTick = Engine.GetPhysicsFrames();
                }
                if (timeline.HasLockedTarget)
                {
                    if (_locked) TestAssert.True(timeline.LockedWorldTarget == _lockedTarget,
                        "MELEE_GUARD: locked world target never tracks the retreat");
                    else { _locked = true; _lockedTarget = timeline.LockedWorldTarget; }
                }
                else TestAssert.True(!_locked, "MELEE_GUARD: locked target cannot be revoked mid-cycle");
                _last = timeline;
                if (timeline.Phase == AttackPhase.Telegraph) { _warnings++; DodgeRequested |= _dodge; }
                if (timeline.Phase == AttackPhase.Active) _actives++;
                if (timeline.Phase == AttackPhase.Recovery) _recoveries++;
                Capture("publication-pre-move", frame.Shape);
            });
            _post = (frame, _) => Observe(() =>
            {
                if (_id == 0 || Ended) return;
                ulong tick = Engine.GetPhysicsFrames();
                TestAssert.True((_lastPostTick == 0 || tick == _lastPostTick + 1) && frame.AttackTimeline.AttackId == _id,
                    "MELEE_GUARD: each first-cycle native post-MoveAndSlide step is continuous");
                _lastPostTick = tick; _postSamples++;
                Capture("native-post-move", default);
            });
            _damaged = (remaining, _) => Observe(() =>
            {
                TestAssert.True(_id == 1 && !Ended && _last.Phase == AttackPhase.Active && remaining == 4 && _damages == 0,
                    "MELEE_GUARD: one native first-active damage event, never health injection");
                _damages++;
            });
            _connected = target => Observe(() =>
            {
                TestAssert.True(ReferenceEquals(target, _hurtbox) && !Ended && _last.Phase == AttackPhase.Active &&
                    _fixture.PlayerHealth.CurrentHealth == 4 && _damages == 1,
                    "MELEE_GUARD: true Hitbox contact follows successful native damage");
                _connections++;
            });
            _actor.AttackFrameChanged += _presentation; _actor.FrameChanged += _post;
            _fixture.PlayerHealth.Damaged += _damaged; _hitbox.Connected += _connected;
        }

        private void Observe(Action action)
        {
            if (_failure != null) return;
            try { action(); } catch (Exception error) { _failure ??= error; }
        }

        private void Capture(string timing, AttackShapeFrame published)
        {
            TestAssert.True(Engine.IsInPhysicsFrame() && _actor.GetInstanceId() == _actorId &&
                _player.GetInstanceId() == _playerId && _fixture.ActorHealth.CurrentHealth == 3 &&
                _actor.HasLineOfSightTo(_player), "MELEE_GUARD: original native identities/LOS/hostile HP remain");
            _fixture.AssertNativeBodies(_damages == 1 && _fixture.PlayerHealth.CurrentHealth == 4);
            if (!_dodge && _damages == 0)
                TestAssert.True(Mathf.Abs(_player.GlobalPosition.X - _startX) < .05f,
                    "MELEE_GUARD: stationary target did not evade before damage");
            var rectangle = _attackShape.Shape as RectangleShape2D ?? throw new InvalidOperationException("Saved attack is not a rectangle");
            var capsule = _targetShape.Shape as CapsuleShape2D ?? throw new InvalidOperationException("Saved target is not a capsule");
            TestAssert.True(rectangle.Size == new Vector2(26, 18) && capsule.Radius == 20 && capsule.Height == 72 &&
                _attackShape.GlobalTransform.X.IsEqualApprox(Vector2.Right) && _attackShape.GlobalTransform.Y.IsEqualApprox(Vector2.Down) &&
                _targetShape.GlobalTransform.X.IsEqualApprox(Vector2.Right) && _targetShape.GlobalTransform.Y.IsEqualApprox(Vector2.Down) &&
                _hitbox.Damage == 1 && _hitbox.Monitoring == (_last.Phase == AttackPhase.Active),
                "MELEE_GUARD: original native shapes, basis, damage and active monitoring");
            Rect2 attack = _attackShape.GlobalTransform * rectangle.GetRect();
            Rect2 target = _targetShape.GlobalTransform * capsule.GetRect();
            float gap = _facing > 0 ? target.Position.X - attack.End.X : attack.Position.X - target.End.X;
            if (_last.Phase == AttackPhase.Active)
            { _minimumActiveGap = Mathf.Min(_minimumActiveGap, gap); _maximumActiveGap = Mathf.Max(_maximumActiveGap, gap); }
            _rows.Add($"RESIDENT_MELEE_STEP case={_name} timing={timing} tick={Engine.GetPhysicsFrames()} " +
                $"id={_id} phase={_last.Phase} remaining={_last.PhaseRemainingSeconds:R} duration={_last.PhaseDurationSeconds:R} " +
                $"actor={_actorId}/{_actor.GlobalPosition} player={_playerId}/{_player.GlobalPosition} facing={_actor.Facing} " +
                $"attack_transform={_attackShape.GlobalTransform} attack_rect={attack} target_transform={_targetShape.GlobalTransform} " +
                $"target_rect={target} straight_y={_targetShape.GlobalPosition.Y-16},{_targetShape.GlobalPosition.Y+16} horizontal_gap={gap:R} " +
                $"locked={_last.HasLockedTarget}/{_last.LockedWorldTarget} published={published.Kind}/{published.Origin}/{published.Extent} " +
                $"monitoring={_hitbox.Monitoring} hp={_fixture.PlayerHealth.CurrentHealth} connected={_connections} damaged={_damages}");
        }

        public void ThrowObservationFailure()
        { if (_failure != null) throw new InvalidOperationException("MELEE_GUARD: native observation failed", _failure); }

        public void Print()
        {
            foreach (string row in _rows) GD.Print(row);
            GD.Print($"RESIDENT_MELEE_FIRST_CYCLE case={_name} id={_id} ended={Ended} ticks={_warningTick}/{_activeTick}/{_recoveryTick}/{_endTick} " +
                $"delta={_step:R} warning={_warnings} active={_actives} recovery={_recoveries} post_samples={_postSamples} " +
                $"active_gap_min={_minimumActiveGap:R} active_gap_max={_maximumActiveGap:R} connected={_connections} damaged={_damages} " +
                $"hp=5->{_fixture.PlayerHealth.CurrentHealth} player={_player.GlobalPosition} actor={_actor.GlobalPosition} " +
                "saved_shapes=true direct_TryConnect=false normal_autospawn=false");
        }

        public void AssertComplete()
        {
            TestAssert.True(Ended && _id == 1 && _warnings > 0 && _actives > 0 && _recoveries > 0 && _locked &&
                _lastPostTick + 1 == _endTick && _postSamples == _warnings + _actives + _recoveries,
                "MELEE_GUARD: complete first identity/warning/active/recovery/native-step coverage");
            Duration(_activeTick - _warningTick, .65f, "warning");
            Duration(_recoveryTick - _activeTick, .14f, "active");
            Duration(_endTick - _recoveryTick, .65f, "recovery");
            if (_dodge)
                TestAssert.True(_connections == 0 && _damages == 0 && _fixture.PlayerHealth.CurrentHealth == 5,
                    "MELEE_DODGE: genuine retreat from a reachable close warning must remain safe");
            else
                TestAssert.True(_connections == 1 && _damages == 1 && _fixture.PlayerHealth.CurrentHealth == 4,
                    $"RESIDENT_MELEE_REACH_RED: first complete static attack must reach real saved target; case={_name} " +
                    $"connections={_connections} HP={_fixture.PlayerHealth.CurrentHealth} active_gap={_minimumActiveGap}..{_maximumActiveGap}");
        }

        private void Duration(ulong ticks, float required, string name) => TestAssert.True(
            ticks * _step >= required - .001f && ticks * _step <= required + _step + .002f,
            $"MELEE_GUARD: original complete {name} duration {ticks*_step} vs {required}");

        public void Detach()
        {
            if (GodotObject.IsInstanceValid(_actor))
            { if (_presentation != null) _actor.AttackFrameChanged -= _presentation; if (_post != null) _actor.FrameChanged -= _post; }
            if (GodotObject.IsInstanceValid(_hitbox) && _connected != null) _hitbox.Connected -= _connected;
            if (GodotObject.IsInstanceValid(_player) && _damaged != null) _fixture.PlayerHealth.Damaged -= _damaged;
        }
    }
}
