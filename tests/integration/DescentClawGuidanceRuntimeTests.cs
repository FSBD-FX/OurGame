using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class DescentClawGuidanceRuntimeTests
{
    internal static int TerminalFailureMask(ulong actualPlayer,ulong expectedPlayer,int requests,
        int recoveries,int hp,PrologueObjective objective) =>
        (actualPlayer != expectedPlayer ? 1 : 0) |
        (requests != 0 ? 2 : 0) |
        (recoveries != 0 ? 4 : 0) |
        (hp != 5 ? 8 : 0) |
        (objective != PrologueObjective.ReachBasin ? 16 : 0);

    public static void RunObservationReadControl()
    {
        RunDamageObservationReadControl();
        // Catch fabricated/default snapshots, default-duration assumptions and
        // reads that change the real model or consume an attack identity.
        var read = typeof(DescentClawGuidanceRuntimeTests).GetMethod(
            "ReadModelForObservation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null, new[] { typeof(PlayerCombatModel) }, null);
        TestAssert.True(read != null,
            "SECOND_J_OBSERVER: missing ReadModelForObservation(PlayerCombatModel)");
        var model = new PlayerCombatModel(new PlayerCombatSettings(
            StartupSeconds: .2f, ActiveSeconds: .3f, RecoverySeconds: .5f));

        T ReadControlScalar<T>(object snapshot, string propertyName)
        {
            var property = snapshot.GetType().GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            TestAssert.True(property != null && property.PropertyType == typeof(T) && property.CanRead,
                "SECOND_J_OBSERVER: scalar property contract " + propertyName);
            object value = property.GetValue(snapshot);
            TestAssert.True(value is T, "SECOND_J_OBSERVER: scalar value contract " + propertyName);
            return (T)value;
        }

        void AssertReadControlState(string state, ulong expectedId, float expectedElapsed, bool expectedAttacking)
        {
            TestAssert.Equal(expectedAttacking, model.IsAttacking,
                "SECOND_J_OBSERVER: actual model before reads " + state);
            for (int repetition = 0; repetition < 2; repetition++)
            {
                object snapshot = read.Invoke(null, new object[] { model });
                TestAssert.Equal(expectedAttacking, model.IsAttacking,
                    $"SECOND_J_OBSERVER: original model unchanged after read {state}/{repetition}");
                TestAssert.True(snapshot != null, "SECOND_J_OBSERVER: non-null snapshot " + state);
                TestAssert.Equal(expectedId, ReadControlScalar<ulong>(snapshot, "ActiveAttackId"),
                    "SECOND_J_OBSERVER: literal retained identity " + state);
                TestAssert.Equal(expectedElapsed, ReadControlScalar<float>(snapshot, "Elapsed"),
                    "SECOND_J_OBSERVER: literal elapsed " + state);
                TestAssert.Equal(expectedAttacking, ReadControlScalar<bool>(snapshot, "Attacking"),
                    "SECOND_J_OBSERVER: literal attacking " + state);
                TestAssert.Equal(1.0f, ReadControlScalar<float>(snapshot, "Duration"),
                    "SECOND_J_OBSERVER: literal non-default settings duration " + state);
                TestAssert.Equal(expectedAttacking, model.IsAttacking,
                    $"SECOND_J_OBSERVER: original model unchanged after scalar reads {state}/{repetition}");
            }
        }

        AssertReadControlState("unstarted", 0UL, 0f, false);
        TestAssert.Equal<ulong?>(1UL, model.TryStart(AttackDirection.Horizontal),
            "SECOND_J_OBSERVER: first real attack identity");
        model.Tick(.25);
        AssertReadControlState("active", 1UL, .25f, true);
        model.Tick(.75);
        AssertReadControlState("naturally-completed", 1UL, 1.0f, false);
        model.Reset();
        AssertReadControlState("reset", 0UL, 0f, false);
        TestAssert.Equal<ulong?>(2UL, model.TryStart(AttackDirection.Horizontal),
            "SECOND_J_OBSERVER: repeated reads do not consume attack identities");
        var baseline = new AttackStartObservation(7, 41, 0, 100, 100, 20, 40);
        var start = new AttackStartObservation(8, 42, 1, 200, 109, 29, 49);
        var live = new AttackWindowObservation(new(250, 110, 30, 50), start,
            new(42, .25f, true, 1f), true, true);
        var controls = new (string Name, AttackWindowObservation Sample, bool Expected)[]
        {
            ("live", live, true),
            ("blocked authority is evidence, not filtered away", live with { CanBeginGrapple = false }, true),
            ("ended", live with { Model = new(42, 1f, false, 1f), IsAttacking = false }, false),
            ("zero identity", live with { Start = start with { AttackId = 0 }, Model = new(0, .25f, true, 1f) }, false),
            ("different identity", live with { Model = new(43, .25f, true, 1f) }, false),
            ("extra start", live with { Start = start with { Count = 9 } }, false),
            ("reused baseline", live with { Start = start with { AttackId = 41 }, Model = new(41, .25f, true, 1f) }, false),
            ("nonfinite elapsed", live with { Model = new(42, float.NaN, true, 1f) }, false),
            ("nonfinite duration", live with { Model = new(42, .25f, true, float.PositiveInfinity) }, false),
            ("zero duration", live with { Model = new(42, 0f, true, 0f) }, false),
            ("negative elapsed", live with { Model = new(42, -.1f, true, 1f) }, false),
            ("public state ended", live with { IsAttacking = false }, false),
            ("before start physics", live with { Clock = live.Clock with { Physics = 108 } }, false),
            ("before start input", live with { Clock = live.Clock with { InputSequence = 48 } }, false)
        };
        foreach (var control in controls)
            TestAssert.Equal(control.Expected, IsSecondAttackWindow(control.Sample, baseline),
                "SECOND_J_WINDOW: independent literal eligibility " + control.Name);
        GD.Print("SECOND_J_WINDOW_ELIGIBILITY_CONTROL_OK cases=14 independent_literals=true authority_not_filtered=true");
        GD.Print("SECOND_J_OBSERVER_READ_CONTROL_OK states=4 repeated=true original_model_unchanged=true duration_from_settings=true native_actor=false");
    }

    private static void RunDamageObservationReadControl()
    {
        var read = typeof(DescentClawGuidanceRuntimeTests).GetMethod("ReadAcceptedDamageIdentity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null, new[] { typeof(HealthComponent) }, null);
        TestAssert.True(read != null, "DESCENT_DAMAGE_OBSERVER: accepted damage reader is missing");
        var health = new HealthComponent { MaxHealth = 100, InvulnerabilitySeconds = 0 };
        int observed = 0;
        DamageAttackIdentity? consumed = null;
        void RejectEmpty(string stage)
        {
            bool rejected = false;
            try { read.Invoke(null, new object[] { health }); }
            catch (System.Reflection.TargetInvocationException failure)
            { rejected = failure.InnerException is InvalidOperationException; }
            TestAssert.True(rejected, "DESCENT_DAMAGE_OBSERVER: empty accepted identity rejected " + stage);
        }
        void Damaged(int remaining, Vector2 _)
        {
            DamageAttackIdentity actual = ReadDamageForEvent(health, remaining, ref consumed);
            TestAssert.Equal(new DamageAttackIdentity((ulong)(700 + observed), (ulong)(1000 + observed)),
                actual, "DESCENT_DAMAGE_OBSERVER: actual synchronous tail, not first or prior attack");
            observed++;
            TestAssert.Equal(100 - observed, remaining, "DESCENT_DAMAGE_OBSERVER: actual HP after accepted request");
            TestAssert.Equal(remaining, health.CurrentHealth, "DESCENT_DAMAGE_OBSERVER: signal follows health mutation");
        }
        health.Damaged += Damaged;
        try
        {
            RejectEmpty("before first request");
            for (int index = 0; index < 65; index++)
                TestAssert.True(health.ReceiveDamage(new DamageRequest(1, Vector2.Right, 0,
                    (ulong)(1000 + index), (ulong)(700 + index))),
                    "DESCENT_DAMAGE_OBSERVER: original health accepts independent request " + index);
            TestAssert.Equal(65, observed, "DESCENT_DAMAGE_OBSERVER: actual event coverage crosses64 capacity");
            TestAssert.True(!health.ReceiveDamage(new DamageRequest(1, Vector2.Right, 0, 1064, 764)) &&
                !health.ReceiveDamage(new DamageRequest(0, Vector2.Right, 0, 9999, 9999)),
                "DESCENT_DAMAGE_OBSERVER: duplicate and zero damage are rejected by real health");
            TestAssert.Equal(65, observed, "DESCENT_DAMAGE_OBSERVER: rejected damage never fabricates events");
            TestAssert.Equal(2, health.RestoreHealth(2), "DESCENT_DAMAGE_OBSERVER: original heal is independent");
            TestAssert.Equal(65, observed, "DESCENT_DAMAGE_OBSERVER: heal is not damage");
            TestAssert.Equal(new DamageAttackIdentity(764, 1064),
                (DamageAttackIdentity)read.Invoke(null, new object[] { health }),
                "DESCENT_DAMAGE_OBSERVER: heal and repeated reads preserve last accepted identity");
            health.ResetFull();
            RejectEmpty("after reset");
        }
        finally { health.Damaged -= Damaged; health.Free(); }
        RunDamageReentrancyControl();
        GD.Print("DESCENT_DAMAGE_READ_CONTROL_OK accepted=65 rejected=2 capacity_crossed=true heals=true reset=true synchronous=true read_only=true");
    }

    private static void RunDamageReentrancyControl()
    {
        var health = new HealthComponent { MaxHealth = 100, InvulnerabilitySeconds = 0 };
        bool reentered = false;
        int callbacks = 0, unattributed = 0;
        DamageAttackIdentity? consumed = null;
        var attributed = new List<DamageAttackIdentity>();
        void Reenter(int _, Vector2 __)
        {
            if (reentered) return;
            reentered = true;
            TestAssert.Equal(1, health.RestoreHealth(1), "DESCENT_DAMAGE_REENTRANCY: heal restores outer event HP");
            TestAssert.True(health.ReceiveDamage(new DamageRequest(1, Vector2.Left, 20, 9001, 7001)),
                "DESCENT_DAMAGE_REENTRANCY: real nested B accepted");
        }
        void Observe(int remaining, Vector2 _)
        {
            callbacks++;
            try { attributed.Add(ReadDamageForEvent(health, remaining, ref consumed)); }
            catch (InvalidOperationException) { unattributed++; }
        }
        health.Damaged += Reenter;
        health.Damaged += Observe;
        try
        {
            TestAssert.True(health.ReceiveDamage(new DamageRequest(1, Vector2.Right, 10, 9000, 7000)),
                "DESCENT_DAMAGE_REENTRANCY: real outer A accepted");
            TestAssert.Equal(99, health.CurrentHealth, "DESCENT_DAMAGE_REENTRANCY: equal HP cannot distinguish A/B");
            TestAssert.Equal(2, callbacks, "DESCENT_DAMAGE_REENTRANCY: both actual events observed");
            TestAssert.Equal(1, attributed.Count,
                "DESCENT_DAMAGE_REENTRANCY: stale outer event must not attribute nested B twice");
            TestAssert.Equal(new DamageAttackIdentity(7001, 9001), attributed[0],
                "DESCENT_DAMAGE_REENTRANCY: only nested B has a current identity");
            TestAssert.Equal(1, unattributed, "DESCENT_DAMAGE_REENTRANCY: outer event explicitly fails closed");
        }
        finally { health.Damaged -= Reenter; health.Damaged -= Observe; health.Free(); }
        GD.Print("DESCENT_DAMAGE_REENTRANCY_OK actual_events=2 attributed=1 outer_rejected=1 equal_hp=true original_health=true");
    }

    private static DamageAttackIdentity ReadDamageForEvent(HealthComponent health, int remaining,
        ref DamageAttackIdentity? consumed)
    {
        DamageAttackIdentity accepted = ReadAcceptedDamageIdentity(health);
        if (health.CurrentHealth != remaining)
            throw new InvalidOperationException("damage event no longer matches its observed HP");
        if (consumed.HasValue && consumed.Value == accepted)
            throw new InvalidOperationException("accepted damage tail was already consumed by a nested event");
        consumed = accepted;
        return accepted;
    }

    private static DamageAttackIdentity ReadAcceptedDamageIdentity(HealthComponent health)
    {
        var field = typeof(HealthComponent).GetField("_processedAttackOrder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (!GodotObject.IsInstanceValid(health) ||
            field?.GetValue(health) is not Queue<DamageAttackIdentity> { Count: > 0 } accepted)
            throw new InvalidOperationException("DESCENT_DAMAGE_OBSERVER: no accepted damage identity available");
        DamageAttackIdentity latest = default;
        foreach (DamageAttackIdentity identity in accepted) latest = identity;
        return latest;
    }

    private readonly record struct ModelObservation(ulong ActiveAttackId, float Elapsed, bool Attacking, float Duration);
    private readonly record struct ObservationClock(ulong EngineMs, ulong Physics, ulong Process, ulong InputSequence);
    private readonly record struct AttackStartObservation(int Count, ulong AttackId, int Direction,
        ulong EngineMs, ulong Physics, ulong Process, ulong InputSequence);
    private readonly record struct AttackWindowObservation(ObservationClock Clock, AttackStartObservation Start,
        ModelObservation Model, bool IsAttacking, bool CanBeginGrapple);
    private readonly record struct AttackCardObservation(AttackWindowObservation Window, bool GuidanceProcessing,
        bool Visible, bool PromptVisible, bool Contained, int TargetCount, PrologueGuidanceTargetKey FirstTarget,
        int RenderedTargets, string StepText, string RenderedStepText, string LabelText);

    private static bool IsSecondAttackWindow(AttackWindowObservation sample, AttackStartObservation baseline) =>
        sample.Start.Count == baseline.Count + 1 && sample.Start.AttackId != 0 &&
        sample.Start.AttackId != baseline.AttackId && sample.Model.ActiveAttackId == sample.Start.AttackId &&
        sample.Model.Attacking && sample.IsAttacking && float.IsFinite(sample.Model.Elapsed) &&
        float.IsFinite(sample.Model.Duration) && sample.Model.Duration > 0 &&
        sample.Model.Elapsed >= 0 && sample.Model.Elapsed < sample.Model.Duration &&
        sample.Clock.Physics >= sample.Start.Physics && sample.Clock.InputSequence >= sample.Start.InputSequence;

    private static void ValidateWindowEvidence(IReadOnlyList<AttackWindowObservation> physicsWindows,
        IReadOnlyList<AttackCardObservation> processWindows, AttackStartObservation baselineStart,
        ObservationClock command, string observationFailure, GameLanguage language)
    {
        TestAssert.True(observationFailure == null, "attack-window observer failed: " + observationFailure);
        TestAssert.True(physicsWindows.Count > 0 && processWindows.Count > 0,
            "real second J has live post-combat AND post-guidance evidence; old or absent cards do not qualify");
        AttackStartObservation sameStart = physicsWindows[0].Start;
        foreach (AttackWindowObservation physics in physicsWindows)
            TestAssert.True(IsSecondAttackWindow(physics, baselineStart) && physics.Start == sameStart &&
                physics.Start.Physics >= command.Physics && physics.Start.EngineMs >= command.EngineMs &&
                physics.CanBeginGrapple,
                "EVERY observed live post-combat row permits K during the same real second attack");
        foreach (AttackCardObservation card in processWindows)
        {
            TestAssert.True(IsSecondAttackWindow(card.Window, baselineStart) && card.Window.Start == sameStart &&
                card.Window.Clock.Process > card.Window.Start.Process && card.Window.CanBeginGrapple,
                "EVERY observed live post-guidance row permits K during the same real second attack");
            TestAssert.True(card.GuidanceProcessing && card.Visible && card.PromptVisible && card.Contained &&
                card.TargetCount == 1 && card.FirstTarget == PrologueGuidanceTargetKey.DescentClawOpportunity &&
                card.RenderedTargets == 1 && card.RenderedStepText == card.StepText &&
                card.LabelText.Contains(card.StepText) && HasOpportunityMeanings(card.StepText, language),
                "EVERY observed live opportunity card remains complete during the SAME actual second J");
        }
    }

    private static async Task CompleteWindowObservation(Func<Task> completionWait, Action freeze, Action validate)
    {
        try { await completionWait(); }
        finally { freeze(); }
        validate();
    }

    private static DescentAttackWindowObserver CreateWindowObserver(Node parent, Node guidance,
        Action<DescentAttackWindowObserver> initialize)
    {
        TestAssert.True(guidance.ProcessPriority < 100 && guidance.IsProcessing() &&
            guidance.CanProcess(), "attack-window process observer runs after the actual enabled guidance owner");
        var observer = new DescentAttackWindowObserver();
        try
        {
            observer.ProcessPhysicsPriority = 100;
            observer.ProcessPriority = 100;
            initialize(observer);
            parent.AddChild(observer);
            return observer;
        }
        catch
        {
            if (GodotObject.IsInstanceValid(observer))
            {
                observer.ObservePhysics = null;
                observer.ObserveProcess = null;
                observer.Free();
            }
            throw;
        }
    }

    public static async Task RunWindowReviewControls(SceneTree tree)
    {
        var failures = new List<string>();
        var baseline = new AttackStartObservation(7, 41, 0, 100, 100, 20, 40);
        var start = new AttackStartObservation(8, 42, 1, 200, 109, 29, 49);
        var command = new ObservationClock(150, 108, 28, 48);
        var live = new AttackWindowObservation(new(250, 110, 30, 50), start,
            new(42, .05f, true, .3f), true, true);
        const string text = "ROUGH CEILING | W: AIM UP | DIAMOND THEN K | HIT: auto-pull | A/D: DESCEND";
        var card = new AttackCardObservation(live, true, true, true, true, 1,
            PrologueGuidanceTargetKey.DescentClawOpportunity, 1, text, text, text);
        void Validate(IReadOnlyList<AttackWindowObservation> physics, IReadOnlyList<AttackCardObservation> process) =>
            ValidateWindowEvidence(physics, process, baseline, command, null, GameLanguage.English);
        void Reject(string name, Action action)
        {
            bool rejected = false;
            try { action(); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected) failures.Add(name);
        }
        Validate(new[] { live }, new[] { card });
        var later = live with { Clock = new(400, 119, 38, 59), Model = new(42, .2f, true, .3f) };
        Reject("I1 later physics lock accepted", () => Validate(
            new[] { live, later with { CanBeginGrapple = false } }, new[] { card }));
        Reject("I1 later process lock accepted", () => Validate(new[] { live },
            new[] { card, card with { Window = later with { CanBeginGrapple = false } } }));
        Reject("I1 later hidden card accepted", () => Validate(new[] { live },
            new[] { card, card with { Window = later, Visible = false } }));

        string deferredFailure = null;
        bool frozen = false, deferredRejected = false;
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task proof = CompleteWindowObservation(() => completion.Task, () => frozen = true,
            () => TestAssert.True(deferredFailure == null, "WINDOW_CALLBACK_DELAYED_SENTINEL"));
        deferredFailure = "recorded after initial valid sample";
        completion.SetResult(true);
        try { await proof; }
        catch (InvalidOperationException error)
        { deferredRejected = error.Message.Contains("WINDOW_CALLBACK_DELAYED_SENTINEL"); }
        if (!deferredRejected || !frozen) failures.Add("I2 deferred callback error accepted or evidence not frozen");

        var root = new Node { Name = "AttackWindowOwnershipControl" };
        var guidance = new Node { Name = "ActualProcessOwner" };
        root.AddChild(guidance); tree.Root.AddChild(root); guidance.SetProcess(true);
        try
        {
            TestAssert.True(guidance.IsProcessing() && guidance.CanProcess(),
                "ownership control has an actual enabled tree owner before rejection probes");
            // Exercise the normal owned lifecycle once before ObjectCount probes.
            // The first managed observer type activation may retain engine script
            // metadata; that is not an unattached instance owned by a failed call.
            DescentAttackWindowObserver positive = CreateWindowObserver(root, guidance, _ => { });
            positive.Free();
            TestAssert.True(!GodotObject.IsInstanceValid(positive) && root.GetChildCount() == 1,
                "normal observer creation/free releases its actual Node before rejection baselines");
            void OwnershipProbe(string stage, bool processing, Action<DescentAttackWindowObserver> initialize,
                string expectedMessage)
            {
                guidance.SetProcess(processing);
                double before = Performance.GetMonitor(Performance.Monitor.ObjectCount);
                bool rejected = false;
                DescentAttackWindowObserver unexpected = null;
                try { unexpected = CreateWindowObserver(root, guidance, initialize); }
                catch (InvalidOperationException error) { rejected = error.Message.Contains(expectedMessage); }
                finally { if (unexpected != null) unexpected.Free(); }
                double retained = Performance.GetMonitor(Performance.Monitor.ObjectCount) - before;
                GD.Print($"SECOND_J_WINDOW_OWNER_CONTROL stage={stage} rejected={rejected} retained={retained}");
                if (!rejected || retained != 0) failures.Add("I3 " + stage + " retained=" + retained);
            }
            OwnershipProbe("precondition", false, _ => { }, "attack-window process observer");
            OwnershipProbe("initialization", true, _ => throw new InvalidOperationException("WINDOW_INIT_SENTINEL"),
                "WINDOW_INIT_SENTINEL");
        }
        finally { root.Free(); }
        TestAssert.True(failures.Count == 0, "SECOND_J_WINDOW_REVIEW_CONTROLS: " + string.Join(" | ", failures));
        GD.Print("SECOND_J_WINDOW_REVIEW_CONTROLS_OK cases=6 later_bad_rows=true deferred_fault=true owned_failure_paths=true");
    }

    private static T ReadObservationField<T>(object owner, string name)
    {
        var field = owner.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Second J observation field missing: " + name);
        return (T)(field.GetValue(owner) ??
            throw new InvalidOperationException("Second J observation field null: " + name));
    }

    private static ModelObservation ReadModelForObservation(PlayerCombatModel model)
    {
        var settings = ReadObservationField<PlayerCombatSettings>(model, "_settings");
        return new(ReadObservationField<ulong>(model, "_activeAttackId"),
            ReadObservationField<float>(model, "_elapsed"), model.IsAttacking,
            settings.StartupSeconds + settings.ActiveSeconds + settings.RecoverySeconds);
    }

    private static ObservationClock ClockForObservation(PlayerInputReader input) =>
        new(Time.GetTicksMsec(), Engine.GetPhysicsFrames(), Engine.GetProcessFrames(), input.CurrentFrameSequence);

    private static void ObserveSecondJ(SceneTree tree, PrologueLevelController level, PlayerInputReader input,
        MechanicalClawController claw, Func<AttackStartObservation> readStart,
        AttackStartObservation baselineStart, ObservationClock command)
    {
        try
        {
            var player = level.Player;
            var combat = player.GetNode<PlayerCombat>("Combat");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var health = player.GetNode<HealthComponent>("Health");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            object ActorState() => new
            {
                PlayerId = player.GetInstanceId(), InputId = input.GetInstanceId(), CombatId = combat.GetInstanceId(),
                Physics = Engine.GetPhysicsFrames(), Process = Engine.GetProcessFrames(),
                InputSequence = input.CurrentFrameSequence, combat.IsAttacking, modes.CanBeginAttack,
                modes.CanBeginGrapple, LastRejection = modes.LastRejection.ToString(), modes.ActiveSlot,
                Mode = modes.CurrentMode.ToString(), energy.EnergyRatio, energy.IsOverheated,
                ShieldActive = shield.IsActive, Claw = claw.CurrentFrame.State.ToString(),
                Flow = flow.State.ToString(), Hp = health.CurrentHealth, tree.Paused,
                X = player.GlobalPosition.X, Y = player.GlobalPosition.Y,
                Vx = player.Velocity.X, Vy = player.Velocity.Y,
                RawAttackPressed = input.RawFrame.AttackPressed, AttackPressed = input.Current.AttackPressed,
                input.Current.ShieldHeld, AimX = input.Current.Aim.X, AimY = input.Current.Aim.Y,
                GuidanceVisible = guidance.CurrentInstruction.Visible,
                GuidanceTargets = string.Join(",", guidance.CurrentInstruction.Targets)
            };
            object before = ActorState();
            var model = ReadObservationField<PlayerCombatModel>(combat, "_model");
            ModelObservation modelBefore = ReadModelForObservation(model);
            ObservationClock observed = ClockForObservation(input);
            AttackStartObservation start = readStart();
            ModelObservation modelAfter = ReadModelForObservation(model);
            object after = ActorState();
            bool readOnly = before.Equals(after) && modelBefore == modelAfter;
            GD.Print("DESCENT_SECOND_J_OBSERVATION " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Command = command, BaselineStart = baselineStart, LatestStart = start,
                Observed = observed, Model = modelAfter, Actor = after, ObserverReadOnly = readOnly
            }));
            if (!readOnly) GD.PushError("Second J observation changed the sampled actor/model state.");
        }
        catch (Exception failure)
        {
            // Keep the original assertion running, but never qualify missing/fabricated observations.
            GD.PushError("Second J observation failed: " + failure);
        }
    }

    private static bool TracingDefinitions =>
        Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--trace-sector-definitions");

#if REFCOUNTED_FATAL_IDENTITY
    internal static void ObserveBeforeMountOwned(PrologueLevelController level)
    {
        if (!Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeTraceActive) return;
        // Still off-tree and exclusively owned here. Do not use this immediate
        // release guard on the mounted level; its original audio finally owns it.
        BasinSupplyRuntimeTests.ObserveWithOwnedRoot(level,
            root => ObserveAssignedStartupResources(root,"descent.before-add-child"));
    }

    internal static void ObserveAssignedStartupResources(PrologueLevelController level,string stage)
    {
        if (!Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeTraceActive) return;
        var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var presentation=level.GetNode<Knight.Presentation.PlayerPresentationCoordinator>("PlayerRoot/Player/Presentation");
        var motor=level.GetNode<PlayerMotor>("PlayerRoot/Player/Motor");
        var combat=level.GetNode<PlayerCombat>("PlayerRoot/Player/Combat");
        ulong rootId=level.GetInstanceId();
        int definitions=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveAssignedSectorDefinitions(rootId,world,stage);
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResource(presentation.Profile,rootId,
            "PlayerRoot/Player/Presentation/Profile","Descent.assigned-player-profile",stage);
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResource(motor.Tuning,rootId,
            "PlayerRoot/Player/Motor/Tuning","Descent.assigned-motor-tuning",stage);
        Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResource(combat.Tuning,rootId,
            "PlayerRoot/Player/Combat/Tuning","Descent.assigned-combat-tuning",stage);
        Console.Error.WriteLine("DESCENT_ASSIGNED_STARTUP_BOUNDARY " + System.Text.Json.JsonSerializer.Serialize(new
        {
            Stage=stage,RootId=rootId,WorldId=world.GetInstanceId(),InsideTree=level.IsInsideTree(),Definitions=definitions,
            ProfileSerial=presentation.Profile.Task14ManagedSerial,
            ProfileNativeId=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObservedNativeId(presentation.Profile.Task14ManagedSerial),
            MotorSerial=motor.Tuning.Task14ManagedSerial,
            MotorNativeId=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObservedNativeId(motor.Tuning.Task14ManagedSerial),
            CombatSerial=combat.Tuning.Task14ManagedSerial,
            CombatNativeId=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObservedNativeId(combat.Tuning.Task14ManagedSerial)
        }));
        Console.Error.Flush();
    }
#endif

    // Opt-in diagnosis only. Scalar values never retain/clone/dispose loaded
    // definitions across await, and add no extra Load, GC or frame wait.
    internal static void TraceSectorDefinitions(SeamlessPrologueWorld world, string stage)
    {
        if (!TracingDefinitions) return;
        var rows = new List<string>();
        int count = world.Definitions?.Count ?? -1;
        for (int index = 0; index < count; index++)
        {
            PrologueSectorDefinition definition = world.Definitions[index];
            bool valid = GodotObject.IsInstanceValid(definition);
            rows.Add($"{index}:null={definition == null},valid={valid},id={(valid ? definition.GetInstanceId() : 0UL)}");
        }
        GD.Print($"SECTOR_DEFINITION_IDENTITY_DIAGNOSTIC stage={stage} world={world.GetInstanceId()} physics={Engine.GetPhysicsFrames()} count={count} items=[{string.Join(';', rows)}] retained=false cache_changed=false");
    }

    internal static PrologueLevelController CreateLevelFixture()
    {
        var level = InstantiateLevelFixture();
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TraceSectorDefinitions(world, "descent.after-instantiate");
        world.LayoutVersion = 2;
        world.InitialSector = PrologueSectorId.CanyonDescent;
        level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position = new Vector2(1600, 1040);
        IsolatedPrologueFixture.Configure(level, "descent-claw-guidance");
        return level;
    }

    private static PrologueLevelController InstantiateLevelFixture()
    {
        // Keep the ordinary load/instantiate expression unchanged. Only the
        // opt-in diagnostic keeps this same packed scene for paired getters;
        // no additional load, cache mode, clone or reference crosses await.
        if (!TracingDefinitions)
            return GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TracePackedDefinitions(packed, "descent.source-pre");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        TraceSectorDefinitions(level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors"),
            "descent.instance-immediate");
        TracePackedDefinitions(packed, "descent.source-post");
        return level;
    }

    private static void TracePackedDefinitions(PackedScene packed, string stage)
    {
        // GetState is a read-only view of the same packed source, not an owned
        // clone. Do not Dispose borrowed SceneState/Resource wrappers. The
        // added getters can perturb wrapper lifetime: this is diagnosis only,
        // never proof of unchanged timing or native-slot health.
        SceneState state = packed.GetState();
        int nodeMatches = 0, propertyMatches = 0, count = -1;
        var rows = new List<string>();
        Variant.Type kind = Variant.Type.Nil;
        for (int node = 0; node < state.GetNodeCount(); node++)
        {
            using NodePath path = state.GetNodePath(node);
            if (path.ToString() is not ("WorldRoot/Sectors" or "./WorldRoot/Sectors")) continue;
            nodeMatches++;
            for (int property = 0; property < state.GetNodePropertyCount(node); property++)
            {
                using StringName name = state.GetNodePropertyName(node, property);
                if (name.ToString() != "Definitions") continue;
                propertyMatches++;
                using Variant value = state.GetNodePropertyValue(node, property);
                kind = value.VariantType;
                if (kind != Variant.Type.Array) continue;
                using Godot.Collections.Array values = value.AsGodotArray();
                count = values.Count;
                for (int index = 0; index < count; index++)
                {
                    using Variant item = values[index];
                    GodotObject definition = item.VariantType == Variant.Type.Object
                        ? item.AsGodotObject() : null;
                    bool valid = GodotObject.IsInstanceValid(definition);
                    rows.Add($"{index}:type={item.VariantType},null={definition == null},valid={valid},id={(valid ? definition.GetInstanceId() : 0UL)}");
                }
            }
        }
        GD.Print($"SECTOR_PACKED_DEFINITION_DIAGNOSTIC stage={stage} packed={packed.GetInstanceId()} " +
            $"state={state.GetInstanceId()} nodes={nodeMatches} properties={propertyMatches} type={kind} " +
            $"count={count} items=[{string.Join(';', rows)}] retained_across_await=false extra_load=false getter_perturbation_possible=true");
        TestAssert.True(nodeMatches == 1 && propertyMatches == 1 && kind == Variant.Type.Array && count == 5,
            "source diagnostic locates the unique saved Definitions export without changing it");
    }

    public static async Task Run(SceneTree tree, bool presentation = false, bool lateContinuation = false)
    {
        if (presentation && lateContinuation)
            throw new ArgumentException("Late attack-window control is headless only.", nameof(lateContinuation));
        var level = CreateLevelFixture();
        int recoveries = 0, requests = 0, attacks = 0;
        void Recovered(string _) => recoveries++;
        void Requested(string _) => requests++;
        level.RecoveryCompleted += Recovered;
        level.RecoveryRequested += Requested;
        TraceSectorDefinitions(level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors"), "descent.before-add-child");
#if REFCOUNTED_FATAL_IDENTITY
        ObserveBeforeMountOwned(level);
#endif
        tree.Root.AddChild(level);
        var player = level.Player;
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var combat = player.GetNode<PlayerCombat>("Combat");
        var health = player.GetNode<HealthComponent>("Health");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var shield = player.GetNode<PlayerShieldController>("Shield");
        var energy = player.GetNode<PlayerEnergyController>("Energy");
        ulong observedPlayerId = player.GetInstanceId(), motorId = motor.GetInstanceId();
        string damageStage = "startup", damageObservationFailure = null;
        int damageRows = 0, healRows = 0, omittedDamageRows = 0;
        DamageAttackIdentity? consumedDamageIdentity = null;
        void Damaged(int remaining, Vector2 impulse)
        {
            if (++damageRows > 32) { omittedDamageRows++; return; }
            try
            {
                DamageAttackIdentity accepted = ReadDamageForEvent(health, remaining, ref consumedDamageIdentity);
                // The shared reader rejects both changed HP and a tail already
                // consumed by a nested event (including heal-then-damage).
                if (player.GetInstanceId() != observedPlayerId)
                    throw new InvalidOperationException("damage event no longer matches its observed player/HP");
                string sourcePath = "<unresolved>", sourceType = "<unresolved>";
                GodotObject source = GodotObject.InstanceFromId(accepted.SourceRuntimeId);
                if (GodotObject.IsInstanceValid(source))
                {
                    sourceType = source.GetType().FullName;
                    if (source is Node sourceNode && sourceNode.IsInsideTree())
                    {
                        using NodePath path = sourceNode.GetPath();
                        sourcePath = path.ToString();
                    }
                }
                GD.Print("DESCENT_ACCEPTED_DAMAGE " + System.Text.Json.JsonSerializer.Serialize(new
                {
                    Stage = damageStage, Physics = Engine.GetPhysicsFrames(), Process = Engine.GetProcessFrames(),
                    InputSequence = input.CurrentFrameSequence, PlayerId = observedPlayerId,
                    Remaining = remaining, accepted.SourceRuntimeId, accepted.LocalAttackId,
                    MotorId = motorId, MotorSource = accepted.SourceRuntimeId == motorId,
                    SourcePath = sourcePath, SourceType = sourceType,
                    X = player.GlobalPosition.X, Y = player.GlobalPosition.Y,
                    Vx = player.Velocity.X, Vy = player.Velocity.Y,
                    ImpulseX = impulse.X, ImpulseY = impulse.Y,
                    ShieldActive = shield.IsActive, energy.IsOverheated, energy.EnergyRatio,
                    Claw = claw.CurrentFrame.State.ToString(), motor.LandingCount,
                    motor.LastFallHeight, motor.LastFallDamage,
                    LastFallAppliedNotRead = true, ObserverReadOnly = true
                }));
            }
            catch (Exception failure)
            {
                damageObservationFailure ??= failure.GetType().Name + ": " + failure.Message;
                GD.Print("DESCENT_DAMAGE_OBSERVER_FAILURE " + damageObservationFailure);
            }
        }
        void Healed(int restored, int remaining)
        {
            if (++healRows > 32) { omittedDamageRows++; return; }
            GD.Print($"DESCENT_HEALED stage={damageStage} physics={Engine.GetPhysicsFrames()} " +
                $"input_sequence={input.CurrentFrameSequence} restored={restored} remaining={remaining} read_only=true");
        }
        AttackStartObservation lastStart = default;
        void Attacked(ulong attackId, int direction)
        {
            attacks++;
            lastStart = new(attacks, attackId, direction, Time.GetTicksMsec(),
                Engine.GetPhysicsFrames(), Engine.GetProcessFrames(), input.CurrentFrameSequence);
        }
        combat.AttackStarted += Attacked;
        health.Damaged += Damaged;
        health.Healed += Healed;
        try
        {
#if REFCOUNTED_FATAL_IDENTITY
            ObserveAssignedStartupResources(level,"descent.after-startup-bindings");
#endif
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            for (int frame = 0; frame < 60 && flow.State != PrologueGameFlowState.Playing; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "real isolated startup completes");
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            var sector = level.CurrentSectorRoot;
            var floor = sector.GetNode<StaticBody2D>("Gameplay/Collision/RiftLowerCatch");
            var target = sector.GetNode<GrappleSurface2D>("Gameplay/Collision/RiftMiddleCatch/GrappleSurfaceProfile");
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            ulong playerId = player.GetInstanceId();
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonDescent && Supported(player, floor) &&
                player.GlobalPosition.DistanceTo(new Vector2(1600, 1040)) < 1,
                "saved V2 second-layer player has real LowerCatch support, not a legacy first-shelf arrangement");
            var bodyCollider = player.GetNode<CollisionShape2D>("BodyCollider");
            var capsule = (CapsuleShape2D)bodyCollider.Shape;
            var floorCollider = floor.GetNode<CollisionShape2D>("Collision");
            var floorRectangle = (RectangleShape2D)floorCollider.Shape;
            TestAssert.True(!floorCollider.Disabled && Mathf.IsZeroApprox(floorCollider.GlobalRotation) &&
                floorCollider.GlobalScale.IsEqualApprox(Vector2.One) && bodyCollider.Position.IsZeroApprox() &&
                Mathf.IsZeroApprox(bodyCollider.GlobalRotation) && bodyCollider.GlobalScale.IsEqualApprox(Vector2.One),
                "support geometry uses live enabled unrotated unit-scale colliders and a centered actual capsule");
            Rect2 floorBounds = new(floorCollider.GlobalPosition - floorRectangle.Size * .5f, floorRectangle.Size);
            TestAssert.True(floorBounds.Position.IsEqualApprox(new Vector2(1400, 1076)) &&
                floorBounds.Size.IsEqualApprox(new Vector2(700, 24)) &&
                Mathf.IsEqualApprox(capsule.Height, 72) && Mathf.IsEqualApprox(capsule.Radius, 20) &&
                player.GlobalPosition.X - capsule.Radius > floorBounds.Position.X &&
                player.GlobalPosition.X + capsule.Radius < floorBounds.End.X &&
                Mathf.Abs(player.GlobalPosition.Y + capsule.Height * .5f - floorBounds.Position.Y) < .5f,
                "complete saved 72x40 capsule lies within actual LowerCatch bounds and touches its top1076");
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective, "real first attack lesson");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 45);
            damageStage = "first-J";
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 30);
            TestAssert.True(attacks == 1 && completion.CurrentObjective == PrologueObjective.LearnGrappleMode,
                "accepted physical J advances the attack lesson, not a Q/E mode event");
            damageStage = "first-attach";
            KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            AssertUpTarget(player, claw, target, 880);
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 45);
            await Attach(tree, input, claw, target);
            await KeyboardActionRuntimeTests.Frames(tree, input, presentation ? 45 : 3);
            TestAssert.Equal(PrologueObjective.ReachBasin, completion.CurrentObjective,
                "real live-target K attachment naturally completes grapple teaching");
            damageStage = "first-cancel-land";
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            await GroundedIdle(tree, input, player, claw, floor);
            guidance.SetLanguage(GameLanguage.English);
            await Settle(tree);
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            Vector2 screen = player.GetGlobalTransformWithCanvas() * player.ToLocal(new Vector2(player.GlobalPosition.X, 880));
            TestAssert.True(player.GetViewportRect().Grow(-48).HasPoint(screen), "actual V2 underside is inside prompt viewport inset");
            bool offered = DescentClawGuidance.TryResolve(sector, player, out Vector2 point);
            TestAssert.True(offered && Mathf.Abs(point.Y - 880) < .5f &&
                guidance.CurrentInstruction.Targets.Count == 1 &&
                guidance.CurrentInstruction.Targets[0] == PrologueGuidanceTargetKey.DescentClawOpportunity &&
                visual.HasVisiblePrompt && visual.RenderedTargetCount == 1,
                "DESCENT_V2_KEYBOARD_OPPORTUNITY_RED: supported current V2 ceiling must automatically offer real W/K opportunity");
            AssertOpportunity(level, "saved V2 local opportunity");
            GD.Print($"DESCENT_V2_OPPORTUNITY_OK player={player.GlobalPosition} point={point} screen={screen} " +
                $"same_player={playerId} prior_real_attachment=true first_shelf=false");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 60);
            else
            {
                damageStage = "guidance-boundaries-and-second-J";
                await Boundaries(tree, level, input, claw, target, () => lastStart, lateContinuation);
                damageStage = "held-shield-natural-overheat";
                await HeldShield(tree, level, input);
            }
            damageStage = "second-attach-and-pull";
            float beforeY = player.GlobalPosition.Y;
            await Attach(tree, input, claw, target);
            for (int tick = 0; tick < 60 && beforeY - player.GlobalPosition.Y < 24; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(beforeY - player.GlobalPosition.Y >= 24 && IsAttached(claw.CurrentFrame.State),
                "real K produces native upward pull on saved terrain, no injected body or claw state");
            await Settle(tree);
            AssertCard(level, PrologueGuidanceTargetKey.ClawAttachment, "actual attachment");
            string attachedText = guidance.CurrentInstruction.StepText;
            TestAssert.True(attachedText.Contains("auto-pull", StringComparison.OrdinalIgnoreCase) &&
                attachedText.Contains("A/D") && attachedText.Contains("SPACE", StringComparison.OrdinalIgnoreCase) &&
                attachedText.Contains("K AGAIN: CANCEL") && attachedText.Contains("J/L: END CLAW"),
                "attachment retains auto-pull, optional swing, anytime Space, K cancel and J/L end meanings");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 60);
            var states = new List<MechanicalClawState>();
            void Released(MechanicalClawFrame frame, GrappleSurface2D _) => states.Add(frame.State);
            int jumps = motor.AcceptedJumpCount;
            damageStage = "attached-Space-release";
            claw.ClawFrameChanged += Released;
            try
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
                await KeyboardActionRuntimeTests.Frames(tree, input, 2);
                KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            }
            finally { claw.ClawFrameChanged -= Released; }
            int boost = states.IndexOf(MechanicalClawState.Boost);
            TestAssert.True(boost >= 0 && states.IndexOf(MechanicalClawState.Released) == boost + 1 &&
                motor.AcceptedJumpCount == jumps,
                "attached physical Space publishes Boost then Released, never an extra ordinary jump");
            damageStage = "release-grounded-idle";
            await GroundedIdle(tree, input, player, claw, floor);
            damageStage = "terminal-settle";
            await Settle(tree);
            int terminalHp = player.GetNode<HealthComponent>("Health").CurrentHealth;
            int terminalMask = TerminalFailureMask(player.GetInstanceId(), playerId, requests,
                recoveries, terminalHp, completion.CurrentObjective);
            GD.Print($"DESCENT_TERMINAL_OBSERVATION mask={terminalMask} " +
                $"player={player.GetInstanceId()} expected_player={playerId} requests={requests} recoveries={recoveries} " +
                $"hp={terminalHp} objective={completion.CurrentObjective} physics={Engine.GetPhysicsFrames()} " +
                $"process={Engine.GetProcessFrames()} input_sequence={input.CurrentFrameSequence} " +
                $"position={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} " +
                $"claw={claw.CurrentFrame.State} landings={motor.LandingCount} fall_height={motor.LastFallHeight} " +
                $"fall_damage={motor.LastFallDamage} fall_applied={motor.LastFallDamageApplied} " +
                $"impact_speed={motor.LastLandingImpact.Speed} impact_reason={motor.LastLandingImpact.Reason} " +
                $"impact_reliable={motor.LastLandingImpact.IsReliable} diagnostic_only=true original_assert_unchanged=true");
            TestAssert.True(player.GetInstanceId() == playerId && requests == 0 && recoveries == 0 &&
                player.GetNode<HealthComponent>("Health").CurrentHealth == 5 &&
                completion.CurrentObjective == PrologueObjective.ReachBasin,
                "same default-health player naturally lands without recovery or objective injection");
            TestAssert.True(damageObservationFailure == null && omittedDamageRows == 0,
                "accepted damage observation must be complete, not fabricated or truncated: " + damageObservationFailure);
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 45);
            GD.Print($"DESCENT_CLAW_GUIDANCE_RUNTIME_OK layout=2 keyboard=true native_pull_release=true " +
                $"presentation={presentation} recoveries={requests}/{recoveries} hp=5");
        }
        finally
        {
            health.Damaged -= Damaged;
            health.Healed -= Healed;
            GD.Print("DESCENT_DAMAGE_OBSERVER_SUMMARY " + System.Text.Json.JsonSerializer.Serialize(new
            {
                DamageEvents = damageRows, HealEvents = healRows, Omitted = omittedDamageRows,
                Failure = damageObservationFailure, OriginalHpGate = true, ReadOnly = true, Human = false
            }));
            foreach (Key key in new[] { Key.J, Key.K, Key.W, Key.L, Key.Space })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            combat.AttackStarted -= Attacked;
            level.RecoveryCompleted -= Recovered;
            level.RecoveryRequested -= Requested;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static async Task Boundaries(SceneTree tree, PrologueLevelController level, PlayerInputReader input,
        MechanicalClawController claw, GrappleSurface2D target, Func<AttackStartObservation> readStart,
        bool lateContinuation)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        await ReboundMoveUpOpportunity(tree, level, input, claw, target);
        foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
        {
            guidance.SetLanguage(language);
            await Settle(tree);
            AssertOpportunity(level, "language " + language);
            GrappleSurfaceMaterial originalMaterial = target.Material;
            target.Material = GrappleSurfaceMaterial.Ice;
            try
            {
                await Settle(tree);
                TestAssert.True(claw.KeyboardPreview.Surface != target, "actual W preview rejects Ice");
                AssertFallback(level, "Ice surface");
            }
            finally { target.Material = originalMaterial; await Settle(tree); }
        }
        target.Visible = false;
        try
        {
            await Settle(tree);
            AssertAxisTarget(level.Player, target, 880);
            TestAssert.True(!target.IsVisibleInTree(), "hidden negative case retains its real physical ceiling");
            AssertFallback(level, "hidden target");
        }
        finally { target.Visible = true; await Settle(tree); }
        var body = target.GetParent<StaticBody2D>();
        Vector2 original = body.Position;
        body.Position = original + Vector2.Down * 120;
        try
        {
            await Settle(tree);
            AssertUpTarget(level.Player, claw, target, 1000);
            CapsuleShape2D capsule = (CapsuleShape2D)level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            MechanicalClawSettings settings = MechanicalClawSettings.Default;
            Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(claw.KeyboardPreview.Snapshot.WorldAnchor,
                Vector2.Down, capsule, settings.MinimumRopeLength, settings.ArrivalMargin);
            TestAssert.True(Mathf.Abs(dock.Y - 1048) < .5f && dock.Y > level.Player.GlobalPosition.Y &&
                Mathf.Abs(level.Player.GlobalPosition.Y - capsule.Height * .5f - 1000 - 4) < .5f,
                "hittable low roof has four pixels head clearance but its actual dock1048 is below player1040");
            Vector2 screen = level.Player.GetGlobalTransformWithCanvas() *
                level.Player.ToLocal(new Vector2(level.Player.GlobalPosition.X, 1000));
            TestAssert.True(level.Player.GetViewportRect().Grow(-48).HasPoint(screen),
                "unusable low roof remains visible, not merely offscreen");
            AssertFallback(level, "native hittable roof with no upward dock space");
        }
        finally { body.Position = original; await Settle(tree); }

        // A real body-side obstruction, clear of the current capsule and the W axis.
        var blocker = new StaticBody2D { Name = "GuidanceBodyFlankBlocker", Position = new Vector2(1618, 970),
            CollisionLayer = CombatCollisionLayers.World };
        blocker.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(8, 8) } });
        level.AddChild(blocker);
        try
        {
            await Settle(tree);
            AssertUpTarget(level.Player, claw, target, 880);
            var capsule = (CapsuleShape2D)level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            MechanicalClawSettings settings = MechanicalClawSettings.Default;
            Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(claw.KeyboardPreview.Snapshot.WorldAnchor,
                Vector2.Down, capsule, settings.MinimumRopeLength, settings.ArrivalMargin);
            Vector2 displacement = dock - level.Player.GlobalPosition;
            TestAssert.True(Mathf.Abs(displacement.X) < .5f && Mathf.Abs(displacement.Y + 112) < .5f,
                "full current-capsule dock sweep is independently112 pixels upward");
            using var collision = new KinematicCollision2D();
            TestAssert.True(level.Player.TestMove(level.Player.GlobalTransform, displacement, collision,
                level.Player.SafeMargin, false) && collision.GetCollider() == blocker,
                "native actual-body query hits flank blocker despite unobstructed W target ray");
            AssertFallback(level, "body-side blocker");
        }
        finally { blocker.QueueFree(); await Settle(tree); }
        AssertOpportunity(level, "native body path restored");
        AttackStartObservation baselineStart = readStart();
        ObservationClock command = ClockForObservation(input);
        var combat = level.Player.GetNode<PlayerCombat>("Combat");
        var modes = level.Player.GetNode<PlayerActionModeController>("ActionModes");
        var model = ReadObservationField<PlayerCombatModel>(combat, "_model");
        var windowVisual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Label label = windowVisual.GetNode<Label>("Text");
        var physicsWindows = new List<AttackWindowObservation>();
        var processWindows = new List<AttackCardObservation>();
        int physicsSamples = 0, processSamples = 0;
        string observationFailure = null;
        AttackWindowObservation CaptureWindow() => new(ClockForObservation(input), readStart(),
            ReadModelForObservation(model), combat.IsAttacking, modes.CanBeginGrapple);
        Action observePhysics = () =>
        {
            try
            {
                AttackWindowObservation sample = CaptureWindow();
                if (!IsSecondAttackWindow(sample, baselineStart)) return;
                physicsSamples++;
                physicsWindows.Add(sample);
            }
            catch (Exception error) { observationFailure ??= error.ToString(); }
        };
        Action observeProcess = () =>
        {
            try
            {
                AttackWindowObservation sample = CaptureWindow();
                if (!IsSecondAttackWindow(sample, baselineStart) || sample.Clock.Process <= sample.Start.Process) return;
                var instruction = guidance.CurrentInstruction;
                var card = new AttackCardObservation(sample, guidance.IsProcessing() && guidance.CanProcess(),
                    instruction.Visible, windowVisual.HasVisiblePrompt, windowVisual.RenderedTextContained,
                    instruction.Targets.Count, instruction.Targets.Count > 0 ? instruction.Targets[0] : (PrologueGuidanceTargetKey)(-1),
                    windowVisual.RenderedTargetCount, instruction.StepText, windowVisual.RenderedStepText, label.Text);
                processSamples++;
                processWindows.Add(card);
            }
            catch (Exception error) { observationFailure ??= error.ToString(); }
        };
        var observer = CreateWindowObserver(level, guidance, node =>
        { node.ObservePhysics = observePhysics; node.ObserveProcess = observeProcess; });
        try
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            await Settle(tree);
            if (lateContinuation)
            {
                // Relocate, do not increase, the original completion wait.
                await KeyboardActionRuntimeTests.Frames(tree, input, 30);
                AttackWindowObservation ended = CaptureWindow();
                TestAssert.True(ended.Start.Count == baselineStart.Count + 1 && ended.Start.AttackId != 0 &&
                    ended.Model.ActiveAttackId == ended.Start.AttackId && !ended.Model.Attacking &&
                    !ended.IsAttacking && ended.Model.Elapsed >= ended.Model.Duration,
                    "late-continuation control uses the same naturally completed real second J");
                GD.Print("DESCENT_SECOND_J_LATE_CONTINUATION_SETUP same_real_attack=true naturally_ended=true moved_original_wait=30 extra_attacks=0");
            }
            ObserveSecondJ(tree, level, input, claw, readStart, baselineStart, command);
            AttackWindowObservation continuation = CaptureWindow();
            await CompleteWindowObservation(
                () => lateContinuation ? Task.CompletedTask : KeyboardActionRuntimeTests.Frames(tree, input, 30),
                () => { observer.ObservePhysics = null; observer.ObserveProcess = null; },
                () => ValidateWindowEvidence(physicsWindows, processWindows, baselineStart, command,
                    observationFailure, guidance.Language));
            AttackWindowObservation physics = physicsWindows[0];
            AttackCardObservation card = processWindows[0];
            AttackWindowObservation finished = CaptureWindow();
            TestAssert.True(finished.Start.Count == baselineStart.Count + 1 &&
                finished.Start.AttackId == physics.Start.AttackId && finished.Model.ActiveAttackId == physics.Start.AttackId &&
                !finished.Model.Attacking && !finished.IsAttacking && finished.Model.Elapsed >= finished.Model.Duration &&
                !IsSecondAttackWindow(finished, baselineStart),
                "same naturally finished second attack is rejected by the SAME live-window eligibility rule");
            GD.Print("DESCENT_SECOND_J_WINDOW_EVIDENCE " + System.Text.Json.JsonSerializer.Serialize(new
            { Command = command, Physics = physics, Process = card, Continuation = continuation, Finished = finished }));
            GD.Print($"DESCENT_SECOND_J_WINDOW_OK physics_samples={physicsSamples} process_samples={processSamples} " +
                $"same_id=true live_authority=true post_guidance=true continuation_ended={(!continuation.IsAttacking).ToString().ToLowerInvariant()} " +
                "late_rejected=true natural_end=true extra_attacks=0");
            if (lateContinuation)
                GD.Print("SECOND_J_NATIVE_LATE_WINDOW_CONTROL_OK real_second_j=true earlier_live_receipts=true ended_snapshot_rejected=true total_completion_wait=30 extra_j=0");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            observer.ObservePhysics = null;
            observer.ObserveProcess = null;
            observer.QueueFree();
        }
        AssertOpportunity(level, "attack finished");
        guidance.SetGuidanceEnabled(false);
        try
        {
            await Settle(tree);
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(!guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt &&
                visual.RenderedTargetCount == 0, "disabled guidance clears actual card and marker");
        }
        finally { guidance.SetGuidanceEnabled(true); await Settle(tree); }
        AssertOpportunity(level, "guidance restored");
    }

    private static async Task ReboundMoveUpOpportunity(SceneTree tree, PrologueLevelController level,
        PlayerInputReader input, MechanicalClawController claw, GrappleSurface2D target)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Label label = visual.GetNode<Label>("Text");
        var originalEvents = new List<InputEvent>(InputMap.ActionGetEvents(InputActions.MoveUp));
        GameLanguage originalLanguage = guidance.Language;
        bool originalW = Input.IsPhysicalKeyPressed(Key.W), originalT = Input.IsPhysicalKeyPressed(Key.T);
        bool originalMoveUp = Input.IsActionPressed(InputActions.MoveUp);
        try
        {
            // Release W while its original map is still present; never carry its action state into T.
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.T, false);
            await Settle(tree);
            TestAssert.True(!Input.IsActionPressed(InputActions.MoveUp),
                "DESCENT_MOVE_UP_REBIND: original W release clears the sampled action before remapping");
            InputMap.ActionEraseEvents(InputActions.MoveUp);
            InputMap.ActionAddEvent(InputActions.MoveUp, new InputEventKey { PhysicalKeycode = Key.T });
            KeyboardActionRuntimeTests.KeyEvent(Key.T, true);
            foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
            {
                guidance.SetLanguage(language);
                await Settle(tree);
                TestAssert.True(Input.IsPhysicalKeyPressed(Key.T) && !Input.IsPhysicalKeyPressed(Key.W) &&
                    Input.IsActionPressed(InputActions.MoveUp) && input.Current.Aim.IsEqualApprox(Vector2.Up),
                    "DESCENT_MOVE_UP_REBIND: physical T supplies the real sampled upward aim " + language);
                AssertUpTarget(level.Player, claw, target, 880);
                Vector2 screen = level.Player.GetGlobalTransformWithCanvas() *
                    level.Player.ToLocal(new Vector2(level.Player.GlobalPosition.X, 880));
                TestAssert.True(target.IsVisibleInTree() && label.IsVisibleInTree() &&
                    level.Player.GetViewportRect().Grow(-48).HasPoint(screen),
                    "DESCENT_MOVE_UP_REBIND: actual ceiling and Label remain visible inside the prompt inset " + language);
                AssertCard(level, PrologueGuidanceTargetKey.DescentClawOpportunity, "rebound move_up " + language);
                string prefix = language == GameLanguage.English ? "T: AIM UP" : "T 瞄准";
                string[] meanings = language == GameLanguage.English
                    ? new[] { "ROUGH CEILING", "DIAMOND THEN K", "HIT: auto-pull", "A/D: DESCEND" }
                    : new[] { "粗糙顶面", "K 发爪", "命中：自动牵引", "A/D：继续下降" };
                foreach (var (surface, text) in new[]
                    { ("RenderedStepText", visual.RenderedStepText), ("Label.Text", label.Text) })
                {
                    TestAssert.True(text.Contains(prefix, StringComparison.Ordinal),
                        $"DESCENT_MOVE_UP_REBIND: {language} {surface} must contain literal '{prefix}' after physical T binding");
                    TestAssert.True(!text.Contains("W 瞄准", StringComparison.Ordinal) &&
                        !text.Contains("W: AIM UP", StringComparison.Ordinal) && !text.Contains('{') && !text.Contains('}'),
                        $"DESCENT_MOVE_UP_REBIND: {language} {surface} leaks neither old W aim prefixes nor unresolved tokens");
                    foreach (string meaning in meanings)
                        TestAssert.True(text.Contains(meaning, StringComparison.Ordinal),
                            $"DESCENT_MOVE_UP_REBIND: {language} {surface} retains '{meaning}'");
                }
            }
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.T, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            InputMap.ActionEraseEvents(InputActions.MoveUp);
            foreach (InputEvent binding in originalEvents) InputMap.ActionAddEvent(InputActions.MoveUp, binding);
            guidance.SetLanguage(originalLanguage);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, originalW);
            KeyboardActionRuntimeTests.KeyEvent(Key.T, originalT);
            await Settle(tree);
        }
        var restoredEvents = InputMap.ActionGetEvents(InputActions.MoveUp);
        TestAssert.Equal(originalEvents.Count, restoredEvents.Count, "DESCENT_MOVE_UP_REBIND: restores every original MoveUp event");
        for (int index = 0; index < originalEvents.Count; index++)
            TestAssert.Equal(originalEvents[index].GetInstanceId(), restoredEvents[index].GetInstanceId(),
                "DESCENT_MOVE_UP_REBIND: restores original MoveUp event identity and order " + index);
        TestAssert.True(guidance.Language == originalLanguage && Input.IsPhysicalKeyPressed(Key.W) == originalW &&
            Input.IsPhysicalKeyPressed(Key.T) == originalT && Input.IsActionPressed(InputActions.MoveUp) == originalMoveUp,
            "DESCENT_MOVE_UP_REBIND: restores original language and W/T/MoveUp held states");
        AssertUpTarget(level.Player, claw, target, 880);
        AssertOpportunity(level, "default W opportunity after move_up restoration");
        GD.Print("DESCENT_MOVE_UP_REBIND_HEADLESS_TEXT_OK languages=zh,en binding=T mounted_scene=true " +
            "rendered_and_label=true map_identity_order_restored=true language_held_restored=true default_W_restored=true pixels=false");
    }

    private static async Task HeldShield(SceneTree tree, PrologueLevelController level, PlayerInputReader input)
    {
        var shield = level.Player.GetNode<PlayerShieldController>("Shield");
        var energy = level.Player.GetNode<PlayerEnergyController>("Energy");
        KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
        try
        {
            await Settle(tree);
            TestAssert.True(input.Current.ShieldHeld && shield.IsActive, "real L starts sustained guard");
            AssertFallback(level, "held active L");
            for (int tick = 0; tick < 300 && !energy.IsOverheated; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            await Settle(tree);
            TestAssert.True(energy.IsOverheated && !shield.IsActive && input.Current.ShieldHeld,
                "natural overheat keeps real L held while shield becomes inactive");
            AssertFallback(level, "inactive overheated shield with L held");
        }
        finally { KeyboardActionRuntimeTests.KeyEvent(Key.L, false); await Settle(tree); }
        AssertOpportunity(level, "L released after natural overheat");
        KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
        try
        {
            await Settle(tree);
            TestAssert.True(input.Current.ShieldHeld, "second physical L is independently sampled without resetting energy");
            AssertFallback(level, "second held L");
        }
        finally { KeyboardActionRuntimeTests.KeyEvent(Key.L, false); await Settle(tree); }
        AssertOpportunity(level, "second L release");
    }

    private static void AssertUpTarget(CharacterBody2D player, MechanicalClawController claw,
        GrappleSurface2D target, float expectedY)
    {
        var preview = claw.KeyboardPreview;
        TestAssert.True(preview.Surface == target && preview.Snapshot.Exists && preview.Snapshot.HasLineOfSight &&
            preview.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Down) &&
            Mathf.Abs(preview.Snapshot.WorldAnchor.Y - expectedY) < .5f,
            "actual upward keyboard preview is the saved underside at independently derived Y=" + expectedY);
        AssertAxisTarget(player, target, expectedY);
    }

    private static void AssertAxisTarget(CharacterBody2D player, GrappleSurface2D target, float expectedY)
    {
        using var query = PhysicsRayQueryParameters2D.Create(player.GlobalPosition,
            new Vector2(player.GlobalPosition.X, expectedY - 1));
        query.CollisionMask = CombatCollisionLayers.World;
        query.CollideWithAreas = false;
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        TestAssert.True(hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == target.GetParent() &&
            Mathf.Abs(hit["position"].AsVector2().Y - expectedY) < .5f,
            "independent native axis first hit agrees with keyboard underside");
    }

    private static async Task Attach(SceneTree tree, PlayerInputReader input, MechanicalClawController claw,
        GrappleSurface2D target)
    {
        KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
        for (int tick = 0; tick < 60 && !IsAttached(claw.CurrentFrame.State); tick++)
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
        KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
        TestAssert.True(IsAttached(claw.CurrentFrame.State) && claw.CurrentTarget == target &&
            claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId(),
            "physical K attaches to the exact shown live terrain target");
        await KeyboardActionRuntimeTests.Frames(tree, input, 1);
    }

    private static async Task GroundedIdle(SceneTree tree, PlayerInputReader input, CharacterBody2D player,
        MechanicalClawController claw, StaticBody2D floor)
    {
        int stable = 0;
        for (int tick = 0; tick < 180 && stable < 6; tick++)
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            stable = Supported(player, floor) && claw.CurrentFrame.State == MechanicalClawState.Idle ? stable + 1 : 0;
        }
        TestAssert.Equal(6, stable, "natural six-sample supported Idle on actual LowerCatch");
    }

    private static bool Supported(CharacterBody2D player, StaticBody2D floor)
    {
        for (int index = 0; player.IsOnFloor() && index < player.GetSlideCollisionCount(); index++)
        {
            var contact = player.GetSlideCollision(index);
            if (contact.GetCollider() == floor && contact.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private static bool IsAttached(MechanicalClawState state) =>
        state is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;

    private static void AssertOpportunity(PrologueLevelController level, string stage)
    {
        AssertCard(level, PrologueGuidanceTargetKey.DescentClawOpportunity, stage);
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        string text = guidance.CurrentInstruction.StepText;
        TestAssert.True(HasOpportunityMeanings(text, guidance.Language),
            "complete rough/W/diamond/K/auto-pull/optional-descent meanings: " + stage);
    }

    private static bool HasOpportunityMeanings(string text, GameLanguage language) =>
        language == GameLanguage.English
            ? text.Contains("ROUGH CEILING") && text.Contains("W: AIM UP") && text.Contains("DIAMOND THEN K") &&
                text.Contains("HIT: auto-pull") && text.Contains("A/D: DESCEND")
            : text.Contains("粗糙顶面") && text.Contains("W 瞄准") && text.Contains("K 发爪") &&
                text.Contains("命中：自动牵引") && text.Contains("A/D：继续下降");

    private static void AssertFallback(PrologueLevelController level, string stage)
    {
        AssertCard(level, PrologueGuidanceTargetKey.DescentIce, stage);
        string text = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction.StepText;
        TestAssert.True(text.Contains("冰") && text.Contains("不可抓") && text.Contains("A/D") ||
            text.Contains("ICE", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("NO GRIP", StringComparison.OrdinalIgnoreCase) && text.Contains("A/D: DESCEND"),
            "ineligible opportunity restores the actual Ice cannot-grab warning: " + stage);
    }

    private static void AssertCard(PrologueLevelController level, PrologueGuidanceTargetKey expected, string stage)
    {
        var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        GD.Print($"DESCENT_V2_CARD stage={stage} target={string.Join(",", instruction.Targets)} " +
            $"rendered={visual.RenderedTargetCount} text={instruction.StepText}");
        TestAssert.True(instruction.Visible && visual.HasVisiblePrompt && visual.RenderedTextContained &&
            instruction.Targets.Count == 1 && instruction.Targets[0] == expected &&
            visual.RenderedTargetCount == 1 && visual.RenderedStepText == instruction.StepText,
            "automatic single rendered complete card matches " + expected + ": " + stage);
    }

    private static async Task Settle(SceneTree tree)
    {
        for (int frame = 0; frame < 2; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}

// Test-owned value observer. Never issue input, refresh guidance or throw from
// native callbacks; the awaited fixture validates captured facts and errors.
internal sealed partial class DescentAttackWindowObserver : Node
{
    internal Action ObservePhysics;
    internal Action ObserveProcess;
    public override void _PhysicsProcess(double delta) => ObservePhysics?.Invoke();
    public override void _Process(double delta) => ObserveProcess?.Invoke();
}
