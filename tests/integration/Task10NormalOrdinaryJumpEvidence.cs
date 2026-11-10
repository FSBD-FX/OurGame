using System;
using System.Reflection;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Expression = System.Linq.Expressions.Expression;

namespace Knight.Tests.Integration;

// Observes the saved Input/Motor without issuing commands or querying physics space.
// Only AcceptedJumpSequence identifies a Motor ordinary jump; upward motion alone does not.
internal sealed class Task10NormalOrdinaryJumpEvidence : IDisposable
{
    [Flags]
    private enum Assist { None = 0, Claw = 1, ClawRelease = 2, Thruster = 4, Environment = 8, Updraft = 16 }

    private static readonly Func<PlayerMotor, ulong> ReadCompletedStep = BindMotorField<ulong>("_lastPresentationPhysicsFrame");
    private static readonly Func<PlayerMotor, bool> ReadJumpSuppressed = BindMotorField<bool>("_suppressJumpForCurrentPhysicsFrame");

    private static Func<PlayerMotor, T> BindMotorField<T>(string name)
    {
        var field = typeof(PlayerMotor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(T))
            throw new InvalidOperationException($"Missing exact saved Motor observation field {name} ({typeof(T).Name}).");
        var motor = Expression.Parameter(typeof(PlayerMotor));
        return Expression.Lambda<Func<PlayerMotor, T>>(Expression.Field(motor, field), motor).Compile();
    }

    private readonly PrologueLevelController _level;
    private readonly PrologueGameFlowController _flow;
    private readonly SeamlessPrologueWorld _world;
    private readonly CharacterBody2D _player;
    private readonly PlayerMotor _motor;
    private readonly PlayerInputReader _input;
    private readonly MechanicalClawController _claw;
    private readonly ThrusterPostMotorObserver _before, _after;
    private readonly string _variant;
    private readonly ulong _playerId, _motorId;
    private readonly Ledger _ledger = new();
    private Tick? _pending;
    private ulong _expectedFirst, _startInput, _startJump;
    private bool _started, _reported, _sealed;
    private string _failure;

    internal Task10NormalOrdinaryJumpEvidence(PrologueLevelController level, PrologueGameFlowController flow,
        SeamlessPrologueWorld world, CharacterBody2D player, string variant)
    {
        _level = level;
        _flow = flow;
        _world = world;
        _player = player;
        _variant = variant;
        _motor = player.GetNode<PlayerMotor>("Motor");
        _input = player.GetNode<PlayerInputReader>("Input");
        _claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        _playerId = player.GetInstanceId();
        _motorId = _motor.GetInstanceId();
        TestAssert.True(_input.ProcessPhysicsPriority == -300 && _claw.ProcessPhysicsPriority == -100 &&
            player.GetNode<Node>("EnvironmentInteraction").ProcessPhysicsPriority == -50 &&
            _motor.ProcessPhysicsPriority == 0, "normal-route probes must bracket the saved Motor after input and claw owners");
        RunDiscriminants();
        _before = new ThrusterPostMotorObserver
        {
            Name = "OrdinaryJumpBeforeMotor", ProcessMode = Node.ProcessModeEnum.Always,
            ProcessPhysicsPriority = -1, Observe = BeforeMotor
        };
        _after = new ThrusterPostMotorObserver
        {
            Name = "OrdinaryJumpAfterMotor", ProcessMode = Node.ProcessModeEnum.Always,
            ProcessPhysicsPriority = 1, Observe = AfterMotor
        };
        level.AddChild(_before);
        level.AddChild(_after);
        flow.SessionStarted += OnSessionStarted;
    }

    private void OnSessionStarted()
    {
        try
        {
            if (_started) { Fail("unexpected second session"); return; }
            _started = true;
            _startInput = _input.CurrentFrameSequence;
            _startJump = _motor.AcceptedJumpSequence;
            _expectedFirst = Engine.GetPhysicsFrames() + (Engine.IsInPhysicsFrame() ? 0UL : 1UL);
        }
        catch (Exception error) { Fail($"session observer: {error}"); }
    }

