using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Tests;

namespace Knight.Tests.Diagnostics;

// One ordinary, isolated product-menu exit. A successful exit belongs only to the product button.
public partial class RunTask14ProductQuitDiagnostic : SceneTree
{
    private const string ScenePath = "res://scenes/prologue/PrologueLevel.tscn";
    private const string StorageArgument = "--case-storage-root=";

    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 1 || !args[0].StartsWith(StorageArgument, StringComparison.Ordinal))
                throw new InvalidOperationException("product-quit diagnostic requires exactly one --case-storage-root");
            string storage = args[0][StorageArgument.Length..];
            if (!Path.IsPathFullyQualified(storage) || !Directory.Exists(storage))
                throw new InvalidOperationException("product-quit storage must be an existing absolute directory");
#if TASK14_DISPOSE_IDENTIFICATION
            throw new InvalidOperationException("product-quit diagnostic requires the ordinary unflagged build");
#else
            GD.Print("TASK14_PRODUCT_QUIT_ORDINARY_BUILD=true");
#endif
            PackedScene packed = GD.Load<PackedScene>(ScenePath) ??
                throw new InvalidOperationException($"normal packed scene missing: {ScenePath}");
            PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
            IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(level, "task14-product-quit");
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            if (player.Position != new Vector2(160f, 600f))
                throw new InvalidOperationException($"authored player spawn changed: {player.Position}");
            ulong playerId = player.GetInstanceId();
            CanvasLayer productUi = level.GetNode<CanvasLayer>("ProductUi");
            Button start = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start");
            ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            Button quit = level.GetNode<Button>("PauseMenu/Panel/QuitToDesktop");
            bool pressedObserved = false;
            bool closedObserved = false;
            pause.Opened += () => GD.Print(
                $"TASK14_PRODUCT_QUIT_PAUSE_OPENED player_id={playerId} menu_open={pause.IsOpen} tree_paused={Paused} flow_state={flow.State}");
            quit.Pressed += () =>
            {
                pressedObserved = true;
                GD.Print($"TASK14_PRODUCT_QUIT_BUTTON_PRESSED player_id={playerId}");
            };
            pause.Closed += () =>
            {
                closedObserved = true;
                GD.Print($"TASK14_PRODUCT_QUIT_PAUSE_CLOSED player_id={playerId}");
            };
            Root.AddChild(level);
            GD.Print($"TASK14_PRODUCT_QUIT_MOUNTED scene={ScenePath} case_root={paths.CaseRoot} player_id={playerId}");

            await WaitUntil(() => flow.State == PrologueGameFlowState.Title && productUi.Visible &&
                start.Visible && start.HasFocus(), "visible focused Title", 120);
            AssertPlayer(level, player, playerId);
            GD.Print($"TASK14_PRODUCT_QUIT_TITLE player_id={playerId} focused={start.HasFocus()}");
            await Capture(paths.CaseRoot, "01-title");

            GD.Print("TASK14_PRODUCT_QUIT_START_INPUT_PRESS");
            SendAction("ui_accept", true);
            await NextDraw();
            SendAction("ui_accept", false);
            GD.Print("TASK14_PRODUCT_QUIT_START_INPUT_RELEASE");
            await WaitUntil(() => flow.State == PrologueGameFlowState.Playing && !productUi.Visible,
                "Playing with hidden title", 120);
            AssertPlayer(level, player, playerId);
            GD.Print($"TASK14_PRODUCT_QUIT_PLAYING player_id={playerId} player_count={level.GetNode("PlayerRoot").GetChildCount()}");
            for (int frame = 0; frame < 15; frame++) await NextDraw();
            if (flow.State != PrologueGameFlowState.Playing)
                throw new InvalidOperationException($"Playing did not persist during native frames: {flow.State}");
            await Capture(paths.CaseRoot, "02-playing");

            await ToSignal(this, SignalName.ProcessFrame);
            GD.Print("TASK14_PRODUCT_QUIT_ESCAPE_INPUT_PRESS");
            SendEscape(true);
            await WaitUntil(() => pause.IsOpen && Paused && flow.State == PrologueGameFlowState.Paused,
                "real paused menu", 120);
            SendEscape(false);
            GD.Print("TASK14_PRODUCT_QUIT_ESCAPE_INPUT_RELEASE");
            AssertPlayer(level, player, playerId);
            if (!quit.IsVisibleInTree() || quit.Disabled)
                throw new InvalidOperationException("authored Quit button is not interactable");
            GD.Print($"TASK14_PRODUCT_QUIT_PAUSED player_id={playerId} tree_paused={Paused} quit_rect={quit.GetGlobalRect()}");
            await Capture(paths.CaseRoot, "03-paused-before-quit");

            Vector2 point = quit.GetGlobalRect().GetCenter();
            if (!quit.GetGlobalRect().HasPoint(point))
                throw new InvalidOperationException("Quit button target is outside its actual packed bounds");
            GD.Print($"TASK14_PRODUCT_QUIT_MOUSE_TARGET x={point.X} y={point.Y}");
            SendMouseMotion(point);
            await NextDraw();
            SendMouseButton(point, true);
            GD.Print("TASK14_PRODUCT_QUIT_MOUSE_PRESS_SENT");
            await NextDraw();
            GD.Print("TASK14_PRODUCT_QUIT_MOUSE_RELEASE_SENDING");
            SendMouseButton(point, false);
            GD.Print($"TASK14_PRODUCT_QUIT_MOUSE_RELEASE_RETURNED pressed={pressedObserved} closed={closedObserved}");
            for (int frame = 0; frame < 120; frame++) await NextDraw();
            throw new InvalidOperationException(
                $"product Quit button did not exit within 120 rendered frames; pressed={pressedObserved} closed={closedObserved}");
        }
        catch (Exception exception)
        {
            GD.PushError($"TASK14_PRODUCT_QUIT_FAILURE {exception}");
            Quit(1);
        }
    }

    private async Task WaitUntil(Func<bool> condition, string phase, int maxDrawFrames)
    {
        for (int frame = 0; frame < maxDrawFrames; frame++)
        {
            if (condition()) return;
            await NextDraw();
        }
        throw new InvalidOperationException($"timed out waiting for {phase} after {maxDrawFrames} drawn frames");
    }

    private async Task Capture(string caseRoot, string name)
    {
        string output = Path.Combine(caseRoot, name + ".png");
        if (File.Exists(output)) throw new IOException($"refusing to overwrite screenshot: {output}");
        await NextDraw();
        using Image image = Root.GetTexture().GetImage();
        if (image.GetWidth() != 1280 || image.GetHeight() != 720)
            throw new InvalidOperationException($"{name} viewport is {image.GetWidth()}x{image.GetHeight()}, expected 1280x720");
        Error result = image.SavePng(output);
        if (result != Error.Ok) throw new IOException($"{name} SavePng failed: {result}");
        GD.Print($"TASK14_PRODUCT_QUIT_SCREENSHOT phase={name} path={output} size=1280x720");
    }

    private async Task NextDraw() =>
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

    private static void AssertPlayer(PrologueLevelController level, CharacterBody2D player, ulong playerId)
    {
        if (!GodotObject.IsInstanceValid(player) || player.GetInstanceId() != playerId ||
            level.GetNode("PlayerRoot").GetChildCount() != 1 ||
            level.GetNode<CharacterBody2D>("PlayerRoot/Player") != player)
            throw new InvalidOperationException("normal packed Player was replaced or duplicated");
    }

    private void SendAction(StringName action, bool pressed)
    {
        var input = new InputEventAction { Action = action, Pressed = pressed,
            Strength = pressed ? 1f : 0f };
        Root.PushInput(input, true);
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }

    private void SendEscape(bool pressed)
    {
        var input = new InputEventKey { PhysicalKeycode = Key.Escape, Pressed = pressed };
        if (pressed)
        {
            bool mapsPause = InputMap.EventIsAction(input, Knight.Shared.InputActions.Pause);
            GD.Print($"TASK14_PRODUCT_QUIT_ESCAPE_EVENT device={input.Device} physical={input.PhysicalKeycode} pressed={input.Pressed} maps_pause={mapsPause}");
            if (!mapsPause)
                throw new InvalidOperationException(
                    $"physical Escape event does not map to pause: device={input.Device} physical={input.PhysicalKeycode}");
        }
        Root.PushInput(input, true);
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
        if (pressed)
        {
            bool actionPressed = Input.IsActionPressed(Knight.Shared.InputActions.Pause);
            bool justPressed = Input.IsActionJustPressed(Knight.Shared.InputActions.Pause);
            GD.Print($"TASK14_PRODUCT_QUIT_ESCAPE_ACTION action_pressed={actionPressed} just_pressed={justPressed}");
            if (!actionPressed || !justPressed)
                throw new InvalidOperationException(
                    $"mapped physical Escape did not register pause press: action_pressed={actionPressed} just_pressed={justPressed}");
        }
    }

    private void SendMouseMotion(Vector2 point)
    {
        var input = new InputEventMouseMotion { Position = point, GlobalPosition = point };
        Root.PushInput(input, true);
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }

    private void SendMouseButton(Vector2 point, bool pressed)
    {
        var input = new InputEventMouseButton { ButtonIndex = MouseButton.Left,
            Pressed = pressed, Position = point, GlobalPosition = point,
            ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0 };
        Root.PushInput(input, true);
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }
}
