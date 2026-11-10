#if REFCOUNTED_FATAL_IDENTITY
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;
using Knight.Config;
using Knight.Build.Prologue;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Knight.Tests.Diagnostics;

// Instrumentation contract only: real owned resources, no collection or drain.
public partial class RunResidentCensusIdentityContract : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args=OS.GetCmdlineUserArgs();
            if(args.Length==2&&args.Count(x=>x.StartsWith("--case-storage-root=",StringComparison.Ordinal))==1&&
                args.Count(x=>x=="--phase=sector-assignment-identity-contract")==1)
            {
                _=IsolatedPrologueFixture.ValidateStorageRoot();
                CheckSectorAssignmentIdentity();
                await DescentResourceBoundaryContracts.Run(this);
                Quit(0);
                return;
            }
            if(args.Length==2&&args.Count(x=>x.StartsWith("--case-storage-root="))==1&&args.Count(x=>x=="--phase=prologue-profile-lifetime-pair")==1)
            {
                await RunProfileOwnerPair(this);
                Quit(0);
                return;
            }
            TestAssert.True(args.Length==2&&args.Count(x=>x.StartsWith("--case-storage-root="))==1&&
                args.Count(x=>x=="--phase=resident-census-identity-contract")==1,"exact isolated identity contract args");
            _=IsolatedPrologueFixture.ValidateStorageRoot();
            CheckPlayerTuningBirthBeforeMain();
            CheckSectorAllocation("main-undeclared", 0, "", "");
            CheckPresentationAllocation("main-undeclared", 0, "", "");
            Task14NativeResourceIdentity.DeclareMainManagedThread();
            CheckPlayerTuningProducers();
            var tuningReviewFailures = new System.Collections.Generic.List<string>();
            foreach (bool coordinatorFallback in new[] { false, true })
            {
                try { await CheckPlayerTuningSavedReady(this, false, coordinatorFallback); }
                catch (InvalidOperationException error)
                {
                    string name = coordinatorFallback ? "I2-coordinator" : "I1-unused-fallback";
                    GD.Print($"PLAYER_TUNING_REVIEW_RED {name} {error.Message}");
                    tuningReviewFailures.Add(name + ": " + error.Message);
                }
            }
            TestAssert.True(tuningReviewFailures.Count == 0, string.Join(" | ", tuningReviewFailures));
            GD.Print("PLAYER_TUNING_REVIEW_CONTROLS_OK I1_unused_fallback=true I2_coordinator_fallback=true resource_fix=false");
            await CheckPlayerTuningSavedReady(this, true);
            CheckPlayerTuningObservationFailure();
            CheckProfileLifetimeIdentity();
            CheckProfileLifetimeSourceCache();
            await CheckProfileReviewControls(this);
            BasinSupplyRuntimeTests.CheckAssignedProfileObservationFailure(this);
            CheckPresentationAllocation("trace-not-started", 0, "", "");
            CheckSectorAllocation("trace-not-started", 0, "", "");
            long a=Check(new ResidentSubtypeProfile(),"profile-A");
            long b=Check(new ResidentSubtypeProfile(),"profile-B");
            long d=Check(new PrologueSectorDefinition(),"temporary-definition");
            TestAssert.True(a!=b&&a!=d&&b!=d,"distinct real wrappers must not share serials");
            TestAssert.Equal("native_identity=unknown",Task14NativeResourceIdentity.DisposeIdentity(long.MaxValue),"unknown serial never borrows last record");
            CheckResidentProfileBoundaryValues();
            CheckResidentProfileAssignmentTrace();
            CheckResidentAfterRejectionSource(IsolatedPrologueFixture.ValidateStorageRoot());
            CheckResidentAssignmentSerialScopes(IsolatedPrologueFixture.ValidateStorageRoot());
            CheckMobilityDisposeCoverage();
            Task14NativeResourceIdentity.BeginOriginalProbeTrace();
            long operation = Task14NativeResourceIdentity.BeginOriginalProbe("allocation-contract", "contract-only/no-load");
            try
            {
                CheckPresentationAllocation("active-original-probe", operation, "allocation-contract", "contract-only/no-load");
                CheckOtherThreadAllocation();
                CheckSectorAllocation("active-original-probe", operation, "allocation-contract", "contract-only/no-load");
                CheckSectorAllocation("active-original-probe", operation, "allocation-contract", "contract-only/no-load", true);
                CheckOtherThreadSectorAllocation();
                Task14NativeResourceIdentity.OriginalProbeBoundary(operation, "instantiate-return",
                    "allocation-contract", "contract-only/no-load", 0);
                CheckSectorAllocation("no-active-probe", 0, "", "");
            }
            finally { Task14NativeResourceIdentity.EndOriginalProbe(operation); }
            long endOnlyOperation = Task14NativeResourceIdentity.BeginOriginalProbe("allocation-end-contract", "contract-only/end-no-load");
            Task14NativeResourceIdentity.EndOriginalProbe(endOnlyOperation);
            CheckPresentationAllocation("no-active-probe", 0, "", "");
            CheckSectorAllocation("no-active-probe", 0, "", "");
            SectorAllocationMethod("AssertSectorAllocationComplete").Invoke(null, null);
            CheckFailedSectorAllocationSink();
            CheckSectorAllocationCap();
            GD.Print("SECTOR_ALLOCATION_CONTRACT_OK real_definitions=7 default_and_parameterized=true exact_serial_once=true early_and_scoped=true instantiate_return_unscoped=true ended_context_unknown=true other_thread_unscoped=true sink_failure_contained=true cap_veto=true native_owner_proved=false behavior_accepted=false");
            Task14NativeResourceIdentity.AssertPresentationAllocationComplete();
            CheckFailedAllocationSink();
            CheckAllocationCap();
            GD.Print("PRESENTATION_ALLOCATION_CONTRACT_OK real_profiles=5 exact_serial_once=true early_and_scoped=true ended_context_unknown=true other_thread_unscoped=true sink_failure_contained=true cap_veto=true behavior_accepted=false");
            GD.Print("CENSUS_IDENTITY_CONTRACT_OK resources=3 profiles=2 definitions=1 actual_native=true disposing_pairs=3 scalar_lookup_after_dispose=true native_ids_released=true managed_gc_proved=false no_added_gc_or_drain=true behavior_accepted=false");
            Quit(0);
        }
        catch(Exception error){GD.PushError(error.ToString());Quit(1);}
    }

    private static void CheckSectorAssignmentIdentity()
    {
        var method=typeof(Task14NativeResourceIdentity).GetMethod("ObserveAssignedSectorDefinitions",
            BindingFlags.NonPublic|BindingFlags.Static);
        TestAssert.True(method!=null,"SECTOR_ASSIGNMENT_RED: actual field observer is missing");
        int Observe(ulong root,SeamlessPrologueWorld world,string stage)
        {
            try {return (int)method.Invoke(null,new object[]{root,world,stage});}
            catch(TargetInvocationException error) when(error.InnerException!=null)
            {System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
        }
        void Reject(Action run)
        {
            bool rejected=false;
            try {run();}
            catch(InvalidOperationException error) {rejected=error.Message.StartsWith("sector assignment",StringComparison.Ordinal);}
            TestAssert.True(rejected,"sector assignment invalid input/cap must fail closed");
        }
        var saved=Console.Error;
        using var captured=new StringWriter(CultureInfo.InvariantCulture);
        Console.SetError(captured);
        try
        {
            TestAssert.Equal(0,Observe(0,null,"inactive"),"inactive observer does not query null world");
            TestAssert.Equal("",captured.ToString(),"inactive scope produces no diagnostic output");
        }
        finally {Console.SetError(saved);}
        Task14NativeResourceIdentity.BeginOriginalProbeTrace();
        var rootNode=new Node();
        var worldNode=new SeamlessPrologueWorld();
        rootNode.AddChild(worldNode);
        using var oldA=new PrologueSectorDefinition {Id=PrologueSectorId.CanyonEntrance};
        using var oldB=new PrologueSectorDefinition {Id=PrologueSectorId.CanyonDescent};
        var fresh=new PrologueSectorDefinition {Id=PrologueSectorId.CanyonBasin};
        var before=new Godot.Collections.Array<PrologueSectorDefinition>{oldA,oldB};
        var after=new Godot.Collections.Array<PrologueSectorDefinition>{fresh};
        var invalid=new Godot.Collections.Array<PrologueSectorDefinition>{null};
        var tooMany=new Godot.Collections.Array<PrologueSectorDefinition>();
        var empty=new Godot.Collections.Array<PrologueSectorDefinition>();
        ulong rootId=rootNode.GetInstanceId(),worldId=worldNode.GetInstanceId(),freshId=fresh.GetInstanceId();
        long serial=fresh.Task14ManagedSerial;
        try
        {
            TestAssert.Equal(0UL,Task14NativeResourceIdentity.ObservedNativeId(serial),"unobserved definition is unknown");
            TestAssert.Equal("native_identity=unknown",Task14NativeResourceIdentity.DisposeIdentity(serial),"unobserved serial must not inherit previous registry");
            Console.SetError(captured);
            try
            {
                worldNode.Definitions=before;
                TestAssert.Equal(2,Observe(rootId,worldNode,"contract.before"),"actual old array fields observed");
                worldNode.Definitions=after;
                TestAssert.Equal(1,Observe(rootId,worldNode,"contract.after"),"actual replacement fields observed");
                string first=Task14NativeResourceIdentity.DisposeIdentity(serial);
                TestAssert.Equal(1,Observe(rootId,worldNode,"contract.repeat"),"repeated real association is still emitted");
                TestAssert.Equal(first,Task14NativeResourceIdentity.DisposeIdentity(serial),"repeat must preserve first registry attribution");
                TestAssert.True(first.Contains("observed_phase=contract.after",StringComparison.Ordinal),"first actual stage remains registered");
            }
            finally {Console.SetError(saved);}
            var rows=captured.ToString().Split('\n').Where(x=>x.StartsWith("TASK14_SECTOR_ASSIGNMENT_ROW ",StringComparison.Ordinal))
                .Select(x=>JsonDocument.Parse(x.Substring("TASK14_SECTOR_ASSIGNMENT_ROW ".Length))).ToArray();
            try
            {
                TestAssert.Equal(4,rows.Length,"four associations, not before data copied to after");
                var expected=new[]{(oldA,0,"contract.before"),(oldB,1,"contract.before"),(fresh,0,"contract.after"),(fresh,0,"contract.repeat")};
                for(int index=0;index<4;index++)
                {
                    var row=rows[index].RootElement;var want=expected[index];
                    TestAssert.True(row.GetProperty("RootId").GetUInt64()==rootId&&row.GetProperty("WorldId").GetUInt64()==worldId&&
                        row.GetProperty("Index").GetInt32()==want.Item2&&row.GetProperty("Stage").GetString()==want.Item3&&
                        row.GetProperty("Sector").GetInt32()==(int)want.Item1.Id&&
                        row.GetProperty("ManagedSerial").GetInt64()==want.Item1.Task14ManagedSerial&&
                        row.GetProperty("NativeId").GetUInt64()==want.Item1.GetInstanceId(),"association must match actual assigned object and stage");
                }
            }
            finally {foreach(var row in rows)row.Dispose();}
            Console.Error.Write(captured.ToString());
            Reject(()=>Observe(rootId,null,"invalid-world"));
            Reject(()=>Observe(0,worldNode,"invalid-root"));
            Reject(()=>Observe(rootId,worldNode," "));
            worldNode.Definitions=null;Reject(()=>Observe(rootId,worldNode,"null-array"));
            worldNode.Definitions=invalid;Reject(()=>Observe(rootId,worldNode,"null-definition"));
            for(int index=0;index<17;index++)tooMany.Add(oldA);
            worldNode.Definitions=tooMany;Reject(()=>Observe(rootId,worldNode,"too-many"));
            worldNode.Definitions=after;
            bool failedSink=false;
            Console.SetError(new FailingAllocationWriter());
            try {Observe(rootId,worldNode,"sink-failure");}
            catch(IOException error) {failedSink=error.Message=="owned allocation sink fault";}
            finally {Console.SetError(saved);}
            TestAssert.True(failedSink,"diagnostic sink failure cannot become a successful zero-row snapshot");
            Task14NativeResourceIdentity.BeginOriginalProbeTrace();
            worldNode.Definitions=empty;
            for(int index=0;index<64;index++)TestAssert.Equal(0,Observe(rootId,worldNode,"cap-control"),"empty current array has zero rows");
            Reject(()=>Observe(rootId,worldNode,"snapshot-cap"));
        }
        finally
        {
            Console.SetError(saved);
            rootNode.Free();
            before.Clear();after.Clear();invalid.Clear();tooMany.Clear();
            fresh.Dispose();
        }
        // Only scalar registry lookup after releasing our own resources. No native
        // query on any disposed wrapper, no forced GC or finalizer drain.
        TestAssert.Equal(freshId,Task14NativeResourceIdentity.ObservedNativeId(serial),"released subject retains scalar identity only");
        GD.Print("SECTOR_ASSIGNMENT_CONTRACT_OK inactive_no_query=true current_fields=true repeated_first_identity=true scalar_after_release=true invalid_and_caps=true no_added_load_or_getstate=true behavior_accepted=false whole_goal=false");
    }

    private static async Task RunProfileOwnerPair(SceneTree tree)
    {
        _ = IsolatedPrologueFixture.ValidateStorageRoot();
        bool previousMode = Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks;
        bool ended = false;
        try
        {
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            GD.Print("PROFILE_LIFETIME_PAIR_BEGIN methods=Task5FallContextContracts,GeneratedMainDeathOnly unchanged=true added_gc=0 added_finalizer_drain=0 new_strong_roots=0 resource_fix=false");
            GD.Print("PROFILE_LIFETIME_PAIR_BOUNDARY method=fall-context stage=enter");
            await ProloguePlaytestRecorderTests.RunTask5FallContextContracts(tree);
            GD.Print("PROFILE_LIFETIME_PAIR_BOUNDARY method=fall-context stage=return");
            GD.Print("PROFILE_LIFETIME_PAIR_BOUNDARY method=generated-main-death stage=enter");
            await PrologueLevelRuntimeTests.RunGeneratedMainDeathOnly(tree);
            GD.Print("PROFILE_LIFETIME_PAIR_BOUNDARY method=generated-main-death stage=return");
            Knight.Tests.RunPrologueTests.EndResidentAssignmentTraceForSuccess();
            ended = true;
            GD.Print("PROFILE_LIFETIME_PAIR_COMPLETE methods_returned=2 original_teardown=true original_post=false behavior_accepted=false whole_goal=false resource_fix=false");
        }
        finally
        {
            if (!ended) Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = previousMode;
        }
    }

    private static void CheckPlayerTuningBirthBeforeMain()
    {
        var journal = new ResidentProfileLifetimeTrace.Journal(_ => { });
        using (ResidentProfileLifetimeTrace.UseJournalForContract(journal))
        {
            using var tuning = new PlayerTuning();
            var birth = journal.Rows.SingleOrDefault(x => x.Kind == "born" && x.Serial == tuning.Task14ManagedSerial);
            TestAssert.True(birth != null && birth.Type == "Knight.Config.PlayerTuning" &&
                birth.Hash == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(tuning) &&
                birth.NativeId == 0 && birth.Stack.Length > 0 && birth.Stack.Length <= 4096 &&
                Task14NativeResourceIdentity.DisposeIdentity(tuning.Task14ManagedSerial) == "native_identity=unknown",
                "PLAYER_TUNING_BIRTH_RED: actual ctor records managed-only birth before main declaration");
        }
        journal.AssertComplete();
        GD.Print("PLAYER_TUNING_BIRTH_OK before_main=true native_queries=0 bounded_stack=true resource_fix=false");
    }

    private static void CheckPlayerTuningProducers()
    {
        var journal = new ResidentProfileLifetimeTrace.Journal(_ => { });
        using var restore = ResidentProfileLifetimeTrace.UseJournalForContract(journal);
        var motor = new Knight.Player.PlayerMotor();
        var combat = new Knight.Player.PlayerCombat();
        var motorTuning = motor.Tuning;
        var combatTuning = combat.Tuning;
        PlayerTuning timelineTuning = null;
        try
        {
            var getTuning = typeof(Knight.Presentation.PlayerPresentationTimeline)
                .GetField("_tuning", BindingFlags.NonPublic | BindingFlags.Instance);
            var timeline = new Knight.Presentation.PlayerPresentationTimeline();
            timelineTuning = (PlayerTuning)getTuning.GetValue(timeline);
            foreach (var item in new[] { (motorTuning, "motor-default"), (combatTuning, "combat-default"),
                (timelineTuning, "timeline-fallback") })
            {
                var birth = journal.Rows.SingleOrDefault(x => x.Kind == "born" && x.Serial == item.Item1.Task14ManagedSerial);
                var created = journal.Rows.SingleOrDefault(x => x.Kind == "player-tuning-created" &&
                    x.Serial == item.Item1.Task14ManagedSerial && x.Detail == item.Item2);
                TestAssert.True(birth != null && created != null && birth.Sequence < created.Sequence &&
                    birth.NativeId == 0 && created.NativeId == item.Item1.GetInstanceId() &&
                    created.Hash == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item.Item1) &&
                    created.ActorId == 0 && created.Type == "Knight.Config.PlayerTuning",
                    "PLAYER_TUNING_CREATED_RED: exact original new result, producer role and native identity after ctor");
            }
            int count = journal.Rows.Length;
            var supplied = new Knight.Presentation.PlayerPresentationTimeline(motorTuning);
            TestAssert.True(ReferenceEquals(getTuning.GetValue(supplied), motorTuning) &&
                journal.Rows.Length == count, "supplied tuning must not allocate an unused timeline fallback");
        }
        finally
        {
            motor.Free(); combat.Free();
            motorTuning.Dispose(); combatTuning.Dispose(); timelineTuning?.Dispose();
        }
        journal.AssertComplete();
        GD.Print("PLAYER_TUNING_PRODUCERS_OK ctor_native_queries=0 exact_new_identity=true supplied_no_allocation=true resource_fix=false");
    }

    private static async Task CheckPlayerTuningSavedReady(SceneTree tree, bool fallback = false, bool coordinatorFallback = false)
    {
        var journal = new ResidentProfileLifetimeTrace.Journal(_ => { });
        using var restore = ResidentProfileLifetimeTrace.UseJournalForContract(journal);
        CharacterBody2D player = null;
        try
        {
            // This control may load the saved Player; the lifetime pair never adds this load.
            var packed = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
            player = packed.Instantiate<CharacterBody2D>();
            var motor = player.GetNode<Knight.Player.PlayerMotor>("Motor");
            var combat = player.GetNode<Knight.Player.PlayerCombat>("Combat");
            var coordinator = player.GetNode<Knight.Presentation.PlayerPresentationCoordinator>("Presentation");
            if (coordinatorFallback)
            {
                coordinator.MotorPath = new NodePath("../AbsentMotor");
                coordinator.CombatPath = new NodePath("../AbsentCombat");
            }
            TestAssert.True(ReferenceEquals(motor.Tuning, combat.Tuning), "saved export sharing is preserved");
            if (fallback) { motor.Tuning = null; combat.Tuning = null; }
            tree.Root.AddChild(player);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var timeline = typeof(Knight.Presentation.PlayerPresentationCoordinator)
                .GetField("_timeline", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(coordinator);
            var timelineTuning = (PlayerTuning)typeof(Knight.Presentation.PlayerPresentationTimeline)
                .GetField("_tuning", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(timeline);
            var coordinatorRows = journal.Rows.Where(x => x.Kind == "player-tuning-created" &&
                x.Detail == "coordinator-fallback").ToArray();
            TestAssert.True(coordinatorRows.Length == (coordinatorFallback ? 1 : 0) &&
                !journal.Rows.Any(x => x.Kind == "player-tuning-created" && x.Detail == "timeline-fallback") &&
                (coordinatorFallback ? coordinatorRows[0].Serial == timelineTuning.Task14ManagedSerial &&
                    coordinatorRows[0].NativeId == timelineTuning.GetInstanceId() &&
                    coordinatorRows[0].Hash == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(timelineTuning) :
                    ReferenceEquals(timelineTuning, motor.Tuning)),
                "PLAYER_TUNING_REVIEW_I2_RED: real coordinator fallback exact role/identity, supplied tuning no unused allocation");
            foreach (var item in new[] { (motor.GetInstanceId(), motor.Tuning, "motor-ready"),
                (combat.GetInstanceId(), combat.Tuning, "combat-ready") })
            {
                TestAssert.True(journal.Rows.Count(x => x.Kind == "player-tuning-assigned" &&
                    x.Serial == item.Item2.Task14ManagedSerial && x.NativeId == item.Item2.GetInstanceId() &&
                    x.ActorId == item.Item1 && x.Detail == item.Item3) == 1,
                    "PLAYER_TUNING_ASSIGNED_RED: actual Ready observes the shared saved export, not discarded defaults");
                var created = journal.Rows.Where(x => x.Kind == "player-tuning-created" &&
                    x.Detail == item.Item3 + "-fallback").ToArray();
                TestAssert.True(created.Length == (fallback ? 1 : 0) &&
                    (!fallback || created[0].Serial == item.Item2.Task14ManagedSerial &&
                        created[0].NativeId == item.Item2.GetInstanceId()),
                    "PLAYER_TUNING_REVIEW_I1_RED: count all role creations, including an unused eagerly allocated fallback");
            }
            TestAssert.True(ReferenceEquals(motor.Tuning, combat.Tuning) != fallback,
                "normal shared export and independent null fallbacks preserve original identity semantics");
            var defaults = journal.Rows.Where(x => x.Kind == "player-tuning-created" &&
                (x.Detail == "motor-default" || x.Detail == "combat-default")).ToArray();
            TestAssert.True(defaults.Length == 2 && defaults.All(x =>
                    x.Serial != motor.Tuning.Task14ManagedSerial && x.Serial != combat.Tuning.Task14ManagedSerial) &&
                defaults.Select(x => x.Serial).Distinct().Count() == 2,
                "actual saved assignment differs from both scalar-recorded constructor defaults");
        }
        finally { if (player != null) { player.GetParent()?.RemoveChild(player); player.Free(); } }
        journal.AssertComplete();
        GD.Print($"PLAYER_TUNING_SAVED_READY_OK null_fallback={fallback} coordinator_fallback={coordinatorFallback} distinct_defaults=2 borrowed_dispose=0 resource_fix=false");
    }

    private static void CheckPlayerTuningObservationFailure()
    {
        var broken = new ResidentProfileLifetimeTrace.Journal(_ => throw new IOException("owned tuning sink fault"));
        using (ResidentProfileLifetimeTrace.UseJournalForContract(broken))
        {
            var motor = new Knight.Player.PlayerMotor();
            var tuning = motor.Tuning;
            try { TestAssert.True(GodotObject.IsInstanceValid(tuning), "failed diagnostic sink does not replace construction result"); }
            finally { motor.Free(); tuning.Dispose(); }
        }
        RequireIncomplete(broken);
        var wrongThread = new ResidentProfileLifetimeTrace.Journal(_ => { });
        using (ResidentProfileLifetimeTrace.UseJournalForContract(wrongThread))
        {
            using var tuning = new PlayerTuning();
            var returned = Task.Run(() => ResidentProfileLifetimeTrace.PlayerTuningCreated(tuning, "wrong-thread-owned-control"))
                .GetAwaiter().GetResult();
            TestAssert.True(ReferenceEquals(returned, tuning) && wrongThread.Rows.Single(x =>
                x.Kind == "player-tuning-created").NativeId == 0 &&
                Task14NativeResourceIdentity.DisposeIdentity(tuning.Task14ManagedSerial) == "native_identity=unknown",
                "wrong-thread observer returns exact argument without native identity lookup");
        }
        RequireIncomplete(wrongThread);
        GD.Print("PLAYER_TUNING_FAILURE_CONTRACT_OK sink_failure_contained=true wrong_thread_native_unknown=true incomplete_veto=true resource_fix=false");
        static void RequireIncomplete(ResidentProfileLifetimeTrace.Journal journal)
        {
            bool rejected = false;
            try { journal.AssertComplete(); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("PROFILE_LIFETIME_INCOMPLETE", StringComparison.Ordinal))
            { rejected = true; }
            TestAssert.True(rejected, "failed tuning observation must veto complete evidence");
        }
    }

    private static void CheckProfileLifetimeIdentity()
    {
        // Missing ctor emission, wrong nesting, borrowed ownership and any retained native
        // wrapper would invalidate this diagnostic even if the old assignment trace passes.
        var output = new System.Collections.Generic.List<string>();
        var journal = new ResidentProfileLifetimeTrace.Journal(output.Add);
        long serial;
        ulong native;
        using (ResidentProfileLifetimeTrace.UseJournalForContract(journal))
        {
            using var scope = journal.Begin("owner-A", 7, 11, "slot-A", "owned/profile-A");
            using var profile = new ResidentSubtypeProfile();
            serial = profile.Task14ManagedSerial;
            native = profile.GetInstanceId();
            var born = journal.Rows.SingleOrDefault(x => x.Kind == "born" && x.Serial == serial);
            TestAssert.True(born != null && born.Hash == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(profile) &&
                born.Type == typeof(ResidentSubtypeProfile).FullName && born.Stack.Length > 0 &&
                born.Stack.Length <= 4096 && born.Context.Owner == "owner-A" && born.Context.Epoch == 7 &&
                born.Context.Reset == 11 && born.Context.Slot == "slot-A" && born.Context.Path == "owned/profile-A",
                "PROFILE_LIFETIME_BIRTH_RED: actual owned Profile ctor must record bounded managed birth in its actual scope");
            using var scene = new PackedScene();
            var actor = new CharacterBody2D();
            var binding = new ResidentSceneBinding();
            try
            {
                actor.AddChild(binding);
                ResidentProfileLifetimeTrace.GateSnapshot(profile, actor, binding, scene, 41);
                var gate = journal.Rows.Single(x => x.Kind == "nonnull-gate" && x.Serial == serial);
                TestAssert.True(gate.NativeId == native && gate.ActorId == actor.GetInstanceId() &&
                    gate.BindingId == binding.GetInstanceId() && gate.SceneId == scene.GetInstanceId() &&
                    gate.Assignment == 41 && gate.Context.Operation == born.Context.Operation,
                    "PROFILE_LIFETIME_GATE_RED: actual native IDs must join the managed serial before configuration, not claim admission");
                using (journal.Begin("owner-B", 13, 17, "slot-B", "owned/profile-B"))
                    TestAssert.True(journal.Current.Owner == "owner-B" && journal.Current.Epoch == 13 && journal.Current.Reset == 17,
                        "PROFILE_LIFETIME_NESTED_RED: literal inner owner and generations");
                TestAssert.True(journal.Current.Owner == "owner-A" && journal.Current.Epoch == 7 && journal.Current.Reset == 11,
                    "nested completion must restore exact outer owner, not latest assignment");
                Task.Run(() => TestAssert.True(journal.Current.Operation == 0,
                    "other managed thread cannot borrow the spawning thread's owner")).GetAwaiter().GetResult();
            }
            finally { actor.Free(); }
        }
        // Only managed rows and the old scalar identity table may be read after disposal.
        TestAssert.True(journal.Current.Operation == 0 && journal.Rows.Single(x => x.Kind == "nonnull-gate" && x.Serial == serial).NativeId == native &&
            Task14NativeResourceIdentity.DisposeIdentity(serial).Contains("native_id=" + native, StringComparison.Ordinal),
            "released owned resources remain identifiable without resurrecting a wrapper");
        journal.AssertComplete();
        var broken = new ResidentProfileLifetimeTrace.Journal(_ => { });
        var outer = broken.Begin("outer", 1, 2, "a", "a");
        var inner = broken.Begin("inner", 3, 4, "b", "b");
        outer.Dispose(); inner.Dispose();
        TestAssert.True(broken.Current.Operation == 0, "non-LIFO cleanup must never restore a disposed owner");
        MustBeIncomplete(broken, "non-LIFO scope order");
        var capped = new ResidentProfileLifetimeTrace.Journal(_ => { }, events: 1);
        capped.Record(new("one")); capped.Record(new("two"));
        MustBeIncomplete(capped, "event cap");
        var linkCap = new ResidentProfileLifetimeTrace.Journal(_ => { }, links: 1);
        linkCap.Record(new("nonnull-gate") { Serial = 101, NativeId = 1001 });
        linkCap.Record(new("nonnull-gate") { Serial = 102, NativeId = 1002 });
        MustBeIncomplete(linkCap, "profile link cap");
        var sink = new ResidentProfileLifetimeTrace.Journal(_ => throw new IOException("deliberate owned sink failure"));
        sink.Record(new("sink-control"));
        MustBeIncomplete(sink, "sink failure");
        var depth = new ResidentProfileLifetimeTrace.Journal(_ => { }, depth: 1);
        using (depth.Begin("a", 1, 2, "a", "a"))
        using (depth.Begin("b", 3, 4, "b", "b"))
            TestAssert.True(depth.Current.Operation == 0, "depth overflow must lose attribution, not borrow its parent");
        MustBeIncomplete(depth, "scope depth cap");
        GD.Print("PROFILE_LIFETIME_CONTRACT_OK real_ctor=true actual_native_ids=true nested_restore=true thread_isolation=true non_lifo_unknown=true event_link_depth_caps=true sink_failure_veto=true scalar_after_dispose=true no_added_gc_or_drain=true behavior_accepted=false");

        static void MustBeIncomplete(ResidentProfileLifetimeTrace.Journal value, string label)
        {
            bool rejected = false;
            try { value.AssertComplete(); } catch (InvalidOperationException) { rejected = true; }
            TestAssert.True(rejected, "PROFILE_LIFETIME_COMPLETENESS_RED: " + label + " must veto completeness without throwing through the producer");
        }
    }

    private static void CheckProfileLifetimeSourceCache()
    {
        var journal = new ResidentProfileLifetimeTrace.Journal(_ => { });
        using var restore = ResidentProfileLifetimeTrace.UseJournalForContract(journal);
        using var scope = journal.Begin("cache-owner", 19, 23, "cache-slot", "owned/cache");
        using var scene = new PackedScene();
        ulong sceneId = scene.GetInstanceId();
        int loads = 0;
        using var cache = new ResidentOriginalSceneSources(new[] { "owned/cache" }, path =>
        {
            TestAssert.Equal("owned/cache", path, "cache loads only the allowed path");
            loads++; return scene;
        });
        TestAssert.True(ReferenceEquals(scene, cache.Get("owned/cache")) && ReferenceEquals(scene, cache.Get("owned/cache")) && loads == 1,
            "trace must not add loads, replace resources or bypass the actual cache");
        var loaded = journal.Rows.SingleOrDefault(x => x.Kind == "source-valid-return");
        var hit = journal.Rows.SingleOrDefault(x => x.Kind == "source-cache-hit");
        TestAssert.True(loaded != null && hit != null && loaded.SceneId == sceneId && hit.SceneId == sceneId &&
            loaded.CacheId > 0 && hit.CacheId == loaded.CacheId && loaded.Context.Owner == "cache-owner" &&
            loaded.Context.Epoch == 19 && loaded.Context.Reset == 23,
            "PROFILE_LIFETIME_CACHE_RED: actual valid load and cache hit must retain scalar source/owner identities");
        cache.Dispose();
        TestAssert.True(GodotObject.IsInstanceValid(scene) && journal.Rows.Any(x => x.Kind == "cache-reference-clear" &&
            x.SceneId == sceneId && x.CacheId == loaded.CacheId),
            "cache clear must record scalar reference custody, not dispose a borrowed saved scene");
        ResidentOriginalSceneSources reentrant = null;
        try
        {
            reentrant = new(new[] { "owned/reentrant" }, _ => { reentrant.Dispose(); return scene; });
            bool disposed = false;
            try { reentrant.Get("owned/reentrant"); } catch (ObjectDisposedException) { disposed = true; }
            TestAssert.True(disposed && GodotObject.IsInstanceValid(scene) &&
                !journal.Rows.Any(x => x.Kind == "source-valid-return" && x.Detail == "owned/reentrant"),
                "post-loader disposed guard remains before any valid-return native snapshot");
        }
        finally { reentrant?.Dispose(); }
        scope.Dispose(); journal.AssertComplete();
        GD.Print("PROFILE_LIFETIME_CACHE_CONTRACT_OK real_cache=true one_load_two_gets=true scalar_source_ids=true clear_not_dispose=true reentrant_disposed_guard=true behavior_accepted=false");
    }

    private static async Task CheckProfileReviewControls(SceneTree tree)
    {
        var failures = new System.Collections.Generic.List<string>();
        Check("I1-explicit-subject", () =>
        {
            var journal = new ResidentProfileLifetimeTrace.Journal(_ => { });
            using var restore = ResidentProfileLifetimeTrace.UseJournalForContract(journal);
            using (journal.Begin("preparation-A", 29, 31, "slot-A", "owned/review"))
            {
                using var packed = new PackedScene();
                using var cache = new ResidentOriginalSceneSources(new[] { "owned/review" }, _ => packed);
                cache.TraceOwner("cache-B", "resident-leaf");
                cache.Get("owned/review"); cache.Dispose();
                ResidentProfileLifetimeTrace.Source(0, "manager-dispose-before-cache-clear", 0, "", "manager-B", "manager");
                var cleared = journal.Rows.Single(x => x.Kind == "cache-reference-clear");
                var disposing = journal.Rows.Single(x => x.Kind == "manager-dispose-before-cache-clear");
                TestAssert.True(cleared.SubjectOwner == "cache-B" && disposing.SubjectOwner == "manager-B" &&
                    cleared.Context.Owner == "preparation-A" && disposing.Context.Owner == "preparation-A",
                    "PROFILE_REVIEW_I1_RED: active preparation and actual cache/manager subject must both survive reentry");
            }
            journal.AssertComplete();
        });
        var fixtureRoot = new Node2D();
        var playerRoot = new Node2D { Name = "PlayerRoot" };
        var player = new CharacterBody2D { Name = "Player", Position = new(1312, 100) };
        playerRoot.AddChild(player); fixtureRoot.AddChild(playerRoot);
        var worldRoot = new Node2D { Name = "WorldRoot" }; fixtureRoot.AddChild(worldRoot);
        var world = new SeamlessPrologueWorld { Name = "Sectors", InitialSector = PrologueSectorId.CanyonDescent };
        worldRoot.AddChild(world);
        var ownedScenes = new System.Collections.Generic.List<PackedScene>();
        var ownedDefinitions = new System.Collections.Generic.List<PrologueSectorDefinition>();
        var definitionsArray = world.Definitions;
        CanyonResidentManager manager = null;
        Godot.CharacterBody2D actor = null;
        try
        {
            // Fully owned, shape-free native stream. This tests manager custody, not
            // authored geometry/combat, and must not load/retain the whole map's assets.
            foreach (var id in Enum.GetValues<PrologueSectorId>())
            {
                var template = new PrologueSector { SectorId = id, Bounds = new(0, 0, 1280, 720) };
                var gameplay = new Node2D { Name = "Gameplay" };
                var actors = new Node2D { Name = "Actors" };
                template.AddChild(gameplay); gameplay.AddChild(actors);
                gameplay.Owner = template; actors.Owner = template;
                var packed = new PackedScene(); ownedScenes.Add(packed);
                try { TestAssert.Equal(Error.Ok, packed.Pack(template), "owned shape-free sector packs"); }
                finally { template.Free(); }
                var definition = new PrologueSectorDefinition(id, packed, new((int)id * 1280, 0), new(0, 0, 1280, 720), null, null);
                ownedDefinitions.Add(definition); world.Definitions.Add(definition);
            }
            tree.Root.AddChild(fixtureRoot);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            world.SetPhysicsProcess(false);
            var slot = new ResidentPopulationDefinition("lifetime.review.live", "rock-wanderer", CanyonRouteNodeId.LowCeilingBridge,
                PrologueSectorId.CanyonDescent, ResidentFamily.Native, ResidentArchetype.Wanderer,
                new(new(1500, 2960), new(1300, 2700, 500, 500), new[] { new Vector2(1450, 2960), new Vector2(1550, 2960) }));
            manager = new(world, new[] { slot });
            // The sink holds only a weak managed observer; it cannot pin the manager/world.
            var weakManager = new WeakReference<CanyonResidentManager>(manager);
            bool releaseObservedAttached = false; int completedReleases = 0;
            var journal = new ResidentProfileLifetimeTrace.Journal(line =>
            {
                using var json = JsonDocument.Parse(line["PROFILE_LIFETIME_ROW ".Length..]);
                if (json.RootElement.GetProperty("Kind").GetString() != "lease-release") return;
                TestAssert.True(weakManager.TryGetTarget(out var observed), "owned manager still available at its synchronous release");
                completedReleases++;
                releaseObservedAttached |= observed.Snapshot("lifetime.review.live").Attached;
            });
            using (ResidentProfileLifetimeTrace.UseJournalForContract(journal))
            {
                Check("I2-early-rejection", () =>
                {
                    using (journal.Begin("outer-owner", 37, 41, "outer-slot", "owned/outer"))
                    {
                        long outer = journal.Current.Operation;
                        int start = journal.Rows.Length;
                        bool result = manager.TrySpawn("attempted-missing-slot", out _, out _, out var issues, true);
                        TestAssert.True(!result && issues.Contains("unknown_or_disposed") &&
                            !journal.Rows.Skip(start).Any(x => x.Kind == "spawn-rejected" && x.Context.Operation == outer),
                            "PROFILE_REVIEW_I2_RED: actual early rejected nested TrySpawn must not reject the outer preparation");
                    }
                });
                var resident = new ProfileLifetimeLeaseActor();
                actor = resident;
                resident.AddChild(new Knight.Combat.HealthComponent { Name = "Health", MaxHealth = 3 });
                TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonDescent, out var sector), "owned native sector is really registered");
                sector.GetNode("Gameplay/Actors").AddChild(resident);
                TestAssert.True(manager.TryTrack(slot.StableId, resident, out var lease), "owned native actor obtains an actual manager lease; not gameplay proof");
                resident.Free(); actor = null;
                Check("I3-release-after-detach", () => TestAssert.True(completedReleases == 1 && !releaseObservedAttached &&
                    !manager.Snapshot(slot.StableId).Attached && journal.Rows.Count(x => x.Kind == "lease-release" &&
                    x.ActorId == lease.ActorInstance && x.Generation == lease.Generation) == 1,
                    "PROFILE_REVIEW_I3_RED: completed lease-release must be emitted after actual custody detachment"));
                journal.AssertComplete();
            }
        }
        finally
        {
            if (GodotObject.IsInstanceValid(actor)) actor.Free();
            manager?.Dispose();
            if (GodotObject.IsInstanceValid(fixtureRoot)) fixtureRoot.Free();
            definitionsArray.Clear();
            foreach (var definition in ownedDefinitions)
            {
                definition.Scene = null;
                ((Godot.Collections.Array)definition.NeighborValues).Dispose();
                ((Godot.Collections.Array)definition.KeyInteractionBounds).Dispose(); definition.Dispose();
            }
            ((Godot.Collections.Array)definitionsArray).Dispose();
            foreach (var packed in ownedScenes) packed.Dispose();
        }
        TestAssert.True(failures.Count == 0, "PROFILE_REVIEW_RED: " + string.Join(" | ", failures));
        GD.Print("PROFILE_REVIEW_CONTROLS_OK explicit_subject=true actual_nested_early_rejection=true actual_release_after_detach=true weak_observer_only=true resource_fix=false");

        void Check(string name, Action control)
        {
            try { control(); GD.Print("PROFILE_REVIEW_CONTROL_OK " + name); }
            catch (InvalidOperationException error) { failures.Add(name + ":" + error.Message); GD.Print("PROFILE_REVIEW_CONTROL_FAIL " + name + " " + error.Message); }
        }
    }

    private static void CheckResidentProfileBoundaryValues()
    {
        using var profile = new ResidentSubtypeProfile { SubtypeId = "profile-boundary-control" };
        using var packed = new PackedScene();
        using var missingPacked = new PackedScene();
        var root = new CharacterBody2D();
        var binding = new ResidentSceneBinding { Name = "ResidentBinding", Profile = profile };
        Node copy = null;
        try
        {
            root.AddChild(binding); binding.Owner = root;
            TestAssert.Equal(Error.Ok, packed.Pack(root), "owned plain boundary control packs real exported Profile");
            ulong expectedId = profile.GetInstanceId();
            var source = Task14NativeResourceIdentity.CaptureResidentProfileSource(packed, "boundary-control", "owned/plain");
            TestAssert.True(source.State == "object" && source.ProfileId == expectedId &&
                source.ManagedSerial == profile.Task14ManagedSerial && source.ManagedType == nameof(ResidentSubtypeProfile) &&
                source.SceneId == packed.GetInstanceId() && !source.LocalToScene,
                "RESIDENT_PROFILE_SOURCE_RED: same native PackedScene exports the actual Profile identity as values");
            copy = packed.Instantiate();
            var actual = copy.GetNode<ResidentSceneBinding>("ResidentBinding");
            var firstAssignment = actual.ProfileAssignments;
            TestAssert.True(firstAssignment.BindingSerial > 0 && firstAssignment.Calls > 0 &&
                firstAssignment.NullCalls < firstAssignment.Calls && !firstAssignment.LastWasNull &&
                firstAssignment.LastManagedType == typeof(ResidentSubtypeProfile).FullName &&
                firstAssignment.LastManagedHash == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(profile) &&
                firstAssignment.Faults == 0,
                "RESIDENT_PROFILE_ASSIGNMENT_RED: actual PackedScene export dispatch records its non-null managed setter value without a native identity query");
            var pair = Task14NativeResourceIdentity.ObserveResidentProfileInstance(source, actual.Profile, true);
            TestAssert.True(pair.State == "object" && pair.ProfileId == expectedId &&
                pair.ManagedSerial == source.ManagedSerial && pair.IdentityRelation == "same_native",
                "source and same-instance exported Profile compare actual identities, not a second load");
            actual.Profile = null; // Deliberate control, never a production repair/reproduction claim.
            var clearedAssignment = actual.ProfileAssignments;
            TestAssert.True(clearedAssignment.BindingSerial == firstAssignment.BindingSerial &&
                clearedAssignment.Calls == firstAssignment.Calls + 1 &&
                clearedAssignment.NullCalls == firstAssignment.NullCalls + 1 && clearedAssignment.LastWasNull &&
                clearedAssignment.LastManagedType == "" && clearedAssignment.LastManagedHash == 0,
                "explicit null assignment is counted and cannot borrow the preceding managed identity");
            var absent = Task14NativeResourceIdentity.ObserveResidentProfileInstance(source, actual.Profile, true);
            var skipped = Task14NativeResourceIdentity.ObserveResidentProfileInstance(source, null, false);
            TestAssert.True(absent.State == "null" && absent.ProfileId == 0 && absent.IdentityRelation == "not_comparable" &&
                skipped.State == "not_evaluated" && skipped.ProfileId == 0,
                "null and an unevaluated original gate never borrow the source or previous native identity");
            actual.Profile = profile;
            var reassigned = actual.ProfileAssignments;
            TestAssert.True(reassigned.BindingSerial == firstAssignment.BindingSerial &&
                reassigned.Calls == firstAssignment.Calls + 2 && reassigned.NullCalls == firstAssignment.NullCalls + 1 &&
                !reassigned.LastWasNull && reassigned.LastManagedHash == firstAssignment.LastManagedHash,
                "later non-null setter retains the earlier null count while reporting the latest value only");
            actual.Profile = null; // Keep the original following null controls unchanged.
            binding.Profile = null;
            TestAssert.Equal(Error.Ok, missingPacked.Pack(root), "same real control also packs an unset export");
            var missing = Task14NativeResourceIdentity.CaptureResidentProfileSource(missingPacked, "boundary-missing", "owned/unset");
            TestAssert.True(missing.State is "nil" or "unknown_missing_property" && missing.ProfileId == 0,
                "unset serialized export remains explicitly nil/missing without fabricating the previous Profile");
            var previous = Console.Error;
            Task14NativeResourceIdentity.ResidentProfileSource faulted;
            try
            {
                Console.SetError(new FailingAllocationWriter());
                faulted = Task14NativeResourceIdentity.CaptureResidentProfileSource(packed, "boundary-sink-fault", "owned/plain");
            }
            finally { Console.SetError(previous); }
            TestAssert.True(faulted.State == "fault" && faulted.ProfileId == 0,
                "a failed diagnostic sink returns unknown/fault without throwing or lending partially recorded identity");
            Exception lifecycleFault = null;
            try
            {
                Console.SetError(new FailingAllocationWriter());
                Task14NativeResourceIdentity.BeginResidentProfileTrace();
                Task14NativeResourceIdentity.EndResidentProfileTrace();
            }
            catch (Exception error) { lifecycleFault = error; }
            finally { Console.SetError(previous); }
            TestAssert.True(lifecycleFault == null && !Task14NativeResourceIdentity.ResidentProfileTraceActive,
                "RESIDENT_PROFILE_LIFECYCLE_RED: trace begin/end sink failures cannot replace original phase errors or leave an active observer");
            using var endWriter = new PreservingEndFlushFailureWriter();
            Exception endFailure = null;
            try
            {
                Console.SetError(endWriter);
                Task14NativeResourceIdentity.BeginResidentProfileTrace();
                var endSource = Task14NativeResourceIdentity.CaptureResidentProfileSource(packed, "end-control", "owned/plain");
                Task14NativeResourceIdentity.ObserveResidentProfileInstance(endSource, actual.Profile, true);
                endWriter.FailFlush = true;
                try { Knight.Tests.RunPrologueTests.EndResidentProfileTraceForSuccess(); }
                catch (Exception error) { endFailure = error; }
            }
            finally { Console.SetError(previous); }
            TestAssert.True(endFailure is InvalidOperationException &&
                endWriter.ToString().Contains("RESIDENT_PROFILE_TRACE_END reads=1 pairs=1 unpaired=0 faults=0 truncated=0") &&
                !Task14NativeResourceIdentity.ResidentProfileTraceActive,
                "RESIDENT_PROFILE_END_FLUSH_RED: a clean-looking END followed by Flush failure must veto the actual success path");
            GD.Print("RESIDENT_PROFILE_BOUNDARY_CONTRACT_OK owned_plain_packed=true source_instance=true null_skipped_distinct=true unset_export=true sink_failure_contained=true extra_game_loads=0 resource_fix=false");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(copy)) copy.Free();
            root.Free();
        }
    }

    private sealed class PreservingEndFlushFailureWriter : StringWriter
    {
        internal bool FailFlush { get; set; }
        public override void Flush()
        {
            if (FailFlush) throw new IOException("intentional end Flush failure after retaining the full line");
            base.Flush();
        }
    }

    private static void CheckResidentProfileAssignmentTrace()
    {
        using var profile = new ResidentSubtypeProfile();
        var assigned = new ResidentSceneBinding { Profile = profile };
        var untouched = new ResidentSceneBinding();
        var previous = Console.Error;
        using var writer = new PreservingEndFlushFailureWriter();
        const string path = "res://scenes/enemies/residents/v001/scrap-scatter.tscn";
        try
        {
            Console.SetError(writer);
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            var operation = Task14NativeResourceIdentity.BeginResidentAssignmentOperation("assignment-control", path);
            Task14NativeResourceIdentity.ObserveResidentAssignment(operation, assigned, assigned.Profile, true, false);
            var skipped = Task14NativeResourceIdentity.BeginResidentAssignmentOperation("untouched-control", path);
            Task14NativeResourceIdentity.ObserveResidentAssignment(skipped, untouched, null, false, null);
            bool complete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            string[] rows = writer.ToString().Split('\n').Where(line => line.StartsWith("RESIDENT_ASSIGNMENT_ROW ")).ToArray();
            TestAssert.True(complete && rows.Length == 2,
                "RESIDENT_ASSIGNMENT_TRACE_RED: two original operations emit two managed-only setter/field records and a complete lifecycle");
            using (JsonDocument row = JsonDocument.Parse(rows[0]["RESIDENT_ASSIGNMENT_ROW ".Length..]))
            {
                JsonElement value = row.RootElement;
                TestAssert.True(value.GetProperty("Operation").GetInt64() == operation.Operation &&
                    value.GetProperty("Setter").GetProperty("Calls").GetInt32() == 1 &&
                    value.GetProperty("Setter").GetProperty("NullCalls").GetInt32() == 0 &&
                    value.GetProperty("FieldState").GetString() == "managed_object" &&
                    value.GetProperty("FieldManagedHash").GetInt32() == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(profile),
                    "actual record contains only the same original field and managed setter values");
            }
            using (JsonDocument row = JsonDocument.Parse(rows[1]["RESIDENT_ASSIGNMENT_ROW ".Length..]))
            {
                JsonElement value = row.RootElement;
                TestAssert.True(value.GetProperty("Setter").GetProperty("Calls").GetInt32() == 0 &&
                    !value.GetProperty("Evaluated").GetBoolean() && value.GetProperty("GateMissing").ValueKind == JsonValueKind.Null &&
                    value.GetProperty("FieldState").GetString() == "not_evaluated",
                    "no setter and an unevaluated original gate remain distinct from a null assignment");
            }
            writer.GetStringBuilder().Clear();
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            operation = Task14NativeResourceIdentity.BeginResidentAssignmentOperation("end-flush-control", path);
            Task14NativeResourceIdentity.ObserveResidentAssignment(operation, assigned, assigned.Profile, true, false);
            writer.FailFlush = true;
            Exception endFailure = null;
            try { Knight.Tests.RunPrologueTests.EndResidentAssignmentTraceForSuccess(); }
            catch (Exception error) { endFailure = error; }
            TestAssert.True(endFailure is InvalidOperationException && !Task14NativeResourceIdentity.ResidentAssignmentTraceActive &&
                writer.ToString().Contains("RESIDENT_ASSIGNMENT_TRACE_END operations=1 records=1 unpaired=0 faults=0 truncated=0"),
                "assignment END full-line then Flush failure must veto the actual runner success path");
            bool faultComplete;
            Console.SetError(new FailingAllocationWriter());
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            operation = Task14NativeResourceIdentity.BeginResidentAssignmentOperation("sink-control", path);
            Task14NativeResourceIdentity.ObserveResidentAssignment(operation, assigned, assigned.Profile, true, false);
            faultComplete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            TestAssert.True(!faultComplete && !Task14NativeResourceIdentity.ResidentAssignmentTraceActive,
                "assignment sink faults stay contained and cannot leave a complete or active trace");
            using var capWriter = new StringWriter();
            Console.SetError(capWriter);
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            for (int index = 0; index < 513; index++)
            {
                operation = Task14NativeResourceIdentity.BeginResidentAssignmentOperation("cap-control", path);
                Task14NativeResourceIdentity.ObserveResidentAssignment(operation, assigned, assigned.Profile, true, false);
            }
            bool capComplete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            TestAssert.True(!capComplete && !Task14NativeResourceIdentity.ResidentAssignmentTraceActive &&
                capWriter.ToString().Split('\n').Count(line => line.StartsWith("RESIDENT_ASSIGNMENT_ROW ")) == 512 &&
                capWriter.ToString().Contains("operations=513 records=512 unpaired=1 faults=0 truncated=1"),
                "RESIDENT_ASSIGNMENT_CAP_RED: the 513th operation emits no row and must veto completeness without changing game execution");
            Console.SetError(previous);
            GD.Print("RESIDENT_ASSIGNMENT_CONTRACT_OK managed_setter=true untouched_distinct=true original_field=true sink_failure_contained=true end_flush_veto=true cap_veto=true added_native_queries=0 resource_fix=false");
        }
        finally
        {
            Console.SetError(previous);
            Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            assigned.Free(); untouched.Free();
        }
    }

    private static void CheckResidentAfterRejectionSource(string storage)
    {
        using var profile = new ResidentSubtypeProfile();
        using var packed = new PackedScene();
        var root = new CharacterBody2D();
        var binding = new ResidentSceneBinding { Name = "ResidentBinding", Profile = profile };
        Node copy = null;
        var previous = Console.Error;
        using var writer = new StringWriter();
        const string path = "res://scenes/enemies/residents/v001/scrap-scatter.tscn";
        try
        {
            root.AddChild(binding); binding.Owner = root;
            TestAssert.Equal(Error.Ok, packed.Pack(root), "owned after-rejection control packs an actual non-null source");
            copy = packed.Instantiate();
            var actual = copy.GetNode<ResidentSceneBinding>("ResidentBinding");
            actual.Profile = null; // Deliberate control, not reproduction of the game defect.
            var before = actual.ProfileAssignments;
            ulong sourceId = packed.GetInstanceId(), profileId = profile.GetInstanceId();
            Console.SetError(writer);
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            var operation = Task14NativeResourceIdentity.BeginResidentAssignmentOperation("after-rejection-control", path);
            bool rowWritten = Task14NativeResourceIdentity.ObserveResidentAssignment(operation, actual, actual.Profile, true, true);
            var noAck = Task14NativeResourceIdentity.ObserveResidentRejectionSource(operation, packed, false, true);
            var notMissing = Task14NativeResourceIdentity.ObserveResidentRejectionSource(operation, packed, true, false);
            var unevaluated = Task14NativeResourceIdentity.ObserveResidentRejectionSource(operation, packed, true, null);
            var snapshot = Task14NativeResourceIdentity.ObserveResidentRejectionSource(operation, packed, rowWritten, true);
            bool complete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            TestAssert.True(rowWritten && complete && noAck.State == null && notMissing.State == null && unevaluated.State == null &&
                snapshot.State == "object" && snapshot.SceneId == sourceId &&
                snapshot.ProfileId == profileId && snapshot.ManagedSerial == profile.Task14ManagedSerial &&
                snapshot.ManagedType == nameof(ResidentSubtypeProfile) && actual.Profile == null &&
                actual.ProfileAssignments == before && GodotObject.IsInstanceValid(packed) && GodotObject.IsInstanceValid(profile),
                "RESIDENT_AFTER_REJECTION_RED: same owned PackedScene yields after-state without repairing the original null field or disposing borrowed resources");
            string text = writer.ToString();
            string row = text.Split('\n').Single(line => line.StartsWith("RESIDENT_ASSIGNMENT_REJECTION_SOURCE "));
            using JsonDocument json = JsonDocument.Parse(row["RESIDENT_ASSIGNMENT_REJECTION_SOURCE ".Length..]);
            TestAssert.True(json.RootElement.GetProperty("Operation").GetInt64() == operation.Operation &&
                json.RootElement.GetProperty("Stage").GetString() == "after_original_profile_null_gate" &&
                json.RootElement.GetProperty("NotAtConversion").GetBoolean() &&
                text.IndexOf("RESIDENT_ASSIGNMENT_ROW ", StringComparison.Ordinal) < text.IndexOf("RESIDENT_ASSIGNMENT_REJECTION_SOURCE ", StringComparison.Ordinal) &&
                !text.Contains("RESIDENT_PROFILE_SOURCE "), "after-state is correlated, explicitly later, and never mixed with the old source-before protocol");
            using var missing = new PackedScene();
            binding.Profile = null;
            TestAssert.Equal(Error.Ok, missing.Pack(root), "actual rejection control packs its deliberately unset export");
            string savedPath = Path.Combine(storage, "after-rejection-unset-control.tscn");
            TestAssert.Equal(Error.Ok, ResourceSaver.Save(missing, savedPath, ResourceSaver.SaverFlags.ChangePath), "owned control saves only in isolated case storage");
            using var loaded = ResourceLoader.Load<PackedScene>(savedPath, cacheMode: ResourceLoader.CacheMode.Ignore);
            TestAssert.Equal(savedPath.Replace('\\', '/'), loaded.ResourcePath.Replace('\\', '/'), "actual factory path gate uses the same privately loaded saved control");
            var expected = CanyonResidentCatalog.Subtypes.Single(type => type.Id == "scrap-scatter") with
                { ScenePath = savedPath, SceneState = ResidentSceneState.ExplicitPlaceholder };
            var slot = CanyonResidentCatalog.All.First(value => value.SubtypeId == expected.Id);
            writer.GetStringBuilder().Clear();
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            ArgumentException rejected = null;
            try { ResidentSceneBinding.Instantiate(loaded, expected, slot, true); }
            catch (ArgumentException error) { rejected = error; }
            bool rejectionComplete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            string[] rejectedRows = writer.ToString().Split('\n').Where(line => line.StartsWith("RESIDENT_ASSIGNMENT_REJECTION_SOURCE ")).ToArray();
            TestAssert.True(rejected != null && rejected.Message.Contains("profile_missing=True") &&
                rejectionComplete && rejectedRows.Length == 1,
                "RESIDENT_AFTER_REJECTION_FACTORY_RED: the actual unchanged null gate records exactly one later source and preserves its original rejection");
            using (JsonDocument rejectedJson = JsonDocument.Parse(rejectedRows[0]["RESIDENT_ASSIGNMENT_REJECTION_SOURCE ".Length..]))
                TestAssert.True(rejectedJson.RootElement.GetProperty("Snapshot").GetProperty("State").GetString() is "nil" or "unknown_missing_property",
                    "unset saved export is nil or unknown-missing, never borrowed from the previous non-null source");
            using var failedFlush = new PreservingEndFlushFailureWriter { FailFlush = true };
            Console.SetError(failedFlush);
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            rejected = null;
            try { ResidentSceneBinding.Instantiate(loaded, expected, slot, true); }
            catch (ArgumentException error) { rejected = error; }
            bool failedComplete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            TestAssert.True(rejected != null && rejected.Message.Contains("profile_missing=True") && !failedComplete &&
                !failedFlush.ToString().Contains("RESIDENT_ASSIGNMENT_REJECTION_SOURCE "),
                "ROW Flush failure vetoes the later native read while preserving the original factory rejection");
            Console.SetError(previous);
            GD.Print("RESIDENT_AFTER_REJECTION_CONTROL_OK same_packed=true original_null_preserved=true borrowed_valid=true actual_factory=true flush_veto=true second_bridge=true original_variant_proved=false");
        }
        finally
        {
            Console.SetError(previous);
            Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            if (GodotObject.IsInstanceValid(copy)) copy.Free();
            root.Free();
        }
    }

    private static void CheckResidentAssignmentSerialScopes(string storage)
    {
        using var profile = new ResidentSubtypeProfile();
        using var packed = new PackedScene();
        var root = new CharacterBody2D();
        var binding = new ResidentSceneBinding { Name = "ResidentBinding", Profile = profile,
            PresentationState = ResidentSceneState.ExplicitPlaceholder };
        TextWriter previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            root.AddChild(binding); binding.Owner = root;
            TestAssert.Equal(Error.Ok, packed.Pack(root), "serial scope control owns a real exported Profile");
            Console.SetError(writer);
            Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
            foreach (string subtype in new[] { "rock-wanderer", "bone-charger" })
            {
                string path = Path.Combine(storage, "serial-scope-" + subtype + ".tscn");
                TestAssert.Equal(Error.Ok, ResourceSaver.Save(packed, path, ResourceSaver.SaverFlags.ChangePath),
                    "private owned saved control has the actual factory path");
                packed.TakeOverPath(path); // Only this fresh case-owned path; saving alone leaves ResourcePath empty.
                var expected = CanyonResidentCatalog.Subtypes.Single(type => type.Id == subtype) with
                    { ScenePath = path, SceneState = ResidentSceneState.ExplicitPlaceholder };
                var slot = CanyonResidentCatalog.All.First(value => value.SubtypeId == subtype);
                ArgumentException rejected = null;
                try { ResidentSceneBinding.Instantiate(packed, expected, slot, allowPlaceholder: false); }
                catch (ArgumentException error) { rejected = error; }
                TestAssert.True(rejected != null && rejected.Message.Contains("profile_missing=False") &&
                    rejected.Message.Contains("actual_state=1") && rejected.Message.Contains("allow_placeholder=False"),
                    "original non-null placeholder admission still rejects before ConfigureActor: " +
                    (rejected?.Message ?? "no rejection") + " source=" + packed.ResourcePath + " expected=" + path);
            }
            binding.Profile = null;
            TestAssert.Equal(Error.Ok, packed.Pack(root), "same owned control deliberately removes its Profile");
            string missingPath = Path.Combine(storage, "serial-scope-null-rock.tscn");
            TestAssert.Equal(Error.Ok, ResourceSaver.Save(packed, missingPath), "null control stays in private storage");
            packed.TakeOverPath(missingPath);
            var nullExpected = CanyonResidentCatalog.Subtypes.Single(type => type.Id == "rock-wanderer") with
                { ScenePath = missingPath, SceneState = ResidentSceneState.ExplicitPlaceholder };
            var nullSlot = CanyonResidentCatalog.All.First(value => value.SubtypeId == nullExpected.Id);
            ArgumentException nullRejected = null;
            try { ResidentSceneBinding.Instantiate(packed, nullExpected, nullSlot, allowPlaceholder: false); }
            catch (ArgumentException error) { nullRejected = error; }
            TestAssert.True(nullRejected != null && nullRejected.Message.Contains("profile_missing=True"),
                "non-scrap null keeps its original rejection, not a repaired Profile");
            bool complete = Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            string[] rows = writer.ToString().Split('\n').Where(line =>
                line.StartsWith("RESIDENT_ASSIGNMENT_ROW ", StringComparison.Ordinal)).ToArray();
            TestAssert.True(complete && rows.Length == 3 &&
                !writer.ToString().Contains("RESIDENT_ASSIGNMENT_REJECTION_SOURCE ", StringComparison.Ordinal),
                "RESIDENT_SERIAL_SCOPE_RED: real non-scrap factories record shared and null fields without new after-source reads");
            string[] subtypes = { "rock-wanderer", "bone-charger" };
            for (int index = 0; index < subtypes.Length; index++)
            {
                using JsonDocument json = JsonDocument.Parse(rows[index]["RESIDENT_ASSIGNMENT_ROW ".Length..]);
                JsonElement row = json.RootElement;
                var slot = CanyonResidentCatalog.All.First(value => value.SubtypeId == subtypes[index]);
                TestAssert.True(row.GetProperty("Slot").GetString() == slot.StableId &&
                    row.GetProperty("Path").GetString() == Path.Combine(storage, "serial-scope-" + subtypes[index] + ".tscn") &&
                    row.GetProperty("FieldState").GetString() == "managed_object" &&
                    row.GetProperty("FieldManagedSerial").GetInt64() == profile.Task14ManagedSerial &&
                    row.GetProperty("Setter").GetProperty("LastManagedSerial").GetInt64() == profile.Task14ManagedSerial &&
                    !row.GetProperty("CaptureAfterSource").GetBoolean() &&
                    row.GetProperty("FieldManagedHash").GetInt32() == System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(profile),
                    "same actual Profile serial retains both original leaf/slot uses, not a guessed exclusive owner");
            }
            using JsonDocument nullJson = JsonDocument.Parse(rows[2]["RESIDENT_ASSIGNMENT_ROW ".Length..]);
            TestAssert.True(nullJson.RootElement.GetProperty("FieldState").GetString() == "null" &&
                nullJson.RootElement.GetProperty("FieldManagedSerial").GetInt64() == 0 &&
                nullJson.RootElement.GetProperty("Setter").GetProperty("LastManagedSerial").GetInt64() == 0 &&
                !nullJson.RootElement.GetProperty("CaptureAfterSource").GetBoolean(),
                "non-scrap null neither borrows a serial nor authorizes an extra native source read");
        }
        finally
        {
            Console.SetError(previous);
            Task14NativeResourceIdentity.EndResidentAssignmentTrace();
            root.Free();
            previous.Write(writer.ToString()); previous.Flush();
        }
        GD.Print("RESIDENT_SERIAL_SCOPE_OK actual_factory=true non_scrap=3 shared_serial=true null_without_source=true original_rejection=true added_getstate=0 native_owner_proved=false resource_fix=false");
    }

    private static void CheckMobilityDisposeCoverage()
    {
        TextWriter previous = Console.Error;
        var writer = new StringWriter(CultureInfo.InvariantCulture);
        MobilityModuleDefinition module = null;
        try
        {
            Console.SetError(writer);
            long before = Task14NativeResourceIdentity.NextSerial();
            module = new MobilityModuleDefinition();
            int hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(module);
            long after = Task14NativeResourceIdentity.NextSerial();
            module.Dispose();
            string[] rows = writer.ToString().Split('\n').Where(line =>
                line.Contains(" type=Knight.Config.MobilityModuleDefinition ", StringComparison.Ordinal) &&
                line.Contains($" managed_hash={hash} ", StringComparison.Ordinal)).ToArray();
            TestAssert.True(rows.Length == 2 && after == before + 2 &&
                rows[0].StartsWith("REFCOUNTED_IDENTITY ENTER ", StringComparison.Ordinal) &&
                rows[1].StartsWith("REFCOUNTED_IDENTITY RETURN ", StringComparison.Ordinal) &&
                rows.All(line => line.Contains(" disposing=True ", StringComparison.Ordinal) &&
                    line.Contains($" managed_serial={before + 1} ", StringComparison.Ordinal)) &&
                Regex.Match(rows[0], @" call=(\d+) ").Groups[1].Value ==
                    Regex.Match(rows[1], @" call=(\d+) ").Groups[1].Value &&
                module.NativeInstance == IntPtr.Zero,
                "MOBILITY_DISPOSE_COVERAGE_RED: owned real module emits paired same-serial disposal records and preserves base release");
        }
        finally
        {
            Console.SetError(previous);
            if (module != null && module.NativeInstance != IntPtr.Zero) module.Dispose();
            previous.Write(writer.ToString());
            previous.Flush();
        }
        GD.Print("MOBILITY_DISPOSE_COVERAGE_OK actual_owned_module=true paired_serial=true base_release=true added_gc=false added_native_identity_queries=0 resource_fix=false");
    }

    private static MethodInfo SectorAllocationMethod(string name)
    {
        var method = typeof(Task14NativeResourceIdentity).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        TestAssert.True(method != null, "sector allocation diagnostic method must exist: " + name);
        return method;
    }

    private static long SectorAllocationValue(string name)
    {
        var property = typeof(Task14NativeResourceIdentity).GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic);
        TestAssert.True(property != null, "sector allocation diagnostic counter must exist: " + name);
        return (long)property.GetValue(null);
    }

    private static void CheckSectorAllocation(string context, long operation, string slot, string path,
        bool parameterized = false)
    {
        var previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        PrologueSectorDefinition definition = null;
        try
        {
            long before = Task14NativeResourceIdentity.NextSerial();
            Console.SetError(writer);
            definition = parameterized
                ? new PrologueSectorDefinition(PrologueSectorId.CanyonBasin, null, Vector2.Zero,
                    new Rect2(0, 0, 1280, 720), PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonAscent)
                : new PrologueSectorDefinition();
            long after = Task14NativeResourceIdentity.NextSerial();
            TestAssert.True(definition.Task14ManagedSerial == before + 1 && after == before + 2,
                "sector initializer advances the shared serial exactly once for either real constructor");
            string[] rows = writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(value => value.StartsWith("TASK14_SECTOR_ALLOCATION ", StringComparison.Ordinal)).ToArray();
            TestAssert.True(rows.Length == 1,
                "SECTOR_ALLOCATION_RED: actual definition constructor must emit one managed allocation origin: " + context);
            using JsonDocument json = JsonDocument.Parse(rows[0]["TASK14_SECTOR_ALLOCATION ".Length..]);
            JsonElement row = json.RootElement;
            string stack = row.GetProperty("ManagedStack").GetString();
            TestAssert.True(row.GetProperty("ManagedType").GetString() == "Knight.Prologue.Seamless.PrologueSectorDefinition" &&
                row.GetProperty("ManagedSerial").GetInt64() == definition.Task14ManagedSerial &&
                row.GetProperty("ManagedThreadId").GetInt32() == System.Environment.CurrentManagedThreadId &&
                row.GetProperty("ContextKind").GetString() == context &&
                row.GetProperty("Operation").GetInt64() == operation &&
                row.GetProperty("Slot").GetString() == slot && row.GetProperty("Path").GetString() == path &&
                !string.IsNullOrWhiteSpace(stack) && stack.Contains("PrologueSectorDefinition..ctor", StringComparison.Ordinal) &&
                !row.GetProperty("StackTruncated").GetBoolean(),
                "sector allocation preserves this wrapper's serial/type/managed origin without native ownership inference");
            definition.Dispose();
            string[] releaseRows = writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(value => value.StartsWith("REFCOUNTED_IDENTITY ", StringComparison.Ordinal) &&
                    value.Contains("type=Knight.Prologue.Seamless.PrologueSectorDefinition ", StringComparison.Ordinal)).ToArray();
            TestAssert.True(releaseRows.Length == 2 && releaseRows[0].StartsWith("REFCOUNTED_IDENTITY ENTER ", StringComparison.Ordinal) &&
                releaseRows[1].StartsWith("REFCOUNTED_IDENTITY RETURN ", StringComparison.Ordinal) &&
                releaseRows.All(value => value.Contains("managed_serial=" + definition.Task14ManagedSerial + " ", StringComparison.Ordinal) &&
                    value.EndsWith("native_identity=unknown", StringComparison.Ordinal)) &&
                Regex.Match(releaseRows[0], @"\bcall=([0-9]+)").Value == Regex.Match(releaseRows[1], @"\bcall=([0-9]+)").Value,
                "actual explicit release joins the constructor serial once and never fabricates a native owner");
        }
        finally
        {
            if (definition != null && definition.NativeInstance != IntPtr.Zero) definition.Dispose();
            Console.SetError(previous); previous.Write(writer.ToString()); previous.Flush();
        }
    }

    private static void CheckOtherThreadSectorAllocation()
    {
        var previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var allocate = SectorAllocationMethod("NextSectorAllocationSerial");
        int mainThread = System.Environment.CurrentManagedThreadId;
        try
        {
            Console.SetError(writer);
            long before = Task14NativeResourceIdentity.NextSerial();
            long serial = Task.Run(() => (long)allocate.Invoke(null, null)).GetAwaiter().GetResult();
            long after = Task14NativeResourceIdentity.NextSerial();
            string line = writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Single();
            using JsonDocument json = JsonDocument.Parse(line["TASK14_SECTOR_ALLOCATION ".Length..]);
            JsonElement row = json.RootElement;
            TestAssert.True(serial == before + 1 && after == before + 2 &&
                row.GetProperty("ManagedSerial").GetInt64() == serial &&
                row.GetProperty("ManagedThreadId").GetInt32() != mainThread &&
                row.GetProperty("ContextKind").GetString() == "other-thread-unscoped" &&
                row.GetProperty("Operation").GetInt64() == 0 &&
                row.GetProperty("Slot").GetString() == "" && row.GetProperty("Path").GetString() == "",
                "pure managed sector allocation on a worker never borrows an active main-thread operation");
        }
        finally { Console.SetError(previous); previous.Write(writer.ToString()); previous.Flush(); }
    }

    private static void CheckFailedSectorAllocationSink()
    {
        var previous = Console.Error;
        PrologueSectorDefinition definition = null;
        long losses = SectorAllocationValue("SectorAllocationLosses");
        long before = Task14NativeResourceIdentity.NextSerial();
        try
        {
            Console.SetError(new FailingAllocationWriter());
            definition = new PrologueSectorDefinition();
        }
        finally { Console.SetError(previous); }
        try
        {
            long after = Task14NativeResourceIdentity.NextSerial();
            TestAssert.True(definition != null && definition.Task14ManagedSerial == before + 1 && after == before + 2 &&
                SectorAllocationValue("SectorAllocationLosses") == losses + 1,
                "sector construction survives the failed sink while preserving serial and completeness loss");
            AssertIncompleteSectorAllocationVeto();
        }
        finally { definition?.Dispose(); }
    }

    private static void CheckSectorAllocationCap()
    {
        var previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var allocate = SectorAllocationMethod("NextSectorAllocationSerial");
        try
        {
            Console.SetError(TextWriter.Null);
            while (SectorAllocationValue("SectorAllocationRows") < 4096) allocate.Invoke(null, null);
            Console.SetError(writer);
            long losses = SectorAllocationValue("SectorAllocationLosses");
            long before = Task14NativeResourceIdentity.NextSerial();
            long first = (long)allocate.Invoke(null, null);
            long second = (long)allocate.Invoke(null, null);
            long after = Task14NativeResourceIdentity.NextSerial();
            TestAssert.True(first == before + 1 && second == before + 2 && after == before + 3 &&
                SectorAllocationValue("SectorAllocationLosses") == losses + 2 &&
                writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length == 1 &&
                writer.ToString().StartsWith("TASK14_SECTOR_ALLOCATION_CAP rows=4096 completeness=unknown", StringComparison.Ordinal),
                "sector cap emits once and counts every omitted origin without losing the serial");
            AssertIncompleteSectorAllocationVeto();
        }
        finally { Console.SetError(previous); previous.Write(writer.ToString()); previous.Flush(); }
    }

    private static void AssertIncompleteSectorAllocationVeto()
    {
        bool rejected = false;
        try { SectorAllocationMethod("AssertSectorAllocationComplete").Invoke(null, null); }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException error &&
            error.Message.StartsWith("Sector allocation context incomplete: lost=", StringComparison.Ordinal))
        { rejected = true; }
        TestAssert.True(rejected, "lost sector origin must veto diagnostic completeness");
    }

    private static void CheckPresentationAllocation(string context, long operation, string slot, string path)
    {
        var previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        PresentationProfile profile = null;
        try
        {
            long before = Task14NativeResourceIdentity.NextSerial();
            Console.SetError(writer);
            profile = new PresentationProfile();
            long after = Task14NativeResourceIdentity.NextSerial();
            TestAssert.True(profile.Task14ManagedSerial == before + 1 && after == before + 2,
                "actual PresentationProfile initializer advances shared serial exactly once");
            string[] lines = writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] rows = lines.Where(value => value.StartsWith("TASK14_PRESENTATION_ALLOCATION ", StringComparison.Ordinal)).ToArray();
            TestAssert.True(rows.Length == 1,
                "actual PresentationProfile constructor must emit exactly one matching allocation-context row: " + context);
            using JsonDocument json = JsonDocument.Parse(rows[0]["TASK14_PRESENTATION_ALLOCATION ".Length..]);
            JsonElement row = json.RootElement;
            TestAssert.True(row.GetProperty("ManagedSerial").GetInt64() == profile.Task14ManagedSerial &&
                row.GetProperty("ManagedThreadId").GetInt32() == System.Environment.CurrentManagedThreadId &&
                row.GetProperty("ContextKind").GetString() == context &&
                row.GetProperty("Operation").GetInt64() == operation &&
                row.GetProperty("Slot").GetString() == slot && row.GetProperty("Path").GetString() == path &&
                !string.IsNullOrWhiteSpace(row.GetProperty("ManagedStack").GetString()) &&
                !row.GetProperty("StackTruncated").GetBoolean(),
                "allocation row preserves actual serial, complete managed origin, and only the current same-thread context");
        }
        finally
        {
            profile?.Dispose();
            Console.SetError(previous); previous.Write(writer.ToString()); previous.Flush();
        }
    }

    private static void CheckOtherThreadAllocation()
    {
        var previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        int mainThread = System.Environment.CurrentManagedThreadId;
        try
        {
            Console.SetError(writer);
            long before = Task14NativeResourceIdentity.NextSerial();
            // The worker calls only the pure managed logger, never a Godot API.
            long serial = Task.Run(Task14NativeResourceIdentity.NextPresentationAllocationSerial).GetAwaiter().GetResult();
            long after = Task14NativeResourceIdentity.NextSerial();
            string line = writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Single();
            using JsonDocument json = JsonDocument.Parse(line["TASK14_PRESENTATION_ALLOCATION ".Length..]);
            JsonElement row = json.RootElement;
            TestAssert.True(serial == before + 1 && after == before + 2 &&
                row.GetProperty("ManagedSerial").GetInt64() == serial &&
                row.GetProperty("ManagedThreadId").GetInt32() != mainThread &&
                row.GetProperty("ContextKind").GetString() == "other-thread-unscoped" &&
                row.GetProperty("Operation").GetInt64() == 0 &&
                row.GetProperty("Slot").GetString() == "" && row.GetProperty("Path").GetString() == "",
                "worker allocation cannot borrow the simultaneously active main-thread probe");
        }
        finally { Console.SetError(previous); previous.Write(writer.ToString()); previous.Flush(); }
    }

    private static void CheckFailedAllocationSink()
    {
        var previous = Console.Error;
        PresentationProfile profile = null;
        long losses = Task14NativeResourceIdentity.PresentationAllocationLosses;
        long before = Task14NativeResourceIdentity.NextSerial();
        try
        {
            Console.SetError(new FailingAllocationWriter());
            profile = new PresentationProfile();
        }
        finally { Console.SetError(previous); }
        try
        {
            long after = Task14NativeResourceIdentity.NextSerial();
            TestAssert.True(profile != null && profile.Task14ManagedSerial == before + 1 && after == before + 2 &&
                Task14NativeResourceIdentity.PresentationAllocationLosses == losses + 1,
                "actual native profile construction survives a failed diagnostic sink with one unique serial");
            AssertIncompleteAllocationVeto();
        }
        finally { profile?.Dispose(); }
    }

    private static void CheckAllocationCap()
    {
        var previous = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            Console.SetError(TextWriter.Null);
            while (Task14NativeResourceIdentity.PresentationAllocationRows < Task14NativeResourceIdentity.MaximumPresentationAllocationRows)
                Task14NativeResourceIdentity.NextPresentationAllocationSerial();
            Console.SetError(writer);
            long losses = Task14NativeResourceIdentity.PresentationAllocationLosses;
            long before = Task14NativeResourceIdentity.NextSerial();
            long first = Task14NativeResourceIdentity.NextPresentationAllocationSerial();
            long second = Task14NativeResourceIdentity.NextPresentationAllocationSerial();
            long after = Task14NativeResourceIdentity.NextSerial();
            TestAssert.True(first == before + 1 && second == before + 2 && after == before + 3 &&
                Task14NativeResourceIdentity.PresentationAllocationLosses == losses + 2 &&
                writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length == 1 &&
                writer.ToString().StartsWith("TASK14_PRESENTATION_ALLOCATION_CAP ", StringComparison.Ordinal),
                "cap logs once, retains unique serials, and marks every omitted allocation incomplete");
            AssertIncompleteAllocationVeto();
        }
        finally { Console.SetError(previous); previous.Write(writer.ToString()); previous.Flush(); }
    }

    private static void AssertIncompleteAllocationVeto()
    {
        bool rejected = false;
        try { Task14NativeResourceIdentity.AssertPresentationAllocationComplete(); }
        catch (InvalidOperationException exception) when (exception.Message.StartsWith("Presentation allocation context incomplete: lost=", StringComparison.Ordinal))
        { rejected = true; }
        TestAssert.True(rejected, "lost allocation context must veto diagnostic completeness");
    }

    private sealed class FailingAllocationWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string value) => throw new IOException("owned allocation sink fault");
    }

    private static long Check(Resource resource,string source)
    {
        var previous=Console.Error;
        using var writer=new StringWriter(CultureInfo.InvariantCulture);
        ulong nativeId=resource.GetInstanceId();string type=resource.GetType().FullName;
        bool disposed=false;
        try
        {
            Console.SetError(writer);
            TestAssert.True(GodotObject.IsInstanceIdValid(nativeId),"actual contract native resource starts alive");
            Task14NativeResourceIdentity.ObserveResource(resource,37,"contract-source/"+source,source,"contract-create");
            resource.Dispose();disposed=true;
            string[] lines=writer.ToString().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            string observed=lines.Single(x=>x.StartsWith("TASK14_NATIVE_IDENTITY_OBSERVED "));
            var serialMatch=Regex.Match(observed,@"\bmanaged_serial=([1-9][0-9]*)\b");
            TestAssert.True(serialMatch.Success&&observed.Contains("native_id="+nativeId+" ")&&observed.EndsWith("registered=yes"),
                "actual owned Profile/Definition requires nonzero creation serial and native/source association: "+observed);
            long serial=long.Parse(serialMatch.Groups[1].Value,CultureInfo.InvariantCulture);
            string expected="native_identity=observed native_id="+nativeId+" resource_path= owner_id=37 owner_path=contract-source/"+source+" owner_label="+source+" observed_phase=contract-create";
            TestAssert.Equal(expected,Task14NativeResourceIdentity.DisposeIdentity(serial),"after actual Dispose mapping depends only on recorded scalar values");
            TestAssert.True(!GodotObject.IsInstanceIdValid(nativeId),"registry cannot prolong disposed native resource; not a managed-GC claim");
            string[] enter=lines.Where(x=>x.StartsWith("REFCOUNTED_IDENTITY ENTER ")&&x.Contains("type="+type+" ")).ToArray();
            string[] returned=lines.Where(x=>x.StartsWith("REFCOUNTED_IDENTITY RETURN ")&&x.Contains("type="+type+" ")).ToArray();
            TestAssert.True(enter.Length==1&&returned.Length==1,"one actual explicit Dispose has one ENTER/RETURN pair");
            TestAssert.Equal(Regex.Match(enter[0],@"\bcall=([0-9]+)").Value,Regex.Match(returned[0],@"\bcall=([0-9]+)").Value,"same actual dispose call");
            foreach(string line in enter.Concat(returned))
                TestAssert.True(line.Contains("disposing=True ")&&line.Contains("managed_serial="+serial+" ")&&line.EndsWith(expected),"actual release log must retain matching creation serial/source/native identity");
            return serial;
        }
        finally
        {
            if(!disposed)resource.Dispose();
            Console.SetError(previous);previous.Write(writer.ToString());previous.Flush();
        }
    }
}

// Real native body/Health/TreeExiting, minimal interface carrier only. No game
// AI, fixture combat certificate, borrowed shapes or saved-asset ownership.
internal partial class ProfileLifetimeLeaseActor : CharacterBody2D,
    Knight.Enemies.Residents.IResidentEnemyAgent, Knight.Prologue.IEncounterActor
{
#pragma warning disable CS0067
    public event Action Defeated;
#pragma warning restore CS0067
    public void ConfigureResidence(ResidentPopulationDefinition definition) { }
    public void SetResidentDormant(bool dormant) { }
    public void ResetForCheckpoint() { }
}
#endif