    private void BeforeMotor()
    {
        if (_sealed || _failure != null) return;
        try
        {
            ulong frame = Engine.GetPhysicsFrames();
            if (_pending is { } orphan)
            { Fail($"no post-Motor observation for {orphan}"); return; }
            if (_flow.State != PrologueGameFlowState.Playing) return;
            if (!_started || _level.Player != _player || _motor.GetParent() != _player || _input.GetParent() != _player ||
                !_motor.IsPhysicsProcessing() || !_motor.CanProcess() || !_input.IsPhysicsProcessing() || !_input.CanProcess())
            { Fail($"missing active original Player/Input/Motor owner at tick={frame}"); return; }
            var current = _input.CurrentFrame;
            var tuning = _motor.MotionSettings;
            ulong completedBefore = ReadCompletedStep(_motor);
            if (completedBefore >= frame)
            { Fail($"Motor completed before its pre-observer: tick={frame} completed={completedBefore}"); return; }
            bool suppressed = ReadJumpSuppressed(_motor), burning = _motor.IsDashing;
            Assist assist = _claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost ? Assist.Claw : Assist.None;
            if (_motor.HasGrappleReleaseMomentum) assist |= Assist.ClawRelease;
            if (_motor.HasEnvironmentalLaunchMomentum) assist |= Assist.Environment;
            if (burning) assist |= Assist.Thruster;
            var sample = new Tick(frame, _input.CurrentFrameSequence, _motor.AcceptedJumpSequence,
                (float)_motor.GetPhysicsProcessDeltaTime(), tuning.CoyoteSeconds, tuning.JumpBufferSeconds,
                _player.IsOnFloor(), _input.RawFrame.JumpPressed, current.JumpPressed, current.JumpHeld,
                suppressed || current.DashPressed || burning,
                assist, _player.GlobalPosition, _player.Velocity, _world.CurrentSector);
            if (_ledger.Samples == 0 && (sample.Frame != _expectedFirst || sample.Input != _startInput + 1 || sample.Jump != _startJump))
            { Fail($"first observation did not immediately follow SessionStarted: expected_tick={_expectedFirst} " +
                $"start_input={_startInput} start_jump={_startJump} pre={sample}"); return; }
            _pending = sample;
        }
        catch (Exception error) { Fail($"pre-Motor observer: {error}"); }
    }

    private void AfterMotor()
    {
        if (_sealed || _failure != null) return;
        try
        {
            ulong frame = Engine.GetPhysicsFrames();
            if (_pending is not { } pre)
            {
                if (_flow.State == PrologueGameFlowState.Playing) Fail($"no pre-Motor observation at tick={frame}");
                return;
            }
            _pending = null;
            // GroundMotionObservation can be revoked by legitimate damage/claw callbacks
            // or disabled processing. This scalar is stamped at the end of the actual
            // Motor step and is not cleared when presentation eligibility changes.
            ulong completed = ReadCompletedStep(_motor);
            var post = new End(frame, _input.CurrentFrameSequence, _motor.AcceptedJumpSequence,
                _player.IsOnFloor(), _motor.UpdraftVelocityDelta != Vector2.Zero, _player.GlobalPosition, _player.Velocity);
            if (_level.Player != _player || completed != frame)
            { Fail($"missing same-tick native Motor completion: completed={completed} pre={pre} post={post}"); return; }
            string rejection = _ledger.Record(pre, post);
            if (rejection != null) Fail($"{rejection} pre={pre} post={post} last_floor_tick={_ledger.LastFloor} " +
                $"coyote_left={_ledger.CoyoteLeft:R} buffer_left={_ledger.BufferLeft:R} support_consumed={_ledger.SupportConsumed}");
        }
        catch (Exception error) { Fail($"post-Motor observer: {error}"); }
    }

