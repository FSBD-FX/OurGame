using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

public readonly record struct MixedLaneMotionPlan(int HeldSteps, int TotalSteps, float StopX);

// Test-driver arithmetic, not collision/safety authority or a production movement rule.
internal sealed class MixedLanePositionControl : IDisposable
{
    private readonly CharacterBody2D _player;
    private readonly StaticBody2D _floor;
    private readonly CanyonEncounterGroup _group;
    private readonly List<string> _rows = new(48);
    private KeyboardRouteInputDriver _driver;
    private PlayerInputReader _input;
    private PlayerMotor _motor;
    private PlayerCombat _combat;
    private PlayerShieldController _shield;
    private MechanicalClawController _claw;
    private HealthComponent _health;
    private RustShellSquire _near;
    private RangedWeaponEnemy _far;
    private Rect2 _nearBounds, _farBounds, _floorBounds;
    private MixedLaneMotionPlan _plan;
    private float _minimum, _maximum, _initialOverhang, _startX;
    private int _ticks, _hp, _nearHp, _farHp, _jumps, _dashes, _edges, _attacks;
    private ulong _nextCast, _releaseSequence;
    private bool _braking, _ownA, _fullSeen, _disposed;
    public bool Started { get; private set; }
    public bool Completed { get; private set; }

    public MixedLanePositionControl(CharacterBody2D player, StaticBody2D floor, CanyonEncounterGroup group)
    { _player = player; _floor = floor; _group = group; }

    internal static bool TryPlan(float x, float velocity, float minimum, float maximum,
        float safeSeconds, PlayerMotionSettings motion, MobilityModuleSettings module,
        double delta, int budget, out MixedLaneMotionPlan plan)
    {
        plan = default;
        if (!float.IsFinite(x) || !float.IsFinite(velocity) || velocity > 0f ||
            !float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum || x < minimum ||
            !float.IsFinite(safeSeconds) || safeSeconds <= 0f || !double.IsFinite(delta) || delta <= 0 ||
            budget is < 4 or > 900) return false;
        for (int held = 0; held <= budget - 3; held++)
        {
            var forecast = FreeMotionForecast.HeldThenNeutral(velocity, -1f, true,
                motion, module, delta, held, budget - 3, .5f);
            if (!forecast.SpeedThresholdReached) continue;
            float stop = x + forecast.NetDistance;
            if (stop > maximum) continue;
            if (stop < minimum || safeSeconds <= (forecast.Steps + 3) * delta) return false;
            plan = new(held, forecast.Steps, stop);
            return true;
        }
        return false;
    }

