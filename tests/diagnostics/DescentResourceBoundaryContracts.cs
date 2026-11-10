#if REFCOUNTED_FATAL_IDENTITY
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;

namespace Knight.Tests.Diagnostics;

// Separate native contract. Never runs before the original pre suite.
internal static class DescentResourceBoundaryContracts
{
    private const string Complete="PROFILE_LIFETIME_PRE_TRACE_COMPLETE checked_faults_and_scopes=true resource_fix=false behavior_accepted=false";
    private readonly record struct Snapshot(ulong RootId,ulong WorldId,int Definitions,
        long ProfileSerial,ulong ProfileNativeId,long MotorSerial,ulong MotorNativeId,long CombatSerial,ulong CombatNativeId);

    internal static async Task Run(SceneTree tree)
    {
        MethodInfo Find(Type type,string name)=>type.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static);
        var before=Find(typeof(DescentClawGuidanceRuntimeTests),"ObserveBeforeMountOwned");
        var observe=Find(typeof(DescentClawGuidanceRuntimeTests),"ObserveAssignedStartupResources");
        var guard=Find(typeof(RunPrologueTests),"AssertPreLifetimeTraceCompleteForSuccess");
        var missing=new[]{before==null?"before-owner":"",observe==null?"field-observer":"",guard==null?"pre-guard":""}.Where(x=>x.Length!=0);
        TestAssert.True(before!=null&&observe!=null&&guard!=null,"DESCENT_BOUNDARY_RED missing="+string.Join(',',missing));
        CheckGuard(guard);
        Task14NativeResourceIdentity.BeginOriginalProbeTrace();
        // _Initialize's first synchronous segment runs before the SceneTree
        // root enters. This wait belongs only to this independent contract.
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        TextWriter saved=Console.Error;
        PrologueLevelController level=null,failed=null,mountedFailure=null;
        Node unrelated=null;
        Exception primaryFailure=null;
        try
        {
            unrelated=new Node();
            level=DescentClawGuidanceRuntimeTests.CreateLevelFixture();
            Snapshot expectedBefore=Read(level),expectedAfter=default;
            using var output=new StringWriter(CultureInfo.InvariantCulture);
            Console.SetError(output);
            try
            {
                Invoke(before,level);
                tree.Root.AddChild(level);
                expectedAfter=Read(level);
                Invoke(observe,level,"descent.contract.after-mount");
            }
            finally {Console.SetError(saved);Console.Error.Write(output.ToString());Console.Error.Flush();}
            var lines=output.ToString().Split('\n').Where(x=>x.StartsWith("DESCENT_ASSIGNED_STARTUP_BOUNDARY ",StringComparison.Ordinal)).ToArray();
            TestAssert.Equal(2,lines.Length,"both real mount stages must be observed exactly once");
            CheckRow(lines[0],expectedBefore,"descent.before-add-child",false);
            CheckRow(lines[1],expectedAfter,"descent.contract.after-mount",true);
            // Retiring an owned scene must revoke deferred startup BEFORE the
            // first audio-drain await, not wait for startup to run after teardown.
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            var audio=level.GetNode<Knight.Audio.PrologueAudioDirector>("Audio");
            var initialFlow=flow.State;
            Task drain=TestSceneTeardown.DrainAudioOwner(tree,level);
            bool retiredImmediately=flow.IsApplicationQuitting&&audio.IsQuiescing&&!level.ResidentPreparationPending;
            Exception drainFailure=null;
            try {await drain;} catch(Exception error){drainFailure=error;}
            TestAssert.True(retiredImmediately&&flow.State==initialFlow,
                $"DESCENT_TEARDOWN_STARTUP_RED: owned teardown must synchronously revoke deferred startup; immediate={retiredImmediately}; initial={initialFlow}; actual={flow.State}; drain={drainFailure}");
            if(drainFailure!=null)ExceptionDispatchInfo.Capture(drainFailure).Throw();
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);level=null;
            failed=DescentClawGuidanceRuntimeTests.CreateLevelFixture();
            var primary=new IOException("owned Descent snapshot sink failure");
            Exception actual=null;
            Console.SetError(new SnapshotFailureOnce(saved,primary));
            try {Invoke(before,failed);}
            catch(Exception error){actual=error;}
            finally {Console.SetError(saved);}
            TestAssert.True(ReferenceEquals(primary,actual)&&!GodotObject.IsInstanceValid(failed)&&GodotObject.IsInstanceValid(unrelated),
                "real before-mount observation failure releases its owned root, preserves exact primary, and leaves unrelated owner alive");
            int added=0;
            void Added(Node node)
            {
                if(node is PrologueLevelController root&&root.SceneFilePath=="res://scenes/prologue/PrologueLevel.tscn")
                {added++;mountedFailure??=root;}
            }
            var afterPrimary=new IOException("owned Descent mounted snapshot failure");
            Exception afterActual=null;
            tree.NodeAdded+=Added;
            Console.SetError(new SnapshotFailureOnce(saved,afterPrimary,"descent.after-startup-bindings"));
            try {await DescentClawGuidanceRuntimeTests.Run(tree);}
            catch(Exception error){afterActual=error;}
            finally {Console.SetError(saved);tree.NodeAdded-=Added;}
            TestAssert.True(ReferenceEquals(afterPrimary,afterActual)&&added==1&&
                !ReferenceEquals(mountedFailure,null)&&!GodotObject.IsInstanceValid(mountedFailure)&&GodotObject.IsInstanceValid(unrelated),
                "DESCENT_AFTER_CLEANUP_RED: actual Run after-observer failure must return through original audio cleanup and preserve primary; "+
                $"actual={afterActual?.GetType().FullName}: {afterActual?.Message}; added={added}; root_valid={GodotObject.IsInstanceValid(mountedFailure)}; unrelated_valid={GodotObject.IsInstanceValid(unrelated)}");
            GD.Print("DESCENT_RESOURCE_BOUNDARY_CONTRACT_OK actual_saved_fixture=true before_after_mount=true observer_failure_owned_root=true after_failure_audio_cleanup=true primary_preserved=true pre_trace_cap_and_scope_veto=true no_added_gc_or_finalizer_drain=true behavior_accepted=false whole_goal=false");
        }
        catch(Exception error){primaryFailure=error;throw;}
        finally
        {
            Console.SetError(saved);
            System.Collections.Generic.List<Exception> cleanupFailures=new();
            foreach(var root in new[]{level,failed,mountedFailure})
            {
                try {await FreeOwnedFixture(tree,root);}
                catch(Exception error){cleanupFailures.Add(error);}
            }
            try {if(GodotObject.IsInstanceValid(unrelated))unrelated.Free();}
            catch(Exception error){cleanupFailures.Add(error);}
            if(cleanupFailures.Count!=0)
            {
                if(primaryFailure!=null)cleanupFailures.Insert(0,primaryFailure);
                throw new AggregateException("Descent standalone contract failed; original failure and owned cleanup failures retained",cleanupFailures);
            }
        }
    }

    private static async Task FreeOwnedFixture(SceneTree tree,PrologueLevelController root)
    {
        if(!GodotObject.IsInstanceValid(root))return;
        if(root.IsInsideTree())await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);
        else root.Free();
    }

    private static void CheckGuard(MethodInfo method)
    {
        var original=ResidentProfileLifetimeTrace.Active;
        foreach(int kind in new[]{0,1,2})
        {
            var journal=new ResidentProfileLifetimeTrace.Journal(_=>{},events:kind==2?4:1);
            using(var restore=ResidentProfileLifetimeTrace.UseJournalForContract(journal))
            {
                IDisposable scope=null;
                if(kind==2)scope=journal.Begin("owned-guard",1,1,"slot","guard-control");
                else {journal.Record(new("one"));if(kind==1)journal.Record(new("two"));}
                TextWriter saved=Console.Error;
                using var output=new StringWriter(CultureInfo.InvariantCulture);
                bool rejected=false;
                Console.SetError(output);
                try {Invoke(method);}
                catch(InvalidOperationException error){rejected=error.Message.StartsWith("PROFILE_LIFETIME_INCOMPLETE",StringComparison.Ordinal);}
                finally {Console.SetError(saved);scope?.Dispose();}
                TestAssert.Equal(kind!=0,rejected,"actual pre guard accepts healthy journal but rejects cap/scopes");
                TestAssert.Equal(kind==0?Complete+System.Environment.NewLine:"",output.ToString(),"failed completeness must never emit success marker");
                if(kind==1)TestAssert.Equal(1,journal.Rows.Length,"second attempted row really exceeds cap1");
                if(kind!=1)journal.AssertComplete();
            }
            TestAssert.True(ReferenceEquals(original,ResidentProfileLifetimeTrace.Active),"contract restores original Active even after deliberate incomplete journal");
            original.AssertComplete();
        }
    }

    private static Snapshot Read(PrologueLevelController level)
    {
        var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var presentation=level.GetNode<PlayerPresentationCoordinator>("PlayerRoot/Player/Presentation");
        var motor=level.GetNode<PlayerMotor>("PlayerRoot/Player/Motor");
        var combat=level.GetNode<PlayerCombat>("PlayerRoot/Player/Combat");
        return new(level.GetInstanceId(),world.GetInstanceId(),world.Definitions.Count,
            presentation.Profile.Task14ManagedSerial,presentation.Profile.GetInstanceId(),motor.Tuning.Task14ManagedSerial,
            motor.Tuning.GetInstanceId(),combat.Tuning.Task14ManagedSerial,combat.Tuning.GetInstanceId());
    }

    private static void CheckRow(string line,Snapshot want,string stage,bool inside)
    {
        using var json=JsonDocument.Parse(line.Substring("DESCENT_ASSIGNED_STARTUP_BOUNDARY ".Length));
        var row=json.RootElement;
        TestAssert.True(row.GetProperty("Stage").GetString()==stage&&row.GetProperty("InsideTree").GetBoolean()==inside&&
            row.GetProperty("RootId").GetUInt64()==want.RootId&&row.GetProperty("WorldId").GetUInt64()==want.WorldId&&
            row.GetProperty("Definitions").GetInt32()==want.Definitions&&want.Definitions==5&&
            row.GetProperty("ProfileSerial").GetInt64()==want.ProfileSerial&&row.GetProperty("ProfileNativeId").GetUInt64()==want.ProfileNativeId&&
            row.GetProperty("MotorSerial").GetInt64()==want.MotorSerial&&row.GetProperty("MotorNativeId").GetUInt64()==want.MotorNativeId&&
            row.GetProperty("CombatSerial").GetInt64()==want.CombatSerial&&row.GetProperty("CombatNativeId").GetUInt64()==want.CombatNativeId,
            "each boundary comes from its own real current fields, not an assumed unchanged Ready binding");
    }

    private static void Invoke(MethodInfo method,params object[] args)
    {
        try {method.Invoke(null,args);}
        catch(TargetInvocationException error)when(error.InnerException!=null)
        {ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
    }

    private sealed class SnapshotFailureOnce(TextWriter fallback,Exception failure,string stage=""):TextWriter
    {
        private int _failed;
        public override Encoding Encoding=>Encoding.UTF8;
        public override void WriteLine(string value)
        {
            if(value?.StartsWith("TASK14_SECTOR_ASSIGNMENT_SNAPSHOT ",StringComparison.Ordinal)==true&&
                (stage.Length==0||value.Contains("\"Stage\":\""+stage+"\"",StringComparison.Ordinal))&&Interlocked.Exchange(ref _failed,1)==0)throw failure;
            fallback.WriteLine(value);
        }
        public override void Write(string value)=>fallback.Write(value);
        public override void Write(char value)=>fallback.Write(value);
        public override void Flush()=>fallback.Flush();
    }
}
#endif
