using System;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

/// <summary>
/// Records the production generated-main Boss sequence. The Task 9 route performs the first
/// attempt, real HealthComponent death, synchronous checkpoint recovery, InputMap re-entry, and
/// final player strike; this presentation driver only observes the completed route.
/// </summary>
public partial class BossRetryPresentationTask6 : SceneTree
{
    private const int CanonicalMovieFrameContract = 2719;
    private const int RawTailProcessFrames = 90;

    public override async void _Initialize()
    {
        BossRetryTrace trace = new(this);
        Task traceTask = trace.Observe();
        try
        {
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            Root.Size = new Vector2I(1280, 720);
            await ToSignal(this, SignalName.ProcessFrame);

            SendAction(InputActions.SelectAttackMode, true);
            await ToSignal(this, SignalName.PhysicsFrame);
            SendAction(InputActions.SelectAttackMode, false);
            GD.Print($"PROLOGUE_BOSS_RETRY_STAGE=route_started frame={Engine.GetPhysicsFrames()}");
            await PrologueTask9GeneratedMainTests.Run(this, captureFrames: false);
            trace.Stop();
            await traceTask;
            trace.AssertCompleted();
            GD.Print("PROLOGUE_BOSS_RETRY_COMPLETE");
            for (int index = 0; index < RawTailProcessFrames; index++)
                await ToSignal(this, SignalName.ProcessFrame);
            GD.Print("TASK6_BOSS_RETRY_RAW_PRODUCER_OK canonical_frames=2719 raw_tail_process_frames=90");
            TestSceneTeardown.DrainManagedWrappersBeforeQuit("task6-boss-retry");
            Quit(0);
        }
        catch (Exception exception)
        {
            trace.Stop();
            try { await traceTask; } catch { /* retain original presentation failure */ }
            GD.PushError("PROLOGUE_BOSS_RETRY_PRESENTATION_FAIL: " + exception);
            Quit(1);
        }
    }

    private static void SendAction(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }

    private sealed class BossRetryTrace
    {
        private readonly SceneTree _tree;
        private PrologueLevelController _level;
        private PrologueCompletionController _completion;
        private bool _stopped;
        private bool _completed;

        public BossRetryTrace(SceneTree tree) => _tree = tree;

        public async Task Observe()
        {
            while (!_stopped)
            {
                AttachGeneratedMain();
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            }
        }

        public void Stop()
        {
            _stopped = true;
            if (GodotObject.IsInstanceValid(_completion))
                _completion.PrologueCompleted -= OnPrologueCompleted;
            _level = null;
            _completion = null;
        }

        public void AssertCompleted()
        {
            if (!_completed)
                throw new InvalidOperationException("Boss retry presentation never observed live completion");
        }

        private void AttachGeneratedMain()
        {
            PrologueLevelController level = FindDescendant<PrologueLevelController>(_tree.Root);
            if (level == null || level.Name != "Task8Fix1GeneratedMainProof" || !level.IsInsideTree() ||
                _level == level)
                return;
            if (GodotObject.IsInstanceValid(_completion))
                _completion.PrologueCompleted -= OnPrologueCompleted;
            _level = level;
            _completion = level.GetNode<PrologueCompletionController>("Completion");
            _completion.PrologueCompleted += OnPrologueCompleted;
        }

        private void OnPrologueCompleted()
        {
            if (_completed) return;
            _completed = true;
            ulong frame = Engine.GetPhysicsFrames();
            GD.Print($"PROLOGUE_BOSS_RETRY_STAGE=completed_real_retry frame={frame}");
            GD.Print($"PROLOGUE_BOSS_RETRY_COMPLETE_FRAME frame={frame}");
        }

        private static T FindDescendant<T>(Node root) where T : Node
        {
            if (root is T typed) return typed;
            foreach (Node child in root.GetChildren())
            {
                T found = FindDescendant<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