    // true owns ONE existing outer tick. It never waits, writes pose/velocity,
    // issues J/K/L, selects a resolver target or alters a combat receipt.
    public bool Step(RustShellSquire near, RangedWeaponEnemy far,
        PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger danger, int loop)
    {
        if (_disposed || Completed) return false;
        if (!Started)
        {
            if (KeyboardRouteInputDriver.Current == null || !Live(_player) || !Live(_floor) || !Live(_group) ||
                _group.CurrentBatch != 1 || !Live(near) || !Live(far) ||
                !_group.OwnsLiveActor(near) || !_group.OwnsLiveActor(far) ||
                near.GetParent() != _group.GetNode(_group.ActorRootPath) || far.GetParent() != near.GetParent() ||
                near.SceneFilePath != CanyonEncounterCatalog.RustShellScene ||
                far.SceneFilePath != CanyonEncounterCatalog.BurstScene || far.Role != RangedWeaponRole.Burst ||
                Health(near) <= 0 || Health(far) <= 0 ||
                !FloorContact() || !Bounds(_player, "BodyCollider", out Rect2 body) ||
                !Bounds(_floor, "Collision", out _floorBounds) ||
                !Bounds(near, "BodyCollider", out _nearBounds) || !Bounds(far, "BodyCollider", out _farBounds)) return false;
            _input = _player.GetNode<PlayerInputReader>("Input");
            _motor = _player.GetNode<PlayerMotor>("Motor");
            _combat = _player.GetNode<PlayerCombat>("Combat");
            _shield = _player.GetNode<PlayerShieldController>("Shield");
            _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
            _health = _player.GetNode<HealthComponent>("Health");
            if (!Quiet() || _motor.EffectiveMovementScale != 1f || _motor.HasEnvironmentalLaunchMomentum ||
                _motor.HasGrappleReleaseMomentum || _player.Velocity.X > 0 || Mathf.Abs(_player.Velocity.Y) > .5f ||
                _player.GlobalPosition.X < _floorBounds.Position.X || _player.GlobalPosition.X > _floorBounds.End.X ||
                body.Position.X < _floorBounds.Position.X) return false;
            float offset = body.GetCenter().X - _player.GlobalPosition.X, half = body.Size.X * .5f;
            _minimum = Mathf.Max(_farBounds.End.X + half + 1f - offset, _floorBounds.Position.X + half - offset);
            _maximum = Mathf.Min(_nearBounds.Position.X - half - 1f - offset, _floorBounds.End.X - half - offset);
            if (_farBounds.End.X >= _nearBounds.Position.X || _player.GlobalPosition.X <= _maximum ||
                !Safe(danger) || !TryPlan(_player.GlobalPosition.X, _player.Velocity.X, _minimum, _maximum,
                    danger.SafeCounterSeconds, _motor.MotionSettings, _motor.ThrusterState.Module,
                    _player.GetPhysicsProcessDeltaTime(), 900 - loop, out _plan)) return false;
            _driver = KeyboardRouteInputDriver.Current; _near = near; _far = far;
            _hp = _health.CurrentHealth; _nearHp = Health(near); _farHp = Health(far);
            _jumps = _motor.AcceptedJumpCount; _dashes = _motor.AcceptedDashCount;
            _edges = _driver.ClawEdges; _attacks = _driver.AttackEdges; _nextCast = _claw.CurrentFrame.NextCastId;
            _initialOverhang = Mathf.Max(0, body.End.X - _floorBounds.End.X);
            _fullSeen = _initialOverhang == 0; _startX = _player.GlobalPosition.X;
            Started = true;
            // Own only A; all other actions were already physically released.
            _driver.Command(InputActions.MoveRight, false);
            Row("start", loop, $"near={near.GetInstanceId()} far={far.GetInstanceId()} shelf={_floor.GetInstanceId()} " +
                $"interval={_minimum:R}..{_maximum:R} held={_plan.HeldSteps} total={_plan.TotalSteps} " +
                $"predicted_stop={_plan.StopX:R} initial_overhang={_initialOverhang:R}");
        }
        TestAssert.True(KeyboardRouteInputDriver.Current == _driver && Live(_near) && Live(_far) && Live(_group) &&
            _group.OwnsLiveActor(_near) && _group.OwnsLiveActor(_far) && Quiet() && Safe(danger) && FloorContact() &&
            _health.CurrentHealth == _hp && Health(_near) == _nearHp && Health(_far) == _farHp &&
            _motor.AcceptedJumpCount == _jumps && _motor.AcceptedDashCount == _dashes &&
            _driver.ClawEdges == _edges && _driver.AttackEdges == _attacks && _claw.CurrentFrame.NextCastId == _nextCast &&
            Bounds(_player, "BodyCollider", out _) && Bounds(_near, "BodyCollider", out Rect2 nearNow) &&
            Bounds(_far, "BodyCollider", out Rect2 farNow) && Bounds(_floor, "Collision", out Rect2 floorNow) &&
            Same(_nearBounds, nearNow) && Same(_farBounds, farNow) && Same(_floorBounds, floorNow),
            $"MIXED_LANE_RETAINED_RED loop={loop} ticks={_ticks} player={_player.GlobalPosition} " +
            $"velocity={_player.Velocity} safe={danger.SafeCounterSeconds:R} HP={_hp}->{_health.CurrentHealth}");
        Bounds(_player, "BodyCollider", out Rect2 actual);
        float overhang = Mathf.Max(0, actual.End.X - _floorBounds.End.X);
        TestAssert.True(actual.Position.X >= _floorBounds.Position.X && overhang <= _initialOverhang + .02f &&
            (!_fullSeen || overhang == 0) && _player.GlobalPosition.X <= _startX + .02f,
            "mixed lane only moves inward from real partial arrival; full support cannot be lost after acquisition");
        _fullSeen |= overhang == 0;
        _initialOverhang = overhang;
        if (!_braking && _ticks >= _plan.HeldSteps)
        {
            _driver.Command(InputActions.MoveLeft, false); _ownA = false;
            _driver.Command(InputActions.MoveRight, false);
            _releaseSequence = _input.CurrentFrameSequence; _braking = true;
            Row("release", loop, $"sequence={_releaseSequence}");
        }
        if (!_braking) { _driver.Command(InputActions.MoveLeft, true); _ownA = true; }
        Row("sample", loop, $"x={_player.GlobalPosition.X:R} vx={_player.Velocity.X:R} " +
            $"axis={_input.Current.MoveAxis:R} full={_fullSeen} safe={danger.SafeCounterSeconds:R}");
        if (_braking && _input.CurrentFrameSequence > _releaseSequence &&
            _input.RawFrame.MoveAxis == 0 && _input.Current.MoveAxis == 0 &&
            !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
            Mathf.Abs(_player.Velocity.X) <= .5f && _fullSeen)
        {
            TestAssert.True(_player.GlobalPosition.X >= _minimum && _player.GlobalPosition.X <= _maximum &&
                Mathf.Abs(_player.GlobalPosition.X - _plan.StopX) <= .15f,
                "mixed lane actual neutral endpoint fits the real two-body gap and bounded forecast rounding");
            Completed = true;
            Row("completed", loop, "actual_neutral=true extra_K=0 extra_J=0 extra_waits=0 K_success_claim=false");
        }
        TestAssert.True(_ticks <= _plan.TotalSteps + 3, "mixed lane cannot create an extra wait pool after its forecast");
        _ticks++;
        return true; // Completion also yields once in the ORIGINAL outer budget.
    }

