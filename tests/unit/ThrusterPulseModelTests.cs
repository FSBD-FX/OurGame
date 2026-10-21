using System;
using System.Reflection;
using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class ThrusterPulseModelTests
{
    private static Type _modelType, _requestType, _decisionType, _reasonType;

    public static void Run()
    {
        Assembly assembly = typeof(MobilityModuleSettings).Assembly;
        _modelType = assembly.GetType("Knight.Player.ThrusterPulseModel");
        TestAssert.True(_modelType != null, "Thruster pulse model is unavailable");
        _requestType = assembly.GetType("Knight.Player.ThrusterPulseStartRequest");
        _decisionType = assembly.GetType("Knight.Player.ThrusterPulseStartDecision");
        _reasonType = assembly.GetType("Knight.Player.ThrusterEndReason");
        TestAssert.True(_requestType != null && _decisionType != null && _reasonType != null,
            "pulse request, decision and termination contracts are available");
        PreviewAndCommitKeepBudgetSeparate();
        RejectionsDoNotPartiallyChangeState();
        DecisionsAreBoundToAnOwnerAndRevision();
        ResetCannotReviveOldWork();
        ModuleReplacementIsAtomicAndDoesNotRefund();
        InvalidCurrentDynamicsAreRejected();
        GD.Print("THRUSTER_PULSE_MODEL_OK preview_readonly=true accepted_sample_deduplicated=true budget=true stale_identity_rejected=true unique_end=true module_snapshot=true player_wired=false");
    }

    private static void PreviewAndCommitKeepBudgetSeparate()
    {
        object model = New();
        object before = Snapshot(model);
        object preview = Call(model, "Preview", Request(model, 1, direction: new Vector2(1f, -1f)));
        TestAssert.True(Get<bool>(preview, "CanStart"), "grounded first edge is eligible");
        Same(before, Snapshot(model), "preview does not reserve budget, burn ID or direction");
        object started = Call(model, "TryCommit", preview);
        TestAssert.True(Get<bool>(started, "Accepted"), "valid decision commits exactly once");
        object firstId = Get<object>(started, "Identity");
        TestAssert.Equal(1UL, Get<ulong>(firstId, "BurnId"), "preview allocated no invisible burn IDs");
        Near(.18f, Get<float>(model, "BurnRemaining"), "commit uses the module duration");
        Near(.7071068f, Get<Vector2>(model, "BurnDirection").X, "direction is normalized and locked");
        TestAssert.True(Get<bool>(model, "AirUseAvailable"), "ground-only Shift preserves the air use");
        object committed = Snapshot(model);
        TestAssert.True(!Get<bool>(Call(model, "TryCommit", preview), "Accepted"), "one decision cannot commit twice");
        Same(committed, Snapshot(model), "duplicate commit changes no pulse state");
        RejectPreview(model, Request(model, 2), "Burning");
        object partial = Call(model, "ApplyDynamics", firstId, .05f, false);
        TestAssert.True(!Get<bool>(partial, "Ended"), "partial last burn is not a termination");
        Near(.05f, Get<float>(model, "BurnRemaining"), "actual core remainder is retained");
        object end = Call(model, "ApplyDynamics", firstId, 0f, false);
        TestAssert.True(Get<bool>(end, "Ended") && Get<object>(end, "Reason").ToString() == "Exhausted",
            "exhaustion reports one typed termination");
        Same(firstId, Get<object>(end, "Identity"), "termination retains the successful identity");
        object ended = Snapshot(model);
        TestAssert.True(!Get<bool>(Call(model, "ApplyDynamics", firstId, 0f, false), "Ended"), "late duplicate result does not end twice");
        Same(ended, Snapshot(model), "duplicate end has no side effect");
        RejectPreview(model, Request(model, 1), "DuplicateSample");

        object air = Call(model, "TryCommit", Call(model, "Preview", Request(model, 2, floor: false)));
        TestAssert.True(Get<bool>(air, "Accepted") && !Get<bool>(model, "AirUseAvailable"), "airborne edge consumes the one air use");
        Call(model, "Interrupt", Reason("ClawTakeover"));
        TestAssert.True(!Get<bool>(model, "AirUseAvailable"), "claw takeover does not refund air budget");
        RejectPreview(model, Request(model, 3, floor: false), "NoAirUse");
        ulong life = Get<ulong>(model, "Lifecycle");
        TestAssert.True(!(bool)Call(model, "ObserveLanding", life - 1, 1UL), "an old lifetime cannot restore air use");
        TestAssert.True((bool)Call(model, "ObserveLanding", life, 1UL), "new same-life landing notice restores air use once");
        object landed = Snapshot(model);
        TestAssert.True(!(bool)Call(model, "ObserveLanding", life, 1UL), "duplicate landing notice is rejected");
        Same(landed, Snapshot(model), "duplicate landing cannot change revision or budget");

        object simultaneous = New();
        Call(simultaneous, "TryCommit", Call(simultaneous, "Preview", Request(simultaneous, 1, rawJump: true)));
        TestAssert.True(!Get<bool>(simultaneous, "AirUseAvailable"), "ground Shift plus original Space edge consumes air use without ordinary jump");
    }

    private static void RejectionsDoNotPartiallyChangeState()
    {
        object model = New();
        RejectPreview(model, Request(model, 1, shift: false), "NoPress");
        RejectPreview(model, Request(model, 0), "InvalidSample");
        RejectPreview(model, Request(model, 1, alive: false), "Dead");
        RejectPreview(model, Request(model, 1, blocked: true), "ControlsBlocked");
        RejectPreview(model, Request(model, 1, guard: true), "GuardHeld");
        RejectPreview(model, Request(model, 1, direction: Vector2.Zero), "InvalidDirection");
        RejectPreview(model, Request(model, 1, direction: new Vector2(float.NaN, 0f)), "InvalidDirection");
        RejectPreview(model, Request(model, 1, lifecycle: 99UL), "StaleRequest");
        RejectPreview(model, Request(model, 1, configuration: 99UL), "StaleRequest");
        ExpectArgument(() => New(MobilityModuleSettings.Standard with { ThrustAcceleration = float.NaN }), "ThrustAcceleration");
        ExpectArgument(() => New(ownerId: 0UL), "ownerId");
    }

    private static void DecisionsAreBoundToAnOwnerAndRevision()
    {
        object left = New(), right = New(); // Deliberately same public owner ID/epoch/revision.
        object decision = Call(left, "Preview", Request(left, 1));
        object rightBefore = Snapshot(right);
        TestAssert.True(!Get<bool>(Call(right, "TryCommit", decision), "Accepted"), "a different model cannot reuse a numerically identical owner's decision");
        Same(rightBefore, Snapshot(right), "cross-owner token does not consume budget");
        TestAssert.True(!Get<bool>(Call(left, "TryCommit", Activator.CreateInstance(_decisionType)), "Accepted"), "default decision grants no authority");
        Call(left, "Interrupt", Reason("ExternalReplacement"));
        object interrupted = Snapshot(left);
        TestAssert.True(!Get<bool>(Call(left, "TryCommit", decision), "Accepted"), "even idle external replacement invalidates provisional authority");
        Same(interrupted, Snapshot(left), "stale provisional commit changes no state");
    }

    private static void ResetCannotReviveOldWork()
    {
        object model = New();
        object first = Call(model, "TryCommit", Call(model, "Preview", Request(model, 1, floor: false)));
        object oldId = Get<object>(first, "Identity");
        Call(model, "Interrupt", Reason("ClawTakeover"));
        object oldDecision = Call(model, "Preview", Request(model, 2));
        ulong oldLife = Get<ulong>(model, "Lifecycle");
        Call(model, "ResetLifecycle", Reason("Death"));
        TestAssert.True(Get<ulong>(model, "Lifecycle") > oldLife && Get<bool>(model, "AirUseAvailable"), "explicit recovery changes lifecycle and restores only its allowed budget");
        TestAssert.Equal(0, Get<int>(model, "AcceptedCount"), "legacy accepted count resets, unlike identity serial");
        object reset = Snapshot(model);
        TestAssert.True(!Get<bool>(Call(model, "TryCommit", oldDecision), "Accepted"), "old lifecycle decision cannot start new work");
        Same(reset, Snapshot(model), "old decision does not partially restart after reset");
        object current = Call(model, "TryCommit", Call(model, "Preview", Request(model, 3)));
        TestAssert.True(Get<bool>(current, "Accepted"), "fresh request starts in the new lifecycle");
        object newId = Get<object>(current, "Identity");
        TestAssert.True(Get<ulong>(newId, "BurnId") > Get<ulong>(oldId, "BurnId"), "successful burn serial never rewinds");
        object active = Snapshot(model);
        TestAssert.True(!Get<bool>(Call(model, "ApplyDynamics", oldId, 0f, true), "Ended"), "old result cannot terminate the current burn");
        Same(active, Snapshot(model), "late old result cannot clear new state");
    }

    private static void ModuleReplacementIsAtomicAndDoesNotRefund()
    {
        object model = New();
        object started = Call(model, "TryCommit", Call(model, "Preview", Request(model, 1, rawJump: true)));
        var other = MobilityModuleSettings.Standard with { ModuleId = "mobility.test.alternate.v1", BurnDuration = .24f, ThrustAcceleration = 4200f };
        object burning = Snapshot(model);
        TestAssert.True(!(bool)Call(model, "TryReplaceIdleModule", other), "burning module replacement is rejected");
        Same(burning, Snapshot(model), "burning replacement is atomic rejection");
        Call(model, "ApplyDynamics", Get<object>(started, "Identity"), 0f, false);
        object idle = Snapshot(model);
        TestAssert.True(!(bool)Call(model, "TryReplaceIdleModule", other with { AirDragRate = float.NaN }), "invalid module is rejected before any change");
        Same(idle, Snapshot(model), "invalid replacement changes neither module nor epoch");
        object oldDecision = Call(model, "Preview", Request(model, 2));
        ulong epoch = Get<ulong>(model, "ConfigurationEpoch");
        TestAssert.True((bool)Call(model, "TryReplaceIdleModule", other), "idle value replacement succeeds only after caller's future world eligibility");
        TestAssert.True(Get<ulong>(model, "ConfigurationEpoch") > epoch && !Get<bool>(model, "AirUseAvailable"), "configuration changes do not refund spent air use");
        TestAssert.Equal(other, Get<MobilityModuleSettings>(model, "Module"), "full replacement value snapshot is installed");
        object replaced = Snapshot(model);
        TestAssert.True(!Get<bool>(Call(model, "TryCommit", oldDecision), "Accepted"), "old configuration decision cannot commit");
        Same(replaced, Snapshot(model), "stale configuration decision has no partial effect");
        Call(model, "TryCommit", Call(model, "Preview", Request(model, 2)));
        Near(.24f, Get<float>(model, "BurnRemaining"), "future pulse consumes the new immutable duration");
    }

    private static void InvalidCurrentDynamicsAreRejected()
    {
        object model = New();
        object started = Call(model, "TryCommit", Call(model, "Preview", Request(model, 1)));
        object identity = Get<object>(started, "Identity");
        foreach (float remaining in new[] { -.01f, .181f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            object before = Snapshot(model);
            ExpectArgument(() => Call(model, "ApplyDynamics", identity, remaining, false), "remaining");
            Same(before, Snapshot(model), "invalid current dynamics cannot partly consume burn");
        }
        ExpectArgument(() => Call(model, "ApplyDynamics", identity, .1f, true), "remaining");
        object cancelled = Call(model, "ApplyDynamics", identity, 0f, true);
        TestAssert.True(Get<bool>(cancelled, "Ended") && Get<object>(cancelled, "Reason").ToString() == "ReverseCancelled", "reverse cancellation has a distinct single termination");
    }

    private static object New(MobilityModuleSettings? settings = null, ulong ownerId = 99UL) =>
        Activator.CreateInstance(_modelType, new object[] { settings ?? MobilityModuleSettings.Standard, ownerId });
    private static object Request(object model, ulong sequence, bool shift = true, bool rawJump = false,
        bool floor = true, bool alive = true, bool blocked = false, bool guard = false,
        Vector2? direction = null, ulong? lifecycle = null, ulong? configuration = null) =>
        Activator.CreateInstance(_requestType, new object[] { sequence, shift, rawJump, floor,
            alive, blocked, guard, direction ?? Vector2.Right,
            lifecycle ?? Get<ulong>(model, "Lifecycle"), configuration ?? Get<ulong>(model, "ConfigurationEpoch") });
    private static object Reason(string name) => Enum.Parse(_reasonType, name);
    private static object Call(object target, string method, params object[] args) =>
        (target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing pulse method " + method)).Invoke(target, args);
    private static T Get<T>(object value, string property) =>
        (T)(value.GetType().GetProperty(property)?.GetValue(value)
            ?? throw new InvalidOperationException("Missing pulse property " + property));
    private static object Snapshot(object model) => Get<object>(model, "Snapshot");
    private static void Same(object expected, object actual, string message) => TestAssert.True(Equals(expected, actual), message);
    private static void Near(float expected, float actual, string message)
    {
        TestAssert.True(float.IsFinite(actual), message + " must be finite");
        TestAssert.Near(expected, actual, .00001f, message);
    }
    private static void RejectPreview(object model, object request, string reason)
    {
        object before = Snapshot(model), decision = Call(model, "Preview", request);
        TestAssert.True(!Get<bool>(decision, "CanStart") && Get<object>(decision, "Reason").ToString() == reason, "preview rejects " + reason);
        TestAssert.True(!Get<bool>(Call(model, "TryCommit", decision), "Accepted"), "rejected " + reason + " cannot commit");
        Same(before, Snapshot(model), "rejected " + reason + " keeps all pulse state unchanged");
    }
    private static void ExpectArgument(Action action, string field)
    {
        Exception rejected = null;
        try { action(); }
        catch (TargetInvocationException exception) { rejected = exception.InnerException; }
        catch (ArgumentException exception) { rejected = exception; }
        TestAssert.True(rejected is ArgumentException && rejected.Message.Contains(field, StringComparison.Ordinal), "invalid pulse " + field + " requires a named ArgumentException");
    }
}
