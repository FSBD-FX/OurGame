using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Ui;

namespace Knight.Tests;

// A diagnostic only: external OS-tool pulses, never an input driver or route certificate.
public partial class RunIsolatedKeyboardProbe : SceneTree
{
    public override async void _Initialize()
    {
        PrologueLevelController level = null;
        IsolatedKeyboardObservation observer = null;
        string observationPath = null;
        string failure = null;
        int exitCode = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length is 1 or 2 &&
                args.Count(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                args.All(arg => arg == "--contract-only" || arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)),
                "isolated keyboard probe accepts only external storage and optional contract-only");
            string root = IsolatedPrologueFixture.ValidateStorageRoot();
            level = CreateFixture();
            AssertOffTreeBinding(level, root);
            GD.Print("ISOLATED_KEYBOARD_OFF_TREE_OK channels=3 mounted=false input_injected=false");
            if (args.Contains("--contract-only"))
            {
                var partialObserver = new IsolatedKeyboardObservation();
                partialObserver.Stop(); partialObserver.Stop(); partialObserver.Free();
                exitCode = 0; return;
            }
            AssertLiveConditions();
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            observationPath = Path.Combine(Path.GetDirectoryName(flow.SavePath)!, "observation.json");
            ulong started = Time.GetTicksMsec();
            Root.AddChild(level);
            observer = new IsolatedKeyboardObservation();
            observer.Configure(level);
            Root.AddChild(observer);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State, "formal Title without auto-start");
            var ui = level.GetNode<PrologueUiRoot>("ProductUi");
            TestAssert.True(ui.Visible && !ui.BasinQualitySampleMode && !flow.BasinQualitySampleMode,
                "formal product UI, not Basin sample");
            TestAssert.True(ui.GetNode<Button>("MainMenu/Panel/Buttons/Start").HasFocus(), "formal Start focus");
            TestAssert.Equal(1, level.GetNode("PlayerRoot").GetChildren().OfType<CharacterBody2D>().Count(),
                "one authored actor");
            TestAssert.True(File.Exists(flow.SettingsPath) && !File.Exists(flow.SavePath),
                "Title settings external, no seeded save");
            observer.Note("ready", new { utc = DateTime.UtcNow, sidecar = observationPath });
            GD.Print("ISOLATED_KEYBOARD_READY " + JsonSerializer.Serialize(new { observationPath,
                playerId = level.GetNode<CharacterBody2D>("PlayerRoot/Player").GetInstanceId(),
                inputDriver = false, human = false, windowMs = 120000 }));
            while (Time.GetTicksMsec() - started < 120000)
                await ToSignal(this, SignalName.ProcessFrame);
            observer.Note("window_end", new { utc = DateTime.UtcNow });
            observer.Stop();
            TestAssert.Equal(0, observer.DroppedRows, "complete bounded observation buffer");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            failure = exception.ToString();
            GD.PushError("ISOLATED_KEYBOARD_PROBE_FAILED: " + exception);
        }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(observer))
                {
                    try { observer.Stop(); observer.WriteNew(observationPath, failure); }
                    finally { observer.Free(); }
                }
            }
            catch (Exception exception) { GD.PushError("ISOLATED_KEYBOARD_TEARDOWN_FAILED: " + exception); exitCode = 1; }
            finally
            {
                try
                {
                    if (GodotObject.IsInstanceValid(level))
                    {
                        if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
                        else level.Free();
                    }
                }
                catch (Exception exception) { GD.PushError("ISOLATED_KEYBOARD_AUDIO_TEARDOWN_FAILED: " + exception); exitCode = 1; }
            }
            if (exitCode == 0 && observationPath != null)
                GD.Print("ISOLATED_KEYBOARD_OBSERVATION_END diagnostic_only=true full_goal_accepted=false");
            Quit(exitCode);
        }
    }

    private static void AssertLiveConditions()
    {
        foreach (string arg in OS.GetCmdlineArgs().Concat(OS.GetCmdlineUserArgs()).Concat(System.Environment.GetCommandLineArgs()))
            TestAssert.True(!new[] { "--fixed-fps", "--write-movie", "--headless", "--editor", "-e" }
                .Any(option => arg == option || arg.StartsWith(option + "=", StringComparison.Ordinal)),
                "live probe rejects non-normal runtime: " + arg);
        TestAssert.True(DisplayServer.GetName() != "headless" && string.IsNullOrEmpty(Engine.GetWriteMoviePath()) &&
            Math.Abs(Engine.TimeScale - 1d) < .000001d, "visible normal-time runtime, no movie");
    }

    private static PrologueLevelController CreateFixture()
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, "os-keyboard-probe");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        return level;
    }

    private static void AssertOffTreeBinding(PrologueLevelController level, string storage)
    {
        TestAssert.True(!level.IsInsideTree() && !level.IsNodeReady(), "isolation checked before AddChild");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        string recorder = level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory;
        foreach (string path in new[] { flow.SavePath, flow.SettingsPath, recorder })
            TestAssert.True(Path.IsPathFullyQualified(path) &&
                Path.GetFullPath(path).StartsWith(storage.TrimEnd('/', '\\') + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase), "probe persistence must be external before mount: " + path);
        TestAssert.True(!File.Exists(flow.SavePath) && !File.Exists(flow.SettingsPath) && !Directory.Exists(recorder),
            "off-tree probe cannot write or seed persistence");
        TestAssert.True(!flow.HeadlessAutoStart && !flow.AutomationAutoStart, "no automatic gameplay start");
        TestAssert.Equal(new Vector2(160f, 600f), level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position,
            "exact formal authored spawn, no override");
    }
}

