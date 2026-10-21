using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Shared;

namespace Knight.Tests.Presentation;

// Production sample entry and menu bindings; automated input is not human playtest evidence.
// No spawn, velocity, health, AI, progress or objective injection. This is not the sample loop.
public partial class BasinQualitySampleEntryProbe : SceneTree
{
    private string _output;
    private CharacterBody2D _player;
    private int _damage, _recoveries;

    public override async void _Initialize()
    {
        BasinQualitySampleEntry entry = null;
        PrologueLevelController level = null;
        HealthComponent health = null;
        int exitCode = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Display required.");
            _output = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--output=", StringComparison.Ordinal))?[9..];
            if (string.IsNullOrWhiteSpace(_output) || !Path.IsPathFullyQualified(_output))
                throw new ArgumentException("An absolute --output outside the project is required.");
            _output = Path.GetFullPath(_output);
            string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
            if ((_output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(project, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Evidence must be outside the project.");
            Directory.CreateDirectory(_output);
            Release();
            entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn").Instantiate<BasinQualitySampleEntry>();
            entry.StorageDirectory = Path.Combine(_output, "isolated-slot-" + Guid.NewGuid().ToString("N"));
            Root.AddChild(entry);
            level = entry.GetNode<PrologueLevelController>("Level");
            _player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            health = _player.GetNode<HealthComponent>("Health");
            health.Damaged += OnDamaged;
            level.RecoveryRequested += OnRecovery;
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            await DrawFrames(35);
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State, "production title requires Start");
            await Capture("title");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").EmitSignal(Button.SignalName.Pressed);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "actual Start binding enters play");
            await Capture("started");
            float startX = _player.GlobalPosition.X;
            Send(InputActions.MoveRight, true);
            await Until(() => _player.GlobalPosition.X >= startX + 35f, 1000, "D movement");
            Send(InputActions.MoveRight, false);
            Send(InputActions.MoveLeft, true);
            await Until(() => _player.GlobalPosition.X <= startX + 8f, 1000, "A return movement");
            Send(InputActions.MoveLeft, false);
            await Capture("moved");
            // Pause is consumed in _Process; inject before node processing, not after rendering.
            await ToSignal(this, SignalName.ProcessFrame);
            Send(InputActions.Pause, true);
            GD.Print($"BASIN_SAMPLE_PAUSE_INPUT before_process=True just_pressed={Input.IsActionJustPressed(InputActions.Pause)} state={flow.State} can_process={pause.CanProcess()}");
            await Until(() => flow.State == PrologueGameFlowState.Paused, 1000, "ESC pause");
            Send(InputActions.Pause, false);
            TestAssert.True(Paused && pause.IsOpen, "normal pause owns the tree");
            await DrawFrames(25);
            await Capture("paused");
            level.GetNode<Button>("PauseMenu/Panel/ReturnToTitle").EmitSignal(Button.SignalName.Pressed);
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State, "actual sample title-return binding");
            TestAssert.True(!Paused && !pause.IsOpen, "return clears pause state");
            Vector2 returned = _player.GlobalPosition;
            await DrawFrames(35);
            TestAssert.Equal(returned, _player.GlobalPosition, "title freezes player");
            TestAssert.Equal(0, _damage, "short entry proof takes no unexpected damage");
            TestAssert.Equal(0, _recoveries, "entry proof performs no recovery");
            TestAssert.True(!flow.Save.Completed && !flow.Save.BossDefeated, "entry cannot claim full prologue completion");
            await Capture("returned-title");
            GD.Print("BASIN_SAMPLE_ENTRY_PROOF_OK production_entry=True button_signals=True inputmap_move_pause=True damage=0 recovery=0 complete_loop=False human=False");
            exitCode = 0;
        }
        catch (Exception exception) { GD.PushError("BASIN_SAMPLE_ENTRY_PROOF_FAILED: " + exception); }
        finally
        {
            Release();
            Paused = false;
            if (GodotObject.IsInstanceValid(health)) health.Damaged -= OnDamaged;
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryRequested -= OnRecovery;
                await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
            }
            if (GodotObject.IsInstanceValid(entry))
            {
                entry.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Quit(exitCode);
        }
    }

    private void OnDamaged(int remaining, Vector2 impulse) => _damage++;
    private void OnRecovery(string reason) => _recoveries++;
    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static void Release()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.Pause,
            InputActions.Jump, InputActions.Dash, InputActions.PrimaryAction, InputActions.Shield,
            InputActions.MoveUp, InputActions.MoveDown, InputActions.SelectAttackMode, InputActions.SelectGrappleMode }) Send(action, false);
    }
    private async Task Until(Func<bool> done, ulong milliseconds, string stage)
    {
        ulong end = Time.GetTicksMsec() + milliseconds;
        while (!done() && Time.GetTicksMsec() < end) await DrawFrames(1);
        GD.Print($"BASIN_SAMPLE_ENTRY_STAGE stage={stage} player={_player.GlobalPosition}");
        TestAssert.True(done(), stage + " timed out");
    }
    private async Task DrawFrames(int count)
    {
        for (int index = 0; index < count; index++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }
    private async Task Capture(string stage)
    {
        await DrawFrames(1);
        using Image frame = Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, frame.SavePng(Path.Combine(_output, stage + ".png")), "evidence screenshot");
    }
}
