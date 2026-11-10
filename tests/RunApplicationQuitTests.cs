using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Prologue;

namespace Knight.Tests;

// An actual application quit runs in its own admitted headless process.
public partial class RunApplicationQuitTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string mode = args.Single(arg => arg.StartsWith("--quit-case=", StringComparison.Ordinal))[12..];
            TestAssert.True(args.Length == 2 && args.Count(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                mode is "menu" or "window" or "paused" or "contracts" or "guard-fault" or "guard-recovery" or "guard-persistence" && DisplayServer.GetName() == "headless" &&
                Engine.GetWriteMoviePath() == "" && Math.Abs(Engine.TimeScale - 1d) < .000001d,
                "application quit requires exact isolated headless arguments");
            IsolatedPrologueFixture.ValidateStorageRoot();
            if (!Root.IsInsideTree()) await ToSignal(this, SignalName.ProcessFrame);
            if (mode.StartsWith("guard-", StringComparison.Ordinal))
                await Integration.ApplicationQuitRuntimeTests.RunReviewGuard(this, mode);
            if (mode == "contracts")
            {
                Unit.AudioQuitDrainGateTests.Run();
                await Integration.ApplicationQuitRuntimeTests.Run(this);
                await Integration.ApplicationQuitRuntimeTests.WindowLeaseControls(this);
            }
            var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            IsolatedPrologueFixture.Configure(level, "application-quit-" + mode);
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = flow.AutomationAutoStart = false;
            Root.AddChild(level);
            CurrentScene = level;
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var audio = level.GetNode<PrologueAudioDirector>("Audio");
            // Title deliberately selects Silent; use real public audio paths so
            // this exit actually owns outstanding music/UI playback.
            audio.SetMusicState(PrologueMusicState.Exploration);
            audio.Play(PrologueAudioCue.UiConfirm, Vector2.Zero);
            var voices = audio.GetChildren().OfType<AudioStreamPlayer>().ToArray();
            TestAssert.True(voices.Any(voice => voice.Stream != null && voice.Playing),
                "actual quit begins with enabled authored music, not a silent fixture");
            if (mode == "paused")
            {
                flow.StartNew();
                for (int frame = 0; frame < 180 && flow.State != PrologueGameFlowState.Playing; frame++)
                    await ToSignal(this, SignalName.ProcessFrame);
                TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "paused quit begins in a real prepared session");
                level.GetNode<ProloguePauseMenu>("PauseMenu").Open();
                TestAssert.True(Paused && flow.State == PrologueGameFlowState.Paused, "actual pause menu owns pause");
            }
            if (mode == "window")
            {
                TestAssert.True(!AutoAcceptQuit, "APPLICATION_WINDOW_QUIT_RED: application owner must intercept close");
                Root.EmitSignal(Window.SignalName.CloseRequested);
                Root.EmitSignal(Window.SignalName.CloseRequested);
            }
            else if (mode == "paused")
            {
                var quit = level.GetNode<Button>("PauseMenu/Panel/QuitToDesktop");
                quit.EmitSignal(BaseButton.SignalName.Pressed);
                quit.EmitSignal(BaseButton.SignalName.Pressed);
            }
            else
            {
                var quit = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Quit");
                quit.EmitSignal(BaseButton.SignalName.Pressed);
                quit.EmitSignal(BaseButton.SignalName.Pressed);
            }
            // Quit is deferred by SceneTree; inspect the synchronous request boundary.
            TestAssert.True(voices.All(voice => voice.Stream == null && !voice.Playing),
                "APPLICATION_QUIT_NOT_QUIESCED_RED: actual quit must silence/detach every voice before yielding");
            if (mode == "paused")
                TestAssert.True(!flow.TrySaveBeforeQuit(out _),
                    "APPLICATION_QUIT_LATE_SAVE_RED: committed quit cannot accept another save transaction");
            MethodInfo prepare = typeof(PrologueGameFlowController).GetMethod("PrepareApplicationQuitAsync");
            TestAssert.True(prepare != null, "APPLICATION_QUIT_DRAIN_RED: production flow must expose its shared preparation");
            var first = (Task)prepare.Invoke(flow, null);
            TestAssert.True(ReferenceEquals(first, prepare.Invoke(flow, null)),
                "repeated application requests share one preparation task");
            await first;
            object result = first.GetType().GetProperty("Result")!.GetValue(first)!;
            TestAssert.Equal("Ready", result.GetType().GetProperty("State")!.GetValue(result)!.ToString(),
                "normal application quit must await a successful observed drain");
            TestAssert.True(voices.All(voice => voice.Stream == null && !voice.Playing),
                "no voice rearmed while draining actual application exit");
            if (mode == "contracts")
                GD.Print("APPLICATION_QUIT_CONTRACTS_OK gate=true enabled_audio=true owner=true original_leaf_unchanged=true human=false whole_goal=false");
            else GD.Print($"APPLICATION_QUIT_NATIVE_OK mode={mode} authored_menu=true enabled_audio=true duplicate=true observed_drain=true isolated=true human=false whole_goal=false");
            // The production menu/window handler, not this driver, owns successful Quit.
        }
        catch (Exception error)
        {
            GD.PushError("APPLICATION_QUIT_TEST_FAILED: " + error);
            Quit(1);
        }
    }
}
