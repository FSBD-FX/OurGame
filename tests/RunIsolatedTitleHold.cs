using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Ui;

namespace Knight.Tests;

// Display check only. No input recorder, synthetic input, foreground activation or timer.
public partial class RunIsolatedTitleHold : SceneTree
{
    public override async void _Initialize()
    {
        IsolatedTitleHoldSession session = null;
        int result = 1;
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            bool contractOnly = args.Contains("--contract-only");
            TestAssert.True(args.Count(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                args.Length == (contractOnly ? 2 : 1) &&
                args.All(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal) || arg == "--contract-only"),
                "title hold accepts one storage root and optional headless contract flag");
            TestAssert.True((DisplayServer.GetName() == "headless") == contractOnly && Engine.GetWriteMoviePath() == "" &&
                Math.Abs(Engine.TimeScale - 1d) < .000001d &&
                !OS.GetCmdlineArgs().Any(arg => arg is "--fixed-fps" or "--write-movie" or "--editor" or "-e" ||
                    arg.StartsWith("--fixed-fps=", StringComparison.Ordinal)), "title hold requires a normal live runtime");
            if (contractOnly)
            {
                GD.Print($"ISOLATED_TITLE_ENTRY_BEGIN root_inside_tree={Root.IsInsideTree()} headless_contract=true user_visible=false");
                await RunContract(this);
                GD.Print("ISOLATED_TITLE_ENTRY_OK standalone=true starts=0 human=false whole_goal=false");
                result = 0;
                return;
            }
            session = new IsolatedTitleHoldSession(this);
            await session.Mount();
            GD.Print("ISOLATED_TITLE_READY user_visible=false human=false auto_start=false auto_exit=false input_driver=false");
            while (!session.CloseRequested)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                session.AssertTitle();
            }
            result = 0;
        }
        catch (Exception error) { GD.PushError("ISOLATED_TITLE_FAILED: " + error); }
        finally
        {
            try { if (session != null) await session.Close(); }
            catch (Exception error) { GD.PushError("ISOLATED_TITLE_CLEANUP_FAILED: " + error); result = 1; }
            GD.Print($"ISOLATED_TITLE_END result={result} human=false whole_goal=false");
            Quit(result);
        }
    }

    internal static async Task RunContract(SceneTree tree)
    {
        for (int mode = 0; mode < 2; mode++)
        {
            var session = new IsolatedTitleHoldSession(tree);
            PrologueLevelController level = null;
            try
            {
                await session.Mount();
                level = session.Level;
                for (int frame = 0; frame < 180; frame++)
                {
                    await tree.ToSignal(tree, SignalName.PhysicsFrame);
                    session.AssertTitle();
                    TestAssert.True(!session.CloseRequested, "title hold cannot end without a close request");
                }
                var ui = session.Ui;
                ui.ShowSettings(session.Flow.Settings); ui.HandleBack();
                ui.ShowControls(); ui.HandleBack();
                session.AssertTitle();
                ui.GetNode<Button>("MainMenu/Panel/Buttons/Start").EmitSignal(BaseButton.SignalName.Pressed);
                ui.GetNode<Button>("MainMenu/Panel/Buttons/Continue").EmitSignal(BaseButton.SignalName.Pressed);
                await tree.ToSignal(tree, SignalName.ProcessFrame);
                session.AssertTitle();
                TestAssert.True(!File.Exists(session.Paths.SavePath) && !Directory.Exists(session.Paths.PlaytestsDirectory),
                    "title-only UI cannot seed a save or playtest");
                if (mode == 0) tree.Root.EmitSignal(Window.SignalName.CloseRequested);
                else ui.GetNode<Button>("MainMenu/Panel/Buttons/Quit").EmitSignal(BaseButton.SignalName.Pressed);
                tree.Root.EmitSignal(Window.SignalName.CloseRequested);
                TestAssert.True(session.CloseRequested && session.AcceptedCloseRequests == 1,
                    "window and menu close share one idempotent request");
            }
            finally { await session.Close(); }
            TestAssert.True(!GodotObject.IsInstanceValid(level), "owned title level released after audio drain");
        }
        GD.Print("ISOLATED_TITLE_CONTRACT_OK mounts=2 hold_physics=180 channels=3 menus=true starts=0 closes=2 headless_contract=true user_visible=false human=false whole_goal=false");
    }
}

internal sealed class IsolatedTitleHoldSession
{
    private readonly SceneTree _tree;
    private readonly bool _originalAutoQuit;
    private bool _closed, _closeSubscribed;
    private int _sessions;
    internal PrologueLevelController Level { get; private set; }
    internal PrologueGameFlowController Flow { get; private set; }
    internal PrologueUiRoot Ui { get; private set; }
    internal IsolatedPrologueFixturePaths Paths { get; private set; }
    internal bool CloseRequested { get; private set; }
    internal int AcceptedCloseRequests { get; private set; }