// No calls to gameplay methods, Input action/parse APIs, focus setters or pose setters.
public partial class IsolatedKeyboardObservation : Node
{
    private const int MaximumRows = 30000;
    private readonly List<object> _rows = new();
    private PrologueGameFlowController _flow;
    private ProloguePauseMenu _pause;
    private CharacterBody2D _player;
    private PlayerInputReader _input;
    private PlayerCombat _combat;
    private MechanicalClawController _claw;
    private PlayerShieldController _shield;
    private Window _window;
    private string _lastState;
    private ulong _nextSample;
    private bool _stopped;
    private bool _focusEnteredSubscribed, _focusExitedSubscribed, _sessionSubscribed, _attackSubscribed;
    public int DroppedRows { get; private set; }

    internal void Configure(PrologueLevelController level)
    {
        ProcessMode = ProcessModeEnum.Always;
        ProcessPhysicsPriority = 1000;
        _flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        _pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        _player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        _input = _player.GetNode<PlayerInputReader>("Input");
        _combat = _player.GetNode<PlayerCombat>("Combat");
        _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
        _shield = _player.GetNode<PlayerShieldController>("Shield");
        _window = level.GetTree().Root;
        _window.FocusEntered += OnFocusEntered;
        _focusEnteredSubscribed = true;
        _window.FocusExited += OnFocusExited;
        _focusExitedSubscribed = true;
        _flow.SessionStarted += OnSessionStarted;
        _sessionSubscribed = true;
        _pause.Opened += OnPauseOpened;
        _pause.Closed += OnPauseClosed;
        _combat.AttackStarted += OnAttackStarted;
        _attackSubscribed = true;
        _claw.ClawFrameChanged += OnClawFrame;
        _shield.ShieldFrameChanged += OnShieldFrame;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey key)
            Note("key", new { id = key.GetInstanceId(), logical = key.Keycode.ToString(),
                physical = key.PhysicalKeycode.ToString(), key.Pressed, key.Echo });
    }

    public override void _Notification(int what)
    {
        if (_flow == null) return;
        if (what == NotificationApplicationFocusIn) Note("application_focus_in", null);
        else if (what == NotificationApplicationFocusOut) Note("application_focus_out", null);
    }

    public override void _Process(double delta)
    {
        string state = _flow.State.ToString();
        if (state == _lastState) return;
        _lastState = state;
        Note("flow", new { state, paused = GetTree().Paused, menu = _pause.IsOpen });
    }

    public override void _PhysicsProcess(double delta)
    {
        PlayerInputFrame frame = _input.CurrentFrame;
        ulong now = Time.GetTicksMsec();
        bool edge = frame.JumpPressed || frame.DashPressed || frame.AttackPressed || frame.ClawPressed || frame.InteractPressed;
        if (!edge && now < _nextSample) return;
        _nextSample = now + 100;
        Note("sample", new { sequence = _input.CurrentFrameSequence, frame.MoveAxis, frame.JumpPressed,
            frame.JumpHeld, frame.DashPressed, frame.AttackPressed, frame.ClawPressed, frame.InteractPressed,
            frame.ShieldHeld, frame.KeyboardActions, frame.RequestedSlot, position = Vec(_player.GlobalPosition),
            velocity = Vec(_player.Velocity), floor = _player.IsOnFloor(), attacking = _combat.IsAttacking,
            shield = _shield.IsActive, claw = _claw.CurrentFrame.State.ToString() });
    }

    internal void Note(string kind, object data)
    {
        if (_stopped) return;
        if (_rows.Count >= MaximumRows) { DroppedRows++; return; }
        _rows.Add(new { kind, ms = Time.GetTicksMsec(), process = Engine.GetProcessFrames(),
            physics = Engine.GetPhysicsFrames(), focus = _window?.HasFocus() ?? false,
            actor = _player?.GetInstanceId() ?? 0, data });
    }

    internal void Stop()
    {
        if (_stopped) return;
        _stopped = true;
        if (GodotObject.IsInstanceValid(_window))
        {
            if (_focusEnteredSubscribed) _window.FocusEntered -= OnFocusEntered;
            if (_focusExitedSubscribed) _window.FocusExited -= OnFocusExited;
        }
        if (_sessionSubscribed && GodotObject.IsInstanceValid(_flow)) _flow.SessionStarted -= OnSessionStarted;
        if (GodotObject.IsInstanceValid(_pause)) { _pause.Opened -= OnPauseOpened; _pause.Closed -= OnPauseClosed; }
        if (_attackSubscribed && GodotObject.IsInstanceValid(_combat)) _combat.AttackStarted -= OnAttackStarted;
        if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClawFrame;
        if (GodotObject.IsInstanceValid(_shield)) _shield.ShieldFrameChanged -= OnShieldFrame;
        SetProcess(false); SetPhysicsProcess(false); SetProcessInput(false);
    }

    internal void WriteNew(string path, string failure)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(stream, new { diagnosticOnly = true, human = false, physicalHardwareOriginProved = false,
            wholeGoalAccepted = false, inputDriver = false, toolMayActivateTargetWindow = true,
            rowCount = _rows.Count, droppedRows = DroppedRows, failure, rows = _rows });
    }

    private static float[] Vec(Vector2 value) => new[] { value.X, value.Y };
    private void OnFocusEntered() => Note("window_focus_in", null);
    private void OnFocusExited() => Note("window_focus_out", null);
    private void OnSessionStarted() => Note("session_started", null);
    private void OnPauseOpened() => Note("pause_opened", null);
    private void OnPauseClosed() => Note("pause_closed", null);
    private void OnAttackStarted(ulong id, int direction) => Note("attack_started", new { id, direction });
    private void OnShieldFrame(bool active) => Note("shield_frame", new { active });
    private void OnClawFrame(MechanicalClawFrame frame, GrappleSurface2D target) => Note("claw_frame",
        new { frame.CastId, state = frame.State.ToString(), failure = frame.Failure.ToString(),
            frame.LockedTargetRuntimeId, frame.Arrived, dock = Vec(frame.DockPoint) });
}
