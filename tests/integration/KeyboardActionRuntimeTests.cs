using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Abilities;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class KeyboardActionRuntimeTests
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static System.Exception CaptureFinalizerFailure(System.Exception error)
    {
        try { throw error; }
        catch (System.Exception captured) { return captured; }
    }

    private static async Task AssertRouteFinalizationFailures()
    {
        var failures=new System.Collections.Generic.List<string>(); int cases=0;
        void Check(string name,bool condition)
        { cases++; if(!condition) failures.Add(name); }
        async Task<System.Exception> Observe(System.Exception primary,params System.Func<Task>[] steps)
        {
            try { await PrologueCanyonGeneratedMainRouteTests.Task10FinalizeRoute(primary,steps); return null; }
            catch(System.Exception error) { return error; }
        }
        var order=new System.Collections.Generic.List<int>();
        var success=await Observe(null,()=>{order.Add(1);return Task.CompletedTask;},
            async()=>{await Task.Yield();order.Add(2);},()=>{order.Add(3);return Task.CompletedTask;});
        Check("success retains ordered exactly-once cleanup",success==null && string.Join(",",order)=="1,2,3");
        var primary=CaptureFinalizerFailure(new System.InvalidOperationException("actual route sentinel"));
        string originalStack=primary.StackTrace;
        var primaryOnly=await Observe(primary);
        Check("primary-only never becomes success",ReferenceEquals(primaryOnly,primary));
        Check("primary stack retains its original throwing frame",primaryOnly?.StackTrace!=null &&
            primaryOnly.StackTrace.StartsWith(originalStack,System.StringComparison.Ordinal));
        var audio=CaptureFinalizerFailure(new System.TimeoutException("AudioServer release sentinel"));
        string audioStack=audio.StackTrace;
        var cleanupOnly=await Observe(null,()=>Task.FromException(audio));
        Check("single cleanup keeps same exception and original stack",ReferenceEquals(cleanupOnly,audio) &&
            cleanupOnly.StackTrace.StartsWith(audioStack,System.StringComparison.Ordinal));
        bool persistence=false,driver=false;
        var pair=await Observe(primary,()=>throw audio,
            ()=>{persistence=true;return Task.CompletedTask;},()=>{driver=true;return Task.CompletedTask;}) as System.AggregateException;
        Check("primary plus audio both survive in original order",pair?.InnerExceptions.Count==2 &&
            ReferenceEquals(pair.InnerExceptions[0],primary) && ReferenceEquals(pair.InnerExceptions[1],audio));
        Check("audio failure cannot skip persistence or driver finalization",persistence && driver);
        var asyncError=new System.IO.IOException("async cleanup sentinel");
        order.Clear();
        var multiple=await Observe(primary,
            ()=>{order.Add(1);throw audio;},
            async()=>{await Task.Yield();order.Add(2);throw asyncError;},
            ()=>{order.Add(3);return Task.CompletedTask;}) as System.AggregateException;
        Check("sync and async cleanup failures all retained",multiple?.InnerExceptions.Count==3 &&
            ReferenceEquals(multiple.InnerExceptions[0],primary) && ReferenceEquals(multiple.InnerExceptions[1],audio) &&
            ReferenceEquals(multiple.InnerExceptions[2],asyncError) && string.Join(",",order)=="1,2,3");
        var cleanups=await Observe(null,()=>throw audio,()=>throw asyncError) as System.AggregateException;
        Check("successful route still fails for multiple cleanup faults",cleanups?.InnerExceptions.Count==2 &&
            ReferenceEquals(cleanups.InnerExceptions[0],audio) && ReferenceEquals(cleanups.InnerExceptions[1],asyncError));
        var nested=new System.AggregateException(primary,asyncError);
        var unflattened=await Observe(nested,()=>throw audio) as System.AggregateException;
        Check("already-aggregate primary is not flattened or replaced",unflattened?.InnerExceptions.Count==2 &&
            ReferenceEquals(unflattened.InnerExceptions[0],nested) && ReferenceEquals(unflattened.InnerExceptions[1],audio));
        foreach(bool early in new[]{true,false})
        {
            int calls=0;
            async Task<int> ReturnThroughFinalizer()
            {
                try { if(early)return 7; await Task.Yield(); return 9; }
                finally { await PrologueCanyonGeneratedMainRouteTests.Task10FinalizeRoute(null,
                    new System.Func<Task>[]{()=>{calls++;return Task.CompletedTask;}}); }
            }
            int result=await ReturnThroughFinalizer();
            Check($"controlled return{early} retains result and cleanup",result==(early?7:9) && calls==1);
        }
        TestAssert.True(failures.Count==0,"TASK10_FINALIZATION_CHAIN_RED: "+string.Join("; ",failures));
        GD.Print($"TASK10_FINALIZATION_CHAIN_OK cases={cases} exact_exception_objects=true ordered_cleanup=true " +
            "controlled_failures=true actual_audio_fix=false route_accepted=false");
    }

    private static async Task RunAttackEnergyBoundary(SceneTree tree)
    {
        // Q-ATTACK-ENERGY-BOUNDARY-01: epsilon/Approved-based teaching must not
        // disagree with the real saved player's captured tuning or J expenditure.
        (string Name, float Max, float Cost, float Drain, float Target, bool Affordable)[] cases =
        {
            ("approved_below", 100f, 12f, 88.00005f, 11.99995f, false),
            ("approved_exact", 100f, 12f, 88f, 12f, true),
            ("approved_above", 100f, 12f, 87.99995f, 12.00005f, true),
            ("alternate_below", 64f, 16f, 48.00005f, 15.99995f, false),
            ("alternate_exact", 64f, 6f, 58f, 6f, true),
            ("zero_cost_empty", 64f, 0f, 64f, 0f, true)
        };
        foreach (var row in cases)
        {
            var root = new Node2D { Name = "AttackEnergyBoundaryFixture" };
            var ownedTuning = new Knight.Config.PlayerEnergyTuning { MaxEnergy = row.Max, AttackCost = row.Cost };
            KeyboardRouteInputDriver driver = null;
            try
            {
                var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
                floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(800, 20) } });
                root.AddChild(floor);
                var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
                player.Position = new Vector2(0, 14);
                var energy = player.GetNode<PlayerEnergyController>("Energy");
                var sharedTuning = energy.Tuning;
                var sharedSettings = sharedTuning.ToSettings();
                energy.Tuning = ownedTuning; // Before Ready captures settings; never mutate the saved resource.
                root.AddChild(player);
                tree.Root.AddChild(root);
                var input = player.GetNode<PlayerInputReader>("Input");
                var combat = player.GetNode<PlayerCombat>("Combat");
                var modes = player.GetNode<PlayerActionModeController>("ActionModes");
                var shield = player.GetNode<PlayerShieldController>("Shield");
                int attacks = 0, rejects = 0, rejectionChanges = 0, energyChanges = 0;
                PlayerActionRejectionReason rejectedReason = PlayerActionRejectionReason.None;
                PlayerEnergyFrame observed = default;
                combat.AttackStarted += (_, _) => attacks++;
                modes.PrimaryRejected += reason => { rejects++; rejectedReason = reason; };
                modes.RejectionChanged += _ => rejectionChanges++;
                energy.EnergyFrameChanged += frame => { observed = frame; energyChanges++; };
                driver = KeyboardRouteInputDriver.Begin(player);
                await Frames(tree, input, 8);
                TestAssert.True(player.IsOnFloor() && input.Current.KeyboardActions && modes.ActiveSlot == 1 &&
                    !combat.IsAttacking && !shield.IsActive && !input.Current.ShieldHeld,
                    $"attack-energy fixture is naturally supported and otherwise attack-ready: {row.Name}");
                float start = energy.EnergyRatio * row.Max;
                TestAssert.True(start == row.Max && ownedTuning.RegenDelaySeconds == .55f &&
                    !ReferenceEquals(sharedTuning, ownedTuning), "attack-energy fixture starts full with isolated tuning");
                TestAssert.True(energy.TrySpendHeavyGuard(row.Drain),
                    $"public heavy-guard expenditure controls the boundary: {row.Name} start={start:R} drain={row.Drain:R}");
                float drained = observed.State.Energy;
                string precision = $"case={row.Name} max={row.Max:R} cost={row.Cost:R} start={start:R} " +
                    $"drain={row.Drain:R} target={row.Target:R} actual={drained:R} ratio_energy={energy.EnergyRatio * row.Max:R}";
                bool controlled = row.Name switch
                {
                    "approved_below" => drained < 12f && drained >= 11.9999f,
                    "approved_exact" => drained == 12f,
                    "approved_above" => drained > 12f && drained <= 12.0001f,
                    "alternate_below" => drained < 16f && drained >= 15.9999f,
                    "alternate_exact" => drained == 6f,
                    "zero_cost_empty" => drained == 0f,
                    _ => false
                };
                TestAssert.True(energyChanges > 0 && controlled && Mathf.Abs(drained - row.Target) < .00001f &&
                    observed.State.RegenDelayRemaining == .55f, "attack-energy precision fixture: " + precision);

                var failures = new System.Collections.Generic.List<string>();
                void Check(string name, bool condition) { if (!condition) failures.Add(name); }
                float ratioBeforePoll = energy.EnergyRatio;
                PlayerEnergyState stateBeforePoll = observed.State;
                int changesBeforePoll = energyChanges;
                bool canAfford = modes.CanAffordAttack, canBegin = modes.CanBeginAttack;
                bool pollsAgree = true;
                for (int poll = 0; poll < 16; poll++)
                {
                    bool polledAfford = modes.CanAffordAttack, polledBegin = modes.CanBeginAttack;
                    pollsAgree &= polledAfford == row.Affordable && polledBegin == row.Affordable;
                }
                Check("readonly affordability agrees with literal expected budget", pollsAgree);
                Check("readonly polling neither spends nor publishes changes", energy.EnergyRatio == ratioBeforePoll &&
                    observed.State == stateBeforePoll && energyChanges == changesBeforePoll && attacks == 0 && rejects == 0 &&
                    rejectionChanges == 0 && modes.LastRejection == PlayerActionRejectionReason.None);

                // No awaited regeneration between drain/polls and J. Keep the real
                // key held briefly to prove one edge, inside the .55s guard delay.
                driver.Command(InputActions.Attack, true);
                await Frames(tree, input, 3);
                driver.ObserveFrame();
                float afterJ = observed.State.Energy;
                TestAssert.True(driver.AttackEdges == 1 && Input.IsPhysicalKeyPressed(Key.J) &&
                    input.Current.KeyboardActions && !input.Current.PrimaryHeld &&
                    observed.State.RegenDelayRemaining > 0f,
                    "attack-energy J uses the ordinary in-engine keyboard route within regeneration delay: " + precision);
                Check("one J has the literal expected AttackStarted count", attacks == (row.Affordable ? 1 : 0));
                Check("one J has the literal expected rejection count", rejects == (row.Affordable ? 0 : 1));
                Check("unaffordable J reports InsufficientEnergy exactly once", row.Affordable
                    ? rejectedReason == PlayerActionRejectionReason.None && rejectionChanges == 0 &&
                        modes.LastRejection == PlayerActionRejectionReason.None
                    : rejectedReason == PlayerActionRejectionReason.InsufficientEnergy && rejectionChanges == 1 &&
                        modes.LastRejection == PlayerActionRejectionReason.InsufficientEnergy);
                Check("J deducts only the literal configured attack cost", afterJ == drained - (row.Affordable ? row.Cost : 0f));
                Check("saved combat activity agrees with literal affordability", combat.IsAttacking == row.Affordable);
                Check("saved shared tuning remains unchanged", sharedTuning.ToSettings() == sharedSettings);
                GD.Print($"ATTACK_ENERGY_BOUNDARY_CASE {precision} can_afford={canAfford} can_begin={canBegin} " +
                    $"expected={row.Affordable} after_J={afterJ:R} spent={drained - afterJ:R} " +
                    $"delay_after_J={observed.State.RegenDelayRemaining:R} attacks={attacks} rejects={rejects} " +
                    $"reason={modes.LastRejection} energy_events={energyChanges} failures={failures.Count} human=false");
                driver.Command(InputActions.Attack, false);
                await Frames(tree, input, 2);
                TestAssert.True(failures.Count == 0, "Q-ATTACK-ENERGY-BOUNDARY-01: " + precision + "; " + string.Join("; ", failures));
            }
            finally
            {
                driver?.Dispose();
                root.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                ownedTuning.Dispose();
            }
        }
        GD.Print($"ATTACK_ENERGY_BOUNDARY_OK cases={cases.Length} readonly_polls_per_case=16 " +
            "saved_player=true captured_tuning=true real_J=true public_guard_drain=true human=false");
    }

    public static async Task Run(SceneTree tree)
    {
        await RunAttackEnergyBoundary(tree);
        var pinFailures = new System.Collections.Generic.List<string>();
        int pinCases = 0;
        void PinCase(string name, PrologueCanyonGeneratedMainRouteTests.Task10PinnedGuardWindow window,
            bool triggered, bool complete, params string[] want)
        {
            pinCases++;
            if (window.Triggered != triggered || window.Complete != complete ||
                !System.Linq.Enumerable.SequenceEqual(window.Rows, want)) pinFailures.Add(name);
        }
        var untouched = new PrologueCanyonGeneratedMainRouteTests.Task10PinnedGuardWindow();
        for (int i = 0; i < 10; i++) untouched.Observe("old" + i, false);
        PinCase("untriggered does not claim captured evidence", untouched, false, false);
        var first = new PrologueCanyonGeneratedMainRouteTests.Task10PinnedGuardWindow();
        first.Observe("hit", true);
        PinCase("trigger on first sample", first, true, false, "hit");
        var shortHistory = new PrologueCanyonGeneratedMainRouteTests.Task10PinnedGuardWindow();
        shortHistory.Observe("a", false); shortHistory.Observe("b", false); shortHistory.Observe("hit", true);
        PinCase("short actual history", shortHistory, true, false, "a", "b", "hit");
        var rolling = new PrologueCanyonGeneratedMainRouteTests.Task10PinnedGuardWindow();
        for (int i = 0; i < 5; i++) rolling.Observe("pre" + i, false);
        rolling.Observe("hit", true);
        PinCase("only last three predecessors", rolling, true, false, "pre2", "pre3", "pre4", "hit");
        rolling.Observe("after1", false); rolling.Observe("after2", true);
        PinCase("second trigger does not reset", rolling, true, false,
            "pre2", "pre3", "pre4", "hit", "after1", "after2");
        rolling.Observe("after3", true); rolling.Observe("after4", false);
        PinCase("four following decisions finish", rolling, true, true,
            "pre2", "pre3", "pre4", "hit", "after1", "after2", "after3", "after4");
        rolling.Observe("must-not-overwrite", true); rolling.Observe("also-ignored", false);
        PinCase("complete window stays pinned", rolling, true, true,
            "pre2", "pre3", "pre4", "hit", "after1", "after2", "after3", "after4");
        foreach (string row in new[] { "after1", "after2", "after3", "after4" }) first.Observe(row, false);
        PinCase("early trigger captures only real rows", first, true, true,
            "hit", "after1", "after2", "after3", "after4");
        TestAssert.True(pinFailures.Count == 0, "MIXED_PINNED_WINDOW_RED: " + string.Join("; ", pinFailures));
        GD.Print($"MIXED_PINNED_WINDOW_OK cases={pinCases} diagnostic_only=true extra_native_queries=0");
        var landingMaskFailures=new System.Collections.Generic.List<string>();
        int landingMaskCases=0;
        void LandingCase(string name,ulong actual,ulong expected,int requests,int recoveries,int hp,
            Knight.Prologue.PrologueObjective objective,int want)
        {
            landingMaskCases++;
            if(DescentClawGuidanceRuntimeTests.TerminalFailureMask(actual,expected,requests,recoveries,hp,objective)!=want)
                landingMaskFailures.Add(name);
        }
        var basin=Knight.Prologue.PrologueObjective.ReachBasin;
        var other=(Knight.Prologue.PrologueObjective)(-1);
        LandingCase("original all satisfied",1,1,0,0,5,basin,0);
        LandingCase("different player",2,1,0,0,5,basin,1);
        LandingCase("unexpected request",1,1,1,0,5,basin,2);
        LandingCase("unexpected recovery",1,1,0,1,5,basin,4);
        LandingCase("HP not original5",1,1,0,0,4,basin,8);
        LandingCase("objective changed",1,1,0,0,5,other,16);
        LandingCase("all five mismatches remainvisible",2,1,1,1,4,other,31);
        LandingCase("negative counters notsilentlyaccepted",1,1,-1,-1,5,basin,6);
        TestAssert.True(landingMaskFailures.Count==0,"DESCENT_TERMINAL_MASK_RED: "+string.Join("; ",landingMaskFailures));
        GD.Print($"DESCENT_TERMINAL_MASK_OK cases={landingMaskCases} diagnostic_only=true original_assert_unchanged=true");
        await AssertRouteFinalizationFailures();
        var root = new Node2D { Name = "KeyboardActionFixture" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(800, 20) } });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14);
        root.AddChild(player);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var combat = player.GetNode<PlayerCombat>("Combat");
        var shield = player.GetNode<PlayerShieldController>("Shield");
        int attacks = 0;
        combat.AttackStarted += (_, _) => attacks++;
        try
        {
            await Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "keyboard fixture rests on real support");
            int tailAdvances = 0;
            var tailResult = await PrologueCanyonGeneratedMainRouteTests.Task10ObserveFailedVolleyTail(
                () => tailAdvances == 3, () => { tailAdvances++; return Task.CompletedTask; });
            TestAssert.True(tailResult.Complete && tailResult.Ticks == 3 && tailAdvances == 3 && tailResult.Fault == null,
                "VOLLEY_PASSIVE_TAIL_RED: post-failure observer must collect completion after exactly three advances");
            tailAdvances = 0;
            tailResult = await PrologueCanyonGeneratedMainRouteTests.Task10ObserveFailedVolleyTail(
                () => true, () => { tailAdvances++; return Task.CompletedTask; });
            TestAssert.True(tailResult.Complete && tailResult.Ticks == 0 && tailAdvances == 0,
                "complete passive tail never advances the scene");
            tailResult = await PrologueCanyonGeneratedMainRouteTests.Task10ObserveFailedVolleyTail(
                () => false, () => { tailAdvances++; return Task.CompletedTask; });
            TestAssert.True(!tailResult.Complete && tailResult.Ticks == 120 && tailAdvances == 120 && tailResult.Fault == null,
                "incomplete passive tail stops at120, never extends a route budget until quiet");
            foreach (bool faultInCompletion in new[] { false, true })
            {
                var primary = new System.InvalidOperationException("original route failure");
                System.Exception observed = null;
                try
                {
                    try { throw primary; }
                    finally
                    {
                        tailResult = await PrologueCanyonGeneratedMainRouteTests.Task10ObserveFailedVolleyTail(
                            () => faultInCompletion ? throw new System.InvalidOperationException("diagnostic completion fault") : false,
                            () => throw new System.InvalidOperationException("diagnostic advance fault"));
                    }
                }
                catch (System.Exception error) { observed = error; }
                TestAssert.True(ReferenceEquals(primary, observed) && !tailResult.Complete && tailResult.Fault != null,
                    "diagnostic callback fault cannot replace the exact primary route exception");
            }
            GD.Print("VOLLEY_PASSIVE_TAIL_POLICY_OK cases=5 simulated_callbacks=true route_accepted=false");
            PrologueCanyonGeneratedMainRouteTests.CheckTask10VolleyTailCycleControls(root, player);
            TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedPreviewPreparationBudgetAccepted(8, true) &&
                PrologueCanyonGeneratedMainRouteTests.Task10MixedPreviewPreparationBudgetAccepted(7, false) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedPreviewPreparationBudgetAccepted(8, false) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedPreviewPreparationBudgetAccepted(9, true) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedPreviewPreparationBudgetAccepted(9, false),
                "MIXED_PREVIEW_DEADLINE_RED: ready at step8 may hand off; unready step8 cannot yield step9; all step9 continuations reject");
            GD.Print("MIXED_PREVIEW_DEADLINE_OK cases=5 ready8=true wait7=true wait8=false continue9=false original_budget=true");
            var laneMotor = player.GetNode<PlayerMotor>("Motor");
            TestAssert.True(MixedLanePositionControl.TryPlan(4788.7393f, -260f, 4676f, 4684.7524f,
                    .64999896f, laneMotor.MotionSettings, laneMotor.ThrusterState.Module,
                    1d / 60d, 701, out var lanePlan),
                "MIXED_LANE_PLAN_RED: the recorded inward grounded arrival must have a bounded held-A then neutral candidate, without extra K or added wait budget");
            TestAssert.True(lanePlan.TotalSteps > 0 && lanePlan.StopX is >= 4676f and <= 4684.7524f,
                "lane arithmetic candidate stops inside the body-clear interval, not an exact magic coordinate");
            TestAssert.True(!MixedLanePositionControl.TryPlan(4788.7393f, -260f, 4676f, 4684.7524f,
                    .1f, laneMotor.MotionSettings, laneMotor.ThrusterState.Module, 1d / 60d, 701, out _) &&
                !MixedLanePositionControl.TryPlan(4788.7393f, -260f, 4685f, 4684f,
                    .65f, laneMotor.MotionSettings, laneMotor.ThrusterState.Module, 1d / 60d, 701, out _) &&
                !MixedLanePositionControl.TryPlan(4788.7393f, 260f, 4676f, 4684.7524f,
                    .65f, laneMotor.MotionSettings, laneMotor.ThrusterState.Module, 1d / 60d, 701, out _) &&
                !MixedLanePositionControl.TryPlan(4788.7393f, -260f, 4676f, 4684.7524f,
                    float.PositiveInfinity, laneMotor.MotionSettings, laneMotor.ThrusterState.Module, 1d / 60d, 701, out _) &&
                !MixedLanePositionControl.TryPlan(4788.7393f, -260f, 4676f, 4684.7524f,
                    .65f, laneMotor.MotionSettings, laneMotor.ThrusterState.Module, 1d / 60d, 4, out _),
                "lane rejects insufficient warning, empty body gap, outward speed, nonfinite allowance and insufficient original ticks");
            GD.Print($"MIXED_LANE_PLAN_OK cases=6 held={lanePlan.HeldSteps} total={lanePlan.TotalSteps} " +
                $"stop={lanePlan.StopX:R} arithmetic_only=true battle_safety=false actual_K=false");
            TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                true, true, true, true, true, true, 2),
                "MIXED_LIVE_ENROLLMENT_RED: opted-in post-Shot driver must bind the actual live HP2 Burst before J, not wait for HP1");
            TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    false, true, true, true, true, true, 1) &&
                PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, true, true, true, true, 1) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    false, true, true, true, true, true, 2) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, true, true, true, true, 0) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, true, true, true, true, -1) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, false, true, true, true, true, 2) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, false, true, true, true, 2) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, true, false, true, true, 2) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, true, true, false, true, 2) &&
                !PrologueCanyonGeneratedMainRouteTests.Task10MixedBurstEnrollmentAccepted(
                    true, true, true, true, true, false, 2),
                "MIXED_LIVE_ENROLLMENT_IDENTITY: default HP1 and all actual availability/ownership/root/prefix/scene/death restrictions remain");
            GD.Print("MIXED_LIVE_ENROLLMENT_CONTRACT_OK cases=11 actual_K_proved=false");
            var rearContractFailures = new System.Collections.Generic.List<string>();
            void RearContract(string name, System.Action check)
            {
                try { check(); GD.Print($"COURTYARD_REAR_CONTRACT_GREEN name={name}"); }
                catch (System.Exception error)
                { rearContractFailures.Add(name); GD.Print($"COURTYARD_REAR_CONTRACT_RED name={name} reason={error.Message}"); }
            }
            RearContract("pending-idle", () =>
            {
                var pending = MechanicalClawFrame.Idle() with { NextCastId = 57 };
                TestAssert.True(CourtyardRearClawControl.AwaitingUnacceptedIdle(pending, 57, false, 0, 0, true) &&
                    !CourtyardRearClawControl.AwaitingUnacceptedIdle(pending, 57, true, 0, 0, true) &&
                    !CourtyardRearClawControl.AwaitingUnacceptedIdle(pending with { NextCastId = 58 }, 57, false, 0, 0, true) &&
                    !CourtyardRearClawControl.AwaitingUnacceptedIdle(pending, 57, false, 1, 0, true) &&
                    !CourtyardRearClawControl.AwaitingUnacceptedIdle(pending, 57, false, 0, 1, true) &&
                    !CourtyardRearClawControl.AwaitingUnacceptedIdle(pending, 57, false, 0, 0, false),
                    "only the unaccepted same-next-cast Idle may await the held original K");
            });
            RearContract("support-loss", () => TestAssert.True(
                CourtyardRearClawControl.RetainedSupportAccepted(true, player.IsOnFloor()) &&
                !CourtyardRearClawControl.RetainedSupportAccepted(false, player.IsOnFloor()) &&
                !CourtyardRearClawControl.RetainedSupportAccepted(true, false),
                "complete support and actual ground are both required, never just a historical launch"));
            RearContract("current-revocation", () => TestAssert.True(
                CourtyardRearClawControl.StunnedRevocationAccepted(true, true) &&
                !CourtyardRearClawControl.StunnedRevocationAccepted(true, false) &&
                !CourtyardRearClawControl.StunnedRevocationAccepted(false, true),
                "historical revocation cannot authorize a current non-revoked Stunned observation"));
            var rearLifetimeParent = new Node2D { Name = "RearLifetimeParent" };
            var rearLifetimeChild = new Node2D { Name = "RearLifetimeChild" };
            rearLifetimeParent.AddChild(rearLifetimeChild); root.AddChild(rearLifetimeParent);
            TestAssert.True(CourtyardRearClawControl.Live(rearLifetimeChild), "live chain positive precedes queued-parent control");
            rearLifetimeParent.QueueFree();
            RearContract("queued-parent", () => TestAssert.True(rearLifetimeChild.IsInsideTree() &&
                !rearLifetimeChild.IsQueuedForDeletion() && !CourtyardRearClawControl.Live(rearLifetimeChild),
                "real queued parent closes child authority before native deletion"));
            TestAssert.True(rearContractFailures.Count == 0,
                $"COURTYARD_REAR_CONTRACTS_RED: {string.Join(',', rearContractFailures)}");
            using (var driver = KeyboardRouteInputDriver.Begin(player))
            {
                var rearControl = new CourtyardRearClawControl(player);
                try
                {
                    driver.Command(InputActions.MoveRight, true);
                    await Frames(tree, input, 3);
                    int clawEdges = driver.ClawEdges;
                    using (var lane = new MixedLanePositionControl(player, floor, null))
                    {
                        TestAssert.True(!lane.Step(null, null, default, 0) && !lane.Started && !lane.Completed,
                            "lane with no exact owned group/actors must not acquire an input lease");
                        await Frames(tree, input, 1);
                        TestAssert.True(driver.ClawEdges == clawEdges && input.Current.MoveAxis > 0 &&
                            Input.IsPhysicalKeyPressed(Key.D), "ineligible lane retains actual original D without extra K");
                    }
                    await Frames(tree, input, 1);
                    TestAssert.True(input.Current.MoveAxis > 0 && Input.IsPhysicalKeyPressed(Key.D),
                        "disposing never-started lane cannot clear another movement owner");
                    GD.Print("MIXED_LANE_NO_OWNER_OK actual_D_preserved=true K_edges=0 movement_success=false");
                    TestAssert.True(!rearControl.Step(null, null, default, float.PositiveInfinity, 0, true, 0),
                        "missing real rear/front cannot acquire input ownership from a nominal infinite allowance");
                    await Frames(tree, input, 1);
                    TestAssert.True(!rearControl.Started && !rearControl.Completed && driver.ClawEdges == clawEdges &&
                        input.Current.MoveAxis > 0f && Input.IsPhysicalKeyPressed(Key.D) &&
                        !Input.IsPhysicalKeyPressed(Key.K),
                        "ineligible rear control emits no K and preserves the actual original D input");
                    rearControl.Dispose();
                    await Frames(tree, input, 1);
                    TestAssert.True(input.Current.MoveAxis > 0f && Input.IsPhysicalKeyPressed(Key.D),
                        "disposing a control that never acquired input cannot clear the original movement owner");
                }
                finally { rearControl.Dispose(); driver.ReleaseAll(); }
                await Frames(tree, input, 8);
                GD.Print("COURTYARD_REAR_INELIGIBLE_INPUT_OK live_player=true actual_D_preserved=true K_edges=0");
            }
            // A route movement intent must survive a temporary stationary guard;
            // only real keys and the production reader/motor may brake the body.
            using(var driver=KeyboardRouteInputDriver.Begin(player))
            foreach(int direction in new[]{1,-1})
            {
                driver.ReleaseAll();
                var movement=direction>0?InputActions.MoveRight:InputActions.MoveLeft;
                driver.Command(movement,true); await Frames(tree,input,3);
                TestAssert.True(input.Current.MoveAxis==direction,"real route direction was sampled before temporary guard");
                Vector2 start=player.GlobalPosition;
                Vector2 guardDirection=new(-direction,0);
                for(int tick=0;tick<12;tick++)
                {
                    driver.PointerButton(MouseButton.Right,true,player,player.GlobalPosition+guardDirection*100,stopForGuard:true);
                    await Frames(tree,input,1);
                    if(tick>=3)
                        TestAssert.True(shield.IsActive && shield.GuardDirection.Dot(guardDirection)>.99f &&
                            input.Current.MoveAxis==0 && !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D),
                            "STATIONARY_ROUTE_GUARD_RED: retaining a pending route direction must not force continuous reverse movement while holding L");
                }
                TestAssert.True(player.IsOnFloor() && Mathf.Abs(player.GlobalPosition.X-start.X)<16 &&
                    !input.HasObservedMouseMotion && input.Current.KeyboardActions,
                    "temporary directional guard brakes naturally on real support without mouse or live pose writes");
                driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero,stopForGuard:true);
                await Frames(tree,input,2);
                TestAssert.True(!shield.IsActive && input.Current.MoveAxis==direction,
                    "releasing temporary guard resumes the still-owned original route direction");
                driver.ReleaseAll();
                driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero,stopForGuard:true);
                await Frames(tree,input,2);
                TestAssert.True(input.Current.MoveAxis==0 && !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D),
                    "ReleaseAll cancels route intent instead of replaying a stale saved direction");
            }
            await Frames(tree,input,3);
            GD.Print("KEYBOARD_STATIONARY_ROUTE_GUARD_OK directions=2 native_reader=true natural_braking=true resumed_intent=true cancelled_intent=true no_pose_writes=true");
            using (var returnDriver = KeyboardRouteInputDriver.Begin(player))
            {
                var returnMotor = player.GetNode<PlayerMotor>("Motor");
                var health = player.GetNode<HealthComponent>("Health");
                var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
                int hp = health.CurrentHealth, jumps = returnMotor.AcceptedJumpCount, dashes = returnMotor.AcceptedDashCount;
                int beforeAttacks = attacks, jEdges = returnDriver.AttackEdges, kEdges = returnDriver.ClawEdges;
                ulong nextCast = claw.CurrentFrame.NextCastId;
                try
                {
                    returnDriver.Command(InputActions.MoveRight, true);
                    await Frames(tree, input, 2);
                    returnDriver.PointerButton(MouseButton.Right, true, player,
                        player.GlobalPosition + Vector2.Right * 100, stopForGuard: true);
                    await Frames(tree, input, 2);
                    TestAssert.True(shield.IsActive && input.Current.ShieldHeld &&
                        input.KeyboardAimDirection.Dot(Vector2.Right) > .99f && player.IsOnFloor(),
                        "return-input fixture has a real sampled Right+L on support before the candidate command");
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10MixedReturnLeftGuard(player, false),
                        "an absent return lease must not acquire the driver's keys");
                    await Frames(tree, input, 1);
                    TestAssert.True(shield.IsActive && shield.GuardDirection.Dot(Vector2.Right) > .99f,
                        "absent lease leaves actual original Right+L intact");
                    ulong beforeSequence = input.CurrentFrameSequence;
                    for (int sample = 0; sample < 6; sample++)
                    {
                        TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedReturnLeftGuard(player, true),
                            "MIXED_RETURN_LEFT_INPUT_RED: leased original driver must sample real Left+L, not release L or retain Right");
                        await Frames(tree, input, 1);
                        TestAssert.True(input.CurrentFrameSequence > beforeSequence && input.Current.KeyboardActions &&
                            input.Current.ShieldHeld && shield.IsActive && Input.IsPhysicalKeyPressed(Key.L) &&
                            shield.GuardDirection.Dot(Vector2.Left) > .99f && input.KeyboardAimDirection.Dot(Vector2.Left) > .99f &&
                            health.CurrentHealth == hp && returnMotor.AcceptedJumpCount == jumps && returnMotor.AcceptedDashCount == dashes &&
                            attacks == beforeAttacks && returnDriver.AttackEdges == jEdges && returnDriver.ClawEdges == kEdges &&
                            claw.CurrentFrame.NextCastId == nextCast && !input.HasObservedMouseMotion &&
                            PrologueCanyonGeneratedMainRouteTests.TryTask10FullFloorSupport(player, out ulong support) &&
                            support == floor.GetInstanceId(),
                            "return input has actual Left shield and unchanged HP/actions/cast/support, without mouse or pose writes");
                        if (sample >= 2)
                            TestAssert.True(input.Current.MoveAxis == 0 && !Input.IsPhysicalKeyPressed(Key.A) &&
                                !Input.IsPhysicalKeyPressed(Key.D) && player.Velocity.X == 0,
                                "return input uses natural stationary guard braking, not persistent stale D or continuous A");
                    }
                    returnDriver.PointerButton(MouseButton.Right, false, player, player.GlobalPosition, stopForGuard: true);
                    await Frames(tree, input, 2);
                    TestAssert.True(!shield.IsActive && input.Current.MoveAxis == 0 &&
                        !Input.IsPhysicalKeyPressed(Key.D), "expired lease cannot replay the cleared original D plan");
                    GD.Print("MIXED_RETURN_LEFT_INPUT_OK actual_Right_to_Left_L=true samples=6 neutral_stop=true " +
                        "stale_D_cleared=true no_extra_actions=true input_fixture_only=true actual_guard_contact_claim=false");
                }
                finally { returnDriver.ReleaseAll(); }
            }
            await Frames(tree, input, 2);
            using (var driver = KeyboardRouteInputDriver.Begin(player))
            {
                var handoffMotor = player.GetNode<PlayerMotor>("Motor");
                var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
                var health = player.GetNode<HealthComponent>("Health");
                int hp = health.CurrentHealth, jumps = handoffMotor.AcceptedJumpCount, dashes = handoffMotor.AcceptedDashCount;
                int originalAttacks = attacks, jEdges = driver.AttackEdges, kEdges = driver.ClawEdges;
                ulong nextCast = claw.CurrentFrame.NextCastId;
                try
                {
                    driver.Command(InputActions.MoveRight, true);
                    await Frames(tree, input, 8);
                    TestAssert.True(player.Velocity.X > 200 && Input.IsPhysicalKeyPressed(Key.D),
                        "owned-preview release fixture first establishes real rightward momentum");
                    for (int sample = 0; sample < 2; sample++)
                    {
                        driver.PointerButton(MouseButton.Right, true, player,
                            player.GlobalPosition + Vector2.Left * 100, stopForGuard: true);
                        await Frames(tree, input, 1);
                    }
                    TestAssert.True(shield.IsActive && input.KeyboardAimDirection.Dot(Vector2.Left) > .999f &&
                        input.Current.MoveAxis == 0 && player.Velocity.X > 0,
                        "owned-preview release fixture establishes actual Left guard, neutral sample and residual momentum with stale D intent");
                    ulong releaseSequence = input.CurrentFrameSequence;
                    TestAssert.True(PrologueCanyonGeneratedMainRouteTests.Task10MixedReleaseGuardToNeutral(player, true),
                        "MIXED_OWN_PREVIEW_RELEASE_RED: owned release must clear stale planned D and real driver shield state instead of resuming rightward pursuit");
                    float priorSpeed = player.Velocity.X;
                    for (int sample = 0; sample < 16; sample++)
                    {
                        await Frames(tree, input, 1);
                        TestAssert.True(input.CurrentFrameSequence > releaseSequence &&
                            !shield.IsActive && !input.Current.ShieldHeld && !input.RawFrame.ShieldHeld &&
                            input.Current.MoveAxis == 0 && input.RawFrame.MoveAxis == 0 &&
                            input.KeyboardAimDirection.Dot(Vector2.Left) > .999f &&
                            !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                            !Input.IsPhysicalKeyPressed(Key.L) && player.Velocity.X >= 0 &&
                            player.Velocity.X <= priorSpeed &&
                            PrologueCanyonGeneratedMainRouteTests.TryTask10FullFloorSupport(player, out ulong id) && id == floor.GetInstanceId() &&
                            health.CurrentHealth == hp && handoffMotor.AcceptedJumpCount == jumps && handoffMotor.AcceptedDashCount == dashes &&
                            attacks == originalAttacks && driver.AttackEdges == jEdges && driver.ClawEdges == kEdges &&
                            claw.CurrentFrame.NextCastId == nextCast && !input.HasObservedMouseMotion,
                            "owned-preview release is actual later Left/neutral input and natural supported braking without J/K/jump/dash/HP changes");
                        priorSpeed = player.Velocity.X;
                    }
                    TestAssert.True(player.Velocity.X == 0, "owned-preview release ends with actual zero speed, not a forecast assertion");
                    driver.Command(InputActions.MoveRight, true);
                    await Frames(tree, input, 2);
                    TestAssert.True(input.Current.MoveAxis > 0 && Input.IsPhysicalKeyPressed(Key.D) && !shield.IsActive,
                        "new D intent works after release, proving the driver's shield mode was cleared, not only physical L-up");
                    TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.Task10MixedReleaseGuardToNeutral(player, false) &&
                        !PrologueCanyonGeneratedMainRouteTests.Task10MixedReleaseGuardToNeutral(player, true),
                        "absent preparation ownership and actual wrong facing cannot acquire the handoff");
                    await Frames(tree, input, 1);
                    TestAssert.True(input.Current.MoveAxis > 0 && Input.IsPhysicalKeyPressed(Key.D),
                        "unowned/wrong-facing handoff preserves the actual original D input");
                    GD.Print("MIXED_OWN_PREVIEW_RELEASE_OK actual_left=true neutral_samples=16 natural_stop=true stale_D_cleared=true new_D_works=true no_owner_D_preserved=true actual_K=false");
                }
                finally { driver.ReleaseAll(); }
            }
            await Frames(tree, input, 8);
            KeyEvent(Key.J, true);
            await Frames(tree, input, 3);
            TestAssert.Equal(1, attacks, "real physical J attacks without Q/E or a pointer event");
            KeyEvent(Key.J, false);
            await Frames(tree, input, 30);
            KeyEvent(Key.L, true);
            await Frames(tree, input, 5);
            TestAssert.True(shield.IsActive, "real physical L raises the energy shield without mouse");
            KeyEvent(Key.L, false);
            await Frames(tree, input, 4);
            TestAssert.True(!shield.IsActive, "releasing L closes the held shield");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.J, true); KeyEvent(Key.K, true); KeyEvent(Key.Q, true);
            await Frames(tree, input, 3);
            TestAssert.Equal(2, attacks, "J+K+Q produces exactly one attack");
            TestAssert.Equal(MechanicalClawState.Idle,
                player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State,
                "accepted J consumes competing K rather than casting or queueing a claw");
            KeyEvent(Key.J, false); KeyEvent(Key.K, false); KeyEvent(Key.Q, false);
            await Frames(tree, input, 30);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Paused = true;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.J, true); // deliberately resume before this edge naturally expires
            tree.Paused = false;
            await Frames(tree, input, 3);
            TestAssert.Equal(2, attacks, "a paused J edge held through resume never replays");
            KeyEvent(Key.J, false); await Frames(tree, input, 2);
            KeyEvent(Key.J, true); await Frames(tree, input, 3);
            TestAssert.Equal(3, attacks, "fresh J after pause is accepted once");
            KeyEvent(Key.J, false); await Frames(tree, input, 30);
            var motor = player.GetNode<PlayerMotor>("Motor");
            TestAssert.True(player.IsOnFloor(), "ordinary guard-jump begins on real support");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.L, true); KeyEvent(Key.Space, true);
            await Frames(tree, input, 4);
            TestAssert.True(shield.IsActive && !player.IsOnFloor() && motor.AcceptedJumpCount == 1,
                "real L+Space preserves one ordinary grounded jump while guarding");
            KeyEvent(Key.Space, false); await Frames(tree, input, 2);
            KeyEvent(Key.Space, true); await Frames(tree, input, 3);
            TestAssert.Equal(1, motor.AcceptedJumpCount, "guarded airborne Space does not create a second jump");
            KeyEvent(Key.Space, false); KeyEvent(Key.L, false);
            await Frames(tree, input, 100);
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            // Prior attacks spend real energy. Let normal regeneration finish so this
            // test isolates overheat, rather than legitimately unaffordable attacks.
            for (int tick=0; tick<300 && energy.EnergyRatio < .999f; tick++) await Frames(tree,input,1);
            KeyEvent(Key.L, true);
            for (int tick=0; tick<300 && !energy.IsOverheated; tick++) await Frames(tree,input,1);
            TestAssert.True(energy.IsOverheated && !shield.IsActive && input.Current.ShieldHeld,
                "sustained real L overheats and closes guard naturally");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            TestAssert.True(modes.CanAffordAttack, "natural overheat case has a real affordable attack budget");
            GD.Print($"KEYBOARD_OVERHEAT_BEFORE energy={energy.EnergyRatio * energy.Tuning.MaxEnergy} " +
                $"canGuard={shield.CanBeginGuard} canAttack={modes.CanBeginAttack} sample={input.CurrentFrameSequence}");
            KeyEvent(Key.J, true);
            for (int tick=0; tick<3; tick++)
            {
                await Frames(tree, input, 1);
                GD.Print($"KEYBOARD_OVERHEAT_SAMPLE attack={input.Current.AttackPressed} shield={input.Current.ShieldHeld} " +
                    $"source={input.Current.KeyboardActions} energy={energy.EnergyRatio * energy.Tuning.MaxEnergy} " +
                    $"reason={modes.LastRejection} count={attacks} sample={input.CurrentFrameSequence}");
            }
            TestAssert.Equal(4, attacks, "fresh J can attack after guard overheat while L stays held");
            GD.Print("KEYBOARD_ACTION_RUNTIME_OK");
        }
        finally
        {
            KeyEvent(Key.J, false); KeyEvent(Key.K, false); KeyEvent(Key.L, false); KeyEvent(Key.Q, false);
            KeyEvent(Key.Space, false);
            tree.Paused = false;
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    public static void KeyEvent(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
        Input.FlushBufferedEvents();
    }

    public static async Task Frames(SceneTree tree, PlayerInputReader input, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            ulong before = input.CurrentFrameSequence;
            for (int wait = 0; wait < 8 && before == input.CurrentFrameSequence; wait++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(input.CurrentFrameSequence > before, "real input sampling advances");
        }
    }
}
