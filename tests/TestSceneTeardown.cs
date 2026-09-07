using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;

namespace Knight.Tests;

public static class TestSceneTeardown
{
    private const int MaximumAudioDrainFrames = 60;
    private const ulong MaximumAudioDrainMilliseconds = 2000;

    public static async Task DrainAudioOwner(SceneTree tree, Node visibleRoot)
    {
        if (!GodotObject.IsInstanceValid(visibleRoot) || !visibleRoot.IsInsideTree())
            throw new InvalidOperationException("audio drain requires a visible root inside the SceneTree");
        PrologueAudioDirector audio = visibleRoot.GetNodeOrNull<PrologueAudioDirector>("Audio") ??
            throw new InvalidOperationException("audio drain requires the formal AudioDirector owner");
        audio.Shutdown();
        audio.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        double priorMixAge = AudioServer.GetTimeSinceLastMix();
        ulong startedAt = Time.GetTicksMsec();
        ulong deadline = startedAt + MaximumAudioDrainMilliseconds;
        bool observedMix = false;
        int observedAfterFrames = -1;
        for (int frame = 1; frame <= MaximumAudioDrainFrames && Time.GetTicksMsec() <= deadline; frame++)
        {
            await Task.Delay(2);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            double mixAge = AudioServer.GetTimeSinceLastMix();
            if (mixAge < priorMixAge)
            {
                observedMix = true;
                observedAfterFrames = frame;
                break;
            }
            priorMixAge = mixAge;
        }
        if (!observedMix)
            throw new TimeoutException(
                $"AudioServer did not mix after AudioDirector release within " +
                $"{MaximumAudioDrainFrames} frames / {MaximumAudioDrainMilliseconds} ms");
        GD.Print($"PROLOGUE_AUDIO_DRAIN_OK frames={observedAfterFrames} " +
            $"elapsed_ms={Time.GetTicksMsec() - startedAt}");
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(visibleRoot) || !visibleRoot.IsInsideTree())
            throw new InvalidOperationException("audio drain removed the visible scene root");
    }

    public static async Task FreeSceneAfterAudioDrain(SceneTree tree, Node root)
    {
        if (root.GetNodeOrNull<PrologueAudioDirector>("Audio") != null)
            await DrainAudioOwner(tree, root);
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    public static void DrainManagedWrappersBeforeQuit(string capture)
    {
        DrainManagedWrappersBeforeQuit(capture, GD.Print, null);
    }

    /// <summary>
    /// Test-only process-exit drain for a phase-isolated prologue test process.
    /// The marker is emitted only after the sentinel and all pending wrappers have finalized.
    /// </summary>
    public static void DrainManagedWrappersAtProcessExit(string phase)
    {
        if (string.IsNullOrWhiteSpace(phase))
            throw new ArgumentException("test phase is required", nameof(phase));

        ManagedWrapperProcessExitFinalizerProbe.Reset();
        CreateManagedWrapperProcessExitFinalizerProbe();
        DrainManagedWrappersCore();
        int finalized = ManagedWrapperProcessExitFinalizerProbe.Finalized;
        if (finalized != 1)
            throw new InvalidOperationException(
                $"managed wrapper process-exit probe expected 1 finalizer, observed {finalized}");
        GD.Print($"TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED phase={phase} count={finalized}");
        GD.Print($"PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase={phase}");
    }

    internal static void DrainManagedWrappersBeforeQuit(string capture,
        Action<string> successMarker, Action postFinalizersDrained)
    {
        if (string.IsNullOrWhiteSpace(capture))
            throw new ArgumentException("capture name is required", nameof(capture));
        if (successMarker == null)
            throw new ArgumentNullException(nameof(successMarker));

        DrainManagedWrappersCore();
        postFinalizersDrained?.Invoke();
        successMarker($"PROLOGUE_CAPTURE_TEARDOWN_OK capture={capture}");
    }

    public static void AssertManagedWrapperDrainMarkerOrdering()
    {
        ManagedWrapperFinalizerProbe.Reset();
        CreateManagedWrapperFinalizerProbe();
        bool finalizersDrained = false;
        var events = new List<string>();
        DrainManagedWrappersBeforeQuit("finalizer-order", marker =>
        {
            events.Add("marker");
            GD.Print(marker);
            if (!finalizersDrained)
                throw new InvalidOperationException(
                    "capture success marker was emitted before pending finalizers drained");
        }, () =>
        {
            int finalized = ManagedWrapperFinalizerProbe.Finalized;
            if (finalized != 1)
                throw new InvalidOperationException(
                    $"managed finalizer probe expected 1 finalized wrapper, observed {finalized}");
            finalizersDrained = true;
            events.Add("finalizer");
            GD.Print($"TASK4_TEARDOWN_FINALIZER_OBSERVED count={finalized}");
        });
        if (events.Count != 2 || events[0] != "finalizer" || events[1] != "marker")
            throw new InvalidOperationException(
                "capture teardown event order must be finalizer,marker; actual=" +
                string.Join(",", events));
        GD.Print("TASK4_TEARDOWN_ORDER_OK sequence=finalizer,marker finalizers=1");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateManagedWrapperFinalizerProbe()
    {
        _ = new ManagedWrapperFinalizerProbe();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateManagedWrapperProcessExitFinalizerProbe()
    {
        _ = new ManagedWrapperProcessExitFinalizerProbe();
    }

    private static void DrainManagedWrappersCore()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private sealed class ManagedWrapperFinalizerProbe
    {
        private static int _finalized;

        public static int Finalized => Volatile.Read(ref _finalized);

        public static void Reset() => Interlocked.Exchange(ref _finalized, 0);

        ~ManagedWrapperFinalizerProbe()
        {
            Interlocked.Increment(ref _finalized);
        }
    }

    private sealed class ManagedWrapperProcessExitFinalizerProbe
    {
        private static int _finalized;

        public static int Finalized => Volatile.Read(ref _finalized);

        public static void Reset() => Interlocked.Exchange(ref _finalized, 0);

        ~ManagedWrapperProcessExitFinalizerProbe()
        {
            Interlocked.Increment(ref _finalized);
        }
    }
}