    private bool Quiet() => Live(_input) && Live(_motor) && Live(_combat) && Live(_shield) && Live(_claw) &&
        Live(_health) && !_player.GetTree().Paused && _health.CurrentHealth > 0 && !_combat.IsAttacking &&
        !_motor.IsDashing && !_shield.IsActive && _claw.CurrentFrame.State == MechanicalClawState.Idle &&
        _claw.CurrentFrame.Failure == MechanicalClawFailure.None && Clean(_input.Current) && Clean(_input.RawFrame) &&
        !Input.IsPhysicalKeyPressed(Key.J) && !Input.IsPhysicalKeyPressed(Key.K) && !Input.IsPhysicalKeyPressed(Key.L) &&
        !Input.IsPhysicalKeyPressed(Key.Space) && !Input.IsPhysicalKeyPressed(Key.Shift) &&
        !Input.IsPhysicalKeyPressed(Key.W) && !Input.IsPhysicalKeyPressed(Key.S);

    private static bool Clean(PlayerInputFrame f) => f.KeyboardActions && !f.JumpPressed && !f.JumpHeld &&
        !f.DashPressed && !f.AttackPressed && !f.ClawPressed && !f.InteractPressed && !f.PrimaryHeld &&
        !f.PrimaryPressed && !f.ShieldHeld && !f.KeyboardShieldHeld && !f.LegacyShieldHeld &&
        !f.ThreadPressed && !f.ThreadHeld && !f.AttackModePressed && !f.GrappleModePressed;
    private static bool Safe(PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger d) =>
        d.CandidateObserverFaults == 0 && d.MissingGuardSourceCount == 0 && !d.NearContactUnknownActive &&
        !d.NearContactOverlappingActive && float.IsFinite(d.SafeCounterSeconds) &&
        d.SafeCounterSeconds > 3f / Engine.PhysicsTicksPerSecond;
    private static bool Live(Node n) => CourtyardRearClawControl.Live(n);
    private static int Health(Node n) => n.GetNode<HealthComponent>("Health").CurrentHealth;
    private static bool Same(Rect2 a, Rect2 b) => a.Position.DistanceTo(b.Position) <= .02f && a.Size.IsEqualApprox(b.Size);
    private static bool Bounds(Node n, string path, out Rect2 r)
    {
        r = default;
        CollisionShape2D s = Live(n) ? n.GetNodeOrNull<CollisionShape2D>(path) : null;
        if (!Live(s) || s.Disabled || !GodotObject.IsInstanceValid(s.Shape) ||
            s.Shape is not (CapsuleShape2D or RectangleShape2D)) return false;
        Transform2D t = s.GlobalTransform;
        if (!t.Origin.IsFinite() || !t.X.IsFinite() || !t.Y.IsFinite() ||
            Mathf.Abs(t.X.Y) > .001f || Mathf.Abs(t.Y.X) > .001f || t.X.X == 0 || t.Y.Y == 0) return false;
        r = t * s.Shape.GetRect();
        return r.Position.IsFinite() && r.Size.IsFinite() && r.Size.X > 0 && r.Size.Y > 0;
    }
    private bool FloorContact()
    {
        if (!Live(_player) || !Live(_floor) || !_player.IsOnFloor()) return false;
        for (int i = 0; i < _player.GetSlideCollisionCount(); i++)
        {
            using KinematicCollision2D c = _player.GetSlideCollision(i);
            if (c.GetCollider() == _floor && c.GetNormal().Y < -.7f) return true;
        }
        return false;
    }
    private void Row(string kind, int loop, string facts)
    {
        if (_rows.Count < 48) _rows.Add($"MIXED_LANE kind={kind} physics={Engine.GetPhysicsFrames()} " +
            $"loop={loop}/900 tick={_ticks} {facts}");
    }
    public void WriteSummary()
    {
        foreach (string row in _rows) GD.Print(row);
        GD.Print($"MIXED_LANE_END started={Started} completed={Completed} ticks={_ticks} rows={_rows.Count}/48 " +
            "extra_K=0 extra_J=0 extra_waits=0 human=false full_goal=false");
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownA && KeyboardRouteInputDriver.Current == _driver && Live(_player))
            _driver.Command(InputActions.MoveLeft, false);
        _ownA = false;
    }
}