    internal void AssertComplete(ulong completionPhysics)
    {
        _sealed = true;
        if (_failure == null && (!_started || _pending != null || _ledger.Samples == 0 || _ledger.Accepted == 0 ||
            _flow.State != PrologueGameFlowState.Completed || completionPhysics == 0 ||
            _ledger.LastFrame > completionPhysics || completionPhysics - _ledger.LastFrame > 1 ||
            _ledger.LastInput != _input.CurrentFrameSequence || _ledger.LastJump != _motor.AcceptedJumpSequence ||
            _ledger.LastJump - _startJump != (ulong)_ledger.Accepted ||
            _ledger.LastInput - _startInput != (ulong)_ledger.Samples))
            Fail($"incomplete terminal coverage: started={_started} samples={_ledger.Samples} accepted={_ledger.Accepted} " +
                $"completion_tick={completionPhysics} last_tick={_ledger.LastFrame} " +
                $"input={_startInput}->{_ledger.LastInput}/{_input.CurrentFrameSequence} " +
                $"jump={_startJump}->{_ledger.LastJump}/{_motor.AcceptedJumpSequence} pending={_pending}");
        TestAssert.True(_failure == null, $"normal route ordinary jump evidence: {_failure}");
        GD.Print($"NORMAL_ROUTE_ORDINARY_JUMP_OK samples={_ledger.Samples} variant={_variant} accepted={_ledger.Accepted} " +
            $"first_tick={_ledger.FirstFrame} last_tick={_ledger.LastFrame} first_input={_startInput + 1} last_input={_ledger.LastInput} " +
            $"first_jump={_startJump} last_jump={_ledger.LastJump} grounded={_ledger.Grounded} coyote={_ledger.Coyote} " +
            $"buffered={_ledger.Buffered} assisted={_ledger.Assisted} assisted_samples={_ledger.AssistedSamples} " +
            $"unsupported_edges={_ledger.UnsupportedEdges} player={_playerId} motor={_motorId} completion_tick={completionPhysics}");
    }

    private void Fail(string reason) => _failure ??= reason;

    internal void ReportFailure()
    {
        if (_failure == null || _reported) return;
        _reported = true;
        GD.Print($"NORMAL_ROUTE_ORDINARY_JUMP_RED variant={_variant} player={_playerId} motor={_motorId} {_failure}");
    }

    public void Dispose()
    {
        _sealed = true;
        _before.Observe = null;
        _after.Observe = null;
        if (GodotObject.IsInstanceValid(_flow)) _flow.SessionStarted -= OnSessionStarted;
    }

    private readonly record struct Tick(ulong Frame, ulong Input, ulong Jump, float Seconds, float CoyoteSeconds,
        float BufferSeconds, bool Floor, bool Raw, bool Resolved, bool Held, bool Blocked, Assist Assistance,
        Vector2 Position, Vector2 Velocity, PrologueSectorId Sector);
    private readonly record struct End(ulong Frame, ulong Input, ulong Jump, bool Floor, bool Updraft,
        Vector2 Position, Vector2 Velocity);

    private sealed class Ledger
    {
        internal int Samples, Accepted, Grounded, Coyote, Buffered, Assisted, AssistedSamples, UnsupportedEdges;
        internal ulong FirstFrame, LastFrame, LastInput, LastJump, LastFloor;
        internal float CoyoteLeft, BufferLeft;
        internal bool SupportConsumed;
        private bool _bufferFromAir, _assistedAir, _lastPostFloor;

        internal string Record(Tick pre, End post)
        {
            if (pre.Frame == 0 || pre.Frame != post.Frame || pre.Input == 0 || pre.Input != post.Input ||
                (Samples > 0 && (pre.Frame != LastFrame + 1 || pre.Input != LastInput + 1 || pre.Jump != LastJump ||
                    pre.Floor != _lastPostFloor)) || (pre.Resolved && !pre.Raw) ||
                post.Jump < pre.Jump || post.Jump - pre.Jump > 1 ||
                !float.IsFinite(pre.Seconds) || pre.Seconds <= 0 ||
                !float.IsFinite(pre.CoyoteSeconds) || pre.CoyoteSeconds < 0 ||
                !float.IsFinite(pre.BufferSeconds) || pre.BufferSeconds < 0)
                return "non-contiguous or invalid real sample pair";

            if (pre.Floor)
            {
                LastFloor = pre.Frame;
                CoyoteLeft = pre.CoyoteSeconds;
                SupportConsumed = false;
            }
            else CoyoteLeft = MathF.Max(0f, CoyoteLeft - pre.Seconds);
            if (pre.Blocked) { BufferLeft = 0f; _bufferFromAir = false; }
            else if (pre.Resolved) { BufferLeft = pre.BufferSeconds; _bufferFromAir = !pre.Floor; }
            else BufferLeft = MathF.Max(0f, BufferLeft - pre.Seconds);
            bool unusedCoyote = LastFloor != 0 && !SupportConsumed && CoyoteLeft > 0f;
            if (!pre.Floor && !unusedCoyote && pre.Raw) UnsupportedEdges++;
            bool accepted = post.Jump == pre.Jump + 1;
            bool assisted = pre.Assistance != Assist.None || post.Updraft || _assistedAir;
            if (accepted)
            {
                if (pre.Blocked) return "ordinary jump while Motor blocked jump";
                if (!unusedCoyote) return "ordinary jump without new or unused coyote support";
                if (BufferLeft <= 0f || (!pre.Resolved && (!pre.Floor || !_lastPostFloor || !_bufferFromAir)))
                    return "ordinary jump without a sampled command or recent air-to-floor buffered command";
                if (!pre.Floor) Coyote++;
                else if (!pre.Resolved) Buffered++;
                else Grounded++;
                if (assisted) Assisted++;
                Accepted++;
                SupportConsumed = true;
                CoyoteLeft = BufferLeft = 0f;
                _bufferFromAir = false;
            }
            if (assisted) AssistedSamples++;
            _assistedAir = !post.Floor && assisted;
            _lastPostFloor = post.Floor;
            if (Samples++ == 0) FirstFrame = pre.Frame;
            LastFrame = pre.Frame;
            LastInput = pre.Input;
            LastJump = post.Jump;
            return null;
        }
    }