    internal IsolatedTitleHoldSession(SceneTree tree) { _tree = tree; _originalAutoQuit = tree.AutoAcceptQuit; }

    internal async Task Mount()
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        // SceneTree._Initialize runs before its root enters the tree. Waiting
        // here (before instantiation) preserves the synchronous Ready -> lock
        // interval below for both the standalone entry and mounted callers.
        if (!_tree.Root.IsInsideTree())
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(_tree.Root.IsInsideTree(), "title mount requires an entered root");
        Level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        Paths = IsolatedPrologueFixture.Configure(Level, "title-hold");
        Flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
        Ui = Level.GetNode<PrologueUiRoot>("ProductUi");
        Flow.HeadlessAutoStart = false;
        Flow.AutomationAutoStart = false;
        TestAssert.True(!Level.IsInsideTree() && !File.Exists(Paths.SavePath) && !File.Exists(Paths.SettingsPath) &&
            !Directory.Exists(Paths.PlaytestsDirectory), "title storage isolated before Ready");
        _tree.AutoAcceptQuit = false;
        _tree.Root.CloseRequested += RequestClose;
        _closeSubscribed = true;
        Flow.SessionStarted += OnSession;
        _tree.Root.AddChild(Level);
        TestAssert.True(Level.IsNodeReady() && Ui.IsNodeReady() && Flow.IsNodeReady(),
            "title handlers must finish Ready before synchronous locking");
        // Ready handlers have bound the authored UI. Do this synchronously before
        // yielding so no external key/click can start a session during setup.
        Ui.StartRequested -= Flow.StartNew;
        Ui.ContinueRequested -= Flow.Continue;
        Ui.GetNode<Button>("MainMenu/Panel/Buttons/Start").Disabled = true;
        Ui.GetNode<Button>("MainMenu/Panel/Buttons/Continue").Disabled = true;
        var quit = Ui.GetNode<Button>("MainMenu/Panel/Buttons/Quit");
        var connections = quit.GetSignalConnectionList(BaseButton.SignalName.Pressed);
        GD.Print($"ISOLATED_TITLE_MOUNT_BOUNDARY root_inside_tree={_tree.Root.IsInsideTree()} level_inside_tree={Level.IsInsideTree()} level_ready={Level.IsNodeReady()} ui_ready={Ui.IsNodeReady()} quit_bindings={connections.Count}");
        TestAssert.Equal(1, connections.Count, "authored quit button has one owned UI binding");
        quit.Disconnect(BaseButton.SignalName.Pressed, connections[0]["callable"].AsCallable());
        quit.Pressed += RequestClose;
        await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
        await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
        AssertTitle();
    }

    internal void AssertTitle()
    {
        TestAssert.True(GodotObject.IsInstanceValid(Level) && Level.IsInsideTree() &&
            Flow.State == PrologueGameFlowState.Title && _sessions == 0 && !Level.ResidentPreparationPending,
            "isolated title must not auto-start");
        TestAssert.True(Flow.SavePath == Paths.SavePath && Flow.SettingsPath == Paths.SettingsPath &&
            Level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory == Paths.PlaytestsDirectory,
            "title retains all three isolated persistence paths");
        TestAssert.True(Ui.GetNode<Button>("MainMenu/Panel/Buttons/Start").Disabled &&
            Ui.GetNode<Button>("MainMenu/Panel/Buttons/Continue").Disabled,
            "display fixture cannot start a game after menu navigation");
        int pages = new[] { "MainMenu", "SettingsMenu", "ControlsView" }
            .Count(path => Ui.GetNode<Control>(path).Visible);
        TestAssert.Equal(1, pages, "one title/settings/controls page visible");
    }

    private void OnSession() => _sessions++;
    private void RequestClose()
    {
        if (CloseRequested || _closed) return;
        CloseRequested = true;
        AcceptedCloseRequests++;
    }

    internal async Task Close()
    {
        if (_closed) return;
        _closed = true;
        try
        {
            if (_closeSubscribed) _tree.Root.CloseRequested -= RequestClose;
            if (GodotObject.IsInstanceValid(Flow)) Flow.SessionStarted -= OnSession;
            if (GodotObject.IsInstanceValid(Level))
            {
                Level.CancelResidentPreparation();
                if (Level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, Level);
                else Level.Free();
            }
        }
        finally { _tree.AutoAcceptQuit = _originalAutoQuit; }
    }
}