    // Scalar controls exercise the same ledger; these do not stand in for native route samples.
    private static void RunDiscriminants()
    {
        string Add(Ledger ledger, ulong n, ulong jump, bool floor, bool landed, bool edge = false,
            bool accept = false, Assist assist = Assist.None, bool blocked = false) => ledger.Record(
            new Tick(n, n, jump, .02f, .10f, .12f, floor, edge, edge, edge, blocked, assist, default, default, default),
            new End(n, n, jump + (accept ? 1UL : 0UL), landed, false, default, default));
        var coyote = new Ledger();
        TestAssert.True(Add(coyote, 1, 0, true, false) == null &&
            Add(coyote, 2, 0, false, false, edge: true, accept: true) == null && coyote.Coyote == 1 &&
            Add(coyote, 3, 1, false, false, edge: true, accept: true) != null,
            "same ordinary ledger admits a real-support coyote token once and rejects a second airborne receipt");
        var buffer = new Ledger();
        TestAssert.True(Add(buffer, 1, 0, false, true, edge: true) == null &&
            Add(buffer, 2, 0, true, false, accept: true) == null && buffer.Buffered == 1,
            "air-edge receipt is only buffered when the next ordinary acceptance is on actual floor");
        var blockedJump = new Ledger();
        TestAssert.Equal("ordinary jump while Motor blocked jump",
            Add(blockedJump, 1, 0, true, false, edge: true, accept: true, blocked: true),
            "a real grounded edge cannot excuse an ordinary acceptance while Motor is blocked");
        var staleBuffer = new Ledger();
        TestAssert.True(Add(staleBuffer, 1, 0, false, false, edge: true) == null &&
            Add(staleBuffer, 2, 0, false, true, blocked: true) == null &&
            Add(staleBuffer, 3, 0, true, false, accept: true) ==
                "ordinary jump without a sampled command or recent air-to-floor buffered command",
            "a blocked tick clears a still-live earlier airborne edge before landing can reuse it");
        var expiredCoyote = new Ledger();
        TestAssert.True(Add(expiredCoyote, 1, 0, true, false) == null, "expiry control earns actual support");
        for (ulong tick = 2; tick <= 6; tick++)
            TestAssert.True(Add(expiredCoyote, tick, 0, false, false) == null, "expiry control stays continuously airborne");
        TestAssert.Equal("ordinary jump without new or unused coyote support",
            Add(expiredCoyote, 7, 0, false, false, edge: true, accept: true),
            "an unused floor token cannot authorize a jump after its real physics window");
        var assisted = new Ledger();
        TestAssert.True(Add(assisted, 1, 0, false, false, assist: Assist.Environment) == null &&
            assisted.Accepted == 0 && assisted.AssistedSamples == 1 &&
            Add(assisted, 2, 0, false, false, edge: true, accept: true, assist: Assist.Environment) != null,
            "forced ascent is not an ordinary jump and cannot excuse an unsupported receipt");
        var gap = new Ledger();
        TestAssert.True(Add(gap, 1, 0, true, true) == null && Add(gap, 3, 0, true, true) != null,
            "ledger refuses a missing physical/input sample even on ordinary ground");
    }
}
